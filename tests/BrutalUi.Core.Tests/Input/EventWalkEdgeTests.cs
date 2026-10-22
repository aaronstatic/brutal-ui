using System;
using Brutal.Numerics;
using Xunit;

namespace BrutalUi.Core.Tests;

/// <summary>
/// Tests of the edge rules of the drain and the walk: a target outside the tree, a tree change during the walk, a deep
/// tree, a nested drain, and a handler exception.
/// </summary>
public sealed class EventWalkEdgeTests
{
    /// <summary>
    /// The drain skips an entry whose target left the tree and an entry whose target belongs to another context
    /// (Decision 7). A skipped entry gets no walk.
    /// </summary>
    [Fact]
    public void ADrainSkipsATargetOutsideTheTree()
    {
        UiContext context = NewContext();
        Element root = new() { Name = "root" };
        Element kept = new() { Name = "kept" };
        Element removed = new() { Name = "removed" };
        Element removedLeaf = new() { Name = "removedLeaf" };
        root.Children.Add(kept);
        root.Children.Add(removed);
        removed.Children.Add(removedLeaf);
        context.AddRoot(root);

        Element oldRoot = new() { Name = "oldRoot" };
        Element oldLeaf = new() { Name = "oldLeaf" };
        oldRoot.Children.Add(oldLeaf);
        context.AddRoot(oldRoot);

        UiContext other = NewContext();
        Element otherRoot = new() { Name = "otherRoot" };
        Element otherLeaf = new() { Name = "otherLeaf" };
        otherRoot.Children.Add(otherLeaf);
        other.AddRoot(otherRoot);

        EventRecorder recorder = new();
        foreach (Element element in new[] { root, kept, removed, removedLeaf, oldRoot, oldLeaf, otherRoot, otherLeaf })
        {
            recorder.Register(element);
        }

        // A removed subtree, a removed root, and an element of another context.
        root.Children.Remove(removed);
        Assert.True(context.RemoveRoot(oldRoot));

        context.PushEvent(Pointer(UiEventType.PointerDown, 1, removedLeaf));
        context.PushEvent(Pointer(UiEventType.PointerDown, 2, removed));
        context.PushEvent(Pointer(UiEventType.PointerDown, 3, oldLeaf));
        context.PushEvent(Pointer(UiEventType.PointerDown, 4, otherLeaf));
        context.PushEvent(Pointer(UiEventType.PointerDown, 5, kept));
        Assert.Equal(5, context.QueuedEventCount);

        int walks = context.DrainEvents();

        Assert.Equal(1, walks);
        Assert.Equal(0, context.QueuedEventCount);

        // The walk of "kept": root capture, kept capture and bubble (target), root bubble.
        Assert.Equal(4, recorder.Count);
        Assert.Same(root, recorder[0].CurrentTarget);
        Assert.Same(kept, recorder[1].CurrentTarget);
        Assert.Same(kept, recorder[2].CurrentTarget);
        Assert.Same(root, recorder[3].CurrentTarget);
    }

    /// <summary>
    /// A handler removes the middle element during the capture phase of the root. The path stays as the walk copied it,
    /// so the middle handler and the leaf handler still run (A3).
    /// </summary>
    [Fact]
    public void ATreeChangeDuringTheWalkKeepsThePath()
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

        root.OnPointerDownCapture = (ref PointerEvent e) =>
        {
            recorder.PointerCapture(ref e);
            root.Children.Remove(middle);
        };

        context.PushEvent(Pointer(UiEventType.PointerDown, 1, leaf));
        Assert.Equal(1, context.DrainEvents());

