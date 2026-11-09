using System;
using Brutal.Numerics;
using BrutalUi.Core;
using BrutalUi.Core.Tests;
using Xunit;

namespace BrutalUi.Framework.Tests;

/// <summary>
/// The tests of the Button queries and of the pointer values of <see cref="UiActionMap"/>:
/// <see cref="UiActionMap.WasPressed"/>, <see cref="UiActionMap.IsHeld"/>, <see cref="UiActionMap.WasReleased"/>,
/// <see cref="UiActionMap.BindMouseDelta"/>, <see cref="UiActionMap.BindScroll"/>, and
/// <see cref="UiActionMap.BindKeyAxis"/>.
/// </summary>
/// <remarks>
/// <para>
/// The demo of the phase: the frame read keeps the pressed, held, and released flags of a Button action, the queries
/// read them, and the queries and the commands agree on one press. The pointer delta fills a Vector2 and the scroll
/// fills an Axis, each with a scale and no clamp.
/// </para>
/// <para>
/// Each test builds a context with a <see cref="RecordingRenderer"/> measurer and a viewport of 400 by 300. A test that
/// needs the pointer builds its tree by hand: a root that fills the window, with <see cref="Element.IsPickable"/> set to
/// <see langword="false"/>, so the root takes no press (the layout-root rule). One child of 80 by 24 sits at the origin,
/// and a point outside it hits no element. A test writes a pad with
/// <see cref="UiContext.SetGamepad(int, in UiGamepadState)"/> before the update. GLFW codes: Escape 256, Space 32, S 83,
/// W 87. Each float assert compares one component with a precision of five decimals.
/// </para>
/// </remarks>
public sealed class ActionQueryTests
{
    /// <summary>The GLFW key code of Escape.</summary>
    private const int EscapeKey = 256;

    /// <summary>The GLFW key code of the Space key.</summary>
    private const int SpaceKey = 32;

    /// <summary>The GLFW key code of the S key.</summary>
    private const int SKey = 83;

    /// <summary>The GLFW key code of the W key.</summary>
    private const int WKey = 87;

    /// <summary>The GLFW key code of the left Control key.</summary>
    private const int ControlKey = 341;

    /// <summary>The name of the Button action that the tests fire with.</summary>
    private static ReadOnlySpan<byte> FireAction => "Fire"u8;

    /// <summary>The name of the Button action that the tests save with.</summary>
    private static ReadOnlySpan<byte> SaveAction => "Save"u8;

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

    /// <summary>The name that no map holds.</summary>
    private static ReadOnlySpan<byte> MissingAction => "Nope"u8;

    /// <summary>The handler that adds one to the counter target.</summary>
    private static readonly CommandHandler s_count = static target => ((Counter)target!).Calls++;

    /// <summary>One key press runs the command one time and reads one press flag.</summary>
    [Fact]
    public void AKeyPressInvokesOneTimeAndReadsOnePress()
    {
        Counter counter = new();
        UiContext context = CreateContext();
        UiActionMap map = CreateMap(counter);
        map.AddAction(UiActions.Cancel, CountCommand);
        map.BindKey(UiActions.Cancel, EscapeKey);
        map.Activate(context);
        context.Update(0f);

        Assert.Equal(0, counter.Calls);
        Assert.False(map.WasPressed(UiActions.Cancel));
        Assert.False(map.IsHeld(UiActions.Cancel));
        Assert.False(map.WasReleased(UiActions.Cancel));

        context.PushEvent(Key(EscapeKey));
        context.Update(0f);
        Assert.Equal(1, counter.Calls);
        Assert.True(map.WasPressed(UiActions.Cancel));
        Assert.True(map.IsHeld(UiActions.Cancel));
        Assert.False(map.WasReleased(UiActions.Cancel));

        context.Update(0f);
        Assert.Equal(1, counter.Calls);
        Assert.False(map.WasPressed(UiActions.Cancel));
        Assert.True(map.IsHeld(UiActions.Cancel));

        context.PushEvent(Key(EscapeKey, isRepeat: true));
        context.Update(0f);
        Assert.Equal(1, counter.Calls);
        Assert.False(map.WasPressed(UiActions.Cancel));
        Assert.True(map.IsHeld(UiActions.Cancel));

        context.PushEvent(KeyUp(EscapeKey));
        context.Update(0f);
        Assert.Equal(1, counter.Calls);
        Assert.False(map.WasPressed(UiActions.Cancel));
        Assert.False(map.IsHeld(UiActions.Cancel));
        Assert.True(map.WasReleased(UiActions.Cancel));

        context.Update(0f);
        Assert.False(map.WasReleased(UiActions.Cancel));
    }

