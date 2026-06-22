namespace ToxicFishing.Shared.Configuration;

/// <summary>
/// An inclusive HSV colour band used by the detector's colour mask.
/// </summary>
/// <param name="HMin">Minimum hue (OpenCV scale, 0–179).</param>
/// <param name="HMax">Maximum hue (OpenCV scale, 0–179).</param>
/// <param name="SMin">Minimum saturation (0–255).</param>
/// <param name="SMax">Maximum saturation (0–255).</param>
/// <param name="VMin">Minimum value/brightness (0–255).</param>
/// <param name="VMax">Maximum value/brightness (0–255).</param>
public readonly record struct HsvRange(int HMin, int HMax, int SMin, int SMax, int VMin, int VMax);
