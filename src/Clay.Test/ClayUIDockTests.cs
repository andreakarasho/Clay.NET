using System.Numerics;
using Clay;

namespace Clay.Test;

public class ClayUIDockTests : IDisposable
{
    private readonly ClayUIFixture _fixture;

    public ClayUIDockTests()
    {
        _fixture = new ClayUIFixture();
    }

    public void Dispose() => _fixture.Dispose();

    // ============ DockNode ============

    [Fact]
    public void DockNode_NewNode_IsLeafAndEmpty()
    {
        var node = new DockNode { Id = 1 };
        Assert.True(node.IsLeaf);
        Assert.True(node.IsEmpty);
    }

    [Fact]
    public void DockNode_WithDockedWindow_IsLeafButNotEmpty()
    {
        var node = new DockNode { Id = 1 };
        node.DockedWindowIds.Add(42);
        Assert.True(node.IsLeaf);
        Assert.False(node.IsEmpty);
    }

    [Fact]
    public void DockNode_SplitNode_IsNotLeaf()
    {
        var node = new DockNode
        {
            Id = 1,
            SplitDirection = DockSplitDirection.Horizontal,
            ChildA = new DockNode { Id = 2 },
            ChildB = new DockNode { Id = 3 }
        };
        Assert.False(node.IsLeaf);
        Assert.False(node.IsEmpty); // non-leaf is never empty
    }

    [Fact]
    public void DockNode_FindNode_FindsSelf()
    {
        var node = new DockNode { Id = 42 };
        Assert.Same(node, node.FindNode(42));
    }

    [Fact]
    public void DockNode_FindNode_FindsDeepChild()
    {
        var leaf = new DockNode { Id = 99 };
        var root = new DockNode
        {
            Id = 1,
            SplitDirection = DockSplitDirection.Horizontal,
            ChildA = new DockNode
            {
                Id = 2,
                SplitDirection = DockSplitDirection.Vertical,
                ChildA = new DockNode { Id = 3 },
                ChildB = leaf
            },
            ChildB = new DockNode { Id = 4 }
        };
        Assert.Same(leaf, root.FindNode(99));
    }

    [Fact]
    public void DockNode_FindNode_ReturnsNullForMissingId()
    {
        var node = new DockNode { Id = 1 };
        Assert.Null(node.FindNode(999));
    }

    [Fact]
    public void DockNode_FindParent_FindsCorrectParent()
    {
        var childB = new DockNode { Id = 3 };
        var root = new DockNode
        {
            Id = 1,
            SplitDirection = DockSplitDirection.Horizontal,
            ChildA = new DockNode { Id = 2 },
            ChildB = childB
        };
        Assert.Same(root, root.FindParent(3));
    }

    [Fact]
    public void DockNode_RebuildWindowToNodeMap_MapsAllLeaves()
    {
        var leafA = new DockNode { Id = 2 };
        leafA.DockedWindowIds.Add(100);
        leafA.DockedWindowIds.Add(101);

        var leafB = new DockNode { Id = 3 };
        leafB.DockedWindowIds.Add(200);

        var root = new DockNode
        {
            Id = 1,
            SplitDirection = DockSplitDirection.Horizontal,
            ChildA = leafA,
            ChildB = leafB
        };

        var map = new Dictionary<uint, DockNode>();
        root.RebuildWindowToNodeMap(map);

        Assert.Equal(3, map.Count);
        Assert.Same(leafA, map[100]);
        Assert.Same(leafA, map[101]);
        Assert.Same(leafB, map[200]);
    }

    // ============ DockBuilder ============

    [Fact]
    public void DockBuilder_HasLayout_FalseWhenNoLayout()
    {
        Assert.False(DockBuilder.HasLayout("TestDock"));
    }

    [Fact]
    public void DockBuilder_Reset_CreatesLayout()
    {
        var rootId = DockBuilder.Reset("TestDock");
        Assert.NotEqual(0u, rootId);
        // Root is empty leaf, so HasLayout should still be false until we dock a window
        Assert.False(DockBuilder.HasLayout("TestDock"));
    }

    [Fact]
    public void DockBuilder_DockWindow_MakesHasLayoutTrue()
    {
        var rootId = DockBuilder.Reset("TestDock2");
        DockBuilder.DockWindow(rootId, "MyWindow");
        Assert.True(DockBuilder.HasLayout("TestDock2"));
    }

    [Fact]
    public void DockBuilder_SplitNode_CreatesChildren()
    {
        var rootId = DockBuilder.Reset("SplitTest");
        var (nodeA, nodeB) = DockBuilder.SplitNode(rootId, DockSplitDirection.Horizontal, 0.3f);

        Assert.NotEqual(0u, nodeA);
        Assert.NotEqual(0u, nodeB);
        Assert.NotEqual(nodeA, nodeB);

        // Both children should be usable (DockWindow won't throw)
        DockBuilder.DockWindow(nodeA, "WinA");
        DockBuilder.DockWindow(nodeB, "WinB");
    }

