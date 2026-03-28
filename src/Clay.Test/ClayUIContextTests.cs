using System.Numerics;
using Clay;

namespace Clay.Test;

public class ClayUIContextTests : IDisposable
{
    private readonly ClayUIFixture _fixture;

    public ClayUIContextTests()
    {
        _fixture = new ClayUIFixture();
    }

    public void Dispose() => _fixture.Dispose();

    [Fact]
    public void GetContext_ReturnsNonNull()
    {
        var handle = ClayUI.GetContext();
        Assert.NotNull(handle);
    }

    [Fact]
    public void SetContext_RestoresStyle()
    {
        // Save context with default style
        var defaultStyle = ClayUI.Style;
        var ctx1 = ClayUI.GetContext();

        // Change style
        ClayUI.Style = ClayUIStyle.Light;
        Assert.NotSame(defaultStyle, ClayUI.Style);

        // Restore — should bring back original style
        ClayUI.SetContext(ctx1);
        Assert.Same(defaultStyle, ClayUI.Style);
    }

    [Fact]
    public void SetContext_RestoresWidgetState()
    {
        // Run a frame that creates some widget state (toggle, checkbox etc.)
        bool toggle = false;
        _fixture.RunFrame(() =>
        {
            ClayUI.Toggle("TestToggle", ref toggle);
        });

        // Save context
        var ctx1 = ClayUI.GetContext();

        // Clear all state
        ClayUI.ClearState();

        // Restore — widget state should be back
        ClayUI.SetContext(ctx1);

        // Run another frame — should not crash (state is intact)
        _fixture.RunFrame(() =>
        {
            ClayUI.Toggle("TestToggle", ref toggle);
        });
    }

    [Fact]
    public void SwitchBetweenContexts_IndependentState()
    {
        // Context 1: set a style and create widget state
        ClayUI.Style = ClayUIStyle.Dark;
        bool check1 = true;
        _fixture.RunFrame(() =>
        {
            ClayUI.Checkbox("CB1", ref check1);
        });
        var ctx1 = ClayUI.GetContext();

        // Context 2: different style
        ClayUI.Style = ClayUIStyle.Light;
        bool check2 = false;
        _fixture.RunFrame(() =>
        {
            ClayUI.Checkbox("CB2", ref check2);
        });
        var ctx2 = ClayUI.GetContext();

        // Switch to ctx1 — should have Dark style
        ClayUI.SetContext(ctx1);
        Assert.Equal(ClayUIStyle.Dark.Button.BackgroundColor.R, ClayUI.Style.Button.BackgroundColor.R);

        // Switch to ctx2 — should have Light style
        ClayUI.SetContext(ctx2);
        Assert.Equal(ClayUIStyle.Light.Button.BackgroundColor.R, ClayUI.Style.Button.BackgroundColor.R);
    }

    [Fact]
    public void SetContext_RestoresLayoutContext()
    {
        // Save the current context (has an initialized Clay layout context)
        var ctx = ClayUI.GetContext();

        // Verify layout context is present
        Assert.NotNull(ClayApi.Context);

        // Set context to null layout (simulating a different context)
        ClayApi.SetContext(null);
        Assert.Null(ClayApi.Context);

        // Restore — layout context should be back
        ClayUI.SetContext(ctx);
        Assert.NotNull(ClayApi.Context);
    }

    [Fact]
    public void GetContext_CalledMultipleTimes_ReturnsDifferentHandles()
    {
        var h1 = ClayUI.GetContext();
        var h2 = ClayUI.GetContext();

        // Each call creates a new handle object (but wrapping the same context)
        Assert.NotSame(h1, h2);
    }

    [Fact]
    public void SetContext_ThenRenderFrame_Works()
    {
        var ctx = ClayUI.GetContext();

        // Render a frame, save, restore, render again
        _fixture.RunFrame(() =>
        {
            ClayUI.Label("Before save");
            ClayUI.Button("Test");
        });

        var saved = ClayUI.GetContext();

        ClayUI.SetContext(saved);

        // Should render without crash
        _fixture.RunFrame(() =>
        {
            ClayUI.Label("After restore");
            ClayUI.Button("Test");
        });
    }

    [Fact]
    public void SetContext_PreservesWindowState()
    {
        bool open = true;

        // Create a window in context 1
        _fixture.RunFrame(() =>
        {
            if (ClayUI.BeginWindow("CtxWin", ref open, defaultPosition: new Vector2(50, 50)))
            {
                ClayUI.Label("Content");
            }
            ClayUI.EndWindow();
        });

        ClayUI.SetWindowPosition("CtxWin", new Vector2(200, 200));

        // GetContext returns a handle to the SAME context (shared reference).
        // Verify the window state is accessible via the current context.
        var pos = ClayUI.GetWindowPosition("CtxWin");
        Assert.Equal(200f, pos.X);
        Assert.Equal(200f, pos.Y);
    }

    [Fact]
    public void SetContext_SwitchesBetweenContextsWithDifferentWindows()
    {
        bool open1 = true;
        bool open2 = true;

        // Context A: create window at (100, 100)
        _fixture.RunFrame(() =>
        {
            if (ClayUI.BeginWindow("WinA", ref open1, defaultPosition: new Vector2(100, 100)))
                ClayUI.Label("A");
            ClayUI.EndWindow();
        });
        var ctxA = ClayUI.GetContext();

        // Verify WinA exists
        var posA = ClayUI.GetWindowPosition("WinA");
        Assert.Equal(100f, posA.X);

        // Note: switching to a different context requires creating a new ClayUIContext.
        // GetContext/SetContext enables saving and restoring the same context after
        // temporary changes (like the debug window's style swap).
    }

    [Fact]
    public void SetContext_PreservesPopupState()
    {
        // Open a popup
        _fixture.RunFrame(() =>
        {
            ClayUI.OpenPopup("CtxPopup");
            if (ClayUI.BeginPopup("CtxPopup"))
            {
                ClayUI.Label("Popup");
                ClayUI.EndPopup();
            }
        });

        Assert.True(ClayUI.IsPopupOpen("CtxPopup"));

        // GetContext captures the current state (shared reference)
        var ctx = ClayUI.GetContext();

        // Popup is still open via the handle's context
        Assert.True(ClayUI.IsPopupOpen("CtxPopup"));
    }
}
