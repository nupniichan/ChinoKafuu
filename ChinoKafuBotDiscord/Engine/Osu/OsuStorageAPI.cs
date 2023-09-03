using ChinoBot.config;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
