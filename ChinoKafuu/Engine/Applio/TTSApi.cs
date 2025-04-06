using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

public class TTSRequest
{
    public string Text { get; set; }
    public string guild_id { get; set; }
    public string Voice { get; set; } = "ja-JP-NanamiNeural";
    public int Rate { get; set; } = 0;
    public int Pitch { get; set; } = 3;
    public int FilterRadius { get; set; } = 4;
    public float IndexRate { get; set; } = 0.6f;
    public int VolumeEnvelope { get; set; } = 1;
    public float Protect { get; set; } = 0.5f;
    public int HopLength { get; set; } = 256;
    public string F0Method { get; set; } = "rmvpe";
    public bool SplitAudio { get; set; } = false;
    public bool F0Autotune { get; set; } = false;
    public float F0AutotuneStrength { get; set; } = 0.12f;
    public bool CleanAudio { get; set; } = true;
    public float CleanStrength { get; set; } = 0.5f;
    public string ExportFormat { get; set; } = "wav";
    public string EmbedderModel { get; set; } = "contentvec";
    public string PthPath { get; set; } = "logs/chino-kafuu/chino-kafuu.pth";
    public string IndexPath { get; set; } = "logs/chino-kafuu/added_IVF209_Flat_nprobe_1_chino-kafuu_v2.index";
    public string F0File { get; set; } = "https://github.com/gradio-app/gradio/raw/main/test/test_files/sample_file.pdf";
    public string EmbedderModelCustom { get; set; } = null;
    public int Gpu { get; set; } = 0; // Use task manager to check GPU. If u want use other GPU, change the number.
    public bool CacheDataInGpu { get; set; } = true; // If u want cache data in GPU, set to true.
}

public class TTSApi
{
    private readonly HttpClient _httpClient;

    public TTSApi(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<string> GenerateTTS(string message, string guildId)
    {
        var ttsRequest = new TTSRequest
        {
            Text = message,
            guild_id = guildId
        };
        HttpResponseMessage response = await _httpClient.PostAsJsonAsync("http://localhost:8000/tts", ttsRequest);
        if (!response.IsSuccessStatusCode)
        {
            string responseContent = await response.Content.ReadAsStringAsync();
            throw new Exception($"Gọi api thất bại: {response.ReasonPhrase} - {responseContent}");
        }

        var result = await response.Content.ReadFromJsonAsync<Dictionary<string, string>>();
        return result["file_name"];
    }

    public async Task DownloadGeneratedTTS(ulong guildId, string fileName, string outputPath)
    {
        string outputDirectory = Path.GetDirectoryName(outputPath);

        Directory.CreateDirectory(outputDirectory);

        string baseApiUrl = "http://localhost:8000";
        string decodedFileName = System.Web.HttpUtility.UrlDecode(fileName);
        string downloadUrl = $"{baseApiUrl}/get-generated/{guildId}/{decodedFileName}";

        HttpResponseMessage response = await _httpClient.GetAsync(downloadUrl);

        if (!response.IsSuccessStatusCode)
        {
            throw new Exception($"Tải file thất bại: {response.ReasonPhrase}");
        }

        await using FileStream fs = new FileStream(outputPath, FileMode.Create, FileAccess.Write, FileShare.None);
        await response.Content.CopyToAsync(fs);
    }
}
