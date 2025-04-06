using System;
using System.IO;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace ChinoBot.config
{
    public sealed class EnvReader
    {
        public string token { get; set; }
        public string prefix { get; set; }
        public string osuToken { get; set; }
        public string geminiAPIKey { get; set; }
        public string geminiTranslateAPIKey { get; set; }
        public ulong allowChannelID_gemini { get; set; }
        public string userDefaultRoleName { get; set; }
        public string openWeatherApi { get; set; }

        public async Task ReadConfigFile()
        {
            string baseDirectory = AppContext.BaseDirectory;
            string relativePath = Path.Combine(baseDirectory, "..", "..", "..", "Configs", ".env");

            try
            {
                var envVars = new Dictionary<string, string>();
                using (StreamReader sr = new StreamReader(relativePath))
                {
                    string line;
                    while ((line = await sr.ReadLineAsync()) != null)
                    {
                        if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#"))
                            continue;

                        var parts = line.Split('=', 2);
                        if (parts.Length == 2)
                        {
                            envVars[parts[0].Trim()] = parts[1].Trim();
                        }
                    }
                }

                this.token = envVars["DISCORD_TOKEN"];
                this.prefix = envVars["PREFIX"];
                this.osuToken = envVars["OSU_TOKEN"];
                this.geminiAPIKey = envVars["GEMINI_API_KEY"];
                this.geminiTranslateAPIKey = envVars["GEMINI_TRANSLATE_API_KEY"];
                this.allowChannelID_gemini = ulong.Parse(envVars["ALLOW_CHANNEL_ID_GEMINI"]);
                this.userDefaultRoleName = envVars["USER_DEFAULT_ROLE_NAME"];
                this.openWeatherApi = envVars["OPEN_WEATHER_API"];
            }
            catch (FileNotFoundException)
            {
                Console.WriteLine("Không tìm thấy file .env");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Lỗi khi đọc file .env: " + ex.Message);
            }
        }
    }

    public sealed class EnvStructure
    {
        public string token { get; set; }
        public string prefix { get; set; }
        public string osuToken { get; set; }
        public string geminiAPIKey { get; set; }
        public string geminiTranslateAPIKey { get; set; }
        public ulong allowChannelID_gemini { get; set; }
        public string userDefaultRoleName { get; set; }
        public string openWeatherApi { get; set; }
    }
}
