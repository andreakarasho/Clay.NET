using Clay;
using ZeroElectric.Vinculum;
using RayColor = ZeroElectric.Vinculum.Color;
using ClayColor = Clay.Color;

namespace Clay.Example;

/// <summary>
/// Raylib renderer implementation for Clay UI.
/// </summary>
public class RaylibRenderer : IClayRenderer
{
    private readonly Font[] _fonts;
    private readonly Stack<Rectangle> _scissorStack = new();

    public RaylibRenderer(Font[] fonts)
    {
        _fonts = fonts;
    }

    public void Render(ReadOnlySpan<RenderCommand> commands)
    {
        _scissorStack.Clear();

        foreach (ref readonly var cmd in commands)
        {
            var box = cmd.BoundingBox;

            switch (cmd.CommandType)
            {
                case RenderCommandType.None:
                    break;

                case RenderCommandType.Rectangle:
                    RenderRectangle(box, cmd.Rectangle);
                    break;

                case RenderCommandType.Border:
                    RenderBorder(box, cmd.Border);
                    break;

                case RenderCommandType.Text:
                    RenderText(box, cmd.Text);
                    break;

                case RenderCommandType.Image:
                    RenderImage(box, cmd.Image);
                    break;

                case RenderCommandType.ScissorStart:
                    PushScissor(box);
                    break;

                case RenderCommandType.ScissorEnd:
                    PopScissor();
                    break;

                case RenderCommandType.Custom:
                    // Custom rendering can be handled via UserData
                    break;
            }
        }
    }

    private void PushScissor(BoundingBox box)
    {
        var newScissor = new Rectangle(box.X, box.Y, box.Width, box.Height);

        // If there's an existing scissor, intersect with it
        if (_scissorStack.Count > 0)
        {
            var current = _scissorStack.Peek();
            newScissor = IntersectRectangles(current, newScissor);
        }

        _scissorStack.Push(newScissor);
        ApplyScissor(newScissor);
    }

    private void PopScissor()
    {
        if (_scissorStack.Count > 0)
        {
            _scissorStack.Pop();
        }

        if (_scissorStack.Count > 0)
        {
            // Restore previous scissor
            ApplyScissor(_scissorStack.Peek());
        }
        else
        {
            // No more scissors, disable scissoring
            Raylib.EndScissorMode();
        }
    }

    private void ApplyScissor(Rectangle rect)
    {
        // Ensure non-negative dimensions
        int x = (int)rect.x;
        int y = (int)rect.y;
        int w = Math.Max(0, (int)rect.width);
        int h = Math.Max(0, (int)rect.height);

        Raylib.BeginScissorMode(x, y, w, h);
    }

    private static Rectangle IntersectRectangles(Rectangle a, Rectangle b)
    {
        float x1 = Math.Max(a.x, b.x);
        float y1 = Math.Max(a.y, b.y);
        float x2 = Math.Min(a.x + a.width, b.x + b.width);
        float y2 = Math.Min(a.y + a.height, b.y + b.height);

        float width = Math.Max(0, x2 - x1);
        float height = Math.Max(0, y2 - y1);

        return new Rectangle(x1, y1, width, height);
    }

    private void RenderRectangle(BoundingBox box, RectangleRenderData data)
    {
        var rect = new Rectangle(box.X, box.Y, box.Width, box.Height);
        var color = ToRayColor(data.BackgroundColor);

        if (data.CornerRadius.TopLeft > 0)
        {
            // Calculate roundness as ratio of corner radius to smallest dimension
            float minDimension = Math.Min(box.Width, box.Height);
            float roundness = (data.CornerRadius.TopLeft * 2) / minDimension;
            roundness = Math.Clamp(roundness, 0, 1);

            Raylib.DrawRectangleRounded(rect, roundness, 8, color);
        }
        else
        {
            Raylib.DrawRectangleRec(rect, color);
        }
    }

    private void RenderBorder(BoundingBox box, BorderRenderData data)
    {
        var color = ToRayColor(data.Color);

        // Use rounded border if corner radius is set
        if (data.CornerRadius.TopLeft > 0)
        {
            var rect = new Rectangle(box.X, box.Y, box.Width, box.Height);
            float minDimension = Math.Min(box.Width, box.Height);
            float roundness = (data.CornerRadius.TopLeft * 2) / minDimension;
            roundness = Math.Clamp(roundness, 0, 1);

            // Use the maximum border width for the line thickness
            float lineThickness = Math.Max(
                Math.Max(data.Width.Top, data.Width.Bottom),
                Math.Max(data.Width.Left, data.Width.Right)
            );

            Raylib.DrawRectangleRoundedLines(rect, roundness, 8, lineThickness, color);
        }
        else
        {
            // Fall back to straight borders
            // Top border
            if (data.Width.Top > 0)
            {
                Raylib.DrawRectangle(
                    (int)box.X,
                    (int)box.Y,
                    (int)box.Width,
                    (int)data.Width.Top,
                    color
                );
            }

            // Bottom border
            if (data.Width.Bottom > 0)
            {
                Raylib.DrawRectangle(
                    (int)box.X,
                    (int)(box.Y + box.Height - data.Width.Bottom),
                    (int)box.Width,
                    (int)data.Width.Bottom,
                    color
                );
            }

            // Left border
            if (data.Width.Left > 0)
            {
                Raylib.DrawRectangle(
                    (int)box.X,
                    (int)box.Y,
                    (int)data.Width.Left,
                    (int)box.Height,
                    color
                );
            }

            // Right border
            if (data.Width.Right > 0)
            {
                Raylib.DrawRectangle(
                    (int)(box.X + box.Width - data.Width.Right),
                    (int)box.Y,
                    (int)data.Width.Right,
                    (int)box.Height,
                    color
                );
            }
        }
    }

    private void RenderText(BoundingBox box, TextRenderData data)
    {
        if (string.IsNullOrEmpty(data.Text))
            return;

        if (data.FontId >= _fonts.Length)
            return;

        var font = _fonts[data.FontId];
        var position = new System.Numerics.Vector2(box.X, box.Y);
        var color = ToRayColor(data.TextColor);

        Raylib.DrawTextEx(
            font,
            data.Text,
            position,
            data.FontSize,
            data.LetterSpacing,
            color
        );
    }

    private void RenderImage(BoundingBox box, ImageRenderData data)
    {
        if (data.ImageData is not Texture texture)
            return;

        var position = new System.Numerics.Vector2(box.X, box.Y);

        // Calculate scale to fit bounding box
        float scaleX = box.Width / texture.width;
        float scaleY = box.Height / texture.height;
        float scale = Math.Min(scaleX, scaleY);

        Raylib.DrawTextureEx(texture, position, 0, scale, Raylib.WHITE);
    }

    private static RayColor ToRayColor(ClayColor color)
    {
        return new RayColor
        {
            r = (byte)Math.Clamp(color.R, 0, 255),
            g = (byte)Math.Clamp(color.G, 0, 255),
            b = (byte)Math.Clamp(color.B, 0, 255),
            a = (byte)Math.Clamp(color.A, 0, 255)
        };
    }
}
