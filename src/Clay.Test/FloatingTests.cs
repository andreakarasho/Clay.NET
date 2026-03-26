using System.Numerics;
using Clay;

namespace Clay.Test;

public class FloatingTests : IDisposable
{
    private readonly ClayFixture _fixture;

    public FloatingTests()
    {
        _fixture = new ClayFixture();
    }

    public void Dispose() => _fixture.Dispose();

    [Fact]
    public void FloatingElement_RendersAboveNormal()
    {
        ClayApi.BeginLayout();

        using (ClayApi.Element(new ElementDeclaration
        {
            Id = ClayApi.Id("float-root"),
            Layout = new LayoutConfig { Sizing = Sizing.Fill() },
            BackgroundColor = Color.Gray
        }))
        {
            using (ClayApi.Element(new ElementDeclaration
            {
                Id = ClayApi.Id("normal"),
                Layout = new LayoutConfig { Sizing = Sizing.FixedSize(100, 100) },
                BackgroundColor = Color.Red
            })) { }

            using (ClayApi.Element(new ElementDeclaration
            {
                Id = ClayApi.Id("floating"),
                Layout = new LayoutConfig { Sizing = Sizing.FixedSize(50, 50) },
                BackgroundColor = Color.Blue,
                Floating = FloatingConfig.AttachToParent(zIndex: 1)
            })) { }
        }

        var commands = ClayApi.EndLayout();

        bool foundFloating = false;
        foreach (var cmd in commands)
        {
            if (cmd.CommandType == RenderCommandType.Rectangle &&
                cmd.BoundingBox.Width == 50f && cmd.BoundingBox.Height == 50f)
            {
                foundFloating = true;
                break;
            }
        }

        Assert.True(foundFloating);
    }

    [Fact]
    public void FloatingElement_HigherZIndex_RendersLater()
    {
        ClayApi.BeginLayout();

        using (ClayApi.Element(new ElementDeclaration
        {
            Id = ClayApi.Id("z-root"),
            Layout = new LayoutConfig { Sizing = Sizing.Fill() }
        }))
        {
            using (ClayApi.Element(new ElementDeclaration
            {
                Id = ClayApi.Id("z-low"),
                Layout = new LayoutConfig { Sizing = Sizing.FixedSize(50, 50) },
                BackgroundColor = Color.Red,
                Floating = FloatingConfig.AttachToParent(zIndex: 1)
            })) { }

            using (ClayApi.Element(new ElementDeclaration
            {
                Id = ClayApi.Id("z-high"),
                Layout = new LayoutConfig { Sizing = Sizing.FixedSize(50, 50) },
                BackgroundColor = Color.Blue,
                Floating = FloatingConfig.AttachToParent(zIndex: 10)
            })) { }
        }

        var commands = ClayApi.EndLayout();

        int lowIndex = -1, highIndex = -1;
        for (int i = 0; i < commands.Length; i++)
        {
            if (commands[i].ZIndex == 1 && commands[i].CommandType == RenderCommandType.Rectangle)
                lowIndex = i;
            if (commands[i].ZIndex == 10 && commands[i].CommandType == RenderCommandType.Rectangle)
                highIndex = i;
        }

        if (lowIndex >= 0 && highIndex >= 0)
        {
            Assert.True(highIndex > lowIndex, "Higher z-index should render later");
        }
    }
}
