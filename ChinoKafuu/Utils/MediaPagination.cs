using ChinoKafuu.Utils;
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
using CsAnilist.Models.Enums;

namespace ChinoBot.Utils
{
    public class MediaPagination
    {
        private const string ANILIST_LOGO = "https://media.discordapp.net/attachments/1023808975185133638/1143013784584208504/AniList_logo.svg.png?width=588&height=588";
        private const string ANILIST_URL = "https://anilist.co/";
        private readonly CsAniListService _anilistService;
        private readonly AniMedia _media;
        private readonly bool _isAnime;
        private readonly InteractionContext _ctx;
        private DiscordMessage _message = null!;

        public MediaPagination(InteractionContext ctx, AniMedia media, bool isAnime)
        {
            _ctx = ctx;
            _media = media;
            _isAnime = isAnime;
            _anilistService = new CsAniListService();
        }

        public async Task StartAsync()
        {
            var embed = CreateMediaEmbed(_media, _isAnime);
            var message = await _ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(embed));
            _message = message;

            // Thêm emoji reactions
            await _message.CreateReactionAsync(DiscordEmoji.FromName(_ctx.Client, ":memo:"));          // 📝 Thông tin chính
            await _message.CreateReactionAsync(DiscordEmoji.FromName(_ctx.Client, ":busts_in_silhouette:")); // 👥 Characters
            await _message.CreateReactionAsync(DiscordEmoji.FromName(_ctx.Client, ":hammer_and_wrench:")); // 🛠️ Staff
            await _message.CreateReactionAsync(DiscordEmoji.FromName(_ctx.Client, ":office:")); // 🏢 Studio
            await _message.CreateReactionAsync(DiscordEmoji.FromName(_ctx.Client, ":link:")); // 🔗 Related Media
            await _message.CreateReactionAsync(DiscordEmoji.FromName(_ctx.Client, ":x:")); // ❌ Đóng

            // Lắng nghe reaction events
            _ctx.Client.MessageReactionAdded += OnReactionAdded;
            
            // Tự động remove listener sau 5 phút
            await Task.Delay(TimeSpan.FromMinutes(5));
            _ctx.Client.MessageReactionAdded -= OnReactionAdded;
        }

        private async Task OnReactionAdded(DiscordClient sender, DSharpPlus.EventArgs.MessageReactionAddEventArgs e)
        {
            if (e.Message.Id != _message.Id || e.User.IsBot) return;

            await e.Message.DeleteReactionAsync(e.Emoji, e.User);

            switch (e.Emoji.Name)
            {
                case "📝":
                    await ShowMainInfo();
                    break;
                case "👥":
                    await ShowCharacters();
                    break;
                case "🛠️":
                    await ShowStaff();
                    break;
                case "🏢":
                    await ShowStudio();
                    break;
                case "🔗":
                    await ShowRelatedMedia();
                    break;
                case "❌":
                    await CloseMenu();
                    break;
            }
        }

        private async Task ShowMainInfo()
        {
            var embed = CreateMediaEmbed(_media, _isAnime);
            await _message.ModifyAsync(embed: embed);
        }

        private async Task ShowCharacters()
        {
            if (_media.characters?.edges == null || !_media.characters.edges.Any())
            {
                var noCharactersEmbed = new DiscordEmbedBuilder()
                    .WithTitle("Không có thông tin nhân vật")
                    .WithDescription("Không tìm thấy thông tin nhân vật cho bộ này.")
                    .WithColor(DiscordColor.Orange)
                    .Build();
                await _message.ModifyAsync(embed: noCharactersEmbed);
                return;
            }

            var characters = _media.characters.edges.Take(10).ToList();
            var embed = new DiscordEmbedBuilder()
                .WithAuthor("Nhân vật chính", null, ANILIST_LOGO)
                .WithTitle($"{_media.title.english ?? _media.title.romaji}")
                .WithColor(DiscordColor.Blue)
                .WithFooter($"{ANILIST_URL}");

            foreach (var character in characters)
            {
                var name = $"{character.node.name.first} {character.node.name.last}".Trim();
                var role = character.role == CharacterRole.MAIN ? "Nhân vật chính" : "Nhân vật phụ";
                var voiceActors = character.voiceActors?.Take(2).Select(va => $"{va.name.first} {va.name.last}").ToList();
                var vaText = voiceActors?.Any() == true ? $"\n**Seiyuu:** {string.Join(", ", voiceActors)}" : "";
                
                embed.AddField($"{name} ({role})", $"❤️ {character.node.favourites} lượt thích{vaText}", true);
            }

            await _message.ModifyAsync(embed: embed.Build());
        }

