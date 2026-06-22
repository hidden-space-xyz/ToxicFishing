---
name: modular-architecture
description: >-
  Enforces the feature-modular architecture of ToxicFishing (one assembly per capability — Shared,
  Activity, Humanization, Vision, Input, Fishing — composed by a WPF/MVVM shell). Use when adding,
  moving, or renaming a type; deciding which module code belongs in; introducing a contract or its
  implementation; wiring a module's dependency injection; or reviewing whether the inter-module
  dependency graph is still acyclic.
---

# Modular architecture (by feature)

The solution is a set of **feature modules**, **one assembly per capability**, plus a thin **shared
kernel** (`Shared`) and a **WPF/MVVM shell** (`App`) that composes them. Each module is a **vertical
slice**: it owns its `public` contract, its `internal sealed` implementation, and its own DI
registration extension — co-located in one project.

## Modules and their dependencies

A module references the *projects* whose contracts it consumes. The inter-module graph is **acyclic**,
rooted at `Shared`.

| Project | Capability (owns) | Target | References |
|---|---|---|---|
| `ToxicFishing.Shared` | Platform-neutral primitives: `PixelPoint`, the `AppOptions` tuning tree, `HsvRange`. No behaviour, no DI. | `net10.0` | **BCL only** (dev-only analyzers with `PrivateAssets=all` aside). |
| `ToxicFishing.Activity` | `IActivityReporter` + `ActivityReporter`; the `ActivityEntry`/`ActivityLevel` models; `AddActivityModule()`. | `net10.0` | BCL + `Microsoft.Extensions.DependencyInjection.Abstractions`. |
| `ToxicFishing.Humanization` | `IHumanizer` + `Humanizer`; `AddHumanizationModule()`. | `net10.0` | Shared. |
| `ToxicFishing.Vision` | `IVision` + `OpenCvVision` + `ScreenCapture`; `AddVisionModule()`. | `net10.0-windows` (`UseWindowsForms`) | Shared, Activity (+ `OpenCvSharp4`, `OpenCvSharp4.runtime.win`). |
| `ToxicFishing.Input` | `IInput`, `IProcessLocator` + `WowInput` + `WowProcessLocator`; `AddInputModule()`. | `net10.0-windows` (`UseWindowsForms`) | Shared, Activity, Humanization. |
| `ToxicFishing.Audio` | `IAudioBiteDetector` + `WasapiAudioBiteDetector` (+ `PeakSpikeAnalyzer`); `AddAudioModule()`. | `net10.0-windows` | Shared, Activity (+ `NAudio.Wasapi`). |
| `ToxicFishing.Fishing` | `IBotController`, `IFishingSession` + their `internal sealed` impls; `AddFishingModule()`. | `net10.0-windows` | Shared, Activity, Vision, Audio, Input, Humanization. |
| `ToxicFishing.App` | WPF MVVM shell (views, view models, converters, `RelayCommand`) **and** the composition root. | `net10.0-windows` (`UseWPF`) | Every module (+ `Microsoft.Extensions.DependencyInjection`). |
| `ToxicFishing.Test` | NUnit + NSubstitute suite, organized `Unit/<Module>` + `Common`. | `net10.0-windows` | Every module (+ the full DI package). |

## Hard rules

1. **One capability per assembly; contract + impl + DI together.** A module exposes a `public`
   interface, an `internal sealed` implementation, and a `public static class <Module>Module` with an
   `Add<Module>Module(this IServiceCollection)` extension — all in that project. The extension can
   register the `internal` type because it lives in the same assembly.
2. **Depend on contracts, never on another module's internals.** Reference the module *project* whose
   `public` interface you need and inject the interface. Never widen an implementation to `public` to
   reach it, and never `new` it from another module.
3. **Keep the dependency graph acyclic, rooted at `Shared`.** `Shared` references nothing.
   `Activity`/`Humanization` are leaves. `Vision`→Activity; `Input`→Activity+Humanization;
   `Fishing`→Vision+Input+Humanization+Activity. If a change would introduce a cycle (e.g. `Vision`
   needing `Fishing`), the shared piece belongs in `Shared` or a new leaf module instead.
