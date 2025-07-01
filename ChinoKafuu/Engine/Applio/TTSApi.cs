using System.Net.Http.Json;
using System.Text.Json.Serialization;

public class TTSRequest
{
    [JsonPropertyName("text")]
    public string Text { get; set; }
    
    [JsonPropertyName("guild_id")]
    public string guild_id { get; set; }
    
    [JsonPropertyName("voice")]
    public string Voice { get; set; } = "ja-JP-NanamiNeural";
    
    [JsonPropertyName("rate")]
    public int Rate { get; set; } = 0;
    
    [JsonPropertyName("pitch")]
    public int Pitch { get; set; } = 4;
    
    [JsonPropertyName("index_rate")]
    public float IndexRate { get; set; } = 0.5f;
    
    [JsonPropertyName("volume_envelope")]
    public int VolumeEnvelope { get; set; } = 1;
    
    [JsonPropertyName("protect")]
    public float Protect { get; set; } = 0.4f;
    
    [JsonPropertyName("hop_length")]
    public int HopLength { get; set; } = 64;
    
    [JsonPropertyName("f0_method")]
    public string F0Method { get; set; } = "rmvpe";
    
    [JsonPropertyName("split_audio")]
    public bool SplitAudio { get; set; } = true;
    
    [JsonPropertyName("f0_autotune")]
    public bool F0Autotune { get; set; } = false;
    
    [JsonPropertyName("f0_autotune_strength")]
    public float F0AutotuneStrength { get; set; } = 0.12f;
    
    [JsonPropertyName("clean_audio")]
    public bool CleanAudio { get; set; } = true;
    
    [JsonPropertyName("clean_strength")]
    public float CleanStrength { get; set; } = 0.2f;
    
    [JsonPropertyName("export_format")]
    public string ExportFormat { get; set; } = "wav";
    
    [JsonPropertyName("embedder_model")]
    public string EmbedderModel { get; set; } = "contentvec";
    
    [JsonPropertyName("pth_path")]
    public string PthPath { get; set; } = "logs/chino-kafuu/chino-kafuu.pth";
    
    [JsonPropertyName("index_path")]
    public string IndexPath { get; set; } = "logs/chino-kafuu/chino-kafuu.index";
    
    [JsonPropertyName("f0_file")]
    public string F0File { get; set; } = "https://github.com/gradio-app/gradio/raw/main/test/test_files/sample_file.pdf";
    
    [JsonPropertyName("embedder_model_custom")]
    public string EmbedderModelCustom { get; set; } = null;
    
    [JsonPropertyName("gpu")]
    public int Gpu { get; set; } = 0; // Use task manager to check GPU. If u want use other GPU, change the number.
    
    [JsonPropertyName("cache_data_in_gpu")]
    public bool CacheDataInGpu { get; set; } = true; // If u want cache data in GPU, set to true.
}

public class TTSApi
{
    private readonly HttpClient _httpClient;
    private const string BASE_API_URL = "http://localhost:8000";

    public TTSApi(HttpClient httpClient)
    {
        _httpClient = httpClient;
        _httpClient.Timeout = TimeSpan.FromMinutes(5);
    }

    public async Task<string> GenerateTTS(string message, string guildId, CancellationToken cancellationToken = default)
    {
        var ttsRequest = new TTSRequest
        {
            Text = message,
            guild_id = guildId
        };
        HttpResponseMessage response = await _httpClient.PostAsJsonAsync($"{BASE_API_URL}/tts", ttsRequest, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            string responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new Exception($"Gọi api thất bại: {response.ReasonPhrase} - {responseContent}");
        }

        var result = await response.Content.ReadFromJsonAsync<Dictionary<string, string>>(cancellationToken: cancellationToken);
        return result["file_name"];
    }

    public async Task DownloadGeneratedTTS(ulong guildId, string fileName, string outputPath, CancellationToken cancellationToken = default)
    {
        string outputDirectory = Path.GetDirectoryName(outputPath);

        Directory.CreateDirectory(outputDirectory);

        string decodedFileName = System.Web.HttpUtility.UrlDecode(fileName);
        string downloadUrl = $"{BASE_API_URL}/get-generated/{guildId}/{decodedFileName}";

        HttpResponseMessage response = await _httpClient.GetAsync(downloadUrl, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new Exception($"Tải file thất bại: {response.ReasonPhrase}");
        }

        await using FileStream fs = new FileStream(outputPath, FileMode.Create, FileAccess.Write, FileShare.None);
        await response.Content.CopyToAsync(fs, cancellationToken);
    }
}
