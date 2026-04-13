using System.Numerics;
using Clay;

namespace Clay.Test;

public class ClayUIBasicWidgetTests : IDisposable
{
    private readonly ClayUIFixture _fixture;

    public ClayUIBasicWidgetTests()
    {
        _fixture = new ClayUIFixture();
    }

    public void Dispose() => _fixture.Dispose();

    // ============ Button ============

    [Fact]
    public void Button_Renders_WithoutCrash()
    {
        _fixture.RunFrame(() => ClayUI.Button("Test Button"));
    }

    [Fact]
    public void Button_NotClicked_ReturnsFalse()
    {
        bool clicked = false;
        _fixture.RunTwoFrames(
            () => clicked = ClayUI.Button("Btn"),
            mousePos: new Vector2(400, 400)); // far from button

        Assert.False(clicked);
    }

    [Fact]
    public void Button_Clicked_ReturnsTrue()
    {
        bool clicked = false;
        // Button is at top-left of the root container, roughly at (0,0)
        // With padding and text, the button should be around (0,0) to (some width, some height)
        _fixture.Click(
            () => clicked = ClayUI.Button("Click Me"),
            mousePos: new Vector2(40, 10));

        Assert.True(clicked);
    }

    [Fact]
    public void Button_Clicked_UpdatesQueryState()
    {
        _fixture.Click(
            () => ClayUI.Button("QueryBtn"),
            mousePos: new Vector2(40, 10));

        Assert.True(ClayUI.WasHovered("QueryBtn"));
        Assert.True(ClayUI.WasClicked("QueryBtn"));
    }

    [Fact]
    public void Button_ClickOutside_ReturnsFalse()
    {
        bool clicked = false;
        _fixture.Click(
            () => clicked = ClayUI.Button("Btn2"),
            mousePos: new Vector2(700, 500));

        Assert.False(clicked);
    }

    [Fact]
    public void Button_GeneratesRenderCommands()
    {
        var commands = _fixture.RunFrame(() => ClayUI.Button("Render Test"));

        // Should have at least a rectangle (background) and text command
        bool hasRect = false;
        bool hasText = false;
        foreach (var cmd in commands)
        {
            if (cmd.CommandType == RenderCommandType.Rectangle) hasRect = true;
            if (cmd.CommandType == RenderCommandType.Text) hasText = true;
        }

        Assert.True(hasRect, "Button should generate a rectangle render command");
        Assert.True(hasText, "Button should generate a text render command");
    }

    // ============ Label ============

    [Fact]
    public void Label_Renders_WithoutCrash()
    {
        _fixture.RunFrame(() => ClayUI.Label("Hello World"));
    }

    [Fact]
    public void Label_GeneratesTextCommand()
    {
        var commands = _fixture.RunFrame(() => ClayUI.Label("Test Label"));

        bool hasText = false;
        foreach (var cmd in commands)
        {
            if (cmd.CommandType == RenderCommandType.Text)
            {
                hasText = true;
                break;
            }
        }

        Assert.True(hasText, "Label should generate a text render command");
    }

    // ============ Heading ============

    [Fact]
    public void Heading_Renders_WithoutCrash()
    {
        _fixture.RunFrame(() => ClayUI.Heading("My Heading"));
    }

    [Fact]
    public void Heading_GeneratesTextCommand()
    {
        var commands = _fixture.RunFrame(() => ClayUI.Heading("Title"));

        bool hasText = false;
        foreach (var cmd in commands)
        {
            if (cmd.CommandType == RenderCommandType.Text)
            {
                hasText = true;
                break;
            }
        }

        Assert.True(hasText, "Heading should generate a text render command");
    }

    // ============ Checkbox ============

    [Fact]
    public void Checkbox_Renders_WithoutCrash()
    {
        bool val = false;
        _fixture.RunFrame(() => ClayUI.Checkbox("Check", ref val));
    }

    [Fact]
    public void Checkbox_NotClicked_DoesNotToggle()
    {
        bool val = false;
        bool changed = false;
        _fixture.RunTwoFrames(
            () => changed = ClayUI.Checkbox("CB1", ref val),
            mousePos: new Vector2(700, 500));

        Assert.False(changed);
        Assert.False(val);
    }

