using System.Text.Json;
using ChinoKafuu.Engine.Gemini.Models;

public class GeminiChat
{
    private readonly string _apiKey;
    private readonly HttpClient _httpClient;
    private const int MAX_CHAT_MESSAGES = 1000;
    private const int SAVE_BATCH_SIZE = 10;
    private const int CRITICAL_SAVE_THRESHOLD = 6;
    private const string API_URL = "https://generativelanguage.googleapis.com/v1beta/models/gemini-2.0-flash:generateContent";

    private readonly string _prompt;
    private readonly Dictionary<string, ChatHistoryCache> _chatHistoryCaches = new();
    private readonly System.Timers.Timer _autoSaveTimer;
    private readonly System.Timers.Timer _emergencySaveTimer;
    private volatile bool _isShuttingDown = false;

    public GeminiChat(string apiKey)
    {
        _apiKey = apiKey;
        _httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromMinutes(5)
        };
        string promptFilePath = Path.Combine(AppContext.BaseDirectory, "../../../Engine/Gemini/Prompt/prompt.txt");
        _prompt = File.ReadAllText(Path.GetFullPath(promptFilePath));
        
        // Set auto save every 5 minutes
        _autoSaveTimer = new System.Timers.Timer(TimeSpan.FromMinutes(2).TotalMilliseconds);
        _autoSaveTimer.Elapsed += async (sender, e) => await SaveAllDirtyCaches();
        _autoSaveTimer.Start();

        // Set emergency save every 30 seconds or the message is >= CRITICAL_SAVE_THRESHOLD
        _emergencySaveTimer = new System.Timers.Timer(TimeSpan.FromSeconds(30).TotalMilliseconds);
        _emergencySaveTimer.Elapsed += async (sender, e) => await EmergencySave();
        _emergencySaveTimer.Start();
        
