using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Clay;

/// <summary>
/// Represents an axis-aligned bounding box with position and size.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct BoundingBox
{
    public float X;
    public float Y;
    public float Width;
    public float Height;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public BoundingBox(float x, float y, float width, float height)
    {
        X = x;
        Y = y;
        Width = width;
        Height = height;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public BoundingBox(Vector2 position, Dimensions size)
    {
        X = position.X;
        Y = position.Y;
        Width = size.Width;
        Height = size.Height;
    }

    public static readonly BoundingBox Zero = new(0, 0, 0, 0);

    public readonly float Left => X;
    public readonly float Top => Y;
    public readonly float Right => X + Width;
    public readonly float Bottom => Y + Height;

    public readonly Vector2 Position => new(X, Y);
    public readonly Dimensions Size => new(Width, Height);

    /// <summary>
    /// Returns true if the given point is inside this bounding box.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly bool Contains(Vector2 point)
        => point.X >= X && point.X <= X + Width &&
           point.Y >= Y && point.Y <= Y + Height;

    /// <summary>
    /// Returns true if this bounding box intersects with another.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly bool Intersects(BoundingBox other)
        => X < other.Right && Right > other.X &&
           Y < other.Bottom && Bottom > other.Y;

    public override string ToString() => $"({X}, {Y}, {Width}, {Height})";
}
