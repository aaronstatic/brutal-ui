using System;
using Brutal.Numerics;
using Xunit;

namespace BrutalUi.Core.Tests;

/// <summary>
/// The allocation check of the event model. After warm-up, a push, a drain, and a walk of each event kind must
/// allocate 0 bytes.
/// </summary>
/// <remarks>
/// <para>
/// The scene: a root, a chain of 40 elements under the root, and a side branch of 2 elements under the root. The
/// path of the deepest leaf holds 41 elements, which is more than <see cref="EventWalk.InitialPathSize"/>. The first
/// step grows the path buffer, so the warm-up covers the growth (A7). Each element registers all 14 slots. One slot
/// is a multicast slot, and one slot stops the event. A detached element with one child is not in the context. It is
/// the target of one event, so the drain measures the false path of the root tree check.
/// </para>
/// <para>
/// Each step pushes 11 events into a ring of 9 entries, so the ring drops the 2 oldest entries in each step. The
/// order of the pushes, from the oldest to the newest:
/// </para>
/// <list type="number">
/// <item>An extra pointer move at the deepest leaf. The ring drops it.</item>
/// <item>A pointer up at the deepest leaf. The ring drops it.</item>
/// <item>A pointer move at the deepest leaf: 82 calls.</item>
/// <item>A pointer down at the deepest leaf: 82 calls and 1 call of the multicast slot, so 83.</item>
/// <item>A scroll at the deepest leaf: 82 calls.</item>
/// <item>A key down at the deepest leaf: 82 calls.</item>
/// <item>A key up at the deepest leaf: 21 calls. The capture slot of level 20 stops the event.</item>
/// <item>A char at the deepest leaf: 82 calls.</item>
/// <item>A pointer down at the side branch: 6 calls.</item>
/// <item>A pointer down at the detached leaf: 0 calls. The target is outside the tree, so the drain skips it.</item>
/// <item>A char with no target: 0 calls. The drain skips it.</item>
/// </list>
/// <para>
/// The 82 calls of a full walk: 40 capture calls (the root and levels 1 to 39), 2 calls at the target (level 40),
/// and 40 bubble calls (levels 39 to 1 and the root). The stopped key up walk calls the root and levels 1 to 19, and
/// then the stop handler of level 20, so 21 calls. The first delegate of the multicast slot counts as a pointer call
/// and the second delegate counts as an extra call. The pointer calls are 82 + 82 + 6 = 170, the extra call is 1, the
/// scroll calls are 82, the key calls are 82 + 21 = 103, and the char calls are 82. The total is 438 calls for each
/// step, in 7 walks.
/// </para>
/// </remarks>
public sealed class EventAllocationTests
{
    private const int ChainLength = 40;
    private const int StopLevel = 20;
    private const int MulticastLevel = 10;
    private const int QueueCapacity = 9;
    private const int Iterations = 100;
    private const int PushCount = 1000;

    private const int CallsOfStep = 438;
    private const int WalksOfStep = 7;
    private const int DropsOfStep = 2;

    /// <summary>Counts the handler calls. Its methods are the targets of the delegates.</summary>
    private sealed class Counter
    {
        public int PointerCalls;
        public int ScrollCalls;
        public int KeyCalls;
        public int CharCalls;
        public int ExtraCalls;
        public int Stops;

        public void CountPointer(ref PointerEvent e) => PointerCalls++;

        public void CountPointerAgain(ref PointerEvent e) => ExtraCalls++;

        public void CountScroll(ref ScrollEvent e) => ScrollCalls++;

        public void CountKey(ref KeyEvent e) => KeyCalls++;

        public void CountChar(ref CharEvent e) => CharCalls++;

        public void StopKey(ref KeyEvent e)
        {
            KeyCalls++;
            Stops++;
            e.Header.StopPropagation();
        }

        public int Total => PointerCalls + ScrollCalls + KeyCalls + CharCalls + ExtraCalls;
    }

    /// <summary>The tree and the values that the measured body uses. The test builds all of it before the measure.</summary>
    private sealed class Scene
    {
        public required UiContext Context { get; init; }

        public required Counter Counter { get; init; }

        public required PointerEvent ExtraMove;
        public required PointerEvent LeafUp;
        public required PointerEvent LeafMove;
        public required PointerEvent LeafDown;
        public required ScrollEvent LeafScroll;
        public required KeyEvent LeafKeyDown;
        public required KeyEvent LeafKeyUp;
        public required CharEvent LeafChar;
        public required PointerEvent SideDown;
        public required PointerEvent OrphanDown;
        public required CharEvent NullChar;

