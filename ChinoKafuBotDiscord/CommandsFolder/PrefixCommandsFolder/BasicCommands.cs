using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using OpenAI_API;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChinoBot.CommandsFolder.PrefixCommandsFolder
{
    public class BasicCommands : BaseCommandModule
    {
        [Command("iuako")]
        public async Task test(CommandContext ctx)
        {
            await ctx.Channel.SendMessageAsync("e iu a <3");
        }
        [Command("say")]
        public async Task TestCommand(CommandContext ctx)
        {
            string test = "a  " + "\n" + "a  " + "\n" + "a  " + "\n" + "a  " + "\n" + "a  " + "\n" + "a  " + "\n" + "a  " + "\n" + "a  " + "\n" + "a  " + "\n" + "a  " + "\n" + "a  " + "\n" ;
            var builder = new DiscordEmbedBuilder
            {
                Title = "Title Here",
                Color = DiscordColor.Azure,
                Description = test
            };
            builder.WithThumbnail(ctx.Client.CurrentUser.AvatarUrl);

            await ctx.Channel.SendMessageAsync(embed: builder.Build());
        }
        [Command("test1")]
        public async Task testa(CommandContext ctx)
        {
            var memberInformation = new DiscordEmbedBuilder()
            {
                Title = ctx.User.Username,
                Description = "hay la minh test di ha",
                Color = DiscordColor.Red
            };

            await ctx.Channel.SendMessageAsync(memberInformation);
        }
    }
}
