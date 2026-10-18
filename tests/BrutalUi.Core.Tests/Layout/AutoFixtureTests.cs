// Ported from Yoga (https://github.com/facebook/yoga), v3.2.1, tests/generated/YGAutoTest.cpp. Copyright (c) Meta Platforms, Inc. and affiliates.
// MIT License. See THIRD-PARTY-NOTICES.md at the repository root.

using Xunit;

namespace BrutalUi.Core.Tests;

/// <summary>
/// The Yoga fixtures of <c>YGAutoTest</c>, the LTR half only.
/// </summary>
/// <remarks>
/// Each Yoga fixture root sets <c>position: absolute</c>, and each port does the same.
/// The fixture ledger in <c>context.md</c> lists the file.
/// </remarks>
public sealed class AutoFixtureTests
{
    [Fact]
    public void AutoWidth()
    {
        // Yoga fixture: auto_width
        Element root = new() { Name = "root" };
        root.Style.FlexDirection = FlexDirection.Row;
        root.Style.Position = PositionType.Absolute;
        root.Style.Width = Length.Auto;
        root.Style.Height = 50;

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

        LayoutFixture.AssertRect(root, 0, 0, 150, 50);
        LayoutFixture.AssertRect(child0, 0, 0, 50, 50);
        LayoutFixture.AssertRect(child1, 50, 0, 50, 50);
        LayoutFixture.AssertRect(child2, 100, 0, 50, 50);
        LayoutFixture.AssertNoLayoutBits(root);
    }

    [Fact]
    public void AutoHeight()
    {
        // Yoga fixture: auto_height
        Element root = new() { Name = "root" };
        root.Style.Position = PositionType.Absolute;
        root.Style.Width = 50;
        root.Style.Height = Length.Auto;

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

        LayoutFixture.AssertRect(root, 0, 0, 50, 150);
        LayoutFixture.AssertRect(child0, 0, 0, 50, 50);
        LayoutFixture.AssertRect(child1, 0, 50, 50, 50);
        LayoutFixture.AssertRect(child2, 0, 100, 50, 50);
        LayoutFixture.AssertNoLayoutBits(root);
    }

    [Fact]
    public void AutoFlexBasis()
    {
        // Yoga fixture: auto_flex_basis
        Element root = new() { Name = "root" };
        root.Style.Position = PositionType.Absolute;
        root.Style.Width = 50;

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

        LayoutFixture.AssertRect(root, 0, 0, 50, 150);
        LayoutFixture.AssertRect(child0, 0, 0, 50, 50);
        LayoutFixture.AssertRect(child1, 0, 50, 50, 50);
        LayoutFixture.AssertRect(child2, 0, 100, 50, 50);
        LayoutFixture.AssertNoLayoutBits(root);
    }

    [Fact]
    public void AutoPosition()
    {
        // Yoga fixture: auto_position
        Element root = new() { Name = "root" };
        root.Style.Position = PositionType.Absolute;
        root.Style.Width = 50;
        root.Style.Height = 50;

        Element child0 = AddChild(root, "root_child0");
        child0.Style.Right = Length.Auto;
        child0.Style.Width = 25;
        child0.Style.Height = 25;

        LayoutFixture.Calculate(root);

        LayoutFixture.AssertRect(root, 0, 0, 50, 50);
        LayoutFixture.AssertRect(child0, 0, 0, 25, 25);
        LayoutFixture.AssertNoLayoutBits(root);
    }

    [Fact]
    public void AutoMargin()
    {
        // Yoga fixture: auto_margin
        Element root = new() { Name = "root" };
        root.Style.Position = PositionType.Absolute;
        root.Style.Width = 50;
        root.Style.Height = 50;

        Element child0 = AddChild(root, "root_child0");
        child0.Style.Margin.Left = Length.Auto;
        child0.Style.Width = 25;
        child0.Style.Height = 25;

        LayoutFixture.Calculate(root);

        LayoutFixture.AssertRect(root, 0, 0, 50, 50);
        LayoutFixture.AssertRect(child0, 25, 0, 25, 25);
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
