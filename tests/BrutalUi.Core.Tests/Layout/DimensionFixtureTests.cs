// Ported from Yoga (https://github.com/facebook/yoga), v3.2.1, tests/generated/YGDimensionTest.cpp. Copyright (c) Meta Platforms, Inc. and affiliates.
// MIT License. See THIRD-PARTY-NOTICES.md at the repository root.

using Xunit;

namespace BrutalUi.Core.Tests;

/// <summary>
/// The Yoga fixtures of <c>YGDimensionTest</c>, the LTR half only.
/// </summary>
/// <remarks>
/// Each Yoga fixture root sets <c>position: absolute</c>. The ports of this file were written before the engine had a position type.
/// For a root, the position type does not change the layout, so the ports omit that line.
/// </remarks>
public sealed class DimensionFixtureTests
{
    [Fact]
    public void WrapChild()
    {
        // Yoga fixture: wrap_child
        Element root = new() { Name = "root" };
        // Phase 5: root position type absolute. It does not change the layout of a root.

        Element child0 = AddChild(root, "root_child0");
        child0.Inline.Width = 100;
        child0.Inline.Height = 100;

        LayoutFixture.Calculate(root);

        LayoutFixture.AssertRect(root, 0, 0, 100, 100);
        LayoutFixture.AssertRect(child0, 0, 0, 100, 100);
        LayoutFixture.AssertNoLayoutBits(root);
    }

    [Fact]
    public void WrapGrandchild()
    {
        // Yoga fixture: wrap_grandchild
        Element root = new() { Name = "root" };
        // Phase 5: root position type absolute. It does not change the layout of a root.

        Element child0 = AddChild(root, "root_child0");

        Element child0Child0 = AddChild(child0, "root_child0_child0");
        child0Child0.Inline.Width = 100;
        child0Child0.Inline.Height = 100;

        LayoutFixture.Calculate(root);

        LayoutFixture.AssertRect(root, 0, 0, 100, 100);
        LayoutFixture.AssertRect(child0, 0, 0, 100, 100);
        LayoutFixture.AssertRect(child0Child0, 0, 0, 100, 100);
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
