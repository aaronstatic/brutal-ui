// Ported from Yoga (https://github.com/facebook/yoga), v3.2.1, tests/generated/YGMarginTest.cpp. Copyright (c) Meta Platforms, Inc. and affiliates.
// MIT License. See THIRD-PARTY-NOTICES.md at the repository root.

using Xunit;

namespace BrutalUi.Core.Tests;

/// <summary>
/// The Yoga fixtures of <c>YGMarginTest</c>, the LTR half only.
/// </summary>
/// <remarks>
/// <para>
/// Each Yoga fixture root sets <c>position: absolute</c>. The ports of this file were written before phase 5 added
/// the position type. For a root, the position type does not change the layout, so the ports omit that line.
/// </para>
/// <para>
/// This class ports the fixtures that need no start or end edge. It includes the 20 auto margin fixtures of
/// phase 6. The fixture ledger in <c>context.md</c> lists each skip.
/// </para>
/// </remarks>
public sealed class MarginFixtureTests
{
    [Fact]
    public void MarginTop()
    {
        // Yoga fixture: margin_top
        Element root = new() { Name = "root" };
        // Phase 5: root position type absolute. It does not change the layout of a root.
        root.Style.Width = 100;
        root.Style.Height = 100;

        Element child0 = AddChild(root, "root_child0");
        child0.Style.Margin.Top = 10;
        child0.Style.Height = 10;

        LayoutFixture.Calculate(root);

        LayoutFixture.AssertRect(root, 0, 0, 100, 100);
        LayoutFixture.AssertRect(child0, 0, 10, 100, 10);
        LayoutFixture.AssertNoLayoutBits(root);
    }

    [Fact]
    public void MarginBottom()
    {
        // Yoga fixture: margin_bottom
        Element root = new() { Name = "root" };
        root.Style.JustifyContent = Justify.FlexEnd;
        // Phase 5: root position type absolute. It does not change the layout of a root.
        root.Style.Width = 100;
        root.Style.Height = 100;

        Element child0 = AddChild(root, "root_child0");
        child0.Style.Margin.Bottom = 10;
        child0.Style.Height = 10;

        LayoutFixture.Calculate(root);

        LayoutFixture.AssertRect(root, 0, 0, 100, 100);
        LayoutFixture.AssertRect(child0, 0, 80, 100, 10);
        LayoutFixture.AssertNoLayoutBits(root);
    }

    [Fact]
    public void MarginAndFlexColumn()
    {
        // Yoga fixture: margin_and_flex_column
        Element root = new() { Name = "root" };
        // Phase 5: root position type absolute. It does not change the layout of a root.
        root.Style.Width = 100;
        root.Style.Height = 100;

        Element child0 = AddChild(root, "root_child0");
        child0.Style.FlexGrow = 1;
        child0.Style.Margin.Top = 10;
        child0.Style.Margin.Bottom = 10;

        LayoutFixture.Calculate(root);

        LayoutFixture.AssertRect(root, 0, 0, 100, 100);
        LayoutFixture.AssertRect(child0, 0, 10, 100, 80);
        LayoutFixture.AssertNoLayoutBits(root);
    }

    [Fact]
    public void MarginAndStretchRow()
    {
        // Yoga fixture: margin_and_stretch_row
        Element root = new() { Name = "root" };
        root.Style.FlexDirection = FlexDirection.Row;
        // Phase 5: root position type absolute. It does not change the layout of a root.
        root.Style.Width = 100;
        root.Style.Height = 100;

        Element child0 = AddChild(root, "root_child0");
        child0.Style.FlexGrow = 1;
        child0.Style.Margin.Top = 10;
        child0.Style.Margin.Bottom = 10;

        LayoutFixture.Calculate(root);

        LayoutFixture.AssertRect(root, 0, 0, 100, 100);
        LayoutFixture.AssertRect(child0, 0, 10, 100, 80);
        LayoutFixture.AssertNoLayoutBits(root);
    }

    [Fact]
    public void MarginWithSiblingColumn()
    {
        // Yoga fixture: margin_with_sibling_column
        Element root = new() { Name = "root" };
        // Phase 5: root position type absolute. It does not change the layout of a root.
        root.Style.Width = 100;
        root.Style.Height = 100;

        Element child0 = AddChild(root, "root_child0");
        child0.Style.FlexGrow = 1;
        child0.Style.Margin.Bottom = 10;

        Element child1 = AddChild(root, "root_child1");
        child1.Style.FlexGrow = 1;

        LayoutFixture.Calculate(root);

        LayoutFixture.AssertRect(root, 0, 0, 100, 100);
        LayoutFixture.AssertRect(child0, 0, 0, 100, 45);
        LayoutFixture.AssertRect(child1, 0, 55, 100, 45);
        LayoutFixture.AssertNoLayoutBits(root);
    }

