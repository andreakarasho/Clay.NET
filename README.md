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

## Examples

### Row Layout with Gap

```csharp
using (Clay.Clay.Element(new ElementDeclaration
{
    Id = Clay.Clay.Id("Toolbar"),
    Layout = new LayoutConfig
    {
        Sizing = Sizing.FillWidth(),
        Direction = LayoutDirection.LeftToRight,
        ChildGap = 8,
        Padding = Padding.All(12)
    },
    BackgroundColor = Color.Rgba(40, 40, 40)
}))
{
    // Children are placed side by side with 8px gap
    Button("Save");
    Button("Load");
    Button("Settings");
}
```

### Sidebar + Content Split

```csharp
using (Clay.Clay.Element(new ElementDeclaration
{
    Id = Clay.Clay.Id("App"),
    Layout = LayoutConfig.FillRow()
}))
{
    // Fixed-width sidebar
    using (Clay.Clay.Element(new ElementDeclaration
    {
        Id = Clay.Clay.Id("Sidebar"),
        Layout = new LayoutConfig
        {
            Sizing = new Sizing(SizingAxis.Fixed(250), SizingAxis.Grow()),
            Direction = LayoutDirection.TopToBottom,
            Padding = Padding.All(16),
            ChildGap = 4
        },
        BackgroundColor = Color.Rgba(25, 25, 25)
    }))
    {
        Clay.Clay.Text("Navigation", new TextConfig { FontSize = 18, TextColor = Color.White });
    }

    // Main content grows to fill remaining space
    using (Clay.Clay.Element(new ElementDeclaration
    {
        Id = Clay.Clay.Id("Content"),
        Layout = new LayoutConfig
        {
            Sizing = Sizing.Fill(),
            Padding = Padding.All(24)
        }
    }))
    {
        Clay.Clay.Text("Main content area", new TextConfig { FontSize = 16, TextColor = Color.White });
    }
}
```

### Scroll Container

```csharp
using (Clay.Clay.Element(new ElementDeclaration
{
    Id = Clay.Clay.Id("ScrollArea"),
    Layout = new LayoutConfig
    {
        Sizing = Sizing.FixedSize(300, 400),
        Direction = LayoutDirection.TopToBottom,
        ChildGap = 4
    },
    Scroll = ScrollConfig.VerticalScroll,
    BackgroundColor = Color.Rgba(20, 20, 20)
}))
{
    for (int i = 0; i < 50; i++)
    {
        using (Clay.Clay.Element(new ElementDeclaration
        {
            Id = Clay.Clay.Id("Item", (uint)i),
            Layout = new LayoutConfig
            {
                Sizing = new Sizing(SizingAxis.Grow(), SizingAxis.Fixed(40)),
                Padding = Padding.Symmetric(12, 8)
            },
            BackgroundColor = Color.Rgba(50, 50, 50)
        }))
        {
            Clay.Clay.Text($"Item {i}", new TextConfig { FontSize = 14, TextColor = Color.White });
        }
    }
}
```

### Floating Tooltip

```csharp
var buttonId = Clay.Clay.Id("HoverButton");

using (Clay.Clay.Element(new ElementDeclaration
{
    Id = buttonId,
    Layout = new LayoutConfig { Padding = Padding.Symmetric(16, 8) },
    BackgroundColor = Color.Rgba(60, 120, 200),
    CornerRadius = CornerRadius.All(4)
}))
{
    Clay.Clay.Text("Hover me", new TextConfig { FontSize = 14, TextColor = Color.White });

    if (Clay.Clay.PointerOver(buttonId))
    {
        using (Clay.Clay.Element(new ElementDeclaration
        {
            Id = Clay.Clay.Id("Tooltip"),
            Layout = new LayoutConfig { Padding = Padding.All(8) },
            BackgroundColor = Color.Rgba(0, 0, 0, 220),
            CornerRadius = CornerRadius.All(4),
            Floating = new FloatingConfig
            {
                Offset = new Vector2(0, 4),
                AttachTo = FloatingAttachTo.Parent,
                AttachPoints = new FloatingAttachPoints
                {
                    Element = FloatingAttachPoint.LeftTop,
                    Parent = FloatingAttachPoint.LeftBottom
                },
                ZIndex = 10
            }
        }))
        {
            Clay.Clay.Text("This is a tooltip", new TextConfig
            {
                FontSize = 12,
                TextColor = Color.White
            });
        }
    }
}
```

### Centering Content

```csharp
using (Clay.Clay.Element(new ElementDeclaration
{
    Id = Clay.Clay.Id("CenterWrapper"),
    Layout = new LayoutConfig
    {
        Sizing = Sizing.Fill(),
        ChildAlignment = ChildAlignment.Center
    }
}))
{
    using (Clay.Clay.Element(new ElementDeclaration
    {
        Id = Clay.Clay.Id("Card"),
        Layout = new LayoutConfig
        {
            Sizing = Sizing.FixedSize(400, 300),
            Direction = LayoutDirection.TopToBottom,
            Padding = Padding.All(24),
            ChildGap = 12,
            ChildAlignment = ChildAlignment.Center
        },
        BackgroundColor = Color.Rgba(45, 45, 45),
        CornerRadius = CornerRadius.All(8),
        Border = BorderConfig.Uniform(1, Color.Rgba(80, 80, 80))
    }))
    {
        Clay.Clay.Text("Centered Card", new TextConfig { FontSize = 20, TextColor = Color.White });
        Clay.Clay.Text("This card is centered in the viewport.", new TextConfig
        {
            FontSize = 14,
            TextColor = Color.Rgba(180, 180, 180)
        });
    }
}
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
