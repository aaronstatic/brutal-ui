// Ported from Yoga (https://github.com/facebook/yoga), v3.2.1, tests/generated/YGAbsolutePositionTest.cpp. Copyright (c) Meta Platforms, Inc. and affiliates.
// MIT License. See THIRD-PARTY-NOTICES.md at the repository root.

using Xunit;

namespace BrutalUi.Core.Tests;

/// <summary>
/// The Yoga fixtures of <c>YGAbsolutePositionTest</c>, the LTR half only.
/// </summary>
/// <remarks>
/// Each Yoga fixture root sets <c>position: absolute</c>, and each port does the same.
/// The ports skip the fixtures with a start or end edge or <c>position: static</c>.
/// The fixture ledger in <c>context.md</c> lists each skip.
/// </remarks>
public sealed class AbsolutePositionFixtureTests
{
    [Fact]
    public void AbsoluteLayoutWidthHeightLeftAutoRight()
    {
        // Yoga fixture: absolute_layout_width_height_left_auto_right
        Element root = new() { Name = "root" };
        root.Inline.Position = PositionType.Absolute;
        root.Inline.Width = 100;
        root.Inline.Height = 100;

        Element child0 = AddChild(root, "root_child0");
        child0.Inline.Position = PositionType.Absolute;
        child0.Inline.Left = Length.Auto;
        child0.Inline.Right = 10;
        child0.Inline.Width = 10;
        child0.Inline.Height = 10;

        LayoutFixture.Calculate(root);

        LayoutFixture.AssertRect(root, 0, 0, 100, 100);
        LayoutFixture.AssertRect(child0, 80, 0, 10, 10);
        LayoutFixture.AssertNoLayoutBits(root);
    }

    [Fact]
    public void AbsoluteLayoutWidthHeightLeftRightAuto()
    {
        // Yoga fixture: absolute_layout_width_height_left_right_auto
        Element root = new() { Name = "root" };
        root.Inline.Position = PositionType.Absolute;
        root.Inline.Width = 100;
        root.Inline.Height = 100;

        Element child0 = AddChild(root, "root_child0");
        child0.Inline.Position = PositionType.Absolute;
        child0.Inline.Left = 10;
        child0.Inline.Right = Length.Auto;
        child0.Inline.Width = 10;
        child0.Inline.Height = 10;

        LayoutFixture.Calculate(root);

        LayoutFixture.AssertRect(root, 0, 0, 100, 100);
        LayoutFixture.AssertRect(child0, 10, 0, 10, 10);
        LayoutFixture.AssertNoLayoutBits(root);
    }

    [Fact]
    public void AbsoluteLayoutWidthHeightLeftAutoRightAuto()
    {
        // Yoga fixture: absolute_layout_width_height_left_auto_right_auto
        Element root = new() { Name = "root" };
        root.Inline.Position = PositionType.Absolute;
        root.Inline.Width = 100;
        root.Inline.Height = 100;

        Element child0 = AddChild(root, "root_child0");
        child0.Inline.Position = PositionType.Absolute;
        child0.Inline.Left = Length.Auto;
        child0.Inline.Right = Length.Auto;
        child0.Inline.Width = 10;
        child0.Inline.Height = 10;

        LayoutFixture.Calculate(root);

        LayoutFixture.AssertRect(root, 0, 0, 100, 100);
        LayoutFixture.AssertRect(child0, 0, 0, 10, 10);
        LayoutFixture.AssertNoLayoutBits(root);
    }

