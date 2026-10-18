// Ported from Yoga (https://github.com/facebook/yoga), v3.2.1, yoga/algorithm/Cache.cpp, yoga/node/CachedMeasurement.h, yoga/node/LayoutResults.h. Copyright (c) Meta Platforms, Inc. and affiliates.
// MIT License. See THIRD-PARTY-NOTICES.md at the repository root.

using System.Runtime.CompilerServices;

namespace BrutalUi.Core;

/// <summary>
/// One cached result of the layout engine: the input constraints and the measured size. Yoga: <c>CachedMeasurement</c>.
/// </summary>
/// <remarks>
/// The available sizes include the margins of the element, as the input of <c>LayoutEngine.LayoutInternal</c> does.
/// The computed sizes are the border box, as <see cref="LayoutNode.MeasuredWidth"/> and
/// <see cref="LayoutNode.MeasuredHeight"/>. <see cref="LayoutCache.Reset"/> writes the empty state: -1 for each size
/// and <see cref="MeasureMode.Undefined"/> for each mode, as Yoga does. The owner sizes are not in the Yoga struct.
/// They are a part of the key only when <see cref="LayoutCache.UsesOwnerSize"/> is <see langword="true"/>.
/// </remarks>
internal struct CachedMeasurement
{
    /// <summary>The available width of the call, with the margins, in pixels. <c>NaN</c> means no limit.</summary>
    internal float AvailableWidth;

    /// <summary>The available height of the call, with the margins, in pixels. <c>NaN</c> means no limit.</summary>
    internal float AvailableHeight;

    /// <summary>
    /// The owner width of the call: the inner width of the parent, in pixels. <c>NaN</c> means undefined.
    /// Yoga has no such field.
    /// </summary>
    internal float OwnerWidth;

    /// <summary>
    /// The owner height of the call: the inner height of the parent, in pixels. <c>NaN</c> means undefined.
    /// Yoga has no such field.
    /// </summary>
    internal float OwnerHeight;

    /// <summary>The rule for the available width of the call. Yoga: <c>widthSizingMode</c>.</summary>
    internal MeasureMode WidthMeasureMode;

    /// <summary>The rule for the available height of the call. Yoga: <c>heightSizingMode</c>.</summary>
    internal MeasureMode HeightMeasureMode;

    /// <summary>The measured width that the call gave, in pixels.</summary>
    internal float ComputedWidth;

    /// <summary>The measured height that the call gave, in pixels.</summary>
    internal float ComputedHeight;
}

/// <summary>
/// The inline buffer of the measurement entries of one element. Yoga: <c>std::array&lt;CachedMeasurement, 8&gt;</c>.
/// </summary>
/// <remarks>The buffer lives inline in <see cref="LayoutCache"/>. Code reads an entry by index or through a span.</remarks>
[InlineArray(LayoutCache.MaxCachedMeasurements)]
internal struct CachedMeasurementBuffer
{
    /// <summary>The first entry. The compiler gives the other entries.</summary>
    private CachedMeasurement _element0;
}

/// <summary>
/// The layout cache of one element: one layout entry and a ring of measurement entries. Yoga: the cache fields of
/// <c>LayoutResults</c>.
/// </summary>
/// <remarks>
/// <para>
/// The cache lives inline in <see cref="LayoutNode"/>. It holds no heap object (Decision 8).
/// A full layout writes <see cref="CachedLayout"/>. A measurement writes the next entry of
/// <see cref="CachedMeasurements"/>. After the last entry, the index goes back to 0.
/// </para>
/// <para>
/// The engine resets the cache on the first visit of a dirty element in a pass, and in a zero-out.
/// <see cref="CanUseCachedMeasurement"/> holds the rules that let a measure callback reuse an entry with other
/// constraints. The engine rounds no value before it compares (Q3). When <see cref="UsesOwnerSize"/> is
/// <see langword="true"/>, a hit also needs the same owner size.
/// </para>
/// </remarks>
internal struct LayoutCache
{
    /// <summary>
    /// The number of measurement entries for each element. Yoga: <c>LayoutResults::MaxCachedMeasurements</c>.
    /// </summary>
    internal const int MaxCachedMeasurements = 8;

