using System.IO.Compression;
using System.Text;
using System.Text.Json;
using ChinoKafuu.Engine.Gemini.Config;
using ChinoKafuu.Engine.Gemini.Models;

namespace ChinoKafuu.Engine.Gemini.Services;

/// <summary>
/// Handles compression and decompression of chat history for efficient storage
/// </summary>
public class HistoryCompressionService
{
    private readonly GeminiConfig _config;
    
    public HistoryCompressionService(GeminiConfig? config = null)
    {
        _config = config ?? GeminiConfig.Instance;
    }
    
    /// <summary>
    /// Compress string to bytes using GZip
    /// </summary>
    public async Task<byte[]> CompressString(string text, CancellationToken cancellationToken = default)
    {
        var bytes = Encoding.UTF8.GetBytes(text);
        
        using var outputStream = new MemoryStream();
        using (var gzipStream = new GZipStream(outputStream, CompressionLevel.Optimal))
        {
            await gzipStream.WriteAsync(bytes, 0, bytes.Length, cancellationToken);
        }
        
        return outputStream.ToArray();
    }
    
    /// <summary>
    /// Decompress bytes to string
    /// </summary>
    public async Task<string> DecompressString(byte[] compressedData, CancellationToken cancellationToken = default)
    {
        using var inputStream = new MemoryStream(compressedData);
        using var gzipStream = new GZipStream(inputStream, CompressionMode.Decompress);
        using var outputStream = new MemoryStream();
        
        await gzipStream.CopyToAsync(outputStream, cancellationToken);
        return Encoding.UTF8.GetString(outputStream.ToArray());
    }
    
    /// <summary>
    /// Compress and save string directly to file
    /// </summary>
    public async Task CompressAndSaveString(string filePath, string content, CancellationToken cancellationToken = default)
    {
        var compressedData = await CompressString(content, cancellationToken);
        
        string actualFilePath = _config.EnableCompression ? filePath + ".gz" : filePath;
        string directory = Path.GetDirectoryName(actualFilePath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }
        
        if (_config.EnableCompression)
        {
            await File.WriteAllBytesAsync(actualFilePath, compressedData, cancellationToken);
            
            // Remove uncompressed version if exists
            if (File.Exists(filePath))
                File.Delete(filePath);
        }
        else
        {
            await File.WriteAllTextAsync(filePath, content, cancellationToken);
            
            // Remove compressed version if exists
            if (File.Exists(filePath + ".gz"))
                File.Delete(filePath + ".gz");
        }
    }
    
    /// <summary>
    /// Load and decompress string from file (handles both compressed and uncompressed)
    /// </summary>
    public async Task<string?> LoadAndDecompressString(string filePath, CancellationToken cancellationToken = default)
    {
        string compressedPath = filePath + ".gz";
        
        // Try compressed version first
        if (File.Exists(compressedPath))
        {
            try
            {
                var compressedData = await File.ReadAllBytesAsync(compressedPath, cancellationToken);
                return await DecompressString(compressedData, cancellationToken);
            }
            catch (Exception ex)
            {
            }
        }
        
        // Try uncompressed version
        if (File.Exists(filePath))
        {
            try
            {
                return await File.ReadAllTextAsync(filePath, cancellationToken);
            }
            catch (Exception ex)
            {
            }
        }
        
        return null;
    }
    
