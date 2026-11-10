using System;
using Brutal.Numerics;
using BrutalUi.Core;
using BrutalUi.Core.Tests;
using Xunit;

namespace BrutalUi.Framework.Tests;

/// <summary>
/// The tests of the active scheme of <see cref="UiActionMap"/>: the device rule of the frame read, the carry of the
/// report across frames, the dead-value rule, and the KeyboardMouse seed of construction and of
/// <see cref="UiActionMap.Activate"/>.
/// </summary>
/// <remarks>
/// <para>
/// The demo of the phase: <see cref="UiActionMap.ActiveScheme"/> reports the device of the last binding that gave a
/// value. A key, a mouse button, a key composite, the pointer delta, and a scroll give KeyboardMouse. A stick, an
/// axis, and a gamepad button give Gamepad. A stored value of exactly 0 gives no value, and the tag of a binding
/// never sets the device.
/// </para>
/// <para>
/// Each test builds a context with a <see cref="RecordingRenderer"/> measurer and a viewport of 400 by 300. A test
/// that needs the pointer builds its tree by hand: a root that fills the window, often with
/// <see cref="Element.IsPickable"/> set to <see langword="false"/> (the layout-root rule). A test writes a pad with
/// <see cref="UiContext.SetGamepad(int, in UiGamepadState)"/> before the update. GLFW codes: Escape 256, W 87, A 65,
/// S 83, D 68. Each float assert compares one component with a precision of five decimals.
/// </para>
/// </remarks>
public sealed class ActiveSchemeTests
{
    /// <summary>The GLFW key code of Escape.</summary>
    private const int EscapeKey = 256;

    /// <summary>The GLFW key code of the W key.</summary>
    private const int WKey = 87;

    /// <summary>The GLFW key code of the A key.</summary>
    private const int AKey = 65;

    /// <summary>The GLFW key code of the S key.</summary>
    private const int SKey = 83;

    /// <summary>The GLFW key code of the D key.</summary>
    private const int DKey = 68;

    /// <summary>The name of the Button action that the tests fire with.</summary>
    private static ReadOnlySpan<byte> FireAction => "Fire"u8;

    /// <summary>The name of the Vector2 action that the tests move with.</summary>
    private static ReadOnlySpan<byte> MoveAction => "Move"u8;

    /// <summary>The name of the Vector2 action that the tests look with.</summary>
    private static ReadOnlySpan<byte> LookAction => "Look"u8;

    /// <summary>The name of the Axis action that the tests zoom with.</summary>
    private static ReadOnlySpan<byte> ZoomAction => "Zoom"u8;

    /// <summary>The name of the Axis action that the tests throttle with.</summary>
    private static ReadOnlySpan<byte> ThrottleAction => "Throttle"u8;

    /// <summary>The name of the command that counts its calls.</summary>
    private static ReadOnlySpan<byte> CountCommand => "count"u8;

    /// <summary>A new map and a fresh activation report KeyboardMouse, and a pad at rest keeps the report.</summary>
    [Fact]
    public void ANewMapAndAFreshActivateReportKeyboardMouse()
    {
        UiContext context = CreateContext();
        UiActionMap map = CreateMap();
        map.AddAction(MoveAction, UiActionType.Vector2);
        map.BindGamepadStick(MoveAction, UiGamepadStick.Left);
        int kb = map.AddScheme(UiScheme.KeyboardMouse);
        int gp = map.AddScheme(UiScheme.Gamepad);
        Assert.Equal(kb, map.ActiveScheme);

        map.Activate(context);
        SetLeftStick(context, 0f, 0f);
        context.Update(0f);
        Assert.Equal(kb, map.ActiveScheme);

        SetLeftStick(context, 0.4f, 0.2f);
        context.Update(0f);
        Assert.Equal(gp, map.ActiveScheme);

        map.Deactivate();
        map.Activate(context);
        Assert.Equal(kb, map.ActiveScheme);

        SetLeftStick(context, 0f, 0f);
        context.Update(0f);
        Assert.Equal(kb, map.ActiveScheme);
    }

