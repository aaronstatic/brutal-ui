// Ported from Yoga (https://github.com/facebook/yoga), v3.2.1, tests/generated/YGAlignItemsTest.cpp. Copyright (c) Meta Platforms, Inc. and affiliates.
// MIT License. See THIRD-PARTY-NOTICES.md at the repository root.

using Xunit;

namespace BrutalUi.Core.Tests;

/// <summary>
/// The Yoga fixtures of <c>YGAlignItemsTest</c>, the LTR half only.
/// </summary>
/// <remarks>
/// Each Yoga fixture root sets <c>position: absolute</c>. The ports of this file were written before the engine had a position type.
/// For a root, the position type does not change the layout, so the ports omit that line.
/// </remarks>
public sealed class AlignItemsFixtureTests
{
    [Fact]
    public void AlignItemsStretch()
    {
        // Yoga fixture: align_items_stretch
        Element root = new() { Name = "root" };
        // Phase 5: root position type absolute. It does not change the layout of a root.
        root.Inline.Width = 100;
        root.Inline.Height = 100;

        Element child0 = AddChild(root, "root_child0");
        child0.Inline.Height = 10;

        LayoutFixture.Calculate(root);

        LayoutFixture.AssertRect(root, 0, 0, 100, 100);
        LayoutFixture.AssertRect(child0, 0, 0, 100, 10);
        LayoutFixture.AssertNoLayoutBits(root);
    }

    [Fact]
    public void AlignItemsCenter()
    {
        // Yoga fixture: align_items_center
        Element root = new() { Name = "root" };
        root.Inline.AlignItems = Align.Center;
        // Phase 5: root position type absolute. It does not change the layout of a root.
        root.Inline.Width = 100;
        root.Inline.Height = 100;

        Element child0 = AddChild(root, "root_child0");
        child0.Inline.Width = 10;
        child0.Inline.Height = 10;

        LayoutFixture.Calculate(root);

        LayoutFixture.AssertRect(root, 0, 0, 100, 100);
        LayoutFixture.AssertRect(child0, 45, 0, 10, 10);
        LayoutFixture.AssertNoLayoutBits(root);
    }

    [Fact]
    public void AlignItemsFlexStart()
    {
        // Yoga fixture: align_items_flex_start
        Element root = new() { Name = "root" };
        root.Inline.AlignItems = Align.FlexStart;
        // Phase 5: root position type absolute. It does not change the layout of a root.
        root.Inline.Width = 100;
        root.Inline.Height = 100;

        Element child0 = AddChild(root, "root_child0");
        child0.Inline.Width = 10;
        child0.Inline.Height = 10;

        LayoutFixture.Calculate(root);

        LayoutFixture.AssertRect(root, 0, 0, 100, 100);
        LayoutFixture.AssertRect(child0, 0, 0, 10, 10);
        LayoutFixture.AssertNoLayoutBits(root);
    }

    [Fact]
    public void AlignItemsFlexEnd()
    {
        // Yoga fixture: align_items_flex_end
        Element root = new() { Name = "root" };
        root.Inline.AlignItems = Align.FlexEnd;
        // Phase 5: root position type absolute. It does not change the layout of a root.
        root.Inline.Width = 100;
        root.Inline.Height = 100;

        Element child0 = AddChild(root, "root_child0");
        child0.Inline.Width = 10;
        child0.Inline.Height = 10;

        LayoutFixture.Calculate(root);

        LayoutFixture.AssertRect(root, 0, 0, 100, 100);
        LayoutFixture.AssertRect(child0, 90, 0, 10, 10);
        LayoutFixture.AssertNoLayoutBits(root);
    }

    [Fact]
    public void AlignItemsCenterChildWithMarginBiggerThanParent()
    {
        // Yoga fixture: align_items_center_child_with_margin_bigger_than_parent
        Element root = new() { Name = "root" };
        root.Inline.JustifyContent = Justify.Center;
        root.Inline.AlignItems = Align.Center;
        // Phase 5: root position type absolute. It does not change the layout of a root.
        root.Inline.Width = 52;
        root.Inline.Height = 52;

        Element child0 = AddChild(root, "root_child0");
        child0.Inline.AlignItems = Align.Center;

        Element child0Child0 = AddChild(child0, "root_child0_child0");
        child0Child0.Inline.Margin = child0Child0.Inline.Margin with { Left = 10 };
        child0Child0.Inline.Margin = child0Child0.Inline.Margin with { Right = 10 };
        child0Child0.Inline.Width = 52;
        child0Child0.Inline.Height = 52;

        LayoutFixture.Calculate(root);

        LayoutFixture.AssertRect(root, 0, 0, 52, 52);
        LayoutFixture.AssertRect(child0, -10, 0, 72, 52);
        LayoutFixture.AssertRect(child0Child0, 10, 0, 52, 52);
        LayoutFixture.AssertNoLayoutBits(root);
    }

