using ChinoBot.config;

namespace ChinoBot.Engine.Osu
{
    public class OsuStorageAPI
    {
        public static async Task<string> OsuAPIConnect()
        {
            var envReader = new EnvReader();
            await envReader.ReadConfigFile();
            return envReader.osuToken;
        }
    }
}
