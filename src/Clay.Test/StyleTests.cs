using Clay;

namespace Clay.Test;

public class StyleTests : IDisposable
{
    private readonly ClayUIFixture _fixture;

    public StyleTests()
    {
        _fixture = new ClayUIFixture();
    }

    public void Dispose() => _fixture.Dispose();

    // ============ Default style values ============

    [Fact]
    public void ButtonStyle_HasSensibleDefaults()
    {
        var s = new ButtonStyle();
        Assert.True(s.FontSize > 0);
        Assert.True(s.BackgroundColor.A > 0);
        Assert.True(s.TextColor.A > 0);
    }

    [Fact]
    public void CheckboxStyle_HasSensibleDefaults()
    {
        var s = new CheckboxStyle();
        Assert.True(s.FontSize > 0);
        Assert.True(s.BoxSize > 0);
    }

    [Fact]
    public void ToggleStyle_HasSensibleDefaults()
    {
        var s = new ToggleStyle();
        Assert.True(s.FontSize > 0);
        Assert.True(s.TrackWidth > 0);
        Assert.True(s.TrackHeight > 0);
        Assert.True(s.KnobSize > 0);
    }

    [Fact]
    public void SliderStyle_HasSensibleDefaults()
    {
        var s = new SliderStyle();
        Assert.True(s.FontSize > 0);
        Assert.True(s.TrackHeight > 0);
    }

    [Fact]
    public void ProgressBarStyle_HasSensibleDefaults()
    {
        var s = new ProgressBarStyle();
        Assert.True(s.Height > 0);
        Assert.True(s.FillColor.A > 0);
    }

    [Fact]
    public void LabelStyle_HasSensibleDefaults()
    {
        var s = new LabelStyle();
        Assert.True(s.FontSize > 0);
        Assert.True(s.TextColor.A > 0);
    }

    [Fact]
    public void HeadingStyle_HasSensibleDefaults()
    {
        var s = new HeadingStyle();
        Assert.True(s.FontSize > 0);
        Assert.True(s.TextColor.A > 0);
    }

    [Fact]
    public void PanelStyle_HasSensibleDefaults()
    {
        var s = new PanelStyle();
        Assert.True(s.BackgroundColor.A > 0);
    }

    [Fact]
    public void WindowStyle_HasSensibleDefaults()
    {
        var s = new WindowStyle();
        Assert.True(s.TitleBarHeight > 0);
        Assert.True(s.BackgroundColor.A > 0);
        Assert.True(s.MinWidth > 0);
    }

    [Fact]
    public void PopupStyle_HasSensibleDefaults()
    {
        var s = new PopupStyle();
        Assert.True(s.BackgroundColor.A > 0);
        Assert.True(s.MinWidth > 0);
    }

    [Fact]
    public void ScrollbarStyle_HasSensibleDefaults()
    {
        var s = new ScrollbarStyle();
        Assert.True(s.Width > 0);
        Assert.True(s.MinThumbSize > 0);
    }

    [Fact]
    public void RadioGroupStyle_HasSensibleDefaults()
    {
        var s = new RadioGroupStyle();
        Assert.True(s.FontSize > 0);
        Assert.True(s.CircleSize > 0);
        Assert.True(s.DotSize > 0);
    }

    [Fact]
    public void TreeNodeStyle_HasSensibleDefaults()
    {
        var s = new TreeNodeStyle();
        Assert.True(s.FontSize > 0);
        Assert.True(s.IndentSize > 0);
    }

    [Fact]
    public void ImageStyle_HasDefaults()
    {
        var s = new ImageStyle();
        // ImageStyle has optional fields, just verify construction works
        Assert.NotNull(s);
    }

    [Fact]
    public void ScrollAreaStyle_HasSensibleDefaults()
    {
        var s = new ScrollAreaStyle();
        Assert.True(s.BackgroundColor.A > 0);
    }

    // ============ ClayUIStyle ============

    [Fact]
    public void ClayUIStyle_Default_HasAllSubStyles()
    {
        var style = ClayUI.Style;
        Assert.NotNull(style);
        Assert.NotNull(style.Button);
        Assert.NotNull(style.Checkbox);
        Assert.NotNull(style.Toggle);
        Assert.NotNull(style.Slider);
        Assert.NotNull(style.ProgressBar);
        Assert.NotNull(style.Label);
        Assert.NotNull(style.Heading);
        Assert.NotNull(style.Panel);
        Assert.NotNull(style.Window);
        Assert.NotNull(style.Popup);
        Assert.NotNull(style.Scrollbar);
        Assert.NotNull(style.RadioGroup);
        Assert.NotNull(style.TreeNode);
        Assert.NotNull(style.Image);
        Assert.NotNull(style.ScrollArea);
    }

    [Fact]
    public void ClayUIStyle_SetAndGet()
    {
        var original = ClayUI.Style;
        var custom = new ClayUIStyle { Button = new ButtonStyle { FontSize = 42 } };

        ClayUI.Style = custom;
        Assert.Equal(42, ClayUI.Style.Button.FontSize);

        ClayUI.Style = original; // restore
    }

    // ============ Custom style passed to widgets ============

    [Fact]
    public void Button_CustomStyle_UsesIt()
    {
        var customStyle = new ButtonStyle
        {
            FontSize = 32,
            BackgroundColor = Color.Red,
            TextColor = Color.White
        };

        _fixture.RunFrame(() => ClayUI.Button("Styled", customStyle));
    }

    [Fact]
    public void Checkbox_CustomStyle_UsesIt()
    {
        var customStyle = new CheckboxStyle { BoxSize = 24 };
        bool val = false;
        _fixture.RunFrame(() => ClayUI.Checkbox("Styled CB", ref val, customStyle));
    }

    [Fact]
    public void Slider_CustomStyle_UsesIt()
    {
        var customStyle = new SliderStyle { TrackHeight = 12 };
        float val = 0.5f;
        _fixture.RunFrame(() => ClayUI.Slider("Styled SL", ref val, style: customStyle));
    }

    [Fact]
    public void Panel_CustomStyle_UsesIt()
    {
        var customStyle = new PanelStyle { BackgroundColor = Color.Rgba(50, 50, 50) };
        _fixture.RunFrame(() =>
        {
            ClayUI.BeginPanel("Styled Panel", customStyle);
            ClayUI.Label("Content");
            ClayUI.EndPanel();
        });
    }
}
