using Clay;
using Clay.Example;
using ZeroElectric.Vinculum;
using Color = Clay.Color;
using Vector2 = System.Numerics.Vector2;

// Window configuration
const int InitialWidth = 1280;
const int InitialHeight = 720;
const string WindowTitle = "Clay .NET - Raylib Example";

// Initialize Raylib
Raylib.SetConfigFlags(
    ConfigFlags.FLAG_WINDOW_RESIZABLE |
    ConfigFlags.FLAG_WINDOW_HIGHDPI |
    ConfigFlags.FLAG_MSAA_4X_HINT |
    ConfigFlags.FLAG_VSYNC_HINT
);
Raylib.InitWindow(InitialWidth, InitialHeight, WindowTitle);

// Load fonts
var fonts = new Font[2];
fonts[0] = Raylib.LoadFont("resources/Roboto-Regular.ttf");
Raylib.SetTextureFilter(fonts[0].texture, TextureFilter.TEXTURE_FILTER_BILINEAR);

// If font loading fails, use default font
unsafe
{
    if (fonts[0].glyphs == null)
    {
        fonts[0] = Raylib.GetFontDefault();
    }
}

// Create text measurer and renderer
var textMeasurer = new RaylibTextMeasurer(fonts);
var renderer = new RaylibRenderer(fonts);

// Initialize Clay
Clay.Clay.Initialize(
    new Dimensions(Raylib.GetScreenWidth(), Raylib.GetScreenHeight()),
    textMeasurer,
    maxElementCount: 8192
);

// Set up clipboard for text edit Ctrl+C/X/V
Clay.Clay.TextEditSetClipboard(new RaylibClipboard());

// Application state
var documents = new[]
{
    new Document("Getting Started", "Welcome to Clay .NET!\n\nThis is a pure .NET implementation of the Clay UI layout library. It provides a declarative, immediate-mode API for creating flexible user interfaces.\n\nKey features:\n- Flexbox-like layout system\n- Zero native dependencies\n- Idiomatic C# API with 'using' pattern\n- Abstract rendering backend"),
    new Document("Layout System", "Clay uses a flexbox-inspired layout model:\n\n- Fixed: Exact pixel size\n- Fit: Shrink to content\n- Grow: Expand to fill space\n- Percent: Percentage of parent\n\nElements flow in rows (LeftToRight) or columns (TopToBottom) with configurable gaps and alignment."),
    new Document("Rendering", "Clay generates render commands that are backend-agnostic. This example uses Raylib, but you can implement IClayRenderer for any graphics system:\n\n- MonoGame/FNA\n- Unity\n- SDL2\n- OpenGL/Vulkan\n- And more!"),
    new Document("Input Handling", "Clay tracks pointer state for hover detection:\n\n1. Call SetPointerState() each frame\n2. Use PointerOver(id) to check hover\n3. Style elements based on hover state\n\nScroll containers are also supported for scrollable content areas."),
    new Document("Performance", "This implementation is optimized for real-time UI:\n\n- Pre-allocated collections\n- Span<T> for zero-copy access\n- Hash map for O(1) element lookup\n- Minimal per-frame allocations after warmup"),
    new Document("Widget Demo", "WIDGET_DEMO")  // Special marker for widget demo page
};
int selectedDocument = 0;

// Menu state
string? openMenu = null;
bool menuActionThisFrame = false;

// Menu definitions
var menuItems = new Dictionary<string, string[]>
{
    ["File"] = ["New", "Open", "Save", "Save As...", "---", "Exit"],
    ["Edit"] = ["Undo", "Redo", "---", "Cut", "Copy", "Paste"],
    ["View"] = ["Zoom In", "Zoom Out", "---", "Full Screen"],
    ["Help"] = ["Documentation", "About"]
};

// ClayUI Demo state
bool checkboxValue = true;
bool toggleValue = false;
float sliderValue = 0.5f;
int clickCount = 0;
float progressValue = 0.3f;
int selectedOption = 0;
string[] radioOptions = ["Option A", "Option B", "Option C"];

// Window demo state
bool demoWindow1Open = true;
bool demoWindow2Open = true;
bool windowCheckbox = false;
float windowSlider = 0.5f;

// Debug window state
bool debugWindowOpen = true;

// TextEdit demo state
string textEditSingleLine = "Hello, Clay!";
string textEditMultiLine = "Line 1\nLine 2\nLine 3";
string textEditEmpty = "";

