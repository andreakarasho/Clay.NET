using Clay;
using Clay.GameEditor;
using ZeroElectric.Vinculum;
using Color = Clay.Color;
using Vector2 = System.Numerics.Vector2;

// Window configuration
const int InitialWidth = 1440;
const int InitialHeight = 900;
const string WindowTitle = "Raylib Game Engine Editor - Clay UI";

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
    maxElementCount: 16384
);
Clay.Clay.TextEditSetClipboard(new RaylibClipboard());

// Apply dark theme by default (game editors are always dark)
ClayUI.Style = ClayUIStyle.Dark;

// ============ Editor Color Palette ============

var colBg = Color.Rgba(30, 30, 30);
var colPanel = Color.Rgba(40, 40, 40);
var colPanelHeader = Color.Rgba(50, 50, 55);
var colMenuBar = Color.Rgba(45, 45, 48);
var colToolbar = Color.Rgba(48, 48, 52);
var colAccent = Color.Rgba(60, 140, 230);
var colAccentHover = Color.Rgba(80, 160, 250);
var colAccentDim = Color.Rgba(40, 100, 180);
var colText = Color.Rgba(210, 210, 210);
var colTextDim = Color.Rgba(140, 140, 140);
var colTextBright = Color.Rgba(240, 240, 240);
var colSelection = Color.Rgba(60, 140, 230, 60);
var colBorder = Color.Rgba(60, 60, 65);
var colViewport = Color.Rgba(25, 25, 28);
var colGrid = Color.Rgba(45, 45, 50);
var colWarning = Color.Rgba(230, 180, 50);
var colError = Color.Rgba(220, 60, 60);
var colSuccess = Color.Rgba(60, 190, 80);

// ============ Editor State ============

// Scene hierarchy
string[] sceneEntities =
[
    "Main Camera", "Directional Light", "Player", "Enemy_Goblin",
    "Enemy_Skeleton", "Ground Plane", "Tree_01", "Tree_02",
    "Rock_Large", "Particle_Fire", "UI_Canvas", "Audio_BGM"
];
bool[] entityExpanded = new bool[sceneEntities.Length];
int selectedEntity = 2; // Player selected by default

// Entity transform/properties
float[] entityPosX = [0, 0, 3.5f, -5, 8, 0, -3, 6, 2, 1, 0, 0];
float[] entityPosY = [10, 20, 0, 0, 0, -0.5f, 0, 0, 0, 0.5f, 0, 0];
float[] entityPosZ = [0, 0, -2, 4, -6, 0, 7, -4, 3, -1, 0, 0];
float[] entityRotX = new float[12];
float[] entityRotY = new float[12];
float[] entityRotZ = new float[12];
float[] entityScaleX = [1, 1, 1, 1, 1, 50, 1, 1, 2, 0.5f, 1, 1];
float[] entityScaleY = [1, 1, 1, 1, 1, 1, 1, 1, 2, 0.5f, 1, 1];
float[] entityScaleZ = [1, 1, 1, 1, 1, 50, 1, 1, 2, 0.5f, 1, 1];
bool[] entityActive = Enumerable.Repeat(true, 12).ToArray();

// Entity tags/layers
string[] entityTags = ["MainCamera", "Light", "Player", "Enemy", "Enemy", "Environment", "Environment", "Environment", "Environment", "FX", "UI", "Audio"];
string[] availableTags = ["Untagged", "MainCamera", "Player", "Enemy", "Environment", "FX", "UI", "Audio", "Light", "Trigger"];
int[] entityTagIndex = [1, 8, 2, 3, 3, 5, 5, 5, 5, 6, 7, 8];

// Components per entity
string[][] entityComponents =
[
    ["Camera", "Audio Listener"],
    ["Light"],
    ["Mesh Renderer", "Rigidbody", "Capsule Collider", "Player Controller", "Animator"],
    ["Mesh Renderer", "Rigidbody", "Sphere Collider", "AI Controller", "Animator"],
    ["Mesh Renderer", "Rigidbody", "Capsule Collider", "AI Controller", "Animator"],
    ["Mesh Renderer", "Box Collider"],
    ["Mesh Renderer", "LOD Group"],
    ["Mesh Renderer", "LOD Group"],
    ["Mesh Renderer", "Mesh Collider"],
    ["Particle System"],
    ["Canvas", "Canvas Scaler", "Graphic Raycaster"],
    ["Audio Source"]
];

// Asset browser
string[] assetFolders = ["Materials", "Models", "Textures", "Scripts", "Prefabs", "Scenes", "Audio", "Animations", "Shaders"];
int selectedAssetFolder = 0;
string[][] assetFiles =
[
    ["Default-Material.mat", "Player.mat", "Enemy.mat", "Ground.mat", "Tree_Bark.mat", "Tree_Leaves.mat", "Rock.mat", "Water.mat"],
    ["Player.fbx", "Goblin.fbx", "Skeleton.fbx", "Tree.fbx", "Rock.fbx", "Ground.fbx"],
    ["T_Player_D.png", "T_Player_N.png", "T_Goblin_D.png", "T_Ground_D.png", "T_Tree_D.png", "T_Rock_D.png", "T_Noise.png"],
    ["PlayerController.cs", "AIController.cs", "CameraFollow.cs", "GameManager.cs", "UIManager.cs", "HealthSystem.cs"],
    ["Player.prefab", "Enemy_Goblin.prefab", "Enemy_Skeleton.prefab", "Tree_01.prefab", "Rock.prefab"],
    ["MainScene.scene", "MenuScene.scene", "TestScene.scene"],
    ["BGM_Forest.ogg", "SFX_Sword.wav", "SFX_Hit.wav", "SFX_Footstep.wav", "AMB_Wind.ogg"],
    ["Player_Idle.anim", "Player_Run.anim", "Player_Attack.anim", "Goblin_Walk.anim", "Goblin_Attack.anim"],
    ["Standard.shader", "Unlit.shader", "Water.shader", "Particle.shader", "Outline.shader"]
];
int selectedAssetFile = -1;

