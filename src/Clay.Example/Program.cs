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

unsafe
{
    if (fonts[0].glyphs == null)
        fonts[0] = Raylib.GetFontDefault();
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
Clay.Clay.TextEditSetClipboard(new RaylibClipboard());

// Load RPG skin assets
RpgSkin.Load();

// ============ Application State ============

// Sidebar
string[] pages = ["Overview", "Buttons", "Text Input", "Checkboxes & Toggles", "Sliders & Progress",
    "Radio Group", "Tree View", "Layout Helpers", "Text Styles", "Color Picker",
    "ListBox & Combo", "Scroll Areas", "Windows", "Docking", "Popups & Context Menus", "Theming", "Disabled States", "RPG Skin"];
int selectedPage = 0;

// Widget state
bool checkboxValue = true;
bool toggleValue = false;
float sliderValue = 0.5f;
int clickCount = 0;
float progressValue = 0.3f;
int selectedOption = 0;
string[] radioOptions = ["Option A", "Option B", "Option C"];

// Text input state
string textEditSingleLine = "Hello, Clay!";
string textEditMultiLine = "Line 1\nLine 2\nLine 3";
string textEditNumber = "42";
string textEditEmpty = "";

// Window state
bool demoWindow1Open = true;
bool demoWindow2Open = true;
bool lockedWindowOpen = true;
bool windowCheckbox = false;
float windowSlider = 0.5f;

// Color picker state
Color pickerColor = Color.Rgba(70, 130, 200);

// ListBox & Combo state
int listBoxSelection = 0;
string[] listBoxItems = ["Apple", "Banana", "Cherry", "Date", "Elderberry", "Fig", "Grape", "Honeydew", "Kiwi", "Lemon", "Mango", "Nectarine", "Orange", "Papaya"];
int comboSelection = 0;
string[] comboItems = ["Small", "Medium", "Large", "Extra Large"];

// Disabled demo state
bool disabledDemoCheckbox = true;
bool disabledDemoToggle = false;
float disabledDemoSlider = 0.6f;
string disabledDemoText = "Can't edit this";

// RPG Skin demo state
bool rpgSkinCheckbox = false;
bool rpgSkinToggle = true;
float rpgSkinSlider = 0.4f;
float rpgSkinProgress = 0.65f;
int rpgSkinRadio = 0;
string[] rpgRadioOptions = ["Warrior", "Mage", "Ranger"];
ClayUISkin? rpgSkin = null;

// Docking demo state
bool dockPanel1Open = true;
bool dockPanel2Open = true;
bool dockPanel3Open = true;
bool dockPanel4Open = true;
bool dockFloatingOpen = true;
int dockDemoCounter = 0;
bool dockDemoCheckbox = false;
float dockDemoSlider = 0.5f;
string[] dockLogMessages = [
    "Application started",
    "Loaded 3 panels",
    "Dock layout initialized",
    "Ready for interaction"
];

// Debug
bool debugWindowOpen = false;

// Theme (0=Default, 1=Dark, 2=Light)
int themeIndex = 0;
int pendingTheme = -1;

// ============ Main Loop ============

while (!Raylib.WindowShouldClose())
{
    var mousePos = Raylib.GetMousePosition();
    var mouseWheel = Raylib.GetMouseWheelMoveV();
    bool mouseDown = Raylib.IsMouseButtonDown(0);
    var scrollDelta = new Vector2(mouseWheel.X, mouseWheel.Y);
    float deltaTime = Raylib.GetFrameTime();

    // Apply deferred theme switch before frame starts
    if (pendingTheme >= 0)
    {
        ClayUI.Style = pendingTheme switch
        {
            1 => ClayUIStyle.Dark,
            2 => ClayUIStyle.Light,
            _ => ClayUIStyle.Default
        };
        themeIndex = pendingTheme;
        pendingTheme = -1;
    }

    ClayUI.BeginFrame(new Dimensions(Raylib.GetScreenWidth(), Raylib.GetScreenHeight()),
        mouseDown, new Vector2(mousePos.X, mousePos.Y), scrollDelta, deltaTime);

    if (Raylib.IsKeyPressed(KeyboardKey.KEY_F12))
        ClayUI.ToggleDebugWindow();

    ForwardKeyboardInput();

    // Root container
    ClayUI.BeginVertical(gap: 16, style: new LayoutStyle
    {
        Sizing = Sizing.Fill(),
        Padding = Padding.All(16),
        BackgroundColor = ClayUI.Style.Window.BackgroundColor
    });

    // ===== Header =====
    RenderHeader();

    // ===== Main Area (Sidebar + Content) =====
    ClayUI.BeginHorizontal(gap: 16, style: new LayoutStyle
    {
        Sizing = Sizing.Fill()
    });
    RenderSidebar();
    RenderContent();
    ClayUI.EndHorizontal();

    // ===== Footer =====
    ClayUI.BeginHorizontal(style: new LayoutStyle { Padding = Padding.Symmetric(8, 4) });
    ClayUI.Label("Pure .NET Clay UI Library - No native dependencies | F12 or Debug button for inspector",
        new LabelStyle { FontSize = 12 });
    ClayUI.EndHorizontal();

    // ===== Windows (rendered at root level) =====
    if (selectedPage == Array.IndexOf(pages, "Windows"))
        RenderDemoWindows();

    // ===== Docking demo windows (rendered at root level) =====
    if (selectedPage == Array.IndexOf(pages, "Docking"))
        RenderDockingDemoWindows();

    if (debugWindowOpen)
        ClayUI.ShowDebugWindow();

    ClayUI.EndVertical();

    var commands = ClayUI.EndFrame();

    Raylib.BeginDrawing();
    Raylib.ClearBackground(Raylib.BLACK);
    renderer.Render(commands);
    Raylib.EndDrawing();
}

RpgSkin.Unload();
Clay.Clay.Shutdown();
Raylib.CloseWindow();

// ============ Header ============

void RenderHeader()
{
    ClayUI.BeginHorizontal(gap: 12, alignment: ChildAlignment.CenterLeft, style: new LayoutStyle
    {
        Sizing = new Sizing(SizingAxis.Grow(), SizingAxis.Fixed(50)),
        Padding = Padding.Horizontal(16),
        BackgroundColor = ClayUI.Style.Window.TitleBarColor,
        CornerRadius = CornerRadius.All(8)
    });

    ClayUI.Heading("Clay .NET", new HeadingStyle { TextColor = Color.Rgba(100, 180, 255) });

    ClayUI.Spacer();

    // Header menu buttons using ClayUI popups
    if (ClayUI.Button("File")) ClayUI.OpenPopup("FileMenu");
    if (ClayUI.Button("Edit")) ClayUI.OpenPopup("EditMenu");
    if (ClayUI.Button("View")) ClayUI.OpenPopup("ViewMenu");
    if (ClayUI.Button("Help")) ClayUI.OpenPopup("HelpMenu");
    if (ClayUI.Button(debugWindowOpen ? "Debug [ON]" : "Debug"))
    {
        debugWindowOpen = !debugWindowOpen;
    }

    // File menu popup
    if (ClayUI.BeginPopup("FileMenu"))
    {
        if (ClayUI.MenuItem("New")) Console.WriteLine("File > New");
        if (ClayUI.MenuItem("Open")) Console.WriteLine("File > Open");
        if (ClayUI.MenuItem("Save")) Console.WriteLine("File > Save");
        ClayUI.MenuSeparator();
        if (ClayUI.MenuItem("Exit")) Environment.Exit(0);
        ClayUI.EndPopup();
    }

    // Edit menu popup
    if (ClayUI.BeginPopup("EditMenu"))
    {
        if (ClayUI.MenuItem("Undo")) Console.WriteLine("Edit > Undo");
        if (ClayUI.MenuItem("Redo")) Console.WriteLine("Edit > Redo");
        ClayUI.MenuSeparator();
        if (ClayUI.MenuItem("Cut")) Console.WriteLine("Edit > Cut");
        if (ClayUI.MenuItem("Copy")) Console.WriteLine("Edit > Copy");
        if (ClayUI.MenuItem("Paste")) Console.WriteLine("Edit > Paste");
        ClayUI.EndPopup();
    }

    // View menu popup
    if (ClayUI.BeginPopup("ViewMenu"))
    {
        if (ClayUI.MenuItem("Zoom In")) Console.WriteLine("View > Zoom In");
        if (ClayUI.MenuItem("Zoom Out")) Console.WriteLine("View > Zoom Out");
        ClayUI.MenuSeparator();
        if (ClayUI.MenuItem("Debug Window (F12)")) ClayUI.ToggleDebugWindow();
        ClayUI.EndPopup();
    }

    // Help menu popup
    if (ClayUI.BeginPopup("HelpMenu"))
    {
        if (ClayUI.MenuItem("Documentation")) Console.WriteLine("Help > Docs");
        if (ClayUI.MenuItem("About")) Console.WriteLine("Help > About");
        ClayUI.EndPopup();
    }

    ClayUI.EndHorizontal();
}

// ============ Sidebar ============

void RenderSidebar()
{
    ClayUI.BeginPanel("Pages", scroll: true, style: new PanelStyle
    {
        BackgroundColor = ClayUI.Style.Panel.BackgroundColor,
        TitleColor = ClayUI.Style.Panel.TitleColor,
        SeparatorColor = ClayUI.Style.Panel.SeparatorColor,
        Border = ClayUI.Style.Panel.Border,
        Padding = Padding.All(12),
        ChildGap = 4
    });

    for (int i = 0; i < pages.Length; i++)
    {
        bool isSelected = selectedPage == i;
        var s = ClayUI.Style.Button;
        var btnStyle = new ButtonStyle
        {
            BackgroundColor = isSelected ? Color.Rgba(70, 130, 200) : Color.Rgba(0, 0, 0, 0),
            HoverColor = isSelected ? Color.Rgba(80, 140, 210) : s.HoverColor,
            PressedColor = Color.Rgba(60, 120, 190),
            TextColor = isSelected ? Color.White : ClayUI.Style.Label.TextColor,
            Padding = Padding.Symmetric(10, 8),
            CornerRadius = CornerRadius.All(6),
            FontSize = 14
        };
        if (ClayUI.Button(pages[i] + $"##page_{i}", btnStyle))
        {
            if (selectedPage != i)
                Clay.Clay.ResetScrollPosition(ClayUI.StableId("ScrollArea_Content"));
            selectedPage = i;
        }
    }

    ClayUI.EndPanel();
}

// ============ Content Area ============

void RenderContent()
{
    ClayUI.BeginScrollArea("Content", horizontal: true, style: new ScrollAreaStyle
    {
        BackgroundColor = ClayUI.Style.Panel.BackgroundColor,
        Padding = Padding.All(20),
        CornerRadius = CornerRadius.All(8)
    });

    ClayUI.Heading(pages[selectedPage]);
    ClayUI.Separator();
    ClayUI.Space(8);

    switch (selectedPage)
    {
        case 0: PageOverview(); break;
        case 1: PageButtons(); break;
        case 2: PageTextInput(); break;
        case 3: PageCheckboxesAndToggles(); break;
        case 4: PageSlidersAndProgress(); break;
        case 5: PageRadioGroup(); break;
        case 6: PageTreeView(); break;
        case 7: PageLayoutHelpers(); break;
        case 8: PageTextStyles(); break;
        case 9: PageColorPicker(); break;
        case 10: PageListBoxAndCombo(); break;
        case 11: PageScrollAreas(); break;
        case 12: PageWindows(); break;
        case 13: PageDocking(); break;
        case 14: PagePopups(); break;
        case 15: PageTheming(); break;
        case 16: PageDisabledStates(); break;
        case 17: PageRpgSkin(); break;
    }

    ClayUI.EndScrollArea();
}

// ============ Pages ============

void PageOverview()
{
    ClayUI.Label("Welcome to the ClayUI Widget Demo!");
    ClayUI.Space(4);
    ClayUI.Label("This example showcases every widget available in the ClayUI immediate-mode API.");
    ClayUI.Label("Use the sidebar to navigate between widget demos.");
    ClayUI.Space(8);
    ClayUI.Label("ClayUI is a high-level widget layer built on top of the Clay layout engine.");
    ClayUI.Label("It provides ready-to-use controls like buttons, sliders, windows, popups, and more.");
    ClayUI.Space(8);
    ClayUI.Label("Key features:");
    ClayUI.Label("  - Immediate-mode API (no retained state to manage)");
    ClayUI.Label("  - Draggable, resizable, collapsible windows");
    ClayUI.Label("  - Popup menus with click-outside-to-close");
    ClayUI.Label("  - HSV color picker with swatch preview");
    ClayUI.Label("  - Full text editing (cursor, selection, undo, clipboard)");
    ClayUI.Label("  - Theme switching (Default, Dark, Light)");
    ClayUI.Label("  - Debug inspector window (F12)");
}

void PageButtons()
{
    ClayUI.Label("Buttons return true when clicked:");
    ClayUI.Space(8);

    ClayUI.BeginHorizontal(gap: 12);
    if (ClayUI.Button("Click Me")) clickCount++;
    ClayUI.Tooltip("Increments the click counter");
    if (ClayUI.Button("Reset")) { clickCount = 0; progressValue = 0; sliderValue = 0.5f; }
    ClayUI.Tooltip("Reset all values to defaults");
    if (ClayUI.Button("+ Progress")) progressValue = Math.Min(1f, progressValue + 0.1f);
    ClayUI.Tooltip("Increase progress by 10%");
    if (ClayUI.Button("- Progress")) progressValue = Math.Max(0f, progressValue - 0.1f);
    ClayUI.Tooltip("Decrease progress by 10%");
    ClayUI.EndHorizontal();

    ClayUI.Space(8);
    ClayUI.Label($"Click count: {clickCount}");

    ClayUI.Space(16);
    ClayUI.Label("Per-widget style override:");
    ClayUI.Space(4);
    ClayUI.Space(8);
    ClayUI.Label("Tooltips appear after hovering for 0.5s:");
    ClayUI.Space(4);
    ClayUI.Button("Hover for Rich Tooltip");
    if (ClayUI.BeginTooltip())
    {
        ClayUI.Label("Rich Tooltip");
        ClayUI.Separator();
        ClayUI.Label("Supports any widget content.");
        ClayUI.EndTooltip();
    }

    ClayUI.Space(16);
    ClayUI.Label("Per-widget style override:");
    ClayUI.Space(4);
    ClayUI.Button("Custom Style", new ButtonStyle
    {
        BackgroundColor = Color.Rgba(180, 60, 60),
        HoverColor = Color.Rgba(200, 80, 80),
        PressedColor = Color.Rgba(160, 40, 40),
        TextColor = Color.White,
        CornerRadius = CornerRadius.All(16),
        Padding = Padding.Symmetric(24, 10)
    });
}

void PageTextInput()
{
    ClayUI.Label("Text input fields powered by StbTextEdit:");
    ClayUI.Space(8);

    ClayUI.Label("Single-line:");
    ClayUI.TextInput("SingleLine", ref textEditSingleLine);

    ClayUI.Space(12);
    ClayUI.Label("Multi-line:");
    ClayUI.TextInput("MultiLine", ref textEditMultiLine, singleLine: false,
        style: new Clay.Widgets.TextInputStyle
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
        });

    ClayUI.Space(12);
    ClayUI.Label("Numbers only (CharFilter):");
    ClayUI.TextInput("NumberOnly", ref textEditNumber,
        style: new Clay.Widgets.TextInputStyle
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
            Sizing = new Sizing(SizingAxis.Fixed(200), SizingAxis.Default),
            CharFilter = Clay.Widgets.TextInputFilters.NumbersOnly,
        });

    ClayUI.Space(12);
    ClayUI.Label("Empty input:");
    ClayUI.TextInput("Empty", ref textEditEmpty);

    ClayUI.Space(12);
    ClayUI.Label($"Values: single=\"{textEditSingleLine}\", number=\"{textEditNumber}\"");
    ClayUI.Label("Supports: Arrow keys, Home/End, Ctrl+A/C/X/V/Z, Shift+select, mouse drag, undo/redo");
}

