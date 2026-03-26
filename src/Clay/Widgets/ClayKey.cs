namespace Clay.Widgets;

/// <summary>
/// Platform-agnostic key codes for text edit input.
/// Map your framework's key codes to these.
/// </summary>
public enum ClayKey
{
    Left, Right, Up, Down,
    Home, End,
    PageUp, PageDown,
    Delete, Backspace,
    Enter, Tab, Insert,
    A, C, V, X, Z,
}

/// <summary>
/// Modifier keys for text edit input.
/// </summary>
[Flags]
public enum ClayKeyModifiers
{
    None = 0,
    Shift = 1,
    Ctrl = 2,
}
