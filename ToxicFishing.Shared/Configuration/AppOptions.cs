namespace ToxicFishing.Shared.Configuration;

/// <summary>
/// Compile-time tuning shared across every module. Every threshold, radius, colour band, history size,
/// and timing lives here as a constant — there is deliberately no <c>appsettings.json</c>, so re-tuning
/// means editing this file. The one runtime-adjustable knob is the bobber template image.
/// </summary>
public static class AppOptions
{
    /// <summary>
    /// High-level fishing-loop behaviour: which keys to press and the loop's coarse timings.
    /// </summary>
    public static class Bot
    {
        /// <summary>
        /// The key bound in-game to the fishing cast.
        /// </summary>
        public const ConsoleKey CastKey = ConsoleKey.D1;

        /// <summary>
        /// The key bound in-game to applying a fishing lure.
        /// </summary>
        public const ConsoleKey LureKey = ConsoleKey.D2;

        /// <summary>
        /// A harmless key pressed at startup to bring the game out of an idle/AFK state.
        /// </summary>
        public const ConsoleKey WakeKey = ConsoleKey.Spacebar;

        /// <summary>
        /// Maximum length of a single fishing session, in minutes, before it self-stops.
        /// </summary>
        public const int MaxFishingMinutes = 60;

        /// <summary>
        /// How often, in minutes, the fishing lure is re-applied.
        /// </summary>
        public const int LureRefreshMinutes = 10;

        /// <summary>
        /// Grace within the refresh minute, in seconds, before the lure is re-applied.
        /// </summary>
        public const int LureRefreshGraceSeconds = 30;

        /// <summary>
        /// Delay after the wake key before the loop starts casting, in milliseconds.
        /// </summary>
        public const int PreStartDelayMs = 1500;

        /// <summary>
        /// How long to let the float settle after a cast before watching for a bite, in milliseconds.
        /// </summary>
        public const int AfterCastSettleMs = 2000;

        /// <summary>
        /// How long to wait after applying the lure for the buff to take hold, in milliseconds.
        /// </summary>
        public const int LureSettleMs = 5000;

        /// <summary>
        /// How long the loop backs off after an unexpected error before retrying, in milliseconds.
        /// </summary>
        public const int ErrorBackoffMs = 2000;
    }

    /// <summary>
    /// Constants for the humanized timing model used by the Humanization module.
    /// </summary>
    public static class Humanizer
    {
        /// <summary>
        /// Spread of the log-normal jitter; larger values widen the variation around the median.
        /// </summary>
        public const double Sigma = 0.30;

        /// <summary>
        /// Median reaction time before reeling in after a bite, in milliseconds.
        /// </summary>
        public const int ReactionMedianMs = 420;

        /// <summary>
        /// Minimum reaction time, in milliseconds.
        /// </summary>
        public const int ReactionMinMs = 200;

        /// <summary>
        /// Maximum reaction time, in milliseconds.
        /// </summary>
        public const int ReactionMaxMs = 1100;

        /// <summary>
        /// Median time a key is held down, in milliseconds.
        /// </summary>
        public const int KeyHoldMedianMs = 80;

        /// <summary>
        /// Minimum key-hold time, in milliseconds.
        /// </summary>
        public const int KeyHoldMinMs = 45;

        /// <summary>
        /// Maximum key-hold time, in milliseconds.
        /// </summary>
        public const int KeyHoldMaxMs = 200;

        /// <summary>
        /// Median time the mouse button is held during a click, in milliseconds.
        /// </summary>
        public const int ClickHoldMedianMs = 55;

        /// <summary>
        /// Minimum click-hold time, in milliseconds.
        /// </summary>
        public const int ClickHoldMinMs = 30;

        /// <summary>
        /// Maximum click-hold time, in milliseconds.
        /// </summary>
        public const int ClickHoldMaxMs = 160;

        /// <summary>
        /// Median settle pause after moving the cursor and before clicking, in milliseconds.
        /// </summary>
        public const int PreClickSettleMedianMs = 45;

        /// <summary>
        /// Minimum pre-click settle time, in milliseconds.
        /// </summary>
        public const int PreClickSettleMinMs = 20;

        /// <summary>
        /// Maximum pre-click settle time, in milliseconds.
        /// </summary>
        public const int PreClickSettleMaxMs = 130;