    /// <summary>
    /// Compress chat session to bytes using GZip
    /// </summary>
    public async Task<byte[]> CompressSession(ChatSession session, CancellationToken cancellationToken = default)
    {
        var options = new JsonSerializerOptions
        {
            WriteIndented = false, // Compact JSON for better compression
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
        
        var json = JsonSerializer.Serialize(session, options);
        var bytes = Encoding.UTF8.GetBytes(json);
        
        using var outputStream = new MemoryStream();
        using (var gzipStream = new GZipStream(outputStream, CompressionLevel.Optimal))
        {
            await gzipStream.WriteAsync(bytes, 0, bytes.Length, cancellationToken);
        }
        
        return outputStream.ToArray();
    }
    
    /// <summary>
    /// Decompress bytes to chat session
    /// </summary>
    public async Task<ChatSession?> DecompressSession(byte[] compressedData, CancellationToken cancellationToken = default)
    {
        using var inputStream = new MemoryStream(compressedData);
        using var gzipStream = new GZipStream(inputStream, CompressionMode.Decompress);
        using var outputStream = new MemoryStream();
        
        await gzipStream.CopyToAsync(outputStream, cancellationToken);
        var json = Encoding.UTF8.GetString(outputStream.ToArray());
        
        return JsonSerializer.Deserialize<ChatSession>(json);
    }
    
    /// <summary>
    /// Save chat session to file with optional compression
    /// </summary>
    public async Task SaveSessionToFile(
        ChatSession session, 
        string filePath, 
        bool forceCompression = false,
        CancellationToken cancellationToken = default)
    {
        // Ensure directory exists
        var directoryPath = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(directoryPath) && !Directory.Exists(directoryPath))
        {
            Directory.CreateDirectory(directoryPath);
        }
        
        // Serialize to JSON first
        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
        
        var json = JsonSerializer.Serialize(session, options);
        var uncompressedSize = Encoding.UTF8.GetByteCount(json);
        
        // Decide whether to compress
        bool shouldCompress = forceCompression || 
                            (_config.EnableCompression && uncompressedSize >= _config.MinFileSizeForCompression);
        
        string actualFilePath = filePath;
        
        if (shouldCompress)
        {
            // Compress and save with .gz extension
            var compressedData = await CompressSession(session, cancellationToken);
            actualFilePath = filePath + ".gz";
            
            var tempPath = actualFilePath + ".tmp";
            await File.WriteAllBytesAsync(tempPath, compressedData, cancellationToken);
            
            if (File.Exists(actualFilePath))
                File.Delete(actualFilePath);
            
            File.Move(tempPath, actualFilePath);
            
            // Update session metadata
            session.IsCompressed = true;
            session.OriginalSize = uncompressedSize;
            session.CompressedSize = compressedData.Length;
            
            // Also remove uncompressed version if exists
            if (File.Exists(filePath))
                File.Delete(filePath);
        }
        else
        {
            // Save uncompressed
            var tempPath = filePath + ".tmp";
            await File.WriteAllTextAsync(tempPath, json, cancellationToken);
            
            if (File.Exists(filePath))
                File.Delete(filePath);
            
            File.Move(tempPath, filePath);
            
            session.IsCompressed = false;
            session.OriginalSize = uncompressedSize;
            session.CompressedSize = 0;
            
            // Remove compressed version if exists
            if (File.Exists(filePath + ".gz"))
                File.Delete(filePath + ".gz");
        }
    }
    
    /// <summary>
    /// Load chat session from file (handles both compressed and uncompressed)
    /// </summary>
    public async Task<ChatSession?> LoadSessionFromFile(string filePath, CancellationToken cancellationToken = default)
    {
        string compressedPath = filePath + ".gz";
        
        // Check compressed version first
        if (File.Exists(compressedPath))
        {
            try
            {
                var compressedData = await File.ReadAllBytesAsync(compressedPath, cancellationToken);
                var session = await DecompressSession(compressedData, cancellationToken);
                
                if (session != null)
                {
                    return session;
                }
            }
            catch (Exception ex)
            {
                // Fall through to try uncompressed version
            }
        }
        
        // Try uncompressed version
        if (File.Exists(filePath))
        {
            try
            {
                var json = await File.ReadAllTextAsync(filePath, cancellationToken);
                var session = JsonSerializer.Deserialize<ChatSession>(json);
                return session;
            }
            catch (Exception ex)
            {
            }
        }
        
        return null;
    }
    
    /// <summary>
    /// Compress existing history file in place
    /// </summary>
    public async Task CompressExistingFile(string filePath, CancellationToken cancellationToken = default)
    {
        if (!File.Exists(filePath))
            throw new FileNotFoundException($"File not found: {filePath}");
        
        var session = await LoadSessionFromFile(filePath, cancellationToken);
        if (session == null)
            throw new InvalidOperationException($"Failed to load session from {filePath}");
        
        await SaveSessionToFile(session, filePath, forceCompression: true, cancellationToken);
    }
    
