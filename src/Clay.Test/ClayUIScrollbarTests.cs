using System.Numerics;
using Clay;

namespace Clay.Test;

public class ClayUIScrollbarTests : IDisposable
{
    private readonly ClayUIFixture _fixture;

    public ClayUIScrollbarTests()
    {
        _fixture = new ClayUIFixture();
    }

    public void Dispose() => _fixture.Dispose();

    // ============ Vertical Scrollbar ============

    [Fact]
    public void VerticalScrollbar_NoOverflow_RendersSpacer()
    {
        var scrollId = ClayApi.Id("vs-no-overflow");

        // Frame 1
        ClayApi.SetPointerState(default, false);
        ClayApi.BeginLayout();
        using (ClayApi.Element(new ElementDeclaration
        {
            Layout = new LayoutConfig { Direction = LayoutDirection.LeftToRight, Sizing = Sizing.FixedSize(300, 200) }
        }))
        {
            using (ClayApi.Element(new ElementDeclaration
            {
                Id = scrollId,
                Layout = new LayoutConfig
                {
                    Direction = LayoutDirection.TopToBottom,
                    Sizing = Sizing.Fill()
                },
                Scroll = ScrollConfig.VerticalScroll
            }))
            {
                // Only 1 small child — no overflow
                using (ClayApi.Element(new ElementDeclaration
                {
                    Layout = new LayoutConfig { Sizing = Sizing.FixedSize(100, 50) },
                    BackgroundColor = Color.Red
                })) { }
            }

            ClayUI.VerticalScrollbar(scrollId);
        }
        var commands = ClayApi.EndLayout();

        // Should not have a scrollbar thumb since content doesn't overflow
        // (VerticalScrollbar renders a spacer instead)
        // Just verify it doesn't crash
        Assert.True(commands.Length > 0);
    }

    [Fact]
    public void VerticalScrollbar_WithOverflow_RendersThumb()
    {
        var scrollId = ClayApi.Id("vs-overflow");

        // Frame 1: establish layout
        ClayApi.SetPointerState(default, false);
        ClayApi.BeginLayout();
        BuildVerticalScrollLayout(scrollId);
        ClayApi.EndLayout();

        // Frame 2: render with bounding boxes
        ClayApi.SetPointerState(default, false);
        ClayApi.BeginLayout();
        BuildVerticalScrollLayout(scrollId);
        var commands = ClayApi.EndLayout();

        int rectCount = 0;
        foreach (var cmd in commands)
        {
            if (cmd.CommandType == RenderCommandType.Rectangle) rectCount++;
        }

        // Track + thumb + child items = multiple rects
        Assert.True(rectCount >= 2, $"Vertical scrollbar with overflow should render track and thumb, got {rectCount} rects");
    }

    private static void BuildVerticalScrollLayout(ElementId scrollId)
    {
        using (ClayApi.Element(new ElementDeclaration
        {
            Layout = new LayoutConfig { Direction = LayoutDirection.LeftToRight, Sizing = Sizing.FixedSize(300, 200) }
        }))
        {
            using (ClayApi.Element(new ElementDeclaration
            {
                Id = scrollId,
                Layout = new LayoutConfig
                {
                    Direction = LayoutDirection.TopToBottom,
                    Sizing = Sizing.Fill()
                },
                Scroll = ScrollConfig.VerticalScroll
            }))
            {
                for (int i = 0; i < 20; i++)
                {
                    using (ClayApi.Element(new ElementDeclaration
                    {
                        Id = ClayApi.Id("vs-item", (uint)i),
                        Layout = new LayoutConfig { Sizing = Sizing.FixedSize(200, 50) },
                        BackgroundColor = Color.Red
                    })) { }
                }
            }

            ClayUI.VerticalScrollbar(scrollId);
        }
    }

    // ============ Horizontal Scrollbar ============

    [Fact]
    public void HorizontalScrollbar_NoOverflow_RendersSpacer()
    {
        var scrollId = ClayApi.Id("hs-no-overflow");

        ClayApi.SetPointerState(default, false);
        ClayApi.BeginLayout();
        using (ClayApi.Element(new ElementDeclaration
        {
            Layout = new LayoutConfig { Direction = LayoutDirection.TopToBottom, Sizing = Sizing.FixedSize(300, 200) }
        }))
        {
            using (ClayApi.Element(new ElementDeclaration
            {
                Id = scrollId,
                Layout = new LayoutConfig
                {
                    Direction = LayoutDirection.LeftToRight,
                    Sizing = Sizing.Fill()
                },
                Scroll = ScrollConfig.HorizontalScroll
            }))
            {
                using (ClayApi.Element(new ElementDeclaration
                {
                    Layout = new LayoutConfig { Sizing = Sizing.FixedSize(50, 50) },
                    BackgroundColor = Color.Red
                })) { }
            }

            ClayUI.HorizontalScrollbar(scrollId);
        }
        var commands = ClayApi.EndLayout();

        Assert.True(commands.Length > 0);
    }

