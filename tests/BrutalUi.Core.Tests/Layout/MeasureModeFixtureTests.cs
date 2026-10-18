// Ported from Yoga (https://github.com/facebook/yoga), v3.2.1, tests/YGMeasureModeTest.cpp. Copyright (c) Meta Platforms, Inc. and affiliates.
// MIT License. See THIRD-PARTY-NOTICES.md at the repository root.

using Xunit;

namespace BrutalUi.Core.Tests;

/// <summary>
/// The tests of the hand-written Yoga file <c>YGMeasureModeTest.cpp</c>. They check the number of calls of a measure
/// callback and the sizes and the modes that each call gets.
/// </summary>
/// <remarks>
/// <para>
/// The file is not a generated fixture file, so it has no <c>position: absolute</c> root and no RTL half.
/// Each test uses a recording <see cref="MeasureStub"/> with <see cref="MeasureStub.YogaModeTest"/> in place of the
/// constraint list of Yoga. Yoga compares a size with <c>ASSERT_FLOAT_EQ</c>. A port uses the tolerance of
/// <see cref="LayoutFixture"/>.
/// </para>
/// <para>
/// Two tests (<c>overflow_scroll_column</c> and <c>overflow_scroll_row</c>) expect one call. Before the layout cache,
/// the engine called the callback two times there. All 10 tests of the file are ports. The fixture ledger in
/// <c>context.md</c> lists the file.
/// </para>
/// </remarks>
public sealed class MeasureModeFixtureTests
{
    [Fact]
    public void ExactlyMeasureStretchedChildColumn()
    {
        // Yoga fixture: exactly_measure_stretched_child_column
        Element root = new() { Name = "root" };
        root.Style.Width = 100;
        root.Style.Height = 100;

        MeasureStub stub = AddMeasuredChild(root);

        LayoutFixture.Calculate(root);

        Assert.Equal(1, stub.Count);
        Assert.Equal(100f, stub.Constraints[0].Width, LayoutFixture.Tolerance);
        Assert.Equal(MeasureMode.Exactly, stub.Constraints[0].WidthMode);
        LayoutFixture.AssertNoLayoutBits(root);
    }

    [Fact]
    public void ExactlyMeasureStretchedChildRow()
    {
        // Yoga fixture: exactly_measure_stretched_child_row
        Element root = new() { Name = "root" };
        root.Style.FlexDirection = FlexDirection.Row;
        root.Style.Width = 100;
        root.Style.Height = 100;

        MeasureStub stub = AddMeasuredChild(root);

        LayoutFixture.Calculate(root);

        Assert.Equal(1, stub.Count);
        Assert.Equal(100f, stub.Constraints[0].Height, LayoutFixture.Tolerance);
        Assert.Equal(MeasureMode.Exactly, stub.Constraints[0].HeightMode);
        LayoutFixture.AssertNoLayoutBits(root);
    }

    [Fact]
    public void AtMostMainAxisColumn()
    {
        // Yoga fixture: at_most_main_axis_column
        Element root = new() { Name = "root" };
        root.Style.Width = 100;
        root.Style.Height = 100;

        MeasureStub stub = AddMeasuredChild(root);

        LayoutFixture.Calculate(root);

        Assert.Equal(1, stub.Count);
        Assert.Equal(100f, stub.Constraints[0].Height, LayoutFixture.Tolerance);
        Assert.Equal(MeasureMode.AtMost, stub.Constraints[0].HeightMode);
        LayoutFixture.AssertNoLayoutBits(root);
    }

    [Fact]
    public void AtMostCrossAxisColumn()
    {
        // Yoga fixture: at_most_cross_axis_column
        Element root = new() { Name = "root" };
        root.Style.AlignItems = Align.FlexStart;
        root.Style.Width = 100;
        root.Style.Height = 100;

        MeasureStub stub = AddMeasuredChild(root);

        LayoutFixture.Calculate(root);

        Assert.Equal(1, stub.Count);
        Assert.Equal(100f, stub.Constraints[0].Width, LayoutFixture.Tolerance);
        Assert.Equal(MeasureMode.AtMost, stub.Constraints[0].WidthMode);
        LayoutFixture.AssertNoLayoutBits(root);
    }

    [Fact]
    public void AtMostMainAxisRow()
    {
        // Yoga fixture: at_most_main_axis_row
        Element root = new() { Name = "root" };
        root.Style.FlexDirection = FlexDirection.Row;
        root.Style.Width = 100;
        root.Style.Height = 100;

        MeasureStub stub = AddMeasuredChild(root);

        LayoutFixture.Calculate(root);

        Assert.Equal(1, stub.Count);
        Assert.Equal(100f, stub.Constraints[0].Width, LayoutFixture.Tolerance);
        Assert.Equal(MeasureMode.AtMost, stub.Constraints[0].WidthMode);
        LayoutFixture.AssertNoLayoutBits(root);
    }

