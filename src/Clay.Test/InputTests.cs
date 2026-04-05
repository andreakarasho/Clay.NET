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

    [Fact]
    public void CustomHitTest_ReturningFalse_BlocksHover()
    {
        var buttonId = ClayApi.Id("hittest1");

        // First frame
        ClayApi.SetPointerState(new Vector2(50, 50), false);
        ClayApi.BeginLayout();
        using (ClayApi.Element(new ElementDeclaration
        {
            Id = buttonId,
            Layout = new LayoutConfig { Sizing = Sizing.FixedSize(100, 100) },
            BackgroundColor = Color.Red
        })) { }
        ClayApi.EndLayout();

        // Set custom hit-test that always rejects
        ClayApi.CustomHitTest = (id, bounds, point) => false;

        // Second frame — pointer is inside bounds but hit-test rejects
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

        ClayApi.CustomHitTest = null;
        Assert.False(isOver, "Custom hit-test returning false should block hover");
    }

    [Fact]
    public void CustomHitTest_ReturningTrue_AllowsHover()
    {
        var buttonId = ClayApi.Id("hittest2");

        // First frame
        ClayApi.SetPointerState(new Vector2(50, 50), false);
        ClayApi.BeginLayout();
        using (ClayApi.Element(new ElementDeclaration
        {
            Id = buttonId,
            Layout = new LayoutConfig { Sizing = Sizing.FixedSize(100, 100) },
            BackgroundColor = Color.Red
        })) { }
        ClayApi.EndLayout();

        // Set custom hit-test that always accepts
        ClayApi.CustomHitTest = (id, bounds, point) => true;

        // Second frame
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

        ClayApi.CustomHitTest = null;
        Assert.True(isOver, "Custom hit-test returning true should allow hover");
    }

    [Fact]
    public void CustomHitTest_ReceivesCorrectParameters()
    {
        var buttonId = ClayApi.Id("hittest3");
        ElementId receivedId = default;
        BoundingBox receivedBounds = default;
        Vector2 receivedPoint = default;

        // First frame
        ClayApi.SetPointerState(new Vector2(25, 35), false);
        ClayApi.BeginLayout();
        using (ClayApi.Element(new ElementDeclaration
        {
            Id = buttonId,
            Layout = new LayoutConfig { Sizing = Sizing.FixedSize(100, 100) },
            BackgroundColor = Color.Red
        })) { }
        ClayApi.EndLayout();

        // Capture parameters
        ClayApi.CustomHitTest = (id, bounds, point) =>
        {
            receivedId = id;
            receivedBounds = bounds;
            receivedPoint = point;
            return true;
        };

        // Second frame
        ClayApi.SetPointerState(new Vector2(25, 35), false);
        ClayApi.BeginLayout();
        using (ClayApi.Element(new ElementDeclaration
        {
            Id = buttonId,
            Layout = new LayoutConfig { Sizing = Sizing.FixedSize(100, 100) },
            BackgroundColor = Color.Red
        })) { }

        ClayApi.PointerOver(buttonId);
        ClayApi.EndLayout();

        ClayApi.CustomHitTest = null;
        Assert.Equal(buttonId.Id, receivedId.Id);
        Assert.Equal(100f, receivedBounds.Width);
        Assert.Equal(100f, receivedBounds.Height);
        Assert.Equal(25f, receivedPoint.X);
        Assert.Equal(35f, receivedPoint.Y);
    }

    [Fact]
    public void CustomHitTest_Null_DefaultBehavior()
    {
        var buttonId = ClayApi.Id("hittest4");
        ClayApi.CustomHitTest = null;

        // First frame
        ClayApi.SetPointerState(new Vector2(50, 50), false);
        ClayApi.BeginLayout();
        using (ClayApi.Element(new ElementDeclaration
        {
            Id = buttonId,
            Layout = new LayoutConfig { Sizing = Sizing.FixedSize(100, 100) },
            BackgroundColor = Color.Red
        })) { }
        ClayApi.EndLayout();

        // Second frame — no custom hit-test, should use default AABB
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

        Assert.True(isOver, "Without custom hit-test, default AABB check should work");
    }

    [Fact]
    public void CustomHitTest_NotCalledWhenOutsideBounds()
    {
        var buttonId = ClayApi.Id("hittest5");
        bool wasCalled = false;

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

        ClayApi.CustomHitTest = (id, bounds, point) =>
        {
            wasCalled = true;
            return true;
        };

        // Second frame — pointer outside bounds
        ClayApi.SetPointerState(new Vector2(200, 200), false);
        ClayApi.BeginLayout();
        using (ClayApi.Element(new ElementDeclaration
        {
            Id = buttonId,
            Layout = new LayoutConfig { Sizing = Sizing.FixedSize(100, 100) },
            BackgroundColor = Color.Red
        })) { }

        ClayApi.PointerOver(buttonId);
        ClayApi.EndLayout();

        ClayApi.CustomHitTest = null;
        Assert.False(wasCalled, "Custom hit-test should not be called when pointer is outside bounding box");
    }
}
