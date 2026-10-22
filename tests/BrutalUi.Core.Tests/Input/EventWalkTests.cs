using Brutal.Numerics;
using Xunit;

namespace BrutalUi.Core.Tests;

/// <summary>
/// Tests of the pointer walk: <see cref="UiContext.PushEvent(in PointerEvent)"/>, the internal
/// <see cref="UiContext.DrainEvents"/>, and <see cref="EventWalk"/>.
/// </summary>
/// <remarks>
/// Each test builds a small tree, adds the root to a context, pushes hand-made events, and drains the ring. An
/// <see cref="EventRecorder"/> records the handler calls.
/// </remarks>
public sealed class EventWalkTests
{
    /// <summary>
    /// The demo of phase 1: a pointer down at the leaf of a 3-deep tree. The walk runs capture (root, middle), target
    /// (leaf: the capture slot, then the bubble slot), and bubble (middle, root).
    /// </summary>
    [Fact]
    public void PointerDownWalksCaptureTargetBubble()
    {
        Element root = new() { Name = "root" };
        Element middle = new() { Name = "middle" };
        Element leaf = new() { Name = "leaf" };
        root.Children.Add(middle);
        middle.Children.Add(leaf);

        UiContext context = NewContext();
        context.AddRoot(root);

        EventRecorder recorder = new();
        recorder.Register(root);
        recorder.Register(middle);
        recorder.Register(leaf);

        context.PushEvent(Pointer(UiEventType.PointerDown, leaf));
        int walks = context.DrainEvents();

        Assert.Equal(1, walks);
        Assert.False(recorder.Overflow);
        Assert.Equal(6, recorder.Count);
        AssertRecord(recorder[0], root, UiEventPhase.Capture, UiEventType.PointerDown, captureSlot: true);
        AssertRecord(recorder[1], middle, UiEventPhase.Capture, UiEventType.PointerDown, captureSlot: true);
        AssertRecord(recorder[2], leaf, UiEventPhase.Target, UiEventType.PointerDown, captureSlot: true);
        AssertRecord(recorder[3], leaf, UiEventPhase.Target, UiEventType.PointerDown, captureSlot: false);
        AssertRecord(recorder[4], middle, UiEventPhase.Bubble, UiEventType.PointerDown, captureSlot: false);
        AssertRecord(recorder[5], root, UiEventPhase.Bubble, UiEventType.PointerDown, captureSlot: false);
    }

    /// <summary>
    /// A root target has no ancestor. The walk calls the capture slot and the bubble slot of the root with the phase
    /// <see cref="UiEventPhase.Target"/>, and calls no slot of a child.
    /// </summary>
    [Fact]
    public void ARootTargetRunsTheTargetPhaseOnly()
    {
        Element root = new() { Name = "root" };
        Element child = new() { Name = "child" };
        root.Children.Add(child);

        UiContext context = NewContext();
        context.AddRoot(root);

        EventRecorder recorder = new();
        recorder.Register(root);
        recorder.Register(child);

        context.PushEvent(Pointer(UiEventType.PointerUp, root));
        int walks = context.DrainEvents();

        Assert.Equal(1, walks);
        Assert.Equal(2, recorder.Count);
        AssertRecord(recorder[0], root, UiEventPhase.Target, UiEventType.PointerUp, captureSlot: true);
        AssertRecord(recorder[1], root, UiEventPhase.Target, UiEventType.PointerUp, captureSlot: false);
    }

