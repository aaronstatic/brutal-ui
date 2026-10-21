// Ported from Yoga (https://github.com/facebook/yoga), v3.2.1, tests/YGMeasureTest.cpp. Copyright (c) Meta Platforms, Inc. and affiliates.
// MIT License. See THIRD-PARTY-NOTICES.md at the repository root.

using Brutal.Numerics;
using Xunit;

namespace BrutalUi.Core.Tests;

/// <summary>
/// The tests of the hand-written Yoga file <c>YGMeasureTest.cpp</c> that use a measure callback.
/// </summary>
/// <remarks>
/// <para>
/// The file is not a generated fixture file, so it has no <c>position: absolute</c> root and no RTL half.
/// Each test uses <see cref="MeasureStub"/> in place of the Yoga measure functions.
/// </para>
/// <para>
/// The ports skip the tests that need a feature that is out of scope: <c>measure_content_box</c> and
/// <c>measure_border_box</c> (box sizing), <c>cant_call_negative_measure_horizontal</c> (a start edge), the two death
/// tests and <c>can_nullify_measure_func_on_any_node</c> (a Yoga node API). The fixture ledger in <c>context.md</c>
/// lists the file.
/// </para>
/// </remarks>
public sealed class MeasureFixtureTests
{
    [Fact]
    public void DontMeasureSingleGrowShrinkChild()
    {
        // Yoga fixture: dont_measure_single_grow_shrink_child
        Element root = new() { Name = "root" };
        root.Inline.Width = 100;
        root.Inline.Height = 100;

        MeasureStub stub = new(10, 10);
        Element child0 = AddChild(root, "root_child0", stub.Callback);
        child0.Inline.FlexGrow = 1;
        child0.Inline.FlexShrink = 1;

        LayoutFixture.Calculate(root);

        Assert.Equal(0, stub.Count);
        LayoutFixture.AssertNoLayoutBits(root);
    }

    [Fact]
    public void MeasureAbsoluteChildWithNoConstraints()
    {
        // Yoga fixture: measure_absolute_child_with_no_constraints
        Element root = new() { Name = "root" };

        Element child0 = AddChild(root, "root_child0", null);

        MeasureStub stub = new(10, 10);
        Element child0Child0 = AddChild(child0, "root_child0_child0", stub.Callback);
        child0Child0.Inline.Position = PositionType.Absolute;

        LayoutFixture.Calculate(root);

        Assert.Equal(1, stub.Count);
        LayoutFixture.AssertNoLayoutBits(root);
    }

    [Fact]
    public void DontMeasureWhenMinEqualsMax()
    {
        // Yoga fixture: dont_measure_when_min_equals_max
        Element root = new() { Name = "root" };
        root.Inline.AlignItems = Align.FlexStart;
        root.Inline.Width = 100;
        root.Inline.Height = 100;

        MeasureStub stub = new(10, 10);
        Element child0 = AddChild(root, "root_child0", stub.Callback);
        child0.Inline.MinWidth = 10;
        child0.Inline.MaxWidth = 10;
        child0.Inline.MinHeight = 10;
        child0.Inline.MaxHeight = 10;

        LayoutFixture.Calculate(root);

        Assert.Equal(0, stub.Count);
        LayoutFixture.AssertRect(child0, 0, 0, 10, 10);
        LayoutFixture.AssertNoLayoutBits(root);
    }

    [Fact]
    public void DontMeasureWhenMinEqualsMaxPercentages()
    {
        // Yoga fixture: dont_measure_when_min_equals_max_percentages
        Element root = new() { Name = "root" };
        root.Inline.AlignItems = Align.FlexStart;
        root.Inline.Width = 100;
        root.Inline.Height = 100;

        MeasureStub stub = new(10, 10);
        Element child0 = AddChild(root, "root_child0", stub.Callback);
        child0.Inline.MinWidth = Length.Percent(10);
        child0.Inline.MaxWidth = Length.Percent(10);
        child0.Inline.MinHeight = Length.Percent(10);
        child0.Inline.MaxHeight = Length.Percent(10);

        LayoutFixture.Calculate(root);

        Assert.Equal(0, stub.Count);
        LayoutFixture.AssertRect(child0, 0, 0, 10, 10);
        LayoutFixture.AssertNoLayoutBits(root);
    }

