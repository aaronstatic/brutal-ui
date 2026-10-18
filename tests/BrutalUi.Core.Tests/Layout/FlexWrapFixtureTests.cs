// Ported from Yoga (https://github.com/facebook/yoga), v3.2.1, tests/generated/YGFlexWrapTest.cpp. Copyright (c) Meta Platforms, Inc. and affiliates.
// MIT License. See THIRD-PARTY-NOTICES.md at the repository root.

using Xunit;

namespace BrutalUi.Core.Tests;

/// <summary>
/// The Yoga fixtures of <c>YGFlexWrapTest</c>, the LTR half only.
/// </summary>
/// <remarks>
/// Each Yoga fixture root sets <c>position: absolute</c>, and each port does the same.
/// The file holds all 24 fixtures of the Yoga file. The fixture ledger in <c>context.md</c> lists the file.
/// </remarks>
public sealed class FlexWrapFixtureTests
{
    [Fact]
    public void WrapColumn()
    {
        // Yoga fixture: wrap_column
        Element root = new() { Name = "root" };
        root.Style.Position = PositionType.Absolute;
        root.Style.FlexWrap = FlexWrap.Wrap;
        root.Style.Height = 100;

        Element child0 = AddChild(root, "root_child0");
        child0.Style.Width = 30;
        child0.Style.Height = 30;

        Element child1 = AddChild(root, "root_child1");
        child1.Style.Width = 30;
        child1.Style.Height = 30;

        Element child2 = AddChild(root, "root_child2");
        child2.Style.Width = 30;
        child2.Style.Height = 30;

        Element child3 = AddChild(root, "root_child3");
        child3.Style.Width = 30;
        child3.Style.Height = 30;

        LayoutFixture.Calculate(root);

        LayoutFixture.AssertRect(root, 0, 0, 60, 100);
        LayoutFixture.AssertRect(child0, 0, 0, 30, 30);
        LayoutFixture.AssertRect(child1, 0, 30, 30, 30);
        LayoutFixture.AssertRect(child2, 0, 60, 30, 30);
        LayoutFixture.AssertRect(child3, 30, 0, 30, 30);
        LayoutFixture.AssertNoLayoutBits(root);
    }

    [Fact]
    public void WrapRow()
    {
        // Yoga fixture: wrap_row
        Element root = new() { Name = "root" };
        root.Style.FlexDirection = FlexDirection.Row;
        root.Style.Position = PositionType.Absolute;
        root.Style.FlexWrap = FlexWrap.Wrap;
        root.Style.Width = 100;

        Element child0 = AddChild(root, "root_child0");
        child0.Style.Width = 30;
        child0.Style.Height = 30;

        Element child1 = AddChild(root, "root_child1");
        child1.Style.Width = 30;
        child1.Style.Height = 30;

        Element child2 = AddChild(root, "root_child2");
        child2.Style.Width = 30;
        child2.Style.Height = 30;

        Element child3 = AddChild(root, "root_child3");
        child3.Style.Width = 30;
        child3.Style.Height = 30;

        LayoutFixture.Calculate(root);

        LayoutFixture.AssertRect(root, 0, 0, 100, 60);
        LayoutFixture.AssertRect(child0, 0, 0, 30, 30);
        LayoutFixture.AssertRect(child1, 30, 0, 30, 30);
        LayoutFixture.AssertRect(child2, 60, 0, 30, 30);
        LayoutFixture.AssertRect(child3, 0, 30, 30, 30);
        LayoutFixture.AssertNoLayoutBits(root);
    }

    [Fact]
    public void WrapRowAlignItemsFlexEnd()
    {
        // Yoga fixture: wrap_row_align_items_flex_end
        Element root = new() { Name = "root" };
        root.Style.FlexDirection = FlexDirection.Row;
        root.Style.AlignItems = Align.FlexEnd;
        root.Style.Position = PositionType.Absolute;
        root.Style.FlexWrap = FlexWrap.Wrap;
        root.Style.Width = 100;

        Element child0 = AddChild(root, "root_child0");
        child0.Style.Width = 30;
        child0.Style.Height = 10;

        Element child1 = AddChild(root, "root_child1");
        child1.Style.Width = 30;
        child1.Style.Height = 20;

        Element child2 = AddChild(root, "root_child2");
        child2.Style.Width = 30;
        child2.Style.Height = 30;

        Element child3 = AddChild(root, "root_child3");
        child3.Style.Width = 30;
        child3.Style.Height = 30;

        LayoutFixture.Calculate(root);

        LayoutFixture.AssertRect(root, 0, 0, 100, 60);
        LayoutFixture.AssertRect(child0, 0, 20, 30, 10);
        LayoutFixture.AssertRect(child1, 30, 10, 30, 20);
        LayoutFixture.AssertRect(child2, 60, 0, 30, 30);
        LayoutFixture.AssertRect(child3, 0, 30, 30, 30);
        LayoutFixture.AssertNoLayoutBits(root);
    }

