using Clay;
using ZeroElectric.Vinculum;

namespace Clay.Example;

/// <summary>
/// Loads RPG GUI sprite sheet and creates a ClayUISkin from its sub-sprites.
/// </summary>
public static class RpgSkin
{
    // Cropped textures (loaded once)
    private static Texture _btnNormal, _btnHover, _btnPressed, _btnDark;
    private static Texture _barThin;
    private static Texture _cbUnchecked, _cbUncheckedHover, _cbUncheckedPressed;
    private static Texture _cbChecked, _cbCheckedHover, _cbCheckedPressed;
    private static Texture _radioOff, _radioOffHover, _radioOffPressed;
    private static Texture _radioOn, _radioOnHover, _radioOnPressed;
    private static Texture _paperBg, _woodBg;
    private static bool _loaded;

    /// <summary>
    /// Loads the RPG GUI sprite sheet and background textures, crops sub-sprites.
    /// Call once after Raylib.InitWindow().
    /// </summary>
    public static unsafe void Load()
    {
        if (_loaded) return;

        // Load sprite sheet as Image for cropping
        var sheet = Raylib.LoadImage("resources/rpg_skin/spritesheet.png");

        // Button bars (3 states + dark variant)
        _btnNormal  = CropTexture(&sheet, 12, 125, 287, 58);
        _btnHover   = CropTexture(&sheet, 12, 203, 287, 58);
        _btnPressed = CropTexture(&sheet, 12, 281, 287, 58);
        _btnDark    = CropTexture(&sheet, 12, 359, 287, 58);

        // Thin horizontal bar (for slider/progress tracks)
        _barThin = CropTexture(&sheet, 794, 72, 208, 20);

        // Checkbox sprites (2 rows of 3: normal, hover, pressed)
        _cbUnchecked        = CropTexture(&sheet, 147, 12, 26, 26);
        _cbUncheckedHover   = CropTexture(&sheet, 192, 12, 26, 26);
        _cbUncheckedPressed = CropTexture(&sheet, 237, 12, 26, 26);
        _cbChecked          = CropTexture(&sheet, 147, 55, 30, 27);
        _cbCheckedHover     = CropTexture(&sheet, 192, 55, 30, 27);
        _cbCheckedPressed   = CropTexture(&sheet, 237, 55, 30, 27);

        // Radio button sprites (2 rows of 3: normal, hover, pressed)
        _radioOff        = CropTexture(&sheet, 17, 13, 24, 24);
        _radioOffHover   = CropTexture(&sheet, 52, 13, 24, 24);
        _radioOffPressed = CropTexture(&sheet, 87, 13, 24, 24);
        _radioOn         = CropTexture(&sheet, 17, 49, 24, 24);
        _radioOnHover    = CropTexture(&sheet, 52, 49, 24, 24);
        _radioOnPressed  = CropTexture(&sheet, 87, 49, 24, 24);

        Raylib.UnloadImage(sheet);

        // Background textures (full images, not from sheet)
        _paperBg = Raylib.LoadTexture("resources/rpg_skin/paper_bg.png");
        _woodBg  = Raylib.LoadTexture("resources/rpg_skin/wood_bg.png");

        // Enable bilinear filtering on all skin textures for smooth scaling
        SetFilter(_btnNormal); SetFilter(_btnHover); SetFilter(_btnPressed); SetFilter(_btnDark);
        SetFilter(_barThin);
        SetFilter(_cbUnchecked); SetFilter(_cbUncheckedHover); SetFilter(_cbUncheckedPressed);
        SetFilter(_cbChecked); SetFilter(_cbCheckedHover); SetFilter(_cbCheckedPressed);
        SetFilter(_radioOff); SetFilter(_radioOffHover); SetFilter(_radioOffPressed);
        SetFilter(_radioOn); SetFilter(_radioOnHover); SetFilter(_radioOnPressed);
        SetFilter(_paperBg); SetFilter(_woodBg);

        _loaded = true;
    }

