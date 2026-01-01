using DocumentValidation.FaceMatching.Models;
using Microsoft.Extensions.Logging;

namespace DocumentValidation.FaceMatching;

/// <summary>
/// Main orchestrator for face matching pipeline.
/// Coordinates capture, normalization, verification, and decision making.
/// </summary>
public class FaceMatchingService
{
    private readonly ILogger<FaceMatchingService> _logger;
    private readonly FaceCapture _faceCapture;
    private readonly FaceNormalize _faceNormalize;
    private readonly FaceVerify _faceVerify;
    private readonly VerificationDecision _verificationDecision;

    public FaceMatchingService(
        ILogger<FaceMatchingService> logger,
        FaceCapture faceCapture,
        FaceNormalize faceNormalize,
        FaceVerify faceVerify,
        VerificationDecision verificationDecision)
    {
        _logger = logger;
        _faceCapture = faceCapture;
        _faceNormalize = faceNormalize;
        _faceVerify = faceVerify;
        _verificationDecision = verificationDecision;
    }

    /// <summary>
    /// Complete verification pipeline: capture, normalize, verify, decide.
    /// Takes burst of selfie frames and single ID photo.
    /// </summary>
    public async Task<VerificationResult> VerifyIdentityAsync(
        IEnumerable<byte[]> selfieFrames,
        byte[] idPhoto)
    {
        _logger.LogInformation("Starting face matching verification pipeline");

        try
        {
            // Step 1: Select best frame from burst capture
            _logger.LogInformation("Step 1: Selecting best selfie frame");
            var bestSelfie = await _faceCapture.SelectBestFrameAsync(selfieFrames);
            
            if (bestSelfie == null)
            {
                _logger.LogWarning("No suitable selfie frame found");
                return new VerificationResult
                {
                    Decision = Models.VerificationDecision.Retry,
                    IsIdentical = false,
                    Confidence = 0.0,
                    Message = "Could not detect face in selfie. Please ensure your face is clearly visible."
                };
            }

            // Step 2: Normalize both images
            _logger.LogInformation("Step 2: Normalizing selfie");
            var normalizedSelfie = await _faceNormalize.NormalizeFaceAsync(bestSelfie);
            
            _logger.LogInformation("Step 2: Normalizing ID photo");
            var normalizedId = await _faceNormalize.NormalizeFaceAsync(idPhoto);

            if (normalizedSelfie == null || normalizedId == null)
            {
                _logger.LogWarning("Face normalization failed");
                return new VerificationResult
                {
                    Decision = Models.VerificationDecision.Retry,
                    IsIdentical = false,
                    Confidence = 0.0,
                    Message = "Could not process images. Please try again."
                };
            }

            // Step 3: Verify faces
            _logger.LogInformation("Step 3: Verifying faces");
            var confidence = await _faceVerify.VerifyFacesAsync(normalizedSelfie, normalizedId);

            // Step 4: Make decision
            _logger.LogInformation("Step 4: Making verification decision");
            var result = _verificationDecision.MakeDecision(confidence);

            _logger.LogInformation(
                "Verification complete: {Decision} (confidence: {Confidence:F2})",
                result.Decision,
                result.Confidence);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Verification pipeline failed");
            return new VerificationResult
            {
                Decision = Models.VerificationDecision.Reject,
                IsIdentical = false,
                Confidence = 0.0,
                Message = "Verification failed due to technical error. Please try again."
            };
        }
    }

    /// <summary>
    /// Simplified verification with single selfie image (no burst capture).
    /// Useful for testing or when burst capture is not available.
    /// </summary>
    public async Task<VerificationResult> VerifyIdentitySimpleAsync(
        byte[] selfieImage,
        byte[] idPhoto)
    {
        return await VerifyIdentityAsync(new[] { selfieImage }, idPhoto);
    }
}
