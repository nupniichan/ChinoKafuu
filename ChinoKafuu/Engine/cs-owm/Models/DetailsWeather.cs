using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WeatherAPI.Models
{
    public class DetailsWeather
    {
        public float temp { get; set; }
        public float feels_like { get; set; }
        public float pressure { get; set; }
        public float humidity { get; set; }
        public float sea_level { get; set; }
        public float grnd_level { get; set; }
    }
}
