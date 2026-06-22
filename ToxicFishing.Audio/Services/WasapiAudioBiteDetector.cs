using System.Diagnostics;

using NAudio.CoreAudioApi;

using ToxicFishing.Activity.Abstractions;
using ToxicFishing.Audio.Abstractions;
using ToxicFishing.Shared.Configuration;

namespace ToxicFishing.Audio.Services;

/// <summary>
/// Default <see cref="IAudioBiteDetector"/>, backed by WASAPI through NAudio. It reads the peak-loudness
/// meter of the game's own audio session (the sessions whose process id matches, falling back to the
/// default render device's overall meter) and feeds it to a <see cref="PeakSpikeAnalyzer"/> that flags
/// the splash. It reads only metering the user can already hear, so it stays least-invasive, and it
/// owns the <see cref="MMDevice"/> it opens, hence <see cref="IDisposable"/>.
/// </summary>
/// <param name="activity">Sink used to report what the listener is bound to.</param>
internal sealed class WasapiAudioBiteDetector(IActivityReporter activity) : IAudioBiteDetector, IDisposable
{
    private readonly MMDeviceEnumerator enumerator = new();
    private readonly PeakSpikeAnalyzer analyzer = new();
    private readonly List<AudioSessionControl> sessions = [];

    private MMDevice? device;
    private long lastBiteTimestamp;

    /// <inheritdoc />
    public void Begin(int? processId)
    {
        Stop();
        analyzer.Reset();
        lastBiteTimestamp = 0;

        try
        {
            device = enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);
            ResolveSessions(device, processId);
        }
        catch (Exception ex)
        {
            activity.Warning($"Couldn't open audio for bite detection: {ex.Message}");
            Stop();
        }
    }

    /// <inheritdoc />
    public bool PollBite()
    {
        var current = device;
        if (current is null)
        {
            return false;
        }

        if (Stopwatch.GetElapsedTime(lastBiteTimestamp).TotalMilliseconds < AppOptions.AudioDetection.RefractoryMs)
        {
            return false;
        }

        double peak;
        try
        {
            peak = ReadPeak(current);
        }
        catch
        {
            // A transient COM failure (e.g. the device was reset) shouldn't crash the watch loop.
            return false;
        }

        if (!analyzer.Observe(peak))
        {
            return false;
        }

        lastBiteTimestamp = Stopwatch.GetTimestamp();
        return true;
    }

    /// <inheritdoc />
    public void Stop()
    {
        sessions.Clear();
        device?.Dispose();
        device = null;
    }

    /// <summary>
    /// Disposes the meter device and the underlying device enumerator.
    /// </summary>
    public void Dispose()
    {
        Stop();
        enumerator.Dispose();
    }

    private void ResolveSessions(MMDevice current, int? processId)
    {
        if (processId is not { } pid)
        {
            activity.Info("Listening for the bite on the default audio output.");
            return;
        }

        var manager = current.AudioSessionManager;
        manager.RefreshSessions();
        var all = manager.Sessions;

        for (var i = 0; i < all.Count; i++)
        {
            var session = all[i];
            try
            {
                if (session.GetProcessID == (uint)pid)
                {
                    sessions.Add(session);
                }
            }
            catch
            {
                // Some sessions (e.g. the system-sounds session) deny process queries; skip them.
            }
        }

        if (sessions.Count == 0)
        {
            activity.Warning("No audio session found for the game — listening to the default output instead.");
        }
        else
        {
            activity.Info("Listening for the bite on the game's audio.");
        }
    }

    private double ReadPeak(MMDevice current)
    {
        if (sessions.Count == 0)
        {
            return current.AudioMeterInformation.MasterPeakValue;
        }

        var peak = 0.0;
        foreach (var session in sessions)
        {
            peak = Math.Max(peak, session.AudioMeterInformation.MasterPeakValue);
        }

        return peak;
    }
}
