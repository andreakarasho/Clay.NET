using Clay;

namespace Clay.Test;

public class ScrollTests : IDisposable
{
    private readonly ClayFixture _fixture;

    public ScrollTests()
    {
        _fixture = new ClayFixture();
    }

    public void Dispose() => _fixture.Dispose();

    [Fact]
    public void ScrollContainer_CreatesScissorCommands()
    {
        ClayApi.BeginLayout();

        using (ClayApi.Element(new ElementDeclaration
        {
            Id = ClayApi.Id("scroll"),
            Layout = new LayoutConfig
            {
                Sizing = Sizing.FixedSize(200, 200),
                Direction = LayoutDirection.TopToBottom
            },
            Scroll = ScrollConfig.VerticalScroll,
            BackgroundColor = Color.Gray
        }))
        {
            for (int i = 0; i < 10; i++)
            {
                using (ClayApi.Element(new ElementDeclaration
                {
                    Id = ClayApi.Id("scroll-item", (uint)i),
                    Layout = new LayoutConfig { Sizing = Sizing.FixedSize(180, 50) },
                    BackgroundColor = Color.Red
                })) { }
            }
        }

        var commands = ClayApi.EndLayout();
        bool hasScissorStart = false;
        bool hasScissorEnd = false;

        foreach (var cmd in commands)
        {
            if (cmd.CommandType == RenderCommandType.ScissorStart) hasScissorStart = true;
            if (cmd.CommandType == RenderCommandType.ScissorEnd) hasScissorEnd = true;
        }

        Assert.True(hasScissorStart, "Expected ScissorStart for scroll container");
        Assert.True(hasScissorEnd, "Expected ScissorEnd for scroll container");
    }

    [Fact]
    public void GetScrollContainerData_ReturnsData()
    {
        var scrollId = ClayApi.Id("scroll-data");

        ClayApi.BeginLayout();

        using (ClayApi.Element(new ElementDeclaration
        {
            Id = scrollId,
            Layout = new LayoutConfig
            {
                Sizing = Sizing.FixedSize(200, 200),
                Direction = LayoutDirection.TopToBottom
            },
            Scroll = ScrollConfig.VerticalScroll,
            BackgroundColor = Color.Gray
        }))
        {
            for (int i = 0; i < 10; i++)
            {
                using (ClayApi.Element(new ElementDeclaration
                {
                    Id = ClayApi.Id("sd-item", (uint)i),
                    Layout = new LayoutConfig { Sizing = Sizing.FixedSize(180, 50) },
                    BackgroundColor = Color.Red
                })) { }
            }
        }

        ClayApi.EndLayout();

        var scrollData = ClayApi.GetScrollContainerData(scrollId);
        Assert.True(scrollData.Found);
    }

    [Fact]
    public void HorizontalScroll_Works()
    {
        ClayApi.BeginLayout();

        using (ClayApi.Element(new ElementDeclaration
        {
            Id = ClayApi.Id("h-scroll"),
            Layout = new LayoutConfig
            {
                Sizing = Sizing.FixedSize(200, 100),
                Direction = LayoutDirection.LeftToRight
            },
            Scroll = ScrollConfig.HorizontalScroll,
            BackgroundColor = Color.Gray
        }))
        {
            for (int i = 0; i < 10; i++)
            {
                using (ClayApi.Element(new ElementDeclaration
                {
                    Id = ClayApi.Id("hs-item", (uint)i),
                    Layout = new LayoutConfig { Sizing = Sizing.FixedSize(50, 80) },
                    BackgroundColor = Color.Red
                })) { }
            }
        }

        var commands = ClayApi.EndLayout();
        bool hasScissor = false;
        foreach (var cmd in commands)
        {
            if (cmd.CommandType == RenderCommandType.ScissorStart)
                hasScissor = true;
        }
        Assert.True(hasScissor, "Expected ScissorStart for horizontal scroll container");
    }
}
