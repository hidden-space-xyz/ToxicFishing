using ToxicFishing.Shared.Configuration;

namespace ToxicFishing.Audio.Services;

/// <summary>
/// Deterministic core of the audio bite detector. Given a stream of normalised peak-loudness samples
/// (each in <c>[0, 1]</c>), it tracks an adaptive "ambient" baseline and reports the sharp upward
/// spike that marks the splash. The ambient baseline follows an exponential moving average updated
/// only while <em>not</em> spiking, so the transient itself never inflates the level it is measured
/// against. The first few samples are treated as warm-up to seed the baseline before arming.
/// </summary>
internal sealed class PeakSpikeAnalyzer
{
    private double baseline;
    private int seenSamples;

    /// <summary>
    /// Clears the baseline and warm-up state so the analyzer can begin a fresh listen.
    /// </summary>
    public void Reset()
    {
        baseline = 0.0;
        seenSamples = 0;
    }

    /// <summary>
    /// Feeds the latest peak sample and reports whether it constitutes a splash spike.
    /// </summary>
    /// <param name="peak">The current peak loudness, normalised to <c>[0, 1]</c>.</param>
    /// <returns><see langword="true"/> when the sample spikes far enough above the ambient baseline to
    /// be a bite; otherwise <see langword="false"/>.</returns>
    public bool Observe(double peak)
    {
        peak = Math.Clamp(peak, 0.0, 1.0);
        seenSamples++;

        if (seenSamples == 1)
        {
            baseline = peak;
            return false;
        }

        var isSpike = peak >= AppOptions.AudioDetection.MinPeak
            && peak >= baseline + AppOptions.AudioDetection.MinRise;

        if (!isSpike)
        {
            baseline += AppOptions.AudioDetection.BaselineAlpha * (peak - baseline);
        }

        return isSpike && seenSamples > AppOptions.AudioDetection.SeedSamples;
    }
}
