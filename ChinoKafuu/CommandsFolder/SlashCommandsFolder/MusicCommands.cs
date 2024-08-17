using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using Lavalink4NET;
using Lavalink4NET.Players.Queued;
using Lavalink4NET.Players;
using Lavalink4NET.Rest.Entities.Tracks;
using Microsoft.Extensions.Options;
using System.Text;

namespace ChinoBot.CommandsFolder.SlashCommandsFolder
{
    public class MusicCommands : ApplicationCommandModule
    {
        private readonly IAudioService _audioService;
        private static Dictionary<ulong, TimeSpan> _pausedPositions = new Dictionary<ulong, TimeSpan>();

        public MusicCommands(IAudioService audioService)
        {
            ArgumentNullException.ThrowIfNull(audioService);

            _audioService = audioService;
        }

        [SlashCommand("mhelp", "Hiển thị danh sách các lệnh và mô tả của chúng")]
        public async Task Help(InteractionContext ctx)
        {
            var embed = new DiscordEmbedBuilder()
                .WithTitle("Hướng dẫn sử dụng để nghe nhạc")
                .WithDescription("Dưới đây là danh sách các lệnh mà em hỗ trợ nè:")
                .WithColor(DiscordColor.Azure);

            embed.AddField("/mplay [url/tên bài nhạc]", "Phát nhạc từ URL hoặc tên bài nhạc/playlist.", true);
            embed.AddField("/mpause", "Tạm dừng phát nhạc hiện tại.", true);
            embed.AddField("/mresume", "Tiếp tục phát nhạc đã tạm dừng.", true);
            embed.AddField("/mstop", "Dừng phát nhạc và thoát khỏi kênh thoại.", true);
            embed.AddField("/mskip", "Bỏ qua bài nhạc hiện tại và phát bài tiếp theo.", true);
            embed.AddField("/mqueue", "Hiển thị danh sách các bài nhạc đang chờ.", true);
            embed.AddField("/mloop [track/queue/off]", "Lặp lại bài nhạc hiện tại hoặc toàn bộ danh sách hàng đợi.", true);
            embed.AddField("/mshuffle", "Xáo trộn thứ tự các bài nhạc trong hàng đợi.", true);
            embed.AddField("/mseek [mm:ss]", "Tua đến một thời điểm cụ thể trong bài nhạc.", true);
            embed.AddField("/mremove [index]", "Xóa bài nhạc tại vị trí chỉ định trong hàng đợi.", true);
            embed.AddField("/mmove [from] [to]", "Di chuyển một bài nhạc từ vị trí này sang vị trí khác trong hàng đợi.", true);
            embed.AddField("/mclearqueue", "Xóa toàn bộ danh sách hàng đợi.", true);

            await ctx.CreateResponseAsync(embed: embed);
        }

