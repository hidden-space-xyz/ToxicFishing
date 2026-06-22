using ToxicFishing.Shared.Primitives;

namespace ToxicFishing.Vision.Abstractions;

/// <summary>
/// Abstraction over the screen-reading vision pipeline. It locates the fishing bobber and exposes
/// only platform-neutral results (<see cref="PixelPoint"/>), keeping OpenCV behind this module.
/// </summary>
public interface IVision
{
    /// <summary>
    /// Prepares the detector for a new session and reports that the game and bobber template are ready.
    /// </summary>
    public void Configure();

    /// <summary>Clears the tracking state so the next scan searches the full screen instead of biasing
    /// toward the previous detection.</summary>
    public void ResetTracking();

    /// <summary>Scans the captured frame for the bobber, biasing toward the last known location so a
    /// freshly-cast float is preferred over background noise.</summary>
    /// <param name="screenPoint">The detected bobber position in screen pixels, or
    /// <see cref="PixelPoint.Empty"/> if none was found.</param>
    /// <returns><see langword="true"/> if the scan completed (whether or not a bobber was located);
    /// <see langword="false"/> if the capture/scan itself failed.</returns>
    public bool TryFindBobber(out PixelPoint screenPoint);

    /// <summary>
    /// Requests a live swap of the bobber template image used for template matching.
    /// </summary>
    /// <param name="templatePath">Path to the replacement template image; applied on the next scan.</param>
    public void LoadTemplate(string templatePath);
}
