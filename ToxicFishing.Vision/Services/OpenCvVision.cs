using OpenCvSharp;

using ToxicFishing.Activity.Abstractions;
using ToxicFishing.Shared.Configuration;
using ToxicFishing.Shared.Primitives;
using ToxicFishing.Vision.Abstractions;
using DPoint = System.Drawing.Point;
using DRect = System.Drawing.Rectangle;
using Point = OpenCvSharp.Point;
using Rect = OpenCvSharp.Rect;
using Size = OpenCvSharp.Size;

namespace ToxicFishing.Vision.Services;

/// <summary>
/// Default <see cref="IVision"/>, backed by OpenCV. Combines HSV colour masking, multi-scale
/// <c>bobber.png</c> template matching, blob filtering, and tracking/centre bias to locate the bobber,
/// returning only a <see cref="PixelPoint"/>. Owns long-lived OpenCV resources (template pyramid and
/// morphology kernel) and disposes every per-frame <see cref="Mat"/>, so it is <see cref="IDisposable"/>.
/// </summary>
internal sealed class OpenCvVision : IVision, IDisposable
{
    private readonly IActivityReporter activity;

    private readonly Lock templateGate = new();
    private Mat? template;
    private readonly List<Mat> templatePyramid = [];
    private readonly Mat morphKernel;
    private bool templateReady;
    private string? pendingTemplatePath;

    private DPoint previousBobber = DPoint.Empty;

    /// <summary>
    /// Initializes the adapter, building the morphology kernel and loading the default template.
    /// </summary>
    /// <param name="activity">Sink used to report template and detection status.</param>
    public OpenCvVision(IActivityReporter activity)
    {
        this.activity = activity;

        var kernelSize = Math.Max(1, AppOptions.Vision.MorphKernel);
        morphKernel = Cv2.GetStructuringElement(MorphShapes.Rect, new Size(kernelSize, kernelSize));

        LoadTemplateCore(ResolvePath(AppOptions.Vision.TemplatePath));
    }

    /// <inheritdoc />
    public void LoadTemplate(string templatePath)
    {
        lock (templateGate)
        {
            pendingTemplatePath = ResolvePath(templatePath);
        }
    }

    private void ApplyPendingTemplate()
    {
        string? path;
        lock (templateGate)
        {
            if (pendingTemplatePath is null)
            {
                return;
            }

            path = pendingTemplatePath;
            pendingTemplatePath = null;
        }

        LoadTemplateCore(path);
        ReportTemplateStatus("updated");
        ResetTracking();
    }

    private void LoadTemplateCore(string path)
    {
        if (File.Exists(path))
        {
            var loaded = Cv2.ImRead(path, ImreadModes.Color);
            if (!loaded.Empty())
            {
                template?.Dispose();
                template = loaded;
                templateReady = true;
                BuildTemplatePyramid();
                return;
            }

            loaded.Dispose();
        }

        template?.Dispose();
        template = null;
        templateReady = false;
        DisposeTemplatePyramid();
    }

    private void ReportTemplateStatus(string state)
    {
        if (templateReady && template is not null)
        {
            activity.Info($"Bobber template {state} ({template.Width}x{template.Height}).");
        }
        else
        {
            activity.Warning("No valid bobber template — using colour-only detection.");
        }
    }

    private void BuildTemplatePyramid()
    {
        DisposeTemplatePyramid();
        if (template is null)
        {
            return;
        }

        foreach (var scale in EnumerateScales())
        {
            var width = (int)Math.Round(template.Width * scale);
            var height = (int)Math.Round(template.Height * scale);
            if (width < 4 || height < 4)
            {
                continue;
            }

            Mat scaledTemplate = new();
            Cv2.Resize(template, scaledTemplate, new Size(width, height));
            templatePyramid.Add(scaledTemplate);
        }
    }

    private void DisposeTemplatePyramid()
    {
        foreach (var scaledTemplate in templatePyramid)
        {
            scaledTemplate.Dispose();
        }

        templatePyramid.Clear();
    }

    /// <inheritdoc />
    public void Configure()
    {
        activity.Info("Detected World of Warcraft.");
        ReportTemplateStatus("ready");
    }

    /// <inheritdoc />
    public void ResetTracking()
    {
        previousBobber = DPoint.Empty;
    }

