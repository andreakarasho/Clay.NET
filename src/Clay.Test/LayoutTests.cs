using Clay;

namespace Clay.Test;

public class LayoutTests : IDisposable
{
    private readonly ClayFixture _fixture;

    public LayoutTests()
    {
        _fixture = new ClayFixture(800, 600);
    }

    public void Dispose() => _fixture.Dispose();

    private ReadOnlySpan<RenderCommand> RunLayout(Action buildUi)
    {
        ClayApi.BeginLayout();
        buildUi();
        return ClayApi.EndLayout();
    }

    [Fact]
    public void SingleElement_FixedSize_CorrectDimensions()
    {
        var id = ClayApi.Id("box");
        RunLayout(() =>
        {
            using (ClayApi.Element(new ElementDeclaration
            {
                Id = id,
                Layout = new LayoutConfig { Sizing = Sizing.FixedSize(200, 100) },
                BackgroundColor = Color.Red
            })) { }
        });

        var data = ClayApi.GetElementData(id);
        Assert.True(data.Found);
        Assert.Equal(200f, data.BoundingBox.Width);
        Assert.Equal(100f, data.BoundingBox.Height);
    }

    [Fact]
    public void SingleElement_Fill_FillsParentDimensions()
    {
        var id = ClayApi.Id("root");
        RunLayout(() =>
        {
            using (ClayApi.Element(new ElementDeclaration
            {
                Id = id,
                Layout = new LayoutConfig { Sizing = Sizing.Fill() },
                BackgroundColor = Color.Blue
            })) { }
        });

        var data = ClayApi.GetElementData(id);
        Assert.True(data.Found);
        // Root-level Grow element should fill layout dimensions
        Assert.True(data.BoundingBox.Width > 0);
        Assert.True(data.BoundingBox.Height > 0);
    }

    [Fact]
    public void Row_ChildrenSideBySide()
    {
        var child1Id = ClayApi.Id("child1");
        var child2Id = ClayApi.Id("child2");

        RunLayout(() =>
        {
            using (ClayApi.Element(new ElementDeclaration
            {
                Id = ClayApi.Id("row"),
                Layout = new LayoutConfig
                {
                    Sizing = Sizing.Fill(),
                    Direction = LayoutDirection.LeftToRight
                }
            }))
            {
                using (ClayApi.Element(new ElementDeclaration
                {
                    Id = child1Id,
                    Layout = new LayoutConfig { Sizing = Sizing.FixedSize(100, 50) },
                    BackgroundColor = Color.Red
                })) { }

                using (ClayApi.Element(new ElementDeclaration
                {
                    Id = child2Id,
                    Layout = new LayoutConfig { Sizing = Sizing.FixedSize(100, 50) },
                    BackgroundColor = Color.Green
                })) { }
            }
        });

        var d1 = ClayApi.GetElementData(child1Id);
        var d2 = ClayApi.GetElementData(child2Id);

        Assert.True(d1.Found);
        Assert.True(d2.Found);
        // Second child should be to the right of the first
        Assert.True(d2.BoundingBox.X >= d1.BoundingBox.X + d1.BoundingBox.Width);
        Assert.Equal(d1.BoundingBox.Y, d2.BoundingBox.Y);
    }

    [Fact]
    public void Column_ChildrenStacked()
    {
        var child1Id = ClayApi.Id("c1");
        var child2Id = ClayApi.Id("c2");

        RunLayout(() =>
        {
            using (ClayApi.Element(new ElementDeclaration
            {
                Id = ClayApi.Id("col"),
                Layout = new LayoutConfig
                {
                    Sizing = Sizing.Fill(),
                    Direction = LayoutDirection.TopToBottom
                }
            }))
            {
                using (ClayApi.Element(new ElementDeclaration
                {
                    Id = child1Id,
                    Layout = new LayoutConfig { Sizing = Sizing.FixedSize(100, 50) },
                    BackgroundColor = Color.Red
                })) { }

                using (ClayApi.Element(new ElementDeclaration
                {
                    Id = child2Id,
                    Layout = new LayoutConfig { Sizing = Sizing.FixedSize(100, 50) },
                    BackgroundColor = Color.Green
                })) { }
            }
        });

        var d1 = ClayApi.GetElementData(child1Id);
        var d2 = ClayApi.GetElementData(child2Id);

        // Second child should be below the first
        Assert.True(d2.BoundingBox.Y >= d1.BoundingBox.Y + d1.BoundingBox.Height);
        Assert.Equal(d1.BoundingBox.X, d2.BoundingBox.X);
    }

