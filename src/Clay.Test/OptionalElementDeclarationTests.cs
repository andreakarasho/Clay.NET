using Clay;

namespace Clay.Test;

/// <summary>
/// Tests for the nullable ElementDeclaration field contract.
/// Declaring a config field (even default) opts the element into that config.
/// Leaving the field null means the config is not emitted.
/// Id remains required.
/// </summary>
public class OptionalElementDeclarationTests : IDisposable
{
    private readonly ClayFixture _fixture;

    public OptionalElementDeclarationTests() => _fixture = new ClayFixture();

    public void Dispose() => _fixture.Dispose();

    private static int CountCommands(ReadOnlySpan<RenderCommand> commands, RenderCommandType type)
    {
        int n = 0;
        foreach (var c in commands)
            if (c.CommandType == type) n++;
        return n;
    }

    // ============ Null-by-default contract ============

    [Fact]
    public void OnlyId_NoLayout_DoesNotCrash()
    {
        ClayApi.BeginLayout();
        using (ClayApi.Element(new ElementDeclaration { Id = ClayApi.Id("only-id") })) { }
        var commands = ClayApi.EndLayout();
        Assert.Equal(0, CountCommands(commands, RenderCommandType.Rectangle));
        Assert.Equal(0, CountCommands(commands, RenderCommandType.Border));
        Assert.Equal(0, CountCommands(commands, RenderCommandType.Shadow));
        Assert.Equal(0, CountCommands(commands, RenderCommandType.Custom));
        Assert.Equal(0, CountCommands(commands, RenderCommandType.Image));
    }

    [Fact]
    public void NullBackgroundColor_DoesNotEmitRectangle()
    {
        ClayApi.BeginLayout();
        using (ClayApi.Element(new ElementDeclaration
        {
            Id = ClayApi.Id("no-bg"),
            Layout = new LayoutConfig { Sizing = Sizing.FixedSize(50, 50) }
        }))
        { }
        var commands = ClayApi.EndLayout();
        Assert.Equal(0, CountCommands(commands, RenderCommandType.Rectangle));
    }

    [Fact]
    public void NullScroll_DoesNotRegisterScrollContainer()
    {
        ClayApi.BeginLayout();
        using (ClayApi.Element(new ElementDeclaration
        {
            Id = ClayApi.Id("no-scroll"),
            Layout = new LayoutConfig { Sizing = Sizing.FixedSize(100, 100) }
        }))
        { }
        ClayApi.EndLayout();
        Assert.Equal(0, ClayApi.GetScrollContainerCount());
    }

    // ============ Declaration-is-enough contract ============

    [Fact]
    public void EmptyCustomConfig_EmitsCustomCommand()
    {
        // Regression for sentinel removal: assigning a default-constructed CustomConfig
        // (CustomData == null) MUST still emit a Custom render command.
        ClayApi.BeginLayout();
        using (ClayApi.Element(new ElementDeclaration
        {
            Id = ClayApi.Id("custom-empty"),
            Layout = new LayoutConfig { Sizing = Sizing.FixedSize(50, 50) },
            Custom = new CustomConfig()
        }))
        { }
        var commands = ClayApi.EndLayout();
        Assert.Equal(1, CountCommands(commands, RenderCommandType.Custom));
    }

    [Fact]
    public void EmptyShadowConfig_EmitsShadowCommand()
    {
        ClayApi.BeginLayout();
        using (ClayApi.Element(new ElementDeclaration
        {
            Id = ClayApi.Id("shadow-empty"),
            Layout = new LayoutConfig { Sizing = Sizing.FixedSize(50, 50) },
            Shadow = new ShadowConfig()
        }))
        { }
        var commands = ClayApi.EndLayout();
        Assert.Equal(1, CountCommands(commands, RenderCommandType.Shadow));
    }

    [Fact]
    public void EmptyBorderConfig_DoesNotCrash()
    {
        // BorderConfig with zero width is registered as a config (sentinel removed)
        // but the visual border command is skipped downstream when Width.HasBorder is false.
        // Confirms the declaration is accepted without throwing.
        ClayApi.BeginLayout();
        using (ClayApi.Element(new ElementDeclaration
        {
            Id = ClayApi.Id("border-empty"),
            Layout = new LayoutConfig { Sizing = Sizing.FixedSize(50, 50) },
            Border = new BorderConfig()
        }))
        { }
        var commands = ClayApi.EndLayout();
        Assert.Equal(0, CountCommands(commands, RenderCommandType.Border));
    }

    [Fact]
    public void EmptyScrollConfig_RegistersScrollContainer()
    {
        ClayApi.BeginLayout();
        using (ClayApi.Element(new ElementDeclaration
        {
            Id = ClayApi.Id("scroll-empty"),
            Layout = new LayoutConfig { Sizing = Sizing.FixedSize(100, 100) },
            Scroll = new ScrollConfig()
        }))
        { }
        ClayApi.EndLayout();
        Assert.True(ClayApi.GetScrollContainerCount() > 0);
    }

