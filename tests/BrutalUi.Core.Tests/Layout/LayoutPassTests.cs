using System;
using System.Collections.Generic;
using Brutal.Numerics;
using Xunit;

namespace BrutalUi.Core.Tests;

/// <summary>
/// Tests of <see cref="UiContext.ViewportSize"/> and of the layout pass in <see cref="UiContext.Update(float)"/>.
/// </summary>
public sealed class LayoutPassTests
{
    [Fact]
    public void ViewportSizeStartsAtZero()
    {
        UiContext context = new();

        Assert.Equal(0f, context.ViewportSize.X);
        Assert.Equal(0f, context.ViewportSize.Y);
    }

    [Fact]
    public void AutoRootGetsViewportSize()
    {
        UiContext context = new() { ViewportSize = new float2(800, 600) };
        Element root = new() { Name = "root" };
        Element child = AddChild(root, "child");
        child.Style.Height = 40;
        context.AddRoot(root);

        context.Update(0f);

        LayoutFixture.AssertRect(root, 0, 0, 800, 600);
        LayoutFixture.AssertRect(child, 0, 0, 800, 40);
        LayoutFixture.AssertNoLayoutBits(root);
    }

    [Fact]
    public void RootWithStyleSizeKeepsStyleSize()
    {
        UiContext context = new() { ViewportSize = new float2(800, 600) };
        Element root = new() { Name = "root" };
        root.Style.Width = 200;
        root.Style.Height = 100;
        context.AddRoot(root);

        context.Update(0f);

        LayoutFixture.AssertRect(root, 0, 0, 200, 100);
        LayoutFixture.AssertNoLayoutBits(root);
    }

    [Fact]
    public void UpdateLaysOutEachRoot()
    {
        UiContext context = new() { ViewportSize = new float2(300, 200) };
        Element first = new() { Name = "first" };
        Element second = new() { Name = "second" };
        second.Style.Width = 50;
        context.AddRoot(first);
        context.AddRoot(second);

        context.Update(0f);

        LayoutFixture.AssertRect(first, 0, 0, 300, 200);
        LayoutFixture.AssertRect(second, 0, 0, 50, 200);
        LayoutFixture.AssertNoLayoutBits(first);
        LayoutFixture.AssertNoLayoutBits(second);
    }

    [Fact]
    public void HeightChangeOfFirstChildMovesSecondChild()
    {
        UiContext context = new() { ViewportSize = new float2(800, 600) };
        Element root = new() { Name = "root" };
        root.Style.Width = 100;
        root.Style.Height = 100;
        Element child0 = AddChild(root, "child0");
        child0.Style.Height = 10;
        Element child1 = AddChild(root, "child1");
        child1.Style.Height = 20;
        context.AddRoot(root);

        context.Update(0f);

        LayoutFixture.AssertRect(child1, 0, 10, 100, 20);
        LayoutFixture.AssertNoLayoutBits(root);

        child0.Style.Height = 30;
        child0.MarkDirty(DirtyFlags.Layout);

        // The CR-1 case: the parent of the changed child holds only the descendant bit.
        Assert.Equal(DirtyFlags.DescendantLayout, root.Dirty & (DirtyFlags.Layout | DirtyFlags.DescendantLayout));
        Assert.Equal(DirtyFlags.None, child1.Dirty);

        context.Update(0f);

        LayoutFixture.AssertRect(root, 0, 0, 100, 100);
        LayoutFixture.AssertRect(child0, 0, 0, 100, 30);
        LayoutFixture.AssertRect(child1, 0, 30, 100, 20);
        LayoutFixture.AssertNoLayoutBits(root);
    }

    [Fact]
    public void LeafGrowthAtDepthFourGrowsEachAutoAncestor()
    {
        UiContext context = new() { ViewportSize = new float2(float.NaN, float.NaN) };
        Element root = new() { Name = "root" };
        Element depth1 = AddChild(root, "depth1");
        Element depth2 = AddChild(depth1, "depth2");
        Element depth3 = AddChild(depth2, "depth3");
        Element leaf = AddChild(depth3, "leaf");
        leaf.Style.Width = 20;
        leaf.Style.Height = 10;
        context.AddRoot(root);

        context.Update(0f);

        Element[] chain = [root, depth1, depth2, depth3, leaf];
        foreach (Element element in chain)
        {
            LayoutFixture.AssertRect(element, 0, 0, 20, 10);
        }

        LayoutFixture.AssertNoLayoutBits(root);

        leaf.Style.Width = 40;
        leaf.Style.Height = 50;
        leaf.MarkDirty(DirtyFlags.Layout);

        context.Update(0f);

        foreach (Element element in chain)
        {
            LayoutFixture.AssertRect(element, 0, 0, 40, 50);
        }

        LayoutFixture.AssertNoLayoutBits(root);
    }