// Console log
string[] consoleMessages =
[
    "[INFO] Scene loaded: MainScene.scene",
    "[INFO] Compiling scripts... done (0.3s)",
    "[WARN] Texture 'T_Noise.png' is not power-of-two, consider resizing",
    "[INFO] Player prefab updated",
    "[ERROR] NullReferenceException in AIController.cs:42 - target is null",
    "[INFO] Build target: Windows x64",
    "[WARN] Unused variable 'tempHealth' in HealthSystem.cs:18",
    "[INFO] Asset import complete: Rock.fbx",
    "[INFO] Lightmap baking started...",
    "[ERROR] Shader compilation failed: Water.shader line 23 - undeclared identifier 'waveOffset'",
    "[INFO] Auto-save completed",
    "[WARN] Physics timestep exceeded 0.02s, simulation may be inaccurate",
];
bool showInfoLogs = true;
bool showWarningLogs = true;
bool showErrorLogs = true;

// Play mode
bool isPlaying = false;
bool isPaused = false;
float playTime = 0;

// Editor panels
bool showConsole = true;
bool showAssetBrowser = true;
bool debugWindowOpen = false;

// Windows
bool aboutWindowOpen = false;
bool buildWindowOpen = false;
bool preferencesWindowOpen = false;

// Build settings
int buildPlatformIndex = 0;
string[] buildPlatforms = ["Windows x64", "Linux x64", "macOS (Universal)", "WebGL", "Android", "iOS"];
bool buildDevelopment = true;
bool buildScriptDebugging = false;

// Preferences
float editorGridSize = 1.0f;
bool editorSnap = true;
bool editorShowGrid = true;
bool editorShowGizmos = true;
float editorCameraSpeed = 5.0f;

// Viewport gizmo
int gizmoMode = 0; // 0=Translate, 1=Rotate, 2=Scale
bool gizmoLocal = false;

// Dockable window open states
bool hierarchyOpen = true;
bool viewportOpen = true;
bool inspectorOpen = true;
bool assetBrowserOpen = true;
bool consoleOpen = true;

// Main loop
while (!Raylib.WindowShouldClose())
{
    var mousePos = Raylib.GetMousePosition();
    var mouseWheel = Raylib.GetMouseWheelMoveV();
    bool mouseDown = Raylib.IsMouseButtonDown(0);
    var scrollDelta = new Vector2(mouseWheel.X, mouseWheel.Y);
    float deltaTime = Raylib.GetFrameTime();

    if (isPlaying && !isPaused)
        playTime += deltaTime;

    ClayUI.BeginFrame(new Dimensions(Raylib.GetScreenWidth(), Raylib.GetScreenHeight()),
        mouseDown, new Vector2(mousePos.X, mousePos.Y), scrollDelta, deltaTime);

    if (Raylib.IsKeyPressed(KeyboardKey.KEY_F12))
        ClayUI.ToggleDebugWindow();

    ForwardKeyboardInput();

    // ===== Root Layout =====
    ClayUI.BeginVertical(gap: 0, style: new LayoutStyle
    {
        Sizing = Sizing.Fill(),
        BackgroundColor = colBg
    });

    RenderMenuBar();
    RenderToolbar();

    // ===== Dock Space =====
    ClayUI.BeginDockSpace("EditorDock", setup: dock =>
    {
        var (main, bottom) = dock.Split(DockSplitDirection.Vertical, 0.7f);
        var (left, centerRight) = dock.Split(main, DockSplitDirection.Horizontal, 0.16f);
        var (center, right) = dock.Split(centerRight, DockSplitDirection.Horizontal, 0.72f);
        var (bottomLeft, bottomRight) = dock.Split(bottom, DockSplitDirection.Horizontal, 0.55f);
        dock.Window(left, "Hierarchy");
        dock.Window(center, "Viewport");
        dock.Window(right, "Inspector");
        dock.Window(bottomLeft, "Asset Browser");
        dock.Window(bottomRight, "Console");
    });

    // Dockable panels — each renders as a docked window
    if (ClayUI.BeginWindow("Hierarchy", ref hierarchyOpen, flags: WindowFlags.NoCollapse))
    {
        RenderHierarchyContent();
    }
    ClayUI.EndWindow();

    if (ClayUI.BeginWindow("Viewport", ref viewportOpen, flags: WindowFlags.NoCollapse | WindowFlags.NoScroll))
    {
        RenderViewportContent();
    }
    ClayUI.EndWindow();

    if (ClayUI.BeginWindow("Inspector", ref inspectorOpen, flags: WindowFlags.NoCollapse))
    {
        RenderInspectorContent();
    }
    ClayUI.EndWindow();

    if (ClayUI.BeginWindow("Asset Browser", ref assetBrowserOpen, flags: WindowFlags.NoCollapse))
    {
        RenderAssetBrowserContent();
    }
    ClayUI.EndWindow();

    if (ClayUI.BeginWindow("Console", ref consoleOpen, flags: WindowFlags.NoCollapse))
    {
        RenderConsoleContent();
    }
    ClayUI.EndWindow();

    ClayUI.EndDockSpace();

    // ===== Status Bar =====
    RenderStatusBar();

    // ===== Floating Windows =====
    RenderFloatingWindows();

    if (debugWindowOpen)
        ClayUI.ShowDebugWindow();

    ClayUI.EndVertical();

    // Update cursor for dock splitter hover/drag
    Raylib.SetMouseCursor(MouseCursor.MOUSE_CURSOR_DEFAULT);

    var commands = ClayUI.EndFrame();

    Raylib.BeginDrawing();
    Raylib.ClearBackground(Raylib.BLACK);
    renderer.Render(commands);
    Raylib.EndDrawing();
}

Clay.Clay.Shutdown();
Raylib.CloseWindow();

// ============ Menu Bar ============

