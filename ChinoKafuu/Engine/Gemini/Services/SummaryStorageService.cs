using System.Text.Json;
using ChinoKafuu.Engine.Gemini.Config;
using ChinoKafuu.Engine.Gemini.Models;

namespace ChinoKafuu.Engine.Gemini.Services;

/// <summary>
/// Manages persistent storage of summaries and archived messages
/// NO MEMORY CACHE - Everything is file-based with lazy loading
/// </summary>
public class SummaryStorageService
{
    private readonly GeminiConfig _config;
    private readonly HistoryCompressionService _compressionService;

    public SummaryStorageService(GeminiConfig config, HistoryCompressionService compressionService)
    {
        _config = config;
        _compressionService = compressionService;
    }

    #region Summary Management

    /// <summary>
    /// Save summaries to separate file (replaces all existing summaries)
    /// </summary>
    public async Task SaveSummaries(string basePath, List<ChatMessage> summaries, CancellationToken cancellationToken = default)
    {
        string summaryPath = GetSummaryPath(basePath);
        
        var summaryData = new SummaryStorage
        {
            CreatedAt = DateTime.UtcNow,
            LastUpdated = DateTime.UtcNow,
            Summaries = summaries,
            OriginalMessageCount = summaries.Sum(s => s.SummarizedMessageCount),
            TotalTokensSaved = summaries.Sum(s => s.Metadata.ContainsKey("tokensSaved") 
                ? Convert.ToInt32(s.Metadata["tokensSaved"]) 
                : 0)
        };

        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        string json = JsonSerializer.Serialize(summaryData, options);
        await _compressionService.CompressAndSaveString(summaryPath, json, cancellationToken);

        Console.WriteLine($"[SummaryStorage] Saved {summaries.Count} summaries (representing {summaryData.OriginalMessageCount} messages)");
    }

    /// <summary>
    /// Load summaries from storage (lazy loading - only when needed)
    /// </summary>
    public async Task<List<ChatMessage>> LoadSummaries(string basePath, CancellationToken cancellationToken = default)
    {
        string summaryPath = GetSummaryPath(basePath);
        
        var json = await _compressionService.LoadAndDecompressString(summaryPath, cancellationToken);
        if (string.IsNullOrEmpty(json))
            return new List<ChatMessage>();

        try
        {
            var summaryData = JsonSerializer.Deserialize<SummaryStorage>(json);
            var summaries = summaryData?.Summaries ?? new List<ChatMessage>();
            
            if (summaries.Count > 0)
            {
                Console.WriteLine($"[SummaryStorage] Loaded {summaries.Count} summaries from disk");
            }
            
            return summaries;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[SummaryStorage] Error loading summaries: {ex.Message}");
            return new List<ChatMessage>();
        }
    }

    /// <summary>
    /// Add a new summary (appends to existing summaries)
    /// </summary>
    public async Task AddSummary(string basePath, ChatMessage summary, CancellationToken cancellationToken = default)
    {
        var existingSummaries = await LoadSummaries(basePath, cancellationToken);
        existingSummaries.Add(summary);
        await SaveSummaries(basePath, existingSummaries, cancellationToken);
    }

    #endregion

    #region Archive Management

    /// <summary>
    /// Archive original messages after summarization
    /// </summary>
    public async Task ArchiveMessages(
        string basePath, 
        List<ChatMessage> messagesToArchive, 
        CancellationToken cancellationToken = default)
    {
        if (!_config.ArchiveOriginalMessagesAfterSummarization || messagesToArchive.Count == 0)
            return;

        string archivePath = GetArchivePath(basePath);
        
        // Load existing archive
        var existingArchive = await LoadArchive(basePath, cancellationToken);
        
        // Add new messages
        existingArchive.Messages.AddRange(messagesToArchive);
        existingArchive.LastUpdated = DateTime.UtcNow;
        existingArchive.TotalMessages = existingArchive.Messages.Count;

        // Save updated archive
        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        string json = JsonSerializer.Serialize(existingArchive, options);
        await _compressionService.CompressAndSaveString(archivePath, json, cancellationToken);

        Console.WriteLine($"[SummaryStorage] Archived {messagesToArchive.Count} messages (total: {existingArchive.TotalMessages})");
    }

    /// <summary>
    /// Load archived messages (lazy loading)
    /// </summary>
    public async Task<MessageArchive> LoadArchive(string basePath, CancellationToken cancellationToken = default)
    {
        string archivePath = GetArchivePath(basePath);
        
        var json = await _compressionService.LoadAndDecompressString(archivePath, cancellationToken);
        if (string.IsNullOrEmpty(json))
        {
            return new MessageArchive
            {
                CreatedAt = DateTime.UtcNow,
                Messages = new List<ChatMessage>(),
                TotalMessages = 0
            };
        }

        try
        {
            var archive = JsonSerializer.Deserialize<MessageArchive>(json);
            return archive ?? new MessageArchive
            {
                CreatedAt = DateTime.UtcNow,
                Messages = new List<ChatMessage>(),
                TotalMessages = 0
            };
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[SummaryStorage] Error loading archive: {ex.Message}");
            return new MessageArchive
            {
                CreatedAt = DateTime.UtcNow,
                Messages = new List<ChatMessage>(),
                TotalMessages = 0
            };
        }
    }

