using StbTextEdit;

namespace Clay.Widgets;

/// <summary>
/// Immediate-mode text input manager. Manages widget instances, focus, and keyboard routing.
/// Create one per UI context (or per screen/window).
/// <para>
/// Usage:
/// <code>
/// var textInput = new TextInput(myMeasurer);
///
/// // Once per frame, before Clay.BeginLayout():
/// textInput.BeginFrame();
///
/// // Forward keyboard events:
/// foreach (var key in pressedKeys)
///     textInput.HandleKey(key, isShiftDown);
/// foreach (var ch in typedChars)
///     textInput.HandleChar(ch);
///
/// // Inside layout (between BeginLayout/EndLayout):
/// if (textInput.Draw(Clay.Id("Name"), ref nameText, myStyle))
///     Console.WriteLine("Name changed!");
/// </code>
/// </para>
/// </summary>
internal sealed class TextInput
{
    private readonly Dictionary<uint, TextInputWidget> _widgets = new();
    private readonly HashSet<uint> _activeIds = new();
    private readonly List<uint> _toRemove = new();
    private readonly ITextMeasurer _measurer;
    private TextInputWidget? _focused;
    private uint _focusedId;
    private bool _focusedDirty;

    // Key repeat state
    private ClayKey? _repeatKey;
    private ClayKeyModifiers _repeatMods;
    private float _repeatTimer;
    private bool _initialRepeatDone;
    private bool _repeatKeyActiveThisFrame;
    private float _scrollDeltaY;

    private const float RepeatDelay = 0.4f;
    private const float RepeatRate = 0.035f;

    /// <summary>
    /// Clipboard provider for cut/copy/paste. Set this to enable Ctrl+C/X/V.
    /// </summary>
    public IClipboard? Clipboard { get; set; }

    public TextInput(ITextMeasurer measurer)
    {
        _measurer = measurer;
    }

    /// <summary>The currently focused text input widget, or null.</summary>
    public TextInputWidget? FocusedWidget => _focused;

    /// <summary>Returns true if any text input currently has focus.</summary>
    public bool HasFocus => _focused != null;

    /// <summary>The element ID of the currently focused text input (0 if none).</summary>
    public uint FocusedElementId => _focusedId;

    /// <summary>
    /// Sets the mouse wheel scroll delta for the current frame.
    /// Positive = scroll up. Applied to hovered multiline text inputs.
    /// </summary>
    public void SetScrollDelta(float scrollDeltaY) => _scrollDeltaY = scrollDeltaY;

    /// <summary>
    /// Call once per frame before processing input or layout.
    /// Removes widgets that were not drawn last frame.
    /// </summary>
    public void BeginFrame()
    {
        _toRemove.Clear();
        foreach (var (id, _) in _widgets)
            if (!_activeIds.Contains(id))
                _toRemove.Add(id);
        foreach (var id in _toRemove)
        {
            if (_focusedId == id)
            {
                _focused = null;
                _focusedId = 0;
                _focusedDirty = false;
            }
            _widgets.Remove(id);
        }
        _activeIds.Clear();

        // If repeat key wasn't reported as held last frame, stop repeating
        if (!_repeatKeyActiveThisFrame)
            _repeatKey = null;
        _repeatKeyActiveThisFrame = false;
        _scrollDeltaY = 0;
    }

