using ChinoKafuu.Engine.Gemini.Config;
using ChinoKafuu.Engine.Gemini.Models;

namespace ChinoKafuu.Engine.Gemini.Services;

/// <summary>
/// Manages token counting, optimization, and sliding window for chat messages
/// </summary>
public class TokenManagementService
{
    private readonly GeminiConfig _config;
    
    public TokenManagementService(GeminiConfig? config = null)
    {
        _config = config ?? GeminiConfig.Instance;
    }
    
    /// <summary>
    /// Estimate tokens for a single message
    /// </summary>
    public int EstimateMessageTokens(ChatMessage message)
    {
        if (message.EstimatedTokenCount > 0)
            return message.EstimatedTokenCount;
        
        int totalTokens = 0;
        foreach (var part in message.Parts)
        {
            totalTokens += _config.EstimateTokens(part);
        }
        
        // Add overhead for role and structure (~20 tokens)
        totalTokens += 20;
        
        message.EstimatedTokenCount = totalTokens;
        return totalTokens;
    }
    
    /// <summary>
    /// Estimate total tokens for a list of messages
    /// </summary>
    public int EstimateTotalTokens(List<ChatMessage> messages)
    {
        return messages.Sum(EstimateMessageTokens);
    }
    
    /// <summary>
    /// Get optimal messages to send using sliding window strategy
    /// Prioritizes: recent messages + important messages + summary of old messages
    /// </summary>
    public List<ChatMessage> GetOptimalMessagesForAPI(ChatSession session)
    {
        var allMessages = session.Messages;
        if (allMessages.Count == 0)
            return new List<ChatMessage>();
        
        // Step 1: Get recent messages (hot tier)
        var recentMessages = allMessages
            .TakeLast(_config.MaxActiveMessages)
            .ToList();
        
        int currentTokens = EstimateTotalTokens(recentMessages);
        
        // Step 2: If we're under token limit, we can include more context
        if (currentTokens < _config.MaxTokensPerRequest)
        {
            // Try to add important older messages
            var olderMessages = allMessages
                .Take(allMessages.Count - _config.MaxActiveMessages)
                .Where(m => m.ImportanceScore >= _config.MinImportanceToPreserveSummarization)
                .OrderByDescending(m => m.ImportanceScore)
                .ToList();
            
            foreach (var msg in olderMessages)
            {
                int msgTokens = EstimateMessageTokens(msg);
                if (currentTokens + msgTokens < _config.MaxTokensPerRequest)
                {
                    recentMessages.Insert(0, msg);
                    currentTokens += msgTokens;
                }
                else
                {
                    break;
                }
            }
        }
        
        // Step 3: If we're over the limit, trim from the middle (keep newest and oldest summary)
        if (currentTokens > _config.MaxTokensPerRequest)
        {
            recentMessages = TrimToTokenLimit(recentMessages, _config.MaxTokensPerRequest);
        }
        
        return recentMessages;
    }
    
    /// <summary>
    /// Trim messages to fit within token limit, preserving conversation flow
    /// </summary>
    private List<ChatMessage> TrimToTokenLimit(List<ChatMessage> messages, int maxTokens)
    {
        var result = new List<ChatMessage>();
        int currentTokens = 0;
        
        // Strategy: Keep newest messages first (LIFO)
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
                // Check if this is a summary message - prioritize it
                if (msg.IsSummarized && result.Count > 10)
                {
                    // Try to make room by removing less important messages from the middle
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
    
    /// <summary>
    /// Remove the least important message from the list
    /// </summary>
    private ChatMessage? RemoveLeastImportantMessage(List<ChatMessage> messages)
    {
        if (messages.Count <= 10) // Keep minimum context
            return null;
        
        // Don't remove first or last 5 messages (preserve context boundaries)
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
    
    /// <summary>
    /// Calculate importance score for a message based on various factors
    /// Score range: 0-10 (higher = more important)
    /// </summary>
    public int CalculateImportanceScore(ChatMessage message, int positionFromEnd)
    {
        int score = 5; // Base score
        
        // Factor 1: Recency (newer = more important)
        var age = DateTime.Now - message.Timestamp;
        if (age.TotalHours < 1) score += 3;
        else if (age.TotalHours < 24) score += 2;
        else if (age.TotalHours < 168) score += 1;
        else score -= 1;
        
        // Factor 2: Position in conversation (recent messages are more important)
        if (positionFromEnd < 10) score += 2;
        else if (positionFromEnd < 50) score += 1;
        
        // Factor 3: Message length (longer might contain more context)
        int totalLength = message.Parts.Sum(p => p.Length);
        if (totalLength > 500) score += 1;
        if (totalLength > 1000) score += 1;
        
        // Factor 4: Already summarized messages are important to keep
        if (message.IsSummarized) score += 2;
        
        // Factor 5: User messages slightly more important (preserve user questions)
        if (message.Role == "user") score += 1;
        
        // Clamp to 0-10 range
        return Math.Clamp(score, 0, 10);
    }
    
    /// <summary>
    /// Update importance scores for all messages in a session
    /// </summary>
    public void UpdateImportanceScores(ChatSession session)
    {
        var messages = session.Messages;
        for (int i = 0; i < messages.Count; i++)
        {
            int positionFromEnd = messages.Count - 1 - i;
            messages[i].ImportanceScore = CalculateImportanceScore(messages[i], positionFromEnd);
        }
    }
    
    /// <summary>
    /// Update storage tiers based on message age
    /// </summary>
    public void UpdateStorageTiers(ChatSession session)
    {
        foreach (var message in session.Messages)
        {
            message.StorageTier = _config.GetStorageTier(message.Timestamp);
        }
    }
    
    /// <summary>
    /// Get statistics about token usage
    /// </summary>
    public TokenStats GetTokenStats(ChatSession session)
    {
        var messages = session.Messages;
        var stats = new TokenStats
        {
            TotalMessages = messages.Count,
            TotalTokens = EstimateTotalTokens(messages),
            HotMessages = messages.Count(m => m.StorageTier == "hot"),
            WarmMessages = messages.Count(m => m.StorageTier == "warm"),
            ColdMessages = messages.Count(m => m.StorageTier == "cold"),
            SummarizedMessages = messages.Count(m => m.IsSummarized),
            ActiveMessages = messages.TakeLast(_config.MaxActiveMessages).Count(),
            AverageTokensPerMessage = messages.Count > 0 ? EstimateTotalTokens(messages) / messages.Count : 0
        };
        
        // Calculate tokens by tier
        stats.HotTokens = EstimateTotalTokens(messages.Where(m => m.StorageTier == "hot").ToList());
        stats.WarmTokens = EstimateTotalTokens(messages.Where(m => m.StorageTier == "warm").ToList());
        stats.ColdTokens = EstimateTotalTokens(messages.Where(m => m.StorageTier == "cold").ToList());
        
        return stats;
    }
}

/// <summary>
/// Token usage statistics
/// </summary>
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
