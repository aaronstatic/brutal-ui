// Ported from Yoga (https://github.com/facebook/yoga), v3.2.1, tests/generated/YGPercentageTest.cpp. Copyright (c) Meta Platforms, Inc. and affiliates.
// MIT License. See THIRD-PARTY-NOTICES.md at the repository root.

using Xunit;

namespace BrutalUi.Core.Tests;

/// <summary>
/// The Yoga fixtures of <c>YGPercentageTest</c>, the LTR half only.
/// </summary>
/// <remarks>
/// Each Yoga fixture root sets <c>position: absolute</c>, and each port does the same.
/// The ports skip the disabled fixtures (<c>GTEST_SKIP</c>) and one fixture that needs the pixel rounding of Yoga.
/// The fixture ledger in <c>context.md</c> lists each skip.
/// </remarks>
public sealed class PercentageFixtureTests
{
    [Fact]
    public void PercentageWidthHeight()
    {
        // Yoga fixture: percentage_width_height
        Element root = new() { Name = "root" };
        root.Inline.FlexDirection = FlexDirection.Row;
        root.Inline.Position = PositionType.Absolute;
        root.Inline.Width = 200;
        root.Inline.Height = 200;

        Element child0 = AddChild(root, "root_child0");
        child0.Inline.Width = Length.Percent(30);
        child0.Inline.Height = Length.Percent(30);

        LayoutFixture.Calculate(root);

        LayoutFixture.AssertRect(root, 0, 0, 200, 200);
        LayoutFixture.AssertRect(child0, 0, 0, 60, 60);
        LayoutFixture.AssertNoLayoutBits(root);
    }

    [Fact]
    public void PercentagePositionLeftTop()
    {
        // Yoga fixture: percentage_position_left_top
        Element root = new() { Name = "root" };
        root.Inline.FlexDirection = FlexDirection.Row;
        root.Inline.Position = PositionType.Absolute;
        root.Inline.Width = 400;
        root.Inline.Height = 400;

        Element child0 = AddChild(root, "root_child0");
        child0.Inline.Left = Length.Percent(10);
        child0.Inline.Top = Length.Percent(20);
        child0.Inline.Width = Length.Percent(45);
        child0.Inline.Height = Length.Percent(55);

        LayoutFixture.Calculate(root);

        LayoutFixture.AssertRect(root, 0, 0, 400, 400);
        LayoutFixture.AssertRect(child0, 40, 80, 180, 220);
        LayoutFixture.AssertNoLayoutBits(root);
    }

    [Fact]
    public void PercentagePositionBottomRight()
    {
        // Yoga fixture: percentage_position_bottom_right
        Element root = new() { Name = "root" };
        root.Inline.FlexDirection = FlexDirection.Row;
        root.Inline.Position = PositionType.Absolute;
        root.Inline.Width = 500;
        root.Inline.Height = 500;

        Element child0 = AddChild(root, "root_child0");
        child0.Inline.Right = Length.Percent(20);
        child0.Inline.Bottom = Length.Percent(10);
        child0.Inline.Width = Length.Percent(55);
        child0.Inline.Height = Length.Percent(15);

        LayoutFixture.Calculate(root);

        LayoutFixture.AssertRect(root, 0, 0, 500, 500);
        LayoutFixture.AssertRect(child0, -100, -50, 275, 75);
        LayoutFixture.AssertNoLayoutBits(root);
    }

    [Fact]
    public void PercentageFlexBasis()
    {
        // Yoga fixture: percentage_flex_basis
        Element root = new() { Name = "root" };
        root.Inline.FlexDirection = FlexDirection.Row;
        root.Inline.Position = PositionType.Absolute;
        root.Inline.Width = 200;
        root.Inline.Height = 200;

        Element child0 = AddChild(root, "root_child0");
        child0.Inline.FlexGrow = 1;
        child0.Inline.FlexBasis = Length.Percent(50);

        Element child1 = AddChild(root, "root_child1");
        child1.Inline.FlexGrow = 1;
        child1.Inline.FlexBasis = Length.Percent(25);

        LayoutFixture.Calculate(root);

        LayoutFixture.AssertRect(root, 0, 0, 200, 200);
        LayoutFixture.AssertRect(child0, 0, 0, 125, 200);
        LayoutFixture.AssertRect(child1, 125, 0, 75, 200);
        LayoutFixture.AssertNoLayoutBits(root);
    }

