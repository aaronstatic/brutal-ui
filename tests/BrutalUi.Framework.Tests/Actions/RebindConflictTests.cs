using System;
using Brutal.Numerics;
using BrutalUi.Core;
using BrutalUi.Core.Tests;
using Xunit;

namespace BrutalUi.Framework.Tests;

/// <summary>
/// The tests of the conflict report of <see cref="UiActionMap"/>: a committed input that another action already holds
/// lands anyway, both bindings stay, and the slot names both actions.
/// </summary>
/// <remarks>
/// <para>
/// The commit of a rebind passes the throwing duplicate check of a bind. Before it removes anything, the map reads the
/// action index of a binding that clashes with the captured input. The clash rule reuses <c>SchemeClashes</c>, so a
/// binding of the default scheme clashes in either order, and two different scheme tags give no clash. The map then
/// commits, keeps both bindings, and calls <see cref="UiActionMap.RebindConflictHandler"/> with the name of the rebound
/// action and the name of the other action. The game decides what comes next (Q8, A4).
/// </para>
/// <para>
/// Each test builds a context with a <see cref="RecordingRenderer"/> measurer and a viewport of 400 by 300. A test
/// writes each slot with one <see langword="static readonly"/> handler over a <see langword="static"/> counter, and
/// resets each counter at its start, because xUnit runs classes in parallel. No public member reads one binding, so each
/// test proves a binding that stays through the frame read or through the press path. GLFW codes: Space 32, W 87, A 65,
/// S 83, D 68, J 74, Escape 256, Up 264, F1 290, Control 341.
/// </para>
/// </remarks>
public sealed class RebindConflictTests
{
    /// <summary>The GLFW key code of the Space key.</summary>
    private const int SpaceKey = 32;

    /// <summary>The GLFW key code of the W key.</summary>
    private const int WKey = 87;

    /// <summary>The GLFW key code of the A key.</summary>
    private const int AKey = 65;

    /// <summary>The name of the Button action that the tests rebind.</summary>
    private static ReadOnlySpan<byte> FireAction => "Fire"u8;

    /// <summary>The name of the Button action that holds the clashing binding.</summary>
    private static ReadOnlySpan<byte> JumpAction => "Jump"u8;

    /// <summary>The name of the command that counts its calls.</summary>
    private static ReadOnlySpan<byte> CountCommand => "count"u8;

    /// <summary>The name of the Vector2 look action that the scheme test binds.</summary>
    private static ReadOnlySpan<byte> LookAction => "Look"u8;

    /// <summary>The six axis values of a pad at rest.</summary>
    private static readonly float[] PadIdle = [0f, 0f, 0f, 0f, 0f, 0f];

    /// <summary>The handler that adds one to the counter target.</summary>
    private static readonly CommandHandler s_count = static target => ((Counter)target!).Calls++;

    private static int s_resultCalls;
    private static int s_resultStatus;
    private static int s_resultCode;

    /// <summary>The handler that counts its calls and keeps the last status and code.</summary>
    private static readonly UiActionMap.RebindResultHandler s_result = static (map, action, status, code) =>
    {
        s_resultCalls++;
        s_resultStatus = status;
        s_resultCode = code;
    };

    private static int s_conflictCalls;
    private static bool s_conflictFireName;
    private static bool s_conflictJumpName;
    private static UiActionMap? s_conflictMap;

    /// <summary>The handler that counts its calls and checks the map and the two names during the call.</summary>
    private static readonly UiActionMap.RebindConflictHandler s_conflict = static (map, action, otherAction) =>
    {
        s_conflictCalls++;
        s_conflictMap = map;
        s_conflictFireName = action.SequenceEqual(FireAction);
        s_conflictJumpName = otherAction.SequenceEqual(JumpAction);
    };

    private static int s_conflictACalls;

    /// <summary>The first handler written to the conflict slot. A later write replaces it.</summary>
    private static readonly UiActionMap.RebindConflictHandler s_conflictA = static (map, action, otherAction) =>
        s_conflictACalls++;

    private static int s_conflictBCalls;

    /// <summary>The second handler written to the conflict slot. It is the one that runs.</summary>
    private static readonly UiActionMap.RebindConflictHandler s_conflictB = static (map, action, otherAction) =>
    {
        s_conflictBCalls++;
        s_conflictMap = map;
        s_conflictFireName = action.SequenceEqual(FireAction);
        s_conflictJumpName = otherAction.SequenceEqual(JumpAction);
    };

    /// <summary>The handler that throws, to prove that a commit before the call stays.</summary>
    private static readonly UiActionMap.RebindConflictHandler s_conflictThrow =
        static (map, action, otherAction) => throw new InvalidOperationException("The conflict handler throws.");