// Main loop
while (!Raylib.WindowShouldClose())
{
    // Reset per-frame state
    menuActionThisFrame = false;

    // Update Clay state
    var mousePos = Raylib.GetMousePosition();
    var mouseWheel = Raylib.GetMouseWheelMoveV();
    bool mousePressed = Raylib.IsMouseButtonPressed(0);
    bool mouseDown = Raylib.IsMouseButtonDown(0);
    var scrollDelta = new Vector2(mouseWheel.X, mouseWheel.Y);

    Clay.Clay.SetPointerState(new Vector2(mousePos.X, mousePos.Y), mouseDown);
    Clay.Clay.SetLayoutDimensions(new Dimensions(Raylib.GetScreenWidth(), Raylib.GetScreenHeight()));

    // Initialize ClayUI for this frame (pass scroll delta for window scrolling)
    ClayUI.BeginFrame(mouseDown, new Vector2(mousePos.X, mousePos.Y), scrollDelta);

    // Only update non-window scroll containers if mouse is not over a window
    if (!ClayUI.IsMouseOverAnyWindow)
    {
        Clay.Clay.UpdateScrollContainers(false, scrollDelta, Raylib.GetFrameTime());
    }

    // Toggle debug window with F12
    if (Raylib.IsKeyPressed(KeyboardKey.KEY_F12))
    {
        debugWindowOpen = !debugWindowOpen;
    }

    // Forward keyboard input to text edit widgets
    ForwardKeyboardInput();

    // Begin layout
    Clay.Clay.BeginLayout();

    // Root container
    using (Clay.Clay.Element(new ElementDeclaration
    {
        Id = Clay.Clay.Id("Root"),
        Layout = new LayoutConfig
        {
            Sizing = Sizing.Fill(),
            Direction = LayoutDirection.TopToBottom,
            Padding = Padding.All(16),
            ChildGap = 16
        },
        BackgroundColor = Color.Rgba(30, 30, 35)
    }))
    {
        // Header with menus
        RenderHeader(menuItems, ref openMenu, ref menuActionThisFrame);

        // Main content area
        using (Clay.Clay.Element(new ElementDeclaration
        {
            Id = Clay.Clay.Id("MainArea"),
            Layout = new LayoutConfig
            {
                Sizing = Sizing.Fill(),
                Direction = LayoutDirection.LeftToRight,
                ChildGap = 16
            }
        }))
        {
            // Sidebar
            RenderSidebar(documents, selectedDocument, out selectedDocument);

            // Content
            RenderContent(documents[selectedDocument]);
        }

        // Footer
        RenderFooter();

        // Render demo windows at root level (only when on Widget Demo page)
        if (documents[selectedDocument].Content == "WIDGET_DEMO")
        {
            RenderDemoWindows();
        }

        // Debug window (always available, toggle with F12)
        if (debugWindowOpen)
            ClayUI.ShowDebugWindow();
    }

    // End layout and get render commands
    var commands = Clay.Clay.EndLayout();

    // Close menu if clicked outside (and no menu action happened this frame)
    if (mousePressed && !menuActionThisFrame && openMenu != null)
    {
        openMenu = null;
    }

    // Render
    Raylib.BeginDrawing();
    Raylib.ClearBackground(Raylib.BLACK);
    renderer.Render(commands);
    Raylib.EndDrawing();
}

// Cleanup
Clay.Clay.Shutdown();
Raylib.CloseWindow();

// ============ UI Components ============

void RenderHeader(Dictionary<string, string[]> menuItems, ref string? openMenu, ref bool menuActionThisFrame)
{
    using (Clay.Clay.Element(new ElementDeclaration
    {
        Id = Clay.Clay.Id("Header"),
        Layout = new LayoutConfig
        {
            Sizing = new Sizing
            {
                Width = SizingAxis.Grow(),
                Height = SizingAxis.Fixed(60)
            },
            Direction = LayoutDirection.LeftToRight,
            Padding = Padding.Horizontal(16),
            ChildGap = 16,
            ChildAlignment = ChildAlignment.CenterLeft
        },
        BackgroundColor = Color.Rgba(45, 45, 50),
        CornerRadius = CornerRadius.All(8)
    }))
    {
        // Logo/Title
        Clay.Clay.Text("Clay .NET", new TextConfig
        {
            FontId = 0,
            FontSize = 24,
            TextColor = Color.Rgba(100, 180, 255)
        });

        // Spacer
        using (Clay.Clay.Element(new ElementDeclaration
        {
            Layout = new LayoutConfig
            {
                Sizing = new Sizing { Width = SizingAxis.Grow() }
            }
        })) { }

        // Menu buttons
        foreach (var menu in menuItems.Keys)
        {
            RenderMenuButton(menu, menuItems[menu], ref openMenu, ref menuActionThisFrame);
        }
    }
}

void RenderMenuButton(string menuName, string[] items, ref string? openMenu, ref bool menuActionThisFrame)
{
    var buttonId = Clay.Clay.Id($"MenuBtn_{menuName}");
    bool isHovered = Clay.Clay.PointerOver(buttonId);
    bool isOpen = openMenu == menuName;

    // Handle click on menu button (blocked by windows)
    if (isHovered && Raylib.IsMouseButtonPressed(0) && !ClayUI.IsMouseOverAnyWindow)
    {
        openMenu = isOpen ? null : menuName;
        menuActionThisFrame = true;
    }

    // Menu button
    using (Clay.Clay.Element(new ElementDeclaration
    {
        Id = buttonId,
        Layout = new LayoutConfig
        {
            Padding = Padding.Symmetric(16, 8)
        },
        BackgroundColor = (isHovered || isOpen)
            ? Color.Rgba(70, 70, 80)
            : Color.Rgba(55, 55, 65),
        CornerRadius = CornerRadius.All(4)
    }))
    {
        Clay.Clay.Text(menuName, new TextConfig
        {
            FontId = 0,
            FontSize = 14,
            TextColor = Color.White
        });

        // Render dropdown if open
        if (isOpen)
        {
            RenderMenuDropdown(menuName, items, ref openMenu, ref menuActionThisFrame);
        }
    }
}

