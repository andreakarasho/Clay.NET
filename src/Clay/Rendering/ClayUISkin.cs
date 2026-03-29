namespace Clay;

/// <summary>
/// Skin image definitions for all ClayUI widgets.
/// Set <see cref="ClayUI.Skin"/> to apply custom textures to widget parts.
/// When null, widgets render with their default color-based styles.
/// </summary>
public class ClayUISkin
{
    public ButtonSkin Button;
    public CheckboxSkin Checkbox;
    public SliderSkin Slider;
    public ToggleSkin Toggle;
    public ProgressBarSkin ProgressBar;
    public ScrollbarSkin Scrollbar;
    public PanelSkin Panel;
    public WindowSkin Window;
    public RadioGroupSkin RadioGroup;
    public ComboSkin Combo;
    public ListBoxSkin ListBox;
    public PopupSkin Popup;
    public TextInputSkin TextInput;
}

/// <summary>
/// Skin for <see cref="ClayUI.Button"/>. Replaces the background rectangle.
/// </summary>
public struct ButtonSkin
{
    /// <summary>
    /// Background image for normal/hover/pressed states.
    /// </summary>
    public StateImages Background;
}

/// <summary>
/// Skin for <see cref="ClayUI.Checkbox"/>.
/// </summary>
public struct CheckboxSkin
{
    /// <summary>
    /// The checkbox box image (unchecked state) with hover/pressed variants.
    /// </summary>
    public StateImages Box;

    /// <summary>
    /// Image for the checked box. Falls back to Box.Normal if not set.
    /// </summary>
    public SkinImage CheckedBox;

    /// <summary>
    /// Image for the checkmark indicator inside the box.
    /// </summary>
    public SkinImage Checkmark;
}

/// <summary>
/// Skin for <see cref="ClayUI.Slider"/>.
/// </summary>
public struct SliderSkin
{
    /// <summary>
    /// The slider track background.
    /// </summary>
    public SkinImage Track;

    /// <summary>
    /// The filled portion of the track.
    /// </summary>
    public SkinImage Fill;
}

/// <summary>
/// Skin for <see cref="ClayUI.Toggle"/>.
/// </summary>
public struct ToggleSkin
{
    /// <summary>
    /// Track image when the toggle is ON.
    /// </summary>
    public SkinImage TrackOn;

    /// <summary>
    /// Track image when the toggle is OFF.
    /// </summary>
    public SkinImage TrackOff;

    /// <summary>
    /// The knob/thumb image.
    /// </summary>
    public SkinImage Knob;
}

/// <summary>
/// Skin for <see cref="ClayUI.ProgressBar"/>.
/// </summary>
public struct ProgressBarSkin
{
    /// <summary>
    /// The track background.
    /// </summary>
    public SkinImage Track;

    /// <summary>
    /// The filled portion.
    /// </summary>
    public SkinImage Fill;
}

/// <summary>
/// Skin for <see cref="ClayUI.VerticalScrollbar"/> and <see cref="ClayUI.HorizontalScrollbar"/>.
/// </summary>
public struct ScrollbarSkin
{
    /// <summary>
    /// The scrollbar track background.
    /// </summary>
    public SkinImage Track;

    /// <summary>
    /// The scrollbar thumb/handle with hover state.
    /// </summary>
    public StateImages Thumb;
}

/// <summary>
/// Skin for <see cref="ClayUI.BeginPanel"/>.
/// </summary>
public struct PanelSkin
{
    /// <summary>
    /// The panel background.
    /// </summary>
    public SkinImage Background;
}

/// <summary>
/// Skin for <see cref="ClayUI.BeginWindow"/>.
/// </summary>
public struct WindowSkin
{
    /// <summary>
    /// Title bar background image.
    /// </summary>
    public SkinImage TitleBar;

    /// <summary>
    /// Window body/content area background.
    /// </summary>
    public SkinImage Body;
}

/// <summary>
/// Skin for <see cref="ClayUI.RadioGroup"/>.
/// </summary>
public struct RadioGroupSkin
{
    /// <summary>
    /// The unselected radio circle image with hover/pressed variants.
    /// </summary>
    public StateImages Circle;

    /// <summary>
    /// The selected radio circle image (with filled dot). Falls back to Circle if not set.
    /// </summary>
    public SkinImage SelectedCircle;
}

/// <summary>
/// Skin for <see cref="ClayUI.Combo"/>.
/// </summary>
public struct ComboSkin
{
    /// <summary>
    /// The combo box button background.
    /// </summary>
    public StateImages Background;
}

/// <summary>
/// Skin for <see cref="ClayUI.BeginListBox"/>.
/// </summary>
public struct ListBoxSkin
{
    /// <summary>
    /// The list box container background.
    /// </summary>
    public SkinImage Background;

    /// <summary>
    /// Selected item highlight.
    /// </summary>
    public SkinImage SelectedItem;
}

/// <summary>
/// Skin for <see cref="ClayUI.BeginPopup"/>.
/// </summary>
public struct PopupSkin
{
    /// <summary>
    /// The popup background.
    /// </summary>
    public SkinImage Background;
}

/// <summary>
/// Skin for <see cref="ClayUI.TextInput"/>.
/// </summary>
public struct TextInputSkin
{
    /// <summary>
    /// The text input field background.
    /// </summary>
    public StateImages Background;
}
