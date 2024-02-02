using Amazon;
using Amazon.Runtime;
using Amazon.SecretsManager;

namespace DynamoDbDemo.Configuration
{
    public class SecretsManagerConfigurationSource : IConfigurationSource
    {
        public SecretsManagerConfigurationProviderOptions Options { get; }

        public AWSCredentials? Credentials { get; }

        public RegionEndpoint? Region { get; set; }

        public SecretsManagerConfigurationSource(AWSCredentials? credentials = null, SecretsManagerConfigurationProviderOptions? options = null)
        {
            Credentials = credentials;
            Options = options ?? new SecretsManagerConfigurationProviderOptions();
        }

        public IConfigurationProvider Build(IConfigurationBuilder builder)
        {
            IAmazonSecretsManager client = CreateClient();
            return new SecretsManagerConfigurationProvider(client, Options);
        }

        private IAmazonSecretsManager CreateClient()
        {
            if (Options.CreateClient != null)
            {
                return Options.CreateClient();
            }

            AmazonSecretsManagerConfig amazonSecretsManagerConfig = new AmazonSecretsManagerConfig
            {
                RegionEndpoint = Region
            };
            Options.ConfigureSecretsManagerConfig(amazonSecretsManagerConfig);
            AWSCredentials credentials = Credentials;
            if (1 == 0)
            {
            }

            AmazonSecretsManagerClient result = ((credentials != null) ? new AmazonSecretsManagerClient(Credentials, amazonSecretsManagerConfig) : new AmazonSecretsManagerClient(amazonSecretsManagerConfig));
            if (1 == 0)
            {
            }

            return result;
        }
    }
}
