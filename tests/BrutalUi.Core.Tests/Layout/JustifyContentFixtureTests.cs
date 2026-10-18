// Ported from Yoga (https://github.com/facebook/yoga), v3.2.1, tests/generated/YGJustifyContentTest.cpp. Copyright (c) Meta Platforms, Inc. and affiliates.
// MIT License. See THIRD-PARTY-NOTICES.md at the repository root.

using Xunit;

namespace BrutalUi.Core.Tests;

/// <summary>
/// The Yoga fixtures of <c>YGJustifyContentTest</c>, the LTR half only.
/// </summary>
/// <remarks>
/// Each Yoga fixture root sets <c>position: absolute</c>. The ports of this file were written before the engine had a position type.
/// For a root, the position type does not change the layout, so the ports omit that line.
/// The ports added later keep that line, because the engine has the position type now.
/// </remarks>
public sealed class JustifyContentFixtureTests
{
    [Fact]
    public void JustifyContentRowFlexStart()
    {
        // Yoga fixture: justify_content_row_flex_start
        Element root = new() { Name = "root" };
        root.Style.FlexDirection = FlexDirection.Row;
        // Phase 5: root position type absolute. It does not change the layout of a root.
        root.Style.Width = 102;
        root.Style.Height = 102;

        Element child0 = AddChild(root, "root_child0");
        child0.Style.Width = 10;

        Element child1 = AddChild(root, "root_child1");
        child1.Style.Width = 10;

        Element child2 = AddChild(root, "root_child2");
        child2.Style.Width = 10;

        LayoutFixture.Calculate(root);

        LayoutFixture.AssertRect(root, 0, 0, 102, 102);
        LayoutFixture.AssertRect(child0, 0, 0, 10, 102);
        LayoutFixture.AssertRect(child1, 10, 0, 10, 102);
        LayoutFixture.AssertRect(child2, 20, 0, 10, 102);
        LayoutFixture.AssertNoLayoutBits(root);
    }

    [Fact]
    public void JustifyContentRowFlexEnd()
    {
        // Yoga fixture: justify_content_row_flex_end
        Element root = new() { Name = "root" };
        root.Style.FlexDirection = FlexDirection.Row;
        root.Style.JustifyContent = Justify.FlexEnd;
        // Phase 5: root position type absolute. It does not change the layout of a root.
        root.Style.Width = 102;
        root.Style.Height = 102;

        Element child0 = AddChild(root, "root_child0");
        child0.Style.Width = 10;

        Element child1 = AddChild(root, "root_child1");
        child1.Style.Width = 10;

        Element child2 = AddChild(root, "root_child2");
        child2.Style.Width = 10;

        LayoutFixture.Calculate(root);

        LayoutFixture.AssertRect(root, 0, 0, 102, 102);
        LayoutFixture.AssertRect(child0, 72, 0, 10, 102);
        LayoutFixture.AssertRect(child1, 82, 0, 10, 102);
        LayoutFixture.AssertRect(child2, 92, 0, 10, 102);
        LayoutFixture.AssertNoLayoutBits(root);
    }

    [Fact]
    public void JustifyContentRowCenter()
    {
        // Yoga fixture: justify_content_row_center
        Element root = new() { Name = "root" };
        root.Style.FlexDirection = FlexDirection.Row;
        root.Style.JustifyContent = Justify.Center;
        // Phase 5: root position type absolute. It does not change the layout of a root.
        root.Style.Width = 102;
        root.Style.Height = 102;

        Element child0 = AddChild(root, "root_child0");
        child0.Style.Width = 10;

        Element child1 = AddChild(root, "root_child1");
        child1.Style.Width = 10;

        Element child2 = AddChild(root, "root_child2");
        child2.Style.Width = 10;

        LayoutFixture.Calculate(root);

        LayoutFixture.AssertRect(root, 0, 0, 102, 102);
        LayoutFixture.AssertRect(child0, 36, 0, 10, 102);
        LayoutFixture.AssertRect(child1, 46, 0, 10, 102);
        LayoutFixture.AssertRect(child2, 56, 0, 10, 102);
        LayoutFixture.AssertNoLayoutBits(root);
    }