void PageCheckboxesAndToggles()
{
    ClayUI.Label("Checkboxes toggle a boolean value:");
    ClayUI.Space(8);
    ClayUI.Checkbox("Enable notifications", ref checkboxValue);
    ClayUI.Checkbox("Auto-save enabled", ref toggleValue);
    ClayUI.Space(8);
    ClayUI.Label($"States: notifications={checkboxValue}, auto-save={toggleValue}");

    ClayUI.Space(20);
    ClayUI.Separator();
    ClayUI.Space(12);

    ClayUI.Label("Toggle switches provide a modern on/off control:");
    ClayUI.Space(8);
    ClayUI.Toggle("Dark mode", ref toggleValue);
    ClayUI.Toggle("Sound effects", ref checkboxValue);
}

void PageSlidersAndProgress()
{
    ClayUI.Label("Sliders select a value within a range:");
    ClayUI.Space(8);
    ClayUI.Slider("Volume", ref sliderValue, 0, 1);
    float tempProgress = progressValue;
    ClayUI.Slider("Progress", ref tempProgress, 0, 1);
    progressValue = tempProgress;

    ClayUI.Space(20);
    ClayUI.Separator();
    ClayUI.Space(12);

    ClayUI.Label("Progress bars display a value visually:");
    ClayUI.Space(4);
    ClayUI.Label($"Current: {progressValue * 100:F0}%");
    ClayUI.ProgressBar(progressValue);
    ClayUI.Space(4);
    ClayUI.Label("Linked to slider:");
    ClayUI.ProgressBar(sliderValue);
}

