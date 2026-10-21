// Ported from Yoga (https://github.com/facebook/yoga), v3.2.1, tests/generated/YGFlexDirectionTest.cpp. Copyright (c) Meta Platforms, Inc. and affiliates.
// MIT License. See THIRD-PARTY-NOTICES.md at the repository root.

using Xunit;

namespace BrutalUi.Core.Tests;

/// <summary>
/// The Yoga fixtures of <c>YGFlexDirectionTest</c>, the LTR half only.
/// </summary>
/// <remarks>
/// Each Yoga fixture root sets <c>position: absolute</c>. The ports of this file were written before the engine had a position type.
/// For a root, the position type does not change the layout, so the ports omit that line.
/// The four relative offset fixtures of <c>YGFlexDirectionTest</c> (<c>flex_direction_row_reverse_pos_left</c>,
/// <c>_pos_right</c>, <c>flex_direction_column_reverse_pos_top</c>, and <c>_pos_bottom</c>) are in
/// <see cref="RelativePositionFixtureTests"/>, not in this class.
/// </remarks>
public sealed class FlexDirectionFixtureTests
{
    [Fact]
    public void FlexDirectionColumnNoHeight()
    {
        // Yoga fixture: flex_direction_column_no_height
        Element root = new() { Name = "root" };
        // Phase 5: root position type absolute. It does not change the layout of a root.
        root.Inline.Width = 100;

        Element child0 = AddChild(root, "root_child0");
        child0.Inline.Height = 10;

        Element child1 = AddChild(root, "root_child1");
        child1.Inline.Height = 10;

        Element child2 = AddChild(root, "root_child2");
        child2.Inline.Height = 10;

        LayoutFixture.Calculate(root);

        LayoutFixture.AssertRect(root, 0, 0, 100, 30);
        LayoutFixture.AssertRect(child0, 0, 0, 100, 10);
        LayoutFixture.AssertRect(child1, 0, 10, 100, 10);
        LayoutFixture.AssertRect(child2, 0, 20, 100, 10);
        LayoutFixture.AssertNoLayoutBits(root);
    }

    [Fact]
    public void FlexDirectionRowNoWidth()
    {
        // Yoga fixture: flex_direction_row_no_width
        Element root = new() { Name = "root" };
        root.Inline.FlexDirection = FlexDirection.Row;
        // Phase 5: root position type absolute. It does not change the layout of a root.
        root.Inline.Height = 100;

        Element child0 = AddChild(root, "root_child0");
        child0.Inline.Width = 10;

        Element child1 = AddChild(root, "root_child1");
        child1.Inline.Width = 10;

        Element child2 = AddChild(root, "root_child2");
        child2.Inline.Width = 10;

        LayoutFixture.Calculate(root);

        LayoutFixture.AssertRect(root, 0, 0, 30, 100);
        LayoutFixture.AssertRect(child0, 0, 0, 10, 100);
        LayoutFixture.AssertRect(child1, 10, 0, 10, 100);
        LayoutFixture.AssertRect(child2, 20, 0, 10, 100);
        LayoutFixture.AssertNoLayoutBits(root);
    }

    [Fact]
    public void FlexDirectionColumn()
    {
        // Yoga fixture: flex_direction_column
        Element root = new() { Name = "root" };
        // Phase 5: root position type absolute. It does not change the layout of a root.
        root.Inline.Width = 100;
        root.Inline.Height = 100;

        Element child0 = AddChild(root, "root_child0");
        child0.Inline.Height = 10;

        Element child1 = AddChild(root, "root_child1");
        child1.Inline.Height = 10;

        Element child2 = AddChild(root, "root_child2");
        child2.Inline.Height = 10;

        LayoutFixture.Calculate(root);

        LayoutFixture.AssertRect(root, 0, 0, 100, 100);
        LayoutFixture.AssertRect(child0, 0, 0, 100, 10);
        LayoutFixture.AssertRect(child1, 0, 10, 100, 10);
        LayoutFixture.AssertRect(child2, 0, 20, 100, 10);
        LayoutFixture.AssertNoLayoutBits(root);
    }

    [Fact]
    public void FlexDirectionRow()
    {
        // Yoga fixture: flex_direction_row
        Element root = new() { Name = "root" };
        root.Inline.FlexDirection = FlexDirection.Row;
        // Phase 5: root position type absolute. It does not change the layout of a root.
        root.Inline.Width = 100;
        root.Inline.Height = 100;

        Element child0 = AddChild(root, "root_child0");
        child0.Inline.Width = 10;

        Element child1 = AddChild(root, "root_child1");
        child1.Inline.Width = 10;

        Element child2 = AddChild(root, "root_child2");
        child2.Inline.Width = 10;

        LayoutFixture.Calculate(root);

        LayoutFixture.AssertRect(root, 0, 0, 100, 100);
        LayoutFixture.AssertRect(child0, 0, 0, 10, 100);
        LayoutFixture.AssertRect(child1, 10, 0, 10, 100);
        LayoutFixture.AssertRect(child2, 20, 0, 10, 100);
        LayoutFixture.AssertNoLayoutBits(root);
    }

    [Fact]
    public void FlexDirectionColumnReverse()
    {
        // Yoga fixture: flex_direction_column_reverse
        Element root = new() { Name = "root" };
        root.Inline.FlexDirection = FlexDirection.ColumnReverse;
        // Phase 5: root position type absolute. It does not change the layout of a root.
        root.Inline.Width = 100;
        root.Inline.Height = 100;

        Element child0 = AddChild(root, "root_child0");
        child0.Inline.Height = 10;

        Element child1 = AddChild(root, "root_child1");
        child1.Inline.Height = 10;

        Element child2 = AddChild(root, "root_child2");
        child2.Inline.Height = 10;

        LayoutFixture.Calculate(root);

        LayoutFixture.AssertRect(root, 0, 0, 100, 100);
        LayoutFixture.AssertRect(child0, 0, 90, 100, 10);
        LayoutFixture.AssertRect(child1, 0, 80, 100, 10);
        LayoutFixture.AssertRect(child2, 0, 70, 100, 10);
        LayoutFixture.AssertNoLayoutBits(root);
    }

