using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Clay;

/// <summary>
/// Defines padding (inner spacing) around an element's content.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct Padding
{
    public ushort Left;
    public ushort Right;
    public ushort Top;
    public ushort Bottom;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Padding(ushort left, ushort right, ushort top, ushort bottom)
    {
        Left = left;
        Right = right;
        Top = top;
        Bottom = bottom;
    }

    /// <summary>
    /// Creates padding with the same value on all sides.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Padding All(ushort value)
        => new(value, value, value, value);

    /// <summary>
    /// Creates padding with horizontal and vertical values.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Padding Symmetric(ushort horizontal, ushort vertical)
        => new(horizontal, horizontal, vertical, vertical);

    /// <summary>
    /// Creates padding with only horizontal (left/right) values.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Padding Horizontal(ushort value)
        => new(value, value, 0, 0);

    /// <summary>
    /// Creates padding with only vertical (top/bottom) values.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Padding Vertical(ushort value)
        => new(0, 0, value, value);

    /// <summary>
    /// Creates padding with individual values for each side.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Padding LRTB(ushort left, ushort right, ushort top, ushort bottom)
        => new(left, right, top, bottom);

    public static readonly Padding Zero = new(0, 0, 0, 0);

    /// <summary>
    /// Total horizontal padding (left + right).
    /// </summary>
    public readonly int HorizontalTotal => Left + Right;

    /// <summary>
    /// Total vertical padding (top + bottom).
    /// </summary>
    public readonly int VerticalTotal => Top + Bottom;
}
