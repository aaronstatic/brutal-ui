// Ported from Yoga (https://github.com/facebook/yoga), v3.2.1, yoga/algorithm/AbsoluteLayout.cpp. Copyright (c) Meta Platforms, Inc. and affiliates.
// MIT License. See THIRD-PARTY-NOTICES.md at the repository root.

using System;

namespace BrutalUi.Core;

// Step 11 of the algorithm: the size and the position of each absolute child. The containing block of an absolute
// child is always its parent (A12). The engine has no static position type, so it does not search for a containing
// block, and it does not compute a static position. The port follows the Yoga path with no errata.
internal static partial class LayoutEngine
{
    /// <summary>
    /// Lays out and positions each absolute child of an element. Yoga: <c>layoutAbsoluteDescendants</c>.
    /// </summary>
    /// <remarks>
    /// The method visits the children only, because the parent is the containing block of each absolute child (A12).
    /// It skips a hidden child. The containing block is the padding box of the element: the measured size less the
    /// border. Yoga then subtracts the offset of the parent from the containing block. That offset is 0 here.
    /// </remarks>
    /// <param name="node">The element. Its measured size is final.</param>
    /// <param name="children">The children of the element.</param>
    /// <param name="widthMode">The rule for the width of the element.</param>
    /// <param name="ownerWidth">The width of the owner of the element, for a percent padding. <c>NaN</c> means undefined.</param>
    /// <param name="generation">The generation number of the pass.</param>
    private static void LayoutAbsoluteDescendants(
        Element node,
        ReadOnlySpan<Element> children,
        MeasureMode widthMode,
        float ownerWidth,
        long generation)
    {
        ref readonly ComputedStyle style = ref node.Style;
        ref readonly LayoutNode layout = ref node.Layout;

        FlexDirection mainAxis = style.FlexDirection;
        FlexDirection crossAxis = LayoutMath.CrossAxis(mainAxis);

        for (int i = 0; i < children.Length; i++)
        {
            Element child = children[i];
            ref readonly ComputedStyle childStyle = ref child.Style;
            if (childStyle.Display == Display.None || childStyle.Position != PositionType.Absolute)
            {
                continue;
            }

            // The containing block is the padding box of the element. Yoga keeps the inner size for the
            // AbsolutePercentAgainstInnerSize errata. The engine has no errata.
            float containingBlockWidth = layout.MeasuredWidth
                - (LayoutMath.LeadingBorder(in style, FlexDirection.Row) + LayoutMath.TrailingBorder(in style, FlexDirection.Row));
            float containingBlockHeight = layout.MeasuredHeight
                - (LayoutMath.LeadingBorder(in style, FlexDirection.Column) + LayoutMath.TrailingBorder(in style, FlexDirection.Column));

            LayoutAbsoluteChild(
                node,
                child,
                containingBlockWidth,
                containingBlockHeight,
                widthMode,
                ownerWidth,
                generation);

            // The child has its position on the flex-start edge of each axis. On a reverse axis, convert it into the
            // position from the left or the top edge.
            ref LayoutNode childLayout = ref child.Layout;
            if (LayoutMath.IsReverse(mainAxis))
            {
                SetChildTrailingPosition(in layout, ref childLayout, mainAxis);
            }

            if (LayoutMath.IsReverse(crossAxis))
            {
                SetChildTrailingPosition(in layout, ref childLayout, crossAxis);
            }
        }
    }

