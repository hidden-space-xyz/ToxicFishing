using ToxicFishing.Audio.Services;
using ToxicFishing.Shared.Configuration;

namespace ToxicFishing.Test.Unit.Audio;

public sealed class PeakSpikeAnalyzerTests
{
    private const double Quiet = 0.05;

    private static PeakSpikeAnalyzer CreateService()
    {
        var analyzer = new PeakSpikeAnalyzer();
        analyzer.Reset();
        return analyzer;
    }

    private static void FeedQuiet(PeakSpikeAnalyzer analyzer, int samples)
    {
        for (var i = 0; i < samples; i++)
        {
            analyzer.Observe(Quiet);
        }
    }

    [Test]
    public void Observe_SteadyQuietAudio_NeverSpikes()
    {
        var analyzer = CreateService();

        var spiked = false;
        for (var i = 0; i < 50; i++)
        {
            spiked |= analyzer.Observe(Quiet);
        }

        Assert.That(spiked, Is.False);
    }

    [Test]
    public void Observe_LoudTransientAfterWarmup_Spikes()
    {
        var analyzer = CreateService();
        FeedQuiet(analyzer, AppOptions.AudioDetection.SeedSamples + 4);

        var spiked = analyzer.Observe(0.95);

        Assert.That(spiked, Is.True);
    }

    [Test]
    public void Observe_RiseAboveBaselineButBelowAbsoluteFloor_NeverSpikes()
    {
        var analyzer = CreateService();

        // Seed with silence so the relative rise alone would qualify the next sample...
        for (var i = 0; i < AppOptions.AudioDetection.SeedSamples + 4; i++)
        {
            analyzer.Observe(0.0);
        }

        // ...but a sample above the rise yet below the absolute floor must not count as a bite.
        var aboveRiseBelowFloor = (AppOptions.AudioDetection.MinPeak + AppOptions.AudioDetection.MinRise) / 2.0;
        var spiked = analyzer.Observe(aboveRiseBelowFloor);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(aboveRiseBelowFloor, Is.GreaterThanOrEqualTo(AppOptions.AudioDetection.MinRise));
            Assert.That(aboveRiseBelowFloor, Is.LessThan(AppOptions.AudioDetection.MinPeak));
            Assert.That(spiked, Is.False);
        }
    }

    [Test]
    public void Observe_LoudTransientDuringWarmup_IsSuppressed()
    {
        var analyzer = CreateService();

        analyzer.Observe(Quiet);
        var spikedDuringWarmup = analyzer.Observe(0.95);

        Assert.That(spikedDuringWarmup, Is.False);
    }
}