    [Fact]
    public void AtMostCrossAxisRow()
    {
        // Yoga fixture: at_most_cross_axis_row
        Element root = new() { Name = "root" };
        root.Style.FlexDirection = FlexDirection.Row;
        root.Style.AlignItems = Align.FlexStart;
        root.Style.Width = 100;
        root.Style.Height = 100;

        MeasureStub stub = AddMeasuredChild(root);

        LayoutFixture.Calculate(root);

        Assert.Equal(1, stub.Count);
        Assert.Equal(100f, stub.Constraints[0].Height, LayoutFixture.Tolerance);
        Assert.Equal(MeasureMode.AtMost, stub.Constraints[0].HeightMode);
        LayoutFixture.AssertNoLayoutBits(root);
    }

    [Fact]
    public void FlexChild()
    {
        // Yoga fixture: flex_child
        Element root = new() { Name = "root" };
        root.Style.Height = 100;

        MeasureStub stub = AddMeasuredChild(root);
        root.Children[0].Style.FlexGrow = 1;

        LayoutFixture.Calculate(root);

        Assert.Equal(2, stub.Count);
        Assert.Equal(100f, stub.Constraints[0].Height, LayoutFixture.Tolerance);
        Assert.Equal(MeasureMode.AtMost, stub.Constraints[0].HeightMode);
        Assert.Equal(100f, stub.Constraints[1].Height, LayoutFixture.Tolerance);
        Assert.Equal(MeasureMode.Exactly, stub.Constraints[1].HeightMode);
        LayoutFixture.AssertNoLayoutBits(root);
    }

    [Fact]
    public void FlexChildWithFlexBasis()
    {
        // Yoga fixture: flex_child_with_flex_basis
        Element root = new() { Name = "root" };
        root.Style.Height = 100;

        MeasureStub stub = AddMeasuredChild(root);
        root.Children[0].Style.FlexGrow = 1;
        root.Children[0].Style.FlexBasis = 0;

        LayoutFixture.Calculate(root);

        Assert.Equal(1, stub.Count);
        Assert.Equal(100f, stub.Constraints[0].Height, LayoutFixture.Tolerance);
        Assert.Equal(MeasureMode.Exactly, stub.Constraints[0].HeightMode);
        LayoutFixture.AssertNoLayoutBits(root);
    }

    [Fact]
    public void OverflowScrollColumn()
    {
        // Yoga fixture: overflow_scroll_column
        Element root = new() { Name = "root" };
        root.Style.AlignItems = Align.FlexStart;
        root.Style.Overflow = Overflow.Scroll;
        root.Style.Height = 100;
        root.Style.Width = 100;

        MeasureStub stub = AddMeasuredChild(root);

        LayoutFixture.Calculate(root);

        Assert.Equal(1, stub.Count);
        Assert.Equal(100f, stub.Constraints[0].Width, LayoutFixture.Tolerance);
        Assert.Equal(MeasureMode.AtMost, stub.Constraints[0].WidthMode);
        Assert.True(float.IsNaN(stub.Constraints[0].Height), $"The height {stub.Constraints[0].Height} is not NaN.");
        Assert.Equal(MeasureMode.Undefined, stub.Constraints[0].HeightMode);
        LayoutFixture.AssertNoLayoutBits(root);
    }

    [Fact]
    public void OverflowScrollRow()
    {
        // Yoga fixture: overflow_scroll_row
        Element root = new() { Name = "root" };
        root.Style.AlignItems = Align.FlexStart;
        root.Style.FlexDirection = FlexDirection.Row;
        root.Style.Overflow = Overflow.Scroll;
        root.Style.Height = 100;
        root.Style.Width = 100;

        MeasureStub stub = AddMeasuredChild(root);

        LayoutFixture.Calculate(root);

        Assert.Equal(1, stub.Count);
        Assert.True(float.IsNaN(stub.Constraints[0].Width), $"The width {stub.Constraints[0].Width} is not NaN.");
        Assert.Equal(MeasureMode.Undefined, stub.Constraints[0].WidthMode);
        Assert.Equal(100f, stub.Constraints[0].Height, LayoutFixture.Tolerance);
        Assert.Equal(MeasureMode.AtMost, stub.Constraints[0].HeightMode);
        LayoutFixture.AssertNoLayoutBits(root);
    }

    /// <summary>
    /// Creates the child <c>root_child0</c> with a recording stub of <see cref="MeasureStub.YogaModeTest"/>, and adds it
    /// to <paramref name="root"/>.
    /// </summary>
    /// <param name="root">The root element.</param>
    /// <returns>The stub of the child.</returns>
    private static MeasureStub AddMeasuredChild(Element root)
    {
        MeasureStub stub = new(MeasureStub.YogaModeTest);
        Element child = new() { Name = "root_child0" };
        child.Layout.Measure = stub.Callback;
        root.Children.Add(child);
        return stub;
    }
}
