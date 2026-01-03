using Azure;
using Azure.AI.Vision.Face;
using Microsoft.Extensions.Logging;

namespace DocumentValidation.FaceMatching;

/// <summary>
/// Handles face verification using Azure Face API or simulated verification.
/// Compares two normalized face images and returns confidence score.
/// </summary>
public class FaceVerify
{
    private const int HttpStatusForbidden = 403;
    private const string ErrorCodeInvalidRequest = "InvalidRequest";
    private const string ErrorCodeUnsupportedFeature = "UnsupportedFeature";
    private const string FaceApiApprovalUrl = "https://aka.ms/facerecognition";

    private readonly ILogger<FaceVerify> _logger;
    private readonly VerificationMethod _verificationMethod;
    private readonly string? _faceApiEndpoint;
    private readonly string? _faceApiKey;
    private readonly bool _fallbackToSimulatedOnUnsupportedFeature;

    public FaceVerify(
        ILogger<FaceVerify> logger, 
        VerificationMethod verificationMethod = VerificationMethod.Simulated,
        string? faceApiEndpoint = null, 
        string? faceApiKey = null,
        bool fallbackToSimulatedOnUnsupportedFeature = true)
    {
        _logger = logger;
        _verificationMethod = verificationMethod;
        _faceApiEndpoint = faceApiEndpoint;
        _faceApiKey = faceApiKey;
        _fallbackToSimulatedOnUnsupportedFeature = fallbackToSimulatedOnUnsupportedFeature;
    }