    /// <summary>One gamepad press runs the command one time from the frame read and reads one press flag.</summary>
    [Fact]
    public void AGamepadPressInvokesOneTimeAndReadsOnePress()
    {
        Counter counter = new();
        UiContext context = CreateContext();
        UiActionMap map = CreateMap(counter);
        map.AddAction(UiActions.Cancel, CountCommand);
        map.BindGamepadButton(UiActions.Cancel, UiGamepadButtons.B);
        map.Activate(context);

        SetPad(context, UiGamepadButtons.None);
        context.Update(0f);
        Assert.Equal(0, counter.Calls);
        Assert.False(map.WasPressed(UiActions.Cancel));
        Assert.False(map.IsHeld(UiActions.Cancel));

        SetPad(context, UiGamepadButtons.B);
        context.Update(0f);
        Assert.Equal(1, counter.Calls);
        Assert.True(map.WasPressed(UiActions.Cancel));
        Assert.True(map.IsHeld(UiActions.Cancel));
        Assert.False(map.WasReleased(UiActions.Cancel));

        SetPad(context, UiGamepadButtons.B);
        context.Update(0f);
        Assert.Equal(1, counter.Calls);
        Assert.False(map.WasPressed(UiActions.Cancel));
        Assert.True(map.IsHeld(UiActions.Cancel));

        SetPad(context, UiGamepadButtons.None);
        context.Update(0f);
        Assert.Equal(1, counter.Calls);
        Assert.False(map.WasPressed(UiActions.Cancel));
        Assert.False(map.IsHeld(UiActions.Cancel));
        Assert.True(map.WasReleased(UiActions.Cancel));

        SetPad(context, UiGamepadButtons.None);
        context.Update(0f);
        Assert.False(map.WasReleased(UiActions.Cancel));
    }

    /// <summary>A right press over no element runs the command one time and reads one press. A press on a button does neither.</summary>
    [Fact]
    public void AMouseButtonOverNoElementInvokesOneTimeAndReadsOnePress()
    {
        float2 outside = new(300f, 200f);
        float2 onButton = new(40f, 12f);
        UiContext context = CreateContext();
        Element root = AddRoot(context);
        root.IsPickable = false;
        root.Children.Add(CreateButton());
        Counter counter = new();
        UiActionMap map = CreateMap(counter);
        map.AddAction(UiActions.Cancel, CountCommand);
        map.BindMouseButton(UiActions.Cancel, PointerButton.Right);
        map.Activate(context);
        context.Update(0f);

        PointerEvent press = Pointer(UiEventType.PointerDown, outside, PointerButton.Right);
        PointerEvent release = Pointer(UiEventType.PointerUp, outside, PointerButton.Right);
        context.PushEvent(in press);
        context.Update(0f);
        Assert.Equal(1, counter.Calls);
        Assert.True(map.WasPressed(UiActions.Cancel));
        Assert.True(map.IsHeld(UiActions.Cancel));
        Assert.False(context.RoutingGate.PointerOverUi);

        context.PushEvent(in release);
        context.Update(0f);
        Assert.Equal(1, counter.Calls);
        Assert.False(map.WasPressed(UiActions.Cancel));
        Assert.False(map.IsHeld(UiActions.Cancel));
        Assert.True(map.WasReleased(UiActions.Cancel));

        PointerEvent hitPress = Pointer(UiEventType.PointerDown, onButton, PointerButton.Right);
        PointerEvent hitRelease = Pointer(UiEventType.PointerUp, onButton, PointerButton.Right);
        context.PushEvent(in hitPress);
        context.PushEvent(in hitRelease);
        context.Update(0f);
        Assert.Equal(1, counter.Calls);
        Assert.False(map.WasPressed(UiActions.Cancel));
        Assert.False(map.IsHeld(UiActions.Cancel));
        Assert.True(context.RoutingGate.PointerOverUi);
    }

