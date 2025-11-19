using Azure.AI.Vision.ImageAnalysis;
using Azure;

namespace DocumentValidationApp.Services;

public interface IAzureAIVisionService
{
    Task<string> AnalyzeImageAsync(byte[] imageData, string prompt);
    Task<bool> IsAvailableAsync();
}

public class AzureAIVisionService : IAzureAIVisionService
{
    private readonly string? _endpoint;
    private readonly string? _key;
    private readonly IConfiguration _configuration;

    public AzureAIVisionService(IConfiguration configuration)
    {
        _configuration = configuration;
        _endpoint = _configuration["AzureAIVision:Endpoint"];
        _key = _configuration["AzureAIVision:Key"];
    }

    public Task<bool> IsAvailableAsync()
    {
        return Task.FromResult(!string.IsNullOrWhiteSpace(_endpoint) && !string.IsNullOrWhiteSpace(_key));
    }

    public async Task<string> AnalyzeImageAsync(byte[] imageData, string prompt)
    {
        if (string.IsNullOrWhiteSpace(_endpoint) || string.IsNullOrWhiteSpace(_key))
        {
            throw new Exception("Azure AI Vision credentials not configured. Please set AzureAIVision:Endpoint and AzureAIVision:Key in appsettings.json or environment variables.");
        }

        try
        {
            var client = new ImageAnalysisClient(new Uri(_endpoint), new AzureKeyCredential(_key));
            var result = await client.AnalyzeAsync(
                BinaryData.FromBytes(imageData),
                VisualFeatures.Caption | VisualFeatures.Read | VisualFeatures.Tags | VisualFeatures.Objects | VisualFeatures.People
            );

            var analysisText = new System.Text.StringBuilder();

            if (result.Value.Caption != null)
            {
                analysisText.AppendLine($"**Image Description:** {result.Value.Caption.Text} (Confidence: {result.Value.Caption.Confidence:P0})");
                analysisText.AppendLine();
            }

            if (result.Value.Read?.Blocks.Count > 0)
            {
                analysisText.AppendLine("**Extracted Text (OCR):**");
                foreach (var block in result.Value.Read.Blocks)
                {
                    foreach (var line in block.Lines)
                    {
                        analysisText.AppendLine(line.Text);
                    }
                }
                analysisText.AppendLine();
            }

            if (result.Value.Tags?.Values.Count > 0)
            {
                analysisText.AppendLine("**Detected Tags:**");
                var topTags = result.Value.Tags.Values
                    .OrderByDescending(t => t.Confidence)
                    .Take(10)
                    .Select(t => $"{t.Name} ({t.Confidence:P0})");
                analysisText.AppendLine(string.Join(", ", topTags));
                analysisText.AppendLine();
            }

            if (result.Value.Objects?.Values.Count > 0)
            {
                analysisText.AppendLine("**Detected Objects:**");
                foreach (var obj in result.Value.Objects.Values.Take(10))
                {
                    analysisText.AppendLine($"- {obj.Tags.FirstOrDefault()?.Name ?? "Unknown"} (Confidence: {obj.Tags.FirstOrDefault()?.Confidence ?? 0:P0})");
                }
                analysisText.AppendLine();
            }

            if (result.Value.People?.Values.Count > 0)
            {
                analysisText.AppendLine($"**People Detected:** {result.Value.People.Values.Count} person(s)");
                analysisText.AppendLine();
            }

            return analysisText.ToString();
        }
        catch (RequestFailedException ex)
        {
            throw new Exception($"Azure AI Vision API request failed: {ex.Message}\nStatus: {ex.Status}\nError Code: {ex.ErrorCode}\n\nPlease verify your endpoint and key are correct.", ex);
        }
        catch (Exception ex)
        {
            throw new Exception($"Azure AI Vision analysis failed: {ex.Message}\n\nTips:\n• Verify Azure AI Vision endpoint is correct\n• Verify Azure AI Vision key is valid\n• Ensure Azure subscription is active\n• Check network connectivity to Azure", ex);
        }
    }
}
