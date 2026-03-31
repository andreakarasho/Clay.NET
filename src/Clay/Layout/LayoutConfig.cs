using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Clay;

/// <summary>
/// Configuration for element layout including sizing, padding, alignment, and direction.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct LayoutConfig
{
    /// <summary>
    /// Controls the sizing behavior of the element.
    /// </summary>
    public Sizing Sizing;

    /// <summary>
    /// Inner spacing between the element's bounds and its content.
    /// </summary>
    public Padding Padding;

    /// <summary>
    /// Gap between child elements in pixels.
    /// </summary>
    public ushort ChildGap;

    /// <summary>
    /// Alignment of child elements within this container.
    /// </summary>
    public ChildAlignment ChildAlignment;

    /// <summary>
    /// Direction in which children are laid out.
    /// </summary>
    public LayoutDirection Direction;

    /// <summary>
    /// When true, children are clipped to this element's bounding box.
    /// </summary>
    public bool ClipContent;

    public static readonly LayoutConfig Default = new()
    {
        Sizing = Sizing.Default,
        Padding = Padding.Zero,
        ChildGap = 0,
        ChildAlignment = ChildAlignment.TopLeft,
        Direction = LayoutDirection.LeftToRight
    };

    /// <summary>
    /// Creates a horizontal layout (left-to-right).
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static LayoutConfig Row(ushort gap = 0, ChildAlignment alignment = default)
        => new()
        {
            Sizing = Sizing.FitContent(),
            Direction = LayoutDirection.LeftToRight,
            ChildGap = gap,
            ChildAlignment = alignment
        };

    /// <summary>
    /// Creates a vertical layout (top-to-bottom).
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static LayoutConfig Column(ushort gap = 0, ChildAlignment alignment = default)
        => new()
        {
            Sizing = Sizing.FitContent(),
            Direction = LayoutDirection.TopToBottom,
            ChildGap = gap,
            ChildAlignment = alignment
        };

    /// <summary>
    /// Creates a layout that fills the parent horizontally.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static LayoutConfig FillRow(ushort gap = 0, ChildAlignment alignment = default)
        => new()
        {
            Sizing = Sizing.Fill(),
            Direction = LayoutDirection.LeftToRight,
            ChildGap = gap,
            ChildAlignment = alignment
        };

    /// <summary>
    /// Creates a layout that fills the parent vertically.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static LayoutConfig FillColumn(ushort gap = 0, ChildAlignment alignment = default)
        => new()
        {
            Sizing = Sizing.Fill(),
            Direction = LayoutDirection.TopToBottom,
            ChildGap = gap,
            ChildAlignment = alignment
        };
}