4. **`Shared` is platform-neutral primitives only** — value types and the `AppOptions` constant tree.
   No WPF/WinForms/Win32/OpenCV, no DI, no behaviour. A module that needs the OS/screen/game process
   (`Vision`, `Input`, and therefore `Fishing`) targets `net10.0-windows`; leaf modules stay `net10.0`.
5. **The shell composes; it does not implement.** `App.xaml.cs` `OnStartup` chains
   `.AddActivityModule().AddHumanizationModule().AddVisionModule().AddInputModule().AddFishingModule()`,
   then adds `MainViewModel` + `MainWindow`, builds the provider, and shows the window. `OnExit` stops
   the bot via `IBotController.Stop()` and disposes the provider. There is **no** separate composition
   assembly.
6. **Implementations are `internal sealed`.** Expose one to the test project via `InternalsVisibleTo`
   (declared in the module `.csproj`) only where a test instantiates it directly — currently
   `Activity`, `Humanization`, and `Fishing`.

## Within a module — folder layout

Each module groups its files into consistent folders, and **the namespace matches the folder**
(`.editorconfig` sets `dotnet_style_namespace_match_folder = true`):

- `Abstractions/` → the `public` contract interfaces → namespace `ToxicFishing.<Module>.Abstractions`.
- `Services/` → the `internal sealed` implementations → namespace `ToxicFishing.<Module>.Services`.
- `Models/` → public data/value types, where the module has any (e.g. `Activity` keeps
  `ActivityEntry`/`ActivityLevel` in `ToxicFishing.Activity.Models`).
- The `Add<Module>Module()` extension stays at the **module root** (`ToxicFishing.<Module>`), so the
  shell and tests compose with just `using ToxicFishing.<Module>;`.

`Shared` is the exception (it owns no contracts/services): it uses `Primitives/`
(`ToxicFishing.Shared.Primitives` — `PixelPoint`) and `Configuration/`
(`ToxicFishing.Shared.Configuration` — `AppOptions`, `HsvRange`). When you add a file, drop it in the
matching folder and give it the matching sub-namespace.

## Where does new code go? (decision guide)

- A **platform-neutral primitive / value type** or a **new tuning constant** → **`Shared`**
  (`AppOptions`, see `vision-and-detection`).
- A **new capability** (a cohesive slice with its own contract + impl) → **a new module project**
  named for the capability; give it an `Add<Module>Module()` extension and reference it from `App`
  (and `Test`).
- A **new service/rule inside an existing capability** → that module, behind its existing contract.
- A **status message type or reporting concern** → **`Activity`**.
- A **humanized-timing concern** → **`Humanization`**.
- **Screen/OpenCV** work → **`Vision`**; **Win32 input / process** work → **`Input`**.
- **Orchestration** of the fishing loop → **`Fishing`**.
- **UI, view models, dialogs, converters, composition** → **`App`**.

## The composition root (the shell)

Each module self-registers via its `Add<Module>Module()` extension (every service a **singleton**).
`App.xaml.cs` is the only place that chains them; `Test`'s `TestHost.CreateProvider()` chains the same
extensions so tests exercise the real graph across module boundaries. To add a registration, edit the
owning module's `*Module.cs` — not the shell.

## Before you finish

- New code lives in the module that owns its capability; its contract, `internal sealed` impl, and DI
  registration are co-located.
- No module references another module's implementation; the inter-module graph is still acyclic with
  `Shared` at the root.
- `Shared` `.csproj` still has **no** `ProjectReference` and no runtime NuGet; leaf modules are still
  `net10.0`.
- No platform type leaked into `Shared` or a leaf (`net10.0`) module.
- New implementations are `internal sealed`; new contracts are `public`; new services are registered
  in their module's `Add<Module>Module()`, not `new`ed across a module boundary.