    [Fact]
    public void AbsoluteLayoutWithinBorder()
    {
        // Yoga fixture: absolute_layout_within_border
        Element root = new() { Name = "root" };
        root.Inline.Position = PositionType.Absolute;
        root.Inline.Margin = LengthEdges.All(10);
        root.Inline.Padding = LengthEdges.All(10);
        root.Inline.BorderWidth = LengthEdges.All(10);
        root.Inline.Width = 100;
        root.Inline.Height = 100;

        Element child0 = AddChild(root, "root_child0");
        child0.Inline.Position = PositionType.Absolute;
        child0.Inline.Left = 0;
        child0.Inline.Top = 0;
        child0.Inline.Width = 50;
        child0.Inline.Height = 50;

        Element child1 = AddChild(root, "root_child1");
        child1.Inline.Position = PositionType.Absolute;
        child1.Inline.Right = 0;
        child1.Inline.Bottom = 0;
        child1.Inline.Width = 50;
        child1.Inline.Height = 50;

        Element child2 = AddChild(root, "root_child2");
        child2.Inline.Position = PositionType.Absolute;
        child2.Inline.Left = 0;
        child2.Inline.Top = 0;
        child2.Inline.Margin = LengthEdges.All(10);
        child2.Inline.Width = 50;
        child2.Inline.Height = 50;

        Element child3 = AddChild(root, "root_child3");
        child3.Inline.Position = PositionType.Absolute;
        child3.Inline.Right = 0;
        child3.Inline.Bottom = 0;
        child3.Inline.Margin = LengthEdges.All(10);
        child3.Inline.Width = 50;
        child3.Inline.Height = 50;

        LayoutFixture.Calculate(root);

        LayoutFixture.AssertRect(root, 10, 10, 100, 100);
        LayoutFixture.AssertRect(child0, 10, 10, 50, 50);
        LayoutFixture.AssertRect(child1, 40, 40, 50, 50);
        LayoutFixture.AssertRect(child2, 20, 20, 50, 50);
        LayoutFixture.AssertRect(child3, 30, 30, 50, 50);
        LayoutFixture.AssertNoLayoutBits(root);
    }

    [Fact]
    public void AbsoluteLayoutAlignItemsAndJustifyContentCenter()
    {
        // Yoga fixture: absolute_layout_align_items_and_justify_content_center
        Element root = new() { Name = "root" };
        root.Inline.JustifyContent = Justify.Center;
        root.Inline.AlignItems = Align.Center;
        root.Inline.Position = PositionType.Absolute;
        root.Inline.FlexGrow = 1;
        root.Inline.Width = 110;
        root.Inline.Height = 100;

        Element child0 = AddChild(root, "root_child0");
        child0.Inline.Position = PositionType.Absolute;
        child0.Inline.Width = 60;
        child0.Inline.Height = 40;

        LayoutFixture.Calculate(root);

        LayoutFixture.AssertRect(root, 0, 0, 110, 100);
        LayoutFixture.AssertRect(child0, 25, 30, 60, 40);
        LayoutFixture.AssertNoLayoutBits(root);
    }

    [Fact]
    public void AbsoluteLayoutAlignItemsAndJustifyContentFlexEnd()
    {
        // Yoga fixture: absolute_layout_align_items_and_justify_content_flex_end
        Element root = new() { Name = "root" };
        root.Inline.JustifyContent = Justify.FlexEnd;
        root.Inline.AlignItems = Align.FlexEnd;
        root.Inline.Position = PositionType.Absolute;
        root.Inline.FlexGrow = 1;
        root.Inline.Width = 110;
        root.Inline.Height = 100;

        Element child0 = AddChild(root, "root_child0");
        child0.Inline.Position = PositionType.Absolute;
        child0.Inline.Width = 60;
        child0.Inline.Height = 40;

        LayoutFixture.Calculate(root);

        LayoutFixture.AssertRect(root, 0, 0, 110, 100);
        LayoutFixture.AssertRect(child0, 50, 60, 60, 40);
        LayoutFixture.AssertNoLayoutBits(root);
    }

