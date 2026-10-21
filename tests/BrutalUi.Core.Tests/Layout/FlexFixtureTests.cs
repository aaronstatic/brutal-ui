// Ported from Yoga (https://github.com/facebook/yoga), v3.2.1, tests/generated/YGFlexTest.cpp. Copyright (c) Meta Platforms, Inc. and affiliates.
// MIT License. See THIRD-PARTY-NOTICES.md at the repository root.

using Xunit;

namespace BrutalUi.Core.Tests;

/// <summary>
/// The Yoga fixtures of <c>YGFlexTest</c>, the LTR half only.
/// </summary>
/// <remarks>
/// Each Yoga fixture root sets <c>position: absolute</c>. The ports of this file were written before the engine had a position type.
/// For a root, the position type does not change the layout, so the ports omit that line.
/// </remarks>
public sealed class FlexFixtureTests
{
    [Fact]
    public void FlexBasisFlexGrowColumn()
    {
        // Yoga fixture: flex_basis_flex_grow_column
        Element root = new() { Name = "root" };
        // Phase 5: root position type absolute. It does not change the layout of a root.
        root.Inline.Width = 100;
        root.Inline.Height = 100;

        Element child0 = AddChild(root, "root_child0");
        child0.Inline.FlexGrow = 1;
        child0.Inline.FlexBasis = 50;

        Element child1 = AddChild(root, "root_child1");
        child1.Inline.FlexGrow = 1;

        LayoutFixture.Calculate(root);

        LayoutFixture.AssertRect(root, 0, 0, 100, 100);
        LayoutFixture.AssertRect(child0, 0, 0, 100, 75);
        LayoutFixture.AssertRect(child1, 0, 75, 100, 25);
        LayoutFixture.AssertNoLayoutBits(root);
    }

    [Fact]
    public void FlexShrinkFlexGrowRow()
    {
        // Yoga fixture: flex_shrink_flex_grow_row
        Element root = new() { Name = "root" };
        root.Inline.FlexDirection = FlexDirection.Row;
        // Phase 5: root position type absolute. It does not change the layout of a root.
        root.Inline.Width = 500;
        root.Inline.Height = 500;

        Element child0 = AddChild(root, "root_child0");
        child0.Inline.FlexShrink = 1;
        child0.Inline.Width = 500;
        child0.Inline.Height = 100;

        Element child1 = AddChild(root, "root_child1");
        child1.Inline.FlexShrink = 1;
        child1.Inline.Width = 500;
        child1.Inline.Height = 100;

        LayoutFixture.Calculate(root);

        LayoutFixture.AssertRect(root, 0, 0, 500, 500);
        LayoutFixture.AssertRect(child0, 0, 0, 250, 100);
        LayoutFixture.AssertRect(child1, 250, 0, 250, 100);
        LayoutFixture.AssertNoLayoutBits(root);
    }

    [Fact]
    public void FlexShrinkFlexGrowChildFlexShrinkOtherChild()
    {
        // Yoga fixture: flex_shrink_flex_grow_child_flex_shrink_other_child
        Element root = new() { Name = "root" };
        root.Inline.FlexDirection = FlexDirection.Row;
        // Phase 5: root position type absolute. It does not change the layout of a root.
        root.Inline.Width = 500;
        root.Inline.Height = 500;

        Element child0 = AddChild(root, "root_child0");
        child0.Inline.FlexShrink = 1;
        child0.Inline.Width = 500;
        child0.Inline.Height = 100;

        Element child1 = AddChild(root, "root_child1");
        child1.Inline.FlexGrow = 1;
        child1.Inline.FlexShrink = 1;
        child1.Inline.Width = 500;
        child1.Inline.Height = 100;

        LayoutFixture.Calculate(root);

        LayoutFixture.AssertRect(root, 0, 0, 500, 500);
        LayoutFixture.AssertRect(child0, 0, 0, 250, 100);
        LayoutFixture.AssertRect(child1, 250, 0, 250, 100);
        LayoutFixture.AssertNoLayoutBits(root);
    }

    [Fact]
    public void FlexBasisFlexGrowRow()
    {
        // Yoga fixture: flex_basis_flex_grow_row
        Element root = new() { Name = "root" };
        root.Inline.FlexDirection = FlexDirection.Row;
        // Phase 5: root position type absolute. It does not change the layout of a root.
        root.Inline.Width = 100;
        root.Inline.Height = 100;

        Element child0 = AddChild(root, "root_child0");
        child0.Inline.FlexGrow = 1;
        child0.Inline.FlexBasis = 50;

        Element child1 = AddChild(root, "root_child1");
        child1.Inline.FlexGrow = 1;

        LayoutFixture.Calculate(root);

        LayoutFixture.AssertRect(root, 0, 0, 100, 100);
        LayoutFixture.AssertRect(child0, 0, 0, 75, 100);
        LayoutFixture.AssertRect(child1, 75, 0, 25, 100);
        LayoutFixture.AssertNoLayoutBits(root);
    }

    [Fact]
    public void FlexBasisFlexShrinkColumn()
    {
        // Yoga fixture: flex_basis_flex_shrink_column
        Element root = new() { Name = "root" };
        // Phase 5: root position type absolute. It does not change the layout of a root.
        root.Inline.Width = 100;
        root.Inline.Height = 100;

        Element child0 = AddChild(root, "root_child0");
        child0.Inline.FlexShrink = 1;
        child0.Inline.FlexBasis = 100;

        Element child1 = AddChild(root, "root_child1");
        child1.Inline.FlexBasis = 50;

        LayoutFixture.Calculate(root);

        LayoutFixture.AssertRect(root, 0, 0, 100, 100);
        LayoutFixture.AssertRect(child0, 0, 0, 100, 50);
        LayoutFixture.AssertRect(child1, 0, 50, 100, 50);
        LayoutFixture.AssertNoLayoutBits(root);
    }

