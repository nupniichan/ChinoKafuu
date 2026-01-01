using ChinoKafuu.Engine.Gemini.Config;
using ChinoKafuu.Engine.Gemini.Models;

namespace ChinoKafuu.Engine.Gemini.Services;

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
    
    public void OrganizeMessageTiers(ChatSession session)
    {
        var now = DateTime.Now;
        
        foreach (var message in session.Messages)
        {
            var age = now - message.Timestamp;
            
            if (message.IsSummarized)
            {
                message.StorageTier = "warm";
            }
            else if (message.ImportanceScore >= _config.MinImportanceToPreserveSummarization)
            {
                message.StorageTier = age.TotalHours < _config.WarmTierThresholdHours * 2 ? "hot" : "warm";
            }
            else
            {
                message.StorageTier = _config.GetStorageTier(message.Timestamp);
            }
        }
        
        var tierInfo = GetTierInfo(session);
        session.Metadata["tierDistribution"] = tierInfo.ToString();
        session.Metadata["lastTierUpdate"] = now;
    }
    
    public StorageTierInfo GetTierInfo(ChatSession session)
    {
        var info = new StorageTierInfo { SessionId = session.SessionId };
        
        foreach (var msg in session.Messages)
        {
            var tokens = _tokenService.EstimateMessageTokens(msg);
            switch (msg.StorageTier)
            {
                case "hot":
                    info.HotCount++;
                    info.HotTokens += tokens;
                    break;
                case "warm":
                    info.WarmCount++;
                    info.WarmTokens += tokens;
                    break;
                case "cold":
                    info.ColdCount++;
                    info.ColdTokens += tokens;
                    break;
            }
        }
        
        info.TotalCount = session.Messages.Count;
        info.TotalTokens = info.HotTokens + info.WarmTokens + info.ColdTokens;
        return info;
    }
    
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
        
        var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        var archiveSession = new ChatSession
        {
            SessionId = $"{session.SessionId}_archive_{timestamp}",
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
        
        var directory = Path.GetDirectoryName(baseHistoryPath) ?? "";
        var filename = Path.GetFileNameWithoutExtension(baseHistoryPath);
        var archivePath = Path.Combine(directory, "archives", $"{filename}_archive_{timestamp}.json");
        
        await _compressionService.SaveSessionToFile(archiveSession, archivePath, forceCompression: true, cancellationToken);
        
        session.Messages = session.Messages
            .Where(m => m.StorageTier != "cold")
            .ToList();
        
        session.Metadata["lastArchived"] = DateTime.Now;
        session.Metadata["archivedMessageCount"] = coldMessages.Count;
        session.Metadata["archivePath"] = archivePath;
        return archivePath;
    }
    
    public async Task<List<ChatMessage>> LoadArchivedMessages(
        string archivePath,
        CancellationToken cancellationToken = default)
    {
        var archiveSession = await _compressionService.LoadSessionFromFile(archivePath, cancellationToken);
        return archiveSession?.Messages ?? new List<ChatMessage>();
    }
    
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
        
        OrganizeMessageTiers(session);
        _tokenService.UpdateImportanceScores(session);
        
        if (session.Messages.Count(m => m.StorageTier == "cold") > 50)
        {
            var archivePath = await ArchiveOldMessages(session, historyPath, cancellationToken);
            if (archivePath != null)
            {
                result.ArchivedMessages = session.Messages.Count(m => m.StorageTier == "cold");
                result.ArchivePath = archivePath;
            }
        }
        
        var compressionStats = await _compressionService.GetCompressionStats(historyPath, cancellationToken);
        if (!compressionStats.IsCompressed && compressionStats.OriginalSize > _config.MinFileSizeForCompression)
        {
            result.CompressionApplied = true;
            result.SpaceSavedByCompression = compressionStats.SpaceSaved;
        }
        
        var afterInfo = GetTierInfo(session);
        result.AfterTierInfo = afterInfo;
        result.FinalMessageCount = session.Messages.Count;
        return result;
    }
    
    public List<ChatMessage> GetMessagesByTier(ChatSession session, string tier)
    {
        return session.Messages
            .Where(m => m.StorageTier == tier)
            .OrderBy(m => m.Timestamp)
            .ToList();
    }
    
    public List<ChatMessage> GetHotMessages(ChatSession session) => GetMessagesByTier(session, "hot");
    public List<ChatMessage> GetWarmMessages(ChatSession session) => GetMessagesByTier(session, "warm");
    
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
    
    public List<string> FindArchiveFiles(string baseHistoryPath)
    {
        var directory = Path.GetDirectoryName(baseHistoryPath) ?? "";
        var filename = Path.GetFileNameWithoutExtension(baseHistoryPath);
        var archiveDir = Path.Combine(directory, "archives");
        
        return !Directory.Exists(archiveDir) 
            ? new List<string>()
            : Directory.GetFiles(archiveDir, $"{filename}_archive_*.json*")
                .OrderByDescending(f => f)
                .ToList();
    }
}

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
