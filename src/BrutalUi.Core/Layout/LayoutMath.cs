// Ported from Yoga (https://github.com/facebook/yoga), v3.2.1, yoga/numeric/Comparison.h, yoga/algorithm/FlexDirection.h, yoga/algorithm/BoundAxis.h, yoga/algorithm/Align.h, yoga/node/Node.cpp, yoga/style/Style.h. Copyright (c) Meta Platforms, Inc. and affiliates.
// MIT License. See THIRD-PARTY-NOTICES.md at the repository root.

using System;

namespace BrutalUi.Core;

/// <summary>
/// The float rules, the axis helpers, the edge helpers, and the bound of a size for the layout engine.
/// </summary>
/// <remarks>
/// <para>
/// <c>NaN</c> means an undefined value, as <c>YGUndefined</c> in Yoga. A compare of two sizes uses
/// <see cref="Epsilon"/>.
/// </para>
/// <para>
/// The edges are left, top, right, and bottom only. There is no start edge and no end edge, because the
/// engine has no text direction. A row axis goes from the left to the right.
/// </para>
/// </remarks>
internal static class LayoutMath
{
    /// <summary>The tolerance of <see cref="InexactEquals(float, float)"/>, as in Yoga.</summary>
    internal const float Epsilon = 0.0001f;

    /// <summary>Tells if a value is undefined (<c>NaN</c>). Yoga: <c>isUndefined</c>.</summary>
    /// <param name="value">The value.</param>
    /// <returns><see langword="true"/> when the value is <c>NaN</c>.</returns>
    internal static bool IsUndefined(float value)
    {
        return float.IsNaN(value);
    }

    /// <summary>Tells if a value is defined (not <c>NaN</c>). Yoga: <c>isDefined</c>.</summary>
    /// <param name="value">The value.</param>
    /// <returns><see langword="true"/> when the value is not <c>NaN</c>.</returns>
    internal static bool IsDefined(float value)
    {
        return !float.IsNaN(value);
    }

    /// <summary>Gives the larger value. When one value is undefined, gives the other value. Yoga: <c>maxOrDefined</c>.</summary>
    /// <param name="a">The first value.</param>
    /// <param name="b">The second value.</param>
    /// <returns>The larger defined value, or <c>NaN</c> when both values are undefined.</returns>
    internal static float MaxOrDefined(float a, float b)
    {
        if (IsDefined(a) && IsDefined(b))
        {
            return MathF.Max(a, b);
        }

        return IsUndefined(a) ? b : a;
    }

    /// <summary>Gives the smaller value. When one value is undefined, gives the other value. Yoga: <c>minOrDefined</c>.</summary>
    /// <param name="a">The first value.</param>
    /// <param name="b">The second value.</param>
    /// <returns>The smaller defined value, or <c>NaN</c> when both values are undefined.</returns>
    internal static float MinOrDefined(float a, float b)
    {
        if (IsDefined(a) && IsDefined(b))
        {
            return MathF.Min(a, b);
        }

        return IsUndefined(a) ? b : a;
    }

    /// <summary>
    /// Compares two values with the tolerance <see cref="Epsilon"/>. Two undefined values are equal. Yoga: <c>inexactEquals</c>.
    /// </summary>
    /// <param name="a">The first value.</param>
    /// <param name="b">The second value.</param>
    /// <returns><see langword="true"/> when the values are equal in the tolerance, or when both values are <c>NaN</c>.</returns>
    internal static bool InexactEquals(float a, float b)
    {
        if (IsDefined(a) && IsDefined(b))
        {
            return MathF.Abs(a - b) < Epsilon;
        }

        return IsUndefined(a) && IsUndefined(b);
    }

    /// <summary>Tells if an axis is horizontal. Yoga: <c>isRow</c>.</summary>
    /// <param name="axis">The axis.</param>
    /// <returns><see langword="true"/> for <see cref="FlexDirection.Row"/> and <see cref="FlexDirection.RowReverse"/>.</returns>
    internal static bool IsRow(FlexDirection axis)
    {
        return axis == FlexDirection.Row || axis == FlexDirection.RowReverse;
    }

    /// <summary>Tells if an axis is vertical. Yoga: <c>isColumn</c>.</summary>
    /// <param name="axis">The axis.</param>
    /// <returns><see langword="true"/> for <see cref="FlexDirection.Column"/> and <see cref="FlexDirection.ColumnReverse"/>.</returns>
    internal static bool IsColumn(FlexDirection axis)
    {
        return axis == FlexDirection.Column || axis == FlexDirection.ColumnReverse;
    }

