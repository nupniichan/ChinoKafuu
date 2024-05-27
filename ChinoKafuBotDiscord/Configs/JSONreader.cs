using Newtonsoft.Json;

namespace ChinoBot.config
{
    public sealed class JSONreader
    {
        public string token { get; set; }
        public string prefix { get; set; }
        public string osuToken { get; set; }
        public string geminiAPIKey { get; set; }
        public string geminiTranslateAPIKey { get; set; }
        public string gemini_folder_path { get; set; }
        public ulong allowChannelID_gemini { get; set; }
        public string python_dll_path { get; set; }
        public string userDefaultRoleName { get; set; }
        public string applioPath { get; set; }
        public string resultApplioFilePath { get; set; }
        public string ffmpegPath { get; set; }
        public async Task ReadJson()
        {
            using (StreamReader sr = new StreamReader("..//..//..//Configs//config.json"))
            {
                string json = await sr.ReadToEndAsync();
                JsonStructer data = JsonConvert.DeserializeObject<JsonStructer>(json);
                this.token = data.token;
                this.prefix = data.prefix;
                this.osuToken = data.osuToken;
                this.geminiAPIKey = data.geminiAPIKey;
                this.geminiTranslateAPIKey = data.geminiTranslateAPIKey;
                this.gemini_folder_path = data.gemini_folder_path;
                this.allowChannelID_gemini = data.allowChannelID_gemini;
                this.python_dll_path = data.python_dll_path;
                this.userDefaultRoleName = data.userDefaultRoleName;
                this.applioPath = data.applioPath;
                this.resultApplioFilePath = data.resultApplioFilePath;
                this.ffmpegPath = data.ffmpegPath;
            }
        }
    }

    public sealed class JsonStructer
    {
        public string token { get; set; }
        public string prefix { get; set; }
        public string osuToken { get; set; }
        public string geminiAPIKey { get; set; }
        public string geminiTranslateAPIKey { get; set; }
        public string gemini_folder_path { get; set; }
        public ulong allowChannelID_gemini { get; set; }
        public string python_dll_path { get; set; }
        public string userDefaultRoleName { get; set; }
        public string applioPath { get; set; }
        public string resultApplioFilePath { get; set; }
        public string ffmpegPath { get; set; }
    }
}