    /// <summary>
    /// Creates a text input element inside the current Clay layout.
    /// Returns true if the text was modified this frame (by keyboard input).
    /// </summary>
    /// <param name="id">Unique element ID for this text input.</param>
    /// <param name="text">
    /// Reference to the caller's text string. Updated when the widget modifies the text.
    /// If the caller changes this value between frames, the widget syncs to it.
    /// </param>
    /// <param name="style">Visual style for the text input.</param>
    /// <param name="singleLine">If true, newlines are blocked and up/down act as left/right.</param>
    /// <returns>True if <paramref name="text"/> was updated by user input this frame.</returns>
    public bool Draw(ElementId id, ref string text, TextInputStyle style, bool singleLine = true)
    {
        // Get or create widget
        if (!_widgets.TryGetValue(id.Id, out var widget))
        {
            widget = new TextInputWidget(_measurer, singleLine);
            widget.Text = text;
            _widgets[id.Id] = widget;
        }

        _activeIds.Add(id.Id);

        // Detect changes
        bool changed = false;

        if (widget == _focused && _focusedDirty)
        {
            // Widget text was modified by keyboard/clipboard input since last Draw
            changed = true;
        }
        else if (!string.Equals(text, widget.Text, StringComparison.Ordinal))
        {
            // Caller changed text externally (programmatic update)
            widget.Text = text;
        }

        // Create the Clay element (handles mouse click/drag/focus/scroll).
        // Pass scroll delta only to multiline widgets — Element() checks focus/hover
        // before applying. Single-line inputs don't scroll vertically.
        float scrollForWidget = widget.SingleLine ? 0 : _scrollDeltaY;
        widget.Element(id, style, scrollForWidget);

        // Sync focus tracking with widget's own focus state
        if (widget.IsFocused)
        {
            if (_focused != widget)
            {
                _focused?.Blur();
                _focused = widget;
                _focusedId = id.Id;
            }
        }
        else if (_focused == widget)
        {
            _focused = null;
            _focusedId = 0;
            _focusedDirty = false;
        }

        // Sync text back to caller if changed
        if (changed)
        {
            text = widget.Text;
            _focusedDirty = false;
        }

        return changed;
    }

    // ── High-level input (recommended) ───────────────────────────────

    /// <summary>
    /// Call each frame for every key that is currently held down.
    /// Handles first-press (immediate execute) and repeat (after delay) automatically.
    /// Includes all shortcuts: Ctrl+C/X/V, Ctrl+Z, Ctrl+A, Ctrl+Arrow, etc.
    /// <para>Set <see cref="Clipboard"/> to enable Ctrl+C/X/V.</para>
    /// </summary>
    public void ProcessKeyDown(ClayKey key, ClayKeyModifiers modifiers, float deltaTime)
    {
        if (_focused == null) return;
        if (_repeatKeyActiveThisFrame) return; // one key per frame

        _repeatKeyActiveThisFrame = true;

        if (_repeatKey == key && _repeatMods == modifiers)
        {
            // Same key held — drive repeat
            _repeatTimer += deltaTime;
            if (!_initialRepeatDone)
            {
                if (_repeatTimer >= RepeatDelay)
                {
                    _initialRepeatDone = true;
                    _repeatTimer = 0;
                    ExecuteKey(key, modifiers);
                }
            }
            else
            {
                while (_repeatTimer >= RepeatRate)
                {
                    _repeatTimer -= RepeatRate;
                    ExecuteKey(key, modifiers);
                }
            }
        }
        else
        {
            // New key — execute immediately, start repeat
            ExecuteKey(key, modifiers);
            _repeatKey = key;
            _repeatMods = modifiers;
            _repeatTimer = 0;
            _initialRepeatDone = false;
        }
    }

    private void ExecuteKey(ClayKey key, ClayKeyModifiers modifiers)
    {
        if (_focused == null) return;

        bool shift = (modifiers & ClayKeyModifiers.Shift) != 0;
        bool ctrl = (modifiers & ClayKeyModifiers.Ctrl) != 0;

        if (ctrl)
        {
            switch (key)
            {
                case ClayKey.A: SelectAll(); return;
                case ClayKey.C:
                    var sel = GetSelectedText();
                    if (sel.Length > 0) Clipboard?.SetText(sel);
                    return;
                case ClayKey.X:
                    var cut = Cut();
                    if (cut != null) Clipboard?.SetText(cut);
                    return;
                case ClayKey.V:
                    var clip = Clipboard?.GetText();
                    if (!string.IsNullOrEmpty(clip)) Paste(clip);
                    return;
                case ClayKey.Z:
                    HandleKey(shift ? TextEditKey.Redo : TextEditKey.Undo);
                    return;
                case ClayKey.Left: HandleKey(TextEditKey.WordLeft, shift); return;
                case ClayKey.Right: HandleKey(TextEditKey.WordRight, shift); return;
                case ClayKey.Home: HandleKey(TextEditKey.TextStart, shift); return;
                case ClayKey.End: HandleKey(TextEditKey.TextEnd, shift); return;
            }
            return;
        }

        switch (key)
        {
            case ClayKey.Left: HandleKey(TextEditKey.Left, shift); break;
            case ClayKey.Right: HandleKey(TextEditKey.Right, shift); break;
            case ClayKey.Up: HandleKey(TextEditKey.Up, shift); break;
            case ClayKey.Down: HandleKey(TextEditKey.Down, shift); break;
            case ClayKey.Home: HandleKey(TextEditKey.LineStart, shift); break;
            case ClayKey.End: HandleKey(TextEditKey.LineEnd, shift); break;
            case ClayKey.PageUp: HandleKey(TextEditKey.PageUp, shift); break;
            case ClayKey.PageDown: HandleKey(TextEditKey.PageDown, shift); break;
            case ClayKey.Delete: HandleKey(TextEditKey.Delete); break;
            case ClayKey.Backspace: HandleKey(TextEditKey.Backspace); break;
            case ClayKey.Enter: ProcessChar('\n'); break;
            case ClayKey.Tab: ProcessChar('\t'); break;
            case ClayKey.Insert: HandleKey(TextEditKey.Insert); break;
        }
    }

