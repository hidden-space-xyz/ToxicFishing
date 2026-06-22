using ToxicFishing.Shared.Primitives;

namespace ToxicFishing.Input.Abstractions;

/// <summary>
/// Abstraction over the least-invasive input channel to the game: standard Win32 window messages and
/// mouse events only. No DLL injection, memory access, or hooks are exposed or permitted behind it.
/// </summary>
public interface IInput
{
    /// <summary>
    /// Sends a humanized key press (down, hold, up) to the game window.
    /// </summary>
    /// <param name="key">The key to press; its <see cref="ConsoleKey"/> value doubles as the Windows
    /// virtual-key code posted to the window.</param>
    public void PressKey(ConsoleKey key);

    /// <summary>Moves the cursor to the given position and performs a humanized right-click, restoring
    /// the previously focused window afterwards so the bot stays unobtrusive.</summary>
    /// <param name="position">The screen position to right-click.</param>
    public void RightClickMouse(PixelPoint position);
}
