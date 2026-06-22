using ToxicFishing.Activity.Models;

namespace ToxicFishing.Activity.Abstractions;

/// <summary>
/// Sink for human-readable status messages raised by the session and adapters. Implementations
/// fan out to the UI; this keeps the other modules free of any UI dependency.
/// </summary>
public interface IActivityReporter
{
    /// <summary>
    /// Raised whenever a new <see cref="ActivityEntry"/> is reported.
    /// </summary>
    public event Action<ActivityEntry>? Reported;

    /// <summary>
    /// Reports an informational message.
    /// </summary>
    /// <param name="message">The message text.</param>
    public void Info(string message);

    /// <summary>
    /// Reports a warning message.
    /// </summary>
    /// <param name="message">The message text.</param>
    public void Warning(string message);

    /// <summary>
    /// Reports an error message.
    /// </summary>
    /// <param name="message">The message text.</param>
    public void Error(string message);
}
