using Clay;

namespace Clay.Test;

public class LayoutStyleTests
{
    [Fact]
    public void Default_HasTransparentBackground()
    {
        var s = new LayoutStyle();
        Assert.False(s.BackgroundColor.IsVisible);
    }

    [Fact]
    public void SetBackgroundColor_Persists()
    {
        var s = new LayoutStyle { BackgroundColor = Color.Red };
        Assert.Equal(255f, s.BackgroundColor.R);
    }

    [Fact]
    public void SetCornerRadius_Persists()
    {
        var s = new LayoutStyle { CornerRadius = CornerRadius.All(8) };
        Assert.True(s.CornerRadius.HasRadius);
    }

    [Fact]
    public void SetBorder_Persists()
    {
        var s = new LayoutStyle { Border = BorderConfig.Uniform(2, Color.Red) };
        Assert.True(s.Border.HasBorder);
    }

    [Fact]
    public void SetPadding_Persists()
    {
        var s = new LayoutStyle { Padding = Padding.All(10) };
        Assert.Equal(10, s.Padding.Left);
    }

    [Fact]
    public void SetSizing_Persists()
    {
        var s = new LayoutStyle { Sizing = Sizing.FixedSize(100, 200) };
        Assert.Equal(SizingType.Fixed, s.Sizing.Width.Type);
    }

    [Fact]
    public void SetClipContent_Persists()
    {
        var s = new LayoutStyle { ClipContent = true };
        Assert.True(s.ClipContent);
    }

    [Fact]
    public void SetShadow_Persists()
    {
        var s = new LayoutStyle { Shadow = ShadowConfig.Drop(1, 2, 3, Color.Black) };
        Assert.True(s.Shadow.HasShadow);
    }

    [Fact]
    public void MergeOver_OverridesTouchedProperties()
    {
        var baseStyle = new LayoutStyle
        {
            BackgroundColor = Color.Red,
            CornerRadius = CornerRadius.All(4),
            ClipContent = false
        };

        var overlay = new LayoutStyle
        {
            BackgroundColor = Color.Blue,
            ClipContent = true
        };

        var merged = overlay.MergeOver(baseStyle);
        Assert.Equal(Color.Blue.B, merged.BackgroundColor.B);
        Assert.True(merged.ClipContent);
        // CornerRadius was not set on overlay, so base value preserved
        Assert.True(merged.CornerRadius.HasRadius);
    }

    [Fact]
    public void MergeOver_EmptyOverlay_PreservesBase()
    {
        var baseStyle = new LayoutStyle
        {
            BackgroundColor = Color.Red,
            Padding = Padding.All(16)
        };

        var empty = new LayoutStyle();
        var merged = empty.MergeOver(baseStyle);
        Assert.Equal(Color.Red.R, merged.BackgroundColor.R);
        Assert.Equal(16, merged.Padding.Left);
    }
}

public class StyleMergeOverTests : IDisposable
{
    private readonly ClayUIFixture _fixture;

    public StyleMergeOverTests()
    {
        _fixture = new ClayUIFixture();
    }

    public void Dispose() => _fixture.Dispose();

    [Fact]
    public void ButtonStyle_MergeOver_OverridesFontSize()
    {
        var baseStyle = new ButtonStyle { FontSize = 14 };
        var overlay = new ButtonStyle { FontSize = 24 };
        var merged = overlay.MergeOver(baseStyle);
        Assert.Equal(24, merged.FontSize);
    }

    [Fact]
    public void LabelStyle_MergeOver_OverridesTextColor()
    {
        var baseStyle = new LabelStyle { TextColor = Color.White };
        var overlay = new LabelStyle { TextColor = Color.Red };
        var merged = overlay.MergeOver(baseStyle);
        Assert.Equal(Color.Red.R, merged.TextColor.R);
    }

    [Fact]
    public void HeadingStyle_MergeOver_OverridesFontSize()
    {
        var baseStyle = new HeadingStyle { FontSize = 20 };
        var overlay = new HeadingStyle { FontSize = 32 };
        var merged = overlay.MergeOver(baseStyle);
        Assert.Equal(32, merged.FontSize);
    }

    [Fact]
    public void CheckboxStyle_MergeOver_OverridesBoxSize()
    {
        var baseStyle = new CheckboxStyle { BoxSize = 18 };
        var overlay = new CheckboxStyle { BoxSize = 24 };
        var merged = overlay.MergeOver(baseStyle);
        Assert.Equal(24, merged.BoxSize);
    }

    [Fact]
    public void SliderStyle_MergeOver_OverridesTrackHeight()
    {
        var baseStyle = new SliderStyle { TrackHeight = 8 };
        var overlay = new SliderStyle { TrackHeight = 16 };
        var merged = overlay.MergeOver(baseStyle);
        Assert.Equal(16, merged.TrackHeight);
    }

