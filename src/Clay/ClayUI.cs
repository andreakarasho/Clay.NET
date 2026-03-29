using System.Numerics;
using System.Runtime.CompilerServices;
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
    internal record struct ScrollWrapperInfo(ElementId ScrollId, bool IsVertical, bool HasWrapper);
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

    /// <summary>
    /// Clears per-frame state. Called at the start of each frame.
    /// </summary>
    internal void BeginFrame(bool mouseDown, Vector2 mousePosition)
    {
        PressedThisFrame.Clear();
        HoveredThisFrame.Clear();
        DisabledDepth = 0;
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
        // pre-consume the click so widgets rendered before BeginPopup don't fire.
        // BeginPopup will handle the actual closing.
        bool justPressed = mouseDown && !MouseWasPressed;
        if (justPressed && OpenPopupStack.Count > 0 && !IsPointOverAnyPopup(mousePosition))
        {
            ClickConsumedThisFrame = true;
        }

        // Release active slider/scrollbar/window/resize when mouse is released
        if (!mouseDown)
        {
            ActiveSliderTrackId = 0;
            ActiveScrollbarId = 0;
            ActiveDragWindowId = 0;
            ActiveResizeWindowId = 0;
            ActiveResizeDirection = ResizeDirection.None;
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
            ? ElementId.Hash($"SbTrackV_{_context.ActiveScrollContainerId.Id}")
            : ElementId.Hash($"SbTrackH_{_context.ActiveScrollContainerId.Id}");
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
            return _context.IsWindowTopmostAtMouse(currentWindowId, Style.Window.TitleBarHeight);
        }
    }

    /// <summary>
    /// Returns true if a click should be processed for this widget.
    /// Widgets inside topmost windows process clicks; widgets outside are blocked if mouse is over any window.
    /// Widgets behind open popups are blocked unless the widget is inside the popup.
    /// </summary>
    private static bool ShouldProcessClick
    {
        get
        {
            if (!IsMouseJustPressed) return false;

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
        => ElementId.Hash(label, _context.IdCounter++, IdSeed);

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
    public static bool Button(string label, ButtonStyle? style = null)
    {
        var s = style ?? Style.Button;
        var id = Id(label);
        bool isHovered = IsHovered(id);
        bool isPressed = isHovered && _context.MousePressed;  // Mouse held down on button
        bool clicked = isHovered && ShouldProcessClick;  // Mouse just clicked (blocked by windows if outside)

        if (isHovered) _context.HoveredThisFrame.Add(id.Id);
        if (clicked) _context.PressedThisFrame.Add(id.Id);

        var bgColor = isPressed ? s.PressedColor
            : isHovered ? s.HoverColor
            : s.BackgroundColor;

        using (Clay.Element(new ElementDeclaration
        {
            Id = id,
            Layout = new LayoutConfig
            {
                Padding = s.Padding,
                ChildAlignment = ChildAlignment.Center
            },
            BackgroundColor = DisabledColor(bgColor),
            CornerRadius = s.CornerRadius
        }))
        {
            Clay.Text(ElementId.GetDisplayLabel(label), new TextConfig
            {
                FontId = s.FontId,
                FontSize = s.FontSize,
                TextColor = DisabledColor(s.TextColor)
            });
        }

        return clicked;
    }

    /// <summary>
    /// Renders a text label.
    /// </summary>
    public static void Label(string text, LabelStyle? style = null)
    {
        var s = style ?? Style.Label;
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
        var s = style ?? Style.Heading;
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
        var s = style ?? Style.Image;
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
        var s = style ?? Style.Image;
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
    public static bool Checkbox(string label, ref bool value, CheckboxStyle? style = null)
    {
        var s = style ?? Style.Checkbox;
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
            using (Clay.Element(new ElementDeclaration
            {
                Layout = new LayoutConfig
                {
                    Sizing = Sizing.FixedSize(s.BoxSize, s.BoxSize),
                    ChildAlignment = ChildAlignment.Center
                },
                BackgroundColor = DisabledColor(value ? s.CheckedColor : s.BoxColor),
                CornerRadius = CornerRadius.All(s.BoxCornerRadius),
                Border = BorderConfig.Uniform(1, DisabledColor(s.BoxBorderColor))
            }))
            {
                if (value)
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
    public static bool Slider(string label, ref float value, float min = 0f, float max = 1f, SliderStyle? style = null)
    {
        var s = style ?? Style.Slider;
        var id = Id(label);
        var trackId = ElementId.Hash($"SlTrack_{id.Id}");
        bool isHovered = IsHovered(trackId);
        bool changed = false;

        // Get track bounds for dragging
        var trackData = Clay.GetElementData(trackId);

        // Check if this slider should become active (mouse just pressed on it)
        if (isHovered && ShouldProcessClick && trackData.Found)
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
                BackgroundColor = DisabledColor(s.TrackColor),
                CornerRadius = CornerRadius.All(s.TrackHeight / 2)
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
                    BackgroundColor = DisabledColor(s.FillColor),
                    CornerRadius = CornerRadius.All(s.TrackHeight / 2)
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
    public static bool Toggle(string label, ref bool value, ToggleStyle? style = null)
    {
        var s = style ?? Style.Toggle;
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
            using (Clay.Element(new ElementDeclaration
            {
                Layout = new LayoutConfig
                {
                    Sizing = Sizing.FixedSize(s.TrackWidth, s.TrackHeight),
                    Padding = new Padding { Left = 2, Right = 2 },
                    ChildAlignment = value ? ChildAlignment.CenterRight : ChildAlignment.CenterLeft
                },
                BackgroundColor = DisabledColor(value ? s.OnColor : s.OffColor),
                CornerRadius = CornerRadius.All(s.TrackHeight / 2)
            }))
            {
                // Toggle knob
                using (Clay.Element(new ElementDeclaration
                {
                    Layout = new LayoutConfig
                    {
                        Sizing = Sizing.FixedSize(s.KnobSize, s.KnobSize)
                    },
                    BackgroundColor = DisabledColor(s.KnobColor),
                    CornerRadius = CornerRadius.All(s.KnobSize / 2)
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
    public static void ProgressBar(float value, float min = 0f, float max = 1f, ProgressBarStyle? style = null)
    {
        var s = style ?? Style.ProgressBar;
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
            BackgroundColor = s.BackgroundColor,
            CornerRadius = CornerRadius.All(s.CornerRadius)
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
                BackgroundColor = s.FillColor,
                CornerRadius = CornerRadius.All(s.CornerRadius)
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
    public static bool TextInput(string label, ref string text, TextInputStyle? style = null, bool singleLine = true)
    {
        var s = style ?? DefaultTextInputStyle;
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
    /// Returns true if the element is hovered and interaction is not disabled.
    /// Widgets should use this instead of Clay.PointerOver() directly.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsHovered(ElementId id)
    {
        return !IsDisabled && Clay.PointerOver(id);
    }

    /// <summary>
    /// Begins a horizontal layout group. Call EndHorizontal() when done.
    /// </summary>
    /// <param name="gap">Gap between children.</param>
    /// <param name="alignment">Child alignment.</param>
    /// <param name="scroll">Enable horizontal scrolling with automatic scrollbar.</param>
    /// <param name="maxWidth">Maximum width before scrolling (only used when scroll=true).</param>
    public static void BeginHorizontal(ushort gap = 8, ChildAlignment alignment = default, bool scroll = false, float? maxWidth = null)
    {
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
                    Sizing = new Sizing
                    {
                        Width = maxWidth.HasValue ? SizingAxis.Fit(0, maxWidth.Value) : SizingAxis.Grow(),
                        Height = SizingAxis.Fit()
                    }
                }
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
                    Sizing = new Sizing
                    {
                        Width = SizingAxis.Grow(),
                        Height = SizingAxis.Fit()
                    }
                }
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
    public static void BeginVertical(ushort gap = 8, ChildAlignment alignment = default, bool scroll = false, float? maxHeight = null)
    {
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
                    Sizing = new Sizing
                    {
                        Width = SizingAxis.Grow(),
                        Height = maxHeight.HasValue ? SizingAxis.Fit(0, maxHeight.Value) : SizingAxis.Fit()
                    }
                }
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
                    Sizing = new Sizing
                    {
                        Width = SizingAxis.Grow(),
                        Height = SizingAxis.Fit()
                    }
                }
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
    public static void BeginPanel(string title, PanelStyle? style = null, bool scroll = false, float? maxHeight = null)
    {
        var s = style ?? Style.Panel;
        var panelId = StableId($"Panel_{title}");

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
                BackgroundColor = s.BackgroundColor,
                CornerRadius = s.CornerRadius,
                Border = s.Border
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
                    Clay.Text(title, new TextConfig
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
                BackgroundColor = s.BackgroundColor,
                CornerRadius = s.CornerRadius,
                Border = s.Border
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
                    Clay.Text(title, new TextConfig
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
        bool topmost = false)
    {
        if (!open)
        {
            _context.WindowStack.Push(0); // Push dummy ID for closed window
            return false;
        }

        var s = style ?? Style.Window;
        var id = StableId($"Window_{title}");

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

        _context.WindowStack.Push(id.Id);
        _context.WindowDepth++;

        var titleBarId = ElementId.Hash($"WinTitle_{id.Id}");
        var collapseButtonId = ElementId.Hash($"WinCollapse_{id.Id}");
        var closeButtonId = ElementId.Hash($"WinClose_{id.Id}");

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
            var scrollId = ElementId.Hash($"WinScroll_{id.Id}");
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
            BackgroundColor = s.BackgroundColor,
            CornerRadius = s.CornerRadius,
            Border = s.Border
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
            BackgroundColor = isBeingDragged ? s.TitleBarDragColor : s.TitleBarColor,
            CornerRadius = new CornerRadius { TopLeft = s.CornerRadius.TopLeft, TopRight = s.CornerRadius.TopRight }
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
                Clay.Text(title, new TextConfig
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
            var scrollId = ElementId.Hash($"WinScroll_{id.Id}");

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
                        var rightColumnId = ElementId.Hash($"WinRCol_{windowId}");
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
        Console.WriteLine($">>> OpenPopup: id='{id}', hash={popupId.Id}, pos={_context.MousePosition}");
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
        }
    }

    /// <summary>
    /// Closes all open popups.
    /// </summary>
    public static void CloseAllPopups()
    {
        foreach (var popupId in _context.OpenPopupStack.ToArray())
        {
            if (_context.PopupStates.TryGetValue(popupId, out var state))
            {
                _context.PopupStates[popupId] = state with { Open = false };
            }
        }
        _context.OpenPopupStack.Clear();
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
        var s = style ?? Style.Popup;

        bool justOpened = false;

        // Debug: only log when there's something to open
        if (_context.PopupToOpen != 0)
        {
            Console.WriteLine($">>> BeginPopup: id='{id}', hash={popupId.Id}, PopupToOpen={_context.PopupToOpen}, match={_context.PopupToOpen == popupId.Id}");
        }

        // Check if this popup was requested to open this frame
        if (_context.PopupToOpen == popupId.Id)
        {
            Console.WriteLine($">>> BeginPopup: OPENING popup '{id}'");
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

        // Handle click-outside-to-close (but not on the frame the popup was just opened).
        // Note: ClickConsumedThisFrame may already be set by BeginFrame's pre-consumption
        // for clicks outside popups, so we don't check it here.
        if (!justOpened && IsMouseJustPressed && !_context.IsPointOverAnyPopup(_context.MousePosition))
        {
            Console.WriteLine($">>> BeginPopup: CLOSING popup '{id}' - click outside detected");
            // Click was outside all popups, close this one and any nested popups
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

        Console.WriteLine($">>> BeginPopup: RENDERING popup '{id}' at ({state.Position.X}, {state.Position.Y}), z={zIndex}");
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

    /// <summary>
    /// Begins a collapsible tree node. Returns true if expanded.
    /// Call EndTreeNode() when done adding content (only if this returns true).
    /// </summary>
    public static bool BeginTreeNode(string label, TreeNodeStyle? style = null)
    {
        var s = style ?? Style.TreeNode;
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
    public static void VerticalScrollbar(ElementId scrollContainerId, ScrollbarStyle? style = null)
    {
        var s = style ?? Style.Scrollbar;
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

        var trackId = ElementId.Hash($"SbTrackV_{scrollContainerId.Id}");
        var thumbId = ElementId.Hash($"SbThumbV_{scrollContainerId.Id}");

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
            BackgroundColor = s.TrackColor,
            CornerRadius = s.CornerRadius
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
                BackgroundColor = thumbColor,
                CornerRadius = s.CornerRadius
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
    public static void HorizontalScrollbar(ElementId scrollContainerId, ScrollbarStyle? style = null)
    {
        var s = style ?? Style.Scrollbar;
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

        var trackId = ElementId.Hash($"SbTrackH_{scrollContainerId.Id}");
        var thumbId = ElementId.Hash($"SbThumbH_{scrollContainerId.Id}");

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
            BackgroundColor = s.TrackColor,
            CornerRadius = s.CornerRadius
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
                BackgroundColor = thumbColor,
                CornerRadius = s.CornerRadius
            })) { }
        }
    }

    /// <summary>
    /// Begins a scrollable area.
    /// </summary>
    public static ElementScope BeginScrollArea(string id, float? maxHeight = null, ScrollAreaStyle? style = null)
    {
        var s = style ?? Style.ScrollArea;
        var elementId = StableId($"ScrollArea_{id}");

        return Clay.Element(new ElementDeclaration
        {
            Id = elementId,
            Layout = new LayoutConfig
            {
                Direction = LayoutDirection.TopToBottom,
                Sizing = new Sizing
                {
                    Width = SizingAxis.Grow(),
                    Height = maxHeight.HasValue
                        ? SizingAxis.Fit(0, maxHeight.Value)
                        : SizingAxis.Grow()
                },
                Padding = s.Padding
            },
            Scroll = ScrollConfig.VerticalScroll,
            BackgroundColor = s.BackgroundColor,
            CornerRadius = s.CornerRadius
        });
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
        var s = style ?? Style.ListBox;
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
        var s = style ?? Style.Combo;
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
    public static bool RadioGroup(string label, ref int selectedIndex, string[] options, RadioGroupStyle? style = null)
    {
        var s = style ?? Style.RadioGroup;
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
                    using (Clay.Element(new ElementDeclaration
                    {
                        Layout = new LayoutConfig
                        {
                            Sizing = Sizing.FixedSize(s.CircleSize, s.CircleSize),
                            ChildAlignment = ChildAlignment.Center
                        },
                        BackgroundColor = s.CircleColor,
                        CornerRadius = CornerRadius.All(s.CircleSize / 2),
                        Border = BorderConfig.Uniform(1, s.CircleBorderColor)
                    }))
                    {
                        if (isSelected)
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

        if (BeginWindow("ClayUI Debug", ref open, defaultPosition: defaultPos, defaultSize: defaultSize))
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
    public Color SeparatorColor = Color.Rgba(60, 60, 65);

    public static ClayUIStyle Default => new();

    public static ClayUIStyle Dark => new()
    {
        Button = new ButtonStyle
        {
            BackgroundColor = Color.Rgba(55, 55, 65),
            HoverColor = Color.Rgba(70, 70, 80),
            PressedColor = Color.Rgba(50, 50, 60),
            TextColor = Color.White
        },
        Panel = new PanelStyle
        {
            BackgroundColor = Color.Rgba(35, 35, 40),
            TitleColor = Color.Rgba(200, 200, 200)
        }
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
        SeparatorColor = Color.Rgba(200, 200, 205)
    };
}

public struct ButtonStyle
{
    public ButtonStyle() { }
    public Color BackgroundColor { get; set; } = Color.Rgba(55, 55, 65);
    public Color HoverColor { get; set; } = Color.Rgba(70, 70, 80);
    public Color PressedColor { get; set; } = Color.Rgba(50, 100, 180);
    public Color TextColor { get; set; } = Color.White;
    public Padding Padding { get; set; } = Padding.Symmetric(16, 10);
    public CornerRadius CornerRadius { get; set; } = CornerRadius.All(6);
    public ushort FontId { get; set; } = 0;
    public ushort FontSize { get; set; } = 14;
}

public struct ImageStyle
{
    public ImageStyle() { }
    public CornerRadius CornerRadius { get; set; } = CornerRadius.Zero;
    public BorderConfig Border { get; set; } = default;
    public BorderConfig HoverBorder { get; set; } = BorderConfig.Uniform(1, Color.Rgba(100, 150, 255));
    public Color HoverTint { get; set; } = Color.Rgba(255, 255, 255, 30);
    public Color PressedTint { get; set; } = Color.Rgba(0, 0, 0, 40);
    public Padding Padding { get; set; } = Padding.Zero;
}

public struct LabelStyle
{
    public LabelStyle() { }
    public Color TextColor { get; set; } = Color.Rgba(200, 200, 200);
    public ushort FontId { get; set; } = 0;
    public ushort FontSize { get; set; } = 14;
    public ushort LineHeight { get; set; } = 20;
}

public struct HeadingStyle
{
    public HeadingStyle() { }
    public Color TextColor { get; set; } = Color.White;
    public ushort FontId { get; set; } = 0;
    public ushort FontSize { get; set; } = 20;
}

public struct CheckboxStyle
{
    public CheckboxStyle() { }
    public Color BoxColor { get; set; } = Color.Rgba(45, 45, 50);
    public Color CheckedColor { get; set; } = Color.Rgba(70, 130, 200);
    public Color CheckmarkColor { get; set; } = Color.White;
    public Color BoxBorderColor { get; set; } = Color.Rgba(80, 80, 85);
    public Color TextColor { get; set; } = Color.Rgba(200, 200, 200);
    public Color HoverColor { get; set; } = Color.Rgba(50, 50, 55);
    public Color PressedColor { get; set; } = Color.Rgba(40, 40, 45);
    public Padding Padding { get; set; } = Padding.Symmetric(4, 4);
    public float BoxSize { get; set; } = 18;
    public float BoxCornerRadius { get; set; } = 4;
    public ushort FontId { get; set; } = 0;
    public ushort FontSize { get; set; } = 14;
}

public struct SliderStyle
{
    public SliderStyle() { }
    public Color TrackColor { get; set; } = Color.Rgba(45, 45, 50);
    public Color FillColor { get; set; } = Color.Rgba(70, 130, 200);
    public Color TextColor { get; set; } = Color.Rgba(200, 200, 200);
    public Color ValueTextColor { get; set; } = Color.Rgba(150, 150, 155);
    public float TrackHeight { get; set; } = 8;
    public ushort FontId { get; set; } = 0;
    public ushort FontSize { get; set; } = 14;
}

public struct ToggleStyle
{
    public ToggleStyle() { }
    public Color OnColor { get; set; } = Color.Rgba(70, 130, 200);
    public Color OffColor { get; set; } = Color.Rgba(60, 60, 65);
    public Color KnobColor { get; set; } = Color.White;
    public Color TextColor { get; set; } = Color.Rgba(200, 200, 200);
    public Color HoverColor { get; set; } = Color.Rgba(50, 50, 55);
    public Color PressedColor { get; set; } = Color.Rgba(40, 40, 45);
    public Padding Padding { get; set; } = Padding.Symmetric(4, 4);
    public float TrackWidth { get; set; } = 44;
    public float TrackHeight { get; set; } = 24;
    public float KnobSize { get; set; } = 20;
    public ushort FontId { get; set; } = 0;
    public ushort FontSize { get; set; } = 14;
}

public struct ProgressBarStyle
{
    public ProgressBarStyle() { }
    public Color BackgroundColor { get; set; } = Color.Rgba(45, 45, 50);
    public Color FillColor { get; set; } = Color.Rgba(70, 130, 200);
    public float Height { get; set; } = 8;
    public float CornerRadius { get; set; } = 4;
}

public struct PanelStyle
{
    public PanelStyle() { }
    public Color BackgroundColor { get; set; } = Color.Rgba(40, 40, 45);
    public Color TitleColor { get; set; } = Color.White;
    public Color SeparatorColor { get; set; } = Color.Rgba(60, 60, 65);
    public Padding Padding { get; set; } = Padding.All(16);
    public CornerRadius CornerRadius { get; set; } = CornerRadius.All(8);
    public BorderConfig Border { get; set; } = BorderConfig.Uniform(1, Color.Rgba(55, 55, 60));
    public ushort ChildGap { get; set; } = 12;
    public ushort TitleFontId { get; set; } = 0;
    public ushort TitleFontSize { get; set; } = 16;
}

public struct TreeNodeStyle
{
    public TreeNodeStyle() { }
    public Color TextColor { get; set; } = Color.Rgba(200, 200, 200);
    public Color ArrowColor { get; set; } = Color.Rgba(150, 150, 155);
    public Color HoverColor { get; set; } = Color.Rgba(50, 50, 55);
    public Padding Padding { get; set; } = Padding.Symmetric(4, 4);
    public string ExpandedIcon { get; set; } = "v";
    public string CollapsedIcon { get; set; } = ">";
    public ushort IndentSize { get; set; } = 20;
    public bool DefaultExpanded { get; set; } = false;
    public ushort FontId { get; set; } = 0;
    public ushort FontSize { get; set; } = 14;
}

public struct ScrollAreaStyle
{
    public ScrollAreaStyle() { }
    public Color BackgroundColor { get; set; } = Color.Rgba(35, 35, 40);
    public Padding Padding { get; set; } = Padding.All(8);
    public CornerRadius CornerRadius { get; set; } = CornerRadius.All(4);
}

public struct RadioGroupStyle
{
    public RadioGroupStyle() { }
    public Color TextColor { get; set; } = Color.Rgba(200, 200, 200);
    public Color LabelColor { get; set; } = Color.Rgba(150, 150, 155);
    public Color CircleColor { get; set; } = Color.Rgba(45, 45, 50);
    public Color CircleBorderColor { get; set; } = Color.Rgba(80, 80, 85);
    public Color DotColor { get; set; } = Color.Rgba(70, 130, 200);
    public Color HoverColor { get; set; } = Color.Rgba(50, 50, 55);
    public Padding OptionPadding { get; set; } = Padding.Symmetric(4, 4);
    public float CircleSize { get; set; } = 18;
    public float DotSize { get; set; } = 10;
    public ushort FontId { get; set; } = 0;
    public ushort FontSize { get; set; } = 14;
}

public struct ScrollbarStyle
{
    public ScrollbarStyle() { }
    public Color TrackColor { get; set; } = Color.Rgba(40, 40, 45);
    public Color ThumbColor { get; set; } = Color.Rgba(80, 80, 90);
    public Color ThumbHoverColor { get; set; } = Color.Rgba(100, 100, 110);
    public float Width { get; set; } = 8;
    public float MinThumbSize { get; set; } = 20;
    public float TrackPadding { get; set; } = 2;
    public CornerRadius CornerRadius { get; set; } = CornerRadius.All(4);
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
    NoResize = 1 << 4
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
    // Window background and border
    public Color BackgroundColor { get; set; } = Color.Rgba(35, 35, 40);
    public CornerRadius CornerRadius { get; set; } = CornerRadius.All(8);
    public BorderConfig Border { get; set; } = BorderConfig.Uniform(1, Color.Rgba(55, 55, 60));

    // Title bar
    public Color TitleBarColor { get; set; } = Color.Rgba(50, 50, 55);
    public Color TitleBarDragColor { get; set; } = Color.Rgba(60, 60, 65);
    public float TitleBarHeight { get; set; } = 32;
    public Padding TitleBarPadding { get; set; } = Padding.Symmetric(8, 6);
    public Color TitleColor { get; set; } = Color.White;

    // Buttons
    public float ButtonSize { get; set; } = 20;
    public Color ButtonHoverColor { get; set; } = Color.Rgba(70, 70, 75);
    public Color CloseButtonHoverColor { get; set; } = Color.Rgba(200, 60, 60);

    // Content area
    public Padding ContentPadding { get; set; } = Padding.All(12);
    public ushort ContentGap { get; set; } = 8;

    // Resize
    public float ResizeHandleSize { get; set; } = 8;
    public float MinWidth { get; set; } = 150;
    public float MinHeight { get; set; } = 100;
    public float MaxWidth { get; set; } = float.MaxValue;
    public float MaxHeight { get; set; } = float.MaxValue;
    public Color ResizeHandleColor { get; set; } = Color.Rgba(100, 100, 110);
    public Color ResizeHandleHoverColor { get; set; } = Color.Rgba(150, 150, 160);

    // Font
    public ushort FontId { get; set; } = 0;
    public ushort FontSize { get; set; } = 14;
}

/// <summary>
/// Style configuration for popup widgets.
/// </summary>
public struct PopupStyle
{
    public PopupStyle() { }
    /// <summary>Background color of the popup.</summary>
    public Color BackgroundColor { get; set; } = Color.Rgba(40, 40, 45);

    /// <summary>Corner radius for rounded corners.</summary>
    public CornerRadius CornerRadius { get; set; } = CornerRadius.All(4);

    /// <summary>Border configuration.</summary>
    public BorderConfig Border { get; set; } = BorderConfig.Uniform(1, Color.Rgba(60, 60, 65));

    /// <summary>Inner padding.</summary>
    public Padding Padding { get; set; } = Padding.All(8);

    /// <summary>Gap between child elements.</summary>
    public ushort ContentGap { get; set; } = 4;

    /// <summary>Shadow/offset from parent element.</summary>
    public Vector2 Offset { get; set; } = new(0, 2);

    /// <summary>Minimum width of the popup.</summary>
    public float MinWidth { get; set; } = 100;

    /// <summary>Maximum width of the popup (0 = no limit).</summary>
    public float MaxWidth { get; set; } = 300;

    /// <summary>Font ID for text in popup.</summary>
    public ushort FontId { get; set; } = 0;

    /// <summary>Font size for text in popup.</summary>
    public ushort FontSize { get; set; } = 14;
}

/// <summary>
/// Style configuration for list box widgets.
/// </summary>
public struct ListBoxStyle
{
    public ListBoxStyle() { }
    public Color BackgroundColor { get; set; } = Color.Rgba(35, 35, 40);
    public Color TextColor { get; set; } = Color.Rgba(200, 200, 200);
    public Color LabelColor { get; set; } = Color.Rgba(150, 150, 155);
    public Color HoverColor { get; set; } = Color.Rgba(55, 55, 65);
    public Color SelectedColor { get; set; } = Color.Rgba(70, 130, 200);
    public Color SelectedTextColor { get; set; } = Color.White;
    public CornerRadius CornerRadius { get; set; } = CornerRadius.All(4);
    public BorderConfig Border { get; set; } = BorderConfig.Uniform(1, Color.Rgba(60, 60, 65));
    public Padding Padding { get; set; } = Padding.All(4);
    public Padding ItemPadding { get; set; } = Padding.Symmetric(8, 4);
    public float ItemCornerRadius { get; set; } = 3;
    public ushort FontId { get; set; } = 0;
    public ushort FontSize { get; set; } = 14;
}

/// <summary>
/// Style configuration for combo box (dropdown) widgets.
/// </summary>
public struct ComboStyle
{
    public ComboStyle() { }
    public Color BackgroundColor { get; set; } = Color.Rgba(45, 45, 50);
    public Color HoverColor { get; set; } = Color.Rgba(55, 55, 65);
    public Color TextColor { get; set; } = Color.Rgba(200, 200, 200);
    public Color LabelColor { get; set; } = Color.Rgba(150, 150, 155);
    public Color ArrowColor { get; set; } = Color.Rgba(150, 150, 155);
    public Color SelectedColor { get; set; } = Color.Rgba(70, 130, 200);
    public Color SelectedTextColor { get; set; } = Color.White;
    public Color ItemHoverColor { get; set; } = Color.Rgba(55, 55, 65);
    public CornerRadius CornerRadius { get; set; } = CornerRadius.All(4);
    public BorderConfig Border { get; set; } = BorderConfig.Uniform(1, Color.Rgba(60, 60, 65));
    public Padding Padding { get; set; } = Padding.Symmetric(8, 6);
    public float MinWidth { get; set; } = 120;
    public float MaxWidth { get; set; } = 300;
    public ushort FontId { get; set; } = 0;
    public ushort FontSize { get; set; } = 14;
}
