using System;
using Brutal.Numerics;
using Xunit;

namespace BrutalUi.Core.Tests;

/// <summary>
/// Tests of the clamp at use of <see cref="ScrollView"/>: the shown offset always fits the content of the last layout.
/// </summary>
/// <remarks>
/// The scene is <see cref="ScrollViewTree"/>: the view is 200 x 100 at (0, 0), the content is 300 x 180, and the maximum
/// offset is (100, 80). The default speed is 40. A test that needs another scene changes the inline style of the view
/// before the first settle.
/// </remarks>
public sealed class ScrollViewClampTests
{
    /// <summary>
    /// The demo of phase 4. The offset (0, 80), a hidden <see cref="ScrollViewTree.Row4"/>, one update, and one draw give
    /// the shown offset (0, 40), <see cref="ScrollViewTree.Row0"/> at (0, -40, 300, 40), and no
    /// <see cref="ScrollView.ScrollChanged"/> call.
    /// </summary>
    [Fact]
    public void AContentShrinkShowsTheEndAtOnce()
    {
        ScrollViewTree tree = new();
        tree.Settle();
        int calls = 0;
        tree.View.ScrollChanged = _ => calls++;

        tree.View.ScrollOffset = new float2(0f, 80f);
        Assert.Equal(new float2(0f, 80f), tree.View.ScrollOffset);
        tree.Row4.Inline.Display = Display.None;
        RecordingRenderer renderer = Draw(tree);
        TestContext.Current.TestOutputHelper?.WriteLine(renderer.Describe());

        Assert.Equal(new float2(0f, 40f), tree.View.ScrollOffset);
        Assert.Equal(new float2(100f, 40f), tree.View.MaxOffset());
        Assert.Equal(0, calls);
        ReadOnlySpan<DrawCall> drawn = renderer.Calls;
        Assert.Equal(new float4(0f, -40f, 300f, 40f), drawn[IndexOfFill(renderer, ScrollViewTree.RowFill(0))].Rect);
        Assert.Equal(new float4(0f, 20f, 300f, 40f), drawn[IndexOfFill(renderer, ScrollViewTree.RowFill(2))].Rect);
        Assert.Equal(new float4(0f, 60f, 300f, 40f), drawn[IndexOfFill(renderer, ScrollViewTree.RowFill(3))].Rect);

        // A set of the shown offset (the stored offset is still (0, 80)) changes nothing, so it marks no paint.
        tree.View.ScrollOffset = new float2(0f, 40f);
        Assert.Equal(DirtyFlags.None, tree.View.Dirty & DirtyFlags.Paint);
        Assert.Equal(new float2(0f, 40f), tree.View.ScrollOffset);
        Assert.Equal(0, calls);
    }