    [Fact]
    public void WrapRowAlignItemsCenter()
    {
        // Yoga fixture: wrap_row_align_items_center
        Element root = new() { Name = "root" };
        root.Style.FlexDirection = FlexDirection.Row;
        root.Style.AlignItems = Align.Center;
        root.Style.Position = PositionType.Absolute;
        root.Style.FlexWrap = FlexWrap.Wrap;
        root.Style.Width = 100;

        Element child0 = AddChild(root, "root_child0");
        child0.Style.Width = 30;
        child0.Style.Height = 10;

        Element child1 = AddChild(root, "root_child1");
        child1.Style.Width = 30;
        child1.Style.Height = 20;

        Element child2 = AddChild(root, "root_child2");
        child2.Style.Width = 30;
        child2.Style.Height = 30;

        Element child3 = AddChild(root, "root_child3");
        child3.Style.Width = 30;
        child3.Style.Height = 30;

        LayoutFixture.Calculate(root);

        LayoutFixture.AssertRect(root, 0, 0, 100, 60);
        LayoutFixture.AssertRect(child0, 0, 10, 30, 10);
        LayoutFixture.AssertRect(child1, 30, 5, 30, 20);
        LayoutFixture.AssertRect(child2, 60, 0, 30, 30);
        LayoutFixture.AssertRect(child3, 0, 30, 30, 30);
        LayoutFixture.AssertNoLayoutBits(root);
    }

    [Fact]
    public void FlexWrapChildrenWithMinMainOverridingFlexBasis()
    {
        // Yoga fixture: flex_wrap_children_with_min_main_overriding_flex_basis
        Element root = new() { Name = "root" };
        root.Style.FlexDirection = FlexDirection.Row;
        root.Style.Position = PositionType.Absolute;
        root.Style.FlexWrap = FlexWrap.Wrap;
        root.Style.Width = 100;

        Element child0 = AddChild(root, "root_child0");
        child0.Style.FlexBasis = 50;
        child0.Style.MinWidth = 55;
        child0.Style.Height = 50;

        Element child1 = AddChild(root, "root_child1");
        child1.Style.FlexBasis = 50;
        child1.Style.MinWidth = 55;
        child1.Style.Height = 50;

        LayoutFixture.Calculate(root);

        LayoutFixture.AssertRect(root, 0, 0, 100, 100);
        LayoutFixture.AssertRect(child0, 0, 0, 55, 50);
        LayoutFixture.AssertRect(child1, 0, 50, 55, 50);
        LayoutFixture.AssertNoLayoutBits(root);
    }

    [Fact]
    public void FlexWrapWrapToChildHeight()
    {
        // Yoga fixture: flex_wrap_wrap_to_child_height
        Element root = new() { Name = "root" };
        root.Style.Position = PositionType.Absolute;

        Element child0 = AddChild(root, "root_child0");
        child0.Style.FlexDirection = FlexDirection.Row;
        child0.Style.AlignItems = Align.FlexStart;
        child0.Style.FlexWrap = FlexWrap.Wrap;

        Element child0Child0 = AddChild(child0, "root_child0_child0");
        child0Child0.Style.Width = 100;

        Element child0Child0Child0 = AddChild(child0Child0, "root_child0_child0_child0");
        child0Child0Child0.Style.Width = 100;
        child0Child0Child0.Style.Height = 100;

        Element child1 = AddChild(root, "root_child1");
        child1.Style.Width = 100;
        child1.Style.Height = 100;

        LayoutFixture.Calculate(root);

        LayoutFixture.AssertRect(root, 0, 0, 100, 200);
        LayoutFixture.AssertRect(child0, 0, 0, 100, 100);
        LayoutFixture.AssertRect(child0Child0, 0, 0, 100, 100);
        LayoutFixture.AssertRect(child0Child0Child0, 0, 0, 100, 100);
        LayoutFixture.AssertRect(child1, 0, 100, 100, 100);
        LayoutFixture.AssertNoLayoutBits(root);
    }

