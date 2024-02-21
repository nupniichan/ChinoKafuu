using ChinoBot.config;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using SauceNET;

namespace ChinoBot.CommandsFolder.SlashCommandsFolder
{
    public class SauceNaoSlashCommand : ApplicationCommandModule
    {
        [SlashCommand("sauce","Bạn muốn tìm sauce à? để mình giúp nhé :3")]
        public async Task FindSauce(InteractionContext ctx, [Option("image","Cho mình xem bức ảnh mà bạn cần tìm đi")] DiscordAttachment image)
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

                // get list of the result
                string results = "";
                string name = "";
                List<string> extUrls = new List<string>();
                foreach (var result in sauce.Results)
                {
                    float resultScore = float.Parse(result.Similarity);
                    if (result.ExtUrls != null && result.ExtUrls.Count > 0 && resultScore >= 60.00)
                    {
                        name = sauce.Results[0].Properties[0].Name;
                        extUrls.AddRange(result.ExtUrls);
                    }
                    else
                        continue;
                }
                // check condition
                if (extUrls == null || !extUrls.Any())
                {
                    var noResultEmbed = new DiscordEmbedBuilder()
                        .WithTitle("Không tìm thấy kết quả")
                        .WithDescription("Mình Không tìm thấy thông tin về bức ảnh này")
                        .WithColor(DiscordColor.Red);
                    await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(noResultEmbed));
                    return;
                }
                else
                {
                    foreach (string source in extUrls)
                    {
                        results += source + "\n";
                    }
                }

                // create EmbedMessage to show the result
                var embed = new DiscordEmbedBuilder()
                    .WithTitle("Đây là sauce của bạn")
                    .WithAuthor("Request từ: " + ctx.User.Username)
                    .WithFooter("Nó có thể đúng hoặc sai do nhiều yếu tố khác nhau~")
                    .AddField("Link:", results)
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