void PageRadioGroup()
{
    ClayUI.Label("Radio groups allow single selection from multiple options:");
    ClayUI.Space(8);
    ClayUI.RadioGroup("Choose your option:", ref selectedOption, radioOptions);
    ClayUI.Space(8);
    ClayUI.Label($"Selected: {radioOptions[selectedOption]}");
}

void PageTreeView()
{
    ClayUI.Label("Tree nodes create collapsible hierarchical content:");
    ClayUI.Space(8);

    if (ClayUI.BeginTreeNode("Application Settings"))
    {
        ClayUI.Label("Configure your application here");

        if (ClayUI.BeginTreeNode("Display"))
        {
            ClayUI.Label("Resolution: 1920x1080");
            ClayUI.Checkbox("VSync", ref checkboxValue);
            ClayUI.EndTreeNode();
        }

        if (ClayUI.BeginTreeNode("Audio"))
        {
            ClayUI.Slider("Master Volume", ref sliderValue, 0, 1);
            ClayUI.Toggle("Mute", ref toggleValue);
            ClayUI.EndTreeNode();
        }

        ClayUI.EndTreeNode();
    }

    if (ClayUI.BeginTreeNode("User Profile"))
    {
        ClayUI.Label("Username: Demo User");
        ClayUI.Label("Email: demo@example.com");
        ClayUI.EndTreeNode();
    }
}

