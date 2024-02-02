using Amazon.DynamoDBv2.DataModel;
using System.Text.Json.Serialization;

namespace DynamoDbDemo
{

    [DynamoDBTable(nameof(WeatherForecast))]
    public class WeatherForecast
    {
        [DynamoDBHashKey(nameof(City))]
        public string City { get; set; }
        [DynamoDBRangeKey(nameof(Date))]
        public DateTime Date { get; set; }
        [DynamoDBProperty(nameof(TemperatureC))]
        public int TemperatureC { get; set; }

        public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
        [DynamoDBProperty(nameof(Summary))]
        public string? Summary { get; set; }
        public Wind Wind { get; set; }
    }

    public class Wind
    {
        public decimal Speed { get; set; }
        public string Direction { get; set; }
    }
}
