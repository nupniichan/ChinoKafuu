using AnimeListBot.Handler.Anilist;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using HtmlAgilityPack;
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
                .AddField("/AnimeInformation", "Xem thông tin về bộ anime")
                .AddField("/MangaInformation", "Xem thông tin về bộ manga")
                .AddField("/AniCharacterInformation", "Xem thông tin về nhân vật")
                .AddField("/AniStaffInformation", "Xem thông tin về những người làm ra")
                .WithFooter("Để sử dụng lệnh cụ thể, nhập /tên-lệnh");

            await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                new DiscordInteractionResponseBuilder().AddEmbed(embed));
        }

        [SlashCommand("AniUser", "Tìm profile trên anilist")]
        public async Task AniUserCommand(InteractionContext ctx, [Option("name", "Tên profile là gì?")] string name)
        {
            await ctx.DeferAsync();
            var user = await AniUserQuery.GetUser(name);
            if (user == null)
            {
                var errorMessage = new DiscordEmbedBuilder()
                        .WithTitle("Lỗi xảy ra")
                        .WithDescription("Mình không tìm thế tên người dùng đó")
                        .WithColor(DiscordColor.Red);

                await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(errorMessage));
            }
            else
            {
                try
                {
                    float minutesWatched = user.statistics.anime.minutesWatched;
                    float daysWatched = (float)minutesWatched / 60 / 24;
                    float dayWatch = (float)Math.Round(daysWatched, 2);

                    // get description 
                    var userAboutString = user.about;
                    var doc = new HtmlDocument();
                    doc.LoadHtml(userAboutString);
                    string userAbout = Helper.ProcessHtmlToMarkdown(doc.DocumentNode);

                    var embed = new DiscordEmbedBuilder()
                        .WithAuthor($"AniList Profile", null, "https://media.discordapp.net/attachments/1023808975185133638/1143013784584208504/AniList_logo.svg.png?width=588&height=588")
                        .WithTitle(user.name)
                        .WithUrl(user.siteUrl)
                        .WithDescription($"**ID: **{user.id}")
                        .AddField("**💬 About**", userAbout)
                        .AddField("\u2014", "\u200B") // Đường kẻ ngang
                        .AddField("📊 Anime Stats", $"Anime Count: {user.statistics.anime.count}\n" +
                                                  $"Mean Score: {user.statistics.anime.meanScore}\n" +
                                                  $"Days Watched: {dayWatch}\n" +
                                                  $"Episodes Watched: {user.statistics.anime.episodesWatched}", false)
                        .AddField("📊 Manga Stats", $"Manga Count: {user.statistics.manga.count}\n" +
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
        }

        [SlashCommand("AniUserFavorite", "Bạn có muốn biết người đó thích các bộ anime/manga nào?")]
        public async Task AniUserFavoriteCommand(InteractionContext ctx, [Option("user", "Tên của người bạn cần tra là ai nè~")] string name)
        {
            try
            {
                await ctx.DeferAsync();
                var user = await AniUserQuery.GetUser(name);
                if (user == null)
                {
                    var errorMessage = new DiscordEmbedBuilder()
                            .WithTitle("Lỗi xảy ra")
                            .WithDescription("Mình không tìm thế tên người dùng đó")
                            .WithColor(DiscordColor.Red);

                    await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(errorMessage));
                }
                else
                {
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
                        .AddField(":star: Favorite Anime", animeFavorites)
                        .AddField(":star: Favorite Manga", mangaFavorites)
                        .AddField(":star: Favorite Characters", characterList.ToString())
                        .AddField("Find Out More", $"[Anilist]({user.siteUrl})")
                        .WithColor(DiscordColor.Azure)
                        .WithFooter("Provided by https://anilist.co/ • Favorite");
                    embed.WithThumbnail(user.Avatar.medium);
                    await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(embed));
                }
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
        [SlashCommand("AnimeID", "Bạn muốn biết thông tin về bộ anime thông qua id?")]
        public async Task AnimeID(InteractionContext ctx, [Option("id", "Cho mình biết id về tên bộ anime đó đi")] long animeID)
        {
            await ctx.DeferAsync();
            var media = await AniMediaQuery.GetMedia((int)animeID, AniMediaType.ANIME);
            try
            {
                if (media.title != null)
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
                            startDateFormat = start.Value.ToString("dd/MM/yyyy");
                        }
                    }

                    AniFuzzyDate endDate = media.endDate;
                    DateTime? end = null;
                    string endDateFormat = "N/A";

                    if (endDate.month != null)
                    {
                        int? year = endDate.year;
                        int? month = endDate.month;
                        int? day = endDate.day;

                        if (year.HasValue && month.HasValue && day.HasValue)
                        {
                            end = new DateTime(year.Value, month.Value, day.Value);
                            endDateFormat = end.Value.ToString("dd/MM/yyyy");
                        }
                    }
                    // get description 
                    var descriptionString = media.description.ToString();
                    var doc = new HtmlDocument();
                    doc.LoadHtml(descriptionString);
                    string description = Helper.ProcessHtmlToMarkdown(doc.DocumentNode);

                    // check if the anime is released
                    bool isRelease = (media.status.ToString() != "NOT_YET_RELEASED");
                    if (isRelease)
                    {
                        // Lấy danh sách thể loại
                        List<string> genres = media.genres;
                        string genresString = string.Join(", ", genres);
                        var embed = new DiscordEmbedBuilder()
                        .WithAuthor($"{media.format}", null, "https://media.discordapp.net/attachments/1023808975185133638/1143013784584208504/AniList_logo.svg.png?width=588&height=588")
                        .WithTitle(media.title.english)
                        .WithUrl(media.siteUrl)
                        .WithDescription(description)
                        .AddField(":minidisc: Episodes", media.episodes.ToString(), true)
                        .AddField("⏱ Episode Duration", media.duration.ToString() + "mins", true)
                        .AddField(":hourglass_flowing_sand: Status", char.ToUpper(media.status.ToString()[0]) + media.status.ToString().Substring(1).ToLower(), true)
                        .AddField(":calendar_spiral: Aired", startDateFormat + " -> " + endDateFormat, false)
                        .AddField(":comet: Season", char.ToUpper(media.season[0]) + media.season.Substring(1).ToLower(), false)
                        .AddField(":file_folder: Source", char.ToUpper(media.source[0]) + media.source.Substring(1).ToLower(), false)
                        .AddField(":star: Average Score", media.averageScore.ToString() + "/100", true)
                        .AddField(":star: Mean Score", media.meanScore.ToString() + "/100", true)
                        .AddField(":arrow_right: Genres", genresString, false)
                        .AddField("🌐 Native", media.title.native, false)
                        .AddField("🛈 For more information", $"[Anilist]({media.siteUrl})")
                        .WithFooter("Provided by https://anilist.co/")
                        .WithColor(DiscordColor.Azure);
                        embed.WithThumbnail(media.coverImage.medium);
                        embed.WithImageUrl(media.bannerImage);
                        await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(embed));
                    }
                    else
                    {
                        var embed = new DiscordEmbedBuilder()
                            .WithAuthor($"{media.format}", null, "https://media.discordapp.net/attachments/1023808975185133638/1143013784584208504/AniList_logo.svg.png?width=588&height=588")
                            .WithTitle(media.title.english)
                            .WithUrl(media.siteUrl)
                            .WithDescription(description)
                            .AddField("Start Date ", startDateFormat, true)
                            .AddField("End Date ", endDateFormat , true)
                            .AddField("Status", media.status.ToString(), false)
                            .AddField("Note", "Bởi vì bộ này có thể dời thời gian nên mình không thể hiện chi tiết thời gian bắt đầu, kết thúc, season và một vài thông tin khác được~")
                            .AddField("For more information", $"[Anilist]({media.siteUrl})")
                            .WithFooter("Provided by https://anilist.co/")
                            .WithColor(DiscordColor.Azure);
                        embed.WithThumbnail(media.coverImage.medium);
                        embed.WithImageUrl(media.bannerImage);
                        await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(embed));
                    }
                }
                else
                {
                    var errorMessage = new DiscordEmbedBuilder()
                    .WithTitle("Thấy lỗi rồi nè~")
                    .WithDescription($"Mình không tìm được kết quả về {animeID} trong dữ liệu của anilist.")
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
        [SlashCommand("Anime", "Bạn muốn biết thông tin về bộ anime đó?")]
        public async Task Anime(InteractionContext ctx, [Option("name", "Cho mình biết tên về tên bộ anime đó đi")] string name)
        {
            await ctx.DeferAsync();
            var media = await AniMediaQuery.SearchMedia(name, AniMediaType.ANIME);
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
                            startDateFormat = start.Value.ToString("dd/MM/yyyy");
                        }
                    }

                    AniFuzzyDate endDate = media.endDate;
                    DateTime? end = null;
                    string endDateFormat = "N/A";

                    if (endDate.month != null)
                    {
                        int? year = endDate.year;
                        int? month = endDate.month;
                        int? day = endDate.day;

                        if (year.HasValue && month.HasValue && day.HasValue)
                        {
                            end = new DateTime(year.Value, month.Value, day.Value);
                            endDateFormat = end.Value.ToString("dd/MM/yyyy");
                        }
                    }
                    // get description 
                    var descriptionString = media.description.ToString();
                    var doc = new HtmlDocument();
                    doc.LoadHtml(descriptionString);
                    string description = Helper.ProcessHtmlToMarkdown(doc.DocumentNode);

                    // check if the anime is released
                    bool isRelease = (media.status.ToString() != "NOT_YET_RELEASED");
                    if (isRelease)
                    {
                        // Lấy danh sách thể loại
                        List<string> genres = media.genres;
                        string genresString = string.Join(", ", genres);
                        var embed = new DiscordEmbedBuilder()
                        .WithAuthor($"{media.format}", null, "https://media.discordapp.net/attachments/1023808975185133638/1143013784584208504/AniList_logo.svg.png?width=588&height=588")
                        .WithTitle(media.title.english)
                        .WithUrl(media.siteUrl)
                        .WithDescription(description)
                        .AddField(":minidisc: Episodes", media.episodes.ToString(), true)
                        .AddField("⏱ Episode Duration", media.duration.ToString() + "mins", true)
                        .AddField(":hourglass_flowing_sand: Status", char.ToUpper(media.status.ToString()[0]) + media.status.ToString().Substring(1).ToLower(), true)
                        .AddField(":calendar_spiral: Aired", startDateFormat + " -> " + endDateFormat, false)
                        .AddField(":comet: Season", char.ToUpper(media.season[0]) + media.season.Substring(1).ToLower(), false)
                        .AddField(":file_folder: Source", char.ToUpper(media.source[0]) + media.source.Substring(1).ToLower(), false)
                        .AddField(":star: Average Score", media.averageScore.ToString() + "/100", true)
                        .AddField(":star: Mean Score", media.meanScore.ToString() + "/100", true)
                        .AddField(":arrow_right: Genres", genresString, false)
                        .AddField("🌐 Native", media.title.native, false)
                        .AddField("🛈 For more information", $"[Anilist]({media.siteUrl})")
                        .WithFooter("Provided by https://anilist.co/")
                        .WithColor(DiscordColor.Azure);
                        embed.WithThumbnail(media.coverImage.medium);
                        embed.WithImageUrl(media.bannerImage);
                        await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(embed));
                    }
                    else
                    {
                        var embed = new DiscordEmbedBuilder()
                            .WithAuthor($"{media.format}", null, "https://media.discordapp.net/attachments/1023808975185133638/1143013784584208504/AniList_logo.svg.png?width=588&height=588")
                            .WithTitle(media.title.english)
                            .WithUrl(media.siteUrl)
                            .WithDescription(description)
                            .AddField("Start Date ", startDateFormat, true)
                            .AddField("End Date ", endDateFormat, true)
                            .AddField("Status", media.status.ToString(), false)
                            .AddField("Note", "Bởi vì bộ này có thể dời thời gian nên mình không thể hiện chi tiết thời gian bắt đầu, kết thúc, season và một vài thông tin khác được~")
                            .AddField("For more information", $"[Anilist]({media.siteUrl})")
                            .WithFooter("Provided by https://anilist.co/")
                            .WithColor(DiscordColor.Azure);
                        embed.WithThumbnail(media.coverImage.medium);
                        embed.WithImageUrl(media.bannerImage);
                        await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(embed));
                    }
                }
                else
                {
                    var errorMessage = new DiscordEmbedBuilder()
                    .WithTitle("Thấy lỗi rồi nè~")
                    .WithDescription($"Mình không tìm được kết quả về {name} trong dữ liệu của anilist. \n Bạn có thể cung cấp cho mình id để mình tìm sâu hơn nha")
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
        [SlashCommand("Manga", "Bạn muốn biết thông tin về bộ manga đó?")]
        public async Task Manga(InteractionContext ctx, [Option("name", "Cho mình biết tên về tên bộ manga đó đi")] string name)
        {
            await ctx.DeferAsync();
            var media = await AniMediaQuery.SearchMedia(name, AniMediaType.MANGA);
            try
            {
                if (media.title != null)
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
                            startDateFormat = start.Value.ToString("dd/MM/yyyy");
                        }
                    }

                    AniFuzzyDate endDate = media.endDate;
                    DateTime? end = null;
                    string endDateFormat = "N/A";

                    if (endDate.month != null)
                    {
                        int? year = endDate.year;
                        int? month = endDate.month;
                        int? day = endDate.day;

                        if (year.HasValue && month.HasValue && day.HasValue)
                        {
                            end = new DateTime(year.Value, month.Value, day.Value);
                            endDateFormat = end.Value.ToString("dd/MM/yyyy");
                        }
                    }
                    // get description 
                    var descriptionString = media.description.ToString();
                    var doc = new HtmlDocument();
                    doc.LoadHtml(descriptionString);
                    string description = Helper.ProcessHtmlToMarkdown(doc.DocumentNode);

                    // check if the anime is released
                    bool isRelease = (media.status.ToString() != "NOT_YET_RELEASED");
                    if (isRelease)
                    {
                        if (endDate.month == null)
                        {
                            // Lấy danh sách thể loại
                            List<string> genres = media.genres;
                            string genresString = string.Join(", ", genres);
                            var embed = new DiscordEmbedBuilder()
                            .WithAuthor($"{media.format}", null, "https://media.discordapp.net/attachments/1023808975185133638/1143013784584208504/AniList_logo.svg.png?width=588&height=588")
                            .WithTitle(media.title.english)
                            .WithUrl(media.siteUrl)
                            .WithDescription(description)
                            .AddField(":hourglass_flowing_sand: Status", char.ToUpper(media.status.ToString()[0]) + media.status.ToString().Substring(1).ToLower(), true)
                            .AddField(":calendar_spiral: Aired", startDateFormat + " -> " + "N/A", true)
                            .AddField(":arrow_right: Genres", genresString, false)
                            .AddField(":file_folder: Source", "Original", false)
                            .AddField(":star: Average Score", media.averageScore.ToString() + "/100", true)
                            .AddField(":star: Mean Score", media.meanScore.ToString() + "/100", true)
                            .AddField("🌐 Native", media.title.native, false)
                            .AddField("🛈 For more information", $"[Anilist]({media.siteUrl})")
                            .WithFooter("Provided by https://anilist.co/")
                            .WithColor(DiscordColor.Azure);
                            embed.WithThumbnail(media.coverImage.medium);
                            embed.WithImageUrl(media.bannerImage);
                            await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(embed));
                        }
                        else
                        {
                            // Lấy danh sách thể loại
                            List<string> genres = media.genres;
                            string genresString = string.Join(", ", genres);
                            var embed = new DiscordEmbedBuilder()
                            .WithAuthor($"{media.format}", null, "https://media.discordapp.net/attachments/1023808975185133638/1143013784584208504/AniList_logo.svg.png?width=588&height=588")
                            .WithTitle(media.title.english)
                            .WithUrl(media.siteUrl)
                            .WithDescription(description)
                            .AddField(":books: Volumes", media.volumes.ToString(), true)
                            .AddField(":book: Chapters", media.chapters.ToString(), true)
                            .AddField(":hourglass_flowing_sand: Status", char.ToUpper(media.status.ToString()[0]) + media.status.ToString().Substring(1).ToLower(), true)
                            .AddField(":calendar_spiral: Aired", startDateFormat + " -> " + endDateFormat, false)
                            .AddField(":file_folder: Source", "Original", false)
                            .AddField(":star: Average Score", media.averageScore.ToString() + "/100", true)
                            .AddField(":star: Mean Score", media.meanScore.ToString() + "/100", true)
                            .AddField(":arrow_right: Genres", genresString, false)
                            .AddField("🌐 Native", media.title.native, false)
                            .AddField("🛈 For more information", $"[Anilist]({media.siteUrl})")
                            .WithFooter("Provided by https://anilist.co/")
                            .WithColor(DiscordColor.Azure);
                            embed.WithThumbnail(media.coverImage.medium);
                            embed.WithImageUrl(media.bannerImage);
                            await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(embed));
                        }
                    }
                    else
                    {
                        var embed = new DiscordEmbedBuilder()
                            .WithAuthor($"{media.format}", null, "https://media.discordapp.net/attachments/1023808975185133638/1143013784584208504/AniList_logo.svg.png?width=588&height=588")
                            .WithTitle(media.title.english)
                            .WithUrl(media.siteUrl)
                            .WithDescription(description)
                            .AddField("Start Date ", startDateFormat, true)
                            .AddField("End Date ", endDateFormat, true)
                            .AddField("Status", media.status.ToString(), false)
                            .AddField("Note", "Bởi vì thời gian bộ này có thể thay đổi nên mình không thể hiện chi tiết một vài thông tin khác được~")
                            .AddField("For more information", $"[Anilist]({media.siteUrl})")
                            .WithFooter("Provided by https://anilist.co/")
                            .WithColor(DiscordColor.Azure);
                        embed.WithThumbnail(media.coverImage.medium);
                        embed.WithImageUrl(media.bannerImage);
                        await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(embed));
                    }
                }
                else
                {
                    var errorMessage = new DiscordEmbedBuilder()
                    .WithTitle("Thấy lỗi rồi nè~")
                    .WithDescription($"Mình không tìm được kết quả về {name} trong dữ liệu của anilist.\nBạn có thể cung cấp cho mình id để mình tìm sâu hơn nha")
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
        [SlashCommand("MangaID", "Bạn muốn biết thông tin về bộ manga thông qua id?")]
        public async Task MangaID(InteractionContext ctx, [Option("id", "Cho mình biết id về tên bộ manga đó đi")] long mangaID)
        {
            await ctx.DeferAsync();
            var media = await AniMediaQuery.GetMedia((int)mangaID, AniMediaType.MANGA);
            try
            {
                if (media.title != null)
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
                            startDateFormat = start.Value.ToString("dd/MM/yyyy");
                        }
                    }

                    AniFuzzyDate endDate = media.endDate;
                    DateTime? end = null;
                    string endDateFormat = "N/A";

                    if (endDate.month != null)
                    {
                        int? year = endDate.year;
                        int? month = endDate.month;
                        int? day = endDate.day;

                        if (year.HasValue && month.HasValue && day.HasValue)
                        {
                            end = new DateTime(year.Value, month.Value, day.Value);
                            endDateFormat = end.Value.ToString("dd/MM/yyyy");
                        }
                    }
                    // get description 
                    var descriptionString = media.description.ToString();
                    var doc = new HtmlDocument();
                    doc.LoadHtml(descriptionString);
                    string description = Helper.ProcessHtmlToMarkdown(doc.DocumentNode);

                    // check if the anime is released
                    bool isRelease = (media.status.ToString() != "NOT_YET_RELEASED");
                    if (isRelease)
                    {
                        if (endDate.month == null)
                        {
                            // Lấy danh sách thể loại
                            List<string> genres = media.genres;
                            string genresString = string.Join(", ", genres);
                            var embed = new DiscordEmbedBuilder()
                            .WithAuthor($"{media.format}", null, "https://media.discordapp.net/attachments/1023808975185133638/1143013784584208504/AniList_logo.svg.png?width=588&height=588")
                            .WithTitle(media.title.english)
                            .WithUrl(media.siteUrl)
                            .WithDescription(description)
                            .AddField(":hourglass_flowing_sand: Status", char.ToUpper(media.status.ToString()[0]) + media.status.ToString().Substring(1).ToLower(), true)
                            .AddField(":calendar_spiral: Aired", startDateFormat + " -> " + "N/A", true)
                            .AddField(":arrow_right: Genres", genresString, false)
                            .AddField(":file_folder: Source", char.ToUpper(media.source[0]) + media.source.Substring(1).ToLower(), false)
                            .AddField(":star: Average Score", media.averageScore.ToString() + "/100", true)
                            .AddField(":star: Mean Score", media.meanScore.ToString() + "/100", true)
                            .AddField("🌐 Native", media.title.native, false)
                            .AddField("🛈 For more information", $"[Anilist]({media.siteUrl})")
                            .WithFooter("Provided by https://anilist.co/")
                            .WithColor(DiscordColor.Azure);
                            embed.WithThumbnail(media.coverImage.medium);
                            embed.WithImageUrl(media.bannerImage);
                            await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(embed));
                        }
                        else
                        {
                            // Lấy danh sách thể loại
                            List<string> genres = media.genres;
                            string genresString = string.Join(", ", genres);
                            var embed = new DiscordEmbedBuilder()
                            .WithAuthor($"{media.format}", null, "https://media.discordapp.net/attachments/1023808975185133638/1143013784584208504/AniList_logo.svg.png?width=588&height=588")
                            .WithTitle(media.title.english)
                            .WithUrl(media.siteUrl)
                            .WithDescription(description)
                            .AddField(":books: Volumes", media.volumes.ToString(), true)
                            .AddField(":book: Chapters", media.chapters.ToString(), true)
                            .AddField(":hourglass_flowing_sand: Status", char.ToUpper(media.status.ToString()[0]) + media.status.ToString().Substring(1).ToLower(), true)
                            .AddField(":calendar_spiral: Aired", startDateFormat + " -> " + endDateFormat, false)
                            .AddField(":file_folder: Source","Original", false)
                            .AddField(":star: Average Score", media.averageScore.ToString() + "/100", true)
                            .AddField(":star: Mean Score", media.meanScore.ToString() + "/100", true)
                            .AddField(":arrow_right: Genres", genresString, false)
                            .AddField("🌐 Native", media.title.native, false)
                            .AddField("🛈 For more information", $"[Anilist]({media.siteUrl})")
                            .WithFooter("Provided by https://anilist.co/")
                            .WithColor(DiscordColor.Azure);
                            embed.WithThumbnail(media.coverImage.medium);
                            embed.WithImageUrl(media.bannerImage);
                            await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(embed));
                        }
                    }
                    else
                    {
                        var embed = new DiscordEmbedBuilder()
                            .WithAuthor($"{media.format}", null, "https://media.discordapp.net/attachments/1023808975185133638/1143013784584208504/AniList_logo.svg.png?width=588&height=588")
                            .WithTitle(media.title.english)
                            .WithUrl(media.siteUrl)
                            .WithDescription(description)
                            .AddField("Start Date ", startDateFormat, true)
                            .AddField("End Date ", endDateFormat, true)
                            .AddField("Status", media.status.ToString(), false)
                            .AddField("Note", "Bởi vì thời gian bộ này có thể thay đổi nên mình không thể hiện chi tiết một vài thông tin khác được~")
                            .AddField("For more information", $"[Anilist]({media.siteUrl})")
                            .WithFooter("Provided by https://anilist.co/")
                            .WithColor(DiscordColor.Azure);
                        embed.WithThumbnail(media.coverImage.medium);
                        embed.WithImageUrl(media.bannerImage);
                        await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(embed));
                    }
                }
                else
                {
                    var errorMessage = new DiscordEmbedBuilder()
                    .WithTitle("Thấy lỗi rồi nè~")
                    .WithDescription($"Mình không tìm được kết quả về {mangaID} trong dữ liệu của anilist.")
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
        [SlashCommand("AniCharacterInformation", "Bạn muốn biết thêm thông tin về nhân vật đó?")]
        public async Task CharacterInformation(InteractionContext ctx, [Option("name", "Cung cấp cho mình về tên nhân vật đó đi")] string name)
        {
            await ctx.DeferAsync();
            var character = await AniCharacterQuery.SearchCharacter(name);
            try
            {
                if (character != null)
                {
                    // get description 
                    var descriptionString = character.description.ToString();
                    var doc = new HtmlDocument();
                    doc.LoadHtml(descriptionString);
                    string description = Helper.ProcessHtmlToMarkdown(doc.DocumentNode);

                    var embed = new DiscordEmbedBuilder()
                        .WithAuthor($"Anilist Character", null, "https://media.discordapp.net/attachments/1023808975185133638/1143013784584208504/AniList_logo.svg.png?width=588&height=588")
                        .WithThumbnail(character.image.medium)
                        .WithTitle($"{character.name.first} {character.name.last}")
                        .WithDescription(description)
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
                        .WithDescription($"Mình không tìm được kết quả về {name} trong dữ liệu của anilist.")
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
        [SlashCommand("AniStaffInformation","Bạn muốn biết thông tin về những người làm ra?")]
        public async Task StaffInformation(InteractionContext ctx, [Option("name", "Cung cấp cho mình về tên của người đó đi")] string name)
        {
            await ctx.DeferAsync();
            var staff = await AniStaffQuery.SearchStaff(name);
            try
            {
                if (staff != null)
                {
                    // get description 
                    var descriptionString = staff.description.ToString();
                    var doc = new HtmlDocument();
                    doc.LoadHtml(descriptionString);
                    string description = Helper.ProcessHtmlToMarkdown(doc.DocumentNode);

                    var embed = new DiscordEmbedBuilder()
                        .WithAuthor($"Anilist Staff", null, "https://media.discordapp.net/attachments/1023808975185133638/1143013784584208504/AniList_logo.svg.png?width=588&height=588")
                        .WithTitle($"{staff.name.first} {staff.name.last}")
                        .WithDescription(description)
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
                        .WithDescription($"Mình không tìm được kết quả về {name} trong dữ liệu của anilist.")
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