    [Fact]
    public void FlexWrapAlignStretchFitsOneRow()
    {
        // Yoga fixture: flex_wrap_align_stretch_fits_one_row
        Element root = new() { Name = "root" };
        root.Style.FlexDirection = FlexDirection.Row;
        root.Style.Position = PositionType.Absolute;
        root.Style.FlexWrap = FlexWrap.Wrap;
        root.Style.Width = 150;
        root.Style.Height = 100;

        Element child0 = AddChild(root, "root_child0");
        child0.Style.Width = 50;

        Element child1 = AddChild(root, "root_child1");
        child1.Style.Width = 50;

        LayoutFixture.Calculate(root);

        LayoutFixture.AssertRect(root, 0, 0, 150, 100);
        LayoutFixture.AssertRect(child0, 0, 0, 50, 0);
        LayoutFixture.AssertRect(child1, 50, 0, 50, 0);
        LayoutFixture.AssertNoLayoutBits(root);
    }

    [Fact]
    public void WrapReverseRowAlignContentFlexStart()
    {
        // Yoga fixture: wrap_reverse_row_align_content_flex_start
        Element root = new() { Name = "root" };
        root.Style.FlexDirection = FlexDirection.Row;
        root.Style.Position = PositionType.Absolute;
        root.Style.FlexWrap = FlexWrap.WrapReverse;
        root.Style.Width = 100;

        Element child0 = AddChild(root, "root_child0");
        child0.Style.Width = 30;
        child0.Style.Height = 10;

        Element child1 = AddChild(root, "root_child1");
        child1.Style.Width = 30;
        child1.Style.Height = 20;

        Element child2 = AddChild(root, "root_child2");
        child2.Style.Width = 30;
        child2.Style.Height = 30;

        Element child3 = AddChild(root, "root_child3");
        child3.Style.Width = 30;
        child3.Style.Height = 40;

        Element child4 = AddChild(root, "root_child4");
        child4.Style.Width = 30;
        child4.Style.Height = 50;

        LayoutFixture.Calculate(root);

        LayoutFixture.AssertRect(root, 0, 0, 100, 80);
        LayoutFixture.AssertRect(child0, 0, 70, 30, 10);
        LayoutFixture.AssertRect(child1, 30, 60, 30, 20);
        LayoutFixture.AssertRect(child2, 60, 50, 30, 30);
        LayoutFixture.AssertRect(child3, 0, 10, 30, 40);
        LayoutFixture.AssertRect(child4, 30, 0, 30, 50);
        LayoutFixture.AssertNoLayoutBits(root);
    }

    [Fact]
    public void WrapReverseRowAlignContentCenter()
    {
        // Yoga fixture: wrap_reverse_row_align_content_center
        Element root = new() { Name = "root" };
        root.Style.FlexDirection = FlexDirection.Row;
        root.Style.AlignContent = Align.Center;
        root.Style.Position = PositionType.Absolute;
        root.Style.FlexWrap = FlexWrap.WrapReverse;
        root.Style.Width = 100;

        Element child0 = AddChild(root, "root_child0");
        child0.Style.Width = 30;
        child0.Style.Height = 10;

        Element child1 = AddChild(root, "root_child1");
        child1.Style.Width = 30;
        child1.Style.Height = 20;

        Element child2 = AddChild(root, "root_child2");
        child2.Style.Width = 30;
        child2.Style.Height = 30;

        Element child3 = AddChild(root, "root_child3");
        child3.Style.Width = 30;
        child3.Style.Height = 40;

        Element child4 = AddChild(root, "root_child4");
        child4.Style.Width = 30;
        child4.Style.Height = 50;

        LayoutFixture.Calculate(root);

        LayoutFixture.AssertRect(root, 0, 0, 100, 80);
        LayoutFixture.AssertRect(child0, 0, 70, 30, 10);
        LayoutFixture.AssertRect(child1, 30, 60, 30, 20);
        LayoutFixture.AssertRect(child2, 60, 50, 30, 30);
        LayoutFixture.AssertRect(child3, 0, 10, 30, 40);
        LayoutFixture.AssertRect(child4, 30, 0, 30, 50);
        LayoutFixture.AssertNoLayoutBits(root);
    }

