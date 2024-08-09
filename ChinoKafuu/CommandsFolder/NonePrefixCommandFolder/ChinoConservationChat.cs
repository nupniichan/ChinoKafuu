using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using DSharpPlus;
using Python.Runtime;
using ChinoBot.config;
using DSharpPlus.VoiceNext;
using NAudio.Wave;
using System.Net.Http;
using System.Linq;
using System.Threading.Tasks;

namespace ChinoBot.CommandsFolder.NonePrefixCommandFolder
{
    internal class ChinoConservationChat
    {
        private readonly JSONreader jsonReader;
        private readonly DiscordClient _client;
        private static VoiceNextConnection connection;

        public static VoiceNextConnection Connection
        {
            get => connection;
            set => connection = value;
        }

        public ChinoConservationChat(DiscordClient client)
        {
            _client = client;
            jsonReader = new JSONreader();
            jsonReader.ReadJson().GetAwaiter().GetResult();
            _client.MessageCreated += Client_MessageCreated;
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
                using var httpClient = new HttpClient();
                using var response = await httpClient.GetAsync(attachment.Url);
                byte[] attachmentData = await response.Content.ReadAsByteArrayAsync();

                string result = await ExecuteGeminiImagePython(attachmentData);
                string translateResult = await Translator(result);
                await SendMessageAndVoiceAsync(message, result, translateResult);
            }
            catch (Exception ex)
            {
                await message.RespondAsync($"Anh ơi có lỗi trong quá trình phân tích ảnh rồi ;-; : {ex.Message}");
            }
        }

        private async Task HandleTextInputAsync(DiscordMessage message)
        {
            string chinoMessage = await ExecuteGeminiTextPython(message);

            if (message.Content.ToLower().Contains("rời voice") || message.Content.ToLower().Contains("out voice") || message.Content.ToLower().Contains("leave voice"))
            {
                connection?.Disconnect();
                await message.Channel.SendMessageAsync(chinoMessage);
                return;
            }

            try
            {
                string translateResult = await Translator(chinoMessage);
                await SendMessageAndVoiceAsync(message, chinoMessage, translateResult);
            }
            catch (Exception e)
            {
                await message.Channel.SendMessageAsync("Híc, có lỗi rồi ;-; " + e.Message);
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
                if (connection == null)
                {
                    connection = await channel.ConnectAsync();
                }

                await message.Channel.SendMessageAsync(textMessage);

                _ = Task.Run(async () =>
                {
                    await RunTTSScript(voiceMessage);
                    await PlayVoice();
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
        private async Task PlayVoice()
        {
            var resultFilePath = jsonReader.resultApplioFilePath;

            using (var audioFile = new AudioFileReader(resultFilePath))
            using (var outputDevice = new WaveOutEvent())
            {
                outputDevice.Init(audioFile);
                outputDevice.Play();

                while (outputDevice.PlaybackState == PlaybackState.Playing)
                {
                    await Task.Delay(100);
                }
            }
        }

        public async Task RunTTSScript(string message)
        {
            try
            {
                await Task.Run(() =>
                {
                    Environment.SetEnvironmentVariable("PYTHONNET_PYDLL", jsonReader.python_dll_path);
                    PythonEngine.Initialize();

                    using (Py.GIL())
                    {
                        dynamic sys = Py.Import("sys");
                        sys.path.append(jsonReader.applioPath);
                        dynamic script = Py.Import("TTSApi");
                        script.TTS(message);
                    }
                });
            }
            finally
            {
                PythonEngine.Shutdown();
            }
        }
    }
}
