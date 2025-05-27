namespace ChinoKafuu.Engine.Gemini.Models;

public class ChatSession
{
    public List<ChatMessage> Messages { get; set; } = new();
    public DateTime LastUpdated { get; set; } = DateTime.Now;
    public string SessionId { get; set; } = Guid.NewGuid().ToString();
    public Dictionary<string, object> Metadata { get; set; } = new();
} 