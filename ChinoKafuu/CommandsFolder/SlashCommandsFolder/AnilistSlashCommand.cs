using AnilistAPI;
using AnilistAPI.AnilistAPI;
using AnilistAPI.AnilistAPI.Enum;
using AnilistAPI.Objects.Object;
using ChinoBot.Engine.Anilist.Objects;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using HtmlAgilityPack;

namespace ChinoBot.CommandsFolder.SlashCommandsFolder
{
    internal class AnilistSlashCommand : ApplicationCommandModule
    {
        private const string ANILIST_LOGO = "https://media.discordapp.net/attachments/1023808975185133638/1143013784584208504/AniList_logo.svg.png?width=588&height=588";
        private const string ANILIST_URL = "anilist.co";

        [SlashCommand("AniHelp", "Hiển thị trợ giúp về các lệnh Anilist")]
        public async Task AniHelpCommand(InteractionContext ctx)
        {
            var embed = new DiscordEmbedBuilder()
                .WithTitle("Danh sách các lệnh Anilist")
                .WithDescription("Dưới đây là danh sách các lệnh Anilist có sẵn:")
                .WithColor(DiscordColor.Azure)
                .AddField("/AniUser", "Tìm profile trên Anilist")
                .AddField("/AniuserFavorite", "Xem những bộ anime/manga mà người đó thích")
                .AddField("/Anime", "Xem thông tin về bộ anime")
                .AddField("/Manga", "Xem thông tin về bộ manga")
                .AddField("/AniCharacter", "Xem thông tin về nhân vật")
                .AddField("/AniStaff", "Xem thông tin về những người làm ra")
                .WithFooter("Để sử dụng lệnh cụ thể, nhập /tên-lệnh");

            await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                new DiscordInteractionResponseBuilder().AddEmbed(embed));
        }

