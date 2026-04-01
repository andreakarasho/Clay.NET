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
    /// The entire string is hashed. Use "##" in labels to separate display text from
    /// the unique ID portion — the display layer will only show text before "##".
    /// For example, "Save##1" and "Save##2" produce different IDs but both display "Save".
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
    /// Creates an ElementId by hashing a string prefix combined with a uint suffix,
    /// without allocating a temporary interpolated string.
    /// For example, HashComposite("SbTrackV_", 42) is equivalent to Hash($"SbTrackV_42")
    /// but avoids the string allocation.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ElementId HashComposite(ReadOnlySpan<char> prefix, uint suffix, uint offset = 0, uint seed = 0)
    {
        uint baseHash = seed;

        // Hash the prefix characters
        for (int i = 0; i < prefix.Length; i++)
        {
            baseHash += prefix[i];
            baseHash += baseHash << 10;
            baseHash ^= baseHash >> 6;
        }

        // Hash the suffix digits (same char sequence as uint.ToString())
        // Build digits in reverse, then hash in forward order
        Span<char> digits = stackalloc char[10]; // uint.MaxValue is 10 digits
        int digitCount = 0;
        if (suffix == 0)
        {
            digits[0] = '0';
            digitCount = 1;
        }
        else
        {
            uint temp = suffix;
            while (temp > 0)
            {
                digits[digitCount++] = (char)('0' + temp % 10);
                temp /= 10;
            }
            // Reverse
            for (int i = 0, j = digitCount - 1; i < j; i++, j--)
            {
                (digits[i], digits[j]) = (digits[j], digits[i]);
            }
        }

        for (int i = 0; i < digitCount; i++)
        {
            baseHash += digits[i];
            baseHash += baseHash << 10;
            baseHash ^= baseHash >> 6;
        }

        uint hash = baseHash;
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
            Id = hash + 1,
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

    /// <summary>
    /// Returns the display portion of a label that may contain "##".
    /// Everything after the first "##" is hidden.
    /// For example, "Save##1" returns "Save", "NoHash" returns "NoHash".
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ReadOnlySpan<char> GetDisplayLabel(ReadOnlySpan<char> label)
    {
        for (int i = 0; i < label.Length - 1; i++)
        {
            if (label[i] == '#' && label[i + 1] == '#')
                return label[..i];
        }
        return label;
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
