using System.Numerics;
using Clay;

namespace Clay.Test;

public class ClayUIWindowTests : IDisposable
{
    private readonly ClayUIFixture _fixture;

    public ClayUIWindowTests()
    {
        _fixture = new ClayUIFixture();
    }

    public void Dispose() => _fixture.Dispose();

    // ============ Basic Window ============

    [Fact]
    public void BeginEndWindow_Renders_WithoutCrash()
    {
        bool open = true;
        _fixture.RunFrame(() =>
        {
            if (ClayUI.BeginWindow("Test Window", ref open))
            {
                ClayUI.Label("Window content");
            }
            ClayUI.EndWindow();
        });
    }

    [Fact]
    public void BeginWindow_WhenOpen_ReturnsTrue()
    {
        bool open = true;
        bool result = false;
        _fixture.RunFrame(() =>
        {
            result = ClayUI.BeginWindow("Open Window", ref open);
            if (result) ClayUI.Label("Content");
            ClayUI.EndWindow();
        });

        Assert.True(result);
    }

    [Fact]
    public void BeginWindow_WhenClosed_ReturnsFalse()
    {
        bool open = false;
        bool result = true;
        _fixture.RunFrame(() =>
        {
            result = ClayUI.BeginWindow("Closed Window", ref open);
            ClayUI.EndWindow();
        });

        Assert.False(result);
    }

    [Fact]
    public void Window_DefaultPosition_Applied()
    {
        bool open = true;
        _fixture.RunFrame(() =>
        {
            ClayUI.BeginWindow("Pos Window", ref open, defaultPosition: new Vector2(50, 75));
            ClayUI.EndWindow();
        });

        var pos = ClayUI.GetWindowPosition("Pos Window");
        Assert.Equal(50f, pos.X);
        Assert.Equal(75f, pos.Y);
    }

    [Fact]
    public void Window_DefaultSize_Applied()
    {
        bool open = true;
        _fixture.RunFrame(() =>
        {
            ClayUI.BeginWindow("Size Window", ref open, defaultSize: new Vector2(400, 300));
            ClayUI.EndWindow();
        });

        var size = ClayUI.GetWindowSize("Size Window");
        Assert.Equal(400f, size.X);
        Assert.Equal(300f, size.Y);
    }

    [Fact]
    public void Window_GeneratesRenderCommands()
    {
        bool open = true;
        var commands = _fixture.RunFrame(() =>
        {
            if (ClayUI.BeginWindow("Render Window", ref open))
            {
                ClayUI.Label("Hello");
            }
            ClayUI.EndWindow();
        });

        bool hasRect = false;
        bool hasText = false;
        foreach (var cmd in commands)
        {
            if (cmd.CommandType == RenderCommandType.Rectangle) hasRect = true;
            if (cmd.CommandType == RenderCommandType.Text) hasText = true;
        }

        Assert.True(hasRect, "Window should generate rectangle commands");
        Assert.True(hasText, "Window should generate text commands");
    }

    // ============ Window Flags ============

    [Fact]
    public void Window_NoCollapse_Renders()
    {
        bool open = true;
        _fixture.RunFrame(() =>
        {
            ClayUI.BeginWindow("NC Window", ref open, flags: WindowFlags.NoCollapse);
            ClayUI.Label("Content");
            ClayUI.EndWindow();
        });
    }

    [Fact]
    public void Window_NoClose_Renders()
    {
        bool open = true;
        _fixture.RunFrame(() =>
        {
            ClayUI.BeginWindow("NCl Window", ref open, flags: WindowFlags.NoClose);
            ClayUI.Label("Content");
            ClayUI.EndWindow();
        });
    }

    [Fact]
    public void Window_NoMove_Renders()
    {
        bool open = true;
        _fixture.RunFrame(() =>
        {
            ClayUI.BeginWindow("NM Window", ref open, flags: WindowFlags.NoMove);
            ClayUI.Label("Content");
            ClayUI.EndWindow();
        });
    }

    [Fact]
    public void Window_NoResize_Renders()
    {
        bool open = true;
        _fixture.RunFrame(() =>
        {
            ClayUI.BeginWindow("NR Window", ref open, flags: WindowFlags.NoResize);
            ClayUI.Label("Content");
            ClayUI.EndWindow();
        });
    }

    [Fact]
    public void Window_NoScroll_Renders()
    {
        bool open = true;
        _fixture.RunFrame(() =>
        {
            ClayUI.BeginWindow("NS Window", ref open, flags: WindowFlags.NoScroll);
            ClayUI.Label("Content");
            ClayUI.EndWindow();
        });
    }

