using System.Runtime.InteropServices;

namespace Clay;

/// <summary>
/// Complete declaration of a UI element with all configuration options.
/// Used when opening a new element in the layout.
/// </summary>
public struct ElementDeclaration
{
    /// <summary>
    /// Unique identifier for this element.
    /// </summary>
    public ElementId Id;

    /// <summary>
    /// Layout configuration (sizing, padding, direction, etc.).
    /// </summary>
    public LayoutConfig Layout;

    /// <summary>
    /// Background color of the element.
    /// </summary>
    public Color BackgroundColor;

    /// <summary>
    /// Corner radius for rounded corners.
    /// </summary>
    public CornerRadius CornerRadius;

    /// <summary>
    /// Image configuration (if this is an image element).
    /// </summary>
    public ImageConfig Image;

    /// <summary>
    /// Floating configuration (for overlay/popup elements).
    /// </summary>
    public FloatingConfig Floating;

    /// <summary>
    /// Custom element configuration.
    /// </summary>
    public CustomConfig Custom;

    /// <summary>
    /// Scroll container configuration.
    /// </summary>
    public ScrollConfig Scroll;

    /// <summary>
    /// Border configuration.
    /// </summary>
    public BorderConfig Border;

    /// <summary>
    /// User-defined data (passed through to render commands).
    /// </summary>
    public nint UserData;

    /// <summary>
    /// Creates a basic container element.
    /// </summary>
    public static ElementDeclaration Container(LayoutConfig layout)
        => new() { Layout = layout };

    /// <summary>
    /// Creates a container with a background color.
    /// </summary>
    public static ElementDeclaration Box(LayoutConfig layout, Color backgroundColor)
        => new() { Layout = layout, BackgroundColor = backgroundColor };

    /// <summary>
    /// Creates a container with a background color and rounded corners.
    /// </summary>
    public static ElementDeclaration RoundedBox(LayoutConfig layout, Color backgroundColor, float cornerRadius)
        => new()
        {
            Layout = layout,
            BackgroundColor = backgroundColor,
            CornerRadius = CornerRadius.All(cornerRadius)
        };
}

/// <summary>
/// Shared configuration data that applies to multiple element types.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct SharedElementConfig
{
    public Color BackgroundColor;
    public CornerRadius CornerRadius;
    public nint UserData;

    public readonly bool HasBackgroundColor => BackgroundColor.A > 0;
    public readonly bool HasCornerRadius => CornerRadius.HasRadius;
}
