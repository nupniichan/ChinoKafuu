using DSharpPlus.Entities;
using DSharpPlus;
using DSharpPlus.SlashCommands;

namespace ChinoBot.CommandsFolder.SlashCommandsFolder
{
    internal class ModerationCommands : ApplicationCommandModule
    {
        [SlashCommand("ban", "Bans một người nào đó khỏi server")]
        public async Task Ban(InteractionContext ctx, [Option("user", "Tên người mà bạn muốn ban")] DiscordUser user,
                                                      [Option("reason", "Lý do")] string reason = null)
        {
            await ctx.DeferAsync();

            if (ctx.Member.Permissions.HasPermission(Permissions.Administrator))
            {
                var member = (DiscordMember)user;
                await ctx.Guild.BanMemberAsync(member, 0, reason);

                var banMessage = new DiscordEmbedBuilder()
                {
                    Title = "Banned user " + member.Username,
                    Description = "Lý do: " + reason,
                    Color = DiscordColor.Red
                };

                await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(banMessage));
            }
            else
            {
                var nonAdminMessage = new DiscordEmbedBuilder()
                {
                    Title = "Không thể thực hiện",
                    Description = "Bạn phải là admin mới thực hiện được quyền này",
                    Color = DiscordColor.Red
                };

                await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(nonAdminMessage));
            }
        }

        [SlashCommand("kick", "Kick ai đó khỏi server")]
        public async Task Kick(InteractionContext ctx, [Option("user", "Tên người mà bạn muốn kick")] DiscordUser user)
        {
            await ctx.DeferAsync();

            if (ctx.Member.Permissions.HasPermission(Permissions.Administrator))
            {
                var member = (DiscordMember)user;
                await member.RemoveAsync();

                var kickMessage = new DiscordEmbedBuilder()
                {
                    Title = member.Username + " đã bị kick khỏi server",
                    Description = "Kicked bởi " + ctx.User.Username,
                    Color = DiscordColor.Red
                };

                await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(kickMessage));
            }
            else
            {
                var nonAdminMessage = new DiscordEmbedBuilder()
                {
                    Title = "Không thể thực hiện",
                    Description = "Bạn phải là admin mới thực hiện được quyền này",
                    Color = DiscordColor.Red
                };

                await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(nonAdminMessage));
            }
        }

        [SlashCommand("timeout", "Hạn chế chat")]
        public async Task Timeout(InteractionContext ctx, [Option("user", "Tên người dùng mà bạn muốn")] DiscordUser user,
                                                          [Option("duration", "Trong thời gian bao lâu")] long duration)
        {
            await ctx.DeferAsync();

            if (ctx.Member.Permissions.HasPermission(Permissions.Administrator))
            {
                var timeDuration = DateTime.Now + TimeSpan.FromSeconds(duration);
                var member = (DiscordMember)user;
                await member.TimeoutAsync(timeDuration);

                var timeoutMessage = new DiscordEmbedBuilder()
                {
                    Title = member.Username + "đã bị cấm chat",
                    Description = "Thời gian: " + TimeSpan.FromSeconds(duration).ToString(),
                    Color = DiscordColor.Red
                };

                await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(timeoutMessage));
            }
            else
            {
                var nonAdminMessage = new DiscordEmbedBuilder()
                {
                    Title = "Không thể thực hiện",
                    Description = "Bạn phải là admin mới thực hiện được quyền này",
                    Color = DiscordColor.Red
                };

                await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(nonAdminMessage));
            }
        }
    }
}
