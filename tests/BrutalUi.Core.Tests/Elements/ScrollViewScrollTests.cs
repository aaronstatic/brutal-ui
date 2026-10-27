using System;
using Brutal.Numerics;
using Xunit;

namespace BrutalUi.Core.Tests;

/// <summary>
/// Tests of the wheel scroll of <see cref="ScrollView"/>: a scroll over the view or a child moves the offset, calls
/// <see cref="ScrollView.ScrollChanged"/>, and moves the draw of the children inside the clip of the view.
/// </summary>
/// <remarks>
/// The scene is <see cref="ScrollViewTree"/>: the view is 200 x 100 at (0, 0), the content is 300 x 180, and the maximum
/// offset is (100, 80). The default speed is 40, so a delta of 1 moves the offset by 40 pixels. The tests use the type id
/// <c>ScrollView</c>, which the view interns. The tests intern no class.
/// </remarks>
public sealed class ScrollViewScrollTests
{
    /// <summary>The fill of the type rule.</summary>
    private static readonly Rgba8 ViewFill = new(12, 34, 56, 255);

    /// <summary>
    /// The demo of phase 1. A scroll with the delta (0, -1) over <see cref="ScrollViewTree.Row0"/> gives the offset
    /// (0, 40) and one call with the view as the sender. The draw puts <see cref="ScrollViewTree.Row0"/> at
    /// (0, -40, 300, 40), between the clip (0, 0, 200, 100) of the view and its pop.
    /// </summary>
    [Fact]
    public void AScrollMovesTheContent()
    {
        ScrollViewTree tree = new();
        tree.Settle();
        int calls = 0;
        ScrollView? sender = null;
        float2 offsetInHandler = default;
        tree.View.ScrollChanged = view =>
        {
            calls++;
            sender = view;
            offsetInHandler = view.ScrollOffset;
        };

        tree.Context.PushEvent(PointerTree.Scroll(ScrollViewTree.OverRow0, new float2(0f, -1f)));
        RecordingRenderer renderer = Draw(tree);
        TestContext.Current.TestOutputHelper?.WriteLine(renderer.Describe());

        Assert.Equal(new float2(0f, 40f), tree.View.ScrollOffset);
        Assert.Equal(1, calls);
        Assert.Same(tree.View, sender);

        // The delegate runs after the change: it reads the new offset.
        Assert.Equal(new float2(0f, 40f), offsetInHandler);

        ReadOnlySpan<DrawCall> drawn = renderer.Calls;
        int push = IndexOfClip(renderer, new float4(0f, 0f, 200f, 100f));
        int pop = IndexOfPop(renderer, push);
        int row = IndexOfFill(renderer, ScrollViewTree.RowFill(0));
        Assert.True(push >= 0);
        Assert.True(push < row && row < pop);
        Assert.Equal(new float4(0f, -40f, 300f, 40f), drawn[row].Rect);

        // The other rows move by the same offset.
        Assert.Equal(new float4(0f, 20f, 300f, 40f), drawn[IndexOfFill(renderer, ScrollViewTree.RowFill(2))].Rect);
        Assert.Equal(new float4(0f, 100f, 300f, 40f), drawn[IndexOfFill(renderer, ScrollViewTree.RowFill(4))].Rect);
        Assert.Equal(0, renderer.ClipDepth);
    }

    /// <summary>A scroll down and then a scroll up give the offset (0, 0) and two calls.</summary>
    [Fact]
    public void AScrollUpMovesTheContentBack()
    {
        ScrollViewTree tree = new();
        tree.Settle();
        int calls = 0;
        tree.View.ScrollChanged = _ => calls++;

        tree.Context.PushEvent(PointerTree.Scroll(ScrollViewTree.OverRow0, new float2(0f, -1f)));
        Assert.Equal(1, tree.Context.DrainEvents());
        Assert.Equal(new float2(0f, 40f), tree.View.ScrollOffset);

        tree.Context.PushEvent(PointerTree.Scroll(ScrollViewTree.OverRow0, new float2(0f, 1f)));
        RecordingRenderer renderer = Draw(tree);

        Assert.Equal(new float2(0f, 0f), tree.View.ScrollOffset);
        Assert.Equal(2, calls);
        Assert.Equal(new float4(0f, 0f, 300f, 40f), renderer.Calls[IndexOfFill(renderer, ScrollViewTree.RowFill(0))].Rect);
    }