    /// <summary>The entry of the last full layout. Yoga: <c>cachedLayout</c>.</summary>
    internal CachedMeasurement CachedLayout;

    /// <summary>The measurement entries. Yoga: <c>cachedMeasurements</c>.</summary>
    internal CachedMeasurementBuffer CachedMeasurements;

    /// <summary>
    /// The number of valid measurement entries, and the index of the next write. Yoga: <c>nextCachedMeasurementsIndex</c>.
    /// </summary>
    internal int NextCachedMeasurementsIndex;

    /// <summary>
    /// Tells if the last computation of the element read the owner size through a percent of the element or of a
    /// child. Yoga has no such field.
    /// </summary>
    /// <remarks>
    /// When the value is <see langword="true"/>, the owner size is a part of the key of each entry: a hit needs the
    /// same <see cref="CachedMeasurement.OwnerWidth"/> and <see cref="CachedMeasurement.OwnerHeight"/>
    /// (<see cref="HasSameOwnerSize"/>). Thus the entries for two owner sizes stay valid together. The value comes
    /// from the style of the element and of its children. A change of these styles or of the child list marks the
    /// element, so the cache resets before the value can change.
    /// </remarks>
    internal bool UsesOwnerSize;

    /// <summary>
    /// Clears the cache. Yoga: the invalidation block of <c>calculateLayoutInternal</c>.
    /// </summary>
    /// <remarks>
    /// The method sets the measurement count to 0 and writes the empty state into the layout entry.
    /// It does not clear the measurement entries, because the count makes them invalid.
    /// </remarks>
    internal void Reset()
    {
        NextCachedMeasurementsIndex = 0;
        CachedLayout.AvailableWidth = -1f;
        CachedLayout.AvailableHeight = -1f;
        CachedLayout.OwnerWidth = -1f;
        CachedLayout.OwnerHeight = -1f;
        CachedLayout.WidthMeasureMode = MeasureMode.Undefined;
        CachedLayout.HeightMeasureMode = MeasureMode.Undefined;
        CachedLayout.ComputedWidth = -1f;
        CachedLayout.ComputedHeight = -1f;
    }

    /// <summary>
    /// Tells if an entry holds the owner size of a new call. Yoga has no such test.
    /// </summary>
    /// <remarks>
    /// The engine calls the method only when <see cref="UsesOwnerSize"/> is <see langword="true"/>. The compare is
    /// exact, as in <see cref="HasSameSpec"/>.
    /// </remarks>
    /// <param name="entry">The cached entry.</param>
    /// <param name="ownerWidth">The new owner width.</param>
    /// <param name="ownerHeight">The new owner height.</param>
    /// <returns><see langword="true"/> when the two owner sizes are the same.</returns>
    internal static bool HasSameOwnerSize(ref readonly CachedMeasurement entry, float ownerWidth, float ownerHeight)
    {
        return IsSameSize(entry.OwnerWidth, ownerWidth) && IsSameSize(entry.OwnerHeight, ownerHeight);
    }

    /// <summary>
    /// Tells if an entry holds the same constraints as a new call. Yoga: the cache test of a layout and of a
    /// measurement in <c>calculateLayoutInternal</c>.
    /// </summary>
    /// <remarks>
    /// Two sizes are the same when they are equal, or when both are <c>NaN</c>. Yoga accepts a difference of less
    /// than <see cref="LayoutMath.Epsilon"/> here. The engine does not: the algorithm has steps that test a size
    /// against 0 (for example the limit of 0 in <c>MeasureNodeWithFixedSize</c>), so two sizes that differ by less
    /// than the tolerance can give two different layouts. An entry with equal inputs always holds the result that a
    /// new computation gives.
    /// </remarks>
    /// <param name="entry">The cached entry.</param>
    /// <param name="widthMode">The new rule for the width.</param>
    /// <param name="availableWidth">The new available width, with the margins.</param>
    /// <param name="heightMode">The new rule for the height.</param>
    /// <param name="availableHeight">The new available height, with the margins.</param>
    /// <returns><see langword="true"/> when the two modes and the two available sizes are the same.</returns>
    internal static bool HasSameSpec(
        ref readonly CachedMeasurement entry,
        MeasureMode widthMode,
        float availableWidth,
        MeasureMode heightMode,
        float availableHeight)
    {
        return IsSameSize(entry.AvailableWidth, availableWidth)
            && IsSameSize(entry.AvailableHeight, availableHeight)
            && entry.WidthMeasureMode == widthMode
            && entry.HeightMeasureMode == heightMode;
    }

