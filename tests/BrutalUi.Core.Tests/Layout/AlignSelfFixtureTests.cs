// Ported from Yoga (https://github.com/facebook/yoga), v3.2.1, tests/generated/YGAlignSelfTest.cpp. Copyright (c) Meta Platforms, Inc. and affiliates.
// MIT License. See THIRD-PARTY-NOTICES.md at the repository root.

using Xunit;

namespace BrutalUi.Core.Tests;

/// <summary>
/// The Yoga fixtures of <c>YGAlignSelfTest</c>, the LTR half only.
/// </summary>
/// <remarks>
/// Each Yoga fixture root sets <c>position: absolute</c>. The ports of this file were written before the engine had a position type.
/// For a root, the position type does not change the layout, so the ports omit that line.
/// </remarks>
public sealed class AlignSelfFixtureTests
{
    [Fact]
    public void AlignSelfCenter()
    {
        // Yoga fixture: align_self_center
        Element root = new() { Name = "root" };
        // Phase 5: root position type absolute. It does not change the layout of a root.
        root.Style.Width = 100;
        root.Style.Height = 100;

        Element child0 = AddChild(root, "root_child0");
        child0.Style.AlignSelf = Align.Center;
        child0.Style.Width = 10;
        child0.Style.Height = 10;

        LayoutFixture.Calculate(root);

        LayoutFixture.AssertRect(root, 0, 0, 100, 100);
        LayoutFixture.AssertRect(child0, 45, 0, 10, 10);
        LayoutFixture.AssertNoLayoutBits(root);
    }

    [Fact]
    public void AlignSelfFlexEnd()
    {
        // Yoga fixture: align_self_flex_end
        Element root = new() { Name = "root" };
        // Phase 5: root position type absolute. It does not change the layout of a root.
        root.Style.Width = 100;
        root.Style.Height = 100;

        Element child0 = AddChild(root, "root_child0");
        child0.Style.AlignSelf = Align.FlexEnd;
        child0.Style.Width = 10;
        child0.Style.Height = 10;

        LayoutFixture.Calculate(root);

        LayoutFixture.AssertRect(root, 0, 0, 100, 100);
        LayoutFixture.AssertRect(child0, 90, 0, 10, 10);
        LayoutFixture.AssertNoLayoutBits(root);
    }

    [Fact]
    public void AlignSelfFlexStart()
    {
        // Yoga fixture: align_self_flex_start
        Element root = new() { Name = "root" };
        // Phase 5: root position type absolute. It does not change the layout of a root.
        root.Style.Width = 100;
        root.Style.Height = 100;

        Element child0 = AddChild(root, "root_child0");
        child0.Style.AlignSelf = Align.FlexStart;
        child0.Style.Width = 10;
        child0.Style.Height = 10;

        LayoutFixture.Calculate(root);

        LayoutFixture.AssertRect(root, 0, 0, 100, 100);
        LayoutFixture.AssertRect(child0, 0, 0, 10, 10);
        LayoutFixture.AssertNoLayoutBits(root);
    }

    [Fact]
    public void AlignSelfFlexEndOverrideFlexStart()
    {
        // Yoga fixture: align_self_flex_end_override_flex_start
        Element root = new() { Name = "root" };
        root.Style.AlignItems = Align.FlexStart;
        // Phase 5: root position type absolute. It does not change the layout of a root.
        root.Style.Width = 100;
        root.Style.Height = 100;

        Element child0 = AddChild(root, "root_child0");
        child0.Style.AlignSelf = Align.FlexEnd;
        child0.Style.Width = 10;
        child0.Style.Height = 10;

        LayoutFixture.Calculate(root);

        LayoutFixture.AssertRect(root, 0, 0, 100, 100);
        LayoutFixture.AssertRect(child0, 90, 0, 10, 10);
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