void PageLayoutHelpers()
{
    ClayUI.Label("Horizontal layout:");
    ClayUI.BeginHorizontal(gap: 8);
    ClayUI.Button("One");
    ClayUI.Button("Two");
    ClayUI.Button("Three");
    ClayUI.EndHorizontal();

    ClayUI.Space(16);

    ClayUI.Label("Vertical layout with separators:");
    ClayUI.BeginVertical(gap: 4);
    ClayUI.Label("Item 1");
    ClayUI.Separator();
    ClayUI.Label("Item 2");
    ClayUI.Separator();
    ClayUI.Label("Item 3");
    ClayUI.EndVertical();

    ClayUI.Space(16);
    ClayUI.Label("Space(n) adds vertical spacing. Separator() draws a dividing line.");
}

void PageTextStyles()
{
    ClayUI.Heading("This is a Heading");
    ClayUI.Space(4);
    ClayUI.Label("This is regular label text.");
    ClayUI.Space(8);
    ClayUI.Label("Custom label style:", new LabelStyle
    {
        TextColor = Color.Rgba(100, 180, 255),
        FontSize = 18
    });
}

void PageColorPicker()
{
    ClayUI.Label("Click the color swatch to open an HSV color picker popup:");
    ClayUI.Space(8);
    pickerColor = ClayUI.ColorPicker("Pick a color", pickerColor);
    ClayUI.Space(8);
    ClayUI.Label($"RGBA: ({(int)pickerColor.R}, {(int)pickerColor.G}, {(int)pickerColor.B}, {(int)pickerColor.A})");

    ClayUI.Space(16);

    ClayUI.Label("The swatch shows the current color. The popup contains:");
    ClayUI.Label("  - Saturation/Value panel (smooth gradient)");
    ClayUI.Label("  - Hue bar");
    ClayUI.Label("  - Current color preview");
    ClayUI.Label("  - R, G, B, A number inputs");
}

void PageListBoxAndCombo()
{
    ClayUI.Label("ListBox — a scrollable list with selectable items:");
    ClayUI.Space(8);

    ClayUI.BeginListBox("Fruits##lb1", maxHeight: 180);
    for (int i = 0; i < listBoxItems.Length; i++)
    {
        if (ClayUI.ListBoxItem(listBoxItems[i] + $"##lb1_{i}", i == listBoxSelection))
            listBoxSelection = i;
    }
    ClayUI.EndListBox();

    ClayUI.Space(8);
    ClayUI.Label($"Selected: {listBoxItems[listBoxSelection]}");

    ClayUI.Space(20);
    ClayUI.Separator();
    ClayUI.Space(12);

    ClayUI.Label("Combo — a dropdown that opens on click:");
    ClayUI.Space(8);

    ClayUI.Combo("Size", ref comboSelection, comboItems);
    ClayUI.Space(8);
    ClayUI.Label($"Selected size: {comboItems[comboSelection]}");
}

void PageScrollAreas()
{
    ClayUI.Label("BeginScrollArea creates a scrollable region with an automatic scrollbar:");
    ClayUI.Space(8);

    ClayUI.BeginScrollArea("DemoScroll", maxHeight: 150);

    for (int i = 0; i < 20; i++)
        ClayUI.Label($"  Scrollable item {i + 1}");

    ClayUI.EndScrollArea();
}

void PageWindows()
{
    ClayUI.Label("Floating, draggable, resizable, collapsible windows:");
    ClayUI.Space(8);

    ClayUI.BeginHorizontal(gap: 12);
    if (ClayUI.Button("Show Window 1")) demoWindow1Open = true;
    if (ClayUI.Button("Show Window 2")) demoWindow2Open = true;
    if (ClayUI.Button("Show Locked Window")) lockedWindowOpen = true;
    if (ClayUI.Button("Close All")) { demoWindow1Open = false; demoWindow2Open = false; lockedWindowOpen = false; }
    ClayUI.EndHorizontal();

    ClayUI.Space(8);
    ClayUI.Label($"Window 1: {(demoWindow1Open ? "open" : "closed")}, Window 2: {(demoWindow2Open ? "open" : "closed")}");

    ClayUI.Space(12);
    ClayUI.Label("Programmatic window control:");
    ClayUI.BeginHorizontal(gap: 8);
    if (ClayUI.Button("Move W1 to (100,100)")) ClayUI.SetWindowPosition("Demo Window", new Vector2(100, 100));
    if (ClayUI.Button("Resize W1 to 400x300")) ClayUI.SetWindowSize("Demo Window", new Vector2(400, 300));
    ClayUI.EndHorizontal();

    ClayUI.Space(8);
    var pos = ClayUI.GetWindowPosition("Demo Window");
    var size = ClayUI.GetWindowSize("Demo Window");
    ClayUI.Label($"Window 1 pos: ({pos.X:F0}, {pos.Y:F0}), size: ({size.X:F0}, {size.Y:F0})");

    ClayUI.Space(12);
    ClayUI.Label("WindowFlags: The 'Locked' window uses NoMove | NoResize | NoCollapse.");
}