    [Fact]
    public void JustifyContentRowSpaceBetween()
    {
        // Yoga fixture: justify_content_row_space_between
        Element root = new() { Name = "root" };
        root.Style.FlexDirection = FlexDirection.Row;
        root.Style.JustifyContent = Justify.SpaceBetween;
        // Phase 5: root position type absolute. It does not change the layout of a root.
        root.Style.Width = 102;
        root.Style.Height = 102;

        Element child0 = AddChild(root, "root_child0");
        child0.Style.Width = 10;

        Element child1 = AddChild(root, "root_child1");
        child1.Style.Width = 10;

        Element child2 = AddChild(root, "root_child2");
        child2.Style.Width = 10;

        LayoutFixture.Calculate(root);

        LayoutFixture.AssertRect(root, 0, 0, 102, 102);
        LayoutFixture.AssertRect(child0, 0, 0, 10, 102);
        LayoutFixture.AssertRect(child1, 46, 0, 10, 102);
        LayoutFixture.AssertRect(child2, 92, 0, 10, 102);
        LayoutFixture.AssertNoLayoutBits(root);
    }

    [Fact]
    public void JustifyContentRowSpaceAround()
    {
        // Yoga fixture: justify_content_row_space_around
        Element root = new() { Name = "root" };
        root.Style.FlexDirection = FlexDirection.Row;
        root.Style.JustifyContent = Justify.SpaceAround;
        // Phase 5: root position type absolute. It does not change the layout of a root.
        root.Style.Width = 102;
        root.Style.Height = 102;

        Element child0 = AddChild(root, "root_child0");
        child0.Style.Width = 10;

        Element child1 = AddChild(root, "root_child1");
        child1.Style.Width = 10;

        Element child2 = AddChild(root, "root_child2");
        child2.Style.Width = 10;

        LayoutFixture.Calculate(root);

        LayoutFixture.AssertRect(root, 0, 0, 102, 102);
        LayoutFixture.AssertRect(child0, 12, 0, 10, 102);
        LayoutFixture.AssertRect(child1, 46, 0, 10, 102);
        LayoutFixture.AssertRect(child2, 80, 0, 10, 102);
        LayoutFixture.AssertNoLayoutBits(root);
    }

    [Fact]
    public void JustifyContentColumnFlexStart()
    {
        // Yoga fixture: justify_content_column_flex_start
        Element root = new() { Name = "root" };
        // Phase 5: root position type absolute. It does not change the layout of a root.
        root.Style.Width = 102;
        root.Style.Height = 102;

        Element child0 = AddChild(root, "root_child0");
        child0.Style.Height = 10;

        Element child1 = AddChild(root, "root_child1");
        child1.Style.Height = 10;

        Element child2 = AddChild(root, "root_child2");
        child2.Style.Height = 10;

        LayoutFixture.Calculate(root);

        LayoutFixture.AssertRect(root, 0, 0, 102, 102);
        LayoutFixture.AssertRect(child0, 0, 0, 102, 10);
        LayoutFixture.AssertRect(child1, 0, 10, 102, 10);
        LayoutFixture.AssertRect(child2, 0, 20, 102, 10);
        LayoutFixture.AssertNoLayoutBits(root);
    }

    [Fact]
    public void JustifyContentColumnFlexEnd()
    {
        // Yoga fixture: justify_content_column_flex_end
        Element root = new() { Name = "root" };
        root.Style.JustifyContent = Justify.FlexEnd;
        // Phase 5: root position type absolute. It does not change the layout of a root.
        root.Style.Width = 102;
        root.Style.Height = 102;

        Element child0 = AddChild(root, "root_child0");
        child0.Style.Height = 10;

        Element child1 = AddChild(root, "root_child1");
        child1.Style.Height = 10;

        Element child2 = AddChild(root, "root_child2");
        child2.Style.Height = 10;

        LayoutFixture.Calculate(root);

        LayoutFixture.AssertRect(root, 0, 0, 102, 102);
        LayoutFixture.AssertRect(child0, 0, 72, 102, 10);
        LayoutFixture.AssertRect(child1, 0, 82, 102, 10);
        LayoutFixture.AssertRect(child2, 0, 92, 102, 10);
        LayoutFixture.AssertNoLayoutBits(root);
    }

