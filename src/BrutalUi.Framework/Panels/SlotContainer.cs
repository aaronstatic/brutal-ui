using System;
using System.Text;
using BrutalUi.Core;

namespace BrutalUi.Framework;

/// <summary>
/// An element that holds the panel roots of one slot as its children, in the order of the mode definition.
/// </summary>
/// <remarks>
/// <para>
/// A game puts a slot container in its layout tree, one for each slot name of its layouts. The mode switch mounts the
/// panel roots of the active mode into the slot, one root at each index.
/// </para>
/// <para>
/// The style pass and the layout pass treat a slot container as a plain <see cref="Element"/>.
/// A stylesheet styles it by the type name <c>SlotContainer</c>, by class, or by name.
/// </para>
/// <para>
/// The constructor copies the slot name and sets <see cref="Element.Name"/> to the text of the slot name.
/// A later set of <see cref="Element.Name"/> changes the style match and does not change <see cref="SlotName"/>.
/// </para>
/// <para>
/// Change the children of a slot container through <see cref="Mount(Element, int)"/> and
/// <see cref="Unmount(Element)"/> only. A direct call on <see cref="Element.Children"/> works, but it skips the parent rule.
/// </para>
/// <para>
/// A slot container runs on the UI thread and has no thread guard, as <see cref="Element"/> does.
/// Each call allocates 0 bytes after the child array of the slot holds the largest count.
/// </para>
/// </remarks>
public sealed class SlotContainer : Element
{
    /// <summary>The error text for a root that another slot holds.</summary>
    private const string HeldByOtherSlotMessage = "The root is in another slot. Unmount it from that slot first.";

    /// <summary>The error text for a root that has another parent.</summary>
    private const string OtherParentMessage = "The root has another parent. Remove it from its parent first.";

    /// <summary>The error text for an empty slot name.</summary>
    private const string EmptyNameMessage = "The slot name is empty.";

    /// <summary>The type id of each slot container.</summary>
    private static readonly ElementTypeId s_typeId = ElementType.Intern("SlotContainer");

    /// <summary>The copy of the slot name.</summary>
    private readonly byte[] _slotName;

    /// <summary>Creates a slot container with a name.</summary>
    /// <remarks>
    /// The constructor copies the name. A change of the source after the call changes nothing.
    /// The constructor also sets <see cref="Element.Name"/> to the text of the name. This is a build-time allocation.
    /// A byte sequence that is not valid UTF-8 becomes U+FFFD in <see cref="Element.Name"/>, so a name rule does not
    /// match such a slot, but <see cref="Find(Element, ReadOnlySpan{byte})"/> still finds it by <see cref="SlotName"/>.
    /// </remarks>
    /// <param name="name">The UTF-8 name of the slot. The name must not be empty.</param>
    /// <exception cref="ArgumentException"><paramref name="name"/> is empty.</exception>
    public SlotContainer(ReadOnlySpan<byte> name)
    {
        if (name.IsEmpty)
        {
            throw new ArgumentException(EmptyNameMessage, nameof(name));
        }

        _slotName = name.ToArray();
        Name = Encoding.UTF8.GetString(name);
    }

    /// <summary>Gets the UTF-8 name of the slot. The span covers the copy that the constructor made.</summary>
    /// <remarks>The read allocates 0 bytes.</remarks>
    public ReadOnlySpan<byte> SlotName => _slotName;

    /// <inheritdoc/>
    public override ElementTypeId TypeId => s_typeId;