    [Fact]
    public void AbsoluteLayoutJustifyContentCenter()
    {
        // Yoga fixture: absolute_layout_justify_content_center
        Element root = new() { Name = "root" };
        root.Inline.JustifyContent = Justify.Center;
        root.Inline.Position = PositionType.Absolute;
        root.Inline.FlexGrow = 1;
        root.Inline.Width = 110;
        root.Inline.Height = 100;

        Element child0 = AddChild(root, "root_child0");
        child0.Inline.Position = PositionType.Absolute;
        child0.Inline.Width = 60;
        child0.Inline.Height = 40;

        LayoutFixture.Calculate(root);

        LayoutFixture.AssertRect(root, 0, 0, 110, 100);
        LayoutFixture.AssertRect(child0, 0, 30, 60, 40);
        LayoutFixture.AssertNoLayoutBits(root);
    }

    [Fact]
    public void AbsoluteLayoutAlignItemsCenter()
    {
        // Yoga fixture: absolute_layout_align_items_center
        Element root = new() { Name = "root" };
        root.Inline.AlignItems = Align.Center;
        root.Inline.Position = PositionType.Absolute;
        root.Inline.FlexGrow = 1;
        root.Inline.Width = 110;
        root.Inline.Height = 100;

        Element child0 = AddChild(root, "root_child0");
        child0.Inline.Position = PositionType.Absolute;
        child0.Inline.Width = 60;
        child0.Inline.Height = 40;

        LayoutFixture.Calculate(root);

        LayoutFixture.AssertRect(root, 0, 0, 110, 100);
        LayoutFixture.AssertRect(child0, 25, 0, 60, 40);
        LayoutFixture.AssertNoLayoutBits(root);
    }

    [Fact]
    public void AbsoluteLayoutAlignItemsCenterOnChildOnly()
    {
        // Yoga fixture: absolute_layout_align_items_center_on_child_only
        Element root = new() { Name = "root" };
        root.Inline.Position = PositionType.Absolute;
        root.Inline.FlexGrow = 1;
        root.Inline.Width = 110;
        root.Inline.Height = 100;

        Element child0 = AddChild(root, "root_child0");
        child0.Inline.AlignSelf = Align.Center;
        child0.Inline.Position = PositionType.Absolute;
        child0.Inline.Width = 60;
        child0.Inline.Height = 40;

        LayoutFixture.Calculate(root);

        LayoutFixture.AssertRect(root, 0, 0, 110, 100);
        LayoutFixture.AssertRect(child0, 25, 0, 60, 40);
        LayoutFixture.AssertNoLayoutBits(root);
    }

    [Fact]
    public void AbsoluteLayoutAlignItemsAndJustifyContentCenterAndTopPosition()
    {
        // Yoga fixture: absolute_layout_align_items_and_justify_content_center_and_top_position
        Element root = new() { Name = "root" };
        root.Inline.JustifyContent = Justify.Center;
        root.Inline.AlignItems = Align.Center;
        root.Inline.Position = PositionType.Absolute;
        root.Inline.FlexGrow = 1;
        root.Inline.Width = 110;
        root.Inline.Height = 100;

        Element child0 = AddChild(root, "root_child0");
        child0.Inline.Position = PositionType.Absolute;
        child0.Inline.Top = 10;
        child0.Inline.Width = 60;
        child0.Inline.Height = 40;

        LayoutFixture.Calculate(root);

        LayoutFixture.AssertRect(root, 0, 0, 110, 100);
        LayoutFixture.AssertRect(child0, 25, 10, 60, 40);
        LayoutFixture.AssertNoLayoutBits(root);
    }

    [Fact]
    public void AbsoluteLayoutAlignItemsAndJustifyContentCenterAndBottomPosition()
    {
        // Yoga fixture: absolute_layout_align_items_and_justify_content_center_and_bottom_position
        Element root = new() { Name = "root" };
        root.Inline.JustifyContent = Justify.Center;
        root.Inline.AlignItems = Align.Center;
        root.Inline.Position = PositionType.Absolute;
        root.Inline.FlexGrow = 1;
        root.Inline.Width = 110;
        root.Inline.Height = 100;

        Element child0 = AddChild(root, "root_child0");
        child0.Inline.Position = PositionType.Absolute;
        child0.Inline.Bottom = 10;
        child0.Inline.Width = 60;
        child0.Inline.Height = 40;

        LayoutFixture.Calculate(root);

        LayoutFixture.AssertRect(root, 0, 0, 110, 100);
        LayoutFixture.AssertRect(child0, 25, 50, 60, 40);
        LayoutFixture.AssertNoLayoutBits(root);
    }

