using System.Text.Json;

public class GeminiTranslate
{
    private readonly string _apiKey;
    private readonly HttpClient _httpClient;
    private const string API_URL = "https://generativelanguage.googleapis.com/v1beta/models/gemini-2.0-flash:generateContent";

    public GeminiTranslate(string apiKey)
    {
        _apiKey = apiKey;
        _httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromMinutes(5)
        };
    }

    public async Task<string> Translate(string messageContent, CancellationToken cancellationToken = default)
    {
        try
        {
            var requestBody = new
            {
                contents = new[]
                {
                    new
                    {
                        role = "user",
                        parts = new[]
                        {
                            new
                            {
                                text = "Bạn là một dịch giả. Hãy dịch những gì tôi gửi sang tiếng Nhật. Đồng thời đừng dịch các đoạn trong dấu * và /n và \\n ví dụ: *cười nhẹ* thì xoá nó luôn cũng như là các emoji ví dụ như: (^▽^), (≧∇≦), (^▽^) ,v.v và các emoji của discord được sử dụng trong cặp dấu :. Chỉ cần giữ lại và dịch đoạn trò chuyện chính thôi. Chỉ trả về kết quả dịch, không cần giải thích thêm."
                            }
                        }
                    },
                    new
                    {
                        role = "model",
                        parts = new[]
                        {
                            new { text = "はい、承知しました。日本語に翻訳します。" }
                        }
                    },
                    new
                    {
                        role = "user",
                        parts = new[]
                        {
                            new { text = messageContent }
                        }
                    }
                },
                generationConfig = new
                {
                    temperature = 0.1f,
                    topK = 1,
                    topP = 1,
                    maxOutputTokens = 1024
                },
                safetySettings = new[]
                {
                    new { category = "HARM_CATEGORY_HARASSMENT", threshold = "BLOCK_NONE" },
                    new { category = "HARM_CATEGORY_HATE_SPEECH", threshold = "BLOCK_NONE" },
                    new { category = "HARM_CATEGORY_SEXUALLY_EXPLICIT", threshold = "BLOCK_NONE" },
                    new { category = "HARM_CATEGORY_DANGEROUS_CONTENT", threshold = "BLOCK_NONE" }
                }
            };

            var json = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync($"{API_URL}?key={_apiKey}", content, cancellationToken);
            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine($"Lỗi khi gọi API Google Gemini: {responseContent}");
                return "Xin lỗi, hiện tại em không thể dịch được. Anh thử lại sau nhé~";
            }

            var responseData = JsonSerializer.Deserialize<JsonElement>(responseContent);
            var translatedText = responseData.GetProperty("candidates")[0]
                                          .GetProperty("content")
                                          .GetProperty("parts")[0]
                                          .GetProperty("text")
                                          .GetString();

            return translatedText;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Lỗi: {ex.Message}");
            return "Xin lỗi, hiện tại em không thể dịch được. Anh thử lại sau nhé~";
        }
    }
}