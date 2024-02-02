
using System;
using System.Collections.Generic;
using Amazon.SecretsManager;
using Amazon.SecretsManager.Model;

namespace DynamoDbDemo.Configuration
{
    public class SecretsManagerConfigurationProviderOptions
    {
        public List<string> AcceptedSecretArns { get; set; } = new List<string>();


        public Func<SecretListEntry, bool> SecretFilter { get; set; } = (SecretListEntry _) => true;


        public List<Filter> ListSecretsFilters { get; set; } = new List<Filter>();


        public Func<SecretListEntry, string, string> KeyGenerator { get; set; } = (SecretListEntry _, string key) => key;


        public Action<GetSecretValueRequest, SecretValueContext> ConfigureSecretValueRequest { get; set; } = delegate
        {
        };


        public Action<AmazonSecretsManagerConfig> ConfigureSecretsManagerConfig { get; set; } = delegate
        {
        };


        public Func<IAmazonSecretsManager>? CreateClient { get; set; }

        public TimeSpan? PollingInterval { get; set; }

        public AppSettingsConfiguration? AppSettingsConfiguration { get; set; }

    }
}
