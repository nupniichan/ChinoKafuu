using WeatherAPI.Models;

public class WeatherData
{
    public string Date { get; set; }
    public string Hour { get; set; }
    public string Minutes { get; set; }
    public Coord coord { get; set; }
    public Weather weather { get; set; }
    public DetailsWeather main { get; set; }
    public int Visibility { get; set; }
    public Wind wind { get; set; }
    public Clouds clouds { get; set; }
}
