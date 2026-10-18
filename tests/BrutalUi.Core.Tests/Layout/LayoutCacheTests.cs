using Brutal.Numerics;
using Xunit;

namespace BrutalUi.Core.Tests;

/// <summary>
/// The tests of the layout cache and the dirty-subtree rule. A counting <see cref="MeasureStub"/> on each leaf shows
/// which leaves the engine measures again.
/// </summary>
/// <remarks>
/// <para>
/// Each stub uses a measure function that keeps the callback contract of the cache: a fixed size,
/// <see cref="MeasureStub.WrappingText"/>, or <see cref="MeasureStub.ClampedText"/>.
/// </para>
/// <para>
/// The regression tests hold the cases that a pure port of the Yoga cache gets wrong (see "The cache is stricter than
/// Yoga" in <c>context.md</c>). Each one compares the incremental result with a hand-computed rect and with a fresh
/// layout of a copy of the tree.
/// </para>
/// </remarks>
public sealed class LayoutCacheTests
{
    [Fact]
    public void SecondUpdateWithNoChangeCallsNoMeasureCallback()
    {
        // A column root with a row header, a wrapped row body, and an absolute leaf. Each leaf counts its calls.
        Element root = new() { Name = "root" };

        Element header = AddChild(root, "header");
        header.Style.FlexDirection = FlexDirection.Row;
        header.Style.Height = 40;
        MeasureStub title = new(MeasureStub.ClampedText);
        Element titleLeaf = AddLeaf(header, "title", title);
        titleLeaf.Style.FlexGrow = 1;
        MeasureStub badge = new(MeasureStub.WrappingText);
        AddLeaf(header, "badge", badge);

        Element body = AddChild(root, "body");
        body.Style.FlexDirection = FlexDirection.Row;
        body.Style.FlexWrap = FlexWrap.Wrap;
        body.Style.FlexGrow = 1;
        MeasureStub[] tiles = new MeasureStub[6];
        for (int i = 0; i < tiles.Length; i++)
        {
            tiles[i] = new MeasureStub(30, 20);
            Element tile = AddLeaf(body, "tile" + i, tiles[i]);
            tile.Style.Margin = LengthEdges.All(5);
        }

        MeasureStub overlay = new(10, 10);
        Element overlayLeaf = AddLeaf(root, "overlay", overlay);
        overlayLeaf.Style.Position = PositionType.Absolute;
        overlayLeaf.Style.Right = 4;

        MeasureStub[] stubs = [title, badge, .. tiles, overlay];

        UiContext context = new() { ViewportSize = new float2(400f, 300f) };
        context.AddRoot(root);
        context.Update(0f);
        LayoutFixture.AssertNoLayoutBits(root);

        foreach (MeasureStub stub in stubs)
        {
            Assert.True(stub.Count > 0, "The first pass measures each leaf.");
            stub.Reset();
        }

        float4 bodyRect = body.Layout.Rect;
        float4 titleRect = titleLeaf.Layout.Rect;

        // The second pass: no mark and the same viewport. The root hits the cache, so no callback runs.
        context.Update(0f);
        LayoutFixture.AssertNoLayoutBits(root);

        Assert.Equal(0, SumOfCounts(stubs));
        Assert.Equal(bodyRect, body.Layout.Rect);
        Assert.Equal(titleRect, titleLeaf.Layout.Rect);
        LayoutDifferentialTests.AssertMatchesFreshLayout(root, context.ViewportSize, "after the second Update");
    }