void RenderMenuDropdown(string menuName, string[] items, ref string? openMenu, ref bool menuActionThisFrame)
{
    using (Clay.Clay.Element(new ElementDeclaration
    {
        Id = Clay.Clay.Id($"MenuDropdown_{menuName}"),
        Layout = new LayoutConfig
        {
            Sizing = new Sizing
            {
                Width = SizingAxis.Fit(150, 250)
            },
            Direction = LayoutDirection.TopToBottom,
            Padding = Padding.All(4)
        },
        Floating = new FloatingConfig
        {
            AttachTo = FloatingAttachTo.Parent,
            AttachPoints = new FloatingAttachPoints
            {
                Parent = FloatingAttachPoint.LeftBottom,
                Element = FloatingAttachPoint.LeftTop
            },
            Offset = new Vector2(0, 4),
            ZIndex = 100
        },
        BackgroundColor = Color.Rgba(50, 50, 55),
        CornerRadius = CornerRadius.All(6),
        Border = new BorderConfig
        {
            Width = BorderWidth.All(1),
            Color = Color.Rgba(70, 70, 75)
        }
    }))
    {
        for (int i = 0; i < items.Length; i++)
        {
            var item = items[i];

            if (item == "---")
            {
                // Separator
                using (Clay.Clay.Element(new ElementDeclaration
                {
                    Layout = new LayoutConfig
                    {
                        Sizing = new Sizing
                        {
                            Width = SizingAxis.Grow(),
                            Height = SizingAxis.Fixed(1)
                        },
                        Padding = new Padding { Left = 8, Right = 8, Top = 4, Bottom = 4 }
                    }
                }))
                {
                    using (Clay.Clay.Element(new ElementDeclaration
                    {
                        Layout = new LayoutConfig
                        {
                            Sizing = new Sizing
                            {
                                Width = SizingAxis.Grow(),
                                Height = SizingAxis.Fixed(1)
                            }
                        },
                        BackgroundColor = Color.Rgba(70, 70, 75)
                    })) { }
                }
            }
            else
            {
                // Menu item
                var itemId = Clay.Clay.Id($"MenuItem_{menuName}_{i}");
                bool itemHovered = Clay.Clay.PointerOver(itemId);

                // Handle click on menu item (blocked by windows)
                if (itemHovered && Raylib.IsMouseButtonPressed(0) && !ClayUI.IsMouseOverAnyWindow)
                {
                    Console.WriteLine($"Menu action: {menuName} > {item}");
                    openMenu = null;
                    menuActionThisFrame = true;

                    // Handle Exit specially
                    if (item == "Exit")
                    {
                        Environment.Exit(0);
                    }
                }

                using (Clay.Clay.Element(new ElementDeclaration
                {
                    Id = itemId,
                    Layout = new LayoutConfig
                    {
                        Sizing = new Sizing { Width = SizingAxis.Grow() },
                        Padding = Padding.Symmetric(12, 8)
                    },
                    BackgroundColor = itemHovered
                        ? Color.Rgba(70, 130, 200)
                        : Color.Rgba(0, 0, 0, 0),
                    CornerRadius = CornerRadius.All(4)
                }))
                {
                    Clay.Clay.Text(item, new TextConfig
                    {
                        FontId = 0,
                        FontSize = 13,
                        TextColor = Color.White
                    });
                }
            }
        }
    }
}

void RenderSidebar(Document[] docs, int currentSelected, out int newSelected)
{
    newSelected = currentSelected;

    using (Clay.Clay.Element(new ElementDeclaration
    {
        Id = Clay.Clay.Id("Sidebar"),
        Layout = new LayoutConfig
        {
            Sizing = new Sizing
            {
                Width = SizingAxis.Fixed(250),
                Height = SizingAxis.Grow()
            },
            Direction = LayoutDirection.TopToBottom,
            Padding = Padding.All(12),
            ChildGap = 8
        },
        BackgroundColor = Color.Rgba(40, 40, 45),
        CornerRadius = CornerRadius.All(8)
    }))
    {
        // Section header
        Clay.Clay.Text("Documents", new TextConfig
        {
            FontId = 0,
            FontSize = 12,
            TextColor = Color.Rgba(150, 150, 160)
        });

        // Spacing
        using (Clay.Clay.Element(new ElementDeclaration
        {
            Layout = new LayoutConfig
            {
                Sizing = new Sizing { Height = SizingAxis.Fixed(8) }
            }
        })) { }

        // Document list
        for (int i = 0; i < docs.Length; i++)
        {
            var id = Clay.Clay.Id("SidebarItem", (uint)i);
            bool isHovered = Clay.Clay.PointerOver(id);
            bool isSelected = currentSelected == i;

            // Handle click (blocked by windows)
            if (isHovered && Raylib.IsMouseButtonPressed(0) && !ClayUI.IsMouseOverAnyWindow)
            {
                newSelected = i;
                // Reset scroll position when switching documents
                if (currentSelected != i)
                {
                    Clay.Clay.ResetScrollPosition(Clay.Clay.Id("Content"));
                }
            }

            using (Clay.Clay.Element(new ElementDeclaration
            {
                Id = id,
                Layout = new LayoutConfig
                {
                    Sizing = new Sizing { Width = SizingAxis.Grow() },
                    Padding = Padding.All(10)
                },
                BackgroundColor = isSelected
                    ? Color.Rgba(70, 130, 200)
                    : isHovered
                        ? Color.Rgba(55, 55, 65)
                        : Color.Rgba(0, 0, 0, 0),
                CornerRadius = CornerRadius.All(6)
            }))
            {
                Clay.Clay.Text(docs[i].Title, new TextConfig
                {
                    FontId = 0,
                    FontSize = 15,
                    TextColor = isSelected
                        ? Color.White
                        : Color.Rgba(220, 220, 225)
                });
            }
        }
    }
}