    [Fact]
    public void MarginShouldNotBePartOfMaxHeight()
    {
        // Yoga fixture: margin_should_not_be_part_of_max_height
        Element root = new() { Name = "root" };
        // Phase 5: root position type absolute. It does not change the layout of a root.
        root.Style.Width = 250;
        root.Style.Height = 250;

        Element child0 = AddChild(root, "root_child0");
        child0.Style.Margin.Top = 20;
        child0.Style.Width = 100;
        child0.Style.Height = 100;
        child0.Style.MaxHeight = 100;

        LayoutFixture.Calculate(root);

        LayoutFixture.AssertRect(root, 0, 0, 250, 250);
        LayoutFixture.AssertRect(child0, 0, 20, 100, 100);
        LayoutFixture.AssertNoLayoutBits(root);
    }

    [Fact]
    public void MarginShouldNotBePartOfMaxWidth()
    {
        // Yoga fixture: margin_should_not_be_part_of_max_width
        Element root = new() { Name = "root" };
        // Phase 5: root position type absolute. It does not change the layout of a root.
        root.Style.Width = 250;
        root.Style.Height = 250;

        Element child0 = AddChild(root, "root_child0");
        child0.Style.Margin.Left = 20;
        child0.Style.Width = 100;
        child0.Style.MaxWidth = 100;
        child0.Style.Height = 100;

        LayoutFixture.Calculate(root);

        LayoutFixture.AssertRect(root, 0, 0, 250, 250);
        LayoutFixture.AssertRect(child0, 20, 0, 100, 100);
        LayoutFixture.AssertNoLayoutBits(root);
    }

    [Fact]
    public void MarginAutoBottom()
    {
        // Yoga fixture: margin_auto_bottom
        Element root = new() { Name = "root" };
        root.Style.AlignItems = Align.Center;
        // Phase 5: root position type absolute. It does not change the layout of a root.
        root.Style.Width = 200;
        root.Style.Height = 200;

        Element child0 = AddChild(root, "root_child0");
        child0.Style.Margin.Bottom = Length.Auto;
        child0.Style.Width = 50;
        child0.Style.Height = 50;

        Element child1 = AddChild(root, "root_child1");
        child1.Style.Width = 50;
        child1.Style.Height = 50;

        LayoutFixture.Calculate(root);

        LayoutFixture.AssertRect(root, 0, 0, 200, 200);
        LayoutFixture.AssertRect(child0, 75, 0, 50, 50);
        LayoutFixture.AssertRect(child1, 75, 150, 50, 50);
        LayoutFixture.AssertNoLayoutBits(root);
    }

    [Fact]
    public void MarginAutoTop()
    {
        // Yoga fixture: margin_auto_top
        Element root = new() { Name = "root" };
        root.Style.AlignItems = Align.Center;
        // Phase 5: root position type absolute. It does not change the layout of a root.
        root.Style.Width = 200;
        root.Style.Height = 200;

        Element child0 = AddChild(root, "root_child0");
        child0.Style.Margin.Top = Length.Auto;
        child0.Style.Width = 50;
        child0.Style.Height = 50;

        Element child1 = AddChild(root, "root_child1");
        child1.Style.Width = 50;
        child1.Style.Height = 50;

        LayoutFixture.Calculate(root);

        LayoutFixture.AssertRect(root, 0, 0, 200, 200);
        LayoutFixture.AssertRect(child0, 75, 100, 50, 50);
        LayoutFixture.AssertRect(child1, 75, 150, 50, 50);
        LayoutFixture.AssertNoLayoutBits(root);
    }

    [Fact]
    public void MarginAutoBottomAndTop()
    {
        // Yoga fixture: margin_auto_bottom_and_top
        Element root = new() { Name = "root" };
        root.Style.AlignItems = Align.Center;
        // Phase 5: root position type absolute. It does not change the layout of a root.
        root.Style.Width = 200;
        root.Style.Height = 200;

        Element child0 = AddChild(root, "root_child0");
        child0.Style.Margin.Top = Length.Auto;
        child0.Style.Margin.Bottom = Length.Auto;
        child0.Style.Width = 50;
        child0.Style.Height = 50;

        Element child1 = AddChild(root, "root_child1");
        child1.Style.Width = 50;
        child1.Style.Height = 50;

        LayoutFixture.Calculate(root);

        LayoutFixture.AssertRect(root, 0, 0, 200, 200);
        LayoutFixture.AssertRect(child0, 75, 50, 50, 50);
        LayoutFixture.AssertRect(child1, 75, 150, 50, 50);
        LayoutFixture.AssertNoLayoutBits(root);
    }

