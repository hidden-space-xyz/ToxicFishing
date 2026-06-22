using System.Diagnostics;

using ToxicFishing.Activity.Abstractions;
using ToxicFishing.Input.Abstractions;
using ToxicFishing.Input.Models;

namespace ToxicFishing.Input.Services;

/// <summary>
/// Default <see cref="IProcessLocator"/>. Drives whichever window the user selected via
/// <see cref="Select"/>; the resolved handle is cached and revalidated before reuse. Reports through
/// <see cref="IActivityReporter"/> when no window is selected or the selected one has exited.
/// </summary>
/// <param name="activity">Sink used to report that no game window is available.</param>
internal sealed class WowProcessLocator(IActivityReporter activity) : IProcessLocator
{
    private Process? cachedProcess;
    private int? selectedProcessId;

    /// <inheritdoc />
    public IReadOnlyList<GameProcess> GetCandidates()
    {
        List<GameProcess> candidates = [];

        foreach (var process in Process.GetProcesses())
        {
            try
            {
                if (process.MainWindowHandle != IntPtr.Zero && !string.IsNullOrWhiteSpace(process.MainWindowTitle))
                {
                    candidates.Add(new GameProcess(process.Id, $"{process.ProcessName} — {process.MainWindowTitle}"));
                }
            }
            catch
            {
                // Some processes deny access to their window information; skip them.
            }
            finally
            {
                process.Dispose();
            }
        }

        candidates.Sort(static (a, b) => string.Compare(a.DisplayName, b.DisplayName, StringComparison.OrdinalIgnoreCase));
        return candidates;
    }

    /// <inheritdoc />
    public void Select(int? processId)
    {
        selectedProcessId = processId;
        cachedProcess?.Dispose();
        cachedProcess = null;
    }

    /// <inheritdoc />
    public Process? GetWowProcess()
    {
        if (IsAlive(cachedProcess))
        {
            return cachedProcess;
        }

        cachedProcess?.Dispose();
        cachedProcess = null;

        if (selectedProcessId is not { } processId)
        {
            activity.Error("No game window selected — pick the World of Warcraft window in the UI.");
            return null;
        }

        cachedProcess = TryGetProcessById(processId);
        if (cachedProcess is null)
        {
            activity.Error("The selected game window is no longer running — pick it again.");
        }

        return cachedProcess;
    }

    private static bool IsAlive(Process? process)
    {
        if (process is null)
        {
            return false;
        }

        try
        {
            return !process.HasExited;
        }
        catch
        {
            return false;
        }
    }

    private static Process? TryGetProcessById(int processId)
    {
        try
        {
            var process = Process.GetProcessById(processId);
            if (process.HasExited)
            {
                process.Dispose();
                return null;
            }

            return process;
        }
        catch (ArgumentException)
        {
            return null;
        }
    }
}
