using System.Text.Json;
using ChinoKafuu.Engine.Gemini.Config;
using ChinoKafuu.Engine.Gemini.Models;
using ChinoKafuu.Engine.Gemini.Services;

namespace ChinoKafuu.Engine.Gemini;

/// <summary>
/// Gemini Chat with integrated optimization features
/// </summary>
public class GeminiChat : IDisposable
{
    private readonly string _apiKey;
    private readonly HttpClient _httpClient;
    private readonly GeminiConfig _config;
    private const string API_URL = "https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash:generateContent";

    private readonly string _prompt;
    
    // Optimization services
    private readonly TokenManagementService _tokenService;
    private readonly HistoryCompressionService _compressionService;
    private readonly MessageSummarizationService _summarizationService;
    private readonly TieredStorageManager _storageManager;
    private readonly SummaryStorageService _summaryStorageService;
    
    // File-based save throttling (per path)
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
        
        // Initialize optimization services
        _tokenService = new TokenManagementService(_config);
        _compressionService = new HistoryCompressionService(_config);
        _summarizationService = new MessageSummarizationService(apiKey, _config, _tokenService);
        _storageManager = new TieredStorageManager(_config, _tokenService, _compressionService);
        _summaryStorageService = new SummaryStorageService(_config, _compressionService);
    }

    /// <summary>
    /// Load chat history from file (lazy loading - NO memory cache)
    /// Also loads summaries if they exist
    /// </summary>
    private async Task<ChatSession> LoadChatHistory(string chatHistoryPath, CancellationToken cancellationToken = default)
    {
        ChatSession chatSession;
        try
        {
            // Load main history
            chatSession = await _compressionService.LoadSessionFromFile(chatHistoryPath, cancellationToken) 
                ?? new ChatSession();
            
            // Load summaries from separate file
            var summaries = await _summaryStorageService.LoadSummaries(chatHistoryPath, cancellationToken);
            if (summaries.Count > 0)
            {
                // Prepend summaries to message list (they represent old conversations)
                chatSession.Messages.InsertRange(0, summaries);
                Console.WriteLine($"[LAZY LOAD] Loaded {summaries.Count} summaries representing {summaries.Sum(s => s.SummarizedMessageCount)} original messages");
            }
        }
        catch (Exception ex)
        {
            chatSession = new ChatSession();
            Console.WriteLine($"[WARNING] Không thể load chat history: {ex.Message}");
        }

        return chatSession;
    }

    /// <summary>
    /// Save chat history to file immediately (NO memory cache)
    /// Summaries are saved to separate file
    /// </summary>
    private async Task SaveChatHistory(
        ChatSession session, 
        string chatHistoryPath, 
        bool force = false, 
        CancellationToken cancellationToken = default)
    {
        // Throttle saves unless forced
        if (!force && _lastSaveTimes.TryGetValue(chatHistoryPath, out var lastSave) 
            && (DateTime.Now - lastSave).TotalSeconds < 5)
        {
            return;
        }

        await _saveSemaphore.WaitAsync(cancellationToken);
        
        try
        {
            // Separate summaries from regular messages
            var summaries = session.Messages.Where(m => m.IsSummary).ToList();
            var regularMessages = session.Messages.Where(m => !m.IsSummary).ToList();
            
            // Update session metadata
            session.LastUpdated = DateTime.Now;
            session.Metadata["totalMessages"] = regularMessages.Count;
            session.Metadata["summaryCount"] = summaries.Count;
            session.Metadata["filePath"] = Path.GetFileName(chatHistoryPath);
            session.Metadata["lastSaveTime"] = DateTime.Now;
            
            // Update token statistics (only for regular messages)
            var tempSession = new ChatSession { Messages = regularMessages };
            var tokenStats = _tokenService.GetTokenStats(tempSession);
            session.TotalTokensUsed = tokenStats.TotalTokens;
            session.ActiveMessagesCount = tokenStats.ActiveMessages;
            session.Metadata["tokenStats"] = tokenStats.ToString();
            
            // Save regular messages to main history file
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

            // Save summaries to separate file
            if (summaries.Count > 0)
            {
                await _summaryStorageService.SaveSummaries(chatHistoryPath, summaries, cancellationToken);
            }
            
            _lastSaveTimes[chatHistoryPath] = DateTime.Now;
            
            Console.WriteLine($"[SAVE] {Path.GetFileName(chatHistoryPath)}: {regularMessages.Count} msgs + {summaries.Count} summaries, {tokenStats.TotalTokens:N0} tokens");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ERROR] Không thể lưu chat history: {ex.Message}");
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
            // Load from file (lazy loading - no cache)
            var chatSession = await LoadChatHistory(chatHistoryPath, cancellationToken);

            // ========== OPTIMIZATION PHASE ==========
            
            // Step 1: Update importance scores and storage tiers
            _tokenService.UpdateImportanceScores(chatSession);
            _storageManager.OrganizeMessageTiers(chatSession);
            
            // Step 2: Check if summarization is needed
            if (_summarizationService.ShouldSummarize(chatSession))
            {
                Console.WriteLine("[OPTIMIZATION] Triggering automatic summarization...");
                
                // Separate summaries from regular messages
                var existingSummaries = chatSession.Messages.Where(m => m.IsSummary).ToList();
                var regularMessages = chatSession.Messages.Where(m => !m.IsSummary).ToList();
                
                // Summarize only regular messages
                var tempSession = new ChatSession { Messages = regularMessages };
                var summarizedSession = await _summarizationService.SummarizeOldMessages(tempSession, cancellationToken);
                
                // Merge back: summaries + new summaries + remaining messages
                var newSummaries = summarizedSession.Messages.Where(m => m.IsSummary).ToList();
                var remainingMessages = summarizedSession.Messages.Where(m => !m.IsSummary).ToList();
                
                chatSession.Messages.Clear();
                chatSession.Messages.AddRange(existingSummaries);
                chatSession.Messages.AddRange(newSummaries);
                chatSession.Messages.AddRange(remainingMessages);
                
                // Archive original messages if configured
                if (_config.ArchiveOriginalMessagesAfterSummarization && newSummaries.Count > 0)
                {
                    // Archive messages that were summarized
                    // (Messages that are in regularMessages but NOT in remainingMessages)
                    var remainingMessageIds = new HashSet<string>(remainingMessages.Select(m => m.Id));
                    var messagesToArchive = regularMessages
                        .Where(m => !remainingMessageIds.Contains(m.Id) && !m.IsSummary)
                        .ToList();
                    
                    if (messagesToArchive.Count > 0)
                    {
                        await _summaryStorageService.ArchiveMessages(chatHistoryPath, messagesToArchive, cancellationToken);
                        Console.WriteLine($"[ARCHIVE] Archived {messagesToArchive.Count} original messages");
                    }
                }
                
                // Save immediately after summarization
                await SaveChatHistory(chatSession, chatHistoryPath, force: true, cancellationToken: cancellationToken);
            }
            
            // Step 3: Get optimized messages for API call (exclude summaries)
            var messagesForAPI = chatSession.Messages.Where(m => !m.IsSummary).ToList();
            var tempSessionForAPI = new ChatSession { Messages = messagesForAPI };
            var optimizedMessages = _tokenService.GetOptimalMessagesForAPI(tempSessionForAPI);
            var tokenStats = _tokenService.GetTokenStats(tempSessionForAPI);
            
            Console.WriteLine($"[TOKEN OPTIMIZATION] {tokenStats}");
            Console.WriteLine($"[TOKEN OPTIMIZATION] Sending {optimizedMessages.Count}/{messagesForAPI.Count} regular messages to API");

            // ========== API CALL PHASE ==========
            
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

            // Add optimized messages to context
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
                Console.WriteLine($"[ERROR] API Error: {response.StatusCode}");
                Console.WriteLine($"[ERROR] Response: {responseContent}");
                return "Xin lỗi, hiện tại em không thể trả lời được. Anh thử lại sau nhé~";
            }

            var responseData = JsonSerializer.Deserialize<JsonElement>(responseContent);
            
            // Kiểm tra xem response có đúng format không
            if (!responseData.TryGetProperty("candidates", out var candidates) || candidates.GetArrayLength() == 0)
            {
                Console.WriteLine($"[ERROR] Invalid response format: {responseContent}");
                return "Xin lỗi, em nhận được phản hồi không hợp lệ từ server. Anh thử lại sau nhé~";
            }

            var candidate = candidates[0];
            
            // Kiểm tra xem có bị block không (safety ratings)
            if (candidate.TryGetProperty("finishReason", out var finishReason))
            {
                var reason = finishReason.GetString();
                if (reason == "SAFETY" || reason == "RECITATION" || reason == "OTHER")
                {
                    Console.WriteLine($"[WARNING] Response blocked: {reason}");
                    return "Xin lỗi, em không thể trả lời câu hỏi này. Anh thử hỏi theo cách khác nhé~";
                }
            }

            // Lấy content một cách an toàn
            if (!candidate.TryGetProperty("content", out var contentProp) ||
                !contentProp.TryGetProperty("parts", out var parts) ||
                parts.GetArrayLength() == 0 ||
                !parts[0].TryGetProperty("text", out var textProp))
            {
                Console.WriteLine($"[ERROR] Cannot extract text from response: {responseContent}");
                return "Xin lỗi, em không thể xử lý phản hồi. Anh thử lại sau nhé~";
            }

            var modelResponse = textProp.GetString();

            // ========== SAVE PHASE ==========
            
            // Create new messages with metadata (reuse currentDateTime from above)
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
            
            // Set optimization metadata for both messages
            int positionFromEnd = chatSession.Messages.Count(m => !m.IsSummary);
            userMessage.ImportanceScore = _tokenService.CalculateImportanceScore(userMessage, positionFromEnd);
            userMessage.StorageTier = _config.GetStorageTier(userMessage.Timestamp);
            userMessage.EstimatedTokenCount = _tokenService.EstimateMessageTokens(userMessage);
            
            modelMessage.ImportanceScore = _tokenService.CalculateImportanceScore(modelMessage, positionFromEnd + 1);
            modelMessage.StorageTier = _config.GetStorageTier(modelMessage.Timestamp);
            modelMessage.EstimatedTokenCount = _tokenService.EstimateMessageTokens(modelMessage);
            
            // Add to session and save immediately
            chatSession.Messages.Add(userMessage);
            chatSession.Messages.Add(modelMessage);
            
            await SaveChatHistory(chatSession, chatHistoryPath, force: true, cancellationToken: cancellationToken);

            return modelResponse ?? "Có lỗi khi gọi Api đến Gemini";
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ERROR] Lỗi: {ex.Message}");
            Console.WriteLine($"[ERROR] Stack trace: {ex.StackTrace}");
            return "Xin lỗi, hiện tại em không thể trả lời được. Anh thử lại sau nhé~";
        }
    }


    /// <summary>
    /// Manually trigger storage optimization (file-based)
    /// </summary>
    public async Task<StorageOptimizationResult?> OptimizeStorage(string chatHistoryPath, CancellationToken cancellationToken = default)
    {
        var session = await LoadChatHistory(chatHistoryPath, cancellationToken);
        var result = await _storageManager.OptimizeStorage(session, chatHistoryPath, cancellationToken);
        await SaveChatHistory(session, chatHistoryPath, force: true, cancellationToken: cancellationToken);
        
        return result;
    }

    /// <summary>
    /// Get comprehensive statistics about a chat session (lazy load from file)
    /// </summary>
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

    /// <summary>
    /// Clear file-based save throttle cache
    /// </summary>
    public void ClearChatCache(string chatHistoryPath) => _lastSaveTimes.Remove(chatHistoryPath);

    /// <summary>
    /// Get information about all tracked chat paths
    /// </summary>
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

    /// <summary>
    /// Get chat session from file (lazy load)
    /// </summary>
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

    /// <summary>
    /// Get messages in time range (lazy load from file)
    /// </summary>
    public async Task<List<ChatMessage>> GetMessagesInTimeRange(
        string chatHistoryPath, 
        DateTime from, 
        DateTime to, 
        CancellationToken cancellationToken = default)
    {
        var session = await LoadChatHistory(chatHistoryPath, cancellationToken);
        return session.Messages.Where(m => m.Timestamp >= from && m.Timestamp <= to).ToList();
    }

    /// <summary>
    /// Get storage information for a specific chat
    /// </summary>
    public async Task<StorageInfo> GetStorageInfo(string chatHistoryPath)
    {
        return await _summaryStorageService.GetStorageInfo(chatHistoryPath);
    }

    /// <summary>
    /// Manually trigger archive cleanup
    /// </summary>
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
