using System;

namespace BrutalUi.Core;

/// <summary>
/// The ordered class list of an element. The list is a view on the storage of its owner.
/// </summary>
/// <remarks>
/// <para>
/// A change that alters the set marks the owner and its full subtree with <see cref="DirtyFlags.Style"/>, because a
/// chain rule of a descendant can read the classes of the owner. Each element of the subtree with a child also gets
/// <see cref="DirtyFlags.DescendantStyle"/>, and each ancestor of the owner gets <see cref="DirtyFlags.DescendantStyle"/>.
/// The mark reads no sheet. A duplicate add and a remove of an absent id mark nothing.
/// </para>
/// <para>
/// A class change on a high ancestor restyles a large subtree on the next <see cref="UiContext.Update(float)"/>.
/// The list and the mark allocate nothing after warm-up.
/// </para>
/// <para>
/// Get a list from <see cref="Element.Classes"/>. A default instance has no owner. A call on it throws.
/// </para>
/// </remarks>
public readonly struct ClassList
{
    private readonly Element _owner;

    /// <summary>Creates a view on the class list of an element.</summary>
    /// <param name="owner">The element that owns the classes.</param>
    internal ClassList(Element owner)
    {
        _owner = owner;
    }

    /// <summary>Gets the number of classes.</summary>
    public int Count => _owner.ClassStorage.Count;

    /// <summary>Gets the class id at an index, in the order of add.</summary>
    /// <param name="index">The index, from 0 to <see cref="Count"/> minus 1.</param>
    /// <exception cref="ArgumentOutOfRangeException">The index is less than 0 or not less than <see cref="Count"/>.</exception>
    public ClassId this[int index] => _owner.ClassStorage[index];

    /// <summary>Adds a class at the end of the list.</summary>
    /// <param name="id">The class id to add.</param>
    /// <returns><see langword="true"/> when the list changed. <see langword="false"/> for a duplicate.</returns>
    /// <exception cref="ArgumentException">The id is <see cref="ClassId.None"/>.</exception>
    public bool Add(ClassId id)
    {
        if (id.IsNone)
        {
            throw new ArgumentException("The id is ClassId.None. Get an id from ClassName.Intern.", nameof(id));
        }

        if (!_owner.ClassStorage.Add(id))
        {
            return false;
        }

        _owner.MarkSubtreeStyle();
        return true;
    }

    /// <summary>Removes a class from the list. The order of the other classes stays.</summary>
    /// <param name="id">The class id to remove.</param>
    /// <returns><see langword="true"/> when the list changed. <see langword="false"/> when the id is absent.</returns>
    public bool Remove(ClassId id)
    {
        if (!_owner.ClassStorage.Remove(id))
        {
            return false;
        }

        _owner.MarkSubtreeStyle();
        return true;
    }

    /// <summary>Checks whether the list holds a class.</summary>
    /// <param name="id">The class id to find.</param>
    /// <returns><see langword="true"/> when the list holds the id.</returns>
    public bool Contains(ClassId id) => _owner.ClassStorage.Contains(id);
}
