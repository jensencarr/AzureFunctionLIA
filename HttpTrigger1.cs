using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.Azure.Functions.Worker.Http;
using Newtonsoft.Json.Linq;
using Microsoft.AspNetCore.WebUtilities;

namespace WeatherApplicationLIA.Functions
{
    public static class GetWeatherTemperatureNowFunction
    {
        [Function("GetWeatherTemperatureNow")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = null)] HttpRequestData req,
            FunctionContext context)
        {
            var logger = context.GetLogger("GetWeatherTemperatureNowFunction");
            logger.LogInformation("Processing weather request.");

            // Läser plats från query-parametern i URL
            var queryParams = QueryHelpers.ParseQuery(req.Url.Query);
            string location = queryParams.ContainsKey("location") ? queryParams["location"].ToString() : null;

            if (string.IsNullOrEmpty(location))
            {
                return new BadRequestObjectResult("Please provide a location in the query string!");
            }

            // Skapa en instans av WeatherService och hämta koordinater
            var weatherService = new WeatherService();
            var (lat, lon) = WeatherService.GetCoordinates(location);

            try
            {
                // Hämta väderdata
                var weatherData = await weatherService.GetWeatherDataAsync("pmp3g", "2", lon, lat);
                var temperature = WeatherService.ParseTemperatureNow(weatherData);

                // Returnera bara temperaturen
                return new OkObjectResult($"{temperature} °C");
            }
            catch (HttpRequestException ex)
            {
                logger.LogError($"Error fetching weather data: {ex.Message}");
                return new BadRequestObjectResult("Error fetching weather data.");
            }
        }
    }
}