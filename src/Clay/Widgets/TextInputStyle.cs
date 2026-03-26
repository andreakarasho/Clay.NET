namespace Clay.Widgets;

/// <summary>
/// Visual style configuration for a <see cref="TextInputWidget"/>.
/// </summary>
public struct TextInputStyle
{
    /// <summary>Background color when unfocused.</summary>
    public Color BackgroundColor;

    /// <summary>Background color when focused.</summary>
    public Color FocusedBackgroundColor;

    /// <summary>Text color.</summary>
    public Color TextColor;

    /// <summary>Cursor (caret) color.</summary>
    public Color CursorColor;

    /// <summary>Selection highlight color (should have some transparency).</summary>
    public Color SelectionColor;

    /// <summary>Corner radius for the input box.</summary>
    public CornerRadius CornerRadius;

    /// <summary>Border configuration.</summary>
    public BorderConfig Border;

    /// <summary>Inner padding around the text area.</summary>
    public Padding Padding;

    /// <summary>Font identifier passed to the text measurer.</summary>
    public ushort FontId;

    /// <summary>Font size in pixels.</summary>
    public ushort FontSize;

    /// <summary>Extra horizontal spacing between characters.</summary>
    public ushort LetterSpacing;

    /// <summary>Explicit line height (0 = auto from font metrics).</summary>
    public ushort LineHeight;

    /// <summary>
    /// Element sizing. Default: grow width, fixed height based on font.
    /// Set Height to <see cref="SizingAxis.Default"/> to auto-compute from font size.
    /// </summary>
    public Sizing Sizing;

    public static readonly TextInputStyle Default = new()
    {
        BackgroundColor = Color.Rgba(255, 255, 255),
        FocusedBackgroundColor = Color.Rgba(255, 255, 255),
        TextColor = Color.Rgba(0, 0, 0),
        CursorColor = Color.Rgba(0, 0, 0),
        SelectionColor = Color.Rgba(51, 153, 255, 100),
        CornerRadius = CornerRadius.All(2),
        Border = default,
        Padding = Padding.All(4),
        FontId = 0,
        FontSize = 16,
        LetterSpacing = 0,
        LineHeight = 0,
        Sizing = new Sizing(SizingAxis.Grow(), SizingAxis.Default),
    };
}