    /// <summary>
    /// Tells if an axis goes from the right or the bottom. Yoga: <c>needsTrailingPosition</c>.
    /// </summary>
    /// <remarks>On a reverse axis, step 10 of the engine converts the offset from the flex-start edge into X or Y.</remarks>
    /// <param name="axis">The axis.</param>
    /// <returns><see langword="true"/> for <see cref="FlexDirection.RowReverse"/> and <see cref="FlexDirection.ColumnReverse"/>.</returns>
    internal static bool IsReverse(FlexDirection axis)
    {
        return axis == FlexDirection.RowReverse || axis == FlexDirection.ColumnReverse;
    }

    /// <summary>
    /// Gives the cross axis of a main axis. Yoga: <c>resolveCrossDirection</c> with a left-to-right direction.
    /// </summary>
    /// <param name="mainAxis">The main axis.</param>
    /// <returns><see cref="FlexDirection.Row"/> for a column axis, and <see cref="FlexDirection.Column"/> for a row axis.</returns>
    internal static FlexDirection CrossAxis(FlexDirection mainAxis)
    {
        return IsColumn(mainAxis) ? FlexDirection.Row : FlexDirection.Column;
    }

    /// <summary>
    /// Gives the style size on an axis: <see cref="ComputedStyle.Width"/> for a row axis, and
    /// <see cref="ComputedStyle.Height"/> for a column axis. Yoga: <c>getProcessedDimension(dimension(axis))</c>.
    /// </summary>
    /// <remarks>
    /// The method applies the rule of Yoga <c>Node::processDimensions</c>: when the maximum is not
    /// <see cref="Length.Undefined"/> and the minimum has the same unit and value, the maximum gives the size.
    /// Yoga stores the result before each layout of a node. The engine computes it on each read.
    /// </remarks>
    /// <param name="style">The style.</param>
    /// <param name="axis">The axis.</param>
    /// <returns>The style size.</returns>
    internal static Length StyleDimension(in ComputedStyle style, FlexDirection axis)
    {
        bool isRow = IsRow(axis);
        Length max = isRow ? style.MaxWidth : style.MaxHeight;
        Length min = isRow ? style.MinWidth : style.MinHeight;

        // Yoga: inexactEquals of two style lengths compares the unit and the value.
        if (max.Unit != LengthUnit.Undefined && max.Unit == min.Unit && InexactEquals(max.Value, min.Value))
        {
            return max;
        }

        return isRow ? style.Width : style.Height;
    }

    /// <summary>
    /// Resolves the style size on an axis to pixels. Yoga: <c>getResolvedDimension</c> with a border box.
    /// </summary>
    /// <param name="style">The style.</param>
    /// <param name="axis">The axis.</param>
    /// <param name="referenceLength">The size of the owner on the axis, for a percent. <c>NaN</c> means undefined.</param>
    /// <returns>The size in pixels, or <c>NaN</c>.</returns>
    internal static float ResolvedDimension(in ComputedStyle style, FlexDirection axis, float referenceLength)
    {
        return StyleDimension(in style, axis).Resolve(referenceLength);
    }

    /// <summary>
    /// Tells if the style size on an axis resolves to a size of 0 or more. Yoga: <c>hasDefiniteLength</c>.
    /// </summary>
    /// <param name="style">The style.</param>
    /// <param name="axis">The axis.</param>
    /// <param name="ownerSize">The size of the owner on the axis, for a percent. <c>NaN</c> means undefined.</param>
    /// <returns><see langword="true"/> when the size is definite.</returns>
    internal static bool HasDefiniteLength(in ComputedStyle style, FlexDirection axis, float ownerSize)
    {
        float usedValue = ResolvedDimension(in style, axis, ownerSize);
        return IsDefined(usedValue) && usedValue >= 0f;
    }

    /// <summary>
    /// Gives the measured size on an axis: the width for a row axis, and the height for a column axis.
    /// Yoga: <c>measuredDimension(dimension(axis))</c>.
    /// </summary>
    /// <param name="layout">The layout data.</param>
    /// <param name="axis">The axis.</param>
    /// <returns>The measured size in pixels.</returns>
    internal static float MeasuredDimension(in LayoutNode layout, FlexDirection axis)
    {
        return IsRow(axis) ? layout.MeasuredWidth : layout.MeasuredHeight;
    }

    /// <summary>
    /// Gives the sum of the two margins on an axis. Yoga: <c>computeMarginForAxis</c>.
    /// </summary>
    /// <remarks>
    /// An undefined margin, an auto margin, and a percent of an undefined owner width count as 0. The engine gives
    /// the free space to an auto margin in a later step. A percent margin on each edge, also on the top and the
    /// bottom edge, resolves against the owner width. A negative margin is valid.
    /// </remarks>
    /// <param name="style">The style.</param>
    /// <param name="axis">The axis.</param>
    /// <param name="widthSize">The width of the owner, for a percent. <c>NaN</c> means undefined.</param>
    /// <returns>The margin sum in pixels.</returns>
    internal static float MarginForAxis(in ComputedStyle style, FlexDirection axis, float widthSize)
    {
        return LeadingMargin(in style, axis, widthSize) + TrailingMargin(in style, axis, widthSize);
    }

