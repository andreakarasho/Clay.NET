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
/// The main Clay context containing all state for layout computation.
/// </summary>
public class ClayContext : IDisposable
{
    // Configuration
    public int MaxElementCount;
    public ErrorHandler? OnError;

    // State
    public BooleanWarnings Warnings;
    public PointerData PointerInfo;
    public Dimensions LayoutDimensions;
    public uint Generation;
    public bool DebugModeEnabled;
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
    public ClayList<SharedElementConfig> SharedElementConfigs;

    // Tree structures
    public ClayList<LayoutElementTreeRoot> LayoutElementTreeRoots;
    public ClayList<LayoutElementHashMapItem> LayoutElementsHashMapInternal;
    public ClayList<int> LayoutElementsHashMap;
    public ClayList<int> OpenClipElementStack;
    public ClayList<ScrollContainerDataInternal> ScrollContainerDatas;

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
        SharedElementConfigs = new ClayList<SharedElementConfig>(maxElementCount);

        LayoutElementTreeRoots = new ClayList<LayoutElementTreeRoot>(256);
        LayoutElementsHashMapInternal = new ClayList<LayoutElementHashMapItem>(maxElementCount);
        LayoutElementsHashMap = new ClayList<int>(maxElementCount);
        OpenClipElementStack = new ClayList<int>(64);
        ScrollContainerDatas = new ClayList<ScrollContainerDataInternal>(16);
    }

    /// <summary>
    /// Begins a new layout frame.
    /// </summary>
    public void BeginLayout()
    {
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
        SharedElementConfigs.Clear();

        LayoutElementTreeRoots.Clear();

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
        return RenderCommands.AsReadOnlySpan();
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

        // Store layout config
        int layoutConfigIndex = LayoutConfigs.Add(declaration.Layout);
        openLayoutElement.LayoutConfigIndex = layoutConfigIndex;

        // Validate percent sizing
        if ((declaration.Layout.Sizing.Width.Type == SizingType.Percent && declaration.Layout.Sizing.Width.Percent > 1) ||
            (declaration.Layout.Sizing.Height.Type == SizingType.Percent && declaration.Layout.Sizing.Height.Percent > 1))
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
        if (declaration.BackgroundColor.A > 0 || declaration.CornerRadius.HasRadius || declaration.UserData != 0)
        {
            int sharedIndex = SharedElementConfigs.Add(new SharedElementConfig
            {
                BackgroundColor = declaration.BackgroundColor,
                CornerRadius = declaration.CornerRadius,
                UserData = declaration.UserData
            });
            ElementConfigs.Add(new ElementConfig { Type = ElementConfigType.Shared, ConfigIndex = sharedIndex });
            openLayoutElement.ElementConfigsLength++;
        }

        // Process image config
        if (declaration.Image.HasImage)
        {
            int imageIndex = ImageElementConfigs.Add(declaration.Image);
            ElementConfigs.Add(new ElementConfig { Type = ElementConfigType.Image, ConfigIndex = imageIndex });
            openLayoutElement.ElementConfigsLength++;
        }

        // Process floating config
        if (declaration.Floating.IsFloating)
        {
            var floatingConfig = declaration.Floating;

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
        if (declaration.Custom.HasCustomData)
        {
            int customIndex = CustomElementConfigs.Add(declaration.Custom);
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

        // Process scroll config
        if (declaration.Scroll.IsScrollable)
        {
            int scrollIndex = ScrollElementConfigs.Add(declaration.Scroll);
            ElementConfigs.Add(new ElementConfig { Type = ElementConfigType.Scroll, ConfigIndex = scrollIndex });
            openLayoutElement.ElementConfigsLength++;
            OpenClipElementStack.Add((int)openLayoutElement.Id);

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
        if (declaration.Border.HasBorder)
        {
            int borderIndex = BorderElementConfigs.Add(declaration.Border);
            ElementConfigs.Add(new ElementConfig { Type = ElementConfigType.Border, ConfigIndex = borderIndex });
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
                OpenClipElementStack.Length--;
                break;
            }
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
        else
        {
            openLayoutElement.Dimensions.Width = 0;
        }

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
        SizeContainersAlongAxis(false);
        PositionElements();
    }

    private void SizeContainersAlongAxis(bool xAxis)
    {
        for (int rootIndex = 0; rootIndex < LayoutElementTreeRoots.Length; rootIndex++)
        {
            ref var root = ref LayoutElementTreeRoots[rootIndex];
            ref var rootElement = ref LayoutElements[root.LayoutElementIndex];

            // BFS traversal
            var queue = new List<int> { root.LayoutElementIndex };

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
                            float size = availableSpace * childSizing.Percent;
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
            // Find the scroll container data
            for (int i = 0; i < ScrollContainerDatas.Length; i++)
            {
                if (ScrollContainerDatas[i].ElementId == element.Id)
                {
                    scrollContainerIndex = i;
                    scrollOffset = ScrollContainerDatas[i].ScrollPosition;
                    // Update bounding box
                    ScrollContainerDatas[i].BoundingBox = new BoundingBox(position, element.Dimensions);
                    break;
                }
            }
            childOffset.X -= scrollOffset.X;
            childOffset.Y -= scrollOffset.Y;
        }

        float contentWidth = element.Dimensions.Width - layoutConfig.Padding.Left - layoutConfig.Padding.Right;
        float contentHeight = element.Dimensions.Height - layoutConfig.Padding.Top - layoutConfig.Padding.Bottom;

        float totalChildrenSize = 0;
        for (int i = 0; i < element.Children.Length; i++)
        {
            int childIndex = LayoutElementChildren[element.Children.StartIndex + i];
            ref var child = ref LayoutElements[childIndex];

            if (layoutConfig.Direction == LayoutDirection.LeftToRight)
                totalChildrenSize += child.Dimensions.Width;
            else
                totalChildrenSize += child.Dimensions.Height;
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

        // Update scroll container content size
        if (scrollContainerIndex >= 0)
        {
            float contentSizeW = totalChildrenSize + layoutConfig.Padding.Left + layoutConfig.Padding.Right;
            float contentSizeH = totalChildrenSize + layoutConfig.Padding.Top + layoutConfig.Padding.Bottom;
            if (layoutConfig.Direction == LayoutDirection.LeftToRight)
            {
                ScrollContainerDatas[scrollContainerIndex].ContentSize = new Dimensions(contentSizeW, element.Dimensions.Height);
            }
            else
            {
                ScrollContainerDatas[scrollContainerIndex].ContentSize = new Dimensions(element.Dimensions.Width, contentSizeH);
            }
        }
    }

    private void GenerateRenderCommands()
    {
        // Sort tree roots by ZIndex so higher z-index elements are rendered last (on top)
        var roots = LayoutElementTreeRoots.AsSpan();
        for (int i = 0; i < roots.Length - 1; i++)
        {
            for (int j = i + 1; j < roots.Length; j++)
            {
                if (roots[j].ZIndex < roots[i].ZIndex)
                {
                    var temp = roots[i];
                    roots[i] = roots[j];
                    roots[j] = temp;
                }
            }
        }

        for (int rootIndex = 0; rootIndex < LayoutElementTreeRoots.Length; rootIndex++)
        {
            ref var root = ref LayoutElementTreeRoots[rootIndex];
            GenerateRenderCommandsRecursive(root.LayoutElementIndex, root.ZIndex);
        }
    }

    private void GenerateRenderCommandsRecursive(int elementIndex, short zIndex)
    {
        ref var element = ref LayoutElements[elementIndex];
        int hashIndex = GetHashMapItemIndex(element.Id);
        if (hashIndex < 0)
            return;

        var boundingBox = LayoutElementsHashMapInternal[hashIndex].BoundingBox;

        // Culling
        if (!CullingDisabled)
        {
            if (boundingBox.Right < 0 || boundingBox.Bottom < 0 ||
                boundingBox.X > LayoutDimensions.Width || boundingBox.Y > LayoutDimensions.Height)
            {
                return;
            }
        }

        // Find configs
        int sharedIndex = FindConfigIndex(ref element, ElementConfigType.Shared);
        int scrollIndex = FindConfigIndex(ref element, ElementConfigType.Scroll);

        SharedElementConfig sharedConfig = sharedIndex >= 0 ? SharedElementConfigs[sharedIndex] : default;

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

        // Scissor start
        if (scrollIndex >= 0)
        {
            ref var scrollConfig = ref ScrollElementConfigs[scrollIndex];
            RenderCommands.Add(new RenderCommand
            {
                BoundingBox = boundingBox,
                CommandType = RenderCommandType.ScissorStart,
                Id = element.Id,
                ZIndex = zIndex,
                Scroll = new ScrollRenderData
                {
                    Horizontal = scrollConfig.Horizontal,
                    Vertical = scrollConfig.Vertical
                }
            });
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
                GenerateRenderCommandsRecursive(childIndex, zIndex);
            }
        }

        // Scissor end
        if (scrollIndex >= 0)
        {
            RenderCommands.Add(new RenderCommand
            {
                BoundingBox = boundingBox,
                CommandType = RenderCommandType.ScissorEnd,
                Id = element.Id,
                ZIndex = zIndex
            });
        }

        // Border
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

        // Image
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
                    BackgroundColor = sharedConfig.BackgroundColor,
                    CornerRadius = sharedConfig.CornerRadius
                }
            });
        }

        // Custom
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
    }

    public void SetLayoutDimensions(Dimensions dimensions)
    {
        LayoutDimensions = dimensions;
    }

    public bool PointerOver(ElementId elementId)
    {
        int hashIndex = GetHashMapItemIndex(elementId.Id);
        if (hashIndex < 0)
            return false;

        return LayoutElementsHashMapInternal[hashIndex].BoundingBox.Contains(PointerInfo.Position);
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
        // Forward scroll delta to text input widgets
        TextInput?.SetScrollDelta(scrollDelta.Y);

        // Find scroll container under pointer
        for (int i = 0; i < ScrollContainerDatas.Length; i++)
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

                // Apply scroll delta
                // Raylib: positive Y = scroll up (wheel toward user), negative Y = scroll down (wheel away)
                // Traditional scrolling: wheel up reveals content above (scroll position decreases)
                //                        wheel down reveals content below (scroll position increases)
                if (scrollConfig.Vertical && scrollDelta.Y != 0)
                {
                    float maxScroll = Math.Max(0, scrollData.ContentSize.Height - scrollData.BoundingBox.Height);
                    scrollData.ScrollPosition.Y = Math.Clamp(
                        scrollData.ScrollPosition.Y - scrollDelta.Y * 30,
                        0, maxScroll
                    );
                }

                if (scrollConfig.Horizontal && scrollDelta.X != 0)
                {
                    float maxScroll = Math.Max(0, scrollData.ContentSize.Width - scrollData.BoundingBox.Width);
                    scrollData.ScrollPosition.X = Math.Clamp(
                        scrollData.ScrollPosition.X - scrollDelta.X * 30,
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
