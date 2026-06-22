namespace ToxicFishing.Input.Models;

/// <summary>
/// A lightweight, UI-friendly descriptor of a running process the bot can target, exposed so the UI can
/// list and select the game window without handling a <see cref="System.Diagnostics.Process"/> directly.
/// </summary>
/// <param name="Id">The operating-system process id.</param>
/// <param name="DisplayName">A human-readable label (process name and window title) shown in the UI.</param>
public readonly record struct GameProcess(int Id, string DisplayName);
