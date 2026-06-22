namespace ToxicFishing.Activity.Models;

/// <summary>
/// The severity of an <see cref="ActivityEntry"/> surfaced to the user.
/// </summary>
public enum ActivityLevel
{
    /// <summary>
    /// Routine progress information (casts, catches, lure application).
    /// </summary>
    Info = 0,

    /// <summary>
    /// A recoverable condition the user may want to act on (e.g. the bobber was lost).
    /// </summary>
    Warning = 1,

    /// <summary>
    /// A failure that interrupted the action being attempted.
    /// </summary>
    Error = 2,
}
