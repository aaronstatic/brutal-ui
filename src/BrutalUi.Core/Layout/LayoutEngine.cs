// Ported from Yoga (https://github.com/facebook/yoga), v3.2.1, yoga/algorithm/CalculateLayout.cpp, yoga/algorithm/TrailingPosition.h, yoga/algorithm/Align.h. Copyright (c) Meta Platforms, Inc. and affiliates.
// MIT License. See THIRD-PARTY-NOTICES.md at the repository root.

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
using Brutal.Numerics;

namespace BrutalUi.Core;

/// <summary>
/// The flexbox layout engine. A structural port of the Yoga 3 algorithm.
/// </summary>
/// <remarks>
/// <para>
/// The engine keeps the function split, the names (in Pascal case), the eleven steps of
/// <see cref="CalculateLayoutImpl"/>, and the formulas of Yoga. <c>NaN</c> means an undefined size, as
/// <c>YGUndefined</c>. Yoga uses a sizing mode inside the algorithm. The engine uses <see cref="MeasureMode"/>:
/// <see cref="MeasureMode.Exactly"/> for the stretch-fit mode, <see cref="MeasureMode.AtMost"/> for the
/// fit-content mode, and <see cref="MeasureMode.Undefined"/> for the max-content mode.
/// </para>
/// <para>
/// The engine reads <see cref="Element.Style"/> as <c>ref readonly</c> and writes <see cref="Element.Layout"/>.
/// It reads the children through the span of the child storage, and it never changes a child list.
/// It reads the layout bits for the cache, and it changes no dirty bit during the computation.
/// <see cref="Calculate"/> clears the layout bits after the computation.
/// </para>
/// <para>
/// Each element holds a <see cref="LayoutCache"/>. <see cref="LayoutInternal"/> reads the cache before it enters an
/// element. A hit gives the measured size and skips the subtree. Thus a clean subtree with the same constraints costs
/// one check.
/// </para>
/// <para>
/// A flex line is an index range over the child span. During a layout, X or Y of a child holds the offset from the
/// flex-start edge of its parent. Step 10 converts the offset on a reverse axis into X or Y in place.
/// </para>
/// </remarks>
internal static partial class LayoutEngine
{
    /// <summary>
    /// The generation counter. Each pass takes a new number. The layout cache uses it to reset the cache of a dirty
    /// element one time in each pass.
    /// The counter is static and atomic, because tests run trees on more than one thread.
    /// </summary>
    private static long s_generationCount;

    /// <summary>
    /// Lays out the tree below <paramref name="root"/> and clears the layout bits of the tree. Yoga: <c>calculateLayout</c>.
    /// </summary>
    /// <remarks>
    /// The root gets the position of its left and top margin, plus its relative offsets. The display type and the
    /// position type of the root have no effect. A defined style size of the root gives that size.
    /// Otherwise a defined maximum gives a limit, and the root takes the size of its content up to that limit.
    /// Otherwise a defined available size gives that size. Otherwise the root takes the size of its content.
    /// Step 9 of the algorithm then clamps the size to the minimum and the maximum of the root.
    /// </remarks>
    /// <param name="root">The root element of the pass.</param>
    /// <param name="availableWidth">The available width in pixels. <c>NaN</c> means no limit.</param>
    /// <param name="availableHeight">The available height in pixels. <c>NaN</c> means no limit.</param>
    internal static void Calculate(Element root, float availableWidth, float availableHeight)
    {
        // Each pass takes a new generation. The return value is the number of this pass, also when another
        // thread starts a pass at the same time.
        long generation = Interlocked.Increment(ref s_generationCount);

        ref readonly ComputedStyle style = ref root.Style;

        float width;
        MeasureMode widthMeasureMode;
        if (LayoutMath.HasDefiniteLength(in style, FlexDirection.Row, availableWidth))
        {
            width = LayoutMath.ResolvedDimension(in style, FlexDirection.Row, availableWidth)
                + LayoutMath.MarginForAxis(in style, FlexDirection.Row, availableWidth);
            widthMeasureMode = MeasureMode.Exactly;
        }
        else if (LayoutMath.IsDefined(LayoutMath.ResolvedMaxDimension(in style, FlexDirection.Row, availableWidth)))
        {
            // A defined maximum width limits the content width of the root.
            width = LayoutMath.ResolvedMaxDimension(in style, FlexDirection.Row, availableWidth);
            widthMeasureMode = MeasureMode.AtMost;
        }
        else
        {
            width = availableWidth;
            widthMeasureMode = LayoutMath.IsUndefined(width) ? MeasureMode.Undefined : MeasureMode.Exactly;
        }

        float height;
        MeasureMode heightMeasureMode;
        if (LayoutMath.HasDefiniteLength(in style, FlexDirection.Column, availableHeight))
        {
            height = LayoutMath.ResolvedDimension(in style, FlexDirection.Column, availableHeight)
                + LayoutMath.MarginForAxis(in style, FlexDirection.Column, availableWidth);
            heightMeasureMode = MeasureMode.Exactly;
        }
        else if (LayoutMath.IsDefined(LayoutMath.ResolvedMaxDimension(in style, FlexDirection.Column, availableHeight)))
        {
            // A defined maximum height limits the content height of the root.
            height = LayoutMath.ResolvedMaxDimension(in style, FlexDirection.Column, availableHeight);
            heightMeasureMode = MeasureMode.AtMost;
        }
        else
        {
            height = availableHeight;
            heightMeasureMode = LayoutMath.IsUndefined(height) ? MeasureMode.Undefined : MeasureMode.Exactly;
        }

        // The root goes through the same cache lookup as each other element. A clean root with the same viewport
        // is a hit, and the call returns after one check.
        LayoutInternal(
            root,
            width,
            height,
            widthMeasureMode,
            heightMeasureMode,
            availableWidth,
            availableHeight,
            performLayout: true,
            generation);

        // Yoga: setPosition on the root. The root position is the left and top margin plus the relative offsets.
        // The position type of the root does not change the result: an absolute root and a relative root get
        // the same layout and the same offsets, as in Yoga. Yoga sets the position only when the root misses the
        // cache. The engine sets it on each pass: a percent margin or offset of the root resolves against the
        // viewport, and a hit at the root does not see a change of the viewport that keeps the root size.
        ref LayoutNode layout = ref root.Layout;
        layout.ComputedRect.X = LayoutMath.LeadingMargin(in style, FlexDirection.Row, availableWidth)
            + LayoutMath.RelativePosition(in style, FlexDirection.Row, availableWidth);
        layout.ComputedRect.Y = LayoutMath.LeadingMargin(in style, FlexDirection.Column, availableWidth)
            + LayoutMath.RelativePosition(in style, FlexDirection.Column, availableHeight);

        // The clear walk. It enters each element with a layout bit, also an element that the computation skipped,
        // and clears the self bit and the descendant bit of the layout kind only.
        ClearVisitor clear = default;
        DirtyWalk.Walk(root, DirtyFlags.Layout, ref clear);
    }

