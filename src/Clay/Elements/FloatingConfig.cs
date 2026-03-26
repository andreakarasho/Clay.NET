using System.Numerics;
using System.Runtime.InteropServices;

namespace Clay;

/// <summary>
/// Specifies attachment points for floating elements.
/// </summary>
public enum FloatingAttachPoint : byte
{
    LeftTop = 0,
    LeftCenter = 1,
    LeftBottom = 2,
    CenterTop = 3,
    CenterCenter = 4,
    CenterBottom = 5,
    RightTop = 6,
    RightCenter = 7,
    RightBottom = 8
}

/// <summary>
/// Specifies how pointer events interact with floating elements.
/// </summary>
public enum PointerCaptureMode : byte
{
    /// <summary>
    /// Capture pointer events (default) - events don't pass through.
    /// </summary>
    Capture = 0,

    /// <summary>
    /// Pass pointer events through to elements underneath.
    /// </summary>
    Passthrough = 1
}

/// <summary>
/// Specifies what a floating element attaches to.
/// </summary>
public enum FloatingAttachTo : byte
{
    /// <summary>
    /// Floating is disabled.
    /// </summary>
    None = 0,

    /// <summary>
    /// Attach to the parent element in the hierarchy.
    /// </summary>
    Parent = 1,

    /// <summary>
    /// Attach to a specific element by ID.
    /// </summary>
    ElementWithId = 2,

    /// <summary>
    /// Attach to the root of the layout (absolute positioning).
    /// </summary>
    Root = 3
}

/// <summary>
/// Attachment point configuration for floating elements.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct FloatingAttachPoints
{
    /// <summary>
    /// The point on the floating element that attaches.
    /// </summary>
    public FloatingAttachPoint Element;

    /// <summary>
    /// The point on the parent element to attach to.
    /// </summary>
    public FloatingAttachPoint Parent;

    public static readonly FloatingAttachPoints TopLeft = new()
    {
        Element = FloatingAttachPoint.LeftTop,
        Parent = FloatingAttachPoint.LeftTop
    };

    public static readonly FloatingAttachPoints Center = new()
    {
        Element = FloatingAttachPoint.CenterCenter,
        Parent = FloatingAttachPoint.CenterCenter
    };

    public static readonly FloatingAttachPoints BottomRight = new()
    {
        Element = FloatingAttachPoint.RightBottom,
        Parent = FloatingAttachPoint.RightBottom
    };
}

/// <summary>
/// Configuration for floating (overlay) elements.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct FloatingConfig
{
    /// <summary>
    /// Offset from the attachment point.
    /// </summary>
    public Vector2 Offset;

    /// <summary>
    /// Expand the floating element's bounds without affecting children.
    /// </summary>
    public Dimensions Expand;

    /// <summary>
    /// ID of the parent element to attach to (when AttachTo = ElementWithId).
    /// </summary>
    public uint ParentId;

    /// <summary>
    /// Z-order for rendering (higher values render on top).
    /// </summary>
    public short ZIndex;

    /// <summary>
    /// How the floating element attaches to its parent.
    /// </summary>
    public FloatingAttachPoints AttachPoints;

    /// <summary>
    /// How pointer events are handled.
    /// </summary>
    public PointerCaptureMode PointerCaptureMode;

    /// <summary>
    /// What this floating element attaches to.
    /// </summary>
    public FloatingAttachTo AttachTo;

    /// <summary>
    /// Returns true if this config enables floating.
    /// </summary>
    public readonly bool IsFloating => AttachTo != FloatingAttachTo.None;

    /// <summary>
    /// Creates a floating config attached to the parent element.
    /// </summary>
    public static FloatingConfig AttachToParent(short zIndex = 0)
        => new()
        {
            AttachTo = FloatingAttachTo.Parent,
            ZIndex = zIndex
        };

    /// <summary>
    /// Creates a floating config attached to a specific element.
    /// </summary>
    public static FloatingConfig AttachToElement(uint elementId, short zIndex = 0)
        => new()
        {
            AttachTo = FloatingAttachTo.ElementWithId,
            ParentId = elementId,
            ZIndex = zIndex
        };

    /// <summary>
    /// Creates an absolutely positioned floating config attached to root.
    /// </summary>
    public static FloatingConfig Absolute(float x, float y, short zIndex = 0)
        => new()
        {
            AttachTo = FloatingAttachTo.Root,
            Offset = new Vector2(x, y),
            ZIndex = zIndex
        };
}
