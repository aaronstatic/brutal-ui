using System;
using Brutal.Numerics;
using BrutalUi.Core;
using BrutalUi.Core.Tests;
using Xunit;

namespace BrutalUi.Framework.Tests;

/// <summary>
/// The tests of the rebind listen of <see cref="UiActionMap"/>: <see cref="UiActionMap.BeginRebind"/>,
/// <see cref="UiActionMap.CancelRebind"/>, <see cref="UiActionMap.IsRebinding"/>,
/// <see cref="UiActionMap.RebindCancelKey"/>, the result slot, and the silence of the five paths.
/// </summary>
/// <remarks>
/// <para>
/// Each test here rebinds a key: the subject of this class is the listen, and
/// <see cref="RebindCaptureTests"/> tests the capture of the seven input shapes.
/// </para>
/// <para>
/// Each test builds a context with a <see cref="RecordingRenderer"/> measurer and a viewport of 400 by 300. A test
/// writes a pad with <see cref="UiContext.SetGamepad(int, in UiGamepadState)"/> before the update, and builds its
/// events by hand. A slot test uses a <see langword="static readonly"/> handler over a <see langword="static"/>
/// counter, and resets the counter at its start, because xUnit runs classes in parallel. GLFW codes: Space 32, W 87,
/// Escape 256, F1 290. Each float assert compares one component with a precision of five decimals.
/// </para>
/// </remarks>
public sealed class RebindListenTests
{
    /// <summary>The GLFW key code of the Space key.</summary>
    private const int SpaceKey = 32;

    /// <summary>The GLFW key code of the W key.</summary>
    private const int WKey = 87;

    /// <summary>The GLFW key code of Escape: the default cancel key.</summary>
    private const int EscapeKey = 256;

    /// <summary>The GLFW key code of F1.</summary>
    private const int F1Key = 290;

    /// <summary>The name of the Button action that the tests rebind.</summary>
    private static ReadOnlySpan<byte> FireAction => "Fire"u8;

    /// <summary>The name of the Button action that the tests move a listen to.</summary>
    private static ReadOnlySpan<byte> JumpAction => "Jump"u8;

    /// <summary>The name of the Vector2 action that the tests hold with a stick.</summary>
    private static ReadOnlySpan<byte> MoveAction => "Move"u8;

    /// <summary>The name of the Axis action that the tests hold with a trigger.</summary>
    private static ReadOnlySpan<byte> ZoomAction => "Zoom"u8;

    /// <summary>The name of the command that counts its calls.</summary>
    private static ReadOnlySpan<byte> CountCommand => "count"u8;

    /// <summary>The name that no map holds.</summary>
    private static ReadOnlySpan<byte> MissingAction => "Nope"u8;

    /// <summary>The handler that adds one to the counter target.</summary>
    private static readonly CommandHandler s_count = static target => ((Counter)target!).Calls++;

    /// <summary>The result handler that counts its calls and keeps the last status and code.</summary>
    private static readonly UiActionMap.RebindResultHandler s_result = static (map, action, status, code) =>
    {
        s_resultCalls++;
        s_resultStatus = status;
        s_resultCode = code;
        s_resultNameLength = action.Length;
    };

    /// <summary>The scheme handler that counts its calls and keeps the last id.</summary>
    private static readonly UiSchemeChangedHandler s_scheme = static (map, scheme) =>
    {
        s_schemeCalls++;
        s_schemeId = scheme;
    };

    private static int s_resultCalls;
    private static int s_resultStatus;
    private static int s_resultCode;
    private static int s_resultNameLength;
    private static int s_schemeCalls;
    private static int s_schemeId;

