namespace Clay;

/// <summary>
/// Configuration for image elements.
/// </summary>
public struct ImageConfig
{
    /// <summary>
    /// Image data (passed through to renderer).
    /// </summary>
    public object? ImageData;

    /// <summary>
    /// Original dimensions of the source image (used for aspect ratio).
    /// </summary>
    public Dimensions SourceDimensions;

    /// <summary>
    /// 9-slice border insets for stretchable image rendering.
    /// When set, the renderer draws the image as a 9-patch.
    /// </summary>
    public NineSlice Slice;

    /// <summary>
    /// Tint color applied to the image. Default (0,0,0,0) means no tint.
    /// Set to Color.White for a neutral tint when using skin images.
    /// </summary>
    public Color Tint;

    /// <summary>
    /// Creates an image config with the specified data and dimensions.
    /// </summary>
    public static ImageConfig Create(object? imageData, float width, float height)
        => new()
        {
            ImageData = imageData,
            SourceDimensions = new Dimensions(width, height)
        };

    /// <summary>
    /// Returns true if this config has image data.
    /// </summary>
    public readonly bool HasImage => ImageData != null;
}
