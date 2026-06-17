using System.Runtime.CompilerServices;

namespace StbTextEdit;

/// <summary>
/// Pure .NET port of stb_textedit.h v1.14 by Sean Barrett.
/// All public methods are zero-allocation on the hot path.
/// </summary>
public static class TextEdit
{
    private const char Newline = '\n';

    private struct FindState
    {
        public float X, Y;
        public float Height;
        public int FirstChar, Length;
        public int PrevFirst;
    }

    /////////////////////////////////////////////////////////////////////////
    //
    //  Mouse input handling
    //

    private static int LocateCoord(ITextEditHandler handler, float x, float y)
    {
        TextEditRow r = default;
        int n = handler.Length;
        float baseY = 0;
        int i = 0;

        // Search rows to find one that straddles 'y'
        while (i < n)
        {
            handler.LayoutRow(out r, i);
            if (r.NumChars <= 0)
                return n;

            if (i == 0 && y < baseY + r.YMin)
                return 0;

            if (y < baseY + r.YMax)
                break;

            i += r.NumChars;
            baseY += r.BaselineYDelta;
        }

        // Below all text, return 'after' last character
        if (i >= n)
            return n;

        // Before beginning of line
        if (x < r.X0)
            return i;

        // Within the line
        if (x < r.X1)
        {
            float prevX = r.X0;
            for (int k = 0; k < r.NumChars; ++k)
            {
                float w = handler.GetCharWidth(i, k);
                if (x < prevX + w)
                    return x < prevX + w / 2 ? k + i : k + i + 1;
                prevX += w;
            }
        }

        // At or past end of line
        if (handler.GetChar(i + r.NumChars - 1) == Newline)
            return i + r.NumChars - 1;
        return i + r.NumChars;
    }

    /// <summary>Call on mouse down to move cursor and reset selection.</summary>
    public static void Click(ITextEditHandler handler, TextEditState state, float x, float y)
    {
        if (state.SingleLine)
        {
            handler.LayoutRow(out var r, 0);
            y = r.YMin;
        }

        state.Cursor = LocateCoord(handler, x, y);
        state.SelectStart = state.Cursor;
        state.SelectEnd = state.Cursor;
        state.HasPreferredX = false;
    }

    /// <summary>Call on mouse drag to update cursor and selection end point.</summary>
    public static void Drag(ITextEditHandler handler, TextEditState state, float x, float y)
    {
        if (state.SingleLine)
        {
            handler.LayoutRow(out var r, 0);
            y = r.YMin;
        }

        if (state.SelectStart == state.SelectEnd)
            state.SelectStart = state.Cursor;

        int p = LocateCoord(handler, x, y);
        state.Cursor = state.SelectEnd = p;
    }

    /////////////////////////////////////////////////////////////////////////
    //
    //  Character position finding
    //

    private static void FindCharPosition(out FindState find, ITextEditHandler handler, int n, bool singleLine)
    {
        TextEditRow r;
        int prevStart = 0;
        int z = handler.Length;
        int i = 0;

        if (n == z)
        {
            if (singleLine)
            {
                handler.LayoutRow(out r, 0);
                find.Y = 0;
                find.FirstChar = 0;
                find.Length = z;
                find.Height = r.YMax - r.YMin;
                find.X = r.X1;
                find.PrevFirst = 0;
            }
            else
            {
                find.Y = 0;
                find.X = 0;
                find.Height = 1;
                while (i < z)
                {
                    handler.LayoutRow(out r, i);
                    prevStart = i;
                    i += r.NumChars;
                }
                find.FirstChar = i;
                find.Length = 0;
                find.PrevFirst = prevStart;
            }
            return;
        }

        // Search rows to find the one that straddles character n
        find.Y = 0;
        find.PrevFirst = 0;
        for (;;)
        {
            handler.LayoutRow(out r, i);
            if (n < i + r.NumChars)
                break;
            prevStart = i;
            i += r.NumChars;
            find.Y += r.BaselineYDelta;
        }

        find.FirstChar = i;
        find.Length = r.NumChars;
        find.Height = r.YMax - r.YMin;
        find.PrevFirst = prevStart;

        // Scan to find xpos
        find.X = r.X0;
        for (int j = 0; i + j < n; ++j)
            find.X += handler.GetCharWidth(i, j);
    }

