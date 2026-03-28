namespace Clay.Widgets;

/// <summary>
/// Type of HSV gradient to render.
/// </summary>
public enum HsvGradientType
{
    /// <summary>
    /// 2D saturation (horizontal) / value (vertical) gradient at a fixed hue.
    /// </summary>
    SaturationValue,

    /// <summary>
    /// Vertical hue bar (0° at top, 360° at bottom) at full saturation and value.
    /// </summary>
    HueBar
}

/// <summary>
/// Custom render data for HSV color picker gradients.
/// Passed through CustomConfig so the renderer can draw smooth gradients
/// instead of a grid of discrete colored rectangles.
/// </summary>
public class HsvGradientData
{
    /// <summary>The type of gradient to render.</summary>
    public HsvGradientType Type { get; init; }

    /// <summary>Current hue in degrees (0-360). Used by SaturationValue gradient.</summary>
    public float Hue { get; init; }
}
