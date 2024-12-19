using WeatherAPI.Models;

public class WeatherData
{
    public string localTime { get; set; }
    public int timezoneOffset { get; set; }
    public Coord coord { get; set; }
    public Weather weather { get; set; }
    public DetailsWeather main { get; set; }
    public int Visibility { get; set; }
    public Wind wind { get; set; }
    public Clouds clouds { get; set; }
}
