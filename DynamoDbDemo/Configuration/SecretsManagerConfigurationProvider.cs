using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Amazon.SecretsManager;
using Amazon.SecretsManager.Model;
using Microsoft.Extensions.Configuration;


namespace DynamoDbDemo.Configuration
{
    public class SecretsManagerConfigurationProvider : ConfigurationProvider, IDisposable
    {
        private HashSet<(string, string?)> _loadedValues = new HashSet<(string, string)>();

        private Task? _pollingTask;

        private CancellationTokenSource? _cancellationToken;

        private bool _isDisposed;

        public SecretsManagerConfigurationProviderOptions Options { get; }

        public IAmazonSecretsManager Client { get; }

        public SecretsManagerConfigurationProvider(IAmazonSecretsManager client, SecretsManagerConfigurationProviderOptions options)
        {
            Options = options ?? throw new ArgumentNullException("options");
            Client = client ?? throw new ArgumentNullException("client");
        }

        public override void Load()
        {
            LoadAsync().ConfigureAwait(continueOnCapturedContext: false).GetAwaiter().GetResult();
        }

        public Task ForceReloadAsync(CancellationToken cancellationToken)
        {
            return ReloadAsync(cancellationToken);
        }

        private async Task LoadAsync()
        {
            _loadedValues = await FetchConfigurationAsync(default(CancellationToken)).ConfigureAwait(continueOnCapturedContext: false);
            SetData(_loadedValues, triggerReload: false);
            if (Options.PollingInterval.HasValue)
            {
                _cancellationToken = new CancellationTokenSource();
                _pollingTask = PollForChangesAsync(Options.PollingInterval.Value, _cancellationToken.Token);
            }
        }

