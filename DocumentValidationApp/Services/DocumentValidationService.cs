using DocumentValidationApp.Models;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas.Parser;
using iText.Kernel.Pdf.Canvas.Parser.Listener;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System.Text.RegularExpressions;
using Tesseract;

namespace DocumentValidationApp.Services;

public interface IDocumentValidationService
{
    Task<DocumentValidationResult> ValidateDocumentAsync(Stream fileStream, string fileName, string contentType, ProcessingMethod method = ProcessingMethod.TesseractOCR);
}

public class DocumentValidationService : IDocumentValidationService
{
    private readonly string _tessDataPath;
    private readonly IOllamaService _ollamaService;

    public DocumentValidationService(IWebHostEnvironment environment, IOllamaService ollamaService)
    {
        _tessDataPath = Path.Combine(environment.WebRootPath, "tessdata");
        _ollamaService = ollamaService;
    }

    public async Task<DocumentValidationResult> ValidateDocumentAsync(Stream fileStream, string fileName, string contentType, ProcessingMethod method = ProcessingMethod.TesseractOCR)
    {
        var result = new DocumentValidationResult();

        try
        {
            // Determine file type
            var isPdf = contentType.Contains("pdf", StringComparison.OrdinalIgnoreCase) || 
                       fileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase);
            var isImage = contentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase) ||
                         fileName.EndsWith(".png", StringComparison.OrdinalIgnoreCase) ||
                         fileName.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) ||
                         fileName.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase);

            if (isPdf)
            {
                await ProcessPdfDocument(fileStream, result);
            }
            else if (isImage)
            {
                await ProcessImageDocument(fileStream, result, fileName, method);
            }
            else
            {
                result.IsValid = false;
                result.ErrorMessage = "Unsupported file type. Please upload PDF or image files (PNG, JPG, JPEG).";
                return result;
            }

            // Classify document and extract data
            ClassifyDocument(result);
            ExtractCriticalData(result);

            result.IsValid = true;
        }
        catch (Exception ex)
        {
            result.IsValid = false;
            result.ErrorMessage = $"Error processing document: {ex.Message}";
        }

        return result;
    }

    private async Task ProcessPdfDocument(Stream fileStream, DocumentValidationResult result)
    {
        using var pdfReader = new PdfReader(fileStream);
        using var pdfDocument = new PdfDocument(pdfReader);

        var numberOfPages = pdfDocument.GetNumberOfPages();
        result.ValidationMessages.Add($"PDF document contains {numberOfPages} page(s).");

        // Extract text from all pages
        for (int i = 1; i <= numberOfPages; i++)
        {
            var page = pdfDocument.GetPage(i);
            var strategy = new SimpleTextExtractionStrategy();
            var text = PdfTextExtractor.GetTextFromPage(page, strategy);
            
            if (!string.IsNullOrWhiteSpace(text))
            {
                result.ExtractedText.Add(text);
            }
        }

        await Task.CompletedTask;
    }

    private async Task ProcessImageDocument(Stream fileStream, DocumentValidationResult result, string fileName, ProcessingMethod method)
    {
        try
        {
            using var image = await Image.LoadAsync<Rgba32>(fileStream);
            
            result.ValidationMessages.Add($"Image file: {fileName}");
            result.ValidationMessages.Add($"Dimensions: {image.Width}x{image.Height} pixels");
            result.ValidationMessages.Add($"Image format detected and validated successfully.");
            result.ValidationMessages.Add($"Processing method: {method}");
            
            if (method == ProcessingMethod.OllamaVision)
            {
                await ProcessImageWithOllama(image, result, fileName);
            }
            else
            {
                await ProcessImageWithTesseract(image, result, fileName);
            }
        }
        catch (Exception ex)
        {
            result.ValidationMessages.Add($"Warning: Could not process image - {ex.Message}");
        }
    }

    private async Task ProcessImageWithOllama(Image<Rgba32> image, DocumentValidationResult result, string fileName)
    {
        try
        {
            // Check if Ollama is available
            var isAvailable = await _ollamaService.IsAvailableAsync();
            if (!isAvailable)
            {
                result.ValidationMessages.Add("⚠️ Ollama service not available. Please ensure Ollama is running and qwen3-vl:8b model is installed.");
                result.ExtractedText.Add($"Image file uploaded: {fileName} ({image.Width}x{image.Height})");
                result.ExtractedText.Add("Note: Run 'ollama pull qwen3-vl:8b' to download the model.");
                return;
            }

            // Convert image to byte array
            using var ms = new MemoryStream();
            await image.SaveAsPngAsync(ms);
            var imageBytes = ms.ToArray();

            // Create detailed prompt for document analysis
            var prompt = @"Analyze this document image and provide the following information in a structured format:

1. Document Type (identify if it's a passport, driver's license, ID card, or other document type)
2. Document Number (if visible)
3. Expiration Date (if visible, in format: MM/DD/YYYY or DD/MM/YYYY)
4. Issuing Authority (if visible)
5. Full text content visible in the document
6. Any other relevant information

Please be thorough and extract all visible text and data from the document.";

            result.ValidationMessages.Add("🤖 Analyzing image with AI Vision (Qwen3-VL)...");
            
            var analysis = await _ollamaService.AnalyzeImageAsync(imageBytes, prompt);
            
            if (!string.IsNullOrWhiteSpace(analysis))
            {
                result.ExtractedText.Add(analysis.Trim());
                result.ValidationMessages.Add($"✓ AI Vision analysis completed successfully using Ollama/Qwen3-VL");
            }
            else
            {
                result.ValidationMessages.Add("⚠️ AI Vision analysis returned no results.");
                result.ExtractedText.Add($"Image file uploaded: {fileName} ({image.Width}x{image.Height})");
            }
        }
        catch (Exception ex)
        {
            result.ValidationMessages.Add($"⚠️ Ollama Vision processing failed: {ex.Message}");
            result.ExtractedText.Add($"Image file uploaded: {fileName} ({image.Width}x{image.Height})");
            result.ExtractedText.Add("Note: Ensure Ollama is running with 'ollama serve' and the model is available.");
        }
    }

    private async Task ProcessImageWithTesseract(Image<Rgba32> image, DocumentValidationResult result, string fileName)
    {
        try
        {
            // Save image to temporary file for Tesseract processing
            var tempImagePath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.png");
            await image.SaveAsPngAsync(tempImagePath);

            try
            {
                // Check if tessdata exists
                if (!Directory.Exists(_tessDataPath) || !File.Exists(Path.Combine(_tessDataPath, "eng.traineddata")))
                {
                    result.ValidationMessages.Add("⚠️ OCR language data not found. Text extraction from images is limited.");
                    result.ExtractedText.Add($"Image file uploaded: {fileName} ({image.Width}x{image.Height})");
                    result.ExtractedText.Add("Note: To enable text extraction from images, download eng.traineddata from https://github.com/tesseract-ocr/tessdata and place it in wwwroot/tessdata/");
                }
                else
                {
                    using var engine = new TesseractEngine(_tessDataPath, "eng", EngineMode.Default);
                    using var tesseractImage = Pix.LoadFromFile(tempImagePath);
                    using var page = engine.Process(tesseractImage);
                    
                    var extractedText = page.GetText();
                    
                    if (!string.IsNullOrWhiteSpace(extractedText))
                    {
                        result.ExtractedText.Add(extractedText.Trim());
                        result.ValidationMessages.Add($"✓ Text successfully extracted from image using OCR (Confidence: {page.GetMeanConfidence():P0})");
                    }
                    else
                    {
                        result.ValidationMessages.Add("⚠️ No text could be extracted from the image. Image may be too low quality or contain no text.");
                        result.ExtractedText.Add($"Image file uploaded: {fileName} ({image.Width}x{image.Height})");
                    }
                }
            }
            finally
            {
                // Clean up temporary file
                if (File.Exists(tempImagePath))
                {
                    File.Delete(tempImagePath);
                }
            }
        }
        catch (Exception ocrEx)
        {
            result.ValidationMessages.Add($"⚠️ OCR processing failed: {ocrEx.Message}");
            result.ExtractedText.Add($"Image file uploaded: {fileName} ({image.Width}x{image.Height})");
            result.ExtractedText.Add("Note: Text extraction from image failed. The image may need preprocessing or higher quality.");
        }
    }

    private void ClassifyDocument(DocumentValidationResult result)
    {
        var allText = string.Join(" ", result.ExtractedText).ToLower();

        // Document classification based on keyword scoring
        var passportScore = CalculatePassportScore(allText);
        var driverLicenseScore = CalculateDriverLicenseScore(allText);
        var idCardScore = CalculateIdCardScore(allText);

        // Choose the type with the highest score
        var maxScore = Math.Max(passportScore, Math.Max(driverLicenseScore, idCardScore));
        
        if (maxScore == 0)
        {
            result.DocumentType = "Unknown Document Type";
            result.Description = "The document type could not be determined automatically. Manual review may be required.";
        }
        else if (passportScore == maxScore)
        {
            result.DocumentType = "Passport";
            result.Description = "This appears to be a passport document.";
        }
        else if (driverLicenseScore == maxScore)
        {
            result.DocumentType = "Driver License / Driver's License";
            result.Description = "This appears to be a driver's license document.";
        }
        else
        {
            result.DocumentType = "Identity Card / ID Card";
            result.Description = "This appears to be an identity card document.";
        }
    }

    private int CalculatePassportScore(string text)
    {
        var score = 0;
        // High-value passport-specific keywords
        if (text.Contains("passport", StringComparison.OrdinalIgnoreCase)) score += 10;
        if (text.Contains("pasaporte", StringComparison.OrdinalIgnoreCase)) score += 10;
        if (text.Contains("passeport", StringComparison.OrdinalIgnoreCase)) score += 10;
        if (text.Contains("passport no", StringComparison.OrdinalIgnoreCase)) score += 5;
        if (text.Contains("passport number", StringComparison.OrdinalIgnoreCase)) score += 5;
        if (text.Contains("type: p", StringComparison.OrdinalIgnoreCase)) score += 3;
        if (text.Contains("p<", StringComparison.OrdinalIgnoreCase)) score += 3;
        return score;
    }

    private int CalculateDriverLicenseScore(string text)
    {
        var score = 0;
        // High-value driver license-specific keywords
        if (text.Contains("driver license", StringComparison.OrdinalIgnoreCase)) score += 10;
        if (text.Contains("driver's license", StringComparison.OrdinalIgnoreCase)) score += 10;
        if (text.Contains("driving license", StringComparison.OrdinalIgnoreCase)) score += 10;
        if (text.Contains("dl number", StringComparison.OrdinalIgnoreCase)) score += 5;
        if (text.Contains("dl no", StringComparison.OrdinalIgnoreCase)) score += 5;
        if (text.Contains("class:", StringComparison.OrdinalIgnoreCase)) score += 3;
        if (text.Contains("restrictions:", StringComparison.OrdinalIgnoreCase)) score += 3;
        if (text.Contains("endorsements:", StringComparison.OrdinalIgnoreCase)) score += 3;
        if (text.Contains("operator", StringComparison.OrdinalIgnoreCase)) score += 2;
        return score;
    }

    private int CalculateIdCardScore(string text)
    {
        var score = 0;
        // High-value ID card-specific keywords
        if (text.Contains("identity card", StringComparison.OrdinalIgnoreCase)) score += 10;
        if (text.Contains("identification card", StringComparison.OrdinalIgnoreCase)) score += 10;
        if (text.Contains("id card", StringComparison.OrdinalIgnoreCase)) score += 10;
        if (text.Contains("national id", StringComparison.OrdinalIgnoreCase)) score += 8;
        if (text.Contains("id number", StringComparison.OrdinalIgnoreCase)) score += 5;
        if (text.Contains("id no", StringComparison.OrdinalIgnoreCase)) score += 5;
        if (text.Contains("identification number", StringComparison.OrdinalIgnoreCase)) score += 5;
        return score;
    }

    private void ExtractCriticalData(DocumentValidationResult result)
    {
        var allText = string.Join(" ", result.ExtractedText);

        // Extract expiration date
        ExtractExpirationDate(allText, result);

        // Extract document number
        ExtractDocumentNumber(allText, result);

        // Extract issuing authority
        ExtractIssuingAuthority(allText, result);
    }

    private void ExtractExpirationDate(string text, DocumentValidationResult result)
    {
        // Pattern for dates: MM/DD/YYYY, DD/MM/YYYY, YYYY-MM-DD
        var datePatterns = new[]
        {
            @"(?:exp(?:iry)?|expiration|valid until|expires?)[:\s]*(\d{1,2}[/-]\d{1,2}[/-]\d{2,4})",
            @"(\d{1,2}[/-]\d{1,2}[/-]\d{4})",
            @"(\d{4}[/-]\d{1,2}[/-]\d{1,2})"
        };

        foreach (var pattern in datePatterns)
        {
            var match = Regex.Match(text, pattern, RegexOptions.IgnoreCase);
            if (match.Success)
            {
                result.ExpirationDate = match.Groups[1].Value;
                
                // Validate if date is expired
                if (TryParseDate(result.ExpirationDate, out var expiryDate))
                {
                    if (expiryDate < DateTime.Now)
                    {
                        result.ValidationMessages.Add($"⚠️ WARNING: Document appears to be EXPIRED (Expiration: {result.ExpirationDate})");
                    }
                    else
                    {
                        result.ValidationMessages.Add($"✓ Document expiration date found: {result.ExpirationDate}");
                    }
                }
                break;
            }
        }

        if (string.IsNullOrEmpty(result.ExpirationDate))
        {
            result.ValidationMessages.Add("⚠️ Could not automatically detect expiration date. Manual verification recommended.");
        }
    }

    private void ExtractDocumentNumber(string text, DocumentValidationResult result)
    {
        // Pattern for document numbers
        var numberPatterns = new[]
        {
            @"(?:passport|license|id)\s*(?:no\.?|number|#)[:\s]*([A-Z0-9]+)",
            @"(?:document|doc)\s*(?:no\.?|number|#)[:\s]*([A-Z0-9]+)",
            @"\b([A-Z]{1,2}\d{6,9})\b" // General pattern for alphanumeric IDs
        };

        foreach (var pattern in numberPatterns)
        {
            var match = Regex.Match(text, pattern, RegexOptions.IgnoreCase);
            if (match.Success)
            {
                result.DocumentNumber = match.Groups[1].Value;
                result.ValidationMessages.Add($"✓ Document number detected: {result.DocumentNumber}");
                break;
            }
        }

        if (string.IsNullOrEmpty(result.DocumentNumber))
        {
            result.ValidationMessages.Add("⚠️ Could not automatically detect document number. Manual verification recommended.");
        }
    }

    private void ExtractIssuingAuthority(string text, DocumentValidationResult result)
    {
        // Pattern for issuing authorities
        var authorityPatterns = new[]
        {
            @"(?:issued by|issuing authority)[:\s]*([A-Za-z\s]+?)(?:\n|$|\.)",
            @"(?:department of|ministry of)\s*([A-Za-z\s]+?)(?:\n|$|\.)",
        };

        foreach (var pattern in authorityPatterns)
        {
            var match = Regex.Match(text, pattern, RegexOptions.IgnoreCase);
            if (match.Success)
            {
                result.IssuingAuthority = match.Groups[1].Value.Trim();
                result.ValidationMessages.Add($"✓ Issuing authority: {result.IssuingAuthority}");
                break;
            }
        }
    }

    private bool TryParseDate(string dateString, out DateTime date)
    {
        // Try different date formats
        var formats = new[]
        {
            "MM/dd/yyyy", "M/d/yyyy",
            "dd/MM/yyyy", "d/M/yyyy",
            "yyyy-MM-dd", "yyyy/MM/dd",
            "MM-dd-yyyy", "M-d-yyyy"
        };

        foreach (var format in formats)
        {
            if (DateTime.TryParseExact(dateString, format, null, System.Globalization.DateTimeStyles.None, out date))
            {
                return true;
            }
        }

        date = DateTime.MinValue;
        return false;
    }
}
