using Microsoft.Win32;

using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Threading;

using ToxicFishing.Activity.Abstractions;
using ToxicFishing.Activity.Models;
using ToxicFishing.App.Mvvm;
using ToxicFishing.Fishing.Abstractions;
using ToxicFishing.Input.Abstractions;
using ToxicFishing.Input.Models;

namespace ToxicFishing.App.ViewModels;

/// <summary>
/// View model for the main window. Exposes the start/stop/schedule/bobber commands and the live
/// status, elapsed/remaining clocks, and activity log, marshalling controller and reporter callbacks
/// onto the UI thread via the <see cref="Dispatcher"/>.
/// </summary>
public sealed class MainViewModel : INotifyPropertyChanged
{
    private const int MaxEntries = 500;

    private static readonly string BobberWorkingPath =
        Path.Combine(AppContext.BaseDirectory, "Resources", "bobber.png");

    private readonly IBotController controller;
    private readonly IProcessLocator locator;
    private readonly Dispatcher dispatcher;
    private readonly DispatcherTimer clockTimer;
    private readonly Stopwatch sessionClock = new();

    private TimeSpan? scheduledDuration;

    /// <summary>Initializes the view model, wiring commands and subscribing to controller and activity
    /// events.</summary>
    /// <param name="controller">The bot controller driven by the start/stop/schedule commands.</param>
    /// <param name="locator">The process locator used to list and select the target game window.</param>
    /// <param name="activity">The activity source whose entries populate the log.</param>
    public MainViewModel(IBotController controller, IProcessLocator locator, IActivityReporter activity)
    {
        this.controller = controller;
        this.locator = locator;
        dispatcher = Application.Current.Dispatcher;

        StartCommand = new RelayCommand(Start, () => !IsRunning);
        StopCommand = new RelayCommand(Stop, () => IsRunning);
        ChangeBobberCommand = new RelayCommand(ChangeBobber);
        ClearCommand = new RelayCommand(ActivityEntries.Clear);
        RefreshProcessesCommand = new RelayCommand(RefreshProcesses);

        this.controller.RunningChanged += OnRunningChanged;
        activity.Reported += OnActivity;

        clockTimer = new DispatcherTimer(DispatcherPriority.Normal, dispatcher)
        {
            Interval = TimeSpan.FromSeconds(1),
        };
        clockTimer.Tick += (_, _) => UpdateClock();

        BobberFileName = File.Exists(BobberWorkingPath) ? "bobber.png (default)" : "(none)";

        RefreshProcesses();
    }

    /// <summary>
    /// Gets the bounded, newest-last log of reported activity entries shown in the UI.
    /// </summary>
    public ObservableCollection<ActivityEntry> ActivityEntries { get; } = [];

    /// <summary>
    /// Gets the command that starts a fishing session (enabled only while stopped).
    /// </summary>
    public RelayCommand StartCommand { get; }

    /// <summary>
    /// Gets the command that stops the running session (enabled only while running).
    /// </summary>
    public RelayCommand StopCommand { get; }

    /// <summary>
    /// Gets the command that prompts for and swaps the bobber template image.
    /// </summary>
    public RelayCommand ChangeBobberCommand { get; }

    /// <summary>
    /// Gets the command that clears the activity log.
    /// </summary>
    public RelayCommand ClearCommand { get; }

    /// <summary>
    /// Gets the command that refreshes the list of selectable game windows.
    /// </summary>
    public RelayCommand RefreshProcessesCommand { get; }

    /// <summary>
    /// Gets the running windows the user can target, refreshed on demand.
    /// </summary>
    public ObservableCollection<GameProcess> Processes { get; } = [];

    /// <summary>
    /// Gets or sets the game window the bot should drive. Selecting one points the locator at it;
    /// clearing it reverts to auto-detecting the client by process name.
    /// </summary>
    public GameProcess? SelectedProcess
    {
        get;
        set
        {
            if (SetProperty(ref field, value))
            {
                locator.Select(value?.Id);
            }
        }
    }

    /// <summary>
    /// Gets a value indicating whether a fishing session is currently running.
    /// </summary>
    public bool IsRunning
    {
        get;
        private set
        {
            if (!SetProperty(ref field, value))
            {
                return;
            }

            OnPropertyChanged(nameof(IsStopped));
            StartCommand.RaiseCanExecuteChanged();
            StopCommand.RaiseCanExecuteChanged();
        }
    }

    /// <summary>
    /// Gets a value indicating whether the session is stopped; the inverse of <see cref="IsRunning"/>.
    /// </summary>
    public bool IsStopped => !IsRunning;

