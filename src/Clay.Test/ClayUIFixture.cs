using System.Numerics;
using Clay;

namespace Clay.Test;

/// <summary>
/// Test fixture for ClayUI widget tests.
/// Handles the full frame lifecycle and provides helpers for interaction testing.
/// </summary>
public sealed class ClayUIFixture : IDisposable
{
    public ClayUIFixture(float width = 800, float height = 600)
    {
        ClayApi.Initialize(new Dimensions(width, height), new SimpleTextMeasurer(), maxElementCount: 8192);
    }

    /// <summary>
    /// Runs a single UI frame: BeginFrame -> BeginLayout -> buildUi -> EndLayout.
    /// </summary>
    public ReadOnlySpan<RenderCommand> RunFrame(
        Action buildUi,
        Vector2 mousePos = default,
        bool mouseDown = false,
        Vector2 scrollDelta = default)
    {
        ClayUI.BeginFrame(new Dimensions(800, 600), mouseDown, mousePos, scrollDelta);

        // Root container so widgets have a parent with known size
        using (ClayApi.Element(new ElementDeclaration
        {
            Layout = new LayoutConfig
            {
                Direction = LayoutDirection.TopToBottom,
                Sizing = Sizing.Fill(),
                Padding = Padding.All(0)
            }
        }))
        {
            buildUi();
        }

        return ClayUI.EndFrame();
    }

    /// <summary>
    /// Runs two identical frames. Frame 1 establishes bounding boxes in the hash map.
    /// Frame 2 can then use PointerOver for hover/click detection.
    /// Returns render commands from frame 2.
    /// </summary>
    public ReadOnlySpan<RenderCommand> RunTwoFrames(
        Action buildUi,
        Vector2 mousePos = default,
        bool mouseDown = false,
        Vector2 scrollDelta = default)
    {
        // Frame 1: establish bounding boxes (no interaction)
        RunFrame(buildUi, mousePos, mouseDown: false);

        // Frame 2: with actual mouse state for interaction
        return RunFrame(buildUi, mousePos, mouseDown, scrollDelta);
    }

    /// <summary>
    /// Simulates a click: runs frame 1 (no press), frame 2 (press down), returns frame 2 commands.
    /// The buildUi action can capture return values via closures.
    /// </summary>
    public ReadOnlySpan<RenderCommand> Click(Action buildUi, Vector2 mousePos)
    {
        return RunTwoFrames(buildUi, mousePos, mouseDown: true);
    }

    public void Dispose()
    {
        ClayUI.ClearState();
        ClayApi.Shutdown();
    }
}
