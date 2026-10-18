// Ported from Yoga (https://github.com/facebook/yoga), v3.2.1, tests/generated/YGFlexDirectionTest.cpp. Copyright (c) Meta Platforms, Inc. and affiliates.
// MIT License. See THIRD-PARTY-NOTICES.md at the repository root.

using Xunit;

namespace BrutalUi.Core.Tests;

/// <summary>
/// The Yoga fixtures that set <c>left</c>, <c>top</c>, <c>right</c>, or <c>bottom</c> on a relative child, the LTR half only.
/// </summary>
/// <remarks>
/// <para>
/// The only source file is <c>tests/generated/YGFlexDirectionTest.cpp</c>. Each fixture of this class sets the offset
/// on a child of a <see cref="FlexDirection.Column"/> root, so the parent axis is not reverse.
/// </para>
/// <para>
/// The other Yoga files with a relative offset do not qualify: <c>YGPercentageTest</c> (a percent offset, phase 6),
/// <c>YGRoundingTest</c> (the pixel rounding), <c>YGAlignItemsTest</c> (baseline), <c>YGStaticPositionTest</c>
/// (<c>position: static</c>), and <c>YGDisplayTest</c> (a hidden child, in <see cref="DisplayFixtureTests"/>).
/// </para>
/// <para>Each Yoga fixture root sets <c>position: absolute</c>, and each port does the same.</para>
/// </remarks>
public sealed class RelativePositionFixtureTests
{
    [Fact]
    public void FlexDirectionRowReversePosLeft()
    {
        // Yoga fixture: flex_direction_row_reverse_pos_left
        Element root = new() { Name = "root" };
        root.Style.Position = PositionType.Absolute;
        root.Style.Width = 100;
        root.Style.Height = 100;

        Element child0 = AddChild(root, "root_child0");
        child0.Style.FlexDirection = FlexDirection.RowReverse;
        child0.Style.Left = 100;
        child0.Style.Width = 100;
        child0.Style.Height = 100;

        Element child0Child0 = AddChild(child0, "root_child0_child0");
        child0Child0.Style.Width = 10;

        Element child0Child1 = AddChild(child0, "root_child0_child1");
        child0Child1.Style.Width = 10;

        Element child0Child2 = AddChild(child0, "root_child0_child2");
        child0Child2.Style.Width = 10;

        LayoutFixture.Calculate(root);

        LayoutFixture.AssertRect(root, 0, 0, 100, 100);
        LayoutFixture.AssertRect(child0, 100, 0, 100, 100);
        LayoutFixture.AssertRect(child0Child0, 90, 0, 10, 100);
        LayoutFixture.AssertRect(child0Child1, 80, 0, 10, 100);
        LayoutFixture.AssertRect(child0Child2, 70, 0, 10, 100);
        LayoutFixture.AssertNoLayoutBits(root);
    }

    [Fact]
    public void FlexDirectionRowReversePosRight()
    {
        // Yoga fixture: flex_direction_row_reverse_pos_right
        Element root = new() { Name = "root" };
        root.Style.Position = PositionType.Absolute;
        root.Style.Width = 100;
        root.Style.Height = 100;

        Element child0 = AddChild(root, "root_child0");
        child0.Style.FlexDirection = FlexDirection.RowReverse;
        child0.Style.Right = 100;
        child0.Style.Width = 100;
        child0.Style.Height = 100;

        Element child0Child0 = AddChild(child0, "root_child0_child0");
        child0Child0.Style.Width = 10;

        Element child0Child1 = AddChild(child0, "root_child0_child1");
        child0Child1.Style.Width = 10;

        Element child0Child2 = AddChild(child0, "root_child0_child2");
        child0Child2.Style.Width = 10;

        LayoutFixture.Calculate(root);

        LayoutFixture.AssertRect(root, 0, 0, 100, 100);
        LayoutFixture.AssertRect(child0, -100, 0, 100, 100);
        LayoutFixture.AssertRect(child0Child0, 90, 0, 10, 100);
        LayoutFixture.AssertRect(child0Child1, 80, 0, 10, 100);
        LayoutFixture.AssertRect(child0Child2, 70, 0, 10, 100);
        LayoutFixture.AssertNoLayoutBits(root);
    }

    [Fact]
    public void FlexDirectionColumnReversePosTop()
    {
        // Yoga fixture: flex_direction_column_reverse_pos_top
        Element root = new() { Name = "root" };
        root.Style.Position = PositionType.Absolute;
        root.Style.Width = 100;
        root.Style.Height = 100;

        Element child0 = AddChild(root, "root_child0");
        child0.Style.FlexDirection = FlexDirection.ColumnReverse;
        child0.Style.Top = 100;
        child0.Style.Width = 100;
        child0.Style.Height = 100;

        Element child0Child0 = AddChild(child0, "root_child0_child0");
        child0Child0.Style.Width = 10;

        Element child0Child1 = AddChild(child0, "root_child0_child1");
        child0Child1.Style.Width = 10;

        Element child0Child2 = AddChild(child0, "root_child0_child2");
        child0Child2.Style.Width = 10;

        LayoutFixture.Calculate(root);

        LayoutFixture.AssertRect(root, 0, 0, 100, 100);
        LayoutFixture.AssertRect(child0, 0, 100, 100, 100);
        LayoutFixture.AssertRect(child0Child0, 0, 100, 10, 0);
        LayoutFixture.AssertRect(child0Child1, 0, 100, 10, 0);
        LayoutFixture.AssertRect(child0Child2, 0, 100, 10, 0);
        LayoutFixture.AssertNoLayoutBits(root);
    }

    [Fact]
    public void FlexDirectionColumnReversePosBottom()
    {
        // Yoga fixture: flex_direction_column_reverse_pos_bottom
        Element root = new() { Name = "root" };
        root.Style.Position = PositionType.Absolute;
        root.Style.Width = 100;
        root.Style.Height = 100;

        Element child0 = AddChild(root, "root_child0");
        child0.Style.FlexDirection = FlexDirection.ColumnReverse;
        child0.Style.Bottom = 100;
        child0.Style.Width = 100;
        child0.Style.Height = 100;

        Element child0Child0 = AddChild(child0, "root_child0_child0");
        child0Child0.Style.Width = 10;

        Element child0Child1 = AddChild(child0, "root_child0_child1");
        child0Child1.Style.Width = 10;

        Element child0Child2 = AddChild(child0, "root_child0_child2");
        child0Child2.Style.Width = 10;

        LayoutFixture.Calculate(root);

        LayoutFixture.AssertRect(root, 0, 0, 100, 100);
        LayoutFixture.AssertRect(child0, 0, -100, 100, 100);
        LayoutFixture.AssertRect(child0Child0, 0, 100, 10, 0);
        LayoutFixture.AssertRect(child0Child1, 0, 100, 10, 0);
        LayoutFixture.AssertRect(child0Child2, 0, 100, 10, 0);
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