    /// <summary>
    /// <see cref="UiActionMap.BeginRebind"/> checks the scheme id, gives <see langword="false"/> for an unknown action,
    /// and opens on a valid pair.
    /// </summary>
    [Fact]
    public void BeginRebindChecksTheSchemeAndGivesFalseForAnUnknownAction()
    {
        UiActionMap map = CreateMap();
        int kb = map.AddScheme(UiScheme.KeyboardMouse);
        map.AddAction(FireAction, UiActionType.Button);
        map.AddAction(MoveAction, UiActionType.Vector2);
        int tooBig = map.AddScheme(UiScheme.Gamepad) + 1;
        int actionCount = map.ActionCount;
        int bindingCount = map.BindingCount;

        ArgumentOutOfRangeException low = Assert.Throws<ArgumentOutOfRangeException>(
            () => map.BeginRebind(FireAction, -1));
        Assert.Equal("scheme", low.ParamName);
        Assert.False(map.IsRebinding);

        ArgumentOutOfRangeException high = Assert.Throws<ArgumentOutOfRangeException>(
            () => map.BeginRebind(FireAction, tooBig));
        Assert.Equal("scheme", high.ParamName);
        Assert.False(map.IsRebinding);

        Assert.False(map.BeginRebind(MissingAction, kb));
        Assert.False(map.IsRebinding);
        Assert.Equal(actionCount, map.ActionCount);
        Assert.Equal(bindingCount, map.BindingCount);

        Assert.True(map.BeginRebind(FireAction, UiScheme.Default));
        Assert.True(map.IsRebinding);
        Assert.Equal(actionCount, map.ActionCount);
        Assert.Equal(bindingCount, map.BindingCount);

        Assert.True(map.CancelRebind());
        Assert.False(map.IsRebinding);
    }

    /// <summary>
    /// A fresh key press after the settle frame commits the new binding, and the press that landed it drives nothing.
    /// </summary>
    [Fact]
    public void AFreshKeyPressAfterTheListenCommitsTheBinding()
    {
        s_resultCalls = 0;
        Counter counter = new();
        UiContext context = CreateContext();
        UiActionMap map = CreateMap(counter);
        int kb = map.AddScheme(UiScheme.KeyboardMouse);
        map.AddAction(FireAction, CountCommand);
        map.BindKey(FireAction, SpaceKey, UiModifiers.None, kb);
        map.OnRebindResult = s_result;
        map.Activate(context);
        context.Update(0f);

        Assert.True(map.BeginRebind(FireAction, kb));
        context.Update(0f);
        Assert.True(map.IsRebinding);
        Assert.Equal(1, map.BindingCount);
        Assert.Equal(0, s_resultCalls);

        context.PushEvent(Key(WKey));
        context.Update(0f);
        Assert.False(map.IsRebinding);
        Assert.Equal(1, map.BindingCount);
        Assert.Equal(1, s_resultCalls);
        Assert.Equal(UiActionMap.RebindBound, s_resultStatus);
        Assert.Equal(WKey, s_resultCode);
        Assert.Equal(FireAction.Length, s_resultNameLength);
        Assert.Equal(0, counter.Calls);
        Assert.False(map.WasPressed(FireAction));

        context.PushEvent(KeyUp(WKey));
        context.PushEvent(Key(SpaceKey));
        context.Update(0f);
        Assert.Equal(0, counter.Calls);
        Assert.False(map.WasPressed(FireAction));

        context.PushEvent(KeyUp(SpaceKey));
        context.PushEvent(Key(WKey));
        context.Update(0f);
        Assert.True(map.WasPressed(FireAction));
        Assert.True(map.IsHeld(FireAction));
        Assert.Equal(1, counter.Calls);
    }