    [Fact]
    public void AssignedBackgroundColor_EmitsRectangle()
    {
        ClayApi.BeginLayout();
        using (ClayApi.Element(new ElementDeclaration
        {
            Id = ClayApi.Id("bg"),
            Layout = new LayoutConfig { Sizing = Sizing.FixedSize(50, 50) },
            BackgroundColor = Color.Red
        }))
        { }
        var commands = ClayApi.EndLayout();
        Assert.Equal(1, CountCommands(commands, RenderCommandType.Rectangle));
    }

    [Fact]
    public void TransparentBackgroundColor_DoesNotCrash()
    {
        // Declaration accepts a transparent color; downstream rectangle emission
        // still skips fully-invisible fills as an optimization.
        ClayApi.BeginLayout();
        using (ClayApi.Element(new ElementDeclaration
        {
            Id = ClayApi.Id("bg-transparent"),
            Layout = new LayoutConfig { Sizing = Sizing.FixedSize(50, 50) },
            BackgroundColor = new Color(0, 0, 0, 0)
        }))
        { }
        var commands = ClayApi.EndLayout();
        Assert.Equal(0, CountCommands(commands, RenderCommandType.Rectangle));
    }

    // ============ Regression: existing behavior preserved ============

    [Fact]
    public void Regression_BackgroundColor_StillEmitsRectangle()
    {
        ClayApi.BeginLayout();
        using (ClayApi.Element(new ElementDeclaration
        {
            Id = ClayApi.Id("reg-bg"),
            Layout = new LayoutConfig { Sizing = Sizing.FixedSize(100, 100) },
            BackgroundColor = Color.Blue
        }))
        { }
        var commands = ClayApi.EndLayout();
        Assert.Equal(1, CountCommands(commands, RenderCommandType.Rectangle));
    }

    [Fact]
    public void Regression_ImageConfig_WithData_EmitsImage()
    {
        ClayApi.BeginLayout();
        using (ClayApi.Element(new ElementDeclaration
        {
            Id = ClayApi.Id("reg-image"),
            Layout = new LayoutConfig { Sizing = Sizing.FixedSize(64, 64) },
            Image = ImageConfig.Create(new object(), 64, 64)
        }))
        { }
        var commands = ClayApi.EndLayout();
        Assert.Equal(1, CountCommands(commands, RenderCommandType.Image));
    }

    [Fact]
    public void Regression_ShadowDrop_EmitsShadow()
    {
        ClayApi.BeginLayout();
        using (ClayApi.Element(new ElementDeclaration
        {
            Id = ClayApi.Id("reg-shadow"),
            Layout = new LayoutConfig { Sizing = Sizing.FixedSize(50, 50) },
            Shadow = ShadowConfig.Drop(2, 2, 4, Color.Black)
        }))
        { }
        var commands = ClayApi.EndLayout();
        Assert.Equal(1, CountCommands(commands, RenderCommandType.Shadow));
    }

    [Fact]
    public void Regression_BorderUniform_EmitsBorder()
    {
        ClayApi.BeginLayout();
        using (ClayApi.Element(new ElementDeclaration
        {
            Id = ClayApi.Id("reg-border"),
            Layout = new LayoutConfig { Sizing = Sizing.FixedSize(50, 50) },
            Border = BorderConfig.Uniform(2, Color.White)
        }))
        { }
        var commands = ClayApi.EndLayout();
        Assert.Equal(1, CountCommands(commands, RenderCommandType.Border));
    }

    [Fact]
    public void Regression_ScrollVertical_RegistersScrollContainer()
    {
        ClayApi.BeginLayout();
        using (ClayApi.Element(new ElementDeclaration
        {
            Id = ClayApi.Id("reg-scroll"),
            Layout = new LayoutConfig { Sizing = Sizing.FixedSize(100, 100) },
            Scroll = ScrollConfig.VerticalScroll
        }))
        {
            using (ClayApi.Element(new ElementDeclaration
            {
                Layout = new LayoutConfig { Sizing = Sizing.FixedSize(100, 500) },
                BackgroundColor = Color.Red
            }))
            { }
        }
        ClayApi.EndLayout();
        Assert.True(ClayApi.GetScrollContainerCount() > 0);
    }

    [Fact]
    public void Regression_ContainerFactory_StillBuilds()
    {
        ClayApi.BeginLayout();
        using (ClayApi.Element(ElementDeclaration.Container(LayoutConfig.FillRow(gap: 4)))) { }
        ClayApi.EndLayout();
        Assert.True(ClayApi.GetElementCount() > 0);
    }

    [Fact]
    public void Regression_BoxFactory_EmitsRectangle()
    {
        ClayApi.BeginLayout();
        using (ClayApi.Element(ElementDeclaration.Box(
            new LayoutConfig { Sizing = Sizing.FixedSize(50, 50) },
            Color.Green))) { }
        var commands = ClayApi.EndLayout();
        Assert.Equal(1, CountCommands(commands, RenderCommandType.Rectangle));
    }