    [Fact]
    public void AlignItemsFlexEndChildWithMarginBiggerThanParent()
    {
        // Yoga fixture: align_items_flex_end_child_with_margin_bigger_than_parent
        Element root = new() { Name = "root" };
        root.Inline.JustifyContent = Justify.Center;
        root.Inline.AlignItems = Align.Center;
        // Phase 5: root position type absolute. It does not change the layout of a root.
        root.Inline.Width = 52;
        root.Inline.Height = 52;

        Element child0 = AddChild(root, "root_child0");
        child0.Inline.AlignItems = Align.FlexEnd;

        Element child0Child0 = AddChild(child0, "root_child0_child0");
        child0Child0.Inline.Margin = child0Child0.Inline.Margin with { Left = 10 };
        child0Child0.Inline.Margin = child0Child0.Inline.Margin with { Right = 10 };
        child0Child0.Inline.Width = 52;
        child0Child0.Inline.Height = 52;

        LayoutFixture.Calculate(root);

        LayoutFixture.AssertRect(root, 0, 0, 52, 52);
        LayoutFixture.AssertRect(child0, -10, 0, 72, 52);
        LayoutFixture.AssertRect(child0Child0, 10, 0, 52, 52);
        LayoutFixture.AssertNoLayoutBits(root);
    }

    [Fact]
    public void AlignItemsCenterChildWithoutMarginBiggerThanParent()
    {
        // Yoga fixture: align_items_center_child_without_margin_bigger_than_parent
        Element root = new() { Name = "root" };
        root.Inline.JustifyContent = Justify.Center;
        root.Inline.AlignItems = Align.Center;
        // Phase 5: root position type absolute. It does not change the layout of a root.
        root.Inline.Width = 52;
        root.Inline.Height = 52;

        Element child0 = AddChild(root, "root_child0");
        child0.Inline.AlignItems = Align.Center;

        Element child0Child0 = AddChild(child0, "root_child0_child0");
        child0Child0.Inline.Width = 72;
        child0Child0.Inline.Height = 72;

        LayoutFixture.Calculate(root);

        LayoutFixture.AssertRect(root, 0, 0, 52, 52);
        LayoutFixture.AssertRect(child0, -10, -10, 72, 72);
        LayoutFixture.AssertRect(child0Child0, 0, 0, 72, 72);
        LayoutFixture.AssertNoLayoutBits(root);
    }

    [Fact]
    public void AlignItemsFlexEndChildWithoutMarginBiggerThanParent()
    {
        // Yoga fixture: align_items_flex_end_child_without_margin_bigger_than_parent
        Element root = new() { Name = "root" };
        root.Inline.JustifyContent = Justify.Center;
        root.Inline.AlignItems = Align.Center;
        // Phase 5: root position type absolute. It does not change the layout of a root.
        root.Inline.Width = 52;
        root.Inline.Height = 52;

        Element child0 = AddChild(root, "root_child0");
        child0.Inline.AlignItems = Align.FlexEnd;

        Element child0Child0 = AddChild(child0, "root_child0_child0");
        child0Child0.Inline.Width = 72;
        child0Child0.Inline.Height = 72;

        LayoutFixture.Calculate(root);

        LayoutFixture.AssertRect(root, 0, 0, 52, 52);
        LayoutFixture.AssertRect(child0, -10, -10, 72, 72);
        LayoutFixture.AssertRect(child0Child0, 0, 0, 72, 72);
        LayoutFixture.AssertNoLayoutBits(root);
    }

