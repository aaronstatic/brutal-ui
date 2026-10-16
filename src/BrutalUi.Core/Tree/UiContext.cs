using System;

namespace BrutalUi.Core;

/// <summary>
/// The owner of the root elements. <see cref="Update(float)"/> runs the passes on the dirty elements in frame order.
/// </summary>
/// <remarks>
/// <para>The tree is main-thread only. The context and the elements use no locks.</para>
/// <para>
/// An element is a root of one context at most. An element holds no reference to its context.
/// It holds a root flag instead. <see cref="AddRoot(Element)"/> throws for an element with the flag.
/// Remove a root from its context before you add it to another context or to a child list.
/// </para>
/// <para>
/// A root keeps its root flag until <see cref="RemoveRoot(Element)"/>.
/// Call <see cref="RemoveRoot(Element)"/> on each root before you drop a context.
/// </para>
/// </remarks>
public sealed class UiContext
{
    private ElementStorage _roots;

    /// <summary>Creates a context with no roots.</summary>
    public UiContext()
    {
    }

    /// <summary>Gets the root elements in the order of add.</summary>
    /// <remarks>
    /// The span is valid until the next change of the roots.
    /// After a growth, the span can read null or the children of another element.
    /// </remarks>
    public ReadOnlySpan<Element> Roots => _roots.AsSpan();

    /// <summary>
    /// Adds a root element at the end of the roots. The root keeps its dirty bits.
    /// </summary>
    /// <remarks>
    /// An element that has a parent leaves that parent first. The old parent gets the
    /// <see cref="DirtyFlags.Layout"/> bit.
    /// An element that is a root already throws, also when the root belongs to another context.
    /// </remarks>
    /// <param name="root">The element to add.</param>
    /// <exception cref="ArgumentNullException"><paramref name="root"/> is <see langword="null"/>.</exception>
    /// <exception cref="InvalidOperationException">The element is a root of this context or of another context.</exception>
    public void AddRoot(Element root)
    {
        ArgumentNullException.ThrowIfNull(root);

        if (root.IsRoot)
        {
            throw new InvalidOperationException("The element is a root already. Remove it from its UiContext first.");
        }

        root.Parent?.Children.Remove(root);
        _roots.Add(root);
        root.IsRoot = true;
    }

    /// <summary>Removes a root element. The element keeps its dirty bits.</summary>
    /// <param name="root">The element to remove.</param>
    /// <returns><see langword="true"/> when the context held the root. Otherwise <see langword="false"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="root"/> is <see langword="null"/>.</exception>
    public bool RemoveRoot(Element root)
    {
        ArgumentNullException.ThrowIfNull(root);

        int index = _roots.IndexOf(root);
        if (index < 0)
        {
            return false;
        }

        _roots.RemoveAt(index);
        root.IsRoot = false;
        return true;
    }

    /// <summary>
    /// Runs the passes on each root in frame order. A clean subtree costs one flag check.
    /// The last pass clears each dirty bit that remains.
    /// </summary>
    /// <param name="deltaTime">The time since the last update, in seconds. No pass reads it yet. The framework and the transitions read it later.</param>
    public void Update(float deltaTime)
    {
        ReadOnlySpan<Element> roots = _roots.AsSpan();

        // Seam: the style pass of the style-system epic goes here.
        // Seam: the layout pass of core-tree/flexbox-layout goes here.
        //       It walks DirtyFlags.Layout and clears the layout bits.

        // The clear pass. It clears the bits that no earlier pass cleared.
        for (int i = 0; i < roots.Length; i++)
        {
            ClearVisitor clear = default;
            DirtyWalk.Walk(roots[i], DirtyFlags.Style | DirtyFlags.Layout | DirtyFlags.Paint, ref clear);
        }
    }
}