    [Fact]
    public void ChildGap_AddsSpaceBetweenChildren_Row()
    {
        var child1Id = ClayApi.Id("g1");
        var child2Id = ClayApi.Id("g2");

        RunLayout(() =>
        {
            using (ClayApi.Element(new ElementDeclaration
            {
                Id = ClayApi.Id("gap-row"),
                Layout = new LayoutConfig
                {
                    Sizing = Sizing.Fill(),
                    Direction = LayoutDirection.LeftToRight,
                    ChildGap = 20
                }
            }))
            {
                using (ClayApi.Element(new ElementDeclaration
                {
                    Id = child1Id,
                    Layout = new LayoutConfig { Sizing = Sizing.FixedSize(100, 50) },
                    BackgroundColor = Color.Red
                })) { }

                using (ClayApi.Element(new ElementDeclaration
                {
                    Id = child2Id,
                    Layout = new LayoutConfig { Sizing = Sizing.FixedSize(100, 50) },
                    BackgroundColor = Color.Green
                })) { }
            }
        });

        var d1 = ClayApi.GetElementData(child1Id);
        var d2 = ClayApi.GetElementData(child2Id);

        // Gap of 20 between children in a row
        var gap = d2.BoundingBox.X - (d1.BoundingBox.X + d1.BoundingBox.Width);
        Assert.Equal(20f, gap);
    }

    [Fact]
    public void Padding_OffsetsChildren()
    {
        var childId = ClayApi.Id("padded-child");

        RunLayout(() =>
        {
            using (ClayApi.Element(new ElementDeclaration
            {
                Id = ClayApi.Id("padded"),
                Layout = new LayoutConfig
                {
                    Sizing = Sizing.Fill(),
                    Padding = Padding.All(16)
                }
            }))
            {
                using (ClayApi.Element(new ElementDeclaration
                {
                    Id = childId,
                    Layout = new LayoutConfig { Sizing = Sizing.FixedSize(100, 50) },
                    BackgroundColor = Color.Red
                })) { }
            }
        });

        var data = ClayApi.GetElementData(childId);
        Assert.True(data.Found);
        // Child should be offset by padding
        Assert.True(data.BoundingBox.X >= 16f);
        Assert.True(data.BoundingBox.Y >= 16f);
    }

    [Fact]
    public void Grow_DistributesRemainingSpace()
    {
        var fixedId = ClayApi.Id("fixed");
        var growId = ClayApi.Id("grow");

        RunLayout(() =>
        {
            using (ClayApi.Element(new ElementDeclaration
            {
                Id = ClayApi.Id("grow-row"),
                Layout = new LayoutConfig
                {
                    Sizing = Sizing.Fill(),
                    Direction = LayoutDirection.LeftToRight
                }
            }))
            {
                using (ClayApi.Element(new ElementDeclaration
                {
                    Id = fixedId,
                    Layout = new LayoutConfig
                    {
                        Sizing = new Sizing(SizingAxis.Fixed(200), SizingAxis.Fixed(50))
                    },
                    BackgroundColor = Color.Red
                })) { }

                using (ClayApi.Element(new ElementDeclaration
                {
                    Id = growId,
                    Layout = new LayoutConfig
                    {
                        Sizing = new Sizing(SizingAxis.Grow(), SizingAxis.Fixed(50))
                    },
                    BackgroundColor = Color.Green
                })) { }
            }
        });

        var fixedData = ClayApi.GetElementData(fixedId);
        var growData = ClayApi.GetElementData(growId);

        Assert.Equal(200f, fixedData.BoundingBox.Width);
        // Grow element should take the remaining space
        Assert.True(growData.BoundingBox.Width > 0);
        Assert.True(growData.BoundingBox.Width > fixedData.BoundingBox.Width);
    }

    [Fact]
    public void TwoGrowElements_SplitSpaceEqually()
    {
        var g1 = ClayApi.Id("gr1");
        var g2 = ClayApi.Id("gr2");

        RunLayout(() =>
        {
            using (ClayApi.Element(new ElementDeclaration
            {
                Id = ClayApi.Id("split-row"),
                Layout = new LayoutConfig
                {
                    Sizing = Sizing.Fill(),
                    Direction = LayoutDirection.LeftToRight
                }
            }))
            {
                using (ClayApi.Element(new ElementDeclaration
                {
                    Id = g1,
                    Layout = new LayoutConfig
                    {
                        Sizing = new Sizing(SizingAxis.Grow(), SizingAxis.Fixed(50))
                    },
                    BackgroundColor = Color.Red
                })) { }

                using (ClayApi.Element(new ElementDeclaration
                {
                    Id = g2,
                    Layout = new LayoutConfig
                    {
                        Sizing = new Sizing(SizingAxis.Grow(), SizingAxis.Fixed(50))
                    },
                    BackgroundColor = Color.Green
                })) { }
            }
        });

        var d1 = ClayApi.GetElementData(g1);
        var d2 = ClayApi.GetElementData(g2);

        // Both grow elements should get equal space
        Assert.True(d1.BoundingBox.Width > 0);
        Assert.Equal(d1.BoundingBox.Width, d2.BoundingBox.Width);
    }

