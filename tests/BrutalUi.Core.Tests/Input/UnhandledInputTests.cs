using System;
using Brutal.Numerics;
using Xunit;

namespace BrutalUi.Core.Tests;

/// <summary>
/// Tests of the slot <see cref="UiContext.OnUnhandledInput"/> (A6 of state-and-commands): the key downs that reach it,
/// the order after the walk, the stop rule, the Tab rule, the target rule, the repeat flag, the edit keys of a text
/// field, and the exception rule.
/// </summary>
/// <remarks>
/// The tree order of <see cref="PointerTree"/> is <see cref="PointerTree.Root"/>, <see cref="PointerTree.Left"/>,
/// <see cref="PointerTree.Right"/>, and <see cref="PointerTree.Inner"/>. The class table is global and xUnit runs test
/// classes in parallel. Each test gives the tree a unique name prefix.
/// </remarks>
public sealed class UnhandledInputTests
{
    private const int EscapeKey = 256;
    private const int EnterKey = 257;
    private const int BackspaceKey = 259;
    private const int LeftKey = 263;
    private const int DownKey = 264;

    /// <summary>
    /// A key down with no target and no focus owner gets no walk. It reaches the slot with its type, its code, its
    /// modifiers, and the repeat flag <see langword="false"/>.
    /// </summary>
    [Fact]
    public void AKeyDownWithNoFocusOwnerCallsTheSlot()
    {
        PointerTree tree = new(nameof(UnhandledInputTests) + "." + nameof(AKeyDownWithNoFocusOwnerCallsTheSlot));
        Recorder recorder = new(tree.Context);
        tree.Context.OnUnhandledInput = recorder.Handle;
        tree.Settle();
        Assert.Null(tree.Context.FocusedElement);

        KeyEvent e = PointerTree.Key(UiEventType.KeyDown, EscapeKey, UiModifiers.Shift);
        tree.Context.PushEvent(in e);
        tree.Context.Update(0f);

        Assert.Equal(1, recorder.Count);
        Assert.Equal(UiEventType.KeyDown, recorder.Types[0]);
        Assert.Equal(EscapeKey, recorder.Codes[0]);
        Assert.Equal(UiModifiers.Shift, recorder.Modifiers[0]);
        Assert.False(recorder.Repeats[0]);
        Assert.Equal(0, tree.Context.QueuedEventCount);
    }

    /// <summary>
    /// A key down walks to the focus owner first. Its handler does not stop the walk, so the slot gets the key after
    /// the walk.
    /// </summary>
    [Fact]
    public void AKeyDownThatNoHandlerStopsCallsTheSlotAfterTheWalk()
    {
        PointerTree tree = new(nameof(UnhandledInputTests) + "." + nameof(AKeyDownThatNoHandlerStopsCallsTheSlotAfterTheWalk));
        Recorder recorder = new(tree.Context);
        tree.Context.OnUnhandledInput = recorder.Handle;
        tree.Left.IsFocusable = true;
        int handlerStep = 0;
        tree.Left.OnKeyDown = (ref KeyEvent e) => handlerStep = ++recorder.Clock;
        tree.Settle();
        Assert.True(tree.Context.SetFocus(tree.Left));

        KeyEvent down = PointerTree.Key(UiEventType.KeyDown, DownKey, UiModifiers.None);
        tree.Context.PushEvent(in down);
        tree.Context.Update(0f);

        Assert.Equal(1, handlerStep);
        Assert.Equal(1, recorder.Count);
        Assert.Equal(2, recorder.Steps[0]);
        Assert.Equal(DownKey, recorder.Codes[0]);
        Assert.Same(tree.Left, recorder.Owners[0]);
    }

