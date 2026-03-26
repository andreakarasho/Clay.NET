using StbTextEdit;

namespace StbTextEdit.Test;

public class TextEditTests
{
    private static (TestHandler handler, TextEditState state) Setup(string initial = "", bool singleLine = false)
    {
        var handler = new TestHandler(initial);
        var state = new TextEditState(singleLine);
        return (handler, state);
    }

    // ── Basic typing ──────────────────────────────────────────────────

    [Fact]
    public void InputChar_InsertsCharacterAtCursor()
    {
        var (h, s) = Setup();
        TextEdit.InputChar(h, s, 'a');
        TextEdit.InputChar(h, s, 'b');
        TextEdit.InputChar(h, s, 'c');
        Assert.Equal("abc", h.Text);
        Assert.Equal(3, s.Cursor);
    }

    [Fact]
    public void InputChar_InsertsInMiddle()
    {
        var (h, s) = Setup("ac");
        s.Cursor = 1;
        TextEdit.InputChar(h, s, 'b');
        Assert.Equal("abc", h.Text);
        Assert.Equal(2, s.Cursor);
    }

    [Fact]
    public void InputChar_NewlineBlockedInSingleLine()
    {
        var (h, s) = Setup("ab", singleLine: true);
        s.Cursor = 1;
        TextEdit.InputChar(h, s, '\n');
        Assert.Equal("ab", h.Text);
    }

    [Fact]
    public void InputChar_NewlineAllowedInMultiLine()
    {
        var (h, s) = Setup("ab");
        s.Cursor = 1;
        TextEdit.InputChar(h, s, '\n');
        Assert.Equal("a\nb", h.Text);
    }

    // ── Cursor movement ───────────────────────────────────────────────

    [Fact]
    public void Key_Left_MovesCursorLeft()
    {
        var (h, s) = Setup("abc");
        s.Cursor = 2;
        TextEdit.Key(h, s, TextEditKey.Left);
        Assert.Equal(1, s.Cursor);
    }

    [Fact]
    public void Key_Left_StopsAtZero()
    {
        var (h, s) = Setup("abc");
        s.Cursor = 0;
        TextEdit.Key(h, s, TextEditKey.Left);
        Assert.Equal(0, s.Cursor);
    }

    [Fact]
    public void Key_Right_MovesCursorRight()
    {
        var (h, s) = Setup("abc");
        s.Cursor = 1;
        TextEdit.Key(h, s, TextEditKey.Right);
        Assert.Equal(2, s.Cursor);
    }

    [Fact]
    public void Key_Right_StopsAtEnd()
    {
        var (h, s) = Setup("abc");
        s.Cursor = 3;
        TextEdit.Key(h, s, TextEditKey.Right);
        Assert.Equal(3, s.Cursor);
    }

    // ── Selection ─────────────────────────────────────────────────────

    [Fact]
    public void ShiftRight_CreatesSelection()
    {
        var (h, s) = Setup("abc");
        s.Cursor = 0;
        TextEdit.Key(h, s, TextEditKey.Right, shift: true);
        TextEdit.Key(h, s, TextEditKey.Right, shift: true);
        Assert.True(s.HasSelection);
        Assert.Equal(0, s.SelectStart);
        Assert.Equal(2, s.SelectEnd);
    }

    [Fact]
    public void ShiftLeft_ExtendsSelectionBackward()
    {
        var (h, s) = Setup("abc");
        s.Cursor = 3;
        TextEdit.Key(h, s, TextEditKey.Left, shift: true);
        Assert.Equal(3, s.SelectStart);
        Assert.Equal(2, s.SelectEnd);
    }

    [Fact]
    public void Left_CollapsesSelection()
    {
        var (h, s) = Setup("abcde");
        s.Cursor = 2;
        s.SelectStart = 1;
        s.SelectEnd = 3;
        TextEdit.Key(h, s, TextEditKey.Left);
        Assert.False(s.HasSelection);
        Assert.Equal(1, s.Cursor);
    }

    [Fact]
    public void Right_CollapsesSelectionToEnd()
    {
        var (h, s) = Setup("abcde");
        s.Cursor = 2;
        s.SelectStart = 1;
        s.SelectEnd = 3;
        TextEdit.Key(h, s, TextEditKey.Right);
        Assert.False(s.HasSelection);
        Assert.Equal(3, s.Cursor);
    }

    // ── Delete / Backspace ────────────────────────────────────────────