    [Fact]
    public void FlexDirectionRowReverse()
    {
        // Yoga fixture: flex_direction_row_reverse
        Element root = new() { Name = "root" };
        root.Inline.FlexDirection = FlexDirection.RowReverse;
        // Phase 5: root position type absolute. It does not change the layout of a root.
        root.Inline.Width = 100;
        root.Inline.Height = 100;

        Element child0 = AddChild(root, "root_child0");
        child0.Inline.Width = 10;

        Element child1 = AddChild(root, "root_child1");
        child1.Inline.Width = 10;

        Element child2 = AddChild(root, "root_child2");
        child2.Inline.Width = 10;

        LayoutFixture.Calculate(root);

        LayoutFixture.AssertRect(root, 0, 0, 100, 100);
        LayoutFixture.AssertRect(child0, 90, 0, 10, 100);
        LayoutFixture.AssertRect(child1, 80, 0, 10, 100);
        LayoutFixture.AssertRect(child2, 70, 0, 10, 100);
        LayoutFixture.AssertNoLayoutBits(root);
    }

    [Fact]
    public void FlexDirectionRowReverseMarginLeft()
    {
        // Yoga fixture: flex_direction_row_reverse_margin_left
        Element root = new() { Name = "root" };
        root.Inline.FlexDirection = FlexDirection.RowReverse;
        // Phase 5: root position type absolute. It does not change the layout of a root.
        root.Inline.Margin = root.Inline.Margin with { Left = 100 };
        root.Inline.Width = 100;
        root.Inline.Height = 100;

        Element child0 = AddChild(root, "root_child0");
        child0.Inline.Width = 10;

        Element child1 = AddChild(root, "root_child1");
        child1.Inline.Width = 10;

        Element child2 = AddChild(root, "root_child2");
        child2.Inline.Width = 10;

        LayoutFixture.Calculate(root);

        LayoutFixture.AssertRect(root, 100, 0, 100, 100);
        LayoutFixture.AssertRect(child0, 90, 0, 10, 100);
        LayoutFixture.AssertRect(child1, 80, 0, 10, 100);
        LayoutFixture.AssertRect(child2, 70, 0, 10, 100);
        LayoutFixture.AssertNoLayoutBits(root);
    }

    [Fact]
    public void FlexDirectionRowReverseMarginRight()
    {
        // Yoga fixture: flex_direction_row_reverse_margin_right
        Element root = new() { Name = "root" };
        root.Inline.FlexDirection = FlexDirection.RowReverse;
        // Phase 5: root position type absolute. It does not change the layout of a root.
        root.Inline.Margin = root.Inline.Margin with { Right = 100 };
        root.Inline.Width = 100;
        root.Inline.Height = 100;

        Element child0 = AddChild(root, "root_child0");
        child0.Inline.Width = 10;

        Element child1 = AddChild(root, "root_child1");
        child1.Inline.Width = 10;

        Element child2 = AddChild(root, "root_child2");
        child2.Inline.Width = 10;

        LayoutFixture.Calculate(root);

        LayoutFixture.AssertRect(root, 0, 0, 100, 100);
        LayoutFixture.AssertRect(child0, 90, 0, 10, 100);
        LayoutFixture.AssertRect(child1, 80, 0, 10, 100);
        LayoutFixture.AssertRect(child2, 70, 0, 10, 100);
        LayoutFixture.AssertNoLayoutBits(root);
    }

    [Fact]
    public void FlexDirectionColumnReverseMarginTop()
    {
        // Yoga fixture: flex_direction_column_reverse_margin_top
        Element root = new() { Name = "root" };
        root.Inline.FlexDirection = FlexDirection.ColumnReverse;
        // Phase 5: root position type absolute. It does not change the layout of a root.
        root.Inline.Margin = root.Inline.Margin with { Top = 100 };
        root.Inline.Width = 100;
        root.Inline.Height = 100;

        Element child0 = AddChild(root, "root_child0");
        child0.Inline.Width = 10;

        Element child1 = AddChild(root, "root_child1");
        child1.Inline.Width = 10;

        Element child2 = AddChild(root, "root_child2");
        child2.Inline.Width = 10;

        LayoutFixture.Calculate(root);

        LayoutFixture.AssertRect(root, 0, 100, 100, 100);
        LayoutFixture.AssertRect(child0, 0, 100, 10, 0);
        LayoutFixture.AssertRect(child1, 0, 100, 10, 0);
        LayoutFixture.AssertRect(child2, 0, 100, 10, 0);
        LayoutFixture.AssertNoLayoutBits(root);
    }

    [Fact]
    public void FlexDirectionColumnReverseMarginBottom()
    {
        // Yoga fixture: flex_direction_column_reverse_margin_bottom
        Element root = new() { Name = "root" };
        root.Inline.FlexDirection = FlexDirection.ColumnReverse;
        // Phase 5: root position type absolute. It does not change the layout of a root.
        root.Inline.Margin = root.Inline.Margin with { Bottom = 100 };
        root.Inline.Width = 100;
        root.Inline.Height = 100;

        Element child0 = AddChild(root, "root_child0");
        child0.Inline.Width = 10;

        Element child1 = AddChild(root, "root_child1");
        child1.Inline.Width = 10;

        Element child2 = AddChild(root, "root_child2");
        child2.Inline.Width = 10;

        LayoutFixture.Calculate(root);

        LayoutFixture.AssertRect(root, 0, 0, 100, 100);
        LayoutFixture.AssertRect(child0, 0, 100, 10, 0);
        LayoutFixture.AssertRect(child1, 0, 100, 10, 0);
        LayoutFixture.AssertRect(child2, 0, 100, 10, 0);
        LayoutFixture.AssertNoLayoutBits(root);
    }

    [Fact]
    public void FlexDirectionRowReversePaddingLeft()
    {
        // Yoga fixture: flex_direction_row_reverse_padding_left
        Element root = new() { Name = "root" };
        root.Inline.FlexDirection = FlexDirection.RowReverse;
        // Phase 5: root position type absolute. It does not change the layout of a root.
        root.Inline.Padding = root.Inline.Padding with { Left = 100 };
        root.Inline.Width = 100;
        root.Inline.Height = 100;

        Element child0 = AddChild(root, "root_child0");
        child0.Inline.Width = 10;

        Element child1 = AddChild(root, "root_child1");
        child1.Inline.Width = 10;

        Element child2 = AddChild(root, "root_child2");
        child2.Inline.Width = 10;

        LayoutFixture.Calculate(root);

        LayoutFixture.AssertRect(root, 0, 0, 100, 100);
        LayoutFixture.AssertRect(child0, 90, 0, 10, 100);
        LayoutFixture.AssertRect(child1, 80, 0, 10, 100);
        LayoutFixture.AssertRect(child2, 70, 0, 10, 100);
        LayoutFixture.AssertNoLayoutBits(root);
    }

