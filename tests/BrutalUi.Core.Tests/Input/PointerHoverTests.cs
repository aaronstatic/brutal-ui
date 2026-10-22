using Brutal.Numerics;
using Xunit;

namespace BrutalUi.Core.Tests;

/// <summary>
/// Tests of the pointer step of the drain: a pointer move with no target finds the element under the pointer, moves
/// <see cref="PseudoStates.Hover"/> to it, stores the hit, and walks to it.
/// </summary>
/// <remarks>
/// The class table is global and xUnit runs test classes in parallel. Each test gives the tree a unique name prefix.
/// </remarks>
public sealed class PointerHoverTests
{
    /// <summary>
    /// The demo of phase 1. A move over the second child sets <see cref="PseudoStates.Hover"/> on it, and the next
    /// update gives it the fill of the hover rule. No ancestor and no sibling gets the state (Decision R1).
    /// </summary>
    [Fact]
    public void AMoveOverAChildSetsHoverAndShowsTheHoverRule()
    {
        PointerTree tree = new(nameof(PointerHoverTests) + "." + nameof(AMoveOverAChildSetsHoverAndShowsTheHoverRule));
        tree.AddHoverSheet();
        tree.Settle();
        Assert.NotEqual(PointerTree.HoverFill, tree.Right.Style.BackgroundColor);

        tree.Context.PushEvent(PointerTree.Move(new float2(175f, 75f)));
        Assert.Equal(1, tree.Context.DrainEvents());
        tree.Settle();

        TestContext.Current.TestOutputHelper?.WriteLine(
            tree.Right.Name + " " + tree.Right.States + " " + tree.Right.Style.BackgroundColor);

        Assert.Equal(PseudoStates.Hover, tree.Right.States);
        Assert.Equal(PointerTree.HoverFill, tree.Right.Style.BackgroundColor);
        Assert.Equal(PseudoStates.None, tree.Root.States);
        Assert.Equal(PseudoStates.None, tree.Left.States);
        Assert.Equal(PseudoStates.None, tree.Inner.States);
        Assert.NotEqual(PointerTree.HoverFill, tree.Left.Style.BackgroundColor);
    }

    /// <summary>A move from the first child to the second child moves the state and the hover fill.</summary>
    [Fact]
    public void AMoveToASiblingMovesHover()
    {
        PointerTree tree = new(nameof(PointerHoverTests) + "." + nameof(AMoveToASiblingMovesHover));
        tree.AddHoverSheet();
        tree.Settle();

        tree.Context.PushEvent(PointerTree.Move(new float2(50f, 50f)));
        Assert.Equal(1, tree.Context.DrainEvents());
        tree.Settle();
        Assert.Equal(PseudoStates.Hover, tree.Left.States);
        Assert.Equal(PointerTree.HoverFill, tree.Left.Style.BackgroundColor);

        tree.Context.PushEvent(PointerTree.Move(new float2(175f, 75f)));
        Assert.Equal(1, tree.Context.DrainEvents());
        tree.Settle();

        Assert.Equal(PseudoStates.None, tree.Left.States);
        Assert.NotEqual(PointerTree.HoverFill, tree.Left.Style.BackgroundColor);
        Assert.Equal(PseudoStates.Hover, tree.Right.States);
        Assert.Equal(PointerTree.HoverFill, tree.Right.Style.BackgroundColor);
    }

    /// <summary>
    /// A move outside each root clears the state and gets no walk. A move to <c>(NaN, NaN)</c> (a cursor leave) also
    /// clears the state.
    /// </summary>
    [Fact]
    public void AMoveOutsideClearsHover()
    {
        PointerTree tree = new(nameof(PointerHoverTests) + "." + nameof(AMoveOutsideClearsHover));
        tree.AddHoverSheet();
        tree.Settle();

        tree.Context.PushEvent(PointerTree.Move(new float2(175f, 75f)));
        Assert.Equal(1, tree.Context.DrainEvents());
        tree.Settle();
        Assert.Equal(PseudoStates.Hover, tree.Right.States);

        tree.Context.PushEvent(PointerTree.Move(new float2(700f, 500f)));
        Assert.Equal(0, tree.Context.DrainEvents());
        tree.Settle();

        Assert.Equal(PseudoStates.None, tree.Right.States);
        Assert.NotEqual(PointerTree.HoverFill, tree.Right.Style.BackgroundColor);
        Assert.Null(tree.Context.PointerHit);

        tree.Context.PushEvent(PointerTree.Move(new float2(50f, 50f)));
        Assert.Equal(1, tree.Context.DrainEvents());
        Assert.Equal(PseudoStates.Hover, tree.Left.States);

        tree.Context.PushEvent(PointerTree.Move(new float2(float.NaN, float.NaN)));
        Assert.Equal(0, tree.Context.DrainEvents());
        Assert.Equal(PseudoStates.None, tree.Left.States);
        Assert.Null(tree.Context.PointerHit);
    }

