
using System;
using System.Collections.Generic;
using Amazon.SecretsManager.Model;


namespace DynamoDbDemo.Configuration
{
    public class SecretValueContext
    {
        public string Name { get; set; }

        public Dictionary<string, List<string>> VersionsToStages { get; set; }

        public SecretValueContext(SecretListEntry secret)
        {
            if (secret == null)
            {
                throw new ArgumentNullException("secret");
            }

            Name = secret.Name;
            VersionsToStages = secret.SecretVersionsToStages;
        }
    }
}
