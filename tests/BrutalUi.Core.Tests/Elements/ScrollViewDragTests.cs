using Brutal.Numerics;
using Xunit;

namespace BrutalUi.Core.Tests;

/// <summary>
/// Tests of the drag of <see cref="ScrollView"/>: a left press on the view itself and a move drag the content with the
/// pointer, and the capture keeps the drag outside the view.
/// </summary>
/// <remarks>
/// The scene is <see cref="ScrollViewTree"/>: the view is 200 x 100 at (0, 0), the content is 300 x 180, and the maximum
/// offset is (100, 80). A press at <see cref="ScrollViewTree.OverGap"/> (160, 50) hits the view itself at the offset 0.
/// The tests intern no class.
/// </remarks>
public sealed class ScrollViewDragTests
{
    /// <summary>
    /// The demo of phase 3. A left press at (160, 50) and a move to (160, 20) give the offset (0, 30) and one
    /// <see cref="ScrollView.ScrollChanged"/> call. The draw puts <see cref="ScrollViewTree.Row0"/> at (0, -30, 300, 40).
    /// </summary>
    [Fact]
    public void ADragMovesTheContentWithThePointer()
    {
        ScrollViewTree tree = new();
        tree.Settle();
        int calls = 0;
        tree.View.ScrollChanged = _ => calls++;

        tree.Context.PushEvent(PointerTree.Down(ScrollViewTree.OverGap, PointerButton.Left));
        tree.Context.PushEvent(PointerTree.Move(new float2(160f, 20f)));
        tree.Context.Update(0f);
        RecordingRenderer renderer = new();
        tree.Context.Draw(renderer);

        TestContext.Current.TestOutputHelper?.WriteLine(
            "Demo: press at (160, 50), move to (160, 20), offset = (" + tree.View.ScrollOffset.X + ", "
            + tree.View.ScrollOffset.Y + "), calls = " + calls);

        Assert.Equal(new float2(0f, 30f), tree.View.ScrollOffset);
        Assert.Equal(1, calls);
        Assert.Equal(new float4(0f, -30f, 300f, 40f), RectOf(renderer, ScrollViewTree.RowFill(0)));
    }

    /// <summary>
    /// A press at <see cref="ScrollViewTree.OverGap"/> and a move to (300, 10) over the root give the offset (0, 40). The
    /// capture keeps the drag on the view. <see cref="PseudoStates.Hover"/> and <see cref="UiContext.PointerHit"/> are on
    /// the root.
    /// </summary>
    [Fact]
    public void ADragOutsideTheViewKeepsDragging()
    {
        ScrollViewTree tree = new();
        tree.Settle();

        tree.Context.PushEvent(PointerTree.Down(ScrollViewTree.OverGap, PointerButton.Left));
        tree.Context.PushEvent(PointerTree.Move(new float2(300f, 10f)));
        tree.Context.Update(0f);

        Assert.Equal(new float2(0f, 40f), tree.View.ScrollOffset);
        Assert.Same(tree.Root, tree.Context.PointerHit);
        Assert.NotEqual(PseudoStates.None, tree.Root.States & PseudoStates.Hover);
        Assert.Equal(PseudoStates.None, tree.View.States & PseudoStates.Hover);
    }

    /// <summary>
    /// A move to (160, -100) gives the offset (0, 80), the maximum. The view stores the pointer, so a move to (160, -90)
    /// gives (0, 70) at once. The next move does not wait for the pointer to come back to the end.
    /// </summary>
    [Fact]
    public void ADragPastTheEndComesBackAtOnce()
    {
        ScrollViewTree tree = new();
        tree.Settle();
        int calls = 0;
        tree.View.ScrollChanged = _ => calls++;

        tree.Context.PushEvent(PointerTree.Down(ScrollViewTree.OverGap, PointerButton.Left));
        tree.Context.PushEvent(PointerTree.Move(new float2(160f, -100f)));
        tree.Context.Update(0f);
        Assert.Equal(new float2(0f, 80f), tree.View.ScrollOffset);
        Assert.Equal(1, calls);

        tree.Context.PushEvent(PointerTree.Move(new float2(160f, -90f)));
        tree.Context.Update(0f);
        Assert.Equal(new float2(0f, 70f), tree.View.ScrollOffset);
        Assert.Equal(2, calls);
    }

