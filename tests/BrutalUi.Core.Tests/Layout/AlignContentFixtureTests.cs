// Ported from Yoga (https://github.com/facebook/yoga), v3.2.1, tests/generated/YGAlignContentTest.cpp. Copyright (c) Meta Platforms, Inc. and affiliates.
// MIT License. See THIRD-PARTY-NOTICES.md at the repository root.

using Xunit;

namespace BrutalUi.Core.Tests;

/// <summary>
/// The Yoga fixtures of <c>YGAlignContentTest</c>, the LTR half only.
/// </summary>
/// <remarks>
/// Each Yoga fixture root sets <c>position: absolute</c>, and each port does the same.
/// The <c>align_content_flex_start_*</c> fixtures set no <c>align-content</c>, because flex-start is the default.
/// The ports skip the fixtures with a <c>gap</c> and the fixtures that need the pixel rounding.
/// The fixture ledger in <c>context.md</c> lists each skip.
/// </remarks>
public sealed class AlignContentFixtureTests
{
    [Fact]
    public void AlignContentFlexStartNowrap()
    {
        // Yoga fixture: align_content_flex_start_nowrap
        Element root = new() { Name = "root" };
        root.Style.FlexDirection = FlexDirection.Row;
        root.Style.Position = PositionType.Absolute;
        root.Style.Width = 140;
        root.Style.Height = 120;

        Element child0 = AddChild(root, "root_child0");
        child0.Style.Width = 50;
        child0.Style.Height = 10;

        Element child1 = AddChild(root, "root_child1");
        child1.Style.Width = 50;
        child1.Style.Height = 10;

        LayoutFixture.Calculate(root);

        LayoutFixture.AssertRect(root, 0, 0, 140, 120);
        LayoutFixture.AssertRect(child0, 0, 0, 50, 10);
        LayoutFixture.AssertRect(child1, 50, 0, 50, 10);
        LayoutFixture.AssertNoLayoutBits(root);
    }

    [Fact]
    public void AlignContentFlexStartWrap()
    {
        // Yoga fixture: align_content_flex_start_wrap
        Element root = new() { Name = "root" };
        root.Style.FlexDirection = FlexDirection.Row;
        root.Style.Position = PositionType.Absolute;
        root.Style.FlexWrap = FlexWrap.Wrap;
        root.Style.Width = 140;
        root.Style.Height = 120;

        Element child0 = AddChild(root, "root_child0");
        child0.Style.Width = 50;
        child0.Style.Height = 10;

        Element child1 = AddChild(root, "root_child1");
        child1.Style.Width = 50;
        child1.Style.Height = 10;

        Element child2 = AddChild(root, "root_child2");
        child2.Style.Width = 50;
        child2.Style.Height = 10;

        Element child3 = AddChild(root, "root_child3");
        child3.Style.Width = 50;
        child3.Style.Height = 10;

        Element child4 = AddChild(root, "root_child4");
        child4.Style.Width = 50;
        child4.Style.Height = 10;

        LayoutFixture.Calculate(root);

        LayoutFixture.AssertRect(root, 0, 0, 140, 120);
        LayoutFixture.AssertRect(child0, 0, 0, 50, 10);
        LayoutFixture.AssertRect(child1, 50, 0, 50, 10);
        LayoutFixture.AssertRect(child2, 0, 10, 50, 10);
        LayoutFixture.AssertRect(child3, 50, 10, 50, 10);
        LayoutFixture.AssertRect(child4, 0, 20, 50, 10);
        LayoutFixture.AssertNoLayoutBits(root);
    }

    [Fact]
    public void AlignContentFlexStartWrapSingleline()
    {
        // Yoga fixture: align_content_flex_start_wrap_singleline
        Element root = new() { Name = "root" };
        root.Style.FlexDirection = FlexDirection.Row;
        root.Style.Position = PositionType.Absolute;
        root.Style.FlexWrap = FlexWrap.Wrap;
        root.Style.Width = 140;
        root.Style.Height = 120;

        Element child0 = AddChild(root, "root_child0");
        child0.Style.Width = 50;
        child0.Style.Height = 10;

        Element child1 = AddChild(root, "root_child1");
        child1.Style.Width = 50;
        child1.Style.Height = 10;

        LayoutFixture.Calculate(root);

        LayoutFixture.AssertRect(root, 0, 0, 140, 120);
        LayoutFixture.AssertRect(child0, 0, 0, 50, 10);
        LayoutFixture.AssertRect(child1, 50, 0, 50, 10);
        LayoutFixture.AssertNoLayoutBits(root);
    }

    [Fact]
    public void AlignContentFlexStartWrappedNegativeSpace()
    {
        // Yoga fixture: align_content_flex_start_wrapped_negative_space
        Element root = new() { Name = "root" };
        root.Style.Position = PositionType.Absolute;
        root.Style.BorderWidth.Left = 60;
        root.Style.BorderWidth.Top = 60;
        root.Style.BorderWidth.Right = 60;
        root.Style.BorderWidth.Bottom = 60;
        root.Style.Width = 320;
        root.Style.Height = 320;

        Element child0 = AddChild(root, "root_child0");
        child0.Style.FlexDirection = FlexDirection.Row;
        child0.Style.JustifyContent = Justify.Center;
        child0.Style.FlexWrap = FlexWrap.Wrap;
        child0.Style.Height = 10;

        Element child0Child0 = AddChild(child0, "root_child0_child0");
        child0Child0.Style.Width = Length.Percent(80);
        child0Child0.Style.Height = 20;

        Element child0Child1 = AddChild(child0, "root_child0_child1");
        child0Child1.Style.Width = Length.Percent(80);
        child0Child1.Style.Height = 20;

        Element child0Child2 = AddChild(child0, "root_child0_child2");
        child0Child2.Style.Width = Length.Percent(80);
        child0Child2.Style.Height = 20;

        LayoutFixture.Calculate(root);

        LayoutFixture.AssertRect(root, 0, 0, 320, 320);
        LayoutFixture.AssertRect(child0, 60, 60, 200, 10);
        LayoutFixture.AssertRect(child0Child0, 20, 0, 160, 20);
        LayoutFixture.AssertRect(child0Child1, 20, 20, 160, 20);
        LayoutFixture.AssertRect(child0Child2, 20, 40, 160, 20);
        LayoutFixture.AssertNoLayoutBits(root);
    }

    [Fact]
    public void AlignContentFlexStartWithoutHeightOnChildren()
    {
        // Yoga fixture: align_content_flex_start_without_height_on_children
        Element root = new() { Name = "root" };
        root.Style.Position = PositionType.Absolute;
        root.Style.FlexWrap = FlexWrap.Wrap;
        root.Style.Width = 100;
        root.Style.Height = 100;

        Element child0 = AddChild(root, "root_child0");
        child0.Style.Width = 50;

        Element child1 = AddChild(root, "root_child1");
        child1.Style.Width = 50;
        child1.Style.Height = 10;

        Element child2 = AddChild(root, "root_child2");
        child2.Style.Width = 50;

        Element child3 = AddChild(root, "root_child3");
        child3.Style.Width = 50;
        child3.Style.Height = 10;

        Element child4 = AddChild(root, "root_child4");
        child4.Style.Width = 50;

        LayoutFixture.Calculate(root);

        LayoutFixture.AssertRect(root, 0, 0, 100, 100);
        LayoutFixture.AssertRect(child0, 0, 0, 50, 0);
        LayoutFixture.AssertRect(child1, 0, 0, 50, 10);
        LayoutFixture.AssertRect(child2, 0, 10, 50, 0);
        LayoutFixture.AssertRect(child3, 0, 10, 50, 10);
        LayoutFixture.AssertRect(child4, 0, 20, 50, 0);
        LayoutFixture.AssertNoLayoutBits(root);
    }

    [Fact]
    public void AlignContentFlexStartWithFlex()
    {
        // Yoga fixture: align_content_flex_start_with_flex
        Element root = new() { Name = "root" };
        root.Style.Position = PositionType.Absolute;
        root.Style.FlexWrap = FlexWrap.Wrap;
        root.Style.Width = 100;
        root.Style.Height = 120;

        Element child0 = AddChild(root, "root_child0");
        child0.Style.FlexGrow = 1;
        child0.Style.FlexBasis = Length.Percent(0);
        child0.Style.Width = 50;

        Element child1 = AddChild(root, "root_child1");
        child1.Style.FlexGrow = 1;
        child1.Style.FlexBasis = Length.Percent(0);
        child1.Style.Width = 50;
        child1.Style.Height = 10;

        Element child2 = AddChild(root, "root_child2");
        child2.Style.Width = 50;

        Element child3 = AddChild(root, "root_child3");
        child3.Style.FlexGrow = 1;
        child3.Style.FlexShrink = 1;
        child3.Style.FlexBasis = Length.Percent(0);
        child3.Style.Width = 50;

        Element child4 = AddChild(root, "root_child4");
        child4.Style.Width = 50;

        LayoutFixture.Calculate(root);

        LayoutFixture.AssertRect(root, 0, 0, 100, 120);
        LayoutFixture.AssertRect(child0, 0, 0, 50, 40);
        LayoutFixture.AssertRect(child1, 0, 40, 50, 40);
        LayoutFixture.AssertRect(child2, 0, 80, 50, 0);
        LayoutFixture.AssertRect(child3, 0, 80, 50, 40);
        LayoutFixture.AssertRect(child4, 0, 120, 50, 0);
        LayoutFixture.AssertNoLayoutBits(root);
    }

    [Fact]
    public void AlignContentFlexEndNowrap()
    {
        // Yoga fixture: align_content_flex_end_nowrap
        Element root = new() { Name = "root" };
        root.Style.FlexDirection = FlexDirection.Row;
        root.Style.AlignContent = Align.FlexEnd;
        root.Style.Position = PositionType.Absolute;
        root.Style.Width = 140;
        root.Style.Height = 120;

        Element child0 = AddChild(root, "root_child0");
        child0.Style.Width = 50;
        child0.Style.Height = 10;

        Element child1 = AddChild(root, "root_child1");
        child1.Style.Width = 50;
        child1.Style.Height = 10;

        LayoutFixture.Calculate(root);

        LayoutFixture.AssertRect(root, 0, 0, 140, 120);
        LayoutFixture.AssertRect(child0, 0, 0, 50, 10);
        LayoutFixture.AssertRect(child1, 50, 0, 50, 10);
        LayoutFixture.AssertNoLayoutBits(root);
    }

