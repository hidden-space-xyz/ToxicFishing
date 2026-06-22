using ToxicFishing.Activity.Abstractions;
using ToxicFishing.Activity.Models;

namespace ToxicFishing.Activity.Services;

/// <summary>
/// Default <see cref="IActivityReporter"/> that raises reported entries to subscribers, collapsing
/// immediately repeated messages so the activity log is not flooded with duplicates.
/// </summary>
internal sealed class ActivityReporter : IActivityReporter
{
    private readonly Lock gate = new();
    private string? lastMessage;

    /// <inheritdoc />
    public event Action<ActivityEntry>? Reported;

    /// <inheritdoc />
    public void Info(string message) => Emit(ActivityLevel.Info, message);

    /// <inheritdoc />
    public void Warning(string message) => Emit(ActivityLevel.Warning, message);

    /// <inheritdoc />
    public void Error(string message) => Emit(ActivityLevel.Error, message);

    private void Emit(ActivityLevel level, string message)
    {
        lock (gate)
        {
            if (message == lastMessage)
            {
                return;
            }

            lastMessage = message;
        }

        Reported?.Invoke(new ActivityEntry(DateTime.Now, level, message));
    }
}