    [Fact]
    public void AbsoluteLayoutAlignItemsAndJustifyContentCenterAndLeftPosition()
    {
        // Yoga fixture: absolute_layout_align_items_and_justify_content_center_and_left_position
        Element root = new() { Name = "root" };
        root.Inline.JustifyContent = Justify.Center;
        root.Inline.AlignItems = Align.Center;
        root.Inline.Position = PositionType.Absolute;
        root.Inline.FlexGrow = 1;
        root.Inline.Width = 110;
        root.Inline.Height = 100;

        Element child0 = AddChild(root, "root_child0");
        child0.Inline.Position = PositionType.Absolute;
        child0.Inline.Left = 5;
        child0.Inline.Width = 60;
        child0.Inline.Height = 40;

        LayoutFixture.Calculate(root);

        LayoutFixture.AssertRect(root, 0, 0, 110, 100);
        LayoutFixture.AssertRect(child0, 5, 30, 60, 40);
        LayoutFixture.AssertNoLayoutBits(root);
    }

    [Fact]
    public void AbsoluteLayoutAlignItemsAndJustifyContentCenterAndRightPosition()
    {
        // Yoga fixture: absolute_layout_align_items_and_justify_content_center_and_right_position
        Element root = new() { Name = "root" };
        root.Inline.JustifyContent = Justify.Center;
        root.Inline.AlignItems = Align.Center;
        root.Inline.Position = PositionType.Absolute;
        root.Inline.FlexGrow = 1;
        root.Inline.Width = 110;
        root.Inline.Height = 100;

        Element child0 = AddChild(root, "root_child0");
        child0.Inline.Position = PositionType.Absolute;
        child0.Inline.Right = 5;
        child0.Inline.Width = 60;
        child0.Inline.Height = 40;

        LayoutFixture.Calculate(root);

        LayoutFixture.AssertRect(root, 0, 0, 110, 100);
        LayoutFixture.AssertRect(child0, 45, 30, 60, 40);
        LayoutFixture.AssertNoLayoutBits(root);
    }

    [Fact]
    public void PositionRootWithRtlShouldPositionWithoutdirection()
    {
        // Yoga fixture: position_root_with_rtl_should_position_withoutdirection
        Element root = new() { Name = "root" };
        root.Inline.Position = PositionType.Absolute;
        root.Inline.Left = 72;
        root.Inline.Width = 52;
        root.Inline.Height = 52;

        LayoutFixture.Calculate(root);

        LayoutFixture.AssertRect(root, 72, 0, 52, 52);
        LayoutFixture.AssertNoLayoutBits(root);
    }

    [Fact]
    public void AbsoluteLayoutPaddingLeft()
    {
        // Yoga fixture: absolute_layout_padding_left
        Element root = new() { Name = "root" };
        root.Inline.Position = PositionType.Absolute;
        root.Inline.Padding = root.Inline.Padding with { Left = 100 };
        root.Inline.Width = 200;
        root.Inline.Height = 200;

        Element child0 = AddChild(root, "root_child0");
        child0.Inline.Position = PositionType.Absolute;
        child0.Inline.Width = 50;
        child0.Inline.Height = 50;

        LayoutFixture.Calculate(root);

        LayoutFixture.AssertRect(root, 0, 0, 200, 200);
        LayoutFixture.AssertRect(child0, 100, 0, 50, 50);
        LayoutFixture.AssertNoLayoutBits(root);
    }

