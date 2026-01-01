using ChinoKafuu.Engine.Gemini.Config;
using ChinoKafuu.Engine.Gemini.Models;

namespace ChinoKafuu.Engine.Gemini.Services;

public class TokenManagementService
{
    private readonly GeminiConfig _config;
    
    public TokenManagementService(GeminiConfig? config = null)
    {
        _config = config ?? GeminiConfig.Instance;
    }
    
    public int EstimateMessageTokens(ChatMessage message)
    {
        if (message.EstimatedTokenCount > 0)
            return message.EstimatedTokenCount;
        
        int totalTokens = 0;
        foreach (var part in message.Parts)
        {
            totalTokens += _config.EstimateTokens(part);
        }
        
        totalTokens += 20;
        
        message.EstimatedTokenCount = totalTokens;
        return totalTokens;
    }
    
    public int EstimateTotalTokens(List<ChatMessage> messages)
    {
        return messages.Sum(EstimateMessageTokens);
    }
    
    public List<ChatMessage> GetOptimalMessagesForAPI(ChatSession session)
    {
        var allMessages = session.Messages;
        if (allMessages.Count == 0)
            return new List<ChatMessage>();
        
        var recentMessages = allMessages.TakeLast(_config.MaxActiveMessages).ToList();
        int currentTokens = EstimateTotalTokens(recentMessages);
        
        if (currentTokens < _config.MaxTokensPerRequest)
        {
            var olderMessages = allMessages
                .Take(allMessages.Count - _config.MaxActiveMessages)
                .Where(m => m.ImportanceScore >= _config.MinImportanceToPreserveSummarization)
                .OrderByDescending(m => m.ImportanceScore);
            
            foreach (var msg in olderMessages)
            {
                int msgTokens = EstimateMessageTokens(msg);
                if (currentTokens + msgTokens >= _config.MaxTokensPerRequest)
                    break;
                recentMessages.Insert(0, msg);
                currentTokens += msgTokens;
            }
        }
        
        return currentTokens > _config.MaxTokensPerRequest 
            ? TrimToTokenLimit(recentMessages, _config.MaxTokensPerRequest)
            : recentMessages;
    }
    
    private List<ChatMessage> TrimToTokenLimit(List<ChatMessage> messages, int maxTokens)
    {
        var result = new List<ChatMessage>();
        int currentTokens = 0;
        
        for (int i = messages.Count - 1; i >= 0; i--)
        {
            var msg = messages[i];
            int msgTokens = EstimateMessageTokens(msg);
            
            if (currentTokens + msgTokens <= maxTokens)
            {
                result.Insert(0, msg);
                currentTokens += msgTokens;
            }
            else
            {
                if (msg.IsSummarized && result.Count > 10)
                {
                    var removed = RemoveLeastImportantMessage(result);
                    if (removed != null)
                    {
                        currentTokens -= EstimateMessageTokens(removed);
                        result.Insert(0, msg);
                        currentTokens += msgTokens;
                    }
                }
                
                break;
            }
        }
        
        return result;
    }
    
    private ChatMessage? RemoveLeastImportantMessage(List<ChatMessage> messages)
    {
        if (messages.Count <= 10)
            return null;
        
        var middleMessages = messages.Skip(5).Take(messages.Count - 10).ToList();
        if (middleMessages.Count == 0)
            return null;
        
        var leastImportant = middleMessages
            .OrderBy(m => m.ImportanceScore)
            .ThenBy(m => m.EstimatedTokenCount)
            .First();
        
        messages.Remove(leastImportant);
        return leastImportant;
    }
    
    public int CalculateImportanceScore(ChatMessage message, int positionFromEnd)
    {
        int score = 5;
        
        var age = DateTime.Now - message.Timestamp;
        if (age.TotalHours < 1) score += 3;
        else if (age.TotalHours < 24) score += 2;
        else if (age.TotalHours < 168) score += 1;
        else score -= 1;
        
        if (positionFromEnd < 10) score += 2;
        else if (positionFromEnd < 50) score += 1;
        
        int totalLength = message.Parts.Sum(p => p.Length);
        if (totalLength > 500) score += 1;
        if (totalLength > 1000) score += 1;
        
        if (message.IsSummarized) score += 2;
        
        if (message.Role == "user") score += 1;
        
        return Math.Clamp(score, 0, 10);
    }
    
    public void UpdateImportanceScores(ChatSession session)
    {
        var messages = session.Messages;
        for (int i = 0; i < messages.Count; i++)
        {
            int positionFromEnd = messages.Count - 1 - i;
            messages[i].ImportanceScore = CalculateImportanceScore(messages[i], positionFromEnd);
        }
    }
    
    public void UpdateStorageTiers(ChatSession session)
    {
        foreach (var message in session.Messages)
        {
            message.StorageTier = _config.GetStorageTier(message.Timestamp);
        }
    }
    
    public TokenStats GetTokenStats(ChatSession session)
    {
        var messages = session.Messages;
        var hotMessages = new List<ChatMessage>();
        var warmMessages = new List<ChatMessage>();
        var coldMessages = new List<ChatMessage>();
        
        foreach (var msg in messages)
        {
            switch (msg.StorageTier)
            {
                case "hot": hotMessages.Add(msg); break;
                case "warm": warmMessages.Add(msg); break;
                case "cold": coldMessages.Add(msg); break;
            }
        }
        
        var totalTokens = EstimateTotalTokens(messages);
        return new TokenStats
        {
            TotalMessages = messages.Count,
            TotalTokens = totalTokens,
            HotMessages = hotMessages.Count,
            WarmMessages = warmMessages.Count,
            ColdMessages = coldMessages.Count,
            HotTokens = EstimateTotalTokens(hotMessages),
            WarmTokens = EstimateTotalTokens(warmMessages),
            ColdTokens = EstimateTotalTokens(coldMessages),
            SummarizedMessages = messages.Count(m => m.IsSummarized),
            ActiveMessages = Math.Min(messages.Count, _config.MaxActiveMessages),
            AverageTokensPerMessage = messages.Count > 0 ? totalTokens / messages.Count : 0
        };
    }
}

public class TokenStats
{
    public int TotalMessages { get; set; }
    public int TotalTokens { get; set; }
    public int HotMessages { get; set; }
    public int WarmMessages { get; set; }
    public int ColdMessages { get; set; }
    public int HotTokens { get; set; }
    public int WarmTokens { get; set; }
    public int ColdTokens { get; set; }
    public int SummarizedMessages { get; set; }
    public int ActiveMessages { get; set; }
    public int AverageTokensPerMessage { get; set; }
    
    public override string ToString()
    {
        return $"Total: {TotalMessages} msgs ({TotalTokens} tokens) | " +
               $"Hot: {HotMessages} ({HotTokens}t) | " +
               $"Warm: {WarmMessages} ({WarmTokens}t) | " +
               $"Cold: {ColdMessages} ({ColdTokens}t) | " +
               $"Summarized: {SummarizedMessages} | " +
               $"Avg: {AverageTokensPerMessage} tokens/msg";
    }
}
