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

                case RenderCommandType.Shadow:
                    RenderShadow(box, cmd.Shadow);
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

        var tint = data.BackgroundColor.A > 0 ? ToRayColor(data.BackgroundColor) : Raylib.WHITE;

        if (data.Slice.HasSlice)
        {
            RenderNineSlice(box, texture, data.Slice, tint);
        }
        else
        {
            // Stretch to fill the bounding box
            var source = new Rectangle(0, 0, texture.width, texture.height);
            var dest = new Rectangle(box.X, box.Y, box.Width, box.Height);
            Raylib.DrawTexturePro(texture, source, dest,
                new System.Numerics.Vector2(0, 0), 0, tint);
        }
    }

    private static void RenderNineSlice(BoundingBox box, Texture texture, NineSlice slice, RayColor tint)
    {
        float srcW = texture.width;
        float srcH = texture.height;
        float sl = slice.Left, sr = slice.Right, st = slice.Top, sb = slice.Bottom;

        // Destination insets — clamp so corners never exceed the dest size
        float dl = Math.Min(sl, box.Width * 0.5f);
        float dr = Math.Min(sr, box.Width * 0.5f);
        float dt = Math.Min(st, box.Height * 0.5f);
        float db = Math.Min(sb, box.Height * 0.5f);

        float midSrcW = srcW - sl - sr;
        float midSrcH = srcH - st - sb;
        float midDstW = box.Width - dl - dr;
        float midDstH = box.Height - dt - db;

        var origin = new System.Numerics.Vector2(0, 0);

        // Helper to draw one patch
        void Patch(float sx, float sy, float sw, float sh, float dx, float dy, float dw, float dh)
        {
            if (sw <= 0 || sh <= 0 || dw <= 0 || dh <= 0) return;
            Raylib.DrawTexturePro(texture,
                new Rectangle(sx, sy, sw, sh),
                new Rectangle(dx, dy, dw, dh),
                origin, 0, tint);
        }

        float x = box.X, y = box.Y;

        // Top row
        Patch(0, 0, sl, st, x, y, dl, dt);                               // top-left
        Patch(sl, 0, midSrcW, st, x + dl, y, midDstW, dt);               // top-center
        Patch(srcW - sr, 0, sr, st, x + dl + midDstW, y, dr, dt);        // top-right

        // Middle row
        Patch(0, st, sl, midSrcH, x, y + dt, dl, midDstH);              // mid-left
        Patch(sl, st, midSrcW, midSrcH, x + dl, y + dt, midDstW, midDstH); // center
        Patch(srcW - sr, st, sr, midSrcH, x + dl + midDstW, y + dt, dr, midDstH); // mid-right

        // Bottom row
        Patch(0, srcH - sb, sl, sb, x, y + dt + midDstH, dl, db);       // bot-left
        Patch(sl, srcH - sb, midSrcW, sb, x + dl, y + dt + midDstH, midDstW, db); // bot-center
        Patch(srcW - sr, srcH - sb, sr, sb, x + dl + midDstW, y + dt + midDstH, dr, db); // bot-right
    }

    // Cached textures for HSV gradients
    private Texture _svTexture;
    private float _svCachedHue = -1;
    private int _svCachedWidth;
    private int _svCachedHeight;
    private Texture _hueBarTexture;
    private int _hueBarCachedWidth;
    private int _hueBarCachedHeight;

    private void RenderCustom(BoundingBox box, CustomRenderData data)
    {
        if (data.CustomData is HsvGradientData gradient)
            RenderHsvGradient(box, gradient);
    }

    private unsafe void RenderHsvGradient(BoundingBox box, HsvGradientData gradient)
    {
        int w = Math.Max(1, (int)box.Width);
        int h = Math.Max(1, (int)box.Height);

        if (gradient.Type == HsvGradientType.SaturationValue)
        {
            // Regenerate texture when hue or size changes
            if (Math.Abs(gradient.Hue - _svCachedHue) > 0.01f || w != _svCachedWidth || h != _svCachedHeight)
            {
                if (_svCachedWidth > 0)
                    Raylib.UnloadTexture(_svTexture);

                var img = Raylib.GenImageColor(w, h, Raylib.BLACK);
                for (int y = 0; y < h; y++)
                {
                    float value = 1f - (float)y / h;
                    for (int x = 0; x < w; x++)
                    {
                        float sat = (float)x / w;
                        var c = Raylib.ColorFromHSV(gradient.Hue, sat, value);
                        Raylib.ImageDrawPixel(&img, x, y, c);
                    }
                }
                _svTexture = Raylib.LoadTextureFromImage(img);
                Raylib.UnloadImage(img);
                _svCachedHue = gradient.Hue;
                _svCachedWidth = w;
                _svCachedHeight = h;
            }

            Raylib.DrawTexturePro(
                _svTexture,
                new Rectangle(0, 0, w, h),
                new Rectangle(box.X, box.Y, box.Width, box.Height),
                new System.Numerics.Vector2(0, 0), 0, Raylib.WHITE);
        }
        else // HueBar
        {
            // Regenerate texture only when size changes (hue bar is static)
            if (w != _hueBarCachedWidth || h != _hueBarCachedHeight)
            {
                if (_hueBarCachedWidth > 0)
                    Raylib.UnloadTexture(_hueBarTexture);

                var img = Raylib.GenImageColor(w, h, Raylib.BLACK);
                for (int y = 0; y < h; y++)
                {
                    float hue = (float)y / h * 360f;
                    var c = Raylib.ColorFromHSV(hue, 1f, 1f);
                    for (int x = 0; x < w; x++)
                        Raylib.ImageDrawPixel(&img, x, y, c);
                }
                _hueBarTexture = Raylib.LoadTextureFromImage(img);
                Raylib.UnloadImage(img);
                _hueBarCachedWidth = w;
                _hueBarCachedHeight = h;
            }

            Raylib.DrawTexturePro(
                _hueBarTexture,
                new Rectangle(0, 0, w, h),
                new Rectangle(box.X, box.Y, box.Width, box.Height),
                new System.Numerics.Vector2(0, 0), 0, Raylib.WHITE);
        }
    }

    private void RenderShadow(BoundingBox box, ShadowRenderData data)
    {
        // The bounding box arrives pre-expanded (offset + blur + spread already applied).
        // We draw concentric rounded rectangles from outermost (full box) to innermost,
        // each layer covering a ring of the blur. Alpha per layer = base_alpha / layers,
        // so they accumulate to full opacity at the center where all layers overlap.
        float blur = data.BlurRadius;

        if (blur <= 0)
        {
            // Sharp shadow — single rectangle
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

        // +1 so the last layer sits exactly at inset=blur (the original element edge)
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
