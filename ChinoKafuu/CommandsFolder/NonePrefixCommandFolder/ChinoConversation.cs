using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using DSharpPlus.VoiceNext;
using DSharpPlus;
using NAudio.Wave;
using System.Collections.Concurrent;
using ChinoBot.config;
using System.Text.Json;
using System.Net.Http;
using System.Text;
using System.Net.Http.Json;
using System.Threading;

namespace ChinoBot.CommandsFolder.NonePrefixCommandFolder
{
    internal class ChinoConversation
    {
        private readonly EnvReader jsonReader;
        private readonly DiscordClient _client;
        private readonly GeminiChat _geminiService;
        private readonly GeminiTranslate _geminiTranslate;
        private readonly ConcurrentDictionary<ulong, VoiceNextConnection> _connections = new();
        private readonly ConcurrentDictionary<ulong, SemaphoreSlim> _channelLocks = new();
        private readonly ConcurrentDictionary<ulong, SemaphoreSlim> _voiceLocks = new();
        private readonly ConcurrentDictionary<ulong, Queue<string>> _audioQueues = new();
        private readonly HttpClient _httpClient = new HttpClient() { Timeout = TimeSpan.FromMinutes(5) };
        private readonly ulong testChannel = 1140906898779017268;
        
        public ChinoConversation(DiscordClient client)
        {
            _client = client;
            jsonReader = new EnvReader();
            jsonReader.ReadConfigFile().GetAwaiter().GetResult();
            _geminiService = new GeminiChat(jsonReader.geminiAPIKey);
            _geminiTranslate = new GeminiTranslate(jsonReader.geminiTranslateAPIKey);
            _client.MessageCreated += Client_MessageCreated;
        }

        public bool TryRemoveConnection(ulong guildId, out VoiceNextConnection connection)
        {
            return _connections.TryRemove(guildId, out connection);
        }

        private async Task Client_MessageCreated(DiscordClient sender, MessageCreateEventArgs e)
        {
            if (e.Author.IsBot || e.Message.Content.StartsWith("BOT") ||
                (e.Channel.Id != testChannel && e.Channel.Id != jsonReader.allowChannelID_gemini))
            {
                return;
            }

            await e.Channel.TriggerTypingAsync();
            
            var channelLock = GetOrCreateChannelLock(e.Channel.Id);
            
            _ = Task.Run(async () =>
            {
                await channelLock.WaitAsync();
                try
                {
                    await HandleTextInputAsync(e.Message);
                }
                catch (Exception ex)
                {
                    await e.Channel.SendMessageAsync($"Hic, có lỗi rồi ;-; {ex.Message}");
                }
                finally
                {
                    channelLock.Release();
                }
            });
        }

        private SemaphoreSlim GetOrCreateChannelLock(ulong channelId)
        {
            return _channelLocks.GetOrAdd(channelId, _ => new SemaphoreSlim(1, 1));
        }

        private async Task HandleTextInputAsync(DiscordMessage message)
        {
            string username = message.Author.Username;
            var member = await message.Channel.Guild.GetMemberAsync(message.Author.Id);
            if (member?.Nickname != null)
            {
                username = member.Nickname;
            }

            string projectRoot = Directory.GetParent(AppContext.BaseDirectory).Parent.Parent.Parent.FullName;
            string chatHistoryPath = Path.Combine(
                projectRoot,
                "CommunicationHistory",
                "HistoryChat",
                message.Channel.Guild.Id.ToString(),
                "chat_history.json"
            );

            string chinoMessage = await _geminiService.RunGeminiAPI(message.Content, username, chatHistoryPath, CancellationToken.None);

            if (message.Content.ToLower().Contains("rời voice") ||
                message.Content.ToLower().Contains("out voice") ||
                message.Content.ToLower().Contains("leave voice"))
            {
                if (_connections.TryRemove(message.Channel.GuildId.Value, out var connection))
                {
                    connection.Disconnect();
                }
                await message.Channel.SendMessageAsync(chinoMessage);
                return;
            }
            await SendMessageAndVoiceAsync(message, chinoMessage);
        }