    [Fact]
    public void MarginAutoBottomAndTopJustifyCenter()
    {
        // Yoga fixture: margin_auto_bottom_and_top_justify_center
        Element root = new() { Name = "root" };
        root.Style.JustifyContent = Justify.Center;
        // Phase 5: root position type absolute. It does not change the layout of a root.
        root.Style.Width = 200;
        root.Style.Height = 200;

        Element child0 = AddChild(root, "root_child0");
        child0.Style.Margin.Top = Length.Auto;
        child0.Style.Margin.Bottom = Length.Auto;
        child0.Style.Width = 50;
        child0.Style.Height = 50;

        Element child1 = AddChild(root, "root_child1");
        child1.Style.Width = 50;
        child1.Style.Height = 50;

        LayoutFixture.Calculate(root);

        LayoutFixture.AssertRect(root, 0, 0, 200, 200);
        LayoutFixture.AssertRect(child0, 0, 50, 50, 50);
        LayoutFixture.AssertRect(child1, 0, 150, 50, 50);
        LayoutFixture.AssertNoLayoutBits(root);
    }

    [Fact]
    public void MarginAutoMultipleChildrenColumn()
    {
        // Yoga fixture: margin_auto_multiple_children_column
        Element root = new() { Name = "root" };
        root.Style.AlignItems = Align.Center;
        // Phase 5: root position type absolute. It does not change the layout of a root.
        root.Style.Width = 200;
        root.Style.Height = 200;

        Element child0 = AddChild(root, "root_child0");
        child0.Style.Margin.Top = Length.Auto;
        child0.Style.Width = 50;
        child0.Style.Height = 50;

        Element child1 = AddChild(root, "root_child1");
        child1.Style.Margin.Top = Length.Auto;
        child1.Style.Width = 50;
        child1.Style.Height = 50;

        Element child2 = AddChild(root, "root_child2");
        child2.Style.Width = 50;
        child2.Style.Height = 50;

        LayoutFixture.Calculate(root);

        LayoutFixture.AssertRect(root, 0, 0, 200, 200);
        LayoutFixture.AssertRect(child0, 75, 25, 50, 50);
        LayoutFixture.AssertRect(child1, 75, 100, 50, 50);
        LayoutFixture.AssertRect(child2, 75, 150, 50, 50);
        LayoutFixture.AssertNoLayoutBits(root);
    }

    [Fact]
    public void MarginAutoMultipleChildrenRow()
    {
        // Yoga fixture: margin_auto_multiple_children_row
        Element root = new() { Name = "root" };
        root.Style.FlexDirection = FlexDirection.Row;
        root.Style.AlignItems = Align.Center;
        // Phase 5: root position type absolute. It does not change the layout of a root.
        root.Style.Width = 200;
        root.Style.Height = 200;

        Element child0 = AddChild(root, "root_child0");
        child0.Style.Margin.Right = Length.Auto;
        child0.Style.Width = 50;
        child0.Style.Height = 50;

        Element child1 = AddChild(root, "root_child1");
        child1.Style.Margin.Right = Length.Auto;
        child1.Style.Width = 50;
        child1.Style.Height = 50;

        Element child2 = AddChild(root, "root_child2");
        child2.Style.Width = 50;
        child2.Style.Height = 50;

        LayoutFixture.Calculate(root);

        LayoutFixture.AssertRect(root, 0, 0, 200, 200);
        LayoutFixture.AssertRect(child0, 0, 75, 50, 50);
        LayoutFixture.AssertRect(child1, 75, 75, 50, 50);
        LayoutFixture.AssertRect(child2, 150, 75, 50, 50);
        LayoutFixture.AssertNoLayoutBits(root);
    }