    /// <summary>Two bindings of one Button action hold it together, and the action releases only when the last one stops.</summary>
    [Fact]
    public void TwoBindingsHoldOneButton()
    {
        UiContext context = CreateContext();
        UiActionMap map = CreateMap();
        map.AddAction(FireAction, UiActionType.Button);
        map.BindKey(FireAction, SpaceKey);
        map.BindGamepadButton(FireAction, UiGamepadButtons.A);
        map.Activate(context);

        context.PushEvent(Key(SpaceKey));
        SetPad(context, UiGamepadButtons.None);
        context.Update(0f);
        Assert.True(map.WasPressed(FireAction));
        Assert.True(map.IsHeld(FireAction));
        Assert.False(map.WasReleased(FireAction));

        SetPad(context, UiGamepadButtons.A);
        context.Update(0f);
        Assert.True(map.WasPressed(FireAction));
        Assert.True(map.IsHeld(FireAction));

        context.PushEvent(KeyUp(SpaceKey));
        SetPad(context, UiGamepadButtons.A);
        context.Update(0f);
        Assert.False(map.WasPressed(FireAction));
        Assert.True(map.IsHeld(FireAction));
        Assert.False(map.WasReleased(FireAction));

        SetPad(context, UiGamepadButtons.None);
        context.Update(0f);
        Assert.False(map.IsHeld(FireAction));
        Assert.True(map.WasReleased(FireAction));
    }

    /// <summary>A press and a release in one frame set the pressed flag and the released flag, and leave the held flag false.</summary>
    [Fact]
    public void APressAndAReleaseInOneFrameSetBothFlags()
    {
        UiContext context = CreateContext();
        UiActionMap map = CreateMap();
        map.AddAction(FireAction, UiActionType.Button);
        map.BindKey(FireAction, SpaceKey);
        map.Activate(context);
        context.Update(0f);

        context.PushEvent(Key(SpaceKey));
        context.PushEvent(KeyUp(SpaceKey));
        context.Update(0f);
        Assert.True(map.WasPressed(FireAction));
        Assert.False(map.IsHeld(FireAction));
        Assert.True(map.WasReleased(FireAction));
    }