    /// <summary>
    /// A conflicting rebind commits, grows the count by one, keeps both bindings, and names both actions once.
    /// </summary>
    [Fact]
    public void AConflictKeepsBothAndNamesBothActions()
    {
        s_resultCalls = 0;
        s_resultStatus = -1;
        s_resultCode = -1;
        s_conflictCalls = 0;
        s_conflictFireName = false;
        s_conflictJumpName = false;
        s_conflictMap = null;
        Counter counter = new();
        UiContext context = CreateContext();
        UiActionMap map = CreateMap(counter);
        int kb = map.AddScheme(UiScheme.KeyboardMouse);
        map.AddAction(JumpAction, CountCommand);
        map.AddAction(FireAction, UiActionType.Button);
        map.BindKey(JumpAction, SpaceKey, UiModifiers.None, kb);
        map.OnRebindResult = s_result;
        map.OnRebindConflict = s_conflict;
        map.Activate(context);
        context.Update(0f);
        Assert.Equal(1, map.BindingCount);

        Assert.True(map.BeginRebind(FireAction, kb));
        context.Update(0f);
        context.PushEvent(Key(SpaceKey));
        context.Update(0f);

        // The commit lands, the count grows by one, and each slot runs one time.
        Assert.False(map.IsRebinding);
        Assert.Equal(2, map.BindingCount);
        Assert.Equal(1, s_resultCalls);
        Assert.Equal(UiActionMap.RebindBound, s_resultStatus);
        Assert.Equal(SpaceKey, s_resultCode);
        Assert.Equal(1, s_conflictCalls);
        Assert.True(s_conflictFireName);
        Assert.True(s_conflictJumpName);
        Assert.Same(map, s_conflictMap);
        Assert.Equal(0, counter.Calls);
        Assert.False(map.WasPressed(FireAction));
        Assert.False(map.WasPressed(JumpAction));

        // Both bindings stay, so one fresh press drives both actions in the frame read.
        context.PushEvent(KeyUp(SpaceKey));
        context.PushEvent(Key(SpaceKey));
        context.Update(0f);
        Assert.True(map.WasPressed(FireAction));
        Assert.True(map.WasPressed(JumpAction));
        Assert.Equal(2, map.BindingCount);
        Assert.Equal(1, counter.Calls);

        // The press path resolves the first binding in the order of the bind, which is the Jump bind.
        Assert.True(map.PressKey(SpaceKey, UiModifiers.None));
        Assert.Equal(2, counter.Calls);
    }

    /// <summary>
    /// A clash against a binding of the default scheme reports in either order of the scheme argument.
    /// </summary>
    [Fact]
    public void AConflictAgainstADefaultBindingReports()
    {
        s_resultCalls = 0;
        s_conflictCalls = 0;
        s_conflictJumpName = false;
        UiContext context = CreateContext();
        UiActionMap map = CreateMap();
        int kb = map.AddScheme(UiScheme.KeyboardMouse);
        map.AddAction(JumpAction, UiActionType.Button);
        map.AddAction(FireAction, UiActionType.Button);
        map.BindKey(JumpAction, SpaceKey);
        map.OnRebindResult = s_result;
        map.OnRebindConflict = s_conflict;
        map.Activate(context);
        context.Update(0f);

        // The rebind into the default scheme clashes with the default bind of Jump.
        Assert.True(map.BeginRebind(FireAction, UiScheme.Default));
        context.Update(0f);
        context.PushEvent(Key(SpaceKey));
        context.Update(0f);
        Assert.False(map.IsRebinding);
        Assert.Equal(2, map.BindingCount);
        Assert.Equal(1, s_resultCalls);
        Assert.Equal(1, s_conflictCalls);
        Assert.True(s_conflictJumpName);

        // The rebind into a tagged scheme clashes with the same default bind.
        s_conflictJumpName = false;
        context.PushEvent(KeyUp(SpaceKey));
        context.Update(0f);
        Assert.True(map.BeginRebind(FireAction, kb));
        context.Update(0f);
        context.PushEvent(Key(SpaceKey));
        context.Update(0f);
        Assert.False(map.IsRebinding);
        Assert.Equal(3, map.BindingCount);
        Assert.Equal(2, s_resultCalls);
        Assert.Equal(2, s_conflictCalls);
        Assert.True(s_conflictJumpName);

        // Each binding of Space stays, so the frame read drives each action.
        context.PushEvent(KeyUp(SpaceKey));
        context.PushEvent(Key(SpaceKey));
        context.Update(0f);
        Assert.True(map.WasPressed(JumpAction));
        Assert.True(map.WasPressed(FireAction));
    }

