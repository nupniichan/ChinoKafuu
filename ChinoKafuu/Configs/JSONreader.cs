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
        public string conversationHistory { get; set; }
        public async Task ReadJson()
        {
            string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
            string relativePath = Path.Combine(baseDirectory, "..", "..", "..", "Configs", "config.json");

            try
            {
                using (StreamReader sr = new StreamReader(relativePath))
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
                    this.conversationHistory = data.conversationHistory;
                }
            }
            catch (FileNotFoundException)
            {
                Console.WriteLine("Không tìm thấy file config.json");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Lỗi khi đọc file config: " + ex.Message);
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
        public string conversationHistory { get; set; }
    }
}
