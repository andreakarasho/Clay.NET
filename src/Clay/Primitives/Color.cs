using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Clay;

/// <summary>
/// Represents an RGBA color with float components (0-255 by convention).
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct Color : IEquatable<Color>
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

    /// <summary>
    /// Converts this color to HSV. Returns (H: 0-360, S: 0-1, V: 0-1).
    /// </summary>
    public readonly (float H, float S, float V) ToHsv()
    {
        float r = R / 255f, g = G / 255f, b = B / 255f;
        float max = Math.Max(r, Math.Max(g, b));
        float min = Math.Min(r, Math.Min(g, b));
        float delta = max - min;

        float h = 0;
        if (delta > 0.0001f)
        {
            if (max == r) h = 60f * (((g - b) / delta) % 6f);
            else if (max == g) h = 60f * (((b - r) / delta) + 2f);
            else h = 60f * (((r - g) / delta) + 4f);
        }
        if (h < 0) h += 360f;

        float s = max > 0.0001f ? delta / max : 0;
        return (h, s, max);
    }

    /// <summary>
    /// Creates a color from HSV values (H: 0-360, S: 0-1, V: 0-1) and alpha (0-255).
    /// </summary>
    public static Color FromHsv(float h, float s, float v, float a = 255f)
    {
        float c = v * s;
        float x = c * (1f - Math.Abs((h / 60f) % 2f - 1f));
        float m = v - c;

        float r, g, b;
        if (h < 60) { r = c; g = x; b = 0; }
        else if (h < 120) { r = x; g = c; b = 0; }
        else if (h < 180) { r = 0; g = c; b = x; }
        else if (h < 240) { r = 0; g = x; b = c; }
        else if (h < 300) { r = x; g = 0; b = c; }
        else { r = c; g = 0; b = x; }

        return new Color((r + m) * 255f, (g + m) * 255f, (b + m) * 255f, a);
    }

    /// <summary>
    /// Returns a copy of this color with its alpha multiplied by the given factor (RGB unchanged).
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly Color ScaleAlpha(float factor)
        => new Color(R, G, B, A * factor);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly bool Equals(Color other) => R == other.R && G == other.G && B == other.B && A == other.A;

    public override readonly bool Equals(object? obj) => obj is Color c && Equals(c);

    public override readonly int GetHashCode() => HashCode.Combine(R, G, B, A);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator ==(Color left, Color right) => left.Equals(right);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator !=(Color left, Color right) => !left.Equals(right);

    public override string ToString() => $"rgba({R}, {G}, {B}, {A})";
}
