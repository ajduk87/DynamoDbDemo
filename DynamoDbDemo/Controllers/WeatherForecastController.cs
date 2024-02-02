using Amazon.DynamoDBv2.DataModel;
using Amazon.DynamoDBv2.DocumentModel;
using Microsoft.AspNetCore.Mvc;

namespace DynamoDbDemo.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class WeatherForecastController : ControllerBase
    {
        private static readonly string[] Summaries = new[]
        {
            "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
        };

        private readonly ILogger<WeatherForecastController> _logger;
        private IDynamoDBContext _dynamoDBContext;

        public WeatherForecastController(ILogger<WeatherForecastController> logger,
                                         IDynamoDBContext dynamoDBContext)
        {
            _logger = logger;
            _dynamoDBContext = dynamoDBContext;
        }

        //[HttpGet(Name = "GetWeatherForecast")]
        //public async Task<IEnumerable<WeatherForecast>> Get(string city = "Belgrade")
        //{

        //    return await _dynamoDBContext.QueryAsync<WeatherForecast>(city)
        //                                 .GetRemainingAsync();
        //}

        [HttpGet(Name = "GetWeatherForecastBetween")]
        public async Task<IEnumerable<WeatherForecast>> GetBeetwen(DateTime startDate, DateTime endDate, string city = "Belgrade")
        {

            return await _dynamoDBContext.QueryAsync<WeatherForecast>(city, QueryOperator.Between, new object[] { startDate, endDate })
                                         .GetRemainingAsync();
        }

        [HttpPut]
        public async Task Post(string city, DateTime date)
        {
           var specificItem = await _dynamoDBContext.LoadAsync<WeatherForecast>(city, date);
            specificItem.Summary = "Test";
            await _dynamoDBContext.SaveAsync(specificItem);
        }

        [HttpPost]
        public async Task Post(WeatherForecast weather) 
        {
            //var data = GenerateDummyWeatherForecast(city);
            //foreach (var weather in data) 
            //{
            //    await _dynamoDBContext.SaveAsync(weather);
            //}
            await _dynamoDBContext.SaveAsync(weather);
        }

        private static IEnumerable<WeatherForecast> GenerateDummyWeatherForecast(string city) 
        {
            var rng = new Random();
            return Enumerable.Range(1, 5).Select(index => new WeatherForecast
            {
                City = city,
                Date = DateTime.Now.AddDays(index),
                TemperatureC = rng.Next(-20, 55),
                Summary = Summaries[rng.Next(Summaries.Length)]

            }) ;

        }
    }
}