    /// <inheritdoc />
    public bool TryFindBobber(out PixelPoint screenPoint)
    {
        screenPoint = PixelPoint.Empty;

        ApplyPendingTemplate();

        var screenBounds = ScreenCapture.FullBounds;
        var hasPrevious = previousBobber != DPoint.Empty;
        var searchRadius = Math.Max(0, AppOptions.Vision.TrackingSearchRadiusPx);

        DRect region;
        if (hasPrevious)
        {
            var templateSpan = templateReady ? Math.Max(template!.Width, template.Height) : 0;
            var half = searchRadius + templateSpan + AppOptions.Vision.TemplatePaddingPx;
            region = new DRect(previousBobber.X - half, previousBobber.Y - half, half * 2, half * 2);
        }
        else
        {
            region = screenBounds;
        }

        Mat? frame = null;
        try
        {
            frame = ScreenCapture.Capture(region, out var captured);
            if (frame?.Empty() != false)
            {
                return false;
            }

            using Mat hsv = new();
            Cv2.CvtColor(frame, hsv, ColorConversionCodes.BGR2HSV);

            using var mask = BuildColorMask(hsv);

            if (Cv2.CountNonZero(mask) > AppOptions.Vision.TooMuchMaskThreshold)
            {
                activity.Warning("Too many matching pixels — adjust lighting or camera angle.");
                return true;
            }

            Cv2.FindContours(mask, out var contours, out _, RetrievalModes.External, ContourApproximationModes.ApproxSimple);
            if (contours.Length == 0)
            {
                return true;
            }

            DPoint screenCenter = new((screenBounds.X + (screenBounds.Width / 2)) - captured.X, (screenBounds.Y + (screenBounds.Height / 2)) - captured.Y);
            var diagonal = Math.Sqrt(((double)screenBounds.Width * screenBounds.Width) + ((double)screenBounds.Height * screenBounds.Height));
            var previousLocal = hasPrevious ? new DPoint(previousBobber.X - captured.X, previousBobber.Y - captured.Y) : DPoint.Empty;

            var best = SelectBest(frame, contours, screenCenter, diagonal, previousLocal, hasPrevious, searchRadius);
            if (best is null)
            {
                return true;
            }

            DPoint screenLocation = new(best.Value.Location.X + captured.X, best.Value.Location.Y + captured.Y);
            previousBobber = screenLocation;
            screenPoint = new PixelPoint(screenLocation.X, screenLocation.Y);
            return true;
        }
        catch (Exception ex)
        {
            activity.Error($"Screen scan failed: {ex.Message}");
            return false;
        }
        finally
        {
            frame?.Dispose();
        }
    }

    private Candidate? SelectBest(
        Mat frame,
        Point[][] contours,
        DPoint screenCenter,
        double diagonal,
        DPoint previousLocal,
        bool hasPrevious,
        int searchRadius)
    {
        List<Blob> blobs = [];
        foreach (var contour in contours)
        {
            var area = Cv2.ContourArea(contour);
            if (area is < AppOptions.Vision.MinBlobArea or > AppOptions.Vision.MaxBlobArea)
            {
                continue;
            }

            var boundingBox = Cv2.BoundingRect(contour);
            DPoint blobCenter = new(boundingBox.X + (boundingBox.Width / 2), boundingBox.Y + (boundingBox.Height / 2));

            var centerBias = 1.0 - Math.Min(1.0, Distance(blobCenter, screenCenter) / diagonal);
            var trackBias = hasPrevious
                ? 1.0 - Math.Min(1.0, Distance(blobCenter, previousLocal) / Math.Max(1, searchRadius))
                : 0.0;
            var preScore = (centerBias * AppOptions.Vision.CenterBiasWeight) + (trackBias * AppOptions.Vision.TrackingBiasWeight);

            blobs.Add(new Blob(contour, boundingBox, area, preScore));
        }

        if (blobs.Count == 0)
        {
            return null;
        }

        IReadOnlyList<Blob> candidates = blobs;
        if (templateReady && blobs.Count > AppOptions.Vision.MaxTemplateCandidates)
        {
            blobs.Sort(static (a, b) => b.PreScore.CompareTo(a.PreScore));
            candidates = blobs.GetRange(0, AppOptions.Vision.MaxTemplateCandidates);
        }

        Candidate? best = null;

        foreach (var blob in candidates)
        {
            var centroid = Centroid(blob.Contour, blob.BoundingBox);

            if (hasPrevious && Distance(centroid, previousLocal) > searchRadius)
            {
                continue;
            }

            var templateScore = 1.0;
            var refined = centroid;

            if (templateReady)
            {
                templateScore = MatchTemplate(frame, centroid, out refined);
                if (templateScore < AppOptions.Vision.TemplateMatchThreshold)
                {
                    continue;
                }
            }

            var centerBias = 1.0 - Math.Min(1.0, Distance(refined, screenCenter) / diagonal);
            var trackBias = hasPrevious
                ? 1.0 - Math.Min(1.0, Distance(refined, previousLocal) / Math.Max(1, searchRadius))
                : 0.0;

            var score = templateScore
                           + (centerBias * AppOptions.Vision.CenterBiasWeight)
                           + (trackBias * AppOptions.Vision.TrackingBiasWeight);

            if (best is null || score > best.Value.Score)
            {
                best = new Candidate(refined, blob.Area, score);
            }
        }

        return best;
    }