    [Fact]
    public void JustifyContentColumnCenter()
    {
        // Yoga fixture: justify_content_column_center
        Element root = new() { Name = "root" };
        root.Style.JustifyContent = Justify.Center;
        // Phase 5: root position type absolute. It does not change the layout of a root.
        root.Style.Width = 102;
        root.Style.Height = 102;

        Element child0 = AddChild(root, "root_child0");
        child0.Style.Height = 10;

        Element child1 = AddChild(root, "root_child1");
        child1.Style.Height = 10;

        Element child2 = AddChild(root, "root_child2");
        child2.Style.Height = 10;

        LayoutFixture.Calculate(root);

        LayoutFixture.AssertRect(root, 0, 0, 102, 102);
        LayoutFixture.AssertRect(child0, 0, 36, 102, 10);
        LayoutFixture.AssertRect(child1, 0, 46, 102, 10);
        LayoutFixture.AssertRect(child2, 0, 56, 102, 10);
        LayoutFixture.AssertNoLayoutBits(root);
    }

    [Fact]
    public void JustifyContentColumnSpaceBetween()
    {
        // Yoga fixture: justify_content_column_space_between
        Element root = new() { Name = "root" };
        root.Style.JustifyContent = Justify.SpaceBetween;
        // Phase 5: root position type absolute. It does not change the layout of a root.
        root.Style.Width = 102;
        root.Style.Height = 102;

        Element child0 = AddChild(root, "root_child0");
        child0.Style.Height = 10;

        Element child1 = AddChild(root, "root_child1");
        child1.Style.Height = 10;

        Element child2 = AddChild(root, "root_child2");
        child2.Style.Height = 10;

        LayoutFixture.Calculate(root);

        LayoutFixture.AssertRect(root, 0, 0, 102, 102);
        LayoutFixture.AssertRect(child0, 0, 0, 102, 10);
        LayoutFixture.AssertRect(child1, 0, 46, 102, 10);
        LayoutFixture.AssertRect(child2, 0, 92, 102, 10);
        LayoutFixture.AssertNoLayoutBits(root);
    }

    [Fact]
    public void JustifyContentColumnSpaceAround()
    {
        // Yoga fixture: justify_content_column_space_around
        Element root = new() { Name = "root" };
        root.Style.JustifyContent = Justify.SpaceAround;
        // Phase 5: root position type absolute. It does not change the layout of a root.
        root.Style.Width = 102;
        root.Style.Height = 102;

        Element child0 = AddChild(root, "root_child0");
        child0.Style.Height = 10;

        Element child1 = AddChild(root, "root_child1");
        child1.Style.Height = 10;

        Element child2 = AddChild(root, "root_child2");
        child2.Style.Height = 10;

        LayoutFixture.Calculate(root);

        LayoutFixture.AssertRect(root, 0, 0, 102, 102);
        LayoutFixture.AssertRect(child0, 0, 12, 102, 10);
        LayoutFixture.AssertRect(child1, 0, 46, 102, 10);
        LayoutFixture.AssertRect(child2, 0, 80, 102, 10);
        LayoutFixture.AssertNoLayoutBits(root);
    }

    [Fact]
    public void JustifyContentRowMinWidthAndMargin()
    {
        // Yoga fixture: justify_content_row_min_width_and_margin
        Element root = new() { Name = "root" };
        root.Style.FlexDirection = FlexDirection.Row;
        root.Style.JustifyContent = Justify.Center;
        // Phase 5: root position type absolute. It does not change the layout of a root.
        root.Style.Margin.Left = 100;
        root.Style.MinWidth = 50;

        Element child0 = AddChild(root, "root_child0");
        child0.Style.Width = 20;
        child0.Style.Height = 20;

        LayoutFixture.Calculate(root);

        LayoutFixture.AssertRect(root, 100, 0, 50, 20);
        LayoutFixture.AssertRect(child0, 15, 0, 20, 20);
        LayoutFixture.AssertNoLayoutBits(root);
    }

