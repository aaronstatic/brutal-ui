using System;
using Brutal.Numerics;
using BrutalUi.Core;
using BrutalUi.Core.Tests;
using Xunit;

namespace BrutalUi.Framework.Tests;

/// <summary>
/// The tests of the typed <see cref="UiActionMap"/>: the action type, the four-key Vector2 composite, the frame read
/// through the slot <see cref="UiContext.OnInputRead"/>, <see cref="UiActionMap.ReadVector"/>, the type checks, and an
/// action with no command.
/// </summary>
/// <remarks>
/// <para>
/// The demo of the phase: the action <c>Move</c> has the type <see cref="UiActionType.Vector2"/> and binds the W, A, S,
/// and D keys. The map is active on a context, so <see cref="UiContext.Update(float)"/> reads the keys through the slot,
/// and <see cref="UiActionMap.ReadVector"/> gives (0, 1) for a held W.
/// </para>
/// <para>
/// Each test builds a context with a <see cref="RecordingRenderer"/> measurer and a viewport of 400 by 300, and a
/// pickable root. The tests build the input events by hand, because the event factories of the Core test project are
/// internal. Each float assert compares one component with a precision of five decimals.
/// </para>
/// </remarks>
public sealed class TypedActionMapTests
{
    /// <summary>The GLFW key code of the A key.</summary>
    private const int AKey = 65;

    /// <summary>The GLFW key code of the S key.</summary>
    private const int SKey = 83;

    /// <summary>The GLFW key code of the W key.</summary>
    private const int WKey = 87;

    /// <summary>The GLFW key code of the D key.</summary>
    private const int DKey = 68;

    /// <summary>The GLFW key code of the Space key.</summary>
    private const int SpaceKey = 32;

    /// <summary>The GLFW key code of the Escape key.</summary>
    private const int EscapeKey = 256;

    /// <summary>The last key code that a bind takes: <see cref="UiKeyState.KeyCount"/>.</summary>
    private const int AfterLastKey = 349;

    /// <summary>The name of the Vector2 action that the tests move with.</summary>
    private static ReadOnlySpan<byte> MoveAction => "Move"u8;

    /// <summary>The name of a Button action that holds no command.</summary>
    private static ReadOnlySpan<byte> FireAction => "Fire"u8;

    /// <summary>The parameter name of each key of a key vector, by position.</summary>
    private static readonly string[] s_keyNames = ["up", "left", "down", "right"];

    /// <summary>The count of calls of the unknown-command handler.</summary>
    private static int s_unknownCalls;

    /// <summary>The handler that adds one to <see cref="s_unknownCalls"/>.</summary>
    private static readonly UnknownCommandHandler s_countUnknown = static _ => s_unknownCalls++;

    /// <summary>
    /// The demo: a held W on the W, A, S, D composite gives (0, 1). <see cref="UiActionMap.Activate"/> writes the read
    /// slot, and one <see cref="UiContext.Update(float)"/> call reads the key.
    /// </summary>
    [Fact]
    public void WOnAWasdVectorReadsZeroOne()
    {
        Scene scene = CreateScene();

        Assert.NotNull(scene.Context.OnInputRead);

        scene.Context.PushEvent(Key(WKey));
        scene.Context.Update(0f);

        AssertVector(scene.Map.ReadVector(MoveAction), 0f, 1f);
    }

