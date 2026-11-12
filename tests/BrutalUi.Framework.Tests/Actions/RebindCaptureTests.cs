using System;
using Brutal.Numerics;
using BrutalUi.Core;
using BrutalUi.Core.Tests;
using Xunit;

namespace BrutalUi.Framework.Tests;

/// <summary>
/// The tests of the seven capture shapes of <see cref="UiActionMap"/>: the scan order, the fit table, the modifier
/// rule, and the reject that keeps the listen open.
/// </summary>
/// <remarks>
/// <para>
/// Phase 2 widens the listen scan to the seven capturable shapes of <c>InputKind</c>: a key, a mouse button, a
/// gamepad button, a stick, a gamepad axis, a pointer delta, and a scroll. The scan takes the first live shape of that
/// order. The fit table gives a key, a mouse button, or a gamepad button to a Button action, a gamepad axis or a scroll
/// to an Axis action, and a stick or a pointer delta to a Vector2 action. A mismatch lands no binding, keeps the listen
/// open, and reports <see cref="UiActionMap.RebindRejected"/> with the code of the candidate.
/// </para>
/// <para>
/// Each test builds a context with a <see cref="RecordingRenderer"/> measurer and a viewport of 400 by 300. A test
/// writes a pad with <see cref="UiContext.SetGamepad(int, in UiGamepadState)"/> before the update, and builds its
/// events by hand. A test writes the result slot with one <see langword="static readonly"/> handler over a
/// <see langword="static"/> counter, and resets the counter at its start, because xUnit runs classes in parallel. No
/// public member reads one binding, so each test proves the captured kind through the frame read of the next frame.
/// GLFW codes: Space 32, W 87, A 65, S 83, D 68, Escape 256, F1 290, Control 341.
/// </para>
/// </remarks>
public sealed class RebindCaptureTests
{
    /// <summary>The GLFW key code of the Space key.</summary>
    private const int SpaceKey = 32;

    /// <summary>The GLFW key code of the W key.</summary>
    private const int WKey = 87;

    /// <summary>The GLFW key code of the A key.</summary>
    private const int AKey = 65;

    /// <summary>The GLFW key code of the S key.</summary>
    private const int SKey = 83;

    /// <summary>The GLFW key code of the D key.</summary>
    private const int DKey = 68;

    /// <summary>The GLFW key code of F1.</summary>
    private const int F1Key = 290;

    /// <summary>The GLFW key code of the left Control key.</summary>
    private const int ControlKey = 341;

    /// <summary>The pointer position that no widget covers.</summary>
    private static readonly float2 Outside = new(300f, 200f);

    /// <summary>The name of the Button action that the tests rebind.</summary>
    private static ReadOnlySpan<byte> FireAction => "Fire"u8;

    /// <summary>The name of the Vector2 action that the tests rebind.</summary>
    private static ReadOnlySpan<byte> MoveAction => "Move"u8;

    /// <summary>The name of the Vector2 look action that the tests rebind.</summary>
    private static ReadOnlySpan<byte> LookAction => "Look"u8;

    /// <summary>The name of the Axis action that the tests rebind.</summary>
    private static ReadOnlySpan<byte> ZoomAction => "Zoom"u8;

    /// <summary>The name of the command that counts its calls.</summary>
    private static ReadOnlySpan<byte> CountCommand => "count"u8;

    /// <summary>The handler that counts its calls and keeps the last status and code.</summary>
    private static readonly UiActionMap.RebindResultHandler s_result = static (map, action, status, code) =>
    {
        s_resultCalls++;
        s_resultStatus = status;
        s_resultCode = code;
    };

    private static int s_resultCalls;
    private static int s_resultStatus;
    private static int s_resultCode;

    /// <summary>The handler that adds one to the counter target.</summary>
    private static readonly CommandHandler s_count = static target => ((Counter)target!).Calls++;

    /// <summary>The six axis values of a pad at rest.</summary>
    private static readonly float[] PadIdle = [0f, 0f, 0f, 0f, 0f, 0f];

