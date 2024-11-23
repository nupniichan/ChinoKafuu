using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.IO;

public class TTSApi
{
    private readonly HttpClient _httpClient;
    public TTSApi()
    {
        _httpClient = new HttpClient();
    }

    public async Task<string> GenerateTTS(
        string message,
        string outputFolder = "resultfolder",
        string outputFile = "result.wav"
        )
    {
        try
        {
            Directory.CreateDirectory(outputFolder);
            string fullOutputPath = Path.Combine(outputFolder, outputFile);

            // Chuẩn bị dữ liệu request
            var requestData = new
            {
                data = new object[]
                {
                    message,                    // Text to speak
                    "ja-JP-NanamiNeural",      // Voice
                    0,                         // TTS rate
                    3,                         // Pitch
                    4,                         // Filter radius
                    0.6,                       // Index rate
                    1,                         // Volume envelope
                    0.5,                       // Protect
                    256,                       // Hop length
                    "rmvpe",                   // F0 method
                    "result",                  // Output TTS path
                    fullOutputPath,            // Output RVC path
                    @"logs\chino-kafuu\chino-kafuu.pth",           // PTH path
                    @"logs\chino-kafuu\added_IVF209_Flat_nprobe_1_chino-kafuu_v2.index", // Index path
                    false,                     // Split audio
                    true,                      // F0 autotune
                    true,                      // Clean audio
                    0.5,                       // Clean strength
                    "WAV",                     // Export format
                    false,                     // Upscale audio
                    new { path = "https://github.com/gradio-app/gradio/raw/main/test/test_files/sample_file.pdf" }, // F0 file
                    "contentvec",              // Embedder model
                    null                       // Embedder model custom
                }
            };

            // Gửi request đầu tiên để lấy EVENT_ID
            var content = new StringContent(
                JsonSerializer.Serialize(requestData),
                System.Text.Encoding.UTF8,
                "application/json");

            var response = await _httpClient.PostAsync("/call/run_tts_script", content);
            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"Initial request failed: {response.StatusCode}");
            }

            var responseContent = await response.Content.ReadAsStringAsync();
            var eventId = JsonSerializer.Deserialize<JsonElement>(responseContent)
                .GetProperty("event_id").GetString();

            // Gửi request thứ hai để lấy kết quả
            var resultResponse = await _httpClient.GetAsync($"/call/run_tts_script/{eventId}");
            if (!resultResponse.IsSuccessStatusCode)
            {
                throw new Exception($"Result request failed: {resultResponse.StatusCode}");
            }

            var result = await resultResponse.Content.ReadAsStringAsync();
            return result;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in TTS generation: {ex.Message}");
            throw;
        }
    }
}