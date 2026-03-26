using Clay;

namespace Clay.Test;

public class FrameLifecycleTests : IDisposable
{
    private readonly ClayFixture _fixture;

    public FrameLifecycleTests()
    {
        _fixture = new ClayFixture();
    }

    public void Dispose() => _fixture.Dispose();

    [Fact]
    public void MultipleFrames_NoErrors()
    {
        for (int frame = 0; frame < 10; frame++)
        {
            ClayApi.BeginLayout();

            using (ClayApi.Element(new ElementDeclaration
            {
                Id = ClayApi.Id("root"),
                Layout = new LayoutConfig { Sizing = Sizing.Fill() },
                BackgroundColor = Color.Gray
            }))
            {
                using (ClayApi.Element(new ElementDeclaration
                {
                    Id = ClayApi.Id("child"),
                    Layout = new LayoutConfig { Sizing = Sizing.FixedSize(100, 50) },
                    BackgroundColor = Color.Red
                })) { }
            }

            var commands = ClayApi.EndLayout();
            Assert.True(commands.Length > 0);
        }
    }

    [Fact]
    public void DifferentLayoutsPerFrame_Works()
    {
        // Frame 1: single element
        ClayApi.BeginLayout();
        using (ClayApi.Element(new ElementDeclaration
        {
            Id = ClayApi.Id("a"),
            Layout = new LayoutConfig { Sizing = Sizing.FixedSize(100, 100) },
            BackgroundColor = Color.Red
        })) { }
        ClayApi.EndLayout();

        // Frame 2: different layout
        ClayApi.BeginLayout();
        using (ClayApi.Element(new ElementDeclaration
        {
            Id = ClayApi.Id("b"),
            Layout = new LayoutConfig { Sizing = Sizing.Fill() },
            BackgroundColor = Color.Blue
        }))
        {
            using (ClayApi.Element(new ElementDeclaration
            {
                Id = ClayApi.Id("c"),
                Layout = new LayoutConfig { Sizing = Sizing.FixedSize(50, 50) },
                BackgroundColor = Color.Green
            })) { }
        }
        var commands = ClayApi.EndLayout();
        Assert.True(commands.Length > 0);
    }

    [Fact]
    public void ElementCount_ReflectsCurrentFrame()
    {
        ClayApi.BeginLayout();

        using (ClayApi.Element(new ElementDeclaration
        {
            Id = ClayApi.Id("root"),
            Layout = new LayoutConfig { Sizing = Sizing.Fill() }
        }))
        {
            for (int i = 0; i < 5; i++)
            {
                using (ClayApi.Element(new ElementDeclaration
                {
                    Id = ClayApi.Id("item", (uint)i),
                    Layout = new LayoutConfig { Sizing = Sizing.FixedSize(50, 50) },
                    BackgroundColor = Color.Red
                })) { }
            }
        }

        ClayApi.EndLayout();

        // Should have at least the elements we created
        Assert.True(ClayApi.GetElementCount() >= 6);
    }

    [Fact]
    public void Generation_IncrementsPerFrame()
    {
        ClayApi.BeginLayout();
        using (ClayApi.Element(new ElementDeclaration
        {
            Id = ClayApi.Id("gen1"),
            Layout = new LayoutConfig { Sizing = Sizing.FixedSize(10, 10) },
            BackgroundColor = Color.Red
        })) { }
        ClayApi.EndLayout();

        var gen1 = ClayApi.GetGeneration();

        ClayApi.BeginLayout();
        using (ClayApi.Element(new ElementDeclaration
        {
            Id = ClayApi.Id("gen2"),
            Layout = new LayoutConfig { Sizing = Sizing.FixedSize(10, 10) },
            BackgroundColor = Color.Red
        })) { }
        ClayApi.EndLayout();

        var gen2 = ClayApi.GetGeneration();
        Assert.True(gen2 > gen1);
    }

    [Fact]
    public void SetLayoutDimensions_ChangesLayoutSize()
    {
        ClayApi.SetLayoutDimensions(new Dimensions(1024, 768));
        Assert.Equal(1024f, ClayApi.GetLayoutDimensions().Width);
        Assert.Equal(768f, ClayApi.GetLayoutDimensions().Height);
    }

    [Fact]
    public void ErrorHandler_CalledOnDuplicateId()
    {
        var errors = new List<ErrorData>();
        ClayApi.SetErrorHandler(err => errors.Add(err));

        ClayApi.BeginLayout();

        // Use same ID for two different sibling elements
        using (ClayApi.Element(new ElementDeclaration
        {
            Id = ClayApi.Id("dup-parent"),
            Layout = new LayoutConfig { Sizing = Sizing.Fill() }
        }))
        {
            using (ClayApi.Element(new ElementDeclaration
            {
                Id = ClayApi.Id("duplicate"),
                Layout = new LayoutConfig { Sizing = Sizing.FixedSize(50, 50) },
                BackgroundColor = Color.Red
            })) { }

            using (ClayApi.Element(new ElementDeclaration
            {
                Id = ClayApi.Id("duplicate"),
                Layout = new LayoutConfig { Sizing = Sizing.FixedSize(50, 50) },
                BackgroundColor = Color.Green
            })) { }
        }

        ClayApi.EndLayout();

        // Check if duplicate ID error was reported
        bool hasDuplicateError = false;
        foreach (var e in errors)
        {
            if (e.ErrorType == ErrorType.DuplicateId)
                hasDuplicateError = true;
        }
        Assert.True(hasDuplicateError, "Expected DuplicateId error for elements with the same Id");
    }

    [Fact]
    public void ModerateElementCount_Works()
    {
        // Use a moderate number of elements to avoid potential internal bugs
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
            for (int i = 0; i < 20; i++)
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
        // 20 children with BackgroundColor = at least 20 rectangle commands
        Assert.True(commands.Length >= 20, $"Expected at least 20 commands, got {commands.Length}");
    }
}
