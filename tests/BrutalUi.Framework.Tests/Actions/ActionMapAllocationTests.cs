using System;
using System.Globalization;
using System.Text;
using Brutal.Numerics;
using BrutalUi.Core;
using BrutalUi.Core.Tests;
using Xunit;

namespace BrutalUi.Framework.Tests;

/// <summary>
/// The allocation gate of the action map (the seventeenth gate). After warm-up, the key path, the mouse path, the
/// gamepad path, the focus actions, and the typed frame read with the five queries, each through <c>Update</c> and
/// <c>Draw</c>, must allocate 0 bytes in the Debug build.
/// </summary>
/// <remarks>
/// <para>
/// The scene holds a <see cref="UiContext"/> with a <see cref="RecordingRenderer"/> measurer and a viewport of 400 by
/// 300. The root is not pickable, so a press outside the panel hits no element and reaches the map. The panel is 200 by
/// 200 and holds three focusable buttons and a <see cref="TextField"/>. The <see cref="CommandRegistry"/> holds 16
/// counted commands <c>cmd-00</c> to <c>cmd-15</c> and the two focus commands. Each counted command has its own
/// <c>Counter</c> target and one shared static handler. The <see cref="UiActionMap"/> holds 23 actions: 16 counted
/// actions <c>act-00</c> to <c>act-15</c>, the two focus actions, and five typed actions <c>Move</c> (Vector2),
/// <c>Look</c> (Vector2), <c>Zoom</c> (Axis), <c>Throttle</c> (Axis), and <c>Fire</c> (Button with no command). It
/// holds 28 bindings: 13 key bindings (some with Control or Shift, and also Escape, Space, W, Up, and Down), 3 mouse
/// button bindings, 5 gamepad button bindings (<c>A</c>, <c>B</c>, <c>DpadUp</c>, <c>DpadDown</c>, and <c>X</c>), one
/// four-key vector, one two-key axis, one stick, two pad axes, one pointer delta, and one scroll. The map is active
/// on the context.
/// </para>
/// <para>
/// The query step drives the typed frame read and the five queries. It writes two pad states of slot 0, pushes W,
/// Space, a pointer move, and a scroll outside the panel, and adds each query result to a float and an int field of
/// the scene. The scene holds the events, the two pad states, and the sums as fields.
/// </para>
/// <para>
/// The rule: a measured body creates no lambda, no string, no boxed value, no array, and no Assert. The scene builds
/// each name, each delegate, and each event before the measure. Each event is a field of the scene, because
/// <see cref="UiContext.PushEvent(in KeyEvent)"/> takes a reference. The test adds no warm-up round to hide an
/// allocation.
/// </para>
/// </remarks>
public sealed class ActionMapAllocationTests
{
    private const int Iterations = 100;
    private const int WarmUpRounds = 2;
    private const int CommandCount = 16;

    private const int EscapeKey = 256;
    private const int DownKey = 264;
    private const int UpKey = 265;
    private const int SKey = 83;
    private const int WKey = 87;
    private const int F1Key = 290;
    private const int F12Key = 301;
    private const int SpaceKey = 32;
    private const int AKey = 65;
    private const int DKey = 68;

    /// <summary>The name of the Vector2 move action of the typed read.</summary>
    private static ReadOnlySpan<byte> MoveAction => "Move"u8;

    /// <summary>The name of the Vector2 look action of the typed read.</summary>
    private static ReadOnlySpan<byte> LookAction => "Look"u8;

    /// <summary>The name of the Axis zoom action of the typed read.</summary>
    private static ReadOnlySpan<byte> ZoomAction => "Zoom"u8;

    /// <summary>The name of the Axis throttle action of the typed read.</summary>
    private static ReadOnlySpan<byte> ThrottleAction => "Throttle"u8;

    /// <summary>The name of the Button fire action of the typed read.</summary>
    private static ReadOnlySpan<byte> FireAction => "Fire"u8;

    /// <summary>The handler that adds one to the counter target. It sits in a field, so no call creates a delegate.</summary>
    private static readonly CommandHandler s_count = static target => ((Counter)target!).Calls++;

