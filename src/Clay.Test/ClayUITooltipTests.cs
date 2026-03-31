using System.Numerics;
using Clay;

namespace Clay.Test;

public class ClayUITooltipTests : IDisposable
{
    private readonly ClayUIFixture _fixture;

    public ClayUITooltipTests()
    {
        _fixture = new ClayUIFixture();
    }

    public void Dispose() => _fixture.Dispose();

    [Fact]
    public void Tooltip_DoesNotRender_WhenNotHovered()
    {
        var commands = _fixture.RunFrame(() =>
        {
            ClayUI.Button("Btn");
            ClayUI.Tooltip("Tip text");
        });

        bool hasTooltipText = false;
        foreach (var cmd in commands)
        {
            if (cmd.CommandType == RenderCommandType.Text && cmd.Text.Text == "Tip text")
            {
                hasTooltipText = true;
                break;
            }
        }
        Assert.False(hasTooltipText, "Tooltip should not render when not hovered");
    }

    [Fact]
    public void Tooltip_DoesNotRender_BeforeDelay()
    {
        // Frame 1: establish bounding boxes
        _fixture.RunFrame(() =>
        {
            ClayUI.Button("DelayBtn");
            ClayUI.Tooltip("Delayed tip");
        }, mousePos: new Vector2(20, 10));

        // Frame 2: hover but only 1 frame (~16ms < 500ms delay)
        var commands = _fixture.RunFrame(() =>
        {
            ClayUI.Button("DelayBtn");
            ClayUI.Tooltip("Delayed tip");
        }, mousePos: new Vector2(20, 10));

        bool hasTooltipText = false;
        foreach (var cmd in commands)
        {
            if (cmd.CommandType == RenderCommandType.Text && cmd.Text.Text == "Delayed tip")
            {
                hasTooltipText = true;
                break;
            }
        }
        Assert.False(hasTooltipText, "Tooltip should not render before delay");
    }

    [Fact]
    public void Tooltip_Renders_AfterDelay()
    {
        // Frame 1: establish bounding boxes with hover
        _fixture.RunFrame(() =>
        {
            ClayUI.Button("ShowBtn");
            ClayUI.Tooltip("Show tip");
        }, mousePos: new Vector2(20, 10));

        // Run enough frames with large deltaTime to exceed 0.5s delay
        ReadOnlySpan<RenderCommand> commands = default;
        for (int i = 0; i < 5; i++)
        {
            commands = _fixture.RunFrame(() =>
            {
                ClayUI.Button("ShowBtn");
                ClayUI.Tooltip("Show tip");
            }, mousePos: new Vector2(20, 10), deltaTime: 0.2f);
        }

        bool hasTooltipText = false;
        foreach (var cmd in commands)
        {
            if (cmd.CommandType == RenderCommandType.Text && cmd.Text.Text == "Show tip")
            {
                hasTooltipText = true;
                break;
            }
        }
        Assert.True(hasTooltipText, "Tooltip should render after hover delay");
    }

    [Fact]
    public void Tooltip_Disappears_WhenUnhovered()
    {
        // Build up hover time
        _fixture.RunFrame(() =>
        {
            ClayUI.Button("UnhoverBtn");
            ClayUI.Tooltip("Unhover tip");
        }, mousePos: new Vector2(20, 10));

        for (int i = 0; i < 5; i++)
        {
            _fixture.RunFrame(() =>
            {
                ClayUI.Button("UnhoverBtn");
                ClayUI.Tooltip("Unhover tip");
            }, mousePos: new Vector2(20, 10), deltaTime: 0.2f);
        }

        // Move mouse away
        var commands = _fixture.RunFrame(() =>
        {
            ClayUI.Button("UnhoverBtn");
            ClayUI.Tooltip("Unhover tip");
        }, mousePos: new Vector2(700, 700));

        bool hasTooltipText = false;
        foreach (var cmd in commands)
        {
            if (cmd.CommandType == RenderCommandType.Text && cmd.Text.Text == "Unhover tip")
            {
                hasTooltipText = true;
                break;
            }
        }
        Assert.False(hasTooltipText, "Tooltip should disappear when mouse moves away");
    }

    [Fact]
    public void BeginTooltip_ReturnsFalse_WhenNotHovered()
    {
        bool opened = true;
        _fixture.RunFrame(() =>
        {
            ClayUI.Button("RichBtn");
            opened = ClayUI.BeginTooltip();
            if (opened) ClayUI.EndTooltip();
        });

        Assert.False(opened);
    }

    [Fact]
    public void BeginTooltip_ReturnsTrue_AfterDelay()
    {
        // Establish bounding boxes
        _fixture.RunFrame(() =>
        {
            ClayUI.Button("RichShow");
            var _ = ClayUI.BeginTooltip();
            if (_) ClayUI.EndTooltip();
        }, mousePos: new Vector2(20, 10));

        // Hover long enough
        bool opened = false;
        for (int i = 0; i < 5; i++)
        {
            _fixture.RunFrame(() =>
            {
                ClayUI.Button("RichShow");
                opened = ClayUI.BeginTooltip();
                if (opened)
                {
                    ClayUI.Label("Rich content");
                    ClayUI.EndTooltip();
                }
            }, mousePos: new Vector2(20, 10), deltaTime: 0.2f);
        }

        Assert.True(opened, "BeginTooltip should return true after hover delay");
    }

    [Fact]
    public void Tooltip_OnlyOnePerFrame()
    {
        // Establish bounding boxes
        _fixture.RunFrame(() =>
        {
            ClayUI.Button("Btn1");
            ClayUI.Tooltip("Tip 1");
            ClayUI.Button("Btn2");
            ClayUI.Tooltip("Tip 2");
        }, mousePos: new Vector2(20, 10));

        // Hover long enough (both buttons may be near the same position)
        ReadOnlySpan<RenderCommand> commands = default;
        for (int i = 0; i < 5; i++)
        {
            commands = _fixture.RunFrame(() =>
            {
                ClayUI.Button("Btn1");
                ClayUI.Tooltip("Tip 1");
                ClayUI.Button("Btn2");
                ClayUI.Tooltip("Tip 2");
            }, mousePos: new Vector2(20, 10), deltaTime: 0.2f);
        }

        int tooltipCount = 0;
        foreach (var cmd in commands)
        {
            if (cmd.CommandType == RenderCommandType.Text &&
                (cmd.Text.Text == "Tip 1" || cmd.Text.Text == "Tip 2"))
            {
                tooltipCount++;
            }
        }
        Assert.True(tooltipCount <= 1, $"Only one tooltip should render per frame, got {tooltipCount}");
    }

    [Fact]
    public void Tooltip_NoWidget_DoesNotCrash()
    {
        // Tooltip with no preceding widget should not crash
        _fixture.RunFrame(() =>
        {
            ClayUI.Tooltip("Orphan tooltip");
        });
    }
}
