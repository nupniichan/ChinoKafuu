using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChinoBot
{
    public class Helper
    {
        public static DiscordColor GetRandomDiscordColor()
        {
            Random random = new Random();
            byte red = (byte)random.Next(256);
            byte green = (byte)random.Next(256);
            byte blue = (byte)random.Next(256);

            return new DiscordColor(red, green, blue);
        }
        public static string getOsuRank(string rank, string mode, InteractionContext ctx)
        {
            if (mode == "Hidden")
            {
                switch (rank)
                {
                    case "A":
                        return DiscordEmoji.FromName(ctx.Client, ":rankingA:", true);
                    case "B":
                        return DiscordEmoji.FromName(ctx.Client, ":rankingB:", true);
                    case "C":
                        return DiscordEmoji.FromName(ctx.Client, ":rankingC:", true);
                    case "D":
                        return DiscordEmoji.FromName(ctx.Client, ":rankingD:", true);
                    case "S":
                        return DiscordEmoji.FromName(ctx.Client, ":rankingSH:", true);
                    case "SS":
                        return DiscordEmoji.FromName(ctx.Client, ":rankingXH:", true);
                }
            }
            else
            {
                switch (rank)
                {
                    case "A":
                        return DiscordEmoji.FromName(ctx.Client, ":rankingA:", true);
                    case "B":
                        return DiscordEmoji.FromName(ctx.Client, ":rankingB:", true);
                    case "C":
                        return DiscordEmoji.FromName(ctx.Client, ":rankingC:", true);
                    case "D":
                        return DiscordEmoji.FromName(ctx.Client, ":rankingD:", true);
                    case "S":
                        return DiscordEmoji.FromName(ctx.Client, ":rankingS:", true);
                    case "SS":
                        return DiscordEmoji.FromName(ctx.Client, ":rankingX :", true);
                }
            }
            return null;
        }
        public static string FormatDate(DateTime? date)
        {
            if (date.HasValue)
            {
                return date.Value.ToString("dd/MM/yyyy");
            }
            else
            {
                return "N/A";
            }
        }
    }
}
