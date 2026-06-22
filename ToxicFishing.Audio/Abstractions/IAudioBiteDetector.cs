namespace ToxicFishing.Audio.Abstractions;

/// <summary>
/// Detects a fish bite from the game's audio: the loud, brief splash sound World of Warcraft plays
/// the instant a fish bites is a far more reliable trigger than the bobber's on-screen motion. The
/// listener watches the peak loudness of the game's own audio session and reports the characteristic
/// spike above the ambient level.
/// </summary>
public interface IAudioBiteDetector
{
    /// <summary>
    /// Begins listening for the splash on the audio session(s) owned by the given process, resetting
    /// the ambient baseline and refractory state. Call once per cast, before polling.
    /// </summary>
    /// <param name="processId">The id of the game process whose audio session should be monitored, or
    /// <see langword="null"/> to fall back to the default playback device's overall output.</param>
    public void Begin(int? processId);

    /// <summary>
    /// Samples the monitored audio's current peak and reports whether a splash spike — and therefore a
    /// bite — has just occurred. Call once per watch-loop iteration after <see cref="Begin"/>.
    /// </summary>
    /// <returns><see langword="true"/> if a bite was detected on this sample; otherwise
    /// <see langword="false"/>.</returns>
    public bool PollBite();

    /// <summary>
    /// Stops listening and releases the audio device acquired by <see cref="Begin"/>.
    /// </summary>
    public void Stop();
}