    [Fact]
    public void WrapReverseRowSingleLineDifferentSize()
    {
        // Yoga fixture: wrap_reverse_row_single_line_different_size
        Element root = new() { Name = "root" };
        root.Style.FlexDirection = FlexDirection.Row;
        root.Style.Position = PositionType.Absolute;
        root.Style.FlexWrap = FlexWrap.WrapReverse;
        root.Style.Width = 300;

        Element child0 = AddChild(root, "root_child0");
        child0.Style.Width = 30;
        child0.Style.Height = 10;

        Element child1 = AddChild(root, "root_child1");
        child1.Style.Width = 30;
        child1.Style.Height = 20;

        Element child2 = AddChild(root, "root_child2");
        child2.Style.Width = 30;
        child2.Style.Height = 30;

        Element child3 = AddChild(root, "root_child3");
        child3.Style.Width = 30;
        child3.Style.Height = 40;

        Element child4 = AddChild(root, "root_child4");
        child4.Style.Width = 30;
        child4.Style.Height = 50;

        LayoutFixture.Calculate(root);

        LayoutFixture.AssertRect(root, 0, 0, 300, 50);
        LayoutFixture.AssertRect(child0, 0, 40, 30, 10);
        LayoutFixture.AssertRect(child1, 30, 30, 30, 20);
        LayoutFixture.AssertRect(child2, 60, 20, 30, 30);
        LayoutFixture.AssertRect(child3, 90, 10, 30, 40);
        LayoutFixture.AssertRect(child4, 120, 0, 30, 50);
        LayoutFixture.AssertNoLayoutBits(root);
    }

    [Fact]
    public void WrapReverseRowAlignContentStretch()
    {
        // Yoga fixture: wrap_reverse_row_align_content_stretch
        Element root = new() { Name = "root" };
        root.Style.FlexDirection = FlexDirection.Row;
        root.Style.AlignContent = Align.Stretch;
        root.Style.Position = PositionType.Absolute;
        root.Style.FlexWrap = FlexWrap.WrapReverse;
        root.Style.Width = 100;

        Element child0 = AddChild(root, "root_child0");
        child0.Style.Width = 30;
        child0.Style.Height = 10;

        Element child1 = AddChild(root, "root_child1");
        child1.Style.Width = 30;
        child1.Style.Height = 20;

        Element child2 = AddChild(root, "root_child2");
        child2.Style.Width = 30;
        child2.Style.Height = 30;

        Element child3 = AddChild(root, "root_child3");
        child3.Style.Width = 30;
        child3.Style.Height = 40;

        Element child4 = AddChild(root, "root_child4");
        child4.Style.Width = 30;
        child4.Style.Height = 50;

        LayoutFixture.Calculate(root);

        LayoutFixture.AssertRect(root, 0, 0, 100, 80);
        LayoutFixture.AssertRect(child0, 0, 70, 30, 10);
        LayoutFixture.AssertRect(child1, 30, 60, 30, 20);
        LayoutFixture.AssertRect(child2, 60, 50, 30, 30);
        LayoutFixture.AssertRect(child3, 0, 10, 30, 40);
        LayoutFixture.AssertRect(child4, 30, 0, 30, 50);
        LayoutFixture.AssertNoLayoutBits(root);
    }

