using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChinoBot.CommandsFolder.SlashCommandsFolder
{
    public class AdministratorCommand : ApplicationCommandModule
    {
        [SlashCommand("ban", "Bạn muốn ban ai đó khỏi server?")]
        public async Task BanCommand(InteractionContext ctx, [Option("user","Người đó tên là gì?")] DiscordUser user,
                                                             [Option("reason", "Tại sao bạn muốn ban người đó?")] string reason = null)
        {
            await ctx.DeferAsync();
            if (ctx.Member.Permissions.HasPermission(Permissions.Administrator))
            {
                var member = (DiscordMember)user;
                await ctx.Guild.BanMemberAsync(member, 0, reason);
                var banMessage = new DiscordEmbedBuilder()
                    .WithTitle(ctx.User.Username + " " +"đã ban" +" "+ member.Username)
                    .WithDescription("Lý do: " + reason)
                    .WithColor(DiscordColor.Green)
                    .WithFooter("Thời gian thực thi lệnh: " + DateTime.Now);

                await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(banMessage));
            }
            else
            {
                var failMessage = new DiscordEmbedBuilder()
                    .WithTitle("Lỗi")
                    .WithDescription("Bạn không thể sử dụng lệnh này~")
                    .WithColor(DiscordColor.Red);
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(failMessage));
            }
        }
        [SlashCommand("kick", "Bạn muốn đá ai đó ra khỏi server?")]
        public async Task KickCommand(InteractionContext ctx, [Option("user", "Người đó tên là gì?")] DiscordUser user,
                                                             [Option("reason", "Tại sao bạn muốn kick người đó?")] string reason = null)
        {
            await ctx.DeferAsync();
            if (ctx.Member.Permissions.HasPermission(Permissions.Administrator))
            {
                var member = (DiscordMember)user;
                await member.RemoveAsync();
                var kickEmbedMessage = new DiscordEmbedBuilder()
                    .WithTitle(ctx.User.Username + " " + "đã kick" + " " + member.Username)
                    .WithDescription("Lý do: " + reason)
                    .WithColor(DiscordColor.Green)
                    .WithFooter("Thời gian thực thi lệnh: " + DateTime.Now);

                await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(kickEmbedMessage));
            }
            else
            {
                var failMessage = new DiscordEmbedBuilder()
                    .WithTitle("Lỗi")
                    .WithDescription("Bạn không thể sử dụng lệnh này~")
                    .WithColor(DiscordColor.Red);
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(failMessage));
            }
        }
        [SlashCommand("mute", "Bạn muốn cấm chat ai đó?")]
        public async Task MuteCommand(InteractionContext ctx, [Option("user", "Người đó tên là gì?")] DiscordUser user,
                                                     [Option("time", "Trong thời gian bao lâu? Tính theo giây")] long time,
                                                     [Option("reason", "Tại sao bạn muốn kick người đó?")] string reason = null)
        {
            await ctx.DeferAsync();
            if (ctx.Member.Permissions.HasPermission(Permissions.Administrator))
            {
                var member = (DiscordMember)user;
                var timeDuration = DateTime.Now + TimeSpan.FromSeconds(time);
                await member.TimeoutAsync(timeDuration);
                var muteEmbedMessage = new DiscordEmbedBuilder()
                    .WithTitle(ctx.User.Username + " " + "đã cấm chat" + " " + member.Username)
                    .WithColor(DiscordColor.Green)
                    .WithDescription("Lý do: " + reason)
                    .WithFooter("Thời gian thực thi lệnh: " + DateTime.Now);
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(muteEmbedMessage));
            }
            else
            {
                var failMessage = new DiscordEmbedBuilder()
                    .WithTitle("Lỗi")
                    .WithDescription("Bạn không thể sử dụng lệnh này~")
                    .WithColor(DiscordColor.Red);
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(failMessage));
            }
        }
    }
}