void RenderMenuBar()
{
    ClayUI.BeginHorizontal(gap: 0, alignment: ChildAlignment.CenterLeft, style: new LayoutStyle
    {
        Sizing = new Sizing(SizingAxis.Grow(), SizingAxis.Fixed(28)),
        Padding = Padding.Horizontal(8),
        BackgroundColor = colMenuBar,
        Border = new BorderConfig { Width = new BorderWidth(0, 0, 0, 1), Color = colBorder }
    });

    var menuBtnStyle = new ButtonStyle
    {
        BackgroundColor = Color.Rgba(0, 0, 0, 0),
        HoverColor = Color.Rgba(255, 255, 255, 20),
        PressedColor = Color.Rgba(255, 255, 255, 30),
        TextColor = colText,
        Padding = Padding.Symmetric(10, 4),
        CornerRadius = CornerRadius.All(3),
        FontSize = 13
    };

    if (ClayUI.Button("File##menu", menuBtnStyle)) ClayUI.OpenPopup("FileMenu");
    if (ClayUI.Button("Edit##menu", menuBtnStyle)) ClayUI.OpenPopup("EditMenu");
    if (ClayUI.Button("Assets##menu", menuBtnStyle)) ClayUI.OpenPopup("AssetsMenu");
    if (ClayUI.Button("GameObject##menu", menuBtnStyle)) ClayUI.OpenPopup("GameObjectMenu");
    if (ClayUI.Button("Component##menu", menuBtnStyle)) ClayUI.OpenPopup("ComponentMenu");
    if (ClayUI.Button("Window##menu", menuBtnStyle)) ClayUI.OpenPopup("WindowMenu");
    if (ClayUI.Button("Help##menu", menuBtnStyle)) ClayUI.OpenPopup("HelpMenu");

    ClayUI.Spacer();

    // Layout selector
    ClayUI.Label("Layout: Default", new LabelStyle { TextColor = colTextDim, FontSize = 12 });

    // File menu
    if (ClayUI.BeginPopup("FileMenu"))
    {
        if (ClayUI.MenuItem("New Scene")) Console.WriteLine("File > New Scene");
        if (ClayUI.MenuItem("Open Scene...")) Console.WriteLine("File > Open Scene");
        if (ClayUI.MenuItem("Save Scene")) Console.WriteLine("File > Save Scene");
        if (ClayUI.MenuItem("Save Scene As...")) Console.WriteLine("File > Save Scene As");
        ClayUI.MenuSeparator();
        if (ClayUI.MenuItem("New Project...")) Console.WriteLine("File > New Project");
        if (ClayUI.MenuItem("Open Project...")) Console.WriteLine("File > Open Project");
        ClayUI.MenuSeparator();
        if (ClayUI.MenuItem("Build Settings...")) buildWindowOpen = true;
        if (ClayUI.MenuItem("Build & Run")) Console.WriteLine("File > Build & Run");
        ClayUI.MenuSeparator();
        if (ClayUI.MenuItem("Exit")) Environment.Exit(0);
        ClayUI.EndPopup();
    }

    // Edit menu
    if (ClayUI.BeginPopup("EditMenu"))
    {
        if (ClayUI.MenuItem("Undo")) Console.WriteLine("Edit > Undo");
        if (ClayUI.MenuItem("Redo")) Console.WriteLine("Edit > Redo");
        ClayUI.MenuSeparator();
        if (ClayUI.MenuItem("Cut")) Console.WriteLine("Edit > Cut");
        if (ClayUI.MenuItem("Copy")) Console.WriteLine("Edit > Copy");
        if (ClayUI.MenuItem("Paste")) Console.WriteLine("Edit > Paste");
        if (ClayUI.MenuItem("Duplicate")) Console.WriteLine("Edit > Duplicate");
        if (ClayUI.MenuItem("Delete")) Console.WriteLine("Edit > Delete");
        ClayUI.MenuSeparator();
        if (ClayUI.MenuItem("Select All")) Console.WriteLine("Edit > Select All");
        ClayUI.MenuSeparator();
        if (ClayUI.MenuItem("Preferences...")) preferencesWindowOpen = true;
        ClayUI.EndPopup();
    }

    // Assets menu
    if (ClayUI.BeginPopup("AssetsMenu"))
    {
        if (ClayUI.BeginMenu("Create"))
        {
            if (ClayUI.MenuItem("Folder")) Console.WriteLine("Assets > Create > Folder");
            if (ClayUI.MenuItem("C# Script")) Console.WriteLine("Assets > Create > C# Script");
            if (ClayUI.MenuItem("Shader")) Console.WriteLine("Assets > Create > Shader");
            if (ClayUI.MenuItem("Material")) Console.WriteLine("Assets > Create > Material");
            ClayUI.EndMenu();
        }
        if (ClayUI.MenuItem("Import Package...")) Console.WriteLine("Assets > Import Package");
        if (ClayUI.MenuItem("Export Package...")) Console.WriteLine("Assets > Export Package");
        ClayUI.MenuSeparator();
        if (ClayUI.MenuItem("Refresh")) Console.WriteLine("Assets > Refresh");
        if (ClayUI.MenuItem("Reimport All")) Console.WriteLine("Assets > Reimport All");
        ClayUI.EndPopup();
    }

    // GameObject menu
    if (ClayUI.BeginPopup("GameObjectMenu"))
    {
        if (ClayUI.MenuItem("Create Empty")) Console.WriteLine("GameObject > Create Empty");
        if (ClayUI.BeginMenu("3D Object"))
        {
            if (ClayUI.MenuItem("Cube")) Console.WriteLine("3D > Cube");
            if (ClayUI.MenuItem("Sphere")) Console.WriteLine("3D > Sphere");
            if (ClayUI.MenuItem("Capsule")) Console.WriteLine("3D > Capsule");
            if (ClayUI.MenuItem("Cylinder")) Console.WriteLine("3D > Cylinder");
            if (ClayUI.MenuItem("Plane")) Console.WriteLine("3D > Plane");
            ClayUI.EndMenu();
        }
        if (ClayUI.BeginMenu("Light"))
        {
            if (ClayUI.MenuItem("Directional Light")) Console.WriteLine("Light > Directional");
            if (ClayUI.MenuItem("Point Light")) Console.WriteLine("Light > Point");
            if (ClayUI.MenuItem("Spot Light")) Console.WriteLine("Light > Spot");
            ClayUI.EndMenu();
        }
        if (ClayUI.BeginMenu("Effects"))
        {
            if (ClayUI.MenuItem("Particle System")) Console.WriteLine("Effects > Particle System");
            if (ClayUI.MenuItem("Trail")) Console.WriteLine("Effects > Trail");
            ClayUI.EndMenu();
        }
        if (ClayUI.MenuItem("Camera")) Console.WriteLine("GameObject > Camera");
        ClayUI.EndPopup();
    }

    // Component menu
    if (ClayUI.BeginPopup("ComponentMenu"))
    {
        if (ClayUI.BeginMenu("Physics"))
        {
            if (ClayUI.MenuItem("Rigidbody")) Console.WriteLine("Component > Rigidbody");
            if (ClayUI.MenuItem("Box Collider")) Console.WriteLine("Component > Box Collider");
            if (ClayUI.MenuItem("Sphere Collider")) Console.WriteLine("Component > Sphere Collider");
            if (ClayUI.MenuItem("Capsule Collider")) Console.WriteLine("Component > Capsule Collider");
            ClayUI.EndMenu();
        }
        if (ClayUI.BeginMenu("Rendering"))
        {
            if (ClayUI.MenuItem("Mesh Renderer")) Console.WriteLine("Component > Mesh Renderer");
            if (ClayUI.MenuItem("Light")) Console.WriteLine("Component > Light");
            if (ClayUI.MenuItem("Camera")) Console.WriteLine("Component > Camera");
            ClayUI.EndMenu();
        }
        if (ClayUI.BeginMenu("Audio"))
        {
            if (ClayUI.MenuItem("Audio Source")) Console.WriteLine("Component > Audio Source");
            if (ClayUI.MenuItem("Audio Listener")) Console.WriteLine("Component > Audio Listener");
            ClayUI.EndMenu();
        }
        if (ClayUI.MenuItem("New Script...")) Console.WriteLine("Component > New Script");
        ClayUI.EndPopup();
    }

    // Window menu
    if (ClayUI.BeginPopup("WindowMenu"))
    {
        if (ClayUI.MenuItem(showConsole ? "Console  [*]" : "Console")) showConsole = !showConsole;
        if (ClayUI.MenuItem(showAssetBrowser ? "Asset Browser  [*]" : "Asset Browser")) showAssetBrowser = !showAssetBrowser;
        ClayUI.MenuSeparator();
        if (ClayUI.MenuItem(debugWindowOpen ? "Debug Inspector  [*]" : "Debug Inspector (F12)")) debugWindowOpen = !debugWindowOpen;
        ClayUI.EndPopup();
    }

    // Help menu
    if (ClayUI.BeginPopup("HelpMenu"))
    {
        if (ClayUI.MenuItem("Documentation")) Console.WriteLine("Help > Docs");
        if (ClayUI.MenuItem("API Reference")) Console.WriteLine("Help > API");
        ClayUI.MenuSeparator();
        if (ClayUI.MenuItem("About")) aboutWindowOpen = true;
        ClayUI.EndPopup();
    }

    ClayUI.EndHorizontal();
}

