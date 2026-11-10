using System;
using Brutal.Numerics;
using BrutalUi.Core;
using BrutalUi.Core.Tests;
using Xunit;

namespace BrutalUi.Framework.Tests;

/// <summary>
/// The tests of the change slot of <see cref="UiActionMap"/>. The map calls <see cref="UiActionMap.OnSchemeChanged"/>
/// at the end of the frame read, inside <see cref="UiContext.Update(float)"/>, with the map and the new scheme id, and
/// only when the scheme that it reports changed.
/// </summary>
/// <remarks>
/// <para>
/// Each test builds a context with a <see cref="RecordingRenderer"/> measurer and a viewport of 400 by 300, and a map
/// that it activates on that context. A test writes a pad with <see cref="UiContext.SetGamepad(int, in UiGamepadState)"/>
/// before the update. A handler is one <see cref="static readonly"/> <see cref="UiSchemeChangedHandler"/> over a private
/// static field, because a slot takes no target. GLFW codes: W 87, A 65, S 83, D 68. Each float assert compares one
/// component with a precision of five decimals.
/// </para>
/// <para>
/// The lock of the map arrived in the phase that followed this one, and each rule that the lock completes has both its
/// halves here: <see cref="AHandlerChangeTakesEffectNextFrame"/> checks a bind that the handler adds and a lock that the
/// handler sets, and <see cref="AFirstUpdateAfterActivateCallsWithTheLockedScheme"/> checks the reset of the report in
/// <see cref="UiActionMap.Activate"/> and the locked report after that reset.
/// </para>
/// </remarks>
public sealed class SchemeChangedSlotTests
{
    /// <summary>The GLFW key code of the W key.</summary>
    private const int WKey = 87;

    /// <summary>The GLFW key code of the A key.</summary>
    private const int AKey = 65;

    /// <summary>The GLFW key code of the S key.</summary>
    private const int SKey = 83;

    /// <summary>The GLFW key code of the D key.</summary>
    private const int DKey = 68;

    /// <summary>The name of the Vector2 action that the tests move with.</summary>
    private static ReadOnlySpan<byte> MoveAction => "Move"u8;

    /// <summary>The name of the Axis action that the tests zoom with.</summary>
    private static ReadOnlySpan<byte> ZoomAction => "Zoom"u8;

    /// <summary>The name of the Axis action that the handler binds inside the read.</summary>
    private static ReadOnlySpan<byte> ThrottleAction => "Throttle"u8;

    /// <summary>The count of calls of the handler of <see cref="SchemeChangedSlotTests"/>.</summary>
    private static int s_calls;

    /// <summary>The scheme id that the last call reported.</summary>
    private static int s_lastScheme;

    /// <summary>The map that the last call got.</summary>
    private static UiActionMap? s_lastMap;

    /// <summary>The count of calls of handler A.</summary>
    private static int s_callsA;

    /// <summary>The count of calls of handler B.</summary>
    private static int s_callsB;

    /// <summary>The value of <see cref="MoveAction"/> that the handler read inside the call.</summary>
    private static float2 s_vector;

    /// <summary>The value of <see cref="ZoomAction"/> that the handler read inside the call.</summary>
    private static float s_axis;

    /// <summary>The value of <see cref="ThrottleAction"/> that the handler read inside the call, after its own bind.</summary>
    private static float s_axisAtCall;

    /// <summary>The handler that records the map, the scheme, and the two finished values of the frame.</summary>
    private static readonly UiSchemeChangedHandler s_recordHandler = static (map, scheme) =>
    {
        s_calls++;
        s_lastMap = map;
        s_lastScheme = scheme;
        s_vector = map.ReadVector(MoveAction);
        s_axis = map.ReadAxis(ZoomAction);
    };

    /// <summary>The handler that counts its calls.</summary>
    private static readonly UiSchemeChangedHandler s_countHandler = static (map, scheme) => s_calls++;

