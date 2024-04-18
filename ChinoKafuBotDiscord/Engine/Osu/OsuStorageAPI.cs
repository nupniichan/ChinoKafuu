using ChinoBot.config;

namespace ChinoBot.Engine.Osu
{
    public class OsuStorageAPI
    {
        public static async Task<string> OsuAPIConnect()
        {
            var jsonReader = new JSONreader();
            await jsonReader.ReadJsonToken();
            return jsonReader.osuToken;
        }
    }
}