// ============ Toolbar ============

void RenderToolbar()
{
    ClayUI.BeginHorizontal(gap: 6, alignment: ChildAlignment.CenterLeft, style: new LayoutStyle
    {
        Sizing = new Sizing(SizingAxis.Grow(), SizingAxis.Fixed(36)),
        Padding = Padding.Symmetric(8, 4),
        BackgroundColor = colToolbar,
        Border = new BorderConfig { Width = new BorderWidth(0, 0, 0, 1), Color = colBorder }
    });

    var toolBtnStyle = new ButtonStyle
    {
        BackgroundColor = Color.Rgba(60, 60, 65),
        HoverColor = Color.Rgba(75, 75, 80),
        PressedColor = Color.Rgba(50, 50, 55),
        TextColor = colText,
        Padding = Padding.Symmetric(10, 4),
        CornerRadius = CornerRadius.All(3),
        FontSize = 12
    };

    var activeBtnStyle = new ButtonStyle
    {
        BackgroundColor = colAccent,
        HoverColor = colAccentHover,
        PressedColor = colAccentDim,
        TextColor = Color.White,
        Padding = Padding.Symmetric(10, 4),
        CornerRadius = CornerRadius.All(3),
        FontSize = 12
    };

    // Transform gizmo tools
    if (ClayUI.Button("Move##gizmo", gizmoMode == 0 ? activeBtnStyle : toolBtnStyle)) gizmoMode = 0;
    if (ClayUI.Button("Rotate##gizmo", gizmoMode == 1 ? activeBtnStyle : toolBtnStyle)) gizmoMode = 1;
    if (ClayUI.Button("Scale##gizmo", gizmoMode == 2 ? activeBtnStyle : toolBtnStyle)) gizmoMode = 2;

    ClayUI.Separator(colBorder, 20);

    if (ClayUI.Button(gizmoLocal ? "Local" : "Global", toolBtnStyle)) gizmoLocal = !gizmoLocal;

    if (ClayUI.Button(editorSnap ? "Snap: ON" : "Snap: OFF", editorSnap ? activeBtnStyle : toolBtnStyle))
        editorSnap = !editorSnap;

    ClayUI.Spacer();

    // Play/Pause/Stop controls (centered)
    var playBtnStyle = new ButtonStyle
    {
        BackgroundColor = isPlaying ? Color.Rgba(40, 160, 60) : Color.Rgba(60, 60, 65),
        HoverColor = isPlaying ? Color.Rgba(50, 180, 70) : Color.Rgba(75, 75, 80),
        PressedColor = isPlaying ? Color.Rgba(30, 140, 50) : Color.Rgba(50, 50, 55),
        TextColor = Color.White,
        Padding = Padding.Symmetric(16, 4),
        CornerRadius = CornerRadius.All(3),
        FontSize = 13
    };

    if (ClayUI.Button(isPlaying ? "Stop" : "Play", playBtnStyle))
    {
        if (isPlaying) { isPlaying = false; isPaused = false; playTime = 0; }
        else { isPlaying = true; isPaused = false; }
    }

    if (isPlaying)
    {
        var pauseBtnStyle = new ButtonStyle
        {
            BackgroundColor = isPaused ? colWarning : Color.Rgba(60, 60, 65),
            HoverColor = isPaused ? Color.Rgba(240, 190, 60) : Color.Rgba(75, 75, 80),
            PressedColor = Color.Rgba(50, 50, 55),
            TextColor = Color.White,
            Padding = Padding.Symmetric(12, 4),
            CornerRadius = CornerRadius.All(3),
            FontSize = 13
        };
        if (ClayUI.Button(isPaused ? "Resume" : "Pause", pauseBtnStyle))
            isPaused = !isPaused;

        if (ClayUI.Button("Step", toolBtnStyle))
            Console.WriteLine("Step frame");
    }

    ClayUI.Spacer();

    // Right side indicators
    if (isPlaying)
    {
        ClayUI.Label($"Time: {playTime:F1}s", new LabelStyle { TextColor = colSuccess, FontSize = 12 });
    }

    ClayUI.EndHorizontal();
}