void RenderContent(Document doc)
{
    var contentId = Clay.Clay.Id("Content");

    // Wrapper container (horizontal: scroll content + scrollbar)
    using (Clay.Clay.Element(new ElementDeclaration
    {
        Layout = new LayoutConfig
        {
            Sizing = Sizing.Fill(),
            Direction = LayoutDirection.LeftToRight
        },
        BackgroundColor = Color.Rgba(40, 40, 45),
        CornerRadius = CornerRadius.All(8)
    }))
    {
        // Scroll container
        using (Clay.Clay.Element(new ElementDeclaration
        {
            Id = contentId,
            Layout = new LayoutConfig
            {
                Sizing = Sizing.Fill(),
                Direction = LayoutDirection.TopToBottom,
                Padding = Padding.All(20),
                ChildGap = 16
            },
            Scroll = new ScrollConfig { Vertical = true }
        }))
        {
            // Document title
            Clay.Clay.Text(doc.Title, new TextConfig
            {
                FontId = 0,
                FontSize = 28,
                TextColor = Color.White
            });

            // Divider
            using (Clay.Clay.Element(new ElementDeclaration
            {
                Layout = new LayoutConfig
                {
                    Sizing = new Sizing
                    {
                        Width = SizingAxis.Grow(),
                        Height = SizingAxis.Fixed(1)
                    }
                },
                BackgroundColor = Color.Rgba(60, 60, 70)
            })) { }

            // Check if this is the Widget Demo page
            if (doc.Content == "WIDGET_DEMO")
            {
                RenderWidgetDemo();
            }
            else
            {
                // Regular document content
                Clay.Clay.Text(doc.Content, new TextConfig
                {
                    FontId = 0,
                    FontSize = 16,
                    TextColor = Color.Rgba(200, 200, 210),
                    LineHeight = 24,
                    WrapMode = TextWrapMode.Words
                });
            }
        }

        // Vertical scrollbar (inline, sibling to scroll container)
        ClayUI.VerticalScrollbar(contentId);
    }
}

