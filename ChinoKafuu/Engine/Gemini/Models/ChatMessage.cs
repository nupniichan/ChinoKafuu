namespace ChinoKafuu.Engine.Gemini.Models;

public class ChatMessage
{
    /// <summary>Unique identifier for this message</summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();
    
    public string Role { get; set; } = "";
    public string[] Parts { get; set; } = Array.Empty<string>();
    public DateTime Timestamp { get; set; } = DateTime.Now;
    
    // Token optimization metadata
    public int EstimatedTokenCount { get; set; } = 0;
    
    /// <summary>True if this message is a summary of multiple old messages</summary>
    public bool IsSummary { get; set; } = false;
    
    /// <summary>Deprecated: Use IsSummary instead</summary>
    public bool IsSummarized 
    { 
        get => IsSummary; 
        set => IsSummary = value; 
    }
    
    public string? OriginalContent { get; set; } = null;
    
    /// <summary>Number of original messages this summary represents (if IsSummary = true)</summary>
    public int SummarizedMessageCount { get; set; } = 0;
    
    // Importance scoring (0-10, higher = more important)
    public int ImportanceScore { get; set; } = 5;
    
    // Storage tier: "hot" (recent), "warm" (medium), "cold" (old/summarized)
    public string StorageTier { get; set; } = "hot";
    
    // For compressed storage
    public bool IsCompressed { get; set; } = false;
    
    public Dictionary<string, object> Metadata { get; set; } = new();
} 