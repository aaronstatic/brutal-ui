using Brutal.Numerics;
using Xunit;

namespace BrutalUi.Core.Tests;

/// <summary>
/// Tests of the four event structs: the push of each kind, the slots that each kind reaches, and the payload
/// that survives the ring.
/// </summary>
/// <remarks>
/// Each test builds a 3-deep tree (root, middle, leaf), adds the root to a context, pushes hand-made events, and
/// drains the ring. An <see cref="EventRecorder"/> records the handler calls.
/// </remarks>
public sealed class EventKindTests
{
    private const double Timestamp = 2.25;

    /// <summary>
    /// Each type reaches only the slots of its own type. All 14 slots are set on each element, but only the 6
    /// calls of the pushed type happen.
    /// </summary>
    /// <param name="type">The type to push.</param>
    [Theory]
    [InlineData(UiEventType.PointerMove)]
    [InlineData(UiEventType.PointerDown)]
    [InlineData(UiEventType.PointerUp)]
    [InlineData(UiEventType.Scroll)]
    [InlineData(UiEventType.KeyDown)]
    [InlineData(UiEventType.KeyUp)]
    [InlineData(UiEventType.Char)]
    public void EachKindReachesOnlyItsOwnSlots(UiEventType type)
    {
        Tree tree = new();
        EventRecorder recorder = tree.Register();

        switch (type)
        {
            case UiEventType.PointerMove:
            case UiEventType.PointerDown:
            case UiEventType.PointerUp:
                tree.Context.PushEvent(Pointer(type, tree.Leaf));
                break;
            case UiEventType.Scroll:
                tree.Context.PushEvent(Scroll(tree.Leaf));
                break;
            case UiEventType.KeyDown:
            case UiEventType.KeyUp:
                tree.Context.PushEvent(Key(type, tree.Leaf));
                break;
            default:
                tree.Context.PushEvent(Char(tree.Leaf));
                break;
        }

        Assert.Equal(1, tree.Context.DrainEvents());
        Assert.False(recorder.Overflow);
        Assert.Equal(6, recorder.Count);
        AssertRecord(recorder[0], tree.Root, UiEventPhase.Capture, type, captureSlot: true);
        AssertRecord(recorder[1], tree.Middle, UiEventPhase.Capture, type, captureSlot: true);
        AssertRecord(recorder[2], tree.Leaf, UiEventPhase.Target, type, captureSlot: true);
        AssertRecord(recorder[3], tree.Leaf, UiEventPhase.Target, type, captureSlot: false);
        AssertRecord(recorder[4], tree.Middle, UiEventPhase.Bubble, type, captureSlot: false);
        AssertRecord(recorder[5], tree.Root, UiEventPhase.Bubble, type, captureSlot: false);
    }

    /// <summary>A pointer event keeps its position, button, modifiers, timestamp, and target through the ring.</summary>
    [Fact]
    public void PointerPayloadSurvivesTheQueue()
    {
        Tree tree = new();
        EventRecorder recorder = tree.Register();

        tree.Context.PushEvent(new PointerEvent
        {
            Header = new UiEventHeader(UiEventType.PointerDown, Timestamp, tree.Leaf),
            Position = new float2(12.5f, -3.25f),
            Button = PointerButton.Middle,
            Modifiers = UiModifiers.Shift | UiModifiers.Alt,
        });
        Assert.Equal(1, tree.Context.DrainEvents());

        PointerEvent last = recorder.LastPointer;
        Assert.Equal(UiEventType.PointerDown, last.Header.Type);
        Assert.Equal(Timestamp, last.Header.Timestamp);
        Assert.Same(tree.Leaf, last.Header.Target);
        Assert.Equal(12.5f, last.Position.X);
        Assert.Equal(-3.25f, last.Position.Y);
        Assert.Equal(PointerButton.Middle, last.Button);
        Assert.Equal(UiModifiers.Shift | UiModifiers.Alt, last.Modifiers);
    }

    /// <summary>A scroll event keeps its position, delta, modifiers, timestamp, and target through the ring.</summary>
    [Fact]
    public void ScrollPayloadSurvivesTheQueue()
    {
        Tree tree = new();
        EventRecorder recorder = tree.Register();

        tree.Context.PushEvent(new ScrollEvent
        {
            Header = new UiEventHeader(UiEventType.Scroll, Timestamp, tree.Leaf),
            Position = new float2(40.5f, 8.75f),
            Delta = new float2(-1.5f, 3f),
            Modifiers = UiModifiers.Control | UiModifiers.CapsLock,
        });
        Assert.Equal(1, tree.Context.DrainEvents());

        ScrollEvent last = recorder.LastScroll;
        Assert.Equal(UiEventType.Scroll, last.Header.Type);
        Assert.Equal(Timestamp, last.Header.Timestamp);
        Assert.Same(tree.Leaf, last.Header.Target);
        Assert.Equal(40.5f, last.Position.X);
        Assert.Equal(8.75f, last.Position.Y);
        Assert.Equal(-1.5f, last.Delta.X);
        Assert.Equal(3f, last.Delta.Y);
        Assert.Equal(UiModifiers.Control | UiModifiers.CapsLock, last.Modifiers);
    }