    /// <summary>
    /// A null set on an element with no handler creates no side object (A2). The walk skips an ancestor with no side
    /// object, and the walk leaves it with no side object.
    /// </summary>
    [Fact]
    public void AnElementWithNoHandlerHasNoSideObject()
    {
        Element root = new() { Name = "root" };
        Element leaf = new() { Name = "leaf" };
        root.Children.Add(leaf);

        Assert.Null(root.Handlers);
        root.OnPointerDown = null;
        root.OnPointerDownCapture = null;
        Assert.Null(root.Handlers);
        Assert.Null(root.OnPointerDown);

        UiContext context = NewContext();
        context.AddRoot(root);

        EventRecorder recorder = new();
        recorder.Register(leaf);
        Assert.NotNull(leaf.Handlers);

        context.PushEvent(Pointer(UiEventType.PointerDown, leaf));
        int walks = context.DrainEvents();

        Assert.Equal(1, walks);
        Assert.Null(root.Handlers);
        Assert.Equal(2, recorder.Count);
        AssertRecord(recorder[0], leaf, UiEventPhase.Target, UiEventType.PointerDown, captureSlot: true);
        AssertRecord(recorder[1], leaf, UiEventPhase.Target, UiEventType.PointerDown, captureSlot: false);

        // A null set on an element with a side object clears the slot and keeps the side object.
        ElementHandlers? handlers = leaf.Handlers;
        leaf.OnPointerDown = null;
        Assert.Same(handlers, leaf.Handlers);
        Assert.Null(leaf.OnPointerDown);
    }

    /// <summary>
    /// Each pointer type reaches the two slots of its own type only. A move calls no down slot and no up slot.
    /// </summary>
    [Fact]
    public void EachPointerTypeReachesItsOwnSlots()
    {
        Element root = new() { Name = "root" };
        Element leaf = new() { Name = "leaf" };
        root.Children.Add(leaf);

        UiContext context = NewContext();
        context.AddRoot(root);

        EventRecorder moves = new();
        EventRecorder downs = new();
        EventRecorder ups = new();
        foreach (Element element in new[] { root, leaf })
        {
            element.OnPointerMove = moves.PointerBubble;
            element.OnPointerMoveCapture = moves.PointerCapture;
            element.OnPointerDown = downs.PointerBubble;
            element.OnPointerDownCapture = downs.PointerCapture;
            element.OnPointerUp = ups.PointerBubble;
            element.OnPointerUpCapture = ups.PointerCapture;
        }

        context.PushEvent(Pointer(UiEventType.PointerMove, leaf));
        Assert.Equal(1, context.DrainEvents());
        Assert.Equal(4, moves.Count);
        Assert.Equal(0, downs.Count);
        Assert.Equal(0, ups.Count);
        AssertRecord(moves[0], root, UiEventPhase.Capture, UiEventType.PointerMove, captureSlot: true);
        AssertRecord(moves[1], leaf, UiEventPhase.Target, UiEventType.PointerMove, captureSlot: true);
        AssertRecord(moves[2], leaf, UiEventPhase.Target, UiEventType.PointerMove, captureSlot: false);
        AssertRecord(moves[3], root, UiEventPhase.Bubble, UiEventType.PointerMove, captureSlot: false);

        context.PushEvent(Pointer(UiEventType.PointerDown, leaf));
        context.PushEvent(Pointer(UiEventType.PointerUp, leaf));
        Assert.Equal(2, context.DrainEvents());
        Assert.Equal(4, moves.Count);
        Assert.Equal(4, downs.Count);
        Assert.Equal(4, ups.Count);
        for (int i = 0; i < 4; i++)
        {
            Assert.Equal(UiEventType.PointerDown, downs[i].Type);
            Assert.Equal(UiEventType.PointerUp, ups[i].Type);
        }
    }