    /// <summary>
    /// Tells if two available sizes are the same value. Two <c>NaN</c> values are the same.
    /// </summary>
    /// <param name="a">The first size.</param>
    /// <param name="b">The second size.</param>
    /// <returns><see langword="true"/> when the two values are equal, or when both are <c>NaN</c>.</returns>
    internal static bool IsSameSize(float a, float b)
    {
        return a == b || (float.IsNaN(a) && float.IsNaN(b));
    }

    /// <summary>
    /// Tells if a cached measurement of a measure callback is valid for new constraints.
    /// Yoga: <c>canUseCachedMeasurement</c>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The method compares the width and the height separately. An axis is compatible when one of these rules is true:
    /// </para>
    /// <list type="number">
    /// <item><description>The mode and the available size are the same.</description></item>
    /// <item><description>The new mode is exact, and the new inner size is the old computed size.</description></item>
    /// <item><description>
    /// The old mode had no limit, the new mode is a limit, and the old computed size fits in the new limit.
    /// </description></item>
    /// <item><description>
    /// The old and the new modes are limits, the new limit is smaller, and the old computed size still fits.
    /// </description></item>
    /// </list>
    /// <para>
    /// A negative computed size makes the entry invalid. The inner size is the available size minus the margins.
    /// Yoga rounds the available sizes to the pixel grid first. The engine has no pixel rounding (Q3), so it compares
    /// the values as they are.
    /// </para>
    /// <para>
    /// Yoga differs in rule 4. Yoga compares the old available size with the margins against the new size without
    /// the margins. Then a limit that is up to one margin larger than the old limit counts as smaller, and a callback
    /// result that the old limit clamped stays in use. The engine removes the margins from the two sizes, so the
    /// rule accepts only a limit that is really smaller. The margins of the element do not change between the two
    /// calls, because a change of the style resets the cache.
    /// </para>
    /// <para>
    /// The rules need a measure callback that gives the size of its content up to the limit. A callback that fills
    /// a limit but gives a smaller size with no limit breaks rule 3.
    /// </para>
    /// </remarks>
    /// <param name="widthMode">The new rule for the width.</param>
    /// <param name="availableWidth">The new available width, with the margins.</param>
    /// <param name="heightMode">The new rule for the height.</param>
    /// <param name="availableHeight">The new available height, with the margins.</param>
    /// <param name="last">The cached entry.</param>
    /// <param name="marginRow">The sum of the left and the right margin of the element.</param>
    /// <param name="marginColumn">The sum of the top and the bottom margin of the element.</param>
    /// <returns><see langword="true"/> when the cached size is the result for the new constraints.</returns>
    internal static bool CanUseCachedMeasurement(
        MeasureMode widthMode,
        float availableWidth,
        MeasureMode heightMode,
        float availableHeight,
        ref readonly CachedMeasurement last,
        float marginRow,
        float marginColumn)
    {
        if ((LayoutMath.IsDefined(last.ComputedHeight) && last.ComputedHeight < 0f)
            || (LayoutMath.IsDefined(last.ComputedWidth) && last.ComputedWidth < 0f))
        {
            return false;
        }

        bool hasSameWidthSpec = last.WidthMeasureMode == widthMode
            && LayoutMath.InexactEquals(last.AvailableWidth, availableWidth);
        bool hasSameHeightSpec = last.HeightMeasureMode == heightMode
            && LayoutMath.InexactEquals(last.AvailableHeight, availableHeight);

        bool widthIsCompatible = hasSameWidthSpec
            || SizeIsExactAndMatchesOldMeasuredSize(widthMode, availableWidth - marginRow, last.ComputedWidth)
            || OldSizeIsMaxContentAndStillFits(
                widthMode,
                availableWidth - marginRow,
                last.WidthMeasureMode,
                last.ComputedWidth)
            || NewSizeIsStricterAndStillValid(
                widthMode,
                availableWidth - marginRow,
                last.WidthMeasureMode,
                last.AvailableWidth - marginRow,
                last.ComputedWidth);

        bool heightIsCompatible = hasSameHeightSpec
            || SizeIsExactAndMatchesOldMeasuredSize(heightMode, availableHeight - marginColumn, last.ComputedHeight)
            || OldSizeIsMaxContentAndStillFits(
                heightMode,
                availableHeight - marginColumn,
                last.HeightMeasureMode,
                last.ComputedHeight)
            || NewSizeIsStricterAndStillValid(
                heightMode,
                availableHeight - marginColumn,
                last.HeightMeasureMode,
                last.AvailableHeight - marginColumn,
                last.ComputedHeight);

        return widthIsCompatible && heightIsCompatible;
    }

