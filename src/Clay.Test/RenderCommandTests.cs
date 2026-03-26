using Clay;

namespace Clay.Test;

public class RenderCommandTests : IDisposable
{
    private readonly ClayFixture _fixture;

    public RenderCommandTests()
    {
        _fixture = new ClayFixture();
    }

    public void Dispose() => _fixture.Dispose();

    [Fact]
    public void EndLayout_ReturnsRenderCommands()
    {
        ClayApi.BeginLayout();

        using (ClayApi.Element(new ElementDeclaration
        {
            Id = ClayApi.Id("cmd-box"),
            Layout = new LayoutConfig { Sizing = Sizing.FixedSize(100, 100) },
            BackgroundColor = Color.Red
        })) { }

        var commands = ClayApi.EndLayout();
        Assert.True(commands.Length > 0);
    }

    [Fact]
    public void Rectangle_Command_HasCorrectData()
    {
        ClayApi.BeginLayout();

        using (ClayApi.Element(new ElementDeclaration
        {
            Id = ClayApi.Id("rect"),
            Layout = new LayoutConfig { Sizing = Sizing.FixedSize(100, 50) },
            BackgroundColor = Color.Red,
            CornerRadius = CornerRadius.All(8)
        })) { }

        var commands = ClayApi.EndLayout();
        RenderCommand? rectCmd = null;

        foreach (var cmd in commands)
        {
            if (cmd.CommandType == RenderCommandType.Rectangle)
            {
                rectCmd = cmd;
                break;
            }
        }

        Assert.NotNull(rectCmd);
        Assert.Equal(RenderCommandType.Rectangle, rectCmd.Value.CommandType);
        Assert.Equal(100f, rectCmd.Value.BoundingBox.Width);
        Assert.Equal(50f, rectCmd.Value.BoundingBox.Height);
    }

    [Fact]
    public void Text_Command_HasCorrectData()
    {
        ClayApi.BeginLayout();

        using (ClayApi.Element(new ElementDeclaration
        {
            Id = ClayApi.Id("text-parent"),
            Layout = new LayoutConfig { Sizing = Sizing.Fill() }
        }))
        {
            ClayApi.Text("Hello World", new TextConfig
            {
                FontSize = 24,
                TextColor = Color.White
            });
        }

        var commands = ClayApi.EndLayout();
        RenderCommand? textCmd = null;

        foreach (var cmd in commands)
        {
            if (cmd.CommandType == RenderCommandType.Text)
            {
                textCmd = cmd;
                break;
            }
        }

        Assert.NotNull(textCmd);
        Assert.Equal("Hello World", textCmd.Value.Text.Text);
        Assert.Equal(24, textCmd.Value.Text.FontSize);
    }

    [Fact]
    public void Border_Command_Generated()
    {
        ClayApi.BeginLayout();

        using (ClayApi.Element(new ElementDeclaration
        {
            Id = ClayApi.Id("bordered"),
            Layout = new LayoutConfig { Sizing = Sizing.FixedSize(100, 100) },
            Border = BorderConfig.Uniform(2, Color.White)
        })) { }

        var commands = ClayApi.EndLayout();
        bool hasBorder = false;

        foreach (var cmd in commands)
        {
            if (cmd.CommandType == RenderCommandType.Border)
            {
                hasBorder = true;
                break;
            }
        }

        Assert.True(hasBorder);
    }

    [Fact]
    public void Image_Command_Generated()
    {
        ClayApi.BeginLayout();

        var imageData = "test-image";
        using (ClayApi.Element(new ElementDeclaration
        {
            Id = ClayApi.Id("img"),
            Layout = new LayoutConfig { Sizing = Sizing.FixedSize(64, 64) },
            Image = ImageConfig.Create(imageData, 64, 64)
        })) { }

        var commands = ClayApi.EndLayout();
        bool hasImage = false;

        foreach (var cmd in commands)
        {
            if (cmd.CommandType == RenderCommandType.Image)
            {
                hasImage = true;
                Assert.Equal(imageData, cmd.Image.ImageData);
                break;
            }
        }

        Assert.True(hasImage);
    }

    [Fact]
    public void MultipleElements_GeneratesMultipleCommands()
    {
        ClayApi.BeginLayout();

        using (ClayApi.Element(new ElementDeclaration
        {
            Id = ClayApi.Id("multi-root"),
            Layout = new LayoutConfig
            {
                Sizing = Sizing.Fill(),
                Direction = LayoutDirection.TopToBottom
            },
            BackgroundColor = Color.Gray
        }))
        {
            for (int i = 0; i < 5; i++)
            {
                using (ClayApi.Element(new ElementDeclaration
                {
                    Id = ClayApi.Id("multi-item", (uint)i),
                    Layout = new LayoutConfig { Sizing = Sizing.FixedSize(100, 30) },
                    BackgroundColor = Color.Red
                })) { }
            }
        }

        var commands = ClayApi.EndLayout();
        int rectCount = 0;
        foreach (var cmd in commands)
        {
            if (cmd.CommandType == RenderCommandType.Rectangle)
                rectCount++;
        }

        // At minimum, parent + 5 children
        Assert.True(rectCount >= 6, $"Expected at least 6 rectangles, got {rectCount}");
    }

    [Fact]
    public void NoBackground_NoRectangleCommand()
    {
        ClayApi.BeginLayout();

        using (ClayApi.Element(new ElementDeclaration
        {
            Id = ClayApi.Id("no-bg"),
            Layout = new LayoutConfig { Sizing = Sizing.FixedSize(100, 100) }
            // No BackgroundColor set
        })) { }

        var commands = ClayApi.EndLayout();
        bool hasRect = false;
        foreach (var cmd in commands)
        {
            if (cmd.CommandType == RenderCommandType.Rectangle)
                hasRect = true;
        }

        Assert.False(hasRect);
    }

    [Fact]
    public void ScrollElement_GeneratesScissorCommands()
    {
        ClayApi.BeginLayout();

        using (ClayApi.Element(new ElementDeclaration
        {
            Id = ClayApi.Id("scroll-box"),
            Layout = new LayoutConfig
            {
                Sizing = Sizing.FixedSize(200, 200),
                Direction = LayoutDirection.TopToBottom
            },
            Scroll = ScrollConfig.VerticalScroll,
            BackgroundColor = Color.Gray
        }))
        {
            using (ClayApi.Element(new ElementDeclaration
            {
                Id = ClayApi.Id("scroll-content"),
                Layout = new LayoutConfig { Sizing = Sizing.FixedSize(200, 500) },
                BackgroundColor = Color.Red
            })) { }
        }

        var commands = ClayApi.EndLayout();
        bool hasScissorStart = false;
        bool hasScissorEnd = false;

        foreach (var cmd in commands)
        {
            if (cmd.CommandType == RenderCommandType.ScissorStart)
                hasScissorStart = true;
            if (cmd.CommandType == RenderCommandType.ScissorEnd)
                hasScissorEnd = true;
        }

        Assert.True(hasScissorStart, "Expected ScissorStart command for scroll container");
        Assert.True(hasScissorEnd, "Expected ScissorEnd command for scroll container");
    }
}