    [Fact]
    public void SecondUpdateWithNoChangeGivesSameRects()
    {
        UiContext context = new() { ViewportSize = new float2(640, 480) };
        Element root = BuildMixedTree();
        context.AddRoot(root);

        context.Update(0f);
        List<float4> first = CollectRects(root);
        LayoutFixture.AssertNoLayoutBits(root);

        context.Update(0f);
        List<float4> second = CollectRects(root);
        LayoutFixture.AssertNoLayoutBits(root);

        Assert.Equal(first.Count, second.Count);
        for (int i = 0; i < first.Count; i++)
        {
            Assert.Equal(first[i].X, second[i].X);
            Assert.Equal(first[i].Y, second[i].Y);
            Assert.Equal(first[i].Z, second[i].Z);
            Assert.Equal(first[i].W, second[i].W);
        }
    }

    [Fact]
    public void NewViewportSizeChangesSizeOfCleanAutoRoot()
    {
        UiContext context = new() { ViewportSize = new float2(800, 600) };
        Element root = new() { Name = "root" };
        Element child = AddChild(root, "child");
        child.Style.Height = 40;
        context.AddRoot(root);

        context.Update(0f);

        LayoutFixture.AssertRect(root, 0, 0, 800, 600);
        Assert.Equal(DirtyFlags.None, root.Dirty);

        context.ViewportSize = new float2(1024, 768);

        // A new viewport size marks nothing.
        Assert.Equal(DirtyFlags.None, root.Dirty);

        context.Update(0f);

        LayoutFixture.AssertRect(root, 0, 0, 1024, 768);
        LayoutFixture.AssertRect(child, 0, 0, 1024, 40);
        LayoutFixture.AssertNoLayoutBits(root);
    }

    [Fact]
    public void NaNViewportComponentLetsRootTakeContentSize()
    {
        UiContext context = new() { ViewportSize = new float2(300, float.NaN) };
        Element root = new() { Name = "root" };
        Element child = AddChild(root, "child");
        child.Style.Height = 25;
        context.AddRoot(root);

        context.Update(0f);

        Assert.Equal(300f, context.ViewportSize.X);
        Assert.True(float.IsNaN(context.ViewportSize.Y));
        LayoutFixture.AssertRect(root, 0, 0, 300, 25);
        LayoutFixture.AssertRect(child, 0, 0, 300, 25);
        LayoutFixture.AssertNoLayoutBits(root);
    }

    [Theory]
    [InlineData(-1f, 0f)]
    [InlineData(0f, -1f)]
    [InlineData(float.PositiveInfinity, 0f)]
    [InlineData(0f, float.PositiveInfinity)]
    [InlineData(float.NegativeInfinity, 0f)]
    [InlineData(0f, float.NegativeInfinity)]
    [InlineData(float.NaN, -1f)]
    public void InvalidViewportComponentThrows(float width, float height)
    {
        UiContext context = new() { ViewportSize = new float2(100, 50) };

        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(
            () => context.ViewportSize = new float2(width, height));

        Assert.Equal("value", exception.ParamName);

        // A failed set keeps the old value.
        Assert.Equal(100f, context.ViewportSize.X);
        Assert.Equal(50f, context.ViewportSize.Y);
    }

    [Fact]
    public void MarkedLeafInHiddenSubtreeGetsZerosAndNoLayoutBit()
    {
        UiContext context = new() { ViewportSize = new float2(800, 600) };
        Element root = new() { Name = "root" };
        root.Style.Width = 100;
        root.Style.Height = 100;
        Element hidden = AddChild(root, "hidden");
        hidden.Style.Display = Display.None;
        hidden.Style.Height = 30;
        Element inner = AddChild(hidden, "inner");
        Element leaf = AddChild(inner, "leaf");
        leaf.Style.Width = 20;
        leaf.Style.Height = 10;
        Element visible = AddChild(root, "visible");
        visible.Style.Height = 10;
        context.AddRoot(root);

        context.Update(0f);

        // The hidden element takes no space, so the visible sibling starts at Y 0.
        LayoutFixture.AssertRect(hidden, 0, 0, 0, 0);
        LayoutFixture.AssertRect(inner, 0, 0, 0, 0);
        LayoutFixture.AssertRect(leaf, 0, 0, 0, 0);
        LayoutFixture.AssertRect(visible, 0, 0, 100, 10);
        LayoutFixture.AssertNoLayoutBits(root);

        // A rect set by hand shows that the zero-out runs again on the marked subtree.
        leaf.Layout.ComputedRect = new float4(1, 2, 3, 4);
        leaf.Style.Width = 40;
        leaf.MarkDirty(DirtyFlags.Layout);

        Assert.Equal(DirtyFlags.DescendantLayout, hidden.Dirty & (DirtyFlags.Layout | DirtyFlags.DescendantLayout));
        Assert.Equal(DirtyFlags.DescendantLayout, inner.Dirty & (DirtyFlags.Layout | DirtyFlags.DescendantLayout));

        context.Update(0f);

        LayoutFixture.AssertRect(hidden, 0, 0, 0, 0);
        LayoutFixture.AssertRect(inner, 0, 0, 0, 0);
        LayoutFixture.AssertRect(leaf, 0, 0, 0, 0);
        LayoutFixture.AssertRect(visible, 0, 0, 100, 10);
        LayoutFixture.AssertNoLayoutBits(root);
    }

