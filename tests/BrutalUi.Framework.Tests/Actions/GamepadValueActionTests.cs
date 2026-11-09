using System;
using Brutal.Numerics;
using BrutalUi.Core;
using BrutalUi.Core.Tests;
using Xunit;

namespace BrutalUi.Framework.Tests;

/// <summary>
/// The tests of the gamepad values of <see cref="UiActionMap"/>: <see cref="UiActionMap.BindGamepadStick"/>,
/// <see cref="UiActionMap.BindGamepadAxis"/>, <see cref="UiActionMap.GamepadSlot"/>, <see cref="UiActionMap.ReadAxis"/>,
/// and the invoke pass of the frame read.
/// </summary>
/// <remarks>
/// <para>
/// The demo of the phase: a stick fills a Vector2, a trigger or a stick axis fills an Axis, and
/// <see cref="UiActionMap.GamepadSlot"/> selects the pad. An active map reads the pad in
/// <see cref="UiContext.Update(float)"/> and invokes the command of each new button press.
/// </para>
/// <para>
/// Each test builds a context with a <see cref="RecordingRenderer"/> measurer and a viewport of 400 by 300. A test
/// writes a pad with <see cref="UiContext.SetGamepad(int, in UiGamepadState)"/> before the update. The axis order of
/// the six-float span is <see cref="UiGamepadAxis.LeftStickX"/>, <see cref="UiGamepadAxis.LeftStickY"/>,
/// <see cref="UiGamepadAxis.RightStickX"/>, <see cref="UiGamepadAxis.RightStickY"/>,
/// <see cref="UiGamepadAxis.LeftTrigger"/>, and <see cref="UiGamepadAxis.RightTrigger"/>. Each float assert compares
/// one component with a precision of five decimals.
/// </para>
/// </remarks>
public sealed class GamepadValueActionTests
{
    /// <summary>The GLFW key code of the A key.</summary>
    private const int AKey = 65;

    /// <summary>The GLFW key code of the S key.</summary>
    private const int SKey = 83;

    /// <summary>The GLFW key code of the W key.</summary>
    private const int WKey = 87;

    /// <summary>The GLFW key code of the D key.</summary>
    private const int DKey = 68;

    /// <summary>The name of the Vector2 action that the tests move with.</summary>
    private static ReadOnlySpan<byte> MoveAction => "Move"u8;

    /// <summary>The name of a second Vector2 action.</summary>
    private static ReadOnlySpan<byte> LookAction => "Look"u8;

    /// <summary>The name of the Axis action that the tests zoom with.</summary>
    private static ReadOnlySpan<byte> ZoomAction => "Zoom"u8;

    /// <summary>The handler that adds one to the counter target.</summary>
    private static readonly CommandHandler s_count = static target => ((Counter)target!).Calls++;

    /// <summary>The handler that writes the identifier of its mark into the log of the mark.</summary>
    private static readonly CommandHandler s_mark = static target => ((Mark)target!).Write();

    /// <summary>The handler that counts, then deactivates the map of its target.</summary>
    private static readonly CommandHandler s_stop = static target =>
    {
        Stopper stopper = (Stopper)target!;
        stopper.Calls++;
        stopper.Map.Deactivate();
    };

    /// <summary>The handler that throws a test exception.</summary>
    private static readonly CommandHandler s_throw = static _ => throw new TestException();

    /// <summary>The left stick fills a Vector2 with up positive. A right stick bind reads axes 2 and 3 the same way. A disconnected slot gives (0, 0).</summary>
    [Fact]
    public void TheLeftStickFillsAVector2WithUpPositive()
    {
        UiContext context = CreateContext();
        UiActionMap map = CreateMap();
        map.AddAction(MoveAction, UiActionType.Vector2);
        map.AddAction(LookAction, UiActionType.Vector2);
        map.BindGamepadStick(MoveAction, UiGamepadStick.Left);
        map.BindGamepadStick(LookAction, UiGamepadStick.Right);
        map.Activate(context);

        context.Update(0f);
        AssertVector(map.ReadVector(MoveAction), 0f, 0f);

        SetPad(context, 0, UiGamepadButtons.None, stackalloc float[] { 0.5f, -0.25f });
        context.Update(0f);
        AssertVector(map.ReadVector(MoveAction), 0.5f, 0.25f);

        SetPad(context, 0, UiGamepadButtons.None, stackalloc float[] { 0f, 0f, 0.5f, -0.25f });
        context.Update(0f);
        AssertVector(map.ReadVector(MoveAction), 0f, 0f);
        AssertVector(map.ReadVector(LookAction), 0.5f, 0.25f);
    }

