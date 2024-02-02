using Newtonsoft.Json;


namespace DynamoDbDemo.Configuration
{
    public record ExaiConfiguration
    {
        #region Properties

        [JsonProperty("AWS")] public Aws? Aws { get; init; }
        [JsonProperty("TablePrefix")] public string TablePrefix { get; init; }
        [JsonProperty("ConnectionStrings")] public ConnectionStrings? ConnectionStrings { get; init; }

        #endregion
    }

    public record Aws
    {
        #region Properties

        [JsonProperty("AwsAccessKeyId")] public string AwsAccessKeyId { get; init; } = null!;
        [JsonProperty("AwsSecretAccessKey")] public string AwsSecretAccessKey { get; init; } = null!;
        [JsonProperty("Bucket")] public string Bucket { get; init; } = null!;
        [JsonProperty("Region")] public string Region { get; init; } = null!;

        #endregion
    }

    public record ConnectionStrings
    {
        #region Properties

        [JsonProperty("Person")] public string Person { get; init; } = null!;

        #endregion
    }
}
