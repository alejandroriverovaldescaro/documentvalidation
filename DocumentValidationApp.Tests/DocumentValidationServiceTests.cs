using DocumentValidationApp.Services;
using DocumentValidationApp.Models;
using Microsoft.AspNetCore.Hosting;
using Moq;
using Xunit;

namespace DocumentValidationApp.Tests;

public class DocumentValidationServiceTests
{
    private readonly DocumentValidationService _service;
    private readonly Mock<IWebHostEnvironment> _mockEnvironment;
    private readonly Mock<IOllamaService> _mockOllamaService;
    private readonly Mock<IAzureAIVisionService> _mockAzureAIVisionService;

    public DocumentValidationServiceTests()
    {
        _mockEnvironment = new Mock<IWebHostEnvironment>();
        _mockEnvironment.Setup(e => e.WebRootPath).Returns("/tmp/testwwwroot");
        
        _mockOllamaService = new Mock<IOllamaService>();
        _mockOllamaService.Setup(o => o.IsAvailableAsync()).ReturnsAsync(false);
        
        _mockAzureAIVisionService = new Mock<IAzureAIVisionService>();
        _mockAzureAIVisionService.Setup(a => a.IsAvailableAsync()).ReturnsAsync(false);
        
        _service = new DocumentValidationService(_mockEnvironment.Object, _mockOllamaService.Object, _mockAzureAIVisionService.Object);
    }

    [Fact]
    public async Task ValidateDocumentAsync_WithPassportPdf_ReturnsPassportType()
    {
        // Arrange
        var pdfPath = "/tmp/test-documents/sample_passport.pdf";
        if (!File.Exists(pdfPath))
        {
            // Skip test if file doesn't exist
            return;
        }

        using var fileStream = File.OpenRead(pdfPath);
        
        // Act
        var result = await _service.ValidateDocumentAsync(fileStream, "sample_passport.pdf", "application/pdf", ProcessingMethod.TesseractOCR);

        // Assert
        Assert.True(result.IsValid);
        Assert.Contains("Passport", result.DocumentType, StringComparison.OrdinalIgnoreCase);
        Assert.NotEmpty(result.ExtractedText);
    }

    [Fact]
    public async Task ValidateDocumentAsync_WithDriverLicensePdf_ReturnsDriverLicenseType()
    {
        // Arrange
        var pdfPath = "/tmp/test-documents/sample_driver_license.pdf";
        if (!File.Exists(pdfPath))
        {
            // Skip test if file doesn't exist
            return;
        }

        using var fileStream = File.OpenRead(pdfPath);
        
        // Act
        var result = await _service.ValidateDocumentAsync(fileStream, "sample_driver_license.pdf", "application/pdf", ProcessingMethod.TesseractOCR);

        // Assert
        Assert.True(result.IsValid);
        Assert.Contains("License", result.DocumentType, StringComparison.OrdinalIgnoreCase);
        Assert.NotEmpty(result.ExtractedText);
    }

    [Fact]
    public async Task ValidateDocumentAsync_WithIdCardPdf_ReturnsIdCardType()
    {
        // Arrange
        var pdfPath = "/tmp/test-documents/sample_id_card.pdf";
        if (!File.Exists(pdfPath))
        {
            // Skip test if file doesn't exist
            return;
        }

        using var fileStream = File.OpenRead(pdfPath);
        
        // Act
        var result = await _service.ValidateDocumentAsync(fileStream, "sample_id_card.pdf", "application/pdf", ProcessingMethod.TesseractOCR);

        // Assert
        Assert.True(result.IsValid);
        Assert.Contains("Identity", result.DocumentType, StringComparison.OrdinalIgnoreCase);
        Assert.NotEmpty(result.ExtractedText);
    }

