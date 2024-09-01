using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using DSharpPlus;
using Python.Runtime;
using ChinoBot.config;
using DSharpPlus.VoiceNext;
using NAudio.Wave;
using System.Collections.Concurrent;

namespace ChinoBot.CommandsFolder.NonePrefixCommandFolder
{
    internal class ChinoConversation
    {
        private readonly JSONreader jsonReader;
        private readonly DiscordClient _client;
        private readonly ConcurrentDictionary<ulong, VoiceNextConnection> _connections = new ConcurrentDictionary<ulong, VoiceNextConnection>();
        private readonly SemaphoreSlim _pythonLock = new SemaphoreSlim(1, 1);
        private readonly ConcurrentDictionary<ulong, SemaphoreSlim> _voiceLocks = new ConcurrentDictionary<ulong, SemaphoreSlim>();
        private readonly ConcurrentDictionary<ulong, Queue<string>> _audioQueues = new ConcurrentDictionary<ulong, Queue<string>>();
        private readonly HttpClient _httpClient = new HttpClient();

        public ChinoConversation(DiscordClient client)
        {
            _client = client;
            jsonReader = new JSONreader();
            jsonReader.ReadJson().GetAwaiter().GetResult();
            _client.MessageCreated += Client_MessageCreated;
        }

        public bool TryRemoveConnection(ulong guildId, out VoiceNextConnection connection)
        {
            return _connections.TryRemove(guildId, out connection);
        }
        private async Task Client_MessageCreated(DiscordClient sender, MessageCreateEventArgs e)
        {
            if (e.Author.IsBot || e.Message.Content.StartsWith("BOT") ||
                (e.Channel.Id != 1140906898779017268 && e.Channel.Id != jsonReader.allowChannelID_gemini))
            {
                return;
            }

            await e.Channel.TriggerTypingAsync();

            _ = Task.Run(async () =>
            {
                if (e.Message.Attachments.Any())
                {
                    await HandleImageInputAsync(e.Message);
                }
                else
                {
                    await HandleTextInputAsync(e.Message);
                }
            });
        }

        private async Task HandleImageInputAsync(DiscordMessage message)
        {
            var attachment = message.Attachments.FirstOrDefault();

            try
            {
                using var response = await _httpClient.GetAsync(attachment.Url);
                byte[] attachmentData = await response.Content.ReadAsByteArrayAsync();

                await _pythonLock.WaitAsync();
                try
                {
                    string result = await ExecuteGeminiImagePython(attachmentData);
                    string translateResult = await Translator(result);
                    await SendMessageAndVoiceAsync(message, result, translateResult);
                }
                finally
                {
                    _pythonLock.Release();
                }
            }
            catch (Exception ex)
            {
                await message.RespondAsync($"Anh ơi có lỗi trong quá trình phân tích ảnh rồi ;-; : {ex.Message}");
            }
        }

        private async Task HandleTextInputAsync(DiscordMessage message)
        {
            await _pythonLock.WaitAsync();
            try
            {
                string chinoMessage = await ExecuteGeminiTextPython(message);

                if (message.Content.ToLower().Contains("rời voice") || message.Content.ToLower().Contains("out voice") || message.Content.ToLower().Contains("leave voice"))
                {
                    if (_connections.TryRemove(message.Channel.GuildId.Value, out var connection))
                    {
                        connection.Disconnect();
                    }
                    await message.Channel.SendMessageAsync(chinoMessage);
                    return;
                }

                string translateResult = await Translator(chinoMessage);
                await SendMessageAndVoiceAsync(message, chinoMessage, translateResult);
            }
            catch (Exception e)
            {
                await message.Channel.SendMessageAsync("Híc, có lỗi rồi ;-; " + e.Message);
            }
            finally
            {
                _pythonLock.Release();
            }
        }

        private async Task<string> ExecuteGeminiTextPython(DiscordMessage message)
        {
            string username = message.Author.Username;
            var member = await message.Channel.Guild.GetMemberAsync(message.Author.Id);

            if (member != null && !string.IsNullOrEmpty(member.Nickname))
            {
                username = member.Nickname;
            }

            try
            {
                return await Task.Run(() =>
                {
                    Environment.SetEnvironmentVariable("PYTHONNET_PYDLL", jsonReader.python_dll_path);
                    PythonEngine.Initialize();

                    using (Py.GIL())
                    {
                        dynamic sys = Py.Import("sys");
                        sys.path.append(jsonReader.gemini_folder_path);
                        dynamic script = Py.Import("Gemini");
                        return script.RunGeminiAPI(jsonReader.geminiAPIKey, message.Content, username);
                    }
                });
            }
            finally
            {
                PythonEngine.Shutdown();
            }
        }

