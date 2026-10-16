using System;

namespace BrutalUi.Core;

/// <summary>
/// The ordered child list of an element. The list is a view on the storage of its owner.
/// </summary>
/// <remarks>
/// <para>
/// A change through the list changes the owner, keeps the parent links correct, and sets the dirty bits.
/// The list allocates nothing after warm-up.
/// </para>
/// <para>
/// Do not change the list during a <c>foreach</c> on the list. The enumerator reads the storage
/// of the owner. A change can move the items or replace the array.
/// After a growth, the enumerator can read null or the children of another element.
/// </para>
/// <para>
/// Get a list from <see cref="Element.Children"/>. A default instance has no owner. A call on it throws.
/// </para>
/// </remarks>
public readonly struct ElementList
{
    private readonly Element _owner;

    /// <summary>Creates a view on the child list of an element.</summary>
    /// <param name="owner">The element that owns the children.</param>
    internal ElementList(Element owner)
    {
        _owner = owner;
    }

    /// <summary>Gets the number of children.</summary>
    public int Count => _owner.ChildStorage.Count;

    /// <summary>Gets the child at an index.</summary>
    /// <param name="index">The index of the child, from 0 to <see cref="Count"/> minus 1.</param>
    /// <returns>The child at the index.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The index is not in the range 0 to <see cref="Count"/> minus 1.</exception>
    public Element this[int index]
    {
        get
        {
            ReadOnlySpan<Element> items = _owner.ChildStorage.AsSpan();
            ArgumentOutOfRangeException.ThrowIfNegative(index);
            ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(index, items.Length);
            return items[index];
        }
    }

    /// <summary>
    /// Adds a child at the end of the list.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The owner gets the <see cref="DirtyFlags.Layout"/> bit. The child gets the <see cref="DirtyFlags.Style"/> bit.
    /// The bits of the child go up the new parent chain as descendant bits.
    /// </para>
    /// <para>
    /// A child that has a parent leaves that parent first. The old parent gets the
    /// <see cref="DirtyFlags.Layout"/> bit. A child of this owner moves to the end of the list.
    /// </para>
    /// </remarks>
    /// <param name="child">The element to add.</param>
    /// <exception cref="ArgumentNullException"><paramref name="child"/> is <see langword="null"/>.</exception>
    /// <exception cref="InvalidOperationException">
    /// The child is the owner or an ancestor of the owner, or the child is a root of a <see cref="UiContext"/>.
    /// A root keeps its root flag until <see cref="UiContext.RemoveRoot(Element)"/>.
    /// Call <see cref="UiContext.RemoveRoot(Element)"/> on each root before you drop a context.
    /// </exception>
    public void Add(Element child)
    {
        ValidateChild(child);
        LeaveParent(child);
        _owner.ChildStorage.Add(child);
        child.AttachToParent(_owner);
        _owner.MarkDirty(DirtyFlags.Layout);
    }

    /// <summary>
    /// Inserts a child at an index. The children from that index shift up by one.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The marks are the same as the marks of <see cref="Add(Element)"/>.
    /// A child that has a parent leaves that parent first.
    /// </para>
    /// <para>
    /// When the child is already a child of this owner, the index refers to the list after the remove.
    /// The valid range is then 0 to <see cref="Count"/> minus 1.
    /// A bad index throws before any change.
    /// </para>
    /// </remarks>
    /// <param name="index">The index for the child, from 0 to <see cref="Count"/>.</param>
    /// <param name="child">The element to insert.</param>
    /// <exception cref="ArgumentNullException"><paramref name="child"/> is <see langword="null"/>.</exception>
    /// <exception cref="InvalidOperationException">
    /// The child is the owner or an ancestor of the owner, or the child is a root of a <see cref="UiContext"/>.
    /// A root keeps its root flag until <see cref="UiContext.RemoveRoot(Element)"/>.
    /// Call <see cref="UiContext.RemoveRoot(Element)"/> on each root before you drop a context.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">The index is not in the valid range.</exception>
    public void Insert(int index, Element child)
    {
        ValidateChild(child);

        // The list after the remove holds one child less when the child is already in this list.
        int count = ReferenceEquals(child.Parent, _owner) ? Count - 1 : Count;
        ArgumentOutOfRangeException.ThrowIfNegative(index);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(index, count);

        LeaveParent(child);
        _owner.ChildStorage.Insert(index, child);
        child.AttachToParent(_owner);
        _owner.MarkDirty(DirtyFlags.Layout);
    }

    /// <summary>
    /// Moves a child from one index to another index in the same list.
    /// The owner gets the <see cref="DirtyFlags.Layout"/> bit.
    /// </summary>
    /// <remarks>A move with the same two indexes changes nothing and marks nothing.</remarks>
    /// <param name="fromIndex">The index of the child to move, from 0 to <see cref="Count"/> minus 1.</param>
    /// <param name="toIndex">The index that the child has after the move, from 0 to <see cref="Count"/> minus 1.</param>
    /// <exception cref="ArgumentOutOfRangeException">An index is not in the range 0 to <see cref="Count"/> minus 1.</exception>
    public void Move(int fromIndex, int toIndex)
    {
        // The storage checks both indexes before any change.
        _owner.ChildStorage.Move(fromIndex, toIndex);
        if (fromIndex != toIndex)
        {
            _owner.MarkDirty(DirtyFlags.Layout);
        }
    }

    /// <summary>
    /// Removes a child from the list. The owner gets the <see cref="DirtyFlags.Layout"/> bit.
    /// The child keeps its dirty bits and loses its parent.
    /// </summary>
    /// <param name="child">The element to remove.</param>
    /// <returns><see langword="true"/> when the list held the child. Otherwise <see langword="false"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="child"/> is <see langword="null"/>.</exception>
    public bool Remove(Element child)
    {
        ArgumentNullException.ThrowIfNull(child);

        int index = _owner.ChildStorage.IndexOf(child);
        if (index < 0)
        {
            return false;
        }

        RemoveAt(index);
        return true;
    }

    /// <summary>
    /// Removes the child at an index. The owner gets the <see cref="DirtyFlags.Layout"/> bit.
    /// The child keeps its dirty bits and loses its parent.
    /// </summary>
    /// <param name="index">The index of the child, from 0 to <see cref="Count"/> minus 1.</param>
    /// <exception cref="ArgumentOutOfRangeException">The index is not in the range 0 to <see cref="Count"/> minus 1.</exception>
    public void RemoveAt(int index)
    {
        ref ElementStorage storage = ref _owner.ChildStorage;

        // The checks run before any change.
        ArgumentOutOfRangeException.ThrowIfNegative(index);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(index, storage.Count);
        Element child = storage.AsSpan()[index];
        storage.RemoveAt(index);
        child.DetachFromParent();
        _owner.MarkDirty(DirtyFlags.Layout);
    }

    /// <summary>Finds the index of a child. The search compares references.</summary>
    /// <param name="child">The element to find.</param>
    /// <returns>The index of the child, or -1 when the list does not hold it.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="child"/> is <see langword="null"/>.</exception>
    public int IndexOf(Element child)
    {
        ArgumentNullException.ThrowIfNull(child);
        return _owner.ChildStorage.IndexOf(child);
    }

    /// <summary>Tells if the list holds a child. The search compares references.</summary>
    /// <param name="child">The element to find.</param>
    /// <returns><see langword="true"/> when the list holds the child. Otherwise <see langword="false"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="child"/> is <see langword="null"/>.</exception>
    public bool Contains(Element child)
    {
        ArgumentNullException.ThrowIfNull(child);
        return _owner.ChildStorage.IndexOf(child) >= 0;
    }

    /// <summary>
    /// Returns an enumerator over the children in order. A <c>foreach</c> on the list uses it and allocates nothing.
    /// </summary>
    /// <remarks>Do not change the list during the <c>foreach</c>.</remarks>
    /// <returns>An enumerator over the current children.</returns>
    public ReadOnlySpan<Element>.Enumerator GetEnumerator() => _owner.ChildStorage.AsSpan().GetEnumerator();

    /// <summary>
    /// Checks a child before an add or an insert. The method changes nothing.
    /// </summary>
    /// <remarks>
    /// The walk goes up from the owner. The cost is the depth of the owner.
    /// The method allocates nothing when the checks pass.
    /// </remarks>
    /// <param name="child">The element that is about to join this list.</param>
    private void ValidateChild(Element child)
    {
        ArgumentNullException.ThrowIfNull(child);

        if (child.IsRoot)
        {
            throw new InvalidOperationException("The child is a root of a UiContext. Remove it from its UiContext first.");
        }

        for (Element? p = _owner; p is not null; p = p.Parent)
        {
            if (ReferenceEquals(p, child))
            {
                throw new InvalidOperationException("The child is the owner or an ancestor of the owner. This add makes a cycle.");
            }
        }
    }

    /// <summary>
    /// Removes a child from its current parent, when it has one. This is the "Remove" row of the structural marks.
    /// </summary>
    /// <param name="child">The element that is about to join this list.</param>
    private static void LeaveParent(Element child)
    {
        Element? oldParent = child.Parent;
        oldParent?.Children.Remove(child);
    }
}
