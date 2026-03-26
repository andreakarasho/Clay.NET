using System.Runtime.InteropServices;

namespace Clay;

/// <summary>
/// Specifies how text wraps when it exceeds the container width.
/// </summary>
public enum TextWrapMode : byte
{
    /// <summary>
    /// Break text on whitespace characters.
    /// </summary>
    Words = 0,

    /// <summary>
    /// Break text only on newline characters.
    /// </summary>
    Newlines = 1,

    /// <summary>
    /// Do not wrap text at all.
    /// </summary>
    None = 2
}

/// <summary>
/// Specifies horizontal alignment of text within its container.
/// </summary>
public enum TextAlignment : byte
{
    /// <summary>
    /// Align text to the left.
    /// </summary>
    Left = 0,

    /// <summary>
    /// Center text horizontally.
    /// </summary>
    Center = 1,

    /// <summary>
    /// Align text to the right.
    /// </summary>
    Right = 2
}

/// <summary>
/// Configuration for text elements.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct TextConfig
{
    /// <summary>
    /// The color of the text.
    /// </summary>
    public Color TextColor;

    /// <summary>
    /// Font identifier (passed to text measurement function).
    /// </summary>
    public ushort FontId;

    /// <summary>
    /// Font size in pixels.
    /// </summary>
    public ushort FontSize;

    /// <summary>
    /// Extra horizontal spacing between characters.
    /// </summary>
    public ushort LetterSpacing;

    /// <summary>
    /// Height of each line of text (0 = auto from font metrics).
    /// </summary>
    public ushort LineHeight;

    /// <summary>
    /// How text wraps when exceeding container width.
    /// </summary>
    public TextWrapMode WrapMode;

    /// <summary>
    /// Horizontal alignment of text.
    /// </summary>
    public TextAlignment Alignment;

    /// <summary>
    /// If true, hash the actual string contents for caching instead of just the pointer.
    /// Use for dynamic text that changes frequently at the same pointer address.
    /// </summary>
    public bool HashStringContents;

    public static readonly TextConfig Default = new()
    {
        TextColor = Color.White,
        FontId = 0,
        FontSize = 16,
        LetterSpacing = 0,
        LineHeight = 0,
        WrapMode = TextWrapMode.Words,
        Alignment = TextAlignment.Left,
        HashStringContents = false
    };
}
