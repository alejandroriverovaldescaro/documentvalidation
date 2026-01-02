using DocumentValidation.FaceMatching;
using DocumentValidation.FaceMatching.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using VerificationDecisionEnum = DocumentValidation.FaceMatching.Models.VerificationDecision;

Console.WriteLine("=== Face Matching Example ===\n");

// Setup dependency injection
var services = new ServiceCollection();

// Add face matching services
services.AddFaceMatching(options =>
{
    // Choose verification method:
    // - VerificationMethod.Simulated (default): For testing without API credentials
    // - VerificationMethod.AzureFaceAPI: For production with real face recognition
    
    options.VerificationMethod = VerificationMethod.Simulated;
    
    // If using AzureFaceAPI, configure credentials:
    // options.VerificationMethod = VerificationMethod.AzureFaceAPI;
    // options.FaceApiEndpoint = Environment.GetEnvironmentVariable("FACE_API_ENDPOINT");
    // options.FaceApiKey = Environment.GetEnvironmentVariable("FACE_API_KEY");
    
    options.BurstFrameCount = 10;
    options.FrameDelayMs = 100;
});

// Add console logging
services.AddLogging(builder =>
{
    builder.AddConsole();
    builder.SetMinimumLevel(LogLevel.Information);
});

var serviceProvider = services.BuildServiceProvider();
var faceMatchingService = serviceProvider.GetRequiredService<FaceMatchingService>();

Console.WriteLine("Generating sample images for demonstration...\n");

// Create sample images for demonstration
var selfieFrames = GenerateSampleFrames(10);
var idPhoto = GenerateSampleIdPhoto();

Console.WriteLine("Running verification pipeline...\n");

// Run verification
var result = await faceMatchingService.VerifyIdentityAsync(selfieFrames, idPhoto);

// Display results
Console.WriteLine("\n=== Verification Result ===");
Console.WriteLine($"Decision: {result.Decision}");
Console.WriteLine($"Confidence: {result.Confidence:P1}");
Console.WriteLine($"Is Match: {result.IsIdentical}");
Console.WriteLine($"Message: {result.Message}");

// Show what to do based on decision
Console.WriteLine("\n=== Action Required ===");
switch (result.Decision)
{
    case VerificationDecisionEnum.AutoAccept:
        Console.WriteLine("✓ APPROVED - High confidence match");
        Console.WriteLine("  Action: Process immediately");
        break;
        
    case VerificationDecisionEnum.Accept:
        Console.WriteLine("✓ APPROVED - Good confidence match");
        Console.WriteLine("  Action: May queue for soft review");
        break;
        
    case VerificationDecisionEnum.Retry:
        Console.WriteLine("⚠ RETRY NEEDED - Uncertain match");
        Console.WriteLine("  Action: Ask user to try again");
        Console.WriteLine("  Hint: 'Please try again with better lighting'");
        break;
        
    case VerificationDecisionEnum.Reject:
        Console.WriteLine("✗ REJECTED - Low confidence match");
        Console.WriteLine("  Action: Flag for manual review or reject");
        break;
}

Console.WriteLine("\n=== Example Complete ===");

// Helper methods to generate sample images
List<byte[]> GenerateSampleFrames(int count)
{
    var frames = new List<byte[]>();
    
    for (int i = 0; i < count; i++)
    {
        // Create a simple colored image as a sample frame
        using var image = new Image<Rgba32>(640, 480);
        
        // Fill with varying colors to simulate different frames
        var color = new Rgba32((byte)(100 + i * 10), (byte)(150 - i * 5), 200);
        image.Mutate(ctx => ctx.BackgroundColor(color));
        
        using var ms = new MemoryStream();
        image.SaveAsJpeg(ms);
        frames.Add(ms.ToArray());
    }
    
    return frames;
}

byte[] GenerateSampleIdPhoto()
{
    // Create a simple colored image as sample ID photo
    using var image = new Image<Rgba32>(400, 500);
    image.Mutate(ctx => ctx.BackgroundColor(new Rgba32(120, 140, 200)));
    
    using var ms = new MemoryStream();
    image.SaveAsJpeg(ms);
    return ms.ToArray();
}

