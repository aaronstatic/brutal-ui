using System;
using Brutal.Numerics;
using BrutalUi.Core;
using BrutalUi.Core.Tests;
using Xunit;

namespace BrutalUi.Framework.Tests;

/// <summary>
/// The tests of the defaults snapshot and the restore of <see cref="UiActionMap"/>: the first activation copies the
/// bindings, and <see cref="UiActionMap.ResetBindings"/> writes them back.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="UiActionMap.Activate"/> calls <c>CaptureDefaults</c> on the first activation only, so the snapshot holds
/// the bindings of the build, before any rebind. A restore copies the snapshot into the existing array and restores the
/// count, so it allocates nothing, and it closes an open listen with no commit and no slot call. The scope is the
/// bindings: the actions, the action types, the commands, the scheme table, the gamepad slot, and the scheme lock keep
/// their state (requirements line 15). A map that no activation reached holds no snapshot, so the call gives
/// <see langword="false"/> and changes nothing. A bind after the first activation is gone after a restore (A6).
/// </para>
/// <para>
/// Each test builds a context with a <see cref="RecordingRenderer"/> measurer and a viewport of 400 by 300. A test
/// writes each slot with one <see langword="static readonly"/> handler over a <see langword="static"/> counter, and
/// resets each counter at its start, because xUnit runs classes in parallel. The <see cref="UiActionMap.ReadBindings"/>
/// half of A6 sits in <see cref="BindingsRoundTripTests"/>. GLFW codes: Space 32, W 87, A 65, S 83, D 68, J 74,
/// Escape 256, Up 264, F1 290.
/// </para>
/// </remarks>
public sealed class RebindResetTests
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

    /// <summary>The GLFW key code of the J key.</summary>
    private const int JKey = 74;

    /// <summary>The GLFW key code of the Escape key.</summary>
    private const int EscapeKey = 256;

    /// <summary>The GLFW key code of the Up arrow key.</summary>
    private const int UpKey = 264;

    /// <summary>The GLFW key code of F1.</summary>
    private const int F1Key = 290;

    /// <summary>The name of the Button action that the tests rebind.</summary>
    private static ReadOnlySpan<byte> FireAction => "Fire"u8;

    /// <summary>The name of the Button action that holds a clashing binding.</summary>
    private static ReadOnlySpan<byte> JumpAction => "Jump"u8;

    /// <summary>The name of the Vector2 action that the scope test binds.</summary>
    private static ReadOnlySpan<byte> MoveAction => "Move"u8;

    /// <summary>The name of the Axis action that the scope test binds.</summary>
    private static ReadOnlySpan<byte> ZoomAction => "Zoom"u8;

    /// <summary>The name of the scheme that the scope test adds.</summary>
    private static ReadOnlySpan<byte> TouchScheme => "Touch"u8;

    /// <summary>The name of the command that counts its calls.</summary>
    private static ReadOnlySpan<byte> CountCommand => "count"u8;

    private static int s_resultCalls;
    private static int s_resultStatus;
    private static int s_conflictCalls;

    /// <summary>The handler that counts its calls and keeps the last status.</summary>
    private static readonly UiActionMap.RebindResultHandler s_result = static (map, action, status, code) =>
    {
        s_resultCalls++;
        s_resultStatus = status;
    };

    /// <summary>The handler that counts its calls.</summary>
    private static readonly UiActionMap.RebindConflictHandler s_conflict = static (map, action, otherAction) =>
        s_conflictCalls++;

    /// <summary>The handler that adds one to the counter target.</summary>
    private static readonly CommandHandler s_count = static target => ((Counter)target!).Calls++;

    /// <summary>
    /// A restore after a rebind gives the bindings of the build back, and the rebound key drives nothing.
    /// </summary>
    [Fact]
    public void ResetBindingsRestoresTheBuildDefaultsAfterARebind()
    {
        s_resultCalls = 0;
        UiContext context = CreateContext();
        UiActionMap map = CreateMap();
        map.AddAction(UiActions.Cancel, CountCommand);
        map.AddAction(UiActions.NavigateUp, UiActionType.Button);
        map.BindKey(UiActions.Cancel, EscapeKey);
        map.BindKey(UiActions.NavigateUp, UpKey);
        map.OnRebindResult = s_result;
        map.Activate(context);
        context.Update(0f);
        int buildCount = map.BindingCount;
        Assert.Equal(2, buildCount);

        Assert.True(map.BeginRebind(UiActions.NavigateUp, UiScheme.Default));
        context.Update(0f);
        context.PushEvent(Key(JKey));
        context.Update(0f);
        Assert.False(map.IsRebinding);
        Assert.Equal(buildCount, map.BindingCount);
        Assert.Equal(1, s_resultCalls);
        Assert.Equal(UiActionMap.RebindBound, s_resultStatus);

        Assert.True(map.ResetBindings());
        Assert.Equal(buildCount, map.BindingCount);

        // The build binding drives NavigateUp again, and the rebound key gives nothing.
        context.PushEvent(KeyUp(JKey));
        context.PushEvent(Key(UpKey));
        context.Update(0f);
        Assert.True(map.WasPressed(UiActions.NavigateUp));
        Assert.True(map.IsHeld(UiActions.NavigateUp));

        // A second rebind after a restore still restores the bindings of the build. The restore never reassigns the
        // array, so a new binding cannot move the snapshot.
        Assert.True(map.BeginRebind(UiActions.NavigateUp, UiScheme.Default));
        context.Update(0f);
        context.PushEvent(KeyUp(UpKey));
        context.PushEvent(Key(JKey));
        context.Update(0f);
        Assert.False(map.IsRebinding);
        Assert.True(map.ResetBindings());
        Assert.Equal(buildCount, map.BindingCount);

        context.PushEvent(KeyUp(JKey));
        context.PushEvent(Key(UpKey));
        context.Update(0f);
        Assert.True(map.WasPressed(UiActions.NavigateUp));

        context.PushEvent(KeyUp(UpKey));
        context.Update(0f);

        // Cancel still reads on Escape, because the restore kept the other build binding.
        context.PushEvent(Key(EscapeKey));
        context.Update(0f);
        Assert.True(map.WasPressed(UiActions.Cancel));
        Assert.False(map.WasPressed(UiActions.NavigateUp));
        Assert.Equal(buildCount, map.BindingCount);
    }

    /// <summary>
    /// The snapshot comes from the first activation: a map that never activated restores nothing, and a bind after the
    /// first activation is gone after a restore.
    /// </summary>
    [Fact]
    public void TheSnapshotComesFromTheFirstActivate()
    {
        UiContext context = CreateContext();
        UiActionMap map = CreateMap();
        map.AddAction(JumpAction, UiActionType.Button);
        map.AddAction(FireAction, UiActionType.Button);
        map.BindKey(JumpAction, SpaceKey);

        // No activation means no snapshot: the call gives false and changes nothing.
        Assert.False(map.ResetBindings());
        Assert.Equal(1, map.BindingCount);

        map.Activate(context);
        context.Update(0f);

        // A bind after the first activation sits outside the snapshot.
        map.BindKey(FireAction, WKey);
        Assert.Equal(2, map.BindingCount);

        // A rebind moves Jump off Space.
        Assert.True(map.BeginRebind(JumpAction, UiScheme.Default));
        context.Update(0f);
        context.PushEvent(Key(WKey));
        context.Update(0f);
        Assert.False(map.IsRebinding);
        Assert.Equal(2, map.BindingCount);

        // A restore drops the rebind and the bind that came after the first activation.
        Assert.True(map.ResetBindings());
        Assert.Equal(1, map.BindingCount);

        context.PushEvent(KeyUp(WKey));
        context.PushEvent(Key(SpaceKey));
        context.Update(0f);
        Assert.True(map.WasPressed(JumpAction));
        Assert.False(map.WasPressed(FireAction));

        context.PushEvent(KeyUp(SpaceKey));
        context.PushEvent(Key(WKey));
        context.Update(0f);
        Assert.False(map.WasPressed(JumpAction));
        Assert.False(map.WasPressed(FireAction));

        // A second activation does not move the snapshot, so a bind after the first activation stays outside it.
        context.PushEvent(KeyUp(WKey));
        context.Update(0f);
        map.BindKey(FireAction, AKey);
        Assert.Equal(2, map.BindingCount);
        map.Activate(context);
        Assert.True(map.ResetBindings());
        Assert.Equal(1, map.BindingCount);

        context.PushEvent(Key(SpaceKey));
        context.Update(0f);
        Assert.True(map.WasPressed(JumpAction));
        Assert.False(map.WasPressed(FireAction));

        context.PushEvent(KeyUp(SpaceKey));
        context.PushEvent(Key(AKey));
        context.Update(0f);
        Assert.False(map.WasPressed(JumpAction));
        Assert.False(map.WasPressed(FireAction));
    }

    /// <summary>
    /// A restore changes the bindings and nothing else: the actions, the types, the commands, the scheme table, the
    /// gamepad slot, the scheme lock, the two slots, and the cancel key keep their state.
    /// </summary>
    [Fact]
    public void ResetBindingsRestoresTheBindingsAndNothingElse()
    {
        s_resultCalls = 0;
        s_conflictCalls = 0;
        Counter counter = new();
        UiContext context = CreateContext();
        UiActionMap map = CreateMap(counter);
        int kb = map.AddScheme(UiScheme.KeyboardMouse);
        int gp = map.AddScheme(UiScheme.Gamepad);
        int touch = map.AddScheme(TouchScheme);
        map.AddAction(JumpAction, CountCommand);
        map.AddAction(FireAction, UiActionType.Button);
        map.AddAction(MoveAction, UiActionType.Vector2);
        map.AddAction(ZoomAction, UiActionType.Axis);
        map.BindKey(JumpAction, SpaceKey);
        map.BindKeyVector(MoveAction, WKey, AKey, SKey, DKey);
        map.BindScroll(ZoomAction, 1f);
        map.GamepadSlot = 2;
        map.RebindCancelKey = F1Key;
        map.SetScheme(kb);
        map.OnRebindResult = s_result;
        map.OnRebindConflict = s_conflict;
        int actionCount = map.ActionCount;
        Assert.Equal(4, actionCount);
        Assert.Equal(3, map.BindingCount);
        map.Activate(context);
        context.Update(0f);

        // One rebind adds a fourth binding for Fire.
        Assert.True(map.BeginRebind(FireAction, UiScheme.Default));
        context.Update(0f);
        context.PushEvent(Key(WKey));
        context.Update(0f);
        Assert.False(map.IsRebinding);
        Assert.Equal(4, map.BindingCount);

        Assert.True(map.ResetBindings());

        // The bindings came back, and the rebind of Fire is gone.
        Assert.Equal(3, map.BindingCount);

        // The actions and the action types stayed.
        Assert.Equal(actionCount, map.ActionCount);
        ArgumentException ex = Assert.Throws<ArgumentException>(() => map.BindKey(MoveAction, F1Key));
        Assert.Equal("action", ex.ParamName);
        ex = Assert.Throws<ArgumentException>(() => map.BindScroll(JumpAction, 1f, gp));
        Assert.Equal("action", ex.ParamName);
        Assert.Equal(3, map.BindingCount);

        // Each command stayed.
        Assert.True(map.InvokeAction(JumpAction));
        Assert.Equal(1, counter.Calls);
        Assert.False(map.InvokeAction(FireAction));
        Assert.False(map.InvokeAction(MoveAction));
        Assert.Equal(1, counter.Calls);

        // The scheme table stayed.
        Assert.True(map.SchemeName(UiScheme.Default).IsEmpty);
        Assert.True(map.SchemeName(kb).SequenceEqual(UiScheme.KeyboardMouse));
        Assert.True(map.SchemeName(gp).SequenceEqual(UiScheme.Gamepad));
        Assert.True(map.SchemeName(touch).SequenceEqual(TouchScheme));
        Assert.Throws<ArgumentOutOfRangeException>(() => map.SchemeName(touch + 1));

        // The gamepad slot, the scheme lock, the two slots, and the cancel key stayed.
        Assert.Equal(2, map.GamepadSlot);
        Assert.Equal(kb, map.LockedScheme);
        Assert.Equal(kb, map.ActiveScheme);
        Assert.Same(s_result, map.OnRebindResult);
        Assert.Same(s_conflict, map.OnRebindConflict);
        Assert.Equal(F1Key, map.RebindCancelKey);
        Assert.Equal(0, s_conflictCalls);

        // A later frame read drives the build binding of Jump, and gives nothing for Fire.
        context.PushEvent(KeyUp(WKey));
        context.Update(0f);
        Assert.Equal(0f, map.ReadVector(MoveAction).X, 5);
        Assert.Equal(0f, map.ReadAxis(ZoomAction), 5);

        context.PushEvent(Key(SpaceKey));
        context.Update(0f);
        Assert.True(map.WasPressed(JumpAction));
        Assert.False(map.WasPressed(FireAction));
    }

    /// <summary>
    /// A restore closes an open listen with no commit and no slot call, and a later call opens a fresh settle frame.
    /// </summary>
    [Fact]
    public void ARestoreEndsTheListenWithNoCommit()
    {
        s_resultCalls = 0;
        s_conflictCalls = 0;
        UiContext context = CreateContext();
        UiActionMap map = CreateMap();
        map.AddAction(JumpAction, UiActionType.Button);
        map.AddAction(FireAction, UiActionType.Button);
        map.BindKey(JumpAction, SpaceKey);
        map.OnRebindResult = s_result;
        map.OnRebindConflict = s_conflict;
        map.Activate(context);
        context.Update(0f);

        // The restore closes the listen with no commit and no slot call.
        Assert.True(map.BeginRebind(FireAction, UiScheme.Default));
        Assert.True(map.IsRebinding);
        Assert.True(map.ResetBindings());
        Assert.False(map.IsRebinding);
        Assert.Equal(1, map.BindingCount);
        Assert.Equal(0, s_resultCalls);
        Assert.Equal(0, s_conflictCalls);

        // The candidate of the next frame reaches the frame read, so the build binding drives its action.
        context.PushEvent(Key(SpaceKey));
        context.Update(0f);
        Assert.True(map.WasPressed(JumpAction));
        Assert.Equal(1, map.BindingCount);
        Assert.Equal(0, s_resultCalls);

        // A later call opens a fresh settle frame that reads no input.
        context.PushEvent(KeyUp(SpaceKey));
        Assert.True(map.BeginRebind(FireAction, UiScheme.Default));
        context.PushEvent(Key(WKey));
        context.Update(0f);
        Assert.True(map.IsRebinding);
        Assert.Equal(1, map.BindingCount);
        Assert.Equal(0, s_resultCalls);

        // The next frame commits. No other action holds W, so the conflict slot stays quiet.
        context.PushEvent(KeyUp(WKey));
        context.PushEvent(Key(WKey));
        context.Update(0f);
        Assert.False(map.IsRebinding);
        Assert.Equal(2, map.BindingCount);
        Assert.Equal(1, s_resultCalls);
        Assert.Equal(0, s_conflictCalls);
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