    /// <summary>
    /// Creates a ClayUISkin using the loaded RPG GUI textures.
    /// </summary>
    public static ClayUISkin CreateSkin()
    {
        // 9-slice insets for the button bars — ornate pointed ends ~50px, top/bottom borders ~10px
        var btnSlice = NineSlice.TRBL(10, 50, 10, 50);
        // Thin bar — small insets
        var barSlice = NineSlice.TRBL(4, 8, 4, 8);

        return new ClayUISkin
        {
            Button = new ButtonSkin
            {
                Background = StateImages.Create(
                    normal:  SkinImage.NineSliced(_btnNormal,  287, 58, btnSlice),
                    hover:   SkinImage.NineSliced(_btnPressed, 287, 58, btnSlice),
                    pressed: SkinImage.NineSliced(_btnHover,   287, 58, btnSlice)
                )
            },
            Checkbox = new CheckboxSkin
            {
                Box = StateImages.Create(
                    normal:  SkinImage.Create(_cbUnchecked, 26, 26),
                    hover:   SkinImage.Create(_cbUncheckedHover, 26, 26),
                    pressed: SkinImage.Create(_cbUncheckedPressed, 26, 26)
                ),
                CheckedBox = SkinImage.Create(_cbChecked, 30, 27)
            },
            Slider = new SliderSkin
            {
                Track = SkinImage.NineSliced(_barThin, 208, 20, barSlice),
                Fill  = SkinImage.NineSliced(_barThin, 208, 20, barSlice)
            },
            Toggle = new ToggleSkin
            {
                TrackOn  = SkinImage.NineSliced(_btnNormal, 287, 58, btnSlice),
                TrackOff = SkinImage.NineSliced(_btnDark,   287, 58, btnSlice),
                Knob     = SkinImage.Create(_radioOn, 24, 24)
            },
            ProgressBar = new ProgressBarSkin
            {
                Track = SkinImage.NineSliced(_barThin, 208, 20, barSlice),
                Fill  = SkinImage.NineSliced(_barThin, 208, 20, barSlice)
            },
            Scrollbar = new ScrollbarSkin
            {
                Track = SkinImage.NineSliced(_btnDark, 287, 58, btnSlice),
                Thumb = StateImages.Create(
                    normal: SkinImage.NineSliced(_btnNormal, 287, 58, btnSlice),
                    hover:  SkinImage.NineSliced(_btnHover,  287, 58, btnSlice)
                )
            },
            RadioGroup = new RadioGroupSkin
            {
                Circle = StateImages.Create(
                    normal:  SkinImage.Create(_radioOff, 24, 24),
                    hover:   SkinImage.Create(_radioOffHover, 24, 24),
                    pressed: SkinImage.Create(_radioOffPressed, 24, 24)
                ),
                SelectedCircle = SkinImage.Create(_radioOn, 24, 24)
            },
            Panel = new PanelSkin
            {
                Background = SkinImage.Create(_paperBg, 256, 256)
            },
            Window = new WindowSkin
            {
                TitleBar = SkinImage.NineSliced(_btnDark, 287, 58, btnSlice),
                Body     = SkinImage.Create(_paperBg, 256, 256)
            }
        };
    }

    /// <summary>
    /// Unloads all loaded textures.
    /// </summary>
    public static void Unload()
    {
        if (!_loaded) return;
        Raylib.UnloadTexture(_btnNormal);
        Raylib.UnloadTexture(_btnHover);
        Raylib.UnloadTexture(_btnPressed);
        Raylib.UnloadTexture(_btnDark);
        Raylib.UnloadTexture(_barThin);
        Raylib.UnloadTexture(_cbUnchecked);
        Raylib.UnloadTexture(_cbUncheckedHover);
        Raylib.UnloadTexture(_cbUncheckedPressed);
        Raylib.UnloadTexture(_cbChecked);
        Raylib.UnloadTexture(_cbCheckedHover);
        Raylib.UnloadTexture(_cbCheckedPressed);
        Raylib.UnloadTexture(_radioOff);
        Raylib.UnloadTexture(_radioOffHover);
        Raylib.UnloadTexture(_radioOffPressed);
        Raylib.UnloadTexture(_radioOn);
        Raylib.UnloadTexture(_radioOnHover);
        Raylib.UnloadTexture(_radioOnPressed);
        Raylib.UnloadTexture(_paperBg);
        Raylib.UnloadTexture(_woodBg);
        _loaded = false;
    }

    private static unsafe Texture CropTexture(Image* sheet, int x, int y, int w, int h)
    {
        var cropped = Raylib.ImageFromImage(*sheet, new Rectangle(x, y, w, h));
        var tex = Raylib.LoadTextureFromImage(cropped);
        Raylib.UnloadImage(cropped);
        return tex;
    }

    private static void SetFilter(Texture tex)
    {
        Raylib.SetTextureFilter(tex, TextureFilter.TEXTURE_FILTER_BILINEAR);
    }
}
