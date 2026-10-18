// Ported from Yoga (https://github.com/facebook/yoga), v3.2.1, tests/generated/YGMinMaxDimensionTest.cpp. Copyright (c) Meta Platforms, Inc. and affiliates.
// MIT License. See THIRD-PARTY-NOTICES.md at the repository root.

using Xunit;

namespace BrutalUi.Core.Tests;

/// <summary>
/// The Yoga fixtures of <c>YGMinMaxDimensionTest</c>, the LTR half only.
/// </summary>
/// <remarks>
/// Each Yoga fixture root sets <c>position: absolute</c>. The ports of this file were written before the engine had a position type.
/// For a root, the position type does not change the layout, so the ports omit that line.
/// </remarks>
public sealed class MinMaxDimensionFixtureTests
{
    [Fact]
    public void MaxWidth()
    {
        // Yoga fixture: max_width
        Element root = new() { Name = "root" };
        // Phase 5: root position type absolute. It does not change the layout of a root.
        root.Style.Width = 100;
        root.Style.Height = 100;

        Element child0 = AddChild(root, "root_child0");
        child0.Style.MaxWidth = 50;
        child0.Style.Height = 10;

        LayoutFixture.Calculate(root);

        LayoutFixture.AssertRect(root, 0, 0, 100, 100);
        LayoutFixture.AssertRect(child0, 0, 0, 50, 10);
        LayoutFixture.AssertNoLayoutBits(root);
    }

    [Fact]
    public void MaxHeight()
    {
        // Yoga fixture: max_height
        Element root = new() { Name = "root" };
        root.Style.FlexDirection = FlexDirection.Row;
        // Phase 5: root position type absolute. It does not change the layout of a root.
        root.Style.Width = 100;
        root.Style.Height = 100;

        Element child0 = AddChild(root, "root_child0");
        child0.Style.Width = 10;
        child0.Style.MaxHeight = 50;

        LayoutFixture.Calculate(root);

        LayoutFixture.AssertRect(root, 0, 0, 100, 100);
        LayoutFixture.AssertRect(child0, 0, 0, 10, 50);
        LayoutFixture.AssertNoLayoutBits(root);
    }

    [Fact]
    public void JustifyContentMinMax()
    {
        // Yoga fixture: justify_content_min_max
        Element root = new() { Name = "root" };
        root.Style.JustifyContent = Justify.Center;
        // Phase 5: root position type absolute. It does not change the layout of a root.
        root.Style.Width = 100;
        root.Style.MinHeight = 100;
        root.Style.MaxHeight = 200;

        Element child0 = AddChild(root, "root_child0");
        child0.Style.Width = 60;
        child0.Style.Height = 60;

        LayoutFixture.Calculate(root);

        LayoutFixture.AssertRect(root, 0, 0, 100, 100);
        LayoutFixture.AssertRect(child0, 0, 20, 60, 60);
        LayoutFixture.AssertNoLayoutBits(root);
    }

    [Fact]
    public void JustifyContentOverflowMinMax()
    {
        // Yoga fixture: justify_content_overflow_min_max
        Element root = new() { Name = "root" };
        root.Style.JustifyContent = Justify.Center;
        // Phase 5: root position type absolute. It does not change the layout of a root.
        root.Style.MinHeight = 100;
        root.Style.MaxHeight = 110;

        Element child0 = AddChild(root, "root_child0");
        child0.Style.Width = 50;
        child0.Style.Height = 50;

        Element child1 = AddChild(root, "root_child1");
        child1.Style.Width = 50;
        child1.Style.Height = 50;

        Element child2 = AddChild(root, "root_child2");
        child2.Style.Width = 50;
        child2.Style.Height = 50;

        LayoutFixture.Calculate(root);

        LayoutFixture.AssertRect(root, 0, 0, 50, 110);
        LayoutFixture.AssertRect(child0, 0, -20, 50, 50);
        LayoutFixture.AssertRect(child1, 0, 30, 50, 50);
        LayoutFixture.AssertRect(child2, 0, 80, 50, 50);
        LayoutFixture.AssertNoLayoutBits(root);
    }

