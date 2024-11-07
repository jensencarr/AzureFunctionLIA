using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

public class WeatherService
{
    private readonly HttpClient _httpClient;

    public WeatherService()
    {
        _httpClient = new HttpClient();
    }

    public async Task<string> GetWeatherDataAsync(string category, string version, double longitude, double latitude)
    {
        string url = $"/api/category/{category}/version/{version}/geotype/point/lon/{longitude.ToString(System.Globalization.CultureInfo.InvariantCulture)}/lat/{latitude.ToString(System.Globalization.CultureInfo.InvariantCulture)}/data.json";
        HttpResponseMessage response = await _httpClient.GetAsync("https://opendata-download-metfcst.smhi.se" + url);
        Console.WriteLine($"Built URL: https://opendata-download-metfcst.smhi.se{url}");

        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadAsStringAsync();
        }
        else
        {
            throw new HttpRequestException($"Error fetching weather data: {response.StatusCode}");
        }
    }

    public static (double lat, double lon) GetCoordinates(string location)
        {
            return location.ToLower() switch
            {
                "stockholm" => (59.3293, 18.0686),
                "gothenburg" => (57.7089, 11.9746),
                "malmo" => (55.6050, 13.0038),
                "uppsala" => (59.8586, 17.6389),
                _ => (59.3293, 18.0686) 
            };
        }

        public static double ParseTemperatureNow(string weatherData)
        {
            double temperature = 0.0;

            try
            {
                var json = JObject.Parse(weatherData);
                var timeSeries = json["timeSeries"];

                if (timeSeries != null && timeSeries.Any())
                {
                    var currentTimeSeries = timeSeries.First();

                    foreach (var parameter in currentTimeSeries["parameters"])
                    {
                        if (parameter["name"]?.ToString() == "t") // Temperatur
                        {
                            temperature = parameter["values"]?[0]?.Value<double>() ?? 0.0;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error parsing weather data: {ex.Message}");
            }

            return temperature;
        }
}