    /// <summary>A key event keeps its key, scan code, modifiers, repeat flag, timestamp, and target through the ring.</summary>
    [Fact]
    public void KeyPayloadSurvivesTheQueue()
    {
        Tree tree = new();
        EventRecorder recorder = tree.Register();

        tree.Context.PushEvent(new KeyEvent
        {
            Header = new UiEventHeader(UiEventType.KeyDown, Timestamp, tree.Leaf),
            Key = 65,
            ScanCode = 38,
            Modifiers = UiModifiers.Super | UiModifiers.NumLock,
            IsRepeat = true,
        });
        Assert.Equal(1, tree.Context.DrainEvents());

        KeyEvent last = recorder.LastKey;
        Assert.Equal(UiEventType.KeyDown, last.Header.Type);
        Assert.Equal(Timestamp, last.Header.Timestamp);
        Assert.Same(tree.Leaf, last.Header.Target);
        Assert.Equal(65, last.Key);
        Assert.Equal(38, last.ScanCode);
        Assert.Equal(UiModifiers.Super | UiModifiers.NumLock, last.Modifiers);
        Assert.True(last.IsRepeat);

        // A key up with no repeat flag keeps the flag clear.
        tree.Context.PushEvent(new KeyEvent
        {
            Header = new UiEventHeader(UiEventType.KeyUp, Timestamp, tree.Leaf),
            Key = 66,
            ScanCode = 56,
            Modifiers = UiModifiers.None,
            IsRepeat = false,
        });
        Assert.Equal(1, tree.Context.DrainEvents());
        Assert.Equal(UiEventType.KeyUp, recorder.LastKey.Header.Type);
        Assert.Equal(66, recorder.LastKey.Key);
        Assert.False(recorder.LastKey.IsRepeat);
    }

    /// <summary>A char event keeps its code point, timestamp, and target through the ring.</summary>
    [Fact]
    public void CharPayloadSurvivesTheQueue()
    {
        Tree tree = new();
        EventRecorder recorder = tree.Register();

        tree.Context.PushEvent(new CharEvent
        {
            Header = new UiEventHeader(UiEventType.Char, Timestamp, tree.Leaf),
            CodePoint = 0x1F600,
        });
        Assert.Equal(1, tree.Context.DrainEvents());

        CharEvent last = recorder.LastChar;
        Assert.Equal(UiEventType.Char, last.Header.Type);
        Assert.Equal(Timestamp, last.Header.Timestamp);
        Assert.Same(tree.Leaf, last.Header.Target);
        Assert.Equal(0x1F600u, last.CodePoint);
    }

    /// <summary>
    /// The demo of phase 3: a key down at the leaf of a 3-deep tree. The walk runs capture (root, middle), target
    /// (leaf: the capture slot, then the bubble slot), and bubble (middle, root).
    /// </summary>
    [Fact]
    public void AKeyDownWalksCaptureTargetBubble()
    {
        Tree tree = new();
        EventRecorder recorder = tree.Register();

        tree.Context.PushEvent(Key(UiEventType.KeyDown, tree.Leaf));
        int walks = tree.Context.DrainEvents();

        Assert.Equal(1, walks);
        Assert.False(recorder.Overflow);
        Assert.Equal(6, recorder.Count);
        AssertRecord(recorder[0], tree.Root, UiEventPhase.Capture, UiEventType.KeyDown, captureSlot: true);
        AssertRecord(recorder[1], tree.Middle, UiEventPhase.Capture, UiEventType.KeyDown, captureSlot: true);
        AssertRecord(recorder[2], tree.Leaf, UiEventPhase.Target, UiEventType.KeyDown, captureSlot: true);
        AssertRecord(recorder[3], tree.Leaf, UiEventPhase.Target, UiEventType.KeyDown, captureSlot: false);
        AssertRecord(recorder[4], tree.Middle, UiEventPhase.Bubble, UiEventType.KeyDown, captureSlot: false);
        AssertRecord(recorder[5], tree.Root, UiEventPhase.Bubble, UiEventType.KeyDown, captureSlot: false);
        Assert.Equal(65, recorder.LastKey.Key);
    }

    private static PointerEvent Pointer(UiEventType type, Element target)
    {
        return new PointerEvent
        {
            Header = new UiEventHeader(type, Timestamp, target),
            Position = new float2(10, 20),
            Button = PointerButton.Left,
            Modifiers = UiModifiers.None,
        };
    }

    private static ScrollEvent Scroll(Element target)
    {
        return new ScrollEvent
        {
            Header = new UiEventHeader(UiEventType.Scroll, Timestamp, target),
            Position = new float2(10, 20),
            Delta = new float2(0, 1),
            Modifiers = UiModifiers.None,
        };
    }

    private static KeyEvent Key(UiEventType type, Element target)
    {
        return new KeyEvent
        {
            Header = new UiEventHeader(type, Timestamp, target),
            Key = 65,
            ScanCode = 38,
            Modifiers = UiModifiers.None,
            IsRepeat = false,
        };
    }

    private static CharEvent Char(Element target)
    {
        return new CharEvent
        {
            Header = new UiEventHeader(UiEventType.Char, Timestamp, target),
            CodePoint = 'a',
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

    /// <summary>A context with a 3-deep tree: root, middle, and leaf.</summary>
    private sealed class Tree
    {
        public Tree()
        {
            Root = new Element { Name = "root" };
            Middle = new Element { Name = "middle" };
            Leaf = new Element { Name = "leaf" };
            Root.Children.Add(Middle);
            Middle.Children.Add(Leaf);
            Context = new UiContext(new UiContext.CreateInfo { Measurer = new RecordingRenderer() });
            Context.AddRoot(Root);
        }

        public Element Root { get; }

        public Element Middle { get; }

        public Element Leaf { get; }

        public UiContext Context { get; }

        public EventRecorder Register()
        {
            EventRecorder recorder = new();
            recorder.Register(Root);
            recorder.Register(Middle);
            recorder.Register(Leaf);
            return recorder;
        }
    }
}