        private async Task ShowStaff()
        {
            if (_media.studios?.edges == null || !_media.studios.edges.Any())
            {
                var noStaffEmbed = new DiscordEmbedBuilder()
                    .WithTitle("Không có thông tin staff")
                    .WithDescription("Không tìm thấy thông tin staff cho bộ này.")
                    .WithColor(DiscordColor.Orange)
                    .Build();
                await _message.ModifyAsync(embed: noStaffEmbed);
                return;
            }

            var embed = new DiscordEmbedBuilder()
                .WithAuthor("Thông tin Staff", null, ANILIST_LOGO)
                .WithTitle($"{_media.title.english ?? _media.title.romaji}")
                .WithColor(DiscordColor.Green)
                .WithFooter($"{ANILIST_URL}");

            embed.AddField("Studio thông tin", "Đang tải thông tin chi tiết...", false);

            await _message.ModifyAsync(embed: embed.Build());
        }

        private async Task ShowStudio()
        {
            if (_media.studios?.edges == null || !_media.studios.edges.Any())
            {
                var noStudioEmbed = new DiscordEmbedBuilder()
                    .WithTitle("Không có thông tin studio")
                    .WithDescription("Không tìm thấy thông tin studio cho bộ này.")
                    .WithColor(DiscordColor.Orange)
                    .Build();
                await _message.ModifyAsync(embed: noStudioEmbed);
                return;
            }

            var embed = new DiscordEmbedBuilder()
                .WithAuthor("Studio", null, ANILIST_LOGO)
                .WithTitle($"{_media.title.english ?? _media.title.romaji}")
                .WithColor(DiscordColor.Purple)
                .WithFooter($"{ANILIST_URL}");

            foreach (var studio in _media.studios.edges)
            {
                var studioType = studio.isMain ? "Studio chính" : "Studio phụ";
                var animationStudio = studio.node.isAnimationStudio ? "Studio animation" : "Studio khác";
                embed.AddField($"{studio.node.name} ({studioType})", $"**Loại:** {animationStudio}", true);
            }

            await _message.ModifyAsync(embed: embed.Build());
        }

        private async Task ShowRelatedMedia()
        {
            if (_media.relations?.edges == null || !_media.relations.edges.Any())
            {
                var noRelatedEmbed = new DiscordEmbedBuilder()
                    .WithTitle("Không có media liên quan")
                    .WithDescription("Không tìm thấy media liên quan cho bộ này.")
                    .WithColor(DiscordColor.Orange)
                    .Build();
                await _message.ModifyAsync(embed: noRelatedEmbed);
                return;
            }

            var embed = new DiscordEmbedBuilder()
                .WithAuthor("Media liên quan", null, ANILIST_LOGO)
                .WithTitle($"{_media.title.english ?? _media.title.romaji}")
                .WithColor(DiscordColor.Red)
                .WithFooter($"{ANILIST_URL}");

            foreach (var relation in _media.relations.edges.Take(10))
            {
                var relationType = relation.relationType switch
                {
                    CsAnilist.Models.Enums.MediaRelation.SEQUEL => "Phần tiếp theo",
                    CsAnilist.Models.Enums.MediaRelation.PREQUEL => "Phần trước",
                    CsAnilist.Models.Enums.MediaRelation.ADAPTATION => "Chuyển thể",
                    CsAnilist.Models.Enums.MediaRelation.SIDE_STORY => "Câu chuyện phụ",
                    CsAnilist.Models.Enums.MediaRelation.SPIN_OFF => "Spin-off",
                    _ => "Khác"
                };

                var title = relation.node.title.english ?? relation.node.title.romaji;
                var format = relation.node.format?.ToString() ?? "N/A";
                var status = relation.node.status switch
                {
                    MediaStatus.FINISHED => "Đã hoàn thành",
                    MediaStatus.RELEASING => "Đang phát sóng",
                    MediaStatus.CANCELLED => "Đã huỷ",
                    _ => "Chưa phát sóng"
                };

                embed.AddField($"{title} ({relationType})", $"**Format:** {format}\n**Status:** {status}", true);
            }

            await _message.ModifyAsync(embed: embed.Build());
        }

        private async Task CloseMenu()
        {
            _ctx.Client.MessageReactionAdded -= OnReactionAdded;
            await _message.DeleteAllReactionsAsync();
            
            var closeEmbed = new DiscordEmbedBuilder()
                .WithTitle("Menu đã đóng")
                .WithDescription("Pagination menu đã được đóng.")
                .WithColor(DiscordColor.Gray)
                .Build();
            
            await _message.ModifyAsync(embed: closeEmbed);
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
                .WithFooter($"{ANILIST_URL} • 📝 Info | 👥 Characters | 🛠️ Staff | 🏢 Studio | 🔗 Related | ❌ Close")
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
            return embed.Build();
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
    }
} 