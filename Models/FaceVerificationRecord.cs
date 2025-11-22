namespace DocumentValidation.Models;

public class FaceVerificationRecord
{
    public Guid VerificationId { get; set; }
    public Guid DocumentId { get; set; }
    public DateTime VerificationTimestamp { get; set; }
    public bool IsMatch { get; set; }
    public decimal ConfidenceScore { get; set; }
    public string? LivePhotoPath { get; set; }
    public string VerificationStatus { get; set; } = string.Empty; // 'Success', 'Failed', 'Error'
    public string? ErrorMessage { get; set; }
    public string? CreatedBy { get; set; }
}
