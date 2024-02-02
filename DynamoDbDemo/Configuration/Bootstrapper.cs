using Amazon;
using System;
using Amazon;
using Amazon.Runtime;
using Microsoft.Extensions.Configuration;
using System.Net;
using Amazon.DynamoDBv2.DataModel;
using Amazon.DynamoDBv2;

namespace DynamoDbDemo.Configuration
{
    public static class Bootstrapper
    {
        public static WebApplicationBuilder AddComponents(this WebApplicationBuilder builder) 
        {
            var applicationName = builder.Environment.ApplicationName;
            var environmentName = builder.Environment.EnvironmentName;
            var configurationPrefix = string.Empty;

            builder.AddConfiguration(out configurationPrefix);

            builder.Services.AddControllers();

            builder.Services.AddCors(cors =>
            {
                cors.AddPolicy("Policy", options =>
                {
                    options.AllowAnyMethod()
                           .AllowAnyOrigin()
                           .AllowAnyHeader();
                });
            });

            builder.Services.ConfigureExaiConfiguration(builder.Configuration, configurationPrefix)
                            .AddEndpointsApiExplorer();
            //.AddAWSServices(builder.Configuration, configurationPrefix);

            // Configure App Configuration
            builder.Configuration
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);

            var accessKey = builder.Configuration["AWS:AwsAccessKeyId"];
            var secretKey = builder.Configuration["AWS:AwsSecretAccessKey"];

            var regionName = builder.Configuration["AWS:Region"];
            RegionEndpoint regionEndpoint = RegionEndpoint.GetBySystemName(regionName);

            var credentials = new BasicAWSCredentials(accessKey, secretKey);
            var config = new AmazonDynamoDBConfig()
            {
                RegionEndpoint = regionEndpoint
            };

            var client = new AmazonDynamoDBClient(credentials,config);
            builder.Services.AddSingleton<IAmazonDynamoDB>(client);
            builder.Services.AddSingleton<IDynamoDBContext, DynamoDBContext>();

            return builder;
        }

      

        private static IServiceCollection AddAWSServices(this IServiceCollection services, IConfiguration configuration, string configurationPrefix)
        {
            var options = configuration.GetAWSOptions(configurationPrefix);
            services.AddDefaultAWSOptions(options)
                    .AddAWSService<IAmazonDynamoDB>();

            services.AddScoped<IDynamoDBContext, DynamoDBContext>(provider =>
            {
                var dynamoDbConfig = new DynamoDBContextConfig
                {
                    //TableNamePrefix = configuration.GetValue<string>($"{configurationPrefix}WeatherForecast_")
                    TableNamePrefix = string.Empty
                };

                var dynamoDbClient = provider.GetService<IAmazonDynamoDB>();
                return new DynamoDBContext(dynamoDbClient, dynamoDbConfig);
            })
                .AddScoped<AmazonDynamoDBClient>();


            return services;
        }


        private static IServiceCollection ConfigureExaiConfiguration(this IServiceCollection services, IConfiguration configuration, string configurationPrefix)
        {
            services.Configure<ExaiConfiguration>(string.IsNullOrWhiteSpace(configurationPrefix) ? configuration : configuration.GetSection(configurationPrefix.TrimEnd(new[] { ':' })));

            return services;
        }


        private static WebApplicationBuilder AddConfiguration(this WebApplicationBuilder builder, out string configurationPrefix)
        {
            var appsettingsFileExists = File.Exists(path: $"{Directory.GetCurrentDirectory()}/appsettings.{builder.Environment.EnvironmentName}.json");

            if (appsettingsFileExists)
            {
                builder.Configuration.SetBasePath(builder.Environment.ContentRootPath)
                                     .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                                     .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: false)
                                     .AddEnvironmentVariables();

                configurationPrefix = string.Empty;

                return builder;
            }

            builder.Configuration.AddSecretsManager(region: RegionEndpoint.USWest1, configurator: options =>
            {
                var secret = $"{builder.Environment.EnvironmentName}/{builder.Environment.ApplicationName}/";
                options.SecretFilter = entry => entry.Name.StartsWith(secret);
                options.KeyGenerator = (_, secretName) => secretName.Replace(secret, string.Empty);
                options.AppSettingsConfiguration = new(
                    generateAppSettings: GetEnvironmentVariableAsBool("GENERATE_APP_SETTINGS"),
                    fileName: $"appsettings.{builder.Environment.EnvironmentName}.json",
                    filePath: Directory.GetCurrentDirectory()
                );
            });

            configurationPrefix = $"{nameof(ExaiConfiguration)}:";

            return builder;
        }

        private static IConfigurationBuilder AddSecretsManager(this IConfigurationBuilder configurationBuilder, AWSCredentials? credentials = null, RegionEndpoint? region = null, Action<SecretsManagerConfigurationProviderOptions>? configurator = null)
        {
            SecretsManagerConfigurationProviderOptions secretsManagerConfigurationProviderOptions = new SecretsManagerConfigurationProviderOptions();
            configurator?.Invoke(secretsManagerConfigurationProviderOptions);
            SecretsManagerConfigurationSource secretsManagerConfigurationSource = new SecretsManagerConfigurationSource(credentials, secretsManagerConfigurationProviderOptions);
            if (region != null)
            {
                secretsManagerConfigurationSource.Region = region;
            }

            configurationBuilder.Add(secretsManagerConfigurationSource);
            return configurationBuilder;
        }

        public static bool GetEnvironmentVariableAsBool(string variableName)
        {
            string environmentVariable = Environment.GetEnvironmentVariable(variableName);
            if (string.IsNullOrWhiteSpace(environmentVariable))
            {
                throw new Exception("Environment variable '" + variableName + "' is not found.");
            }

            switch (environmentVariable.ToLowerInvariant())
            {
                case "true":
                case "yes":
                case "1":
                    return true;
                case "false":
                case "no":
                case "0":
                    return false;
                default:
                    throw new Exception("Environment variable '" + variableName + "' is not boolean value.");
            }
        }

    }
}
