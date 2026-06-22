namespace ToxicFishing.Fishing.Abstractions;

/// <summary>
/// Orchestrates the cast → watch → loot fishing loop, coordinating vision, bite detection, and input
/// until cancelled. Intended to run on a background thread owned by <see cref="IBotController"/>.
/// </summary>
public interface IFishingSession
{
    /// <summary>
    /// Runs the fishing loop until the token is cancelled or the configured session cap is hit.
    /// </summary>
    /// <param name="cancellationToken">Token used to stop the loop cooperatively.</param>
    public void Run(CancellationToken cancellationToken = default);
}
