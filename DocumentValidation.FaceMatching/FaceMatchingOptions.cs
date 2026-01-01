namespace DocumentValidation.FaceMatching;

/// <summary>
/// Configuration options for face matching service
/// </summary>
public class FaceMatchingOptions
{
    /// <summary>
    /// Azure Face API endpoint (optional, uses simulation if not provided)
    /// </summary>
    public string? FaceApiEndpoint { get; set; }

    /// <summary>
    /// Azure Face API key (optional, uses simulation if not provided)
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
