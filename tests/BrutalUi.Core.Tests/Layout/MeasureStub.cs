// Ported from Yoga (https://github.com/facebook/yoga), v3.2.1, tests/YGMeasureTest.cpp, tests/YGMeasureCacheTest.cpp, tests/YGMeasureModeTest.cpp. Copyright (c) Meta Platforms, Inc. and affiliates.
// MIT License. See THIRD-PARTY-NOTICES.md at the repository root.
using System;
using Brutal.Numerics;
using Xunit;

namespace BrutalUi.Core.Tests;

/// <summary>
/// The constraints of one call of a measure callback.
/// </summary>
/// <param name="Width">The available inner width. <c>NaN</c> for <see cref="MeasureMode.Undefined"/>.</param>
/// <param name="WidthMode">The rule for the width.</param>
/// <param name="Height">The available inner height. <c>NaN</c> for <see cref="MeasureMode.Undefined"/>.</param>
/// <param name="HeightMode">The rule for the height.</param>
internal readonly record struct MeasureConstraint(float Width, MeasureMode WidthMode, float Height, MeasureMode HeightMode);

/// <summary>
/// The measure callbacks of the layout tests.
/// </summary>
/// <remarks>
/// <para>
/// An instance counts its calls. It returns a fixed size, or the result of a function that the constructor gets.
/// It also records the constraints of the first <see cref="MaxRecordedCalls"/> calls. Create the instance one time
/// in the test setup, and give <see cref="Callback"/> to <c>Layout.Measure</c>. The delegate and the record buffer are
/// created one time in the constructor, so a layout pass allocates nothing through the stub.
/// </para>
/// <para>
/// The static delegates are the measure functions of the Yoga test files <c>YGMeasureTest.cpp</c>,
/// <c>YGMeasureCacheTest.cpp</c>, and <c>YGMeasureModeTest.cpp</c>. Each one is a method group that is created one
/// time. They keep no state.
/// </para>
/// </remarks>
internal sealed class MeasureStub
{
    /// <summary>The number of calls that an instance records. A later call is counted but not recorded.</summary>
    public const int MaxRecordedCalls = 16;

    private readonly float2 _size;
    private readonly MeasureCallback? _function;
    private readonly MeasureConstraint[] _constraints = new MeasureConstraint[MaxRecordedCalls];