    /// <summary>
    /// A listening frame writes no value, runs no command on all five paths, emits no release edge, and queues nothing.
    /// </summary>
    [Fact]
    public void TheListenGivesNoValueAndRunsNoCommandOnEveryPath()
    {
        Counter counter = new();
        UiContext context = CreateContext();
        UiActionMap map = CreateMap(counter);
        int kb = map.AddScheme(UiScheme.KeyboardMouse);
        map.AddAction(MoveAction, UiActionType.Vector2);
        map.AddAction(ZoomAction, UiActionType.Axis);
        map.AddAction(FireAction, CountCommand);
        map.BindGamepadStick(MoveAction, UiGamepadStick.Left);
        map.BindGamepadAxis(ZoomAction, UiGamepadAxis.LeftTrigger);
        map.BindKey(FireAction, SpaceKey, UiModifiers.None, kb);
        map.BindMouseButton(FireAction, PointerButton.Right, UiModifiers.None, kb);
        map.BindGamepadButton(FireAction, UiGamepadButtons.A);
        map.Activate(context);

        float[] held = [1f, 0f, 0f, 0f, 0.5f, 0f];
        SetPad(context, UiGamepadButtons.None, in held);
        context.PushEvent(Key(WKey));
        context.PushEvent(Key(SpaceKey));
        context.Update(0f);
        AssertVector(map.ReadVector(MoveAction), 1f, 0f);
        Assert.Equal(0.5f, map.ReadAxis(ZoomAction), 5);
        Assert.True(map.WasPressed(FireAction));
        Assert.True(map.IsHeld(FireAction));
        Assert.Equal(1, counter.Calls);
        int bindingCount = map.BindingCount;

        Assert.True(map.BeginRebind(FireAction, kb));
        for (int frame = 0; frame < 3; frame++)
        {
            SetPad(context, UiGamepadButtons.None, in held);
            context.PushEvent(Key(WKey, isRepeat: true));
            context.PushEvent(Key(SpaceKey, isRepeat: true));
            context.Update(0f);
        }

        Assert.True(map.IsRebinding);
        Assert.Equal(bindingCount, map.BindingCount);
        AssertVector(map.ReadVector(MoveAction), 1f, 0f);
        Assert.Equal(0.5f, map.ReadAxis(ZoomAction), 5);
        Assert.True(map.WasPressed(FireAction));
        Assert.True(map.IsHeld(FireAction));
        Assert.False(map.WasReleased(FireAction));
        Assert.Equal(1, counter.Calls);

        Assert.False(map.PressKey(SpaceKey, UiModifiers.None));
        Assert.False(map.PressMouseButton(PointerButton.Right, UiModifiers.None));
        Assert.Equal(0, map.PushGamepad(UiGamepadButtons.A));
        Assert.False(map.InvokeAction(FireAction));
        Assert.Equal(1, counter.Calls);

        Assert.True(map.CancelRebind());
        Assert.False(map.IsRebinding);
        Assert.True(map.PressKey(SpaceKey, UiModifiers.None));
        Assert.True(map.PressMouseButton(PointerButton.Right, UiModifiers.None));
        Assert.Equal(0, map.PushGamepad(UiGamepadButtons.A));
        Assert.Equal(0, map.PushGamepad(UiGamepadButtons.None));
        Assert.Equal(1, map.PushGamepad(UiGamepadButtons.A));
        Assert.True(map.InvokeAction(FireAction));

        SetPad(context, UiGamepadButtons.None, in held);
        context.Update(0f);
        AssertVector(map.ReadVector(MoveAction), 1f, 0f);
        Assert.Equal(0.5f, map.ReadAxis(ZoomAction), 5);
        Assert.True(map.IsHeld(FireAction));
    }

    /// <summary>
    /// Escape closes the listen before the map's own Cancel bind can fire, and the listen consumes the edge.
    /// </summary>
    [Fact]
    public void EscapeCancelsTheListenBeforeTheMapsOwnCancelBindCanFire()
    {
        s_resultCalls = 0;
        CommandRegistry registry = new();
        Counter cancelCounter = new();
        Counter jumpCounter = new();
        registry.Register("menu.cancel"u8, s_count, cancelCounter);
        registry.Register("menu.jump"u8, s_count, jumpCounter);
        UiActionMap map = new(new UiActionMap.CreateInfo { Commands = registry });
        map.AddAction(UiActions.Cancel, "menu.cancel"u8);
        map.AddAction(JumpAction, "menu.jump"u8);
        map.BindKey(UiActions.Cancel, EscapeKey);
        map.BindKey(JumpAction, SpaceKey);
        map.OnRebindResult = s_result;
        UiContext context = CreateContext();
        map.Activate(context);
        context.Update(0f);

        Assert.True(map.BeginRebind(JumpAction, UiScheme.Default));
        context.Update(0f);
        context.PushEvent(Key(EscapeKey));
        context.Update(0f);
        Assert.False(map.IsRebinding);
        Assert.Equal(0, cancelCounter.Calls);
        Assert.Equal(0, jumpCounter.Calls);
        Assert.Equal(2, map.BindingCount);
        Assert.Equal(1, s_resultCalls);
        Assert.Equal(UiActionMap.RebindCancelled, s_resultStatus);
        Assert.Equal(EscapeKey, s_resultCode);

        context.PushEvent(KeyUp(EscapeKey));
        context.PushEvent(Key(EscapeKey));
        context.Update(0f);
        Assert.Equal(1, cancelCounter.Calls);
    }

