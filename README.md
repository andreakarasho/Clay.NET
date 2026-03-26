# Clay.NET

A pure .NET reimplementation of the [Clay](https://github.com/nicbarker/clay) UI layout library. Clay.NET provides a declarative, immediate-mode UI framework with a flexbox-like layout system, zero per-frame allocations after warmup, and no native dependencies.

## Features

- **Immediate-mode API** - Describe your UI declaratively each frame using `using` scopes
- **Flexbox-like layout** - Sizing (Fixed, Fit, Grow, Percent), padding, alignment, child gaps, and directional flow
- **Backend-agnostic rendering** - Generates render commands for any graphics backend (Raylib, MonoGame, Unity, etc.)
- **Pointer input handling** - Hover detection and pointer state tracking with O(1) element lookup
- **Scroll containers** - Native support for scrollable content with automatic clipping
- **Floating/overlay elements** - Z-indexed absolute positioning for tooltips, dropdowns, and modals
- **Zero dependencies** - Pure managed .NET code, no interop required
- **High performance** - Aggressive inlining, span-based access, pre-allocated buffers, zero per-frame GC pressure after warmup

## Project Structure

```
Clay.NET/
├── Clay.slnx
└── src/
    ├── Clay/               # Core layout library
    ├── Clay.Example/       # Raylib-based example application
    └── Clay.Test/          # xUnit test suite
```

## Quick Start

```csharp
using Clay;
using System.Numerics;

// Initialize once
Clay.Clay.Initialize(new Dimensions(1920, 1080), new SimpleTextMeasurer());

// Each frame
Clay.Clay.SetPointerState(new Vector2(mouseX, mouseY), mousePressed);
Clay.Clay.BeginLayout();

using (Clay.Clay.Element(new ElementDeclaration
{
    Id = Clay.Clay.Id("Root"),
    Layout = new LayoutConfig
    {
        Sizing = Sizing.Fill(),
        Direction = LayoutDirection.TopToBottom,
        Padding = Padding.All(16),
        ChildGap = 8
    },
    BackgroundColor = Color.Rgba(30, 30, 30)
}))
{
    Clay.Clay.Text("Hello, Clay!", new TextConfig
    {
        FontSize = 24,
        TextColor = Color.White
    });

    using (Clay.Clay.Element(new ElementDeclaration
    {
        Id = Clay.Clay.Id("Button"),
        Layout = new LayoutConfig { Padding = Padding.Symmetric(16, 8) },
        BackgroundColor = Clay.Clay.PointerOver(Clay.Clay.Id("Button"))
            ? Color.Rgba(80, 80, 80)
            : Color.Rgba(60, 60, 60),
        CornerRadius = CornerRadius.All(4)
    }))
    {
        Clay.Clay.Text("Click Me", new TextConfig
        {
            FontSize = 16,
            TextColor = Color.White
        });
    }
}

ReadOnlySpan<RenderCommand> commands = Clay.Clay.EndLayout();
// Pass commands to your renderer
```

## Sizing Types

| Type | Behavior |
|------|----------|
| `SizingAxis.Fixed(size)` | Exact pixel size |
| `SizingAxis.Fit(min, max)` | Shrink to content, respecting bounds |
| `SizingAxis.Grow(min, max)` | Expand to fill available space |
| `SizingAxis.PercentOf(pct)` | Percentage of parent size (0.0 - 1.0) |

## Rendering

Implement `IClayRenderer` for your graphics backend:

```csharp
public class MyRenderer : IClayRenderer
{
    public void Render(ReadOnlySpan<RenderCommand> commands)
    {
        foreach (var cmd in commands)
        {
            switch (cmd.CommandType)
            {
                case RenderCommandType.Rectangle:
                    DrawRect(cmd.BoundingBox, cmd.Rectangle.BackgroundColor);
                    break;
                case RenderCommandType.Text:
                    DrawText(cmd.Text.Text, cmd.BoundingBox, cmd.Text.TextColor);
                    break;
                // ... handle other command types
            }
        }
    }
}
```

## Building

Requires .NET 9.0 SDK.

```bash
dotnet build Clay.slnx
dotnet test src/Clay.Test/Clay.Test.csproj
```

## Credits

Based on the original [Clay](https://github.com/nicbarker/clay) C library by Nic Barker.

This project was developed with the assistance of AI (Claude by Anthropic).
