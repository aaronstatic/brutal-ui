using System;
using System.Runtime.CompilerServices;

namespace BrutalUi.Core;

/// <summary>
/// The style pass. It computes the style of each element with <see cref="DirtyFlags.Style"/> from the cascade, and
/// marks the element for <see cref="DirtyFlags.Layout"/> or <see cref="DirtyFlags.Paint"/> when the style changes.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="UiContext.Update(float)"/> runs the pass before the layout pass. The pass uses its own recursion and
/// not <see cref="DirtyWalk"/> (Decision 3), because the pass marks the children of an element during the visit (A3).
/// </para>
/// <para>
/// The pass enters an element only through <see cref="DirtyFlags.Style"/> or <see cref="DirtyFlags.DescendantStyle"/>.
/// A clean subtree costs one flag check. The pass clears the two style bits on each element that it enters, after
/// the subtree of the element. It marks only <see cref="DirtyFlags.Layout"/> and <see cref="DirtyFlags.Paint"/>,
/// which it does not clear, so the dirty-bit invariant holds at each step.
/// </para>
/// <para>
/// The pass rebuilds the style of a dirty element from <c>default(ComputedStyle)</c>. It applies each matching rule of
/// the cascade in the order of the cascade, so a later rule wins. A rule skips each member of the inline set. Then the
/// pass applies the inline set, so an inline value wins over each rule. The pass allocates nothing.
/// </para>
/// <para>
/// The inheritance: a text member (<see cref="StyleMasks.Text"/>) that no matching rule and no inline value set gets
/// the value of the parent. A root gets the text defaults of its context. When a text member of an element changes,
/// the pass marks each child with <see cref="DirtyFlags.Style"/> (A3), so the new value flows down in the same pass.
/// A child that sets its own value keeps it, and its children get no mark.
/// </para>
/// <para>
/// A text change gives <see cref="DirtyFlags.Paint"/>. A change of the font or of the font size on a
/// <see cref="Label"/> also gives <see cref="DirtyFlags.Layout"/>, because the label measures its text with them.
/// </para>
/// <para>
/// A rule matches through <see cref="Selector"/>. The subject compound tests the pseudo-states, the type id, one
/// class, and the name of the element. A child chain then tests the parent, and a descendant chain tests the parent
/// and then the grandparent. The pass reads <see cref="Element.TypeId"/> one time for each restyled element and passes
/// it to each rule. The match reads the tree, not the computed style, so the order of the visit does not change it.
/// </para>
/// </remarks>
internal static class StylePass
{
    /// <summary>The self bit and the descendant bit of the style kind.</summary>
    private const DirtyFlags StyleBits = DirtyFlags.Style | DirtyFlags.DescendantStyle;

    /// <summary>Runs the style pass on each root and its subtree, in the order of the roots.</summary>
    /// <param name="roots">The roots of the context.</param>
    /// <param name="cascade">The rules of each sheet, sorted by the specificity, then by the sheet, then by the rule.</param>
    /// <param name="defaults">The text defaults of the context. Each root inherits its unset text members from them.</param>
    internal static void Run(ReadOnlySpan<Element> roots, ReadOnlySpan<StyleRule> cascade, in ComputedStyle defaults)
    {
        for (int i = 0; i < roots.Length; i++)
        {
            Element root = roots[i];
            if ((root.Dirty & StyleBits) != DirtyFlags.None)
            {
                Visit(root, cascade, in defaults);
            }
        }
    }

