using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Clay;

/// <summary>
/// Represents corner rounding radius for rectangles, borders, and images.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct CornerRadius
{
    public float TopLeft;
    public float TopRight;
    public float BottomLeft;
    public float BottomRight;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public CornerRadius(float topLeft, float topRight, float bottomLeft, float bottomRight)
    {
        TopLeft = topLeft;
        TopRight = topRight;
        BottomLeft = bottomLeft;
        BottomRight = bottomRight;
    }

    /// <summary>
    /// Creates a corner radius with the same value for all corners.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static CornerRadius All(float radius)
        => new(radius, radius, radius, radius);

    /// <summary>
    /// Creates a corner radius with different values for top and bottom.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static CornerRadius TopBottom(float top, float bottom)
        => new(top, top, bottom, bottom);

    /// <summary>
    /// Creates a corner radius with different values for left and right.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static CornerRadius LeftRight(float left, float right)
        => new(left, right, left, right);

    public static readonly CornerRadius Zero = new(0, 0, 0, 0);

    /// <summary>
    /// Returns true if any corner has a radius > 0.
    /// </summary>
    public readonly bool HasRadius => TopLeft > 0 || TopRight > 0 || BottomLeft > 0 || BottomRight > 0;

    public override string ToString() => $"({TopLeft}, {TopRight}, {BottomLeft}, {BottomRight})";
}
