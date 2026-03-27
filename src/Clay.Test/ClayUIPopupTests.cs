using System.Numerics;
using Clay;

namespace Clay.Test;

public class ClayUIPopupTests : IDisposable
{
    private readonly ClayUIFixture _fixture;

    public ClayUIPopupTests()
    {
        _fixture = new ClayUIFixture();
    }

    public void Dispose() => _fixture.Dispose();

    // ============ Popup Lifecycle ============

    [Fact]
    public void IsPopupOpen_BeforeOpen_ReturnsFalse()
    {
        _fixture.RunFrame(() =>
        {
            Assert.False(ClayUI.IsPopupOpen("test"));
        });
    }

    [Fact]
    public void OpenPopup_SetsPopupOpen()
    {
        _fixture.RunFrame(() =>
        {
            ClayUI.OpenPopup("myPopup");
            // BeginPopup processes the open request
            if (ClayUI.BeginPopup("myPopup"))
            {
                ClayUI.EndPopup();
            }
        });

        Assert.True(ClayUI.IsPopupOpen("myPopup"));
    }

    [Fact]
    public void OpenPopupAt_SetsPopupOpen()
    {
        _fixture.RunFrame(() =>
        {
            ClayUI.OpenPopupAt("posPopup", new Vector2(100, 200));
            if (ClayUI.BeginPopup("posPopup"))
            {
                ClayUI.EndPopup();
            }
        });

        Assert.True(ClayUI.IsPopupOpen("posPopup"));
    }

    [Fact]
    public void ClosePopup_ClosesPopup()
    {
        // Open popup
        _fixture.RunFrame(() =>
        {
            ClayUI.OpenPopup("closeable");
            if (ClayUI.BeginPopup("closeable"))
            {
                ClayUI.EndPopup();
            }
        });

        Assert.True(ClayUI.IsPopupOpen("closeable"));

        ClayUI.ClosePopup("closeable");
        Assert.False(ClayUI.IsPopupOpen("closeable"));
    }

    [Fact]
    public void CloseAllPopups_ClosesAll()
    {
        // Open multiple popups
        _fixture.RunFrame(() =>
        {
            ClayUI.OpenPopupAt("pop1", new Vector2(10, 10));
            if (ClayUI.BeginPopup("pop1"))
            {
                ClayUI.EndPopup();
            }
        });

        _fixture.RunFrame(() =>
        {
            ClayUI.OpenPopupAt("pop2", new Vector2(100, 100));
            if (ClayUI.BeginPopup("pop1"))
                ClayUI.EndPopup();
            if (ClayUI.BeginPopup("pop2"))
                ClayUI.EndPopup();
        });

        ClayUI.CloseAllPopups();
        Assert.False(ClayUI.IsPopupOpen("pop1"));
        Assert.False(ClayUI.IsPopupOpen("pop2"));
    }

    // ============ BeginPopup ============

    [Fact]
    public void BeginPopup_WhenClosed_ReturnsFalse()
    {
        bool opened = true;
        _fixture.RunFrame(() =>
        {
            opened = ClayUI.BeginPopup("notOpen");
            if (opened) ClayUI.EndPopup();
        });

        Assert.False(opened);
    }

    [Fact]
    public void BeginPopup_WhenOpen_ReturnsTrue()
    {
        bool opened = false;
        _fixture.RunFrame(() =>
        {
            ClayUI.OpenPopup("isOpen");
            opened = ClayUI.BeginPopup("isOpen");
            if (opened) ClayUI.EndPopup();
        });

        Assert.True(opened);
    }

    [Fact]
    public void BeginPopup_GeneratesRenderCommands()
    {
        var commands = _fixture.RunFrame(() =>
        {
            ClayUI.OpenPopupAt("renderPop", new Vector2(50, 50));
            if (ClayUI.BeginPopup("renderPop"))
            {
                ClayUI.Label("Popup content");
                ClayUI.EndPopup();
            }
        });

        bool hasRect = false;
        foreach (var cmd in commands)
        {
            if (cmd.CommandType == RenderCommandType.Rectangle)
            {
                hasRect = true;
                break;
            }
        }

        Assert.True(hasRect, "Open popup should generate render commands");
    }

    // ============ MenuItem ============

    [Fact]
    public void MenuItem_InsidePopup_Renders()
    {
        _fixture.RunFrame(() =>
        {
            ClayUI.OpenPopupAt("menuPop", new Vector2(50, 50));
            if (ClayUI.BeginPopup("menuPop"))
            {
                ClayUI.MenuItem("Action 1");
                ClayUI.MenuItem("Action 2");
                ClayUI.MenuItem("Disabled", enabled: false);
                ClayUI.EndPopup();
            }
        });
    }

    [Fact]
    public void MenuItem_NotClicked_ReturnsFalse()
    {
        bool clicked = true;
        _fixture.RunTwoFrames(() =>
        {
            ClayUI.OpenPopupAt("menuPop2", new Vector2(50, 50));
            if (ClayUI.BeginPopup("menuPop2"))
            {
                clicked = ClayUI.MenuItem("Do thing");
                ClayUI.EndPopup();
            }
        }, mousePos: new Vector2(700, 700)); // far from popup

        Assert.False(clicked);
    }

    // ============ MenuSeparator ============

    [Fact]
    public void MenuSeparator_InsidePopup_Renders()
    {
        _fixture.RunFrame(() =>
        {
            ClayUI.OpenPopupAt("sepPop", new Vector2(50, 50));
            if (ClayUI.BeginPopup("sepPop"))
            {
                ClayUI.MenuItem("Above");
                ClayUI.Separator();
                ClayUI.MenuItem("Below");
                ClayUI.EndPopup();
            }
        });
    }

    // ============ Context Menu ============

    [Fact]
    public void BeginContextMenu_WhenNotTriggered_ReturnsFalse()
    {
        var triggerId = ClayApi.Id("trigger");

        bool opened = true;
        _fixture.RunFrame(() =>
        {
            using (ClayApi.Element(new ElementDeclaration
            {
                Id = triggerId,
                Layout = new LayoutConfig { Sizing = Sizing.FixedSize(100, 100) },
                BackgroundColor = Color.Gray
            })) { }

            opened = ClayUI.BeginContextMenu("ctx", triggerId);
            ClayUI.EndContextMenu();
        });

        Assert.False(opened);
    }

    // ============ OpenPopupBelow ============

    [Fact]
    public void OpenPopupBelow_OpensPopup()
    {
        var anchor = new BoundingBox(100, 50, 200, 30);

        _fixture.RunFrame(() =>
        {
            ClayUI.OpenPopupBelow("belowPop", anchor);
            if (ClayUI.BeginPopup("belowPop"))
            {
                ClayUI.MenuItem("Item");
                ClayUI.EndPopup();
            }
        });

        Assert.True(ClayUI.IsPopupOpen("belowPop"));
    }
}