    /// <summary>WASD and the left stick in one action sum, then clamp the length to 1.</summary>
    [Fact]
    public void AStickAndAKeyCompositeSumAndClamp()
    {
        UiContext context = CreateContext();
        UiActionMap map = CreateMap();
        map.AddAction(MoveAction, UiActionType.Vector2);
        map.BindKeyVector(MoveAction, WKey, AKey, SKey, DKey);
        map.BindGamepadStick(MoveAction, UiGamepadStick.Left);
        map.Activate(context);

        context.PushEvent(Key(WKey));
        SetPad(context, 0, UiGamepadButtons.None, stackalloc float[] { 1f, 0f });
        context.Update(0f);
        AssertVector(map.ReadVector(MoveAction), 0.707107f, 0.707107f);

        SetPad(context, 0, UiGamepadButtons.None, stackalloc float[] { 0f, -1f });
        context.Update(0f);
        AssertVector(map.ReadVector(MoveAction), 0f, 1f);

        SetPad(context, 0, UiGamepadButtons.None, stackalloc float[] { 0f, 1f });
        context.Update(0f);
        AssertVector(map.ReadVector(MoveAction), 0f, 0f);
    }

    /// <summary>A trigger fills an Axis. A scale multiplies, a negative scale turns one trigger negative, and the sum clamps to -1 to 1.</summary>
    [Fact]
    public void ATriggerFillsAnAxis()
    {
        UiContext context = CreateContext();
        UiActionMap map = CreateMap();
        map.AddAction(ZoomAction, UiActionType.Axis);
        map.BindGamepadAxis(ZoomAction, UiGamepadAxis.RightTrigger);
        map.BindGamepadAxis(ZoomAction, UiGamepadAxis.LeftTrigger, -1f);
        map.Activate(context);
        UiContext scaledContext = CreateContext();
        UiActionMap scaledMap = CreateMap();
        scaledMap.AddAction(ZoomAction, UiActionType.Axis);
        scaledMap.BindGamepadAxis(ZoomAction, UiGamepadAxis.RightTrigger, 2f);
        scaledMap.Activate(scaledContext);

        SetPad(context, 0, UiGamepadButtons.None, stackalloc float[] { 0f, 0f, 0f, 0f, 0f, 0.5f });
        context.Update(0f);
        Assert.Equal(0.5f, map.ReadAxis(ZoomAction), 5);

        SetPad(context, 0, UiGamepadButtons.None, stackalloc float[] { 0f, 0f, 0f, 0f, 0.75f, 0f });
        context.Update(0f);
        Assert.Equal(-0.75f, map.ReadAxis(ZoomAction), 5);

        SetPad(context, 0, UiGamepadButtons.None, stackalloc float[] { 0f, 0f, 0f, 0f, 0.25f, 1f });
        context.Update(0f);
        Assert.Equal(0.75f, map.ReadAxis(ZoomAction), 5);

        SetPad(scaledContext, 0, UiGamepadButtons.None, stackalloc float[] { 0f, 0f, 0f, 0f, 0f, 0.75f });
        scaledContext.Update(0f);
        Assert.Equal(1f, scaledMap.ReadAxis(ZoomAction), 5);
    }

    /// <summary>A stick axis fills an Axis, and a Y axis flips so up is positive.</summary>
    [Fact]
    public void AStickAxisFillsAnAxisWithUpPositive()
    {
        UiContext context = CreateContext();
        UiActionMap map = CreateMap();
        map.AddAction(MoveAction, UiActionType.Axis);
        map.AddAction(ZoomAction, UiActionType.Axis);
        map.BindGamepadAxis(MoveAction, UiGamepadAxis.LeftStickX);
        map.BindGamepadAxis(ZoomAction, UiGamepadAxis.LeftStickY);
        map.Activate(context);

        SetPad(context, 0, UiGamepadButtons.None, stackalloc float[] { 0.5f, -0.5f });
        context.Update(0f);
        Assert.Equal(0.5f, map.ReadAxis(MoveAction), 5);
        Assert.Equal(0.5f, map.ReadAxis(ZoomAction), 5);
    }

