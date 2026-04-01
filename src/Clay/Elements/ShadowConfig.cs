using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Clay;

/// <summary>
/// Configuration for element drop shadows.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct ShadowConfig
{
    /// <summary>
    /// Shadow color (typically semi-transparent black).
    /// </summary>
    public Color Color;

    /// <summary>
    /// Horizontal offset of the shadow (positive = right).
    /// </summary>
    public float OffsetX;

    /// <summary>
    /// Vertical offset of the shadow (positive = down).
    /// </summary>
    public float OffsetY;

    /// <summary>
    /// Blur radius — larger values produce softer shadows.
    /// </summary>
    public float BlurRadius;

    /// <summary>
    /// Spread radius — expands (positive) or contracts (negative) the shadow shape.
    /// </summary>
    public float SpreadRadius;

    /// <summary>
    /// Corner radius for the shadow shape. When default (zero), inherits from the element's corner radius.
    /// </summary>
    public CornerRadius CornerRadius;

    /// <summary>
    /// Returns true if this config describes a visible shadow.
    /// </summary>
    public readonly bool HasShadow => Color.IsVisible;

    /// <summary>
    /// Creates a simple drop shadow.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ShadowConfig Drop(float offsetX, float offsetY, float blurRadius, Color color)
        => new()
        {
            Color = color,
            OffsetX = offsetX,
            OffsetY = offsetY,
            BlurRadius = blurRadius
        };

    /// <summary>
    /// Creates a uniform shadow (equal offset in both directions).
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ShadowConfig Uniform(float offset, float blurRadius, Color color)
        => new()
        {
            Color = color,
            OffsetX = offset,
            OffsetY = offset,
            BlurRadius = blurRadius
        };

    /// <summary>
    /// Creates an ambient shadow with no offset (glow effect).
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ShadowConfig Ambient(float blurRadius, Color color)
        => new()
        {
            Color = color,
            BlurRadius = blurRadius
        };

    /// <summary>
    /// Creates a full shadow configuration.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ShadowConfig Create(float offsetX, float offsetY, float blurRadius, float spreadRadius, Color color)
        => new()
        {
            Color = color,
            OffsetX = offsetX,
            OffsetY = offsetY,
            BlurRadius = blurRadius,
            SpreadRadius = spreadRadius
        };
}
