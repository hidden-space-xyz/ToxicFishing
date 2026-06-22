---
name: input-and-discretion
description: >-
  The input and discretion model of ToxicFishing — how it talks to the game and why it stays
  least-invasive. Use when touching input (WowInput), the game-process locator, the Humanizer
  / timing model, or the session threading/cancellation in BotController. The least-invasive,
  human-like principle is non-negotiable here.
---

# Input & Discretion

This covers **how ToxicFishing sends input to the game** and the principles that keep it
least-invasive and non-robotic. These principles outrank performance and convenience — see the
priority order in `CLAUDE.md`.

## Least-invasive by design (non-negotiable)

The tool interacts with the game using **only** standard means:

- **Keys** — `WowInput.PressKey` posts Win32 `WM_KEYDOWN` / `WM_KEYUP` to the game window
  (`PostMessage` to `MainWindowHandle`).
- **Clicks** — `RightClickMouse` moves the cursor (a smoothed move) and fires `mouse_event`
  RIGHTDOWN/UP, then **refocuses the original foreground window** so the bot is unobtrusive.
- **Reading** — the only "reads" are screen pixels (vision) and enumerating the game process by name.

**Never** add DLL injection, reading or writing the game's memory, driver/kernel hooks, or anything
that touches the game process internals. That constraint is the project's reason to exist; it keeps
the user's system safe and is not negotiable for any feature or speedup.

## Targeting the game process

`WowProcessLocator` (`Input`) drives **whichever window the user picked** in the UI:
`IProcessLocator.GetCandidates()` lists running processes that own a visible main window (as
`GameProcess` items), the view model binds them to a combo box, and `IProcessLocator.Select(pid)`
points the locator at the chosen one. There is **no name-based auto-detection** — if no window is
selected (or the selected one has exited), the locator reports via `IActivityReporter` and input
becomes a no-op (`PostMessage`/click guard on a null process). The resolved handle is **cached** and
revalidated (`IsAlive`) before reuse. `IProcessLocator` lives in the `Input` module alongside its Win32
implementation because it exposes a `Process` (see `modular-architecture`).

## Human-like timing (the Humanizer)

**Every action delay flows through `IHumanizer`.** `Humanizer` produces randomised, log-normal-style
jitter around a median (`Sigma`) clamped to `[min, max]`, plus a **per-session fatigue model**
(`FatiguePerHour`, `FatigueMax`) that gradually lengthens delays the longer a session runs.
`StartSession()` resets the fatigue clock at the start of each run.

Rules:

- Route reaction time, key-hold, click-hold, pre-click settle, and post-loot delays through
  `IHumanizer` with the median/min/max constants from `AppOptions.Humanizer` — **never** hard-code a
  fixed `Thread.Sleep` where a humanized delay belongs, and never produce a perfectly repeatable
  pattern.
- A few *structural* waits (e.g. the fixed post-lure settle, despawn polling) are legitimately
  constant — but anything that simulates a human *act* (pressing, clicking, reacting) must be
  humanized.
- New tuning belongs in `AppOptions`, not inline: humanized delays in `AppOptions.Humanizer`, the
  smoothed cursor-move shape in `AppOptions.Cursor`, and window-restore timing in
  `AppOptions.WindowFocus`.

## Session threading & lifecycle

- `BotController` is the single Start/Stop authority. `Start` launches the session on a **background
  `Task`** (`RunLoop` → `FishingSession.Run`); the UI thread is never blocked.
- It is **thread-safe** via a `Lock` guarding the `CancellationTokenSource`. A scheduled run uses
  `CancelAfter(duration)`; `Stop()` cancels the token; `OperationCanceledException` is swallowed
  cleanly. `RunningChanged` notifies the UI (marshalled through the `Dispatcher` in the view model).
- `App.OnExit` stops the bot, so a running session always ends on app close. Preserve this — never
  leave a session running without a cancellation path.

## Responsible use

Automation may violate the game's Terms of Service. Keep the warning visible in the README and UI.
Do **not** add features whose purpose is to defeat anti-cheat detection — humanization here exists to
avoid robotic, low-quality behaviour and to keep the tool least-invasive, not to evade enforcement.

## Before you finish

- No injection / memory access / hooks introduced; input stays `PostMessage` + `mouse_event` only.
- Every simulated human action delay goes through `IHumanizer`; new timing constants live in `AppOptions`.
- The session still runs off the UI thread and remains cancellable; `OnExit` still stops it.
- No platform type leaked into `Shared` or a leaf module (see `modular-architecture`).
