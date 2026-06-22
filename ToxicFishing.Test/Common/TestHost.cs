using Microsoft.Extensions.DependencyInjection;

using ToxicFishing.Activity;
using ToxicFishing.Audio;
using ToxicFishing.Fishing;
using ToxicFishing.Humanization;
using ToxicFishing.Input;
using ToxicFishing.Vision;

namespace ToxicFishing.Test.Common;

/// <summary>
/// Builds a real dependency-injection container by composing every module exactly as the app's shell
/// does, so tests can verify that the object graph wires up and resolves across module boundaries.
/// </summary>
public static class TestHost
{
    /// <summary>
    /// Creates a provider with the same module registrations the app uses.
    /// </summary>
    /// <returns>A built <see cref="ServiceProvider"/>; the caller owns and should dispose it.</returns>
    public static ServiceProvider CreateProvider()
    {
        return new ServiceCollection()
            .AddActivityModule()
            .AddHumanizationModule()
            .AddAudioModule()
            .AddVisionModule()
            .AddInputModule()
            .AddFishingModule()
            .BuildServiceProvider();
    }
}