    /// <summary>
    /// <see cref="UiActionMap.RebindCancelKey"/> moves the cancel key, -1 turns it off, and a bad set keeps the old key.
    /// </summary>
    [Fact]
    public void TheCancelKeyIsOneSettablePropertyWithARangeCheck()
    {
        s_resultCalls = 0;
        UiContext context = CreateContext();
        UiActionMap map = CreateMap();
        int kb = map.AddScheme(UiScheme.KeyboardMouse);
        map.AddAction(FireAction, UiActionType.Button);
        map.BindKey(FireAction, SpaceKey, UiModifiers.None, kb);
        map.OnRebindResult = s_result;
        map.Activate(context);
        context.Update(0f);

        Assert.Equal(EscapeKey, map.RebindCancelKey);

        map.RebindCancelKey = F1Key;
        Assert.Equal(F1Key, map.RebindCancelKey);
        Assert.True(map.BeginRebind(FireAction, kb));
        context.Update(0f);
        context.PushEvent(Key(EscapeKey));
        context.Update(0f);
        Assert.False(map.IsRebinding);
        Assert.Equal(1, s_resultCalls);
        Assert.Equal(UiActionMap.RebindBound, s_resultStatus);
        Assert.Equal(EscapeKey, s_resultCode);
        Assert.Equal(1, map.BindingCount);

        context.PushEvent(KeyUp(EscapeKey));
        s_resultCalls = 0;
        Assert.True(map.BeginRebind(FireAction, kb));
        context.Update(0f);
        context.PushEvent(Key(F1Key));
        context.Update(0f);
        Assert.False(map.IsRebinding);
        Assert.Equal(UiActionMap.RebindCancelled, s_resultStatus);
        Assert.Equal(F1Key, s_resultCode);
        Assert.Equal(1, map.BindingCount);

        context.PushEvent(KeyUp(F1Key));
        s_resultCalls = 0;
        map.RebindCancelKey = -1;
        Assert.Equal(-1, map.RebindCancelKey);
        Assert.True(map.BeginRebind(FireAction, kb));
        context.Update(0f);
        context.PushEvent(Key(EscapeKey));
        context.Update(0f);
        Assert.False(map.IsRebinding);
        Assert.Equal(UiActionMap.RebindBound, s_resultStatus);
        Assert.Equal(EscapeKey, s_resultCode);

        ArgumentOutOfRangeException high = Assert.Throws<ArgumentOutOfRangeException>(
            () => map.RebindCancelKey = UiKeyState.KeyCount);
        Assert.Equal("value", high.ParamName);
        ArgumentOutOfRangeException low = Assert.Throws<ArgumentOutOfRangeException>(
            () => map.RebindCancelKey = -2);
        Assert.Equal("value", low.ParamName);
        Assert.Equal(-1, map.RebindCancelKey);
    }

    /// <summary>
    /// The settle frame ignores the input that reached the listen in the frame that opened it.
    /// </summary>
    [Fact]
    public void TheListenIgnoresTheInputThatReachedItInTheOpenFrame()
    {
        s_resultCalls = 0;
        Counter counter = new();
        UiContext context = CreateContext();
        UiActionMap map = CreateMap(counter);
        int kb = map.AddScheme(UiScheme.KeyboardMouse);
        map.AddAction(FireAction, CountCommand);
        map.BindKey(FireAction, SpaceKey, UiModifiers.None, kb);
        map.OnRebindResult = s_result;
        map.Activate(context);
        context.Update(0f);

        context.PushEvent(Key(SpaceKey));
        Assert.True(map.BeginRebind(FireAction, kb));
        context.Update(0f);
        Assert.True(map.IsRebinding);
        Assert.Equal(0, s_resultCalls);
        Assert.Equal(1, map.BindingCount);
        Assert.Equal(0, counter.Calls);

        context.Update(0f);
        Assert.True(map.IsRebinding);
        Assert.Equal(0, s_resultCalls);
        Assert.Equal(1, map.BindingCount);

        Assert.True(map.CancelRebind());
        context.PushEvent(KeyUp(SpaceKey));
        context.PushEvent(Key(SpaceKey));
        context.Update(0f);
        Assert.Equal(1, counter.Calls);
        Assert.Equal(UiActionMap.RebindCancelled, s_resultStatus);
    }

