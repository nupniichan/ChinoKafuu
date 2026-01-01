namespace ChinoKafuu.Engine.Gemini.Models;

public class ChatMessage
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    
    public string Role { get; set; } = "";
    public string[] Parts { get; set; } = Array.Empty<string>();
    public DateTime Timestamp { get; set; } = DateTime.Now;
    
    public int EstimatedTokenCount { get; set; } = 0;
    
    public bool IsSummary { get; set; } = false;
    
    public bool IsSummarized 
    { 
        get => IsSummary; 
        set => IsSummary = value; 
    }
    
    public string? OriginalContent { get; set; } = null;
    
    public int SummarizedMessageCount { get; set; } = 0;
    
    public int ImportanceScore { get; set; } = 5;
    
    public string StorageTier { get; set; } = "hot";
    
    public bool IsCompressed { get; set; } = false;
    
    public Dictionary<string, object> Metadata { get; set; } = new();
} 