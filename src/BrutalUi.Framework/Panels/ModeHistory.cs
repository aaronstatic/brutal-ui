using System;
using BrutalUi.Core;

namespace BrutalUi.Framework;

/// <summary>
/// The ring of the modes that each apply replaced. <see cref="Back"/> sets the newest entry as the target of the
/// manager, and a game has no forward step.
/// </summary>
/// <remarks>
/// <para>
/// The history rule: the history holds the mode that each apply replaced. The first apply adds nothing. A target
/// that equals the current mode adds nothing.
/// </para>
/// <para>
/// <see cref="Back"/> sets the newest entry as the target of the manager, drops it, and returns
/// <see langword="true"/>. The next <see cref="UiContext.Update(float)"/> applies it, and that apply adds no entry
/// (A3, no forward step). A <see cref="UiManager.SetMode(ReadOnlySpan{byte})"/> after <see cref="Back"/> in the same
/// frame wins, and the dropped entry stays dropped.
/// </para>
/// <para>
/// The ring rule: a full ring drops the oldest entry. A mode never unregisters, so a stored index stays valid.
/// </para>
/// <para>
/// The allocation rule: <see cref="Back"/>, the push, and <see cref="Clear"/> allocate nothing. The constructor of
/// the manager creates the ring array, as <see cref="UiContext"/> creates its event ring.
/// </para>
/// <para>The thread rule: the history runs on the UI thread and has no thread guard, as <see cref="UiManager"/> has
/// none.</para>
/// </remarks>
public sealed class ModeHistory
{
    /// <summary>The message of a back step during the mode diff.</summary>
    private const string ApplyingMessage = "The back step must not run during the mode diff.";

    /// <summary>The manager whose target a back step sets.</summary>
    private readonly UiManager _manager;

    /// <summary>The ring array. The constructor creates it, so a push and a pop never allocate.</summary>
    private readonly int[] _entries;

    /// <summary>The index of the oldest entry in the ring.</summary>
    private int _head;

    /// <summary>The count of used entries.</summary>
    private int _count;

    /// <summary>
    /// Creates a history ring with a fixed capacity. The manager creates the one instance.
    /// </summary>
    /// <param name="manager">The manager whose target a back step sets.</param>
    /// <param name="capacity">The count of entries in the ring. The value must be 1 or more.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="capacity"/> is 0 or less.</exception>
    internal ModeHistory(UiManager manager, int capacity)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(capacity);
        _manager = manager;
        _entries = new int[capacity];
    }

    /// <summary>Gets the count of modes that the history holds.</summary>
    public int Count => _count;

    /// <summary>Gets the greatest count of entries that the ring holds.</summary>
    public int Capacity => _entries.Length;

    /// <summary>
    /// Sets the newest entry as the target of the manager, drops it, and returns <see langword="true"/>. An empty
    /// history returns <see langword="false"/> and changes no target.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The next <see cref="UiContext.Update(float)"/> applies the target, and that apply adds no entry, so there is
    /// no forward step (A3). A <see cref="UiManager.SetMode(ReadOnlySpan{byte})"/> after this call in the same frame
    /// wins, and the dropped entry stays dropped. The call allocates nothing.
    /// </para>
    /// <para>
    /// The check rule: a call during the mode diff throws <see cref="InvalidOperationException"/> before it pops an
    /// entry, so the history keeps its entries.
    /// </para>
    /// </remarks>
    /// <returns><see langword="true"/> when the history had an entry; else <see langword="false"/>.</returns>
    /// <exception cref="InvalidOperationException">The manager runs the mode diff.</exception>
    public bool Back()
    {
        if (_manager.IsApplying)
        {
            throw new InvalidOperationException(ApplyingMessage);
        }

        if (_count == 0)
        {
            return false;
        }

        _count--;
        int mode = _entries[(_head + _count) % _entries.Length];
        _manager.SetBackTarget(mode);
        return true;
    }

    /// <summary>
    /// Empties the history. The call allocates nothing.
    /// </summary>
    public void Clear()
    {
        _count = 0;
    }

    /// <summary>
    /// Writes the newest entry, and drops the oldest entry when the ring is full. The manager calls it at the end
    /// of each apply that replaces a mode, except the apply of a back step. The call allocates nothing.
    /// </summary>
    /// <param name="mode">The index of the mode entry that the apply replaced.</param>
    internal void Push(int mode)
    {
        if (_count == _entries.Length)
        {
            // The tail slot is the head slot. The new entry overwrites the oldest entry, and the head moves on.
            _entries[_head] = mode;
            _head = (_head + 1) % _entries.Length;
            return;
        }

        _entries[(_head + _count) % _entries.Length] = mode;
        _count++;
    }
}
