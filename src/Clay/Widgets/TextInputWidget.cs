using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using StbTextEdit;

namespace Clay.Widgets;

/// <summary>
/// A text editing widget powered by StbTextEdit, integrated with Clay's layout and rendering system.
/// <para>
/// Creates a <see cref="RenderCommandType.Custom"/> render command. The renderer should check
/// <c>cmd.Custom.CustomData is TextInputWidget</c> and draw background, selection, text, and cursor.
/// </para>
/// <para>
/// Usage:
/// <code>
/// // Setup (once)
/// var textInput = new TextInputWidget(myMeasurer);
///
/// // Each frame, inside BeginLayout/EndLayout:
/// textInput.Element(Clay.Id("MyInput"), style);
///
/// // Forward keyboard events from your framework:
/// textInput.HandleKey(TextEditKey.Left, shift: false);
/// textInput.HandleChar('a');
/// </code>
/// </para>
/// </summary>
public sealed class TextInputWidget : ITextEditHandler
{
    private readonly List<char> _chars = new();
    private readonly TextEditState _editState;
    private readonly ITextMeasurer _measurer;

    private string? _textCache;
    private ushort _fontId, _fontSize, _letterSpacing;
    private float _cachedLineHeight;

    // ── Construction ──────────────────────────────────────────────────

    public TextInputWidget(ITextMeasurer measurer, bool singleLine = true)
    {
        _measurer = measurer;
        _editState = new TextEditState(singleLine);
    }

    // ── Public state (for the renderer) ───────────────────────────────

    /// <summary>Current text content. Setting replaces the entire buffer and resets edit state.</summary>
    public string Text
    {
        get => _textCache ??= new string(CollectionsMarshal.AsSpan(_chars));
        set
        {
            _chars.Clear();
            foreach (char c in value)
                _chars.Add(c);
            _textCache = value;
            _editState.Initialize(_editState.SingleLine);
        }
    }

    /// <summary>Character index of the cursor.</summary>
    public int CursorIndex => _editState.Cursor;

    /// <summary>Selection start (character index). Equal to <see cref="SelectionEnd"/> when no selection.</summary>
    public int SelectionStart => _editState.SelectStart;

    /// <summary>Selection end (character index).</summary>
    public int SelectionEnd => _editState.SelectEnd;

    /// <summary>True when there is an active text selection.</summary>
    public bool HasSelection => _editState.HasSelection;

    /// <summary>Whether this widget currently has input focus.</summary>
    public bool IsFocused { get; private set; }

    /// <summary>Whether this is a single-line input.</summary>
    public bool SingleLine => _editState.SingleLine;

    /// <summary>The style applied during the last <see cref="Element"/> call.</summary>
    public TextInputStyle CurrentStyle { get; private set; }

    // ── Keyboard input (call from your framework) ─────────────────────

    /// <summary>Processes a control key. Set <paramref name="shift"/> to extend selection.</summary>
    public void HandleKey(TextEditKey key, bool shift = false)
    {
        InvalidateTextCache();
        TextEdit.Key(this, _editState, key, shift);
    }

    /// <summary>Inserts a character at the cursor position.</summary>
    public void HandleChar(char ch)
    {
        InvalidateTextCache();
        TextEdit.InputChar(this, _editState, ch);
    }

    /// <summary>
    /// Returns the selected text and deletes it. Returns null if no selection.
    /// Copy the result to your clipboard before discarding.
    /// </summary>
    public string? Cut()
    {
        if (!_editState.HasSelection) return null;
        string text = GetSelectedText();
        InvalidateTextCache();
        TextEdit.Cut(this, _editState);
        return text;
    }

    /// <summary>Pastes text at the cursor, replacing any selection.</summary>
    public bool Paste(ReadOnlySpan<char> text)
    {
        InvalidateTextCache();
        return TextEdit.Paste(this, _editState, text);
    }