    [Fact]
    public void Checkbox_Clicked_TogglesValue()
    {
        bool val = false;
        bool changed = false;
        _fixture.Click(
            () => changed = ClayUI.Checkbox("CB2", ref val),
            mousePos: new Vector2(20, 10));

        Assert.True(changed, "Checkbox should report changed=true on click");
        Assert.True(val, "Checkbox should toggle from false to true");
    }

    [Fact]
    public void Checkbox_Clicked_TogglesBackToFalse()
    {
        bool val = true;
        bool changed = false;
        _fixture.Click(
            () => changed = ClayUI.Checkbox("CB3", ref val),
            mousePos: new Vector2(20, 10));

        Assert.True(changed);
        Assert.False(val, "Checkbox should toggle from true to false");
    }

    [Fact]
    public void Checkbox_Checked_RendersMoreRectsThanUnchecked()
    {
        // Unchecked
        bool unchecked_ = false;
        var cmdsUnchecked = _fixture.RunFrame(() => ClayUI.Checkbox("CBU", ref unchecked_));
        int uncheckedRects = 0;
        foreach (var cmd in cmdsUnchecked)
            if (cmd.CommandType == RenderCommandType.Rectangle) uncheckedRects++;

        // Checked
        bool checked_ = true;
        var cmdsChecked = _fixture.RunFrame(() => ClayUI.Checkbox("CBC", ref checked_));
        int checkedRects = 0;
        foreach (var cmd in cmdsChecked)
            if (cmd.CommandType == RenderCommandType.Rectangle) checkedRects++;

        Assert.True(checkedRects > uncheckedRects,
            $"Checked checkbox ({checkedRects} rects) should have more rectangles than unchecked ({uncheckedRects} rects)");
    }

    // ============ Toggle ============

    [Fact]
    public void Toggle_Renders_WithoutCrash()
    {
        bool val = false;
        _fixture.RunFrame(() => ClayUI.Toggle("Toggle", ref val));
    }

    [Fact]
    public void Toggle_NotClicked_DoesNotChange()
    {
        bool val = false;
        bool changed = false;
        _fixture.RunTwoFrames(
            () => changed = ClayUI.Toggle("Tgl1", ref val),
            mousePos: new Vector2(700, 500));

        Assert.False(changed);
        Assert.False(val);
    }

    [Fact]
    public void Toggle_Clicked_TogglesValue()
    {
        bool val = false;
        bool changed = false;
        _fixture.Click(
            () => changed = ClayUI.Toggle("Tgl2", ref val),
            mousePos: new Vector2(20, 10));

        Assert.True(changed, "Toggle should report changed=true on click");
        Assert.True(val, "Toggle should toggle from false to true");
    }

    // ============ ProgressBar ============

    [Fact]
    public void ProgressBar_Renders_WithoutCrash()
    {
        _fixture.RunFrame(() => ClayUI.ProgressBar(0.5f));
    }

    [Fact]
    public void ProgressBar_ZeroPercent_Renders()
    {
        var commands = _fixture.RunFrame(() => ClayUI.ProgressBar(0f));

        bool hasRect = false;
        foreach (var cmd in commands)
        {
            if (cmd.CommandType == RenderCommandType.Rectangle)
            {
                hasRect = true;
                break;
            }
        }

        Assert.True(hasRect, "ProgressBar should render rectangle background");
    }

    [Fact]
    public void ProgressBar_FullPercent_Renders()
    {
        _fixture.RunFrame(() => ClayUI.ProgressBar(1f));
    }

    [Fact]
    public void ProgressBar_ClampsOutOfRange()
    {
        // Should not crash with values outside 0-1
        _fixture.RunFrame(() => ClayUI.ProgressBar(-0.5f));
        _fixture.RunFrame(() => ClayUI.ProgressBar(1.5f));
    }

    [Fact]
    public void ProgressBar_CustomRange()
    {
        _fixture.RunFrame(() => ClayUI.ProgressBar(50f, min: 0f, max: 100f));
    }

    [Fact]
    public void ProgressBar_ZeroRange_DoesNotCrash()
    {
        _fixture.RunFrame(() => ClayUI.ProgressBar(10f, min: 10f, max: 10f));
    }