    [Fact]
    public void Percent_SizesRelativeToParent()
    {
        var childId = ClayApi.Id("pct-child");

        RunLayout(() =>
        {
            using (ClayApi.Element(new ElementDeclaration
            {
                Id = ClayApi.Id("pct-parent"),
                Layout = new LayoutConfig { Sizing = Sizing.Fill() }
            }))
            {
                using (ClayApi.Element(new ElementDeclaration
                {
                    Id = childId,
                    Layout = new LayoutConfig
                    {
                        Sizing = new Sizing(SizingAxis.PercentOf(0.5f), SizingAxis.PercentOf(0.25f))
                    },
                    BackgroundColor = Color.Red
                })) { }
            }
        });

        var data = ClayApi.GetElementData(childId);
        Assert.True(data.Found);
        // Percent sizing should be relative to parent
        Assert.True(data.BoundingBox.Width > 0);
        Assert.True(data.BoundingBox.Height > 0);
    }

    [Fact]
    public void Fit_ShrinkWrapsContent()
    {
        var parentId = ClayApi.Id("fit-parent");

        RunLayout(() =>
        {
            using (ClayApi.Element(new ElementDeclaration
            {
                Id = parentId,
                Layout = new LayoutConfig
                {
                    Sizing = new Sizing(SizingAxis.Fit(), SizingAxis.Fit()),
                    Direction = LayoutDirection.LeftToRight
                },
                BackgroundColor = Color.Gray
            }))
            {
                using (ClayApi.Element(new ElementDeclaration
                {
                    Id = ClayApi.Id("fit-c1"),
                    Layout = new LayoutConfig { Sizing = Sizing.FixedSize(80, 40) },
                    BackgroundColor = Color.Red
                })) { }

                using (ClayApi.Element(new ElementDeclaration
                {
                    Id = ClayApi.Id("fit-c2"),
                    Layout = new LayoutConfig { Sizing = Sizing.FixedSize(60, 30) },
                    BackgroundColor = Color.Green
                })) { }
            }
        });

        var data = ClayApi.GetElementData(parentId);
        Assert.Equal(140f, data.BoundingBox.Width);  // 80 + 60
        Assert.Equal(40f, data.BoundingBox.Height);   // max(40, 30)
    }

    [Fact]
    public void NestedLayouts_ThreeLevels()
    {
        var innerId = ClayApi.Id("inner");

        RunLayout(() =>
        {
            using (ClayApi.Element(new ElementDeclaration
            {
                Id = ClayApi.Id("outer"),
                Layout = new LayoutConfig
                {
                    Sizing = Sizing.Fill(),
                    Padding = Padding.All(10)
                }
            }))
            {
                using (ClayApi.Element(new ElementDeclaration
                {
                    Id = ClayApi.Id("middle"),
                    Layout = new LayoutConfig
                    {
                        Sizing = Sizing.Fill(),
                        Padding = Padding.All(20)
                    }
                }))
                {
                    using (ClayApi.Element(new ElementDeclaration
                    {
                        Id = innerId,
                        Layout = new LayoutConfig { Sizing = Sizing.Fill() },
                        BackgroundColor = Color.Red
                    })) { }
                }
            }
        });

        var data = ClayApi.GetElementData(innerId);
        Assert.True(data.Found);
        // Inner element offset should include both levels of padding
        Assert.True(data.BoundingBox.X >= 30f);
        Assert.True(data.BoundingBox.Y >= 30f);
    }

    [Fact]
    public void Alignment_CenterCenter()
    {
        var childId = ClayApi.Id("centered");

        RunLayout(() =>
        {
            using (ClayApi.Element(new ElementDeclaration
            {
                Id = ClayApi.Id("center-parent"),
                Layout = new LayoutConfig
                {
                    Sizing = Sizing.Fill(),
                    ChildAlignment = ChildAlignment.Center
                }
            }))
            {
                using (ClayApi.Element(new ElementDeclaration
                {
                    Id = childId,
                    Layout = new LayoutConfig { Sizing = Sizing.FixedSize(100, 100) },
                    BackgroundColor = Color.Red
                })) { }
            }
        });

        var data = ClayApi.GetElementData(childId);
        Assert.True(data.Found);
        // Child should be centered — not at (0,0)
        Assert.True(data.BoundingBox.X > 0);
        Assert.True(data.BoundingBox.Y > 0);
    }

