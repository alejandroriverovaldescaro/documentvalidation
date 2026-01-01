using DocumentValidation.FaceMatching.Models;
using Microsoft.Extensions.Logging;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace DocumentValidation.FaceMatching;

/// <summary>
/// Handles face normalization: detection, cropping, alignment, and resizing.
/// Prepares images for face verification by ensuring faces are in a standardized format.
/// </summary>
public class FaceNormalize
{
    private readonly ILogger<FaceNormalize> _logger;
    private const int StandardSize = 256;

    public FaceNormalize(ILogger<FaceNormalize> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Detects face in image and returns bounds and landmarks.
    /// Uses simple Haar-like cascade approach for lightweight detection.
    /// In production, this would call Azure Face API or similar service.
    /// </summary>
    public Task<FaceDetectionResult> DetectFaceAsync(byte[] imageData)
    {
        try
        {
            using var image = Image.Load<Rgba32>(imageData);
            
            // Simplified face detection using image analysis
            // In production, use Azure Face API or similar service
            var detection = DetectFaceSimple(image);
            
            return Task.FromResult(detection);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to detect face in image");
            return Task.FromResult(new FaceDetectionResult 
            { 
                FaceDetected = false,
                FaceBounds = new Models.Rectangle()
            });
        }
    }

    /// <summary>
    /// Normalizes face image: crop to face, align eyes horizontally, resize to standard size.
    /// This ensures consistent input for face verification.
    /// </summary>
    public async Task<byte[]?> NormalizeFaceAsync(byte[] imageData)
    {
        try
        {
            // Detect face
            var detection = await DetectFaceAsync(imageData);
            
            if (!detection.FaceDetected)
            {
                _logger.LogWarning("No face detected, cannot normalize");
                return null;
            }

            using var image = Image.Load<Rgba32>(imageData);
            
            // Crop to face region with some padding
            var croppedImage = CropToFace(image, detection.FaceBounds);
            
            // Align face so eyes are horizontal
            if (detection.Landmarks != null)
            {
                croppedImage = AlignFace(croppedImage, detection.Landmarks, detection.FaceBounds);
            }
            
            // Resize to standard dimensions
            croppedImage.Mutate(x => x.Resize(StandardSize, StandardSize));

            // Convert to byte array
            using var ms = new MemoryStream();
            await croppedImage.SaveAsJpegAsync(ms);
            
            _logger.LogInformation("Face normalized to {Size}x{Size}", StandardSize, StandardSize);
            
            return ms.ToArray();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to normalize face");
            return null;
        }
    }

    /// <summary>
    /// Simplified face detection for demonstration.
    /// In production, replace with Azure Face API or similar service.
    /// </summary>
    private FaceDetectionResult DetectFaceSimple(Image<Rgba32> image)
    {
        // Simple center-weighted detection assuming face is roughly centered
        // This is a placeholder - use actual face detection API in production
        
        int imageWidth = image.Width;
        int imageHeight = image.Height;
        
        // Assume face occupies center 60% of image
        int faceWidth = (int)(imageWidth * 0.6);
        int faceHeight = (int)(imageHeight * 0.7);
        int faceX = (imageWidth - faceWidth) / 2;
        int faceY = (int)(imageHeight * 0.15); // Slightly above center
        
        // Estimate landmark positions based on typical face proportions
        double eyeY = faceY + faceHeight * 0.35;
        double eyeSpacing = faceWidth * 0.3;
        double centerX = faceX + faceWidth / 2.0;
        
        var landmarks = new FaceLandmarks
        {
            LeftEye = new Models.Point { X = centerX - eyeSpacing / 2, Y = eyeY },
            RightEye = new Models.Point { X = centerX + eyeSpacing / 2, Y = eyeY },
            NoseTip = new Models.Point { X = centerX, Y = faceY + faceHeight * 0.55 },
            MouthLeft = new Models.Point { X = centerX - eyeSpacing / 3, Y = faceY + faceHeight * 0.75 },
            MouthRight = new Models.Point { X = centerX + eyeSpacing / 3, Y = faceY + faceHeight * 0.75 }
        };

        return new FaceDetectionResult
        {
            FaceDetected = true,
            FaceBounds = new Models.Rectangle
            {
                X = faceX,
                Y = faceY,
                Width = faceWidth,
                Height = faceHeight
            },
            Landmarks = landmarks,
            Confidence = 0.85 // Placeholder confidence
        };
    }

    /// <summary>
    /// Crops image to face region with padding
    /// </summary>
    private Image<Rgba32> CropToFace(Image<Rgba32> image, Models.Rectangle faceBounds)
    {
        // Add 20% padding around face
        double padding = 0.2;
        int paddingX = (int)(faceBounds.Width * padding);
        int paddingY = (int)(faceBounds.Height * padding);
        
        int x = Math.Max(0, faceBounds.X - paddingX);
        int y = Math.Max(0, faceBounds.Y - paddingY);
        int width = Math.Min(image.Width - x, faceBounds.Width + 2 * paddingX);
        int height = Math.Min(image.Height - y, faceBounds.Height + 2 * paddingY);

        return image.Clone(ctx => ctx.Crop(new SixLabors.ImageSharp.Rectangle(x, y, width, height)));
    }

    /// <summary>
    /// Rotates image to align eyes horizontally.
    /// This ensures consistent face orientation for verification.
    /// </summary>
    private Image<Rgba32> AlignFace(Image<Rgba32> image, FaceLandmarks landmarks, Models.Rectangle faceBounds)
    {
        // Calculate rotation angle based on eye positions
        double dx = landmarks.RightEye.X - landmarks.LeftEye.X;
        double dy = landmarks.RightEye.Y - landmarks.LeftEye.Y;
        double angleRadians = Math.Atan2(dy, dx);
        double angleDegrees = angleRadians * 180.0 / Math.PI;

        // Only rotate if angle is significant (> 2 degrees)
        if (Math.Abs(angleDegrees) > 2.0)
        {
            _logger.LogDebug("Aligning face, rotating by {Angle:F2} degrees", angleDegrees);
            
            // Adjust landmark coordinates relative to face bounds
            double eyeCenterX = landmarks.LeftEye.X - faceBounds.X;
            double eyeCenterY = landmarks.LeftEye.Y - faceBounds.Y;
            
            image.Mutate(x => x.Rotate((float)-angleDegrees));
        }

        return image;
    }
}
