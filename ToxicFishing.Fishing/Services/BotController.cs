using ToxicFishing.Activity.Abstractions;
using ToxicFishing.Fishing.Abstractions;
using ToxicFishing.Input.Abstractions;
using ToxicFishing.Shared.Configuration;
using ToxicFishing.Vision.Abstractions;

namespace ToxicFishing.Fishing.Services;

/// <summary>
/// Default <see cref="IBotController"/> and the single Start/Stop authority. Launches the fishing
/// session on a background <see cref="Task"/>, guards its <see cref="CancellationTokenSource"/> with a
/// lock for thread-safety, and swallows cancellation cleanly so the UI thread is never blocked.
/// </summary>
/// <param name="session">The fishing loop to run.</param>
/// <param name="input">Game input used to send the initial wake key.</param>
/// <param name="vision">Vision layer used to swap the bobber template.</param>
/// <param name="activity">Sink for status messages.</param>
internal sealed class BotController(
    IFishingSession session,
    IInput input,
    IVision vision,
    IActivityReporter activity) : IBotController
{
    private readonly Lock gate = new();
    private CancellationTokenSource? cancellation;
    private Task? runTask;

    /// <inheritdoc />
    public bool IsRunning => runTask is { IsCompleted: false };

    /// <inheritdoc />
    public event EventHandler<bool>? RunningChanged;

    /// <inheritdoc />
    public void Start(TimeSpan? maxDuration = null)
    {
        lock (gate)
        {
            if (IsRunning)
            {
                return;
            }

            CancellationTokenSource cancellation = new();
            if (maxDuration is { } duration && duration > TimeSpan.Zero)
            {
                activity.Info($"Scheduled to stop in {duration.TotalMinutes:0.#} minute(s).");
                cancellation.CancelAfter(duration);
            }

            this.cancellation = cancellation;
            var token = cancellation.Token;

            runTask = Task.Run(() => RunLoop(token), token);
        }
    }

    private void RunLoop(CancellationToken cancellationToken)
    {
        RunningChanged?.Invoke(this, true);
        try
        {
            activity.Info("Bot started.");

            input.PressKey(AppOptions.Bot.WakeKey);
            Thread.Sleep(AppOptions.Bot.PreStartDelayMs);

            session.Run(cancellationToken);
        }
        catch (OperationCanceledException)
        {
            // Ignore
        }
        catch (Exception ex)
        {
            activity.Error($"The fishing session ended unexpectedly: {ex.Message}");
        }
        finally
        {
            activity.Info("Bot stopped.");
            RunningChanged?.Invoke(this, false);
        }
    }

    /// <inheritdoc />
    public void Stop()
    {
        lock (gate)
        {
            cancellation?.Cancel();
        }
    }

    /// <inheritdoc />
    public void SetBobberTemplate(string templatePath) => vision.LoadTemplate(templatePath);
}
