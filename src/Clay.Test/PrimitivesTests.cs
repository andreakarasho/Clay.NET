using System.Numerics;
using Clay;

namespace Clay.Test;

public class DimensionsTests
{
    [Fact]
    public void Constructor_SetsWidthHeight()
    {
        var d = new Dimensions(100, 200);
        Assert.Equal(100f, d.Width);
        Assert.Equal(200f, d.Height);
    }

    [Fact]
    public void Max_ReturnsLargerDimensions()
    {
        var a = new Dimensions(10, 50);
        var b = new Dimensions(30, 20);
        var result = Dimensions.Max(a, b);
        Assert.Equal(30f, result.Width);
        Assert.Equal(50f, result.Height);
    }

    [Fact]
    public void Min_ReturnsSmallerDimensions()
    {
        var a = new Dimensions(10, 50);
        var b = new Dimensions(30, 20);
        var result = Dimensions.Min(a, b);
        Assert.Equal(10f, result.Width);
        Assert.Equal(20f, result.Height);
    }
}

public class BoundingBoxTests
{
    [Fact]
    public void Constructor_SetsFields()
    {
        var bb = new BoundingBox(10, 20, 100, 200);
        Assert.Equal(10f, bb.X);
        Assert.Equal(20f, bb.Y);
        Assert.Equal(100f, bb.Width);
        Assert.Equal(200f, bb.Height);
    }

    [Fact]
    public void Constructor_FromVectorAndDimensions()
    {
        var bb = new BoundingBox(new Vector2(5, 10), new Dimensions(50, 60));
        Assert.Equal(5f, bb.X);
        Assert.Equal(10f, bb.Y);
        Assert.Equal(50f, bb.Width);
        Assert.Equal(60f, bb.Height);
    }

    [Fact]
    public void Properties_LeftTopRightBottom()
    {
        var bb = new BoundingBox(10, 20, 100, 200);
        Assert.Equal(10f, bb.Left);
        Assert.Equal(20f, bb.Top);
        Assert.Equal(110f, bb.Right);
        Assert.Equal(220f, bb.Bottom);
    }

    [Fact]
    public void Contains_PointInside_ReturnsTrue()
    {
        var bb = new BoundingBox(0, 0, 100, 100);
        Assert.True(bb.Contains(new Vector2(50, 50)));
    }

    [Fact]
    public void Contains_PointOutside_ReturnsFalse()
    {
        var bb = new BoundingBox(0, 0, 100, 100);
        Assert.False(bb.Contains(new Vector2(150, 50)));
    }

    [Fact]
    public void Contains_PointOnEdge_ReturnsTrue()
    {
        var bb = new BoundingBox(0, 0, 100, 100);
        Assert.True(bb.Contains(new Vector2(0, 0)));
        Assert.True(bb.Contains(new Vector2(100, 100)));
    }

    [Fact]
    public void Intersects_Overlapping_ReturnsTrue()
    {
        var a = new BoundingBox(0, 0, 100, 100);
        var b = new BoundingBox(50, 50, 100, 100);
        Assert.True(a.Intersects(b));
    }

    [Fact]
    public void Intersects_NonOverlapping_ReturnsFalse()
    {
        var a = new BoundingBox(0, 0, 50, 50);
        var b = new BoundingBox(100, 100, 50, 50);
        Assert.False(a.Intersects(b));
    }
}

public class ColorTests
{
    [Fact]
    public void Rgba_CreatesCorrectColor()
    {
        var c = Color.Rgba(255, 128, 0, 200);
        Assert.Equal(255f, c.R);
        Assert.Equal(128f, c.G);
        Assert.Equal(0f, c.B);
        Assert.Equal(200f, c.A);
    }

    [Fact]
    public void Transparent_HasZeroAlpha()
    {
        Assert.Equal(0f, Color.Transparent.A);
    }

    [Fact]
    public void IsVisible_TransparentReturnsFalse()
    {
        Assert.False(Color.Transparent.IsVisible);
    }

    [Fact]
    public void IsVisible_OpaqueReturnsTrue()
    {
        Assert.True(Color.White.IsVisible);
    }

    [Fact]
    public void FromHex_ParsesCorrectly()
    {
        var c = Color.FromHex(0xFF0000FF); // Red, full alpha
        Assert.Equal(255f, c.R);
        Assert.Equal(0f, c.G);
        Assert.Equal(0f, c.B);
        Assert.Equal(255f, c.A);
    }
}

public class CornerRadiusTests
{
    [Fact]
    public void All_SetsAllCorners()
    {
        var cr = CornerRadius.All(10);
        Assert.Equal(10f, cr.TopLeft);
        Assert.Equal(10f, cr.TopRight);
        Assert.Equal(10f, cr.BottomLeft);
        Assert.Equal(10f, cr.BottomRight);
    }

    [Fact]
    public void HasRadius_ZeroReturnsFalse()
    {
        Assert.False(CornerRadius.Zero.HasRadius);
    }

