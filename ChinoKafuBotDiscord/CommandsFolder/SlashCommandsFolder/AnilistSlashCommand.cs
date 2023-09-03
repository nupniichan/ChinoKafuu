using AnimeListBot.Handler.Anilist;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity;
using DSharpPlus.SlashCommands;
using GraphQL.Types.Relay.DataObjects;
using System.Runtime.CompilerServices;
using System.Text;

namespace ChinoBot.CommandsFolder.SlashCommandsFolder
{
    internal class AnilistSlashCommand : ApplicationCommandModule
    {
        [SlashCommand("AniHelp", "Hiển thị trợ giúp về các lệnh Anilist")]
        public async Task AniHelpCommand(InteractionContext ctx)
        {
            var embed = new DiscordEmbedBuilder()
                .WithTitle("Danh sách các lệnh Anilist")
                .WithDescription("Dưới đây là danh sách các lệnh Anilist có sẵn:")
                .WithColor(DiscordColor.Azure)
                .AddField("/AniUser", "Tìm profile trên Anilist")
                .AddField("/AniuserFavorite", "Xem những bộ anime/manga mà người đó thích")
                .AddField("/AniAnimeInfo", "Xem thông tin về bộ anime")
                .AddField("/AniMangaInfo", "Xem thông tin về bộ manga")
                .AddField("/AniCharacterInfo", "Xem thông tin về nhân vật")
                .AddField("/AniStaffInfo", "Xem thông tin về những người làm ra")
                .WithFooter("Để sử dụng lệnh cụ thể, nhập /tên-lệnh");

            await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                new DiscordInteractionResponseBuilder().AddEmbed(embed));
        }