    [Fact]
    public void ToggleStyle_MergeOver_OverridesKnobSize()
    {
        var baseStyle = new ToggleStyle { KnobSize = 20 };
        var overlay = new ToggleStyle { KnobSize = 28 };
        var merged = overlay.MergeOver(baseStyle);
        Assert.Equal(28, merged.KnobSize);
    }

    [Fact]
    public void ProgressBarStyle_MergeOver_OverridesHeight()
    {
        var baseStyle = new ProgressBarStyle { Height = 8 };
        var overlay = new ProgressBarStyle { Height = 16 };
        var merged = overlay.MergeOver(baseStyle);
        Assert.Equal(16, merged.Height);
    }

    [Fact]
    public void PanelStyle_MergeOver_OverridesChildGap()
    {
        var baseStyle = new PanelStyle { ChildGap = 12 };
        var overlay = new PanelStyle { ChildGap = 24 };
        var merged = overlay.MergeOver(baseStyle);
        Assert.Equal(24, merged.ChildGap);
    }

    [Fact]
    public void WindowStyle_MergeOver_OverridesMinWidth()
    {
        var baseStyle = new WindowStyle { MinWidth = 150 };
        var overlay = new WindowStyle { MinWidth = 300 };
        var merged = overlay.MergeOver(baseStyle);
        Assert.Equal(300, merged.MinWidth);
    }

    [Fact]
    public void RadioGroupStyle_MergeOver_OverridesCircleSize()
    {
        var baseStyle = new RadioGroupStyle { CircleSize = 18 };
        var overlay = new RadioGroupStyle { CircleSize = 24 };
        var merged = overlay.MergeOver(baseStyle);
        Assert.Equal(24, merged.CircleSize);
    }

    [Fact]
    public void ScrollbarStyle_MergeOver_OverridesWidth()
    {
        var baseStyle = new ScrollbarStyle { Width = 8 };
        var overlay = new ScrollbarStyle { Width = 12 };
        var merged = overlay.MergeOver(baseStyle);
        Assert.Equal(12, merged.Width);
    }

    [Fact]
    public void SplitterStyle_MergeOver_OverridesThickness()
    {
        var baseStyle = new SplitterStyle { Thickness = 5 };
        var overlay = new SplitterStyle { Thickness = 10 };
        var merged = overlay.MergeOver(baseStyle);
        Assert.Equal(10, merged.Thickness);
    }

    [Fact]
    public void ScrollAreaStyle_MergeOver_OverridesPadding()
    {
        var baseStyle = new ScrollAreaStyle { Padding = Padding.All(8) };
        var overlay = new ScrollAreaStyle { Padding = Padding.All(16) };
        var merged = overlay.MergeOver(baseStyle);
        Assert.Equal(16, merged.Padding.Left);
    }

    [Fact]
    public void TooltipStyle_HasSensibleDefaults()
    {
        var s = new TooltipStyle();
        Assert.True(s.FontSize > 0);
        Assert.True(s.BackgroundColor.A > 0);
        Assert.True(s.MaxWidth > 0);
    }

    [Fact]
    public void TooltipStyle_MergeOver_OverridesMaxWidth()
    {
        var baseStyle = new TooltipStyle { MaxWidth = 300 };
        var overlay = new TooltipStyle { MaxWidth = 500 };
        var merged = overlay.MergeOver(baseStyle);
        Assert.Equal(500, merged.MaxWidth);
    }

    [Fact]
    public void ModalStyle_HasSensibleDefaults()
    {
        var s = new ModalStyle();
        Assert.True(s.MinWidth > 0);
        Assert.True(s.BackgroundColor.A > 0);
    }

    [Fact]
    public void ModalStyle_MergeOver_OverridesMinWidth()
    {
        var baseStyle = new ModalStyle { MinWidth = 300 };
        var overlay = new ModalStyle { MinWidth = 400 };
        var merged = overlay.MergeOver(baseStyle);
        Assert.Equal(400, merged.MinWidth);
    }

    [Fact]
    public void DockSpaceStyle_HasSensibleDefaults()
    {
        var s = new DockSpaceStyle();
        Assert.True(s.TabBarHeight > 0);
        Assert.True(s.SplitterThickness > 0);
        Assert.True(s.FontSize > 0);
    }

    [Fact]
    public void DockSpaceStyle_MergeOver_OverridesTabBarHeight()
    {
        var baseStyle = new DockSpaceStyle { TabBarHeight = 26 };
        var overlay = new DockSpaceStyle { TabBarHeight = 32 };
        var merged = overlay.MergeOver(baseStyle);
        Assert.Equal(32, merged.TabBarHeight);
    }

    [Fact]
    public void ImageStyle_MergeOver_OverridesCornerRadius()
    {
        var baseStyle = new ImageStyle { CornerRadius = CornerRadius.Zero };
        var overlay = new ImageStyle { CornerRadius = CornerRadius.All(8) };
        var merged = overlay.MergeOver(baseStyle);
        Assert.True(merged.CornerRadius.HasRadius);
    }
}
