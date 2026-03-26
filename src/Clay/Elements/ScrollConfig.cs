using System.Runtime.InteropServices;

namespace Clay;

/// <summary>
/// Configuration for scroll container elements.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct ScrollConfig
{
    /// <summary>
    /// Enable horizontal scrolling.
    /// </summary>
    public bool Horizontal;

    /// <summary>
    /// Enable vertical scrolling.
    /// </summary>
    public bool Vertical;

    /// <summary>
    /// Creates a scroll config with the specified axes enabled.
    /// </summary>
    public ScrollConfig(bool horizontal, bool vertical)
    {
        Horizontal = horizontal;
        Vertical = vertical;
    }

    /// <summary>
    /// Returns true if any scrolling is enabled.
    /// </summary>
    public readonly bool IsScrollable => Horizontal || Vertical;

    /// <summary>
    /// Scroll container that scrolls vertically.
    /// </summary>
    public static readonly ScrollConfig VerticalScroll = new(false, true);

    /// <summary>
    /// Scroll container that scrolls horizontally.
    /// </summary>
    public static readonly ScrollConfig HorizontalScroll = new(true, false);

    /// <summary>
    /// Scroll container that scrolls in both directions.
    /// </summary>
    public static readonly ScrollConfig BothScroll = new(true, true);
}
