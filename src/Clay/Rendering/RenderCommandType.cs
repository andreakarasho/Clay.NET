namespace Clay;

/// <summary>
/// Specifies the type of render command to be executed.
/// </summary>
public enum RenderCommandType : byte
{
    /// <summary>
    /// No-op command (skip).
    /// </summary>
    None = 0,

    /// <summary>
    /// Draw a solid color rectangle.
    /// </summary>
    Rectangle = 1,

    /// <summary>
    /// Draw a colored border inset into the bounding box.
    /// </summary>
    Border = 2,

    /// <summary>
    /// Draw text.
    /// </summary>
    Text = 3,

    /// <summary>
    /// Draw an image.
    /// </summary>
    Image = 4,

    /// <summary>
    /// Begin scissor/clip region - only render content within the bounding box.
    /// </summary>
    ScissorStart = 5,

    /// <summary>
    /// End scissor/clip region - resume normal rendering.
    /// </summary>
    ScissorEnd = 6,

    /// <summary>
    /// Custom render command - implementation defined by the renderer.
    /// </summary>
    Custom = 7
}
