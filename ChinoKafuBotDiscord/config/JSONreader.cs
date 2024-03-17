using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChinoBot.config
{
    public class JSONreader
    {
        public string token { get; set; }
        public string prefix { get; set; }

        public string saucenaoToken { get; set; }

        public string osuToken { get; set; }
        public string cAIToken { get; set; }
        public async Task ReadJson()
        {
            using (StreamReader sr = new StreamReader("config.json"))
            {
                string json = await sr.ReadToEndAsync();
                JsonStructer data = JsonConvert.DeserializeObject<JsonStructer>(json);
                this.token = data.token;
                this.prefix = data.prefix;
            }
        }
        public async Task ReadJsonToken()
        {
            using (StreamReader sr = new StreamReader("token.json"))
            {
                string json = await sr.ReadToEndAsync();
                JsonStructer data = JsonConvert.DeserializeObject<JsonStructer>(json);
                this.saucenaoToken = data.saucenaoToken;
                this.osuToken = data.osuToken;
            }
        }
    }

    public class JsonStructer
    {
        public string token { get; set; }
        public string prefix { get; set; }

        public string saucenaoToken { get; set; }

        public string osuToken { get; set; }
    }
}
