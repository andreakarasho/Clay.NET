using System.Numerics;
using System.Runtime.CompilerServices;

namespace Clay;

/// <summary>
/// Warning flags for capacity and configuration issues.
/// </summary>
public struct BooleanWarnings
{
    public bool MaxElementsExceeded;
    public bool MaxRenderCommandsExceeded;
    public bool MaxTextMeasureCacheExceeded;
    public bool TextMeasurementFunctionNotSet;
}

/// <summary>
/// Error types that can occur during layout computation.
/// </summary>
public enum ErrorType : byte
{
    TextMeasurementFunctionNotProvided,
    ArenaCapacityExceeded,
    ElementsCapacityExceeded,
    TextMeasurementCapacityExceeded,
    DuplicateId,
    FloatingContainerParentNotFound,
    PercentageOver1,
    InternalError
}

/// <summary>
/// Error data passed to the error handler.
/// </summary>
public struct ErrorData
{
    public ErrorType ErrorType;
    public string ErrorText;
}

/// <summary>
/// Delegate for error handling.
/// </summary>
public delegate void ErrorHandler(ErrorData errorData);

/// <summary>
/// Callback invoked during hover detection to allow custom hit-testing (e.g., pixel-perfect picking).
/// Return true if the point should count as hitting the element, false to discard.
/// </summary>
public delegate bool HitTestHandler(ElementId id, BoundingBox bounds, Vector2 point);

/// <summary>
/// The main Clay context containing all state for layout computation.
/// </summary>

public class ClayContext : IDisposable
{
    // Configuration
    public int MaxElementCount;
    public ErrorHandler? OnError;
    public HitTestHandler? CustomHitTest;

    // State
    public BooleanWarnings Warnings;
    public PointerData PointerInfo;
    public Dimensions LayoutDimensions;
    public uint Generation;
    public float DeltaTime;
    public bool DebugModeEnabled;
    // Debug overlay: outline the elements under the pointer (topmost lime,
    // occluded-beneath red). Emitted by GeneratePointerHighlightCommands.
    public bool PointerHighlightEnabled;
    public bool CullingDisabled;

    // Text measurement
    public ITextMeasurer? TextMeasurer;

    // Text input widget management
    internal Widgets.TextInput? TextInput;

    // Default configs
    private LayoutConfig _defaultLayout;

    // Layout Elements
    public ClayList<LayoutElement> LayoutElements;
    public ClayList<RenderCommand> RenderCommands;
    public ClayList<int> OpenLayoutElementStack;
    public ClayList<int> LayoutElementChildren;
    public ClayList<int> LayoutElementChildrenBuffer;
    public ClayList<TextElementData> TextElementData;
    public ClayList<int> LayoutElementClipElementIds;

    // Configs
    public ClayList<LayoutConfig> LayoutConfigs;
    public ClayList<ElementConfig> ElementConfigs;
    public ClayList<TextConfig> TextElementConfigs;
    public ClayList<ImageConfig> ImageElementConfigs;
    public ClayList<FloatingConfig> FloatingElementConfigs;
    public ClayList<ScrollConfig> ScrollElementConfigs;
    public ClayList<CustomConfig> CustomElementConfigs;
    public ClayList<BorderConfig> BorderElementConfigs;
    public ClayList<ShadowConfig> ShadowElementConfigs;
    public ClayList<SharedElementConfig> SharedElementConfigs;

    // Tree structures
    public ClayList<LayoutElementTreeRoot> LayoutElementTreeRoots;
    public ClayList<LayoutElementHashMapItem> LayoutElementsHashMapInternal;
    public ClayList<int> LayoutElementsHashMap;
    public ClayList<int> OpenClipElementStack;
    public ClayList<ScrollContainerDataInternal> ScrollContainerDatas;

    // IDs whose bounding box contains the pointer this frame.
    // Ordered topmost-first: highest-ZIndex root first, and within a root the
    // deepest last-child appears before its ancestors (postorder + forward-child push).
    public ClayList<ElementId> PointerOverIds;

    // Bounding boxes parallel to PointerOverIds, captured at hit-test time so
    // PointerOver can pass them to CustomHitTest without re-querying the element
    // hash map (which is cleared each frame at BeginLayout).
    private ClayList<BoundingBox> _pointerOverBoxes;

    // Reusable temporary collections (avoid per-frame allocations)
    private readonly List<int> _bfsQueue = new(256);
    private readonly Dictionary<uint, int> _scrollContainerIndex = new();
    private ClayList<int> _pointerDfsStack = new(256);
    private ClayList<bool> _pointerDfsVisited = new(256);

    public ClayContext(int maxElementCount = 8192)
    {
        MaxElementCount = maxElementCount;
        _defaultLayout = LayoutConfig.Default;

        // Initialize all lists
        LayoutElements = new ClayList<LayoutElement>(maxElementCount);
        RenderCommands = new ClayList<RenderCommand>(maxElementCount);
        OpenLayoutElementStack = new ClayList<int>(256);
        LayoutElementChildren = new ClayList<int>(maxElementCount);
        LayoutElementChildrenBuffer = new ClayList<int>(maxElementCount);
        TextElementData = new ClayList<TextElementData>(maxElementCount);
        LayoutElementClipElementIds = new ClayList<int>(maxElementCount);

        LayoutConfigs = new ClayList<LayoutConfig>(maxElementCount);
        ElementConfigs = new ClayList<ElementConfig>(maxElementCount);
        TextElementConfigs = new ClayList<TextConfig>(maxElementCount);
        ImageElementConfigs = new ClayList<ImageConfig>(maxElementCount);
        FloatingElementConfigs = new ClayList<FloatingConfig>(maxElementCount);
        ScrollElementConfigs = new ClayList<ScrollConfig>(maxElementCount);
        CustomElementConfigs = new ClayList<CustomConfig>(maxElementCount);
        BorderElementConfigs = new ClayList<BorderConfig>(maxElementCount);
        ShadowElementConfigs = new ClayList<ShadowConfig>(maxElementCount);
        SharedElementConfigs = new ClayList<SharedElementConfig>(maxElementCount);

        LayoutElementTreeRoots = new ClayList<LayoutElementTreeRoot>(256);
        LayoutElementsHashMapInternal = new ClayList<LayoutElementHashMapItem>(maxElementCount);
        LayoutElementsHashMap = new ClayList<int>(maxElementCount);
        OpenClipElementStack = new ClayList<int>(64);
        ScrollContainerDatas = new ClayList<ScrollContainerDataInternal>(16);
        PointerOverIds = new ClayList<ElementId>(maxElementCount);
        _pointerOverBoxes = new ClayList<BoundingBox>(maxElementCount);
    }

    /// <summary>
    /// Begins a new layout frame.
    /// </summary>
    public void BeginLayout(float deltaTime = 0)
    {
        DeltaTime = deltaTime;
        TextInput?.BeginFrame();
        Warnings = default;
        Generation++;

        // Reset ephemeral data
        LayoutElements.Clear();
        RenderCommands.Clear();
        OpenLayoutElementStack.Clear();
        LayoutElementChildren.Clear();
        LayoutElementChildrenBuffer.Clear();
        TextElementData.Clear();
        LayoutElementClipElementIds.Clear();

        LayoutConfigs.Clear();
        ElementConfigs.Clear();
        TextElementConfigs.Clear();
        ImageElementConfigs.Clear();
        FloatingElementConfigs.Clear();
        ScrollElementConfigs.Clear();
        CustomElementConfigs.Clear();
        BorderElementConfigs.Clear();
        ShadowElementConfigs.Clear();
        SharedElementConfigs.Clear();

        LayoutElementTreeRoots.Clear();

        // Rebuild the element hash map from scratch each frame. It is keyed by
        // element id; left to persist it only grows (AddHashMapItem appends every
        // new id) until MaxElementCount, after which new elements silently fail to
        // register (no hash item -> zero bounding box -> culled / "vanish"). Under
        // entity-index recycling, stale ids also linger and re-collide.
        // Zero the bucket backing array explicitly: Clear() only resets length,
        // and Set() of a high bucket leaves the lower un-set gaps holding stale
        // bucket-head indices that point into the freshly-cleared internal list.
        // This runs AFTER UpdateScrollContainers (frame start), so that prune
        // still sees the previous frame's map.
        System.Array.Clear(LayoutElementsHashMap.InternalArray, 0, LayoutElementsHashMap.InternalArray.Length);
        LayoutElementsHashMap.Clear();
        LayoutElementsHashMapInternal.Clear();

        // Create root element
        var rootId = ElementId.Hash("Clay__RootContainer");

        OpenElement();
        ConfigureOpenElement(new ElementDeclaration
        {
            Id = rootId,
            Layout = new LayoutConfig
            {
                Sizing = new Sizing(
                    SizingAxis.Fixed(LayoutDimensions.Width),
                    SizingAxis.Fixed(LayoutDimensions.Height)
                )
            }
        });

        // Add root to tree roots
        LayoutElementTreeRoots.Add(new LayoutElementTreeRoot
        {
            LayoutElementIndex = 0,
            ParentId = 0,
            ClipElementId = 0,
            ZIndex = 0
        });
    }

