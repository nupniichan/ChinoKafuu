using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using System.Diagnostics;


namespace ChinoBot.CommandsFolder.SlashCommandsFolder
{
    public class AdministratorCommand : ApplicationCommandModule
    {
        [SlashCommand("admin-help","Xem các câu lệnh được hỗ trợ")]
        public async Task AdminHelp(InteractionContext ctx)
        {
            if (!ctx.Member.Permissions.HasPermission(Permissions.Administrator))
            {
                await SendPermissionError(ctx);
                return;
            }
            var embed = new DiscordEmbedBuilder()
                .WithTitle("Danh sách các lệnh được hỗ trợ")
                .WithDescription("Dưới đây là danh sách các lệnh được Chino hỗ trợ hiện tại:")
                .WithColor(DiscordColor.Azure)
                .AddField("/ban", "Ban một người nào đó khỏi server")
                .AddField("/kick", "Đá người đó ra khỏi server")
                .AddField("/mute", "Cấm chat ( tính theo giây )")
                .AddField("/clear", "Xoá tin nhắn trong kênh")
                .AddField("/poll", "Tạo khảo sát ( chỉ có yes hoặc no )")
                .WithFooter("Để sử dụng lệnh cụ thể, nhập /tên-lệnh");

            await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                new DiscordInteractionResponseBuilder().AddEmbed(embed));
        }
        [SlashCommand("ban", "Bạn muốn ban ai đó khỏi server?")]
        public async Task BanCommand(InteractionContext ctx, [Option("user", "Người đó tên là gì?")] DiscordUser user,
                                     [Option("reason", "Tại sao bạn muốn ban người đó?")] string reason = null)
        {
            await ctx.DeferAsync();

            if (!ctx.Member.Permissions.HasPermission(Permissions.Administrator))
            {
                await SendPermissionError(ctx);
                return;
            }

            if (ctx.User.Id == user.Id)
            {
                await SendSelfActionError(ctx, "ban");
                return;
            }

            try
            {
                var member = await ctx.Guild.GetMemberAsync(user.Id);
                await ctx.Guild.BanMemberAsync(member, 0, reason);

                var banEmbed = new DiscordEmbedBuilder()
                    .WithTitle($"{ctx.User.Username} đã ban {member.Username}")
                    .WithDescription($"Lý do: {reason ?? "Không có lý do"}")
                    .WithColor(DiscordColor.Green)
                    .WithTimestamp(DateTime.UtcNow);

                await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(banEmbed));
            }
            catch (Exception ex)
            {
                await SendErrorEmbed(ctx, $"Không thể ban thành viên: {ex.Message}");
            }
        }

        [SlashCommand("kick", "Bạn muốn đá ai đó ra khỏi server?")]
        public async Task KickCommand(InteractionContext ctx, [Option("user", "Người đó tên là gì?")] DiscordUser user,
                                      [Option("reason", "Tại sao bạn muốn kick người đó?")] string reason = null)
        {
            await ctx.DeferAsync();

            if (!ctx.Member.Permissions.HasPermission(Permissions.Administrator))
            {
                await SendPermissionError(ctx);
                return;
            }

            if (ctx.User.Id == user.Id)
            {
                await SendSelfActionError(ctx, "kick");
                return;
            }

            try
            {
                var member = await ctx.Guild.GetMemberAsync(user.Id);
                await member.RemoveAsync();

                var kickEmbed = new DiscordEmbedBuilder()
                    .WithTitle($"{ctx.User.Username} đã kick {member.Username}")
                    .WithDescription($"Lý do: {reason ?? "Không có lý do"}")
                    .WithColor(DiscordColor.Green)
                    .WithTimestamp(DateTime.UtcNow);

                await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(kickEmbed));
            }
            catch (Exception ex)
            {
                await SendErrorEmbed(ctx, $"Không thể kick thành viên: {ex.Message}");
            }
        }

        [SlashCommand("mute", "Bạn muốn cấm chat ai đó?")]
        public async Task MuteCommand(InteractionContext ctx, [Option("user", "Người đó tên là gì?")] DiscordUser user,
                                      [Option("time", "Trong thời gian bao lâu? Tính theo giây")] long time,
                                      [Option("reason", "Tại sao bạn muốn mute người đó?")] string reason = null)
        {
            await ctx.DeferAsync();

            if (!ctx.Member.Permissions.HasPermission(Permissions.Administrator))
            {
                await SendPermissionError(ctx);
                return;
            }

            if (ctx.User.Id == user.Id)
            {
                await SendSelfActionError(ctx, "mute");
                return;
            }

            if (time <= 0 || time > 2419200) // Discord limit 28 days so if you hate them, then ban them instead lol
            {
                await SendErrorEmbed(ctx, "Thời gian không hợp lệ. Vui lòng nhập thời gian từ 1 đến 2419200 giây.");
                return;
            }

            try
            {
                var member = await ctx.Guild.GetMemberAsync(user.Id);
                var timeDuration = DateTime.Now + TimeSpan.FromSeconds(time);
                await member.TimeoutAsync(timeDuration);

                var muteEmbed = new DiscordEmbedBuilder()
                    .WithTitle($"{ctx.User.Username} đã cấm chat {member.Username}")
                    .WithDescription($"Lý do: {reason ?? "Không có lý do"}")
                    .WithColor(DiscordColor.Green)
                    .WithTimestamp(DateTime.UtcNow);

                await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(muteEmbed));
            }
            catch (Exception ex)
            {
                await SendErrorEmbed(ctx, $"Không thể mute thành viên: {ex.Message}");
            }
        }

        [SlashCommand("clear", "Xóa tin nhắn trong kênh.")]
        public async Task PurgeCommand(InteractionContext ctx, [Option("count", "Số lượng tin nhắn cần xóa")] long count)
        {
            await ctx.DeferAsync(true); 

            if (!ctx.Member.Permissions.HasPermission(Permissions.ManageMessages))
            {
                await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                    .WithContent("Bạn không có quyền quản lý tin nhắn."));
                return;
            }

            if (count < 1 || count > 100)
            {
                await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                    .WithContent("Số lượng tin nhắn cần xóa phải từ 1 đến 100."));
                return;
            }

            try
            {
                var messages = await ctx.Channel.GetMessagesAsync((int)count);

                await ctx.Channel.DeleteMessagesAsync(messages);

                await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                    .WithContent($"✅ Đã xóa {count} tin nhắn trong kênh {ctx.Channel.Name}."));
            }
            catch (Exception ex)
            {

                await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                    .WithContent($"❌ Đã xảy ra lỗi: {ex.Message}"));
            }
        }

        [SlashCommand("poll", "Tạo cuộc khảo sát cho người dùng (Admin).")]
        public async Task PollCommand(InteractionContext ctx, [Option("question", "Câu hỏi khảo sát")] string question)
        {
            if (!ctx.Member.Permissions.HasPermission(DSharpPlus.Permissions.Administrator))
            {
                var embed = new DiscordEmbedBuilder()
                    .WithTitle("❌ Lỗi")
                    .WithDescription("Bạn không có quyền sử dụng lệnh này.")
                    .WithColor(DiscordColor.Red);
                await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                    new DiscordInteractionResponseBuilder().AddEmbed(embed));
                return;
            }

            var embedPoll = new DiscordEmbedBuilder()
                .WithTitle($"Khảo sát: {question}")
                .WithDescription("Sử dụng emoji :thumbsup: hoặc :thumbsdown: để trả lời.")
                .WithColor(DiscordColor.Goldenrod);

            var response = new DiscordInteractionResponseBuilder()
                .AddEmbed(embedPoll);

            await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, response);

            var pollMessage = await ctx.GetOriginalResponseAsync();

            await pollMessage.CreateReactionAsync(DiscordEmoji.FromName(ctx.Client, ":thumbsup:"));
            await pollMessage.CreateReactionAsync(DiscordEmoji.FromName(ctx.Client, ":thumbsdown:"));
        }
        private async Task SendPermissionError(InteractionContext ctx)
        {
            var embed = new DiscordEmbedBuilder()
                .WithTitle("Lỗi")
                .WithDescription("Stop, định làm gì à?")
                .WithColor(DiscordColor.Red);

            await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(embed));
        }

        private async Task SendSelfActionError(InteractionContext ctx, string action)
        {
            var embed = new DiscordEmbedBuilder()
                .WithTitle("Lỗi")
                .WithDescription($"Sao bro tại tự làm hại bản thân thế?")
                .WithColor(DiscordColor.Red);

            await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(embed));
        }

        private async Task SendErrorEmbed(InteractionContext ctx, string message)
        {
            var embed = new DiscordEmbedBuilder()
                .WithTitle("Lỗi")
                .WithDescription(message)
                .WithColor(DiscordColor.Red);

            await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(embed));
        }
    }
}
