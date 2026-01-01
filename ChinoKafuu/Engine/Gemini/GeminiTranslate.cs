using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

public class GeminiTranslate
{
    private readonly string _apiKey;
    private readonly HttpClient _httpClient;
    private const string API_URL = "https://generativelanguage.googleapis.com/v1beta/models/gemini-3-flash-preview:generateContent";

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
            string cleanText = Regex.Replace(messageContent, @"\*.*?\*", "");

            cleanText = Regex.Replace(cleanText, @":[a-zA-Z0-9_]+:", "");

            cleanText = cleanText.Replace("\n", " ").Replace("\r", " ");

            cleanText = Regex.Replace(cleanText, @"\s+", " ").Trim();

            if (string.IsNullOrWhiteSpace(cleanText))
            {
                return "";
            }

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
                                text = "Bạn là một dịch giả chuyên nghiệp. Hãy dịch câu hội thoại bên dưới sang tiếng Nhật (giọng điệu tự nhiên, giống anime/manga). " +
                                       "QUY TẮC: " +
                                       "1. Nếu vẫn còn sót lại Kaomoji (ví dụ (≧∇≦), (^^))... hãy BỎ QUA chúng, KHÔNG dịch, KHÔNG giữ lại. " +
                                       "2. Chỉ trả về kết quả dịch của lời thoại, không giải thích thêm."
                            }
                        }
                    },
                    new
                    {
                        role = "model",
                        parts = new[]
                        {
                            new { text = "はい、承知しました。" }
                        }
                    },
                    new
                    {
                        role = "user",
                        parts = new[]
                        {
                            new { text = cleanText }
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
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync($"{API_URL}?key={_apiKey}", content, cancellationToken);
            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                return "Xin lỗi, hiện tại em không thể dịch được. Anh thử lại sau nhé~";
            }

            using var doc = JsonDocument.Parse(responseContent);
            var root = doc.RootElement;

            if (!root.TryGetProperty("candidates", out var candidates) || candidates.GetArrayLength() == 0)
            {
                return "Xin lỗi, em nhận được phản hồi không hợp lệ. Anh thử lại sau nhé~";
            }

            var candidate = candidates[0];

            if (candidate.TryGetProperty("finishReason", out var finishReason))
            {
                var reason = finishReason.GetString();
                if (reason == "SAFETY" || reason == "RECITATION" || reason == "OTHER")
                {
                    return "Xin lỗi, em không thể dịch nội dung này do chính sách an toàn.";
                }
            }

            if (!candidate.TryGetProperty("content", out var contentProp) ||
                !contentProp.TryGetProperty("parts", out var parts) ||
                parts.GetArrayLength() == 0 ||
                !parts[0].TryGetProperty("text", out var textProp))
            {
                return "Xin lỗi, lỗi xử lý dữ liệu.";
            }

            var translatedText = textProp.GetString()?.Trim();

            return translatedText ?? "";
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Exception Gemini: {ex.Message}");
            return "Xin lỗi, hiện tại em không thể dịch được. Anh thử lại sau nhé~";
        }
    }
}