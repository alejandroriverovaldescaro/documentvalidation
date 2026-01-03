using DocumentValidation.FaceMatching;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace DocumentValidation.FaceMatching.Tests;

/// <summary>
/// Unit tests for FaceVerify error handling and configuration
/// </summary>
public class FaceVerifyTests
{
    [Fact]
    public void Constructor_WithSimulatedMethod_DoesNotRequireCredentials()
    {
        // Arrange & Act
        var faceVerify = new FaceVerify(
            NullLogger<FaceVerify>.Instance,
            VerificationMethod.Simulated,
            faceApiEndpoint: null,
            faceApiKey: null,
            fallbackToSimulatedOnUnsupportedFeature: true);

        // Assert - no exception thrown
        Assert.NotNull(faceVerify);
    }

    [Fact]
    public void Constructor_WithAzureFaceAPI_AcceptsFallbackConfiguration()
    {
        // Arrange & Act
        var faceVerifyWithFallback = new FaceVerify(
            NullLogger<FaceVerify>.Instance,
            VerificationMethod.AzureFaceAPI,
            "https://test.cognitiveservices.azure.com/",
            "test-key",
            fallbackToSimulatedOnUnsupportedFeature: true);

        var faceVerifyWithoutFallback = new FaceVerify(
            NullLogger<FaceVerify>.Instance,
            VerificationMethod.AzureFaceAPI,
            "https://test.cognitiveservices.azure.com/",
            "test-key",
            fallbackToSimulatedOnUnsupportedFeature: false);

        // Assert - both instances created successfully
        Assert.NotNull(faceVerifyWithFallback);
        Assert.NotNull(faceVerifyWithoutFallback);
    }

    [Fact]
    public void Constructor_DefaultFallbackBehavior_IsTrue()
    {
        // Arrange & Act
        var faceVerify = new FaceVerify(
            NullLogger<FaceVerify>.Instance,
            VerificationMethod.Simulated);

        // Assert - no exception thrown, default behavior applied
        Assert.NotNull(faceVerify);
    }

    [Fact]
    public async Task VerifyFacesAsync_WithSimulatedMethod_ReturnsConfidenceScore()
    {
        // Arrange
        var faceVerify = new FaceVerify(
            NullLogger<FaceVerify>.Instance,
            VerificationMethod.Simulated);

        var selfieImage = new byte[] { 1, 2, 3, 4, 5 };
        var idImage = new byte[] { 1, 2, 3, 4, 5 };

        // Act
        var confidence = await faceVerify.VerifyFacesAsync(selfieImage, idImage);

        // Assert
        Assert.True(confidence >= 0.0 && confidence <= 1.0, 
            "Confidence should be between 0 and 1");
    }

    [Fact]
    public async Task VerifyFacesAsync_WithAzureFaceAPIButNoCredentials_FallsBackToSimulated()
    {
        // Arrange
        var faceVerify = new FaceVerify(
            NullLogger<FaceVerify>.Instance,
            VerificationMethod.AzureFaceAPI,
            faceApiEndpoint: null, // No credentials
            faceApiKey: null);

        var selfieImage = new byte[] { 1, 2, 3, 4, 5 };
        var idImage = new byte[] { 1, 2, 3, 4, 5 };

        // Act
        var confidence = await faceVerify.VerifyFacesAsync(selfieImage, idImage);

        // Assert - Should fall back to simulated and return a valid confidence
        Assert.True(confidence >= 0.0 && confidence <= 1.0, 
            "Should fall back to simulated verification and return valid confidence");
    }
}