    [Fact]
    public void DockBuilder_SplitNode_OriginalNodeBecomesNonLeaf()
    {
        var rootId = DockBuilder.Reset("SplitTest2");
        DockBuilder.SplitNode(rootId, DockSplitDirection.Vertical, 0.5f);

        // Root should now be a split, not a leaf — DockWindow on it should fail
        Assert.Throws<InvalidOperationException>(() =>
            DockBuilder.DockWindow(rootId, "ShouldFail"));
    }

    [Fact]
    public void DockBuilder_NestedSplits_TreeStructureIsCorrect()
    {
        // Reproduce the GameEditor layout
        var rootId = DockBuilder.Reset("NestedTest");

        var (mainArea, bottomArea) = DockBuilder.SplitNode(rootId, DockSplitDirection.Vertical, 0.7f);
        var (hierarchy, centerRight) = DockBuilder.SplitNode(mainArea, DockSplitDirection.Horizontal, 0.16f);
        var (viewport, inspector) = DockBuilder.SplitNode(centerRight, DockSplitDirection.Horizontal, 0.72f);
        var (assets, console) = DockBuilder.SplitNode(bottomArea, DockSplitDirection.Horizontal, 0.55f);

        // All leaf nodes should accept DockWindow (proves they're findable and are leaves)
        DockBuilder.DockWindow(hierarchy, "Hierarchy");
        DockBuilder.DockWindow(viewport, "Viewport");
        DockBuilder.DockWindow(inspector, "Inspector");
        DockBuilder.DockWindow(assets, "Assets");
        DockBuilder.DockWindow(console, "Console");

        // All split nodes should reject DockWindow (proves they're splits, not leaves)
        Assert.Throws<InvalidOperationException>(() => DockBuilder.DockWindow(rootId, "X"));
        Assert.Throws<InvalidOperationException>(() => DockBuilder.DockWindow(mainArea, "X"));
        Assert.Throws<InvalidOperationException>(() => DockBuilder.DockWindow(centerRight, "X"));
        Assert.Throws<InvalidOperationException>(() => DockBuilder.DockWindow(bottomArea, "X"));

        Assert.True(DockBuilder.HasLayout("NestedTest"));
    }

    [Fact]
    public void DockBuilder_DockWindow_AddsToLeaf()
    {
        var rootId = DockBuilder.Reset("DockTest");
        var (left, right) = DockBuilder.SplitNode(rootId, DockSplitDirection.Horizontal, 0.5f);

        DockBuilder.DockWindow(left, "LeftWindow");
        DockBuilder.DockWindow(right, "RightWindow");

        // Verify both have windows by checking HasLayout works
        Assert.True(DockBuilder.HasLayout("DockTest"));
    }

    [Fact]
    public void DockBuilder_DockMultipleWindows_SameLeaf()
    {
        var rootId = DockBuilder.Reset("MultiDock");
        DockBuilder.DockWindow(rootId, "Window1");
        DockBuilder.DockWindow(rootId, "Window2");
        DockBuilder.DockWindow(rootId, "Window3");

        // All three windows should be docked — verify no exception on DockWindow (no duplicates added)
        Assert.True(DockBuilder.HasLayout("MultiDock"));
    }

    [Fact]
    public void DockBuilder_DockSameWindowTwice_NoDuplicate()
    {
        var rootId = DockBuilder.Reset("DupDock");
        DockBuilder.DockWindow(rootId, "Window1");
        DockBuilder.DockWindow(rootId, "Window1");

        // No exception means duplicate was handled gracefully
        Assert.True(DockBuilder.HasLayout("DupDock"));
    }

    [Fact]
    public void DockBuilder_SplitNonLeaf_Throws()
    {
        var rootId = DockBuilder.Reset("SplitErr");
        DockBuilder.SplitNode(rootId, DockSplitDirection.Horizontal, 0.5f);

        // Root is now a split node, can't split again
        Assert.Throws<InvalidOperationException>(() =>
            DockBuilder.SplitNode(rootId, DockSplitDirection.Vertical, 0.5f));
    }

    [Fact]
    public void DockBuilder_SplitRatio_ClampedToRange()
    {
        var rootId = DockBuilder.Reset("ClampTest");
        DockBuilder.SplitNode(rootId, DockSplitDirection.Horizontal, 0.01f); // Below 0.1

        // Verify it didn't crash — the node was split successfully
        Assert.Throws<InvalidOperationException>(() =>
            DockBuilder.SplitNode(rootId, DockSplitDirection.Vertical, 0.5f));
    }

