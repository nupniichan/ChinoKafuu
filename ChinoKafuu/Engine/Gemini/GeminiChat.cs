using System.Text.Json;
using ChinoKafuu.Engine.Gemini.Config;
using ChinoKafuu.Engine.Gemini.Models;
using ChinoKafuu.Engine.Gemini.Services;

namespace ChinoKafuu.Engine.Gemini;

public class GeminiChat : IDisposable
{
    private readonly string _apiKey;
    private readonly HttpClient _httpClient;
    private readonly GeminiConfig _config;
    private const string API_URL = "https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash:generateContent";

    private readonly string _prompt;
    
    private readonly TokenManagementService _tokenService;
    private readonly HistoryCompressionService _compressionService;
    private readonly MessageSummarizationService _summarizationService;
    private readonly TieredStorageManager _storageManager;
    private readonly SummaryStorageService _summaryStorageService;
    
    private readonly Dictionary<string, DateTime> _lastSaveTimes = new();
    private readonly SemaphoreSlim _saveSemaphore = new(1, 1);

    public GeminiChat(string apiKey, GeminiConfig? config = null)
    {
        _apiKey = apiKey;
        _config = config ?? GeminiConfig.Instance;
        
        _httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromMinutes(_config.ApiTimeoutMinutes)
        };
        
        string promptFilePath = Path.Combine(AppContext.BaseDirectory, "../../../Engine/Gemini/Prompt/prompt.txt");
        _prompt = File.ReadAllText(Path.GetFullPath(promptFilePath));
        