    /// <summary>The target of a command: a call counter.</summary>
    private sealed class Counter
    {
        public int Calls;
    }

    /// <summary>The map, the registry, the tree, the events, and the counters that the measured bodies use.</summary>
    private sealed class Scene
    {
        public required UiActionMap Map;

        public required UiContext Context;

        public required RecordingRenderer Renderer;

        public required Counter[] Counters;

        public required byte[] LastAction;

        public required Button First;

        public required Button Second;

        public required Button Third;

        public required TextField Field;

        public required KeyEvent Escape;

        public required KeyEvent EscapeRepeat;

        public required KeyEvent ControlS;

        public required KeyEvent W;

        public required KeyEvent Unbound;

        public required KeyEvent Down;

        public required KeyEvent Up;

        public required PointerEvent PressRight;

        public required PointerEvent ReleaseRight;

        public required PointerEvent PressLeft;

        public required PointerEvent ReleaseLeft;

        public required KeyEvent Space;

        public required KeyEvent WRelease;

        public required KeyEvent SpaceRelease;

        public required PointerEvent MoveOutside;

        public required ScrollEvent ScrollOutside;

        public required UiGamepadState PadDown;

        public required UiGamepadState PadUp;

        /// <summary>The sum of each vector and axis read by the query step.</summary>
        public float ValueSum;

        /// <summary>The sum of each true Button flag read by the query step.</summary>
        public int FlagSum;
    }