    /// <summary>
    /// Sizes and positions one absolute child. Yoga: <c>layoutAbsoluteChild</c>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The size: a definite width of the style gives the width. Else <see cref="ComputedStyle.Left"/> and
    /// <see cref="ComputedStyle.Right"/>, when both are set, give the width. The height uses the same rules with
    /// <see cref="ComputedStyle.Top"/> and <see cref="ComputedStyle.Bottom"/>. When a size stays undefined, a
    /// measurement of the child gives it. Then the child gets its layout with the two sizes as exact sizes.
    /// </para>
    /// <para>
    /// The percents: the padding box of the parent is the containing block, as in the Yoga path with no errata.
    /// A percent width, minimum width, maximum width, left offset, and right offset resolve against the width of the
    /// padding box. A percent height, minimum height, maximum height, top offset, and bottom offset resolve against
    /// its height. A percent margin resolves against its width on all four edges, in the size and in each position,
    /// as in CSS. Yoga resolves it against the size of the axis in a position from an offset. The engine does not
    /// follow Yoga here. An auto margin counts as 0.
    /// </para>
    /// <para>
    /// The position: see <see cref="PositionAbsoluteChild"/>. Yoga also applies the aspect ratio here.
    /// The engine has no aspect ratio.
    /// </para>
    /// </remarks>
    /// <param name="node">The parent. It is also the containing block.</param>
    /// <param name="child">The absolute child.</param>
    /// <param name="containingBlockWidth">The width of the padding box of the parent, in pixels.</param>
    /// <param name="containingBlockHeight">The height of the padding box of the parent, in pixels.</param>
    /// <param name="widthMode">The rule for the width of the parent.</param>
    /// <param name="nodeOwnerWidth">The width of the owner of the parent, for a percent padding of the parent. <c>NaN</c> means undefined.</param>
    /// <param name="generation">The generation number of the pass.</param>
    private static void LayoutAbsoluteChild(
        Element node,
        Element child,
        float containingBlockWidth,
        float containingBlockHeight,
        MeasureMode widthMode,
        float nodeOwnerWidth,
        long generation)
    {
        ref readonly ComputedStyle style = ref node.Style;
        ref readonly LayoutNode layout = ref node.Layout;
        ref readonly ComputedStyle childStyle = ref child.Style;
        ref readonly LayoutNode childLayout = ref child.Layout;

        FlexDirection mainAxis = style.FlexDirection;
        FlexDirection crossAxis = LayoutMath.CrossAxis(mainAxis);
        bool isMainAxisRow = LayoutMath.IsRow(mainAxis);

        float childWidth = float.NaN;
        float childHeight = float.NaN;
        MeasureMode childWidthMeasureMode;
        MeasureMode childHeightMeasureMode;

        float marginRow = LayoutMath.MarginForAxis(in childStyle, FlexDirection.Row, containingBlockWidth);
        float marginColumn = LayoutMath.MarginForAxis(in childStyle, FlexDirection.Column, containingBlockWidth);

        if (LayoutMath.HasDefiniteLength(in childStyle, FlexDirection.Row, containingBlockWidth))
        {
            childWidth = LayoutMath.ResolvedDimension(in childStyle, FlexDirection.Row, containingBlockWidth) + marginRow;
        }
        else if (LayoutMath.IsFlexStartPositionDefined(in childStyle, FlexDirection.Row)
            && LayoutMath.IsFlexEndPositionDefined(in childStyle, FlexDirection.Row))
        {
            // The child has no definite width, so the left and the right offsets give the width.
            childWidth = layout.MeasuredWidth
                - (LayoutMath.LeadingBorder(in style, FlexDirection.Row) + LayoutMath.TrailingBorder(in style, FlexDirection.Row))
                - (LayoutMath.FlexStartPosition(in childStyle, FlexDirection.Row, containingBlockWidth)
                    + LayoutMath.FlexEndPosition(in childStyle, FlexDirection.Row, containingBlockWidth));
            childWidth = LayoutMath.BoundAxis(
                in childStyle,
                FlexDirection.Row,
                childWidth,
                containingBlockWidth,
                containingBlockWidth);
        }

        if (LayoutMath.HasDefiniteLength(in childStyle, FlexDirection.Column, containingBlockHeight))
        {
            childHeight = LayoutMath.ResolvedDimension(in childStyle, FlexDirection.Column, containingBlockHeight)
                + marginColumn;
        }
        else if (LayoutMath.IsFlexStartPositionDefined(in childStyle, FlexDirection.Column)
            && LayoutMath.IsFlexEndPositionDefined(in childStyle, FlexDirection.Column))
        {
            // The child has no definite height, so the top and the bottom offsets give the height.
            childHeight = layout.MeasuredHeight
                - (LayoutMath.LeadingBorder(in style, FlexDirection.Column) + LayoutMath.TrailingBorder(in style, FlexDirection.Column))
                - (LayoutMath.FlexStartPosition(in childStyle, FlexDirection.Column, containingBlockHeight)
                    + LayoutMath.FlexEndPosition(in childStyle, FlexDirection.Column, containingBlockHeight));
            childHeight = LayoutMath.BoundAxis(
                in childStyle,
                FlexDirection.Column,
                childHeight,
                containingBlockHeight,
                containingBlockWidth);
        }

        // Yoga applies the aspect ratio here when one size is undefined. The engine has no aspect ratio.

        // When a size is still undefined, measure the content.
        if (LayoutMath.IsUndefined(childWidth) || LayoutMath.IsUndefined(childHeight))
        {
            childWidthMeasureMode = LayoutMath.IsUndefined(childWidth) ? MeasureMode.Undefined : MeasureMode.Exactly;
            childHeightMeasureMode = LayoutMath.IsUndefined(childHeight) ? MeasureMode.Undefined : MeasureMode.Exactly;

            // In a column parent with a defined width, the width of the containing block limits the child, so that
            // the content of the child can wrap at that width, as a browser does.
            if (!isMainAxisRow
                && LayoutMath.IsUndefined(childWidth)
                && widthMode != MeasureMode.Undefined
                && LayoutMath.IsDefined(containingBlockWidth)
                && containingBlockWidth > 0f)
            {
                childWidth = containingBlockWidth;
                childWidthMeasureMode = MeasureMode.AtMost;
            }

            LayoutInternal(
                child,
                childWidth,
                childHeight,
                childWidthMeasureMode,
                childHeightMeasureMode,
                containingBlockWidth,
                containingBlockHeight,
                performLayout: false,
                generation);

            childWidth = childLayout.MeasuredWidth + marginRow;
            childHeight = childLayout.MeasuredHeight + marginColumn;
        }

        LayoutInternal(
            child,
            childWidth,
            childHeight,
            MeasureMode.Exactly,
            MeasureMode.Exactly,
            containingBlockWidth,
            containingBlockHeight,
            performLayout: true,
            generation);

        PositionAbsoluteChild(
            node,
            child,
            mainAxis,
            isMainAxis: true,
            containingBlockWidth,
            containingBlockHeight,
            nodeOwnerWidth);
        PositionAbsoluteChild(
            node,
            child,
            crossAxis,
            isMainAxis: false,
            containingBlockWidth,
            containingBlockHeight,
            nodeOwnerWidth);
    }