    /// <summary>
    /// Decompress existing .gz file to regular JSON
    /// </summary>
    public async Task DecompressExistingFile(string filePath, CancellationToken cancellationToken = default)
    {
        string compressedPath = filePath.EndsWith(".gz") ? filePath : filePath + ".gz";
        
        if (!File.Exists(compressedPath))
            throw new FileNotFoundException($"Compressed file not found: {compressedPath}");
        
        var session = await LoadSessionFromFile(filePath.Replace(".gz", ""), cancellationToken);
        if (session == null)
            throw new InvalidOperationException($"Failed to decompress session from {compressedPath}");
        
        // Save uncompressed
        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
        
        var json = JsonSerializer.Serialize(session, options);
        string outputPath = filePath.Replace(".gz", "");
        await File.WriteAllTextAsync(outputPath, json, cancellationToken);
    }
    
    /// <summary>
    /// Get compression statistics for a file
    /// </summary>
    public async Task<CompressionStats> GetCompressionStats(string filePath, CancellationToken cancellationToken = default)
    {
        var stats = new CompressionStats { FilePath = filePath };
        
        string compressedPath = filePath + ".gz";
        
        if (File.Exists(compressedPath))
        {
            stats.IsCompressed = true;
            stats.CompressedSize = new FileInfo(compressedPath).Length;
            
            var session = await LoadSessionFromFile(filePath, cancellationToken);
            if (session != null)
            {
                stats.OriginalSize = session.OriginalSize > 0 ? session.OriginalSize : 
                    Encoding.UTF8.GetByteCount(JsonSerializer.Serialize(session));
                stats.MessageCount = session.Messages.Count;
            }
        }
        else if (File.Exists(filePath))
        {
            stats.IsCompressed = false;
            stats.OriginalSize = new FileInfo(filePath).Length;
            
            var session = await LoadSessionFromFile(filePath, cancellationToken);
            if (session != null)
            {
                stats.MessageCount = session.Messages.Count;
                
                // Calculate potential compressed size
                var compressedData = await CompressSession(session, cancellationToken);
                stats.CompressedSize = compressedData.Length;
            }
        }
        
        return stats;
    }
    
    /// <summary>
    /// Batch compress multiple history files
    /// </summary>
    public async Task<List<CompressionStats>> BatchCompressFiles(
        IEnumerable<string> filePaths, 
        CancellationToken cancellationToken = default)
    {
        var results = new List<CompressionStats>();
        
        foreach (var filePath in filePaths)
        {
            try
            {
                var beforeStats = await GetCompressionStats(filePath, cancellationToken);
                
                if (!beforeStats.IsCompressed)
                {
                    await CompressExistingFile(filePath, cancellationToken);
                    var afterStats = await GetCompressionStats(filePath, cancellationToken);
                    results.Add(afterStats);
                }
            }
            catch (Exception ex)
            {
            }
        }
        
        return results;
    }
}

/// <summary>
/// Compression statistics for a file
/// </summary>
public class CompressionStats
{
    public string FilePath { get; set; } = "";
    public bool IsCompressed { get; set; }
    public long OriginalSize { get; set; }
    public long CompressedSize { get; set; }
    public int MessageCount { get; set; }
    
    public double CompressionRatio => OriginalSize > 0 ? (double)CompressedSize / OriginalSize : 0;
    public long SpaceSaved => OriginalSize - CompressedSize;
    
    public override string ToString()
    {
        if (!IsCompressed)
            return $"{Path.GetFileName(FilePath)}: {OriginalSize:N0} bytes (uncompressed) - " +
                   $"Could save {SpaceSaved:N0} bytes ({(1 - CompressionRatio) * 100:F1}%)";
        
        return $"{Path.GetFileName(FilePath)}: {OriginalSize:N0} → {CompressedSize:N0} bytes " +
               $"({CompressionRatio * 100:F1}% of original, saved {SpaceSaved:N0} bytes)";
    }
}
