namespace StbTextEdit;

/// <summary>
/// Provides the string and layout operations needed by the text editor.
/// Implement this interface to connect the editor to your string storage and layout system.
/// </summary>
public interface ITextEditHandler
{
    /// <summary>Returns the total length of the string in characters.</summary>
    int Length { get; }

    /// <summary>Returns the character at the given index.</summary>
    char GetChar(int index);

    /// <summary>
    /// Returns the pixel width of the character at position <paramref name="charIndex"/>
    /// within a line that starts at <paramref name="lineStartIndex"/>.
    /// This allows accounting for kerning with the previous character.
    /// </summary>
    float GetCharWidth(int lineStartIndex, int charIndex);

    /// <summary>
    /// Computes the layout of a row of characters starting at <paramref name="lineStartIndex"/>.
    /// Must set <see cref="TextEditRow.NumChars"/> to the number of characters consumed by this row
    /// (including any trailing newline).
    /// </summary>
    void LayoutRow(out TextEditRow row, int lineStartIndex);

    /// <summary>
    /// Inserts characters at the given index. Returns true on success.
    /// </summary>
    bool InsertChars(int index, ReadOnlySpan<char> chars);

    /// <summary>
    /// Deletes <paramref name="count"/> characters starting at <paramref name="index"/>.
    /// </summary>
    void DeleteChars(int index, int count);
}