    /// <summary>Escape down with a bound command runs the command and the report reads KeyboardMouse.</summary>
    [Fact]
    public void AKeyBindingSetsTheKeyboardScheme()
    {
        Counter counter = new();
        UiContext context = CreateContext();
        UiActionMap map = CreateMap(counter);
        map.AddAction(UiActions.Cancel, CountCommand);
        map.BindKey(UiActions.Cancel, EscapeKey);
        int kb = map.AddScheme(UiScheme.KeyboardMouse);
        map.Activate(context);
        context.Update(0f);
        Assert.Equal(kb, map.ActiveScheme);

        context.PushEvent(Key(EscapeKey));
        context.Update(0f);
        Assert.Equal(1, counter.Calls);
        Assert.True(map.WasPressed(UiActions.Cancel));
        Assert.Equal(kb, map.ActiveScheme);
    }

    /// <summary>A pointer move of (40, -30) and a scroll of (0, 3) outside the panel pull the report to KeyboardMouse.</summary>
    [Fact]
    public void APassivePointerReadsSetTheKeyboardScheme()
    {
        UiContext context = CreateContext();
        Element root = AddRoot(context);
        root.IsPickable = false;
        root.Children.Add(CreateButton());
        UiActionMap map = CreateMap();
        map.AddAction(LookAction, UiActionType.Vector2);
        map.BindMouseDelta(LookAction);
        map.AddAction(ZoomAction, UiActionType.Axis);
        map.BindScroll(ZoomAction);
        map.AddAction(ThrottleAction, UiActionType.Axis);
        map.BindGamepadAxis(ThrottleAction, UiGamepadAxis.LeftTrigger);
        int kb = map.AddScheme(UiScheme.KeyboardMouse);
        int gp = map.AddScheme(UiScheme.Gamepad);
        map.Activate(context);

        PointerEvent anchor = Pointer(UiEventType.PointerMove, new float2(100f, 100f));
        context.PushEvent(in anchor);
        SetLeftTrigger(context, 0f);
        context.Update(0f);
        Assert.Equal(kb, map.ActiveScheme);

        SetLeftTrigger(context, 0.5f);
        context.Update(0f);
        Assert.Equal(gp, map.ActiveScheme);

        PointerEvent move = Pointer(UiEventType.PointerMove, new float2(140f, 70f));
        context.PushEvent(in move);
        SetLeftTrigger(context, 0f);
        context.Update(0f);
        AssertVector(map.ReadVector(LookAction), 40f, -30f);
        Assert.Equal(kb, map.ActiveScheme);

        SetLeftTrigger(context, 0.5f);
        context.Update(0f);
        Assert.Equal(gp, map.ActiveScheme);

        ScrollEvent scroll = Scroll(new float2(150f, 80f), new float2(0f, 3f));
        context.PushEvent(in scroll);
        SetLeftTrigger(context, 0f);
        context.Update(0f);
        Assert.Equal(3f, map.ReadAxis(ZoomAction), 5);
        Assert.Equal(kb, map.ActiveScheme);

        // A scale of 0 multiplies the value and not the device test, so a real scroll still reports KeyboardMouse.
        UiContext scaleContext = CreateContext();
        Element scaleRoot = AddRoot(scaleContext);
        scaleRoot.IsPickable = false;
        scaleRoot.Children.Add(CreateButton());
        UiActionMap scaleMap = CreateMap();
        scaleMap.AddAction(ZoomAction, UiActionType.Axis);
        scaleMap.BindScroll(ZoomAction, 0f);
        scaleMap.AddAction(MoveAction, UiActionType.Vector2);
        scaleMap.BindGamepadStick(MoveAction, UiGamepadStick.Left);
        int scaleKb = scaleMap.AddScheme(UiScheme.KeyboardMouse);
        int scaleGp = scaleMap.AddScheme(UiScheme.Gamepad);
        scaleMap.Activate(scaleContext);

        SetLeftStick(scaleContext, 0.5f, 0f);
        scaleContext.Update(0f);
        Assert.Equal(scaleGp, scaleMap.ActiveScheme);

        ScrollEvent zeroScroll = Scroll(new float2(150f, 80f), new float2(0f, 3f));
        scaleContext.PushEvent(in zeroScroll);
        SetLeftStick(scaleContext, 0f, 0f);
        scaleContext.Update(0f);
        Assert.Equal(0f, scaleMap.ReadAxis(ZoomAction), 5);
        Assert.Equal(scaleKb, scaleMap.ActiveScheme);
    }

