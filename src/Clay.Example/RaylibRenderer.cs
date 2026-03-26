using Clay;
using Clay.Widgets;
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
                    RenderCustom(box, cmd.Custom);
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

    private void RenderCustom(BoundingBox box, CustomRenderData data)
    {
        if (data.CustomData is TextInputWidget widget)
            RenderTextInput(box, widget);
    }

    private void RenderTextInput(BoundingBox box, TextInputWidget widget)
    {
        var style = widget.CurrentStyle;
        var padding = style.Padding;

        // Background
        var bgRect = new Rectangle(box.X, box.Y, box.Width, box.Height);
        var bgColor = widget.IsFocused ? style.FocusedBackgroundColor : style.BackgroundColor;
        if (style.CornerRadius.TopLeft > 0)
        {
            float minDim = Math.Min(box.Width, box.Height);
            float roundness = Math.Clamp((style.CornerRadius.TopLeft * 2) / minDim, 0, 1);
            Raylib.DrawRectangleRounded(bgRect, roundness, 8, ToRayColor(bgColor));
        }
        else
        {
            Raylib.DrawRectangleRec(bgRect, ToRayColor(bgColor));
        }

        // Border
        if (style.Border.Width.Top > 0 || style.Border.Width.Left > 0)
        {
            float minDim = Math.Min(box.Width, box.Height);
            float roundness = style.CornerRadius.TopLeft > 0
                ? Math.Clamp((style.CornerRadius.TopLeft * 2) / minDim, 0, 1) : 0;
            float lineThick = Math.Max(
                Math.Max(style.Border.Width.Top, style.Border.Width.Bottom),
                Math.Max(style.Border.Width.Left, style.Border.Width.Right));
            Raylib.DrawRectangleRoundedLines(bgRect, roundness, 8, lineThick, ToRayColor(style.Border.Color));
        }

        // Clip content to element bounds
        PushScissor(box);

        // Content area (offset by scroll)
        float textX = box.X + padding.Left;
        float textY = box.Y + padding.Top - widget.ScrollY;
        float lineHeight = widget.ComputedLineHeight;

        // Draw selection highlight
        if (widget.IsFocused && widget.HasSelection)
        {
            int selStart = Math.Min(widget.SelectionStart, widget.SelectionEnd);
            int selEnd = Math.Max(widget.SelectionStart, widget.SelectionEnd);
            var selColor = ToRayColor(style.SelectionColor);

            // For each line that intersects the selection
            int pos = 0;
            int row = 0;
            string text = widget.Text;
            while (pos <= text.Length && pos < selEnd)
            {
                int lineStart = pos;
                int lineEnd = text.IndexOf('\n', pos);
                if (lineEnd < 0) lineEnd = text.Length;

                // Does this line intersect the selection?
                if (lineEnd > selStart && lineStart < selEnd)
                {
                    int hlStart = Math.Max(lineStart, selStart);
                    int hlEnd = Math.Min(lineEnd, selEnd);
                    float x1 = textX + widget.MeasureSubstring(lineStart, hlStart);
                    float x2 = textX + widget.MeasureSubstring(lineStart, hlEnd);
                    float w = x2 - x1;

                    // Selection spans past the end of this line (into the \n):
                    // show a minimal marker so the user sees the line is selected
                    if (selEnd > lineEnd && w < 1f)
                        w = 1f;

                    float y = textY + row * lineHeight;
                    Raylib.DrawRectangleRec(
                        new Rectangle(x1, y, w, lineHeight),
                        selColor);
                }

                pos = lineEnd + 1;
                row++;
            }
        }

        // Draw text line by line (so line spacing matches cursor positioning exactly)
        if (widget.Text.Length > 0 && style.FontId < _fonts.Length)
        {
            var font = _fonts[style.FontId];
            var textColor = ToRayColor(style.TextColor);
            string text = widget.Text;
            int lineStart = 0;
            int row = 0;
            while (lineStart <= text.Length)
            {
                int lineEnd = text.IndexOf('\n', lineStart);
                if (lineEnd < 0) lineEnd = text.Length;
                if (lineEnd > lineStart)
                {
                    string line = text.Substring(lineStart, lineEnd - lineStart);
                    Raylib.DrawTextEx(font, line,
                        new System.Numerics.Vector2(textX, textY + row * lineHeight),
                        style.FontSize, style.LetterSpacing, textColor);
                }
                lineStart = lineEnd + 1;
                row++;
            }
        }

        // Draw cursor (blink every 0.5s)
        if (widget.IsFocused && (int)(Raylib.GetTime() * 2) % 2 == 0)
        {
            var (cursorRow, cursorCol) = widget.GetRowCol(widget.CursorIndex);
            int lineStart = widget.FindLineStart(widget.CursorIndex);
            float cursorX = textX + widget.MeasureSubstring(lineStart, widget.CursorIndex);
            float cursorY = textY + cursorRow * lineHeight;

            Raylib.DrawRectangleRec(
                new Rectangle(cursorX, cursorY, 1.5f, lineHeight),
                ToRayColor(style.CursorColor));
        }

        PopScissor();
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