    /// <summary>
    /// A handler of the focus owner stops the walk of a key down. The key is a handled key, so the slot gets no call.
    /// </summary>
    [Fact]
    public void AKeyDownThatAHandlerStopsDoesNotCallTheSlot()
    {
        PointerTree tree = new(nameof(UnhandledInputTests) + "." + nameof(AKeyDownThatAHandlerStopsDoesNotCallTheSlot));
        Recorder recorder = new(tree.Context);
        tree.Context.OnUnhandledInput = recorder.Handle;
        tree.Left.IsFocusable = true;
        int handlerCalls = 0;
        tree.Left.OnKeyDown = (ref KeyEvent e) =>
        {
            handlerCalls++;
            e.Header.StopPropagation();
        };
        tree.Settle();
        Assert.True(tree.Context.SetFocus(tree.Left));

        KeyEvent escape = PointerTree.Key(UiEventType.KeyDown, EscapeKey, UiModifiers.None);
        tree.Context.PushEvent(in escape);
        tree.Context.Update(0f);

        Assert.Equal(1, handlerCalls);
        Assert.Equal(0, recorder.Count);
    }

    /// <summary>
    /// A key up and a char never reach the slot: not with a focus owner, and not with no focus owner.
    /// </summary>
    [Fact]
    public void AKeyUpAndACharDoNotCallTheSlot()
    {
        PointerTree tree = new(nameof(UnhandledInputTests) + "." + nameof(AKeyUpAndACharDoNotCallTheSlot));
        Recorder recorder = new(tree.Context);
        tree.Context.OnUnhandledInput = recorder.Handle;
        tree.Left.IsFocusable = true;
        tree.Settle();

        KeyEvent up = PointerTree.Key(UiEventType.KeyUp, EscapeKey, UiModifiers.None);
        CharEvent letter = PointerTree.Char('a');
        tree.Context.PushEvent(in up);
        tree.Context.Update(0f);
        Assert.Equal(0, recorder.Count);
        tree.Context.PushEvent(in letter);
        tree.Context.Update(0f);
        Assert.Equal(0, recorder.Count);

        Assert.True(tree.Context.SetFocus(tree.Left));
        tree.Context.PushEvent(in up);
        tree.Context.Update(0f);
        Assert.Equal(0, recorder.Count);
        tree.Context.PushEvent(in letter);
        tree.Context.Update(0f);
        Assert.Equal(0, recorder.Count);
        Assert.Equal(0, tree.Context.QueuedEventCount);
    }

    /// <summary>
    /// Tab and Shift+Tab move the focus. A Tab move is a built-in action, so the slot gets no call. Control+Tab is not
    /// a Tab move: it keeps the focus and reaches the slot with the Tab key code.
    /// </summary>
    [Fact]
    public void ATabMoveDoesNotCallTheSlotAndAControlTabDoes()
    {
        PointerTree tree = new(nameof(UnhandledInputTests) + "." + nameof(ATabMoveDoesNotCallTheSlotAndAControlTabDoes));
        Recorder recorder = new(tree.Context);
        tree.Context.OnUnhandledInput = recorder.Handle;
        tree.Left.IsFocusable = true;
        tree.Right.IsFocusable = true;
        tree.Settle();

        KeyEvent tab = PointerTree.Tab(shift: false);
        tree.Context.PushEvent(in tab);
        tree.Context.Update(0f);
        Assert.Same(tree.Left, tree.Context.FocusedElement);
        Assert.Equal(0, recorder.Count);

        KeyEvent shiftTab = PointerTree.Tab(shift: true);
        tree.Context.PushEvent(in shiftTab);
        tree.Context.Update(0f);
        Assert.Same(tree.Right, tree.Context.FocusedElement);
        Assert.Equal(0, recorder.Count);

        KeyEvent controlTab = PointerTree.Key(UiEventType.KeyDown, UiContext.TabKey, UiModifiers.Control);
        tree.Context.PushEvent(in controlTab);
        tree.Context.Update(0f);
        Assert.Same(tree.Right, tree.Context.FocusedElement);
        Assert.Equal(1, recorder.Count);
        Assert.Equal(UiEventType.KeyDown, recorder.Types[0]);
        Assert.Equal(258, recorder.Codes[0]);
        Assert.Equal(UiModifiers.Control, recorder.Modifiers[0]);
        Assert.Same(tree.Right, recorder.Owners[0]);
    }