    [Fact]
    public void AlignCenterShouldSizeBasedOnContent()
    {
        // Yoga fixture: align_center_should_size_based_on_content
        Element root = new() { Name = "root" };
        root.Inline.AlignItems = Align.Center;
        // Phase 5: root position type absolute. It does not change the layout of a root.
        root.Inline.Margin = root.Inline.Margin with { Top = 20 };
        root.Inline.Width = 100;
        root.Inline.Height = 100;

        Element child0 = AddChild(root, "root_child0");
        child0.Inline.JustifyContent = Justify.Center;
        child0.Inline.FlexShrink = 1;

        Element child0Child0 = AddChild(child0, "root_child0_child0");
        child0Child0.Inline.FlexGrow = 1;
        child0Child0.Inline.FlexShrink = 1;

        Element child0Child0Child0 = AddChild(child0Child0, "root_child0_child0_child0");
        child0Child0Child0.Inline.Width = 20;
        child0Child0Child0.Inline.Height = 20;

        LayoutFixture.Calculate(root);

        LayoutFixture.AssertRect(root, 0, 20, 100, 100);
        LayoutFixture.AssertRect(child0, 40, 0, 20, 20);
        LayoutFixture.AssertRect(child0Child0, 0, 0, 20, 20);
        LayoutFixture.AssertRect(child0Child0Child0, 0, 0, 20, 20);
        LayoutFixture.AssertNoLayoutBits(root);
    }

    [Fact]
    public void AlignStretchShouldSizeBasedOnParent()
    {
        // Yoga fixture: align_stretch_should_size_based_on_parent
        Element root = new() { Name = "root" };
        // Phase 5: root position type absolute. It does not change the layout of a root.
        root.Inline.Margin = root.Inline.Margin with { Top = 20 };
        root.Inline.Width = 100;
        root.Inline.Height = 100;

        Element child0 = AddChild(root, "root_child0");
        child0.Inline.JustifyContent = Justify.Center;
        child0.Inline.FlexShrink = 1;

        Element child0Child0 = AddChild(child0, "root_child0_child0");
        child0Child0.Inline.FlexGrow = 1;
        child0Child0.Inline.FlexShrink = 1;

        Element child0Child0Child0 = AddChild(child0Child0, "root_child0_child0_child0");
        child0Child0Child0.Inline.Width = 20;
        child0Child0Child0.Inline.Height = 20;

        LayoutFixture.Calculate(root);

        LayoutFixture.AssertRect(root, 0, 20, 100, 100);
        LayoutFixture.AssertRect(child0, 0, 0, 100, 20);
        LayoutFixture.AssertRect(child0Child0, 0, 0, 100, 20);
        LayoutFixture.AssertRect(child0Child0Child0, 0, 0, 20, 20);
        LayoutFixture.AssertNoLayoutBits(root);
    }

    [Fact]
    public void AlignFlexStartWithShrinkingChildren()
    {
        // Yoga fixture: align_flex_start_with_shrinking_children
        Element root = new() { Name = "root" };
        // Phase 5: root position type absolute. It does not change the layout of a root.
        root.Inline.Width = 500;
        root.Inline.Height = 500;

        Element child0 = AddChild(root, "root_child0");
        child0.Inline.AlignItems = Align.FlexStart;

        Element child0Child0 = AddChild(child0, "root_child0_child0");
        child0Child0.Inline.FlexGrow = 1;
        child0Child0.Inline.FlexShrink = 1;

        Element child0Child0Child0 = AddChild(child0Child0, "root_child0_child0_child0");
        child0Child0Child0.Inline.FlexGrow = 1;
        child0Child0Child0.Inline.FlexShrink = 1;

        LayoutFixture.Calculate(root);

        LayoutFixture.AssertRect(root, 0, 0, 500, 500);
        LayoutFixture.AssertRect(child0, 0, 0, 500, 0);
        LayoutFixture.AssertRect(child0Child0, 0, 0, 0, 0);
        LayoutFixture.AssertRect(child0Child0Child0, 0, 0, 0, 0);
        LayoutFixture.AssertNoLayoutBits(root);
    }

    [Fact]
    public void AlignFlexStartWithStretchingChildren()
    {
        // Yoga fixture: align_flex_start_with_stretching_children
        Element root = new() { Name = "root" };
        // Phase 5: root position type absolute. It does not change the layout of a root.
        root.Inline.Width = 500;
        root.Inline.Height = 500;

        Element child0 = AddChild(root, "root_child0");

        Element child0Child0 = AddChild(child0, "root_child0_child0");
        child0Child0.Inline.FlexGrow = 1;
        child0Child0.Inline.FlexShrink = 1;

        Element child0Child0Child0 = AddChild(child0Child0, "root_child0_child0_child0");
        child0Child0Child0.Inline.FlexGrow = 1;
        child0Child0Child0.Inline.FlexShrink = 1;

        LayoutFixture.Calculate(root);

        LayoutFixture.AssertRect(root, 0, 0, 500, 500);
        LayoutFixture.AssertRect(child0, 0, 0, 500, 0);
        LayoutFixture.AssertRect(child0Child0, 0, 0, 500, 0);
        LayoutFixture.AssertRect(child0Child0Child0, 0, 0, 500, 0);
        LayoutFixture.AssertNoLayoutBits(root);
    }

