// Ported from Yoga (https://github.com/facebook/yoga), v3.2.1, tests/generated/YGDisplayTest.cpp. Copyright (c) Meta Platforms, Inc. and affiliates.
// MIT License. See THIRD-PARTY-NOTICES.md at the repository root.

using Xunit;

namespace BrutalUi.Core.Tests;

/// <summary>
/// The Yoga fixtures of <c>YGDisplayTest</c>, the LTR half only.
/// </summary>
/// <remarks>
/// Each Yoga fixture root sets <c>position: absolute</c>, and each port does the same.
/// The ports skip the fixtures with <c>display: contents</c>.
/// The fixture ledger in <c>context.md</c> lists each skip.
/// </remarks>
public sealed class DisplayFixtureTests
{
    [Fact]
    public void DisplayNone()
    {
        // Yoga fixture: display_none
        Element root = new() { Name = "root" };
        root.Inline.FlexDirection = FlexDirection.Row;
        root.Inline.Position = PositionType.Absolute;
        root.Inline.Width = 100;
        root.Inline.Height = 100;

        Element child0 = AddChild(root, "root_child0");
        child0.Inline.FlexGrow = 1;

        Element child1 = AddChild(root, "root_child1");
        child1.Inline.FlexGrow = 1;
        child1.Inline.Display = Display.None;

        LayoutFixture.Calculate(root);

        LayoutFixture.AssertRect(root, 0, 0, 100, 100);
        LayoutFixture.AssertRect(child0, 0, 0, 100, 100);
        LayoutFixture.AssertRect(child1, 0, 0, 0, 0);
        LayoutFixture.AssertNoLayoutBits(root);
    }

    [Fact]
    public void DisplayNoneFixedSize()
    {
        // Yoga fixture: display_none_fixed_size
        Element root = new() { Name = "root" };
        root.Inline.FlexDirection = FlexDirection.Row;
        root.Inline.Position = PositionType.Absolute;
        root.Inline.Width = 100;
        root.Inline.Height = 100;

        Element child0 = AddChild(root, "root_child0");
        child0.Inline.FlexGrow = 1;

        Element child1 = AddChild(root, "root_child1");
        child1.Inline.Width = 20;
        child1.Inline.Height = 20;
        child1.Inline.Display = Display.None;

        LayoutFixture.Calculate(root);

        LayoutFixture.AssertRect(root, 0, 0, 100, 100);
        LayoutFixture.AssertRect(child0, 0, 0, 100, 100);
        LayoutFixture.AssertRect(child1, 0, 0, 0, 0);
        LayoutFixture.AssertNoLayoutBits(root);
    }

    [Fact]
    public void DisplayNoneWithMargin()
    {
        // Yoga fixture: display_none_with_margin
        Element root = new() { Name = "root" };
        root.Inline.FlexDirection = FlexDirection.Row;
        root.Inline.Position = PositionType.Absolute;
        root.Inline.Width = 100;
        root.Inline.Height = 100;

        Element child0 = AddChild(root, "root_child0");
        child0.Inline.Margin = LengthEdges.All(10);
        child0.Inline.Width = 20;
        child0.Inline.Height = 20;
        child0.Inline.Display = Display.None;

        Element child1 = AddChild(root, "root_child1");
        child1.Inline.FlexGrow = 1;

        LayoutFixture.Calculate(root);

        LayoutFixture.AssertRect(root, 0, 0, 100, 100);
        LayoutFixture.AssertRect(child0, 0, 0, 0, 0);
        LayoutFixture.AssertRect(child1, 0, 0, 100, 100);
        LayoutFixture.AssertNoLayoutBits(root);
    }

    [Fact]
    public void DisplayNoneWithPosition()
    {
        // Yoga fixture: display_none_with_position
        Element root = new() { Name = "root" };
        root.Inline.FlexDirection = FlexDirection.Row;
        root.Inline.Position = PositionType.Absolute;
        root.Inline.Width = 100;
        root.Inline.Height = 100;

        Element child0 = AddChild(root, "root_child0");
        child0.Inline.FlexGrow = 1;

        Element child1 = AddChild(root, "root_child1");
        child1.Inline.FlexGrow = 1;
        child1.Inline.Top = 10;
        child1.Inline.Display = Display.None;

        LayoutFixture.Calculate(root);

        LayoutFixture.AssertRect(root, 0, 0, 100, 100);
        LayoutFixture.AssertRect(child0, 0, 0, 100, 100);
        LayoutFixture.AssertRect(child1, 0, 0, 0, 0);
        LayoutFixture.AssertNoLayoutBits(root);
    }

    [Fact]
    public void DisplayNoneWithPositionAbsolute()
    {
        // Yoga fixture: display_none_with_position_absolute
        Element root = new() { Name = "root" };
        root.Inline.Position = PositionType.Absolute;
        root.Inline.Width = 100;
        root.Inline.Height = 100;

        Element child0 = AddChild(root, "root_child0");
        child0.Inline.Position = PositionType.Absolute;
        child0.Inline.Width = 100;
        child0.Inline.Height = 100;
        child0.Inline.Display = Display.None;

        LayoutFixture.Calculate(root);

        LayoutFixture.AssertRect(root, 0, 0, 100, 100);
        LayoutFixture.AssertRect(child0, 0, 0, 0, 0);
        LayoutFixture.AssertNoLayoutBits(root);
    }

    [Fact]
    public void DisplayNoneWithChild()
    {
        // Yoga fixture: display_none_with_child
        Element root = new() { Name = "root" };
        root.Inline.FlexDirection = FlexDirection.Row;
        root.Inline.Position = PositionType.Absolute;
        root.Inline.Width = 100;
        root.Inline.Height = 100;

        Element child0 = AddChild(root, "root_child0");
        child0.Inline.FlexGrow = 1;
        child0.Inline.FlexShrink = 1;
        child0.Inline.FlexBasis = Length.Percent(0);

        Element child1 = AddChild(root, "root_child1");
        child1.Inline.FlexGrow = 1;
        child1.Inline.FlexShrink = 1;
        child1.Inline.FlexBasis = Length.Percent(0);
        child1.Inline.Display = Display.None;

        Element child1Child0 = AddChild(child1, "root_child1_child0");
        child1Child0.Inline.FlexGrow = 1;
        child1Child0.Inline.FlexShrink = 1;
        child1Child0.Inline.FlexBasis = Length.Percent(0);
        child1Child0.Inline.Width = 20;

        Element child2 = AddChild(root, "root_child2");
        child2.Inline.FlexGrow = 1;
        child2.Inline.FlexShrink = 1;
        child2.Inline.FlexBasis = Length.Percent(0);

        LayoutFixture.Calculate(root);

        LayoutFixture.AssertRect(root, 0, 0, 100, 100);
        LayoutFixture.AssertRect(child0, 0, 0, 50, 100);
        LayoutFixture.AssertRect(child1, 0, 0, 0, 0);
        LayoutFixture.AssertRect(child1Child0, 0, 0, 0, 0);
        LayoutFixture.AssertRect(child2, 50, 0, 50, 100);
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
