using ChinoKafuu.Utils;
using CsAnilist.Models.Enums;
using CsAnilist.Models.Character;
using CsAnilist.Models.Media;
using CsAnilist.Models.Staff;
using CsAnilist.Models.Studio;
using CsAnilist.Models.User;
using CsAnilist.Services;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using System.Net;
using System.Text.RegularExpressions;


namespace ChinoBot.CommandsFolder.SlashCommandsFolder
{
    internal class AnilistSlashCommand : ApplicationCommandModule
    {
        private const string ANILIST_LOGO = "https://media.discordapp.net/attachments/1023808975185133638/1143013784584208504/AniList_logo.svg.png?width=588&height=588";
        private const string ANILIST_URL = "https://anilist.co/";
        private CsAniListService anilistService = new CsAniListService();

        [SlashCommand("ani-help", "Hiển thị trợ giúp về các lệnh Anilist")]
        public async Task AniHelpCommand(InteractionContext ctx)
        {
            var embed = new DiscordEmbedBuilder()
                .WithTitle("Danh sách các lệnh Anilist")
                .WithDescription("Dưới đây là danh sách các lệnh Anilist có sẵn:")
                .WithColor(DiscordColor.Azure)
                .AddField("/ani-user", "Tìm profile trên Anilist")
                .AddField("/ani-userFavorite", "Xem những bộ anime/manga mà người đó thích")
                .AddField("/anime", "Xem thông tin về bộ anime")
                .AddField("/manga", "Xem thông tin về bộ manga")
                .AddField("/ani-character", "Xem thông tin về nhân vật")
                .AddField("/ani-staff", "Xem thông tin về những người làm ra")
                .AddField("/ani-studio", "Xem một vài thông tin về studio đó")
                .AddField("/ani-trailer", "Xem trailer của bộ phim đó")
                .WithFooter("Để sử dụng lệnh cụ thể, nhập /tên-lệnh");

            await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                new DiscordInteractionResponseBuilder().AddEmbed(embed));
        }