// ============ Hierarchy Panel ============

void RenderHierarchyContent()
{
    // Scene label
    ClayUI.BeginHorizontal(style: new LayoutStyle
    {
        Padding = Padding.Symmetric(8, 4),
        BackgroundColor = Color.Rgba(55, 55, 60)
    });
    ClayUI.Label("MainScene", new LabelStyle { TextColor = colAccent, FontSize = 12 });
    ClayUI.EndHorizontal();

    // Entity list
    for (int i = 0; i < sceneEntities.Length; i++)
    {
        bool isSelected = selectedEntity == i;
        string icon = entityActive[i] ? "" : "(off) ";

        var itemStyle = new ButtonStyle
        {
            BackgroundColor = isSelected ? colSelection : Color.Rgba(0, 0, 0, 0),
            HoverColor = isSelected ? colSelection : Color.Rgba(255, 255, 255, 10),
            PressedColor = colSelection,
            TextColor = entityActive[i] ? (isSelected ? colTextBright : colText) : colTextDim,
            Padding = Padding.Symmetric(8, 3),
            CornerRadius = CornerRadius.All(2),
            FontSize = 13
        };

        if (ClayUI.Button($"{icon}{sceneEntities[i]}##entity_{i}", itemStyle))
            selectedEntity = i;

        // Context menu per entity
        if (ClayUI.BeginContextMenu($"EntityCtx_{i}", ClayUI.Id($"{icon}{sceneEntities[i]}##entity_{i}")))
        {
            if (ClayUI.MenuItem("Duplicate")) Console.WriteLine($"Duplicate {sceneEntities[i]}");
            if (ClayUI.MenuItem("Delete")) Console.WriteLine($"Delete {sceneEntities[i]}");
            ClayUI.MenuSeparator();
            if (ClayUI.MenuItem(entityActive[i] ? "Deactivate" : "Activate"))
                entityActive[i] = !entityActive[i];
            if (ClayUI.MenuItem("Rename")) Console.WriteLine($"Rename {sceneEntities[i]}");
            ClayUI.EndContextMenu();
        }
    }
}

// ============ Viewport ============

void RenderViewportContent()
{
    // Viewport content area (simulated 3D view)
    ClayUI.Spacer();

    // Center info overlay
    ClayUI.BeginHorizontal(alignment: ChildAlignment.Center, style: new LayoutStyle
    {
        Sizing = new Sizing(SizingAxis.Grow(), SizingAxis.Fit())
    });
    ClayUI.BeginVertical(gap: 8, alignment: ChildAlignment.Center, style: new LayoutStyle
    {
        Padding = Padding.All(20),
        BackgroundColor = Color.Rgba(0, 0, 0, 80),
        CornerRadius = CornerRadius.All(8),
        ClipContent = true
    });

    if (isPlaying)
    {
        ClayUI.Heading("PLAY MODE", new HeadingStyle { TextColor = colSuccess, FontSize = 18 });
        ClayUI.Label($"Scene: MainScene  |  Time: {playTime:F2}s  |  FPS: {Raylib.GetFPS()}", new LabelStyle { TextColor = colText, FontSize = 13 });
        if (isPaused)
            ClayUI.Label("PAUSED", new LabelStyle { TextColor = colWarning, FontSize = 14 });
    }
    else
    {
        ClayUI.Label("3D Scene Viewport", new LabelStyle { TextColor = colTextDim, FontSize = 14 });
        ClayUI.Label($"Selected: {sceneEntities[selectedEntity]}  |  FPS: {Raylib.GetFPS()}", new LabelStyle { TextColor = colText, FontSize = 13 });
        ClayUI.Label($"Tool: {(gizmoMode == 0 ? "Move" : gizmoMode == 1 ? "Rotate" : "Scale")} ({(gizmoLocal ? "Local" : "Global")})", new LabelStyle { TextColor = colTextDim, FontSize = 12 });
    }

    ClayUI.EndVertical();
    ClayUI.EndHorizontal();

    ClayUI.Spacer();

    // Bottom-left camera info
    ClayUI.BeginHorizontal(style: new LayoutStyle
    {
        Padding = Padding.Symmetric(8, 4)
    });
    ClayUI.Label("Persp  |  Free Camera  |  FOV: 60", new LabelStyle { TextColor = Color.Rgba(100, 100, 110), FontSize = 11 });
    ClayUI.EndHorizontal();
}

// ============ Inspector Panel ============

