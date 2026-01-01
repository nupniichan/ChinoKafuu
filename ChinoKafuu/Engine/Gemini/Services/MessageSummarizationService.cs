using System.Text;
using System.Text.Json;
using ChinoKafuu.Engine.Gemini.Config;
using ChinoKafuu.Engine.Gemini.Models;

namespace ChinoKafuu.Engine.Gemini.Services;

public class MessageSummarizationService
{
    private readonly string _apiKey;
    private readonly HttpClient _httpClient;
    private readonly GeminiConfig _config;
    private readonly TokenManagementService _tokenService;
    private const string API_URL = "https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash:generateContent";
    
    public MessageSummarizationService(
        string apiKey, 
        GeminiConfig? config = null,
        TokenManagementService? tokenService = null)
    {
        _apiKey = apiKey;
        _httpClient = new HttpClient { Timeout = TimeSpan.FromMinutes(2) };
        _config = config ?? GeminiConfig.Instance;
        _tokenService = tokenService ?? new TokenManagementService(_config);
    }
    
    public async Task<ChatSession> SummarizeOldMessages(
        ChatSession session, 
        CancellationToken cancellationToken = default)
    {
        if (!_config.EnableAutoSummarization)
            return session;
        
        if (session.Messages.Count <= _config.SummarizationThreshold)
            return session;
        var recentMessages = session.Messages
            .TakeLast(_config.PostSummarizationTarget)
            .ToList();
        
        var oldMessages = session.Messages
            .Take(session.Messages.Count - _config.PostSummarizationTarget)
            .Where(m => !m.IsSummarized)
            .Where(m => m.ImportanceScore < _config.MinImportanceToPreserveSummarization)
            .ToList();
        
        if (oldMessages.Count < _config.SummarizationBatchSize)
        {
            return session;
        }
        
        var summaries = new List<ChatMessage>();
        int totalSummarized = 0;
        
        for (int i = 0; i < oldMessages.Count; i += _config.SummarizationBatchSize)
        {
            var batch = oldMessages.Skip(i).Take(_config.SummarizationBatchSize).ToList();
            try
            {
                var summary = await SummarizeBatch(batch, cancellationToken);
                if (summary != null)
                {
                    summaries.Add(summary);
                    totalSummarized += batch.Count;
                }
            }
            catch { }
        }
        
        var importantOldMessages = session.Messages
            .Take(session.Messages.Count - _config.PostSummarizationTarget)
            .Where(m => m.ImportanceScore >= _config.MinImportanceToPreserveSummarization || m.IsSummarized)
            .ToList();
        
        session.Messages = importantOldMessages.Concat(summaries).Concat(recentMessages).ToList();
        session.SummarizedMessagesCount = totalSummarized;
        session.LastSummarizedAt = DateTime.Now;
        return session;
    }
    
