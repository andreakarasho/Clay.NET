namespace Clay.Widgets;

/// <summary>
/// Provides clipboard access for text edit cut/copy/paste.
/// Implement for your platform (e.g., Raylib, SDL, WinForms).
/// </summary>
public interface IClipboard
{
    /// <summary>Gets the current clipboard text, or null if empty.</summary>
    string? GetText();

    /// <summary>Sets the clipboard text.</summary>
    void SetText(string text);
}
