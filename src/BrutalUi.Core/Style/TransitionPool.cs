using System;
using System.Collections.Generic;
using Brutal.Numerics;

namespace BrutalUi.Core;

/// <summary>
/// The running tweens of a context: one <see cref="TransitionRecord"/> for each element with a running tween.
/// </summary>
/// <remarks>
/// <para>
/// The style pass calls <see cref="Track"/> for a restyled element before it writes the new style. <see cref="Track"/>
/// starts, keeps, turns, or ends the tween of each member that can transition. Then <see cref="Advance"/> runs after
/// the style pass in the same update. It writes the value of each running tween over the target that the style pass
/// wrote, and it marks <see cref="DirtyFlags.Paint"/> only. A color tween also writes its value into each descendant
/// that follows the color (<see cref="Element.FollowsColor"/>).
/// </para>
/// <para>
/// The lifetime rule (Decision 9): <see cref="Advance"/> drops the record of an element outside the tree of its
/// context with no write and no mark. <see cref="Track"/> drops the record of an element that joined a tree
/// (<see cref="Element.IsStyled"/> is <see langword="false"/>), so a join starts no tween.
/// </para>
/// <para>
/// The records live in a dense array. The records 0 to <see cref="ActiveCount"/> - 1 run. A release moves the last
/// record into the freed index and clears the last index, so the pool holds no reference to an element after its
/// release. A dictionary with <see cref="ReferenceEqualityComparer"/> gives the index of the record of an element.
/// </para>
/// <para>
/// The growth rule: the array starts empty. The first rent grows it to 4 records, and each later growth doubles it.
/// The array and the dictionary never shrink. Thus the pool allocates only while it grows to the peak count of the
/// game. The zero-allocation rule: after this warm-up, <see cref="Track"/> and <see cref="Advance"/> allocate no
/// managed memory.
/// </para>
/// <para>
/// A value of a member goes into four floats: a color as R, G, B, and A (0 to 255), the corners as top-left,
/// top-right, bottom-right, and bottom-left, and the opacity in the first float. <see cref="Read"/>,
/// <see cref="Write"/>, and <see cref="Interpolate"/> hold the map.
/// </para>
/// </remarks>
internal sealed class TransitionPool
{
    /// <summary>The size of the array at the first rent.</summary>
    private const int FirstCapacity = 4;

    /// <summary>The mask with a bit for each slot.</summary>
    private const int AllSlots = (1 << StyleTransitions.SlotCount) - 1;

    /// <summary>The slot of <see cref="ComputedStyle.Color"/>, from <see cref="StyleTransitions.SlotOf(StyleProperty)"/>.</summary>
    private static readonly int s_colorSlot = StyleTransitions.SlotOf(StyleProperty.Color);

    private readonly Dictionary<Element, int> _index = new(ReferenceEqualityComparer.Instance);
    private TransitionRecord[] _records = [];
    private int _count;

    /// <summary>Gets the number of records that run: one for each element with a running tween.</summary>
    internal int ActiveCount => _count;

    /// <summary>Gets the length of the record array. The value grows during warm-up and never shrinks.</summary>
    internal int Capacity => _records.Length;

