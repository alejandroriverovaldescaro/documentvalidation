# Face Matching Pipeline for Identity Verification

A lightweight, production-ready face matching system for "selfie vs photo ID" verification. Designed to maximize user success rate while maintaining security through simple, pragmatic improvements.

## Features

- **Burst Capture & Best Frame Selection**: Captures 5-10 frames and automatically selects the best one based on:
  - Largest detected face
  - Highest sharpness (Laplacian variance)
  - Most frontal pose (horizontal eye alignment)

- **Face Normalization**: 
  - Detects faces in both selfie and ID photo
  - Crops tightly to face region
  - Aligns face so eyes are horizontal
  - Resizes to standard size (256x256)

- **Smart Decision Logic**: Uses confidence bands instead of hard thresholds:
  - `>= 0.80`: Auto-accept (high confidence)
  - `0.60-0.79`: Accept (may need soft review)
  - `0.45-0.59`: Request retry
  - `< 0.45`: Reject

- **User-Friendly**: Simple instruction: "Look at the camera" - the system adapts to users

## Installation

Add the NuGet package to your project:

```bash
dotnet add package DocumentValidation.FaceMatching
```

Or add to your `.csproj`:

```xml
<PackageReference Include="DocumentValidation.FaceMatching" Version="1.0.0" />
```

## Quick Start

### 1. Register Services

```csharp
using DocumentValidation.FaceMatching;
using Microsoft.Extensions.DependencyInjection;

var services = new ServiceCollection();

// Add face matching services
services.AddFaceMatching(options =>
{
    // Optional: Configure Azure Face API
    options.FaceApiEndpoint = "https://your-endpoint.cognitiveservices.azure.com/";
    options.FaceApiKey = "your-api-key";
    
    // Optional: Configure burst capture
    options.BurstFrameCount = 10;
    options.FrameDelayMs = 100;
});

// Add logging
services.AddLogging();

var serviceProvider = services.BuildServiceProvider();
```

### 2. Verify Identity

```csharp
var faceMatchingService = serviceProvider.GetRequiredService<FaceMatchingService>();

// Capture burst of selfie frames (from camera)
var selfieFrames = new List<byte[]>
{
    frame1, frame2, frame3, frame4, frame5,
    frame6, frame7, frame8, frame9, frame10
};

// Load ID photo
var idPhoto = File.ReadAllBytes("id_photo.jpg");

// Run verification pipeline
var result = await faceMatchingService.VerifyIdentityAsync(selfieFrames, idPhoto);

// Handle result based on decision
switch (result.Decision)
{
    case VerificationDecision.AutoAccept:
        Console.WriteLine($"✓ Verified! (confidence: {result.Confidence:P0})");
        break;
        
    case VerificationDecision.Accept:
        Console.WriteLine($"✓ Verified (may need review) (confidence: {result.Confidence:P0})");
        break;
        
    case VerificationDecision.Retry:
        Console.WriteLine($"⚠ Please try again with better lighting (confidence: {result.Confidence:P0})");
        break;
        
    case VerificationDecision.Reject:
        Console.WriteLine($"✗ Verification failed (confidence: {result.Confidence:P0})");
        break;
}
```

### 3. Simple Verification (Single Frame)

If you don't have burst capture available:

```csharp
var selfieImage = File.ReadAllBytes("selfie.jpg");
var idPhoto = File.ReadAllBytes("id_photo.jpg");

var result = await faceMatchingService.VerifyIdentitySimpleAsync(selfieImage, idPhoto);
```

## Architecture

The pipeline consists of four main components:

```
┌─────────────┐      ┌──────────────┐      ┌─────────────┐      ┌──────────────────┐
│             │      │              │      │             │      │                  │
│ FaceCapture │─────▶│ FaceNormalize│─────▶│ FaceVerify  │─────▶│ Verification     │
│             │      │              │      │             │      │ Decision         │
│  - Burst    │      │  - Detect    │      │  - Compare  │      │  - Thresholds    │
│  - Quality  │      │  - Crop      │      │  - API call │      │  - Bands         │
│  - Select   │      │  - Align     │      │  - Score    │      │  - Logging       │
│             │      │  - Resize    │      │             │      │                  │
└─────────────┘      └──────────────┘      └─────────────┘      └──────────────────┘
```

### Components

1. **FaceCapture** (`FaceCapture.cs`): Burst capture and best frame selection
2. **FaceNormalize** (`FaceNormalize.cs`): Face detection, cropping, alignment, resizing
3. **FaceVerify** (`FaceVerify.cs`): Face comparison using Azure Face API
4. **VerificationDecision** (`VerificationDecision.cs`): Threshold-based decision logic

## Configuration

### Azure Face API (Optional)

The system works with or without Azure Face API:

- **With API**: Configure endpoint and key for production face recognition
- **Without API**: Uses simulation mode for testing/development

```csharp
services.AddFaceMatching(options =>
{
    options.FaceApiEndpoint = Environment.GetEnvironmentVariable("FACE_API_ENDPOINT");
    options.FaceApiKey = Environment.GetEnvironmentVariable("FACE_API_KEY");
});
```

### Decision Thresholds

The default thresholds are optimized for security and user experience:

| Threshold | Value | Decision | Description |
|-----------|-------|----------|-------------|
| Auto-Accept | 0.80 | Accept automatically | High confidence match |
| Accept | 0.60 | Accept (soft review) | Good confidence match |
| Retry | 0.45 | Request retry | Uncertain, may improve |
| Reject | < 0.45 | Reject | Low confidence |

To customize thresholds, modify constants in `VerificationDecision.cs`.

## Performance

- **Latency**: ~1 second per verification (including burst capture)
- **Dependencies**: Minimal (ImageSharp, Azure SDK)
- **Face Detection**: Lightweight heuristics (or Azure Face API)
- **Memory**: Low footprint, suitable for serverless

## Testing

Run the unit tests:

```bash
dotnet test
```

The test suite includes comprehensive tests for decision logic covering:
- All threshold boundaries
- Edge cases (0, 1, exact thresholds)
- Message validation

## Production Considerations

### Security
- Always run CodeQL or similar security scanning
- Validate input image sizes and formats
- Rate limit API calls to prevent abuse
- Store verification logs for audit

### Monitoring
- Log all verification attempts with confidence scores
- Track decision distribution (accept/retry/reject rates)
- Monitor API latency and errors
- Alert on unusual patterns (high retry/reject rates)

### Scaling
- Use caching for frequently verified IDs
- Consider batch processing for large volumes
- Implement circuit breakers for API calls
- Use message queues for async processing

## UX Guidelines

### User Instructions
Keep it simple:
- ✓ "Look at the camera"
- ✗ Don't ask for specific angles, lighting, or expressions

### Retry Flow
- Only show retry option for scores in retry band (0.45-0.59)
- Provide helpful hints: "Try better lighting" or "Move closer to camera"
- Limit retries to 2-3 attempts before requiring manual review

### Error Messages
- Be specific but not technical
- Provide actionable guidance
- Maintain professional tone

## License

MIT License - see LICENSE file for details.

## Support

For issues or questions, please open an issue on GitHub.