    /// <summary>
    /// A key down with a target from the caller walks to that target only. The drain did not route it, so the slot
    /// gets no call, also when no handler stops the walk and no focus owner exists.
    /// </summary>
    [Fact]
    public void AKeyWithATargetFromTheCallerDoesNotCallTheSlot()
    {
        PointerTree tree = new(nameof(UnhandledInputTests) + "." + nameof(AKeyWithATargetFromTheCallerDoesNotCallTheSlot));
        Recorder recorder = new(tree.Context);
        tree.Context.OnUnhandledInput = recorder.Handle;
        int handlerCalls = 0;
        tree.Left.OnKeyDown = (ref KeyEvent e) => handlerCalls++;
        tree.Settle();
        Assert.Null(tree.Context.FocusedElement);

        KeyEvent targeted = PointerTree.Key(UiEventType.KeyDown, EscapeKey, UiModifiers.None);
        targeted.Header = new UiEventHeader(UiEventType.KeyDown, 0d, tree.Left);
        tree.Context.PushEvent(in targeted);
        tree.Context.Update(0f);

        Assert.Equal(1, handlerCalls);
        Assert.Equal(0, recorder.Count);
    }

    /// <summary>
    /// A key repeat reaches the slot with the repeat flag <see langword="true"/>. The map decides what a repeat does.
    /// </summary>
    [Fact]
    public void ARepeatCallsTheSlotWithTheRepeatFlag()
    {
        PointerTree tree = new(nameof(UnhandledInputTests) + "." + nameof(ARepeatCallsTheSlotWithTheRepeatFlag));
        Recorder recorder = new(tree.Context);
        tree.Context.OnUnhandledInput = recorder.Handle;
        tree.Settle();

        KeyEvent repeat = PointerTree.Key(UiEventType.KeyDown, DownKey, UiModifiers.None);
        repeat.IsRepeat = true;
        tree.Context.PushEvent(in repeat);
        tree.Context.Update(0f);

        Assert.Equal(1, recorder.Count);
        Assert.Equal(DownKey, recorder.Codes[0]);
        Assert.True(recorder.Repeats[0]);
    }

    /// <summary>
    /// A focused text field stops the walk of its edit keys, so Left, Backspace, and the main Enter key never reach
    /// the slot. Escape and Down walk on, and each reaches the slot one time.
    /// </summary>
    [Fact]
    public void AFocusedTextFieldKeepsItsEditKeys()
    {
        TextFieldTree tree = new();
        Recorder recorder = new(tree.Context);
        tree.Context.OnUnhandledInput = recorder.Handle;
        tree.Focus();
        Assert.Same(tree.Field, tree.Context.FocusedElement);

        KeyEvent left = PointerTree.Key(UiEventType.KeyDown, LeftKey, UiModifiers.None);
        KeyEvent backspace = PointerTree.Key(UiEventType.KeyDown, BackspaceKey, UiModifiers.None);
        KeyEvent enter = PointerTree.Key(UiEventType.KeyDown, EnterKey, UiModifiers.None);
        tree.Context.PushEvent(in left);
        tree.Context.PushEvent(in backspace);
        tree.Context.PushEvent(in enter);
        tree.Context.Update(0f);
        Assert.Equal(0, recorder.Count);

        KeyEvent escape = PointerTree.Key(UiEventType.KeyDown, EscapeKey, UiModifiers.None);
        KeyEvent down = PointerTree.Key(UiEventType.KeyDown, DownKey, UiModifiers.None);
        tree.Context.PushEvent(in escape);
        tree.Context.PushEvent(in down);
        tree.Context.Update(0f);

        Assert.Equal(2, recorder.Count);
        Assert.Equal(EscapeKey, recorder.Codes[0]);
        Assert.Equal(DownKey, recorder.Codes[1]);
        Assert.Same(tree.Field, recorder.Owners[0]);
        Assert.Same(tree.Field, recorder.Owners[1]);
        Assert.Same(tree.Field, tree.Context.FocusedElement);
    }