    /// <summary>
    /// The drain returns the number of walks, skips an event with no target (A4), and empties the ring. A second
    /// drain finds no event.
    /// </summary>
    [Fact]
    public void DrainReturnsTheWalkCountAndEmptiesTheQueue()
    {
        Element root = new() { Name = "root" };
        Element leaf = new() { Name = "leaf" };
        root.Children.Add(leaf);

        UiContext context = NewContext();
        context.AddRoot(root);

        EventRecorder recorder = new();
        recorder.Register(leaf);

        Assert.Equal(0, context.QueuedEventCount);
        context.PushEvent(Pointer(UiEventType.PointerMove, leaf));
        context.PushEvent(Pointer(UiEventType.PointerDown, null));
        context.PushEvent(Pointer(UiEventType.PointerDown, leaf));
        context.PushEvent(Pointer(UiEventType.PointerUp, leaf));
        Assert.Equal(4, context.QueuedEventCount);

        int walks = context.DrainEvents();

        Assert.Equal(3, walks);
        Assert.Equal(0, context.QueuedEventCount);
        Assert.Equal(6, recorder.Count);
        Assert.Equal(UiEventType.PointerMove, recorder[0].Type);
        Assert.Equal(UiEventType.PointerDown, recorder[2].Type);
        Assert.Equal(UiEventType.PointerUp, recorder[4].Type);

        Assert.Equal(0, context.DrainEvents());
        Assert.Equal(6, recorder.Count);
    }

    /// <summary>A stop in the capture phase of an ancestor ends the walk. No later element gets a call.</summary>
    [Fact]
    public void AStopInCaptureEndsTheWalk()
    {
        Element root = new() { Name = "root" };
        Element middle = new() { Name = "middle" };
        Element leaf = new() { Name = "leaf" };
        root.Children.Add(middle);
        middle.Children.Add(leaf);

        UiContext context = NewContext();
        context.AddRoot(root);

        EventRecorder recorder = new();
        recorder.Register(root);
        recorder.Register(middle);
        recorder.Register(leaf);
        recorder.StopAt(middle, UiEventPhase.Capture, captureSlot: true);

        context.PushEvent(Pointer(UiEventType.PointerDown, leaf));

        Assert.Equal(1, context.DrainEvents());
        Assert.Equal(2, recorder.Count);
        AssertRecord(recorder[0], root, UiEventPhase.Capture, UiEventType.PointerDown, captureSlot: true);
        AssertRecord(recorder[1], middle, UiEventPhase.Capture, UiEventType.PointerDown, captureSlot: true);
    }

    /// <summary>
    /// A stop in the capture slot of the target ends the walk. The bubble slot of the target and the bubble phase do
    /// not run (Decision R1).
    /// </summary>
    [Fact]
    public void AStopInTheTargetCaptureSlotSkipsTheTargetBubbleSlot()
    {
        Element root = new() { Name = "root" };
        Element middle = new() { Name = "middle" };
        Element leaf = new() { Name = "leaf" };
        root.Children.Add(middle);
        middle.Children.Add(leaf);

        UiContext context = NewContext();
        context.AddRoot(root);

        EventRecorder recorder = new();
        recorder.Register(root);
        recorder.Register(middle);
        recorder.Register(leaf);
        recorder.StopAt(leaf, UiEventPhase.Target, captureSlot: true);

        context.PushEvent(Pointer(UiEventType.PointerDown, leaf));

        Assert.Equal(1, context.DrainEvents());
        Assert.Equal(3, recorder.Count);
        AssertRecord(recorder[0], root, UiEventPhase.Capture, UiEventType.PointerDown, captureSlot: true);
        AssertRecord(recorder[1], middle, UiEventPhase.Capture, UiEventType.PointerDown, captureSlot: true);
        AssertRecord(recorder[2], leaf, UiEventPhase.Target, UiEventType.PointerDown, captureSlot: true);
    }

    /// <summary>A stop in the bubble phase of an element skips the bubble slots of the ancestors above it.</summary>
    [Fact]
    public void AStopInBubbleSkipsTheAncestors()
    {
        Element root = new() { Name = "root" };
        Element middle = new() { Name = "middle" };
        Element leaf = new() { Name = "leaf" };
        root.Children.Add(middle);
        middle.Children.Add(leaf);

        UiContext context = NewContext();
        context.AddRoot(root);

        EventRecorder recorder = new();
        recorder.Register(root);
        recorder.Register(middle);
        recorder.Register(leaf);
        recorder.StopAt(middle, UiEventPhase.Bubble, captureSlot: false);

        context.PushEvent(Pointer(UiEventType.PointerDown, leaf));

        Assert.Equal(1, context.DrainEvents());
        Assert.Equal(5, recorder.Count);
        AssertRecord(recorder[3], leaf, UiEventPhase.Target, UiEventType.PointerDown, captureSlot: false);
        AssertRecord(recorder[4], middle, UiEventPhase.Bubble, UiEventType.PointerDown, captureSlot: false);
    }