void RenderInspectorContent()
{
    if (selectedEntity >= 0 && selectedEntity < sceneEntities.Length)
    {
        ClayUI.BeginVertical(gap: 8, style: new LayoutStyle
        {
            Padding = Padding.All(8),
            Sizing = new Sizing(SizingAxis.Grow(), SizingAxis.Fit())
        });

        // Entity name and active toggle
        ClayUI.BeginHorizontal(gap: 8, alignment: ChildAlignment.CenterLeft);
        ClayUI.Checkbox($"##active_{selectedEntity}", ref entityActive[selectedEntity]);
        ClayUI.Heading(sceneEntities[selectedEntity], new HeadingStyle { TextColor = colTextBright, FontSize = 15 });
        ClayUI.EndHorizontal();

        // Tag
        ClayUI.BeginHorizontal(gap: 8, alignment: ChildAlignment.CenterLeft);
        ClayUI.Label("Tag:", new LabelStyle { TextColor = colTextDim, FontSize = 12 });
        ClayUI.Combo($"##tag_{selectedEntity}", ref entityTagIndex[selectedEntity], availableTags);
        ClayUI.EndHorizontal();

        ClayUI.Space(4);
        ClayUI.Separator(colBorder);
        ClayUI.Space(4);

        // Transform component
        RenderTransformComponent();

        ClayUI.Space(4);
        ClayUI.Separator(colBorder);
        ClayUI.Space(4);

        // Other components
        var components = entityComponents[selectedEntity];
        for (int c = 0; c < components.Length; c++)
        {
            RenderComponentHeader(components[c], c);
        }

        // Add Component button
        ClayUI.Space(8);
        var addCompStyle = new ButtonStyle
        {
            BackgroundColor = Color.Rgba(55, 55, 60),
            HoverColor = Color.Rgba(65, 65, 70),
            PressedColor = Color.Rgba(50, 50, 55),
            TextColor = colText,
            Padding = Padding.Symmetric(0, 6),
            CornerRadius = CornerRadius.All(4),
            FontSize = 13
        };
        if (ClayUI.Button("Add Component##addcomp", addCompStyle))
            ClayUI.OpenPopup("AddComponentPopup");

        if (ClayUI.BeginPopup("AddComponentPopup"))
        {
            if (ClayUI.BeginMenu("Physics"))
            {
                ClayUI.MenuItem("Rigidbody");
                ClayUI.MenuItem("Box Collider");
                ClayUI.MenuItem("Sphere Collider");
                ClayUI.EndMenu();
            }
            if (ClayUI.BeginMenu("Rendering"))
            {
                ClayUI.MenuItem("Mesh Renderer");
                ClayUI.MenuItem("Mesh Filter");
                ClayUI.MenuItem("Light");
                ClayUI.EndMenu();
            }
            if (ClayUI.BeginMenu("Audio"))
            {
                ClayUI.MenuItem("Audio Source");
                ClayUI.MenuItem("Audio Listener");
                ClayUI.EndMenu();
            }
            if (ClayUI.MenuItem("New Script..."))
                Console.WriteLine("Add new script");
            ClayUI.EndPopup();
        }

        ClayUI.EndVertical();
    }
}

void RenderTransformComponent()
{
    if (ClayUI.BeginTreeNode("Transform##comp"))
    {
        var labelStyle = new LabelStyle { TextColor = colTextDim, FontSize = 12 };

        ClayUI.Label("Position", labelStyle);
        ClayUI.BeginHorizontal(gap: 4);
        RenderFloatField("X##posX", ref entityPosX[selectedEntity], Color.Rgba(200, 60, 60));
        RenderFloatField("Y##posY", ref entityPosY[selectedEntity], Color.Rgba(60, 180, 60));
        RenderFloatField("Z##posZ", ref entityPosZ[selectedEntity], Color.Rgba(60, 100, 220));
        ClayUI.EndHorizontal();

        ClayUI.Space(4);
        ClayUI.Label("Rotation", labelStyle);
        ClayUI.BeginHorizontal(gap: 4);
        RenderFloatField("X##rotX", ref entityRotX[selectedEntity], Color.Rgba(200, 60, 60));
        RenderFloatField("Y##rotY", ref entityRotY[selectedEntity], Color.Rgba(60, 180, 60));
        RenderFloatField("Z##rotZ", ref entityRotZ[selectedEntity], Color.Rgba(60, 100, 220));
        ClayUI.EndHorizontal();

        ClayUI.Space(4);
        ClayUI.Label("Scale", labelStyle);
        ClayUI.BeginHorizontal(gap: 4);
        RenderFloatField("X##sclX", ref entityScaleX[selectedEntity], Color.Rgba(200, 60, 60));
        RenderFloatField("Y##sclY", ref entityScaleY[selectedEntity], Color.Rgba(60, 180, 60));
        RenderFloatField("Z##sclZ", ref entityScaleZ[selectedEntity], Color.Rgba(60, 100, 220));
        ClayUI.EndHorizontal();

        ClayUI.EndTreeNode();
    }
}

void RenderFloatField(string label, ref float value, Color accentColor)
{
    ClayUI.BeginHorizontal(gap: 2, alignment: ChildAlignment.CenterLeft, style: new LayoutStyle
    {
        Sizing = new Sizing(SizingAxis.Grow(), SizingAxis.Fit())
    });

    // Axis color indicator
    ClayUI.BeginVertical(style: new LayoutStyle
    {
        Sizing = new Sizing(SizingAxis.Fixed(3), SizingAxis.Fixed(18)),
        BackgroundColor = accentColor,
        CornerRadius = CornerRadius.All(1)
    });
    ClayUI.EndVertical();

    ClayUI.Label(label.Split('#')[0], new LabelStyle { TextColor = colTextDim, FontSize = 11 });
    ClayUI.Slider($"##{label}_slider", ref value, -100, 100, new SliderStyle
    {
        TrackColor = Color.Rgba(50, 50, 55),
        FillColor = accentColor,
        TrackHeight = 16,
        TextColor = Color.Rgba(0, 0, 0, 0), // hide label
        ValueTextColor = colText,
        FontSize = 11
    });

    ClayUI.EndHorizontal();
}