    /// <summary>
    /// Starts, keeps, turns, or ends the tween of each member of an element that the style pass restyles.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The style pass calls this method before it writes <paramref name="next"/> into the element. The method reads
    /// <paramref name="current"/> and <paramref name="next"/> and changes neither. For each slot, the first branch
    /// that applies wins:
    /// </para>
    /// <list type="number">
    /// <item><description>
    /// Keep: the slot runs, the target equals the target of the tween, the merged set holds the slot with a valid
    /// duration (<see cref="CanRun"/>), and the inline set does not hold the member. The tween keeps its start value,
    /// its elapsed time, and its duration. A restyle with the same target (for example, a state change on an
    /// ancestor) thus does not restart the fade. The set of the new style decides (Decision 3): a new style with no
    /// valid slot for the member skips this branch, and the slot ends in the "no change" branch or in the "else"
    /// branch, as in step 4 of the CSS start algorithm.
    /// </description></item>
    /// <item><description>No change: the target equals the visible value. A running slot ends.</description></item>
    /// <item><description>
    /// Start or turn: the merged set holds the slot with a valid duration (<see cref="CanRun"/>), the visible value
    /// and the target are finite, and the inline set does not hold the member. The tween starts from the visible
    /// value with the full duration, and it takes the duration and the easing of the merged set of the new style (the
    /// hover in uses the hover rule, and the hover out uses the base rule). A running tween with another target turns
    /// in this branch: it starts again from the visible value. The first start of an element rents a record.
    /// </description></item>
    /// <item><description>
    /// Else: a running slot ends. The style pass writes the target at once. This branch holds three cases: the merged
    /// set holds no slot with a valid duration, a value is not finite, and the inline set holds the member.
    /// </description></item>
    /// </list>
    /// <para>
    /// The inline rule: a member of the inline set never fades, because the set of <paramref name="inlineMask"/>
    /// skips the keep branch and the start branch. An inline write changes the member at once, and it ends a running
    /// tween of the member. A member that leaves the inline set follows the start rule: the visible value is the old
    /// inline value, the target is the value of the rules, and the member fades when the merged set holds the slot.
    /// </para>
    /// <para>
    /// The method releases the record when no slot runs. The first style of an element in its tree
    /// (<see cref="Element.IsStyled"/> is <see langword="false"/>) starts no tween, and it releases the record of the
    /// element. The join rule (Decision 9): an attach and <see cref="UiContext.AddRoot(Element)"/> set
    /// <see cref="Element.IsStyled"/> to <see langword="false"/> on the full subtree. Thus an element that a
    /// <c>Remove</c> and an <c>Add</c> (or an <c>Insert</c>) move inside its tree, or that leaves and joins again
    /// before the next update, ends each tween and gets its target at once. <see cref="ElementList.Move"/> does not
    /// attach, so it keeps each tween.
    /// </para>
    /// <para>The method allocates only when it rents a record and the array or the dictionary grows.</para>
    /// </remarks>
    /// <param name="element">The element that the style pass restyles.</param>
    /// <param name="current">The style that the element holds: the visible value of each member.</param>
    /// <param name="next">The new style from the cascade: the target of each member.</param>
    /// <param name="merged">The transitions of the matched rules, merged for each slot in the order of the cascade.</param>
    /// <param name="inlineMask">
    /// The set mask of the inline set of the element. Bit <c>n</c> is <see cref="StyleProperty"/> <c>n</c>. An inline
    /// member starts no tween and ends its running slot.
    /// </param>
    internal void Track(
        Element element,
        in ComputedStyle current,
        in ComputedStyle next,
        in StyleTransitions merged,
        ulong inlineMask)
    {
        // Step 1: find the record of the element. A pool with no record skips the lookup.
        int index = -1;
        if (_count > 0 && _index.TryGetValue(element, out int found))
        {
            index = found;
        }

        // Step 2: the first style of an element in its tree starts no tween. A join (an attach or AddRoot) sets
        // IsStyled to false on the full subtree, so a moved element drops its record here. The style pass then writes
        // the target at once.
        if (!element.IsStyled)
        {
            if (index >= 0)
            {
                Release(index);
            }

            return;
        }

        // Step 3: the rule of each slot.
        for (int slot = 0; slot < StyleTransitions.SlotCount; slot++)
        {
            int bit = 1 << slot;
            StyleProperty property = StyleTransitions.PropertyOf(slot);
            bool isInline = (inlineMask & (1UL << (int)property)) != 0;
            bool running = index >= 0 && (_records[index].Mask & bit) != 0;
            float4 visible = Read(in current, property);
            float4 target = Read(in next, property);

            // Keep: the same target as the running tween, and the new style holds the slot with a valid duration.
            if (running && !isInline && CanRun(in merged, slot) && Equal(in target, in _records[index].Slots[slot].To))
            {
                continue;
            }

            // No change: the visible value is the target.
            if (Equal(in target, in visible))
            {
                if (running)
                {
                    _records[index].Mask &= (byte)~bit;
                }

                continue;
            }

            // Start or turn: a tween from the visible value to the new target.
            // CanRun rejects a duration of 0, a negative value, NaN, and infinity, so the progress never divides by 0.
            if (!isInline && CanRun(in merged, slot) && IsFinite(in visible) && IsFinite(in target))
            {
                StyleTransition transition = merged.GetSlot(slot);
                if (index < 0)
                {
                    index = Rent(element);
                }

                ref TransitionRecord record = ref _records[index];
                ref TweenSlot tween = ref record.Slots[slot];
                tween.From = visible;
                tween.To = target;
                tween.Elapsed = 0f;
                tween.Duration = transition.Duration;
                tween.Easing = transition.Easing;
                record.Mask |= (byte)bit;
                continue;
            }

            // Else: end the slot. The style pass writes the target at once.
            if (running)
            {
                _records[index].Mask &= (byte)~bit;
            }
        }

        // Step 4: release the record when no slot runs.
        if (index >= 0 && (_records[index].Mask & AllSlots) == 0)
        {
            Release(index);
        }
    }