    /// <summary>
    /// Positions an absolute child on one axis. Yoga: <c>positionAbsoluteChild</c>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A set inline-start offset (left or top) gives the position from the inner border edge of the parent.
    /// Else a set inline-end offset (right or bottom) gives it from the opposite edge. Else
    /// <see cref="ComputedStyle.JustifyContent"/> of the parent (main axis) or the alignment of the child
    /// (cross axis) gives it.
    /// </para>
    /// <para>
    /// The method writes the offset from the flex-start edge into X or Y. On a reverse axis, the inline-start edge is
    /// the flex-end edge, so the method converts the position. The caller converts it back on a reverse axis.
    /// </para>
    /// </remarks>
    /// <param name="node">The parent. It is also the containing block.</param>
    /// <param name="child">The absolute child. Its measured size is final.</param>
    /// <param name="axis">The axis.</param>
    /// <param name="isMainAxis"><see langword="true"/> when <paramref name="axis"/> is the main axis of the parent.</param>
    /// <param name="containingBlockWidth">The width of the padding box of the parent, in pixels. A percent margin of the child on each axis resolves against it.</param>
    /// <param name="containingBlockHeight">The height of the padding box of the parent, in pixels.</param>
    /// <param name="nodeOwnerWidth">The width of the owner of the parent, for a percent padding of the parent. <c>NaN</c> means undefined.</param>
    private static void PositionAbsoluteChild(
        Element node,
        Element child,
        FlexDirection axis,
        bool isMainAxis,
        float containingBlockWidth,
        float containingBlockHeight,
        float nodeOwnerWidth)
    {
        ref readonly ComputedStyle style = ref node.Style;
        ref readonly LayoutNode layout = ref node.Layout;
        ref readonly ComputedStyle childStyle = ref child.Style;
        ref LayoutNode childLayout = ref child.Layout;

        bool isAxisRow = LayoutMath.IsRow(axis);
        float containingBlockSize = isAxisRow ? containingBlockWidth : containingBlockHeight;

        // The inline-start edge (left or top) is the flex-start edge of the forward axis.
        FlexDirection inlineAxis = isAxisRow ? FlexDirection.Row : FlexDirection.Column;

        // The inline-start offset has priority over the inline-end offset. The method writes the position on the
        // flex-start edge, because the algorithm converts the flex-start positions at the end.
        // An offset resolves a percent against the size of the axis. A margin resolves a percent against the width
        // on both axes, as in CSS. Yoga uses the size of the axis for the margin here. The engine does not.
        if (LayoutMath.IsInlineStartPositionDefined(in childStyle, axis))
        {
            float positionRelativeToInlineStart = LayoutMath.InlineStartPosition(in childStyle, axis, containingBlockSize)
                + LayoutMath.LeadingBorder(in style, inlineAxis)
                + LayoutMath.LeadingMargin(in childStyle, inlineAxis, containingBlockWidth);

            // On a reverse axis, the inline-start edge is not the flex-start edge.
            float positionRelativeToFlexStart = LayoutMath.IsReverse(axis)
                ? GetPositionOfOppositeEdge(positionRelativeToInlineStart, axis, in layout, in childLayout)
                : positionRelativeToInlineStart;

            PositionOnAxis(ref childLayout, axis) = positionRelativeToFlexStart;
        }
        else if (LayoutMath.IsInlineEndPositionDefined(in childStyle, axis))
        {
            float positionRelativeToInlineStart = LayoutMath.MeasuredDimension(in layout, axis)
                - LayoutMath.MeasuredDimension(in childLayout, axis)
                - LayoutMath.TrailingBorder(in style, inlineAxis)
                - LayoutMath.TrailingMargin(in childStyle, inlineAxis, containingBlockWidth)
                - LayoutMath.InlineEndPosition(in childStyle, axis, containingBlockSize);

            float positionRelativeToFlexStart = LayoutMath.IsReverse(axis)
                ? GetPositionOfOppositeEdge(positionRelativeToInlineStart, axis, in layout, in childLayout)
                : positionRelativeToInlineStart;

            PositionOnAxis(ref childLayout, axis) = positionRelativeToFlexStart;
        }
        else if (isMainAxis)
        {
            JustifyAbsoluteChild(node, child, axis, containingBlockWidth, nodeOwnerWidth);
        }
        else
        {
            AlignAbsoluteChild(node, child, axis, containingBlockWidth, nodeOwnerWidth);
        }
    }

