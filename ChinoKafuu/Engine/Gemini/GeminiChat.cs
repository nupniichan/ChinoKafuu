using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.IO;
using System.Collections.Generic;

public class GeminiChat
{
    private readonly string _apiKey;
    private readonly HttpClient _httpClient;
    private const int MAX_CHAT_HISTORY_LENGTH = 500;
    private const string API_URL = "https://generativelanguage.googleapis.com/v1beta/models/gemini-1.5-flash:generateContent";
    private readonly string _prompt;

    public GeminiChat(string apiKey)
    {
        _apiKey = apiKey;
        _httpClient = new HttpClient();
        _prompt = File.ReadAllText("../../../Engine/Gemini/Prompt/prompt.txt");
    }

    public async Task<string> RunGeminiAPI(string messageContent, string username, string chatHistoryPath)
    {
        try
        {
            // Đọc chat history trước
            List<Dictionary<string, object>> chatHistory;
            try
            {
                var historyJson = await File.ReadAllTextAsync(chatHistoryPath);
                chatHistory = JsonSerializer.Deserialize<List<Dictionary<string, object>>>(historyJson) ?? new List<Dictionary<string, object>>();
            }
            catch (Exception ex)
            {
                chatHistory = new List<Dictionary<string, object>>();
            }

            // Tạo contents list với prompt đầu tiên
            var contents = new List<object>
            {
                new
                {
                    role = "user",
                    parts = new[]
                    {
                        new { text = _prompt }
                    }
                }
            };

            // Thêm chat history vào contents
            foreach (var message in chatHistory)
            {
                var parts = message["parts"];
                string text;
                
                // Xử lý parts tùy thuộc vào kiểu dữ liệu
                if (parts is JsonElement jsonElement)
                {
                    text = jsonElement[0].GetString();
                }
                else if (parts is string[] stringArray)
                {
                    text = stringArray[0];
                }
                else
                {
                    continue; // Bỏ qua nếu không xử lý được
                }

                Console.WriteLine($"[DEBUG] Message parts type: {message["parts"].GetType()}");
                Console.WriteLine($"[DEBUG] Message content: {JsonSerializer.Serialize(message)}");

                contents.Add(new
                {
                    role = message["role"].ToString(),
                    parts = new[]
                    {
                        new { text = text }
                    }
                });
            }

            // Thêm tin nhắn hiện tại
            contents.Add(new
            {
                role = "user",
                parts = new[]
                {
                    new { text = $"{username}: {messageContent}" }
                }
            });

            var requestBody = new
            {
                contents = contents,
                generationConfig = new
                {
                    temperature = 1.2f,
                    topK = 1,
                    topP = 1,
                    maxOutputTokens = 512,
                    responseMimeType = "text/plain"
                }
            };

            var json = JsonSerializer.Serialize(requestBody);

            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync($"{API_URL}?key={_apiKey}", content);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                return "Xin lỗi, hiện tại em không thể trả lời được. Anh thử lại sau nhé~";
            }

            // Kiểm tra và tạo thư mục nếu chưa tồn tại
            var directoryPath = Path.GetDirectoryName(chatHistoryPath);
            if (!Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }

            // Kiểm tra và tạo file nếu chưa tồn tại
            if (!File.Exists(chatHistoryPath))
            {
                await File.WriteAllTextAsync(chatHistoryPath, "[]", System.Text.Encoding.UTF8);
            }

            // Giới hạn độ dài chat history
            if (chatHistory.Count > MAX_CHAT_HISTORY_LENGTH)
            {
                chatHistory = chatHistory.GetRange(chatHistory.Count - MAX_CHAT_HISTORY_LENGTH, MAX_CHAT_HISTORY_LENGTH);
            }

            var responseData = JsonSerializer.Deserialize<JsonElement>(responseContent);
            var modelResponse = responseData.GetProperty("candidates")[0]
                                         .GetProperty("content")
                                         .GetProperty("parts")[0]
                                         .GetProperty("text")
                                         .GetString();
            // Thêm tin nhắn mới vào history với định dạng mong muốn
            chatHistory.Add(new Dictionary<string, object>
            {
                { "role", "user" },
                { "parts", new[] { $"{username}: {messageContent}" } }
            });

            chatHistory.Add(new Dictionary<string, object>
            {
                { "role", "model" },
                { "parts", new[] { modelResponse } }
            });

            // Ghi lại chat history
            var options = new JsonSerializerOptions 
            { 
                WriteIndented = true,
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            };

            var serializedHistory = JsonSerializer.Serialize(chatHistory, options);
            await File.WriteAllTextAsync(chatHistoryPath, serializedHistory);

            return modelResponse;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ERROR] Lỗi: {ex.Message}");
            Console.WriteLine($"[ERROR] Stack trace: {ex.StackTrace}");
            return "Xin lỗi, hiện tại em không thể trả lời được. Anh thử lại sau nhé~";
        }
    }
}