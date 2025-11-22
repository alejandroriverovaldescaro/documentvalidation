using DocumentValidation.Models;

namespace DocumentValidation.Services;

public interface IFaceVerificationService
{
    Task<FaceVerificationResult> VerifyFaceAsync(byte[] idPhoto, byte[] livePhoto);
    Task<double> CalculateFaceSimilarityAsync(byte[] photo1, byte[] photo2);
}