    /// <summary>
    /// Processes a typed character. Ignores non-printable characters (below 32).
    /// </summary>
    public void ProcessChar(char ch)
    {
        if (_focused == null) return;
        _focused.HandleChar(ch);
        _focusedDirty = true;
    }

    // ── Low-level input routing ───────────────────────────────────────

    /// <summary>
    /// Routes a control key directly to the focused text input.
    /// Prefer <see cref="ProcessKey"/> which handles modifiers and shortcuts.
    /// </summary>
    public void HandleKey(TextEditKey key, bool shift = false)
    {
        if (_focused == null) return;
        _focused.HandleKey(key, shift);
        _focusedDirty = true;
    }

    /// <summary>
    /// Routes a character directly to the focused text input.
    /// Prefer <see cref="ProcessChar"/> for the high-level API.
    /// </summary>
    public void HandleChar(char ch)
    {
        if (_focused == null) return;
        _focused.HandleChar(ch);
        _focusedDirty = true;
    }

    /// <summary>
    /// Selects all text in the focused text input.
    /// </summary>
    public void SelectAll()
    {
        if (_focused == null) return;
        _focused.HandleKey(TextEditKey.TextStart);
        _focused.HandleKey(TextEditKey.TextEnd, shift: true);
    }

    /// <summary>
    /// Cuts the selected text from the focused text input and returns it.
    /// Returns null if no selection or no focused widget.
    /// </summary>
    public string? Cut()
    {
        if (_focused == null) return null;
        var result = _focused.Cut();
        if (result != null) _focusedDirty = true;
        return result;
    }

    /// <summary>
    /// Pastes text into the focused text input, replacing any selection.
    /// Returns false if no focused widget or paste failed.
    /// </summary>
    public bool Paste(ReadOnlySpan<char> text)
    {
        if (_focused == null) return false;
        bool ok = _focused.Paste(text);
        if (ok) _focusedDirty = true;
        return ok;
    }

    /// <summary>
    /// Pastes text into the focused text input.
    /// </summary>
    public bool Paste(string text) => Paste(text.AsSpan());

    /// <summary>
    /// Returns the selected text in the focused widget, or empty string.
    /// Use for implementing Ctrl+C (copy without cut).
    /// </summary>
    public string GetSelectedText()
    {
        return _focused?.GetSelectedText() ?? string.Empty;
    }

    /// <summary>
    /// Gives focus to the text input with the given ID.
    /// The widget must have been drawn at least once.
    /// </summary>
    public bool Focus(ElementId id)
    {
        if (_widgets.TryGetValue(id.Id, out var widget))
        {
            _focused?.Blur();
            widget.Focus();
            _focused = widget;
            _focusedId = id.Id;
            return true;
        }
        return false;
    }

    /// <summary>
    /// Removes focus from the currently focused text input.
    /// </summary>
    public void Blur()
    {
        _focused?.Blur();
        _focused = null;
        _focusedId = 0;
        _focusedDirty = false;
    }

    /// <summary>
    /// Removes all cached widgets and resets state.
    /// Call when tearing down the UI or switching screens.
    /// </summary>
    public void Reset()
    {
        _widgets.Clear();
        _activeIds.Clear();
        _toRemove.Clear();
        _focused = null;
        _focusedId = 0;
        _focusedDirty = false;
    }
}