    [Fact]
    public void FlexDirectionRowReversePaddingRight()
    {
        // Yoga fixture: flex_direction_row_reverse_padding_right
        Element root = new() { Name = "root" };
        root.Inline.FlexDirection = FlexDirection.RowReverse;
        // Phase 5: root position type absolute. It does not change the layout of a root.
        root.Inline.Padding = root.Inline.Padding with { Right = 100 };
        root.Inline.Width = 100;
        root.Inline.Height = 100;

        Element child0 = AddChild(root, "root_child0");
        child0.Inline.Width = 10;

        Element child1 = AddChild(root, "root_child1");
        child1.Inline.Width = 10;

        Element child2 = AddChild(root, "root_child2");
        child2.Inline.Width = 10;

        LayoutFixture.Calculate(root);

        LayoutFixture.AssertRect(root, 0, 0, 100, 100);
        LayoutFixture.AssertRect(child0, -10, 0, 10, 100);
        LayoutFixture.AssertRect(child1, -20, 0, 10, 100);
        LayoutFixture.AssertRect(child2, -30, 0, 10, 100);
        LayoutFixture.AssertNoLayoutBits(root);
    }

    [Fact]
    public void FlexDirectionColumnReversePaddingTop()
    {
        // Yoga fixture: flex_direction_column_reverse_padding_top
        Element root = new() { Name = "root" };
        root.Inline.FlexDirection = FlexDirection.ColumnReverse;
        // Phase 5: root position type absolute. It does not change the layout of a root.
        root.Inline.Padding = root.Inline.Padding with { Top = 100 };
        root.Inline.Width = 100;
        root.Inline.Height = 100;

        Element child0 = AddChild(root, "root_child0");
        child0.Inline.Width = 10;

        Element child1 = AddChild(root, "root_child1");
        child1.Inline.Width = 10;

        Element child2 = AddChild(root, "root_child2");
        child2.Inline.Width = 10;

        LayoutFixture.Calculate(root);

        LayoutFixture.AssertRect(root, 0, 0, 100, 100);
        LayoutFixture.AssertRect(child0, 0, 100, 10, 0);
        LayoutFixture.AssertRect(child1, 0, 100, 10, 0);
        LayoutFixture.AssertRect(child2, 0, 100, 10, 0);
        LayoutFixture.AssertNoLayoutBits(root);
    }

    [Fact]
    public void FlexDirectionColumnReversePaddingBottom()
    {
        // Yoga fixture: flex_direction_column_reverse_padding_bottom
        Element root = new() { Name = "root" };
        root.Inline.FlexDirection = FlexDirection.ColumnReverse;
        // Phase 5: root position type absolute. It does not change the layout of a root.
        root.Inline.Padding = root.Inline.Padding with { Bottom = 100 };
        root.Inline.Width = 100;
        root.Inline.Height = 100;

        Element child0 = AddChild(root, "root_child0");
        child0.Inline.Width = 10;

        Element child1 = AddChild(root, "root_child1");
        child1.Inline.Width = 10;

        Element child2 = AddChild(root, "root_child2");
        child2.Inline.Width = 10;

        LayoutFixture.Calculate(root);

        LayoutFixture.AssertRect(root, 0, 0, 100, 100);
        LayoutFixture.AssertRect(child0, 0, 0, 10, 0);
        LayoutFixture.AssertRect(child1, 0, 0, 10, 0);
        LayoutFixture.AssertRect(child2, 0, 0, 10, 0);
        LayoutFixture.AssertNoLayoutBits(root);
    }

    [Fact]
    public void FlexDirectionRowReverseBorderLeft()
    {
        // Yoga fixture: flex_direction_row_reverse_border_left
        Element root = new() { Name = "root" };
        root.Inline.FlexDirection = FlexDirection.RowReverse;
        // Phase 5: root position type absolute. It does not change the layout of a root.
        root.Inline.BorderWidth = root.Inline.BorderWidth with { Left = 100 };
        root.Inline.Width = 100;
        root.Inline.Height = 100;

        Element child0 = AddChild(root, "root_child0");
        child0.Inline.Width = 10;

        Element child1 = AddChild(root, "root_child1");
        child1.Inline.Width = 10;

        Element child2 = AddChild(root, "root_child2");
        child2.Inline.Width = 10;

        LayoutFixture.Calculate(root);

        LayoutFixture.AssertRect(root, 0, 0, 100, 100);
        LayoutFixture.AssertRect(child0, 90, 0, 10, 100);
        LayoutFixture.AssertRect(child1, 80, 0, 10, 100);
        LayoutFixture.AssertRect(child2, 70, 0, 10, 100);
        LayoutFixture.AssertNoLayoutBits(root);
    }

    [Fact]
    public void FlexDirectionRowReverseBorderRight()
    {
        // Yoga fixture: flex_direction_row_reverse_border_right
        Element root = new() { Name = "root" };
        root.Inline.FlexDirection = FlexDirection.RowReverse;
        // Phase 5: root position type absolute. It does not change the layout of a root.
        root.Inline.BorderWidth = root.Inline.BorderWidth with { Right = 100 };
        root.Inline.Width = 100;
        root.Inline.Height = 100;

        Element child0 = AddChild(root, "root_child0");
        child0.Inline.Width = 10;

        Element child1 = AddChild(root, "root_child1");
        child1.Inline.Width = 10;

        Element child2 = AddChild(root, "root_child2");
        child2.Inline.Width = 10;

        LayoutFixture.Calculate(root);

        LayoutFixture.AssertRect(root, 0, 0, 100, 100);
        LayoutFixture.AssertRect(child0, -10, 0, 10, 100);
        LayoutFixture.AssertRect(child1, -20, 0, 10, 100);
        LayoutFixture.AssertRect(child2, -30, 0, 10, 100);
        LayoutFixture.AssertNoLayoutBits(root);
    }