    /// <summary>
    /// Gives the margin on the flex-start edge of an axis: left for a row, right for a row reverse, top for a column,
    /// and bottom for a column reverse. Yoga: <c>computeFlexStartMargin</c>.
    /// </summary>
    /// <param name="style">The style.</param>
    /// <param name="axis">The axis.</param>
    /// <param name="widthSize">The width of the owner, for a percent. <c>NaN</c> means undefined.</param>
    /// <returns>The margin in pixels.</returns>
    internal static float LeadingMargin(in ComputedStyle style, FlexDirection axis, float widthSize)
    {
        return ResolveMargin(in FlexStartEdge(in style.Margin, axis), widthSize);
    }

    /// <summary>
    /// Gives the margin on the flex-end edge of an axis: right for a row, left for a row reverse, bottom for a column,
    /// and top for a column reverse. Yoga: <c>computeFlexEndMargin</c>.
    /// </summary>
    /// <param name="style">The style.</param>
    /// <param name="axis">The axis.</param>
    /// <param name="widthSize">The width of the owner, for a percent. <c>NaN</c> means undefined.</param>
    /// <returns>The margin in pixels.</returns>
    internal static float TrailingMargin(in ComputedStyle style, FlexDirection axis, float widthSize)
    {
        return ResolveMargin(in FlexEndEdge(in style.Margin, axis), widthSize);
    }

    /// <summary>
    /// Gives the padding on the flex-start edge of an axis. Yoga: <c>computeFlexStartPadding</c>.
    /// </summary>
    /// <param name="style">The style.</param>
    /// <param name="axis">The axis.</param>
    /// <param name="widthSize">The width of the owner, for a percent. <c>NaN</c> means undefined.</param>
    /// <returns>The padding in pixels. A value of 0 or more.</returns>
    internal static float LeadingPadding(in ComputedStyle style, FlexDirection axis, float widthSize)
    {
        return ResolvePadding(in FlexStartEdge(in style.Padding, axis), widthSize);
    }

    /// <summary>
    /// Gives the padding on the flex-end edge of an axis. Yoga: <c>computeFlexEndPadding</c>.
    /// </summary>
    /// <param name="style">The style.</param>
    /// <param name="axis">The axis.</param>
    /// <param name="widthSize">The width of the owner, for a percent. <c>NaN</c> means undefined.</param>
    /// <returns>The padding in pixels. A value of 0 or more.</returns>
    internal static float TrailingPadding(in ComputedStyle style, FlexDirection axis, float widthSize)
    {
        return ResolvePadding(in FlexEndEdge(in style.Padding, axis), widthSize);
    }

    /// <summary>
    /// Gives the border width on the flex-start edge of an axis. Yoga: <c>computeFlexStartBorder</c>.
    /// </summary>
    /// <param name="style">The style.</param>
    /// <param name="axis">The axis.</param>
    /// <returns>The border width in pixels. A value of 0 or more.</returns>
    internal static float LeadingBorder(in ComputedStyle style, FlexDirection axis)
    {
        return ResolveBorder(in FlexStartEdge(in style.BorderWidth, axis));
    }

    /// <summary>
    /// Gives the border width on the flex-end edge of an axis. Yoga: <c>computeFlexEndBorder</c>.
    /// </summary>
    /// <param name="style">The style.</param>
    /// <param name="axis">The axis.</param>
    /// <returns>The border width in pixels. A value of 0 or more.</returns>
    internal static float TrailingBorder(in ComputedStyle style, FlexDirection axis)
    {
        return ResolveBorder(in FlexEndEdge(in style.BorderWidth, axis));
    }

    /// <summary>
    /// Gives the padding and the border on the flex-start edge of an axis. Yoga: <c>computeFlexStartPaddingAndBorder</c>.
    /// </summary>
    /// <param name="style">The style.</param>
    /// <param name="axis">The axis.</param>
    /// <param name="widthSize">The width of the owner, for a percent. <c>NaN</c> means undefined.</param>
    /// <returns>The sum in pixels.</returns>
    internal static float LeadingPaddingAndBorder(in ComputedStyle style, FlexDirection axis, float widthSize)
    {
        return LeadingPadding(in style, axis, widthSize) + LeadingBorder(in style, axis);
    }