    /// <summary>
    /// Positions an absolute child with no offset on the main axis of its parent, from
    /// <see cref="ComputedStyle.JustifyContent"/> of the parent. Yoga: <c>justifyAbsoluteChild</c>.
    /// </summary>
    /// <param name="node">The parent.</param>
    /// <param name="child">The absolute child.</param>
    /// <param name="mainAxis">The main axis of the parent.</param>
    /// <param name="containingBlockWidth">The width of the padding box of the parent, for a percent margin.</param>
    /// <param name="nodeOwnerWidth">The width of the owner of the parent, for a percent padding of the parent. <c>NaN</c> means undefined.</param>
    private static void JustifyAbsoluteChild(
        Element node,
        Element child,
        FlexDirection mainAxis,
        float containingBlockWidth,
        float nodeOwnerWidth)
    {
        switch (node.Style.JustifyContent)
        {
            case Justify.FlexEnd:
                SetFlexEndLayoutPosition(node, child, mainAxis, containingBlockWidth, nodeOwnerWidth);
                break;
            case Justify.Center:
            case Justify.SpaceAround:
            case Justify.SpaceEvenly:
                SetCenterLayoutPosition(node, child, mainAxis, containingBlockWidth, nodeOwnerWidth);
                break;
            default:
                // Justify.FlexStart and Justify.SpaceBetween.
                SetFlexStartLayoutPosition(node, child, mainAxis, containingBlockWidth, nodeOwnerWidth);
                break;
        }
    }

