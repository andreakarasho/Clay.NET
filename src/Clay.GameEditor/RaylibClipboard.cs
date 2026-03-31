using Clay.Widgets;
using ZeroElectric.Vinculum;

namespace Clay.GameEditor;

public sealed class RaylibClipboard : IClipboard
{
    public string? GetText() => Raylib.GetClipboardTextAsString();
    public void SetText(string text) => Raylib.SetClipboardText(text);
}
