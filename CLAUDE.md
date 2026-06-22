# CLAUDE.md

Guidance for AI agents working in this repository. This file is **always in context** — it is
deliberately short. Detailed, step-by-step procedures live in **Skills** (see below) and load on
demand, so read this first and let the relevant skill load when you act.

---

## 1. Project

**ToxicFishing** is a Windows desktop tool (.NET 10, **WPF / MVVM**) that automates fishing in
**World of Warcraft**. It captures the screen, locates the fishing bobber with computer vision
(OpenCV: HSV colour masking + `bobber.png` template matching), detects a bite from the game's splash
**sound** (WASAPI peak metering of the game's own audio session), and reacts by sending input to the
game window.

It interacts with the game in the **least-invasive way possible**: it only reads pixels and audio
metering the user can already see and hear, and sends standard Win32 input messages. It does **not**
inject DLLs, read or write game memory, or hook the kernel.

> Automation may violate the game's Terms of Service and server rules. This is **sample tooling**
> the user runs at their own risk; keep that warning visible in the README and UI, and never add
> features whose purpose is to defeat anti-cheat protections.

---

## 2. The non-negotiable priorities — in this order, ALWAYS

1. **Least-invasive by design.** Game interaction stays limited to reading screen pixels and sending
   standard OS input (Win32 `PostMessage` / `mouse_event`). **Never** add DLL injection, memory
   reading/writing, driver/kernel hooks, or any technique that touches the game's process internals.
   This protects the user's system and is the project's defining constraint.
2. **Human-like, non-deterministic behaviour.** Every action delay flows through `IHumanizer`
   (randomised jitter + a per-session fatigue model). Never hard-code a fixed, robotic delay where a
   humanized one belongs, and never produce a perfectly repeatable timing pattern.
3. **Detection reliability.** Find the bobber and detect bites with as few false positives/negatives
   as possible — a wrong detection wastes a cast or misses a fish.
4. **Low overhead & responsiveness.** The vision loop polls full-screen every ~20 ms; keep it
   lightweight, dispose every OpenCV `Mat`, run the session off the UI thread, and keep the UI
   responsive.

Everything else (clean code, architecture, docs, tests) is mandatory too, but when a genuine
trade-off forces a choice, resolve it in the order above. Never ship code that is incorrect or that
breaks the build to satisfy any priority.

---

## 3. Architecture — Modular by feature (MVVM shell)

The solution is a set of **feature modules — one assembly per capability** — plus a thin **shared
kernel** and a **WPF/MVVM shell** that composes them. Each module is a vertical slice that owns its
`public` contract, its `internal sealed` implementation, and its own DI registration
(`Add<Module>Module()`), all co-located. A module references the *projects* whose contracts it
consumes; the inter-module dependency graph is **acyclic** with `Shared` at the root. The shell is the
single composition root that wires everything.

```
 App (shell) ─▶ Fishing ─▶ Vision ───────────▶ Activity
                  │  ├────▶ Audio ────────────▶ Activity
                  │  ├────▶ Input ────────────▶ Activity, Humanization
                  │  └────▶ Humanization
                  └─────────────────────────────────────────▶ Shared  ◀── (every module)
 Test ─▶ every module (composes them with Add<Module>Module() exactly as the shell does)
```

| Project | Capability | Target | References |
|---|---|---|---|
| `ToxicFishing.Shared` | Shared kernel: `PixelPoint`, the `AppOptions` tuning tree, `HsvRange`. No behaviour. | `net10.0` | **BCL only** |
| `ToxicFishing.Activity` | Status/log reporting: `IActivityReporter` (+ `ActivityEntry`, `ActivityLevel`). | `net10.0` | BCL only |
| `ToxicFishing.Humanization` | Humanized timing: `IHumanizer`. | `net10.0` | Shared |
| `ToxicFishing.Vision` | OpenCV detection + screen capture: `IVision` (`OpenCvVision`, `ScreenCapture`). | `net10.0-windows` (WinForms) | Shared, Activity (+ OpenCvSharp4) |
| `ToxicFishing.Input` | Win32 input + process locator: `IInput`, `IProcessLocator` (`WowInput`, `WowProcessLocator`). | `net10.0-windows` (WinForms) | Shared, Activity, Humanization |
| `ToxicFishing.Audio` | Sound-based bite detection: `IAudioBiteDetector` (`WasapiAudioBiteDetector`). | `net10.0-windows` | Shared, Activity (+ NAudio.Wasapi) |
| `ToxicFishing.Fishing` | Orchestration: `IBotController`, `IFishingSession`. | `net10.0-windows` | Shared, Activity, Vision, Audio, Input, Humanization |
| `ToxicFishing.App` | WPF MVVM shell + composition root. | `net10.0-windows` (WPF) | every module |
| `ToxicFishing.Test` | NUnit + NSubstitute suite, organized `Unit/<Module>`. | `net10.0-windows` | every module |

Hard rules (full detail → **modular-architecture** skill):
- **One assembly per capability.** A module ships a `public` contract + `internal sealed`
  implementation + a `public static Add<Module>Module()` DI extension, together in that project.
- **Depend on contracts, not internals, and keep the graph acyclic** with `Shared` at the root.
  Never `new` another module's implementation — register and inject the interface.
- **`Shared` holds only platform-neutral primitives.** Modules that touch the OS/screen/audio/game
  process (`Vision`, `Audio`, `Input`, and therefore `Fishing`) target `net10.0-windows`; the leaf
  modules (`Activity`, `Humanization`, `Shared`) stay `net10.0`.
- **The shell composes; it does not implement.** `App.xaml.cs` chains the modules'
  `Add<Module>Module()` extensions, then adds `MainViewModel`/`MainWindow`; `Test` composes the same
  way via `TestHost`.
- Implementations are `internal sealed`, exposed to `Test` via `InternalsVisibleTo` where a test
  instantiates one directly (`Activity`, `Humanization`, `Audio`, `Fishing`).

---

## 4. Definition of Done

A change is complete only when **every** applicable box holds. Each maps to a skill.

- [ ] **Architecture** module boundaries respected; the inter-module dependency graph stays acyclic; new code lives in the right module with its contract, `internal sealed` impl, and DI registration co-located. → `modular-architecture`
- [ ] **Vision/detection** changes keep `Mat`s disposed and reliability intact; new tuning lives in `AppOptions`. → `vision-and-detection`
- [ ] **Input/discretion** changes stay least-invasive; all action delays route through `IHumanizer`. → `input-and-discretion`
- [ ] **Build is clean**: zero new analyzer/Roslynator warnings; code matches `.editorconfig`. → `clean-code`
- [ ] **NuGet** packages are on their latest stable, compatible versions (`OpenCvSharp4` and its runtime pinned together). → `clean-code`
- [ ] **Docs**: every non-`private` member has XML doc comments; `README.md` updated if behaviour/usage changed. → `documentation`
- [ ] **Tests** cover any new deterministic logic and `dotnet test` is green. → `testing`

---

## 5. Commands

```bash
# Build (Windows only; analyzers run during build, warnings must be zero)
dotnet build ToxicFishing.sln -c Release

# Run the WPF app (builds to ToxicFishing.exe)
dotnet run --project ToxicFishing.App

# Run the test suite (NUnit + NSubstitute)
dotnet test ToxicFishing.sln -c Release

# Auto-format to .editorconfig before committing
dotnet format ToxicFishing.sln
```

---

## 6. Skills

These project skills live in `.claude/skills/` and load when their trigger matches. Invoke one
explicitly with `/<name>` if it does not fire on its own.

| Skill | Use it when… |
|---|---|
| `modular-architecture` | adding/moving a type, choosing a module, wiring a module's DI, or reviewing the inter-module dependency graph |
| `clean-code` | writing/refactoring C#, fixing analyzer warnings, or updating NuGet packages |
| `documentation` | adding/changing any non-`private` member, or shipping a user-visible change |
| `testing` | adding deterministic logic that should be covered, or writing/fixing tests |
| `vision-and-detection` | touching screen capture, OpenCV detection, sound-based bite detection, or the `AppOptions` tuning constants |
| `input-and-discretion` | touching input, the game-process locator, humanized timing, or session threading |

---

## 7. Token efficiency

1. **Progressive disclosure.** This file stays small; depth lives in skills that load only when
   relevant. Point to a skill instead of pasting a procedure inline.
2. **Scoped tool use.** Prefer `Grep`/`Glob` and ranged `Read`s over reading whole files or trees.
   Don't re-read a file you just edited.
3. **Concise output.** No preamble/postamble; answer, then stop. Report failures plainly.

---

## 8. Conventions

- **Commits**: Conventional Commits (`feat:`, `fix:`, `chore:`, `docs:`, `refactor:`, `test:`).
- **Branching**: feature branches off `develop`; `master` is for releases.
- Commit or push **only when asked**.