    /// <summary>
    /// Gives the padding and the border on the flex-end edge of an axis. Yoga: <c>computeFlexEndPaddingAndBorder</c>.
    /// </summary>
    /// <param name="style">The style.</param>
    /// <param name="axis">The axis.</param>
    /// <param name="widthSize">The width of the owner, for a percent. <c>NaN</c> means undefined.</param>
    /// <returns>The sum in pixels.</returns>
    internal static float TrailingPaddingAndBorder(in ComputedStyle style, FlexDirection axis, float widthSize)
    {
        return TrailingPadding(in style, axis, widthSize) + TrailingBorder(in style, axis);
    }

    /// <summary>
    /// Gives the edge length on the flex-start side of an axis: left, right, top, or bottom.
    /// Yoga: <c>flexStartEdge</c>.
    /// </summary>
    /// <param name="edges">The four edge lengths.</param>
    /// <param name="axis">The axis.</param>
    /// <returns>A reference to the length of the edge.</returns>
    private static ref readonly Length FlexStartEdge(in LengthEdges edges, FlexDirection axis)
    {
        switch (axis)
        {
            case FlexDirection.Row:
                return ref edges.Left;
            case FlexDirection.RowReverse:
                return ref edges.Right;
            case FlexDirection.ColumnReverse:
                return ref edges.Bottom;
            default:
                return ref edges.Top;
        }
    }

    /// <summary>
    /// Gives the edge length on the flex-end side of an axis: right, left, bottom, or top.
    /// Yoga: <c>flexEndEdge</c>.
    /// </summary>
    /// <param name="edges">The four edge lengths.</param>
    /// <param name="axis">The axis.</param>
    /// <returns>A reference to the length of the edge.</returns>
    private static ref readonly Length FlexEndEdge(in LengthEdges edges, FlexDirection axis)
    {
        switch (axis)
        {
            case FlexDirection.Row:
                return ref edges.Right;
            case FlexDirection.RowReverse:
                return ref edges.Left;
            case FlexDirection.ColumnReverse:
                return ref edges.Top;
            default:
                return ref edges.Bottom;
        }
    }

    /// <summary>Resolves a margin to pixels. An unresolved margin counts as 0.</summary>
    /// <param name="margin">The margin length.</param>
    /// <param name="widthSize">The width of the owner, for a percent. <c>NaN</c> means undefined.</param>
    /// <returns>The margin in pixels.</returns>
    private static float ResolveMargin(in Length margin, float widthSize)
    {
        float value = margin.Resolve(widthSize);
        return IsDefined(value) ? value : 0f;
    }

    /// <summary>Resolves a padding to pixels. An unresolved or negative padding counts as 0.</summary>
    /// <param name="padding">The padding length.</param>
    /// <param name="widthSize">The width of the owner, for a percent. <c>NaN</c> means undefined.</param>
    /// <returns>The padding in pixels.</returns>
    private static float ResolvePadding(in Length padding, float widthSize)
    {
        return MaxOrDefined(padding.Resolve(widthSize), 0f);
    }

    /// <summary>Resolves a border width to pixels. Only a pixel value counts. A negative value counts as 0.</summary>
    /// <param name="border">The border length.</param>
    /// <returns>The border width in pixels.</returns>
    private static float ResolveBorder(in Length border)
    {
        return border.Unit == LengthUnit.Pixel ? MaxOrDefined(border.Value, 0f) : 0f;
    }

    /// <summary>
    /// Gives the sum of the padding and the border on both edges of an axis. Yoga: <c>paddingAndBorderForAxis</c>.
    /// </summary>
    /// <remarks>The padding and the border resolve with a floor of 0 on each edge.</remarks>
    /// <param name="style">The style.</param>
    /// <param name="axis">The axis.</param>
    /// <param name="widthSize">The width of the owner, for a percent. <c>NaN</c> means undefined.</param>
    /// <returns>The sum in pixels.</returns>
    internal static float PaddingAndBorderForAxis(in ComputedStyle style, FlexDirection axis, float widthSize)
    {
        return LeadingPaddingAndBorder(in style, axis, widthSize) + TrailingPaddingAndBorder(in style, axis, widthSize);
    }

    /// <summary>
    /// Gives the measured size and the margins of an element on an axis. Yoga: <c>Node::dimensionWithMargin</c>.
    /// </summary>
    /// <param name="style">The style of the element.</param>
    /// <param name="layout">The layout data of the element.</param>
    /// <param name="axis">The axis.</param>
    /// <param name="widthSize">The width of the owner, for a percent margin. <c>NaN</c> means undefined.</param>
    /// <returns>The outer size in pixels.</returns>
    internal static float DimensionWithMargin(in ComputedStyle style, in LayoutNode layout, FlexDirection axis, float widthSize)
    {
        return MeasuredDimension(in layout, axis) + MarginForAxis(in style, axis, widthSize);
    }