    /// <summary>
    /// Initializes a new instance of the <see cref="MeasureStub"/> class that returns a fixed size.
    /// </summary>
    /// <param name="width">The width that the callback returns.</param>
    /// <param name="height">The height that the callback returns.</param>
    public MeasureStub(float width, float height)
    {
        _size = new float2(width, height);
        Callback = Measure;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="MeasureStub"/> class that returns the result of a function.
    /// </summary>
    /// <param name="function">The function that gives the size. The stub counts and records each call first.</param>
    public MeasureStub(MeasureCallback function)
    {
        ArgumentNullException.ThrowIfNull(function);
        _function = function;
        Callback = Measure;
    }

    /// <summary>The measure function to give to <c>Layout.Measure</c>. It counts each call.</summary>
    public MeasureCallback Callback { get; }

    /// <summary>Gets the number of calls since the creation of the stub or the last <see cref="Reset"/>.</summary>
    public int Count { get; private set; }

    /// <summary>Gets the constraints of the recorded calls, in the order of the calls.</summary>
    public ReadOnlySpan<MeasureConstraint> Constraints =>
        _constraints.AsSpan(0, Math.Min(Count, MaxRecordedCalls));

    /// <summary>
    /// The Yoga <c>_measureMax</c> function of <c>YGMeasureCacheTest.cpp</c>: it returns the available size, or 10
    /// on an undefined axis.
    /// </summary>
    /// <remarks>This function fills the space, so it breaks the callback contract of the cache. Only the Yoga ports use it.</remarks>
    public static readonly MeasureCallback YogaMax = MeasureYogaMax;

    /// <summary>
    /// The Yoga <c>_measureMin</c> function of <c>YGMeasureCacheTest.cpp</c>: it returns 10 on an undefined axis and
    /// for an <see cref="MeasureMode.AtMost"/> limit over 10, and the available size otherwise.
    /// </summary>
    public static readonly MeasureCallback YogaMin = MeasureYogaMin;

    /// <summary>The Yoga <c>_measure_84_49</c> function of <c>YGMeasureCacheTest.cpp</c>: it returns 84 by 49 pixels.</summary>
    public static readonly MeasureCallback Size84By49 = Measure84By49;

    /// <summary>
    /// The Yoga <c>_measure</c> function of <c>YGMeasureModeTest.cpp</c>. The width is the available width, or 10
    /// when it is undefined. The height is 10 when the height is undefined, and otherwise the available width, as in
    /// Yoga.
    /// </summary>
    public static readonly MeasureCallback YogaModeTest = MeasureYogaModeTest;

    /// <summary>
    /// A text of 60 pixels that wraps into lines of 16 pixels. It keeps the callback contract of the cache.
    /// </summary>
    /// <remarks>
    /// The line width is 60 for an undefined width, the width for <see cref="MeasureMode.Exactly"/>, and the smaller of
    /// 60 and the width for <see cref="MeasureMode.AtMost"/>. The height is 16 times the line count
    /// (<c>ceil(60 / max(lineWidth, 1))</c>), the height for <see cref="MeasureMode.Exactly"/>, and not more than the
    /// height for <see cref="MeasureMode.AtMost"/>. Thus with an <see cref="MeasureMode.AtMost"/> limit, the function
    /// gives the result of an undefined size when that result fits.
    /// </remarks>
    public static readonly MeasureCallback ClampedText = MeasureClampedText;

    /// <summary>Sets <see cref="Count"/> to 0 and clears the record.</summary>
    public void Reset()
    {
        Count = 0;
    }

    /// <summary>The Yoga <c>_measure</c> function without a counter: it returns a size of 10 by 10 pixels.</summary>
    public static readonly MeasureCallback Ten = MeasureTen;

    /// <summary>
    /// The Yoga <c>_simulate_wrapping_text</c> function. It returns 68 by 16 when the width mode is undefined or the
    /// width is 68 or more, and 50 by 32 otherwise.
    /// </summary>
    public static readonly MeasureCallback WrappingText = MeasureWrappingText;

    /// <summary>The Yoga <c>_measure_assert_negative</c> function. It fails when a size is negative, and it returns 0 by 0.</summary>
    public static readonly MeasureCallback AssertNotNegative = MeasureAssertNotNegative;

    /// <summary>The Yoga <c>_measure_90_10</c> function: it returns a size of 90 by 10 pixels.</summary>
    public static readonly MeasureCallback Size90By10 = Measure90By10;

    /// <summary>The Yoga <c>_measure_100_100</c> function: it returns a size of 100 by 100 pixels.</summary>
    public static readonly MeasureCallback Size100By100 = Measure100By100;

    /// <summary>The function returns the available width (0 when it is undefined) and a fixed height of 20 pixels.</summary>
    public static readonly MeasureCallback AvailableWidthBy20 = MeasureAvailableWidthBy20;

    /// <summary>The function returns a size of 40 by 30 pixels. A test uses it for the min and max bounds.</summary>
    public static readonly MeasureCallback Size40By30 = Measure40By30;

    private float2 Measure(Element element, float width, MeasureMode widthMode, float height, MeasureMode heightMode)
    {
        if (Count < MaxRecordedCalls)
        {
            _constraints[Count] = new MeasureConstraint(width, widthMode, height, heightMode);
        }

        Count++;
        return _function is null ? _size : _function(element, width, widthMode, height, heightMode);
    }

    private static float2 MeasureYogaMax(Element element, float width, MeasureMode widthMode, float height, MeasureMode heightMode)
    {
        return new float2(
            widthMode == MeasureMode.Undefined ? 10f : width,
            heightMode == MeasureMode.Undefined ? 10f : height);
    }

    private static float2 MeasureYogaMin(Element element, float width, MeasureMode widthMode, float height, MeasureMode heightMode)
    {
        return new float2(
            widthMode == MeasureMode.Undefined || (widthMode == MeasureMode.AtMost && width > 10f) ? 10f : width,
            heightMode == MeasureMode.Undefined || (heightMode == MeasureMode.AtMost && height > 10f) ? 10f : height);
    }

    private static float2 MeasureClampedText(Element element, float width, MeasureMode widthMode, float height, MeasureMode heightMode)
    {
        float lineWidth = widthMode switch
        {
            MeasureMode.Undefined => 60f,
            MeasureMode.Exactly => width,
            _ => MathF.Min(60f, width),
        };

        float lines = MathF.Ceiling(60f / MathF.Max(lineWidth, 1f));
        float textHeight = heightMode switch
        {
            MeasureMode.Undefined => 16f * lines,
            MeasureMode.Exactly => height,
            _ => MathF.Min(16f * lines, height),
        };

        return new float2(lineWidth, textHeight);
    }

    private static float2 Measure84By49(Element element, float width, MeasureMode widthMode, float height, MeasureMode heightMode)
    {
        return new float2(84f, 49f);
    }

    private static float2 MeasureYogaModeTest(Element element, float width, MeasureMode widthMode, float height, MeasureMode heightMode)
    {
        return new float2(
            widthMode == MeasureMode.Undefined ? 10f : width,
            heightMode == MeasureMode.Undefined ? 10f : width);
    }

    private static float2 MeasureTen(Element element, float width, MeasureMode widthMode, float height, MeasureMode heightMode)
    {
        return new float2(10f, 10f);
    }

    private static float2 MeasureWrappingText(Element element, float width, MeasureMode widthMode, float height, MeasureMode heightMode)
    {
        if (widthMode == MeasureMode.Undefined || width >= 68f)
        {
            return new float2(68f, 16f);
        }

        return new float2(50f, 32f);
    }

    private static float2 MeasureAssertNotNegative(Element element, float width, MeasureMode widthMode, float height, MeasureMode heightMode)
    {
        Assert.True(width >= 0f, $"The width {width} is negative.");
        Assert.True(height >= 0f, $"The height {height} is negative.");
        return new float2(0f, 0f);
    }

    private static float2 Measure90By10(Element element, float width, MeasureMode widthMode, float height, MeasureMode heightMode)
    {
        return new float2(90f, 10f);
    }

    private static float2 Measure100By100(Element element, float width, MeasureMode widthMode, float height, MeasureMode heightMode)
    {
        return new float2(100f, 100f);
    }

    private static float2 MeasureAvailableWidthBy20(Element element, float width, MeasureMode widthMode, float height, MeasureMode heightMode)
    {
        return new float2(widthMode == MeasureMode.Undefined ? 0f : width, 20f);
    }

    private static float2 Measure40By30(Element element, float width, MeasureMode widthMode, float height, MeasureMode heightMode)
    {
        return new float2(40f, 30f);
    }
}
