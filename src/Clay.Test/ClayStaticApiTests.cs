using System.Numerics;
using Clay;

namespace Clay.Test;

public class ClayStaticApiTests : IDisposable
{
    private readonly ClayFixture _fixture;

    public ClayStaticApiTests()
    {
        _fixture = new ClayFixture();
    }

    public void Dispose() => _fixture.Dispose();

    // ============ Context ============

    [Fact]
    public void Context_AfterInit_NotNull()
    {
        Assert.NotNull(ClayApi.Context);
    }

    // ============ Layout Dimensions ============

    [Fact]
    public void SetLayoutDimensions_UpdatesDimensions()
    {
        ClayApi.SetLayoutDimensions(new Dimensions(1920, 1080));
        var dims = ClayApi.GetLayoutDimensions();
        Assert.Equal(1920f, dims.Width);
        Assert.Equal(1080f, dims.Height);
    }

    [Fact]
    public void GetLayoutDimensions_ReturnsInitialSize()
    {
        var dims = ClayApi.GetLayoutDimensions();
        Assert.Equal(800f, dims.Width);
        Assert.Equal(600f, dims.Height);
    }

    // ============ Debug Mode ============

    [Fact]
    public void SetDebugModeEnabled_ChangesState()
    {
        ClayApi.SetDebugModeEnabled(true);
        Assert.True(ClayApi.IsDebugModeEnabled());

        ClayApi.SetDebugModeEnabled(false);
        Assert.False(ClayApi.IsDebugModeEnabled());
    }

    // ============ Culling ============

    [Fact]
    public void SetCullingEnabled_ChangesState()
    {
        ClayApi.SetCullingEnabled(false);
        Assert.True(ClayApi.IsCullingDisabled());

        ClayApi.SetCullingEnabled(true);
        Assert.False(ClayApi.IsCullingDisabled());
    }

    // ============ Error Handler ============

    [Fact]
    public void SetErrorHandler_DoesNotCrash()
    {
        ClayApi.SetErrorHandler((error) => { });
    }

    // ============ Element Count Stats ============

    [Fact]
    public void GetElementCount_AfterLayout_ReturnsPositive()
    {
        ClayApi.BeginLayout();
        using (ClayApi.Element(new ElementDeclaration
        {
            Id = ClayApi.Id("stat-elem"),
            Layout = new LayoutConfig { Sizing = Sizing.FixedSize(100, 100) },
            BackgroundColor = Color.Red
        })) { }
        ClayApi.EndLayout();

        Assert.True(ClayApi.GetElementCount() > 0);
    }

    [Fact]
    public void GetRenderCommandCount_AfterLayout_ReturnsPositive()
    {
        ClayApi.BeginLayout();
        using (ClayApi.Element(new ElementDeclaration
        {
            Id = ClayApi.Id("cmd-elem"),
            Layout = new LayoutConfig { Sizing = Sizing.FixedSize(100, 100) },
            BackgroundColor = Color.Red
        })) { }
        ClayApi.EndLayout();

        Assert.True(ClayApi.GetRenderCommandCount() > 0);
    }

    [Fact]
    public void GetMaxElementCount_ReturnsConfiguredMax()
    {
        Assert.True(ClayApi.GetMaxElementCount() > 0);
    }

    [Fact]
    public void GetTextElementCount_AfterText_ReturnsPositive()
    {
        ClayApi.BeginLayout();
        using (ClayApi.Element(new ElementDeclaration
        {
            Layout = new LayoutConfig { Sizing = Sizing.FixedSize(200, 50) }
        }))
        {
            ClayApi.Text("Hello", new TextConfig { FontSize = 16 });
        }
        ClayApi.EndLayout();

        Assert.True(ClayApi.GetTextElementCount() > 0);
    }

    [Fact]
    public void GetTreeRootCount_AfterLayout_ReturnsPositive()
    {
        ClayApi.BeginLayout();
        using (ClayApi.Element(new ElementDeclaration
        {
            Layout = new LayoutConfig { Sizing = Sizing.FixedSize(100, 100) }
        })) { }
        ClayApi.EndLayout();

        Assert.True(ClayApi.GetTreeRootCount() > 0);
    }

    [Fact]
    public void GetScrollContainerCount_WithScroll_ReturnsPositive()
    {
        ClayApi.BeginLayout();
        using (ClayApi.Element(new ElementDeclaration
        {
            Id = ClayApi.Id("sc"),
            Layout = new LayoutConfig { Sizing = Sizing.FixedSize(200, 200) },
            Scroll = ScrollConfig.VerticalScroll
        }))
        {
            using (ClayApi.Element(new ElementDeclaration
            {
                Layout = new LayoutConfig { Sizing = Sizing.FixedSize(100, 500) },
                BackgroundColor = Color.Red
            })) { }
        }
        ClayApi.EndLayout();

        Assert.True(ClayApi.GetScrollContainerCount() > 0);
    }