    /// <summary>The delta (-1, 0) gives the offset (40, 0), and <see cref="ScrollViewTree.Row0"/> draws at (-40, 0).</summary>
    [Fact]
    public void AHorizontalScrollMovesTheX()
    {
        ScrollViewTree tree = new();
        tree.Settle();
        int calls = 0;
        tree.View.ScrollChanged = _ => calls++;

        tree.Context.PushEvent(PointerTree.Scroll(ScrollViewTree.OverRow0, new float2(-1f, 0f)));
        RecordingRenderer renderer = Draw(tree);

        Assert.Equal(new float2(40f, 0f), tree.View.ScrollOffset);
        Assert.Equal(1, calls);
        Assert.Equal(new float4(-40f, 0f, 300f, 40f), renderer.Calls[IndexOfFill(renderer, ScrollViewTree.RowFill(0))].Rect);
    }

    /// <summary>
    /// Three scrolls of (0, -1) in one drain give the offset (0, 80) and two calls. The third scroll does not move the
    /// offset, so it walks on, and the bubble handler of the root gets the third scroll only.
    /// </summary>
    [Fact]
    public void TheScrollStopsAtTheEnd()
    {
        ScrollViewTree tree = new();
        tree.Settle();
        int calls = 0;
        tree.View.ScrollChanged = _ => calls++;
        int rootCalls = 0;
        float2 offsetAtRoot = new(-1f, -1f);
        ScrollView view = tree.View;
        tree.Root.OnScroll += (ref ScrollEvent e) =>
        {
            rootCalls++;
            offsetAtRoot = view.ScrollOffset;
        };

        for (int i = 0; i < 3; i++)
        {
            tree.Context.PushEvent(PointerTree.Scroll(ScrollViewTree.OverRow0, new float2(0f, -1f)));
        }

        Assert.Equal(3, tree.Context.DrainEvents());

        Assert.Equal(new float2(0f, 80f), tree.View.ScrollOffset);
        Assert.Equal(2, calls);
        Assert.Equal(1, rootCalls);
        Assert.Equal(new float2(0f, 80f), offsetAtRoot);
    }

    /// <summary>
    /// The delta (0, 1) at the offset 0 changes nothing and calls nothing. The scroll walks on to the bubble handler of
    /// the root.
    /// </summary>
    [Fact]
    public void AScrollAtTheStartWalksOn()
    {
        ScrollViewTree tree = new();
        tree.Settle();
        int calls = 0;
        tree.View.ScrollChanged = _ => calls++;
        int rootCalls = 0;
        tree.Root.OnScroll += (ref ScrollEvent e) => rootCalls++;

        tree.Context.PushEvent(PointerTree.Scroll(ScrollViewTree.OverRow0, new float2(0f, 1f)));
        Assert.Equal(1, tree.Context.DrainEvents());

        Assert.Equal(new float2(0f, 0f), tree.View.ScrollOffset);
        Assert.Equal(0, calls);
        Assert.Equal(1, rootCalls);
    }

    /// <summary>
    /// A scroll that moves the offset stops the walk: the capture handler of the root runs, and the bubble handler of
    /// the root does not.
    /// </summary>
    [Fact]
    public void AScrollThatMovesStopsTheWalk()
    {
        ScrollViewTree tree = new();
        tree.Settle();
        int captureCalls = 0;
        int bubbleCalls = 0;
        tree.Root.OnScrollCapture += (ref ScrollEvent e) => captureCalls++;
        tree.Root.OnScroll += (ref ScrollEvent e) => bubbleCalls++;

        tree.Context.PushEvent(PointerTree.Scroll(ScrollViewTree.OverRow0, new float2(0f, -1f)));
        Assert.Equal(1, tree.Context.DrainEvents());

        Assert.Equal(new float2(0f, 40f), tree.View.ScrollOffset);
        Assert.Equal(1, captureCalls);
        Assert.Equal(0, bubbleCalls);
    }