    /// <summary>Handler A, which counts its calls.</summary>
    private static readonly UiSchemeChangedHandler s_handlerA = static (map, scheme) => s_callsA++;

    /// <summary>Handler B, which counts its calls and records the map and the scheme.</summary>
    private static readonly UiSchemeChangedHandler s_handlerB = static (map, scheme) =>
    {
        s_callsB++;
        s_lastMap = map;
        s_lastScheme = scheme;
    };

    /// <summary>The handler that adds one bind inside the read and records what it read there.</summary>
    private static readonly UiSchemeChangedHandler s_bindHandler = static (map, scheme) =>
    {
        s_calls++;
        s_lastScheme = scheme;
        s_axisAtCall = map.ReadAxis(ThrottleAction);
        map.BindGamepadAxis(ThrottleAction, UiGamepadAxis.LeftTrigger);
    };

    /// <summary>
    /// The handler that records the finished vector of the frame and then locks the map to Gamepad, so the test can see
    /// that the lock takes effect at the next frame.
    /// </summary>
    private static readonly UiSchemeChangedHandler s_lockHandler = static (map, scheme) =>
    {
        s_calls++;
        s_lastScheme = scheme;
        s_vector = map.ReadVector(MoveAction);
        map.SetScheme(map.AddScheme(UiScheme.Gamepad));
    };

    /// <summary>The handler that throws, for the exception rule of the slot.</summary>
    private static readonly UiSchemeChangedHandler s_throwHandler = static (map, scheme) => throw new InvalidOperationException("The glyph table is missing.");

    /// <summary>
    /// The slot runs at the end of the read in each <see cref="UiContext.Update(float)"/>, so the handler sees the
    /// finished values of the frame.
    /// </summary>
    [Fact]
    public void TheSlotRunsAfterTheReadInEachUpdate()
    {
        s_calls = 0;
        UiContext context = CreateContext();
        UiActionMap map = CreateMap();
        map.AddAction(MoveAction, UiActionType.Vector2);
        map.BindKeyVector(MoveAction, WKey, AKey, SKey, DKey);
        map.BindGamepadStick(MoveAction, UiGamepadStick.Left);
        map.AddAction(ZoomAction, UiActionType.Axis);
        map.BindGamepadAxis(ZoomAction, UiGamepadAxis.LeftTrigger);
        int gp = map.AddScheme(UiScheme.Gamepad);
        map.OnSchemeChanged = s_recordHandler;
        map.Activate(context);

        context.PushEvent(Key(WKey));
        SetPad(context, 0.5f, 0f, 0.3f);
        context.Update(0f);

        Assert.Equal(1, s_calls);
        Assert.Equal(gp, s_lastScheme);
        Assert.Equal(gp, map.ActiveScheme);

        // The stick and the key give the clamped sum (0.5, 1), clamped to a length of 1.
        Assert.Equal(0.44721f, s_vector.X, 5);
        Assert.Equal(0.89443f, s_vector.Y, 5);
        Assert.Equal(0.3f, s_axis, 5);

        // The value that the handler read is the value that the frame keeps.
        Assert.Equal(s_vector.X, map.ReadVector(MoveAction).X, 5);
        Assert.Equal(s_axis, map.ReadAxis(ZoomAction), 5);
    }

    /// <summary>One stick move gives one call with the Gamepad id, and the same stick value gives no further call.</summary>
    [Fact]
    public void TheSlotCallsOneTimePerChange()
    {
        s_calls = 0;
        UiContext context = CreateContext();
        UiActionMap map = CreateMap();
        map.AddAction(MoveAction, UiActionType.Vector2);
        map.BindGamepadStick(MoveAction, UiGamepadStick.Left);
        int gp = map.AddScheme(UiScheme.Gamepad);
        map.OnSchemeChanged = s_countHandler;
        map.Activate(context);

        SetPad(context, 0.5f, 0f, 0f);
        context.Update(0f);
        Assert.Equal(1, s_calls);
        Assert.Equal(gp, map.ActiveScheme);

        context.Update(0f);
        context.Update(0f);
        context.Update(0f);
        Assert.Equal(1, s_calls);
    }