    /// <summary>
    /// At the end of the content, one more move changes nothing and calls nothing. The move still stops the walk, so the
    /// bubble handler of the root gets no move. The press that starts the drag stops the walk too, so the bubble handler
    /// of the root gets no press.
    /// </summary>
    [Fact]
    public void ADragMoveStopsTheWalkAlsoWithNoChange()
    {
        ScrollViewTree tree = new();
        tree.Settle();
        int calls = 0;
        tree.View.ScrollChanged = _ => calls++;
        int rootMoves = 0;
        tree.Root.OnPointerMove += (ref PointerEvent e) => rootMoves++;
        int rootDowns = 0;
        tree.Root.OnPointerDown += (ref PointerEvent e) => rootDowns++;

        tree.Context.PushEvent(PointerTree.Down(ScrollViewTree.OverGap, PointerButton.Left));
        tree.Context.PushEvent(PointerTree.Move(new float2(160f, -100f)));
        tree.Context.Update(0f);
        Assert.Equal(new float2(0f, 80f), tree.View.ScrollOffset);
        Assert.Equal(1, calls);
        Assert.Equal(0, rootDowns);

        tree.Context.PushEvent(PointerTree.Move(new float2(160f, -110f)));
        tree.Context.Update(0f);

        Assert.Equal(new float2(0f, 80f), tree.View.ScrollOffset);
        Assert.Equal(1, calls);
        Assert.Equal(0, rootMoves);
    }

    /// <summary>
    /// A press at <see cref="ScrollViewTree.OverRow0"/>, a pickable child, gives no drag. A move to (100, 0) changes
    /// nothing and calls nothing, and it walks on to the bubble handler of the root.
    /// </summary>
    [Fact]
    public void APressOnAPickableChildGivesNoDrag()
    {
        ScrollViewTree tree = new();
        tree.Settle();
        int calls = 0;
        tree.View.ScrollChanged = _ => calls++;
        int rootMoves = 0;
        tree.Root.OnPointerMove += (ref PointerEvent e) => rootMoves++;

        tree.Context.PushEvent(PointerTree.Down(ScrollViewTree.OverRow0, PointerButton.Left));
        tree.Context.PushEvent(PointerTree.Move(new float2(100f, 0f)));
        tree.Context.Update(0f);

        Assert.Equal(new float2(0f, 0f), tree.View.ScrollOffset);
        Assert.Equal(0, calls);
        Assert.Equal(1, rootMoves);
    }

    /// <summary>
    /// At the offset (0, 80), a press at (100, 50) over <see cref="ScrollViewTree.Row3"/>, a child that is not pickable,
    /// starts a drag. A move to (100, 70) gives the offset (0, 60). After the release, a code set of (0, 80) and the hide
    /// of <see cref="ScrollViewTree.Row4"/> make the maximum offset (0, 40), so the shown offset is (0, 40). A press at
    /// (100, 80) over <see cref="ScrollViewTree.Row3"/> and a move to (100, 100) give (0, 20). The drag starts from the
    /// shown offset and not from the stored offset (0, 80).
    /// </summary>
    [Fact]
    public void APressOnANonPickableChildDrags()
    {
        ScrollViewTree tree = new();
        tree.Settle();
        tree.View.ScrollOffset = new float2(0f, 80f);
        Assert.Same(tree.View, tree.Context.HitTest(new float2(100f, 50f)));

        tree.Context.PushEvent(PointerTree.Down(new float2(100f, 50f), PointerButton.Left));
        tree.Context.PushEvent(PointerTree.Move(new float2(100f, 70f)));
        tree.Context.Update(0f);

        Assert.Equal(new float2(0f, 60f), tree.View.ScrollOffset);

        tree.Context.PushEvent(PointerTree.Up(new float2(100f, 70f), PointerButton.Left));
        tree.Context.Update(0f);
        tree.View.ScrollOffset = new float2(0f, 80f);
        tree.Row4.Inline.Display = Display.None;
        tree.Settle();
        Assert.Equal(new float2(0f, 40f), tree.View.ScrollOffset);
        Assert.Same(tree.View, tree.Context.HitTest(new float2(100f, 80f)));

        tree.Context.PushEvent(PointerTree.Down(new float2(100f, 80f), PointerButton.Left));
        tree.Context.PushEvent(PointerTree.Move(new float2(100f, 100f)));
        tree.Context.Update(0f);

        Assert.Equal(new float2(0f, 20f), tree.View.ScrollOffset);
    }

    /// <summary>
    /// A right press at <see cref="ScrollViewTree.OverGap"/> gives no drag. A move to (160, 20) changes nothing, and it
    /// walks on to the bubble handler of the root.
    /// </summary>
    [Fact]
    public void ARightPressGivesNoDrag()
    {
        ScrollViewTree tree = new();
        tree.Settle();
        int calls = 0;
        tree.View.ScrollChanged = _ => calls++;
        int rootMoves = 0;
        tree.Root.OnPointerMove += (ref PointerEvent e) => rootMoves++;

        tree.Context.PushEvent(PointerTree.Down(ScrollViewTree.OverGap, PointerButton.Right));
        tree.Context.PushEvent(PointerTree.Move(new float2(160f, 20f)));
        tree.Context.Update(0f);

        Assert.Equal(new float2(0f, 0f), tree.View.ScrollOffset);
        Assert.Equal(0, calls);
        Assert.Equal(1, rootMoves);
    }