        [SlashCommand("ani-user", "Tìm profile trên anilist")]
        public async Task AniUserCommand(InteractionContext ctx, [Option("name", "Tên profile là gì?")] string name)
        {
            await ctx.DeferAsync();
            var user = await anilistService.SearchUserAsync(name);

            if (user == null)
            {
                await SendErrorEmbed(ctx, $"Không tìm thấy người dùng: {name}");
                return;
            }

            var embed = CreateUserEmbed(user);
            await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(embed));
        }

        [SlashCommand("ani-userFavorite", "Xem những bộ anime/manga mà người đó thích")]
        public async Task AniUserFavoriteCommand(InteractionContext ctx, [Option("user", "Tên của người bạn cần tra là ai nè~")] string name)
        {
            await ctx.DeferAsync();
            var user = await anilistService.SearchUserAsync(name);

            if (user == null)
            {
                await SendErrorEmbed(ctx, $"Không tìm thấy người dùng: {name}");
                return;
            }

            var embed = CreateUserFavoriteEmbed(user);
            await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(embed));
        }

        [SlashCommand("anime", "Xem thông tin về bộ anime")]
        public async Task AnimeCommand(InteractionContext ctx, [Option("name", "Tên anime")] string name)
        {
            await SearchMediaCommand(ctx, name, MediaType.ANIME);
        }

        [SlashCommand("manga", "Xem thông tin về bộ manga")]
        public async Task MangaCommand(InteractionContext ctx, [Option("name", "Tên manga")] string name)
        {
            await SearchMediaCommand(ctx, name, MediaType.MANGA);
        }

        [SlashCommand("ani-character", "Xem thông tin về nhân vật")]
        public async Task CharacterInformationCommand(InteractionContext ctx, [Option("name", "Tên nhân vật")] string name)
        {
            await ctx.DeferAsync();
            var character = await anilistService.SearchCharacterAsync(name);
            if (character == null)
            {
                await SendErrorEmbed(ctx, $"Không tìm thấy nhân vật: {name}");
                return;
            }

            var embed = CreateCharacterEmbed(character);
            await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(embed));
        }

        [SlashCommand("ani-staff", "Xem thông tin về những người đóng góp vào bộ anime đó")]
        public async Task StaffInformationCommand(InteractionContext ctx, [Option("name", "Tên của người đó")] string name)
        {
            await ctx.DeferAsync();
            var staff = await anilistService.SearchStaffAsync(name);

            if (staff == null)
            {
                await SendErrorEmbed(ctx, $"Không tìm thấy staff: {name}");
                return;
            }

            var embed = CreateStaffEmbed(staff);
            await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(embed));
        }

        [SlashCommand("ani-studio", "Xem một vài thông tin về studio đó")]
        public async Task StudioInformationCommand(InteractionContext ctx, [Option("name", "Tên của studio")] string name)
        {
            await ctx.DeferAsync();
            var studio = await anilistService.SearchStudioAsync(name);

            if (studio == null)
            {
                await SendErrorEmbed(ctx, $"Không tìm thấy studio: {name}");
                return;
            }

            var embed = CreateStudioEmbed(studio);
            await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(embed));
        }

        [SlashCommand("ani-trailer", "Xem trailer của bộ anime đó")]
        public async Task GetVideoTrailer(InteractionContext ctx, [Option("name", "Tên anime")] string name)
        {
            await ctx.DeferAsync();
            var media = await anilistService.SearchMedia(name, MediaType.ANIME);
            if (media == null)
            {
                await SendErrorEmbed(ctx, $"Không tìm thấy anime: {name}");
                return;
            }
            if (media.trailer == null)
            {
                await SendErrorEmbed(ctx, $"Không tìm thấy trailer anime: {name}");
                return;
            }

            var trailerUrl = $"https://www.{media.trailer.site}.com/watch?v={media.trailer.id}";

            await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent(trailerUrl));
        }

        private async Task SearchMediaCommand(InteractionContext ctx, string search, MediaType type)
        {
            try
            {
                await ctx.DeferAsync();
                var media = await anilistService.SearchMedia(search, type);

                if (media == null)
                {
                    await SendErrorEmbed(ctx, $"Không tìm thấy {(type == MediaType.ANIME ? "anime" : "manga")}: {search}");
                    return;
                }

                var embed = CreateMediaEmbed(media, type == MediaType.ANIME);
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(embed));
            }
            catch (Exception e)
            {
                await Console.Out.WriteLineAsync(e.Message);
                return;
            }
        }

        private DiscordEmbed CreateUserEmbed(AniUser user)
        {
            float daysWatched = user.statistics.anime.minutesWatched > 0
                ? (float)user.statistics.anime.minutesWatched / 60 / 24
                : 0;

            string userAbout = ProcessDescription(user.about);

            if (string.IsNullOrWhiteSpace(userAbout))
            {
                userAbout = "*Không có mô tả*";
            }

            var embedBuilder = new DiscordEmbedBuilder()
                .WithAuthor("AniList Profile", null, ANILIST_LOGO)
                .WithTitle(user.name)
                .WithUrl(user.siteUrl)
                .WithDescription($"**ID: **{user.id}")
                .AddField("**💬 Mô tả**", userAbout, false)
                .AddField("\u2014", "\u200B", false)
                .AddField("📊 **Anime Stats**",
                    $"**Tổng bộ anime đã xem:** {user.statistics.anime.count:N0} bộ\n" + 
                    $"**Điểm trung bình:** {user.statistics.anime.meanScore:F2}/100\n" +
                    $"**Số ngày đã xem:** {daysWatched:F2} ngày\n" +
                    $"**Số tập đã xem:** {user.statistics.anime.episodesWatched:N0} tập",
                    false)
                .AddField("📚 **Manga Stats**",
                    $"**Số manga đã đọc:** {user.statistics.manga.count:N0} bộ\n" +
                    $"**Điểm trung bình:** {user.statistics.manga.meanScore:F2}/100\n" +
                    $"**Số chapters đã đọc:** {user.statistics.manga.chaptersRead:N0} chapters\n" +
                    $"**Số volumes đã đọc:** {user.statistics.manga.volumesRead:N0} volumes",
                    false)
                .WithColor(DiscordColor.Azure)
                .WithFooter($"{ANILIST_URL}")
                .WithThumbnail(user.avatar.medium);

            if (!string.IsNullOrWhiteSpace(user.bannerImage))
            {
                embedBuilder.WithImageUrl(user.bannerImage);
            }

            return embedBuilder;
        }


        private DiscordEmbed CreateUserFavoriteEmbed(AniUser user)
        {
            string animeFavorites = string.Join("\n", user.favourites.anime.nodes.Select(a => $"[{a.title.english}]({a.siteUrl})"));
            string mangaFavorites = string.Join("\n", user.favourites.manga.nodes.Select(m => $"[{m.title.english}]({m.siteUrl})"));
            string characterFavorites = string.Join("\n", user.favourites.characters.nodes.Select(c => $"[{c.name.first} {c.name.last}]({c.siteUrl})"));
            string staffFavorites = string.Join("\n", user.favourites.staff.nodes.Select(c => $"[{c.name.first} {c.name.last}]({c.siteUrl})"));
            string studioFavorites = string.Join("\n", user.favourites.studios.nodes.Select(c => $"[{c.name}]({c.siteUrl})"));

            string animeCount = user.favourites.anime.nodes.Count > 0 ? $"{user.favourites.anime.nodes.Count}" : "0";
            string mangaCount = user.favourites.manga.nodes.Count > 0 ? $"{user.favourites.manga.nodes.Count}" : "0";
            string characterCount = user.favourites.characters.nodes.Count > 0 ? $"{user.favourites.characters.nodes.Count}" : "0";
            string staffCount = user.favourites.staff.nodes.Count > 0 ? $"{user.favourites.staff.nodes.Count}" : "0";
            string studioCount = user.favourites.studios.nodes.Count > 0 ? $"{user.favourites.studios.nodes.Count}" : "0";

            return new DiscordEmbedBuilder()
                .WithAuthor("AniList Favorite", null, ANILIST_LOGO)
                .WithTitle(user.name)
                .AddField($":star: **Anime yêu thích - {animeCount} bộ**", string.IsNullOrEmpty(animeFavorites) ? $"**Không tìm thấy**" : $"{animeFavorites}\n\n", false)
                .AddField($":star: **Manga yêu thích - {mangaCount} bộ**", string.IsNullOrEmpty(mangaFavorites) ? $"**Không tìm thấy**" : $"{mangaFavorites}\n\n", false)
                .AddField($":star: **Nhân vật yêu thích - {characterCount} nhân vật**", string.IsNullOrEmpty(characterFavorites) ? $"**Không tìm thấy**" : $"{characterFavorites}\n\n", false)
                .AddField($":star: **Staff yêu thích - {staffCount} staff**", string.IsNullOrEmpty(staffFavorites) ? $"**Không tìm thấy**" : $"{staffFavorites}\n\n", false)
                .AddField($":star: **Studio yêu thích - {studioCount} studio**", string.IsNullOrEmpty(studioFavorites) ? $"**Không tìm thấy**" : $"{studioFavorites}\n\n", false)
                .AddField("Xem thêm tại đây", $"[Anilist]({user.siteUrl})")
                .WithColor(DiscordColor.Azure)
                .WithFooter($"{ANILIST_URL}")
                .WithThumbnail(user.avatar.medium)
                .WithImageUrl(user.bannerImage ?? ANILIST_LOGO) 

                .Build();
        }

        private DiscordEmbed CreateMediaEmbed(AniMedia media, bool isAnime)
        {
            string description = ProcessDescription(media.description);
            string startDate = FormatDate(media.startDate);
            string endDate = media.endDate == null ? "N/A" : FormatDate(media.endDate);
            string status = media.status switch
            {
                MediaStatus.FINISHED => "Đã hoàn thành",
                MediaStatus.RELEASING => "Đang phát sóng",
                MediaStatus.CANCELLED => "Đã bị huỷ",
                _ => "Chưa phát sóng"
            };

            var embed = new DiscordEmbedBuilder()
                .WithAuthor($"{media.format}", null, ANILIST_LOGO)
                .WithTitle(media.title.english ?? media.title.romaji)
                .WithUrl(media.siteUrl)
                .WithDescription(description)
                .WithFooter($"{ANILIST_URL}")
                .WithColor(DiscordColor.Azure)
                .WithThumbnail(media.coverImage.medium)
                .WithImageUrl(media.bannerImage);

            string utcOffsetString = Util.GetUtcOffsetString();
            
            string seasonName = media.season.HasValue ? media.season.ToString() : "UNKNOWN";
            string sourceName = media.source.HasValue ? media.source.ToString() : "UNKNOWN";

            if (isAnime)
            {
                if (media.status == MediaStatus.FINISHED)
                {
                    embed.AddField(":minidisc: Số tập", media.episodes.ToString(), true)
                         .AddField("⏱ Thời lượng", $"{media.duration} phút", true)
                         .AddField(":hourglass_flowing_sand: Trạng thái", status, true)
                         .AddField(":calendar_spiral: Phát sóng", $"{startDate} -> {endDate}", false)
                         .AddField(":comet: Mùa", FormatEnumValue(seasonName), false)
                         .AddField(":file_folder: Nguồn", FormatEnumValue(sourceName), false)
                         .AddField(":star: Điểm trung bình", $"{media.averageScore}/100", true)
                         .AddField(":star: Điểm trung vị", $"{media.meanScore}/100", true)
                         .AddField(":thumbsup: Số lượt thích", $"{media.favourites}", true)
                         .AddField(":arrow_right: Thể loại", string.Join(", ", media.genres), false)
                         .AddField("🌐 Tên gốc", media.title.native, false)
                         .AddField("🛈 Thông tin thêm", $"[Anilist]({media.siteUrl})");
                }
                else if (media.status == MediaStatus.RELEASING)
                {
                    if (media.airingSchedule?.nodes?.FirstOrDefault() != null)
                    {
                        var nextEpisode = media.airingSchedule.nodes.First();
                        var airingTime = CalculateAiringTime(nextEpisode.timeUntilAiring);

                        embed.AddField(":calendar_spiral: Phát sóng", $"{startDate} -> N/A", true)
                             .AddField(":hourglass_flowing_sand: Trạng thái", status, true)
                             .AddField(":comet: Mùa", FormatEnumValue(seasonName), true)
                             .AddField(":calendar: Tập tiếp theo", $"Tập {nextEpisode.episode}, sẽ được phát sóng sau: {airingTime} ({utcOffsetString})", false)
                             .AddField("⏱ Thời lượng tập", $"{media.duration} phút", false)
                             .AddField(":file_folder: Nguồn", FormatEnumValue(sourceName), false)
                             .AddField(":star: Điểm trung bình", $"{media.averageScore}/100", true)
                             .AddField(":star: Điểm trung vị", $"{media.meanScore}/100", true)
                             .AddField(":thumbsup: Số lượt thích", $"{media.favourites}", true)
                             .AddField(":arrow_right: Thể loại", string.Join(", ", media.genres), false)
                             .AddField("🌐 Tên gốc", media.title.native, false)
                             .AddField("🛈 Thông tin thêm", $"[Anilist]({media.siteUrl})");
                    }
                    else
                    {
                        embed.AddField(":calendar_spiral: Phát sóng", $"{startDate} -> N/A", true)
                             .AddField(":hourglass_flowing_sand: Trạng thái", status, true)
                             .AddField(":comet: Mùa", FormatEnumValue(seasonName), true)
                             .AddField("⏱ Thời lượng tập", $"{media.duration} phút", false)
                             .AddField(":file_folder: Nguồn", FormatEnumValue(sourceName), false)
                             .AddField(":star: Điểm trung bình", $"{media.averageScore}/100", true)
                             .AddField(":star: Điểm trung vị", $"{media.meanScore}/100", true)
                             .AddField(":thumbsup: Số lượt thích", $"{media.favourites}", true)
                             .AddField(":arrow_right: Thể loại", string.Join(", ", media.genres), false)
                             .AddField("🌐 Tên gốc", media.title.native, false)
                             .AddField("🛈 Thông tin thêm", $"[Anilist]({media.siteUrl})");
                    }
                }
                else
                {
                    embed.AddField(":calendar_spiral: Phát sóng", $"{startDate} -> N/A", true)
                             .AddField(":hourglass_flowing_sand: Trạng thái", status, true)
                             .AddField("⏱ Thời lượng tập", $"{media.duration} phút", false)
                         .AddField(":file_folder: Nguồn", FormatEnumValue(sourceName), false)
                             .AddField(":star: Điểm trung bình", $"{media.averageScore}/100", true)
                             .AddField(":star: Điểm trung vị", $"{media.meanScore}/100", true)
                             .AddField(":thumbsup: Số lượt thích", $"{media.favourites}", true)
                             .AddField(":arrow_right: Thể loại", string.Join(", ", media.genres), false)
                             .AddField("🌐 Tên gốc", media.title.native, false)
                             .AddField("🛈 Thông tin thêm", $"[Anilist]({media.siteUrl})");
                }
            }
            else
            {
                if (media.status == MediaStatus.FINISHED)
                {
                    embed.AddField(":hourglass_flowing_sand: Trạng thái: ", status, true)
                         .AddField(":calendar_spiral: Phát hành", $"{startDate} -> {endDate}", true)
                         .AddField(":arrow_right: Thể loại", string.Join(", ", media.genres), false)
                         .AddField(":file_folder: Nguồn", FormatEnumValue(sourceName), false)
                         .AddField(":star: Điểm trung bình", $"{media.averageScore}/100", true)
                         .AddField(":star: Điểm trung vị", $"{media.meanScore}/100", true)
                         .AddField(":thumbsup: Số lượt thích", $"{media.favourites}", true)
                         .AddField("🌐 Tên gốc", media.title.native, false)
                         .AddField("🛈 Thông tin thêm", $"[Anilist]({media.siteUrl})");
                }
                else if (media.status == MediaStatus.RELEASING)
                {
                    embed.AddField(":hourglass_flowing_sand: Trạng thái", status, true)
                         .AddField(":calendar_spiral: Phát hành", $"{startDate} -> N/A", true)
                         .AddField(":arrow_right: Thể loại", string.Join(", ", media.genres), false)
                         .AddField(":file_folder: Nguồn", FormatEnumValue(sourceName), false)
                         .AddField(":star: Điểm trung bình", $"{media.averageScore}/100", true)
                         .AddField(":star: Điểm trung vị", $"{media.meanScore}/100", true)
                         .AddField(":thumbsup: Số lượt thích", $"{media.favourites}", true)
                         .AddField("🌐 Tên gốc", media.title.native, false)
                         .AddField("🛈 Thông tin thêm", $"[Anilist]({media.siteUrl})");
                }
                else
                {
                    embed.AddField(":hourglass_flowing_sand: Trạng thái", status, true)
                         .AddField(":calendar_spiral: Phát hành", $"{startDate} -> {(endDate != null ? endDate.ToString() : "N/A")}", true)
                         .AddField(":arrow_right: Thể loại", string.Join(", ", media.genres), false)
                         .AddField(":file_folder: Nguồn", FormatEnumValue(sourceName), false)
                         .AddField(":star: Điểm trung bình", $"{media.averageScore}/100", true)
                         .AddField(":star: Điểm trung vị", $"{media.meanScore}/100", true)
                         .AddField(":thumbsup: Số lượt thích", $"{media.favourites}", true)
                         .AddField("🌐 Tên gốc", media.title.native, false)
                         .AddField("🛈 Thông tin thêm", $"[Anilist]({media.siteUrl})");
                }
            }
            return embed;
        }


        private DiscordEmbed CreateCharacterEmbed(AniCharacter character)
        {
            string description = ProcessDescription(character.description);

            return new DiscordEmbedBuilder()
                .WithAuthor("Anilist Character", null, ANILIST_LOGO)
                .WithThumbnail(character.image.medium)
                .WithTitle($"{character.name.first} {character.name.last}")
                .WithDescription(description)
                .WithUrl(character.siteUrl)
                .AddField("Giới tính: ", character.gender == "Female" ? "Nữ" : "Nam", true)
                .AddField(":calendar_spiral: Ngày sinh: ", $"{character.dateOfBirth.day}/{character.dateOfBirth.month}",true)
                .AddField(":heart: Số lượt thích", $"{character.favourites}", true)
                .AddField("Tên khác", string.Join(", ", character.name.alternative))
                .WithColor(DiscordColor.Azure)
                .WithFooter($"{ANILIST_URL}");
        }

        private DiscordEmbed CreateStaffEmbed(AniStaff staff)
        {
            string description = ProcessDescription(staff.description);

            return new DiscordEmbedBuilder()
                .WithAuthor("Anilist Staff", null, ANILIST_LOGO)
                .WithTitle($"{staff.name.first} {staff.name.last}")
                .WithDescription(description)
                .WithColor(DiscordColor.Azure)
                .WithUrl(staff.siteUrl)
                .AddField("Giới tính: ", staff.gender != null ? staff.gender : "Không có thông tin",true )
                .AddField(":homes: Quê quán: ", staff.homeTown != null ? staff.homeTown : "Không có thông tin",true)
                .AddField(":heart: Số lượt thích", $"{staff.favourites}", true)
                .WithThumbnail(staff.image.medium)
                .WithFooter($"{ANILIST_URL}");
        }

        private DiscordEmbed CreateStudioEmbed(AniStudio studio)
        {
            return new DiscordEmbedBuilder()
                .WithAuthor("Anilist Staff", null, ANILIST_LOGO)
                .WithTitle($"{studio.name}")
                .WithColor(DiscordColor.Azure)
                .WithUrl(studio.siteUrl)
                .AddField(":heart: Số lượt thích", $"{studio.favourites}", true)
                .WithFooter($"{ANILIST_URL}");
        }

        private string ProcessDescription(string description)
        {
            if (string.IsNullOrEmpty(description))
                return string.Empty;

            description = Regex.Replace(description, @"<[^>]*>", "");
            description = Regex.Replace(description, @"<img[^>]*>", "");
            description = description.Replace("~~~", "").Trim();
            description = WebUtility.HtmlDecode(description);

            if (description.Length > 1024)
            {
                description = description.Substring(0, 1021) + "...";
            }

            return description;
        }

        private string FormatDate(MediaDate date)
        {
            return date.year.HasValue && date.month.HasValue && date.day.HasValue
                ? new DateTime(date.year.Value, date.month.Value, date.day.Value).ToString("dd/MM/yyyy")
                : "N/A";
        }

        private string CalculateAiringTime(int secondsUntilAiring)
        {
            var days = secondsUntilAiring / (60 * 60 * 24);
            var hours = (secondsUntilAiring % (60 * 60 * 24)) / (60 * 60);
            var minutes = (secondsUntilAiring % (60 * 60)) / 60;
            return $"{days}d {hours}h {minutes}m";
        }

        private string FormatEnumValue(string enumValue)
        {
            if (string.IsNullOrEmpty(enumValue))
                return "Không xác định";
                
            return char.ToUpper(enumValue[0]) + enumValue.Substring(1).ToLower();
        }

        private async Task SendErrorEmbed(InteractionContext ctx, string errorMessage)
        {
            var errorEmbed = new DiscordEmbedBuilder()
                .WithTitle("Lỗi xảy ra")
                .WithDescription(errorMessage)
                .WithColor(DiscordColor.Red);

            await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(errorEmbed));
        }
    }
}