void RenderComponentHeader(string componentName, int index)
{
    if (ClayUI.BeginTreeNode($"{componentName}##comp_{selectedEntity}_{index}"))
    {
        ClayUI.Label($"({componentName} properties)", new LabelStyle { TextColor = colTextDim, FontSize = 12 });

        // Show some sample properties based on component type
        if (componentName == "Mesh Renderer")
        {
            ClayUI.Label("Material: Default-Material", new LabelStyle { TextColor = colText, FontSize = 12 });
            ClayUI.Label("Cast Shadows: On", new LabelStyle { TextColor = colText, FontSize = 12 });
            ClayUI.Label("Receive Shadows: On", new LabelStyle { TextColor = colText, FontSize = 12 });
        }
        else if (componentName == "Rigidbody")
        {
            ClayUI.Label("Mass: 1.0", new LabelStyle { TextColor = colText, FontSize = 12 });
            ClayUI.Label("Drag: 0.0", new LabelStyle { TextColor = colText, FontSize = 12 });
            ClayUI.Label("Use Gravity: Yes", new LabelStyle { TextColor = colText, FontSize = 12 });
        }
        else if (componentName == "Light")
        {
            ClayUI.Label("Type: Directional", new LabelStyle { TextColor = colText, FontSize = 12 });
            ClayUI.Label("Intensity: 1.0", new LabelStyle { TextColor = colText, FontSize = 12 });
            ClayUI.Label("Color: White", new LabelStyle { TextColor = colText, FontSize = 12 });
        }
        else if (componentName == "Camera")
        {
            ClayUI.Label("FOV: 60", new LabelStyle { TextColor = colText, FontSize = 12 });
            ClayUI.Label("Near: 0.1", new LabelStyle { TextColor = colText, FontSize = 12 });
            ClayUI.Label("Far: 1000", new LabelStyle { TextColor = colText, FontSize = 12 });
        }
        else if (componentName.Contains("Collider"))
        {
            ClayUI.Label("Is Trigger: No", new LabelStyle { TextColor = colText, FontSize = 12 });
            ClayUI.Label("Center: (0, 0, 0)", new LabelStyle { TextColor = colText, FontSize = 12 });
        }
        else if (componentName == "Particle System")
        {
            ClayUI.Label("Max Particles: 1000", new LabelStyle { TextColor = colText, FontSize = 12 });
            ClayUI.Label("Duration: 5.0s", new LabelStyle { TextColor = colText, FontSize = 12 });
            ClayUI.Label("Looping: Yes", new LabelStyle { TextColor = colText, FontSize = 12 });
        }
        else if (componentName == "Audio Source")
        {
            ClayUI.Label("Clip: BGM_Forest.ogg", new LabelStyle { TextColor = colText, FontSize = 12 });
            ClayUI.Label("Volume: 1.0", new LabelStyle { TextColor = colText, FontSize = 12 });
            ClayUI.Label("Loop: Yes", new LabelStyle { TextColor = colText, FontSize = 12 });
        }
        else if (componentName == "Animator")
        {
            ClayUI.Label("Controller: Default", new LabelStyle { TextColor = colText, FontSize = 12 });
            ClayUI.Label("Apply Root Motion: No", new LabelStyle { TextColor = colText, FontSize = 12 });
        }

        ClayUI.EndTreeNode();
    }
}

// ============ Bottom Area (Console + Asset Browser) ============

// RenderBottomArea removed — now handled by dock space

void RenderAssetBrowserContent()
{
    // Content: folder tree + file list
    ClayUI.BeginHorizontal(gap: 0, style: new LayoutStyle
    {
        Sizing = Sizing.Fill()
    });

    // Folder tree
    ClayUI.BeginScrollArea("AssetFolders", style: new ScrollAreaStyle
    {
        BackgroundColor = Color.Rgba(38, 38, 42),
        Padding = Padding.All(4)
    });

    for (int i = 0; i < assetFolders.Length; i++)
    {
        bool isSel = selectedAssetFolder == i;
        var folderStyle = new ButtonStyle
        {
            BackgroundColor = isSel ? colSelection : Color.Rgba(0, 0, 0, 0),
            HoverColor = Color.Rgba(255, 255, 255, 10),
            PressedColor = colSelection,
            TextColor = isSel ? colAccent : colText,
            Padding = Padding.Symmetric(6, 2),
            CornerRadius = CornerRadius.All(2),
            FontSize = 12
        };
        if (ClayUI.Button($"{assetFolders[i]}##folder_{i}", folderStyle))
        {
            selectedAssetFolder = i;
            selectedAssetFile = -1;
        }
    }

    ClayUI.EndScrollArea();

    // File list
    ClayUI.BeginScrollArea("AssetFiles", style: new ScrollAreaStyle
    {
        BackgroundColor = colPanel,
        Padding = Padding.All(4)
    });

    var files = assetFiles[selectedAssetFolder];
    for (int i = 0; i < files.Length; i++)
    {
        bool isSel = selectedAssetFile == i;

        // File type icon prefix
        string ext = files[i].Contains('.') ? files[i][(files[i].LastIndexOf('.') + 1)..] : "";
        Color fileColor = ext switch
        {
            "cs" => Color.Rgba(80, 180, 80),
            "mat" => Color.Rgba(180, 80, 180),
            "fbx" => Color.Rgba(80, 160, 220),
            "png" => Color.Rgba(220, 160, 60),
            "prefab" => Color.Rgba(60, 140, 220),
            "scene" => Color.Rgba(220, 220, 80),
            "shader" => Color.Rgba(220, 100, 100),
            "ogg" or "wav" => Color.Rgba(220, 140, 60),
            "anim" => Color.Rgba(100, 200, 160),
            _ => colText
        };

        var fileStyle = new ButtonStyle
        {
            BackgroundColor = isSel ? colSelection : Color.Rgba(0, 0, 0, 0),
            HoverColor = Color.Rgba(255, 255, 255, 10),
            PressedColor = colSelection,
            TextColor = isSel ? colTextBright : fileColor,
            Padding = Padding.Symmetric(6, 2),
            CornerRadius = CornerRadius.All(2),
            FontSize = 12
        };
        if (ClayUI.Button($"{files[i]}##file_{selectedAssetFolder}_{i}", fileStyle))
            selectedAssetFile = i;
    }

    ClayUI.EndScrollArea();

    ClayUI.EndHorizontal();
}

