using DocumentValidationApp.Models;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas.Parser;
using iText.Kernel.Pdf.Canvas.Parser.Listener;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System.Text.RegularExpressions;

namespace DocumentValidationApp.Services;

public interface IDocumentValidationService
{
    Task<DocumentValidationResult> ValidateDocumentAsync(Stream fileStream, string fileName, string contentType);
}

public class DocumentValidationService : IDocumentValidationService
{
    public async Task<DocumentValidationResult> ValidateDocumentAsync(Stream fileStream, string fileName, string contentType)
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
                await ProcessImageDocument(fileStream, result, fileName);
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

    private async Task ProcessImageDocument(Stream fileStream, DocumentValidationResult result, string fileName)
    {
        try
        {
            using var image = await Image.LoadAsync<Rgba32>(fileStream);
            
            result.ValidationMessages.Add($"Image file: {fileName}");
            result.ValidationMessages.Add($"Dimensions: {image.Width}x{image.Height} pixels");
            result.ValidationMessages.Add($"Image format detected and validated successfully.");
            
            // For images, we'll provide basic information
            // In a real-world scenario, you would integrate with an OCR service
            result.ExtractedText.Add($"Image file uploaded: {fileName} ({image.Width}x{image.Height})");
            result.ExtractedText.Add("Note: For advanced text extraction from images, consider integrating with OCR services like Azure Computer Vision or Google Cloud Vision API.");
        }
        catch (Exception ex)
        {
            result.ValidationMessages.Add($"Warning: Could not process image - {ex.Message}");
        }
    }

    private void ClassifyDocument(DocumentValidationResult result)
    {
        var allText = string.Join(" ", result.ExtractedText).ToLower();

        // Document classification based on keywords
        if (ContainsPassportKeywords(allText))
        {
            result.DocumentType = "Passport";
            result.Description = "This appears to be a passport document.";
        }
        else if (ContainsDriverLicenseKeywords(allText))
        {
            result.DocumentType = "Driver License / Driver's License";
            result.Description = "This appears to be a driver's license document.";
        }
        else if (ContainsIdCardKeywords(allText))
        {
            result.DocumentType = "Identity Card / ID Card";
            result.Description = "This appears to be an identity card document.";
        }
        else
        {
            result.DocumentType = "Unknown Document Type";
            result.Description = "The document type could not be determined automatically. Manual review may be required.";
        }
    }

    private bool ContainsPassportKeywords(string text)
    {
        var passportKeywords = new[] { "passport", "pasaporte", "passeport", "reisepass", "passaporto", 
                                       "nationality", "date of birth", "place of birth", "issuing authority",
                                       "passport no", "passport number", "p<" };
        return passportKeywords.Any(keyword => text.Contains(keyword, StringComparison.OrdinalIgnoreCase));
    }

    private bool ContainsDriverLicenseKeywords(string text)
    {
        var dlKeywords = new[] { "driver license", "driver's license", "driving license", "licencia de conducir",
                                 "permis de conduire", "führerschein", "class", "restrictions", "endorsements",
                                 "dl no", "license number", "operator", "dl class" };
        return dlKeywords.Any(keyword => text.Contains(keyword, StringComparison.OrdinalIgnoreCase));
    }

    private bool ContainsIdCardKeywords(string text)
    {
        var idKeywords = new[] { "identity card", "identification card", "id card", "national id",
                                 "cedula", "carte d'identité", "personalausweis", "carta d'identità",
                                 "id no", "id number", "identification number" };
        return idKeywords.Any(keyword => text.Contains(keyword, StringComparison.OrdinalIgnoreCase));
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
