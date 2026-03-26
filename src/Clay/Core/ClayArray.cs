using System.Runtime.CompilerServices;

namespace Clay;

/// <summary>
/// A resizable array optimized for Clay's usage patterns.
/// Uses a backing array that can be reset without reallocating.
/// </summary>
public class ClayList<T> where T : struct
{
    private T[] _items;
    private int _length;

    public ClayList(int initialCapacity = 64)
    {
        _items = new T[initialCapacity];
        _length = 0;
    }

    public int Length
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _length;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        set => _length = value;
    }

    public int Capacity
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _items.Length;
    }

    public ref T this[int index]
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
#if DEBUG
            if ((uint)index >= (uint)_length)
                throw new IndexOutOfRangeException($"Index {index} is out of range [0, {_length})");
#endif
            return ref _items[index];
        }
    }

    /// <summary>
    /// Gets the value at the specified index, or default if out of range.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T GetValue(int index)
    {
        if ((uint)index >= (uint)_length)
            return default;
        return _items[index];
    }

    /// <summary>
    /// Adds an item and returns its index.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int Add(T item)
    {
        EnsureCapacity(_length + 1);
        _items[_length] = item;
        return _length++;
    }

    /// <summary>
    /// Adds an item and returns a reference to it.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref T AddRef(T item)
    {
        EnsureCapacity(_length + 1);
        _items[_length] = item;
        return ref _items[_length++];
    }

    /// <summary>
    /// Sets the value at the specified index, expanding if necessary.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Set(int index, T value)
    {
        EnsureCapacity(index + 1);
        _items[index] = value;
        if (index >= _length)
            _length = index + 1;
    }

    /// <summary>
    /// Removes the item at the specified index by swapping with the last item.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T RemoveSwapback(int index)
    {
        if ((uint)index >= (uint)_length)
            return default;

        _length--;
        T removed = _items[index];
        _items[index] = _items[_length];
        return removed;
    }

    /// <summary>
    /// Clears the list (resets length to 0).
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Clear() => _length = 0;

    /// <summary>
    /// Returns a Span view of the valid elements.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Span<T> AsSpan() => _items.AsSpan(0, _length);

    /// <summary>
    /// Returns a ReadOnlySpan view of the valid elements.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ReadOnlySpan<T> AsReadOnlySpan() => _items.AsSpan(0, _length);

    /// <summary>
    /// Gets the underlying array (use with caution).
    /// </summary>
    public T[] InternalArray => _items;

    public bool HasCapacity => _length < _items.Length;
    public bool IsEmpty => _length == 0;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void EnsureCapacity(int min)
    {
        if (_items.Length < min)
        {
            int newCapacity = _items.Length == 0 ? 64 : _items.Length * 2;
            if (newCapacity < min) newCapacity = min;
            Array.Resize(ref _items, newCapacity);
        }
    }
}

/// <summary>
/// A slice/view into a ClayList.
/// </summary>
public readonly struct ClaySlice<T> where T : struct
{
    private readonly T[] _array;
    private readonly int _start;
    public readonly int Length;

    public ClaySlice(T[] array, int start, int length)
    {
        _array = array;
        _start = start;
        Length = length;
    }

    public ref T this[int index]
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => ref _array[_start + index];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Span<T> AsSpan() => _array.AsSpan(_start, Length);
}
