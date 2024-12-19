using Newtonsoft.Json;
using WeatherAPI.Models;

public class WeatherService
{
    private readonly HttpClient _httpClient = new HttpClient();
    private readonly string _openWeatherMapAPIKey;

    public WeatherService(string openWeatherMapAPIKey)
    {
        _openWeatherMapAPIKey = openWeatherMapAPIKey;
    }

    public async Task<WeatherData> GetWeatherDataAsync(string location)
    {
        try
        {
            var geoResponse = await _httpClient.GetStringAsync($"http://api.openweathermap.org/geo/1.0/direct?q={location}&limit=1&appid={_openWeatherMapAPIKey}");
            dynamic geoData = JsonConvert.DeserializeObject(geoResponse);

            if (geoData == null || geoData.Count == 0)
            {
                throw new Exception("Can't find location.");
            }

            var lat = geoData[0].lat;
            var lon = geoData[0].lon;

            var weatherResponse = await _httpClient.GetStringAsync($"https://api.openweathermap.org/data/2.5/weather?lat={lat}&lon={lon}&appid={_openWeatherMapAPIKey}&units=metric");
            dynamic weatherDataResponse = JsonConvert.DeserializeObject(weatherResponse);

            if (weatherDataResponse == null)
            {
                throw new Exception("Can't find weather data.");
            }

            int unixTime = weatherDataResponse.dt;
            int timeZoneOffset = weatherDataResponse.timezone;

            var utcTime = DateTimeOffset.FromUnixTimeSeconds(unixTime).UtcDateTime;
            var localTime = utcTime.AddSeconds(timeZoneOffset);

            return new WeatherData
            { 
                coord = new Coord
                {
                    Latitude = weatherDataResponse.coord.lat,
                    Longitude = weatherDataResponse.coord.lon
                },
                weather = new Weather
                {
                    id = weatherDataResponse.weather[0].id,
                    main = weatherDataResponse.weather[0].main,
                    description = weatherDataResponse.weather[0].description,
                    icon = weatherDataResponse.weather[0].icon
                },
                main = new DetailsWeather
                {
                    temp = weatherDataResponse.main.temp,
                    feels_like = weatherDataResponse.main.feels_like,
                    pressure = weatherDataResponse.main.pressure,
                    humidity = weatherDataResponse.main.humidity,
                    sea_level = weatherDataResponse.main.sea_level ?? 0,
                    grnd_level = weatherDataResponse.main.grnd_level ?? 0
                },
                Visibility = weatherDataResponse.visibility,
                wind = new Wind
                {
                    speed = weatherDataResponse.wind.speed,
                    deg = weatherDataResponse.wind.deg,
                    gust = weatherDataResponse.wind.gust ?? 0
                },
                clouds = new Clouds
                {
                    all = weatherDataResponse.clouds.all
                },
                timezoneOffset = timeZoneOffset, 
                localTime = localTime.ToString("dd-MM-yyyy HH:mm:ss") 
            };
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error fetching weather data: " + ex.Message);
            return null;
        }
    }
    public string GetWeatherIconUrl(string iconCode)
    {
        const string baseUrl = "https://openweathermap.org/img/wn/";
        return $"{baseUrl}{iconCode}@2x.png";
    }
}