    /// <summary>Puts a panel root at an index of the slot.</summary>
    /// <remarks>
    /// <para>
    /// A root with no parent joins the slot at the index. The valid range is 0 to <see cref="ElementList.Count"/>.
    /// The slot gets the <see cref="DirtyFlags.Layout"/> bit. The root and its descendants get the
    /// <see cref="DirtyFlags.Style"/> bit.
    /// </para>
    /// <para>
    /// A root that the slot holds moves to the index. The valid range is then 0 to <see cref="ElementList.Count"/>
    /// minus 1. The move is no join, so the root gets no <see cref="DirtyFlags.Style"/> bit. A move to the same index
    /// marks nothing.
    /// </para>
    /// <para>
    /// A root that has another parent throws. This includes a root that another slot holds. Unmount or remove the root
    /// from that parent first.
    /// </para>
    /// <para>
    /// Each throw comes before a change. A failed mount leaves the slot, the other parent, and the root unchanged.
    /// </para>
    /// <para>The call allocates 0 bytes after the child array of the slot holds the largest count.</para>
    /// </remarks>
    /// <param name="root">The panel root.</param>
    /// <param name="index">The index in the slot.</param>
    /// <exception cref="ArgumentNullException"><paramref name="root"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// The index is not in the valid range. The <see cref="ArgumentException.ParamName"/> is <c>index</c>. The range is 0 to
    /// <see cref="ElementList.Count"/> for a new root, and 0 to <see cref="ElementList.Count"/> minus 1 for a held root.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// The root has another parent, the root is a root of a <see cref="UiContext"/>, or the root is the slot or an
    /// ancestor of the slot.
    /// </exception>
    public void Mount(Element root, int index)
    {
        ArgumentNullException.ThrowIfNull(root);

        Element? parent = root.Parent;
        if (ReferenceEquals(parent, this))
        {
            // The slot checks the index before a change, so ParamName is index, as for a new root.
            // A move to the same index marks nothing.
            ArgumentOutOfRangeException.ThrowIfNegative(index);
            ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(index, Children.Count);
            Children.Move(Children.IndexOf(root), index);
            return;
        }

        if (parent is not null)
        {
            throw new InvalidOperationException(parent is SlotContainer ? HeldByOtherSlotMessage : OtherParentMessage);
        }

        // The list checks the root and the index before a change.
        Children.Insert(index, root);
    }

    /// <summary>Detaches a panel root from the slot.</summary>
    /// <remarks>
    /// <para>
    /// The slot drops its reference and gets the <see cref="DirtyFlags.Layout"/> bit. The root keeps its subtree, its
    /// dirty bits, its classes, its inline style, its name, and its bindings. The caller keeps the root alive.
    /// At the next <c>Update</c>, Core clears the hover, the active state, and the focus of the subtree, and drops each
    /// running tween with no write.
    /// </para>
    /// <para>A root that the slot does not hold gives <see langword="false"/>. Another slot keeps its root.</para>
    /// <para>The call allocates 0 bytes.</para>
    /// </remarks>
    /// <param name="root">The panel root.</param>
    /// <returns><see langword="true"/> when the slot held the root. Otherwise <see langword="false"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="root"/> is <see langword="null"/>.</exception>
    public bool Unmount(Element root)
    {
        ArgumentNullException.ThrowIfNull(root);
        return Children.Remove(root);
    }

    /// <summary>Finds the slot container of a name in a layout tree.</summary>
    /// <remarks>
    /// <para>
    /// The walk checks the root itself, then each child in depth-first pre-order. It does not enter the children of a
    /// slot container, so the result depends on the layout tree only. The mounted panels change neither the cost nor
    /// the result.
    /// </para>
    /// <para>
    /// The first match wins. Two slots with one name are a bug of the layout.
    /// </para>
    /// <para>
    /// The compare is by bytes, so <c>left</c> and <c>Left</c> are two names. The call reads <see cref="SlotName"/>
    /// and not <see cref="Element.Name"/>.
    /// </para>
    /// <para>
    /// An empty name or a missing name gives <see langword="null"/>.
    /// </para>
    /// <para>
    /// The walk allocates 0 bytes. The mode switch can call it at build time and at the first diff, and cache the result.
    /// </para>
    /// </remarks>
    /// <param name="root">The root of the layout tree.</param>
    /// <param name="name">The UTF-8 name of the slot.</param>
    /// <returns>The first slot container of the name, or <see langword="null"/> when the tree holds none.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="root"/> is <see langword="null"/>.</exception>
    public static SlotContainer? Find(Element root, ReadOnlySpan<byte> name)
    {
        ArgumentNullException.ThrowIfNull(root);
        return Walk(root, name);
    }

    /// <summary>Walks a subtree in depth-first pre-order and stops at the first slot container of the name.</summary>
    /// <param name="element">The element to check.</param>
    /// <param name="name">The UTF-8 name of the slot.</param>
    /// <returns>The slot container, or <see langword="null"/>.</returns>
    private static SlotContainer? Walk(Element element, ReadOnlySpan<byte> name)
    {
        if (element is SlotContainer slot)
        {
            // The walk does not enter the children of a slot.
            return slot._slotName.AsSpan().SequenceEqual(name) ? slot : null;
        }

        foreach (Element child in element.Children)
        {
            if (Walk(child, name) is SlotContainer hit)
            {
                return hit;
            }
        }

        return null;
    }
}