    /// <summary>
    /// Each key of the composite gives its own direction. Two keys that are not opposite sum and clamp to a length of 1.
    /// </summary>
    [Fact]
    public void EachKeyOfTheCompositeGivesItsDirection()
    {
        Scene scene = CreateScene();
        UiContext context = scene.Context;
        UiActionMap map = scene.Map;

        context.PushEvent(Key(AKey));
        context.Update(0f);
        AssertVector(map.ReadVector(MoveAction), -1f, 0f);

        context.PushEvent(KeyUp(AKey));
        context.PushEvent(Key(SKey));
        context.Update(0f);
        AssertVector(map.ReadVector(MoveAction), 0f, -1f);

        context.PushEvent(KeyUp(SKey));
        context.PushEvent(Key(DKey));
        context.Update(0f);
        AssertVector(map.ReadVector(MoveAction), 1f, 0f);

        context.PushEvent(Key(WKey));
        context.Update(0f);
        AssertVector(map.ReadVector(MoveAction), 0.707107f, 0.707107f);

        context.PushEvent(KeyUp(DKey));
        context.PushEvent(Key(SKey));
        context.Update(0f);
        AssertVector(map.ReadVector(MoveAction), 0f, 0f);

        context.PushEvent(Key(AKey));
        context.Update(0f);
        AssertVector(map.ReadVector(MoveAction), -1f, 0f);
    }

    /// <summary>
    /// The value of the last frame read stays fixed until the next <see cref="UiContext.Update(float)"/> call. A key up
    /// ends the value.
    /// </summary>
    [Fact]
    public void AKeyUpEndsTheValue()
    {
        Scene scene = CreateScene();
        UiContext context = scene.Context;
        UiActionMap map = scene.Map;

        context.PushEvent(Key(WKey));
        context.Update(0f);
        AssertVector(map.ReadVector(MoveAction), 0f, 1f);

        context.Update(0f);
        AssertVector(map.ReadVector(MoveAction), 0f, 1f);

        context.PushEvent(KeyUp(WKey));
        context.Update(0f);
        AssertVector(map.ReadVector(MoveAction), 0f, 0f);
    }

    /// <summary>
    /// The routing gate empties the keys of <see cref="UiContext.GameInput"/> while a focus owner of the UI holds the
    /// focus, so the composite reads (0, 0). With no focus owner the composite reads Shift+W, because it needs no
    /// modifier.
    /// </summary>
    [Fact]
    public void TheGateEmptiesTheVector()
    {
        UiContext context = CreateContext();
        Element root = AddRoot(context);
        Button button = new(new Button.CreateInfo { Text = "Go" });
        root.Children.Add(button);
        context.Update(0f);
        Assert.True(context.SetFocus(button));

        UiActionMap map = CreateMap();
        map.AddAction(MoveAction, UiActionType.Vector2);
        map.BindKeyVector(MoveAction, WKey, AKey, SKey, DKey);
        map.Activate(context);

        context.PushEvent(Key(WKey));
        context.Update(0f);
        AssertVector(map.ReadVector(MoveAction), 0f, 0f);

        context.ClearFocus();
        context.PushEvent(Key(WKey, UiModifiers.Shift));
        context.Update(0f);
        AssertVector(map.ReadVector(MoveAction), 0f, 1f);
    }