    /// <summary>
    /// After the shrink of the demo, a wheel and a drag move at the end change nothing and store nothing. A show of
    /// <see cref="ScrollViewTree.Row4"/> then brings the offset (0, 80) back, with no call. The get did not write the
    /// clamp. A set from code of the shown offset after a second shrink stores it, so the next growth keeps (0, 40).
    /// </summary>
    [Fact]
    public void TheOffsetComesBackWhenTheContentGrows()
    {
        ScrollViewTree tree = new();
        tree.Settle();
        int calls = 0;
        tree.View.ScrollChanged = _ => calls++;
        tree.View.ScrollOffset = new float2(0f, 80f);
        tree.Row4.Inline.Display = Display.None;
        Draw(tree);
        Assert.Equal(new float2(0f, 40f), tree.View.ScrollOffset);

        // A wheel at the end and a drag move at the end change nothing, so they store nothing.
        tree.Context.PushEvent(PointerTree.Scroll(ScrollViewTree.OverRow0, new float2(0f, -1f)));
        Assert.Equal(1, tree.Context.DrainEvents());
        Assert.Same(tree.View, tree.Context.HitTest(new float2(100f, 80f)));
        tree.Context.PushEvent(PointerTree.Down(new float2(100f, 80f), PointerButton.Left));
        tree.Context.PushEvent(PointerTree.Move(new float2(100f, 70f)));
        tree.Context.PushEvent(PointerTree.Move(new float2(100f, 60f)));
        tree.Context.PushEvent(PointerTree.Up(new float2(100f, 60f), PointerButton.Left));
        tree.Context.Update(0f);
        Assert.Equal(new float2(0f, 40f), tree.View.ScrollOffset);
        Assert.Equal(0, calls);

        tree.Row4.Inline.Display = Display.Flex;
        RecordingRenderer renderer = Draw(tree);

        Assert.Equal(new float2(0f, 80f), tree.View.ScrollOffset);
        Assert.Equal(0, calls);
        Assert.Equal(new float4(0f, -80f, 300f, 40f), renderer.Calls[IndexOfFill(renderer, ScrollViewTree.RowFill(0))].Rect);

        // A set from code of the shown offset stores it, so a growth does not bring the old offset back.
        tree.Row4.Inline.Display = Display.None;
        Draw(tree);
        Assert.Equal(new float2(0f, 40f), tree.View.ScrollOffset);
        tree.View.ScrollOffset = new float2(0f, 40f);
        tree.Row4.Inline.Display = Display.Flex;
        Draw(tree);

        Assert.Equal(new float2(0f, 40f), tree.View.ScrollOffset);
        Assert.Equal(0, calls);
    }

    /// <summary>
    /// After the shrink, a scroll of (0, 1) starts from the shown offset (0, 40) and gives (0, 0) with one call. A show
    /// of <see cref="ScrollViewTree.Row4"/> then gives (0, 0), because the user input stored the clamped result.
    /// </summary>
    [Fact]
    public void AUserScrollAfterAShrinkStartsFromTheShownOffset()
    {
        ScrollViewTree tree = new();
        tree.Settle();
        int calls = 0;
        tree.View.ScrollChanged = _ => calls++;
        tree.View.ScrollOffset = new float2(0f, 80f);
        tree.Row4.Inline.Display = Display.None;
        Draw(tree);
        Assert.Equal(new float2(0f, 40f), tree.View.ScrollOffset);

        tree.Context.PushEvent(PointerTree.Scroll(ScrollViewTree.OverRow0, new float2(0f, 1f)));
        Assert.Equal(1, tree.Context.DrainEvents());

        Assert.Equal(new float2(0f, 0f), tree.View.ScrollOffset);
        Assert.Equal(1, calls);

        tree.Row4.Inline.Display = Display.Flex;
        Draw(tree);

        Assert.Equal(new float2(0f, 0f), tree.View.ScrollOffset);
        Assert.Equal(1, calls);
    }

    /// <summary>
    /// A set of (50, 50) in a tree with no update gives (0, 0), because the layout is empty. The value stays (0, 0) after
    /// a settle, because the set stored the clamped value.
    /// </summary>
    [Fact]
    public void ASetBeforeTheFirstUpdateGivesZero()
    {
        ScrollViewTree tree = new();
        int calls = 0;
        tree.View.ScrollChanged = _ => calls++;

        tree.View.ScrollOffset = new float2(50f, 50f);
        Assert.Equal(new float2(0f, 0f), tree.View.ScrollOffset);

        tree.Settle();
        Assert.Equal(new float2(0f, 0f), tree.View.ScrollOffset);
        Assert.Equal(new float2(100f, 80f), tree.View.MaxOffset());
        Assert.Equal(0, calls);
    }