    [Fact]
    public void Delete_RemovesCharacterAtCursor()
    {
        var (h, s) = Setup("abc");
        s.Cursor = 1;
        TextEdit.Key(h, s, TextEditKey.Delete);
        Assert.Equal("ac", h.Text);
        Assert.Equal(1, s.Cursor);
    }

    [Fact]
    public void Backspace_RemovesCharacterBeforeCursor()
    {
        var (h, s) = Setup("abc");
        s.Cursor = 2;
        TextEdit.Key(h, s, TextEditKey.Backspace);
        Assert.Equal("ac", h.Text);
        Assert.Equal(1, s.Cursor);
    }

    [Fact]
    public void Delete_WithSelection_RemovesSelection()
    {
        var (h, s) = Setup("abcde");
        s.SelectStart = 1;
        s.SelectEnd = 3;
        s.Cursor = 3;
        TextEdit.Key(h, s, TextEditKey.Delete);
        Assert.Equal("ade", h.Text);
        Assert.Equal(1, s.Cursor);
    }

    [Fact]
    public void Backspace_WithSelection_RemovesSelection()
    {
        var (h, s) = Setup("abcde");
        s.SelectStart = 1;
        s.SelectEnd = 4;
        s.Cursor = 4;
        TextEdit.Key(h, s, TextEditKey.Backspace);
        Assert.Equal("ae", h.Text);
        Assert.Equal(1, s.Cursor);
    }

    // ── Cut / Paste ───────────────────────────────────────────────────

    [Fact]
    public void Cut_RemovesSelection()
    {
        var (h, s) = Setup("abcde");
        s.SelectStart = 1;
        s.SelectEnd = 4;
        s.Cursor = 4;
        bool result = TextEdit.Cut(h, s);
        Assert.True(result);
        Assert.Equal("ae", h.Text);
        Assert.Equal(1, s.Cursor);
    }

    [Fact]
    public void Cut_ReturnsFalseWithNoSelection()
    {
        var (h, s) = Setup("abc");
        s.Cursor = 1;
        Assert.False(TextEdit.Cut(h, s));
        Assert.Equal("abc", h.Text);
    }

    [Fact]
    public void Paste_InsertsText()
    {
        var (h, s) = Setup("ae");
        s.Cursor = 1;
        bool result = TextEdit.Paste(h, s, "bcd");
        Assert.True(result);
        Assert.Equal("abcde", h.Text);
        Assert.Equal(4, s.Cursor);
    }

    [Fact]
    public void Paste_ReplacesSelection()
    {
        var (h, s) = Setup("aXXe");
        s.SelectStart = 1;
        s.SelectEnd = 3;
        s.Cursor = 3;
        TextEdit.Paste(h, s, "bcd");
        Assert.Equal("abcde", h.Text);
        Assert.Equal(4, s.Cursor);
    }

    // ── Undo / Redo ───────────────────────────────────────────────────

    [Fact]
    public void Undo_ReversesInsert()
    {
        var (h, s) = Setup("ac");
        s.Cursor = 1;
        TextEdit.InputChar(h, s, 'b');
        Assert.Equal("abc", h.Text);

        TextEdit.Key(h, s, TextEditKey.Undo);
        Assert.Equal("ac", h.Text);
    }

    [Fact]
    public void Redo_RestoresInsert()
    {
        var (h, s) = Setup("ac");
        s.Cursor = 1;
        TextEdit.InputChar(h, s, 'b');
        TextEdit.Key(h, s, TextEditKey.Undo);
        Assert.Equal("ac", h.Text);

        TextEdit.Key(h, s, TextEditKey.Redo);
        Assert.Equal("abc", h.Text);
    }

    [Fact]
    public void Undo_ReversesDelete()
    {
        var (h, s) = Setup("abc");
        s.Cursor = 1;
        TextEdit.Key(h, s, TextEditKey.Delete);
        Assert.Equal("ac", h.Text);

        TextEdit.Key(h, s, TextEditKey.Undo);
        Assert.Equal("abc", h.Text);
    }

    [Fact]
    public void Undo_ReversesBackspace()
    {
        var (h, s) = Setup("abc");
        s.Cursor = 2;
        TextEdit.Key(h, s, TextEditKey.Backspace);
        Assert.Equal("ac", h.Text);

        TextEdit.Key(h, s, TextEditKey.Undo);
        Assert.Equal("abc", h.Text);
    }

