using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace DocumentValidation.FaceMatching;

/// <summary>
/// Extension methods for registering face matching services
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds face matching services to the dependency injection container
    /// </summary>
    public static IServiceCollection AddFaceMatching(
        this IServiceCollection services,
        Action<FaceMatchingOptions>? configure = null)
    {
        var options = new FaceMatchingOptions();
        configure?.Invoke(options);

        // Register services
        services.AddSingleton<FaceNormalize>();
        services.AddSingleton<FaceCapture>();
        services.AddSingleton<VerificationDecision>();
        
        services.AddSingleton<FaceVerify>(sp =>
        {
            var logger = sp.GetRequiredService<ILogger<FaceVerify>>();
            return new FaceVerify(logger, options.FaceApiEndpoint, options.FaceApiKey);
        });

        services.AddSingleton<FaceMatchingService>();

        return services;
    }
}