    /// <summary>Checks whether a merged transition set holds a slot with a duration that can run a tween.</summary>
    /// <remarks>
    /// <para>
    /// A valid duration is finite and more than 0. Decision R3: a slot with another duration (0, a negative value,
    /// <c>NaN</c>, or an infinity) acts as no transition. It starts no tween, it keeps no tween, and it does not stop
    /// the follow of <see cref="ComputedStyle.Color"/>.
    /// </para>
    /// <para>
    /// The start rule and the keep rule of <see cref="Track"/> and the write of <see cref="Element.FollowsColor"/> in
    /// the style pass use this one check, so the three rules cannot drift. The method allocates nothing.
    /// </para>
    /// </remarks>
    /// <param name="merged">The merged transition set of the matched rules.</param>
    /// <param name="slot">The slot index, from <see cref="StyleTransitions.SlotOf(StyleProperty)"/>.</param>
    /// <returns><see langword="true"/> when the set holds the slot and its duration is finite and more than 0.</returns>
    internal static bool CanRun(in StyleTransitions merged, int slot)
    {
        if (!merged.IsSlotSet(slot))
        {
            return false;
        }

        float duration = merged.GetSlot(slot).Duration;
        return float.IsFinite(duration) && duration > 0f;
    }

    /// <summary>
    /// Advances each running tween, writes its value into the style of its element, and marks
    /// <see cref="DirtyFlags.Paint"/> on each element with a changed member.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The pass visits the records from the last to the first, so a release (a move of the last record into the freed
    /// index) skips no record. For each running slot, the pass adds <paramref name="deltaTime"/> to the elapsed time.
    /// A progress of 1 or more writes the exact target and ends the slot. Else the value is
    /// <c>From + (To - From) * curve(progress)</c>. A color takes the step in premultiplied form.
    /// </para>
    /// <para>
    /// The clock rule (Decision 6): a <paramref name="deltaTime"/> that is not more than 0 advances nothing, also
    /// <c>NaN</c>. A tween thus never moves back, and a color never gets <c>NaN</c>. The pass still writes the visible
    /// value of each running slot, so a <paramref name="deltaTime"/> of 0 puts the value of the tween over the target
    /// of the style pass. An infinite <paramref name="deltaTime"/> ends each tween at its exact target. The start rule of
    /// <see cref="Track"/> starts a tween only for a duration that is finite and more than 0, so the progress never
    /// divides by 0.
    /// </para>
    /// <para>
    /// The pass writes a value only when it differs from the member. It marks <see cref="DirtyFlags.Paint"/> only,
    /// never <see cref="DirtyFlags.Style"/> or <see cref="DirtyFlags.Layout"/>. The pass allocates nothing.
    /// </para>
    /// <para>
    /// The follow rule (Decision 8): the children of an element inherit the target of its color in the style pass.
    /// Thus after a step of a <see cref="ComputedStyle.Color"/> tween, also the last step, the pass writes the visible
    /// color into each descendant that follows it (<see cref="FollowColor"/>). The pass runs the walk also when the
    /// bytes of the color did not change, because the style pass can write the target into a child in the same update.
    /// </para>
    /// <para>
    /// The pass runs in two loops, so the order of the records does not change the result. The first loop advances
    /// each running slot and writes its value. The second loop runs the follow walks and releases each record with no
    /// running slot. Thus each walk sees the final state of each slot. A descendant with its own color transition
    /// never takes the color of its ancestor (Decision R2), so the end of its tween in this step keeps its target. A
    /// descendant with only a fill tween follows, and the order of the records does not change its color. Between
    /// the two loops, the pass keeps the follow mark of a record in <see cref="TransitionRecord.Follow"/>. The first
    /// loop sets the mark before the color step, because the last step clears the slot bit. The second loop clears
    /// the mark, so no record holds a mark after the call.
    /// </para>
    /// <para>
    /// The leave rule (Decision 9): before the two loops, the pass checks the tree of each record. It walks up the
    /// parents of the element to the top element, and then it compares the top element with each root in
    /// <paramref name="roots"/> by reference. When no root matches, the element left the tree of this context, and the
    /// pass releases its record with no write, no mark, and no follow walk. An element has no context reference, so
    /// the check goes from the element up. A write at the drop could write over the style that a second context gave
    /// the element after a move. The check costs one walk to the top for each record, and it allocates nothing.
    /// </para>
    /// </remarks>
    /// <param name="deltaTime">The time since the last update, in seconds.</param>
    /// <param name="roots">The roots of the context. The tree check compares the top element of each record with them.</param>
    internal void Advance(float deltaTime, ReadOnlySpan<Element> roots)
    {
        // Loop 0: the tree check. A record of an element outside the tree of this context drops with no write and no
        // mark. The loop runs before loop 1, so no record holds the follow mark yet.
        // A release moves the last record into the freed index, and the loop already visited the last record.
        for (int i = _count - 1; i >= 0; i--)
        {
            if (!IsInTree(_records[i].Element, roots))
            {
                Release(i);
            }
        }

        // The compare is false for NaN: a delta time that is not more than 0 advances nothing.
        float step = deltaTime > 0f ? deltaTime : 0f;
        int colorBit = 1 << s_colorSlot;

        // Loop 1: advance each running slot and write its value. A record that runs a color step gets the follow mark.
        for (int i = _count - 1; i >= 0; i--)
        {
            ref TransitionRecord record = ref _records[i];
            Element element = record.Element;
            ref ComputedStyle style = ref element.StyleStorage;
            bool changed = false;

            // The mark comes before the step, because the last step of the color clears its slot bit.
            record.Follow = (record.Mask & colorBit) != 0;

            for (int slot = 0; slot < StyleTransitions.SlotCount; slot++)
            {
                int bit = 1 << slot;
                if ((record.Mask & bit) == 0)
                {
                    continue;
                }

                ref TweenSlot tween = ref record.Slots[slot];
                StyleProperty property = StyleTransitions.PropertyOf(slot);
                tween.Elapsed += step;
                float progress = tween.Elapsed / tween.Duration;

                float4 value;
                if (progress >= 1f)
                {
                    value = tween.To;
                    record.Mask &= (byte)~bit;
                }
                else
                {
                    value = Interpolate(property, in tween.From, in tween.To, TransitionCurves.Evaluate(tween.Easing, progress));
                }

                changed |= Write(ref style, property, in value);
            }

            if (changed)
            {
                element.MarkDirty(DirtyFlags.Paint);
            }
        }

        // Loop 2: the follow walks, then the release of each record with no running slot. A release moves the last
        // record into the freed index, and the loop already visited the last record.
        for (int i = _count - 1; i >= 0; i--)
        {
            ref TransitionRecord record = ref _records[i];
            if (record.Follow)
            {
                record.Follow = false;
                Element element = record.Element;
                FollowColor(element, element.StyleStorage.Color);
            }

            if ((record.Mask & AllSlots) == 0)
            {
                Release(i);
            }
        }
    }

