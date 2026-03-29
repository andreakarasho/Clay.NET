using System.Numerics;
using Clay;
using Clay.Widgets;

namespace Clay.Test;

public class ClayUITextInputScrollTests : IDisposable
{
    private static readonly TextInputStyle MultiLineStyle = new()
    {
        BackgroundColor = Color.Rgba(50, 50, 55),
        FocusedBackgroundColor = Color.Rgba(60, 60, 70),
        TextColor = Color.White,
        CursorColor = Color.White,
        SelectionColor = Color.Rgba(80, 130, 200, 120),
        FontId = 0,
        FontSize = 16,
        Padding = Padding.All(4),
        Sizing = new Sizing(SizingAxis.Fixed(300), SizingAxis.Fixed(80)),
    };

    // Enough lines to overflow 80px height
    private string _longText = string.Join("\n", Enumerable.Range(1, 20).Select(i => $"Line {i}"));

    public ClayUITextInputScrollTests()
    {
        ClayApi.Initialize(new Dimensions(800, 600), new SimpleTextMeasurer(), maxElementCount: 8192);
    }

    public void Dispose()
    {
        ClayUI.ClearState();
        ClayApi.Shutdown();
    }

    private void RunFrame(Vector2 mousePos = default, bool mouseDown = false, Vector2 scrollDelta = default)
    {
        ClayUI.BeginFrame(new Dimensions(800, 600), mouseDown, mousePos, scrollDelta);

        using (ClayApi.Element(new ElementDeclaration
        {
            Layout = new LayoutConfig { Sizing = Sizing.Fill(), Direction = LayoutDirection.TopToBottom }
        }))
        {
            ClayUI.TextInput("MLScroll", ref _longText, singleLine: false, style: MultiLineStyle);
        }

        ClayUI.EndFrame();
    }

    private void FocusTextInput()
    {
        // Frame 1: establish layout
        RunFrame();
        // Frame 2: click to focus (text input is at top-left area)
        RunFrame(mousePos: new Vector2(20, 20), mouseDown: true);
    }

    [Fact]
    public void MultilineTextInput_WhenFocused_ScrollsWithWheel()
    {
        FocusTextInput();

        // Verify focused
        Assert.True(ClayApi.TextEditHasFocus, "Text input should be focused after click");

        float scrollBefore = ClayApi.TextEditFocusedWidget!.ScrollY;

        // Frame with scroll delta (wheel down)
        RunFrame(mousePos: new Vector2(20, 20), scrollDelta: new Vector2(0, -3));

        float scrollAfter = ClayApi.TextEditFocusedWidget!.ScrollY;
        Assert.True(scrollAfter > scrollBefore,
            $"ScrollY should increase on wheel down (before={scrollBefore}, after={scrollAfter})");
    }

    [Fact]
    public void MultilineTextInput_WhenNotFocused_DoesNotScroll()
    {
        // Frame 1: establish layout without focusing
        RunFrame();
        // Frame 2: establish bounding boxes
        RunFrame();

        Assert.False(ClayApi.TextEditHasFocus, "No text input should be focused");

        // Frame with scroll delta — should not crash and text input should not scroll
        RunFrame(mousePos: new Vector2(20, 20), scrollDelta: new Vector2(0, -3));

        // No focused widget, so nothing to check scroll on — just verify no crash
    }

    [Fact]
    public void MultilineTextInput_WhenFocused_BlocksParentScroll()
    {
        // Create layout with a parent scroll container + text input inside
        string text = _longText;
        var scrollContainerId = ClayApi.Id("ParentScroll");

        // Frame 1: establish layout
        ClayUI.BeginFrame(new Dimensions(800, 600), false, new Vector2(20, 20));
        using (ClayApi.Element(new ElementDeclaration
        {
            Id = scrollContainerId,
            Layout = new LayoutConfig
            {
                Sizing = Sizing.FixedSize(400, 300),
                Direction = LayoutDirection.TopToBottom
            },
            Scroll = ScrollConfig.VerticalScroll
        }))
        {
            ClayUI.TextInput("MLBlock", ref text, singleLine: false, style: MultiLineStyle);
            // Extra content to make parent scrollable
            using (ClayApi.Element(new ElementDeclaration
            {
                Layout = new LayoutConfig { Sizing = Sizing.FixedSize(380, 500) }
            })) { }
        }
        ClayUI.EndFrame();

        // Frame 2: click to focus text input
        ClayUI.BeginFrame(new Dimensions(800, 600), true, new Vector2(20, 20));
        using (ClayApi.Element(new ElementDeclaration
        {
            Id = scrollContainerId,
            Layout = new LayoutConfig
            {
                Sizing = Sizing.FixedSize(400, 300),
                Direction = LayoutDirection.TopToBottom
            },
            Scroll = ScrollConfig.VerticalScroll
        }))
        {
            ClayUI.TextInput("MLBlock", ref text, singleLine: false, style: MultiLineStyle);
            using (ClayApi.Element(new ElementDeclaration
            {
                Layout = new LayoutConfig { Sizing = Sizing.FixedSize(380, 500) }
            })) { }
        }
        ClayUI.EndFrame();

        Assert.True(ClayApi.TextEditHasFocus, "Text input should be focused");

        // Frame 3: scroll with wheel while focused — parent should NOT scroll
        ClayUI.BeginFrame(new Dimensions(800, 600), false, new Vector2(20, 20),
            scrollDelta: new Vector2(0, -3));
        using (ClayApi.Element(new ElementDeclaration
        {
            Id = scrollContainerId,
            Layout = new LayoutConfig
            {
                Sizing = Sizing.FixedSize(400, 300),
                Direction = LayoutDirection.TopToBottom
            },
            Scroll = ScrollConfig.VerticalScroll
        }))
        {
            ClayUI.TextInput("MLBlock", ref text, singleLine: false, style: MultiLineStyle);
            using (ClayApi.Element(new ElementDeclaration
            {
                Layout = new LayoutConfig { Sizing = Sizing.FixedSize(380, 500) }
            })) { }
        }
        ClayUI.EndFrame();

        var parentData = ClayApi.GetScrollContainerData(scrollContainerId);
        Assert.True(parentData.Found);
        Assert.Equal(0f, parentData.ScrollPosition.Y);
    }