    [Fact]
    public void AlignContentFlexEndWrap()
    {
        // Yoga fixture: align_content_flex_end_wrap
        Element root = new() { Name = "root" };
        root.Style.FlexDirection = FlexDirection.Row;
        root.Style.AlignContent = Align.FlexEnd;
        root.Style.Position = PositionType.Absolute;
        root.Style.FlexWrap = FlexWrap.Wrap;
        root.Style.Width = 140;
        root.Style.Height = 120;

        Element child0 = AddChild(root, "root_child0");
        child0.Style.Width = 50;
        child0.Style.Height = 10;

        Element child1 = AddChild(root, "root_child1");
        child1.Style.Width = 50;
        child1.Style.Height = 10;

        Element child2 = AddChild(root, "root_child2");
        child2.Style.Width = 50;
        child2.Style.Height = 10;

        Element child3 = AddChild(root, "root_child3");
        child3.Style.Width = 50;
        child3.Style.Height = 10;

        Element child4 = AddChild(root, "root_child4");
        child4.Style.Width = 50;
        child4.Style.Height = 10;

        LayoutFixture.Calculate(root);

        LayoutFixture.AssertRect(root, 0, 0, 140, 120);
        LayoutFixture.AssertRect(child0, 0, 90, 50, 10);
        LayoutFixture.AssertRect(child1, 50, 90, 50, 10);
        LayoutFixture.AssertRect(child2, 0, 100, 50, 10);
        LayoutFixture.AssertRect(child3, 50, 100, 50, 10);
        LayoutFixture.AssertRect(child4, 0, 110, 50, 10);
        LayoutFixture.AssertNoLayoutBits(root);
    }

    [Fact]
    public void AlignContentFlexEndWrapSingleline()
    {
        // Yoga fixture: align_content_flex_end_wrap_singleline
        Element root = new() { Name = "root" };
        root.Style.FlexDirection = FlexDirection.Row;
        root.Style.AlignContent = Align.FlexEnd;
        root.Style.Position = PositionType.Absolute;
        root.Style.FlexWrap = FlexWrap.Wrap;
        root.Style.Width = 140;
        root.Style.Height = 120;

        Element child0 = AddChild(root, "root_child0");
        child0.Style.Width = 50;
        child0.Style.Height = 10;

        Element child1 = AddChild(root, "root_child1");
        child1.Style.Width = 50;
        child1.Style.Height = 10;

        LayoutFixture.Calculate(root);

        LayoutFixture.AssertRect(root, 0, 0, 140, 120);
        LayoutFixture.AssertRect(child0, 0, 110, 50, 10);
        LayoutFixture.AssertRect(child1, 50, 110, 50, 10);
        LayoutFixture.AssertNoLayoutBits(root);
    }

    [Fact]
    public void AlignContentFlexEndWrappedNegativeSpace()
    {
        // Yoga fixture: align_content_flex_end_wrapped_negative_space
        Element root = new() { Name = "root" };
        root.Style.Position = PositionType.Absolute;
        root.Style.BorderWidth.Left = 60;
        root.Style.BorderWidth.Top = 60;
        root.Style.BorderWidth.Right = 60;
        root.Style.BorderWidth.Bottom = 60;
        root.Style.Width = 320;
        root.Style.Height = 320;

        Element child0 = AddChild(root, "root_child0");
        child0.Style.FlexDirection = FlexDirection.Row;
        child0.Style.JustifyContent = Justify.Center;
        child0.Style.AlignContent = Align.FlexEnd;
        child0.Style.FlexWrap = FlexWrap.Wrap;
        child0.Style.Height = 10;

        Element child0Child0 = AddChild(child0, "root_child0_child0");
        child0Child0.Style.Width = Length.Percent(80);
        child0Child0.Style.Height = 20;

        Element child0Child1 = AddChild(child0, "root_child0_child1");
        child0Child1.Style.Width = Length.Percent(80);
        child0Child1.Style.Height = 20;

        Element child0Child2 = AddChild(child0, "root_child0_child2");
        child0Child2.Style.Width = Length.Percent(80);
        child0Child2.Style.Height = 20;

        LayoutFixture.Calculate(root);

        LayoutFixture.AssertRect(root, 0, 0, 320, 320);
        LayoutFixture.AssertRect(child0, 60, 60, 200, 10);
        LayoutFixture.AssertRect(child0Child0, 20, -50, 160, 20);
        LayoutFixture.AssertRect(child0Child1, 20, -30, 160, 20);
        LayoutFixture.AssertRect(child0Child2, 20, -10, 160, 20);
        LayoutFixture.AssertNoLayoutBits(root);
    }

    [Fact]
    public void AlignContentCenterNowrap()
    {
        // Yoga fixture: align_content_center_nowrap
        Element root = new() { Name = "root" };
        root.Style.FlexDirection = FlexDirection.Row;
        root.Style.AlignContent = Align.Center;
        root.Style.Position = PositionType.Absolute;
        root.Style.Width = 140;
        root.Style.Height = 120;

        Element child0 = AddChild(root, "root_child0");
        child0.Style.Width = 50;
        child0.Style.Height = 10;

        Element child1 = AddChild(root, "root_child1");
        child1.Style.Width = 50;
        child1.Style.Height = 10;

        LayoutFixture.Calculate(root);

        LayoutFixture.AssertRect(root, 0, 0, 140, 120);
        LayoutFixture.AssertRect(child0, 0, 0, 50, 10);
        LayoutFixture.AssertRect(child1, 50, 0, 50, 10);
        LayoutFixture.AssertNoLayoutBits(root);
    }

    [Fact]
    public void AlignContentCenterWrap()
    {
        // Yoga fixture: align_content_center_wrap
        Element root = new() { Name = "root" };
        root.Style.FlexDirection = FlexDirection.Row;
        root.Style.AlignContent = Align.Center;
        root.Style.Position = PositionType.Absolute;
        root.Style.FlexWrap = FlexWrap.Wrap;
        root.Style.Width = 140;
        root.Style.Height = 120;

        Element child0 = AddChild(root, "root_child0");
        child0.Style.Width = 50;
        child0.Style.Height = 10;

        Element child1 = AddChild(root, "root_child1");
        child1.Style.Width = 50;
        child1.Style.Height = 10;

        Element child2 = AddChild(root, "root_child2");
        child2.Style.Width = 50;
        child2.Style.Height = 10;

        Element child3 = AddChild(root, "root_child3");
        child3.Style.Width = 50;
        child3.Style.Height = 10;

        Element child4 = AddChild(root, "root_child4");
        child4.Style.Width = 50;
        child4.Style.Height = 10;

        LayoutFixture.Calculate(root);

        LayoutFixture.AssertRect(root, 0, 0, 140, 120);
        LayoutFixture.AssertRect(child0, 0, 45, 50, 10);
        LayoutFixture.AssertRect(child1, 50, 45, 50, 10);
        LayoutFixture.AssertRect(child2, 0, 55, 50, 10);
        LayoutFixture.AssertRect(child3, 50, 55, 50, 10);
        LayoutFixture.AssertRect(child4, 0, 65, 50, 10);
        LayoutFixture.AssertNoLayoutBits(root);
    }

    [Fact]
    public void AlignContentCenterWrapSingleline()
    {
        // Yoga fixture: align_content_center_wrap_singleline
        Element root = new() { Name = "root" };
        root.Style.FlexDirection = FlexDirection.Row;
        root.Style.AlignContent = Align.Center;
        root.Style.Position = PositionType.Absolute;
        root.Style.FlexWrap = FlexWrap.Wrap;
        root.Style.Width = 140;
        root.Style.Height = 120;

        Element child0 = AddChild(root, "root_child0");
        child0.Style.Width = 50;
        child0.Style.Height = 10;

        Element child1 = AddChild(root, "root_child1");
        child1.Style.Width = 50;
        child1.Style.Height = 10;

        LayoutFixture.Calculate(root);

        LayoutFixture.AssertRect(root, 0, 0, 140, 120);
        LayoutFixture.AssertRect(child0, 0, 55, 50, 10);
        LayoutFixture.AssertRect(child1, 50, 55, 50, 10);
        LayoutFixture.AssertNoLayoutBits(root);
    }

    [Fact]
    public void AlignContentCenterWrappedNegativeSpace()
    {
        // Yoga fixture: align_content_center_wrapped_negative_space
        Element root = new() { Name = "root" };
        root.Style.Position = PositionType.Absolute;
        root.Style.BorderWidth.Left = 60;
        root.Style.BorderWidth.Top = 60;
        root.Style.BorderWidth.Right = 60;
        root.Style.BorderWidth.Bottom = 60;
        root.Style.Width = 320;
        root.Style.Height = 320;

        Element child0 = AddChild(root, "root_child0");
        child0.Style.FlexDirection = FlexDirection.Row;
        child0.Style.JustifyContent = Justify.Center;
        child0.Style.AlignContent = Align.Center;
        child0.Style.FlexWrap = FlexWrap.Wrap;
        child0.Style.Height = 10;

        Element child0Child0 = AddChild(child0, "root_child0_child0");
        child0Child0.Style.Width = Length.Percent(80);
        child0Child0.Style.Height = 20;

        Element child0Child1 = AddChild(child0, "root_child0_child1");
        child0Child1.Style.Width = Length.Percent(80);
        child0Child1.Style.Height = 20;

        Element child0Child2 = AddChild(child0, "root_child0_child2");
        child0Child2.Style.Width = Length.Percent(80);
        child0Child2.Style.Height = 20;

        LayoutFixture.Calculate(root);

        LayoutFixture.AssertRect(root, 0, 0, 320, 320);
        LayoutFixture.AssertRect(child0, 60, 60, 200, 10);
        LayoutFixture.AssertRect(child0Child0, 20, -25, 160, 20);
        LayoutFixture.AssertRect(child0Child1, 20, -5, 160, 20);
        LayoutFixture.AssertRect(child0Child2, 20, 15, 160, 20);
        LayoutFixture.AssertNoLayoutBits(root);
    }

