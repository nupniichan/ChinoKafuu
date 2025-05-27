namespace ChinoKafuu.Engine.Gemini.Models;

public class ChatMessage
{
    public string Role { get; set; } = "";
    public string[] Parts { get; set; } = Array.Empty<string>();
    public DateTime Timestamp { get; set; } = DateTime.Now;
} 