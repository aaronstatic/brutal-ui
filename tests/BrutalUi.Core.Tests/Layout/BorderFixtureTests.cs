// Ported from Yoga (https://github.com/facebook/yoga), v3.2.1, tests/generated/YGBorderTest.cpp. Copyright (c) Meta Platforms, Inc. and affiliates.
// MIT License. See THIRD-PARTY-NOTICES.md at the repository root.

using Xunit;

namespace BrutalUi.Core.Tests;

/// <summary>
/// The Yoga fixtures of <c>YGBorderTest</c>, the LTR half only.
/// </summary>
/// <remarks>
/// Each Yoga fixture root sets <c>position: absolute</c>. The ports of this file were written before the engine had a position type.
/// For a root, the position type does not change the layout, so the ports omit that line.
/// </remarks>
public sealed class BorderFixtureTests
{
    [Fact]
    public void BorderNoSize()
    {
        // Yoga fixture: border_no_size
        Element root = new() { Name = "root" };
        // Phase 5: root position type absolute. It does not change the layout of a root.
        root.Inline.BorderWidth = LengthEdges.All(10);

        LayoutFixture.Calculate(root);

        LayoutFixture.AssertRect(root, 0, 0, 20, 20);
        LayoutFixture.AssertNoLayoutBits(root);
    }

    [Fact]
    public void BorderContainerMatchChild()
    {
        // Yoga fixture: border_container_match_child
        Element root = new() { Name = "root" };
        // Phase 5: root position type absolute. It does not change the layout of a root.
        root.Inline.BorderWidth = LengthEdges.All(10);

        Element child0 = AddChild(root, "root_child0");
        child0.Inline.Width = 10;
        child0.Inline.Height = 10;

        LayoutFixture.Calculate(root);

        LayoutFixture.AssertRect(root, 0, 0, 30, 30);
        LayoutFixture.AssertRect(child0, 10, 10, 10, 10);
        LayoutFixture.AssertNoLayoutBits(root);
    }

    [Fact]
    public void BorderFlexChild()
    {
        // Yoga fixture: border_flex_child
        Element root = new() { Name = "root" };
        // Phase 5: root position type absolute. It does not change the layout of a root.
        root.Inline.BorderWidth = LengthEdges.All(10);
        root.Inline.Width = 100;
        root.Inline.Height = 100;

        Element child0 = AddChild(root, "root_child0");
        child0.Inline.FlexGrow = 1;
        child0.Inline.Width = 10;

        LayoutFixture.Calculate(root);

        LayoutFixture.AssertRect(root, 0, 0, 100, 100);
        LayoutFixture.AssertRect(child0, 10, 10, 10, 80);
        LayoutFixture.AssertNoLayoutBits(root);
    }

    [Fact]
    public void BorderStretchChild()
    {
        // Yoga fixture: border_stretch_child
        Element root = new() { Name = "root" };
        // Phase 5: root position type absolute. It does not change the layout of a root.
        root.Inline.BorderWidth = LengthEdges.All(10);
        root.Inline.Width = 100;
        root.Inline.Height = 100;

        Element child0 = AddChild(root, "root_child0");
        child0.Inline.Height = 10;

        LayoutFixture.Calculate(root);

        LayoutFixture.AssertRect(root, 0, 0, 100, 100);
        LayoutFixture.AssertRect(child0, 10, 10, 80, 10);
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