    [Fact]
    public void HiddenElementThatBecomesVisibleWithMarkGetsFullLayout()
    {
        UiContext context = new() { ViewportSize = new float2(800, 600) };
        Element root = new() { Name = "root" };
        root.Style.Width = 100;
        root.Style.Height = 100;
        Element first = AddChild(root, "first");
        first.Style.Height = 10;
        Element hidden = AddChild(root, "hidden");
        hidden.Style.Display = Display.None;
        hidden.Style.FlexDirection = FlexDirection.Row;
        hidden.Style.Height = 30;
        Element leaf = AddChild(hidden, "leaf");
        leaf.Style.Width = 20;
        Element last = AddChild(root, "last");
        last.Style.Height = 10;
        context.AddRoot(root);

        context.Update(0f);

        LayoutFixture.AssertRect(first, 0, 0, 100, 10);
        LayoutFixture.AssertRect(hidden, 0, 0, 0, 0);
        LayoutFixture.AssertRect(leaf, 0, 0, 0, 0);
        LayoutFixture.AssertRect(last, 0, 10, 100, 10);
        LayoutFixture.AssertNoLayoutBits(root);

        hidden.Style.Display = Display.Flex;
        hidden.MarkDirty(DirtyFlags.Layout);

        context.Update(0f);

        // first: Y 0, height 10. hidden: Y 0 + 10 = 10, width 100 (stretch), height 30.
        // leaf: X 0, width 20, height 30 (stretch in the row). last: Y 10 + 30 = 40.
        LayoutFixture.AssertRect(first, 0, 0, 100, 10);
        LayoutFixture.AssertRect(hidden, 0, 10, 100, 30);
        LayoutFixture.AssertRect(leaf, 0, 0, 20, 30);
        LayoutFixture.AssertRect(last, 0, 40, 100, 10);
        LayoutFixture.AssertNoLayoutBits(root);
    }

    [Fact]
    public void CleanHiddenSubtreeIsNotEntered()
    {
        UiContext context = new() { ViewportSize = new float2(800, 600) };
        Element root = new() { Name = "root" };
        root.Style.Width = 100;
        root.Style.Height = 100;
        Element hidden = AddChild(root, "hidden");
        hidden.Style.Display = Display.None;
        Element leaf = AddChild(hidden, "leaf");
        leaf.Style.Width = 20;
        leaf.Style.Height = 10;
        Element visible = AddChild(root, "visible");
        visible.Style.Height = 10;
        context.AddRoot(root);

        context.Update(0f);

        LayoutFixture.AssertRect(hidden, 0, 0, 0, 0);
        LayoutFixture.AssertRect(leaf, 0, 0, 0, 0);
        LayoutFixture.AssertNoLayoutBits(root);

        // The engine zeros a clean hidden subtree only when its generation is not 0. The first zero-out set the
        // generation to 0, so the engine does not enter the subtree. A zero-out would clear these rects.
        hidden.Layout.ComputedRect = new float4(5, 6, 7, 8);
        leaf.Layout.ComputedRect = new float4(1, 2, 3, 4);
        visible.Style.Height = 20;
        visible.MarkDirty(DirtyFlags.Layout);

        context.Update(0f);

        LayoutFixture.AssertRect(hidden, 5, 6, 7, 8);
        LayoutFixture.AssertRect(leaf, 1, 2, 3, 4);
        LayoutFixture.AssertRect(visible, 0, 0, 100, 20);
        LayoutFixture.AssertNoLayoutBits(root);
    }

