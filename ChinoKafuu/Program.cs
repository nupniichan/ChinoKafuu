using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.Interactivity.Extensions;
using DSharpPlus.Interactivity;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using ChinoBot.config;
using ChinoBot.CommandsFolder.PrefixCommandsFolder;
using ChinoBot.CommandsFolder.SlashCommandsFolder;
using ChinoBot;
using ChinoBot.CommandsFolder.NonePrefixCommandFolder;
using DSharpPlus.EventArgs;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Lavalink4NET.Extensions;
using System.Data;
using DSharpPlus.VoiceNext;

internal sealed class Program
{
    public static DiscordClient Client;
    public static HostApplicationBuilder builder;
    static async Task Main(string[] args)
    {
        builder = new HostApplicationBuilder(args);

        var jsonReader = new JSONreader();
        await jsonReader.ReadJson();

        builder.Services.AddHostedService<ApplicationHost>();
        builder.Services.AddSingleton<DiscordClient>();
        builder.Services.AddSingleton(new DiscordConfiguration()
        {
            Intents = DiscordIntents.All,
            Token = jsonReader.token,
            TokenType = TokenType.Bot,
            AutoReconnect = true
        });

        if (!Helper.SkipLavalink)
        {
            builder.Services.AddLavalink();
        }
        else
        {
            Console.WriteLine("Skipping Lavalink initialization in CI environment");
        }
        builder.Build().Run();
    }
}
file sealed class ApplicationHost : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly DiscordClient _discordClient;
    private static JSONreader jsonReader;

    private static Dictionary<string, ulong> voiceChannelIDs = new Dictionary<string, ulong>();

    public ApplicationHost(IServiceProvider serviceProvider, DiscordClient discordClient)
    {
        ArgumentNullException.ThrowIfNull(serviceProvider);
        ArgumentNullException.ThrowIfNull(discordClient);

        _serviceProvider = serviceProvider;
        _discordClient = discordClient;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Đăng ký event
        _discordClient
            .UseInteractivity(new InteractivityConfiguration
            {
                Timeout = TimeSpan.FromMinutes(2)
            });
        _discordClient.Ready += Client_Ready;
        _discordClient.VoiceStateUpdated += VoiceChannelHandler;
        _discordClient.GuildMemberAdded += UserJoinHandler;
        _discordClient.GuildMemberRemoved += UserLeaveHandler;
        _discordClient.VoiceStateUpdated += VoiceStateHandler;

        _discordClient.UseVoiceNext();
        // Kiểm tra voice có còn ai không, nếu không thì out
        _discordClient.VoiceStateUpdated += async (client, args) =>
        {
            var guild = args.Guild;
            var botMember = await guild.GetMemberAsync(_discordClient.CurrentUser.Id).ConfigureAwait(false);

            if (botMember.VoiceState?.Channel != null)
            {
                if (botMember.VoiceState.Channel.Users.Count == 1)
                {
                    await botMember.ModifyAsync(properties => properties.VoiceChannel = null).ConfigureAwait(false);
                }
            }
        };

        // Đọc dữ liệu file config
        jsonReader = new JSONreader();
        await jsonReader.ReadJson();

        var autoMessageHandler = new ChinoConversation(_discordClient);
        var commandsConfig = new CommandsNextConfiguration()
        {
            StringPrefixes = new string[] { jsonReader.prefix },
            EnableMentionPrefix = true,
            EnableDms = true,
            EnableDefaultHelp = false
        };
        // Đăng ký sử dụng lệnh prefix
        var commands = _discordClient.UseCommandsNext(commandsConfig);
        commands.RegisterCommands<BasicCommands>();

        // Đăng ký sử dụng lệnh slash ( / )
        var slashCommands = _discordClient.UseSlashCommands(new SlashCommandsConfiguration { Services = _serviceProvider });
        await slashCommands.RefreshCommands();
        slashCommands.RegisterCommands<BasicSlashCommands>();
        slashCommands.RegisterCommands<AdministratorCommand>();
        slashCommands.RegisterCommands<AnilistSlashCommand>();
        slashCommands.RegisterCommands<OsuSlashCommand>();

        if (!Helper.SkipLavalink)
        {
            slashCommands.RegisterCommands<MusicCommands>();
        }

        await _discordClient.ConnectAsync().ConfigureAwait(false);

        var readyTaskCompletionSource = new TaskCompletionSource();
        Task SetResult(DiscordClient client, ReadyEventArgs eventArgs)
        {
            readyTaskCompletionSource.TrySetResult();
            return Task.CompletedTask;
        }
        _discordClient.Ready += SetResult;
        await readyTaskCompletionSource.Task.ConfigureAwait(false);
        _discordClient.Ready -= SetResult;

        await Task.Delay(Timeout.InfiniteTimeSpan, stoppingToken).ConfigureAwait(false);
    }
    private static async Task UserLeaveHandler(DiscordClient sender, DSharpPlus.EventArgs.GuildMemberRemoveEventArgs e)
    {
        var defaultChannel = e.Guild.GetDefaultChannel();
        string goodByeImage = "https://cdn.discordapp.com/attachments/1023808975185133638/1143429728901021747/Is-the-Order-a-Rabbit-BLOOM-Season-2-1.png";
        var goodByeEmbed = new DiscordEmbedBuilder()
            .WithColor(DiscordColor.Red)
            .WithTitle($"Sayonara {e.Member.Username}")
            .WithDescription("Cảm ơn bạn đã đến quán của mình~" + "\n" + "Nếu có dịp mong bạn hãy ghé quán tiếp nhé!")
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
        var defaultRolePair = e.Guild.Roles.FirstOrDefault(r => r.Value.Name == jsonReader.userDefaultRoleName);
        var defaultRole = defaultRolePair.Value;
        await e.Member.GrantRoleAsync(defaultRole);
        await defaultChannel.SendMessageAsync(embed: welcomeEmbed);
    }

    private static async Task VoiceChannelHandler(DiscordClient sender, DSharpPlus.EventArgs.VoiceStateUpdateEventArgs e)
    {
        if (e.Channel != null && e.Channel.Name == "Tạo Phòng" && e.Before == null)
        {
            var userVC = await e.Guild.CreateVoiceChannelAsync($"Voice chat của {e.User.Username}", e.Channel.Parent);
            voiceChannelIDs.Add(e.User.Username, userVC.Id);
            var member = await e.Guild.GetMemberAsync(e.User.Id);
            await member.ModifyAsync(x => x.VoiceChannel = userVC);
        }
        if (e.Channel == null && e.Before != null && e.Before.Channel != null)
        {
            var channelToDelete = e.Before.Channel;
            if (voiceChannelIDs.ContainsValue(channelToDelete.Id) && channelToDelete.Name.StartsWith("Voice chat của "))
            {
                if (channelToDelete.Users.Count == 0)
                {
                    await channelToDelete.DeleteAsync();
                    var key = voiceChannelIDs.FirstOrDefault(x => x.Value == channelToDelete.Id).Key;
                    if (key != null)
                    {
                        voiceChannelIDs.Remove(key);
                    }
                }
            }
        }
    }

    private async Task VoiceStateHandler(DiscordClient sender, VoiceStateUpdateEventArgs e)
    {
        var user = e.User;
        bool isBot = user.IsBot;

        if (isBot)
        {
            return;
        }
        if (e.After != null)
        {
            if (e.Before?.Channel != null && e.After.Channel == null)
            {
                if (ChinoConversation.Connection != null)
                {
                    ChinoConversation.Connection = null;
                }
            }
        }
    }
    private async Task Client_Ready(DiscordClient sender, DSharpPlus.EventArgs.ReadyEventArgs args)
    {
        await _discordClient.UpdateStatusAsync(new DiscordActivity("with Cocoa and Rize~", ActivityType.Playing));
    }
}