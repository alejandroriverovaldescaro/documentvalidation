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
        _modelName = "qwen2-vl:8b";
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
                stream = false
            };

            var jsonContent = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

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
        catch (Exception ex)
        {
            throw new Exception($"Ollama analysis failed: {ex.Message}", ex);
        }
    }
}
