using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Clay;

/// <summary>
/// Specifies how an element's size is determined along an axis.
/// </summary>
public enum SizingType : byte
{
    /// <summary>
    /// Element wraps tightly to fit its contents.
    /// </summary>
    Fit = 0,

    /// <summary>
    /// Element expands to fill available space in the parent.
    /// </summary>
    Grow = 1,

    /// <summary>
    /// Element size is a percentage of the parent (0-1 range).
    /// </summary>
    Percent = 2,

    /// <summary>
    /// Element size is fixed to exact pixel values.
    /// </summary>
    Fixed = 3
}

/// <summary>
/// Defines min/max constraints for element sizing.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct SizingMinMax
{
    public float Min;
    public float Max;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public SizingMinMax(float min, float max)
    {
        Min = min;
        Max = max;
    }
}

/// <summary>
/// Sizing configuration for a single axis.
/// </summary>
[StructLayout(LayoutKind.Explicit)]
public struct SizingAxis
{
    [FieldOffset(0)]
    public SizingMinMax MinMax;

    [FieldOffset(0)]
    public float Percent;

    [FieldOffset(8)]
    public SizingType Type;

    /// <summary>
    /// Creates a FIT sizing that wraps to content with optional min/max constraints.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static SizingAxis Fit(float min = 0, float max = float.MaxValue)
        => new() { MinMax = new(min, max), Type = SizingType.Fit };

    /// <summary>
    /// Creates a GROW sizing that expands to fill available space.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static SizingAxis Grow(float min = 0, float max = float.MaxValue)
        => new() { MinMax = new(min, max), Type = SizingType.Grow };

    /// <summary>
    /// Creates a FIXED sizing with exact pixel size.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static SizingAxis Fixed(float size)
        => new() { MinMax = new(size, size), Type = SizingType.Fixed };

    /// <summary>
    /// Creates a PERCENT sizing relative to parent (0-1 range).
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static SizingAxis PercentOf(float percent)
        => new() { Percent = percent, Type = SizingType.Percent };

    public static readonly SizingAxis Default = Fit();
}

/// <summary>
/// Combined sizing for both width and height axes.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct Sizing
{
    public SizingAxis Width;
    public SizingAxis Height;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Sizing(SizingAxis width, SizingAxis height)
    {
        Width = width;
        Height = height;
    }

    /// <summary>
    /// Creates fixed width and height sizing.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Sizing FixedSize(float width, float height)
        => new(SizingAxis.Fixed(width), SizingAxis.Fixed(height));

    /// <summary>
    /// Creates sizing that fills available space in both axes.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Sizing Fill()
        => new(SizingAxis.Grow(), SizingAxis.Grow());

    /// <summary>
    /// Creates sizing that wraps content in both axes.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Sizing FitContent()
        => new(SizingAxis.Fit(), SizingAxis.Fit());

    /// <summary>
    /// Creates sizing with grow width and fit height.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Sizing FillWidth()
        => new(SizingAxis.Grow(), SizingAxis.Fit());

    /// <summary>
    /// Creates sizing with fit width and grow height.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Sizing FillHeight()
        => new(SizingAxis.Fit(), SizingAxis.Grow());

    public static readonly Sizing Default = FitContent();
}
