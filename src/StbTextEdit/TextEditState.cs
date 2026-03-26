namespace StbTextEdit;

/// <summary>
/// Encapsulates the full state of a text editing widget: cursor, selection, and undo history.
/// Allocate once and reuse. All per-frame operations are zero-allocation.
/// </summary>
public sealed class TextEditState
{
    public const int UndoStateCount = 99;
    public const int UndoCharCount = 999;

    // ── Public state ──────────────────────────────────────────────────

    /// <summary>Cursor position (character index).</summary>
    public int Cursor;

    /// <summary>Selection start point. Equal to <see cref="SelectEnd"/> when no selection.</summary>
    public int SelectStart;

    /// <summary>Selection end point. May be less than or greater than <see cref="SelectStart"/>.</summary>
    public int SelectEnd;

    /// <summary>Toggle insert/overwrite mode.</summary>
    public bool InsertMode;

    /// <summary>
    /// Number of rows per page, used for PageUp/PageDown.
    /// Must be set to a value greater than 0 for page navigation to work.
    /// </summary>
    public int RowCountPerPage;

    /// <summary>Whether the editor is in single-line mode.</summary>
    public bool SingleLine;

    // ── Internal state ────────────────────────────────────────────────

    internal bool CursorAtEndOfLine;
    internal bool HasPreferredX;
    internal float PreferredX;

    // ── Undo state ────────────────────────────────────────────────────

    internal readonly UndoRecord[] UndoRec = new UndoRecord[UndoStateCount];
    internal readonly char[] UndoChar = new char[UndoCharCount];
    internal int UndoPoint;
    internal int RedoPoint;
    internal int UndoCharPoint;
    internal int RedoCharPoint;

    // ── Convenience ───────────────────────────────────────────────────

    /// <summary>True when there is an active selection.</summary>
    public bool HasSelection => SelectStart != SelectEnd;

    public TextEditState(bool singleLine = false)
    {
        Initialize(singleLine);
    }

    /// <summary>Resets all state to defaults. Clears undo history.</summary>
    public void Initialize(bool singleLine = false)
    {
        Cursor = 0;
        SelectStart = 0;
        SelectEnd = 0;
        InsertMode = false;
        RowCountPerPage = 0;
        SingleLine = singleLine;
        CursorAtEndOfLine = false;
        HasPreferredX = false;
        PreferredX = 0;
        UndoPoint = 0;
        UndoCharPoint = 0;
        RedoPoint = UndoStateCount;
        RedoCharPoint = UndoCharCount;
    }
}

internal struct UndoRecord
{
    public int Where;
    public int InsertLength;
    public int DeleteLength;
    public int CharStorage;
}