    /// <summary>
    /// Resolves the minimum size of the style on an axis to pixels. Yoga: <c>Style::resolvedMinDimension</c> with a border box.
    /// </summary>
    /// <param name="style">The style.</param>
    /// <param name="axis">The axis: a row axis gives <see cref="ComputedStyle.MinWidth"/>, a column axis gives <see cref="ComputedStyle.MinHeight"/>.</param>
    /// <param name="referenceLength">The size of the owner on the axis, for a percent. <c>NaN</c> means undefined.</param>
    /// <returns>The minimum in pixels, or <c>NaN</c> when the style gives no minimum.</returns>
    internal static float ResolvedMinDimension(in ComputedStyle style, FlexDirection axis, float referenceLength)
    {
        return (IsRow(axis) ? style.MinWidth : style.MinHeight).Resolve(referenceLength);
    }

    /// <summary>
    /// Resolves the maximum size of the style on an axis to pixels. Yoga: <c>Style::resolvedMaxDimension</c> with a border box.
    /// </summary>
    /// <param name="style">The style.</param>
    /// <param name="axis">The axis: a row axis gives <see cref="ComputedStyle.MaxWidth"/>, a column axis gives <see cref="ComputedStyle.MaxHeight"/>.</param>
    /// <param name="referenceLength">The size of the owner on the axis, for a percent. <c>NaN</c> means undefined.</param>
    /// <returns>The maximum in pixels, or <c>NaN</c> when the style gives no maximum.</returns>
    internal static float ResolvedMaxDimension(in ComputedStyle style, FlexDirection axis, float referenceLength)
    {
        return (IsRow(axis) ? style.MaxWidth : style.MaxHeight).Resolve(referenceLength);
    }

    /// <summary>
    /// Clamps a size between the minimum and the maximum of the style on an axis. Yoga: <c>boundAxisWithinMinAndMax</c>.
    /// </summary>
    /// <remarks>
    /// The maximum applies first, and then the minimum, so the minimum overrides the maximum, as in Yoga.
    /// A limit applies only when it resolves to 0 or more. An undefined <paramref name="value"/> stays undefined,
    /// because a compare with <c>NaN</c> is false.
    /// </remarks>
    /// <param name="style">The style.</param>
    /// <param name="axis">The axis.</param>
    /// <param name="value">The size in pixels. <c>NaN</c> means undefined.</param>
    /// <param name="axisSize">The size of the owner on the axis, for a percent. <c>NaN</c> means undefined.</param>
    /// <param name="widthSize">The width of the owner. <c>NaN</c> means undefined. The border box needs no padding, so the method does not use it.</param>
    /// <returns>The clamped size, or <c>NaN</c> when <paramref name="value"/> is undefined.</returns>
    internal static float BoundAxisWithinMinAndMax(in ComputedStyle style, FlexDirection axis, float value, float axisSize, float widthSize)
    {
        float min = ResolvedMinDimension(in style, axis, axisSize);
        float max = ResolvedMaxDimension(in style, axis, axisSize);

        // A compare with NaN is false, as the FloatOptional compare of Yoga.
        if (max >= 0f && value > max)
        {
            return max;
        }

        if (min >= 0f && value < min)
        {
            return min;
        }

        return value;
    }

    /// <summary>
    /// Gives the alignment of a child on the cross axis of its parent. Yoga: <c>resolveChildAlignment</c>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// An <see cref="ComputedStyle.AlignSelf"/> value that is not <see cref="Align.Auto"/> gives the alignment.
    /// Else the <see cref="ComputedStyle.AlignItems"/> value of the parent gives it.
    /// </para>
    /// <para>
    /// The method applies two rules that Yoga does not have. Yoga has the default <c>stretch</c> on
    /// <c>align-items</c>, but the default of <see cref="ComputedStyle.AlignItems"/> is <see cref="Align.Auto"/>.
    /// Thus <see cref="Align.Auto"/> gives <see cref="Align.Stretch"/>. The three space values give
    /// <see cref="Align.FlexStart"/>. The result is never <see cref="Align.Auto"/> or a space value.
    /// Yoga has a baseline rule for a column parent here. The engine has no baseline value.
    /// </para>
    /// </remarks>
    /// <param name="parent">The style of the parent.</param>
    /// <param name="child">The style of the child.</param>
    /// <returns><see cref="Align.FlexStart"/>, <see cref="Align.Center"/>, <see cref="Align.FlexEnd"/>, or <see cref="Align.Stretch"/>.</returns>
    internal static Align ResolveChildAlignment(ref readonly ComputedStyle parent, ref readonly ComputedStyle child)
    {
        Align align = child.AlignSelf == Align.Auto ? parent.AlignItems : child.AlignSelf;
        switch (align)
        {
            case Align.Auto:
                return Align.Stretch;
            case Align.SpaceBetween:
            case Align.SpaceAround:
            case Align.SpaceEvenly:
                return Align.FlexStart;
            default:
                return align;
        }
    }