        [SlashCommand("mplay", "Phát nhạc hoặc playlist từ YouTube")]
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
                        .WithDescription("Em không tìm thấy kết quả~ anh thử bài khác xem sao")
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
                        .WithDescription($"Em đã thêm {trackLoadResult.Tracks.Count()} bài hát từ playlist vào hàng chờ rồi ạ~")
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
                            .WithDescription($"Hiện tại em đang phát: [{track.Title}]({track.Uri}) - {track.Duration}")
                            .WithColor(Helper.GetRandomDiscordColor());
                    }
                    else
                    {
                        embed = new DiscordEmbedBuilder()
                            .WithTitle(":notes: Nhạc đã thêm vào hàng chờ")
                            .WithDescription($"Em đã thêm nhạc vào hàng chờ: [{track.Title}]({track.Uri}) - {track.Duration}")
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
                    .WithContent($"Em phát hiện lỗi rồi nè~: {e.Message}"))
                    .ConfigureAwait(false);
            }
        }

        [SlashCommand("mpause", "Dừng phát nhạc")]
        public async Task Pause(InteractionContext ctx)
        {
            try
            {
                await ctx.DeferAsync().ConfigureAwait(false);

                var player = await GetPlayerAsync(ctx, connectToVoiceChannel: true).ConfigureAwait(false);

                if (player == null || player.CurrentTrack == null)
                {
                    await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent("Không có bài hát nào đang phát.")).ConfigureAwait(false);
                    return;
                }

                var currentPosition = player.Position;
                if (currentPosition.HasValue)
                {
                    _pausedPositions[ctx.Guild.Id] = currentPosition.Value.Position;

                    var embed = new DiscordEmbedBuilder()
                        .WithTitle(":pause_button: Nhạc đang được dừng")
                        .WithDescription($"Em đã dừng bài nhạc tại: {currentPosition.Value.Position:mm\\:ss}")
                        .WithColor(DiscordColor.White);

                    await ctx
                        .FollowUpAsync(new DiscordFollowupMessageBuilder()
                        .AddEmbed(embed))
                        .ConfigureAwait(false);

                    await player.PauseAsync().ConfigureAwait(false);
                }
                else
                {
                    await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent("Không thể xác định vị trí hiện tại của bài hát.")).ConfigureAwait(false);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent($"Đã xảy ra lỗi: {e.Message}")).ConfigureAwait(false);
            }
        }

        [SlashCommand("mresume", "Tiếp tục phát nhạc")]
        public async Task Resume(InteractionContext ctx)
        {
            try
            {
                await ctx.DeferAsync().ConfigureAwait(false);

                var player = await GetPlayerAsync(ctx, connectToVoiceChannel: true).ConfigureAwait(false);

                if (player == null)
                {
                    await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent("Không có phiên phát nhạc nào đang hoạt động.")).ConfigureAwait(false);
                    return;
                }

                var embed = new DiscordEmbedBuilder()
                    .WithTitle(":arrow_forward: Nhạc đang được phát tiếp")
                    .WithColor(DiscordColor.White);

                if (_pausedPositions.TryGetValue(ctx.Guild.Id, out TimeSpan pausedPosition))
                {
                    await player.SeekAsync(pausedPosition).ConfigureAwait(false);
                    embed.WithDescription($"Tiếp tục phát từ: {pausedPosition:mm\\:ss}");
                    _pausedPositions.Remove(ctx.Guild.Id);
                }
                else
                {
                    embed.WithDescription("Tiếp tục phát nhạc");
                }

                await player.ResumeAsync().ConfigureAwait(false);

                await ctx
                    .FollowUpAsync(new DiscordFollowupMessageBuilder()
                    .AddEmbed(embed))
                    .ConfigureAwait(false);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent($"Đã xảy ra lỗi: {e.Message}")).ConfigureAwait(false);
            }
        }

        [SlashCommand("mskip", "Bỏ qua bài nhạc hiện tại")]
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
                    var embed = new DiscordEmbedBuilder()
                        .WithTitle(":red_circle: Em không thể xử lý vì không có bài nhạc nào trong hàng đợi")
                        .WithColor(DiscordColor.Azure);

                    await ctx
                        .FollowUpAsync(new DiscordFollowupMessageBuilder()
                        .AddEmbed(embed))
                        .ConfigureAwait(false);

                }
                else
                {
                    await player.SkipAsync().ConfigureAwait(false);

                    var embed = new DiscordEmbedBuilder()
                        .WithTitle(":fast_forward: Em đã bỏ qua bài nhạc hiện tại rồi ạ~")
                        .WithDescription($"Đang tiến hành phát bài khác: [{player.CurrentTrack.Title}]({player.CurrentItem.Track.Uri})")
                        .WithColor(DiscordColor.Azure);

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
                    .WithContent($"Em phát hiện lỗi rồi nè~: {e.Message}"))
                    .ConfigureAwait(false);
            }
        }

        [SlashCommand("mleave", "Chino rời khỏi voice chat")]
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
                    .WithTitle("Dạ~")
                    .WithColor(DiscordColor.Green);

                await ctx
                    .FollowUpAsync(new DiscordFollowupMessageBuilder()
                    .AddEmbed(embed))
                    .ConfigureAwait(false);
            }
            catch (Exception e)
            {
                await ctx
                    .FollowUpAsync(new DiscordFollowupMessageBuilder()
                    .WithContent($"Em phát hiện lỗi rồi nè~: {e.Message}"))
                    .ConfigureAwait(false);
            }
        }

        [SlashCommand("mstop", "Dừng phát nhạc hoàn toàn")]
        public async Task Stop(InteractionContext ctx)
        {
            try
            {
                await ctx.DeferAsync().ConfigureAwait(false);

                var player = await GetPlayerAsync(ctx, connectToVoiceChannel: false).ConfigureAwait(false);
                if (player == null)
                {
                    await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent("Không có phiên phát nhạc nào đang hoạt động.")).ConfigureAwait(false);
                    return;
                }

                await player.StopAsync().ConfigureAwait(false);
                var botMember = ctx.Guild.CurrentMember;
                await botMember.ModifyAsync(properties => properties.VoiceChannel = null).ConfigureAwait(false);

                var embed = new DiscordEmbedBuilder()
                    .WithTitle(":mute: Em đã dừng phát nhạc")
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
                    .WithContent($"Em phát hiện lỗi rồi nè~: {e.Message}"))
                    .ConfigureAwait(false);
            }
        }

        [SlashCommand("mnowplaying", "Xem bài nhạc đang phát là gì")]
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

                var embed = new DiscordEmbedBuilder()
                    .WithTitle($":musical_note: Nhạc hiện tại đang được phát là:")
                    .WithDescription($"- [{player.CurrentTrack.Title}]({player.CurrentTrack.Uri}) - {player.CurrentTrack.Duration}")
                    .WithColor(DiscordColor.Azure);

                await ctx
                    .FollowUpAsync(new DiscordFollowupMessageBuilder().AddEmbed(embed))
                    .ConfigureAwait(false);
            }
            catch (Exception e)
            {
                await ctx
                    .FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent($"Chino phát hiện lỗi rồi nè~: {e.Message}"))
                    .ConfigureAwait(false);
            }
        }

        [SlashCommand("mqueue", "Xem danh sách nhạc đang chờ được phát")]
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

                StringBuilder queueContent = new StringBuilder();
                int index = 0;

                foreach (var track in player.Queue)
                {
                    string title = track.Track.Title.ToString();
                    if (Uri.TryCreate(title, UriKind.Absolute, out Uri uri))
                    {
                        title = System.Web.HttpUtility.UrlDecode(Path.GetFileNameWithoutExtension(uri.AbsolutePath));
                    }

                    queueContent.AppendLine($"{index}: [{title}]({track.Track.Uri})");
                    index++; 
                }

                var embed = new DiscordEmbedBuilder()
                    .WithTitle(":musical_note: Đây là danh sách các bài nhạc đang chờ ạ:")
                    .WithDescription(queueContent.ToString())
                    .WithColor(DiscordColor.Azure);

                await ctx
                    .FollowUpAsync(new DiscordFollowupMessageBuilder()
                    .AddEmbed(embed))
                    .ConfigureAwait(false);
            }
            catch (Exception e)
            {
                await ctx
                    .FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent($"Em phát hiện lỗi rồi nè~: {e.Message}"))
                    .ConfigureAwait(false);
            }
        }

        [SlashCommand("mvolume", "Chỉnh âm lượng phát nhạc")]
        public async Task SetVolume(InteractionContext ctx, [Option("volume", "Mức âm lượng (0-100)")] long volume)
        {
            try
            {
                await ctx.DeferAsync().ConfigureAwait(false);

                var player = await GetPlayerAsync(ctx, connectToVoiceChannel: false).ConfigureAwait(false);

                if (player == null)
                {
                    await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent(":warning: Hiện không có nhạc nào đang phát. Anh thử phát một bài nào đó rồi thử lại xem sao~")).ConfigureAwait(false);
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
                     .WithTitle(":sound: Vâng~ âm lượng đã được thay đổi")
                     .WithDescription($"Âm lượng đã được đặt thành {volume}%")
                     .WithColor(DiscordColor.Green); 

                await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder()
                    .AddEmbed(embed))
                    .ConfigureAwait(false);
            }
            catch (Exception e)
            {
                await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent($"Em phát hiện lỗi rồi nè~: {e.Message}")).ConfigureAwait(false);
            }
        }

        [SlashCommand("mloop", "Lặp lại bài nhạc hiện tại hoặc toàn bộ danh sách hàng đợi")]
        public async Task Loop(InteractionContext ctx, [Option("mode", "Chế độ lặp lại (track/queue/off)")] string mode)
        {
            try
            {
                await ctx.DeferAsync().ConfigureAwait(false);

                var player = await GetPlayerAsync(ctx, connectToVoiceChannel: false).ConfigureAwait(false);

                if (player == null)
                {
                    return;
                }

                switch (mode.ToLower())
                {
                    case "track":
                        player.RepeatMode = TrackRepeatMode.Track;
                        var trackEmbed = new DiscordEmbedBuilder()
                            .WithTitle(":repeat_one: Chế độ lặp lại")
                            .WithDescription("Vâng~ em sẽ lặp lại bài hát hiện tại")
                            .WithColor(DiscordColor.Green);
                        await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().AddEmbed(trackEmbed)).ConfigureAwait(false);
                        break;
                    case "queue":
                        if (player.Queue.Count == 0)
                        {
                            var queueErrorEmbed = new DiscordEmbedBuilder()
                                    .WithTitle(":repeat: Chế độ lặp lại")
                                    .WithDescription("Hiện không có bài hát nào trong hàng đợi")
                                    .WithColor(DiscordColor.Red);
                            await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().AddEmbed(queueErrorEmbed)).ConfigureAwait(false);
                            return;
                        }
                        player.RepeatMode = TrackRepeatMode.Queue;
                        var queueEmbed = new DiscordEmbedBuilder()
                            .WithTitle(":repeat: Chế độ lặp lại")
                            .WithDescription("Em hiểu rồi~ em sẽ lặp lại toàn bộ danh sách hàng đợi")
                            .WithColor(DiscordColor.Green);
                        await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().AddEmbed(queueEmbed)).ConfigureAwait(false);
                        break;
                    case "off":
                        player.RepeatMode = TrackRepeatMode.None;
                        var offEmbed = new DiscordEmbedBuilder()
                            .WithTitle(":arrow_forward: Chế độ lặp lại")
                            .WithDescription("Em đã tắt chế độ lặp lại")
                            .WithColor(DiscordColor.Red);
                        await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().AddEmbed(offEmbed)).ConfigureAwait(false);
                        break;
                    default:
                        var errorEmbed = new DiscordEmbedBuilder()
                            .WithTitle("Lỗi: Chế độ lặp lại không hợp lệ")
                            .WithDescription("Hiện tại em chỉ hỗ trợ các mode như `track`, `queue`, hoặc `off` thôi~")
                            .WithColor(DiscordColor.Red);
                        await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().AddEmbed(errorEmbed)).ConfigureAwait(false);
                        break;
                }
            }
            catch (Exception e)
            {
                await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent($"Em phát hiện lỗi rồi nè~: {e.Message}")).ConfigureAwait(false);
            }
        }

        [SlashCommand("mshuffle", "Xáo trộn danh sách phát")]
        public async Task Shuffle(InteractionContext ctx)
        {
            try
            {
                await ctx.DeferAsync().ConfigureAwait(false);

                var player = await GetPlayerAsync(ctx, connectToVoiceChannel: false).ConfigureAwait(false);

                if (player == null)
                {
                    return;
                }

                var shuffledList = player.Queue.ToList();
                var random = new Random();
                int n = shuffledList.Count;
                while (n > 1)
                {
                    n--;
                    int k = random.Next(n + 1);
                    var value = shuffledList[k];
                    shuffledList[k] = shuffledList[n];
                    shuffledList[n] = value;
                }

                player.Queue.ClearAsync();
                foreach (var item in shuffledList)
                {
                    player.Queue.AddAsync(item);
                }

                var embed = new DiscordEmbedBuilder()
                    .WithTitle(":twisted_rightwards_arrows: Danh sách phát đã được xáo trộn")
                    .WithColor(DiscordColor.Green);

                await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().AddEmbed(embed)).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent($"Em phát hiện lỗi rồi nè~: {e.Message}")).ConfigureAwait(false);
            }
        }

        [SlashCommand("mseek", "Tua đến một vị trí cụ thể trong bài hát")]
        public async Task Seek(InteractionContext ctx, [Option("time", "Thời gian (ví dụ: 00:01:30)")] string timeString)
        {
            try
            {
                await ctx.DeferAsync().ConfigureAwait(false);

                var player = await GetPlayerAsync(ctx, connectToVoiceChannel: false).ConfigureAwait(false);

                if (player == null || player.CurrentTrack == null)
                {
                    await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent("Không có bài hát nào đang phát.")).ConfigureAwait(false);
                    return;
                }

                if (TimeSpan.Parse(timeString) > player.CurrentTrack.Duration)
                {
                    await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent("Không thể tua quá thời lượng bài ")).ConfigureAwait(false);
                    return;
                }

                if (TimeSpan.TryParse(timeString, out TimeSpan time))
                {
                    await player.SeekAsync(time).ConfigureAwait(false);

                    var embed = new DiscordEmbedBuilder()
                        .WithTitle(":fast_forward: Đã tua đến vị trí mới")
                        .WithDescription($"Vị trí hiện tại: {time:mm\\:ss}")
                        .WithColor(DiscordColor.Green);

                    await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().AddEmbed(embed)).ConfigureAwait(false);
                }
                else
                {
                    await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent("Định dạng thời gian không hợp lệ. Vui lòng sử dụng định dạng mm:ss.")).ConfigureAwait(false);
                }
            }
            catch (Exception e)
            {
                await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent($"Em phát hiện lỗi rồi nè~: {e.Message}")).ConfigureAwait(false);
            }
        }

        [SlashCommand("mremove", "Xóa một bài hát khỏi hàng đợi")]
        public async Task Remove(InteractionContext ctx, [Option("position", "Vị trí của bài hát trong hàng đợi")] long position)
        {
            try
            {
                await ctx.DeferAsync().ConfigureAwait(false);

                var player = await GetPlayerAsync(ctx, connectToVoiceChannel: false).ConfigureAwait(false);

                if (player == null)
                {
                    return;
                }

                if (position < 1 || position > player.Queue.Count)
                {
                    await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent("Vị trí không hợp lệ.")).ConfigureAwait(false);
                    return;
                }

                var trackToRemove = player.Queue.ElementAt((int)position - 1);
                var newQueue = player.Queue.Where((_, index) => index != position - 1).ToList();
                player.Queue.ClearAsync();
                foreach (var track in newQueue)
                {
                    player.Queue.AddAsync(track);
                }

                var embed = new DiscordEmbedBuilder()
                    .WithTitle(":x: Đã xóa bài hát khỏi hàng đợi")
                    .WithDescription($"Em đã xóa: [{trackToRemove.Track.Title}]({trackToRemove.Track.Uri}) ra khỏi hàng đợi")
                    .WithColor(DiscordColor.Red);

                await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().AddEmbed(embed)).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent($":x: phát hiện lỗi rồi nè~: {e.Message}")).ConfigureAwait(false);
            }
        }

        [SlashCommand("mmove", "Di chuyển một bài hát trong hàng đợi")]
        public async Task Move(InteractionContext ctx, [Option("from", "Vị trí hiện tại của bài hát")] long from, [Option("to", "Vị trí mới của bài hát")] long to)
        {
            try
            {
                await ctx.DeferAsync().ConfigureAwait(false);

                var player = await GetPlayerAsync(ctx, connectToVoiceChannel: false).ConfigureAwait(false);

                if (player == null)
                {
                    return;
                }

                if (from < 1 || from > player.Queue.Count || to < 1 || to > player.Queue.Count)
                {
                    await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent("Vị trí không hợp lệ.")).ConfigureAwait(false);
                    return;
                }
                var trackList = player.Queue.ToList();
                var trackToMove = trackList[(int)from - 1];
                trackList.RemoveAt((int)from - 1);
                trackList.Insert((int)to - 1, trackToMove);

                player.Queue.ClearAsync();
                foreach (var track in trackList)
                {
                    player.Queue.AddAsync(track);
                }

                var embed = new DiscordEmbedBuilder()
                    .WithTitle(":arrow_right_hook: Đã di chuyển bài hát")
                    .WithDescription($"Đã di chuyển [{trackToMove.Track.Title}]({trackToMove.Track.Uri}) từ vị trí {from} đến {to}")
                    .WithColor(DiscordColor.Green);

                await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().AddEmbed(embed)).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent($":x: phát hiện lỗi rồi nè~: {e.Message}")).ConfigureAwait(false);
            }
        }

        [SlashCommand("mclear", "Xóa toàn bộ hàng đợi")]
        public async Task Clear(InteractionContext ctx)
        {
            try
            {
                await ctx.DeferAsync().ConfigureAwait(false);

                var player = await GetPlayerAsync(ctx, connectToVoiceChannel: false).ConfigureAwait(false);

                if (player == null)
                {
                    return;
                }

                player.Queue.ClearAsync();

                var embed = new DiscordEmbedBuilder()
                    .WithTitle(":broom: Đã xóa hàng đợi")
                    .WithDescription("Toàn bộ hàng đợi đã được xóa.")
                    .WithColor(DiscordColor.Red);

                await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().AddEmbed(embed)).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent($":x: phát hiện lỗi rồi nè~: {e.Message}")).ConfigureAwait(false);
            }
        }
        private async ValueTask<QueuedLavalinkPlayer?> GetPlayerAsync(InteractionContext interactionContext, bool connectToVoiceChannel = true)
        {
            ArgumentNullException.ThrowIfNull(interactionContext);

            var voiceChannelBeforeAction = interactionContext.Member?.VoiceState?.Channel;

            if (connectToVoiceChannel && voiceChannelBeforeAction == null)
            {
                var errorResponse = new DiscordFollowupMessageBuilder()
                    .WithContent("Anh hiện đang không có trong voice channel nên em không phát nhạc được ;-;")
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
                    PlayerRetrieveStatus.BotNotConnected => "Em hiện tại chưa kết nối được với node của server. Anh thử hỏi nup xem sao",
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