    /// <summary>
    /// A key binding with Control matches only a frame whose modifiers hold Control, also for the queries. The mouse
    /// limit: a Control mouse bind runs its command on a Ctrl press and reads no press flag, and a mouse bind with no
    /// modifier reads a press flag on a Ctrl press and runs no command. The modifier rule for a held key: a modifier
    /// press releases the action, and a fresh press of the key is needed.
    /// </summary>
    [Fact]
    public void TheModifierRuleAppliesToTheQueries()
    {
        Counter counter = new();
        UiContext context = CreateContext();
        UiActionMap map = CreateMap(counter);
        map.AddAction(SaveAction, CountCommand);
        map.BindKey(SaveAction, SKey, UiModifiers.Control);
        map.Activate(context);
        context.Update(0f);

        context.PushEvent(Key(SKey, UiModifiers.Control));
        context.Update(0f);
        Assert.Equal(1, counter.Calls);
        Assert.True(map.WasPressed(SaveAction));
        Assert.True(map.IsHeld(SaveAction));

        context.PushEvent(KeyUp(SKey, UiModifiers.Control));
        context.Update(0f);
        Assert.False(map.WasPressed(SaveAction));
        Assert.False(map.IsHeld(SaveAction));

        context.PushEvent(Key(SKey));
        context.Update(0f);
        Assert.Equal(1, counter.Calls);
        Assert.False(map.WasPressed(SaveAction));
        Assert.False(map.IsHeld(SaveAction));

        context.PushEvent(KeyUp(SKey));
        context.Update(0f);
        Assert.False(map.WasReleased(SaveAction));

        // The mouse limit, one direction: a Control mouse bind. The slot path reads the modifiers of the pointer event,
        // so the command runs. The frame read has no key event, so the press flag stays off.
        float2 outside = new(300f, 200f);
        Counter mouseCounter = new();
        UiContext mouseContext = CreateContext();
        Element mouseRoot = AddRoot(mouseContext);
        mouseRoot.IsPickable = false;
        UiActionMap mouseMap = CreateMap(mouseCounter);
        mouseMap.AddAction(FireAction, CountCommand);
        mouseMap.BindMouseButton(FireAction, PointerButton.Right, UiModifiers.Control);
        mouseMap.Activate(mouseContext);
        mouseContext.Update(0f);

        PointerEvent controlPress = Pointer(UiEventType.PointerDown, outside, PointerButton.Right, UiModifiers.Control);
        mouseContext.PushEvent(in controlPress);
        mouseContext.Update(0f);
        Assert.Equal(1, mouseCounter.Calls);
        Assert.False(mouseMap.WasPressed(FireAction));

        // The mouse limit, the other direction: a mouse bind with no modifier. The slot path skips the press, and the
        // frame read sets the press flag.
        Counter plainCounter = new();
        UiContext plainContext = CreateContext();
        Element plainRoot = AddRoot(plainContext);
        plainRoot.IsPickable = false;
        UiActionMap plainMap = CreateMap(plainCounter);
        plainMap.AddAction(FireAction, CountCommand);
        plainMap.BindMouseButton(FireAction, PointerButton.Right);
        plainMap.Activate(plainContext);
        plainContext.Update(0f);

        PointerEvent modifiedPress = Pointer(UiEventType.PointerDown, outside, PointerButton.Right, UiModifiers.Control);
        plainContext.PushEvent(in modifiedPress);
        plainContext.Update(0f);
        Assert.Equal(0, plainCounter.Calls);
        Assert.True(plainMap.WasPressed(FireAction));

        // The modifier rule for a held key: the Control press stops the plain Space bind, the latch releases the action,
        // and the release of Control does not bring it back.
        UiContext holdContext = CreateContext();
        UiActionMap holdMap = CreateMap();
        holdMap.AddAction(FireAction, UiActionType.Button);
        holdMap.BindKey(FireAction, SpaceKey);
        holdMap.Activate(holdContext);

        holdContext.PushEvent(Key(SpaceKey));
        holdContext.Update(0f);
        Assert.True(holdMap.IsHeld(FireAction));

        holdContext.PushEvent(Key(ControlKey, UiModifiers.Control));
        holdContext.Update(0f);
        Assert.False(holdMap.IsHeld(FireAction));
        Assert.True(holdMap.WasReleased(FireAction));

        holdContext.PushEvent(KeyUp(ControlKey));
        holdContext.Update(0f);
        Assert.False(holdMap.IsHeld(FireAction));
        Assert.False(holdMap.WasPressed(FireAction));

        holdContext.PushEvent(KeyUp(SpaceKey));
        holdContext.PushEvent(Key(SpaceKey));
        holdContext.Update(0f);
        Assert.True(holdMap.WasPressed(FireAction));
        Assert.True(holdMap.IsHeld(FireAction));
    }

    /// <summary>A focused text field keeps the command and the flags off a text key. A focused button keeps the command and empties the flags.</summary>
    [Fact]
    public void TheGateAndTheTextGuardAgree()
    {
        CommandRegistry registry = new();
        Counter confirmCounter = new();
        Counter cancelCounter = new();
        registry.Register("menu.ok"u8, s_count, confirmCounter);
        registry.Register("menu.cancel"u8, s_count, cancelCounter);
        UiActionMap map = new(new UiActionMap.CreateInfo { Commands = registry });
        map.AddAction(UiActions.Confirm, "menu.ok"u8);
        map.AddAction(UiActions.Cancel, "menu.cancel"u8);
        map.BindKey(UiActions.Confirm, WKey);
        map.BindKey(UiActions.Cancel, EscapeKey);

        UiContext context = CreateContext();
        Element root = AddRoot(context);
        TextField field = new(new TextField.CreateInfo { Capacity = 16 });
        Button button = new(new Button.CreateInfo { Text = "Go" });
        root.Children.Add(field);
        root.Children.Add(button);
        map.Activate(context);
        context.Update(0f);

        Assert.True(context.SetFocus(field));
        context.PushEvent(Key(WKey));
        context.Update(0f);
        Assert.Equal(0, confirmCounter.Calls);
        Assert.False(map.WasPressed(UiActions.Confirm));
        Assert.True(context.RoutingGate.KeyboardFocusInUi);

        Assert.True(context.SetFocus(button));
        context.PushEvent(Key(EscapeKey));
        context.Update(0f);
        Assert.Equal(1, cancelCounter.Calls);
        Assert.False(map.WasPressed(UiActions.Cancel));

        context.PushEvent(KeyUp(EscapeKey));
        context.Update(0f);
        Assert.False(map.WasReleased(UiActions.Cancel));
    }

