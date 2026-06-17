using Clay;
using Xunit;

namespace Clay.Test;

/// <summary>
/// Regression coverage for docked-window content positioning. The dock content
/// reads its leaf bounding box from the cross-frame element hash map; a per-frame
/// blanket clear of that map collapsed all docked content to the origin while the
/// window chrome (laid out directly in the tree) still positioned correctly.
/// </summary>
public class ClayUIDockContentTests : IDisposable
{
    private readonly ClayUIFixture _fixture;

    public ClayUIDockContentTests() => _fixture = new ClayUIFixture();

    public void Dispose() => _fixture.Dispose();

    [Fact]
    public void DockedContent_InheritsLeafPosition_NotOrigin()
    {
        bool open = true;

        Action build = () =>
        {
            ClayUI.BeginDockSpace("DockContentTest", setup: dock =>
            {
                var (left, right) = dock.Split(DockSplitDirection.Horizontal, 0.3f);
                dock.Window(left, "Left");
                dock.Window(right, "Right");
            });

            if (ClayUI.BeginWindow("Left", ref open, flags: WindowFlags.NoCollapse)) { }
            ClayUI.EndWindow();

            if (ClayUI.BeginWindow("Right", ref open, flags: WindowFlags.NoCollapse))
            {
                ClayApi.Text("RIGHT_CONTENT", new TextConfig { FontSize = 14 });
            }
            ClayUI.EndWindow();

            ClayUI.EndDockSpace();
        };

        // Frames 1-2 establish dock leaf bounds in the hash map; frame 3 reads them.
        _fixture.RunFrame(build);
        _fixture.RunFrame(build);
        var cmds = _fixture.RunFrame(build);

        float markerX = float.NaN;
        foreach (var c in cmds)
        {
            if (c.CommandType == RenderCommandType.Text && c.Text.Text == "RIGHT_CONTENT")
                markerX = c.BoundingBox.X;
        }

        Assert.False(float.IsNaN(markerX), "RIGHT_CONTENT text command not found");
        // Right panel starts at ~0.3 * 800 = 240. Content must land inside it, not at the origin.
        Assert.True(markerX > 200, $"docked content collapsed to origin: X={markerX} (expected > 200)");
    }
}
