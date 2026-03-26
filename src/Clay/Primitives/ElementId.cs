using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Clay;

/// <summary>
/// Represents a hashed element ID used for identifying UI elements.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct ElementId
{
    /// <summary>
    /// The computed hash value.
    /// </summary>
    public uint Id;

    /// <summary>
    /// A numerical offset applied after computing the hash (for indexed elements).
    /// </summary>
    public uint Offset;

    /// <summary>
    /// A base hash value to start from (e.g., parent element ID for local IDs).
    /// </summary>
    public uint BaseId;

    public static readonly ElementId None = default;

    /// <summary>
    /// Creates an ElementId by hashing the given string.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ElementId Hash(ReadOnlySpan<char> key, uint offset = 0, uint seed = 0)
    {
        uint hash = 0;
        uint baseHash = seed;

        for (int i = 0; i < key.Length; i++)
        {
            baseHash += key[i];
            baseHash += baseHash << 10;
            baseHash ^= baseHash >> 6;
        }

        hash = baseHash;
        hash += offset;
        hash += hash << 10;
        hash ^= hash >> 6;

        hash += hash << 3;
        baseHash += baseHash << 3;
        hash ^= hash >> 11;
        baseHash ^= baseHash >> 11;
        hash += hash << 15;
        baseHash += baseHash << 15;

        return new ElementId
        {
            Id = hash + 1, // Reserve 0 as "null id"
            Offset = offset,
            BaseId = baseHash + 1
        };
    }

    /// <summary>
    /// Creates a local ElementId relative to a parent element.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ElementId HashLocal(ReadOnlySpan<char> key, uint parentId, uint offset = 0)
        => Hash(key, offset, parentId);

    /// <summary>
    /// Creates an ElementId from a numeric index (for anonymous/indexed elements).
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ElementId HashNumber(uint number, uint seed = 0)
    {
        uint hash = seed;
        hash += number + 48;
        hash += hash << 10;
        hash ^= hash >> 6;

        hash += hash << 3;
        hash ^= hash >> 11;
        hash += hash << 15;

        return new ElementId
        {
            Id = hash + 1,
            Offset = number,
            BaseId = seed
        };
    }

    public readonly bool IsValid => Id != 0;

    public override string ToString() => $"ElementId({Id}, offset={Offset}, base={BaseId})";

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator ==(ElementId left, ElementId right) => left.Id == right.Id;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator !=(ElementId left, ElementId right) => left.Id != right.Id;

    public override bool Equals(object? obj) => obj is ElementId id && Id == id.Id;

    public override int GetHashCode() => (int)Id;
}