    [Fact]
    public void AlignContentSpaceBetweenNowrap()
    {
        // Yoga fixture: align_content_space_between_nowrap
        Element root = new() { Name = "root" };
        root.Style.FlexDirection = FlexDirection.Row;
        root.Style.AlignContent = Align.SpaceBetween;
        root.Style.Position = PositionType.Absolute;
        root.Style.Width = 140;
        root.Style.Height = 120;

        Element child0 = AddChild(root, "root_child0");
        child0.Style.Width = 50;
        child0.Style.Height = 10;

        Element child1 = AddChild(root, "root_child1");
        child1.Style.Width = 50;
        child1.Style.Height = 10;

        LayoutFixture.Calculate(root);

        LayoutFixture.AssertRect(root, 0, 0, 140, 120);
        LayoutFixture.AssertRect(child0, 0, 0, 50, 10);
        LayoutFixture.AssertRect(child1, 50, 0, 50, 10);
        LayoutFixture.AssertNoLayoutBits(root);
    }

    [Fact]
    public void AlignContentSpaceBetweenWrap()
    {
        // Yoga fixture: align_content_space_between_wrap
        Element root = new() { Name = "root" };
        root.Style.FlexDirection = FlexDirection.Row;
        root.Style.AlignContent = Align.SpaceBetween;
        root.Style.Position = PositionType.Absolute;
        root.Style.FlexWrap = FlexWrap.Wrap;
        root.Style.Width = 140;
        root.Style.Height = 120;

        Element child0 = AddChild(root, "root_child0");
        child0.Style.Width = 50;
        child0.Style.Height = 10;

        Element child1 = AddChild(root, "root_child1");
        child1.Style.Width = 50;
        child1.Style.Height = 10;

        Element child2 = AddChild(root, "root_child2");
        child2.Style.Width = 50;
        child2.Style.Height = 10;

        Element child3 = AddChild(root, "root_child3");
        child3.Style.Width = 50;
        child3.Style.Height = 10;

        Element child4 = AddChild(root, "root_child4");
        child4.Style.Width = 50;
        child4.Style.Height = 10;

        LayoutFixture.Calculate(root);

        LayoutFixture.AssertRect(root, 0, 0, 140, 120);
        LayoutFixture.AssertRect(child0, 0, 0, 50, 10);
        LayoutFixture.AssertRect(child1, 50, 0, 50, 10);
        LayoutFixture.AssertRect(child2, 0, 55, 50, 10);
        LayoutFixture.AssertRect(child3, 50, 55, 50, 10);
        LayoutFixture.AssertRect(child4, 0, 110, 50, 10);
        LayoutFixture.AssertNoLayoutBits(root);
    }

    [Fact]
    public void AlignContentSpaceBetweenWrapSingleline()
    {
        // Yoga fixture: align_content_space_between_wrap_singleline
        Element root = new() { Name = "root" };
        root.Style.FlexDirection = FlexDirection.Row;
        root.Style.AlignContent = Align.SpaceBetween;
        root.Style.Position = PositionType.Absolute;
        root.Style.FlexWrap = FlexWrap.Wrap;
        root.Style.Width = 140;
        root.Style.Height = 120;

        Element child0 = AddChild(root, "root_child0");
        child0.Style.Width = 50;
        child0.Style.Height = 10;

        Element child1 = AddChild(root, "root_child1");
        child1.Style.Width = 50;
        child1.Style.Height = 10;

        LayoutFixture.Calculate(root);

        LayoutFixture.AssertRect(root, 0, 0, 140, 120);
        LayoutFixture.AssertRect(child0, 0, 0, 50, 10);
        LayoutFixture.AssertRect(child1, 50, 0, 50, 10);
        LayoutFixture.AssertNoLayoutBits(root);
    }

    [Fact]
    public void AlignContentSpaceBetweenWrappedNegativeSpace()
    {
        // Yoga fixture: align_content_space_between_wrapped_negative_space
        Element root = new() { Name = "root" };
        root.Style.Position = PositionType.Absolute;
        root.Style.BorderWidth.Left = 60;
        root.Style.BorderWidth.Top = 60;
        root.Style.BorderWidth.Right = 60;
        root.Style.BorderWidth.Bottom = 60;
        root.Style.Width = 320;
        root.Style.Height = 320;

        Element child0 = AddChild(root, "root_child0");
        child0.Style.FlexDirection = FlexDirection.Row;
        child0.Style.JustifyContent = Justify.Center;
        child0.Style.AlignContent = Align.SpaceBetween;
        child0.Style.FlexWrap = FlexWrap.Wrap;
        child0.Style.Height = 10;

        Element child0Child0 = AddChild(child0, "root_child0_child0");
        child0Child0.Style.Width = Length.Percent(80);
        child0Child0.Style.Height = 20;

        Element child0Child1 = AddChild(child0, "root_child0_child1");
        child0Child1.Style.Width = Length.Percent(80);
        child0Child1.Style.Height = 20;

        Element child0Child2 = AddChild(child0, "root_child0_child2");
        child0Child2.Style.Width = Length.Percent(80);
        child0Child2.Style.Height = 20;

        LayoutFixture.Calculate(root);

        LayoutFixture.AssertRect(root, 0, 0, 320, 320);
        LayoutFixture.AssertRect(child0, 60, 60, 200, 10);
        LayoutFixture.AssertRect(child0Child0, 20, 0, 160, 20);
        LayoutFixture.AssertRect(child0Child1, 20, 20, 160, 20);
        LayoutFixture.AssertRect(child0Child2, 20, 40, 160, 20);
        LayoutFixture.AssertNoLayoutBits(root);
    }

    [Fact]
    public void AlignContentSpaceBetweenWrappedNegativeSpaceRowReverse()
    {
        // Yoga fixture: align_content_space_between_wrapped_negative_space_row_reverse
        Element root = new() { Name = "root" };
        root.Style.Position = PositionType.Absolute;
        root.Style.BorderWidth.Left = 60;
        root.Style.BorderWidth.Top = 60;
        root.Style.BorderWidth.Right = 60;
        root.Style.BorderWidth.Bottom = 60;
        root.Style.Width = 320;
        root.Style.Height = 320;

        Element child0 = AddChild(root, "root_child0");
        child0.Style.FlexDirection = FlexDirection.RowReverse;
        child0.Style.JustifyContent = Justify.Center;
        child0.Style.AlignContent = Align.SpaceBetween;
        child0.Style.FlexWrap = FlexWrap.Wrap;
        child0.Style.Height = 10;

        Element child0Child0 = AddChild(child0, "root_child0_child0");
        child0Child0.Style.Width = Length.Percent(80);
        child0Child0.Style.Height = 20;

        Element child0Child1 = AddChild(child0, "root_child0_child1");
        child0Child1.Style.Width = Length.Percent(80);
        child0Child1.Style.Height = 20;

        Element child0Child2 = AddChild(child0, "root_child0_child2");
        child0Child2.Style.Width = Length.Percent(80);
        child0Child2.Style.Height = 20;

        LayoutFixture.Calculate(root);

        LayoutFixture.AssertRect(root, 0, 0, 320, 320);
        LayoutFixture.AssertRect(child0, 60, 60, 200, 10);
        LayoutFixture.AssertRect(child0Child0, 20, 0, 160, 20);
        LayoutFixture.AssertRect(child0Child1, 20, 20, 160, 20);
        LayoutFixture.AssertRect(child0Child2, 20, 40, 160, 20);
        LayoutFixture.AssertNoLayoutBits(root);
    }

    [Fact]
    public void AlignContentSpaceAroundNowrap()
    {
        // Yoga fixture: align_content_space_around_nowrap
        Element root = new() { Name = "root" };
        root.Style.FlexDirection = FlexDirection.Row;
        root.Style.AlignContent = Align.SpaceAround;
        root.Style.Position = PositionType.Absolute;
        root.Style.Width = 140;
        root.Style.Height = 120;

        Element child0 = AddChild(root, "root_child0");
        child0.Style.Width = 50;
        child0.Style.Height = 10;

        Element child1 = AddChild(root, "root_child1");
        child1.Style.Width = 50;
        child1.Style.Height = 10;

        LayoutFixture.Calculate(root);

        LayoutFixture.AssertRect(root, 0, 0, 140, 120);
        LayoutFixture.AssertRect(child0, 0, 0, 50, 10);
        LayoutFixture.AssertRect(child1, 50, 0, 50, 10);
        LayoutFixture.AssertNoLayoutBits(root);
    }

    [Fact]
    public void AlignContentSpaceAroundWrap()
    {
        // Yoga fixture: align_content_space_around_wrap
        Element root = new() { Name = "root" };
        root.Style.FlexDirection = FlexDirection.Row;
        root.Style.AlignContent = Align.SpaceAround;
        root.Style.Position = PositionType.Absolute;
        root.Style.FlexWrap = FlexWrap.Wrap;
        root.Style.Width = 140;
        root.Style.Height = 120;

        Element child0 = AddChild(root, "root_child0");
        child0.Style.Width = 50;
        child0.Style.Height = 10;

        Element child1 = AddChild(root, "root_child1");
        child1.Style.Width = 50;
        child1.Style.Height = 10;

        Element child2 = AddChild(root, "root_child2");
        child2.Style.Width = 50;
        child2.Style.Height = 10;

        Element child3 = AddChild(root, "root_child3");
        child3.Style.Width = 50;
        child3.Style.Height = 10;

        Element child4 = AddChild(root, "root_child4");
        child4.Style.Width = 50;
        child4.Style.Height = 10;

        LayoutFixture.Calculate(root);

        LayoutFixture.AssertRect(root, 0, 0, 140, 120);
        LayoutFixture.AssertRect(child0, 0, 15, 50, 10);
        LayoutFixture.AssertRect(child1, 50, 15, 50, 10);
        LayoutFixture.AssertRect(child2, 0, 55, 50, 10);
        LayoutFixture.AssertRect(child3, 50, 55, 50, 10);
        LayoutFixture.AssertRect(child4, 0, 95, 50, 10);
        LayoutFixture.AssertNoLayoutBits(root);
    }