        AppDomain.CurrentDomain.ProcessExit += OnProcessExit;
        Console.CancelKeyPress += OnCancelKeyPress;
    }

    private async Task<ChatSession> LoadChatHistory(string chatHistoryPath, CancellationToken cancellationToken = default)
    {
        if (_chatHistoryCaches.TryGetValue(chatHistoryPath, out var cache))
        {
            return cache.Session;
        }

        ChatSession chatSession;
        try
        {
            if (File.Exists(chatHistoryPath))
            {
                var historyJson = await File.ReadAllTextAsync(chatHistoryPath, cancellationToken);
                
                try
                {
                    chatSession = JsonSerializer.Deserialize<ChatSession>(historyJson) ?? new ChatSession();
                }
                catch
                {
                    chatSession = await ConvertOldFormatToNew(historyJson);
                }
            }
            else
            {
                chatSession = new ChatSession();
            }
        }
        catch (Exception ex)
        {
            chatSession = new ChatSession();
            Console.WriteLine($"[WARNING] Không thể load chat history: {ex.Message}");
        }

        _chatHistoryCaches[chatHistoryPath] = new ChatHistoryCache
        {
            Session = chatSession,
            UnsavedMessageCount = 0,
            LastSaved = DateTime.Now,
            IsDirty = false
        };

        return chatSession;
    }

    // If there is any old format chat history, convert it to the new format
    private async Task<ChatSession> ConvertOldFormatToNew(string oldFormatJson)
    {
        try
        {
            var oldMessages = JsonSerializer.Deserialize<List<Dictionary<string, object>>>(oldFormatJson) ?? new List<Dictionary<string, object>>();
            var newSession = new ChatSession();

            foreach (var oldMessage in oldMessages)
            {
                var newMessage = new ChatMessage
                {
                    Role = oldMessage["role"].ToString() ?? "",
                    Timestamp = DateTime.Now
                };

                var parts = oldMessage["parts"];
                if (parts is JsonElement jsonElement)
                {
                    var partsList = new List<string>();
                    if (jsonElement.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var element in jsonElement.EnumerateArray())
                        {
                            partsList.Add(element.GetString() ?? "");
                        }
                    }
                    newMessage.Parts = partsList.ToArray();
                }
                else if (parts is string[] stringArray)
                {
                    newMessage.Parts = stringArray;
                }

                newSession.Messages.Add(newMessage);
            }

            return newSession;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[WARNING] Không thể convert format cũ: {ex.Message}");
            return new ChatSession();
        }
    }

    // Save history chat by those methods:
    // If the cache has unsaved messages and the batch size is reached, save it.
    // If the cache is dirty but not enough unsaved messages, wait for the next auto-save.
    private async Task SaveChatHistory(string chatHistoryPath, bool forceWrite = false, CancellationToken cancellationToken = default)
    {
        if (_isShuttingDown && !forceWrite) return;
        
        if (!_chatHistoryCaches.TryGetValue(chatHistoryPath, out var cache))
            return;

        if (!forceWrite && cache.UnsavedMessageCount < SAVE_BATCH_SIZE)
        {
            return;
        }

        try
        {
            var directoryPath = Path.GetDirectoryName(chatHistoryPath);
            if (!Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }

            var session = cache.Session;
            if (session.Messages.Count > MAX_CHAT_MESSAGES)
            {
                session.Messages = session.Messages.TakeLast(MAX_CHAT_MESSAGES).ToList();
            }

            session.LastUpdated = DateTime.Now;
            session.Metadata["totalMessages"] = session.Messages.Count;
            session.Metadata["filePath"] = Path.GetFileName(chatHistoryPath);
            session.Metadata["lastSaveTime"] = DateTime.Now;

            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };

            var serializedHistory = JsonSerializer.Serialize(session, options);
            
            var tempPath = chatHistoryPath + ".tmp";
            await File.WriteAllTextAsync(tempPath, serializedHistory, cancellationToken);
            
            if (File.Exists(chatHistoryPath))
            {
                File.Delete(chatHistoryPath);
            }
            File.Move(tempPath, chatHistoryPath);

            cache.UnsavedMessageCount = 0;
            cache.LastSaved = DateTime.Now;
            cache.IsDirty = false;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ERROR] Không thể lưu chat history: {ex.Message}");
        }
    }

    private async Task SaveAllDirtyCaches()
    {
        if (_isShuttingDown) return;
        
        var saveTasks = new List<Task>();
        
        foreach (var kvp in _chatHistoryCaches.ToList())
        {
            if (kvp.Value.UnsavedMessageCount > 0)
            {
                saveTasks.Add(SaveChatHistory(kvp.Key, forceWrite: true));
            }
        }

        if (saveTasks.Count > 0)
        {
            await Task.WhenAll(saveTasks);
        }
    }

    private void AddMessageToCache(string chatHistoryPath, ChatMessage message)
    {
        if (!_chatHistoryCaches.TryGetValue(chatHistoryPath, out var cache))
        {
            cache = new ChatHistoryCache();
            _chatHistoryCaches[chatHistoryPath] = cache;
        }

        cache.Session.Messages.Add(message);
        cache.Session.LastUpdated = DateTime.Now;
        cache.UnsavedMessageCount++;
    }

    public async Task<string> RunGeminiAPI(string messageContent, string username, string chatHistoryPath, CancellationToken cancellationToken = default)
    {
        if (_isShuttingDown)
        {
            return "Hệ thống đang shutdown, vui lòng thử lại sau.";
        }
        
        try
        {
            var chatSession = await LoadChatHistory(chatHistoryPath, cancellationToken);

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

            var recentMessages = chatSession.Messages.TakeLast(150).ToList();
            
            foreach (var message in recentMessages)
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
                    temperature = 1.2f,
                    topK = 1,
                    topP = 1,
                    maxOutputTokens = 512,
                    responseMimeType = "text/plain"
                }
            };

            var json = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync($"{API_URL}?key={_apiKey}", content, cancellationToken);
            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                string responseMessage = response.ToString();
                Console.WriteLine(responseMessage);
                return "Xin lỗi, hiện tại em không thể trả lời được. Anh thử lại sau nhé~";
            }

            var responseData = JsonSerializer.Deserialize<JsonElement>(responseContent);
            var modelResponse = responseData.GetProperty("candidates")[0]
                                             .GetProperty("content")
                                             .GetProperty("parts")[0]
                                             .GetProperty("text")
                                             .GetString();

            AddMessageToCache(chatHistoryPath, new ChatMessage
            {
                Role = "user",
                Parts = new[] { formattedMessage },
                Timestamp = currentDateTime
            });

            AddMessageToCache(chatHistoryPath, new ChatMessage
            {
                Role = "model",
                Parts = new[] { modelResponse ?? "" },
                Timestamp = DateTime.Now
            });

            // It only save when the condition reached above
            await SaveChatHistory(chatHistoryPath, cancellationToken: cancellationToken);

            return modelResponse ?? "Có lỗi khi gọi Api đến Gemini";
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ERROR] Lỗi: {ex.Message}");
            Console.WriteLine($"[ERROR] Stack trace: {ex.StackTrace}");
            return "Xin lỗi, hiện tại em không thể trả lời được. Anh thử lại sau nhé~";
        }
    }

    public async Task SaveAllCaches() => await SaveAllDirtyCaches();

    public void ClearChatCache(string chatHistoryPath) => _chatHistoryCaches.Remove(chatHistoryPath);

    public Dictionary<string, object> GetCacheStats()
    {
        return new Dictionary<string, object>
        {
            { "TotalCaches", _chatHistoryCaches.Count },
            { "TotalUnsavedMessages", _chatHistoryCaches.Values.Sum(c => c.UnsavedMessageCount) },
            { "DirtyCaches", _chatHistoryCaches.Values.Count(c => c.IsDirty) },
            { "CriticalCaches", _chatHistoryCaches.Values.Count(c => c.UnsavedMessageCount >= CRITICAL_SAVE_THRESHOLD) },
            { "Sessions", _chatHistoryCaches.ToDictionary(kvp => kvp.Key, kvp => new {
                SessionId = kvp.Value.Session.SessionId,
                MessageCount = kvp.Value.Session.Messages.Count,
                LastUpdated = kvp.Value.Session.LastUpdated,
                UnsavedCount = kvp.Value.UnsavedMessageCount,
                IsCritical = kvp.Value.UnsavedMessageCount >= CRITICAL_SAVE_THRESHOLD
            })}
        };
    }

    public ChatSession? GetChatSession(string chatHistoryPath) => _chatHistoryCaches.TryGetValue(chatHistoryPath, out var cache) ? cache.Session : null;

    public async Task<List<ChatMessage>> GetMessagesInTimeRange(string chatHistoryPath, DateTime from, DateTime to)
    {
        var session = await LoadChatHistory(chatHistoryPath);
        return session.Messages.Where(m => m.Timestamp >= from && m.Timestamp <= to).ToList();
    }

    public void Dispose()
    {
        GracefulShutdown().Wait();
        _autoSaveTimer?.Dispose();
        _emergencySaveTimer?.Dispose();
        _httpClient?.Dispose();
    }

    #region Handle shutdown or unexpected termination
    private async void OnProcessExit(object? sender, EventArgs e)
    {
        await GracefulShutdown();
    }

    private async void OnCancelKeyPress(object? sender, ConsoleCancelEventArgs e)
    {
        e.Cancel = true;
        await GracefulShutdown();
        Environment.Exit(0);
    }

    private async Task GracefulShutdown()
    {
        if (_isShuttingDown) return;
        _isShuttingDown = true;

        Console.WriteLine("[SHUTDOWN] Đang lưu tất cả dữ liệu trước khi tắt...");

        _autoSaveTimer?.Stop();
        _emergencySaveTimer?.Stop();

        try
        {
            await SaveAllCaches();
            Console.WriteLine("[SHUTDOWN] Đã lưu xong tất cả dữ liệu!");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[SHUTDOWN ERROR] {ex.Message}");
        }
    }

    private async Task EmergencySave()
    {
        if (_isShuttingDown) return;

        try
        {
            var urgentCaches = _chatHistoryCaches.Where(kvp =>
                kvp.Value.UnsavedMessageCount >= CRITICAL_SAVE_THRESHOLD).ToList();

            if (urgentCaches.Any())
            {
                var saveTasks = urgentCaches.Select(kvp =>
                    SaveChatHistory(kvp.Key, forceWrite: true)).ToArray();
                await Task.WhenAll(saveTasks);

                Console.WriteLine($"[EMERGENCY SAVE] Đã lưu {urgentCaches.Count} chat có dữ liệu quan trọng");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[EMERGENCY SAVE ERROR] {ex.Message}");
        }
    }
    #endregion
}