        /// <summary>
        /// Median pause after looting before the next cast, in milliseconds.
        /// </summary>
        public const int PostLootMedianMs = 1050;

        /// <summary>
        /// Minimum post-loot pause, in milliseconds.
        /// </summary>
        public const int PostLootMinMs = 700;

        /// <summary>
        /// Maximum post-loot pause, in milliseconds.
        /// </summary>
        public const int PostLootMaxMs = 1900;

        /// <summary>
        /// Additional fractional delay accrued per hour of continuous play (the fatigue ramp).
        /// </summary>
        public const double FatiguePerHour = 0.20;

        /// <summary>
        /// Upper bound on the fatigue multiplier so delays never grow without limit.
        /// </summary>
        public const double FatigueMax = 0.30;
    }

    /// <summary>
    /// Tuning for the humanized smooth cursor move the Input module performs before the reel-in click.
    /// </summary>
    public static class Cursor
    {
        /// <summary>
        /// Base move duration, in milliseconds, before the per-distance term is added.
        /// </summary>
        public const double BaseMoveMs = 55;

        /// <summary>
        /// Additional move duration accrued per pixel of travel, in milliseconds.
        /// </summary>
        public const double MsPerPixel = 0.35;

        /// <summary>
        /// Lower clamp on the move duration, in milliseconds.
        /// </summary>
        public const int MinMoveMs = 45;

        /// <summary>
        /// Upper clamp on the move duration, in milliseconds.
        /// </summary>
        public const int MaxMoveMs = 200;

        /// <summary>
        /// Nominal milliseconds per interpolation step, used to derive the step count from the duration.
        /// </summary>
        public const int StepTickMs = 1;

        /// <summary>
        /// Lower clamp on the number of interpolation steps in a move.
        /// </summary>
        public const int MinSteps = 18;

        /// <summary>
        /// Upper clamp on the number of interpolation steps in a move.
        /// </summary>
        public const int MaxSteps = 120;

        /// <summary>
        /// Upper clamp on the lateral arc amplitude of the move, in pixels.
        /// </summary>
        public const double MaxCurvePx = 8.0;

        /// <summary>
        /// Divisor mapping travel distance to arc amplitude; larger values flatten the arc.
        /// </summary>
        public const double CurveDistanceDivisor = 25.0;
    }

    /// <summary>
    /// Timings for restoring the previously-active window after a reel-in click, used by the Input module.
    /// </summary>
    public static class WindowFocus
    {
        /// <summary>
        /// Gap between synthesized press and release when restoring the prior window, in milliseconds.
        /// </summary>
        public const int RestoreSettleMs = 30;
    }

    /// <summary>
    /// Timings for the watch loop that tracks the bobber (for the loot click) while the audio listener
    /// decides the bite.
    /// </summary>
    public static class Watch
    {
        /// <summary>
        /// Consecutive missing frames after which the watch is abandoned and the loop recasts.
        /// </summary>
        public const int MaxLostBeforeAbort = 12;

        /// <summary>
        /// Poll cadence of the watch loop, in milliseconds.
        /// </summary>
        public const int PollMs = 20;

        /// <summary>
        /// Maximum time to watch a single cast for a bite before recasting, in seconds.
        /// </summary>
        public const int MaxWatchSeconds = 25;

        /// <summary>
        /// Maximum time to search for the freshly-cast bobber before recasting, in seconds.
        /// </summary>
        public const int FindBobberTimeoutSeconds = 5;

        /// <summary>
        /// Pause before retrying after ignoring a ghost re-detection of the just-looted float, in
        /// milliseconds.
        /// </summary>
        public const int GhostRetryMs = 35;
    }

    /// <summary>
    /// Guards against re-locking the just-looted bobber for a short window after looting.
    /// </summary>
    public static class GhostBobber
    {
        /// <summary>
        /// Radius around the looted position, in pixels, within which re-detections are ignored.
        /// </summary>
        public const int IgnoreRadiusPx = 70;

        /// <summary>
        /// How long, in milliseconds, re-detections near the looted position remain ignored.
        /// </summary>
        public const int IgnoreWindowMs = 3500;
    }

    /// <summary>
    /// Controls waiting for the previous float to despawn before recasting.
    /// </summary>
    public static class DespawnWait
    {
        /// <summary>
        /// Maximum time to wait for the old float to vanish, in milliseconds.
        /// </summary>
        public const int MaxMs = 2500;

