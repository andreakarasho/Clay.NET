using System.Collections;
using System.Reflection;
using Clay;

namespace Clay.Test;

public class ClayUIColorPickerTests : IDisposable
{
    private readonly ClayUIFixture _fixture;

    public ClayUIColorPickerTests()
    {
        _fixture = new ClayUIFixture();
    }

    public void Dispose() => _fixture.Dispose();

    [Fact]
    public void ColorPicker_StateKey_RemainsStable_WhenWidgetOrderChanges()
    {
        _fixture.RunFrame(() =>
        {
            ClayUI.Button("BeforePicker");
            _ = ClayUI.ColorPicker("Picker", Color.Red);
        });

        _fixture.RunFrame(() =>
        {
            _ = ClayUI.ColorPicker("Picker", Color.Red);
        });

        Assert.Equal(1, GetColorPickerStateCount());
    }

    private static int GetColorPickerStateCount()
    {
        var handle = ClayUI.GetContext();
        var uiContextProp = typeof(ClayUIContextHandle).GetProperty("UIContext", BindingFlags.Instance | BindingFlags.NonPublic)!;
        var uiContext = uiContextProp.GetValue(handle)!;
        var statesField = uiContext.GetType().GetField("ColorPickerStates", BindingFlags.Instance | BindingFlags.NonPublic)!;
        var states = (IDictionary)statesField.GetValue(uiContext)!;
        return states.Count;
    }
}