    [Fact]
    public void MultipleUndoRedo()
    {
        var (h, s) = Setup();
        TextEdit.InputChar(h, s, 'a');
        TextEdit.InputChar(h, s, 'b');
        TextEdit.InputChar(h, s, 'c');
        Assert.Equal("abc", h.Text);

        TextEdit.Key(h, s, TextEditKey.Undo);
        Assert.Equal("ab", h.Text);
        TextEdit.Key(h, s, TextEditKey.Undo);
        Assert.Equal("a", h.Text);
        TextEdit.Key(h, s, TextEditKey.Undo);
        Assert.Equal("", h.Text);

        TextEdit.Key(h, s, TextEditKey.Redo);
        Assert.Equal("a", h.Text);
        TextEdit.Key(h, s, TextEditKey.Redo);
        Assert.Equal("ab", h.Text);
        TextEdit.Key(h, s, TextEditKey.Redo);
        Assert.Equal("abc", h.Text);
    }

    // ── Word movement ─────────────────────────────────────────────────

    [Fact]
    public void WordRight_MovesToNextWordBoundary()
    {
        var (h, s) = Setup("hello world foo");
        s.Cursor = 0;
        TextEdit.Key(h, s, TextEditKey.WordRight);
        Assert.Equal(6, s.Cursor); // after "hello "
    }

    [Fact]
    public void WordLeft_MovesToPreviousWordBoundary()
    {
        var (h, s) = Setup("hello world foo");
        s.Cursor = 8; // in "world"
        TextEdit.Key(h, s, TextEditKey.WordLeft);
        Assert.Equal(6, s.Cursor); // start of "world"
    }

    // ── Line start / end ──────────────────────────────────────────────

    [Fact]
    public void LineStart_MovesCursorToStartOfLine()
    {
        var (h, s) = Setup("hello\nworld");
        s.Cursor = 8; // in "world"
        TextEdit.Key(h, s, TextEditKey.LineStart);
        Assert.Equal(6, s.Cursor); // start of "world"
    }

    [Fact]
    public void LineEnd_MovesCursorToEndOfLine()
    {
        var (h, s) = Setup("hello\nworld");
        s.Cursor = 7; // in "world"
        TextEdit.Key(h, s, TextEditKey.LineEnd);
        Assert.Equal(11, s.Cursor); // end of "world"
    }

    // ── Text start / end ──────────────────────────────────────────────

    [Fact]
    public void TextStart_MovesCursorToBeginning()
    {
        var (h, s) = Setup("hello world");
        s.Cursor = 5;
        TextEdit.Key(h, s, TextEditKey.TextStart);
        Assert.Equal(0, s.Cursor);
    }

    [Fact]
    public void TextEnd_MovesCursorToEnd()
    {
        var (h, s) = Setup("hello world");
        s.Cursor = 0;
        TextEdit.Key(h, s, TextEditKey.TextEnd);
        Assert.Equal(11, s.Cursor);
    }

    // ── Up/Down multiline ─────────────────────────────────────────────

    [Fact]
    public void Down_MovesToNextRow()
    {
        var (h, s) = Setup("hello\nworld");
        s.Cursor = 2; // in "hello"
        TextEdit.Key(h, s, TextEditKey.Down);
        // Cursor should be in "world" row, at approximate same x position
        Assert.True(s.Cursor >= 6 && s.Cursor <= 11);
    }

    [Fact]
    public void Up_MovesToPreviousRow()
    {
        var (h, s) = Setup("hello\nworld");
        s.Cursor = 8; // in "world"
        TextEdit.Key(h, s, TextEditKey.Up);
        // Cursor should be in "hello" row
        Assert.True(s.Cursor >= 0 && s.Cursor <= 5);
    }

    [Fact]
    public void UpDown_SingleLine_BehavesAsLeftRight()
    {
        var (h, s) = Setup("hello", singleLine: true);
        s.Cursor = 3;
        TextEdit.Key(h, s, TextEditKey.Down);
        Assert.Equal(4, s.Cursor); // acts as Right
        TextEdit.Key(h, s, TextEditKey.Up);
        Assert.Equal(3, s.Cursor); // acts as Left
    }

    // ── Mouse click / drag ────────────────────────────────────────────

    [Fact]
    public void Click_SetsCursorPosition()
    {
        var (h, s) = Setup("hello");
        // Click at x=25 (between char 2 and 3 at 10px each)
        TextEdit.Click(h, s, 25f, 5f);
        Assert.True(s.Cursor >= 2 && s.Cursor <= 3);
        Assert.False(s.HasSelection);
    }

    [Fact]
    public void Drag_CreatesSelection()
    {
        var (h, s) = Setup("hello world");
        TextEdit.Click(h, s, 0f, 5f);
        TextEdit.Drag(h, s, 50f, 5f);
        Assert.True(s.HasSelection);
    }

