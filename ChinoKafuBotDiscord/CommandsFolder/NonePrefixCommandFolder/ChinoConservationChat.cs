using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using DSharpPlus;
using Python.Runtime;
using ChinoBot.config;
using System.Diagnostics;
using DSharpPlus.VoiceNext;
using static GraphQL.Validation.Rules.OverlappingFieldsCanBeMerged;
using static System.Net.Mime.MediaTypeNames;

namespace ChinoBot.CommandsFolder.NonePrefixCommandFolder
{
    internal class ChinoConservationChat
    {
        private readonly JSONreader jsonReader;
        private readonly DiscordClient _client;
        private static VoiceNextConnection connection;
        public static VoiceNextConnection Connection
        {
            get { return connection; }
            set { connection = value; }
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
            if (!e.Author.IsBot && !e.Message.Content.StartsWith("BOT"))
            {
                if (e.Channel.Id == 1140906898779017268 || e.Channel.Id == jsonReader.allowChannelID_gemini)
                {
                    await e.Channel.TriggerTypingAsync();

                    if (e.Message.Attachments.Any())
                        await HandleImageInputAsync(e.Message);
                    else
                        await HandleTextInputAsync(e.Message);
                }
            }
        }
        private async Task<string> ExecuteGeminiImagePython(byte[] attachmentData)
        {
            string chinoMessage = "";
            try
            {
                Environment.SetEnvironmentVariable("PYTHONNET_PYDLL", $"{jsonReader.python_dll_path}");
                PythonEngine.Initialize();

                dynamic sys = Py.Import("sys");
                dynamic io = Py.Import("io");

                dynamic bytesIO = io.BytesIO(attachmentData);

                using (Py.GIL())
                {
                    sys.path.append($"{jsonReader.gemini_folder_path}");
                    dynamic script = Py.Import("Gemini");

                    dynamic convo = script.convo;
                    dynamic img = script.PIL.Image.open(bytesIO);

                    dynamic response = convo.send_message(img);

                    chinoMessage = response.text;
                }
            }
            catch (Exception ex)
            {
                return $"Anh ơi có lỗi rồi nè~ : {ex.Message}";
            }
            finally
            {
                PythonEngine.Shutdown();
            }

            return chinoMessage;
        }
        private async Task HandleImageInputAsync(DiscordMessage message)
        {
            var attachment = message.Attachments.FirstOrDefault();

            try
            {
                using (var httpClient = new HttpClient())
                {
                    using (var response = await httpClient.GetAsync(attachment.Url))
                    {
                        byte[] attachmentData = await response.Content.ReadAsByteArrayAsync();

                        string result = await ExecuteGeminiImagePython(attachmentData);

                        var guild = message.Channel.Guild;
                        var member = await guild.GetMemberAsync(message.Author.Id);
                        var voiceState = member?.VoiceState;
                        var channel = voiceState?.Channel;

                        string translateResult = await Translator(result);
                        if (channel != null)
                        {
                            if (connection == null)
                            {
                                connection = await channel.ConnectAsync();
                                await message.RespondAsync(result);
                                await RunTTSScript(translateResult);
                                await PlayVoice();
                            }
                            else
                            {
                                await message.RespondAsync(result);
                                await RunTTSScript(translateResult);
                                await PlayVoice();
                            }
                        }
                        else
                        {
                            await message.Channel.SendMessageAsync(result);
                        }
                    }
                }
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
                connection.Disconnect();
                await message.Channel.SendMessageAsync(chinoMessage);
                return;
            }
            try
            {
                var guild = message.Channel.Guild;
                var member = await guild.GetMemberAsync(message.Author.Id); 
                var voiceState = member?.VoiceState;
                var channel = voiceState?.Channel;

                if (channel != null)
                {
                    string translateResult = await Translator(chinoMessage);
                    if (connection == null)
                    {
                        connection = await channel.ConnectAsync();
                        await RunTTSScript(translateResult);
                        await message.Channel.SendMessageAsync(chinoMessage);
                        await PlayVoice();
                    }
                    else
                    {
                        await RunTTSScript(translateResult);
                        await message.Channel.SendMessageAsync(chinoMessage);
                        await PlayVoice();
                    }
                }
                else
                {
                    await message.Channel.SendMessageAsync(chinoMessage);
                }
            }
            catch (Exception e)
            {
                await message.Channel.SendMessageAsync("Híc, có lỗi rồi ;-; " +e.Message);
            }
        }

        private async Task<string> ExecuteGeminiTextPython(DiscordMessage message)
        {
            string chinoMessage = "";
            string username = message.Author.Username;
            var member = await message.Channel.Guild.GetMemberAsync(message.Author.Id);
            if (member != null && !string.IsNullOrEmpty(member.Nickname))
            {
                username = member.Nickname;
            }
            try
            {
                await Task.Run(() =>
                {
                    Environment.SetEnvironmentVariable("PYTHONNET_PYDLL", $"{jsonReader.python_dll_path}");
                    PythonEngine.Initialize();
                    using (Py.GIL())
                    {
                        dynamic sys = Py.Import("sys");
                        sys.path.append($"{jsonReader.gemini_folder_path}");

                        dynamic script = Py.Import("Gemini");
                        dynamic response = script.RunGeminiAPI(jsonReader.geminiAPIKey, message.Content, username);
                        chinoMessage = response;
                    }
                });
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
            finally
            {
                PythonEngine.Shutdown();
            }
            return chinoMessage;
        }
        private async Task<string> Translator(string messageContent)
        {
            string result = "";
            try
            {
                await Task.Run(() =>
                {
                    Environment.SetEnvironmentVariable("PYTHONNET_PYDLL", $"{jsonReader.python_dll_path}");
                    PythonEngine.Initialize();
                    using (Py.GIL())
                    {
                        dynamic sys = Py.Import("sys");
                        sys.path.append($"{jsonReader.gemini_folder_path}");

                        dynamic script = Py.Import("GeminiTranslate");
                        dynamic response = script.GeminiTranslate(jsonReader.geminiTranslateAPIKey, messageContent);
                        result = response;
                    }
                });
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
            finally
            {
                PythonEngine.Shutdown();
            }
            return result;
        }
        private async Task PlayVoice()
        {

            var resultFilePath = $"{jsonReader.resultApplioFilePath}";
            var ffmpegPath = $"{jsonReader.ffmpegPath}";

            var ffmpeg = Process.Start(new ProcessStartInfo
            {
                FileName = ffmpegPath,
                Arguments = $@"-i ""{resultFilePath}"" -ac 2 -f s16le -ar 48000 pipe:1",
                RedirectStandardOutput = true,
                UseShellExecute = false
            });
            using var pcm = ffmpeg.StandardOutput.BaseStream;
            var transmit = connection.GetTransmitSink();
            await pcm.CopyToAsync(transmit);
        }
        public async Task RunTTSScript(string message)
        {
            try
            {
                await Task.Run(() =>
                {
                    Environment.SetEnvironmentVariable("PYTHONNET_PYDLL", $"{jsonReader.python_dll_path}");
                    PythonEngine.Initialize();
                    using (Py.GIL())
                    {
                        dynamic sys = Py.Import("sys");
                        sys.path.append($"{jsonReader.applioPath}");

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
