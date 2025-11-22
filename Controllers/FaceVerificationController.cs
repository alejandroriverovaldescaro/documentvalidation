using DocumentValidation.Models;
using DocumentValidation.Services;
using Microsoft.AspNetCore.Mvc;

namespace DocumentValidation.Controllers;

[ApiController]
[Route("api/[controller]")]
public class FaceVerificationController : ControllerBase
{
    private readonly IFaceVerificationService _faceVerificationService;
    private readonly ILogger<FaceVerificationController> _logger;
    private static readonly Dictionary<Guid, FaceVerificationRecord> _verificationHistory = new();

    public FaceVerificationController(
        IFaceVerificationService faceVerificationService,
        ILogger<FaceVerificationController> logger)
    {
        _faceVerificationService = faceVerificationService;
        _logger = logger;
    }

    [HttpPost("verify")]
    public async Task<ActionResult<FaceVerificationResult>> VerifyFace([FromForm] IFormFile? idPhoto, [FromForm] IFormFile? livePhoto, [FromForm] string? documentId)
    {
        try
        {
            _logger.LogInformation("Face verification request received");

            if (idPhoto == null || livePhoto == null)
            {
                return BadRequest(new { error = "Both ID photo and live photo are required" });
            }

            if (string.IsNullOrEmpty(documentId))
            {
                documentId = Guid.NewGuid().ToString();
            }

            // Read photos into byte arrays
            byte[] idPhotoBytes;
            byte[] livePhotoBytes;

            using (var ms = new MemoryStream())
            {
                await idPhoto.CopyToAsync(ms);
                idPhotoBytes = ms.ToArray();
            }

            using (var ms = new MemoryStream())
            {
                await livePhoto.CopyToAsync(ms);
                livePhotoBytes = ms.ToArray();
            }

            // Perform verification
            var result = await _faceVerificationService.VerifyFaceAsync(idPhotoBytes, livePhotoBytes);

            // Store verification record
            var verificationId = Guid.NewGuid();
            var record = new FaceVerificationRecord
            {
                VerificationId = verificationId,
                DocumentId = Guid.Parse(documentId),
                VerificationTimestamp = result.VerificationTimestamp,
                IsMatch = result.IsMatch,
                ConfidenceScore = (decimal)result.ConfidenceScore,
                VerificationStatus = result.IsMatch ? "Success" : "Failed",
                ErrorMessage = result.IsMatch ? null : result.Message
            };

            _verificationHistory[verificationId] = record;

            _logger.LogInformation("Face verification completed. Match: {IsMatch}, Confidence: {ConfidenceScore}%", 
                result.IsMatch, result.ConfidenceScore);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing face verification request");
            return StatusCode(500, new { error = "Internal server error during verification" });
        }
    }

    [HttpGet("history/{documentId}")]
    public async Task<ActionResult<List<FaceVerificationRecord>>> GetVerificationHistory(Guid documentId)
    {
        try
        {
            await Task.CompletedTask; // Async placeholder
            var history = _verificationHistory.Values
                .Where(v => v.DocumentId == documentId)
                .OrderByDescending(v => v.VerificationTimestamp)
                .ToList();

            return Ok(history);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving verification history");
            return StatusCode(500, new { error = "Error retrieving verification history" });
        }
    }

    [HttpGet("status/{verificationId}")]
    public async Task<ActionResult<FaceVerificationRecord>> GetVerificationStatus(Guid verificationId)
    {
        try
        {
            await Task.CompletedTask; // Async placeholder
            if (_verificationHistory.TryGetValue(verificationId, out var record))
            {
                return Ok(record);
            }

            return NotFound(new { error = "Verification record not found" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving verification status");
            return StatusCode(500, new { error = "Error retrieving verification status" });
        }
    }
}