    /// <summary>The default slot is 0. The slot picks the pad for the values and the invoke pass, and a set outside 0 to 15 keeps the old value.</summary>
    [Fact]
    public void TheGamepadSlotSelectsThePad()
    {
        Counter counter = new();
        UiContext context = CreateContext();
        UiActionMap map = CreateMap();
        map.AddAction(MoveAction, UiActionType.Vector2);
        map.AddAction(UiActions.Cancel, "cancel"u8);
        map.Commands.Register("cancel"u8, s_count, counter);
        map.BindGamepadStick(MoveAction, UiGamepadStick.Left);
        map.BindGamepadButton(UiActions.Cancel, UiGamepadButtons.B);
        map.Activate(context);

        Assert.Equal(0, map.GamepadSlot);
        map.GamepadSlot = 2;

        SetPad(context, 0, UiGamepadButtons.None, stackalloc float[] { 0.5f, -0.25f });
        context.Update(0f);
        AssertVector(map.ReadVector(MoveAction), 0f, 0f);

        SetPad(context, 2, UiGamepadButtons.None, stackalloc float[] { 0.5f, -0.25f });
        context.Update(0f);
        AssertVector(map.ReadVector(MoveAction), 0.5f, 0.25f);

        SetPad(context, 0, UiGamepadButtons.B, []);
        context.Update(0f);
        Assert.Equal(0, counter.Calls);

        SetPad(context, 2, UiGamepadButtons.B, []);
        context.Update(0f);
        Assert.Equal(1, counter.Calls);

        SetPad(context, 0, UiGamepadButtons.None, []);
        SetPad(context, 2, UiGamepadButtons.None, []);
        context.Update(0f);

        SetPad(context, 0, UiGamepadButtons.B, []);
        context.Update(0f);
        Assert.Equal(1, counter.Calls);

        ArgumentOutOfRangeException lowError = Assert.Throws<ArgumentOutOfRangeException>(() => map.GamepadSlot = -1);
        Assert.Equal("value", lowError.ParamName);

        ArgumentOutOfRangeException highError = Assert.Throws<ArgumentOutOfRangeException>(() => map.GamepadSlot = 16);
        Assert.Equal("value", highError.ParamName);

        Assert.Equal(2, map.GamepadSlot);
    }

    /// <summary>An active map invokes the command of each new gamepad press from the frame read, in the order of the bind. A state press and a push of one button in one frame run the command once per path (Decision 9).</summary>
    [Fact]
    public void AGamepadPressInvokesTheCommandFromTheFrameRead()
    {
        Counter counter = new();
        CommandRegistry registry = new();
        registry.Register("cancel"u8, s_count, counter);
        UiActionMap map = new(new UiActionMap.CreateInfo { Commands = registry });
        map.AddAction(UiActions.Cancel, "cancel"u8);
        map.BindGamepadButton(UiActions.Cancel, UiGamepadButtons.B);
        UiContext context = CreateContext();
        map.Activate(context);

        SetPad(context, 0, UiGamepadButtons.B, []);
        context.Update(0f);
        Assert.Equal(1, counter.Calls);

        SetPad(context, 0, UiGamepadButtons.B, []);
        context.Update(0f);
        Assert.Equal(1, counter.Calls);

        SetPad(context, 0, UiGamepadButtons.None, []);
        context.Update(0f);
        SetPad(context, 0, UiGamepadButtons.B, []);
        context.Update(0f);
        Assert.Equal(2, counter.Calls);

        SetPad(context, 0, UiGamepadButtons.None, []);
        context.Update(0f);

        // Decision 9: a game that pushes a pad that the frame read also sees runs the command once per path.
        Assert.Equal(1, map.PushGamepad(UiGamepadButtons.B));
        SetPad(context, 0, UiGamepadButtons.B, []);
        context.Update(0f);
        Assert.Equal(4, counter.Calls);
        Assert.True(map.WasPressed(UiActions.Cancel));

        Counter inactiveCounter = new();
        CommandRegistry otherRegistry = new();
        otherRegistry.Register("cancel"u8, s_count, inactiveCounter);
        UiActionMap inactive = new(new UiActionMap.CreateInfo { Commands = otherRegistry });
        inactive.AddAction(UiActions.Cancel, "cancel"u8);
        inactive.BindGamepadButton(UiActions.Cancel, UiGamepadButtons.B);
        UiContext otherContext = CreateContext();

        SetPad(otherContext, 0, UiGamepadButtons.B, []);
        otherContext.Update(0f);
        Assert.Equal(0, inactiveCounter.Calls);

        Log log = new();
        Mark markA = new(log, 1);
        Mark markB = new(log, 2);
        CommandRegistry orderRegistry = new();
        orderRegistry.Register("mark-b"u8, s_mark, markB);
        orderRegistry.Register("mark-a"u8, s_mark, markA);
        UiActionMap orderMap = new(new UiActionMap.CreateInfo { Commands = orderRegistry });
        orderMap.AddAction(UiActions.Cancel, "mark-b"u8);
        orderMap.AddAction(UiActions.Confirm, "mark-a"u8);
        orderMap.BindGamepadButton(UiActions.Cancel, UiGamepadButtons.B);
        orderMap.BindGamepadButton(UiActions.Confirm, UiGamepadButtons.A);
        UiContext orderContext = CreateContext();
        orderMap.Activate(orderContext);

        SetPad(orderContext, 0, UiGamepadButtons.A | UiGamepadButtons.B, []);
        orderContext.Update(0f);
        Assert.Equal(2, log.Count);
        Assert.Equal(2, log.Entries[0]);
        Assert.Equal(1, log.Entries[1]);
    }

