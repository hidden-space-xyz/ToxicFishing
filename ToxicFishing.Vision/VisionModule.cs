using Microsoft.Extensions.DependencyInjection;

using ToxicFishing.Vision.Abstractions;
using ToxicFishing.Vision.Services;

namespace ToxicFishing.Vision;

/// <summary>
/// Registers the Vision module. The OpenCV adapter is a singleton so the long-lived template pyramid
/// and morphology kernel are built once and shared (and disposed with the container).
/// </summary>
public static class VisionModule
{
    /// <summary>
    /// Adds the Vision module's services to the container.
    /// </summary>
    /// <param name="services">The service collection to add the registrations to.</param>
    /// <returns>The same <paramref name="services"/> instance, to allow call chaining.</returns>
    public static IServiceCollection AddVisionModule(this IServiceCollection services)
    {
        services.AddSingleton<IVision, OpenCvVision>();

        return services;
    }
}
