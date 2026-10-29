using System;
using Brutal.Numerics;
using BrutalUi.Core;
using BrutalUi.Core.Tests;
using Xunit;

namespace BrutalUi.Framework.Tests;

/// <summary>
/// The tests of <see cref="UiActionMap"/> and <see cref="UiActions"/>: the key path through the slot of the context,
/// the raw resolve, the modifier rule, the repeat rule, the unknown command, the one-map rule, and the mouse path.
/// </summary>
/// <remarks>
/// <para>
/// Each test builds a registry with one command that adds one to a <see cref="Counter"/> target. The handler is a
/// static lambda in a <see langword="static"/> <see langword="readonly"/> field, as a game uses it. The tests that run
/// <see cref="UiContext.Update(float)"/> build a context with a <see cref="RecordingRenderer"/> measurer, a viewport
/// of 400 by 300, and a pickable root with no focus owner. The mouse tests make the root not pickable, so a press
/// outside each child hits no element.
/// </para>
/// <para>
/// The tests build the input events by hand, because the event factories of the Core test project are internal.
/// </para>
/// </remarks>
public sealed class UiActionMapTests
{
    /// <summary>The GLFW key code of the Escape key.</summary>
    private const int EscapeKey = 256;

    /// <summary>The GLFW key code of the Enter key.</summary>
    private const int EnterKey = 257;

    /// <summary>The GLFW key code of the S key.</summary>
    private const int SKey = 83;

    /// <summary>The GLFW key code of the W key.</summary>
    private const int WKey = 87;

    /// <summary>The GLFW key code of the Down key.</summary>
    private const int DownKey = 264;

    /// <summary>The GLFW key code of the F1 key.</summary>
    private const int F1Key = 290;

    /// <summary>The GLFW key code of the F2 key.</summary>
    private const int F2Key = 291;

    /// <summary>The handler that adds one to the counter target.</summary>
    private static readonly CommandHandler s_count = static target => ((Counter)target!).Calls++;

    /// <summary>The handler that binds F2 to <c>Confirm</c> on the map of its <see cref="Binder"/> target.</summary>
    private static readonly CommandHandler s_bindLater = static target =>
    {
        Binder binder = (Binder)target!;
        binder.Calls++;
        if (binder.Calls == 1)
        {
            binder.Map.BindKey(UiActions.Confirm, F2Key);
        }
    };

    /// <summary>
    /// The handler that binds the gamepad button B to <c>Confirm</c> on the map of its <see cref="Binder"/> target.
    /// </summary>
    private static readonly CommandHandler s_bindPadLater = static target =>
    {
        Binder binder = (Binder)target!;
        binder.Calls++;
        if (binder.Calls == 1)
        {
            binder.Map.BindGamepadButton(UiActions.Confirm, UiGamepadButtons.B);
        }
    };

    /// <summary>The handler that throws a <see cref="BoomException"/>.</summary>
    private static readonly CommandHandler s_boom = static _ => throw new BoomException();

    /// <summary>
    /// The demo of phase 1. The action <c>Cancel</c> has a command and a bind of Escape. The map is active. A key down
    /// of Escape with no target and no focus owner reaches the map through the slot, and one update runs the command
    /// one time.
    /// </summary>
    [Fact]
    public void AnUnhandledKeyInvokesTheCommandOfItsAction()
    {
        UiContext context = CreateContext();
        AddRoot(context);
        Counter counter = new();
        UiActionMap map = CreateMap(counter, "menu.close"u8);
        map.AddAction(UiActions.Cancel, "menu.close"u8);
        map.BindKey(UiActions.Cancel, EscapeKey);

        map.Activate(context);

        Assert.NotNull(context.OnUnhandledInput);
        Assert.Equal(1, map.ActionCount);
        Assert.Equal(1, map.BindingCount);

        context.PushEvent(Key(EscapeKey));
        context.Update(0f);

        Assert.Null(context.FocusedElement);
        Assert.Equal(0, context.QueuedEventCount);
        Assert.Equal(1, counter.Calls);
    }

    /// <summary>A bound key gives <see langword="true"/> and runs the command. An unbound key gives <see langword="false"/>.</summary>
    [Fact]
    public void PressKeyReturnsTrueForABoundKeyAndFalseForAnUnboundKey()
    {
        Counter counter = new();
        UiActionMap map = CreateMap(counter, "menu.close"u8);
        map.AddAction(UiActions.Cancel, "menu.close"u8);
        map.BindKey(UiActions.Cancel, EscapeKey);

        Assert.True(map.PressKey(EscapeKey, UiModifiers.None));
        Assert.Equal(1, counter.Calls);

        Assert.False(map.PressKey(EnterKey, UiModifiers.None));
        Assert.Equal(1, counter.Calls);
    }

