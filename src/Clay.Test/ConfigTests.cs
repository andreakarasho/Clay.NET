using Clay;

namespace Clay.Test;

public class SizingTests
{
    [Fact]
    public void FixedSize_CreatesBothAxes()
    {
        var s = Sizing.FixedSize(100, 200);
        Assert.Equal(SizingType.Fixed, s.Width.Type);
        Assert.Equal(SizingType.Fixed, s.Height.Type);
    }

    [Fact]
    public void Fill_CreatesBothGrow()
    {
        var s = Sizing.Fill();
        Assert.Equal(SizingType.Grow, s.Width.Type);
        Assert.Equal(SizingType.Grow, s.Height.Type);
    }

    [Fact]
    public void FitContent_CreatesBothFit()
    {
        var s = Sizing.FitContent();
        Assert.Equal(SizingType.Fit, s.Width.Type);
        Assert.Equal(SizingType.Fit, s.Height.Type);
    }

    [Fact]
    public void SizingAxis_Fixed_SetsMinMax()
    {
        var axis = SizingAxis.Fixed(100);
        Assert.Equal(SizingType.Fixed, axis.Type);
        Assert.Equal(100f, axis.MinMax.Min);
        Assert.Equal(100f, axis.MinMax.Max);
    }

    [Fact]
    public void SizingAxis_Fit_DefaultMinMax()
    {
        var axis = SizingAxis.Fit();
        Assert.Equal(SizingType.Fit, axis.Type);
        Assert.Equal(0f, axis.MinMax.Min);
        Assert.Equal(float.MaxValue, axis.MinMax.Max);
    }

    [Fact]
    public void SizingAxis_Grow_DefaultMinMax()
    {
        var axis = SizingAxis.Grow();
        Assert.Equal(SizingType.Grow, axis.Type);
        Assert.Equal(0f, axis.MinMax.Min);
    }

    [Fact]
    public void SizingAxis_Percent_SetsPercent()
    {
        var axis = SizingAxis.PercentOf(0.5f);
        Assert.Equal(SizingType.Percent, axis.Type);
        Assert.Equal(0.5f, axis.Percent);
    }
}

public class LayoutConfigTests
{
    [Fact]
    public void Row_SetsLeftToRight()
    {
        var config = LayoutConfig.Row();
        Assert.Equal(LayoutDirection.LeftToRight, config.Direction);
    }

    [Fact]
    public void Column_SetsTopToBottom()
    {
        var config = LayoutConfig.Column();
        Assert.Equal(LayoutDirection.TopToBottom, config.Direction);
    }

    [Fact]
    public void FillRow_HasGrowSizing()
    {
        var config = LayoutConfig.FillRow();
        Assert.Equal(SizingType.Grow, config.Sizing.Width.Type);
        Assert.Equal(SizingType.Grow, config.Sizing.Height.Type);
        Assert.Equal(LayoutDirection.LeftToRight, config.Direction);
    }

    [Fact]
    public void FillColumn_HasGrowSizing()
    {
        var config = LayoutConfig.FillColumn();
        Assert.Equal(SizingType.Grow, config.Sizing.Width.Type);
        Assert.Equal(SizingType.Grow, config.Sizing.Height.Type);
        Assert.Equal(LayoutDirection.TopToBottom, config.Direction);
    }

    [Fact]
    public void Row_WithGap_SetsChildGap()
    {
        var config = LayoutConfig.Row(gap: 12);
        Assert.Equal(12, config.ChildGap);
    }
}

public class BorderConfigTests
{
    [Fact]
    public void Uniform_SetsAllSides()
    {
        var border = BorderConfig.Uniform(2, Color.Red);
        Assert.True(border.HasBorder);
        Assert.Equal(Color.Red.R, border.Color.R);
    }

    [Fact]
    public void NoBorder_HasBorderFalse()
    {
        var border = new BorderConfig();
        Assert.False(border.HasBorder);
    }
}

public class ScrollConfigTests
{
    [Fact]
    public void VerticalScroll_IsScrollable()
    {
        Assert.True(ScrollConfig.VerticalScroll.IsScrollable);
        Assert.True(ScrollConfig.VerticalScroll.Vertical);
        Assert.False(ScrollConfig.VerticalScroll.Horizontal);
    }

    [Fact]
    public void HorizontalScroll_IsScrollable()
    {
        Assert.True(ScrollConfig.HorizontalScroll.IsScrollable);
        Assert.True(ScrollConfig.HorizontalScroll.Horizontal);
        Assert.False(ScrollConfig.HorizontalScroll.Vertical);
    }

    [Fact]
    public void BothScroll_IsScrollable()
    {
        Assert.True(ScrollConfig.BothScroll.IsScrollable);
        Assert.True(ScrollConfig.BothScroll.Horizontal);
        Assert.True(ScrollConfig.BothScroll.Vertical);
    }

    [Fact]
    public void Default_NotScrollable()
    {
        var config = new ScrollConfig();
        Assert.False(config.IsScrollable);
    }
}

public class FloatingConfigTests
{
    [Fact]
    public void AttachToParent_IsFloating()
    {
        var config = FloatingConfig.AttachToParent();
        Assert.True(config.IsFloating);
    }

    [Fact]
    public void Default_NotFloating()
    {
        var config = new FloatingConfig();
        Assert.False(config.IsFloating);
    }

    [Fact]
    public void AttachToParent_SetsZIndex()
    {
        var config = FloatingConfig.AttachToParent(zIndex: 5);
        Assert.Equal(5, config.ZIndex);
    }
}

public class ImageConfigTests
{
    [Fact]
    public void Create_HasImage()
    {
        var config = ImageConfig.Create("img", 64, 64);
        Assert.True(config.HasImage);
        Assert.Equal("img", config.ImageData);
    }

    [Fact]
    public void Default_HasNoImage()
    {
        var config = new ImageConfig();
        Assert.False(config.HasImage);
    }
}

public class ElementDeclarationTests
{
    [Fact]
    public void Container_SetsLayout()
    {
        var layout = LayoutConfig.FillRow(gap: 8);
        var decl = ElementDeclaration.Container(layout);
        Assert.Equal(8, decl.Layout.ChildGap);
    }

    [Fact]
    public void Box_SetsLayoutAndColor()
    {
        var decl = ElementDeclaration.Box(LayoutConfig.FillRow(), Color.Red);
        Assert.Equal(Color.Red.R, decl.BackgroundColor.R);
    }

    [Fact]
    public void RoundedBox_SetsCornerRadius()
    {
        var decl = ElementDeclaration.RoundedBox(LayoutConfig.FillRow(), Color.Red, 8f);
        Assert.True(decl.CornerRadius.HasRadius);
    }
}