    [Fact]
    public void FlexGrowToMin()
    {
        // Yoga fixture: flex_grow_to_min
        Element root = new() { Name = "root" };
        // Phase 5: root position type absolute. It does not change the layout of a root.
        root.Style.Width = 100;
        root.Style.MinHeight = 100;
        root.Style.MaxHeight = 500;

        Element child0 = AddChild(root, "root_child0");
        child0.Style.FlexGrow = 1;
        child0.Style.FlexShrink = 1;

        Element child1 = AddChild(root, "root_child1");
        child1.Style.Height = 50;

        LayoutFixture.Calculate(root);

        LayoutFixture.AssertRect(root, 0, 0, 100, 100);
        LayoutFixture.AssertRect(child0, 0, 0, 100, 50);
        LayoutFixture.AssertRect(child1, 0, 50, 100, 50);
        LayoutFixture.AssertNoLayoutBits(root);
    }

    [Fact]
    public void FlexGrowChild()
    {
        // Yoga fixture: flex_grow_child
        Element root = new() { Name = "root" };
        root.Style.FlexDirection = FlexDirection.Row;
        // Phase 5: root position type absolute. It does not change the layout of a root.

        Element child0 = AddChild(root, "root_child0");
        child0.Style.FlexGrow = 1;
        child0.Style.FlexBasis = 0;
        child0.Style.Height = 100;

        LayoutFixture.Calculate(root);

        LayoutFixture.AssertRect(root, 0, 0, 0, 100);
        LayoutFixture.AssertRect(child0, 0, 0, 0, 100);
        LayoutFixture.AssertNoLayoutBits(root);
    }

    [Fact]
    public void FlexGrowWithinConstrainedMinMaxColumn()
    {
        // Yoga fixture: flex_grow_within_constrained_min_max_column
        Element root = new() { Name = "root" };
        // Phase 5: root position type absolute. It does not change the layout of a root.
        root.Style.MinHeight = 100;
        root.Style.MaxHeight = 200;

        Element child0 = AddChild(root, "root_child0");
        child0.Style.FlexGrow = 1;

        Element child1 = AddChild(root, "root_child1");
        child1.Style.Height = 50;

        LayoutFixture.Calculate(root);

        LayoutFixture.AssertRect(root, 0, 0, 0, 100);
        LayoutFixture.AssertRect(child0, 0, 0, 0, 50);
        LayoutFixture.AssertRect(child1, 0, 50, 0, 50);
        LayoutFixture.AssertNoLayoutBits(root);
    }

    [Fact]
    public void FlexGrowWithinMaxWidth()
    {
        // Yoga fixture: flex_grow_within_max_width
        Element root = new() { Name = "root" };
        // Phase 5: root position type absolute. It does not change the layout of a root.
        root.Style.Width = 200;
        root.Style.Height = 100;

        Element child0 = AddChild(root, "root_child0");
        child0.Style.FlexDirection = FlexDirection.Row;
        child0.Style.MaxWidth = 100;

        Element child0Child0 = AddChild(child0, "root_child0_child0");
        child0Child0.Style.FlexGrow = 1;
        child0Child0.Style.Height = 20;

        LayoutFixture.Calculate(root);

        LayoutFixture.AssertRect(root, 0, 0, 200, 100);
        LayoutFixture.AssertRect(child0, 0, 0, 100, 20);
        LayoutFixture.AssertRect(child0Child0, 0, 0, 100, 20);
        LayoutFixture.AssertNoLayoutBits(root);
    }

    [Fact]
    public void FlexGrowWithinConstrainedMaxWidth()
    {
        // Yoga fixture: flex_grow_within_constrained_max_width
        Element root = new() { Name = "root" };
        // Phase 5: root position type absolute. It does not change the layout of a root.
        root.Style.Width = 200;
        root.Style.Height = 100;

        Element child0 = AddChild(root, "root_child0");
        child0.Style.FlexDirection = FlexDirection.Row;
        child0.Style.MaxWidth = 300;

        Element child0Child0 = AddChild(child0, "root_child0_child0");
        child0Child0.Style.FlexGrow = 1;
        child0Child0.Style.Height = 20;

        LayoutFixture.Calculate(root);

        LayoutFixture.AssertRect(root, 0, 0, 200, 100);
        LayoutFixture.AssertRect(child0, 0, 0, 200, 20);
        LayoutFixture.AssertRect(child0Child0, 0, 0, 200, 20);
        LayoutFixture.AssertNoLayoutBits(root);
    }

