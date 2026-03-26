using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Clay;

/// <summary>
/// Represents width and height dimensions.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct Dimensions
{
    public float Width;
    public float Height;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Dimensions(float width, float height)
    {
        Width = width;
        Height = height;
    }

    public static readonly Dimensions Zero = new(0, 0);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Dimensions Max(Dimensions a, Dimensions b)
        => new(Math.Max(a.Width, b.Width), Math.Max(a.Height, b.Height));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Dimensions Min(Dimensions a, Dimensions b)
        => new(Math.Min(a.Width, b.Width), Math.Min(a.Height, b.Height));

    public override string ToString() => $"({Width}, {Height})";
}