    /// <summary>A bind of Control+S matches Control+S only: S alone and Control+Shift+S do not match.</summary>
    [Fact]
    public void TheModifiersMustMatch()
    {
        Counter counter = new();
        UiActionMap map = CreateMap(counter, "file.save"u8);
        map.AddAction(UiActions.Confirm, "file.save"u8);
        map.BindKey(UiActions.Confirm, SKey, UiModifiers.Control);

        Assert.False(map.PressKey(SKey, UiModifiers.None));
        Assert.True(map.PressKey(SKey, UiModifiers.Control));
        Assert.False(map.PressKey(SKey, UiModifiers.Control | UiModifiers.Shift));
        Assert.Equal(1, counter.Calls);
    }

    /// <summary>
    /// Caps Lock and Num Lock do not count at a press. A bind also drops them, so a bind with Caps Lock matches the
    /// same key with no lock.
    /// </summary>
    [Fact]
    public void CapsLockAndNumLockDoNotBlockAMatch()
    {
        Counter counter = new();
        UiActionMap map = CreateMap(counter, "menu.close"u8);
        map.AddAction(UiActions.Cancel, "menu.close"u8);
        map.BindKey(UiActions.Cancel, EscapeKey);
        map.BindKey(UiActions.Cancel, SKey, UiModifiers.Control | UiModifiers.CapsLock);

        Assert.True(map.PressKey(EscapeKey, UiModifiers.CapsLock));
        Assert.True(map.PressKey(EscapeKey, UiModifiers.NumLock));
        Assert.True(map.PressKey(EscapeKey, UiModifiers.CapsLock | UiModifiers.NumLock));
        Assert.True(map.PressKey(SKey, UiModifiers.Control));
        Assert.True(map.PressKey(SKey, UiModifiers.Control | UiModifiers.NumLock));
        Assert.False(map.PressKey(EscapeKey, UiModifiers.CapsLock | UiModifiers.Shift));
        Assert.Equal(5, counter.Calls);
    }

    /// <summary>
    /// An invoke of an action runs its command and gives <see langword="true"/>. An unknown action gives
    /// <see langword="false"/>, runs nothing, and does not call the unknown command slot of the registry.
    /// </summary>
    [Fact]
    public void InvokeActionInvokesTheCommandOfTheAction()
    {
        Counter counter = new();
        UiActionMap map = CreateMap(counter, "menu.close"u8);
        int unknownCalls = 0;
        map.Commands.UnknownCommand = _ => unknownCalls++;
        map.AddAction(UiActions.Cancel, "menu.close"u8);

        Assert.True(map.InvokeAction(UiActions.Cancel));
        Assert.Equal(1, counter.Calls);

        Assert.False(map.InvokeAction(UiActions.Confirm));
        Assert.False(map.InvokeAction("cancel"u8));
        Assert.Equal(1, counter.Calls);
        Assert.Equal(0, unknownCalls);
    }

    /// <summary>
    /// An action with a command that the registry does not hold gives <see langword="false"/>. The registry gives the
    /// command name to its unknown command slot.
    /// </summary>
    [Fact]
    public void AnActionWithAnUnknownCommandCallsTheUnknownCommandSlot()
    {
        Counter counter = new();
        UiActionMap map = CreateMap(counter, "menu.close"u8);
        int unknownCalls = 0;
        byte[]? unknownName = null;
        map.Commands.UnknownCommand = name =>
        {
            unknownCalls++;
            unknownName = name.ToArray();
        };
        map.AddAction(UiActions.Cancel, "menu.missing"u8);
        map.BindKey(UiActions.Cancel, EscapeKey);

        Assert.False(map.PressKey(EscapeKey, UiModifiers.None));

        Assert.Equal(1, unknownCalls);
        Assert.Equal("menu.missing"u8.ToArray(), unknownName);
        Assert.Equal(0, counter.Calls);
    }

    /// <summary>
    /// A key repeat through the slot invokes nothing. The next key down with no repeat invokes the command, so the path
    /// works.
    /// </summary>
    [Fact]
    public void ARepeatInvokesNothing()
    {
        UiContext context = CreateContext();
        AddRoot(context);
        Counter counter = new();
        UiActionMap map = CreateMap(counter, "menu.close"u8);
        map.AddAction(UiActions.Cancel, "menu.close"u8);
        map.BindKey(UiActions.Cancel, EscapeKey);
        map.Activate(context);

        context.PushEvent(Key(EscapeKey, UiModifiers.None, isRepeat: true));
        context.Update(0f);

        Assert.Equal(0, counter.Calls);

        context.PushEvent(Key(EscapeKey));
        context.Update(0f);

        Assert.Equal(1, counter.Calls);
    }

    /// <summary>
    /// A map with no <see cref="UiActionMap.Activate"/> leaves the slot empty, so a key down through the context
    /// invokes nothing. A direct press still works.
    /// </summary>
    [Fact]
    public void AMapWithNoActivateInvokesNothing()
    {
        UiContext context = CreateContext();
        AddRoot(context);
        Counter counter = new();
        UiActionMap map = CreateMap(counter, "menu.close"u8);
        map.AddAction(UiActions.Cancel, "menu.close"u8);
        map.BindKey(UiActions.Cancel, EscapeKey);

        context.PushEvent(Key(EscapeKey));
        context.Update(0f);

        Assert.Null(context.OnUnhandledInput);
        Assert.Equal(0, context.QueuedEventCount);
        Assert.Equal(0, counter.Calls);

        Assert.True(map.PressKey(EscapeKey, UiModifiers.None));
        Assert.Equal(1, counter.Calls);
    }

