using Clay;
using Clay.Widgets;

namespace Clay.Test;

public class TextInputFilterTests
{
    [Theory]
    [InlineData('0', true)]
    [InlineData('9', true)]
    [InlineData('-', true)]
    [InlineData('.', true)]
    [InlineData('a', false)]
    [InlineData(' ', false)]
    [InlineData('+', false)]
    public void NumbersOnly(char ch, bool expected)
        => Assert.Equal(expected, TextInputFilters.NumbersOnly(ch));

    [Theory]
    [InlineData('5', true)]
    [InlineData('-', true)]
    [InlineData('.', false)]
    [InlineData('a', false)]
    public void IntegersOnly(char ch, bool expected)
        => Assert.Equal(expected, TextInputFilters.IntegersOnly(ch));

    [Theory]
    [InlineData('0', true)]
    [InlineData('9', true)]
    [InlineData('-', false)]
    [InlineData('a', false)]
    public void DigitsOnly(char ch, bool expected)
        => Assert.Equal(expected, TextInputFilters.DigitsOnly(ch));

    [Theory]
    [InlineData('a', true)]
    [InlineData('Z', true)]
    [InlineData('0', false)]
    [InlineData(' ', false)]
    public void AlphaOnly(char ch, bool expected)
        => Assert.Equal(expected, TextInputFilters.AlphaOnly(ch));

    [Theory]
    [InlineData('a', true)]
    [InlineData('5', true)]
    [InlineData(' ', false)]
    [InlineData('-', false)]
    public void AlphaNumeric(char ch, bool expected)
        => Assert.Equal(expected, TextInputFilters.AlphaNumeric(ch));

    [Theory]
    [InlineData('0', true)]
    [InlineData('9', true)]
    [InlineData('a', true)]
    [InlineData('f', true)]
    [InlineData('A', true)]
    [InlineData('F', true)]
    [InlineData('g', false)]
    [InlineData('z', false)]
    public void HexOnly(char ch, bool expected)
        => Assert.Equal(expected, TextInputFilters.HexOnly(ch));

    [Theory]
    [InlineData('a', true)]
    [InlineData('1', true)]
    [InlineData(' ', false)]
    [InlineData('\t', false)]
    [InlineData('\n', false)]
    public void NoWhitespace(char ch, bool expected)
        => Assert.Equal(expected, TextInputFilters.NoWhitespace(ch));
}

public class TextInputStyleTests
{
    [Fact]
    public void Default_HasSensibleValues()
    {
        var s = TextInputStyle.Default;
        Assert.True(s.FontSize > 0);
        Assert.True(s.BackgroundColor.A > 0);
        Assert.True(s.FocusedBackgroundColor.A > 0);
        Assert.True(s.TextColor.A > 0);
        Assert.True(s.CursorColor.A > 0);
        Assert.True(s.SelectionColor.A > 0);
        Assert.True(s.CornerRadius.HasRadius);
        Assert.False(s.Password);
        Assert.False(s.DisableInteraction);
        Assert.Null(s.CharFilter);
    }
}

public class SkinImageTests
{
    [Fact]
    public void HasImage_NullData_ReturnsFalse()
    {
        Assert.False(new SkinImage().HasImage);
    }

    [Fact]
    public void HasImage_WithData_ReturnsTrue()
    {
        Assert.True(SkinImage.Create("tex", 64, 64).HasImage);
    }

    [Fact]
    public void Create_SetsFields()
    {
        var img = SkinImage.Create("tex", 100, 200);
        Assert.Equal("tex", img.ImageData);
        Assert.Equal(100f, img.SourceDimensions.Width);
        Assert.Equal(200f, img.SourceDimensions.Height);
        Assert.Equal(255f, img.Tint.A);
    }

    [Fact]
    public void NineSliced_SetsSlice()
    {
        var img = SkinImage.NineSliced("tex", 64, 64, NineSlice.Uniform(8));
        Assert.True(img.HasImage);
        Assert.True(img.Slice.HasSlice);
        Assert.Equal(8f, img.Slice.Top);
    }
}

public class StateImagesTests
{
    [Fact]
    public void HasImages_NoNormal_ReturnsFalse()
    {
        Assert.False(new StateImages().HasImages);
    }

    [Fact]
    public void HasImages_WithNormal_ReturnsTrue()
    {
        var si = StateImages.Create(SkinImage.Create("tex", 64, 64));
        Assert.True(si.HasImages);
    }

