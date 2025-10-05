using ChinoKafuu.Engine.Gemini.Config;
using ChinoKafuu.Engine.Gemini.Models;

namespace ChinoKafuu.Engine.Gemini.Services;

/// <summary>
/// Manages hot/warm/cold storage tiers for chat messages
/// Hot: Recent, frequently accessed messages (in memory)
/// Warm: Medium-age messages (cached)
/// Cold: Old messages, summarized (archived)
/// </summary>
public class TieredStorageManager
{
    private readonly GeminiConfig _config;
    private readonly TokenManagementService _tokenService;
    private readonly HistoryCompressionService _compressionService;
    private readonly Dictionary<string, StorageTierInfo> _tierCache = new();
    
    public TieredStorageManager(
        GeminiConfig? config = null,
        TokenManagementService? tokenService = null,
        HistoryCompressionService? compressionService = null)
    {
        _config = config ?? GeminiConfig.Instance;
        _tokenService = tokenService ?? new TokenManagementService(_config);
        _compressionService = compressionService ?? new HistoryCompressionService(_config);
    }
    
    /// <summary>
    /// Organize messages into storage tiers based on age and importance
    /// </summary>
    public void OrganizeMessageTiers(ChatSession session)
    {
        var now = DateTime.Now;
        
        foreach (var message in session.Messages)
        {
            var age = now - message.Timestamp;
            
            // Determine tier
            if (message.IsSummarized)
            {
                // Summary messages stay in warm tier for easy access
                message.StorageTier = "warm";
            }
            else if (message.ImportanceScore >= _config.MinImportanceToPreserveSummarization)
            {
                // Important messages stay hot longer
                message.StorageTier = age.TotalHours < _config.WarmTierThresholdHours * 2 ? "hot" : "warm";
            }
            else
            {
                // Regular messages follow standard tiering
                message.StorageTier = _config.GetStorageTier(message.Timestamp);
            }
        }
        
        // Update session metadata
        var tierInfo = GetTierInfo(session);
        session.Metadata["tierDistribution"] = tierInfo.ToString();
        session.Metadata["lastTierUpdate"] = now;
    }
    
    /// <summary>
    /// Get tier distribution information
    /// </summary>
    public StorageTierInfo GetTierInfo(ChatSession session)
    {
        var info = new StorageTierInfo
        {
            SessionId = session.SessionId
        };
        
        foreach (var msg in session.Messages)
        {
            switch (msg.StorageTier)
            {
                case "hot":
                    info.HotCount++;
                    info.HotTokens += _tokenService.EstimateMessageTokens(msg);
                    break;
                case "warm":
                    info.WarmCount++;
                    info.WarmTokens += _tokenService.EstimateMessageTokens(msg);
                    break;
                case "cold":
                    info.ColdCount++;
                    info.ColdTokens += _tokenService.EstimateMessageTokens(msg);
                    break;
            }
        }
        
        info.TotalCount = session.Messages.Count;
        info.TotalTokens = info.HotTokens + info.WarmTokens + info.ColdTokens;
        
        return info;
    }
    
    /// <summary>
    /// Archive old messages to separate files for better organization
    /// </summary>
    public async Task<string?> ArchiveOldMessages(
        ChatSession session,
        string baseHistoryPath,
        CancellationToken cancellationToken = default)
    {
        var coldMessages = session.Messages
            .Where(m => m.StorageTier == "cold")
            .ToList();
        
        if (coldMessages.Count == 0)
            return null;
        
        // Create archive session
        var archiveSession = new ChatSession
        {
            SessionId = session.SessionId + "_archive_" + DateTime.Now.ToString("yyyyMMdd_HHmmss"),
            Messages = coldMessages,
            LastUpdated = DateTime.Now,
            Metadata = new Dictionary<string, object>
            {
                { "isArchive", true },
                { "parentSessionId", session.SessionId },
                { "archivedAt", DateTime.Now },
                { "messageCount", coldMessages.Count }
            }
        };
        
        // Save to archive file
        var directory = Path.GetDirectoryName(baseHistoryPath);
        var filename = Path.GetFileNameWithoutExtension(baseHistoryPath);
        var archivePath = Path.Combine(
            directory ?? "", 
            "archives", 
            $"{filename}_archive_{DateTime.Now:yyyyMMdd_HHmmss}.json"
        );
        
        await _compressionService.SaveSessionToFile(archiveSession, archivePath, forceCompression: true, cancellationToken);
        
        // Remove cold messages from active session
        session.Messages = session.Messages
            .Where(m => m.StorageTier != "cold")
            .ToList();
        
        session.Metadata["lastArchived"] = DateTime.Now;
        session.Metadata["archivedMessageCount"] = coldMessages.Count;
        session.Metadata["archivePath"] = archivePath;
        
        Console.WriteLine($"[TIERED STORAGE] Archived {coldMessages.Count} cold messages to {Path.GetFileName(archivePath)}");
        
        return archivePath;
    }
    
    /// <summary>
    /// Load archived messages back into session if needed
    /// </summary>
    public async Task<List<ChatMessage>> LoadArchivedMessages(
        string archivePath,
        CancellationToken cancellationToken = default)
    {
        var archiveSession = await _compressionService.LoadSessionFromFile(archivePath, cancellationToken);
        return archiveSession?.Messages ?? new List<ChatMessage>();
    }
    