void RenderWidgetDemo()
{
    ClayUI.Label("Welcome to the ClayUI Widget Demo! This page showcases all available widgets in the ImGui-style API.");
    ClayUI.Space(8);
    ClayUI.Label("Each widget returns a boolean indicating if it was interacted with, making it easy to handle user input.");
    ClayUI.Space(20);

    // ========== BUTTONS ==========
    ClayUI.BeginPanel("Buttons");
    ClayUI.Label("Buttons return true when clicked:");
    ClayUI.Space(8);

    ClayUI.BeginHorizontal(gap: 12);
    if (ClayUI.Button("Click Me"))
    {
        clickCount++;
    }

    if (ClayUI.Button("Reset All"))
    {
        clickCount = 0;
        progressValue = 0;
        sliderValue = 0.5f;
        checkboxValue = true;
        toggleValue = false;
    }

    if (ClayUI.Button("+ Progress"))
    {
        progressValue = Math.Min(1.0f, progressValue + 0.1f);
    }

    if (ClayUI.Button("- Progress"))
    {
        progressValue = Math.Max(0f, progressValue - 0.1f);
    }
    ClayUI.EndHorizontal();

    ClayUI.Space(8);
    ClayUI.Label($"Button click count: {clickCount}");

    ClayUI.Space(12);
    ClayUI.Label("Usage: if (ClayUI.Button(\"Label\")) {{ /* clicked */ }}");
    ClayUI.EndPanel();

    ClayUI.Space(16);

    // ========== TEXT EDIT ==========
    ClayUI.BeginPanel("Text Edit");
    ClayUI.Label("Text edit fields powered by StbTextEdit - full cursor, selection, undo/redo, clipboard:");
    ClayUI.Space(8);

    ClayUI.Label("Single-line input:");
    ClayUI.Space(4);
    if (Clay.Clay.TextEdit(Clay.Clay.Id("SingleLineInput"), ref textEditSingleLine, new Clay.Widgets.TextInputStyle
    {
        BackgroundColor = Color.Rgba(50, 50, 55),
        FocusedBackgroundColor = Color.Rgba(60, 60, 70),
        TextColor = Color.Rgba(220, 220, 220),
        CursorColor = Color.Rgba(100, 180, 255),
        SelectionColor = Color.Rgba(80, 130, 200, 120),
        CornerRadius = CornerRadius.All(4),
        Border = new BorderConfig { Width = BorderWidth.All(1), Color = Color.Rgba(80, 80, 90) },
        Padding = Padding.Symmetric(8, 6),
        FontId = 0,
        FontSize = 16,
        Sizing = new Sizing(SizingAxis.Grow(), SizingAxis.Default),
    }))
    {
        // Text changed
    }

    ClayUI.Space(12);
    ClayUI.Label("Multi-line input:");
    ClayUI.Space(4);
    if (Clay.Clay.TextEdit(Clay.Clay.Id("MultiLineInput"), ref textEditMultiLine, new Clay.Widgets.TextInputStyle
    {
        BackgroundColor = Color.Rgba(50, 50, 55),
        FocusedBackgroundColor = Color.Rgba(60, 60, 70),
        TextColor = Color.Rgba(220, 220, 220),
        CursorColor = Color.Rgba(100, 180, 255),
        SelectionColor = Color.Rgba(80, 130, 200, 120),
        CornerRadius = CornerRadius.All(4),
        Border = new BorderConfig { Width = BorderWidth.All(1), Color = Color.Rgba(80, 80, 90) },
        Padding = Padding.All(8),
        FontId = 0,
        FontSize = 16,
        Sizing = new Sizing(SizingAxis.Grow(), SizingAxis.Fixed(120)),
    }, singleLine: false))
    {
        // Text changed
    }

    ClayUI.Space(12);
    ClayUI.Label("Empty (placeholder-style):");
    ClayUI.Space(4);
    Clay.Clay.TextEdit(Clay.Clay.Id("EmptyInput"), ref textEditEmpty, new Clay.Widgets.TextInputStyle
    {
        BackgroundColor = Color.Rgba(50, 50, 55),
        FocusedBackgroundColor = Color.Rgba(60, 60, 70),
        TextColor = Color.Rgba(220, 220, 220),
        CursorColor = Color.Rgba(100, 180, 255),
        SelectionColor = Color.Rgba(80, 130, 200, 120),
        CornerRadius = CornerRadius.All(4),
        Border = new BorderConfig { Width = BorderWidth.All(1), Color = Color.Rgba(80, 80, 90) },
        Padding = Padding.Symmetric(8, 6),
        FontId = 0,
        FontSize = 16,
        Sizing = new Sizing(SizingAxis.Grow(), SizingAxis.Default),
    });

    ClayUI.Space(12);
    ClayUI.Label($"Single-line value: \"{textEditSingleLine}\"");
    ClayUI.Label($"Multi-line lines: {textEditMultiLine.Split('\n').Length}");
    ClayUI.Label($"Empty value: \"{textEditEmpty}\"");
    ClayUI.Space(8);
    ClayUI.Label("Supports: Arrow keys, Home/End, Ctrl+A/C/X/V/Z, Shift+select, mouse click/drag, undo/redo");

    ClayUI.Space(12);
    ClayUI.Label("Usage: if (Clay.TextEdit(id, ref text, style)) {{ /* changed */ }}");
    ClayUI.EndPanel();

    ClayUI.Space(16);

    // ========== CHECKBOX ==========
    ClayUI.BeginPanel("Checkbox");
    ClayUI.Label("Checkboxes toggle a boolean value:");
    ClayUI.Space(8);

    ClayUI.Checkbox("Enable notifications", ref checkboxValue);
    ClayUI.Checkbox("Auto-save enabled", ref toggleValue);

    ClayUI.Space(12);
    ClayUI.Label($"Checkbox state: {checkboxValue}, Toggle state: {toggleValue}");

    ClayUI.Space(12);
    ClayUI.Label("Usage: ClayUI.Checkbox(\"Label\", ref boolValue)");
    ClayUI.EndPanel();

    ClayUI.Space(16);

    // ========== TOGGLE ==========
    ClayUI.BeginPanel("Toggle Switch");
    ClayUI.Label("Toggle switches provide a modern switch-style control:");
    ClayUI.Space(8);

    ClayUI.Toggle("Dark mode", ref toggleValue);
    ClayUI.Toggle("Sound effects", ref checkboxValue);

    ClayUI.Space(12);
    ClayUI.Label("Usage: ClayUI.Toggle(\"Label\", ref boolValue)");
    ClayUI.EndPanel();

    ClayUI.Space(16);

    // ========== SLIDER ==========
    ClayUI.BeginPanel("Slider");
    ClayUI.Label("Sliders allow selecting a value within a range:");
    ClayUI.Space(8);

    ClayUI.Slider("Volume", ref sliderValue, 0, 1);

    float tempProgress = progressValue;
    ClayUI.Slider("Progress", ref tempProgress, 0, 1);
    progressValue = tempProgress;

    ClayUI.Space(12);
    ClayUI.Label("Usage: ClayUI.Slider(\"Label\", ref floatValue, min, max)");
    ClayUI.EndPanel();

    ClayUI.Space(16);

    // ========== PROGRESS BAR ==========
    ClayUI.BeginPanel("Progress Bar");
    ClayUI.Label("Progress bars display a value visually:");
    ClayUI.Space(8);

    ClayUI.Label($"Current progress: {progressValue * 100:F0}%");
    ClayUI.Space(4);
    ClayUI.ProgressBar(progressValue);

    ClayUI.Space(8);
    ClayUI.Label("Download simulation:");
    ClayUI.Space(4);
    ClayUI.ProgressBar(sliderValue);

    ClayUI.Space(12);
    ClayUI.Label("Usage: ClayUI.ProgressBar(value, min, max)");
    ClayUI.EndPanel();

    ClayUI.Space(16);

    // ========== RADIO GROUP ==========
    ClayUI.BeginPanel("Radio Group");
    ClayUI.Label("Radio groups allow single selection from multiple options:");
    ClayUI.Space(8);

    ClayUI.RadioGroup("Choose your theme:", ref selectedOption, radioOptions);

    ClayUI.Space(8);
    ClayUI.Label($"Selected: {radioOptions[selectedOption]}");

    ClayUI.Space(12);
    ClayUI.Label("Usage: ClayUI.RadioGroup(\"Label\", ref selected, options)");
    ClayUI.EndPanel();

    ClayUI.Space(16);

    // ========== TREE VIEW ==========
    ClayUI.BeginPanel("Tree View / Collapsible Sections");
    ClayUI.Label("Tree nodes create collapsible hierarchical content:");
    ClayUI.Space(8);

    if (ClayUI.BeginTreeNode("Application Settings"))
    {
        ClayUI.Label("Configure your application here");

        if (ClayUI.BeginTreeNode("Display"))
        {
            ClayUI.Label("Resolution: 1920x1080");
            ClayUI.Label("Refresh Rate: 60 Hz");
            ClayUI.Checkbox("VSync", ref checkboxValue);
            ClayUI.EndTreeNode();
        }

        if (ClayUI.BeginTreeNode("Audio"))
        {
            ClayUI.Slider("Master Volume", ref sliderValue, 0, 1);
            ClayUI.Toggle("Mute", ref toggleValue);
            ClayUI.EndTreeNode();
        }

        if (ClayUI.BeginTreeNode("Controls"))
        {
            ClayUI.Label("Mouse Sensitivity: 50%");
            ClayUI.Label("Invert Y-Axis: No");
            ClayUI.EndTreeNode();
        }

        ClayUI.EndTreeNode();
    }

    if (ClayUI.BeginTreeNode("User Profile"))
    {
        ClayUI.Label("Username: Demo User");
        ClayUI.Label("Email: demo@example.com");
        ClayUI.Label("Member since: 2024");
        ClayUI.EndTreeNode();
    }

    ClayUI.Space(12);
    ClayUI.Label("Usage: if (ClayUI.BeginTreeNode(\"Label\")) { ... ClayUI.EndTreeNode(); }");
    ClayUI.EndPanel();

    ClayUI.Space(16);

    // ========== LAYOUT HELPERS ==========
    ClayUI.BeginPanel("Layout Helpers");
    ClayUI.Label("Layout helpers for organizing content:");
    ClayUI.Space(8);

    ClayUI.Label("Horizontal layout:");
    ClayUI.BeginHorizontal(gap: 8);
    ClayUI.Button("One");
    ClayUI.Button("Two");
    ClayUI.Button("Three");
    ClayUI.EndHorizontal();

    ClayUI.Space(12);

    ClayUI.Label("Vertical layout with separator:");
    ClayUI.BeginVertical(gap: 4);
    ClayUI.Label("Item 1");
    ClayUI.Separator();
    ClayUI.Label("Item 2");
    ClayUI.Separator();
    ClayUI.Label("Item 3");
    ClayUI.EndVertical();

    ClayUI.Space(12);
    ClayUI.Label("Available: BeginHorizontal/EndHorizontal, BeginVertical/EndVertical, Space, Separator");
    ClayUI.EndPanel();

    ClayUI.Space(16);

    // ========== TEXT STYLES ==========
    ClayUI.BeginPanel("Text Styles");
    ClayUI.Heading("This is a Heading");
    ClayUI.Space(4);
    ClayUI.Label("This is regular label text.");
    ClayUI.Space(8);
    ClayUI.Label("Usage: ClayUI.Heading(\"text\") and ClayUI.Label(\"text\")");
    ClayUI.EndPanel();

    ClayUI.Space(16);

    // ========== WINDOWS ==========
    ClayUI.BeginPanel("Windows (ImGui-style)");
    ClayUI.Label("Floating, draggable, collapsible windows:");
    ClayUI.Space(8);

    ClayUI.BeginHorizontal(gap: 12);
    if (ClayUI.Button("Show Window 1"))
        demoWindow1Open = true;
    if (ClayUI.Button("Show Window 2"))
        demoWindow2Open = true;
    if (ClayUI.Button("Close All"))
    {
        demoWindow1Open = false;
        demoWindow2Open = false;
    }
    ClayUI.EndHorizontal();

    ClayUI.Space(8);
    ClayUI.Label($"Window 1 open: {demoWindow1Open}, Window 2 open: {demoWindow2Open}");
    ClayUI.Space(12);
    ClayUI.Label("Usage: if (ClayUI.BeginWindow(\"Title\", ref open)) {{ ... }} ClayUI.EndWindow();");
    ClayUI.EndPanel();

    // Note: Windows are rendered at root level, not here (see RenderDemoWindows)

    ClayUI.Space(16);

    // ========== POPUPS ==========
    ClayUI.BeginPanel("Popups & Context Menus");
    ClayUI.Label("Popups appear at mouse position and close when clicking outside:");
    ClayUI.Space(8);

    // Debug: Show a floating element to test if floating works
    using (Clay.Clay.Element(new ElementDeclaration
    {
        Id = Clay.Clay.Id("TestFloat"),
        Layout = new LayoutConfig
        {
            Sizing = new Sizing { Width = SizingAxis.Fixed(100), Height = SizingAxis.Fixed(50) },
            Padding = Padding.All(8)
        },
        BackgroundColor = Color.Rgba(255, 0, 0),
        Floating = new FloatingConfig
        {
            AttachTo = FloatingAttachTo.Root,
            Offset = new Vector2(50, 50),
            ZIndex = 2000
        }
    }))
    {
        Clay.Clay.Text("FLOATING", new TextConfig { FontSize = 12, TextColor = Color.White });
    }
    ClayUI.Space(8);

    ClayUI.BeginHorizontal(gap: 12);

    // Basic popup button
    bool buttonClicked = ClayUI.Button("Open Popup");
    if (buttonClicked)
    {
        Console.WriteLine(">>> Button clicked! Opening DemoPopup");
        ClayUI.OpenPopup("DemoPopup");
    }

    // Dropdown-style button
    if (ClayUI.Button("Dropdown Menu"))
    {
        ClayUI.OpenPopup("DropdownPopup");
    }

    // Action menu
    if (ClayUI.Button("Actions..."))
    {
        ClayUI.OpenPopup("ActionsPopup");
    }

    ClayUI.EndHorizontal();

    // Render the popups
    if (ClayUI.BeginPopup("DemoPopup"))
    {
        ClayUI.Label("Hello from a popup!");
        ClayUI.Space(4);
        if (ClayUI.MenuItem("Option 1"))
            Console.WriteLine("Popup: Option 1");
        if (ClayUI.MenuItem("Option 2"))
            Console.WriteLine("Popup: Option 2");
        ClayUI.MenuSeparator();
        if (ClayUI.MenuItem("Close"))
        { } // MenuItem auto-closes
        ClayUI.EndPopup();
    }

    if (ClayUI.BeginPopup("DropdownPopup"))
    {
        if (ClayUI.MenuItem("New File"))
            Console.WriteLine("New File");
        if (ClayUI.MenuItem("Open File"))
            Console.WriteLine("Open File");
        if (ClayUI.MenuItem("Save"))
            Console.WriteLine("Save");
        ClayUI.MenuSeparator();
        if (ClayUI.MenuItem("Exit"))
            Console.WriteLine("Exit");
        ClayUI.EndPopup();
    }

    if (ClayUI.BeginPopup("ActionsPopup"))
    {
        if (ClayUI.MenuItem("Cut"))
            Console.WriteLine("Cut");
        if (ClayUI.MenuItem("Copy"))
            Console.WriteLine("Copy");
        if (ClayUI.MenuItem("Paste"))
            Console.WriteLine("Paste");
        ClayUI.MenuSeparator();
        if (ClayUI.MenuItem("Select All"))
            Console.WriteLine("Select All");
        ClayUI.MenuSeparator();
        ClayUI.MenuItem("Disabled Item", enabled: false);
        ClayUI.EndPopup();
    }

    ClayUI.Space(12);
    string popupState = "All closed";
    if (ClayUI.IsPopupOpen("DemoPopup")) popupState = "DemoPopup OPEN";
    else if (ClayUI.IsPopupOpen("DropdownPopup")) popupState = "DropdownPopup OPEN";
    else if (ClayUI.IsPopupOpen("ActionsPopup")) popupState = "ActionsPopup OPEN";
    ClayUI.Label($"Popup state: {popupState}");
    ClayUI.Space(12);
    ClayUI.Label("Usage: ClayUI.OpenPopup(\"id\"); if (ClayUI.BeginPopup(\"id\")) {{ ClayUI.MenuItem(...); ClayUI.EndPopup(); }}");
    ClayUI.EndPanel();

    ClayUI.Space(20);
    ClayUI.Separator();
    ClayUI.Space(8);
    ClayUI.Label("End of Widget Demo - Scroll up to see more!");
}