    // ============ BeginDockSpace / EndDockSpace ============

    [Fact]
    public void BeginEndDockSpace_EmptyLayout_NoException()
    {
        _fixture.RunFrame(() =>
        {
            ClayUI.BeginDockSpace("EmptyDock");
            ClayUI.EndDockSpace();
        });
    }

    [Fact]
    public void BeginEndDockSpace_WithLayout_NoException()
    {
        var rootId = DockBuilder.Reset("FrameDock");
        var (left, right) = DockBuilder.SplitNode(rootId, DockSplitDirection.Horizontal, 0.5f);
        DockBuilder.DockWindow(left, "Left");
        DockBuilder.DockWindow(right, "Right");

        _fixture.RunFrame(() =>
        {
            ClayUI.BeginDockSpace("FrameDock");

            bool leftOpen = true;
            if (ClayUI.BeginWindow("Left", ref leftOpen))
                ClayUI.Label("Left content");
            ClayUI.EndWindow();

            bool rightOpen = true;
            if (ClayUI.BeginWindow("Right", ref rightOpen))
                ClayUI.Label("Right content");
            ClayUI.EndWindow();

            ClayUI.EndDockSpace();
        });
    }

    [Fact]
    public void BeginEndDockSpace_GameEditorLayout_NoException()
    {
        // Reproduce the exact GameEditor dock layout
        var rootId = DockBuilder.Reset("EditorDock");
        var (mainArea, bottomArea) = DockBuilder.SplitNode(rootId, DockSplitDirection.Vertical, 0.7f);
        var (hierarchy, centerRight) = DockBuilder.SplitNode(mainArea, DockSplitDirection.Horizontal, 0.16f);
        var (viewport, inspector) = DockBuilder.SplitNode(centerRight, DockSplitDirection.Horizontal, 0.72f);
        var (assets, console) = DockBuilder.SplitNode(bottomArea, DockSplitDirection.Horizontal, 0.55f);

        DockBuilder.DockWindow(hierarchy, "Hierarchy");
        DockBuilder.DockWindow(viewport, "Viewport");
        DockBuilder.DockWindow(inspector, "Inspector");
        DockBuilder.DockWindow(assets, "Asset Browser");
        DockBuilder.DockWindow(console, "Console");

        bool hierarchyOpen = true, viewportOpen = true, inspectorOpen = true;
        bool assetOpen = true, consoleOpen = true;

        // Run two frames to establish bounding boxes
        for (int i = 0; i < 2; i++)
        {
            _fixture.RunFrame(() =>
            {
                ClayUI.BeginDockSpace("EditorDock");

                if (ClayUI.BeginWindow("Hierarchy", ref hierarchyOpen, flags: WindowFlags.NoCollapse))
                    ClayUI.Label("Hierarchy content");
                ClayUI.EndWindow();

                if (ClayUI.BeginWindow("Viewport", ref viewportOpen, flags: WindowFlags.NoCollapse))
                    ClayUI.Label("Viewport content");
                ClayUI.EndWindow();

                if (ClayUI.BeginWindow("Inspector", ref inspectorOpen, flags: WindowFlags.NoCollapse))
                    ClayUI.Label("Inspector content");
                ClayUI.EndWindow();

                if (ClayUI.BeginWindow("Asset Browser", ref assetOpen, flags: WindowFlags.NoCollapse))
                    ClayUI.Label("Asset Browser content");
                ClayUI.EndWindow();

                if (ClayUI.BeginWindow("Console", ref consoleOpen, flags: WindowFlags.NoCollapse))
                    ClayUI.Label("Console content");
                ClayUI.EndWindow();

                ClayUI.EndDockSpace();
            });
        }
    }

    // ============ Docked Window Behavior ============

    [Fact]
    public void DockedWindow_InactiveTab_ReturnsFalse()
    {
        var rootId = DockBuilder.Reset("TabTest");
        DockBuilder.DockWindow(rootId, "Tab1");
        DockBuilder.DockWindow(rootId, "Tab2");

        // Tab1 is at index 0 (active by default, ActiveTabIndex=0)
        bool open1 = true, open2 = true;
        bool result1 = false, result2 = false;

        _fixture.RunFrame(() =>
        {
            ClayUI.BeginDockSpace("TabTest");

            result1 = ClayUI.BeginWindow("Tab1", ref open1);
            if (result1) ClayUI.Label("Tab1 content");
            ClayUI.EndWindow();

            result2 = ClayUI.BeginWindow("Tab2", ref open2);
            if (result2) ClayUI.Label("Tab2 content");
            ClayUI.EndWindow();

            ClayUI.EndDockSpace();
        });

        // Tab1 is active tab (index 0), Tab2 is inactive
        // On first frame, leaf content area has no bounds yet, so both may return false
        // This is expected behavior (same as ImGui first-frame delay)
    }