        Assert.Null(middle.Parent);
        Assert.Equal(6, recorder.Count);
        Assert.Same(root, recorder[0].CurrentTarget);
        Assert.Equal(UiEventPhase.Capture, recorder[0].Phase);
        Assert.Same(middle, recorder[1].CurrentTarget);
        Assert.Equal(UiEventPhase.Capture, recorder[1].Phase);
        Assert.Same(leaf, recorder[2].CurrentTarget);
        Assert.Equal(UiEventPhase.Target, recorder[2].Phase);
        Assert.Same(leaf, recorder[3].CurrentTarget);
        Assert.Same(middle, recorder[4].CurrentTarget);
        Assert.Equal(UiEventPhase.Bubble, recorder[4].Phase);
        Assert.Same(root, recorder[5].CurrentTarget);
        Assert.Equal(UiEventPhase.Bubble, recorder[5].Phase);
    }

    /// <summary>
    /// A chain of 40 ancestors and one leaf is deeper than the start path buffer of 32. The walk grows the buffer and
    /// visits the elements in order: 40 capture records, 2 target records, and 40 bubble records.
    /// </summary>
    [Fact]
    public void ATreeDeeperThanThePathBufferWalksInOrder()
    {
        const int ancestors = 40;
        Element[] chain = new Element[ancestors + 1];
        for (int i = 0; i <= ancestors; i++)
        {
            chain[i] = new Element { Name = "level" + i };
            if (i > 0)
            {
                chain[i - 1].Children.Add(chain[i]);
            }
        }

        UiContext context = NewContext();
        context.AddRoot(chain[0]);

        EventRecorder recorder = new();
        for (int i = 0; i <= ancestors; i++)
        {
            recorder.Register(chain[i]);
        }

        Assert.True(ancestors + 1 > EventWalk.InitialPathSize);

        context.PushEvent(Pointer(UiEventType.PointerDown, 1, chain[ancestors]));
        Assert.Equal(1, context.DrainEvents());

        Assert.False(recorder.Overflow);
        Assert.Equal((2 * ancestors) + 2, recorder.Count);

        for (int i = 0; i < ancestors; i++)
        {
            Assert.Same(chain[i], recorder[i].CurrentTarget);
            Assert.Equal(UiEventPhase.Capture, recorder[i].Phase);
            Assert.True(recorder[i].CaptureSlot);
        }

        Assert.Same(chain[ancestors], recorder[ancestors].CurrentTarget);
        Assert.Equal(UiEventPhase.Target, recorder[ancestors].Phase);
        Assert.True(recorder[ancestors].CaptureSlot);
        Assert.Same(chain[ancestors], recorder[ancestors + 1].CurrentTarget);
        Assert.Equal(UiEventPhase.Target, recorder[ancestors + 1].Phase);
        Assert.False(recorder[ancestors + 1].CaptureSlot);

        for (int i = 0; i < ancestors; i++)
        {
            EventRecord record = recorder[ancestors + 2 + i];
            Assert.Same(chain[ancestors - 1 - i], record.CurrentTarget);
            Assert.Equal(UiEventPhase.Bubble, record.Phase);
            Assert.False(record.CaptureSlot);
        }

        // The buffer grew to hold the chain and holds no element reference after the walk.
        Assert.True(context.EventPathLength >= ancestors + 1);
        AssertPathClean(context.EventPathSlots);
    }

    /// <summary>
    /// A handler that calls the drain of its own context gets an <see cref="InvalidOperationException"/>. When the
    /// handler does not catch it, the outer drain leaves with it. In both cases the next drain runs.
    /// </summary>
    [Fact]
    public void ANestedDrainThrows()
    {
        UiContext context = NewContext();
        Element root = new() { Name = "root" };
        context.AddRoot(root);

        // Case 1: the handler catches the exception and records it.
        InvalidOperationException? caught = null;
        int calls = 0;
        root.OnPointerDown = (ref PointerEvent e) =>
        {
            calls++;
            try
            {
                context.DrainEvents();
            }
            catch (InvalidOperationException exception)
            {
                caught = exception;
            }
        };

        context.PushEvent(Pointer(UiEventType.PointerDown, 1, root));
        Assert.Equal(1, context.DrainEvents());
        Assert.Equal(1, calls);
        Assert.NotNull(caught);
        Assert.Contains("runs already", caught!.Message, StringComparison.Ordinal);

        // Case 2: the handler does not catch it. The outer drain leaves with the exception.
        root.OnPointerDown = (ref PointerEvent e) =>
        {
            calls++;
            context.DrainEvents();
        };

        context.PushEvent(Pointer(UiEventType.PointerDown, 2, root));
        InvalidOperationException escaped = Assert.Throws<InvalidOperationException>(() => context.DrainEvents());
        Assert.Contains("runs already", escaped.Message, StringComparison.Ordinal);
        Assert.Equal(2, calls);

        // The flag is clear, so a later drain runs.
        root.OnPointerDown = (ref PointerEvent e) => calls++;
        context.PushEvent(Pointer(UiEventType.PointerDown, 3, root));
        Assert.Equal(1, context.DrainEvents());
        Assert.Equal(3, calls);
    }

    /// <summary>
    /// A handler throws. The exception leaves the drain, the entries that the drain did not pop stay in the ring, the
    /// next drain runs, and neither the path buffer nor a popped ring slot holds an element reference.
    /// </summary>
    [Fact]
    public void AHandlerExceptionLeavesTheQueueAndThePathClean()
    {
        UiContext context = NewContext();
        Element root = new() { Name = "root" };
        Element leaf = new() { Name = "leaf" };
        root.Children.Add(leaf);
        context.AddRoot(root);

        bool shouldThrow = true;
        int calls = 0;
        leaf.OnPointerDown = (ref PointerEvent e) =>
        {
            calls++;
            if (shouldThrow)
            {
                throw new FormatException("boom");
            }
        };

        // Three events: the first throws. The ring holds 256 entries, so the three entries sit in slots 0 to 2.
        context.PushEvent(Pointer(UiEventType.PointerDown, 1, leaf));
        context.PushEvent(Pointer(UiEventType.PointerDown, 2, leaf));
        context.PushEvent(Pointer(UiEventType.PointerDown, 3, leaf));

        FormatException exception = Assert.Throws<FormatException>(() => context.DrainEvents());
        Assert.Equal("boom", exception.Message);
        Assert.Equal(1, calls);
        Assert.Equal(2, context.QueuedEventCount);

        AssertPathClean(context.EventPathSlots);

        // The popped slot holds no element reference. The two waiting slots still hold their targets.
        ReadOnlySpan<UiQueuedEvent> items = context.EventRingSlots;
        Assert.Null(items[0].Header.Target);
        Assert.Same(leaf, items[1].Header.Target);
        Assert.Same(leaf, items[2].Header.Target);

        // The flag is clear, so the next drain runs and walks the two waiting events.
        shouldThrow = false;
        Assert.Equal(2, context.DrainEvents());
        Assert.Equal(3, calls);
        Assert.Equal(0, context.QueuedEventCount);

        AssertPathClean(context.EventPathSlots);
        AssertRingClean(context);
    }

    private static void AssertPathClean(ReadOnlySpan<Element?> path)
    {
        for (int i = 0; i < path.Length; i++)
        {
            Assert.Null(path[i]);
        }
    }

    private static void AssertRingClean(UiContext context)
    {
        ReadOnlySpan<UiQueuedEvent> items = context.EventRingSlots;
        for (int i = 0; i < items.Length; i++)
        {
            Assert.Null(items[i].Header.Target);
        }
    }

    private static UiContext NewContext()
    {
        return new UiContext(new UiContext.CreateInfo { Measurer = new RecordingRenderer() });
    }

    private static PointerEvent Pointer(UiEventType type, double timestamp, Element? target)
    {
        return new PointerEvent
        {
            Header = new UiEventHeader(type, timestamp, target),
            Position = new float2(10, 20),
            Button = PointerButton.Left,
            Modifiers = UiModifiers.None,
        };
    }
}