    /// <summary>The pointer delta and the scroll keep their value free: no clamp, and the gate empties both over the UI.</summary>
    [Fact]
    public void TheMouseDeltaAndTheScrollHaveNoClamp()
    {
        UiContext context = CreateContext();
        Element root = AddRoot(context);
        root.IsPickable = false;
        UiActionMap map = CreateMap();
        map.AddAction(LookAction, UiActionType.Vector2);
        map.AddAction(ZoomAction, UiActionType.Axis);
        map.BindMouseDelta(LookAction, 0.5f);
        map.BindScroll(ZoomAction, 2f);
        map.Activate(context);

        PointerEvent start = Pointer(UiEventType.PointerMove, new float2(100f, 100f));
        context.PushEvent(in start);
        context.Update(0f);
        AssertVector(map.ReadVector(LookAction), 0f, 0f);

        PointerEvent end = Pointer(UiEventType.PointerMove, new float2(140f, 70f));
        context.PushEvent(in end);
        context.Update(0f);
        AssertVector(map.ReadVector(LookAction), 20f, -15f);
        Assert.Equal(0f, map.ReadAxis(ZoomAction), 5);

        ScrollEvent scroll = Scroll(new float2(140f, 70f), new float2(0f, 3f));
        context.PushEvent(in scroll);
        context.Update(0f);
        Assert.Equal(6f, map.ReadAxis(ZoomAction), 5);
        AssertVector(map.ReadVector(LookAction), 0f, 0f);

        UiContext zoomContext = CreateContext();
        Element zoomRoot = AddRoot(zoomContext);
        zoomRoot.IsPickable = false;
        UiActionMap zoomMap = CreateMap();
        zoomMap.AddAction(ZoomAction, UiActionType.Axis);
        zoomMap.BindGamepadAxis(ZoomAction, UiGamepadAxis.RightTrigger);
        zoomMap.BindScroll(ZoomAction, 1f);
        zoomMap.Activate(zoomContext);

        zoomContext.SetGamepad(0, new UiGamepadState(UiGamepadButtons.None, stackalloc float[] { 0f, 0f, 0f, 0f, 0f, 1f }));
        ScrollEvent zoomScroll = Scroll(new float2(50f, 50f), new float2(0f, 3f));
        zoomContext.PushEvent(in zoomScroll);
        zoomContext.Update(0f);
        Assert.Equal(4f, zoomMap.ReadAxis(ZoomAction), 5);

        PointerEvent overUi = Pointer(UiEventType.PointerMove, new float2(200f, 150f));
        root.IsPickable = true;
        context.PushEvent(in overUi);
        context.Update(0f);
        AssertVector(map.ReadVector(LookAction), 0f, 0f);
        Assert.True(context.RoutingGate.PointerOverUi);
    }

    /// <summary>A two-key Axis reads 1 for the positive key, -1 for the negative key, and 0 for both or neither.</summary>
    [Fact]
    public void ATwoKeyAxisReadsMinusOneToOne()
    {
        UiContext context = CreateContext();
        UiActionMap map = CreateMap();
        map.AddAction(ThrottleAction, UiActionType.Axis);
        map.BindKeyAxis(ThrottleAction, SKey, WKey);
        map.Activate(context);
        context.Update(0f);

        context.PushEvent(Key(WKey));
        context.Update(0f);
        Assert.Equal(1f, map.ReadAxis(ThrottleAction), 5);

        context.PushEvent(KeyUp(WKey));
        context.PushEvent(Key(SKey));
        context.Update(0f);
        Assert.Equal(-1f, map.ReadAxis(ThrottleAction), 5);

        context.PushEvent(Key(WKey));
        context.Update(0f);
        Assert.Equal(0f, map.ReadAxis(ThrottleAction), 5);

        context.PushEvent(KeyUp(WKey));
        context.PushEvent(KeyUp(SKey));
        context.Update(0f);
        Assert.Equal(0f, map.ReadAxis(ThrottleAction), 5);

        ArgumentException swappedError = Assert.Throws<ArgumentException>(
            () => map.BindKeyAxis(ThrottleAction, WKey, SKey));
        Assert.Equal("action", swappedError.ParamName);
        Assert.Equal(1, map.ActionCount);
        Assert.Equal(1, map.BindingCount);
    }

