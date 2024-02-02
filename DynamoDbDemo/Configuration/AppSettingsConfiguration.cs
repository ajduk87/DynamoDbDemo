namespace DynamoDbDemo.Configuration
{
    public class AppSettingsConfiguration
    {
        public bool GenerateAppSettings { get; set; }

        public string? FileName { get; set; }

        public string? FilePath { get; set; }

        public AppSettingsConfiguration(bool generateAppSettings, string? fileName = null, string? filePath = null)
        {
            GenerateAppSettings = generateAppSettings;
            FileName = fileName;
            FilePath = filePath;
        }
    }
}
