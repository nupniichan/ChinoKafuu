using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ChinoBot.config;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using Newtonsoft.Json;
using SauceNET;

namespace ChinoBot.CommandsFolder.SlashCommandsFolder
{
    public class SauceNaoSlashCommand : ApplicationCommandModule
    {
        [SlashCommand("sauce","Bạn có muốn giao dịch không?")]
        public async Task FindSauce(InteractionContext ctx, [Option("image","Cho tớ xem bức ảnh bạn cần nhận sauce đi")] DiscordAttachment image)
        {
            try
            {
                // Connect to API
                await ctx.DeferAsync();
                var jsonReader = new JSONreader();
                await jsonReader.ReadJsonToken();
                string apiKey = jsonReader.saucenaoToken;
                var client = new SauceNETClient(apiKey);
                
                // get image
                var sauce = await client.GetSauceAsync(image.Url);

                // get back result
                string source = sauce.Results[0].SourceURL;
                // check condition
                if (sauce.Results == null || !sauce.Results.Any())
                {
                    var noResultEmbed = new DiscordEmbedBuilder()
                        .WithTitle("Không tìm thấy kết quả")
                        .WithDescription("Không có thông tin sauce nào cho bức ảnh này.")
                        .WithColor(DiscordColor.Red);

                    await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(noResultEmbed));
                    return;
                }

                // create EmbedMessage to show the result
                var embed = new DiscordEmbedBuilder()
                    .WithTitle("Đây là sauce của bạn")
                    .WithAuthor(ctx.User.Username)
                    .WithFooter("Nó có thể đúng hoặc sai do còn nhiều hạn chế")
                    .AddField("Sauce", source)
                    .WithThumbnail(image.Url)
                    .WithImageUrl(sauce.Results[0].ThumbnailURL)
                    .WithColor(DiscordColor.Green);
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(embed));
            }
            // catch unknow error
            catch (Exception ex)
            {
                var errorEmbed = new DiscordEmbedBuilder()
                    .WithTitle("Có lỗi xảy ra")
                    .WithDescription($"Đã xảy ra lỗi: {ex.Message}")
                    .WithColor(DiscordColor.Red);

                await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(errorEmbed));
            }
        }

    }
}