    /// <summary>Each Button query and each new bind checks the type of the action, and one rejected call changes no count.</summary>
    [Fact]
    public void TheButtonQueriesCheckTheType()
    {
        UiActionMap map = CreateMap();
        map.AddAction(FireAction, UiActionType.Button);
        map.AddAction(ZoomAction, UiActionType.Axis);
        map.AddAction(MoveAction, UiActionType.Vector2);
        int actionCount = map.ActionCount;
        int bindingCount = map.BindingCount;

        ArgumentException pressedOnAxis = Assert.Throws<ArgumentException>(() => map.WasPressed(ZoomAction));
        Assert.Equal("action", pressedOnAxis.ParamName);

        ArgumentException pressedOnVector = Assert.Throws<ArgumentException>(() => map.WasPressed(MoveAction));
        Assert.Equal("action", pressedOnVector.ParamName);

        ArgumentException heldOnAxis = Assert.Throws<ArgumentException>(() => map.IsHeld(ZoomAction));
        Assert.Equal("action", heldOnAxis.ParamName);

        ArgumentException heldOnVector = Assert.Throws<ArgumentException>(() => map.IsHeld(MoveAction));
        Assert.Equal("action", heldOnVector.ParamName);

        ArgumentException releasedOnAxis = Assert.Throws<ArgumentException>(() => map.WasReleased(ZoomAction));
        Assert.Equal("action", releasedOnAxis.ParamName);

        ArgumentException releasedOnVector = Assert.Throws<ArgumentException>(() => map.WasReleased(MoveAction));
        Assert.Equal("action", releasedOnVector.ParamName);

        ArgumentException axisOnButton = Assert.Throws<ArgumentException>(() => map.ReadAxis(FireAction));
        Assert.Equal("action", axisOnButton.ParamName);

        Assert.False(map.WasPressed(MissingAction));
        Assert.False(map.IsHeld(MissingAction));
        Assert.False(map.WasReleased(MissingAction));
        Assert.Equal(0f, map.ReadAxis(MissingAction), 5);
        AssertVector(map.ReadVector(MissingAction), 0f, 0f);

        ArgumentException deltaOnAxis = Assert.Throws<ArgumentException>(() => map.BindMouseDelta(ZoomAction));
        Assert.Equal("action", deltaOnAxis.ParamName);

        ArgumentException scrollOnVector = Assert.Throws<ArgumentException>(() => map.BindScroll(MoveAction));
        Assert.Equal("action", scrollOnVector.ParamName);

        ArgumentException keyAxisOnVector = Assert.Throws<ArgumentException>(() => map.BindKeyAxis(MoveAction, SKey, WKey));
        Assert.Equal("action", keyAxisOnVector.ParamName);

        ArgumentOutOfRangeException deltaScale = Assert.Throws<ArgumentOutOfRangeException>(
            () => map.BindMouseDelta(MoveAction, float.NaN));
        Assert.Equal("scale", deltaScale.ParamName);

        ArgumentOutOfRangeException scrollScale = Assert.Throws<ArgumentOutOfRangeException>(
            () => map.BindScroll(ZoomAction, float.PositiveInfinity));
        Assert.Equal("scale", scrollScale.ParamName);

        ArgumentException sameKeys = Assert.Throws<ArgumentException>(() => map.BindKeyAxis(ZoomAction, WKey, WKey));
        Assert.Equal("positive", sameKeys.ParamName);

        map.BindKeyAxis(ZoomAction, SKey, WKey);
        Assert.Throws<ArgumentException>(() => map.BindKeyAxis(ZoomAction, SKey, WKey));

        map.BindMouseDelta(MoveAction);
        Assert.Throws<ArgumentException>(() => map.BindMouseDelta(MoveAction, 2f));

        map.BindScroll(ZoomAction);
        Assert.Throws<ArgumentException>(() => map.BindScroll(ZoomAction, 3f));

        Assert.Equal(actionCount, map.ActionCount);
        Assert.Equal(bindingCount + 3, map.BindingCount);
    }

    /// <summary>Writes the buttons of slot 0 of a context, with no axis value.</summary>
    /// <param name="context">The context.</param>
    /// <param name="buttons">The buttons that are down.</param>
    private static void SetPad(UiContext context, UiGamepadButtons buttons)
    {
        context.SetGamepad(0, new UiGamepadState(buttons, []));
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
            registry.Register(CountCommand, s_count, counter);
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
