---
name: testing
description: >-
  Testing conventions for ToxicFishing. Use when adding or changing deterministic logic that should
  be covered, or when writing or fixing tests. The contract/DI boundary of each module makes its
  logic testable; the platform edges are not.
---

# Testing

The solution has a **`ToxicFishing.Test`** project (NUnit + NSubstitute).

## What is worth testing (deterministic, platform-free)

The modules are designed to be testable through their contracts:

- **`PixelPoint`** (`Shared`) — `DistanceTo`, `IsEmpty`, equality.
- **`PeakSpikeAnalyzer`** (`Audio`) — the spike/baseline decision over a sequence of normalised peak
  samples, driven by `AppOptions.AudioDetection` (warm-up, absolute floor, rise-above-baseline). The
  WASAPI device access in `WasapiAudioBiteDetector` is a platform edge — see below.
- **`Humanizer`** — that `NextDelayMs(median, min, max)` and `NextReactionMs()` always fall within
  `[min, max]`, and that the fatigue model trends as configured across a session.
- **`FishingSession` / `BotController`** — branch coverage of the cast → watch → loot loop and
  the start/stop/cancel paths, with `IVision`, `IInput`, `IAudioBiteDetector`, and
  `IActivityReporter` replaced by test doubles.

## What NOT to unit-test (platform edges)

These wrap Win32 / OpenCV / the screen / the process table and have no deterministic seam below their
interface — cover them by their abstraction and test the *callers*, not the boundary itself:

- `WowInput` (Win32 `PostMessage` / `mouse_event`), `WowProcessLocator` (`Process` enumeration).
- `OpenCvVision`, `ScreenCapture` (GDI capture + OpenCV pixel work).

## The test project — layout and conventions

`ToxicFishing.Test` references every module. Tests live under `Unit/<Module>` (e.g. `Unit/Shared`,
`Unit/Activity`, `Unit/Humanization`, `Unit/Fishing`, and `Unit/Modules` for the DI-registration test)
with shared helpers in `Common` (e.g. `TestHost`, which builds the real graph by composing every module
exactly as the shell does). `AssemblyHooks.cs` sets
`[assembly: FixtureLifeCycle(LifeCycle.InstancePerTestCase)]`.

- Use **NUnit + NSubstitute**. `NUnit.Framework` is a global `Using`; quality is guarded by
  `NUnit.Analyzers`.
- Test classes are `public sealed`; name methods `Method_Scenario_ExpectedResult`
  (e.g. `IsBite_SharpDipThenRebound_ReportsBiteOnRebound`).
- Use the **constraint model**: `Assert.That(actual, Is.EqualTo(expected))`; group related asserts in
  `using (Assert.EnterMultipleScope())`.
- Drive variants with `[TestCase]` / `[TestCaseSource]`.
- Inject `NSubstitute` doubles for the abstractions to isolate a unit and exercise error branches.
- Implementations are `internal sealed`; the test project sees them through `InternalsVisibleTo`
  (declared in the `Activity`, `Humanization`, and `Fishing` `.csproj`), so it can `new` them up
  directly — no need to widen anything to `public`.
- To measure coverage, add `coverlet.collector` to the test project, then
  `dotnet test --collect:"XPlat Code Coverage"`.

## Definition of done for a change

- New/changed **deterministic** behaviour has direct tests (including error branches).
- Platform-edge code is isolated behind its interface so it doesn't block coverage of the logic above it.
- `dotnet test` is green.
