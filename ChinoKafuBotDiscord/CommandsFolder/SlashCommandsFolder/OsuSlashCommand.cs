using ChinoBot.config;
using ChinoBot.Engine.Osu;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using OpenAI_API.Models;
using OsuNet;
using OsuNet.Abstractions;
using OsuNet.Enums;
using OsuNet.Models;
using OsuNet.Models.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace ChinoBot.CommandsFolder.SlashCommandsFolder
{
    public class OsuSlashCommand : ApplicationCommandModule
    {
        [SlashCommand("ohelp", "Để tớ giúp cậu nắm rõ các lệnh nhé")]
        public async Task HelpOsuCommand(InteractionContext ctx)
        {
            var embed = new DiscordEmbedBuilder()
                .WithTitle("Danh sách các lệnh")
                .WithDescription("Dưới đây là danh sách các lệnh có sẵn:")
                .WithColor(new DiscordColor(255, 124, 187))
                .AddField("/ou", "Tra thông tin osu của người chơi đó")
                .AddField("/orc", "Xem lại điểm số của lần chơi gần đây nhất")
                .AddField("/obs", "Xem top 5 map mà người đó có thành tích cao nhất")
                .AddField("/obmi", "Xem thông tin về beatmap đó")
                .WithFooter("Để sử dụng lệnh cụ thể, nhập /tên-lệnh");

            await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                new DiscordInteractionResponseBuilder().AddEmbed(embed));
        }

        [SlashCommand("ou", "Tra thông tin osu của người chơi đó")]
        public async Task OsuProfile(InteractionContext ctx, [Option("user","Tên của người đó là gì")] string name,
                                                             [Option("mode", "Chọn loại mode mà bạn muốn xem")] BeatmapMode mode)
        {
            await ctx.DeferAsync();
            try
            {
                // Connect to Osu API
                string osuAPI = await OsuStorageAPI.OsuAPIConnect();
                OsuApi api = new OsuApi(osuAPI);

                // send query to API consist of user and mode
                GetUserOptions userOptions = new GetUserOptions
                {
                    User = name,
                    Mode = mode
                };

                // get result from API
                User[] users = await api.GetUserAsync(userOptions);

                if (users != null && users.Length > 0)
                {
                    // get the first user from osu searchBar
                    User user = users[0];

                    // convert seconds to day/hour/minutes played
                    float totalSecondsPlayed = (float)user.TotalSecondsPlayed;
                    int daysPlayed = (int)(totalSecondsPlayed / (60 * 60 * 24));
                    int hoursPlayed = (int)((totalSecondsPlayed / (60 * 60)) % 24);
                    int minutesPlayed = (int)((totalSecondsPlayed / 60) % 60);

                    // make a description string to store properties
                    string stringDescription = $"**Country: ** #{user.Country}" + "\n" +
                                               $"**Global Rank: ** #{user.PPRank} ({user.Country}#{user.PPCountryRank})" + "\n" +
                                               $"**PP: ** {user.PPRaw}" + "\n" +
                                               $"**Accuracy: ** {user.Accuracy:F2}%" + "\n" +
                                               $"**Level: ** {user.Level:F2}" + "\n" +
                                               $"**Rank: ** {DiscordEmoji.FromName(ctx.Client, ":rankingXH:", true)}`{user.CountRankSSH}` {DiscordEmoji.FromName(ctx.Client, ":rankingX:", true)}`{user.CountRankSS}` {DiscordEmoji.FromName(ctx.Client, ":rankingSH:", true)}`{user.CountRankSH}` {DiscordEmoji.FromName(ctx.Client, ":rankingS:", true)}`{user.CountRankS}` {DiscordEmoji.FromName(ctx.Client, ":rankingA:", true)}`{user.CountRankA}`";

                    // create embed and return the result to user
                    var embed = new DiscordEmbedBuilder()
                            .WithAuthor($"{mode} Profile", null, "https://cdn.discordapp.com/attachments/1023808975185133638/1143737343002542202/Osu_Logo_2016.svg.png")
                            .WithTitle($"{user.Username}")
                            .WithUrl(user.GetUrl())
                            .WithDescription(stringDescription)
                            .WithColor(new DiscordColor(255, 124, 187))
                            .WithThumbnail(user.GetAvatar())
                            .WithFooter("Provided by osu.ppy.sh")
                            .WithTimestamp(DateTime.UtcNow)
                            .AddField("Join Date", user.JoinDate.Day.ToString() + "/" + user.JoinDate.Month.ToString() + "/" + user.JoinDate.Year.ToString())
                            .AddField("Play count: ", user.PlayCount.ToString(), true)
                            .AddField("\u200B", "\u200B", true)
                            .AddField("Play Time: ", daysPlayed + "d" + hoursPlayed.ToString() + "h" + minutesPlayed.ToString() + "m", true)
                            .WithImageUrl("https://media.discordapp.net/attachments/1140906898779017268/1143770839922266152/w6He59j.jpg?width=1440&height=180");
                    await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(embed));
                }
                // if user not found
                else
                {
                    var errorMessage = new DiscordEmbedBuilder()
                                        .WithTitle("Có lỗi nè~")
                                        .WithDescription($"Mình không tìm thấy '{name}'")
                                        .WithColor(DiscordColor.Red);

                    await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(errorMessage));
                }
            }
            // catch another error
            catch (Exception ex)
            {
                var errorMessage = new DiscordEmbedBuilder()
                    .WithTitle("Có lỗi nè~")
                    .WithDescription($"Mình không tìm thấy '{name}': {ex.Message}")
                    .WithColor(DiscordColor.Red);

                await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(errorMessage));
            }
        }
        [SlashCommand("orc", "Xem lại điểm số của lần chơi gần đây nhất")]
        public async Task OsurecentCommand(InteractionContext ctx, [Option("name", "Tên của người bạn cần xem là ai nè")] string name)
        {
            await ctx.DeferAsync();
            try
            {
                // Kết nối với API
                string osuAPI = await OsuStorageAPI.OsuAPIConnect();
                OsuApi api = new OsuApi(osuAPI);
                GetUserRecentOptions userRecentOptions = new GetUserRecentOptions
                {
                    User = name
                };
                UserRecent[] users = await api.GetUserRecentAsync(userRecentOptions);
                if (users != null && users.Length > 0)
                {
                    // Get the first user from search Bar
                    UserRecent user = users[0];

                    // Gọi API để lấy thông tin của beatmap dựa trên BeatmapId
                    GetBeatmapOptions beatmapOptions = new GetBeatmapOptions
                    {
                        BeatmapId = user.BeatmapId
                    };
                    Beatmap[] beatmapArray = await api.GetBeatmapAsync(beatmapOptions);
                    Beatmap beatmapInfo = beatmapArray[0];
                    beatmapInfo.BeatmapId = user.BeatmapId;

                    // Gọi API để lấy thông tin của score dựa trên ScoreId
                    GetScoresOptions scoreOptions = new GetScoresOptions
                    {
                        BeatmapId = user.BeatmapId,
                        User = name
                    };
                    Scores[] scores = await api.GetScoresAsync(scoreOptions);
                    float pp = scores[0].PP;

                    // change format player got the recent score by UTC
                    string dateTimeString = user.DateTime.ToString("dd/MM/yyyy HH:mm:ss");

                    // create string description to store properties
                    string resultString = $"**Rank:** {Helper.getOsuRank(user.Rank, user.EnabledMods.ToString(), ctx)}\n" +
                                          $"**Combo:** x{user.MaxCombo}/{beatmapInfo.MaxCombo}\n" +
                                          $"**Details:** [{user.Count300}/{user.Count100}/{user.Count50}/{user.CountMiss}]\n" +
                                          $"**PP:** {pp:F2}\n" +
                                          $"**Score:** {user.Score}" + "\n";

                    // create embed and response to user has requested
                    var embed = new DiscordEmbedBuilder()
                        .WithTitle($"[{beatmapInfo.Title}] ({beatmapInfo.DiffecultyRating:F2}★) {beatmapInfo.Version} +{user.EnabledMods}")
                        .WithUrl(beatmapInfo.GetUrl())
                        .WithAuthor($"Recent Play by {name}", null, "https://cdn.discordapp.com/attachments/1023808975185133638/1143737343002542202/Osu_Logo_2016.svg.png")
                        .WithColor(new DiscordColor(255, 124, 187)) // Tùy chỉnh màu sắc cho embed
                        .WithDescription(resultString)
                        .WithThumbnail(beatmapInfo.GetThumbnail())
                        .WithFooter($"Provided by osu.ppy.sh • {dateTimeString} UTC")
                        .WithImageUrl("https://media.discordapp.net/attachments/1140906898779017268/1143770839922266152/w6He59j.jpg?width=1440&height=180");

                    await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(embed));
                }
                else
                {
                    // catch error if not found user recent play
                    var errorEmbed = new DiscordEmbedBuilder()
                        .WithTitle("Lỗi")
                        .WithDescription("Không thể tìm thấy dữ liệu từ lần chơi gần nhất của người chơi " + name)
                        .WithColor(DiscordColor.Red);
                    await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(errorEmbed));
                }
            }
            catch (Exception e)
            {
                var errorEmbed = new DiscordEmbedBuilder()
                    .WithTitle("Lỗi")
                    .WithDescription(e.Message)
                    .WithColor(DiscordColor.Red);
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(errorEmbed));
            }
        }



        [SlashCommand("obs", "Xem top 5 map mà người đó có thành thích cao nhất")]
        public async Task OsuUserBestScoreCommand(InteractionContext ctx, [Option("name", "Tên của người bạn cần xem là ai nè")] string name,
                                          [Option("mode", "Chọn loại mode mà bạn muốn xem")] BeatmapMode mode)
        {
            await ctx.DeferAsync();

            string osuAPI = await OsuStorageAPI.OsuAPIConnect();
            OsuApi api = new OsuApi(osuAPI);

            try
            {
                GetUserBestOptions userBestOptions = new GetUserBestOptions()
                {
                    User = name,
                    Limit = 5
                };
                GetUserOptions userOptions = new GetUserOptions()
                {
                    User = name
                };
                User[] users = await api.GetUserAsync(userOptions);
                var user = users[0];
                UserBest[] userBestScores = await api.GetUserBestAsync(userBestOptions);

                var embed = new DiscordEmbedBuilder()
                    .WithTitle($"{name}'s Leading 5 Best Scores: ");

                string descriptionString = "";
                
                foreach (var userBestScore in userBestScores)
                {
                    GetBeatmapOptions beatmapOptions = new GetBeatmapOptions
                    {
                        BeatmapId = userBestScore.BeatmapId
                    };

                    // Gọi API để lấy thông tin của score dựa trên ScoreId
                    GetScoresOptions scoreOptions = new GetScoresOptions
                    {
                        BeatmapId = userBestScore.BeatmapId,
                        User = name
                    };
                    Scores[] scores = await api.GetScoresAsync(scoreOptions);
                    float pp = scores[0].PP;

                    Beatmap[] beatmapArray = await api.GetBeatmapAsync(beatmapOptions);
                    Beatmap beatMapInfo = beatmapArray.FirstOrDefault();

                    string formattedDateTime = userBestScore.DateTime.ToString("dd/MM/yyyy HH:mm:ss");
                    string rankEmoji = Helper.getOsuRank(userBestScore.Rank.ToString(), mode.ToString(), ctx);
                    string beatmapTitle = $"[**{beatMapInfo?.Title}**]({beatMapInfo?.GetUrl()}) - ID: {beatMapInfo?.BeatmapId}" + "\n" +
                                          $"**Difficulty Rating: ** {beatMapInfo?.Version} ({beatMapInfo?.DiffecultyRating:F2}★) +{userBestScore?.EnabledMods}" + "\n" +
                                          $"**Rank: ** {rankEmoji}" + "\n" +
                                          $"**PP: {pp:F2}**" + " \n" + 
                                          $"{formattedDateTime} UTC" + "\n";
                    descriptionString += $"{beatmapTitle}\n";
                }

                embed.WithDescription(descriptionString);
                embed.WithColor(new DiscordColor(255, 124, 187));
                embed.WithThumbnail(user.GetAvatar());
                embed.WithAuthor($"Best Scores", null, "https://cdn.discordapp.com/attachments/1023808975185133638/1143737343002542202/Osu_Logo_2016.svg.png");
                embed.WithImageUrl("https://media.discordapp.net/attachments/1140906898779017268/1143770839922266152/w6He59j.jpg?width=1440&height=180");
                embed.WithFooter($"Provided by osu.ppy.sh • Overview");
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(embed));
            }
            catch (Exception e)
            {
                var errorEmbed = new DiscordEmbedBuilder()
                    .WithTitle("Lỗi")
                    .WithDescription(e.Message)
                    .WithColor(DiscordColor.Red);

                await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(errorEmbed));
            }
        }
        [SlashCommand("obmi","Xem thông tin về beatmap đó")]
        public async Task CheckBeatMapInformationCommand(InteractionContext ctx, [Option("ID","Map bạn cần xem có mã id là gì?")] long id)
        {
            await ctx.DeferAsync();

            try
            {
                string osuAPI = await OsuStorageAPI.OsuAPIConnect();
                OsuApi api = new OsuApi(osuAPI);

                GetBeatmapOptions beatmapOptions = new GetBeatmapOptions()
                {
                    BeatmapId = (ulong)id
                };

                Beatmap[] beatmapArray = await api.GetBeatmapAsync(beatmapOptions);
                Beatmap beatmapInformation = beatmapArray[0];
                if (beatmapInformation != null)
                {
                    string approvedDate = Helper.FormatDate(beatmapInformation.ApprovedDate);
                    string lastUpdate = Helper.FormatDate(beatmapInformation.LastUpdate);
                    string submitDate = Helper.FormatDate(beatmapInformation.SubmitDate);

                    string descriptionString = $"**ApproveStatus:** {beatmapInformation.Approved} ({approvedDate})" + "\n" +
                                               $"**Mode: ** {beatmapInformation.Mode}" + "\n" +
                                               $"**Artist: **{beatmapInformation.Artist}" + "\n" +
                                               $"**Pass Count:** {beatmapInformation.PassCount}" + "\n" +
                                               $"**Favorite Count:** {beatmapInformation.FavouriteCount}" + "\n" +
                                               $"**Creator: **{beatmapInformation.Creator} [#{beatmapInformation.CreatorId}]({beatmapInformation.GetCreatorUrl()})" + "\n" +
                                               $"**SubmitDate: **{submitDate}" + "\n" +
                                               $"**Last Update: **{lastUpdate}" + "\n";
                    var embed = new DiscordEmbedBuilder()
                            .WithAuthor("BeatMap", null, "https://cdn.discordapp.com/attachments/1023808975185133638/1143737343002542202/Osu_Logo_2016.svg.png")
                            .WithTitle($"[{beatmapInformation.Title}]")
                            .WithUrl(beatmapInformation.GetUrl())
                            .WithThumbnail(beatmapInformation.GetThumbnail())
                            .WithDescription(descriptionString)
                            .AddField("Genres", string.Join(' ', beatmapInformation.GenreId), true)
                            .AddField("\u200B", "\u200B",true)
                            .AddField("Language", string.Join(' ', beatmapInformation.LanguageId), true)
                            .AddField("Tags", beatmapInformation.Tags, false)
                            .AddField("Source", beatmapInformation.Source)
                            .WithFooter("Provided by osu.ppy.sh • Overview")
                            .WithImageUrl("https://media.discordapp.net/attachments/1140906898779017268/1143770839922266152/w6He59j.jpg?width=1440&height=180")
                            .WithColor(new DiscordColor(255, 124, 187));
                    await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(embed));
                }
                else
                {
                    var errorEmbed = new DiscordEmbedBuilder()
                            .WithTitle("Lỗi")
                            .WithDescription("Không thể tìm thấy beatmap với ID: " + id)
                            .WithColor(DiscordColor.Red);

                    await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(errorEmbed));
                }
            }
            catch (Exception e)
            {
                var errorEmbed = new DiscordEmbedBuilder()
                    .WithTitle("Lỗi")
                    .WithDescription(e.Message)
                    .WithColor(DiscordColor.Red);

                await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(errorEmbed));
            }
        }
    }
}