    [Fact]
    public void AlignContentSpaceAroundWrapSingleline()
    {
        // Yoga fixture: align_content_space_around_wrap_singleline
        Element root = new() { Name = "root" };
        root.Style.FlexDirection = FlexDirection.Row;
        root.Style.AlignContent = Align.SpaceAround;
        root.Style.Position = PositionType.Absolute;
        root.Style.FlexWrap = FlexWrap.Wrap;
        root.Style.Width = 140;
        root.Style.Height = 120;

        Element child0 = AddChild(root, "root_child0");
        child0.Style.Width = 50;
        child0.Style.Height = 10;

        Element child1 = AddChild(root, "root_child1");
        child1.Style.Width = 50;
        child1.Style.Height = 10;

        LayoutFixture.Calculate(root);

        LayoutFixture.AssertRect(root, 0, 0, 140, 120);
        LayoutFixture.AssertRect(child0, 0, 55, 50, 10);
        LayoutFixture.AssertRect(child1, 50, 55, 50, 10);
        LayoutFixture.AssertNoLayoutBits(root);
    }

    [Fact]
    public void AlignContentSpaceAroundWrappedNegativeSpace()
    {
        // Yoga fixture: align_content_space_around_wrapped_negative_space
        Element root = new() { Name = "root" };
        root.Style.Position = PositionType.Absolute;
        root.Style.BorderWidth.Left = 60;
        root.Style.BorderWidth.Top = 60;
        root.Style.BorderWidth.Right = 60;
        root.Style.BorderWidth.Bottom = 60;
        root.Style.Width = 320;
        root.Style.Height = 320;

        Element child0 = AddChild(root, "root_child0");
        child0.Style.FlexDirection = FlexDirection.Row;
        child0.Style.JustifyContent = Justify.Center;
        child0.Style.AlignContent = Align.SpaceAround;
        child0.Style.FlexWrap = FlexWrap.Wrap;
        child0.Style.Height = 10;

        Element child0Child0 = AddChild(child0, "root_child0_child0");
        child0Child0.Style.Width = Length.Percent(80);
        child0Child0.Style.Height = 20;

        Element child0Child1 = AddChild(child0, "root_child0_child1");
        child0Child1.Style.Width = Length.Percent(80);
        child0Child1.Style.Height = 20;

        Element child0Child2 = AddChild(child0, "root_child0_child2");
        child0Child2.Style.Width = Length.Percent(80);
        child0Child2.Style.Height = 20;

        LayoutFixture.Calculate(root);

        LayoutFixture.AssertRect(root, 0, 0, 320, 320);
        LayoutFixture.AssertRect(child0, 60, 60, 200, 10);
        LayoutFixture.AssertRect(child0Child0, 20, 0, 160, 20);
        LayoutFixture.AssertRect(child0Child1, 20, 20, 160, 20);
        LayoutFixture.AssertRect(child0Child2, 20, 40, 160, 20);
        LayoutFixture.AssertNoLayoutBits(root);
    }

    [Fact]
    public void AlignContentSpaceAroundWrappedNegativeSpaceRowReverse()
    {
        // Yoga fixture: align_content_space_around_wrapped_negative_space_row_reverse
        Element root = new() { Name = "root" };
        root.Style.Position = PositionType.Absolute;
        root.Style.BorderWidth.Left = 60;
        root.Style.BorderWidth.Top = 60;
        root.Style.BorderWidth.Right = 60;
        root.Style.BorderWidth.Bottom = 60;
        root.Style.Width = 320;
        root.Style.Height = 320;

        Element child0 = AddChild(root, "root_child0");
        child0.Style.FlexDirection = FlexDirection.RowReverse;
        child0.Style.JustifyContent = Justify.Center;
        child0.Style.AlignContent = Align.SpaceAround;
        child0.Style.FlexWrap = FlexWrap.Wrap;
        child0.Style.Height = 10;

        Element child0Child0 = AddChild(child0, "root_child0_child0");
        child0Child0.Style.Width = Length.Percent(80);
        child0Child0.Style.Height = 20;

        Element child0Child1 = AddChild(child0, "root_child0_child1");
        child0Child1.Style.Width = Length.Percent(80);
        child0Child1.Style.Height = 20;

        Element child0Child2 = AddChild(child0, "root_child0_child2");
        child0Child2.Style.Width = Length.Percent(80);
        child0Child2.Style.Height = 20;

        LayoutFixture.Calculate(root);

        LayoutFixture.AssertRect(root, 0, 0, 320, 320);
        LayoutFixture.AssertRect(child0, 60, 60, 200, 10);
        LayoutFixture.AssertRect(child0Child0, 20, 0, 160, 20);
        LayoutFixture.AssertRect(child0Child1, 20, 20, 160, 20);
        LayoutFixture.AssertRect(child0Child2, 20, 40, 160, 20);
        LayoutFixture.AssertNoLayoutBits(root);
    }

    [Fact]
    public void AlignContentSpaceEvenlyNowrap()
    {
        // Yoga fixture: align_content_space_evenly_nowrap
        Element root = new() { Name = "root" };
        root.Style.FlexDirection = FlexDirection.Row;
        root.Style.AlignContent = Align.SpaceEvenly;
        root.Style.Position = PositionType.Absolute;
        root.Style.Width = 140;
        root.Style.Height = 120;

        Element child0 = AddChild(root, "root_child0");
        child0.Style.Width = 50;
        child0.Style.Height = 10;

        Element child1 = AddChild(root, "root_child1");
        child1.Style.Width = 50;
        child1.Style.Height = 10;

        LayoutFixture.Calculate(root);

        LayoutFixture.AssertRect(root, 0, 0, 140, 120);
        LayoutFixture.AssertRect(child0, 0, 0, 50, 10);
        LayoutFixture.AssertRect(child1, 50, 0, 50, 10);
        LayoutFixture.AssertNoLayoutBits(root);
    }

    [Fact]
    public void AlignContentSpaceEvenlyWrapSingleline()
    {
        // Yoga fixture: align_content_space_evenly_wrap_singleline
        Element root = new() { Name = "root" };
        root.Style.FlexDirection = FlexDirection.Row;
        root.Style.AlignContent = Align.SpaceEvenly;
        root.Style.Position = PositionType.Absolute;
        root.Style.FlexWrap = FlexWrap.Wrap;
        root.Style.Width = 140;
        root.Style.Height = 120;

        Element child0 = AddChild(root, "root_child0");
        child0.Style.Width = 50;
        child0.Style.Height = 10;

        Element child1 = AddChild(root, "root_child1");
        child1.Style.Width = 50;
        child1.Style.Height = 10;

        LayoutFixture.Calculate(root);

        LayoutFixture.AssertRect(root, 0, 0, 140, 120);
        LayoutFixture.AssertRect(child0, 0, 55, 50, 10);
        LayoutFixture.AssertRect(child1, 50, 55, 50, 10);
        LayoutFixture.AssertNoLayoutBits(root);
    }

    [Fact]
    public void AlignContentSpaceEvenlyWrappedNegativeSpace()
    {
        // Yoga fixture: align_content_space_evenly_wrapped_negative_space
        Element root = new() { Name = "root" };
        root.Style.Position = PositionType.Absolute;
        root.Style.BorderWidth.Left = 60;
        root.Style.BorderWidth.Top = 60;
        root.Style.BorderWidth.Right = 60;
        root.Style.BorderWidth.Bottom = 60;
        root.Style.Width = 320;
        root.Style.Height = 320;

        Element child0 = AddChild(root, "root_child0");
        child0.Style.FlexDirection = FlexDirection.Row;
        child0.Style.JustifyContent = Justify.Center;
        child0.Style.AlignContent = Align.SpaceEvenly;
        child0.Style.FlexWrap = FlexWrap.Wrap;
        child0.Style.Height = 10;

        Element child0Child0 = AddChild(child0, "root_child0_child0");
        child0Child0.Style.Width = Length.Percent(80);
        child0Child0.Style.Height = 20;

        Element child0Child1 = AddChild(child0, "root_child0_child1");
        child0Child1.Style.Width = Length.Percent(80);
        child0Child1.Style.Height = 20;

        Element child0Child2 = AddChild(child0, "root_child0_child2");
        child0Child2.Style.Width = Length.Percent(80);
        child0Child2.Style.Height = 20;

        LayoutFixture.Calculate(root);

        LayoutFixture.AssertRect(root, 0, 0, 320, 320);
        LayoutFixture.AssertRect(child0, 60, 60, 200, 10);
        LayoutFixture.AssertRect(child0Child0, 20, 0, 160, 20);
        LayoutFixture.AssertRect(child0Child1, 20, 20, 160, 20);
        LayoutFixture.AssertRect(child0Child2, 20, 40, 160, 20);
        LayoutFixture.AssertNoLayoutBits(root);
    }

    [Fact]
    public void AlignContentStretch()
    {
        // Yoga fixture: align_content_stretch
        Element root = new() { Name = "root" };
        root.Style.AlignContent = Align.Stretch;
        root.Style.Position = PositionType.Absolute;
        root.Style.FlexWrap = FlexWrap.Wrap;
        root.Style.Width = 150;
        root.Style.Height = 100;

        Element child0 = AddChild(root, "root_child0");
        child0.Style.Width = 50;

        Element child1 = AddChild(root, "root_child1");
        child1.Style.Width = 50;

        Element child2 = AddChild(root, "root_child2");
        child2.Style.Width = 50;

        Element child3 = AddChild(root, "root_child3");
        child3.Style.Width = 50;

        Element child4 = AddChild(root, "root_child4");
        child4.Style.Width = 50;

        LayoutFixture.Calculate(root);

        LayoutFixture.AssertRect(root, 0, 0, 150, 100);
        LayoutFixture.AssertRect(child0, 0, 0, 50, 0);
        LayoutFixture.AssertRect(child1, 0, 0, 50, 0);
        LayoutFixture.AssertRect(child2, 0, 0, 50, 0);
        LayoutFixture.AssertRect(child3, 0, 0, 50, 0);
        LayoutFixture.AssertRect(child4, 0, 0, 50, 0);
        LayoutFixture.AssertNoLayoutBits(root);
    }

