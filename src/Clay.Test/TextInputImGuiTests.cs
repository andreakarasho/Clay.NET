using Clay.Widgets;
using StbTextEdit;

namespace Clay.Test;

public class TextInputImGuiTests : IDisposable
{
    private static readonly ElementId TestId = Clay.Id("test");

    private static readonly TextInputStyle TestStyle = new()
    {
        BackgroundColor = Color.White,
        FocusedBackgroundColor = Color.White,
        TextColor = Color.Rgba(0, 0, 0),
        CursorColor = Color.Rgba(0, 0, 0),
        SelectionColor = Color.Rgba(0, 0, 255, 100),
        FontId = 0,
        FontSize = 16,
        Padding = Padding.All(4),
        Sizing = new Sizing(SizingAxis.Fixed(200), SizingAxis.Fixed(30)),
    };

    private readonly TextInput _textInput;

    public TextInputImGuiTests()
    {
        Clay.Initialize(new Dimensions(800, 600), new SimpleTextMeasurer());
        Clay.SetPointerState(new System.Numerics.Vector2(-1, -1), false);
        _textInput = new TextInput(new SimpleTextMeasurer());
    }

    public void Dispose()
    {
        _textInput.Reset();
        Clay.Shutdown();
    }

    private bool DrawFrame(ref string text)
    {
        bool changed = false;
        _textInput.BeginFrame();
        Clay.BeginLayout();
        using (Clay.Element(new ElementDeclaration { Layout = LayoutConfig.FillColumn() }))
        {
            changed = _textInput.Draw(TestId, ref text, TestStyle);
        }
        Clay.EndLayout();
        return changed;
    }

    // ── Basic Draw ────────────────────────────────────────────────────

    [Fact]
    public void Draw_CreatesElement_NoChange()
    {
        string text = "hello";
        bool changed = DrawFrame(ref text);
        Assert.False(changed);
        Assert.Equal("hello", text);
    }

    [Fact]
    public void Draw_ExternalTextChange_SyncsToWidget()
    {
        string text = "hello";
        DrawFrame(ref text);

        text = "world";
        bool changed = DrawFrame(ref text);
        Assert.False(changed);
        Assert.Equal("world", text);
    }

    // ── Typing ────────────────────────────────────────────────────────

    [Fact]
    public void HandleChar_ModifiesText()
    {
        string text = "";
        DrawFrame(ref text);
        _textInput.Focus(TestId);

        _textInput.HandleChar('h');
        _textInput.HandleChar('i');

        bool changed = DrawFrame(ref text);
        Assert.True(changed);
        Assert.Equal("hi", text);
    }

    [Fact]
    public void HandleKey_DeleteModifiesText()
    {
        string text = "abc";
        DrawFrame(ref text);
        _textInput.Focus(TestId);

        _textInput.HandleKey(TextEditKey.Right);
        _textInput.HandleKey(TextEditKey.Delete);

        bool changed = DrawFrame(ref text);
        Assert.True(changed);
        Assert.Equal("ac", text);
    }

    [Fact]
    public void HandleKey_BackspaceModifiesText()
    {
        string text = "abc";
        DrawFrame(ref text);
        _textInput.Focus(TestId);

        _textInput.HandleKey(TextEditKey.TextEnd);
        _textInput.HandleKey(TextEditKey.Backspace);

        bool changed = DrawFrame(ref text);
        Assert.True(changed);
        Assert.Equal("ab", text);
    }

    // ── Cut / Copy / Paste ────────────────────────────────────────────

    [Fact]
    public void CutPaste_RoundTrips()
    {
        string text = "hello world";
        DrawFrame(ref text);
        _textInput.Focus(TestId);

        for (int i = 0; i < 5; i++)
            _textInput.HandleKey(TextEditKey.Right, shift: true);

        string? cut = _textInput.Cut();
        Assert.Equal("hello", cut);

        bool changed = DrawFrame(ref text);
        Assert.True(changed);
        Assert.Equal(" world", text);

        _textInput.HandleKey(TextEditKey.TextEnd);
        _textInput.Paste(cut!);

        changed = DrawFrame(ref text);
        Assert.True(changed);
        Assert.Equal(" worldhello", text);
    }

    [Fact]
    public void GetSelectedText_ReturnsSelection()
    {
        string text = "hello world";
        DrawFrame(ref text);
        _textInput.Focus(TestId);

        _textInput.HandleKey(TextEditKey.Right);
        for (int i = 0; i < 4; i++)
            _textInput.HandleKey(TextEditKey.Right, shift: true);

        Assert.Equal("ello", _textInput.GetSelectedText());
    }

    [Fact]
    public void SelectAll_SelectsEntireText()
    {
        string text = "hello";
        DrawFrame(ref text);
        _textInput.Focus(TestId);

        _textInput.SelectAll();
        Assert.Equal("hello", _textInput.GetSelectedText());
    }

    // ── Focus management ──────────────────────────────────────────────

