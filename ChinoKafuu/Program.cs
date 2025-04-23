using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.Interactivity.Extensions;
using DSharpPlus.Interactivity;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using ChinoBot.config;
using ChinoBot.CommandsFolder.PrefixCommandsFolder;
using ChinoBot.CommandsFolder.SlashCommandsFolder;
using ChinoBot.CommandsFolder.NonePrefixCommandFolder;
using DSharpPlus.EventArgs;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using DSharpPlus.VoiceNext;
using ChinoKafuu.Utils;
using ChinoKafuu.CommandsFolder.SlashCommandsFolder;

internal sealed class Program
{
    public static DiscordClient Client;
    public static HostApplicationBuilder builder;
    static async Task Main(string[] args)
    {
        builder = new HostApplicationBuilder(args);

        var _config = new Config();
        await _config.ReadConfigFile();

        builder.Services.AddHostedService<ApplicationHost>();
        builder.Services.AddSingleton<DiscordClient>();
        builder.Services.AddSingleton(new DiscordConfiguration()
        {
            Intents = DiscordIntents.All,
            Token = _config.token,
            TokenType = TokenType.Bot,
            AutoReconnect = true
        });

        try
        {
            builder.Build().Run();
        }
        catch (TaskCanceledException)
        {
            Console.WriteLine("Application shutdown gracefully.");
        }
    }
}

file sealed class ApplicationHost : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly DiscordClient _discordClient;
    private static Config config;
    private ChinoConversation _chinoConversation;

    private static Dictionary<string, ulong> voiceChannelIDs = new Dictionary<string, ulong>();

    public ApplicationHost(IServiceProvider serviceProvider, DiscordClient discordClient)
    {
        ArgumentNullException.ThrowIfNull(serviceProvider);
        ArgumentNullException.ThrowIfNull(discordClient);

        _serviceProvider = serviceProvider;
        _discordClient = discordClient;
        _chinoConversation = new ChinoConversation(discordClient);
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
        config = new Config();
        await config.ReadConfigFile();

        var commandsConfig = new CommandsNextConfiguration()
        {
            StringPrefixes = new string[] { config.prefix },
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
        slashCommands.RegisterCommands<AdministratorCommand>();
        slashCommands.RegisterCommands<AnilistSlashCommand>();
        slashCommands.RegisterCommands<OsuSlashCommand>();
        slashCommands.RegisterCommands<UserSlashCommands>();
        slashCommands.RegisterCommands<SteamSlashCommands>();

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

    private static async Task UserLeaveHandler(DiscordClient sender, GuildMemberRemoveEventArgs e)
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

    private static async Task UserJoinHandler(DiscordClient sender, GuildMemberAddEventArgs e)
    {
        var defaultChannel = e.Guild.GetDefaultChannel();
        string welcomeGifUrl = "https://cdn.discordapp.com/attachments/1023808975185133638/1143428547642409080/gochiusa-welcome.gif";
        var welcomeEmbed = new DiscordEmbedBuilder()
            .WithColor(Util.GetRandomDiscordColor())
            .WithTitle($"Ohayo/Konichiwa/Konbawa {e.Member.Username} đã vào quán")
            .WithDescription("Chào mừng bạn đến với CaféDeNup~")
            .WithThumbnail(e.Member.AvatarUrl)
            .WithImageUrl(welcomeGifUrl);
        var defaultRolePair = e.Guild.Roles.FirstOrDefault(r => r.Value.Name == config.userDefaultRoleName);
        var defaultRole = defaultRolePair.Value;
        await e.Member.GrantRoleAsync(defaultRole);
        await defaultChannel.SendMessageAsync(embed: welcomeEmbed);
    }

    private static async Task VoiceChannelHandler(DiscordClient sender, VoiceStateUpdateEventArgs e)
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
        if (user.IsBot)
        {
            return;
        }

        if (e.Before?.Channel != null && e.After?.Channel == null)
        {
            var guildId = e.Guild.Id;
            if (_chinoConversation.TryRemoveConnection(guildId, out var connection))
            {
                await Task.Run(() => connection.Disconnect());
            }
        }
    }

    private async Task Client_Ready(DiscordClient sender, ReadyEventArgs args)
    {
        await _discordClient.UpdateStatusAsync(new DiscordActivity("with Cocoa and Rize~", ActivityType.Playing));
    }
}