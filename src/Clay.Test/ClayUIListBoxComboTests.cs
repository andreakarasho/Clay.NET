using System.Numerics;
using Clay;

namespace Clay.Test;

public class ClayUIListBoxComboTests : IDisposable
{
    private readonly ClayUIFixture _fixture;

    public ClayUIListBoxComboTests()
    {
        _fixture = new ClayUIFixture();
    }

    public void Dispose() => _fixture.Dispose();

    // ============ ListBox ============

    [Fact]
    public void ListBox_Renders_WithoutCrash()
    {
        _fixture.RunFrame(() =>
        {
            ClayUI.BeginListBox("TestList");
            ClayUI.ListBoxItem("Item 1", isSelected: true);
            ClayUI.ListBoxItem("Item 2", isSelected: false);
            ClayUI.ListBoxItem("Item 3", isSelected: false);
            ClayUI.EndListBox();
        });
    }

    [Fact]
    public void ListBox_ManyItems_Renders_WithoutCrash()
    {
        _fixture.RunFrame(() =>
        {
            ClayUI.BeginListBox("BigList", maxHeight: 100);
            for (int i = 0; i < 50; i++)
                ClayUI.ListBoxItem($"Item {i}##big_{i}", isSelected: i == 0);
            ClayUI.EndListBox();
        });
    }

    [Fact]
    public void ListBoxItem_NotClicked_ReturnsFalse()
    {
        bool clicked = false;
        _fixture.RunTwoFrames(() =>
        {
            ClayUI.BeginListBox("ClickList");
            clicked = ClayUI.ListBoxItem("Item 1", isSelected: false);
            ClayUI.EndListBox();
        }, mousePos: new Vector2(700, 700)); // far away

        Assert.False(clicked);
    }

    [Fact]
    public void ListBoxItem_Clicked_ReturnsTrue()
    {
        bool clicked = false;
        Action buildUi = () =>
        {
            ClayUI.BeginListBox("ClickList2");
            clicked = ClayUI.ListBoxItem("Item A##cl2_a", isSelected: false);
            ClayUI.EndListBox();
        };

        // Frame 1: establish layout
        _fixture.RunFrame(buildUi);

        // Frame 2: press down on the item
        _fixture.RunFrame(buildUi, mousePos: new Vector2(30, 30), mouseDown: true);

        // Frame 3: release — click fires on mouse release
        _fixture.RunFrame(buildUi, mousePos: new Vector2(30, 30), mouseDown: false);

        Assert.True(clicked);
    }

    [Fact]
    public void ListBox_SelectionTracking_Works()
    {
        int selected = 0;
        string[] items = ["Alpha", "Beta", "Gamma"];

        // Frame 1: establish layout
        _fixture.RunFrame(() =>
        {
            ClayUI.BeginListBox("SelectList");
            for (int i = 0; i < items.Length; i++)
            {
                if (ClayUI.ListBoxItem(items[i] + $"##sel_{i}", i == selected))
                    selected = i;
            }
            ClayUI.EndListBox();
        });

        Assert.Equal(0, selected); // Initial selection
    }

    [Fact]
    public void ListBox_WithLabel_Renders()
    {
        _fixture.RunFrame(() =>
        {
            ClayUI.BeginListBox("Labeled List");
            ClayUI.ListBoxItem("Only Item", isSelected: true);
            ClayUI.EndListBox();
        });
    }

    [Fact]
    public void ListBox_EmptyLabel_Renders()
    {
        _fixture.RunFrame(() =>
        {
            ClayUI.BeginListBox("##hidden_label");
            ClayUI.ListBoxItem("Item", isSelected: false);
            ClayUI.EndListBox();
        });
    }

    [Fact]
    public void ListBox_CustomStyle_Renders()
    {
        _fixture.RunFrame(() =>
        {
            ClayUI.BeginListBox("StyledList", style: new ListBoxStyle
            {
                BackgroundColor = Color.Rgba(20, 20, 25),
                SelectedColor = Color.Rgba(200, 50, 50),
                HoverColor = Color.Rgba(40, 40, 50)
            });
            ClayUI.ListBoxItem("Styled Item", isSelected: true);
            ClayUI.EndListBox();
        });
    }

    // ============ Combo ============

    [Fact]
    public void Combo_Renders_WithoutCrash()
    {
        int selected = 0;
        string[] options = ["One", "Two", "Three"];

        _fixture.RunFrame(() =>
        {
            ClayUI.Combo("Test", ref selected, options);
        });
    }

    [Fact]
    public void Combo_NotClicked_ReturnsFalse()
    {
        int selected = 1;
        string[] options = ["A", "B", "C"];
        bool changed = true;

        _fixture.RunTwoFrames(() =>
        {
            changed = ClayUI.Combo("NoClick", ref selected, options);
        }, mousePos: new Vector2(700, 700)); // far away

        Assert.False(changed);
        Assert.Equal(1, selected); // Unchanged
    }

    [Fact]
    public void Combo_ShowsSelectedOption()
    {
        int selected = 2;
        string[] options = ["Small", "Medium", "Large"];

        var commands = _fixture.RunFrame(() =>
        {
            ClayUI.Combo("Size", ref selected, options);
        });

        // Verify text "Large" appears in render commands
        bool hasSelectedText = false;
        foreach (var cmd in commands)
        {
            if (cmd.CommandType == RenderCommandType.Text && cmd.Text.Text == "Large")
            {
                hasSelectedText = true;
                break;
            }
        }
        Assert.True(hasSelectedText, "Combo should display the selected option text");
    }

    [Fact]
    public void Combo_InitialSelection_Preserved()
    {
        int selected = 0;
        string[] options = ["X", "Y", "Z"];

        _fixture.RunFrame(() =>
        {
            ClayUI.Combo("Preserve", ref selected, options);
        });

        Assert.Equal(0, selected);
    }

    [Fact]
    public void Combo_CustomStyle_Renders()
    {
        int selected = 0;
        string[] options = ["Alpha", "Beta"];

        _fixture.RunFrame(() =>
        {
            ClayUI.Combo("Styled", ref selected, options, style: new ComboStyle
            {
                BackgroundColor = Color.Rgba(60, 20, 20),
                SelectedColor = Color.Rgba(200, 50, 50)
            });
        });
    }

    [Fact]
    public void Combo_EmptyOptions_Renders()
    {
        int selected = 0;
        string[] options = [];

        _fixture.RunFrame(() =>
        {
            ClayUI.Combo("Empty", ref selected, options);
        });
    }

    [Fact]
    public void Combo_WithLabel_RendersLabel()
    {
        int selected = 0;
        string[] options = ["Opt"];

        var commands = _fixture.RunFrame(() =>
        {
            ClayUI.Combo("My Label", ref selected, options);
        });

        bool hasLabel = false;
        foreach (var cmd in commands)
        {
            if (cmd.CommandType == RenderCommandType.Text && cmd.Text.Text == "My Label")
            {
                hasLabel = true;
                break;
            }
        }
        Assert.True(hasLabel, "Combo should render its label");
    }

    [Fact]
    public void Combo_MultipleOnSamePage_Render()
    {
        int sel1 = 0, sel2 = 1;
        string[] opts1 = ["A", "B"];
        string[] opts2 = ["X", "Y", "Z"];

        _fixture.RunFrame(() =>
        {
            ClayUI.Combo("First##c1", ref sel1, opts1);
            ClayUI.Combo("Second##c2", ref sel2, opts2);
        });
    }
}
