using System;
using Brutal.Numerics;

namespace BrutalUi.Core;

/// <summary>
/// The layout output of an element. The struct lives inline in the element.
/// </summary>
/// <remarks>
/// <para>
/// The layout pass of <see cref="UiContext.Update(float)"/> writes the struct. Code reads the result
/// through <see cref="Rect"/>. The other fields are the scratch state of the engine.
/// </para>
/// <para>
/// The struct is a mutable struct. <see cref="Element.Layout"/> returns a <c>ref</c> to it.
/// A write through that <c>ref</c> sets no dirty bit.
/// </para>
/// <para>The struct also holds the layout cache of the engine, inline, with no heap object.</para>
/// </remarks>
public struct LayoutNode
{
    /// <summary>
    /// The rectangle that the last layout pass computed. <see cref="Rect"/> reads it.
    /// </summary>
    /// <remarks>
    /// X and Y hold the position of the border box relative to the border box of the parent.
    /// Z holds the width and W holds the height. During the pass, X or Y of a child on a reverse main axis
    /// holds the offset from the flex-start edge. Step 10 of the engine converts it.
    /// </remarks>
    internal float4 ComputedRect;

    /// <summary>
    /// The padding plus the border of each edge from the last layout of the element, in pixels.
    /// X holds the left edge, Y the top edge, Z the right edge, and W the bottom edge.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The engine writes the value in the layout block of <c>LayoutEngine.LayoutInternal</c>, the block that writes
    /// the size of <see cref="ComputedRect"/>. The block runs for a computed layout and for a layout cache hit, so a
    /// cache hit writes the value too. A measurement does not write the value.
    /// </para>
    /// <para>
    /// The engine resolves a percent padding against the owner width of the layout call: the inner width of the parent,
    /// or the viewport width for a root. This is the width that the engine uses for the padding in the layout.
    /// An undefined owner width gives 0 for a percent padding. A border has no percent.
    /// </para>
    /// <para>
    /// A hidden element and each element of its subtree get zeros, as <see cref="ComputedRect"/> does.
    /// Before the first layout pass, all values are 0.
    /// </para>
    /// </remarks>
    internal float4 ContentInsets;

    /// <summary>
    /// The width of the border box from the last layout or measurement of the element, in pixels.
    /// A measurement writes it also when it does not write <see cref="ComputedRect"/>.
    /// </summary>
    internal float MeasuredWidth;

    /// <summary>
    /// The height of the border box from the last layout or measurement of the element, in pixels.
    /// A measurement writes it also when it does not write <see cref="ComputedRect"/>.
    /// </summary>
    internal float MeasuredHeight;

    /// <summary>
    /// The flex basis that the parent computed for the element in the current pass, in pixels.
    /// It includes no margin.
    /// </summary>
    internal float ComputedFlexBasis;

    /// <summary>
    /// The index of the flex line of the element in its parent, from the last layout or measurement of the parent.
    /// Yoga: <c>Node::lineIndex_</c>.
    /// </summary>
    /// <remarks>
    /// Step 4 of the engine writes it for each child in the flow. Step 8 reads it to find the children of each line.
    /// The value of a hidden child and of an absolute child is not used.
    /// </remarks>
    internal int LineIndex;

    /// <summary>
    /// The generation number of the last pass that visited the element. Yoga: <c>LayoutResults::generationCount</c>.
    /// </summary>
    /// <remarks>
    /// The engine resets <see cref="Cache"/> on the first visit of a dirty element in a pass. A value that is not
    /// the number of the current pass marks the first visit. The value 0 means that no pass visited the element.
    /// </remarks>
    internal long Generation;

    /// <summary>
    /// The layout cache of the element: one layout entry and 8 measurement entries (Decision 8).
    /// </summary>
    /// <remarks>
    /// The engine reads the cache before it lays out or measures the element. A hit gives the measured size and
    /// does not enter the subtree.
    /// </remarks>
    internal LayoutCache Cache;

    /// <summary>
    /// Gets the rectangle of the element from the last layout pass.
    /// </summary>
    /// <remarks>
    /// X and Y are the position of the border box relative to the border box of the parent. Z is the width,
    /// and W is the height. All values are in pixels. For a root, X and Y hold its left and top margin plus its
    /// offsets, resolved against <see cref="UiContext.ViewportSize"/>.
    /// To get a position in the viewport, add the positions of the ancestors on the walk down.
    /// Before the first layout pass, all values are 0.
    /// </remarks>
    public readonly float4 Rect => ComputedRect;

    /// <summary>
    /// Gives the content box of a border box: the border box minus <see cref="ContentInsets"/> on each edge.
    /// </summary>
    /// <remarks>
    /// The width is <c>w - (left + right)</c> and the height is <c>h - (top + bottom)</c>. The sum of the two insets
    /// comes first, in the order of the engine for the inner size. A negative width or height gives 0.
    /// </remarks>
    /// <param name="borderBox">The border box: X and Y are the position, Z is the width, and W is the height.</param>
    /// <returns>The content box in the same space as <paramref name="borderBox"/>.</returns>
    internal readonly float4 ContentBox(in float4 borderBox)
    {
        float4 insets = ContentInsets;
        return new float4(
            borderBox.X + insets.X,
            borderBox.Y + insets.Y,
            MathF.Max(borderBox.Z - (insets.X + insets.Z), 0f),
            MathF.Max(borderBox.W - (insets.Y + insets.W), 0f));
    }

    /// <summary>
    /// Gets or sets the measure callback of the element. The default is <see langword="null"/>.
    /// </summary>
    /// <remarks>
    /// The engine calls the callback for an element with no children only. A callback on an element with
    /// children has no effect. A set marks no dirty bit. After a set, call
    /// <see cref="Element.MarkDirty(DirtyFlags)"/> with <see cref="DirtyFlags.Layout"/>. See
    /// <see cref="MeasureCallback"/> for the rules of the callback.
    /// </remarks>
    public MeasureCallback? Measure { readonly get; set; }
}