    /// <summary>No change gives no call: no handler and a null handler do nothing, and a key-only frame calls nothing.</summary>
    [Fact]
    public void TheSlotDoesNotCallWhenTheSchemeDoesNotChange()
    {
        s_calls = 0;

        UiContext plainContext = CreateContext();
        UiActionMap plainMap = CreateMap();
        plainMap.AddAction(MoveAction, UiActionType.Vector2);
        plainMap.BindKeyVector(MoveAction, WKey, AKey, SKey, DKey);
        plainMap.Activate(plainContext);
        plainContext.PushEvent(Key(WKey));
        plainContext.Update(0f);
        plainContext.Update(0f);
        Assert.Equal(plainMap.AddScheme(UiScheme.KeyboardMouse), plainMap.ActiveScheme);

        UiContext nullContext = CreateContext();
        UiActionMap nullMap = CreateMap();
        nullMap.AddAction(MoveAction, UiActionType.Vector2);
        nullMap.BindKeyVector(MoveAction, WKey, AKey, SKey, DKey);
        nullMap.OnSchemeChanged = s_countHandler;
        nullMap.Activate(nullContext);
        nullMap.OnSchemeChanged = null;
        nullContext.PushEvent(Key(WKey));
        nullContext.Update(0f);
        nullContext.Update(0f);
        Assert.Equal(0, s_calls);

        // A key-only frame keeps the seeded KeyboardMouse report, so the slot calls nothing.
        UiContext heldContext = CreateContext();
        UiActionMap heldMap = CreateMap();
        heldMap.AddAction(MoveAction, UiActionType.Vector2);
        heldMap.BindKeyVector(MoveAction, WKey, AKey, SKey, DKey);
        heldMap.OnSchemeChanged = s_countHandler;
        heldMap.Activate(heldContext);
        heldContext.PushEvent(Key(WKey));
        heldContext.Update(0f);
        heldContext.Update(0f);
        Assert.Equal(0, s_calls);
    }

    /// <summary>A write with <c>=</c> replaces the handler, and the new handler gets the map instance and the new id.</summary>
    [Fact]
    public void TheSlotTakesOneHandlerWrittenWithEquals()
    {
        s_callsA = 0;
        s_callsB = 0;
        UiContext context = CreateContext();
        UiActionMap map = CreateMap();
        map.AddAction(MoveAction, UiActionType.Vector2);
        map.BindKeyVector(MoveAction, WKey, AKey, SKey, DKey);
        map.BindGamepadStick(MoveAction, UiGamepadStick.Left);
        int gp = map.AddScheme(UiScheme.Gamepad);
        map.OnSchemeChanged = s_handlerA;
        map.OnSchemeChanged = s_handlerB;
        map.Activate(context);

        SetPad(context, 0.5f, 0f, 0f);
        context.Update(0f);

        Assert.Equal(0, s_callsA);
        Assert.Equal(1, s_callsB);
        Assert.Same(map, s_lastMap);
        Assert.Equal(gp, s_lastScheme);
    }

