// Ported from Yoga (https://github.com/facebook/yoga), v3.2.1, yoga/algorithm/CalculateLayout.cpp, yoga/algorithm/FlexLine.cpp, yoga/algorithm/FlexLine.h, yoga/node/Node.cpp. Copyright (c) Meta Platforms, Inc. and affiliates.
// MIT License. See THIRD-PARTY-NOTICES.md at the repository root.

using System;

namespace BrutalUi.Core;

// Steps 3 to 6 of the algorithm: the flex basis, the flex lines, the distribution of the free space, and the
// justification on the main axis. A flex line is an index range over the child span. This file holds no list.
internal static partial class LayoutEngine
{
    /// <summary>
    /// The largest ratio of a rounding residual to the original total grow factor or total scaled shrink factor of a
    /// line. After the first pass of the flex step, a smaller total becomes 0. The value covers the float rounding
    /// of about 80 subtractions.
    /// </summary>
    private const float GrowFactorResidualRatio = 1e-5f;

    /// <summary>
    /// Gives the flex grow factor of an element. Yoga: <c>Node::resolveFlexGrow</c> with no <c>flex</c> shorthand.
    /// </summary>
    /// <param name="node">The element.</param>
    /// <returns>
    /// 0 for a root element and for a factor that is negative, infinite, or <c>NaN</c>. Otherwise <see cref="ComputedStyle.FlexGrow"/>.
    /// </returns>
    private static float ResolveFlexGrow(Element node)
    {
        // A root element does not grow.
        if (node.Parent is null)
        {
            return 0f;
        }

        // Difference from Yoga: a negative or infinite factor counts as 0, as in CSS. Yoga gives a wrong rect.
        float flexGrow = node.Style.FlexGrow;
        return flexGrow >= 0f && float.IsFinite(flexGrow) ? flexGrow : 0f;
    }

    /// <summary>
    /// Gives the flex shrink factor of an element. Yoga: <c>Node::resolveFlexShrink</c> with no <c>flex</c> shorthand
    /// and no web defaults.
    /// </summary>
    /// <param name="node">The element.</param>
    /// <returns>
    /// 0 for a root element and for a factor that is negative, infinite, or <c>NaN</c>. Otherwise <see cref="ComputedStyle.FlexShrink"/>.
    /// </returns>
    private static float ResolveFlexShrink(Element node)
    {
        // A root element does not shrink.
        if (node.Parent is null)
        {
            return 0f;
        }

        // Difference from Yoga: a negative or infinite factor counts as 0, as in CSS. Yoga gives a wrong rect.
        float flexShrink = node.Style.FlexShrink;
        return flexShrink >= 0f && float.IsFinite(flexShrink) ? flexShrink : 0f;
    }

    /// <summary>
    /// Tells if an element can grow or shrink. Yoga: <c>Node::isNodeFlexible</c>.
    /// </summary>
    /// <param name="node">The element.</param>
    /// <remarks>
    /// An absolute element is not flexible. A hidden element can be flexible, as in Yoga. Only the test for one
    /// flexible child in <see cref="ComputeFlexBasisForChildren"/> sees a hidden element.
    /// </remarks>
    /// <returns><see langword="true"/> when the element is not absolute, and the grow factor or the shrink factor is not 0.</returns>
    private static bool IsNodeFlexible(Element node)
    {
        return node.Style.Position != PositionType.Absolute
            && (ResolveFlexGrow(node) != 0f || ResolveFlexShrink(node) != 0f);
    }

    /// <summary>
    /// Resolves the flex basis of the style to pixels. Yoga: <c>Node::resolveFlexBasis</c> with a border box and
    /// <c>Node::processFlexBasis</c> with no <c>flex</c> shorthand.
    /// </summary>
    /// <remarks>
    /// A percent basis resolves against the main size of the owner. When that size is undefined, the result is
    /// <c>NaN</c>, so the percent basis acts as auto.
    /// </remarks>
    /// <param name="style">The style of the element.</param>
    /// <param name="referenceLength">The main size of the owner, for a percent. <c>NaN</c> means undefined.</param>
    /// <returns>The basis in pixels, or <c>NaN</c> for an auto basis and for a percent of an undefined size.</returns>
    private static float ResolveFlexBasis(in ComputedStyle style, float referenceLength)
    {
        // Undefined and Auto both resolve to NaN, so both mean auto.
        return style.FlexBasis.Resolve(referenceLength);
    }

