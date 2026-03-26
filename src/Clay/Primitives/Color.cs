using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Clay;

/// <summary>
/// Represents an RGBA color with float components (0-255 by convention).
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct Color
{
    public float R;
    public float G;
    public float B;
    public float A;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Color(float r, float g, float b, float a = 255f)
    {
        R = r;
        G = g;
        B = b;
        A = a;
    }

    /// <summary>
    /// Creates a color from byte components (0-255).
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Color Rgba(byte r, byte g, byte b, byte a = 255)
        => new(r, g, b, a);

    /// <summary>
    /// Creates a color from normalized float components (0-1).
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Color FromNormalized(float r, float g, float b, float a = 1f)
        => new(r * 255f, g * 255f, b * 255f, a * 255f);

    /// <summary>
    /// Creates a color from a hex value (0xRRGGBBAA or 0xRRGGBB).
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Color FromHex(uint hex)
    {
        if (hex <= 0xFFFFFF)
            hex = (hex << 8) | 0xFF;
        return new Color(
            (hex >> 24) & 0xFF,
            (hex >> 16) & 0xFF,
            (hex >> 8) & 0xFF,
            hex & 0xFF
        );
    }

    // Common colors
    public static readonly Color Transparent = new(0, 0, 0, 0);
    public static readonly Color White = new(255, 255, 255, 255);
    public static readonly Color Black = new(0, 0, 0, 255);
    public static readonly Color Red = new(255, 0, 0, 255);
    public static readonly Color Green = new(0, 255, 0, 255);
    public static readonly Color Blue = new(0, 0, 255, 255);
    public static readonly Color Yellow = new(255, 255, 0, 255);
    public static readonly Color Cyan = new(0, 255, 255, 255);
    public static readonly Color Magenta = new(255, 0, 255, 255);
    public static readonly Color Gray = new(128, 128, 128, 255);

    /// <summary>
    /// Returns true if this color has any opacity (A > 0).
    /// </summary>
    public readonly bool IsVisible => A > 0;

    public override string ToString() => $"rgba({R}, {G}, {B}, {A})";
}