    [Fact]
    public void AbsoluteLayoutPaddingRight()
    {
        // Yoga fixture: absolute_layout_padding_right
        Element root = new() { Name = "root" };
        root.Inline.Position = PositionType.Absolute;
        root.Inline.Padding = root.Inline.Padding with { Right = 100 };
        root.Inline.Width = 200;
        root.Inline.Height = 200;

        Element child0 = AddChild(root, "root_child0");
        child0.Inline.Position = PositionType.Absolute;
        child0.Inline.Width = 50;
        child0.Inline.Height = 50;

        LayoutFixture.Calculate(root);

        LayoutFixture.AssertRect(root, 0, 0, 200, 200);
        LayoutFixture.AssertRect(child0, 0, 0, 50, 50);
        LayoutFixture.AssertNoLayoutBits(root);
    }

    [Fact]
    public void AbsoluteLayoutPaddingTop()
    {
        // Yoga fixture: absolute_layout_padding_top
        Element root = new() { Name = "root" };
        root.Inline.Position = PositionType.Absolute;
        root.Inline.Padding = root.Inline.Padding with { Top = 100 };
        root.Inline.Width = 200;
        root.Inline.Height = 200;

        Element child0 = AddChild(root, "root_child0");
        child0.Inline.Position = PositionType.Absolute;
        child0.Inline.Width = 50;
        child0.Inline.Height = 50;

        LayoutFixture.Calculate(root);

        LayoutFixture.AssertRect(root, 0, 0, 200, 200);
        LayoutFixture.AssertRect(child0, 0, 100, 50, 50);
        LayoutFixture.AssertNoLayoutBits(root);
    }

    [Fact]
    public void AbsoluteLayoutPaddingBottom()
    {
        // Yoga fixture: absolute_layout_padding_bottom
        Element root = new() { Name = "root" };
        root.Inline.Position = PositionType.Absolute;
        root.Inline.Padding = root.Inline.Padding with { Bottom = 100 };
        root.Inline.Width = 200;
        root.Inline.Height = 200;

        Element child0 = AddChild(root, "root_child0");
        child0.Inline.Position = PositionType.Absolute;
        child0.Inline.Width = 50;
        child0.Inline.Height = 50;

        LayoutFixture.Calculate(root);

        LayoutFixture.AssertRect(root, 0, 0, 200, 200);
        LayoutFixture.AssertRect(child0, 0, 0, 50, 50);
        LayoutFixture.AssertNoLayoutBits(root);
    }

    [Fact]
    public void AbsoluteLayoutColumnReverseMarginBorder()
    {
        // Yoga fixture: absolute_layout_column_reverse_margin_border
        Element root = new() { Name = "root" };
        root.Inline.FlexDirection = FlexDirection.ColumnReverse;
        root.Inline.Position = PositionType.Absolute;
        root.Inline.Width = 200;
        root.Inline.Height = 200;

        Element child0 = AddChild(root, "root_child0");
        child0.Inline.Position = PositionType.Absolute;
        child0.Inline.Left = 5;
        child0.Inline.Right = 3;
        child0.Inline.Margin = child0.Inline.Margin with { Left = 3 };
        child0.Inline.Margin = child0.Inline.Margin with { Right = 4 };
        child0.Inline.BorderWidth = child0.Inline.BorderWidth with { Left = 1 };
        child0.Inline.BorderWidth = child0.Inline.BorderWidth with { Right = 7 };
        child0.Inline.Width = 50;
        child0.Inline.Height = 50;

        LayoutFixture.Calculate(root);

        LayoutFixture.AssertRect(root, 0, 0, 200, 200);
        LayoutFixture.AssertRect(child0, 8, 150, 50, 50);
        LayoutFixture.AssertNoLayoutBits(root);
    }

