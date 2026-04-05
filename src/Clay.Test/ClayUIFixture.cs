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
        Vector2 scrollDelta = default,
        float deltaTime = 1f / 60f)
    {
        ClayUI.BeginFrame(new Dimensions(800, 600), mouseDown, mousePos, scrollDelta, deltaTime);

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
    /// Simulates a click: runs frame 1 (establish bounds), frame 2 (press down),
    /// frame 3 (release — triggers click). Returns frame 3 commands.
    /// The buildUi action can capture return values via closures.
    /// </summary>
    public ReadOnlySpan<RenderCommand> Click(Action buildUi, Vector2 mousePos)
    {
        // Frame 1: establish bounding boxes (no interaction)
        RunFrame(buildUi, mousePos, mouseDown: false);

        // Frame 2: press down
        RunFrame(buildUi, mousePos, mouseDown: true);

        // Frame 3: release — ShouldProcessClick fires on mouse release
        return RunFrame(buildUi, mousePos, mouseDown: false);
    }

    public void Dispose()
    {
        ClayUI.ClearState();
        ClayApi.Shutdown();
    }
}