    /// <summary>The left stick above rest gives Gamepad, and the stick at rest keeps the Gamepad report.</summary>
    [Fact]
    public void AStickAboveRestSetsTheGamepadScheme()
    {
        UiContext context = CreateContext();
        UiActionMap map = CreateMap();
        map.AddAction(MoveAction, UiActionType.Vector2);
        map.BindKeyVector(MoveAction, WKey, AKey, SKey, DKey);
        map.BindGamepadStick(MoveAction, UiGamepadStick.Left);
        int kb = map.AddScheme(UiScheme.KeyboardMouse);
        int gp = map.AddScheme(UiScheme.Gamepad);
        map.Activate(context);

        SetLeftStick(context, 0f, 0f);
        context.Update(0f);
        Assert.Equal(kb, map.ActiveScheme);

        SetLeftStick(context, 0.4f, -0.2f);
        context.Update(0f);
        AssertVector(map.ReadVector(MoveAction), 0.4f, 0.2f);
        Assert.Equal(gp, map.ActiveScheme);

        SetLeftStick(context, 0f, 0f);
        context.Update(0f);
        Assert.Equal(gp, map.ActiveScheme);
    }

    /// <summary>Pad A and a left trigger above 0 give Gamepad, and the left trigger of exactly 0 changes nothing.</summary>
    [Fact]
    public void AGamepadButtonSetsTheGamepadScheme()
    {
        UiContext context = CreateContext();
        UiActionMap map = CreateMap();
        map.AddAction(FireAction, UiActionType.Button);
        map.BindGamepadButton(FireAction, UiGamepadButtons.A);
        map.AddAction(ThrottleAction, UiActionType.Axis);
        map.BindGamepadAxis(ThrottleAction, UiGamepadAxis.LeftTrigger);
        int kb = map.AddScheme(UiScheme.KeyboardMouse);
        int gp = map.AddScheme(UiScheme.Gamepad);
        map.Activate(context);

        context.Update(0f);
        Assert.Equal(kb, map.ActiveScheme);

        SetPad(context, UiGamepadButtons.A);
        context.Update(0f);
        Assert.True(map.WasPressed(FireAction));
        Assert.Equal(gp, map.ActiveScheme);

        SetLeftTrigger(context, 0.3f);
        context.Update(0f);
        Assert.Equal(0.3f, map.ReadAxis(ThrottleAction), 5);
        Assert.Equal(gp, map.ActiveScheme);

        SetLeftTrigger(context, 0f);
        context.Update(0f);
        Assert.Equal(gp, map.ActiveScheme);
    }

    /// <summary>The last binding in the order of the bind that gave a value sets the report.</summary>
    [Fact]
    public void TheLastBindingThatGaveAValueWins()
    {
        int kb1;
        int gp1;
        {
            Counter counter = new();
            UiContext context = CreateContext();
            UiActionMap map = CreateMap(counter);
            map.AddAction(UiActions.Cancel, CountCommand);
            map.BindKey(UiActions.Cancel, EscapeKey);
            map.BindGamepadButton(UiActions.Cancel, UiGamepadButtons.B);
            kb1 = map.AddScheme(UiScheme.KeyboardMouse);
            gp1 = map.AddScheme(UiScheme.Gamepad);
            map.Activate(context);

            context.PushEvent(Key(EscapeKey));
            SetPad(context, UiGamepadButtons.B);
            context.Update(0f);
            Assert.Equal(2, counter.Calls);
            Assert.Equal(gp1, map.ActiveScheme);
        }

        int kb2;
        int gp2;
        {
            Counter counter = new();
            UiContext context = CreateContext();
            UiActionMap map = CreateMap(counter);
            map.AddAction(UiActions.Cancel, CountCommand);
            map.BindGamepadButton(UiActions.Cancel, UiGamepadButtons.B);
            map.BindKey(UiActions.Cancel, EscapeKey);
            kb2 = map.AddScheme(UiScheme.KeyboardMouse);
            gp2 = map.AddScheme(UiScheme.Gamepad);
            map.Activate(context);

            context.PushEvent(Key(EscapeKey));
            SetPad(context, UiGamepadButtons.B);
            context.Update(0f);
            Assert.Equal(2, counter.Calls);
            Assert.Equal(kb2, map.ActiveScheme);
        }
    }