    [Fact]
    public void AbsoluteLayoutPercentageBottomBasedOnParentHeight()
    {
        // Yoga fixture: absolute_layout_percentage_bottom_based_on_parent_height
        Element root = new() { Name = "root" };
        root.Inline.Position = PositionType.Absolute;
        root.Inline.Width = 100;
        root.Inline.Height = 200;

        Element child0 = AddChild(root, "root_child0");
        child0.Inline.Position = PositionType.Absolute;
        child0.Inline.Top = Length.Percent(50);
        child0.Inline.Width = 10;
        child0.Inline.Height = 10;

        Element child1 = AddChild(root, "root_child1");
        child1.Inline.Position = PositionType.Absolute;
        child1.Inline.Bottom = Length.Percent(50);
        child1.Inline.Width = 10;
        child1.Inline.Height = 10;

        Element child2 = AddChild(root, "root_child2");
        child2.Inline.Position = PositionType.Absolute;
        child2.Inline.Top = Length.Percent(10);
        child2.Inline.Bottom = Length.Percent(10);
        child2.Inline.Width = 10;

        LayoutFixture.Calculate(root);

        LayoutFixture.AssertRect(root, 0, 0, 100, 200);
        LayoutFixture.AssertRect(child0, 0, 100, 10, 10);
        LayoutFixture.AssertRect(child1, 0, 90, 10, 10);
        LayoutFixture.AssertRect(child2, 0, 20, 10, 160);
        LayoutFixture.AssertNoLayoutBits(root);
    }

    [Fact]
    public void PercentAbsolutePositionInfiniteHeight()
    {
        // Yoga fixture: percent_absolute_position_infinite_height
        Element root = new() { Name = "root" };
        root.Inline.Position = PositionType.Absolute;
        root.Inline.Width = 300;

        Element child0 = AddChild(root, "root_child0");
        child0.Inline.Width = 300;

        Element child1 = AddChild(root, "root_child1");
        child1.Inline.Position = PositionType.Absolute;
        child1.Inline.Left = Length.Percent(20);
        child1.Inline.Top = Length.Percent(20);
        child1.Inline.Width = Length.Percent(20);
        child1.Inline.Height = Length.Percent(20);

        LayoutFixture.Calculate(root);

        LayoutFixture.AssertRect(root, 0, 0, 300, 0);
        LayoutFixture.AssertRect(child0, 0, 0, 300, 0);
        LayoutFixture.AssertRect(child1, 60, 0, 60, 0);
        LayoutFixture.AssertNoLayoutBits(root);
    }

    [Fact]
    public void AbsoluteLayoutPercentageHeightBasedOnPaddedParent()
    {
        // Yoga fixture: absolute_layout_percentage_height_based_on_padded_parent
        Element root = new() { Name = "root" };
        root.Inline.Position = PositionType.Absolute;
        root.Inline.Padding = root.Inline.Padding with { Top = 10 };
        root.Inline.BorderWidth = root.Inline.BorderWidth with { Top = 10 };
        root.Inline.Width = 100;
        root.Inline.Height = 100;

        Element child0 = AddChild(root, "root_child0");
        child0.Inline.Position = PositionType.Absolute;
        child0.Inline.Width = 100;
        child0.Inline.Height = Length.Percent(50);

        LayoutFixture.Calculate(root);

        LayoutFixture.AssertRect(root, 0, 0, 100, 100);
        LayoutFixture.AssertRect(child0, 0, 20, 100, 45);
        LayoutFixture.AssertNoLayoutBits(root);
    }

    [Fact]
    public void AbsoluteLayoutPercentageHeightBasedOnPaddedParentAndAlignItemsCenter()
    {
        // Yoga fixture: absolute_layout_percentage_height_based_on_padded_parent_and_align_items_center
        Element root = new() { Name = "root" };
        root.Inline.JustifyContent = Justify.Center;
        root.Inline.AlignItems = Align.Center;
        root.Inline.Position = PositionType.Absolute;
        root.Inline.Padding = root.Inline.Padding with { Top = 20 };
        root.Inline.Padding = root.Inline.Padding with { Bottom = 20 };
        root.Inline.Width = 100;
        root.Inline.Height = 100;

        Element child0 = AddChild(root, "root_child0");
        child0.Inline.Position = PositionType.Absolute;
        child0.Inline.Width = 100;
        child0.Inline.Height = Length.Percent(50);

        LayoutFixture.Calculate(root);

        LayoutFixture.AssertRect(root, 0, 0, 100, 100);
        LayoutFixture.AssertRect(child0, 0, 25, 100, 50);
        LayoutFixture.AssertNoLayoutBits(root);
    }