    /// <summary>
    /// Tells if the margin on the flex-start edge of an axis is auto: left for a row, right for a row reverse, top for
    /// a column, and bottom for a column reverse. Yoga: <c>Style::flexStartMarginIsAuto</c>.
    /// </summary>
    /// <remarks>
    /// An auto margin resolves to 0 in <see cref="LeadingMargin"/>. The justification of the main axis and the
    /// alignment of the cross axis give it the free space.
    /// </remarks>
    /// <param name="style">The style.</param>
    /// <param name="axis">The axis.</param>
    /// <returns><see langword="true"/> when the margin is auto.</returns>
    internal static bool FlexStartMarginIsAuto(ref readonly ComputedStyle style, FlexDirection axis)
    {
        return FlexStartEdge(in style.Margin, axis).Unit == LengthUnit.Auto;
    }

    /// <summary>
    /// Tells if the margin on the flex-end edge of an axis is auto: right for a row, left for a row reverse, bottom for
    /// a column, and top for a column reverse. Yoga: <c>Style::flexEndMarginIsAuto</c>.
    /// </summary>
    /// <remarks>
    /// An auto margin resolves to 0 in <see cref="TrailingMargin"/>. The justification of the main axis and the
    /// alignment of the cross axis give it the free space.
    /// </remarks>
    /// <param name="style">The style.</param>
    /// <param name="axis">The axis.</param>
    /// <returns><see langword="true"/> when the margin is auto.</returns>
    internal static bool FlexEndMarginIsAuto(ref readonly ComputedStyle style, FlexDirection axis)
    {
        return FlexEndEdge(in style.Margin, axis).Unit == LengthUnit.Auto;
    }

    /// <summary>
    /// Tells if the offset on the flex-start edge of an axis is set: left for a row, right for a row reverse,
    /// top for a column, and bottom for a column reverse.
    /// Yoga: <c>Style::isFlexStartPositionDefined</c> and not <c>Style::isFlexStartPositionAuto</c>.
    /// </summary>
    /// <param name="style">The style.</param>
    /// <param name="axis">The axis.</param>
    /// <returns><see langword="true"/> for a pixel or a percent offset. <see langword="false"/> for undefined and auto.</returns>
    internal static bool IsFlexStartPositionDefined(in ComputedStyle style, FlexDirection axis)
    {
        return IsPositionDefined(in FlexStartPositionEdge(in style, axis));
    }

    /// <summary>
    /// Tells if the offset on the flex-end edge of an axis is set: right for a row, left for a row reverse,
    /// bottom for a column, and top for a column reverse.
    /// Yoga: <c>Style::isFlexEndPositionDefined</c> and not <c>Style::isFlexEndPositionAuto</c>.
    /// </summary>
    /// <param name="style">The style.</param>
    /// <param name="axis">The axis.</param>
    /// <returns><see langword="true"/> for a pixel or a percent offset. <see langword="false"/> for undefined and auto.</returns>
    internal static bool IsFlexEndPositionDefined(in ComputedStyle style, FlexDirection axis)
    {
        return IsPositionDefined(in FlexEndPositionEdge(in style, axis));
    }

    /// <summary>
    /// Tells if the offset on the inline-start edge of an axis is set: left for a row axis, and top for a column axis.
    /// Yoga: <c>Style::isInlineStartPositionDefined</c> and not <c>Style::isInlineStartPositionAuto</c> with a
    /// left-to-right direction.
    /// </summary>
    /// <param name="style">The style.</param>
    /// <param name="axis">The axis. A reverse axis gives the same edge as its forward axis.</param>
    /// <returns><see langword="true"/> for a pixel or a percent offset. <see langword="false"/> for undefined and auto.</returns>
    internal static bool IsInlineStartPositionDefined(in ComputedStyle style, FlexDirection axis)
    {
        return IsPositionDefined(in InlineStartPositionEdge(in style, axis));
    }

    /// <summary>
    /// Tells if the offset on the inline-end edge of an axis is set: right for a row axis, and bottom for a column axis.
    /// Yoga: <c>Style::isInlineEndPositionDefined</c> and not <c>Style::isInlineEndPositionAuto</c> with a
    /// left-to-right direction.
    /// </summary>
    /// <param name="style">The style.</param>
    /// <param name="axis">The axis. A reverse axis gives the same edge as its forward axis.</param>
    /// <returns><see langword="true"/> for a pixel or a percent offset. <see langword="false"/> for undefined and auto.</returns>
    internal static bool IsInlineEndPositionDefined(in ComputedStyle style, FlexDirection axis)
    {
        return IsPositionDefined(in InlineEndPositionEdge(in style, axis));
    }