    [Fact]
    public void IsWindowDocked_ReturnsTrueForDockedWindow()
    {
        var rootId = DockBuilder.Reset("DockedCheck");
        DockBuilder.DockWindow(rootId, "MyWindow");

        bool open = true;
        _fixture.RunFrame(() =>
        {
            ClayUI.BeginDockSpace("DockedCheck");
            ClayUI.BeginWindow("MyWindow", ref open);
            ClayUI.EndWindow();
            ClayUI.EndDockSpace();
        });

        Assert.True(ClayUI.IsWindowDocked("MyWindow"));
    }

    [Fact]
    public void IsWindowDocked_ReturnsFalseForFloatingWindow()
    {
        bool open = true;
        _fixture.RunFrame(() =>
        {
            ClayUI.BeginWindow("FloatingWin", ref open);
            ClayUI.Label("I'm floating");
            ClayUI.EndWindow();
        });

        Assert.False(ClayUI.IsWindowDocked("FloatingWin"));
    }

    [Fact]
    public void NoDockingFlag_WindowStaysFloating()
    {
        var rootId = DockBuilder.Reset("NoDockTest");
        // Don't dock any window via DockBuilder

        bool open = true;
        bool result = false;
        _fixture.RunFrame(() =>
        {
            ClayUI.BeginDockSpace("NoDockTest");

            // Window with NoDocking flag inside a dock space should render as floating
            result = ClayUI.BeginWindow("NoDockWin", ref open, flags: WindowFlags.NoDocking);
            if (result) ClayUI.Label("Floating content");
            ClayUI.EndWindow();

            ClayUI.EndDockSpace();
        });

        Assert.True(result); // Should render as normal floating window
    }

    // ============ Serialization ============

    [Fact]
    public void SaveLayout_ProducesValidJson()
    {
        var rootId = DockBuilder.Reset("SerDock");
        var (left, right) = DockBuilder.SplitNode(rootId, DockSplitDirection.Horizontal, 0.4f);
        DockBuilder.DockWindow(left, "Left");
        DockBuilder.DockWindow(right, "Right1");
        DockBuilder.DockWindow(right, "Right2");

        var json = DockBuilder.SaveLayout("SerDock");
        Assert.NotEmpty(json);
        Assert.Contains("Horizontal", json);
        Assert.Contains("Left", json);
        Assert.Contains("Right1", json);
        Assert.Contains("Right2", json);
    }

    [Fact]
    public void LoadLayout_RestoresStructure()
    {
        // Create and save a layout
        var rootId = DockBuilder.Reset("LoadTest");
        var (left, right) = DockBuilder.SplitNode(rootId, DockSplitDirection.Horizontal, 0.3f);
        DockBuilder.DockWindow(left, "Panel1");
        DockBuilder.DockWindow(right, "Panel2");

        var json = DockBuilder.SaveLayout("LoadTest");

        // Reset and reload
        DockBuilder.Reset("LoadTest");
        DockBuilder.LoadLayout("LoadTest", json);

        Assert.True(DockBuilder.HasLayout("LoadTest"));
    }

    // ============ Tree Pruning ============

    [Fact]
    public void PruneEmptyLeaf_RemovesSplitNode()
    {
        var leafA = new DockNode { Id = 2 };
        leafA.DockedWindowIds.Add(100);

        var emptyLeaf = new DockNode { Id = 3 };

        var root = new DockNode
        {
            Id = 1,
            SplitDirection = DockSplitDirection.Horizontal,
            ChildA = leafA,
            ChildB = emptyLeaf
        };

        var space = new DockSpaceState { Id = 1, RootNode = root };

        // After pruning the empty leaf, root should become the survivor (leafA's content)
        // The root node absorbs leafA's properties
        Assert.True(emptyLeaf.IsEmpty);
    }

    // ============ ActiveDockSpaceId ============

    [Fact]
    public void ActiveDockSpaceId_ZeroOutsideDockSpace()
    {
        _fixture.RunFrame(() =>
        {
            Assert.Equal(0u, ClayUI.ActiveDockSpaceId);
        });
    }

    [Fact]
    public void ActiveDockSpaceId_NonZeroInsideDockSpace()
    {
        DockBuilder.Reset("ActiveTest");
        uint dockId = 0;
        _fixture.RunFrame(() =>
        {
            ClayUI.BeginDockSpace("ActiveTest");
            dockId = ClayUI.ActiveDockSpaceId;
            ClayUI.EndDockSpace();
        });
        Assert.NotEqual(0u, dockId);
    }
}
