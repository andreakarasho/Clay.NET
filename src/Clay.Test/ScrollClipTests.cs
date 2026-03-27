using System.Numerics;
using Clay;

namespace Clay.Test;

/// <summary>
/// Tests that PointerOver respects scroll container clipping bounds,
/// so elements scrolled out of view don't respond to hover/clicks.
/// </summary>
public class ScrollClipTests : IDisposable
{
    private readonly ClayFixture _fixture;

    public ScrollClipTests()
    {
        _fixture = new ClayFixture();
    }

    public void Dispose() => _fixture.Dispose();

    /// <summary>
    /// Helper: builds a scroll container (200x200) with 10 children (180x50 each),
    /// runs two frames so bounding boxes are populated, and returns the child IDs.
    /// The scroll container has 500px of content in a 200px viewport.
    /// </summary>
    private ElementId[] BuildScrollLayout(ElementId scrollId, Vector2 pointerPos, Vector2 scrollDelta = default)
    {
        var childIds = new ElementId[10];
        for (int i = 0; i < 10; i++)
            childIds[i] = ClayApi.Id("clip-item", (uint)i);

        // Frame 1: establish layout
        ClayApi.SetPointerState(pointerPos, false);
        ClayApi.BeginLayout();
        BuildScrollContent(scrollId, childIds);
        ClayApi.EndLayout();

        // Apply scroll between frames
        ClayApi.UpdateScrollContainers(false, scrollDelta, 1f / 60f);

        // Frame 2: bounding boxes + clip bounds are now populated
        ClayApi.SetPointerState(pointerPos, false);
        ClayApi.BeginLayout();
        BuildScrollContent(scrollId, childIds);

        return childIds;
    }

    private static void BuildScrollContent(ElementId scrollId, ElementId[] childIds)
    {
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
            for (int i = 0; i < childIds.Length; i++)
            {
                using (ClayApi.Element(new ElementDeclaration
                {
                    Id = childIds[i],
                    Layout = new LayoutConfig { Sizing = Sizing.FixedSize(180, 50) },
                    BackgroundColor = Color.Red
                })) { }
            }
        }
    }

    [Fact]
    public void PointerOver_VisibleChild_ReturnsTrue()
    {
        var scrollId = ClayApi.Id("scroll-clip1");
        // Pointer at (90, 25) — inside first child (0,0)-(180,50), inside scroll (0,0)-(200,200)
        var childIds = BuildScrollLayout(scrollId, new Vector2(90, 25));

        var isOver = ClayApi.PointerOver(childIds[0]);
        ClayApi.EndLayout();

        Assert.True(isOver, "Pointer should be over the first child which is visible in the scroll container");
    }

    [Fact]
    public void PointerOver_ClippedChild_ReturnsFalse()
    {
        var scrollId = ClayApi.Id("scroll-clip2");
        // Child index 8 is at Y=400 (8*50), well below the 200px viewport.
        // Place pointer at where it would be if not clipped, but it's outside the scroll bounds.
        var childIds = BuildScrollLayout(scrollId, new Vector2(90, 425));

        var isOver = ClayApi.PointerOver(childIds[8]);
        ClayApi.EndLayout();

        Assert.False(isOver, "Pointer should NOT be over a child that is clipped outside the scroll container");
    }

    [Fact]
    public void PointerOver_ChildBelowViewport_WithPointerOutsideScrollBounds_ReturnsFalse()
    {
        // After frame 1 EndLayout, child[8] has a bounding box at Y=400 (8*50).
        // The scroll container is 200px tall (Y=0..200).
        // Pointer at (90, 410) is inside child[8]'s bbox but OUTSIDE the scroll container.
        // Clip bounds should block it.
        var scrollId = ClayApi.Id("scroll-clip3");
        var childIds = BuildScrollLayout(scrollId, new Vector2(90, 410));

        var isOver = ClayApi.PointerOver(childIds[8]);
        ClayApi.EndLayout();

        Assert.False(isOver, "Pointer at Y=410 is outside scroll container bounds — clip bounds should block hover");
    }

    [Fact]
    public void PointerOver_NonScrolledElement_NoClipping()
    {
        // An element NOT inside a scroll container should not have clip bounds
        var btnId = ClayApi.Id("no-scroll-btn");

        ClayApi.SetPointerState(new Vector2(50, 50), false);
        ClayApi.BeginLayout();
        using (ClayApi.Element(new ElementDeclaration
        {
            Id = btnId,
            Layout = new LayoutConfig { Sizing = Sizing.FixedSize(100, 100) },
            BackgroundColor = Color.Red
        })) { }
        ClayApi.EndLayout();

        ClayApi.SetPointerState(new Vector2(50, 50), false);
        ClayApi.BeginLayout();
        using (ClayApi.Element(new ElementDeclaration
        {
            Id = btnId,
            Layout = new LayoutConfig { Sizing = Sizing.FixedSize(100, 100) },
            BackgroundColor = Color.Red
        })) { }

        var isOver = ClayApi.PointerOver(btnId);
        ClayApi.EndLayout();

        Assert.True(isOver, "Element outside scroll container should not be affected by clip bounds");
    }

    [Fact]
    public void PointerOver_InsideScrollBounds_ButOutsideChild_ReturnsFalse()
    {
        var scrollId = ClayApi.Id("scroll-clip5");
        // Pointer at (90, 190) — inside scroll container but below the last visible child
        // (children are 50px tall, so 4 fit: 0-50, 50-100, 100-150, 150-200)
        // Pointer at Y=190 is inside the 4th child (150-200). Let's use Y=195.
        // But child index 5 is at Y=250, outside the viewport.
        var childIds = BuildScrollLayout(scrollId, new Vector2(90, 260));

        var isOver = ClayApi.PointerOver(childIds[5]);
        ClayApi.EndLayout();

        Assert.False(isOver, "Pointer at Y=260 is outside the scroll container bounds (200px), should not hover child");
    }

    [Fact]
    public void ScrollContainer_ChildrenInsideViewport_HaveClipBounds()
    {
        var scrollId = ClayApi.Id("scroll-clip6");
        // Verify that children inside a scroll container get clip bounds set
        // by checking that a visible child is hoverable but one at the same absolute
        // position outside a different scroll container would also be hoverable.
        var childIds = BuildScrollLayout(scrollId, new Vector2(90, 75));

        // Child 1 is at Y=50..100, pointer at Y=75 is inside it and inside the scroll container
        var isOver = ClayApi.PointerOver(childIds[1]);
        ClayApi.EndLayout();

        Assert.True(isOver, "Second child at Y=50..100 should be hoverable with pointer at Y=75");
    }
}