        /// <summary>The number of walks of the last drain.</summary>
        public int LastWalks;
    }

    private static Scene BuildScene()
    {
        Counter counter = new();
        UiEventHandler<PointerEvent> pointer = counter.CountPointer;
        UiEventHandler<PointerEvent> pointerAgain = counter.CountPointerAgain;
        UiEventHandler<ScrollEvent> scroll = counter.CountScroll;
        UiEventHandler<KeyEvent> key = counter.CountKey;
        UiEventHandler<KeyEvent> stopKey = counter.StopKey;
        UiEventHandler<CharEvent> character = counter.CountChar;

        Element root = new() { Name = "root" };
        Register(root, pointer, scroll, key, character);

        Element[] chain = new Element[ChainLength + 1];
        chain[0] = root;
        for (int i = 1; i <= ChainLength; i++)
        {
            Element element = new() { Name = "level" + i };
            Register(element, pointer, scroll, key, character);
            chain[i - 1].Children.Add(element);
            chain[i] = element;
        }

        Element side = new() { Name = "side" };
        Register(side, pointer, scroll, key, character);
        root.Children.Add(side);
        Element sideLeaf = new() { Name = "side-leaf" };
        Register(sideLeaf, pointer, scroll, key, character);
        side.Children.Add(sideLeaf);

        // A detached element with a child. The context does not hold it, so its leaf is outside the tree.
        Element orphan = new() { Name = "orphan" };
        Register(orphan, pointer, scroll, key, character);
        Element orphanLeaf = new() { Name = "orphan-leaf" };
        Register(orphanLeaf, pointer, scroll, key, character);
        orphan.Children.Add(orphanLeaf);

        // A multicast slot: the bubble slot of level 10 for a pointer down holds two delegates.
        chain[MulticastLevel].OnPointerDown = (UiEventHandler<PointerEvent>)Delegate.Combine(pointer, pointerAgain);

        // A stop handler: the capture slot of level 20 for a key up.
        chain[StopLevel].OnKeyUpCapture = stopKey;

        UiContext context = new(new UiContext.CreateInfo
        {
            Measurer = new RecordingRenderer(),
            EventQueueCapacity = QueueCapacity,
        });
        context.AddRoot(root);
        context.Update(0f);

        Element leaf = chain[ChainLength];

        return new Scene
        {
            Context = context,
            Counter = counter,
            ExtraMove = MakePointer(UiEventType.PointerMove, leaf),
            LeafUp = MakePointer(UiEventType.PointerUp, leaf),
            LeafMove = MakePointer(UiEventType.PointerMove, leaf),
            LeafDown = MakePointer(UiEventType.PointerDown, leaf),
            LeafScroll = new ScrollEvent
            {
                Header = new UiEventHeader(UiEventType.Scroll, 1.5, leaf),
                Position = new float2(10, 20),
                Delta = new float2(0, -1),
                Modifiers = UiModifiers.None,
            },
            LeafKeyDown = MakeKey(UiEventType.KeyDown, leaf),
            LeafKeyUp = MakeKey(UiEventType.KeyUp, leaf),
            LeafChar = new CharEvent
            {
                Header = new UiEventHeader(UiEventType.Char, 1.5, leaf),
                CodePoint = 'a',
            },
            SideDown = MakePointer(UiEventType.PointerDown, sideLeaf),
            OrphanDown = MakePointer(UiEventType.PointerDown, orphanLeaf),
            NullChar = new CharEvent
            {
                Header = new UiEventHeader(UiEventType.Char, 1.5, null),
                CodePoint = 'b',
            },
        };
    }

    private static void Register(
        Element element,
        UiEventHandler<PointerEvent> pointer,
        UiEventHandler<ScrollEvent> scroll,
        UiEventHandler<KeyEvent> key,
        UiEventHandler<CharEvent> character)
    {
        element.OnPointerMove = pointer;
        element.OnPointerMoveCapture = pointer;
        element.OnPointerDown = pointer;
        element.OnPointerDownCapture = pointer;
        element.OnPointerUp = pointer;
        element.OnPointerUpCapture = pointer;
        element.OnScroll = scroll;
        element.OnScrollCapture = scroll;
        element.OnKeyDown = key;
        element.OnKeyDownCapture = key;
        element.OnKeyUp = key;
        element.OnKeyUpCapture = key;
        element.OnChar = character;
        element.OnCharCapture = character;
    }