    [Fact]
    public void MarkOnOneLeafCallsOnlyThatLeaf()
    {
        // A row of 4 equal text leaves. Each leaf gets the same constraints: a basis measurement with
        // (AtMost 400, AtMost 300), and a layout with (Exactly 60, AtMost 300). The exact width 60 is the old
        // measured width, so the layout hits the measurement entry: 1 call for each leaf in the first pass.
        Element root = new() { Name = "root" };
        root.Style.FlexDirection = FlexDirection.Row;
        root.Style.AlignItems = Align.FlexStart;
        root.Style.Width = 400;
        root.Style.Height = 300;

        MeasureStub[] stubs = new MeasureStub[4];
        Element[] leaves = new Element[4];
        for (int i = 0; i < stubs.Length; i++)
        {
            stubs[i] = new MeasureStub(MeasureStub.ClampedText);
            leaves[i] = AddLeaf(root, "leaf" + i, stubs[i]);
        }

        UiContext context = new() { ViewportSize = new float2(800f, 600f) };
        context.AddRoot(root);
        context.Update(0f);
        LayoutFixture.AssertNoLayoutBits(root);

        foreach (MeasureStub stub in stubs)
        {
            Assert.Equal(1, stub.Count);
            stub.Reset();
        }

        // The mark resets the cache of leaf 2 only. The root lays out its children again, and the clean siblings
        // hit their caches with the same constraints.
        leaves[2].MarkDirty(DirtyFlags.Layout);
        context.Update(0f);
        LayoutFixture.AssertNoLayoutBits(root);

        Assert.Equal(0, stubs[0].Count);
        Assert.Equal(0, stubs[1].Count);
        Assert.Equal(1, stubs[2].Count);
        Assert.Equal(0, stubs[3].Count);
        LayoutFixture.AssertRect(leaves[2], 120, 0, 60, 16);
        LayoutDifferentialTests.AssertMatchesFreshLayout(root, context.ViewportSize, "after the mark of leaf2");

        // A third pass with no mark calls no callback. The fresh copy shares the callbacks, so its layout added
        // counts: reset each stub first.
        foreach (MeasureStub stub in stubs)
        {
            stub.Reset();
        }

        context.Update(0f);
        LayoutFixture.AssertNoLayoutBits(root);
        Assert.Equal(0, SumOfCounts(stubs));
    }

    [Fact]
    public void GrowOfOneChildCallsSiblingWhoseFreeSpaceChanges()
    {
        // A row of 200 by 100: a fixed child A, a text leaf B that can shrink, and a fixed-size box C with a leaf L.
        Element root = new() { Name = "root" };
        root.Style.FlexDirection = FlexDirection.Row;
        root.Style.AlignItems = Align.FlexStart;
        root.Style.Width = 200;
        root.Style.Height = 100;

        Element a = AddChild(root, "a");
        a.Style.Width = 50;
        a.Style.Height = 10;

        MeasureStub textStub = new(MeasureStub.WrappingText);
        Element b = AddLeaf(root, "b", textStub);
        b.Style.FlexShrink = 1;

        Element c = AddChild(root, "c");
        c.Style.Width = 30;
        c.Style.Height = 30;
        MeasureStub innerStub = new(10, 10);
        AddLeaf(c, "l", innerStub);

        UiContext context = new() { ViewportSize = new float2(500f, 500f) };
        context.AddRoot(root);
        context.Update(0f);
        LayoutFixture.AssertNoLayoutBits(root);

        // The free space is 200 - 50 - 68 - 30 = 52, so B keeps its width of 68.
        LayoutFixture.AssertRect(b, 50, 0, 68, 16);
        textStub.Reset();
        innerStub.Reset();

        // A grows to 150. The free space is 200 - 150 - 68 - 30 = -48, so B shrinks to 68 - 48 = 20. The text
        // wraps at a width under 68 and gives a height of 32. C keeps its constraints (Exactly 30, Exactly 30).
        a.Style.Width = 150;
        a.MarkDirty(DirtyFlags.Layout);
        context.Update(0f);
        LayoutFixture.AssertNoLayoutBits(root);

        Assert.Equal(1, textStub.Count);
        Assert.Equal(0, innerStub.Count);
        LayoutFixture.AssertRect(b, 150, 0, 20, 32);
        LayoutFixture.AssertRect(c, 170, 0, 30, 30);
        LayoutDifferentialTests.AssertMatchesFreshLayout(root, context.ViewportSize, "after the grow of a");
    }

    [Fact]
    public void NewViewportSizeCallsOnlySubtreesWithNewConstraints()
    {
        // A column root with an auto size. P has a fixed size, so its constraints do not change with the viewport.
        // Q stretches to the viewport width, so its text leaf gets a new exact width.
        Element root = new() { Name = "root" };

        Element p = AddChild(root, "p");
        p.Style.Width = 100;
        p.Style.Height = 50;
        MeasureStub fixedStub = new(10, 10);
        AddLeaf(p, "fixed", fixedStub);

        Element q = AddChild(root, "q");
        MeasureStub textStub = new(MeasureStub.ClampedText);
        Element text = AddLeaf(q, "text", textStub);

        UiContext context = new() { ViewportSize = new float2(300f, 200f) };
        context.AddRoot(root);
        context.Update(0f);
        LayoutFixture.AssertNoLayoutBits(root);
        LayoutFixture.AssertRect(text, 0, 0, 300, 16);
        fixedStub.Reset();
        textStub.Reset();

        // A new viewport sets no dirty bit. The root misses the cache with the new size.
        context.ViewportSize = new float2(400f, 200f);
        context.Update(0f);
        LayoutFixture.AssertNoLayoutBits(root);

        Assert.True(textStub.Count > 0, "The text leaf gets a new exact width, so the engine measures it again.");
        Assert.Equal(0, fixedStub.Count);
        LayoutFixture.AssertRect(q, 0, 50, 400, 16);
        LayoutFixture.AssertRect(text, 0, 0, 400, 16);
        LayoutDifferentialTests.AssertMatchesFreshLayout(root, context.ViewportSize, "after the new viewport");
    }