    [Fact]
    public void WrapReverseRowAlignContentSpaceAround()
    {
        // Yoga fixture: wrap_reverse_row_align_content_space_around
        Element root = new() { Name = "root" };
        root.Style.FlexDirection = FlexDirection.Row;
        root.Style.AlignContent = Align.SpaceAround;
        root.Style.Position = PositionType.Absolute;
        root.Style.FlexWrap = FlexWrap.WrapReverse;
        root.Style.Width = 100;

        Element child0 = AddChild(root, "root_child0");
        child0.Style.Width = 30;
        child0.Style.Height = 10;

        Element child1 = AddChild(root, "root_child1");
        child1.Style.Width = 30;
        child1.Style.Height = 20;

        Element child2 = AddChild(root, "root_child2");
        child2.Style.Width = 30;
        child2.Style.Height = 30;

        Element child3 = AddChild(root, "root_child3");
        child3.Style.Width = 30;
        child3.Style.Height = 40;

        Element child4 = AddChild(root, "root_child4");
        child4.Style.Width = 30;
        child4.Style.Height = 50;

        LayoutFixture.Calculate(root);

        LayoutFixture.AssertRect(root, 0, 0, 100, 80);
        LayoutFixture.AssertRect(child0, 0, 70, 30, 10);
        LayoutFixture.AssertRect(child1, 30, 60, 30, 20);
        LayoutFixture.AssertRect(child2, 60, 50, 30, 30);
        LayoutFixture.AssertRect(child3, 0, 10, 30, 40);
        LayoutFixture.AssertRect(child4, 30, 0, 30, 50);
        LayoutFixture.AssertNoLayoutBits(root);
    }

    [Fact]
    public void WrapReverseColumnFixedSize()
    {
        // Yoga fixture: wrap_reverse_column_fixed_size
        Element root = new() { Name = "root" };
        root.Style.AlignItems = Align.Center;
        root.Style.Position = PositionType.Absolute;
        root.Style.FlexWrap = FlexWrap.WrapReverse;
        root.Style.Width = 200;
        root.Style.Height = 100;

        Element child0 = AddChild(root, "root_child0");
        child0.Style.Width = 30;
        child0.Style.Height = 10;

        Element child1 = AddChild(root, "root_child1");
        child1.Style.Width = 30;
        child1.Style.Height = 20;

        Element child2 = AddChild(root, "root_child2");
        child2.Style.Width = 30;
        child2.Style.Height = 30;

        Element child3 = AddChild(root, "root_child3");
        child3.Style.Width = 30;
        child3.Style.Height = 40;

        Element child4 = AddChild(root, "root_child4");
        child4.Style.Width = 30;
        child4.Style.Height = 50;

        LayoutFixture.Calculate(root);

        LayoutFixture.AssertRect(root, 0, 0, 200, 100);
        LayoutFixture.AssertRect(child0, 170, 0, 30, 10);
        LayoutFixture.AssertRect(child1, 170, 10, 30, 20);
        LayoutFixture.AssertRect(child2, 170, 30, 30, 30);
        LayoutFixture.AssertRect(child3, 170, 60, 30, 40);
        LayoutFixture.AssertRect(child4, 140, 0, 30, 50);
        LayoutFixture.AssertNoLayoutBits(root);
    }

    [Fact]
    public void WrappedRowWithinAlignItemsCenter()
    {
        // Yoga fixture: wrapped_row_within_align_items_center
        Element root = new() { Name = "root" };
        root.Style.AlignItems = Align.Center;
        root.Style.Position = PositionType.Absolute;
        root.Style.Width = 200;
        root.Style.Height = 200;

        Element child0 = AddChild(root, "root_child0");
        child0.Style.FlexDirection = FlexDirection.Row;
        child0.Style.FlexWrap = FlexWrap.Wrap;

        Element child0Child0 = AddChild(child0, "root_child0_child0");
        child0Child0.Style.Width = 150;
        child0Child0.Style.Height = 80;

        Element child0Child1 = AddChild(child0, "root_child0_child1");
        child0Child1.Style.Width = 80;
        child0Child1.Style.Height = 80;

        LayoutFixture.Calculate(root);

        LayoutFixture.AssertRect(root, 0, 0, 200, 200);
        LayoutFixture.AssertRect(child0, 0, 0, 200, 160);
        LayoutFixture.AssertRect(child0Child0, 0, 0, 150, 80);
        LayoutFixture.AssertRect(child0Child1, 0, 80, 80, 80);
        LayoutFixture.AssertNoLayoutBits(root);
    }