    /// <summary>
    /// Restyles one element when it has <see cref="DirtyFlags.Style"/>, then enters each child with a style bit.
    /// </summary>
    /// <param name="element">The element to visit. It has a style bit.</param>
    /// <param name="cascade">The sorted rules.</param>
    /// <param name="parentText">
    /// The style of the parent, or the text defaults of the context for a root. The pass reads its text members only.
    /// </param>
    private static void Visit(Element element, ReadOnlySpan<StyleRule> cascade, in ComputedStyle parentText)
    {
        // Step 1: restyle the element when it has the self bit, also with an empty cascade. The pass owns the style,
        // so an element with no rule and no inline value gets the defaults and the parent text.
        DirtyFlags flags = element.Dirty;
        if ((flags & DirtyFlags.Style) != DirtyFlags.None)
        {
            Restyle(element, cascade, in parentText);
        }

        // Step 2: read the bits again. A text change in Restyle marks each child with Style, and the mark sets
        // DescendantStyle here (A3). A stale value of flags skips the children, and they keep the old text.
        flags = element.Dirty;
        if ((flags & DirtyFlags.DescendantStyle) != DirtyFlags.None)
        {
            // The children inherit from the current style of this element. The style is current also when the visit
            // did not restyle the element.
            ref readonly ComputedStyle style = ref element.StyleStorage;
            ReadOnlySpan<Element> children = element.ChildStorage.AsSpan();
            for (int i = 0; i < children.Length; i++)
            {
                Element child = children[i];
                if ((child.Dirty & StyleBits) != DirtyFlags.None)
                {
                    Visit(child, cascade, in style);
                }
            }
        }

        // Step 3: clear the style bits after the subtree, as DirtyWalk does.
        element.ClearDirty(StyleBits);
    }

    /// <summary>
    /// Computes the style of one element from the cascade, the inline set, and the parent text, writes it, and marks
    /// the element for the changed kinds. A text change also marks each child with <see cref="DirtyFlags.Style"/>.
    /// </summary>
    /// <remarks>
    /// The method is not inlined, so the large <see cref="ComputedStyle"/> local stays out of the frame of the
    /// recursion in <see cref="Visit"/>.
    /// </remarks>
    /// <param name="element">The element to restyle.</param>
    /// <param name="cascade">The sorted rules.</param>
    /// <param name="parentText">The style of the parent, or the text defaults of the context for a root.</param>
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void Restyle(Element element, ReadOnlySpan<StyleRule> cascade, in ComputedStyle parentText)
    {
        ComputedStyle next = default;
        ref readonly StyleProperties inline = ref element.InlineStorage;
        ulong inlineMask = inline.SetMask;
        ulong applied = inlineMask;

        // A7: a scan of each rule of each sheet. A later rule writes over an earlier rule. A rule skips each member
        // of the inline set, because the inline value writes over it below. The type id is one virtual call, so the
        // pass reads it one time and not for each rule.
        ElementTypeId elementType = element.TypeId;
        for (int i = 0; i < cascade.Length; i++)
        {
            ref readonly StyleRule rule = ref cascade[i];
            if (rule.Selector.Matches(element, elementType))
            {
                rule.Properties.ApplyTo(ref next, inlineMask);
                applied |= rule.Properties.SetMask;
            }
        }

        // The inline set comes last, so an inline value wins over each rule.
        inline.ApplyTo(ref next, 0UL);

        // The inheritance: a text member that no rule and no inline value set takes the value of the parent.
        if ((applied & StyleMasks.Text) != StyleMasks.Text)
        {
            Inherit(ref next, in parentText, applied);
        }

        ref ComputedStyle current = ref element.StyleStorage;
        DirtyFlags bits = Compare(in current, in next, out bool textChanged, out bool sizeTextChanged);
        current = next;

        // A label measures its text with the font and the font size, so a change of either one measures it again.
        // The color does not change the size.
        if (sizeTextChanged && element is Label)
        {
            bits |= DirtyFlags.Layout;
        }

        if (bits != DirtyFlags.None)
        {
            element.MarkDirty(bits);
        }

        // A3: the children inherit the text, so a text change restyles each child. A child mark of Style sends
        // DescendantStyle up to this element, and Visit reads the bits again after this call.
        if (textChanged)
        {
            ReadOnlySpan<Element> children = element.ChildStorage.AsSpan();
            for (int i = 0; i < children.Length; i++)
            {
                children[i].MarkDirty(DirtyFlags.Style);
            }
        }
    }

    /// <summary>Copies each text member that <paramref name="applied"/> does not hold from the parent text.</summary>
    /// <param name="next">The new style.</param>
    /// <param name="parentText">The style of the parent, or the text defaults of the context.</param>
    /// <param name="applied">The bits of the members that a matching rule or the inline set wrote.</param>
    private static void Inherit(ref ComputedStyle next, in ComputedStyle parentText, ulong applied)
    {
        if ((applied & (1UL << (int)StyleProperty.Color)) == 0)
        {
            next.Color = parentText.Color;
        }

        if ((applied & (1UL << (int)StyleProperty.Font)) == 0)
        {
            next.Font = parentText.Font;
        }

        if ((applied & (1UL << (int)StyleProperty.FontSize)) == 0)
        {
            next.FontSize = parentText.FontSize;
        }
    }

