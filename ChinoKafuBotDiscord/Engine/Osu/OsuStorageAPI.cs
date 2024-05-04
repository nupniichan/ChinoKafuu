using ChinoBot.config;

namespace ChinoBot.Engine.Osu
{
    public class OsuStorageAPI
    {
        public static async Task<string> OsuAPIConnect()
        {
            var jsonReader = new JSONreader();
            await jsonReader.ReadJson();
            return jsonReader.osuToken;
        }
    }
}