    [Fact]
    public void HiddenRootThatMovesIntoTreeGetsZeros()
    {
        UiContext context = new() { ViewportSize = new float2(300, 200) };
        Element panel = new() { Name = "panel" };
        panel.Style.Display = Display.None;
        Element panelChild = AddChild(panel, "panel_child");
        panelChild.Style.Height = 20;
        Element host = new() { Name = "host" };
        host.Style.Width = 300;
        host.Style.Height = 200;
        Element visible = AddChild(host, "visible");
        visible.Style.Height = 10;
        context.AddRoot(panel);
        context.AddRoot(host);

        context.Update(0f);

        // Display has no effect on a root, so the hidden panel gets the viewport size and a visible layout.
        LayoutFixture.AssertRect(panel, 0, 0, 300, 200);
        LayoutFixture.AssertRect(panelChild, 0, 0, 300, 20);
        LayoutFixture.AssertNoLayoutBits(panel);

        // The move marks the host only. The panel and its child stay clean and keep the visible layout.
        Assert.True(context.RemoveRoot(panel));
        host.Children.Add(panel);

        Assert.Equal(DirtyFlags.None, panel.Dirty & (DirtyFlags.Layout | DirtyFlags.DescendantLayout));
        Assert.Equal(DirtyFlags.None, panelChild.Dirty & (DirtyFlags.Layout | DirtyFlags.DescendantLayout));

        context.Update(0f);

        // The panel is now a hidden child, so the panel and its subtree get zeros. The visible sibling keeps its rect.
        LayoutFixture.AssertRect(panel, 0, 0, 0, 0);
        LayoutFixture.AssertRect(panelChild, 0, 0, 0, 0);
        LayoutFixture.AssertRect(visible, 0, 0, 300, 10);
        LayoutFixture.AssertNoLayoutBits(host);

        // A fresh layout of the same tree gives the same rects.
        UiContext freshContext = new() { ViewportSize = new float2(300, 200) };
        Element freshHost = new() { Name = "host" };
        freshHost.Style.Width = 300;
        freshHost.Style.Height = 200;
        AddChild(freshHost, "visible").Style.Height = 10;
        Element freshPanel = AddChild(freshHost, "panel");
        freshPanel.Style.Display = Display.None;
        AddChild(freshPanel, "panel_child").Style.Height = 20;
        freshContext.AddRoot(freshHost);

        freshContext.Update(0f);

        Assert.Equal(CollectRects(freshHost), CollectRects(host));
    }

    [Fact]
    public void LeftOffsetInRowMovesChildRight()
    {
        UiContext context = new() { ViewportSize = new float2(800, 600) };
        Element root = BuildOffsetTree(FlexDirection.Row, out Element first, out Element second);
        second.Style.Left = 10;
        context.AddRoot(root);

        context.Update(0f);

        // Flex position of second: X = 20 (width of first). Left 10 moves it right: X = 20 + 10 = 30.
        LayoutFixture.AssertRect(first, 0, 0, 20, 100);
        LayoutFixture.AssertRect(second, 30, 0, 30, 100);
        LayoutFixture.AssertNoLayoutBits(root);
    }

    [Fact]
    public void LeftOffsetInRowReverseMovesChildRight()
    {
        UiContext context = new() { ViewportSize = new float2(800, 600) };
        Element root = BuildOffsetTree(FlexDirection.RowReverse, out Element first, out Element second);
        second.Style.Left = 10;
        context.AddRoot(root);

        context.Update(0f);

        // first: X = 100 - 20 = 80. Flex position of second: X = 80 - 30 = 50.
        // Left 10 moves it right: X = 50 + 10 = 60.
        LayoutFixture.AssertRect(first, 80, 0, 20, 100);
        LayoutFixture.AssertRect(second, 60, 0, 30, 100);
        LayoutFixture.AssertNoLayoutBits(root);
    }

    [Fact]
    public void RightOffsetInRowReverseMovesChildLeft()
    {
        UiContext context = new() { ViewportSize = new float2(800, 600) };
        Element root = BuildOffsetTree(FlexDirection.RowReverse, out Element first, out Element second);
        second.Style.Right = 10;
        context.AddRoot(root);

        context.Update(0f);

        // first: X = 100 - 20 = 80. Flex position of second: X = 80 - 30 = 50.
        // Right 10 moves it left: X = 50 - 10 = 40.
        LayoutFixture.AssertRect(first, 80, 0, 20, 100);
        LayoutFixture.AssertRect(second, 40, 0, 30, 100);
        LayoutFixture.AssertNoLayoutBits(root);
    }