    private async Task<ChatMessage?> SummarizeBatch(
        List<ChatMessage> messages, 
        CancellationToken cancellationToken = default)
    {
        if (messages.Count == 0)
            return null;
        
        var conversationText = new StringBuilder();
        conversationText.AppendLine("Hãy tóm tắt đoạn hội thoại sau đây một cách ngắn gọn nhưng giữ lại những thông tin quan trọng:");
        conversationText.AppendLine();
        
        foreach (var msg in messages)
        {
            var role = msg.Role == "user" ? "Người dùng" : "Chino";
            var content = string.Join(" ", msg.Parts);
            conversationText.AppendLine($"{role} ({msg.Timestamp:dd/MM/yyyy HH:mm}): {content}");
        }
        
        conversationText.AppendLine();
        conversationText.AppendLine("Tóm tắt ngắn gọn (2-3 câu) nội dung chính và bối cảnh của đoạn hội thoại:");
        
        var requestBody = new
        {
            contents = new[]
            {
                new
                {
                    role = "user",
                    parts = new[] { new { text = conversationText.ToString() } }
                }
            },
            generationConfig = new
            {
                temperature = 0.3f,
                topK = 20,
                topP = 0.8f,
                maxOutputTokens = 500,
                responseMimeType = "text/plain"
            }
        };
        
        try
        {
            var json = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            
            var response = await _httpClient.PostAsync($"{API_URL}?key={_apiKey}", content, cancellationToken);
            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
            
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }
            
            var responseData = JsonSerializer.Deserialize<JsonElement>(responseContent);
            
            if (!responseData.TryGetProperty("candidates", out var candidates) || 
                candidates.GetArrayLength() == 0)
                return null;
            
            var candidate = candidates[0];
            
            if (!candidate.TryGetProperty("content", out var contentProp) ||
                !contentProp.TryGetProperty("parts", out var parts) ||
                parts.GetArrayLength() == 0 ||
                !parts[0].TryGetProperty("text", out var textProp))
                return null;
            
            var summaryText = textProp.GetString();
            if (string.IsNullOrWhiteSpace(summaryText))
                return null;
            
            var summary = new ChatMessage
            {
                Role = "model",
                Parts = new[] { $"[TÓM TẮT {messages.Count} tin nhắn từ {messages.First().Timestamp:dd/MM HH:mm} đến {messages.Last().Timestamp:dd/MM HH:mm}]: {summaryText}" },
                Timestamp = messages.Last().Timestamp,
                IsSummarized = true,
                ImportanceScore = 8,
                StorageTier = "warm",
                Metadata = new Dictionary<string, object>
                {
                    { "originalMessageCount", messages.Count },
                    { "summarizedAt", DateTime.Now },
                    { "timeRange", $"{messages.First().Timestamp:dd/MM/yyyy HH:mm} - {messages.Last().Timestamp:dd/MM/yyyy HH:mm}" }
                }
            };
            
            int originalTokens = messages.Sum(m => _tokenService.EstimateMessageTokens(m));
            int summaryTokens = _tokenService.EstimateMessageTokens(summary);
            
            summary.Metadata["originalTokenCount"] = originalTokens;
            summary.Metadata["tokensSaved"] = originalTokens - summaryTokens;
            summary.Metadata["compressionRatio"] = (double)summaryTokens / originalTokens;
            if (_config.KeepOriginalMessages)
            {
                summary.OriginalContent = JsonSerializer.Serialize(messages);
            }
            
            return summary;
        }
        catch
        {
            return null;
        }
    }
    
    public bool ShouldSummarize(ChatSession session)
    {
        return _config.ShouldTriggerSummarization(session.Messages.Count);
    }
    
    public SummarizationStats GetSummarizationStats(ChatSession session)
    {
        var stats = new SummarizationStats
        {
            TotalMessages = session.Messages.Count,
            SummarizedMessages = session.Messages.Count(m => m.IsSummarized),
            LastSummarizedAt = session.LastSummarizedAt
        };
        
        var summarizedMsgs = session.Messages.Where(m => m.IsSummarized).ToList();
        
        if (summarizedMsgs.Count > 0)
        {
            stats.OriginalMessageCount = summarizedMsgs
                .Sum(m => m.Metadata.TryGetValue("originalMessageCount", out var count) ? Convert.ToInt32(count) : 0);
            
            stats.TokensSaved = summarizedMsgs
                .Sum(m => m.Metadata.TryGetValue("tokensSaved", out var saved) ? Convert.ToInt32(saved) : 0);
            
            stats.AverageCompressionRatio = summarizedMsgs
                .Average(m => m.Metadata.TryGetValue("compressionRatio", out var ratio) ? Convert.ToDouble(ratio) : 1.0);
        }
        
        return stats;
    }
    
    public List<ChatMessage>? RestoreOriginalMessages(ChatMessage summaryMessage)
    {
        if (!summaryMessage.IsSummarized || string.IsNullOrEmpty(summaryMessage.OriginalContent))
            return null;
        
        try
        {
            return JsonSerializer.Deserialize<List<ChatMessage>>(summaryMessage.OriginalContent);
        }
        catch
        {
            return null;
        }
    }
}

public class SummarizationStats
{
    public int TotalMessages { get; set; }
    public int SummarizedMessages { get; set; }
    public int OriginalMessageCount { get; set; }
    public int TokensSaved { get; set; }
    public double AverageCompressionRatio { get; set; }
    public DateTime? LastSummarizedAt { get; set; }
    
    public override string ToString()
    {
        return $"Total: {TotalMessages} msgs | " +
               $"Summarized: {SummarizedMessages} summaries (from {OriginalMessageCount} original msgs) | " +
               $"Tokens saved: {TokensSaved:N0} | " +
               $"Avg compression: {AverageCompressionRatio * 100:F1}% | " +
               $"Last: {LastSummarizedAt?.ToString("dd/MM/yyyy HH:mm") ?? "Never"}";
    }
}