    [Fact]
    public void MarginAutoLeftAndRightColumn()
    {
        // Yoga fixture: margin_auto_left_and_right_column
        Element root = new() { Name = "root" };
        root.Style.FlexDirection = FlexDirection.Row;
        root.Style.AlignItems = Align.Center;
        // Phase 5: root position type absolute. It does not change the layout of a root.
        root.Style.Width = 200;
        root.Style.Height = 200;

        Element child0 = AddChild(root, "root_child0");
        child0.Style.Margin.Left = Length.Auto;
        child0.Style.Margin.Right = Length.Auto;
        child0.Style.Width = 50;
        child0.Style.Height = 50;

        Element child1 = AddChild(root, "root_child1");
        child1.Style.Width = 50;
        child1.Style.Height = 50;

        LayoutFixture.Calculate(root);

        LayoutFixture.AssertRect(root, 0, 0, 200, 200);
        LayoutFixture.AssertRect(child0, 50, 75, 50, 50);
        LayoutFixture.AssertRect(child1, 150, 75, 50, 50);
        LayoutFixture.AssertNoLayoutBits(root);
    }

    [Fact]
    public void MarginAutoLeftAndRight()
    {
        // Yoga fixture: margin_auto_left_and_right
        Element root = new() { Name = "root" };
        // Phase 5: root position type absolute. It does not change the layout of a root.
        root.Style.Width = 200;
        root.Style.Height = 200;

        Element child0 = AddChild(root, "root_child0");
        child0.Style.Margin.Left = Length.Auto;
        child0.Style.Margin.Right = Length.Auto;
        child0.Style.Width = 50;
        child0.Style.Height = 50;

        Element child1 = AddChild(root, "root_child1");
        child1.Style.Width = 50;
        child1.Style.Height = 50;

        LayoutFixture.Calculate(root);

        LayoutFixture.AssertRect(root, 0, 0, 200, 200);
        LayoutFixture.AssertRect(child0, 75, 0, 50, 50);
        LayoutFixture.AssertRect(child1, 0, 50, 50, 50);
        LayoutFixture.AssertNoLayoutBits(root);
    }

    [Fact]
    public void MarginAutoLeftAndRightColumnAndCenter()
    {
        // Yoga fixture: margin_auto_left_and_right_column_and_center
        Element root = new() { Name = "root" };
        root.Style.AlignItems = Align.Center;
        // Phase 5: root position type absolute. It does not change the layout of a root.
        root.Style.Width = 200;
        root.Style.Height = 200;

        Element child0 = AddChild(root, "root_child0");
        child0.Style.Margin.Left = Length.Auto;
        child0.Style.Margin.Right = Length.Auto;
        child0.Style.Width = 50;
        child0.Style.Height = 50;

        Element child1 = AddChild(root, "root_child1");
        child1.Style.Width = 50;
        child1.Style.Height = 50;

        LayoutFixture.Calculate(root);

        LayoutFixture.AssertRect(root, 0, 0, 200, 200);
        LayoutFixture.AssertRect(child0, 75, 0, 50, 50);
        LayoutFixture.AssertRect(child1, 75, 50, 50, 50);
        LayoutFixture.AssertNoLayoutBits(root);
    }

    [Fact]
    public void MarginAutoLeft()
    {
        // Yoga fixture: margin_auto_left
        Element root = new() { Name = "root" };
        root.Style.AlignItems = Align.Center;
        // Phase 5: root position type absolute. It does not change the layout of a root.
        root.Style.Width = 200;
        root.Style.Height = 200;

        Element child0 = AddChild(root, "root_child0");
        child0.Style.Margin.Left = Length.Auto;
        child0.Style.Width = 50;
        child0.Style.Height = 50;

        Element child1 = AddChild(root, "root_child1");
        child1.Style.Width = 50;
        child1.Style.Height = 50;

        LayoutFixture.Calculate(root);

        LayoutFixture.AssertRect(root, 0, 0, 200, 200);
        LayoutFixture.AssertRect(child0, 150, 0, 50, 50);
        LayoutFixture.AssertRect(child1, 75, 50, 50, 50);
        LayoutFixture.AssertNoLayoutBits(root);
    }

    [Fact]
    public void MarginAutoRight()
    {
        // Yoga fixture: margin_auto_right
        Element root = new() { Name = "root" };
        root.Style.AlignItems = Align.Center;
        // Phase 5: root position type absolute. It does not change the layout of a root.
        root.Style.Width = 200;
        root.Style.Height = 200;

        Element child0 = AddChild(root, "root_child0");
        child0.Style.Margin.Right = Length.Auto;
        child0.Style.Width = 50;
        child0.Style.Height = 50;

        Element child1 = AddChild(root, "root_child1");
        child1.Style.Width = 50;
        child1.Style.Height = 50;

        LayoutFixture.Calculate(root);

        LayoutFixture.AssertRect(root, 0, 0, 200, 200);
        LayoutFixture.AssertRect(child0, 0, 0, 50, 50);
        LayoutFixture.AssertRect(child1, 75, 50, 50, 50);
        LayoutFixture.AssertNoLayoutBits(root);
    }