    /// <summary>
    /// Lays out or measures one element. Yoga: <c>calculateLayoutInternal</c>.
    /// </summary>
    /// <remarks>
    /// The method writes <see cref="LayoutNode.MeasuredWidth"/> and <see cref="LayoutNode.MeasuredHeight"/>.
    /// When <paramref name="performLayout"/> is <see langword="true"/>, it also writes the size of
    /// <see cref="LayoutNode.ComputedRect"/> and the positions of the children. The parent writes the position of the element.
    /// </remarks>
    /// <param name="node">The element.</param>
    /// <param name="availableWidth">The available width with the margins, in pixels. <c>NaN</c> means no limit.</param>
    /// <param name="availableHeight">The available height with the margins, in pixels. <c>NaN</c> means no limit.</param>
    /// <param name="widthMeasureMode">The rule for the available width.</param>
    /// <param name="heightMeasureMode">The rule for the available height.</param>
    /// <param name="ownerWidth">The inner width of the parent, for a percent. <c>NaN</c> means undefined.</param>
    /// <param name="ownerHeight">The inner height of the parent, for a percent. <c>NaN</c> means undefined.</param>
    /// <param name="performLayout"><see langword="true"/> for a full layout. <see langword="false"/> for a measurement only.</param>
    /// <param name="generation">The generation number of the pass.</param>
    /// <returns><see langword="true"/> when the method computed the layout, and <see langword="false"/> for a cache hit.</returns>
    private static bool LayoutInternal(
        Element node,
        float availableWidth,
        float availableHeight,
        MeasureMode widthMeasureMode,
        MeasureMode heightMeasureMode,
        float ownerWidth,
        float ownerHeight,
        bool performLayout,
        long generation)
    {
        ref LayoutNode layout = ref node.Layout;
        ref LayoutCache cache = ref layout.Cache;

        // An element is dirty for layout when it holds the self bit or the descendant bit of the layout kind.
        // The pass does not change the bits, so the test gives the same answer on each visit in one pass.
        // The first visit of a dirty element in a pass resets the cache. A later visit in the same pass can use the
        // entries of that pass. An element that no pass visited (generation 0) also resets its cache, because the
        // default entries are not the empty state.
        bool isDirty = (node.Dirty & (DirtyFlags.Layout | DirtyFlags.DescendantLayout)) != DirtyFlags.None;
        bool needToVisitNode = (isDirty && layout.Generation != generation) || layout.Generation == 0;

        if (needToVisitNode)
        {
            cache.Reset();
        }

        // The computation of an element resolves some percents against the owner size (see UsesOwnerSize). For such
        // an element, the owner size is a part of the key of each entry, so a hit needs the same owner size. Then
        // the entries for two owner sizes stay valid together, also between two visits in one pass. Yoga has no
        // owner size in the key. After a reset, the flag can be stale, but then no entry is valid.
        bool keyHasOwnerSize = cache.UsesOwnerSize;

        // Find a cached result. A full layout of a container uses the layout entry only, because a layout writes
        // the positions of the children. A measurement uses the measurement entries only. A leaf with a measure
        // callback reads both, with the relaxed rules of CanUseCachedMeasurement, because a callback costs the most.
        ref CachedMeasurement cachedResults = ref Unsafe.NullRef<CachedMeasurement>();
        if (layout.Measure is not null && node.ChildStorage.AsSpan().Length == 0)
        {
            ref readonly ComputedStyle style = ref node.Style;
            float marginAxisRow = LayoutMath.MarginForAxis(in style, FlexDirection.Row, ownerWidth);
            float marginAxisColumn = LayoutMath.MarginForAxis(in style, FlexDirection.Column, ownerWidth);

            // The relaxed rules read the stored size as the result of the callback. A minimum or a maximum can clamp
            // the stored size, and then the rules give a wrong size. Yoga uses the relaxed rules here too.
            // The engine uses them only for a leaf with no minimum and no maximum. Other leaves need the same
            // constraints for a hit.
            bool useRelaxedRules = !HasMinOrMaxSize(in style);

            if (CanUseCachedEntry(
                    in cache.CachedLayout,
                    useRelaxedRules,
                    widthMeasureMode,
                    availableWidth,
                    heightMeasureMode,
                    availableHeight,
                    marginAxisRow,
                    marginAxisColumn)
                && (!keyHasOwnerSize || LayoutCache.HasSameOwnerSize(in cache.CachedLayout, ownerWidth, ownerHeight)))
            {
                cachedResults = ref cache.CachedLayout;
            }
            else
            {
                for (int i = 0; i < cache.NextCachedMeasurementsIndex; i++)
                {
                    ref CachedMeasurement entry = ref cache.CachedMeasurements[i];
                    if (CanUseCachedEntry(
                            in entry,
                            useRelaxedRules,
                            widthMeasureMode,
                            availableWidth,
                            heightMeasureMode,
                            availableHeight,
                            marginAxisRow,
                            marginAxisColumn)
                        && (!keyHasOwnerSize || LayoutCache.HasSameOwnerSize(in entry, ownerWidth, ownerHeight)))
                    {
                        cachedResults = ref entry;
                        break;
                    }
                }
            }
        }
        else if (performLayout)
        {
            ref CachedMeasurement entry = ref cache.CachedLayout;
            if (LayoutCache.HasSameSpec(in entry, widthMeasureMode, availableWidth, heightMeasureMode, availableHeight)
                && (!keyHasOwnerSize || LayoutCache.HasSameOwnerSize(in entry, ownerWidth, ownerHeight)))
            {
                cachedResults = ref entry;
            }
        }
        else
        {
            for (int i = 0; i < cache.NextCachedMeasurementsIndex; i++)
            {
                ref CachedMeasurement entry = ref cache.CachedMeasurements[i];
                if (LayoutCache.HasSameSpec(in entry, widthMeasureMode, availableWidth, heightMeasureMode, availableHeight)
                    && (!keyHasOwnerSize || LayoutCache.HasSameOwnerSize(in entry, ownerWidth, ownerHeight)))
                {
                    cachedResults = ref entry;
                    break;
                }
            }
        }

        bool hasCachedResults = !Unsafe.IsNullRef(ref cachedResults);
        if (!needToVisitNode && hasCachedResults)
        {
            // A hit. The measured size comes from the cache, and the subtree keeps its layout.
            layout.MeasuredWidth = cachedResults.ComputedWidth;
            layout.MeasuredHeight = cachedResults.ComputedHeight;
        }
        else
        {
            CalculateLayoutImpl(
                node,
                availableWidth,
                availableHeight,
                widthMeasureMode,
                heightMeasureMode,
                ownerWidth,
                ownerHeight,
                performLayout,
                generation);

            // A style change marks the element or its parent, so the flag stays correct while the element is clean.
            cache.UsesOwnerSize = UsesOwnerSize(node);

            // Yoga stores no new entry when the lookup found one, also on the first visit of a dirty element.
            if (!hasCachedResults)
            {
                if (cache.NextCachedMeasurementsIndex == LayoutCache.MaxCachedMeasurements)
                {
                    cache.NextCachedMeasurementsIndex = 0;
                }

                ref CachedMeasurement newCacheEntry = ref cache.CachedLayout;
                if (!performLayout)
                {
                    newCacheEntry = ref cache.CachedMeasurements[cache.NextCachedMeasurementsIndex];
                    cache.NextCachedMeasurementsIndex++;
                }

                newCacheEntry.AvailableWidth = availableWidth;
                newCacheEntry.AvailableHeight = availableHeight;
                newCacheEntry.OwnerWidth = ownerWidth;
                newCacheEntry.OwnerHeight = ownerHeight;
                newCacheEntry.WidthMeasureMode = widthMeasureMode;
                newCacheEntry.HeightMeasureMode = heightMeasureMode;
                newCacheEntry.ComputedWidth = layout.MeasuredWidth;
                newCacheEntry.ComputedHeight = layout.MeasuredHeight;
            }
        }

        if (performLayout)
        {
            layout.ComputedRect.Z = layout.MeasuredWidth;
            layout.ComputedRect.W = layout.MeasuredHeight;
        }

        layout.Generation = generation;

        return needToVisitNode || !hasCachedResults;
    }

    /// <summary>
    /// Tells if a cached entry of a leaf with a measure callback is valid for new constraints.
    /// </summary>
    /// <param name="entry">The cached entry.</param>
    /// <param name="useRelaxedRules">
    /// <see langword="true"/> for the rules of <see cref="LayoutCache.CanUseCachedMeasurement"/>, and
    /// <see langword="false"/> for the same constraints only.
    /// </param>
    /// <param name="widthMode">The new rule for the width.</param>
    /// <param name="availableWidth">The new available width, with the margins.</param>
    /// <param name="heightMode">The new rule for the height.</param>
    /// <param name="availableHeight">The new available height, with the margins.</param>
    /// <param name="marginRow">The sum of the left and the right margin of the leaf.</param>
    /// <param name="marginColumn">The sum of the top and the bottom margin of the leaf.</param>
    /// <returns><see langword="true"/> when the cached size is the result for the new constraints.</returns>
    private static bool CanUseCachedEntry(
        ref readonly CachedMeasurement entry,
        bool useRelaxedRules,
        MeasureMode widthMode,
        float availableWidth,
        MeasureMode heightMode,
        float availableHeight,
        float marginRow,
        float marginColumn)
    {
        if (useRelaxedRules)
        {
            return LayoutCache.CanUseCachedMeasurement(
                widthMode,
                availableWidth,
                heightMode,
                availableHeight,
                in entry,
                marginRow,
                marginColumn);
        }

        return LayoutCache.HasSameSpec(in entry, widthMode, availableWidth, heightMode, availableHeight);
    }

    /// <summary>
    /// Tells if an element has a minimum or a maximum size on one of the two axes.
    /// </summary>
    /// <param name="style">The style of the element.</param>
    /// <returns><see langword="true"/> when one of the four limits holds a pixel or a percent value.</returns>
    private static bool HasMinOrMaxSize(in ComputedStyle style)
    {
        return IsPixelOrPercent(style.MinWidth)
            || IsPixelOrPercent(style.MaxWidth)
            || IsPixelOrPercent(style.MinHeight)
            || IsPixelOrPercent(style.MaxHeight);
    }

