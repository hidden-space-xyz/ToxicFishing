using System.Diagnostics;
using System.Runtime.InteropServices;

using ToxicFishing.Humanization.Abstractions;
using ToxicFishing.Input.Abstractions;
using ToxicFishing.Shared.Configuration;
using ToxicFishing.Shared.Primitives;

namespace ToxicFishing.Input.Services;

/// <summary>
/// Default <see cref="IInput"/>. Talks to the game using only standard Win32 input — posts
/// <c>WM_KEYDOWN</c>/<c>WM_KEYUP</c> to the game window and fires <c>mouse_event</c> right-clicks —
/// with humanized hold times and a smoothed cursor move, then restores the prior foreground window.
/// No injection, memory access, or hooks are used.
/// </summary>
/// <param name="locator">Locator for the target game process.</param>
/// <param name="humanizer">Source of humanized key-hold, settle, and click-hold delays.</param>
internal sealed class WowInput(IProcessLocator locator, IHumanizer humanizer) : IInput
{
    private const uint WM_KEYDOWN = 0x0100;
    private const uint WM_KEYUP = 0x0101;
    private const uint WM_RBUTTONDOWN = 0x0204;
    private const uint WM_RBUTTONUP = 0x0205;
    private const int VK_RMB = 0x02;

    /// <inheritdoc />
    public void PressKey(ConsoleKey key)
    {
        var wowProcess = locator.GetWowProcess();
        if (wowProcess is null)
        {
            return;
        }

        PostMessage(wowProcess.MainWindowHandle, WM_KEYDOWN, (int)key, 0);
        Thread.Sleep(humanizer.NextDelayMs(AppOptions.Humanizer.KeyHoldMedianMs, AppOptions.Humanizer.KeyHoldMinMs, AppOptions.Humanizer.KeyHoldMaxMs));
        PostMessage(wowProcess.MainWindowHandle, WM_KEYUP, (int)key, 0);
    }

    /// <inheritdoc />
    public void RightClickMouse(PixelPoint position)
    {
        var activeProcess = GetActiveProcess();
        var wowProcess = locator.GetWowProcess();
        if (wowProcess is null)
        {
            return;
        }

        var oldPosition = Cursor.Position;

        SmoothMove(new Point(position.X, position.Y));

        Thread.Sleep(humanizer.NextDelayMs(AppOptions.Humanizer.PreClickSettleMedianMs, AppOptions.Humanizer.PreClickSettleMinMs, AppOptions.Humanizer.PreClickSettleMaxMs));

        mouse_event(MouseEvent.RIGHTDOWN, 0, 0, 0, UIntPtr.Zero);
        Thread.Sleep(humanizer.NextDelayMs(AppOptions.Humanizer.ClickHoldMedianMs, AppOptions.Humanizer.ClickHoldMinMs, AppOptions.Humanizer.ClickHoldMaxMs));
        mouse_event(MouseEvent.RIGHTUP, 0, 0, 0, UIntPtr.Zero);

        RefocusOnOldScreen(activeProcess, wowProcess, oldPosition);
    }

    private static Process GetActiveProcess()
    {
        var hwnd = GetForegroundWindow();
        GetWindowThreadProcessId(hwnd, out var pid);
        return Process.GetProcessById((int)pid);
    }

    private static void RefocusOnOldScreen(Process activeProcess, Process wowProcess, Point oldPosition)
    {
        try
        {
            if (!string.Equals(activeProcess.MainWindowTitle, wowProcess.MainWindowTitle, StringComparison.Ordinal))
            {
                PostMessage(activeProcess.MainWindowHandle, WM_RBUTTONDOWN, VK_RMB, 0);
                Thread.Sleep(AppOptions.WindowFocus.RestoreSettleMs);
                PostMessage(activeProcess.MainWindowHandle, WM_RBUTTONUP, VK_RMB, 0);

                PostMessage(wowProcess.MainWindowHandle, WM_KEYDOWN, (int)ConsoleKey.Escape, 0);
                Thread.Sleep(AppOptions.WindowFocus.RestoreSettleMs);
                PostMessage(wowProcess.MainWindowHandle, WM_KEYUP, (int)ConsoleKey.Escape, 0);

                Cursor.Position = oldPosition;
            }
        }
        catch
        {
        }
    }

    private static void SmoothMove(Point target)
    {
        var start = Cursor.Position;

        var dx = target.X - start.X;
        var dy = target.Y - start.Y;

        var distance = Math.Sqrt((dx * dx) + (dy * dy));

        var durationMs = (int)Math.Round(AppOptions.Cursor.BaseMoveMs + (AppOptions.Cursor.MsPerPixel * distance));
        durationMs = Math.Max(AppOptions.Cursor.MinMoveMs, Math.Min(AppOptions.Cursor.MaxMoveMs, durationMs));

        var steps = Math.Max(AppOptions.Cursor.MinSteps, Math.Min(AppOptions.Cursor.MaxSteps, durationMs / AppOptions.Cursor.StepTickMs));

        static double SmoothStep(double t) => t * t * (3.0 - (2.0 * t));

        var curve = Math.Min(AppOptions.Cursor.MaxCurvePx, distance / AppOptions.Cursor.CurveDistanceDivisor);

        var length = distance > 0 ? distance : 1;
        var perpX = -dy / length;
        var perpY = dx / length;

        var curveSign = Random.Shared.Next(0, 2) == 0 ? -1.0 : 1.0;
        var curveAmplitude = curve * curveSign;

        var stopwatch = Stopwatch.StartNew();

        for (var i = 1; i <= steps; i++)
        {
            var t = (double)i / steps;
            var eased = SmoothStep(t);

            var x = start.X + (dx * eased);
            var y = start.Y + (dy * eased);

            var arc = Math.Sin(Math.PI * t);
            x += perpX * curveAmplitude * arc;
            y += perpY * curveAmplitude * arc;

            Cursor.Position = new Point((int)Math.Round(x), (int)Math.Round(y));

            var targetElapsed = (int)Math.Round(durationMs * t);
            var sleepMs = targetElapsed - (int)stopwatch.ElapsedMilliseconds;
            if (sleepMs > 0)
            {
                Thread.Sleep(sleepMs);
            }
        }

        Cursor.Position = target;
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool PostMessage(IntPtr hWnd, uint msg, int wParam, int lParam);

    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    private static extern IntPtr GetWindowThreadProcessId(IntPtr hWnd, out uint processId);

    [DllImport("user32.dll")]
    private static extern void mouse_event(MouseEvent dwFlags, uint dx, uint dy, uint dwData, UIntPtr dwExtraInfo);

    private enum MouseEvent : uint
    {
        RIGHTDOWN = 0x0008,
        RIGHTUP = 0x0010,
    }
}