    [Fact]
    public void PercentageFlexBasisCross()
    {
        // Yoga fixture: percentage_flex_basis_cross
        Element root = new() { Name = "root" };
        root.Inline.Position = PositionType.Absolute;
        root.Inline.Width = 200;
        root.Inline.Height = 200;

        Element child0 = AddChild(root, "root_child0");
        child0.Inline.FlexGrow = 1;
        child0.Inline.FlexBasis = Length.Percent(50);

        Element child1 = AddChild(root, "root_child1");
        child1.Inline.FlexGrow = 1;
        child1.Inline.FlexBasis = Length.Percent(25);

        LayoutFixture.Calculate(root);

        LayoutFixture.AssertRect(root, 0, 0, 200, 200);
        LayoutFixture.AssertRect(child0, 0, 0, 200, 125);
        LayoutFixture.AssertRect(child1, 0, 125, 200, 75);
        LayoutFixture.AssertNoLayoutBits(root);
    }

    [Fact]
    public void PercentageFlexBasisMainMaxHeight()
    {
        // Yoga fixture: percentage_flex_basis_main_max_height
        Element root = new() { Name = "root" };
        root.Inline.FlexDirection = FlexDirection.Row;
        root.Inline.Position = PositionType.Absolute;
        root.Inline.Width = 200;
        root.Inline.Height = 200;

        Element child0 = AddChild(root, "root_child0");
        child0.Inline.FlexGrow = 1;
        child0.Inline.FlexBasis = Length.Percent(10);
        child0.Inline.MaxHeight = Length.Percent(60);

        Element child1 = AddChild(root, "root_child1");
        child1.Inline.FlexGrow = 4;
        child1.Inline.FlexBasis = Length.Percent(10);
        child1.Inline.MaxHeight = Length.Percent(20);

        LayoutFixture.Calculate(root);

        LayoutFixture.AssertRect(root, 0, 0, 200, 200);
        LayoutFixture.AssertRect(child0, 0, 0, 52, 120);
        LayoutFixture.AssertRect(child1, 52, 0, 148, 40);
        LayoutFixture.AssertNoLayoutBits(root);
    }

    [Fact]
    public void PercentageFlexBasisCrossMaxHeight()
    {
        // Yoga fixture: percentage_flex_basis_cross_max_height
        Element root = new() { Name = "root" };
        root.Inline.Position = PositionType.Absolute;
        root.Inline.Width = 200;
        root.Inline.Height = 200;

        Element child0 = AddChild(root, "root_child0");
        child0.Inline.FlexGrow = 1;
        child0.Inline.FlexBasis = Length.Percent(10);
        child0.Inline.MaxHeight = Length.Percent(60);

        Element child1 = AddChild(root, "root_child1");
        child1.Inline.FlexGrow = 4;
        child1.Inline.FlexBasis = Length.Percent(10);
        child1.Inline.MaxHeight = Length.Percent(20);

        LayoutFixture.Calculate(root);

        LayoutFixture.AssertRect(root, 0, 0, 200, 200);
        LayoutFixture.AssertRect(child0, 0, 0, 200, 120);
        LayoutFixture.AssertRect(child1, 0, 120, 200, 40);
        LayoutFixture.AssertNoLayoutBits(root);
    }