    /// <summary>
    /// Tells if the computation of an element can read its owner size through a percent.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The element resolves its own percent sizes, limits, margins, and paddings against the owner size. As in Yoga,
    /// it also resolves the percent limits and paddings of its children against its own owner size
    /// (<c>mainAxisOwnerSize</c>). A child gets the inner size of the element as its owner size, so the test needs
    /// one level of children only.
    /// </para>
    /// <para>The method tests all children, also a hidden child and an absolute child, because a larger set is safe.</para>
    /// </remarks>
    /// <param name="node">The element.</param>
    /// <returns><see langword="true"/> when the element or one of its children holds a percent in a tested field.</returns>
    private static bool UsesOwnerSize(Element node)
    {
        if (HasOwnerRelativePercent(in node.Style))
        {
            return true;
        }

        ReadOnlySpan<Element> children = node.ChildStorage.AsSpan();
        for (int i = 0; i < children.Length; i++)
        {
            if (HasOwnerRelativePercent(in children[i].Style))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Tells if a style holds a percent in a field that the engine can resolve against an owner size.
    /// </summary>
    /// <remarks>
    /// The fields are the size, the minimum, and the maximum on each axis, the flex basis, and the four margins and
    /// paddings. The parent resolves the offsets of a child against its inner size, so they are not in the list.
    /// A border has no percent.
    /// </remarks>
    /// <param name="style">The style.</param>
    /// <returns><see langword="true"/> when one of the fields holds a percent value.</returns>
    private static bool HasOwnerRelativePercent(in ComputedStyle style)
    {
        return style.FlexBasis.Unit == LengthUnit.Percent
            || style.Width.Unit == LengthUnit.Percent
            || style.Height.Unit == LengthUnit.Percent
            || style.MinWidth.Unit == LengthUnit.Percent
            || style.MinHeight.Unit == LengthUnit.Percent
            || style.MaxWidth.Unit == LengthUnit.Percent
            || style.MaxHeight.Unit == LengthUnit.Percent
            || HasPercentEdge(in style.Margin)
            || HasPercentEdge(in style.Padding);
    }

    /// <summary>
    /// Tells if one of the four edges holds a percent value.
    /// </summary>
    /// <param name="edges">The edges.</param>
    /// <returns><see langword="true"/> when one edge holds a percent value.</returns>
    private static bool HasPercentEdge(in LengthEdges edges)
    {
        return edges.Left.Unit == LengthUnit.Percent
            || edges.Top.Unit == LengthUnit.Percent
            || edges.Right.Unit == LengthUnit.Percent
            || edges.Bottom.Unit == LengthUnit.Percent;
    }

    /// <summary>
    /// Tells if a length holds a pixel or a percent value.
    /// </summary>
    /// <param name="length">The length.</param>
    /// <returns><see langword="true"/> for <see cref="LengthUnit.Pixel"/> and <see cref="LengthUnit.Percent"/>.</returns>
    private static bool IsPixelOrPercent(Length length)
    {
        return length.Unit == LengthUnit.Pixel || length.Unit == LengthUnit.Percent;
    }

    /// <summary>
    /// Runs the flexbox algorithm on one element. Yoga: <c>calculateLayoutImpl</c>.
    /// </summary>
    /// <remarks>
    /// When an available size is <c>NaN</c>, its measure mode must be <see cref="MeasureMode.Undefined"/>.
    /// The measured size includes the padding and the border, but not the margins.
    /// </remarks>
    /// <param name="node">The element.</param>
    /// <param name="availableWidth">The available width with the margins, in pixels. <c>NaN</c> means no limit.</param>
    /// <param name="availableHeight">The available height with the margins, in pixels. <c>NaN</c> means no limit.</param>
    /// <param name="widthMeasureMode">The rule for the available width.</param>
    /// <param name="heightMeasureMode">The rule for the available height.</param>
    /// <param name="ownerWidth">The inner width of the parent, for a percent. <c>NaN</c> means undefined.</param>
    /// <param name="ownerHeight">The inner height of the parent, for a percent. <c>NaN</c> means undefined.</param>
    /// <param name="performLayout"><see langword="true"/> for a full layout. <see langword="false"/> for a measurement only.</param>
    /// <param name="generation">The generation number of the pass.</param>
    private static void CalculateLayoutImpl(
        Element node,
        float availableWidth,
        float availableHeight,
        MeasureMode widthMeasureMode,
        MeasureMode heightMeasureMode,
        float ownerWidth,
        float ownerHeight,
        bool performLayout,
        long generation)
    {
        Debug.Assert(
            LayoutMath.IsDefined(availableWidth) || widthMeasureMode == MeasureMode.Undefined,
            "An undefined available width needs the measure mode Undefined.");
        Debug.Assert(
            LayoutMath.IsDefined(availableHeight) || heightMeasureMode == MeasureMode.Undefined,
            "An undefined available height needs the measure mode Undefined.");

        ref readonly ComputedStyle style = ref node.Style;
        ref LayoutNode layout = ref node.Layout;

        // The engine does not store the margins, the borders, and the paddings in the layout data.
        // The style gives them again when a step needs them.
        float marginAxisRow = LayoutMath.MarginForAxis(in style, FlexDirection.Row, ownerWidth);
        float marginAxisColumn = LayoutMath.MarginForAxis(in style, FlexDirection.Column, ownerWidth);

        // A leaf with a measure callback takes the size of its content. A callback on an element with children has
        // no effect (A10). Yoga forbids the combination. The engine skips the callback instead.
        ReadOnlySpan<Element> children = node.ChildStorage.AsSpan();
        MeasureCallback? measure = layout.Measure;
        if (children.Length == 0 && measure is not null)
        {
            MeasureNodeWithMeasureFunc(
                node,
                measure,
                availableWidth - marginAxisRow,
                availableHeight - marginAxisColumn,
                widthMeasureMode,
                heightMeasureMode,
                ownerWidth,
                ownerHeight);
            return;
        }

        if (children.Length == 0)
        {
            MeasureNodeWithoutChildren(
                node,
                availableWidth - marginAxisRow,
                availableHeight - marginAxisColumn,
                widthMeasureMode,
                heightMeasureMode,
                ownerWidth,
                ownerHeight);
            return;
        }

        // A measurement can skip the algorithm when the size is already known.
        if (!performLayout
            && MeasureNodeWithFixedSize(
                node,
                availableWidth - marginAxisRow,
                availableHeight - marginAxisColumn,
                widthMeasureMode,
                heightMeasureMode,
                ownerWidth,
                ownerHeight))
        {
            return;
        }

        // Step 1: calculate the values for the rest of the algorithm.
        FlexDirection mainAxis = style.FlexDirection;
        FlexDirection crossAxis = LayoutMath.CrossAxis(mainAxis);
        bool isMainAxisRow = LayoutMath.IsRow(mainAxis);

        bool isNodeFlexWrap = style.FlexWrap != FlexWrap.NoWrap;

        float mainAxisOwnerSize = isMainAxisRow ? ownerWidth : ownerHeight;
        float crossAxisOwnerSize = isMainAxisRow ? ownerHeight : ownerWidth;

        float paddingAndBorderAxisMain = LayoutMath.PaddingAndBorderForAxis(in style, mainAxis, ownerWidth);
        float paddingAndBorderAxisCross = LayoutMath.PaddingAndBorderForAxis(in style, crossAxis, ownerWidth);

        float leadingPaddingAndBorderCross = LayoutMath.LeadingPaddingAndBorder(in style, crossAxis, ownerWidth);

        MeasureMode measureModeMainDim = isMainAxisRow ? widthMeasureMode : heightMeasureMode;
        MeasureMode measureModeCrossDim = isMainAxisRow ? heightMeasureMode : widthMeasureMode;

        float paddingAndBorderAxisRow = isMainAxisRow ? paddingAndBorderAxisMain : paddingAndBorderAxisCross;
        float paddingAndBorderAxisColumn = isMainAxisRow ? paddingAndBorderAxisCross : paddingAndBorderAxisMain;

        // Step 2: determine the available size in the main and the cross directions.
        float availableInnerWidth = CalculateAvailableInnerDimension(
            in style,
            FlexDirection.Row,
            availableWidth - marginAxisRow,
            paddingAndBorderAxisRow,
            ownerWidth,
            ownerWidth);
        float availableInnerHeight = CalculateAvailableInnerDimension(
            in style,
            FlexDirection.Column,
            availableHeight - marginAxisColumn,
            paddingAndBorderAxisColumn,
            ownerHeight,
            ownerWidth);

        float availableInnerMainDim = isMainAxisRow ? availableInnerWidth : availableInnerHeight;
        float availableInnerCrossDim = isMainAxisRow ? availableInnerHeight : availableInnerWidth;

        // Step 3: determine the flex basis for each item.
        float totalMainDim = ComputeFlexBasisForChildren(
            node,
            children,
            availableInnerWidth,
            availableInnerHeight,
            widthMeasureMode,
            heightMeasureMode,
            mainAxis,
            performLayout,
            generation);

        bool mainAxisOverflows = measureModeMainDim != MeasureMode.Undefined && totalMainDim > availableInnerMainDim;

        // A wrapped container with an overflow fills the available main size: its children go on more lines.
        if (isNodeFlexWrap && mainAxisOverflows && measureModeMainDim == MeasureMode.AtMost)
        {
            measureModeMainDim = MeasureMode.Exactly;
        }

        // Step 4: collect the flex items into flex lines.
        // Each line is an index range over the child span (Decision 7). Steps 5 to 7 run for each line, and the
        // next line starts at the end index of the line. The line count is 1 or more, because the span is not empty.
        int startOfLineIndex = 0;
        int lineCount = 0;

        // The sum of the cross sizes of the lines so far.
        float totalLineCrossDim = 0f;

        // The largest main size of the lines.
        float maxLineMainDim = 0f;
        for (; startOfLineIndex < children.Length; lineCount++)
        {
            FlexLine flexLine = CalculateFlexLine(
                node,
                children,
                ownerWidth,
                mainAxisOwnerSize,
                availableInnerWidth,
                availableInnerMainDim,
                startOfLineIndex,
                lineCount);
            startOfLineIndex = flexLine.EndIndex;

            // A measurement with an exact cross size does not need the flex step.
            bool canSkipFlex = !performLayout && measureModeCrossDim == MeasureMode.Exactly;

            // Step 5: resolve the flexible lengths on the main axis.
            // When the main size is not known, the line length gives it, and no free space stays.
            bool sizeBasedOnContent = false;
            if (measureModeMainDim != MeasureMode.Exactly)
            {
                // With no exact main size, the size must obey the minimum and the maximum of the element.
                float minInnerWidth = LayoutMath.ResolvedMinDimension(in style, FlexDirection.Row, ownerWidth)
                    - paddingAndBorderAxisRow;
                float maxInnerWidth = LayoutMath.ResolvedMaxDimension(in style, FlexDirection.Row, ownerWidth)
                    - paddingAndBorderAxisRow;
                float minInnerHeight = LayoutMath.ResolvedMinDimension(in style, FlexDirection.Column, ownerHeight)
                    - paddingAndBorderAxisColumn;
                float maxInnerHeight = LayoutMath.ResolvedMaxDimension(in style, FlexDirection.Column, ownerHeight)
                    - paddingAndBorderAxisColumn;

                float minInnerMainDim = isMainAxisRow ? minInnerWidth : minInnerHeight;
                float maxInnerMainDim = isMainAxisRow ? maxInnerWidth : maxInnerHeight;

                if (LayoutMath.IsDefined(minInnerMainDim) && flexLine.SizeConsumed < minInnerMainDim)
                {
                    availableInnerMainDim = minInnerMainDim;
                }
                else if (LayoutMath.IsDefined(maxInnerMainDim) && flexLine.SizeConsumed > maxInnerMainDim)
                {
                    availableInnerMainDim = maxInnerMainDim;
                }
                else
                {
                    // Yoga keeps a legacy branch here for the StretchFlexBasis errata. The engine has no errata.
                    float nodeFlexGrow = ResolveFlexGrow(node);
                    if ((LayoutMath.IsDefined(flexLine.TotalFlexGrowFactors) && flexLine.TotalFlexGrowFactors == 0f)
                        || (LayoutMath.IsDefined(nodeFlexGrow) && nodeFlexGrow == 0f))
                    {
                        // No child can grow, or the element itself cannot grow, so the used space is all the space
                        // that the line needs. A root element also takes the smallest size.
                        availableInnerMainDim = flexLine.SizeConsumed;
                    }

                    sizeBasedOnContent = true;
                }
            }

            if (!sizeBasedOnContent && LayoutMath.IsDefined(availableInnerMainDim))
            {
                flexLine.RemainingFreeSpace = availableInnerMainDim - flexLine.SizeConsumed;
            }
            else if (flexLine.SizeConsumed < 0f)
            {
                // The content gives the size, and the used space is negative. The element gives 0 to its content.
                flexLine.RemainingFreeSpace = -flexLine.SizeConsumed;
            }

            if (!canSkipFlex)
            {
                ResolveFlexibleLength(
                    node,
                    children,
                    ref flexLine,
                    mainAxis,
                    crossAxis,
                    ownerWidth,
                    mainAxisOwnerSize,
                    availableInnerMainDim,
                    availableInnerCrossDim,
                    availableInnerWidth,
                    availableInnerHeight,
                    mainAxisOverflows,
                    measureModeCrossDim,
                    performLayout,
                    generation);
            }

            // Step 6: main-axis justification and cross-axis size determination.
            JustifyMainAxis(
                in style,
                children,
                ref flexLine,
                mainAxis,
                crossAxis,
                measureModeMainDim,
                measureModeCrossDim,
                mainAxisOwnerSize,
                ownerWidth,
                availableInnerMainDim,
                availableInnerCrossDim,
                availableInnerWidth,
                performLayout);

            float containerCrossAxis = availableInnerCrossDim;
            if (measureModeCrossDim == MeasureMode.Undefined || measureModeCrossDim == MeasureMode.AtMost)
            {
                // The largest cross size of the children gives the cross size of the container.
                containerCrossAxis = LayoutMath.BoundAxis(
                    in style,
                    crossAxis,
                    flexLine.CrossDim + paddingAndBorderAxisCross,
                    crossAxisOwnerSize,
                    ownerWidth) - paddingAndBorderAxisCross;
            }

            // With no wrap, the container gives the cross size of the line.
            if (!isNodeFlexWrap && measureModeCrossDim == MeasureMode.Exactly)
            {
                flexLine.CrossDim = availableInnerCrossDim;
            }

            // The cross size of the line in a single-line container obeys the minimum and the maximum.
            if (!isNodeFlexWrap)
            {
                flexLine.CrossDim = LayoutMath.BoundAxis(
                    in style,
                    crossAxis,
                    flexLine.CrossDim + paddingAndBorderAxisCross,
                    crossAxisOwnerSize,
                    ownerWidth) - paddingAndBorderAxisCross;
            }

            // Step 7: cross-axis alignment.
            // A measurement of the container does not need the alignment of the children.
            if (performLayout)
            {
                for (int i = flexLine.StartIndex; i < flexLine.EndIndex; i++)
                {
                    Element child = children[i];
                    ref readonly ComputedStyle childStyle = ref child.Style;
                    if (!IsInFlow(in childStyle))
                    {
                        continue;
                    }

                    ref LayoutNode childLayout = ref child.Layout;

                    float leadingCrossDim = leadingPaddingAndBorderCross;

                    // The align-self value of the child, or else the align-items value of the container, gives
                    // the position on the cross axis.
                    Align alignItem = LayoutMath.ResolveChildAlignment(in style, in childStyle);

                    // A child with the stretch alignment gets one more layout, with the cross size of the line
                    // as the exact cross size. An auto margin on the cross axis stops the stretch.
                    if (alignItem == Align.Stretch
                        && !LayoutMath.FlexStartMarginIsAuto(in childStyle, crossAxis)
                        && !LayoutMath.FlexEndMarginIsAuto(in childStyle, crossAxis))
                    {
                        // A child with a definite cross size does not stretch. The other children get a second
                        // layout with the cross size of the line.
                        if (!LayoutMath.HasDefiniteLength(in childStyle, crossAxis, availableInnerCrossDim))
                        {
                            float childMainSize = LayoutMath.MeasuredDimension(in childLayout, mainAxis);
                            float childCrossSize = flexLine.CrossDim;

                            childMainSize += LayoutMath.MarginForAxis(in childStyle, mainAxis, availableInnerWidth);

                            // The maximum of the child limits the two sizes. Yoga keeps only the sizes here.
                            // The rules below come from the sizes, so the changed rules are not used.
                            MeasureMode childMainMeasureMode = MeasureMode.Exactly;
                            MeasureMode childCrossMeasureMode = MeasureMode.Exactly;
                            ConstrainMaxSizeForMode(
                                in childStyle,
                                mainAxis,
                                availableInnerMainDim,
                                availableInnerWidth,
                                ref childMainMeasureMode,
                                ref childMainSize);
                            ConstrainMaxSizeForMode(
                                in childStyle,
                                crossAxis,
                                availableInnerCrossDim,
                                availableInnerWidth,
                                ref childCrossMeasureMode,
                                ref childCrossSize);

                            float childWidth = isMainAxisRow ? childMainSize : childCrossSize;
                            float childHeight = !isMainAxisRow ? childMainSize : childCrossSize;

                            // In a wrapped container with no stretch of the lines, the cross size of a line can
                            // still change in step 8. Thus the child takes the size of its content on the cross
                            // axis here, and step 8 lays it out again with the final cross size of the line.
                            bool crossAxisDoesNotGrow = style.AlignContent != Align.Stretch && isNodeFlexWrap;
                            MeasureMode childWidthMeasureMode =
                                LayoutMath.IsUndefined(childWidth) || (!isMainAxisRow && crossAxisDoesNotGrow)
                                    ? MeasureMode.Undefined
                                    : MeasureMode.Exactly;
                            MeasureMode childHeightMeasureMode =
                                LayoutMath.IsUndefined(childHeight) || (isMainAxisRow && crossAxisDoesNotGrow)
                                    ? MeasureMode.Undefined
                                    : MeasureMode.Exactly;

                            LayoutInternal(
                                child,
                                childWidth,
                                childHeight,
                                childWidthMeasureMode,
                                childHeightMeasureMode,
                                availableInnerWidth,
                                availableInnerHeight,
                                performLayout: true,
                                generation);
                        }
                    }
                    else
                    {
                        // The free space on the cross axis of the container, after the outer size of the child.
                        // It is negative when the child is larger than the container.
                        float remainingCrossDim = containerCrossAxis
                            - LayoutMath.DimensionWithMargin(in childStyle, in childLayout, crossAxis, availableInnerWidth);

                        // The auto margins on the cross axis win over the alignment. Two auto margins center the
                        // child. One auto margin pushes the child to the other edge. An auto margin takes no
                        // negative space, so a child that is larger than the line stays at the flex-start edge.
                        bool flexStartMarginIsAuto = LayoutMath.FlexStartMarginIsAuto(in childStyle, crossAxis);
                        bool flexEndMarginIsAuto = LayoutMath.FlexEndMarginIsAuto(in childStyle, crossAxis);
                        if (flexStartMarginIsAuto && flexEndMarginIsAuto)
                        {
                            leadingCrossDim += LayoutMath.MaxOrDefined(0f, remainingCrossDim / 2f);
                        }
                        else if (flexEndMarginIsAuto)
                        {
                            // The auto margin on the flex-end edge takes the free space. The child stays at the
                            // flex-start edge.
                        }
                        else if (flexStartMarginIsAuto)
                        {
                            leadingCrossDim += LayoutMath.MaxOrDefined(0f, remainingCrossDim);
                        }
                        else if (alignItem == Align.FlexStart)
                        {
                            // The child stays at the flex-start edge.
                        }
                        else if (alignItem == Align.Center)
                        {
                            leadingCrossDim += remainingCrossDim / 2f;
                        }
                        else
                        {
                            leadingCrossDim += remainingCrossDim;
                        }
                    }

                    // Apply the position.
                    PositionOnAxis(ref childLayout, crossAxis) += totalLineCrossDim + leadingCrossDim;
                }
            }

            totalLineCrossDim += flexLine.CrossDim;
            maxLineMainDim = LayoutMath.MaxOrDefined(maxLineMainDim, flexLine.MainDim);
        }

        // Step 8: multi-line content alignment.
        // Yoga also runs this step for a baseline layout. The engine has no baseline alignment (A7).
        if (performLayout && isNodeFlexWrap)
        {
            float leadPerLine = 0f;
            float currentLead = leadingPaddingAndBorderCross;
            float extraSpacePerLine = 0f;

            float unclampedCrossDim;
            if (measureModeCrossDim == MeasureMode.Exactly)
            {
                unclampedCrossDim = availableInnerCrossDim + paddingAndBorderAxisCross;
            }
            else if (LayoutMath.HasDefiniteLength(in style, crossAxis, crossAxisOwnerSize))
            {
                unclampedCrossDim = LayoutMath.ResolvedDimension(in style, crossAxis, crossAxisOwnerSize);
            }
            else
            {
                unclampedCrossDim = totalLineCrossDim + paddingAndBorderAxisCross;
            }

            // CSS difference from Yoga: Yoga gives the owner height as the axis size here, also when the cross axis
            // is the row axis. The engine gives the owner size of the cross axis, as step 9 does for the same bound.
            // Thus a percent minimum or maximum width of a wrapped column resolves against the owner width.
            float innerCrossDim = LayoutMath.BoundAxis(
                in style,
                crossAxis,
                unclampedCrossDim,
                crossAxisOwnerSize,
                ownerWidth) - paddingAndBorderAxisCross;

            float remainingAlignContentDim = innerCrossDim - totalLineCrossDim;

            // When the lines need more space than the element has, the space values and stretch act as flex-start.
            Align alignContent = remainingAlignContentDim >= 0f
                ? style.AlignContent
                : FallbackAlignment(style.AlignContent);

            switch (alignContent)
            {
                case Align.FlexEnd:
                    currentLead += remainingAlignContentDim;
                    break;
                case Align.Center:
                    currentLead += remainingAlignContentDim / 2f;
                    break;
                case Align.Stretch:
                    extraSpacePerLine = remainingAlignContentDim / lineCount;
                    break;
                case Align.SpaceAround:
                    currentLead += remainingAlignContentDim / (2f * lineCount);
                    leadPerLine = remainingAlignContentDim / lineCount;
                    break;
                case Align.SpaceEvenly:
                    currentLead += remainingAlignContentDim / (lineCount + 1);
                    leadPerLine = remainingAlignContentDim / (lineCount + 1);
                    break;
                case Align.SpaceBetween:
                    if (lineCount > 1)
                    {
                        leadPerLine = remainingAlignContentDim / (lineCount - 1);
                    }

                    break;
                default:
                    // Align.Auto and Align.FlexStart: the lines start at the flex-start edge.
                    break;
            }

            // The relative offset on the cross axis resolves against the inner cross size, as in step 3.
            float crossAxisInnerSize = isMainAxisRow ? availableInnerHeight : availableInnerWidth;

            int endIndex = 0;
            for (int lineIndex = 0; lineIndex < lineCount; lineIndex++)
            {
                int startIndex = endIndex;
                int index = startIndex;

                // Find the end index of the line, and its cross size: the largest cross size of its items, with the
                // cross margins. An absolute child and a hidden child do not end a line and add no size.
                float lineHeight = 0f;
                for (; index < children.Length; index++)
                {
                    Element child = children[index];
                    ref readonly ComputedStyle childStyle = ref child.Style;
                    if (!IsInFlow(in childStyle))
                    {
                        continue;
                    }

                    ref readonly LayoutNode childLayout = ref child.Layout;
                    if (childLayout.LineIndex != lineIndex)
                    {
                        break;
                    }

                    // Yoga: Node::isLayoutDimensionDefined.
                    float childCrossDim = LayoutMath.MeasuredDimension(in childLayout, crossAxis);
                    if (LayoutMath.IsDefined(childCrossDim) && childCrossDim >= 0f)
                    {
                        lineHeight = LayoutMath.MaxOrDefined(
                            lineHeight,
                            childCrossDim + LayoutMath.MarginForAxis(in childStyle, crossAxis, availableInnerWidth));
                    }
                }

                endIndex = index;
                lineHeight += extraSpacePerLine;

                for (index = startIndex; index < endIndex; index++)
                {
                    Element child = children[index];
                    ref readonly ComputedStyle childStyle = ref child.Style;
                    if (!IsInFlow(in childStyle))
                    {
                        continue;
                    }

                    ref LayoutNode childLayout = ref child.Layout;

                    // CSS difference from Yoga: Yoga drops the relative offset here, except a flex-start offset on a
                    // flex-start item. The engine adds the physical offset to each position (the decision "A relative
                    // offset on a reverse axis follows CSS").
                    float relativePosition = LayoutMath.RelativePosition(in childStyle, crossAxis, crossAxisInnerSize);

                    // Each case puts the margin box of the item in the line, as CSS does.
                    switch (LayoutMath.ResolveChildAlignment(in style, in childStyle))
                    {
                        case Align.FlexStart:
                            // CSS difference from Yoga: the margin box starts at the line start, so the engine adds
                            // the leading cross margin. Yoga adds no margin here.
                            PositionOnAxis(ref childLayout, crossAxis) = currentLead
                                + LayoutMath.LeadingMargin(in childStyle, crossAxis, availableInnerWidth)
                                + relativePosition;
                            break;
                        case Align.FlexEnd:
                            // The margin box ends at the line end. Yoga gives the same result.
                            PositionOnAxis(ref childLayout, crossAxis) = currentLead
                                + lineHeight
                                - LayoutMath.TrailingMargin(in childStyle, crossAxis, availableInnerWidth)
                                - LayoutMath.MeasuredDimension(in childLayout, crossAxis)
                                + relativePosition;
                            break;
                        case Align.Center:
                        {
                            // CSS difference from Yoga: the engine centers the margin box in the line, and Yoga
                            // centers the border box. The two results are the same for equal cross margins.
                            float outerCrossSize = LayoutMath.MeasuredDimension(in childLayout, crossAxis)
                                + LayoutMath.MarginForAxis(in childStyle, crossAxis, availableInnerWidth);
                            PositionOnAxis(ref childLayout, crossAxis) = currentLead
                                + ((lineHeight - outerCrossSize) / 2f)
                                + LayoutMath.LeadingMargin(in childStyle, crossAxis, availableInnerWidth)
                                + relativePosition;
                            break;
                        }

                        case Align.Stretch:
                        {
                            PositionOnAxis(ref childLayout, crossAxis) = currentLead
                                + LayoutMath.LeadingMargin(in childStyle, crossAxis, availableInnerWidth)
                                + relativePosition;

                            // Lay out the child again with the cross size of the line. Steps 5 and 7 gave it only
                            // the cross size of the container or of its content.
                            if (!LayoutMath.HasDefiniteLength(in childStyle, crossAxis, availableInnerCrossDim))
                            {
                                // Each size is an outer size: the child takes its own margins away from it. The cross
                                // size is the line size, so the margin box fills the line.
                                // CSS difference from Yoga: Yoga adds the cross margins (horizontal) to the measured
                                // height of a column item, so the item grows by its horizontal margins less its
                                // vertical margins. The engine adds the main-axis margins (vertical), so the item
                                // keeps its height. The row case adds the main-axis margins (horizontal) to the
                                // width in Yoga too.
                                // CSS difference from Yoga: Yoga gives the cross size leadPerLine + lineHeight. With
                                // AlignContent SpaceBetween, SpaceAround, or SpaceEvenly, leadPerLine is the space
                                // between two lines, so the item also fills the space after its line. CSS distributes
                                // that space between the lines and does not grow the lines. Thus the engine gives the
                                // line size only. For AlignContent Stretch, leadPerLine is 0, and lineHeight already
                                // holds the equal part of the free space (extraSpacePerLine), as CSS requires.
                                float childWidth = isMainAxisRow
                                    ? childLayout.MeasuredWidth
                                        + LayoutMath.MarginForAxis(in childStyle, mainAxis, availableInnerWidth)
                                    : lineHeight;
                                float childHeight = !isMainAxisRow
                                    ? childLayout.MeasuredHeight
                                        + LayoutMath.MarginForAxis(in childStyle, mainAxis, availableInnerWidth)
                                    : lineHeight;

                                if (!(LayoutMath.InexactEquals(childWidth, childLayout.MeasuredWidth)
                                    && LayoutMath.InexactEquals(childHeight, childLayout.MeasuredHeight)))
                                {
                                    LayoutInternal(
                                        child,
                                        childWidth,
                                        childHeight,
                                        MeasureMode.Exactly,
                                        MeasureMode.Exactly,
                                        availableInnerWidth,
                                        availableInnerHeight,
                                        performLayout: true,
                                        generation);
                                }
                            }

                            break;
                        }

                        default:
                            // ResolveChildAlignment gives no other value.
                            break;
                    }
                }

                currentLead += leadPerLine + lineHeight;
            }
        }

        // Step 9: compute the final dimensions.
        layout.MeasuredWidth = LayoutMath.BoundAxis(
            in style,
            FlexDirection.Row,
            availableWidth - marginAxisRow,
            ownerWidth,
            ownerWidth);
        layout.MeasuredHeight = LayoutMath.BoundAxis(
            in style,
            FlexDirection.Column,
            availableHeight - marginAxisColumn,
            ownerHeight,
            ownerWidth);

        // When the style gives no size, the children give the size. A scroll container with a maximum size
        // takes the available size and not the size of its content, but not less than the padding and the border.
        bool isScroll = style.Overflow == Overflow.Scroll;
        if (measureModeMainDim == MeasureMode.Undefined || (!isScroll && measureModeMainDim == MeasureMode.AtMost))
        {
            SetMeasuredDimension(
                ref layout,
                mainAxis,
                LayoutMath.BoundAxis(in style, mainAxis, maxLineMainDim, mainAxisOwnerSize, ownerWidth));
        }
        else if (measureModeMainDim == MeasureMode.AtMost && isScroll)
        {
            SetMeasuredDimension(
                ref layout,
                mainAxis,
                LayoutMath.MaxOrDefined(
                    LayoutMath.MinOrDefined(
                        availableInnerMainDim + paddingAndBorderAxisMain,
                        LayoutMath.BoundAxisWithinMinAndMax(in style, mainAxis, maxLineMainDim, mainAxisOwnerSize, ownerWidth)),
                    paddingAndBorderAxisMain));
        }

        if (measureModeCrossDim == MeasureMode.Undefined || (!isScroll && measureModeCrossDim == MeasureMode.AtMost))
        {
            SetMeasuredDimension(
                ref layout,
                crossAxis,
                LayoutMath.BoundAxis(
                    in style,
                    crossAxis,
                    totalLineCrossDim + paddingAndBorderAxisCross,
                    crossAxisOwnerSize,
                    ownerWidth));
        }
        else if (measureModeCrossDim == MeasureMode.AtMost && isScroll)
        {
            SetMeasuredDimension(
                ref layout,
                crossAxis,
                LayoutMath.MaxOrDefined(
                    LayoutMath.MinOrDefined(
                        availableInnerCrossDim + paddingAndBorderAxisCross,
                        LayoutMath.BoundAxisWithinMinAndMax(
                            in style,
                            crossAxis,
                            totalLineCrossDim + paddingAndBorderAxisCross,
                            crossAxisOwnerSize,
                            ownerWidth)),
                    paddingAndBorderAxisCross));
        }

        // Steps 7 and 8 put the lines from the flex-start edge of the cross axis. With wrap-reverse, mirror the cross
        // position of each child inside the content box of the element. Yoga skips an absolute child here, and the
        // engine also skips a hidden child.
        if (performLayout && style.FlexWrap == FlexWrap.WrapReverse)
        {
            float crossDim = LayoutMath.MeasuredDimension(in layout, crossAxis);
            float crossAxisInnerSize = isMainAxisRow ? availableInnerHeight : availableInnerWidth;
            float trailingPaddingAndBorderCross = LayoutMath.TrailingPaddingAndBorder(in style, crossAxis, ownerWidth);
            for (int i = 0; i < children.Length; i++)
            {
                Element child = children[i];
                ref readonly ComputedStyle childStyle = ref child.Style;
                if (!IsInFlow(in childStyle))
                {
                    continue;
                }

                ref LayoutNode childLayout = ref child.Layout;

                // Step 8 added the physical relative offset. Yoga mirrors the offset too, so a top offset moves the
                // child up. The engine mirrors the position without the offset and adds the offset after, so the
                // offset keeps its physical direction (the decision "A relative offset on a reverse axis follows CSS").
                float relativePosition = LayoutMath.RelativePosition(in childStyle, crossAxis, crossAxisInnerSize);

                // CSS difference from Yoga: Yoga mirrors the border box against the full cross size, so the gap at
                // the flex-end edge is the leading padding, border, and margin. CSS swaps the cross-start and the
                // cross-end edges. Thus the engine mirrors the margin box inside the content box: the first line
                // starts inside the trailing padding and border, and the trailing margin of the item comes first.
                float leadingMarginCross = LayoutMath.LeadingMargin(in childStyle, crossAxis, availableInnerWidth);
                float trailingMarginCross = LayoutMath.TrailingMargin(in childStyle, crossAxis, availableInnerWidth);
                ref float position = ref PositionOnAxis(ref childLayout, crossAxis);

                // The offset of the margin box from the start of the content box.
                float marginBoxOffset = position - relativePosition - leadingPaddingAndBorderCross - leadingMarginCross;
                position = crossDim
                    - trailingPaddingAndBorderCross
                    - marginBoxOffset
                    - trailingMarginCross
                    - LayoutMath.MeasuredDimension(in childLayout, crossAxis)
                    + relativePosition;
            }
        }

        if (performLayout)
        {
            // Step 10: set the trailing positions for the children.
            // On a reverse axis, X or Y holds the offset from the flex-start edge. Convert it into the position
            // from the left or the top edge.
            bool needsMainTrailingPos = LayoutMath.IsReverse(mainAxis);
            bool needsCrossTrailingPos = LayoutMath.IsReverse(crossAxis);

            if (needsMainTrailingPos || needsCrossTrailingPos)
            {
                for (int i = 0; i < children.Length; i++)
                {
                    // Step 11 positions an absolute child. A hidden child has no position.
                    Element child = children[i];
                    if (!IsInFlow(in child.Style))
                    {
                        continue;
                    }

                    ref LayoutNode childLayout = ref child.Layout;

                    if (needsMainTrailingPos)
                    {
                        SetChildTrailingPosition(in layout, ref childLayout, mainAxis);
                    }

                    if (needsCrossTrailingPos)
                    {
                        SetChildTrailingPosition(in layout, ref childLayout, crossAxis);
                    }
                }
            }

            // Step 11: size and position the absolute children.
            // The element is the containing block of its absolute children (A12). Yoga tests the position type of
            // the element here. The engine has no static position type, so each element lays out its absolute children.
            LayoutAbsoluteDescendants(
                node,
                children,
                isMainAxisRow ? measureModeMainDim : measureModeCrossDim,
                ownerWidth,
                generation);
        }
    }

    /// <summary>
    /// Gives the alignment of the lines when the lines need more space than the element has.
    /// Yoga: <c>fallbackAlignment(Align)</c> of <c>Align.h</c>.
    /// </summary>
    /// <param name="align">The <see cref="ComputedStyle.AlignContent"/> value.</param>
    /// <returns>
    /// <see cref="Align.FlexStart"/> for <see cref="Align.Stretch"/>, <see cref="Align.SpaceBetween"/>,
    /// <see cref="Align.SpaceAround"/>, and <see cref="Align.SpaceEvenly"/>. Otherwise <paramref name="align"/>.
    /// </returns>
    private static Align FallbackAlignment(Align align)
    {
        switch (align)
        {
            case Align.Stretch:
            case Align.SpaceBetween:
            case Align.SpaceAround:
            case Align.SpaceEvenly:
                return Align.FlexStart;
            default:
                return align;
        }
    }

    /// <summary>
    /// Tells if a child is an item of the flex flow of its parent. Yoga: the test that keeps a child out of
    /// <c>FlexLine::itemsInFlow</c>.
    /// </summary>
    /// <param name="style">The style of the child.</param>
    /// <returns><see langword="false"/> for a hidden child and for an absolute child.</returns>
    private static bool IsInFlow(in ComputedStyle style)
    {
        return style.Display != Display.None && style.Position != PositionType.Absolute;
    }

    /// <summary>
    /// Sets the layout of an element and of each element of its subtree to zeros. Yoga: <c>zeroOutLayoutRecursively</c>.
    /// </summary>
    /// <remarks>
    /// The method sets the rect, the measured size, and the flex basis to 0. It also resets the layout cache and the
    /// generation of each element of the subtree. The engine calls it on a hidden child that holds a layout bit, and
    /// on a hidden child with a generation that is not 0 (Decision 9). The pass never visits a hidden child, so a
    /// generation that is not 0 shows a stale visible layout. A hidden root that moves into a tree gives this case.
    /// A clean hidden child with generation 0 got its zero-out in an earlier pass, or no pass laid it out.
    /// The clear walk of <see cref="Calculate"/> clears the bits of the subtree.
    /// </remarks>
    /// <param name="node">The first element of the subtree.</param>
    private static void ZeroOutLayoutRecursively(Element node)
    {
        ref LayoutNode layout = ref node.Layout;
        layout.ComputedRect = default;
        layout.MeasuredWidth = 0f;
        layout.MeasuredHeight = 0f;
        layout.ComputedFlexBasis = 0f;

        // Yoga: `getLayout() = {}` also clears the cache and the generation. A clean descendant of a hidden element
        // must not keep its cache: its rect is now 0, so a later hit would give a size with no layout of its subtree.
        layout.Cache.Reset();
        layout.Generation = 0;

        ReadOnlySpan<Element> children = node.ChildStorage.AsSpan();
        for (int i = 0; i < children.Length; i++)
        {
            ZeroOutLayoutRecursively(children[i]);
        }
    }

    /// <summary>
    /// Sets the measured size of a leaf from its measure callback. Yoga: <c>measureNodeWithMeasureFunc</c>.
    /// </summary>
    /// <remarks>
    /// The method gives the callback the inner size and the two modes. With two exact sizes, it does not call the
    /// callback. A callback result that is <c>NaN</c> or negative counts as 0, as in Yoga (<c>Node::measure</c>).
    /// </remarks>
    /// <param name="node">The leaf element.</param>
    /// <param name="measure">The measure callback of the leaf.</param>
    /// <param name="availableWidth">The available width with no margins, in pixels. <c>NaN</c> means no limit.</param>
    /// <param name="availableHeight">The available height with no margins, in pixels. <c>NaN</c> means no limit.</param>
    /// <param name="widthMeasureMode">The rule for the available width.</param>
    /// <param name="heightMeasureMode">The rule for the available height.</param>
    /// <param name="ownerWidth">The inner width of the parent. <c>NaN</c> means undefined.</param>
    /// <param name="ownerHeight">The inner height of the parent. <c>NaN</c> means undefined.</param>
    private static void MeasureNodeWithMeasureFunc(
        Element node,
        MeasureCallback measure,
        float availableWidth,
        float availableHeight,
        MeasureMode widthMeasureMode,
        MeasureMode heightMeasureMode,
        float ownerWidth,
        float ownerHeight)
    {
        ref readonly ComputedStyle style = ref node.Style;
        ref LayoutNode layout = ref node.Layout;

        if (widthMeasureMode == MeasureMode.Undefined)
        {
            availableWidth = float.NaN;
        }

        if (heightMeasureMode == MeasureMode.Undefined)
        {
            availableHeight = float.NaN;
        }

        float paddingAndBorderAxisRow = LayoutMath.PaddingAndBorderForAxis(in style, FlexDirection.Row, ownerWidth);
        float paddingAndBorderAxisColumn = LayoutMath.PaddingAndBorderForAxis(in style, FlexDirection.Column, ownerWidth);

        // The callback never gets a negative size.
        float innerWidth = LayoutMath.IsUndefined(availableWidth)
            ? availableWidth
            : LayoutMath.MaxOrDefined(0f, availableWidth - paddingAndBorderAxisRow);
        float innerHeight = LayoutMath.IsUndefined(availableHeight)
            ? availableHeight
            : LayoutMath.MaxOrDefined(0f, availableHeight - paddingAndBorderAxisColumn);

        if (widthMeasureMode == MeasureMode.Exactly && heightMeasureMode == MeasureMode.Exactly)
        {
            // Both sizes are known, so the callback has nothing to add.
            layout.MeasuredWidth = LayoutMath.BoundAxis(in style, FlexDirection.Row, availableWidth, ownerWidth, ownerWidth);
            layout.MeasuredHeight = LayoutMath.BoundAxis(in style, FlexDirection.Column, availableHeight, ownerHeight, ownerWidth);
            return;
        }

        float2 measuredSize = measure(node, innerWidth, widthMeasureMode, innerHeight, heightMeasureMode);

        // An invalid size counts as 0 (Yoga: Node::measure).
        float measuredWidth = LayoutMath.MaxOrDefined(0f, measuredSize.X);
        float measuredHeight = LayoutMath.MaxOrDefined(0f, measuredSize.Y);

        layout.MeasuredWidth = LayoutMath.BoundAxis(
            in style,
            FlexDirection.Row,
            widthMeasureMode == MeasureMode.Exactly ? availableWidth : measuredWidth + paddingAndBorderAxisRow,
            ownerWidth,
            ownerWidth);
        layout.MeasuredHeight = LayoutMath.BoundAxis(
            in style,
            FlexDirection.Column,
            heightMeasureMode == MeasureMode.Exactly ? availableHeight : measuredHeight + paddingAndBorderAxisColumn,
            ownerHeight,
            ownerWidth);
    }

    /// <summary>
    /// Sets the measured size of a leaf with no measure callback. Yoga: <c>measureNodeWithoutChildren</c>.
    /// </summary>
    /// <param name="node">The leaf element.</param>
    /// <param name="availableWidth">The available width with no margins, in pixels. <c>NaN</c> means no limit.</param>
    /// <param name="availableHeight">The available height with no margins, in pixels. <c>NaN</c> means no limit.</param>
    /// <param name="widthMeasureMode">The rule for the available width.</param>
    /// <param name="heightMeasureMode">The rule for the available height.</param>
    /// <param name="ownerWidth">The inner width of the parent. <c>NaN</c> means undefined.</param>
    /// <param name="ownerHeight">The inner height of the parent. <c>NaN</c> means undefined.</param>
    private static void MeasureNodeWithoutChildren(
        Element node,
        float availableWidth,
        float availableHeight,
        MeasureMode widthMeasureMode,
        MeasureMode heightMeasureMode,
        float ownerWidth,
        float ownerHeight)
    {
        ref readonly ComputedStyle style = ref node.Style;
        ref LayoutNode layout = ref node.Layout;

        // A leaf with no exact size has no content. The padding and the border give its size.
        float width = availableWidth;
        if (widthMeasureMode == MeasureMode.Undefined || widthMeasureMode == MeasureMode.AtMost)
        {
            width = LayoutMath.PaddingAndBorderForAxis(in style, FlexDirection.Row, ownerWidth);
        }

        layout.MeasuredWidth = LayoutMath.BoundAxis(in style, FlexDirection.Row, width, ownerWidth, ownerWidth);

        float height = availableHeight;
        if (heightMeasureMode == MeasureMode.Undefined || heightMeasureMode == MeasureMode.AtMost)
        {
            height = LayoutMath.PaddingAndBorderForAxis(in style, FlexDirection.Column, ownerWidth);
        }

        layout.MeasuredHeight = LayoutMath.BoundAxis(in style, FlexDirection.Column, height, ownerHeight, ownerWidth);
    }

    /// <summary>
    /// Sets the measured size when the constraints give it with no layout of the children.
    /// Yoga: <c>measureNodeWithFixedSize</c>.
    /// </summary>
    /// <param name="node">The element.</param>
    /// <param name="availableWidth">The available width with no margins, in pixels. <c>NaN</c> means no limit.</param>
    /// <param name="availableHeight">The available height with no margins, in pixels. <c>NaN</c> means no limit.</param>
    /// <param name="widthMeasureMode">The rule for the available width.</param>
    /// <param name="heightMeasureMode">The rule for the available height.</param>
    /// <param name="ownerWidth">The inner width of the parent. <c>NaN</c> means undefined.</param>
    /// <param name="ownerHeight">The inner height of the parent. <c>NaN</c> means undefined.</param>
    /// <returns><see langword="true"/> when the method set the size.</returns>
    private static bool MeasureNodeWithFixedSize(
        Element node,
        float availableWidth,
        float availableHeight,
        MeasureMode widthMeasureMode,
        MeasureMode heightMeasureMode,
        float ownerWidth,
        float ownerHeight)
    {
        if ((LayoutMath.IsDefined(availableWidth) && widthMeasureMode == MeasureMode.AtMost && availableWidth <= 0f)
            || (LayoutMath.IsDefined(availableHeight) && heightMeasureMode == MeasureMode.AtMost && availableHeight <= 0f)
            || (widthMeasureMode == MeasureMode.Exactly && heightMeasureMode == MeasureMode.Exactly))
        {
            ref readonly ComputedStyle style = ref node.Style;
            ref LayoutNode layout = ref node.Layout;

            layout.MeasuredWidth = LayoutMath.BoundAxis(
                in style,
                FlexDirection.Row,
                LayoutMath.IsUndefined(availableWidth) || (widthMeasureMode == MeasureMode.AtMost && availableWidth < 0f)
                    ? 0f
                    : availableWidth,
                ownerWidth,
                ownerWidth);

            layout.MeasuredHeight = LayoutMath.BoundAxis(
                in style,
                FlexDirection.Column,
                LayoutMath.IsUndefined(availableHeight) || (heightMeasureMode == MeasureMode.AtMost && availableHeight < 0f)
                    ? 0f
                    : availableHeight,
                ownerHeight,
                ownerWidth);
            return true;
        }

        return false;
    }

    /// <summary>
    /// Gives the inner size on an axis: the available size minus the padding and the border, clamped to the
    /// inner minimum and maximum. Yoga: <c>calculateAvailableInnerDimension</c>.
    /// </summary>
    /// <param name="style">The style of the element.</param>
    /// <param name="axis">The axis: <see cref="FlexDirection.Row"/> for the width, <see cref="FlexDirection.Column"/> for the height.</param>
    /// <param name="availableDim">The available size with no margins, in pixels. <c>NaN</c> means no limit.</param>
    /// <param name="paddingAndBorder">The padding and border sum on the axis, in pixels.</param>
    /// <param name="ownerDim">The size of the owner on the axis. <c>NaN</c> means undefined.</param>
    /// <param name="ownerWidth">The width of the owner. <c>NaN</c> means undefined.</param>
    /// <returns>The inner size in pixels, or <c>NaN</c>.</returns>
    private static float CalculateAvailableInnerDimension(
        in ComputedStyle style,
        FlexDirection axis,
        float availableDim,
        float paddingAndBorder,
        float ownerDim,
        float ownerWidth)
    {
        float availableInnerDim = availableDim - paddingAndBorder;

        // The maximum overrides the available size. The minimum overrides both.
        if (LayoutMath.IsDefined(availableInnerDim))
        {
            // The resolved minimum and maximum of the style, minus the padding and the border.
            // An undefined minimum gives 0, and an undefined maximum gives float.MaxValue, as in Yoga.
            float minDimension = LayoutMath.ResolvedMinDimension(in style, axis, ownerDim);
            float minInnerDim = LayoutMath.IsUndefined(minDimension) ? 0f : minDimension - paddingAndBorder;

            float maxDimension = LayoutMath.ResolvedMaxDimension(in style, axis, ownerDim);
            float maxInnerDim = LayoutMath.IsUndefined(maxDimension) ? float.MaxValue : maxDimension - paddingAndBorder;

            availableInnerDim = LayoutMath.MaxOrDefined(LayoutMath.MinOrDefined(availableInnerDim, maxInnerDim), minInnerDim);
        }

        return availableInnerDim;
    }

    /// <summary>
    /// Limits a size and its rule to the maximum of the style on an axis. Yoga: <c>constrainMaxSizeForMode</c>.
    /// </summary>
    /// <remarks>
    /// The maximum here includes the margins, because the size includes them. With <see cref="MeasureMode.Exactly"/>
    /// or <see cref="MeasureMode.AtMost"/>, a size larger than the maximum gets the maximum. With
    /// <see cref="MeasureMode.Undefined"/>, a defined maximum gives the size and the rule <see cref="MeasureMode.AtMost"/>.
    /// </remarks>
    /// <param name="style">The style of the element.</param>
    /// <param name="axis">The axis.</param>
    /// <param name="ownerAxisSize">The size of the owner on the axis, for a percent maximum. <c>NaN</c> means undefined.</param>
    /// <param name="ownerWidth">The width of the owner, for a percent margin. <c>NaN</c> means undefined.</param>
    /// <param name="mode">The rule for the size. The method can change it.</param>
    /// <param name="size">The size with the margins, in pixels. The method can change it.</param>
    private static void ConstrainMaxSizeForMode(
        in ComputedStyle style,
        FlexDirection axis,
        float ownerAxisSize,
        float ownerWidth,
        ref MeasureMode mode,
        ref float size)
    {
        // An undefined maximum stays undefined after the add, as the FloatOptional add of Yoga.
        float maxSize = LayoutMath.ResolvedMaxDimension(in style, axis, ownerAxisSize)
            + LayoutMath.MarginForAxis(in style, axis, ownerWidth);

        switch (mode)
        {
            case MeasureMode.Exactly:
            case MeasureMode.AtMost:
                size = LayoutMath.IsUndefined(maxSize) || size < maxSize ? size : maxSize;
                break;
            default:
                // MeasureMode.Undefined: Yoga MaxContent.
                if (LayoutMath.IsDefined(maxSize))
                {
                    mode = MeasureMode.AtMost;
                    size = maxSize;
                }

                break;
        }
    }

    /// <summary>
    /// Converts the offset of a child from the flex-start edge into the position from the left or the top edge.
    /// Yoga: <c>setChildTrailingPosition</c>.
    /// </summary>
    /// <param name="nodeLayout">The layout data of the container.</param>
    /// <param name="childLayout">The layout data of the child.</param>
    /// <param name="axis">The reverse axis.</param>
    private static void SetChildTrailingPosition(in LayoutNode nodeLayout, ref LayoutNode childLayout, FlexDirection axis)
    {
        ref float position = ref PositionOnAxis(ref childLayout, axis);
        position = GetPositionOfOppositeEdge(position, axis, in nodeLayout, in childLayout);
    }

    /// <summary>
    /// Converts the offset of an element from one edge of its container into the offset from the opposite edge on
    /// the same axis. Yoga: <c>getPositionOfOppositeEdge</c>.
    /// </summary>
    /// <remarks>The measured sizes of the container and of the element must be final.</remarks>
    /// <param name="position">The offset from one edge, in pixels.</param>
    /// <param name="axis">The axis.</param>
    /// <param name="containingLayout">The layout data of the container.</param>
    /// <param name="layout">The layout data of the element.</param>
    /// <returns>The offset from the opposite edge, in pixels.</returns>
    private static float GetPositionOfOppositeEdge(
        float position,
        FlexDirection axis,
        in LayoutNode containingLayout,
        in LayoutNode layout)
    {
        return LayoutMath.MeasuredDimension(in containingLayout, axis)
            - LayoutMath.MeasuredDimension(in layout, axis)
            - position;
    }

    /// <summary>
    /// Gives a reference to the position on an axis: X for a row axis, and Y for a column axis.
    /// </summary>
    /// <param name="layout">The layout data.</param>
    /// <param name="axis">The axis.</param>
    /// <returns>A reference to X or Y of <see cref="LayoutNode.ComputedRect"/>.</returns>
    private static ref float PositionOnAxis(ref LayoutNode layout, FlexDirection axis)
    {
        if (LayoutMath.IsRow(axis))
        {
            return ref layout.ComputedRect.X;
        }

        return ref layout.ComputedRect.Y;
    }

    /// <summary>
    /// Sets the measured size on an axis: the width for a row axis, and the height for a column axis.
    /// </summary>
    /// <param name="layout">The layout data.</param>
    /// <param name="axis">The axis.</param>
    /// <param name="value">The size in pixels.</param>
    private static void SetMeasuredDimension(ref LayoutNode layout, FlexDirection axis, float value)
    {
        if (LayoutMath.IsRow(axis))
        {
            layout.MeasuredWidth = value;
        }
        else
        {
            layout.MeasuredHeight = value;
        }
    }
}
