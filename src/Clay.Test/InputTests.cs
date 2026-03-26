using System.Numerics;
using Clay;

namespace Clay.Test;

public class InputTests : IDisposable
{
    private readonly ClayFixture _fixture;

    public InputTests()
    {
        _fixture = new ClayFixture();
    }

    public void Dispose() => _fixture.Dispose();

    [Fact]
    public void PointerOver_InsideElement_ReturnsTrue()
    {
        var buttonId = ClayApi.Id("btn");

        // First frame to establish bounding boxes in the hash map
        ClayApi.SetPointerState(new Vector2(50, 50), false);
        ClayApi.BeginLayout();
        using (ClayApi.Element(new ElementDeclaration
        {
            Id = buttonId,
            Layout = new LayoutConfig { Sizing = Sizing.FixedSize(100, 100) },
            BackgroundColor = Color.Red
        })) { }
        ClayApi.EndLayout();

        // Second frame — hash map now has bounding box from frame 1
        ClayApi.SetPointerState(new Vector2(50, 50), false);
        ClayApi.BeginLayout();
        using (ClayApi.Element(new ElementDeclaration
        {
            Id = buttonId,
            Layout = new LayoutConfig { Sizing = Sizing.FixedSize(100, 100) },
            BackgroundColor = Color.Red
        })) { }

        var isOver = ClayApi.PointerOver(buttonId);
        ClayApi.EndLayout();

        Assert.True(isOver, "Pointer at (50,50) should be over a 100x100 element at origin");
    }

    [Fact]
    public void PointerOver_OutsideElement_ReturnsFalse()
    {
        var buttonId = ClayApi.Id("btn2");

        // First frame
        ClayApi.SetPointerState(new Vector2(200, 200), false);
        ClayApi.BeginLayout();
        using (ClayApi.Element(new ElementDeclaration
        {
            Id = buttonId,
            Layout = new LayoutConfig { Sizing = Sizing.FixedSize(100, 100) },
            BackgroundColor = Color.Red
        })) { }
        ClayApi.EndLayout();

        // Second frame
        ClayApi.SetPointerState(new Vector2(200, 200), false);
        ClayApi.BeginLayout();
        using (ClayApi.Element(new ElementDeclaration
        {
            Id = buttonId,
            Layout = new LayoutConfig { Sizing = Sizing.FixedSize(100, 100) },
            BackgroundColor = Color.Red
        })) { }

        var isOver = ClayApi.PointerOver(buttonId);
        ClayApi.EndLayout();

        Assert.False(isOver, "Pointer at (200,200) should not be over a 100x100 element at origin");
    }

    [Fact]
    public void PointerState_TracksPosition()
    {
        ClayApi.SetPointerState(new Vector2(123, 456), false);
        var state = ClayApi.GetPointerState();
        Assert.Equal(123f, state.Position.X);
        Assert.Equal(456f, state.Position.Y);
    }

    [Fact]
    public void PointerState_PressedTracking()
    {
        ClayApi.SetPointerState(new Vector2(0, 0), true);
        var state = ClayApi.GetPointerState();
        Assert.True(state.IsPressed);
    }

    [Fact]
    public void GetElementData_ValidId_ReturnsFound()
    {
        var id = ClayApi.Id("lookup");

        ClayApi.BeginLayout();
        using (ClayApi.Element(new ElementDeclaration
        {
            Id = id,
            Layout = new LayoutConfig { Sizing = Sizing.FixedSize(50, 50) },
            BackgroundColor = Color.Red
        })) { }
        ClayApi.EndLayout();

        var data = ClayApi.GetElementData(id);
        Assert.True(data.Found);
    }

    [Fact]
    public void GetElementData_InvalidId_ReturnsNotFound()
    {
        ClayApi.BeginLayout();
        using (ClayApi.Element(new ElementDeclaration
        {
            Id = ClayApi.Id("exists"),
            Layout = new LayoutConfig { Sizing = Sizing.FixedSize(50, 50) },
            BackgroundColor = Color.Red
        })) { }
        ClayApi.EndLayout();

        var data = ClayApi.GetElementData(ClayApi.Id("nonexistent"));
        Assert.False(data.Found);
    }
}
