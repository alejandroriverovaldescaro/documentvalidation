using DocumentValidation.FaceMatching;
using DocumentValidation.FaceMatching.Models;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace DocumentValidation.FaceMatching.Tests;

/// <summary>
/// Unit tests for VerificationDecision threshold logic
/// </summary>
public class VerificationDecisionTests
{
    private readonly VerificationDecision _decision;

    public VerificationDecisionTests()
    {
        _decision = new VerificationDecision(NullLogger<VerificationDecision>.Instance);
    }

    [Fact]
    public void MakeDecision_HighConfidence_ReturnsAutoAccept()
    {
        // Arrange
        double confidence = 0.85;

        // Act
        var result = _decision.MakeDecision(confidence);

        // Assert
        Assert.Equal(Models.VerificationDecision.AutoAccept, result.Decision);
        Assert.True(result.IsIdentical);
        Assert.Equal(0.85, result.Confidence);
    }

    [Fact]
    public void MakeDecision_ExactAutoAcceptThreshold_ReturnsAutoAccept()
    {
        // Arrange
        double confidence = 0.80;

        // Act
        var result = _decision.MakeDecision(confidence);

        // Assert
        Assert.Equal(Models.VerificationDecision.AutoAccept, result.Decision);
        Assert.True(result.IsIdentical);
    }

    [Fact]
    public void MakeDecision_GoodConfidence_ReturnsAccept()
    {
        // Arrange
        double confidence = 0.70;

        // Act
        var result = _decision.MakeDecision(confidence);

        // Assert
        Assert.Equal(Models.VerificationDecision.Accept, result.Decision);
        Assert.True(result.IsIdentical);
        Assert.Equal(0.70, result.Confidence);
    }

    [Fact]
    public void MakeDecision_ExactAcceptThreshold_ReturnsAccept()
    {
        // Arrange
        double confidence = 0.60;

        // Act
        var result = _decision.MakeDecision(confidence);

        // Assert
        Assert.Equal(Models.VerificationDecision.Accept, result.Decision);
        Assert.True(result.IsIdentical);
    }

    [Fact]
    public void MakeDecision_UncertainConfidence_ReturnsRetry()
    {
        // Arrange
        double confidence = 0.55;

        // Act
        var result = _decision.MakeDecision(confidence);

        // Assert
        Assert.Equal(Models.VerificationDecision.Retry, result.Decision);
        Assert.False(result.IsIdentical);
        Assert.Equal(0.55, result.Confidence);
    }

    [Fact]
    public void MakeDecision_ExactRetryThreshold_ReturnsRetry()
    {
        // Arrange
        double confidence = 0.45;

        // Act
        var result = _decision.MakeDecision(confidence);

        // Assert
        Assert.Equal(Models.VerificationDecision.Retry, result.Decision);
        Assert.False(result.IsIdentical);
    }

    [Fact]
    public void MakeDecision_LowConfidence_ReturnsReject()
    {
        // Arrange
        double confidence = 0.30;

        // Act
        var result = _decision.MakeDecision(confidence);

        // Assert
        Assert.Equal(Models.VerificationDecision.Reject, result.Decision);
        Assert.False(result.IsIdentical);
        Assert.Equal(0.30, result.Confidence);
    }

    [Fact]
    public void MakeDecision_ZeroConfidence_ReturnsReject()
    {
        // Arrange
        double confidence = 0.0;

        // Act
        var result = _decision.MakeDecision(confidence);

        // Assert
        Assert.Equal(Models.VerificationDecision.Reject, result.Decision);
        Assert.False(result.IsIdentical);
    }

    [Fact]
    public void MakeDecision_PerfectConfidence_ReturnsAutoAccept()
    {
        // Arrange
        double confidence = 1.0;

        // Act
        var result = _decision.MakeDecision(confidence);

        // Assert
        Assert.Equal(Models.VerificationDecision.AutoAccept, result.Decision);
        Assert.True(result.IsIdentical);
    }

    [Theory]
    [InlineData(0.79, Models.VerificationDecision.Accept)]
    [InlineData(0.59, Models.VerificationDecision.Retry)]
    [InlineData(0.44, Models.VerificationDecision.Reject)]
    public void MakeDecision_BoundaryConditions_ReturnsCorrectDecision(
        double confidence, 
        Models.VerificationDecision expectedDecision)
    {
        // Act
        var result = _decision.MakeDecision(confidence);

        // Assert
        Assert.Equal(expectedDecision, result.Decision);
    }

    [Fact]
    public void GetThresholds_ReturnsCorrectValues()
    {
        // Act
        var (autoAccept, accept, retry) = _decision.GetThresholds();

        // Assert
        Assert.Equal(0.80, autoAccept);
        Assert.Equal(0.60, accept);
        Assert.Equal(0.45, retry);
    }

    [Fact]
    public void MakeDecision_AllResults_HaveMessages()
    {
        // Test that all decisions include user-friendly messages
        var decisions = new[] { 0.85, 0.70, 0.55, 0.30 };

        foreach (var confidence in decisions)
        {
            var result = _decision.MakeDecision(confidence);
            Assert.NotNull(result.Message);
            Assert.NotEmpty(result.Message);
        }
    }
}