    /// <summary>Returns the currently selected text, or empty string if no selection.</summary>
    public string GetSelectedText()
    {
        if (!_editState.HasSelection) return string.Empty;
        int start = Math.Min(_editState.SelectStart, _editState.SelectEnd);
        int end = Math.Max(_editState.SelectStart, _editState.SelectEnd);
        return new string(CollectionsMarshal.AsSpan(_chars).Slice(start, end - start));
    }

    /// <summary>Gives focus to this widget.</summary>
    public void Focus() => IsFocused = true;

    /// <summary>Removes focus from this widget.</summary>
    public void Blur() => IsFocused = false;

    /// <summary>Number of rows per page for PageUp/PageDown (set before processing keys).</summary>
    public int RowCountPerPage
    {
        get => _editState.RowCountPerPage;
        set => _editState.RowCountPerPage = value;
    }

    // ── Render helpers (call from your renderer) ──────────────────────

    /// <summary>
    /// Measures the pixel width of a substring [from..to).
    /// Use to compute cursor X position and selection rectangles.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public float MeasureSubstring(int from, int to)
    {
        if (from >= to || from >= _chars.Count) return 0;
        to = Math.Min(to, _chars.Count);
        var span = CollectionsMarshal.AsSpan(_chars).Slice(from, to - from);
        return _measurer.MeasureText(span, _fontId, _fontSize, _letterSpacing).Width;
    }

    /// <summary>Computed line height in pixels.</summary>
    public float ComputedLineHeight => _cachedLineHeight;

    /// <summary>
    /// Finds the start index of the line containing the given character index.
    /// </summary>
    public int FindLineStart(int charIndex)
    {
        charIndex = Math.Min(charIndex, _chars.Count);
        int i = charIndex;
        while (i > 0 && _chars[i - 1] != '\n')
            --i;
        return i;
    }

    /// <summary>
    /// Finds the end index of the line containing the given character index (exclusive, before newline).
    /// </summary>
    public int FindLineEnd(int charIndex)
    {
        charIndex = Math.Min(charIndex, _chars.Count);
        int i = charIndex;
        while (i < _chars.Count && _chars[i] != '\n')
            ++i;
        return i;
    }

    /// <summary>
    /// Returns the (row, column) of a character index, where row is the 0-based line number.
    /// </summary>
    public (int row, int col) GetRowCol(int charIndex)
    {
        charIndex = Math.Min(charIndex, _chars.Count);
        int row = 0;
        int lastLineStart = 0;
        for (int i = 0; i < charIndex; i++)
        {
            if (_chars[i] == '\n')
            {
                row++;
                lastLineStart = i + 1;
            }
        }
        return (row, charIndex - lastLineStart);
    }

    // ── Clay integration ──────────────────────────────────────────────

    /// <summary>
    /// Creates a Clay element for this text input. Call inside BeginLayout/EndLayout.
    /// <para>
    /// Emits a <see cref="RenderCommandType.Custom"/> command with this widget as
    /// <see cref="CustomRenderData.CustomData"/>. The renderer should draw background,
    /// selection highlights, text, and cursor.
    /// </para>
    /// </summary>
    public void Element(ElementId id, TextInputStyle style)
    {
        // Cache font parameters for ITextEditHandler methods
        _fontId = style.FontId;
        _fontSize = style.FontSize;
        _letterSpacing = style.LetterSpacing;
        _cachedLineHeight = ComputeLineHeight(style);
        CurrentStyle = style;

        // Handle pointer input (uses previous frame's element data)
        HandlePointerInput(id, style);

        // Compute sizing
        Sizing sizing = style.Sizing;
        if (sizing.Height.Type == SizingType.Fit && sizing.Height.MinMax.Max == float.MaxValue)
        {
            // Auto height: compute from line height + padding
            float h = _cachedLineHeight + style.Padding.Top + style.Padding.Bottom;
            if (!_editState.SingleLine)
            {
                // For multi-line, count lines
                int lineCount = 1;
                var span = CollectionsMarshal.AsSpan(_chars);
                for (int i = 0; i < span.Length; i++)
                    if (span[i] == '\n') lineCount++;
                h = lineCount * _cachedLineHeight + style.Padding.Top + style.Padding.Bottom;
            }
            sizing.Height = SizingAxis.Fixed(h);
        }

        // Create Clay element with Custom config
        using (Clay.Element(new ElementDeclaration
        {
            Id = id,
            Layout = new LayoutConfig
            {
                Sizing = sizing,
                Padding = style.Padding,
            },
            Custom = CustomConfig.Create(this),
        }))
        {
            // No children. The Custom render handler draws text, cursor, selection.
        }
    }

