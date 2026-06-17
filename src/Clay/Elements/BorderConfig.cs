using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Clay;

/// <summary>
/// Defines border widths for each side of an element.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct BorderWidth
{
    public ushort Left;
    public ushort Right;
    public ushort Top;
    public ushort Bottom;

    /// <summary>
    /// Width of borders drawn between child elements.
    /// </summary>
    public ushort BetweenChildren;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public BorderWidth(ushort left, ushort right, ushort top, ushort bottom, ushort betweenChildren = 0)
    {
        Left = left;
        Right = right;
        Top = top;
        Bottom = bottom;
        BetweenChildren = betweenChildren;
    }

    /// <summary>
    /// Creates border width with the same value on all outer sides.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static BorderWidth All(ushort width)
        => new(width, width, width, width, 0);

    /// <summary>
    /// Creates border width on all outer sides plus between children.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static BorderWidth AllWithChildren(ushort outer, ushort betweenChildren)
        => new(outer, outer, outer, outer, betweenChildren);

    /// <summary>
    /// Creates border width between children only.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static BorderWidth Between(ushort width)
        => new(0, 0, 0, 0, width);

    public static readonly BorderWidth Zero = new(0, 0, 0, 0, 0);

    /// <summary>
    /// Returns true if any border has a width > 0.
    /// </summary>
    public readonly bool HasBorder => Left > 0 || Right > 0 || Top > 0 || Bottom > 0 || BetweenChildren > 0;
}

/// <summary>
/// Configuration for element borders.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct BorderConfig
{
    /// <summary>
    /// Color of the border.
    /// </summary>
    public Color Color;

    /// <summary>
    /// Width of each border side.
    /// </summary>
    public BorderWidth Width;

    /// <summary>
    /// Creates a border config with uniform width and color.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static BorderConfig Uniform(ushort width, Color color)
        => new()
        {
            Width = BorderWidth.All(width),
            Color = color
        };

    /// <summary>
    /// Creates a border config with the specified width and color.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static BorderConfig Create(BorderWidth width, Color color)
        => new()
        {
            Width = width,
            Color = color
        };

    /// <summary>
    /// Returns true if this config has any visible border.
    /// </summary>
    public readonly bool HasBorder => Width.HasBorder && Color.IsVisible;
}
