using Microsoft.Extensions.DependencyInjection;

using ToxicFishing.Fishing.Abstractions;
using ToxicFishing.Fishing.Services;

namespace ToxicFishing.Fishing;

/// <summary>
/// Registers the Fishing module: the fishing session loop and the bot controller. Both are singletons
/// so session state (tracking history, cancellation) persists across the run.
/// </summary>
public static class FishingModule
{
    /// <summary>
    /// Adds the Fishing module's services to the container.
    /// </summary>
    /// <param name="services">The service collection to add the registrations to.</param>
    /// <returns>The same <paramref name="services"/> instance, to allow call chaining.</returns>
    public static IServiceCollection AddFishingModule(this IServiceCollection services)
    {
        services.AddSingleton<IFishingSession, FishingSession>();
        services.AddSingleton<IBotController, BotController>();

        return services;
    }
}