    /// <summary>
    /// A fresh right press with Shift and Control commits a mouse binding with the modifiers of the frame, and a plain
    /// press of the same button gives no flag.
    /// </summary>
    [Fact]
    public void AMouseButtonPressCommitsAMouseBinding()
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

        context.PushEvent(Pointer(UiEventType.PointerMove, Outside));
        context.Update(0f);
        context.PushEvent(Key(ControlKey, UiModifiers.Shift | UiModifiers.Control));
        context.Update(0f);

        Assert.True(map.BeginRebind(FireAction, kb));
        context.Update(0f);
        Assert.True(map.IsRebinding);
        Assert.Equal(0, s_resultCalls);

        context.PushEvent(Pointer(UiEventType.PointerDown, Outside, PointerButton.Right, UiModifiers.Shift | UiModifiers.Control));
        context.Update(0f);
        Assert.False(map.IsRebinding);
        Assert.Equal(1, s_resultCalls);
        Assert.Equal(UiActionMap.RebindBound, s_resultStatus);
        Assert.Equal((int)PointerButton.Right, s_resultCode);
        Assert.Equal(1, map.BindingCount);
        Assert.Equal(0, counter.Calls);
        Assert.False(map.WasPressed(FireAction));

        context.Update(0f);
        Assert.False(map.IsHeld(FireAction));
        Assert.Equal(0, counter.Calls);

        // The Button latch starts from a press edge, so a fresh press with the same modifiers reads the new bind.
        context.PushEvent(Pointer(UiEventType.PointerUp, Outside, PointerButton.Right, UiModifiers.Shift | UiModifiers.Control));
        context.Update(0f);
        context.PushEvent(Pointer(UiEventType.PointerDown, Outside, PointerButton.Right, UiModifiers.Shift | UiModifiers.Control));
        context.Update(0f);
        Assert.True(map.WasPressed(FireAction));
        Assert.True(map.IsHeld(FireAction));
        Assert.Equal(1, counter.Calls);

        // A plain press of the same button gives no flag, because the bind carries Shift and Control.
        context.PushEvent(Pointer(UiEventType.PointerUp, Outside, PointerButton.Right, UiModifiers.Shift | UiModifiers.Control));
        context.PushEvent(KeyUp(ControlKey));
        context.Update(0f);
        Assert.False(map.IsHeld(FireAction));

