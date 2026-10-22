using System;
using System.Collections.Generic;
using Brutal.Numerics;
using Xunit;

namespace BrutalUi.Core.Tests;

/// <summary>
/// Tests of the event ring of <see cref="UiContext"/>: the drop of the oldest entry, the capacity, the checks of the
/// push, and the rules of the drain.
/// </summary>
public sealed class EventQueueTests
{
    /// <summary>
    /// The demo of phase 4: a ring with 4 entries gets 6 pointer pushes. The ring drops events 1 and 2. The drain walks
    /// events 3 to 6 in order.
    /// </summary>
    [Fact]
    public void AFullQueueDropsTheOldestEvent()
    {
        UiContext context = NewContext(capacity: 4);
        Element root = new() { Name = "root" };
        context.AddRoot(root);

        List<double> timestamps = [];
        root.OnPointerDown = (ref PointerEvent e) => timestamps.Add(e.Header.Timestamp);

        for (int i = 1; i <= 6; i++)
        {
            context.PushEvent(Pointer(UiEventType.PointerDown, i, root));
        }

        Assert.Equal(4, context.QueuedEventCount);
        Assert.Equal(2, context.DroppedEventCount);

        int walks = context.DrainEvents();

        Assert.Equal(4, walks);
        Assert.Equal(new double[] { 3, 4, 5, 6 }, timestamps);
        Assert.Equal(0, context.QueuedEventCount);
        Assert.Equal(2, context.DroppedEventCount);
    }

    /// <summary>The drop count grows by 1 for each push into a full ring, and a drain does not reset it.</summary>
    [Fact]
    public void TheDropCountGrowsForEachDrop()
    {
        UiContext context = NewContext(capacity: 2);
        Element root = new() { Name = "root" };
        context.AddRoot(root);

        Assert.Equal(0, context.DroppedEventCount);

        context.PushEvent(Pointer(UiEventType.PointerMove, 1, root));
        context.PushEvent(Pointer(UiEventType.PointerMove, 2, root));
        Assert.Equal(0, context.DroppedEventCount);

        for (int i = 1; i <= 5; i++)
        {
            context.PushEvent(Pointer(UiEventType.PointerMove, 2 + i, root));
            Assert.Equal(i, context.DroppedEventCount);
            Assert.Equal(2, context.QueuedEventCount);
        }

        context.DrainEvents();
        Assert.Equal(5, context.DroppedEventCount);
        Assert.Equal(0, context.QueuedEventCount);
    }

    /// <summary>Both constructors give a ring of 256 entries. A <c>CreateInfo</c> capacity of 0 means the default.</summary>
    [Fact]
    public void TheDefaultCapacityIs256()
    {
        Assert.Equal(256, UiContext.DefaultEventQueueCapacity);

        UiContext fromParameterless = new();
        Assert.Equal(256, fromParameterless.EventQueueCapacity);

        UiContext fromCreateInfo = new(new UiContext.CreateInfo { Measurer = new RecordingRenderer(), EventQueueCapacity = 0 });
        Assert.Equal(256, fromCreateInfo.EventQueueCapacity);

        Element root = new() { Name = "root" };
        fromCreateInfo.AddRoot(root);
        for (int i = 0; i < 257; i++)
        {
            fromCreateInfo.PushEvent(Pointer(UiEventType.PointerMove, i, root));
        }

        Assert.Equal(256, fromCreateInfo.QueuedEventCount);
        Assert.Equal(1, fromCreateInfo.DroppedEventCount);
    }

    /// <summary>A capacity in the <c>CreateInfo</c> sets the size of the ring.</summary>
    [Fact]
    public void ACapacityFromTheCreateInfoSizesTheRing()
    {
        UiContext context = NewContext(capacity: 4);
        Assert.Equal(4, context.EventQueueCapacity);

        UiContext large = NewContext(capacity: 1000);
        Assert.Equal(1000, large.EventQueueCapacity);

        Element root = new() { Name = "root" };
        large.AddRoot(root);
        for (int i = 0; i < 1000; i++)
        {
            large.PushEvent(Pointer(UiEventType.PointerMove, i, root));
        }

        Assert.Equal(1000, large.QueuedEventCount);
        Assert.Equal(0, large.DroppedEventCount);
    }