    [Fact]
    public void AlignContentStretchRow()
    {
        // Yoga fixture: align_content_stretch_row
        Element root = new() { Name = "root" };
        root.Style.FlexDirection = FlexDirection.Row;
        root.Style.AlignContent = Align.Stretch;
        root.Style.Position = PositionType.Absolute;
        root.Style.FlexWrap = FlexWrap.Wrap;
        root.Style.Width = 150;
        root.Style.Height = 100;

        Element child0 = AddChild(root, "root_child0");
        child0.Style.Width = 50;

        Element child1 = AddChild(root, "root_child1");
        child1.Style.Width = 50;

        Element child2 = AddChild(root, "root_child2");
        child2.Style.Width = 50;

        Element child3 = AddChild(root, "root_child3");
        child3.Style.Width = 50;

        Element child4 = AddChild(root, "root_child4");
        child4.Style.Width = 50;

        LayoutFixture.Calculate(root);

        LayoutFixture.AssertRect(root, 0, 0, 150, 100);
        LayoutFixture.AssertRect(child0, 0, 0, 50, 50);
        LayoutFixture.AssertRect(child1, 50, 0, 50, 50);
        LayoutFixture.AssertRect(child2, 100, 0, 50, 50);
        LayoutFixture.AssertRect(child3, 0, 50, 50, 50);
        LayoutFixture.AssertRect(child4, 50, 50, 50, 50);
        LayoutFixture.AssertNoLayoutBits(root);
    }

    [Fact]
    public void AlignContentStretchRowWithChildren()
    {
        // Yoga fixture: align_content_stretch_row_with_children
        Element root = new() { Name = "root" };
        root.Style.FlexDirection = FlexDirection.Row;
        root.Style.AlignContent = Align.Stretch;
        root.Style.Position = PositionType.Absolute;
        root.Style.FlexWrap = FlexWrap.Wrap;
        root.Style.Width = 150;
        root.Style.Height = 100;

        Element child0 = AddChild(root, "root_child0");
        child0.Style.Width = 50;

        Element child0Child0 = AddChild(child0, "root_child0_child0");
        child0Child0.Style.FlexGrow = 1;
        child0Child0.Style.FlexShrink = 1;
        child0Child0.Style.FlexBasis = Length.Percent(0);

        Element child1 = AddChild(root, "root_child1");
        child1.Style.Width = 50;

        Element child2 = AddChild(root, "root_child2");
        child2.Style.Width = 50;

        Element child3 = AddChild(root, "root_child3");
        child3.Style.Width = 50;

        Element child4 = AddChild(root, "root_child4");
        child4.Style.Width = 50;

        LayoutFixture.Calculate(root);

        LayoutFixture.AssertRect(root, 0, 0, 150, 100);
        LayoutFixture.AssertRect(child0, 0, 0, 50, 50);
        LayoutFixture.AssertRect(child0Child0, 0, 0, 50, 50);
        LayoutFixture.AssertRect(child1, 50, 0, 50, 50);
        LayoutFixture.AssertRect(child2, 100, 0, 50, 50);
        LayoutFixture.AssertRect(child3, 0, 50, 50, 50);
        LayoutFixture.AssertRect(child4, 50, 50, 50, 50);
        LayoutFixture.AssertNoLayoutBits(root);
    }

    [Fact]
    public void AlignContentStretchRowWithFlex()
    {
        // Yoga fixture: align_content_stretch_row_with_flex
        Element root = new() { Name = "root" };
        root.Style.FlexDirection = FlexDirection.Row;
        root.Style.AlignContent = Align.Stretch;
        root.Style.Position = PositionType.Absolute;
        root.Style.FlexWrap = FlexWrap.Wrap;
        root.Style.Width = 150;
        root.Style.Height = 100;

        Element child0 = AddChild(root, "root_child0");
        child0.Style.Width = 50;

        Element child1 = AddChild(root, "root_child1");
        child1.Style.FlexGrow = 1;
        child1.Style.FlexShrink = 1;
        child1.Style.FlexBasis = Length.Percent(0);
        child1.Style.Width = 50;

        Element child2 = AddChild(root, "root_child2");
        child2.Style.Width = 50;

        Element child3 = AddChild(root, "root_child3");
        child3.Style.FlexGrow = 1;
        child3.Style.FlexShrink = 1;
        child3.Style.FlexBasis = Length.Percent(0);
        child3.Style.Width = 50;

        Element child4 = AddChild(root, "root_child4");
        child4.Style.Width = 50;

        LayoutFixture.Calculate(root);

        LayoutFixture.AssertRect(root, 0, 0, 150, 100);
        LayoutFixture.AssertRect(child0, 0, 0, 50, 100);
        LayoutFixture.AssertRect(child1, 50, 0, 0, 100);
        LayoutFixture.AssertRect(child2, 50, 0, 50, 100);
        LayoutFixture.AssertRect(child3, 100, 0, 0, 100);
        LayoutFixture.AssertRect(child4, 100, 0, 50, 100);
        LayoutFixture.AssertNoLayoutBits(root);
    }

    [Fact]
    public void AlignContentStretchRowWithFlexNoShrink()
    {
        // Yoga fixture: align_content_stretch_row_with_flex_no_shrink
        Element root = new() { Name = "root" };
        root.Style.FlexDirection = FlexDirection.Row;
        root.Style.AlignContent = Align.Stretch;
        root.Style.Position = PositionType.Absolute;
        root.Style.FlexWrap = FlexWrap.Wrap;
        root.Style.Width = 150;
        root.Style.Height = 100;

        Element child0 = AddChild(root, "root_child0");
        child0.Style.Width = 50;

        Element child1 = AddChild(root, "root_child1");
        child1.Style.FlexGrow = 1;
        child1.Style.FlexShrink = 1;
        child1.Style.FlexBasis = Length.Percent(0);
        child1.Style.Width = 50;

        Element child2 = AddChild(root, "root_child2");
        child2.Style.Width = 50;

        Element child3 = AddChild(root, "root_child3");
        child3.Style.FlexGrow = 1;
        child3.Style.FlexBasis = Length.Percent(0);
        child3.Style.Width = 50;

        Element child4 = AddChild(root, "root_child4");
        child4.Style.Width = 50;

        LayoutFixture.Calculate(root);

        LayoutFixture.AssertRect(root, 0, 0, 150, 100);
        LayoutFixture.AssertRect(child0, 0, 0, 50, 100);
        LayoutFixture.AssertRect(child1, 50, 0, 0, 100);
        LayoutFixture.AssertRect(child2, 50, 0, 50, 100);
        LayoutFixture.AssertRect(child3, 100, 0, 0, 100);
        LayoutFixture.AssertRect(child4, 100, 0, 50, 100);
        LayoutFixture.AssertNoLayoutBits(root);
    }

    [Fact]
    public void AlignContentStretchRowWithMargin()
    {
        // Yoga fixture: align_content_stretch_row_with_margin
        Element root = new() { Name = "root" };
        root.Style.FlexDirection = FlexDirection.Row;
        root.Style.AlignContent = Align.Stretch;
        root.Style.Position = PositionType.Absolute;
        root.Style.FlexWrap = FlexWrap.Wrap;
        root.Style.Width = 150;
        root.Style.Height = 100;

        Element child0 = AddChild(root, "root_child0");
        child0.Style.Width = 50;

        Element child1 = AddChild(root, "root_child1");
        child1.Style.Margin.Left = 10;
        child1.Style.Margin.Top = 10;
        child1.Style.Margin.Right = 10;
        child1.Style.Margin.Bottom = 10;
        child1.Style.Width = 50;

        Element child2 = AddChild(root, "root_child2");
        child2.Style.Width = 50;

        Element child3 = AddChild(root, "root_child3");
        child3.Style.Margin.Left = 10;
        child3.Style.Margin.Top = 10;
        child3.Style.Margin.Right = 10;
        child3.Style.Margin.Bottom = 10;
        child3.Style.Width = 50;

        Element child4 = AddChild(root, "root_child4");
        child4.Style.Width = 50;

        LayoutFixture.Calculate(root);

        LayoutFixture.AssertRect(root, 0, 0, 150, 100);
        LayoutFixture.AssertRect(child0, 0, 0, 50, 40);
        LayoutFixture.AssertRect(child1, 60, 10, 50, 20);
        LayoutFixture.AssertRect(child2, 0, 40, 50, 40);
        LayoutFixture.AssertRect(child3, 60, 50, 50, 20);
        LayoutFixture.AssertRect(child4, 0, 80, 50, 20);
        LayoutFixture.AssertNoLayoutBits(root);
    }

    [Fact]
    public void AlignContentStretchRowWithPadding()
    {
        // Yoga fixture: align_content_stretch_row_with_padding
        Element root = new() { Name = "root" };
        root.Style.FlexDirection = FlexDirection.Row;
        root.Style.AlignContent = Align.Stretch;
        root.Style.Position = PositionType.Absolute;
        root.Style.FlexWrap = FlexWrap.Wrap;
        root.Style.Width = 150;
        root.Style.Height = 100;

        Element child0 = AddChild(root, "root_child0");
        child0.Style.Width = 50;

        Element child1 = AddChild(root, "root_child1");
        child1.Style.Padding.Left = 10;
        child1.Style.Padding.Top = 10;
        child1.Style.Padding.Right = 10;
        child1.Style.Padding.Bottom = 10;
        child1.Style.Width = 50;

        Element child2 = AddChild(root, "root_child2");
        child2.Style.Width = 50;

        Element child3 = AddChild(root, "root_child3");
        child3.Style.Padding.Left = 10;
        child3.Style.Padding.Top = 10;
        child3.Style.Padding.Right = 10;
        child3.Style.Padding.Bottom = 10;
        child3.Style.Width = 50;

        Element child4 = AddChild(root, "root_child4");
        child4.Style.Width = 50;

        LayoutFixture.Calculate(root);

        LayoutFixture.AssertRect(root, 0, 0, 150, 100);
        LayoutFixture.AssertRect(child0, 0, 0, 50, 50);
        LayoutFixture.AssertRect(child1, 50, 0, 50, 50);
        LayoutFixture.AssertRect(child2, 100, 0, 50, 50);
        LayoutFixture.AssertRect(child3, 0, 50, 50, 50);
        LayoutFixture.AssertRect(child4, 50, 50, 50, 50);
        LayoutFixture.AssertNoLayoutBits(root);
    }

