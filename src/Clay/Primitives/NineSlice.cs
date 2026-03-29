using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Clay;

/// <summary>
/// Defines the border insets for 9-slice (9-patch) image rendering.
/// The image is divided into a 3x3 grid: 4 corners (unscaled), 4 edges (scaled in one axis),
/// and 1 center (scaled in both axes).
/// Values are in source-image pixels.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct NineSlice
{
    public float Top;
    public float Right;
    public float Bottom;
    public float Left;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public NineSlice(float top, float right, float bottom, float left)
    {
        Top = top;
        Right = right;
        Bottom = bottom;
        Left = left;
    }

    /// <summary>
    /// Same inset on all four sides.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static NineSlice Uniform(float inset)
        => new(inset, inset, inset, inset);

    /// <summary>
    /// Symmetric horizontal and vertical insets.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static NineSlice Symmetric(float horizontal, float vertical)
        => new(vertical, horizontal, vertical, horizontal);

    /// <summary>
    /// Individual insets per side.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static NineSlice TRBL(float top, float right, float bottom, float left)
        => new(top, right, bottom, left);

    /// <summary>
    /// Returns true if any slice inset is defined (i.e., this should be rendered as 9-slice).
    /// </summary>
    public readonly bool HasSlice => Top > 0 || Right > 0 || Bottom > 0 || Left > 0;

    public static readonly NineSlice Zero = default;
}
