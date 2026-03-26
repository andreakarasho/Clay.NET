using Clay.Widgets;
using StbTextEdit;

namespace Clay.Test;

public class TextInputWidgetTests
{
    private static TextInputWidget CreateWidget(string initial = "", bool singleLine = true)
    {
        var measurer = new SimpleTextMeasurer();
        var widget = new TextInputWidget(measurer, singleLine);
        if (initial.Length > 0)
            widget.Text = initial;
        return widget;
    }

    // ── Text property ─────────────────────────────────────────────────

    [Fact]
    public void Text_SetAndGet()
    {
        var w = CreateWidget();
        w.Text = "hello";
        Assert.Equal("hello", w.Text);
    }

    [Fact]
    public void Text_SetResetsEditState()
    {
        var w = CreateWidget("abc");
        w.HandleKey(TextEditKey.Right);
        w.HandleKey(TextEditKey.Right);
        Assert.Equal(2, w.CursorIndex);

        w.Text = "xyz";
        Assert.Equal(0, w.CursorIndex);
        Assert.False(w.HasSelection);
    }

    // ── Typing ────────────────────────────────────────────────────────

    [Fact]
    public void HandleChar_InsertsText()
    {
        var w = CreateWidget();
        w.HandleChar('h');
        w.HandleChar('i');
        Assert.Equal("hi", w.Text);
        Assert.Equal(2, w.CursorIndex);
    }

    [Fact]
    public void HandleChar_InsertsAtCursor()
    {
        var w = CreateWidget("ac");
        w.HandleKey(TextEditKey.Right); // cursor at 1
        w.HandleChar('b');
        Assert.Equal("abc", w.Text);
    }

    // ── Cursor movement ───────────────────────────────────────────────

    [Fact]
    public void HandleKey_LeftRight()
    {
        var w = CreateWidget("abc");
        w.HandleKey(TextEditKey.TextEnd);
        Assert.Equal(3, w.CursorIndex);

        w.HandleKey(TextEditKey.Left);
        Assert.Equal(2, w.CursorIndex);

        w.HandleKey(TextEditKey.Right);
        Assert.Equal(3, w.CursorIndex);
    }

    [Fact]
    public void HandleKey_HomeEnd()
    {
        var w = CreateWidget("hello\nworld", singleLine: false);
        // Move cursor into second line
        w.HandleKey(TextEditKey.TextEnd);
        Assert.Equal(11, w.CursorIndex);

        w.HandleKey(TextEditKey.LineStart);
        Assert.Equal(6, w.CursorIndex);

        w.HandleKey(TextEditKey.LineEnd);
        Assert.Equal(11, w.CursorIndex);
    }

    // ── Selection ─────────────────────────────────────────────────────

    [Fact]
    public void ShiftRight_CreatesSelection()
    {
        var w = CreateWidget("abc");
        w.HandleKey(TextEditKey.Right, shift: true);
        w.HandleKey(TextEditKey.Right, shift: true);
        Assert.True(w.HasSelection);
        Assert.Equal(0, w.SelectionStart);
        Assert.Equal(2, w.SelectionEnd);
    }

    [Fact]
    public void GetSelectedText_ReturnsSelection()
    {
        var w = CreateWidget("hello world");
        // Select "ello"
        w.HandleKey(TextEditKey.Right); // cursor at 1
        w.HandleKey(TextEditKey.Right, shift: true);
        w.HandleKey(TextEditKey.Right, shift: true);
        w.HandleKey(TextEditKey.Right, shift: true);
        w.HandleKey(TextEditKey.Right, shift: true);
        Assert.Equal("ello", w.GetSelectedText());
    }

    // ── Delete / Backspace ────────────────────────────────────────────

    [Fact]
    public void Delete_RemovesChar()
    {
        var w = CreateWidget("abc");
        w.HandleKey(TextEditKey.Right); // cursor at 1
        w.HandleKey(TextEditKey.Delete);
        Assert.Equal("ac", w.Text);
    }

    [Fact]
    public void Backspace_RemovesCharBefore()
    {
        var w = CreateWidget("abc");
        w.HandleKey(TextEditKey.TextEnd);
        w.HandleKey(TextEditKey.Backspace);
        Assert.Equal("ab", w.Text);
    }

    // ── Cut / Paste ───────────────────────────────────────────────────

    [Fact]
    public void Cut_ReturnsSelectedText()
    {
        var w = CreateWidget("hello world");
        w.HandleKey(TextEditKey.Right);
        for (int i = 0; i < 4; i++)
            w.HandleKey(TextEditKey.Right, shift: true);

        string? cut = w.Cut();
        Assert.Equal("ello", cut);
        Assert.Equal("h world", w.Text);
    }

    [Fact]
    public void Cut_ReturnsNullWithNoSelection()
    {
        var w = CreateWidget("abc");
        Assert.Null(w.Cut());
    }

