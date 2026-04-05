using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text.Json;
using Clay.Widgets;
using static Clay.Widgets.HsvGradientType;

namespace Clay;

/// <summary>
/// Holds all runtime state for ClayUI. Internal use only.
/// </summary>
internal class ClayUIContext
{
    // Persistent widget states
    internal readonly Dictionary<uint, bool> ToggleStates = new();
    internal readonly Dictionary<uint, float> SliderStates = new();
    internal readonly Dictionary<uint, string> TextInputStates = new();
    internal readonly Dictionary<uint, bool> ExpandedStates = new();
    internal readonly Dictionary<uint, Color> ColorPickerStates = new();

    // Window states
    internal record struct WindowState(Vector2 Position, Vector2 Size, bool Collapsed, bool Open, bool Topmost);
    internal readonly Dictionary<uint, WindowState> WindowStates = new();

    // Popup states (position is where popup should appear, ParentId is the element that opened it)
    internal record struct PopupState(Vector2 Position, uint ParentId, bool Open);
    internal readonly Dictionary<uint, PopupState> PopupStates = new();
    internal readonly List<uint> OpenPopupStack = new(); // Stack of open popups for nesting
    internal readonly List<(uint PopupId, BoundingBox Bounds)> OpenPopupBounds = new(); // Bounds of open popups this frame
    internal uint PopupToOpen; // Popup to open this frame (set by OpenPopup)
    internal Vector2 PopupOpenPosition; // Position for the popup to open at
    internal uint PopupOpenParentId; // Parent element for the popup
    internal readonly HashSet<uint> ModalPopups = new(); // Popups that are modal (no click-outside-close)

    // Window focus order (last element = topmost/focused window)
    internal readonly List<uint> WindowFocusOrder = new();

    // Open window bounding boxes for input blocking (updated each frame)
    // Stored as (windowId, bounds) to support z-order aware hit testing
    internal readonly List<(uint WindowId, BoundingBox Bounds)> OpenWindowBounds = new();
    internal bool WindowBoundsRebuiltThisFrame;

    // Per-frame tracking
    internal readonly HashSet<uint> PressedThisFrame = new();
    internal readonly HashSet<uint> HoveredThisFrame = new();

    // Container depth tracking
    internal int TreeNodeDepth;
    internal int PanelDepth;
    internal int LayoutDepth;
    internal int WindowDepth;
    internal int PopupDepth;

    // Scroll container info for automatic scrollbar rendering
    internal record struct ScrollWrapperInfo(ElementId ScrollId, bool IsVertical, bool HasWrapper, bool HasBothAxes = false);
    internal record struct WindowFrameInfo(ElementId ScrollId, bool ShowScrollbar, bool ShowResize, bool HasRightColumn);
    internal readonly Stack<ScrollWrapperInfo?> PanelScrollInfo = new();
    internal readonly Stack<ScrollWrapperInfo?> LayoutScrollInfo = new();
    internal readonly Stack<WindowFrameInfo?> WindowScrollInfo = new();

    // Window info stack for nested windows
    internal readonly Stack<uint> WindowStack = new();

    // ID generation
    internal uint IdCounter;

    // Mouse state
    internal bool MouseWasPressed;
    internal bool MousePressed;
    internal Vector2 MousePosition;

    // Active slider tracking for drag behavior
    internal uint ActiveSliderTrackId;
    internal float ActiveSliderMin;
    internal float ActiveSliderMax = 1;

    // Active splitter tracking for drag behavior
    internal uint ActiveSplitterId;
    internal float SplitterDragStartMouse;  // Mouse position at drag start (on the relevant axis)
    internal float SplitterDragStartSize1;  // size1 value at drag start

    // Active scrollbar tracking for drag behavior
    internal uint ActiveScrollbarId;
    internal ElementId ActiveScrollContainerId;
    internal bool IsVerticalScrollbar;
    internal float ScrollbarDragOffset;
    internal ElementId ScrollbarHitAreaId;

    // Active window drag tracking
    internal uint ActiveDragWindowId;
    internal Vector2 WindowDragOffset;

    // Active window resize tracking
    internal uint ActiveResizeWindowId;
    internal ResizeDirection ActiveResizeDirection;
    internal Vector2 ResizeStartMousePos;
    internal Vector2 ResizeStartSize;
    internal Vector2 ResizeStartPos;

    // Scroll handling
    internal Vector2 ScrollDelta;
    internal bool ScrollConsumedByWindow;

    // Frame timing
    internal float DeltaTime;

    // Modifier key state (updated by KeyDown, reset each frame)
    internal bool ShiftHeld;
    internal bool CtrlHeld;
    internal bool AltHeld;
    internal bool SuperHeld;
    internal bool WasShiftHeld; // Previous frame's Shift state for scroll remapping

    // Click consumption - set when window chrome (title bar, close, collapse) handles a click
    internal bool ClickConsumedThisFrame;

    // Disabled region depth (> 0 means currently inside a BeginDisabled/EndDisabled block)
    internal int DisabledDepth;

    // Submenu hover tracking (for hover-to-open delay)
    internal uint HoveredMenuItemId;      // Which menu item is currently hovered
    internal float HoveredMenuItemTime;   // How long it has been hovered (seconds)
    internal const float SubmenuOpenDelay = 0.26f; // Delay before submenu opens on hover

    // Menu bar state: when any menu bar item's popup is open, hovering siblings opens them without click
    internal bool InsideMenuBar;                             // True between BeginMenuBar/EndMenuBar
    internal int MenuBarMenuDepth;                           // Tracks how many menu-bar-level menus are open (for restoring InsideMenuBar)
    internal ButtonStyle? MenuBarStyle;                      // Style for menu bar items (set by BeginMenuBar)
    internal readonly List<string> MenuBarPopupIds = new();  // Popup IDs registered by BeginMenu this frame
    internal readonly List<string> PrevMenuBarPopupIds = new(); // Previous frame's menu bar popup IDs
    internal string? ActiveMenuBarPopupId;                   // Which menu bar popup is currently open (null = none)

    // Tooltip tracking
    internal ElementId LastWidgetId;       // ID of the most recently rendered widget
    internal uint TooltipHoveredId;        // Which widget is being hovered for tooltip
    internal float TooltipHoverTime;       // How long it has been hovered
    internal const float TooltipDelay = 0.5f; // Delay before tooltip appears
    internal bool TooltipShownThisFrame;   // Whether a tooltip was rendered this frame

    // Dock space persistent state
    internal readonly Dictionary<uint, DockSpaceState> DockSpaceStates = new();
    internal readonly Dictionary<uint, string> DockedWindowTitles = new(); // windowId -> title
    internal readonly Dictionary<uint, ElementId> DockedWindowScrollIds = new(); // windowId -> scroll container ID

    // Dock space stack (for BeginDockSpace/EndDockSpace)
    internal readonly Stack<uint> DockSpaceStack = new();

    // Per-frame dock tracking
    internal readonly HashSet<uint> DockableWindowsThisFrame = new();
    internal readonly HashSet<uint> NoDockingWindows = new(); // windows with NoDocking flag
    internal readonly List<(uint NodeId, BoundingBox Bounds, uint SpaceId)> DockLeafBounds = new();

    // Active dock splitter drag (same pattern as ActiveSplitterId)
    internal uint ActiveDockSplitterId;
    internal float DockSplitterDragStartMouse;
    internal float DockSplitterDragStartRatio;
    internal DockNode? ActiveDockSplitterNode; // Reference to the node being split

    // Active dock tab drag / undocking
    internal uint ActiveDockTabDragWindowId;
    internal uint ActiveDockTabSourceSpaceId;
    internal Vector2 DockTabDragStartPos;
    internal bool DockTabDragUndocked;

    // Dock drop preview
    internal uint DockDropTargetNodeId;
    internal DockDropZone DockDropZone;
    internal uint DockDropTargetSpaceId;

    // Pending dock operation (set in BeginFrame when drag ends over a dock zone)
    internal uint PendingDockWindowId;
    internal uint PendingDockTargetNodeId;
    internal DockDropZone PendingDockDropZone;
    internal uint PendingDockSpaceId;

    // Pending undock operation (deferred to avoid modifying tree during traversal)
    internal uint PendingUndockWindowId;
    internal uint PendingUndockSpaceId;

    /// <summary>
    /// Clears per-frame state. Called at the start of each frame.
    /// </summary>
    internal void BeginFrame(bool mouseDown, Vector2 mousePosition)
    {
        PressedThisFrame.Clear();
        HoveredThisFrame.Clear();
        DisabledDepth = 0;
        LastWidgetId = default;
        TooltipShownThisFrame = false;
        // Modifier state from previous frame is used for scroll remapping in BeginFrame,
        // then reset. KeyDown calls during this frame will set them again.
        WasShiftHeld = ShiftHeld;
        ShiftHeld = false;
        CtrlHeld = false;
        AltHeld = false;
        SuperHeld = false;

        // If no windows rendered last frame, clear stale bounds.
        // Otherwise keep previous frame's bounds so IsPointOverAnyWindow works
        // before BeginWindow re-populates them this frame.
        if (!WindowBoundsRebuiltThisFrame)
            OpenWindowBounds.Clear();
        WindowBoundsRebuiltThisFrame = false;
        IdCounter = 0;
        MouseWasPressed = MousePressed;
        MousePressed = mouseDown;
        MousePosition = mousePosition;
        ScrollConsumedByWindow = false;
        ClickConsumedThisFrame = false;
        PopupDepth = 0;

        // Rebuild popup bounds from previous frame's layout data for all open popups.
        // This ensures click-inside detection works before BeginPopup re-renders.
        OpenPopupBounds.Clear();
        foreach (var popupUint in OpenPopupStack)
        {
            var eid = new ElementId { Id = popupUint };
            var data = Clay.GetElementData(eid);
            if (data.Found)
                OpenPopupBounds.Add((popupUint, data.BoundingBox));
        }

        // If any popup is open and a click just happened outside all popups,
        // close them all immediately and consume the click so widgets behind don't fire.
        // However, if any modal popup is open, don't auto-close — modals require explicit close.
        bool justPressed = mouseDown && !MouseWasPressed;
        bool hasModalOpen = false;
        foreach (var pid in OpenPopupStack)
        {
            if (ModalPopups.Contains(pid)) { hasModalOpen = true; break; }
        }
        if (justPressed && OpenPopupStack.Count > 0 && !IsPointOverAnyPopup(mousePosition))
        {
            ClickConsumedThisFrame = true;
            if (!hasModalOpen)
            {
                ClayUI.CloseAllPopups();
                OpenPopupBounds.Clear();
            }
        }

        // Capture pending dock operation BEFORE clearing dock state
        PendingDockWindowId = 0;
        if (!mouseDown && MouseWasPressed && ActiveDragWindowId != 0 &&
            DockDropTargetNodeId != 0 && DockDropZone != DockDropZone.None)
        {
            // Mouse was just released while dragging a window over a dock zone
            PendingDockWindowId = ActiveDragWindowId;
            PendingDockTargetNodeId = DockDropTargetNodeId;
            PendingDockDropZone = DockDropZone;
            PendingDockSpaceId = DockDropTargetSpaceId;
        }

        // Reset dock per-frame state (after capturing pending operation)
        DockDropTargetNodeId = 0;
        DockDropZone = DockDropZone.None;
        DockableWindowsThisFrame.Clear();
        DockLeafBounds.Clear();
        PendingUndockWindowId = 0;

        // Release active slider/scrollbar/window/resize when mouse is released
        if (!mouseDown)
        {
            ActiveSliderTrackId = 0;
            ActiveSplitterId = 0;
            ActiveScrollbarId = 0;
            ActiveDragWindowId = 0;
            ActiveResizeWindowId = 0;
            ActiveResizeDirection = ResizeDirection.None;
            ActiveDockSplitterId = 0;
            ActiveDockSplitterNode = null;
            ActiveDockTabDragWindowId = 0;
            DockTabDragUndocked = false;
        }

        // Reset popup opening state
        PopupToOpen = 0;
        PopupOpenPosition = default;
        PopupOpenParentId = 0;
    }

    /// <summary>
    /// Clears all persistent widget states.
    /// </summary>
    internal void ClearState()
    {
        ToggleStates.Clear();
        SliderStates.Clear();
        TextInputStates.Clear();
        ExpandedStates.Clear();
        ColorPickerStates.Clear();
        WindowStates.Clear();
        WindowFocusOrder.Clear();
        PopupStates.Clear();
        OpenPopupStack.Clear();
        ModalPopups.Clear();
        DockSpaceStates.Clear();
        DockedWindowTitles.Clear();
    }