    [Fact]
    public void FlexDirectionColumnReverseBorderTop()
    {
        // Yoga fixture: flex_direction_column_reverse_border_top
        Element root = new() { Name = "root" };
        root.Inline.FlexDirection = FlexDirection.ColumnReverse;
        // Phase 5: root position type absolute. It does not change the layout of a root.
        root.Inline.BorderWidth = root.Inline.BorderWidth with { Top = 100 };
        root.Inline.Width = 100;
        root.Inline.Height = 100;

        Element child0 = AddChild(root, "root_child0");
        child0.Inline.Width = 10;

        Element child1 = AddChild(root, "root_child1");
        child1.Inline.Width = 10;

        Element child2 = AddChild(root, "root_child2");
        child2.Inline.Width = 10;

        LayoutFixture.Calculate(root);

        LayoutFixture.AssertRect(root, 0, 0, 100, 100);
        LayoutFixture.AssertRect(child0, 0, 100, 10, 0);
        LayoutFixture.AssertRect(child1, 0, 100, 10, 0);
        LayoutFixture.AssertRect(child2, 0, 100, 10, 0);
        LayoutFixture.AssertNoLayoutBits(root);
    }

    [Fact]
    public void FlexDirectionColumnReverseBorderBottom()
    {
        // Yoga fixture: flex_direction_column_reverse_border_bottom
        Element root = new() { Name = "root" };
        root.Inline.FlexDirection = FlexDirection.ColumnReverse;
        // Phase 5: root position type absolute. It does not change the layout of a root.
        root.Inline.BorderWidth = root.Inline.BorderWidth with { Bottom = 100 };
        root.Inline.Width = 100;
        root.Inline.Height = 100;

        Element child0 = AddChild(root, "root_child0");
        child0.Inline.Width = 10;

        Element child1 = AddChild(root, "root_child1");
        child1.Inline.Width = 10;

        Element child2 = AddChild(root, "root_child2");
        child2.Inline.Width = 10;

        LayoutFixture.Calculate(root);

        LayoutFixture.AssertRect(root, 0, 0, 100, 100);
        LayoutFixture.AssertRect(child0, 0, 0, 10, 0);
        LayoutFixture.AssertRect(child1, 0, 0, 10, 0);
        LayoutFixture.AssertRect(child2, 0, 0, 10, 0);
        LayoutFixture.AssertNoLayoutBits(root);
    }

    [Fact]
    public void FlexDirectionRowReverseInnerPosLeft()
    {
        // Yoga fixture: flex_direction_row_reverse_inner_pos_left
        Element root = new() { Name = "root" };
        // Phase 5: root position type absolute. It does not change the layout of a root.
        root.Inline.Width = 100;
        root.Inline.Height = 100;

        Element child0 = AddChild(root, "root_child0");
        child0.Inline.FlexDirection = FlexDirection.RowReverse;
        child0.Inline.Width = 100;
        child0.Inline.Height = 100;

        Element child0Child0 = AddChild(child0, "root_child0_child0");
        child0Child0.Inline.Position = PositionType.Absolute;
        child0Child0.Inline.Left = 10;
        child0Child0.Inline.Width = 10;
        child0Child0.Inline.Height = 10;

        Element child0Child1 = AddChild(child0, "root_child0_child1");
        child0Child1.Inline.Width = 10;

        Element child0Child2 = AddChild(child0, "root_child0_child2");
        child0Child2.Inline.Width = 10;

        LayoutFixture.Calculate(root);

        LayoutFixture.AssertRect(root, 0, 0, 100, 100);
        LayoutFixture.AssertRect(child0, 0, 0, 100, 100);
        LayoutFixture.AssertRect(child0Child0, 10, 0, 10, 10);
        LayoutFixture.AssertRect(child0Child1, 90, 0, 10, 100);
        LayoutFixture.AssertRect(child0Child2, 80, 0, 10, 100);
        LayoutFixture.AssertNoLayoutBits(root);
    }

    [Fact]
    public void FlexDirectionRowReverseInnerPosRight()
    {
        // Yoga fixture: flex_direction_row_reverse_inner_pos_right
        Element root = new() { Name = "root" };
        // Phase 5: root position type absolute. It does not change the layout of a root.
        root.Inline.Width = 100;
        root.Inline.Height = 100;

        Element child0 = AddChild(root, "root_child0");
        child0.Inline.FlexDirection = FlexDirection.RowReverse;
        child0.Inline.Width = 100;
        child0.Inline.Height = 100;

        Element child0Child0 = AddChild(child0, "root_child0_child0");
        child0Child0.Inline.Position = PositionType.Absolute;
        child0Child0.Inline.Right = 10;
        child0Child0.Inline.Width = 10;
        child0Child0.Inline.Height = 10;

        Element child0Child1 = AddChild(child0, "root_child0_child1");
        child0Child1.Inline.Width = 10;

        Element child0Child2 = AddChild(child0, "root_child0_child2");
        child0Child2.Inline.Width = 10;

        LayoutFixture.Calculate(root);

        LayoutFixture.AssertRect(root, 0, 0, 100, 100);
        LayoutFixture.AssertRect(child0, 0, 0, 100, 100);
        LayoutFixture.AssertRect(child0Child0, 80, 0, 10, 10);
        LayoutFixture.AssertRect(child0Child1, 90, 0, 10, 100);
        LayoutFixture.AssertRect(child0Child2, 80, 0, 10, 100);
        LayoutFixture.AssertNoLayoutBits(root);
    }

    [Fact]
    public void FlexDirectionColReverseInnerPosTop()
    {
        // Yoga fixture: flex_direction_col_reverse_inner_pos_top
        Element root = new() { Name = "root" };
        // Phase 5: root position type absolute. It does not change the layout of a root.
        root.Inline.Width = 100;
        root.Inline.Height = 100;

        Element child0 = AddChild(root, "root_child0");
        child0.Inline.FlexDirection = FlexDirection.ColumnReverse;
        child0.Inline.Width = 100;
        child0.Inline.Height = 100;

        Element child0Child0 = AddChild(child0, "root_child0_child0");
        child0Child0.Inline.Position = PositionType.Absolute;
        child0Child0.Inline.Top = 10;
        child0Child0.Inline.Width = 10;
        child0Child0.Inline.Height = 10;

        Element child0Child1 = AddChild(child0, "root_child0_child1");
        child0Child1.Inline.Width = 10;

        Element child0Child2 = AddChild(child0, "root_child0_child2");
        child0Child2.Inline.Width = 10;

        LayoutFixture.Calculate(root);

        LayoutFixture.AssertRect(root, 0, 0, 100, 100);
        LayoutFixture.AssertRect(child0, 0, 0, 100, 100);
        LayoutFixture.AssertRect(child0Child0, 0, 10, 10, 10);
        LayoutFixture.AssertRect(child0Child1, 0, 100, 10, 0);
        LayoutFixture.AssertRect(child0Child2, 0, 100, 10, 0);
        LayoutFixture.AssertNoLayoutBits(root);
    }