    [Fact]
    public void PercentageFlexBasisMainMaxWidth()
    {
        // Yoga fixture: percentage_flex_basis_main_max_width
        Element root = new() { Name = "root" };
        root.Inline.FlexDirection = FlexDirection.Row;
        root.Inline.Position = PositionType.Absolute;
        root.Inline.Width = 200;
        root.Inline.Height = 200;

        Element child0 = AddChild(root, "root_child0");
        child0.Inline.FlexGrow = 1;
        child0.Inline.FlexBasis = Length.Percent(15);
        child0.Inline.MaxWidth = Length.Percent(60);

        Element child1 = AddChild(root, "root_child1");
        child1.Inline.FlexGrow = 4;
        child1.Inline.FlexBasis = Length.Percent(10);
        child1.Inline.MaxWidth = Length.Percent(20);

        LayoutFixture.Calculate(root);

        LayoutFixture.AssertRect(root, 0, 0, 200, 200);
        LayoutFixture.AssertRect(child0, 0, 0, 120, 200);
        LayoutFixture.AssertRect(child1, 120, 0, 40, 200);
        LayoutFixture.AssertNoLayoutBits(root);
    }

    [Fact]
    public void PercentageFlexBasisCrossMaxWidth()
    {
        // Yoga fixture: percentage_flex_basis_cross_max_width
        Element root = new() { Name = "root" };
        root.Inline.Position = PositionType.Absolute;
        root.Inline.Width = 200;
        root.Inline.Height = 200;

        Element child0 = AddChild(root, "root_child0");
        child0.Inline.FlexGrow = 1;
        child0.Inline.FlexBasis = Length.Percent(10);
        child0.Inline.MaxWidth = Length.Percent(60);

        Element child1 = AddChild(root, "root_child1");
        child1.Inline.FlexGrow = 4;
        child1.Inline.FlexBasis = Length.Percent(15);
        child1.Inline.MaxWidth = Length.Percent(20);

        LayoutFixture.Calculate(root);

        LayoutFixture.AssertRect(root, 0, 0, 200, 200);
        LayoutFixture.AssertRect(child0, 0, 0, 120, 50);
        LayoutFixture.AssertRect(child1, 0, 50, 40, 150);
        LayoutFixture.AssertNoLayoutBits(root);
    }

    [Fact]
    public void PercentageFlexBasisMainMinWidth()
    {
        // Yoga fixture: percentage_flex_basis_main_min_width
        Element root = new() { Name = "root" };
        root.Inline.FlexDirection = FlexDirection.Row;
        root.Inline.Position = PositionType.Absolute;
        root.Inline.Width = 200;
        root.Inline.Height = 200;

        Element child0 = AddChild(root, "root_child0");
        child0.Inline.FlexGrow = 1;
        child0.Inline.FlexBasis = Length.Percent(15);
        child0.Inline.MinWidth = Length.Percent(60);

        Element child1 = AddChild(root, "root_child1");
        child1.Inline.FlexGrow = 4;
        child1.Inline.FlexBasis = Length.Percent(10);
        child1.Inline.MinWidth = Length.Percent(20);

        LayoutFixture.Calculate(root);

        LayoutFixture.AssertRect(root, 0, 0, 200, 200);
        LayoutFixture.AssertRect(child0, 0, 0, 120, 200);
        LayoutFixture.AssertRect(child1, 120, 0, 80, 200);
        LayoutFixture.AssertNoLayoutBits(root);
    }

    [Fact]
    public void PercentageFlexBasisCrossMinWidth()
    {
        // Yoga fixture: percentage_flex_basis_cross_min_width
        Element root = new() { Name = "root" };
        root.Inline.Position = PositionType.Absolute;
        root.Inline.Width = 200;
        root.Inline.Height = 200;

        Element child0 = AddChild(root, "root_child0");
        child0.Inline.FlexGrow = 1;
        child0.Inline.FlexBasis = Length.Percent(10);
        child0.Inline.MinWidth = Length.Percent(60);

        Element child1 = AddChild(root, "root_child1");
        child1.Inline.FlexGrow = 4;
        child1.Inline.FlexBasis = Length.Percent(15);
        child1.Inline.MinWidth = Length.Percent(20);

        LayoutFixture.Calculate(root);

        LayoutFixture.AssertRect(root, 0, 0, 200, 200);
        LayoutFixture.AssertRect(child0, 0, 0, 200, 50);
        LayoutFixture.AssertRect(child1, 0, 50, 200, 150);
        LayoutFixture.AssertNoLayoutBits(root);
    }

