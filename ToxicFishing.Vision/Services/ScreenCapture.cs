using OpenCvSharp;

using System.Drawing.Imaging;

namespace ToxicFishing.Vision.Services;

/// <summary>
/// Grabs frames from the primary monitor via GDI (<c>Graphics.CopyFromScreen</c>) into an OpenCV
/// <see cref="Mat"/> for the detector. This is an internal helper of the Vision module, not part of the
/// <see cref="ToxicFishing.Vision.Abstractions.IVision"/> contract.
/// </summary>
internal static class ScreenCapture
{
    /// <summary>
    /// Gets the bounds of the primary monitor, falling back to 1920×1080 if none is reported.
    /// </summary>
    public static Rectangle FullBounds =>
        Screen.PrimaryScreen?.Bounds ?? new Rectangle(0, 0, 1920, 1080);

    /// <summary>
    /// Captures a region of the primary monitor as a BGR image.
    /// </summary>
    /// <param name="region">The requested screen region; it is clamped to the monitor bounds, and an
    /// empty intersection falls back to the full screen.</param>
    /// <param name="captured">The region actually captured after clamping, used to map results back to
    /// screen coordinates.</param>
    /// <returns>A newly allocated BGR <see cref="Mat"/>; the caller owns it and must dispose it.</returns>
    public static Mat Capture(Rectangle region, out Rectangle captured)
    {
        captured = Rectangle.Intersect(region, FullBounds);
        if (captured.Width <= 0 || captured.Height <= 0)
        {
            captured = FullBounds;
        }

        var width = Math.Max(1, captured.Width);
        var height = Math.Max(1, captured.Height);

        using Bitmap bitmap = new(width, height, PixelFormat.Format32bppArgb);
        using (var graphics = Graphics.FromImage(bitmap))
        {
            graphics.CopyFromScreen(captured.Location, new System.Drawing.Point(0, 0), captured.Size, CopyPixelOperation.SourceCopy);
        }

        var bitmapData = bitmap.LockBits(
            new Rectangle(0, 0, width, height),
            ImageLockMode.ReadOnly,
            PixelFormat.Format32bppArgb);

        try
        {
            using var bgra = Mat.FromPixelData(height, width, MatType.CV_8UC4, bitmapData.Scan0, bitmapData.Stride);
            Mat bgr = new();
            Cv2.CvtColor(bgra, bgr, ColorConversionCodes.BGRA2BGR);
            return bgr;
        }
        finally
        {
            bitmap.UnlockBits(bitmapData);
        }
    }
}