        _tokenService = new TokenManagementService(_config);
        _compressionService = new HistoryCompressionService(_config);
        _summarizationService = new MessageSummarizationService(apiKey, _config, _tokenService);
        _storageManager = new TieredStorageManager(_config, _tokenService, _compressionService);
        _summaryStorageService = new SummaryStorageService(_config, _compressionService);
    }

    private async Task<ChatSession> LoadChatHistory(string chatHistoryPath, CancellationToken cancellationToken = default)
    {
        ChatSession chatSession;
        try
        {
            chatSession = await _compressionService.LoadSessionFromFile(chatHistoryPath, cancellationToken) 
                ?? new ChatSession();
            
            var summaries = await _summaryStorageService.LoadSummaries(chatHistoryPath, cancellationToken);
            if (summaries.Count > 0)
            {
                chatSession.Messages.InsertRange(0, summaries);
            }
        }
        catch (Exception)
        {
            chatSession = new ChatSession();
        }

        return chatSession;
    }

    private async Task SaveChatHistory(
        ChatSession session, 
        string chatHistoryPath, 
        bool force = false, 
        CancellationToken cancellationToken = default)
    {
        if (!force && _lastSaveTimes.TryGetValue(chatHistoryPath, out var lastSave) 
            && (DateTime.Now - lastSave).TotalSeconds < 5)
        {
            return;
        }

        await _saveSemaphore.WaitAsync(cancellationToken);
        
        try
        {
            var summaries = session.Messages.Where(m => m.IsSummary).ToList();
            var regularMessages = session.Messages.Where(m => !m.IsSummary).ToList();
            
            session.LastUpdated = DateTime.Now;
            session.Metadata["totalMessages"] = regularMessages.Count;
            session.Metadata["summaryCount"] = summaries.Count;
            session.Metadata["filePath"] = Path.GetFileName(chatHistoryPath);
            session.Metadata["lastSaveTime"] = DateTime.Now;
            
            var tempSession = new ChatSession { Messages = regularMessages };
            var tokenStats = _tokenService.GetTokenStats(tempSession);
            session.TotalTokensUsed = tokenStats.TotalTokens;
            session.ActiveMessagesCount = tokenStats.ActiveMessages;
            session.Metadata["tokenStats"] = tokenStats.ToString();
            
            var mainSession = new ChatSession
            {
                SessionId = session.SessionId,
                UserId = session.UserId,
                CreatedAt = session.CreatedAt,
                LastUpdated = session.LastUpdated,
                Messages = regularMessages,
                TotalTokensUsed = session.TotalTokensUsed,
                ActiveMessagesCount = session.ActiveMessagesCount,
                IsCompressed = session.IsCompressed,
                OriginalSize = session.OriginalSize,
                CompressedSize = session.CompressedSize,
                Metadata = session.Metadata
            };
            
            await _compressionService.SaveSessionToFile(mainSession, chatHistoryPath, cancellationToken: cancellationToken);

            if (summaries.Count > 0)
            {
                await _summaryStorageService.SaveSummaries(chatHistoryPath, summaries, cancellationToken);
            }
            
            _lastSaveTimes[chatHistoryPath] = DateTime.Now;
        }
        catch (Exception)
        {
        }
        finally
        {
            _saveSemaphore.Release();
        }
    }

    public async Task<string> RunGeminiAPI(string messageContent, string username, string chatHistoryPath, CancellationToken cancellationToken = default)
    {
        try
        {
            var chatSession = await LoadChatHistory(chatHistoryPath, cancellationToken);

            _tokenService.UpdateImportanceScores(chatSession);
            _storageManager.OrganizeMessageTiers(chatSession);
            
            if (_summarizationService.ShouldSummarize(chatSession))
            {
                var existingSummaries = chatSession.Messages.Where(m => m.IsSummary).ToList();
                var regularMessages = chatSession.Messages.Where(m => !m.IsSummary).ToList();
                
                var tempSession = new ChatSession { Messages = regularMessages };
                var summarizedSession = await _summarizationService.SummarizeOldMessages(tempSession, cancellationToken);
                
                var newSummaries = summarizedSession.Messages.Where(m => m.IsSummary).ToList();
                var remainingMessages = summarizedSession.Messages.Where(m => !m.IsSummary).ToList();
                
                chatSession.Messages.Clear();
                chatSession.Messages.AddRange(existingSummaries);
                chatSession.Messages.AddRange(newSummaries);
                chatSession.Messages.AddRange(remainingMessages);
                
                if (_config.ArchiveOriginalMessagesAfterSummarization && newSummaries.Count > 0)
                {
                    var remainingMessageIds = new HashSet<string>(remainingMessages.Select(m => m.Id));
                    var messagesToArchive = regularMessages
                        .Where(m => !remainingMessageIds.Contains(m.Id) && !m.IsSummary)
                        .ToList();
                    
                    if (messagesToArchive.Count > 0)
                    {
                        await _summaryStorageService.ArchiveMessages(chatHistoryPath, messagesToArchive, cancellationToken);
                    }
                }
                
                await SaveChatHistory(chatSession, chatHistoryPath, force: true, cancellationToken: cancellationToken);
            }
            
            var messagesForAPI = chatSession.Messages.Where(m => !m.IsSummary).ToList();
            var tempSessionForAPI = new ChatSession { Messages = messagesForAPI };
            var optimizedMessages = _tokenService.GetOptimalMessagesForAPI(tempSessionForAPI);
            var tokenStats = _tokenService.GetTokenStats(tempSessionForAPI);

            var contents = new List<object>
            {
                new
                {
                    role = "user",
                    parts = new[] { new { text = _prompt } }
                },
                new
                {
                    role = "model",
                    parts = new[] { new { text = "Vâng, em hiểu rồi ạ. Em sẽ cố gắng hết sức để nhập vai Chino Kafuu một cách tốt nhất. Mong anh sẽ ghé Rabbit House thường xuyên ạ `(˶ᵔ ᵕ ᵔ˶)`" } }
                }
            };

            foreach (var message in optimizedMessages)
            {
                contents.Add(new
                {
                    role = message.Role,
                    parts = message.Parts.Select(part => new { text = part }).ToArray()
                });
            }

            DateTime currentDateTime = DateTime.Now;
            string formattedMessage = $"{username} ({currentDateTime:dd/MM/yyyy HH:mm:ss}): {messageContent}";

            contents.Add(new
            {
                role = "user",
                parts = new[] { new { text = formattedMessage } }
            });

            var requestBody = new
            {
                contents = contents,
                generationConfig = new
                {
                    temperature = _config.Temperature,
                    topK = _config.TopK,
                    topP = _config.TopP,
                    maxOutputTokens = _config.MaxOutputTokens,
                    responseMimeType = "text/plain"
                }
            };

            var json = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync($"{API_URL}?key={_apiKey}", content, cancellationToken);
            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                return "Xin lỗi, hiện tại em không thể trả lời được. Anh thử lại sau nhé~";
            }

            var responseData = JsonSerializer.Deserialize<JsonElement>(responseContent);
            
            if (!responseData.TryGetProperty("candidates", out var candidates) || candidates.GetArrayLength() == 0)
            {
                return "Xin lỗi, em nhận được phản hồi không hợp lệ từ server. Anh thử lại sau nhé~";
            }

            var candidate = candidates[0];
            
            if (candidate.TryGetProperty("finishReason", out var finishReason))
            {
                var reason = finishReason.GetString();
                if (reason == "SAFETY" || reason == "RECITATION" || reason == "OTHER")
                {
                    return "Xin lỗi, em không thể trả lời câu hỏi này. Anh thử hỏi theo cách khác nhé~";
                }
            }

            if (!candidate.TryGetProperty("content", out var contentProp) ||
                !contentProp.TryGetProperty("parts", out var parts) ||
                parts.GetArrayLength() == 0 ||
                !parts[0].TryGetProperty("text", out var textProp))
            {
                return "Xin lỗi, em không thể xử lý phản hồi. Anh thử lại sau nhé~";
            }

            var modelResponse = textProp.GetString();

            var userMessage = new ChatMessage
            {
                Role = "user",
                Parts = new[] { formattedMessage },
                Timestamp = currentDateTime
            };
            
            var modelMessage = new ChatMessage
            {
                Role = "model",
                Parts = new[] { modelResponse ?? "" },
                Timestamp = DateTime.Now
            };
            
            int positionFromEnd = chatSession.Messages.Count(m => !m.IsSummary);
            userMessage.ImportanceScore = _tokenService.CalculateImportanceScore(userMessage, positionFromEnd);
            userMessage.StorageTier = _config.GetStorageTier(userMessage.Timestamp);
            userMessage.EstimatedTokenCount = _tokenService.EstimateMessageTokens(userMessage);
            
            modelMessage.ImportanceScore = _tokenService.CalculateImportanceScore(modelMessage, positionFromEnd + 1);
            modelMessage.StorageTier = _config.GetStorageTier(modelMessage.Timestamp);
            modelMessage.EstimatedTokenCount = _tokenService.EstimateMessageTokens(modelMessage);
            
            chatSession.Messages.Add(userMessage);
            chatSession.Messages.Add(modelMessage);
            
            await SaveChatHistory(chatSession, chatHistoryPath, force: true, cancellationToken: cancellationToken);

            return modelResponse ?? "Có lỗi khi gọi Api đến Gemini";
        }
        catch (Exception)
        {
            return "Xin lỗi, hiện tại em không thể trả lời được. Anh thử lại sau nhé~";
        }
    }

    public async Task<StorageOptimizationResult?> OptimizeStorage(string chatHistoryPath, CancellationToken cancellationToken = default)
    {
        var session = await LoadChatHistory(chatHistoryPath, cancellationToken);
        var result = await _storageManager.OptimizeStorage(session, chatHistoryPath, cancellationToken);
        await SaveChatHistory(session, chatHistoryPath, force: true, cancellationToken: cancellationToken);
        
        return result;
    }

    public async Task<Dictionary<string, object>> GetSessionStats(string chatHistoryPath, CancellationToken cancellationToken = default)
    {
        try
        {
            var session = await LoadChatHistory(chatHistoryPath, cancellationToken);
            
            var summaries = session.Messages.Where(m => m.IsSummary).ToList();
            var regularMessages = session.Messages.Where(m => !m.IsSummary).ToList();
            
            var tempSession = new ChatSession { Messages = regularMessages };
            var tokenStats = _tokenService.GetTokenStats(tempSession);
            var tierInfo = _storageManager.GetTierInfo(tempSession);
            var summarizationStats = _summarizationService.GetSummarizationStats(session);
            var storageInfo = await _summaryStorageService.GetStorageInfo(chatHistoryPath);

            return new Dictionary<string, object>
            {
                { "SessionId", session.SessionId },
                { "TotalMessages", regularMessages.Count },
                { "SummaryCount", summaries.Count },
                { "OriginalMessagesSummarized", summaries.Sum(s => s.SummarizedMessageCount) },
                { "LastUpdated", session.LastUpdated },
                { "TokenStats", tokenStats },
                { "TierInfo", tierInfo },
                { "SummarizationStats", summarizationStats },
                { "StorageInfo", storageInfo.ToString() },
                { "IsCompressed", session.IsCompressed },
                { "CompressionRatio", session.CompressedSize > 0 ? (double)session.CompressedSize / session.OriginalSize : 1.0 }
            };
        }
        catch (Exception ex)
        {
            return new Dictionary<string, object> { { "error", ex.Message } };
        }
    }

    public void ClearChatCache(string chatHistoryPath) => _lastSaveTimes.Remove(chatHistoryPath);

    public Dictionary<string, object> GetCacheStats()
    {
        return new Dictionary<string, object>
        {
            { "TrackedPaths", _lastSaveTimes.Count },
            { "Paths", _lastSaveTimes.Select(kvp => new {
                Path = kvp.Key,
                LastSaved = kvp.Value
            })}
        };
    }

    public async Task<ChatSession?> GetChatSession(string chatHistoryPath, CancellationToken cancellationToken = default)
    {
        try
        {
            return await LoadChatHistory(chatHistoryPath, cancellationToken);
        }
        catch
        {
            return null;
        }
    }

    public async Task<List<ChatMessage>> GetMessagesInTimeRange(
        string chatHistoryPath, 
        DateTime from, 
        DateTime to, 
        CancellationToken cancellationToken = default)
    {
        var session = await LoadChatHistory(chatHistoryPath, cancellationToken);
        return session.Messages.Where(m => m.Timestamp >= from && m.Timestamp <= to).ToList();
    }

    public async Task<StorageInfo> GetStorageInfo(string chatHistoryPath)
    {
        return await _summaryStorageService.GetStorageInfo(chatHistoryPath);
    }

    public async Task CleanupArchives(string chatHistoryPath, CancellationToken cancellationToken = default)
    {
        await _summaryStorageService.CleanupOldArchives(chatHistoryPath, cancellationToken);
    }

    public void Dispose()
    {
        _httpClient?.Dispose();
        _saveSemaphore?.Dispose();
    }
}