    private static PointerEvent MakePointer(UiEventType type, Element target)
    {
        return new PointerEvent
        {
            Header = new UiEventHeader(type, 1.5, target),
            Position = new float2(10, 20),
            Button = PointerButton.Left,
            Modifiers = UiModifiers.None,
        };
    }

    private static KeyEvent MakeKey(UiEventType type, Element target)
    {
        return new KeyEvent
        {
            Header = new UiEventHeader(type, 1.5, target),
            Key = 65,
            ScanCode = 30,
            Modifiers = UiModifiers.None,
            IsRepeat = false,
        };
    }

    /// <summary>The measured body. It uses no lambda, no boxing, no string formatting, and no Assert.</summary>
    private static void Step(Scene scene)
    {
        UiContext context = scene.Context;

        context.PushEvent(in scene.ExtraMove);
        context.PushEvent(in scene.LeafUp);
        context.PushEvent(in scene.LeafMove);
        context.PushEvent(in scene.LeafDown);
        context.PushEvent(in scene.LeafScroll);
        context.PushEvent(in scene.LeafKeyDown);
        context.PushEvent(in scene.LeafKeyUp);
        context.PushEvent(in scene.LeafChar);
        context.PushEvent(in scene.SideDown);
        context.PushEvent(in scene.OrphanDown);
        context.PushEvent(in scene.NullChar);

        scene.LastWalks = context.DrainEvents();
    }

    private static void Run(Scene scene, int iterations)
    {
        for (int i = 0; i < iterations; i++)
        {
            Step(scene);
        }
    }

    [Fact]
    public void PushAndDrainAllocateNothingAfterWarmUp()
    {
        Scene scene = BuildScene();

        // Warm-up: two steps let the JIT compile the code and let the path buffer grow (A7).
        Run(scene, 1);
        Run(scene, 1);

        // The release empties the allocation context, so a background GC cannot add bytes. See AllocationGate.
        AllocationGate.ReleaseAllocationContext();
        long before = GC.GetAllocatedBytesForCurrentThread();
        Run(scene, Iterations);
        long after = GC.GetAllocatedBytesForCurrentThread();

        Assert.Equal(0L, after - before);
    }

    [Fact]
    public void APushToAFullQueueAllocatesNothing()
    {
        Scene scene = BuildScene();

        Run(scene, 1);
        Run(scene, 1);

        // Fill the ring. The ring is full after 9 pushes, and no drain runs.
        for (int i = 0; i < QueueCapacity; i++)
        {
            scene.Context.PushEvent(in scene.LeafMove);
        }

        Assert.Equal(QueueCapacity, scene.Context.QueuedEventCount);

        long dropsBefore = scene.Context.DroppedEventCount;
        AllocationGate.ReleaseAllocationContext();
        long before = GC.GetAllocatedBytesForCurrentThread();
        for (int i = 0; i < PushCount; i++)
        {
            scene.Context.PushEvent(in scene.LeafMove);
        }

        long after = GC.GetAllocatedBytesForCurrentThread();
        long drops = scene.Context.DroppedEventCount - dropsBefore;

        Assert.Equal(0L, after - before);
        Assert.Equal((long)PushCount, drops);
        Assert.Equal(QueueCapacity, scene.Context.QueuedEventCount);
    }

    [Fact]
    public void TheMeasuredBodyDoesRealWork()
    {
        Scene scene = BuildScene();
        Counter counter = scene.Counter;

        Run(scene, 10);

        Assert.Equal(WalksOfStep, scene.LastWalks);
        Assert.Equal(10 * CallsOfStep, counter.Total);
        Assert.Equal(10 * 170, counter.PointerCalls);
        Assert.Equal(10, counter.ExtraCalls);
        Assert.Equal(10 * 82, counter.ScrollCalls);
        Assert.Equal(10 * 103, counter.KeyCalls);
        Assert.Equal(10, counter.Stops);
        Assert.Equal(10 * 82, counter.CharCalls);
        Assert.Equal(10L * DropsOfStep, scene.Context.DroppedEventCount);
        Assert.Equal(0, scene.Context.QueuedEventCount);
        Assert.True(scene.Context.EventPathLength >= 64);
    }
}