    /// <summary>
    /// Two maps on one context bind Escape to two counters. After the second
    /// <see cref="UiActionMap.Activate"/>, an Escape adds one to the second counter only.
    /// </summary>
    [Fact]
    public void ASecondActivateReplacesTheFirstMap()
    {
        UiContext context = CreateContext();
        AddRoot(context);
        Counter first = new();
        Counter second = new();
        UiActionMap firstMap = CreateMap(first, "first.close"u8);
        firstMap.AddAction(UiActions.Cancel, "first.close"u8);
        firstMap.BindKey(UiActions.Cancel, EscapeKey);
        UiActionMap secondMap = CreateMap(second, "second.close"u8);
        secondMap.AddAction(UiActions.Cancel, "second.close"u8);
        secondMap.BindKey(UiActions.Cancel, EscapeKey);

        firstMap.Activate(context);
        context.PushEvent(Key(EscapeKey));
        context.Update(0f);

        Assert.Equal(1, first.Calls);
        Assert.Equal(0, second.Calls);

        secondMap.Activate(context);
        context.PushEvent(Key(EscapeKey));
        context.Update(0f);

        Assert.Equal(1, first.Calls);
        Assert.Equal(1, second.Calls);

        // One map on two contexts: the last Activate wins and the slot of the first context is clear.
        TextScene scene = CreateTextScene();
        UiContext other = CreateContext();
        AddRoot(other);
        Counter moved = new();
        UiActionMap movedMap = CreateMap(moved, "moved.close"u8);
        movedMap.AddAction(UiActions.Cancel, "moved.close"u8);
        movedMap.BindKey(UiActions.Cancel, WKey);

        movedMap.Activate(scene.Context);
        movedMap.Activate(other);

        Assert.Null(scene.Context.OnUnhandledInput);
        Assert.NotNull(other.OnUnhandledInput);
        Assert.True(movedMap.IsActive);

        Assert.True(scene.Context.SetFocus(scene.Field));
        scene.Context.PushEvent(Key(WKey));
        scene.Context.PushEvent(Char('w'));
        scene.Context.Update(0f);
        Assert.Equal(0, moved.Calls);

        movedMap.Activate(other);
        Assert.True(movedMap.IsActive);
        Assert.NotNull(other.OnUnhandledInput);

        movedMap.Deactivate();

        Assert.Null(other.OnUnhandledInput);
        Assert.Null(scene.Context.OnUnhandledInput);
        Assert.False(movedMap.IsActive);
    }

    /// <summary>
    /// <see cref="UiActionMap.Deactivate"/> of a map that another map replaced leaves the other map active. The
    /// <see cref="UiActionMap.Deactivate"/> of the active map then clears the slot.
    /// </summary>
    [Fact]
    public void DeactivateClearsTheSlotOfItsOwnMapOnly()
    {
        UiContext context = CreateContext();
        AddRoot(context);
        Counter first = new();
        Counter second = new();
        UiActionMap firstMap = CreateMap(first, "first.close"u8);
        firstMap.AddAction(UiActions.Cancel, "first.close"u8);
        firstMap.BindKey(UiActions.Cancel, EscapeKey);
        UiActionMap secondMap = CreateMap(second, "second.close"u8);
        secondMap.AddAction(UiActions.Cancel, "second.close"u8);
        secondMap.BindKey(UiActions.Cancel, EscapeKey);
        firstMap.Activate(context);
        secondMap.Activate(context);

        firstMap.Deactivate();

        Assert.NotNull(context.OnUnhandledInput);
        Assert.True(secondMap.IsActive);
        context.PushEvent(Key(EscapeKey));
        context.Update(0f);
        Assert.Equal(0, first.Calls);
        Assert.Equal(1, second.Calls);

        secondMap.Deactivate();

        Assert.Null(context.OnUnhandledInput);
        context.PushEvent(Key(EscapeKey));
        context.Update(0f);
        Assert.Equal(0, first.Calls);
        Assert.Equal(1, second.Calls);
    }

    /// <summary>
    /// <see cref="UiActionMap.IsActive"/> is <see langword="false"/> before an activate, <see langword="true"/> after
    /// it, <see langword="false"/> for a replaced map, and <see langword="false"/> after a deactivate.
    /// </summary>
    [Fact]
    public void IsActiveFollowsTheSlot()
    {
        UiContext context = CreateContext();
        UiActionMap firstMap = CreateMap(new Counter(), "first.close"u8);
        UiActionMap secondMap = CreateMap(new Counter(), "second.close"u8);

        Assert.False(firstMap.IsActive);

        firstMap.Activate(context);

        Assert.True(firstMap.IsActive);
        Assert.False(secondMap.IsActive);

        secondMap.Activate(context);

        Assert.False(firstMap.IsActive);
        Assert.True(secondMap.IsActive);

        secondMap.Deactivate();

        Assert.False(secondMap.IsActive);
        Assert.Null(context.OnUnhandledInput);

        firstMap.Deactivate();

        Assert.False(firstMap.IsActive);
        Assert.Null(context.OnUnhandledInput);
    }