    /// <summary>
    /// The scan reads the raw view, so a rebind captures a key that the routing gate empties from the game view.
    /// </summary>
    [Fact]
    public void ARebindCapturesAKeyThatTheRoutingGateEmpties()
    {
        s_resultCalls = 0;
        UiContext context = CreateContext();
        Element root = AddRoot(context);
        Button button = CreateButton();
        root.Children.Add(button);
        UiActionMap map = CreateMap();
        map.AddAction(FireAction, UiActionType.Button);
        map.BindKey(FireAction, SpaceKey);
        map.OnRebindResult = s_result;
        map.Activate(context);
        context.Update(0f);

        Assert.True(context.SetFocus(button));
        context.PushEvent(Key(WKey));
        context.Update(0f);
        Assert.True(context.RoutingGate.KeyboardFocusInUi);
        Assert.False(map.WasPressed(FireAction));

        Assert.True(map.BeginRebind(FireAction, UiScheme.Default));
        context.Update(0f);
        Assert.True(map.IsRebinding);
        context.PushEvent(KeyUp(WKey));
        context.PushEvent(Key(WKey));
        context.Update(0f);
        Assert.False(map.IsRebinding);
        Assert.Equal(1, s_resultCalls);
        Assert.Equal(UiActionMap.RebindBound, s_resultStatus);
        Assert.Equal(WKey, s_resultCode);
        Assert.Equal(1, map.BindingCount);
    }

    /// <summary>
    /// A second <see cref="UiActionMap.BeginRebind"/> moves the listen to the new target and leaves the first alone.
    /// </summary>
    [Fact]
    public void ASecondBeginRebindMovesTheListenToTheNewTarget()
    {
        s_resultCalls = 0;
        Counter counter = new();
        UiContext context = CreateContext();
        UiActionMap map = CreateMap(counter);
        int kb = map.AddScheme(UiScheme.KeyboardMouse);
        int gp = map.AddScheme(UiScheme.Gamepad);
        map.AddAction(FireAction, CountCommand);
        map.AddAction(JumpAction, UiActionType.Button);
        map.BindKey(FireAction, SpaceKey, UiModifiers.None, kb);
        map.BindKey(JumpAction, SpaceKey, UiModifiers.None, gp);
        map.OnRebindResult = s_result;
        map.Activate(context);
        context.Update(0f);

        Assert.True(map.BeginRebind(FireAction, kb));
        Assert.True(map.BeginRebind(JumpAction, gp));
        context.Update(0f);
        Assert.True(map.IsRebinding);
        context.PushEvent(Key(WKey));
        context.Update(0f);
        Assert.False(map.IsRebinding);
        Assert.Equal(1, s_resultCalls);
        Assert.Equal(JumpAction.Length, s_resultNameLength);
        Assert.Equal(2, map.BindingCount);

        context.PushEvent(KeyUp(WKey));
        context.PushEvent(Key(WKey));
        context.Update(0f);
        Assert.True(map.WasPressed(JumpAction));
        Assert.False(map.WasPressed(FireAction));
        Assert.True(map.PressKey(SpaceKey, UiModifiers.None));
        Assert.Equal(1, counter.Calls);
    }

