using System.Runtime.CompilerServices;
using Clay.Widgets;
using StbTextEdit;

namespace Clay;

public static partial class Clay
{
    /// <summary>
    /// Creates a text edit element. Returns true if the text was modified by user input this frame.
    /// <para>
    /// Usage:
    /// <code>
    /// // Setup (once):
    /// Clay.TextEditSetClipboard(myClipboard);
    ///
    /// // Each frame, forward raw key events:
    /// Clay.TextEditProcessKey(ClayKey.Left, ClayKeyModifiers.Shift);
    /// Clay.TextEditProcessChar('a');
    ///
    /// // Inside layout:
    /// if (Clay.TextEdit(Clay.Id("Name"), ref name, style))
    ///     Console.WriteLine("Name changed!");
    /// </code>
    /// </para>
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool TextEdit(ElementId id, ref string text, TextInputStyle style, bool singleLine = true)
    {
        return _context?.TextInput?.Draw(id, ref text, style, singleLine) ?? false;
    }

    // ── High-level input (recommended) ────────────────────────────────

    /// <summary>
    /// Call each frame for every key that is held down (use your framework's IsKeyDown).
    /// Handles first-press, repeat, and all shortcuts (Ctrl+C/X/V/Z/A, word movement, etc.).
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void TextEditKeyDown(ClayKey key, ClayKeyModifiers modifiers, float deltaTime)
    {
        _context?.TextInput?.ProcessKeyDown(key, modifiers, deltaTime);
    }

    /// <summary>Processes a typed character for the focused text edit widget.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void TextEditProcessChar(char ch)
    {
        _context?.TextInput?.ProcessChar(ch);
    }

    /// <summary>
    /// Sets the clipboard provider for text edit cut/copy/paste.
    /// </summary>
    public static void TextEditSetClipboard(IClipboard? clipboard)
    {
        if (_context?.TextInput != null)
            _context.TextInput.Clipboard = clipboard;
    }

    // ── Low-level input ───────────────────────────────────────────────

    /// <summary>Routes a StbTextEdit key directly. Prefer <see cref="TextEditProcessKey"/>.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void TextEditHandleKey(TextEditKey key, bool shift = false)
    {
        _context?.TextInput?.HandleKey(key, shift);
    }

    /// <summary>Routes a character directly. Prefer <see cref="TextEditProcessChar"/>.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void TextEditHandleChar(char ch)
    {
        _context?.TextInput?.HandleChar(ch);
    }

    // ── Clipboard / Selection ─────────────────────────────────────────

    /// <summary>Selects all text in the focused text edit widget.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void TextEditSelectAll()
    {
        _context?.TextInput?.SelectAll();
    }

    /// <summary>Cuts selected text. Returns the cut text, or null.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string? TextEditCut()
    {
        return _context?.TextInput?.Cut();
    }

    /// <summary>Pastes text into the focused text edit widget.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool TextEditPaste(ReadOnlySpan<char> text)
    {
        return _context?.TextInput?.Paste(text) ?? false;
    }

    /// <summary>Pastes text into the focused text edit widget.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool TextEditPaste(string text) => TextEditPaste(text.AsSpan());

    /// <summary>Returns the selected text in the focused text edit widget.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string TextEditGetSelectedText()
    {
        return _context?.TextInput?.GetSelectedText() ?? string.Empty;
    }

    // ── Focus ─────────────────────────────────────────────────────────

    /// <summary>Gives focus to the text edit widget with the given ID.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool TextEditFocus(ElementId id)
    {
        return _context?.TextInput?.Focus(id) ?? false;
    }

    /// <summary>Removes focus from the currently focused text edit widget.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void TextEditBlur()
    {
        _context?.TextInput?.Blur();
    }

    /// <summary>Returns true if a text edit widget currently has focus.</summary>
    public static bool TextEditHasFocus => _context?.TextInput?.HasFocus ?? false;

    /// <summary>The currently focused text edit widget, or null.</summary>
    public static TextInputWidget? TextEditFocusedWidget => _context?.TextInput?.FocusedWidget;

    /// <summary>
    /// Sets the mouse wheel scroll delta for text input widgets this frame.
    /// </summary>
    internal static void SetTextInputScrollDelta(float scrollDeltaY)
    {
        _context?.TextInput?.SetScrollDelta(scrollDeltaY);
    }
}
