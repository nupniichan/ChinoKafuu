using System.Text.Json;
using ChinoKafuu.Engine.Gemini.Config;
using ChinoKafuu.Engine.Gemini.Models;

namespace ChinoKafuu.Engine.Gemini.Services;

public class SummaryStorageService
{
    private readonly GeminiConfig _config;
    private readonly HistoryCompressionService _compressionService;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    public SummaryStorageService(GeminiConfig config, HistoryCompressionService compressionService)
    {
        _config = config;
        _compressionService = compressionService;
    }

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

        string json = JsonSerializer.Serialize(summaryData, JsonOptions);
        await _compressionService.CompressAndSaveString(summaryPath, json, cancellationToken);
    }

    public async Task<List<ChatMessage>> LoadSummaries(string basePath, CancellationToken cancellationToken = default)
    {
        string summaryPath = GetSummaryPath(basePath);
        var json = await _compressionService.LoadAndDecompressString(summaryPath, cancellationToken);
        
        if (string.IsNullOrEmpty(json))
            return new List<ChatMessage>();

        try
        {
            return JsonSerializer.Deserialize<SummaryStorage>(json)?.Summaries ?? new List<ChatMessage>();
        }
        catch
        {
            return new List<ChatMessage>();
        }
    }

    public async Task AddSummary(string basePath, ChatMessage summary, CancellationToken cancellationToken = default)
    {
        var existingSummaries = await LoadSummaries(basePath, cancellationToken);
        existingSummaries.Add(summary);
        await SaveSummaries(basePath, existingSummaries, cancellationToken);
    }

    public async Task ArchiveMessages(
        string basePath, 
        List<ChatMessage> messagesToArchive, 
        CancellationToken cancellationToken = default)
    {
        if (!_config.ArchiveOriginalMessagesAfterSummarization || messagesToArchive.Count == 0)
            return;

        string archivePath = GetArchivePath(basePath);
        
        var existingArchive = await LoadArchive(basePath, cancellationToken);
        
        existingArchive.Messages.AddRange(messagesToArchive);
        existingArchive.LastUpdated = DateTime.UtcNow;
        existingArchive.TotalMessages = existingArchive.Messages.Count;

        string json = JsonSerializer.Serialize(existingArchive, JsonOptions);
        await _compressionService.CompressAndSaveString(archivePath, json, cancellationToken);
    }

    public async Task<MessageArchive> LoadArchive(string basePath, CancellationToken cancellationToken = default)
    {
        string archivePath = GetArchivePath(basePath);
        var json = await _compressionService.LoadAndDecompressString(archivePath, cancellationToken);
        
        if (string.IsNullOrEmpty(json))
            return CreateEmptyArchive();

        try
        {
            return JsonSerializer.Deserialize<MessageArchive>(json) ?? CreateEmptyArchive();
        }
        catch
        {
            return CreateEmptyArchive();
        }
    }
    
    private static MessageArchive CreateEmptyArchive() => new()
    {
        CreatedAt = DateTime.UtcNow,
        Messages = new List<ChatMessage>(),
        TotalMessages = 0
    };

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

            string json = JsonSerializer.Serialize(archive, JsonOptions);
            await _compressionService.CompressAndSaveString(GetArchivePath(basePath), json, cancellationToken);
        }
    }

    public async Task<StorageInfo> GetStorageInfo(string basePath)
    {
        var info = new StorageInfo { BasePath = basePath };
        
        info.MainHistorySize = GetFileSize(basePath);
        info.SummaryFileSize = GetFileSize(GetSummaryPath(basePath));
        info.ArchiveFileSize = GetFileSize(GetArchivePath(basePath));
        info.TotalSize = info.MainHistorySize + info.SummaryFileSize + info.ArchiveFileSize;

        try
        {
            var summaries = await LoadSummaries(basePath);
            info.SummaryCount = summaries.Count;
            info.OriginalMessagesSummarized = summaries.Sum(s => s.SummarizedMessageCount);
            info.ArchivedMessageCount = (await LoadArchive(basePath)).TotalMessages;
        }
        catch { }

        return info;
    }
    
    private static long GetFileSize(string filePath)
    {
        if (File.Exists(filePath))
            return new FileInfo(filePath).Length;
        if (File.Exists(filePath + ".gz"))
            return new FileInfo(filePath + ".gz").Length;
        return 0;
    }

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
}

public class SummaryStorage
{
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
    public List<ChatMessage> Summaries { get; set; } = new();
    public int OriginalMessageCount { get; set; }
    public int TotalTokensSaved { get; set; }
}

public class MessageArchive
{
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
    public List<ChatMessage> Messages { get; set; } = new();
    public int TotalMessages { get; set; }
}

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
