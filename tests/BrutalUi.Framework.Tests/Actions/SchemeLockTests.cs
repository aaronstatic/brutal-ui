using System;
using Brutal.Numerics;
using BrutalUi.Core;
using BrutalUi.Core.Tests;
using Xunit;

namespace BrutalUi.Framework.Tests;

/// <summary>
/// The tests of the lock of <see cref="UiActionMap"/>. <see cref="UiActionMap.SetScheme(int)"/> locks the map to one
/// scheme, and a binding of another scheme gives no value on the frame read, on the press path, and on the gamepad
/// invoke, including <see cref="UiActionMap.PushGamepad"/>.
/// </summary>
/// <remarks>
/// <para>
/// The filter rule: the lock is the only filter. An unlocked map reads every binding, whatever its tag, so a bind of one
/// device can still report that device. Under a lock, a binding of the default scheme reads, and a binding of the locked
/// scheme reads. <see cref="UiActionMap.ActiveScheme"/> reads the locked id while the lock stands.
/// </para>
/// <para>
/// Each test builds a context with a <see cref="RecordingRenderer"/> measurer and a viewport of 400 by 300, and a map
/// that it activates on that context. A test writes a pad with <see cref="UiContext.SetGamepad(int, in UiGamepadState)"/>
/// before the update. A test of <see cref="UiActionMap.PushGamepad"/> uses a map that is not active, because an active
/// map also reads the pad in the update and would invoke each press a second time. GLFW codes: Escape 256, W 87, A 65,
/// S 83, D 68, Space 32. Each float assert compares one component with a precision of five decimals.
/// </para>
/// </remarks>
public sealed class SchemeLockTests
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

    /// <summary>The GLFW key code of the Space key.</summary>
    private const int SpaceKey = 32;

    /// <summary>The name of the Button action that the tests fire with.</summary>
    private static ReadOnlySpan<byte> FireAction => "Fire"u8;

    /// <summary>The name of the Button action that the tests cancel with.</summary>
    private static ReadOnlySpan<byte> CancelAction => "Cancel"u8;

    /// <summary>The name of the Vector2 action that the tests move with.</summary>
    private static ReadOnlySpan<byte> MoveAction => "Move"u8;

    /// <summary>The name of the command that the tests count.</summary>
    private static ReadOnlySpan<byte> CountCommand => "count"u8;

    /// <summary>
    /// <see cref="UiActionMap.SetScheme(int)"/> rejects a negative id and an id at or above the count of valid ids, and
    /// keeps the lock that the map held. <see cref="UiActionMap.LockedScheme"/> reads the lock, and 0 clears it. A map
    /// that holds no lock reports the device of the frame read.
    /// </summary>
    [Fact]
    public void SetSchemeChecksTheIdAndKeepsTheOldLock()
    {
        UiContext context = CreateContext();
        UiActionMap map = CreateMap();
        int kb = map.AddScheme(UiScheme.KeyboardMouse);
        int gp = map.AddScheme(UiScheme.Gamepad);
        int beyond = map.AddScheme("Third"u8) + 1;
        map.AddAction(MoveAction, UiActionType.Vector2);
        map.BindKeyVector(MoveAction, WKey, AKey, SKey, DKey);
        map.Activate(context);

        map.SetScheme(gp);
        Assert.Equal(gp, map.LockedScheme);

        ArgumentOutOfRangeException negative = Assert.Throws<ArgumentOutOfRangeException>(() => map.SetScheme(-1));
        Assert.Equal("scheme", negative.ParamName);
        Assert.Equal(gp, map.LockedScheme);

        ArgumentOutOfRangeException high = Assert.Throws<ArgumentOutOfRangeException>(() => map.SetScheme(beyond));
        Assert.Equal("scheme", high.ParamName);
        Assert.Equal(gp, map.LockedScheme);

        map.SetScheme(0);
        Assert.Equal(0, map.LockedScheme);

        context.PushEvent(Key(WKey));
        context.Update(0f);
        Assert.Equal(kb, map.ActiveScheme);
    }

    /// <summary>
    /// Locked to Gamepad, the keyboard composite of one action gives no value, and the stick gives its value. Locked to
    /// KeyboardMouse, the two flip.
    /// </summary>
    [Fact]
    public void ALockEmptiesTheForeignFrameRead()
    {
        UiContext context = CreateContext();
        UiActionMap map = CreateMap();
        int kb = map.AddScheme(UiScheme.KeyboardMouse);
        int gp = map.AddScheme(UiScheme.Gamepad);
        map.AddAction(MoveAction, UiActionType.Vector2);
        map.BindKeyVector(MoveAction, WKey, AKey, SKey, DKey, kb);
        map.BindGamepadStick(MoveAction, UiGamepadStick.Left, gp);
        map.Activate(context);

        context.PushEvent(Key(WKey));
        SetLeftStick(context, 0.5f, 0f);

        // Locked to Gamepad: the stick reads, and W gives nothing.
        map.SetScheme(gp);
        context.Update(0f);
        AssertVector(map.ReadVector(MoveAction), 0.5f, 0f);

        SetLeftStick(context, 0f, 0f);
        context.Update(0f);
        AssertVector(map.ReadVector(MoveAction), 0f, 0f);

        // Locked to KeyboardMouse: the composite reads, and the stick gives nothing.
        SetLeftStick(context, 0.5f, 0f);
        map.SetScheme(kb);
        context.Update(0f);
        AssertVector(map.ReadVector(MoveAction), 0f, 1f);
    }

    /// <summary>Under a Gamepad lock, a key binding of KeyboardMouse gives no flag, and a pad binding gives its press.</summary>
    [Fact]
    public void ALockEmptiesTheForeignButtonFlags()
    {
        Counter counter = new();
        UiContext context = CreateContext();
        UiActionMap map = CreateMap(counter);
        int kb = map.AddScheme(UiScheme.KeyboardMouse);
        int gp = map.AddScheme(UiScheme.Gamepad);
        map.AddAction(CancelAction, CountCommand);
        map.BindKey(CancelAction, EscapeKey, UiModifiers.None, kb);
        map.AddAction(FireAction, CountCommand);
        map.BindGamepadButton(FireAction, UiGamepadButtons.B, gp);
        map.Activate(context);

        map.SetScheme(gp);
        context.PushEvent(Key(EscapeKey));
        SetPadButtons(context, UiGamepadButtons.B);
        context.Update(0f);

        Assert.False(map.WasPressed(CancelAction));
        Assert.False(map.IsHeld(CancelAction));
        Assert.False(map.WasReleased(CancelAction));
        Assert.True(map.WasPressed(FireAction));

        // Only the pad press ran a command: the frame read invokes no key command, and the lock emptied the key flags.
        Assert.Equal(1, counter.Calls);

        // A lock that stands over a held foreign input ends that action, and reports one release for it.
        map.SetScheme(0);
        context.PushEvent(Key(EscapeKey));
        SetPadButtons(context, UiGamepadButtons.None);
        context.Update(0f);
        Assert.True(map.IsHeld(CancelAction));

        map.SetScheme(gp);
        context.Update(0f);
        Assert.False(map.IsHeld(CancelAction));
        Assert.True(map.WasReleased(CancelAction));
        Assert.False(map.WasPressed(CancelAction));

        // The latch clears, so the release comes one time and not in each frame under the lock.
        context.Update(0f);
        Assert.False(map.WasReleased(CancelAction));
    }

    /// <summary>
    /// Under a Gamepad lock, <see cref="UiActionMap.PressKey"/> and <see cref="UiActionMap.PressMouseButton"/> take no
    /// binding of KeyboardMouse, and run no command. A binding of the default scheme still matches.
    /// </summary>
    [Fact]
    public void ALockEmptiesThePressPath()
    {
        Counter counter = new();
        UiContext context = CreateContext();
        UiActionMap map = CreateMap(counter);
        int kb = map.AddScheme(UiScheme.KeyboardMouse);
        int gp = map.AddScheme(UiScheme.Gamepad);
        map.AddAction(FireAction, CountCommand);
        map.BindKey(FireAction, SpaceKey, UiModifiers.None, kb);
        map.AddAction(CancelAction, CountCommand);
        map.BindMouseButton(CancelAction, PointerButton.Right, UiModifiers.None, kb);
        map.Activate(context);

        map.SetScheme(gp);
        Assert.False(map.PressKey(SpaceKey, UiModifiers.None));
        Assert.False(map.PressMouseButton(PointerButton.Right, UiModifiers.None));
        Assert.Equal(0, counter.Calls);

        // Unlocked, the same two calls match and run their command.
        map.SetScheme(0);
        Assert.True(map.PressKey(SpaceKey, UiModifiers.None));
        Assert.True(map.PressMouseButton(PointerButton.Right, UiModifiers.None));
        Assert.Equal(2, counter.Calls);

        // A binding of the default scheme reads under the same lock.
        Counter defaultCounter = new();
        UiActionMap defaultMap = CreateMap(defaultCounter);
        defaultMap.AddScheme(UiScheme.Gamepad);
        defaultMap.AddAction(FireAction, CountCommand);
        defaultMap.BindKey(FireAction, SpaceKey);
        defaultMap.SetScheme(defaultMap.AddScheme(UiScheme.Gamepad));
        Assert.True(defaultMap.PressKey(SpaceKey, UiModifiers.None));
        Assert.Equal(1, defaultCounter.Calls);
    }

    /// <summary>
    /// Under a KeyboardMouse lock, a pad press invokes nothing from the frame read and nothing from
    /// <see cref="UiActionMap.PushGamepad"/>. Under a Gamepad lock, each path invokes one time.
    /// </summary>
    [Fact]
    public void ALockEmptiesTheGamepadInvoke()
    {
        Counter counter = new();
        UiContext context = CreateContext();
        UiActionMap activeMap = CreateMap(counter);
        int kb = activeMap.AddScheme(UiScheme.KeyboardMouse);
        int gp = activeMap.AddScheme(UiScheme.Gamepad);
        activeMap.AddAction(FireAction, CountCommand);
        activeMap.BindGamepadButton(FireAction, UiGamepadButtons.A, gp);
        activeMap.Activate(context);

        // The frame read of an active map under a KeyboardMouse lock.
        activeMap.SetScheme(kb);
        SetPadButtons(context, UiGamepadButtons.A);
        context.Update(0f);
        Assert.Equal(0, counter.Calls);

        // The same press under a Gamepad lock invokes one time.
        activeMap.SetScheme(gp);
        SetPadButtons(context, UiGamepadButtons.None);
        context.Update(0f);
        SetPadButtons(context, UiGamepadButtons.A);
        context.Update(0f);
        Assert.Equal(1, counter.Calls);

        // The push path of a map that is not active, under a KeyboardMouse lock.
        UiActionMap pushedMap = CreateMap(counter);
        pushedMap.AddAction(FireAction, CountCommand);
        pushedMap.BindGamepadButton(FireAction, UiGamepadButtons.A, gp);
        pushedMap.SetScheme(kb);
        Assert.Equal(0, pushedMap.PushGamepad(UiGamepadButtons.A));

        // The same push under a Gamepad lock invokes one time.
        UiActionMap pushedPadMap = CreateMap(counter);
        pushedPadMap.AddAction(FireAction, CountCommand);
        pushedPadMap.BindGamepadButton(FireAction, UiGamepadButtons.A, gp);
        pushedPadMap.SetScheme(gp);
        Assert.Equal(1, pushedPadMap.PushGamepad(UiGamepadButtons.A));
    }

    /// <summary>
    /// A binding of the default scheme gives its held flag, its pressed flag, and its command under a KeyboardMouse
    /// lock, under a Gamepad lock, and unlocked.
    /// </summary>
    [Fact]
    public void ADefaultBindingReadsUnderEveryLock()
    {
        for (int testCase = 0; testCase < 3; testCase++)
        {
            Counter counter = new();
            UiContext context = CreateContext();
            UiActionMap map = CreateMap(counter);
            int kb = map.AddScheme(UiScheme.KeyboardMouse);
            int gp = map.AddScheme(UiScheme.Gamepad);
            map.AddAction(FireAction, CountCommand);
            map.BindKey(FireAction, SpaceKey);
            map.Activate(context);

            map.SetScheme(testCase == 0 ? kb : testCase == 1 ? gp : 0);
            Assert.Equal(testCase == 2 ? 0 : testCase == 0 ? kb : gp, map.LockedScheme);

            context.PushEvent(Key(SpaceKey));
            context.Update(0f);

            Assert.True(map.IsHeld(FireAction), $"case {testCase}");
            Assert.True(map.WasPressed(FireAction), $"case {testCase}");
            Assert.Equal(1, counter.Calls);
        }
    }

    /// <summary>
    /// A locked map reports the locked scheme, whatever the frame read reads, and the device again at the update after a
    /// clear of the lock.
    /// </summary>
    [Fact]
    public void ALockReportsTheLockedScheme()
    {
        UiContext context = CreateContext();
        UiActionMap map = CreateMap();
        int kb = map.AddScheme(UiScheme.KeyboardMouse);
        int gp = map.AddScheme(UiScheme.Gamepad);
        map.AddAction(MoveAction, UiActionType.Vector2);
        map.BindKeyVector(MoveAction, WKey, AKey, SKey, DKey);
        map.AddAction(FireAction, UiActionType.Button);
        map.BindGamepadButton(FireAction, UiGamepadButtons.A, gp);
        map.Activate(context);

        // A default bind reads under the lock, so the keyboard value is there, yet the report is the locked id.
        map.SetScheme(gp);
        context.PushEvent(Key(WKey));
        context.Update(0f);
        AssertVector(map.ReadVector(MoveAction), 0f, 1f);
        Assert.Equal(gp, map.ActiveScheme);
        Assert.True("Gamepad"u8.SequenceEqual(map.SchemeName(map.ActiveScheme)));

        // After the clear, the next update reports the device of the frame read again.
        map.SetScheme(0);
        context.Update(0f);
        Assert.Equal(kb, map.ActiveScheme);
    }

    /// <summary>Writes the buttons of slot 0 of a context, with no axis value.</summary>
    /// <param name="context">The context.</param>
    /// <param name="buttons">The buttons that are down.</param>
    private static void SetPadButtons(UiContext context, UiGamepadButtons buttons)
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

    /// <summary>The target of the counter command.</summary>
    private sealed class Counter
    {
        /// <summary>The count of calls.</summary>
        public int Calls;
    }
}
