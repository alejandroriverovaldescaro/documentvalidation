namespace DocumentValidationApp.Models;

public class DocumentValidationResult
{
    public bool IsValid { get; set; }
    public string DocumentType { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? ExpirationDate { get; set; }
    public string? DocumentNumber { get; set; }
    public string? IssuingAuthority { get; set; }
    public List<string> ExtractedText { get; set; } = new();
    public List<string> ValidationMessages { get; set; } = new();
    public string? ErrorMessage { get; set; }
}