    /// <summary>A command that deactivates the map stops the pass, and a command that throws leaves the values of the frame.</summary>
    [Fact]
    public void TheInvokePassStopsAndKeepsTheValues()
    {
        CommandRegistry registry = new();
        Counter afterCounter = new();
        registry.Register("after"u8, s_count, afterCounter);
        UiActionMap map = new(new UiActionMap.CreateInfo { Commands = registry });
        map.AddAction(UiActions.Cancel, "stop"u8);
        map.AddAction(UiActions.Confirm, "after"u8);
        map.BindGamepadButton(UiActions.Cancel, UiGamepadButtons.B);
        map.BindGamepadButton(UiActions.Confirm, UiGamepadButtons.A);
        Stopper stopper = new() { Map = map };
        registry.Register("stop"u8, s_stop, stopper);
        UiContext context = CreateContext();
        map.Activate(context);

        SetPad(context, 0, UiGamepadButtons.A | UiGamepadButtons.B, []);
        context.Update(0f);
        Assert.Equal(1, stopper.Calls);
        Assert.Equal(0, afterCounter.Calls);

        CommandRegistry throwRegistry = new();
        throwRegistry.Register("boom"u8, s_throw, null);
        UiActionMap throwMap = new(new UiActionMap.CreateInfo { Commands = throwRegistry });
        throwMap.AddAction(UiActions.Cancel, "boom"u8);
        throwMap.AddAction(ZoomAction, UiActionType.Axis);
        throwMap.BindGamepadButton(UiActions.Cancel, UiGamepadButtons.B);
        throwMap.BindGamepadAxis(ZoomAction, UiGamepadAxis.RightTrigger);
        UiContext throwContext = CreateContext();
        throwMap.Activate(throwContext);

        SetPad(throwContext, 0, UiGamepadButtons.B, stackalloc float[] { 0f, 0f, 0f, 0f, 0f, 0.75f });
        Assert.Throws<TestException>(() => throwContext.Update(0f));
        Assert.Equal(0.75f, throwMap.ReadAxis(ZoomAction), 5);
    }