    [Fact]
    public void MeasureNodesWithMarginAutoAndStretch()
    {
        // Yoga fixture: measure_nodes_with_margin_auto_and_stretch
        Element root = new() { Name = "root" };
        root.Inline.Width = 500;
        root.Inline.Height = 500;

        MeasureStub stub = new(10, 10);
        Element child0 = AddChild(root, "root_child0", stub.Callback);
        child0.Inline.Margin = child0.Inline.Margin with { Left = Length.Auto };

        LayoutFixture.Calculate(root);

        LayoutFixture.AssertRect(child0, 490, 0, 10, 10);
        LayoutFixture.AssertNoLayoutBits(root);
    }

    [Fact]
    public void DontMeasureWhenMinEqualsMaxMixedWidthPercent()
    {
        // Yoga fixture: dont_measure_when_min_equals_max_mixed_width_percent
        Element root = new() { Name = "root" };
        root.Inline.AlignItems = Align.FlexStart;
        root.Inline.Width = 100;
        root.Inline.Height = 100;

        MeasureStub stub = new(10, 10);
        Element child0 = AddChild(root, "root_child0", stub.Callback);
        child0.Inline.MinWidth = Length.Percent(10);
        child0.Inline.MaxWidth = Length.Percent(10);
        child0.Inline.MinHeight = 10;
        child0.Inline.MaxHeight = 10;

        LayoutFixture.Calculate(root);

        Assert.Equal(0, stub.Count);
        LayoutFixture.AssertRect(child0, 0, 0, 10, 10);
        LayoutFixture.AssertNoLayoutBits(root);
    }

    [Fact]
    public void DontMeasureWhenMinEqualsMaxMixedHeightPercent()
    {
        // Yoga fixture: dont_measure_when_min_equals_max_mixed_height_percent
        Element root = new() { Name = "root" };
        root.Inline.AlignItems = Align.FlexStart;
        root.Inline.Width = 100;
        root.Inline.Height = 100;

        MeasureStub stub = new(10, 10);
        Element child0 = AddChild(root, "root_child0", stub.Callback);
        child0.Inline.MinWidth = 10;
        child0.Inline.MaxWidth = 10;
        child0.Inline.MinHeight = Length.Percent(10);
        child0.Inline.MaxHeight = Length.Percent(10);

        LayoutFixture.Calculate(root);

        Assert.Equal(0, stub.Count);
        LayoutFixture.AssertRect(child0, 0, 0, 10, 10);
        LayoutFixture.AssertNoLayoutBits(root);
    }

    [Fact]
    public void MeasureEnoughSizeShouldBeInSingleLine()
    {
        // Yoga fixture: measure_enough_size_should_be_in_single_line
        Element root = new() { Name = "root" };
        root.Inline.Width = 100;

        Element child0 = AddChild(root, "root_child0", MeasureStub.WrappingText);
        child0.Inline.AlignSelf = Align.FlexStart;

        LayoutFixture.Calculate(root);

        Assert.Equal(68f, child0.Layout.Rect.Z, LayoutFixture.Tolerance);
        Assert.Equal(16f, child0.Layout.Rect.W, LayoutFixture.Tolerance);
        LayoutFixture.AssertNoLayoutBits(root);
    }