    /// <summary>
    /// <see cref="UiActionMap.Deactivate"/> and <see cref="UiActionMap.Activate"/> close a listen with no commit and no
    /// slot call.
    /// </summary>
    [Fact]
    public void ActivateAndDeactivateCloseTheListenWithNoCommitAndNoSlotCall()
    {
        s_resultCalls = 0;
        UiContext context = CreateContext();
        UiActionMap map = CreateMap();
        int kb = map.AddScheme(UiScheme.KeyboardMouse);
        map.AddAction(FireAction, UiActionType.Button);
        map.BindKey(FireAction, SpaceKey, UiModifiers.None, kb);
        map.OnRebindResult = s_result;
        map.Activate(context);
        context.Update(0f);

        Assert.True(map.BeginRebind(FireAction, kb));
        context.Update(0f);
        Assert.True(map.IsRebinding);

        map.Deactivate();
        Assert.False(map.IsRebinding);
        Assert.Equal(0, s_resultCalls);
        Assert.Equal(1, map.BindingCount);

        context.PushEvent(Key(WKey));
        context.Update(0f);
        Assert.False(map.IsRebinding);
        Assert.Equal(1, map.BindingCount);
        Assert.Equal(0, s_resultCalls);

        map.Activate(context);
        Assert.True(map.BeginRebind(FireAction, kb));
        context.Update(0f);
        Assert.True(map.IsRebinding);
        context.PushEvent(KeyUp(WKey));
        context.PushEvent(Key(WKey));
        context.Update(0f);
        Assert.False(map.IsRebinding);
        Assert.Equal(1, s_resultCalls);
        Assert.Equal(UiActionMap.RebindBound, s_resultStatus);
        Assert.Equal(WKey, s_resultCode);
        Assert.Equal(1, map.BindingCount);
    }

    /// <summary>
    /// The device track and <see cref="UiActionMap.OnSchemeChanged"/> keep running in a suppressed frame, and each value
    /// keeps its pre-listen state.
    /// </summary>
    [Fact]
    public void TheDeviceTrackAndTheSchemeSlotRunInASuppressedFrame()
    {
        s_schemeCalls = 0;
        UiContext context = CreateContext();
        UiActionMap map = CreateMap();
        int kb = map.AddScheme(UiScheme.KeyboardMouse);
        int gp = map.AddScheme(UiScheme.Gamepad);
        map.AddAction(MoveAction, UiActionType.Vector2);
        map.AddAction(ZoomAction, UiActionType.Axis);
        map.AddAction(FireAction, UiActionType.Button);
        map.BindGamepadStick(MoveAction, UiGamepadStick.Left);
        map.BindGamepadAxis(ZoomAction, UiGamepadAxis.LeftTrigger);
        map.BindKey(FireAction, SpaceKey, UiModifiers.None, kb);
        map.Activate(context);

        float[] held = [1f, 0f, 0f, 0f, 0.5f, 0f];
        SetPad(context, UiGamepadButtons.None, in held);
        context.PushEvent(Key(SpaceKey));
        context.Update(0f);
        Assert.Equal(kb, map.ActiveScheme);
        AssertVector(map.ReadVector(MoveAction), 1f, 0f);
        Assert.Equal(0.5f, map.ReadAxis(ZoomAction), 5);
        Assert.True(map.WasPressed(FireAction));

        map.OnSchemeChanged = s_scheme;
        Assert.True(map.BeginRebind(FireAction, kb));

        SetPad(context, UiGamepadButtons.None, in held);
        context.Update(0f);
        Assert.True(map.IsRebinding);
        Assert.Equal(kb, map.ActiveScheme);
        Assert.Equal(0, s_schemeCalls);

        SetPad(context, UiGamepadButtons.None, in held);
        context.Update(0f);
        Assert.True(map.IsRebinding);
        Assert.Equal(gp, map.ActiveScheme);
        Assert.Equal(1, s_schemeCalls);
        Assert.Equal(gp, s_schemeId);
        AssertVector(map.ReadVector(MoveAction), 1f, 0f);
        Assert.Equal(0.5f, map.ReadAxis(ZoomAction), 5);
        Assert.True(map.WasPressed(FireAction));
        Assert.True(map.IsHeld(FireAction));
        Assert.False(map.WasReleased(FireAction));

        Assert.True(map.CancelRebind());
        SetPad(context, UiGamepadButtons.None, in held);
        context.Update(0f);
        Assert.True(map.IsHeld(FireAction));
    }

    /// <summary>Writes the buttons and the axes of slot 0 of a context.</summary>
    /// <param name="context">The context.</param>
    /// <param name="buttons">The buttons that are down.</param>
    /// <param name="axes">The six axis values, in the GLFW axis order.</param>
    private static void SetPad(UiContext context, UiGamepadButtons buttons, in float[] axes)
    {
        context.SetGamepad(0, new UiGamepadState(buttons, axes));
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