        /// <summary>
        /// Poll cadence while waiting for despawn, in milliseconds.
        /// </summary>
        public const int PollMs = 60;

        /// <summary>
        /// Consecutive "clear" polls required to conclude the old float has despawned.
        /// </summary>
        public const int RequireClearStreak = 3;
    }

    /// <summary>
    /// Constants for sound-based bite detection used by the Audio module. The bite is inferred from the
    /// loud, brief splash spike in the game's audio output. Peak values are normalised to <c>[0, 1]</c>.
    /// These are starting points — they are best validated and tuned in-game.
    /// </summary>
    public static class AudioDetection
    {
        /// <summary>
        /// Absolute peak floor a sample must exceed before it can count as a splash, guarding against
        /// quiet ambience tripping detection.
        /// </summary>
        public const double MinPeak = 0.18;

        /// <summary>
        /// Required rise above the adaptive ambient baseline for a sample to count as a splash spike.
        /// </summary>
        public const double MinRise = 0.10;

        /// <summary>
        /// Exponential-moving-average weight used to track the ambient baseline; larger values let the
        /// baseline follow the ambient level more quickly.
        /// </summary>
        public const double BaselineAlpha = 0.08;

        /// <summary>
        /// Warm-up samples observed before detection arms, letting the baseline settle after a cast.
        /// </summary>
        public const int SeedSamples = 8;

        /// <summary>
        /// Refractory period after a detected bite, in milliseconds, suppressing duplicate triggers.
        /// </summary>
        public const int RefractoryMs = 1500;
    }

    /// <summary>
    /// Constants for the OpenCV detector used by the Vision module.
    /// </summary>
    public static class Vision
    {
        /// <summary>
        /// Default bobber template file, resolved relative to the application base directory.
        /// </summary>
        public const string TemplatePath = @"Resources\bobber.png";

        /// <summary>
        /// Minimum normalised template-match score accepted as a bobber candidate.
        /// </summary>
        public const double TemplateMatchThreshold = 0.40;

        /// <summary>
        /// Smallest template scale in the multi-scale match pyramid.
        /// </summary>
        public const double TemplateScaleMin = 0.5;

        /// <summary>
        /// Largest template scale in the multi-scale match pyramid.
        /// </summary>
        public const double TemplateScaleMax = 1.6;

        /// <summary>
        /// Number of discrete scales generated between the min and max template scales.
        /// </summary>
        public const int TemplateScaleSteps = 6;

        /// <summary>
        /// Extra padding, in pixels, added around the template-match search window.
        /// </summary>
        public const int TemplatePaddingPx = 12;

        /// <summary>
        /// Radius, in pixels, around the previous detection within which tracking is preferred.
        /// </summary>
        public const int TrackingSearchRadiusPx = 90;

        /// <summary>
        /// Smallest contour area, in pixels, accepted as a candidate blob.
        /// </summary>
        public const int MinBlobArea = 15;

        /// <summary>
        /// Largest contour area, in pixels, accepted as a candidate blob.
        /// </summary>
        public const int MaxBlobArea = 6000;

        /// <summary>
        /// Maximum number of blob candidates passed to template matching per frame.
        /// </summary>
        public const int MaxTemplateCandidates = 64;

        /// <summary>
        /// Side length, in pixels, of the morphology kernel used to clean the colour mask.
        /// </summary>
        public const int MorphKernel = 3;

        /// <summary>
        /// Mask pixel count above which the frame is treated as too noisy to scan.
        /// </summary>
        public const int TooMuchMaskThreshold = 400_000;

        /// <summary>
        /// Weight given to proximity to the screen centre when scoring candidates.
        /// </summary>
        public const double CenterBiasWeight = 0.25;

        /// <summary>
        /// Weight given to proximity to the previous detection when scoring candidates.
        /// </summary>
        public const double TrackingBiasWeight = 0.50;

        /// <summary>
        /// HSV colour bands the bobber's float matches, tuned to include its colours across UI scales
        /// while excluding the game's red UI.
        /// </summary>
        public static readonly HsvRange[] ColorRanges =
        [
            new(0, 12, 90, 255, 70, 255),
            new(165, 179, 90, 255, 70, 255),
            new(8, 25, 80, 255, 90, 255),
            new(95, 130, 90, 255, 70, 255),
        ];
    }
}