    /// <summary>
    /// The move walks to the hit element: the root capture, the target capture, the target bubble, and the root
    /// bubble. The target of the event is the hit element. The states change before the walk (Decision 9): the
    /// target handler reads <see cref="PseudoStates.Hover"/> on the new owner and no state on the old owner.
    /// </summary>
    [Fact]
    public void TheMoveWalksToTheHitElement()
    {
        PointerTree tree = new(nameof(PointerHoverTests) + "." + nameof(TheMoveWalksToTheHitElement));
        tree.Settle();

        // Hover the old owner first, so the move to the new owner has a state to clear.
        tree.Context.PushEvent(PointerTree.Move(new float2(50f, 50f)));
        Assert.Equal(1, tree.Context.DrainEvents());
        Assert.Equal(PseudoStates.Hover, tree.Left.States);

        EventRecorder recorder = new();
        recorder.Register(tree.Root);
        recorder.Register(tree.Right);

        PseudoStates rightInHandler = PseudoStates.None;
        PseudoStates leftInHandler = PseudoStates.Hover;
        tree.Right.OnPointerMove += (ref PointerEvent e) =>
        {
            rightInHandler = tree.Right.States;
            leftInHandler = tree.Left.States;
        };

        tree.Context.PushEvent(PointerTree.Move(new float2(175f, 75f)));
        Assert.Equal(1, tree.Context.DrainEvents());

        Assert.Equal(PseudoStates.Hover, rightInHandler);
        Assert.Equal(PseudoStates.None, leftInHandler);

        Assert.False(recorder.Overflow);
        Assert.Equal(4, recorder.Count);

        Assert.Same(tree.Root, recorder[0].CurrentTarget);
        Assert.Equal(UiEventPhase.Capture, recorder[0].Phase);
        Assert.True(recorder[0].CaptureSlot);

        Assert.Same(tree.Right, recorder[1].CurrentTarget);
        Assert.Equal(UiEventPhase.Target, recorder[1].Phase);
        Assert.True(recorder[1].CaptureSlot);

        Assert.Same(tree.Right, recorder[2].CurrentTarget);
        Assert.Equal(UiEventPhase.Target, recorder[2].Phase);
        Assert.False(recorder[2].CaptureSlot);

        Assert.Same(tree.Root, recorder[3].CurrentTarget);
        Assert.Equal(UiEventPhase.Bubble, recorder[3].Phase);
        Assert.False(recorder[3].CaptureSlot);

        for (int i = 0; i < recorder.Count; i++)
        {
            Assert.Equal(UiEventType.PointerMove, recorder[i].Type);
        }

        Assert.Same(tree.Right, recorder.LastPointer.Header.Target);
        Assert.Equal(new float2(175f, 75f), recorder.LastPointer.Position);
    }

    /// <summary>
    /// The context stores the hit and the position of the last pointer entry with no target. A miss stores a null hit
    /// and keeps the position.
    /// </summary>
    [Fact]
    public void TheContextStoresTheLastHit()
    {
        PointerTree tree = new(nameof(PointerHoverTests) + "." + nameof(TheContextStoresTheLastHit));
        tree.Settle();

        Assert.Null(tree.Context.PointerHit);
        Assert.False(tree.Context.HasPointerPosition);
        Assert.Equal(new float2(0f, 0f), tree.Context.PointerPosition);

        tree.Context.PushEvent(PointerTree.Move(new float2(125f, 25f)));
        Assert.Equal(1, tree.Context.DrainEvents());

        Assert.Same(tree.Inner, tree.Context.PointerHit);
        Assert.True(tree.Context.HasPointerPosition);
        Assert.Equal(new float2(125f, 25f), tree.Context.PointerPosition);

        tree.Context.PushEvent(PointerTree.Move(new float2(300f, 200f)));
        Assert.Equal(1, tree.Context.DrainEvents());

        Assert.Same(tree.Root, tree.Context.PointerHit);
        Assert.Equal(new float2(300f, 200f), tree.Context.PointerPosition);

        tree.Context.PushEvent(PointerTree.Move(new float2(700f, 500f)));
        Assert.Equal(0, tree.Context.DrainEvents());

        Assert.Null(tree.Context.PointerHit);
        Assert.True(tree.Context.HasPointerPosition);
        Assert.Equal(new float2(700f, 500f), tree.Context.PointerPosition);
    }
}
