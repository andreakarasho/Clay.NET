using System.Numerics;
using System.Runtime.CompilerServices;

namespace Clay;

/// <summary>
/// Main static API for the Clay UI layout library.
/// </summary>
public static partial class Clay
{
    private static ClayContext? _context;

    /// <summary>
    /// Gets the current context.
    /// </summary>
    public static ClayContext? Context => _context;

    /// <summary>
    /// Initializes Clay with the given configuration.
    /// </summary>
    /// <param name="layoutDimensions">The initial layout dimensions (e.g., screen size).</param>
    /// <param name="textMeasurer">Text measurement implementation.</param>
    /// <param name="maxElementCount">Maximum number of UI elements.</param>
    public static void Initialize(
        Dimensions layoutDimensions,
        ITextMeasurer? textMeasurer = null,
        int maxElementCount = 8192)
    {
        _context?.Dispose();
        var measurer = textMeasurer ?? new SimpleTextMeasurer();
        _context = new ClayContext(maxElementCount)
        {
            LayoutDimensions = layoutDimensions,
            TextMeasurer = measurer,
            TextInput = new Widgets.TextInput(measurer)
        };
    }

    /// <summary>
    /// Begins a new layout frame.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void BeginLayout()
    {
        _context?.BeginLayout();
    }

    /// <summary>
    /// Ends the layout frame and returns render commands.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ReadOnlySpan<RenderCommand> EndLayout()
    {
        if (_context == null)
            return ReadOnlySpan<RenderCommand>.Empty;
        return _context.EndLayout();
    }

    /// <summary>
    /// Opens a new element with the given declaration.
    /// Use with 'using' for automatic closing.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ElementScope Element(ElementDeclaration declaration)
    {
        _context?.OpenElement();
        _context?.ConfigureOpenElement(declaration);
        return new ElementScope(_context);
    }

    /// <summary>
    /// Opens a new element without configuration.
    /// Call ConfigureElement() to set properties.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ElementScope Element()
    {
        _context?.OpenElement();
        return new ElementScope(_context);
    }

    /// <summary>
    /// Configures the currently open element.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ConfigureElement(ElementDeclaration declaration)
    {
        _context?.ConfigureOpenElement(declaration);
    }

    /// <summary>
    /// Closes the currently open element.
    /// Use this for Begin/End style APIs instead of the using pattern.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void CloseElement()
    {
        _context?.CloseElement();
    }

    /// <summary>
    /// Adds a text element.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Text(string text, TextConfig config = default)
    {
        _context?.AddText(text.AsSpan(), config.FontSize > 0 ? config : TextConfig.Default);
    }

    /// <summary>
    /// Adds a text element.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Text(ReadOnlySpan<char> text, TextConfig config = default)
    {
        _context?.AddText(text, config.FontSize > 0 ? config : TextConfig.Default);
    }

    /// <summary>
    /// Sets the pointer (mouse/touch) state.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void SetPointerState(Vector2 position, bool pressed)
    {
        _context?.SetPointerState(position, pressed);
    }

    /// <summary>
    /// Sets the layout dimensions.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void SetLayoutDimensions(Dimensions dimensions)
    {
        _context?.SetLayoutDimensions(dimensions);
    }

    /// <summary>
    /// Returns true if the pointer is currently over the element with the given ID.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool PointerOver(ElementId elementId)
    {
        return _context?.PointerOver(elementId) ?? false;
    }

    /// <summary>
    /// Gets element data for the element with the given ID.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ElementData GetElementData(ElementId id)
    {
        return _context?.GetElementData(id) ?? default;
    }

    /// <summary>
    /// Creates an element ID from a string.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ElementId Id(string label)
        => ElementId.Hash(label);

    /// <summary>
    /// Creates an element ID from a string with an index.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ElementId Id(string label, uint index)
        => ElementId.Hash(label, index);

    /// <summary>
    /// Creates a local element ID relative to the current parent.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ElementId IdLocal(string label)
    {
        var parentId = _context?.LayoutElements.Length > 1
            ? _context.LayoutElements[_context.OpenLayoutElementStack[_context.OpenLayoutElementStack.Length - 1]].Id
            : 0u;
        return ElementId.HashLocal(label, parentId);
    }

    /// <summary>
    /// Enables or disables debug mode.
    /// </summary>
    public static void SetDebugModeEnabled(bool enabled)
    {
        if (_context != null)
            _context.DebugModeEnabled = enabled;
    }