    /// <summary>
    /// Positions an absolute child with no offset on the cross axis of its parent, from the alignment of the child.
    /// Yoga: <c>alignAbsoluteChild</c>.
    /// </summary>
    /// <remarks>
    /// <see cref="Align.FlexEnd"/> and <see cref="Align.Center"/> move the child. Each other value, also
    /// <see cref="Align.Stretch"/>, puts it at the flex-start edge. An absolute child does not stretch.
    /// In a <see cref="FlexWrap.WrapReverse"/> parent, <see cref="Align.FlexEnd"/> puts the child at the flex-start
    /// edge, and each other value except <see cref="Align.Center"/> puts it at the flex-end edge.
    /// </remarks>
    /// <param name="node">The parent.</param>
    /// <param name="child">The absolute child.</param>
    /// <param name="crossAxis">The cross axis of the parent.</param>
    /// <param name="containingBlockWidth">The width of the padding box of the parent, for a percent margin.</param>
    /// <param name="nodeOwnerWidth">The width of the owner of the parent, for a percent padding of the parent. <c>NaN</c> means undefined.</param>
    private static void AlignAbsoluteChild(
        Element node,
        Element child,
        FlexDirection crossAxis,
        float containingBlockWidth,
        float nodeOwnerWidth)
    {
        Align itemAlign = LayoutMath.ResolveChildAlignment(in node.Style, in child.Style);

        // A wrap-reverse parent puts its lines from the flex-end edge of the cross axis. Thus flex-end changes to
        // flex-start, and each value except center changes to flex-end.
        if (node.Style.FlexWrap == FlexWrap.WrapReverse)
        {
            if (itemAlign == Align.FlexEnd)
            {
                itemAlign = Align.FlexStart;
            }
            else if (itemAlign != Align.Center)
            {
                itemAlign = Align.FlexEnd;
            }
        }

        switch (itemAlign)
        {
            case Align.FlexEnd:
                SetFlexEndLayoutPosition(node, child, crossAxis, containingBlockWidth, nodeOwnerWidth);
                break;
            case Align.Center:
                SetCenterLayoutPosition(node, child, crossAxis, containingBlockWidth, nodeOwnerWidth);
                break;
            default:
                // Align.FlexStart and Align.Stretch. ResolveChildAlignment gives no other value.
                SetFlexStartLayoutPosition(node, child, crossAxis, containingBlockWidth, nodeOwnerWidth);
                break;
        }
    }