    /// <summary>
    /// Two bindings of two different scheme tags give no conflict call, and the device track follows the input.
    /// </summary>
    [Fact]
    public void TwoDifferentSchemesGiveNoConflict()
    {
        s_resultCalls = 0;
        s_conflictCalls = 0;
        UiContext context = CreateContext();
        UiActionMap map = CreateMap();
        int kb = map.AddScheme(UiScheme.KeyboardMouse);
        int gp = map.AddScheme(UiScheme.Gamepad);
        map.AddAction(JumpAction, UiActionType.Button);
        map.AddAction(FireAction, UiActionType.Button);
        map.AddAction(LookAction, UiActionType.Vector2);
        map.BindKey(JumpAction, SpaceKey, UiModifiers.None, gp);
        map.BindGamepadStick(LookAction, UiGamepadStick.Left, gp);
        map.OnRebindResult = s_result;
        map.OnRebindConflict = s_conflict;
        map.Activate(context);
        SetPad(context, UiGamepadButtons.None, PadIdle);
        context.Update(0f);

        // A live stick moves the device track to the Gamepad id, and the report carries.
        SetPad(context, UiGamepadButtons.None, Axes(0.8f, 0f, 0f, 0f, 0f, 0f));
        context.Update(0f);
        Assert.Equal(gp, map.ActiveScheme);
        SetPad(context, UiGamepadButtons.None, PadIdle);
        context.Update(0f);
        Assert.Equal(gp, map.ActiveScheme);

        // The same code in another scheme tag is not a clash, so both bindings stay and no slot runs.
        Assert.True(map.BeginRebind(FireAction, kb));
        context.Update(0f);
        context.PushEvent(Key(SpaceKey));
        context.Update(0f);
        Assert.False(map.IsRebinding);
        Assert.Equal(3, map.BindingCount);
        Assert.Equal(1, s_resultCalls);
        Assert.Equal(0, s_conflictCalls);

        // The report follows the device of the captured input, not the tag of the other binding.
        Assert.Equal(kb, map.ActiveScheme);

        // The Gamepad-tagged bind of Space still drives Jump, and the new bind drives Fire.
        context.PushEvent(KeyUp(SpaceKey));
        context.PushEvent(Key(SpaceKey));
        context.Update(0f);
        Assert.True(map.WasPressed(JumpAction));
        Assert.True(map.WasPressed(FireAction));
    }

    /// <summary>
    /// The conflict slot holds one handler: a later write replaces the first, a null slot calls nothing, and a handler
    /// that throws leaves the commit in place.
    /// </summary>
    [Fact]
    public void TheConflictSlotTakesOneHandlerWrittenWithEquals()
    {
        s_resultCalls = 0;
        s_conflictACalls = 0;
        s_conflictBCalls = 0;
        s_conflictFireName = false;
        s_conflictJumpName = false;
        s_conflictMap = null;
        UiContext context = CreateContext();
        UiActionMap map = CreateMap();
        int kb = map.AddScheme(UiScheme.KeyboardMouse);
        map.AddAction(JumpAction, UiActionType.Button);
        map.AddAction(FireAction, UiActionType.Button);
        map.BindKey(JumpAction, SpaceKey, UiModifiers.None, kb);
        map.BindKey(JumpAction, WKey, UiModifiers.None, kb);
        map.BindKey(JumpAction, AKey, UiModifiers.None, kb);
        map.OnRebindResult = s_result;
        map.OnRebindConflict = s_conflictA;
        map.OnRebindConflict = s_conflictB;
        map.Activate(context);
        context.Update(0f);
        Assert.Equal(3, map.BindingCount);

        // One clash gives one call, to the handler of the last write, with the map and the two names.
        Assert.True(map.BeginRebind(FireAction, kb));
        context.Update(0f);
        context.PushEvent(Key(SpaceKey));
        context.Update(0f);
        Assert.Equal(4, map.BindingCount);
        Assert.Equal(1, s_resultCalls);
        Assert.Equal(1, s_conflictBCalls);
        Assert.Equal(0, s_conflictACalls);
        Assert.Same(map, s_conflictMap);
        Assert.True(s_conflictFireName);
        Assert.True(s_conflictJumpName);

        // A null slot calls nothing, and the commit still lands.
        s_conflictFireName = false;
        map.OnRebindConflict = null;
        context.PushEvent(KeyUp(SpaceKey));
        context.Update(0f);
        Assert.True(map.BeginRebind(FireAction, kb));
        context.Update(0f);
        context.PushEvent(Key(WKey));
        context.Update(0f);
        Assert.False(map.IsRebinding);
        Assert.Equal(2, s_resultCalls);
        Assert.Equal(1, s_conflictBCalls);
        Assert.False(s_conflictFireName);
        Assert.Equal(4, map.BindingCount);

        context.PushEvent(KeyUp(WKey));
        context.PushEvent(Key(WKey));
        context.Update(0f);
        Assert.True(map.WasPressed(FireAction));
        context.PushEvent(KeyUp(WKey));
        context.PushEvent(Key(SpaceKey));
        context.Update(0f);
        Assert.False(map.WasPressed(FireAction));
        Assert.True(map.WasPressed(JumpAction));

        // A handler that throws finds the listen closed and the new binding in place.
        map.OnRebindConflict = s_conflictThrow;
        context.PushEvent(KeyUp(SpaceKey));
        context.Update(0f);
        Assert.True(map.BeginRebind(FireAction, kb));
        context.Update(0f);
        context.PushEvent(Key(AKey));
        Assert.Throws<InvalidOperationException>(() => context.Update(0f));
        Assert.False(map.IsRebinding);
        Assert.Equal(3, s_resultCalls);
        Assert.Equal(4, map.BindingCount);

        context.PushEvent(KeyUp(AKey));
        context.PushEvent(Key(AKey));
        context.Update(0f);
        Assert.True(map.WasPressed(FireAction));
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

    /// <summary>A target that counts the calls of its command.</summary>
    private sealed class Counter
    {
        /// <summary>The count of calls.</summary>
        public int Calls;
    }
}