    [Fact]
    public void FlexRootIgnored()
    {
        // Yoga fixture: flex_root_ignored
        Element root = new() { Name = "root" };
        // Phase 5: root position type absolute. It does not change the layout of a root.
        root.Style.FlexGrow = 1;
        root.Style.Width = 100;
        root.Style.MinHeight = 100;
        root.Style.MaxHeight = 500;

        Element child0 = AddChild(root, "root_child0");
        child0.Style.FlexGrow = 1;
        child0.Style.FlexBasis = 200;

        Element child1 = AddChild(root, "root_child1");
        child1.Style.Height = 100;

        LayoutFixture.Calculate(root);

        LayoutFixture.AssertRect(root, 0, 0, 100, 300);
        LayoutFixture.AssertRect(child0, 0, 0, 100, 200);
        LayoutFixture.AssertRect(child1, 0, 200, 100, 100);
        LayoutFixture.AssertNoLayoutBits(root);
    }

    [Fact]
    public void FlexGrowRootMinimized()
    {
        // Yoga fixture: flex_grow_root_minimized
        Element root = new() { Name = "root" };
        // Phase 5: root position type absolute. It does not change the layout of a root.
        root.Style.Width = 100;
        root.Style.MinHeight = 100;
        root.Style.MaxHeight = 500;

        Element child0 = AddChild(root, "root_child0");
        child0.Style.FlexGrow = 1;
        child0.Style.MinHeight = 100;
        child0.Style.MaxHeight = 500;

        Element child0Child0 = AddChild(child0, "root_child0_child0");
        child0Child0.Style.FlexGrow = 1;
        child0Child0.Style.FlexBasis = 200;

        Element child0Child1 = AddChild(child0, "root_child0_child1");
        child0Child1.Style.Height = 100;

        LayoutFixture.Calculate(root);

        LayoutFixture.AssertRect(root, 0, 0, 100, 300);
        LayoutFixture.AssertRect(child0, 0, 0, 100, 300);
        LayoutFixture.AssertRect(child0Child0, 0, 0, 100, 200);
        LayoutFixture.AssertRect(child0Child1, 0, 200, 100, 100);
        LayoutFixture.AssertNoLayoutBits(root);
    }

    [Fact]
    public void FlexGrowHeightMaximized()
    {
        // Yoga fixture: flex_grow_height_maximized
        Element root = new() { Name = "root" };
        // Phase 5: root position type absolute. It does not change the layout of a root.
        root.Style.Width = 100;
        root.Style.Height = 500;

        Element child0 = AddChild(root, "root_child0");
        child0.Style.FlexGrow = 1;
        child0.Style.MinHeight = 100;
        child0.Style.MaxHeight = 500;

        Element child0Child0 = AddChild(child0, "root_child0_child0");
        child0Child0.Style.FlexGrow = 1;
        child0Child0.Style.FlexBasis = 200;

        Element child0Child1 = AddChild(child0, "root_child0_child1");
        child0Child1.Style.Height = 100;

        LayoutFixture.Calculate(root);

        LayoutFixture.AssertRect(root, 0, 0, 100, 500);
        LayoutFixture.AssertRect(child0, 0, 0, 100, 500);
        LayoutFixture.AssertRect(child0Child0, 0, 0, 100, 400);
        LayoutFixture.AssertRect(child0Child1, 0, 400, 100, 100);
        LayoutFixture.AssertNoLayoutBits(root);
    }

    [Fact]
    public void FlexGrowWithinConstrainedMinRow()
    {
        // Yoga fixture: flex_grow_within_constrained_min_row
        Element root = new() { Name = "root" };
        root.Style.FlexDirection = FlexDirection.Row;
        // Phase 5: root position type absolute. It does not change the layout of a root.
        root.Style.MinWidth = 100;
        root.Style.Height = 100;

        Element child0 = AddChild(root, "root_child0");
        child0.Style.FlexGrow = 1;

        Element child1 = AddChild(root, "root_child1");
        child1.Style.Width = 50;

        LayoutFixture.Calculate(root);

        LayoutFixture.AssertRect(root, 0, 0, 100, 100);
        LayoutFixture.AssertRect(child0, 0, 0, 50, 100);
        LayoutFixture.AssertRect(child1, 50, 0, 50, 100);
        LayoutFixture.AssertNoLayoutBits(root);
    }

