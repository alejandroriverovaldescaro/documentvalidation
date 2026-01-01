# Document Validation - Face Matching Pipeline

A lightweight, production-ready face matching system for identity verification. This repository implements an improved "selfie vs photo ID" face matching pipeline designed to maximize user success rates while maintaining security.

## 🎯 Project Goals

- **User-Friendly**: Simple "look at the camera" instruction - no technical requirements
- **Reliable**: Smart frame selection and normalization increase match success
- **Maintainable**: Clean, modular code with minimal dependencies
- **Production-Ready**: Includes logging, testing, and monitoring capabilities

## 🚀 Features

### Intelligent Frame Capture
- Captures 5-10 frames in burst mode (~1 second)
- Automatically selects best frame based on:
  - **Face size** (larger = closer to camera)
  - **Sharpness** (Laplacian variance for focus detection)
  - **Frontal pose** (horizontal eye alignment)

### Face Normalization
- Detects faces in both selfie and ID photo
- Crops tightly to face region with padding
- Aligns faces (horizontal eye alignment)
- Resizes to standard dimensions (256x256)

### Smart Decision Logic
Uses confidence bands instead of hard thresholds:

| Score Range | Decision | Action |
|-------------|----------|--------|
| ≥ 0.80 | Auto-Accept | Process immediately |
| 0.60-0.79 | Accept | May queue for soft review |
| 0.45-0.59 | Retry | Ask user to retry |
| < 0.45 | Reject | Manual review or reject |

## 📦 Project Structure

```
documentvalidation/
├── DocumentValidation.FaceMatching/       # Core library
│   ├── Models/                            # Data models
│   ├── FaceCapture.cs                     # Burst capture & selection
│   ├── FaceNormalize.cs                   # Detection & normalization
│   ├── FaceVerify.cs                      # Face comparison API
│   ├── VerificationDecision.cs            # Decision logic
│   ├── FaceMatchingService.cs             # Main orchestrator
│   └── README.md                          # Detailed documentation
├── DocumentValidation.FaceMatching.Tests/ # Unit tests
│   └── VerificationDecisionTests.cs       # Decision logic tests
├── DocumentValidation.Example/            # Example console app
│   └── Program.cs                         # Usage demonstration
└── README.md                              # This file
```

## 🏃 Quick Start

### Prerequisites
- .NET 8.0 SDK or later
- (Optional) Azure Face API credentials

### 1. Clone and Build

```bash
git clone https://github.com/alejandroriverovaldescaro/documentvalidation.git
cd documentvalidation
dotnet build
```

### 2. Run Tests

```bash
dotnet test
```

All tests should pass:
```
Test Run Successful.
Total tests: 14
     Passed: 14
```

### 3. Run Example

```bash
cd DocumentValidation.Example
dotnet run
```

You should see output showing the verification pipeline in action with simulated images.

## 💻 Usage

### Basic Integration

```csharp
using DocumentValidation.FaceMatching;
using Microsoft.Extensions.DependencyInjection;

// Setup
var services = new ServiceCollection();
services.AddFaceMatching();
services.AddLogging();

var serviceProvider = services.BuildServiceProvider();
var faceMatchingService = serviceProvider.GetRequiredService<FaceMatchingService>();

// Verify
var selfieFrames = GetCameraFrames(); // Your camera capture logic
var idPhoto = LoadIdPhoto();          // Load from storage

var result = await faceMatchingService.VerifyIdentityAsync(selfieFrames, idPhoto);

// Handle result
if (result.Decision == VerificationDecision.AutoAccept)
{
    // Process verification
}
```

See [DocumentValidation.FaceMatching/README.md](DocumentValidation.FaceMatching/README.md) for detailed documentation.

## 🔧 Configuration

### Azure Face API (Optional)

```csharp
services.AddFaceMatching(options =>
{
    options.FaceApiEndpoint = "https://your-endpoint.cognitiveservices.azure.com/";
    options.FaceApiKey = "your-api-key";
    options.BurstFrameCount = 10;
    options.FrameDelayMs = 100;
});
```

Without API credentials, the system uses simulation mode for testing.

## 🧪 Testing

The project includes comprehensive unit tests for decision logic:

```bash
dotnet test --logger "console;verbosity=detailed"
```

Tests cover:
- All threshold boundaries
- Edge cases (0.0, 1.0, exact thresholds)
- Decision correctness
- Message validation

## 📊 Design Principles

### 1. User-Centric
- **Simple instructions**: "Look at the camera"
- **Automatic quality**: System selects best frame
- **Helpful feedback**: Clear retry messages

### 2. Pragmatic
- **No ML training**: Uses existing APIs
- **Deterministic**: Clear, testable logic
- **Low latency**: ~1 second total time

### 3. Production-Ready
- **Logging**: All decisions logged with confidence scores
- **Testing**: Comprehensive test coverage
- **Monitoring**: Track success/retry/reject rates
- **Maintainable**: Clean, documented code

## 🔒 Security Considerations

- Validate input image sizes and formats
- Rate limit API calls
- Store verification logs for audit
- Run security scans (CodeQL recommended)
- Never store raw biometric data

## 📈 Monitoring

Track these metrics in production:
- **Auto-accept rate**: Target > 60%
- **Accept rate**: Target 20-30%
- **Retry rate**: Target < 15%
- **Reject rate**: Target < 5%
- **Average confidence**: Target > 0.70

## 🤝 Contributing

Contributions are welcome! Please:
1. Fork the repository
2. Create a feature branch
3. Add tests for new functionality
4. Ensure all tests pass
5. Submit a pull request

## 📄 License

MIT License - see LICENSE file for details.

## 📞 Support

For issues or questions:
- Open an issue on GitHub
- Review documentation in [DocumentValidation.FaceMatching/README.md](DocumentValidation.FaceMatching/README.md)

## 🙏 Acknowledgments

Built with:
- [ImageSharp](https://github.com/SixLabors/ImageSharp) - Image processing
- [Azure Face API](https://azure.microsoft.com/en-us/services/cognitive-services/face/) - Face recognition (optional)
- .NET 8.0 - Runtime platform
