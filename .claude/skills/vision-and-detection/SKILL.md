---
name: vision-and-detection
description: >-
  The computer-vision and bite-detection pipeline of ToxicFishing. Use when touching screen capture,
  the OpenCV bobber detector (HSV masking + template matching + tracking), bite detection, the
  ghost/despawn logic, OpenCV Mat lifetime/performance, or the AppOptions tuning constants. Detection
  reliability and bounded overhead are the priorities here.
---

# Vision & Detection

This pipeline decides *where the bobber is* and *when a fish bites*. Priorities, in order:
**reliability → low overhead** — and both sit below the least-invasive principle in `CLAUDE.md`.

## The pipeline

1. **Capture** — `ScreenCapture` (`Vision`) grabs the **primary monitor** via GDI
   `Graphics.CopyFromScreen` into an OpenCV `Mat`. Capture is full-screen so the bobber is found
   wherever it lands.
2. **Detect** — `OpenCvVision.TryFindBobber` combines:
   - **HSV colour masking** over `AppOptions.Vision.ColorRanges` (several `HsvRange` bands tuned to the
     bobber's float and to *exclude* the game's red UI), with a morphology kernel
     (`MorphKernel`) to clean the mask. If the mask covers more than `TooMuchMaskThreshold` pixels the
     frame is treated as too noisy.
   - **Multi-scale template matching** of `bobber.png` (a pyramid from `TemplateScaleMin` to
     `TemplateScaleMax` in `TemplateScaleSteps`), accepted above `TemplateMatchThreshold`.
   - **Blob filtering** by area (`MinBlobArea`..`MaxBlobArea`) and a candidate cap
     (`MaxTemplateCandidates`).
   - **Tracking & centre bias** — candidates near the previous detection (`TrackingSearchRadiusPx`)
     and near the screen centre are preferred (`TrackingBiasWeight`, `CenterBiasWeight`) so a
     freshly-cast float wins over background noise.
   It outputs a `PixelPoint` (`Shared`).
3. **Decide a bite** — the bite trigger is **sound**, not motion. `IAudioBiteDetector` (the `Audio`
   module's `WasapiAudioBiteDetector`) watches the peak-loudness meter of the game's own audio session
   (WASAPI via NAudio) and reports the sharp splash spike. The decision lives in the deterministic
   `PeakSpikeAnalyzer`, governed by `AppOptions.AudioDetection` (`MinPeak`, `MinRise`, `BaselineAlpha`,
   `SeedSamples`, `RefractoryMs`). `FishingSession` keeps **visually tracking** the bobber only to know
   where to right-click and to recast if it loses sight of it (`Watch.MaxLostBeforeAbort`, poll cadence
   `Watch.PollMs`).
4. **Loot & settle** — after looting, `GhostBobber` (ignore re-detections near the just-looted spot
   for a window) and `DespawnWait` (wait for the old float to vanish before recasting) prevent the
   detector from re-locking the corpse of the previous bobber.

## Tuning lives in `AppOptions`

**Every** threshold, radius, colour band, history size, and timing is a compile-time constant in
`ToxicFishing.Shared/AppOptions.cs`; there is **no `appsettings.json`**. To re-tune detection, edit
`AppOptions`; add new knobs there (in the right nested class: `Vision`, `AudioDetection`, `Watch`,
`GhostBobber`, `DespawnWait`) rather than scattering magic numbers through the services. The **only**
runtime knob is the bobber template: `IVision.LoadTemplate` / the app's **Change bobber…** command
swaps `bobber.png` live.

## Reliability rules

- **Prefer fewer false positives.** A false bite wastes a cast and desyncs the loop; the tracking
  bias and ghost/despawn logic exist to avoid re-detecting the previous float. When in doubt, tighten.
- Keep HSV bands narrow enough to reject the red UI but wide enough for the bobber across UI scales.
- When you change a detection rule, sanity-check it against varied resolutions / UI scales — there is
  no automated detector test (see `testing`).

## Performance & `Mat` lifetime (priority: low overhead)

The detector runs in a tight poll loop (~`Watch.PollMs` = 20 ms) over full-screen frames, so it
must stay light:

- **Dispose every OpenCV `Mat`.** `Mat` holds unmanaged memory; a leak in the loop is a fast memory
  blowout. Use `using` for per-frame Mats; the template pyramid and morph kernel are long-lived fields
  disposed with the adapter (`OpenCvVision : IDisposable`). Never return a live `Mat` across
  the abstraction — `IVision` deals in `PixelPoint`, not pixels.
- Reuse the long-lived `morphKernel` and template pyramid; rebuild the pyramid only when the template
  changes (guarded by `templateGate`).
- Cap work: respect `MaxTemplateCandidates` and the `TooMuchMaskThreshold` early-out.
- The vision/session loop runs on a **background `Task`** (started by `BotController`), never the UI
  thread. Report progress through `IActivityReporter`; the view model marshals it to the UI via the
  `Dispatcher`. Keep heavy work off the UI thread.

## Before you finish

- No `Mat` (or other OpenCV/`IDisposable`) leaks on any frame path.
- New constants live in `AppOptions`, documented (see `documentation`).
- No platform/OpenCV type leaked into `Shared` or a leaf module (see `modular-architecture`); the
  detector stays behind `IVision`.
- Reliability not regressed: false-positive guards (tracking bias, ghost/despawn) intact.