    [Fact]
    public void PercentageMarginShouldCalculateBasedOnlyOnWidth()
    {
        // Yoga fixture: percentage_margin_should_calculate_based_only_on_width
        Element root = new() { Name = "root" };
        root.Inline.Position = PositionType.Absolute;
        root.Inline.Width = 200;
        root.Inline.Height = 100;

        Element child0 = AddChild(root, "root_child0");
        child0.Inline.FlexGrow = 1;
        child0.Inline.Margin = LengthEdges.All(Length.Percent(10));

        Element child0Child0 = AddChild(child0, "root_child0_child0");
        child0Child0.Inline.Width = 10;
        child0Child0.Inline.Height = 10;

        LayoutFixture.Calculate(root);

        LayoutFixture.AssertRect(root, 0, 0, 200, 100);
        LayoutFixture.AssertRect(child0, 20, 20, 160, 60);
        LayoutFixture.AssertRect(child0Child0, 0, 0, 10, 10);
        LayoutFixture.AssertNoLayoutBits(root);
    }

    [Fact]
    public void PercentagePaddingShouldCalculateBasedOnlyOnWidth()
    {
        // Yoga fixture: percentage_padding_should_calculate_based_only_on_width
        Element root = new() { Name = "root" };
        root.Inline.Position = PositionType.Absolute;
        root.Inline.Width = 200;
        root.Inline.Height = 100;

        Element child0 = AddChild(root, "root_child0");
        child0.Inline.FlexGrow = 1;
        child0.Inline.Padding = LengthEdges.All(Length.Percent(10));

        Element child0Child0 = AddChild(child0, "root_child0_child0");
        child0Child0.Inline.Width = 10;
        child0Child0.Inline.Height = 10;

        LayoutFixture.Calculate(root);

        LayoutFixture.AssertRect(root, 0, 0, 200, 100);
        LayoutFixture.AssertRect(child0, 0, 0, 200, 100);
        LayoutFixture.AssertRect(child0Child0, 20, 20, 10, 10);
        LayoutFixture.AssertNoLayoutBits(root);
    }

    [Fact]
    public void PercentageAbsolutePosition()
    {
        // Yoga fixture: percentage_absolute_position
        Element root = new() { Name = "root" };
        root.Inline.Position = PositionType.Absolute;
        root.Inline.Width = 200;
        root.Inline.Height = 100;

        Element child0 = AddChild(root, "root_child0");
        child0.Inline.Position = PositionType.Absolute;
        child0.Inline.Left = Length.Percent(30);
        child0.Inline.Top = Length.Percent(10);
        child0.Inline.Width = 10;
        child0.Inline.Height = 10;

        LayoutFixture.Calculate(root);

        LayoutFixture.AssertRect(root, 0, 0, 200, 100);
        LayoutFixture.AssertRect(child0, 60, 10, 10, 10);
        LayoutFixture.AssertNoLayoutBits(root);
    }

    [Fact]
    public void PercentageWidthHeightUndefinedParentSize()
    {
        // Yoga fixture: percentage_width_height_undefined_parent_size
        Element root = new() { Name = "root" };
        root.Inline.Position = PositionType.Absolute;

        Element child0 = AddChild(root, "root_child0");
        child0.Inline.Width = Length.Percent(50);
        child0.Inline.Height = Length.Percent(50);

        LayoutFixture.Calculate(root);

        LayoutFixture.AssertRect(root, 0, 0, 0, 0);
        LayoutFixture.AssertRect(child0, 0, 0, 0, 0);
        LayoutFixture.AssertNoLayoutBits(root);
    }

