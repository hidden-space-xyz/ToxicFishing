using System.Diagnostics;

using ToxicFishing.Humanization.Abstractions;
using ToxicFishing.Shared.Configuration;

namespace ToxicFishing.Humanization.Services;

/// <summary>
/// Default <see cref="IHumanizer"/>. Draws delays from a log-normal distribution around the requested
/// median (spread by <see cref="AppOptions.Humanizer.Sigma"/>) and stretches them by a per-session
/// fatigue factor that grows the longer a run lasts, so timing never forms a robotic pattern.
/// </summary>
internal sealed class Humanizer : IHumanizer
{
    private readonly Random random = new();
    private readonly Stopwatch sessionClock = new();

    /// <inheritdoc />
    public void StartSession() => sessionClock.Restart();

    /// <inheritdoc />
    public int NextDelayMs(int medianMs, int minMs, int maxMs)
    {
        var sample = medianMs * Math.Exp(AppOptions.Humanizer.Sigma * NextGaussian()) * FatigueFactor();
        return (int)Math.Clamp(Math.Round(sample), minMs, maxMs);
    }

    /// <inheritdoc />
    public int NextReactionMs()
        => NextDelayMs(AppOptions.Humanizer.ReactionMedianMs, AppOptions.Humanizer.ReactionMinMs, AppOptions.Humanizer.ReactionMaxMs);

    private double FatigueFactor()
        => 1.0 + Math.Min(AppOptions.Humanizer.FatigueMax, sessionClock.Elapsed.TotalHours * AppOptions.Humanizer.FatiguePerHour);

    private double NextGaussian()
    {
        var u1 = 1.0 - random.NextDouble();
        var u2 = 1.0 - random.NextDouble();
        return Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Cos(2.0 * Math.PI * u2);
    }
}
