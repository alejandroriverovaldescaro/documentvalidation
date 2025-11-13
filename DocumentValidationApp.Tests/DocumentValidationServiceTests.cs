using DocumentValidationApp.Services;
using Xunit;

namespace DocumentValidationApp.Tests;

public class DocumentValidationServiceTests
{
    private readonly DocumentValidationService _service;

    public DocumentValidationServiceTests()
    {
        _service = new DocumentValidationService();
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
        var result = await _service.ValidateDocumentAsync(fileStream, "sample_passport.pdf", "application/pdf");

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
        var result = await _service.ValidateDocumentAsync(fileStream, "sample_driver_license.pdf", "application/pdf");

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
        var result = await _service.ValidateDocumentAsync(fileStream, "sample_id_card.pdf", "application/pdf");

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
        var result = await _service.ValidateDocumentAsync(stream, "test.txt", "text/plain");

        // Assert
        Assert.False(result.IsValid);
        Assert.NotNull(result.ErrorMessage);
        Assert.Contains("Unsupported file type", result.ErrorMessage);
    }
}
