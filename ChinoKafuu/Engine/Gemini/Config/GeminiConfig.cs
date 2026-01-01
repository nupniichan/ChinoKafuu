namespace ChinoKafuu.Engine.Gemini.Config;

public class GeminiConfig
{
    public int MaxActiveMessages { get; set; } = 150;
    
    public int SummarizationThreshold { get; set; } = 300;
    
    public int PostSummarizationTarget { get; set; } = 200;
    
    public int MaxTokensPerRequest { get; set; } = 30000;
    
    public int AverageTokensPerMessage { get; set; } = 150;
    
    public bool EnableCompression { get; set; } = true;
    
    public long MinFileSizeForCompression { get; set; } = 10 * 1024;
    
    public int AutoSaveIntervalSeconds { get; set; } = 30;
    
    public long MaxHistoryFileSize { get; set; } = 5 * 1024 * 1024;
    
    public int WarmTierThresholdHours { get; set; } = 24;
    
    public int ColdTierThresholdHours { get; set; } = 168;
    
    public int MinRecentMessagesToKeep { get; set; } = 50;
    
    public int ApiTimeoutMinutes { get; set; } = 5;
    
    public float Temperature { get; set; } = 1.0f;
    
    public int TopK { get; set; } = 40;
    
    public float TopP { get; set; } = 0.95f;
    
    public int MaxOutputTokens { get; set; } = 2048;
    
    public bool EnableParallelProcessing { get; set; } = true;
    
    public int CacheExpirationMinutes { get; set; } = 60;
    
    public int BatchSize { get; set; } = 10;
    
    public bool EnableAutoSummarization { get; set; } = true;
    
    public int SummarizationBatchSize { get; set; } = 50;
    
    public bool KeepOriginalMessages { get; set; } = false;
    
    public bool ArchiveOriginalMessagesAfterSummarization { get; set; } = true;
    
    public int ArchiveRetentionDays { get; set; } = 90;
    
    public int MinImportanceToPreserveSummarization { get; set; } = 7;
    
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
    
    public int EstimateTokens(string text)
    {
        return text.Length / 2;
    }
    
    public bool ShouldTriggerSummarization(int messageCount)
    {
        return EnableAutoSummarization && messageCount >= SummarizationThreshold;
    }
    
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