    /// <summary>
    /// Checks if a point is inside any open window.
    /// Uses WindowStates to check ALL windows, not just those processed this frame.
    /// </summary>
    internal bool IsPointOverAnyWindow(Vector2 point, float titleBarHeight = 32)
    {
        // Use OpenWindowBounds (populated each frame when windows render) instead of
        // WindowStates (which persists even when windows aren't rendered this frame).
        foreach (var (_, bounds) in OpenWindowBounds)
        {
            if (point.X >= bounds.X && point.X <= bounds.X + bounds.Width &&
                point.Y >= bounds.Y && point.Y <= bounds.Y + bounds.Height)
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// Gets the topmost window at a given point, respecting z-order.
    /// Returns 0 if no window is at that point.
    /// </summary>
    internal uint GetTopmostWindowAtPoint(Vector2 point)
    {
        uint topmostWindow = 0;
        int highestOrder = -1;

        foreach (var (windowId, bounds) in OpenWindowBounds)
        {
            if (point.X >= bounds.X && point.X <= bounds.X + bounds.Width &&
                point.Y >= bounds.Y && point.Y <= bounds.Y + bounds.Height)
            {
                int order = WindowFocusOrder.IndexOf(windowId);
                if (order > highestOrder)
                {
                    highestOrder = order;
                    topmostWindow = windowId;
                }
            }
        }
        return topmostWindow;
    }

    /// <summary>
    /// Checks if a specific window is the topmost window at the mouse position.
    /// Uses WindowStates to check ALL windows, not just those processed this frame.
    /// </summary>
    internal bool IsWindowTopmostAtMouse(uint windowId, float titleBarHeight = 32)
    {
        if (!WindowStates.TryGetValue(windowId, out var myState))
            return false;

        // Check if mouse is within this window's bounds
        float myHeight = myState.Collapsed ? titleBarHeight : myState.Size.Y;
        if (MousePosition.X < myState.Position.X || MousePosition.X > myState.Position.X + myState.Size.X ||
            MousePosition.Y < myState.Position.Y || MousePosition.Y > myState.Position.Y + myHeight)
        {
            return false; // Mouse not over this window
        }

        int myOrder = WindowFocusOrder.IndexOf(windowId);
        if (myOrder < 0) myOrder = int.MaxValue; // New window

        // Check if any other window with higher focus order contains the mouse
        foreach (var (otherId, otherState) in WindowStates)
        {
            if (otherId == windowId) continue;
            if (!otherState.Open) continue; // Skip closed windows

            int otherOrder = WindowFocusOrder.IndexOf(otherId);
            if (otherOrder <= myOrder) continue; // Other window is below us

            float otherHeight = otherState.Collapsed ? titleBarHeight : otherState.Size.Y;
            if (MousePosition.X >= otherState.Position.X && MousePosition.X <= otherState.Position.X + otherState.Size.X &&
                MousePosition.Y >= otherState.Position.Y && MousePosition.Y <= otherState.Position.Y + otherHeight)
            {
                return false; // Another window is on top at this position
            }
        }

        return true;
    }

    /// <summary>
    /// Brings a window to the front of the focus order.
    /// </summary>
    internal void BringWindowToFront(uint windowId)
    {
        WindowFocusOrder.Remove(windowId);
        WindowFocusOrder.Add(windowId);
    }

    /// <summary>
    /// Gets the z-index for a window based on focus order.
    /// Higher index in focus order = topmost = higher z-index (rendered last).
    /// Topmost windows get a base z-index of 1000+ to always stay above normal windows.
    /// </summary>
    internal short GetWindowZIndex(uint windowId)
    {
        int index = WindowFocusOrder.IndexOf(windowId);
        if (index < 0)
        {
            // New window, add to focus order
            WindowFocusOrder.Add(windowId);
            index = WindowFocusOrder.Count - 1;
        }

        // Check if window is marked as topmost
        bool isTopmost = WindowStates.TryGetValue(windowId, out var state) && state.Topmost;

        // Topmost windows get base z-index of 1000, normal windows get 100
        int baseZ = isTopmost ? 1000 : 100;
        return (short)(baseZ + index * 10);
    }

    /// <summary>
    /// Gets the z-index for a popup. Popups always render above windows.
    /// </summary>
    internal short GetPopupZIndex(uint popupId)
    {
        int stackIndex = OpenPopupStack.IndexOf(popupId);
        if (stackIndex < 0) stackIndex = 0;
        // Popups get z-index 2000+ to always be above topmost windows
        return (short)(2000 + stackIndex * 10);
    }

    /// <summary>
    /// Checks if a point is inside any open popup.
    /// </summary>
    internal bool IsPointOverAnyPopup(Vector2 point)
    {
        foreach (var (_, bounds) in OpenPopupBounds)
        {
            if (point.X >= bounds.X && point.X <= bounds.X + bounds.Width &&
                point.Y >= bounds.Y && point.Y <= bounds.Y + bounds.Height)
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// Closes all popups that don't contain the given point (click-outside behavior).
    /// </summary>
    internal void ClosePopupsOutsidePoint(Vector2 point)
    {
        // Close popups from the end of the stack (topmost first)
        for (int i = OpenPopupStack.Count - 1; i >= 0; i--)
        {
            uint popupId = OpenPopupStack[i];
            // Find bounds for this popup
            bool isOverPopup = false;
            foreach (var (id, bounds) in OpenPopupBounds)
            {
                if (id == popupId)
                {
                    if (point.X >= bounds.X && point.X <= bounds.X + bounds.Width &&
                        point.Y >= bounds.Y && point.Y <= bounds.Y + bounds.Height)
                    {
                        isOverPopup = true;
                    }
                    break;
                }
            }

            if (!isOverPopup)
            {
                // Close this popup and all popups above it
                for (int j = OpenPopupStack.Count - 1; j >= i; j--)
                {
                    uint closeId = OpenPopupStack[j];
                    if (PopupStates.TryGetValue(closeId, out var state))
                    {
                        PopupStates[closeId] = state with { Open = false };
                    }
                    OpenPopupStack.RemoveAt(j);
                }
                break;
            }
        }
    }
}

/// <summary>
/// ImGui-style immediate mode UI API built on top of Clay.
/// Provides convenient widget methods that return interaction state.
/// </summary>
public static class ClayUI
{
    // ============ Context ============

    private static ClayUIContext _context = new();

    /// <summary>
    /// Gets the current UI context as an opaque handle. Use with <see cref="SetContext"/>
    /// to switch between independent UI contexts (e.g., multiple viewports or render targets).
    /// </summary>
    public static ClayUIContextHandle GetContext()
    {
        return new ClayUIContextHandle(_context, Style, Clay.Context);
    }

    /// <summary>
    /// Restores a previously saved UI context. Both the ClayUI widget state and the
    /// underlying Clay layout context are switched.
    /// </summary>
    public static void SetContext(ClayUIContextHandle handle)
    {
        _context = handle.UIContext;
        Style = handle.Style;
        Clay.SetContext(handle.LayoutContext);
    }

    // ============ Style Configuration ============

    /// <summary>
    /// Current UI style settings. Modify to customize appearance.
    /// </summary>
    public static ClayUIStyle Style { get; set; } = ClayUIStyle.Default;

    /// <summary>
    /// Current skin for applying custom images/textures to widgets.
    /// When null, widgets render with their default color-based styles.
    /// </summary>
    public static ClayUISkin? Skin { get; set; }

    // ============ Frame Management ============

    /// <summary>
    /// Call at the start of each frame before using ClayUI widgets.
    /// Updates scroll containers, handles active scrollbar/window dragging/resizing.
    /// </summary>
    /// <param name="mouseDown">Whether the mouse button is currently pressed.</param>
    /// <param name="mousePosition">Current mouse position.</param>
    /// <param name="scrollDelta">Mouse wheel scroll delta (optional, for window scrolling).</param>
    /// <param name="deltaTime">Frame delta time in seconds (for scroll momentum).</param>
    /// <param name="layoutDimensions">Layout dimensions (e.g., screen size). If default, dimensions are unchanged.</param>
    public static void BeginFrame(Dimensions layoutDimensions, bool mouseDown, Vector2 mousePosition = default, Vector2 scrollDelta = default, float deltaTime = 1f / 60f)
    {
        // Forward state to the layout engine
        Clay.SetLayoutDimensions(layoutDimensions);
        Clay.SetPointerState(mousePosition, mouseDown);

        _context.BeginFrame(mouseDown, mousePosition);
        _context.ScrollDelta = scrollDelta;
        _context.DeltaTime = deltaTime;

        // Shift+Wheel: remap vertical scroll to horizontal (uses previous frame's Shift state)
        if (_context.WasShiftHeld && scrollDelta.Y != 0)
            scrollDelta = new Vector2(scrollDelta.Y, 0);

        // Update scroll containers (blocked by windows and popups)
        if (!IsMouseOverAnyWindow && !IsMouseOverAnyPopup)
        {
            Clay.UpdateScrollContainers(false, scrollDelta, deltaTime);
        }

        // Handle active scrollbar dragging
        if (_context.ActiveScrollbarId != 0 && mouseDown)
        {
            UpdateActiveScrollbar();
        }

        // Handle active window dragging
        if (_context.ActiveDragWindowId != 0 && mouseDown)
        {
            UpdateActiveWindowDrag();
        }

        // Handle active window resizing
        if (_context.ActiveResizeWindowId != 0 && mouseDown)
        {
            UpdateActiveWindowResize();
        }

        // Begin layout pass (this calls TextInput.BeginFrame which resets per-frame state)
        Clay.BeginLayout();

        // Forward scroll delta to text inputs AFTER BeginLayout so it isn't cleared
        // by TextInput.BeginFrame() which runs inside BeginLayout.
        Clay.SetTextInputScrollDelta(scrollDelta.Y);
    }

    /// <summary>
    /// Ends the current frame. Computes layout and returns render commands.
    /// </summary>
    public static ReadOnlySpan<RenderCommand> EndFrame()
    {
        return Clay.EndLayout();
    }

    /// <summary>
    /// Forwards a key-down event to ClayUI. Routes to text editing when a text input
    /// has focus, and may handle other ClayUI shortcuts in the future.
    /// Call this for every key that is held down each frame.
    /// Uses the deltaTime passed to <see cref="BeginFrame"/>.
    /// </summary>
    /// <param name="key">The platform-agnostic key code.</param>
    /// <param name="modifiers">Active modifier keys (Shift, Ctrl).</param>
    public static void KeyDown(Widgets.ClayKey key, Widgets.ClayKeyModifiers modifiers = Widgets.ClayKeyModifiers.None)
    {
        // Track modifier key state
        switch (key)
        {
            case Widgets.ClayKey.Shift: _context.ShiftHeld = true; return;
            case Widgets.ClayKey.Ctrl: _context.CtrlHeld = true; return;
            case Widgets.ClayKey.Alt: _context.AltHeld = true; return;
            case Widgets.ClayKey.Super: _context.SuperHeld = true; return;
        }

        if (Clay.TextEditHasFocus)
            Clay.TextEditKeyDown(key, modifiers, _context.DeltaTime);
    }

    /// <summary>
    /// Forwards a character input event to ClayUI. Routes to the focused text input.
    /// Call this for each character produced by the OS text input system.
    /// </summary>
    /// <param name="ch">The character to insert.</param>
    public static void CharInput(char ch)
    {
        if (ch >= 32 && ch != '\r' && Clay.TextEditHasFocus)
            Clay.TextEditProcessChar(ch);
    }

    /// <summary>
    /// Returns true if scroll input was consumed by a window this frame.
    /// Call this to determine if underlying scroll containers should be updated.
    /// </summary>
    public static bool ScrollConsumedByWindow => _context.ScrollConsumedByWindow;

    /// <summary>
    /// Updates window size and position based on active resize.
    /// </summary>
    private static void UpdateActiveWindowResize()
    {
        if (_context.ActiveResizeWindowId == 0)
            return;

        if (!_context.WindowStates.TryGetValue(_context.ActiveResizeWindowId, out var state))
            return;

        var s = Style.Window;
        var delta = new Vector2(
            _context.MousePosition.X - _context.ResizeStartMousePos.X,
            _context.MousePosition.Y - _context.ResizeStartMousePos.Y
        );

        var newSize = _context.ResizeStartSize;
        var newPos = _context.ResizeStartPos;
        var dir = _context.ActiveResizeDirection;

        // Handle horizontal resize
        if (dir.HasFlag(ResizeDirection.Right))
        {
            newSize.X = Math.Clamp(_context.ResizeStartSize.X + delta.X, s.MinWidth, s.MaxWidth);
        }
        else if (dir.HasFlag(ResizeDirection.Left))
        {
            float newWidth = Math.Clamp(_context.ResizeStartSize.X - delta.X, s.MinWidth, s.MaxWidth);
            float widthDelta = _context.ResizeStartSize.X - newWidth;
            newPos.X = _context.ResizeStartPos.X + widthDelta;
            newSize.X = newWidth;
        }

        // Handle vertical resize
        if (dir.HasFlag(ResizeDirection.Bottom))
        {
            newSize.Y = Math.Clamp(_context.ResizeStartSize.Y + delta.Y, s.MinHeight, s.MaxHeight);
        }
        else if (dir.HasFlag(ResizeDirection.Top))
        {
            float newHeight = Math.Clamp(_context.ResizeStartSize.Y - delta.Y, s.MinHeight, s.MaxHeight);
            float heightDelta = _context.ResizeStartSize.Y - newHeight;
            newPos.Y = _context.ResizeStartPos.Y + heightDelta;
            newSize.Y = newHeight;
        }

        _context.WindowStates[_context.ActiveResizeWindowId] = state with { Position = newPos, Size = newSize };
    }

    /// <summary>
    /// Updates window position based on active drag.
    /// </summary>
    private static void UpdateActiveWindowDrag()
    {
        if (_context.ActiveDragWindowId == 0)
            return;

        if (!_context.WindowStates.TryGetValue(_context.ActiveDragWindowId, out var state))
            return;

        var newPosition = new Vector2(
            _context.MousePosition.X - _context.WindowDragOffset.X,
            _context.MousePosition.Y - _context.WindowDragOffset.Y
        );

        _context.WindowStates[_context.ActiveDragWindowId] = state with { Position = newPosition };
    }

    /// <summary>
    /// Updates scroll position based on active scrollbar drag.
    /// </summary>
    private static void UpdateActiveScrollbar()
    {
        var scrollData = Clay.GetScrollContainerData(_context.ActiveScrollContainerId);
        if (!scrollData.Found) return;

        var pointerData = Clay.GetPointerState();

        // Get the scrollbar track element to calculate relative position
        var trackId = _context.IsVerticalScrollbar
            ? ElementId.HashComposite("SbTrackV_", _context.ActiveScrollContainerId.Id)
            : ElementId.HashComposite("SbTrackH_", _context.ActiveScrollContainerId.Id);
        var trackData = Clay.GetElementData(trackId);
        if (!trackData.Found) return;

        var s = Style.Scrollbar;

        if (_context.IsVerticalScrollbar)
        {
            float containerHeight = scrollData.ScrollContainerDimensions.Height;
            float contentHeight = scrollData.ContentDimensions.Height;
            float maxScrollY = scrollData.MaxScrollY;

            // Use track's actual inner height for thumb size (matches rendering)
            float trackInnerHeight = trackData.BoundingBox.Height - s.TrackPadding * 2;
            float thumbHeight = Math.Max(s.MinThumbSize, (containerHeight / contentHeight) * trackInnerHeight);
            float trackTravel = trackInnerHeight - thumbHeight;

            if (trackTravel <= 0) return;

            // Calculate where the thumb top should be based on mouse position minus offset
            // Subtract track position and padding to get thumb position in track space
            float thumbTopY = pointerData.Position.Y - _context.ScrollbarDragOffset - trackData.BoundingBox.Y - s.TrackPadding;
            float normalizedY = Math.Clamp(thumbTopY / trackTravel, 0f, 1f);
            float newScrollY = normalizedY * maxScrollY;

            Clay.SetScrollPosition(_context.ActiveScrollContainerId, new Vector2(scrollData.ScrollPosition.X, newScrollY));
        }
        else
        {
            float containerWidth = scrollData.ScrollContainerDimensions.Width;
            float contentWidth = scrollData.ContentDimensions.Width;
            float maxScrollX = scrollData.MaxScrollX;

            // Use track's actual inner width for thumb size (matches rendering)
            float trackInnerWidth = trackData.BoundingBox.Width - s.TrackPadding * 2;
            float thumbWidth = Math.Max(s.MinThumbSize, (containerWidth / contentWidth) * trackInnerWidth);
            float trackTravel = trackInnerWidth - thumbWidth;

            if (trackTravel <= 0) return;

            // Calculate where the thumb left should be based on mouse position minus offset
            // Subtract track position and padding to get thumb position in track space
            float thumbLeftX = pointerData.Position.X - _context.ScrollbarDragOffset - trackData.BoundingBox.X - s.TrackPadding;
            float normalizedX = Math.Clamp(thumbLeftX / trackTravel, 0f, 1f);
            float newScrollX = normalizedX * maxScrollX;

            Clay.SetScrollPosition(_context.ActiveScrollContainerId, new Vector2(newScrollX, scrollData.ScrollPosition.Y));
        }
    }

    /// <summary>
    /// Returns true if a click just happened (mouse was up, now down).
    /// </summary>
    public static bool IsMouseJustPressed => _context.MousePressed && !_context.MouseWasPressed;

    /// <summary>
    /// Returns true if the mouse was just released (mouse was down, now up).
    /// </summary>
    public static bool IsMouseJustReleased => !_context.MousePressed && _context.MouseWasPressed;

    /// <summary>
    /// Returns true if the mouse is currently over any open window.
    /// Use this to block input to elements behind windows.
    /// </summary>
    public static bool IsMouseOverAnyWindow => _context.IsPointOverAnyWindow(_context.MousePosition, Style.Window.TitleBarHeight);

    /// <summary>
    /// Returns true if the mouse is currently over any open popup.
    /// Use this to block input to elements behind popups.
    /// </summary>
    public static bool IsMouseOverAnyPopup => _context.IsPointOverAnyPopup(_context.MousePosition);

    /// <summary>
    /// Returns true if we are currently rendering inside a popup (between BeginPopup/EndPopup).
    /// </summary>
    public static bool IsInsidePopup => _context.PopupDepth > 0;

    /// <summary>
    /// Gets the number of windows currently tracked (for debugging).
    /// </summary>
    public static int WindowCount => _context.WindowStates.Count;

    /// <summary>
    /// Returns true if a window is currently being dragged (for debugging).
    /// </summary>
    public static bool IsWindowBeingDragged => _context.ActiveDragWindowId != 0;

    /// <summary>
    /// Debug: prints window state info to console.
    /// </summary>
    public static void DebugPrintWindowInfo()
    {
        var mouse = _context.MousePosition;
        Console.WriteLine($"Mouse: ({mouse.X:F0}, {mouse.Y:F0}), IsOverWindow: {IsMouseOverAnyWindow}");
        foreach (var (id, state) in _context.WindowStates)
        {
            float h = state.Collapsed ? 32 : state.Size.Y;
            bool contains = mouse.X >= state.Position.X && mouse.X <= state.Position.X + state.Size.X &&
                           mouse.Y >= state.Position.Y && mouse.Y <= state.Position.Y + h;
            Console.WriteLine($"  Window {id}: Pos({state.Position.X:F0},{state.Position.Y:F0}) Size({state.Size.X:F0},{state.Size.Y:F0}) Open:{state.Open} Contains:{contains}");
        }
    }

    /// <summary>
    /// Returns true if a click just happened and no window is blocking it.
    /// Use this instead of IsMouseJustPressed for elements that should be blocked by windows.
    /// </summary>
    public static bool IsMouseJustPressedUnblocked => IsMouseJustPressed && !IsMouseOverAnyWindow && !IsMouseOverAnyPopup;

    /// <summary>
    /// Returns true if we're currently inside a window context (between BeginWindow and EndWindow).
    /// </summary>
    public static bool IsInsideWindow => _context.WindowStack.Count > 0 && _context.WindowStack.Peek() != 0;

    /// <summary>
    /// Returns true if the current window (if any) is the topmost window at the mouse position.
    /// </summary>
    private static bool IsCurrentWindowTopmost
    {
        get
        {
            if (!IsInsideWindow) return false;
            uint currentWindowId = _context.WindowStack.Peek();

            // Docked windows don't overlap — always process clicks
            foreach (var (_, dockSpace) in _context.DockSpaceStates)
            {
                if (dockSpace.WindowToNode.ContainsKey(currentWindowId))
                    return true;
            }

            return _context.IsWindowTopmostAtMouse(currentWindowId, Style.Window.TitleBarHeight);
        }
    }

    /// <summary>
    /// Returns true if a click should be processed for this widget.
    /// Triggers on mouse release (matching UO's button behavior).
    /// Widgets inside topmost windows process clicks; widgets outside are blocked if mouse is over any window.
    /// Widgets behind open popups are blocked unless the widget is inside the popup.
    /// </summary>
    private static bool ShouldProcessClick
    {
        get
        {
            if (!IsMouseJustReleased) return false;

            // Block all interaction in disabled regions
            if (IsDisabled) return false;

            // If a window's chrome (title bar, close, collapse) already handled this click, block it
            if (_context.ClickConsumedThisFrame) return false;

            // Block clicks on widgets behind popups (but allow clicks inside the popup itself)
            if (!IsInsidePopup && IsMouseOverAnyPopup) return false;

            // If inside a window, only process if that window is topmost
            if (IsInsideWindow)
            {
                return IsCurrentWindowTopmost;
            }

            // Outside windows, block if mouse is over any window
            return !IsMouseOverAnyWindow;
        }
    }

    // ============ ID Generation ============

    /// <summary>
    /// Generates a unique ID for a widget. The label is hashed together with
    /// a per-frame counter to guarantee uniqueness even for identical labels.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ElementId Id(string label)
    {
        var id = ElementId.Hash(label, _context.IdCounter++, IdSeed);
        _context.LastWidgetId = id;
        return id;
    }

    /// <summary>
    /// Generates a stable ID for a widget (doesn't auto-increment).
    /// Use for widgets that need the same ID across frames (windows, panels, etc.).
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ElementId StableId(string label)
        => ElementId.Hash(label, seed: IdSeed);

    // Seed to distinguish ClayUI IDs from user IDs
    private const uint IdSeed = 0x436C6179; // "Clay"

    // ============ Basic Widgets ============

    /// <summary>
    /// Renders a clickable button. Returns true when clicked.
    /// </summary>
    public static bool Button(string label, ButtonStyle? style = null, ButtonSkin? skin = null)
    {
        var s = style.HasValue ? style.Value.MergeOver(Style.Button) : Style.Button;
        var sk = skin ?? Skin?.Button ?? default;
        var id = Id(label);
        bool isHovered = IsHovered(id);
        bool isPressed = isHovered && _context.MousePressed;  // Mouse held down on button
        bool clicked = isHovered && ShouldProcessClick;  // Mouse just clicked (blocked by windows if outside)

        if (isHovered) _context.HoveredThisFrame.Add(id.Id);
        if (clicked) _context.PressedThisFrame.Add(id.Id);

        var bgColor = isPressed ? s.PressedColor
            : isHovered ? s.HoverColor
            : s.BackgroundColor;

        var skinImg = sk.Background.HasImages ? sk.Background.ForState(isPressed, isHovered) : default;

        var layout = new LayoutConfig
        {
            Padding = s.Padding,
            ChildAlignment = ChildAlignment.Center
        };
        if (s.HasSizing)
        {
            layout.Sizing = s.Sizing;
            layout.ClipContent = true;
        }

        using (Clay.Element(new ElementDeclaration
        {
            Id = id,
            Layout = layout,
            BackgroundColor = skinImg.HasImage ? Color.Transparent : DisabledColor(bgColor),
            CornerRadius = skinImg.HasImage ? CornerRadius.Zero : s.CornerRadius,
            Image = skinImg.HasImage ? SkinToImageConfig(skinImg) : default
        }))
        {
            var textFontSize = (isHovered && s.HasHoverFontSize) ? s.HoverFontSize : s.FontSize;
            var textColor = (isHovered && s.HasHoverTextColor) ? s.HoverTextColor : s.TextColor;

            Clay.Text(ElementId.GetDisplayLabel(label), new TextConfig
            {
                FontId = s.FontId,
                FontSize = textFontSize,
                TextColor = DisabledColor(textColor)
            });
        }

        return clicked;
    }

    /// <summary>
    /// Renders a text label.
    /// </summary>
    public static void Label(string text, LabelStyle? style = null)
    {
        var s = style.HasValue ? style.Value.MergeOver(Style.Label) : Style.Label;
        Clay.Text(text, new TextConfig
        {
            FontId = s.FontId,
            FontSize = s.FontSize,
            TextColor = DisabledColor(s.TextColor),
            LineHeight = s.LineHeight
        });
    }

    /// <summary>
    /// Renders a heading text.
    /// </summary>
    public static void Heading(string text, HeadingStyle? style = null)
    {
        var s = style.HasValue ? style.Value.MergeOver(Style.Heading) : Style.Heading;
        Clay.Text(text, new TextConfig
        {
            FontId = s.FontId,
            FontSize = s.FontSize,
            TextColor = s.TextColor
        });
    }

    /// <summary>
    /// Renders an image. Similar to ImGui::Image().
    /// </summary>
    /// <param name="imageData">Image data object passed through to the renderer.</param>
    /// <param name="width">Display width in pixels.</param>
    /// <param name="height">Display height in pixels.</param>
    /// <param name="style">Optional image style.</param>
    public static void Image(object imageData, float width, float height, ImageStyle? style = null)
    {
        var s = style.HasValue ? style.Value.MergeOver(Style.Image) : Style.Image;
        var id = Id("Image");

        using (Clay.Element(new ElementDeclaration
        {
            Id = id,
            Layout = new LayoutConfig
            {
                Sizing = Sizing.FixedSize(width, height)
            },
            Image = ImageConfig.Create(imageData, width, height),
            CornerRadius = s.CornerRadius,
            Border = s.Border
        })) { }
    }

    /// <summary>
    /// Renders a clickable image button. Similar to ImGui::ImageButton().
    /// Returns true when clicked.
    /// </summary>
    /// <param name="imageData">Image data object passed through to the renderer.</param>
    /// <param name="width">Display width in pixels.</param>
    /// <param name="height">Display height in pixels.</param>
    /// <param name="style">Optional image style.</param>
    public static bool ImageButton(object imageData, float width, float height, ImageStyle? style = null)
    {
        var s = style.HasValue ? style.Value.MergeOver(Style.Image) : Style.Image;
        var id = Id("ImageButton");
        bool isHovered = IsHovered(id);
        bool isPressed = isHovered && _context.MousePressed;
        bool clicked = isHovered && ShouldProcessClick;

        if (isHovered) _context.HoveredThisFrame.Add(id.Id);
        if (clicked) _context.PressedThisFrame.Add(id.Id);

        var bgColor = isPressed ? s.PressedTint
            : isHovered ? s.HoverTint
            : Color.Transparent;

        using (Clay.Element(new ElementDeclaration
        {
            Id = id,
            Layout = new LayoutConfig
            {
                Sizing = Sizing.FixedSize(width, height),
                Padding = s.Padding
            },
            Image = ImageConfig.Create(imageData, width, height),
            BackgroundColor = bgColor,
            CornerRadius = s.CornerRadius,
            Border = isHovered ? s.HoverBorder : s.Border
        })) { }

        return clicked;
    }

    /// <summary>
    /// Renders a checkbox. Returns true when the state changes.
    /// </summary>
    public static bool Checkbox(string label, ref bool value, CheckboxStyle? style = null, CheckboxSkin? skin = null)
    {
        var s = style.HasValue ? style.Value.MergeOver(Style.Checkbox) : Style.Checkbox;
        var sk = skin ?? Skin?.Checkbox ?? default;
        var id = Id(label);
        bool isHovered = IsHovered(id);
        bool isPressed = isHovered && _context.MousePressed;
        bool clicked = isHovered && ShouldProcessClick;
        bool changed = false;

        if (clicked)
        {
            value = !value;
            changed = true;
            _context.PressedThisFrame.Add(id.Id);
        }

        var bgColor = isPressed ? s.PressedColor
            : isHovered ? s.HoverColor
            : Color.Transparent;

        using (Clay.Element(new ElementDeclaration
        {
            Id = id,
            Layout = new LayoutConfig
            {
                Direction = LayoutDirection.LeftToRight,
                ChildGap = 8,
                ChildAlignment = ChildAlignment.CenterLeft,
                Padding = s.Padding
            },
            BackgroundColor = bgColor
        }))
        {
            // Checkbox box
            var boxSkin = sk.Box.HasImages ? sk.Box.ForState(isPressed, isHovered) : default;
            var checkedBoxSkin = value && sk.CheckedBox.HasImage ? sk.CheckedBox
                : value && boxSkin.HasImage ? boxSkin
                : default;
            var activeBoxSkin = value ? checkedBoxSkin : boxSkin;

            using (Clay.Element(new ElementDeclaration
            {
                Layout = new LayoutConfig
                {
                    Sizing = Sizing.FixedSize(s.BoxSize, s.BoxSize),
                    ChildAlignment = ChildAlignment.Center
                },
                BackgroundColor = activeBoxSkin.HasImage ? Color.Transparent : DisabledColor(value ? s.CheckedColor : s.BoxColor),
                CornerRadius = activeBoxSkin.HasImage ? CornerRadius.Zero : CornerRadius.All(s.BoxCornerRadius),
                Border = activeBoxSkin.HasImage ? default : BorderConfig.Uniform(1, DisabledColor(s.BoxBorderColor)),
                Image = activeBoxSkin.HasImage ? SkinToImageConfig(activeBoxSkin) : default
            }))
            {
                if (value)
                {
                    if (sk.Checkmark.HasImage)
                    {
                        using (Clay.Element(new ElementDeclaration
                        {
                            Layout = new LayoutConfig
                            {
                                Sizing = Sizing.FixedSize(s.BoxSize * 0.5f, s.BoxSize * 0.5f)
                            },
                            Image = SkinToImageConfig(sk.Checkmark)
                        })) { }
                    }
                    else
                    {
                        // Checkmark (simple square for now)
                        using (Clay.Element(new ElementDeclaration
                        {
                            Layout = new LayoutConfig
                            {
                                Sizing = Sizing.FixedSize(s.BoxSize * 0.5f, s.BoxSize * 0.5f)
                            },
                            BackgroundColor = DisabledColor(s.CheckmarkColor),
                            CornerRadius = CornerRadius.All(2)
                        })) { }
                    }
                }
            }

            // Label
            Clay.Text(ElementId.GetDisplayLabel(label), new TextConfig
            {
                FontId = s.FontId,
                FontSize = s.FontSize,
                TextColor = DisabledColor(s.TextColor)
            });
        }

        return changed;
    }

    /// <summary>
    /// Renders a horizontal slider. Returns true when the value changes.
    /// </summary>
    public static bool Slider(string label, ref float value, float min = 0f, float max = 1f, SliderStyle? style = null, SliderSkin? skin = null)
    {
        var s = style.HasValue ? style.Value.MergeOver(Style.Slider) : Style.Slider;
        var sk = skin ?? Skin?.Slider ?? default;
        var id = Id(label);
        var trackId = ElementId.HashComposite("SlTrack_", id.Id);
        bool isHovered = IsHovered(trackId);
        bool changed = false;

        // Get track bounds for dragging
        var trackData = Clay.GetElementData(trackId);

        // Check if this slider should become active (mouse just pressed on it)
        if (isHovered && IsMouseJustPressed && trackData.Found)
        {
            _context.ActiveSliderTrackId = trackId.Id;
            _context.ActiveSliderMin = min;
            _context.ActiveSliderMax = max;
        }

        // Update value if this is the active slider and mouse is down
        bool isActiveSlider = _context.ActiveSliderTrackId == trackId.Id;
        if (isActiveSlider && _context.MousePressed && trackData.Found)
        {
            var pointerData = Clay.GetPointerState();
            float relativeX = pointerData.Position.X - trackData.BoundingBox.X;
            float normalizedValue = Math.Clamp(relativeX / trackData.BoundingBox.Width, 0f, 1f);
            float newValue = _context.ActiveSliderMin + normalizedValue * (_context.ActiveSliderMax - _context.ActiveSliderMin);

            if (Math.Abs(newValue - value) > 0.001f)
            {
                value = newValue;
                changed = true;
            }
        }

        float fillPercent = (value - min) / (max - min);

        using (Clay.Element(new ElementDeclaration
        {
            Id = id,
            Layout = new LayoutConfig
            {
                Direction = LayoutDirection.LeftToRight,
                ChildGap = 12,
                ChildAlignment = ChildAlignment.CenterLeft,
                Sizing = new Sizing { Width = SizingAxis.Grow() }
            }
        }))
        {
            // Label
            if (!string.IsNullOrEmpty(label))
            {
                Clay.Text(ElementId.GetDisplayLabel(label), new TextConfig
                {
                    FontId = s.FontId,
                    FontSize = s.FontSize,
                    TextColor = DisabledColor(s.TextColor)
                });
            }

            // Track
            using (Clay.Element(new ElementDeclaration
            {
                Id = trackId,
                Layout = new LayoutConfig
                {
                    Sizing = new Sizing
                    {
                        Width = SizingAxis.Grow(),
                        Height = SizingAxis.Fixed(s.TrackHeight)
                    },
                    ChildAlignment = ChildAlignment.CenterLeft
                },
                BackgroundColor = sk.Track.HasImage ? Color.Transparent : DisabledColor(s.TrackColor),
                CornerRadius = sk.Track.HasImage ? CornerRadius.Zero : CornerRadius.All(s.TrackHeight / 2),
                Image = sk.Track.HasImage ? SkinToImageConfig(sk.Track) : default
            }))
            {
                // Fill
                using (Clay.Element(new ElementDeclaration
                {
                    Layout = new LayoutConfig
                    {
                        Sizing = new Sizing
                        {
                            Width = SizingAxis.PercentOf(fillPercent),
                            Height = SizingAxis.Fixed(s.TrackHeight)
                        }
                    },
                    BackgroundColor = sk.Fill.HasImage ? Color.Transparent : DisabledColor(s.FillColor),
                    CornerRadius = sk.Fill.HasImage ? CornerRadius.Zero : CornerRadius.All(s.TrackHeight / 2),
                    Image = sk.Fill.HasImage ? SkinToImageConfig(sk.Fill) : default
                })) { }
            }

            // Value display
            Clay.Text(value.ToString("F2"), new TextConfig
            {
                FontId = s.FontId,
                FontSize = s.FontSize,
                TextColor = DisabledColor(s.ValueTextColor)
            });
        }

        return changed;
    }

    /// <summary>
    /// Renders a toggle switch. Returns true when the state changes.
    /// </summary>
    public static bool Toggle(string label, ref bool value, ToggleStyle? style = null, ToggleSkin? skin = null)
    {
        var s = style.HasValue ? style.Value.MergeOver(Style.Toggle) : Style.Toggle;
        var sk = skin ?? Skin?.Toggle ?? default;
        var id = Id(label);
        bool isHovered = IsHovered(id);
        bool isPressed = isHovered && _context.MousePressed;
        bool clicked = isHovered && ShouldProcessClick;
        bool changed = false;

        if (clicked)
        {
            value = !value;
            changed = true;
        }

        var bgColor = isPressed ? s.PressedColor
            : isHovered ? s.HoverColor
            : Color.Transparent;

        using (Clay.Element(new ElementDeclaration
        {
            Id = id,
            Layout = new LayoutConfig
            {
                Direction = LayoutDirection.LeftToRight,
                ChildGap = 10,
                ChildAlignment = ChildAlignment.CenterLeft,
                Padding = s.Padding
            },
            BackgroundColor = bgColor
        }))
        {
            // Toggle track
            var trackSkinImg = value && sk.TrackOn.HasImage ? sk.TrackOn
                : !value && sk.TrackOff.HasImage ? sk.TrackOff
                : default;
            using (Clay.Element(new ElementDeclaration
            {
                Layout = new LayoutConfig
                {
                    Sizing = Sizing.FixedSize(s.TrackWidth, s.TrackHeight),
                    Padding = new Padding { Left = 2, Right = 2 },
                    ChildAlignment = value ? ChildAlignment.CenterRight : ChildAlignment.CenterLeft
                },
                BackgroundColor = trackSkinImg.HasImage ? Color.Transparent : DisabledColor(value ? s.OnColor : s.OffColor),
                CornerRadius = trackSkinImg.HasImage ? CornerRadius.Zero : CornerRadius.All(s.TrackHeight / 2),
                Image = trackSkinImg.HasImage ? SkinToImageConfig(trackSkinImg) : default
            }))
            {
                // Toggle knob
                using (Clay.Element(new ElementDeclaration
                {
                    Layout = new LayoutConfig
                    {
                        Sizing = Sizing.FixedSize(s.KnobSize, s.KnobSize)
                    },
                    BackgroundColor = sk.Knob.HasImage ? Color.Transparent : DisabledColor(s.KnobColor),
                    CornerRadius = sk.Knob.HasImage ? CornerRadius.Zero : CornerRadius.All(s.KnobSize / 2),
                    Image = sk.Knob.HasImage ? SkinToImageConfig(sk.Knob) : default
                })) { }
            }

            // Label
            if (!string.IsNullOrEmpty(label))
            {
                Clay.Text(ElementId.GetDisplayLabel(label), new TextConfig
                {
                    FontId = s.FontId,
                    FontSize = s.FontSize,
                    TextColor = DisabledColor(s.TextColor)
                });
            }
        }

        return changed;
    }

    /// <summary>
    /// Renders a progress bar.
    /// </summary>
    public static void ProgressBar(float value, float min = 0f, float max = 1f, ProgressBarStyle? style = null, ProgressBarSkin? skin = null)
    {
        var s = style.HasValue ? style.Value.MergeOver(Style.ProgressBar) : Style.ProgressBar;
        var sk = skin ?? Skin?.ProgressBar ?? default;
        float fillPercent = Math.Clamp((value - min) / (max - min), 0f, 1f);

        using (Clay.Element(new ElementDeclaration
        {
            Layout = new LayoutConfig
            {
                Sizing = new Sizing
                {
                    Width = SizingAxis.Grow(),
                    Height = SizingAxis.Fixed(s.Height)
                }
            },
            BackgroundColor = sk.Track.HasImage ? Color.Transparent : s.BackgroundColor,
            CornerRadius = sk.Track.HasImage ? CornerRadius.Zero : CornerRadius.All(s.CornerRadius),
            Image = sk.Track.HasImage ? SkinToImageConfig(sk.Track) : default
        }))
        {
            using (Clay.Element(new ElementDeclaration
            {
                Layout = new LayoutConfig
                {
                    Sizing = new Sizing
                    {
                        Width = SizingAxis.PercentOf(fillPercent),
                        Height = SizingAxis.Fixed(s.Height)
                    }
                },
                BackgroundColor = sk.Fill.HasImage ? Color.Transparent : s.FillColor,
                CornerRadius = sk.Fill.HasImage ? CornerRadius.Zero : CornerRadius.All(s.CornerRadius),
                Image = sk.Fill.HasImage ? SkinToImageConfig(sk.Fill) : default
            })) { }
        }
    }

    /// <summary>
    /// Renders a text input field. Returns true when the text was modified by user input.
    /// Use <see cref="Clay.TextEditKeyDown"/> and <see cref="Clay.TextEditProcessChar"/>
    /// to forward keyboard input.
    /// </summary>
    /// <param name="label">Label used to generate a stable element ID.</param>
    /// <param name="text">Reference to the text string. Updated when the user edits it.</param>
    /// <param name="style">Visual style. Use <see cref="TextInputStyle.Default"/> or customize.</param>
    /// <param name="singleLine">Block newlines and make up/down act as left/right.</param>
    public static bool TextInput(string label, ref string text, TextInputStyle? style = null, bool singleLine = true, bool password = false)
    {
        var s = style ?? DefaultTextInputStyle;
        if (password) s.Password = true;
        var id = StableId(label);

        if (IsDisabled)
        {
            // Render with dimmed colors but don't allow interaction
            s = s with
            {
                BackgroundColor = DisabledColor(s.BackgroundColor),
                FocusedBackgroundColor = DisabledColor(s.BackgroundColor),
                TextColor = DisabledColor(s.TextColor),
                CursorColor = Color.Transparent,
                SelectionColor = Color.Transparent,
                Border = new BorderConfig { Width = s.Border.Width, Color = DisabledColor(s.Border.Color) },
                DisableInteraction = true,
            };
        }

        return Clay.TextEdit(id, ref text, s, singleLine);
    }

    private static readonly TextInputStyle DefaultTextInputStyle = new()
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
    };

    // ============ Layout Helpers ============

    /// <summary>
    /// Adds vertical spacing.
    /// </summary>
    public static void Space(float height = 8)
    {
        using (Clay.Element(new ElementDeclaration
        {
            Layout = new LayoutConfig
            {
                Sizing = new Sizing
                {
                    Width = SizingAxis.Grow(),
                    Height = SizingAxis.Fixed(height)
                }
            }
        })) { }
    }

    /// <summary>
    /// Renders a horizontal separator line.
    /// </summary>
    public static void Separator(Color? color = null, float height = 1)
    {
        var c = color ?? Style.SeparatorColor;
        using (Clay.Element(new ElementDeclaration
        {
            Layout = new LayoutConfig
            {
                Sizing = new Sizing
                {
                    Width = SizingAxis.Grow(),
                    Height = SizingAxis.Fixed(height)
                }
            },
            BackgroundColor = c
        })) { }
    }

    /// <summary>
    /// Adds a flexible spacer that grows to fill available space along the parent's layout direction.
    /// Use inside horizontal layouts to push siblings apart, or in vertical layouts for vertical spacing.
    /// </summary>
    public static void Spacer()
    {
        using (Clay.Element(new ElementDeclaration
        {
            Layout = new LayoutConfig
            {
                Sizing = new Sizing
                {
                    Width = SizingAxis.Grow(),
                    Height = SizingAxis.Grow()
                }
            }
        })) { }
    }

    // ============ Splitter ============

    /// <summary>
    /// Renders a draggable splitter between two panels. When the user drags the splitter,
    /// size1 increases and size2 decreases (or vice versa) by the drag delta.
    /// Works like ImGui's SplitterBehavior: the caller uses the resulting sizes to lay out
    /// adjacent panels with Fixed sizing.
    /// </summary>
    /// <param name="label">Unique label for this splitter (supports ## suffix for hidden IDs).</param>
    /// <param name="size1">Size of the panel before the splitter (left or top).</param>
    /// <param name="size2">Size of the panel after the splitter (right or bottom).</param>
    /// <param name="minSize1">Minimum allowed size for the first panel.</param>
    /// <param name="minSize2">Minimum allowed size for the second panel.</param>
    /// <param name="vertical">If true, the splitter is a vertical bar between horizontal panels (resizes width).
    /// If false, the splitter is a horizontal bar between vertical panels (resizes height).</param>
    /// <param name="style">Optional visual style.</param>
    /// <returns>True if the splitter is currently being dragged.</returns>
    public static bool Splitter(string label, ref float size1, ref float size2,
        float minSize1 = 50, float minSize2 = 50, bool vertical = true, SplitterStyle? style = null)
    {
        var s = style.HasValue ? style.Value.MergeOver(Style.Splitter) : Style.Splitter;
        var id = StableId(label);
        bool isHovered = !IsDisabled && Clay.PointerOver(id);
        bool justPressed = isHovered && IsMouseJustPressed;

        if (isHovered) _context.HoveredThisFrame.Add(id.Id);

        // Check if this splitter should become active (mouse just pressed on it)
        if (justPressed)
        {
            _context.ActiveSplitterId = id.Id;
            _context.SplitterDragStartMouse = vertical ? _context.MousePosition.X : _context.MousePosition.Y;
            _context.SplitterDragStartSize1 = size1;
        }

        bool isActive = _context.ActiveSplitterId == id.Id;

        // Process drag: compute new size1 directly from mouse delta since drag start
        if (isActive && _context.MousePressed)
        {
            float mousePos = vertical ? _context.MousePosition.X : _context.MousePosition.Y;
            float delta = mousePos - _context.SplitterDragStartMouse;
            float totalSize = size1 + size2;

            float newSize1 = Math.Clamp(_context.SplitterDragStartSize1 + delta, minSize1, totalSize - minSize2);
            float newSize2 = totalSize - newSize1;

            size1 = newSize1;
            size2 = newSize2;
        }

        // Determine visual state
        Color bgColor = isActive ? s.DragColor
            : isHovered ? s.HoverColor
            : s.BackgroundColor;

        // Render the splitter element
        using (Clay.Element(new ElementDeclaration
        {
            Id = id,
            Layout = new LayoutConfig
            {
                Sizing = vertical
                    ? new Sizing(SizingAxis.Fixed(s.Thickness), SizingAxis.Grow())
                    : new Sizing(SizingAxis.Grow(), SizingAxis.Fixed(s.Thickness))
            },
            BackgroundColor = bgColor
        })) { }

        return isActive;
    }

    /// <summary>
    /// Returns true if any splitter is currently being dragged.
    /// Useful for changing the mouse cursor in the application.
    /// </summary>
    public static bool IsSplitterBeingDragged => _context.ActiveSplitterId != 0;

    /// <summary>
    /// Gets the ID of the currently active (being dragged) splitter, or 0 if none.
    /// </summary>
    public static uint ActiveSplitterId => _context.ActiveSplitterId;

    // ============ Disabled Region ============

    /// <summary>
    /// Returns true if currently inside a BeginDisabled/EndDisabled block.
    /// When disabled, widgets do not respond to interaction and are rendered with reduced opacity.
    /// </summary>
    public static bool IsDisabled => _context.DisabledDepth > 0;

    /// <summary>
    /// Begins a disabled region. All widgets rendered between BeginDisabled and EndDisabled
    /// will be non-interactive and visually dimmed. Can be nested.
    /// </summary>
    public static void BeginDisabled()
    {
        _context.DisabledDepth++;
    }

    /// <summary>
    /// Ends a disabled region started by BeginDisabled.
    /// </summary>
    public static void EndDisabled()
    {
        if (_context.DisabledDepth > 0)
            _context.DisabledDepth--;
    }

    /// <summary>
    /// Applies a disabled dim effect to a color (reduces alpha by ~50%).
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Color DisabledColor(Color color)
    {
        return IsDisabled ? new Color(color.R, color.G, color.B, color.A * 0.45f) : color;
    }

    /// <summary>
    /// Converts a SkinImage to an ImageConfig for use in ElementDeclaration.
    /// </summary>
    private static ImageConfig SkinToImageConfig(SkinImage img)
        => new()
        {
            ImageData = img.ImageData,
            SourceDimensions = img.SourceDimensions,
            Slice = img.Slice,
            Tint = IsDisabled
                ? new Color(img.Tint.R, img.Tint.G, img.Tint.B, img.Tint.A * 0.45f)
                : img.Tint
        };

    /// <summary>
    /// Returns true if the element is hovered and interaction is not disabled.
    /// Widgets should use this instead of Clay.PointerOver() directly.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsHovered(ElementId id)
    {
        if (IsDisabled) return false;
        if (!IsInsidePopup && IsMouseOverAnyPopup) return false;
        if (IsInsideWindow && !IsCurrentWindowTopmost) return false;
        if (!IsInsideWindow && !IsInsidePopup && IsMouseOverAnyWindow) return false;
        return Clay.PointerOver(id);
    }

    /// <summary>
    /// Begins a horizontal layout group. Call EndHorizontal() when done.
    /// </summary>
    /// <param name="gap">Gap between children.</param>
    /// <param name="alignment">Child alignment.</param>
    /// <param name="scroll">Enable horizontal scrolling with automatic scrollbar.</param>
    /// <param name="maxWidth">Maximum width before scrolling (only used when scroll=true).</param>
    /// <param name="style">Optional visual style (background, border, corner radius, padding, sizing).</param>
    public static void BeginHorizontal(ushort gap = 8, ChildAlignment alignment = default, bool scroll = false, float? maxWidth = null, LayoutStyle? style = null)
    {
        var hasStyle = style.HasValue;
        var s = style.GetValueOrDefault();
        var hasSizingW = hasStyle && s.Sizing.Width.MinMax.Max != 0;
        var hasSizingH = hasStyle && s.Sizing.Height.MinMax.Max != 0;

        if (scroll)
        {
            var scrollId = Id("HLayout");
            _context.LayoutScrollInfo.Push(new ClayUIContext.ScrollWrapperInfo(scrollId, IsVertical: false, HasWrapper: true));

            // Create wrapper container (vertical: content + scrollbar)
            Clay.Element(new ElementDeclaration
            {
                Layout = new LayoutConfig
                {
                    Direction = LayoutDirection.TopToBottom,
                    Padding = hasStyle ? s.Padding : default,
                    Sizing = new Sizing
                    {
                        Width = maxWidth.HasValue ? SizingAxis.Fit(0, maxWidth.Value) : hasSizingW ? s.Sizing.Width : SizingAxis.Grow(),
                        Height = hasSizingH ? s.Sizing.Height : SizingAxis.Fit()
                    }
                },
                BackgroundColor = hasStyle ? s.BackgroundColor : default,
                CornerRadius = hasStyle ? s.CornerRadius : default,
                Border = hasStyle ? s.Border : default,
                Shadow = hasStyle ? s.Shadow : default
            });
            _context.LayoutDepth++;

            // Create scroll container inside wrapper
            Clay.Element(new ElementDeclaration
            {
                Id = scrollId,
                Layout = new LayoutConfig
                {
                    Direction = LayoutDirection.LeftToRight,
                    ChildGap = gap,
                    ChildAlignment = alignment,
                    Sizing = new Sizing
                    {
                        Width = SizingAxis.Grow(),
                        Height = SizingAxis.Fit()
                    }
                },
                Scroll = ScrollConfig.HorizontalScroll
            });
            _context.LayoutDepth++;
        }
        else
        {
            _context.LayoutScrollInfo.Push(null);

            Clay.Element(new ElementDeclaration
            {
                Layout = new LayoutConfig
                {
                    Direction = LayoutDirection.LeftToRight,
                    ChildGap = gap,
                    ChildAlignment = alignment,
                    Padding = hasStyle ? s.Padding : default,
                    ClipContent = s.ClipContent,
                    Sizing = new Sizing
                    {
                        Width = hasSizingW ? s.Sizing.Width : SizingAxis.Grow(),
                        Height = hasSizingH ? s.Sizing.Height : SizingAxis.Fit()
                    }
                },
                BackgroundColor = hasStyle ? s.BackgroundColor : default,
                CornerRadius = hasStyle ? s.CornerRadius : default,
                Border = hasStyle ? s.Border : default,
                Shadow = hasStyle ? s.Shadow : default
            });
            _context.LayoutDepth++;
        }
    }

    /// <summary>
    /// Ends a horizontal layout group.
    /// </summary>
    public static void EndHorizontal()
    {
        if (_context.LayoutDepth > 0)
        {
            ClayUIContext.ScrollWrapperInfo? scrollInfo = null;
            if (_context.LayoutScrollInfo.Count > 0)
            {
                scrollInfo = _context.LayoutScrollInfo.Pop();
            }

            // Close the scroll container (or simple container if no scroll)
            Clay.CloseElement();
            _context.LayoutDepth--;

            if (scrollInfo.HasValue && scrollInfo.Value.HasWrapper)
            {
                // Add scrollbar as sibling to scroll container
                HorizontalScrollbar(scrollInfo.Value.ScrollId);

                // Close the wrapper container
                Clay.CloseElement();
                _context.LayoutDepth--;
            }
        }
    }

    /// <summary>
    /// Begins a vertical layout group. Call EndVertical() when done.
    /// </summary>
    /// <param name="gap">Gap between children.</param>
    /// <param name="alignment">Child alignment.</param>
    /// <param name="scroll">Enable vertical scrolling with automatic scrollbar.</param>
    /// <param name="maxHeight">Maximum height before scrolling (only used when scroll=true).</param>
    /// <param name="style">Optional visual style (background, border, corner radius, padding, sizing).</param>
    public static void BeginVertical(ushort gap = 8, ChildAlignment alignment = default, bool scroll = false, float? maxHeight = null, LayoutStyle? style = null)
    {
        var hasStyle = style.HasValue;
        var s = style.GetValueOrDefault();
        var hasSizingW = hasStyle && s.Sizing.Width.MinMax.Max != 0;
        var hasSizingH = hasStyle && s.Sizing.Height.MinMax.Max != 0;

        if (scroll)
        {
            var scrollId = Id("VLayout");
            _context.LayoutScrollInfo.Push(new ClayUIContext.ScrollWrapperInfo(scrollId, IsVertical: true, HasWrapper: true));

            // Create wrapper container (horizontal: content + scrollbar)
            Clay.Element(new ElementDeclaration
            {
                Layout = new LayoutConfig
                {
                    Direction = LayoutDirection.LeftToRight,
                    Padding = hasStyle ? s.Padding : default,
                    Sizing = new Sizing
                    {
                        Width = hasSizingW ? s.Sizing.Width : SizingAxis.Grow(),
                        Height = maxHeight.HasValue ? SizingAxis.Fit(0, maxHeight.Value) : hasSizingH ? s.Sizing.Height : SizingAxis.Fit()
                    }
                },
                BackgroundColor = hasStyle ? s.BackgroundColor : default,
                CornerRadius = hasStyle ? s.CornerRadius : default,
                Border = hasStyle ? s.Border : default,
                Shadow = hasStyle ? s.Shadow : default
            });
            _context.LayoutDepth++;

            // Create scroll container inside wrapper
            Clay.Element(new ElementDeclaration
            {
                Id = scrollId,
                Layout = new LayoutConfig
                {
                    Direction = LayoutDirection.TopToBottom,
                    ChildGap = gap,
                    ChildAlignment = alignment,
                    Sizing = new Sizing
                    {
                        Width = SizingAxis.Grow(),
                        Height = SizingAxis.Grow()
                    }
                },
                Scroll = ScrollConfig.VerticalScroll
            });
            _context.LayoutDepth++;
        }
        else
        {
            _context.LayoutScrollInfo.Push(null);

            Clay.Element(new ElementDeclaration
            {
                Layout = new LayoutConfig
                {
                    Direction = LayoutDirection.TopToBottom,
                    ChildGap = gap,
                    ChildAlignment = alignment,
                    Padding = hasStyle ? s.Padding : default,
                    ClipContent = s.ClipContent,
                    Sizing = new Sizing
                    {
                        Width = hasSizingW ? s.Sizing.Width : SizingAxis.Grow(),
                        Height = hasSizingH ? s.Sizing.Height : SizingAxis.Fit()
                    }
                },
                BackgroundColor = hasStyle ? s.BackgroundColor : default,
                CornerRadius = hasStyle ? s.CornerRadius : default,
                Border = hasStyle ? s.Border : default,
                Shadow = hasStyle ? s.Shadow : default
            });
            _context.LayoutDepth++;
        }
    }

    /// <summary>
    /// Ends a vertical layout group.
    /// </summary>
    public static void EndVertical()
    {
        if (_context.LayoutDepth > 0)
        {
            ClayUIContext.ScrollWrapperInfo? scrollInfo = null;
            if (_context.LayoutScrollInfo.Count > 0)
            {
                scrollInfo = _context.LayoutScrollInfo.Pop();
            }

            // Close the scroll container (or simple container if no scroll)
            Clay.CloseElement();
            _context.LayoutDepth--;

            if (scrollInfo.HasValue && scrollInfo.Value.HasWrapper)
            {
                // Add scrollbar as sibling to scroll container
                VerticalScrollbar(scrollInfo.Value.ScrollId);

                // Close the wrapper container
                Clay.CloseElement();
                _context.LayoutDepth--;
            }
        }
    }

    // ============ Containers ============

    /// <summary>
    /// Begins a panel/window container. Call EndPanel() when done.
    /// </summary>
    /// <param name="title">Panel title.</param>
    /// <param name="style">Optional panel style.</param>
    /// <param name="scroll">Enable vertical scrolling with automatic scrollbar.</param>
    /// <param name="maxHeight">Maximum height before scrolling (only used when scroll=true).</param>
    public static void BeginPanel(string title, PanelStyle? style = null, bool scroll = false, float? maxHeight = null, PanelSkin? skin = null)
    {
        var s = style.HasValue ? style.Value.MergeOver(Style.Panel) : Style.Panel;
        var sk = skin ?? Skin?.Panel ?? default;
        var panelId = StableId($"Panel_{title}");
        var displayTitle = ElementId.GetDisplayLabel(title).ToString();

        if (scroll)
        {
            var scrollId = Id($"PanelScroll_{title}");
            _context.PanelScrollInfo.Push(new ClayUIContext.ScrollWrapperInfo(scrollId, IsVertical: true, HasWrapper: true));

            // Panel container (non-scrollable, has styling)
            Clay.Element(new ElementDeclaration
            {
                Id = panelId,
                Layout = new LayoutConfig
                {
                    Direction = LayoutDirection.TopToBottom,
                    Padding = s.Padding,
                    ChildGap = s.ChildGap,
                    Sizing = new Sizing
                    {
                        Width = SizingAxis.Grow(),
                        Height = maxHeight.HasValue ? SizingAxis.Fit(0, maxHeight.Value) : SizingAxis.Grow()
                    }
                },
                BackgroundColor = sk.Background.HasImage ? Color.Transparent : s.BackgroundColor,
                CornerRadius = sk.Background.HasImage ? CornerRadius.Zero : s.CornerRadius,
                Border = sk.Background.HasImage ? default : s.Border,
                Image = sk.Background.HasImage ? SkinToImageConfig(sk.Background) : default,
                Shadow = s.Shadow
            });
            _context.PanelDepth++;

            // Title bar (outside scroll area)
            if (!string.IsNullOrEmpty(title))
            {
                using (Clay.Element(new ElementDeclaration
                {
                    Layout = new LayoutConfig
                    {
                        Sizing = new Sizing { Width = SizingAxis.Grow() },
                        Padding = new Padding { Bottom = 8 }
                    }
                }))
                {
                    Clay.Text(displayTitle, new TextConfig
                    {
                        FontId = s.TitleFontId,
                        FontSize = s.TitleFontSize,
                        TextColor = s.TitleColor
                    });
                }

                Separator(s.SeparatorColor);
                Space(4);
            }

            // Content wrapper (horizontal: scroll area + scrollbar)
            // Bottom corners match panel's bottom corners (accounting for padding inset)
            Clay.Element(new ElementDeclaration
            {
                Layout = new LayoutConfig
                {
                    Direction = LayoutDirection.LeftToRight,
                    Sizing = new Sizing
                    {
                        Width = SizingAxis.Grow(),
                        Height = SizingAxis.Grow()
                    }
                },
                CornerRadius = new CornerRadius
                {
                    BottomLeft = Math.Max(0, s.CornerRadius.BottomLeft - s.Padding.Bottom),
                    BottomRight = Math.Max(0, s.CornerRadius.BottomRight - s.Padding.Bottom)
                }
            });
            _context.PanelDepth++;

            // Scroll container
            // Bottom-left corner matches wrapper
            Clay.Element(new ElementDeclaration
            {
                Id = scrollId,
                Layout = new LayoutConfig
                {
                    Direction = LayoutDirection.TopToBottom,
                    ChildGap = s.ChildGap,
                    Sizing = new Sizing
                    {
                        Width = SizingAxis.Grow(),
                        Height = SizingAxis.Grow()
                    }
                },
                Scroll = ScrollConfig.VerticalScroll,
                CornerRadius = new CornerRadius
                {
                    BottomLeft = Math.Max(0, s.CornerRadius.BottomLeft - s.Padding.Bottom)
                }
            });
            _context.PanelDepth++;
        }
        else
        {
            _context.PanelScrollInfo.Push(null);

            Clay.Element(new ElementDeclaration
            {
                Id = panelId,
                Layout = new LayoutConfig
                {
                    Direction = LayoutDirection.TopToBottom,
                    Padding = s.Padding,
                    ChildGap = s.ChildGap,
                    Sizing = new Sizing
                    {
                        Width = SizingAxis.Grow(),
                        Height = SizingAxis.Fit()
                    }
                },
                BackgroundColor = sk.Background.HasImage ? Color.Transparent : s.BackgroundColor,
                CornerRadius = sk.Background.HasImage ? CornerRadius.Zero : s.CornerRadius,
                Border = sk.Background.HasImage ? default : s.Border,
                Image = sk.Background.HasImage ? SkinToImageConfig(sk.Background) : default,
                Shadow = s.Shadow
            });
            _context.PanelDepth++;

            // Title bar
            if (!string.IsNullOrEmpty(title))
            {
                using (Clay.Element(new ElementDeclaration
                {
                    Layout = new LayoutConfig
                    {
                        Sizing = new Sizing { Width = SizingAxis.Grow() },
                        Padding = new Padding { Bottom = 8 }
                    }
                }))
                {
                    Clay.Text(displayTitle, new TextConfig
                    {
                        FontId = s.TitleFontId,
                        FontSize = s.TitleFontSize,
                        TextColor = s.TitleColor
                    });
                }

                Separator(s.SeparatorColor);
                Space(4);
            }
        }
    }

    /// <summary>
    /// Ends a panel started with BeginPanel.
    /// </summary>
    public static void EndPanel()
    {
        if (_context.PanelDepth > 0)
        {
            ClayUIContext.ScrollWrapperInfo? scrollInfo = null;
            if (_context.PanelScrollInfo.Count > 0)
            {
                scrollInfo = _context.PanelScrollInfo.Pop();
            }

            // Close the scroll container (or panel if no scroll)
            Clay.CloseElement();
            _context.PanelDepth--;

            if (scrollInfo.HasValue && scrollInfo.Value.HasWrapper)
            {
                // Add scrollbar as sibling to scroll container
                VerticalScrollbar(scrollInfo.Value.ScrollId);

                // Close the content wrapper
                Clay.CloseElement();
                _context.PanelDepth--;

                // Close the panel container
                Clay.CloseElement();
                _context.PanelDepth--;
            }
        }
    }

    // ============ Window Widget ============

    /// <summary>
    /// Begins a draggable, collapsible window. Returns true if window is open and not collapsed.
    /// Call EndWindow() when done (always call it, even if BeginWindow returns false).
    /// </summary>
    /// <param name="title">Window title displayed in title bar.</param>
    /// <param name="open">Reference to open state. Set to false when close button is clicked.</param>
    /// <param name="style">Optional window style.</param>
    /// <param name="defaultPosition">Initial position when window is first created.</param>
    /// <param name="defaultSize">Initial size when window is first created.</param>
    /// <param name="flags">Window behavior flags.</param>
    /// <param name="topmost">If true, window always stays above non-topmost windows.</param>
    /// <returns>True if window content should be rendered (window is open and not collapsed).</returns>
    public static bool BeginWindow(string title, ref bool open, WindowStyle? style = null,
        Vector2? defaultPosition = null, Vector2? defaultSize = null, WindowFlags flags = WindowFlags.None,
        bool topmost = false, WindowSkin? skin = null)
    {
        if (!open)
        {
            _context.WindowStack.Push(0); // Push dummy ID for closed window
            return false;
        }

        var s = style.HasValue ? style.Value.MergeOver(Style.Window) : Style.Window;
        var sk = skin ?? Skin?.Window ?? default;
        var id = StableId($"Window_{title}");
        var displayTitle = ElementId.GetDisplayLabel(title).ToString();

        // Track window title for dock system (needed when window gets docked via drag)
        _context.DockedWindowTitles[id.Id] = displayTitle;

        // Track NoDocking flag so drag-to-dock can check it
        if (flags.HasFlag(WindowFlags.NoDocking))
            _context.NoDockingWindows.Add(id.Id);

        // Get or create window state
        if (!_context.WindowStates.TryGetValue(id.Id, out var state))
        {
            state = new ClayUIContext.WindowState(
                Position: defaultPosition ?? new Vector2(100, 100),
                Size: defaultSize ?? new Vector2(300, 200),
                Collapsed: false,
                Open: true,
                Topmost: topmost
            );
            _context.WindowStates[id.Id] = state;
        }
        else
        {
            // Update topmost and open state
            if (!state.Open || state.Topmost != topmost)
            {
                state = state with { Open = true, Topmost = topmost };
                _context.WindowStates[id.Id] = state;
            }
        }

        // Check if this window is docked in the active dock space
        if (_context.DockSpaceStack.Count > 0 && !flags.HasFlag(WindowFlags.NoDocking))
        {
            var spaceId = _context.DockSpaceStack.Peek();
            if (_context.DockSpaceStates.TryGetValue(spaceId, out var dockSpace))
            {
                // Auto-dock: if the window isn't already in the dock tree, add it
                // Skip windows being actively dragged (they were just undocked)
                bool isDragging = _context.ActiveDragWindowId == id.Id;
                if (!dockSpace.WindowToNode.ContainsKey(id.Id) && !isDragging)
                {
                    AutoDockWindow(dockSpace, id.Id, displayTitle);
                }
            }

            if (_context.DockSpaceStates.TryGetValue(spaceId, out dockSpace) &&
                dockSpace.WindowToNode.TryGetValue(id.Id, out var leafNode))
            {
                // Track this window title for tab bar display
                _context.DockedWindowTitles[id.Id] = displayTitle;
                _context.DockableWindowsThisFrame.Add(id.Id);

                _context.WindowStack.Push(id.Id);
                _context.WindowDepth++;

                // Is this the active tab?
                bool isActiveTab = leafNode.ActiveTabIndex >= 0 &&
                    leafNode.ActiveTabIndex < leafNode.DockedWindowIds.Count &&
                    leafNode.DockedWindowIds[leafNode.ActiveTabIndex] == id.Id;

                if (!isActiveTab)
                {
                    _context.WindowScrollInfo.Push(null);
                    return false;
                }

                // Get the leaf content area bounds from previous frame
                var leafContentId = ElementId.HashComposite("DockLeafArea_", leafNode.Id);
                var leafData = Clay.GetElementData(leafContentId);

                if (!leafData.Found)
                {
                    // First frame — content area not laid out yet, skip rendering
                    _context.WindowScrollInfo.Push(null);
                    return false;
                }

                var ds = Style.DockSpace;
                var scrollId = ElementId.HashComposite("DockWinScroll_", id.Id);
                bool showScrollbar = !flags.HasFlag(WindowFlags.NoScroll);

                // Store scroll info so EmitDockLeaf can render the scrollbar
                if (showScrollbar)
                    _context.DockedWindowScrollIds[id.Id] = scrollId;
                else
                    _context.DockedWindowScrollIds.Remove(id.Id);

                // Open content as a floating element positioned at the leaf content area
                Clay.Element(new ElementDeclaration
                {
                    Id = id,
                    Layout = new LayoutConfig
                    {
                        Direction = LayoutDirection.TopToBottom,
                        Sizing = Sizing.FixedSize(leafData.BoundingBox.Width, leafData.BoundingBox.Height),
                        ClipContent = true
                    },
                    Floating = new FloatingConfig
                    {
                        AttachTo = FloatingAttachTo.Root,
                        Offset = new Vector2(leafData.BoundingBox.X, leafData.BoundingBox.Y),
                        ZIndex = 1
                    }
                });

                // Scroll container for content
                var ws = style ?? Style.Window;
                Clay.Element(new ElementDeclaration
                {
                    Id = scrollId,
                    Layout = new LayoutConfig
                    {
                        Direction = LayoutDirection.TopToBottom,
                        Sizing = Sizing.Fill(),
                        Padding = ws.ContentPadding,
                        ChildGap = ws.ContentGap
                    },
                    Scroll = ScrollConfig.VerticalScroll
                });

                _context.WindowScrollInfo.Push(new ClayUIContext.WindowFrameInfo(scrollId, false, false, false));
                return true;
            }
        }

        _context.WindowStack.Push(id.Id);
        _context.WindowDepth++;

        var titleBarId = ElementId.HashComposite("WinTitle_", id.Id);
        var collapseButtonId = ElementId.HashComposite("WinCollapse_", id.Id);
        var closeButtonId = ElementId.HashComposite("WinClose_", id.Id);

        // Calculate window bounds
        float windowHeight = state.Collapsed ? s.TitleBarHeight : state.Size.Y;
        var windowBounds = new BoundingBox(
            state.Position.X, state.Position.Y,
            state.Size.X, windowHeight
        );

        // Track window bounds for input blocking (clear previous frame's data on first window)
        if (!_context.WindowBoundsRebuiltThisFrame)
        {
            _context.OpenWindowBounds.Clear();
            _context.WindowBoundsRebuiltThisFrame = true;
        }
        _context.OpenWindowBounds.Add((id.Id, windowBounds));

        // Manual bounds checking for window hover (Clay.PointerOver doesn't respect z-order)
        var mouse = _context.MousePosition;
        bool isMouseInWindowBounds = mouse.X >= windowBounds.X && mouse.X <= windowBounds.X + windowBounds.Width &&
                                      mouse.Y >= windowBounds.Y && mouse.Y <= windowBounds.Y + windowBounds.Height;

        // Check if this window is topmost at mouse position by checking all windows with higher focus order
        bool isTopmostAtMouse = isMouseInWindowBounds;
        if (isMouseInWindowBounds)
        {
            int myOrder = _context.WindowFocusOrder.IndexOf(id.Id);
            if (myOrder < 0) myOrder = _context.WindowFocusOrder.Count; // New window, will be added at end

            // Check if any other OPEN window with higher focus order is also under the mouse
            foreach (var (otherId, otherState) in _context.WindowStates)
            {
                if (otherId == id.Id) continue;
                if (!otherState.Open) continue; // Skip closed windows

                int otherOrder = _context.WindowFocusOrder.IndexOf(otherId);
                if (otherOrder <= myOrder) continue; // Other window is below us in focus order

                // Check if other window's bounds contain the mouse
                float otherHeight = otherState.Collapsed ? s.TitleBarHeight : otherState.Size.Y;
                if (mouse.X >= otherState.Position.X && mouse.X <= otherState.Position.X + otherState.Size.X &&
                    mouse.Y >= otherState.Position.Y && mouse.Y <= otherState.Position.Y + otherHeight)
                {
                    isTopmostAtMouse = false;
                    break;
                }
            }
        }

        bool isBeingDragged = _context.ActiveDragWindowId == id.Id;

        // Manual title bar bounds check (more reliable than Clay.PointerOver for floating elements)
        bool isInTitleBarBounds = isMouseInWindowBounds &&
            mouse.Y >= state.Position.Y && mouse.Y <= state.Position.Y + s.TitleBarHeight;

        // Use Clay.PointerOver for buttons within the window, but only if we're topmost
        bool isTitleBarHovered = isTopmostAtMouse && isInTitleBarBounds;
        bool isCollapseHovered = isTopmostAtMouse && IsHovered(collapseButtonId);
        bool isCloseHovered = isTopmostAtMouse && IsHovered(closeButtonId);

        // Bring window to front when clicked anywhere on it (only if topmost at that position, not blocked by popup)
        if (isTopmostAtMouse && IsMouseJustPressed && !IsMouseOverAnyPopup)
        {
            _context.BringWindowToFront(id.Id);
        }

        // Any click on the title bar is consumed so it doesn't pass through to content widgets
        if (isTitleBarHovered && IsMouseJustPressed && !IsMouseOverAnyPopup)
        {
            _context.ClickConsumedThisFrame = true;
        }

        // Handle title bar drag (only if topmost, in title bar, not clicking buttons, and move is enabled)
        if (isTitleBarHovered && !isCollapseHovered && !isCloseHovered && IsMouseJustPressed && !IsMouseOverAnyPopup && !flags.HasFlag(WindowFlags.NoMove))
        {
            _context.ActiveDragWindowId = id.Id;
            // Use actual position for drag offset calculation
            _context.WindowDragOffset = new Vector2(
                _context.MousePosition.X - state.Position.X,
                _context.MousePosition.Y - state.Position.Y
            );
        }

        // Handle collapse button
        if (isCollapseHovered && IsMouseJustPressed && !IsMouseOverAnyPopup && !flags.HasFlag(WindowFlags.NoCollapse))
        {
            state = state with { Collapsed = !state.Collapsed };
            _context.WindowStates[id.Id] = state;
        }

        // Handle close button
        if (isCloseHovered && IsMouseJustPressed && !IsMouseOverAnyPopup && !flags.HasFlag(WindowFlags.NoClose))
        {
            open = false;
            _context.WindowStates[id.Id] = state with { Open = false };
        }

        // Resize handle detection (only when not collapsed and resize is enabled)
        bool isBeingResized = _context.ActiveResizeWindowId == id.Id;
        ResizeDirection resizeDir = ResizeDirection.None;

        if (!state.Collapsed && !flags.HasFlag(WindowFlags.NoResize) && isTopmostAtMouse && !isInTitleBarBounds)
        {
            float handleSize = s.ResizeHandleSize;
            float left = state.Position.X;
            float right = state.Position.X + state.Size.X;
            float top = state.Position.Y;
            float bottom = state.Position.Y + state.Size.Y;

            // Check edges
            bool nearLeft = mouse.X >= left && mouse.X <= left + handleSize;
            bool nearRight = mouse.X >= right - handleSize && mouse.X <= right;
            bool nearTop = mouse.Y >= top && mouse.Y <= top + handleSize;
            bool nearBottom = mouse.Y >= bottom - handleSize && mouse.Y <= bottom;

            // Determine resize direction
            if (nearRight && nearBottom) resizeDir = ResizeDirection.BottomRight;
            else if (nearLeft && nearBottom) resizeDir = ResizeDirection.BottomLeft;
            else if (nearRight && nearTop) resizeDir = ResizeDirection.TopRight;
            else if (nearLeft && nearTop) resizeDir = ResizeDirection.TopLeft;
            else if (nearRight) resizeDir = ResizeDirection.Right;
            else if (nearLeft) resizeDir = ResizeDirection.Left;
            else if (nearBottom) resizeDir = ResizeDirection.Bottom;
            else if (nearTop) resizeDir = ResizeDirection.Top;

            // Start resize on click (blocked by open popups)
            if (resizeDir != ResizeDirection.None && IsMouseJustPressed && !IsMouseOverAnyPopup)
            {
                _context.ActiveResizeWindowId = id.Id;
                _context.ActiveResizeDirection = resizeDir;
                _context.ResizeStartMousePos = _context.MousePosition;
                _context.ResizeStartSize = state.Size;
                _context.ResizeStartPos = state.Position;
                _context.ClickConsumedThisFrame = true;
            }
        }

        // Handle mouse wheel scrolling for this window (if topmost and not collapsed)
        if (isTopmostAtMouse && !state.Collapsed && (_context.ScrollDelta.X != 0 || _context.ScrollDelta.Y != 0))
        {
            var scrollId = ElementId.HashComposite("WinScroll_", id.Id);
            var scrollData = Clay.GetScrollContainerData(scrollId);
            if (scrollData.Found)
            {
                // Apply scroll delta (positive Y = scroll up, content moves down, scroll position decreases)
                float newScrollY = Math.Clamp(
                    scrollData.ScrollPosition.Y - _context.ScrollDelta.Y * 30,
                    0,
                    scrollData.MaxScrollY
                );
                float newScrollX = Math.Clamp(
                    scrollData.ScrollPosition.X - _context.ScrollDelta.X * 30,
                    0,
                    scrollData.MaxScrollX
                );
                Clay.SetScrollPosition(scrollId, new Vector2(newScrollX, newScrollY));
                _context.ScrollConsumedByWindow = true;
            }
        }

        // Get dynamic z-index based on focus order
        short zIndex = _context.GetWindowZIndex(id.Id);

        // Window container (floating at position)
        Clay.Element(new ElementDeclaration
        {
            Id = id,
            Layout = new LayoutConfig
            {
                Direction = LayoutDirection.TopToBottom,
                Sizing = state.Collapsed
                    ? new Sizing { Width = SizingAxis.Fixed(state.Size.X), Height = SizingAxis.Fit() }
                    : new Sizing { Width = SizingAxis.Fixed(state.Size.X), Height = SizingAxis.Fixed(state.Size.Y) }
            },
            Floating = new FloatingConfig
            {
                AttachTo = FloatingAttachTo.Root,
                Offset = state.Position,
                ZIndex = zIndex
            },
            BackgroundColor = sk.Body.HasImage ? Color.Transparent : s.BackgroundColor,
            CornerRadius = sk.Body.HasImage ? CornerRadius.Zero : s.CornerRadius,
            Border = sk.Body.HasImage ? default : s.Border,
            Image = sk.Body.HasImage ? SkinToImageConfig(sk.Body) : default,
            Shadow = s.Shadow
        });

        // Title bar
        using (Clay.Element(new ElementDeclaration
        {
            Id = titleBarId,
            Layout = new LayoutConfig
            {
                Direction = LayoutDirection.LeftToRight,
                Sizing = new Sizing { Width = SizingAxis.Grow(), Height = SizingAxis.Fixed(s.TitleBarHeight) },
                Padding = s.TitleBarPadding,
                ChildAlignment = ChildAlignment.CenterLeft,
                ChildGap = 8
            },
            BackgroundColor = sk.TitleBar.HasImage ? Color.Transparent : (isBeingDragged ? s.TitleBarDragColor : s.TitleBarColor),
            CornerRadius = sk.TitleBar.HasImage ? CornerRadius.Zero : new CornerRadius { TopLeft = s.CornerRadius.TopLeft, TopRight = s.CornerRadius.TopRight },
            Image = sk.TitleBar.HasImage ? SkinToImageConfig(sk.TitleBar) : default
        }))
        {
            // Collapse button (if enabled)
            if (!flags.HasFlag(WindowFlags.NoCollapse))
            {
                using (Clay.Element(new ElementDeclaration
                {
                    Id = collapseButtonId,
                    Layout = new LayoutConfig
                    {
                        Sizing = Sizing.FixedSize(s.ButtonSize, s.ButtonSize),
                        ChildAlignment = ChildAlignment.Center
                    },
                    BackgroundColor = isCollapseHovered ? s.ButtonHoverColor : Color.Transparent,
                    CornerRadius = CornerRadius.All(s.ButtonSize / 4)
                }))
                {
                    Clay.Text(state.Collapsed ? "+" : "-", new TextConfig
                    {
                        FontId = s.FontId,
                        FontSize = s.FontSize,
                        TextColor = s.TitleColor
                    });
                }
            }

            // Title text (grows to fill space)
            using (Clay.Element(new ElementDeclaration
            {
                Layout = new LayoutConfig
                {
                    Sizing = new Sizing { Width = SizingAxis.Grow(), Height = SizingAxis.Fit() }
                }
            }))
            {
                Clay.Text(displayTitle, new TextConfig
                {
                    FontId = s.FontId,
                    FontSize = s.FontSize,
                    TextColor = s.TitleColor
                });
            }

            // Close button (if enabled)
            if (!flags.HasFlag(WindowFlags.NoClose))
            {
                using (Clay.Element(new ElementDeclaration
                {
                    Id = closeButtonId,
                    Layout = new LayoutConfig
                    {
                        Sizing = Sizing.FixedSize(s.ButtonSize, s.ButtonSize),
                        ChildAlignment = ChildAlignment.Center
                    },
                    BackgroundColor = isCloseHovered ? s.CloseButtonHoverColor : Color.Transparent,
                    CornerRadius = CornerRadius.All(s.ButtonSize / 4)
                }))
                {
                    Clay.Text("x", new TextConfig
                    {
                        FontId = s.FontId,
                        FontSize = s.FontSize,
                        TextColor = isCloseHovered ? Color.White : s.TitleColor
                    });
                }
            }
        }

        // Content area (only if not collapsed)
        if (!state.Collapsed)
        {
            bool showScrollbar = !flags.HasFlag(WindowFlags.NoScroll);
            bool showResize = !flags.HasFlag(WindowFlags.NoResize);
            bool hasRightColumn = showScrollbar || showResize;
            var scrollId = ElementId.HashComposite("WinScroll_", id.Id);

            // Track window frame info for EndWindow
            _context.WindowScrollInfo.Push(new ClayUIContext.WindowFrameInfo(scrollId, showScrollbar, showResize, hasRightColumn));

            // Content wrapper (horizontal: scroll area + right column)
            // Bottom corners match window's bottom corners
            Clay.Element(new ElementDeclaration
            {
                Layout = new LayoutConfig
                {
                    Direction = LayoutDirection.LeftToRight,
                    Sizing = Sizing.Fill()
                },
                CornerRadius = new CornerRadius
                {
                    BottomLeft = s.CornerRadius.BottomLeft,
                    BottomRight = s.CornerRadius.BottomRight
                }
            });

            // Scroll container (always created for clipping via scissor)
            // Bottom-left corner matches window when no right column
            Clay.Element(new ElementDeclaration
            {
                Id = scrollId,
                Layout = new LayoutConfig
                {
                    Direction = LayoutDirection.TopToBottom,
                    Sizing = Sizing.Fill(),
                    Padding = s.ContentPadding,
                    ChildGap = s.ContentGap
                },
                Scroll = ScrollConfig.VerticalScroll, // Always enable for scissor clipping
                CornerRadius = hasRightColumn
                    ? new CornerRadius { BottomLeft = s.CornerRadius.BottomLeft }
                    : new CornerRadius { BottomLeft = s.CornerRadius.BottomLeft, BottomRight = s.CornerRadius.BottomRight }
            });

            return true;
        }

        _context.WindowScrollInfo.Push(null); // Push null for collapsed windows
        return false;
    }

    /// <summary>
    /// Ends a window started with BeginWindow. Always call this, even if BeginWindow returned false.
    /// </summary>
    public static void EndWindow()
    {
        if (_context.WindowStack.Count > 0)
        {
            // Peek the window ID first — keep it on the stack so scrollbar click detection
            // correctly sees itself as inside the window (ShouldProcessClick checks WindowStack).
            var windowId = _context.WindowStack.Peek();

            // Pop window frame info
            ClayUIContext.WindowFrameInfo? frameInfo = null;
            if (_context.WindowScrollInfo.Count > 0)
            {
                frameInfo = _context.WindowScrollInfo.Pop();
            }

            if (windowId != 0 && _context.WindowDepth > 0)
            {
                // Check if window is docked
                bool isDocked = false;
                foreach (var (_, dockSpace) in _context.DockSpaceStates)
                {
                    if (dockSpace.WindowToNode.ContainsKey(windowId))
                    {
                        isDocked = true;
                        break;
                    }
                }

                if (isDocked)
                {
                    // Docked window: close scroll container + window container
                    // Scrollbar is rendered by EmitDockLeaf in the dock tree layout
                    if (frameInfo.HasValue)
                    {
                        Clay.CloseElement(); // scroll container
                        Clay.CloseElement(); // window container
                    }
                    _context.WindowDepth--;
                    _context.WindowStack.Pop();
                    return;
                }

                // Check if window was collapsed
                if (_context.WindowStates.TryGetValue(windowId, out var state) && !state.Collapsed)
                {
                    // Close scroll container
                    Clay.CloseElement();

                    if (frameInfo.HasValue && frameInfo.Value.HasRightColumn)
                    {
                        var info = frameInfo.Value;
                        var s = Style.Window;

                        // Right column (vertical: scrollbar + resize grip)
                        // Bottom-right corner matches window
                        var rightColumnId = ElementId.HashComposite("WinRCol_", windowId);
                        using (Clay.Element(new ElementDeclaration
                        {
                            Id = rightColumnId,
                            Layout = new LayoutConfig
                            {
                                Direction = LayoutDirection.TopToBottom,
                                Sizing = new Sizing
                                {
                                    Width = SizingAxis.Fixed(Style.Scrollbar.Width),
                                    Height = SizingAxis.Grow()
                                }
                            },
                            CornerRadius = new CornerRadius { BottomRight = s.CornerRadius.BottomRight }
                        }))
                        {
                            // Scrollbar (grows to fill)
                            if (info.ShowScrollbar)
                            {
                                _context.ScrollbarHitAreaId = rightColumnId;
                                VerticalScrollbar(info.ScrollId);
                                _context.ScrollbarHitAreaId = default;
                            }
                            else
                            {
                                // Spacer when no scrollbar
                                using (Clay.Element(new ElementDeclaration
                                {
                                    Layout = new LayoutConfig
                                    {
                                        Sizing = new Sizing { Width = SizingAxis.Grow(), Height = SizingAxis.Grow() }
                                    }
                                })) { }
                            }

                            // Resize grip at bottom (invisible, just reserves space)
                            if (info.ShowResize)
                            {
                                using (Clay.Element(new ElementDeclaration
                                {
                                    Layout = new LayoutConfig
                                    {
                                        Sizing = Sizing.FixedSize(s.ResizeHandleSize * 2, s.ResizeHandleSize * 2)
                                    }
                                })) { }
                            }
                        }
                    }

                    // Close the content wrapper (always opened in BeginWindow)
                    Clay.CloseElement();
                }

                // Close window container
                Clay.CloseElement();
                _context.WindowDepth--;
            }

            // Pop the window ID after all window content (including scrollbar) is processed
            _context.WindowStack.Pop();
        }
    }

    /// <summary>
    /// Gets the current position of a window.
    /// </summary>
    public static Vector2 GetWindowPosition(string title)
    {
        var id = StableId($"Window_{title}");
        if (_context.WindowStates.TryGetValue(id.Id, out var state))
            return state.Position;
        return default;
    }

    /// <summary>
    /// Sets the position of a window.
    /// </summary>
    public static void SetWindowPosition(string title, Vector2 position)
    {
        var id = StableId($"Window_{title}");
        if (_context.WindowStates.TryGetValue(id.Id, out var state))
            _context.WindowStates[id.Id] = state with { Position = position };
    }

    /// <summary>
    /// Gets the current size of a window.
    /// </summary>
    public static Vector2 GetWindowSize(string title)
    {
        var id = StableId($"Window_{title}");
        if (_context.WindowStates.TryGetValue(id.Id, out var state))
            return state.Size;
        return default;
    }

    /// <summary>
    /// Sets the size of a window.
    /// </summary>
    public static void SetWindowSize(string title, Vector2 size)
    {
        var id = StableId($"Window_{title}");
        if (_context.WindowStates.TryGetValue(id.Id, out var state))
            _context.WindowStates[id.Id] = state with { Size = size };
    }

    /// <summary>
    /// Gets whether a window is set to topmost.
    /// </summary>
    public static bool GetWindowTopmost(string title)
    {
        var id = StableId($"Window_{title}");
        if (_context.WindowStates.TryGetValue(id.Id, out var state))
            return state.Topmost;
        return false;
    }

    /// <summary>
    /// Sets whether a window should stay above non-topmost windows.
    /// </summary>
    public static void SetWindowTopmost(string title, bool topmost)
    {
        var id = StableId($"Window_{title}");
        if (_context.WindowStates.TryGetValue(id.Id, out var state))
            _context.WindowStates[id.Id] = state with { Topmost = topmost };
    }

    // ============ Docking ============

    /// <summary>
    /// Gets the dock space state for a given ID, or null if not found.
    /// </summary>
    internal static DockSpaceState? GetDockSpaceState(uint id)
    {
        _context.DockSpaceStates.TryGetValue(id, out var space);
        return space;
    }

    /// <summary>
    /// Gets or creates a dock space state for a given ID.
    /// </summary>
    internal static DockSpaceState GetOrCreateDockSpaceState(uint id)
    {
        if (!_context.DockSpaceStates.TryGetValue(id, out var space))
        {
            space = new DockSpaceState { Id = id };
            _context.DockSpaceStates[id] = space;
        }
        return space;
    }

    /// <summary>
    /// Finds a dock node by ID across all dock spaces.
    /// </summary>
    internal static (DockSpaceState? space, DockNode? node) FindDockNode(uint nodeId)
    {
        foreach (var (_, space) in _context.DockSpaceStates)
        {
            var node = space.RootNode?.FindNode(nodeId);
            if (node != null)
                return (space, node);
        }
        return (null, null);
    }

    /// <summary>
    /// Sets the title for a docked window (used by DockBuilder).
    /// </summary>
    internal static void SetDockedWindowTitle(uint windowId, string title)
    {
        _context.DockedWindowTitles[windowId] = title;
    }

    internal static string? GetDockedWindowTitle(uint windowId)
    {
        return _context.DockedWindowTitles.TryGetValue(windowId, out var title) ? title : null;
    }

    /// <summary>
    /// Clears a dock space layout, causing all windows to be auto-docked again on next frame.
    /// </summary>
    public static void ClearDockSpace(string label)
    {
        var id = StableId(label);
        if (_context.DockSpaceStates.TryGetValue(id.Id, out var space))
        {
            space.NextNodeId = 0;
            space.RootNode = new DockNode { Id = space.GenerateNodeId() };
            space.WindowToNode.Clear();
        }
    }

    /// <summary>
    /// Checks if a window is currently docked in any dock space.
    /// </summary>
    public static bool IsWindowDocked(string title)
    {
        var id = StableId($"Window_{title}");
        foreach (var (_, space) in _context.DockSpaceStates)
        {
            if (space.WindowToNode.ContainsKey(id.Id))
                return true;
        }
        return false;
    }

    /// <summary>
    /// Begins a dock space that fills its parent element. Dockable windows rendered
    /// between BeginDockSpace/EndDockSpace will have their content placed into dock
    /// nodes instead of rendering as floating windows.
    /// </summary>
    /// <summary>
    /// Begins a dock space with an optional setup callback that defines the default layout.
    /// The setup callback only runs once — when the dock space has no existing layout.
    /// </summary>
    /// <example>
    /// ClayUI.BeginDockSpace("Editor", setup: dock => {
    ///     var (main, bottom) = dock.Split(DockSplitDirection.Vertical, 0.7f);
    ///     var (left, right) = dock.Split(main, DockSplitDirection.Horizontal, 0.3f);
    ///     dock.Window(left, "Hierarchy");
    ///     dock.Window(right, "Viewport");
    ///     dock.Window(bottom, "Console");
    /// });
    /// </example>
    public static void BeginDockSpace(string label, Action<DockLayout>? setup = null, DockSpaceStyle? style = null)
    {
        var s = style.HasValue ? style.Value.MergeOver(Style.DockSpace) : Style.DockSpace;
        var id = StableId(label);

        // Get or create dock space state
        var space = GetOrCreateDockSpaceState(id.Id);
        if (space.RootNode == null)
        {
            space.RootNode = new DockNode { Id = space.GenerateNodeId() };
        }

        // Run setup callback once if layout is empty
        if (setup != null && space.RootNode.IsEmpty && space.RootNode.IsLeaf)
        {
            var layout = new DockLayout(space);
            setup(layout);
        }

        // Push onto dock space stack
        _context.DockSpaceStack.Push(id.Id);
        _context.DockableWindowsThisFrame.Clear();

        // Rebuild the WindowToNode lookup
        space.WindowToNode.Clear();
        space.RootNode.RebuildWindowToNodeMap(space.WindowToNode);

        // Open the dock space container element
        Clay.Element(new ElementDeclaration
        {
            Id = id,
            Layout = new LayoutConfig
            {
                Sizing = Sizing.Fill(),
                Direction = LayoutDirection.LeftToRight
            }
        });

        // Recursively emit the dock tree layout
        if (!space.RootNode.IsEmpty || !space.RootNode.IsLeaf)
        {
            EmitDockNode(space.RootNode, space, s);
        }
    }

    /// <summary>
    /// Ends the dock space started with BeginDockSpace.
    /// </summary>
    public static void EndDockSpace()
    {
        // Close dock space container
        Clay.CloseElement();

        if (_context.DockSpaceStack.Count > 0)
        {
            var spaceId = _context.DockSpaceStack.Pop();

            // Handle drag-to-dock: detect window being dragged over dock leaves
            if (_context.ActiveDragWindowId != 0 && _context.DockSpaceStates.TryGetValue(spaceId, out var space))
            {
                UpdateDockDropPreview(space);
            }

            // Execute pending dock operation (from previous frame's drag release)
            if (_context.PendingDockWindowId != 0 && _context.PendingDockSpaceId == spaceId &&
                _context.DockSpaceStates.TryGetValue(spaceId, out var pendingSpace))
            {
                ExecuteDockOperation(pendingSpace);
                _context.PendingDockWindowId = 0;
            }

            // Execute pending undock operation (deferred from tab drag)
            if (_context.PendingUndockWindowId != 0 && _context.PendingUndockSpaceId == spaceId &&
                _context.DockSpaceStates.TryGetValue(spaceId, out var undockSpace))
            {
                var undockWindowId = _context.PendingUndockWindowId;
                if (undockSpace.WindowToNode.TryGetValue(undockWindowId, out var undockLeaf))
                {
                    UndockWindow(undockWindowId, undockLeaf, undockSpace);
                }
                _context.PendingUndockWindowId = 0;
            }

            // Render drop preview overlay
            RenderDockDropPreview();
        }
    }

    /// <summary>
    /// Gets the ID of the currently active dock space, or 0 if not inside one.
    /// </summary>
    public static uint ActiveDockSpaceId => _context.DockSpaceStack.Count > 0 ? _context.DockSpaceStack.Peek() : 0;

    private static void EmitDockNode(DockNode node, DockSpaceState space, DockSpaceStyle s)
    {
        if (node == null) return;

        if (node.IsLeaf)
        {
            EmitDockLeaf(node, space, s);
            return;
        }

        if (node.ChildA == null || node.ChildB == null) return;

        // Internal split node: container with two children and a splitter
        bool isHorizontal = node.SplitDirection == DockSplitDirection.Horizontal;
        var dir = isHorizontal ? LayoutDirection.LeftToRight : LayoutDirection.TopToBottom;
        var nodeId = ElementId.HashComposite("DockSplit_", node.Id);

        using (Clay.Element(new ElementDeclaration
        {
            Id = nodeId,
            Layout = new LayoutConfig
            {
                Direction = dir,
                Sizing = Sizing.Fill()
            }
        }))
        {
            // ChildA container
            var childAId = ElementId.HashComposite("DockChild_", node.ChildA!.Id);
            using (Clay.Element(new ElementDeclaration
            {
                Id = childAId,
                Layout = new LayoutConfig
                {
                    Direction = LayoutDirection.TopToBottom,
                    Sizing = isHorizontal
                        ? new Sizing(SizingAxis.PercentOf(node.SplitRatio), SizingAxis.Grow())
                        : new Sizing(SizingAxis.Grow(), SizingAxis.PercentOf(node.SplitRatio))
                }
            }))
            {
                EmitDockNode(node.ChildA, space, s);
            }

            // Splitter between children
            EmitDockSplitter(node, s);

            // ChildB container
            var childBId = ElementId.HashComposite("DockChild_", node.ChildB!.Id);
            float ratioB = 1.0f - node.SplitRatio;
            using (Clay.Element(new ElementDeclaration
            {
                Id = childBId,
                Layout = new LayoutConfig
                {
                    Direction = LayoutDirection.TopToBottom,
                    Sizing = isHorizontal
                        ? new Sizing(SizingAxis.PercentOf(ratioB), SizingAxis.Grow())
                        : new Sizing(SizingAxis.Grow(), SizingAxis.PercentOf(ratioB))
                }
            }))
            {
                EmitDockNode(node.ChildB, space, s);
            }
        }
    }

    private static void EmitDockLeaf(DockNode leaf, DockSpaceState space, DockSpaceStyle s)
    {
        var leafId = ElementId.HashComposite("DockLeaf_", leaf.Id);

        // Determine if the active tab's window has a scrollbar
        ElementId activeScrollId = default;
        bool hasScrollbar = false;
        if (leaf.ActiveTabIndex >= 0 && leaf.ActiveTabIndex < leaf.DockedWindowIds.Count)
        {
            var activeWindowId = leaf.DockedWindowIds[leaf.ActiveTabIndex];
            if (_context.DockedWindowScrollIds.TryGetValue(activeWindowId, out var scrollId))
            {
                activeScrollId = scrollId;
                hasScrollbar = true;
            }
        }

        // Vertical container: tab bar on top, content area below
        using (Clay.Element(new ElementDeclaration
        {
            Id = leafId,
            Layout = new LayoutConfig
            {
                Direction = LayoutDirection.TopToBottom,
                Sizing = Sizing.Fill()
            }
        }))
        {
            // Tab bar
            RenderDockLeafTabBar(leaf, space, s);

            // Content row: content area + optional scrollbar
            using (Clay.Element(new ElementDeclaration
            {
                Layout = new LayoutConfig
                {
                    Direction = LayoutDirection.LeftToRight,
                    Sizing = Sizing.Fill()
                }
            }))
            {
                // Content area placeholder (active tab's BeginWindow will fill this via floating element)
                var contentId = ElementId.HashComposite("DockLeafArea_", leaf.Id);
                using (Clay.Element(new ElementDeclaration
                {
                    Id = contentId,
                    Layout = new LayoutConfig
                    {
                        Direction = LayoutDirection.TopToBottom,
                        Sizing = Sizing.Fill()
                    },
                    BackgroundColor = s.ContentBackgroundColor
                })) { }

                // Scrollbar column (part of dock layout, not floating window)
                if (hasScrollbar)
                {
                    var rightColumnId = ElementId.HashComposite("DockWinRCol_", leaf.Id);
                    using (Clay.Element(new ElementDeclaration
                    {
                        Id = rightColumnId,
                        Layout = new LayoutConfig
                        {
                            Direction = LayoutDirection.TopToBottom,
                            Sizing = new Sizing
                            {
                                Width = SizingAxis.Fixed(Style.Scrollbar.Width),
                                Height = SizingAxis.Grow()
                            }
                        },
                        BackgroundColor = s.ContentBackgroundColor
                    }))
                    {
                        _context.ScrollbarHitAreaId = rightColumnId;
                        VerticalScrollbar(activeScrollId);
                        _context.ScrollbarHitAreaId = default;
                    }
                }
            }

            // Track leaf bounds for drop zone hit testing
            var leafData = Clay.GetElementData(leafId);
            if (leafData.Found)
            {
                _context.DockLeafBounds.Add((leaf.Id, leafData.BoundingBox, space.Id));
            }
        }
    }

    private static void RenderDockLeafTabBar(DockNode leaf, DockSpaceState space, DockSpaceStyle s)
    {
        var tabBarId = ElementId.HashComposite("DockTabBar_", leaf.Id);

        using (Clay.Element(new ElementDeclaration
        {
            Id = tabBarId,
            Layout = new LayoutConfig
            {
                Direction = LayoutDirection.LeftToRight,
                Sizing = new Sizing(SizingAxis.Grow(), SizingAxis.Fixed(s.TabBarHeight)),
                ChildAlignment = ChildAlignment.BottomLeft
            },
            BackgroundColor = s.TabBarColor
        }))
        {
            for (int i = 0; i < leaf.DockedWindowIds.Count; i++)
            {
                var windowId = leaf.DockedWindowIds[i];
                string windowTitle = _context.DockedWindowTitles.TryGetValue(windowId, out var title) ? title : "?";
                bool isActive = i == leaf.ActiveTabIndex;

                var tabId = ElementId.Hash($"DockTab_{leaf.Id}_{i}");
                bool isHovered = !IsDisabled && Clay.PointerOver(tabId);

                // Handle tab click / drag start
                if (isHovered && IsMouseJustPressed)
                {
                    leaf.ActiveTabIndex = i;
                    isActive = true;

                    // Start tab drag tracking (for undocking)
                    _context.ActiveDockTabDragWindowId = windowId;
                    _context.ActiveDockTabSourceSpaceId = space.Id;
                    _context.DockTabDragStartPos = _context.MousePosition;
                    _context.DockTabDragUndocked = false;
                }

                // Handle tab drag → undock (deferred to avoid modifying tree during traversal)
                if (_context.ActiveDockTabDragWindowId == windowId && !_context.DockTabDragUndocked &&
                    _context.MousePressed)
                {
                    float dx = _context.MousePosition.X - _context.DockTabDragStartPos.X;
                    float dy = _context.MousePosition.Y - _context.DockTabDragStartPos.Y;
                    float dist = MathF.Sqrt(dx * dx + dy * dy);

                    if (dist > s.UndockThreshold)
                    {
                        // Defer the undock to EndDockSpace (after tree traversal is done)
                        _context.PendingUndockWindowId = windowId;
                        _context.PendingUndockSpaceId = space.Id;
                        _context.DockTabDragUndocked = true;

                        // Transfer to normal window drag
                        _context.ActiveDragWindowId = windowId;
                        _context.WindowDragOffset = new Vector2(s.TabMinWidth / 2, s.TabBarHeight / 2);
                        _context.ActiveDockTabDragWindowId = 0;
                    }
                }

                Color tabColor = isActive ? s.TabActiveColor
                    : isHovered ? s.TabHoverColor
                    : s.TabInactiveColor;

                using (Clay.Element(new ElementDeclaration
                {
                    Id = tabId,
                    Layout = new LayoutConfig
                    {
                        Sizing = new Sizing(
                            SizingAxis.Fit(s.TabMinWidth, s.TabMaxWidth),
                            SizingAxis.Grow()),
                        Padding = Padding.Horizontal((ushort)s.TabPadding),
                        ChildAlignment = ChildAlignment.CenterLeft
                    },
                    BackgroundColor = tabColor
                }))
                {
                    Clay.Text(windowTitle, new TextConfig
                    {
                        FontId = s.FontId,
                        FontSize = s.FontSize,
                        TextColor = isActive ? s.TabActiveTextColor : s.TabTextColor
                    });
                }
            }
        }
    }

    private static void UpdateDockDropPreview(DockSpaceState space)
    {
        var mouse = _context.MousePosition;
        _context.DockDropTargetNodeId = 0;
        _context.DockDropZone = DockDropZone.None;
        _context.DockDropTargetSpaceId = 0;

        var dragWindowId = _context.ActiveDragWindowId;
        // Skip windows that have NoDocking flag or are already docked
        if (_context.NoDockingWindows.Contains(dragWindowId) || space.WindowToNode.ContainsKey(dragWindowId))
            return;

        // Hit-test dock leaf bounds (from previous frame)
        foreach (var (nodeId, bounds, leafSpaceId) in _context.DockLeafBounds)
        {
            if (leafSpaceId != space.Id) continue;
            if (mouse.X < bounds.X || mouse.X > bounds.X + bounds.Width ||
                mouse.Y < bounds.Y || mouse.Y > bounds.Y + bounds.Height)
                continue;

            _context.DockDropTargetNodeId = nodeId;
            _context.DockDropTargetSpaceId = space.Id;

            // Determine drop zone based on position within the leaf
            float relX = (mouse.X - bounds.X) / bounds.Width;
            float relY = (mouse.Y - bounds.Y) / bounds.Height;

            const float edgeSize = 0.2f;

            if (relX < edgeSize) _context.DockDropZone = DockDropZone.Left;
            else if (relX > 1f - edgeSize) _context.DockDropZone = DockDropZone.Right;
            else if (relY < edgeSize) _context.DockDropZone = DockDropZone.Top;
            else if (relY > 1f - edgeSize) _context.DockDropZone = DockDropZone.Bottom;
            else _context.DockDropZone = DockDropZone.Center;

            break; // Found the target leaf
        }
    }

    private static void ExecuteDockOperation(DockSpaceState space)
    {
        var targetNode = space.RootNode.FindNode(_context.PendingDockTargetNodeId);
        if (targetNode == null || !targetNode.IsLeaf) return;

        var windowId = _context.PendingDockWindowId;
        var zone = _context.PendingDockDropZone;

        // Remove window from its current source leaf (if already docked somewhere)
        if (space.WindowToNode.TryGetValue(windowId, out var sourceLeaf) && sourceLeaf != targetNode)
        {
            var idx = sourceLeaf.DockedWindowIds.IndexOf(windowId);
            if (idx >= 0)
            {
                sourceLeaf.DockedWindowIds.RemoveAt(idx);
                if (sourceLeaf.ActiveTabIndex >= sourceLeaf.DockedWindowIds.Count)
                    sourceLeaf.ActiveTabIndex = Math.Max(0, sourceLeaf.DockedWindowIds.Count - 1);
            }

            if (sourceLeaf.IsEmpty)
            {
                PruneEmptyLeaf(sourceLeaf, space);
                // Re-find target node since tree structure may have changed
                targetNode = space.RootNode.FindNode(_context.PendingDockTargetNodeId);
                if (targetNode == null || !targetNode.IsLeaf) return;
            }
        }

        if (zone == DockDropZone.Center)
        {
            // Add as a new tab in the target leaf
            if (!targetNode.DockedWindowIds.Contains(windowId))
            {
                targetNode.DockedWindowIds.Add(windowId);
                targetNode.ActiveTabIndex = targetNode.DockedWindowIds.Count - 1;
            }
        }
        else
        {
            // Split the target node
            var direction = (zone == DockDropZone.Left || zone == DockDropZone.Right)
                ? DockSplitDirection.Horizontal
                : DockSplitDirection.Vertical;

            bool newNodeIsFirst = (zone == DockDropZone.Left || zone == DockDropZone.Top);

            var idA = space.GenerateNodeId();
            var idB = space.GenerateNodeId();

            var existingChild = new DockNode
            {
                Id = newNodeIsFirst ? idB : idA,
                DockedWindowIds = targetNode.DockedWindowIds,
                ActiveTabIndex = targetNode.ActiveTabIndex
            };
            var newChild = new DockNode
            {
                Id = newNodeIsFirst ? idA : idB,
                DockedWindowIds = new List<uint> { windowId },
                ActiveTabIndex = 0
            };

            targetNode.SplitDirection = direction;
            targetNode.ChildA = newNodeIsFirst ? newChild : existingChild;
            targetNode.ChildB = newNodeIsFirst ? existingChild : newChild;
            targetNode.SplitRatio = 0.5f;
            targetNode.DockedWindowIds = new List<uint>();
            targetNode.ActiveTabIndex = 0;
        }

        // Reset drop state — window will render as docked next frame
        _context.DockDropTargetNodeId = 0;
        _context.DockDropZone = DockDropZone.None;
    }

    private static void RenderDockDropPreview()
    {
        if (_context.DockDropZone == DockDropZone.None || _context.DockDropTargetNodeId == 0)
            return;

        // Find the target leaf's bounds
        BoundingBox targetBounds = default;
        bool found = false;
        foreach (var (nodeId, bounds, _) in _context.DockLeafBounds)
        {
            if (nodeId == _context.DockDropTargetNodeId)
            {
                targetBounds = bounds;
                found = true;
                break;
            }
        }
        if (!found) return;

        // Compute preview rectangle based on drop zone
        float previewX = targetBounds.X;
        float previewY = targetBounds.Y;
        float previewW = targetBounds.Width;
        float previewH = targetBounds.Height;

        switch (_context.DockDropZone)
        {
            case DockDropZone.Left:
                previewW *= 0.5f;
                break;
            case DockDropZone.Right:
                previewX += previewW * 0.5f;
                previewW *= 0.5f;
                break;
            case DockDropZone.Top:
                previewH *= 0.5f;
                break;
            case DockDropZone.Bottom:
                previewY += previewH * 0.5f;
                previewH *= 0.5f;
                break;
            case DockDropZone.Center:
                // Full area highlight
                break;
        }

        var ds = Style.DockSpace;
        using (Clay.Element(new ElementDeclaration
        {
            Id = StableId("DockDropPreview"),
            Layout = new LayoutConfig
            {
                Sizing = Sizing.FixedSize(previewW, previewH)
            },
            Floating = new FloatingConfig
            {
                AttachTo = FloatingAttachTo.Root,
                Offset = new Vector2(previewX, previewY),
                ZIndex = 2000,
                PointerCaptureMode = PointerCaptureMode.Passthrough
            },
            BackgroundColor = ds.DropPreviewColor
        })) { }
    }

    /// <summary>
    /// Automatically docks a window into the dock space.
    /// Each window gets its own leaf by splitting the largest existing leaf,
    /// alternating horizontal/vertical splits for a balanced tiled layout.
    /// </summary>
    private static void AutoDockWindow(DockSpaceState space, uint windowId, string title)
    {
        _context.DockedWindowTitles[windowId] = title;

        if (space.RootNode == null)
            space.RootNode = new DockNode { Id = space.GenerateNodeId() };

        // If root is an empty leaf, just dock here
        if (space.RootNode.IsEmpty)
        {
            space.RootNode.DockedWindowIds.Add(windowId);
            space.WindowToNode[windowId] = space.RootNode;
            return;
        }

        // Find the leaf with the fewest windows to split
        var target = FindLargestLeaf(space.RootNode);
        if (target == null) return;

        // Pick split direction: alternate based on tree depth at the target
        int depth = GetNodeDepth(space.RootNode, target.Id);
        var dir = (depth % 2 == 0) ? DockSplitDirection.Horizontal : DockSplitDirection.Vertical;

        // Split the target: existing windows stay in childA, new window in childB
        var idA = space.GenerateNodeId();
        var idB = space.GenerateNodeId();

        target.SplitDirection = dir;
        target.ChildA = new DockNode
        {
            Id = idA,
            DockedWindowIds = target.DockedWindowIds,
            ActiveTabIndex = target.ActiveTabIndex
        };
        target.ChildB = new DockNode
        {
            Id = idB,
            DockedWindowIds = new List<uint> { windowId },
            ActiveTabIndex = 0
        };
        target.SplitRatio = 0.5f;
        target.DockedWindowIds = new List<uint>();
        target.ActiveTabIndex = 0;

        space.WindowToNode.Clear();
        space.RootNode.RebuildWindowToNodeMap(space.WindowToNode);
    }

    private static DockNode? FindLargestLeaf(DockNode node)
    {
        if (node.IsLeaf) return node;
        var left = node.ChildA != null ? FindLargestLeaf(node.ChildA) : null;
        var right = node.ChildB != null ? FindLargestLeaf(node.ChildB) : null;
        if (left == null) return right;
        if (right == null) return left;
        // Prefer the leaf with more windows (it's "larger" / more splittable)
        return left.DockedWindowIds.Count >= right.DockedWindowIds.Count ? left : right;
    }

    private static int GetNodeDepth(DockNode root, uint targetId, int depth = 0)
    {
        if (root.Id == targetId) return depth;
        if (root.IsLeaf) return -1;
        int d = root.ChildA != null ? GetNodeDepth(root.ChildA, targetId, depth + 1) : -1;
        if (d >= 0) return d;
        return root.ChildB != null ? GetNodeDepth(root.ChildB, targetId, depth + 1) : -1;
    }

    private static void UndockWindow(uint windowId, DockNode leaf, DockSpaceState space)
    {
        // Remove from leaf
        int idx = leaf.DockedWindowIds.IndexOf(windowId);
        if (idx >= 0)
        {
            leaf.DockedWindowIds.RemoveAt(idx);
            if (leaf.ActiveTabIndex >= leaf.DockedWindowIds.Count)
                leaf.ActiveTabIndex = Math.Max(0, leaf.DockedWindowIds.Count - 1);
        }

        // Create/restore floating window state at mouse position
        if (!_context.WindowStates.ContainsKey(windowId))
        {
            _context.WindowStates[windowId] = new ClayUIContext.WindowState(
                Position: _context.MousePosition - new Vector2(100, 15),
                Size: new Vector2(300, 200),
                Collapsed: false,
                Open: true,
                Topmost: false
            );
        }
        else
        {
            var state = _context.WindowStates[windowId];
            _context.WindowStates[windowId] = state with
            {
                Position = _context.MousePosition - new Vector2(100, 15),
                Open = true
            };
        }

        // Prune empty leaf if no more windows
        if (leaf.IsEmpty)
        {
            PruneEmptyLeaf(leaf, space);
        }
    }

    private static void PruneEmptyLeaf(DockNode emptyLeaf, DockSpaceState space)
    {
        // Find parent of this empty leaf
        var parent = space.RootNode.FindParent(emptyLeaf.Id);
        if (parent == null) return; // Root leaf, leave it

        // Replace parent with the surviving sibling
        var survivor = parent.ChildA?.Id == emptyLeaf.Id ? parent.ChildB! : parent.ChildA!;

        // Copy survivor's properties into parent (replaces the split with the survivor)
        parent.SplitDirection = survivor.SplitDirection;
        parent.ChildA = survivor.ChildA;
        parent.ChildB = survivor.ChildB;
        parent.SplitRatio = survivor.SplitRatio;
        parent.DockedWindowIds = survivor.DockedWindowIds;
        parent.ActiveTabIndex = survivor.ActiveTabIndex;
    }

    private static void EmitDockSplitter(DockNode parentNode, DockSpaceStyle s)
    {
        bool isHorizontal = parentNode.SplitDirection == DockSplitDirection.Horizontal;
        var splitterId = ElementId.HashComposite("DockSplitter_", parentNode.Id);

        bool isHovered = !IsDisabled && Clay.PointerOver(splitterId);
        bool justPressed = isHovered && IsMouseJustPressed;

        if (justPressed)
        {
            _context.ActiveDockSplitterId = splitterId.Id;
            _context.DockSplitterDragStartMouse = isHorizontal
                ? _context.MousePosition.X : _context.MousePosition.Y;
            _context.DockSplitterDragStartRatio = parentNode.SplitRatio;
            _context.ActiveDockSplitterNode = parentNode;
        }

        bool isActive = _context.ActiveDockSplitterId == splitterId.Id;

        // Handle drag
        if (isActive && _context.MousePressed && _context.ActiveDockSplitterNode == parentNode)
        {
            var parentElementId = ElementId.HashComposite("DockSplit_", parentNode.Id);
            var parentData = Clay.GetElementData(parentElementId);
            if (parentData.Found)
            {
                float totalSize = isHorizontal ? parentData.BoundingBox.Width : parentData.BoundingBox.Height;
                float mousePos = isHorizontal ? _context.MousePosition.X : _context.MousePosition.Y;
                float delta = mousePos - _context.DockSplitterDragStartMouse;
                float ratioDelta = totalSize > 0 ? delta / totalSize : 0;

                parentNode.SplitRatio = Math.Clamp(
                    _context.DockSplitterDragStartRatio + ratioDelta,
                    0.1f, 0.9f);
            }
        }

        Color bgColor = isActive ? s.SplitterDragColor
            : isHovered ? s.SplitterHoverColor
            : s.SplitterColor;

        using (Clay.Element(new ElementDeclaration
        {
            Id = splitterId,
            Layout = new LayoutConfig
            {
                Sizing = isHorizontal
                    ? new Sizing(SizingAxis.Fixed(s.SplitterThickness), SizingAxis.Grow())
                    : new Sizing(SizingAxis.Grow(), SizingAxis.Fixed(s.SplitterThickness))
            },
            BackgroundColor = bgColor
        })) { }
    }

    // ============ Tooltip ============

    /// <summary>
    /// Shows a simple text tooltip when the previously rendered widget is hovered.
    /// Call immediately after the widget you want to attach the tooltip to.
    /// The tooltip appears after a short delay (0.5s) near the mouse cursor.
    /// </summary>
    /// <param name="text">Tooltip text to display.</param>
    /// <param name="style">Optional tooltip style.</param>
    public static void Tooltip(string text, TooltipStyle? style = null)
    {
        if (_context.TooltipShownThisFrame) return;
        var targetId = _context.LastWidgetId;
        if (targetId.Id == 0) return;

        bool isHovered = IsHovered(targetId);

        // Track hover time
        if (isHovered)
        {
            if (_context.TooltipHoveredId == targetId.Id)
            {
                _context.TooltipHoverTime += _context.DeltaTime;
            }
            else
            {
                _context.TooltipHoveredId = targetId.Id;
                _context.TooltipHoverTime = 0;
            }
        }
        else if (_context.TooltipHoveredId == targetId.Id)
        {
            _context.TooltipHoveredId = 0;
            _context.TooltipHoverTime = 0;
        }

        if (!isHovered || _context.TooltipHoverTime < ClayUIContext.TooltipDelay) return;

        var s = style.HasValue ? style.Value.MergeOver(Style.Tooltip) : Style.Tooltip;

        // Position near mouse cursor with a small offset
        var pos = _context.MousePosition + new Vector2(12, 16);

        // Clamp to screen
        var layoutDims = Clay.GetLayoutDimensions();
        var tooltipId = StableId($"Tooltip_{targetId.Id}");
        var prevData = Clay.GetElementData(tooltipId);
        float tipWidth = prevData.Found ? prevData.BoundingBox.Width : 100;
        float tipHeight = prevData.Found ? prevData.BoundingBox.Height : 20;

        if (pos.X + tipWidth > layoutDims.Width)
            pos.X = layoutDims.Width - tipWidth;
        if (pos.Y + tipHeight > layoutDims.Height)
            pos.Y = _context.MousePosition.Y - tipHeight - 4;
        if (pos.X < 0) pos.X = 0;
        if (pos.Y < 0) pos.Y = 0;

        using (Clay.Element(new ElementDeclaration
        {
            Id = tooltipId,
            Layout = new LayoutConfig
            {
                Padding = s.Padding,
                Sizing = new Sizing
                {
                    Width = SizingAxis.Fit(0, s.MaxWidth),
                    Height = SizingAxis.Fit()
                }
            },
            BackgroundColor = s.BackgroundColor,
            CornerRadius = s.CornerRadius,
            Border = s.Border,
            Floating = new FloatingConfig
            {
                AttachTo = FloatingAttachTo.Root,
                Offset = pos,
                ZIndex = 3000 // Above everything
            }
        }))
        {
            Clay.Text(text, new TextConfig
            {
                FontId = s.FontId,
                FontSize = s.FontSize,
                TextColor = s.TextColor,
                WrapMode = TextWrapMode.Words
            });
        }

        _context.TooltipShownThisFrame = true;
    }

    /// <summary>
    /// Begins a rich tooltip when the previously rendered widget is hovered.
    /// Call immediately after the widget you want to attach the tooltip to.
    /// Returns true if the tooltip is visible and content should be rendered.
    /// Call EndTooltip() when done (only if this returns true).
    /// </summary>
    /// <param name="style">Optional tooltip style.</param>
    /// <returns>True if tooltip should be rendered.</returns>
    public static bool BeginTooltip(TooltipStyle? style = null)
    {
        if (_context.TooltipShownThisFrame) return false;
        var targetId = _context.LastWidgetId;
        if (targetId.Id == 0) return false;

        bool isHovered = IsHovered(targetId);

        // Track hover time
        if (isHovered)
        {
            if (_context.TooltipHoveredId == targetId.Id)
            {
                _context.TooltipHoverTime += _context.DeltaTime;
            }
            else
            {
                _context.TooltipHoveredId = targetId.Id;
                _context.TooltipHoverTime = 0;
            }
        }
        else if (_context.TooltipHoveredId == targetId.Id)
        {
            _context.TooltipHoveredId = 0;
            _context.TooltipHoverTime = 0;
        }

        if (!isHovered || _context.TooltipHoverTime < ClayUIContext.TooltipDelay) return false;

        var s = style.HasValue ? style.Value.MergeOver(Style.Tooltip) : Style.Tooltip;

        var pos = _context.MousePosition + new Vector2(12, 16);

        var layoutDims = Clay.GetLayoutDimensions();
        var tooltipId = StableId($"Tooltip_{targetId.Id}");
        var prevData = Clay.GetElementData(tooltipId);
        float tipWidth = prevData.Found ? prevData.BoundingBox.Width : 100;
        float tipHeight = prevData.Found ? prevData.BoundingBox.Height : 20;

        if (pos.X + tipWidth > layoutDims.Width)
            pos.X = layoutDims.Width - tipWidth;
        if (pos.Y + tipHeight > layoutDims.Height)
            pos.Y = _context.MousePosition.Y - tipHeight - 4;
        if (pos.X < 0) pos.X = 0;
        if (pos.Y < 0) pos.Y = 0;

        Clay.Element(new ElementDeclaration
        {
            Id = tooltipId,
            Layout = new LayoutConfig
            {
                Direction = LayoutDirection.TopToBottom,
                Padding = s.Padding,
                ChildGap = 4,
                Sizing = new Sizing
                {
                    Width = SizingAxis.Fit(0, s.MaxWidth),
                    Height = SizingAxis.Fit()
                }
            },
            BackgroundColor = s.BackgroundColor,
            CornerRadius = s.CornerRadius,
            Border = s.Border,
            Floating = new FloatingConfig
            {
                AttachTo = FloatingAttachTo.Root,
                Offset = pos,
                ZIndex = 3000
            }
        });

        _context.TooltipShownThisFrame = true;
        return true;
    }

    /// <summary>
    /// Ends a rich tooltip. Must be called if BeginTooltip returned true.
    /// </summary>
    public static void EndTooltip()
    {
        Clay.CloseElement();
    }

    // ============ Popup Widget ============

    /// <summary>
    /// Opens a popup at the current mouse position.
    /// The popup will appear on the next BeginPopup call with the same id.
    /// </summary>
    /// <param name="id">Unique popup identifier.</param>
    public static void OpenPopup(string id)
    {
        var popupId = StableId($"Popup_{id}");
        _context.PopupToOpen = popupId.Id;
        _context.PopupOpenPosition = _context.MousePosition;
        _context.PopupOpenParentId = 0;
    }

    /// <summary>
    /// Opens a popup at a specific position.
    /// </summary>
    /// <param name="id">Unique popup identifier.</param>
    /// <param name="position">Position where the popup should appear.</param>
    public static void OpenPopupAt(string id, Vector2 position)
    {
        var popupId = StableId($"Popup_{id}");
        _context.PopupToOpen = popupId.Id;
        _context.PopupOpenPosition = position;
        _context.PopupOpenParentId = 0;
    }

    /// <summary>
    /// Closes a popup.
    /// </summary>
    /// <param name="id">Unique popup identifier.</param>
    public static void ClosePopup(string id)
    {
        var popupId = StableId($"Popup_{id}");
        if (_context.PopupStates.TryGetValue(popupId.Id, out var state))
        {
            _context.PopupStates[popupId.Id] = state with { Open = false };
            _context.OpenPopupStack.Remove(popupId.Id);
            _context.ModalPopups.Remove(popupId.Id);
        }
    }

    /// <summary>
    /// Closes all open popups.
    /// </summary>
    public static void CloseAllPopups()
    {
        for (int i = _context.OpenPopupStack.Count - 1; i >= 0; i--)
        {
            var popupId = _context.OpenPopupStack[i];
            if (_context.PopupStates.TryGetValue(popupId, out var state))
            {
                _context.PopupStates[popupId] = state with { Open = false };
            }
        }
        _context.OpenPopupStack.Clear();
        _context.ModalPopups.Clear();
    }

    /// <summary>
    /// Returns true if the specified popup is open.
    /// </summary>
    public static bool IsPopupOpen(string id)
    {
        var popupId = StableId($"Popup_{id}");
        return _context.PopupStates.TryGetValue(popupId.Id, out var state) && state.Open;
    }

    /// <summary>
    /// Begins a popup. Returns true if the popup is open and content should be rendered.
    /// Call EndPopup() when done (only if this returns true).
    /// </summary>
    /// <param name="id">Unique popup identifier (same as used in OpenPopup).</param>
    /// <param name="style">Optional popup style.</param>
    /// <returns>True if popup is open and content should be rendered.</returns>
    public static bool BeginPopup(string id, PopupStyle? style = null)
    {
        var popupId = StableId($"Popup_{id}");
        var s = style.HasValue ? style.Value.MergeOver(Style.Popup) : Style.Popup;

        bool justOpened = false;

        // Check if this popup was requested to open this frame
        if (_context.PopupToOpen == popupId.Id)
        {
            // Open the popup
            var newState = new ClayUIContext.PopupState(
                Position: _context.PopupOpenPosition + s.Offset,
                ParentId: _context.PopupOpenParentId,
                Open: true
            );
            _context.PopupStates[popupId.Id] = newState;

            // Add to open stack if not already there
            if (!_context.OpenPopupStack.Contains(popupId.Id))
            {
                _context.OpenPopupStack.Add(popupId.Id);
            }

            _context.PopupToOpen = 0; // Clear the open request
            justOpened = true; // Don't close on the same frame we opened
        }

        // Check if popup is open
        if (!_context.PopupStates.TryGetValue(popupId.Id, out var state) || !state.Open)
        {
            return false;
        }

        // Handle click-outside-to-close (but not for modal popups or on the frame just opened).
        bool isModal = _context.ModalPopups.Contains(popupId.Id);
        if (!isModal && !justOpened && IsMouseJustPressed && !_context.IsPointOverAnyPopup(_context.MousePosition))
        {
            _context.ClosePopupsOutsidePoint(_context.MousePosition);

            // Re-check if this popup is still open
            if (!_context.PopupStates.TryGetValue(popupId.Id, out state) || !state.Open)
            {
                return false;
            }
        }

        // Get z-index for this popup
        short zIndex = _context.GetPopupZIndex(popupId.Id);

        // Clamp popup position to stay within layout bounds
        var layoutDims = Clay.GetLayoutDimensions();
        var popupPos = state.Position;

        // Use previous frame's bounding box for accurate size, fall back to MinWidth estimate
        var prevData = Clay.GetElementData(popupId);
        float popupWidth = prevData.Found ? prevData.BoundingBox.Width : s.MinWidth;
        float popupHeight = prevData.Found ? prevData.BoundingBox.Height : 50;

        if (popupPos.X + popupWidth > layoutDims.Width)
            popupPos.X = layoutDims.Width - popupWidth;
        if (popupPos.Y + popupHeight > layoutDims.Height)
            popupPos.Y = layoutDims.Height - popupHeight;
        if (popupPos.X < 0) popupPos.X = 0;
        if (popupPos.Y < 0) popupPos.Y = 0;

        // Render popup container
        Clay.Element(new ElementDeclaration
        {
            Id = popupId,
            Layout = new LayoutConfig
            {
                Direction = LayoutDirection.TopToBottom,
                Padding = s.Padding,
                ChildGap = s.ContentGap,
                Sizing = new Sizing
                {
                    Width = SizingAxis.Fit(s.MinWidth, s.MaxWidth),
                    Height = SizingAxis.Fit()
                }
            },
            BackgroundColor = s.BackgroundColor,
            CornerRadius = s.CornerRadius,
            Border = s.Border,
            Floating = new FloatingConfig
            {
                AttachTo = FloatingAttachTo.Root,
                Offset = popupPos,
                ZIndex = zIndex
            }
        });

        // Update popup bounds for click-outside detection (may already exist from frame-start rebuild)
        var estimatedBounds = new BoundingBox(popupPos.X, popupPos.Y, popupWidth, popupHeight);
        bool boundsUpdated = false;
        for (int i = 0; i < _context.OpenPopupBounds.Count; i++)
        {
            if (_context.OpenPopupBounds[i].PopupId == popupId.Id)
            {
                _context.OpenPopupBounds[i] = (popupId.Id, estimatedBounds);
                boundsUpdated = true;
                break;
            }
        }
        if (!boundsUpdated)
            _context.OpenPopupBounds.Add((popupId.Id, estimatedBounds));

        _context.PopupDepth++;
        return true;
    }

    /// <summary>
    /// Ends a popup. Must be called if BeginPopup returned true.
    /// </summary>
    public static void EndPopup()
    {
        if (_context.PopupDepth > 0)
            _context.PopupDepth--;
        Clay.CloseElement();
    }

    // ============ Menu Bar ============

    /// <summary>
    /// Begins a menu bar region. Between BeginMenuBar/EndMenuBar, BeginMenu calls render
    /// as top-bar buttons that support hover-to-switch when any sibling menu is already open.
    /// </summary>
    /// <param name="style">Optional button style for menu bar items.</param>
    public static void BeginMenuBar(ButtonStyle? style = null)
    {
        _context.InsideMenuBar = true;
        _context.MenuBarStyle = style;

        // Swap current → previous, then clear current for this frame's registration
        _context.PrevMenuBarPopupIds.Clear();
        foreach (var id in _context.MenuBarPopupIds)
            _context.PrevMenuBarPopupIds.Add(id);
        _context.MenuBarPopupIds.Clear();

        // Detect which menu bar popup (if any) is still open from last frame
        _context.ActiveMenuBarPopupId = null;
        foreach (var id in _context.PrevMenuBarPopupIds)
        {
            if (IsPopupOpen(id))
            {
                _context.ActiveMenuBarPopupId = id;
                break;
            }
        }
    }

    /// <summary>
    /// Ends a menu bar region.
    /// </summary>
    public static void EndMenuBar()
    {
        _context.InsideMenuBar = false;
    }

    /// <summary>
    /// Helper to show a context menu popup on right-click.
    /// Returns true if the context menu should be opened.
    /// </summary>
    /// <param name="id">Unique context menu identifier.</param>
    /// <param name="triggerId">Element ID that triggers the context menu.</param>
    /// <returns>True if context menu was just opened.</returns>
    public static bool BeginContextMenu(string id, ElementId triggerId)
    {
        // Check for right-click on trigger element
        bool rightClicked = IsHovered(triggerId) && _context.MousePressed && !_context.MouseWasPressed;

        // For now, treat any click as potential context menu trigger
        // In a real implementation, you'd check for right-click specifically
        if (rightClicked)
        {
            OpenPopup(id);
        }

        return BeginPopup(id);
    }

    /// <summary>
    /// Ends a context menu. Must be called if BeginContextMenu was called (regardless of return value).
    /// </summary>
    public static void EndContextMenu()
    {
        // Only close element if popup was actually open
        // BeginPopup returns false when closed, so we don't need to close anything
    }

    /// <summary>
    /// Renders a menu item inside a popup. Returns true if clicked.
    /// Automatically closes the popup when clicked.
    /// </summary>
    /// <param name="label">Menu item label.</param>
    /// <param name="enabled">Whether the item is enabled.</param>
    /// <returns>True if the item was clicked.</returns>
    public static bool MenuItem(string label, bool enabled = true)
    {
        var id = StableId($"MenuItem_{label}");
        bool isHovered = IsHovered(id) && enabled;
        bool clicked = isHovered && ShouldProcessClick;

        // Participate in the hovered-menu-item system so sibling submenus close
        if (isHovered && _context.HoveredMenuItemId != id.Id)
        {
            _context.HoveredMenuItemId = id.Id;
            _context.HoveredMenuItemTime = 0;
        }

        var s = Style.Popup;
        var textColor = enabled ? Color.White : Color.Rgba(120, 120, 125);
        var bgColor = isHovered ? Color.Rgba(60, 60, 70) : Color.Transparent;

        using (Clay.Element(new ElementDeclaration
        {
            Id = id,
            Layout = new LayoutConfig
            {
                Sizing = new Sizing { Width = SizingAxis.Grow(), Height = SizingAxis.Fit() },
                Padding = Padding.Symmetric(8, 4)
            },
            BackgroundColor = bgColor,
            CornerRadius = CornerRadius.All(2)
        }))
        {
            Clay.Text(ElementId.GetDisplayLabel(label), new TextConfig
            {
                FontId = s.FontId,
                FontSize = s.FontSize,
                TextColor = textColor
            });
        }

        if (clicked)
        {
            // Close all popups when an item is clicked
            CloseAllPopups();
        }

        return clicked;
    }

    /// <summary>
    /// Renders a separator line in a popup menu.
    /// </summary>
    public static void MenuSeparator()
    {
        using (Clay.Element(new ElementDeclaration
        {
            Layout = new LayoutConfig
            {
                Sizing = new Sizing { Width = SizingAxis.Grow(), Height = SizingAxis.Fixed(1) },
                Padding = Padding.Symmetric(0, 4)
            }
        }))
        {
            Clay.Element(new ElementDeclaration
            {
                Layout = new LayoutConfig
                {
                    Sizing = new Sizing { Width = SizingAxis.Grow(), Height = SizingAxis.Fixed(1) }
                },
                BackgroundColor = Style.SeparatorColor
            });
            Clay.CloseElement();
        }
    }

    /// <summary>
    /// Begins a menu. Behavior depends on context:
    /// <list type="bullet">
    /// <item>Inside BeginMenuBar: renders a top-bar button that opens a dropdown popup below.
    /// Click to toggle; hover to switch when any sibling menu is already open.</item>
    /// <item>Inside a popup: renders a submenu row with ">" arrow that opens a child popup
    /// to the right after a short hover delay.</item>
    /// </list>
    /// Returns true if the menu is open and content should be rendered.
    /// Call EndMenu() when done (only if this returns true).
    /// </summary>
    /// <param name="label">Menu label.</param>
    /// <param name="enabled">Whether the menu can be opened.</param>
    /// <returns>True if the menu is open.</returns>
    public static bool BeginMenu(string label, bool enabled = true)
    {
        if (_context.InsideMenuBar)
            return BeginMenuBarMenu(label, enabled);
        return BeginSubmenu(label, enabled);
    }

    private static bool BeginMenuBarMenu(string label, bool enabled)
    {
        string menuKey = $"SubMenu_{label}";
        _context.MenuBarPopupIds.Add(menuKey);

        var s = _context.MenuBarStyle.HasValue
            ? _context.MenuBarStyle.Value.MergeOver(Style.Button)
            : Style.Button;
        var itemId = StableId(menuKey);
        bool isHovered = IsHovered(itemId) && enabled;
        bool isOpen = IsPopupOpen(menuKey);

        // Check if any menu bar popup is currently open
        bool anyMenuBarPopupOpen = _context.ActiveMenuBarPopupId != null
            && IsPopupOpen(_context.ActiveMenuBarPopupId);

        bool clicked = isHovered && ShouldProcessClick;

        // Hover-to-switch: if another menu bar popup is open and we hover this item, switch
        if (isHovered && anyMenuBarPopupOpen && !isOpen && enabled)
        {
            if (_context.ActiveMenuBarPopupId != null)
                ClosePopup(_context.ActiveMenuBarPopupId);

            var itemData = Clay.GetElementData(itemId);
            if (itemData.Found)
                OpenPopupBelow(menuKey, itemData.BoundingBox);
            else
                OpenPopup(menuKey);
            _context.ActiveMenuBarPopupId = menuKey;
        }
        else if (clicked && enabled)
        {
            // Click-to-toggle
            if (isOpen)
            {
                ClosePopup(menuKey);
                _context.ActiveMenuBarPopupId = null;
            }
            else
            {
                var itemData = Clay.GetElementData(itemId);
                if (itemData.Found)
                    OpenPopupBelow(menuKey, itemData.BoundingBox);
                else
                    OpenPopup(menuKey);
                _context.ActiveMenuBarPopupId = menuKey;
            }
        }

        // Render as a button-style element
        bool isPressed = isHovered && _context.MousePressed;
        var textColor = enabled ? s.TextColor : Color.Rgba(120, 120, 125);
        var bgColor = (isOpen || isPressed) ? s.PressedColor
            : isHovered ? s.HoverColor
            : s.BackgroundColor;

        using (Clay.Element(new ElementDeclaration
        {
            Id = itemId,
            Layout = new LayoutConfig
            {
                Padding = s.Padding,
                ChildAlignment = ChildAlignment.Center
            },
            BackgroundColor = bgColor,
            CornerRadius = s.CornerRadius
        }))
        {
            Clay.Text(ElementId.GetDisplayLabel(label), new TextConfig
            {
                FontId = s.FontId,
                FontSize = s.FontSize,
                TextColor = textColor
            });
        }

        // Exit menu bar mode so nested BeginMenu calls inside the popup act as submenus
        bool result = BeginPopup(menuKey);
        if (result)
        {
            _context.InsideMenuBar = false;
            _context.MenuBarMenuDepth++;
        }
        return result;
    }

    private static bool BeginSubmenu(string label, bool enabled)
    {
        string menuKey = $"SubMenu_{label}";
        var itemId = StableId(menuKey);
        bool isHovered = IsHovered(itemId) && enabled;
        bool isOpen = IsPopupOpen(menuKey);

        // Track hover time for this menu item
        if (isHovered)
        {
            if (_context.HoveredMenuItemId == itemId.Id)
            {
                _context.HoveredMenuItemTime += _context.DeltaTime;
            }
            else
            {
                _context.HoveredMenuItemId = itemId.Id;
                _context.HoveredMenuItemTime = 0;
            }
        }
        else if (_context.HoveredMenuItemId == itemId.Id)
        {
            if (!isOpen)
            {
                _context.HoveredMenuItemId = 0;
                _context.HoveredMenuItemTime = 0;
            }
        }

        // Open submenu after hover delay
        if (isHovered && !isOpen && enabled && _context.HoveredMenuItemTime >= ClayUIContext.SubmenuOpenDelay)
        {
            var itemData = Clay.GetElementData(itemId);
            if (itemData.Found)
            {
                var bounds = itemData.BoundingBox;
                OpenPopupAt(menuKey, new Vector2(bounds.X + bounds.Width, bounds.Y));
            }
            else
            {
                OpenPopup(menuKey);
            }
        }

        // Close submenu if mouse moved to a different sibling menu item
        if (isOpen && !isHovered && _context.HoveredMenuItemId != itemId.Id && _context.HoveredMenuItemId != 0)
        {
            // Check if mouse is over this submenu popup or any of its descendant popups.
            var subPopupId = StableId($"Popup_{menuKey}");
            int stackIdx = _context.OpenPopupStack.IndexOf(subPopupId.Id);
            bool overSubmenuOrDescendant = false;

            if (stackIdx >= 0)
            {
                for (int si = stackIdx; si < _context.OpenPopupStack.Count; si++)
                {
                    uint checkId = _context.OpenPopupStack[si];
                    foreach (var (id, bounds) in _context.OpenPopupBounds)
                    {
                        if (id == checkId && bounds.Contains(_context.MousePosition))
                        {
                            overSubmenuOrDescendant = true;
                            break;
                        }
                    }
                    if (overSubmenuOrDescendant) break;
                }
            }

            if (!overSubmenuOrDescendant)
            {
                ClosePopup(menuKey);
            }
        }

        // Render the menu item row with ">" arrow
        var s = Style.Popup;
        var textColor = enabled ? Color.White : Color.Rgba(120, 120, 125);
        var bgColor = (isHovered || isOpen) ? Color.Rgba(60, 60, 70) : Color.Transparent;

        using (Clay.Element(new ElementDeclaration
        {
            Id = itemId,
            Layout = new LayoutConfig
            {
                Sizing = new Sizing { Width = SizingAxis.Grow(), Height = SizingAxis.Fit() },
                Padding = Padding.Symmetric(8, 4),
                Direction = LayoutDirection.LeftToRight,
                ChildAlignment = new ChildAlignment { Y = AlignY.Center }
            },
            BackgroundColor = bgColor,
            CornerRadius = CornerRadius.All(2)
        }))
        {
            using (Clay.Element(new ElementDeclaration
            {
                Layout = new LayoutConfig
                {
                    Sizing = new Sizing { Width = SizingAxis.Grow(), Height = SizingAxis.Fit() }
                }
            }))
            {
                Clay.Text(ElementId.GetDisplayLabel(label), new TextConfig
                {
                    FontId = s.FontId,
                    FontSize = s.FontSize,
                    TextColor = textColor
                });
            }

            Clay.Text(">", new TextConfig
            {
                FontId = s.FontId,
                FontSize = s.FontSize,
                TextColor = textColor
            });
        }

        bool result = BeginPopup(menuKey, s with { Offset = default });
        if (result)
        {
            // Track depth so EndMenu doesn't prematurely restore InsideMenuBar
            _context.MenuBarMenuDepth++;
        }
        return result;
    }

    /// <summary>
    /// Ends a menu. Must be called if BeginMenu returned true.
    /// </summary>
    public static void EndMenu()
    {
        EndPopup();
        // If this was a top-level menu bar menu, restore menu bar mode
        if (_context.MenuBarMenuDepth > 0)
        {
            _context.MenuBarMenuDepth--;
            if (_context.MenuBarMenuDepth == 0)
                _context.InsideMenuBar = true;
        }
    }

    /// <summary>
    /// Opens a popup below a button or other element.
    /// Use with GetLastElementBounds() to position relative to an element.
    /// </summary>
    /// <param name="id">Unique popup identifier.</param>
    /// <param name="anchorBounds">Bounding box of the element to attach to.</param>
    /// <param name="alignRight">If true, align popup to right edge of anchor.</param>
    public static void OpenPopupBelow(string id, BoundingBox anchorBounds, bool alignRight = false)
    {
        float x = alignRight
            ? anchorBounds.X + anchorBounds.Width - Style.Popup.MinWidth
            : anchorBounds.X;
        float y = anchorBounds.Y + anchorBounds.Height;

        OpenPopupAt(id, new Vector2(x, y));
    }

    // ============ Modal Popup ============

    /// <summary>
    /// Begins a modal popup. Modal popups block all input behind them with a dimming overlay
    /// and cannot be closed by clicking outside — you must explicitly call ClosePopup or CloseAllPopups.
    /// Use OpenPopup to open a modal, same as regular popups.
    /// Returns true if the modal is open and content should be rendered.
    /// Call EndPopup() when done (only if this returns true).
    /// </summary>
    /// <param name="id">Unique popup identifier (same as used in OpenPopup).</param>
    /// <param name="style">Optional modal style.</param>
    /// <returns>True if modal is open and content should be rendered.</returns>
    public static bool BeginPopupModal(string id, ModalStyle? style = null)
    {
        var popupId = StableId($"Popup_{id}");
        var s = style.HasValue ? style.Value.MergeOver(Style.Modal) : Style.Modal;

        bool justOpened = false;

        // Check if this popup was requested to open this frame
        if (_context.PopupToOpen == popupId.Id)
        {
            // Center the modal on screen
            var layoutDims = Clay.GetLayoutDimensions();
            var prevData = Clay.GetElementData(popupId);
            float modalWidth = prevData.Found ? prevData.BoundingBox.Width : s.MinWidth;
            float modalHeight = prevData.Found ? prevData.BoundingBox.Height : 100;

            var centeredPos = new Vector2(
                (layoutDims.Width - modalWidth) / 2,
                (layoutDims.Height - modalHeight) / 2
            );

            var newState = new ClayUIContext.PopupState(
                Position: centeredPos,
                ParentId: _context.PopupOpenParentId,
                Open: true
            );
            _context.PopupStates[popupId.Id] = newState;

            if (!_context.OpenPopupStack.Contains(popupId.Id))
                _context.OpenPopupStack.Add(popupId.Id);

            // Mark as modal so click-outside doesn't close it
            _context.ModalPopups.Add(popupId.Id);

            _context.PopupToOpen = 0;
            justOpened = true;
        }

        // Check if popup is open
        if (!_context.PopupStates.TryGetValue(popupId.Id, out var state) || !state.Open)
            return false;

        // Re-center each frame using previous frame's actual size
        if (!justOpened)
        {
            var layoutDims = Clay.GetLayoutDimensions();
            var prevData = Clay.GetElementData(popupId);
            if (prevData.Found)
            {
                float w = prevData.BoundingBox.Width;
                float h = prevData.BoundingBox.Height;
                state = state with { Position = new Vector2((layoutDims.Width - w) / 2, (layoutDims.Height - h) / 2) };
                _context.PopupStates[popupId.Id] = state;
            }
        }

        short zIndex = _context.GetPopupZIndex(popupId.Id);

        // Render fullscreen dimming overlay (one z-level below the modal)
        Clay.Element(new ElementDeclaration
        {
            Layout = new LayoutConfig
            {
                Sizing = new Sizing
                {
                    Width = SizingAxis.Fixed(Clay.GetLayoutDimensions().Width),
                    Height = SizingAxis.Fixed(Clay.GetLayoutDimensions().Height)
                }
            },
            BackgroundColor = s.DimColor,
            Floating = new FloatingConfig
            {
                AttachTo = FloatingAttachTo.Root,
                Offset = default,
                ZIndex = (short)(zIndex - 1)
            }
        });
        Clay.CloseElement();

        // Render modal container
        Clay.Element(new ElementDeclaration
        {
            Id = popupId,
            Layout = new LayoutConfig
            {
                Direction = LayoutDirection.TopToBottom,
                Sizing = new Sizing
                {
                    Width = SizingAxis.Fit(s.MinWidth, s.MaxWidth),
                    Height = SizingAxis.Fit()
                }
            },
            BackgroundColor = s.BackgroundColor,
            CornerRadius = s.CornerRadius,
            Border = s.Border,
            Floating = new FloatingConfig
            {
                AttachTo = FloatingAttachTo.Root,
                Offset = state.Position,
                ZIndex = zIndex
            }
        });

        // Title bar
        var titleBarId = StableId($"ModalTitle_{id}");
        using (Clay.Element(new ElementDeclaration
        {
            Id = titleBarId,
            Layout = new LayoutConfig
            {
                Direction = LayoutDirection.LeftToRight,
                Sizing = new Sizing { Width = SizingAxis.Grow(), Height = SizingAxis.Fixed(s.TitleBarHeight) },
                Padding = Padding.Symmetric(s.Padding.Left, 0),
                ChildAlignment = new ChildAlignment { Y = AlignY.Center }
            },
            BackgroundColor = s.TitleBarColor
        }))
        {
            // Title text
            using (Clay.Element(new ElementDeclaration
            {
                Layout = new LayoutConfig
                {
                    Sizing = new Sizing { Width = SizingAxis.Grow(), Height = SizingAxis.Fit() }
                }
            }))
            {
                Clay.Text(id, new TextConfig
                {
                    FontId = s.FontId,
                    FontSize = s.TitleFontSize,
                    TextColor = s.TitleColor
                });
            }
        }

        // Content area with padding
        Clay.Element(new ElementDeclaration
        {
            Layout = new LayoutConfig
            {
                Direction = LayoutDirection.TopToBottom,
                Padding = s.Padding,
                ChildGap = s.ContentGap,
                Sizing = new Sizing { Width = SizingAxis.Grow(), Height = SizingAxis.Fit() }
            }
        });

        // Update popup bounds
        var layoutDims2 = Clay.GetLayoutDimensions();
        var prevData2 = Clay.GetElementData(popupId);
        float popupWidth = prevData2.Found ? prevData2.BoundingBox.Width : s.MinWidth;
        float popupHeight = prevData2.Found ? prevData2.BoundingBox.Height : 100;
        var estimatedBounds = new BoundingBox(state.Position.X, state.Position.Y, popupWidth, popupHeight);

        bool boundsUpdated = false;
        for (int i = 0; i < _context.OpenPopupBounds.Count; i++)
        {
            if (_context.OpenPopupBounds[i].PopupId == popupId.Id)
            {
                _context.OpenPopupBounds[i] = (popupId.Id, estimatedBounds);
                boundsUpdated = true;
                break;
            }
        }
        if (!boundsUpdated)
            _context.OpenPopupBounds.Add((popupId.Id, estimatedBounds));

        _context.PopupDepth++;
        return true;
    }

    /// <summary>
    /// Ends a modal popup. Must be called if BeginPopupModal returned true.
    /// Closes the content area and the modal container.
    /// </summary>
    public static void EndPopupModal()
    {
        if (_context.PopupDepth > 0)
            _context.PopupDepth--;
        Clay.CloseElement(); // content area
        Clay.CloseElement(); // modal container
    }

    /// <summary>
    /// Begins a collapsible tree node. Returns true if expanded.
    /// Call EndTreeNode() when done adding content (only if this returns true).
    /// </summary>
    public static bool BeginTreeNode(string label, TreeNodeStyle? style = null)
    {
        var s = style.HasValue ? style.Value.MergeOver(Style.TreeNode) : Style.TreeNode;
        var id = StableId($"TreeNode_{label}");
        bool isHovered = IsHovered(id);
        bool clicked = isHovered && ShouldProcessClick;

        // Get/set expanded state
        if (!_context.ExpandedStates.TryGetValue(id.Id, out bool expanded))
        {
            expanded = s.DefaultExpanded;
            _context.ExpandedStates[id.Id] = expanded;
        }

        if (clicked)
        {
            expanded = !expanded;
            _context.ExpandedStates[id.Id] = expanded;
        }

        // Header
        using (Clay.Element(new ElementDeclaration
        {
            Id = id,
            Layout = new LayoutConfig
            {
                Direction = LayoutDirection.LeftToRight,
                ChildGap = 6,
                ChildAlignment = ChildAlignment.CenterLeft,
                Padding = s.Padding,
                Sizing = new Sizing { Width = SizingAxis.Grow() }
            },
            BackgroundColor = isHovered ? s.HoverColor : Color.Transparent
        }))
        {
            // Arrow indicator
            Clay.Text(expanded ? s.ExpandedIcon : s.CollapsedIcon, new TextConfig
            {
                FontId = s.FontId,
                FontSize = s.FontSize,
                TextColor = s.ArrowColor
            });

            // Label
            Clay.Text(ElementId.GetDisplayLabel(label), new TextConfig
            {
                FontId = s.FontId,
                FontSize = s.FontSize,
                TextColor = s.TextColor
            });
        }

        if (expanded)
        {
            // Content container with indentation
            Clay.Element(new ElementDeclaration
            {
                Layout = new LayoutConfig
                {
                    Direction = LayoutDirection.TopToBottom,
                    Padding = new Padding { Left = s.IndentSize },
                    ChildGap = 4,
                    Sizing = new Sizing { Width = SizingAxis.Grow() }
                }
            });
            _context.TreeNodeDepth++;
            return true;
        }

        return false;
    }

    /// <summary>
    /// Ends a tree node started with BeginTreeNode.
    /// Only call this if BeginTreeNode returned true.
    /// </summary>
    public static void EndTreeNode()
    {
        if (_context.TreeNodeDepth > 0)
        {
            Clay.CloseElement();
            _context.TreeNodeDepth--;
        }
    }

    /// <summary>
    /// Renders an inline vertical scrollbar for a scroll container.
    /// Use this when manually creating scroll containers with the raw Clay API.
    /// Must be called as a sibling to the scroll container, inside a horizontal wrapper.
    /// </summary>
    /// <param name="scrollContainerId">The ElementId of the scroll container.</param>
    /// <param name="style">Optional scrollbar style.</param>
    public static void VerticalScrollbar(ElementId scrollContainerId, ScrollbarStyle? style = null, ScrollbarSkin? skin = null)
    {
        var s = style.HasValue ? style.Value.MergeOver(Style.Scrollbar) : Style.Scrollbar;
        var sk = skin ?? Skin?.Scrollbar ?? default;
        var scrollData = Clay.GetScrollContainerData(scrollContainerId);

        // Only render if content actually overflows
        if (!scrollData.Found || !scrollData.OverflowsY)
        {
            // Add empty spacer to maintain layout consistency
            using (Clay.Element(new ElementDeclaration
            {
                Layout = new LayoutConfig
                {
                    Sizing = new Sizing
                    {
                        Width = SizingAxis.Fixed(s.Width + s.TrackPadding * 2),
                        Height = SizingAxis.Grow()
                    }
                }
            })) { }
            return;
        }

        float containerHeight = scrollData.ScrollContainerDimensions.Height;
        float contentHeight = scrollData.ContentDimensions.Height;
        float scrollY = scrollData.ScrollPosition.Y;
        float maxScrollY = scrollData.MaxScrollY;

        var trackId = ElementId.HashComposite("SbTrackV_", scrollContainerId.Id);
        var thumbId = ElementId.HashComposite("SbThumbV_", scrollContainerId.Id);

        // Use the track's actual rendered height from previous frame when available,
        // so thumb positioning stays correct even when the track is shorter than the
        // scroll container (e.g. resize grip taking space in the right column).
        var trackData = Clay.GetElementData(trackId);
        float trackInnerHeight = trackData.Found
            ? trackData.BoundingBox.Height - s.TrackPadding * 2
            : containerHeight - s.TrackPadding * 2;

        // Calculate thumb size and position
        float thumbHeight = Math.Max(s.MinThumbSize, (containerHeight / contentHeight) * trackInnerHeight);
        float thumbTravel = trackInnerHeight - thumbHeight;
        float thumbY = maxScrollY > 0 ? (scrollY / maxScrollY) * thumbTravel : 0;

        // Use the hit area (or track) for hover detection, but check thumb Y range
        var hoverId = _context.ScrollbarHitAreaId.Id != 0 ? _context.ScrollbarHitAreaId : trackId;
        bool isHitAreaHovered = IsHovered(hoverId);
        bool isThumbHovered = false;
        bool isActiveScrollbar = _context.ActiveScrollbarId == thumbId.Id;

        if (isHitAreaHovered || isActiveScrollbar)
        {
            var thumbData = Clay.GetElementData(thumbId);
            if (thumbData.Found)
            {
                float mouseY = Clay.GetPointerState().Position.Y;
                isThumbHovered = mouseY >= thumbData.BoundingBox.Y && mouseY <= thumbData.BoundingBox.Y + thumbData.BoundingBox.Height;
            }
        }

        // Check if mouse is within the track's actual bounds (not just the wider hit area)
        bool isTrackHovered = trackData.Found && Clay.GetPointerState().Position.Y >= trackData.BoundingBox.Y
            && Clay.GetPointerState().Position.Y <= trackData.BoundingBox.Y + trackData.BoundingBox.Height;

        // Handle click — scrollbar takes priority over window resize when both
        // overlap on the right edge, so use IsMouseJustPressed directly and cancel
        // any resize that was started this frame.
        if ((isThumbHovered || (isHitAreaHovered && isTrackHovered)) && IsMouseJustPressed && !IsMouseOverAnyPopup)
        {
            if (isThumbHovered)
            {
                // Clicked on thumb — start dragging from current position
                _context.ActiveScrollbarId = thumbId.Id;
                _context.ActiveScrollContainerId = scrollContainerId;
                _context.IsVerticalScrollbar = true;

                var pointerData = Clay.GetPointerState();
                var thumbData = Clay.GetElementData(thumbId);
                if (thumbData.Found)
                {
                    _context.ScrollbarDragOffset = pointerData.Position.Y - thumbData.BoundingBox.Y;
                }
            }
            else if (thumbTravel > 0)
            {
                // Clicked on track — jump thumb center to click position, then start dragging
                float mouseY = Clay.GetPointerState().Position.Y;
                float clickInTrack = mouseY - trackData.BoundingBox.Y - s.TrackPadding - thumbHeight / 2;
                float normalizedY = Math.Clamp(clickInTrack / thumbTravel, 0f, 1f);
                Clay.SetScrollPosition(scrollContainerId, new Vector2(scrollData.ScrollPosition.X, normalizedY * maxScrollY));

                _context.ActiveScrollbarId = thumbId.Id;
                _context.ActiveScrollContainerId = scrollContainerId;
                _context.IsVerticalScrollbar = true;
                _context.ScrollbarDragOffset = thumbHeight / 2;
            }

            _context.ActiveResizeWindowId = 0;
            _context.ActiveResizeDirection = ResizeDirection.None;
            _context.ClickConsumedThisFrame = true;
        }

        // Determine thumb color based on state
        var thumbColor = (isActiveScrollbar || isThumbHovered) ? s.ThumbHoverColor : s.ThumbColor;

        // Vertical scrollbar track — uses spacers for thumb positioning instead of
        // padding so the track always fills its parent height correctly.
        using (Clay.Element(new ElementDeclaration
        {
            Id = trackId,
            Layout = new LayoutConfig
            {
                Direction = LayoutDirection.TopToBottom,
                Sizing = new Sizing
                {
                    Width = SizingAxis.Fixed(s.Width),
                    Height = SizingAxis.Grow()
                },
                Padding = new Padding
                {
                    Left = (ushort)s.TrackPadding,
                    Right = (ushort)s.TrackPadding,
                    Top = (ushort)s.TrackPadding,
                    Bottom = (ushort)s.TrackPadding
                }
            },
            BackgroundColor = sk.Track.HasImage ? Color.Transparent : s.TrackColor,
            CornerRadius = sk.Track.HasImage ? CornerRadius.Zero : s.CornerRadius,
            Image = sk.Track.HasImage ? SkinToImageConfig(sk.Track) : default
        }))
        {
            // Top spacer — positions the thumb
            using (Clay.Element(new ElementDeclaration
            {
                Layout = new LayoutConfig
                {
                    Sizing = new Sizing
                    {
                        Width = SizingAxis.Grow(),
                        Height = SizingAxis.Fixed(thumbY)
                    }
                }
            })) { }

            // Thumb
            var thumbSkinImg = sk.Thumb.HasImages
                ? sk.Thumb.ForState(isActiveScrollbar, isThumbHovered)
                : default;
            using (Clay.Element(new ElementDeclaration
            {
                Id = thumbId,
                Layout = new LayoutConfig
                {
                    Sizing = new Sizing
                    {
                        Width = SizingAxis.Grow(),
                        Height = SizingAxis.Fixed(thumbHeight)
                    }
                },
                BackgroundColor = thumbSkinImg.HasImage ? Color.Transparent : thumbColor,
                CornerRadius = thumbSkinImg.HasImage ? CornerRadius.Zero : s.CornerRadius,
                Image = thumbSkinImg.HasImage ? SkinToImageConfig(thumbSkinImg) : default
            })) { }
        }
    }

    /// <summary>
    /// Renders an inline horizontal scrollbar for a scroll container.
    /// Use this when manually creating scroll containers with the raw Clay API.
    /// Must be called as a sibling to the scroll container, inside a vertical wrapper.
    /// </summary>
    /// <param name="scrollContainerId">The ElementId of the scroll container.</param>
    /// <param name="style">Optional scrollbar style.</param>
    public static void HorizontalScrollbar(ElementId scrollContainerId, ScrollbarStyle? style = null, ScrollbarSkin? skin = null)
    {
        var s = style.HasValue ? style.Value.MergeOver(Style.Scrollbar) : Style.Scrollbar;
        var sk = skin ?? Skin?.Scrollbar ?? default;
        var scrollData = Clay.GetScrollContainerData(scrollContainerId);

        // Only render if content actually overflows
        if (!scrollData.Found || !scrollData.OverflowsX)
        {
            // Add empty spacer to maintain layout consistency
            using (Clay.Element(new ElementDeclaration
            {
                Layout = new LayoutConfig
                {
                    Sizing = new Sizing
                    {
                        Width = SizingAxis.Grow(),
                        Height = SizingAxis.Fixed(s.Width + s.TrackPadding * 2)
                    }
                }
            })) { }
            return;
        }

        float containerWidth = scrollData.ScrollContainerDimensions.Width;
        float contentWidth = scrollData.ContentDimensions.Width;
        float scrollX = scrollData.ScrollPosition.X;
        float maxScrollX = scrollData.MaxScrollX;

        var trackId = ElementId.HashComposite("SbTrackH_", scrollContainerId.Id);
        var thumbId = ElementId.HashComposite("SbThumbH_", scrollContainerId.Id);

        // Use the track's actual rendered width from previous frame when available
        var trackData = Clay.GetElementData(trackId);
        float trackInnerWidth = trackData.Found
            ? trackData.BoundingBox.Width - s.TrackPadding * 2
            : containerWidth - s.TrackPadding * 2;

        // Calculate thumb size and position
        float thumbWidth = Math.Max(s.MinThumbSize, (containerWidth / contentWidth) * trackInnerWidth);
        float thumbTravel = trackInnerWidth - thumbWidth;
        float thumbX = maxScrollX > 0 ? (scrollX / maxScrollX) * thumbTravel : 0;

        // Use the hit area (or track) for hover detection, but check thumb X range
        var hoverId = _context.ScrollbarHitAreaId.Id != 0 ? _context.ScrollbarHitAreaId : trackId;
        bool isHitAreaHovered = IsHovered(hoverId);
        bool isThumbHovered = false;
        bool isActiveScrollbar = _context.ActiveScrollbarId == thumbId.Id;

        if (isHitAreaHovered || isActiveScrollbar)
        {
            var thumbData = Clay.GetElementData(thumbId);
            if (thumbData.Found)
            {
                float mouseX = Clay.GetPointerState().Position.X;
                isThumbHovered = mouseX >= thumbData.BoundingBox.X && mouseX <= thumbData.BoundingBox.X + thumbData.BoundingBox.Width;
            }
        }

        // Check if mouse is within the track's actual bounds
        bool isTrackHovered = trackData.Found && Clay.GetPointerState().Position.X >= trackData.BoundingBox.X
            && Clay.GetPointerState().Position.X <= trackData.BoundingBox.X + trackData.BoundingBox.Width;

        // Handle click — scrollbar takes priority over window resize when both
        // overlap, so use IsMouseJustPressed directly and cancel any resize.
        if ((isThumbHovered || (isHitAreaHovered && isTrackHovered)) && IsMouseJustPressed && !IsMouseOverAnyPopup)
        {
            if (isThumbHovered)
            {
                // Clicked on thumb — start dragging from current position
                _context.ActiveScrollbarId = thumbId.Id;
                _context.ActiveScrollContainerId = scrollContainerId;
                _context.IsVerticalScrollbar = false;

                var pointerData = Clay.GetPointerState();
                var thumbData = Clay.GetElementData(thumbId);
                if (thumbData.Found)
                {
                    _context.ScrollbarDragOffset = pointerData.Position.X - thumbData.BoundingBox.X;
                }
            }
            else if (thumbTravel > 0)
            {
                // Clicked on track — jump thumb center to click position, then start dragging
                float mouseX = Clay.GetPointerState().Position.X;
                float clickInTrack = mouseX - trackData.BoundingBox.X - s.TrackPadding - thumbWidth / 2;
                float normalizedX = Math.Clamp(clickInTrack / thumbTravel, 0f, 1f);
                Clay.SetScrollPosition(scrollContainerId, new Vector2(normalizedX * maxScrollX, scrollData.ScrollPosition.Y));

                _context.ActiveScrollbarId = thumbId.Id;
                _context.ActiveScrollContainerId = scrollContainerId;
                _context.IsVerticalScrollbar = false;
                _context.ScrollbarDragOffset = thumbWidth / 2;
            }

            _context.ActiveResizeWindowId = 0;
            _context.ActiveResizeDirection = ResizeDirection.None;
            _context.ClickConsumedThisFrame = true;
        }

        // Determine thumb color based on state
        var thumbColor = (isActiveScrollbar || isThumbHovered) ? s.ThumbHoverColor : s.ThumbColor;

        // Horizontal scrollbar track — uses spacers for thumb positioning
        using (Clay.Element(new ElementDeclaration
        {
            Id = trackId,
            Layout = new LayoutConfig
            {
                Direction = LayoutDirection.LeftToRight,
                Sizing = new Sizing
                {
                    Width = SizingAxis.Grow(),
                    Height = SizingAxis.Fixed(s.Width)
                },
                Padding = new Padding
                {
                    Left = (ushort)s.TrackPadding,
                    Top = (ushort)s.TrackPadding,
                    Bottom = (ushort)s.TrackPadding,
                    Right = (ushort)s.TrackPadding
                }
            },
            BackgroundColor = sk.Track.HasImage ? Color.Transparent : s.TrackColor,
            CornerRadius = sk.Track.HasImage ? CornerRadius.Zero : s.CornerRadius,
            Image = sk.Track.HasImage ? SkinToImageConfig(sk.Track) : default
        }))
        {
            // Left spacer — positions the thumb
            using (Clay.Element(new ElementDeclaration
            {
                Layout = new LayoutConfig
                {
                    Sizing = new Sizing
                    {
                        Width = SizingAxis.Fixed(thumbX),
                        Height = SizingAxis.Grow()
                    }
                }
            })) { }

            // Thumb
            var thumbSkinImg = sk.Thumb.HasImages
                ? sk.Thumb.ForState(isActiveScrollbar, isThumbHovered)
                : default;
            using (Clay.Element(new ElementDeclaration
            {
                Id = thumbId,
                Layout = new LayoutConfig
                {
                    Sizing = new Sizing
                    {
                        Width = SizingAxis.Fixed(thumbWidth),
                        Height = SizingAxis.Grow()
                    }
                },
                BackgroundColor = thumbSkinImg.HasImage ? Color.Transparent : thumbColor,
                CornerRadius = thumbSkinImg.HasImage ? CornerRadius.Zero : s.CornerRadius,
                Image = thumbSkinImg.HasImage ? SkinToImageConfig(thumbSkinImg) : default
            })) { }
        }
    }

    /// <summary>
    /// Begins a scrollable area with automatic scrollbars.
    /// Call <see cref="EndScrollArea"/> when done adding children.
    /// </summary>
    /// <param name="id">Unique identifier for the scroll area.</param>
    /// <param name="maxHeight">Maximum height before vertical scrolling kicks in.</param>
    /// <param name="horizontal">Enable horizontal scrolling in addition to vertical.</param>
    /// <param name="style">Optional visual style.</param>
    public static void BeginScrollArea(string id, float? maxHeight = null, bool horizontal = false, ScrollAreaStyle? style = null)
    {
        var s = style.HasValue ? style.Value.MergeOver(Style.ScrollArea) : Style.ScrollArea;
        var scrollId = StableId($"ScrollArea_{id}");

        var scrollConfig = horizontal
            ? new ScrollConfig { Vertical = true, Horizontal = true }
            : ScrollConfig.VerticalScroll;

        if (horizontal)
        {
            _context.LayoutScrollInfo.Push(new ClayUIContext.ScrollWrapperInfo(scrollId, IsVertical: true, HasWrapper: true, HasBothAxes: true));

            // Outer wrapper (vertical: [row] + horizontal scrollbar)
            Clay.Element(new ElementDeclaration
            {
                Layout = new LayoutConfig
                {
                    Direction = LayoutDirection.TopToBottom,
                    Sizing = new Sizing
                    {
                        Width = SizingAxis.Grow(),
                        Height = maxHeight.HasValue
                            ? SizingAxis.Fit(0, maxHeight.Value)
                            : SizingAxis.Grow()
                    }
                },
                BackgroundColor = s.BackgroundColor,
                CornerRadius = s.CornerRadius,
                Shadow = s.Shadow
            });
            _context.LayoutDepth++;

            // Inner row (horizontal: scroll container + vertical scrollbar)
            Clay.Element(new ElementDeclaration
            {
                Layout = new LayoutConfig
                {
                    Direction = LayoutDirection.LeftToRight,
                    Sizing = Sizing.Fill()
                }
            });
            _context.LayoutDepth++;

            // Scroll container
            Clay.Element(new ElementDeclaration
            {
                Id = scrollId,
                Layout = new LayoutConfig
                {
                    Direction = LayoutDirection.TopToBottom,
                    Sizing = Sizing.Fill(),
                    Padding = s.Padding
                },
                Scroll = scrollConfig
            });
            _context.LayoutDepth++;
        }
        else
        {
            _context.LayoutScrollInfo.Push(new ClayUIContext.ScrollWrapperInfo(scrollId, IsVertical: true, HasWrapper: true));

            // Wrapper container (horizontal: scroll content + vertical scrollbar)
            Clay.Element(new ElementDeclaration
            {
                Layout = new LayoutConfig
                {
                    Direction = LayoutDirection.LeftToRight,
                    Sizing = new Sizing
                    {
                        Width = SizingAxis.Grow(),
                        Height = maxHeight.HasValue
                            ? SizingAxis.Fit(0, maxHeight.Value)
                            : SizingAxis.Grow()
                    }
                },
                BackgroundColor = s.BackgroundColor,
                CornerRadius = s.CornerRadius,
                Shadow = s.Shadow
            });
            _context.LayoutDepth++;

            // Scroll container inside wrapper
            Clay.Element(new ElementDeclaration
            {
                Id = scrollId,
                Layout = new LayoutConfig
                {
                    Direction = LayoutDirection.TopToBottom,
                    Sizing = Sizing.Fill(),
                    Padding = s.Padding
                },
                Scroll = scrollConfig
            });
            _context.LayoutDepth++;
        }
    }

    /// <summary>
    /// Ends a scrollable area started with <see cref="BeginScrollArea"/>.
    /// Automatically adds scrollbars.
    /// </summary>
    public static void EndScrollArea()
    {
        // Close the scroll container
        if (_context.LayoutDepth > 0)
        {
            Clay.CloseElement();
            _context.LayoutDepth--;
        }

        ClayUIContext.ScrollWrapperInfo? scrollInfo = null;
        if (_context.LayoutScrollInfo.Count > 0)
            scrollInfo = _context.LayoutScrollInfo.Pop();

        if (scrollInfo.HasValue && scrollInfo.Value.HasBothAxes)
        {
            // Dual-axis: close inner row (add v-scrollbar), then close outer (add h-scrollbar)
            if (scrollInfo.Value.HasWrapper)
                VerticalScrollbar(scrollInfo.Value.ScrollId);

            // Close inner row
            if (_context.LayoutDepth > 0)
            {
                Clay.CloseElement();
                _context.LayoutDepth--;
            }

            // Add horizontal scrollbar + close outer
            HorizontalScrollbar(scrollInfo.Value.ScrollId);

            if (_context.LayoutDepth > 0)
            {
                Clay.CloseElement();
                _context.LayoutDepth--;
            }
        }
        else
        {
            // Vertical-only: close wrapper + add v-scrollbar
            if (_context.LayoutDepth > 0)
            {
                if (scrollInfo.HasValue && scrollInfo.Value.HasWrapper)
                    VerticalScrollbar(scrollInfo.Value.ScrollId);

                Clay.CloseElement();
                _context.LayoutDepth--;
            }
        }
    }

    // ============ ListBox ============

    /// <summary>
    /// Begins a scrollable list box. Each item is rendered with <see cref="ListBoxItem"/>.
    /// Call <see cref="EndListBox"/> when done.
    /// </summary>
    /// <param name="label">Label displayed above the list box. Use ## for unique IDs.</param>
    /// <param name="maxHeight">Maximum height before scrolling. Default 150.</param>
    /// <param name="style">Optional style override.</param>
    public static void BeginListBox(string label, float maxHeight = 150, ListBoxStyle? style = null)
    {
        var s = style.HasValue ? style.Value.MergeOver(Style.ListBox) : Style.ListBox;
        var listId = StableId($"ListBox_{label}");
        var scrollId = StableId($"ListBoxScroll_{label}");

        // Outer container
        Clay.Element(new ElementDeclaration
        {
            Layout = new LayoutConfig
            {
                Direction = LayoutDirection.TopToBottom,
                ChildGap = 4,
                Sizing = new Sizing { Width = SizingAxis.Grow() }
            }
        });
        _context.LayoutDepth++;
        _context.LayoutScrollInfo.Push(null);

        // Label
        if (!string.IsNullOrEmpty(label))
        {
            Clay.Text(ElementId.GetDisplayLabel(label), new TextConfig
            {
                FontId = s.FontId,
                FontSize = s.FontSize,
                TextColor = s.LabelColor
            });
        }

        // Scroll wrapper (horizontal: scroll content + scrollbar)
        Clay.Element(new ElementDeclaration
        {
            Id = listId,
            Layout = new LayoutConfig
            {
                Direction = LayoutDirection.LeftToRight,
                Sizing = new Sizing
                {
                    Width = SizingAxis.Grow(),
                    Height = SizingAxis.Fit(0, maxHeight)
                }
            },
            BackgroundColor = s.BackgroundColor,
            CornerRadius = s.CornerRadius,
            Border = s.Border
        });
        _context.LayoutDepth++;
        _context.LayoutScrollInfo.Push(new ClayUIContext.ScrollWrapperInfo(scrollId, IsVertical: true, HasWrapper: true));

        // Scroll container
        Clay.Element(new ElementDeclaration
        {
            Id = scrollId,
            Layout = new LayoutConfig
            {
                Direction = LayoutDirection.TopToBottom,
                Sizing = Sizing.Fill(),
                Padding = s.Padding
            },
            Scroll = ScrollConfig.VerticalScroll
        });
        _context.LayoutDepth++;
        _context.LayoutScrollInfo.Push(null);
    }

    /// <summary>
    /// Renders a selectable item inside a list box. Returns true when clicked.
    /// </summary>
    /// <param name="label">Item text.</param>
    /// <param name="isSelected">Whether this item is currently selected.</param>
    /// <returns>True if the item was clicked.</returns>
    public static bool ListBoxItem(string label, bool isSelected)
    {
        var s = Style.ListBox;
        var itemId = Id($"LBI_{label}");
        bool isHovered = IsHovered(itemId);
        bool clicked = isHovered && ShouldProcessClick;

        var bgColor = isSelected ? s.SelectedColor
            : isHovered ? s.HoverColor
            : Color.Transparent;

        using (Clay.Element(new ElementDeclaration
        {
            Id = itemId,
            Layout = new LayoutConfig
            {
                Sizing = new Sizing { Width = SizingAxis.Grow() },
                Padding = s.ItemPadding
            },
            BackgroundColor = bgColor,
            CornerRadius = CornerRadius.All(s.ItemCornerRadius)
        }))
        {
            Clay.Text(ElementId.GetDisplayLabel(label), new TextConfig
            {
                FontId = s.FontId,
                FontSize = s.FontSize,
                TextColor = isSelected ? s.SelectedTextColor : s.TextColor
            });
        }

        return clicked;
    }

    /// <summary>
    /// Ends a list box started with <see cref="BeginListBox"/>.
    /// </summary>
    public static void EndListBox()
    {
        // Close scroll container
        if (_context.LayoutDepth > 0)
        {
            _context.LayoutScrollInfo.Pop();
            Clay.CloseElement();
            _context.LayoutDepth--;
        }

        // Close scroll wrapper + add vertical scrollbar
        if (_context.LayoutDepth > 0)
        {
            var scrollInfo = _context.LayoutScrollInfo.Pop();
            if (scrollInfo.HasValue && scrollInfo.Value.HasWrapper)
                VerticalScrollbar(scrollInfo.Value.ScrollId);
            Clay.CloseElement();
            _context.LayoutDepth--;
        }

        // Close outer container
        if (_context.LayoutDepth > 0)
        {
            _context.LayoutScrollInfo.Pop();
            Clay.CloseElement();
            _context.LayoutDepth--;
        }
    }

    // ============ Combo ============

    /// <summary>
    /// Renders a combo box (dropdown). Shows the currently selected item; clicking opens a popup
    /// with all options. Returns true when selection changes.
    /// </summary>
    /// <param name="label">Combo label. Use ## for unique IDs.</param>
    /// <param name="selectedIndex">Index of the currently selected option.</param>
    /// <param name="options">Display labels for each option.</param>
    /// <param name="style">Optional style override.</param>
    /// <returns>True if selection changed.</returns>
    public static bool Combo(string label, ref int selectedIndex, string[] options, ComboStyle? style = null)
    {
        var s = style.HasValue ? style.Value.MergeOver(Style.Combo) : Style.Combo;
        string popupId = $"Combo_{label}";
        var buttonId = Id($"ComboBtn_{label}");
        bool changed = false;

        using (Clay.Element(new ElementDeclaration
        {
            Layout = new LayoutConfig
            {
                Direction = LayoutDirection.LeftToRight,
                ChildGap = 8,
                ChildAlignment = ChildAlignment.CenterLeft,
                Sizing = new Sizing { Width = SizingAxis.Grow() }
            }
        }))
        {
            // Label
            if (!string.IsNullOrEmpty(label))
            {
                Clay.Text(ElementId.GetDisplayLabel(label), new TextConfig
                {
                    FontId = s.FontId,
                    FontSize = s.FontSize,
                    TextColor = s.LabelColor
                });
            }

            // Combo button showing current selection
            bool isHovered = IsHovered(buttonId);
            string displayText = selectedIndex >= 0 && selectedIndex < options.Length
                ? options[selectedIndex]
                : "";

            using (Clay.Element(new ElementDeclaration
            {
                Id = buttonId,
                Layout = new LayoutConfig
                {
                    Sizing = new Sizing
                    {
                        Width = SizingAxis.Grow(s.MinWidth),
                        Height = SizingAxis.Fit()
                    },
                    Padding = s.Padding,
                    Direction = LayoutDirection.LeftToRight,
                    ChildAlignment = ChildAlignment.CenterLeft
                },
                BackgroundColor = isHovered ? s.HoverColor : s.BackgroundColor,
                CornerRadius = s.CornerRadius,
                Border = s.Border
            }))
            {
                // Selected text
                Clay.Text(displayText, new TextConfig
                {
                    FontId = s.FontId,
                    FontSize = s.FontSize,
                    TextColor = s.TextColor
                });

                // Spacer + arrow
                using (Clay.Element(new ElementDeclaration
                {
                    Layout = new LayoutConfig { Sizing = new Sizing { Width = SizingAxis.Grow() } }
                })) { }

                Clay.Text(IsPopupOpen(popupId) ? "^" : "v", new TextConfig
                {
                    FontId = s.FontId,
                    FontSize = (ushort)(s.FontSize - 2),
                    TextColor = s.ArrowColor
                });
            }

            // Open popup on click
            if (isHovered && ShouldProcessClick)
            {
                var btnData = Clay.GetElementData(buttonId);
                if (btnData.Found)
                {
                    var box = btnData.BoundingBox;
                    OpenPopupAt(popupId, new Vector2(box.X, box.Y + box.Height + 2));
                }
                else
                {
                    OpenPopup(popupId);
                }
            }
        }

        // Dropdown popup
        if (BeginPopup(popupId, new PopupStyle
        {
            MinWidth = s.MinWidth,
            MaxWidth = s.MaxWidth,
            Padding = Padding.All(4),
            ContentGap = 2
        }))
        {
            for (int i = 0; i < options.Length; i++)
            {
                var itemId = StableId($"ComboItem_{label}_{i}");
                bool isItemHovered = IsHovered(itemId);
                bool isItemSelected = i == selectedIndex;
                bool itemClicked = isItemHovered && ShouldProcessClick;

                var bgColor = isItemSelected ? s.SelectedColor
                    : isItemHovered ? s.ItemHoverColor
                    : Color.Transparent;

                using (Clay.Element(new ElementDeclaration
                {
                    Id = itemId,
                    Layout = new LayoutConfig
                    {
                        Sizing = new Sizing { Width = SizingAxis.Grow() },
                        Padding = Padding.Symmetric(8, 4)
                    },
                    BackgroundColor = bgColor,
                    CornerRadius = CornerRadius.All(2)
                }))
                {
                    Clay.Text(options[i], new TextConfig
                    {
                        FontId = s.FontId,
                        FontSize = s.FontSize,
                        TextColor = isItemSelected ? s.SelectedTextColor : s.TextColor
                    });
                }

                if (itemClicked)
                {
                    selectedIndex = i;
                    changed = true;
                    CloseAllPopups();
                }
            }

            EndPopup();
        }

        return changed;
    }

    // ============ Selection Widgets ============

    /// <summary>
    /// Renders a radio button group with an int index. Returns true when selection changes.
    /// </summary>
    /// <param name="label">Group label.</param>
    /// <param name="selectedIndex">Index of the currently selected option.</param>
    /// <param name="options">Display labels for each option.</param>
    /// <param name="style">Optional style.</param>
    public static bool RadioGroup(string label, ref int selectedIndex, string[] options, RadioGroupStyle? style = null, RadioGroupSkin? skin = null)
    {
        var s = style.HasValue ? style.Value.MergeOver(Style.RadioGroup) : Style.RadioGroup;
        var sk = skin ?? Skin?.RadioGroup ?? default;
        bool changed = false;

        using (Clay.Element(new ElementDeclaration
        {
            Layout = new LayoutConfig
            {
                Direction = LayoutDirection.TopToBottom,
                ChildGap = 4,
                Sizing = new Sizing { Width = SizingAxis.Grow() }
            }
        }))
        {
            if (!string.IsNullOrEmpty(label))
            {
                Clay.Text(ElementId.GetDisplayLabel(label), new TextConfig
                {
                    FontId = s.FontId,
                    FontSize = s.FontSize,
                    TextColor = s.LabelColor
                });
                Space(4);
            }

            for (int i = 0; i < options.Length; i++)
            {
                var optionId = Id($"Radio_{label}_{i}");
                bool isSelected = i == selectedIndex;
                bool isHovered = IsHovered(optionId);
                bool clicked = isHovered && ShouldProcessClick;

                if (clicked && !isSelected)
                {
                    selectedIndex = i;
                    changed = true;
                }

                using (Clay.Element(new ElementDeclaration
                {
                    Id = optionId,
                    Layout = new LayoutConfig
                    {
                        Direction = LayoutDirection.LeftToRight,
                        ChildGap = 8,
                        ChildAlignment = ChildAlignment.CenterLeft,
                        Padding = s.OptionPadding
                    },
                    BackgroundColor = isHovered ? s.HoverColor : Color.Transparent
                }))
                {
                    // Radio circle
                    var circleSkin = isSelected && sk.SelectedCircle.HasImage
                        ? sk.SelectedCircle
                        : sk.Circle.HasImages ? sk.Circle.ForState(false, isHovered) : default;

                    using (Clay.Element(new ElementDeclaration
                    {
                        Layout = new LayoutConfig
                        {
                            Sizing = Sizing.FixedSize(s.CircleSize, s.CircleSize),
                            ChildAlignment = ChildAlignment.Center
                        },
                        BackgroundColor = circleSkin.HasImage ? Color.Transparent : s.CircleColor,
                        CornerRadius = circleSkin.HasImage ? CornerRadius.Zero : CornerRadius.All(s.CircleSize / 2),
                        Border = circleSkin.HasImage ? default : BorderConfig.Uniform(1, s.CircleBorderColor),
                        Image = circleSkin.HasImage ? SkinToImageConfig(circleSkin) : default
                    }))
                    {
                        if (isSelected && !circleSkin.HasImage)
                        {
                            using (Clay.Element(new ElementDeclaration
                            {
                                Layout = new LayoutConfig
                                {
                                    Sizing = Sizing.FixedSize(s.DotSize, s.DotSize)
                                },
                                BackgroundColor = s.DotColor,
                                CornerRadius = CornerRadius.All(s.DotSize / 2)
                            })) { }
                        }
                    }

                    // Option label
                    Clay.Text(options[i], new TextConfig
                    {
                        FontId = s.FontId,
                        FontSize = s.FontSize,
                        TextColor = s.TextColor
                    });
                }
            }
        }

        return changed;
    }

    // ============ Utility ============

    /// <summary>
    /// Returns true if the widget with the given label was hovered this frame.
    /// </summary>
    public static bool WasHovered(string label)
    {
        var id = StableId(label);
        return _context.HoveredThisFrame.Contains(id.Id);
    }

    /// <summary>
    /// Returns true if the widget with the given label was clicked this frame.
    /// </summary>
    public static bool WasClicked(string label)
    {
        var id = StableId(label);
        return _context.PressedThisFrame.Contains(id.Id);
    }

    /// <summary>
    /// Clears all stored widget states (useful when changing screens).
    /// </summary>
    public static void ClearState()
    {
        _context.ClearState();
    }

    // ============ Debug Window ============

    private static bool _debugWindowOpen = true;

    /// <summary>
    /// Renders a debug window showing internal state like ImGui's metrics window.
    /// </summary>
    /// <param name="open">Reference to control window visibility.</param>
    public static void DebugWindow(ref bool open)
    {
        if (!open) return;

        // Use default style for the debug window so user style edits don't break the editor UI.
        // The style editor reads/writes from editStyle (the user's style) and we restore it after.
        var editStyle = Style;
        Style = ClayUIStyle.Default;

        var defaultPos = new Vector2(10, 10);
        var defaultSize = new Vector2(320, 400);

        if (BeginWindow("ClayUI Debug", ref open, defaultPosition: defaultPos, defaultSize: defaultSize, flags: WindowFlags.NoDocking))
        {
            // Frame stats
            if (BeginTreeNode("Clay Stats"))
            {
                Label($"Elements: {Clay.GetElementCount()} / {Clay.GetMaxElementCount()}");
                Label($"Render Commands: {Clay.GetRenderCommandCount()}");
                Label($"Text Elements: {Clay.GetTextElementCount()}");
                Label($"Scroll Containers: {Clay.GetScrollContainerCount()}");
                Label($"Tree Roots: {Clay.GetTreeRootCount()}");
                Label($"Generation (Frame): {Clay.GetGeneration()}");

                var dims = Clay.GetLayoutDimensions();
                Label($"Layout Size: {dims.Width:F0} x {dims.Height:F0}");

                Label($"Debug Mode: {(Clay.IsDebugModeEnabled() ? "On" : "Off")}");
                Label($"Culling: {(Clay.IsCullingDisabled() ? "Disabled" : "Enabled")}");
                EndTreeNode();
            }

            Separator();

            // Warnings
            var warnings = Clay.GetWarnings();
            if (warnings.MaxElementsExceeded || warnings.MaxRenderCommandsExceeded ||
                warnings.MaxTextMeasureCacheExceeded || warnings.TextMeasurementFunctionNotSet)
            {
                if (BeginTreeNode("Warnings"))
                {
                    if (warnings.MaxElementsExceeded)
                        Label("! Max Elements Exceeded", new LabelStyle { TextColor = Color.Rgba(255, 100, 100) });
                    if (warnings.MaxRenderCommandsExceeded)
                        Label("! Max Render Commands Exceeded", new LabelStyle { TextColor = Color.Rgba(255, 100, 100) });
                    if (warnings.MaxTextMeasureCacheExceeded)
                        Label("! Max Text Cache Exceeded", new LabelStyle { TextColor = Color.Rgba(255, 100, 100) });
                    if (warnings.TextMeasurementFunctionNotSet)
                        Label("! Text Measure Function Not Set", new LabelStyle { TextColor = Color.Rgba(255, 100, 100) });
                    EndTreeNode();
                }
                Separator();
            }

            // Hovered element
            if (BeginTreeNode("Hovered Element"))
            {
                var pointer = Clay.GetPointerState();
                var ctx = Clay.Context;
                uint hoveredId = 0;
                BoundingBox hoveredBox = default;

                if (ctx != null)
                {
                    float smallestArea = float.MaxValue;
                    var hashMap = ctx.LayoutElementsHashMapInternal;

                    for (int i = 0; i < hashMap.Length; i++)
                    {
                        ref var item = ref hashMap[i];
                        if (item.ElementId.Id != 0 &&
                            item.BoundingBox.Width > 0 &&
                            item.BoundingBox.Height > 0 &&
                            item.BoundingBox.Contains(pointer.Position))
                        {
                            float area = item.BoundingBox.Width * item.BoundingBox.Height;
                            if (area < smallestArea)
                            {
                                smallestArea = area;
                                hoveredId = item.ElementId.Id;
                                hoveredBox = item.BoundingBox;
                            }
                        }
                    }
                }

                if (hoveredId != 0)
                {
                    Label($"ID: {hoveredId}");
                    Label($"Position: ({hoveredBox.X:F0}, {hoveredBox.Y:F0})");
                    Label($"Size: ({hoveredBox.Width:F0} x {hoveredBox.Height:F0})");
                }
                else
                {
                    Label("(none)");
                }

                EndTreeNode();
            }

            Separator();

            // Input state
            if (BeginTreeNode("Input State"))
            {
                var pointer = Clay.GetPointerState();
                Label($"Mouse Position: ({pointer.Position.X:F0}, {pointer.Position.Y:F0})");
                Label($"Mouse Pressed: {_context.MousePressed}");
                Label($"Mouse Just Pressed: {IsMouseJustPressed}");
                Label($"Scroll Delta: ({_context.ScrollDelta.X:F1}, {_context.ScrollDelta.Y:F1})");
                Label($"Over Any Window: {IsMouseOverAnyWindow}");
                EndTreeNode();
            }

            Separator();

            // ClayUI state
            if (BeginTreeNode("ClayUI State"))
            {
                Label($"Window Count: {_context.WindowStates.Count}");
                Label($"Toggle States: {_context.ToggleStates.Count}");
                Label($"Slider States: {_context.SliderStates.Count}");
                Label($"Expanded States: {_context.ExpandedStates.Count}");
                Label($"Text Input States: {_context.TextInputStates.Count}");
                EndTreeNode();
            }

            Separator();

            // Active interactions
            if (BeginTreeNode("Active Interactions"))
            {
                if (_context.ActiveDragWindowId != 0)
                    Label($"Dragging Window: {_context.ActiveDragWindowId}");
                else
                    Label("Dragging Window: None");

                if (_context.ActiveResizeWindowId != 0)
                    Label($"Resizing Window: {_context.ActiveResizeWindowId} ({_context.ActiveResizeDirection})");
                else
                    Label("Resizing Window: None");

                if (_context.ActiveScrollbarId != 0)
                    Label($"Active Scrollbar: {_context.ActiveScrollbarId}");
                else
                    Label("Active Scrollbar: None");

                if (_context.ActiveSliderTrackId != 0)
                    Label($"Active Slider: {_context.ActiveSliderTrackId}");
                else
                    Label("Active Slider: None");

                Label($"Scroll Consumed: {_context.ScrollConsumedByWindow}");
                EndTreeNode();
            }

            Separator();

            // Windows list
            if (BeginTreeNode("Windows"))
            {
                foreach (var (windowId, state) in _context.WindowStates)
                {
                    int focusOrder = _context.WindowFocusOrder.IndexOf(windowId);
                    string status = state.Open
                        ? (state.Collapsed ? "Collapsed" : "Open")
                        : "Closed";

                    if (BeginTreeNode($"Window {windowId} [{status}]"))
                    {
                        Label($"Position: ({state.Position.X:F0}, {state.Position.Y:F0})");
                        Label($"Size: ({state.Size.X:F0}, {state.Size.Y:F0})");
                        Label($"Focus Order: {(focusOrder >= 0 ? focusOrder.ToString() : "N/A")}");
                        Label($"Z-Index: {_context.GetWindowZIndex(windowId)}");
                        Label($"Topmost: {state.Topmost}");
                        EndTreeNode();
                    }
                }

                if (_context.WindowStates.Count == 0)
                    Label("(no windows)");

                EndTreeNode();
            }

            // Popups list
            if (BeginTreeNode("Popups"))
            {
                Label($"Open Popups: {_context.OpenPopupStack.Count}");

                foreach (var popupId in _context.OpenPopupStack)
                {
                    if (_context.PopupStates.TryGetValue(popupId, out var state))
                    {
                        if (BeginTreeNode($"Popup {popupId}"))
                        {
                            Label($"Position: ({state.Position.X:F0}, {state.Position.Y:F0})");
                            Label($"Z-Index: {_context.GetPopupZIndex(popupId)}");
                            EndTreeNode();
                        }
                    }
                }

                if (_context.OpenPopupStack.Count == 0)
                    Label("(no open popups)");

                EndTreeNode();
            }
            Separator();

            // Styles editor
            if (BeginTreeNode("Styles"))
            {
                if (BeginTreeNode("Theme Presets"))
                {
                    BeginHorizontal(gap: 8);
                    if (Button("Default")) editStyle = ClayUIStyle.Default;
                    if (Button("Dark")) editStyle = ClayUIStyle.Dark;
                    if (Button("Light")) editStyle = ClayUIStyle.Light;
                    EndHorizontal();
                    EndTreeNode();
                }

                if (BeginTreeNode("Button"))
                {
                    editStyle.Button.BackgroundColor = StyleColorEditor("Background##btn_bg", editStyle.Button.BackgroundColor);
                    editStyle.Button.HoverColor = StyleColorEditor("Hover##btn_hv", editStyle.Button.HoverColor);
                    editStyle.Button.PressedColor = StyleColorEditor("Pressed##btn_pr", editStyle.Button.PressedColor);
                    editStyle.Button.TextColor = StyleColorEditor("Text##btn_tx", editStyle.Button.TextColor);
                    editStyle.Button.FontSize = StyleUshortEditor("Font Size##btn_fs", editStyle.Button.FontSize, 8, 32);
                    editStyle.Button.CornerRadius = StyleCornerRadiusEditor("Corner Radius##btn_cr", editStyle.Button.CornerRadius);
                    EndTreeNode();
                }

                if (BeginTreeNode("Label"))
                {
                    editStyle.Label.TextColor = StyleColorEditor("Text##lbl_tx", editStyle.Label.TextColor);
                    editStyle.Label.FontSize = StyleUshortEditor("Font Size##lbl_fs", editStyle.Label.FontSize, 8, 32);
                    editStyle.Label.LineHeight = StyleUshortEditor("Line Height##lbl_lh", editStyle.Label.LineHeight, 10, 40);
                    EndTreeNode();
                }

                if (BeginTreeNode("Heading"))
                {
                    editStyle.Heading.TextColor = StyleColorEditor("Text##hd_tx", editStyle.Heading.TextColor);
                    editStyle.Heading.FontSize = StyleUshortEditor("Font Size##hd_fs", editStyle.Heading.FontSize, 10, 48);
                    EndTreeNode();
                }

                if (BeginTreeNode("Checkbox"))
                {
                    editStyle.Checkbox.BoxColor = StyleColorEditor("Box##cb_box", editStyle.Checkbox.BoxColor);
                    editStyle.Checkbox.CheckedColor = StyleColorEditor("Checked##cb_chk", editStyle.Checkbox.CheckedColor);
                    editStyle.Checkbox.CheckmarkColor = StyleColorEditor("Checkmark##cb_cm", editStyle.Checkbox.CheckmarkColor);
                    editStyle.Checkbox.BoxBorderColor = StyleColorEditor("Border##cb_brd", editStyle.Checkbox.BoxBorderColor);
                    editStyle.Checkbox.TextColor = StyleColorEditor("Text##cb_tx", editStyle.Checkbox.TextColor);
                    editStyle.Checkbox.HoverColor = StyleColorEditor("Hover##cb_hv", editStyle.Checkbox.HoverColor);
                    editStyle.Checkbox.BoxSize = StyleFloatEditor("Box Size##cb_bs", editStyle.Checkbox.BoxSize, 10, 30);
                    editStyle.Checkbox.FontSize = StyleUshortEditor("Font Size##cb_fs", editStyle.Checkbox.FontSize, 8, 32);
                    EndTreeNode();
                }

                if (BeginTreeNode("Slider"))
                {
                    editStyle.Slider.TrackColor = StyleColorEditor("Track##sl_tr", editStyle.Slider.TrackColor);
                    editStyle.Slider.FillColor = StyleColorEditor("Fill##sl_fl", editStyle.Slider.FillColor);
                    editStyle.Slider.TextColor = StyleColorEditor("Text##sl_tx", editStyle.Slider.TextColor);
                    editStyle.Slider.ValueTextColor = StyleColorEditor("Value Text##sl_vt", editStyle.Slider.ValueTextColor);
                    editStyle.Slider.TrackHeight = StyleFloatEditor("Track Height##sl_th", editStyle.Slider.TrackHeight, 4, 20);
                    editStyle.Slider.FontSize = StyleUshortEditor("Font Size##sl_fs", editStyle.Slider.FontSize, 8, 32);
                    EndTreeNode();
                }

                if (BeginTreeNode("Toggle"))
                {
                    editStyle.Toggle.OnColor = StyleColorEditor("On##tg_on", editStyle.Toggle.OnColor);
                    editStyle.Toggle.OffColor = StyleColorEditor("Off##tg_off", editStyle.Toggle.OffColor);
                    editStyle.Toggle.KnobColor = StyleColorEditor("Knob##tg_kn", editStyle.Toggle.KnobColor);
                    editStyle.Toggle.TextColor = StyleColorEditor("Text##tg_tx", editStyle.Toggle.TextColor);
                    editStyle.Toggle.TrackWidth = StyleFloatEditor("Track Width##tg_tw", editStyle.Toggle.TrackWidth, 30, 60);
                    editStyle.Toggle.TrackHeight = StyleFloatEditor("Track Height##tg_th", editStyle.Toggle.TrackHeight, 16, 36);
                    editStyle.Toggle.KnobSize = StyleFloatEditor("Knob Size##tg_ks", editStyle.Toggle.KnobSize, 12, 32);
                    EndTreeNode();
                }

                if (BeginTreeNode("Progress Bar"))
                {
                    editStyle.ProgressBar.BackgroundColor = StyleColorEditor("Background##pb_bg", editStyle.ProgressBar.BackgroundColor);
                    editStyle.ProgressBar.FillColor = StyleColorEditor("Fill##pb_fl", editStyle.ProgressBar.FillColor);
                    editStyle.ProgressBar.Height = StyleFloatEditor("Height##pb_h", editStyle.ProgressBar.Height, 4, 20);
                    editStyle.ProgressBar.CornerRadius = StyleFloatEditor("Corner Radius##pb_cr", editStyle.ProgressBar.CornerRadius, 0, 10);
                    EndTreeNode();
                }

                if (BeginTreeNode("Panel"))
                {
                    editStyle.Panel.BackgroundColor = StyleColorEditor("Background##pn_bg", editStyle.Panel.BackgroundColor);
                    editStyle.Panel.TitleColor = StyleColorEditor("Title##pn_tt", editStyle.Panel.TitleColor);
                    editStyle.Panel.SeparatorColor = StyleColorEditor("Separator##pn_sp", editStyle.Panel.SeparatorColor);
                    editStyle.Panel.ChildGap = StyleUshortEditor("Child Gap##pn_cg", editStyle.Panel.ChildGap, 0, 24);
                    editStyle.Panel.TitleFontSize = StyleUshortEditor("Title Font##pn_tf", editStyle.Panel.TitleFontSize, 10, 24);
                    EndTreeNode();
                }

                if (BeginTreeNode("Window"))
                {
                    editStyle.Window.BackgroundColor = StyleColorEditor("Background##wn_bg", editStyle.Window.BackgroundColor);
                    editStyle.Window.TitleBarColor = StyleColorEditor("Title Bar##wn_tb", editStyle.Window.TitleBarColor);
                    editStyle.Window.TitleColor = StyleColorEditor("Title Text##wn_tt", editStyle.Window.TitleColor);
                    editStyle.Window.ButtonHoverColor = StyleColorEditor("Btn Hover##wn_bh", editStyle.Window.ButtonHoverColor);
                    editStyle.Window.CloseButtonHoverColor = StyleColorEditor("Close Hover##wn_ch", editStyle.Window.CloseButtonHoverColor);
                    editStyle.Window.TitleBarHeight = StyleFloatEditor("Title Bar Height##wn_tbh", editStyle.Window.TitleBarHeight, 20, 50);
                    editStyle.Window.ButtonSize = StyleFloatEditor("Button Size##wn_bs", editStyle.Window.ButtonSize, 14, 30);
                    editStyle.Window.FontSize = StyleUshortEditor("Font Size##wn_fs", editStyle.Window.FontSize, 10, 24);
                    EndTreeNode();
                }

                if (BeginTreeNode("Separator"))
                {
                    editStyle.SeparatorColor = StyleColorEditor("Color##sep_cl", editStyle.SeparatorColor);
                    EndTreeNode();
                }

                EndTreeNode();
            }
        }
        EndWindow();

        // Restore user style (with any edits applied)
        Style = editStyle;
    }

    // ============ Color Picker ============

    private const float SvPanelSize = 150;
    private const float HueBarWidth = 20;
    private const float HueBarHeight = 150;

    /// <summary>
    /// Renders a color swatch showing the current color. When clicked, opens a popup
    /// with a full HSV color picker. The swatch updates when the popup is closed.
    /// Returns the updated color.
    /// </summary>
    public static Color ColorPicker(string label, Color color)
    {
        string popupId = $"cpPopup_{label}";
        var swatchId = Id($"cpSwatch_{label}");
        uint stateKey = swatchId.Id;

        // Initialize editing state from the passed-in color if not yet tracked
        if (!_context.ColorPickerStates.ContainsKey(stateKey))
            _context.ColorPickerStates[stateKey] = color;

        // The display color is always the stored state (updated live while popup is open)
        var displayColor = _context.ColorPickerStates[stateKey];

        // If the caller changed the color externally (e.g. reset), sync state
        if (!IsPopupOpen(popupId) &&
            (displayColor.R != color.R || displayColor.G != color.G ||
             displayColor.B != color.B || displayColor.A != color.A))
            _context.ColorPickerStates[stateKey] = color;

        // === Swatch trigger (label + color rectangle) ===
        BeginHorizontal(gap: 6);
        Label(ElementId.GetDisplayLabel(label).ToString(), new LabelStyle { FontSize = 12 });

        bool isHovered = IsHovered(swatchId);
        using (Clay.Element(new ElementDeclaration
        {
            Id = swatchId,
            Layout = new LayoutConfig { Sizing = Sizing.FixedSize(40, 18) },
            BackgroundColor = _context.ColorPickerStates[stateKey],
            CornerRadius = CornerRadius.All(3),
            Border = BorderConfig.Uniform(1, isHovered ? Color.Rgba(140, 140, 150) : Color.Rgba(80, 80, 80))
        })) { }

        EndHorizontal();

        // Open popup on left-click on the swatch
        if (isHovered && ShouldProcessClick)
        {
            // Position popup below the swatch
            var swatchData = Clay.GetElementData(swatchId);
            if (swatchData.Found)
            {
                var box = swatchData.BoundingBox;
                OpenPopupAt(popupId, new Vector2(box.X, box.Y + box.Height + 2));
            }
            else
            {
                OpenPopup(popupId);
            }
        }

        // === Popup with full color picker ===
        if (BeginPopup(popupId, new PopupStyle
        {
            MinWidth = 220,
            MaxWidth = 400,
            Padding = Padding.All(8),
            ContentGap = 6
        }))
        {
            var editColor = _context.ColorPickerStates[stateKey];
            var (h, s, v) = editColor.ToHsv();
            float alpha = editColor.A;
            bool changed = false;

            BeginHorizontal(gap: 8);

            // === SV Panel (single custom element — renderer draws the gradient) ===
            {
                var panelId = Id($"cpSV_{label}");
                var panelData = Clay.GetElementData(panelId);

                using (Clay.Element(new ElementDeclaration
                {
                    Id = panelId,
                    Layout = new LayoutConfig
                    {
                        Sizing = Sizing.FixedSize(SvPanelSize, SvPanelSize),
                    },
                    Custom = CustomConfig.Create(new HsvGradientData { Type = SaturationValue, Hue = h }),
                    Border = BorderConfig.Uniform(1, Color.Rgba(60, 60, 60))
                })) { }

                if (panelData.Found && _context.MousePressed)
                {
                    var mouse = _context.MousePosition;
                    var box = panelData.BoundingBox;
                    if (box.Contains(mouse))
                    {
                        s = Math.Clamp((mouse.X - box.X) / box.Width, 0f, 1f);
                        v = Math.Clamp(1f - (mouse.Y - box.Y) / box.Height, 0f, 1f);
                        changed = true;
                    }
                }
            }

            // === Hue Bar (single custom element — renderer draws the gradient) ===
            {
                var hueId = Id($"cpHue_{label}");
                var hueData = Clay.GetElementData(hueId);

                using (Clay.Element(new ElementDeclaration
                {
                    Id = hueId,
                    Layout = new LayoutConfig
                    {
                        Sizing = Sizing.FixedSize(HueBarWidth, HueBarHeight),
                    },
                    Custom = CustomConfig.Create(new HsvGradientData { Type = HueBar }),
                    Border = BorderConfig.Uniform(1, Color.Rgba(60, 60, 60))
                })) { }

                if (hueData.Found && _context.MousePressed)
                {
                    var mouse = _context.MousePosition;
                    var box = hueData.BoundingBox;
                    if (box.Contains(mouse))
                    {
                        h = Math.Clamp((mouse.Y - box.Y) / box.Height, 0f, 1f) * 360f;
                        changed = true;
                    }
                }
            }

            // === Preview swatch inside popup ===
            BeginVertical(gap: 4);
            Label("Current", new LabelStyle { FontSize = 10 });
            using (Clay.Element(new ElementDeclaration
            {
                Id = Id($"cpCur_{label}"),
                Layout = new LayoutConfig { Sizing = Sizing.FixedSize(40, 24) },
                BackgroundColor = changed ? Color.FromHsv(h, s, v, alpha) : editColor,
                CornerRadius = CornerRadius.All(3),
                Border = BorderConfig.Uniform(1, Color.Rgba(80, 80, 80))
            })) { }
            EndVertical();

            EndHorizontal();

            // === RGBA number inputs ===
            var result = changed ? Color.FromHsv(h, s, v, alpha) : editColor;

            var numStyle = new TextInputStyle
            {
                BackgroundColor = Color.Rgba(40, 40, 45),
                FocusedBackgroundColor = Color.Rgba(55, 55, 65),
                TextColor = Color.Rgba(220, 220, 220),
                CursorColor = Color.Rgba(100, 180, 255),
                SelectionColor = Color.Rgba(80, 130, 200, 120),
                CornerRadius = CornerRadius.All(3),
                Border = new BorderConfig { Width = BorderWidth.All(1), Color = Color.Rgba(70, 70, 80) },
                Padding = Padding.Symmetric(4, 3),
                FontId = 0,
                FontSize = 12,
                Sizing = new Sizing(SizingAxis.Fixed(42), SizingAxis.Default),
                CharFilter = TextInputFilters.DigitsOnly,
            };

            string rStr = ((int)result.R).ToString();
            string gStr = ((int)result.G).ToString();
            string bStr = ((int)result.B).ToString();
            string aStr = ((int)result.A).ToString();

            BeginHorizontal(gap: 4);
            Label("R", new LabelStyle { FontSize = 10 });
            bool rChanged = TextInput($"R##{label}_r", ref rStr, style: numStyle);
            Label("G", new LabelStyle { FontSize = 10 });
            bool gChanged = TextInput($"G##{label}_g", ref gStr, style: numStyle);
            Label("B", new LabelStyle { FontSize = 10 });
            bool bChanged = TextInput($"B##{label}_b", ref bStr, style: numStyle);
            Label("A", new LabelStyle { FontSize = 10 });
            bool aChanged = TextInput($"A##{label}_a", ref aStr, style: numStyle);
            EndHorizontal();

            bool rgbaChanged = rChanged || gChanged || bChanged || aChanged;
            changed |= rgbaChanged;

            // Update stored color if anything changed
            if (changed)
            {
                if (rgbaChanged)
                {
                    float r = int.TryParse(rStr, out var ri) ? Math.Clamp(ri, 0, 255) : result.R;
                    float g = int.TryParse(gStr, out var gi) ? Math.Clamp(gi, 0, 255) : result.G;
                    float b = int.TryParse(bStr, out var bi) ? Math.Clamp(bi, 0, 255) : result.B;
                    float a2 = int.TryParse(aStr, out var ai) ? Math.Clamp(ai, 0, 255) : result.A;
                    _context.ColorPickerStates[stateKey] = new Color(r, g, b, a2);
                }
                else
                {
                    _context.ColorPickerStates[stateKey] = Color.FromHsv(h, s, v, alpha);
                }
            }

            EndPopup();
        }

        // Return the current stored color (reflects live edits and persists after popup closes)
        return _context.ColorPickerStates[stateKey];
    }

    // ============ Style Editor Helpers ============

    private static Color StyleColorEditor(string label, Color color)
    {
        return ColorPicker(label, color);
    }

    private static float StyleFloatEditor(string label, float value, float min = 0, float max = 20)
    {
        Slider(label, ref value, min, max);
        return value;
    }

    private static ushort StyleUshortEditor(string label, ushort value, ushort min = 0, ushort max = 48)
    {
        float v = value;
        if (Slider(label, ref v, min, max))
            value = (ushort)v;
        return value;
    }

    private static CornerRadius StyleCornerRadiusEditor(string label, CornerRadius value)
    {
        float r = value.TopLeft;
        if (Slider(label, ref r, 0, 20))
            value = CornerRadius.All(r);
        return value;
    }

    /// <summary>
    /// Shows the debug window using internal open state.
    /// </summary>
    public static void ShowDebugWindow()
    {
        DebugWindow(ref _debugWindowOpen);
    }

    /// <summary>
    /// Toggles the debug window visibility.
    /// </summary>
    public static void ToggleDebugWindow()
    {
        _debugWindowOpen = !_debugWindowOpen;
    }

    /// <summary>
    /// Returns true if the debug window is currently open.
    /// </summary>
    public static bool IsDebugWindowOpen => _debugWindowOpen;
}

// ============ Context Handle ============

/// <summary>
/// Opaque handle to a complete ClayUI context (widget state, style, and layout context).
/// Obtain via <see cref="ClayUI.GetContext"/> and restore via <see cref="ClayUI.SetContext"/>.
/// </summary>
public sealed class ClayUIContextHandle
{
    internal ClayUIContext UIContext { get; }
    internal ClayUIStyle Style { get; }
    internal ClayContext? LayoutContext { get; }

    internal ClayUIContextHandle(ClayUIContext uiContext, ClayUIStyle style, ClayContext? layoutContext)
    {
        UIContext = uiContext;
        Style = style;
        LayoutContext = layoutContext;
    }
}

// ============ Style Definitions ============

/// <summary>
/// Complete UI style configuration.
/// </summary>
public class ClayUIStyle
{
    public ButtonStyle Button = new();
    public ImageStyle Image = new();
    public LabelStyle Label = new();
    public HeadingStyle Heading = new();
    public CheckboxStyle Checkbox = new();
    public SliderStyle Slider = new();
    public ToggleStyle Toggle = new();
    public ProgressBarStyle ProgressBar = new();
    public PanelStyle Panel = new();
    public TreeNodeStyle TreeNode = new();
    public ScrollAreaStyle ScrollArea = new();
    public RadioGroupStyle RadioGroup = new();
    public ScrollbarStyle Scrollbar = new();
    public ListBoxStyle ListBox = new();
    public ComboStyle Combo = new();
    public WindowStyle Window = new();
    public PopupStyle Popup = new();
    public ModalStyle Modal = new();
    public TooltipStyle Tooltip = new();
    public SplitterStyle Splitter = new();
    public DockSpaceStyle DockSpace = new();
    public Color SeparatorColor = Color.Rgba(60, 60, 65);

    public static ClayUIStyle Default => new();

    public static ClayUIStyle Dark => new()
    {
        Button = new ButtonStyle
        {
            BackgroundColor = Color.Rgba(72, 72, 72),
            HoverColor = Color.Rgba(88, 88, 88),
            PressedColor = Color.Rgba(60, 60, 60),
            TextColor = Color.Rgba(220, 220, 220),
            CornerRadius = CornerRadius.All(4)
        },
        Label = new LabelStyle
        {
            TextColor = Color.Rgba(200, 200, 200)
        },
        Heading = new HeadingStyle
        {
            TextColor = Color.Rgba(220, 220, 220)
        },
        Checkbox = new CheckboxStyle
        {
            BoxColor = Color.Rgba(56, 56, 56),
            CheckedColor = Color.Rgba(100, 150, 200),
            CheckmarkColor = Color.White,
            BoxBorderColor = Color.Rgba(90, 90, 90),
            TextColor = Color.Rgba(200, 200, 200),
            HoverColor = Color.Rgba(68, 68, 68),
            PressedColor = Color.Rgba(52, 52, 52)
        },
        Slider = new SliderStyle
        {
            TrackColor = Color.Rgba(56, 56, 56),
            FillColor = Color.Rgba(100, 150, 200),
            TextColor = Color.Rgba(200, 200, 200),
            ValueTextColor = Color.Rgba(150, 150, 150)
        },
        Toggle = new ToggleStyle
        {
            OnColor = Color.Rgba(100, 150, 200),
            OffColor = Color.Rgba(72, 72, 72),
            KnobColor = Color.Rgba(220, 220, 220),
            TextColor = Color.Rgba(200, 200, 200),
            HoverColor = Color.Rgba(64, 64, 64),
            PressedColor = Color.Rgba(56, 56, 56)
        },
        ProgressBar = new ProgressBarStyle
        {
            BackgroundColor = Color.Rgba(56, 56, 56),
            FillColor = Color.Rgba(100, 150, 200)
        },
        Panel = new PanelStyle
        {
            BackgroundColor = Color.Rgba(48, 48, 48),
            TitleColor = Color.Rgba(200, 200, 200),
            SeparatorColor = Color.Rgba(72, 72, 72),
            Border = BorderConfig.Uniform(1, Color.Rgba(64, 64, 64))
        },
        TreeNode = new TreeNodeStyle
        {
            TextColor = Color.Rgba(200, 200, 200),
            ArrowColor = Color.Rgba(150, 150, 150),
            HoverColor = Color.Rgba(64, 64, 64)
        },
        ScrollArea = new ScrollAreaStyle
        {
            BackgroundColor = Color.Rgba(42, 42, 42)
        },
        RadioGroup = new RadioGroupStyle
        {
            TextColor = Color.Rgba(200, 200, 200),
            LabelColor = Color.Rgba(150, 150, 150),
            CircleColor = Color.Rgba(56, 56, 56),
            CircleBorderColor = Color.Rgba(90, 90, 90),
            DotColor = Color.Rgba(100, 150, 200),
            HoverColor = Color.Rgba(68, 68, 68)
        },
        Scrollbar = new ScrollbarStyle
        {
            TrackColor = Color.Rgba(48, 48, 48),
            ThumbColor = Color.Rgba(88, 88, 88),
            ThumbHoverColor = Color.Rgba(108, 108, 108)
        },
        ListBox = new ListBoxStyle
        {
            BackgroundColor = Color.Rgba(42, 42, 42),
            TextColor = Color.Rgba(200, 200, 200),
            LabelColor = Color.Rgba(150, 150, 150),
            HoverColor = Color.Rgba(60, 60, 60),
            SelectedColor = Color.Rgba(100, 150, 200),
            SelectedTextColor = Color.White,
            Border = BorderConfig.Uniform(1, Color.Rgba(64, 64, 64))
        },
        Combo = new ComboStyle
        {
            BackgroundColor = Color.Rgba(56, 56, 56),
            HoverColor = Color.Rgba(68, 68, 68),
            TextColor = Color.Rgba(200, 200, 200),
            LabelColor = Color.Rgba(150, 150, 150),
            ArrowColor = Color.Rgba(150, 150, 150),
            SelectedColor = Color.Rgba(100, 150, 200),
            SelectedTextColor = Color.White,
            ItemHoverColor = Color.Rgba(68, 68, 68),
            Border = BorderConfig.Uniform(1, Color.Rgba(72, 72, 72))
        },
        Window = new WindowStyle
        {
            BackgroundColor = Color.Rgba(48, 48, 48),
            CornerRadius = CornerRadius.All(6),
            Border = BorderConfig.Uniform(1, Color.Rgba(72, 72, 72)),
            TitleBarColor = Color.Rgba(56, 56, 56),
            TitleBarDragColor = Color.Rgba(64, 64, 64),
            TitleColor = Color.Rgba(210, 210, 210),
            ButtonHoverColor = Color.Rgba(80, 80, 80),
            CloseButtonHoverColor = Color.Rgba(180, 60, 60),
            ResizeHandleColor = Color.Rgba(88, 88, 88),
            ResizeHandleHoverColor = Color.Rgba(120, 120, 120)
        },
        Popup = new PopupStyle
        {
            BackgroundColor = Color.Rgba(52, 52, 52),
            CornerRadius = CornerRadius.All(4),
            Border = BorderConfig.Uniform(1, Color.Rgba(72, 72, 72))
        },
        Modal = new ModalStyle
        {
            BackgroundColor = Color.Rgba(52, 52, 52),
            CornerRadius = CornerRadius.All(6),
            Border = BorderConfig.Uniform(1, Color.Rgba(80, 80, 80)),
            DimColor = Color.Rgba(0, 0, 0, 140),
            TitleBarColor = Color.Rgba(60, 60, 60),
            TitleColor = Color.Rgba(210, 210, 210)
        },
        Tooltip = new TooltipStyle
        {
            BackgroundColor = Color.Rgba(32, 32, 32),
            TextColor = Color.Rgba(210, 210, 210),
            Border = BorderConfig.Uniform(1, Color.Rgba(72, 72, 72))
        },
        DockSpace = new DockSpaceStyle
        {
            TabActiveColor = Color.Rgba(56, 56, 56),
            TabInactiveColor = Color.Rgba(40, 40, 40),
            TabHoverColor = Color.Rgba(64, 64, 64),
            TabTextColor = Color.Rgba(190, 190, 190),
            TabActiveTextColor = Color.Rgba(230, 230, 230),
            TabBarColor = Color.Rgba(36, 36, 36),
            SplitterColor = Color.Rgba(28, 28, 28),
            SplitterHoverColor = Color.Rgba(100, 150, 200),
            SplitterDragColor = Color.Rgba(100, 150, 200),
            ContentBackgroundColor = Color.Rgba(48, 48, 48),
            DropPreviewColor = Color.Rgba(100, 150, 200, 80)
        },
        SeparatorColor = Color.Rgba(72, 72, 72)
    };

    public static ClayUIStyle Light => new()
    {
        Button = new ButtonStyle
        {
            BackgroundColor = Color.Rgba(220, 220, 225),
            HoverColor = Color.Rgba(200, 200, 210),
            PressedColor = Color.Rgba(180, 180, 190),
            TextColor = Color.Rgba(30, 30, 35)
        },
        Label = new LabelStyle
        {
            TextColor = Color.Rgba(30, 30, 35)
        },
        Heading = new HeadingStyle
        {
            TextColor = Color.Rgba(30, 30, 35)
        },
        Checkbox = new CheckboxStyle
        {
            BoxColor = Color.Rgba(230, 230, 235),
            CheckedColor = Color.Rgba(70, 130, 200),
            CheckmarkColor = Color.White,
            BoxBorderColor = Color.Rgba(180, 180, 185),
            TextColor = Color.Rgba(30, 30, 35),
            HoverColor = Color.Rgba(210, 210, 215),
            PressedColor = Color.Rgba(200, 200, 205)
        },
        Slider = new SliderStyle
        {
            TrackColor = Color.Rgba(210, 210, 215),
            FillColor = Color.Rgba(70, 130, 200),
            TextColor = Color.Rgba(30, 30, 35),
            ValueTextColor = Color.Rgba(100, 100, 105)
        },
        Toggle = new ToggleStyle
        {
            OnColor = Color.Rgba(70, 130, 200),
            OffColor = Color.Rgba(200, 200, 205),
            KnobColor = Color.White,
            TextColor = Color.Rgba(30, 30, 35),
            HoverColor = Color.Rgba(210, 210, 215),
            PressedColor = Color.Rgba(200, 200, 205)
        },
        ProgressBar = new ProgressBarStyle
        {
            BackgroundColor = Color.Rgba(210, 210, 215),
            FillColor = Color.Rgba(70, 130, 200)
        },
        Panel = new PanelStyle
        {
            BackgroundColor = Color.Rgba(245, 245, 250),
            TitleColor = Color.Rgba(30, 30, 35),
            SeparatorColor = Color.Rgba(200, 200, 205),
            Border = BorderConfig.Uniform(1, Color.Rgba(210, 210, 215))
        },
        TreeNode = new TreeNodeStyle
        {
            TextColor = Color.Rgba(30, 30, 35),
            ArrowColor = Color.Rgba(100, 100, 105),
            HoverColor = Color.Rgba(220, 220, 225)
        },
        ScrollArea = new ScrollAreaStyle
        {
            BackgroundColor = Color.Rgba(235, 235, 240)
        },
        RadioGroup = new RadioGroupStyle
        {
            TextColor = Color.Rgba(30, 30, 35),
            LabelColor = Color.Rgba(100, 100, 105),
            CircleColor = Color.Rgba(230, 230, 235),
            CircleBorderColor = Color.Rgba(180, 180, 185),
            DotColor = Color.Rgba(70, 130, 200),
            HoverColor = Color.Rgba(210, 210, 215)
        },
        Scrollbar = new ScrollbarStyle
        {
            TrackColor = Color.Rgba(230, 230, 235),
            ThumbColor = Color.Rgba(180, 180, 190),
            ThumbHoverColor = Color.Rgba(150, 150, 160)
        },
        ListBox = new ListBoxStyle
        {
            BackgroundColor = Color.Rgba(240, 240, 245),
            TextColor = Color.Rgba(30, 30, 35),
            LabelColor = Color.Rgba(100, 100, 105),
            HoverColor = Color.Rgba(220, 220, 225),
            SelectedColor = Color.Rgba(70, 130, 200),
            SelectedTextColor = Color.White,
            Border = BorderConfig.Uniform(1, Color.Rgba(200, 200, 205))
        },
        Combo = new ComboStyle
        {
            BackgroundColor = Color.Rgba(240, 240, 245),
            HoverColor = Color.Rgba(225, 225, 230),
            TextColor = Color.Rgba(30, 30, 35),
            LabelColor = Color.Rgba(100, 100, 105),
            ArrowColor = Color.Rgba(100, 100, 105),
            SelectedColor = Color.Rgba(70, 130, 200),
            SelectedTextColor = Color.White,
            ItemHoverColor = Color.Rgba(220, 220, 225),
            Border = BorderConfig.Uniform(1, Color.Rgba(200, 200, 205))
        },
        Window = new WindowStyle
        {
            BackgroundColor = Color.Rgba(245, 245, 250),
            CornerRadius = CornerRadius.All(8),
            Border = BorderConfig.Uniform(1, Color.Rgba(200, 200, 205)),
            TitleBarColor = Color.Rgba(230, 230, 235),
            TitleBarDragColor = Color.Rgba(220, 220, 225),
            TitleColor = Color.Rgba(30, 30, 35),
            ButtonHoverColor = Color.Rgba(210, 210, 215),
            CloseButtonHoverColor = Color.Rgba(200, 60, 60),
            ResizeHandleColor = Color.Rgba(180, 180, 190),
            ResizeHandleHoverColor = Color.Rgba(140, 140, 150)
        },
        Popup = new PopupStyle
        {
            BackgroundColor = Color.Rgba(250, 250, 255),
            CornerRadius = CornerRadius.All(4),
            Border = BorderConfig.Uniform(1, Color.Rgba(200, 200, 205))
        },
        Modal = new ModalStyle
        {
            BackgroundColor = Color.Rgba(250, 250, 255),
            CornerRadius = CornerRadius.All(6),
            Border = BorderConfig.Uniform(1, Color.Rgba(200, 200, 205)),
            DimColor = Color.Rgba(0, 0, 0, 100),
            TitleBarColor = Color.Rgba(230, 230, 238),
            TitleColor = Color.Rgba(30, 30, 35)
        },
        Tooltip = new TooltipStyle
        {
            BackgroundColor = Color.Rgba(50, 50, 55),
            TextColor = Color.Rgba(240, 240, 245),
            Border = BorderConfig.Uniform(1, Color.Rgba(80, 80, 90))
        },
        DockSpace = new DockSpaceStyle
        {
            TabActiveColor = Color.Rgba(255, 255, 255),
            TabInactiveColor = Color.Rgba(235, 235, 240),
            TabHoverColor = Color.Rgba(245, 245, 250),
            TabTextColor = Color.Rgba(80, 80, 85),
            TabActiveTextColor = Color.Rgba(30, 30, 35),
            TabBarColor = Color.Rgba(225, 225, 230),
            SplitterColor = Color.Rgba(210, 210, 215),
            SplitterHoverColor = Color.Rgba(100, 150, 200),
            SplitterDragColor = Color.Rgba(100, 150, 200),
            ContentBackgroundColor = Color.Rgba(250, 250, 255),
            DropPreviewColor = Color.Rgba(100, 150, 200, 60)
        },
        SeparatorColor = Color.Rgba(200, 200, 205)
    };
}

public struct ButtonStyle
{
    public ButtonStyle() { }
    private uint _set;

    private Color _backgroundColor = Color.Rgba(55, 55, 65);
    public Color BackgroundColor { get => _backgroundColor; set { _backgroundColor = value; _set |= 1u << 0; } }

    private Color _hoverColor = Color.Rgba(70, 70, 80);
    public Color HoverColor { get => _hoverColor; set { _hoverColor = value; _set |= 1u << 1; } }

    private Color _pressedColor = Color.Rgba(50, 100, 180);
    public Color PressedColor { get => _pressedColor; set { _pressedColor = value; _set |= 1u << 2; } }

    private Color _textColor = Color.White;
    public Color TextColor { get => _textColor; set { _textColor = value; _set |= 1u << 3; } }

    private Padding _padding = Padding.Symmetric(16, 10);
    public Padding Padding { get => _padding; set { _padding = value; _set |= 1u << 4; } }

    private CornerRadius _cornerRadius = CornerRadius.All(6);
    public CornerRadius CornerRadius { get => _cornerRadius; set { _cornerRadius = value; _set |= 1u << 5; } }

    private ushort _fontId = 0;
    public ushort FontId { get => _fontId; set { _fontId = value; _set |= 1u << 6; } }

    private ushort _fontSize = 14;
    public ushort FontSize { get => _fontSize; set { _fontSize = value; _set |= 1u << 7; } }

    private Sizing _sizing;
    public Sizing Sizing { get => _sizing; set { _sizing = value; _set |= 1u << 8; } }

    private ushort _hoverFontSize;
    public ushort HoverFontSize { get => _hoverFontSize; set { _hoverFontSize = value; _set |= 1u << 9; } }

    private Color _hoverTextColor;
    public Color HoverTextColor { get => _hoverTextColor; set { _hoverTextColor = value; _set |= 1u << 10; } }

    public ButtonStyle MergeOver(ButtonStyle @base)
    {
        var result = @base;
        if ((_set & (1u << 0)) != 0) result._backgroundColor = _backgroundColor;
        if ((_set & (1u << 1)) != 0) result._hoverColor = _hoverColor;
        if ((_set & (1u << 2)) != 0) result._pressedColor = _pressedColor;
        if ((_set & (1u << 3)) != 0) result._textColor = _textColor;
        if ((_set & (1u << 4)) != 0) result._padding = _padding;
        if ((_set & (1u << 5)) != 0) result._cornerRadius = _cornerRadius;
        if ((_set & (1u << 6)) != 0) result._fontId = _fontId;
        if ((_set & (1u << 7)) != 0) result._fontSize = _fontSize;
        if ((_set & (1u << 8)) != 0) result._sizing = _sizing;
        if ((_set & (1u << 9)) != 0) result._hoverFontSize = _hoverFontSize;
        if ((_set & (1u << 10)) != 0) result._hoverTextColor = _hoverTextColor;
        result._set = @base._set | _set;
        return result;
    }

    internal bool HasSizing => (_set & (1u << 8)) != 0;
    internal bool HasHoverFontSize => (_set & (1u << 9)) != 0;
    internal bool HasHoverTextColor => (_set & (1u << 10)) != 0;
}

public struct ImageStyle
{
    public ImageStyle() { }
    private uint _set;

    private CornerRadius _cornerRadius = CornerRadius.Zero;
    public CornerRadius CornerRadius { get => _cornerRadius; set { _cornerRadius = value; _set |= 1u << 0; } }

    private BorderConfig _border = default;
    public BorderConfig Border { get => _border; set { _border = value; _set |= 1u << 1; } }

    private BorderConfig _hoverBorder = BorderConfig.Uniform(1, Color.Rgba(100, 150, 255));
    public BorderConfig HoverBorder { get => _hoverBorder; set { _hoverBorder = value; _set |= 1u << 2; } }

    private Color _hoverTint = Color.Rgba(255, 255, 255, 30);
    public Color HoverTint { get => _hoverTint; set { _hoverTint = value; _set |= 1u << 3; } }

    private Color _pressedTint = Color.Rgba(0, 0, 0, 40);
    public Color PressedTint { get => _pressedTint; set { _pressedTint = value; _set |= 1u << 4; } }

    private Padding _padding = Padding.Zero;
    public Padding Padding { get => _padding; set { _padding = value; _set |= 1u << 5; } }

    public ImageStyle MergeOver(ImageStyle @base)
    {
        var result = @base;
        if ((_set & (1u << 0)) != 0) result._cornerRadius = _cornerRadius;
        if ((_set & (1u << 1)) != 0) result._border = _border;
        if ((_set & (1u << 2)) != 0) result._hoverBorder = _hoverBorder;
        if ((_set & (1u << 3)) != 0) result._hoverTint = _hoverTint;
        if ((_set & (1u << 4)) != 0) result._pressedTint = _pressedTint;
        if ((_set & (1u << 5)) != 0) result._padding = _padding;
        return result;
    }
}

public struct LabelStyle
{
    public LabelStyle() { }
    private uint _set;

    private Color _textColor = Color.Rgba(200, 200, 200);
    public Color TextColor { get => _textColor; set { _textColor = value; _set |= 1u << 0; } }

    private ushort _fontId = 0;
    public ushort FontId { get => _fontId; set { _fontId = value; _set |= 1u << 1; } }

    private ushort _fontSize = 14;
    public ushort FontSize { get => _fontSize; set { _fontSize = value; _set |= 1u << 2; } }

    private ushort _lineHeight = 20;
    public ushort LineHeight { get => _lineHeight; set { _lineHeight = value; _set |= 1u << 3; } }

    public LabelStyle MergeOver(LabelStyle @base)
    {
        var result = @base;
        if ((_set & (1u << 0)) != 0) result._textColor = _textColor;
        if ((_set & (1u << 1)) != 0) result._fontId = _fontId;
        if ((_set & (1u << 2)) != 0) result._fontSize = _fontSize;
        if ((_set & (1u << 3)) != 0) result._lineHeight = _lineHeight;
        return result;
    }
}

public struct HeadingStyle
{
    public HeadingStyle() { }
    private uint _set;

    private Color _textColor = Color.White;
    public Color TextColor { get => _textColor; set { _textColor = value; _set |= 1u << 0; } }

    private ushort _fontId = 0;
    public ushort FontId { get => _fontId; set { _fontId = value; _set |= 1u << 1; } }

    private ushort _fontSize = 20;
    public ushort FontSize { get => _fontSize; set { _fontSize = value; _set |= 1u << 2; } }

    public HeadingStyle MergeOver(HeadingStyle @base)
    {
        var result = @base;
        if ((_set & (1u << 0)) != 0) result._textColor = _textColor;
        if ((_set & (1u << 1)) != 0) result._fontId = _fontId;
        if ((_set & (1u << 2)) != 0) result._fontSize = _fontSize;
        return result;
    }
}

public struct CheckboxStyle
{
    public CheckboxStyle() { }
    private uint _set;

    private Color _boxColor = Color.Rgba(45, 45, 50);
    public Color BoxColor { get => _boxColor; set { _boxColor = value; _set |= 1u << 0; } }

    private Color _checkedColor = Color.Rgba(70, 130, 200);
    public Color CheckedColor { get => _checkedColor; set { _checkedColor = value; _set |= 1u << 1; } }

    private Color _checkmarkColor = Color.White;
    public Color CheckmarkColor { get => _checkmarkColor; set { _checkmarkColor = value; _set |= 1u << 2; } }

    private Color _boxBorderColor = Color.Rgba(80, 80, 85);
    public Color BoxBorderColor { get => _boxBorderColor; set { _boxBorderColor = value; _set |= 1u << 3; } }

    private Color _textColor = Color.Rgba(200, 200, 200);
    public Color TextColor { get => _textColor; set { _textColor = value; _set |= 1u << 4; } }

    private Color _hoverColor = Color.Rgba(50, 50, 55);
    public Color HoverColor { get => _hoverColor; set { _hoverColor = value; _set |= 1u << 5; } }

    private Color _pressedColor = Color.Rgba(40, 40, 45);
    public Color PressedColor { get => _pressedColor; set { _pressedColor = value; _set |= 1u << 6; } }

    private Padding _padding = Padding.Symmetric(4, 4);
    public Padding Padding { get => _padding; set { _padding = value; _set |= 1u << 7; } }

    private float _boxSize = 18;
    public float BoxSize { get => _boxSize; set { _boxSize = value; _set |= 1u << 8; } }

    private float _boxCornerRadius = 4;
    public float BoxCornerRadius { get => _boxCornerRadius; set { _boxCornerRadius = value; _set |= 1u << 9; } }

    private ushort _fontId = 0;
    public ushort FontId { get => _fontId; set { _fontId = value; _set |= 1u << 10; } }

    private ushort _fontSize = 14;
    public ushort FontSize { get => _fontSize; set { _fontSize = value; _set |= 1u << 11; } }

    public CheckboxStyle MergeOver(CheckboxStyle @base)
    {
        var result = @base;
        if ((_set & (1u << 0)) != 0) result._boxColor = _boxColor;
        if ((_set & (1u << 1)) != 0) result._checkedColor = _checkedColor;
        if ((_set & (1u << 2)) != 0) result._checkmarkColor = _checkmarkColor;
        if ((_set & (1u << 3)) != 0) result._boxBorderColor = _boxBorderColor;
        if ((_set & (1u << 4)) != 0) result._textColor = _textColor;
        if ((_set & (1u << 5)) != 0) result._hoverColor = _hoverColor;
        if ((_set & (1u << 6)) != 0) result._pressedColor = _pressedColor;
        if ((_set & (1u << 7)) != 0) result._padding = _padding;
        if ((_set & (1u << 8)) != 0) result._boxSize = _boxSize;
        if ((_set & (1u << 9)) != 0) result._boxCornerRadius = _boxCornerRadius;
        if ((_set & (1u << 10)) != 0) result._fontId = _fontId;
        if ((_set & (1u << 11)) != 0) result._fontSize = _fontSize;
        return result;
    }
}

public struct SliderStyle
{
    public SliderStyle() { }
    private uint _set;

    private Color _trackColor = Color.Rgba(45, 45, 50);
    public Color TrackColor { get => _trackColor; set { _trackColor = value; _set |= 1u << 0; } }

    private Color _fillColor = Color.Rgba(70, 130, 200);
    public Color FillColor { get => _fillColor; set { _fillColor = value; _set |= 1u << 1; } }

    private Color _textColor = Color.Rgba(200, 200, 200);
    public Color TextColor { get => _textColor; set { _textColor = value; _set |= 1u << 2; } }

    private Color _valueTextColor = Color.Rgba(150, 150, 155);
    public Color ValueTextColor { get => _valueTextColor; set { _valueTextColor = value; _set |= 1u << 3; } }

    private float _trackHeight = 8;
    public float TrackHeight { get => _trackHeight; set { _trackHeight = value; _set |= 1u << 4; } }

    private ushort _fontId = 0;
    public ushort FontId { get => _fontId; set { _fontId = value; _set |= 1u << 5; } }

    private ushort _fontSize = 14;
    public ushort FontSize { get => _fontSize; set { _fontSize = value; _set |= 1u << 6; } }

    public SliderStyle MergeOver(SliderStyle @base)
    {
        var result = @base;
        if ((_set & (1u << 0)) != 0) result._trackColor = _trackColor;
        if ((_set & (1u << 1)) != 0) result._fillColor = _fillColor;
        if ((_set & (1u << 2)) != 0) result._textColor = _textColor;
        if ((_set & (1u << 3)) != 0) result._valueTextColor = _valueTextColor;
        if ((_set & (1u << 4)) != 0) result._trackHeight = _trackHeight;
        if ((_set & (1u << 5)) != 0) result._fontId = _fontId;
        if ((_set & (1u << 6)) != 0) result._fontSize = _fontSize;
        return result;
    }
}

public struct ToggleStyle
{
    public ToggleStyle() { }
    private uint _set;

    private Color _onColor = Color.Rgba(70, 130, 200);
    public Color OnColor { get => _onColor; set { _onColor = value; _set |= 1u << 0; } }

    private Color _offColor = Color.Rgba(60, 60, 65);
    public Color OffColor { get => _offColor; set { _offColor = value; _set |= 1u << 1; } }

    private Color _knobColor = Color.White;
    public Color KnobColor { get => _knobColor; set { _knobColor = value; _set |= 1u << 2; } }

    private Color _textColor = Color.Rgba(200, 200, 200);
    public Color TextColor { get => _textColor; set { _textColor = value; _set |= 1u << 3; } }

    private Color _hoverColor = Color.Rgba(50, 50, 55);
    public Color HoverColor { get => _hoverColor; set { _hoverColor = value; _set |= 1u << 4; } }

    private Color _pressedColor = Color.Rgba(40, 40, 45);
    public Color PressedColor { get => _pressedColor; set { _pressedColor = value; _set |= 1u << 5; } }

    private Padding _padding = Padding.Symmetric(4, 4);
    public Padding Padding { get => _padding; set { _padding = value; _set |= 1u << 6; } }

    private float _trackWidth = 44;
    public float TrackWidth { get => _trackWidth; set { _trackWidth = value; _set |= 1u << 7; } }

    private float _trackHeight = 24;
    public float TrackHeight { get => _trackHeight; set { _trackHeight = value; _set |= 1u << 8; } }

    private float _knobSize = 20;
    public float KnobSize { get => _knobSize; set { _knobSize = value; _set |= 1u << 9; } }

    private ushort _fontId = 0;
    public ushort FontId { get => _fontId; set { _fontId = value; _set |= 1u << 10; } }

    private ushort _fontSize = 14;
    public ushort FontSize { get => _fontSize; set { _fontSize = value; _set |= 1u << 11; } }

    public ToggleStyle MergeOver(ToggleStyle @base)
    {
        var result = @base;
        if ((_set & (1u << 0)) != 0) result._onColor = _onColor;
        if ((_set & (1u << 1)) != 0) result._offColor = _offColor;
        if ((_set & (1u << 2)) != 0) result._knobColor = _knobColor;
        if ((_set & (1u << 3)) != 0) result._textColor = _textColor;
        if ((_set & (1u << 4)) != 0) result._hoverColor = _hoverColor;
        if ((_set & (1u << 5)) != 0) result._pressedColor = _pressedColor;
        if ((_set & (1u << 6)) != 0) result._padding = _padding;
        if ((_set & (1u << 7)) != 0) result._trackWidth = _trackWidth;
        if ((_set & (1u << 8)) != 0) result._trackHeight = _trackHeight;
        if ((_set & (1u << 9)) != 0) result._knobSize = _knobSize;
        if ((_set & (1u << 10)) != 0) result._fontId = _fontId;
        if ((_set & (1u << 11)) != 0) result._fontSize = _fontSize;
        return result;
    }
}

public struct ProgressBarStyle
{
    public ProgressBarStyle() { }
    private uint _set;

    private Color _backgroundColor = Color.Rgba(45, 45, 50);
    public Color BackgroundColor { get => _backgroundColor; set { _backgroundColor = value; _set |= 1u << 0; } }

    private Color _fillColor = Color.Rgba(70, 130, 200);
    public Color FillColor { get => _fillColor; set { _fillColor = value; _set |= 1u << 1; } }

    private float _height = 8;
    public float Height { get => _height; set { _height = value; _set |= 1u << 2; } }

    private float _cornerRadius = 4;
    public float CornerRadius { get => _cornerRadius; set { _cornerRadius = value; _set |= 1u << 3; } }

    public ProgressBarStyle MergeOver(ProgressBarStyle @base)
    {
        var result = @base;
        if ((_set & (1u << 0)) != 0) result._backgroundColor = _backgroundColor;
        if ((_set & (1u << 1)) != 0) result._fillColor = _fillColor;
        if ((_set & (1u << 2)) != 0) result._height = _height;
        if ((_set & (1u << 3)) != 0) result._cornerRadius = _cornerRadius;
        return result;
    }
}

public struct PanelStyle
{
    public PanelStyle() { }
    private uint _set;

    private Color _backgroundColor = Color.Rgba(40, 40, 45);
    public Color BackgroundColor { get => _backgroundColor; set { _backgroundColor = value; _set |= 1u << 0; } }

    private Color _titleColor = Color.White;
    public Color TitleColor { get => _titleColor; set { _titleColor = value; _set |= 1u << 1; } }

    private Color _separatorColor = Color.Rgba(60, 60, 65);
    public Color SeparatorColor { get => _separatorColor; set { _separatorColor = value; _set |= 1u << 2; } }

    private Padding _padding = Padding.All(16);
    public Padding Padding { get => _padding; set { _padding = value; _set |= 1u << 3; } }

    private CornerRadius _cornerRadius = CornerRadius.All(8);
    public CornerRadius CornerRadius { get => _cornerRadius; set { _cornerRadius = value; _set |= 1u << 4; } }

    private BorderConfig _border = BorderConfig.Uniform(1, Color.Rgba(55, 55, 60));
    public BorderConfig Border { get => _border; set { _border = value; _set |= 1u << 5; } }

    private ushort _childGap = 12;
    public ushort ChildGap { get => _childGap; set { _childGap = value; _set |= 1u << 6; } }

    private ushort _titleFontId = 0;
    public ushort TitleFontId { get => _titleFontId; set { _titleFontId = value; _set |= 1u << 7; } }

    private ushort _titleFontSize = 16;
    public ushort TitleFontSize { get => _titleFontSize; set { _titleFontSize = value; _set |= 1u << 8; } }

    private ShadowConfig _shadow = default;
    public ShadowConfig Shadow { get => _shadow; set { _shadow = value; _set |= 1u << 9; } }

    public PanelStyle MergeOver(PanelStyle @base)
    {
        var result = @base;
        if ((_set & (1u << 0)) != 0) result._backgroundColor = _backgroundColor;
        if ((_set & (1u << 1)) != 0) result._titleColor = _titleColor;
        if ((_set & (1u << 2)) != 0) result._separatorColor = _separatorColor;
        if ((_set & (1u << 3)) != 0) result._padding = _padding;
        if ((_set & (1u << 4)) != 0) result._cornerRadius = _cornerRadius;
        if ((_set & (1u << 5)) != 0) result._border = _border;
        if ((_set & (1u << 6)) != 0) result._childGap = _childGap;
        if ((_set & (1u << 7)) != 0) result._titleFontId = _titleFontId;
        if ((_set & (1u << 8)) != 0) result._titleFontSize = _titleFontSize;
        if ((_set & (1u << 9)) != 0) result._shadow = _shadow;
        return result;
    }
}

public struct TreeNodeStyle
{
    public TreeNodeStyle() { }
    private uint _set;

    private Color _textColor = Color.Rgba(200, 200, 200);
    public Color TextColor { get => _textColor; set { _textColor = value; _set |= 1u << 0; } }

    private Color _arrowColor = Color.Rgba(150, 150, 155);
    public Color ArrowColor { get => _arrowColor; set { _arrowColor = value; _set |= 1u << 1; } }

    private Color _hoverColor = Color.Rgba(50, 50, 55);
    public Color HoverColor { get => _hoverColor; set { _hoverColor = value; _set |= 1u << 2; } }

    private Padding _padding = Padding.Symmetric(4, 4);
    public Padding Padding { get => _padding; set { _padding = value; _set |= 1u << 3; } }

    private string _expandedIcon = "v";
    public string ExpandedIcon { get => _expandedIcon; set { _expandedIcon = value; _set |= 1u << 4; } }

    private string _collapsedIcon = ">";
    public string CollapsedIcon { get => _collapsedIcon; set { _collapsedIcon = value; _set |= 1u << 5; } }

    private ushort _indentSize = 20;
    public ushort IndentSize { get => _indentSize; set { _indentSize = value; _set |= 1u << 6; } }

    private bool _defaultExpanded = false;
    public bool DefaultExpanded { get => _defaultExpanded; set { _defaultExpanded = value; _set |= 1u << 7; } }

    private ushort _fontId = 0;
    public ushort FontId { get => _fontId; set { _fontId = value; _set |= 1u << 8; } }

    private ushort _fontSize = 14;
    public ushort FontSize { get => _fontSize; set { _fontSize = value; _set |= 1u << 9; } }

    public TreeNodeStyle MergeOver(TreeNodeStyle @base)
    {
        var result = @base;
        if ((_set & (1u << 0)) != 0) result._textColor = _textColor;
        if ((_set & (1u << 1)) != 0) result._arrowColor = _arrowColor;
        if ((_set & (1u << 2)) != 0) result._hoverColor = _hoverColor;
        if ((_set & (1u << 3)) != 0) result._padding = _padding;
        if ((_set & (1u << 4)) != 0) result._expandedIcon = _expandedIcon;
        if ((_set & (1u << 5)) != 0) result._collapsedIcon = _collapsedIcon;
        if ((_set & (1u << 6)) != 0) result._indentSize = _indentSize;
        if ((_set & (1u << 7)) != 0) result._defaultExpanded = _defaultExpanded;
        if ((_set & (1u << 8)) != 0) result._fontId = _fontId;
        if ((_set & (1u << 9)) != 0) result._fontSize = _fontSize;
        return result;
    }
}

/// <summary>
/// Optional visual style for BeginHorizontal/BeginVertical layout containers.
/// </summary>
public struct LayoutStyle
{
    public LayoutStyle() { }
    private uint _set;

    private Color _backgroundColor = Color.Transparent;
    public Color BackgroundColor { get => _backgroundColor; set { _backgroundColor = value; _set |= 1u << 0; } }

    private CornerRadius _cornerRadius = default;
    public CornerRadius CornerRadius { get => _cornerRadius; set { _cornerRadius = value; _set |= 1u << 1; } }

    private BorderConfig _border = default;
    public BorderConfig Border { get => _border; set { _border = value; _set |= 1u << 2; } }

    private Padding _padding = default;
    public Padding Padding { get => _padding; set { _padding = value; _set |= 1u << 3; } }

    private Sizing _sizing = default;
    public Sizing Sizing { get => _sizing; set { _sizing = value; _set |= 1u << 4; } }

    /// <summary>
    /// When true, children are clipped to this element's bounding box.
    /// </summary>
    private bool _clipContent = false;
    public bool ClipContent { get => _clipContent; set { _clipContent = value; _set |= 1u << 5; } }

    private ShadowConfig _shadow = default;
    public ShadowConfig Shadow { get => _shadow; set { _shadow = value; _set |= 1u << 6; } }

    public LayoutStyle MergeOver(LayoutStyle @base)
    {
        var result = @base;
        if ((_set & (1u << 0)) != 0) result._backgroundColor = _backgroundColor;
        if ((_set & (1u << 1)) != 0) result._cornerRadius = _cornerRadius;
        if ((_set & (1u << 2)) != 0) result._border = _border;
        if ((_set & (1u << 3)) != 0) result._padding = _padding;
        if ((_set & (1u << 4)) != 0) result._sizing = _sizing;
        if ((_set & (1u << 5)) != 0) result._clipContent = _clipContent;
        if ((_set & (1u << 6)) != 0) result._shadow = _shadow;
        return result;
    }
}

public struct ScrollAreaStyle
{
    public ScrollAreaStyle() { }
    private uint _set;

    private Color _backgroundColor = Color.Rgba(35, 35, 40);
    public Color BackgroundColor { get => _backgroundColor; set { _backgroundColor = value; _set |= 1u << 0; } }

    private Padding _padding = Padding.All(8);
    public Padding Padding { get => _padding; set { _padding = value; _set |= 1u << 1; } }

    private CornerRadius _cornerRadius = CornerRadius.All(4);
    public CornerRadius CornerRadius { get => _cornerRadius; set { _cornerRadius = value; _set |= 1u << 2; } }

    private ShadowConfig _shadow = default;
    public ShadowConfig Shadow { get => _shadow; set { _shadow = value; _set |= 1u << 3; } }

    public ScrollAreaStyle MergeOver(ScrollAreaStyle @base)
    {
        var result = @base;
        if ((_set & (1u << 0)) != 0) result._backgroundColor = _backgroundColor;
        if ((_set & (1u << 1)) != 0) result._padding = _padding;
        if ((_set & (1u << 2)) != 0) result._cornerRadius = _cornerRadius;
        if ((_set & (1u << 3)) != 0) result._shadow = _shadow;
        return result;
    }
}

public struct SplitterStyle
{
    public SplitterStyle() { }
    private uint _set;

    /// <summary>Thickness of the splitter handle in pixels.</summary>
    private float _thickness = 5;
    public float Thickness { get => _thickness; set { _thickness = value; _set |= 1u << 0; } }

    /// <summary>Background color when idle (transparent = invisible until hovered).</summary>
    private Color _backgroundColor = Color.Rgba(0, 0, 0, 0);
    public Color BackgroundColor { get => _backgroundColor; set { _backgroundColor = value; _set |= 1u << 1; } }

    /// <summary>Background color when the mouse is hovering over the splitter.</summary>
    private Color _hoverColor = Color.Rgba(60, 140, 230, 100);
    public Color HoverColor { get => _hoverColor; set { _hoverColor = value; _set |= 1u << 2; } }

    /// <summary>Background color while the splitter is being dragged.</summary>
    private Color _dragColor = Color.Rgba(60, 140, 230, 180);
    public Color DragColor { get => _dragColor; set { _dragColor = value; _set |= 1u << 3; } }

    public SplitterStyle MergeOver(SplitterStyle @base)
    {
        var result = @base;
        if ((_set & (1u << 0)) != 0) result._thickness = _thickness;
        if ((_set & (1u << 1)) != 0) result._backgroundColor = _backgroundColor;
        if ((_set & (1u << 2)) != 0) result._hoverColor = _hoverColor;
        if ((_set & (1u << 3)) != 0) result._dragColor = _dragColor;
        return result;
    }
}

public struct RadioGroupStyle
{
    public RadioGroupStyle() { }
    private uint _set;

    private Color _textColor = Color.Rgba(200, 200, 200);
    public Color TextColor { get => _textColor; set { _textColor = value; _set |= 1u << 0; } }

    private Color _labelColor = Color.Rgba(150, 150, 155);
    public Color LabelColor { get => _labelColor; set { _labelColor = value; _set |= 1u << 1; } }

    private Color _circleColor = Color.Rgba(45, 45, 50);
    public Color CircleColor { get => _circleColor; set { _circleColor = value; _set |= 1u << 2; } }

    private Color _circleBorderColor = Color.Rgba(80, 80, 85);
    public Color CircleBorderColor { get => _circleBorderColor; set { _circleBorderColor = value; _set |= 1u << 3; } }

    private Color _dotColor = Color.Rgba(70, 130, 200);
    public Color DotColor { get => _dotColor; set { _dotColor = value; _set |= 1u << 4; } }

    private Color _hoverColor = Color.Rgba(50, 50, 55);
    public Color HoverColor { get => _hoverColor; set { _hoverColor = value; _set |= 1u << 5; } }

    private Padding _optionPadding = Padding.Symmetric(4, 4);
    public Padding OptionPadding { get => _optionPadding; set { _optionPadding = value; _set |= 1u << 6; } }

    private float _circleSize = 18;
    public float CircleSize { get => _circleSize; set { _circleSize = value; _set |= 1u << 7; } }

    private float _dotSize = 10;
    public float DotSize { get => _dotSize; set { _dotSize = value; _set |= 1u << 8; } }

    private ushort _fontId = 0;
    public ushort FontId { get => _fontId; set { _fontId = value; _set |= 1u << 9; } }

    private ushort _fontSize = 14;
    public ushort FontSize { get => _fontSize; set { _fontSize = value; _set |= 1u << 10; } }

    public RadioGroupStyle MergeOver(RadioGroupStyle @base)
    {
        var result = @base;
        if ((_set & (1u << 0)) != 0) result._textColor = _textColor;
        if ((_set & (1u << 1)) != 0) result._labelColor = _labelColor;
        if ((_set & (1u << 2)) != 0) result._circleColor = _circleColor;
        if ((_set & (1u << 3)) != 0) result._circleBorderColor = _circleBorderColor;
        if ((_set & (1u << 4)) != 0) result._dotColor = _dotColor;
        if ((_set & (1u << 5)) != 0) result._hoverColor = _hoverColor;
        if ((_set & (1u << 6)) != 0) result._optionPadding = _optionPadding;
        if ((_set & (1u << 7)) != 0) result._circleSize = _circleSize;
        if ((_set & (1u << 8)) != 0) result._dotSize = _dotSize;
        if ((_set & (1u << 9)) != 0) result._fontId = _fontId;
        if ((_set & (1u << 10)) != 0) result._fontSize = _fontSize;
        return result;
    }
}

public struct ScrollbarStyle
{
    public ScrollbarStyle() { }
    private uint _set;

    private Color _trackColor = Color.Rgba(40, 40, 45);
    public Color TrackColor { get => _trackColor; set { _trackColor = value; _set |= 1u << 0; } }

    private Color _thumbColor = Color.Rgba(80, 80, 90);
    public Color ThumbColor { get => _thumbColor; set { _thumbColor = value; _set |= 1u << 1; } }

    private Color _thumbHoverColor = Color.Rgba(100, 100, 110);
    public Color ThumbHoverColor { get => _thumbHoverColor; set { _thumbHoverColor = value; _set |= 1u << 2; } }

    private float _width = 8;
    public float Width { get => _width; set { _width = value; _set |= 1u << 3; } }

    private float _minThumbSize = 20;
    public float MinThumbSize { get => _minThumbSize; set { _minThumbSize = value; _set |= 1u << 4; } }

    private float _trackPadding = 2;
    public float TrackPadding { get => _trackPadding; set { _trackPadding = value; _set |= 1u << 5; } }

    private CornerRadius _cornerRadius = CornerRadius.All(4);
    public CornerRadius CornerRadius { get => _cornerRadius; set { _cornerRadius = value; _set |= 1u << 6; } }

    public ScrollbarStyle MergeOver(ScrollbarStyle @base)
    {
        var result = @base;
        if ((_set & (1u << 0)) != 0) result._trackColor = _trackColor;
        if ((_set & (1u << 1)) != 0) result._thumbColor = _thumbColor;
        if ((_set & (1u << 2)) != 0) result._thumbHoverColor = _thumbHoverColor;
        if ((_set & (1u << 3)) != 0) result._width = _width;
        if ((_set & (1u << 4)) != 0) result._minThumbSize = _minThumbSize;
        if ((_set & (1u << 5)) != 0) result._trackPadding = _trackPadding;
        if ((_set & (1u << 6)) != 0) result._cornerRadius = _cornerRadius;
        return result;
    }
}

/// <summary>
/// Flags to customize window behavior.
/// </summary>
[Flags]
public enum WindowFlags
{
    None = 0,
    /// <summary>Disable the collapse button.</summary>
    NoCollapse = 1 << 0,
    /// <summary>Disable the close button.</summary>
    NoClose = 1 << 1,
    /// <summary>Disable scrolling in the content area.</summary>
    NoScroll = 1 << 2,
    /// <summary>Disable window dragging.</summary>
    NoMove = 1 << 3,
    /// <summary>Disable window resizing.</summary>
    NoResize = 1 << 4,
    /// <summary>Prevent this window from being docked.</summary>
    NoDocking = 1 << 5
}

/// <summary>
/// Directions for window resize handles.
/// </summary>
[Flags]
public enum ResizeDirection
{
    None = 0,
    Left = 1 << 0,
    Right = 1 << 1,
    Top = 1 << 2,
    Bottom = 1 << 3,
    TopLeft = Top | Left,
    TopRight = Top | Right,
    BottomLeft = Bottom | Left,
    BottomRight = Bottom | Right
}

public struct WindowStyle
{
    public WindowStyle() { }
    private uint _set;

    // Window background and border
    private Color _backgroundColor = Color.Rgba(35, 35, 40);
    public Color BackgroundColor { get => _backgroundColor; set { _backgroundColor = value; _set |= 1u << 0; } }

    private CornerRadius _cornerRadius = CornerRadius.All(8);
    public CornerRadius CornerRadius { get => _cornerRadius; set { _cornerRadius = value; _set |= 1u << 1; } }

    private BorderConfig _border = BorderConfig.Uniform(1, Color.Rgba(55, 55, 60));
    public BorderConfig Border { get => _border; set { _border = value; _set |= 1u << 2; } }

    // Title bar
    private Color _titleBarColor = Color.Rgba(50, 50, 55);
    public Color TitleBarColor { get => _titleBarColor; set { _titleBarColor = value; _set |= 1u << 3; } }

    private Color _titleBarDragColor = Color.Rgba(60, 60, 65);
    public Color TitleBarDragColor { get => _titleBarDragColor; set { _titleBarDragColor = value; _set |= 1u << 4; } }

    private float _titleBarHeight = 32;
    public float TitleBarHeight { get => _titleBarHeight; set { _titleBarHeight = value; _set |= 1u << 5; } }

    private Padding _titleBarPadding = Padding.Symmetric(8, 6);
    public Padding TitleBarPadding { get => _titleBarPadding; set { _titleBarPadding = value; _set |= 1u << 6; } }

    private Color _titleColor = Color.White;
    public Color TitleColor { get => _titleColor; set { _titleColor = value; _set |= 1u << 7; } }

    // Buttons
    private float _buttonSize = 20;
    public float ButtonSize { get => _buttonSize; set { _buttonSize = value; _set |= 1u << 8; } }

    private Color _buttonHoverColor = Color.Rgba(70, 70, 75);
    public Color ButtonHoverColor { get => _buttonHoverColor; set { _buttonHoverColor = value; _set |= 1u << 9; } }

    private Color _closeButtonHoverColor = Color.Rgba(200, 60, 60);
    public Color CloseButtonHoverColor { get => _closeButtonHoverColor; set { _closeButtonHoverColor = value; _set |= 1u << 10; } }

    // Content area
    private Padding _contentPadding = Padding.All(12);
    public Padding ContentPadding { get => _contentPadding; set { _contentPadding = value; _set |= 1u << 11; } }

    private ushort _contentGap = 8;
    public ushort ContentGap { get => _contentGap; set { _contentGap = value; _set |= 1u << 12; } }

    // Resize
    private float _resizeHandleSize = 8;
    public float ResizeHandleSize { get => _resizeHandleSize; set { _resizeHandleSize = value; _set |= 1u << 13; } }

    private float _minWidth = 150;
    public float MinWidth { get => _minWidth; set { _minWidth = value; _set |= 1u << 14; } }

    private float _minHeight = 100;
    public float MinHeight { get => _minHeight; set { _minHeight = value; _set |= 1u << 15; } }

    private float _maxWidth = float.MaxValue;
    public float MaxWidth { get => _maxWidth; set { _maxWidth = value; _set |= 1u << 16; } }

    private float _maxHeight = float.MaxValue;
    public float MaxHeight { get => _maxHeight; set { _maxHeight = value; _set |= 1u << 17; } }

    private Color _resizeHandleColor = Color.Rgba(100, 100, 110);
    public Color ResizeHandleColor { get => _resizeHandleColor; set { _resizeHandleColor = value; _set |= 1u << 18; } }

    private Color _resizeHandleHoverColor = Color.Rgba(150, 150, 160);
    public Color ResizeHandleHoverColor { get => _resizeHandleHoverColor; set { _resizeHandleHoverColor = value; _set |= 1u << 19; } }

    // Font
    private ushort _fontId = 0;
    public ushort FontId { get => _fontId; set { _fontId = value; _set |= 1u << 20; } }

    private ushort _fontSize = 14;
    public ushort FontSize { get => _fontSize; set { _fontSize = value; _set |= 1u << 21; } }

    private ShadowConfig _shadow = default;
    public ShadowConfig Shadow { get => _shadow; set { _shadow = value; _set |= 1u << 22; } }

    public WindowStyle MergeOver(WindowStyle @base)
    {
        var result = @base;
        if ((_set & (1u << 0)) != 0) result._backgroundColor = _backgroundColor;
        if ((_set & (1u << 1)) != 0) result._cornerRadius = _cornerRadius;
        if ((_set & (1u << 2)) != 0) result._border = _border;
        if ((_set & (1u << 3)) != 0) result._titleBarColor = _titleBarColor;
        if ((_set & (1u << 4)) != 0) result._titleBarDragColor = _titleBarDragColor;
        if ((_set & (1u << 5)) != 0) result._titleBarHeight = _titleBarHeight;
        if ((_set & (1u << 6)) != 0) result._titleBarPadding = _titleBarPadding;
        if ((_set & (1u << 7)) != 0) result._titleColor = _titleColor;
        if ((_set & (1u << 8)) != 0) result._buttonSize = _buttonSize;
        if ((_set & (1u << 9)) != 0) result._buttonHoverColor = _buttonHoverColor;
        if ((_set & (1u << 10)) != 0) result._closeButtonHoverColor = _closeButtonHoverColor;
        if ((_set & (1u << 11)) != 0) result._contentPadding = _contentPadding;
        if ((_set & (1u << 12)) != 0) result._contentGap = _contentGap;
        if ((_set & (1u << 13)) != 0) result._resizeHandleSize = _resizeHandleSize;
        if ((_set & (1u << 14)) != 0) result._minWidth = _minWidth;
        if ((_set & (1u << 15)) != 0) result._minHeight = _minHeight;
        if ((_set & (1u << 16)) != 0) result._maxWidth = _maxWidth;
        if ((_set & (1u << 17)) != 0) result._maxHeight = _maxHeight;
        if ((_set & (1u << 18)) != 0) result._resizeHandleColor = _resizeHandleColor;
        if ((_set & (1u << 19)) != 0) result._resizeHandleHoverColor = _resizeHandleHoverColor;
        if ((_set & (1u << 20)) != 0) result._fontId = _fontId;
        if ((_set & (1u << 21)) != 0) result._fontSize = _fontSize;
        if ((_set & (1u << 22)) != 0) result._shadow = _shadow;
        return result;
    }
}

/// <summary>
/// Style configuration for popup widgets.
/// </summary>
public struct PopupStyle
{
    public PopupStyle() { }
    private uint _set;

    /// <summary>Background color of the popup.</summary>
    private Color _backgroundColor = Color.Rgba(40, 40, 45);
    public Color BackgroundColor { get => _backgroundColor; set { _backgroundColor = value; _set |= 1u << 0; } }

    /// <summary>Corner radius for rounded corners.</summary>
    private CornerRadius _cornerRadius = CornerRadius.All(4);
    public CornerRadius CornerRadius { get => _cornerRadius; set { _cornerRadius = value; _set |= 1u << 1; } }

    /// <summary>Border configuration.</summary>
    private BorderConfig _border = BorderConfig.Uniform(1, Color.Rgba(60, 60, 65));
    public BorderConfig Border { get => _border; set { _border = value; _set |= 1u << 2; } }

    /// <summary>Inner padding.</summary>
    private Padding _padding = Padding.All(8);
    public Padding Padding { get => _padding; set { _padding = value; _set |= 1u << 3; } }

    /// <summary>Gap between child elements.</summary>
    private ushort _contentGap = 4;
    public ushort ContentGap { get => _contentGap; set { _contentGap = value; _set |= 1u << 4; } }

    /// <summary>Shadow/offset from parent element.</summary>
    private Vector2 _offset = new(0, 2);
    public Vector2 Offset { get => _offset; set { _offset = value; _set |= 1u << 5; } }

    /// <summary>Minimum width of the popup.</summary>
    private float _minWidth = 100;
    public float MinWidth { get => _minWidth; set { _minWidth = value; _set |= 1u << 6; } }

    /// <summary>Maximum width of the popup (0 = no limit).</summary>
    private float _maxWidth = 300;
    public float MaxWidth { get => _maxWidth; set { _maxWidth = value; _set |= 1u << 7; } }

    /// <summary>Font ID for text in popup.</summary>
    private ushort _fontId = 0;
    public ushort FontId { get => _fontId; set { _fontId = value; _set |= 1u << 8; } }

    /// <summary>Font size for text in popup.</summary>
    private ushort _fontSize = 14;
    public ushort FontSize { get => _fontSize; set { _fontSize = value; _set |= 1u << 9; } }

    public PopupStyle MergeOver(PopupStyle @base)
    {
        var result = @base;
        if ((_set & (1u << 0)) != 0) result._backgroundColor = _backgroundColor;
        if ((_set & (1u << 1)) != 0) result._cornerRadius = _cornerRadius;
        if ((_set & (1u << 2)) != 0) result._border = _border;
        if ((_set & (1u << 3)) != 0) result._padding = _padding;
        if ((_set & (1u << 4)) != 0) result._contentGap = _contentGap;
        if ((_set & (1u << 5)) != 0) result._offset = _offset;
        if ((_set & (1u << 6)) != 0) result._minWidth = _minWidth;
        if ((_set & (1u << 7)) != 0) result._maxWidth = _maxWidth;
        if ((_set & (1u << 8)) != 0) result._fontId = _fontId;
        if ((_set & (1u << 9)) != 0) result._fontSize = _fontSize;
        return result;
    }
}

/// <summary>
/// Style configuration for modal popup widgets.
/// </summary>
public struct ModalStyle
{
    public ModalStyle() { }
    private uint _set;

    private Color _backgroundColor = Color.Rgba(40, 40, 45);
    public Color BackgroundColor { get => _backgroundColor; set { _backgroundColor = value; _set |= 1u << 0; } }

    private CornerRadius _cornerRadius = CornerRadius.All(6);
    public CornerRadius CornerRadius { get => _cornerRadius; set { _cornerRadius = value; _set |= 1u << 1; } }

    private BorderConfig _border = BorderConfig.Uniform(1, Color.Rgba(70, 70, 80));
    public BorderConfig Border { get => _border; set { _border = value; _set |= 1u << 2; } }

    private Padding _padding = Padding.All(12);
    public Padding Padding { get => _padding; set { _padding = value; _set |= 1u << 3; } }

    private ushort _contentGap = 8;
    public ushort ContentGap { get => _contentGap; set { _contentGap = value; _set |= 1u << 4; } }

    private float _minWidth = 300;
    public float MinWidth { get => _minWidth; set { _minWidth = value; _set |= 1u << 5; } }

    private float _maxWidth = 500;
    public float MaxWidth { get => _maxWidth; set { _maxWidth = value; _set |= 1u << 6; } }

    private Color _dimColor = Color.Rgba(0, 0, 0, 128);
    public Color DimColor { get => _dimColor; set { _dimColor = value; _set |= 1u << 7; } }

    private Color _titleBarColor = Color.Rgba(50, 50, 58);
    public Color TitleBarColor { get => _titleBarColor; set { _titleBarColor = value; _set |= 1u << 8; } }

    private Color _titleColor = Color.White;
    public Color TitleColor { get => _titleColor; set { _titleColor = value; _set |= 1u << 9; } }

    private float _titleBarHeight = 32;
    public float TitleBarHeight { get => _titleBarHeight; set { _titleBarHeight = value; _set |= 1u << 10; } }

    private ushort _titleFontSize = 16;
    public ushort TitleFontSize { get => _titleFontSize; set { _titleFontSize = value; _set |= 1u << 11; } }

    private ushort _fontId = 0;
    public ushort FontId { get => _fontId; set { _fontId = value; _set |= 1u << 12; } }

    private ushort _fontSize = 14;
    public ushort FontSize { get => _fontSize; set { _fontSize = value; _set |= 1u << 13; } }

    public ModalStyle MergeOver(ModalStyle @base)
    {
        var result = @base;
        if ((_set & (1u << 0)) != 0) result._backgroundColor = _backgroundColor;
        if ((_set & (1u << 1)) != 0) result._cornerRadius = _cornerRadius;
        if ((_set & (1u << 2)) != 0) result._border = _border;
        if ((_set & (1u << 3)) != 0) result._padding = _padding;
        if ((_set & (1u << 4)) != 0) result._contentGap = _contentGap;
        if ((_set & (1u << 5)) != 0) result._minWidth = _minWidth;
        if ((_set & (1u << 6)) != 0) result._maxWidth = _maxWidth;
        if ((_set & (1u << 7)) != 0) result._dimColor = _dimColor;
        if ((_set & (1u << 8)) != 0) result._titleBarColor = _titleBarColor;
        if ((_set & (1u << 9)) != 0) result._titleColor = _titleColor;
        if ((_set & (1u << 10)) != 0) result._titleBarHeight = _titleBarHeight;
        if ((_set & (1u << 11)) != 0) result._titleFontSize = _titleFontSize;
        if ((_set & (1u << 12)) != 0) result._fontId = _fontId;
        if ((_set & (1u << 13)) != 0) result._fontSize = _fontSize;
        return result;
    }
}

/// <summary>
/// Style configuration for tooltip widgets.
/// </summary>
public struct TooltipStyle
{
    public TooltipStyle() { }
    private uint _set;

    private Color _backgroundColor = Color.Rgba(20, 20, 25);
    public Color BackgroundColor { get => _backgroundColor; set { _backgroundColor = value; _set |= 1u << 0; } }

    private Color _textColor = Color.Rgba(220, 220, 225);
    public Color TextColor { get => _textColor; set { _textColor = value; _set |= 1u << 1; } }

    private CornerRadius _cornerRadius = CornerRadius.All(4);
    public CornerRadius CornerRadius { get => _cornerRadius; set { _cornerRadius = value; _set |= 1u << 2; } }

    private BorderConfig _border = BorderConfig.Uniform(1, Color.Rgba(60, 60, 65));
    public BorderConfig Border { get => _border; set { _border = value; _set |= 1u << 3; } }

    private Padding _padding = Padding.Symmetric(8, 6);
    public Padding Padding { get => _padding; set { _padding = value; _set |= 1u << 4; } }

    private float _maxWidth = 300;
    public float MaxWidth { get => _maxWidth; set { _maxWidth = value; _set |= 1u << 5; } }

    private ushort _fontId = 0;
    public ushort FontId { get => _fontId; set { _fontId = value; _set |= 1u << 6; } }

    private ushort _fontSize = 13;
    public ushort FontSize { get => _fontSize; set { _fontSize = value; _set |= 1u << 7; } }

    public TooltipStyle MergeOver(TooltipStyle @base)
    {
        var result = @base;
        if ((_set & (1u << 0)) != 0) result._backgroundColor = _backgroundColor;
        if ((_set & (1u << 1)) != 0) result._textColor = _textColor;
        if ((_set & (1u << 2)) != 0) result._cornerRadius = _cornerRadius;
        if ((_set & (1u << 3)) != 0) result._border = _border;
        if ((_set & (1u << 4)) != 0) result._padding = _padding;
        if ((_set & (1u << 5)) != 0) result._maxWidth = _maxWidth;
        if ((_set & (1u << 6)) != 0) result._fontId = _fontId;
        if ((_set & (1u << 7)) != 0) result._fontSize = _fontSize;
        return result;
    }
}

/// <summary>
/// Style configuration for list box widgets.
/// </summary>
public struct ListBoxStyle
{
    public ListBoxStyle() { }
    private uint _set;

    private Color _backgroundColor = Color.Rgba(35, 35, 40);
    public Color BackgroundColor { get => _backgroundColor; set { _backgroundColor = value; _set |= 1u << 0; } }

    private Color _textColor = Color.Rgba(200, 200, 200);
    public Color TextColor { get => _textColor; set { _textColor = value; _set |= 1u << 1; } }

    private Color _labelColor = Color.Rgba(150, 150, 155);
    public Color LabelColor { get => _labelColor; set { _labelColor = value; _set |= 1u << 2; } }

    private Color _hoverColor = Color.Rgba(55, 55, 65);
    public Color HoverColor { get => _hoverColor; set { _hoverColor = value; _set |= 1u << 3; } }

    private Color _selectedColor = Color.Rgba(70, 130, 200);
    public Color SelectedColor { get => _selectedColor; set { _selectedColor = value; _set |= 1u << 4; } }

    private Color _selectedTextColor = Color.White;
    public Color SelectedTextColor { get => _selectedTextColor; set { _selectedTextColor = value; _set |= 1u << 5; } }

    private CornerRadius _cornerRadius = CornerRadius.All(4);
    public CornerRadius CornerRadius { get => _cornerRadius; set { _cornerRadius = value; _set |= 1u << 6; } }

    private BorderConfig _border = BorderConfig.Uniform(1, Color.Rgba(60, 60, 65));
    public BorderConfig Border { get => _border; set { _border = value; _set |= 1u << 7; } }

    private Padding _padding = Padding.All(4);
    public Padding Padding { get => _padding; set { _padding = value; _set |= 1u << 8; } }

    private Padding _itemPadding = Padding.Symmetric(8, 4);
    public Padding ItemPadding { get => _itemPadding; set { _itemPadding = value; _set |= 1u << 9; } }

    private float _itemCornerRadius = 3;
    public float ItemCornerRadius { get => _itemCornerRadius; set { _itemCornerRadius = value; _set |= 1u << 10; } }

    private ushort _fontId = 0;
    public ushort FontId { get => _fontId; set { _fontId = value; _set |= 1u << 11; } }

    private ushort _fontSize = 14;
    public ushort FontSize { get => _fontSize; set { _fontSize = value; _set |= 1u << 12; } }

    public ListBoxStyle MergeOver(ListBoxStyle @base)
    {
        var result = @base;
        if ((_set & (1u << 0)) != 0) result._backgroundColor = _backgroundColor;
        if ((_set & (1u << 1)) != 0) result._textColor = _textColor;
        if ((_set & (1u << 2)) != 0) result._labelColor = _labelColor;
        if ((_set & (1u << 3)) != 0) result._hoverColor = _hoverColor;
        if ((_set & (1u << 4)) != 0) result._selectedColor = _selectedColor;
        if ((_set & (1u << 5)) != 0) result._selectedTextColor = _selectedTextColor;
        if ((_set & (1u << 6)) != 0) result._cornerRadius = _cornerRadius;
        if ((_set & (1u << 7)) != 0) result._border = _border;
        if ((_set & (1u << 8)) != 0) result._padding = _padding;
        if ((_set & (1u << 9)) != 0) result._itemPadding = _itemPadding;
        if ((_set & (1u << 10)) != 0) result._itemCornerRadius = _itemCornerRadius;
        if ((_set & (1u << 11)) != 0) result._fontId = _fontId;
        if ((_set & (1u << 12)) != 0) result._fontSize = _fontSize;
        return result;
    }
}

/// <summary>
/// Style configuration for combo box (dropdown) widgets.
/// </summary>
public struct ComboStyle
{
    public ComboStyle() { }
    private uint _set;

    private Color _backgroundColor = Color.Rgba(45, 45, 50);
    public Color BackgroundColor { get => _backgroundColor; set { _backgroundColor = value; _set |= 1u << 0; } }

    private Color _hoverColor = Color.Rgba(55, 55, 65);
    public Color HoverColor { get => _hoverColor; set { _hoverColor = value; _set |= 1u << 1; } }

    private Color _textColor = Color.Rgba(200, 200, 200);
    public Color TextColor { get => _textColor; set { _textColor = value; _set |= 1u << 2; } }

    private Color _labelColor = Color.Rgba(150, 150, 155);
    public Color LabelColor { get => _labelColor; set { _labelColor = value; _set |= 1u << 3; } }

    private Color _arrowColor = Color.Rgba(150, 150, 155);
    public Color ArrowColor { get => _arrowColor; set { _arrowColor = value; _set |= 1u << 4; } }

    private Color _selectedColor = Color.Rgba(70, 130, 200);
    public Color SelectedColor { get => _selectedColor; set { _selectedColor = value; _set |= 1u << 5; } }

    private Color _selectedTextColor = Color.White;
    public Color SelectedTextColor { get => _selectedTextColor; set { _selectedTextColor = value; _set |= 1u << 6; } }

    private Color _itemHoverColor = Color.Rgba(55, 55, 65);
    public Color ItemHoverColor { get => _itemHoverColor; set { _itemHoverColor = value; _set |= 1u << 7; } }

    private CornerRadius _cornerRadius = CornerRadius.All(4);
    public CornerRadius CornerRadius { get => _cornerRadius; set { _cornerRadius = value; _set |= 1u << 8; } }

    private BorderConfig _border = BorderConfig.Uniform(1, Color.Rgba(60, 60, 65));
    public BorderConfig Border { get => _border; set { _border = value; _set |= 1u << 9; } }

    private Padding _padding = Padding.Symmetric(8, 6);
    public Padding Padding { get => _padding; set { _padding = value; _set |= 1u << 10; } }

    private float _minWidth = 120;
    public float MinWidth { get => _minWidth; set { _minWidth = value; _set |= 1u << 11; } }

    private float _maxWidth = 300;
    public float MaxWidth { get => _maxWidth; set { _maxWidth = value; _set |= 1u << 12; } }

    private ushort _fontId = 0;
    public ushort FontId { get => _fontId; set { _fontId = value; _set |= 1u << 13; } }

    private ushort _fontSize = 14;
    public ushort FontSize { get => _fontSize; set { _fontSize = value; _set |= 1u << 14; } }

    public ComboStyle MergeOver(ComboStyle @base)
    {
        var result = @base;
        if ((_set & (1u << 0)) != 0) result._backgroundColor = _backgroundColor;
        if ((_set & (1u << 1)) != 0) result._hoverColor = _hoverColor;
        if ((_set & (1u << 2)) != 0) result._textColor = _textColor;
        if ((_set & (1u << 3)) != 0) result._labelColor = _labelColor;
        if ((_set & (1u << 4)) != 0) result._arrowColor = _arrowColor;
        if ((_set & (1u << 5)) != 0) result._selectedColor = _selectedColor;
        if ((_set & (1u << 6)) != 0) result._selectedTextColor = _selectedTextColor;
        if ((_set & (1u << 7)) != 0) result._itemHoverColor = _itemHoverColor;
        if ((_set & (1u << 8)) != 0) result._cornerRadius = _cornerRadius;
        if ((_set & (1u << 9)) != 0) result._border = _border;
        if ((_set & (1u << 10)) != 0) result._padding = _padding;
        if ((_set & (1u << 11)) != 0) result._minWidth = _minWidth;
        if ((_set & (1u << 12)) != 0) result._maxWidth = _maxWidth;
        if ((_set & (1u << 13)) != 0) result._fontId = _fontId;
        if ((_set & (1u << 14)) != 0) result._fontSize = _fontSize;
        return result;
    }
}

// ============ Docking System ============

/// <summary>
/// Split direction for dock nodes.
/// </summary>
public enum DockSplitDirection : byte
{
    /// <summary>Leaf node (holds tabs, no split).</summary>
    None = 0,
    /// <summary>Children split left/right.</summary>
    Horizontal = 1,
    /// <summary>Children split top/bottom.</summary>
    Vertical = 2
}

/// <summary>
/// Drop zone when dragging a window over a dock node.
/// </summary>
public enum DockDropZone : byte
{
    None = 0,
    Left = 1,
    Right = 2,
    Top = 3,
    Bottom = 4,
    Center = 5
}

/// <summary>
/// A node in the dock tree. Internal nodes split space between two children.
/// Leaf nodes hold one or more docked windows displayed as tabs.
/// </summary>
public class DockNode
{
    public uint Id;
    public DockSplitDirection SplitDirection;

    // Internal node fields (valid when SplitDirection != None)
    public DockNode? ChildA;       // Left or Top child
    public DockNode? ChildB;       // Right or Bottom child
    public float SplitRatio = 0.5f; // 0-1, fraction of space given to ChildA

    // Leaf node fields (valid when SplitDirection == None)
    public List<uint> DockedWindowIds = new();
    public int ActiveTabIndex;

    public bool IsLeaf => SplitDirection == DockSplitDirection.None;
    public bool IsEmpty => IsLeaf && DockedWindowIds.Count == 0;

    /// <summary>
    /// Finds a node by ID in this subtree.
    /// </summary>
    public DockNode? FindNode(uint nodeId)
    {
        if (Id == nodeId) return this;
        if (!IsLeaf)
        {
            var found = ChildA?.FindNode(nodeId);
            if (found != null) return found;
            found = ChildB?.FindNode(nodeId);
            if (found != null) return found;
        }
        return null;
    }

    /// <summary>
    /// Finds the parent of a given node in this subtree.
    /// </summary>
    public DockNode? FindParent(uint childId)
    {
        if (IsLeaf) return null;
        if (ChildA?.Id == childId || ChildB?.Id == childId) return this;
        var found = ChildA?.FindParent(childId);
        if (found != null) return found;
        return ChildB?.FindParent(childId);
    }

    /// <summary>
    /// Rebuilds the window-to-node lookup map for all leaves in this subtree.
    /// </summary>
    public void RebuildWindowToNodeMap(Dictionary<uint, DockNode> map)
    {
        if (IsLeaf)
        {
            foreach (var winId in DockedWindowIds)
                map[winId] = this;
        }
        else
        {
            ChildA?.RebuildWindowToNodeMap(map);
            ChildB?.RebuildWindowToNodeMap(map);
        }
    }
}

/// <summary>
/// Persistent state for a single DockSpace.
/// </summary>
public class DockSpaceState
{
    public uint Id;
    public DockNode RootNode = null!;
    public readonly Dictionary<uint, DockNode> WindowToNode = new();

    internal uint NextNodeId;

    /// <summary>
    /// Generates a unique node ID for this dock space.
    /// </summary>
    internal uint GenerateNodeId()
    {
        return ElementId.Hash($"DockNode_{Id}_{NextNodeId++}").Id;
    }
}

/// <summary>
/// Style configuration for dock space widgets.
/// </summary>
public struct DockSpaceStyle
{
    public DockSpaceStyle() { }
    private uint _set;

    private float _tabBarHeight = 26;
    public float TabBarHeight { get => _tabBarHeight; set { _tabBarHeight = value; _set |= 1u << 0; } }

    private float _tabMinWidth = 60;
    public float TabMinWidth { get => _tabMinWidth; set { _tabMinWidth = value; _set |= 1u << 1; } }

    private float _tabMaxWidth = 200;
    public float TabMaxWidth { get => _tabMaxWidth; set { _tabMaxWidth = value; _set |= 1u << 2; } }

    private float _tabPadding = 8;
    public float TabPadding { get => _tabPadding; set { _tabPadding = value; _set |= 1u << 3; } }

    private float _splitterThickness = 4;
    public float SplitterThickness { get => _splitterThickness; set { _splitterThickness = value; _set |= 1u << 4; } }

    private float _undockThreshold = 15;
    public float UndockThreshold { get => _undockThreshold; set { _undockThreshold = value; _set |= 1u << 5; } }

    private Color _tabActiveColor = Color.Rgba(50, 50, 55);
    public Color TabActiveColor { get => _tabActiveColor; set { _tabActiveColor = value; _set |= 1u << 6; } }

    private Color _tabInactiveColor = Color.Rgba(35, 35, 38);
    public Color TabInactiveColor { get => _tabInactiveColor; set { _tabInactiveColor = value; _set |= 1u << 7; } }

    private Color _tabHoverColor = Color.Rgba(60, 60, 65);
    public Color TabHoverColor { get => _tabHoverColor; set { _tabHoverColor = value; _set |= 1u << 8; } }

    private Color _tabTextColor = Color.Rgba(200, 200, 200);
    public Color TabTextColor { get => _tabTextColor; set { _tabTextColor = value; _set |= 1u << 9; } }

    private Color _tabActiveTextColor = Color.Rgba(240, 240, 240);
    public Color TabActiveTextColor { get => _tabActiveTextColor; set { _tabActiveTextColor = value; _set |= 1u << 10; } }

    private Color _tabBarColor = Color.Rgba(30, 30, 33);
    public Color TabBarColor { get => _tabBarColor; set { _tabBarColor = value; _set |= 1u << 11; } }

    private Color _dropPreviewColor = Color.Rgba(60, 140, 230, 80);
    public Color DropPreviewColor { get => _dropPreviewColor; set { _dropPreviewColor = value; _set |= 1u << 12; } }

    private Color _splitterColor = Color.Rgba(20, 20, 22);
    public Color SplitterColor { get => _splitterColor; set { _splitterColor = value; _set |= 1u << 13; } }

    private Color _splitterHoverColor = Color.Rgba(60, 140, 230);
    public Color SplitterHoverColor { get => _splitterHoverColor; set { _splitterHoverColor = value; _set |= 1u << 14; } }

    private Color _splitterDragColor = Color.Rgba(60, 140, 230);
    public Color SplitterDragColor { get => _splitterDragColor; set { _splitterDragColor = value; _set |= 1u << 15; } }

    private Color _contentBackgroundColor = Color.Rgba(35, 35, 40);
    public Color ContentBackgroundColor { get => _contentBackgroundColor; set { _contentBackgroundColor = value; _set |= 1u << 16; } }

    private ushort _fontId = 0;
    public ushort FontId { get => _fontId; set { _fontId = value; _set |= 1u << 17; } }

    private ushort _fontSize = 13;
    public ushort FontSize { get => _fontSize; set { _fontSize = value; _set |= 1u << 18; } }

    public DockSpaceStyle MergeOver(DockSpaceStyle @base)
    {
        var result = @base;
        if ((_set & (1u << 0)) != 0) result._tabBarHeight = _tabBarHeight;
        if ((_set & (1u << 1)) != 0) result._tabMinWidth = _tabMinWidth;
        if ((_set & (1u << 2)) != 0) result._tabMaxWidth = _tabMaxWidth;
        if ((_set & (1u << 3)) != 0) result._tabPadding = _tabPadding;
        if ((_set & (1u << 4)) != 0) result._splitterThickness = _splitterThickness;
        if ((_set & (1u << 5)) != 0) result._undockThreshold = _undockThreshold;
        if ((_set & (1u << 6)) != 0) result._tabActiveColor = _tabActiveColor;
        if ((_set & (1u << 7)) != 0) result._tabInactiveColor = _tabInactiveColor;
        if ((_set & (1u << 8)) != 0) result._tabHoverColor = _tabHoverColor;
        if ((_set & (1u << 9)) != 0) result._tabTextColor = _tabTextColor;
        if ((_set & (1u << 10)) != 0) result._tabActiveTextColor = _tabActiveTextColor;
        if ((_set & (1u << 11)) != 0) result._tabBarColor = _tabBarColor;
        if ((_set & (1u << 12)) != 0) result._dropPreviewColor = _dropPreviewColor;
        if ((_set & (1u << 13)) != 0) result._splitterColor = _splitterColor;
        if ((_set & (1u << 14)) != 0) result._splitterHoverColor = _splitterHoverColor;
        if ((_set & (1u << 15)) != 0) result._splitterDragColor = _splitterDragColor;
        if ((_set & (1u << 16)) != 0) result._contentBackgroundColor = _contentBackgroundColor;
        if ((_set & (1u << 17)) != 0) result._fontId = _fontId;
        if ((_set & (1u << 18)) != 0) result._fontSize = _fontSize;
        return result;
    }
}

/// <summary>
/// Fluent API for defining dock layouts, used with BeginDockSpace's setup callback.
/// </summary>
public class DockLayout
{
    private readonly DockSpaceState _space;
    private readonly uint _rootId;

    internal DockLayout(DockSpaceState space)
    {
        _space = space;
        _rootId = space.RootNode.Id;
    }

    /// <summary>
    /// Splits the root node. Returns the IDs of the two children.
    /// </summary>
    public (uint a, uint b) Split(DockSplitDirection direction, float ratio)
        => Split(_rootId, direction, ratio);

    /// <summary>
    /// Splits a node by ID. Returns the IDs of the two children.
    /// </summary>
    public (uint a, uint b) Split(uint nodeId, DockSplitDirection direction, float ratio)
    {
        var node = _space.RootNode.FindNode(nodeId);
        if (node == null || !node.IsLeaf)
            throw new InvalidOperationException($"Node {nodeId} not found or is not a leaf.");

        var idA = _space.GenerateNodeId();
        var idB = _space.GenerateNodeId();

        node.SplitDirection = direction;
        node.ChildA = new DockNode
        {
            Id = idA,
            DockedWindowIds = node.DockedWindowIds,
            ActiveTabIndex = node.ActiveTabIndex
        };
        node.ChildB = new DockNode { Id = idB };
        node.SplitRatio = Math.Clamp(ratio, 0.1f, 0.9f);
        node.DockedWindowIds = new List<uint>();
        node.ActiveTabIndex = 0;

        return (idA, idB);
    }

    /// <summary>
    /// Docks a window into a leaf node.
    /// </summary>
    public void Window(uint nodeId, string title)
    {
        var node = _space.RootNode.FindNode(nodeId);
        if (node == null || !node.IsLeaf)
            throw new InvalidOperationException($"Node {nodeId} not found or is not a leaf.");

        var windowId = ElementId.Hash($"Window_{title}", seed: 0x436C6179).Id;
        if (!node.DockedWindowIds.Contains(windowId))
        {
            node.DockedWindowIds.Add(windowId);
            ClayUI.SetDockedWindowTitle(windowId, title);
        }
    }

    /// <summary>
    /// Docks a window into the root node (convenience for single-window dock).
    /// </summary>
    public void Window(string title) => Window(_rootId, title);
}

/// <summary>
/// Provides methods to programmatically build dock layouts.
/// </summary>
public static class DockBuilder
{
    /// <summary>
    /// Checks if a dock space has been initialized with a layout.
    /// </summary>
    public static bool HasLayout(string dockSpaceLabel)
    {
        var id = ElementId.Hash(dockSpaceLabel, seed: 0x436C6179);
        var space = ClayUI.GetDockSpaceState(id.Id);
        return space?.RootNode != null && !space.RootNode.IsEmpty;
    }

    /// <summary>
    /// Gets or creates a dock space state, clearing any existing layout.
    /// Returns the root node ID.
    /// </summary>
    public static uint Reset(string dockSpaceLabel)
    {
        var id = ElementId.Hash(dockSpaceLabel, seed: 0x436C6179);
        var space = ClayUI.GetOrCreateDockSpaceState(id.Id);
        space.NextNodeId = 0;
        var rootId = space.GenerateNodeId();
        space.RootNode = new DockNode { Id = rootId };
        space.WindowToNode.Clear();
        return rootId;
    }

    /// <summary>
    /// Splits a leaf node into two children. Returns the IDs of the two new child nodes.
    /// </summary>
    public static (uint nodeIdA, uint nodeIdB) SplitNode(
        uint nodeId, DockSplitDirection direction, float sizeRatioForNodeA)
    {
        var (space, node) = ClayUI.FindDockNode(nodeId);
        if (space == null || node == null)
            throw new InvalidOperationException($"Dock node {nodeId} not found.");
        if (!node.IsLeaf)
            throw new InvalidOperationException($"Dock node {nodeId} is not a leaf node.");
        if (direction == DockSplitDirection.None)
            throw new ArgumentException("Split direction must be Horizontal or Vertical.");

        var idA = space.GenerateNodeId();
        var idB = space.GenerateNodeId();

        // Move existing docked windows to child A
        var childA = new DockNode
        {
            Id = idA,
            DockedWindowIds = node.DockedWindowIds,
            ActiveTabIndex = node.ActiveTabIndex
        };
        var childB = new DockNode
        {
            Id = idB
        };

        node.SplitDirection = direction;
        node.ChildA = childA;
        node.ChildB = childB;
        node.SplitRatio = Math.Clamp(sizeRatioForNodeA, 0.1f, 0.9f);
        node.DockedWindowIds = new List<uint>(); // Clear leaf data
        node.ActiveTabIndex = 0;

        return (idA, idB);
    }

    /// <summary>
    /// Docks a window into a specific leaf node.
    /// </summary>
    public static void DockWindow(uint nodeId, string windowTitle)
    {
        var (space, node) = ClayUI.FindDockNode(nodeId);
        if (space == null || node == null)
            throw new InvalidOperationException($"Dock node {nodeId} not found.");
        if (!node.IsLeaf)
            throw new InvalidOperationException($"Dock node {nodeId} is not a leaf node.");

        var windowId = ElementId.Hash($"Window_{windowTitle}", seed: 0x436C6179).Id;
        if (!node.DockedWindowIds.Contains(windowId))
        {
            node.DockedWindowIds.Add(windowId);
            ClayUI.SetDockedWindowTitle(windowId, windowTitle);
        }
    }

    /// <summary>
    /// Serializes the dock layout to a JSON string for saving.
    /// </summary>
    public static string SaveLayout(string dockSpaceLabel)
    {
        var id = ElementId.Hash(dockSpaceLabel, seed: 0x436C6179);
        var space = ClayUI.GetDockSpaceState(id.Id);
        if (space?.RootNode == null)
            return "{}";

        var root = SerializeNode(space.RootNode);
        return JsonSerializer.Serialize(root, new JsonSerializerOptions { WriteIndented = true });
    }

    /// <summary>
    /// Restores a dock layout from a previously saved JSON string.
    /// </summary>
    public static void LoadLayout(string dockSpaceLabel, string json)
    {
        var id = ElementId.Hash(dockSpaceLabel, seed: 0x436C6179);
        var space = ClayUI.GetOrCreateDockSpaceState(id.Id);

        var doc = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json);
        if (doc == null) return;

        space.NextNodeId = 0;
        space.RootNode = DeserializeNode(doc, space);
        space.WindowToNode.Clear();
    }

    private static Dictionary<string, object?> SerializeNode(DockNode node)
    {
        var dict = new Dictionary<string, object?>();

        if (node.IsLeaf)
        {
            // Leaf: store window titles
            var titles = new List<string>();
            foreach (var winId in node.DockedWindowIds)
            {
                if (ClayUI.GetDockedWindowTitle(winId) is { } title)
                    titles.Add(title);
            }
            dict["windows"] = titles;
            dict["activeTab"] = node.ActiveTabIndex;
        }
        else
        {
            dict["split"] = node.SplitDirection.ToString();
            dict["ratio"] = node.SplitRatio;
            dict["childA"] = SerializeNode(node.ChildA!);
            dict["childB"] = SerializeNode(node.ChildB!);
        }

        return dict;
    }

    private static DockNode DeserializeNode(Dictionary<string, JsonElement> data, DockSpaceState space)
    {
        var node = new DockNode { Id = space.GenerateNodeId() };

        if (data.TryGetValue("split", out var splitEl))
        {
            // Internal node
            node.SplitDirection = Enum.Parse<DockSplitDirection>(splitEl.GetString()!);
            node.SplitRatio = data["ratio"].GetSingle();
            node.ChildA = DeserializeNode(
                data["childA"].Deserialize<Dictionary<string, JsonElement>>()!, space);
            node.ChildB = DeserializeNode(
                data["childB"].Deserialize<Dictionary<string, JsonElement>>()!, space);
        }
        else if (data.TryGetValue("windows", out var windowsEl))
        {
            // Leaf node
            foreach (var winEl in windowsEl.EnumerateArray())
            {
                var title = winEl.GetString()!;
                var windowId = ElementId.Hash($"Window_{title}", seed: 0x436C6179).Id;
                node.DockedWindowIds.Add(windowId);
                ClayUI.SetDockedWindowTitle(windowId, title);
            }
            if (data.TryGetValue("activeTab", out var tabEl))
                node.ActiveTabIndex = tabEl.GetInt32();
        }

        return node;
    }
}