    /// <summary>
    /// Enables or disables culling.
    /// </summary>
    public static void SetCullingEnabled(bool enabled)
    {
        if (_context != null)
            _context.CullingDisabled = !enabled;
    }

    /// <summary>
    /// Sets the error handler.
    /// </summary>
    public static void SetErrorHandler(ErrorHandler handler)
    {
        if (_context != null)
            _context.OnError = handler;
    }

    /// <summary>
    /// Updates scroll containers based on mouse wheel input.
    /// Call this before BeginLayout() each frame.
    /// </summary>
    /// <param name="enableDragScrolling">Enable click-and-drag scrolling.</param>
    /// <param name="scrollDelta">Mouse wheel delta (positive = scroll up).</param>
    /// <param name="deltaTime">Time since last frame in seconds.</param>
    public static void UpdateScrollContainers(bool enableDragScrolling, Vector2 scrollDelta, float deltaTime)
    {
        _context?.UpdateScrollContainers(enableDragScrolling, scrollDelta, deltaTime);
    }

    /// <summary>
    /// Gets scroll container data for the given element ID.
    /// </summary>
    public static ScrollContainerData GetScrollContainerData(ElementId elementId)
    {
        return _context?.GetScrollContainerData(elementId) ?? default;
    }

    /// <summary>
    /// Sets the scroll position for a scroll container.
    /// </summary>
    public static void SetScrollPosition(ElementId elementId, Vector2 position)
    {
        _context?.SetScrollPosition(elementId, position);
    }

    /// <summary>
    /// Resets the scroll position for a scroll container to (0, 0).
    /// </summary>
    public static void ResetScrollPosition(ElementId elementId)
    {
        _context?.ResetScrollPosition(elementId);
    }

    /// <summary>
    /// Gets the current pointer state.
    /// </summary>
    public static PointerData GetPointerState()
    {
        return _context?.PointerInfo ?? default;
    }

    /// <summary>
    /// Disposes the Clay context and releases all resources.
    /// </summary>
    public static void Shutdown()
    {
        _context?.Dispose();
        _context = null;
    }

    // ============ Debug Stats ============

    /// <summary>
    /// Gets the number of layout elements in the current frame.
    /// </summary>
    public static int GetElementCount() => _context?.LayoutElements.Length ?? 0;

    /// <summary>
    /// Gets the number of render commands generated.
    /// </summary>
    public static int GetRenderCommandCount() => _context?.RenderCommands.Length ?? 0;

    /// <summary>
    /// Gets the number of active scroll containers.
    /// </summary>
    public static int GetScrollContainerCount() => _context?.ScrollContainerDatas.Length ?? 0;

    /// <summary>
    /// Gets the number of tree roots (floating elements + main).
    /// </summary>
    public static int GetTreeRootCount() => _context?.LayoutElementTreeRoots.Length ?? 0;

    /// <summary>
    /// Gets the number of text elements.
    /// </summary>
    public static int GetTextElementCount() => _context?.TextElementData.Length ?? 0;

    /// <summary>
    /// Gets the maximum element capacity.
    /// </summary>
    public static int GetMaxElementCount() => _context?.MaxElementCount ?? 0;

    /// <summary>
    /// Gets the current layout dimensions.
    /// </summary>
    public static Dimensions GetLayoutDimensions() => _context?.LayoutDimensions ?? default;

    /// <summary>
    /// Gets the current generation (frame) number.
    /// </summary>
    public static uint GetGeneration() => _context?.Generation ?? 0;

    /// <summary>
    /// Gets the current warning flags.
    /// </summary>
    public static BooleanWarnings GetWarnings() => _context?.Warnings ?? default;

    /// <summary>
    /// Returns true if debug mode is enabled.
    /// </summary>
    public static bool IsDebugModeEnabled() => _context?.DebugModeEnabled ?? false;

    /// <summary>
    /// Returns true if culling is disabled.
    /// </summary>
    public static bool IsCullingDisabled() => _context?.CullingDisabled ?? false;
}

/// <summary>
/// Scope for element lifetime management using the 'using' pattern.
/// </summary>
public readonly ref struct ElementScope
{
    private readonly ClayContext? _context;

    internal ElementScope(ClayContext? context)
    {
        _context = context;
    }

    /// <summary>
    /// Configures the element with the given declaration.
    /// </summary>
    public ElementScope Configure(ElementDeclaration declaration)
    {
        _context?.ConfigureOpenElement(declaration);
        return this;
    }

    /// <summary>
    /// Closes the element.
    /// </summary>
    public void Dispose()
    {
        _context?.CloseElement();
    }
}