    /// <summary>
    /// A bind that the handler adds, and a lock that the handler sets, both take effect at the next frame, because the
    /// read copied its arrays and its counts to locals before it scanned, and the report of the frame stays the value
    /// that the unlocked read made.
    /// </summary>
    [Fact]
    public void AHandlerChangeTakesEffectNextFrame()
    {
        s_calls = 0;
        UiContext context = CreateContext();
        UiActionMap map = CreateMap();
        map.AddAction(MoveAction, UiActionType.Vector2);
        map.BindGamepadStick(MoveAction, UiGamepadStick.Left);
        map.AddAction(ThrottleAction, UiActionType.Axis);
        int gp = map.AddScheme(UiScheme.Gamepad);
        map.OnSchemeChanged = s_bindHandler;
        map.Activate(context);

        SetPad(context, 0.5f, 0f, 0.3f);
        context.Update(0f);

        // The frame that fired the call keeps the value that the read made before the bind.
        Assert.Equal(1, s_calls);
        Assert.Equal(gp, s_lastScheme);
        Assert.Equal(0f, s_axisAtCall, 5);
        Assert.Equal(0f, map.ReadAxis(ThrottleAction), 5);

        // The next frame reads the new binding.
        context.Update(0f);
        Assert.Equal(1, s_calls);
        Assert.Equal(0.3f, map.ReadAxis(ThrottleAction), 5);

        // The lock half: a SetScheme inside the handler takes effect at the next frame.
        s_calls = 0;
        s_lastScheme = -1;
        UiContext lockContext = CreateContext();
        UiActionMap lockMap = CreateMap();
        int lockKb = lockMap.AddScheme(UiScheme.KeyboardMouse);
        int lockGp = lockMap.AddScheme(UiScheme.Gamepad);
        lockMap.AddAction(MoveAction, UiActionType.Vector2);
        lockMap.BindKeyVector(MoveAction, WKey, AKey, SKey, DKey, lockKb);
        lockMap.BindGamepadStick(MoveAction, UiGamepadStick.Left, lockGp);
        lockMap.OnSchemeChanged = s_lockHandler;
        lockMap.Activate(lockContext);

        // A keyboard-only frame keeps the seeded report, so the handler runs nothing.
        lockContext.PushEvent(Key(WKey));
        lockContext.Update(0f);
        Assert.Equal(0, s_calls);
        Assert.Equal(lockKb, lockMap.ActiveScheme);

        // The stick moves the report, so the handler runs once and sets the lock. The frame keeps the unlocked value:
        // the composite (0, 1) and the stick (0.5, 0) clamp to a length of 1.
        SetPad(lockContext, 0.5f, 0f, 0f);
        lockContext.Update(0f);
        Assert.Equal(1, s_calls);
        Assert.Equal(lockGp, s_lastScheme);
        Assert.Equal(0.44721f, s_vector.X, 5);
        Assert.Equal(0.89443f, s_vector.Y, 5);
        Assert.Equal(lockGp, lockMap.LockedScheme);
        Assert.Equal(0.44721f, lockMap.ReadVector(MoveAction).X, 5);

        // The next frame drops the keyboard value of the held W, because the lock filters the composite.
        SetPad(lockContext, 0f, 0f, 0f);
        lockContext.Update(0f);
        Assert.Equal(1, s_calls);
        Assert.Equal(lockGp, lockMap.ActiveScheme);
        Assert.Equal(0f, lockMap.ReadVector(MoveAction).X, 5);
        Assert.Equal(0f, lockMap.ReadVector(MoveAction).Y, 5);
    }

    /// <summary>
    /// <see cref="UiActionMap.Activate"/> resets the report, so the first update after it calls one time, and a second
    /// update calls nothing. A locked map keeps its lock across the reset, and the first update calls with the locked id.
    /// </summary>
    [Fact]
    public void AFirstUpdateAfterActivateCallsWithTheLockedScheme()
    {
        s_calls = 0;
        s_lastScheme = -1;
        UiContext context = CreateContext();
        UiActionMap map = CreateMap();
        map.AddAction(MoveAction, UiActionType.Vector2);
        map.BindGamepadStick(MoveAction, UiGamepadStick.Left);
        int kb = map.AddScheme(UiScheme.KeyboardMouse);
        int gp = map.AddScheme(UiScheme.Gamepad);
        map.OnSchemeChanged = s_countHandler;
        map.Activate(context);

        SetPad(context, 0.5f, 0f, 0f);
        context.Update(0f);
        Assert.Equal(1, s_calls);
        Assert.Equal(gp, map.ActiveScheme);

        context.Update(0f);
        Assert.Equal(1, s_calls);

        map.Deactivate();
        map.Activate(context);
        Assert.Equal(kb, map.ActiveScheme);

        context.Update(0f);
        Assert.Equal(2, s_calls);
        Assert.Equal(gp, map.ActiveScheme);

        context.Update(0f);
        Assert.Equal(2, s_calls);

        // The locked half: an activate keeps the lock and resets the report, so the first update calls once with the
        // locked id, and a second update calls nothing.
        map.SetScheme(gp);
        map.Deactivate();
        map.Activate(context);
        Assert.Equal(gp, map.LockedScheme);
        Assert.Equal(gp, map.ActiveScheme);

        context.Update(0f);
        Assert.Equal(3, s_calls);
        context.Update(0f);
        Assert.Equal(3, s_calls);

        // A clear of the lock returns the report to the device of the frame read, which is Gamepad here, so nothing calls.
        map.SetScheme(0);
        context.Update(0f);
        Assert.Equal(3, s_calls);
        Assert.Equal(gp, map.ActiveScheme);
    }

