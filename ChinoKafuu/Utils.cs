using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ChinoBot
{
    public class Utils
    {
        public static bool SkipLavalink => System.Environment.GetEnvironmentVariable("SKIP_LAVALINK") == "true";
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
    }
}