    /// <summary>
    /// Clean up old archived messages based on retention policy
    /// </summary>
    public async Task CleanupOldArchives(string basePath, CancellationToken cancellationToken = default)
    {
        if (_config.ArchiveRetentionDays <= 0)
            return;

        var archive = await LoadArchive(basePath, cancellationToken);
        var cutoffDate = DateTime.UtcNow.AddDays(-_config.ArchiveRetentionDays);
        
        int originalCount = archive.Messages.Count;
        archive.Messages.RemoveAll(m => m.Timestamp < cutoffDate);
        
        int removedCount = originalCount - archive.Messages.Count;
        if (removedCount > 0)
        {
            archive.LastUpdated = DateTime.UtcNow;
            archive.TotalMessages = archive.Messages.Count;

            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            };

            string json = JsonSerializer.Serialize(archive, options);
            await _compressionService.CompressAndSaveString(GetArchivePath(basePath), json, cancellationToken);

            Console.WriteLine($"[SummaryStorage] Cleaned up {removedCount} archived messages older than {_config.ArchiveRetentionDays} days");
        }
    }

    #endregion

    #region Storage Information

    /// <summary>
    /// Get comprehensive storage information for all files
    /// </summary>
    public async Task<StorageInfo> GetStorageInfo(string basePath)
    {
        var info = new StorageInfo
        {
            BasePath = basePath
        };

        // Main history size
        if (File.Exists(basePath))
        {
            info.MainHistorySize = new FileInfo(basePath).Length;
        }
        else if (File.Exists(basePath + ".gz"))
        {
            info.MainHistorySize = new FileInfo(basePath + ".gz").Length;
        }

        // Summary file size
        string summaryPath = GetSummaryPath(basePath);
        if (File.Exists(summaryPath))
        {
            info.SummaryFileSize = new FileInfo(summaryPath).Length;
        }
        else if (File.Exists(summaryPath + ".gz"))
        {
            info.SummaryFileSize = new FileInfo(summaryPath + ".gz").Length;
        }

        // Archive file size
        string archivePath = GetArchivePath(basePath);
        if (File.Exists(archivePath))
        {
            info.ArchiveFileSize = new FileInfo(archivePath).Length;
        }
        else if (File.Exists(archivePath + ".gz"))
        {
            info.ArchiveFileSize = new FileInfo(archivePath + ".gz").Length;
        }

        info.TotalSize = info.MainHistorySize + info.SummaryFileSize + info.ArchiveFileSize;

        // Load counts (lazy)
        try
        {
            var summaries = await LoadSummaries(basePath);
            info.SummaryCount = summaries.Count;
            info.OriginalMessagesSummarized = summaries.Sum(s => s.SummarizedMessageCount);

            var archive = await LoadArchive(basePath);
            info.ArchivedMessageCount = archive.TotalMessages;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[SummaryStorage] Error getting storage info: {ex.Message}");
        }

        return info;
    }

    #endregion

    #region Helper Methods

    private string GetSummaryPath(string basePath)
    {
        string directory = Path.GetDirectoryName(basePath)!;
        string filename = Path.GetFileNameWithoutExtension(basePath);
        return Path.Combine(directory, $"{filename}_summaries.json");
    }

    private string GetArchivePath(string basePath)
    {
        string directory = Path.GetDirectoryName(basePath)!;
        string filename = Path.GetFileNameWithoutExtension(basePath);
        return Path.Combine(directory, $"{filename}_archive.json");
    }

    #endregion
}

#region Data Models

/// <summary>
/// Storage container for summaries
/// </summary>
public class SummaryStorage
{
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
    public List<ChatMessage> Summaries { get; set; } = new();
    public int OriginalMessageCount { get; set; }
    public int TotalTokensSaved { get; set; }
}

/// <summary>
/// Storage container for archived messages
/// </summary>
public class MessageArchive
{
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
    public List<ChatMessage> Messages { get; set; } = new();
    public int TotalMessages { get; set; }
}

/// <summary>
/// Comprehensive storage information
/// </summary>
public class StorageInfo
{
    public string BasePath { get; set; } = "";
    public long MainHistorySize { get; set; }
    public long SummaryFileSize { get; set; }
    public long ArchiveFileSize { get; set; }
    public long TotalSize { get; set; }
    public int SummaryCount { get; set; }
    public int OriginalMessagesSummarized { get; set; }
    public int ArchivedMessageCount { get; set; }

    public override string ToString()
    {
        return $@"Storage Information:
├─ Main History: {FormatBytes(MainHistorySize)}
├─ Summaries: {FormatBytes(SummaryFileSize)} ({SummaryCount} summaries covering {OriginalMessagesSummarized} messages)
├─ Archive: {FormatBytes(ArchiveFileSize)} ({ArchivedMessageCount} archived messages)
└─ Total: {FormatBytes(TotalSize)}";
    }

    private string FormatBytes(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB" };
        double len = bytes;
        int order = 0;
        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len /= 1024;
        }
        return $"{len:0.##} {sizes[order]}";
    }
}

#endregion