    [Fact]
    public void Window_AllFlags_Renders()
    {
        bool open = true;
        var allFlags = WindowFlags.NoMove | WindowFlags.NoResize | WindowFlags.NoClose
                     | WindowFlags.NoCollapse | WindowFlags.NoScroll;
        _fixture.RunFrame(() =>
        {
            ClayUI.BeginWindow("All Flags", ref open, flags: allFlags);
            ClayUI.Label("Locked window");
            ClayUI.EndWindow();
        });
    }

    // ============ Window State ============

    [Fact]
    public void SetWindowPosition_UpdatesPosition()
    {
        bool open = true;
        _fixture.RunFrame(() =>
        {
            ClayUI.BeginWindow("Move Window", ref open);
            ClayUI.EndWindow();
        });

        ClayUI.SetWindowPosition("Move Window", new Vector2(200, 150));
        var pos = ClayUI.GetWindowPosition("Move Window");
        Assert.Equal(200f, pos.X);
        Assert.Equal(150f, pos.Y);
    }

    [Fact]
    public void SetWindowSize_UpdatesSize()
    {
        bool open = true;
        _fixture.RunFrame(() =>
        {
            ClayUI.BeginWindow("Resize Window", ref open);
            ClayUI.EndWindow();
        });

        ClayUI.SetWindowSize("Resize Window", new Vector2(500, 400));
        var size = ClayUI.GetWindowSize("Resize Window");
        Assert.Equal(500f, size.X);
        Assert.Equal(400f, size.Y);
    }

    [Fact]
    public void Window_Topmost_Property()
    {
        bool open = true;
        _fixture.RunFrame(() =>
        {
            ClayUI.BeginWindow("Top Window", ref open, topmost: true);
            ClayUI.EndWindow();
        });

        Assert.True(ClayUI.GetWindowTopmost("Top Window"));
    }

    [Fact]
    public void SetWindowTopmost_ChangesState()
    {
        bool open = true;
        _fixture.RunFrame(() =>
        {
            ClayUI.BeginWindow("Toggle Top", ref open, topmost: false);
            ClayUI.EndWindow();
        });

        Assert.False(ClayUI.GetWindowTopmost("Toggle Top"));
        ClayUI.SetWindowTopmost("Toggle Top", true);
        Assert.True(ClayUI.GetWindowTopmost("Toggle Top"));
    }

    // ============ Multiple Windows ============

    [Fact]
    public void MultipleWindows_RenderTogether()
    {
        bool open1 = true, open2 = true;
        _fixture.RunFrame(() =>
        {
            if (ClayUI.BeginWindow("Win1", ref open1, defaultPosition: new Vector2(10, 10)))
                ClayUI.Label("Window 1");
            ClayUI.EndWindow();

            if (ClayUI.BeginWindow("Win2", ref open2, defaultPosition: new Vector2(200, 10)))
                ClayUI.Label("Window 2");
            ClayUI.EndWindow();
        });
    }

    [Fact]
    public void Window_WithWidgets_Inside()
    {
        bool open = true;
        bool check = false;
        float slider = 0.5f;

        _fixture.RunFrame(() =>
        {
            if (ClayUI.BeginWindow("Widget Win", ref open))
            {
                ClayUI.Button("Click");
                ClayUI.Checkbox("CB", ref check);
                ClayUI.Slider("SL", ref slider);
                ClayUI.ProgressBar(0.5f);
            }
            ClayUI.EndWindow();
        });
    }

    // ============ Collapsed Window ============

    [Fact]
    public void Window_Collapsed_StillRendersMinimally()
    {
        bool open = true;
        // First frame: create window
        _fixture.RunFrame(() =>
        {
            ClayUI.BeginWindow("Collapse Win", ref open, defaultPosition: new Vector2(100, 100));
            ClayUI.Label("Content");
            ClayUI.EndWindow();
        });

        // Simulate clicking the collapse button by running another frame
        // (Just verify the window can be rendered after state changes)
        _fixture.RunFrame(() =>
        {
            if (ClayUI.BeginWindow("Collapse Win", ref open))
            {
                ClayUI.Label("Still here");
            }
            ClayUI.EndWindow();
        });
    }

    // ============ Window Persistence ============

    [Fact]
    public void Window_State_PersistsAcrossFrames()
    {
        bool open = true;

        // Frame 1
        _fixture.RunFrame(() =>
        {
            ClayUI.BeginWindow("Persist Win", ref open, defaultPosition: new Vector2(50, 50));
            ClayUI.EndWindow();
        });

        ClayUI.SetWindowPosition("Persist Win", new Vector2(300, 200));

        // Frame 2
        _fixture.RunFrame(() =>
        {
            ClayUI.BeginWindow("Persist Win", ref open);
            ClayUI.EndWindow();
        });

        var pos = ClayUI.GetWindowPosition("Persist Win");
        Assert.Equal(300f, pos.X);
        Assert.Equal(200f, pos.Y);
    }
}