void RenderConsoleContent()
{
    // Console filter bar
    ClayUI.BeginHorizontal(gap: 8, alignment: ChildAlignment.CenterLeft, style: new LayoutStyle
    {
        Sizing = new Sizing(SizingAxis.Grow(), SizingAxis.Fixed(24)),
        Padding = Padding.Horizontal(8),
        BackgroundColor = colPanelHeader
    });

    var filterStyle = new CheckboxStyle
    {
        BoxSize = 14,
        TextColor = colText,
        FontSize = 11,
        Padding = Padding.Symmetric(2, 2)
    };
    ClayUI.Checkbox("Info##consFilter", ref showInfoLogs, filterStyle);
    ClayUI.Checkbox("Warn##consFilter", ref showWarningLogs, filterStyle);
    ClayUI.Checkbox("Error##consFilter", ref showErrorLogs, filterStyle);

    ClayUI.Spacer();

    var clearBtnStyle = new ButtonStyle
    {
        BackgroundColor = Color.Rgba(0, 0, 0, 0),
        HoverColor = Color.Rgba(255, 255, 255, 20),
        TextColor = colTextDim,
        Padding = Padding.Symmetric(6, 2),
        FontSize = 11
    };
    if (ClayUI.Button("Clear##console", clearBtnStyle))
        Console.WriteLine("Console cleared");

    ClayUI.EndHorizontal();

    // Console messages
    foreach (var msg in consoleMessages)
    {
        bool isInfo = msg.StartsWith("[INFO]");
        bool isWarn = msg.StartsWith("[WARN]");
        bool isError = msg.StartsWith("[ERROR]");

        if (isInfo && !showInfoLogs) continue;
        if (isWarn && !showWarningLogs) continue;
        if (isError && !showErrorLogs) continue;

        Color msgColor = isError ? colError : isWarn ? colWarning : Color.Rgba(170, 170, 170);

        ClayUI.Label(msg, new LabelStyle { TextColor = msgColor, FontSize = 11 });
    }
}

// ============ Status Bar ============

void RenderStatusBar()
{
    ClayUI.BeginHorizontal(gap: 16, alignment: ChildAlignment.CenterLeft, style: new LayoutStyle
    {
        Sizing = new Sizing(SizingAxis.Grow(), SizingAxis.Fixed(22)),
        Padding = Padding.Horizontal(10),
        BackgroundColor = isPlaying ? Color.Rgba(30, 80, 40) : Color.Rgba(35, 35, 38),
        Border = new BorderConfig { Width = new BorderWidth(1, 0, 0, 0), Color = colBorder }
    });

    var statusStyle = new LabelStyle { TextColor = colTextDim, FontSize = 11 };

    if (isPlaying)
        ClayUI.Label("PLAY MODE ACTIVE", new LabelStyle { TextColor = colSuccess, FontSize = 11 });
    else
        ClayUI.Label("Ready", statusStyle);

    ClayUI.Spacer();

    ClayUI.Label($"Entities: {sceneEntities.Length}", statusStyle);
    ClayUI.Label($"FPS: {Raylib.GetFPS()}", statusStyle);
    ClayUI.Label($"{Raylib.GetScreenWidth()}x{Raylib.GetScreenHeight()}", statusStyle);

    ClayUI.EndHorizontal();
}

// ============ Floating Windows ============

void RenderFloatingWindows()
{
    // About window
    if (aboutWindowOpen)
    {
        if (ClayUI.BeginWindow("About##editor", ref aboutWindowOpen,
            defaultPosition: new Vector2(400, 250), defaultSize: new Vector2(360, 200)))
        {
            ClayUI.Heading("Raylib Game Engine Editor", new HeadingStyle { TextColor = colAccent, FontSize = 16 });
            ClayUI.Space(8);
            ClayUI.Label("Built with Clay .NET UI Framework", new LabelStyle { TextColor = colText, FontSize = 13 });
            ClayUI.Label("Rendering: Raylib 5.0", new LabelStyle { TextColor = colText, FontSize = 13 });
            ClayUI.Space(8);
            ClayUI.Label("A demonstration of ClayUI used to build", new LabelStyle { TextColor = colTextDim, FontSize = 12 });
            ClayUI.Label("a game engine editor interface.", new LabelStyle { TextColor = colTextDim, FontSize = 12 });
            ClayUI.Space(12);
            if (ClayUI.Button("OK##aboutClose"))
                aboutWindowOpen = false;
            ClayUI.EndWindow();
        }
    }

    // Build Settings window
    if (buildWindowOpen)
    {
        if (ClayUI.BeginWindow("Build Settings##editor", ref buildWindowOpen,
            defaultPosition: new Vector2(300, 150), defaultSize: new Vector2(420, 320)))
        {
            ClayUI.Combo("Platform", ref buildPlatformIndex, buildPlatforms);
            ClayUI.Space(8);
            ClayUI.Checkbox("Development Build", ref buildDevelopment);
            ClayUI.Checkbox("Script Debugging", ref buildScriptDebugging);
            ClayUI.Space(8);

            ClayUI.Label("Scenes in Build:", new LabelStyle { TextColor = colTextBright, FontSize = 13 });
            ClayUI.Space(4);
            ClayUI.Label("  0: MainScene.scene", new LabelStyle { TextColor = colText, FontSize = 12 });
            ClayUI.Label("  1: MenuScene.scene", new LabelStyle { TextColor = colText, FontSize = 12 });
            ClayUI.Label("  2: TestScene.scene", new LabelStyle { TextColor = colTextDim, FontSize = 12 });

            ClayUI.Space(12);
            ClayUI.BeginHorizontal(gap: 8);
            if (ClayUI.Button("Build##doBuild"))
                Console.WriteLine($"Building for {buildPlatforms[buildPlatformIndex]}...");
            if (ClayUI.Button("Build & Run##doBuildRun"))
                Console.WriteLine($"Building & running for {buildPlatforms[buildPlatformIndex]}...");
            ClayUI.EndHorizontal();

            ClayUI.EndWindow();
        }
    }

    // Preferences window
    if (preferencesWindowOpen)
    {
        if (ClayUI.BeginWindow("Preferences##editor", ref preferencesWindowOpen,
            defaultPosition: new Vector2(350, 180), defaultSize: new Vector2(380, 280)))
        {
            ClayUI.Heading("Editor", new HeadingStyle { TextColor = colTextBright, FontSize = 14 });
            ClayUI.Space(4);
            ClayUI.Checkbox("Show Grid", ref editorShowGrid);
            ClayUI.Checkbox("Show Gizmos", ref editorShowGizmos);
            ClayUI.Checkbox("Snap to Grid", ref editorSnap);
            ClayUI.Slider("Grid Size", ref editorGridSize, 0.1f, 10f);
            ClayUI.Slider("Camera Speed", ref editorCameraSpeed, 0.5f, 20f);

            ClayUI.Space(12);
            if (ClayUI.Button("Close##prefsClose"))
                preferencesWindowOpen = false;

            ClayUI.EndWindow();
        }
    }
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
