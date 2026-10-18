// Ported from Yoga (https://github.com/facebook/yoga), v3.2.1, tests/generated/YGSizeOverflowTest.cpp. Copyright (c) Meta Platforms, Inc. and affiliates.
// MIT License. See THIRD-PARTY-NOTICES.md at the repository root.

using Xunit;

namespace BrutalUi.Core.Tests;

/// <summary>
/// The Yoga fixtures of <c>YGSizeOverflowTest</c>, the LTR half only.
/// </summary>
/// <remarks>
/// <para>
/// Each Yoga fixture root sets <c>position: absolute</c>, and each port does the same.
/// The fixture ledger in <c>context.md</c> lists the file.
/// </para>
/// <para>
/// No fixture of the file sets <c>overflow: scroll</c>. The only generated fixture with <c>overflow: scroll</c> is
/// <c>aspect_ratio_does_not_stretch_cross_axis_dim</c> of <c>YGAspectRatioTest</c>. It needs aspect ratio, and
/// Yoga disables it. The scroll tests live in <c>BoxModelTests</c>, because they follow the Yoga rule and are not
/// fixtures.
/// </para>
/// </remarks>
public sealed class SizeOverflowFixtureTests
{
    [Fact]
    public void NestedOverflowingChild()
    {
        // Yoga fixture: nested_overflowing_child
        Element root = new() { Name = "root" };
        root.Style.Position = PositionType.Absolute;
        root.Style.Width = 100;
        root.Style.Height = 100;

        Element child0 = AddChild(root, "root_child0");

        Element child0Child0 = AddChild(child0, "root_child0_child0");
        child0Child0.Style.Width = 200;
        child0Child0.Style.Height = 200;

        LayoutFixture.Calculate(root);

        LayoutFixture.AssertRect(root, 0, 0, 100, 100);
        LayoutFixture.AssertRect(child0, 0, 0, 100, 200);
        LayoutFixture.AssertRect(child0Child0, 0, 0, 200, 200);
        LayoutFixture.AssertNoLayoutBits(root);
    }

    [Fact]
    public void NestedOverflowingChildInConstraintParent()
    {
        // Yoga fixture: nested_overflowing_child_in_constraint_parent
        Element root = new() { Name = "root" };
        root.Style.Position = PositionType.Absolute;
        root.Style.Width = 100;
        root.Style.Height = 100;

        Element child0 = AddChild(root, "root_child0");
        child0.Style.Width = 100;
        child0.Style.Height = 100;

        Element child0Child0 = AddChild(child0, "root_child0_child0");
        child0Child0.Style.Width = 200;
        child0Child0.Style.Height = 200;

        LayoutFixture.Calculate(root);

        LayoutFixture.AssertRect(root, 0, 0, 100, 100);
        LayoutFixture.AssertRect(child0, 0, 0, 100, 100);
        LayoutFixture.AssertRect(child0Child0, 0, 0, 200, 200);
        LayoutFixture.AssertNoLayoutBits(root);
    }

    [Fact]
    public void ParentWrapChildSizeOverflowingParent()
    {
        // Yoga fixture: parent_wrap_child_size_overflowing_parent
        Element root = new() { Name = "root" };
        root.Style.Position = PositionType.Absolute;
        root.Style.Width = 100;
        root.Style.Height = 100;

        Element child0 = AddChild(root, "root_child0");
        child0.Style.Width = 100;

        Element child0Child0 = AddChild(child0, "root_child0_child0");
        child0Child0.Style.Width = 100;
        child0Child0.Style.Height = 200;

        LayoutFixture.Calculate(root);

        LayoutFixture.AssertRect(root, 0, 0, 100, 100);
        LayoutFixture.AssertRect(child0, 0, 0, 100, 200);
        LayoutFixture.AssertRect(child0Child0, 0, 0, 100, 200);
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