    /// <summary>A dead value gives no value: a summed-out composite, a gated key, and a gated pointer move keep the report.</summary>
    [Fact]
    public void ADeadValueSetsNoScheme()
    {
        // W and S together give (0, 0), so the composite gives no value.
        UiContext deadContext = CreateContext();
        UiActionMap deadMap = CreateMap();
        deadMap.AddAction(MoveAction, UiActionType.Vector2);
        deadMap.BindKeyVector(MoveAction, WKey, AKey, SKey, DKey);
        int kb = deadMap.AddScheme(UiScheme.KeyboardMouse);
        deadMap.Activate(deadContext);
        deadContext.Update(0f);
        Assert.Equal(kb, deadMap.ActiveScheme);

        deadContext.PushEvent(Key(WKey));
        deadContext.PushEvent(Key(SKey));
        deadContext.Update(0f);
        AssertVector(deadMap.ReadVector(MoveAction), 0f, 0f);
        Assert.Equal(kb, deadMap.ActiveScheme);

        // A focused Button closes the key gate, so the gated key gives no value.
        UiContext gatedContext = CreateContext();
        Element gatedRoot = AddRoot(gatedContext);
        gatedRoot.IsPickable = false;
        Button button = CreateButton();
        gatedRoot.Children.Add(button);
        UiActionMap gatedMap = CreateMap();
        gatedMap.AddAction(MoveAction, UiActionType.Vector2);
        gatedMap.BindKeyVector(MoveAction, WKey, AKey, SKey, DKey);
        int kb2 = gatedMap.AddScheme(UiScheme.KeyboardMouse);
        gatedMap.Activate(gatedContext);
        gatedContext.Update(0f);
        Assert.True(gatedContext.SetFocus(button));

        gatedContext.PushEvent(Key(WKey));
        gatedContext.Update(0f);
        AssertVector(gatedMap.ReadVector(MoveAction), 0f, 0f);
        Assert.Equal(kb2, gatedMap.ActiveScheme);

        // A pointer move over a pickable root closes the pointer gate, so the delta gives no value.
        UiContext pickContext = CreateContext();
        AddRoot(pickContext);
        UiActionMap pickMap = CreateMap();
        pickMap.AddAction(LookAction, UiActionType.Vector2);
        pickMap.BindMouseDelta(LookAction);
        int kb3 = pickMap.AddScheme(UiScheme.KeyboardMouse);
        pickMap.Activate(pickContext);
        pickContext.Update(0f);

        PointerEvent anchor = Pointer(UiEventType.PointerMove, new float2(100f, 100f));
        pickContext.PushEvent(in anchor);
        pickContext.Update(0f);
        Assert.Equal(kb3, pickMap.ActiveScheme);

        PointerEvent move = Pointer(UiEventType.PointerMove, new float2(140f, 70f));
        pickContext.PushEvent(in move);
        pickContext.Update(0f);
        AssertVector(pickMap.ReadVector(LookAction), 0f, 0f);
        Assert.Equal(kb3, pickMap.ActiveScheme);
    }

