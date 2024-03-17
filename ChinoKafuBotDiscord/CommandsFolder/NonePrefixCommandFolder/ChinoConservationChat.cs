﻿using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using DSharpPlus;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Python.Runtime;
using static GraphQL.Validation.Rules.OverlappingFieldsCanBeMerged;

namespace ChinoBot.CommandsFolder.NonePrefixCommandFolder
{
    internal class ChinoConservationChat
    {
        private readonly DiscordClient _client;
        // change this one
        private readonly ulong allowChannelID = 12345678; 
        // private readonly ulong _anotherChannelId = ...;
        public ChinoConservationChat(DiscordClient client)
        {
            _client = client;
            _client.MessageCreated += Client_MessageCreated;
        }

        private async Task Client_MessageCreated(DiscordClient sender, MessageCreateEventArgs e)
        {
            if (!e.Author.IsBot && !e.Message.Content.StartsWith("BOT") && e.Channel.Id == allowChannelID)
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

                        string result = await ExecuteGeminiVisionPython(attachmentData);

                        await message.RespondAsync(result);
                    }
                }
            }
            catch (Exception ex)
            {
                await message.RespondAsync($"Anh ơi có lỗi trong quá trình phân tích ảnh rồi ;-; : {ex.Message}");
            }
        }

        // still fixing ...
        private async Task<string> ExecuteGeminiVisionPython(byte[] attachmentData)
        {
            string chinoMessage = "";

            try
            {
                Environment.SetEnvironmentVariable("PYTHONNET_PYDLL", "C:\\Users\\nup\\AppData\\Local\\Programs\\Python\\Python39\\python39.dll");
                PythonEngine.Initialize();

                dynamic sys = Py.Import("sys");
                dynamic io = Py.Import("io");

                dynamic bytesIO = io.BytesIO(attachmentData);

                using (Py.GIL())
                {
                    sys.path.append(@"G:\Programming\Discord\ChinoKafu\ChinoKafuBotDiscord\Engine\Gemini");
                    dynamic script = Py.Import("GeminiVision");

                    dynamic convo = script.convo;
                    dynamic img = script.PIL.Image.open(bytesIO); 

                    dynamic model = script.genai.GenerativeModel("gemini-pro-vision");
                    dynamic response = model.generate_content(img);

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

        private async Task HandleTextInputAsync(DiscordMessage message)
        {
            string chinoMessage = await ExecuteGeminiTextPython(message);
            await message.Channel.SendMessageAsync(chinoMessage);
        }

        private async Task<string> ExecuteGeminiTextPython(DiscordMessage message)
        {
            string chinoMessage = "";

            try
            {
                await Task.Run(() =>
                {
                    // Your python environment path: example: "C:\\Users\\{YourUser}\\AppData\\Local\\Programs\\Python\\Python39\\python39.dll
                    Environment.SetEnvironmentVariable("PYTHONNET_PYDLL", "C:\\Users\\nup\\AppData\\Local\\Programs\\Python\\Python39\\python39.dll");
                    PythonEngine.Initialize();
                    using (Py.GIL())
                    {
                        dynamic sys = Py.Import("sys");
                        sys.path.append(@"G:\Programming\Discord\ChinoKafu\ChinoKafuBotDiscord\Engine\Gemini");

                        dynamic script = Py.Import("GeminiText");
                        dynamic convo = script.convo;
                        string messageContent = message.Content;
                        dynamic response = convo.send_message(messageContent);
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