        private async Task PollForChangesAsync(TimeSpan interval, CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                await Task.Delay(interval, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
                await ReloadAsync(cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
            }
        }

        private async Task ReloadAsync(CancellationToken cancellationToken)
        {
            HashSet<(string, string?)> oldValues = _loadedValues;
            HashSet<(string, string?)> newValues = await FetchConfigurationAsync(cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
            if (!oldValues.SetEquals(newValues))
            {
                _loadedValues = newValues;
                SetData(_loadedValues, triggerReload: true);
            }
        }

        private static bool TryParseJson(string data, out JsonElement? jsonElement)
        {
            jsonElement = null;
            data = data.TrimStart();
            char c = data.FirstOrDefault();
            if (c.Equals('[') && c.Equals('{'))
            {
                return false;
            }

            try
            {
                using JsonDocument jsonDocument = JsonDocument.Parse(data);
                jsonElement = jsonDocument.RootElement.Clone();
                return true;
            }
            catch (JsonException)
            {
                return false;
            }
        }

        private static IEnumerable<(string key, string value)> ExtractValues(JsonElement? jsonElement, string prefix)
        {
            if (!jsonElement.HasValue)
            {
                yield break;
            }

            if (jsonElement.Value.ValueKind.Equals(JsonValueKind.Array))
            {
                int currentIndex = 0;
                foreach (JsonElement el in jsonElement.Value.EnumerateArray())
                {
                    foreach (var (key2, value2) in ExtractValues(prefix: $"{prefix}{ConfigurationPath.KeyDelimiter}{currentIndex}", jsonElement: el))
                    {
                        yield return (key2, value2);
                    }

                    currentIndex++;
                }
            }
            else if (jsonElement.Value.ValueKind.Equals(JsonValueKind.Number))
            {
                yield return (prefix, jsonElement.Value.GetRawText());
            }
            else if (jsonElement.Value.ValueKind.Equals(JsonValueKind.String))
            {
                yield return (prefix, jsonElement.Value.GetString() ?? "");
            }
            else if (jsonElement.Value.ValueKind.Equals(JsonValueKind.True) || jsonElement.Value.ValueKind.Equals(JsonValueKind.False))
            {
                yield return (prefix, jsonElement.Value.GetBoolean().ToString());
            }
            else if (jsonElement.Value.ValueKind.Equals(JsonValueKind.Object))
            {
                foreach (JsonProperty property in jsonElement.Value.EnumerateObject())
                {
                    foreach (var (key, value) in ExtractValues(prefix: prefix + ConfigurationPath.KeyDelimiter + property.Name, jsonElement: property.Value))
                    {
                        yield return (key, value);
                    }
                }
            }
            else
            {
                if (!jsonElement.Value.ValueKind.Equals(JsonValueKind.Null) && !jsonElement.Value.ValueKind.Equals(JsonValueKind.Undefined))
                {
                    throw new FormatException("Unsupported json token");
                }

                yield return (prefix, string.Empty);
            }
        }

        private void SetData(IEnumerable<(string, string?)> values, bool triggerReload)
        {
            base.Data = values.ToDictionary<(string, string), string, string>(((string, string) x) => x.Item1, ((string, string) x) => x.Item2, StringComparer.InvariantCultureIgnoreCase);
            if (triggerReload)
            {
                OnReload();
            }
        }

        private async Task<IReadOnlyList<SecretListEntry>> FetchAllSecretsAsync(CancellationToken cancellationToken)
        {
            ListSecretsResponse response = null;
            if (Options.AcceptedSecretArns.Count > 0)
            {
                return Options.AcceptedSecretArns.Select((string x) => new SecretListEntry
                {
                    ARN = x,
                    Name = x
                }).ToList();
            }

            List<SecretListEntry> result = new List<SecretListEntry>();
            do
            {
                string nextToken = response?.NextToken;
                ListSecretsRequest request = new ListSecretsRequest
                {
                    NextToken = nextToken,
                    Filters = Options.ListSecretsFilters
                };
                response = await Client.ListSecretsAsync(request, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
                result.AddRange(response.SecretList);
            }
            while (response.NextToken != null);
            return result;
        }

        private async Task<HashSet<(string, string?)>> FetchConfigurationAsync(CancellationToken cancellationToken)
        {
            IReadOnlyList<SecretListEntry> secrets = await FetchAllSecretsAsync(cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
            HashSet<(string, string?)> configuration = new HashSet<(string, string)>();
            foreach (SecretListEntry secret in secrets)
            {
                try
                {
                    if (!Options.SecretFilter(secret))
                    {
                        continue;
                    }

                    GetSecretValueRequest request = new GetSecretValueRequest
                    {
                        SecretId = secret.ARN
                    };
                    Options.ConfigureSecretValueRequest?.Invoke(request, new SecretValueContext(secret));
                    GetSecretValueResponse secretValue = await Client.GetSecretValueAsync(request, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
                    SecretListEntry secretEntry = ((Options.AcceptedSecretArns.Count > 0) ? new SecretListEntry
                    {
                        ARN = secret.ARN,
                        Name = secretValue.Name,
                        CreatedDate = secretValue.CreatedDate
                    } : secret);
                    string secretName = secretEntry.Name;
                    string secretString = secretValue.SecretString;
                    if (secretString == null)
                    {
                        continue;
                    }

                    if (TryParseJson(secretString, out var jElement))
                    {
                        HandleAppSettingsFile(secretString);
                        IEnumerable<(string key, string value)> values = ExtractValues(jElement, secretName);
                        foreach (var item in values)
                        {
                            string key = item.key;
                            string value = item.value;
                            string configurationKey2 = Options.KeyGenerator(secretEntry, key);
                            configuration.Add((configurationKey2, value));
                        }
                    }
                    else
                    {
                        string configurationKey = Options.KeyGenerator(secretEntry, secretName);
                        configuration.Add((configurationKey, secretString));
                    }

                    jElement = null;
                }
                catch (ResourceNotFoundException ex)
                {
                    throw new MissingSecretValueException(exception: ex, errorMessage: "Error retrieving secret value (Secret: " + secret.Name + " Arn: " + secret.ARN + ")", secretName: secret.Name, secretArn: secret.ARN);
                }
            }

            return configuration;
        }

        private void HandleAppSettingsFile(string? secretString)
        {
            if (string.IsNullOrWhiteSpace(secretString))
            {
                return;
            }

            AppSettingsConfiguration? appSettingsConfiguration = Options.AppSettingsConfiguration;
            if (appSettingsConfiguration != null && appSettingsConfiguration.GenerateAppSettings)
            {
                string path = Directory.GetCurrentDirectory();
                string path2 = "appsettings.json";
                File.WriteAllText(Path.Combine(path, path2), secretString);
                if (!string.IsNullOrWhiteSpace(Options.AppSettingsConfiguration?.FilePath))
                {
                    path = Options.AppSettingsConfiguration?.FilePath;
                }

                if (!string.IsNullOrWhiteSpace(Options.AppSettingsConfiguration?.FileName))
                {
                    path2 = Options.AppSettingsConfiguration?.FileName;
                }

                File.WriteAllText(Path.Combine(path, path2), secretString);
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_isDisposed)
            {
                if (disposing)
                {
                    _cancellationToken?.Cancel();
                    _cancellationToken = null;
                    _pollingTask?.GetAwaiter().GetResult();
                    _pollingTask = null;
                }

                _isDisposed = true;
            }
        }
    }
}