    [Fact]
    public void HasRadius_NonZeroReturnsTrue()
    {
        Assert.True(CornerRadius.All(5).HasRadius);
    }
}

public class PaddingTests
{
    [Fact]
    public void All_SetsAllSides()
    {
        var p = Padding.All(10);
        Assert.Equal(10, p.Left);
        Assert.Equal(10, p.Right);
        Assert.Equal(10, p.Top);
        Assert.Equal(10, p.Bottom);
    }

    [Fact]
    public void Symmetric_SetsHorizontalVertical()
    {
        var p = Padding.Symmetric(10, 20);
        Assert.Equal(10, p.Left);
        Assert.Equal(10, p.Right);
        Assert.Equal(20, p.Top);
        Assert.Equal(20, p.Bottom);
    }

    [Fact]
    public void HorizontalTotal()
    {
        var p = Padding.LRTB(5, 10, 0, 0);
        Assert.Equal(15, p.HorizontalTotal);
    }

    [Fact]
    public void VerticalTotal()
    {
        var p = Padding.LRTB(0, 0, 5, 10);
        Assert.Equal(15, p.VerticalTotal);
    }
}

public class ElementIdTests
{
    [Fact]
    public void Hash_SameInput_SameResult()
    {
        var a = ElementId.Hash("test");
        var b = ElementId.Hash("test");
        Assert.Equal(a.Id, b.Id);
    }

    [Fact]
    public void Hash_DifferentInput_DifferentResult()
    {
        var a = ElementId.Hash("foo");
        var b = ElementId.Hash("bar");
        Assert.NotEqual(a.Id, b.Id);
    }

    [Fact]
    public void IsValid_NonZeroIdReturnsTrue()
    {
        var id = ElementId.Hash("test");
        Assert.True(id.IsValid);
    }

    [Fact]
    public void None_IsNotValid()
    {
        Assert.False(ElementId.None.IsValid);
    }

    [Fact]
    public void Equality_Operators()
    {
        var a = ElementId.Hash("same");
        var b = ElementId.Hash("same");
        Assert.True(a == b);
        Assert.False(a != b);
    }

    [Fact]
    public void HashWithOffset_ProducesDifferentIds()
    {
        var a = ElementId.Hash("item", offset: 0);
        var b = ElementId.Hash("item", offset: 1);
        Assert.NotEqual(a.Id, b.Id);
    }

    // ## separator tests: full string is hashed, display shows only before ##

    [Fact]
    public void DoubleHash_FullStringIsHashed()
    {
        // "Save##btn1" hashes the entire string, not just "btn1"
        var a = ElementId.Hash("Save##btn1");
        var b = ElementId.Hash("btn1");
        Assert.NotEqual(a.Id, b.Id);
    }

    [Fact]
    public void DoubleHash_SameFullString_SameId()
    {
        var a = ElementId.Hash("Save##btn1");
        var b = ElementId.Hash("Save##btn1");
        Assert.Equal(a.Id, b.Id);
    }

    [Fact]
    public void DoubleHash_SameLabel_DifferentIdPart_DifferentId()
    {
        // Same display "Save" but different ## suffix → different IDs
        var a = ElementId.Hash("Save##1");
        var b = ElementId.Hash("Save##2");
        Assert.NotEqual(a.Id, b.Id);
    }

    [Fact]
    public void DoubleHash_DifferentLabel_SameIdPart_DifferentId()
    {
        // Different display but same ## suffix → different IDs (full string hashed)
        var a = ElementId.Hash("Save##action");
        var b = ElementId.Hash("Load##action");
        Assert.NotEqual(a.Id, b.Id);
    }

    [Fact]
    public void GetDisplayLabel_StripsAfterDoubleHash()
    {
        var display = ElementId.GetDisplayLabel("Save##btn1");
        Assert.Equal("Save", display.ToString());
    }

    [Fact]
    public void GetDisplayLabel_NoSeparator_ReturnsFullString()
    {
        var display = ElementId.GetDisplayLabel("NoHash");
        Assert.Equal("NoHash", display.ToString());
    }

    [Fact]
    public void GetDisplayLabel_EmptyBeforeSeparator_ReturnsEmpty()
    {
        var display = ElementId.GetDisplayLabel("##hidden");
        Assert.Equal("", display.ToString());
    }

    [Fact]
    public void GetDisplayLabel_EmptyAfterSeparator()
    {
        var display = ElementId.GetDisplayLabel("Visible##");
        Assert.Equal("Visible", display.ToString());
    }

    [Fact]
    public void GetDisplayLabel_SingleHash_NotASeparator()
    {
        var display = ElementId.GetDisplayLabel("color#red");
        Assert.Equal("color#red", display.ToString());
    }

    [Fact]
    public void GetDisplayLabel_MultipleSeparators_SplitsAtFirst()
    {
        var display = ElementId.GetDisplayLabel("A##B##C");
        Assert.Equal("A", display.ToString());
    }
}