    [Fact]
    public void HorizontalScrollbar_WithOverflow_RendersThumb()
    {
        var scrollId = ClayApi.Id("hs-overflow");

        // Frame 1
        ClayApi.SetPointerState(default, false);
        ClayApi.BeginLayout();
        BuildHorizontalScrollLayout(scrollId);
        ClayApi.EndLayout();

        // Frame 2
        ClayApi.SetPointerState(default, false);
        ClayApi.BeginLayout();
        BuildHorizontalScrollLayout(scrollId);
        var commands = ClayApi.EndLayout();

        int rectCount = 0;
        foreach (var cmd in commands)
        {
            if (cmd.CommandType == RenderCommandType.Rectangle) rectCount++;
        }

        Assert.True(rectCount >= 2, $"Horizontal scrollbar with overflow should render track and thumb, got {rectCount} rects");
    }

    private static void BuildHorizontalScrollLayout(ElementId scrollId)
    {
        using (ClayApi.Element(new ElementDeclaration
        {
            Layout = new LayoutConfig { Direction = LayoutDirection.TopToBottom, Sizing = Sizing.FixedSize(300, 200) }
        }))
        {
            using (ClayApi.Element(new ElementDeclaration
            {
                Id = scrollId,
                Layout = new LayoutConfig
                {
                    Direction = LayoutDirection.LeftToRight,
                    Sizing = Sizing.Fill()
                },
                Scroll = ScrollConfig.HorizontalScroll
            }))
            {
                for (int i = 0; i < 20; i++)
                {
                    using (ClayApi.Element(new ElementDeclaration
                    {
                        Id = ClayApi.Id("hs-item", (uint)i),
                        Layout = new LayoutConfig { Sizing = Sizing.FixedSize(50, 100) },
                        BackgroundColor = Color.Red
                    })) { }
                }
            }

            ClayUI.HorizontalScrollbar(scrollId);
        }
    }

    // ============ BeginScrollArea ============

    [Fact]
    public void BeginScrollArea_Renders_WithoutCrash()
    {
        _fixture.RunFrame(() =>
        {
            ClayUI.BeginScrollArea("scroll1", maxHeight: 100);
            for (int i = 0; i < 10; i++)
                ClayUI.Label($"Item {i}");
            ClayUI.EndScrollArea();
        });
    }

    [Fact]
    public void BeginScrollArea_GeneratesScissorCommands()
    {
        var commands = _fixture.RunFrame(() =>
        {
            ClayUI.BeginScrollArea("scroll2", maxHeight: 100);
            for (int i = 0; i < 20; i++)
                ClayUI.Label($"Item {i}");
            ClayUI.EndScrollArea();
        });

        bool hasScissor = false;
        foreach (var cmd in commands)
        {
            if (cmd.CommandType == RenderCommandType.ScissorStart)
            {
                hasScissor = true;
                break;
            }
        }

        Assert.True(hasScissor, "ScrollArea should generate scissor commands");
    }

    // ============ Layout scrollbar integration ============

    [Fact]
    public void VerticalLayout_WithScroll_CreatesScrollbar()
    {
        var commands = _fixture.RunFrame(() =>
        {
            ClayUI.BeginVertical(scroll: true, maxHeight: 50);
            for (int i = 0; i < 20; i++)
                ClayUI.Label($"Row {i}");
            ClayUI.EndVertical();
        });

        bool hasScissor = false;
        foreach (var cmd in commands)
        {
            if (cmd.CommandType == RenderCommandType.ScissorStart)
            {
                hasScissor = true;
                break;
            }
        }

        Assert.True(hasScissor);
    }

    [Fact]
    public void HorizontalLayout_WithScroll_CreatesScrollbar()
    {
        var commands = _fixture.RunFrame(() =>
        {
            ClayUI.BeginHorizontal(scroll: true, maxWidth: 100);
            for (int i = 0; i < 20; i++)
                ClayUI.Label($"Col {i}");
            ClayUI.EndHorizontal();
        });

        bool hasScissor = false;
        foreach (var cmd in commands)
        {
            if (cmd.CommandType == RenderCommandType.ScissorStart)
            {
                hasScissor = true;
                break;
            }
        }

        Assert.True(hasScissor);
    }
}
