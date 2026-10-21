using Brutal.Numerics;
using Xunit;

namespace BrutalUi.Core.Tests;

/// <summary>
/// The tests of <see cref="LayoutNode.ContentInsets"/> and <see cref="LayoutNode.ContentBox(in float4)"/>.
/// </summary>
/// <remarks>
/// The layout pass writes the padding plus the border of each edge. A percent padding resolves against the owner width
/// of the layout call: the inner width of the parent, the padding box width of the parent for an absolute child, or the
/// viewport width for a root. Some tests also compare the left inset with the position of a child at the flex-start
/// edge, so the insets agree with the padding that the engine uses for the children.
/// </remarks>
public sealed class ContentInsetsTests
{
    [Fact]
    public void PixelPaddingAndBorderGiveEachEdge()
    {
        Element root = new() { Name = "root" };
        root.Inline.Width = 200;
        root.Inline.Height = 100;
        root.Inline.Padding = new LengthEdges(1, 2, 3, 4);
        root.Inline.BorderWidth = new LengthEdges(10, 20, 30, 40);

        Element child = AddChild(root, "child");
        child.Inline.Width = 10;
        child.Inline.Height = 10;
        child.Inline.AlignSelf = Align.FlexStart;

        Layout(root, new float2(400f, 300f));

        AssertInsets(root, 11, 22, 33, 44);
        AssertInsets(child, 0, 0, 0, 0);
        LayoutFixture.AssertRect(child, 11, 22, 10, 10);
    }

    [Fact]
    public void ContentBoxRemovesTheInsets()
    {
        Element root = new() { Name = "root" };
        root.Inline.Width = 200;
        root.Inline.Height = 100;
        root.Inline.Padding = new LengthEdges(1, 2, 3, 4);
        root.Inline.BorderWidth = new LengthEdges(10, 20, 30, 40);

        Layout(root, new float2(400f, 300f));

        float4 box = root.Layout.ContentBox(new float4(5f, 7f, 200f, 100f));

        // 200 - (11 + 33) is 156, and 100 - (22 + 44) is 34.
        Assert.Equal(new float4(16f, 29f, 156f, 34f), box);
    }

    [Fact]
    public void ContentBoxGivesNoNegativeSize()
    {
        LayoutNode layout = default;
        layout.ContentInsets = new float4(5f, 6f, 7f, 8f);

        float4 box = layout.ContentBox(new float4(1f, 2f, 10f, 12f));

        Assert.Equal(new float4(6f, 8f, 0f, 0f), box);
    }

    [Fact]
    public void PaddingLargerThanTheSizeGivesAnEmptyContentBox()
    {
        Element root = new() { Name = "root" };
        root.Inline.Width = 10;
        root.Inline.Height = 10;
        root.Inline.Padding = LengthEdges.All(8);

        Layout(root, new float2(400f, 300f));

        // The engine grows the border box to the padding: 16 by 16.
        LayoutFixture.AssertRect(root, 0, 0, 16, 16);
        AssertInsets(root, 8, 8, 8, 8);
        Assert.Equal(new float4(8f, 8f, 0f, 0f), root.Layout.ContentBox(root.Layout.Rect));
    }

    [Fact]
    public void PercentPaddingOfAChildResolvesAgainstTheInnerWidthOfTheParent()
    {
        Element root = new() { Name = "root" };
        root.Inline.Width = 220;
        root.Inline.Height = 300;
        root.Inline.Padding = root.Inline.Padding with { Left = 5, Right = 5 };
        root.Inline.BorderWidth = root.Inline.BorderWidth with { Left = 5, Right = 5 };

        Element child = AddChild(root, "child");
        child.Inline.Height = 100;
        child.Inline.Padding = LengthEdges.All(Length.Percent(10));
        child.Inline.BorderWidth = child.Inline.BorderWidth with { Left = 3 };

        Element grand = AddChild(child, "grand");
        grand.Inline.Width = 10;
        grand.Inline.Height = 10;
        grand.Inline.AlignSelf = Align.FlexStart;

        Layout(root, new float2(400f, 300f));

        // The inner width of the parent is 220 - 20 = 200. Each percent edge resolves against the width.
        AssertInsets(child, 23, 20, 20, 20);
        LayoutFixture.AssertRect(grand, 23, 20, 10, 10);
    }