    /// <summary>
    /// The demo of phase 4. The root is not pickable and holds a panel of 100 by 100. A right press and a release at
    /// (300, 200) hit no element, so the press reaches the map through the slot. One update runs the command of
    /// <c>Cancel</c> one time. The release invokes nothing.
    /// </summary>
    [Fact]
    public void AMouseButtonOverNoElementInvokesItsAction()
    {
        UiContext context = CreateContext();
        Element root = AddRoot(context);
        root.IsPickable = false;
        root.Children.Add(CreatePanel());
        Counter counter = new();
        UiActionMap map = CreateMap(counter, "menu.close"u8);
        map.AddAction(UiActions.Cancel, "menu.close"u8);
        map.BindMouseButton(UiActions.Cancel, PointerButton.Right);
        map.Activate(context);
        context.Update(0f);

        PointerEvent press = Pointer(UiEventType.PointerDown, new float2(300f, 200f), PointerButton.Right);
        PointerEvent release = Pointer(UiEventType.PointerUp, new float2(300f, 200f), PointerButton.Right);
        context.PushEvent(in press);
        context.PushEvent(in release);
        context.Update(0f);

        Assert.Equal(1, counter.Calls);
        Assert.Equal(1, map.BindingCount);
        Assert.False(context.RoutingGate.PointerOverUi);
        Assert.Equal(0, context.QueuedEventCount);
    }

    /// <summary>
    /// A hit takes a press. The same right press inside the panel invokes nothing. A pickable root takes a press
    /// outside the panel too (the layout-root rule).
    /// </summary>
    [Fact]
    public void AMouseButtonOverAnElementInvokesNothing()
    {
        UiContext context = CreateContext();
        Element root = AddRoot(context);
        root.IsPickable = false;
        root.Children.Add(CreatePanel());
        Counter counter = new();
        UiActionMap map = CreateMap(counter, "menu.close"u8);
        map.AddAction(UiActions.Cancel, "menu.close"u8);
        map.BindMouseButton(UiActions.Cancel, PointerButton.Right);
        map.Activate(context);
        context.Update(0f);

        PointerEvent press = Pointer(UiEventType.PointerDown, new float2(50f, 50f), PointerButton.Right);
        PointerEvent release = Pointer(UiEventType.PointerUp, new float2(50f, 50f), PointerButton.Right);
        context.PushEvent(in press);
        context.PushEvent(in release);
        context.Update(0f);

        Assert.Equal(0, counter.Calls);
        Assert.True(context.RoutingGate.PointerOverUi);

        root.IsPickable = true;
        PointerEvent rootPress = Pointer(UiEventType.PointerDown, new float2(300f, 200f), PointerButton.Right);
        PointerEvent rootRelease = Pointer(UiEventType.PointerUp, new float2(300f, 200f), PointerButton.Right);
        context.PushEvent(in rootPress);
        context.PushEvent(in rootRelease);
        context.Update(0f);

        Assert.Equal(0, counter.Calls);
        Assert.True(context.RoutingGate.PointerOverUi);
    }

    /// <summary>
    /// A bind of Control and the right button matches the right button with Control only. The left button, no
    /// modifier, and Control+Shift do not match. Caps Lock does not block a match.
    /// </summary>
    [Fact]
    public void PressMouseButtonMatchesTheButtonAndTheModifiers()
    {
        Counter counter = new();
        UiActionMap map = CreateMap(counter, "menu.close"u8);
        map.AddAction(UiActions.Cancel, "menu.close"u8);
        map.BindMouseButton(UiActions.Cancel, PointerButton.Right, UiModifiers.Control);

        Assert.False(map.PressMouseButton(PointerButton.Right, UiModifiers.None));
        Assert.False(map.PressMouseButton(PointerButton.Left, UiModifiers.Control));
        Assert.False(map.PressMouseButton(PointerButton.Middle, UiModifiers.Control));
        Assert.False(map.PressMouseButton(PointerButton.Right, UiModifiers.Control | UiModifiers.Shift));
        Assert.Equal(0, counter.Calls);

        Assert.True(map.PressMouseButton(PointerButton.Right, UiModifiers.Control));
        Assert.True(map.PressMouseButton(PointerButton.Right, UiModifiers.Control | UiModifiers.CapsLock));
        Assert.Equal(2, counter.Calls);
    }