    private Mat BuildColorMask(Mat hsv)
    {
        Mat mask = new(hsv.Size(), MatType.CV_8UC1, Scalar.All(0));

        using Mat rangeMask = new();
        foreach (var range in AppOptions.Vision.ColorRanges)
        {
            Cv2.InRange(hsv,
                new Scalar(range.HMin, range.SMin, range.VMin),
                new Scalar(range.HMax, range.SMax, range.VMax),
                rangeMask);
            Cv2.BitwiseOr(mask, rangeMask, mask);
        }

        Cv2.MorphologyEx(mask, mask, MorphTypes.Open, morphKernel);
        Cv2.MorphologyEx(mask, mask, MorphTypes.Close, morphKernel);

        return mask;
    }

    private double MatchTemplate(Mat frame, DPoint centroid, out DPoint refined)
    {
        refined = centroid;
        if (!templateReady)
        {
            return 0.0;
        }

        var baseSize = Math.Max(template!.Width, template.Height);
        var half = (int)(baseSize * AppOptions.Vision.TemplateScaleMax * 0.5) + AppOptions.Vision.TemplatePaddingPx;

        var window = new Rect(centroid.X - half, centroid.Y - half, half * 2, half * 2)
            & new Rect(0, 0, frame.Width, frame.Height);

        if (window.Width < 8 || window.Height < 8)
        {
            return 0.0;
        }

        using Mat roi = new(frame, window);

        var bestScore = 0.0;
        var bestLocation = centroid;

        foreach (var scaledTemplate in templatePyramid)
        {
            var templateWidth = scaledTemplate.Width;
            var templateHeight = scaledTemplate.Height;
            if (templateWidth > window.Width || templateHeight > window.Height)
            {
                continue;
            }

            using Mat matchResult = new();
            Cv2.MatchTemplate(roi, scaledTemplate, matchResult, TemplateMatchModes.CCoeffNormed);
            Cv2.MinMaxLoc(matchResult, out _, out var maxScore, out _, out var maxLocation);

            if (maxScore > bestScore)
            {
                bestScore = maxScore;
                bestLocation = new DPoint(window.X + maxLocation.X + (templateWidth / 2), window.Y + maxLocation.Y + (templateHeight / 2));
            }
        }

        refined = bestLocation;
        return bestScore;
    }

    private static IEnumerable<double> EnumerateScales()
    {
        var steps = Math.Max(1, AppOptions.Vision.TemplateScaleSteps);
        if (steps == 1)
        {
            yield return AppOptions.Vision.TemplateScaleMin;
            yield break;
        }

        var step = (AppOptions.Vision.TemplateScaleMax - AppOptions.Vision.TemplateScaleMin) / (steps - 1);
        for (var i = 0; i < steps; i++)
        {
            yield return AppOptions.Vision.TemplateScaleMin + (step * i);
        }
    }

    private static DPoint Centroid(Point[] contour, Rect boundingBox)
    {
        var moments = Cv2.Moments(contour);
        if (moments.M00 != 0)
        {
            return new DPoint((int)(moments.M10 / moments.M00), (int)(moments.M01 / moments.M00));
        }
        return new DPoint(boundingBox.X + (boundingBox.Width / 2), boundingBox.Y + (boundingBox.Height / 2));
    }

    private static double Distance(DPoint from, DPoint to)
    {
        double dx = from.X - to.X;
        double dy = from.Y - to.Y;
        return Math.Sqrt((dx * dx) + (dy * dy));
    }

    private static string ResolvePath(string path)
        => Path.IsPathRooted(path) ? path : Path.Combine(AppContext.BaseDirectory, path);

    /// <summary>Disposes the long-lived OpenCV resources (the template, its scale pyramid, and the
    /// morphology kernel).</summary>
    public void Dispose()
    {
        lock (templateGate)
        {
            template?.Dispose();
            template = null;
            templateReady = false;
            DisposeTemplatePyramid();
            morphKernel.Dispose();
        }
    }

    private readonly record struct Candidate(DPoint Location, double Area, double Score);

    private readonly record struct Blob(Point[] Contour, Rect BoundingBox, double Area, double PreScore);
}