    /// <summary>A negative capacity throws and names the <c>info</c> parameter.</summary>
    [Fact]
    public void ANegativeCapacityThrows()
    {
        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(() =>
            new UiContext(new UiContext.CreateInfo { Measurer = new RecordingRenderer(), EventQueueCapacity = -1 }));

        Assert.Equal("info", exception.ParamName);
        Assert.Equal(-1, exception.ActualValue);
        Assert.Contains("must not be negative", exception.Message, StringComparison.Ordinal);
    }

    /// <summary>
    /// Each push method rejects a type that its struct does not carry, also <c>None</c> and an undefined value. A
    /// rejected push leaves the ring empty.
    /// </summary>
    [Fact]
    public void APushWithAWrongTypeThrows()
    {
        UiContext context = NewContext(capacity: 4);
        Element root = new() { Name = "root" };
        context.AddRoot(root);

        UiEventType[] pointerWrong = [UiEventType.Scroll, UiEventType.KeyDown, UiEventType.KeyUp, UiEventType.Char, UiEventType.None, (UiEventType)99];
        UiEventType[] scrollWrong = [UiEventType.PointerMove, UiEventType.PointerDown, UiEventType.PointerUp, UiEventType.KeyDown, UiEventType.KeyUp, UiEventType.Char, UiEventType.None];
        UiEventType[] keyWrong = [UiEventType.PointerMove, UiEventType.PointerDown, UiEventType.PointerUp, UiEventType.Scroll, UiEventType.Char, UiEventType.None];
        UiEventType[] charWrong = [UiEventType.PointerMove, UiEventType.PointerDown, UiEventType.PointerUp, UiEventType.Scroll, UiEventType.KeyDown, UiEventType.KeyUp, UiEventType.None];

        foreach (UiEventType type in pointerWrong)
        {
            PointerEvent e = Pointer(type, 1, root);
            ArgumentException exception = Assert.Throws<ArgumentException>(() => context.PushEvent(e));
            Assert.Equal("e", exception.ParamName);
            Assert.Contains("A PointerEvent cannot carry the type", exception.Message, StringComparison.Ordinal);
        }

        foreach (UiEventType type in scrollWrong)
        {
            ScrollEvent e = new() { Header = new UiEventHeader(type, 1, root) };
            ArgumentException exception = Assert.Throws<ArgumentException>(() => context.PushEvent(e));
            Assert.Equal("e", exception.ParamName);
            Assert.Contains("A ScrollEvent cannot carry the type", exception.Message, StringComparison.Ordinal);
        }

        foreach (UiEventType type in keyWrong)
        {
            KeyEvent e = new() { Header = new UiEventHeader(type, 1, root) };
            ArgumentException exception = Assert.Throws<ArgumentException>(() => context.PushEvent(e));
            Assert.Equal("e", exception.ParamName);
            Assert.Contains("A KeyEvent cannot carry the type", exception.Message, StringComparison.Ordinal);
        }

        foreach (UiEventType type in charWrong)
        {
            CharEvent e = new() { Header = new UiEventHeader(type, 1, root) };
            ArgumentException exception = Assert.Throws<ArgumentException>(() => context.PushEvent(e));
            Assert.Equal("e", exception.ParamName);
            Assert.Contains("A CharEvent cannot carry the type", exception.Message, StringComparison.Ordinal);
        }

        // The hints name the right struct, and None gets the generic hint.
        ArgumentException hint = Assert.Throws<ArgumentException>(() => context.PushEvent(Pointer(UiEventType.Scroll, 1, root)));
        Assert.Contains("Use a ScrollEvent.", hint.Message, StringComparison.Ordinal);
        hint = Assert.Throws<ArgumentException>(() => context.PushEvent(Pointer(UiEventType.None, 1, root)));
        Assert.Contains("Set a valid type in the event header.", hint.Message, StringComparison.Ordinal);

        Assert.Equal(0, context.QueuedEventCount);
        Assert.Equal(0, context.DroppedEventCount);
    }

