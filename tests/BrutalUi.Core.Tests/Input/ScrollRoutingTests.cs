using Brutal.Numerics;
using Xunit;

namespace BrutalUi.Core.Tests;

/// <summary>
/// Tests of the scroll step of the drain: a scroll with no target walks to the element under its position and moves
/// <see cref="PseudoStates.Hover"/> to it.
/// </summary>
/// <remarks>
/// The class table is global and xUnit runs test classes in parallel. Each test gives the tree a unique name prefix.
/// </remarks>
public sealed class ScrollRoutingTests
{
    /// <summary>
    /// A scroll over the child of the second child bubbles from that child to the second child and to the root. The
    /// delta and the position survive the ring.
    /// </summary>
    [Fact]
    public void AScrollGoesToTheElementUnderThePointer()
    {
        PointerTree tree = new(nameof(ScrollRoutingTests) + "." + nameof(AScrollGoesToTheElementUnderThePointer));
        tree.Settle();

        EventRecorder recorder = new();
        recorder.Register(tree.Root);
        recorder.Register(tree.Right);
        recorder.Register(tree.Inner);

        tree.Context.PushEvent(PointerTree.Scroll(new float2(125f, 25f), new float2(1f, -3f)));
        Assert.Equal(1, tree.Context.DrainEvents());

        // Capture: root, Right. Target: Inner in the capture slot and in the bubble slot. Bubble: Right, root.
        Assert.False(recorder.Overflow);
        Assert.Equal(6, recorder.Count);

        Assert.Same(tree.Root, recorder[0].CurrentTarget);
        Assert.Equal(UiEventPhase.Capture, recorder[0].Phase);
        Assert.Same(tree.Right, recorder[1].CurrentTarget);
        Assert.Equal(UiEventPhase.Capture, recorder[1].Phase);
        Assert.Same(tree.Inner, recorder[2].CurrentTarget);
        Assert.Equal(UiEventPhase.Target, recorder[2].Phase);
        Assert.Same(tree.Inner, recorder[3].CurrentTarget);
        Assert.Equal(UiEventPhase.Target, recorder[3].Phase);
        Assert.Same(tree.Right, recorder[4].CurrentTarget);
        Assert.Equal(UiEventPhase.Bubble, recorder[4].Phase);
        Assert.Same(tree.Root, recorder[5].CurrentTarget);
        Assert.Equal(UiEventPhase.Bubble, recorder[5].Phase);

        for (int i = 0; i < recorder.Count; i++)
        {
            Assert.Equal(UiEventType.Scroll, recorder[i].Type);
        }

        Assert.Same(tree.Inner, recorder.LastScroll.Header.Target);
        Assert.Equal(new float2(125f, 25f), recorder.LastScroll.Position);
        Assert.Equal(new float2(1f, -3f), recorder.LastScroll.Delta);
        Assert.Equal(UiModifiers.None, recorder.LastScroll.Modifiers);
    }

    /// <summary>A scroll over no element gets no walk, and it clears <see cref="PseudoStates.Hover"/>.</summary>
    [Fact]
    public void AScrollOverNothingWalksNothing()
    {
        PointerTree tree = new(nameof(ScrollRoutingTests) + "." + nameof(AScrollOverNothingWalksNothing));
        tree.Settle();

        EventRecorder recorder = new();
        recorder.Register(tree.Root);
        recorder.Register(tree.Right);

        tree.Context.PushEvent(PointerTree.Move(new float2(175f, 75f)));
        Assert.Equal(1, tree.Context.DrainEvents());
        Assert.Equal(PseudoStates.Hover, tree.Right.States);
        recorder.Reset();

        tree.Context.PushEvent(PointerTree.Scroll(new float2(700f, 500f), new float2(0f, 2f)));
        Assert.Equal(0, tree.Context.DrainEvents());

        Assert.Equal(0, recorder.Count);
        Assert.Equal(0, tree.Context.QueuedEventCount);
        Assert.Null(tree.Context.PointerHit);
        Assert.Equal(PseudoStates.None, tree.Right.States);
        Assert.Equal(new float2(700f, 500f), tree.Context.PointerPosition);
    }

    /// <summary>
    /// A scroll moves <see cref="PseudoStates.Hover"/> to the element under its position and stores the hit and the
    /// position (Decision 7).
    /// </summary>
    [Fact]
    public void AScrollMovesHover()
    {
        PointerTree tree = new(nameof(ScrollRoutingTests) + "." + nameof(AScrollMovesHover));
        tree.AddHoverSheet();
        tree.Settle();

        tree.Context.PushEvent(PointerTree.Move(new float2(50f, 50f)));
        Assert.Equal(1, tree.Context.DrainEvents());
        tree.Settle();
        Assert.Equal(PseudoStates.Hover, tree.Left.States);

        tree.Context.PushEvent(PointerTree.Scroll(new float2(175f, 75f), new float2(0f, -1f)));
        Assert.Equal(1, tree.Context.DrainEvents());
        tree.Settle();

        Assert.Equal(PseudoStates.None, tree.Left.States);
        Assert.Equal(PseudoStates.Hover, tree.Right.States);
        Assert.Equal(PointerTree.HoverFill, tree.Right.Style.BackgroundColor);
        Assert.NotEqual(PointerTree.HoverFill, tree.Left.Style.BackgroundColor);
        Assert.Same(tree.Right, tree.Context.PointerHit);
        Assert.Equal(new float2(175f, 75f), tree.Context.PointerPosition);
        Assert.Null(tree.Context.ActiveElement);
        Assert.Equal(PseudoStates.None, tree.Root.States);
    }

    /// <summary>
    /// A scroll with a target from the caller keeps the target. It skips the hit test and changes neither the hover
    /// nor the stored hit nor the stored position.
    /// </summary>
    [Fact]
    public void AScrollWithACallerTargetKeepsTheTarget()
    {
        PointerTree tree = new(nameof(ScrollRoutingTests) + "." + nameof(AScrollWithACallerTargetKeepsTheTarget));
        tree.Settle();

        EventRecorder recorder = new();
        recorder.Register(tree.Root);
        recorder.Register(tree.Left);

        // The position is over Right, and the target is Left.
        ScrollEvent scroll = PointerTree.Scroll(new float2(175f, 75f), new float2(0f, 5f));
        scroll.Header.Target = tree.Left;
        tree.Context.PushEvent(scroll);
        Assert.Equal(1, tree.Context.DrainEvents());

        Assert.False(recorder.Overflow);
        Assert.Equal(4, recorder.Count);
        Assert.Same(tree.Left, recorder.LastScroll.Header.Target);
        Assert.Equal(new float2(0f, 5f), recorder.LastScroll.Delta);
        Assert.Same(tree.Left, recorder[1].CurrentTarget);
        Assert.Same(tree.Root, recorder[3].CurrentTarget);

        Assert.Null(tree.Context.PointerHit);
        Assert.False(tree.Context.HasPointerPosition);
        Assert.Equal(PseudoStates.None, tree.Left.States);
        Assert.Equal(PseudoStates.None, tree.Right.States);
    }
}
