namespace ChinoKafuu.Engine.Gemini.Models;

internal class ChatHistoryCache
{
    public ChatSession Session { get; set; } = new();
    public int UnsavedMessageCount { get; set; } = 0;
    public DateTime LastSaved { get; set; } = DateTime.Now;
    public bool IsDirty { get; set; } = false;
} 