    private static Scene BuildScene()
    {
        CommandRegistry registry = new();
        Counter[] counters = new Counter[CommandCount];
        byte[][] commands = new byte[CommandCount][];
        byte[][] actions = new byte[CommandCount][];
        for (int i = 0; i < CommandCount; i++)
        {
            string number = i.ToString("00", CultureInfo.InvariantCulture);
            commands[i] = Encoding.UTF8.GetBytes("cmd-" + number);
            actions[i] = Encoding.UTF8.GetBytes("act-" + number);
            counters[i] = new Counter();
            registry.Register(commands[i], s_count, counters[i]);
        }

        UiContext context = new(new UiContext.CreateInfo { Measurer = new RecordingRenderer() })
        {
            ViewportSize = new float2(400, 300),
        };
        FocusCommands.Register(registry, context);

        Element root = new();
        root.Inline.FlexDirection = FlexDirection.Column;
        root.Inline.AlignItems = Align.FlexStart;
        root.Inline.Width = 400;
        root.Inline.Height = 300;
        root.IsPickable = false;
        context.AddRoot(root);

        Element panel = new();
        panel.Inline.FlexDirection = FlexDirection.Column;
        panel.Inline.AlignItems = Align.FlexStart;
        panel.Inline.Width = 200;
        panel.Inline.Height = 200;
        root.Children.Add(panel);
        Button first = AddButton(panel, "One");
        Button second = AddButton(panel, "Two");
        Button third = AddButton(panel, "Three");
        TextField field = new(new TextField.CreateInfo { Capacity = 16 });
        field.Inline.Width = 120;
        field.Inline.Height = 24;
        panel.Children.Add(field);

        UiActionMap map = new(new UiActionMap.CreateInfo { Commands = registry });
        for (int i = 0; i < CommandCount; i++)
        {
            map.AddAction(actions[i], commands[i]);
        }

        map.AddAction(UiActions.NavigateDown, FocusCommands.Next);
        map.AddAction(UiActions.NavigateUp, FocusCommands.Previous);

        map.BindKey(actions[0], EscapeKey);
        map.BindKey(actions[1], SKey, UiModifiers.Control);
        map.BindKey(actions[2], WKey);
        map.BindKey(UiActions.NavigateUp, UpKey);
        map.BindKey(UiActions.NavigateDown, DownKey);
        map.BindKey(actions[3], F1Key);
        map.BindKey(actions[4], F1Key + 1, UiModifiers.Shift);
        map.BindKey(actions[5], F1Key + 2, UiModifiers.Control);
        map.BindKey(actions[6], F1Key + 3);
        map.BindKey(actions[7], F1Key + 4, UiModifiers.Control | UiModifiers.Shift);
        map.BindKey(actions[8], F1Key + 5);
        map.BindKey(actions[9], F1Key + 6);
        map.BindMouseButton(actions[10], PointerButton.Right);
        map.BindMouseButton(actions[11], PointerButton.Middle);
        map.BindMouseButton(actions[12], PointerButton.Left);
        map.BindGamepadButton(actions[13], UiGamepadButtons.A);
        map.BindGamepadButton(actions[14], UiGamepadButtons.B);
        map.BindGamepadButton(UiActions.NavigateUp, UiGamepadButtons.DpadUp);
        map.BindGamepadButton(UiActions.NavigateDown, UiGamepadButtons.DpadDown);

        map.AddAction(MoveAction, UiActionType.Vector2);
        map.AddAction(LookAction, UiActionType.Vector2);
        map.AddAction(ZoomAction, UiActionType.Axis);
        map.AddAction(ThrottleAction, UiActionType.Axis);
        map.AddAction(FireAction, UiActionType.Button);
        map.BindKey(FireAction, SpaceKey);
        map.BindKeyVector(MoveAction, WKey, AKey, SKey, DKey);
        map.BindKeyAxis(ThrottleAction, SKey, WKey);
        map.BindGamepadButton(FireAction, UiGamepadButtons.X);
        map.BindGamepadStick(MoveAction, UiGamepadStick.Left);
        map.BindGamepadAxis(ZoomAction, UiGamepadAxis.RightTrigger);
        map.BindGamepadAxis(ZoomAction, UiGamepadAxis.LeftTrigger, -1f);
        map.BindMouseDelta(LookAction);
        map.BindScroll(ZoomAction);
        map.Activate(context);

        UiGamepadState padDown = new(UiGamepadButtons.X | UiGamepadButtons.A, stackalloc float[] { 1f, 0f, 0f, 0f, 0.25f, 0.5f });
        UiGamepadState padUp = new(UiGamepadButtons.None, []);

        float2 outside = new(300f, 250f);
        float2 onButton = new(40f, 12f);
        Scene scene = new()
        {
            Map = map,
            Context = context,
            Renderer = new RecordingRenderer(),
            Counters = counters,
            LastAction = actions[CommandCount - 1],
            First = first,
            Second = second,
            Third = third,
            Field = field,
            Escape = Key(EscapeKey, UiModifiers.None, false),
            EscapeRepeat = Key(EscapeKey, UiModifiers.None, true),
            ControlS = Key(SKey, UiModifiers.Control, false),
            W = Key(WKey, UiModifiers.None, false),
            Unbound = Key(F12Key, UiModifiers.None, false),
            Down = Key(DownKey, UiModifiers.None, false),
            Up = Key(UpKey, UiModifiers.None, false),
            PressRight = Pointer(UiEventType.PointerDown, outside, PointerButton.Right),
            ReleaseRight = Pointer(UiEventType.PointerUp, outside, PointerButton.Right),
            PressLeft = Pointer(UiEventType.PointerDown, onButton, PointerButton.Left),
            ReleaseLeft = Pointer(UiEventType.PointerUp, onButton, PointerButton.Left),
            Space = Key(SpaceKey, UiModifiers.None, false),
            WRelease = KeyUp(WKey),
            SpaceRelease = KeyUp(SpaceKey),
            MoveOutside = Pointer(UiEventType.PointerMove, outside),
            ScrollOutside = Scroll(outside, new float2(0f, 3f)),
            PadDown = padDown,
            PadUp = padUp,
        };

        // One frame builds the layout, so the first press hits the right place.
        context.Update(0f);
        context.Draw(scene.Renderer);
        return scene;
    }

    private static Button AddButton(Element parent, string text)
    {
        Button button = new(new Button.CreateInfo { Text = text });
        button.Inline.Width = 80;
        button.Inline.Height = 24;
        parent.Children.Add(button);
        return button;
    }

    /// <summary>Creates a key down event with no target.</summary>
    private static KeyEvent Key(int key, UiModifiers modifiers, bool isRepeat)
    {
        return new KeyEvent
        {
            Header = new UiEventHeader(UiEventType.KeyDown, 0d, null),
            Key = key,
            ScanCode = 0,
            Modifiers = modifiers,
            IsRepeat = isRepeat,
        };
    }

