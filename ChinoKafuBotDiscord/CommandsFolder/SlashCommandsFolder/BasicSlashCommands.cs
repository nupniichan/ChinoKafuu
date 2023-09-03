using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using DSharpPlus.Interactivity.Extensions;
using DSharpPlus.SlashCommands;
using GraphQL.Types;
using OpenAI_API;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace ChinoBot.CommandsFolder.SlashCommandsFolder
{
    public class BasicSlashCommands : ApplicationCommandModule
    {
        [SlashCommand("hoizui","mình hỏi nhau một câu đố vui há :3")]
        public async Task YesNoQuestion(InteractionContext ctx, [Option("option1", "Lựa chọn 1")] string option1,
                                                              [Option("option2", "Lựa chọn 2")] string option2,
                                                              [Option("timeLimit","Thời gian cho câu hỏi")] long timeLimit,
                                                              [Option("Question","Câu hỏi bạn muốn hỏi")] string question)
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
            
            foreach(var emoji in optionEmojis)
            {
                await putReactionOn.CreateReactionAsync(emoji);
            }

            var result = await interactivity.CollectReactionsAsync(putReactionOn, timer);

            int count1 = 0;
            int count2 = 0;
            foreach(var emoji in result)
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

        [SlashCommand("caption","Cho bức ảnh của bạn một cái caption thú vị nè~")]
        public async Task CaptionCommand(InteractionContext ctx, [Option("image","Tải lên hình ảnh của bạn")] DiscordAttachment img,
                                                                 [Option("caption","Caption mà bạn muốn thêm")] string caption)
        {
            await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder()
                                                                    .WithContent("Đây là lệnh mà bạn yêu cầu nè~"));

            var captionMessage = new DiscordMessageBuilder().
                AddEmbed(new DiscordEmbedBuilder()
                .WithColor(DiscordColor.Green)
                .WithFooter(caption)
                .WithImageUrl(img.Url)
                );
            await ctx.Channel.SendMessageAsync(captionMessage);
        }
        [SlashCommand("userInfo","Kiểm tra thông tin của ai đó")]
        public async Task InforCheckCommand(InteractionContext ctx, [Option("user","Người mà bạn muốn kiểm tra tên gì nè~")] DiscordUser user)
        {
            await ctx.DeferAsync();
            var member = (DiscordMember)user;
            try
            {
                string roles = string.Join(", ", member.Roles.Select(role => role.Mention));
                string stringInformation = $"**ID**: {member.Id}" + "\n" +
                                           $"**Trạng thái: **{member.Presence.Status}" + "\n" +
                                           $"**Quyền hạn trong server:** {roles}";
                var memberInformation = new DiscordEmbedBuilder()
                    .WithTitle(member.Username)
                    .WithThumbnail(member.AvatarUrl)
                    .WithDescription(stringInformation)
                    .WithColor(member.Color)
                    .WithImageUrl(member.BannerUrl)
                    .AddField("Ngày tham gia discord", member.CreationTimestamp.ToString("dd/MM/yyyy"), true)
                    .AddField("Ngày tham gia server này", member.Guild.JoinedAt.ToString("dd/MM/yyyy"), true);
                memberInformation.WithFooter($"Được tra bởi {ctx.User.Username} | {DateTime.Now}");
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(memberInformation));
            }
            catch (Exception e)
            {
                var errorEmbed = new DiscordEmbedBuilder()
                    .WithTitle("Phát hiện lỗi~")
                    .WithColor(DiscordColor.Red)
                    .WithDescription(e.Message);
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(errorEmbed));
            }
        }

    }
}
