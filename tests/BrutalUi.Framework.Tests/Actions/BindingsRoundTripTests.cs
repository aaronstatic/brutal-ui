using System;
using Brutal.Numerics;
using BrutalUi.Core;
using BrutalUi.Core.Tests;
using Xunit;

namespace BrutalUi.Framework.Tests;

/// <summary>
/// The tests of <see cref="UiActionMap.ReadBindings(ReadOnlySpan{byte})"/> and of the round trip with
/// <see cref="UiActionMap.WriteBindings(Span{byte})"/>: all nine shapes, the merge per action and scheme pair, the bad
/// line that the read skips and counts, and the version that the read does not know.
/// </summary>
/// <remarks>
/// <para>
/// The grammar: one version line <c>version=1</c>, then one line per binding. A line holds its fields in the order
/// <c>action</c>, <c>scheme</c>, <c>device</c>, <c>code</c>, <c>code1</c>, <c>code2</c>, <c>code3</c>, <c>mods</c>, and
/// <c>scale</c>, one field after a semicolon, and one name and value after an equals sign. A field appears only when its
/// value is not its default. The <c>device</c> number is the <c>InputKind</c> number: 0 a key, 1 a mouse button, 2 a
/// gamepad button, 3 a key vector, 4 a gamepad stick, 5 a gamepad axis, 6 a key axis, 7 a pointer delta, and 8 a scroll.
/// </para>
/// <para>
/// No public member reads one binding (A10), so each test proves a read shape by driving a frame and reading the value
/// through the five queries, the drain path, or the command of the frame read invoke pass. The handle and its index read
/// are internal, so every assert here reaches the feature through the public API.
/// </para>
/// <para>
/// Each test builds its maps on a new <see cref="CommandRegistry"/>. A map that the text reads into holds the same action
/// names, the same action types, and the same scheme names, and no bind. GLFW codes: Space 32, W 87, A 65, S 83, D 68,
/// F1 290, Control 341.
/// </para>
/// </remarks>
public sealed class BindingsRoundTripTests
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

    /// <summary>The name of the Button action that the key bind drives.</summary>
    private static ReadOnlySpan<byte> FireAction => "Fire"u8;

    /// <summary>The name of the Button action that the modifier bind drives.</summary>
    private static ReadOnlySpan<byte> JumpAction => "Jump"u8;

    /// <summary>The name of the Button action that the mouse button bind drives.</summary>
    private static ReadOnlySpan<byte> KickAction => "Kick"u8;

    /// <summary>The name of the Button action with a command that a pad bind drives.</summary>
    private static ReadOnlySpan<byte> PadAction => "Pad"u8;

    /// <summary>The name of the Vector2 action that the key vector bind drives.</summary>
    private static ReadOnlySpan<byte> MoveAction => "Move"u8;

    /// <summary>The name of the Vector2 action that the stick bind drives.</summary>
    private static ReadOnlySpan<byte> TurnAction => "Turn"u8;

    /// <summary>The name of the Vector2 action that the pointer delta bind drives.</summary>
    private static ReadOnlySpan<byte> LookAction => "Look"u8;

    /// <summary>The name of the Axis action that the key axis bind drives.</summary>
    private static ReadOnlySpan<byte> ZoomAction => "Zoom"u8;

    /// <summary>The name of the Axis action that the trigger bind drives.</summary>
    private static ReadOnlySpan<byte> AimAction => "Aim"u8;

    /// <summary>The name of the Axis action that the scroll bind drives.</summary>
    private static ReadOnlySpan<byte> PanAction => "Pan"u8;

    /// <summary>The name of a Button action that holds the field separator: the name-rule case.</summary>
    private static ReadOnlySpan<byte> SplitActionName => "attack;heavy"u8;

    /// <summary>The name of the command that counts its calls.</summary>
    private static ReadOnlySpan<byte> CountCommand => "count"u8;

    /// <summary>The handler that adds one to the counter target.</summary>
    private static readonly CommandHandler s_count = static target => ((Counter)target!).Calls++;

    /// <summary>The six axis values of a pad at rest.</summary>
    private static readonly float[] PadIdle = [0f, 0f, 0f, 0f, 0f, 0f];

    /// <summary>
    /// One map with all nine shapes, one modifier bind, and one scale of -1 writes its text, and a second map with the
    /// same names reads it back. The read gives every binding, skips nothing, names version 1, and its rewrite equals
    /// the bytes that it read. A driven frame proves each shape.
    /// </summary>
    [Fact]
    public void AWrittenFileReadsBackIntoEveryKind()
    {
        Counter counter = new();
        UiActionMap written = CreateMap(counter);
        int kb = written.AddScheme(UiScheme.KeyboardMouse);
        int gp = written.AddScheme(UiScheme.Gamepad);
        AddActions(written);
        written.BindKey(FireAction, SpaceKey, UiModifiers.None, UiScheme.Default);
        written.BindKey(JumpAction, F1Key, UiModifiers.Control, kb);
        written.BindMouseButton(KickAction, PointerButton.Right, UiModifiers.None, kb);
        written.BindGamepadButton(PadAction, UiGamepadButtons.A, gp);
        written.BindKeyVector(MoveAction, WKey, AKey, SKey, DKey, kb);
        written.BindGamepadStick(TurnAction, UiGamepadStick.Left, gp);
        written.BindGamepadAxis(AimAction, UiGamepadAxis.LeftTrigger, 1f, gp);
        written.BindKeyAxis(ZoomAction, AKey, DKey, kb);
        written.BindMouseDelta(LookAction, 1f, UiScheme.Default);
        written.BindScroll(PanAction, -1f, gp);

        byte[] text = WriteAll(written);
        Assert.Equal(10, written.BindingCount);

        UiContext context = CreateContext();
        UiActionMap read = CreateMap(counter);
        AddActions(read);
        read.AddScheme(UiScheme.KeyboardMouse);
        read.AddScheme(UiScheme.Gamepad);

        (int Applied, int Skipped, int Version) result = read.ReadBindings(text);
        Assert.Equal(written.BindingCount, result.Applied);
        Assert.Equal(0, result.Skipped);
        Assert.Equal(1, result.Version);
        Assert.Equal(written.BindingCount, read.BindingCount);

        byte[] again = new byte[read.WriteBindings([])];
        Assert.Equal(text.Length, read.WriteBindings(again));
        Assert.True(text.SequenceEqual(again));

        read.Activate(context);
        context.Update(0f);

        // The key bind of the default scheme, with no mods field.
        context.PushEvent(Key(SpaceKey));
        context.Update(0f);
        Assert.True(read.WasPressed(FireAction));
        context.PushEvent(KeyUp(SpaceKey));
        context.Update(0f);
        Assert.False(read.IsHeld(FireAction));

        // The mouse button bind: a press over no element drives the action.
        context.PushEvent(Pointer(UiEventType.PointerMove, Outside));
        context.Update(0f);
        context.PushEvent(Pointer(UiEventType.PointerDown, Outside, PointerButton.Right));
        context.Update(0f);
        Assert.True(read.WasPressed(KickAction));
        context.PushEvent(Pointer(UiEventType.PointerUp, Outside, PointerButton.Right));
        context.Update(0f);
        Assert.False(read.IsHeld(KickAction));

        // The gamepad button bind: the frame read invoke pass runs its command once.
        int calls = counter.Calls;
        SetPad(context, UiGamepadButtons.A, PadIdle);
        context.Update(0f);
        Assert.True(read.WasPressed(PadAction));
        Assert.Equal(calls + 1, counter.Calls);
        SetPad(context, UiGamepadButtons.None, PadIdle);
        context.Update(0f);
        Assert.True(read.WasReleased(PadAction));

        // The same bind through the drain path: one push of that bit runs one command.
        Assert.Equal(1, read.PushGamepad(UiGamepadButtons.A));
        Assert.Equal(calls + 2, counter.Calls);
        _ = read.PushGamepad(UiGamepadButtons.None);

        // The key vector bind: the up key gives (0, 1).
        context.PushEvent(Key(WKey));
        context.Update(0f);
        AssertVector(read.ReadVector(MoveAction), 0f, 1f);
        context.PushEvent(KeyUp(WKey));
        context.Update(0f);
        AssertVector(read.ReadVector(MoveAction), 0f, 0f);

        // The stick bind: the left stick, with the Y flip of the frame read.
        SetPad(context, UiGamepadButtons.None, Axes(0.4f, -0.3f, 0f, 0f, 0f, 0f));
        context.Update(0f);
        AssertVector(read.ReadVector(TurnAction), 0.4f, 0.3f);
        SetPad(context, UiGamepadButtons.None, PadIdle);
        context.Update(0f);

        // The gamepad axis bind: the left trigger at the scale of 1.
        SetPad(context, UiGamepadButtons.None, Axes(0f, 0f, 0f, 0f, 0.25f, 0f));
        context.Update(0f);
        Assert.Equal(0.25f, read.ReadAxis(AimAction), 5);
        SetPad(context, UiGamepadButtons.None, PadIdle);
        context.Update(0f);

        // The key axis bind: the positive key gives 1, and the negative key gives -1.
        context.PushEvent(Key(DKey));
        context.Update(0f);
        Assert.Equal(1f, read.ReadAxis(ZoomAction), 5);
        context.PushEvent(KeyUp(DKey));
        context.PushEvent(Key(AKey));
        context.Update(0f);
        Assert.Equal(-1f, read.ReadAxis(ZoomAction), 5);
        context.PushEvent(KeyUp(AKey));
        context.Update(0f);

        // The pointer delta bind at the scale of 1: the delta of the frame.
        context.PushEvent(Pointer(UiEventType.PointerMove, Outside));
        context.Update(0f);
        context.PushEvent(Pointer(UiEventType.PointerMove, new float2(Outside.X + 30f, Outside.Y + 20f)));
        context.Update(0f);
        AssertVector(read.ReadVector(LookAction), 30f, 20f);

        // The scroll bind at the scale of -1: the sign comes back from the text.
        context.PushEvent(Scroll(Outside, new float2(0f, 3f)));
        context.Update(0f);
        Assert.Equal(-3f, read.ReadAxis(PanAction), 5);

        // The modifier bind: no modifier gives nothing, and Control down gives the bind.
        context.PushEvent(Key(F1Key));
        context.Update(0f);
        Assert.False(read.WasPressed(JumpAction));
        context.PushEvent(KeyUp(F1Key));
        context.Update(0f);
        context.PushEvent(Key(ControlKey, UiModifiers.Control));
        context.Update(0f);
        context.PushEvent(Key(F1Key, UiModifiers.Control));
        context.Update(0f);
        Assert.True(read.WasPressed(JumpAction));
    }

    /// <summary>
    /// The read replaces each pair that its lines name and keeps each pair that no line names. Two lines of one pair
    /// give two bindings, a re-read of the same text is idempotent, and <see cref="UiActionMap.ResetBindings"/> after a
    /// read gives the bindings of the build.
    /// </summary>
    [Fact]
    public void ReadBindingsMergesTheNamedGroupsAndKeepsTheRest()
    {
        UiContext context = CreateContext();
        UiActionMap map = CreateMap();
        int kb = map.AddScheme(UiScheme.KeyboardMouse);
        int gp = map.AddScheme(UiScheme.Gamepad);
        map.AddAction(FireAction, UiActionType.Button);
        map.AddAction(ZoomAction, UiActionType.Axis);
        map.AddAction(LookAction, UiActionType.Vector2);
        map.AddAction(MoveAction, UiActionType.Vector2);
        map.BindKey(FireAction, SpaceKey, UiModifiers.None, kb);
        map.BindGamepadStick(LookAction, UiGamepadStick.Left, gp);
        map.BindKeyVector(MoveAction, WKey, AKey, SKey, DKey, gp);
        map.Activate(context);
        context.Update(0f);
        int build = map.BindingCount;
        Assert.Equal(3, build);

        (int Applied, int Skipped, int Version) result = map.ReadBindings(MergeText);
        Assert.Equal(3, result.Applied);
        Assert.Equal(0, result.Skipped);
        Assert.Equal(1, result.Version);
        Assert.Equal(build + 2, map.BindingCount);

        // The named pair of Fire in KeyboardMouse now holds the two read lines, and the build line is gone.
        context.PushEvent(Key(SpaceKey));
        context.Update(0f);
        Assert.False(map.WasPressed(FireAction));
        context.PushEvent(KeyUp(SpaceKey));
        context.PushEvent(Key(WKey));
        context.Update(0f);
        Assert.True(map.WasPressed(FireAction));
        context.PushEvent(KeyUp(WKey));
        context.Update(0f);

        // Each unnamed pair still drives its action: the stick on Look and the four keys on Move.
        SetPad(context, UiGamepadButtons.None, Axes(0.5f, 0f, 0f, 0f, 0f, 0f));
        context.Update(0f);
        AssertVector(map.ReadVector(LookAction), 0.5f, 0f);
        SetPad(context, UiGamepadButtons.None, PadIdle);
        context.Update(0f);
        context.PushEvent(Key(SKey));
        context.Update(0f);
        AssertVector(map.ReadVector(MoveAction), 0f, -1f);
        context.PushEvent(KeyUp(SKey));
        context.Update(0f);

        // A re-read of the same text is idempotent.
        (int Applied, int Skipped, int Version) again = map.ReadBindings(MergeText);
        Assert.Equal(3, again.Applied);
        Assert.Equal(0, again.Skipped);
        Assert.Equal(1, again.Version);
        Assert.Equal(build + 2, map.BindingCount);

        // The read did not move the snapshot of the first Activate (A6).
        Assert.True(map.ResetBindings());
        Assert.Equal(build, map.BindingCount);
        context.PushEvent(Key(WKey));
        context.Update(0f);
        Assert.False(map.WasPressed(FireAction));
        context.PushEvent(KeyUp(WKey));
        context.PushEvent(Key(SpaceKey));
        context.Update(0f);
        Assert.True(map.WasPressed(FireAction));
    }

    /// <summary>
    /// One text with fifteen bad lines and three good lines: the read counts the bad lines, applies the good lines, and
    /// throws nothing. Each surviving bind still drives its action in a driven frame. One gamepad line holds a bit above
    /// the 15-button cap, so the read skips it. One last block reads a name that holds the field separator, so it
    /// proves the name rule from the public side.
    /// </summary>
    /// <remarks>
    /// The name case: the separator guard belongs to <c>AddAction</c>, and the Definition-of-Done check I2 of this
    /// feature freezes <c>UiActionMap.Bindings.cs</c>, so this pass ships no throw there. The rule ships as a named
    /// contract of the text format, not as a throw. A first pass that may open that file adds the guard.
    /// </remarks>
    [Fact]
    public void ABadLineIsSkippedAndCounted()
    {
        UiContext context = CreateContext();
        UiActionMap map = CreateMap();
        map.AddScheme(UiScheme.KeyboardMouse);
        int gp = map.AddScheme(UiScheme.Gamepad);
        map.AddAction(FireAction, UiActionType.Button);
        map.AddAction(MoveAction, UiActionType.Vector2);
        map.AddAction(ZoomAction, UiActionType.Axis);

        (int Applied, int Skipped, int Version) result = map.ReadBindings(BadText);
        Assert.Equal(3, result.Applied);
        Assert.Equal(15, result.Skipped);
        Assert.Equal(1, result.Version);
        Assert.Equal(3, map.BindingCount);

        map.Activate(context);
        context.Update(0f);

        // The read key line drives Fire.
        context.PushEvent(Key(WKey));
        context.Update(0f);
        Assert.True(map.WasPressed(FireAction));
        context.PushEvent(KeyUp(WKey));
        context.Update(0f);

        // The read axis line drives Zoom through the trigger.
        SetPad(context, UiGamepadButtons.None, Axes(0f, 0f, 0f, 0f, 0.5f, 0f));
        context.Update(0f);
        Assert.Equal(0.5f, map.ReadAxis(ZoomAction), 5);
        SetPad(context, UiGamepadButtons.None, PadIdle);
        context.Update(0f);

        // The read key vector line drives Move.
        context.PushEvent(Key(SKey));
        context.Update(0f);
        AssertVector(map.ReadVector(MoveAction), 0f, -1f);

        // The name rule: a written name that holds the field separator gives a line that reads back bad.
        UiActionMap named = CreateMap();
        named.AddAction(SplitActionName, UiActionType.Button);
        named.BindKey(SplitActionName, WKey);
        byte[] namedText = WriteAll(named);

        UiActionMap reader = CreateMap();
        reader.AddAction(SplitActionName, UiActionType.Button);
        (int Applied, int Skipped, int Version) nameRule = reader.ReadBindings(namedText);
        Assert.Equal(0, nameRule.Applied);
        Assert.Equal(1, nameRule.Skipped);
        Assert.Equal(1, nameRule.Version);
        Assert.Equal(0, reader.BindingCount);
    }

    /// <summary>
    /// A version that the reader does not know applies nothing and reports its number. A missing or unreadable version,
    /// and a version line that is not the first non-empty line, give version 0 and change no binding.
    /// </summary>
    [Fact]
    public void AnUnknownVersionAppliesNothing()
    {
        UiActionMap map = CreateMap();
        int kb = map.AddScheme(UiScheme.KeyboardMouse);
        map.AddAction(FireAction, UiActionType.Button);
        map.AddAction(ZoomAction, UiActionType.Axis);
        map.BindKey(FireAction, SpaceKey, UiModifiers.None, kb);
        int build = map.BindingCount;

        // A known version with unknown lines still reports nothing applied.
        (int Applied, int Skipped, int Version) future = map.ReadBindings(FutureText);
        Assert.Equal(0, future.Applied);
        Assert.Equal(0, future.Skipped);
        Assert.Equal(2, future.Version);
        Assert.Equal(build, map.BindingCount);

        // A text with no version line gives version 0.
        (int Applied, int Skipped, int Version) missing = map.ReadBindings(NoVersionText);
        Assert.Equal(0, missing.Applied);
        Assert.Equal(0, missing.Version);
        Assert.Equal(build, map.BindingCount);

        // A version line that is not the first non-empty line gives version 0.
        (int Applied, int Skipped, int Version) late = map.ReadBindings(LateVersionText);
        Assert.Equal(0, late.Applied);
        Assert.Equal(0, late.Version);
        Assert.Equal(build, map.BindingCount);

        // An unreadable version number gives version 0.
        (int Applied, int Skipped, int Version) unreadable = map.ReadBindings(UnreadableText);
        Assert.Equal(0, unreadable.Applied);
        Assert.Equal(0, unreadable.Version);
        Assert.Equal(build, map.BindingCount);

        // A text of one line feed alone gives (0, 0, 0), and so does an empty text.
        Assert.Equal((0, 0, 0), map.ReadBindings("\n"u8));
        Assert.Equal((0, 0, 0), map.ReadBindings([]));
        Assert.Equal(build, map.BindingCount);

        // The version line with a carriage return before the line feed is version 1.
        (int Applied, int Skipped, int Version) returnLine = map.ReadBindings(ReturnText);
        Assert.Equal(0, returnLine.Applied);
        Assert.Equal(0, returnLine.Skipped);
        Assert.Equal(1, returnLine.Version);
        Assert.Equal(build, map.BindingCount);

        // No read above changed a binding, so the build bind still drives its action.
        UiContext context = CreateContext();
        map.Activate(context);
        context.Update(0f);
        context.PushEvent(Key(SpaceKey));
        context.Update(0f);
        Assert.True(map.WasPressed(FireAction));
    }

    /// <summary>The merge text: two lines of the Fire pair, and one line of a new Zoom pair.</summary>
    private static ReadOnlySpan<byte> MergeText => """
        version=1
        action=Fire;scheme=KeyboardMouse;device=0;code=87
        action=Fire;scheme=KeyboardMouse;device=1;code=1
        action=Zoom;scheme=Gamepad;device=5;code=4
        """u8;

    /// <summary>The bad text: fifteen bad lines and three good lines.</summary>
    private static ReadOnlySpan<byte> BadText => """
        version=1
        action=Ghost;scheme=-;device=0;code=32
        action=Fire;scheme=-;device=0;code=32;bogus=1
        action=Fire;scheme=-;device=0;code=32;code=33
        scheme=-;device=0;code=32
        action=Fire;scheme=-;device=0;code=abc
        action=Fire;scheme=-;device=0;code=99999
        action=Fire;scheme=-;device=9
        action=Fire;scheme=-;device=-2
        action=Fire;scheme=-;device=0;code=32;mods=64
        action=Zoom;scheme=-;device=8;scale=NaN
        action=Fire;scheme=Joystick;device=0;code=32
        action=Fire;scheme=Gamepad;device=2;code=3
        action=Fire;scheme=Gamepad;device=2;code=65536
        action=Move;scheme=KeyboardMouse;device=3;code=87;code1=87;code2=83;code3=68
        action=Zoom;scheme=KeyboardMouse;device=6;code=65;code1=65
        action=Fire;scheme=KeyboardMouse;device=0;code=87
        action=Zoom;scheme=Gamepad;device=5;code=4
        action=Move;scheme=KeyboardMouse;device=3;code=87;code1=65;code2=83;code3=68
        """u8;

    /// <summary>A version line of 2 with two valid binding lines.</summary>
    private static ReadOnlySpan<byte> FutureText => """
        version=2
        action=Fire;scheme=KeyboardMouse;device=0;code=87
        action=Zoom;scheme=KeyboardMouse;device=6;code=65;code1=68
        """u8;

    /// <summary>One valid binding line with no version line.</summary>
    private static ReadOnlySpan<byte> NoVersionText => "action=Fire;scheme=KeyboardMouse;device=0;code=87\n"u8;

    /// <summary>A version line after one binding line.</summary>
    private static ReadOnlySpan<byte> LateVersionText => "action=Fire;scheme=KeyboardMouse;device=0;code=87\nversion=1\n"u8;

    /// <summary>A version line whose number is not readable.</summary>
    private static ReadOnlySpan<byte> UnreadableText => "version=abc\n"u8;

    /// <summary>The version line and an empty list, with a carriage return before the line feed.</summary>
    private static ReadOnlySpan<byte> ReturnText => "version=1\r\n"u8;

    /// <summary>
    /// Adds the ten actions of the round trip map: four Button actions, three Vector2 actions, and three Axis actions.
    /// The <c>Pad</c> action holds the command that the counter counts.
    /// </summary>
    /// <param name="map">The map to fill.</param>
    private static void AddActions(UiActionMap map)
    {
        map.AddAction(FireAction, UiActionType.Button);
        map.AddAction(JumpAction, UiActionType.Button);
        map.AddAction(KickAction, UiActionType.Button);
        map.AddAction(PadAction, CountCommand);
        map.AddAction(MoveAction, UiActionType.Vector2);
        map.AddAction(TurnAction, UiActionType.Vector2);
        map.AddAction(LookAction, UiActionType.Vector2);
        map.AddAction(ZoomAction, UiActionType.Axis);
        map.AddAction(AimAction, UiActionType.Axis);
        map.AddAction(PanAction, UiActionType.Axis);
    }

    /// <summary>Measures the text of one map, writes it into a buffer of that size, and gives the bytes.</summary>
    /// <param name="map">The map to write.</param>
    /// <returns>The buffer that holds the whole text.</returns>
    private static byte[] WriteAll(UiActionMap map)
    {
        int needed = map.WriteBindings([]);
        byte[] buffer = new byte[needed];
        Assert.Equal(needed, map.WriteBindings(buffer));
        return buffer;
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
    private static PointerEvent Pointer(
        UiEventType type,
        float2 position,
        PointerButton button = PointerButton.Left,
        UiModifiers modifiers = UiModifiers.None)
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

    /// <summary>A target that counts the calls of its command.</summary>
    private sealed class Counter
    {
        /// <summary>The count of calls.</summary>
        public int Calls;
    }
}
