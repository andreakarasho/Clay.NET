using Clay;
using Clay.Widgets;
using ZeroElectric.Vinculum;
using RayColor = ZeroElectric.Vinculum.Color;
using ClayColor = Clay.Color;

namespace Clay.GameEditor;

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

                case RenderCommandType.Shadow:
                    RenderShadow(box, cmd.Shadow);
                    break;
            }
        }
    }

    private void PushScissor(BoundingBox box)
    {
        var newScissor = new Rectangle(box.X, box.Y, box.Width, box.Height);

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
            _scissorStack.Pop();

        if (_scissorStack.Count > 0)
            ApplyScissor(_scissorStack.Peek());
        else
            Raylib.EndScissorMode();
    }

    private void ApplyScissor(Rectangle rect)
    {
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
        return new Rectangle(x1, y1, Math.Max(0, x2 - x1), Math.Max(0, y2 - y1));
    }

    private void RenderRectangle(BoundingBox box, RectangleRenderData data)
    {
        var rect = new Rectangle(box.X, box.Y, box.Width, box.Height);
        var color = ToRayColor(data.BackgroundColor);

        if (data.CornerRadius.TopLeft > 0)
        {
            float minDimension = Math.Min(box.Width, box.Height);
            float roundness = Math.Clamp((data.CornerRadius.TopLeft * 2) / minDimension, 0, 1);
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

        if (data.CornerRadius.TopLeft > 0)
        {
            float r = data.CornerRadius.TopLeft;
            float bw = Math.Max(
                Math.Max(data.Width.Top, data.Width.Bottom),
                Math.Max(data.Width.Left, data.Width.Right));

            // Clamp radius to half the smallest dimension
            r = Math.Min(r, Math.Min(box.Width, box.Height) / 2f);

            // Draw four straight border segments (between the corner arcs)
            // Top
            Raylib.DrawRectangleRec(new Rectangle(box.X + r, box.Y, box.Width - 2 * r, bw), color);
            // Bottom
            Raylib.DrawRectangleRec(new Rectangle(box.X + r, box.Y + box.Height - bw, box.Width - 2 * r, bw), color);
            // Left
            Raylib.DrawRectangleRec(new Rectangle(box.X, box.Y + r, bw, box.Height - 2 * r), color);
            // Right
            Raylib.DrawRectangleRec(new Rectangle(box.X + box.Width - bw, box.Y + r, bw, box.Height - 2 * r), color);

            // Draw four corner arcs using DrawRing (annulus sector)
            float outerR = r;
            float innerR = Math.Max(0, r - bw);
            int segments = 8;

            // Top-left corner
            Raylib.DrawRing(
                new System.Numerics.Vector2(box.X + r, box.Y + r),
                innerR, outerR, 180, 270, segments, color);
            // Top-right corner
            Raylib.DrawRing(
                new System.Numerics.Vector2(box.X + box.Width - r, box.Y + r),
                innerR, outerR, 270, 360, segments, color);
            // Bottom-right corner
            Raylib.DrawRing(
                new System.Numerics.Vector2(box.X + box.Width - r, box.Y + box.Height - r),
                innerR, outerR, 0, 90, segments, color);
            // Bottom-left corner
            Raylib.DrawRing(
                new System.Numerics.Vector2(box.X + r, box.Y + box.Height - r),
                innerR, outerR, 90, 180, segments, color);
        }
        else
        {
            if (data.Width.Top > 0)
                Raylib.DrawRectangle((int)box.X, (int)box.Y, (int)box.Width, (int)data.Width.Top, color);
            if (data.Width.Bottom > 0)
                Raylib.DrawRectangle((int)box.X, (int)(box.Y + box.Height - data.Width.Bottom), (int)box.Width, (int)data.Width.Bottom, color);
            if (data.Width.Left > 0)
                Raylib.DrawRectangle((int)box.X, (int)box.Y, (int)data.Width.Left, (int)box.Height, color);
            if (data.Width.Right > 0)
                Raylib.DrawRectangle((int)(box.X + box.Width - data.Width.Right), (int)box.Y, (int)data.Width.Right, (int)box.Height, color);
        }
    }

    private void RenderText(BoundingBox box, TextRenderData data)
    {
        if (string.IsNullOrEmpty(data.Text)) return;
        if (data.FontId >= _fonts.Length) return;

        var font = _fonts[data.FontId];
        Raylib.DrawTextEx(font, data.Text,
            new System.Numerics.Vector2(box.X, box.Y),
            data.FontSize, data.LetterSpacing, ToRayColor(data.TextColor));
    }

    private void RenderImage(BoundingBox box, ImageRenderData data)
    {
        if (data.ImageData is not Texture texture) return;

        var tint = data.BackgroundColor.A > 0 ? ToRayColor(data.BackgroundColor) : Raylib.WHITE;

        if (data.Slice.HasSlice)
        {
            RenderNineSlice(box, texture, data.Slice, tint);
        }
        else
        {
            var source = new Rectangle(0, 0, texture.width, texture.height);
            var dest = new Rectangle(box.X, box.Y, box.Width, box.Height);
            Raylib.DrawTexturePro(texture, source, dest, new System.Numerics.Vector2(0, 0), 0, tint);
        }
    }

    private static void RenderNineSlice(BoundingBox box, Texture texture, NineSlice slice, RayColor tint)
    {
        float srcW = texture.width, srcH = texture.height;
        float sl = slice.Left, sr = slice.Right, st = slice.Top, sb = slice.Bottom;
        float dl = Math.Min(sl, box.Width * 0.5f), dr = Math.Min(sr, box.Width * 0.5f);
        float dt = Math.Min(st, box.Height * 0.5f), db = Math.Min(sb, box.Height * 0.5f);
        float midSrcW = srcW - sl - sr, midSrcH = srcH - st - sb;
        float midDstW = box.Width - dl - dr, midDstH = box.Height - dt - db;
        var origin = new System.Numerics.Vector2(0, 0);

        void Patch(float sx, float sy, float sw, float sh, float dx, float dy, float dw, float dh)
        {
            if (sw <= 0 || sh <= 0 || dw <= 0 || dh <= 0) return;
            Raylib.DrawTexturePro(texture, new Rectangle(sx, sy, sw, sh), new Rectangle(dx, dy, dw, dh), origin, 0, tint);
        }

        float x = box.X, y = box.Y;
        Patch(0, 0, sl, st, x, y, dl, dt);
        Patch(sl, 0, midSrcW, st, x + dl, y, midDstW, dt);
        Patch(srcW - sr, 0, sr, st, x + dl + midDstW, y, dr, dt);
        Patch(0, st, sl, midSrcH, x, y + dt, dl, midDstH);
        Patch(sl, st, midSrcW, midSrcH, x + dl, y + dt, midDstW, midDstH);
        Patch(srcW - sr, st, sr, midSrcH, x + dl + midDstW, y + dt, dr, midDstH);
        Patch(0, srcH - sb, sl, sb, x, y + dt + midDstH, dl, db);
        Patch(sl, srcH - sb, midSrcW, sb, x + dl, y + dt + midDstH, midDstW, db);
        Patch(srcW - sr, srcH - sb, sr, sb, x + dl + midDstW, y + dt + midDstH, dr, db);
    }

    private Texture _svTexture;
    private float _svCachedHue = -1;
    private int _svCachedWidth, _svCachedHeight;
    private Texture _hueBarTexture;
    private int _hueBarCachedWidth, _hueBarCachedHeight;

    private void RenderCustom(BoundingBox box, CustomRenderData data)
    {
        if (data.CustomData is TextInputWidget widget)
            RenderTextInput(box, widget);
        else if (data.CustomData is HsvGradientData gradient)
            RenderHsvGradient(box, gradient);
        else if (data.CustomData is ViewportTextureData viewport)
            RenderViewportTexture(box, viewport);
    }

    public void RenderViewportTexture(BoundingBox box, ViewportTextureData viewport)
    {
        if (viewport.RenderTexture is not RenderTexture rt) return;
        var texture = rt.texture;
        // RenderTexture is flipped vertically, so we use negative height in source rect
        var source = new Rectangle(0, 0, texture.width, -texture.height);
        var dest = new Rectangle(box.X, box.Y, box.Width, box.Height);
        Raylib.DrawTexturePro(texture, source, dest, new System.Numerics.Vector2(0, 0), 0, Raylib.WHITE);
    }

    private unsafe void RenderHsvGradient(BoundingBox box, HsvGradientData gradient)
    {
        int w = Math.Max(1, (int)box.Width);
        int h = Math.Max(1, (int)box.Height);

        if (gradient.Type == HsvGradientType.SaturationValue)
        {
            if (Math.Abs(gradient.Hue - _svCachedHue) > 0.01f || w != _svCachedWidth || h != _svCachedHeight)
            {
                if (_svCachedWidth > 0) Raylib.UnloadTexture(_svTexture);
                var img = Raylib.GenImageColor(w, h, Raylib.BLACK);
                for (int y = 0; y < h; y++)
                {
                    float value = 1f - (float)y / h;
                    for (int x = 0; x < w; x++)
                        Raylib.ImageDrawPixel(&img, x, y, Raylib.ColorFromHSV(gradient.Hue, (float)x / w, value));
                }
                _svTexture = Raylib.LoadTextureFromImage(img);
                Raylib.UnloadImage(img);
                _svCachedHue = gradient.Hue;
                _svCachedWidth = w;
                _svCachedHeight = h;
            }
            Raylib.DrawTexturePro(_svTexture, new Rectangle(0, 0, w, h),
                new Rectangle(box.X, box.Y, box.Width, box.Height), new System.Numerics.Vector2(0, 0), 0, Raylib.WHITE);
        }
        else
        {
            if (w != _hueBarCachedWidth || h != _hueBarCachedHeight)
            {
                if (_hueBarCachedWidth > 0) Raylib.UnloadTexture(_hueBarTexture);
                var img = Raylib.GenImageColor(w, h, Raylib.BLACK);
                for (int y = 0; y < h; y++)
                {
                    var c = Raylib.ColorFromHSV((float)y / h * 360f, 1f, 1f);
                    for (int x = 0; x < w; x++)
                        Raylib.ImageDrawPixel(&img, x, y, c);
                }
                _hueBarTexture = Raylib.LoadTextureFromImage(img);
                Raylib.UnloadImage(img);
                _hueBarCachedWidth = w;
                _hueBarCachedHeight = h;
            }
            Raylib.DrawTexturePro(_hueBarTexture, new Rectangle(0, 0, w, h),
                new Rectangle(box.X, box.Y, box.Width, box.Height), new System.Numerics.Vector2(0, 0), 0, Raylib.WHITE);
        }
    }

    private void RenderTextInput(BoundingBox box, TextInputWidget widget)
    {
        var style = widget.CurrentStyle;
        var padding = style.Padding;

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

        PushScissor(box);

        float textX = box.X + padding.Left;
        float textY = box.Y + padding.Top - widget.ScrollY;
        float lineHeight = widget.ComputedLineHeight;

        int firstVisibleRow = Math.Max(0, (int)(widget.ScrollY / lineHeight) - 1);
        int lastVisibleRow = (int)((widget.ScrollY + box.Height) / lineHeight) + 1;

        if (widget.IsFocused && widget.HasSelection)
        {
            int selStart = Math.Min(widget.SelectionStart, widget.SelectionEnd);
            int selEnd = Math.Max(widget.SelectionStart, widget.SelectionEnd);
            var selColor = ToRayColor(style.SelectionColor);
            int pos = 0, row = 0;
            string text = widget.DisplayText;
            while (pos <= text.Length && pos < selEnd)
            {
                int lineStart = pos;
                int lineEnd = text.IndexOf('\n', pos);
                if (lineEnd < 0) lineEnd = text.Length;
                if (row > lastVisibleRow) break;
                if (row >= firstVisibleRow && lineEnd > selStart && lineStart < selEnd)
                {
                    int hlStart = Math.Max(lineStart, selStart);
                    int hlEnd = Math.Min(lineEnd, selEnd);
                    float x1 = textX + widget.MeasureSubstring(lineStart, hlStart);
                    float x2 = textX + widget.MeasureSubstring(lineStart, hlEnd);
                    float w = x2 - x1;
                    if (selEnd > lineEnd && w < 1f) w = 1f;
                    Raylib.DrawRectangleRec(new Rectangle(x1, textY + row * lineHeight, w, lineHeight), selColor);
                }
                pos = lineEnd + 1;
                row++;
            }
        }

        if (widget.Text.Length > 0 && style.FontId < _fonts.Length)
        {
            var font = _fonts[style.FontId];
            var textColor = ToRayColor(style.TextColor);
            string text = widget.DisplayText;
            int lineStart = 0, row = 0;
            while (row < firstVisibleRow && lineStart <= text.Length)
            {
                int lineEnd = text.IndexOf('\n', lineStart);
                if (lineEnd < 0) { lineStart = text.Length + 1; break; }
                lineStart = lineEnd + 1;
                row++;
            }
            while (lineStart <= text.Length && row <= lastVisibleRow)
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

        if (widget.IsFocused && (int)(Raylib.GetTime() * 2) % 2 == 0)
        {
            var (cursorRow, cursorCol) = widget.GetRowCol(widget.CursorIndex);
            int lineStart = widget.FindLineStart(widget.CursorIndex);
            float cursorX = textX + widget.MeasureSubstring(lineStart, widget.CursorIndex);
            float cursorY = textY + cursorRow * lineHeight;
            Raylib.DrawRectangleRec(new Rectangle(cursorX, cursorY, 1.5f, lineHeight), ToRayColor(style.CursorColor));
        }

        PopScissor();
    }

    private void RenderShadow(BoundingBox box, ShadowRenderData data)
    {
        float blur = data.BlurRadius;

        if (blur <= 0)
        {
            var rect = new Rectangle(box.X, box.Y, box.Width, box.Height);
            var color = ToRayColor(data.Color);

            if (data.CornerRadius.TopLeft > 0)
            {
                float minDim = Math.Min(box.Width, box.Height);
                float roundness = Math.Clamp((data.CornerRadius.TopLeft * 2) / minDim, 0, 1);
                Raylib.DrawRectangleRounded(rect, roundness, 8, color);
            }
            else
            {
                Raylib.DrawRectangleRec(rect, color);
            }
            return;
        }

        int layers = Math.Clamp((int)blur, 4, 24) + 1;
        float perLayerAlpha = data.Color.A / layers;
        byte r = (byte)Math.Clamp(data.Color.R, 0, 255);
        byte g = (byte)Math.Clamp(data.Color.G, 0, 255);
        byte b = (byte)Math.Clamp(data.Color.B, 0, 255);
        byte alpha = (byte)Math.Clamp(perLayerAlpha, 1, 255);

        for (int i = 0; i < layers; i++)
        {
            float inset = blur * ((float)i / (layers - 1));
            var rect = new Rectangle(
                box.X + inset,
                box.Y + inset,
                box.Width - inset * 2,
                box.Height - inset * 2
            );

            if (rect.width <= 0 || rect.height <= 0) continue;

            var layerColor = new RayColor { r = r, g = g, b = b, a = alpha };

            if (data.CornerRadius.TopLeft > 0)
            {
                float minDim = Math.Min(rect.width, rect.height);
                float roundness = Math.Clamp((data.CornerRadius.TopLeft * 2) / minDim, 0, 1);
                Raylib.DrawRectangleRounded(rect, roundness, 8, layerColor);
            }
            else
            {
                Raylib.DrawRectangleRec(rect, layerColor);
            }
        }
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