    [Fact]
    public void MeasureNotEnoughSizeShouldWrap()
    {
        // Yoga fixture: measure_not_enough_size_should_wrap
        Element root = new() { Name = "root" };
        root.Inline.Width = 55;

        Element child0 = AddChild(root, "root_child0", MeasureStub.WrappingText);
        child0.Inline.AlignSelf = Align.FlexStart;

        LayoutFixture.Calculate(root);

        Assert.Equal(50f, child0.Layout.Rect.Z, LayoutFixture.Tolerance);
        Assert.Equal(32f, child0.Layout.Rect.W, LayoutFixture.Tolerance);
        LayoutFixture.AssertNoLayoutBits(root);
    }

    [Fact]
    public void MeasureZeroSpaceShouldGrow()
    {
        // Yoga fixture: measure_zero_space_should_grow
        Element root = new() { Name = "root" };
        root.Inline.Height = 200;
        root.Inline.FlexDirection = FlexDirection.Column;
        root.Inline.FlexGrow = 0;

        MeasureStub stub = new(10, 10);
        Element child0 = AddChild(root, "root_child0", stub.Callback);
        child0.Inline.FlexDirection = FlexDirection.Column;
        child0.Inline.Padding = LengthEdges.All(100);

        UiContext context = new()
        {
            ViewportSize = new float2(282f, float.NaN),
        };

        context.AddRoot(root);
        context.Update(0f);
        context.RemoveRoot(root);

        Assert.Equal(282f, child0.Layout.Rect.Z, LayoutFixture.Tolerance);
        Assert.Equal(0f, child0.Layout.Rect.Y, LayoutFixture.Tolerance);
        LayoutFixture.AssertNoLayoutBits(root);
    }

    [Fact]
    public void MeasureFlexDirectionRowAndPadding()
    {
        // Yoga fixture: measure_flex_direction_row_and_padding
        Element root = new() { Name = "root" };
        root.Inline.FlexDirection = FlexDirection.Row;
        root.Inline.Padding = LengthEdges.All(25);
        root.Inline.Width = 50;
        root.Inline.Height = 50;

        Element child0 = AddChild(root, "root_child0", MeasureStub.WrappingText);

        Element child1 = AddChild(root, "root_child1", null);
        child1.Inline.Width = 5;
        child1.Inline.Height = 5;

        LayoutFixture.Calculate(root);

        LayoutFixture.AssertRect(root, 0, 0, 50, 50);
        LayoutFixture.AssertRect(child0, 25, 25, 50, 0);
        LayoutFixture.AssertRect(child1, 75, 25, 5, 5);
        LayoutFixture.AssertNoLayoutBits(root);
    }

    [Fact]
    public void MeasureFlexDirectionColumnAndPadding()
    {
        // Yoga fixture: measure_flex_direction_column_and_padding
        Element root = new() { Name = "root" };
        root.Inline.Margin = root.Inline.Margin with { Top = 20 };
        root.Inline.Padding = LengthEdges.All(25);
        root.Inline.Width = 50;
        root.Inline.Height = 50;

        Element child0 = AddChild(root, "root_child0", MeasureStub.WrappingText);

        Element child1 = AddChild(root, "root_child1", null);
        child1.Inline.Width = 5;
        child1.Inline.Height = 5;

        LayoutFixture.Calculate(root);

        LayoutFixture.AssertRect(root, 0, 20, 50, 50);
        LayoutFixture.AssertRect(child0, 25, 25, 0, 32);
        LayoutFixture.AssertRect(child1, 25, 57, 5, 5);
        LayoutFixture.AssertNoLayoutBits(root);
    }

    [Fact]
    public void MeasureFlexDirectionRowNoPadding()
    {
        // Yoga fixture: measure_flex_direction_row_no_padding
        Element root = new() { Name = "root" };
        root.Inline.FlexDirection = FlexDirection.Row;
        root.Inline.Margin = root.Inline.Margin with { Top = 20 };
        root.Inline.Width = 50;
        root.Inline.Height = 50;

        Element child0 = AddChild(root, "root_child0", MeasureStub.WrappingText);

        Element child1 = AddChild(root, "root_child1", null);
        child1.Inline.Width = 5;
        child1.Inline.Height = 5;

        LayoutFixture.Calculate(root);

        LayoutFixture.AssertRect(root, 0, 20, 50, 50);
        LayoutFixture.AssertRect(child0, 0, 0, 50, 50);
        LayoutFixture.AssertRect(child1, 50, 0, 5, 5);
        LayoutFixture.AssertNoLayoutBits(root);
    }