    [Fact]
    public void PercentPaddingOfAnAbsoluteChildResolvesAgainstThePaddingBoxOfTheParent()
    {
        Element root = new() { Name = "root" };
        root.Inline.Width = 220;
        root.Inline.Height = 300;
        root.Inline.Padding = root.Inline.Padding with { Left = 5, Right = 5 };
        root.Inline.BorderWidth = root.Inline.BorderWidth with { Left = 5, Right = 5 };

        Element child = AddChild(root, "child");
        child.Inline.Position = PositionType.Absolute;
        child.Inline.Width = 100;
        child.Inline.Height = 100;
        child.Inline.Padding = child.Inline.Padding with { Left = Length.Percent(10) };

        Element grand = AddChild(child, "grand");
        grand.Inline.Width = 10;
        grand.Inline.Height = 10;

        Layout(root, new float2(400f, 300f));

        // The containing block is the padding box of the parent: 220 - 10 = 210.
        AssertInsets(child, 21, 0, 0, 0);
        Assert.Equal(child.Layout.ContentInsets.X, grand.Layout.Rect.X, LayoutFixture.Tolerance);
    }

    [Fact]
    public void PercentPaddingOfARootResolvesAgainstTheViewportWidth()
    {
        Element root = new() { Name = "root" };
        root.Inline.Width = 100;
        root.Inline.Height = 100;
        root.Inline.Padding = new LengthEdges(Length.Percent(10), Length.Percent(5), Length.Percent(2.5f), 3);

        Element grand = AddChild(root, "grand");
        grand.Inline.Width = 10;
        grand.Inline.Height = 10;

        Layout(root, new float2(400f, 300f));

        // Each percent edge resolves against the viewport width 400, also the top edge.
        AssertInsets(root, 40, 20, 10, 3);
        LayoutFixture.AssertRect(grand, 40, 20, 10, 10);
    }

    [Fact]
    public void PercentPaddingOfARootWithAnUndefinedViewportWidthGivesZero()
    {
        Element root = new() { Name = "root" };
        root.Inline.Width = 100;
        root.Inline.Height = 100;
        root.Inline.Padding = root.Inline.Padding with { Left = Length.Percent(10), Top = Length.Percent(10), Right = 4 };
        root.Inline.BorderWidth = root.Inline.BorderWidth with { Bottom = 2 };

        Layout(root, new float2(float.NaN, 300f));

        AssertInsets(root, 0, 0, 4, 2);
        Assert.False(float.IsNaN(root.Layout.ContentInsets.X));
    }

    [Fact]
    public void InsetsFollowTheParentWidthWithACleanChild()
    {
        Element root = new() { Name = "root" };
        root.Inline.Width = 200;
        root.Inline.Height = 200;
        root.Inline.AlignItems = Align.FlexStart;

        Element child = AddChild(root, "child");
        child.Inline.Width = 100;
        child.Inline.Height = 100;
        child.Inline.Padding = child.Inline.Padding with { Left = Length.Percent(10), Top = Length.Percent(5) };

        UiContext context = new() { ViewportSize = new float2(1000f, 1000f) };
        context.AddRoot(root);
        context.Update(0f);
        AssertInsets(child, 20, 10, 0, 0);

        root.Inline.Width = 300;
        root.MarkDirty(DirtyFlags.Layout);
        Assert.Equal(DirtyFlags.None, child.Dirty & (DirtyFlags.Layout | DirtyFlags.DescendantLayout));

        context.Update(0f);

        // 10 percent of 300 is 30, and 5 percent of 300 is 15.
        AssertInsets(child, 30, 15, 0, 0);
        LayoutFixture.AssertNoLayoutBits(root);
        context.RemoveRoot(root);
    }

    [Fact]
    public void InsetsFollowANewViewportAtACleanRoot()
    {
        Element root = new() { Name = "root" };
        root.Inline.Width = 100;
        root.Inline.Height = 100;
        root.Inline.Padding = root.Inline.Padding with { Left = Length.Percent(10) };

        UiContext context = new() { ViewportSize = new float2(200f, 200f) };
        context.AddRoot(root);
        context.Update(0f);
        AssertInsets(root, 20, 0, 0, 0);

        context.ViewportSize = new float2(400f, 400f);
        context.Update(0f);

        AssertInsets(root, 40, 0, 0, 0);
        context.RemoveRoot(root);
    }