    /// <summary>
    /// Ends the layout frame and computes the final layout.
    /// </summary>
    public ReadOnlySpan<RenderCommand> EndLayout()
    {
        CloseElement();
        ComputeLayout();
        GenerateRenderCommands();
        GeneratePointerHighlightCommands();
        return RenderCommands.AsReadOnlySpan();
    }

    // Debug overlay: frame the elements under the pointer. The topmost hit
    // (PointerOverIds[0]) is outlined lime; every element occluded beneath it
    // at the same point is outlined red. Appended after the normal commands so
    // the outlines paint on top. Pure bbox/z hit — the host's per-pixel
    // CustomHitTest is NOT applied here, so this shows Clay's own stacking pick.
    private void GeneratePointerHighlightCommands()
    {
        if (!PointerHighlightEnabled)
            return;

        // Gray frame around every element laid out this frame (Generation + 1
        // is this frame's stamp; the hash map keeps recycled entries from prior
        // frames). Emitted first so the lime/red pointer outlines paint over it.
        var gray = new Color(150, 150, 150, 110);
        for (int i = 0; i < LayoutElementsHashMapInternal.Length; i++)
        {
            ref var item = ref LayoutElementsHashMapInternal[i];
            if (item.Generation != Generation + 1)
                continue;
            RenderCommands.Add(new RenderCommand
            {
                BoundingBox = item.BoundingBox,
                CommandType = RenderCommandType.Border,
                Id = item.ElementId.Id,
                ZIndex = short.MaxValue,
                Border = new BorderRenderData
                {
                    Color = gray,
                    Width = BorderWidth.All(1),
                    CornerRadius = default,
                }
            });
        }

        // Rebuild against the freshly computed layout (SetPointerState earlier
        // in the frame hit-tested the previous frame's tree).
        RebuildPointerOverIds(PointerInfo.Position);

        var lime = new Color(0, 255, 0, 255);
        var red = new Color(255, 0, 0, 255);

        for (int i = 0; i < PointerOverIds.Length; i++)
        {
            RenderCommands.Add(new RenderCommand
            {
                BoundingBox = _pointerOverBoxes[i],
                CommandType = RenderCommandType.Border,
                Id = PointerOverIds[i].Id,
                ZIndex = short.MaxValue,
                Border = new BorderRenderData
                {
                    Color = i == 0 ? lime : red,
                    Width = BorderWidth.All(2),
                    CornerRadius = default,
                }
            });
        }
    }

    /// <summary>
    /// Opens a new element.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void OpenElement()
    {
        if (LayoutElements.Length >= MaxElementCount - 1 || Warnings.MaxElementsExceeded)
        {
            Warnings.MaxElementsExceeded = true;
            return;
        }

        LayoutElements.Add(new LayoutElement());
        OpenLayoutElementStack.Add(LayoutElements.Length - 1);

        // Set clip element ID
        int clipId = OpenClipElementStack.Length > 0
            ? OpenClipElementStack[OpenClipElementStack.Length - 1]
            : 0;
        LayoutElementClipElementIds.Set(LayoutElements.Length - 1, clipId);
    }

    /// <summary>
    /// Configures the currently open element.
    /// </summary>
    public void ConfigureOpenElement(ElementDeclaration declaration)
    {
        if (Warnings.MaxElementsExceeded)
            return;

        ref var openLayoutElement = ref GetOpenLayoutElement();

        // Store layout config (default when not provided)
        var layout = declaration.Layout ?? default;
        int layoutConfigIndex = LayoutConfigs.Add(layout);
        openLayoutElement.LayoutConfigIndex = layoutConfigIndex;

        // Validate percent sizing
        if ((layout.Sizing.Width.Type == SizingType.Percent && layout.Sizing.Width.Percent > 1) ||
            (layout.Sizing.Height.Type == SizingType.Percent && layout.Sizing.Height.Percent > 1))
        {
            OnError?.Invoke(new ErrorData
            {
                ErrorType = ErrorType.PercentageOver1,
                ErrorText = "An element was configured with PERCENT sizing, but the value was over 1.0."
            });
        }

        // Set up element configs
        openLayoutElement.ElementConfigsStart = ElementConfigs.Length;

        // Process shared config (background color, corner radius, user data)
        if (declaration.BackgroundColor is not null || declaration.CornerRadius is not null || declaration.UserData is not null)
        {
            int sharedIndex = SharedElementConfigs.Add(new SharedElementConfig
            {
                BackgroundColor = declaration.BackgroundColor ?? default,
                CornerRadius = declaration.CornerRadius ?? default,
                UserData = declaration.UserData ?? 0
            });
            ElementConfigs.Add(new ElementConfig { Type = ElementConfigType.Shared, ConfigIndex = sharedIndex });
            openLayoutElement.ElementConfigsLength++;
        }

        // Process image config
        if (declaration.Image is not null)
        {
            int imageIndex = ImageElementConfigs.Add(declaration.Image.Value);
            ElementConfigs.Add(new ElementConfig { Type = ElementConfigType.Image, ConfigIndex = imageIndex });
            openLayoutElement.ElementConfigsLength++;
        }

        // Process floating config
        if (declaration.Floating is not null)
        {
            var floatingConfig = declaration.Floating.Value;

            if (OpenLayoutElementStack.Length >= 2)
            {
                ref var hierarchicalParent = ref LayoutElements[
                    OpenLayoutElementStack[OpenLayoutElementStack.Length - 2]];

                uint clipElementId = 0;
                if (floatingConfig.AttachTo == FloatingAttachTo.Parent)
                {
                    floatingConfig.ParentId = hierarchicalParent.Id;
                    if (OpenClipElementStack.Length > 0)
                    {
                        clipElementId = (uint)OpenClipElementStack[OpenClipElementStack.Length - 1];
                    }
                }
                else if (floatingConfig.AttachTo == FloatingAttachTo.Root)
                {
                    floatingConfig.ParentId = ElementId.Hash("Clay__RootContainer").Id;
                }

                var openElementId = declaration.Id;
                if (!openElementId.IsValid)
                {
                    openElementId = ElementId.Hash("Clay__FloatingContainer", (uint)LayoutElementTreeRoots.Length);
                }

                LayoutElementTreeRoots.Add(new LayoutElementTreeRoot
                {
                    LayoutElementIndex = OpenLayoutElementStack[OpenLayoutElementStack.Length - 1],
                    ParentId = floatingConfig.ParentId,
                    ClipElementId = clipElementId,
                    ZIndex = floatingConfig.ZIndex
                });

                int floatingIndex = FloatingElementConfigs.Add(floatingConfig);
                ElementConfigs.Add(new ElementConfig { Type = ElementConfigType.Floating, ConfigIndex = floatingIndex });
                openLayoutElement.ElementConfigsLength++;
            }
        }

        // Process custom config
        if (declaration.Custom is not null)
        {
            int customIndex = CustomElementConfigs.Add(declaration.Custom.Value);
            ElementConfigs.Add(new ElementConfig { Type = ElementConfigType.Custom, ConfigIndex = customIndex });
            openLayoutElement.ElementConfigsLength++;
        }

        // Attach ID
        if (declaration.Id.IsValid)
        {
            AttachId(declaration.Id);
        }
        else if (openLayoutElement.Id == 0)
        {
            GenerateIdForAnonymousElement(ref openLayoutElement);
        }

        // Push to clip stack if this element clips children (one entry per element)
        if (layout.ClipContent || declaration.Scroll is not null)
        {
            OpenClipElementStack.Add((int)openLayoutElement.Id);
        }

        // Process scroll config
        if (declaration.Scroll is not null)
        {
            int scrollIndex = ScrollElementConfigs.Add(declaration.Scroll.Value);
            ElementConfigs.Add(new ElementConfig { Type = ElementConfigType.Scroll, ConfigIndex = scrollIndex });
            openLayoutElement.ElementConfigsLength++;

            // Find or create scroll container data
            bool found = false;
            for (int i = 0; i < ScrollContainerDatas.Length; i++)
            {
                ref var scrollData = ref ScrollContainerDatas[i];
                if (openLayoutElement.Id == scrollData.ElementId)
                {
                    scrollData.LayoutElementIndex = OpenLayoutElementStack[OpenLayoutElementStack.Length - 1];
                    scrollData.OpenThisFrame = true;
                    found = true;
                    break;
                }
            }

            if (!found)
            {
                ScrollContainerDatas.Add(new ScrollContainerDataInternal
                {
                    LayoutElementIndex = OpenLayoutElementStack[OpenLayoutElementStack.Length - 1],
                    ScrollOrigin = new Vector2(-1, -1),
                    ElementId = openLayoutElement.Id,
                    OpenThisFrame = true
                });
            }
        }

        // Process border config
        if (declaration.Border is not null)
        {
            int borderIndex = BorderElementConfigs.Add(declaration.Border.Value);
            ElementConfigs.Add(new ElementConfig { Type = ElementConfigType.Border, ConfigIndex = borderIndex });
            openLayoutElement.ElementConfigsLength++;
        }

        // Process shadow config
        if (declaration.Shadow is not null)
        {
            int shadowIndex = ShadowElementConfigs.Add(declaration.Shadow.Value);
            ElementConfigs.Add(new ElementConfig { Type = ElementConfigType.Shadow, ConfigIndex = shadowIndex });
            openLayoutElement.ElementConfigsLength++;
        }
    }