    /// <summary>
    /// Computes the flex basis of each child and gives the sum of the outer flex bases. Yoga: <c>computeFlexBasisForChildren</c>.
    /// </summary>
    /// <param name="node">The container.</param>
    /// <param name="children">The children of the container.</param>
    /// <param name="availableInnerWidth">The inner width of the container. <c>NaN</c> means undefined.</param>
    /// <param name="availableInnerHeight">The inner height of the container. <c>NaN</c> means undefined.</param>
    /// <param name="widthMeasureMode">The rule for the width of the container.</param>
    /// <param name="heightMeasureMode">The rule for the height of the container.</param>
    /// <param name="mainAxis">The main axis of the container.</param>
    /// <param name="performLayout"><see langword="true"/> for a full layout of the container.</param>
    /// <param name="generation">The generation number of the pass.</param>
    /// <returns>The sum of the flex bases and the main margins, in pixels.</returns>
    private static float ComputeFlexBasisForChildren(
        Element node,
        ReadOnlySpan<Element> children,
        float availableInnerWidth,
        float availableInnerHeight,
        MeasureMode widthMeasureMode,
        MeasureMode heightMeasureMode,
        FlexDirection mainAxis,
        bool performLayout,
        long generation)
    {
        float totalOuterFlexBasis = 0f;

        // The index of the only flexible child, or -1. Yoga: singleFlexChild.
        int singleFlexChildIndex = -1;
        MeasureMode measureModeMainDim = LayoutMath.IsRow(mainAxis) ? widthMeasureMode : heightMeasureMode;

        // When only one child can grow and shrink, its flex basis is 0. The flex step then gives it the exact
        // free space, and the child needs no measurement.
        if (measureModeMainDim == MeasureMode.Exactly)
        {
            for (int i = 0; i < children.Length; i++)
            {
                Element child = children[i];
                if (IsNodeFlexible(child))
                {
                    if (singleFlexChildIndex >= 0
                        || LayoutMath.InexactEquals(ResolveFlexGrow(child), 0f)
                        || LayoutMath.InexactEquals(ResolveFlexShrink(child), 0f))
                    {
                        // A second flexible child, or a flexible child that cannot both grow and shrink. Stop.
                        singleFlexChildIndex = -1;
                        break;
                    }

                    singleFlexChildIndex = i;
                }
            }
        }

        for (int i = 0; i < children.Length; i++)
        {
            Element child = children[i];
            ref readonly ComputedStyle childStyle = ref child.Style;

            // A hidden child and its subtree get zeros and no flex basis (Decision 9). A child with a layout bit gets
            // the zero-out. A clean child with a generation that is not 0 also gets the zero-out: the pass never
            // visits a hidden child, and the zero-out sets the generation to 0, so the child holds a stale visible
            // layout. A hidden root that moves into a tree gives this case, because a root ignores Display.None.
            // A clean child with generation 0 holds zeros, or no pass laid it out, so it gets no zero-out.
            if (childStyle.Display == Display.None)
            {
                bool isChildDirty = (child.Dirty & (DirtyFlags.Layout | DirtyFlags.DescendantLayout)) != DirtyFlags.None;
                if (isChildDirty || child.Layout.Generation != 0)
                {
                    ZeroOutLayoutRecursively(child);
                }

                continue;
            }

            ref LayoutNode childLayout = ref child.Layout;
            if (performLayout)
            {
                // Set the initial position relative to the parent. Yoga: setPosition.
                // X or Y holds the margin on the flex-start edge of the axis plus the relative offset on the axis.
                // Steps 6 and 7 add to this value.
                FlexDirection crossAxis = LayoutMath.CrossAxis(mainAxis);
                bool isMainAxisRow = LayoutMath.IsRow(mainAxis);

                // The relative offset is physical: a positive value moves the child to the right or down, as in
                // CSS. On a reverse main axis, X or Y holds the distance from the right or the bottom edge, and
                // step 10 mirrors it. Thus the engine subtracts the offset there. Yoga adds it on each axis, so a
                // left offset in a row-reverse parent moves the child to the left in Yoga. The engine follows CSS.
                // The cross axis is never reverse here, so the cross offset is always added.
                float mainRelativePosition = LayoutMath.RelativePosition(
                    in childStyle,
                    mainAxis,
                    isMainAxisRow ? availableInnerWidth : availableInnerHeight);
                PositionOnAxis(ref childLayout, mainAxis) =
                    LayoutMath.LeadingMargin(in childStyle, mainAxis, availableInnerWidth)
                    + (LayoutMath.IsReverse(mainAxis) ? -mainRelativePosition : mainRelativePosition);
                PositionOnAxis(ref childLayout, crossAxis) =
                    LayoutMath.LeadingMargin(in childStyle, crossAxis, availableInnerWidth)
                    + LayoutMath.RelativePosition(
                        in childStyle,
                        crossAxis,
                        isMainAxisRow ? availableInnerHeight : availableInnerWidth);
            }

            // Step 11 of the parent sizes and positions an absolute child.
            if (childStyle.Position == PositionType.Absolute)
            {
                continue;
            }

            if (i == singleFlexChildIndex)
            {
                childLayout.ComputedFlexBasis = 0f;
            }
            else
            {
                ComputeFlexBasisForChild(
                    node,
                    child,
                    availableInnerWidth,
                    widthMeasureMode,
                    availableInnerHeight,
                    availableInnerWidth,
                    availableInnerHeight,
                    heightMeasureMode,
                    generation);
            }

            totalOuterFlexBasis += childLayout.ComputedFlexBasis
                + LayoutMath.MarginForAxis(in child.Style, mainAxis, availableInnerWidth);
        }

        return totalOuterFlexBasis;
    }

