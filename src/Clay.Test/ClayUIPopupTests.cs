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

    // ============ Popup Input Blocking ============

    [Fact]
    public void Popup_ClickOutside_ClosesPopup()
    {
        // Frame 1: open popup and establish layout
        _fixture.RunFrame(() =>
        {
            ClayUI.OpenPopupAt("closePop", new Vector2(100, 100));
            if (ClayUI.BeginPopup("closePop"))
            {
                ClayUI.MenuItem("Item");
                ClayUI.EndPopup();
            }
        });
        Assert.True(ClayUI.IsPopupOpen("closePop"));

        // Frame 2: no click, keeps bounding boxes valid
        _fixture.RunFrame(() =>
        {
            if (ClayUI.BeginPopup("closePop"))
            {
                ClayUI.MenuItem("Item");
                ClayUI.EndPopup();
            }
        });
        Assert.True(ClayUI.IsPopupOpen("closePop"));

        // Frame 3: click far outside popup — should close it
        _fixture.RunFrame(() =>
        {
            if (ClayUI.BeginPopup("closePop"))
            {
                ClayUI.MenuItem("Item");
                ClayUI.EndPopup();
            }
        }, mousePos: new Vector2(700, 700), mouseDown: true);

        Assert.False(ClayUI.IsPopupOpen("closePop"));
    }

    [Fact]
    public void Popup_ClickInside_DoesNotClose()
    {
        // Frame 1: open popup at known position
        _fixture.RunFrame(() =>
        {
            ClayUI.OpenPopupAt("stayPop", new Vector2(50, 50));
            if (ClayUI.BeginPopup("stayPop"))
            {
                ClayUI.Label("Content");
                ClayUI.EndPopup();
            }
        });
        Assert.True(ClayUI.IsPopupOpen("stayPop"));

        // Frame 2: establish layout data
        _fixture.RunFrame(() =>
        {
            if (ClayUI.BeginPopup("stayPop"))
            {
                ClayUI.Label("Content");
                ClayUI.EndPopup();
            }
        });

        // Frame 3: click inside the popup area — should stay open
        _fixture.RunFrame(() =>
        {
            if (ClayUI.BeginPopup("stayPop"))
            {
                ClayUI.Label("Content");
                ClayUI.EndPopup();
            }
        }, mousePos: new Vector2(80, 70), mouseDown: true);

        Assert.True(ClayUI.IsPopupOpen("stayPop"));
    }

    [Fact]
    public void Popup_ClickOutside_BlocksButtonBehind()
    {
        bool buttonClicked = false;

        // Frame 1: open popup, render button behind it
        _fixture.RunFrame(() =>
        {
            ClayUI.Button("Behind");
            ClayUI.OpenPopupAt("blockPop", new Vector2(10, 10));
            if (ClayUI.BeginPopup("blockPop"))
            {
                ClayUI.Label("Popup");
                ClayUI.EndPopup();
            }
        });

        // Frame 2: establish layout
        _fixture.RunFrame(() =>
        {
            ClayUI.Button("Behind");
            if (ClayUI.BeginPopup("blockPop"))
            {
                ClayUI.Label("Popup");
                ClayUI.EndPopup();
            }
        });

        // Frame 3: click outside popup to close it — the button behind should NOT fire
        _fixture.RunFrame(() =>
        {
            buttonClicked = ClayUI.Button("Behind");
            if (ClayUI.BeginPopup("blockPop"))
            {
                ClayUI.Label("Popup");
                ClayUI.EndPopup();
            }
        }, mousePos: new Vector2(5, 5), mouseDown: true);

        Assert.False(ClayUI.IsPopupOpen("blockPop"), "Popup should have closed");
        Assert.False(buttonClicked, "Button behind popup should not receive click on close frame");
    }

    [Fact]
    public void Popup_Open_BlocksButtonBehind()
    {
        bool buttonClicked = false;

        // Frame 1: open popup that overlaps a button area
        _fixture.RunFrame(() =>
        {
            buttonClicked = ClayUI.Button("Behind");
            ClayUI.OpenPopupAt("overlapPop", new Vector2(0, 0));
            if (ClayUI.BeginPopup("overlapPop", new PopupStyle { MinWidth = 200 }))
            {
                ClayUI.Label("Popup content");
                ClayUI.EndPopup();
            }
        });

        // Frame 2: establish layout
        _fixture.RunFrame(() =>
        {
            buttonClicked = ClayUI.Button("Behind");
            if (ClayUI.BeginPopup("overlapPop", new PopupStyle { MinWidth = 200 }))
            {
                ClayUI.Label("Popup content");
                ClayUI.EndPopup();
            }
        });

        // Frame 3: click in the popup area — button behind should be blocked
        _fixture.RunFrame(() =>
        {
            buttonClicked = ClayUI.Button("Behind");
            if (ClayUI.BeginPopup("overlapPop", new PopupStyle { MinWidth = 200 }))
            {
                ClayUI.Label("Popup content");
                ClayUI.EndPopup();
            }
        }, mousePos: new Vector2(20, 20), mouseDown: true);

        Assert.True(ClayUI.IsPopupOpen("overlapPop"), "Popup should stay open (click was inside)");
        Assert.False(buttonClicked, "Button behind open popup should not receive click");
    }

    // ============ Popup Blocks Window Interactions ============

    [Fact]
    public void Popup_Open_BlocksWindowResize()
    {
        bool open = true;
        var winPos = new Vector2(100, 100);
        var winSize = new Vector2(300, 200);

        // Frame 1: create window and open popup over the window's right edge
        _fixture.RunFrame(() =>
        {
            ClayUI.BeginWindow("ResizeWin", ref open, defaultPosition: winPos, defaultSize: winSize);
            ClayUI.Label("Content");
            ClayUI.EndWindow();

            // Popup overlaps the window's right border (at x=400)
            ClayUI.OpenPopupAt("resizePop", new Vector2(390, 150));
            if (ClayUI.BeginPopup("resizePop"))
            {
                ClayUI.Label("Popup");
                ClayUI.EndPopup();
            }
        });

        // Frame 2: establish layout
        _fixture.RunFrame(() =>
        {
            ClayUI.BeginWindow("ResizeWin", ref open);
            ClayUI.Label("Content");
            ClayUI.EndWindow();
            if (ClayUI.BeginPopup("resizePop"))
            {
                ClayUI.Label("Popup");
                ClayUI.EndPopup();
            }
        });

        var sizeBefore = ClayUI.GetWindowSize("ResizeWin");

        // Frame 3: click on the window border where the popup overlaps — resize should be blocked
        _fixture.RunFrame(() =>
        {
            ClayUI.BeginWindow("ResizeWin", ref open);
            ClayUI.Label("Content");
            ClayUI.EndWindow();
            if (ClayUI.BeginPopup("resizePop"))
            {
                ClayUI.Label("Popup");
                ClayUI.EndPopup();
            }
        }, mousePos: new Vector2(399, 160), mouseDown: true);

        var sizeAfter = ClayUI.GetWindowSize("ResizeWin");
        Assert.Equal(sizeBefore.X, sizeAfter.X);
        Assert.Equal(sizeBefore.Y, sizeAfter.Y);
    }

    [Fact]
    public void Popup_Open_BlocksWindowDrag()
    {
        bool open = true;
        var winPos = new Vector2(100, 100);
        var winSize = new Vector2(300, 200);

        // Frame 1: create window and open popup over its title bar
        _fixture.RunFrame(() =>
        {
            ClayUI.BeginWindow("DragWin", ref open, defaultPosition: winPos, defaultSize: winSize);
            ClayUI.Label("Content");
            ClayUI.EndWindow();

            // Popup overlaps the title bar area (y=100..132)
            ClayUI.OpenPopupAt("dragPop", new Vector2(150, 105));
            if (ClayUI.BeginPopup("dragPop"))
            {
                ClayUI.Label("Popup");
                ClayUI.EndPopup();
            }
        });

        // Frame 2: establish layout
        _fixture.RunFrame(() =>
        {
            ClayUI.BeginWindow("DragWin", ref open);
            ClayUI.Label("Content");
            ClayUI.EndWindow();
            if (ClayUI.BeginPopup("dragPop"))
            {
                ClayUI.Label("Popup");
                ClayUI.EndPopup();
            }
        });

        var posBefore = ClayUI.GetWindowPosition("DragWin");

        // Frame 3: click on title bar where popup overlaps — drag should be blocked
        _fixture.RunFrame(() =>
        {
            ClayUI.BeginWindow("DragWin", ref open);
            ClayUI.Label("Content");
            ClayUI.EndWindow();
            if (ClayUI.BeginPopup("dragPop"))
            {
                ClayUI.Label("Popup");
                ClayUI.EndPopup();
            }
        }, mousePos: new Vector2(180, 115), mouseDown: true);

        var posAfter = ClayUI.GetWindowPosition("DragWin");
        Assert.Equal(posBefore.X, posAfter.X);
        Assert.Equal(posBefore.Y, posAfter.Y);
    }

    [Fact]
    public void Popup_Open_BlocksWindowClose()
    {
        bool open = true;
        var winPos = new Vector2(100, 100);
        var winSize = new Vector2(300, 200);

        // Frame 1: create window and open popup over its close button area
        _fixture.RunFrame(() =>
        {
            ClayUI.BeginWindow("CloseWin", ref open, defaultPosition: winPos, defaultSize: winSize);
            ClayUI.Label("Content");
            ClayUI.EndWindow();

            // Popup over the top-right area where close button is
            ClayUI.OpenPopupAt("closeBtnPop", new Vector2(370, 100));
            if (ClayUI.BeginPopup("closeBtnPop"))
            {
                ClayUI.Label("Popup");
                ClayUI.EndPopup();
            }
        });

        // Frame 2: establish layout
        _fixture.RunFrame(() =>
        {
            ClayUI.BeginWindow("CloseWin", ref open);
            ClayUI.Label("Content");
            ClayUI.EndWindow();
            if (ClayUI.BeginPopup("closeBtnPop"))
            {
                ClayUI.Label("Popup");
                ClayUI.EndPopup();
            }
        });

        // Frame 3: click where close button and popup overlap — window should NOT close
        _fixture.RunFrame(() =>
        {
            ClayUI.BeginWindow("CloseWin", ref open);
            ClayUI.Label("Content");
            ClayUI.EndWindow();
            if (ClayUI.BeginPopup("closeBtnPop"))
            {
                ClayUI.Label("Popup");
                ClayUI.EndPopup();
            }
        }, mousePos: new Vector2(390, 110), mouseDown: true);

        Assert.True(open, "Window should not close when popup is over the close button");
    }
}
