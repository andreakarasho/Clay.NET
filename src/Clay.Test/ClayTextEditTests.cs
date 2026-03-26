using Clay.Widgets;
using StbTextEdit;

namespace Clay.Test;

public class ClayTextEditTests : IDisposable
{
    private static readonly TextInputStyle Style = new()
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

    public ClayTextEditTests()
    {
        Clay.Initialize(new Dimensions(800, 600), new SimpleTextMeasurer());
        Clay.SetPointerState(new System.Numerics.Vector2(-1, -1), false);
    }

    public void Dispose() => Clay.Shutdown();

    private bool Frame(ref string text)
    {
        bool changed = false;
        Clay.BeginLayout(); // calls TextInput.BeginFrame() automatically
        using (Clay.Element(new ElementDeclaration { Layout = LayoutConfig.FillColumn() }))
        {
            changed = Clay.TextEdit(Clay.Id("t"), ref text, Style);
        }
        Clay.EndLayout();
        return changed;
    }

    [Fact]
    public void Draw_NoChange()
    {
        string text = "hello";
        Assert.False(Frame(ref text));
        Assert.Equal("hello", text);
    }

    [Fact]
    public void Type_ReturnsChanged()
    {
        string text = "";
        Frame(ref text);
        Clay.TextEditFocus(Clay.Id("t"));

        Clay.TextEditHandleChar('h');
        Clay.TextEditHandleChar('i');

        Assert.True(Frame(ref text));
        Assert.Equal("hi", text);
    }

    [Fact]
    public void Delete_ReturnsChanged()
    {
        string text = "abc";
        Frame(ref text);
        Clay.TextEditFocus(Clay.Id("t"));

        Clay.TextEditHandleKey(TextEditKey.Right);
        Clay.TextEditHandleKey(TextEditKey.Delete);

        Assert.True(Frame(ref text));
        Assert.Equal("ac", text);
    }

    [Fact]
    public void ExternalChange_Syncs()
    {
        string text = "hello";
        Frame(ref text);
        text = "world";
        Assert.False(Frame(ref text));
        Assert.Equal("world", text);
    }

    [Fact]
    public void CutPaste()
    {
        string text = "hello";
        Frame(ref text);
        Clay.TextEditFocus(Clay.Id("t"));

        Clay.TextEditSelectAll();
        string? cut = Clay.TextEditCut();
        Assert.Equal("hello", cut);

        Frame(ref text);
        Assert.Equal("", text);

        Clay.TextEditPaste("world");
        Frame(ref text);
        Assert.Equal("world", text);
    }

    [Fact]
    public void UndoRedo()
    {
        string text = "";
        Frame(ref text);
        Clay.TextEditFocus(Clay.Id("t"));

        Clay.TextEditHandleChar('a');
        Clay.TextEditHandleChar('b');
        Frame(ref text);
        Assert.Equal("ab", text);

        Clay.TextEditHandleKey(TextEditKey.Undo);
        Frame(ref text);
        Assert.Equal("a", text);

        Clay.TextEditHandleKey(TextEditKey.Redo);
        Frame(ref text);
        Assert.Equal("ab", text);
    }

    [Fact]
    public void Focus_Blur()
    {
        string text = "";
        Frame(ref text);

        Assert.False(Clay.TextEditHasFocus);
        Clay.TextEditFocus(Clay.Id("t"));
        Assert.True(Clay.TextEditHasFocus);
        Assert.NotNull(Clay.TextEditFocusedWidget);

        Clay.TextEditBlur();
        Assert.False(Clay.TextEditHasFocus);
    }

    [Fact]
    public void GetSelectedText()
    {
        string text = "hello";
        Frame(ref text);
        Clay.TextEditFocus(Clay.Id("t"));

        Clay.TextEditHandleKey(TextEditKey.Right);
        Clay.TextEditHandleKey(TextEditKey.Right, shift: true);
        Clay.TextEditHandleKey(TextEditKey.Right, shift: true);

        Assert.Equal("el", Clay.TextEditGetSelectedText());
    }

    [Fact]
    public void NoFocus_InputIgnored()
    {
        Assert.False(Clay.TextEditHasFocus);
        Clay.TextEditHandleChar('x');
        Clay.TextEditHandleKey(TextEditKey.Left);
        Assert.Null(Clay.TextEditCut());
        Assert.False(Clay.TextEditPaste("x"));
    }

    [Fact]
    public void MultipleWidgets()
    {
        string t1 = "aaa", t2 = "bbb";
        var id1 = Clay.Id("i1");
        var id2 = Clay.Id("i2");

        Clay.BeginLayout();
        using (Clay.Element(new ElementDeclaration { Layout = LayoutConfig.FillColumn() }))
        {
            Clay.TextEdit(id1, ref t1, Style);
            Clay.TextEdit(id2, ref t2, Style);
        }
        Clay.EndLayout();

        Clay.TextEditFocus(id2);
        Clay.TextEditHandleChar('X');

        bool c1, c2;
        Clay.BeginLayout();
        using (Clay.Element(new ElementDeclaration { Layout = LayoutConfig.FillColumn() }))
        {
            c1 = Clay.TextEdit(id1, ref t1, Style);
            c2 = Clay.TextEdit(id2, ref t2, Style);
        }
        Clay.EndLayout();

        Assert.False(c1);
        Assert.True(c2);
        Assert.Equal("aaa", t1);
        Assert.Equal("Xbbb", t2);
    }

    [Fact]
    public void BeginFrame_CalledAutomatically()
    {
        string text = "hello";
        Frame(ref text);
        Clay.TextEditFocus(Clay.Id("t"));
        Assert.True(Clay.TextEditHasFocus);

        // Don't draw the widget for one frame
        Clay.BeginLayout();
        using (Clay.Element(new ElementDeclaration { Layout = LayoutConfig.FillColumn() })) { }
        Clay.EndLayout();

        // Next BeginLayout calls BeginFrame which cleans up
        Clay.BeginLayout();
        using (Clay.Element(new ElementDeclaration { Layout = LayoutConfig.FillColumn() })) { }
        Clay.EndLayout();

        Assert.False(Clay.TextEditHasFocus);
    }
}