        [SlashCommand("AniUser", "Tìm profile trên anilist")]
        public async Task AniUserCommand(InteractionContext ctx, [Option("name", "Tên profile là gì?")] string name)
        {
            await ctx.DeferAsync();
            var user = await AnilistGraphQL.GetUserAsync(AniQuery.UserSearchQuery, new { name, asHtml = false });

            if (user == null)
            {
                await SendErrorEmbed(ctx, $"Không tìm thấy người dùng: {name}");
                return;
            }

            var embed = CreateUserEmbed(user);
            await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(embed));
        }

        [SlashCommand("AniUserFavorite", "Xem những bộ anime/manga mà người đó thích")]
        public async Task AniUserFavoriteCommand(InteractionContext ctx, [Option("user", "Tên của người bạn cần tra là ai nè~")] string name)
        {
            await ctx.DeferAsync();
            var user = await AnilistGraphQL.GetUserAsync(AniQuery.UserSearchQuery, new { name, asHtml = false });

            if (user == null)
            {
                await SendErrorEmbed(ctx, $"Không tìm thấy người dùng: {name}");
                return;
            }

            var embed = CreateUserFavoriteEmbed(user);
            await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(embed));
        }

        [SlashCommand("Anime", "Xem thông tin về bộ anime")]
        public async Task AnimeCommand(InteractionContext ctx, [Option("name", "Tên anime")] string name)
        {
            await SearchMediaCommand(ctx, name, AniMediaType.ANIME);
        }

        [SlashCommand("Manga", "Xem thông tin về bộ manga")]
        public async Task MangaCommand(InteractionContext ctx, [Option("name", "Tên manga")] string name)
        {
            await SearchMediaCommand(ctx, name, AniMediaType.MANGA);
        }

        [SlashCommand("AniCharacter", "Xem thông tin về nhân vật")]
        public async Task CharacterInformationCommand(InteractionContext ctx, [Option("name", "Tên nhân vật")] string name)
        {
            await ctx.DeferAsync();
            var character = await AnilistGraphQL.GetCharacterAsync(AniQuery.CharacterSearchQuery, new { search = name, asHtml = false });
            if (character == null)
            {
                await SendErrorEmbed(ctx, $"Không tìm thấy nhân vật: {name}");
                return;
            }

            var embed = CreateCharacterEmbed(character);
            await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(embed));
        }

        [SlashCommand("AniStaff", "Xem thông tin về những người đóng góp vào bộ anime đó")]
        public async Task StaffInformationCommand(InteractionContext ctx, [Option("name", "Tên của người đó")] string name)
        {
            await ctx.DeferAsync();
            var staff = await AnilistGraphQL.GetStaffAsync(AniQuery.StaffSearchQuery, new { search = name, asHtml = true });

            if (staff == null)
            {
                await SendErrorEmbed(ctx, $"Không tìm thấy staff: {name}");
                return;
            }

            var embed = CreateStaffEmbed(staff);
            await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(embed));
        }

        private async Task SearchMediaCommand(InteractionContext ctx, string search, AniMediaType type)
        {
            try
            {
                await ctx.DeferAsync();
                var query = type == AniMediaType.ANIME ? AniQuery.AnimeNameQuery : AniQuery.MangaNameQuery;
                var media = await AnilistGraphQL.GetMediaAsync(query, new { search, type = Enum.GetName(typeof(AniMediaType), type), asHtml = true });

                if (media == null)
                {
                    await SendErrorEmbed(ctx, $"Không tìm thấy {(type == AniMediaType.ANIME ? "anime" : "manga")}: {search}");
                    return;
                }

                var embed = CreateMediaEmbed(media, type == AniMediaType.ANIME);
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
            float daysWatched = (float)user.statistics.anime.minutesWatched / 60 / 24;
            string userAbout = ProcessDescription(user.about);

            return new DiscordEmbedBuilder()
                .WithAuthor("AniList Profile", null, ANILIST_LOGO)
                .WithTitle(user.name)
                .WithUrl(user.siteUrl)
                .WithDescription($"**ID: **{user.id}")
                .AddField("**💬 Mô tả**", userAbout)
                .AddField("\u2014", "\u200B")
                .AddField("📊 Anime Stats", $"**Tổng bộ anime đã xem:** {user.statistics.anime.count} bộ \n " +
                                            $"**Điểm trung bình:** {user.statistics.anime.meanScore}/100 \n " +
                                            $"**Số ngày đã xem:** {daysWatched:F2} ngày \n " +
                                            $"**Số tập đã xem:** {user.statistics.anime.episodesWatched} tập", false)
                .AddField("📊 Manga Stats", $"**Số manga đã đọc:** {user.statistics.manga.count} bộ \n " +
                                            $"**Điểm trung bình:** {user.statistics.manga.meanScore}/100 \n " +
                                            $"**Số chapters đã đọc: **{user.statistics.manga.chaptersRead} chapters\n " +
                                            $"**Số volumes đã đoc:** {user.statistics.manga.volumesRead} volumes", false)
                .WithImageUrl(user.bannerImage)
                .WithColor(DiscordColor.Azure)
                .WithFooter($"{ANILIST_URL}")
                .WithThumbnail(user.avatar.medium);
        }

        private DiscordEmbed CreateUserFavoriteEmbed(AniUser user)
        {
            string animeFavorites = string.Join("\n", user.favourites.anime.nodes.Select(a => $"[{a.title.english}]({a.siteUrl})"));
            string mangaFavorites = string.Join("\n", user.favourites.manga.nodes.Select(m => $"[{m.title.english}]({m.siteUrl})"));
            string characterFavorites = string.Join("\n", user.favourites.characters.nodes.Select(c => $"[{c.name.first} {c.name.last}]({c.siteUrl})"));
            string staffFavorites = string.Join("\n", user.favourites.staff.nodes.Select(c => $"[{c.name.first} {c.name.last}]({c.siteUrl})"));
            string studioFavorites = string.Join("\n", user.favourites.studios.nodes.Select(c => $"[{c.name}]({c.siteUrl})"));

            return new DiscordEmbedBuilder()
                .WithAuthor("AniList Favorite", null, ANILIST_LOGO)
                .WithTitle(user.name)
                .AddField(":star: Favorite Anime", string.IsNullOrEmpty(animeFavorites) ? "N/A" : animeFavorites)
                .AddField(":star: Favorite Manga", string.IsNullOrEmpty(mangaFavorites) ? "N/A" : mangaFavorites)
                .AddField(":star: Favorite Characters", string.IsNullOrEmpty(characterFavorites) ? "N/A" : characterFavorites)
                .AddField(":star: Favorite Staffs", string.IsNullOrEmpty(staffFavorites) ? "N/A" : staffFavorites)
                .AddField(":star: Favorite Studios", string.IsNullOrEmpty(studioFavorites) ? "N/A" : studioFavorites)
                .AddField("Xem thêm tại đây", $"[Anilist]({user.siteUrl})")
                .WithColor(DiscordColor.Azure)
                .WithFooter($"{ANILIST_URL}")
                .WithThumbnail(user.avatar.medium);
        }

        private DiscordEmbed CreateMediaEmbed(AniMedia media, bool isAnime)
        {
            string description = ProcessDescription(media.description);
            string startDate = FormatDate(media.startDate);
            string endDate = media.endDate == null ? "N/A" : FormatDate(media.endDate);
            string status = media.status switch
            {
                AniMediaStatus.FINISHED => "Đã hoàn thành",
                AniMediaStatus.RELEASING => "Đang phát sóng",
                AniMediaStatus.CANCELLED => "Đã bị huỷ",
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

            string utcOffsetString = Helper.GetUtcOffsetString();

            if (isAnime)
            {
                if (media.status == AniMediaStatus.FINISHED)
                {
                    embed.AddField(":minidisc: Số tập", media.episodes.ToString(), true)
                         .AddField("⏱ Thời lượng", $"{media.duration} phút", true)
                         .AddField(":hourglass_flowing_sand: Trạng thái", status, true)
                         .AddField(":calendar_spiral: Phát sóng", $"{startDate} -> {endDate}", false)
                         .AddField(":comet: Mùa", char.ToUpper(media.season[0]) + media.season.Substring(1).ToLower(), false)
                         .AddField(":file_folder: Nguồn", char.ToUpper(media.source[0]) + media.source.Substring(1).ToLower(), false)
                         .AddField(":star: Điểm trung bình", $"{media.averageScore}/100", true)
                         .AddField(":star: Điểm trung vị", $"{media.meanScore}/100", true)
                         .AddField(":arrow_right: Thể loại", string.Join(", ", media.genres), false)
                         .AddField("🌐 Tên gốc", media.title.native, false)
                         .AddField("🛈 Thông tin thêm", $"[Anilist]({media.siteUrl})");
                }
                else if (media.status == AniMediaStatus.RELEASING)
                {
                    if (media.airingSchedule?.nodes?.FirstOrDefault() != null)
                    {
                        var nextEpisode = media.airingSchedule.nodes.First();
                        var airingTime = CalculateAiringTime(nextEpisode.timeUntilAiring);

                        embed.AddField(":calendar_spiral: Phát sóng", $"{startDate} -> N/A", true)
                             .AddField(":hourglass_flowing_sand: Trạng thái", status, true)
                             .AddField(":comet: Mùa", char.ToUpper(media.season[0]) + media.season.Substring(1).ToLower(), true)
                             .AddField(":calendar: Tập tiếp theo", $"Tập {nextEpisode.episode}, sẽ được phát sóng sau: {airingTime} ({utcOffsetString})", false)
                             .AddField("⏱ Thời lượng tập", $"{media.duration} phút", false)
                             .AddField(":file_folder: Nguồn", char.ToUpper(media.source[0]) + media.source.Substring(1).ToLower(), false)
                             .AddField(":star: Điểm trung bình", $"{media.averageScore}/100", true)
                             .AddField(":star: Điểm trung vị", $"{media.meanScore}/100", true)
                             .AddField(":arrow_right: Thể loại", string.Join(", ", media.genres), false)
                             .AddField("🌐 Tên gốc", media.title.native, false)
                             .AddField("🛈 Thông tin thêm", $"[Anilist]({media.siteUrl})");
                    }
                    else
                    {
                        embed.AddField(":calendar_spiral: Phát sóng", $"{startDate} -> N/A", true)
                             .AddField(":hourglass_flowing_sand: Trạng thái", status, true)
                             .AddField(":comet: Mùa", char.ToUpper(media.season[0]) + media.season.Substring(1).ToLower(), true)
                             .AddField("⏱ Thời lượng tập", $"{media.duration} phút", false)
                             .AddField(":file_folder: Nguồn", char.ToUpper(media.source[0]) + media.source.Substring(1).ToLower(), false)
                             .AddField(":star: Điểm trung bình", $"{media.averageScore}/100", true)
                             .AddField(":star: Điểm trung vị", $"{media.meanScore}/100", true)
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
                             .AddField(":file_folder: Nguồn", char.ToUpper(media.source[0]) + media.source.Substring(1).ToLower(), false)
                             .AddField(":star: Điểm trung bình", $"{media.averageScore}/100", true)
                             .AddField(":star: Điểm trung vị", $"{media.meanScore}/100", true)
                             .AddField(":arrow_right: Thể loại", string.Join(", ", media.genres), false)
                             .AddField("🌐 Tên gốc", media.title.native, false)
                             .AddField("🛈 Thông tin thêm", $"[Anilist]({media.siteUrl})");
                }
            }
            else
            {
                if (media.status == AniMediaStatus.FINISHED)
                {
                    embed.AddField(":hourglass_flowing_sand: Trạng thái: ", "Đã hoàn thành", true)
                         .AddField(":calendar_spiral: Phát hành", $"{startDate} -> N/A", true)
                         .AddField(":arrow_right: Thể loại", string.Join(", ", media.genres), false)
                         .AddField(":file_folder: Nguồn", "Original", false)
                         .AddField(":star: Điểm trung bình", $"{media.averageScore}/100", true)
                         .AddField(":star: Điểm trung vị", $"{media.meanScore}/100", true)
                         .AddField("🌐 Tên gốc", media.title.native, false)
                         .AddField("🛈 Thông tin thêm", $"[Anilist]({media.siteUrl})");
                }
                else if (media.status == AniMediaStatus.RELEASING)
                {
                    embed.AddField(":hourglass_flowing_sand: Trạng thái", "Đang phát hành", true)
                         .AddField(":calendar_spiral: Phát hành", $"{startDate} -> N/A", true)
                         .AddField(":arrow_right: Thể loại", string.Join(", ", media.genres), false)
                         .AddField(":file_folder: Nguồn", "Original", false)
                         .AddField(":star: Điểm trung bình", $"{media.averageScore}/100", true)
                         .AddField(":star: Điểm trung vị", $"{media.meanScore}/100", true)
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
                .AddField("Giới tính: ", character.gender == "FEMALE" ? "Nữ" : "Nam", true)
                .AddField("Ngày sinh: ", $"{character.dateOfBirth.day}/{character.dateOfBirth.month}",true)
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
                .WithThumbnail(staff.image.medium)
                .WithFooter($"{ANILIST_URL}");
        }

        private string ProcessDescription(string description)
        {
            var doc = new HtmlDocument();
            doc.LoadHtml(description);
            return Helper.ProcessHtmlToMarkdown(doc.DocumentNode);
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