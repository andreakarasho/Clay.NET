using System.Numerics;
using Clay;

namespace Clay.Test;

public class BugReproTests : IDisposable
{
    private readonly ClayFixture _fixture;

    public BugReproTests()
    {
        _fixture = new ClayFixture();
    }

    public void Dispose() => _fixture.Dispose();

    [Fact]
    public void ManyElements_AllRender_WithCullingDisabled()
    {
        ClayApi.SetCullingEnabled(false);
        ClayApi.BeginLayout();

        using (ClayApi.Element(new ElementDeclaration
        {
            Id = ClayApi.Id("stress-root"),
            Layout = new LayoutConfig
            {
                Sizing = Sizing.Fill(),
                Direction = LayoutDirection.TopToBottom
            }
        }))
        {
            for (int i = 0; i < 100; i++)
            {
                using (ClayApi.Element(new ElementDeclaration
                {
                    Id = ClayApi.Id("stress", (uint)i),
                    Layout = new LayoutConfig { Sizing = Sizing.FixedSize(10, 10) },
                    BackgroundColor = Color.Red
                })) { }
            }
        }

        var commands = ClayApi.EndLayout();
        int rectCount = 0;
        foreach (var cmd in commands)
            if (cmd.CommandType == RenderCommandType.Rectangle)
                rectCount++;
        Assert.Equal(100, rectCount);
    }

    [Fact]
    public void ManyElements_CulledOutsideViewport()
    {
        // With culling on, elements outside viewport are not rendered
        ClayApi.SetCullingEnabled(true);
        ClayApi.BeginLayout();

        using (ClayApi.Element(new ElementDeclaration
        {
            Id = ClayApi.Id("cull-root"),
            Layout = new LayoutConfig
            {
                Sizing = Sizing.Fill(),
                Direction = LayoutDirection.TopToBottom
            }
        }))
        {
            // 100 elements of 10px height = 1000px total, viewport is 600px
            for (int i = 0; i < 100; i++)
            {
                using (ClayApi.Element(new ElementDeclaration
                {
                    Id = ClayApi.Id("cull", (uint)i),
                    Layout = new LayoutConfig { Sizing = Sizing.FixedSize(10, 10) },
                    BackgroundColor = Color.Red
                })) { }
            }
        }

        var commands = ClayApi.EndLayout();
        int rectCount = 0;
        foreach (var cmd in commands)
            if (cmd.CommandType == RenderCommandType.Rectangle)
                rectCount++;

        // Should have fewer than 100 since elements past y=600 are culled
        Assert.True(rectCount < 100, $"Expected culling to reduce render count, got {rectCount}");
        Assert.True(rectCount > 0, "Should have some visible elements");
    }

    [Fact]
    public void FloatingElement_WithOffset_AttachToParent()
    {
        var floatId = ClayApi.Id("offset-float");

        ClayApi.BeginLayout();

        using (ClayApi.Element(new ElementDeclaration
        {
            Id = ClayApi.Id("offset-root"),
            Layout = new LayoutConfig { Sizing = Sizing.Fill() }
        }))
        {
            using (ClayApi.Element(new ElementDeclaration
            {
                Id = floatId,
                Layout = new LayoutConfig { Sizing = Sizing.FixedSize(50, 50) },
                BackgroundColor = Color.Blue,
                Floating = new FloatingConfig
                {
                    Offset = new Vector2(10, 20),
                    AttachTo = FloatingAttachTo.Parent,
                    ZIndex = 1
                }
            })) { }
        }

        var commands = ClayApi.EndLayout();
        var data = ClayApi.GetElementData(floatId);
        Assert.True(data.Found);
        Assert.Equal(50f, data.BoundingBox.Width);
        Assert.Equal(50f, data.BoundingBox.Height);
    }

    [Fact]
    public void SimpleTextMeasurer_HandlesNewlines()
    {
        var measurer = new SimpleTextMeasurer();
        var singleLine = measurer.MeasureText("Hello".AsSpan(), 0, 16, 0);
        var multiLine = measurer.MeasureText("Hello\nWorld".AsSpan(), 0, 16, 0);

        // Width should be max of line widths (both "Hello" and "World" are 5 chars)
        Assert.Equal(singleLine.Width, multiLine.Width);

        // Height should be 2 lines
        Assert.Equal(singleLine.Height * 2, multiLine.Height);
    }

    [Fact]
    public void SimpleTextMeasurer_NewlineOnly()
    {
        var measurer = new SimpleTextMeasurer();
        var result = measurer.MeasureText("\n".AsSpan(), 0, 16, 0);

        // Single newline = 2 lines (empty line + empty line), width = 0
        Assert.Equal(0f, result.Width);
        float lineHeight = 16 * measurer.LineHeightRatio;
        Assert.Equal(lineHeight * 2, result.Height);
    }

    [Fact]
    public void SimpleTextMeasurer_MultipleNewlines()
    {
        var measurer = new SimpleTextMeasurer();
        var result = measurer.MeasureText("A\nBCD\nEF".AsSpan(), 0, 16, 0);

        // Width should be max line = "BCD" = 3 chars
        float charWidth = 16 * measurer.CharacterWidthRatio;
        Assert.Equal(3 * charWidth, result.Width);

        // 3 lines
        float lineHeight = 16 * measurer.LineHeightRatio;
        Assert.Equal(lineHeight * 3, result.Height);
    }
}