    [Fact]
    public async Task ValidateDocumentAsync_WithUnsupportedFileType_ReturnsInvalid()
    {
        // Arrange
        using var stream = new MemoryStream();
        
        // Act
        var result = await _service.ValidateDocumentAsync(stream, "test.txt", "text/plain", ProcessingMethod.TesseractOCR);

        // Assert
        Assert.False(result.IsValid);
        Assert.NotNull(result.ErrorMessage);
        Assert.Contains("Unsupported file type", result.ErrorMessage);
    }

    [Theory]
    [InlineData("Expiration: 12/31/2025", "12/31/2025")]
    [InlineData("EXP 01-15-2026", "01-15-2026")]
    [InlineData("Valid Until: 15.06.2024", "15.06.2024")]
    [InlineData("EXPIRES 25 JAN 2027", "25 JAN 2027")]
    [InlineData("Expiry Date: JAN 25 2027", "JAN 25 2027")]
    [InlineData("Valid thru 03 15 2025", "03 15 2025")]
    [InlineData("EXP: 12312025", "12312025")]
    public void ExpirationDateExtraction_WithVariousFormats_ShouldDetectDate(string textWithDate, string expectedDateSubstring)
    {
        // This test verifies that the improved expiration date extraction can handle various formats
        // We'll use reflection to call the private ExtractCriticalData method
        var method = typeof(DocumentValidationService).GetMethod("ExtractCriticalData", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        
        var result = new DocumentValidationResult();
        result.ExtractedText.Add(textWithDate);
        
        // Act
        method?.Invoke(_service, new object[] { result });
        
        // Assert
        Assert.NotNull(result.ExpirationDate);
        Assert.Contains(expectedDateSubstring, result.ExpirationDate, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("Passport Number: AB123456\nExpiration Date: 12/31/2025\nIssued By: Department of State")]
    [InlineData("Driver License\nDL No: D1234567\nEXP: 01/15/2026\nClass: C")]
    [InlineData("ID CARD\nID Number: 987654321\nValid Until 15.06.2024\nCountry: USA")]
    public void DocumentValidation_WithExpirationDates_ShouldExtractCorrectly(string documentText)
    {
        // This test verifies end-to-end expiration date extraction
        var method = typeof(DocumentValidationService).GetMethod("ExtractCriticalData", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        
        var result = new DocumentValidationResult();
        result.ExtractedText.Add(documentText);
        
        // Act
        method?.Invoke(_service, new object[] { result });
        
        // Assert - Should detect an expiration date
        Assert.NotNull(result.ExpirationDate);
        Assert.NotEmpty(result.ExpirationDate);
        // Should have a validation message about the expiration date
        Assert.Contains(result.ValidationMessages, msg => 
            msg.Contains("expiration date", StringComparison.OrdinalIgnoreCase) || 
            msg.Contains("EXPIRED", StringComparison.OrdinalIgnoreCase));
    }

    [Theory]
    [InlineData("EXPIRATION DATE\n12/31/2025", "12/31/2025")]
    [InlineData("EXP\n01-15-2026", "01-15-2026")]
    [InlineData("EXPIRY DATE\n15.06.2024", "15.06.2024")]
    [InlineData("VALID UNTIL\n25 JAN 2027", "25 JAN 2027")]
    [InlineData("EXPIRES\nJAN 25 2027", "JAN 25 2027")]
    [InlineData("**Extracted Text (OCR):**\nPASSPORT\nEXPIRATION DATE\n12/31/2025\nPASSPORT NO\nAB123456", "12/31/2025")]
    public void ExpirationDateExtraction_WithMultilineFormat_ShouldDetectDate(string textWithDate, string expectedDateSubstring)
    {
        // This test verifies Azure AI Vision multi-line format where keyword and date are on separate lines
        var method = typeof(DocumentValidationService).GetMethod("ExtractCriticalData", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        
        var result = new DocumentValidationResult();
        result.ExtractedText.Add(textWithDate);
        
        // Act
        method?.Invoke(_service, new object[] { result });
        
        // Assert
        Assert.NotNull(result.ExpirationDate);
        Assert.Contains(expectedDateSubstring, result.ExpirationDate, StringComparison.OrdinalIgnoreCase);
    }
}