    /// <summary>
    /// A bind of key 1 and a bind of mouse button 1 go to two actions. A key never matches the mouse binding, and a
    /// mouse button never matches the key binding: through the raw resolve and through the slot.
    /// </summary>
    [Fact]
    public void AKeyAndAMouseButtonWithTheSameCodeStayApart()
    {
        UiContext context = CreateContext();
        Element root = AddRoot(context);
        root.IsPickable = false;
        Counter keyCounter = new();
        Counter mouseCounter = new();
        UiActionMap map = CreateMap(keyCounter, "key.one"u8);
        map.Commands.Register("mouse.one"u8, s_count, mouseCounter);
        map.AddAction(UiActions.Confirm, "key.one"u8);
        map.AddAction(UiActions.Cancel, "mouse.one"u8);
        map.BindKey(UiActions.Confirm, 1);
        map.BindMouseButton(UiActions.Cancel, (PointerButton)1);

        Assert.True(map.PressKey(1, UiModifiers.None));
        Assert.Equal(1, keyCounter.Calls);
        Assert.Equal(0, mouseCounter.Calls);

        Assert.True(map.PressMouseButton(PointerButton.Right, UiModifiers.None));
        Assert.Equal(1, keyCounter.Calls);
        Assert.Equal(1, mouseCounter.Calls);

        map.Activate(context);
        context.Update(0f);
        KeyEvent key = Key(1);
        PointerEvent press = Pointer(UiEventType.PointerDown, new float2(300f, 200f), PointerButton.Right);
        context.PushEvent(in key);
        context.Update(0f);

        Assert.Equal(2, keyCounter.Calls);
        Assert.Equal(1, mouseCounter.Calls);

        context.PushEvent(in press);
        context.Update(0f);

        Assert.Equal(2, keyCounter.Calls);
        Assert.Equal(2, mouseCounter.Calls);
    }

    /// <summary>
    /// The demo of phase 6. The key W (87) is bound to <c>Confirm</c>. A text field holds the focus. A key down of W
    /// and a char <c>w</c> go through one update: the map calls no action, and the field takes the char. With the focus
    /// on a button, the same key down calls the action one time.
    /// </summary>
    [Fact]
    public void TheTextGuardSkipsATextKeyWhileATextFieldHoldsTheFocus()
    {
        TextScene scene = CreateTextScene();
        Counter counter = new();
        UiActionMap map = CreateMap(counter, "menu.ok"u8);
        map.AddAction(UiActions.Confirm, "menu.ok"u8);
        map.BindKey(UiActions.Confirm, WKey);
        map.Activate(scene.Context);
        Assert.True(scene.Context.SetFocus(scene.Field));

        scene.Context.PushEvent(Key(WKey));
        scene.Context.PushEvent(Char('w'));
        scene.Context.Update(0f);

        Assert.Equal(0, counter.Calls);
        Assert.True(scene.Field.Text.SequenceEqual("w"u8));

        Assert.True(scene.Context.SetFocus(scene.Button));
        scene.Context.PushEvent(Key(WKey));
        scene.Context.Update(0f);

        Assert.Equal(1, counter.Calls);
        Assert.True(scene.Field.Text.SequenceEqual("w"u8));
        Assert.True(map.PressKey(WKey, UiModifiers.None));
        Assert.Equal(2, counter.Calls);
    }

    /// <summary>
    /// With a text field in focus, Escape, Down, F1 (290), Control+S, Alt+S, and Super+S each call their action one
    /// time. Shift alone
    /// does not lift the guard: Shift+W calls nothing.
    /// </summary>
    [Fact]
    public void TheTextGuardPassesANonTextKeyAndAShortcut()
    {
        TextScene scene = CreateTextScene();
        Counter counter = new();
        UiActionMap map = CreateMap(counter, "menu.ok"u8);
        map.AddAction(UiActions.Confirm, "menu.ok"u8);
        map.BindKey(UiActions.Confirm, EscapeKey);
        map.BindKey(UiActions.Confirm, DownKey);
        map.BindKey(UiActions.Confirm, F1Key);
        map.BindKey(UiActions.Confirm, SKey, UiModifiers.Control);
        map.BindKey(UiActions.Confirm, WKey, UiModifiers.Shift);
        map.BindKey(UiActions.Confirm, SKey, UiModifiers.Alt);
        map.BindKey(UiActions.Confirm, SKey, UiModifiers.Super);
        map.Activate(scene.Context);
        Assert.True(scene.Context.SetFocus(scene.Field));

        scene.Context.PushEvent(Key(EscapeKey));
        scene.Context.Update(0f);
        Assert.Equal(1, counter.Calls);

        scene.Context.PushEvent(Key(DownKey));
        scene.Context.Update(0f);
        Assert.Equal(2, counter.Calls);

        scene.Context.PushEvent(Key(F1Key));
        scene.Context.Update(0f);
        Assert.Equal(3, counter.Calls);

        scene.Context.PushEvent(Key(SKey, UiModifiers.Control));
        scene.Context.Update(0f);
        Assert.Equal(4, counter.Calls);

        scene.Context.PushEvent(Key(SKey, UiModifiers.Alt));
        scene.Context.Update(0f);
        Assert.Equal(5, counter.Calls);

        scene.Context.PushEvent(Key(SKey, UiModifiers.Super));
        scene.Context.Update(0f);
        Assert.Equal(6, counter.Calls);

        scene.Context.PushEvent(Key(WKey, UiModifiers.Shift));
        scene.Context.Update(0f);
        Assert.Equal(6, counter.Calls);
        Assert.Same(scene.Field, scene.Context.FocusedElement);
    }

