using System.Text;
using System.Text.Json;

namespace DocumentValidationApp.Services;

public interface IOllamaService
{
    Task<string> AnalyzeImageAsync(byte[] imageData, string prompt);
    Task<bool> IsAvailableAsync();
}

public class OllamaService : IOllamaService
{
    private readonly HttpClient _httpClient;
    private readonly string _ollamaBaseUrl;
    private readonly string _modelName;

    public OllamaService(HttpClient httpClient)
    {
        _httpClient = httpClient;
        _ollamaBaseUrl = "http://localhost:11434";
        _modelName = "qwen3-vl:8b";
    }

    public async Task<bool> IsAvailableAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync($"{_ollamaBaseUrl}/api/tags");
            if (!response.IsSuccessStatusCode)
                return false;

            var content = await response.Content.ReadAsStringAsync();
            return content.Contains(_modelName);
        }
        catch
        {
            return false;
        }
    }

    public async Task<string> AnalyzeImageAsync(byte[] imageData, string prompt)
    {
        try
        {
            var base64Image = Convert.ToBase64String(imageData);

            var requestBody = new
            {
                model = _modelName,
                prompt = prompt,
                images = new[] { base64Image },
                stream = false,
                options = new
                {
                    temperature = 0.1,        // Lower temperature for more consistent outputs
                    num_predict = 2048,       // Allow longer responses
                    num_ctx = 4096,           // Context window size
                    num_gpu = 999,            // Use all available GPU layers (0 for CPU only)
                    num_thread = 8            // CPU threads to use
                }
            };

            var jsonContent = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            // Vision models can take 10-30 minutes on CPU, especially on first run when model loads
            var response = await _httpClient.PostAsync($"{_ollamaBaseUrl}/api/generate", content);
            response.EnsureSuccessStatusCode();

            var responseContent = await response.Content.ReadAsStringAsync();
            var jsonResponse = JsonSerializer.Deserialize<JsonElement>(responseContent);

            if (jsonResponse.TryGetProperty("response", out var responseText))
            {
                return responseText.GetString() ?? string.Empty;
            }

            return string.Empty;
        }
        catch (TaskCanceledException ex)
        {
            throw new Exception($"Vision model processing timed out. This can happen when:\n" +
                              $"• First time running (model needs to load into memory)\n" +
                              $"• Running on CPU without GPU acceleration\n" +
                              $"• Processing very large/complex images\n" +
                              $"\nTips to resolve:\n" +
                              $"• Ensure Ollama is running: ollama serve\n" +
                              $"• Check if model is loaded: ollama list\n" +
                              $"• Try a smaller image file\n" +
                              $"• Consider using Tesseract OCR for faster processing\n" +
                              $"• If on CPU, first run may take 10-30 minutes\n" +
                              $"\nOriginal error: {ex.Message}", ex);
        }
        catch (HttpRequestException ex)
        {
            throw new Exception($"Failed to connect to Ollama at {_ollamaBaseUrl}.\n" +
                              $"Ensure Ollama is running with: ollama serve\n" +
                              $"Error: {ex.Message}", ex);
        }
        catch (Exception ex)
        {
            throw new Exception($"Ollama analysis failed: {ex.Message}", ex);
        }
    }
}