    [Fact]
    public void JustifyContentRowMaxWidthAndMargin()
    {
        // Yoga fixture: justify_content_row_max_width_and_margin
        Element root = new() { Name = "root" };
        root.Style.FlexDirection = FlexDirection.Row;
        root.Style.JustifyContent = Justify.Center;
        // Phase 5: root position type absolute. It does not change the layout of a root.
        root.Style.Margin.Left = 100;
        root.Style.Width = 100;
        root.Style.MaxWidth = 80;

        Element child0 = AddChild(root, "root_child0");
        child0.Style.Width = 20;
        child0.Style.Height = 20;

        LayoutFixture.Calculate(root);

        LayoutFixture.AssertRect(root, 100, 0, 80, 20);
        LayoutFixture.AssertRect(child0, 30, 0, 20, 20);
        LayoutFixture.AssertNoLayoutBits(root);
    }

    [Fact]
    public void JustifyContentColumnMinHeightAndMargin()
    {
        // Yoga fixture: justify_content_column_min_height_and_margin
        Element root = new() { Name = "root" };
        root.Style.JustifyContent = Justify.Center;
        // Phase 5: root position type absolute. It does not change the layout of a root.
        root.Style.Margin.Top = 100;
        root.Style.MinHeight = 50;

        Element child0 = AddChild(root, "root_child0");
        child0.Style.Width = 20;
        child0.Style.Height = 20;

        LayoutFixture.Calculate(root);

        LayoutFixture.AssertRect(root, 0, 100, 20, 50);
        LayoutFixture.AssertRect(child0, 0, 15, 20, 20);
        LayoutFixture.AssertNoLayoutBits(root);
    }

    [Fact]
    public void JustifyContentColumnMaxHeightAndMargin()
    {
        // Yoga fixture: justify_content_column_max_height_and_margin
        Element root = new() { Name = "root" };
        root.Style.JustifyContent = Justify.Center;
        // Phase 5: root position type absolute. It does not change the layout of a root.
        root.Style.Margin.Top = 100;
        root.Style.Height = 100;
        root.Style.MaxHeight = 80;

        Element child0 = AddChild(root, "root_child0");
        child0.Style.Width = 20;
        child0.Style.Height = 20;

        LayoutFixture.Calculate(root);

        LayoutFixture.AssertRect(root, 0, 100, 20, 80);
        LayoutFixture.AssertRect(child0, 0, 30, 20, 20);
        LayoutFixture.AssertNoLayoutBits(root);
    }

    [Fact]
    public void JustifyContentColumnSpaceEvenly()
    {
        // Yoga fixture: justify_content_column_space_evenly
        Element root = new() { Name = "root" };
        root.Style.JustifyContent = Justify.SpaceEvenly;
        // Phase 5: root position type absolute. It does not change the layout of a root.
        root.Style.Width = 102;
        root.Style.Height = 102;

        Element child0 = AddChild(root, "root_child0");
        child0.Style.Height = 10;

        Element child1 = AddChild(root, "root_child1");
        child1.Style.Height = 10;

        Element child2 = AddChild(root, "root_child2");
        child2.Style.Height = 10;

        LayoutFixture.Calculate(root);

        LayoutFixture.AssertRect(root, 0, 0, 102, 102);
        LayoutFixture.AssertRect(child0, 0, 18, 102, 10);
        LayoutFixture.AssertRect(child1, 0, 46, 102, 10);
        LayoutFixture.AssertRect(child2, 0, 74, 102, 10);
        LayoutFixture.AssertNoLayoutBits(root);
    }

    [Fact]
    public void JustifyContentFlexStartRowReverse()
    {
        // Yoga fixture: justify_content_flex_start_row_reverse
        Element root = new() { Name = "root" };
        root.Style.FlexDirection = FlexDirection.RowReverse;
        // Phase 5: root position type absolute. It does not change the layout of a root.
        root.Style.Width = 100;
        root.Style.Height = 100;

        Element child0 = AddChild(root, "root_child0");
        child0.Style.Width = 20;

        Element child1 = AddChild(root, "root_child1");
        child1.Style.Width = 20;

        Element child2 = AddChild(root, "root_child2");
        child2.Style.Width = 20;

        LayoutFixture.Calculate(root);

        LayoutFixture.AssertRect(root, 0, 0, 100, 100);
        LayoutFixture.AssertRect(child0, 80, 0, 20, 100);
        LayoutFixture.AssertRect(child1, 60, 0, 20, 100);
        LayoutFixture.AssertRect(child2, 40, 0, 20, 100);
        LayoutFixture.AssertNoLayoutBits(root);
    }

