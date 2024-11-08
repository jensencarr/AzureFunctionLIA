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

    public static (double lat, double lon) GetCordinates(string location)
{
    return location switch
    {
        "stockholm" => (59.3293, 18.0686),
        "göteborg" => (57.7089, 11.9746),
        "malmö" => (55.6050, 13.0038),
        "uppsala" => (59.8586, 17.6389),
        "västerås" => (59.6162, 16.5528),
        "örebro" => (59.2741, 15.2066),
        "helsingborg" => (56.0465, 12.6945),
        "jönköping" => (57.7815, 14.1562),
        "norrköping" => (58.5877, 16.1924),
        "lund" => (55.7047, 13.1910),
        "umeå" => (63.8258, 20.2630),
        "gävle" => (60.6749, 17.1413),
        "kiruna" => (67.8557, 20.2253),
        "örnsköldsvik" => (63.2909, 18.7152),
    };
}

        public static double GetTemperatureNow(string weatherData)
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
        public async Task<string> GetWeatherDataForDateAsync(string location, string date, string parameter = null)
{
    var (lat, lon) = GetCordinates(location);
    var weatherData = await GetWeatherDataAsync("pmp3g", "2", lon, lat);
    var json = JObject.Parse(weatherData);
    var timeSeries = json["timeSeries"];

    if (!DateTime.TryParseExact(date, "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.AssumeUniversal, out var selectedDate))
    {
        throw new ArgumentException("Fel format på datum. Använd formatet yyyy-MM-dd.");
    }

    var dataForSelectedDate = timeSeries
        .Where(ts =>
        {
            var time = DateTime.Parse(ts["validTime"].ToString());
            return time.Date == selectedDate.Date && time.TimeOfDay >= TimeSpan.FromHours(8);
        })
        .Take(14)
        .Select(ts =>
        {
            var time = ts["validTime"].ToString();
            var temperature = ts["parameters"].FirstOrDefault(p => p["name"].ToString() == "t")?["values"]?[0]?.Value<double>() ?? 0.0;
            var windSpeed = ts["parameters"].FirstOrDefault(p => p["name"].ToString() == "ws")?["values"]?[0]?.Value<double>() ?? 0.0;
            var windDirection = ts["parameters"].FirstOrDefault(p => p["name"].ToString() == "wd")?["values"]?[0]?.Value<double>() ?? 0.0;
            var precipitation = ts["parameters"].FirstOrDefault(p => p["name"].ToString() == "pmean")?["values"]?[0]?.Value<double>() ?? 0.0;

            return (Time: time, Temperature: temperature, WindSpeed: windSpeed, WindDirection: windDirection, Precipitation: precipitation);
        })
        .ToList();

    if (!dataForSelectedDate.Any())
    {
        return "Ingen data|wi-na";
    }

    // Filtrera data baserat på specificerad parameter
    var filteredData = dataForSelectedDate.Select(d =>
    {
        return parameter?.ToLower() switch
        {
            "temperature" => $"\nGenomsnittlig temperatur för {location}\n{d.Time} \n{d.Temperature:F1} °C",
            "windspeed" => $"\nGenomsnittlig vindhastighet för {location}\n{d.Time}: \n{d.WindSpeed:F1} m/s",
            "winddirection" => $"\nGenomsnittlig vindriktning för {location}\n{d.Time}: \n{d.WindDirection:F1} grader",
            "precipitation" => $"\nGenomsnittlig nederbörd för {location}\n{d.Time}: \n{d.Precipitation:F1} mm",
            _ => $"Genomsnittlig väderdata för {location} den {d.Time}:\n" +
                 $"Temperatur: {d.Temperature:F1} °C\n" +
                 $"Vindhastighet: {d.WindSpeed:F1} m/s\n" +
                 $"Vindriktning: {d.WindDirection:F1} grader\n" +
                 $"Nederbörd: {d.Precipitation:F1} mm\n"
        };
    });

    return string.Join("\n", filteredData);
}

public async Task<string> GetDailyAveragesAsync(string location, string date)
{
    var (lat, lon) = GetCordinates(location);
    var weatherData = await GetWeatherDataAsync("pmp3g", "2", lon, lat);
    var json = JObject.Parse(weatherData);
    var timeSeries = json["timeSeries"];

    if (!DateTime.TryParseExact(date, "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.AssumeUniversal, out var selectedDate))
    {
        throw new ArgumentException("Fel format på datum. Använd formatet yyyy-MM-dd.");
    }

    var dataForSelectedDate = timeSeries
        .Where(ts =>
        {
            var time = DateTime.Parse(ts["validTime"].ToString());
            return time.Date == selectedDate.Date;
        })
        .ToList();

    if (!dataForSelectedDate.Any())
    {
        return "Ingen data";
    }

    double totalTemperature = 0.0;
    double totalWindSpeed = 0.0;
    double totalprecipitation = 0.0;
    int count = dataForSelectedDate.Count();

    foreach (var ts in dataForSelectedDate)
    {
        var temperature = ts["parameters"].FirstOrDefault(p => p["name"].ToString() == "t")?["values"]?[0]?.Value<double>() ?? 0.0;
        var windSpeed = ts["parameters"].FirstOrDefault(p => p["name"].ToString() == "ws")?["values"]?[0]?.Value<double>() ?? 0.0;
        var precipitation = ts["parameters"].FirstOrDefault(p => p["name"].ToString() == "pmean")?["values"]?[0]?.Value<double>() ?? 0.0;

        totalTemperature += temperature;
        totalWindSpeed += windSpeed;
        totalprecipitation += precipitation;
    }

    // Beräkna genomsnitt
    var avgTemperature = totalTemperature / count;
    var avgWindSpeed = totalWindSpeed / count;
    var avgPrecipitation = totalprecipitation / count;

    // Returnera resultatet som en formaterad textsträng
    return $"Genomsnittlig väderdata för {location} den {date}:\n" +
       $"Temperatur: {avgTemperature:F1} °C\n" +
       $"Vindhastighet: {avgWindSpeed:F1} m/s\n" +
       $"Nederbörd: {avgPrecipitation:F1} mm";
}
        
}