    /// <summary>Creates a pointer event with no target.</summary>
    private static PointerEvent Pointer(UiEventType type, float2 position, PointerButton button = PointerButton.Left)
    {
        return new PointerEvent
        {
            Header = new UiEventHeader(type, 0d, null),
            Position = position,
            Button = button,
            Modifiers = UiModifiers.None,
        };
    }

    /// <summary>Creates a key up event with no target.</summary>
    private static KeyEvent KeyUp(int key)
    {
        return new KeyEvent
        {
            Header = new UiEventHeader(UiEventType.KeyUp, 0d, null),
            Key = key,
            ScanCode = 0,
            Modifiers = UiModifiers.None,
            IsRepeat = false,
        };
    }

    /// <summary>Creates a scroll event with no target.</summary>
    private static ScrollEvent Scroll(float2 position, float2 delta)
    {
        return new ScrollEvent
        {
            Header = new UiEventHeader(UiEventType.Scroll, 0d, null),
            Position = position,
            Delta = delta,
            Modifiers = UiModifiers.None,
        };
    }

    /// <summary>
    /// The measured body of the first test. The first group focuses the first button and pushes Escape, Control+S, an
    /// unbound key, and a repeat of Escape. One update runs it. The second group focuses the text field and pushes W and
    /// Escape. Then <c>Update</c> and <c>Draw</c> run. It uses no lambda, no boxing, no string, and no Assert.
    /// </summary>
    private static void KeyStep(Scene scene)
    {
        UiContext context = scene.Context;
        context.SetFocus(scene.First);
        context.PushEvent(in scene.Escape);
        context.PushEvent(in scene.ControlS);
        context.PushEvent(in scene.Unbound);
        context.PushEvent(in scene.EscapeRepeat);
        context.Update(0.016f);

        context.SetFocus(scene.Field);
        context.PushEvent(in scene.W);
        context.PushEvent(in scene.Escape);
        context.Update(0.016f);
        context.Draw(scene.Renderer);
    }

    /// <summary>
    /// The measured body of the second test. A right press and release outside the panel, and a left press and release
    /// on a button, then <c>Update</c> and <c>Draw</c>.
    /// </summary>
    private static void MouseStep(Scene scene)
    {
        UiContext context = scene.Context;
        context.PushEvent(in scene.PressRight);
        context.PushEvent(in scene.ReleaseRight);
        context.PushEvent(in scene.PressLeft);
        context.PushEvent(in scene.ReleaseLeft);
        context.Update(0.016f);
        context.Draw(scene.Renderer);
    }

    /// <summary>
    /// The measured body of the third test. A push of <c>A</c> and <c>DpadDown</c>, a push of <c>None</c>, and an invoke
    /// of one action, then <c>Update</c> and <c>Draw</c>.
    /// </summary>
    private static void GamepadStep(Scene scene)
    {
        scene.Map.PushGamepad(UiGamepadButtons.A | UiGamepadButtons.DpadDown);
        scene.Map.PushGamepad(UiGamepadButtons.None);
        scene.Map.InvokeAction(scene.LastAction);
        scene.Context.Update(0.016f);
        scene.Context.Draw(scene.Renderer);
    }

    /// <summary>
    /// The measured body of the fourth test. Down, Down, and Up with no target, then <c>Update</c> and <c>Draw</c>.
    /// </summary>
    private static void FocusStep(Scene scene)
    {
        UiContext context = scene.Context;
        context.PushEvent(in scene.Down);
        context.PushEvent(in scene.Down);
        context.PushEvent(in scene.Up);
        context.Update(0.016f);
        context.Draw(scene.Renderer);
    }