    /// <summary>
    /// Computes the flex basis of one child and stores it in <see cref="LayoutNode.ComputedFlexBasis"/>.
    /// Yoga: <c>computeFlexBasisForChild</c>.
    /// </summary>
    /// <remarks>
    /// A defined <see cref="ComputedStyle.FlexBasis"/> gives the basis when the main size of the container is defined.
    /// Else a definite main size gives it. Else a measurement of the child gives it. The engine computes the basis
    /// again on each call. It does not reuse a basis, as Yoga does with <c>computedFlexBasisGeneration</c>.
    /// </remarks>
    /// <param name="node">The container.</param>
    /// <param name="child">The child.</param>
    /// <param name="width">The inner width of the container. <c>NaN</c> means undefined.</param>
    /// <param name="widthMode">The rule for the width of the container.</param>
    /// <param name="height">The inner height of the container. <c>NaN</c> means undefined.</param>
    /// <param name="ownerWidth">The inner width of the container, for a percent. <c>NaN</c> means undefined.</param>
    /// <param name="ownerHeight">The inner height of the container, for a percent. <c>NaN</c> means undefined.</param>
    /// <param name="heightMode">The rule for the height of the container.</param>
    /// <param name="generation">The generation number of the pass.</param>
    private static void ComputeFlexBasisForChild(
        Element node,
        Element child,
        float width,
        MeasureMode widthMode,
        float height,
        float ownerWidth,
        float ownerHeight,
        MeasureMode heightMode,
        long generation)
    {
        ref readonly ComputedStyle style = ref node.Style;
        ref readonly ComputedStyle childStyle = ref child.Style;
        ref LayoutNode childLayout = ref child.Layout;

        FlexDirection mainAxis = style.FlexDirection;
        bool isMainAxisRow = LayoutMath.IsRow(mainAxis);
        float mainAxisSize = isMainAxisRow ? width : height;
        float mainAxisOwnerSize = isMainAxisRow ? ownerWidth : ownerHeight;

        float resolvedFlexBasis = ResolveFlexBasis(in childStyle, mainAxisOwnerSize);
        bool isRowStyleDimDefined = LayoutMath.HasDefiniteLength(in childStyle, FlexDirection.Row, ownerWidth);
        bool isColumnStyleDimDefined = LayoutMath.HasDefiniteLength(in childStyle, FlexDirection.Column, ownerHeight);

        if (LayoutMath.IsDefined(resolvedFlexBasis) && LayoutMath.IsDefined(mainAxisSize))
        {
            // The flex basis of the style gives the basis, with the padding and border sum as the floor.
            // Yoga sets it only when the stored basis is undefined. The engine always sets it again (Decision 1).
            float paddingAndBorder = LayoutMath.PaddingAndBorderForAxis(in childStyle, mainAxis, ownerWidth);
            childLayout.ComputedFlexBasis = LayoutMath.MaxOrDefined(resolvedFlexBasis, paddingAndBorder);
        }
        else if (isMainAxisRow && isRowStyleDimDefined)
        {
            // The width is definite, so it gives the flex basis.
            float paddingAndBorder = LayoutMath.PaddingAndBorderForAxis(in childStyle, FlexDirection.Row, ownerWidth);
            childLayout.ComputedFlexBasis = LayoutMath.MaxOrDefined(
                LayoutMath.ResolvedDimension(in childStyle, FlexDirection.Row, ownerWidth),
                paddingAndBorder);
        }
        else if (!isMainAxisRow && isColumnStyleDimDefined)
        {
            // The height is definite, so it gives the flex basis.
            float paddingAndBorder = LayoutMath.PaddingAndBorderForAxis(in childStyle, FlexDirection.Column, ownerWidth);
            childLayout.ComputedFlexBasis = LayoutMath.MaxOrDefined(
                LayoutMath.ResolvedDimension(in childStyle, FlexDirection.Column, ownerHeight),
                paddingAndBorder);
        }
        else
        {
            // Compute the flex basis and the hypothetical main size: measure the child.
            float childWidth = float.NaN;
            float childHeight = float.NaN;
            MeasureMode childWidthMeasureMode = MeasureMode.Undefined;
            MeasureMode childHeightMeasureMode = MeasureMode.Undefined;

            float marginRow = LayoutMath.MarginForAxis(in childStyle, FlexDirection.Row, ownerWidth);
            float marginColumn = LayoutMath.MarginForAxis(in childStyle, FlexDirection.Column, ownerWidth);

            if (isRowStyleDimDefined)
            {
                childWidth = LayoutMath.ResolvedDimension(in childStyle, FlexDirection.Row, ownerWidth) + marginRow;
                childWidthMeasureMode = MeasureMode.Exactly;
            }

            if (isColumnStyleDimDefined)
            {
                childHeight = LayoutMath.ResolvedDimension(in childStyle, FlexDirection.Column, ownerHeight) + marginColumn;
                childHeightMeasureMode = MeasureMode.Exactly;
            }

            // The child gets the inner size of the container as a maximum. A scroll container gives no maximum on
            // its main axis: the content can be longer than the container. Browsers do the same (the rule of Yoga).
            bool isScroll = style.Overflow == Overflow.Scroll;
            if ((!isMainAxisRow || !isScroll) && LayoutMath.IsUndefined(childWidth) && LayoutMath.IsDefined(width))
            {
                childWidth = width;
                childWidthMeasureMode = MeasureMode.AtMost;
            }

            if ((isMainAxisRow || !isScroll) && LayoutMath.IsUndefined(childHeight) && LayoutMath.IsDefined(height))
            {
                childHeight = height;
                childHeightMeasureMode = MeasureMode.AtMost;
            }

            // A child with no cross size and the stretch alignment gets the exact inner cross size of the container.
            bool isStretch = LayoutMath.ResolveChildAlignment(in style, in childStyle) == Align.Stretch;
            bool hasExactWidth = LayoutMath.IsDefined(width) && widthMode == MeasureMode.Exactly;
            bool childWidthStretch = isStretch && childWidthMeasureMode != MeasureMode.Exactly;
            if (!isMainAxisRow && !isRowStyleDimDefined && hasExactWidth && childWidthStretch)
            {
                childWidth = width;
                childWidthMeasureMode = MeasureMode.Exactly;
            }

            bool hasExactHeight = LayoutMath.IsDefined(height) && heightMode == MeasureMode.Exactly;
            bool childHeightStretch = isStretch && childHeightMeasureMode != MeasureMode.Exactly;
            if (isMainAxisRow && !isColumnStyleDimDefined && hasExactHeight && childHeightStretch)
            {
                childHeight = height;
                childHeightMeasureMode = MeasureMode.Exactly;
            }

            // The maximum of the child limits the two sizes of the measurement.
            ConstrainMaxSizeForMode(
                in childStyle,
                FlexDirection.Row,
                ownerWidth,
                ownerWidth,
                ref childWidthMeasureMode,
                ref childWidth);
            ConstrainMaxSizeForMode(
                in childStyle,
                FlexDirection.Column,
                ownerHeight,
                ownerWidth,
                ref childHeightMeasureMode,
                ref childHeight);

            // Measure the child.
            LayoutInternal(
                child,
                childWidth,
                childHeight,
                childWidthMeasureMode,
                childHeightMeasureMode,
                ownerWidth,
                ownerHeight,
                performLayout: false,
                generation);

            childLayout.ComputedFlexBasis = LayoutMath.MaxOrDefined(
                LayoutMath.MeasuredDimension(in childLayout, mainAxis),
                LayoutMath.PaddingAndBorderForAxis(in childStyle, mainAxis, ownerWidth));
        }
    }