        private async Task<string> Translator(string messageContent)
        {
            try
            {
                return await Task.Run(() =>
                {
                    Environment.SetEnvironmentVariable("PYTHONNET_PYDLL", jsonReader.python_dll_path);
                    PythonEngine.Initialize();

                    using (Py.GIL())
                    {
                        dynamic sys = Py.Import("sys");
                        sys.path.append(jsonReader.gemini_folder_path);
                        dynamic script = Py.Import("GeminiTranslate");
                        return script.GeminiTranslate(jsonReader.geminiTranslateAPIKey, messageContent);
                    }
                });
            }
            finally
            {
                PythonEngine.Shutdown();
            }
        }

        private async Task SendMessageAndVoiceAsync(DiscordMessage message, string textMessage, string voiceMessage)
        {
            var guild = message.Channel.Guild;
            var member = await guild.GetMemberAsync(message.Author.Id);
            var voiceState = member?.VoiceState;
            var channel = voiceState?.Channel;

            if (channel != null)
            {
                if (!_connections.TryGetValue(guild.Id, out var connection))
                {
                    connection = await channel.ConnectAsync();
                    _connections[guild.Id] = connection;
                }

                await message.Channel.SendMessageAsync(textMessage);

                _ = Task.Run(async () =>
                {
                    string audioFile = await RunTTSScript(voiceMessage, guild.Id);

                    if (!_audioQueues.TryGetValue(guild.Id, out var queue))
                    {
                        queue = new Queue<string>();
                        _audioQueues[guild.Id] = queue;
                    }

                    queue.Enqueue(audioFile);

                    // Start playing if nothing is currently playing
                    if (queue.Count == 1)
                    {
                        string resultFilePath = jsonReader.resultAudioFilePath + audioFile;
                        await PlayVoice(resultFilePath, connection, guild.Id);
                    }
                });
            }
            else
            {
                await message.Channel.SendMessageAsync(textMessage);
            }
        }

        private async Task<string> ExecuteGeminiImagePython(byte[] attachmentData)
        {
            try
            {
                return await Task.Run(() =>
                {
                    Environment.SetEnvironmentVariable("PYTHONNET_PYDLL", jsonReader.python_dll_path);
                    PythonEngine.Initialize();

                    using (Py.GIL())
                    {
                        dynamic sys = Py.Import("sys");
                        dynamic io = Py.Import("io");
                        sys.path.append(jsonReader.gemini_folder_path);

                        dynamic bytesIO = io.BytesIO(attachmentData);
                        dynamic script = Py.Import("Gemini");
                        dynamic convo = script.convo;
                        dynamic img = script.PIL.Image.open(bytesIO);
                        dynamic response = convo.send_message(img);

                        return response.text;
                    }
                });
            }
            catch (Exception ex)
            {
                return $"Anh ơi có lỗi rồi nè~ : {ex.Message}";
            }
            finally
            {
                PythonEngine.Shutdown();
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
                ProcessNextInQueue(guildId, connection); 
            }
        }

        private SemaphoreSlim GetVoiceLock(ulong guildId)
        {
            if (!_voiceLocks.TryGetValue(guildId, out var voiceLock))
            {
                voiceLock = new SemaphoreSlim(1, 1);
                _voiceLocks[guildId] = voiceLock;
            }
            return voiceLock;
        }

        private async Task PlayAudioFileAsync(string filePath, VoiceNextConnection connection, float volume)
        {
            using var audioFile = new AudioFileReader(filePath);
            using var volumeStream = new WaveChannel32(audioFile) { Volume = volume };
            using var resampler = new MediaFoundationResampler(volumeStream, new WaveFormat(48000, 16, 2))
            {
                ResamplerQuality = 60
            };

            var buffer = new byte[resampler.WaveFormat.AverageBytesPerSecond / 25]; 
            var transmitStream = connection.GetTransmitSink();

            int bytesRead;
            while ((bytesRead = resampler.Read(buffer, 0, buffer.Length)) > 0)
            {
                await transmitStream.WriteAsync(buffer, 0, bytesRead);
                await Task.Delay(20);
            }

            await transmitStream.FlushAsync();
        }

        private void ProcessNextInQueue(ulong guildId, VoiceNextConnection connection)
        {
            if (_audioQueues.TryGetValue(guildId, out var queue) && queue.TryDequeue(out var nextAudioFile))
            {
                _ = PlayVoice(nextAudioFile, connection, guildId);
            }
        }

        private async Task<string> RunTTSScript(string message, ulong guildId)
        {
            string audioFile = $"result_{guildId}_{DateTime.Now.Ticks}.wav";
            await _pythonLock.WaitAsync();
            try
            {
                Environment.SetEnvironmentVariable("PYTHONNET_PYDLL", jsonReader.python_dll_path);
                PythonEngine.Initialize();

                using (Py.GIL())
                {
                    dynamic sys = Py.Import("sys");
                    sys.path.append(jsonReader.applioPath);
                    dynamic script = Py.Import("TTSApi");
                    script.TTS(message, jsonReader.resultAudioFilePath ,audioFile);
                }
                return audioFile;
            }
            finally
            {
                PythonEngine.Shutdown();
                _pythonLock.Release();
            }
        }
    }
}
