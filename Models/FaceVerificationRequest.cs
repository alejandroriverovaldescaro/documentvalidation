namespace DocumentValidation.Models;

public class FaceVerificationRequest
{
    public string DocumentId { get; set; } = string.Empty;
    public byte[] LivePhotoData { get; set; } = Array.Empty<byte>();
    public string PhotoFormat { get; set; } = string.Empty; // jpeg, png
}
