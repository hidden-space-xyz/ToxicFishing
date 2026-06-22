namespace ToxicFishing.Shared.Primitives;

/// <summary>
/// An immutable screen coordinate in device pixels, used across every module as the platform-neutral
/// way to talk about where the bobber is without leaking a platform-specific point type.
/// </summary>
/// <param name="X">The horizontal position in screen pixels.</param>
/// <param name="Y">The vertical position in screen pixels.</param>
public readonly record struct PixelPoint(int X, int Y)
{
    /// <summary>
    /// The origin point <c>(0, 0)</c>, used as the "no detection" sentinel value.
    /// </summary>
    public static readonly PixelPoint Empty = new(0, 0);

    /// <summary>
    /// Gets a value indicating whether this point is the <see cref="Empty"/> sentinel, i.e. no
    /// bobber position is currently held.
    /// </summary>
    public bool IsEmpty => this == Empty;

    /// <summary>
    /// Computes the Euclidean distance, in pixels, between this point and another.
    /// </summary>
    /// <param name="other">The point to measure the distance to.</param>
    /// <returns>The straight-line distance between the two points, in pixels.</returns>
    public double DistanceTo(PixelPoint other)
    {
        double dx = X - other.X;
        double dy = Y - other.Y;
        return Math.Sqrt((dx * dx) + (dy * dy));
    }

    /// <summary>
    /// Returns a human-readable <c>(X, Y)</c> representation of this point.
    /// </summary>
    /// <returns>The point formatted as <c>(X, Y)</c>.</returns>
    public override string ToString() => $"({X}, {Y})";
}