    [Fact]
    public void LeftOffsetWinsOverRightOffsetInRowReverse()
    {
        UiContext context = new() { ViewportSize = new float2(800, 600) };
        Element root = BuildOffsetTree(FlexDirection.RowReverse, out _, out Element second);
        second.Style.Left = 10;
        second.Style.Right = 25;
        context.AddRoot(root);

        context.Update(0f);

        // Flex position of second: X = 100 - 20 - 30 = 50. Left applies and Right has no effect: X = 50 + 10 = 60.
        LayoutFixture.AssertRect(second, 60, 0, 30, 100);
        LayoutFixture.AssertNoLayoutBits(root);
    }

    [Fact]
    public void TopOffsetOnCrossAxisOfRowReverseMovesChildDown()
    {
        UiContext context = new() { ViewportSize = new float2(800, 600) };
        Element root = BuildOffsetTree(FlexDirection.RowReverse, out _, out Element second);
        second.Style.Top = 5;
        context.AddRoot(root);

        context.Update(0f);

        // The cross axis of a row reverse is a forward column. Flex position of second: X = 100 - 20 - 30 = 50, Y = 0.
        // Top 5 moves it down: Y = 0 + 5 = 5.
        LayoutFixture.AssertRect(second, 50, 5, 30, 100);
        LayoutFixture.AssertNoLayoutBits(root);
    }

    [Fact]
    public void TopOffsetInColumnReverseMovesChildDown()
    {
        UiContext context = new() { ViewportSize = new float2(800, 600) };
        Element root = BuildOffsetTree(FlexDirection.ColumnReverse, out Element first, out Element second);
        second.Style.Top = 10;
        context.AddRoot(root);

        context.Update(0f);

        // first: Y = 100 - 20 = 80. Flex position of second: Y = 80 - 30 = 50.
        // Top 10 moves it down: Y = 50 + 10 = 60.
        LayoutFixture.AssertRect(first, 0, 80, 100, 20);
        LayoutFixture.AssertRect(second, 0, 60, 100, 30);
        LayoutFixture.AssertNoLayoutBits(root);
    }

    [Fact]
    public void BottomOffsetInColumnReverseMovesChildUp()
    {
        UiContext context = new() { ViewportSize = new float2(800, 600) };
        Element root = BuildOffsetTree(FlexDirection.ColumnReverse, out Element first, out Element second);
        second.Style.Bottom = 10;
        context.AddRoot(root);

        context.Update(0f);

        // first: Y = 100 - 20 = 80. Flex position of second: Y = 80 - 30 = 50.
        // Bottom 10 moves it up: Y = 50 - 10 = 40.
        LayoutFixture.AssertRect(first, 0, 80, 100, 20);
        LayoutFixture.AssertRect(second, 0, 40, 100, 30);
        LayoutFixture.AssertNoLayoutBits(root);
    }

    [Fact]
    public void LeftOffsetInRowReverseKeepsSameRectOnSecondUpdate()
    {
        UiContext context = new() { ViewportSize = new float2(800, 600) };
        Element root = BuildOffsetTree(FlexDirection.RowReverse, out _, out Element second);
        second.Style.Left = 10;
        context.AddRoot(root);

        context.Update(0f);
        context.Update(0f);

        // Step 3 sets the start position again on each pass, so the offset does not add up: X = 50 + 10 = 60.
        LayoutFixture.AssertRect(second, 60, 0, 30, 100);
        LayoutFixture.AssertNoLayoutBits(root);
    }

    [Fact]
    public void ItemsOfWrapReverseRowWithNoOffsetMirrorLines()
    {
        UiContext context = new() { ViewportSize = new float2(800, 600) };
        Element root = BuildWrapTree(
            FlexDirection.Row,
            FlexWrap.WrapReverse,
            out Element first,
            out Element second,
            out Element third);
        context.AddRoot(root);

        context.Update(0f);

        // Before the mirror: line 0 (first, second) at Y 0, line 1 (third) at Y 20.
        // The mirror: Y = 100 - Y - 20. first and second: 100 - 0 - 20 = 80. third: 100 - 20 - 20 = 60.
        LayoutFixture.AssertRect(first, 0, 80, 50, 20);
        LayoutFixture.AssertRect(second, 50, 80, 50, 20);
        LayoutFixture.AssertRect(third, 0, 60, 50, 20);
        LayoutFixture.AssertNoLayoutBits(root);
    }

