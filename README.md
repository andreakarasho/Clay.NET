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

## ClayUI - Immediate Mode Widgets

Clay.NET includes `ClayUI`, a higher-level immediate-mode widget layer built on top of the core layout engine. It provides ready-to-use UI controls with built-in state management, hover/click handling, and theming.

### Setup

Call `ClayUI.BeginFrame()` each frame before using widgets, between `Clay.BeginLayout()` and `Clay.EndLayout()`:

```csharp
Clay.Clay.BeginLayout();
ClayUI.BeginFrame(mouseDown, new Vector2(mouseX, mouseY), scrollDelta);

// ... use ClayUI widgets here ...

var commands = Clay.Clay.EndLayout();
```

### Buttons and Labels

```csharp
ClayUI.Heading("Settings");
ClayUI.Label("Configure your preferences below.");

if (ClayUI.Button("Save"))
{
    SaveSettings();
}

if (ClayUI.Button("Cancel"))
{
    RevertChanges();
}
```

### Image and ImageButton

```csharp
// Display an image (pass your texture/image object through to the renderer)
ClayUI.Image(myTexture, width: 128, height: 128);

// Rounded image
ClayUI.Image(avatar, 64, 64, style: new ImageStyle
{
    CornerRadius = CornerRadius.All(32) // circular
});

// Clickable image button (like ImGui::ImageButton)
if (ClayUI.ImageButton(icon, 32, 32))
{
    DoAction();
}
```

### Checkbox and Toggle

```csharp
bool darkMode = true;
bool notifications = false;

ClayUI.Checkbox("Enable dark mode", ref darkMode);
ClayUI.Toggle("Notifications", ref notifications);
```

### Slider

```csharp
float volume = 0.75f;
float brightness = 1.0f;

ClayUI.Slider("Volume", ref volume, 0f, 1f);
ClayUI.Slider("Brightness", ref brightness, 0f, 2f);
```

### Radio Group

```csharp
string quality = "Medium";
ClayUI.RadioGroup("Quality", ref quality, new[] { "Low", "Medium", "High" });
```

### Progress Bar

```csharp
ClayUI.ProgressBar(downloadProgress, 0f, 100f);
```

### Panels

Panels are titled, styled containers for grouping related widgets:

```csharp
ClayUI.BeginPanel("Player Info", scroll: true, maxHeight: 300);
    ClayUI.Label($"Name: {player.Name}");
    ClayUI.Label($"Health: {player.Health}");
    ClayUI.Label($"Score: {player.Score}");
    ClayUI.Separator();
    ClayUI.Slider("Speed", ref player.Speed, 0f, 10f);
ClayUI.EndPanel();
```

### Horizontal and Vertical Layouts

```csharp
ClayUI.BeginHorizontal(gap: 8);
    ClayUI.Button("Left");
    ClayUI.Button("Center");
    ClayUI.Button("Right");
ClayUI.EndHorizontal();

ClayUI.BeginVertical(gap: 4);
    ClayUI.Label("Line 1");
    ClayUI.Label("Line 2");
    ClayUI.Label("Line 3");
ClayUI.EndVertical();
```

### Windows

Draggable, resizable, collapsible windows with automatic focus management:

```csharp
bool showInventory = true;

if (ClayUI.BeginWindow("Inventory", ref showInventory,
    defaultPosition: new Vector2(400, 150),
    defaultSize: new Vector2(300, 200)))
{
    for (int i = 0; i < items.Count; i++)
    {
        ClayUI.BeginHorizontal();
            ClayUI.Label(items[i].Name);
            if (ClayUI.Button("Use"))
                UseItem(items[i]);
        ClayUI.EndHorizontal();
    }
}
ClayUI.EndWindow();
```

### Popups and Context Menus

```csharp
var triggerId = Clay.Clay.Id("RightClickArea");

if (ClayUI.BeginContextMenu("MyMenu", triggerId))
{
    if (ClayUI.MenuItem("Cut"))   DoCut();
    if (ClayUI.MenuItem("Copy"))  DoCopy();
    if (ClayUI.MenuItem("Paste")) DoPaste();
    ClayUI.MenuSeparator();
    if (ClayUI.MenuItem("Delete")) DoDelete();
    ClayUI.EndContextMenu();
}
```

### Tree Nodes

```csharp
if (ClayUI.BeginTreeNode("Root"))
{
    if (ClayUI.BeginTreeNode("Child A"))
    {
        ClayUI.Label("Leaf 1");
        ClayUI.Label("Leaf 2");
        ClayUI.EndTreeNode();
    }
    if (ClayUI.BeginTreeNode("Child B"))
    {
        ClayUI.Label("Leaf 3");
        ClayUI.EndTreeNode();
    }
    ClayUI.EndTreeNode();
}
```

### Theming

```csharp
// Built-in themes
ClayUI.Style = ClayUIStyle.Dark;
ClayUI.Style = ClayUIStyle.Light;

// Or customize individual widget styles
ClayUI.Button("Danger", style: new ButtonStyle
{
    BackgroundColor = Color.Rgba(180, 40, 40),
    HoverColor = Color.Rgba(200, 60, 60),
    PressedColor = Color.Rgba(140, 30, 30),
    CornerRadius = CornerRadius.All(8)
});
```

### Debug Window

```csharp
// Toggle with a key press
if (keyPressed == Key.F12)
    ClayUI.ToggleDebugWindow();

// Or show directly
ClayUI.ShowDebugWindow();
```

## Low-Level Layout Examples

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