    /// <summary>Compares two styles and gives the dirty bits of the change.</summary>
    /// <remarks>The overload discards the text result. See <see cref="Compare(in ComputedStyle, in ComputedStyle, out bool)"/>.</remarks>
    /// <param name="current">The style that the element holds.</param>
    /// <param name="next">The new style.</param>
    /// <returns>The bits <see cref="DirtyFlags.Layout"/> and <see cref="DirtyFlags.Paint"/> of the change, or <see cref="DirtyFlags.None"/>.</returns>
    internal static DirtyFlags Compare(in ComputedStyle current, in ComputedStyle next)
    {
        return Compare(in current, in next, out _, out _);
    }

    /// <summary>Compares two styles, gives the dirty bits of the change, and tells whether a text member changed.</summary>
    /// <remarks>The overload discards the size result. See <see cref="Compare(in ComputedStyle, in ComputedStyle, out bool, out bool)"/>.</remarks>
    /// <param name="current">The style that the element holds.</param>
    /// <param name="next">The new style.</param>
    /// <param name="textChanged">
    /// <see langword="true"/> when <see cref="ComputedStyle.Color"/>, <see cref="ComputedStyle.Font"/>, or
    /// <see cref="ComputedStyle.FontSize"/> changed.
    /// </param>
    /// <returns>The bits <see cref="DirtyFlags.Layout"/> and <see cref="DirtyFlags.Paint"/> of the change, or <see cref="DirtyFlags.None"/>.</returns>
    internal static DirtyFlags Compare(in ComputedStyle current, in ComputedStyle next, out bool textChanged)
    {
        return Compare(in current, in next, out textChanged, out _);
    }

    /// <summary>
    /// Compares two styles, gives the dirty bits of the change, and tells whether a text member and a size text member
    /// changed.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A changed layout member gives <see cref="DirtyFlags.Layout"/>. A changed paint member gives
    /// <see cref="DirtyFlags.Paint"/>. A changed text member gives <see cref="DirtyFlags.Paint"/> and sets
    /// <paramref name="textChanged"/>. <see cref="ComputedStyle.Overflow"/> is a layout member (A9), so it gives
    /// <see cref="DirtyFlags.Layout"/> only.
    /// </para>
    /// <para>
    /// A changed <see cref="ComputedStyle.Font"/> or <see cref="ComputedStyle.FontSize"/> also sets
    /// <paramref name="sizeTextChanged"/>. The method does not give <see cref="DirtyFlags.Layout"/> for it, because
    /// only a <see cref="Label"/> measures its text. The caller adds <see cref="DirtyFlags.Layout"/> for a label.
    /// </para>
    /// <para>
    /// A float compares with <see cref="float.Equals(float)"/>, so <c>NaN</c> to <c>NaN</c> is no change.
    /// <see cref="ComputedStyle.Opacity"/> compares the getter values, so an unset value and a set 1 are the same.
    /// <see cref="Rgba8"/> and <see cref="FontHandle"/> use their own equality.
    /// </para>
    /// </remarks>
    /// <param name="current">The style that the element holds.</param>
    /// <param name="next">The new style.</param>
    /// <param name="textChanged">
    /// <see langword="true"/> when <see cref="ComputedStyle.Color"/>, <see cref="ComputedStyle.Font"/>, or
    /// <see cref="ComputedStyle.FontSize"/> changed.
    /// </param>
    /// <param name="sizeTextChanged">
    /// <see langword="true"/> when <see cref="ComputedStyle.Font"/> or <see cref="ComputedStyle.FontSize"/> changed.
    /// </param>
    /// <returns>The bits <see cref="DirtyFlags.Layout"/> and <see cref="DirtyFlags.Paint"/> of the change, or <see cref="DirtyFlags.None"/>.</returns>
    internal static DirtyFlags Compare(
        in ComputedStyle current,
        in ComputedStyle next,
        out bool textChanged,
        out bool sizeTextChanged)
    {
        DirtyFlags bits = DirtyFlags.None;

        if (LayoutChanged(in current, in next))
        {
            bits |= DirtyFlags.Layout;
        }

        sizeTextChanged = SizeTextChanged(in current, in next);
        textChanged = sizeTextChanged || !current.Color.Equals(next.Color);
        if (textChanged || PaintChanged(in current, in next))
        {
            bits |= DirtyFlags.Paint;
        }

        return bits;
    }