    [Fact]
    public void ForState_NotPressedNotHovered_ReturnsNormal()
    {
        var normal = SkinImage.Create("normal", 64, 64);
        var si = StateImages.Create(normal);
        Assert.Equal("normal", si.ForState(false, false).ImageData);
    }

    [Fact]
    public void ForState_Hovered_ReturnsHover()
    {
        var normal = SkinImage.Create("normal", 64, 64);
        var hover = SkinImage.Create("hover", 64, 64);
        var si = StateImages.Create(normal, hover);
        Assert.Equal("hover", si.ForState(false, true).ImageData);
    }

    [Fact]
    public void ForState_Pressed_ReturnsPressed()
    {
        var normal = SkinImage.Create("normal", 64, 64);
        var pressed = SkinImage.Create("pressed", 64, 64);
        var si = StateImages.Create(normal, pressed: pressed);
        Assert.Equal("pressed", si.ForState(true, false).ImageData);
    }

    [Fact]
    public void ForState_PressedAndHovered_PrefersPressed()
    {
        var normal = SkinImage.Create("normal", 64, 64);
        var hover = SkinImage.Create("hover", 64, 64);
        var pressed = SkinImage.Create("pressed", 64, 64);
        var si = StateImages.Create(normal, hover, pressed);
        Assert.Equal("pressed", si.ForState(true, true).ImageData);
    }

    [Fact]
    public void ForState_HoveredButNoHoverImage_FallsBackToNormal()
    {
        var normal = SkinImage.Create("normal", 64, 64);
        var si = StateImages.Create(normal);
        Assert.Equal("normal", si.ForState(false, true).ImageData);
    }
}

public class ClaySliceTests
{
    [Fact]
    public void Constructor_SetsLength()
    {
        var array = new int[] { 10, 20, 30, 40, 50 };
        var slice = new ClaySlice<int>(array, 1, 3);
        Assert.Equal(3, slice.Length);
    }

    [Fact]
    public void Indexer_AccessesCorrectElement()
    {
        var array = new int[] { 10, 20, 30, 40, 50 };
        var slice = new ClaySlice<int>(array, 1, 3);
        Assert.Equal(20, slice[0]);
        Assert.Equal(30, slice[1]);
        Assert.Equal(40, slice[2]);
    }

    [Fact]
    public void AsSpan_ReturnsCorrectSlice()
    {
        var array = new int[] { 10, 20, 30, 40, 50 };
        var slice = new ClaySlice<int>(array, 2, 2);
        var span = slice.AsSpan();
        Assert.Equal(2, span.Length);
        Assert.Equal(30, span[0]);
        Assert.Equal(40, span[1]);
    }

    [Fact]
    public void Indexer_IsRef_CanMutate()
    {
        var array = new int[] { 1, 2, 3 };
        var slice = new ClaySlice<int>(array, 0, 3);
        slice[1] = 99;
        Assert.Equal(99, array[1]);
    }
}

public class RenderCommandExtensionsTests : IDisposable
{
    private readonly ClayFixture _fixture;

    public RenderCommandExtensionsTests()
    {
        _fixture = new ClayFixture();
    }

    public void Dispose() => _fixture.Dispose();

    [Fact]
    public void ForEach_IteratesAllCommands()
    {
        ClayApi.BeginLayout();
        using (ClayApi.Element(new ElementDeclaration
        {
            Layout = new LayoutConfig { Sizing = Sizing.FixedSize(100, 100) },
            BackgroundColor = Color.Red
        })) { }
        var commands = ClayApi.EndLayout();

        int count = 0;
        commands.ForEach(cmd => count++);
        Assert.True(count > 0);
    }

    [Fact]
    public void ForEachWithIndex_ProvidesCorrectIndices()
    {
        ClayApi.BeginLayout();
        using (ClayApi.Element(new ElementDeclaration
        {
            Layout = new LayoutConfig { Sizing = Sizing.FixedSize(100, 100) },
            BackgroundColor = Color.Red
        })) { }
        var commands = ClayApi.EndLayout();

        int lastIndex = -1;
        commands.ForEach((cmd, idx) =>
        {
            Assert.Equal(lastIndex + 1, idx);
            lastIndex = idx;
        });
        Assert.True(lastIndex >= 0);
    }
}
