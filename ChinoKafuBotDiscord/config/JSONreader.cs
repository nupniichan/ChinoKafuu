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
        public string osuToken { get; set; }
        public string gemini_folder_path { get; set; }
        public ulong allowChannelID_gemini { get; set; }
        public string python_dll_path { get; set; }

        public async Task ReadJson()
        {
            using (StreamReader sr = new StreamReader("config.json"))
            {
                string json = await sr.ReadToEndAsync();
                JsonStructer data = JsonConvert.DeserializeObject<JsonStructer>(json);
                this.token = data.token;
                this.prefix = data.prefix;
                this.osuToken = data.osuToken;
                this.gemini_folder_path = data.gemini_folder_path;
                this.allowChannelID_gemini = data.allowChannelID_gemini;
                this.python_dll_path = data.python_dll_path;
            }
        }
        public async Task ReadJsonToken()
        {
            using (StreamReader sr = new StreamReader("otherconfig.json"))
            {
                string json = await sr.ReadToEndAsync();
                JsonStructer data = JsonConvert.DeserializeObject<JsonStructer>(json);
                this.osuToken = data.osuToken;
                this.gemini_folder_path = data.gemini_folder_path;
                this.allowChannelID_gemini = data.allowChannelID_gemini;
                this.python_dll_path = data.python_dll_path;
            }
        }
    }

    public class JsonStructer
    {
        public string token { get; set; }
        public string prefix { get; set; }
        public string osuToken { get; set; }
        public string gemini_folder_path { get; set; }
        public ulong allowChannelID_gemini { get; set; }
        public string python_dll_path { get; set; }

    }
}