        [SlashCommand("AniUser", "Tìm profile trên anilist")]
        public async Task AniUserCommand(InteractionContext ctx, [Option("name", "Tên profile là gì?")] string name)
        {
            await ctx.DeferAsync();
            var user = await AniUserQuery.GetUser(name);
            try
            {
                float minutesWatched = user.statistics.anime.minutesWatched;
                float daysWatched = (float)minutesWatched / 60 / 24;
                float dayWatch = (float)Math.Round(daysWatched, 2);

                var embed = new DiscordEmbedBuilder()
                    .WithAuthor($"AniList Profile", null, "https://media.discordapp.net/attachments/1023808975185133638/1143013784584208504/AniList_logo.svg.png?width=588&height=588")
                    .WithTitle(user.name)
                    .WithUrl(user.siteUrl)
                    .WithDescription($"**ID: **{user.id}")
                    .AddField("**About**", user.about)
                    .AddField("\u2014", "\u200B") // Đường kẻ ngang
                    .AddField("Anime Stats", $"Anime Count: {user.statistics.anime.count}\n" +
                                              $"Mean Score: {user.statistics.anime.meanScore}\n" +
                                              $"Days Watched: {dayWatch}\n" +
                                              $"Episodes Watched: {user.statistics.anime.episodesWatched}", false)
                    .AddField("Manga Stats", $"Manga Count: {user.statistics.manga.count}\n" +
                                              $"Mean Score: {user.statistics.manga.meanScore}\n" +
                                              $"Chapters Read: {user.statistics.manga.chaptersRead}\n" +
                                              $"Volumes Read: {user.statistics.manga.volumesRead}", false)
                    .WithImageUrl(user.bannerImage)
                    .WithColor(DiscordColor.Azure)
                    .WithFooter("Provided by https://anilist.co/ • Overview");
                embed.WithThumbnail(user.Avatar.medium);

                await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(embed));
            }
            catch (Exception ex)
            {
                var errorMessage = new DiscordEmbedBuilder()
                    .WithTitle("Có lỗi nè~")
                    .WithDescription($"Mình không tìm thấy '{name}': {ex.Message}")
                    .WithColor(DiscordColor.Red);

                await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(errorMessage));
            }
        }

        [SlashCommand("AniuserFavorite", "Bạn có muốn biết người đó thích các bộ anime/manga nào?")]
        public async Task AniUserFavoriteCommand(InteractionContext ctx, [Option("user", "Tên của người bạn cần tra là ai nè~")] string name)
        {
            try
            {
                await ctx.DeferAsync();
                var user = await AniUserQuery.GetUser(name);

                List<AniMediaResponse.AniMedia> favAnimeList = user.favourites.anime.nodes;
                StringBuilder animelist = new StringBuilder();
                foreach (var anime in favAnimeList)
                {
                    animelist.AppendLine($"[{anime.title.english}]({anime.siteUrl})");
                }
                string animeFavorites = animelist.ToString();

                List<AniMediaResponse.AniMedia> mangaFavorList = user.favourites.manga.nodes;
                StringBuilder mangaList = new StringBuilder();
                foreach (var manga in mangaFavorList)
                {
                    mangaList.AppendLine($"[{manga.title.english}]({manga.siteUrl})");
                }
                string mangaFavorites = mangaList.ToString();

                var characters = user.favourites.characters.nodes;
                StringBuilder characterList = new StringBuilder();
                foreach (var character in characters)
                {
                    characterList.AppendLine($"[{character.name.first} {character.name.last}]({character.siteUrl})");
                }

                var embed = new DiscordEmbedBuilder()
                    .WithAuthor("AniList Favorite", null, "https://media.discordapp.net/attachments/1023808975185133638/1143013784584208504/AniList_logo.svg.png?width=588&height=588")
                    .WithTitle(user.name)
                    .AddField("Favorite Anime", animeFavorites)
                    .AddField("Favorite Manga", mangaFavorites)
                    .AddField("Favorite Characters", characterList.ToString())
                    .AddField("Find Out More", $"[Anilist]({user.siteUrl})")
                    .WithColor(DiscordColor.Azure)
                    .WithFooter("Provided by https://anilist.co/ • Favorite");
                embed.WithThumbnail(user.Avatar.medium);
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(embed));
            }
            catch (Exception ex)
            {
                var errorMessage = new DiscordEmbedBuilder()
                    .WithTitle("Lỗi xảy ra")
                    .WithDescription(ex.Message)
                    .WithColor(DiscordColor.Red);

                await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(errorMessage));
            }
        }

        [SlashCommand("AniAnimeInfo", "Bạn muốn biết thông tin về bộ anime đó?")]
        public async Task AnimeInformation(InteractionContext ctx, [Option("name", "Cho mình biết tên về tên bộ anime đó đi")] string name)
        {
            await ctx.DeferAsync();
            var media = await AniMediaQuery.SearchMedia(name, AniMediaType.ANIME);
            try
            {
                if (media != null)
                {
                    AniFuzzyDate startDate = media.startDate; // Đổi media.startDate thành media.endDate
                    DateTime? start = null;
                    string startDateFormat = "N/A"; // Giá trị mặc định khi endDate là null
                    if (startDate != null)
                    {
                        int? year = startDate.year;
                        int? month = startDate.month;
                        int? day = startDate.day;

                        if (year.HasValue && month.HasValue && day.HasValue)
                        {
                            start = new DateTime(year.Value, month.Value, day.Value);
                            startDateFormat = start.Value.ToString("dd/MMMM/yyyy");
                        }
                    }

                    AniFuzzyDate endDate = media.endDate; // Đổi media.startDate thành media.endDate
                    DateTime? end = null;
                    string endDateFormat = "N/A"; // Giá trị mặc định khi endDate là null

                    if (endDate != null)
                    {
                        int? year = endDate.year;
                        int? month = endDate.month;
                        int? day = endDate.day;

                        if (year.HasValue && month.HasValue && day.HasValue)
                        {
                            end = new DateTime(year.Value, month.Value, day.Value);
                            endDateFormat = end.Value.ToString("dd/MMMM/yyyy");
                        }
                    }
                    // Lấy danh sách thể loại
                    List<string> genres = media.genres;

                    // Biến danh sách thể loại thành chuỗi để hiển thị trong embed
                    string genresString = string.Join(", ", genres);

                    var embed = new DiscordEmbedBuilder()
                    .WithAuthor($"{media.format}", null, "https://media.discordapp.net/attachments/1023808975185133638/1143013784584208504/AniList_logo.svg.png?width=588&height=588")
                    .WithTitle(media.title.english)
                    .WithUrl(media.siteUrl)
                    .WithDescription(media.description)
                    .AddField("Episodes", media.episodes.ToString())
                    .AddField("Start Date ", startDateFormat, true)
                    .AddField("End Date ", endDateFormat, true)
                    .AddField("Season", media.season, true)
                    .AddField("Status", media.status.ToString(), false)
                    .AddField("Source", media.source, false)
                    .AddField("Genres", genresString, false)
                    .AddField("Average Score", media.averageScore.ToString() + "/100")
                    .AddField("Mean Score", media.meanScore.ToString() + "/100")
                    .AddField("For more information", $"[Anilist]({media.siteUrl})")
                    .WithFooter("Provided by https://anilist.co/")
                    .WithColor(DiscordColor.Azure);
                    embed.WithThumbnail(media.coverImage.medium);
                    embed.WithImageUrl(media.bannerImage);
                    await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(embed));
                }
                else
                {
                    var errorMessage = new DiscordEmbedBuilder()
                    .WithTitle("Thấy lỗi rồi nè~")
                    .WithDescription($"Mình không tìm được kết quả về {name} trong thanh tìm kiếm của anilist.")
                        .WithColor(DiscordColor.Red);

                    await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(errorMessage));
                }
            }
            catch (Exception ex)
            {
                var errorMessage = new DiscordEmbedBuilder()
                    .WithTitle("Thấy lỗi rồi nè~")
                    .WithDescription(ex.Message)
                    .WithColor(DiscordColor.Red);

                await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(errorMessage));
            }
        }
        [SlashCommand("AniMangaInfo", "Bạn muốn biết thông tin về bộ manga đó?")]
        public async Task MangaInformation(InteractionContext ctx, [Option("name", "Cho mình biết tên về tên bộ manga đó đi")] string name)
        {
            await ctx.DeferAsync();
            var media = await AniMediaQuery.SearchMedia(name, AniMediaType.MANGA);
            try
            {
                if (media != null)
                {
                    AniFuzzyDate startDate = media.startDate;
                    DateTime? start = null;
                    string startDateFormat = "N/A";
                    if (startDate != null)
                    {
                        int? year = startDate.year;
                        int? month = startDate.month;
                        int? day = startDate.day;

                        if (year.HasValue && month.HasValue && day.HasValue)
                        {
                            start = new DateTime(year.Value, month.Value, day.Value);
                            startDateFormat = start.Value.ToString("dd/MMMM/yyyy");
                        }
                    }

                    AniFuzzyDate endDate = media.endDate;
                    DateTime? end = null;
                    string endDateFormat = "N/A";

                    if (endDate != null)
                    {
                        int? year = endDate.year;
                        int? month = endDate.month;
                        int? day = endDate.day;

                        if (year.HasValue && month.HasValue && day.HasValue)
                        {
                            end = new DateTime(year.Value, month.Value, day.Value);
                            endDateFormat = end.Value.ToString("dd/MMMM/yyyy");
                        }
                    }
                    // Lấy danh sách thể loại
                    List<string> genres = media.genres;
                    // Biến danh sách thể loại thành chuỗi để hiển thị trong embed
                    string genresString = string.Join(", ", genres);

                    var embed = new DiscordEmbedBuilder()
                    .WithAuthor($"{media.format}", null, "https://media.discordapp.net/attachments/1023808975185133638/1143013784584208504/AniList_logo.svg.png?width=588&height=588")
                    .WithTitle(media.title.english)
                    .WithUrl(media.siteUrl)
                    .WithImageUrl(media.bannerImage)
                    .WithThumbnail(media.coverImage.medium)
                    .WithDescription(media.description)
                    .AddField("Status", media.status.ToString(), true)
                    .AddField("Start Date", startDateFormat, true)
                    .AddField("End Date", endDateFormat, true)
                    .AddField("Average Score", media.averageScore.ToString() + "/100")
                    .AddField("Mean Score", media.meanScore.ToString() + "/100")
                    .AddField("Genres", string.Join(' ', media.genres))
                    .AddField("For more information", $"[Anilist]({media.siteUrl})")
                    .WithColor(DiscordColor.Azure)
                    .WithFooter("Provided by https://anilist.co/");
                    await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(embed));
                }
                else
                {
                    var errorMessage = new DiscordEmbedBuilder()
                    .WithTitle("Thấy lỗi rồi nè~")
                    .WithDescription($"Mình không tìm được kết quả về {name} trong thanh tìm kiếm của anilist.")
                        .WithColor(DiscordColor.Red);

                    await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(errorMessage));
                }
            }
            catch (Exception ex)
            {
                var errorMessage = new DiscordEmbedBuilder()
                    .WithTitle("Thấy lỗi rồi nè~")
                    .WithDescription(ex.Message)
                    .WithColor(DiscordColor.Red);

                await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(errorMessage));
            }
        }
        [SlashCommand("AniCharacterInfo", "Bạn muốn biết thêm thông tin về nhân vật đó?")]
        public async Task CharacterInformation(InteractionContext ctx, [Option("name", "Cung cấp cho mình về tên nhân vật đó đi")] string name)
        {
            await ctx.DeferAsync();
            var character = await AniCharacterQuery.SearchCharacter(name);
            try
            {
                if (character != null)
                {
                    var embed = new DiscordEmbedBuilder()
                        .WithAuthor($"Anilist Character", null, "https://media.discordapp.net/attachments/1023808975185133638/1143013784584208504/AniList_logo.svg.png?width=588&height=588")
                        .WithThumbnail(character.image.medium)
                        .WithTitle($"{character.name.first} {character.name.last}")
                        .WithDescription(character.description)
                        .WithUrl(character.siteUrl)
                        .AddField("Synonyms", string.Join(' ', character.name.alternative))
                        .WithColor(DiscordColor.White)
                        .WithFooter("Provided by https://anilist.co/");
                    await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(embed));
                }
                else
                {
                    var errorMessage = new DiscordEmbedBuilder()
                        .WithTitle("Thấy lỗi rồi nè~")
                        .WithDescription($"Mình không tìm được kết quả về {name} trong thanh tìm kiếm của anilist.")
                        .WithColor(DiscordColor.Red);

                    await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(errorMessage));
                }
            }
            catch (Exception ex)
            {
                var errorMessage = new DiscordEmbedBuilder()
                    .WithTitle("Thấy lỗi rồi nè~")
                    .WithDescription(ex.Message)
                    .WithColor(DiscordColor.Red);

                await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(errorMessage));
            }
        }
        [SlashCommand("AniStaffInfo","Bạn muốn biết thông tin về những người làm ra?")]
        public async Task StaffInformation(InteractionContext ctx, [Option("name", "Cung cấp cho mình về tên của người đó đi")] string name)
        {
            await ctx.DeferAsync();
            var staff = await AniStaffQuery.SearchStaff(name);
            try
            {
                if (staff != null)
                {
                    var embed = new DiscordEmbedBuilder()
                        .WithAuthor($"Anilist Staff", null, "https://media.discordapp.net/attachments/1023808975185133638/1143013784584208504/AniList_logo.svg.png?width=588&height=588")
                        .WithTitle($"{staff.name.first} {staff.name.last}")
                        .WithDescription(staff.description)
                        .WithColor(DiscordColor.White)
                        .WithUrl(staff.siteUrl)
                        .WithThumbnail(staff.image.medium)
                        .WithFooter("Provided by https://anilist.co/"); 
                    await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(embed));
                }
                else
                {
                    var errorMessage = new DiscordEmbedBuilder()
                        .WithTitle("Thấy lỗi rồi nè~")
                        .WithDescription($"Mình không tìm được kết quả về {name} trong thanh tìm kiếm của anilist.")
                        .WithColor(DiscordColor.Red);

                    await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(errorMessage));
                }
            }
            catch(Exception ex)
            {
                var errorMessage = new DiscordEmbedBuilder()
                     .WithTitle("Thấy lỗi rồi nè~")
                    .WithDescription(ex.Message)
                    .WithColor(DiscordColor.Red);

                await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(errorMessage));
            }
        }

    }
}