    [Fact]
    public void NoFocus_InputIgnored()
    {
        Assert.False(_textInput.HasFocus);
        _textInput.HandleChar('a');
        _textInput.HandleKey(TextEditKey.Left);
        Assert.Null(_textInput.Cut());
        Assert.False(_textInput.Paste("text"));
        Assert.Equal(string.Empty, _textInput.GetSelectedText());
    }

    [Fact]
    public void Focus_ById()
    {
        string text = "hello";
        DrawFrame(ref text);

        Assert.False(_textInput.HasFocus);
        bool ok = _textInput.Focus(TestId);
        Assert.True(ok);
        Assert.True(_textInput.HasFocus);
        Assert.NotNull(_textInput.FocusedWidget);
    }

    [Fact]
    public void Focus_UnknownId_ReturnsFalse()
    {
        Assert.False(_textInput.Focus(Clay.Id("nonexistent")));
    }

    [Fact]
    public void Blur_RemovesFocus()
    {
        string text = "";
        DrawFrame(ref text);
        _textInput.Focus(TestId);
        Assert.True(_textInput.HasFocus);

        _textInput.Blur();
        Assert.False(_textInput.HasFocus);
    }

    // ── Widget lifecycle ──────────────────────────────────────────────

    [Fact]
    public void Widget_RemovedWhenNotDrawn()
    {
        string text = "hello";
        DrawFrame(ref text);
        _textInput.Focus(TestId);
        Assert.True(_textInput.HasFocus);

        // Frame without drawing the widget
        _textInput.BeginFrame();
        Clay.BeginLayout();
        using (Clay.Element(new ElementDeclaration { Layout = LayoutConfig.FillColumn() }))
        {
        }
        Clay.EndLayout();

        // Next BeginFrame cleans up
        _textInput.BeginFrame();
        Assert.False(_textInput.HasFocus);

        // Drawing again creates a fresh widget
        string text2 = "world";
        Clay.BeginLayout();
        using (Clay.Element(new ElementDeclaration { Layout = LayoutConfig.FillColumn() }))
        {
            _textInput.Draw(TestId, ref text2, TestStyle);
        }
        Clay.EndLayout();
        Assert.Equal("world", text2);
    }

    [Fact]
    public void Reset_ClearsEverything()
    {
        string text = "hello";
        DrawFrame(ref text);
        _textInput.Focus(TestId);

        _textInput.Reset();
        Assert.False(_textInput.HasFocus);
        Assert.Null(_textInput.FocusedWidget);
    }

    // ── Undo / Redo ───────────────────────────────────────────────────

    [Fact]
    public void UndoRedo_ThroughImGui()
    {
        string text = "";
        DrawFrame(ref text);
        _textInput.Focus(TestId);

        _textInput.HandleChar('a');
        _textInput.HandleChar('b');
        DrawFrame(ref text);
        Assert.Equal("ab", text);

        _textInput.HandleKey(TextEditKey.Undo);
        DrawFrame(ref text);
        Assert.Equal("a", text);

        _textInput.HandleKey(TextEditKey.Redo);
        DrawFrame(ref text);
        Assert.Equal("ab", text);
    }

    // ── Multiple widgets ──────────────────────────────────────────────

    [Fact]
    public void MultipleWidgets_OnlyFocusedReceivesInput()
    {
        string text1 = "aaa";
        string text2 = "bbb";
        var id1 = Clay.Id("input1");
        var id2 = Clay.Id("input2");

        _textInput.BeginFrame();
        Clay.BeginLayout();
        using (Clay.Element(new ElementDeclaration { Layout = LayoutConfig.FillColumn() }))
        {
            _textInput.Draw(id1, ref text1, TestStyle);
            _textInput.Draw(id2, ref text2, TestStyle);
        }
        Clay.EndLayout();

        _textInput.Focus(id2);
        _textInput.HandleChar('X');

        _textInput.BeginFrame();
        Clay.BeginLayout();
        bool changed1, changed2;
        using (Clay.Element(new ElementDeclaration { Layout = LayoutConfig.FillColumn() }))
        {
            changed1 = _textInput.Draw(id1, ref text1, TestStyle);
            changed2 = _textInput.Draw(id2, ref text2, TestStyle);
        }
        Clay.EndLayout();

        Assert.False(changed1);
        Assert.True(changed2);
        Assert.Equal("aaa", text1);
        Assert.Equal("Xbbb", text2);
    }

    // ── Multiple instances are independent ────────────────────────────

    [Fact]
    public void SeparateInstances_AreIndependent()
    {
        var input2 = new TextInput(new SimpleTextMeasurer());

        string text1 = "aaa";
        string text2 = "bbb";

        DrawFrame(ref text1);
        _textInput.Focus(TestId);

        input2.BeginFrame();
        Clay.BeginLayout();
        using (Clay.Element(new ElementDeclaration { Layout = LayoutConfig.FillColumn() }))
        {
            input2.Draw(Clay.Id("other"), ref text2, TestStyle);
        }
        Clay.EndLayout();

        _textInput.HandleChar('X');
        DrawFrame(ref text1);

        Assert.Equal("Xaaa", text1);
        Assert.Equal("bbb", text2); // Unaffected
    }
}
