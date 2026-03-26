using System.Numerics;
using System.Runtime.InteropServices;

namespace Clay;

/// <summary>
/// Represents the current state of pointer (mouse/touch) interaction.
/// </summary>
public enum PointerInteractionState : byte
{
    /// <summary>
    /// A click/touch started this frame.
    /// </summary>
    PressedThisFrame = 0,

    /// <summary>
    /// The pointer is held down (started in a previous frame).
    /// </summary>
    Pressed = 1,

    /// <summary>
    /// The pointer was released this frame.
    /// </summary>
    ReleasedThisFrame = 2,

    /// <summary>
    /// The pointer is not pressed.
    /// </summary>
    Released = 3
}

/// <summary>
/// Information about the current pointer state.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct PointerData
{
    /// <summary>
    /// Position of the pointer relative to the layout root.
    /// </summary>
    public Vector2 Position;

    /// <summary>
    /// Current interaction state of the pointer.
    /// </summary>
    public PointerInteractionState State;

    /// <summary>
    /// Returns true if the pointer is currently pressed.
    /// </summary>
    public readonly bool IsPressed => State == PointerInteractionState.PressedThisFrame || State == PointerInteractionState.Pressed;

    /// <summary>
    /// Returns true if the pointer was just pressed this frame.
    /// </summary>
    public readonly bool JustPressed => State == PointerInteractionState.PressedThisFrame;

    /// <summary>
    /// Returns true if the pointer was just released this frame.
    /// </summary>
    public readonly bool JustReleased => State == PointerInteractionState.ReleasedThisFrame;
}
