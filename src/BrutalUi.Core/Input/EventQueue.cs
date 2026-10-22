using System;
using System.Runtime.InteropServices;
using Brutal.Numerics;

namespace BrutalUi.Core;

/// <summary>
/// The fields of a <see cref="PointerEvent"/> that the ring stores outside the header.
/// </summary>
internal struct PointerPayload
{
    /// <summary>The pointer position in the coordinates of the context.</summary>
    internal float2 Position;

    /// <summary>The button that goes down or up.</summary>
    internal PointerButton Button;

    /// <summary>The modifier keys that are down at the time of the event.</summary>
    internal UiModifiers Modifiers;
}

/// <summary>
/// The fields of a <see cref="ScrollEvent"/> that the ring stores outside the header.
/// </summary>
internal struct ScrollPayload
{
    /// <summary>The pointer position in the coordinates of the context.</summary>
    internal float2 Position;

    /// <summary>The scroll amount on the x axis and the y axis.</summary>
    internal float2 Delta;

    /// <summary>The modifier keys that are down at the time of the event.</summary>
    internal UiModifiers Modifiers;
}

/// <summary>
/// The fields of a <see cref="KeyEvent"/> that the ring stores outside the header.
/// </summary>
internal struct KeyPayload
{
    /// <summary>The GLFW key code value of the key.</summary>
    internal int Key;

    /// <summary>The platform scan code of the key.</summary>
    internal int ScanCode;

    /// <summary>The modifier keys that are down at the time of the event.</summary>
    internal UiModifiers Modifiers;

    /// <summary><see langword="true"/> when the event comes from a key repeat.</summary>
    internal bool IsRepeat;
}

/// <summary>
/// The fields of a <see cref="CharEvent"/> that the ring stores outside the header.
/// </summary>
internal struct CharPayload
{
    /// <summary>The Unicode scalar value of the character.</summary>
    internal uint CodePoint;
}

/// <summary>
/// The kind fields of a ring entry. The payload structs of all kinds overlap at offset 0.
/// </summary>
/// <remarks>
/// The payload structs hold no reference, so the overlap is legal. The CLR rejects an overlap of a
/// reference field and a value field. For this reason the target reference stays in the header, outside
/// the overlap. The type of the header tells which payload struct is valid.
/// </remarks>
[StructLayout(LayoutKind.Explicit)]
internal struct UiEventPayload
{
    /// <summary>The payload of a pointer event.</summary>
    [FieldOffset(0)]
    internal PointerPayload Pointer;

    /// <summary>The payload of a scroll event.</summary>
    [FieldOffset(0)]
    internal ScrollPayload Scroll;

    /// <summary>The payload of a key event.</summary>
    [FieldOffset(0)]
    internal KeyPayload Key;

    /// <summary>The payload of a char event.</summary>
    [FieldOffset(0)]
    internal CharPayload Char;
}

/// <summary>
/// One entry of the event ring: the header and the payload.
/// </summary>
/// <remarks>The type of the header is the tag of the payload.</remarks>
internal struct UiQueuedEvent
{
    /// <summary>The type, the timestamp, and the target of the event.</summary>
    internal UiEventHeader Header;

    /// <summary>The kind fields of the event.</summary>
    internal UiEventPayload Payload;
}

/// <summary>
/// A ring of queued events with a fixed capacity. A full ring drops the oldest entry and counts the drop.
/// </summary>
/// <remarks>
/// <para>
/// This is a mutable struct, in the pattern of <see cref="ElementStorage"/>. Keep it in a field, and
/// change it only through the field or a <c>ref</c> to the field. A change on a copy is lost.
/// </para>
/// <para>
/// The constructor creates the array, so a push and a pop never allocate. A pop writes the default entry
/// into the free slot, so the ring holds no element reference after the pop.
/// </para>
/// <para>A default instance has no array. Its count and its capacity are 0. Do not push to it.</para>
/// </remarks>
internal struct EventQueue
{
    private readonly UiQueuedEvent[] _items;
    private int _head;
    private int _count;
    private long _dropped;

    /// <summary>Creates a ring with a fixed capacity.</summary>
    /// <param name="capacity">The number of entries in the ring. The value must be 1 or more.</param>
    /// <exception cref="ArgumentOutOfRangeException">The capacity is 0 or less.</exception>
    public EventQueue(int capacity)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(capacity);
        _items = new UiQueuedEvent[capacity];
    }

    /// <summary>Gets the number of entries in the ring.</summary>
    public readonly int Count => _count;

    /// <summary>Gets the number of entries that the ring holds at most.</summary>
    public readonly int Capacity => _items is null ? 0 : _items.Length;

    /// <summary>Gets the number of entries that a push into a full ring dropped.</summary>
    public readonly long Dropped => _dropped;

    /// <summary>Gets all slots of the ring, popped slots included. This member serves the tests.</summary>
    public readonly ReadOnlySpan<UiQueuedEvent> Slots => _items;

    /// <summary>
    /// Adds an entry at the tail. A full ring drops the oldest entry, adds 1 to <see cref="Dropped"/>,
    /// and keeps the count.
    /// </summary>
    /// <param name="entry">The entry to add.</param>
    public void Push(in UiQueuedEvent entry)
    {
        UiQueuedEvent[] items = _items;
        int capacity = items.Length;
        if (_count == capacity)
        {
            // The tail slot is the head slot. The new entry overwrites the oldest entry.
            items[_head] = entry;
            _head = (_head + 1) % capacity;
            _dropped++;
            return;
        }

        items[(_head + _count) % capacity] = entry;
        _count++;
    }

    /// <summary>
    /// Removes the oldest entry. The free slot gets the default entry, so no element reference stays.
    /// </summary>
    /// <param name="entry">The oldest entry, or the default entry when the ring is empty.</param>
    /// <returns><see langword="true"/> when the ring had an entry.</returns>
    public bool TryPop(out UiQueuedEvent entry)
    {
        if (_count == 0)
        {
            entry = default;
            return false;
        }

        UiQueuedEvent[] items = _items;
        entry = items[_head];
        items[_head] = default;
        _head = (_head + 1) % items.Length;
        _count--;
        return true;
    }
}
