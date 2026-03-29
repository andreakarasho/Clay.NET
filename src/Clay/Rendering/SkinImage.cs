using System.Runtime.CompilerServices;

namespace Clay;

/// <summary>
/// An image that can be applied to a widget part as a skin.
/// Carries the texture reference, source dimensions, optional 9-slice insets, and tint color.
/// </summary>
public struct SkinImage
{
    /// <summary>
    /// Backend-specific image/texture handle (e.g., Raylib Texture2D).
    /// </summary>
    public object? ImageData;

    /// <summary>
    /// Width and height of the source image in pixels.
    /// </summary>
    public Dimensions SourceDimensions;

    /// <summary>
    /// 9-slice border insets. When HasSlice is true, the renderer draws the image
    /// as a 9-patch: corners are unscaled, edges stretch in one axis, center stretches in both.
    /// </summary>
    public NineSlice Slice;

    /// <summary>
    /// Color tint applied to the image. Defaults to White (no tint).
    /// </summary>
    public Color Tint;

    /// <summary>
    /// Returns true if this skin image has a texture assigned.
    /// </summary>
    public readonly bool HasImage => ImageData != null;

    /// <summary>
    /// Creates a skin image that stretches to fill its element.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static SkinImage Create(object imageData, float width, float height)
        => new()
        {
            ImageData = imageData,
            SourceDimensions = new Dimensions(width, height),
            Tint = Color.White
        };

    /// <summary>
    /// Creates a skin image with 9-slice rendering for proper stretching of bordered/rounded textures.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static SkinImage NineSliced(object imageData, float width, float height, NineSlice slice)
        => new()
        {
            ImageData = imageData,
            SourceDimensions = new Dimensions(width, height),
            Slice = slice,
            Tint = Color.White
        };
}

/// <summary>
/// A set of skin images for different interaction states (normal, hover, pressed).
/// When hover or pressed images are not set, the system falls back to normal.
/// </summary>
public struct StateImages
{
    public SkinImage Normal;
    public SkinImage Hover;
    public SkinImage Pressed;

    /// <summary>
    /// Returns true if at least the normal image is set.
    /// </summary>
    public readonly bool HasImages => Normal.HasImage;

    /// <summary>
    /// Returns the appropriate image for the current interaction state, with automatic fallback.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly SkinImage ForState(bool isPressed, bool isHovered)
    {
        if (isPressed && Pressed.HasImage) return Pressed;
        if (isHovered && Hover.HasImage) return Hover;
        return Normal;
    }

    /// <summary>
    /// Creates state images. Hover and pressed are optional — they fall back to normal when not set.
    /// </summary>
    public static StateImages Create(SkinImage normal, SkinImage? hover = null, SkinImage? pressed = null)
        => new()
        {
            Normal = normal,
            Hover = hover ?? default,
            Pressed = pressed ?? default
        };
}