    [Fact]
    public void AlignContentStretchRowWithSingleRow()
    {
        // Yoga fixture: align_content_stretch_row_with_single_row
        Element root = new() { Name = "root" };
        root.Style.FlexDirection = FlexDirection.Row;
        root.Style.AlignContent = Align.Stretch;
        root.Style.Position = PositionType.Absolute;
        root.Style.FlexWrap = FlexWrap.Wrap;
        root.Style.Width = 150;
        root.Style.Height = 100;

        Element child0 = AddChild(root, "root_child0");
        child0.Style.Width = 50;

        Element child1 = AddChild(root, "root_child1");
        child1.Style.Width = 50;

        LayoutFixture.Calculate(root);

        LayoutFixture.AssertRect(root, 0, 0, 150, 100);
        LayoutFixture.AssertRect(child0, 0, 0, 50, 100);
        LayoutFixture.AssertRect(child1, 50, 0, 50, 100);
        LayoutFixture.AssertNoLayoutBits(root);
    }

    [Fact]
    public void AlignContentStretchRowWithFixedHeight()
    {
        // Yoga fixture: align_content_stretch_row_with_fixed_height
        Element root = new() { Name = "root" };
        root.Style.FlexDirection = FlexDirection.Row;
        root.Style.AlignContent = Align.Stretch;
        root.Style.Position = PositionType.Absolute;
        root.Style.FlexWrap = FlexWrap.Wrap;
        root.Style.Width = 150;
        root.Style.Height = 100;

        Element child0 = AddChild(root, "root_child0");
        child0.Style.Width = 50;

        Element child1 = AddChild(root, "root_child1");
        child1.Style.Width = 50;
        child1.Style.Height = 60;

        Element child2 = AddChild(root, "root_child2");
        child2.Style.Width = 50;

        Element child3 = AddChild(root, "root_child3");
        child3.Style.Width = 50;

        Element child4 = AddChild(root, "root_child4");
        child4.Style.Width = 50;

        LayoutFixture.Calculate(root);

        LayoutFixture.AssertRect(root, 0, 0, 150, 100);
        LayoutFixture.AssertRect(child0, 0, 0, 50, 80);
        LayoutFixture.AssertRect(child1, 50, 0, 50, 60);
        LayoutFixture.AssertRect(child2, 100, 0, 50, 80);
        LayoutFixture.AssertRect(child3, 0, 80, 50, 20);
        LayoutFixture.AssertRect(child4, 50, 80, 50, 20);
        LayoutFixture.AssertNoLayoutBits(root);
    }

    [Fact]
    public void AlignContentStretchRowWithMaxHeight()
    {
        // Yoga fixture: align_content_stretch_row_with_max_height
        Element root = new() { Name = "root" };
        root.Style.FlexDirection = FlexDirection.Row;
        root.Style.AlignContent = Align.Stretch;
        root.Style.Position = PositionType.Absolute;
        root.Style.FlexWrap = FlexWrap.Wrap;
        root.Style.Width = 150;
        root.Style.Height = 100;

        Element child0 = AddChild(root, "root_child0");
        child0.Style.Width = 50;

        Element child1 = AddChild(root, "root_child1");
        child1.Style.Width = 50;
        child1.Style.MaxHeight = 20;

        Element child2 = AddChild(root, "root_child2");
        child2.Style.Width = 50;

        Element child3 = AddChild(root, "root_child3");
        child3.Style.Width = 50;

        Element child4 = AddChild(root, "root_child4");
        child4.Style.Width = 50;

        LayoutFixture.Calculate(root);

        LayoutFixture.AssertRect(root, 0, 0, 150, 100);
        LayoutFixture.AssertRect(child0, 0, 0, 50, 50);
        LayoutFixture.AssertRect(child1, 50, 0, 50, 20);
        LayoutFixture.AssertRect(child2, 100, 0, 50, 50);
        LayoutFixture.AssertRect(child3, 0, 50, 50, 50);
        LayoutFixture.AssertRect(child4, 50, 50, 50, 50);
        LayoutFixture.AssertNoLayoutBits(root);
    }

    [Fact]
    public void AlignContentStretchRowWithMinHeight()
    {
        // Yoga fixture: align_content_stretch_row_with_min_height
        Element root = new() { Name = "root" };
        root.Style.FlexDirection = FlexDirection.Row;
        root.Style.AlignContent = Align.Stretch;
        root.Style.Position = PositionType.Absolute;
        root.Style.FlexWrap = FlexWrap.Wrap;
        root.Style.Width = 150;
        root.Style.Height = 100;

        Element child0 = AddChild(root, "root_child0");
        child0.Style.Width = 50;

        Element child1 = AddChild(root, "root_child1");
        child1.Style.Width = 50;
        child1.Style.MinHeight = 80;

        Element child2 = AddChild(root, "root_child2");
        child2.Style.Width = 50;

        Element child3 = AddChild(root, "root_child3");
        child3.Style.Width = 50;

        Element child4 = AddChild(root, "root_child4");
        child4.Style.Width = 50;

        LayoutFixture.Calculate(root);

        LayoutFixture.AssertRect(root, 0, 0, 150, 100);
        LayoutFixture.AssertRect(child0, 0, 0, 50, 90);
        LayoutFixture.AssertRect(child1, 50, 0, 50, 90);
        LayoutFixture.AssertRect(child2, 100, 0, 50, 90);
        LayoutFixture.AssertRect(child3, 0, 90, 50, 10);
        LayoutFixture.AssertRect(child4, 50, 90, 50, 10);
        LayoutFixture.AssertNoLayoutBits(root);
    }

    [Fact]
    public void AlignContentStretchColumn()
    {
        // Yoga fixture: align_content_stretch_column
        Element root = new() { Name = "root" };
        root.Style.AlignContent = Align.Stretch;
        root.Style.Position = PositionType.Absolute;
        root.Style.FlexWrap = FlexWrap.Wrap;
        root.Style.Width = 100;
        root.Style.Height = 150;

        Element child0 = AddChild(root, "root_child0");
        child0.Style.Height = 50;

        Element child0Child0 = AddChild(child0, "root_child0_child0");
        child0Child0.Style.FlexGrow = 1;
        child0Child0.Style.FlexShrink = 1;
        child0Child0.Style.FlexBasis = Length.Percent(0);

        Element child1 = AddChild(root, "root_child1");
        child1.Style.FlexGrow = 1;
        child1.Style.FlexShrink = 1;
        child1.Style.FlexBasis = Length.Percent(0);
        child1.Style.Height = 50;

        Element child2 = AddChild(root, "root_child2");
        child2.Style.Height = 50;

        Element child3 = AddChild(root, "root_child3");
        child3.Style.Height = 50;

        Element child4 = AddChild(root, "root_child4");
        child4.Style.Height = 50;

        LayoutFixture.Calculate(root);

        LayoutFixture.AssertRect(root, 0, 0, 100, 150);
        LayoutFixture.AssertRect(child0, 0, 0, 50, 50);
        LayoutFixture.AssertRect(child0Child0, 0, 0, 50, 50);
        LayoutFixture.AssertRect(child1, 0, 50, 50, 0);
        LayoutFixture.AssertRect(child2, 0, 50, 50, 50);
        LayoutFixture.AssertRect(child3, 0, 100, 50, 50);
        LayoutFixture.AssertRect(child4, 50, 0, 50, 50);
        LayoutFixture.AssertNoLayoutBits(root);
    }

    [Fact]
    public void AlignContentStretchIsNotOverridingAlignItems()
    {
        // Yoga fixture: align_content_stretch_is_not_overriding_align_items
        Element root = new() { Name = "root" };
        root.Style.AlignContent = Align.Stretch;
        root.Style.Position = PositionType.Absolute;

        Element child0 = AddChild(root, "root_child0");
        child0.Style.FlexDirection = FlexDirection.Row;
        child0.Style.AlignContent = Align.Stretch;
        child0.Style.AlignItems = Align.Center;
        child0.Style.Width = 100;
        child0.Style.Height = 100;

        Element child0Child0 = AddChild(child0, "root_child0_child0");
        child0Child0.Style.AlignContent = Align.Stretch;
        child0Child0.Style.Width = 10;
        child0Child0.Style.Height = 10;

        LayoutFixture.Calculate(root);

        LayoutFixture.AssertRect(root, 0, 0, 100, 100);
        LayoutFixture.AssertRect(child0, 0, 0, 100, 100);
        LayoutFixture.AssertRect(child0Child0, 0, 45, 10, 10);
        LayoutFixture.AssertNoLayoutBits(root);
    }

    [Fact]
    public void AlignContentStretchWithMinCrossAxis()
    {
        // Yoga fixture: align_content_stretch_with_min_cross_axis
        Element root = new() { Name = "root" };
        root.Style.FlexDirection = FlexDirection.Row;
        root.Style.AlignContent = Align.Stretch;
        root.Style.Position = PositionType.Absolute;
        root.Style.FlexWrap = FlexWrap.Wrap;
        root.Style.Width = 500;
        root.Style.MinHeight = 500;

        Element child0 = AddChild(root, "root_child0");
        child0.Style.Width = 400;
        child0.Style.Height = 200;

        Element child1 = AddChild(root, "root_child1");
        child1.Style.Width = 400;
        child1.Style.Height = 200;

        LayoutFixture.Calculate(root);

        LayoutFixture.AssertRect(root, 0, 0, 500, 500);
        LayoutFixture.AssertRect(child0, 0, 0, 400, 200);
        LayoutFixture.AssertRect(child1, 0, 250, 400, 200);
        LayoutFixture.AssertNoLayoutBits(root);
    }