    /// <summary>
    /// A keypad key (320 to 336) is a text key. The first and the last keypad key and the key 321 call nothing with a
    /// text field in focus. The key 319 and the key 337 are not text keys, so they call their action.
    /// </summary>
    [Fact]
    public void TheTextGuardSkipsAKeypadKey()
    {
        TextScene scene = CreateTextScene();
        Counter counter = new();
        UiActionMap map = CreateMap(counter, "menu.ok"u8);
        map.AddAction(UiActions.Confirm, "menu.ok"u8);
        map.BindKey(UiActions.Confirm, 319);
        map.BindKey(UiActions.Confirm, 320);
        map.BindKey(UiActions.Confirm, 321);
        map.BindKey(UiActions.Confirm, 336);
        map.BindKey(UiActions.Confirm, 337);
        map.Activate(scene.Context);
        Assert.True(scene.Context.SetFocus(scene.Field));

        scene.Context.PushEvent(Key(320));
        scene.Context.PushEvent(Key(321));
        scene.Context.PushEvent(Key(336));
        scene.Context.Update(0f);
        Assert.Equal(0, counter.Calls);

        scene.Context.PushEvent(Key(319));
        scene.Context.PushEvent(Key(337));
        scene.Context.Update(0f);
        Assert.Equal(2, counter.Calls);

        Assert.True(scene.Context.SetFocus(scene.Button));
        scene.Context.PushEvent(Key(321));
        scene.Context.Update(0f);
        Assert.Equal(3, counter.Calls);
    }

    /// <summary>
    /// An empty action name, an empty command name, and a second action with the same name fail with an
    /// <see cref="ArgumentException"/> and change no count. A map needs a registry, and <c>Activate</c> needs a context.
    /// </summary>
    [Fact]
    public void AddActionRejectsAnEmptyOrDuplicateName()
    {
        Counter counter = new();
        UiActionMap map = CreateMap(counter, "menu.ok"u8);

        ArgumentException empty = Assert.Throws<ArgumentException>(() => map.AddAction(""u8, "menu.ok"u8));
        Assert.Equal("action", empty.ParamName);
        ArgumentException noCommand = Assert.Throws<ArgumentException>(() => map.AddAction(UiActions.Cancel, ""u8));
        Assert.Equal("command", noCommand.ParamName);
        Assert.Equal(0, map.ActionCount);

        map.AddAction(UiActions.Cancel, "menu.ok"u8);
        ArgumentException duplicate = Assert.Throws<ArgumentException>(() => map.AddAction(UiActions.Cancel, "menu.other"u8));
        Assert.Equal("action", duplicate.ParamName);
        Assert.Equal(1, map.ActionCount);

        map.AddAction(UiActions.Confirm, "menu.ok"u8);
        Assert.Equal(2, map.ActionCount);

        Assert.Throws<ArgumentNullException>(() => new UiActionMap(new UiActionMap.CreateInfo { Commands = null! }));
        Assert.Throws<ArgumentNullException>(() => map.Activate(null!));
        Assert.False(map.IsActive);
    }

