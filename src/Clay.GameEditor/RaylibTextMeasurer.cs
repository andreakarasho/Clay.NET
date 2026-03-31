using Clay;
using ZeroElectric.Vinculum;

namespace Clay.GameEditor;

public class RaylibTextMeasurer : ITextMeasurer
{
    private readonly Font[] _fonts;

    public RaylibTextMeasurer(Font[] fonts)
    {
        _fonts = fonts;
    }

    public unsafe Dimensions MeasureText(ReadOnlySpan<char> text, ushort fontId, ushort fontSize, ushort letterSpacing)
    {
        if (fontId >= _fonts.Length)
            return default;

        var font = _fonts[fontId];
        if (font.glyphs == null)
            return default;

        float factor = (float)fontSize / font.baseSize;
        float maxTextWidth = 0;
        float currentTextWidth = 0;
        float lineHeight = fontSize;
        int lineCount = 1;

        foreach (var ch in text)
        {
            if (ch == '\n')
            {
                maxTextWidth = Math.Max(maxTextWidth, currentTextWidth);
                currentTextWidth = 0;
                lineCount++;
                continue;
            }

            int index = ch - 32;
            if (index < 0 || index >= font.glyphCount)
                continue;

            if (font.glyphs[index].advanceX != 0)
                currentTextWidth += font.glyphs[index].advanceX;
            else
                currentTextWidth += font.recs[index].width + font.glyphs[index].offsetX;

            currentTextWidth += letterSpacing;
        }

        maxTextWidth = Math.Max(maxTextWidth, currentTextWidth);
        return new Dimensions(maxTextWidth * factor, lineHeight * lineCount);
    }
}