    /// <summary>
    /// A stop in a multicast slot skips the next delegate of the same slot, and skips the later phases (Decision 5).
    /// </summary>
    [Fact]
    public void AStopInAMulticastSlotSkipsTheNextDelegate()
    {
        Element root = new() { Name = "root" };
        Element leaf = new() { Name = "leaf" };
        root.Children.Add(leaf);

        UiContext context = NewContext();
        context.AddRoot(root);

        EventRecorder first = new();
        EventRecorder second = new();
        EventRecorder ancestor = new();
        ancestor.Register(root);
        first.Register(leaf);
        leaf.OnPointerDown = first.PointerBubble + second.PointerBubble;
        first.StopAt(leaf, UiEventPhase.Target, captureSlot: false);

        context.PushEvent(Pointer(UiEventType.PointerDown, leaf));

        Assert.Equal(1, context.DrainEvents());
        Assert.Equal(2, first.Count);
        Assert.Equal(0, second.Count);
        Assert.Equal(1, ancestor.Count);
    }

    /// <summary>
    /// A stop applies to the event that a handler stopped. The next event walks in full, also when the caller stopped
    /// the struct before the push.
    /// </summary>
    [Fact]
    public void AStopAppliesToOneEventOnly()
    {
        Element root = new() { Name = "root" };
        Element middle = new() { Name = "middle" };
        Element leaf = new() { Name = "leaf" };
        root.Children.Add(middle);
        middle.Children.Add(leaf);

        UiContext context = NewContext();
        context.AddRoot(root);

        EventRecorder downs = new();
        EventRecorder ups = new();
        foreach (Element element in new[] { root, middle, leaf })
        {
            element.OnPointerDown = downs.PointerBubble;
            element.OnPointerDownCapture = downs.PointerCapture;
            element.OnPointerUp = ups.PointerBubble;
            element.OnPointerUpCapture = ups.PointerCapture;
        }

        downs.StopAt(middle, UiEventPhase.Capture, captureSlot: true);

        PointerEvent up = Pointer(UiEventType.PointerUp, leaf);
        up.Header.StopPropagation();
        Assert.True(up.Header.IsPropagationStopped);

        context.PushEvent(Pointer(UiEventType.PointerDown, leaf));
        context.PushEvent(up);

        Assert.Equal(2, context.DrainEvents());
        Assert.Equal(2, downs.Count);
        Assert.Equal(6, ups.Count);
    }

    /// <summary>
    /// The walk reads the event type one time at the start. A capture handler of the root that changes
    /// <see cref="UiEventHeader.Type"/> does not change the slots of the walk: the six down calls run, and no up slot
    /// gets a call (P1-9).
    /// </summary>
    [Fact]
    public void AHandlerThatChangesTheTypeDoesNotChangeTheSlots()
    {
        Element root = new() { Name = "root" };
        Element middle = new() { Name = "middle" };
        Element leaf = new() { Name = "leaf" };
        root.Children.Add(middle);
        middle.Children.Add(leaf);

        UiContext context = NewContext();
        context.AddRoot(root);

        EventRecorder downs = new();
        EventRecorder ups = new();
        foreach (Element element in new[] { root, middle, leaf })
        {
            element.OnPointerDown = downs.PointerBubble;
            element.OnPointerDownCapture = downs.PointerCapture;
            element.OnPointerUp = ups.PointerBubble;
            element.OnPointerUpCapture = ups.PointerCapture;
        }

        root.OnPointerDownCapture = (ref PointerEvent e) =>
        {
            downs.PointerCapture(ref e);
            e.Header.Type = UiEventType.PointerUp;
        };

        context.PushEvent(Pointer(UiEventType.PointerDown, leaf));

        Assert.Equal(1, context.DrainEvents());
        Assert.Equal(6, downs.Count);
        Assert.Equal(0, ups.Count);
    }