    [Fact]
    public void FlexDirectionColReverseInnerPosBottom()
    {
        // Yoga fixture: flex_direction_col_reverse_inner_pos_bottom
        Element root = new() { Name = "root" };
        // Phase 5: root position type absolute. It does not change the layout of a root.
        root.Inline.Width = 100;
        root.Inline.Height = 100;

        Element child0 = AddChild(root, "root_child0");
        child0.Inline.FlexDirection = FlexDirection.ColumnReverse;
        child0.Inline.Width = 100;
        child0.Inline.Height = 100;

        Element child0Child0 = AddChild(child0, "root_child0_child0");
        child0Child0.Inline.Position = PositionType.Absolute;
        child0Child0.Inline.Bottom = 10;
        child0Child0.Inline.Width = 10;
        child0Child0.Inline.Height = 10;

        Element child0Child1 = AddChild(child0, "root_child0_child1");
        child0Child1.Inline.Width = 10;

        Element child0Child2 = AddChild(child0, "root_child0_child2");
        child0Child2.Inline.Width = 10;

        LayoutFixture.Calculate(root);

        LayoutFixture.AssertRect(root, 0, 0, 100, 100);
        LayoutFixture.AssertRect(child0, 0, 0, 100, 100);
        LayoutFixture.AssertRect(child0Child0, 0, 80, 10, 10);
        LayoutFixture.AssertRect(child0Child1, 0, 100, 10, 0);
        LayoutFixture.AssertRect(child0Child2, 0, 100, 10, 0);
        LayoutFixture.AssertNoLayoutBits(root);
    }

    [Fact]
    public void FlexDirectionRowReverseInnerMarginLeft()
    {
        // Yoga fixture: flex_direction_row_reverse_inner_margin_left
        Element root = new() { Name = "root" };
        // Phase 5: root position type absolute. It does not change the layout of a root.
        root.Inline.Width = 100;
        root.Inline.Height = 100;

        Element child0 = AddChild(root, "root_child0");
        child0.Inline.FlexDirection = FlexDirection.RowReverse;
        child0.Inline.Width = 100;
        child0.Inline.Height = 100;

        Element child0Child0 = AddChild(child0, "root_child0_child0");
        child0Child0.Inline.Position = PositionType.Absolute;
        child0Child0.Inline.Margin = child0Child0.Inline.Margin with { Left = 10 };
        child0Child0.Inline.Width = 10;
        child0Child0.Inline.Height = 10;

        Element child0Child1 = AddChild(child0, "root_child0_child1");
        child0Child1.Inline.Width = 10;

        Element child0Child2 = AddChild(child0, "root_child0_child2");
        child0Child2.Inline.Width = 10;

        LayoutFixture.Calculate(root);

        LayoutFixture.AssertRect(root, 0, 0, 100, 100);
        LayoutFixture.AssertRect(child0, 0, 0, 100, 100);
        LayoutFixture.AssertRect(child0Child0, 90, 0, 10, 10);
        LayoutFixture.AssertRect(child0Child1, 90, 0, 10, 100);
        LayoutFixture.AssertRect(child0Child2, 80, 0, 10, 100);
        LayoutFixture.AssertNoLayoutBits(root);
    }

    [Fact]
    public void FlexDirectionRowReverseInnerMarginRight()
    {
        // Yoga fixture: flex_direction_row_reverse_inner_margin_right
        Element root = new() { Name = "root" };
        // Phase 5: root position type absolute. It does not change the layout of a root.
        root.Inline.Width = 100;
        root.Inline.Height = 100;

        Element child0 = AddChild(root, "root_child0");
        child0.Inline.FlexDirection = FlexDirection.RowReverse;
        child0.Inline.Width = 100;
        child0.Inline.Height = 100;

        Element child0Child0 = AddChild(child0, "root_child0_child0");
        child0Child0.Inline.Position = PositionType.Absolute;
        child0Child0.Inline.Margin = child0Child0.Inline.Margin with { Right = 10 };
        child0Child0.Inline.Width = 10;
        child0Child0.Inline.Height = 10;

        Element child0Child1 = AddChild(child0, "root_child0_child1");
        child0Child1.Inline.Width = 10;

        Element child0Child2 = AddChild(child0, "root_child0_child2");
        child0Child2.Inline.Width = 10;

        LayoutFixture.Calculate(root);

        LayoutFixture.AssertRect(root, 0, 0, 100, 100);
        LayoutFixture.AssertRect(child0, 0, 0, 100, 100);
        LayoutFixture.AssertRect(child0Child0, 80, 0, 10, 10);
        LayoutFixture.AssertRect(child0Child1, 90, 0, 10, 100);
        LayoutFixture.AssertRect(child0Child2, 80, 0, 10, 100);
        LayoutFixture.AssertNoLayoutBits(root);
    }

    [Fact]
    public void FlexDirectionColReverseInnerMarginTop()
    {
        // Yoga fixture: flex_direction_col_reverse_inner_margin_top
        Element root = new() { Name = "root" };
        // Phase 5: root position type absolute. It does not change the layout of a root.
        root.Inline.Width = 100;
        root.Inline.Height = 100;

        Element child0 = AddChild(root, "root_child0");
        child0.Inline.FlexDirection = FlexDirection.ColumnReverse;
        child0.Inline.Width = 100;
        child0.Inline.Height = 100;

        Element child0Child0 = AddChild(child0, "root_child0_child0");
        child0Child0.Inline.Position = PositionType.Absolute;
        child0Child0.Inline.Margin = child0Child0.Inline.Margin with { Top = 10 };
        child0Child0.Inline.Width = 10;
        child0Child0.Inline.Height = 10;

        Element child0Child1 = AddChild(child0, "root_child0_child1");
        child0Child1.Inline.Width = 10;

        Element child0Child2 = AddChild(child0, "root_child0_child2");
        child0Child2.Inline.Width = 10;

        LayoutFixture.Calculate(root);

        LayoutFixture.AssertRect(root, 0, 0, 100, 100);
        LayoutFixture.AssertRect(child0, 0, 0, 100, 100);
        LayoutFixture.AssertRect(child0Child0, 0, 90, 10, 10);
        LayoutFixture.AssertRect(child0Child1, 0, 100, 10, 0);
        LayoutFixture.AssertRect(child0Child2, 0, 100, 10, 0);
        LayoutFixture.AssertNoLayoutBits(root);
    }

