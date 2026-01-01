namespace DocumentValidation.FaceMatching.Models;

/// <summary>
/// Result of face detection containing face bounding box and landmarks
/// </summary>
public class FaceDetectionResult
{
    public bool FaceDetected { get; set; }
    public required Rectangle FaceBounds { get; set; }
    public FaceLandmarks? Landmarks { get; set; }
    public double Confidence { get; set; }
}

/// <summary>
/// Rectangle defining face bounds in image
/// </summary>
public class Rectangle
{
    public int X { get; set; }
    public int Y { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
}

/// <summary>
/// Face landmarks for alignment (eyes, nose, mouth corners)
/// </summary>
public class FaceLandmarks
{
    public required Point LeftEye { get; set; }
    public required Point RightEye { get; set; }
    public required Point NoseTip { get; set; }
    public required Point MouthLeft { get; set; }
    public required Point MouthRight { get; set; }
}

/// <summary>
/// 2D point coordinate
/// </summary>
public class Point
{
    public double X { get; set; }
    public double Y { get; set; }
}