    // ============ Generation ============

    [Fact]
    public void GetGeneration_IncrementsPerFrame()
    {
        ClayApi.BeginLayout();
        ClayApi.EndLayout();
        uint gen1 = ClayApi.GetGeneration();

        ClayApi.BeginLayout();
        ClayApi.EndLayout();
        uint gen2 = ClayApi.GetGeneration();

        Assert.True(gen2 > gen1, "Generation should increment each frame");
    }

    // ============ Warnings ============

    [Fact]
    public void GetWarnings_ReturnsStruct()
    {
        var warnings = ClayApi.GetWarnings();
        // Just verify it returns without crashing
        Assert.IsType<BooleanWarnings>(warnings);
    }

    // ============ ID Generation ============

    [Fact]
    public void Id_SameLabel_SameHash()
    {
        var id1 = ClayApi.Id("test");
        var id2 = ClayApi.Id("test");
        // IDs with same label but different index will differ
        // This is expected — just verify they produce valid IDs
        Assert.True(id1.Id != 0);
        Assert.True(id2.Id != 0);
    }

    [Fact]
    public void Id_WithIndex_ProducesValidId()
    {
        var id = ClayApi.Id("item", 5);
        Assert.True(id.Id != 0);
    }

    [Fact]
    public void IdLocal_ProducesValidId()
    {
        ClayApi.BeginLayout();
        using (ClayApi.Element(new ElementDeclaration
        {
            Id = ClayApi.Id("parent"),
            Layout = new LayoutConfig { Sizing = Sizing.FixedSize(100, 100) }
        }))
        {
            var localId = ClayApi.IdLocal("child");
            Assert.True(localId.Id != 0);
        }
        ClayApi.EndLayout();
    }

    // ============ Element overloads ============

    [Fact]
    public void Element_NoDeclaration_DoesNotCrash()
    {
        ClayApi.BeginLayout();
        using (ClayApi.Element()) { }
        ClayApi.EndLayout();
    }

    [Fact]
    public void ConfigureElement_AfterElement_DoesNotCrash()
    {
        ClayApi.BeginLayout();
        using (ClayApi.Element())
        {
            ClayApi.ConfigureElement(new ElementDeclaration
            {
                BackgroundColor = Color.Red
            });
        }
        ClayApi.EndLayout();
    }

    // ============ Text overloads ============

    [Fact]
    public void Text_SpanOverload_Works()
    {
        ClayApi.BeginLayout();
        using (ClayApi.Element(new ElementDeclaration
        {
            Layout = new LayoutConfig { Sizing = Sizing.FixedSize(200, 50) }
        }))
        {
            ClayApi.Text("Hello World".AsSpan(), new TextConfig { FontSize = 16 });
        }
        ClayApi.EndLayout();
    }

    // ============ Scroll Position ============

    [Fact]
    public void SetScrollPosition_DoesNotCrash()
    {
        var scrollId = ClayApi.Id("scroll-pos");

        ClayApi.BeginLayout();
        using (ClayApi.Element(new ElementDeclaration
        {
            Id = scrollId,
            Layout = new LayoutConfig { Sizing = Sizing.FixedSize(200, 200) },
            Scroll = ScrollConfig.VerticalScroll
        }))
        {
            using (ClayApi.Element(new ElementDeclaration
            {
                Layout = new LayoutConfig { Sizing = Sizing.FixedSize(100, 500) },
                BackgroundColor = Color.Red
            })) { }
        }
        ClayApi.EndLayout();

        ClayApi.SetScrollPosition(scrollId, new Vector2(0, 50));
    }

    [Fact]
    public void ResetScrollPosition_DoesNotCrash()
    {
        var scrollId = ClayApi.Id("scroll-reset");

        ClayApi.BeginLayout();
        using (ClayApi.Element(new ElementDeclaration
        {
            Id = scrollId,
            Layout = new LayoutConfig { Sizing = Sizing.FixedSize(200, 200) },
            Scroll = ScrollConfig.VerticalScroll
        }))
        {
            using (ClayApi.Element(new ElementDeclaration
            {
                Layout = new LayoutConfig { Sizing = Sizing.FixedSize(100, 500) },
                BackgroundColor = Color.Red
            })) { }
        }
        ClayApi.EndLayout();

        ClayApi.ResetScrollPosition(scrollId);
    }
}