    /// <summary>
    /// The speed 10 and the delta (0, -2.5) give the offset (0, 25). A create-info speed of 0 gives the speed 40.
    /// </summary>
    [Fact]
    public void TheSpeedScalesTheDelta()
    {
        ScrollViewTree tree = new(new ScrollView.CreateInfo { ScrollSpeed = 10f });
        tree.Settle();
        Assert.Equal(10f, tree.View.ScrollSpeed);

        tree.Context.PushEvent(PointerTree.Scroll(ScrollViewTree.OverRow0, new float2(0f, -2.5f)));
        Assert.Equal(1, tree.Context.DrainEvents());
        Assert.Equal(new float2(0f, 25f), tree.View.ScrollOffset);

        Assert.Equal(40f, ScrollView.DefaultScrollSpeed);
        Assert.Equal(40f, new ScrollViewTree(new ScrollView.CreateInfo { ScrollSpeed = 0f }).View.ScrollSpeed);
        Assert.Equal(40f, new ScrollViewTree().View.ScrollSpeed);
        Assert.Equal(40f, new ScrollView(new ScrollView.CreateInfo { ScrollSpeed = -0f }).ScrollSpeed);
    }

    /// <summary>
    /// A set from code clamps and calls nothing. Before the first update, a set gives (0, 0). The set (500, 500) gives
    /// (100, 80) and marks <see cref="DirtyFlags.Paint"/>. The set (-5, 10) gives (0, 10). The same offset marks nothing
    /// after a settle.
    /// </summary>
    [Fact]
    public void ACodeSetClampsAndCallsNothing()
    {
        ScrollViewTree tree = new();
        int calls = 0;
        tree.View.ScrollChanged = _ => calls++;

        // No layout yet, so the maximum offset is (0, 0).
        tree.View.ScrollOffset = new float2(500f, 500f);
        Assert.Equal(new float2(0f, 0f), tree.View.ScrollOffset);

        tree.Settle();
        Assert.Equal(DirtyFlags.None, tree.View.Dirty);
        Assert.Equal(new float2(100f, 80f), tree.View.MaxOffset());

        tree.View.ScrollOffset = new float2(500f, 500f);
        Assert.Equal(new float2(100f, 80f), tree.View.ScrollOffset);
        Assert.Equal(DirtyFlags.Paint, tree.View.Dirty & DirtyFlags.Paint);

        tree.View.ScrollOffset = new float2(-5f, 10f);
        Assert.Equal(new float2(0f, 10f), tree.View.ScrollOffset);

        tree.Settle();
        Assert.Equal(DirtyFlags.None, tree.View.Dirty);
        tree.View.ScrollOffset = new float2(0f, 10f);
        Assert.Equal(new float2(0f, 10f), tree.View.ScrollOffset);
        Assert.Equal(DirtyFlags.None, tree.View.Dirty);

        Assert.Equal(0, calls);
    }

    /// <summary>
    /// The type id is <c>ScrollView</c>, the overflow is <see cref="Overflow.Scroll"/>, and the view is not focusable. A
    /// speed of -1, NaN, or infinity throws with the parameter name <c>info</c>.
    /// </summary>
    [Fact]
    public void TheCreateInfoTheTypeAndTheOverflow()
    {
        ScrollViewTree tree = new();
        tree.Settle();

        Assert.Equal(ElementType.Intern("ScrollView"), tree.View.TypeId);
        Assert.Equal(Overflow.Scroll, tree.View.Inline.Overflow);
        Assert.Equal(Overflow.Scroll, tree.View.Style.Overflow);
        Assert.False(tree.View.IsFocusable);
        Assert.Equal(new float4(0f, 0f, 200f, 100f), tree.View.Layout.Rect);

        float[] bad = { -1f, float.NaN, float.PositiveInfinity, float.NegativeInfinity };
        for (int i = 0; i < bad.Length; i++)
        {
            float speed = bad[i];
            ArgumentOutOfRangeException ex = Assert.Throws<ArgumentOutOfRangeException>(
                () => new ScrollView(new ScrollView.CreateInfo { ScrollSpeed = speed }));
            Assert.Equal("info", ex.ParamName);
            Assert.Contains("CreateInfo.ScrollSpeed", ex.Message, StringComparison.Ordinal);
        }
    }