    [Fact]
    public void MarginAutoLeftAndRightStretch()
    {
        // Yoga fixture: margin_auto_left_and_right_stretch
        Element root = new() { Name = "root" };
        root.Style.FlexDirection = FlexDirection.Row;
        // Phase 5: root position type absolute. It does not change the layout of a root.
        root.Style.Width = 200;
        root.Style.Height = 200;

        Element child0 = AddChild(root, "root_child0");
        child0.Style.Margin.Left = Length.Auto;
        child0.Style.Margin.Right = Length.Auto;
        child0.Style.Width = 50;
        child0.Style.Height = 50;

        Element child1 = AddChild(root, "root_child1");
        child1.Style.Width = 50;
        child1.Style.Height = 50;

        LayoutFixture.Calculate(root);

        LayoutFixture.AssertRect(root, 0, 0, 200, 200);
        LayoutFixture.AssertRect(child0, 50, 0, 50, 50);
        LayoutFixture.AssertRect(child1, 150, 0, 50, 50);
        LayoutFixture.AssertNoLayoutBits(root);
    }

    [Fact]
    public void MarginAutoTopAndBottomStretch()
    {
        // Yoga fixture: margin_auto_top_and_bottom_stretch
        Element root = new() { Name = "root" };
        // Phase 5: root position type absolute. It does not change the layout of a root.
        root.Style.Width = 200;
        root.Style.Height = 200;

        Element child0 = AddChild(root, "root_child0");
        child0.Style.Margin.Top = Length.Auto;
        child0.Style.Margin.Bottom = Length.Auto;
        child0.Style.Width = 50;
        child0.Style.Height = 50;

        Element child1 = AddChild(root, "root_child1");
        child1.Style.Width = 50;
        child1.Style.Height = 50;

        LayoutFixture.Calculate(root);

        LayoutFixture.AssertRect(root, 0, 0, 200, 200);
        LayoutFixture.AssertRect(child0, 0, 50, 50, 50);
        LayoutFixture.AssertRect(child1, 0, 150, 50, 50);
        LayoutFixture.AssertNoLayoutBits(root);
    }

    [Fact]
    public void MarginAutoLeftRightChildBiggerThanParent()
    {
        // Yoga fixture: margin_auto_left_right_child_bigger_than_parent
        Element root = new() { Name = "root" };
        root.Style.JustifyContent = Justify.Center;
        // Phase 5: root position type absolute. It does not change the layout of a root.
        root.Style.Width = 52;
        root.Style.Height = 52;

        Element child0 = AddChild(root, "root_child0");
        child0.Style.Margin.Left = Length.Auto;
        child0.Style.Margin.Right = Length.Auto;
        child0.Style.Width = 72;
        child0.Style.Height = 72;

        LayoutFixture.Calculate(root);

        LayoutFixture.AssertRect(root, 0, 0, 52, 52);
        LayoutFixture.AssertRect(child0, 0, -10, 72, 72);
        LayoutFixture.AssertNoLayoutBits(root);
    }

    [Fact]
    public void MarginAutoLeftChildBiggerThanParent()
    {
        // Yoga fixture: margin_auto_left_child_bigger_than_parent
        Element root = new() { Name = "root" };
        root.Style.JustifyContent = Justify.Center;
        // Phase 5: root position type absolute. It does not change the layout of a root.
        root.Style.Width = 52;
        root.Style.Height = 52;

        Element child0 = AddChild(root, "root_child0");
        child0.Style.Margin.Left = Length.Auto;
        child0.Style.Width = 72;
        child0.Style.Height = 72;

        LayoutFixture.Calculate(root);

        LayoutFixture.AssertRect(root, 0, 0, 52, 52);
        LayoutFixture.AssertRect(child0, 0, -10, 72, 72);
        LayoutFixture.AssertNoLayoutBits(root);
    }

