using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using Lavalink4NET;
using Lavalink4NET.Players.Queued;
using Lavalink4NET.Players;
using Lavalink4NET.Rest.Entities.Tracks;
using Microsoft.Extensions.Options;
using DSharpPlus;

namespace ChinoBot.CommandsFolder.SlashCommandsFolder
{
    public class MusicCommands : ApplicationCommandModule
    {
        private readonly IAudioService _audioService;

        public MusicCommands(IAudioService audioService)
        {
            ArgumentNullException.ThrowIfNull(audioService);

            _audioService = audioService;
        }

        [SlashCommand("musicHelp", "Hiển thị trợ giúp về các lệnh điều khiển nhạc")]
        public async Task AniHelpCommand(InteractionContext ctx)
        {
            var embed = new DiscordEmbedBuilder()
                .WithTitle("Danh sách các lệnh điều khiển nhạc")
                .WithDescription("Dưới đây là danh sách các lệnh điều khiển nhạc có sẵn:")
                .WithColor(DiscordColor.Azure)
                .AddField("/play", "Phát một bài nhạc mà bạn cung cấp url hoặc tên bài nhạc đó")
                .AddField("/pause", "Tạm thời dừng bài nhạc đang phát")
                .AddField("/resume", "Tiếp tục phát bài nhạc đó")
                .AddField("/skip", "Bỏ qua bài nhạc hiện tại và phát bài nhạc kế tiếp (nếu có)")
                .AddField("/leave", "Chino sẽ ngừng phát nhạc và rời khỏi voice chat")
                .AddField("/stop", "Dừng toàn bộ bài nhạc kể cả trong danh sách chờ")
                .WithFooter("Để sử dụng lệnh cụ thể, nhập /tên-lệnh");

            await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                new DiscordInteractionResponseBuilder().AddEmbed(embed));
        }