    /// <summary>
    /// Closes the currently open element.
    /// </summary>
    public void CloseElement()
    {
        if (Warnings.MaxElementsExceeded)
            return;

        ref var openLayoutElement = ref GetOpenLayoutElement();
        ref var layoutConfig = ref LayoutConfigs[openLayoutElement.LayoutConfigIndex];

        bool elementHasScrollHorizontal = false;
        bool elementHasScrollVertical = false;

        // Check for scroll config
        for (int i = 0; i < openLayoutElement.ElementConfigsLength; i++)
        {
            ref var config = ref ElementConfigs[openLayoutElement.ElementConfigsStart + i];
            if (config.Type == ElementConfigType.Scroll)
            {
                ref var scrollConfig = ref ScrollElementConfigs[config.ConfigIndex];
                elementHasScrollHorizontal = scrollConfig.Horizontal;
                elementHasScrollVertical = scrollConfig.Vertical;
                break;
            }
        }

        // Pop clip stack (one entry per element, matching the push in ConfigureOpenElement)
        if (layoutConfig.ClipContent || elementHasScrollHorizontal || elementHasScrollVertical)
        {
            OpenClipElementStack.Length--;
        }

        // Attach children
        openLayoutElement.Children.StartIndex = LayoutElementChildren.Length;

        if (layoutConfig.Direction == LayoutDirection.LeftToRight)
        {
            openLayoutElement.Dimensions.Width = layoutConfig.Padding.Left + layoutConfig.Padding.Right;

            for (int i = 0; i < openLayoutElement.Children.Length; i++)
            {
                int childIndex = LayoutElementChildrenBuffer[
                    LayoutElementChildrenBuffer.Length - openLayoutElement.Children.Length + i];
                ref var child = ref LayoutElements[childIndex];

                openLayoutElement.Dimensions.Width += child.Dimensions.Width;
                openLayoutElement.Dimensions.Height = Math.Max(openLayoutElement.Dimensions.Height,
                    child.Dimensions.Height + layoutConfig.Padding.Top + layoutConfig.Padding.Bottom);

                if (!elementHasScrollHorizontal)
                    openLayoutElement.MinDimensions.Width += child.MinDimensions.Width;
                if (!elementHasScrollVertical)
                    openLayoutElement.MinDimensions.Height = Math.Max(openLayoutElement.MinDimensions.Height,
                        child.MinDimensions.Height + layoutConfig.Padding.Top + layoutConfig.Padding.Bottom);

                LayoutElementChildren.Add(childIndex);
            }

            float childGap = Math.Max(openLayoutElement.Children.Length - 1, 0) * layoutConfig.ChildGap;
            openLayoutElement.Dimensions.Width += childGap;
            openLayoutElement.MinDimensions.Width += childGap;
        }
        else
        {
            openLayoutElement.Dimensions.Height = layoutConfig.Padding.Top + layoutConfig.Padding.Bottom;

            for (int i = 0; i < openLayoutElement.Children.Length; i++)
            {
                int childIndex = LayoutElementChildrenBuffer[
                    LayoutElementChildrenBuffer.Length - openLayoutElement.Children.Length + i];
                ref var child = ref LayoutElements[childIndex];

                openLayoutElement.Dimensions.Height += child.Dimensions.Height;
                openLayoutElement.Dimensions.Width = Math.Max(openLayoutElement.Dimensions.Width,
                    child.Dimensions.Width + layoutConfig.Padding.Left + layoutConfig.Padding.Right);

                if (!elementHasScrollVertical)
                    openLayoutElement.MinDimensions.Height += child.MinDimensions.Height;
                if (!elementHasScrollHorizontal)
                    openLayoutElement.MinDimensions.Width = Math.Max(openLayoutElement.MinDimensions.Width,
                        child.MinDimensions.Width + layoutConfig.Padding.Left + layoutConfig.Padding.Right);

                LayoutElementChildren.Add(childIndex);
            }

            float childGap = Math.Max(openLayoutElement.Children.Length - 1, 0) * layoutConfig.ChildGap;
            openLayoutElement.Dimensions.Height += childGap;
            openLayoutElement.MinDimensions.Height += childGap;
        }

        LayoutElementChildrenBuffer.Length -= openLayoutElement.Children.Length;

        // Apply min/max constraints
        if (layoutConfig.Sizing.Width.Type != SizingType.Percent)
        {
            float maxWidth = layoutConfig.Sizing.Width.MinMax.Max;
            if (maxWidth <= 0) maxWidth = float.MaxValue;
            openLayoutElement.Dimensions.Width = Math.Clamp(openLayoutElement.Dimensions.Width,
                layoutConfig.Sizing.Width.MinMax.Min, maxWidth);
            openLayoutElement.MinDimensions.Width = Math.Clamp(openLayoutElement.MinDimensions.Width,
                layoutConfig.Sizing.Width.MinMax.Min, maxWidth);
        }
        // NOTE: a Percent element keeps the content-derived width computed from
        // its children here (it is NOT reset to 0). The width pass overwrites it
        // with the resolved percent against the real parent, so the only effect
        // of keeping it is that a FIT parent can size to this element's content.
        // Resetting to 0 collapsed a fit parent whose sole child is percent/grow
        // width — e.g. a tooltip wrapper around flowing text got 0 width and thus
        // 0 hit area (no hover). Content sizing matches CSS min-content behaviour.

        if (layoutConfig.Sizing.Height.Type != SizingType.Percent)
        {
            float maxHeight = layoutConfig.Sizing.Height.MinMax.Max;
            if (maxHeight <= 0) maxHeight = float.MaxValue;
            openLayoutElement.Dimensions.Height = Math.Clamp(openLayoutElement.Dimensions.Height,
                layoutConfig.Sizing.Height.MinMax.Min, maxHeight);
            openLayoutElement.MinDimensions.Height = Math.Clamp(openLayoutElement.MinDimensions.Height,
                layoutConfig.Sizing.Height.MinMax.Min, maxHeight);
        }
        else
        {
            openLayoutElement.Dimensions.Height = 0;
        }

        // Handle aspect ratio for images
        UpdateAspectRatioBox(ref openLayoutElement);

        // Check if element is floating
        bool elementIsFloating = HasConfig(ref openLayoutElement, ElementConfigType.Floating);

        // Close the currently open element
        int closingElementIndex = OpenLayoutElementStack.RemoveSwapback(OpenLayoutElementStack.Length - 1);

        // Add this element as a child of its parent (if it has one)
        if (!elementIsFloating && OpenLayoutElementStack.Length >= 1)
        {
            ref var parent = ref GetOpenLayoutElement();
            parent.Children.Length++;
            LayoutElementChildrenBuffer.Add(closingElementIndex);
        }
    }