    /// <summary>
    /// A rule with the type <c>ScrollView</c> sets a fill on the view and not on <see cref="ScrollViewTree.Outside"/>. The
    /// same rule sets <c>overflow: visible</c>, and the inline value of the view wins, so the clip stays.
    /// </summary>
    [Fact]
    public void ATypeRuleMatchesTheScrollView()
    {
        ScrollViewTree tree = new();
        StyleSheet sheet = new StyleSheet.Builder()
            .Add(
                Selector.For(new CompoundSelector { Type = ElementType.Intern("ScrollView") }),
                new StyleProperties { BackgroundColor = ViewFill, Overflow = Overflow.Visible })
            .Build();
        tree.Context.AddSheet(sheet);

        RecordingRenderer renderer = Draw(tree);

        Assert.Equal(ViewFill, tree.View.Style.BackgroundColor);
        Assert.NotEqual(ViewFill, tree.Outside.Style.BackgroundColor);
        Assert.NotEqual(ViewFill, tree.Root.Style.BackgroundColor);
        Assert.Equal(Overflow.Scroll, tree.View.Style.Overflow);

        int fill = IndexOfFill(renderer, ViewFill);
        int push = IndexOfClip(renderer, new float4(0f, 0f, 200f, 100f));
        Assert.Equal(new float4(0f, 0f, 200f, 100f), renderer.Calls[fill].Rect);
        Assert.True(fill < push);
        Assert.True(push < IndexOfFill(renderer, ScrollViewTree.RowFill(0)));
    }

    /// <summary>Runs one update and one draw of the tree.</summary>
    /// <param name="tree">The tree.</param>
    /// <returns>The renderer with the calls of the frame.</returns>
    private static RecordingRenderer Draw(ScrollViewTree tree)
    {
        RecordingRenderer renderer = new();
        tree.Context.Update(0f);
        tree.Context.Draw(renderer);
        return renderer;
    }

    /// <summary>Gives the index of the only rect call with a fill.</summary>
    /// <param name="renderer">The renderer.</param>
    /// <param name="color">The fill.</param>
    /// <returns>The index among all calls.</returns>
    private static int IndexOfFill(RecordingRenderer renderer, Rgba8 color)
    {
        int found = -1;
        ReadOnlySpan<DrawCall> calls = renderer.Calls;
        for (int i = 0; i < calls.Length; i++)
        {
            if (calls[i].Kind == DrawCallKind.Rect && calls[i].Color.Equals(color))
            {
                Assert.Equal(-1, found);
                found = i;
            }
        }

        Assert.NotEqual(-1, found);
        return found;
    }

    /// <summary>Gives the index of the only clip push with a rect.</summary>
    /// <param name="renderer">The renderer.</param>
    /// <param name="rect">The clip rect.</param>
    /// <returns>The index among all calls.</returns>
    private static int IndexOfClip(RecordingRenderer renderer, float4 rect)
    {
        int found = -1;
        ReadOnlySpan<DrawCall> calls = renderer.Calls;
        for (int i = 0; i < calls.Length; i++)
        {
            if (calls[i].Kind == DrawCallKind.PushClip && calls[i].Rect.Equals(rect))
            {
                Assert.Equal(-1, found);
                found = i;
            }
        }

        Assert.NotEqual(-1, found);
        return found;
    }

    /// <summary>Gives the index of the pop that matches a clip push.</summary>
    /// <param name="renderer">The renderer.</param>
    /// <param name="push">The index of the push.</param>
    /// <returns>The index among all calls.</returns>
    private static int IndexOfPop(RecordingRenderer renderer, int push)
    {
        ReadOnlySpan<DrawCall> calls = renderer.Calls;
        int depth = 0;
        for (int i = push; i < calls.Length; i++)
        {
            if (calls[i].Kind == DrawCallKind.PushClip)
            {
                depth++;
            }
            else if (calls[i].Kind == DrawCallKind.PopClip)
            {
                depth--;
                if (depth == 0)
                {
                    return i;
                }
            }
        }

        Assert.Fail("The clip push has no pop.");
        return -1;
    }
}
