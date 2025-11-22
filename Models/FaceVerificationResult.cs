namespace DocumentValidation.Models;

public class FaceVerificationResult
{
    public bool IsMatch { get; set; }
    public double ConfidenceScore { get; set; }
    public string Message { get; set; } = string.Empty;
    public DateTime VerificationTimestamp { get; set; }
    public List<string> Warnings { get; set; } = new();
}