    [Fact]
    public void FlexBasisFlexShrinkRow()
    {
        // Yoga fixture: flex_basis_flex_shrink_row
        Element root = new() { Name = "root" };
        root.Inline.FlexDirection = FlexDirection.Row;
        // Phase 5: root position type absolute. It does not change the layout of a root.
        root.Inline.Width = 100;
        root.Inline.Height = 100;

        Element child0 = AddChild(root, "root_child0");
        child0.Inline.FlexShrink = 1;
        child0.Inline.FlexBasis = 100;

        Element child1 = AddChild(root, "root_child1");
        child1.Inline.FlexBasis = 50;

        LayoutFixture.Calculate(root);

        LayoutFixture.AssertRect(root, 0, 0, 100, 100);
        LayoutFixture.AssertRect(child0, 0, 0, 50, 100);
        LayoutFixture.AssertRect(child1, 50, 0, 50, 100);
        LayoutFixture.AssertNoLayoutBits(root);
    }

    [Fact]
    public void FlexShrinkToZero()
    {
        // Yoga fixture: flex_shrink_to_zero
        Element root = new() { Name = "root" };
        // Phase 5: root position type absolute. It does not change the layout of a root.
        root.Inline.Height = 75;

        Element child0 = AddChild(root, "root_child0");
        child0.Inline.Width = 50;
        child0.Inline.Height = 50;

        Element child1 = AddChild(root, "root_child1");
        child1.Inline.FlexShrink = 1;
        child1.Inline.Width = 50;
        child1.Inline.Height = 50;

        Element child2 = AddChild(root, "root_child2");
        child2.Inline.Width = 50;
        child2.Inline.Height = 50;

        LayoutFixture.Calculate(root);

        LayoutFixture.AssertRect(root, 0, 0, 50, 75);
        LayoutFixture.AssertRect(child0, 0, 0, 50, 50);
        LayoutFixture.AssertRect(child1, 0, 50, 50, 0);
        LayoutFixture.AssertRect(child2, 0, 50, 50, 50);
        LayoutFixture.AssertNoLayoutBits(root);
    }

    [Fact]
    public void FlexBasisOverridesMainSize()
    {
        // Yoga fixture: flex_basis_overrides_main_size
        Element root = new() { Name = "root" };
        // Phase 5: root position type absolute. It does not change the layout of a root.
        root.Inline.Width = 100;
        root.Inline.Height = 100;

        Element child0 = AddChild(root, "root_child0");
        child0.Inline.FlexGrow = 1;
        child0.Inline.FlexBasis = 50;
        child0.Inline.Height = 20;

        Element child1 = AddChild(root, "root_child1");
        child1.Inline.FlexGrow = 1;
        child1.Inline.Height = 10;

        Element child2 = AddChild(root, "root_child2");
        child2.Inline.FlexGrow = 1;
        child2.Inline.Height = 10;

        LayoutFixture.Calculate(root);

        LayoutFixture.AssertRect(root, 0, 0, 100, 100);
        LayoutFixture.AssertRect(child0, 0, 0, 100, 60);
        LayoutFixture.AssertRect(child1, 0, 60, 100, 20);
        LayoutFixture.AssertRect(child2, 0, 80, 100, 20);
        LayoutFixture.AssertNoLayoutBits(root);
    }

    [Fact]
    public void FlexGrowShrinkAtMost()
    {
        // Yoga fixture: flex_grow_shrink_at_most
        Element root = new() { Name = "root" };
        // Phase 5: root position type absolute. It does not change the layout of a root.
        root.Inline.Width = 100;
        root.Inline.Height = 100;

        Element child0 = AddChild(root, "root_child0");

        Element child0Child0 = AddChild(child0, "root_child0_child0");
        child0Child0.Inline.FlexGrow = 1;
        child0Child0.Inline.FlexShrink = 1;

        LayoutFixture.Calculate(root);

        LayoutFixture.AssertRect(root, 0, 0, 100, 100);
        LayoutFixture.AssertRect(child0, 0, 0, 100, 0);
        LayoutFixture.AssertRect(child0Child0, 0, 0, 100, 0);
        LayoutFixture.AssertNoLayoutBits(root);
    }

    [Fact]
    public void FlexGrowLessThanFactorOne()
    {
        // Yoga fixture: flex_grow_less_than_factor_one
        Element root = new() { Name = "root" };
        // Phase 5: root position type absolute. It does not change the layout of a root.
        root.Inline.Width = 200;
        root.Inline.Height = 500;

        Element child0 = AddChild(root, "root_child0");
        child0.Inline.FlexGrow = 0.2f;
        child0.Inline.FlexBasis = 40;

        Element child1 = AddChild(root, "root_child1");
        child1.Inline.FlexGrow = 0.2f;

        Element child2 = AddChild(root, "root_child2");
        child2.Inline.FlexGrow = 0.4f;

        LayoutFixture.Calculate(root);

        LayoutFixture.AssertRect(root, 0, 0, 200, 500);
        LayoutFixture.AssertRect(child0, 0, 0, 200, 132);
        LayoutFixture.AssertRect(child1, 0, 132, 200, 92);
        LayoutFixture.AssertRect(child2, 0, 224, 200, 184);
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