    [Fact]
    public void MeasureFlexDirectionRowNoPaddingAlignItemsFlexstart()
    {
        // Yoga fixture: measure_flex_direction_row_no_padding_align_items_flexstart
        Element root = new() { Name = "root" };
        root.Inline.FlexDirection = FlexDirection.Row;
        root.Inline.Margin = root.Inline.Margin with { Top = 20 };
        root.Inline.Width = 50;
        root.Inline.Height = 50;
        root.Inline.AlignItems = Align.FlexStart;

        Element child0 = AddChild(root, "root_child0", MeasureStub.WrappingText);

        Element child1 = AddChild(root, "root_child1", null);
        child1.Inline.Width = 5;
        child1.Inline.Height = 5;

        LayoutFixture.Calculate(root);

        LayoutFixture.AssertRect(root, 0, 20, 50, 50);
        LayoutFixture.AssertRect(child0, 0, 0, 50, 32);
        LayoutFixture.AssertRect(child1, 50, 0, 5, 5);
        LayoutFixture.AssertNoLayoutBits(root);
    }

    [Fact]
    public void MeasureWithFixedSize()
    {
        // Yoga fixture: measure_with_fixed_size
        Element root = new() { Name = "root" };
        root.Inline.Margin = root.Inline.Margin with { Top = 20 };
        root.Inline.Padding = LengthEdges.All(25);
        root.Inline.Width = 50;
        root.Inline.Height = 50;

        Element child0 = AddChild(root, "root_child0", MeasureStub.WrappingText);
        child0.Inline.Width = 10;
        child0.Inline.Height = 10;

        Element child1 = AddChild(root, "root_child1", null);
        child1.Inline.Width = 5;
        child1.Inline.Height = 5;

        LayoutFixture.Calculate(root);

        LayoutFixture.AssertRect(root, 0, 20, 50, 50);
        LayoutFixture.AssertRect(child0, 25, 25, 10, 10);
        LayoutFixture.AssertRect(child1, 25, 35, 5, 5);
        LayoutFixture.AssertNoLayoutBits(root);
    }

    [Fact]
    public void MeasureWithFlexShrink()
    {
        // Yoga fixture: measure_with_flex_shrink
        Element root = new() { Name = "root" };
        root.Inline.Margin = root.Inline.Margin with { Top = 20 };
        root.Inline.Padding = LengthEdges.All(25);
        root.Inline.Width = 50;
        root.Inline.Height = 50;

        Element child0 = AddChild(root, "root_child0", MeasureStub.WrappingText);
        child0.Inline.FlexShrink = 1;

        Element child1 = AddChild(root, "root_child1", null);
        child1.Inline.Width = 5;
        child1.Inline.Height = 5;

        LayoutFixture.Calculate(root);

        LayoutFixture.AssertRect(root, 0, 20, 50, 50);
        LayoutFixture.AssertRect(child0, 25, 25, 0, 0);
        LayoutFixture.AssertRect(child1, 25, 25, 5, 5);
        LayoutFixture.AssertNoLayoutBits(root);
    }