    /// <summary>
    /// A drag moves the content. After the release at <see cref="ScrollViewTree.PastRight"/>, a move over the view changes
    /// nothing, because the capture has ended. A press that a root capture handler stops gives no jump: the first move
    /// after it gives no change and no call, and the next move drags by the pointer difference.
    /// </summary>
    [Fact]
    public void TheCaptureEndsAtTheRelease()
    {
        ScrollViewTree tree = new();
        tree.Settle();
        int calls = 0;
        tree.View.ScrollChanged = _ => calls++;

        tree.Context.PushEvent(PointerTree.Down(ScrollViewTree.OverGap, PointerButton.Left));
        tree.Context.PushEvent(PointerTree.Move(new float2(160f, 30f)));
        tree.Context.Update(0f);
        Assert.Equal(new float2(0f, 20f), tree.View.ScrollOffset);
        Assert.Equal(1, calls);

        tree.Context.PushEvent(PointerTree.Up(ScrollViewTree.PastRight, PointerButton.Left));
        tree.Context.PushEvent(PointerTree.Move(new float2(160f, 10f)));
        tree.Context.Update(0f);

        Assert.Equal(new float2(0f, 20f), tree.View.ScrollOffset);
        Assert.Equal(1, calls);

        // A root capture handler stops the next press at the gap of the view, so the view gets no press and no anchor. The
        // first move gives no jump. The move after it drags by the pointer difference.
        tree.Root.OnPointerDownCapture += (ref PointerEvent e) => e.Header.StopPropagation();
        // At the offset 20, the gap of the view is at (160, 30).
        Assert.Same(tree.View, tree.Context.HitTest(new float2(160f, 30f)));
        tree.Context.PushEvent(PointerTree.Down(new float2(160f, 30f), PointerButton.Left));
        tree.Context.PushEvent(PointerTree.Move(new float2(160f, 29f)));
        tree.Context.Update(0f);

        Assert.Equal(new float2(0f, 20f), tree.View.ScrollOffset);
        Assert.Equal(1, calls);

        tree.Context.PushEvent(PointerTree.Move(new float2(160f, 19f)));
        tree.Context.Update(0f);

        Assert.Equal(new float2(0f, 30f), tree.View.ScrollOffset);
        Assert.Equal(2, calls);
    }

    /// <summary>
    /// A wheel scroll during a drag adds up with the drag. A press at <see cref="ScrollViewTree.OverGap"/> and a move to
    /// (160, 40) give (0, 10). A scroll of (0, -1) gives (0, 50). A move to (160, 30) gives (0, 60).
    /// </summary>
    [Fact]
    public void AWheelDuringADragAddsUp()
    {
        ScrollViewTree tree = new();
        tree.Settle();

        tree.Context.PushEvent(PointerTree.Down(ScrollViewTree.OverGap, PointerButton.Left));
        tree.Context.PushEvent(PointerTree.Move(new float2(160f, 40f)));
        tree.Context.Update(0f);
        Assert.Equal(new float2(0f, 10f), tree.View.ScrollOffset);

        tree.Context.PushEvent(PointerTree.Scroll(new float2(160f, 40f), new float2(0f, -1f)));
        tree.Context.Update(0f);
        Assert.Equal(new float2(0f, 50f), tree.View.ScrollOffset);

        tree.Context.PushEvent(PointerTree.Move(new float2(160f, 30f)));
        tree.Context.Update(0f);
        Assert.Equal(new float2(0f, 60f), tree.View.ScrollOffset);
    }

    /// <summary>Gives the rect of the only rect call with a fill.</summary>
    /// <param name="renderer">The renderer.</param>
    /// <param name="color">The fill.</param>
    /// <returns>The rect of the call.</returns>
    private static float4 RectOf(RecordingRenderer renderer, Rgba8 color)
    {
        int found = -1;
        System.ReadOnlySpan<DrawCall> calls = renderer.Calls;
        for (int i = 0; i < calls.Length; i++)
        {
            if (calls[i].Kind == DrawCallKind.Rect && calls[i].Color.Equals(color))
            {
                Assert.Equal(-1, found);
                found = i;
            }
        }

        Assert.NotEqual(-1, found);
        return calls[found].Rect;
    }
}
