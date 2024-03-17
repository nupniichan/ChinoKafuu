using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.Interactivity.Extensions;
using DSharpPlus.Interactivity;
using DSharpPlus.CommandsNext.Exceptions;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using ChinoBot.config;
using ChinoBot.CommandsFolder.PrefixCommandsFolder;
using ChinoBot.CommandsFolder.SlashCommandsFolder;
using Microsoft.Extensions.Logging;
using AnimeListBot.Handler;
using ChinoBot;
using static System.Net.WebRequestMethods;
using System.Net.Sockets;
using DSharpPlus.EventArgs;
using Python.Runtime;
using static System.Runtime.InteropServices.JavaScript.JSType;
using ChinoBot.CommandsFolder.NonePrefixCommandFolder;

internal class Program
{
    public static DiscordClient Client;
    private static CommandsNextExtension Commands;
    private static Dictionary<string, ulong> voiceChannelIDs = new Dictionary<string, ulong>();
    static async Task Main(string[] args)
    {
        var jsonReader = new JSONreader();
        await jsonReader.ReadJson();

        var discordConfig = new DiscordConfiguration()
        {
            Intents = DiscordIntents.All,
            Token = jsonReader.token,
            TokenType = TokenType.Bot,
            AutoReconnect = true
        };

        Client = new DiscordClient(discordConfig);

        Client.UseInteractivity(new InteractivityConfiguration()
        {
            Timeout = TimeSpan.FromMinutes(2)
        });
        Client.Ready += Client_Ready;
        Client.VoiceStateUpdated += VoiceChannelHandler;
        Client.GuildMemberAdded += UserJoinHandler;
        Client.GuildMemberRemoved += UserLeaveHandler;
        var autoMessageHandler = new ChinoConservationChat(Client);
        var commandsConfig = new CommandsNextConfiguration()
        {
            StringPrefixes = new string[] { jsonReader.prefix },
            EnableMentionPrefix = true,
            EnableDms = true,
            EnableDefaultHelp = false
        };
        Commands = Client.UseCommandsNext(commandsConfig);
        Commands.RegisterCommands<BasicCommands>();
        var slashCommands = Client.UseSlashCommands();
        slashCommands.RegisterCommands<BasicSlashCommands>();
        slashCommands.RegisterCommands<AdministratorCommand>();
        slashCommands.RegisterCommands<AnilistSlashCommand>();
        slashCommands.RegisterCommands<OsuSlashCommand>();
        await Client.ConnectAsync();
        await Task.Delay(-1);
    }

    private static async Task UserLeaveHandler(DiscordClient sender, DSharpPlus.EventArgs.GuildMemberRemoveEventArgs e)
    {
        var defaultChannel = e.Guild.GetDefaultChannel();
        string goodByeImage = "https://cdn.discordapp.com/attachments/1023808975185133638/1143429728901021747/Is-the-Order-a-Rabbit-BLOOM-Season-2-1.png";
        var goodByeEmbed = new DiscordEmbedBuilder()
            .WithColor(DiscordColor.Red)
            .WithTitle($"Sayonara {e.Member.Username}")
            .WithDescription("Cảm ơn bạn đã đến quán của mình~" +"\n" +"Nếu có dịp mong bạn hãy ghé quán tiếp nhé!")
            .WithThumbnail(e.Member.AvatarUrl)
            .WithImageUrl(goodByeImage);
        await defaultChannel.SendMessageAsync(embed: goodByeEmbed);
    }

    private static async Task UserJoinHandler(DiscordClient sender, DSharpPlus.EventArgs.GuildMemberAddEventArgs e)
    {
        var defaultChannel = e.Guild.GetDefaultChannel();
        string welcomeGifUrl = "https://cdn.discordapp.com/attachments/1023808975185133638/1143428547642409080/gochiusa-welcome.gif";
        var welcomeEmbed = new DiscordEmbedBuilder()
            .WithColor(Helper.GetRandomDiscordColor())
            .WithTitle($"Ohayo/Konichiwa/Konbawa {e.Member.Username} đã vào quán")
            .WithDescription("Mong bạn sẽ có trải nghiệm vui vẻ ở quán của mình~")
            .WithThumbnail(e.Member.AvatarUrl)
            .WithImageUrl(welcomeGifUrl);
        await defaultChannel.SendMessageAsync(embed: welcomeEmbed);
    }

    private static async Task VoiceChannelHandler(DiscordClient sender, DSharpPlus.EventArgs.VoiceStateUpdateEventArgs e)
    {
            // Joining
            if (e.Channel != null && e.Channel.Name == "Tạo Phòng" && e.Before == null)
            {
                var userVC = await e.Guild.CreateVoiceChannelAsync($"🎙 {e.User.Username}'s Voice Channel", e.Channel.Parent);

                voiceChannelIDs.Add(e.User.Username, userVC.Id);

                var member = await e.Guild.GetMemberAsync(e.User.Id);

                await member.ModifyAsync(x => x.VoiceChannel = userVC);
            }
            // Leaving
            if (e.Channel == null && e.Before != null && e.Before.Channel != null && e.Before.Channel.Name == $"🎙 {e.User.Username}'s Voice Channel")
            {
                var channelSearch = voiceChannelIDs.TryGetValue(e.User.Username, out ulong channelID);
                var channelToDelete = e.Guild.GetChannel(channelID);
                await channelToDelete.DeleteAsync();

                voiceChannelIDs.Remove(e.User.Username);
            }
    }
    private static async Task Client_Ready(DiscordClient sender, DSharpPlus.EventArgs.ReadyEventArgs args)
    {
        await Client.UpdateStatusAsync(new DiscordActivity("with Cocoa and Rize~", ActivityType.Playing));;
    }
}