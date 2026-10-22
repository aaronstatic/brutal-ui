using System;

namespace BrutalUi.Core;

/// <summary>
/// One frame of the explicit stack of the focus walk. The walk pushes a frame when it enters an element and pops the
/// frame after the last child.
/// </summary>
internal struct FocusFrame
{
    /// <summary>The element of the frame.</summary>
    public Element Element;

    /// <summary>The opacity product of the element and its ancestors, after the clamp. It is above 0.</summary>
    public float Opacity;

    /// <summary>
    /// The index of the next child to visit. The walk counts up from 0. A value equal to the child count means that
    /// the walk visited each child, so the walk pops the frame.
    /// </summary>
    public int NextChild;
}

/// <summary>
/// The focus walk. It finds the next or the previous element that accepts the keyboard focus, for the Tab and the
/// Shift+Tab moves.
/// </summary>
/// <remarks>
/// <para>
/// The order is the paint order of <see cref="PaintWalk"/>: the roots in the order of add, the children in the order
/// of the child list, and a parent before its children. Tab moves forward in this order, and Shift+Tab moves back.
/// </para>
/// <para>
/// The hidden rule copies steps 1 and 2 of the enter step of <c>HitWalk</c>. An element with
/// <see cref="Display.None"/> is not a candidate, and neither is its subtree. The same holds when the opacity product
/// is 0 or less. A value below 0 counts as 0, a value above 1 counts as 1, and NaN counts as 1. The walk reads no clip,
/// no rect, and no <see cref="Element.IsPickable"/>, so a clipped element and an element that is not pickable can take
/// the focus. <see cref="IsShown(Element)"/> applies the same rule to one element, for <c>SetFocus</c> and the stale
/// rule of the drain.
/// </para>
/// <para>
/// A candidate is a shown element with <see cref="Element.AcceptsFocus"/>. The move wraps: after the last candidate,
/// Tab gives the first candidate, and before the first candidate, Shift+Tab gives the last candidate. When the current
/// element is the only candidate, the walk gives the current element, so the focus stays. When the walk does not see
/// the current element (it is <see langword="null"/>, hidden, or outside the roots), Tab gives the first candidate and
/// Shift+Tab gives the last candidate. With no candidate, the walk gives <see langword="null"/>.
/// </para>
/// <para>
/// The walk makes one pass and keeps four references: the first candidate, the last candidate, the last candidate
/// before the current element, and the first candidate after it. It keeps no list of candidates. It returns as soon as
/// the result is known, so it visits each element one time at most.
/// </para>
/// <para>
/// The walk uses the pattern of <c>HitWalk</c>: an explicit stack of <see cref="FocusFrame"/> values and no
/// recursion. No method of this class calls itself. The caller owns the stack. The walk grows it by doubling and
/// never shrinks it, so the walk allocates only during warm-up. The walk clears the used frames before it returns,
/// also after an early return and after an exception, so the stack keeps no element reference.
/// </para>
/// <para>
/// The walk reads the tree only. It changes no state and no dirty bit, and it calls no code of the element.
/// </para>
/// </remarks>
internal static class FocusWalk
{
    /// <summary>The start length of the frame stack. A tree deeper than this value grows the stack one time.</summary>
    internal const int InitialStackSize = 32;

    /// <summary>
    /// Finds the next or the previous element that accepts the keyboard focus, in the paint order.
    /// </summary>
    /// <param name="roots">The roots in the order of add.</param>
    /// <param name="current">The element that holds the focus now, or <see langword="null"/>.</param>
    /// <param name="backward"><see langword="true"/> for Shift+Tab, <see langword="false"/> for Tab.</param>
    /// <param name="stack">The frame stack. The walk replaces it with a larger array when it is full.</param>
    /// <returns>The element that gets the focus, or <see langword="null"/> when no element accepts the focus.</returns>
    internal static Element? Find(ReadOnlySpan<Element> roots, Element? current, bool backward, ref FocusFrame[] stack)
    {
        Candidates found = default;
        int depth = 0;
        try
        {
            for (int r = 0; r < roots.Length; r++)
            {
                Element root = roots[r];
                if (Enter(root, 1f, ref stack, ref depth) && Visit(root, current, backward, ref found))
                {
                    // The finally block clears the used frames.
                    return found.Result;
                }

                while (depth > 0)
                {
                    ref FocusFrame top = ref stack[depth - 1];
                    ReadOnlySpan<Element> children = top.Element.ChildStorage.AsSpan();

                    if (top.NextChild < children.Length)
                    {
                        // Gotcha: Enter can grow the stack, and then the ref is stale. Copy the values and move
                        // NextChild before the call. Do not use the ref after the call.
                        Element child = children[top.NextChild];
                        top.NextChild++;
                        float opacity = top.Opacity;
                        if (Enter(child, opacity, ref stack, ref depth) && Visit(child, current, backward, ref found))
                        {
                            // The finally block clears the used frames, also the frame of the child.
                            return found.Result;
                        }
                    }
                    else
                    {
                        // The walk visited each child. Pop the frame and keep no element reference in it.
                        top = default;
                        depth--;
                    }
                }
            }

            return backward ? found.Before ?? found.Last : found.After ?? found.First;
        }
        finally
        {
            // A full pass ends at depth 0. After an early return or an exception, clear the used frames.
            if (depth > 0)
            {
                Array.Clear(stack, 0, depth);
            }
        }
    }

