using System.Numerics;
using Clay;
using Xunit;

namespace Clay.Test;

/// <summary>
/// Press-capture semantics: a click fires only when the mouse is pressed and
/// released over the same element. Pressing on one element, dragging, and
/// releasing over another must NOT click the release target.
/// </summary>
public class ClayUIClickCaptureTests : IDisposable
{
    private readonly ClayUIFixture _fixture;

    public ClayUIClickCaptureTests() => _fixture = new ClayUIFixture();

    public void Dispose() => _fixture.Dispose();

    private static Vector2 CenterOf(ReadOnlySpan<RenderCommand> cmds, string label)
    {
        foreach (var c in cmds)
            if (c.CommandType == RenderCommandType.Text && c.Text.Text == label)
                return new Vector2(c.BoundingBox.X + c.BoundingBox.Width / 2,
                                   c.BoundingBox.Y + c.BoundingBox.Height / 2);
        return new Vector2(-1, -1);
    }

    [Fact]
    public void PressOnOneElement_ReleaseOnAnother_DoesNotClick()
    {
        bool aClicked = false, bClicked = false;
        Action build = () =>
        {
            if (ClayUI.Button("BtnA")) aClicked = true;
            if (ClayUI.Button("BtnB")) bClicked = true;
        };

        // Frame 1 establishes button bounds in the hash map.
        var cmds = _fixture.RunFrame(build);
        var aPos = CenterOf(cmds, "BtnA");
        var bPos = CenterOf(cmds, "BtnB");
        Assert.True(aPos.X > 0 && bPos.X > 0, "buttons were not laid out");

        // Press on A, drag, release over B.
        aClicked = bClicked = false;
        _fixture.RunFrame(build, aPos, mouseDown: false); // hover A
        _fixture.RunFrame(build, aPos, mouseDown: true);  // press on A
        _fixture.RunFrame(build, bPos, mouseDown: false); // release over B

        Assert.False(bClicked, "B must not click when the press began on A");
        Assert.False(aClicked, "A must not click when released over B");

        // Control: press + release on the same element clicks it.
        aClicked = bClicked = false;
        _fixture.RunFrame(build, aPos, mouseDown: false);
        _fixture.RunFrame(build, aPos, mouseDown: true);
        _fixture.RunFrame(build, aPos, mouseDown: false);

        Assert.True(aClicked, "A must click on same-element press + release");
        Assert.False(bClicked, "B must not click");
    }
}
