using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using Lavalink4NET;
using Lavalink4NET.Players.Queued;
using Lavalink4NET.Players;
using Lavalink4NET.Rest.Entities.Tracks;
using Microsoft.Extensions.Options;
using DSharpPlus;
using System.Text;
using System.Runtime.InteropServices;
using DSharpPlus.VoiceNext;
using System.Diagnostics;

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
                .AddField("/nowplaying", "Xem nhạc hiện tại đang được phát")
                .AddField("/queue", "Xem nhạc hiện tại trong danh sách chờ")
                .AddField("/volume", "Điều chỉnh âm thanh phát nhạc")
                .WithFooter("Để sử dụng lệnh cụ thể, nhập /tên-lệnh");

            await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                new DiscordInteractionResponseBuilder().AddEmbed(embed));
        }

        [SlashCommand("play", "Phát nhạc hoặc playlist từ YouTube")]
        public async Task Play(InteractionContext ctx, [Option("input", "URL hoặc tên bài hát/playlist")] string input)
        {
            try
            {
                await ctx.DeferAsync().ConfigureAwait(false);

                var player = await GetPlayerAsync(ctx, connectToVoiceChannel: true).ConfigureAwait(false);

                if (player == null)
                {
                    return;
                }

                await player.SetVolumeAsync(0.75f).ConfigureAwait(false);

                var trackLoadResult = await _audioService.Tracks
                    .LoadTracksAsync(input, TrackSearchMode.YouTube)
                    .ConfigureAwait(false);

                if (!trackLoadResult.HasMatches)
                {
                    var errorMessage = new DiscordEmbedBuilder()
                        .WithTitle("Lỗi xảy ra")
                        .WithDescription("Chino không tìm thấy kết quả mà bạn nhập")
                        .WithColor(DiscordColor.Red);
                    await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(errorMessage));
                    return;
                }

                if (trackLoadResult.IsPlaylist)
                {
                    foreach (var track in trackLoadResult.Tracks)
                    {
                        await player.PlayAsync(track).ConfigureAwait(false);
                    }

                    var playlistEmbed = new DiscordEmbedBuilder()
                        .WithTitle(":notes: Playlist đã được thêm vào hàng chờ")
                        .WithDescription($"Chino đã thêm {trackLoadResult.Tracks.Count()} bài hát từ playlist vào hàng chờ")
                        .WithColor(DiscordColor.Green);

                    await ctx
                        .FollowUpAsync(new DiscordFollowupMessageBuilder()
                        .AddEmbed(playlistEmbed))
                        .ConfigureAwait(false);
                }
                else
                {
                    var track = trackLoadResult.Tracks.First();
                    var position = await player
                        .PlayAsync(track)
                        .ConfigureAwait(false);

                    DiscordEmbedBuilder embed;
                    if (position is 0)
                    {
                        embed = new DiscordEmbedBuilder()
                            .WithTitle(":loud_sound: Nhạc đang phát")
                            .WithDescription($"Chino hiện đang phát: [{track.Title}]({track.Uri})")
                            .WithColor(Helper.GetRandomDiscordColor());
                    }
                    else
                    {
                        embed = new DiscordEmbedBuilder()
                            .WithTitle(":notes: Nhạc đã thêm vào hàng chờ")
                            .WithDescription($"Chino đã thêm nhạc vào hàng chờ: [{track.Title}]({track.Uri})")
                            .WithColor(DiscordColor.Green);
                    }

                    await ctx
                        .FollowUpAsync(new DiscordFollowupMessageBuilder()
                        .AddEmbed(embed))
                        .ConfigureAwait(false);
                }
            }
            catch (Exception e)
            {
                await ctx
                    .FollowUpAsync(new DiscordFollowupMessageBuilder()
                    .WithContent($"Chino phát hiện lỗi rồi nè~: {e.Message}"))
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

                var embed = new DiscordEmbedBuilder()
                    .WithTitle(":pause_button: Nhạc đang được dừng")
                    .WithColor(DiscordColor.White);

                await ctx
                    .FollowUpAsync(new DiscordFollowupMessageBuilder()
                    .AddEmbed(embed))
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

                var embed = new DiscordEmbedBuilder()
                    .WithTitle(":arrow_forward: Nhạc đang được phát tiếp")
                    .WithColor(DiscordColor.White);

                await ctx
                    .FollowUpAsync(new DiscordFollowupMessageBuilder()
                    .AddEmbed(embed))
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
                if (player.Queue.Count < 1)
                {
                    await ctx
                        .FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent($":fast_forward: Chino không thể xử lý vì không có bài nhạc nào trong hàng đợi"))
                        .ConfigureAwait(false);
                }
                await player.SkipAsync().ConfigureAwait(false);

                var embed = new DiscordEmbedBuilder()
                    .WithTitle(":fast_forward: Chino đã bỏ qua bài nhạc hiện tại")
                    .WithDescription($"Đang tiến hành phát bài khác: {player.CurrentItem.Track.Uri}")
                    .WithColor(DiscordColor.Azure); 

                await ctx
                    .FollowUpAsync(new DiscordFollowupMessageBuilder()
                    .AddEmbed(embed))
                    .ConfigureAwait(false);

            }
            catch (Exception e)
            {
                await ctx
                    .FollowUpAsync(new DiscordFollowupMessageBuilder()
                    .WithContent($"Chino phát hiện lỗi rồi nè~: {e.Message}"))
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

                var embed = new DiscordEmbedBuilder()
                    .WithTitle(":mute: Chino đã rời voice chat")
                    .WithColor(DiscordColor.Red);

                await ctx
                    .FollowUpAsync(new DiscordFollowupMessageBuilder()
                    .AddEmbed(embed))
                    .ConfigureAwait(false);
            }
            catch (Exception e)
            {
                await ctx
                    .FollowUpAsync(new DiscordFollowupMessageBuilder()
                    .WithContent($"Chino phát hiện lỗi rồi nè~: {e.Message}"))
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

                var embed = new DiscordEmbedBuilder()
                    .WithTitle(":mute: Chino đã dừng phát tất cả nhạc")
                    .WithColor(DiscordColor.Red); 

                await ctx
                    .FollowUpAsync(new DiscordFollowupMessageBuilder()
                    .AddEmbed(embed))
                    .ConfigureAwait(false);

                await player.StopAsync().ConfigureAwait(false);
            }
            catch (Exception e)
            {
                await ctx
                    .FollowUpAsync(new DiscordFollowupMessageBuilder()
                    .WithContent($"Chino phát hiện lỗi rồi nè~: {e.Message}"))
                    .ConfigureAwait(false);
            }
        }

        [SlashCommand("nowplaying", "Xem bài nhạc đang phát là gì")]
        public async Task NowPlaying(InteractionContext ctx)
        {
            try
            {
                await ctx.DeferAsync().ConfigureAwait(false);

                var player = await GetPlayerAsync(ctx, connectToVoiceChannel: true).ConfigureAwait(false);

                if (player == null || player.CurrentTrack == null)
                {
                    await ctx
                        .FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent(":musical_note: Hiện không có bài nhạc đang được phát."))
                        .ConfigureAwait(false);
                    return;
                }

                await ctx
                    .FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent($":musical_note: Nhạc hiện tại đang được phát là: {player.CurrentTrack.Uri}"))
                    .ConfigureAwait(false);
            }
            catch (Exception e)
            {
                await ctx
                    .FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent($"Chino phát hiện lỗi rồi nè~: {e.Message}"))
                    .ConfigureAwait(false);
            }
        }

        [SlashCommand("queue", "Xem danh sách nhạc đang chờ được phát")]
        public async Task musicQueue(InteractionContext ctx)
        {
            try
            {
                await ctx.DeferAsync().ConfigureAwait(false);

                var player = await GetPlayerAsync(ctx, connectToVoiceChannel: true).ConfigureAwait(false);

                if (player == null || player.Queue.Count == 0)
                {
                    await ctx
                        .FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent(":musical_note: Hiện không có bài nhạc nào trong hàng đợi."))
                        .ConfigureAwait(false);
                    return;
                }

                StringBuilder queueContent = new StringBuilder(":musical_note: Danh sách các bài nhạc đang chờ:\n");
                foreach (var track in player.Queue)
                {
                    queueContent.AppendLine($"- {track.Track.Uri}");
                }

                await ctx
                    .FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent(queueContent.ToString()))
                    .ConfigureAwait(false);
            }
            catch (Exception e)
            {
                await ctx
                    .FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent($"Chino phát hiện lỗi rồi nè~: {e.Message}"))
                    .ConfigureAwait(false);
            }
        }

        [SlashCommand("volume", "Chỉnh âm lượng phát nhạc")]
        public async Task SetVolume(InteractionContext ctx, [Option("volume", "Mức âm lượng (0-100)")] long volume)
        {
            try
            {
                await ctx.DeferAsync().ConfigureAwait(false);

                var player = await GetPlayerAsync(ctx, connectToVoiceChannel: false).ConfigureAwait(false);

                if (player == null)
                {
                    await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent(":warning: Hiện không có nhạc nào đang phát hoặc bot chưa tham gia voice channel.")).ConfigureAwait(false);
                    return;
                }

                if (volume < 0 || volume > 100)
                {
                    await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent(":warning: Mức âm lượng phải nằm trong khoảng từ 0 đến 100.")).ConfigureAwait(false);
                    return;
                }

                float volumeNormalized = volume / 100f;

                await player.SetVolumeAsync(volumeNormalized).ConfigureAwait(false);

                var embed = new DiscordEmbedBuilder()
                     .WithTitle(":sound: Âm lượng đã được thay đổi")
                     .WithDescription($"Âm lượng đã được đặt thành {volume}%")
                     .WithColor(DiscordColor.Green); 

                await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder()
                    .AddEmbed(embed))
                    .ConfigureAwait(false);
            }
            catch (Exception e)
            {
                await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent($"Chino phát hiện lỗi rồi nè~: {e.Message}")).ConfigureAwait(false);
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
                    PlayerRetrieveStatus.UserNotInVoiceChannel => "Bạn chưa kết nối vào voice chat",
                    PlayerRetrieveStatus.BotNotConnected => "Chino hiện tại chưa kết nối được với node của server",
                    _ => "Lỗi không xác định được.",
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