    [Fact]
    public void FlexDirectionColReverseInnerMarginBottom()
    {
        // Yoga fixture: flex_direction_col_reverse_inner_margin_bottom
        Element root = new() { Name = "root" };
        // Phase 5: root position type absolute. It does not change the layout of a root.
        root.Inline.Width = 100;
        root.Inline.Height = 100;

        Element child0 = AddChild(root, "root_child0");
        child0.Inline.FlexDirection = FlexDirection.ColumnReverse;
        child0.Inline.Width = 100;
        child0.Inline.Height = 100;

        Element child0Child0 = AddChild(child0, "root_child0_child0");
        child0Child0.Inline.Position = PositionType.Absolute;
        child0Child0.Inline.Margin = child0Child0.Inline.Margin with { Bottom = 10 };
        child0Child0.Inline.Width = 10;
        child0Child0.Inline.Height = 10;

        Element child0Child1 = AddChild(child0, "root_child0_child1");
        child0Child1.Inline.Width = 10;

        Element child0Child2 = AddChild(child0, "root_child0_child2");
        child0Child2.Inline.Width = 10;

        LayoutFixture.Calculate(root);

        LayoutFixture.AssertRect(root, 0, 0, 100, 100);
        LayoutFixture.AssertRect(child0, 0, 0, 100, 100);
        LayoutFixture.AssertRect(child0Child0, 0, 80, 10, 10);
        LayoutFixture.AssertRect(child0Child1, 0, 100, 10, 0);
        LayoutFixture.AssertRect(child0Child2, 0, 100, 10, 0);
        LayoutFixture.AssertNoLayoutBits(root);
    }

    [Fact]
    public void FlexDirectionRowReverseInnerBorderLeft()
    {
        // Yoga fixture: flex_direction_row_reverse_inner_border_left
        Element root = new() { Name = "root" };
        // Phase 5: root position type absolute. It does not change the layout of a root.
        root.Inline.Width = 100;
        root.Inline.Height = 100;

        Element child0 = AddChild(root, "root_child0");
        child0.Inline.FlexDirection = FlexDirection.RowReverse;
        child0.Inline.Width = 100;
        child0.Inline.Height = 100;

        Element child0Child0 = AddChild(child0, "root_child0_child0");
        child0Child0.Inline.Position = PositionType.Absolute;
        child0Child0.Inline.BorderWidth = child0Child0.Inline.BorderWidth with { Left = 10 };
        child0Child0.Inline.Width = 10;
        child0Child0.Inline.Height = 10;

        Element child0Child1 = AddChild(child0, "root_child0_child1");
        child0Child1.Inline.Width = 10;

        Element child0Child2 = AddChild(child0, "root_child0_child2");
        child0Child2.Inline.Width = 10;

        LayoutFixture.Calculate(root);

        LayoutFixture.AssertRect(root, 0, 0, 100, 100);
        LayoutFixture.AssertRect(child0, 0, 0, 100, 100);
        LayoutFixture.AssertRect(child0Child0, 90, 0, 10, 10);
        LayoutFixture.AssertRect(child0Child1, 90, 0, 10, 100);
        LayoutFixture.AssertRect(child0Child2, 80, 0, 10, 100);
        LayoutFixture.AssertNoLayoutBits(root);
    }

    [Fact]
    public void FlexDirectionRowReverseInnerBorderRight()
    {
        // Yoga fixture: flex_direction_row_reverse_inner_border_right
        Element root = new() { Name = "root" };
        // Phase 5: root position type absolute. It does not change the layout of a root.
        root.Inline.Width = 100;
        root.Inline.Height = 100;

        Element child0 = AddChild(root, "root_child0");
        child0.Inline.FlexDirection = FlexDirection.RowReverse;
        child0.Inline.Width = 100;
        child0.Inline.Height = 100;

        Element child0Child0 = AddChild(child0, "root_child0_child0");
        child0Child0.Inline.Position = PositionType.Absolute;
        child0Child0.Inline.BorderWidth = child0Child0.Inline.BorderWidth with { Right = 10 };
        child0Child0.Inline.Width = 10;
        child0Child0.Inline.Height = 10;

        Element child0Child1 = AddChild(child0, "root_child0_child1");
        child0Child1.Inline.Width = 10;

        Element child0Child2 = AddChild(child0, "root_child0_child2");
        child0Child2.Inline.Width = 10;

        LayoutFixture.Calculate(root);

        LayoutFixture.AssertRect(root, 0, 0, 100, 100);
        LayoutFixture.AssertRect(child0, 0, 0, 100, 100);
        LayoutFixture.AssertRect(child0Child0, 90, 0, 10, 10);
        LayoutFixture.AssertRect(child0Child1, 90, 0, 10, 100);
        LayoutFixture.AssertRect(child0Child2, 80, 0, 10, 100);
        LayoutFixture.AssertNoLayoutBits(root);
    }

    [Fact]
    public void FlexDirectionColReverseInnerBorderTop()
    {
        // Yoga fixture: flex_direction_col_reverse_inner_border_top
        Element root = new() { Name = "root" };
        // Phase 5: root position type absolute. It does not change the layout of a root.
        root.Inline.Width = 100;
        root.Inline.Height = 100;

        Element child0 = AddChild(root, "root_child0");
        child0.Inline.FlexDirection = FlexDirection.ColumnReverse;
        child0.Inline.Width = 100;
        child0.Inline.Height = 100;

        Element child0Child0 = AddChild(child0, "root_child0_child0");
        child0Child0.Inline.Position = PositionType.Absolute;
        child0Child0.Inline.BorderWidth = child0Child0.Inline.BorderWidth with { Top = 10 };
        child0Child0.Inline.Width = 10;
        child0Child0.Inline.Height = 10;

        Element child0Child1 = AddChild(child0, "root_child0_child1");
        child0Child1.Inline.Width = 10;

        Element child0Child2 = AddChild(child0, "root_child0_child2");
        child0Child2.Inline.Width = 10;

        LayoutFixture.Calculate(root);

        LayoutFixture.AssertRect(root, 0, 0, 100, 100);
        LayoutFixture.AssertRect(child0, 0, 0, 100, 100);
        LayoutFixture.AssertRect(child0Child0, 0, 90, 10, 10);
        LayoutFixture.AssertRect(child0Child1, 0, 100, 10, 0);
        LayoutFixture.AssertRect(child0Child2, 0, 100, 10, 0);
        LayoutFixture.AssertNoLayoutBits(root);
    }