    [Fact]
    public void MarginFixLeftAutoRightChildBiggerThanParent()
    {
        // Yoga fixture: margin_fix_left_auto_right_child_bigger_than_parent
        Element root = new() { Name = "root" };
        root.Style.JustifyContent = Justify.Center;
        // Phase 5: root position type absolute. It does not change the layout of a root.
        root.Style.Width = 52;
        root.Style.Height = 52;

        Element child0 = AddChild(root, "root_child0");
        child0.Style.Margin.Left = 10;
        child0.Style.Margin.Right = Length.Auto;
        child0.Style.Width = 72;
        child0.Style.Height = 72;

        LayoutFixture.Calculate(root);

        LayoutFixture.AssertRect(root, 0, 0, 52, 52);
        LayoutFixture.AssertRect(child0, 10, -10, 72, 72);
        LayoutFixture.AssertNoLayoutBits(root);
    }

    [Fact]
    public void MarginAutoLeftFixRightChildBiggerThanParent()
    {
        // Yoga fixture: margin_auto_left_fix_right_child_bigger_than_parent
        Element root = new() { Name = "root" };
        root.Style.JustifyContent = Justify.Center;
        // Phase 5: root position type absolute. It does not change the layout of a root.
        root.Style.Width = 52;
        root.Style.Height = 52;

        Element child0 = AddChild(root, "root_child0");
        child0.Style.Margin.Left = Length.Auto;
        child0.Style.Margin.Right = 10;
        child0.Style.Width = 72;
        child0.Style.Height = 72;

        LayoutFixture.Calculate(root);

        LayoutFixture.AssertRect(root, 0, 0, 52, 52);
        LayoutFixture.AssertRect(child0, 0, -10, 72, 72);
        LayoutFixture.AssertNoLayoutBits(root);
    }

    [Fact]
    public void MarginAutoTopStretchingChild()
    {
        // Yoga fixture: margin_auto_top_stretching_child
        Element root = new() { Name = "root" };
        root.Style.AlignItems = Align.Center;
        // Phase 5: root position type absolute. It does not change the layout of a root.
        root.Style.Width = 200;
        root.Style.Height = 200;

        Element child0 = AddChild(root, "root_child0");
        child0.Style.FlexGrow = 1;
        child0.Style.FlexShrink = 1;
        child0.Style.FlexBasis = Length.Percent(0);
        child0.Style.Margin.Top = Length.Auto;

        Element child1 = AddChild(root, "root_child1");
        child1.Style.Width = 50;
        child1.Style.Height = 50;

        LayoutFixture.Calculate(root);

        LayoutFixture.AssertRect(root, 0, 0, 200, 200);
        LayoutFixture.AssertRect(child0, 100, 0, 0, 150);
        LayoutFixture.AssertRect(child1, 75, 150, 50, 50);
        LayoutFixture.AssertNoLayoutBits(root);
    }

    [Fact]
    public void MarginAutoLeftStretchingChild()
    {
        // Yoga fixture: margin_auto_left_stretching_child
        Element root = new() { Name = "root" };
        root.Style.AlignItems = Align.Center;
        // Phase 5: root position type absolute. It does not change the layout of a root.
        root.Style.Width = 200;
        root.Style.Height = 200;

        Element child0 = AddChild(root, "root_child0");
        child0.Style.FlexGrow = 1;
        child0.Style.FlexShrink = 1;
        child0.Style.FlexBasis = Length.Percent(0);
        child0.Style.Margin.Left = Length.Auto;

        Element child1 = AddChild(root, "root_child1");
        child1.Style.Width = 50;
        child1.Style.Height = 50;

        LayoutFixture.Calculate(root);

        LayoutFixture.AssertRect(root, 0, 0, 200, 200);
        LayoutFixture.AssertRect(child0, 200, 0, 0, 150);
        LayoutFixture.AssertRect(child1, 75, 150, 50, 50);
        LayoutFixture.AssertNoLayoutBits(root);
    }

    [Fact]
    public void MarginAutoOverflowingContainer()
    {
        // Yoga fixture: margin_auto_overflowing_container
        Element root = new() { Name = "root" };
        root.Style.AlignItems = Align.Center;
        // Phase 5: root position type absolute. It does not change the layout of a root.
        root.Style.Width = 200;
        root.Style.Height = 200;

        Element child0 = AddChild(root, "root_child0");
        child0.Style.Margin.Bottom = Length.Auto;
        child0.Style.Width = 50;
        child0.Style.Height = 150;

        Element child1 = AddChild(root, "root_child1");
        child1.Style.Width = 50;
        child1.Style.Height = 150;

        LayoutFixture.Calculate(root);

        LayoutFixture.AssertRect(root, 0, 0, 200, 200);
        LayoutFixture.AssertRect(child0, 75, 0, 50, 150);
        LayoutFixture.AssertRect(child1, 75, 150, 50, 150);
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