    /// <summary>
    /// Collects the children of one flex line, from <paramref name="startIndex"/> to the end of the line.
    /// Yoga: <c>calculateFlexLine</c>.
    /// </summary>
    /// <remarks>
    /// In a wrapped container, an item ends the line when the line size with the item goes past
    /// <paramref name="availableInnerMainDim"/>. The line size uses the flex basis of each item with its minimum and
    /// maximum, plus its main margins. A line always holds one item or more. The method writes
    /// <paramref name="lineCount"/> into <see cref="LayoutNode.LineIndex"/> of each item that it reads.
    /// </remarks>
    /// <param name="node">The container.</param>
    /// <param name="children">The children of the container.</param>
    /// <param name="ownerWidth">The width of the owner of the container. <c>NaN</c> means undefined.</param>
    /// <param name="mainAxisOwnerSize">The size of the owner of the container on the main axis. <c>NaN</c> means undefined.</param>
    /// <param name="availableInnerWidth">The inner width of the container. <c>NaN</c> means undefined.</param>
    /// <param name="availableInnerMainDim">The inner main size of the container. <c>NaN</c> means undefined.</param>
    /// <param name="startIndex">The index of the first child of the line.</param>
    /// <param name="lineCount">The index of the line.</param>
    /// <returns>
    /// The line. <see cref="FlexLine.EndIndex"/> is the end index of the line (exclusive). The next line starts at
    /// that index.
    /// </returns>
    private static FlexLine CalculateFlexLine(
        Element node,
        ReadOnlySpan<Element> children,
        float ownerWidth,
        float mainAxisOwnerSize,
        float availableInnerWidth,
        float availableInnerMainDim,
        int startIndex,
        int lineCount)
    {
        float sizeConsumed = 0f;
        float totalFlexGrowFactors = 0f;
        float totalFlexShrinkScaledFactors = 0f;
        int itemsInFlowCount = 0;
        int lastItemIndex = -1;
        int numberOfAutoMargins = 0;
        float sizeConsumedIncludingMinConstraint = 0f;

        FlexDirection mainAxis = node.Style.FlexDirection;
        bool isNodeFlexWrap = node.Style.FlexWrap != FlexWrap.NoWrap;

        // Add the items to the line until it is full or no item stays.
        int i = startIndex;
        for (; i < children.Length; i++)
        {
            Element child = children[i];
            ref readonly ComputedStyle childStyle = ref child.Style;

            // A hidden child and an absolute child are not items of the line.
            if (!IsInFlow(in childStyle))
            {
                continue;
            }

            // Count the auto margins on the main axis. JustifyMainAxis gives each one an equal part of the free space.
            if (LayoutMath.FlexStartMarginIsAuto(in childStyle, mainAxis))
            {
                numberOfAutoMargins++;
            }

            if (LayoutMath.FlexEndMarginIsAuto(in childStyle, mainAxis))
            {
                numberOfAutoMargins++;
            }

            // Yoga writes the line index before the break test. An item that ends the line gets the index of the
            // next line in the next call.
            ref LayoutNode childLayout = ref child.Layout;
            childLayout.LineIndex = lineCount;

            float childMarginMainAxis = LayoutMath.MarginForAxis(in childStyle, mainAxis, availableInnerWidth);
            float flexBasisWithMinAndMaxConstraints = LayoutMath.BoundAxisWithinMinAndMax(
                in childStyle,
                mainAxis,
                childLayout.ComputedFlexBasis,
                mainAxisOwnerSize,
                ownerWidth);

            // In a multi-line container, an item that goes past the available size ends the line. The first item
            // always stays on the line. An undefined available size never ends a line, because a compare with NaN
            // is false.
            if (sizeConsumedIncludingMinConstraint + flexBasisWithMinAndMaxConstraints + childMarginMainAxis > availableInnerMainDim
                && isNodeFlexWrap
                && itemsInFlowCount > 0)
            {
                break;
            }

            sizeConsumedIncludingMinConstraint += flexBasisWithMinAndMaxConstraints + childMarginMainAxis;
            sizeConsumed += flexBasisWithMinAndMaxConstraints + childMarginMainAxis;

            if (IsNodeFlexible(child))
            {
                totalFlexGrowFactors += ResolveFlexGrow(child);

                // The shrink factor scales with the flex basis of the item. The grow factor does not.
                totalFlexShrinkScaledFactors += -ResolveFlexShrink(child) * childLayout.ComputedFlexBasis;
            }

            lastItemIndex = i;
            itemsInFlowCount++;
        }

        // The total flex factors have a floor of 1.
        if (totalFlexGrowFactors > 0f && totalFlexGrowFactors < 1f)
        {
            totalFlexGrowFactors = 1f;
        }

        if (totalFlexShrinkScaledFactors > 0f && totalFlexShrinkScaledFactors < 1f)
        {
            totalFlexShrinkScaledFactors = 1f;
        }

        FlexLine line = new()
        {
            StartIndex = startIndex,
            EndIndex = i,
            LastItemIndex = lastItemIndex,
            ItemsInFlowCount = itemsInFlowCount,
            NumberOfAutoMargins = numberOfAutoMargins,
            SizeConsumed = sizeConsumed,
            TotalFlexGrowFactors = totalFlexGrowFactors,
            TotalFlexShrinkScaledFactors = totalFlexShrinkScaledFactors,
        };

        return line;
    }

