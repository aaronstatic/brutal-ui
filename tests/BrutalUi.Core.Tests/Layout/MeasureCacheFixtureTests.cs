// Ported from Yoga (https://github.com/facebook/yoga), v3.2.1, tests/YGMeasureCacheTest.cpp. Copyright (c) Meta Platforms, Inc. and affiliates.
// MIT License. See THIRD-PARTY-NOTICES.md at the repository root.

using Brutal.Numerics;
using Xunit;

namespace BrutalUi.Core.Tests;

/// <summary>
/// The tests of the hand-written Yoga file <c>YGMeasureCacheTest.cpp</c>. They count the calls of a measure callback
/// over one or two layout passes, so they test the layout cache.
/// </summary>
/// <remarks>
/// <para>
/// The file is not a generated fixture file, so it has no <c>position: absolute</c> root and no RTL half.
/// Each test uses a counting <see cref="MeasureStub"/> with the Yoga measure function in place of the node context
/// counter of Yoga.
/// </para>
/// <para>
/// A call of <c>YGNodeCalculateLayout(root, w, h)</c> is a <see cref="UiContext.Update(float)"/> with a
/// <see cref="UiContext.ViewportSize"/> of (w, h). The root stays in the same context between two calls, so the
/// second call reads the cache of the first call. A style setter of Yoga marks the node dirty, so a port calls
/// <see cref="Element.MarkDirty(DirtyFlags)"/> after a style change.
/// </para>
/// <para>All 6 tests of the file are ports. The fixture ledger in <c>context.md</c> lists the file.</para>
/// </remarks>
public sealed class MeasureCacheFixtureTests
{
    [Fact]
    public void MeasureOnceSingleFlexibleChild()
    {
        // Yoga fixture: measure_once_single_flexible_child
        Element root = new() { Name = "root" };
        root.Inline.FlexDirection = FlexDirection.Row;
        root.Inline.AlignItems = Align.FlexStart;
        root.Inline.Width = 100;
        root.Inline.Height = 100;

        MeasureStub stub = new(MeasureStub.YogaMax);
        Element child0 = AddChild(root, "root_child0", stub.Callback);
        child0.Inline.FlexGrow = 1;

        LayoutFixture.Calculate(root);

        Assert.Equal(1, stub.Count);
        LayoutFixture.AssertNoLayoutBits(root);
    }

    [Fact]
    public void RemeasureWithSameExactWidthLargerThanNeededHeight()
    {
        // Yoga fixture: remeasure_with_same_exact_width_larger_than_needed_height
        Element root = new() { Name = "root" };

        MeasureStub stub = new(MeasureStub.YogaMin);
        AddChild(root, "root_child0", stub.Callback);

        UiContext context = new();
        context.AddRoot(root);

        Calculate(context, 100, 100);
        Calculate(context, 100, 50);

        Assert.Equal(1, stub.Count);
        LayoutFixture.AssertNoLayoutBits(root);
    }

    [Fact]
    public void RemeasureWithSameAtMostWidthLargerThanNeededHeight()
    {
        // Yoga fixture: remeasure_with_same_atmost_width_larger_than_needed_height
        Element root = new() { Name = "root" };
        root.Inline.AlignItems = Align.FlexStart;

        MeasureStub stub = new(MeasureStub.YogaMin);
        AddChild(root, "root_child0", stub.Callback);

        UiContext context = new();
        context.AddRoot(root);

        Calculate(context, 100, 100);
        Calculate(context, 100, 50);

        Assert.Equal(1, stub.Count);
        LayoutFixture.AssertNoLayoutBits(root);
    }

    [Fact]
    public void RemeasureWithComputedWidthLargerThanNeededHeight()
    {
        // Yoga fixture: remeasure_with_computed_width_larger_than_needed_height
        Element root = new() { Name = "root" };
        root.Inline.AlignItems = Align.FlexStart;

        MeasureStub stub = new(MeasureStub.YogaMin);
        AddChild(root, "root_child0", stub.Callback);

        UiContext context = new();
        context.AddRoot(root);

        Calculate(context, 100, 100);

        // YGNodeStyleSetAlignItems marks the node dirty in Yoga.
        root.Inline.AlignItems = Align.Stretch;
        root.MarkDirty(DirtyFlags.Layout);
        Calculate(context, 10, 50);

        Assert.Equal(1, stub.Count);
        LayoutFixture.AssertNoLayoutBits(root);
    }

    [Fact]
    public void RemeasureWithAtMostComputedWidthUndefinedHeight()
    {
        // Yoga fixture: remeasure_with_atmost_computed_width_undefined_height
        Element root = new() { Name = "root" };
        root.Inline.AlignItems = Align.FlexStart;

        MeasureStub stub = new(MeasureStub.YogaMin);
        AddChild(root, "root_child0", stub.Callback);

        UiContext context = new();
        context.AddRoot(root);

        Calculate(context, 100, float.NaN);
        Calculate(context, 10, float.NaN);

        Assert.Equal(1, stub.Count);
        LayoutFixture.AssertNoLayoutBits(root);
    }

    [Fact]
    public void RemeasureWithAlreadyMeasuredValueSmallerButStillFloatEqual()
    {
        // Yoga fixture: remeasure_with_already_measured_value_smaller_but_still_float_equal
        Element root = new() { Name = "root" };
        root.Inline.Width = 288f;
        root.Inline.Height = 288f;
        root.Inline.FlexDirection = FlexDirection.Row;

        Element child0 = AddChild(root, "root_child0", null);
        child0.Inline.Padding = LengthEdges.All(2.88f);
        child0.Inline.FlexDirection = FlexDirection.Row;

        MeasureStub stub = new(MeasureStub.Size84By49);
        AddChild(child0, "root_child0_child0", stub.Callback);

        LayoutFixture.Calculate(root);

        Assert.Equal(1, stub.Count);
        LayoutFixture.AssertNoLayoutBits(root);
    }

    /// <summary>Runs one layout pass with a new viewport size. This matches <c>YGNodeCalculateLayout(root, width, height)</c>.</summary>
    /// <param name="context">The context that holds the root.</param>
    /// <param name="width">The available width, or <c>NaN</c> for no limit.</param>
    /// <param name="height">The available height, or <c>NaN</c> for no limit.</param>
    private static void Calculate(UiContext context, float width, float height)
    {
        context.ViewportSize = new float2(width, height);
        context.Update(0f);
    }

    /// <summary>Creates a named child with a measure callback and adds it at the end of the children of <paramref name="parent"/>.</summary>
    /// <param name="parent">The parent element.</param>
    /// <param name="name">The name of the child, as in the Yoga test.</param>
    /// <param name="measure">The measure callback, or <see langword="null"/> for none.</param>
    /// <returns>The new child.</returns>
    private static Element AddChild(Element parent, string name, MeasureCallback? measure)
    {
        Element child = new() { Name = name };
        child.Layout.Measure = measure;
        parent.Children.Add(child);
        return child;
    }
}