    /// <summary>
    /// The walk reads the bubble slot of the target after the capture call of the target (P1-10). A capture handler
    /// that sets the bubble slot to a new delegate makes the walk call the new delegate, and not the old delegate.
    /// </summary>
    [Fact]
    public void ATargetCaptureHandlerThatSetsTheBubbleSlotGetsTheNewSlot()
    {
        Element root = new() { Name = "root" };
        Element leaf = new() { Name = "leaf" };
        root.Children.Add(leaf);

        UiContext context = NewContext();
        context.AddRoot(root);

        EventRecorder first = new();
        EventRecorder second = new();
        leaf.OnPointerDown = first.PointerBubble;
        leaf.OnPointerDownCapture = (ref PointerEvent e) =>
        {
            first.PointerCapture(ref e);
            leaf.OnPointerDown = second.PointerBubble;
        };

        context.PushEvent(Pointer(UiEventType.PointerDown, leaf));

        Assert.Equal(1, context.DrainEvents());
        Assert.Equal(1, first.Count);
        AssertRecord(first[0], leaf, UiEventPhase.Target, UiEventType.PointerDown, captureSlot: true);
        Assert.Equal(1, second.Count);
        AssertRecord(second[0], leaf, UiEventPhase.Target, UiEventType.PointerDown, captureSlot: false);
    }

    /// <summary>
    /// A capture handler of the target that sets the bubble slot to <see langword="null"/> makes the walk skip the
    /// bubble slot of the target (P1-10). The walk still runs the bubble phase of the ancestors.
    /// </summary>
    [Fact]
    public void ATargetCaptureHandlerThatClearsTheBubbleSlotSkipsIt()
    {
        Element root = new() { Name = "root" };
        Element leaf = new() { Name = "leaf" };
        root.Children.Add(leaf);

        UiContext context = NewContext();
        context.AddRoot(root);

        EventRecorder first = new();
        EventRecorder ancestor = new();
        ancestor.Register(root);
        leaf.OnPointerDown = first.PointerBubble;
        leaf.OnPointerDownCapture = (ref PointerEvent e) =>
        {
            first.PointerCapture(ref e);
            leaf.OnPointerDown = null;
        };

        context.PushEvent(Pointer(UiEventType.PointerDown, leaf));

        Assert.Equal(1, context.DrainEvents());
        Assert.Equal(1, first.Count);
        AssertRecord(first[0], leaf, UiEventPhase.Target, UiEventType.PointerDown, captureSlot: true);
        Assert.Equal(2, ancestor.Count);
        AssertRecord(ancestor[1], root, UiEventPhase.Bubble, UiEventType.PointerDown, captureSlot: false);
    }

    private static UiContext NewContext()
    {
        return new UiContext(new UiContext.CreateInfo { Measurer = new RecordingRenderer() });
    }

    private static PointerEvent Pointer(UiEventType type, Element? target)
    {
        return new PointerEvent
        {
            Header = new UiEventHeader(type, 1.5, target),
            Position = new float2(10, 20),
            Button = PointerButton.Left,
            Modifiers = UiModifiers.None,
        };
    }

    private static void AssertRecord(
        EventRecord record,
        Element expectedTarget,
        UiEventPhase expectedPhase,
        UiEventType expectedType,
        bool captureSlot)
    {
        Assert.Same(expectedTarget, record.CurrentTarget);
        Assert.Equal(expectedPhase, record.Phase);
        Assert.Equal(expectedType, record.Type);
        Assert.Equal(captureSlot, record.CaptureSlot);
    }
}