    [Fact]
    public void NewViewportSizeWithFixedSizeRootCallsNoCallback()
    {
        Element root = new() { Name = "root" };
        root.Style.FlexDirection = FlexDirection.Row;
        root.Style.Width = 400;
        root.Style.Height = 300;

        MeasureStub[] stubs = new MeasureStub[3];
        for (int i = 0; i < stubs.Length; i++)
        {
            Element column = AddChild(root, "column" + i);
            column.Style.Width = 100;
            stubs[i] = new MeasureStub(MeasureStub.ClampedText);
            AddLeaf(column, "leaf" + i, stubs[i]);
        }

        UiContext context = new() { ViewportSize = new float2(400f, 300f) };
        context.AddRoot(root);
        context.Update(0f);
        foreach (MeasureStub stub in stubs)
        {
            stub.Reset();
        }

        // The root has a style size, so its constraints stay (Exactly 400, Exactly 300). The root hits the cache.
        context.ViewportSize = new float2(800f, 600f);
        context.Update(0f);
        LayoutFixture.AssertNoLayoutBits(root);

        Assert.Equal(0, SumOfCounts(stubs));
        LayoutDifferentialTests.AssertMatchesFreshLayout(root, context.ViewportSize, "after the new viewport");
    }

    [Fact]
    public void LeafWithTopMarginGetsNewSizeWhenParentHeightBecomesAuto()
    {
        // Regression (rule 4 of the measurement lookup compares inner sizes). Yoga compares the old limit with the
        // margins and the new limit without the margins, so a clamped size can stay under a larger limit.
        Element a = new() { Name = "a" };

        Element b = AddChild(a, "b");
        b.Style.Width = 23;
        b.Style.Height = 128;
        b.Style.Padding.Left = 5;
        b.Style.Padding.Right = 7;
        b.Style.Padding.Top = 4;

        Element c = AddChild(b, "c");
        c.Style.Padding.Left = 0;
        c.Style.Padding.Top = 6;
        c.Style.BorderWidth.Top = 10;

        Element d = AddChild(c, "d");
        d.Style.Margin.Left = 0;
        d.Style.Margin.Top = 9;
        d.Style.Padding.Top = 4;
        d.Layout.Measure = MeasureStub.ClampedText;

        UiContext context = new() { ViewportSize = new float2(391f, float.NaN) };
        context.AddRoot(a);
        context.Update(0f);
        LayoutFixture.AssertNoLayoutBits(a);

        b.Style.Height = Length.Undefined;
        b.MarkDirty(DirtyFlags.Layout);
        context.Update(0f);
        LayoutFixture.AssertNoLayoutBits(a);

        // The inner width of b is 23 - 5 - 7 = 11. The text wraps into ceil(60 / 11) = 6 lines of 16 = 96.
        // d: 96 + 4 = 100. c: 9 + 100 + 6 + 10 = 125. b: 125 + 4 = 129.
        LayoutFixture.AssertRect(d, 0, 25, 11, 100);
        LayoutFixture.AssertRect(c, 5, 4, 11, 125);
        LayoutFixture.AssertRect(b, 0, 0, 23, 129);
        LayoutDifferentialTests.AssertMatchesFreshLayout(a, context.ViewportSize, "after the auto height of b");
    }