    /////////////////////////////////////////////////////////////////////////
    //
    //  Selection and cursor helpers
    //

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void Clamp(ITextEditHandler handler, TextEditState state)
    {
        int n = handler.Length;
        if (state.HasSelection)
        {
            if (state.SelectStart > n) state.SelectStart = n;
            if (state.SelectEnd > n) state.SelectEnd = n;
            if (state.SelectStart == state.SelectEnd)
                state.Cursor = state.SelectStart;
        }
        if (state.Cursor > n) state.Cursor = n;
    }

    private static void DeleteInternal(ITextEditHandler handler, TextEditState state, int where, int len)
    {
        MakeUndoDelete(handler, state, where, len);
        handler.DeleteChars(where, len);
        state.HasPreferredX = false;
    }

    private static void DeleteSelection(ITextEditHandler handler, TextEditState state)
    {
        Clamp(handler, state);
        if (state.HasSelection)
        {
            if (state.SelectStart < state.SelectEnd)
            {
                DeleteInternal(handler, state, state.SelectStart, state.SelectEnd - state.SelectStart);
                state.SelectEnd = state.Cursor = state.SelectStart;
            }
            else
            {
                DeleteInternal(handler, state, state.SelectEnd, state.SelectStart - state.SelectEnd);
                state.SelectStart = state.Cursor = state.SelectEnd;
            }
            state.HasPreferredX = false;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void SortSelection(TextEditState state)
    {
        if (state.SelectEnd < state.SelectStart)
            (state.SelectStart, state.SelectEnd) = (state.SelectEnd, state.SelectStart);
    }

    private static void MoveToFirst(TextEditState state)
    {
        if (state.HasSelection)
        {
            SortSelection(state);
            state.Cursor = state.SelectStart;
            state.SelectEnd = state.SelectStart;
            state.HasPreferredX = false;
        }
    }

    private static void MoveToLast(ITextEditHandler handler, TextEditState state)
    {
        if (state.HasSelection)
        {
            SortSelection(state);
            Clamp(handler, state);
            state.Cursor = state.SelectEnd;
            state.SelectStart = state.SelectEnd;
            state.HasPreferredX = false;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsWordBoundary(ITextEditHandler handler, int idx)
    {
        return idx > 0
            && char.IsWhiteSpace(handler.GetChar(idx - 1))
            && !char.IsWhiteSpace(handler.GetChar(idx));
    }

    private static int MoveWordLeft(ITextEditHandler handler, int cursor)
    {
        --cursor;
        while (cursor >= 0 && !IsWordBoundary(handler, cursor))
            --cursor;
        return cursor < 0 ? 0 : cursor;
    }

    private static int MoveWordRight(ITextEditHandler handler, int cursor)
    {
        int len = handler.Length;
        ++cursor;
        while (cursor < len && !IsWordBoundary(handler, cursor))
            ++cursor;
        return cursor > len ? len : cursor;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void PrepSelectionAtCursor(TextEditState state)
    {
        if (!state.HasSelection)
            state.SelectStart = state.SelectEnd = state.Cursor;
        else
            state.Cursor = state.SelectEnd;
    }

    private static void MoveToLineEdge(ITextEditHandler handler, TextEditState state, bool toEnd, bool shift)
    {
        Clamp(handler, state);
        if (shift)
            PrepSelectionAtCursor(state);
        else
            MoveToFirst(state);

        if (state.SingleLine)
        {
            state.Cursor = toEnd ? handler.Length : 0;
        }
        else if (toEnd)
        {
            int n = handler.Length;
            while (state.Cursor < n && handler.GetChar(state.Cursor) != Newline)
                ++state.Cursor;
        }
        else
        {
            while (state.Cursor > 0 && handler.GetChar(state.Cursor - 1) != Newline)
                --state.Cursor;
        }

        if (shift)
            state.SelectEnd = state.Cursor;
        state.HasPreferredX = false;
    }

    /////////////////////////////////////////////////////////////////////////
    //
    //  Public API: Cut / Paste
    //

    /// <summary>
    /// Deletes the current selection. Returns true if there was a selection to cut.
    /// You should copy the selection to the clipboard BEFORE calling this.
    /// </summary>
    public static bool Cut(ITextEditHandler handler, TextEditState state)
    {
        if (state.HasSelection)
        {
            DeleteSelection(handler, state);
            state.HasPreferredX = false;
            return true;
        }
        return false;
    }

    /// <summary>
    /// Pastes text at the cursor or replaces the current selection.
    /// Returns true on success.
    /// </summary>
    public static bool Paste(ITextEditHandler handler, TextEditState state, ReadOnlySpan<char> text)
    {
        Clamp(handler, state);
        DeleteSelection(handler, state);
        if (handler.InsertChars(state.Cursor, text))
        {
            MakeUndoInsert(state, state.Cursor, text.Length);
            state.Cursor += text.Length;
            state.HasPreferredX = false;
            return true;
        }
        return false;
    }

    /////////////////////////////////////////////////////////////////////////
    //
    //  Public API: Character input
    //

    /// <summary>
    /// Inserts a single character at the cursor position, handling insert mode and selection.
    /// </summary>
    public static void InputChar(ITextEditHandler handler, TextEditState state, char ch)
    {
        if (ch == Newline && state.SingleLine)
            return;

        ReadOnlySpan<char> span = new(in ch);

        if (state.InsertMode && !state.HasSelection && state.Cursor < handler.Length)
        {
            MakeUndoReplace(handler, state, state.Cursor, 1, 1);
            handler.DeleteChars(state.Cursor, 1);
            if (handler.InsertChars(state.Cursor, span))
            {
                ++state.Cursor;
                state.HasPreferredX = false;
            }
        }
        else
        {
            DeleteSelection(handler, state);
            if (handler.InsertChars(state.Cursor, span))
            {
                MakeUndoInsert(state, state.Cursor, 1);
                ++state.Cursor;
                state.HasPreferredX = false;
            }
        }
    }

    /////////////////////////////////////////////////////////////////////////
    //
    //  Public API: Key input
    //

    /// <summary>
    /// Processes a control key action. Set <paramref name="shift"/> to true to extend selection
    /// on cursor movement keys.
    /// </summary>
    public static void Key(ITextEditHandler handler, TextEditState state, TextEditKey key, bool shift = false)
    {
        switch (key)
        {
            case TextEditKey.Insert:
                state.InsertMode = !state.InsertMode;
                break;

            case TextEditKey.Undo:
                Undo(handler, state);
                state.HasPreferredX = false;
                break;

            case TextEditKey.Redo:
                Redo(handler, state);
                state.HasPreferredX = false;
                break;

            case TextEditKey.Left:
                if (shift)
                {
                    Clamp(handler, state);
                    PrepSelectionAtCursor(state);
                    if (state.SelectEnd > 0)
                        --state.SelectEnd;
                    state.Cursor = state.SelectEnd;
                    state.HasPreferredX = false;
                }
                else
                {
                    if (state.HasSelection)
                        MoveToFirst(state);
                    else if (state.Cursor > 0)
                        --state.Cursor;
                    state.HasPreferredX = false;
                }
                break;

            case TextEditKey.Right:
                if (shift)
                {
                    PrepSelectionAtCursor(state);
                    ++state.SelectEnd;
                    Clamp(handler, state);
                    state.Cursor = state.SelectEnd;
                    state.HasPreferredX = false;
                }
                else
                {
                    if (state.HasSelection)
                        MoveToLast(handler, state);
                    else
                        ++state.Cursor;
                    Clamp(handler, state);
                    state.HasPreferredX = false;
                }
                break;

            case TextEditKey.WordLeft:
                if (shift)
                {
                    if (!state.HasSelection)
                        PrepSelectionAtCursor(state);
                    state.Cursor = MoveWordLeft(handler, state.Cursor);
                    state.SelectEnd = state.Cursor;
                    Clamp(handler, state);
                }
                else
                {
                    if (state.HasSelection)
                        MoveToFirst(state);
                    else
                    {
                        state.Cursor = MoveWordLeft(handler, state.Cursor);
                        Clamp(handler, state);
                    }
                }
                break;

            case TextEditKey.WordRight:
                if (shift)
                {
                    if (!state.HasSelection)
                        PrepSelectionAtCursor(state);
                    state.Cursor = MoveWordRight(handler, state.Cursor);
                    state.SelectEnd = state.Cursor;
                    Clamp(handler, state);
                }
                else
                {
                    if (state.HasSelection)
                        MoveToLast(handler, state);
                    else
                    {
                        state.Cursor = MoveWordRight(handler, state.Cursor);
                        Clamp(handler, state);
                    }
                }
                break;

            case TextEditKey.Down:
            case TextEditKey.PageDown:
                HandleVerticalMovement(handler, state, key, shift, down: true);
                break;

            case TextEditKey.Up:
            case TextEditKey.PageUp:
                HandleVerticalMovement(handler, state, key, shift, down: false);
                break;

            case TextEditKey.Delete:
                if (state.HasSelection)
                    DeleteSelection(handler, state);
                else
                {
                    int n = handler.Length;
                    if (state.Cursor < n)
                        DeleteInternal(handler, state, state.Cursor, 1);
                }
                state.HasPreferredX = false;
                break;

            case TextEditKey.Backspace:
                if (state.HasSelection)
                    DeleteSelection(handler, state);
                else
                {
                    Clamp(handler, state);
                    if (state.Cursor > 0)
                    {
                        DeleteInternal(handler, state, state.Cursor - 1, 1);
                        --state.Cursor;
                    }
                }
                state.HasPreferredX = false;
                break;

            case TextEditKey.TextStart:
                if (shift)
                {
                    PrepSelectionAtCursor(state);
                    state.Cursor = state.SelectEnd = 0;
                    state.HasPreferredX = false;
                }
                else
                {
                    state.Cursor = state.SelectStart = state.SelectEnd = 0;
                    state.HasPreferredX = false;
                }
                break;

            case TextEditKey.TextEnd:
                if (shift)
                {
                    PrepSelectionAtCursor(state);
                    state.Cursor = state.SelectEnd = handler.Length;
                    state.HasPreferredX = false;
                }
                else
                {
                    state.Cursor = handler.Length;
                    state.SelectStart = state.SelectEnd = 0;
                    state.HasPreferredX = false;
                }
                break;

            case TextEditKey.LineStart:
                MoveToLineEdge(handler, state, toEnd: false, shift);
                break;

            case TextEditKey.LineEnd:
                MoveToLineEdge(handler, state, toEnd: true, shift);
                break;
        }
    }

    /////////////////////////////////////////////////////////////////////////
    //
    //  Up/Down movement (complex enough to be its own method)
    //

    private static void HandleVerticalMovement(
        ITextEditHandler handler, TextEditState state,
        TextEditKey key, bool shift, bool down)
    {
        bool isPage = key == TextEditKey.PageUp || key == TextEditKey.PageDown;

        // In single-line mode, up/down behave like left/right
        if (!isPage && state.SingleLine)
        {
            Key(handler, state, down ? TextEditKey.Right : TextEditKey.Left, shift);
            return;
        }

        if (shift)
            PrepSelectionAtCursor(state);
        else if (state.HasSelection)
        {
            if (down)
                MoveToLast(handler, state);
            else
                MoveToFirst(state);
        }

        Clamp(handler, state);
        FindCharPosition(out var find, handler, state.Cursor, state.SingleLine);

        int rowCount = isPage ? state.RowCountPerPage : 1;

        // Place cursor on the row starting at rowStartIndex by walking chars
        // until accumulated x exceeds goalX, then clamp and update preferred-x /
        // selection. Returns the row's char count.
        static int PlaceOnRow(ITextEditHandler handler, TextEditState state, bool shift,
            int rowStartIndex, float goalX)
        {
            state.Cursor = rowStartIndex;
            handler.LayoutRow(out var row, state.Cursor);
            float x = row.X0;
            for (int i = 0; i < row.NumChars; ++i)
            {
                x += handler.GetCharWidth(rowStartIndex, i);
                if (x > goalX)
                    break;
                ++state.Cursor;
            }
            Clamp(handler, state);
            state.HasPreferredX = true;
            state.PreferredX = goalX;
            if (shift)
                state.SelectEnd = state.Cursor;
            return row.NumChars;
        }

        if (down)
        {
            for (int j = 0; j < rowCount; ++j)
            {
                float goalX = state.HasPreferredX ? state.PreferredX : find.X;
                if (find.Length == 0)
                    break;
                int start = find.FirstChar + find.Length;
                int numChars = PlaceOnRow(handler, state, shift, start, goalX);
                find.FirstChar = start;
                find.Length = numChars;
            }
        }
        else // up
        {
            for (int j = 0; j < rowCount; ++j)
            {
                float goalX = state.HasPreferredX ? state.PreferredX : find.X;
                if (find.PrevFirst == find.FirstChar)
                    break;
                PlaceOnRow(handler, state, shift, find.PrevFirst, goalX);
                int prevScan = find.PrevFirst > 0 ? find.PrevFirst - 1 : 0;
                while (prevScan > 0 && handler.GetChar(prevScan - 1) != Newline)
                    --prevScan;
                find.FirstChar = find.PrevFirst;
                find.PrevFirst = prevScan;
            }
        }
    }

    /////////////////////////////////////////////////////////////////////////
    //
    //  Undo processing
    //

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void FlushRedo(TextEditState s)
    {
        s.RedoPoint = TextEditState.UndoStateCount;
        s.RedoCharPoint = TextEditState.UndoCharCount;
    }

    private static void DiscardUndo(TextEditState s)
    {
        if (s.UndoPoint > 0)
        {
            if (s.UndoRec[0].CharStorage >= 0)
            {
                int n = s.UndoRec[0].InsertLength;
                s.UndoCharPoint -= n;
                Array.Copy(s.UndoChar, n, s.UndoChar, 0, s.UndoCharPoint);
                for (int i = 0; i < s.UndoPoint; ++i)
                    if (s.UndoRec[i].CharStorage >= 0)
                        s.UndoRec[i].CharStorage -= n;
            }
            --s.UndoPoint;
            Array.Copy(s.UndoRec, 1, s.UndoRec, 0, s.UndoPoint);
        }
    }

    private static void DiscardRedo(TextEditState s)
    {
        int k = TextEditState.UndoStateCount - 1;
        if (s.RedoPoint <= k)
        {
            if (s.UndoRec[k].CharStorage >= 0)
            {
                int n = s.UndoRec[k].InsertLength;
                s.RedoCharPoint += n;
                Array.Copy(s.UndoChar, s.RedoCharPoint - n, s.UndoChar, s.RedoCharPoint,
                    TextEditState.UndoCharCount - s.RedoCharPoint);
                for (int i = s.RedoPoint; i < k; ++i)
                    if (s.UndoRec[i].CharStorage >= 0)
                        s.UndoRec[i].CharStorage += n;
            }
            Array.Copy(s.UndoRec, s.RedoPoint, s.UndoRec, s.RedoPoint + 1,
                TextEditState.UndoStateCount - s.RedoPoint - 1);
            ++s.RedoPoint;
        }
    }

    /// <returns>Index into UndoRec, or -1 if failed.</returns>
    private static int CreateUndoRecord(TextEditState s, int numChars)
    {
        FlushRedo(s);

        if (s.UndoPoint == TextEditState.UndoStateCount)
            DiscardUndo(s);

        if (numChars > TextEditState.UndoCharCount)
        {
            s.UndoPoint = 0;
            s.UndoCharPoint = 0;
            return -1;
        }

        while (s.UndoCharPoint + numChars > TextEditState.UndoCharCount)
            DiscardUndo(s);

        return s.UndoPoint++;
    }

    /// <returns>Char storage index in UndoChar, or -1.</returns>
    private static int CreateUndo(TextEditState s, int pos, int insertLen, int deleteLen)
    {
        int recIdx = CreateUndoRecord(s, insertLen);
        if (recIdx < 0)
            return -1;

        ref var r = ref s.UndoRec[recIdx];
        r.Where = pos;
        r.InsertLength = insertLen;
        r.DeleteLength = deleteLen;

        if (insertLen == 0)
        {
            r.CharStorage = -1;
            return -1;
        }

        r.CharStorage = s.UndoCharPoint;
        s.UndoCharPoint += insertLen;
        return r.CharStorage;
    }

    private static void Undo(ITextEditHandler handler, TextEditState state)
    {
        if (state.UndoPoint == 0)
            return;

        UndoRecord u = state.UndoRec[state.UndoPoint - 1];
        ref UndoRecord r = ref state.UndoRec[state.RedoPoint - 1];
        r.CharStorage = -1;
        r.InsertLength = u.DeleteLength;
        r.DeleteLength = u.InsertLength;
        r.Where = u.Where;

        if (u.DeleteLength > 0)
        {
            if (state.UndoCharPoint + u.DeleteLength >= TextEditState.UndoCharCount)
            {
                r.InsertLength = 0;
            }
            else
            {
                while (state.UndoCharPoint + u.DeleteLength > state.RedoCharPoint)
                {
                    if (state.RedoPoint == TextEditState.UndoStateCount)
                        return;
                    DiscardRedo(state);
                }

                r = ref state.UndoRec[state.RedoPoint - 1];
                r.CharStorage = state.RedoCharPoint - u.DeleteLength;
                state.RedoCharPoint = state.RedoCharPoint - u.DeleteLength;

                for (int i = 0; i < u.DeleteLength; ++i)
                    state.UndoChar[r.CharStorage + i] = handler.GetChar(u.Where + i);
            }

            handler.DeleteChars(u.Where, u.DeleteLength);
        }

        if (u.InsertLength > 0)
        {
            handler.InsertChars(u.Where, state.UndoChar.AsSpan(u.CharStorage, u.InsertLength));
            state.UndoCharPoint -= u.InsertLength;
        }

        state.Cursor = u.Where + u.InsertLength;
        state.UndoPoint--;
        state.RedoPoint--;
    }

    private static void Redo(ITextEditHandler handler, TextEditState state)
    {
        if (state.RedoPoint == TextEditState.UndoStateCount)
            return;

        ref UndoRecord u = ref state.UndoRec[state.UndoPoint];
        UndoRecord r = state.UndoRec[state.RedoPoint];

        u.DeleteLength = r.InsertLength;
        u.InsertLength = r.DeleteLength;
        u.Where = r.Where;
        u.CharStorage = -1;

        if (r.DeleteLength > 0)
        {
            if (state.UndoCharPoint + u.InsertLength > state.RedoCharPoint)
            {
                u.InsertLength = 0;
                u.DeleteLength = 0;
            }
            else
            {
                u.CharStorage = state.UndoCharPoint;
                state.UndoCharPoint += u.InsertLength;

                for (int i = 0; i < u.InsertLength; ++i)
                    state.UndoChar[u.CharStorage + i] = handler.GetChar(u.Where + i);
            }

            handler.DeleteChars(r.Where, r.DeleteLength);
        }

        if (r.InsertLength > 0)
        {
            handler.InsertChars(r.Where, state.UndoChar.AsSpan(r.CharStorage, r.InsertLength));
            state.RedoCharPoint += r.InsertLength;
        }

        state.Cursor = r.Where + r.InsertLength;
        state.UndoPoint++;
        state.RedoPoint++;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void MakeUndoInsert(TextEditState state, int where, int length)
    {
        CreateUndo(state, where, 0, length);
    }

    private static void MakeUndoDelete(ITextEditHandler handler, TextEditState state, int where, int length)
    {
        int charIdx = CreateUndo(state, where, length, 0);
        if (charIdx >= 0)
        {
            for (int i = 0; i < length; ++i)
                state.UndoChar[charIdx + i] = handler.GetChar(where + i);
        }
    }

    private static void MakeUndoReplace(ITextEditHandler handler, TextEditState state, int where, int oldLength, int newLength)
    {
        int charIdx = CreateUndo(state, where, oldLength, newLength);
        if (charIdx >= 0)
        {
            for (int i = 0; i < oldLength; ++i)
                state.UndoChar[charIdx + i] = handler.GetChar(where + i);
        }
    }
}