    /// <summary>
    /// A NaN component gives 0 on its axis: (NaN, 50) gives (0, 50). Positive infinity gives the maximum and negative
    /// infinity gives 0: (+infinity, -infinity) gives (100, 0).
    /// </summary>
    [Fact]
    public void ANaNOrInfiniteSetFollowsTheRule()
    {
        ScrollViewTree tree = new();
        tree.Settle();

        tree.View.ScrollOffset = new float2(float.NaN, 50f);
        Assert.Equal(new float2(0f, 50f), tree.View.ScrollOffset);

        tree.View.ScrollOffset = new float2(float.PositiveInfinity, float.NegativeInfinity);
        Assert.Equal(new float2(100f, 0f), tree.View.ScrollOffset);

        tree.View.ScrollOffset = new float2(30f, float.NaN);
        Assert.Equal(new float2(30f, 0f), tree.View.ScrollOffset);

        tree.View.ScrollOffset = new float2(float.NegativeInfinity, float.PositiveInfinity);
        Assert.Equal(new float2(0f, 80f), tree.View.ScrollOffset);
    }

    /// <summary>
    /// A NaN delta component counts as 0: (NaN, -1) gives (0, 40) with one call. The delta (+infinity, 0) changes nothing,
    /// calls nothing, and walks on to the bubble handler of the root.
    /// </summary>
    [Fact]
    public void ANonFiniteDeltaCountsAsZero()
    {
        ScrollViewTree tree = new();
        tree.Settle();
        int calls = 0;
        tree.View.ScrollChanged = _ => calls++;
        int rootCalls = 0;
        tree.Root.OnScroll += (ref ScrollEvent e) => rootCalls++;

        tree.Context.PushEvent(PointerTree.Scroll(ScrollViewTree.OverRow0, new float2(float.NaN, -1f)));
        Assert.Equal(1, tree.Context.DrainEvents());

        Assert.Equal(new float2(0f, 40f), tree.View.ScrollOffset);
        Assert.Equal(1, calls);
        Assert.Equal(0, rootCalls);

        tree.Context.PushEvent(PointerTree.Scroll(ScrollViewTree.OverRow0, new float2(float.PositiveInfinity, 0f)));
        Assert.Equal(1, tree.Context.DrainEvents());

        Assert.Equal(new float2(0f, 40f), tree.View.ScrollOffset);
        Assert.Equal(1, calls);
        Assert.Equal(1, rootCalls);

        // At an x offset above 0, a non-finite x delta must not move the x offset to 0.
        tree.View.ScrollOffset = new float2(40f, 40f);
        Assert.Equal(new float2(40f, 40f), tree.View.ScrollOffset);

        tree.Context.PushEvent(PointerTree.Scroll(ScrollViewTree.OverRow0, new float2(float.NaN, -1f)));
        Assert.Equal(1, tree.Context.DrainEvents());

        Assert.Equal(new float2(40f, 80f), tree.View.ScrollOffset);
        Assert.Equal(2, calls);
        Assert.Equal(1, rootCalls);

        tree.Context.PushEvent(PointerTree.Scroll(ScrollViewTree.OverRow0, new float2(float.PositiveInfinity, 0f)));
        Assert.Equal(1, tree.Context.DrainEvents());

        Assert.Equal(new float2(40f, 80f), tree.View.ScrollOffset);
        Assert.Equal(2, calls);
        Assert.Equal(2, rootCalls);
    }

