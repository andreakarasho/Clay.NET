using System.Numerics;
using System.Runtime.InteropServices;

namespace Clay;

/// <summary>
/// Data about a scroll container's current state.
/// </summary>
public struct ScrollContainerData
{
    /// <summary>
    /// The current scroll position.
    /// </summary>
    public Vector2 ScrollPosition;

    /// <summary>
    /// Dimensions of the scroll container (visible area).
    /// </summary>
    public Dimensions ScrollContainerDimensions;

    /// <summary>
    /// Dimensions of the content inside the scroll container.
    /// </summary>
    public Dimensions ContentDimensions;

    /// <summary>
    /// The scroll configuration for this container.
    /// </summary>
    public ScrollConfig Config;

    /// <summary>
    /// Whether a scroll container with the requested ID was found.
    /// </summary>
    public bool Found;

    /// <summary>
    /// The maximum horizontal scroll offset.
    /// </summary>
    public readonly float MaxScrollX => Math.Max(0, ContentDimensions.Width - ScrollContainerDimensions.Width);

    /// <summary>
    /// The maximum vertical scroll offset.
    /// </summary>
    public readonly float MaxScrollY => Math.Max(0, ContentDimensions.Height - ScrollContainerDimensions.Height);

    /// <summary>
    /// Returns true if the content overflows horizontally.
    /// </summary>
    public readonly bool OverflowsX => ContentDimensions.Width > ScrollContainerDimensions.Width;

    /// <summary>
    /// Returns true if the content overflows vertically.
    /// </summary>
    public readonly bool OverflowsY => ContentDimensions.Height > ScrollContainerDimensions.Height;
}


/// <summary>
/// Bounding box and other data for a specific UI element.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct ElementData
{
    /// <summary>
    /// The bounding box of the element.
    /// </summary>
    public BoundingBox BoundingBox;

    /// <summary>
    /// Whether an element with the requested ID was found.
    /// </summary>
    public bool Found;
}
