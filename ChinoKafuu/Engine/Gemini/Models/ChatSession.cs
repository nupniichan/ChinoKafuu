namespace ChinoKafuu.Engine.Gemini.Models;

public class ChatSession
{
    public List<ChatMessage> Messages { get; set; } = new();
    public string SessionId { get; set; } = Guid.NewGuid().ToString();
    public string UserId { get; set; } = "";
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime LastUpdated { get; set; } = DateTime.Now;
    public Dictionary<string, object> Metadata { get; set; } = new();
    
    public int TotalTokensUsed { get; set; } = 0;
    public int ActiveMessagesCount { get; set; } = 0;
    public int SummarizedMessagesCount { get; set; } = 0;
    
    public DateTime? LastSummarizedAt { get; set; } = null;
    
    public bool IsCompressed { get; set; } = false;
    public long OriginalSize { get; set; } = 0;
    public long CompressedSize { get; set; } = 0;
} 