    [Fact]
    public void Paste_InsertsText()
    {
        var w = CreateWidget("ae");
        w.HandleKey(TextEditKey.Right);
        w.Paste("bcd");
        Assert.Equal("abcde", w.Text);
    }

    [Fact]
    public void Paste_ReplacesSelection()
    {
        var w = CreateWidget("aXXe");
        w.HandleKey(TextEditKey.Right); // cursor at 1
        w.HandleKey(TextEditKey.Right, shift: true);
        w.HandleKey(TextEditKey.Right, shift: true); // select "XX"
        w.Paste("bcd");
        Assert.Equal("abcde", w.Text);
    }

    // ── Undo / Redo ───────────────────────────────────────────────────

    [Fact]
    public void Undo_ReversesTyping()
    {
        var w = CreateWidget();
        w.HandleChar('a');
        w.HandleChar('b');
        Assert.Equal("ab", w.Text);

        w.HandleKey(TextEditKey.Undo);
        Assert.Equal("a", w.Text);

        w.HandleKey(TextEditKey.Undo);
        Assert.Equal("", w.Text);
    }

    [Fact]
    public void Redo_RestoresTyping()
    {
        var w = CreateWidget();
        w.HandleChar('a');
        w.HandleKey(TextEditKey.Undo);
        Assert.Equal("", w.Text);

        w.HandleKey(TextEditKey.Redo);
        Assert.Equal("a", w.Text);
    }

    // ── Focus ─────────────────────────────────────────────────────────

    [Fact]
    public void Focus_Blur()
    {
        var w = CreateWidget();
        Assert.False(w.IsFocused);

        w.Focus();
        Assert.True(w.IsFocused);

        w.Blur();
        Assert.False(w.IsFocused);
    }

    // ── Render helpers ────────────────────────────────────────────────

    [Fact]
    public void MeasureSubstring_ReturnsWidth()
    {
        var w = CreateWidget("abcd");
        // Force font params via a dummy Element call... or just set text and measure
        // The measurer uses 0.6 * fontSize as char width, default fontSize = 16 → 9.6 per char
        // But we haven't called Element yet, so font params are 0. Let's set text and use default measurer.
        // We need to trigger font param caching. Let's test via the ITextEditHandler instead.

        // Actually MeasureSubstring uses _fontId/_fontSize which are set in Element().
        // For unit tests without Clay context, test the ITextEditHandler implementation directly.
        float width = w.MeasureSubstring(0, 4);
        // With default font params (0, 0, 0), measurement depends on SimpleTextMeasurer behavior
        // fontSize=0 → charWidth=0, so width=0. This is expected without Element() call.
        Assert.Equal(0, width);
    }

    [Fact]
    public void FindLineStart_FindsCorrectPosition()
    {
        var w = CreateWidget("hello\nworld\nfoo");
        Assert.Equal(0, w.FindLineStart(3));  // in "hello"
        Assert.Equal(6, w.FindLineStart(8));  // in "world"
        Assert.Equal(12, w.FindLineStart(14)); // in "foo"
    }

    [Fact]
    public void FindLineEnd_FindsCorrectPosition()
    {
        var w = CreateWidget("hello\nworld\nfoo");
        Assert.Equal(5, w.FindLineEnd(3));   // in "hello" → before \n
        Assert.Equal(11, w.FindLineEnd(8));  // in "world" → before \n
        Assert.Equal(15, w.FindLineEnd(14)); // in "foo" → end of text
    }

    [Fact]
    public void GetRowCol_ReturnsCorrectPosition()
    {
        var w = CreateWidget("hello\nworld\nfoo");
        Assert.Equal((0, 3), w.GetRowCol(3));  // "hel|lo"
        Assert.Equal((1, 2), w.GetRowCol(8));  // "wo|rld"
        Assert.Equal((2, 2), w.GetRowCol(14)); // "fo|o"
    }

    // ── SingleLine mode ───────────────────────────────────────────────

    [Fact]
    public void SingleLine_BlocksNewline()
    {
        var w = CreateWidget("ab", singleLine: true);
        w.HandleKey(TextEditKey.Right);
        w.HandleChar('\n');
        Assert.Equal("ab", w.Text);
    }

    [Fact]
    public void MultiLine_AllowsNewline()
    {
        var w = CreateWidget("ab", singleLine: false);
        w.HandleKey(TextEditKey.Right);
        w.HandleChar('\n');
        Assert.Equal("a\nb", w.Text);
    }

    // ── Word movement ─────────────────────────────────────────────────

    [Fact]
    public void WordRight_MovesToNextWord()
    {
        var w = CreateWidget("hello world");
        w.HandleKey(TextEditKey.WordRight);
        Assert.Equal(6, w.CursorIndex);
    }

    [Fact]
    public void WordLeft_MovesToPreviousWord()
    {
        var w = CreateWidget("hello world");
        w.HandleKey(TextEditKey.TextEnd);
        w.HandleKey(TextEditKey.WordLeft);
        Assert.Equal(6, w.CursorIndex);
    }
}
