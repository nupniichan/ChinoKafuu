using System.Text.Json;

public class GeminiTranslate
{
    private readonly string _apiKey;
    private readonly HttpClient _httpClient;
    private const string API_URL = "https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash:generateContent";

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
                    temperature = 0.3f,
                    topK = 40,
                    topP = 0.95f,
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
                return "Xin lỗi, hiện tại em không thể dịch được. Anh thử lại sau nhé~";
            }

            var responseData = JsonSerializer.Deserialize<JsonElement>(responseContent);
            
            if (!responseData.TryGetProperty("candidates", out var candidates) || candidates.GetArrayLength() == 0)
            {
                return "Xin lỗi, em nhận được phản hồi không hợp lệ từ server. Anh thử lại sau nhé~";
            }

            var candidate = candidates[0];
            
            if (candidate.TryGetProperty("finishReason", out var finishReason))
            {
                var reason = finishReason.GetString();
                if (reason == "SAFETY" || reason == "RECITATION" || reason == "OTHER")
                {
                    return "Xin lỗi, em không thể dịch nội dung này. Anh thử lại với nội dung khác nhé~";
                }
            }

            if (!candidate.TryGetProperty("content", out var contentProp) ||
                !contentProp.TryGetProperty("parts", out var parts) ||
                parts.GetArrayLength() == 0 ||
                !parts[0].TryGetProperty("text", out var textProp))
            {
                return "Xin lỗi, em không thể xử lý phản hồi. Anh thử lại sau nhé~";
            }

            var translatedText = textProp.GetString();

            return translatedText;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Lỗi: {ex.Message}");
            return "Xin lỗi, hiện tại em không thể dịch được. Anh thử lại sau nhé~";
        }
    }
}