    [Fact]
    public void AlignFlexStartWithShrinkingChildrenWithStretch()
    {
        // Yoga fixture: align_flex_start_with_shrinking_children_with_stretch
        Element root = new() { Name = "root" };
        // Phase 5: root position type absolute. It does not change the layout of a root.
        root.Inline.Width = 500;
        root.Inline.Height = 500;

        Element child0 = AddChild(root, "root_child0");
        child0.Inline.AlignItems = Align.FlexStart;

        Element child0Child0 = AddChild(child0, "root_child0_child0");
        child0Child0.Inline.FlexGrow = 1;
        child0Child0.Inline.FlexShrink = 1;

        Element child0Child0Child0 = AddChild(child0Child0, "root_child0_child0_child0");
        child0Child0Child0.Inline.FlexGrow = 1;
        child0Child0Child0.Inline.FlexShrink = 1;

        LayoutFixture.Calculate(root);

        LayoutFixture.AssertRect(root, 0, 0, 500, 500);
        LayoutFixture.AssertRect(child0, 0, 0, 500, 0);
        LayoutFixture.AssertRect(child0Child0, 0, 0, 0, 0);
        LayoutFixture.AssertRect(child0Child0Child0, 0, 0, 0, 0);
        LayoutFixture.AssertNoLayoutBits(root);
    }

    [Fact]
    public void AlignFlexEndWithRowReverse()
    {
        // Yoga fixture: align_flex_end_with_row_reverse
        Element root = new() { Name = "root" };
        root.Inline.AlignItems = Align.FlexEnd;
        // Phase 5: root position type absolute. It does not change the layout of a root.
        root.Inline.FlexWrap = FlexWrap.Wrap;
        root.Inline.Width = 100;
        root.Inline.Height = 75;

        Element child0 = AddChild(root, "root_child0");
        child0.Inline.Margin = child0.Inline.Margin with { Left = 3 };
        child0.Inline.Margin = child0.Inline.Margin with { Right = 5 };
        child0.Inline.Width = 50;
        child0.Inline.Height = 50;

        Element child1 = AddChild(root, "root_child1");
        child1.Inline.Width = 50;
        child1.Inline.Height = 50;

        LayoutFixture.Calculate(root);

        LayoutFixture.AssertRect(root, 0, 0, 100, 75);
        LayoutFixture.AssertRect(child0, 3, 0, 50, 50);
        LayoutFixture.AssertRect(child1, 58, 0, 50, 50);
        LayoutFixture.AssertNoLayoutBits(root);
    }

    [Fact]
    public void AlignStretchWithRowReverse()
    {
        // Yoga fixture: align_stretch_with_row_reverse
        Element root = new() { Name = "root" };
        // Phase 5: root position type absolute. It does not change the layout of a root.
        root.Inline.FlexWrap = FlexWrap.Wrap;
        root.Inline.Width = 100;
        root.Inline.Height = 75;

        Element child0 = AddChild(root, "root_child0");
        child0.Inline.Margin = child0.Inline.Margin with { Left = 3 };
        child0.Inline.Margin = child0.Inline.Margin with { Right = 5 };
        child0.Inline.Width = 50;
        child0.Inline.Height = 50;

        Element child1 = AddChild(root, "root_child1");
        child1.Inline.Width = 50;
        child1.Inline.Height = 50;

        LayoutFixture.Calculate(root);

        LayoutFixture.AssertRect(root, 0, 0, 100, 75);
        LayoutFixture.AssertRect(child0, 3, 0, 50, 50);
        LayoutFixture.AssertRect(child1, 58, 0, 50, 50);
        LayoutFixture.AssertNoLayoutBits(root);
    }

    /// <summary>Creates a child with a name and adds it to the end of the child list of the parent.</summary>
    /// <param name="parent">The parent.</param>
    /// <param name="name">The Yoga node name.</param>
    /// <returns>The new child.</returns>
    private static Element AddChild(Element parent, string name)
    {
        Element child = new() { Name = name };
        parent.Children.Add(child);
        return child;
    }
}