    /// <summary>
    /// The slot throws at the first of two key downs. The exception leaves <see cref="UiContext.Update(float)"/>, and
    /// the second key stays in the ring. The next update calls the slot for the second key and empties the ring.
    /// </summary>
    [Fact]
    public void ASlotThatThrowsLeavesUpdateAndTheNextUpdateDrainsTheRest()
    {
        PointerTree tree = new(nameof(UnhandledInputTests) + "." + nameof(ASlotThatThrowsLeavesUpdateAndTheNextUpdateDrainsTheRest));
        Recorder recorder = new(tree.Context) { ThrowAtCall = 0 };
        tree.Context.OnUnhandledInput = recorder.Handle;
        tree.Settle();

        KeyEvent escape = PointerTree.Key(UiEventType.KeyDown, EscapeKey, UiModifiers.None);
        KeyEvent down = PointerTree.Key(UiEventType.KeyDown, DownKey, UiModifiers.None);
        tree.Context.PushEvent(in escape);
        tree.Context.PushEvent(in down);

        Assert.Throws<SlotException>(() => tree.Context.Update(0f));
        Assert.Equal(1, recorder.Count);
        Assert.Equal(EscapeKey, recorder.Codes[0]);
        Assert.Equal(1, tree.Context.QueuedEventCount);

        tree.Context.Update(0f);

        Assert.Equal(2, recorder.Count);
        Assert.Equal(DownKey, recorder.Codes[1]);
        Assert.Equal(0, tree.Context.QueuedEventCount);
    }

    /// <summary>
    /// The root is not pickable, so a press inside the root and outside each child hits no element. A right press
    /// with Shift reaches the slot with its type, the GLFW button value 1, the modifiers, and the repeat flag
    /// <see langword="false"/>. The click focus rule runs first, so the slot reads no focus owner.
    /// </summary>
    [Fact]
    public void APressOverNoElementCallsTheSlotWithTheButton()
    {
        PointerTree tree = new(nameof(UnhandledInputTests) + "." + nameof(APressOverNoElementCallsTheSlotWithTheButton));
        Recorder recorder = new(tree.Context);
        tree.Context.OnUnhandledInput = recorder.Handle;
        tree.Root.IsPickable = false;
        tree.Left.IsFocusable = true;
        tree.Settle();
        Assert.True(tree.Context.SetFocus(tree.Left));

        PointerEvent press = PointerTree.Down(new float2(300f, 200f), PointerButton.Right);
        press.Modifiers = UiModifiers.Shift;
        tree.Context.PushEvent(in press);
        tree.Context.Update(0f);

        Assert.Equal(1, recorder.Count);
        Assert.Equal(UiEventType.PointerDown, recorder.Types[0]);
        Assert.Equal(1, recorder.Codes[0]);
        Assert.Equal(UiModifiers.Shift, recorder.Modifiers[0]);
        Assert.False(recorder.Repeats[0]);
        Assert.Null(recorder.Owners[0]);
        Assert.Null(tree.Context.FocusedElement);
        Assert.False(tree.Context.RoutingGate.PointerOverUi);
        Assert.Equal(0, tree.Context.QueuedEventCount);
    }