    [Fact]
    public void PercentWithinFlexGrow()
    {
        // Yoga fixture: percent_within_flex_grow
        Element root = new() { Name = "root" };
        root.Inline.FlexDirection = FlexDirection.Row;
        root.Inline.Position = PositionType.Absolute;
        root.Inline.Width = 350;
        root.Inline.Height = 100;

        Element child0 = AddChild(root, "root_child0");
        child0.Inline.Width = 100;

        Element child1 = AddChild(root, "root_child1");
        child1.Inline.FlexGrow = 1;

        Element child1Child0 = AddChild(child1, "root_child1_child0");
        child1Child0.Inline.Width = Length.Percent(100);

        Element child2 = AddChild(root, "root_child2");
        child2.Inline.Width = 100;

        LayoutFixture.Calculate(root);

        LayoutFixture.AssertRect(root, 0, 0, 350, 100);
        LayoutFixture.AssertRect(child0, 0, 0, 100, 100);
        LayoutFixture.AssertRect(child1, 100, 0, 150, 100);
        LayoutFixture.AssertRect(child1Child0, 0, 0, 150, 0);
        LayoutFixture.AssertRect(child2, 250, 0, 100, 100);
        LayoutFixture.AssertNoLayoutBits(root);
    }

    [Fact]
    public void PercentageContainerInWrappingContainer()
    {
        // Yoga fixture: percentage_container_in_wrapping_container
        Element root = new() { Name = "root" };
        root.Inline.JustifyContent = Justify.Center;
        root.Inline.AlignItems = Align.Center;
        root.Inline.Position = PositionType.Absolute;
        root.Inline.Width = 200;
        root.Inline.Height = 200;

        Element child0 = AddChild(root, "root_child0");

        Element child0Child0 = AddChild(child0, "root_child0_child0");
        child0Child0.Inline.FlexDirection = FlexDirection.Row;
        child0Child0.Inline.JustifyContent = Justify.Center;
        child0Child0.Inline.Width = Length.Percent(100);

        Element child0Child0Child0 = AddChild(child0Child0, "root_child0_child0_child0");
        child0Child0Child0.Inline.Width = 50;
        child0Child0Child0.Inline.Height = 50;

        Element child0Child0Child1 = AddChild(child0Child0, "root_child0_child0_child1");
        child0Child0Child1.Inline.Width = 50;
        child0Child0Child1.Inline.Height = 50;

        LayoutFixture.Calculate(root);

        LayoutFixture.AssertRect(root, 0, 0, 200, 200);
        LayoutFixture.AssertRect(child0, 50, 75, 100, 50);
        LayoutFixture.AssertRect(child0Child0, 0, 0, 100, 50);
        LayoutFixture.AssertRect(child0Child0Child0, 0, 0, 50, 50);
        LayoutFixture.AssertRect(child0Child0Child1, 50, 0, 50, 50);
        LayoutFixture.AssertNoLayoutBits(root);
    }

    [Fact]
    public void PercentAbsolutePosition()
    {
        // Yoga fixture: percent_absolute_position
        Element root = new() { Name = "root" };
        root.Inline.Position = PositionType.Absolute;
        root.Inline.Width = 60;
        root.Inline.Height = 50;

        Element child0 = AddChild(root, "root_child0");
        child0.Inline.FlexDirection = FlexDirection.Row;
        child0.Inline.Position = PositionType.Absolute;
        child0.Inline.Left = Length.Percent(50);
        child0.Inline.Width = Length.Percent(100);
        child0.Inline.Height = 50;

        Element child0Child0 = AddChild(child0, "root_child0_child0");
        child0Child0.Inline.Width = Length.Percent(100);

        Element child0Child1 = AddChild(child0, "root_child0_child1");
        child0Child1.Inline.Width = Length.Percent(100);

        LayoutFixture.Calculate(root);

        LayoutFixture.AssertRect(root, 0, 0, 60, 50);
        LayoutFixture.AssertRect(child0, 30, 0, 60, 50);
        LayoutFixture.AssertRect(child0Child0, 0, 0, 60, 50);
        LayoutFixture.AssertRect(child0Child1, 60, 0, 60, 50);
        LayoutFixture.AssertNoLayoutBits(root);
    }