    [Fact]
    public void FlexGrowWithinConstrainedMinColumn()
    {
        // Yoga fixture: flex_grow_within_constrained_min_column
        Element root = new() { Name = "root" };
        // Phase 5: root position type absolute. It does not change the layout of a root.
        root.Style.MinHeight = 100;

        Element child0 = AddChild(root, "root_child0");
        child0.Style.FlexGrow = 1;

        Element child1 = AddChild(root, "root_child1");
        child1.Style.Height = 50;

        LayoutFixture.Calculate(root);

        LayoutFixture.AssertRect(root, 0, 0, 0, 100);
        LayoutFixture.AssertRect(child0, 0, 0, 0, 50);
        LayoutFixture.AssertRect(child1, 0, 50, 0, 50);
        LayoutFixture.AssertNoLayoutBits(root);
    }

    [Fact]
    public void FlexGrowWithinConstrainedMaxRow()
    {
        // Yoga fixture: flex_grow_within_constrained_max_row
        Element root = new() { Name = "root" };
        // Phase 5: root position type absolute. It does not change the layout of a root.
        root.Style.Width = 200;

        Element child0 = AddChild(root, "root_child0");
        child0.Style.FlexDirection = FlexDirection.Row;
        child0.Style.MaxWidth = 100;
        child0.Style.Height = 100;

        Element child0Child0 = AddChild(child0, "root_child0_child0");
        child0Child0.Style.FlexShrink = 1;
        child0Child0.Style.FlexBasis = 100;

        Element child0Child1 = AddChild(child0, "root_child0_child1");
        child0Child1.Style.Width = 50;

        LayoutFixture.Calculate(root);

        LayoutFixture.AssertRect(root, 0, 0, 200, 100);
        LayoutFixture.AssertRect(child0, 0, 0, 100, 100);
        LayoutFixture.AssertRect(child0Child0, 0, 0, 50, 100);
        LayoutFixture.AssertRect(child0Child1, 50, 0, 50, 100);
        LayoutFixture.AssertNoLayoutBits(root);
    }

    [Fact]
    public void FlexGrowWithinConstrainedMaxColumn()
    {
        // Yoga fixture: flex_grow_within_constrained_max_column
        Element root = new() { Name = "root" };
        // Phase 5: root position type absolute. It does not change the layout of a root.
        root.Style.Width = 100;
        root.Style.MaxHeight = 100;

        Element child0 = AddChild(root, "root_child0");
        child0.Style.FlexShrink = 1;
        child0.Style.FlexBasis = 100;

        Element child1 = AddChild(root, "root_child1");
        child1.Style.Height = 50;

        LayoutFixture.Calculate(root);

        LayoutFixture.AssertRect(root, 0, 0, 100, 100);
        LayoutFixture.AssertRect(child0, 0, 0, 100, 50);
        LayoutFixture.AssertRect(child1, 0, 50, 100, 50);
        LayoutFixture.AssertNoLayoutBits(root);
    }

    [Fact]
    public void MinWidthOverridesWidth()
    {
        // Yoga fixture: min_width_overrides_width
        Element root = new() { Name = "root" };
        // Phase 5: root position type absolute. It does not change the layout of a root.
        root.Style.Width = 50;
        root.Style.MinWidth = 100;

        LayoutFixture.Calculate(root);

        LayoutFixture.AssertRect(root, 0, 0, 100, 0);
        LayoutFixture.AssertNoLayoutBits(root);
    }

    [Fact]
    public void MaxWidthOverridesWidth()
    {
        // Yoga fixture: max_width_overrides_width
        Element root = new() { Name = "root" };
        // Phase 5: root position type absolute. It does not change the layout of a root.
        root.Style.Width = 200;
        root.Style.MaxWidth = 100;

        LayoutFixture.Calculate(root);

        LayoutFixture.AssertRect(root, 0, 0, 100, 0);
        LayoutFixture.AssertNoLayoutBits(root);
    }

    [Fact]
    public void MinHeightOverridesHeight()
    {
        // Yoga fixture: min_height_overrides_height
        Element root = new() { Name = "root" };
        // Phase 5: root position type absolute. It does not change the layout of a root.
        root.Style.Height = 50;
        root.Style.MinHeight = 100;

        LayoutFixture.Calculate(root);

        LayoutFixture.AssertRect(root, 0, 0, 0, 100);
        LayoutFixture.AssertNoLayoutBits(root);
    }

