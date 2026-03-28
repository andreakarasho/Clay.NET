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
    public void NestedScroll_InnermostContainerScrollsFirst()
    {
        var outerId = ClayApi.Id("outer-scroll");
        var innerId = ClayApi.Id("inner-scroll");

        // Frame 1: build the layout to establish bounding boxes
        ClayApi.SetPointerState(new System.Numerics.Vector2(100, 100), false);
        ClayApi.BeginLayout();

        using (ClayApi.Element(new ElementDeclaration
        {
            Id = outerId,
            Layout = new LayoutConfig
            {
                Sizing = Sizing.FixedSize(400, 400),
                Direction = LayoutDirection.TopToBottom
            },
            Scroll = ScrollConfig.VerticalScroll
        }))
        {
            // Some content before inner scroll
            using (ClayApi.Element(new ElementDeclaration
            {
                Layout = new LayoutConfig { Sizing = Sizing.FixedSize(380, 50) }
            })) { }

            // Inner scroll container
            using (ClayApi.Element(new ElementDeclaration
            {
                Id = innerId,
                Layout = new LayoutConfig
                {
                    Sizing = Sizing.FixedSize(380, 150),
                    Direction = LayoutDirection.TopToBottom
                },
                Scroll = ScrollConfig.VerticalScroll
            }))
            {
                for (int i = 0; i < 20; i++)
                {
                    using (ClayApi.Element(new ElementDeclaration
                    {
                        Id = ClayApi.Id("inner-item", (uint)i),
                        Layout = new LayoutConfig { Sizing = Sizing.FixedSize(360, 40) }
                    })) { }
                }
            }

            // More content after
            using (ClayApi.Element(new ElementDeclaration
            {
                Layout = new LayoutConfig { Sizing = Sizing.FixedSize(380, 500) }
            })) { }
        }

        ClayApi.EndLayout();

        // Frame 2: scroll with mouse over the inner container area
        ClayApi.SetPointerState(new System.Numerics.Vector2(100, 100), false);
        ClayApi.BeginLayout();

        using (ClayApi.Element(new ElementDeclaration
        {
            Id = outerId,
            Layout = new LayoutConfig
            {
                Sizing = Sizing.FixedSize(400, 400),
                Direction = LayoutDirection.TopToBottom
            },
            Scroll = ScrollConfig.VerticalScroll
        }))
        {
            using (ClayApi.Element(new ElementDeclaration
            {
                Layout = new LayoutConfig { Sizing = Sizing.FixedSize(380, 50) }
            })) { }

            using (ClayApi.Element(new ElementDeclaration
            {
                Id = innerId,
                Layout = new LayoutConfig
                {
                    Sizing = Sizing.FixedSize(380, 150),
                    Direction = LayoutDirection.TopToBottom
                },
                Scroll = ScrollConfig.VerticalScroll
            }))
            {
                for (int i = 0; i < 20; i++)
                {
                    using (ClayApi.Element(new ElementDeclaration
                    {
                        Id = ClayApi.Id("inner-item", (uint)i),
                        Layout = new LayoutConfig { Sizing = Sizing.FixedSize(360, 40) }
                    })) { }
                }
            }

            using (ClayApi.Element(new ElementDeclaration
            {
                Layout = new LayoutConfig { Sizing = Sizing.FixedSize(380, 500) }
            })) { }
        }

        ClayApi.EndLayout();

        // Scroll down with mouse over the inner scroll area
        ClayApi.UpdateScrollContainers(false, new System.Numerics.Vector2(0, -5), 1f / 60f);

        var innerData = ClayApi.GetScrollContainerData(innerId);
        var outerData = ClayApi.GetScrollContainerData(outerId);

        Assert.True(innerData.Found, "Inner scroll container should exist");
        Assert.True(outerData.Found, "Outer scroll container should exist");
        Assert.True(innerData.ScrollPosition.Y > 0, "Inner scroll container should have scrolled");
        Assert.Equal(0f, outerData.ScrollPosition.Y);
    }

    [Fact]
    public void NestedScroll_OuterScrollsWhenMouseOutsideInner()
    {
        var outerId = ClayApi.Id("outer-scroll2");
        var innerId = ClayApi.Id("inner-scroll2");

        // Frame 1: build layout with mouse BELOW the inner container (in the outer's area)
        ClayApi.SetPointerState(new System.Numerics.Vector2(100, 350), false);
        ClayApi.BeginLayout();

        using (ClayApi.Element(new ElementDeclaration
        {
            Id = outerId,
            Layout = new LayoutConfig
            {
                Sizing = Sizing.FixedSize(400, 400),
                Direction = LayoutDirection.TopToBottom
            },
            Scroll = ScrollConfig.VerticalScroll
        }))
        {
            using (ClayApi.Element(new ElementDeclaration
            {
                Id = innerId,
                Layout = new LayoutConfig
                {
                    Sizing = Sizing.FixedSize(380, 150),
                    Direction = LayoutDirection.TopToBottom
                },
                Scroll = ScrollConfig.VerticalScroll
            }))
            {
                for (int i = 0; i < 20; i++)
                {
                    using (ClayApi.Element(new ElementDeclaration
                    {
                        Id = ClayApi.Id("inner-item2", (uint)i),
                        Layout = new LayoutConfig { Sizing = Sizing.FixedSize(360, 40) }
                    })) { }
                }
            }

            // Large content so outer can scroll
            using (ClayApi.Element(new ElementDeclaration
            {
                Layout = new LayoutConfig { Sizing = Sizing.FixedSize(380, 600) }
            })) { }
        }

        ClayApi.EndLayout();

        // Frame 2
        ClayApi.SetPointerState(new System.Numerics.Vector2(100, 350), false);
        ClayApi.BeginLayout();

        using (ClayApi.Element(new ElementDeclaration
        {
            Id = outerId,
            Layout = new LayoutConfig
            {
                Sizing = Sizing.FixedSize(400, 400),
                Direction = LayoutDirection.TopToBottom
            },
            Scroll = ScrollConfig.VerticalScroll
        }))
        {
            using (ClayApi.Element(new ElementDeclaration
            {
                Id = innerId,
                Layout = new LayoutConfig
                {
                    Sizing = Sizing.FixedSize(380, 150),
                    Direction = LayoutDirection.TopToBottom
                },
                Scroll = ScrollConfig.VerticalScroll
            }))
            {
                for (int i = 0; i < 20; i++)
                {
                    using (ClayApi.Element(new ElementDeclaration
                    {
                        Id = ClayApi.Id("inner-item2", (uint)i),
                        Layout = new LayoutConfig { Sizing = Sizing.FixedSize(360, 40) }
                    })) { }
                }
            }

            using (ClayApi.Element(new ElementDeclaration
            {
                Layout = new LayoutConfig { Sizing = Sizing.FixedSize(380, 600) }
            })) { }
        }

        ClayApi.EndLayout();

        // Mouse at y=350, which is below the inner container (150px tall at top)
        // Should scroll the outer container
        ClayApi.UpdateScrollContainers(false, new System.Numerics.Vector2(0, -5), 1f / 60f);

        var innerData = ClayApi.GetScrollContainerData(innerId);
        var outerData = ClayApi.GetScrollContainerData(outerId);

        Assert.True(outerData.Found);
        Assert.True(outerData.ScrollPosition.Y > 0, "Outer scroll container should have scrolled");
        Assert.Equal(0f, innerData.ScrollPosition.Y);
    }

    [Fact]
    public void Scroll_NotBlockedByNonRenderedWindows()
    {
        // Simulate: user had windows open on a different page, then navigated away.
        // Windows are still "open" in state but not rendered. Scroll should work.

        var scrollId = ClayApi.Id("blocked-scroll");
        var mousePos = new System.Numerics.Vector2(100, 100);

        // Frame 1: open a window that overlaps the scroll area
        ClayUI.BeginFrame(new Dimensions(800, 600), false, mousePos);

        using (ClayApi.Element(new ElementDeclaration
        {
            Layout = new LayoutConfig { Sizing = Sizing.Fill(), Direction = LayoutDirection.TopToBottom }
        }))
        {
            using (ClayApi.Element(new ElementDeclaration
            {
                Id = scrollId,
                Layout = new LayoutConfig
                {
                    Sizing = Sizing.FixedSize(400, 200),
                    Direction = LayoutDirection.TopToBottom
                },
                Scroll = ScrollConfig.VerticalScroll
            }))
            {
                for (int i = 0; i < 20; i++)
                {
                    using (ClayApi.Element(new ElementDeclaration
                    {
                        Id = ClayApi.Id("bs-item", (uint)i),
                        Layout = new LayoutConfig { Sizing = Sizing.FixedSize(380, 40) }
                    })) { }
                }
            }

            // Render a window overlapping the scroll area
            bool windowOpen = true;
            if (ClayUI.BeginWindow("BlockTest", ref windowOpen,
                defaultPosition: new System.Numerics.Vector2(50, 50),
                defaultSize: new System.Numerics.Vector2(300, 200)))
            {
                ClayUI.Label("Window content");
            }
            ClayUI.EndWindow();
        }

        ClayUI.EndFrame();

        // Frame 2: do NOT render the window (simulating navigation to a different page)
        ClayUI.BeginFrame(new Dimensions(800, 600), false, mousePos);

        using (ClayApi.Element(new ElementDeclaration
        {
            Layout = new LayoutConfig { Sizing = Sizing.Fill(), Direction = LayoutDirection.TopToBottom }
        }))
        {
            using (ClayApi.Element(new ElementDeclaration
            {
                Id = scrollId,
                Layout = new LayoutConfig
                {
                    Sizing = Sizing.FixedSize(400, 200),
                    Direction = LayoutDirection.TopToBottom
                },
                Scroll = ScrollConfig.VerticalScroll
            }))
            {
                for (int i = 0; i < 20; i++)
                {
                    using (ClayApi.Element(new ElementDeclaration
                    {
                        Id = ClayApi.Id("bs-item", (uint)i),
                        Layout = new LayoutConfig { Sizing = Sizing.FixedSize(380, 40) }
                    })) { }
                }
            }
            // No window rendered this frame
        }

        ClayUI.EndFrame();

        // Frame 3: still no window. By now stale bounds should be cleared.
        // Pass scroll delta so UpdateScrollContainers (inside BeginFrame) processes it.
        ClayUI.BeginFrame(new Dimensions(800, 600), false, mousePos, scrollDelta: new System.Numerics.Vector2(0, -5));

        // IsMouseOverAnyWindow should be false since no window was rendered
        Assert.False(ClayUI.IsMouseOverAnyWindow,
            "Non-rendered windows should not block input");

        using (ClayApi.Element(new ElementDeclaration
        {
            Layout = new LayoutConfig { Sizing = Sizing.Fill(), Direction = LayoutDirection.TopToBottom }
        }))
        {
            using (ClayApi.Element(new ElementDeclaration
            {
                Id = scrollId,
                Layout = new LayoutConfig
                {
                    Sizing = Sizing.FixedSize(400, 200),
                    Direction = LayoutDirection.TopToBottom
                },
                Scroll = ScrollConfig.VerticalScroll
            }))
            {
                for (int i = 0; i < 20; i++)
                {
                    using (ClayApi.Element(new ElementDeclaration
                    {
                        Id = ClayApi.Id("bs-item", (uint)i),
                        Layout = new LayoutConfig { Sizing = Sizing.FixedSize(380, 40) }
                    })) { }
                }
            }
        }

        ClayUI.EndFrame();

        var scrollData = ClayApi.GetScrollContainerData(scrollId);
        Assert.True(scrollData.Found);
        Assert.True(scrollData.ScrollPosition.Y > 0,
            "Scroll container should have scrolled since no window blocks it");
    }

    [Fact]
    public void VerticalScroll_DetectsHorizontalOverflow()
    {
        // A vertical scroll container with a child wider than the container
        // should report horizontal overflow for horizontal scrollbar support
        var scrollId = ClayApi.Id("hoverflow-scroll");

        ClayApi.BeginLayout();

        using (ClayApi.Element(new ElementDeclaration
        {
            Id = scrollId,
            Layout = new LayoutConfig
            {
                Sizing = Sizing.FixedSize(200, 200),
                Direction = LayoutDirection.TopToBottom
            },
            Scroll = new ScrollConfig { Vertical = true, Horizontal = true }
        }))
        {
            // Child wider than container
            using (ClayApi.Element(new ElementDeclaration
            {
                Layout = new LayoutConfig { Sizing = Sizing.FixedSize(500, 50) }
            })) { }
        }

        ClayApi.EndLayout();

        var scrollData = ClayApi.GetScrollContainerData(scrollId);
        Assert.True(scrollData.Found);
        Assert.True(scrollData.ContentDimensions.Width > scrollData.ScrollContainerDimensions.Width,
            $"Content width ({scrollData.ContentDimensions.Width}) should exceed container width ({scrollData.ScrollContainerDimensions.Width})");
        Assert.True(scrollData.OverflowsX, "Should detect horizontal overflow");
    }

    [Fact]
    public void HorizontalScroll_DetectsVerticalOverflow()
    {
        // A horizontal scroll container with a child taller than the container
        var scrollId = ClayApi.Id("voverflow-scroll");

        ClayApi.BeginLayout();

        using (ClayApi.Element(new ElementDeclaration
        {
            Id = scrollId,
            Layout = new LayoutConfig
            {
                Sizing = Sizing.FixedSize(200, 200),
                Direction = LayoutDirection.LeftToRight
            },
            Scroll = new ScrollConfig { Vertical = true, Horizontal = true }
        }))
        {
            // Child taller than container
            using (ClayApi.Element(new ElementDeclaration
            {
                Layout = new LayoutConfig { Sizing = Sizing.FixedSize(50, 500) }
            })) { }
        }

        ClayApi.EndLayout();

        var scrollData = ClayApi.GetScrollContainerData(scrollId);
        Assert.True(scrollData.Found);
        Assert.True(scrollData.ContentDimensions.Height > scrollData.ScrollContainerDimensions.Height,
            $"Content height ({scrollData.ContentDimensions.Height}) should exceed container height ({scrollData.ScrollContainerDimensions.Height})");
        Assert.True(scrollData.OverflowsY, "Should detect vertical overflow");
    }

    [Fact]
    public void VerticalWheel_RedirectsToHorizontal_WhenOnlyHorizontalScroll()
    {
        var scrollId = ClayApi.Id("honly-scroll");

        // Frame 1: build layout
        ClayApi.SetPointerState(new System.Numerics.Vector2(100, 50), false);
        ClayApi.BeginLayout();

        using (ClayApi.Element(new ElementDeclaration
        {
            Id = scrollId,
            Layout = new LayoutConfig
            {
                Sizing = Sizing.FixedSize(200, 100),
                Direction = LayoutDirection.LeftToRight
            },
            Scroll = ScrollConfig.HorizontalScroll
        }))
        {
            for (int i = 0; i < 10; i++)
            {
                using (ClayApi.Element(new ElementDeclaration
                {
                    Id = ClayApi.Id("honly-item", (uint)i),
                    Layout = new LayoutConfig { Sizing = Sizing.FixedSize(50, 80) }
                })) { }
            }
        }

        ClayApi.EndLayout();

        // Frame 2: same layout
        ClayApi.SetPointerState(new System.Numerics.Vector2(100, 50), false);
        ClayApi.BeginLayout();

        using (ClayApi.Element(new ElementDeclaration
        {
            Id = scrollId,
            Layout = new LayoutConfig
            {
                Sizing = Sizing.FixedSize(200, 100),
                Direction = LayoutDirection.LeftToRight
            },
            Scroll = ScrollConfig.HorizontalScroll
        }))
        {
            for (int i = 0; i < 10; i++)
            {
                using (ClayApi.Element(new ElementDeclaration
                {
                    Id = ClayApi.Id("honly-item", (uint)i),
                    Layout = new LayoutConfig { Sizing = Sizing.FixedSize(50, 80) }
                })) { }
            }
        }

        ClayApi.EndLayout();

        // Scroll with vertical wheel (Y delta only, no X delta)
        ClayApi.UpdateScrollContainers(false, new System.Numerics.Vector2(0, -5), 1f / 60f);

        var data = ClayApi.GetScrollContainerData(scrollId);
        Assert.True(data.Found);
        Assert.True(data.ScrollPosition.X > 0,
            "Vertical wheel should redirect to horizontal when container only scrolls horizontally");
    }

    [Fact]
    public void VerticalWheel_RedirectsToHorizontal_WhenNoVerticalOverflow()
    {
        var scrollId = ClayApi.Id("novert-scroll");

        // Frame 1: both axes enabled, but content only overflows horizontally
        ClayApi.SetPointerState(new System.Numerics.Vector2(100, 50), false);
        ClayApi.BeginLayout();

        using (ClayApi.Element(new ElementDeclaration
        {
            Id = scrollId,
            Layout = new LayoutConfig
            {
                Sizing = Sizing.FixedSize(200, 200),
                Direction = LayoutDirection.TopToBottom
            },
            Scroll = new ScrollConfig { Vertical = true, Horizontal = true }
        }))
        {
            // Single child: short but very wide
            using (ClayApi.Element(new ElementDeclaration
            {
                Id = ClayApi.Id("novert-item"),
                Layout = new LayoutConfig { Sizing = Sizing.FixedSize(600, 50) }
            })) { }
        }

        ClayApi.EndLayout();

        // Frame 2
        ClayApi.SetPointerState(new System.Numerics.Vector2(100, 50), false);
        ClayApi.BeginLayout();

        using (ClayApi.Element(new ElementDeclaration
        {
            Id = scrollId,
            Layout = new LayoutConfig
            {
                Sizing = Sizing.FixedSize(200, 200),
                Direction = LayoutDirection.TopToBottom
            },
            Scroll = new ScrollConfig { Vertical = true, Horizontal = true }
        }))
        {
            using (ClayApi.Element(new ElementDeclaration
            {
                Id = ClayApi.Id("novert-item"),
                Layout = new LayoutConfig { Sizing = Sizing.FixedSize(600, 50) }
            })) { }
        }

        ClayApi.EndLayout();

        // Vertical wheel — should redirect to horizontal since no vertical overflow
        ClayApi.UpdateScrollContainers(false, new System.Numerics.Vector2(0, -5), 1f / 60f);

        var data = ClayApi.GetScrollContainerData(scrollId);
        Assert.True(data.Found);
        Assert.True(data.ScrollPosition.X > 0,
            "Vertical wheel should redirect to horizontal when no vertical overflow exists");
        Assert.Equal(0f, data.ScrollPosition.Y);
    }

    [Fact]
    public void VerticalWheel_NotRedirected_WhenVerticalOverflowExists()
    {
        var scrollId = ClayApi.Id("bothover-scroll");

        // Frame 1: both axes enabled, content overflows both directions
        ClayApi.SetPointerState(new System.Numerics.Vector2(100, 100), false);
        ClayApi.BeginLayout();

        using (ClayApi.Element(new ElementDeclaration
        {
            Id = scrollId,
            Layout = new LayoutConfig
            {
                Sizing = Sizing.FixedSize(200, 200),
                Direction = LayoutDirection.TopToBottom
            },
            Scroll = new ScrollConfig { Vertical = true, Horizontal = true }
        }))
        {
            // Wide AND tall content
            using (ClayApi.Element(new ElementDeclaration
            {
                Id = ClayApi.Id("bothover-item"),
                Layout = new LayoutConfig { Sizing = Sizing.FixedSize(600, 600) }
            })) { }
        }

        ClayApi.EndLayout();

        // Frame 2
        ClayApi.SetPointerState(new System.Numerics.Vector2(100, 100), false);
        ClayApi.BeginLayout();

        using (ClayApi.Element(new ElementDeclaration
        {
            Id = scrollId,
            Layout = new LayoutConfig
            {
                Sizing = Sizing.FixedSize(200, 200),
                Direction = LayoutDirection.TopToBottom
            },
            Scroll = new ScrollConfig { Vertical = true, Horizontal = true }
        }))
        {
            using (ClayApi.Element(new ElementDeclaration
            {
                Id = ClayApi.Id("bothover-item"),
                Layout = new LayoutConfig { Sizing = Sizing.FixedSize(600, 600) }
            })) { }
        }

        ClayApi.EndLayout();

        // Vertical wheel — should scroll vertically since vertical overflow exists
        ClayApi.UpdateScrollContainers(false, new System.Numerics.Vector2(0, -5), 1f / 60f);

        var data = ClayApi.GetScrollContainerData(scrollId);
        Assert.True(data.Found);
        Assert.True(data.ScrollPosition.Y > 0,
            "Vertical wheel should scroll vertically when vertical overflow exists");
        Assert.Equal(0f, data.ScrollPosition.X);
    }

    [Fact]
    public void RemappedDelta_ForcesHorizontalScroll_EvenWithVerticalOverflow()
    {
        var scrollId = ClayApi.Id("shift-scroll");

        // Frame 1: both axes overflow
        ClayApi.SetPointerState(new System.Numerics.Vector2(100, 100), false);
        ClayApi.BeginLayout();

        using (ClayApi.Element(new ElementDeclaration
        {
            Id = scrollId,
            Layout = new LayoutConfig
            {
                Sizing = Sizing.FixedSize(200, 200),
                Direction = LayoutDirection.TopToBottom
            },
            Scroll = new ScrollConfig { Vertical = true, Horizontal = true }
        }))
        {
            using (ClayApi.Element(new ElementDeclaration
            {
                Id = ClayApi.Id("shift-item"),
                Layout = new LayoutConfig { Sizing = Sizing.FixedSize(600, 600) }
            })) { }
        }

        ClayApi.EndLayout();

        // Frame 2
        ClayApi.SetPointerState(new System.Numerics.Vector2(100, 100), false);
        ClayApi.BeginLayout();

        using (ClayApi.Element(new ElementDeclaration
        {
            Id = scrollId,
            Layout = new LayoutConfig
            {
                Sizing = Sizing.FixedSize(200, 200),
                Direction = LayoutDirection.TopToBottom
            },
            Scroll = new ScrollConfig { Vertical = true, Horizontal = true }
        }))
        {
            using (ClayApi.Element(new ElementDeclaration
            {
                Id = ClayApi.Id("shift-item"),
                Layout = new LayoutConfig { Sizing = Sizing.FixedSize(600, 600) }
            })) { }
        }

        ClayApi.EndLayout();

        // Caller remaps Shift+Wheel by swapping Y delta to X (like the app layer would)
        ClayApi.UpdateScrollContainers(false, new System.Numerics.Vector2(-5, 0), 1f / 60f);

        var data = ClayApi.GetScrollContainerData(scrollId);
        Assert.True(data.Found);
        Assert.True(data.ScrollPosition.X > 0,
            "Remapped horizontal delta should scroll horizontally");
        Assert.Equal(0f, data.ScrollPosition.Y);
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
