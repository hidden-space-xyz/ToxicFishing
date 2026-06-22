namespace ToxicFishing.Activity.Models;

/// <summary>
/// A single timestamped message reported from the fishing session for display in the UI.
/// </summary>
/// <param name="Timestamp">The moment the entry was created.</param>
/// <param name="Level">The severity of the entry.</param>
/// <param name="Message">The human-readable message text.</param>
public readonly record struct ActivityEntry(DateTime Timestamp, ActivityLevel Level, string Message)
{
    /// <summary>
    /// Gets the entry formatted for the activity log as <c>HH:mm:ss  message</c>.
    /// </summary>
    public string Display => $"{Timestamp:HH:mm:ss}  {Message}";
}
