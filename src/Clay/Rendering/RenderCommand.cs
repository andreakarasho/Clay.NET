namespace Clay;

/// <summary>
/// A command to be executed by the renderer.
/// </summary>
public struct RenderCommand
{
    /// <summary>
    /// The bounding box for this command (position and size).
    /// </summary>
    public BoundingBox BoundingBox;

    /// <summary>
    /// User-defined data.
    /// </summary>
    public nint UserData;

    /// <summary>
    /// Element ID this command belongs to.
    /// </summary>
    public uint Id;

    /// <summary>
    /// Z-order for drawing (higher values on top).
    /// </summary>
    public short ZIndex;

    /// <summary>
    /// The type of command to execute.
    /// </summary>
    public RenderCommandType CommandType;

    // Type-specific render data (only one is valid based on CommandType)
    public RectangleRenderData Rectangle;
    public TextRenderData Text;
    public ImageRenderData Image;
    public CustomRenderData Custom;
    public BorderRenderData Border;
    public ScrollRenderData Scroll;
}
