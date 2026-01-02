namespace DocumentValidation.FaceMatching;

/// <summary>
/// Verification method to use for face matching
/// </summary>
public enum VerificationMethod
{
    /// <summary>
    /// Use simulated verification (for testing without API credentials)
    /// </summary>
    Simulated,
    
    /// <summary>
    /// Use Azure Face API for real face verification
    /// </summary>
    AzureFaceAPI
}

/// <summary>
/// Configuration options for face matching service
/// </summary>
public class FaceMatchingOptions
{
    /// <summary>
    /// Verification method to use (default: Simulated)
    /// </summary>
    public VerificationMethod VerificationMethod { get; set; } = VerificationMethod.Simulated;

    /// <summary>
    /// Azure Face API endpoint (required when using AzureFaceAPI method)
    /// </summary>
    public string? FaceApiEndpoint { get; set; }

    /// <summary>
    /// Azure Face API key (required when using AzureFaceAPI method)
    /// </summary>
    public string? FaceApiKey { get; set; }

    /// <summary>
    /// Number of frames to capture in burst mode (default: 10)
    /// </summary>
    public int BurstFrameCount { get; set; } = 10;

    /// <summary>
    /// Delay between frames in milliseconds (default: 100ms for ~1 second total)
    /// </summary>
    public int FrameDelayMs { get; set; } = 100;
}