    [Fact]
    public void TopOffsetInWrapReverseRowMovesChildDown()
    {
        UiContext context = new() { ViewportSize = new float2(800, 600) };
        Element root = BuildWrapTree(
            FlexDirection.Row,
            FlexWrap.WrapReverse,
            out Element first,
            out _,
            out Element third);
        third.Style.Top = 10;
        context.AddRoot(root);

        context.Update(0f);

        // With no offset, third has Y = 100 - 20 - 20 = 60. Top 10 moves it down: Y = 60 + 10 = 70.
        // Yoga mirrors the offset too and gives 50. first does not move: Y = 80.
        LayoutFixture.AssertRect(first, 0, 80, 50, 20);
        LayoutFixture.AssertRect(third, 0, 70, 50, 20);
        LayoutFixture.AssertNoLayoutBits(root);
    }

    [Fact]
    public void BottomOffsetInWrapReverseRowMovesChildUp()
    {
        UiContext context = new() { ViewportSize = new float2(800, 600) };
        Element root = BuildWrapTree(
            FlexDirection.Row,
            FlexWrap.WrapReverse,
            out Element first,
            out _,
            out Element third);
        third.Style.Bottom = 10;
        context.AddRoot(root);

        context.Update(0f);

        // With no offset, third has Y = 100 - 20 - 20 = 60. Bottom 10 moves it up: Y = 60 - 10 = 50.
        // Yoga drops the offset in step 8 and gives 60. first does not move: Y = 80.
        LayoutFixture.AssertRect(first, 0, 80, 50, 20);
        LayoutFixture.AssertRect(third, 0, 50, 50, 20);
        LayoutFixture.AssertNoLayoutBits(root);
    }

    [Fact]
    public void LeftOffsetInWrapReverseColumnMovesChildRight()
    {
        UiContext context = new() { ViewportSize = new float2(800, 600) };
        Element root = BuildWrapTree(
            FlexDirection.Column,
            FlexWrap.WrapReverse,
            out Element first,
            out Element second,
            out Element third);
        third.Style.Left = 10;
        context.AddRoot(root);

        context.Update(0f);

        // Before the mirror: line 0 (first, second) at X 0, line 1 (third) at X 20.
        // The mirror: X = 100 - X - 20. first and second: 80. third with no offset: 100 - 20 - 20 = 60.
        // Left 10 moves third right: X = 60 + 10 = 70. Yoga gives 60.
        LayoutFixture.AssertRect(first, 80, 0, 20, 50);
        LayoutFixture.AssertRect(second, 80, 50, 20, 50);
        LayoutFixture.AssertRect(third, 70, 0, 20, 50);
        LayoutFixture.AssertNoLayoutBits(root);
    }

    [Fact]
    public void TopOffsetInWrapRowWithStretchMovesChildDown()
    {
        UiContext context = new() { ViewportSize = new float2(800, 600) };
        Element root = BuildWrapTree(
            FlexDirection.Row,
            FlexWrap.Wrap,
            out Element first,
            out Element second,
            out Element third);
        second.Style.Height = Length.Auto;
        second.Style.Top = 10;
        third.Style.Top = 10;
        context.AddRoot(root);

        context.Update(0f);

        // The default alignment is stretch. Line 0 height = max(first 20, second 0) = 20. Line 1 starts at 20.
        // second stretches to the line height 20. With no offset, Y = 0. Top 10 moves it down: Y = 0 + 10 = 10.
        // third has a fixed height. With no offset, Y = 20. Top 10 moves it down: Y = 20 + 10 = 30.
        // Yoga drops both offsets in the stretch case and gives 0 and 20.
        LayoutFixture.AssertRect(first, 0, 0, 50, 20);
        LayoutFixture.AssertRect(second, 50, 10, 50, 20);
        LayoutFixture.AssertRect(third, 0, 30, 50, 20);
        LayoutFixture.AssertNoLayoutBits(root);
    }

    [Theory]
    [InlineData(0f, 0f, 0f, 0f, 80f)]
    [InlineData(10f, 0f, 0f, 0f, 80f)]
    [InlineData(0f, 10f, 0f, 0f, 70f)]
    [InlineData(0f, 0f, 5f, 0f, 80f)]
    [InlineData(0f, 0f, 0f, 5f, 75f)]
    public void WrapReverseRowPutsItemInsideBottomPaddingAndMargin(
        float paddingTop,
        float paddingBottom,
        float marginTop,
        float marginBottom,
        float expectedY)
    {
        UiContext context = new() { ViewportSize = new float2(800, 600) };
        Element root = new() { Name = "root" };
        root.Style.FlexDirection = FlexDirection.Row;
        root.Style.FlexWrap = FlexWrap.WrapReverse;
        root.Style.AlignContent = Align.FlexStart;
        root.Style.AlignItems = Align.FlexStart;
        root.Style.Width = 100;
        root.Style.Height = 100;
        root.Style.Padding.Top = paddingTop;
        root.Style.Padding.Bottom = paddingBottom;
        Element item = AddChild(root, "item");
        item.Style.Width = 20;
        item.Style.Height = 20;
        item.Style.Margin.Top = marginTop;
        item.Style.Margin.Bottom = marginBottom;
        context.AddRoot(root);

        context.Update(0f);

        // CSS: wrap-reverse swaps the cross-start and the cross-end edges. The line starts at the bottom edge, inside
        // the bottom padding, and the margin box of the item ends at the line end: Y = 100 - paddingBottom -
        // marginBottom - 20. The top padding and the top margin have no effect. Yoga mirrors the border box against
        // the full height and gives Y = 100 - paddingTop - marginTop - 20.
        LayoutFixture.AssertRect(item, 0, expectedY, 20, 20);
        LayoutFixture.AssertNoLayoutBits(root);
    }