    [Fact]
    public void JustifyContentFlexEndRowReverse()
    {
        // Yoga fixture: justify_content_flex_end_row_reverse
        // The Yoga fixture sets justify-content: flex-start in the HTML source, so the generated test sets no justify.
        Element root = new() { Name = "root" };
        root.Style.FlexDirection = FlexDirection.RowReverse;
        // Phase 5: root position type absolute. It does not change the layout of a root.
        root.Style.Width = 100;
        root.Style.Height = 100;

        Element child0 = AddChild(root, "root_child0");
        child0.Style.Width = 20;

        Element child1 = AddChild(root, "root_child1");
        child1.Style.Width = 20;

        Element child2 = AddChild(root, "root_child2");
        child2.Style.Width = 20;

        LayoutFixture.Calculate(root);

        LayoutFixture.AssertRect(root, 0, 0, 100, 100);
        LayoutFixture.AssertRect(child0, 80, 0, 20, 100);
        LayoutFixture.AssertRect(child1, 60, 0, 20, 100);
        LayoutFixture.AssertRect(child2, 40, 0, 20, 100);
        LayoutFixture.AssertNoLayoutBits(root);
    }

    [Fact]
    public void JustifyContentOverflowRowFlexStart()
    {
        // Yoga fixture: justify_content_overflow_row_flex_start
        Element root = new() { Name = "root" };
        root.Style.FlexDirection = FlexDirection.Row;
        // Phase 5: root position type absolute. It does not change the layout of a root.
        root.Style.Width = 102;
        root.Style.Height = 102;

        Element child0 = AddChild(root, "root_child0");
        child0.Style.Width = 40;

        Element child1 = AddChild(root, "root_child1");
        child1.Style.Width = 40;

        Element child2 = AddChild(root, "root_child2");
        child2.Style.Width = 40;

        LayoutFixture.Calculate(root);

        LayoutFixture.AssertRect(root, 0, 0, 102, 102);
        LayoutFixture.AssertRect(child0, 0, 0, 40, 102);
        LayoutFixture.AssertRect(child1, 40, 0, 40, 102);
        LayoutFixture.AssertRect(child2, 80, 0, 40, 102);
        LayoutFixture.AssertNoLayoutBits(root);
    }

    [Fact]
    public void JustifyContentOverflowRowFlexEnd()
    {
        // Yoga fixture: justify_content_overflow_row_flex_end
        Element root = new() { Name = "root" };
        root.Style.FlexDirection = FlexDirection.Row;
        root.Style.JustifyContent = Justify.FlexEnd;
        // Phase 5: root position type absolute. It does not change the layout of a root.
        root.Style.Width = 102;
        root.Style.Height = 102;

        Element child0 = AddChild(root, "root_child0");
        child0.Style.Width = 40;

        Element child1 = AddChild(root, "root_child1");
        child1.Style.Width = 40;

        Element child2 = AddChild(root, "root_child2");
        child2.Style.Width = 40;

        LayoutFixture.Calculate(root);

        LayoutFixture.AssertRect(root, 0, 0, 102, 102);
        LayoutFixture.AssertRect(child0, -18, 0, 40, 102);
        LayoutFixture.AssertRect(child1, 22, 0, 40, 102);
        LayoutFixture.AssertRect(child2, 62, 0, 40, 102);
        LayoutFixture.AssertNoLayoutBits(root);
    }

