using System.Runtime.InteropServices;

namespace Clay;

/// <summary>
/// Render data for rectangle commands.
/// </summary>
public struct RectangleRenderData
{
    /// <summary>
    /// Background color to fill the rectangle with.
    /// </summary>
    public Color BackgroundColor;

    /// <summary>
    /// Corner rounding radius.
    /// </summary>
    public CornerRadius CornerRadius;
}

/// <summary>
/// Render data for text commands.
/// </summary>
public struct TextRenderData
{
    /// <summary>
    /// The text content.
    /// </summary>
    public string Text;

    /// <summary>
    /// Text color.
    /// </summary>
    public Color TextColor;

    /// <summary>
    /// Font identifier.
    /// </summary>
    public ushort FontId;

    /// <summary>
    /// Font size in pixels.
    /// </summary>
    public ushort FontSize;

    /// <summary>
    /// Extra spacing between characters.
    /// </summary>
    public ushort LetterSpacing;

    /// <summary>
    /// Line height in pixels.
    /// </summary>
    public ushort LineHeight;
}

/// <summary>
/// Render data for image commands.
/// </summary>
public struct ImageRenderData
{
    /// <summary>
    /// Tint/background color (default 0,0,0,0 = no tint).
    /// </summary>
    public Color BackgroundColor;

    /// <summary>
    /// Corner rounding radius.
    /// </summary>
    public CornerRadius CornerRadius;

    /// <summary>
    /// Original dimensions of the source image.
    /// </summary>
    public Dimensions SourceDimensions;

    /// <summary>
    /// The image data.
    /// </summary>
    public object? ImageData;
}

/// <summary>
/// Render data for custom commands.
/// </summary>
public struct CustomRenderData
{
    /// <summary>
    /// Background color.
    /// </summary>
    public Color BackgroundColor;

    /// <summary>
    /// Corner rounding radius.
    /// </summary>
    public CornerRadius CornerRadius;

    /// <summary>
    /// Custom data.
    /// </summary>
    public object? CustomData;
}

/// <summary>
/// Render data for border commands.
/// </summary>
public struct BorderRenderData
{
    /// <summary>
    /// Border color.
    /// </summary>
    public Color Color;

    /// <summary>
    /// Corner rounding radius.
    /// </summary>
    public CornerRadius CornerRadius;

    /// <summary>
    /// Width of each border side.
    /// </summary>
    public BorderWidth Width;
}

/// <summary>
/// Render data for scissor (clip) commands.
/// </summary>
public struct ScrollRenderData
{
    /// <summary>
    /// Whether horizontal scrolling is enabled.
    /// </summary>
    public bool Horizontal;

    /// <summary>
    /// Whether vertical scrolling is enabled.
    /// </summary>
    public bool Vertical;
}