    [Fact]
    public void CallbackLeafWithMaxWidthZeroIsMeasuredForNewLimit()
    {
        // Regression (a leaf with a minimum or a maximum uses the exact key). The maximum clamps the stored size, so
        // the relaxed rules of Yoga read a wrong callback size for a new limit.
        Element a = new() { Name = "a" };
        Element b = AddChild(a, "b");
        Element c = AddChild(b, "c");
        Element d = AddChild(c, "d");
        Element f = AddChild(d, "f");
        f.Style.FlexDirection = FlexDirection.Row;
        Element g = AddChild(f, "g");
        Element h = AddChild(g, "h");
        h.Style.Position = PositionType.Absolute;
        h.Style.MaxWidth = 0;
        h.Layout.Measure = MeasureStub.ClampedText;

        UiContext context = new() { ViewportSize = new float2(413f, 268f) };
        context.AddRoot(a);
        context.Update(0f);
        LayoutFixture.AssertNoLayoutBits(a);

        g.Style.FlexBasis = 25;
        g.MarkDirty(DirtyFlags.Layout);
        context.Update(0f);
        LayoutFixture.AssertNoLayoutBits(a);

        // g is 25 wide. The callback of h gets (AtMost 25, Undefined): ceil(60 / 25) = 3 lines of 16 = 48.
        // The maximum width of 0 clamps the width.
        LayoutFixture.AssertRect(g, 0, 0, 25, 0);
        LayoutFixture.AssertRect(h, 0, 0, 0, 48);
        LayoutDifferentialTests.AssertMatchesFreshLayout(a, context.ViewportSize, "after the basis of g");
    }

    [Fact]
    public void PercentPaddingOfCleanChildFollowsNewOwnerWidth()
    {
        // Regression (a new owner size resets the cache). The child keeps the same constraints, but its percent
        // padding resolves against the new width of its parent.
        Element root = new() { Name = "root" };
        root.Style.Width = 200;
        root.Style.Height = 200;
        root.Style.AlignItems = Align.FlexStart;

        Element child = AddChild(root, "child");
        child.Style.Width = 100;
        child.Style.Height = 100;
        child.Style.Padding.Left = Length.Percent(10);

        Element grand = AddChild(child, "grand");
        grand.Style.Width = 10;
        grand.Style.Height = 10;

        UiContext context = new() { ViewportSize = new float2(1000f, 1000f) };
        context.AddRoot(root);
        context.Update(0f);
        LayoutFixture.AssertRect(grand, 20, 0, 10, 10);

        root.Style.Width = 300;
        root.MarkDirty(DirtyFlags.Layout);
        context.Update(0f);
        LayoutFixture.AssertNoLayoutBits(root);

        // 10 percent of 300 is 30.
        LayoutFixture.AssertRect(grand, 30, 0, 10, 10);
        LayoutDifferentialTests.AssertMatchesFreshLayout(root, context.ViewportSize, "after the new width of root");
    }

    [Fact]
    public void PercentPaddingOfFixedSizeRootFollowsNewViewport()
    {
        // Regression (a new owner size resets the cache). The root resolves its percent padding against the viewport.
        Element root = new() { Name = "root" };
        root.Style.Width = 100;
        root.Style.Height = 100;
        root.Style.Padding.Left = Length.Percent(10);

        Element grand = AddChild(root, "grand");
        grand.Style.Width = 10;
        grand.Style.Height = 10;

        UiContext context = new() { ViewportSize = new float2(200f, 200f) };
        context.AddRoot(root);
        context.Update(0f);
        LayoutFixture.AssertRect(grand, 20, 0, 10, 10);

        context.ViewportSize = new float2(400f, 400f);
        context.Update(0f);
        LayoutFixture.AssertNoLayoutBits(root);

        // 10 percent of 400 is 40.
        LayoutFixture.AssertRect(grand, 40, 0, 10, 10);
        LayoutDifferentialTests.AssertMatchesFreshLayout(root, context.ViewportSize, "after the new viewport");
    }

    [Fact]
    public void PercentLeftOfFixedSizeRootFollowsNewViewport()
    {
        // Regression (the root position on each pass). The root hits the cache, but its percent offset resolves
        // against the new viewport.
        Element root = new() { Name = "root" };
        root.Style.Width = 100;
        root.Style.Height = 100;
        root.Style.Left = Length.Percent(10);

        UiContext context = new() { ViewportSize = new float2(200f, 200f) };
        context.AddRoot(root);
        context.Update(0f);
        LayoutFixture.AssertRect(root, 20, 0, 100, 100);

        context.ViewportSize = new float2(400f, 400f);
        context.Update(0f);
        LayoutFixture.AssertNoLayoutBits(root);

        // 10 percent of 400 is 40.
        LayoutFixture.AssertRect(root, 40, 0, 100, 100);
        LayoutDifferentialTests.AssertMatchesFreshLayout(root, context.ViewportSize, "after the new viewport");
    }