        [SlashCommand("play", description: "Phát nhạc từ youtube, soundcloud, local file,...")]
        public async Task Play(InteractionContext ctx, [Option("url", "Địa chỉ bài nhạc hoặc tên bài nhạc")] string url)
        {
            try
            {
                await ctx.DeferAsync().ConfigureAwait(false);

                var player = await GetPlayerAsync(ctx, connectToVoiceChannel: true).ConfigureAwait(false);

                if (player == null)
                {
                    return;
                }

                var track = await _audioService.Tracks
                    .LoadTrackAsync(url, TrackSearchMode.YouTube)
                    .ConfigureAwait(false);

                if (track is null)
                {
                    var errorMessage = new DiscordEmbedBuilder()
                            .WithTitle("Lỗi xảy ra")
                            .WithDescription("Chino không tìm thấy kết quả mà bạn nhập")
                            .WithColor(DiscordColor.Red);
                    await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(errorMessage));
                    return;
                }

                var position = await player
                    .PlayAsync(track)
                    .ConfigureAwait(false);

                if (position is 0)
                {
                    await ctx
                        .FollowUpAsync(new DiscordFollowupMessageBuilder()
                        .WithContent($"🔈 Chino hiện đang phát: {track.Uri}"))
                        .ConfigureAwait(false);
                }
                else
                {
                    await ctx
                        .FollowUpAsync(new DiscordFollowupMessageBuilder()
                        .WithContent($"🔈 Chino đã thêm nhạc vào hàng chờ: {track.Uri}"))
                        .ConfigureAwait(false);
                }
            }
            catch (Exception e)
            {
                await ctx
                    .FollowUpAsync(new DiscordFollowupMessageBuilder()
                    .WithContent($"🔈 Chino phát hiện lỗi rồi nè~: {e.Message}"))
                    .ConfigureAwait(false);
            }
        }

        [SlashCommand("pause", "Dừng phát nhạc")]
        public async Task Pause(InteractionContext ctx)
        {
            try
            {
                await ctx.DeferAsync().ConfigureAwait(false);

                var player = await GetPlayerAsync(ctx, connectToVoiceChannel: true).ConfigureAwait(false);

                if (player == null)
                {
                    return;
                }

                await ctx
                    .FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent(":pause_button: Nhạc đang được dừng."))
                    .ConfigureAwait(false);

                await player.PauseAsync().ConfigureAwait(false);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }

        [SlashCommand("resume", "Tiếp tục phát nhạc")]
        public async Task Resume(InteractionContext ctx)
        {
            try
            {
                await ctx.DeferAsync().ConfigureAwait(false);

                var player = await GetPlayerAsync(ctx, connectToVoiceChannel: true).ConfigureAwait(false);

                if (player == null)
                {
                    return;
                }

                await ctx
                    .FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent("🔈 Nhạc đang được phát tiếp."))
                    .ConfigureAwait(false);

                await player.ResumeAsync().ConfigureAwait(false);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }
        [SlashCommand("skip", "Bỏ qua bài nhạc hiện tại")]
        public async Task Skip(InteractionContext ctx)
        {
            try
            {
                await ctx.DeferAsync().ConfigureAwait(false);

                var player = await GetPlayerAsync(ctx, connectToVoiceChannel: true).ConfigureAwait(false);

                if (player == null)
                {
                    return;
                }
                await ctx
                    .FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent($"🔈 Chino đã bỏ qua bài nhạc hiện tại, đang tiến hành phát bài khác {player.CurrentItem}"))
                    .ConfigureAwait(false);

                await player.SkipAsync().ConfigureAwait(false);
            }
            catch (Exception e)
            {
                await ctx
                    .FollowUpAsync(new DiscordFollowupMessageBuilder()
                    .WithContent($"🔈 Chino phát hiện lỗi rồi nè~: {e.Message}"))
                    .ConfigureAwait(false);
            }
        }

        [SlashCommand("leave", "Chino rời khỏi voice chat")]
        public async Task Leave(InteractionContext ctx)
        {
            try
            {
                await ctx.DeferAsync().ConfigureAwait(false);

                var player = await GetPlayerAsync(ctx, connectToVoiceChannel: true).ConfigureAwait(false);

                if (player == null)
                {
                    return;
                }
                var botMember = ctx.Guild.CurrentMember;
                await botMember.ModifyAsync(properties => properties.VoiceChannel = null).ConfigureAwait(false);

                await ctx
                    .FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent("🔈 Chino đã rời voice chat."))
                    .ConfigureAwait(false);
            }
            catch (Exception e)
            {
                await ctx
                    .FollowUpAsync(new DiscordFollowupMessageBuilder()
                    .WithContent($"🔈 Chino phát hiện lỗi rồi nè~: {e.Message}"))
                    .ConfigureAwait(false);
            }
        }

        [SlashCommand("stop", "Dừng phát nhạc hoàn toàn")]
        public async Task Stop(InteractionContext ctx)
        {
            try
            {
                await ctx.DeferAsync().ConfigureAwait(false);

                var player = await GetPlayerAsync(ctx, connectToVoiceChannel: true).ConfigureAwait(false);

                if (player == null)
                {
                    return;
                }
                var botMember = ctx.Guild.CurrentMember;
                await botMember.ModifyAsync(properties => properties.VoiceChannel = null).ConfigureAwait(false);

                await ctx
                    .FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent(":mute: Chino đã dừng phát tất cả nhạc."))
                    .ConfigureAwait(false);

                await player.StopAsync().ConfigureAwait(false);
            }
            catch (Exception e)
            {
                await ctx
                    .FollowUpAsync(new DiscordFollowupMessageBuilder()
                    .WithContent($"🔈 Chino phát hiện lỗi rồi nè~: {e.Message}"))
                    .ConfigureAwait(false);
            }
        }
        private async ValueTask<QueuedLavalinkPlayer?> GetPlayerAsync(InteractionContext interactionContext, bool connectToVoiceChannel = true)
        {
            ArgumentNullException.ThrowIfNull(interactionContext);

            var voiceChannelBeforeAction = interactionContext.Member?.VoiceState?.Channel;

            if (connectToVoiceChannel && voiceChannelBeforeAction == null)
            {
                var errorResponse = new DiscordFollowupMessageBuilder()
                    .WithContent("Bạn hiện đang không có trong voice channel nên Chino không phát nhạc được ;-;")
                    .AsEphemeral();

                await interactionContext
                    .FollowUpAsync(errorResponse)
                    .ConfigureAwait(false);

                return null;
            }

            var retrieveOptions = new PlayerRetrieveOptions(
                ChannelBehavior: connectToVoiceChannel ? PlayerChannelBehavior.Join : PlayerChannelBehavior.None);

            var playerOptions = new QueuedLavalinkPlayerOptions { HistoryCapacity = 10000 };

            var result = await _audioService.Players
                .RetrieveAsync(interactionContext.Guild.Id, interactionContext.Member?.VoiceState.Channel.Id, playerFactory: PlayerFactory.Queued, Options.Create(playerOptions), retrieveOptions)
                .ConfigureAwait(false);

            if (!result.IsSuccess)
            {
                var errorMessage = result.Status switch
                {
                    PlayerRetrieveStatus.UserNotInVoiceChannel => "You are not connected to a voice channel.",
                    PlayerRetrieveStatus.BotNotConnected => "The bot is currently not connected.",
                    _ => "Unknown error.",
                };

                var errorResponse = new DiscordFollowupMessageBuilder()
                    .WithContent(errorMessage)
                    .AsEphemeral();

                await interactionContext
                    .FollowUpAsync(errorResponse)
                    .ConfigureAwait(false);

                return null;
            }

            var voiceChannelAfterAction = interactionContext.Member?.VoiceState?.Channel;

            if (connectToVoiceChannel && voiceChannelBeforeAction != voiceChannelAfterAction)
            {
                var botMember = interactionContext.Guild.CurrentMember;
                await botMember.ModifyAsync(properties => properties.VoiceChannel = null).ConfigureAwait(false);
            }

            return result.Player;
        }
    }
}
