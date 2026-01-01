using DocumentValidation.FaceMatching.Models;
using Microsoft.Extensions.Logging;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace DocumentValidation.FaceMatching;

/// <summary>
/// Handles burst capture of frames and selection of the best frame based on quality heuristics.
/// Users only need to "look at the camera" - no special instructions required.
/// </summary>
public class FaceCapture
{
    private readonly ILogger<FaceCapture> _logger;
    private readonly FaceNormalize _faceNormalize;

    public FaceCapture(ILogger<FaceCapture> logger, FaceNormalize faceNormalize)
    {
        _logger = logger;
        _faceNormalize = faceNormalize;
    }

    /// <summary>
    /// Selects the best frame from a burst of captured frames.
    /// Selection criteria:
    /// - Largest detected face (closer to camera)
    /// - Highest sharpness (using variance of Laplacian)
    /// - Most frontal pose (eyes horizontal, minimal head rotation)
    /// </summary>
    public async Task<byte[]?> SelectBestFrameAsync(IEnumerable<byte[]> frames)
    {
        var frameList = frames.ToList();
        if (!frameList.Any())
        {
            _logger.LogWarning("No frames provided for selection");
            return null;
        }

        _logger.LogInformation("Evaluating {Count} frames for best quality", frameList.Count);

        var qualityScores = new List<FrameQualityScore>();

        for (int i = 0; i < frameList.Count; i++)
        {
            var score = await EvaluateFrameQualityAsync(frameList[i], i);
            if (score != null)
            {
                qualityScores.Add(score);
            }
        }

        if (!qualityScores.Any())
        {
            _logger.LogWarning("No frames with detectable faces");
            return null;
        }

        // Select frame with highest total score
        var bestFrame = qualityScores.OrderByDescending(s => s.TotalScore).First();
        
        _logger.LogInformation(
            "Selected frame {Index} with score {Score:F2} (FaceSize: {FaceSize:F2}, Sharpness: {Sharpness:F2}, Frontal: {Frontal:F2})",
            bestFrame.FrameIndex,
            bestFrame.TotalScore,
            bestFrame.FaceSize,
            bestFrame.Sharpness,
            bestFrame.FrontalScore);

        return bestFrame.ImageData;
    }

    /// <summary>
    /// Evaluates quality of a single frame using multiple heuristics
    /// </summary>
    private async Task<FrameQualityScore?> EvaluateFrameQualityAsync(byte[] imageData, int frameIndex)
    {
        try
        {
            using var image = Image.Load<Rgba32>(imageData);
            
            // Detect face to get bounds and landmarks
            var detection = await _faceNormalize.DetectFaceAsync(imageData);
            
            if (!detection.FaceDetected)
            {
                return null;
            }

            // Calculate face size score (normalized by image size)
            double imageArea = image.Width * image.Height;
            double faceArea = detection.FaceBounds.Width * detection.FaceBounds.Height;
            double faceSizeScore = Math.Sqrt(faceArea / imageArea); // Normalize to 0-1 range

            // Calculate sharpness using Laplacian variance
            double sharpnessScore = CalculateSharpness(image, detection.FaceBounds);

            // Calculate frontal pose score based on eye alignment
            double frontalScore = CalculateFrontalScore(detection.Landmarks);

            // Weighted total score
            // Face size is most important (closer = better), then sharpness, then frontal
            double totalScore = (faceSizeScore * 0.5) + (sharpnessScore * 0.3) + (frontalScore * 0.2);

            return new FrameQualityScore
            {
                FrameIndex = frameIndex,
                FaceSize = faceSizeScore,
                Sharpness = sharpnessScore,
                FrontalScore = frontalScore,
                TotalScore = totalScore,
                ImageData = imageData
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to evaluate frame {Index}", frameIndex);
            return null;
        }
    }

    /// <summary>
    /// Calculates image sharpness using variance of Laplacian.
    /// Higher variance indicates sharper edges and better focus.
    /// </summary>
    private double CalculateSharpness(Image<Rgba32> image, Models.Rectangle faceBounds)
    {
        // Extract face region for sharpness calculation
        var faceRegion = image.Clone(ctx => ctx.Crop(
            new SixLabors.ImageSharp.Rectangle(
                faceBounds.X,
                faceBounds.Y,
                faceBounds.Width,
                faceBounds.Height)));

        // Convert to grayscale for Laplacian calculation
        faceRegion.Mutate(x => x.Grayscale());

        // Simple Laplacian approximation using pixel differences
        double sumSquares = 0;
        int count = 0;

        for (int y = 1; y < faceRegion.Height - 1; y++)
        {
            for (int x = 1; x < faceRegion.Width - 1; x++)
            {
                var center = faceRegion[x, y].R;
                var left = faceRegion[x - 1, y].R;
                var right = faceRegion[x + 1, y].R;
                var top = faceRegion[x, y - 1].R;
                var bottom = faceRegion[x, y + 1].R;

                // Laplacian kernel: center*4 - (left + right + top + bottom)
                double laplacian = (center * 4) - (left + right + top + bottom);
                sumSquares += laplacian * laplacian;
                count++;
            }
        }

        // Normalize variance to 0-1 range (using empirical max of 10000)
        double variance = sumSquares / count;
        return Math.Min(variance / 10000.0, 1.0);
    }

    /// <summary>
    /// Calculates how frontal the face is based on eye alignment.
    /// Score of 1.0 means eyes are perfectly horizontal.
    /// </summary>
    private double CalculateFrontalScore(FaceLandmarks? landmarks)
    {
        if (landmarks == null)
        {
            return 0.5; // Neutral score if no landmarks
        }

        // Calculate angle between eyes
        double dx = landmarks.RightEye.X - landmarks.LeftEye.X;
        double dy = landmarks.RightEye.Y - landmarks.LeftEye.Y;
        
        // Angle from horizontal (in radians)
        double angleRadians = Math.Abs(Math.Atan2(dy, dx));
        
        // Convert to score: 0 radians = 1.0, π/4 radians (45°) = 0.0
        double maxAngle = Math.PI / 4; // 45 degrees
        double score = 1.0 - Math.Min(angleRadians / maxAngle, 1.0);
        
        return score;
    }
}