    [Fact]
    public void AlignContentStretchWithMaxCrossAxis()
    {
        // Yoga fixture: align_content_stretch_with_max_cross_axis
        Element root = new() { Name = "root" };
        root.Style.FlexDirection = FlexDirection.Row;
        root.Style.AlignContent = Align.Stretch;
        root.Style.Position = PositionType.Absolute;
        root.Style.FlexWrap = FlexWrap.Wrap;
        root.Style.Width = 500;
        root.Style.MaxHeight = 500;

        Element child0 = AddChild(root, "root_child0");
        child0.Style.Width = 400;
        child0.Style.Height = 200;

        Element child1 = AddChild(root, "root_child1");
        child1.Style.Width = 400;
        child1.Style.Height = 200;

        LayoutFixture.Calculate(root);

        LayoutFixture.AssertRect(root, 0, 0, 500, 400);
        LayoutFixture.AssertRect(child0, 0, 0, 400, 200);
        LayoutFixture.AssertRect(child1, 0, 200, 400, 200);
        LayoutFixture.AssertNoLayoutBits(root);
    }

    [Fact]
    public void AlignContentStretchWithMaxCrossAxisAndBorderPadding()
    {
        // Yoga fixture: align_content_stretch_with_max_cross_axis_and_border_padding
        Element root = new() { Name = "root" };
        root.Style.FlexDirection = FlexDirection.Row;
        root.Style.AlignContent = Align.Stretch;
        root.Style.Position = PositionType.Absolute;
        root.Style.FlexWrap = FlexWrap.Wrap;
        root.Style.Padding.Left = 2;
        root.Style.Padding.Top = 2;
        root.Style.Padding.Right = 2;
        root.Style.Padding.Bottom = 2;
        root.Style.BorderWidth.Left = 5;
        root.Style.BorderWidth.Top = 5;
        root.Style.BorderWidth.Right = 5;
        root.Style.BorderWidth.Bottom = 5;
        root.Style.Width = 500;
        root.Style.MaxHeight = 500;

        Element child0 = AddChild(root, "root_child0");
        child0.Style.Width = 400;
        child0.Style.Height = 200;

        Element child1 = AddChild(root, "root_child1");
        child1.Style.Width = 400;
        child1.Style.Height = 200;

        LayoutFixture.Calculate(root);

        LayoutFixture.AssertRect(root, 0, 0, 500, 414);
        LayoutFixture.AssertRect(child0, 7, 7, 400, 200);
        LayoutFixture.AssertRect(child1, 7, 207, 400, 200);
        LayoutFixture.AssertNoLayoutBits(root);
    }

    [Fact]
    public void AlignContentSpaceEvenlyWithMaxCrossAxis()
    {
        // Yoga fixture: align_content_space_evenly_with_max_cross_axis
        Element root = new() { Name = "root" };
        root.Style.FlexDirection = FlexDirection.Row;
        root.Style.AlignContent = Align.SpaceEvenly;
        root.Style.Position = PositionType.Absolute;
        root.Style.FlexWrap = FlexWrap.Wrap;
        root.Style.Width = 500;
        root.Style.MaxHeight = 500;

        Element child0 = AddChild(root, "root_child0");
        child0.Style.Width = 400;
        child0.Style.Height = 200;

        Element child1 = AddChild(root, "root_child1");
        child1.Style.Width = 400;
        child1.Style.Height = 200;

        LayoutFixture.Calculate(root);

        LayoutFixture.AssertRect(root, 0, 0, 500, 400);
        LayoutFixture.AssertRect(child0, 0, 0, 400, 200);
        LayoutFixture.AssertRect(child1, 0, 200, 400, 200);
        LayoutFixture.AssertNoLayoutBits(root);
    }

    [Fact]
    public void AlignContentSpaceEvenlyWithMaxCrossAxisViolated()
    {
        // Yoga fixture: align_content_space_evenly_with_max_cross_axis_violated
        Element root = new() { Name = "root" };
        root.Style.FlexDirection = FlexDirection.Row;
        root.Style.AlignContent = Align.SpaceEvenly;
        root.Style.Position = PositionType.Absolute;
        root.Style.FlexWrap = FlexWrap.Wrap;
        root.Style.Width = 500;
        root.Style.MaxHeight = 300;

        Element child0 = AddChild(root, "root_child0");
        child0.Style.Width = 400;
        child0.Style.Height = 200;

        Element child1 = AddChild(root, "root_child1");
        child1.Style.Width = 400;
        child1.Style.Height = 200;

        LayoutFixture.Calculate(root);

        LayoutFixture.AssertRect(root, 0, 0, 500, 300);
        LayoutFixture.AssertRect(child0, 0, 0, 400, 200);
        LayoutFixture.AssertRect(child1, 0, 200, 400, 200);
        LayoutFixture.AssertNoLayoutBits(root);
    }

    [Fact]
    public void AlignContentSpaceEvenlyWithMaxCrossAxisViolatedPaddingAndBorder()
    {
        // Yoga fixture: align_content_space_evenly_with_max_cross_axis_violated_padding_and_border
        Element root = new() { Name = "root" };
        root.Style.FlexDirection = FlexDirection.Row;
        root.Style.AlignContent = Align.SpaceEvenly;
        root.Style.Position = PositionType.Absolute;
        root.Style.FlexWrap = FlexWrap.Wrap;
        root.Style.Padding.Left = 2;
        root.Style.Padding.Top = 2;
        root.Style.Padding.Right = 2;
        root.Style.Padding.Bottom = 2;
        root.Style.BorderWidth.Left = 5;
        root.Style.BorderWidth.Top = 5;
        root.Style.BorderWidth.Right = 5;
        root.Style.BorderWidth.Bottom = 5;
        root.Style.Width = 500;
        root.Style.MaxHeight = 300;

        Element child0 = AddChild(root, "root_child0");
        child0.Style.Width = 400;
        child0.Style.Height = 200;

        Element child1 = AddChild(root, "root_child1");
        child1.Style.Width = 400;
        child1.Style.Height = 200;

        LayoutFixture.Calculate(root);

        LayoutFixture.AssertRect(root, 0, 0, 500, 300);
        LayoutFixture.AssertRect(child0, 7, 7, 400, 200);
        LayoutFixture.AssertRect(child1, 7, 207, 400, 200);
        LayoutFixture.AssertNoLayoutBits(root);
    }

    [Fact]
    public void AlignContentFlexStartStretchDoesntInfluenceLineBoxDim()
    {
        // Yoga fixture: align_content_flex_start_stretch_doesnt_influence_line_box_dim
        Element root = new() { Name = "root" };
        root.Style.FlexDirection = FlexDirection.Row;
        root.Style.Position = PositionType.Absolute;
        root.Style.Padding.Left = 20;
        root.Style.Padding.Top = 20;
        root.Style.Padding.Right = 20;
        root.Style.Padding.Bottom = 20;
        root.Style.Width = 400;

        Element child0 = AddChild(root, "root_child0");
        child0.Style.Margin.Right = 20;
        child0.Style.Width = 100;
        child0.Style.Height = 100;

        Element child1 = AddChild(root, "root_child1");
        child1.Style.FlexDirection = FlexDirection.Row;
        child1.Style.FlexWrap = FlexWrap.Wrap;
        child1.Style.FlexGrow = 1;
        child1.Style.FlexShrink = 1;

        Element child1Child0 = AddChild(child1, "root_child1_child0");
        child1Child0.Style.Margin.Right = 20;
        child1Child0.Style.Width = 30;
        child1Child0.Style.Height = 30;

        Element child1Child1 = AddChild(child1, "root_child1_child1");
        child1Child1.Style.Margin.Right = 20;
        child1Child1.Style.Width = 30;
        child1Child1.Style.Height = 30;

        Element child1Child2 = AddChild(child1, "root_child1_child2");
        child1Child2.Style.Margin.Right = 20;
        child1Child2.Style.Width = 30;
        child1Child2.Style.Height = 30;

        Element child1Child3 = AddChild(child1, "root_child1_child3");
        child1Child3.Style.Margin.Right = 20;
        child1Child3.Style.Width = 30;
        child1Child3.Style.Height = 30;

        Element child1Child4 = AddChild(child1, "root_child1_child4");
        child1Child4.Style.Margin.Right = 20;
        child1Child4.Style.Width = 30;
        child1Child4.Style.Height = 30;

        Element child2 = AddChild(root, "root_child2");
        child2.Style.Margin.Left = 20;
        child2.Style.Width = 50;
        child2.Style.Height = 50;

        LayoutFixture.Calculate(root);

        LayoutFixture.AssertRect(root, 0, 0, 400, 140);
        LayoutFixture.AssertRect(child0, 20, 20, 100, 100);
        LayoutFixture.AssertRect(child1, 140, 20, 170, 100);
        LayoutFixture.AssertRect(child1Child0, 0, 0, 30, 30);
        LayoutFixture.AssertRect(child1Child1, 50, 0, 30, 30);
        LayoutFixture.AssertRect(child1Child2, 100, 0, 30, 30);
        LayoutFixture.AssertRect(child1Child3, 0, 30, 30, 30);
        LayoutFixture.AssertRect(child1Child4, 50, 30, 30, 30);
        LayoutFixture.AssertRect(child2, 330, 20, 50, 50);
        LayoutFixture.AssertNoLayoutBits(root);
    }