    /// <summary>A held input re-asserts its device in each frame, and a held key with a held stick gives the later bind.</summary>
    [Fact]
    public void AHeldInputKeepsItsSchemeEachFrame()
    {
        // The W bind comes first, so the held stick is the later bind.
        UiContext context = CreateContext();
        UiActionMap map = CreateMap();
        map.AddAction(FireAction, UiActionType.Button);
        map.BindKey(FireAction, WKey);
        map.AddAction(MoveAction, UiActionType.Vector2);
        map.BindGamepadStick(MoveAction, UiGamepadStick.Left);
        int kb = map.AddScheme(UiScheme.KeyboardMouse);
        int gp = map.AddScheme(UiScheme.Gamepad);
        map.Activate(context);

        context.PushEvent(Key(WKey));
        SetLeftStick(context, 0f, 0f);
        context.Update(0f);
        Assert.Equal(kb, map.ActiveScheme);
        context.Update(0f);
        Assert.Equal(kb, map.ActiveScheme);
        context.Update(0f);
        Assert.Equal(kb, map.ActiveScheme);

        context.PushEvent(KeyUp(WKey));
        SetLeftStick(context, 0.5f, 0f);
        context.Update(0f);
        Assert.Equal(gp, map.ActiveScheme);
        context.Update(0f);
        Assert.Equal(gp, map.ActiveScheme);
        context.Update(0f);
        Assert.Equal(gp, map.ActiveScheme);

        context.PushEvent(Key(WKey));
        context.Update(0f);
        Assert.Equal(gp, map.ActiveScheme);

        // The stick bind comes first, so the held W is the later bind.
        UiContext otherContext = CreateContext();
        UiActionMap otherMap = CreateMap();
        otherMap.AddAction(MoveAction, UiActionType.Vector2);
        otherMap.BindGamepadStick(MoveAction, UiGamepadStick.Left);
        otherMap.AddAction(FireAction, UiActionType.Button);
        otherMap.BindKey(FireAction, WKey);
        int kb2 = otherMap.AddScheme(UiScheme.KeyboardMouse);
        otherMap.Activate(otherContext);

        SetLeftStick(otherContext, 0.5f, 0f);
        otherContext.PushEvent(Key(WKey));
        otherContext.Update(0f);
        Assert.Equal(0.5f, otherMap.ReadVector(MoveAction).X, 5);
        Assert.Equal(kb2, otherMap.ActiveScheme);
    }

    /// <summary>A stick move sets Gamepad, and two frames with no edge and no key keep Gamepad.</summary>
    [Fact]
    public void TheReportCarriesWithNoInput()
    {
        UiContext context = CreateContext();
        UiActionMap map = CreateMap();
        map.AddAction(MoveAction, UiActionType.Vector2);
        map.BindGamepadStick(MoveAction, UiGamepadStick.Left);
        int kb = map.AddScheme(UiScheme.KeyboardMouse);
        int gp = map.AddScheme(UiScheme.Gamepad);
        map.Activate(context);

        SetLeftStick(context, 0.4f, 0.2f);
        context.Update(0f);
        Assert.Equal(gp, map.ActiveScheme);

        SetLeftStick(context, 0f, 0f);
        context.Update(0f);
        Assert.Equal(gp, map.ActiveScheme);

        context.Update(0f);
        Assert.Equal(gp, map.ActiveScheme);
    }

    /// <summary>Writes the buttons of slot 0 of a context, with no axis value.</summary>
    /// <param name="context">The context.</param>
    /// <param name="buttons">The buttons that are down.</param>
    private static void SetPad(UiContext context, UiGamepadButtons buttons)
    {
        context.SetGamepad(0, new UiGamepadState(buttons, []));
    }

    /// <summary>Writes the left stick of slot 0, with no button down.</summary>
    /// <param name="context">The context.</param>
    /// <param name="x">The left stick X value.</param>
    /// <param name="y">The left stick Y value.</param>
    private static void SetLeftStick(UiContext context, float x, float y)
    {
        context.SetGamepad(0, new UiGamepadState(UiGamepadButtons.None, stackalloc float[] { x, y, 0f, 0f, 0f, 0f }));
    }

    /// <summary>Writes the left trigger of slot 0, with no button down.</summary>
    /// <param name="context">The context.</param>
    /// <param name="value">The left trigger value.</param>
    private static void SetLeftTrigger(UiContext context, float value)
    {
        context.SetGamepad(0, new UiGamepadState(UiGamepadButtons.None, stackalloc float[] { 0f, 0f, 0f, 0f, value, 0f }));
    }

