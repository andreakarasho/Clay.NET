using System.Numerics;
using Clay;

namespace Clay.Test;

public class ClayUISliderTests : IDisposable
{
    private readonly ClayUIFixture _fixture;

    public ClayUISliderTests()
    {
        _fixture = new ClayUIFixture();
    }

    public void Dispose() => _fixture.Dispose();

    [Fact]
    public void Slider_Renders_WithoutCrash()
    {
        float val = 0.5f;
        _fixture.RunFrame(() => ClayUI.Slider("Slider", ref val));
    }

    [Fact]
    public void Slider_NotClicked_DoesNotChange()
    {
        float val = 0.5f;
        bool changed = false;
        _fixture.RunTwoFrames(
            () => changed = ClayUI.Slider("S1", ref val),
            mousePos: new Vector2(700, 500));

        Assert.False(changed);
        Assert.Equal(0.5f, val);
    }

    [Fact]
    public void Slider_DefaultValue_Preserved()
    {
        float val = 0.3f;
        _fixture.RunFrame(() => ClayUI.Slider("S2", ref val));

        Assert.Equal(0.3f, val);
    }

    [Fact]
    public void Slider_CustomRange_Renders()
    {
        float val = 50f;
        _fixture.RunFrame(() => ClayUI.Slider("S3", ref val, min: 0f, max: 100f));
    }

    [Fact]
    public void Slider_EmptyLabel_Renders()
    {
        float val = 0.5f;
        _fixture.RunFrame(() => ClayUI.Slider("", ref val));
    }

    [Fact]
    public void Slider_GeneratesRenderCommands()
    {
        float val = 0.5f;
        var commands = _fixture.RunFrame(() => ClayUI.Slider("SRender", ref val));

        int rectCount = 0;
        foreach (var cmd in commands)
        {
            if (cmd.CommandType == RenderCommandType.Rectangle) rectCount++;
        }

        // Should have track background + fill + possibly value text
        Assert.True(rectCount >= 2, $"Slider should render track and fill, got {rectCount} rects");
    }

    [Fact]
    public void Slider_WithLabel_GeneratesText()
    {
        float val = 0.5f;
        var commands = _fixture.RunFrame(() => ClayUI.Slider("My Slider", ref val));

        bool hasText = false;
        foreach (var cmd in commands)
        {
            if (cmd.CommandType == RenderCommandType.Text)
            {
                hasText = true;
                break;
            }
        }

        Assert.True(hasText, "Slider with label should generate text render command");
    }
}