    [Fact]
    public void AbsoluteLayoutInWrapReverseColumnContainer()
    {
        // Yoga fixture: absolute_layout_in_wrap_reverse_column_container
        Element root = new() { Name = "root" };
        root.Inline.Position = PositionType.Absolute;
        root.Inline.FlexWrap = FlexWrap.WrapReverse;
        root.Inline.Width = 100;
        root.Inline.Height = 100;

        Element child0 = AddChild(root, "root_child0");
        child0.Inline.Position = PositionType.Absolute;
        child0.Inline.Width = 20;
        child0.Inline.Height = 20;

        LayoutFixture.Calculate(root);

        LayoutFixture.AssertRect(root, 0, 0, 100, 100);
        LayoutFixture.AssertRect(child0, 80, 0, 20, 20);
        LayoutFixture.AssertNoLayoutBits(root);
    }

    [Fact]
    public void AbsoluteLayoutInWrapReverseColumnContainerFlexEnd()
    {
        // Yoga fixture: absolute_layout_in_wrap_reverse_column_container_flex_end
        Element root = new() { Name = "root" };
        root.Inline.Position = PositionType.Absolute;
        root.Inline.FlexWrap = FlexWrap.WrapReverse;
        root.Inline.Width = 100;
        root.Inline.Height = 100;

        Element child0 = AddChild(root, "root_child0");
        child0.Inline.AlignSelf = Align.FlexEnd;
        child0.Inline.Position = PositionType.Absolute;
        child0.Inline.Width = 20;
        child0.Inline.Height = 20;

        LayoutFixture.Calculate(root);

        LayoutFixture.AssertRect(root, 0, 0, 100, 100);
        LayoutFixture.AssertRect(child0, 0, 0, 20, 20);
        LayoutFixture.AssertNoLayoutBits(root);
    }

    [Fact]
    public void AbsoluteLayoutInWrapReverseRowContainer()
    {
        // Yoga fixture: absolute_layout_in_wrap_reverse_row_container
        Element root = new() { Name = "root" };
        root.Inline.FlexDirection = FlexDirection.Row;
        root.Inline.Position = PositionType.Absolute;
        root.Inline.FlexWrap = FlexWrap.WrapReverse;
        root.Inline.Width = 100;
        root.Inline.Height = 100;

        Element child0 = AddChild(root, "root_child0");
        child0.Inline.Position = PositionType.Absolute;
        child0.Inline.Width = 20;
        child0.Inline.Height = 20;

        LayoutFixture.Calculate(root);

        LayoutFixture.AssertRect(root, 0, 0, 100, 100);
        LayoutFixture.AssertRect(child0, 0, 80, 20, 20);
        LayoutFixture.AssertNoLayoutBits(root);
    }

    [Fact]
    public void AbsoluteLayoutInWrapReverseRowContainerFlexEnd()
    {
        // Yoga fixture: absolute_layout_in_wrap_reverse_row_container_flex_end
        Element root = new() { Name = "root" };
        root.Inline.FlexDirection = FlexDirection.Row;
        root.Inline.Position = PositionType.Absolute;
        root.Inline.FlexWrap = FlexWrap.WrapReverse;
        root.Inline.Width = 100;
        root.Inline.Height = 100;

        Element child0 = AddChild(root, "root_child0");
        child0.Inline.AlignSelf = Align.FlexEnd;
        child0.Inline.Position = PositionType.Absolute;
        child0.Inline.Width = 20;
        child0.Inline.Height = 20;

        LayoutFixture.Calculate(root);

        LayoutFixture.AssertRect(root, 0, 0, 100, 100);
        LayoutFixture.AssertRect(child0, 0, 0, 20, 20);
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
