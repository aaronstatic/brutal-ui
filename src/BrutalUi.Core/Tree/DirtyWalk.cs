using System;
using System.Diagnostics;

namespace BrutalUi.Core;

/// <summary>
/// A pass that the dirty walk calls on each element with a self bit of the walked kinds.
/// </summary>
/// <remarks>
/// <para>Implement the visitor as a struct. The generic walk then calls it directly, with no boxing and no delegate.</para>
/// <para>
/// The rules for a visitor: a visitor must not change the tree. A visitor must not mark the kind that
/// its walk clears. It can mark another kind. A mark of the walked kind breaks the dirty-bit invariant,
/// because the walk clears the ancestors after the visit.
/// </para>
/// <para>
/// The walk calls the visitor only for an element with a self bit. A pass that must also run on an element
/// with only a descendant bit uses its own recursion and <c>ClearDirty</c>, or calls <c>Walk</c> with
/// <c>ClearVisitor</c> on the subtree after the pass.
/// </para>
/// </remarks>
internal interface IDirtyVisitor
{
    /// <summary>Runs the pass on one element.</summary>
    /// <param name="element">The element with a self bit of the walked kinds.</param>
    /// <param name="flags">The self bits of the element that the walk found. Each bit is a walked kind.</param>
    void Visit(Element element, DirtyFlags flags);
}

/// <summary>
/// The depth-first walk over the dirty bits of a tree.
/// </summary>
/// <remarks>
/// The walk enters a child only when the element has a descendant bit of a walked kind.
/// A clean subtree costs one flag check. The walk clears the walked bits on each element that it enters.
/// The recursion uses the call stack. It allocates nothing.
/// </remarks>
internal static class DirtyWalk
{
    /// <summary>
    /// Walks the tree below <paramref name="root"/>, calls the visitor on each element with a self bit
    /// of <paramref name="kinds"/>, and clears the self bits and the descendant bits of those kinds.
    /// </summary>
    /// <typeparam name="TVisitor">The visitor type. A struct gives a direct call.</typeparam>
    /// <param name="root">The element where the walk starts.</param>
    /// <param name="kinds">The kinds to walk. Use self bits only.</param>
    /// <param name="visitor">The visitor. The walk passes it by reference, so the visitor can keep state.</param>
    internal static void Walk<TVisitor>(Element root, DirtyFlags kinds, ref TVisitor visitor)
        where TVisitor : struct, IDirtyVisitor
    {
        Debug.Assert((kinds & ~DirtyBits.SelfMask) == DirtyFlags.None, "Walk the self bits only.");

        DirtyFlags descendantKinds = DirtyBits.ToDescendant(kinds);

        // A root with no bit of the kinds returns at once.
        if ((root.Dirty & (kinds | descendantKinds)) == DirtyFlags.None)
        {
            return;
        }

        WalkElement(root, kinds, descendantKinds, ref visitor);
    }

    private static void WalkElement<TVisitor>(Element element, DirtyFlags kinds, DirtyFlags descendantKinds, ref TVisitor visitor)
        where TVisitor : struct, IDirtyVisitor
    {
        // Step 1: read the self bits and the descendant bits of the walked kinds.
        DirtyFlags flags = element.Dirty;
        DirtyFlags self = flags & kinds;
        DirtyFlags desc = flags & descendantKinds;
        DirtyFlags walked = kinds | descendantKinds;

        // Step 2: visit the element when it has a self bit.
        if (self != DirtyFlags.None)
        {
            visitor.Visit(element, self);
        }

        // Step 3: enter the children when the element has a descendant bit. Skip a clean child.
        if (desc != DirtyFlags.None)
        {
            ReadOnlySpan<Element> children = element.ChildStorage.AsSpan();
            for (int i = 0; i < children.Length; i++)
            {
                Element child = children[i];
                if ((child.Dirty & walked) != DirtyFlags.None)
                {
                    WalkElement(child, kinds, descendantKinds, ref visitor);
                }
            }
        }

        // Step 4: clear the walked bits after the subtree, so the invariant holds at each step.
        element.ClearDirty(walked);
    }
}

/// <summary>
/// A visitor that does nothing. The clear pass uses it, because the walk itself clears the bits.
/// </summary>
internal readonly struct ClearVisitor : IDirtyVisitor
{
    /// <inheritdoc />
    public void Visit(Element element, DirtyFlags flags)
    {
    }
}
