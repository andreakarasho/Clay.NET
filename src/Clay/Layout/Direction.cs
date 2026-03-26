namespace Clay;

/// <summary>
/// Specifies the direction in which child elements are laid out.
/// </summary>
public enum LayoutDirection : byte
{
    /// <summary>
    /// Children are arranged horizontally from left to right.
    /// </summary>
    LeftToRight = 0,

    /// <summary>
    /// Children are arranged vertically from top to bottom.
    /// </summary>
    TopToBottom = 1
}

/// <summary>
/// Horizontal alignment of child elements.
/// </summary>
public enum AlignX : byte
{
    /// <summary>
    /// Align children to the left.
    /// </summary>
    Left = 0,

    /// <summary>
    /// Align children to the right.
    /// </summary>
    Right = 1,

    /// <summary>
    /// Center children horizontally.
    /// </summary>
    Center = 2
}

/// <summary>
/// Vertical alignment of child elements.
/// </summary>
public enum AlignY : byte
{
    /// <summary>
    /// Align children to the top.
    /// </summary>
    Top = 0,

    /// <summary>
    /// Align children to the bottom.
    /// </summary>
    Bottom = 1,

    /// <summary>
    /// Center children vertically.
    /// </summary>
    Center = 2
}

/// <summary>
/// Combined child alignment for both axes.
/// </summary>
public struct ChildAlignment
{
    public AlignX X;
    public AlignY Y;

    public ChildAlignment(AlignX x, AlignY y)
    {
        X = x;
        Y = y;
    }

    public static readonly ChildAlignment TopLeft = new(AlignX.Left, AlignY.Top);
    public static readonly ChildAlignment TopCenter = new(AlignX.Center, AlignY.Top);
    public static readonly ChildAlignment TopRight = new(AlignX.Right, AlignY.Top);
    public static readonly ChildAlignment CenterLeft = new(AlignX.Left, AlignY.Center);
    public static readonly ChildAlignment Center = new(AlignX.Center, AlignY.Center);
    public static readonly ChildAlignment CenterRight = new(AlignX.Right, AlignY.Center);
    public static readonly ChildAlignment BottomLeft = new(AlignX.Left, AlignY.Bottom);
    public static readonly ChildAlignment BottomCenter = new(AlignX.Center, AlignY.Bottom);
    public static readonly ChildAlignment BottomRight = new(AlignX.Right, AlignY.Bottom);
}
