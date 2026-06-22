using Microsoft.Extensions.DependencyInjection;

using ToxicFishing.Humanization.Abstractions;
using ToxicFishing.Humanization.Services;

namespace ToxicFishing.Humanization;

/// <summary>
/// Registers the Humanization module. The humanizer is a singleton so its per-session fatigue clock
/// persists across the run.
/// </summary>
public static class HumanizationModule
{
    /// <summary>
    /// Adds the Humanization module's services to the container.
    /// </summary>
    /// <param name="services">The service collection to add the registrations to.</param>
    /// <returns>The same <paramref name="services"/> instance, to allow call chaining.</returns>
    public static IServiceCollection AddHumanizationModule(this IServiceCollection services)
    {
        services.AddSingleton<IHumanizer, Humanizer>();

        return services;
    }
}
