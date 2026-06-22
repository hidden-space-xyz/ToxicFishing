using Microsoft.Extensions.DependencyInjection;

using ToxicFishing.Activity;
using ToxicFishing.Activity.Abstractions;
using ToxicFishing.Audio;
using ToxicFishing.Audio.Abstractions;
using ToxicFishing.Fishing;
using ToxicFishing.Fishing.Abstractions;
using ToxicFishing.Humanization;
using ToxicFishing.Humanization.Abstractions;
using ToxicFishing.Input;
using ToxicFishing.Input.Abstractions;
using ToxicFishing.Test.Common;
using ToxicFishing.Vision;
using ToxicFishing.Vision.Abstractions;

namespace ToxicFishing.Test.Unit.Modules;

public sealed class ModuleRegistrationTests
{
    [Test]
    public void EachModule_RegistersItsContracts()
    {
        var services = new ServiceCollection()
            .AddActivityModule()
            .AddHumanizationModule()
            .AddAudioModule()
            .AddVisionModule()
            .AddInputModule()
            .AddFishingModule();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(IsRegistered<IActivityReporter>(services), Is.True);
            Assert.That(IsRegistered<IHumanizer>(services), Is.True);
            Assert.That(IsRegistered<IVision>(services), Is.True);
            Assert.That(IsRegistered<IProcessLocator>(services), Is.True);
            Assert.That(IsRegistered<IInput>(services), Is.True);
            Assert.That(IsRegistered<IAudioBiteDetector>(services), Is.True);
            Assert.That(IsRegistered<IFishingSession>(services), Is.True);
            Assert.That(IsRegistered<IBotController>(services), Is.True);
        }
    }

    [Test]
    public void Registrations_AreSingletons()
    {
        var services = new ServiceCollection()
            .AddActivityModule()
            .AddHumanizationModule()
            .AddAudioModule()
            .AddVisionModule()
            .AddInputModule()
            .AddFishingModule();

        Assert.That(services, Has.All.Property(nameof(ServiceDescriptor.Lifetime)).EqualTo(ServiceLifetime.Singleton));
    }

    [Test]
    public void Provider_ResolvesAPlatformNeutralService()
    {
        using var provider = TestHost.CreateProvider();

        Assert.That(provider.GetService<IHumanizer>(), Is.Not.Null);
    }

    private static bool IsRegistered<T>(IServiceCollection services)
        => services.Any(descriptor => descriptor.ServiceType == typeof(T));
}
