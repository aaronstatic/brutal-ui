using System;
using System.Buffers;
using System.Runtime.CompilerServices;

namespace BrutalUi.Core;

/// <summary>Eight inline slots for class ids.</summary>
[InlineArray(ClassSet.InlineCapacity)]
internal struct ClassIdBuffer
{
    private ClassId _element0;
}

/// <summary>
/// The ordered set of class ids of an element. The first 8 ids live inline. The ninth id moves the set to a pooled array.
/// </summary>
/// <remarks>
/// <para>
/// This is a mutable struct. Keep it in a field, and change it only through the field or a
/// <c>ref</c> to the field. A change on a copy is lost.
/// </para>
/// <para>
/// The set keeps the order of add. It holds no duplicate. A full array rents an array of two times
/// the length, copies the ids, and returns the old array without a clear, because <see cref="ClassId"/> holds no reference.
/// The set stays on the array after a remove below 8. It never moves back to the inline slots.
/// </para>
/// </remarks>
internal struct ClassSet
{
    /// <summary>The number of ids that the inline buffer holds.</summary>
    internal const int InlineCapacity = 8;

    /// <summary>The length that the move to the array asks from the pool. The shared pool gives 16 slots.</summary>
    private const int OverflowCapacity = 16;

    private ClassIdBuffer _inline;
    private ClassId[]? _overflow;
    private int _count;

    /// <summary>Gets the number of ids in the set.</summary>
    public readonly int Count => _count;

    /// <summary>Gets the id at an index, in the order of add.</summary>
    /// <param name="index">The index, from 0 to <see cref="Count"/> minus 1.</param>
    public readonly ClassId this[int index]
    {
        get
        {
            ArgumentOutOfRangeException.ThrowIfNegative(index);
            ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(index, _count);
            return _overflow is null ? _inline[index] : _overflow[index];
        }
    }

    /// <summary>Checks whether the set holds an id.</summary>
    /// <param name="id">The id to find.</param>
    /// <returns><see langword="true"/> when the set holds the id.</returns>
    public readonly bool Contains(ClassId id)
    {
        return IndexOf(id) >= 0;
    }

    /// <summary>Adds an id at the end. A duplicate changes nothing.</summary>
    /// <param name="id">The id to add.</param>
    /// <returns><see langword="true"/> when the set changed. <see langword="false"/> for a duplicate.</returns>
    public bool Add(ClassId id)
    {
        if (IndexOf(id) >= 0)
        {
            return false;
        }

        if (_overflow is null)
        {
            if (_count < InlineCapacity)
            {
                _inline[_count] = id;
                _count++;
                return true;
            }

            // The ninth id: rent the array and copy the 8 inline ids.
            ClassId[] rented = ArrayPool<ClassId>.Shared.Rent(OverflowCapacity);
            ReadOnlySpan<ClassId> inline = _inline;
            inline.CopyTo(rented);
            _overflow = rented;
        }
        else if (_count == _overflow.Length)
        {
            Grow();
        }

        _overflow[_count] = id;
        _count++;
        return true;
    }

    /// <summary>Removes an id. The ids after it shift down, and the order stays.</summary>
    /// <param name="id">The id to remove.</param>
    /// <returns><see langword="true"/> when the set changed. <see langword="false"/> when the id is absent.</returns>
    public bool Remove(ClassId id)
    {
        int index = IndexOf(id);
        if (index < 0)
        {
            return false;
        }

        int last = _count - 1;
        if (_overflow is null)
        {
            Span<ClassId> inline = _inline;
            if (index < last)
            {
                inline.Slice(index + 1, last - index).CopyTo(inline.Slice(index));
            }

            inline[last] = default;
        }
        else
        {
            if (index < last)
            {
                Array.Copy(_overflow, index + 1, _overflow, index, last - index);
            }

            _overflow[last] = default;
        }

        _count = last;
        return true;
    }

    /// <summary>Finds the index of an id by a linear search.</summary>
    private readonly int IndexOf(ClassId id)
    {
        if (_overflow is null)
        {
            ReadOnlySpan<ClassId> inline = _inline;
            return inline.Slice(0, _count).IndexOf(id);
        }

        return new ReadOnlySpan<ClassId>(_overflow, 0, _count).IndexOf(id);
    }

    /// <summary>Rents an array of two times the length, copies the ids, and returns the old array.</summary>
    private void Grow()
    {
        ClassId[] old = _overflow!;
        ClassId[] next = ArrayPool<ClassId>.Shared.Rent(old.Length * 2);
        Array.Copy(old, next, _count);
        _overflow = next;
        ArrayPool<ClassId>.Shared.Return(old, clearArray: false);
    }
}