    [Fact]
    public void WrapReverseColumnPutsItemInsideRightPaddingAndMargin()
    {
        UiContext context = new() { ViewportSize = new float2(800, 600) };
        Element root = new() { Name = "root" };
        root.Style.FlexDirection = FlexDirection.Column;
        root.Style.FlexWrap = FlexWrap.WrapReverse;
        root.Style.AlignContent = Align.FlexStart;
        root.Style.AlignItems = Align.FlexStart;
        root.Style.Width = 100;
        root.Style.Height = 100;
        root.Style.Padding.Left = 10;
        Element item = AddChild(root, "item");
        item.Style.Width = 20;
        item.Style.Height = 20;
        item.Style.Margin.Right = 5;
        context.AddRoot(root);

        context.Update(0f);

        // The cross axis is the row axis. The line starts at the right edge (no right padding), and the margin box of
        // the item ends there: X = 100 - 0 - 5 - 20 = 75. The left padding has no effect. Yoga gives 100 - 10 - 20 = 70.
        LayoutFixture.AssertRect(item, 75, 0, 20, 20);
        LayoutFixture.AssertNoLayoutBits(root);
    }

    [Theory]
    [InlineData(Align.FlexStart, 71f, 20f)]
    [InlineData(Align.FlexEnd, 59f, 20f)]
    [InlineData(Align.Center, 65f, 20f)]
    [InlineData(Align.Stretch, 59f, 32f)]
    public void WrapReverseRowMirrorsMarginBoxForEachAlignment(Align alignSelf, float expectedY, float expectedHeight)
    {
        UiContext context = new() { ViewportSize = new float2(800, 600) };
        Element root = new() { Name = "root" };
        root.Style.FlexDirection = FlexDirection.Row;
        root.Style.FlexWrap = FlexWrap.WrapReverse;
        root.Style.AlignContent = Align.FlexStart;
        root.Style.Width = 100;
        root.Style.Height = 100;
        root.Style.Padding.Top = 10;
        root.Style.Padding.Bottom = 4;
        Element item = AddChild(root, "item");
        item.Style.AlignSelf = alignSelf;
        item.Style.Width = 20;
        if (alignSelf != Align.Stretch)
        {
            item.Style.Height = 20;
        }

        item.Style.Margin.Top = 3;
        item.Style.Margin.Bottom = 5;
        Element tall = AddChild(root, "tall");
        tall.Style.Width = 20;
        tall.Style.Height = 40;
        context.AddRoot(root);

        context.Update(0f);

        // The content box spans Y 10 to 96. The line height is max(3 + 20 + 5, 40) = 40. With wrap-reverse, the line
        // spans Y 56 to 96, and its cross-start edge is the bottom edge.
        // FlexStart: the margin box ends at the bottom: Y = 96 - 5 - 20 = 71.
        // FlexEnd: the margin box starts at the top: Y = 56 + 3 = 59.
        // Center: the margin box of 28 starts at 56 + (40 - 28) / 2 = 62: Y = 62 + 3 = 65.
        // Stretch: the margin box fills the line: height = 40 - 3 - 5 = 32, Y = 56 + 3 = 59.
        // tall fills the line: Y = 56.
        LayoutFixture.AssertRect(item, 0, expectedY, 20, expectedHeight);
        LayoutFixture.AssertRect(tall, 20, 56, 20, 40);
        LayoutFixture.AssertNoLayoutBits(root);
    }

