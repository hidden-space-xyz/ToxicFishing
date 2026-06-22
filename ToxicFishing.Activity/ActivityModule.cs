using Microsoft.Extensions.DependencyInjection;

using ToxicFishing.Activity.Abstractions;
using ToxicFishing.Activity.Services;

namespace ToxicFishing.Activity;

/// <summary>
/// Registers the Activity module. The reporter is a singleton so every module fans status messages
/// out through the same sink for the lifetime of the session.
/// </summary>
public static class ActivityModule
{
    /// <summary>
    /// Adds the Activity module's services to the container.
    /// </summary>
    /// <param name="services">The service collection to add the registrations to.</param>
    /// <returns>The same <paramref name="services"/> instance, to allow call chaining.</returns>
    public static IServiceCollection AddActivityModule(this IServiceCollection services)
    {
        services.AddSingleton<IActivityReporter, ActivityReporter>();

        return services;
    }
}