    [Fact]
    public void AlignContentStretchStretchDoesInfluenceLineBoxDim()
    {
        // Yoga fixture: align_content_stretch_stretch_does_influence_line_box_dim
        Element root = new() { Name = "root" };
        root.Style.FlexDirection = FlexDirection.Row;
        root.Style.Position = PositionType.Absolute;
        root.Style.Padding.Left = 20;
        root.Style.Padding.Top = 20;
        root.Style.Padding.Right = 20;
        root.Style.Padding.Bottom = 20;
        root.Style.Width = 400;

        Element child0 = AddChild(root, "root_child0");
        child0.Style.Margin.Right = 20;
        child0.Style.Width = 100;
        child0.Style.Height = 100;

        Element child1 = AddChild(root, "root_child1");
        child1.Style.FlexDirection = FlexDirection.Row;
        child1.Style.AlignContent = Align.Stretch;
        child1.Style.FlexWrap = FlexWrap.Wrap;
        child1.Style.FlexGrow = 1;
        child1.Style.FlexShrink = 1;

        Element child1Child0 = AddChild(child1, "root_child1_child0");
        child1Child0.Style.Margin.Right = 20;
        child1Child0.Style.Width = 30;
        child1Child0.Style.Height = 30;

        Element child1Child1 = AddChild(child1, "root_child1_child1");
        child1Child1.Style.Margin.Right = 20;
        child1Child1.Style.Width = 30;
        child1Child1.Style.Height = 30;

        Element child1Child2 = AddChild(child1, "root_child1_child2");
        child1Child2.Style.Margin.Right = 20;
        child1Child2.Style.Width = 30;
        child1Child2.Style.Height = 30;

        Element child1Child3 = AddChild(child1, "root_child1_child3");
        child1Child3.Style.Margin.Right = 20;
        child1Child3.Style.Width = 30;
        child1Child3.Style.Height = 30;

        Element child1Child4 = AddChild(child1, "root_child1_child4");
        child1Child4.Style.Margin.Right = 20;
        child1Child4.Style.Width = 30;
        child1Child4.Style.Height = 30;

        Element child2 = AddChild(root, "root_child2");
        child2.Style.Margin.Left = 20;
        child2.Style.Width = 50;
        child2.Style.Height = 50;

        LayoutFixture.Calculate(root);

        LayoutFixture.AssertRect(root, 0, 0, 400, 140);
        LayoutFixture.AssertRect(child0, 20, 20, 100, 100);
        LayoutFixture.AssertRect(child1, 140, 20, 170, 100);
        LayoutFixture.AssertRect(child1Child0, 0, 0, 30, 30);
        LayoutFixture.AssertRect(child1Child1, 50, 0, 30, 30);
        LayoutFixture.AssertRect(child1Child2, 100, 0, 30, 30);
        LayoutFixture.AssertRect(child1Child3, 0, 50, 30, 30);
        LayoutFixture.AssertRect(child1Child4, 50, 50, 30, 30);
        LayoutFixture.AssertRect(child2, 330, 20, 50, 50);
        LayoutFixture.AssertNoLayoutBits(root);
    }

    [Fact]
    public void AlignContentSpaceEvenlyStretchDoesInfluenceLineBoxDim()
    {
        // Yoga fixture: align_content_space_evenly_stretch_does_influence_line_box_dim
        Element root = new() { Name = "root" };
        root.Style.FlexDirection = FlexDirection.Row;
        root.Style.Position = PositionType.Absolute;
        root.Style.Padding.Left = 20;
        root.Style.Padding.Top = 20;
        root.Style.Padding.Right = 20;
        root.Style.Padding.Bottom = 20;
        root.Style.Width = 400;

        Element child0 = AddChild(root, "root_child0");
        child0.Style.Margin.Right = 20;
        child0.Style.Width = 100;
        child0.Style.Height = 100;

        Element child1 = AddChild(root, "root_child1");
        child1.Style.FlexDirection = FlexDirection.Row;
        child1.Style.AlignContent = Align.Stretch;
        child1.Style.FlexWrap = FlexWrap.Wrap;
        child1.Style.FlexGrow = 1;
        child1.Style.FlexShrink = 1;

        Element child1Child0 = AddChild(child1, "root_child1_child0");
        child1Child0.Style.Margin.Right = 20;
        child1Child0.Style.Width = 30;
        child1Child0.Style.Height = 30;

        Element child1Child1 = AddChild(child1, "root_child1_child1");
        child1Child1.Style.Margin.Right = 20;
        child1Child1.Style.Width = 30;
        child1Child1.Style.Height = 30;

        Element child1Child2 = AddChild(child1, "root_child1_child2");
        child1Child2.Style.Margin.Right = 20;
        child1Child2.Style.Width = 30;
        child1Child2.Style.Height = 30;

        Element child1Child3 = AddChild(child1, "root_child1_child3");
        child1Child3.Style.Margin.Right = 20;
        child1Child3.Style.Width = 30;
        child1Child3.Style.Height = 30;

        Element child1Child4 = AddChild(child1, "root_child1_child4");
        child1Child4.Style.Margin.Right = 20;
        child1Child4.Style.Width = 30;
        child1Child4.Style.Height = 30;

        Element child2 = AddChild(root, "root_child2");
        child2.Style.Margin.Left = 20;
        child2.Style.Width = 50;
        child2.Style.Height = 50;

        LayoutFixture.Calculate(root);

        LayoutFixture.AssertRect(root, 0, 0, 400, 140);
        LayoutFixture.AssertRect(child0, 20, 20, 100, 100);
        LayoutFixture.AssertRect(child1, 140, 20, 170, 100);
        LayoutFixture.AssertRect(child1Child0, 0, 0, 30, 30);
        LayoutFixture.AssertRect(child1Child1, 50, 0, 30, 30);
        LayoutFixture.AssertRect(child1Child2, 100, 0, 30, 30);
        LayoutFixture.AssertRect(child1Child3, 0, 50, 30, 30);
        LayoutFixture.AssertRect(child1Child4, 50, 50, 30, 30);
        LayoutFixture.AssertRect(child2, 330, 20, 50, 50);
        LayoutFixture.AssertNoLayoutBits(root);
    }

    [Fact]
    public void AlignContentStretchAndAlignItemsFlexEndWithFlexWrap()
    {
        // Yoga fixture: align_content_stretch_and_align_items_flex_end_with_flex_wrap
        Element root = new() { Name = "root" };
        root.Style.FlexDirection = FlexDirection.Row;
        root.Style.AlignContent = Align.Stretch;
        root.Style.AlignItems = Align.FlexEnd;
        root.Style.Position = PositionType.Absolute;
        root.Style.FlexWrap = FlexWrap.Wrap;
        root.Style.Width = 300;
        root.Style.Height = 300;

        Element child0 = AddChild(root, "root_child0");
        child0.Style.AlignSelf = Align.FlexStart;
        child0.Style.Width = 150;
        child0.Style.Height = 50;

        Element child1 = AddChild(root, "root_child1");
        child1.Style.Width = 120;
        child1.Style.Height = 100;

        Element child2 = AddChild(root, "root_child2");
        child2.Style.Width = 120;
        child2.Style.Height = 50;

        LayoutFixture.Calculate(root);

        LayoutFixture.AssertRect(root, 0, 0, 300, 300);
        LayoutFixture.AssertRect(child0, 0, 0, 150, 50);
        LayoutFixture.AssertRect(child1, 150, 75, 120, 100);
        LayoutFixture.AssertRect(child2, 0, 250, 120, 50);
        LayoutFixture.AssertNoLayoutBits(root);
    }

    [Fact]
    public void AlignContentStretchAndAlignItemsFlexStartWithFlexWrap()
    {
        // Yoga fixture: align_content_stretch_and_align_items_flex_start_with_flex_wrap
        Element root = new() { Name = "root" };
        root.Style.FlexDirection = FlexDirection.Row;
        root.Style.AlignContent = Align.Stretch;
        root.Style.AlignItems = Align.FlexStart;
        root.Style.Position = PositionType.Absolute;
        root.Style.FlexWrap = FlexWrap.Wrap;
        root.Style.Width = 300;
        root.Style.Height = 300;

        Element child0 = AddChild(root, "root_child0");
        child0.Style.AlignSelf = Align.FlexEnd;
        child0.Style.Width = 150;
        child0.Style.Height = 50;

        Element child1 = AddChild(root, "root_child1");
        child1.Style.Width = 120;
        child1.Style.Height = 100;

        Element child2 = AddChild(root, "root_child2");
        child2.Style.Width = 120;
        child2.Style.Height = 50;

        LayoutFixture.Calculate(root);

        LayoutFixture.AssertRect(root, 0, 0, 300, 300);
        LayoutFixture.AssertRect(child0, 0, 125, 150, 50);
        LayoutFixture.AssertRect(child1, 150, 0, 120, 100);
        LayoutFixture.AssertRect(child2, 0, 175, 120, 50);
        LayoutFixture.AssertNoLayoutBits(root);
    }

    [Fact]
    public void AlignContentStretchAndAlignItemsStretchWithFlexWrap()
    {
        // Yoga fixture: align_content_stretch_and_align_items_stretch_with_flex_wrap
        Element root = new() { Name = "root" };
        root.Style.FlexDirection = FlexDirection.Row;
        root.Style.AlignContent = Align.Stretch;
        root.Style.Position = PositionType.Absolute;
        root.Style.FlexWrap = FlexWrap.Wrap;
        root.Style.Width = 300;
        root.Style.Height = 300;

        Element child0 = AddChild(root, "root_child0");
        child0.Style.AlignSelf = Align.FlexEnd;
        child0.Style.Width = 150;
        child0.Style.Height = 50;

        Element child1 = AddChild(root, "root_child1");
        child1.Style.Width = 120;
        child1.Style.Height = 100;

        Element child2 = AddChild(root, "root_child2");
        child2.Style.Width = 120;
        child2.Style.Height = 50;

        LayoutFixture.Calculate(root);

        LayoutFixture.AssertRect(root, 0, 0, 300, 300);
        LayoutFixture.AssertRect(child0, 0, 125, 150, 50);
        LayoutFixture.AssertRect(child1, 150, 0, 120, 100);
        LayoutFixture.AssertRect(child2, 0, 175, 120, 50);
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