    /// <summary>
    /// Finds out whether an element is shown: no element of its parent chain has <see cref="Display.None"/>, and the
    /// opacity product of the chain is above 0.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The method applies the hidden rule of the walk to one element. It uses the same clamp as the walk
    /// (<see cref="ClampOpacity(float)"/>), so the two rules cannot drift. The method reads the style of the last
    /// <see cref="UiContext.Update(float)"/>, as the hit test does.
    /// </para>
    /// <para>
    /// The method walks up the parent chain in a loop, from the element to the top. It does not call itself, and it
    /// allocates nothing. It stops at the first hidden element. It does not check the roots of a context.
    /// </para>
    /// </remarks>
    /// <param name="element">The element.</param>
    /// <returns><see langword="true"/> when the element and each ancestor are shown.</returns>
    internal static bool IsShown(Element element)
    {
        float opacity = 1f;
        for (Element? current = element; current is not null; current = current.Parent)
        {
            ref readonly ComputedStyle style = ref current.Style;
            if (style.Display == Display.None)
            {
                return false;
            }

            opacity *= ClampOpacity(style.Opacity);
            if (opacity <= 0f)
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Clamps the opacity of one element for the hidden rule, as in <c>HitWalk</c>. A value below 0 counts as 0, a
    /// value above 1 counts as 1, and NaN counts as 1.
    /// </summary>
    /// <param name="value">The opacity of the computed style.</param>
    /// <returns>The clamped opacity, from 0 to 1.</returns>
    private static float ClampOpacity(float value)
    {
        if (float.IsNaN(value) || value > 1f)
        {
            return 1f;
        }

        return value < 0f ? 0f : value;
    }

    /// <summary>
    /// Enters one element: it applies the hidden rule, then it pushes a frame with the opacity product and the first
    /// child index. A hidden subtree pushes no frame.
    /// </summary>
    /// <param name="element">The element.</param>
    /// <param name="opacity">The opacity product of the parent, or 1 for a root.</param>
    /// <param name="stack">The frame stack.</param>
    /// <param name="depth">The count of frames on the stack.</param>
    /// <returns><see langword="true"/> when the element is shown and the walk pushed its frame.</returns>
    private static bool Enter(Element element, float opacity, ref FocusFrame[] stack, ref int depth)
    {
        ref readonly ComputedStyle style = ref element.Style;

        // Step 1: Display.None hides the element and its subtree, as in HitWalk.
        if (style.Display == Display.None)
        {
            return false;
        }

        // Step 2: the opacity product, as in HitWalk. ClampOpacity holds the clamp that IsShown also uses.
        opacity *= ClampOpacity(style.Opacity);
        if (opacity <= 0f)
        {
            return false;
        }

        // Step 3: grow the stack when it is full, then push the frame.
        if (depth == stack.Length)
        {
            Array.Resize(ref stack, Math.Max(stack.Length * 2, InitialStackSize));
        }

        stack[depth] = new FocusFrame
        {
            Element = element,
            Opacity = opacity,
            NextChild = 0,
        };
        depth++;
        return true;
    }

    /// <summary>
    /// Visits one shown element, after its parent and before its children. It updates the four references and tells
    /// whether the result is known.
    /// </summary>
    /// <param name="element">The element.</param>
    /// <param name="current">The element that holds the focus now, or <see langword="null"/>.</param>
    /// <param name="backward"><see langword="true"/> for Shift+Tab, <see langword="false"/> for Tab.</param>
    /// <param name="found">The references of the walk. The method writes the result into it.</param>
    /// <returns><see langword="true"/> when the walk can stop and return <see cref="Candidates.Result"/>.</returns>
    private static bool Visit(Element element, Element? current, bool backward, ref Candidates found)
    {
        bool isCurrent = ReferenceEquals(element, current);
        if (isCurrent)
        {
            found.Seen = true;
            if (backward && found.Before is not null)
            {
                found.Result = found.Before;
                return true;
            }
        }

        if (!element.AcceptsFocus)
        {
            return false;
        }

        // The current element counts here, so a single candidate keeps the focus.
        found.First ??= element;
        found.Last = element;

        if (isCurrent)
        {
            return false;
        }

        if (!found.Seen)
        {
            found.Before = element;
        }
        else if (found.After is null)
        {
            found.After = element;
            if (!backward)
            {
                found.Result = element;
                return true;
            }
        }

        return false;
    }

    /// <summary>The references that the walk keeps during one pass. The struct lives on the stack of the caller.</summary>
    private struct Candidates
    {
        /// <summary>The first candidate in the paint order.</summary>
        public Element? First;

        /// <summary>The last candidate that the walk saw.</summary>
        public Element? Last;

        /// <summary>The last candidate before the current element.</summary>
        public Element? Before;

        /// <summary>The first candidate after the current element.</summary>
        public Element? After;

        /// <summary>The result of an early return.</summary>
        public Element? Result;

        /// <summary><see langword="true"/> after the walk visits the current element.</summary>
        public bool Seen;
    }
}
