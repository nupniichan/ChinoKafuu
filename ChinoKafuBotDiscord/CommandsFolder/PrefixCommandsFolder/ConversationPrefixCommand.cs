using CharacterAI;
using ChinoBot.config;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChinoBot.CommandsFolder.PrefixCommandsFolder
{
    public class ConversationPrefixCommand : BaseCommandModule
    {
        [Command("c")]
        [Description("Have a conversation with me (English Only)")]
        public async Task ConservationCommand(CommandContext ctx, [Description("what will we say?")] params string[] chat)
        {
            try
            {
                var jsonReader = new JSONreader();
                await jsonReader.ReadJsonToken();
                string token = jsonReader.cAIToken;
                var client = new CharacterAIClient(token);

                // Launch Puppeteer headless browser
                await client.LaunchBrowserAsync(killDuplicates: true);

                // Highly recommend to do this
                AppDomain.CurrentDomain.ProcessExit += (s, args) => client.KillBrowser();

                // Send message to a character
                string characterId = "zRG-rUxaNf7gfOlGSCuSF6csdGqMBrD2Sfu5wmWlXDg";
                var character = await client.GetInfoAsync(characterId);
                var historyId = "7h2Ua061aBQq0RU1hQsSDgYSjQGfRzo0LLPBHi0g_og";
                Console.WriteLine(historyId);

                var characterResponse = await client.CallCharacterAsync(
                    characterId: character.Id,
                    characterTgt: character.Tgt,
                    historyId: historyId,
                    message: string.Join(' ', chat)
                );

                if (!characterResponse.IsSuccessful)
                {
                    var errorEmbed = new DiscordEmbedBuilder()
                        .WithTitle("Error")
                        .WithColor(DiscordColor.Red)
                        .WithDescription(characterResponse.ErrorReason);
                    await ctx.RespondAsync(embed: errorEmbed);
                }

                await ctx.RespondAsync(characterResponse.Response.Text);
            }
            catch (Exception e)
            {
                var errorEmbed = new DiscordEmbedBuilder()
                    .WithTitle("Error")
                    .WithColor(DiscordColor.Red)
                    .WithDescription(e.Message);
                await ctx.RespondAsync(embed: errorEmbed);
            }
        }
    }
}
