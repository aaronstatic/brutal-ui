// Ported from Yoga (https://github.com/facebook/yoga), v3.2.1, tests/generated/YGRoundingTest.cpp. Copyright (c) Meta Platforms, Inc. and affiliates.
// MIT License. See THIRD-PARTY-NOTICES.md at the repository root.

using Xunit;

namespace BrutalUi.Core.Tests;

/// <summary>
/// The Yoga fixtures of <c>YGRoundingTest</c> whose expected values need no pixel rounding, the LTR half only.
/// </summary>
/// <remarks>
/// <para>
/// Each Yoga fixture root sets <c>position: absolute</c>, and each port does the same.
/// The fixture ledger in <c>context.md</c> lists the file.
/// </para>
/// <para>
/// The engine does not round to the pixel grid (decision Q3). The file holds the one fixture of the 13 whose
/// unrounded layout equals the Yoga result. The other 12 stay skipped, because their expected values come from the
/// rounding.
/// </para>
/// </remarks>
public sealed class RoundingFixtureTests
{
    [Fact]
    public void RoundingFlexBasisFlexShrinkRow()
    {
        // Yoga fixture: rounding_flex_basis_flex_shrink_row
        Element root = new() { Name = "root" };
        root.Style.FlexDirection = FlexDirection.Row;
        root.Style.Position = PositionType.Absolute;
        root.Style.Width = 101;
        root.Style.Height = 100;

        Element child0 = AddChild(root, "root_child0");
        child0.Style.FlexShrink = 1;
        child0.Style.FlexBasis = 100;

        Element child1 = AddChild(root, "root_child1");
        child1.Style.FlexBasis = 25;

        Element child2 = AddChild(root, "root_child2");
        child2.Style.FlexBasis = 25;

        LayoutFixture.Calculate(root);

        LayoutFixture.AssertRect(root, 0, 0, 101, 100);
        LayoutFixture.AssertRect(child0, 0, 0, 51, 100);
        LayoutFixture.AssertRect(child1, 51, 0, 25, 100);
        LayoutFixture.AssertRect(child2, 76, 0, 25, 100);
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