void PagePopups()
{
    ClayUI.Label("Popups appear at mouse position and close when clicking outside:");
    ClayUI.Space(8);

    ClayUI.BeginHorizontal(gap: 12);
    if (ClayUI.Button("Open Popup")) ClayUI.OpenPopup("DemoPopup");
    if (ClayUI.Button("Dropdown Menu")) ClayUI.OpenPopup("DropdownPopup");
    if (ClayUI.Button("Actions...")) ClayUI.OpenPopup("ActionsPopup");
    ClayUI.EndHorizontal();

    // Popups
    if (ClayUI.BeginPopup("DemoPopup"))
    {
        ClayUI.Label("Hello from a popup!");
        ClayUI.Space(4);
        if (ClayUI.MenuItem("Option 1")) Console.WriteLine("Popup: Option 1");
        if (ClayUI.MenuItem("Option 2")) Console.WriteLine("Popup: Option 2");
        ClayUI.MenuSeparator();
        ClayUI.MenuItem("Close"); // auto-closes
        ClayUI.EndPopup();
    }

    if (ClayUI.BeginPopup("DropdownPopup"))
    {
        if (ClayUI.MenuItem("New File")) Console.WriteLine("New File");
        if (ClayUI.MenuItem("Open File")) Console.WriteLine("Open File");
        if (ClayUI.MenuItem("Save")) Console.WriteLine("Save");
        ClayUI.MenuSeparator();
        ClayUI.MenuItem("Disabled Item", enabled: false);
        ClayUI.EndPopup();
    }

    if (ClayUI.BeginPopup("ActionsPopup"))
    {
        if (ClayUI.MenuItem("Cut")) Console.WriteLine("Cut");
        if (ClayUI.MenuItem("Copy")) Console.WriteLine("Copy");
        if (ClayUI.MenuItem("Paste")) Console.WriteLine("Paste");
        ClayUI.MenuSeparator();
        if (ClayUI.BeginMenu("Transform"))
        {
            if (ClayUI.MenuItem("Uppercase")) Console.WriteLine("Uppercase");
            if (ClayUI.MenuItem("Lowercase")) Console.WriteLine("Lowercase");
            if (ClayUI.BeginMenu("Encoding"))
            {
                if (ClayUI.MenuItem("UTF-8")) Console.WriteLine("UTF-8");
                if (ClayUI.MenuItem("ASCII")) Console.WriteLine("ASCII");
                if (ClayUI.MenuItem("Base64")) Console.WriteLine("Base64");
                ClayUI.EndMenu();
            }
            ClayUI.EndMenu();
        }
        if (ClayUI.BeginMenu("Insert"))
        {
            if (ClayUI.MenuItem("Date/Time")) Console.WriteLine("Date/Time");
            if (ClayUI.MenuItem("UUID")) Console.WriteLine("UUID");
            ClayUI.EndMenu();
        }
        ClayUI.MenuSeparator();
        if (ClayUI.MenuItem("Select All")) Console.WriteLine("Select All");
        ClayUI.EndPopup();
    }

    ClayUI.Space(12);
    string popupState = "All closed";
    if (ClayUI.IsPopupOpen("DemoPopup")) popupState = "DemoPopup open";
    else if (ClayUI.IsPopupOpen("DropdownPopup")) popupState = "DropdownPopup open";
    else if (ClayUI.IsPopupOpen("ActionsPopup")) popupState = "ActionsPopup open";
    ClayUI.Label($"Popup state: {popupState}");

    ClayUI.Space(16);
    ClayUI.Separator();
    ClayUI.Space(8);

    ClayUI.Label("Context menus open on right-click (not yet wired in this demo — requires right-click input).");
    ClayUI.Label("API: ClayUI.BeginContextMenu(id, triggerId) / ClayUI.EndContextMenu()");

    ClayUI.Space(16);
    ClayUI.Separator();
    ClayUI.Space(8);

    ClayUI.Label("Modal popups block all input behind them with a dimming overlay:");
    ClayUI.Space(8);
    if (ClayUI.Button("Show Modal")) ClayUI.OpenPopup("DemoModal");

    if (ClayUI.BeginPopupModal("DemoModal"))
    {
        ClayUI.Label("This is a modal dialog.");
        ClayUI.Label("You cannot interact with anything behind it.");
        ClayUI.Space(8);
        ClayUI.BeginHorizontal(gap: 8);
        if (ClayUI.Button("OK"))
        {
            Console.WriteLine("Modal: OK");
            ClayUI.CloseAllPopups();
        }
        if (ClayUI.Button("Cancel"))
        {
            Console.WriteLine("Modal: Cancel");
            ClayUI.CloseAllPopups();
        }
        ClayUI.EndHorizontal();
        ClayUI.EndPopupModal();
    }
}