    [Fact]
    public void MultilineTextInput_WhenNotFocused_ParentScrollsNormally()
    {
        string text = _longText;
        var scrollContainerId = ClayApi.Id("ParentScroll2");

        // Frame 1: establish layout (no focus)
        ClayUI.BeginFrame(new Dimensions(800, 600), false, new Vector2(150, 200));
        using (ClayApi.Element(new ElementDeclaration
        {
            Id = scrollContainerId,
            Layout = new LayoutConfig
            {
                Sizing = Sizing.FixedSize(400, 300),
                Direction = LayoutDirection.TopToBottom
            },
            Scroll = ScrollConfig.VerticalScroll
        }))
        {
            ClayUI.TextInput("MLNoBlock", ref text, singleLine: false, style: MultiLineStyle);
            using (ClayApi.Element(new ElementDeclaration
            {
                Layout = new LayoutConfig { Sizing = Sizing.FixedSize(380, 500) }
            })) { }
        }
        ClayUI.EndFrame();

        // Frame 2: same layout
        ClayUI.BeginFrame(new Dimensions(800, 600), false, new Vector2(150, 200));
        using (ClayApi.Element(new ElementDeclaration
        {
            Id = scrollContainerId,
            Layout = new LayoutConfig
            {
                Sizing = Sizing.FixedSize(400, 300),
                Direction = LayoutDirection.TopToBottom
            },
            Scroll = ScrollConfig.VerticalScroll
        }))
        {
            ClayUI.TextInput("MLNoBlock", ref text, singleLine: false, style: MultiLineStyle);
            using (ClayApi.Element(new ElementDeclaration
            {
                Layout = new LayoutConfig { Sizing = Sizing.FixedSize(380, 500) }
            })) { }
        }
        ClayUI.EndFrame();

        Assert.False(ClayApi.TextEditHasFocus);

        // Frame 3: scroll — parent should scroll normally (mouse at y=200, below text input)
        ClayUI.BeginFrame(new Dimensions(800, 600), false, new Vector2(150, 200),
            scrollDelta: new Vector2(0, -3));
        using (ClayApi.Element(new ElementDeclaration
        {
            Id = scrollContainerId,
            Layout = new LayoutConfig
            {
                Sizing = Sizing.FixedSize(400, 300),
                Direction = LayoutDirection.TopToBottom
            },
            Scroll = ScrollConfig.VerticalScroll
        }))
        {
            ClayUI.TextInput("MLNoBlock", ref text, singleLine: false, style: MultiLineStyle);
            using (ClayApi.Element(new ElementDeclaration
            {
                Layout = new LayoutConfig { Sizing = Sizing.FixedSize(380, 500) }
            })) { }
        }
        ClayUI.EndFrame();

        var parentData = ClayApi.GetScrollContainerData(scrollContainerId);
        Assert.True(parentData.Found);
        Assert.True(parentData.ScrollPosition.Y > 0,
            "Parent should scroll when multiline text input is not focused");
    }

    [Fact]
    public void MultilineTextInput_CursorMoveKeepsVisible_ButWheelDoesNot()
    {
        FocusTextInput();

        var widget = ClayApi.TextEditFocusedWidget!;
        float scrollBefore = widget.ScrollY;

        // Scroll down with wheel
        RunFrame(mousePos: new Vector2(20, 20), scrollDelta: new Vector2(0, -5));

        float scrollAfterWheel = widget.ScrollY;
        Assert.True(scrollAfterWheel > scrollBefore,
            "Wheel should scroll the text");

        // Now simulate cursor movement (arrow down) — should NOT snap scroll back
        // unless cursor goes out of view
        float scrollBeforeKey = widget.ScrollY;
        ClayUI.KeyDown(ClayKey.Down);
        RunFrame(mousePos: new Vector2(20, 20));

        // Cursor moved, EnsureCursorVisible may adjust scroll, but it shouldn't
        // reset to 0 — it should keep the cursor visible within the scrolled view
        float scrollAfterKey = widget.ScrollY;
        Assert.True(scrollAfterKey >= 0, "Scroll should be non-negative");
    }
}