    [Fact]
    public void FlexDirectionColReverseInnerBorderBottom()
    {
        // Yoga fixture: flex_direction_col_reverse_inner_border_bottom
        Element root = new() { Name = "root" };
        // Phase 5: root position type absolute. It does not change the layout of a root.
        root.Inline.Width = 100;
        root.Inline.Height = 100;

        Element child0 = AddChild(root, "root_child0");
        child0.Inline.FlexDirection = FlexDirection.ColumnReverse;
        child0.Inline.Width = 100;
        child0.Inline.Height = 100;

        Element child0Child0 = AddChild(child0, "root_child0_child0");
        child0Child0.Inline.Position = PositionType.Absolute;
        child0Child0.Inline.BorderWidth = child0Child0.Inline.BorderWidth with { Bottom = 10 };
        child0Child0.Inline.Width = 10;
        child0Child0.Inline.Height = 10;

        Element child0Child1 = AddChild(child0, "root_child0_child1");
        child0Child1.Inline.Width = 10;

        Element child0Child2 = AddChild(child0, "root_child0_child2");
        child0Child2.Inline.Width = 10;

        LayoutFixture.Calculate(root);

        LayoutFixture.AssertRect(root, 0, 0, 100, 100);
        LayoutFixture.AssertRect(child0, 0, 0, 100, 100);
        LayoutFixture.AssertRect(child0Child0, 0, 90, 10, 10);
        LayoutFixture.AssertRect(child0Child1, 0, 100, 10, 0);
        LayoutFixture.AssertRect(child0Child2, 0, 100, 10, 0);
        LayoutFixture.AssertNoLayoutBits(root);
    }

    [Fact]
    public void FlexDirectionRowReverseInnerBorderStart()
    {
        // Yoga fixture: flex_direction_row_reverse_inner_border_start
        // The generated test sets the left edge, not the start edge.
        Element root = new() { Name = "root" };
        // Phase 5: root position type absolute. It does not change the layout of a root.
        root.Inline.Width = 100;
        root.Inline.Height = 100;

        Element child0 = AddChild(root, "root_child0");
        child0.Inline.FlexDirection = FlexDirection.RowReverse;
        child0.Inline.Width = 100;
        child0.Inline.Height = 100;

        Element child0Child0 = AddChild(child0, "root_child0_child0");
        child0Child0.Inline.Position = PositionType.Absolute;
        child0Child0.Inline.BorderWidth = child0Child0.Inline.BorderWidth with { Left = 10 };
        child0Child0.Inline.Width = 10;
        child0Child0.Inline.Height = 10;

        Element child0Child1 = AddChild(child0, "root_child0_child1");
        child0Child1.Inline.Width = 10;

        Element child0Child2 = AddChild(child0, "root_child0_child2");
        child0Child2.Inline.Width = 10;

        LayoutFixture.Calculate(root);

        LayoutFixture.AssertRect(root, 0, 0, 100, 100);
        LayoutFixture.AssertRect(child0, 0, 0, 100, 100);
        LayoutFixture.AssertRect(child0Child0, 90, 0, 10, 10);
        LayoutFixture.AssertRect(child0Child1, 90, 0, 10, 100);
        LayoutFixture.AssertRect(child0Child2, 80, 0, 10, 100);
        LayoutFixture.AssertNoLayoutBits(root);
    }

    [Fact]
    public void FlexDirectionRowReverseInnerBorderEnd()
    {
        // Yoga fixture: flex_direction_row_reverse_inner_border_end
        // The generated test sets the right edge, not the end edge.
        Element root = new() { Name = "root" };
        // Phase 5: root position type absolute. It does not change the layout of a root.
        root.Inline.Width = 100;
        root.Inline.Height = 100;

        Element child0 = AddChild(root, "root_child0");
        child0.Inline.FlexDirection = FlexDirection.RowReverse;
        child0.Inline.Width = 100;
        child0.Inline.Height = 100;

        Element child0Child0 = AddChild(child0, "root_child0_child0");
        child0Child0.Inline.Position = PositionType.Absolute;
        child0Child0.Inline.BorderWidth = child0Child0.Inline.BorderWidth with { Right = 10 };
        child0Child0.Inline.Width = 10;
        child0Child0.Inline.Height = 10;

        Element child0Child1 = AddChild(child0, "root_child0_child1");
        child0Child1.Inline.Width = 10;

        Element child0Child2 = AddChild(child0, "root_child0_child2");
        child0Child2.Inline.Width = 10;

        LayoutFixture.Calculate(root);

        LayoutFixture.AssertRect(root, 0, 0, 100, 100);
        LayoutFixture.AssertRect(child0, 0, 0, 100, 100);
        LayoutFixture.AssertRect(child0Child0, 90, 0, 10, 10);
        LayoutFixture.AssertRect(child0Child1, 90, 0, 10, 100);
        LayoutFixture.AssertRect(child0Child2, 80, 0, 10, 100);
        LayoutFixture.AssertNoLayoutBits(root);
    }

    [Fact]
    public void FlexDirectionRowReverseInnerPaddingLeft()
    {
        // Yoga fixture: flex_direction_row_reverse_inner_padding_left
        Element root = new() { Name = "root" };
        // Phase 5: root position type absolute. It does not change the layout of a root.
        root.Inline.Width = 100;
        root.Inline.Height = 100;

        Element child0 = AddChild(root, "root_child0");
        child0.Inline.FlexDirection = FlexDirection.RowReverse;
        child0.Inline.Width = 100;
        child0.Inline.Height = 100;

        Element child0Child0 = AddChild(child0, "root_child0_child0");
        child0Child0.Inline.Position = PositionType.Absolute;
        child0Child0.Inline.Padding = child0Child0.Inline.Padding with { Left = 10 };
        child0Child0.Inline.Width = 10;
        child0Child0.Inline.Height = 10;

        Element child0Child1 = AddChild(child0, "root_child0_child1");
        child0Child1.Inline.Width = 10;

        Element child0Child2 = AddChild(child0, "root_child0_child2");
        child0Child2.Inline.Width = 10;

        LayoutFixture.Calculate(root);

        LayoutFixture.AssertRect(root, 0, 0, 100, 100);
        LayoutFixture.AssertRect(child0, 0, 0, 100, 100);
        LayoutFixture.AssertRect(child0Child0, 90, 0, 10, 10);
        LayoutFixture.AssertRect(child0Child1, 90, 0, 10, 100);
        LayoutFixture.AssertRect(child0Child2, 80, 0, 10, 100);
        LayoutFixture.AssertNoLayoutBits(root);
    }

