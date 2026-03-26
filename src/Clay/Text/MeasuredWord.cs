using System.Runtime.InteropServices;

namespace Clay;

/// <summary>
/// A measured word for text wrapping calculations.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct MeasuredWord
{
    /// <summary>
    /// Starting offset in the source text.
    /// </summary>
    public int StartOffset;

    /// <summary>
    /// Length of the word in characters.
    /// </summary>
    public int Length;

    /// <summary>
    /// Width of the word in pixels.
    /// </summary>
    public float Width;

    /// <summary>
    /// Index of the next word in the linked list (-1 = end).
    /// </summary>
    public int Next;
}

/// <summary>
/// Cached text measurement data.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct MeasureTextCacheItem
{
    /// <summary>
    /// Dimensions of the text without wrapping.
    /// </summary>
    public Dimensions UnwrappedDimensions;

    /// <summary>
    /// Index of the first measured word.
    /// </summary>
    public int MeasuredWordsStartIndex;

    /// <summary>
    /// Whether the text contains newline characters.
    /// </summary>
    public bool ContainsNewlines;

    /// <summary>
    /// Hash ID for cache lookup.
    /// </summary>
    public uint Id;

    /// <summary>
    /// Index of next item in hash bucket (-1 = end).
    /// </summary>
    public int NextIndex;

    /// <summary>
    /// Generation counter for cache invalidation.
    /// </summary>
    public uint Generation;
}

/// <summary>
/// A line of wrapped text.
/// </summary>
public struct WrappedTextLine
{
    /// <summary>
    /// Dimensions of this line.
    /// </summary>
    public Dimensions Dimensions;

    /// <summary>
    /// Start index in the source text.
    /// </summary>
    public int StartIndex;

    /// <summary>
    /// Length of this line in characters.
    /// </summary>
    public int Length;
}