    [Fact]
    public void WrappedRowWithinAlignItemsFlexStart()
    {
        // Yoga fixture: wrapped_row_within_align_items_flex_start
        Element root = new() { Name = "root" };
        root.Style.AlignItems = Align.FlexStart;
        root.Style.Position = PositionType.Absolute;
        root.Style.Width = 200;
        root.Style.Height = 200;

        Element child0 = AddChild(root, "root_child0");
        child0.Style.FlexDirection = FlexDirection.Row;
        child0.Style.FlexWrap = FlexWrap.Wrap;

        Element child0Child0 = AddChild(child0, "root_child0_child0");
        child0Child0.Style.Width = 150;
        child0Child0.Style.Height = 80;

        Element child0Child1 = AddChild(child0, "root_child0_child1");
        child0Child1.Style.Width = 80;
        child0Child1.Style.Height = 80;

        LayoutFixture.Calculate(root);

        LayoutFixture.AssertRect(root, 0, 0, 200, 200);
        LayoutFixture.AssertRect(child0, 0, 0, 200, 160);
        LayoutFixture.AssertRect(child0Child0, 0, 0, 150, 80);
        LayoutFixture.AssertRect(child0Child1, 0, 80, 80, 80);
        LayoutFixture.AssertNoLayoutBits(root);
    }

    [Fact]
    public void WrappedRowWithinAlignItemsFlexEnd()
    {
        // Yoga fixture: wrapped_row_within_align_items_flex_end
        Element root = new() { Name = "root" };
        root.Style.AlignItems = Align.FlexEnd;
        root.Style.Position = PositionType.Absolute;
        root.Style.Width = 200;
        root.Style.Height = 200;

        Element child0 = AddChild(root, "root_child0");
        child0.Style.FlexDirection = FlexDirection.Row;
        child0.Style.FlexWrap = FlexWrap.Wrap;

        Element child0Child0 = AddChild(child0, "root_child0_child0");
        child0Child0.Style.Width = 150;
        child0Child0.Style.Height = 80;

        Element child0Child1 = AddChild(child0, "root_child0_child1");
        child0Child1.Style.Width = 80;
        child0Child1.Style.Height = 80;

        LayoutFixture.Calculate(root);

        LayoutFixture.AssertRect(root, 0, 0, 200, 200);
        LayoutFixture.AssertRect(child0, 0, 0, 200, 160);
        LayoutFixture.AssertRect(child0Child0, 0, 0, 150, 80);
        LayoutFixture.AssertRect(child0Child1, 0, 80, 80, 80);
        LayoutFixture.AssertNoLayoutBits(root);
    }

    [Fact]
    public void WrappedColumnMaxHeight()
    {
        // Yoga fixture: wrapped_column_max_height
        Element root = new() { Name = "root" };
        root.Style.JustifyContent = Justify.Center;
        root.Style.AlignContent = Align.Center;
        root.Style.AlignItems = Align.Center;
        root.Style.Position = PositionType.Absolute;
        root.Style.FlexWrap = FlexWrap.Wrap;
        root.Style.Width = 700;
        root.Style.Height = 500;

        Element child0 = AddChild(root, "root_child0");
        child0.Style.Width = 100;
        child0.Style.Height = 500;
        child0.Style.MaxHeight = 200;

        Element child1 = AddChild(root, "root_child1");
        child1.Style.Margin.Left = 20;
        child1.Style.Margin.Top = 20;
        child1.Style.Margin.Right = 20;
        child1.Style.Margin.Bottom = 20;
        child1.Style.Width = 200;
        child1.Style.Height = 200;

        Element child2 = AddChild(root, "root_child2");
        child2.Style.Width = 100;
        child2.Style.Height = 100;

        LayoutFixture.Calculate(root);

        LayoutFixture.AssertRect(root, 0, 0, 700, 500);
        LayoutFixture.AssertRect(child0, 250, 30, 100, 200);
        LayoutFixture.AssertRect(child1, 200, 250, 200, 200);
        LayoutFixture.AssertRect(child2, 420, 200, 100, 100);
        LayoutFixture.AssertNoLayoutBits(root);
    }