    /// <summary>
    /// Gives each item of a line its main size and lays out the item. Yoga: <c>distributeFreeSpaceSecondPass</c>.
    /// </summary>
    /// <remarks>
    /// The free space goes to the items that the first pass did not freeze. A frozen item gets its minimum or
    /// maximum again, because the same clamp applies. An item that stretches on the cross axis gets a measurement
    /// only. Step 7 lays it out with the cross size of the line.
    /// </remarks>
    /// <param name="node">The container.</param>
    /// <param name="children">The children of the container.</param>
    /// <param name="flexLine">The line.</param>
    /// <param name="mainAxis">The main axis.</param>
    /// <param name="crossAxis">The cross axis.</param>
    /// <param name="ownerWidth">The width of the owner of the container. <c>NaN</c> means undefined.</param>
    /// <param name="mainAxisOwnerSize">The size of the owner on the main axis. <c>NaN</c> means undefined.</param>
    /// <param name="availableInnerMainDim">The inner main size of the container. <c>NaN</c> means undefined.</param>
    /// <param name="availableInnerCrossDim">The inner cross size of the container. <c>NaN</c> means undefined.</param>
    /// <param name="availableInnerWidth">The inner width of the container. <c>NaN</c> means undefined.</param>
    /// <param name="availableInnerHeight">The inner height of the container. <c>NaN</c> means undefined.</param>
    /// <param name="mainAxisOverflows"><see langword="true"/> when the flex bases are larger than the main size.</param>
    /// <param name="measureModeCrossDim">The rule for the cross size of the container.</param>
    /// <param name="performLayout"><see langword="true"/> for a full layout of the container.</param>
    /// <param name="generation">The generation number of the pass.</param>
    /// <returns>The sum of the changes of the main sizes, in pixels.</returns>
    private static float DistributeFreeSpaceSecondPass(
        Element node,
        ReadOnlySpan<Element> children,
        ref FlexLine flexLine,
        FlexDirection mainAxis,
        FlexDirection crossAxis,
        float ownerWidth,
        float mainAxisOwnerSize,
        float availableInnerMainDim,
        float availableInnerCrossDim,
        float availableInnerWidth,
        float availableInnerHeight,
        bool mainAxisOverflows,
        MeasureMode measureModeCrossDim,
        bool performLayout,
        long generation)
    {
        float deltaFreeSpace = 0f;
        bool isMainAxisRow = LayoutMath.IsRow(mainAxis);

        // In a wrapped container with an overflow, the lines are not known yet when the children get their sizes.
        // Thus a stretch child does not get the exact cross size of the container here.
        bool isNodeFlexWrap = node.Style.FlexWrap != FlexWrap.NoWrap;

        for (int i = flexLine.StartIndex; i < flexLine.EndIndex; i++)
        {
            Element child = children[i];
            ref readonly ComputedStyle childStyle = ref child.Style;
            if (!IsInFlow(in childStyle))
            {
                continue;
            }

            ref readonly LayoutNode childLayout = ref child.Layout;

            float childFlexBasis = LayoutMath.BoundAxisWithinMinAndMax(
                in childStyle,
                mainAxis,
                childLayout.ComputedFlexBasis,
                mainAxisOwnerSize,
                ownerWidth);
            float updatedMainSize = childFlexBasis;

            if (LayoutMath.IsDefined(flexLine.RemainingFreeSpace) && flexLine.RemainingFreeSpace < 0f)
            {
                float flexShrinkScaledFactor = -ResolveFlexShrink(child) * childFlexBasis;

                // The child can shrink.
                if (flexShrinkScaledFactor != 0f)
                {
                    float childSize;
                    if (LayoutMath.IsDefined(flexLine.TotalFlexShrinkScaledFactors)
                        && flexLine.TotalFlexShrinkScaledFactors == 0f)
                    {
                        childSize = childFlexBasis + flexShrinkScaledFactor;
                    }
                    else
                    {
                        childSize = childFlexBasis
                            + (flexLine.RemainingFreeSpace / flexLine.TotalFlexShrinkScaledFactors * flexShrinkScaledFactor);
                    }

                    updatedMainSize = LayoutMath.BoundAxis(
                        in childStyle,
                        mainAxis,
                        childSize,
                        availableInnerMainDim,
                        availableInnerWidth);
                }
            }
            else if (LayoutMath.IsDefined(flexLine.RemainingFreeSpace) && flexLine.RemainingFreeSpace > 0f)
            {
                float flexGrowFactor = ResolveFlexGrow(child);

                // The child can grow.
                if (!float.IsNaN(flexGrowFactor) && flexGrowFactor != 0f)
                {
                    // The first pass can freeze each growing item and set the total factor to 0, while free space
                    // stays. A division by 0 then gives an infinite size. Thus a total of 0 gives the bound flex
                    // basis: the item keeps its frozen size. Difference from Yoga: Yoga v3.2.1 divides by 0 here.
                    float childSize;
                    if (LayoutMath.IsDefined(flexLine.TotalFlexGrowFactors) && flexLine.TotalFlexGrowFactors == 0f)
                    {
                        childSize = childFlexBasis;
                    }
                    else
                    {
                        childSize = childFlexBasis
                            + (flexLine.RemainingFreeSpace / flexLine.TotalFlexGrowFactors * flexGrowFactor);
                    }

                    updatedMainSize = LayoutMath.BoundAxis(
                        in childStyle,
                        mainAxis,
                        childSize,
                        availableInnerMainDim,
                        availableInnerWidth);
                }
            }

            deltaFreeSpace += updatedMainSize - childFlexBasis;

            float marginMain = LayoutMath.MarginForAxis(in childStyle, mainAxis, availableInnerWidth);
            float marginCross = LayoutMath.MarginForAxis(in childStyle, crossAxis, availableInnerWidth);

            float childCrossSize;
            float childMainSize = updatedMainSize + marginMain;
            MeasureMode childCrossMeasureMode;
            MeasureMode childMainMeasureMode = MeasureMode.Exactly;

            // A child with the stretch alignment and no auto margin on the cross axis stretches.
            bool isStretch = LayoutMath.ResolveChildAlignment(in node.Style, in childStyle) == Align.Stretch
                && !LayoutMath.FlexStartMarginIsAuto(in childStyle, crossAxis)
                && !LayoutMath.FlexEndMarginIsAuto(in childStyle, crossAxis);

            bool hasDefiniteCrossLength = LayoutMath.HasDefiniteLength(in childStyle, crossAxis, availableInnerCrossDim);
            if (LayoutMath.IsDefined(availableInnerCrossDim)
                && !hasDefiniteCrossLength
                && measureModeCrossDim == MeasureMode.Exactly
                && !(isNodeFlexWrap && mainAxisOverflows)
                && isStretch)
            {
                childCrossSize = availableInnerCrossDim;
                childCrossMeasureMode = MeasureMode.Exactly;
            }
            else if (!hasDefiniteCrossLength)
            {
                childCrossSize = availableInnerCrossDim;
                childCrossMeasureMode = LayoutMath.IsUndefined(childCrossSize) ? MeasureMode.Undefined : MeasureMode.AtMost;
            }
            else
            {
                childCrossSize = LayoutMath.ResolvedDimension(in childStyle, crossAxis, availableInnerCrossDim) + marginCross;

                // A percent cross size in a container with no exact cross size is a loose measurement: the child
                // gets the Undefined rule, so its content gives its cross size. Yoga: isLoosePercentageMeasurement.
                bool isLoosePercentageMeasurement =
                    LayoutMath.StyleDimension(in childStyle, crossAxis).Unit == LengthUnit.Percent
                    && measureModeCrossDim != MeasureMode.Exactly;
                childCrossMeasureMode = LayoutMath.IsUndefined(childCrossSize) || isLoosePercentageMeasurement
                    ? MeasureMode.Undefined
                    : MeasureMode.Exactly;
            }

            // The maximum of the child limits the two sizes.
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

            bool requiresStretchLayout = !hasDefiniteCrossLength && isStretch;

            float childWidth = isMainAxisRow ? childMainSize : childCrossSize;
            float childHeight = !isMainAxisRow ? childMainSize : childCrossSize;

            MeasureMode childWidthMeasureMode = isMainAxisRow ? childMainMeasureMode : childCrossMeasureMode;
            MeasureMode childHeightMeasureMode = !isMainAxisRow ? childMainMeasureMode : childCrossMeasureMode;

            // Lay out the child with the new main size. A child that stretches gets its layout in step 7.
            bool isLayoutPass = performLayout && !requiresStretchLayout;
            LayoutInternal(
                child,
                childWidth,
                childHeight,
                childWidthMeasureMode,
                childHeightMeasureMode,
                availableInnerWidth,
                availableInnerHeight,
                isLayoutPass,
                generation);

            // Yoga also keeps an overflow flag here (setLayoutHadOverflow). The engine has no reader for it, so it keeps no flag.
        }

        return deltaFreeSpace;
    }

