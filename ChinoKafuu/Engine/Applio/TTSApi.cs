using System;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

public class TTSApi
{
    private readonly HttpClient _httpClient;
    // cần sửa
    private readonly string URL = "http://127.0.0.1:5000";
    public TTSApi()
    {
        _httpClient = new HttpClient();
    }

    public async Task<string> GenerateTTS(
        string message,
        string outputFolder,
        string outputFile
    )
    {
        try
        {
            Directory.CreateDirectory(outputFolder); 

            // Chuẩn bị dữ liệu request
            var requestData = new
            {
                message = message,
                guildId = Path.GetFileName(outputFolder) 
            };

            // Gửi request đến Flask server
            var content = new StringContent(
                JsonSerializer.Serialize(requestData),
                System.Text.Encoding.UTF8,
                "application/json");

            var response = await _httpClient.PostAsync("http://127.0.0.1:5000/tts", content);
            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"Request failed: {response.StatusCode}");
            }

            // Parse kết quả từ server
            using var jsonDoc = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
            var root = jsonDoc.RootElement;

            if (root.GetProperty("success").GetBoolean())
            {
                string relativeFilePath = root.GetProperty("file_path").GetString();

                // Tải file từ Flask server
                var fileResponse = await _httpClient.GetAsync($"http://127.0.0.1:5000/download?file={Uri.EscapeDataString(relativeFilePath)}");
                if (!fileResponse.IsSuccessStatusCode)
                {
                    throw new Exception($"File download failed: {fileResponse.StatusCode}");
                }

                string fullOutputPath = Path.Combine(outputFolder, outputFile);

                // Lưu file vào thư mục đầu ra
                await using var fileStream = new FileStream(fullOutputPath, FileMode.Create, FileAccess.Write);
                await fileResponse.Content.CopyToAsync(fileStream);

                return fullOutputPath; // Trả về đường dẫn đầy đủ của file
            }
            else
            {
                throw new Exception($"TTS generation failed: {root.GetProperty("error").GetString()}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in GenerateTTS: {ex.Message}");
            throw;
        }
    }
}