    /// <summary>Asserts the two components of a vector with a precision of five decimals.</summary>
    /// <param name="value">The value to check.</param>
    /// <param name="x">The expected X component.</param>
    /// <param name="y">The expected Y component.</param>
    private static void AssertVector(float2 value, float x, float y)
    {
        Assert.Equal(x, value.X, 5);
        Assert.Equal(y, value.Y, 5);
    }

    /// <summary>Creates a context with a measurer and a viewport of 400 by 300.</summary>
    /// <returns>The context.</returns>
    private static UiContext CreateContext()
    {
        return new UiContext(new UiContext.CreateInfo { Measurer = new RecordingRenderer() })
        {
            ViewportSize = new float2(400, 300),
        };
    }

    /// <summary>Creates a map on a new registry that holds one counter command.</summary>
    /// <param name="counter">The target of the command, or <see langword="null"/> for no command.</param>
    /// <returns>The map with no action.</returns>
    private static UiActionMap CreateMap(Counter? counter = null)
    {
        CommandRegistry registry = new();
        if (counter is not null)
        {
            registry.Register(CountCommand, target => ((Counter)target!).Calls++, counter);
        }

        return new UiActionMap(new UiActionMap.CreateInfo { Commands = registry });
    }

    /// <summary>Adds a pickable root of 400 by 300 to a context.</summary>
    /// <param name="context">The context.</param>
    /// <returns>The root.</returns>
    private static Element AddRoot(UiContext context)
    {
        Element root = new();
        root.Inline.FlexDirection = FlexDirection.Column;
        root.Inline.AlignItems = Align.FlexStart;
        root.Inline.Width = 400;
        root.Inline.Height = 300;
        root.IsPickable = true;
        context.AddRoot(root);
        return root;
    }

    /// <summary>Creates a button of 80 by 24.</summary>
    /// <returns>The button.</returns>
    private static Button CreateButton()
    {
        Button button = new(new Button.CreateInfo { Text = "Go" });
        button.Inline.Width = 80;
        button.Inline.Height = 24;
        return button;
    }

    /// <summary>Creates a pointer event with no target.</summary>
    /// <param name="type">The pointer type.</param>
    /// <param name="position">The position in viewport pixels.</param>
    /// <param name="button">The button.</param>
    /// <param name="modifiers">The modifier keys that are down.</param>
    /// <returns>The event.</returns>
    private static PointerEvent Pointer(UiEventType type, float2 position, PointerButton button = PointerButton.Left, UiModifiers modifiers = UiModifiers.None)
    {
        return new PointerEvent
        {
            Header = new UiEventHeader(type, 0d, null),
            Position = position,
            Button = button,
            Modifiers = modifiers,
        };
    }

    /// <summary>Creates a scroll event with no target.</summary>
    /// <param name="position">The position in viewport pixels.</param>
    /// <param name="delta">The scroll amount.</param>
    /// <returns>The event.</returns>
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

    /// <summary>Creates a key down event with no target.</summary>
    /// <param name="key">The GLFW key code.</param>
    /// <param name="modifiers">The modifier keys.</param>
    /// <param name="isRepeat"><see langword="true"/> for a key repeat.</param>
    /// <returns>The event.</returns>
    private static KeyEvent Key(int key, UiModifiers modifiers = UiModifiers.None, bool isRepeat = false)
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

    /// <summary>Creates a key up event with no target.</summary>
    /// <param name="key">The GLFW key code.</param>
    /// <param name="modifiers">The modifier keys.</param>
    /// <returns>The event.</returns>
    private static KeyEvent KeyUp(int key, UiModifiers modifiers = UiModifiers.None)
    {
        return new KeyEvent
        {
            Header = new UiEventHeader(UiEventType.KeyUp, 0d, null),
            Key = key,
            ScanCode = 0,
            Modifiers = modifiers,
            IsRepeat = false,
        };
    }

    /// <summary>A target that counts the calls of its command.</summary>
    private sealed class Counter
    {
        /// <summary>The count of calls.</summary>
        public int Calls;
    }
}