    [Fact]
    public void LinesOfWrapReverseRowWithPaddingAndOffsetStartInsideBottomPadding()
    {
        UiContext context = new() { ViewportSize = new float2(800, 600) };
        Element root = BuildWrapTree(
            FlexDirection.Row,
            FlexWrap.WrapReverse,
            out Element first,
            out Element second,
            out Element third);
        root.Style.Padding.Top = 10;
        root.Style.Padding.Bottom = 4;
        third.Style.Top = 10;
        context.AddRoot(root);

        context.Update(0f);

        // The content box spans Y 10 to 96. Line 0 (first, second) spans Y 76 to 96, and line 1 (third) spans Y 56
        // to 76. Top 10 moves third down: Y = 56 + 10 = 66.
        LayoutFixture.AssertRect(first, 0, 76, 50, 20);
        LayoutFixture.AssertRect(second, 50, 76, 50, 20);
        LayoutFixture.AssertRect(third, 0, 66, 50, 20);
        LayoutFixture.AssertNoLayoutBits(root);
    }

    /// <summary>Creates a named child and adds it at the end of the children of <paramref name="parent"/>.</summary>
    /// <param name="parent">The parent element.</param>
    /// <param name="name">The name of the child.</param>
    /// <returns>The new child.</returns>
    private static Element AddChild(Element parent, string name)
    {
        Element child = new() { Name = name };
        parent.Children.Add(child);
        return child;
    }

    /// <summary>
    /// Builds a root of 100 by 100 pixels with two relative children. The main size of the first child is 20, and the
    /// main size of the second child is 30. The cross size of each child stretches to 100.
    /// </summary>
    /// <param name="direction">The flex direction of the root.</param>
    /// <param name="first">The first child.</param>
    /// <param name="second">The second child.</param>
    /// <returns>The root.</returns>
    private static Element BuildOffsetTree(FlexDirection direction, out Element first, out Element second)
    {
        Element root = new() { Name = "root" };
        root.Style.FlexDirection = direction;
        root.Style.Width = 100;
        root.Style.Height = 100;
        first = AddChild(root, "first");
        second = AddChild(root, "second");

        if (direction is FlexDirection.Row or FlexDirection.RowReverse)
        {
            first.Style.Width = 20;
            second.Style.Width = 30;
        }
        else
        {
            first.Style.Height = 20;
            second.Style.Height = 30;
        }

        return root;
    }

    /// <summary>
    /// Builds a wrapped root of 100 by 100 pixels with flex-start lines and three relative children. Each child has a
    /// main size of 50 and a cross size of 20, so the first and the second child fill line 0, and the third child is on
    /// line 1.
    /// </summary>
    /// <param name="direction">The flex direction of the root.</param>
    /// <param name="wrap">The flex wrap of the root.</param>
    /// <param name="first">The first child.</param>
    /// <param name="second">The second child.</param>
    /// <param name="third">The third child.</param>
    /// <returns>The root.</returns>
    private static Element BuildWrapTree(
        FlexDirection direction,
        FlexWrap wrap,
        out Element first,
        out Element second,
        out Element third)
    {
        Element root = new() { Name = "root" };
        root.Style.FlexDirection = direction;
        root.Style.FlexWrap = wrap;
        root.Style.AlignContent = Align.FlexStart;
        root.Style.Width = 100;
        root.Style.Height = 100;
        first = AddChild(root, "first");
        second = AddChild(root, "second");
        third = AddChild(root, "third");

        bool isRow = direction is FlexDirection.Row or FlexDirection.RowReverse;
        foreach (Element child in root.Children)
        {
            child.Style.Width = isRow ? 50 : 20;
            child.Style.Height = isRow ? 20 : 50;
        }

        return root;
    }

    /// <summary>Builds a tree with a column root, a row, a reverse column, fixed sizes, and auto sizes.</summary>
    /// <returns>The root of the tree.</returns>
    private static Element BuildMixedTree()
    {
        Element root = new() { Name = "root" };

        Element row = AddChild(root, "row");
        row.Style.FlexDirection = FlexDirection.Row;
        row.Style.Height = 50;
        AddChild(row, "row_child0").Style.Width = 30;
        AddChild(row, "row_child1").Style.Width = Length.Percent(25);

        Element reverse = AddChild(root, "reverse");
        reverse.Style.FlexDirection = FlexDirection.ColumnReverse;
        reverse.Style.Width = 200;
        AddChild(reverse, "reverse_child0").Style.Height = 15;
        AddChild(reverse, "reverse_child1").Style.Height = 25;

        return root;
    }

    /// <summary>Collects the rect of each element of the tree in depth-first order.</summary>
    /// <param name="root">The root of the tree.</param>
    /// <returns>The rects.</returns>
    private static List<float4> CollectRects(Element root)
    {
        List<float4> rects = [];
        Collect(root, rects);
        return rects;

        static void Collect(Element element, List<float4> rects)
        {
            rects.Add(element.Layout.Rect);
            foreach (Element child in element.Children)
            {
                Collect(child, rects);
            }
        }
    }
}