    /// <summary>
    /// Writes a color into each child that follows it, and then into the children of that child, down the tree.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The walk skips a child with <see cref="Element.FollowsColor"/> <see langword="false"/> and its subtree, because
    /// the descendants of that child inherit from it. A child with its own <see cref="ComputedStyle.Color"/> transition
    /// holds <see langword="false"/> (Decision R2), so the walk does not write over the end of its own tween. The walk
    /// also skips a child with a running <see cref="ComputedStyle.Color"/> slot and its subtree, because the child runs
    /// its own tween and its own walk. This skip is a guard: the keep rule of <see cref="Track"/> ends a slot that the
    /// merged set does not hold with a valid duration (<see cref="CanRun"/>), so a running color slot comes with
    /// <see cref="Element.FollowsColor"/> <see langword="false"/>. A child with a record and no running color slot
    /// (for example, a fill tween) follows.
    /// </para>
    /// <para>
    /// The walk writes the color only when it differs from the color of the child, and then it marks
    /// <see cref="DirtyFlags.Paint"/> only. A color does not change the size of a <see cref="Label"/>, so the walk
    /// gives no <see cref="DirtyFlags.Layout"/>. The walk visits each child also when its color did not change,
    /// because the style pass can write the target into a grandchild.
    /// </para>
    /// <para>
    /// The recursion depth is the depth of the tree, as in the style pass. The walk allocates nothing.
    /// </para>
    /// </remarks>
    /// <param name="parent">The element whose children get the color.</param>
    /// <param name="color">The visible color of the tween.</param>
    private void FollowColor(Element parent, Rgba8 color)
    {
        ReadOnlySpan<Element> children = parent.ChildStorage.AsSpan();
        for (int i = 0; i < children.Length; i++)
        {
            Element child = children[i];
            if (!child.FollowsColor || HasRunningColor(child))
            {
                continue;
            }

            ref ComputedStyle style = ref child.StyleStorage;
            if (!style.Color.Equals(color))
            {
                style.Color = color;
                child.MarkDirty(DirtyFlags.Paint);
            }

            FollowColor(child, color);
        }
    }

