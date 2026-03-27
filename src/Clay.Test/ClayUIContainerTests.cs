using System.Numerics;
using Clay;

namespace Clay.Test;

public class ClayUIContainerTests : IDisposable
{
    private readonly ClayUIFixture _fixture;

    public ClayUIContainerTests()
    {
        _fixture = new ClayUIFixture();
    }

    public void Dispose() => _fixture.Dispose();

    // ============ Horizontal Layout ============

    [Fact]
    public void BeginEndHorizontal_Renders_WithoutCrash()
    {
        _fixture.RunFrame(() =>
        {
            ClayUI.BeginHorizontal();
            ClayUI.Label("A");
            ClayUI.Label("B");
            ClayUI.EndHorizontal();
        });
    }

    [Fact]
    public void BeginEndHorizontal_CustomGap_Renders()
    {
        _fixture.RunFrame(() =>
        {
            ClayUI.BeginHorizontal(gap: 16);
            ClayUI.Label("X");
            ClayUI.Label("Y");
            ClayUI.EndHorizontal();
        });
    }

    [Fact]
    public void BeginEndHorizontal_WithScroll_Renders()
    {
        _fixture.RunFrame(() =>
        {
            ClayUI.BeginHorizontal(scroll: true, maxWidth: 200);
            for (int i = 0; i < 20; i++)
                ClayUI.Label($"Item {i}");
            ClayUI.EndHorizontal();
        });
    }

    [Fact]
    public void BeginEndHorizontal_WithScroll_GeneratesScissorCommands()
    {
        var commands = _fixture.RunFrame(() =>
        {
            ClayUI.BeginHorizontal(scroll: true, maxWidth: 200);
            for (int i = 0; i < 20; i++)
                ClayUI.Label($"Item {i}");
            ClayUI.EndHorizontal();
        });

        bool hasScissor = false;
        foreach (var cmd in commands)
        {
            if (cmd.CommandType == RenderCommandType.ScissorStart)
            {
                hasScissor = true;
                break;
            }
        }

        Assert.True(hasScissor, "Horizontal scroll should generate scissor commands");
    }

    // ============ Vertical Layout ============

    [Fact]
    public void BeginEndVertical_Renders_WithoutCrash()
    {
        _fixture.RunFrame(() =>
        {
            ClayUI.BeginVertical();
            ClayUI.Label("Row 1");
            ClayUI.Label("Row 2");
            ClayUI.EndVertical();
        });
    }

    [Fact]
    public void BeginEndVertical_CustomGapAndAlignment_Renders()
    {
        _fixture.RunFrame(() =>
        {
            ClayUI.BeginVertical(gap: 4, alignment: ChildAlignment.Center);
            ClayUI.Label("Centered");
            ClayUI.EndVertical();
        });
    }

    [Fact]
    public void BeginEndVertical_WithScroll_Renders()
    {
        _fixture.RunFrame(() =>
        {
            ClayUI.BeginVertical(scroll: true, maxHeight: 100);
            for (int i = 0; i < 20; i++)
                ClayUI.Label($"Row {i}");
            ClayUI.EndVertical();
        });
    }

    [Fact]
    public void BeginEndVertical_WithScroll_GeneratesScissorCommands()
    {
        var commands = _fixture.RunFrame(() =>
        {
            ClayUI.BeginVertical(scroll: true, maxHeight: 100);
            for (int i = 0; i < 20; i++)
                ClayUI.Label($"Row {i}");
            ClayUI.EndVertical();
        });

        bool hasScissor = false;
        foreach (var cmd in commands)
        {
            if (cmd.CommandType == RenderCommandType.ScissorStart)
            {
                hasScissor = true;
                break;
            }
        }

        Assert.True(hasScissor, "Vertical scroll should generate scissor commands");
    }

    // ============ Nesting ============

    [Fact]
    public void Nested_HorizontalInVertical_Renders()
    {
        _fixture.RunFrame(() =>
        {
            ClayUI.BeginVertical();
            ClayUI.Label("Top");
            ClayUI.BeginHorizontal();
            ClayUI.Button("A");
            ClayUI.Button("B");
            ClayUI.EndHorizontal();
            ClayUI.Label("Bottom");
            ClayUI.EndVertical();
        });
    }

    [Fact]
    public void Nested_VerticalInHorizontal_Renders()
    {
        _fixture.RunFrame(() =>
        {
            ClayUI.BeginHorizontal();
            ClayUI.BeginVertical();
            ClayUI.Label("Col1 Row1");
            ClayUI.Label("Col1 Row2");
            ClayUI.EndVertical();
            ClayUI.BeginVertical();
            ClayUI.Label("Col2 Row1");
            ClayUI.Label("Col2 Row2");
            ClayUI.EndVertical();
            ClayUI.EndHorizontal();
        });
    }

    // ============ Panel ============

    [Fact]
    public void BeginEndPanel_WithTitle_Renders()
    {
        _fixture.RunFrame(() =>
        {
            ClayUI.BeginPanel("My Panel");
            ClayUI.Label("Panel content");
            ClayUI.EndPanel();
        });
    }

    [Fact]
    public void BeginEndPanel_WithoutTitle_Renders()
    {
        _fixture.RunFrame(() =>
        {
            ClayUI.BeginPanel("");
            ClayUI.Label("No title panel");
            ClayUI.EndPanel();
        });
    }

    [Fact]
    public void BeginEndPanel_WithScroll_Renders()
    {
        _fixture.RunFrame(() =>
        {
            ClayUI.BeginPanel("Scrolling Panel", scroll: true, maxHeight: 100);
            for (int i = 0; i < 20; i++)
                ClayUI.Label($"Line {i}");
            ClayUI.EndPanel();
        });
    }

    [Fact]
    public void BeginEndPanel_GeneratesRenderCommands()
    {
        var commands = _fixture.RunFrame(() =>
        {
            ClayUI.BeginPanel("Styled Panel");
            ClayUI.Label("Content");
            ClayUI.EndPanel();
        });

        bool hasRect = false;
        bool hasText = false;
        foreach (var cmd in commands)
        {
            if (cmd.CommandType == RenderCommandType.Rectangle) hasRect = true;
            if (cmd.CommandType == RenderCommandType.Text) hasText = true;
        }

        Assert.True(hasRect, "Panel should generate rectangle for background");
        Assert.True(hasText, "Panel should generate text for title and content");
    }

    [Fact]
    public void Panel_Nested_Renders()
    {
        _fixture.RunFrame(() =>
        {
            ClayUI.BeginPanel("Outer");
            ClayUI.BeginPanel("Inner");
            ClayUI.Label("Deep content");
            ClayUI.EndPanel();
            ClayUI.EndPanel();
        });
    }

    [Fact]
    public void Panel_WithWidgetsInside_Renders()
    {
        bool check = false;
        float slider = 0.5f;

        _fixture.RunFrame(() =>
        {
            ClayUI.BeginPanel("Widgets");
            ClayUI.Button("Click");
            ClayUI.Checkbox("Check", ref check);
            ClayUI.Slider("Slide", ref slider);
            ClayUI.ProgressBar(0.7f);
            ClayUI.EndPanel();
        });
    }
}