    /// <summary>Checks the 2 text members that change the size of a text: the font and the font size.</summary>
    /// <param name="a">The style that the element holds.</param>
    /// <param name="b">The new style.</param>
    /// <returns><see langword="true"/> when the font or the font size changed.</returns>
    private static bool SizeTextChanged(in ComputedStyle a, in ComputedStyle b)
    {
        return !a.Font.Equals(b.Font)
            || !a.FontSize.Equals(b.FontSize);
    }

    /// <summary>Checks the 25 layout members.</summary>
    /// <param name="a">The style that the element holds.</param>
    /// <param name="b">The new style.</param>
    /// <returns><see langword="true"/> when one layout member or more changed.</returns>
    private static bool LayoutChanged(in ComputedStyle a, in ComputedStyle b)
    {
        return a.FlexDirection != b.FlexDirection
            || !a.Width.Equals(b.Width)
            || !a.Height.Equals(b.Height)
            || !EdgesEqual(in a.Margin, in b.Margin)
            || !EdgesEqual(in a.Padding, in b.Padding)
            || !EdgesEqual(in a.BorderWidth, in b.BorderWidth)
            || !a.FlexGrow.Equals(b.FlexGrow)
            || !a.FlexShrink.Equals(b.FlexShrink)
            || !a.FlexBasis.Equals(b.FlexBasis)
            || !a.MinWidth.Equals(b.MinWidth)
            || !a.MinHeight.Equals(b.MinHeight)
            || !a.MaxWidth.Equals(b.MaxWidth)
            || !a.MaxHeight.Equals(b.MaxHeight)
            || a.JustifyContent != b.JustifyContent
            || a.AlignItems != b.AlignItems
            || a.AlignSelf != b.AlignSelf
            || a.AlignContent != b.AlignContent
            || a.FlexWrap != b.FlexWrap
            || a.Display != b.Display
            || a.Position != b.Position
            || !a.Left.Equals(b.Left)
            || !a.Top.Equals(b.Top)
            || !a.Right.Equals(b.Right)
            || !a.Bottom.Equals(b.Bottom)
            || a.Overflow != b.Overflow;
    }

    /// <summary>Checks the 4 paint members.</summary>
    /// <param name="a">The style that the element holds.</param>
    /// <param name="b">The new style.</param>
    /// <returns><see langword="true"/> when one paint member or more changed.</returns>
    private static bool PaintChanged(in ComputedStyle a, in ComputedStyle b)
    {
        return !a.BackgroundColor.Equals(b.BackgroundColor)
            || !a.BorderColor.Equals(b.BorderColor)
            || !CornersEqual(in a.CornerRadius, in b.CornerRadius)
            || !a.Opacity.Equals(b.Opacity);
    }

    /// <summary>
    /// Compares each edge of two edge sets. <see cref="LengthEdges"/> has no equality. <see cref="InlineStyle"/> also
    /// uses this method.
    /// </summary>
    /// <param name="a">The first set.</param>
    /// <param name="b">The second set.</param>
    /// <returns><see langword="true"/> when each edge is equal.</returns>
    internal static bool EdgesEqual(in LengthEdges a, in LengthEdges b)
    {
        return a.Left.Equals(b.Left)
            && a.Top.Equals(b.Top)
            && a.Right.Equals(b.Right)
            && a.Bottom.Equals(b.Bottom);
    }

    /// <summary>
    /// Compares each corner of two corner sets. <see cref="Corners"/> has no equality. <see cref="InlineStyle"/> also
    /// uses this method.
    /// </summary>
    /// <param name="a">The first set.</param>
    /// <param name="b">The second set.</param>
    /// <returns><see langword="true"/> when each corner is equal. <c>NaN</c> to <c>NaN</c> is equal.</returns>
    internal static bool CornersEqual(in Corners a, in Corners b)
    {
        return a.TopLeft.Equals(b.TopLeft)
            && a.TopRight.Equals(b.TopRight)
            && a.BottomRight.Equals(b.BottomRight)
            && a.BottomLeft.Equals(b.BottomLeft);
    }
}