    [Fact]
    public void PercentOfMinmaxMain()
    {
        // Yoga fixture: percent_of_minmax_main
        Element root = new() { Name = "root" };
        root.Inline.FlexDirection = FlexDirection.Row;
        root.Inline.Position = PositionType.Absolute;
        root.Inline.MinWidth = 60;
        root.Inline.MaxWidth = 60;
        root.Inline.Height = 50;

        Element child0 = AddChild(root, "root_child0");
        child0.Inline.Width = Length.Percent(50);
        child0.Inline.Height = 20;

        LayoutFixture.Calculate(root);

        LayoutFixture.AssertRect(root, 0, 0, 60, 50);
        LayoutFixture.AssertRect(child0, 0, 0, 30, 20);
        LayoutFixture.AssertNoLayoutBits(root);
    }

    [Fact]
    public void PercentOfMinmaxCrossStretched()
    {
        // Yoga fixture: percent_of_minmax_cross_stretched
        Element root = new() { Name = "root" };
        root.Inline.Position = PositionType.Absolute;
        root.Inline.MinWidth = 60;
        root.Inline.MaxWidth = 60;
        root.Inline.Height = 50;

        Element child0 = AddChild(root, "root_child0");
        child0.Inline.Width = Length.Percent(50);
        child0.Inline.Height = 20;

        LayoutFixture.Calculate(root);

        LayoutFixture.AssertRect(root, 0, 0, 60, 50);
        LayoutFixture.AssertRect(child0, 0, 0, 30, 20);
        LayoutFixture.AssertNoLayoutBits(root);
    }

    [Fact]
    public void PercentAbsoluteOfMinmaxCrossStretched()
    {
        // Yoga fixture: percent_absolute_of_minmax_cross_stretched
        Element root = new() { Name = "root" };
        root.Inline.Position = PositionType.Absolute;
        root.Inline.MinWidth = 60;
        root.Inline.MaxWidth = 60;
        root.Inline.Height = 50;

        Element child0 = AddChild(root, "root_child0");
        child0.Inline.Position = PositionType.Absolute;
        child0.Inline.Width = Length.Percent(50);
        child0.Inline.Height = 20;

        LayoutFixture.Calculate(root);

        LayoutFixture.AssertRect(root, 0, 0, 60, 50);
        LayoutFixture.AssertRect(child0, 0, 0, 30, 20);
        LayoutFixture.AssertNoLayoutBits(root);
    }

    [Fact]
    public void PercentOfMinmaxCrossUnstretched()
    {
        // Yoga fixture: percent_of_minmax_cross_unstretched
        Element root = new() { Name = "root" };
        root.Inline.AlignItems = Align.FlexStart;
        root.Inline.Position = PositionType.Absolute;
        root.Inline.MinWidth = 60;
        root.Inline.MaxWidth = 60;
        root.Inline.Height = 50;

        Element child0 = AddChild(root, "root_child0");
        child0.Inline.Width = Length.Percent(50);
        child0.Inline.Height = 20;

        LayoutFixture.Calculate(root);

        LayoutFixture.AssertRect(root, 0, 0, 60, 50);
        LayoutFixture.AssertRect(child0, 0, 0, 30, 20);
        LayoutFixture.AssertNoLayoutBits(root);
    }

    [Fact]
    public void PercentOfMaxCrossUnstretched()
    {
        // Yoga fixture: percent_of_max_cross_unstretched
        Element root = new() { Name = "root" };
        root.Inline.AlignItems = Align.FlexStart;
        root.Inline.Position = PositionType.Absolute;
        root.Inline.MaxWidth = 60;
        root.Inline.Height = 50;

        Element child0 = AddChild(root, "root_child0");
        child0.Inline.Width = Length.Percent(50);
        child0.Inline.Height = 20;

        LayoutFixture.Calculate(root);

        LayoutFixture.AssertRect(root, 0, 0, 0, 50);
        LayoutFixture.AssertRect(child0, 0, 0, 0, 20);
        LayoutFixture.AssertNoLayoutBits(root);
    }

    /// <summary>Creates a named child and adds it at the end of the children of <paramref name="parent"/>.</summary>
    /// <param name="parent">The parent element.</param>
    /// <param name="name">The name of the child, as in the Yoga fixture.</param>
    /// <returns>The new child.</returns>
    private static Element AddChild(Element parent, string name)
    {
        Element child = new() { Name = name };
        parent.Children.Add(child);
        return child;
    }
}