    // ============ Image ============

    [Fact]
    public void Image_Renders_WithoutCrash()
    {
        _fixture.RunFrame(() => ClayUI.Image("test_image", 64, 64));
    }

    [Fact]
    public void Image_GeneratesImageCommand()
    {
        var commands = _fixture.RunFrame(() => ClayUI.Image("img_data", 100, 50));

        bool hasImage = false;
        foreach (var cmd in commands)
        {
            if (cmd.CommandType == RenderCommandType.Image)
            {
                hasImage = true;
                break;
            }
        }

        Assert.True(hasImage, "Image should generate an image render command");
    }

    // ============ ImageButton ============

    [Fact]
    public void ImageButton_Renders_WithoutCrash()
    {
        _fixture.RunFrame(() => ClayUI.ImageButton("btn_img", 32, 32));
    }

    [Fact]
    public void ImageButton_NotClicked_ReturnsFalse()
    {
        bool clicked = false;
        _fixture.RunTwoFrames(
            () => clicked = ClayUI.ImageButton("ibtn1", 32, 32),
            mousePos: new Vector2(700, 500));

        Assert.False(clicked);
    }

    [Fact]
    public void ImageButton_Clicked_ReturnsTrue()
    {
        bool clicked = false;
        _fixture.Click(
            () => clicked = ClayUI.ImageButton("ibtn2", 32, 32),
            mousePos: new Vector2(16, 16));

        Assert.True(clicked);
    }

    // ============ Space ============

    [Fact]
    public void Space_Renders_WithoutCrash()
    {
        _fixture.RunFrame(() => ClayUI.Space());
    }

    [Fact]
    public void Space_CustomHeight_Renders()
    {
        _fixture.RunFrame(() => ClayUI.Space(20));
    }

    // ============ Separator ============

    [Fact]
    public void Separator_Renders_WithoutCrash()
    {
        _fixture.RunFrame(() => ClayUI.Separator());
    }

    [Fact]
    public void Separator_CustomColor_Renders()
    {
        _fixture.RunFrame(() => ClayUI.Separator(Color.Red));
    }

    [Fact]
    public void Separator_CustomHeight_Renders()
    {
        _fixture.RunFrame(() => ClayUI.Separator(height: 3));
    }

    [Fact]
    public void Separator_GeneratesRectangle()
    {
        var commands = _fixture.RunFrame(() => ClayUI.Separator());

        bool hasRect = false;
        foreach (var cmd in commands)
        {
            if (cmd.CommandType == RenderCommandType.Rectangle)
            {
                hasRect = true;
                break;
            }
        }

        Assert.True(hasRect, "Separator should generate a rectangle command");
    }

    // ============ RadioGroup ============

    [Fact]
    public void RadioGroup_Renders_WithoutCrash()
    {
        int selected = 0;
        _fixture.RunFrame(() => ClayUI.RadioGroup("Radio", ref selected, ["A", "B", "C"]));
    }

    [Fact]
    public void RadioGroup_NotClicked_DoesNotChange()
    {
        int selected = 0;
        bool changed = false;
        _fixture.RunTwoFrames(
            () => changed = ClayUI.RadioGroup("RG1", ref selected, ["X", "Y"]),
            mousePos: new Vector2(700, 500));

        Assert.False(changed);
        Assert.Equal(0, selected);
    }

    // ============ Multiple widgets in one frame ============

    [Fact]
    public void MultipleWidgets_RenderTogether_WithoutCrash()
    {
        bool check = false;
        bool toggle = false;
        float slider = 0.5f;
        int radio = 0;

        _fixture.RunFrame(() =>
        {
            ClayUI.Heading("Title");
            ClayUI.Label("Some text");
            ClayUI.Button("Click");
            ClayUI.Checkbox("Check", ref check);
            ClayUI.Toggle("Toggle", ref toggle);
            ClayUI.Slider("Slider", ref slider);
            ClayUI.ProgressBar(0.75f);
            ClayUI.RadioGroup("Radio", ref radio, ["A", "B"]);
            ClayUI.Space();
            ClayUI.Separator();
            ClayUI.Image("img", 32, 32);
        });
    }
}
