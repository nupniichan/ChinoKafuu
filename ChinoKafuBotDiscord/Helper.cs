using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using HtmlAgilityPack;
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
                        // Xử lý các thẻ HTML
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
    }
}