        context.PushEvent(Pointer(UiEventType.PointerDown, Outside, PointerButton.Right));
        context.Update(0f);
        Assert.False(map.IsHeld(FireAction));
        Assert.False(map.WasPressed(FireAction));
        Assert.Equal(1, counter.Calls);
        Assert.Equal(1, map.BindingCount);
    }

    /// <summary>
    /// One push with two gamepad buttons captures the lowest set bit with no modifier, and the frame read latches on
    /// that bit.
    /// </summary>
    [Fact]
    public void AGamepadButtonPressCommitsABindingWithNoModifier()
    {
        s_resultCalls = 0;
        Counter counter = new();
        UiContext context = CreateContext();
        UiActionMap map = CreateMap(counter);
        int gp = map.AddScheme(UiScheme.Gamepad);
        map.AddAction(FireAction, CountCommand);
        map.BindKey(FireAction, SpaceKey, UiModifiers.None, gp);
        map.OnRebindResult = s_result;
        map.Activate(context);
        SetPad(context, UiGamepadButtons.None, PadIdle);
        context.Update(0f);

        Assert.True(map.BeginRebind(FireAction, gp));
        SetPad(context, UiGamepadButtons.None, PadIdle);
        context.Update(0f);
        Assert.True(map.IsRebinding);
        Assert.Equal(0, s_resultCalls);

        SetPad(context, UiGamepadButtons.X | UiGamepadButtons.Y, PadIdle);
        context.Update(0f);
        Assert.False(map.IsRebinding);
        Assert.Equal(1, s_resultCalls);
        Assert.Equal(UiActionMap.RebindBound, s_resultStatus);
        Assert.Equal((int)UiGamepadButtons.X, s_resultCode);
        Assert.Equal(1, map.BindingCount);
        Assert.Equal(0, counter.Calls);
        Assert.False(map.WasPressed(FireAction));

        // The commit frame runs no pass, so the press that landed the binding drives nothing.
        SetPad(context, UiGamepadButtons.None, PadIdle);
        context.Update(0f);
        Assert.False(map.IsHeld(FireAction));
        Assert.False(map.WasPressed(FireAction));

        // A press of the other bit gives nothing: the capture took the lowest bit of the two.
        SetPad(context, UiGamepadButtons.Y, PadIdle);
        context.Update(0f);
        Assert.False(map.IsHeld(FireAction));
        Assert.False(map.WasPressed(FireAction));
        Assert.Equal(0, counter.Calls);

        SetPad(context, UiGamepadButtons.None, PadIdle);
        context.Update(0f);
        SetPad(context, UiGamepadButtons.X, PadIdle);
        context.Update(0f);
        Assert.True(map.IsHeld(FireAction));
        Assert.True(map.WasPressed(FireAction));
        Assert.Equal(1, counter.Calls);

        SetPad(context, UiGamepadButtons.None, PadIdle);
        context.Update(0f);
        Assert.True(map.WasReleased(FireAction));
        Assert.False(map.IsHeld(FireAction));

        context.PushEvent(Key(ControlKey, UiModifiers.Control));
        SetPad(context, UiGamepadButtons.None, PadIdle);
        context.Update(0f);
        SetPad(context, UiGamepadButtons.X, PadIdle);
        context.Update(0f);
        Assert.True(map.WasPressed(FireAction));
        Assert.Equal(2, counter.Calls);
    }

    /// <summary>
    /// A stick away from rest commits a stick binding: the left stick gives code 0 and the right stick gives code 1, and
    /// a stick at rest gives no candidate.
    /// </summary>
    [Fact]
    public void AStickAwayFromRestCommitsAStickBinding()
    {
        s_resultCalls = 0;
        UiContext context = CreateContext();
        UiActionMap map = CreateMap();
        int gp = map.AddScheme(UiScheme.Gamepad);
        map.AddAction(MoveAction, UiActionType.Vector2);
        map.BindKeyVector(MoveAction, WKey, AKey, SKey, DKey, gp);
        map.OnRebindResult = s_result;
        map.Activate(context);
        SetPad(context, UiGamepadButtons.None, PadIdle);
        context.Update(0f);

        Assert.True(map.BeginRebind(MoveAction, gp));
        SetPad(context, UiGamepadButtons.None, PadIdle);
        context.Update(0f);
        Assert.True(map.IsRebinding);

        // A stick below the poll deadzone reads exactly (0, 0), so the same no-candidate shape covers both cases (A5).
        SetPad(context, UiGamepadButtons.None, PadIdle);
        context.Update(0f);
        Assert.True(map.IsRebinding);
        Assert.Equal(0, s_resultCalls);
        Assert.Equal(1, map.BindingCount);

        SetPad(context, UiGamepadButtons.None, Axes(0.4f, -0.2f, 0f, 0f, 0f, 0f));
        context.Update(0f);
        Assert.False(map.IsRebinding);
        Assert.Equal(1, s_resultCalls);
        Assert.Equal(UiActionMap.RebindBound, s_resultStatus);
        Assert.Equal((int)UiGamepadStick.Left, s_resultCode);
        Assert.Equal(1, map.BindingCount);

        SetPad(context, UiGamepadButtons.None, Axes(0.4f, -0.2f, 0f, 0f, 0f, 0f));
        context.Update(0f);
        AssertVector(map.ReadVector(MoveAction), 0.4f, 0.2f);

        SetPad(context, UiGamepadButtons.None, PadIdle);
        context.Update(0f);
        Assert.True(map.BeginRebind(MoveAction, gp));
        SetPad(context, UiGamepadButtons.None, PadIdle);
        context.Update(0f);
        Assert.True(map.IsRebinding);

        SetPad(context, UiGamepadButtons.None, Axes(0f, 0f, 0f, 0.5f, 0f, 0f));
        context.Update(0f);
        Assert.False(map.IsRebinding);
        Assert.Equal(2, s_resultCalls);
        Assert.Equal((int)UiGamepadStick.Right, s_resultCode);
        Assert.Equal(1, map.BindingCount);

        SetPad(context, UiGamepadButtons.None, Axes(0f, 0f, 0f, 0.5f, 0f, 0f));
        context.Update(0f);
        AssertVector(map.ReadVector(MoveAction), 0f, -0.5f);
    }

    /// <summary>
    /// A trigger above rest commits an axis binding with its axis code, and a stick that is live in the same frame wins
    /// the order over the axis.
    /// </summary>
    [Fact]
    public void ATriggerAboveRestCommitsAnAxisBinding()
    {
        s_resultCalls = 0;
        UiContext context = CreateContext();
        UiActionMap map = CreateMap();
        int gp = map.AddScheme(UiScheme.Gamepad);
        map.AddAction(ZoomAction, UiActionType.Axis);
        map.OnRebindResult = s_result;
        map.Activate(context);
        SetPad(context, UiGamepadButtons.None, PadIdle);
        context.Update(0f);

        Assert.True(map.BeginRebind(ZoomAction, gp));
        SetPad(context, UiGamepadButtons.None, PadIdle);
        context.Update(0f);

        // A trigger at exactly 0 is no candidate, because the poll baked the deadzone into the stored value.
        SetPad(context, UiGamepadButtons.None, PadIdle);
        context.Update(0f);
        Assert.True(map.IsRebinding);
        Assert.Equal(0, s_resultCalls);
        Assert.Equal(0, map.BindingCount);

        // The stick sits before the axis: axis 2 away from rest is a right-stick candidate, and the Axis target rejects
        // it, so the listen stays open and no axis binding lands.
        SetPad(context, UiGamepadButtons.None, Axes(0f, 0f, 0.6f, 0f, 0.3f, 0f));
        context.Update(0f);
        Assert.True(map.IsRebinding);
        Assert.Equal(1, s_resultCalls);
        Assert.Equal(UiActionMap.RebindRejected, s_resultStatus);
        Assert.Equal((int)UiGamepadStick.Right, s_resultCode);
        Assert.Equal(0, map.BindingCount);

        SetPad(context, UiGamepadButtons.None, Axes(0f, 0f, 0f, 0f, 0.3f, 0f));
        context.Update(0f);
        Assert.False(map.IsRebinding);
        Assert.Equal(2, s_resultCalls);
        Assert.Equal(UiActionMap.RebindBound, s_resultStatus);
        Assert.Equal((int)UiGamepadAxis.LeftTrigger, s_resultCode);
        Assert.Equal(1, map.BindingCount);

        SetPad(context, UiGamepadButtons.None, Axes(0f, 0f, 0f, 0f, 0.3f, 0f));
        context.Update(0f);
        Assert.Equal(0.3f, map.ReadAxis(ZoomAction), 5);
    }

    /// <summary>
    /// A pointer delta above the threshold commits a delta binding with no code and a scale of 1, and a small or a
    /// non-finite delta gives no capture.
    /// </summary>
    [Fact]
    public void APointerDeltaAboveTheThresholdCommitsADeltaBinding()
    {
        s_resultCalls = 0;
        UiContext context = CreateContext();
        UiActionMap map = CreateMap();
        int kb = map.AddScheme(UiScheme.KeyboardMouse);
        map.AddAction(LookAction, UiActionType.Vector2);
        map.BindKeyVector(LookAction, WKey, AKey, SKey, DKey, kb);
        map.OnRebindResult = s_result;
        map.Activate(context);
        context.PushEvent(Pointer(UiEventType.PointerMove, new float2(100f, 100f)));
        context.Update(0f);

        Assert.True(map.BeginRebind(LookAction, kb));
        context.Update(0f);
        Assert.True(map.IsRebinding);

        // A move of two pixels is below the eight-pixel threshold, so it gives no capture.
        context.PushEvent(Pointer(UiEventType.PointerMove, new float2(102f, 100f)));
        context.Update(0f);
        Assert.True(map.IsRebinding);
        Assert.Equal(0, s_resultCalls);
        Assert.Equal(1, map.BindingCount);

        // A delta whose length is not finite gives no capture. The Core delta is always finite, so the test reaches the
        // guard through a length that overflows the float range.
        context.PushEvent(Pointer(UiEventType.PointerMove, new float2(1e20f, 1e20f)));
        context.Update(0f);
        Assert.True(map.IsRebinding);
        Assert.Equal(0, s_resultCalls);

        context.PushEvent(Pointer(UiEventType.PointerMove, new float2(100f, 100f)));
        context.Update(0f);
        Assert.True(map.IsRebinding);
        Assert.Equal(0, s_resultCalls);

        context.PushEvent(Pointer(UiEventType.PointerMove, new float2(140f, 70f)));
        context.Update(0f);
        Assert.False(map.IsRebinding);
        Assert.Equal(1, s_resultCalls);
        Assert.Equal(UiActionMap.RebindBound, s_resultStatus);
        Assert.Equal(-1, s_resultCode);
        Assert.Equal(1, map.BindingCount);

        context.Update(0f);
        AssertVector(map.ReadVector(LookAction), 0f, 0f);

        context.PushEvent(Pointer(UiEventType.PointerMove, new float2(170f, 70f)));
        context.Update(0f);
        AssertVector(map.ReadVector(LookAction), 30f, 0f);
    }

    /// <summary>
    /// A scroll with a non-zero Y commits a scroll binding with no code, and a scroll with only an X gives no capture.
    /// </summary>
    [Fact]
    public void ANonZeroScrollCommitsAScrollBinding()
    {
        s_resultCalls = 0;
        UiContext context = CreateContext();
        UiActionMap map = CreateMap();
        int kb = map.AddScheme(UiScheme.KeyboardMouse);
        map.AddAction(ZoomAction, UiActionType.Axis);
        map.OnRebindResult = s_result;
        map.Activate(context);
        context.PushEvent(Scroll(new float2(100f, 100f), new float2(0f, 0f)));
        context.Update(0f);

        Assert.True(map.BeginRebind(ZoomAction, kb));
        context.Update(0f);
        Assert.True(map.IsRebinding);

        // The frame read uses ScrollDelta.Y only, so a scroll with only an X is no candidate.
        context.PushEvent(Scroll(new float2(100f, 100f), new float2(2f, 0f)));
        context.Update(0f);
        Assert.True(map.IsRebinding);
        Assert.Equal(0, s_resultCalls);
        Assert.Equal(0, map.BindingCount);

        context.PushEvent(Scroll(new float2(100f, 100f), new float2(0f, 3f)));
        context.Update(0f);
        Assert.False(map.IsRebinding);
        Assert.Equal(1, s_resultCalls);
        Assert.Equal(UiActionMap.RebindBound, s_resultStatus);
        Assert.Equal(-1, s_resultCode);
        Assert.Equal(1, map.BindingCount);

        context.PushEvent(Scroll(new float2(100f, 100f), new float2(0f, 2f)));
        context.Update(0f);
        Assert.Equal(2f, map.ReadAxis(ZoomAction), 5);
    }

    /// <summary>
    /// One frame with three live shapes gives the first one of the table order, and a later frame gives the next shape.
    /// </summary>
    [Fact]
    public void TheScanTakesTheFirstCandidateInTheTableOrder()
    {
        s_resultCalls = 0;
        UiContext context = CreateContext();
        UiActionMap map = CreateMap();
        int kb = map.AddScheme(UiScheme.KeyboardMouse);
        map.AddAction(MoveAction, UiActionType.Vector2);
        map.BindKeyVector(MoveAction, WKey, AKey, SKey, DKey, kb);
        map.OnRebindResult = s_result;
        map.Activate(context);
        SetPad(context, UiGamepadButtons.None, PadIdle);
        context.Update(0f);

        Assert.True(map.BeginRebind(MoveAction, kb));
        SetPad(context, UiGamepadButtons.None, PadIdle);
        context.Update(0f);

        // A key press, a stick away from rest, and a scroll in one frame: the key is the first shape of the order, and
        // the Vector2 target rejects it, so the listen stays open.
        context.PushEvent(Key(WKey));
        SetPad(context, UiGamepadButtons.None, Axes(0.5f, 0f, 0f, 0f, 0f, 0f));
        context.PushEvent(Scroll(new float2(100f, 100f), new float2(0f, 2f)));
        context.Update(0f);
        Assert.True(map.IsRebinding);
        Assert.Equal(1, s_resultCalls);
        Assert.Equal(UiActionMap.RebindRejected, s_resultStatus);
        Assert.Equal(WKey, s_resultCode);
        Assert.Equal(1, map.BindingCount);

        // The next frame holds only the stick, so the stick is the candidate and the Vector2 target takes it.
        context.PushEvent(KeyUp(WKey));
        SetPad(context, UiGamepadButtons.None, Axes(0.5f, 0f, 0f, 0f, 0f, 0f));
        context.Update(0f);
        Assert.False(map.IsRebinding);
        Assert.Equal(2, s_resultCalls);
        Assert.Equal(UiActionMap.RebindBound, s_resultStatus);
        Assert.Equal((int)UiGamepadStick.Left, s_resultCode);
        Assert.Equal(1, map.BindingCount);

        SetPad(context, UiGamepadButtons.None, Axes(0.5f, 0f, 0f, 0f, 0f, 0f));
        context.Update(0f);
        AssertVector(map.ReadVector(MoveAction), 0.5f, 0f);

        // A fresh listen on the same target with only the scroll live rejects the scroll, which holds no code.
        SetPad(context, UiGamepadButtons.None, PadIdle);
        context.Update(0f);
        Assert.True(map.BeginRebind(MoveAction, kb));
        context.Update(0f);
        context.PushEvent(Scroll(new float2(100f, 100f), new float2(0f, 2f)));
        context.Update(0f);
        Assert.True(map.IsRebinding);
        Assert.Equal(3, s_resultCalls);
        Assert.Equal(UiActionMap.RebindRejected, s_resultStatus);
        Assert.Equal(-1, s_resultCode);
        Assert.Equal(1, map.BindingCount);

        Assert.True(map.CancelRebind());
    }

    /// <summary>
    /// A mismatched capture keeps the listen open, reports the reason in each frame that it stays live, and lands no
    /// binding.
    /// </summary>
    [Fact]
    public void AMismatchedCaptureStaysInTheListenAndReportsTheReason()
    {
        s_resultCalls = 0;
        Counter counter = new();
        UiContext context = CreateContext();
        UiActionMap map = CreateMap(counter);
        int kb = map.AddScheme(UiScheme.KeyboardMouse);
        map.AddAction(MoveAction, UiActionType.Vector2);
        map.AddAction(FireAction, CountCommand);
        map.AddAction(ZoomAction, UiActionType.Axis);
        map.BindKeyVector(MoveAction, WKey, AKey, SKey, DKey, kb);
        map.BindKey(FireAction, SpaceKey, UiModifiers.None, kb);
        map.BindScroll(ZoomAction, kb);
        map.OnRebindResult = s_result;
        map.Activate(context);
        SetPad(context, UiGamepadButtons.None, PadIdle);
        context.Update(0f);
        int bindingCount = map.BindingCount;

        // A key on a Vector2 target.
        Assert.True(map.BeginRebind(MoveAction, kb));
        context.Update(0f);
        context.PushEvent(Key(F1Key));
        context.Update(0f);
        Assert.True(map.IsRebinding);
        Assert.Equal(1, s_resultCalls);
        Assert.Equal(UiActionMap.RebindRejected, s_resultStatus);
        Assert.Equal(F1Key, s_resultCode);
        Assert.Equal(bindingCount, map.BindingCount);
        Assert.Equal(0, counter.Calls);
        Assert.True(map.CancelRebind());

        // A stick on a Button target that stays away from rest for three frames reports three times.
        s_resultCalls = 0;
        Assert.True(map.BeginRebind(FireAction, kb));
        context.Update(0f);
        for (int frame = 0; frame < 3; frame++)
        {
            SetPad(context, UiGamepadButtons.None, Axes(0.5f, 0f, 0f, 0f, 0f, 0f));
            context.Update(0f);
            Assert.True(map.IsRebinding);
            Assert.Equal(frame + 1, s_resultCalls);
            Assert.Equal(UiActionMap.RebindRejected, s_resultStatus);
            Assert.Equal((int)UiGamepadStick.Left, s_resultCode);
        }

        Assert.Equal(bindingCount, map.BindingCount);
        Assert.Equal(0, counter.Calls);
        Assert.True(map.CancelRebind());

        // A key on an Axis target.
        s_resultCalls = 0;
        Assert.True(map.BeginRebind(ZoomAction, kb));
        SetPad(context, UiGamepadButtons.None, PadIdle);
        context.Update(0f);
        context.PushEvent(Key(WKey));
        context.Update(0f);
        Assert.True(map.IsRebinding);
        Assert.Equal(1, s_resultCalls);
        Assert.Equal(UiActionMap.RebindRejected, s_resultStatus);
        Assert.Equal(WKey, s_resultCode);
        Assert.Equal(bindingCount, map.BindingCount);
        Assert.True(map.CancelRebind());

        // Each old binding still drives its action after the three rejects.
        context.PushEvent(KeyUp(F1Key));
        context.PushEvent(KeyUp(WKey));
        SetPad(context, UiGamepadButtons.None, PadIdle);
        context.Update(0f);
        context.PushEvent(Key(WKey));
        context.Update(0f);
        AssertVector(map.ReadVector(MoveAction), 0f, 1f);

        context.PushEvent(KeyUp(WKey));
        context.PushEvent(Key(SpaceKey));
        context.Update(0f);
        Assert.True(map.WasPressed(FireAction));
        Assert.Equal(1, counter.Calls);

        context.PushEvent(KeyUp(SpaceKey));
        context.PushEvent(Scroll(new float2(100f, 100f), new float2(0f, 3f)));
        context.Update(0f);
        Assert.Equal(3f, map.ReadAxis(ZoomAction), 5);
        Assert.Equal(bindingCount, map.BindingCount);
    }

    /// <summary>Writes the buttons and the axes of slot 0 of a context.</summary>
    /// <param name="context">The context.</param>
    /// <param name="buttons">The buttons that are down.</param>
    /// <param name="axes">The six axis values, in the GLFW axis order.</param>
    private static void SetPad(UiContext context, UiGamepadButtons buttons, in float[] axes)
    {
        context.SetGamepad(0, new UiGamepadState(buttons, axes));
    }

    /// <summary>Builds the six axis values of a pad write.</summary>
    /// <param name="a0">Axis 0: the left stick X.</param>
    /// <param name="a1">Axis 1: the left stick Y.</param>
    /// <param name="a2">Axis 2: the right stick X.</param>
    /// <param name="a3">Axis 3: the right stick Y.</param>
    /// <param name="a4">Axis 4: the left trigger.</param>
    /// <param name="a5">Axis 5: the right trigger.</param>
    /// <returns>The axis array for <see cref="SetPad"/>.</returns>
    private static float[] Axes(float a0, float a1, float a2, float a3, float a4, float a5)
    {
        return [a0, a1, a2, a3, a4, a5];
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
