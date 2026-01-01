using DocumentValidation.FaceMatching.Models;
using Microsoft.Extensions.Logging;

namespace DocumentValidation.FaceMatching;

/// <summary>
/// Implements threshold-based decision logic for face verification.
/// Uses bands instead of single hard threshold to handle edge cases gracefully.
/// </summary>
public class VerificationDecision
{
    private readonly ILogger<VerificationDecision> _logger;

    // Decision thresholds based on confidence score
    private const double AutoAcceptThreshold = 0.80;
    private const double AcceptThreshold = 0.60;
    private const double RetryThreshold = 0.45;

    public VerificationDecision(ILogger<VerificationDecision> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Makes verification decision based on confidence score using band thresholds.
    /// Band logic:
    /// - score >= 0.80 → auto-accept (high confidence match)
    /// - 0.60 <= score < 0.80 → accept or soft-review (good match)
    /// - 0.45 <= score < 0.60 → request retry (uncertain, may improve with better frame)
    /// - score < 0.45 → reject (low confidence, likely different person)
    /// </summary>
    public VerificationResult MakeDecision(double confidence)
    {
        var result = new VerificationResult
        {
            Confidence = confidence
        };

        if (confidence >= AutoAcceptThreshold)
        {
            result.Decision = Models.VerificationDecision.AutoAccept;
            result.IsIdentical = true;
            result.Message = "High confidence match - automatically accepted";
            
            _logger.LogInformation(
                "DECISION: AUTO-ACCEPT (confidence: {Confidence:F2})",
                confidence);
        }
        else if (confidence >= AcceptThreshold)
        {
            result.Decision = Models.VerificationDecision.Accept;
            result.IsIdentical = true;
            result.Message = "Good confidence match - accepted (may need soft review)";
            
            _logger.LogInformation(
                "DECISION: ACCEPT (confidence: {Confidence:F2})",
                confidence);
        }
        else if (confidence >= RetryThreshold)
        {
            result.Decision = Models.VerificationDecision.Retry;
            result.IsIdentical = false;
            result.Message = "Uncertain match - please try again with better lighting";
            
            _logger.LogInformation(
                "DECISION: RETRY (confidence: {Confidence:F2})",
                confidence);
        }
        else
        {
            result.Decision = Models.VerificationDecision.Reject;
            result.IsIdentical = false;
            result.Message = "Low confidence match - verification failed";
            
            _logger.LogInformation(
                "DECISION: REJECT (confidence: {Confidence:F2})",
                confidence);
        }

        return result;
    }

    /// <summary>
    /// Gets the decision thresholds for reference
    /// </summary>
    public (double autoAccept, double accept, double retry) GetThresholds()
    {
        return (AutoAcceptThreshold, AcceptThreshold, RetryThreshold);
    }
}