    [Fact]
    public void MeasureNoPadding()
    {
        // Yoga fixture: measure_no_padding
        Element root = new() { Name = "root" };
        root.Inline.Margin = root.Inline.Margin with { Top = 20 };
        root.Inline.Width = 50;
        root.Inline.Height = 50;

        Element child0 = AddChild(root, "root_child0", MeasureStub.WrappingText);
        child0.Inline.FlexShrink = 1;

        Element child1 = AddChild(root, "root_child1", null);
        child1.Inline.Width = 5;
        child1.Inline.Height = 5;

        LayoutFixture.Calculate(root);

        LayoutFixture.AssertRect(root, 0, 20, 50, 50);
        LayoutFixture.AssertRect(child0, 0, 0, 50, 32);
        LayoutFixture.AssertRect(child1, 0, 32, 5, 5);
        LayoutFixture.AssertNoLayoutBits(root);
    }

    [Fact]
    public void CantCallNegativeMeasure()
    {
        // Yoga fixture: cant_call_negative_measure
        Element root = new() { Name = "root" };
        root.Inline.FlexDirection = FlexDirection.Column;
        root.Inline.Width = 50;
        root.Inline.Height = 10;

        Element child0 = AddChild(root, "root_child0", MeasureStub.AssertNotNegative);
        child0.Inline.Margin = child0.Inline.Margin with { Top = 20 };

        LayoutFixture.Calculate(root);

        LayoutFixture.AssertNoLayoutBits(root);
    }

    [Fact]
    public void PercentWithTextNode()
    {
        // Yoga fixture: percent_with_text_node
        Element root = new() { Name = "root" };
        root.Inline.FlexDirection = FlexDirection.Row;
        root.Inline.JustifyContent = Justify.SpaceBetween;
        root.Inline.AlignItems = Align.Center;
        root.Inline.Width = 100;
        root.Inline.Height = 80;

        Element child0 = AddChild(root, "root_child0", null);

        Element child1 = AddChild(root, "root_child1", MeasureStub.Size90By10);
        child1.Inline.MaxWidth = Length.Percent(50);
        child1.Inline.Padding = child1.Inline.Padding with { Top = Length.Percent(50) };

        LayoutFixture.Calculate(root);

        LayoutFixture.AssertRect(root, 0, 0, 100, 80);
        LayoutFixture.AssertRect(child0, 0, 40, 0, 0);
        LayoutFixture.AssertRect(child1, 50, 10, 50, 60);
        LayoutFixture.AssertNoLayoutBits(root);
    }

    [Fact]
    public void PercentMarginWithMeasureFunc()
    {
        // Yoga fixture: percent_margin_with_measure_func
        Element root = new() { Name = "root" };
        root.Inline.FlexDirection = FlexDirection.Row;
        root.Inline.Width = 500;
        root.Inline.Height = 500;

        Element child0 = AddChild(root, "root_child0", MeasureStub.Size100By100);
        child0.Inline.Width = 100;
        child0.Inline.Height = 100;
        child0.Inline.Margin = child0.Inline.Margin with { Top = 0 };

        Element child1 = AddChild(root, "root_child1", MeasureStub.Size100By100);
        child1.Inline.Width = 100;
        child1.Inline.Height = 100;
        child1.Inline.Margin = child1.Inline.Margin with { Top = 100 };

        Element child2 = AddChild(root, "root_child2", MeasureStub.Size100By100);
        child2.Inline.Width = 100;
        child2.Inline.Height = 100;
        child2.Inline.Margin = child2.Inline.Margin with { Top = Length.Percent(10) };

        Element child3 = AddChild(root, "root_child3", MeasureStub.Size100By100);
        child3.Inline.Width = 100;
        child3.Inline.Height = 100;
        child3.Inline.Margin = child3.Inline.Margin with { Top = Length.Percent(20) };

        LayoutFixture.Calculate(root);

        LayoutFixture.AssertRect(root, 0, 0, 500, 500);
        LayoutFixture.AssertRect(child0, 0, 0, 100, 100);
        LayoutFixture.AssertRect(child1, 100, 100, 100, 100);
        LayoutFixture.AssertRect(child2, 200, 50, 100, 100);
        LayoutFixture.AssertRect(child3, 300, 100, 100, 100);
        LayoutFixture.AssertNoLayoutBits(root);
    }

