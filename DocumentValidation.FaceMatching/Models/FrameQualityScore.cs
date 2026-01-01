namespace DocumentValidation.FaceMatching.Models;

/// <summary>
/// Quality metrics for a captured frame
/// </summary>
public class FrameQualityScore
{
    public int FrameIndex { get; set; }
    public double FaceSize { get; set; }
    public double Sharpness { get; set; }
    public double FrontalScore { get; set; }
    public double TotalScore { get; set; }
    public byte[] ImageData { get; set; } = Array.Empty<byte>();
}