void PageTheming()
{
    ClayUI.Label("Switch between built-in themes:");
    ClayUI.Space(8);

    ClayUI.BeginHorizontal(gap: 12);
    if (ClayUI.Button("Default Theme")) pendingTheme = 0;
    if (ClayUI.Button("Dark Theme")) pendingTheme = 1;
    if (ClayUI.Button("Light Theme")) pendingTheme = 2;
    ClayUI.EndHorizontal();

    ClayUI.Space(8);
    ClayUI.Label($"Current theme: {(themeIndex == 0 ? "Default" : themeIndex == 1 ? "Dark" : "Light")}");

    ClayUI.Space(16);
    ClayUI.Label("Theme changes affect all ClayUI widgets rendered after the switch.");
    ClayUI.Label("You can also create custom themes by constructing a new ClayUIStyle.");

    ClayUI.Space(16);
    ClayUI.Separator();
    ClayUI.Space(8);

    ClayUI.Label("Preview widgets with current theme:");
    ClayUI.Space(4);
    ClayUI.Button("Sample Button");
    bool tempBool = true;
    ClayUI.Checkbox("Sample Checkbox", ref tempBool);
    ClayUI.Toggle("Sample Toggle", ref tempBool);
    float tempFloat = 0.6f;
    ClayUI.Slider("Sample Slider", ref tempFloat, 0, 1);
    ClayUI.ProgressBar(0.7f);
}

void PageDisabledStates()
{
    ClayUI.Label("BeginDisabled/EndDisabled makes widgets non-interactive and grayed out:");
    ClayUI.Space(8);

    // Normal widgets for comparison
    ClayUI.Label("Normal (enabled):");
    ClayUI.Space(4);
    ClayUI.BeginHorizontal(gap: 12);
    ClayUI.Button("Enabled Button");
    ClayUI.Checkbox("Enabled Checkbox", ref disabledDemoCheckbox);
    ClayUI.EndHorizontal();

    ClayUI.Space(16);
    ClayUI.Separator();
    ClayUI.Space(8);

    // Disabled widgets
    ClayUI.Label("Disabled:");
    ClayUI.Space(4);
    ClayUI.BeginDisabled();

    ClayUI.BeginHorizontal(gap: 12);
    ClayUI.Button("Disabled Button");
    ClayUI.Button("Can't Click");
    ClayUI.EndHorizontal();
    ClayUI.Space(4);

    ClayUI.Checkbox("Disabled Checkbox", ref disabledDemoCheckbox);
    ClayUI.Toggle("Disabled Toggle", ref disabledDemoToggle);
    ClayUI.Slider("Disabled Slider", ref disabledDemoSlider, 0, 1);
    ClayUI.ProgressBar(0.7f);
    ClayUI.TextInput("DisabledText", ref disabledDemoText);

    ClayUI.EndDisabled();

    ClayUI.Space(16);
    ClayUI.Separator();
    ClayUI.Space(8);

    // Back to normal
    ClayUI.Label("After EndDisabled, widgets are interactive again:");
    ClayUI.Space(4);
    ClayUI.Button("This works!");
}

void PageRpgSkin()
{
    // Lazily create the skin on first use
    rpgSkin ??= RpgSkin.CreateSkin();

    ClayUI.Label("Custom skin using RPG GUI sprites (CC-BY 3.0, by Lamoot).");
    ClayUI.Label("Widgets below use image textures instead of solid-color rectangles.");
    ClayUI.Label("The same widgets, same API -- just with ClayUI.Skin set.");
    ClayUI.Space(12);

    // Apply the RPG skin for the rest of this page
    var previousSkin = ClayUI.Skin;
    ClayUI.Skin = rpgSkin;

    // Style overrides sized for the ornate RPG sprites
    var rpgButton = new ButtonStyle
    {
        Padding = Padding.Symmetric(52, 14),
        TextColor = Color.Rgba(230, 215, 180),
        FontSize = 16
    };
    var rpgCheckbox = new CheckboxStyle
    {
        BoxSize = 28,
        BoxCornerRadius = 0,
        TextColor = Color.Rgba(220, 210, 180),
        FontSize = 15
    };
    var rpgToggle = new ToggleStyle
    {
        TrackWidth = 60,
        TrackHeight = 30,
        KnobSize = 26,
        TextColor = Color.Rgba(220, 210, 180),
        FontSize = 15
    };
    var rpgSlider = new SliderStyle
    {
        TrackHeight = 20,
        TextColor = Color.Rgba(220, 210, 180),
        ValueTextColor = Color.Rgba(200, 190, 160),
        FontSize = 15
    };
    var rpgProgress = new ProgressBarStyle
    {
        Height = 20,
        CornerRadius = 0
    };
    var rpgPanel = new PanelStyle
    {
        TitleColor = Color.Rgba(80, 60, 30),
        TitleFontSize = 18,
        Padding = Padding.All(20),
        ChildGap = 8
    };
    var rpgLabel = new LabelStyle
    {
        TextColor = Color.Rgba(60, 45, 20),
        FontSize = 15
    };

    // --- Buttons ---
    ClayUI.Label("Buttons:");
    ClayUI.Space(4);
    ClayUI.BeginHorizontal(gap: 16);
    ClayUI.Button("Attack", rpgButton);
    ClayUI.Button("Defend", rpgButton);
    ClayUI.Button("Magic", rpgButton);
    ClayUI.EndHorizontal();
    ClayUI.Space(4);
    ClayUI.Button("Inventory", rpgButton);

    ClayUI.Space(16);
    ClayUI.Separator();
    ClayUI.Space(8);

    // --- Checkboxes ---
    ClayUI.Label("Checkboxes:");
    ClayUI.Space(4);
    ClayUI.Checkbox("Show minimap", ref rpgSkinCheckbox, rpgCheckbox);
    bool tempCb = true;
    ClayUI.Checkbox("Enable sound", ref tempCb, rpgCheckbox);

    ClayUI.Space(16);
    ClayUI.Separator();
    ClayUI.Space(8);

    // --- Radio Group ---
    ClayUI.Label("Radio Group:");
    ClayUI.Space(4);
    var rpgRadio = new RadioGroupStyle
    {
        CircleSize = 24,
        DotSize = 14,
        TextColor = Color.Rgba(220, 210, 180),
        LabelColor = Color.Rgba(180, 170, 140),
        FontSize = 15
    };
    ClayUI.RadioGroup("Class", ref rpgSkinRadio, rpgRadioOptions, rpgRadio);

    ClayUI.Space(16);
    ClayUI.Separator();
    ClayUI.Space(8);

    // --- Toggle ---
    ClayUI.Label("Toggle:");
    ClayUI.Space(4);
    ClayUI.Toggle("Fullscreen", ref rpgSkinToggle, rpgToggle);

    ClayUI.Space(16);
    ClayUI.Separator();
    ClayUI.Space(8);

    // --- Slider ---
    ClayUI.Label("Slider:");
    ClayUI.Space(4);
    ClayUI.Slider("Volume", ref rpgSkinSlider, 0, 1, rpgSlider);

    ClayUI.Space(16);
    ClayUI.Separator();
    ClayUI.Space(8);

    // --- Progress Bar ---
    ClayUI.Label("Progress Bar:");
    ClayUI.Space(4);
    rpgSkinProgress += Raylib.GetFrameTime() * 0.05f;
    if (rpgSkinProgress > 1f) rpgSkinProgress = 0f;
    ClayUI.ProgressBar(rpgSkinProgress, style: rpgProgress);

    ClayUI.Space(16);
    ClayUI.Separator();
    ClayUI.Space(8);

    // --- Panel ---
    ClayUI.Label("Panel:");
    ClayUI.Space(4);
    ClayUI.BeginPanel("Quest Log", rpgPanel);
    ClayUI.Label("- Defeat the dragon", rpgLabel);
    ClayUI.Label("- Find the lost sword", rpgLabel);
    ClayUI.Label("- Return to the village", rpgLabel);
    ClayUI.EndPanel();

    // Restore previous skin
    ClayUI.Skin = previousSkin;
}