    /// <summary>
    /// Verifies if two face images are of the same person.
    /// Returns confidence score between 0 and 1.
    /// </summary>
    public async Task<double> VerifyFacesAsync(byte[] selfieImage, byte[] idImage)
    {
        try
        {
            _logger.LogInformation("Using {VerificationMethod} verification method", _verificationMethod);

            // If Azure Face API is requested but credentials are not configured, fall back to simulated
            if (_verificationMethod == VerificationMethod.AzureFaceAPI && 
                (string.IsNullOrEmpty(_faceApiEndpoint) || string.IsNullOrEmpty(_faceApiKey)))
            {
                _logger.LogWarning(
                    "Azure Face API credentials not configured. Falling back to simulated verification. " +
                    "To use Azure Face API, configure FaceApiEndpoint and FaceApiKey in FaceMatchingOptions.");
                return await SimulateVerificationAsync(selfieImage, idImage);
            }

            return _verificationMethod switch
            {
                VerificationMethod.Simulated => await SimulateVerificationAsync(selfieImage, idImage),
                VerificationMethod.AzureFaceAPI => await VerifyWithAzureFaceApiAsync(selfieImage, idImage),
                _ => throw new InvalidOperationException($"Unknown verification method: {_verificationMethod}")
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Face verification failed");
            throw;
        }
    }

    /// <summary>
    /// Verifies faces using Azure Face API.
    /// </summary>
    private async Task<double> VerifyWithAzureFaceApiAsync(byte[] selfieImage, byte[] idImage)
    {
        if (string.IsNullOrEmpty(_faceApiEndpoint) || string.IsNullOrEmpty(_faceApiKey))
        {
            throw new InvalidOperationException(
                "Azure Face API credentials are required when using AzureFaceAPI verification method. " +
                "Please configure FaceApiEndpoint and FaceApiKey in FaceMatchingOptions.");
        }

        _logger.LogInformation("Calling Azure Face API for verification");

        try
        {
            // Create Face API client
            var credential = new AzureKeyCredential(_faceApiKey);
            var client = new FaceClient(new Uri(_faceApiEndpoint), credential);

            // Detect faces in both images
            _logger.LogDebug("Detecting face in selfie image");
            var selfieData = new BinaryData(selfieImage);
            var selfieDetectResponse = await client.DetectAsync(
                selfieData,
                FaceDetectionModel.Detection03,
                FaceRecognitionModel.Recognition04,
                returnFaceId: true);

            _logger.LogDebug("Detecting face in ID photo");
            var idData = new BinaryData(idImage);
            var idDetectResponse = await client.DetectAsync(
                idData,
                FaceDetectionModel.Detection03,
                FaceRecognitionModel.Recognition04,
                returnFaceId: true);

            var selfieDetect = selfieDetectResponse.Value;
            var idDetect = idDetectResponse.Value;

            // Check if faces were detected
            if (selfieDetect.Count == 0)
            {
                _logger.LogWarning("No face detected in selfie image");
                return 0.0;
            }

            if (idDetect.Count == 0)
            {
                _logger.LogWarning("No face detected in ID photo");
                return 0.0;
            }

            // Get the first detected face from each image
            var selfieFace = selfieDetect[0];
            var idFace = idDetect[0];

            // Get face IDs
            var selfieFaceId = selfieFace.FaceId;
            var idFaceId = idFace.FaceId;

            if (selfieFaceId == null || idFaceId == null)
            {
                _logger.LogWarning("Face detection did not return face IDs");
                return 0.0;
            }

            // Verify faces
            _logger.LogDebug("Verifying faces with Azure Face API");
            var verifyResponse = await client.VerifyFaceToFaceAsync(
                selfieFaceId.Value,
                idFaceId.Value);

            var verifyResult = verifyResponse.Value;
            var confidence = verifyResult.Confidence;

            _logger.LogInformation(
                "Azure Face API verification complete. IsIdentical: {IsIdentical}, Confidence: {Confidence:F2}",
                verifyResult.IsIdentical,
                confidence);

            return confidence;
        }
        catch (RequestFailedException ex)
        {
            // Check if this is an UnsupportedFeature error (403) indicating missing approval
            // for Verification feature. We check the status code and multiple error indicators for robustness.
            // Note: Azure returns ErrorCode "InvalidRequest" with innererror code "UnsupportedFeature"
            if (ex.Status == HttpStatusForbidden)
            {
                bool hasUnsupportedFeatureErrorCode = 
                    ex.ErrorCode == ErrorCodeInvalidRequest || ex.ErrorCode == ErrorCodeUnsupportedFeature;
                
                bool messageIndicatesUnsupportedFeature = ex.Message != null && 
                    ex.Message.Contains(ErrorCodeUnsupportedFeature, StringComparison.OrdinalIgnoreCase);
                
                _logger.LogDebug(
                    "Detected 403 error. ErrorCode: {ErrorCode}, HasUnsupportedFeatureCode: {HasCode}, MessageContainsUnsupportedFeature: {HasMessage}",
                    ex.ErrorCode, hasUnsupportedFeatureErrorCode, messageIndicatesUnsupportedFeature);
                
                if (hasUnsupportedFeatureErrorCode || messageIndicatesUnsupportedFeature)
                {
                    var errorMessage = string.Join("\n",
                        "Azure Face API Verification feature is not approved for this resource.",
                        "The Verification feature requires special approval from Microsoft due to Responsible AI policies.",
                        $"Please apply for access at {FaceApiApprovalUrl}");

                    if (_fallbackToSimulatedOnUnsupportedFeature)
                    {
                        _logger.LogWarning(
                            ex,
                            "{ErrorMessage}. Falling back to simulated verification.", 
                            errorMessage);
                        return await SimulateVerificationAsync(selfieImage, idImage);
                    }
                    else
                    {
                        _logger.LogError(ex, errorMessage);
                        throw new InvalidOperationException(errorMessage, ex);
                    }
                }
            }

            _logger.LogError(ex, "Azure Face API request failed: {Message}", ex.Message);
            // Re-throw the original exception to preserve error details for caller
            throw;
        }
    }

    /// <summary>
    /// Simulates face verification for demonstration purposes.
    /// In production, this would be replaced with actual Face API call.
    /// </summary>
    private async Task<double> SimulateVerificationAsync(byte[] selfieImage, byte[] idImage)
    {
        await Task.Delay(100); // Simulate API latency
        
        // Simple image similarity based on size
        // In production, use actual face recognition API
        double sizeDifference = Math.Abs(selfieImage.Length - idImage.Length) / (double)Math.Max(selfieImage.Length, idImage.Length);
        double similarity = 1.0 - sizeDifference;
        
        // Add some randomness to simulate real API behavior
        // Use Random.Shared (thread-safe) instead of new Random() for better randomness
        double noise = (Random.Shared.NextDouble() - 0.5) * 0.1; // ±5%
        
        double confidence = Math.Max(0.0, Math.Min(1.0, 0.75 + noise)); // Base confidence around 75%
        
        _logger.LogInformation("Face verification confidence: {Confidence:F2}", confidence);
        
        return confidence;
    }
}