    [Fact]
    public void JustifyContentOverflowRowCenter()
    {
        // Yoga fixture: justify_content_overflow_row_center
        Element root = new() { Name = "root" };
        root.Style.FlexDirection = FlexDirection.Row;
        root.Style.JustifyContent = Justify.Center;
        // Phase 5: root position type absolute. It does not change the layout of a root.
        root.Style.Width = 102;
        root.Style.Height = 102;

        Element child0 = AddChild(root, "root_child0");
        child0.Style.Width = 40;

        Element child1 = AddChild(root, "root_child1");
        child1.Style.Width = 40;

        Element child2 = AddChild(root, "root_child2");
        child2.Style.Width = 40;

        LayoutFixture.Calculate(root);

        LayoutFixture.AssertRect(root, 0, 0, 102, 102);
        LayoutFixture.AssertRect(child0, -9, 0, 40, 102);
        LayoutFixture.AssertRect(child1, 31, 0, 40, 102);
        LayoutFixture.AssertRect(child2, 71, 0, 40, 102);
        LayoutFixture.AssertNoLayoutBits(root);
    }

    [Fact]
    public void JustifyContentOverflowRowSpaceBetween()
    {
        // Yoga fixture: justify_content_overflow_row_space_between
        Element root = new() { Name = "root" };
        root.Style.FlexDirection = FlexDirection.Row;
        root.Style.JustifyContent = Justify.SpaceBetween;
        // Phase 5: root position type absolute. It does not change the layout of a root.
        root.Style.Width = 102;
        root.Style.Height = 102;

        Element child0 = AddChild(root, "root_child0");
        child0.Style.Width = 40;

        Element child1 = AddChild(root, "root_child1");
        child1.Style.Width = 40;

        Element child2 = AddChild(root, "root_child2");
        child2.Style.Width = 40;

        LayoutFixture.Calculate(root);

        LayoutFixture.AssertRect(root, 0, 0, 102, 102);
        LayoutFixture.AssertRect(child0, 0, 0, 40, 102);
        LayoutFixture.AssertRect(child1, 40, 0, 40, 102);
        LayoutFixture.AssertRect(child2, 80, 0, 40, 102);
        LayoutFixture.AssertNoLayoutBits(root);
    }

    [Fact]
    public void JustifyContentOverflowRowSpaceAround()
    {
        // Yoga fixture: justify_content_overflow_row_space_around
        Element root = new() { Name = "root" };
        root.Style.FlexDirection = FlexDirection.Row;
        root.Style.JustifyContent = Justify.SpaceAround;
        // Phase 5: root position type absolute. It does not change the layout of a root.
        root.Style.Width = 102;
        root.Style.Height = 102;

        Element child0 = AddChild(root, "root_child0");
        child0.Style.Width = 40;

        Element child1 = AddChild(root, "root_child1");
        child1.Style.Width = 40;

        Element child2 = AddChild(root, "root_child2");
        child2.Style.Width = 40;

        LayoutFixture.Calculate(root);

        LayoutFixture.AssertRect(root, 0, 0, 102, 102);
        LayoutFixture.AssertRect(child0, 0, 0, 40, 102);
        LayoutFixture.AssertRect(child1, 40, 0, 40, 102);
        LayoutFixture.AssertRect(child2, 80, 0, 40, 102);
        LayoutFixture.AssertNoLayoutBits(root);
    }

    [Fact]
    public void JustifyContentOverflowRowSpaceEvenly()
    {
        // Yoga fixture: justify_content_overflow_row_space_evenly
        Element root = new() { Name = "root" };
        root.Style.FlexDirection = FlexDirection.Row;
        root.Style.JustifyContent = Justify.SpaceEvenly;
        // Phase 5: root position type absolute. It does not change the layout of a root.
        root.Style.Width = 102;
        root.Style.Height = 102;

        Element child0 = AddChild(root, "root_child0");
        child0.Style.Width = 40;

        Element child1 = AddChild(root, "root_child1");
        child1.Style.Width = 40;

        Element child2 = AddChild(root, "root_child2");
        child2.Style.Width = 40;

        LayoutFixture.Calculate(root);

        LayoutFixture.AssertRect(root, 0, 0, 102, 102);
        LayoutFixture.AssertRect(child0, 0, 0, 40, 102);
        LayoutFixture.AssertRect(child1, 40, 0, 40, 102);
        LayoutFixture.AssertRect(child2, 80, 0, 40, 102);
        LayoutFixture.AssertNoLayoutBits(root);
    }

