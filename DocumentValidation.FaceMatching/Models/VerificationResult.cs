namespace DocumentValidation.FaceMatching.Models;

/// <summary>
/// Result of face verification comparison
/// </summary>
public class VerificationResult
{
    public bool IsIdentical { get; set; }
    public double Confidence { get; set; }
    public VerificationDecision Decision { get; set; }
    public string Message { get; set; } = string.Empty;
}

/// <summary>
/// Decision bands for verification
/// </summary>
public enum VerificationDecision
{
    Reject,
    Retry,
    Accept,
    AutoAccept
}