    [Fact]
    public void FlexDirectionRowReverseInnerPaddingRight()
    {
        // Yoga fixture: flex_direction_row_reverse_inner_padding_right
        Element root = new() { Name = "root" };
        // Phase 5: root position type absolute. It does not change the layout of a root.
        root.Inline.Width = 100;
        root.Inline.Height = 100;

        Element child0 = AddChild(root, "root_child0");
        child0.Inline.FlexDirection = FlexDirection.RowReverse;
        child0.Inline.Width = 100;
        child0.Inline.Height = 100;

        Element child0Child0 = AddChild(child0, "root_child0_child0");
        child0Child0.Inline.Position = PositionType.Absolute;
        child0Child0.Inline.Padding = child0Child0.Inline.Padding with { Right = 10 };
        child0Child0.Inline.Width = 10;
        child0Child0.Inline.Height = 10;

        Element child0Child1 = AddChild(child0, "root_child0_child1");
        child0Child1.Inline.Width = 10;

        Element child0Child2 = AddChild(child0, "root_child0_child2");
        child0Child2.Inline.Width = 10;

        LayoutFixture.Calculate(root);

        LayoutFixture.AssertRect(root, 0, 0, 100, 100);
        LayoutFixture.AssertRect(child0, 0, 0, 100, 100);
        LayoutFixture.AssertRect(child0Child0, 90, 0, 10, 10);
        LayoutFixture.AssertRect(child0Child1, 90, 0, 10, 100);
        LayoutFixture.AssertRect(child0Child2, 80, 0, 10, 100);
        LayoutFixture.AssertNoLayoutBits(root);
    }

    [Fact]
    public void FlexDirectionColReverseInnerPaddingTop()
    {
        // Yoga fixture: flex_direction_col_reverse_inner_padding_top
        Element root = new() { Name = "root" };
        // Phase 5: root position type absolute. It does not change the layout of a root.
        root.Inline.Width = 100;
        root.Inline.Height = 100;

        Element child0 = AddChild(root, "root_child0");
        child0.Inline.FlexDirection = FlexDirection.ColumnReverse;
        child0.Inline.Width = 100;
        child0.Inline.Height = 100;

        Element child0Child0 = AddChild(child0, "root_child0_child0");
        child0Child0.Inline.Position = PositionType.Absolute;
        child0Child0.Inline.Padding = child0Child0.Inline.Padding with { Top = 10 };
        child0Child0.Inline.Width = 10;
        child0Child0.Inline.Height = 10;

        Element child0Child1 = AddChild(child0, "root_child0_child1");
        child0Child1.Inline.Width = 10;

        Element child0Child2 = AddChild(child0, "root_child0_child2");
        child0Child2.Inline.Width = 10;

        LayoutFixture.Calculate(root);

        LayoutFixture.AssertRect(root, 0, 0, 100, 100);
        LayoutFixture.AssertRect(child0, 0, 0, 100, 100);
        LayoutFixture.AssertRect(child0Child0, 0, 90, 10, 10);
        LayoutFixture.AssertRect(child0Child1, 0, 100, 10, 0);
        LayoutFixture.AssertRect(child0Child2, 0, 100, 10, 0);
        LayoutFixture.AssertNoLayoutBits(root);
    }

    [Fact]
    public void FlexDirectionColReverseInnerPaddingBottom()
    {
        // Yoga fixture: flex_direction_col_reverse_inner_padding_bottom
        Element root = new() { Name = "root" };
        // Phase 5: root position type absolute. It does not change the layout of a root.
        root.Inline.Width = 100;
        root.Inline.Height = 100;

        Element child0 = AddChild(root, "root_child0");
        child0.Inline.FlexDirection = FlexDirection.ColumnReverse;
        child0.Inline.Width = 100;
        child0.Inline.Height = 100;

        Element child0Child0 = AddChild(child0, "root_child0_child0");
        child0Child0.Inline.Position = PositionType.Absolute;
        child0Child0.Inline.Padding = child0Child0.Inline.Padding with { Bottom = 10 };
        child0Child0.Inline.Width = 10;
        child0Child0.Inline.Height = 10;

        Element child0Child1 = AddChild(child0, "root_child0_child1");
        child0Child1.Inline.Width = 10;

        Element child0Child2 = AddChild(child0, "root_child0_child2");
        child0Child2.Inline.Width = 10;

        LayoutFixture.Calculate(root);

        LayoutFixture.AssertRect(root, 0, 0, 100, 100);
        LayoutFixture.AssertRect(child0, 0, 0, 100, 100);
        LayoutFixture.AssertRect(child0Child0, 0, 90, 10, 10);
        LayoutFixture.AssertRect(child0Child1, 0, 100, 10, 0);
        LayoutFixture.AssertRect(child0Child2, 0, 100, 10, 0);
        LayoutFixture.AssertNoLayoutBits(root);
    }

    [Fact]
    public void FlexDirectionAlternatingWithPercent()
    {
        // Yoga fixture: flex_direction_alternating_with_percent
        Element root = new() { Name = "root" };
        root.Inline.Position = PositionType.Absolute;
        root.Inline.Width = 200;
        root.Inline.Height = 300;

        Element child0 = AddChild(root, "root_child0");
        child0.Inline.FlexDirection = FlexDirection.Row;
        child0.Inline.Left = Length.Percent(10);
        child0.Inline.Top = Length.Percent(10);
        child0.Inline.Width = Length.Percent(50);
        child0.Inline.Height = Length.Percent(50);

        LayoutFixture.Calculate(root);

        LayoutFixture.AssertRect(root, 0, 0, 200, 300);
        LayoutFixture.AssertRect(child0, 20, 30, 100, 150);
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
