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

    /// <summary>
    /// Measures text wrapped to <paramref name="maxWidth"/>: the returned width is
    /// the widest resulting line (≤ maxWidth) and the height grows with the line
    /// count. The layout pass calls this once an element's available width is
    /// known so a text run that overflows its container reflows instead of running
    /// off-box. Default implementation ignores the constraint (no wrap) for
    /// measurers that don't support it.
    /// </summary>
    Dimensions MeasureTextWrapped(ReadOnlySpan<char> text, ushort fontId, ushort fontSize, ushort letterSpacing, float maxWidth)
        => MeasureText(text, fontId, fontSize, letterSpacing);
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
        float lineHeight = fontSize * LineHeightRatio;

        if (text.IsEmpty)
            return new Dimensions(0, lineHeight);

        float charWidth = fontSize * CharacterWidthRatio + letterSpacing;
        float maxLineWidth = 0;
        int lineCount = 1;
        int currentLineLength = 0;

        for (int i = 0; i < text.Length; i++)
        {
            if (text[i] == '\n')
            {
                float lineWidth = currentLineLength * charWidth;
                if (lineWidth > maxLineWidth)
                    maxLineWidth = lineWidth;
                currentLineLength = 0;
                lineCount++;
            }
            else
            {
                currentLineLength++;
            }
        }

        // Account for the last line
        float lastLineWidth = currentLineLength * charWidth;
        if (lastLineWidth > maxLineWidth)
            maxLineWidth = lastLineWidth;

        return new Dimensions(maxLineWidth, lineCount * lineHeight);
    }
}