    /// <summary>
    /// A hit takes a press. A press on an element with a handler that does not stop the walk, a press on a pickable
    /// element with no handler, and a press with a target from the caller never reach the slot.
    /// </summary>
    [Fact]
    public void APressOnAnElementDoesNotCallTheSlot()
    {
        PointerTree tree = new(nameof(UnhandledInputTests) + "." + nameof(APressOnAnElementDoesNotCallTheSlot));
        Recorder recorder = new(tree.Context);
        tree.Context.OnUnhandledInput = recorder.Handle;
        int handlerCalls = 0;
        tree.Left.OnPointerDown = (ref PointerEvent e) => handlerCalls++;
        tree.Settle();

        // A press on an element with a handler that does not stop the walk.
        PointerEvent onLeft = PointerTree.Down(new float2(50f, 50f), PointerButton.Right);
        PointerEvent upLeft = PointerTree.Up(new float2(50f, 50f), PointerButton.Right);
        tree.Context.PushEvent(in onLeft);
        tree.Context.PushEvent(in upLeft);
        tree.Context.Update(0f);
        Assert.Equal(1, handlerCalls);
        Assert.Equal(0, recorder.Count);

        // A press on the pickable root, which has no handler.
        PointerEvent onRoot = PointerTree.Down(new float2(300f, 200f), PointerButton.Left);
        PointerEvent upRoot = PointerTree.Up(new float2(300f, 200f), PointerButton.Left);
        tree.Context.PushEvent(in onRoot);
        tree.Context.PushEvent(in upRoot);
        tree.Context.Update(0f);
        Assert.True(tree.Context.RoutingGate.PointerOverUi);
        Assert.Equal(0, recorder.Count);

        // A press with a target from the caller, at a point over no element. The drain does not route it.
        tree.Root.IsPickable = false;
        PointerEvent targeted = PointerTree.Down(new float2(300f, 200f), PointerButton.Left);
        targeted.Header = new UiEventHeader(UiEventType.PointerDown, 0d, tree.Root);
        tree.Context.PushEvent(in targeted);
        tree.Context.Update(0f);
        Assert.Equal(0, recorder.Count);
        Assert.Equal(0, tree.Context.QueuedEventCount);
    }

    /// <summary>
    /// The slot gets a press only. A move, a release, and a scroll over no element never reach it.
    /// </summary>
    [Fact]
    public void AReleaseAMoveAndAScrollOverNoElementDoNotCallTheSlot()
    {
        PointerTree tree = new(nameof(UnhandledInputTests) + "." + nameof(AReleaseAMoveAndAScrollOverNoElementDoNotCallTheSlot));
        Recorder recorder = new(tree.Context);
        tree.Context.OnUnhandledInput = recorder.Handle;
        tree.Root.IsPickable = false;
        tree.Settle();

        PointerEvent move = PointerTree.Move(new float2(300f, 200f));
        PointerEvent up = PointerTree.Up(new float2(300f, 200f), PointerButton.Left);
        ScrollEvent scroll = PointerTree.Scroll(new float2(300f, 200f), new float2(0f, 1f));
        tree.Context.PushEvent(in move);
        tree.Context.PushEvent(in up);
        tree.Context.PushEvent(in scroll);
        tree.Context.Update(0f);

        Assert.Equal(0, recorder.Count);
        Assert.False(tree.Context.RoutingGate.PointerOverUi);
        Assert.Equal(0, tree.Context.QueuedEventCount);
    }

    /// <summary>The exception that <see cref="Recorder"/> throws at <see cref="Recorder.ThrowAtCall"/>.</summary>
    private sealed class SlotException : Exception
    {
        public SlotException()
            : base("The slot throws on purpose.")
        {
        }
    }

    /// <summary>
    /// The target of the slot. It stores each call in fixed arrays, with the step of a clock that a test handler can
    /// share, and the focus owner at the time of the call.
    /// </summary>
    private sealed class Recorder
    {
        private const int Capacity = 8;

        public readonly UiEventType[] Types = new UiEventType[Capacity];
        public readonly int[] Codes = new int[Capacity];
        public readonly UiModifiers[] Modifiers = new UiModifiers[Capacity];
        public readonly bool[] Repeats = new bool[Capacity];
        public readonly int[] Steps = new int[Capacity];
        public readonly Element?[] Owners = new Element?[Capacity];

        private readonly UiContext _context;

        public Recorder(UiContext context)
        {
            _context = context;
        }

        public int Count;
        public int Clock;
        public int ThrowAtCall = -1;

        public void Handle(UiEventType type, int code, UiModifiers modifiers, bool isRepeat)
        {
            int index = Count;
            Types[index] = type;
            Codes[index] = code;
            Modifiers[index] = modifiers;
            Repeats[index] = isRepeat;
            Steps[index] = ++Clock;
            Owners[index] = _context.FocusedElement;
            Count++;
            if (index == ThrowAtCall)
            {
                throw new SlotException();
            }
        }
    }
}