    [Fact]
    public void WrappedColumnMaxHeightFlex()
    {
        // Yoga fixture: wrapped_column_max_height_flex
        Element root = new() { Name = "root" };
        root.Style.JustifyContent = Justify.Center;
        root.Style.AlignContent = Align.Center;
        root.Style.AlignItems = Align.Center;
        root.Style.Position = PositionType.Absolute;
        root.Style.FlexWrap = FlexWrap.Wrap;
        root.Style.Width = 700;
        root.Style.Height = 500;

        Element child0 = AddChild(root, "root_child0");
        child0.Style.FlexGrow = 1;
        child0.Style.FlexShrink = 1;
        child0.Style.FlexBasis = Length.Percent(0);
        child0.Style.Width = 100;
        child0.Style.Height = 500;
        child0.Style.MaxHeight = 200;

        Element child1 = AddChild(root, "root_child1");
        child1.Style.FlexGrow = 1;
        child1.Style.FlexShrink = 1;
        child1.Style.FlexBasis = Length.Percent(0);
        child1.Style.Margin.Left = 20;
        child1.Style.Margin.Top = 20;
        child1.Style.Margin.Right = 20;
        child1.Style.Margin.Bottom = 20;
        child1.Style.Width = 200;
        child1.Style.Height = 200;

        Element child2 = AddChild(root, "root_child2");
        child2.Style.Width = 100;
        child2.Style.Height = 100;

        LayoutFixture.Calculate(root);

        LayoutFixture.AssertRect(root, 0, 0, 700, 500);
        LayoutFixture.AssertRect(child0, 300, 0, 100, 180);
        LayoutFixture.AssertRect(child1, 250, 200, 200, 180);
        LayoutFixture.AssertRect(child2, 300, 400, 100, 100);
        LayoutFixture.AssertNoLayoutBits(root);
    }

    [Fact]
    public void WrapNodesWithContentSizingOverflowingMargin()
    {
        // Yoga fixture: wrap_nodes_with_content_sizing_overflowing_margin
        Element root = new() { Name = "root" };
        root.Style.Position = PositionType.Absolute;
        root.Style.Width = 500;
        root.Style.Height = 500;

        Element child0 = AddChild(root, "root_child0");
        child0.Style.FlexDirection = FlexDirection.Row;
        child0.Style.FlexWrap = FlexWrap.Wrap;
        child0.Style.Width = 85;

        Element child0Child0 = AddChild(child0, "root_child0_child0");

        Element child0Child0Child0 = AddChild(child0Child0, "root_child0_child0_child0");
        child0Child0Child0.Style.Width = 40;
        child0Child0Child0.Style.Height = 40;

        Element child0Child1 = AddChild(child0, "root_child0_child1");
        child0Child1.Style.Margin.Right = 10;

        Element child0Child1Child0 = AddChild(child0Child1, "root_child0_child1_child0");
        child0Child1Child0.Style.Width = 40;
        child0Child1Child0.Style.Height = 40;

        LayoutFixture.Calculate(root);

        LayoutFixture.AssertRect(root, 0, 0, 500, 500);
        LayoutFixture.AssertRect(child0, 0, 0, 85, 80);
        LayoutFixture.AssertRect(child0Child0, 0, 0, 40, 40);
        LayoutFixture.AssertRect(child0Child0Child0, 0, 0, 40, 40);
        LayoutFixture.AssertRect(child0Child1, 0, 40, 40, 40);
        LayoutFixture.AssertRect(child0Child1Child0, 0, 0, 40, 40);
        LayoutFixture.AssertNoLayoutBits(root);
    }

    [Fact]
    public void WrapNodesWithContentSizingMarginCross()
    {
        // Yoga fixture: wrap_nodes_with_content_sizing_margin_cross
        Element root = new() { Name = "root" };
        root.Style.Position = PositionType.Absolute;
        root.Style.Width = 500;
        root.Style.Height = 500;

        Element child0 = AddChild(root, "root_child0");
        child0.Style.FlexDirection = FlexDirection.Row;
        child0.Style.FlexWrap = FlexWrap.Wrap;
        child0.Style.Width = 70;

        Element child0Child0 = AddChild(child0, "root_child0_child0");

        Element child0Child0Child0 = AddChild(child0Child0, "root_child0_child0_child0");
        child0Child0Child0.Style.Width = 40;
        child0Child0Child0.Style.Height = 40;

        Element child0Child1 = AddChild(child0, "root_child0_child1");
        child0Child1.Style.Margin.Top = 10;

        Element child0Child1Child0 = AddChild(child0Child1, "root_child0_child1_child0");
        child0Child1Child0.Style.Width = 40;
        child0Child1Child0.Style.Height = 40;

        LayoutFixture.Calculate(root);

        LayoutFixture.AssertRect(root, 0, 0, 500, 500);
        LayoutFixture.AssertRect(child0, 0, 0, 70, 90);
        LayoutFixture.AssertRect(child0Child0, 0, 0, 40, 40);
        LayoutFixture.AssertRect(child0Child0Child0, 0, 0, 40, 40);
        LayoutFixture.AssertRect(child0Child1, 0, 50, 40, 40);
        LayoutFixture.AssertRect(child0Child1Child0, 0, 0, 40, 40);
        LayoutFixture.AssertNoLayoutBits(root);
    }