// ============ Demo Windows ============

void PageDocking()
{
    ClayUI.Label("DockSpace provides an ImGui-style docking system where windows can be:");
    ClayUI.Space(4);
    ClayUI.Label("  - Arranged in a split layout with resizable splitters");
    ClayUI.Label("  - Shown as tabs when multiple windows share the same slot");
    ClayUI.Label("  - Undocked by dragging a tab out of the dock");
    ClayUI.Label("  - Re-docked by dragging a floating window over a dock zone");
    ClayUI.Space(12);

    ClayUI.Label("The dock space below demonstrates these features. Try:");
    ClayUI.Space(4);
    ClayUI.Label("  1. Click tabs to switch between docked panels");
    ClayUI.Label("  2. Drag a tab to undock it into a floating window");
    ClayUI.Label("  3. Drag the floating window back over a panel to re-dock it");
    ClayUI.Label("  4. Drag splitters between panels to resize them");

    ClayUI.Space(12);
    ClayUI.BeginHorizontal(gap: 8);
    if (ClayUI.Button("Reset Layout"))
    {
        ClayUI.ClearDockSpace("DemoDock");
        dockPanel1Open = true;
        dockPanel2Open = true;
        dockPanel3Open = true;
        dockPanel4Open = true;
        dockFloatingOpen = true;
    }
    if (ClayUI.Button("Show All")) { dockPanel1Open = true; dockPanel2Open = true; dockPanel3Open = true; dockPanel4Open = true; dockFloatingOpen = true; }
    ClayUI.EndHorizontal();

    ClayUI.Space(4);
    ClayUI.Label($"Panel 1: {(dockPanel1Open ? "open" : "closed")}, Panel 2: {(dockPanel2Open ? "open" : "closed")}, "
        + $"Panel 3: {(dockPanel3Open ? "open" : "closed")}, Panel 4: {(dockPanel4Open ? "open" : "closed")}");
    ClayUI.Label($"Floating: {(dockFloatingOpen ? "open" : "closed")}");

    ClayUI.Space(12);
    ClayUI.Separator();
    ClayUI.Space(8);

    ClayUI.Label("Usage — windows auto-dock when placed inside a dock space:");
    ClayUI.Space(4);
    ClayUI.Label("  ClayUI.BeginDockSpace(\"MyDock\");");
    ClayUI.Label("  if (ClayUI.BeginWindow(\"Panel A\", ref open)) { ... }");
    ClayUI.Label("  ClayUI.EndWindow();");
    ClayUI.Label("  if (ClayUI.BeginWindow(\"Panel B\", ref open)) { ... }");
    ClayUI.Label("  ClayUI.EndWindow();");
    ClayUI.Label("  ClayUI.EndDockSpace();");
}