    [Fact]
    public void MaxHeightOverridesHeight()
    {
        // Yoga fixture: max_height_overrides_height
        Element root = new() { Name = "root" };
        // Phase 5: root position type absolute. It does not change the layout of a root.
        root.Style.Height = 200;
        root.Style.MaxHeight = 100;

        LayoutFixture.Calculate(root);

        LayoutFixture.AssertRect(root, 0, 0, 0, 100);
        LayoutFixture.AssertNoLayoutBits(root);
    }

    [Fact]
    public void AlignItemsMinMax()
    {
        // Yoga fixture: align_items_min_max
        Element root = new() { Name = "root" };
        root.Style.AlignItems = Align.Center;
        // Phase 5: root position type absolute. It does not change the layout of a root.
        root.Style.MinWidth = 100;
        root.Style.MaxWidth = 200;
        root.Style.Height = 100;

        Element child0 = AddChild(root, "root_child0");
        child0.Style.Width = 60;
        child0.Style.Height = 60;

        LayoutFixture.Calculate(root);

        LayoutFixture.AssertRect(root, 0, 0, 100, 100);
        LayoutFixture.AssertRect(child0, 20, 0, 60, 60);
        LayoutFixture.AssertNoLayoutBits(root);
    }

    [Fact]
    public void FlexGrowInAtMostContainer()
    {
        // Yoga fixture: flex_grow_in_at_most_container
        Element root = new() { Name = "root" };
        root.Style.FlexDirection = FlexDirection.Row;
        root.Style.AlignItems = Align.FlexStart;
        // Phase 5: root position type absolute. It does not change the layout of a root.
        root.Style.Width = 100;
        root.Style.Height = 100;

        Element child0 = AddChild(root, "root_child0");
        child0.Style.FlexDirection = FlexDirection.Row;

        Element child0Child0 = AddChild(child0, "root_child0_child0");
        child0Child0.Style.FlexGrow = 1;
        child0Child0.Style.FlexBasis = 0;

        LayoutFixture.Calculate(root);

        LayoutFixture.AssertRect(root, 0, 0, 100, 100);
        LayoutFixture.AssertRect(child0, 0, 0, 0, 0);
        LayoutFixture.AssertRect(child0Child0, 0, 0, 0, 0);
        LayoutFixture.AssertNoLayoutBits(root);
    }

    [Fact]
    public void ChildMinMaxWidthFlexing()
    {
        // Yoga fixture: child_min_max_width_flexing
        Element root = new() { Name = "root" };
        root.Style.FlexDirection = FlexDirection.Row;
        root.Style.Position = PositionType.Absolute;
        root.Style.Width = 120;
        root.Style.Height = 50;

        Element child0 = AddChild(root, "root_child0");
        child0.Style.FlexGrow = 1;
        child0.Style.FlexBasis = 0;
        child0.Style.MinWidth = 60;

        Element child1 = AddChild(root, "root_child1");
        child1.Style.FlexGrow = 1;
        child1.Style.FlexBasis = Length.Percent(50);
        child1.Style.MaxWidth = 20;

        LayoutFixture.Calculate(root);

        LayoutFixture.AssertRect(root, 0, 0, 120, 50);
        LayoutFixture.AssertRect(child0, 0, 0, 100, 50);
        LayoutFixture.AssertRect(child1, 100, 0, 20, 50);
        LayoutFixture.AssertNoLayoutBits(root);
    }

    [Fact]
    public void MinMaxPercentNoWidthHeight()
    {
        // Yoga fixture: min_max_percent_no_width_height
        Element root = new() { Name = "root" };
        root.Style.AlignItems = Align.FlexStart;
        root.Style.Position = PositionType.Absolute;
        root.Style.Width = 100;
        root.Style.Height = 100;

        Element child0 = AddChild(root, "root_child0");
        child0.Style.MinWidth = Length.Percent(10);
        child0.Style.MaxWidth = Length.Percent(10);
        child0.Style.MinHeight = Length.Percent(10);
        child0.Style.MaxHeight = Length.Percent(10);

        LayoutFixture.Calculate(root);

        LayoutFixture.AssertRect(root, 0, 0, 100, 100);
        LayoutFixture.AssertRect(child0, 0, 0, 10, 10);
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