    /// <summary>
    /// Gets the human-readable status shown in the UI (e.g. "Fishing", "Stopped").
    /// </summary>
    public string StatusText
    {
        get;
        private set => SetProperty(ref field, value);
    } = "Stopped";

    /// <summary>Gets or sets a value indicating whether the session should auto-stop after
    /// <see cref="DurationMinutes"/>.</summary>
    public bool ScheduleEnabled
    {
        get;
        set => SetProperty(ref field, value);
    }

    /// <summary>
    /// Gets or sets the scheduled run length in minutes, clamped to <c>[1, 1440]</c>.
    /// </summary>
    public int DurationMinutes
    {
        get;
        set => SetProperty(ref field, Math.Clamp(value, 1, 24 * 60));
    } = 30;

    /// <summary>
    /// Gets the display name of the currently active bobber template.
    /// </summary>
    public string BobberFileName
    {
        get;
        private set => SetProperty(ref field, value);
    } = "(none)";

    /// <summary>
    /// Gets the elapsed session time formatted as <c>hh:mm:ss</c>.
    /// </summary>
    public string ElapsedText
    {
        get;
        private set => SetProperty(ref field, value);
    } = "00:00:00";

    /// <summary>Gets the countdown to a scheduled stop (e.g. "Stops in 00:12:30"), or empty when no
    /// schedule is active.</summary>
    public string RemainingText
    {
        get;
        private set => SetProperty(ref field, value);
    } = string.Empty;

    private void Start()
    {
        if (IsRunning)
        {
            return;
        }

        TimeSpan? duration = ScheduleEnabled && DurationMinutes > 0
            ? TimeSpan.FromMinutes(DurationMinutes)
            : null;

        sessionClock.Restart();
        scheduledDuration = duration;

        controller.Start(duration);
    }

    private void Stop() => controller.Stop();

    private void RefreshProcesses()
    {
        var selectedId = SelectedProcess?.Id;

        Processes.Clear();
        foreach (var process in locator.GetCandidates())
        {
            Processes.Add(process);
        }

        SelectedProcess = FindById(selectedId);
    }

    private GameProcess? FindById(int? processId)
    {
        if (processId is not { } id)
        {
            return null;
        }

        foreach (var process in Processes)
        {
            if (process.Id == id)
            {
                return process;
            }
        }

        return null;
    }

    private void ChangeBobber()
    {
        OpenFileDialog dialog = new()
        {
            Title = "Select the bobber image",
            Filter = "PNG images (*.png)|*.png|All images|*.png;*.jpg;*.jpeg;*.bmp",
            CheckFileExists = true,
        };

        if (dialog.ShowDialog() != true)
        {
            return;
        }

        try
        {
            if (!string.Equals(Path.GetFullPath(dialog.FileName), Path.GetFullPath(BobberWorkingPath),
                    StringComparison.OrdinalIgnoreCase))
            {
                File.Copy(dialog.FileName, BobberWorkingPath, overwrite: true);
            }

            controller.SetBobberTemplate(BobberWorkingPath);
            BobberFileName = Path.GetFileName(dialog.FileName);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Couldn't change the bobber:\n{ex.Message}",
                "ToxicFishing", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void OnRunningChanged(object? sender, bool running)
    {
        dispatcher.Invoke(() =>
        {
            IsRunning = running;
            if (running)
            {
                StatusText = "Fishing";
                UpdateClock();
                clockTimer.Start();
            }
            else
            {
                clockTimer.Stop();
                StatusText = scheduledDuration is { } limit && sessionClock.Elapsed >= limit
                    ? "Stopped (time elapsed)"
                    : "Stopped";
                RemainingText = string.Empty;
            }
        });
    }

    private void OnActivity(ActivityEntry entry)
    {
        dispatcher.BeginInvoke(() =>
        {
            ActivityEntries.Add(entry);
            while (ActivityEntries.Count > MaxEntries)
            {
                ActivityEntries.RemoveAt(0);
            }
        });
    }

    private void UpdateClock()
    {
        ElapsedText = sessionClock.Elapsed.ToString(@"hh\:mm\:ss");

        if (scheduledDuration is { } limit)
        {
            var remaining = limit - sessionClock.Elapsed;
            if (remaining < TimeSpan.Zero)
            {
                remaining = TimeSpan.Zero;
            }

            RemainingText = $"Stops in {remaining:hh\\:mm\\:ss}";
        }
        else
        {
            RemainingText = string.Empty;
        }
    }

    /// <inheritdoc />
    public event PropertyChangedEventHandler? PropertyChanged;

    private bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? name = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return false;
        }

        field = value;
        OnPropertyChanged(name);
        return true;
    }

    private void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
