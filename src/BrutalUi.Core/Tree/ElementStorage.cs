using System;
using System.Buffers;

namespace BrutalUi.Core;

/// <summary>
/// A pooled list of elements. <see cref="Element"/> uses it for the children.
/// <see cref="UiContext"/> uses it for the roots.
/// </summary>
/// <remarks>
/// This is a mutable struct. Keep it in a field, and change it only through the field or a
/// <c>ref</c> to the field. A change on a copy is lost.
/// The first add rents an array from <see cref="ArrayPool{T}.Shared"/>. A full array rents an
/// array of two times the length, copies the items, and returns the old array with a clear.
/// The storage never shrinks.
/// </remarks>
internal struct ElementStorage
{
    /// <summary>The length that the first add asks from the pool. The shared pool gives 16 slots.</summary>
    private const int InitialCapacity = 4;

    private Element[]? _items;
    private int _count;

    /// <summary>Gets the number of elements in the storage.</summary>
    public readonly int Count => _count;

    /// <summary>
    /// Returns the elements in order. The span is empty when the storage has no array.
    /// </summary>
    /// <remarks>The span is valid until the next change of the storage.</remarks>
    public readonly ReadOnlySpan<Element> AsSpan()
    {
        return _items is null ? ReadOnlySpan<Element>.Empty : new ReadOnlySpan<Element>(_items, 0, _count);
    }

    /// <summary>Adds an element at the end.</summary>
    /// <param name="element">The element to add.</param>
    public void Add(Element element)
    {
        if (_items is null)
        {
            _items = ArrayPool<Element>.Shared.Rent(InitialCapacity);
        }
        else if (_count == _items.Length)
        {
            Grow();
        }

        _items[_count] = element;
        _count++;
    }

    /// <summary>Inserts an element at an index. The tail shifts up by one.</summary>
    /// <param name="index">The index for the element, from 0 to <see cref="Count"/>.</param>
    /// <param name="element">The element to insert.</param>
    /// <exception cref="ArgumentOutOfRangeException">The index is not in the range 0 to <see cref="Count"/>.</exception>
    public void Insert(int index, Element element)
    {
        // The check runs before any change.
        ArgumentOutOfRangeException.ThrowIfNegative(index);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(index, _count);

        if (_items is null)
        {
            _items = ArrayPool<Element>.Shared.Rent(InitialCapacity);
        }
        else if (_count == _items.Length)
        {
            Grow();
        }

        // Span.CopyTo handles an overlap of the source and the destination.
        Element[] items = _items;
        if (index < _count)
        {
            items.AsSpan(index, _count - index).CopyTo(items.AsSpan(index + 1));
        }

        items[index] = element;
        _count++;
    }

    /// <summary>
    /// Moves the element at one index to another index. The elements between the two indexes shift by one.
    /// </summary>
    /// <param name="fromIndex">The index of the element to move, from 0 to <see cref="Count"/> minus 1.</param>
    /// <param name="toIndex">The index that the element has after the move, from 0 to <see cref="Count"/> minus 1.</param>
    /// <exception cref="ArgumentOutOfRangeException">An index is not in the range 0 to <see cref="Count"/> minus 1.</exception>
    public void Move(int fromIndex, int toIndex)
    {
        // The checks run before any change.
        ArgumentOutOfRangeException.ThrowIfNegative(fromIndex);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(fromIndex, _count);
        ArgumentOutOfRangeException.ThrowIfNegative(toIndex);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(toIndex, _count);

        if (fromIndex == toIndex)
        {
            return;
        }

        Element[] items = _items!;
        Element moved = items[fromIndex];
        if (fromIndex < toIndex)
        {
            items.AsSpan(fromIndex + 1, toIndex - fromIndex).CopyTo(items.AsSpan(fromIndex));
        }
        else
        {
            items.AsSpan(toIndex, fromIndex - toIndex).CopyTo(items.AsSpan(toIndex + 1));
        }

        items[toIndex] = moved;
    }

    /// <summary>
    /// Removes the element at an index. The tail shifts down by one. The free slot gets
    /// <see langword="null"/>, so the array holds no stale reference.
    /// </summary>
    /// <param name="index">The index of the element to remove.</param>
    /// <exception cref="ArgumentOutOfRangeException">The index is not in the range 0 to <see cref="Count"/> minus 1.</exception>
    public void RemoveAt(int index)
    {
        // This check also stops a silent remove of the last item for an index past the count.
        ArgumentOutOfRangeException.ThrowIfNegative(index);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(index, _count);

        Element[] items = _items!;
        int last = _count - 1;
        if (index < last)
        {
            Array.Copy(items, index + 1, items, index, last - index);
        }

        // The slot at the old end is free. The array type has no null slot, so the code forces it.
        items[last] = null!;
        _count = last;
    }

    /// <summary>Finds the index of an element. The search compares references.</summary>
    /// <param name="element">The element to find.</param>
    /// <returns>The index of the element, or -1 when the storage does not hold it.</returns>
    public readonly int IndexOf(Element element)
    {
        ReadOnlySpan<Element> items = AsSpan();
        for (int i = 0; i < items.Length; i++)
        {
            if (ReferenceEquals(items[i], element))
            {
                return i;
            }
        }

        return -1;
    }

    /// <summary>
    /// Rents an array of two times the length, copies the items, and returns the old array.
    /// </summary>
    private void Grow()
    {
        Element[] old = _items!;
        Element[] next = ArrayPool<Element>.Shared.Rent(old.Length * 2);
        Array.Copy(old, next, _count);
        _items = next;
        ArrayPool<Element>.Shared.Return(old, clearArray: true);
    }
}