    /// <summary>
    /// Finds the flexible items whose minimum or maximum applies, and freezes them. Yoga: <c>distributeFreeSpaceFirstPass</c>.
    /// </summary>
    /// <remarks>
    /// For a frozen item, the method removes the clamped change of size from the free space, and it removes the
    /// factor of the item from the total factor. The second pass then gives the rest of the free space to the
    /// other items.
    /// </remarks>
    /// <param name="children">The children of the container.</param>
    /// <param name="flexLine">The line.</param>
    /// <param name="mainAxis">The main axis.</param>
    /// <param name="ownerWidth">The width of the owner of the container. <c>NaN</c> means undefined.</param>
    /// <param name="mainAxisOwnerSize">The size of the owner on the main axis. <c>NaN</c> means undefined.</param>
    /// <param name="availableInnerMainDim">The inner main size of the container. <c>NaN</c> means undefined.</param>
    /// <param name="availableInnerWidth">The inner width of the container. <c>NaN</c> means undefined.</param>
    private static void DistributeFreeSpaceFirstPass(
        ReadOnlySpan<Element> children,
        ref FlexLine flexLine,
        FlexDirection mainAxis,
        float ownerWidth,
        float mainAxisOwnerSize,
        float availableInnerMainDim,
        float availableInnerWidth)
    {
        float deltaFreeSpace = 0f;
        float originalTotalFlexGrowFactors = flexLine.TotalFlexGrowFactors;
        float originalTotalFlexShrinkScaledFactors = flexLine.TotalFlexShrinkScaledFactors;

        for (int i = flexLine.StartIndex; i < flexLine.EndIndex; i++)
        {
            Element child = children[i];
            ref readonly ComputedStyle childStyle = ref child.Style;
            if (!IsInFlow(in childStyle))
            {
                continue;
            }

            ref readonly LayoutNode childLayout = ref child.Layout;

            float childFlexBasis = LayoutMath.BoundAxisWithinMinAndMax(
                in childStyle,
                mainAxis,
                childLayout.ComputedFlexBasis,
                mainAxisOwnerSize,
                ownerWidth);

            if (flexLine.RemainingFreeSpace < 0f)
            {
                float flexShrinkScaledFactor = -ResolveFlexShrink(child) * childFlexBasis;

                // The child can shrink.
                if (LayoutMath.IsDefined(flexShrinkScaledFactor) && flexShrinkScaledFactor != 0f)
                {
                    float baseMainSize = childFlexBasis
                        + (flexLine.RemainingFreeSpace / flexLine.TotalFlexShrinkScaledFactors * flexShrinkScaledFactor);
                    float boundMainSize = LayoutMath.BoundAxis(
                        in childStyle,
                        mainAxis,
                        baseMainSize,
                        availableInnerMainDim,
                        availableInnerWidth);
                    if (LayoutMath.IsDefined(baseMainSize)
                        && LayoutMath.IsDefined(boundMainSize)
                        && baseMainSize != boundMainSize)
                    {
                        // The clamp applies. Remove the size and the factor of the item, so that the second pass
                        // gives the item the same clamped size.
                        deltaFreeSpace += boundMainSize - childFlexBasis;
                        flexLine.TotalFlexShrinkScaledFactors -= -ResolveFlexShrink(child) * childLayout.ComputedFlexBasis;
                    }
                }
            }
            else if (LayoutMath.IsDefined(flexLine.RemainingFreeSpace) && flexLine.RemainingFreeSpace > 0f)
            {
                float flexGrowFactor = ResolveFlexGrow(child);

                // The child can grow. When an earlier item set the total factor to 0, the second pass gives this
                // item its bound flex basis. The item then needs no freeze here: a freeze removes its factor and
                // makes the total less than 0, and the second pass then divides by a wrong total.
                // Difference from Yoga: Yoga v3.2.1 divides by 0 here.
                if (LayoutMath.IsDefined(flexGrowFactor)
                    && flexGrowFactor != 0f
                    && !(LayoutMath.IsDefined(flexLine.TotalFlexGrowFactors) && flexLine.TotalFlexGrowFactors == 0f))
                {
                    float baseMainSize = childFlexBasis
                        + (flexLine.RemainingFreeSpace / flexLine.TotalFlexGrowFactors * flexGrowFactor);
                    float boundMainSize = LayoutMath.BoundAxis(
                        in childStyle,
                        mainAxis,
                        baseMainSize,
                        availableInnerMainDim,
                        availableInnerWidth);
                    if (LayoutMath.IsDefined(baseMainSize)
                        && LayoutMath.IsDefined(boundMainSize)
                        && baseMainSize != boundMainSize)
                    {
                        // The clamp applies. Remove the size and the factor of the item, so that the second pass
                        // gives the item the same clamped size.
                        deltaFreeSpace += boundMainSize - childFlexBasis;
                        flexLine.TotalFlexGrowFactors -= flexGrowFactor;
                    }
                }
            }
        }

        // When each growing item freezes, the subtractions can leave a rounding residual in place of 0. For example,
        // the factors 0.3, 0.3, and 0.4 leave a small negative value, and 1.1, 2.2, and 3.3 leave a small positive
        // value. A division by the residual gives a very large size or a size of 0. Thus a residual that is less than
        // 1e-5 of the original total becomes 0, and the second pass gives each item its bound flex basis.
        // A total that the floor of 1 keeps more than 0 is not a residual, so it does not change.
        // Difference from Yoga: Yoga v3.2.1 divides by the residual.
        if (LayoutMath.IsDefined(flexLine.TotalFlexGrowFactors)
            && MathF.Abs(flexLine.TotalFlexGrowFactors) <= MathF.Abs(originalTotalFlexGrowFactors) * GrowFactorResidualRatio)
        {
            flexLine.TotalFlexGrowFactors = 0f;
        }

        // The total of the scaled shrink factors has the same residual when each shrinking item freezes. The total
        // is negative, so the test uses the absolute values. A residual divides the free space into a very large
        // size. The second pass gives an item with a total of 0 the size of its basis less its scaled factor, and
        // the bound of the item then gives the frozen size.
        // Difference from Yoga: Yoga v3.2.1 divides by the residual.
        if (LayoutMath.IsDefined(flexLine.TotalFlexShrinkScaledFactors)
            && MathF.Abs(flexLine.TotalFlexShrinkScaledFactors)
                <= MathF.Abs(originalTotalFlexShrinkScaledFactors) * GrowFactorResidualRatio)
        {
            flexLine.TotalFlexShrinkScaledFactors = 0f;
        }

        flexLine.RemainingFreeSpace -= deltaFreeSpace;
    }