    /// <summary>
    /// Resolves the offset on the flex-start edge of an axis to pixels. Yoga: <c>Style::computeFlexStartPosition</c>.
    /// </summary>
    /// <param name="style">The style.</param>
    /// <param name="axis">The axis.</param>
    /// <param name="axisSize">The size for a percent. <c>NaN</c> means undefined.</param>
    /// <returns>The offset in pixels. An offset that is not set, or that does not resolve, gives 0.</returns>
    internal static float FlexStartPosition(in ComputedStyle style, FlexDirection axis, float axisSize)
    {
        return ResolvePosition(in FlexStartPositionEdge(in style, axis), axisSize);
    }

    /// <summary>
    /// Resolves the offset on the flex-end edge of an axis to pixels. Yoga: <c>Style::computeFlexEndPosition</c>.
    /// </summary>
    /// <param name="style">The style.</param>
    /// <param name="axis">The axis.</param>
    /// <param name="axisSize">The size for a percent. <c>NaN</c> means undefined.</param>
    /// <returns>The offset in pixels. An offset that is not set, or that does not resolve, gives 0.</returns>
    internal static float FlexEndPosition(in ComputedStyle style, FlexDirection axis, float axisSize)
    {
        return ResolvePosition(in FlexEndPositionEdge(in style, axis), axisSize);
    }

    /// <summary>
    /// Resolves the offset on the inline-start edge of an axis to pixels: left for a row axis, and top for a column
    /// axis. Yoga: <c>Style::computeInlineStartPosition</c> with a left-to-right direction.
    /// </summary>
    /// <param name="style">The style.</param>
    /// <param name="axis">The axis.</param>
    /// <param name="axisSize">The size for a percent. <c>NaN</c> means undefined.</param>
    /// <returns>The offset in pixels. An offset that is not set, or that does not resolve, gives 0.</returns>
    internal static float InlineStartPosition(in ComputedStyle style, FlexDirection axis, float axisSize)
    {
        return ResolvePosition(in InlineStartPositionEdge(in style, axis), axisSize);
    }

    /// <summary>
    /// Resolves the offset on the inline-end edge of an axis to pixels: right for a row axis, and bottom for a column
    /// axis. Yoga: <c>Style::computeInlineEndPosition</c> with a left-to-right direction.
    /// </summary>
    /// <param name="style">The style.</param>
    /// <param name="axis">The axis.</param>
    /// <param name="axisSize">The size for a percent. <c>NaN</c> means undefined.</param>
    /// <returns>The offset in pixels. An offset that is not set, or that does not resolve, gives 0.</returns>
    internal static float InlineEndPosition(in ComputedStyle style, FlexDirection axis, float axisSize)
    {
        return ResolvePosition(in InlineEndPositionEdge(in style, axis), axisSize);
    }

    /// <summary>
    /// Gives the move of an element from its flex position on an axis. Yoga: <c>Node::relativePosition</c>.
    /// </summary>
    /// <remarks>
    /// When the inline-start offset (left or top) is set, it gives the move. Else the negative inline-end offset
    /// (right or bottom) gives it. A reverse axis uses the same edges as its forward axis, as Yoga does with a
    /// left-to-right direction. Thus the value is physical on each axis. Yoga adds the value at the flex-start edge,
    /// so on a reverse axis the move goes the other way. The engine follows CSS: the caller subtracts the value on a
    /// reverse axis, because the flex-start edge there is the right or the bottom edge. The engine has no static
    /// position type, so each element gets the move.
    /// </remarks>
    /// <param name="style">The style.</param>
    /// <param name="axis">The axis.</param>
    /// <param name="axisSize">The size of the owner on the axis, for a percent. <c>NaN</c> means undefined.</param>
    /// <returns>The move in pixels. A positive value moves the element to the right or down.</returns>
    internal static float RelativePosition(in ComputedStyle style, FlexDirection axis, float axisSize)
    {
        if (IsInlineStartPositionDefined(in style, axis))
        {
            return InlineStartPosition(in style, axis, axisSize);
        }

        return -InlineEndPosition(in style, axis, axisSize);
    }

    /// <summary>Tells if an offset is set. Only a pixel or a percent value is set.</summary>
    /// <param name="position">The offset.</param>
    /// <returns><see langword="true"/> for a pixel or a percent offset.</returns>
    private static bool IsPositionDefined(in Length position)
    {
        return position.Unit == LengthUnit.Pixel || position.Unit == LengthUnit.Percent;
    }

