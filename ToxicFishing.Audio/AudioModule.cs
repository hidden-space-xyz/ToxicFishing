using Microsoft.Extensions.DependencyInjection;

using ToxicFishing.Audio.Abstractions;
using ToxicFishing.Audio.Services;

namespace ToxicFishing.Audio;

/// <summary>
/// Registers the Audio module: the WASAPI splash listener used for sound-based bite detection. It is a
/// singleton so the device enumerator is created once and reused across casts.
/// </summary>
public static class AudioModule
{
    /// <summary>
    /// Adds the Audio module's services to the container.
    /// </summary>
    /// <param name="services">The service collection to add the registrations to.</param>
    /// <returns>The same <paramref name="services"/> instance, to allow call chaining.</returns>
    public static IServiceCollection AddAudioModule(this IServiceCollection services)
    {
        services.AddSingleton<IAudioBiteDetector, WasapiAudioBiteDetector>();

        return services;
    }
}
