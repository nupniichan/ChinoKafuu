using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using DSharpPlus;
using Python.Runtime;
using ChinoBot.config;

namespace ChinoBot.CommandsFolder.NonePrefixCommandFolder
{
    internal class ChinoConservationChat
    {
        private readonly JSONreader jsonReader;
        private readonly DiscordClient _client;
        public ChinoConservationChat(DiscordClient client)
        {
            _client = client;
            jsonReader = new JSONreader();
            jsonReader.ReadJson().GetAwaiter().GetResult();
            _client.MessageCreated += Client_MessageCreated;
        }

        private async Task Client_MessageCreated(DiscordClient sender, MessageCreateEventArgs e)
        {
            if (!e.Author.IsBot && !e.Message.Content.StartsWith("BOT") && e.Channel.Id == jsonReader.allowChannelID_gemini)
            {
                await e.Channel.TriggerTypingAsync();

                if (e.Message.Attachments.Any())
                {
                    await HandleImageInputAsync(e.Message);
                }
                else
                {
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

                        await message.RespondAsync(result);
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
            await message.Channel.SendMessageAsync(chinoMessage);
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
                        dynamic convo = script.convo;
                        string messageContent = message.Content;
                        dynamic response = convo.send_message(username + ": " + messageContent);
                        chinoMessage = convo.last.text;
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
    }
}
