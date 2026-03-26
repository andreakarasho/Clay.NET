using Clay;

namespace Clay.Test;

public class TextTests : IDisposable
{
    private readonly ClayFixture _fixture;

    public TextTests()
    {
        _fixture = new ClayFixture();
    }

    public void Dispose() => _fixture.Dispose();

    [Fact]
    public void Text_GeneratesTextRenderCommand()
    {
        ClayApi.BeginLayout();

        using (ClayApi.Element(new ElementDeclaration
        {
            Id = ClayApi.Id("text-host"),
            Layout = new LayoutConfig { Sizing = Sizing.Fill() }
        }))
        {
            ClayApi.Text("Hello", new TextConfig { FontSize = 16, TextColor = Color.White });
        }

        var commands = ClayApi.EndLayout();
        bool hasText = false;
        foreach (var cmd in commands)
        {
            if (cmd.CommandType == RenderCommandType.Text)
            {
                hasText = true;
                Assert.Equal("Hello", cmd.Text.Text);
                Assert.Equal(16, cmd.Text.FontSize);
            }
        }
        Assert.True(hasText);
    }

    [Fact]
    public void Text_AffectsParentFitSize()
    {
        var parentId = ClayApi.Id("text-fit");

        ClayApi.BeginLayout();

        using (ClayApi.Element(new ElementDeclaration
        {
            Id = parentId,
            Layout = new LayoutConfig
            {
                Sizing = new Sizing(SizingAxis.Fit(), SizingAxis.Fit())
            },
            BackgroundColor = Color.Gray
        }))
        {
            ClayApi.Text("Some text content", new TextConfig { FontSize = 16, TextColor = Color.White });
        }

        ClayApi.EndLayout();

        var data = ClayApi.GetElementData(parentId);
        Assert.True(data.Found);
        Assert.True(data.BoundingBox.Width > 0, "Parent width should be > 0 due to text content");
        Assert.True(data.BoundingBox.Height > 0, "Parent height should be > 0 due to text content");
    }

    [Fact]
    public void MultipleTextElements_StackInColumn()
    {
        ClayApi.BeginLayout();

        using (ClayApi.Element(new ElementDeclaration
        {
            Id = ClayApi.Id("multi-text"),
            Layout = new LayoutConfig
            {
                Sizing = Sizing.Fill(),
                Direction = LayoutDirection.TopToBottom
            }
        }))
        {
            ClayApi.Text("Line 1", new TextConfig { FontSize = 16, TextColor = Color.White });
            ClayApi.Text("Line 2", new TextConfig { FontSize = 16, TextColor = Color.White });
            ClayApi.Text("Line 3", new TextConfig { FontSize = 16, TextColor = Color.White });
        }

        var commands = ClayApi.EndLayout();
        int textCount = 0;
        foreach (var cmd in commands)
        {
            if (cmd.CommandType == RenderCommandType.Text)
                textCount++;
        }
        Assert.Equal(3, textCount);
    }

    [Fact]
    public void TextConfig_Default_HasReasonableValues()
    {
        var config = TextConfig.Default;
        Assert.True(config.FontSize >= 0);
    }
}

public class SimpleTextMeasurerTests
{
    [Fact]
    public void MeasureText_EmptyString_ReturnsZeroWidth()
    {
        var measurer = new SimpleTextMeasurer();
        var result = measurer.MeasureText("".AsSpan(), 0, 16, 0);
        Assert.Equal(0f, result.Width);
        Assert.True(result.Height > 0);
    }

    [Fact]
    public void MeasureText_NonEmpty_ReturnsPositiveDimensions()
    {
        var measurer = new SimpleTextMeasurer();
        var result = measurer.MeasureText("Hello".AsSpan(), 0, 16, 0);
        Assert.True(result.Width > 0);
        Assert.True(result.Height > 0);
    }

    [Fact]
    public void MeasureText_LongerText_WiderResult()
    {
        var measurer = new SimpleTextMeasurer();
        var short_ = measurer.MeasureText("Hi".AsSpan(), 0, 16, 0);
        var long_ = measurer.MeasureText("Hello World".AsSpan(), 0, 16, 0);
        Assert.True(long_.Width > short_.Width);
    }

    [Fact]
    public void MeasureText_LargerFontSize_LargerDimensions()
    {
        var measurer = new SimpleTextMeasurer();
        var small = measurer.MeasureText("Test".AsSpan(), 0, 12, 0);
        var large = measurer.MeasureText("Test".AsSpan(), 0, 24, 0);
        Assert.True(large.Width > small.Width);
        Assert.True(large.Height > small.Height);
    }

    [Fact]
    public void MeasureText_WithLetterSpacing_IncreaseWidth()
    {
        var measurer = new SimpleTextMeasurer();
        var normal = measurer.MeasureText("Test".AsSpan(), 0, 16, 0);
        var spaced = measurer.MeasureText("Test".AsSpan(), 0, 16, 2);
        Assert.True(spaced.Width > normal.Width);
    }
}
