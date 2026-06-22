namespace ToxicFishing.Fishing.Abstractions;

/// <summary>
/// The single Start/Stop authority for the fishing bot. Runs the session on a background task and
/// owns its cancellation, so the UI thread is never blocked and a run always has a stop path.
/// </summary>
public interface IBotController
{
    /// <summary>
    /// Gets a value indicating whether a fishing session is currently running.
    /// </summary>
    public bool IsRunning { get; }

    /// <summary>Raised when the running state changes, carrying the new value
    /// (<see langword="true"/> when started, <see langword="false"/> when stopped).</summary>
    public event EventHandler<bool>? RunningChanged;

    /// <summary>
    /// Starts a fishing session if one is not already running.
    /// </summary>
    /// <param name="maxDuration">Optional auto-stop duration; when supplied and positive, the session
    /// is cancelled after this long. <see langword="null"/> runs until stopped manually.</param>
    public void Start(TimeSpan? maxDuration = null);

    /// <summary>
    /// Requests cancellation of the running session; safe to call when nothing is running.
    /// </summary>
    public void Stop();

    /// <summary>
    /// Swaps the bobber template image used by the vision layer.
    /// </summary>
    /// <param name="templatePath">Path to the replacement template image.</param>
    public void SetBobberTemplate(string templatePath);
}