    /// <summary>Checks whether an element is in the tree of a root of this context.</summary>
    /// <remarks>
    /// The method walks up the parents to the top element, and then it compares the top element with each root by
    /// reference. The cost is the depth of the element plus the count of the roots. The method allocates nothing.
    /// </remarks>
    /// <param name="element">The element of a record.</param>
    /// <param name="roots">The roots of the context.</param>
    /// <returns><see langword="true"/> when the top element of <paramref name="element"/> is a root in <paramref name="roots"/>.</returns>
    private static bool IsInTree(Element element, ReadOnlySpan<Element> roots)
    {
        Element top = element;
        for (Element? parent = top.Parent; parent is not null; parent = parent.Parent)
        {
            top = parent;
        }

        for (int i = 0; i < roots.Length; i++)
        {
            if (ReferenceEquals(roots[i], top))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>Checks whether an element has a running <see cref="ComputedStyle.Color"/> slot.</summary>
    /// <param name="element">The element.</param>
    /// <returns><see langword="true"/> when the element has a record with a running color slot.</returns>
    private bool HasRunningColor(Element element)
    {
        return _index.TryGetValue(element, out int index) && (_records[index].Mask & (1 << s_colorSlot)) != 0;
    }

    /// <summary>Reads the value of a member as four floats.</summary>
    /// <param name="style">The style to read.</param>
    /// <param name="property">A member that can transition.</param>
    /// <returns>The value as four floats.</returns>
    private static float4 Read(in ComputedStyle style, StyleProperty property)
    {
        switch (property)
        {
            case StyleProperty.BackgroundColor: return FromColor(style.BackgroundColor);
            case StyleProperty.BorderColor: return FromColor(style.BorderColor);
            case StyleProperty.Color: return FromColor(style.Color);
            case StyleProperty.CornerRadius:
                Corners corners = style.CornerRadius;
                return new float4(corners.TopLeft, corners.TopRight, corners.BottomRight, corners.BottomLeft);
            case StyleProperty.Opacity: return new float4(style.Opacity, 0f, 0f, 0f);
            default: throw new ArgumentOutOfRangeException(nameof(property), property, "The member cannot transition.");
        }
    }

    /// <summary>Writes a value of four floats into a member when it differs from the member.</summary>
    /// <remarks>
    /// A color channel rounds to the nearest byte and clamps to 0 to 255. The corners and the opacity write the exact
    /// floats. A float compares with <see cref="float.Equals(float)"/>, as the style pass does.
    /// </remarks>
    /// <param name="style">The style to write.</param>
    /// <param name="property">A member that can transition.</param>
    /// <param name="value">The value as four floats.</param>
    /// <returns><see langword="true"/> when the member changed.</returns>
    private static bool Write(ref ComputedStyle style, StyleProperty property, in float4 value)
    {
        switch (property)
        {
            case StyleProperty.BackgroundColor: return WriteColor(ref style.BackgroundColor, in value);
            case StyleProperty.BorderColor: return WriteColor(ref style.BorderColor, in value);
            case StyleProperty.Color: return WriteColor(ref style.Color, in value);
            case StyleProperty.CornerRadius:
                Corners corners = new(value.X, value.Y, value.Z, value.W);
                Corners old = style.CornerRadius;
                if (old.TopLeft.Equals(corners.TopLeft)
                    && old.TopRight.Equals(corners.TopRight)
                    && old.BottomRight.Equals(corners.BottomRight)
                    && old.BottomLeft.Equals(corners.BottomLeft))
                {
                    return false;
                }

                style.CornerRadius = corners;
                return true;
            case StyleProperty.Opacity:
                if (style.Opacity.Equals(value.X))
                {
                    return false;
                }

                style.Opacity = value.X;
                return true;
            default: throw new ArgumentOutOfRangeException(nameof(property), property, "The member cannot transition.");
        }
    }

    /// <summary>Gives the value of a member at a part of the change.</summary>
    /// <remarks>
    /// <para>
    /// A color takes the step in premultiplied form: each RGB channel times its alpha, then a linear step, then a
    /// divide by the new alpha. Thus a fade from <see cref="Rgba8.Transparent"/> keeps the RGB channels of the target.
    /// With a new alpha of 0, the RGB channels take a straight linear step.
    /// </para>
    /// <para>The corners and the opacity take a linear step for each float.</para>
    /// </remarks>
    /// <param name="property">A member that can transition.</param>
    /// <param name="from">The start value.</param>
    /// <param name="to">The target.</param>
    /// <param name="t">The part of the change, from 0 to 1.</param>
    /// <returns>The value as four floats.</returns>
    private static float4 Interpolate(StyleProperty property, in float4 from, in float4 to, float t)
    {
        if (property is StyleProperty.CornerRadius or StyleProperty.Opacity)
        {
            return new float4(
                Lerp(from.X, to.X, t),
                Lerp(from.Y, to.Y, t),
                Lerp(from.Z, to.Z, t),
                Lerp(from.W, to.W, t));
        }

        float alpha = Lerp(from.W, to.W, t);
        if (alpha <= 0f)
        {
            return new float4(Lerp(from.X, to.X, t), Lerp(from.Y, to.Y, t), Lerp(from.Z, to.Z, t), alpha);
        }

        // The channel times the alpha is exact for bytes, and the divide by the new alpha takes the scale back out.
        return new float4(
            Lerp(from.X * from.W, to.X * to.W, t) / alpha,
            Lerp(from.Y * from.W, to.Y * to.W, t) / alpha,
            Lerp(from.Z * from.W, to.Z * to.W, t) / alpha,
            alpha);
    }

    /// <summary>Gives the linear step from one float to another.</summary>
    /// <param name="from">The start value.</param>
    /// <param name="to">The end value.</param>
    /// <param name="t">The part of the change.</param>
    /// <returns><c>from + (to - from) * t</c>.</returns>
    private static float Lerp(float from, float to, float t)
    {
        return from + ((to - from) * t);
    }

    /// <summary>Compares two values with <see cref="float.Equals(float)"/> on each float.</summary>
    /// <param name="a">The first value.</param>
    /// <param name="b">The second value.</param>
    /// <returns><see langword="true"/> when each float is equal. <c>NaN</c> equals <c>NaN</c>, and 0 equals -0.</returns>
    private static bool Equal(in float4 a, in float4 b)
    {
        return a.X.Equals(b.X) && a.Y.Equals(b.Y) && a.Z.Equals(b.Z) && a.W.Equals(b.W);
    }

    /// <summary>Checks that each float of a value is finite.</summary>
    /// <param name="value">The value.</param>
    /// <returns><see langword="true"/> when no float is <c>NaN</c> or infinity.</returns>
    private static bool IsFinite(in float4 value)
    {
        return float.IsFinite(value.X) && float.IsFinite(value.Y) && float.IsFinite(value.Z) && float.IsFinite(value.W);
    }

    /// <summary>Gives a color as four floats from 0 to 255.</summary>
    /// <param name="color">The color.</param>
    /// <returns>R, G, B, and A.</returns>
    private static float4 FromColor(Rgba8 color)
    {
        return new float4(color.R, color.G, color.B, color.A);
    }

    /// <summary>Writes a color of four floats into a color member when the bytes differ.</summary>
    /// <param name="member">The color member.</param>
    /// <param name="value">R, G, B, and A as floats.</param>
    /// <returns><see langword="true"/> when the member changed.</returns>
    private static bool WriteColor(ref Rgba8 member, in float4 value)
    {
        Rgba8 color = new(ToByte(value.X), ToByte(value.Y), ToByte(value.Z), ToByte(value.W));
        if (member.Equals(color))
        {
            return false;
        }

        member = color;
        return true;
    }

    /// <summary>Rounds a channel to the nearest byte and clamps it to 0 to 255.</summary>
    /// <param name="value">The channel. <c>NaN</c> gives 0.</param>
    /// <returns>The byte.</returns>
    private static byte ToByte(float value)
    {
        if (!(value > 0f))
        {
            return 0;
        }

        if (value >= 255f)
        {
            return 255;
        }

        return (byte)(value + 0.5f);
    }

    /// <summary>Takes a free record for an element and adds the element to the index.</summary>
    /// <remarks>The array grows to 4 records at the first rent and doubles after that. It never shrinks.</remarks>
    /// <param name="element">The element of the record.</param>
    /// <returns>The index of the record. The record holds no running slot.</returns>
    private int Rent(Element element)
    {
        if (_count == _records.Length)
        {
            Array.Resize(ref _records, _records.Length == 0 ? FirstCapacity : _records.Length * 2);
        }

        int index = _count;
        _count = index + 1;
        ref TransitionRecord record = ref _records[index];
        record = default;
        record.Element = element;
        _index.Add(element, index);
        return index;
    }

    /// <summary>
    /// Releases a record. The last record moves into the freed index, and the last index gets
    /// <c>default(TransitionRecord)</c>, so the pool holds no reference to the element.
    /// </summary>
    /// <param name="index">The index of the record.</param>
    private void Release(int index)
    {
        TransitionRecord[] records = _records;
        int last = _count - 1;
        _index.Remove(records[index].Element);

        if (index != last)
        {
            records[index] = records[last];
            _index[records[index].Element] = index;
        }

        records[last] = default;
        _count = last;
    }
}