    /// <summary>
    /// A padding of 10 and a border of 2 on the view give the maximum offset (124, 104) and the clip (2, 2, 196, 96).
    /// <see cref="ScrollViewTree.Row0"/> draws at (12 - ox, 12 - oy).
    /// </summary>
    [Fact]
    public void ThePaddingAndTheBorderCount()
    {
        ScrollViewTree tree = new();
        tree.View.Inline.Padding = LengthEdges.All(10);
        tree.View.Inline.BorderWidth = LengthEdges.All(2);
        tree.Settle();

        Assert.Equal(new float2(124f, 104f), tree.View.MaxOffset());

        tree.View.ScrollOffset = new float2(50f, 30f);
        RecordingRenderer renderer = Draw(tree);

        Assert.Equal(new float2(50f, 30f), tree.View.ScrollOffset);
        int push = IndexOfClip(renderer, new float4(2f, 2f, 196f, 96f));
        int row = IndexOfFill(renderer, ScrollViewTree.RowFill(0));
        Assert.True(push < row);
        Assert.Equal(new float4(12f - 50f, 12f - 30f, 300f, 40f), renderer.Calls[row].Rect);

        tree.View.ScrollOffset = new float2(1000f, 1000f);
        Assert.Equal(new float2(124f, 104f), tree.View.ScrollOffset);

        // Unequal sides: the maximum x adds the trailing inset (0 + 2), not the leading inset (30 + 2).
        // The far edge is 2 + 30 + 300 = 332, so the maximum x is 332 + 2 - 200 = 134.
        tree.View.Inline.Padding = new LengthEdges(30, 10, 0, 10);
        tree.Settle();

        Assert.Equal(134f, tree.View.MaxOffset().X);
    }

    /// <summary>
    /// An absolute child at the left 500 with the width 50 gives the maximum x 350. A hide of that child gives 100 again.
    /// </summary>
    [Fact]
    public void AnAbsoluteChildCountsAndAHiddenChildDoesNot()
    {
        ScrollViewTree tree = new();
        Element floating = new() { Name = "floating" };
        floating.Inline.Position = PositionType.Absolute;
        floating.Inline.Left = 500;
        floating.Inline.Top = 0;
        floating.Inline.Width = 50;
        floating.Inline.Height = 10;
        tree.View.Children.Add(floating);
        tree.Settle();

        Assert.Equal(new float2(350f, 80f), tree.View.MaxOffset());
        tree.View.ScrollOffset = new float2(1000f, 0f);
        Assert.Equal(new float2(350f, 0f), tree.View.ScrollOffset);

        floating.Inline.Display = Display.None;
        tree.Settle();

        Assert.Equal(new float2(100f, 80f), tree.View.MaxOffset());
        Assert.Equal(new float2(100f, 0f), tree.View.ScrollOffset);
    }

    /// <summary>
    /// A view of 400 x 300 is larger than the content of 300 x 180. The maximum offset is (0, 0), and a scroll walks on
    /// to the bubble handler of the root with no call.
    /// </summary>
    [Fact]
    public void AViewLargerThanTheContentDoesNotScroll()
    {
        ScrollViewTree tree = new();
        tree.View.Inline.Width = 400;
        tree.View.Inline.Height = 300;
        tree.Settle();
        int calls = 0;
        tree.View.ScrollChanged = _ => calls++;
        int rootCalls = 0;
        tree.Root.OnScroll += (ref ScrollEvent e) => rootCalls++;

        Assert.Equal(new float2(0f, 0f), tree.View.MaxOffset());

        tree.Context.PushEvent(PointerTree.Scroll(ScrollViewTree.OverRow0, new float2(-1f, -1f)));
        Assert.Equal(1, tree.Context.DrainEvents());

        Assert.Equal(new float2(0f, 0f), tree.View.ScrollOffset);
        Assert.Equal(0, calls);
        Assert.Equal(1, rootCalls);
    }

    /// <summary>The offset (0, 80) and a view height of 150 give the shown offset (0, 30), with no call.</summary>
    [Fact]
    public void AViewResizeClampsTheShownOffset()
    {
        ScrollViewTree tree = new();
        tree.Settle();
        int calls = 0;
        tree.View.ScrollChanged = _ => calls++;
        tree.View.ScrollOffset = new float2(0f, 80f);
        Assert.Equal(new float2(0f, 80f), tree.View.ScrollOffset);

        tree.View.Inline.Height = 150;
        Draw(tree);

        Assert.Equal(new float2(100f, 30f), tree.View.MaxOffset());
        Assert.Equal(new float2(0f, 30f), tree.View.ScrollOffset);
        Assert.Equal(0, calls);
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
}
