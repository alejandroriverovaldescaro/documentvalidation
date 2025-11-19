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
    private readonly IAzureAIVisionService _azureAIVisionService;

    public DocumentValidationService(IWebHostEnvironment environment, IOllamaService ollamaService, IAzureAIVisionService azureAIVisionService)
    {
        _tessDataPath = Path.Combine(environment.WebRootPath, "tessdata");
        _ollamaService = ollamaService;
        _azureAIVisionService = azureAIVisionService;
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
            else if (method == ProcessingMethod.AzureAIVision)
            {
                await ProcessImageWithAzureAIVision(image, result, fileName);
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

    private async Task ProcessImageWithAzureAIVision(Image<Rgba32> image, DocumentValidationResult result, string fileName)
    {
        try
        {
            var isAvailable = await _azureAIVisionService.IsAvailableAsync();
            if (!isAvailable)
            {
                result.ValidationMessages.Add("⚠️ Azure AI Vision service not configured. Please set AzureAIVision:Endpoint and AzureAIVision:Key in configuration.");
                result.ExtractedText.Add($"Image file uploaded: {fileName} ({image.Width}x{image.Height})");
                result.ExtractedText.Add("Note: Configure Azure AI Vision credentials in appsettings.json or environment variables.");
                return;
            }

            using var ms = new MemoryStream();
            await image.SaveAsPngAsync(ms);
            var imageBytes = ms.ToArray();

            result.ValidationMessages.Add("☁️ Analyzing image with Azure AI Vision...");
            
            var analysis = await _azureAIVisionService.AnalyzeImageAsync(imageBytes, "");
            
            if (!string.IsNullOrWhiteSpace(analysis))
            {
                result.ExtractedText.Add(analysis.Trim());
                result.ValidationMessages.Add($"✓ Azure AI Vision analysis completed successfully");
            }
            else
            {
                result.ValidationMessages.Add("⚠️ Azure AI Vision analysis returned no results.");
                result.ExtractedText.Add($"Image file uploaded: {fileName} ({image.Width}x{image.Height})");
            }
        }
        catch (Exception ex)
        {
            result.ValidationMessages.Add($"⚠️ Azure AI Vision processing failed: {ex.Message}");
            result.ExtractedText.Add($"Image file uploaded: {fileName} ({image.Width}x{image.Height})");
            result.ExtractedText.Add("Note: Verify Azure AI Vision credentials and ensure the service is accessible.");
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
        // Comprehensive patterns for expiration dates
        // Supports various formats: MM/DD/YYYY, DD/MM/YYYY, YYYY-MM-DD, DD.MM.YYYY, DD MMM YYYY, etc.
        var datePatterns = new[]
        {
            // Dates with explicit expiration keywords on same line
            @"(?:exp(?:iry)?\.?|expiration|valid\s*(?:until|thru|through|to)|expires?)[:\s]*(\d{1,2}[\s/.-]\d{1,2}[\s/.-]\d{2,4})",
            @"(?:exp(?:iry)?\.?|expiration|valid\s*(?:until|thru|through|to)|expires?)[:\s]*(\d{1,2}[\s/.-][A-Za-z]{3,}[\s/.-]\d{2,4})",
            @"(?:exp(?:iry)?\.?|expiration|valid\s*(?:until|thru|through|to)|expires?)[:\s]*([A-Za-z]{3,}[\s/.-]\d{1,2}[\s/.-]\d{2,4})",
            
            // Dates without separators near keywords on same line
            @"(?:exp(?:iry)?\.?|expiration|valid\s*(?:until|thru|through|to)|expires?)[:\s]*(\d{8})",
            
            // Multi-line patterns: keyword on one line, date on next line (for Azure AI Vision OCR)
            // This handles cases where Azure OCR splits "EXPIRATION DATE" and "12/31/2025" across lines
            @"(?:exp(?:iry)?\.?|expiration|valid\s*(?:until|thru|through|to)|expires?)(?:\s*date)?[\s\r\n:]+(\d{1,2}[\s/.-]\d{1,2}[\s/.-]\d{2,4})",
            @"(?:exp(?:iry)?\.?|expiration|valid\s*(?:until|thru|through|to)|expires?)(?:\s*date)?[\s\r\n:]+(\d{1,2}[\s/.-][A-Za-z]{3,}[\s/.-]\d{2,4})",
            @"(?:exp(?:iry)?\.?|expiration|valid\s*(?:until|thru|through|to)|expires?)(?:\s*date)?[\s\r\n:]+([A-Za-z]{3,}[\s/.-]\d{1,2}[\s/.-]\d{2,4})",
            @"(?:exp(?:iry)?\.?|expiration|valid\s*(?:until|thru|through|to)|expires?)(?:\s*date)?[\s\r\n:]+(\d{8})",
            
            // Standard date formats with various separators (lower priority)
            @"(\d{1,2}[\s/.-]\d{1,2}[\s/.-]\d{4})",
            @"(\d{4}[\s/.-]\d{1,2}[\s/.-]\d{1,2})",
            
            // Dates with month names (lower priority)
            @"(\d{1,2}[\s/.-][A-Za-z]{3,}[\s/.-]\d{2,4})",
            @"([A-Za-z]{3,}[\s/.-]\d{1,2}[\s/.-]\d{2,4})",
            
            // Dates without separators - 8 digits: MMDDYYYY or DDMMYYYY (lowest priority)
            @"\b(\d{2}\s?\d{2}\s?\d{4})\b"
        };

        foreach (var pattern in datePatterns)
        {
            var matches = Regex.Matches(text, pattern, RegexOptions.IgnoreCase | RegexOptions.Multiline);
            foreach (Match match in matches)
            {
                var dateString = match.Groups[1].Value.Trim();
                
                // Try to parse the date to validate it
                if (TryParseDate(dateString, out var expiryDate))
                {
                    // Only accept dates that are in a reasonable range (past or future, but not too far)
                    var yearsDiff = Math.Abs((expiryDate - DateTime.Now).TotalDays / 365.25);
                    if (yearsDiff <= 50) // Document dates should be within 50 years of current date
                    {
                        result.ExpirationDate = dateString;
                        
                        // Validate if date is expired
                        if (expiryDate < DateTime.Now)
                        {
                            result.ValidationMessages.Add($"⚠️ WARNING: Document appears to be EXPIRED (Expiration: {result.ExpirationDate})");
                        }
                        else
                        {
                            result.ValidationMessages.Add($"✓ Document expiration date found: {result.ExpirationDate}");
                        }
                        return; // Exit once a valid date is found
                    }
                }
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
        // Clean up the date string (remove extra spaces)
        dateString = Regex.Replace(dateString, @"\s+", " ").Trim();
        
        // Try different date formats
        var formats = new[]
        {
            // Formats with slashes
            "MM/dd/yyyy", "M/d/yyyy", "MM/dd/yy", "M/d/yy",
            "dd/MM/yyyy", "d/M/yyyy", "dd/MM/yy", "d/M/yy",
            "yyyy/MM/dd", "yyyy/M/d",
            
            // Formats with dashes
            "MM-dd-yyyy", "M-d-yyyy", "MM-dd-yy", "M-d-yy",
            "dd-MM-yyyy", "d-M-yyyy", "dd-MM-yy", "d-M-yy",
            "yyyy-MM-dd", "yyyy-M-d",
            
            // Formats with dots
            "MM.dd.yyyy", "M.d.yyyy", "MM.dd.yy", "M.d.yy",
            "dd.MM.yyyy", "d.M.yyyy", "dd.MM.yy", "d.M.yy",
            "yyyy.MM.dd", "yyyy.M.d",
            
            // Formats with spaces
            "MM dd yyyy", "M d yyyy", 
            "dd MM yyyy", "d M yyyy",
            "yyyy MM dd", "yyyy M d",
            
            // Formats with month names
            "dd MMM yyyy", "d MMM yyyy", "dd MMM yy", "d MMM yy",
            "dd MMMM yyyy", "d MMMM yyyy", "dd MMMM yy", "d MMMM yy",
            "MMM dd yyyy", "MMM d yyyy", "MMM dd yy", "MMM d yy",
            "MMMM dd yyyy", "MMMM d yyyy", "MMMM dd yy", "MMMM d yy",
            
            // Formats without separators
            "MMddyyyy", "ddMMyyyy", "yyyyMMdd"
        };

        foreach (var format in formats)
        {
            if (DateTime.TryParseExact(dateString, format, System.Globalization.CultureInfo.InvariantCulture, 
                System.Globalization.DateTimeStyles.None, out date))
            {
                // If year is 2-digit, ensure it's interpreted correctly
                if (date.Year < 100)
                {
                    date = date.AddYears(2000);
                }
                return true;
            }
        }

        // Try general parsing as a fallback
        if (DateTime.TryParse(dateString, System.Globalization.CultureInfo.InvariantCulture, 
            System.Globalization.DateTimeStyles.None, out date))
        {
            return true;
        }

        date = DateTime.MinValue;
        return false;
    }
}
