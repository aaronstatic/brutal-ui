using System;
using Brutal.Numerics;
using Xunit;

namespace BrutalUi.Core.Tests;

/// <summary>
/// Tests of the disabled rule of <see cref="ScrollView"/> (Decision 11): a view that holds
/// <see cref="PseudoStates.Disabled"/> takes no wheel, no drag, and no capture, and it does not stop the walk. The walks
/// still apply its offset, and a set from code still works.
/// </summary>
/// <remarks>
/// The scene is <see cref="ScrollViewTree"/>: the view is 200 x 100 at (0, 0), the content is 300 x 180, and the maximum
/// offset is (100, 80). The root is pickable, so a handler on the root shows that an event walked past the view. The tests
/// intern no class.
/// </remarks>
public sealed class ScrollViewDisabledTests
{
    /// <summary>
    /// A scroll of (0, -1) over <see cref="ScrollViewTree.Row0"/> of a disabled view changes nothing and calls nothing.
    /// The bubble handler of the root gets the scroll, because the view does not stop the walk.
    /// </summary>
    [Fact]
    public void ADisabledViewIgnoresTheWheel()
    {
        ScrollViewTree tree = new();
        tree.View.States |= PseudoStates.Disabled;
        tree.Settle();
        int calls = 0;
        tree.View.ScrollChanged = _ => calls++;
        int rootScrolls = 0;
        tree.Root.OnScroll = (ref ScrollEvent e) => rootScrolls++;

        tree.Context.PushEvent(PointerTree.Scroll(ScrollViewTree.OverRow0, new float2(0f, -1f)));
        tree.Context.Update(0f);

        Assert.Equal(new float2(0f, 0f), tree.View.ScrollOffset);
        Assert.Equal(0, calls);
        Assert.Equal(1, rootScrolls);
        Assert.Equal(PseudoStates.Disabled, tree.View.States & PseudoStates.Disabled);
    }

    /// <summary>
    /// A left press at <see cref="ScrollViewTree.OverGap"/> on a disabled view reaches the bubble handler of the root. A
    /// move to (160, 20) changes nothing, calls nothing, and also reaches the root.
    /// </summary>
    [Fact]
    public void ADisabledViewIgnoresADrag()
    {
        ScrollViewTree tree = new();
        tree.View.States |= PseudoStates.Disabled;
        tree.Settle();
        int calls = 0;
        tree.View.ScrollChanged = _ => calls++;
        int rootDowns = 0;
        tree.Root.OnPointerDown = (ref PointerEvent e) => rootDowns++;
        int rootMoves = 0;
        tree.Root.OnPointerMove = (ref PointerEvent e) => rootMoves++;

        tree.Context.PushEvent(PointerTree.Down(ScrollViewTree.OverGap, PointerButton.Left));
        Assert.Equal(1, tree.Context.DrainEvents());
        Assert.Equal(1, rootDowns);

        tree.Context.PushEvent(PointerTree.Move(new float2(160f, 20f)));
        Assert.Equal(1, tree.Context.DrainEvents());

        Assert.Equal(new float2(0f, 0f), tree.View.ScrollOffset);
        Assert.Equal(0, calls);
        Assert.Equal(1, rootMoves);
    }

    /// <summary>
    /// A press and a move give the offset (0, 30). After a disable, a move to <see cref="ScrollViewTree.PastRight"/> walks
    /// to the root and the offset stays. With the capture, this move would give the offset (0, 0).
    /// </summary>
    [Fact]
    public void ADisableDuringADragStopsTheDragAndTheCapture()
    {
        ScrollViewTree tree = new();
        tree.Settle();
        int calls = 0;
        tree.View.ScrollChanged = _ => calls++;
        int rootMoves = 0;
        Element? moveTarget = null;
        tree.Root.OnPointerMove = (ref PointerEvent e) =>
        {
            rootMoves++;
            moveTarget = e.Header.Target;
        };

        tree.Context.PushEvent(PointerTree.Down(ScrollViewTree.OverGap, PointerButton.Left));
        tree.Context.PushEvent(PointerTree.Move(new float2(160f, 20f)));
        Assert.Equal(2, tree.Context.DrainEvents());
        Assert.Equal(new float2(0f, 30f), tree.View.ScrollOffset);
        Assert.Equal(1, calls);
        Assert.Equal(0, rootMoves);

        tree.View.States |= PseudoStates.Disabled;
        tree.Context.PushEvent(PointerTree.Move(ScrollViewTree.PastRight));
        Assert.Equal(1, tree.Context.DrainEvents());

        Assert.Equal(new float2(0f, 30f), tree.View.ScrollOffset);
        Assert.Equal(1, calls);
        Assert.Equal(1, rootMoves);
        Assert.Same(tree.Root, moveTarget);
    }

    /// <summary>
    /// A disabled view with the offset (0, 40) draws <see cref="ScrollViewTree.Row0"/> at (0, -40, 300, 40). A move at
    /// (100, 30) hits <see cref="ScrollViewTree.Row2"/>, because the hit walk applies the offset too.
    /// </summary>
    [Fact]
    public void ADisabledViewKeepsItsOffsetInTheWalks()
    {
        ScrollViewTree tree = new();
        tree.Settle();
        tree.View.ScrollOffset = new float2(0f, 40f);
        tree.View.States |= PseudoStates.Disabled;

        RecordingRenderer renderer = new();
        tree.Context.Draw(renderer);
        Assert.Equal(new float4(0f, -40f, 300f, 40f), RectOf(renderer, ScrollViewTree.RowFill(0)));

        float2 point = new(100f, 30f);
        Assert.Same(tree.Row2, tree.Context.HitTest(point));
        tree.Context.PushEvent(PointerTree.Move(point));
        tree.Context.Update(0f);

        Assert.Same(tree.Row2, tree.Context.PointerHit);
        Assert.Equal(new float2(0f, 40f), tree.View.ScrollOffset);
    }

    /// <summary>
    /// A set of <see cref="ScrollView.ScrollOffset"/> works on a disabled view, because
    /// <see cref="PseudoStates.Disabled"/> blocks user input only. The set clamps the value and calls no
    /// <see cref="ScrollView.ScrollChanged"/>.
    /// </summary>
    [Fact]
    public void ACodeSetWorksOnADisabledView()
    {
        ScrollViewTree tree = new();
        tree.View.States |= PseudoStates.Disabled;
        tree.Settle();
        int calls = 0;
        tree.View.ScrollChanged = _ => calls++;

        tree.View.ScrollOffset = new float2(0f, 40f);

        Assert.Equal(new float2(0f, 40f), tree.View.ScrollOffset);
        Assert.Equal(0, calls);

        tree.View.ScrollOffset = new float2(500f, 500f);

        Assert.Equal(new float2(100f, 80f), tree.View.ScrollOffset);
        Assert.Equal(0, calls);
        Assert.Equal(PseudoStates.Disabled, tree.View.States & PseudoStates.Disabled);
    }

    /// <summary>Gives the rect of the only rect call with a fill.</summary>
    /// <param name="renderer">The renderer.</param>
    /// <param name="color">The fill.</param>
    /// <returns>The rect.</returns>
    private static float4 RectOf(RecordingRenderer renderer, Rgba8 color)
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
        return calls[found].Rect;
    }
}
