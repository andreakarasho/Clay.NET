namespace Clay;

/// <summary>
/// Interface for measuring text dimensions.
/// Implement this interface to integrate your font rendering system with Clay.
/// </summary>
public interface ITextMeasurer
{
    /// <summary>
    /// Measures the dimensions of the given text with the specified configuration.
    /// </summary>
    /// <param name="text">The text to measure.</param>
    /// <param name="fontId">Font identifier.</param>
    /// <param name="fontSize">Font size in pixels.</param>
    /// <param name="letterSpacing">Extra spacing between characters.</param>
    /// <returns>The dimensions of the rendered text.</returns>
    Dimensions MeasureText(ReadOnlySpan<char> text, ushort fontId, ushort fontSize, ushort letterSpacing);
}

/// <summary>
/// A simple text measurer that estimates text size based on character count.
/// Use only for testing - implement ITextMeasurer properly for production.
/// </summary>
public class SimpleTextMeasurer : ITextMeasurer
{
    /// <summary>
    /// Average character width as a fraction of font size.
    /// </summary>
    public float CharacterWidthRatio { get; set; } = 0.6f;

    /// <summary>
    /// Line height as a fraction of font size.
    /// </summary>
    public float LineHeightRatio { get; set; } = 1.2f;

    public Dimensions MeasureText(ReadOnlySpan<char> text, ushort fontId, ushort fontSize, ushort letterSpacing)
    {
        if (text.IsEmpty)
            return new Dimensions(0, fontSize * LineHeightRatio);

        float charWidth = fontSize * CharacterWidthRatio + letterSpacing;
        float width = text.Length * charWidth;
        float height = fontSize * LineHeightRatio;

        return new Dimensions(width, height);
    }
}
