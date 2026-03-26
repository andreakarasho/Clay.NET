using StbTextEdit;

namespace StbTextEdit.Test;

/// <summary>
/// Simple fixed-width-character text handler for testing.
/// Each character is 10px wide, each row is 20px tall.
/// </summary>
public sealed class TestHandler : ITextEditHandler
{
    private readonly List<char> _chars = new();

    public const float CharWidth = 10f;
    public const float RowHeight = 20f;

    public TestHandler(string initial = "")
    {
        _chars.AddRange(initial);
    }

    public string Text => new(_chars.ToArray());
    public int Length => _chars.Count;

    public char GetChar(int index) => _chars[index];

    public float GetCharWidth(int lineStartIndex, int charIndex) => CharWidth;

    public void LayoutRow(out TextEditRow row, int lineStartIndex)
    {
        int end = lineStartIndex;
        while (end < _chars.Count && _chars[end] != '\n')
            end++;
        if (end < _chars.Count && _chars[end] == '\n')
            end++; // include the newline in the row

        int numChars = end - lineStartIndex;
        row = new TextEditRow
        {
            X0 = 0,
            X1 = numChars * CharWidth,
            BaselineYDelta = RowHeight,
            YMin = 0f,
            YMax = RowHeight,
            NumChars = numChars,
        };
    }

    public bool InsertChars(int index, ReadOnlySpan<char> chars)
    {
        for (int i = chars.Length - 1; i >= 0; i--)
            _chars.Insert(index, chars[i]);
        return true;
    }

    public void DeleteChars(int index, int count)
    {
        _chars.RemoveRange(index, count);
    }
}
