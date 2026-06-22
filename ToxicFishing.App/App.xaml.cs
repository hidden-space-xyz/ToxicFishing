using Microsoft.Extensions.DependencyInjection;

using System.Windows;

using ToxicFishing.Activity;
using ToxicFishing.App.ViewModels;
using ToxicFishing.Audio;
using ToxicFishing.Fishing;
using ToxicFishing.Fishing.Abstractions;
using ToxicFishing.Humanization;
using ToxicFishing.Input;
using ToxicFishing.Vision;

namespace ToxicFishing.App;

/// <summary>
/// WPF application entry point and the dependency-injection composition root. Registers every service
/// as a singleton on startup, resolves and shows the main window, and stops the bot and disposes the
/// provider on exit.
/// </summary>
public partial class App : System.Windows.Application
{
    private ServiceProvider? serviceProvider;

    /// <summary>
    /// Builds the service container, then resolves and shows the main window.
    /// </summary>
    /// <param name="e">The startup event arguments.</param>
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        ServiceCollection services = [];

        services
            .AddActivityModule()
            .AddHumanizationModule()
            .AddAudioModule()
            .AddVisionModule()
            .AddInputModule()
            .AddFishingModule();

        services.AddSingleton<MainViewModel>();
        services.AddSingleton<MainWindow>();

        serviceProvider = services.BuildServiceProvider();

        var window = serviceProvider.GetRequiredService<MainWindow>();
        window.DataContext = serviceProvider.GetRequiredService<MainViewModel>();
        window.Show();
    }

    /// <summary>
    /// Stops any running session and disposes the service provider so the app exits cleanly.
    /// </summary>
    /// <param name="e">The exit event arguments.</param>
    protected override void OnExit(ExitEventArgs e)
    {
        serviceProvider?.GetService<IBotController>()?.Stop();
        serviceProvider?.Dispose();
        base.OnExit(e);
    }
}