    /// <summary>
    /// An exception of the handler leaves <see cref="UiContext.Update(float)"/>, and the map keeps the values and the
    /// report of the frame, so the same change calls no second time.
    /// </summary>
    [Fact]
    public void AnExceptionOfTheHandlerLeavesUpdateAndKeepsTheReport()
    {
        s_calls = 0;
        UiContext context = CreateContext();
        UiActionMap map = CreateMap();
        map.AddAction(MoveAction, UiActionType.Vector2);
        map.BindGamepadStick(MoveAction, UiGamepadStick.Left);
        int kb = map.AddScheme(UiScheme.KeyboardMouse);
        int gp = map.AddScheme(UiScheme.Gamepad);
        map.OnSchemeChanged = s_throwHandler;
        map.Activate(context);

        SetPad(context, 0.5f, 0f, 0f);
        Assert.Throws<InvalidOperationException>(() => context.Update(0f));

        // The frame kept its values, because the finish ran before the handler.
        Assert.Equal(0.5f, map.ReadVector(MoveAction).X, 5);
        Assert.Equal(gp, map.ActiveScheme);

        // The frame kept its report, so a counting handler gets no call for the change the throw came from.
        map.OnSchemeChanged = s_countHandler;
        SetPad(context, 0.5f, 0f, 0f);
        context.Update(0f);
        Assert.Equal(0, s_calls);

        // A real change still calls: the report moves with the scheme, and not with the exception.
        map.SetScheme(kb);
        context.Update(0f);
        Assert.Equal(1, s_calls);
    }

    /// <summary>Writes slot 0 with the left stick and the left trigger, and no button down.</summary>
    /// <param name="context">The context.</param>
    /// <param name="stickX">The left stick X value.</param>
    /// <param name="stickY">The left stick Y value.</param>
    /// <param name="leftTrigger">The left trigger value.</param>
    private static void SetPad(UiContext context, float stickX, float stickY, float leftTrigger)
    {
        context.SetGamepad(0, new UiGamepadState(UiGamepadButtons.None, stackalloc float[] { stickX, stickY, 0f, 0f, leftTrigger, 0f }));
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

    /// <summary>Creates a map on a new registry with no command.</summary>
    /// <returns>The map with no action.</returns>
    private static UiActionMap CreateMap()
    {
        return new UiActionMap(new UiActionMap.CreateInfo { Commands = new CommandRegistry() });
    }

    /// <summary>Creates a key down event with no target.</summary>
    /// <param name="key">The GLFW key code.</param>
    /// <param name="modifiers">The modifier keys.</param>
    /// <returns>The event.</returns>
    private static KeyEvent Key(int key, UiModifiers modifiers = UiModifiers.None)
    {
        return new KeyEvent
        {
            Header = new UiEventHeader(UiEventType.KeyDown, 0d, null),
            Key = key,
            ScanCode = 0,
            Modifiers = modifiers,
            IsRepeat = false,
        };
    }
}
