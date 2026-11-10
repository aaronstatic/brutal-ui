using System;
using BrutalUi.Core;
using Xunit;

namespace BrutalUi.Framework.Tests;

/// <summary>
/// The tests of the scheme group of <see cref="UiActionMap"/>: the name table, the intern, the id checks, the scheme
/// tag on a binding, the nine scheme overloads, and the duplicate check inside one scheme.
/// </summary>
/// <remarks>
/// <para>
/// A scheme is one <see cref="int"/> id that resolves to a UTF-8 name. Id 0 is the default scheme: it holds no name,
/// and a binding with it belongs to each scheme. The constructor seeds <see cref="UiScheme.KeyboardMouse"/> and
/// <see cref="UiScheme.Gamepad"/>, so a new map resolves both names with no add.
/// </para>
/// <para>
/// The duplicate rule: the map reads a second bind of one input as a duplicate inside one scheme. Two tags that differ
/// are two binds, so one key can bind two actions in two schemes. The default scheme clashes with every scheme, in
/// either order. A composite compares its codes as a set inside one scheme.
/// </para>
/// <para>
/// Each test builds one map on a new <see cref="CommandRegistry"/> and changes no frame value: the group is a
/// build-time rule. GLFW codes: Space 32, W 87, A 65, S 83, D 68.
/// </para>
/// </remarks>
public sealed class SchemeGroupTests
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

    /// <summary>The name of the Button action that the tests fire with.</summary>
    private static ReadOnlySpan<byte> FireAction => "Fire"u8;

    /// <summary>The name of the Button action that the tests jump with.</summary>
    private static ReadOnlySpan<byte> JumpAction => "Jump"u8;

    /// <summary>The name of the Button action that the tests duck with.</summary>
    private static ReadOnlySpan<byte> DuckAction => "Duck"u8;

    /// <summary>The name of the Vector2 action that the tests move with.</summary>
    private static ReadOnlySpan<byte> MoveAction => "Move"u8;

    /// <summary>The name of the Vector2 action that the tests look with.</summary>
    private static ReadOnlySpan<byte> LookAction => "Look"u8;

    /// <summary>The name of the Vector2 action that the tests turn with.</summary>
    private static ReadOnlySpan<byte> TurnAction => "Turn"u8;

    /// <summary>The name of the Axis action that the tests zoom with.</summary>
    private static ReadOnlySpan<byte> ZoomAction => "Zoom"u8;

    /// <summary>The name of the Axis action that the tests throttle with.</summary>
    private static ReadOnlySpan<byte> ThrottleAction => "Throttle"u8;

    /// <summary>The name of the Axis action that the tests aim with.</summary>
    private static ReadOnlySpan<byte> AimAction => "Aim"u8;

    /// <summary>The name of the command that the Button add takes.</summary>
    private static ReadOnlySpan<byte> CountCommand => "count"u8;

    /// <summary>
    /// <see cref="UiActionMap.AddScheme(ReadOnlySpan{byte})"/> gives one id per name, and
    /// <see cref="UiActionMap.SchemeName(int)"/> gives the bytes back.
    /// </summary>
    [Fact]
    public void AddSchemeGivesOneIdPerName()
    {
        UiActionMap map = CreateMap();

        int keyboard = map.AddScheme(UiScheme.KeyboardMouse);
        Assert.Equal(keyboard, map.AddScheme(UiScheme.KeyboardMouse));

        int gamepad = map.AddScheme(UiScheme.Gamepad);
        Assert.NotEqual(keyboard, gamepad);
        Assert.Equal(gamepad, map.AddScheme("Gamepad"u8));

        int lower = map.AddScheme("gamepad"u8);
        Assert.NotEqual(gamepad, lower);
        Assert.NotEqual(keyboard, lower);

        Assert.True(map.SchemeName(keyboard).SequenceEqual(UiScheme.KeyboardMouse));
        Assert.True(map.SchemeName(gamepad).SequenceEqual(UiScheme.Gamepad));
        Assert.True(map.SchemeName(lower).SequenceEqual("gamepad"u8));
    }

    /// <summary>
    /// A new map holds the two standard names, a custom name appends after them, and id 0 gives an empty name.
    /// </summary>
    [Fact]
    public void TheStandardNamesStartInTheTable()
    {
        UiActionMap map = CreateMap();

        int keyboard = map.AddScheme(UiScheme.KeyboardMouse);
        int gamepad = map.AddScheme(UiScheme.Gamepad);
        Assert.NotEqual(0, keyboard);
        Assert.NotEqual(0, gamepad);
        Assert.True(map.SchemeName(keyboard).SequenceEqual(UiScheme.KeyboardMouse));
        Assert.True(map.SchemeName(gamepad).SequenceEqual(UiScheme.Gamepad));

        int custom = map.AddScheme("FlightStick"u8);
        Assert.True(custom > keyboard);
        Assert.True(custom > gamepad);
        Assert.True(map.SchemeName(custom).SequenceEqual("FlightStick"u8));

        Assert.True(map.SchemeName(0).IsEmpty);
    }

    /// <summary>
    /// <see cref="UiActionMap.SchemeName(int)"/> rejects a negative id and an id above the table, and it keeps the
    /// parameter name <c>scheme</c>.
    /// </summary>
    [Fact]
    public void SchemeNameChecksTheId()
    {
        UiActionMap map = CreateMap();
        int keyboard = map.AddScheme(UiScheme.KeyboardMouse);
        int custom = map.AddScheme("FlightStick"u8);

        ArgumentOutOfRangeException negative = Assert.Throws<ArgumentOutOfRangeException>(() => map.SchemeName(-1));
        Assert.Equal("scheme", negative.ParamName);

        ArgumentOutOfRangeException above = Assert.Throws<ArgumentOutOfRangeException>(() => map.SchemeName(custom + 1));
        Assert.Equal("scheme", above.ParamName);

        Assert.True(map.SchemeName(0).IsEmpty);
        Assert.True(map.SchemeName(keyboard).SequenceEqual(UiScheme.KeyboardMouse));
        Assert.True(map.SchemeName(custom).SequenceEqual("FlightStick"u8));
    }

    /// <summary>
    /// <see cref="UiActionMap.AddScheme(ReadOnlySpan{byte})"/> rejects an empty name with the parameter name
    /// <c>name</c>, and a rejected call changes no count.
    /// </summary>
    [Fact]
    public void AddSchemeRejectsAnEmptyName()
    {
        UiActionMap map = CreateMap();
        map.AddAction(FireAction, UiActionType.Button);
        map.BindKey(FireAction, SpaceKey);
        int actions = map.ActionCount;
        int bindings = map.BindingCount;

        ArgumentException empty = Assert.Throws<ArgumentException>(() => map.AddScheme(default));
        Assert.Equal("name", empty.ParamName);

        ArgumentException span = Assert.Throws<ArgumentException>(() => map.AddScheme(ReadOnlySpan<byte>.Empty));
        Assert.Equal("name", span.ParamName);

        Assert.Equal(actions, map.ActionCount);
        Assert.Equal(bindings, map.BindingCount);
    }

    /// <summary>
    /// One key binds two actions in two schemes, and the same key twice in one scheme is a duplicate.
    /// </summary>
    [Fact]
    public void ASecondBindOfTheSameKeyInTwoSchemesBinds()
    {
        UiActionMap map = CreateMap();
        map.AddAction(FireAction, UiActionType.Button);
        map.AddAction(JumpAction, UiActionType.Button);
        int keyboard = map.AddScheme(UiScheme.KeyboardMouse);
        int gamepad = map.AddScheme(UiScheme.Gamepad);

        map.BindKey(FireAction, SpaceKey, UiModifiers.None, keyboard);
        int bindings = map.BindingCount;

        map.BindKey(JumpAction, SpaceKey, UiModifiers.None, gamepad);
        Assert.Equal(bindings + 1, map.BindingCount);

        ArgumentException duplicate = Assert.Throws<ArgumentException>(
            () => map.BindKey(JumpAction, SpaceKey, UiModifiers.None, keyboard));
        Assert.Equal("action", duplicate.ParamName);
        Assert.Equal(bindings + 1, map.BindingCount);
    }

    /// <summary>
    /// A bind of the default scheme clashes with a tagged bind of the same input, in either order.
    /// </summary>
    [Fact]
    public void TheDefaultSchemeClashesWithEveryScheme()
    {
        UiActionMap defaultFirst = CreateMap();
        defaultFirst.AddAction(FireAction, UiActionType.Button);
        defaultFirst.AddAction(JumpAction, UiActionType.Button);
        int gamepad = defaultFirst.AddScheme(UiScheme.Gamepad);
        defaultFirst.BindKey(FireAction, SpaceKey);
        int bindings = defaultFirst.BindingCount;

        ArgumentException tagged = Assert.Throws<ArgumentException>(
            () => defaultFirst.BindKey(JumpAction, SpaceKey, UiModifiers.None, gamepad));
        Assert.Equal("action", tagged.ParamName);
        Assert.Equal(bindings, defaultFirst.BindingCount);

        UiActionMap taggedFirst = CreateMap();
        taggedFirst.AddAction(FireAction, UiActionType.Button);
        taggedFirst.AddAction(JumpAction, UiActionType.Button);
        int keyboard = taggedFirst.AddScheme(UiScheme.KeyboardMouse);
        taggedFirst.BindKey(FireAction, SpaceKey, UiModifiers.None, keyboard);
        bindings = taggedFirst.BindingCount;

        ArgumentException plain = Assert.Throws<ArgumentException>(() => taggedFirst.BindKey(JumpAction, SpaceKey));
        Assert.Equal("action", plain.ParamName);
        Assert.Equal(bindings, taggedFirst.BindingCount);
    }

    /// <summary>
    /// The duplicate check of a composite is per scheme, and it keeps the set compare inside one scheme.
    /// </summary>
    [Fact]
    public void ACompositeDuplicateIsPerScheme()
    {
        UiActionMap map = CreateMap();
        map.AddAction(MoveAction, UiActionType.Vector2);
        map.AddAction(ThrottleAction, UiActionType.Axis);
        int keyboard = map.AddScheme(UiScheme.KeyboardMouse);
        int gamepad = map.AddScheme(UiScheme.Gamepad);

        map.BindKeyVector(MoveAction, WKey, AKey, SKey, DKey, keyboard);
        int bindings = map.BindingCount;

        ArgumentException sameSet = Assert.Throws<ArgumentException>(
            () => map.BindKeyVector(MoveAction, WKey, DKey, SKey, AKey, keyboard));
        Assert.Equal("action", sameSet.ParamName);
        Assert.Equal(bindings, map.BindingCount);

        map.BindKeyVector(MoveAction, WKey, AKey, SKey, DKey, gamepad);
        Assert.Equal(bindings + 1, map.BindingCount);
        bindings = map.BindingCount;

        map.BindKeyAxis(ThrottleAction, SKey, WKey, keyboard);
        bindings = map.BindingCount;
        ArgumentException samePair = Assert.Throws<ArgumentException>(
            () => map.BindKeyAxis(ThrottleAction, WKey, SKey, keyboard));
        Assert.Equal("action", samePair.ParamName);
        Assert.Equal(bindings, map.BindingCount);

        map.BindKeyAxis(ThrottleAction, WKey, SKey, gamepad);
        Assert.Equal(bindings + 1, map.BindingCount);

        map.BindGamepadStick(MoveAction, UiGamepadStick.Left, keyboard);
        bindings = map.BindingCount;
        map.BindGamepadStick(MoveAction, UiGamepadStick.Left, gamepad);
        Assert.Equal(bindings + 1, map.BindingCount);
    }

    /// <summary>
    /// Each of the eleven current signatures compiles and binds into the default scheme, and the scheme checks throw
    /// with the parameter name <c>scheme</c>.
    /// </summary>
    [Fact]
    public void TheOldSignaturesBindIntoTheDefaultScheme()
    {
        UiActionMap map = CreateMap();
        map.AddAction(FireAction, CountCommand);
        map.AddAction(JumpAction, UiActionType.Button);
        map.AddAction(DuckAction, UiActionType.Button);
        map.AddAction(MoveAction, UiActionType.Vector2);
        map.AddAction(LookAction, UiActionType.Vector2);
        map.AddAction(TurnAction, UiActionType.Vector2);
        map.AddAction(ZoomAction, UiActionType.Axis);
        map.AddAction(ThrottleAction, UiActionType.Axis);
        map.AddAction(AimAction, UiActionType.Axis);

        map.BindKey(FireAction, SpaceKey);
        map.BindMouseButton(JumpAction, PointerButton.Right);
        map.BindGamepadButton(DuckAction, UiGamepadButtons.A);
        map.BindKeyVector(MoveAction, WKey, AKey, SKey, DKey);
        map.BindKeyAxis(ThrottleAction, SKey, WKey);
        map.BindGamepadStick(TurnAction, UiGamepadStick.Left);
        map.BindGamepadAxis(AimAction, UiGamepadAxis.LeftStickX);
        map.BindMouseDelta(LookAction);
        map.BindScroll(ZoomAction);

        Assert.Equal(9, map.ActionCount);
        Assert.Equal(9, map.BindingCount);

        int gamepad = map.AddScheme(UiScheme.Gamepad);
        ArgumentException clash = Assert.Throws<ArgumentException>(
            () => map.BindKey(JumpAction, SpaceKey, UiModifiers.None, gamepad));
        Assert.Equal("action", clash.ParamName);
        Assert.Equal(9, map.BindingCount);

        UiActionMap taggedFirst = CreateMap();
        taggedFirst.AddAction(FireAction, UiActionType.Button);
        taggedFirst.AddAction(JumpAction, UiActionType.Button);
        int keyboard = taggedFirst.AddScheme(UiScheme.KeyboardMouse);
        taggedFirst.BindGamepadButton(FireAction, UiGamepadButtons.A, keyboard);
        ArgumentException plainClash = Assert.Throws<ArgumentException>(
            () => taggedFirst.BindGamepadButton(JumpAction, UiGamepadButtons.A));
        Assert.Equal("action", plainClash.ParamName);
        Assert.Equal(1, taggedFirst.BindingCount);

        int actions = map.ActionCount;
        int bindings = map.BindingCount;
        ArgumentOutOfRangeException negative = Assert.Throws<ArgumentOutOfRangeException>(
            () => map.BindKey(FireAction, WKey, UiModifiers.None, -1));
        Assert.Equal("scheme", negative.ParamName);

        ArgumentOutOfRangeException above = Assert.Throws<ArgumentOutOfRangeException>(
            () => map.BindScroll(ZoomAction, 1f, 99));
        Assert.Equal("scheme", above.ParamName);

        ArgumentOutOfRangeException vector = Assert.Throws<ArgumentOutOfRangeException>(
            () => map.BindKeyVector(MoveAction, WKey, AKey, SKey, DKey, 99));
        Assert.Equal("scheme", vector.ParamName);

        ArgumentException wrongType = Assert.Throws<ArgumentException>(
            () => map.BindKey(MoveAction, WKey, UiModifiers.None, gamepad));
        Assert.Equal("action", wrongType.ParamName);

        Assert.Equal(actions, map.ActionCount);
        Assert.Equal(bindings, map.BindingCount);
    }

    /// <summary>Creates a map on a new registry with no command.</summary>
    /// <returns>The map with no action.</returns>
    private static UiActionMap CreateMap()
    {
        return new UiActionMap(new UiActionMap.CreateInfo { Commands = new CommandRegistry() });
    }
}