    /// <summary>Resolves an offset to pixels. An offset that does not resolve gives 0. Yoga: <c>unwrapOrDefault(0)</c>.</summary>
    /// <param name="position">The offset.</param>
    /// <param name="axisSize">The size for a percent. <c>NaN</c> means undefined.</param>
    /// <returns>The offset in pixels.</returns>
    private static float ResolvePosition(in Length position, float axisSize)
    {
        float value = position.Resolve(axisSize);
        return IsDefined(value) ? value : 0f;
    }

    /// <summary>Gives the offset on the flex-start edge of an axis. Yoga: <c>computePosition(flexStartEdge(axis))</c>.</summary>
    /// <param name="style">The style.</param>
    /// <param name="axis">The axis.</param>
    /// <returns>A reference to <see cref="ComputedStyle.Left"/>, <see cref="ComputedStyle.Right"/>, <see cref="ComputedStyle.Top"/>, or <see cref="ComputedStyle.Bottom"/>.</returns>
    private static ref readonly Length FlexStartPositionEdge(in ComputedStyle style, FlexDirection axis)
    {
        switch (axis)
        {
            case FlexDirection.Row:
                return ref style.Left;
            case FlexDirection.RowReverse:
                return ref style.Right;
            case FlexDirection.ColumnReverse:
                return ref style.Bottom;
            default:
                return ref style.Top;
        }
    }

    /// <summary>Gives the offset on the flex-end edge of an axis. Yoga: <c>computePosition(flexEndEdge(axis))</c>.</summary>
    /// <param name="style">The style.</param>
    /// <param name="axis">The axis.</param>
    /// <returns>A reference to <see cref="ComputedStyle.Right"/>, <see cref="ComputedStyle.Left"/>, <see cref="ComputedStyle.Bottom"/>, or <see cref="ComputedStyle.Top"/>.</returns>
    private static ref readonly Length FlexEndPositionEdge(in ComputedStyle style, FlexDirection axis)
    {
        switch (axis)
        {
            case FlexDirection.Row:
                return ref style.Right;
            case FlexDirection.RowReverse:
                return ref style.Left;
            case FlexDirection.ColumnReverse:
                return ref style.Top;
            default:
                return ref style.Bottom;
        }
    }

    /// <summary>Gives the offset on the inline-start edge of an axis: left or top. Yoga: <c>computePosition(inlineStartEdge(axis))</c>.</summary>
    /// <param name="style">The style.</param>
    /// <param name="axis">The axis.</param>
    /// <returns>A reference to <see cref="ComputedStyle.Left"/> for a row axis, or to <see cref="ComputedStyle.Top"/> for a column axis.</returns>
    private static ref readonly Length InlineStartPositionEdge(in ComputedStyle style, FlexDirection axis)
    {
        if (IsRow(axis))
        {
            return ref style.Left;
        }

        return ref style.Top;
    }

    /// <summary>Gives the offset on the inline-end edge of an axis: right or bottom. Yoga: <c>computePosition(inlineEndEdge(axis))</c>.</summary>
    /// <param name="style">The style.</param>
    /// <param name="axis">The axis.</param>
    /// <returns>A reference to <see cref="ComputedStyle.Right"/> for a row axis, or to <see cref="ComputedStyle.Bottom"/> for a column axis.</returns>
    private static ref readonly Length InlineEndPositionEdge(in ComputedStyle style, FlexDirection axis)
    {
        if (IsRow(axis))
        {
            return ref style.Right;
        }

        return ref style.Bottom;
    }

    /// <summary>
    /// Clamps a size between the minimum and the maximum of the style, and keeps it at the padding and border sum
    /// or more. Yoga: <c>boundAxis</c>.
    /// </summary>
    /// <remarks>An undefined <paramref name="value"/> gives the padding and border sum, as in Yoga.</remarks>
    /// <param name="style">The style.</param>
    /// <param name="axis">The axis.</param>
    /// <param name="value">The size in pixels. <c>NaN</c> means undefined.</param>
    /// <param name="axisSize">The size of the owner on the axis, for a percent. <c>NaN</c> means undefined.</param>
    /// <param name="widthSize">The width of the owner. <c>NaN</c> means undefined.</param>
    /// <returns>The bound size in pixels.</returns>
    internal static float BoundAxis(in ComputedStyle style, FlexDirection axis, float value, float axisSize, float widthSize)
    {
        return MaxOrDefined(
            BoundAxisWithinMinAndMax(in style, axis, value, axisSize, widthSize),
            PaddingAndBorderForAxis(in style, axis, widthSize));
    }
}
