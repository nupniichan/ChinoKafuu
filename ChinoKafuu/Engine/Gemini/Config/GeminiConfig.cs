namespace ChinoKafuu.Engine.Gemini.Config;

/// <summary>
/// Centralized configuration for Gemini API and chat optimization
/// </summary>
public class GeminiConfig
{
    // ============= TOKEN MANAGEMENT =============
    
    /// <summary>Maximum messages to keep in active memory (hot storage)</summary>
    public int MaxActiveMessages { get; set; } = 150;
    
    /// <summary>Maximum messages before triggering summarization</summary>
    public int SummarizationThreshold { get; set; } = 200;
    
    /// <summary>Target messages after summarization (keeps recent + summary)</summary>
    public int PostSummarizationTarget { get; set; } = 100;
    
    /// <summary>Maximum tokens to send to API per request</summary>
    public int MaxTokensPerRequest { get; set; } = 30000; // Gemini 2.5 Flash supports 1M, but we limit for efficiency
    
    /// <summary>Average tokens per message (estimation)</summary>
    public int AverageTokensPerMessage { get; set; } = 150;
    
    // ============= STORAGE OPTIMIZATION =============
    
    /// <summary>Enable compression for history files</summary>
    public bool EnableCompression { get; set; } = true;
    
    /// <summary>Minimum file size (bytes) before compression kicks in</summary>
    public long MinFileSizeForCompression { get; set; } = 10 * 1024; // 10KB
    
    /// <summary>Auto-save interval in seconds (0 = disabled)</summary>
    public int AutoSaveIntervalSeconds { get; set; } = 30;
    
    /// <summary>Maximum chat history file size before archiving (bytes)</summary>
    public long MaxHistoryFileSize { get; set; } = 5 * 1024 * 1024; // 5MB
    
    // ============= MESSAGE TIERING =============
    
    /// <summary>Messages older than this (hours) move to "warm" tier</summary>
    public int WarmTierThresholdHours { get; set; } = 24;
    
    /// <summary>Messages older than this (hours) move to "cold" tier</summary>
    public int ColdTierThresholdHours { get; set; } = 168; // 7 days
    
    /// <summary>Keep at least this many recent messages regardless of tier</summary>
    public int MinRecentMessagesToKeep { get; set; } = 50;
    
    // ============= API CONFIGURATION =============
    
    /// <summary>API request timeout in minutes</summary>
    public int ApiTimeoutMinutes { get; set; } = 5;
    
    /// <summary>Temperature for API requests (0.0 - 2.0)</summary>
    public float Temperature { get; set; } = 1.0f;
    
    /// <summary>Top-K sampling parameter</summary>
    public int TopK { get; set; } = 40;
    
    /// <summary>Top-P (nucleus sampling) parameter</summary>
    public float TopP { get; set; } = 0.95f;
    
    /// <summary>Max output tokens per response</summary>
    public int MaxOutputTokens { get; set; } = 2048;
    
    // ============= PERFORMANCE TUNING =============
    
    /// <summary>Enable parallel processing where possible</summary>
    public bool EnableParallelProcessing { get; set; } = true;
    
    /// <summary>Cache expiration time in minutes</summary>
    public int CacheExpirationMinutes { get; set; } = 60;
    
    /// <summary>Batch size for bulk operations</summary>
    public int BatchSize { get; set; } = 10;
    
    // ============= SUMMARIZATION SETTINGS =============
    
    /// <summary>Enable automatic summarization of old messages</summary>
    public bool EnableAutoSummarization { get; set; } = true;
    
    /// <summary>Summarize in batches of this many messages</summary>
    public int SummarizationBatchSize { get; set; } = 50;
    
    /// <summary>Keep original messages even after summarization</summary>
    public bool KeepOriginalMessages { get; set; } = false;
    
    /// <summary>Archive original messages to separate file after summarization (recommended: true)</summary>
    public bool ArchiveOriginalMessagesAfterSummarization { get; set; } = true;
    
    /// <summary>Delete archived messages older than X days (0 = never delete)</summary>
    public int ArchiveRetentionDays { get; set; } = 90;
    
    /// <summary>Minimum importance score to prevent summarization (0-10)</summary>
    public int MinImportanceToPreserveSummarization { get; set; } = 7;
    
    // ============= STATIC INSTANCE =============
    
    private static GeminiConfig? _instance;
    private static readonly object _lock = new();
    
    public static GeminiConfig Instance
    {
        get
        {
            if (_instance == null)
            {
                lock (_lock)
                {
                    _instance ??= new GeminiConfig();
                }
            }
            return _instance;
        }
    }
    
    // ============= HELPER METHODS =============
    
    /// <summary>Calculate estimated tokens for a message</summary>
    public int EstimateTokens(string text)
    {
        // Rough estimation: 1 token ≈ 4 characters for English, ~1.5 chars for Vietnamese
        // This is a simple heuristic, not perfect but good enough for our use case
        return text.Length / 2; // Conservative estimate for mixed EN/VN
    }
    
    /// <summary>Check if summarization should be triggered</summary>
    public bool ShouldTriggerSummarization(int messageCount)
    {
        return EnableAutoSummarization && messageCount >= SummarizationThreshold;
    }
    
    /// <summary>Get storage tier based on message age</summary>
    public string GetStorageTier(DateTime messageTimestamp)
    {
        var age = DateTime.Now - messageTimestamp;
        
        if (age.TotalHours < WarmTierThresholdHours)
            return "hot";
        else if (age.TotalHours < ColdTierThresholdHours)
            return "warm";
        else
            return "cold";
    }
    
    /// <summary>Validate configuration values</summary>
    public void Validate()
    {
        if (MaxActiveMessages < 10)
            throw new InvalidOperationException("MaxActiveMessages must be at least 10");
        
        if (SummarizationThreshold <= MaxActiveMessages)
            throw new InvalidOperationException("SummarizationThreshold must be greater than MaxActiveMessages");
        
        if (PostSummarizationTarget > MaxActiveMessages)
            throw new InvalidOperationException("PostSummarizationTarget must be <= MaxActiveMessages");
        
        if (Temperature < 0 || Temperature > 2)
            throw new InvalidOperationException("Temperature must be between 0 and 2");
        
        if (TopP < 0 || TopP > 1)
            throw new InvalidOperationException("TopP must be between 0 and 1");
    }
}