    /// <summary>
    /// Puts an absolute child at the flex-start edge of the content box of its parent on an axis.
    /// Yoga: <c>setFlexStartLayoutPosition</c>.
    /// </summary>
    /// <param name="node">The parent.</param>
    /// <param name="child">The absolute child.</param>
    /// <param name="axis">The axis.</param>
    /// <param name="containingBlockWidth">The width of the padding box of the parent, for a percent margin.</param>
    /// <param name="nodeOwnerWidth">The width of the owner of the parent, for a percent padding of the parent. <c>NaN</c> means undefined.</param>
    private static void SetFlexStartLayoutPosition(
        Element node,
        Element child,
        FlexDirection axis,
        float containingBlockWidth,
        float nodeOwnerWidth)
    {
        ref readonly ComputedStyle style = ref node.Style;
        ref readonly ComputedStyle childStyle = ref child.Style;

        // Yoga omits the padding for the AbsolutePositionWithoutInsetsExcludesPadding errata. The engine has no errata.
        float position = LayoutMath.LeadingMargin(in childStyle, axis, containingBlockWidth)
            + LayoutMath.LeadingBorder(in style, axis)
            + LayoutMath.LeadingPadding(in style, axis, nodeOwnerWidth);

        PositionOnAxis(ref child.Layout, axis) = position;
    }

    /// <summary>
    /// Puts an absolute child at the flex-end edge of the content box of its parent on an axis.
    /// Yoga: <c>setFlexEndLayoutPosition</c>.
    /// </summary>
    /// <param name="node">The parent.</param>
    /// <param name="child">The absolute child.</param>
    /// <param name="axis">The axis.</param>
    /// <param name="containingBlockWidth">The width of the padding box of the parent, for a percent margin.</param>
    /// <param name="nodeOwnerWidth">The width of the owner of the parent, for a percent padding of the parent. <c>NaN</c> means undefined.</param>
    private static void SetFlexEndLayoutPosition(
        Element node,
        Element child,
        FlexDirection axis,
        float containingBlockWidth,
        float nodeOwnerWidth)
    {
        ref readonly ComputedStyle style = ref node.Style;
        ref readonly ComputedStyle childStyle = ref child.Style;
        ref LayoutNode childLayout = ref child.Layout;

        float flexEndPosition = LayoutMath.TrailingBorder(in style, axis)
            + LayoutMath.TrailingMargin(in childStyle, axis, containingBlockWidth)
            + LayoutMath.TrailingPadding(in style, axis, nodeOwnerWidth);

        PositionOnAxis(ref childLayout, axis) =
            GetPositionOfOppositeEdge(flexEndPosition, axis, in node.Layout, in childLayout);
    }

    /// <summary>
    /// Puts an absolute child at the center of the content box of its parent on an axis.
    /// Yoga: <c>setCenterLayoutPosition</c>.
    /// </summary>
    /// <param name="node">The parent.</param>
    /// <param name="child">The absolute child.</param>
    /// <param name="axis">The axis.</param>
    /// <param name="containingBlockWidth">The width of the padding box of the parent, for a percent margin.</param>
    /// <param name="nodeOwnerWidth">The width of the owner of the parent, for a percent padding of the parent. <c>NaN</c> means undefined.</param>
    private static void SetCenterLayoutPosition(
        Element node,
        Element child,
        FlexDirection axis,
        float containingBlockWidth,
        float nodeOwnerWidth)
    {
        ref readonly ComputedStyle style = ref node.Style;
        ref readonly ComputedStyle childStyle = ref child.Style;
        ref LayoutNode childLayout = ref child.Layout;

        float leadingBorder = LayoutMath.LeadingBorder(in style, axis);
        float leadingPadding = LayoutMath.LeadingPadding(in style, axis, nodeOwnerWidth);

        float parentContentBoxSize = LayoutMath.MeasuredDimension(in node.Layout, axis)
            - leadingBorder
            - LayoutMath.TrailingBorder(in style, axis)
            - leadingPadding
            - LayoutMath.TrailingPadding(in style, axis, nodeOwnerWidth);

        float childOuterSize = LayoutMath.MeasuredDimension(in childLayout, axis)
            + LayoutMath.MarginForAxis(in childStyle, axis, containingBlockWidth);

        float position = ((parentContentBoxSize - childOuterSize) / 2f)
            + leadingBorder
            + LayoutMath.LeadingMargin(in childStyle, axis, containingBlockWidth)
            + leadingPadding;

        PositionOnAxis(ref childLayout, axis) = position;
    }
}