void RenderFooter()
{
    using (Clay.Clay.Element(new ElementDeclaration
    {
        Id = Clay.Clay.Id("Footer"),
        Layout = new LayoutConfig
        {
            Sizing = new Sizing
            {
                Width = SizingAxis.Grow(),
                Height = SizingAxis.Fixed(40)
            },
            Direction = LayoutDirection.LeftToRight,
            Padding = Padding.Horizontal(16),
            ChildAlignment = ChildAlignment.Center
        },
        BackgroundColor = Color.Rgba(35, 35, 40),
        CornerRadius = CornerRadius.All(8)
    }))
    {
        Clay.Clay.Text("Pure .NET Clay UI Library - No native dependencies", new TextConfig
        {
            FontId = 0,
            FontSize = 12,
            TextColor = Color.Rgba(120, 120, 130)
        });
    }
}

// ============ Demo Windows ============

void RenderDemoWindows()
{
    if (ClayUI.BeginWindow("Demo Window", ref demoWindow1Open,
        defaultPosition: new Vector2(400, 150),
        defaultSize: new Vector2(300, 200)))
    {
        ClayUI.Label("This is a draggable window!");
        ClayUI.Space(8);
        ClayUI.Checkbox("A checkbox", ref windowCheckbox);
        ClayUI.Slider("Volume", ref windowSlider, 0, 1);
        ClayUI.Space(8);
        if (ClayUI.Button("Click me!"))
        {
            windowCheckbox = !windowCheckbox;
        }
    }
    ClayUI.EndWindow();

    if (ClayUI.BeginWindow("Settings", ref demoWindow2Open,
        defaultPosition: new Vector2(720, 200),
        defaultSize: new Vector2(250, 180)))
    {
        ClayUI.Label("Window without scrolling");
        ClayUI.Space(8);
        ClayUI.Toggle("Dark mode", ref toggleValue);
        ClayUI.Toggle("Sound", ref checkboxValue);
    }
    ClayUI.EndWindow();
}