    [Fact]
    public void LayoutCacheHitAfterAMeasurementWithAnotherOwnerWidthKeepsTheFinalInsets()
    {
        // Pass 1 gives the parent a fixed width, so the child gets a layout with the owner width 100 and no
        // measurement. Pass 2 removes the width of the parent. The parent gets the limit 300 for its flex basis, so the
        // parent measurement measures the clean child with the owner width 300 (a miss). Then the parent layout gives
        // the child the same layout constraints as pass 1 (a hit). A measurement must not change the insets, and the
        // hit must keep the value of the final layout.
        Element root = new() { Name = "root" };
        root.Inline.FlexDirection = FlexDirection.Row;
        root.Inline.Width = 300;
        root.Inline.Height = 200;
        root.Inline.AlignItems = Align.FlexStart;

        Element parent = AddChild(root, "parent");
        parent.Inline.Width = 100;
        parent.Inline.Height = 50;

        Element child = AddChild(parent, "child");
        child.Inline.Width = 100;
        child.Inline.Height = 50;
        child.Inline.Padding = child.Inline.Padding with { Left = Length.Percent(10) };

        UiContext context = new() { ViewportSize = new float2(1000f, 1000f) };
        context.AddRoot(root);
        context.Update(0f);
        AssertInsets(child, 10, 0, 0, 0);
        Assert.Equal(0, child.Layout.Cache.NextCachedMeasurementsIndex);

        parent.Inline.Width = Length.Auto;
        parent.MarkDirty(DirtyFlags.Layout);
        context.Update(0f);

        // The measurement with the owner width 300 ran and stored an entry. It resolves the padding to 30.
        Assert.Equal(1, child.Layout.Cache.NextCachedMeasurementsIndex);
        Assert.Equal(300f, child.Layout.Cache.CachedMeasurements[0].OwnerWidth);
        Assert.Equal(100f, child.Layout.Cache.CachedLayout.OwnerWidth);

        LayoutFixture.AssertRect(parent, 0, 0, 100, 50);
        AssertInsets(child, 10, 0, 0, 0);
        LayoutFixture.AssertNoLayoutBits(root);
        LayoutDifferentialTests.AssertMatchesFreshLayout(root, context.ViewportSize, "after the auto width of parent");
        context.RemoveRoot(root);
    }

    [Fact]
    public void HiddenElementGetsZeroInsets()
    {
        Element root = new() { Name = "root" };
        root.Inline.Width = 200;
        root.Inline.Height = 200;

        Element child = AddChild(root, "child");
        child.Inline.Height = 50;
        child.Inline.Padding = LengthEdges.All(4);

        Element grand = AddChild(child, "grand");
        grand.Inline.Height = 10;
        grand.Inline.BorderWidth = LengthEdges.All(2);

        UiContext context = new() { ViewportSize = new float2(400f, 400f) };
        context.AddRoot(root);
        context.Update(0f);
        AssertInsets(child, 4, 4, 4, 4);
        AssertInsets(grand, 2, 2, 2, 2);

        child.Inline.Display = Display.None;
        child.MarkDirty(DirtyFlags.Layout);
        context.Update(0f);

        LayoutFixture.AssertRect(child, 0, 0, 0, 0);
        AssertInsets(child, 0, 0, 0, 0);
        AssertInsets(grand, 0, 0, 0, 0);

        child.Inline.Display = Display.Flex;
        child.MarkDirty(DirtyFlags.Layout);
        context.Update(0f);

        AssertInsets(child, 4, 4, 4, 4);
        AssertInsets(grand, 2, 2, 2, 2);
        context.RemoveRoot(root);
    }

    [Fact]
    public void ElementBeforeTheFirstLayoutHasZeroInsets()
    {
        Element element = new() { Name = "element" };
        element.Inline.Padding = LengthEdges.All(4);

        Assert.Equal(default, element.Layout.ContentInsets);
    }

    private static void Layout(Element root, float2 viewport)
    {
        UiContext context = new() { ViewportSize = viewport };
        context.AddRoot(root);
        context.Update(0f);
        context.RemoveRoot(root);
    }

    private static void AssertInsets(Element element, float left, float top, float right, float bottom)
    {
        float4 insets = element.Layout.ContentInsets;
        Assert.Equal(left, insets.X, LayoutFixture.Tolerance);
        Assert.Equal(top, insets.Y, LayoutFixture.Tolerance);
        Assert.Equal(right, insets.Z, LayoutFixture.Tolerance);
        Assert.Equal(bottom, insets.W, LayoutFixture.Tolerance);
    }

    private static Element AddChild(Element parent, string name)
    {
        Element child = new() { Name = name };
        parent.Children.Add(child);
        return child;
    }
}