    /// <summary>
    /// Each bind needs an action of its own type, the typed add needs a defined type, and the map holds one action of
    /// each name. A rejected call changes no count.
    /// </summary>
    [Fact]
    public void TheBuildChecksTheActionType()
    {
        UiActionMap map = CreateMap();
        map.AddAction(MoveAction, UiActionType.Vector2);
        map.AddAction(UiActions.Cancel, "menu.close"u8);
        int actionCount = map.ActionCount;
        int bindingCount = map.BindingCount;

        ArgumentException keyError = Assert.Throws<ArgumentException>(() => map.BindKey(MoveAction, EscapeKey));
        Assert.Equal("action", keyError.ParamName);

        ArgumentException mouseError = Assert.Throws<ArgumentException>(
            () => map.BindMouseButton(MoveAction, PointerButton.Left));
        Assert.Equal("action", mouseError.ParamName);

        ArgumentException padError = Assert.Throws<ArgumentException>(
            () => map.BindGamepadButton(MoveAction, UiGamepadButtons.A));
        Assert.Equal("action", padError.ParamName);

        ArgumentException vectorError = Assert.Throws<ArgumentException>(
            () => map.BindKeyVector(UiActions.Cancel, WKey, AKey, SKey, DKey));
        Assert.Equal("action", vectorError.ParamName);

        ArgumentOutOfRangeException keyRange = Assert.Throws<ArgumentOutOfRangeException>(
            () => map.BindKey(UiActions.Cancel, AfterLastKey));
        Assert.Equal("key", keyRange.ParamName);

        ArgumentOutOfRangeException buttonRange = Assert.Throws<ArgumentOutOfRangeException>(
            () => map.BindMouseButton(UiActions.Cancel, (PointerButton)UiPointerButtonState.ButtonCount));
        Assert.Equal("button", buttonRange.ParamName);

        ArgumentOutOfRangeException typeError = Assert.Throws<ArgumentOutOfRangeException>(
            () => map.AddAction("Bad"u8, (UiActionType)3));
        Assert.Equal("type", typeError.ParamName);

        Assert.Throws<ArgumentException>(() => map.AddAction(MoveAction, UiActionType.Vector2));

        Assert.Equal(actionCount, map.ActionCount);
        Assert.Equal(bindingCount, map.BindingCount);
    }

    /// <summary>
    /// <see cref="UiActionMap.BindKeyVector"/> rejects a key outside the key range with the parameter name of that key,
    /// two keys with the same code with the parameter name of the later key, and four keys that the map binds already.
    /// </summary>
    [Fact]
    public void BindKeyVectorChecksTheKeys()
    {
        UiActionMap map = CreateMap();
        map.AddAction(MoveAction, UiActionType.Vector2);

        AssertKeyThrows(map, 0, -1);
        AssertKeyThrows(map, 1, -1);
        AssertKeyThrows(map, 2, -1);
        AssertKeyThrows(map, 3, -1);
        AssertKeyThrows(map, 0, AfterLastKey);
        AssertKeyThrows(map, 1, AfterLastKey);
        AssertKeyThrows(map, 2, AfterLastKey);
        AssertKeyThrows(map, 3, AfterLastKey);

        ArgumentException sameError = Assert.Throws<ArgumentException>(
            () => map.BindKeyVector(MoveAction, WKey, AKey, WKey, DKey));
        Assert.Equal("down", sameError.ParamName);

        map.BindKeyVector(MoveAction, WKey, AKey, SKey, DKey);
        Assert.Throws<ArgumentException>(() => map.BindKeyVector(MoveAction, WKey, AKey, SKey, DKey));
        ArgumentException permutedError = Assert.Throws<ArgumentException>(
            () => map.BindKeyVector(MoveAction, DKey, SKey, AKey, WKey));
        Assert.Equal("action", permutedError.ParamName);
        ArgumentException rotatedError = Assert.Throws<ArgumentException>(
            () => map.BindKeyVector(MoveAction, AKey, SKey, DKey, WKey));
        Assert.Equal("action", rotatedError.ParamName);

        Assert.Equal(1, map.ActionCount);
        Assert.Equal(1, map.BindingCount);
    }

    /// <summary>
    /// <see cref="UiActionMap.ReadVector"/> reads a Vector2 action only. A name that the map does not hold gives (0, 0).
    /// </summary>
    [Fact]
    public void ReadVectorChecksTheType()
    {
        UiActionMap map = CreateMap();
        map.AddAction(UiActions.Cancel, "menu.close"u8);

        ArgumentException error = Assert.Throws<ArgumentException>(() => map.ReadVector(UiActions.Cancel));
        Assert.Equal("action", error.ParamName);

        AssertVector(map.ReadVector("Nope"u8), 0f, 0f);
        AssertVector(map.ReadVector(MoveAction), 0f, 0f);
    }

