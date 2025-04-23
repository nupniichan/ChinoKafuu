using System.Text.Json;

public class GeminiChat
{
    private readonly string _apiKey;
    private readonly HttpClient _httpClient;
    private const int MAX_CHAT_HISTORY_LENGTH = 1000;
    private const string API_URL = "https://generativelanguage.googleapis.com/v1beta/models/gemini-2.0-flash:generateContent";
    private readonly string _prompt;

    public GeminiChat(string apiKey)
    {
        _apiKey = apiKey;
        _httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromMinutes(5)
        };
        string promptFilePath = Path.Combine(AppContext.BaseDirectory, "../../../Engine/Gemini/Prompt/prompt.txt");
        _prompt = File.ReadAllText(Path.GetFullPath(promptFilePath));
    }

    public async Task<string> RunGeminiAPI(string messageContent, string username, string chatHistoryPath, CancellationToken cancellationToken = default)
    {
        try
        {
            List<Dictionary<string, object>> chatHistory;
            try
            {
                var historyJson = await File.ReadAllTextAsync(chatHistoryPath, cancellationToken);
                chatHistory = JsonSerializer.Deserialize<List<Dictionary<string, object>>>(historyJson) ?? new List<Dictionary<string, object>>();
            }
            catch (Exception ex)
            {
                chatHistory = new List<Dictionary<string, object>>();
                Console.WriteLine(ex.Message);
            }

            var contents = new List<object>
            {
                new
                {
                    role = "user",
                    parts = new[] { new { text = _prompt } }
                },
                new
                {
                    role = "model",
                    parts = new[] { new { text = "Em hiểu rồi ạ… Em sẽ cố gắng hết sức để làm tốt vai Chino! (˶ᵔ ᵕ ᵔ˶) Anh cứ trò chuyện với em nhé!" } }
                }
            };

            foreach (var message in chatHistory)
            {
                var parts = message["parts"];
                string text;

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
                    continue;
                }

                contents.Add(new
                {
                    role = message["role"].ToString(),
                    parts = new[] { new { text = text } }
                });
            }

            DateTime currentDateTime = DateTime.Now;

            contents.Add(new
            {
                role = "user",
                parts = new[] { new { text = $"{username} ({currentDateTime}): {messageContent}" } }
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

            var response = await _httpClient.PostAsync($"{API_URL}?key={_apiKey}", content, cancellationToken);
            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                string responseMessage = response.ToString();
                Console.WriteLine(responseMessage);
                return "Xin lỗi, hiện tại em không thể trả lời được. Anh thử lại sau nhé~";
            }

            var directoryPath = Path.GetDirectoryName(chatHistoryPath);
            if (!Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }

            if (!File.Exists(chatHistoryPath))
            {
                await File.WriteAllTextAsync(chatHistoryPath, "[]", System.Text.Encoding.UTF8, cancellationToken);
            }

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

            chatHistory.Add(new Dictionary<string, object>
        {
            { "role", "user" },
            { "parts", new[] { $"{username} ({currentDateTime}): {messageContent}" } }
        });

            chatHistory.Add(new Dictionary<string, object>
        {
            { "role", "model" },
            { "parts", new[] { modelResponse } }
        });

            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            };

            var serializedHistory = JsonSerializer.Serialize(chatHistory, options);
            await File.WriteAllTextAsync(chatHistoryPath, serializedHistory, cancellationToken);

            return modelResponse != null ? modelResponse : "Có lỗi khi gọi Api đến Gemini";
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ERROR] Lỗi: {ex.Message}");
            Console.WriteLine($"[ERROR] Stack trace: {ex.StackTrace}");
            return "Xin lỗi, hiện tại em không thể trả lời được. Anh thử lại sau nhé~";
        }
    }
}