    [Fact]
    public void Alignment_BottomRight()
    {
        var childId = ClayApi.Id("br-child");

        RunLayout(() =>
        {
            using (ClayApi.Element(new ElementDeclaration
            {
                Id = ClayApi.Id("br-parent"),
                Layout = new LayoutConfig
                {
                    Sizing = Sizing.Fill(),
                    ChildAlignment = ChildAlignment.BottomRight
                }
            }))
            {
                using (ClayApi.Element(new ElementDeclaration
                {
                    Id = childId,
                    Layout = new LayoutConfig { Sizing = Sizing.FixedSize(100, 100) },
                    BackgroundColor = Color.Red
                })) { }
            }
        });

        var data = ClayApi.GetElementData(childId);
        Assert.True(data.Found);
        // Child should be at bottom-right — X and Y should both be > 0
        Assert.True(data.BoundingBox.X > 0);
        Assert.True(data.BoundingBox.Y > 0);
    }

    [Fact]
    public void EmptyElement_HasZeroFitSize()
    {
        var emptyId = ClayApi.Id("empty");

        RunLayout(() =>
        {
            using (ClayApi.Element(new ElementDeclaration
            {
                Id = emptyId,
                Layout = new LayoutConfig
                {
                    Sizing = new Sizing(SizingAxis.Fit(), SizingAxis.Fit())
                },
                BackgroundColor = Color.Red
            })) { }
        });

        var data = ClayApi.GetElementData(emptyId);
        Assert.True(data.Found);
        Assert.Equal(0f, data.BoundingBox.Width);
        Assert.Equal(0f, data.BoundingBox.Height);
    }

    [Fact]
    public void PaddingWithFit_IncludesPaddingInSize()
    {
        var parentId = ClayApi.Id("pad-fit");

        RunLayout(() =>
        {
            using (ClayApi.Element(new ElementDeclaration
            {
                Id = parentId,
                Layout = new LayoutConfig
                {
                    Sizing = new Sizing(SizingAxis.Fit(), SizingAxis.Fit()),
                    Padding = Padding.All(20)
                },
                BackgroundColor = Color.Gray
            }))
            {
                using (ClayApi.Element(new ElementDeclaration
                {
                    Id = ClayApi.Id("pad-fit-child"),
                    Layout = new LayoutConfig { Sizing = Sizing.FixedSize(60, 40) },
                    BackgroundColor = Color.Red
                })) { }
            }
        });

        var data = ClayApi.GetElementData(parentId);
        Assert.Equal(100f, data.BoundingBox.Width);  // 60 + 20*2
        Assert.Equal(80f, data.BoundingBox.Height);   // 40 + 20*2
    }

    [Fact]
    public void ChildGap_InColumn()
    {
        var c1 = ClayApi.Id("cg-c1");
        var c2 = ClayApi.Id("cg-c2");
        var c3 = ClayApi.Id("cg-c3");

        RunLayout(() =>
        {
            using (ClayApi.Element(new ElementDeclaration
            {
                Id = ClayApi.Id("cg-col"),
                Layout = new LayoutConfig
                {
                    Sizing = Sizing.Fill(),
                    Direction = LayoutDirection.TopToBottom,
                    ChildGap = 10
                }
            }))
            {
                using (ClayApi.Element(new ElementDeclaration
                {
                    Id = c1,
                    Layout = new LayoutConfig { Sizing = Sizing.FixedSize(100, 30) },
                    BackgroundColor = Color.Red
                })) { }
                using (ClayApi.Element(new ElementDeclaration
                {
                    Id = c2,
                    Layout = new LayoutConfig { Sizing = Sizing.FixedSize(100, 30) },
                    BackgroundColor = Color.Green
                })) { }
                using (ClayApi.Element(new ElementDeclaration
                {
                    Id = c3,
                    Layout = new LayoutConfig { Sizing = Sizing.FixedSize(100, 30) },
                    BackgroundColor = Color.Blue
                })) { }
            }
        });

        var d1 = ClayApi.GetElementData(c1);
        var d2 = ClayApi.GetElementData(c2);
        var d3 = ClayApi.GetElementData(c3);

        // Gaps of 10 between children in column
        var gap1 = d2.BoundingBox.Y - (d1.BoundingBox.Y + d1.BoundingBox.Height);
        var gap2 = d3.BoundingBox.Y - (d2.BoundingBox.Y + d2.BoundingBox.Height);
        Assert.Equal(10f, gap1);
        Assert.Equal(10f, gap2);
    }
}
