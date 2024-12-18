using ChinoBot.config;
using ChinoKafuu.Utils;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity.Extensions;
using DSharpPlus.SlashCommands;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace ChinoKafuu.CommandsFolder.SlashCommandsFolder
{
    public class UserSlashCommands : ApplicationCommandModule
    {
        private readonly JSONreader jsonReader;

        public UserSlashCommands()
        {
            jsonReader = new JSONreader();
            jsonReader.ReadJson().GetAwaiter().GetResult();
        }

        [SlashCommand("user-help", "Xem các câu lệnh được hỗ trợ")]
        public async Task UserHelp(InteractionContext ctx)
        {
            var embed = new DiscordEmbedBuilder()
                .WithTitle("Danh sách các lệnh được Chino hỗ trợ")
                .WithDescription("Dưới đây là danh sách các lệnh được Chino hỗ trợ hiện tại:")
                .WithColor(DiscordColor.Azure)
                .AddField("/server-info", "Hiển thị thông tin máy chủ")
                .AddField("/user-info", "Hiển thị thông tin người dùng")
                .AddField("/ping", "Kiểm tra độ trễ")
                .AddField("/uptime", "Hiển thị thời gian hoạt động của Chino")
                .AddField("/remind", "Tạo nhắc nhở")
                .WithFooter("Để sử dụng lệnh cụ thể, nhập /tên-lệnh");

            await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                new DiscordInteractionResponseBuilder().AddEmbed(embed));
        }
        [SlashCommand("server-info", "Hiển thị thông tin máy chủ.")]
        public async Task ServerInfoCommand(InteractionContext ctx)
        {
            var owner = await ctx.Client.GetUserAsync(ctx.Guild.OwnerId);

            var serverIconUrl = ctx.Guild.IconUrl ?? "https://via.placeholder.com/150";

            var embed = new DiscordEmbedBuilder()
                .WithTitle($":milky_way: Thông tin server: {ctx.Guild.Name}")
                .WithThumbnail(serverIconUrl)
                .AddField(":earth_asia: ID máy chủ", ctx.Guild.Id.ToString(), false)
                .AddField(":star: Chủ sở hữu", $"{owner.Username}#{owner.Discriminator}", false)
                .AddField(":calendar_spiral: Ngày thành lập", ctx.Guild.CreationTimestamp.ToString("dd/MM/yyyy HH:mm:ss"), true)
                .AddField(":busts_in_silhouette: Số thành viên", ctx.Guild.MemberCount.ToString(), true)
                .AddField(":hammer_pick: Tổng vai trò", ctx.Guild.Roles.Count.ToString(), true)
                .AddField("Cấp độ boost", ctx.Guild.PremiumTier.ToString(), true)
                .AddField("Số lượng Boost", ctx.Guild.PremiumSubscriptionCount.ToString() ?? "0", true)
                .WithColor(DiscordColor.Azure)
                .WithFooter("Chino Kafuu")
                .WithImageUrl(ctx.Guild.BannerUrl);

            await ctx.CreateResponseAsync(embed);
        }

        [SlashCommand("user-info", "Hiển thị thông tin người dùng.")]
        public async Task UserInfoCommand(InteractionContext ctx, [Option("user", "Chọn người dùng cần xem")] DiscordUser user)
        {
            var member = await ctx.Guild.GetMemberAsync(user.Id);

            var roles = member.Roles.Any()
                ? string.Join(", ", member.Roles.Select(r => r.Name))
                : "Không có vai trò";

            var embed = new DiscordEmbedBuilder()
                .WithTitle($":bust_in_silhouette: Thông tin người dùng: {user.Username}")
                .AddField("Tên người dùng", user.Username, true)
                .AddField("Nickname", member.Nickname ?? "Không có", true)
                .AddField("ID người dùng", user.Id.ToString(), true)
                .AddField(":calendar_spiral: Ngày tham gia server", member.JoinedAt.ToString("dd/MM/yyyy HH:mm:ss"), true)
                .AddField(":calendar_spiral: Ngày tạo tài khoản", user.CreationTimestamp.ToString("dd/MM/yyyy HH:mm:ss"), true)
                .AddField(":tools: Quyền trong server", roles, false)
                .WithThumbnail(user.AvatarUrl)
                .WithImageUrl(user.BannerUrl)
                .WithColor(user.BannerColor ?? DiscordColor.Azure)
                .WithFooter("Chino Kafuu");

            if (!string.IsNullOrWhiteSpace(user.BannerUrl))
            {
                embed.WithImageUrl(user.BannerUrl);
            }

            await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                new DiscordInteractionResponseBuilder().AddEmbed(embed));
        }

        [SlashCommand("ping", "Kiểm tra độ trễ.")]
        public async Task PingCommand(InteractionContext ctx)
        {
            var start = DateTime.Now;
            var embed = new DiscordEmbedBuilder()
                .WithTitle("Pong!")
                .WithDescription($"Độ trễ: {(DateTime.Now - start).TotalMilliseconds}ms")
                .WithColor(DiscordColor.Green)
                .WithFooter("Chino Kafuu");

            await ctx.CreateResponseAsync(embed);
        }

        [SlashCommand("uptime", "Hiển thị thời gian hoạt động của Chino.")]
        public async Task UptimeCommand(InteractionContext ctx)
        {
            var uptime = DateTime.Now - Process.GetCurrentProcess().StartTime;
            var embed = new DiscordEmbedBuilder()
                .WithTitle("Thời gian hoạt động của Chino")
                .WithDescription($"Chino đã hoạt động được {uptime.Days} ngày, {uptime.Hours} giờ, {uptime.Minutes} phút.")
                .WithColor(DiscordColor.Purple)
                .WithFooter("Chino Kafuu");

            await ctx.CreateResponseAsync(embed);
        }

        [SlashCommand("remind", "Đặt nhắc nhở cho một khoảng thời gian.")]
        public async Task RemindCommand(
            InteractionContext ctx,
            [Option("time", "Thời gian nhắc nhở (phút)")] long time,
            [Option("message", "Lời nhắc nhở")] string message)
        {
            if (time <= 0 || string.IsNullOrWhiteSpace(message))
            {
                var errorEmbed = new DiscordEmbedBuilder()
                    .WithTitle("❌ Lỗi")
                    .WithDescription("Thời gian phải lớn hơn 0 và tin nhắn không được để trống.")
                    .WithColor(DiscordColor.Red);

                await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                    new DiscordInteractionResponseBuilder().AddEmbed(errorEmbed));
                return;
            }

            var reminderTime = DateTime.Now.AddMinutes(time);

            var confirmEmbed = new DiscordEmbedBuilder()
                .WithTitle("✅ Nhắc nhở")
                .WithDescription($"Đã đặt nhắc nhở sau {time} phút.")
                .AddField("Tin nhắn", message)
                .WithColor(DiscordColor.Green)
                .WithFooter("Chino Kafuu");

            await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                new DiscordInteractionResponseBuilder().AddEmbed(confirmEmbed));

            await Task.Delay((int)(time * 60 * 1000));

            var textChannel = ctx.Channel;

            await textChannel.SendMessageAsync($"{ctx.User.Mention} - Nhắc nhở: {message}");
        }
        [SlashCommand("hoizui", "mình hỏi nhau một câu đố vui há :3")]
        public async Task YesNoQuestion(InteractionContext ctx, [Option("option1", "Lựa chọn 1")] string option1,
                                                      [Option("option2", "Lựa chọn 2")] string option2,
                                                      [Option("timeLimit", "Thời gian cho câu hỏi")] long timeLimit,
                                                      [Option("Question", "Câu hỏi bạn muốn hỏi")] string question)
        {
            await ctx.DeferAsync();
            var interactivity = ctx.Client.GetInteractivity();
            TimeSpan timer = TimeSpan.FromSeconds(timeLimit);

            DiscordEmoji[] optionEmojis =
            {
                DiscordEmoji.FromName(ctx.Client, ":white_check_mark:" , false),
                DiscordEmoji.FromName(ctx.Client, ":regional_indicator_x:", false)
            };

            string optionString = optionEmojis[0] + " | " + option1 + "\n" +
                                  optionEmojis[1] + " | " + option2 + "\n";
            var messageEmbed = new DiscordMessageBuilder()
                .AddEmbed(new DiscordEmbedBuilder()
                .WithTitle(string.Join(" ", question))
                .WithDescription(optionString)
                .WithColor(DiscordColor.White));

            var putReactionOn = await ctx.Channel.SendMessageAsync(messageEmbed);

            foreach (var emoji in optionEmojis)
            {
                await putReactionOn.CreateReactionAsync(emoji);
            }

            var result = await interactivity.CollectReactionsAsync(putReactionOn, timer);

            int count1 = 0;
            int count2 = 0;
            foreach (var emoji in result)
            {
                if (emoji.Emoji == optionEmojis[0])
                {
                    count1++;
                }
                else if (emoji.Emoji == optionEmojis[1])
                {
                    count2++;
                }
            }
            int totalVotes = count1 + count2;

            string resultString = "Đã có " + totalVotes + " " + "người vote với: " + "\n" +
                                  optionEmojis[0] + ":" + " " + count1 + " " + "votes" + "\n" +
                                  optionEmojis[1] + ":" + " " + count2 + " " + "votes" + "\n";

            await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(new DiscordEmbedBuilder()
                                                                            .WithTitle("Kết quả vote")
                                                                            .WithDescription(resultString)
                                                                            .WithColor(DiscordColor.Green)));
        }
        [SlashCommand("weather", "Xem thời tiết hiện tại")]
        public async Task WeatherCommand(InteractionContext ctx, [Option("location", "Thành phố bạn cần xem. Ví dụ: Ho Chi Minh City")] string location)
        {
            await ctx.DeferAsync();
            try
            {
                WeatherService weatherService = new WeatherService(jsonReader.openWeatherApi);
                var weatherData = await weatherService.GetWeatherDataAsync(location);

                if (weatherData != null)
                {
                    string iconUrl = weatherService.GetWeatherIconUrl(weatherData.weather.icon);

                    string windDirection = Util.GetWindDirection(weatherData.wind.deg);

                    string cloudDescription = Util.GetCloudDescription(weatherData.clouds.all);

                    var embed = new DiscordEmbedBuilder()
                        .WithTitle($"Thời tiết tại {location}")
                        .WithDescription($"🕒 Cập nhật: {weatherData.Date} {weatherData.Hour:D2}:{weatherData.Minutes:D2}")
                        .WithColor(Util.GetEmbedColor(weatherData.main.temp))
                        .WithThumbnail(iconUrl)
                        .AddField("🌡️ Nhiệt độ",
                            $"Hiện tại: **{weatherData.main.temp:F1}°C**\n" +
                            $"Cảm giác như: **{weatherData.main.feels_like:F1}°C**", true)
                        .AddField("💨 Gió",
                            $"Tốc độ: **{weatherData.wind.speed} m/s**\n" +
                            $"Hướng: **{windDirection} ({weatherData.wind.deg}°)**\n" +
                            $"Giật gió: **{weatherData.wind.gust} m/s**", true)
                        .AddField("💧 Độ ẩm",
                            $"**{weatherData.main.humidity}%**", true)
                        .AddField("☁️ Mây",
                            $"{cloudDescription} (**{weatherData.clouds.all}%**)", true)
                        .AddField("📊 Áp suất",
                            $"Mặt đất: **{weatherData.main.grnd_level} hPa**\n" +
                            $"Mực biển: **{weatherData.main.sea_level} hPa**", true)
                        .AddField("👀 Tầm nhìn",
                            $"**{weatherData.Visibility / 1000} km**", true)
                        .WithFooter(
                            $"Tọa độ: Kinh độ: [{weatherData.coord.Longitude}], Vĩ độ: [{weatherData.coord.Latitude}]");

                    await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(embed));
                }
                else
                {
                    var nullEmbed = new DiscordEmbedBuilder()
                        .WithTitle("❌ Không tìm thấy thông tin")
                        .WithDescription($"Api bị lỗi òi")
                        .WithColor(DiscordColor.Red);

                    await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(nullEmbed));
                }
            }
            catch (Exception e)
            {
                var errorEmbed = new DiscordEmbedBuilder()
                    .WithTitle("❌ Lỗi")
                    .WithDescription($"Đã có lỗi xảy ra: {e.Message}")
                    .WithColor(DiscordColor.Red);

                await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(errorEmbed));
            }
        }
    }
}