void RenderDockingDemoWindows()
{
    if (ClayUI.BeginWindow("Dock Demo", ref dockFloatingOpen,
        defaultPosition: new Vector2(350, 100),
        defaultSize: new Vector2(700, 450),
        flags: WindowFlags.NoDocking | WindowFlags.NoScroll))
    {
        ClayUI.BeginDockSpace("DemoDock");

        // Panel 1: Properties
        if (ClayUI.BeginWindow("Properties", ref dockPanel1Open, flags: WindowFlags.NoCollapse))
        {
            ClayUI.Label("Entity Properties", new LabelStyle { FontSize = 14 });
            ClayUI.Space(8);
            ClayUI.Checkbox("Active", ref dockDemoCheckbox);
            ClayUI.Slider("Scale", ref dockDemoSlider, 0.1f, 3.0f);
            ClayUI.Space(8);
            ClayUI.Label($"Scale: {dockDemoSlider:F2}");
        }
        ClayUI.EndWindow();

        // Panel 2: Log
        if (ClayUI.BeginWindow("Log", ref dockPanel2Open, flags: WindowFlags.NoCollapse))
        {
            foreach (var msg in dockLogMessages)
                ClayUI.Label($"> {msg}", new LabelStyle { FontSize = 12 });
            ClayUI.Space(4);
            if (ClayUI.Button("Add Log"))
            {
                dockLogMessages = [..dockLogMessages, $"Event #{dockDemoCounter++}"];
            }
        }
        ClayUI.EndWindow();

        // Panel 3: Viewport (shares tab with Scene Graph)
        if (ClayUI.BeginWindow("Viewport", ref dockPanel3Open, flags: WindowFlags.NoCollapse))
        {
            ClayUI.Spacer();
            ClayUI.BeginHorizontal(alignment: ChildAlignment.Center, style: new LayoutStyle
            {
                Sizing = new Sizing(SizingAxis.Grow(), SizingAxis.Fit())
            });
            ClayUI.BeginVertical(gap: 4, alignment: ChildAlignment.Center);
            ClayUI.Label("3D Viewport", new LabelStyle { FontSize = 16 });
            ClayUI.Label("(drag this tab to undock)", new LabelStyle { FontSize = 12 });
            ClayUI.EndVertical();
            ClayUI.EndHorizontal();
            ClayUI.Spacer();
        }
        ClayUI.EndWindow();

        // Panel 4: Scene Graph (tab alongside Viewport)
        if (ClayUI.BeginWindow("Scene Graph", ref dockPanel4Open, flags: WindowFlags.NoCollapse))
        {
            ClayUI.Label("Scene Hierarchy", new LabelStyle { FontSize = 14 });
            ClayUI.Space(4);
            if (ClayUI.BeginTreeNode("Root"))
            {
                if (ClayUI.BeginTreeNode("Camera")) ClayUI.EndTreeNode();
                if (ClayUI.BeginTreeNode("Light")) ClayUI.EndTreeNode();
                if (ClayUI.BeginTreeNode("Player"))
                {
                    if (ClayUI.BeginTreeNode("Mesh")) ClayUI.EndTreeNode();
                    if (ClayUI.BeginTreeNode("Collider")) ClayUI.EndTreeNode();
                    ClayUI.EndTreeNode();
                }
                if (ClayUI.BeginTreeNode("Ground")) ClayUI.EndTreeNode();
                ClayUI.EndTreeNode();
            }
        }
        ClayUI.EndWindow();

        ClayUI.EndDockSpace();
    }
    ClayUI.EndWindow();
}

void RenderDemoWindows()
{
    // Standard draggable window
    if (ClayUI.BeginWindow("Demo Window", ref demoWindow1Open,
        defaultPosition: new Vector2(400, 150),
        defaultSize: new Vector2(300, 200)))
    {
        ClayUI.Label("This is a draggable window!");
        ClayUI.Space(8);
        ClayUI.Checkbox("A checkbox", ref windowCheckbox);
        ClayUI.Slider("Volume", ref windowSlider, 0, 1);
        ClayUI.Space(8);
        if (ClayUI.Button("Toggle checkbox"))
            windowCheckbox = !windowCheckbox;
    }
    ClayUI.EndWindow();

    // Second window
    if (ClayUI.BeginWindow("Settings", ref demoWindow2Open,
        defaultPosition: new Vector2(720, 200),
        defaultSize: new Vector2(250, 180)))
    {
        ClayUI.Toggle("Dark mode", ref toggleValue);
        ClayUI.Toggle("Sound", ref checkboxValue);
        ClayUI.Space(8);
        ClayUI.Label("Drag, resize, collapse, or close this window.");
    }
    ClayUI.EndWindow();

    // Locked window (NoMove | NoResize | NoCollapse)
    if (ClayUI.BeginWindow("Locked Window", ref lockedWindowOpen,
        defaultPosition: new Vector2(500, 350),
        defaultSize: new Vector2(250, 120),
        flags: WindowFlags.NoMove | WindowFlags.NoResize | WindowFlags.NoCollapse))
    {
        ClayUI.Label("This window cannot be moved,");
        ClayUI.Label("resized, or collapsed.");
        ClayUI.Label("It can only be closed.");
    }
    ClayUI.EndWindow();
}

// ============ Keyboard Input ============

void ForwardKeyboardInput()
{
    bool shift = Raylib.IsKeyDown(KeyboardKey.KEY_LEFT_SHIFT) || Raylib.IsKeyDown(KeyboardKey.KEY_RIGHT_SHIFT);
    bool ctrl = Raylib.IsKeyDown(KeyboardKey.KEY_LEFT_CONTROL) || Raylib.IsKeyDown(KeyboardKey.KEY_RIGHT_CONTROL);
    var mods = (shift ? Clay.Widgets.ClayKeyModifiers.Shift : 0)
             | (ctrl ? Clay.Widgets.ClayKeyModifiers.Ctrl : 0);

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
        // Modifiers — ClayUI tracks these internally for Shift+Wheel etc.
        (KeyboardKey.KEY_LEFT_SHIFT, Clay.Widgets.ClayKey.Shift),
        (KeyboardKey.KEY_RIGHT_SHIFT, Clay.Widgets.ClayKey.Shift),
        (KeyboardKey.KEY_LEFT_CONTROL, Clay.Widgets.ClayKey.Ctrl),
        (KeyboardKey.KEY_RIGHT_CONTROL, Clay.Widgets.ClayKey.Ctrl),
        (KeyboardKey.KEY_LEFT_ALT, Clay.Widgets.ClayKey.Alt),
        (KeyboardKey.KEY_RIGHT_ALT, Clay.Widgets.ClayKey.Alt),
        (KeyboardKey.KEY_LEFT_SUPER, Clay.Widgets.ClayKey.Super),
        (KeyboardKey.KEY_RIGHT_SUPER, Clay.Widgets.ClayKey.Super),
    ];

    foreach (var (rayKey, clayKey) in keyMap)
        if (Raylib.IsKeyDown(rayKey))
            ClayUI.KeyDown(clayKey, mods);

    int ch;
    while ((ch = Raylib.GetCharPressed()) != 0)
        ClayUI.CharInput((char)ch);
}
