using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ChinoKafuu.Utils
{
    public class Util
    {
        public static bool SkipLavalink => Environment.GetEnvironmentVariable("SKIP_LAVALINK") == "true";
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
                case "X":
                    return DiscordEmoji.FromName(ctx.Client, ":rankingX:", true);
                case "SH":
                    return DiscordEmoji.FromName(ctx.Client, ":rankingSH:", true);
                case "XH":
                    return DiscordEmoji.FromName(ctx.Client, ":rankingXH:", true);
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
        public static string ProcessHtmlToMarkdown(HtmlNode node)
        {
            StringBuilder markdownBuilder = new StringBuilder();

            foreach (var childNode in node.ChildNodes)
            {
                switch (childNode.NodeType)
                {
                    case HtmlNodeType.Element:
                        switch (childNode.Name)
                        {
                            case "b":
                                markdownBuilder.Append("**");
                                markdownBuilder.Append(ProcessHtmlToMarkdown(childNode));
                                markdownBuilder.Append("**");
                                break;
                            case "i":
                                markdownBuilder.Append("*");
                                markdownBuilder.Append(ProcessHtmlToMarkdown(childNode));
                                markdownBuilder.Append("*");
                                break;
                            case "u":
                                markdownBuilder.Append("__");
                                markdownBuilder.Append(ProcessHtmlToMarkdown(childNode));
                                markdownBuilder.Append("__");
                                break;
                            case "s":
                                markdownBuilder.Append("~~");
                                markdownBuilder.Append(ProcessHtmlToMarkdown(childNode));
                                markdownBuilder.Append("~~");
                                break;
                            case "h1":
                                markdownBuilder.Append("# ");
                                markdownBuilder.Append(ProcessHtmlToMarkdown(childNode));
                                markdownBuilder.AppendLine();
                                break;
                            case "h2":
                                markdownBuilder.Append("## ");
                                markdownBuilder.Append(ProcessHtmlToMarkdown(childNode));
                                markdownBuilder.AppendLine();
                                break;
                            case "ul":
                            case "ol":
                                foreach (var listItem in childNode.ChildNodes)
                                {
                                    if (listItem.Name == "li")
                                    {
                                        markdownBuilder.Append(childNode.Name == "ul" ? "* " : "1. ");
                                        markdownBuilder.Append(ProcessHtmlToMarkdown(listItem));
                                        markdownBuilder.AppendLine();
                                    }
                                }
                                break;
                            case "a":
                                var href = childNode.GetAttributeValue("href", string.Empty);
                                markdownBuilder.Append("[");
                                markdownBuilder.Append(ProcessHtmlToMarkdown(childNode));
                                markdownBuilder.Append("](");
                                markdownBuilder.Append(href);
                                markdownBuilder.Append(")");
                                break;
                            case "img":
                                var src = childNode.GetAttributeValue("src", string.Empty);
                                var alt = childNode.GetAttributeValue("alt", string.Empty);
                                markdownBuilder.Append("![");
                                markdownBuilder.Append(alt);
                                markdownBuilder.Append("](");
                                markdownBuilder.Append(src);
                                markdownBuilder.Append(")");
                                break;
                            case "code":
                                markdownBuilder.Append("`");
                                markdownBuilder.Append(ProcessHtmlToMarkdown(childNode));
                                markdownBuilder.Append("`");
                                break;
                            case "pre":
                                markdownBuilder.Append("```\n");
                                markdownBuilder.Append(ProcessHtmlToMarkdown(childNode));
                                markdownBuilder.Append("\n```");
                                break;
                            default:
                                markdownBuilder.Append(ProcessHtmlToMarkdown(childNode));
                                break;
                        }
                        break;
                    case HtmlNodeType.Text:
                        markdownBuilder.Append(childNode.InnerText);
                        break;
                }
            }

            return markdownBuilder.ToString();
        }


        public static string GetUtcOffsetString()
        {
            TimeZoneInfo localTimeZone = TimeZoneInfo.Local;
            TimeSpan utcOffset = localTimeZone.GetUtcOffset(DateTime.UtcNow);

            string sign = utcOffset >= TimeSpan.Zero ? "+" : "-";
            string formattedOffset = $"{sign}{Math.Abs(utcOffset.Hours):00}:{Math.Abs(utcOffset.Minutes):00}";

            return $"UTC{formattedOffset}";
        }

        public static DiscordColor GetEmbedColor(double temperature)
        {
            if (temperature < 10) return DiscordColor.Blue;     
            if (temperature < 20) return DiscordColor.Cyan;    
            if (temperature < 30) return DiscordColor.Green;    
            if (temperature < 35) return DiscordColor.Yellow;   
            return DiscordColor.Orange;                        
        }

        public static string GetWindDirection(float degrees)
        {
            string[] directions = {
        "Bắc", "Bắc Đông Bắc", "Đông Bắc",
        "Đông", "Đông Nam", "Nam Đông Nam",
        "Nam", "Nam Tây Nam", "Tây Nam",
        "Tây", "Tây Bắc", "Bắc Tây Bắc"
    };

            int index = (int)((degrees + 15) % 360 / 30);
            return directions[index];
        }

        public static string GetCloudDescription(int cloudiness)
        {
            if (cloudiness < 10) return "Trời quang";
            if (cloudiness < 25) return "Ít mây";
            if (cloudiness < 50) return "Mây rải rác";
            if (cloudiness < 75) return "Nhiều mây";
            return "Trời âm u";
        }
        public static string GetMainWeather(string weather)
        {
            if (weather == "Clear") return ":sunny: Trời quang";
            if (weather == "Clouds") return ":cloud: Có mây";
            if (weather == "Rain") return ":cloud_rain: Mưa";
            if (weather == "Drizzle") return "🌧 Mưa phùn";
            if (weather == "Thunderstorm") return "⛈ Giông bão";
            if (weather == "Snow") return "❄ Tuyết";
            if (weather == "Mist") return "🌫 Sương mù nhẹ";
            if (weather == "Smoke") return "💨 Khói"; 
            if (weather == "Haze") return "🌫 Sương mù do khói bụi";
            if (weather == "Dust") return "🌪 Bụi"; 
            if (weather == "Fog") return "🌫 Sương mù dày đặc";
            if (weather == "Sand") return "🌪 Cát hoặc bão cát"; 
            if (weather == "Ash") return "🌪 Tro núi lửa"; 
            if (weather == "Squall") return "🌬 Gió giật"; 
            if (weather == "Tornado") return ":cloud_tornado: Lốc xoáy"; 

            return "❓ Không xác định"; 
        }
        public static string GetTimeOfDay(int hour)
        {
            if (hour >= 5 && hour < 12)
            {
                return ":sunrise: Trời sáng";
            }
            else if (hour >= 12 && hour < 17)
            {
                return ":sunny: Trời trưa";
            }
            else if (hour >= 17 && hour < 19)
            {
                return ":city_sunset: Trời chiều";
            }
            else
            {
                return ":milky_way: Trời tối";
            }
        }
    }
}