    /// <summary>
    /// Tells if an exact size is the old computed size. Yoga: <c>sizeIsExactAndMatchesOldMeasuredSize</c>.
    /// </summary>
    /// <param name="sizeMode">The new rule.</param>
    /// <param name="size">The new inner size.</param>
    /// <param name="lastComputedSize">The old computed size.</param>
    /// <returns><see langword="true"/> for an exact mode with a size equal to the old computed size.</returns>
    private static bool SizeIsExactAndMatchesOldMeasuredSize(MeasureMode sizeMode, float size, float lastComputedSize)
    {
        return sizeMode == MeasureMode.Exactly && LayoutMath.InexactEquals(size, lastComputedSize);
    }

    /// <summary>
    /// Tells if a size with no limit still fits in a new limit. Yoga: <c>oldSizeIsMaxContentAndStillFits</c>.
    /// </summary>
    /// <remarks>
    /// The max-content mode of Yoga is <see cref="MeasureMode.Undefined"/>. The fit-content mode is
    /// <see cref="MeasureMode.AtMost"/>.
    /// </remarks>
    /// <param name="sizeMode">The new rule.</param>
    /// <param name="size">The new inner size.</param>
    /// <param name="lastSizeMode">The old rule.</param>
    /// <param name="lastComputedSize">The old computed size.</param>
    /// <returns><see langword="true"/> when the content size of the old call is not more than the new limit.</returns>
    private static bool OldSizeIsMaxContentAndStillFits(
        MeasureMode sizeMode,
        float size,
        MeasureMode lastSizeMode,
        float lastComputedSize)
    {
        return sizeMode == MeasureMode.AtMost
            && lastSizeMode == MeasureMode.Undefined
            && (size >= lastComputedSize || LayoutMath.InexactEquals(size, lastComputedSize));
    }

    /// <summary>
    /// Tells if a smaller limit still holds the old computed size. Yoga: <c>newSizeIsStricterAndStillValid</c>.
    /// </summary>
    /// <param name="sizeMode">The new rule.</param>
    /// <param name="size">The new inner size.</param>
    /// <param name="lastSizeMode">The old rule.</param>
    /// <param name="lastSize">The old inner size: the old available size minus the margins. Yoga keeps the margins here.</param>
    /// <param name="lastComputedSize">The old computed size.</param>
    /// <returns>
    /// <see langword="true"/> when the two rules are limits, the new limit is smaller, and the old computed size fits.
    /// </returns>
    private static bool NewSizeIsStricterAndStillValid(
        MeasureMode sizeMode,
        float size,
        MeasureMode lastSizeMode,
        float lastSize,
        float lastComputedSize)
    {
        return lastSizeMode == MeasureMode.AtMost
            && sizeMode == MeasureMode.AtMost
            && LayoutMath.IsDefined(lastSize)
            && LayoutMath.IsDefined(size)
            && LayoutMath.IsDefined(lastComputedSize)
            && lastSize > size
            && (lastComputedSize <= size || LayoutMath.InexactEquals(size, lastComputedSize));
    }
}