    [Fact]
    public void WrapWithMinCrossAxis()
    {
        // Yoga fixture: wrap_with_min_cross_axis
        Element root = new() { Name = "root" };
        root.Style.FlexDirection = FlexDirection.Row;
        root.Style.Position = PositionType.Absolute;
        root.Style.FlexWrap = FlexWrap.Wrap;
        root.Style.Width = 500;
        root.Style.MinHeight = 500;

        Element child0 = AddChild(root, "root_child0");
        child0.Style.Width = 400;
        child0.Style.Height = 200;

        Element child1 = AddChild(root, "root_child1");
        child1.Style.Width = 400;
        child1.Style.Height = 200;

        LayoutFixture.Calculate(root);

        LayoutFixture.AssertRect(root, 0, 0, 500, 500);
        LayoutFixture.AssertRect(child0, 0, 0, 400, 200);
        LayoutFixture.AssertRect(child1, 0, 200, 400, 200);
        LayoutFixture.AssertNoLayoutBits(root);
    }

    [Fact]
    public void WrapWithMaxCrossAxis()
    {
        // Yoga fixture: wrap_with_max_cross_axis
        Element root = new() { Name = "root" };
        root.Style.FlexDirection = FlexDirection.Row;
        root.Style.Position = PositionType.Absolute;
        root.Style.FlexWrap = FlexWrap.Wrap;
        root.Style.Width = 500;
        root.Style.MaxHeight = 500;

        Element child0 = AddChild(root, "root_child0");
        child0.Style.Width = 400;
        child0.Style.Height = 200;

        Element child1 = AddChild(root, "root_child1");
        child1.Style.Width = 400;
        child1.Style.Height = 200;

        LayoutFixture.Calculate(root);

        LayoutFixture.AssertRect(root, 0, 0, 500, 400);
        LayoutFixture.AssertRect(child0, 0, 0, 400, 200);
        LayoutFixture.AssertRect(child1, 0, 200, 400, 200);
        LayoutFixture.AssertNoLayoutBits(root);
    }

    [Fact]
    public void NowrapExpandsFlexlineBoxToMinCross()
    {
        // Yoga fixture: nowrap_expands_flexline_box_to_min_cross
        Element root = new() { Name = "root" };
        root.Style.FlexDirection = FlexDirection.Row;
        root.Style.Position = PositionType.Absolute;
        root.Style.MinHeight = 400;

        Element child0 = AddChild(root, "root_child0");
        child0.Style.FlexGrow = 1;
        child0.Style.FlexShrink = 1;
        child0.Style.FlexBasis = Length.Percent(0);

        LayoutFixture.Calculate(root);

        LayoutFixture.AssertRect(root, 0, 0, 0, 400);
        LayoutFixture.AssertRect(child0, 0, 0, 0, 400);
        LayoutFixture.AssertNoLayoutBits(root);
    }

    [Fact]
    public void WrapDoesNotImposeMinCrossOntoSingleFlexline()
    {
        // Yoga fixture: wrap_does_not_impose_min_cross_onto_single_flexline
        Element root = new() { Name = "root" };
        root.Style.FlexDirection = FlexDirection.Row;
        root.Style.Position = PositionType.Absolute;
        root.Style.FlexWrap = FlexWrap.Wrap;
        root.Style.MinHeight = 400;

        Element child0 = AddChild(root, "root_child0");
        child0.Style.FlexGrow = 1;
        child0.Style.FlexShrink = 1;
        child0.Style.FlexBasis = Length.Percent(0);

        LayoutFixture.Calculate(root);

        LayoutFixture.AssertRect(root, 0, 0, 0, 400);
        LayoutFixture.AssertRect(child0, 0, 0, 0, 0);
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
