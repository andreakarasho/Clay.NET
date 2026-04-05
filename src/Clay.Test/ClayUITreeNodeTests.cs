using System.Numerics;
using Clay;

namespace Clay.Test;

public class ClayUITreeNodeTests : IDisposable
{
    private readonly ClayUIFixture _fixture;

    public ClayUITreeNodeTests()
    {
        _fixture = new ClayUIFixture();
    }

    public void Dispose() => _fixture.Dispose();

    [Fact]
    public void BeginTreeNode_CollapsedByDefault_ReturnsFalse()
    {
        bool expanded = true;
        _fixture.RunFrame(() =>
        {
            expanded = ClayUI.BeginTreeNode("Node");
            if (expanded) ClayUI.EndTreeNode();
        });

        Assert.False(expanded, "Tree node should be collapsed by default");
    }

    [Fact]
    public void BeginTreeNode_DefaultExpanded_ReturnsTrue()
    {
        bool expanded = false;
        _fixture.RunFrame(() =>
        {
            expanded = ClayUI.BeginTreeNode("Open Node", new TreeNodeStyle { DefaultExpanded = true });
            if (expanded)
            {
                ClayUI.Label("Child content");
                ClayUI.EndTreeNode();
            }
        });

        Assert.True(expanded, "Tree node with DefaultExpanded should be expanded");
    }

    [Fact]
    public void BeginTreeNode_Collapsed_GeneratesHeaderOnly()
    {
        var commands = _fixture.RunFrame(() =>
        {
            var opened = ClayUI.BeginTreeNode("Collapsed");
            if (opened) ClayUI.EndTreeNode();
        });

        // Should have text for the arrow and label
        int textCount = 0;
        foreach (var cmd in commands)
        {
            if (cmd.CommandType == RenderCommandType.Text) textCount++;
        }

        Assert.True(textCount >= 2, $"Collapsed tree node should render arrow + label text, got {textCount}");
    }

    [Fact]
    public void BeginTreeNode_Expanded_GeneratesChildContent()
    {
        var commands = _fixture.RunFrame(() =>
        {
            var opened = ClayUI.BeginTreeNode("Expanded", new TreeNodeStyle { DefaultExpanded = true });
            if (opened)
            {
                ClayUI.Label("Child");
                ClayUI.EndTreeNode();
            }
        });

        int textCount = 0;
        foreach (var cmd in commands)
        {
            if (cmd.CommandType == RenderCommandType.Text) textCount++;
        }

        // Arrow + label + child = at least 3
        Assert.True(textCount >= 3, $"Expanded tree node should render arrow + label + child, got {textCount}");
    }

    [Fact]
    public void TreeNode_Click_TogglesExpanded()
    {
        bool expandedFrame3 = false;

        Action buildUi = () =>
        {
            var expanded = ClayUI.BeginTreeNode("Toggle Node");
            if (expanded)
            {
                ClayUI.Label("Now visible");
                ClayUI.EndTreeNode();
            }
        };

        // Frame 1: establish bounding boxes (collapsed, no interaction)
        _fixture.RunFrame(buildUi);

        // Frame 2: press down on the node header
        _fixture.RunFrame(buildUi, mousePos: new Vector2(30, 5), mouseDown: true);

        // Frame 3: release — click fires on mouse release
        _fixture.RunFrame(() =>
        {
            expandedFrame3 = ClayUI.BeginTreeNode("Toggle Node");
            if (expandedFrame3)
            {
                ClayUI.Label("Now visible");
                ClayUI.EndTreeNode();
            }
        },
        mousePos: new Vector2(30, 5),
        mouseDown: false);

        Assert.True(expandedFrame3, "Clicking tree node header should expand it");
    }

    [Fact]
    public void TreeNode_Nested_Renders()
    {
        _fixture.RunFrame(() =>
        {
            var outer = ClayUI.BeginTreeNode("Outer", new TreeNodeStyle { DefaultExpanded = true });
            if (outer)
            {
                var inner = ClayUI.BeginTreeNode("Inner", new TreeNodeStyle { DefaultExpanded = true });
                if (inner)
                {
                    ClayUI.Label("Leaf");
                    ClayUI.EndTreeNode();
                }
                ClayUI.EndTreeNode();
            }
        });
    }

    [Fact]
    public void TreeNode_StatePersistedAcrossFrames()
    {
        // Frame 1: expand via DefaultExpanded
        _fixture.RunFrame(() =>
        {
            var expanded = ClayUI.BeginTreeNode("Persist Node", new TreeNodeStyle { DefaultExpanded = true });
            if (expanded)
            {
                ClayUI.Label("Content");
                ClayUI.EndTreeNode();
            }
        });

        // Frame 2: same node without DefaultExpanded — should still be expanded from state
        bool stillExpanded = false;
        _fixture.RunFrame(() =>
        {
            stillExpanded = ClayUI.BeginTreeNode("Persist Node");
            if (stillExpanded)
            {
                ClayUI.Label("Still here");
                ClayUI.EndTreeNode();
            }
        });

        Assert.True(stillExpanded, "Tree node expanded state should persist across frames");
    }
}