    /// <summary>
    /// Optimize storage by moving messages between tiers
    /// </summary>
    public async Task<StorageOptimizationResult> OptimizeStorage(
        ChatSession session,
        string historyPath,
        CancellationToken cancellationToken = default)
    {
        var result = new StorageOptimizationResult
        {
            SessionId = session.SessionId,
            InitialMessageCount = session.Messages.Count
        };
        
        var beforeInfo = GetTierInfo(session);
        result.BeforeTierInfo = beforeInfo;
        
        // Step 1: Update tiers
        OrganizeMessageTiers(session);
        _tokenService.UpdateImportanceScores(session);
        
        // Step 2: Archive cold messages if enabled
        if (session.Messages.Count(m => m.StorageTier == "cold") > 50)
        {
            var archivePath = await ArchiveOldMessages(session, historyPath, cancellationToken);
            if (archivePath != null)
            {
                result.ArchivedMessages = session.Messages.Count(m => m.StorageTier == "cold");
                result.ArchivePath = archivePath;
            }
        }
        
        // Step 3: Compress if beneficial
        var compressionStats = await _compressionService.GetCompressionStats(historyPath, cancellationToken);
        if (!compressionStats.IsCompressed && compressionStats.OriginalSize > _config.MinFileSizeForCompression)
        {
            result.CompressionApplied = true;
            result.SpaceSavedByCompression = compressionStats.SpaceSaved;
        }
        
        var afterInfo = GetTierInfo(session);
        result.AfterTierInfo = afterInfo;
        result.FinalMessageCount = session.Messages.Count;
        
        Console.WriteLine($"[TIERED STORAGE] Optimization completed:");
        Console.WriteLine($"  Messages: {result.InitialMessageCount} → {result.FinalMessageCount}");
        Console.WriteLine($"  Archived: {result.ArchivedMessages}");
        Console.WriteLine($"  Space saved: {result.SpaceSavedByCompression:N0} bytes");
        
        return result;
    }
    
    /// <summary>
    /// Get hot messages for active use (fastest access)
    /// </summary>
    public List<ChatMessage> GetHotMessages(ChatSession session)
    {
        return session.Messages
            .Where(m => m.StorageTier == "hot")
            .OrderBy(m => m.Timestamp)
            .ToList();
    }
    
    /// <summary>
    /// Get warm messages (cached, medium access speed)
    /// </summary>
    public List<ChatMessage> GetWarmMessages(ChatSession session)
    {
        return session.Messages
            .Where(m => m.StorageTier == "warm")
            .OrderBy(m => m.Timestamp)
            .ToList();
    }
    
    /// <summary>
    /// Promote a message to a higher tier (increase priority)
    /// </summary>
    public void PromoteMessage(ChatMessage message)
    {
        message.StorageTier = message.StorageTier switch
        {
            "cold" => "warm",
            "warm" => "hot",
            _ => message.StorageTier
        };
        
        message.ImportanceScore = Math.Min(10, message.ImportanceScore + 2);
    }
    
    /// <summary>
    /// Demote a message to a lower tier (decrease priority)
    /// </summary>
    public void DemoteMessage(ChatMessage message)
    {
        message.StorageTier = message.StorageTier switch
        {
            "hot" => "warm",
            "warm" => "cold",
            _ => message.StorageTier
        };
        
        message.ImportanceScore = Math.Max(0, message.ImportanceScore - 1);
    }
    
    /// <summary>
    /// Find all archive files for a session
    /// </summary>
    public List<string> FindArchiveFiles(string baseHistoryPath)
    {
        var directory = Path.GetDirectoryName(baseHistoryPath);
        var filename = Path.GetFileNameWithoutExtension(baseHistoryPath);
        var archiveDir = Path.Combine(directory ?? "", "archives");
        
        if (!Directory.Exists(archiveDir))
            return new List<string>();
        
        return Directory.GetFiles(archiveDir, $"{filename}_archive_*.json*")
            .OrderByDescending(f => f)
            .ToList();
    }
}

/// <summary>
/// Information about storage tier distribution
/// </summary>
public class StorageTierInfo
{
    public string SessionId { get; set; } = "";
    public int HotCount { get; set; }
    public int WarmCount { get; set; }
    public int ColdCount { get; set; }
    public int TotalCount { get; set; }
    public int HotTokens { get; set; }
    public int WarmTokens { get; set; }
    public int ColdTokens { get; set; }
    public int TotalTokens { get; set; }
    
    public override string ToString()
    {
        return $"Hot: {HotCount} msgs ({HotTokens}t) | " +
               $"Warm: {WarmCount} msgs ({WarmTokens}t) | " +
               $"Cold: {ColdCount} msgs ({ColdTokens}t) | " +
               $"Total: {TotalCount} msgs ({TotalTokens}t)";
    }
}

/// <summary>
/// Result of storage optimization
/// </summary>
public class StorageOptimizationResult
{
    public string SessionId { get; set; } = "";
    public int InitialMessageCount { get; set; }
    public int FinalMessageCount { get; set; }
    public int ArchivedMessages { get; set; }
    public bool CompressionApplied { get; set; }
    public long SpaceSavedByCompression { get; set; }
    public string? ArchivePath { get; set; }
    public StorageTierInfo? BeforeTierInfo { get; set; }
    public StorageTierInfo? AfterTierInfo { get; set; }
    
    public override string ToString()
    {
        return $"Optimization for {SessionId}:\n" +
               $"  Messages: {InitialMessageCount} → {FinalMessageCount}\n" +
               $"  Archived: {ArchivedMessages}\n" +
               $"  Compression: {(CompressionApplied ? $"Yes ({SpaceSavedByCompression:N0} bytes saved)" : "No")}\n" +
               $"  Before: {BeforeTierInfo}\n" +
               $"  After: {AfterTierInfo}";
    }
}