    /// <summary>
    /// The measured body of the fifth test. Frame 1: no focus, pad slot 0 with <c>X</c> and <c>A</c> down and stick
    /// values, W down, Space down, a pointer move, and a scroll outside the panel, then <c>Update</c> and the five
    /// queries. Frame 2: the pad with no button, W up, Space up, <c>Update</c>, the queries again, and <c>Draw</c>.
    /// </summary>
    private static void QueryStep(Scene scene)
    {
        UiContext context = scene.Context;
        UiActionMap map = scene.Map;
        context.ClearFocus();

        context.SetGamepad(0, in scene.PadDown);
        context.PushEvent(in scene.W);
        context.PushEvent(in scene.Space);
        context.PushEvent(in scene.MoveOutside);
        context.PushEvent(in scene.ScrollOutside);
        context.Update(0.016f);
        ReadQueries(scene);

        context.SetGamepad(0, in scene.PadUp);
        context.PushEvent(in scene.WRelease);
        context.PushEvent(in scene.SpaceRelease);
        context.Update(0.016f);
        ReadQueries(scene);
        context.Draw(scene.Renderer);
    }

    /// <summary>Runs the five queries and adds the results to the sum fields of the scene.</summary>
    private static void ReadQueries(Scene scene)
    {
        UiActionMap map = scene.Map;
        float2 move = map.ReadVector(MoveAction);
        float2 look = map.ReadVector(LookAction);
        scene.ValueSum += move.X + move.Y + look.X + look.Y + map.ReadAxis(ZoomAction) + map.ReadAxis(ThrottleAction);
        scene.FlagSum += Count(map.WasPressed(FireAction)) + Count(map.IsHeld(FireAction)) + Count(map.WasReleased(FireAction));
    }

    /// <summary>Gives 1 for <see langword="true"/> and 0 for <see langword="false"/>.</summary>
    private static int Count(bool value) => value ? 1 : 0;

    private static void RunKeySteps(Scene scene, int steps)
    {
        for (int i = 0; i < steps; i++)
        {
            KeyStep(scene);
        }
    }

    private static void RunMouseSteps(Scene scene, int steps)
    {
        for (int i = 0; i < steps; i++)
        {
            MouseStep(scene);
        }
    }

    private static void RunGamepadSteps(Scene scene, int steps)
    {
        for (int i = 0; i < steps; i++)
        {
            GamepadStep(scene);
        }
    }

    private static void RunFocusSteps(Scene scene, int steps)
    {
        for (int i = 0; i < steps; i++)
        {
            FocusStep(scene);
        }
    }

    private static void RunQuerySteps(Scene scene, int steps)
    {
        for (int i = 0; i < steps; i++)
        {
            QueryStep(scene);
        }
    }

    /// <summary>An unhandled key, a repeat, an unbound key, and a guarded text key allocate nothing after warm-up.</summary>
    [Fact]
    public void AnUnhandledKeyAllocatesNothing()
    {
        Scene scene = BuildScene();
        RunKeySteps(scene, WarmUpRounds);

        AllocationGate.ReleaseAllocationContext();
        long before = GC.GetAllocatedBytesForCurrentThread();
        RunKeySteps(scene, Iterations);
        long after = GC.GetAllocatedBytesForCurrentThread();

        Assert.Equal(0L, after - before);
    }

    /// <summary>A mouse button over no element, and a click on a button, allocate nothing after warm-up.</summary>
    [Fact]
    public void AMouseButtonOverNoElementAllocatesNothing()
    {
        Scene scene = BuildScene();
        RunMouseSteps(scene, WarmUpRounds);

        AllocationGate.ReleaseAllocationContext();
        long before = GC.GetAllocatedBytesForCurrentThread();
        RunMouseSteps(scene, Iterations);
        long after = GC.GetAllocatedBytesForCurrentThread();

        Assert.Equal(0L, after - before);
    }

    /// <summary>A gamepad push and an invoke by action name allocate nothing after warm-up.</summary>
    [Fact]
    public void AGamepadPushAllocatesNothing()
    {
        Scene scene = BuildScene();
        RunGamepadSteps(scene, WarmUpRounds);

        AllocationGate.ReleaseAllocationContext();
        long before = GC.GetAllocatedBytesForCurrentThread();
        RunGamepadSteps(scene, Iterations);
        long after = GC.GetAllocatedBytesForCurrentThread();

        Assert.Equal(0L, after - before);
    }