    /// <summary>The drain pops an event with no target and sends it to no walk.</summary>
    [Fact]
    public void ADrainSkipsAnEventWithNoTarget()
    {
        UiContext context = NewContext(capacity: 4);
        Element root = new() { Name = "root" };
        context.AddRoot(root);

        EventRecorder recorder = new();
        recorder.Register(root);

        context.PushEvent(Pointer(UiEventType.PointerDown, 1, null));
        context.PushEvent(Pointer(UiEventType.PointerDown, 2, root));
        Assert.Equal(2, context.QueuedEventCount);

        int walks = context.DrainEvents();

        Assert.Equal(1, walks);
        Assert.Equal(0, context.QueuedEventCount);
        Assert.Equal(2, recorder.Count);
    }

    /// <summary>
    /// A handler pushes an event during the drain. The event stays in the ring until the next drain (Decision 6).
    /// </summary>
    [Fact]
    public void AnEventFromAHandlerWaitsForTheNextDrain()
    {
        UiContext context = NewContext(capacity: 4);
        Element root = new() { Name = "root" };
        context.AddRoot(root);

        List<double> timestamps = [];
        root.OnPointerDown = (ref PointerEvent e) =>
        {
            timestamps.Add(e.Header.Timestamp);
            if (e.Header.Timestamp < 2)
            {
                context.PushEvent(Pointer(UiEventType.PointerDown, 2, root));
            }
        };

        context.PushEvent(Pointer(UiEventType.PointerDown, 1, root));

        Assert.Equal(1, context.DrainEvents());
        Assert.Equal(new double[] { 1 }, timestamps);
        Assert.Equal(1, context.QueuedEventCount);

        Assert.Equal(1, context.DrainEvents());
        Assert.Equal(new double[] { 1, 2 }, timestamps);
        Assert.Equal(0, context.QueuedEventCount);
    }

    /// <summary>
    /// A handler pushes into a full ring during the drain. The push drops an entry of the current drain that did not
    /// walk yet. The drain does not walk the events of the handler. They wait for the next drain (Decision 6).
    /// </summary>
    [Fact]
    public void AHandlerPushIntoAFullRingWaitsForTheNextDrain()
    {
        UiContext context = NewContext(capacity: 4);
        Element root = new() { Name = "root" };
        context.AddRoot(root);

        List<double> timestamps = [];
        root.OnPointerDown = (ref PointerEvent e) =>
        {
            timestamps.Add(e.Header.Timestamp);
            if (e.Header.Timestamp == 1)
            {
                context.PushEvent(Pointer(UiEventType.PointerDown, 10, root));
                context.PushEvent(Pointer(UiEventType.PointerDown, 11, root));
            }
        };

        for (int i = 1; i <= 4; i++)
        {
            context.PushEvent(Pointer(UiEventType.PointerDown, i, root));
        }

        int walks = context.DrainEvents();

        Assert.Equal(3, walks);
        Assert.Equal(new double[] { 1, 3, 4 }, timestamps);
        Assert.Equal(2, context.QueuedEventCount);
        Assert.Equal(1, context.DroppedEventCount);

        Assert.Equal(2, context.DrainEvents());
        Assert.Equal(new double[] { 1, 3, 4, 10, 11 }, timestamps);
        Assert.Equal(0, context.QueuedEventCount);
    }

    private static UiContext NewContext(int capacity)
    {
        return new UiContext(new UiContext.CreateInfo { Measurer = new RecordingRenderer(), EventQueueCapacity = capacity });
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
