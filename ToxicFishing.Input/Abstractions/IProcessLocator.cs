using System.Diagnostics;

using ToxicFishing.Input.Models;

namespace ToxicFishing.Input.Abstractions;

/// <summary>
/// Locates the World of Warcraft process the bot drives. The user picks the target window from
/// <see cref="GetCandidates"/> and sets it with <see cref="Select"/>. The contract exposes
/// <see cref="Process"/>, a platform type, so it lives in the Input module alongside its Win32
/// implementation.
/// </summary>
public interface IProcessLocator
{
    /// <summary>
    /// Lists the running processes that own a visible main window, so the UI can offer them for
    /// selection.
    /// </summary>
    /// <returns>The selectable windowed processes, ordered by display name.</returns>
    public IReadOnlyList<GameProcess> GetCandidates();

    /// <summary>
    /// Chooses which process the bot should drive.
    /// </summary>
    /// <param name="processId">The id of the process to target, or <see langword="null"/> to clear the
    /// selection.</param>
    public void Select(int? processId);

    /// <summary>
    /// Gets the selected game process, when one is selected and still alive.
    /// </summary>
    /// <returns>The selected game <see cref="Process"/>, or <see langword="null"/> if none is selected
    /// or it has exited.</returns>
    public Process? GetWowProcess();
}
