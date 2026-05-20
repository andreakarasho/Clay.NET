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
        Assert.Equal(8, decl.Layout!.Value.ChildGap);
    }

    [Fact]
    public void Box_SetsLayoutAndColor()
    {
        var decl = ElementDeclaration.Box(LayoutConfig.FillRow(), Color.Red);
        Assert.Equal(Color.Red.R, decl.BackgroundColor!.Value.R);
    }

    [Fact]
    public void RoundedBox_SetsCornerRadius()
    {
        var decl = ElementDeclaration.RoundedBox(LayoutConfig.FillRow(), Color.Red, 8f);
        Assert.True(decl.CornerRadius!.Value.HasRadius);
    }
}

public class ShadowConfigTests
{
    [Fact]
    public void Default_HasNoShadow()
    {
        Assert.False(new ShadowConfig().HasShadow);
    }

    [Fact]
    public void Drop_HasShadow()
    {
        var s = ShadowConfig.Drop(2, 4, 8, Color.Black);
        Assert.True(s.HasShadow);
        Assert.Equal(2f, s.OffsetX);
        Assert.Equal(4f, s.OffsetY);
        Assert.Equal(8f, s.BlurRadius);
    }

    [Fact]
    public void Uniform_SetsEqualOffset()
    {
        var s = ShadowConfig.Uniform(5, 10, Color.Black);
        Assert.Equal(5f, s.OffsetX);
        Assert.Equal(5f, s.OffsetY);
        Assert.Equal(10f, s.BlurRadius);
    }

    [Fact]
    public void Ambient_HasZeroOffset()
    {
        var s = ShadowConfig.Ambient(12, Color.Black);
        Assert.Equal(0f, s.OffsetX);
        Assert.Equal(0f, s.OffsetY);
        Assert.Equal(12f, s.BlurRadius);
    }

    [Fact]
    public void Create_SetsAllFields()
    {
        var s = ShadowConfig.Create(1, 2, 3, 4, Color.Red);
        Assert.Equal(1f, s.OffsetX);
        Assert.Equal(2f, s.OffsetY);
        Assert.Equal(3f, s.BlurRadius);
        Assert.Equal(4f, s.SpreadRadius);
    }

    [Fact]
    public void HasShadow_TransparentColor_ReturnsFalse()
    {
        var s = ShadowConfig.Drop(5, 5, 10, Color.Transparent);
        Assert.False(s.HasShadow);
    }
}

public class FloatingAttachPointsTests
{
    [Fact]
    public void TopLeft_HasCorrectValues()
    {
        Assert.Equal(FloatingAttachPoint.LeftTop, FloatingAttachPoints.TopLeft.Element);
        Assert.Equal(FloatingAttachPoint.LeftTop, FloatingAttachPoints.TopLeft.Parent);
    }

    [Fact]
    public void Center_HasCorrectValues()
    {
        Assert.Equal(FloatingAttachPoint.CenterCenter, FloatingAttachPoints.Center.Element);
        Assert.Equal(FloatingAttachPoint.CenterCenter, FloatingAttachPoints.Center.Parent);
    }

    [Fact]
    public void BottomRight_HasCorrectValues()
    {
        Assert.Equal(FloatingAttachPoint.RightBottom, FloatingAttachPoints.BottomRight.Element);
        Assert.Equal(FloatingAttachPoint.RightBottom, FloatingAttachPoints.BottomRight.Parent);
    }
}

public class FloatingConfigExtendedTests
{
    [Fact]
    public void AttachToElement_SetsParentId()
    {
        var c = FloatingConfig.AttachToElement(42, zIndex: 3);
        Assert.True(c.IsFloating);
        Assert.Equal(FloatingAttachTo.ElementWithId, c.AttachTo);
        Assert.Equal(42u, c.ParentId);
        Assert.Equal(3, c.ZIndex);
    }

    [Fact]
    public void Absolute_SetsRootAttach()
    {
        var c = FloatingConfig.Absolute(100, 200, zIndex: 1);
        Assert.True(c.IsFloating);
        Assert.Equal(FloatingAttachTo.Root, c.AttachTo);
        Assert.Equal(100f, c.Offset.X);
        Assert.Equal(200f, c.Offset.Y);
        Assert.Equal(1, c.ZIndex);
    }

    [Fact]
    public void IsFloating_None_ReturnsFalse()
    {
        Assert.False(new FloatingConfig { AttachTo = FloatingAttachTo.None }.IsFloating);
    }

    [Fact]
    public void IsFloating_Parent_ReturnsTrue()
    {
        Assert.True(new FloatingConfig { AttachTo = FloatingAttachTo.Parent }.IsFloating);
    }
}

public class SharedElementConfigTests
{
    [Fact]
    public void HasBackgroundColor_Transparent_ReturnsFalse()
    {
        var c = new SharedElementConfig { BackgroundColor = Color.Transparent };
        Assert.False(c.HasBackgroundColor);
    }

    [Fact]
    public void HasBackgroundColor_Opaque_ReturnsTrue()
    {
        var c = new SharedElementConfig { BackgroundColor = Color.Red };
        Assert.True(c.HasBackgroundColor);
    }

    [Fact]
    public void HasCornerRadius_Zero_ReturnsFalse()
    {
        var c = new SharedElementConfig { CornerRadius = CornerRadius.Zero };
        Assert.False(c.HasCornerRadius);
    }

    [Fact]
    public void HasCornerRadius_NonZero_ReturnsTrue()
    {
        var c = new SharedElementConfig { CornerRadius = CornerRadius.All(5) };
        Assert.True(c.HasCornerRadius);
    }
}