    [Fact]
    public void HideAndShowOfSubtreeGivesFreshLayout()
    {
        // Regression (the zero-out clears the cache). A clean descendant of a hidden element must not keep its cache.
        Element root = new() { Name = "root" };
        Element a = AddChild(root, "a");
        a.Style.Height = 50;
        Element b = AddChild(root, "b");
        Element inner = AddChild(b, "inner");
        inner.Style.Width = 30;
        inner.Style.Height = 30;

        UiContext context = new() { ViewportSize = new float2(300f, 300f) };
        context.AddRoot(root);
        context.Update(0f);

        b.Style.Display = Display.None;
        b.MarkDirty(DirtyFlags.Layout);
        context.Update(0f);
        LayoutFixture.AssertNoLayoutBits(root);
        LayoutFixture.AssertRect(b, 0, 0, 0, 0);
        LayoutFixture.AssertRect(inner, 0, 0, 0, 0);

        b.Style.Display = Display.Flex;
        b.MarkDirty(DirtyFlags.Layout);
        context.Update(0f);
        LayoutFixture.AssertNoLayoutBits(root);

        LayoutFixture.AssertRect(b, 0, 50, 300, 30);
        LayoutFixture.AssertRect(inner, 0, 0, 30, 30);
        LayoutDifferentialTests.AssertMatchesFreshLayout(root, context.ViewportSize, "after the show of b");
    }

    [Fact]
    public void LeafMarkInDeepPercentChainStaysInCallBudget()
    {
        // Regression (CR-1 of the code review: the owner size is a part of the key). A chain of 20 levels below the
        // root. The levels alternate Row and Column, and each level has a percent padding of 1 on each edge. The last
        // level is a text leaf. A mark on the leaf dirties each level. When a new owner size reset all entries of an
        // element, one Update gave 5951 callback calls. With the owner size in the key, it gives 3031 calls.
        // The budget of 4000 is the new count with a margin of about 30 percent. The old form fails it.
        // The count still grows with the depth (46 calls at depth 10), because the leaf gets about 2500 different
        // constraints. A later feature (allocation-guard) owns a benchmark for that growth.
        const int Depth = 20;
        const int CallBudget = 4000;

        Element root = new() { Name = "root" };
        Element current = root;
        for (int i = 0; i < Depth; i++)
        {
            Element level = AddChild(current, "level" + i);
            level.Style.FlexDirection = (i % 2 == 0) ? FlexDirection.Row : FlexDirection.Column;
            level.Style.Padding = LengthEdges.All(Length.Percent(1));
            current = level;
        }

        MeasureStub text = new(MeasureStub.ClampedText);
        current.Layout.Measure = text.Callback;

        UiContext context = new() { ViewportSize = new float2(800f, 600f) };
        context.AddRoot(root);
        context.Update(0f);
        LayoutFixture.AssertNoLayoutBits(root);
        Assert.True(text.Count > 0, "The first pass measures the leaf.");
        text.Reset();

        current.MarkDirty(DirtyFlags.Layout);
        context.Update(0f);
        LayoutFixture.AssertNoLayoutBits(root);

        int calls = text.Count;
        Assert.True(calls > 0, "The mark measures the leaf again.");
        Assert.True(calls <= CallBudget, $"A leaf mark gave {calls} callback calls. The budget is {CallBudget}.");
        LayoutDifferentialTests.AssertMatchesFreshLayout(root, context.ViewportSize, "after the leaf mark");
    }

    /// <summary>Gives the sum of the call counts of the stubs.</summary>
    /// <param name="stubs">The stubs.</param>
    /// <returns>The sum.</returns>
    private static int SumOfCounts(MeasureStub[] stubs)
    {
        int sum = 0;
        foreach (MeasureStub stub in stubs)
        {
            sum += stub.Count;
        }

        return sum;
    }

    /// <summary>Creates a named child with no callback and adds it at the end of the children of <paramref name="parent"/>.</summary>
    /// <param name="parent">The parent element.</param>
    /// <param name="name">The name of the child.</param>
    /// <returns>The new child.</returns>
    private static Element AddChild(Element parent, string name)
    {
        Element child = new() { Name = name };
        parent.Children.Add(child);
        return child;
    }

    /// <summary>Creates a named leaf with the callback of a stub and adds it at the end of the children of <paramref name="parent"/>.</summary>
    /// <param name="parent">The parent element.</param>
    /// <param name="name">The name of the leaf.</param>
    /// <param name="stub">The counting stub of the leaf.</param>
    /// <returns>The new leaf.</returns>
    private static Element AddLeaf(Element parent, string name, MeasureStub stub)
    {
        Element leaf = AddChild(parent, name);
        leaf.Layout.Measure = stub.Callback;
        return leaf;
    }
}