        private async Task SendMessageAndVoiceAsync(DiscordMessage message, string textMessage)
        {
            var guild = message.Channel.Guild;
            var member = await guild.GetMemberAsync(message.Author.Id);
            var voiceState = member?.VoiceState;
            var channel = voiceState?.Channel;

            await message.Channel.SendMessageAsync(textMessage);

            if (channel != null)
            {
                if (!_connections.TryGetValue(guild.Id, out var connection))
                {
                    connection = await channel.ConnectAsync();
                    _connections[guild.Id] = connection;
                }

                try
                {
                    string translateResult = await _geminiTranslate.Translate(textMessage, CancellationToken.None);
                    if (string.IsNullOrEmpty(translateResult))
                    {
                        Console.WriteLine("Translation result is empty!");
                        return;
                    }

                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            string audioFile = await RunTTSScript(translateResult, guild.Id);

                            if (!_audioQueues.TryGetValue(guild.Id, out var queue))
                            {
                                queue = new Queue<string>();
                                _audioQueues[guild.Id] = queue;
                            }

                            queue.Enqueue(audioFile);

                            if (queue.Count == 1)
                            {
                                await PlayVoice(audioFile, connection, guild.Id);
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Error in voice processing: {ex}");
                        }
                    });
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Phát sinh lỗi trong quá trình dịch: {ex}");
                }
            }
        }

        private async Task<string> RunTTSScript(string message, ulong guildId)
        {
            try
            {
                string baseDirectory = AppContext.BaseDirectory;
                string outputFolder = Path.Combine(baseDirectory, "..", "..", "..", "CommunicationHistory", "VoiceHistory", guildId.ToString());
                Directory.CreateDirectory(outputFolder);

                using var httpClient = new HttpClient() { Timeout = TimeSpan.FromMinutes(5) };
                var ttsService = new TTSApi(httpClient);

                string generatedFileName = await ttsService.GenerateTTS(message, guildId.ToString(), CancellationToken.None);

                string fileName = Path.GetFileName(generatedFileName);
                string localFilePath = Path.Combine(outputFolder, fileName);

                await ttsService.DownloadGeneratedTTS(guildId, fileName, localFilePath, CancellationToken.None);

                return localFilePath;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Lỗi khi gọi API TTS: {ex.Message}");
                throw;
            }
        }

        private async Task PlayVoice(string filePath, VoiceNextConnection connection, ulong guildId, float volume = 0.5f)
        {
            var voiceLock = GetVoiceLock(guildId);
            await voiceLock.WaitAsync();
            try
            {
                await PlayAudioFileAsync(filePath, connection, volume);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error playing voice: {ex.Message}");
            }
            finally
            {
                voiceLock.Release();

                if (_audioQueues.TryGetValue(guildId, out var queue))
                {
                    if (queue.TryPeek(out var nextAudioFile) && nextAudioFile == filePath)
                    {
                        queue.TryDequeue(out _);
                    }
                }
                await ProcessNextInQueue(guildId, connection);
            }
        }

        private SemaphoreSlim GetVoiceLock(ulong guildId)
        {
            return _voiceLocks.GetOrAdd(guildId, _ => new SemaphoreSlim(1, 1));
        }

        private async Task PlayAudioFileAsync(string filePath, VoiceNextConnection connection, float volume = 1f)
        {
            if (connection == null)
                throw new InvalidOperationException("User not in voice channel");

            try
            {
                var transmitStream = connection.GetTransmitSink();
                using (var audioFile = new AudioFileReader(filePath))
                {
                    audioFile.Volume = volume;
                    var outFormat = new WaveFormat(48000, 16, 2);

                    using (var resampler = new MediaFoundationResampler(audioFile, outFormat))
                    {
                        var buffer = new byte[16384];
                        int bytesRead;

                        var bufferedProvider = new BufferedWaveProvider(outFormat)
                        {
                            BufferLength = 1048576, 
                            ReadFully = false
                        };

                        while ((bytesRead = resampler.Read(buffer, 0, buffer.Length)) > 0)
                        {
                            if (bytesRead < buffer.Length)
                            {
                                var tempBuffer = new byte[bytesRead];
                                Array.Copy(buffer, tempBuffer, bytesRead);
                                await transmitStream.WriteAsync(tempBuffer, 0, bytesRead);
                            }
                            else
                            {
                                await transmitStream.WriteAsync(buffer, 0, bytesRead);
                            }

                            await Task.Delay(60); 
                        }
                    }

                    await Task.Delay(100);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error playing audio through Discord: {ex.Message}");
                throw;
            }
        }

        private async Task ProcessNextInQueue(ulong guildId, VoiceNextConnection connection)
        {
            if (_audioQueues.TryGetValue(guildId, out var queue) && queue.Count > 0)
            {
                if (queue.TryPeek(out var nextAudioFile))
                {
                    await PlayVoice(nextAudioFile, connection, guildId);
                }
                if (queue.Count == 0)
                {
                    _audioQueues.TryRemove(guildId, out _);
                }
            }
        }
    }
}