    /// <summary>The two focus actions allocate nothing after warm-up.</summary>
    [Fact]
    public void TheFocusActionsAllocateNothing()
    {
        Scene scene = BuildScene();
        RunFocusSteps(scene, WarmUpRounds);

        AllocationGate.ReleaseAllocationContext();
        long before = GC.GetAllocatedBytesForCurrentThread();
        RunFocusSteps(scene, Iterations);
        long after = GC.GetAllocatedBytesForCurrentThread();

        Assert.Equal(0L, after - before);
    }

    /// <summary>The typed frame read and the five queries allocate nothing after warm-up.</summary>
    [Fact]
    public void TheTypedReadAndTheQueriesAllocateNothing()
    {
        Scene scene = BuildScene();
        RunQuerySteps(scene, WarmUpRounds);

        AllocationGate.ReleaseAllocationContext();
        long before = GC.GetAllocatedBytesForCurrentThread();
        RunQuerySteps(scene, Iterations);
        long after = GC.GetAllocatedBytesForCurrentThread();

        Assert.Equal(0L, after - before);
    }

    /// <summary>
    /// The measured bodies do real work. The test reads each count and the focus owner before each body runs, runs one
    /// step of each body, and asserts a fixed increment. An empty body cannot pass.
    /// </summary>
    [Fact]
    public void TheMeasuredBodyDoesRealWork()
    {
        Scene scene = BuildScene();
        Counter[] counters = scene.Counters;

        // The key step: Escape runs two times (one for the button and one for the field), Control+S one time, and W
        // never (the field takes it). The unbound key and the repeat run nothing.
        int escape = counters[0].Calls;
        int controlS = counters[1].Calls;
        int w = counters[2].Calls;
        int f1 = counters[3].Calls;

        KeyStep(scene);

        Assert.Equal(escape + 2, counters[0].Calls);
        Assert.Equal(controlS + 1, counters[1].Calls);
        Assert.Equal(w, counters[2].Calls);
        Assert.Equal(f1, counters[3].Calls);
        Assert.Same(scene.Field, scene.Context.FocusedElement);

        // The mouse step: the right button runs its command one time. The left press hit a button, so the left command
        // does not run.
        int right = counters[10].Calls;
        int middle = counters[11].Calls;
        int left = counters[12].Calls;

        MouseStep(scene);

        Assert.Equal(right + 1, counters[10].Calls);
        Assert.Equal(middle, counters[11].Calls);
        Assert.Equal(left, counters[12].Calls);

        // The gamepad step: A runs its command one time, B never, and the invoke runs the last command one time. The
        // D-pad moves the focus from the first to the second button.
        int a = counters[13].Calls;
        int b = counters[14].Calls;
        int last = counters[CommandCount - 1].Calls;
        Assert.True(scene.Context.SetFocus(scene.First));
        Assert.Same(scene.First, scene.Context.FocusedElement);

        GamepadStep(scene);

        Assert.Equal(a + 1, counters[13].Calls);
        Assert.Equal(b, counters[14].Calls);
        Assert.Equal(last + 1, counters[CommandCount - 1].Calls);
        Assert.Same(scene.Second, scene.Context.FocusedElement);

        // The focus step: from the first button, Down, Down, and Up leave the focus on the second button.
        Assert.True(scene.Context.SetFocus(scene.First));
        Assert.Same(scene.First, scene.Context.FocusedElement);

        FocusStep(scene);

        Assert.Same(scene.Second, scene.Context.FocusedElement);

        // The query step: pad A runs its command one time through the frame read. The five queries add a fixed
        // amount to the sums. Frame 1: Move reads the stick (1, 0) - W and S are both down from the key step, so the
        // four-key part gives 0 - Look reads the first move (260, 238) from the last pointer place, Zoom reads 0.25
        // plus the scroll 3, and Throttle reads 0. Frame 2: W goes up, so Move reads (0, -1) and Throttle reads -1.
        // The flags: Fire reads pressed and held in frame 1, and released in frame 2.
        int aFrameRead = counters[13].Calls;
        float valueBefore = scene.ValueSum;
        int flagBefore = scene.FlagSum;

        QueryStep(scene);

        Assert.Equal(aFrameRead + 1, counters[13].Calls);
        Assert.Equal(500.25, (double)scene.ValueSum - valueBefore, 5);
        Assert.Equal(3, scene.FlagSum - flagBefore);
    }
}