    /// <summary>
    /// <see cref="UiActionMap.Activate"/> writes the read slot, a move to a second context clears the slot of the first
    /// context, and <see cref="UiActionMap.Deactivate"/> clears its own slot and leaves the slot of another map.
    /// </summary>
    [Fact]
    public void ActivateWritesTheReadSlotAndDeactivateClearsIt()
    {
        UiContext first = CreateContext();
        UiContext second = CreateContext();
        UiActionMap map = CreateMap();
        UiActionMap other = CreateMap();

        Assert.Null(first.OnInputRead);

        map.Activate(first);
        Assert.NotNull(first.OnInputRead);

        map.Activate(second);
        Assert.Null(first.OnInputRead);
        Assert.NotNull(second.OnInputRead);

        other.Activate(second);
        map.Deactivate();
        Assert.NotNull(second.OnInputRead);

        other.Deactivate();
        Assert.Null(second.OnInputRead);
    }

    /// <summary>
    /// An action with no command invokes nothing: the resolve gives <see langword="false"/>, and the registry calls no
    /// unknown-command handler.
    /// </summary>
    [Fact]
    public void ACommandlessButtonInvokesNothing()
    {
        s_unknownCalls = 0;
        CommandRegistry registry = new();
        registry.UnknownCommand = s_countUnknown;
        UiActionMap map = new(new UiActionMap.CreateInfo { Commands = registry });
        map.AddAction(FireAction, UiActionType.Button);
        map.BindKey(FireAction, SpaceKey);

        Assert.False(map.PressKey(SpaceKey, UiModifiers.None));
        Assert.False(map.InvokeAction(FireAction));
        Assert.Equal(0, s_unknownCalls);
    }

    /// <summary>Asserts one key position of a vector composite that is outside the key range.</summary>
    /// <param name="map">The map with a Vector2 action named <c>Move</c>.</param>
    /// <param name="position">The position of the key: 0 up, 1 left, 2 down, 3 right.</param>
    /// <param name="badKey">The key code outside the range.</param>
    private static void AssertKeyThrows(UiActionMap map, int position, int badKey)
    {
        ArgumentOutOfRangeException error = position switch
        {
            0 => Assert.Throws<ArgumentOutOfRangeException>(() => map.BindKeyVector(MoveAction, badKey, AKey, SKey, DKey)),
            1 => Assert.Throws<ArgumentOutOfRangeException>(() => map.BindKeyVector(MoveAction, WKey, badKey, SKey, DKey)),
            2 => Assert.Throws<ArgumentOutOfRangeException>(() => map.BindKeyVector(MoveAction, WKey, AKey, badKey, DKey)),
            _ => Assert.Throws<ArgumentOutOfRangeException>(() => map.BindKeyVector(MoveAction, WKey, AKey, SKey, badKey)),
        };

        Assert.Equal(s_keyNames[position], error.ParamName);
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

    /// <summary>Creates a context with an active map that holds the <c>Move</c> composite.</summary>
    /// <returns>The context, the root, and the map.</returns>
    private static Scene CreateScene()
    {
        UiContext context = CreateContext();
        Element root = AddRoot(context);
        UiActionMap map = CreateMap();
        map.AddAction(MoveAction, UiActionType.Vector2);
        map.BindKeyVector(MoveAction, WKey, AKey, SKey, DKey);
        map.Activate(context);
        return new Scene { Context = context, Root = root, Map = map };
    }

    /// <summary>Creates a map on a new empty registry.</summary>
    /// <returns>The map with no action.</returns>
    private static UiActionMap CreateMap()
    {
        return new UiActionMap(new UiActionMap.CreateInfo { Commands = new CommandRegistry() });
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

    /// <summary>A context with an active map and a pickable root.</summary>
    private sealed class Scene
    {
        /// <summary>The context.</summary>
        public required UiContext Context;

        /// <summary>The root.</summary>
        public required Element Root;

        /// <summary>The map with the <c>Move</c> Vector2 action.</summary>
        public required UiActionMap Map;
    }
}