    /// <summary>Each gamepad value bind checks its arguments, the action name, the type, and a duplicate.</summary>
    [Fact]
    public void TheBuildChecksTheGamepadValueBinds()
    {
        UiActionMap map = CreateMap();
        map.AddAction(MoveAction, UiActionType.Vector2);
        map.AddAction(ZoomAction, UiActionType.Axis);
        map.AddAction(UiActions.Cancel, "cancel"u8);
        int actionCount = map.ActionCount;
        int bindingCount = map.BindingCount;

        ArgumentException stickOnAxis = Assert.Throws<ArgumentException>(
            () => map.BindGamepadStick(ZoomAction, UiGamepadStick.Left));
        Assert.Equal("action", stickOnAxis.ParamName);

        ArgumentException stickOnButton = Assert.Throws<ArgumentException>(
            () => map.BindGamepadStick(UiActions.Cancel, UiGamepadStick.Left));
        Assert.Equal("action", stickOnButton.ParamName);

        ArgumentException axisOnVector = Assert.Throws<ArgumentException>(
            () => map.BindGamepadAxis(MoveAction, UiGamepadAxis.RightTrigger));
        Assert.Equal("action", axisOnVector.ParamName);

        ArgumentOutOfRangeException badStick = Assert.Throws<ArgumentOutOfRangeException>(
            () => map.BindGamepadStick(MoveAction, (UiGamepadStick)2));
        Assert.Equal("stick", badStick.ParamName);

        ArgumentOutOfRangeException badAxis = Assert.Throws<ArgumentOutOfRangeException>(
            () => map.BindGamepadAxis(ZoomAction, (UiGamepadAxis)6));
        Assert.Equal("axis", badAxis.ParamName);

        ArgumentOutOfRangeException nanScale = Assert.Throws<ArgumentOutOfRangeException>(
            () => map.BindGamepadAxis(ZoomAction, UiGamepadAxis.RightTrigger, float.NaN));
        Assert.Equal("scale", nanScale.ParamName);

        ArgumentOutOfRangeException infiniteScale = Assert.Throws<ArgumentOutOfRangeException>(
            () => map.BindGamepadAxis(ZoomAction, UiGamepadAxis.RightTrigger, float.PositiveInfinity));
        Assert.Equal("scale", infiniteScale.ParamName);

        map.BindGamepadStick(MoveAction, UiGamepadStick.Left);
        Assert.Throws<ArgumentException>(() => map.BindGamepadStick(MoveAction, UiGamepadStick.Left));

        map.BindGamepadAxis(ZoomAction, UiGamepadAxis.RightTrigger);
        Assert.Throws<ArgumentException>(() => map.BindGamepadAxis(ZoomAction, UiGamepadAxis.RightTrigger));

        ArgumentException readError = Assert.Throws<ArgumentException>(() => map.ReadAxis(MoveAction));
        Assert.Equal("action", readError.ParamName);

        Assert.Equal(0f, map.ReadAxis("Nope"u8), 5);

        Assert.Equal(actionCount, map.ActionCount);
        Assert.Equal(bindingCount + 2, map.BindingCount);
    }

    /// <summary>Writes one gamepad slot of a context.</summary>
    /// <param name="context">The context.</param>
    /// <param name="slot">The slot index.</param>
    /// <param name="buttons">The buttons that are down.</param>
    /// <param name="axes">The axis values in the GLFW order. The state fills the rest with 0.</param>
    private static void SetPad(UiContext context, int slot, UiGamepadButtons buttons, ReadOnlySpan<float> axes)
    {
        context.SetGamepad(slot, new UiGamepadState(buttons, axes));
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

    /// <summary>Creates a map on a new empty registry.</summary>
    /// <returns>The map with no action.</returns>
    private static UiActionMap CreateMap()
    {
        return new UiActionMap(new UiActionMap.CreateInfo { Commands = new CommandRegistry() });
    }

    /// <summary>Creates a key down event with no target.</summary>
    /// <param name="key">The GLFW key code.</param>
    /// <returns>The event.</returns>
    private static KeyEvent Key(int key)
    {
        return new KeyEvent
        {
            Header = new UiEventHeader(UiEventType.KeyDown, 0d, null),
            Key = key,
            ScanCode = 0,
            Modifiers = UiModifiers.None,
            IsRepeat = false,
        };
    }

    /// <summary>A target that counts the calls of its command.</summary>
    private sealed class Counter
    {
        /// <summary>The count of calls.</summary>
        public int Calls;
    }

    /// <summary>A log of the identifiers that the marks wrote, in order.</summary>
    private sealed class Log
    {
        /// <summary>The identifiers in the order of the writes.</summary>
        public readonly int[] Entries = new int[8];

        /// <summary>The count of entries.</summary>
        public int Count;
    }

    /// <summary>A target that writes its identifier into a log.</summary>
    private sealed class Mark
    {
        /// <summary>The log.</summary>
        private readonly Log _log;

        /// <summary>The identifier.</summary>
        private readonly int _id;

        /// <summary>Creates a mark.</summary>
        /// <param name="log">The log.</param>
        /// <param name="id">The identifier.</param>
        public Mark(Log log, int id)
        {
            _log = log;
            _id = id;
        }

        /// <summary>Writes the identifier into the log.</summary>
        public void Write()
        {
            _log.Entries[_log.Count] = _id;
            _log.Count++;
        }
    }

    /// <summary>A target that counts the calls of its command, then deactivates a map.</summary>
    private sealed class Stopper
    {
        /// <summary>The count of calls.</summary>
        public int Calls;

        /// <summary>The map that the command deactivates.</summary>
        public required UiActionMap Map;
    }

    /// <summary>The exception of a failing test command.</summary>
    private sealed class TestException : Exception
    {
    }
}