// ============ Keyboard Input for TextEdit ============

void ForwardKeyboardInput()
{
    if (!Clay.Clay.TextEditHasFocus) return;

    bool shift = Raylib.IsKeyDown(KeyboardKey.KEY_LEFT_SHIFT) || Raylib.IsKeyDown(KeyboardKey.KEY_RIGHT_SHIFT);
    bool ctrl = Raylib.IsKeyDown(KeyboardKey.KEY_LEFT_CONTROL) || Raylib.IsKeyDown(KeyboardKey.KEY_RIGHT_CONTROL);
    var mods = (shift ? Clay.Widgets.ClayKeyModifiers.Shift : 0)
             | (ctrl ? Clay.Widgets.ClayKeyModifiers.Ctrl : 0);
    float dt = Raylib.GetFrameTime();

    ReadOnlySpan<(KeyboardKey rayKey, Clay.Widgets.ClayKey clayKey)> keyMap =
    [
        (KeyboardKey.KEY_LEFT, Clay.Widgets.ClayKey.Left),
        (KeyboardKey.KEY_RIGHT, Clay.Widgets.ClayKey.Right),
        (KeyboardKey.KEY_UP, Clay.Widgets.ClayKey.Up),
        (KeyboardKey.KEY_DOWN, Clay.Widgets.ClayKey.Down),
        (KeyboardKey.KEY_HOME, Clay.Widgets.ClayKey.Home),
        (KeyboardKey.KEY_END, Clay.Widgets.ClayKey.End),
        (KeyboardKey.KEY_PAGE_UP, Clay.Widgets.ClayKey.PageUp),
        (KeyboardKey.KEY_PAGE_DOWN, Clay.Widgets.ClayKey.PageDown),
        (KeyboardKey.KEY_DELETE, Clay.Widgets.ClayKey.Delete),
        (KeyboardKey.KEY_BACKSPACE, Clay.Widgets.ClayKey.Backspace),
        (KeyboardKey.KEY_ENTER, Clay.Widgets.ClayKey.Enter),
        (KeyboardKey.KEY_KP_ENTER, Clay.Widgets.ClayKey.Enter),
        (KeyboardKey.KEY_TAB, Clay.Widgets.ClayKey.Tab),
        (KeyboardKey.KEY_INSERT, Clay.Widgets.ClayKey.Insert),
        (KeyboardKey.KEY_A, Clay.Widgets.ClayKey.A),
        (KeyboardKey.KEY_C, Clay.Widgets.ClayKey.C),
        (KeyboardKey.KEY_V, Clay.Widgets.ClayKey.V),
        (KeyboardKey.KEY_X, Clay.Widgets.ClayKey.X),
        (KeyboardKey.KEY_Z, Clay.Widgets.ClayKey.Z),
    ];

    foreach (var (rayKey, clayKey) in keyMap)
        if (Raylib.IsKeyDown(rayKey))
            Clay.Clay.TextEditKeyDown(clayKey, mods, dt);

    int ch;
    while ((ch = Raylib.GetCharPressed()) != 0)
        if (ch >= 32)
            Clay.Clay.TextEditProcessChar((char)ch);
}

// ============ Data Types ============

record Document(string Title, string Content);