    [Fact]
    public void JustifyContentSpaceBetweenIndefiniteContainerDimWithFreeSpace()
    {
        // Yoga fixture: justify_content_space_between_indefinite_container_dim_with_free_space
        Element root = new() { Name = "root" };
        root.Style.AlignItems = Align.Center;
        // Phase 5: root position type absolute. It does not change the layout of a root.
        root.Style.Width = 300;

        Element child0 = AddChild(root, "root_child0");
        child0.Style.FlexDirection = FlexDirection.Row;
        child0.Style.JustifyContent = Justify.SpaceBetween;
        child0.Style.MinWidth = 200;

        Element child0Child0 = AddChild(child0, "root_child0_child0");
        child0Child0.Style.Width = 50;
        child0Child0.Style.Height = 50;

        Element child0Child1 = AddChild(child0, "root_child0_child1");
        child0Child1.Style.Width = 50;
        child0Child1.Style.Height = 50;

        LayoutFixture.Calculate(root);

        LayoutFixture.AssertRect(root, 0, 0, 300, 50);
        LayoutFixture.AssertRect(child0, 50, 0, 200, 50);
        LayoutFixture.AssertRect(child0Child0, 0, 0, 50, 50);
        LayoutFixture.AssertRect(child0Child1, 150, 0, 50, 50);
        LayoutFixture.AssertNoLayoutBits(root);
    }

    [Fact]
    public void JustifyContentOverflowRowSpaceEvenlyAutoMargin()
    {
        // Yoga fixture: justify_content_overflow_row_space_evenly_auto_margin
        Element root = new() { Name = "root" };
        root.Style.FlexDirection = FlexDirection.Row;
        root.Style.JustifyContent = Justify.SpaceEvenly;
        root.Style.Position = PositionType.Absolute;
        root.Style.Width = 102;
        root.Style.Height = 102;

        Element child0 = AddChild(root, "root_child0");
        child0.Style.Margin.Right = Length.Auto;
        child0.Style.Width = 40;

        Element child1 = AddChild(root, "root_child1");
        child1.Style.Width = 40;

        Element child2 = AddChild(root, "root_child2");
        child2.Style.Width = 40;

        LayoutFixture.Calculate(root);

        LayoutFixture.AssertRect(root, 0, 0, 102, 102);
        LayoutFixture.AssertRect(child0, 0, 0, 40, 102);
        LayoutFixture.AssertRect(child1, 40, 0, 40, 102);
        LayoutFixture.AssertRect(child2, 80, 0, 40, 102);
        LayoutFixture.AssertNoLayoutBits(root);
    }

    [Fact]
    public void JustifyContentMinWidthWithPaddingChildWidthGreaterThanParent()
    {
        // Yoga fixture: justify_content_min_width_with_padding_child_width_greater_than_parent
        Element root = new() { Name = "root" };
        root.Style.AlignContent = Align.Stretch;
        root.Style.Position = PositionType.Absolute;
        root.Style.Width = 1000;
        root.Style.Height = 1584;

        Element child0 = AddChild(root, "root_child0");
        child0.Style.FlexDirection = FlexDirection.Row;
        child0.Style.AlignContent = Align.Stretch;

        Element child0Child0 = AddChild(child0, "root_child0_child0");
        child0Child0.Style.FlexDirection = FlexDirection.Row;
        child0Child0.Style.JustifyContent = Justify.Center;
        child0Child0.Style.AlignContent = Align.Stretch;
        child0Child0.Style.Padding.Left = 100;
        child0Child0.Style.Padding.Right = 100;
        child0Child0.Style.MinWidth = 400;

        Element child0Child0Child0 = AddChild(child0Child0, "root_child0_child0_child0");
        child0Child0Child0.Style.FlexDirection = FlexDirection.Row;
        child0Child0Child0.Style.AlignContent = Align.Stretch;
        child0Child0Child0.Style.Width = 300;
        child0Child0Child0.Style.Height = 100;

        LayoutFixture.Calculate(root);

        LayoutFixture.AssertRect(root, 0, 0, 1000, 1584);
        LayoutFixture.AssertRect(child0, 0, 0, 1000, 100);
        LayoutFixture.AssertRect(child0Child0, 0, 0, 500, 100);
        LayoutFixture.AssertRect(child0Child0Child0, 100, 0, 300, 100);
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