    // ── ITextEditHandler implementation ───────────────────────────────

    int ITextEditHandler.Length => _chars.Count;

    char ITextEditHandler.GetChar(int index) => _chars[index];

    float ITextEditHandler.GetCharWidth(int lineStartIndex, int charIndex)
    {
        int idx = lineStartIndex + charIndex;
        if (idx >= _chars.Count) return 0;
        var span = CollectionsMarshal.AsSpan(_chars);
        if (span[idx] == '\n') return 0;
        return _measurer.MeasureText(span.Slice(idx, 1), _fontId, _fontSize, _letterSpacing).Width;
    }

    void ITextEditHandler.LayoutRow(out TextEditRow row, int lineStartIndex)
    {
        var span = CollectionsMarshal.AsSpan(_chars);
        int end = lineStartIndex;
        while (end < span.Length && span[end] != '\n')
            end++;
        bool hasNewline = end < span.Length;
        if (hasNewline) end++;

        int numChars = end - lineStartIndex;
        int textChars = hasNewline ? numChars - 1 : numChars;

        float width = 0;
        if (textChars > 0)
            width = _measurer.MeasureText(span.Slice(lineStartIndex, textChars), _fontId, _fontSize, _letterSpacing).Width;

        float lh = _cachedLineHeight;
        row = new TextEditRow
        {
            X0 = 0,
            X1 = width,
            BaselineYDelta = lh,
            YMin = 0,
            YMax = lh,
            NumChars = numChars,
        };
    }

    bool ITextEditHandler.InsertChars(int index, ReadOnlySpan<char> chars)
    {
        InvalidateTextCache();
        // Insert in correct order
        for (int i = 0; i < chars.Length; i++)
            _chars.Insert(index + i, chars[i]);
        return true;
    }

    void ITextEditHandler.DeleteChars(int index, int count)
    {
        InvalidateTextCache();
        _chars.RemoveRange(index, count);
    }

    // ── Private helpers ───────────────────────────────────────────────

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void InvalidateTextCache() => _textCache = null;

    private float ComputeLineHeight(TextInputStyle style)
    {
        if (style.LineHeight > 0) return style.LineHeight;
        return _measurer.MeasureText("Ay", style.FontId, style.FontSize, style.LetterSpacing).Height;
    }

    private void HandlePointerInput(ElementId id, TextInputStyle style)
    {
        var pointer = Clay.GetPointerState();
        bool isOver = Clay.PointerOver(id);

        if (isOver && pointer.JustPressed)
        {
            IsFocused = true;
            var elementData = Clay.GetElementData(id);
            if (elementData.Found)
            {
                float localX = pointer.Position.X - elementData.BoundingBox.X - style.Padding.Left;
                float localY = pointer.Position.Y - elementData.BoundingBox.Y - style.Padding.Top;
                TextEdit.Click(this, _editState, localX, localY);
            }
        }
        else if (IsFocused && pointer.IsPressed && !pointer.JustPressed)
        {
            // Dragging
            var elementData = Clay.GetElementData(id);
            if (elementData.Found)
            {
                float localX = pointer.Position.X - elementData.BoundingBox.X - style.Padding.Left;
                float localY = pointer.Position.Y - elementData.BoundingBox.Y - style.Padding.Top;
                TextEdit.Drag(this, _editState, localX, localY);
            }
        }
        else if (!isOver && pointer.JustPressed)
        {
            IsFocused = false;
        }
    }
}
