using System;

namespace BrutalUi.Core;

/// <summary>
/// The binding pass. It calls the handler of each binding whose source holds a new version.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="UiContext.Update(float)"/> runs the pass after the input pass and before the style pass. Thus a value
/// that an input handler writes into a source reaches the elements in the same update, and the style pass reads the
/// new values.
/// </para>
/// <para>
/// The skip rule: the pass enters an element only when <see cref="Element.HasBoundSubtree"/> is
/// <see langword="true"/>. A branch with no binding costs one flag check, and a tree with no binding costs one flag
/// check for each root. After the visit of an element, the pass writes the flag again from the own binding count and
/// the flags of the children, so a stale flag from a remove goes away.
/// </para>
/// <para>
/// The order: the pass visits a parent before its children, and the children in the order of the child list. It
/// applies the bindings of one element in the order of add. For each binding, the pass compares the version of the
/// source with the applied version. On a difference, it writes the applied version first and then calls the handler
/// (Decision 7). Thus an exception of the handler leaves the binding applied, and the next update does not call the
/// handler again for the same version.
/// </para>
/// <para>
/// The pass marks no dirty bit itself. Each handler writes through a setter that marks the bits of its change. The
/// pass allocates nothing. The recursion depth is the depth of the tree, as in <see cref="StylePass"/>.
/// </para>
/// </remarks>
internal static class BindingPass
{
    /// <summary>Runs the binding pass on each root and its subtree, in the order of the roots.</summary>
    /// <param name="roots">The roots of the context.</param>
    internal static void Run(ReadOnlySpan<Element> roots)
    {
        for (int i = 0; i < roots.Length; i++)
        {
            Visit(roots[i]);
        }
    }

    /// <summary>
    /// Applies the changed bindings of one element, then visits each child, then writes the subtree flag again.
    /// </summary>
    /// <param name="element">The element to visit.</param>
    private static void Visit(Element element)
    {
        if (!element.HasBoundSubtree)
        {
            return;
        }

        // Step 1: apply each binding with a new version, in the order of add. The loop reads the count and the slot
        // again after each handler call, so the loop never holds a reference into an old array.
        for (int i = 0; i < element.BindingCount; i++)
        {
            ref ElementBinding binding = ref element.BindingAt(i);
            ulong version = binding.Source.Version;
            if (version == binding.AppliedVersion)
            {
                continue;
            }

            binding.AppliedVersion = version;
            binding.Handler(element, binding.Target);
        }

        // Step 2: visit each child. A child with no bound subtree costs one flag check.
        bool bound = element.BindingCount > 0;
        ReadOnlySpan<Element> children = element.ChildStorage.AsSpan();
        for (int i = 0; i < children.Length; i++)
        {
            Element child = children[i];
            Visit(child);
            bound |= child.HasBoundSubtree;
        }

        // Step 3: write the flag again. This clears a stale flag after a remove.
        element.HasBoundSubtree = bound;
    }
}