    /// <summary>
    /// A bind of an unknown action, a second bind of the same input, a key of -1, a mouse button of -1, the gamepad
    /// value <c>None</c>, and the gamepad value <c>A | B</c> fail. The map keeps its counts. The same key with another
    /// modifier, and the same code of another kind, bind.
    /// </summary>
    [Fact]
    public void BindRejectsAnUnknownActionADuplicateInputAndABadCode()
    {
        Counter counter = new();
        UiActionMap map = CreateMap(counter, "menu.ok"u8);
        map.AddAction(UiActions.Cancel, "menu.ok"u8);
        map.BindKey(UiActions.Cancel, EscapeKey);
        map.BindMouseButton(UiActions.Cancel, PointerButton.Right);
        map.BindGamepadButton(UiActions.Cancel, UiGamepadButtons.B);
        Assert.Equal(3, map.BindingCount);

        ArgumentException unknownKey = Assert.Throws<ArgumentException>(() => map.BindKey("missing"u8, 100));
        Assert.Equal("action", unknownKey.ParamName);
        Assert.Throws<ArgumentException>(() => map.BindMouseButton("missing"u8, PointerButton.Left));
        Assert.Throws<ArgumentException>(() => map.BindGamepadButton("missing"u8, UiGamepadButtons.A));

        ArgumentException duplicateKey = Assert.Throws<ArgumentException>(() => map.BindKey(UiActions.Cancel, EscapeKey));
        Assert.Equal("action", duplicateKey.ParamName);
        Assert.Throws<ArgumentException>(() => map.BindKey(UiActions.Cancel, EscapeKey, UiModifiers.CapsLock));
        Assert.Throws<ArgumentException>(() => map.BindMouseButton(UiActions.Cancel, PointerButton.Right));
        Assert.Throws<ArgumentException>(() => map.BindGamepadButton(UiActions.Cancel, UiGamepadButtons.B));

        ArgumentOutOfRangeException badKey = Assert.Throws<ArgumentOutOfRangeException>(() => map.BindKey(UiActions.Cancel, -1));
        Assert.Equal("key", badKey.ParamName);
        Assert.Throws<ArgumentOutOfRangeException>(() => map.BindMouseButton(UiActions.Cancel, (PointerButton)(-1)));
        Assert.Throws<ArgumentException>(() => map.BindGamepadButton(UiActions.Cancel, UiGamepadButtons.None));
        Assert.Throws<ArgumentException>(() => map.BindGamepadButton(UiActions.Cancel, UiGamepadButtons.A | UiGamepadButtons.B));

        Assert.Equal(1, map.ActionCount);
        Assert.Equal(3, map.BindingCount);

        map.BindKey(UiActions.Cancel, EscapeKey, UiModifiers.Control);
        map.BindMouseButton(UiActions.Cancel, PointerButton.Left);
        map.BindGamepadButton(UiActions.Cancel, UiGamepadButtons.A);
        Assert.Equal(6, map.BindingCount);
    }

    /// <summary>
    /// The map copies the names. A change of the source arrays after the add changes neither the action nor its
    /// command.
    /// </summary>
    [Fact]
    public void AddActionAndBindKeyCopyTheNames()
    {
        Counter counter = new();
        UiActionMap map = CreateMap(counter, "menu.close"u8);
        byte[] action = UiActions.Cancel.ToArray();
        byte[] command = "menu.close"u8.ToArray();

        map.AddAction(action, command);
        map.BindKey(action, EscapeKey);
        action[0] = (byte)'X';
        command[0] = (byte)'X';

        Assert.True(map.PressKey(EscapeKey, UiModifiers.None));
        Assert.Equal(1, counter.Calls);
        Assert.True(map.InvokeAction(UiActions.Cancel));
        Assert.Equal(2, counter.Calls);
        Assert.False(map.InvokeAction(action));
        Assert.Equal(2, counter.Calls);
    }

    /// <summary>
    /// A command binds F2 during the press of F1. The map holds 8 bindings, so the bind grows the array in the middle of
    /// the press. The press of F1 ends with no fault and does not match F2. The next press of F2 calls its action. A
    /// command that binds a gamepad button during a <see cref="UiActionMap.PushGamepad"/> does not fire that button in
    /// the same push.
    /// </summary>
    [Fact]
    public void ABindDuringAPressTakesEffectAtTheNextPress()
    {
        Counter counter = new();
        UiActionMap map = CreateMap(counter, "menu.ok"u8);
        Binder binder = new() { Map = map };
        map.Commands.Register("bind.later"u8, s_bindLater, binder);
        map.AddAction(UiActions.Cancel, "bind.later"u8);
        map.AddAction(UiActions.Confirm, "menu.ok"u8);
        map.BindKey(UiActions.Cancel, F1Key);
        for (int key = 65; key < 72; key++)
        {
            map.BindKey(UiActions.Confirm, key);
        }

        Assert.Equal(8, map.BindingCount);

        Assert.True(map.PressKey(F1Key, UiModifiers.None));

        Assert.Equal(1, binder.Calls);
        Assert.Equal(9, map.BindingCount);
        Assert.Equal(0, counter.Calls);

        Assert.True(map.PressKey(F2Key, UiModifiers.None));
        Assert.Equal(1, counter.Calls);
        Assert.True(map.PressKey(F1Key, UiModifiers.None));
        Assert.Equal(2, binder.Calls);
        Assert.Equal(9, map.BindingCount);

        // The scan of PushGamepad runs a command and then goes on: the new bind of B waits for the next push.
        Binder padBinder = new() { Map = map };
        map.Commands.Register("bind.pad"u8, s_bindPadLater, padBinder);
        map.AddAction(UiActions.NavigateUp, "bind.pad"u8);
        map.BindGamepadButton(UiActions.NavigateUp, UiGamepadButtons.A);

        Assert.Equal(1, map.PushGamepad(UiGamepadButtons.A | UiGamepadButtons.B));
        Assert.Equal(1, padBinder.Calls);
        Assert.Equal(1, counter.Calls);
        Assert.Equal(11, map.BindingCount);

        Assert.Equal(0, map.PushGamepad(UiGamepadButtons.None));
        Assert.Equal(1, map.PushGamepad(UiGamepadButtons.B));
        Assert.Equal(2, counter.Calls);
    }