    // ── Insert mode ───────────────────────────────────────────────────

    [Fact]
    public void InsertMode_OverwritesCharacter()
    {
        var (h, s) = Setup("abc");
        s.Cursor = 1;
        TextEdit.Key(h, s, TextEditKey.Insert); // toggle insert mode on
        TextEdit.InputChar(h, s, 'X');
        Assert.Equal("aXc", h.Text);
        Assert.Equal(2, s.Cursor);
    }

    // ── InputChar replaces selection ──────────────────────────────────

    [Fact]
    public void InputChar_ReplacesSelection()
    {
        var (h, s) = Setup("abcde");
        s.SelectStart = 1;
        s.SelectEnd = 4;
        s.Cursor = 4;
        TextEdit.InputChar(h, s, 'X');
        Assert.Equal("aXe", h.Text);
    }

    // ── Edge cases ────────────────────────────────────────────────────

    [Fact]
    public void EmptyString_OperationsDoNotCrash()
    {
        var (h, s) = Setup();
        TextEdit.Key(h, s, TextEditKey.Left);
        TextEdit.Key(h, s, TextEditKey.Right);
        TextEdit.Key(h, s, TextEditKey.Delete);
        TextEdit.Key(h, s, TextEditKey.Backspace);
        TextEdit.Key(h, s, TextEditKey.LineStart);
        TextEdit.Key(h, s, TextEditKey.LineEnd);
        TextEdit.Key(h, s, TextEditKey.TextStart);
        TextEdit.Key(h, s, TextEditKey.TextEnd);
        TextEdit.Key(h, s, TextEditKey.WordLeft);
        TextEdit.Key(h, s, TextEditKey.WordRight);
        TextEdit.Key(h, s, TextEditKey.Undo);
        TextEdit.Key(h, s, TextEditKey.Redo);
        Assert.Equal("", h.Text);
    }

    [Fact]
    public void Undo_OnFreshState_DoesNothing()
    {
        var (h, s) = Setup("abc");
        TextEdit.Key(h, s, TextEditKey.Undo);
        Assert.Equal("abc", h.Text);
    }

    [Fact]
    public void Redo_OnFreshState_DoesNothing()
    {
        var (h, s) = Setup("abc");
        TextEdit.Key(h, s, TextEditKey.Redo);
        Assert.Equal("abc", h.Text);
    }

    // ── Selection with shift + line/text keys ─────────────────────────

    [Fact]
    public void ShiftLineEnd_SelectsToEndOfLine()
    {
        var (h, s) = Setup("hello\nworld");
        s.Cursor = 2;
        TextEdit.Key(h, s, TextEditKey.LineEnd, shift: true);
        Assert.Equal(5, s.Cursor);
        Assert.True(s.HasSelection);
        Assert.Equal(2, s.SelectStart);
        Assert.Equal(5, s.SelectEnd);
    }

    [Fact]
    public void ShiftTextStart_SelectsToBeginning()
    {
        var (h, s) = Setup("hello world");
        s.Cursor = 5;
        TextEdit.Key(h, s, TextEditKey.TextStart, shift: true);
        Assert.Equal(0, s.Cursor);
        Assert.True(s.HasSelection);
    }

    [Fact]
    public void ShiftTextEnd_SelectsToEnd()
    {
        var (h, s) = Setup("hello world");
        s.Cursor = 0;
        TextEdit.Key(h, s, TextEditKey.TextEnd, shift: true);
        Assert.Equal(11, s.Cursor);
        Assert.True(s.HasSelection);
    }

    // ── Paste undo ────────────────────────────────────────────────────

    [Fact]
    public void Paste_CanBeUndone()
    {
        var (h, s) = Setup("ae");
        s.Cursor = 1;
        TextEdit.Paste(h, s, "bcd");
        Assert.Equal("abcde", h.Text);

        TextEdit.Key(h, s, TextEditKey.Undo);
        Assert.Equal("ae", h.Text);
    }

    // ── Cut undo ──────────────────────────────────────────────────────

    [Fact]
    public void Cut_CanBeUndone()
    {
        var (h, s) = Setup("abcde");
        s.SelectStart = 1;
        s.SelectEnd = 4;
        s.Cursor = 4;
        TextEdit.Cut(h, s);
        Assert.Equal("ae", h.Text);

        TextEdit.Key(h, s, TextEditKey.Undo);
        Assert.Equal("abcde", h.Text);
    }
}
