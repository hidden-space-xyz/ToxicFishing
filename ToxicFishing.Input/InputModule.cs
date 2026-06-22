using Microsoft.Extensions.DependencyInjection;

using ToxicFishing.Input.Abstractions;
using ToxicFishing.Input.Services;

namespace ToxicFishing.Input;

/// <summary>
/// Registers the Input module. Both services are singletons so the cached game-process handle is
/// shared across the session.
/// </summary>
public static class InputModule
{
    /// <summary>
    /// Adds the Input module's services to the container.
    /// </summary>
    /// <param name="services">The service collection to add the registrations to.</param>
    /// <returns>The same <paramref name="services"/> instance, to allow call chaining.</returns>
    public static IServiceCollection AddInputModule(this IServiceCollection services)
    {
        services.AddSingleton<IProcessLocator, WowProcessLocator>();
        services.AddSingleton<IInput, WowInput>();

        return services;
    }
}