    /// <summary>
    /// Resolves the main sizes of the items of a line and lays out each item. Yoga: <c>resolveFlexibleLength</c>.
    /// </summary>
    /// <remarks>
    /// The method makes two passes over the items, as Yoga does. The first pass freezes the items whose minimum or
    /// maximum applies. The second pass gives the free space to the other items. The CSS specification repeats the
    /// freeze a variable number of times. Yoga, and thus the engine, does not.
    /// </remarks>
    /// <param name="node">The container.</param>
    /// <param name="children">The children of the container.</param>
    /// <param name="flexLine">The line.</param>
    /// <param name="mainAxis">The main axis.</param>
    /// <param name="crossAxis">The cross axis.</param>
    /// <param name="ownerWidth">The width of the owner of the container. <c>NaN</c> means undefined.</param>
    /// <param name="mainAxisOwnerSize">The size of the owner on the main axis. <c>NaN</c> means undefined.</param>
    /// <param name="availableInnerMainDim">The inner main size of the container. <c>NaN</c> means undefined.</param>
    /// <param name="availableInnerCrossDim">The inner cross size of the container. <c>NaN</c> means undefined.</param>
    /// <param name="availableInnerWidth">The inner width of the container. <c>NaN</c> means undefined.</param>
    /// <param name="availableInnerHeight">The inner height of the container. <c>NaN</c> means undefined.</param>
    /// <param name="mainAxisOverflows"><see langword="true"/> when the flex bases are larger than the main size.</param>
    /// <param name="measureModeCrossDim">The rule for the cross size of the container.</param>
    /// <param name="performLayout"><see langword="true"/> for a full layout of the container.</param>
    /// <param name="generation">The generation number of the pass.</param>
    private static void ResolveFlexibleLength(
        Element node,
        ReadOnlySpan<Element> children,
        ref FlexLine flexLine,
        FlexDirection mainAxis,
        FlexDirection crossAxis,
        float ownerWidth,
        float mainAxisOwnerSize,
        float availableInnerMainDim,
        float availableInnerCrossDim,
        float availableInnerWidth,
        float availableInnerHeight,
        bool mainAxisOverflows,
        MeasureMode measureModeCrossDim,
        bool performLayout,
        long generation)
    {
        float originalFreeSpace = flexLine.RemainingFreeSpace;

        // First pass: find the items whose minimum or maximum applies.
        DistributeFreeSpaceFirstPass(
            children,
            ref flexLine,
            mainAxis,
            ownerWidth,
            mainAxisOwnerSize,
            availableInnerMainDim,
            availableInnerWidth);

        // Second pass: resolve the sizes of the flexible items, and lay out each item.
        float distributedFreeSpace = DistributeFreeSpaceSecondPass(
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

        flexLine.RemainingFreeSpace = originalFreeSpace - distributedFreeSpace;
    }

    /// <summary>
    /// Positions the items of a line on the main axis, and computes the main size and the cross size of the line.
    /// Yoga: <c>justifyMainAxis</c>.
    /// </summary>
    /// <param name="style">The style of the container.</param>
    /// <param name="children">The children of the container.</param>
    /// <param name="flexLine">The line.</param>
    /// <param name="mainAxis">The main axis.</param>
    /// <param name="crossAxis">The cross axis.</param>
    /// <param name="measureModeMainDim">The rule for the main size of the container.</param>
    /// <param name="measureModeCrossDim">The rule for the cross size of the container.</param>
    /// <param name="mainAxisOwnerSize">The size of the owner of the container on the main axis, for a percent minimum. <c>NaN</c> means undefined.</param>
    /// <param name="ownerWidth">The width of the owner of the container, for a percent padding. <c>NaN</c> means undefined.</param>
    /// <param name="availableInnerMainDim">The inner main size of the container. <c>NaN</c> means undefined.</param>
    /// <param name="availableInnerCrossDim">The inner cross size of the container. <c>NaN</c> means undefined.</param>
    /// <param name="availableInnerWidth">The inner width of the container. <c>NaN</c> means undefined.</param>
    /// <param name="performLayout"><see langword="true"/> for a full layout of the container.</param>
    private static void JustifyMainAxis(
        in ComputedStyle style,
        ReadOnlySpan<Element> children,
        ref FlexLine flexLine,
        FlexDirection mainAxis,
        FlexDirection crossAxis,
        MeasureMode measureModeMainDim,
        MeasureMode measureModeCrossDim,
        float mainAxisOwnerSize,
        float ownerWidth,
        float availableInnerMainDim,
        float availableInnerCrossDim,
        float availableInnerWidth,
        bool performLayout)
    {
        float leadingPaddingAndBorderMain = LayoutMath.LeadingPaddingAndBorder(in style, mainAxis, ownerWidth);
        float trailingPaddingAndBorderMain = LayoutMath.TrailingPaddingAndBorder(in style, mainAxis, ownerWidth);

        // With AtMost on the main axis, no free space stays, unless a minimum main size needs it.
        if (measureModeMainDim == MeasureMode.AtMost && flexLine.RemainingFreeSpace > 0f)
        {
            // Yoga also tests that the unit of the minimum is defined. A resolved minimum implies it.
            float minDimension = LayoutMath.ResolvedMinDimension(in style, mainAxis, mainAxisOwnerSize);
            if (LayoutMath.IsDefined(minDimension))
            {
                // The free space fills the content up to the minimum. The minimum here excludes the padding and the border.
                float minAvailableMainDim = minDimension - leadingPaddingAndBorderMain - trailingPaddingAndBorderMain;
                float occupiedSpaceByChildNodes = availableInnerMainDim - flexLine.RemainingFreeSpace;
                flexLine.RemainingFreeSpace = LayoutMath.MaxOrDefined(0f, minAvailableMainDim - occupiedSpaceByChildNodes);
            }
            else
            {
                flexLine.RemainingFreeSpace = 0f;
            }
        }

        // The space before the first item, and the space between two items.
        float leadingMainDim = 0f;
        float betweenMainDim = 0f;

        // A negative free space changes the three space values to flex-start. Yoga: fallbackAlignment.
        Justify justifyContent = style.JustifyContent;
        if (!(flexLine.RemainingFreeSpace >= 0f)
            && (justifyContent == Justify.SpaceBetween
                || justifyContent == Justify.SpaceAround
                || justifyContent == Justify.SpaceEvenly))
        {
            justifyContent = Justify.FlexStart;
        }

        // When the line has auto margins on the main axis, the auto margins take the free space, and
        // justify-content gives no space.
        switch (flexLine.NumberOfAutoMargins == 0 ? justifyContent : Justify.FlexStart)
        {
            case Justify.Center:
                leadingMainDim = flexLine.RemainingFreeSpace / 2f;
                break;
            case Justify.FlexEnd:
                leadingMainDim = flexLine.RemainingFreeSpace;
                break;
            case Justify.SpaceBetween:
                if (flexLine.ItemsInFlowCount > 1)
                {
                    betweenMainDim += flexLine.RemainingFreeSpace / (flexLine.ItemsInFlowCount - 1);
                }

                break;
            case Justify.SpaceEvenly:
                // The space is equal at the edges and between the items.
                leadingMainDim = flexLine.RemainingFreeSpace / (flexLine.ItemsInFlowCount + 1);
                betweenMainDim += leadingMainDim;
                break;
            case Justify.SpaceAround:
                // The space at the edges is half the space between two items. Yoga divides by 0 when the line has
                // no item (only hidden or absolute children), and can give an infinite size. The engine gives no
                // space in that case.
                if (flexLine.ItemsInFlowCount > 0)
                {
                    leadingMainDim = 0.5f * flexLine.RemainingFreeSpace / flexLine.ItemsInFlowCount;
                    betweenMainDim += leadingMainDim * 2f;
                }

                break;
            default:
                // Justify.FlexStart: no space before the first item.
                break;
        }

        flexLine.MainDim = leadingPaddingAndBorderMain + leadingMainDim;
        flexLine.CrossDim = 0f;

        for (int i = flexLine.StartIndex; i < flexLine.EndIndex; i++)
        {
            Element child = children[i];
            ref readonly ComputedStyle childStyle = ref child.Style;
            if (!IsInFlow(in childStyle))
            {
                continue;
            }

            ref LayoutNode childLayout = ref child.Layout;

            // An auto margin on the flex-start edge takes its part of a positive free space before the item.
            if (LayoutMath.FlexStartMarginIsAuto(in childStyle, mainAxis) && flexLine.RemainingFreeSpace > 0f)
            {
                flexLine.MainDim += flexLine.RemainingFreeSpace / flexLine.NumberOfAutoMargins;
            }

            if (performLayout)
            {
                PositionOnAxis(ref childLayout, mainAxis) += flexLine.MainDim;
            }

            if (i != flexLine.LastItemIndex)
            {
                flexLine.MainDim += betweenMainDim;
            }

            // An auto margin on the flex-end edge takes its part of a positive free space after the item.
            if (LayoutMath.FlexEndMarginIsAuto(in childStyle, mainAxis) && flexLine.RemainingFreeSpace > 0f)
            {
                flexLine.MainDim += flexLine.RemainingFreeSpace / flexLine.NumberOfAutoMargins;
            }

            bool canSkipFlex = !performLayout && measureModeCrossDim == MeasureMode.Exactly;
            if (canSkipFlex)
            {
                // The flex step did not run, so the measured sizes are not valid. Use the flex basis.
                flexLine.MainDim += LayoutMath.MarginForAxis(in childStyle, mainAxis, availableInnerWidth)
                    + childLayout.ComputedFlexBasis;
                flexLine.CrossDim = availableInnerCrossDim;
            }
            else
            {
                // The main size is the sum of the item sizes and the spaces.
                flexLine.MainDim += LayoutMath.DimensionWithMargin(in childStyle, in childLayout, mainAxis, availableInnerWidth);

                // The cross size is the largest item, because one line holds one item on the cross axis.
                flexLine.CrossDim = LayoutMath.MaxOrDefined(
                    flexLine.CrossDim,
                    LayoutMath.DimensionWithMargin(in childStyle, in childLayout, crossAxis, availableInnerWidth));
            }
        }

        flexLine.MainDim += trailingPaddingAndBorderMain;
    }

    /// <summary>
    /// One flex line: an index range over the child span, and the running layout of the line. Yoga: <c>FlexLine</c>.
    /// </summary>
    /// <remarks>
    /// The struct lives on the stack. It holds no list. A loop over the items goes from <see cref="StartIndex"/>
    /// to <see cref="EndIndex"/> and skips a child that is not in the flow.
    /// </remarks>
    private struct FlexLine
    {
        /// <summary>The index of the first child of the line.</summary>
        public int StartIndex;

        /// <summary>The index after the last child of the line.</summary>
        public int EndIndex;

        /// <summary>The index of the last item in the flow, or -1 when the line has no item. Yoga: <c>itemsInFlow.back()</c>.</summary>
        public int LastItemIndex;

        /// <summary>The number of items in the flow. Yoga: <c>itemsInFlow.size()</c>.</summary>
        public int ItemsInFlowCount;

        /// <summary>
        /// The number of auto margins on the main axis of the items. An item with two auto margins counts two times.
        /// Yoga: <c>numberOfAutoMargins</c>.
        /// </summary>
        public int NumberOfAutoMargins;

        /// <summary>The sum of the clamped flex bases and the main margins of the items.</summary>
        public float SizeConsumed;

        /// <summary>The sum of the flex grow factors, with a floor of 1 when it is more than 0.</summary>
        public float TotalFlexGrowFactors;

        /// <summary>
        /// The sum of the negative flex shrink factors times the flex bases. Yoga: <c>totalFlexShrinkScaledFactors</c>.
        /// The value is 0 or less for factors of 0 or more. Only a negative shrink factor gives a value of more than 0.
        /// A value of more than 0 and less than 1 gets a floor of 1.
        /// </summary>
        public float TotalFlexShrinkScaledFactors;

        /// <summary>The free space on the main axis. The flex step changes it, and the justification reads it.</summary>
        public float RemainingFreeSpace;

        /// <summary>The main size of the line, with the padding and the border of the container.</summary>
        public float MainDim;

        /// <summary>The cross size of the line.</summary>
        public float CrossDim;
    }
}
