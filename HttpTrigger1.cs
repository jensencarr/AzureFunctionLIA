using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.Azure.Functions.Worker.Http;
using Newtonsoft.Json.Linq;

namespace WeatherApplicationLIA.Functions
{
    public static class GetWeatherData
    {



[Function("GetTemperatureNow")]
public static async Task<IActionResult> GetTemperatureNow(
    [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = null)] HttpRequestData req,
    FunctionContext context)
{
    var logger = context.GetLogger("GetWeatherTemperatureNowFunction");
    logger.LogInformation("Processing weather request.");

    // Läs bodyn och tolka den som JSON
    string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
    var data = JObject.Parse(requestBody);
    string location = data["location"]?.ToString();
    string scale = data["scale"]?.ToString()?.ToUpper(); // Ny parameter för temperaturskala

    if (string.IsNullOrEmpty(location))
    {
        return new BadRequestObjectResult("Please provide a location in the body!");
    }

    // Skapa en instans av WeatherService och hämta koordinater
    var weatherService = new WeatherService();
    var (lat, lon) = WeatherService.GetCordinates(location);

    try
    {
        // Hämta väderdata
        var weatherData = await weatherService.GetWeatherDataAsync("pmp3g", "2", lon, lat);
        var temperatureCelsius = WeatherService.GetTemperatureNow(weatherData);

        // Konvertera till Fahrenheit om det begärs
        if (scale == "F")
        {
            var temperatureFahrenheit = WeatherService.ConvertCelsiusToFahrenheit(temperatureCelsius);
            return new OkObjectResult($"{temperatureFahrenheit:F1} °F");
        }
        else
        {
            return new OkObjectResult($"{temperatureCelsius:F1} °C");
        }
        
    }
    catch (HttpRequestException ex)
    {
        logger.LogError($"Error fetching weather data: {ex.Message}");
        return new BadRequestObjectResult("Error fetching weather data.");
    }
}



 [Function("GetTemperatureForSelectedDate")]
public static async Task<IActionResult> GetTemperatureForSelectedDate(
    [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = null)] HttpRequestData req,
    FunctionContext context)
{
    var logger = context.GetLogger("GetTemperatureForDateFunction");
    logger.LogInformation("Processing temperature request for a specific date.");

    string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
    var data = JObject.Parse(requestBody);
    
    string location = data["location"]?.ToString();
    string date = data["date"]?.ToString();
    string parameters = data["parameters"]?.ToString();
    string scale = data["scale"]?.ToString()?.ToUpper() ?? "C"; // Standard till Celsius om ej angivet

    if (string.IsNullOrEmpty(location) || string.IsNullOrEmpty(date))
    {
        return new BadRequestObjectResult("Please provide both 'location' and 'date' in the body.");
    }

    var weatherService = new WeatherService();

    try
    {
        var result = await weatherService.GetWeatherDataForDateAsync(location, date, parameters, scale);
        return new OkObjectResult(result);
    }
    catch (ArgumentException ex)
    {
        logger.LogError(ex.Message);
        return new BadRequestObjectResult(ex.Message);
    }
    catch (HttpRequestException ex)
    {
        logger.LogError($"Error fetching weather data: {ex.Message}");
        return new BadRequestObjectResult("Error fetching weather data.");
    }
}





[Function("GetDailyAverages")]
public static async Task<IActionResult> GetAverageWeaterDataForDate(
    [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = null)] HttpRequestData req,
    FunctionContext context)
{
    var logger = context.GetLogger("GetDailyAveragesFunction");
    logger.LogInformation("Processing daily averages request.");

    string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
    var data = JObject.Parse(requestBody);

    string location = data["location"]?.ToString();
    string date = data["date"]?.ToString();
    string scale = data["scale"]?.ToString()?.ToUpper() ?? "C"; // Standard till Celsius om ej angivet

    if (string.IsNullOrEmpty(location) || string.IsNullOrEmpty(date))
    {
        return new BadRequestObjectResult("Please provide both 'location' and 'date' in the body.");
    }

    var weatherService = new WeatherService();

    try
    {
        var result = await weatherService.GetDailyAveragesAsync(location, date);

        // Konvertera temperaturen om skalan är Fahrenheit
        if (scale == "F")
        {
            result = WeatherService.ConvertTemperatureInResult(result, scale);
        }

        return new OkObjectResult(result);
    }
    catch (ArgumentException ex)
    {
        logger.LogError(ex.Message);
        return new BadRequestObjectResult(ex.Message);
    }
    catch (HttpRequestException ex)
    {
        logger.LogError($"Error fetching weather data: {ex.Message}");
        return new BadRequestObjectResult("Error fetching weather data.");
    }
}



    }
}