    /// <summary>
    /// Adds a text element.
    /// </summary>
    public void AddText(ReadOnlySpan<char> text, TextConfig config)
    {
        if (Warnings.MaxElementsExceeded || LayoutElements.Length >= MaxElementCount - 1)
        {
            Warnings.MaxElementsExceeded = true;
            return;
        }

        ref var parentElement = ref GetOpenLayoutElement();
        string textString = text.ToString();

        // Create text element
        LayoutElements.Add(new LayoutElement());
        int elementIndex = LayoutElements.Length - 1;
        ref var element = ref LayoutElements[elementIndex];

        // Set clip element ID
        int clipId = OpenClipElementStack.Length > 0
            ? OpenClipElementStack[OpenClipElementStack.Length - 1]
            : 0;
        LayoutElementClipElementIds.Set(elementIndex, clipId);

        LayoutElementChildrenBuffer.Add(elementIndex);

        // Measure text
        Dimensions textDimensions;
        if (TextMeasurer != null)
        {
            textDimensions = TextMeasurer.MeasureText(text, config.FontId, config.FontSize, config.LetterSpacing);
            // Scale height based on LineHeight if specified (measurer returns fontSize-based height)
            if (config.LineHeight > 0 && config.FontSize > 0)
            {
                float lineHeightRatio = config.LineHeight / (float)config.FontSize;
                textDimensions.Height *= lineHeightRatio;
            }
        }
        else
        {
            // Count lines for simple estimation
            int lineCount = 1;
            foreach (var ch in text)
            {
                if (ch == '\n') lineCount++;
            }
            float lineHeight = config.LineHeight > 0 ? config.LineHeight : config.FontSize * 1.2f;
            textDimensions = new Dimensions(text.Length * config.FontSize * 0.6f, lineHeight * lineCount);
        }

        // Generate element ID
        var elementId = ElementId.HashNumber((uint)parentElement.Children.Length, parentElement.Id);
        element.Id = elementId.Id;
        AddHashMapItem(elementId, elementIndex, 0);

        // Set dimensions
        element.Dimensions = textDimensions;
        element.MinDimensions = new Dimensions(textDimensions.Height, textDimensions.Height);

        // Store text element data
        int textDataIndex = TextElementData.Add(new TextElementData
        {
            Text = textString,
            PreferredDimensions = textDimensions,
            ElementIndex = elementIndex
        });
        element.TextData = new TextElementDataRef { Index = textDataIndex, IsText = true };

        // Store text config
        int textConfigIndex = TextElementConfigs.Add(config);
        element.ElementConfigsStart = ElementConfigs.Length;
        element.ElementConfigsLength = 1;
        ElementConfigs.Add(new ElementConfig { Type = ElementConfigType.Text, ConfigIndex = textConfigIndex });

        // Use default layout
        element.LayoutConfigIndex = LayoutConfigs.Add(_defaultLayout);

        parentElement.Children.Length++;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private ref LayoutElement GetOpenLayoutElement()
    {
        return ref LayoutElements[OpenLayoutElementStack[OpenLayoutElementStack.Length - 1]];
    }

    private void AttachId(ElementId elementId)
    {
        if (Warnings.MaxElementsExceeded)
            return;

        ref var openLayoutElement = ref GetOpenLayoutElement();
        uint idAlias = openLayoutElement.Id;
        openLayoutElement.Id = elementId.Id;
        AddHashMapItem(elementId, OpenLayoutElementStack[OpenLayoutElementStack.Length - 1], idAlias);
    }

    private void GenerateIdForAnonymousElement(ref LayoutElement openLayoutElement)
    {
        if (OpenLayoutElementStack.Length < 2)
            return;

        ref var parentElement = ref LayoutElements[OpenLayoutElementStack[OpenLayoutElementStack.Length - 2]];
        var elementId = ElementId.HashNumber((uint)parentElement.Children.Length, parentElement.Id);
        openLayoutElement.Id = elementId.Id;
        AddHashMapItem(elementId, OpenLayoutElementStack[OpenLayoutElementStack.Length - 1], 0);
    }

    private void AddHashMapItem(ElementId elementId, int layoutElementIndex, uint idAlias)
    {
        if (LayoutElementsHashMapInternal.Length >= MaxElementCount - 1)
            return;

        var item = new LayoutElementHashMapItem
        {
            ElementId = elementId,
            LayoutElementIndex = layoutElementIndex,
            NextIndex = -1,
            Generation = Generation + 1,
            IdAlias = idAlias
        };

        int hashBucket = (int)(elementId.Id % (uint)Math.Max(LayoutElementsHashMap.Capacity, 1024));

        // Ensure hash map is large enough
        while (LayoutElementsHashMap.Capacity <= hashBucket)
        {
            LayoutElementsHashMap.Set(LayoutElementsHashMap.Capacity, 0);
        }

        int hashItemPrevious = -1;
        // Hash map stores index+1 (0 means empty)
        int hashItemIndex = LayoutElementsHashMap.GetValue(hashBucket) - 1;

        while (hashItemIndex >= 0 && hashItemIndex < LayoutElementsHashMapInternal.Length)
        {
            ref var hashItem = ref LayoutElementsHashMapInternal[hashItemIndex];
            if (hashItem.ElementId.Id == elementId.Id)
            {
                item.NextIndex = hashItem.NextIndex;
                if (hashItem.Generation <= Generation)
                {
                    hashItem.ElementId = elementId;
                    hashItem.Generation = Generation + 1;
                    hashItem.LayoutElementIndex = layoutElementIndex;
                }
                else
                {
                    OnError?.Invoke(new ErrorData
                    {
                        ErrorType = ErrorType.DuplicateId,
                        ErrorText = "Duplicate element ID"
                    });
                }
                return;
            }
            hashItemPrevious = hashItemIndex;
            hashItemIndex = hashItem.NextIndex;
        }

        int newIndex = LayoutElementsHashMapInternal.Add(item);

        if (hashItemPrevious >= 0)
        {
            LayoutElementsHashMapInternal[hashItemPrevious].NextIndex = newIndex;
        }
        else
        {
            // Store index+1 so that 0 means empty
            LayoutElementsHashMap.Set(hashBucket, newIndex + 1);
        }
    }

    private int GetHashMapItemIndex(uint id)
    {
        if (LayoutElementsHashMap.Length == 0)
            return -1;

        int hashBucket = (int)(id % (uint)Math.Max(LayoutElementsHashMap.Capacity, 1024));
        if (hashBucket >= LayoutElementsHashMap.Capacity)
            return -1;

        // Hash map stores index+1 (0 means empty)
        int elementIndex = LayoutElementsHashMap.GetValue(hashBucket) - 1;

        while (elementIndex >= 0 && elementIndex < LayoutElementsHashMapInternal.Length)
        {
            ref var hashEntry = ref LayoutElementsHashMapInternal[elementIndex];
            if (hashEntry.ElementId.Id == id)
                return elementIndex;
            elementIndex = hashEntry.NextIndex;
        }

        return -1;
    }

    private bool HasConfig(ref LayoutElement element, ElementConfigType type)
    {
        for (int i = 0; i < element.ElementConfigsLength; i++)
        {
            if (ElementConfigs[element.ElementConfigsStart + i].Type == type)
                return true;
        }
        return false;
    }

    private int FindConfigIndex(ref LayoutElement element, ElementConfigType type)
    {
        for (int i = 0; i < element.ElementConfigsLength; i++)
        {
            ref var config = ref ElementConfigs[element.ElementConfigsStart + i];
            if (config.Type == type)
                return config.ConfigIndex;
        }
        return -1;
    }

    private void UpdateAspectRatioBox(ref LayoutElement layoutElement)
    {
        int imageIndex = FindConfigIndex(ref layoutElement, ElementConfigType.Image);
        if (imageIndex < 0) return;

        ref var imageConfig = ref ImageElementConfigs[imageIndex];
        if (imageConfig.SourceDimensions.Width == 0 || imageConfig.SourceDimensions.Height == 0)
            return;

        float aspect = imageConfig.SourceDimensions.Width / imageConfig.SourceDimensions.Height;
        if (layoutElement.Dimensions.Width == 0 && layoutElement.Dimensions.Height != 0)
        {
            layoutElement.Dimensions.Width = layoutElement.Dimensions.Height * aspect;
        }
        else if (layoutElement.Dimensions.Width != 0 && layoutElement.Dimensions.Height == 0)
        {
            layoutElement.Dimensions.Height = layoutElement.Dimensions.Width / aspect;
        }
    }

    private void ComputeLayout()
    {
        SizeContainersAlongAxis(true);
        // Width pass reflows overflowing text leaves to multiple lines; their FIT
        // ancestors were height-summed at CloseElement against the pre-wrap single
        // line, so re-sum them before the height pass / positioning runs.
        RecomputeFitHeights();
        SizeContainersAlongAxis(false);
        PositionElements();
    }

    // Bottom-up (post-order) re-accumulation of FIT element heights. Only FIT
    // containers are touched — Fixed/Percent/Grow heights are owned by other
    // passes. Mirrors the CloseElement FIT rule: a column sums child heights +
    // gaps + padding, a row takes the tallest child + padding.
    private void RecomputeFitHeights()
    {
        for (int r = 0; r < LayoutElementTreeRoots.Length; r++)
            RecomputeFitHeightsRecursive(LayoutElementTreeRoots[r].LayoutElementIndex);
    }

    private float RecomputeFitHeightsRecursive(int elementIndex)
    {
        // Re-fetch by index after recursing (the elements array never resizes
        // here, but avoid holding a ref across the child calls regardless).
        if (LayoutElements[elementIndex].IsTextElement)
            return LayoutElements[elementIndex].Dimensions.Height;

        int childStart = LayoutElements[elementIndex].Children.StartIndex;
        int childCount = LayoutElements[elementIndex].Children.Length;
        for (int i = 0; i < childCount; i++)
            RecomputeFitHeightsRecursive(LayoutElementChildren[childStart + i]);

        ref var el = ref LayoutElements[elementIndex];
        ref var cfg = ref LayoutConfigs[el.LayoutConfigIndex];
        if (cfg.Sizing.Height.Type != SizingType.Fit)
            return el.Dimensions.Height;

        float padV = cfg.Padding.Top + cfg.Padding.Bottom;
        float height;
        if (cfg.Direction == LayoutDirection.TopToBottom)
        {
            height = padV;
            for (int i = 0; i < childCount; i++)
                height += LayoutElements[LayoutElementChildren[childStart + i]].Dimensions.Height;
            height += Math.Max(childCount - 1, 0) * cfg.ChildGap;
        }
        else
        {
            float maxChild = 0;
            for (int i = 0; i < childCount; i++)
                maxChild = Math.Max(maxChild, LayoutElements[LayoutElementChildren[childStart + i]].Dimensions.Height);
            height = maxChild + padV;
        }

        float maxHeight = cfg.Sizing.Height.MinMax.Max > 0 ? cfg.Sizing.Height.MinMax.Max : float.MaxValue;
        el.Dimensions.Height = Math.Clamp(height, cfg.Sizing.Height.MinMax.Min, maxHeight);
        return el.Dimensions.Height;
    }

    private void SizeContainersAlongAxis(bool xAxis)
    {
        for (int rootIndex = 0; rootIndex < LayoutElementTreeRoots.Length; rootIndex++)
        {
            ref var root = ref LayoutElementTreeRoots[rootIndex];
            ref var rootElement = ref LayoutElements[root.LayoutElementIndex];

            // BFS traversal (reuse list to avoid per-root allocation)
            _bfsQueue.Clear();
            _bfsQueue.Add(root.LayoutElementIndex);
            var queue = _bfsQueue;

            for (int qi = 0; qi < queue.Count; qi++)
            {
                int parentIndex = queue[qi];
                ref var parent = ref LayoutElements[parentIndex];
                ref var layoutConfig = ref LayoutConfigs[parent.LayoutConfigIndex];

                float parentSize = xAxis ? parent.Dimensions.Width : parent.Dimensions.Height;
                float parentPadding = xAxis
                    ? layoutConfig.Padding.Left + layoutConfig.Padding.Right
                    : layoutConfig.Padding.Top + layoutConfig.Padding.Bottom;

                bool sizingAlongAxis = (xAxis && layoutConfig.Direction == LayoutDirection.LeftToRight) ||
                                       (!xAxis && layoutConfig.Direction == LayoutDirection.TopToBottom);

                float availableSpace = parentSize - parentPadding;
                if (sizingAlongAxis)
                {
                    availableSpace -= Math.Max(0, parent.Children.Length - 1) * layoutConfig.ChildGap;
                }

                // Pre-pass: sum Fixed/Fit children along the main axis so that
                // Percent children are computed relative to the remaining space
                // (i.e. parent minus non-flexible siblings like splitters).
                float fixedFitUsedSpace = 0;
                if (sizingAlongAxis)
                {
                    for (int i = 0; i < parent.Children.Length; i++)
                    {
                        int ci = LayoutElementChildren[parent.Children.StartIndex + i];
                        ref var ch = ref LayoutElements[ci];
                        ref var chLayout = ref LayoutConfigs[ch.LayoutConfigIndex];
                        var chSizing = xAxis ? chLayout.Sizing.Width : chLayout.Sizing.Height;
                        if (chSizing.Type != SizingType.Grow && chSizing.Type != SizingType.Percent)
                        {
                            fixedFitUsedSpace += xAxis ? ch.Dimensions.Width : ch.Dimensions.Height;
                        }
                    }
                }

                float percentAvailableSpace = availableSpace - fixedFitUsedSpace;

                // First pass: calculate space used by non-grow elements and count grow elements
                float usedSpace = 0;
                int growCount = 0;
                float totalGrowWeight = 0;

                for (int i = 0; i < parent.Children.Length; i++)
                {
                    int childIndex = LayoutElementChildren[parent.Children.StartIndex + i];
                    ref var child = ref LayoutElements[childIndex];

                    if (!child.IsTextElement && child.Children.Length > 0)
                    {
                        queue.Add(childIndex);
                    }

                    // Text reflow: a text leaf is measured single-line at AddText
                    // time (FIT), so it overflows a narrower container. Now that the
                    // parent's width is resolved (top-down BFS), re-wrap any text
                    // child to the available content width — width shrinks to the
                    // widest wrapped line, height grows with the line count.
                    // RecomputeFitHeights (run after this pass) propagates the new
                    // height up through FIT ancestors. Height-only effect here;
                    // widths of FIT ancestors were already framed by non-text
                    // siblings / fixed sizing in CloseElement.
                    if (xAxis && child.IsTextElement && TextMeasurer != null
                        && availableSpace > 0 && child.Dimensions.Width > availableSpace)
                    {
                        int tcIndex = FindConfigIndex(ref child, ElementConfigType.Text);
                        // WrapMode.None keeps the single-line AddText measurement:
                        // the text overflows its container (the parent clips it
                        // when ClipContent is set) instead of reflowing.
                        if (tcIndex >= 0 && TextElementConfigs[tcIndex].WrapMode != TextWrapMode.None)
                        {
                            ref var td = ref TextElementData[child.TextData.Index];
                            ref var tc = ref TextElementConfigs[tcIndex];
                            var wrapped = TextMeasurer.MeasureText(
                                td.Text.AsSpan(), tc.FontId, tc.FontSize, tc.LetterSpacing, availableSpace);
                            if (tc.LineHeight > 0 && tc.FontSize > 0)
                                wrapped.Height *= tc.LineHeight / (float)tc.FontSize;
                            child.Dimensions.Width = Math.Min(wrapped.Width, availableSpace);
                            child.Dimensions.Height = wrapped.Height;
                            td.PreferredDimensions = child.Dimensions;
                        }
                    }

                    ref var childLayoutConfig = ref LayoutConfigs[child.LayoutConfigIndex];
                    var childSizing = xAxis ? childLayoutConfig.Sizing.Width : childLayoutConfig.Sizing.Height;

                    if (sizingAlongAxis)
                    {
                        // Along the main axis
                        if (childSizing.Type == SizingType.Grow)
                        {
                            growCount++;
                            totalGrowWeight += 1.0f; // Could be weighted in the future
                            usedSpace += childSizing.MinMax.Min;
                        }
                        else if (childSizing.Type == SizingType.Percent)
                        {
                            float size = percentAvailableSpace * childSizing.Percent;
                            if (xAxis)
                                child.Dimensions.Width = size;
                            else
                                child.Dimensions.Height = size;
                            usedSpace += size;
                        }
                        else
                        {
                            // Fit or Fixed - use calculated dimension
                            usedSpace += xAxis ? child.Dimensions.Width : child.Dimensions.Height;
                        }
                    }
                    else
                    {
                        // Cross axis - handle Grow and Percent
                        if (childSizing.Type == SizingType.Grow)
                        {
                            float maxSize = childSizing.MinMax.Max > 0 ? childSizing.MinMax.Max : float.MaxValue;
                            float size = Math.Clamp(availableSpace, childSizing.MinMax.Min, maxSize);
                            if (xAxis)
                                child.Dimensions.Width = size;
                            else
                                child.Dimensions.Height = size;
                        }
                        else if (childSizing.Type == SizingType.Percent)
                        {
                            float size = availableSpace * childSizing.Percent;
                            if (xAxis)
                                child.Dimensions.Width = size;
                            else
                                child.Dimensions.Height = size;
                        }
                    }
                }

                // Second pass: distribute remaining space to grow elements (along main axis)
                if (sizingAlongAxis && growCount > 0)
                {
                    float remainingSpace = availableSpace - usedSpace;
                    if (remainingSpace > 0)
                    {
                        float spacePerGrow = remainingSpace / totalGrowWeight;

                        for (int i = 0; i < parent.Children.Length; i++)
                        {
                            int childIndex = LayoutElementChildren[parent.Children.StartIndex + i];
                            ref var child = ref LayoutElements[childIndex];
                            ref var childLayoutConfig = ref LayoutConfigs[child.LayoutConfigIndex];
                            var childSizing = xAxis ? childLayoutConfig.Sizing.Width : childLayoutConfig.Sizing.Height;

                            if (childSizing.Type == SizingType.Grow)
                            {
                                float maxSize = childSizing.MinMax.Max > 0 ? childSizing.MinMax.Max : float.MaxValue;
                                float newSize = childSizing.MinMax.Min + spacePerGrow;
                                newSize = Math.Clamp(newSize, childSizing.MinMax.Min, maxSize);
                                if (xAxis)
                                    child.Dimensions.Width = newSize;
                                else
                                    child.Dimensions.Height = newSize;
                            }
                        }
                    }
                }
            }
        }
    }

    private void PositionElements()
    {
        // Build scroll container index for O(1) lookup during positioning
        _scrollContainerIndex.Clear();
        for (int i = 0; i < ScrollContainerDatas.Length; i++)
        {
            _scrollContainerIndex[ScrollContainerDatas[i].ElementId] = i;
        }

        for (int rootIndex = 0; rootIndex < LayoutElementTreeRoots.Length; rootIndex++)
        {
            ref var root = ref LayoutElementTreeRoots[rootIndex];
            ref var rootElement = ref LayoutElements[root.LayoutElementIndex];

            Vector2 rootPosition = Vector2.Zero;

            if (HasConfig(ref rootElement, ElementConfigType.Floating))
            {
                int hashIndex = GetHashMapItemIndex(root.ParentId);
                if (hashIndex >= 0)
                {
                    rootPosition = LayoutElementsHashMapInternal[hashIndex].BoundingBox.Position;
                }

                // Apply floating offset from FloatingConfig
                int floatingConfigIndex = FindConfigIndex(ref rootElement, ElementConfigType.Floating);
                if (floatingConfigIndex >= 0)
                {
                    ref var floatingConfig = ref FloatingElementConfigs[floatingConfigIndex];
                    rootPosition.X += floatingConfig.Offset.X;
                    rootPosition.Y += floatingConfig.Offset.Y;
                }
            }

            PositionElementRecursive(root.LayoutElementIndex, rootPosition);
        }
    }

    private void PositionElementRecursive(int elementIndex, Vector2 position)
    {
        ref var element = ref LayoutElements[elementIndex];
        ref var layoutConfig = ref LayoutConfigs[element.LayoutConfigIndex];

        // Store element position
        int hashIndex = GetHashMapItemIndex(element.Id);
        if (hashIndex >= 0)
        {
            LayoutElementsHashMapInternal[hashIndex].BoundingBox = new BoundingBox(position, element.Dimensions);
        }

        Vector2 childOffset = new Vector2(
            position.X + layoutConfig.Padding.Left,
            position.Y + layoutConfig.Padding.Top
        );

        // Check if this is a scroll container and apply scroll offset
        Vector2 scrollOffset = Vector2.Zero;
        int scrollContainerIndex = -1;
        int scrollConfigIndex = FindConfigIndex(ref element, ElementConfigType.Scroll);
        if (scrollConfigIndex >= 0)
        {
            // Find the scroll container data via indexed lookup
            if (_scrollContainerIndex.TryGetValue(element.Id, out int sci))
            {
                scrollContainerIndex = sci;
                scrollOffset = ScrollContainerDatas[sci].ScrollPosition;
                // Update bounding box
                ScrollContainerDatas[sci].BoundingBox = new BoundingBox(position, element.Dimensions);
            }
            childOffset.X -= scrollOffset.X;
            childOffset.Y -= scrollOffset.Y;
        }

        float contentWidth = element.Dimensions.Width - layoutConfig.Padding.Left - layoutConfig.Padding.Right;
        float contentHeight = element.Dimensions.Height - layoutConfig.Padding.Top - layoutConfig.Padding.Bottom;

        float totalChildrenSize = 0;
        float maxChildCrossSize = 0;
        for (int i = 0; i < element.Children.Length; i++)
        {
            int childIndex = LayoutElementChildren[element.Children.StartIndex + i];
            ref var child = ref LayoutElements[childIndex];

            if (layoutConfig.Direction == LayoutDirection.LeftToRight)
            {
                totalChildrenSize += child.Dimensions.Width;
                maxChildCrossSize = Math.Max(maxChildCrossSize, child.Dimensions.Height);
            }
            else
            {
                totalChildrenSize += child.Dimensions.Height;
                maxChildCrossSize = Math.Max(maxChildCrossSize, child.Dimensions.Width);
            }
        }
        totalChildrenSize += Math.Max(0, element.Children.Length - 1) * layoutConfig.ChildGap;

        // Apply alignment
        if (layoutConfig.Direction == LayoutDirection.LeftToRight)
        {
            switch (layoutConfig.ChildAlignment.X)
            {
                case AlignX.Center:
                    childOffset.X += (contentWidth - totalChildrenSize) / 2;
                    break;
                case AlignX.Right:
                    childOffset.X += contentWidth - totalChildrenSize;
                    break;
            }
        }
        else
        {
            switch (layoutConfig.ChildAlignment.Y)
            {
                case AlignY.Center:
                    childOffset.Y += (contentHeight - totalChildrenSize) / 2;
                    break;
                case AlignY.Bottom:
                    childOffset.Y += contentHeight - totalChildrenSize;
                    break;
            }
        }

        // Position children
        for (int i = 0; i < element.Children.Length; i++)
        {
            int childIndex = LayoutElementChildren[element.Children.StartIndex + i];
            ref var child = ref LayoutElements[childIndex];

            Vector2 childPos = childOffset;

            // Cross-axis alignment
            if (layoutConfig.Direction == LayoutDirection.LeftToRight)
            {
                switch (layoutConfig.ChildAlignment.Y)
                {
                    case AlignY.Center:
                        childPos.Y += (contentHeight - child.Dimensions.Height) / 2;
                        break;
                    case AlignY.Bottom:
                        childPos.Y += contentHeight - child.Dimensions.Height;
                        break;
                }
            }
            else
            {
                switch (layoutConfig.ChildAlignment.X)
                {
                    case AlignX.Center:
                        childPos.X += (contentWidth - child.Dimensions.Width) / 2;
                        break;
                    case AlignX.Right:
                        childPos.X += contentWidth - child.Dimensions.Width;
                        break;
                }
            }

            if (!child.IsTextElement)
            {
                PositionElementRecursive(childIndex, childPos);
            }
            else
            {
                int childHashIndex = GetHashMapItemIndex(child.Id);
                if (childHashIndex >= 0)
                {
                    LayoutElementsHashMapInternal[childHashIndex].BoundingBox = new BoundingBox(childPos, child.Dimensions);
                }
            }

            if (layoutConfig.Direction == LayoutDirection.LeftToRight)
                childOffset.X += child.Dimensions.Width + layoutConfig.ChildGap;
            else
                childOffset.Y += child.Dimensions.Height + layoutConfig.ChildGap;
        }

        // Update scroll container content size and clamp scroll position
        if (scrollContainerIndex >= 0)
        {
            float contentSizeW, contentSizeH;
            if (layoutConfig.Direction == LayoutDirection.LeftToRight)
            {
                contentSizeW = totalChildrenSize + layoutConfig.Padding.Left + layoutConfig.Padding.Right;
                contentSizeH = maxChildCrossSize + layoutConfig.Padding.Top + layoutConfig.Padding.Bottom;
            }
            else
            {
                contentSizeW = maxChildCrossSize + layoutConfig.Padding.Left + layoutConfig.Padding.Right;
                contentSizeH = totalChildrenSize + layoutConfig.Padding.Top + layoutConfig.Padding.Bottom;
            }
            ScrollContainerDatas[scrollContainerIndex].ContentSize = new Dimensions(contentSizeW, contentSizeH);

            // Clamp scroll position to valid range (handles window resize making content fit)
            ref var sd = ref ScrollContainerDatas[scrollContainerIndex];
            float maxScrollX = Math.Max(0, sd.ContentSize.Width - sd.BoundingBox.Width);
            float maxScrollY = Math.Max(0, sd.ContentSize.Height - sd.BoundingBox.Height);
            sd.ScrollPosition.X = Math.Clamp(sd.ScrollPosition.X, 0, maxScrollX);
            sd.ScrollPosition.Y = Math.Clamp(sd.ScrollPosition.Y, 0, maxScrollY);
        }
    }

    private void GenerateRenderCommands()
    {
        // Sort tree roots by ZIndex so higher z-index elements are rendered last (on top)
        // Insertion sort: O(N) for nearly-sorted data (typical case), O(N²) worst case
        var roots = LayoutElementTreeRoots.AsSpan();
        for (int i = 1; i < roots.Length; i++)
        {
            var key = roots[i];
            int j = i - 1;
            while (j >= 0 && roots[j].ZIndex > key.ZIndex)
            {
                roots[j + 1] = roots[j];
                j--;
            }
            roots[j + 1] = key;
        }

        for (int rootIndex = 0; rootIndex < LayoutElementTreeRoots.Length; rootIndex++)
        {
            ref var root = ref LayoutElementTreeRoots[rootIndex];
            GenerateRenderCommandsRecursive(root.LayoutElementIndex, root.ZIndex, default, false);
        }
    }

    private void GenerateRenderCommandsRecursive(int elementIndex, short zIndex, BoundingBox clipBounds, bool hasClip)
    {
        ref var element = ref LayoutElements[elementIndex];
        int hashIndex = GetHashMapItemIndex(element.Id);
        if (hashIndex < 0)
            return;

        ref var hashItem = ref LayoutElementsHashMapInternal[hashIndex];
        hashItem.HasClipBounds = hasClip;
        hashItem.ClipBounds = clipBounds;

        var boundingBox = hashItem.BoundingBox;

        // Culling against viewport
        if (!CullingDisabled)
        {
            if (boundingBox.Right < 0 || boundingBox.Bottom < 0 ||
                boundingBox.X > LayoutDimensions.Width || boundingBox.Y > LayoutDimensions.Height)
            {
                return;
            }

            // Culling against scroll container clip bounds
            if (hasClip)
            {
                if (boundingBox.Right < clipBounds.X || boundingBox.Bottom < clipBounds.Y ||
                    boundingBox.X > clipBounds.Right || boundingBox.Y > clipBounds.Bottom)
                {
                    return;
                }
            }
        }

        // Find configs
        int sharedIndex = FindConfigIndex(ref element, ElementConfigType.Shared);
        int scrollIndex = FindConfigIndex(ref element, ElementConfigType.Scroll);
        int shadowIndex = FindConfigIndex(ref element, ElementConfigType.Shadow);

        SharedElementConfig sharedConfig = sharedIndex >= 0 ? SharedElementConfigs[sharedIndex] : default;

        // Shadow render command (before rectangle so it renders behind)
        if (shadowIndex >= 0)
        {
            ref var shadowConfig = ref ShadowElementConfigs[shadowIndex];
            // Use the shadow's own corner radius, or fall back to the element's
            var cornerRadius = shadowConfig.CornerRadius.HasRadius
                ? shadowConfig.CornerRadius
                : sharedConfig.CornerRadius;

            // Expand bounding box by offset, spread, and blur.
            // Extra 1px ensures the innermost shadow layer overlaps under the element,
            // avoiding a sub-pixel gap from anti-aliased rounded corners.
            float expand = shadowConfig.SpreadRadius + shadowConfig.BlurRadius + 1;
            var shadowBox = new BoundingBox
            {
                X = boundingBox.X + shadowConfig.OffsetX - expand,
                Y = boundingBox.Y + shadowConfig.OffsetY - expand,
                Width = boundingBox.Width + expand * 2,
                Height = boundingBox.Height + expand * 2
            };

            RenderCommands.Add(new RenderCommand
            {
                BoundingBox = shadowBox,
                CommandType = RenderCommandType.Shadow,
                Id = element.Id,
                ZIndex = zIndex,
                Shadow = new ShadowRenderData
                {
                    Color = shadowConfig.Color,
                    CornerRadius = cornerRadius,
                    OffsetX = shadowConfig.OffsetX,
                    OffsetY = shadowConfig.OffsetY,
                    BlurRadius = shadowConfig.BlurRadius,
                    SpreadRadius = shadowConfig.SpreadRadius
                }
            });
        }

        // Rectangle render command
        if (sharedIndex >= 0 && sharedConfig.BackgroundColor.A > 0)
        {
            RenderCommands.Add(new RenderCommand
            {
                BoundingBox = boundingBox,
                CommandType = RenderCommandType.Rectangle,
                Id = element.Id,
                ZIndex = zIndex,
                Rectangle = new RectangleRenderData
                {
                    BackgroundColor = sharedConfig.BackgroundColor,
                    CornerRadius = sharedConfig.CornerRadius
                }
            });
        }

        // Image (skin background — drawn behind children/text)
        int imageIndex = FindConfigIndex(ref element, ElementConfigType.Image);
        if (imageIndex >= 0)
        {
            ref var imageConfig = ref ImageElementConfigs[imageIndex];
            RenderCommands.Add(new RenderCommand
            {
                BoundingBox = boundingBox,
                CommandType = RenderCommandType.Image,
                Id = element.Id,
                ZIndex = zIndex,
                Image = new ImageRenderData
                {
                    ImageData = imageConfig.ImageData,
                    SourceDimensions = imageConfig.SourceDimensions,
                    BackgroundColor = imageConfig.Tint.A > 0 ? imageConfig.Tint : sharedConfig.BackgroundColor,
                    CornerRadius = sharedConfig.CornerRadius,
                    Slice = imageConfig.Slice
                }
            });
        }

        // Custom (a background fill, e.g. a UO gump sprite) — emitted with the
        // other backgrounds BEFORE children so the element's own fill paints
        // behind its children/text. (Border stays after children below, so it
        // frames them on top.) Previously custom was emitted at the end, which
        // painted an opaque gump bg over its own labels — blank rows.
        int customIndex = FindConfigIndex(ref element, ElementConfigType.Custom);
        if (customIndex >= 0)
        {
            ref var customConfig = ref CustomElementConfigs[customIndex];
            RenderCommands.Add(new RenderCommand
            {
                BoundingBox = boundingBox,
                CommandType = RenderCommandType.Custom,
                Id = element.Id,
                ZIndex = zIndex,
                Custom = new CustomRenderData
                {
                    CustomData = customConfig.CustomData,
                    BackgroundColor = sharedConfig.BackgroundColor,
                    CornerRadius = sharedConfig.CornerRadius
                }
            });
        }

        // Scissor start (for scroll containers or ClipContent elements)
        bool needsScissor = scrollIndex >= 0 ||
            LayoutConfigs[element.LayoutConfigIndex].ClipContent;
        if (needsScissor)
        {
            ScrollRenderData scrollData = default;
            if (scrollIndex >= 0)
            {
                ref var scrollConfig = ref ScrollElementConfigs[scrollIndex];
                scrollData = new ScrollRenderData
                {
                    Horizontal = scrollConfig.Horizontal,
                    Vertical = scrollConfig.Vertical
                };
            }
            RenderCommands.Add(new RenderCommand
            {
                BoundingBox = boundingBox,
                CommandType = RenderCommandType.ScissorStart,
                Id = element.Id,
                ZIndex = zIndex,
                Scroll = scrollData
            });
        }

        // If this element clips children, its bounding box clips children
        var childClip = clipBounds;
        bool childHasClip = hasClip;
        if (needsScissor)
        {
            childClip = boundingBox;
            childHasClip = true;
        }

        // Process children
        for (int i = 0; i < element.Children.Length; i++)
        {
            int childIndex = LayoutElementChildren[element.Children.StartIndex + i];
            ref var child = ref LayoutElements[childIndex];

            if (child.IsTextElement)
            {
                int childHashIndex = GetHashMapItemIndex(child.Id);
                if (childHashIndex < 0) continue;

                // Store clip bounds on text elements too
                ref var childHashItem = ref LayoutElementsHashMapInternal[childHashIndex];
                childHashItem.HasClipBounds = childHasClip;
                childHashItem.ClipBounds = childClip;

                // Cull text elements outside scroll container bounds
                if (!CullingDisabled && childHasClip)
                {
                    var childBox = childHashItem.BoundingBox;
                    if (childBox.Right < childClip.X || childBox.Bottom < childClip.Y ||
                        childBox.X > childClip.Right || childBox.Y > childClip.Bottom)
                    {
                        continue;
                    }
                }

                int textConfigIndex = FindConfigIndex(ref child, ElementConfigType.Text);
                if (textConfigIndex < 0) continue;

                ref var textConfig = ref TextElementConfigs[textConfigIndex];
                ref var textData = ref TextElementData[child.TextData.Index];

                RenderCommands.Add(new RenderCommand
                {
                    BoundingBox = LayoutElementsHashMapInternal[childHashIndex].BoundingBox,
                    CommandType = RenderCommandType.Text,
                    Id = child.Id,
                    ZIndex = zIndex,
                    Text = new TextRenderData
                    {
                        Text = textData.Text,
                        TextColor = textConfig.TextColor,
                        FontId = textConfig.FontId,
                        FontSize = textConfig.FontSize,
                        LetterSpacing = textConfig.LetterSpacing,
                        LineHeight = textConfig.LineHeight
                    }
                });
            }
            else
            {
                GenerateRenderCommandsRecursive(childIndex, zIndex, childClip, childHasClip);
            }
        }

        // Scissor end
        if (needsScissor)
        {
            RenderCommands.Add(new RenderCommand
            {
                BoundingBox = boundingBox,
                CommandType = RenderCommandType.ScissorEnd,
                Id = element.Id,
                ZIndex = zIndex
            });
        }

        // Border — emitted after children so it frames them on top.
        int borderIndex = FindConfigIndex(ref element, ElementConfigType.Border);
        if (borderIndex >= 0)
        {
            ref var borderConfig = ref BorderElementConfigs[borderIndex];
            if (borderConfig.Width.HasBorder)
            {
                RenderCommands.Add(new RenderCommand
                {
                    BoundingBox = boundingBox,
                    CommandType = RenderCommandType.Border,
                    Id = element.Id,
                    ZIndex = zIndex,
                    Border = new BorderRenderData
                    {
                        Color = borderConfig.Color,
                        Width = borderConfig.Width,
                        CornerRadius = sharedConfig.CornerRadius
                    }
                });
            }
        }
    }

    public void SetPointerState(Vector2 position, bool pressed)
    {
        var previousState = PointerInfo.State;
        PointerInfo.Position = position;

        if (pressed)
        {
            PointerInfo.State = previousState == PointerInteractionState.Released ||
                                previousState == PointerInteractionState.ReleasedThisFrame
                ? PointerInteractionState.PressedThisFrame
                : PointerInteractionState.Pressed;
        }
        else
        {
            PointerInfo.State = previousState == PointerInteractionState.Pressed ||
                                previousState == PointerInteractionState.PressedThisFrame
                ? PointerInteractionState.ReleasedThisFrame
                : PointerInteractionState.Released;
        }

        RebuildPointerOverIds(position);
    }

    /// <summary>
    /// Rebuilds <see cref="PointerOverIds"/> for the current pointer position
    /// without advancing the pointer state machine. Call after <see cref="EndLayout"/>
    /// when hit-testing must run against the freshly computed layout.
    /// </summary>
    public void RefreshPointerOverIds() => RebuildPointerOverIds(PointerInfo.Position);

    private void RebuildPointerOverIds(Vector2 position)
    {
        PointerOverIds.Clear();
        _pointerOverBoxes.Clear();
        if (LayoutElementTreeRoots.Length == 0 || LayoutElements.Length == 0)
            return;

        // Iterate roots topmost-first. EndLayout sorts roots ascending by ZIndex,
        // so reverse iteration visits highest-ZIndex root first.
        for (int rootIndex = LayoutElementTreeRoots.Length - 1; rootIndex >= 0; rootIndex--)
        {
            ref var root = ref LayoutElementTreeRoots[rootIndex];
            var pointerOffset = root.PointerOffset;
            var adjustedPos = new Vector2(position.X + pointerOffset.X, position.Y + pointerOffset.Y);

            _pointerDfsStack.Clear();
            _pointerDfsVisited.Clear();
            _pointerDfsStack.Add(root.LayoutElementIndex);
            _pointerDfsVisited.Add(false);

            bool found = false;
            while (_pointerDfsStack.Length > 0)
            {
                int depth = _pointerDfsStack.Length - 1;
                int elementIndex = _pointerDfsStack[depth];

                if (_pointerDfsVisited[depth])
                {
                    // Post-order add: children already processed.
                    int hashIdx = GetHashMapItemIndex(LayoutElements[elementIndex].Id);
                    if (hashIdx >= 0)
                    {
                        ref var item = ref LayoutElementsHashMapInternal[hashIdx];
                        // Bounding box + clip-bounds gate membership here, because the
                        // element hash map is cleared at BeginLayout and PointerOver can no
                        // longer re-query it mid-frame. CustomHitTest is NOT applied here —
                        // it is a per-query callback (PointerOver), so it runs there using
                        // the bounding box snapshotted alongside the id.
                        if (item.BoundingBox.Contains(adjustedPos) &&
                            (!item.HasClipBounds || item.ClipBounds.Contains(adjustedPos)))
                        {
                            PointerOverIds.Add(item.ElementId);
                            _pointerOverBoxes.Add(item.BoundingBox);
                            found = true;
                        }
                    }
                    _pointerDfsStack.Length--;
                    _pointerDfsVisited.Length--;
                    continue;
                }

                _pointerDfsVisited[depth] = true;
                ref var element = ref LayoutElements[elementIndex];

                // Text leaves: no children to descend into.
                if (HasConfig(ref element, ElementConfigType.Text))
                    continue;

                // Push children forward so last-child pops first (topmost subtree first).
                int childStart = element.Children.StartIndex;
                int childLen = element.Children.Length;
                for (int i = 0; i < childLen; i++)
                {
                    _pointerDfsStack.Add(LayoutElementChildren[childStart + i]);
                    _pointerDfsVisited.Add(false);
                }
            }

            // Floating root with Capture mode swallows pointer events: don't continue
            // into roots underneath it.
            if (!found) continue;
            ref var rootElement = ref LayoutElements[root.LayoutElementIndex];
            int floatingIdx = FindConfigIndex(ref rootElement, ElementConfigType.Floating);
            if (floatingIdx >= 0 &&
                FloatingElementConfigs[floatingIdx].PointerCaptureMode == PointerCaptureMode.Capture)
            {
                break;
            }
        }
    }

    public void SetLayoutDimensions(Dimensions dimensions)
    {
        LayoutDimensions = dimensions;
    }

    public bool PointerOver(ElementId elementId)
    {
        // Membership test against the cached hit list. The element hash map is
        // cleared every frame at BeginLayout, so a fresh GetHashMapItemIndex query
        // here would miss elements during the build pass (immediate-mode widgets
        // call PointerOver while laying themselves out). PointerOverIds is built by
        // SetPointerState against the previous frame's map — bounding box, clip
        // bounds and CustomHitTest are all applied there. Mirrors Clay_PointerOver.
        for (int i = 0; i < PointerOverIds.Length; i++)
        {
            if (PointerOverIds[i].Id != elementId.Id)
                continue;

            // Element passed the bounding-box / clip-bounds gate at hit-test time.
            // CustomHitTest gets the final say, using the snapshotted bounds.
            if (CustomHitTest != null)
                return CustomHitTest(elementId, _pointerOverBoxes[i], PointerInfo.Position);

            return true;
        }
        return false;
    }

    public ElementData GetElementData(ElementId id)
    {
        int hashIndex = GetHashMapItemIndex(id.Id);
        if (hashIndex < 0)
            return new ElementData { Found = false };

        return new ElementData
        {
            BoundingBox = LayoutElementsHashMapInternal[hashIndex].BoundingBox,
            Found = true
        };
    }

    public void UpdateScrollContainers(bool enableDragScrolling, Vector2 scrollDelta, float deltaTime)
    {
        // Prune scroll containers whose element was NOT laid out last frame (ports
        // upstream Clay_UpdateScrollContainers). ScrollContainerDatas is keyed by
        // element id and otherwise persists forever — so a recycled entity index
        // inherits the dead element's scroll offset and renders scrolled out of
        // view ("vanishes"). This runs at frame start (before BeginLayout), so
        // OpenThisFrame still reflects the previous frame's layout. Iterate
        // backwards so RemoveSwapback doesn't skip survivors.
        for (int i = ScrollContainerDatas.Length - 1; i >= 0; i--)
        {
            ref var sc = ref ScrollContainerDatas[i];
            if (!sc.OpenThisFrame || GetHashMapItemIndex(sc.ElementId) < 0)
            {
                ScrollContainerDatas.RemoveSwapback(i);
                continue;
            }
            sc.OpenThisFrame = false;
        }

        // Note: scroll delta for text inputs is forwarded separately after BeginLayout
        // (via TextInputScrollDelta) to avoid being cleared by TextInput.BeginFrame().

        // If a focused multiline text input is hovered, it consumes the scroll —
        // don't pass it to parent scroll containers.
        if (scrollDelta.Y != 0 && TextInput?.FocusedWidget is { } focused && !focused.SingleLine)
        {
            var focusedId = new ElementId { Id = TextInput.FocusedElementId };
            var data = GetElementData(focusedId);
            if (data.Found && data.BoundingBox.Contains(PointerInfo.Position))
                return;
        }

        // Find innermost scroll container under pointer (iterate in reverse so children take priority)
        for (int i = ScrollContainerDatas.Length - 1; i >= 0; i--)
        {
            ref var scrollData = ref ScrollContainerDatas[i];

            // Check if pointer is over this scroll container
            if (scrollData.BoundingBox.Contains(PointerInfo.Position))
            {
                // Get the scroll config for this container
                int elementIndex = scrollData.LayoutElementIndex;
                if (elementIndex < 0 || elementIndex >= LayoutElements.Length)
                    continue;

                ref var element = ref LayoutElements[elementIndex];
                int scrollConfigIndex = FindConfigIndex(ref element, ElementConfigType.Scroll);
                if (scrollConfigIndex < 0)
                    continue;

                ref var scrollConfig = ref ScrollElementConfigs[scrollConfigIndex];

                // Redirect vertical wheel to horizontal when the container only scrolls
                // horizontally, or has no vertical overflow. For explicit Shift+Wheel,
                // the caller should remap scrollDelta before passing it in.
                float effectiveDeltaX = scrollDelta.X;
                float effectiveDeltaY = scrollDelta.Y;

                if (scrollConfig.Horizontal && scrollDelta.Y != 0)
                {
                    bool hasVerticalOverflow = scrollData.ContentSize.Height > scrollData.BoundingBox.Height;
                    if (!scrollConfig.Vertical || !hasVerticalOverflow)
                    {
                        effectiveDeltaX += effectiveDeltaY;
                        effectiveDeltaY = 0;
                    }
                }

                // Apply scroll delta
                if (scrollConfig.Vertical && effectiveDeltaY != 0)
                {
                    float maxScroll = Math.Max(0, scrollData.ContentSize.Height - scrollData.BoundingBox.Height);
                    scrollData.ScrollPosition.Y = Math.Clamp(
                        scrollData.ScrollPosition.Y - effectiveDeltaY * 30,
                        0, maxScroll
                    );
                }

                if (scrollConfig.Horizontal && effectiveDeltaX != 0)
                {
                    float maxScroll = Math.Max(0, scrollData.ContentSize.Width - scrollData.BoundingBox.Width);
                    scrollData.ScrollPosition.X = Math.Clamp(
                        scrollData.ScrollPosition.X - effectiveDeltaX * 30,
                        0, maxScroll
                    );
                }

                break; // Only scroll the topmost container
            }
        }
    }

    public ScrollContainerData GetScrollContainerData(ElementId elementId)
    {
        for (int i = 0; i < ScrollContainerDatas.Length; i++)
        {
            ref var scrollData = ref ScrollContainerDatas[i];
            if (scrollData.ElementId == elementId.Id)
            {
                return new ScrollContainerData
                {
                    ScrollPosition = scrollData.ScrollPosition,
                    ScrollContainerDimensions = new Dimensions(scrollData.BoundingBox.Width, scrollData.BoundingBox.Height),
                    ContentDimensions = scrollData.ContentSize,
                    Found = true
                };
            }
        }
        return default;
    }

    /// <summary>
    /// Sets the scroll position for a scroll container.
    /// </summary>
    public void SetScrollPosition(ElementId elementId, Vector2 position)
    {
        for (int i = 0; i < ScrollContainerDatas.Length; i++)
        {
            ref var scrollData = ref ScrollContainerDatas[i];
            if (scrollData.ElementId == elementId.Id)
            {
                scrollData.ScrollPosition = position;
                scrollData.ScrollMomentum = default;
                return;
            }
        }
    }

    /// <summary>
    /// Resets the scroll position for a scroll container to (0, 0).
    /// </summary>
    public void ResetScrollPosition(ElementId elementId)
    {
        SetScrollPosition(elementId, default);
    }

    public void Dispose()
    {
        // No unmanaged resources to dispose
    }
}

/// <summary>
/// Internal scroll container tracking data.
/// </summary>
public struct ScrollContainerDataInternal
{
    public int LayoutElementIndex;
    public BoundingBox BoundingBox;
    public Dimensions ContentSize;
    public Vector2 ScrollOrigin;
    public Vector2 PointerOrigin;
    public Vector2 ScrollMomentum;
    public Vector2 ScrollPosition;
    public Vector2 PreviousDelta;
    public float MomentumTime;
    public uint ElementId;
    public bool OpenThisFrame;
    public bool PointerScrollActive;
}
