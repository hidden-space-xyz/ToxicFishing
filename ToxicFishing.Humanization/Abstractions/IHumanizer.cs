namespace ToxicFishing.Humanization.Abstractions;

/// <summary>
/// Produces randomised, human-like delays for every simulated action. Combines log-normal jitter
/// around a median with a per-session fatigue model so timing is never a perfectly repeatable pattern.
/// </summary>
public interface IHumanizer
{
    /// <summary>
    /// Resets the fatigue clock; call once at the start of each fishing session.
    /// </summary>
    public void StartSession();

    /// <summary>Samples a randomised delay around <paramref name="medianMs"/>, scaled by the current
    /// session fatigue and clamped to the supplied bounds.</summary>
    /// <param name="medianMs">The central tendency of the delay, in milliseconds.</param>
    /// <param name="minMs">The inclusive lower bound, in milliseconds.</param>
    /// <param name="maxMs">The inclusive upper bound, in milliseconds.</param>
    /// <returns>A delay in milliseconds within <c>[<paramref name="minMs"/>, <paramref name="maxMs"/>]</c>.</returns>
    public int NextDelayMs(int medianMs, int minMs, int maxMs);

    /// <summary>
    /// Samples a humanized reaction time using the configured reaction-delay constants.
    /// </summary>
    /// <returns>A reaction delay in milliseconds within the configured reaction bounds.</returns>
    public int NextReactionMs();
}