    [Fact]
    public void Regression_RoundedBoxFactory_BuildsAndRenders()
    {
        ClayApi.BeginLayout();
        using (ClayApi.Element(ElementDeclaration.RoundedBox(
            new LayoutConfig { Sizing = Sizing.FixedSize(50, 50) },
            Color.Green,
            8f))) { }
        var commands = ClayApi.EndLayout();
        Assert.Equal(1, CountCommands(commands, RenderCommandType.Rectangle));
    }

    [Fact]
    public void Regression_FloatingConfig_RegistersTreeRoot()
    {
        ClayApi.BeginLayout();
        using (ClayApi.Element(new ElementDeclaration
        {
            Id = ClayApi.Id("float-parent"),
            Layout = new LayoutConfig { Sizing = Sizing.FixedSize(200, 200) }
        }))
        {
            using (ClayApi.Element(new ElementDeclaration
            {
                Id = ClayApi.Id("float-child"),
                Layout = new LayoutConfig { Sizing = Sizing.FixedSize(50, 50) },
                Floating = new FloatingConfig { AttachTo = FloatingAttachTo.Parent }
            }))
            { }
        }
        ClayApi.EndLayout();
        // Root container + the floating element each get a tree root entry.
        Assert.True(ClayApi.GetTreeRootCount() >= 2);
    }

    [Fact]
    public void NullFloating_DoesNotAddTreeRoot()
    {
        ClayApi.BeginLayout();
        int before = ClayApi.GetTreeRootCount();
        using (ClayApi.Element(new ElementDeclaration
        {
            Id = ClayApi.Id("no-float"),
            Layout = new LayoutConfig { Sizing = Sizing.FixedSize(50, 50) }
        }))
        { }
        ClayApi.EndLayout();
        // Only the root container's tree root should exist (added during BeginLayout).
        Assert.Equal(1, ClayApi.GetTreeRootCount() - before + 1);
    }

    [Fact]
    public void Regression_CornerRadius_AppliesToRectangle()
    {
        ClayApi.BeginLayout();
        using (ClayApi.Element(new ElementDeclaration
        {
            Id = ClayApi.Id("rad"),
            Layout = new LayoutConfig { Sizing = Sizing.FixedSize(50, 50) },
            BackgroundColor = Color.Red,
            CornerRadius = CornerRadius.All(6f)
        }))
        { }
        var commands = ClayApi.EndLayout();
        Assert.Equal(1, CountCommands(commands, RenderCommandType.Rectangle));
    }

    [Fact]
    public void UserData_AssignedOrNull_DoesNotCrash()
    {
        // UserData is now nint? — both null and an assigned value must be accepted by the layout pipeline.
        ClayApi.BeginLayout();
        using (ClayApi.Element(new ElementDeclaration
        {
            Id = ClayApi.Id("ud-set"),
            Layout = new LayoutConfig { Sizing = Sizing.FixedSize(50, 50) },
            BackgroundColor = Color.Red,
            UserData = (nint)0xABCD
        }))
        { }
        using (ClayApi.Element(new ElementDeclaration
        {
            Id = ClayApi.Id("ud-null"),
            Layout = new LayoutConfig { Sizing = Sizing.FixedSize(50, 50) },
            BackgroundColor = Color.Red
        }))
        { }
        var commands = ClayApi.EndLayout();
        Assert.Equal(2, CountCommands(commands, RenderCommandType.Rectangle));
    }

    // ============ ClipContent / Scroll clip stack ============

    [Fact]
    public void NullLayout_DoesNotPushClipStack()
    {
        // Was previously checking layout.ClipContent — must tolerate null Layout.
        ClayApi.BeginLayout();
        using (ClayApi.Element(new ElementDeclaration { Id = ClayApi.Id("no-layout-clip") })) { }
        var commands = ClayApi.EndLayout();
        Assert.Equal(0, CountCommands(commands, RenderCommandType.ScissorStart));
    }

    [Fact]
    public void Regression_ClipContent_EmitsScissor()
    {
        ClayApi.BeginLayout();
        using (ClayApi.Element(new ElementDeclaration
        {
            Id = ClayApi.Id("clip"),
            Layout = new LayoutConfig
            {
                Sizing = Sizing.FixedSize(100, 100),
                ClipContent = true
            }
        }))
        {
            using (ClayApi.Element(new ElementDeclaration
            {
                Layout = new LayoutConfig { Sizing = Sizing.FixedSize(50, 50) },
                BackgroundColor = Color.Red
            }))
            { }
        }
        var commands = ClayApi.EndLayout();
        Assert.True(CountCommands(commands, RenderCommandType.ScissorStart) >= 1);
        Assert.True(CountCommands(commands, RenderCommandType.ScissorEnd) >= 1);
    }
}