    /// <summary>
    /// A command that throws leaves <c>Update</c> and <c>PushGamepad</c>. The map stays usable: the next key calls the
    /// other action, and a held gamepad button does not fire again.
    /// </summary>
    [Fact]
    public void ACommandThatThrowsLeavesTheMapUsable()
    {
        UiContext context = CreateContext();
        AddRoot(context);
        Counter counter = new();
        UiActionMap map = CreateMap(counter, "menu.ok"u8);
        map.Commands.Register("menu.boom"u8, s_boom, null);
        map.AddAction(UiActions.Cancel, "menu.boom"u8);
        map.AddAction(UiActions.Confirm, "menu.ok"u8);
        map.BindKey(UiActions.Cancel, EscapeKey);
        map.BindKey(UiActions.Confirm, EnterKey);
        map.BindGamepadButton(UiActions.Cancel, UiGamepadButtons.B);
        map.BindGamepadButton(UiActions.Confirm, UiGamepadButtons.A);
        map.Activate(context);

        context.PushEvent(Key(EscapeKey));
        context.PushEvent(Key(EnterKey));
        Assert.Throws<BoomException>(() => context.Update(0f));
        Assert.Equal(0, counter.Calls);

        context.Update(0f);

        Assert.Equal(1, counter.Calls);
        Assert.Equal(0, context.QueuedEventCount);
        Assert.True(map.IsActive);

        Assert.Throws<BoomException>(() => map.PushGamepad(UiGamepadButtons.B));
        Assert.Equal(0, map.PushGamepad(UiGamepadButtons.B));
        Assert.Equal(1, map.PushGamepad(UiGamepadButtons.B | UiGamepadButtons.A));
        Assert.Equal(2, counter.Calls);
        Assert.Equal(0, map.PushGamepad(UiGamepadButtons.B | UiGamepadButtons.A));
        Assert.Equal(2, counter.Calls);

        Assert.Throws<BoomException>(() => map.PressKey(EscapeKey, UiModifiers.None));
        Assert.True(map.PressKey(EnterKey, UiModifiers.None));
        Assert.Equal(3, counter.Calls);
    }

    /// <summary>Creates a map on a new registry that holds one counter command.</summary>
    /// <param name="counter">The target of the command.</param>
    /// <param name="command">The UTF-8 name of the command.</param>
    /// <returns>The map with no action.</returns>
    private static UiActionMap CreateMap(Counter counter, ReadOnlySpan<byte> command)
    {
        CommandRegistry registry = new();
        registry.Register(command, s_count, counter);
        return new UiActionMap(new UiActionMap.CreateInfo { Commands = registry });
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

    /// <summary>Creates a panel of 100 by 100. It is pickable and has no handler.</summary>
    /// <returns>The panel.</returns>
    private static Element CreatePanel()
    {
        Element panel = new();
        panel.Inline.Width = 100;
        panel.Inline.Height = 100;
        return panel;
    }

    /// <summary>Creates a pointer event with no target.</summary>
    /// <param name="type">The pointer type.</param>
    /// <param name="position">The position in viewport pixels.</param>
    /// <param name="button">The button.</param>
    /// <param name="modifiers">The modifier keys.</param>
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

    /// <summary>Creates a char event with no target.</summary>
    /// <param name="character">The character. The tests use the Basic Latin block.</param>
    /// <returns>The event.</returns>
    private static CharEvent Char(char character)
    {
        return new CharEvent
        {
            Header = new UiEventHeader(UiEventType.Char, 0d, null),
            CodePoint = character,
        };
    }

    /// <summary>Creates a context with a text field and a button under a root. A first update lays them out.</summary>
    /// <returns>The scene.</returns>
    private static TextScene CreateTextScene()
    {
        UiContext context = CreateContext();
        Element root = AddRoot(context);
        TextField field = new(new TextField.CreateInfo { Capacity = 16 });
        Button button = new(new Button.CreateInfo { Text = "Go" });
        root.Children.Add(field);
        root.Children.Add(button);
        context.Update(0f);
        return new TextScene { Context = context, Field = field, Button = button };
    }

    /// <summary>A context with a focusable text field and a focusable button.</summary>
    private sealed class TextScene
    {
        /// <summary>The context.</summary>
        public required UiContext Context;

        /// <summary>The text field.</summary>
        public required TextField Field;

        /// <summary>The button.</summary>
        public required Button Button;
    }

    /// <summary>A target that counts the calls of its command.</summary>
    private sealed class Counter
    {
        /// <summary>The count of calls.</summary>
        public int Calls;
    }

    /// <summary>A target whose command binds F2 on its map.</summary>
    private sealed class Binder
    {
        /// <summary>The map that the command changes.</summary>
        public required UiActionMap Map;

        /// <summary>The count of calls.</summary>
        public int Calls;
    }

    /// <summary>The exception that the command <c>menu.boom</c> throws.</summary>
    private sealed class BoomException : Exception
    {
        /// <summary>Creates the exception.</summary>
        public BoomException()
            : base("The command throws on purpose.")
        {
        }
    }
}