    [Fact]
    public void PercentPaddingWithMeasureFunc()
    {
        // Yoga fixture: percent_padding_with_measure_func
        Element root = new() { Name = "root" };
        root.Inline.FlexDirection = FlexDirection.Row;
        root.Inline.AlignItems = Align.FlexStart;
        root.Inline.AlignContent = Align.FlexStart;
        root.Inline.Width = 500;
        root.Inline.Height = 500;

        Element child0 = AddChild(root, "root_child0", MeasureStub.Size100By100);
        child0.Inline.Width = 100;
        child0.Inline.Height = 100;
        child0.Inline.Padding = child0.Inline.Padding with { Top = 0 };

        Element child1 = AddChild(root, "root_child1", MeasureStub.Size100By100);
        child1.Inline.Width = 100;
        child1.Inline.Height = 100;
        child1.Inline.Padding = child1.Inline.Padding with { Top = 100 };

        Element child2 = AddChild(root, "root_child2", MeasureStub.Size100By100);
        child2.Inline.Padding = child2.Inline.Padding with { Top = Length.Percent(10) };

        Element child3 = AddChild(root, "root_child3", MeasureStub.Size100By100);
        child3.Inline.Padding = child3.Inline.Padding with { Top = Length.Percent(20) };

        LayoutFixture.Calculate(root);

        LayoutFixture.AssertRect(root, 0, 0, 500, 500);
        LayoutFixture.AssertRect(child0, 0, 0, 100, 100);
        LayoutFixture.AssertRect(child1, 100, 0, 100, 100);
        LayoutFixture.AssertRect(child2, 200, 0, 100, 150);
        LayoutFixture.AssertRect(child3, 300, 0, 100, 200);
        LayoutFixture.AssertNoLayoutBits(root);
    }

    [Fact]
    public void PercentPaddingAndPercentMarginWithMeasureFunc()
    {
        // Yoga fixture: percent_padding_and_percent_margin_with_measure_func
        Element root = new() { Name = "root" };
        root.Inline.FlexDirection = FlexDirection.Row;
        root.Inline.AlignItems = Align.FlexStart;
        root.Inline.AlignContent = Align.FlexStart;
        root.Inline.Width = 500;
        root.Inline.Height = 500;

        Element child0 = AddChild(root, "root_child0", MeasureStub.Size100By100);
        child0.Inline.Width = 100;
        child0.Inline.Height = 100;
        child0.Inline.Padding = child0.Inline.Padding with { Top = 0 };

        Element child1 = AddChild(root, "root_child1", MeasureStub.Size100By100);
        child1.Inline.Width = 100;
        child1.Inline.Height = 100;
        child1.Inline.Padding = child1.Inline.Padding with { Top = 100 };

        Element child2 = AddChild(root, "root_child2", MeasureStub.Size100By100);
        child2.Inline.Padding = child2.Inline.Padding with { Top = Length.Percent(10) };
        child2.Inline.Margin = child2.Inline.Margin with { Top = Length.Percent(10) };

        Element child3 = AddChild(root, "root_child3", MeasureStub.Size100By100);
        child3.Inline.Padding = child3.Inline.Padding with { Top = Length.Percent(20) };
        child3.Inline.Margin = child3.Inline.Margin with { Top = Length.Percent(20) };

        LayoutFixture.Calculate(root);

        LayoutFixture.AssertRect(root, 0, 0, 500, 500);
        LayoutFixture.AssertRect(child0, 0, 0, 100, 100);
        LayoutFixture.AssertRect(child1, 100, 0, 100, 100);
        LayoutFixture.AssertRect(child2, 200, 50, 100, 150);
        LayoutFixture.AssertRect(child3, 300, 100, 100, 200);
        LayoutFixture.AssertNoLayoutBits(root);
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
