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

public class NineSliceTests
{
    [Fact]
    public void Uniform_SetsAllSides()
    {
        var ns = NineSlice.Uniform(10);
        Assert.Equal(10f, ns.Top);
        Assert.Equal(10f, ns.Right);
        Assert.Equal(10f, ns.Bottom);
        Assert.Equal(10f, ns.Left);
    }

    [Fact]
    public void Symmetric_SetsHorizontalVertical()
    {
        var ns = NineSlice.Symmetric(10, 20);
        Assert.Equal(20f, ns.Top);
        Assert.Equal(10f, ns.Right);
        Assert.Equal(20f, ns.Bottom);
        Assert.Equal(10f, ns.Left);
    }

    [Fact]
    public void TRBL_SetsIndividualSides()
    {
        var ns = NineSlice.TRBL(1, 2, 3, 4);
        Assert.Equal(1f, ns.Top);
        Assert.Equal(2f, ns.Right);
        Assert.Equal(3f, ns.Bottom);
        Assert.Equal(4f, ns.Left);
    }

    [Fact]
    public void HasSlice_ZeroReturnsFalse()
    {
        Assert.False(NineSlice.Zero.HasSlice);
    }

    [Fact]
    public void HasSlice_NonZeroReturnsTrue()
    {
        Assert.True(NineSlice.Uniform(5).HasSlice);
    }

    [Fact]
    public void HasSlice_SingleSideReturnsTrue()
    {
        Assert.True(new NineSlice(0, 0, 0, 1).HasSlice);
    }

    [Fact]
    public void Constructor_SetsAllFields()
    {
        var ns = new NineSlice(1, 2, 3, 4);
        Assert.Equal(1f, ns.Top);
        Assert.Equal(2f, ns.Right);
        Assert.Equal(3f, ns.Bottom);
        Assert.Equal(4f, ns.Left);
    }
}

public class ColorExtendedTests
{
    [Fact]
    public void FromNormalized_ScalesTo255()
    {
        var c = Color.FromNormalized(1f, 0.5f, 0f, 1f);
        Assert.Equal(255f, c.R);
        Assert.InRange(c.G, 127f, 128f);
        Assert.Equal(0f, c.B);
        Assert.Equal(255f, c.A);
    }

    [Fact]
    public void FromHex_6Digit_AddsFullAlpha()
    {
        var c = Color.FromHex(0xFF0000);
        Assert.Equal(255f, c.R);
        Assert.Equal(0f, c.G);
        Assert.Equal(0f, c.B);
        Assert.Equal(255f, c.A);
    }

    [Fact]
    public void FromHsv_Red()
    {
        var c = Color.FromHsv(0, 1, 1);
        Assert.InRange(c.R, 254f, 255f);
        Assert.InRange(c.G, 0f, 1f);
        Assert.InRange(c.B, 0f, 1f);
    }

    [Fact]
    public void FromHsv_Green()
    {
        var c = Color.FromHsv(120, 1, 1);
        Assert.InRange(c.G, 254f, 255f);
    }

    [Fact]
    public void FromHsv_Blue()
    {
        var c = Color.FromHsv(240, 1, 1);
        Assert.InRange(c.B, 254f, 255f);
    }

    [Fact]
    public void FromHsv_Cyan()
    {
        var c = Color.FromHsv(180, 1, 1);
        Assert.InRange(c.G, 254f, 255f);
        Assert.InRange(c.B, 254f, 255f);
    }

    [Fact]
    public void FromHsv_Yellow()
    {
        var c = Color.FromHsv(60, 1, 1);
        Assert.InRange(c.R, 254f, 255f);
        Assert.InRange(c.G, 254f, 255f);
    }

    [Fact]
    public void FromHsv_Magenta()
    {
        var c = Color.FromHsv(300, 1, 1);
        Assert.InRange(c.R, 254f, 255f);
        Assert.InRange(c.B, 254f, 255f);
    }

    [Fact]
    public void ToHsv_Red_ReturnsCorrectHue()
    {
        var (h, s, v) = Color.Red.ToHsv();
        Assert.InRange(h, 0f, 1f);
        Assert.InRange(s, 0.99f, 1f);
        Assert.InRange(v, 0.99f, 1f);
    }

    [Fact]
    public void ToHsv_Black_ReturnsZeroSV()
    {
        var (_, s, v) = Color.Black.ToHsv();
        Assert.Equal(0f, s);
        Assert.Equal(0f, v);
    }

    [Fact]
    public void ToHsv_White_ReturnsZeroSaturation()
    {
        var (_, s, v) = Color.White.ToHsv();
        Assert.Equal(0f, s);
        Assert.InRange(v, 0.99f, 1f);
    }

    [Fact]
    public void ToHsv_Gray_ReturnsZeroSaturation()
    {
        var (_, s, _) = Color.Gray.ToHsv();
        Assert.Equal(0f, s);
    }

    [Fact]
    public void Constructor_DefaultAlpha255()
    {
        var c = new Color(100, 50, 25);
        Assert.Equal(255f, c.A);
    }

    [Fact]
    public void ToString_FormatsCorrectly()
    {
        Assert.Equal("rgba(255, 0, 0, 255)", Color.Red.ToString());
    }

    [Fact]
    public void StaticColors_AreOpaque()
    {
        Assert.True(Color.White.IsVisible);
        Assert.True(Color.Black.IsVisible);
        Assert.True(Color.Red.IsVisible);
        Assert.True(Color.Green.IsVisible);
        Assert.True(Color.Blue.IsVisible);
        Assert.True(Color.Yellow.IsVisible);
        Assert.True(Color.Cyan.IsVisible);
        Assert.True(Color.Magenta.IsVisible);
        Assert.True(Color.Gray.IsVisible);
    }
}
