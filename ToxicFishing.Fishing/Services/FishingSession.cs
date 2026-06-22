using System.Diagnostics;

using ToxicFishing.Activity.Abstractions;
using ToxicFishing.Audio.Abstractions;
using ToxicFishing.Fishing.Abstractions;
using ToxicFishing.Humanization.Abstractions;
using ToxicFishing.Input.Abstractions;
using ToxicFishing.Shared.Configuration;
using ToxicFishing.Shared.Primitives;
using ToxicFishing.Vision.Abstractions;

namespace ToxicFishing.Fishing.Services;

/// <summary>
/// Default <see cref="IFishingSession"/>. Drives the cast → watch → loot loop: applies the
/// lure, casts, watches for the splash sound that signals a bite while visually tracking the bobber
/// for the reel-in click, reels in, and waits out the ghost/despawn window before recasting — all
/// humanized and cancellable.
/// </summary>
/// <param name="vision">Locates and tracks the bobber (for the loot click and recast logic).</param>
/// <param name="audio">Decides a bite from the game's splash sound.</param>
/// <param name="input">Sends casts, lure presses, and the reel-in click.</param>
/// <param name="locator">Resolves the game process whose audio session is monitored.</param>
/// <param name="activity">Sink for status messages.</param>
/// <param name="humanizer">Source of humanized reaction and post-loot delays.</param>
internal sealed class FishingSession(
    IVision vision,
    IAudioBiteDetector audio,
    IInput input,
    IProcessLocator locator,
    IActivityReporter activity,
    IHumanizer humanizer) : IFishingSession
{
    private bool enabled;
    private int catches;

    private readonly Stopwatch sessionTimer = new();
    private readonly Stopwatch lureTimer = new();

    private PixelPoint lastLootedBobberPosition = PixelPoint.Empty;
    private long lastLootedTimestamp;

    /// <inheritdoc />
    public void Run(CancellationToken cancellationToken = default)
    {
        enabled = true;
        sessionTimer.Restart();
        humanizer.StartSession();

        vision.Configure();

        lureTimer.Restart();
        ApplyLure();

        while (enabled && !cancellationToken.IsCancellationRequested)
        {
            try
            {
                RefreshLureIfDue();

                activity.Info("Casting…");
                input.PressKey(AppOptions.Bot.CastKey);

                SettleAfterCast(AppOptions.Bot.AfterCastSettleMs);

                WatchForBite(cancellationToken);
                StopIfMaxTimeReached();
            }
            catch (Exception ex)
            {
                activity.Error($"Unexpected error: {ex.Message}");
                Thread.Sleep(AppOptions.Bot.ErrorBackoffMs);
            }
        }
    }

    private void Stop()
    {
        enabled = false;
        sessionTimer.Reset();
    }

    private void StopIfMaxTimeReached()
    {
        if ((int)sessionTimer.Elapsed.TotalMinutes > AppOptions.Bot.MaxFishingMinutes)
        {
            activity.Warning("Maximum session time reached — stopping.");
            Stop();
        }
    }

    private void RefreshLureIfDue()
    {
        var elapsed = lureTimer.Elapsed;

        if ((elapsed.TotalMinutes >= AppOptions.Bot.LureRefreshMinutes && elapsed.Seconds > AppOptions.Bot.LureRefreshGraceSeconds) || elapsed.TotalMinutes > AppOptions.Bot.LureRefreshMinutes)
        {
            ApplyLure();
            lureTimer.Restart();
        }
    }

    private void ApplyLure()
    {
        activity.Info("Applying lure.");
        input.PressKey(AppOptions.Bot.LureKey);
        Thread.Sleep(AppOptions.Bot.LureSettleMs);
    }

    private void SettleAfterCast(int milliseconds)
    {
        var stopwatch = Stopwatch.StartNew();
        vision.ResetTracking();

        while (stopwatch.ElapsedMilliseconds < milliseconds)
        {
            vision.TryFindBobber(out _);
            Thread.Sleep(AppOptions.Watch.PollMs);
        }
    }

    private void WatchForBite(CancellationToken cancellationToken)
    {
        vision.ResetTracking();

        var bobberPosition = FindBobberWithTimeout(cancellationToken);
        if (bobberPosition.IsEmpty)
        {
            return;
        }

        audio.Begin(locator.GetWowProcess()?.Id);
        try
        {
            var deadline = Stopwatch.StartNew();

            var lastSeen = bobberPosition;
            var lostStreak = 0;

            while (enabled && !cancellationToken.IsCancellationRequested)
            {
                if (audio.PollBite())
                {
                    Loot(lastSeen);
                    return;
                }

                var found = vision.TryFindBobber(out var currentPosition)
                             && currentPosition != PixelPoint.Empty
                             && currentPosition.X != 0;

                if (found)
                {
                    lostStreak = 0;
                    lastSeen = currentPosition;

                    if (IsGhostCandidate(currentPosition))
                    {
                        vision.ResetTracking();
                        return;
                    }
                }
                else
                {
                    lostStreak++;
                    if (lostStreak >= AppOptions.Watch.MaxLostBeforeAbort)
                    {
                        return;
                    }
                }

                if (deadline.Elapsed.TotalSeconds >= AppOptions.Watch.MaxWatchSeconds)
                {
                    return;
                }

                Thread.Sleep(AppOptions.Watch.PollMs);
            }
        }
        finally
        {
            audio.Stop();
        }
    }

    private PixelPoint FindBobberWithTimeout(CancellationToken cancellationToken)
    {
        var deadline = Stopwatch.StartNew();

        while (!cancellationToken.IsCancellationRequested)
        {
            if (vision.TryFindBobber(out var position) && !position.IsEmpty)
            {
                if (IsGhostCandidate(position))
                {
                    vision.ResetTracking();
                    Thread.Sleep(AppOptions.Watch.GhostRetryMs);
                    continue;
                }

                return position;
            }

            if (deadline.Elapsed.TotalSeconds >= AppOptions.Watch.FindBobberTimeoutSeconds)
            {
                activity.Warning("Couldn't find the bobber — recasting.");
                return PixelPoint.Empty;
            }

            Thread.Sleep(AppOptions.Watch.PollMs);
        }

        return PixelPoint.Empty;
    }

    private void Loot(PixelPoint bobberPosition)
    {
        activity.Info("Bite! Reeling in…");

        Sleep(humanizer.NextReactionMs());
        input.RightClickMouse(bobberPosition);
        Sleep(humanizer.NextDelayMs(AppOptions.Humanizer.PostLootMedianMs, AppOptions.Humanizer.PostLootMinMs, AppOptions.Humanizer.PostLootMaxMs));

        lastLootedBobberPosition = bobberPosition;
        lastLootedTimestamp = Stopwatch.GetTimestamp();

        WaitForLootedBobberToVanish();

        catches++;
        activity.Info($"Caught fish #{catches}.");
    }

    private bool IsGhostCandidate(PixelPoint position)
    {
        if (lastLootedBobberPosition.IsEmpty)
        {
            return false;
        }

        var msSinceLoot = Stopwatch.GetElapsedTime(lastLootedTimestamp).TotalMilliseconds;
        if (msSinceLoot > AppOptions.GhostBobber.IgnoreWindowMs)
        {
            return false;
        }

        return position.DistanceTo(lastLootedBobberPosition) <= AppOptions.GhostBobber.IgnoreRadiusPx;
    }

    private void WaitForLootedBobberToVanish()
    {
        if (lastLootedBobberPosition.IsEmpty)
        {
            return;
        }

        var stopwatch = Stopwatch.StartNew();
        var clearStreak = 0;

        vision.ResetTracking();

        while (stopwatch.ElapsedMilliseconds < AppOptions.DespawnWait.MaxMs)
        {
            if (!enabled)
            {
                return;
            }

            var seenNear = vision.TryFindBobber(out var position) && !position.IsEmpty
                && position.DistanceTo(lastLootedBobberPosition) <= AppOptions.GhostBobber.IgnoreRadiusPx;

            if (!seenNear)
            {
                clearStreak++;
                if (clearStreak >= AppOptions.DespawnWait.RequireClearStreak)
                {
                    return;
                }
            }
            else
            {
                clearStreak = 0;
            }

            Thread.Sleep(AppOptions.DespawnWait.PollMs);
        }
    }

    private static void Sleep(int ms)
    {
        var stopwatch = Stopwatch.StartNew();
        while (stopwatch.ElapsedMilliseconds < ms)
        {
            var remaining = ms - (int)stopwatch.ElapsedMilliseconds;
            Thread.Sleep(Math.Clamp(remaining, 1, 20));
        }
    }
}
