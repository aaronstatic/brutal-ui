using System;
using Brutal.Numerics;
using BrutalUi.Core;

namespace BrutalUi.Framework;

/// <summary>
/// A map from an input to a named action, and from an action to a command of a <see cref="CommandRegistry"/>.
/// </summary>
/// <remarks>
/// <para>
/// An action maps a UTF-8 name to a type and a command name. The type is <see cref="UiActionType.Button"/>,
/// <see cref="UiActionType.Axis"/>, or <see cref="UiActionType.Vector2"/>. A binding maps a key with modifiers, a mouse
/// button with modifiers, a gamepad button, four keys, a gamepad stick, or a gamepad axis to an action. The names compare
/// by their bytes. <see cref="UiActions"/> holds the standard names, and a game can use other names.
/// <see cref="AddAction(ReadOnlySpan{byte}, UiActionType)"/> adds an action with no command, and an action with no
/// command invokes nothing.
/// </para>
/// <para>
/// The build-time rule: the map copies each name, and an add or a bind can grow an array. Thus add the actions and
/// the bindings at build time, not in the frame loop.
/// </para>
/// <para>
/// The resolve rule: a resolve scans the bindings in the order of the bind and uses the first match. It allocates
/// nothing and throws nothing of its own. The map calls <see cref="CommandRegistry.Invoke"/>, so a command that the
/// registry does not hold calls <see cref="CommandRegistry.UnknownCommand"/> of the registry. An exception of a
/// command leaves the call, and the map stays usable.
/// </para>
/// <para>
/// The modifier rule: a binding matches only the same Shift, Control, Alt, and Super keys. Caps Lock and Num Lock do
/// not count. Thus Control+S does not match a bind of S, and S does not match a bind of Control+S.
/// </para>
/// <para>
/// The type rule: each action has a type of <see cref="UiActionType"/>. A bind needs an action of the type that its
/// input fills, and a query needs the type that it reads. Thus <see cref="BindKey(ReadOnlySpan{byte}, int, UiModifiers)"/>
/// needs a Button action, and
/// <see cref="BindKeyVector(ReadOnlySpan{byte}, int, int, int, int)"/> needs a Vector2 action. <see cref="ReadVector"/> reads a Vector2 action,
/// <see cref="ReadAxis"/> reads an Axis action, and <see cref="WasPressed"/>, <see cref="IsHeld"/>, and
/// <see cref="WasReleased"/> read a Button action. A bind or a query of the wrong type throws
/// <see cref="ArgumentException"/> with the parameter name <c>action</c>.
/// </para>
/// <para>
/// The value rule: the frame read of an active map writes the value of each typed action one time in each
/// <see cref="UiContext.Update(float)"/> call. The read sums the value of each binding of the action, in the order of
/// the bind, then clamps the sum, then adds the sum of each binding that keeps its value free (no clamp). A Vector2
/// clamp keeps the length at 1, so W and D together give (0.707107, 0.707107). An Axis clamp keeps the value at -1 to
/// 1. The pointer delta and the scroll keep their value free (Q3), so they add after the clamp and read any size. The
/// delta keeps the pointer coordinates of the context, so it points Y down (Decision 6). A query reads the value of the
/// last frame, and the value stays fixed until the next call.
/// </para>
/// <para>
/// The press rule: the frame read of an active map writes the pressed, held, and released flags of each Button action,
/// and <see cref="WasPressed"/>, <see cref="IsHeld"/>, and <see cref="WasReleased"/> read them. The read takes the press
/// from the press edge of the state, in the same frame in which the slot path invokes the command of that press, so the
/// queries and the commands agree on one press (A4). The frame read never invokes a key command or a mouse command. The
/// routing gate applies to each query, and the text guard applies to the slot path only (A1). The gate limit: an element
/// that is not a text field closes the key gate of the game view, and a key that no element took still reaches the slot,
/// so its command runs while <see cref="WasPressed"/> reads <see langword="false"/>. The mouse limit: a mouse-button bind
/// with modifiers matches the modifiers of the last key event, and the slot path uses the pointer event modifiers, so
/// the query and the command can disagree for a modified mouse press only. The held flag keeps the latch: an
/// action becomes held only through a press that the map reads.
/// </para>
/// <para>
/// The gamepad rule: an active map reads the pad of <see cref="GamepadSlot"/> in the frame read. The read fills a
/// Vector2 from a stick and an Axis from a trigger or a stick axis, and it invokes the command of each gamepad button
/// press in <see cref="UiGamepadState.Pressed"/>, in the order of the bind. A command that deactivates the map or
/// replaces the read slot stops the invoke pass. <see cref="PushGamepad"/> works on every map, and an active map also
/// reads the pad in <see cref="UiContext.Update(float)"/>, so a game must not push the same pad into an active map.
/// </para>
/// <para>
/// The group rule: a scheme is one <see cref="int"/> id that resolves to a UTF-8 name. The table holds
/// <see cref="UiScheme.KeyboardMouse"/> and <see cref="UiScheme.Gamepad"/> from construction, so
/// <see cref="AddScheme(ReadOnlySpan{byte})"/> gives their id with no add, and a game can add other names. A binding
/// with the default id 0 belongs to each scheme. <see cref="UiScheme"/> names the standard names, and
/// <see cref="SchemeName(int)"/> reads the name of any id.
/// </para>
/// <para>
/// The active rule: <see cref="ActiveScheme"/> reports the device of the last binding that gave a value. A key, a
/// mouse button, a key composite, the pointer delta, and a scroll give KeyboardMouse. A stick, an axis, and a
/// gamepad button give Gamepad. A stored value of exactly 0 gives no value. The report carries from frame to frame, so
/// a frame with no input keeps the last report. The report starts at KeyboardMouse at construction and after
/// <see cref="Activate"/>. A custom scheme name groups bindings only, because the report follows the device, not the
/// tag of a binding.
/// </para>
/// <para>
/// The change-slot rule: <see cref="OnSchemeChanged"/> is one handler that a game writes with <c>=</c>. The frame read
/// calls it at the end of <see cref="UiContext.Update(float)"/>, after the finish pass and after the gamepad invoke
/// pass, and only when the scheme that the map reports changed, so one change gives one call. A locked map reports the
/// locked id. See <see cref="UiSchemeChangedHandler"/>.
/// </para>
/// <para>
/// The lock rule: <see cref="SetScheme(int)"/> locks the map to one scheme, and <see cref="LockedScheme"/> reads the
/// lock. A binding of another scheme gives no value on three paths: the binding loop of the frame read, the press path
/// of <see cref="PressKey"/> and <see cref="PressMouseButton"/>, and the gamepad invoke of the frame read and of
/// <see cref="PushGamepad"/>. A binding of the default scheme reads under every lock. With no lock, the map reads every
/// binding, whatever its tag, so the report can still move to another device. <see cref="ActiveScheme"/> reads the
/// locked id while the lock stands. <see cref="Activate"/> and <see cref="Deactivate"/> keep the lock, because it is a
/// build setting of the map. <see cref="InvokeAction"/> names an action, not an input, so the lock does not touch it:
/// the lock already emptied the values of the queries.
/// </para>
/// <para>
/// The slot rule: <see cref="Activate"/> writes two slots: <see cref="UiContext.OnUnhandledInput"/> for the input that
/// no element took, and <see cref="UiContext.OnInputRead"/> for the read of each frame. Each context holds one of each
/// slot, so one map is active on a context (A8). A map is active on one context at most: <see cref="Activate"/> on a
/// second context clears the two slots of the first context. <see cref="Deactivate"/> clears the two slots of its
/// context, also when another map replaced the unhandled slot. The context then gives the map each key down that no
/// element took, and <see cref="UiContext.Update(float)"/> calls the frame read of the map after the drain. A key repeat
/// invokes nothing. <see cref="PressKey"/>, <see cref="PressMouseButton"/>, and
/// <see cref="InvokeAction"/> work with no <see cref="Activate"/>.
/// </para>
/// <para>
/// The switch rule: <see cref="Deactivate"/> releases each action (A6): it sets each value to zero, and it sets each
/// flag to <see langword="false"/>. A fresh <see cref="Activate"/> starts with no value: the method resets each frame
/// value when the map was not active on that context. A second <see cref="Activate"/> on the same context keeps each
/// value, so the same map in two modes keeps its values. A query of a map that is not active reads
/// <see langword="false"/> or zero, so a map that another map replaced reads nothing. The pair test:
/// <see cref="IsActive"/> reads both slots, and the frame read starts with the same test, so a hand-written slot
/// cannot leave a map active with frozen values. A Button action needs a new
/// press in the next map, because the latch starts from no held flag. An Axis action and a Vector2 action read a held
/// input in the next map at once, because a value has no edge to go stale (Decision 12).
/// </para>
/// <para>
/// The mouse rule: the context gives the map a press over no element only. A press on an element never reaches the
/// map, also when no handler of the element stops it. A layout root that is pickable takes each press inside its
/// rect. Thus set <see cref="Element.IsPickable"/> to <see langword="false"/> on a root that fills the window (the
/// layout-root rule). A key binding and a mouse binding never match each other, also with the same code.
/// </para>
/// <para>
/// The text guard: a key down through the slot is skipped when a <see cref="TextField"/> holds the focus, no Control,
/// Alt, or Super key is down, and the key is a text key. A text key is each key code below 256 (the printable keys and
/// the world keys of GLFW) and each keypad key from 320 to 336. Each key from 256 to 319 passes: Escape, Enter, Tab,
/// Backspace, Insert, Delete, the arrow keys, Page Up, Page Down, Home, End, the lock keys, Print Screen, Pause, and
/// F1 to F25. Shift alone does not lift the guard. The reason: GLFW sends a key down and a char for a text key, and the
/// field takes the char. Without the guard, a letter that is bound to an action would type and also call the action.
/// The guard applies to the slot path only. <see cref="PressKey"/> is the raw resolve and applies no guard.
/// </para>
/// <para>
/// The build rules: <see cref="AddAction(ReadOnlySpan{byte}, UiActionType)"/> and
/// <see cref="AddAction(ReadOnlySpan{byte}, ReadOnlySpan{byte})"/> reject an empty name and an action name that the map
/// holds, and the typed add rejects a type that <see cref="UiActionType"/> does not define. Each bind
/// checks in this order: its input arguments, the action name, the action type, the scheme id, then a duplicate. A
/// bind rejects an action that the map does not hold, and an input that the map binds already in its scheme (the same
/// kind, code, and modifiers). The duplicate check is per scheme: the default scheme belongs to each scheme, so a
/// default bind clashes with a tagged bind of the same input, in either order, and two tags that differ are not a
/// duplicate. A bind rejects a scheme id that <see cref="AddScheme(ReadOnlySpan{byte})"/> did not give and that is not
/// the default id 0.
/// A composite bind compares its codes as a set: two keys of a
/// <see cref="BindKeyAxis(ReadOnlySpan{byte}, int, int)"/> bind, or four keys
/// of a <see cref="BindKeyVector(ReadOnlySpan{byte}, int, int, int, int)"/> bind, in another order are the same input,
/// so the second bind throws.
/// <see cref="BindKey(ReadOnlySpan{byte}, int, UiModifiers)"/> rejects a key outside 0 to
/// <see cref="UiKeyState.KeyCount"/> - 1,
/// <see cref="BindMouseButton(ReadOnlySpan{byte}, BrutalUi.Core.PointerButton, UiModifiers)"/> rejects a button outside
/// 0 to <see cref="UiPointerButtonState.ButtonCount"/> - 1,
/// and <see cref="BindGamepadButton(ReadOnlySpan{byte}, UiGamepadButtons)"/> rejects <see cref="UiGamepadButtons.None"/>
/// and a value with more than one bit.
/// <see cref="BindKeyVector(ReadOnlySpan{byte}, int, int, int, int)"/> rejects a key outside 0 to
/// <see cref="UiKeyState.KeyCount"/> - 1, two keys with the same
/// code, and four keys that the map binds already in its scheme.
/// <see cref="BindGamepadStick(ReadOnlySpan{byte}, UiGamepadStick)"/> rejects a stick that
/// <see cref="UiGamepadStick"/> does not define and a stick that the map binds already in its scheme.
/// <see cref="BindGamepadAxis(ReadOnlySpan{byte}, BrutalUi.Core.UiGamepadAxis, float)"/> rejects an axis outside 0 to 5,
/// a scale that is not finite, and an axis that the map
/// binds already in its scheme. <see cref="BindKeyAxis(ReadOnlySpan{byte}, int, int)"/> rejects a key outside 0 to
/// <see cref="UiKeyState.KeyCount"/> - 1, two keys
/// with the same code, and the same two keys in either order.
/// <see cref="BindMouseDelta(ReadOnlySpan{byte}, float)"/> and
/// <see cref="BindScroll(ReadOnlySpan{byte}, float)"/> reject a scale that is not finite, and one map holds one pointer
/// delta binding and one scroll binding in each scheme. The type rule: each bind needs an action of its own type (see
/// <see cref="UiActionType"/>), and a bind of the wrong type throws <see cref="ArgumentException"/> with the parameter
/// name <c>action</c>. A rejected call changes no count.
/// </para>
/// <para>
/// The re-entry rule: each resolve copies the binding array and the count before it scans, each invoke copies the
/// command name before the call, and the frame read copies both arrays and both counts before it scans. A command
/// that adds an action or a binding thus takes effect at the next input.
/// </para>
/// <para>
/// The error rule: the resolve methods throw nothing of their own. An exception of a command leaves
/// <see cref="PressKey"/>, <see cref="PressMouseButton"/>, <see cref="PushGamepad"/>, or
/// <see cref="UiContext.Update(float)"/>, and the map stays usable. <see cref="PushGamepad"/> stores the buttons
/// before it calls a command, so a held button does not fire again after an exception. The frame read finishes every
/// action before its invoke pass, so an exception of a command leaves <see cref="UiContext.Update(float)"/> and the
/// map keeps the values of the frame. An exception of the handler of <see cref="OnSchemeChanged"/> leaves
/// <see cref="UiContext.Update(float)"/> before the binding pass, and the map keeps the values and the report of the
/// frame.
/// </para>
/// <para>
/// The thread rule: the map runs on the UI thread. It uses no lock.
/// </para>
/// </remarks>
public sealed partial class UiActionMap
{
    /// <summary>The first size of each array.</summary>
    private const int InitialCapacity = 8;

    /// <summary>The modifier keys that a binding compares. Caps Lock and Num Lock do not count.</summary>
    private const UiModifiers MatchMask = UiModifiers.Shift | UiModifiers.Control | UiModifiers.Alt | UiModifiers.Super;

    /// <summary>
    /// The public parameter name of every bind and every query: <c>action</c>. Each throw names it through this constant,
    /// so a rename of a parameter cannot leave a wrong name in a throw.
    /// </summary>
    private const string ActionParameter = "action";

    /// <summary>The first key code that is not a text key: GLFW_KEY_ESCAPE (256). Each code below it is a text key.</summary>
    private const int FirstNonTextKey = 256;

    /// <summary>The first keypad key: GLFW_KEY_KP_0 (320).</summary>
    private const int KeypadFirstKey = 320;

    /// <summary>The last keypad key: GLFW_KEY_KP_EQUAL (336).</summary>
    private const int KeypadLastKey = 336;

    /// <summary>The handler that <see cref="Activate"/> writes into the slot. The constructor creates it one time.</summary>
    private readonly UiUnhandledInputHandler _onInput;

    /// <summary>The frame read that <see cref="Activate"/> writes into the read slot. The constructor creates it one time.</summary>
    private readonly UiInputReadHandler _onRead;

    /// <summary>The actions in the order of the add.</summary>
    private ActionEntry[] _actions = [];

    /// <summary>The count of used action entries.</summary>
    private int _actionCount;

    /// <summary>The bindings in the order of the bind.</summary>
    private InputBinding[] _bindings = [];

    /// <summary>The count of used bindings.</summary>
    private int _bindingCount;

    /// <summary>The context of the last <see cref="Activate"/>, or <see langword="null"/>.</summary>
    private UiContext? _context;

    /// <summary>The buttons of the last <see cref="PushGamepad"/>.</summary>
    private UiGamepadButtons _gamepad;

    /// <summary>The gamepad slot that the frame read reads. The default is 0.</summary>
    private int _gamepadSlot;

    /// <summary>
    /// Creates an empty map that invokes the commands of a registry.
    /// </summary>
    /// <param name="info">The create-info with the registry.</param>
    /// <exception cref="ArgumentNullException">The <see cref="CreateInfo.Commands"/> of <paramref name="info"/> is <see langword="null"/>.</exception>
    public UiActionMap(in CreateInfo info)
    {
        if (info.Commands is null)
        {
            throw new ArgumentNullException(nameof(info), "The Commands of the UiActionMap.CreateInfo must not be null.");
        }

        Commands = info.Commands;
        _onInput = HandleInput;
        _onRead = ReadFrame;
        SeedSchemes();
    }

    /// <summary>Gets the registry that holds the commands of the actions.</summary>
    public CommandRegistry Commands { get; }

    /// <summary>Gets the count of actions.</summary>
    public int ActionCount => _actionCount;

    /// <summary>Gets the count of bindings.</summary>
    public int BindingCount => _bindingCount;

    /// <summary>
    /// Gets or sets the gamepad slot that the frame read of an active map reads.
    /// </summary>
    /// <remarks>
    /// The default is 0. A set that is outside 0 to 15 throws <see cref="ArgumentOutOfRangeException"/> and keeps the
    /// old value. The next frame read uses the new slot. <see cref="PushGamepad"/> does not read this property: the
    /// method works on the buttons that its caller gives.
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">
    /// A set outside 0 to 15. The parameter name is <c>value</c>.
    /// </exception>
    public int GamepadSlot
    {
        get => _gamepadSlot;
        set
        {
            if ((uint)value >= UiInputState.GamepadCount)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(value),
                    "The gamepad slot must be from 0 to UiInputState.GamepadCount - 1.");
            }

            _gamepadSlot = value;
        }
    }

    /// <summary>
    /// Reads the value of a Vector2 action.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The value rule: the method gives the value of the last frame read. <see cref="UiContext.Update(float)"/> writes
    /// each value one time in each frame, through the slot <see cref="UiContext.OnInputRead"/>, and the value stays
    /// fixed until the next call. A name that the map does not hold gives (0, 0). A map that is not active gives
    /// (0, 0) (the switch rule). The map reads the game view of the input state, so the routing gate empties a value
    /// that a focus owner of the UI takes.
    /// </para>
    /// <para>The read copies one small vector: it allocates nothing, so it gives 0 bytes.</para>
    /// </remarks>
    /// <param name="action">The UTF-8 name of the action.</param>
    /// <returns>The value of the last frame read.</returns>
    /// <exception cref="ArgumentException">
    /// The map holds an action of the name <paramref name="action"/>, and the action is not a
    /// <see cref="UiActionType.Vector2"/>. The parameter name is <c>action</c>.
    /// </exception>
    public float2 ReadVector(ReadOnlySpan<byte> action)
    {
        int index = IndexOfAction(action);
        if (index < 0)
        {
            return new float2(0f, 0f);
        }

        CheckActionType(index, UiActionType.Vector2);
        if (!IsActive)
        {
            return new float2(0f, 0f);
        }

        return _actions[index].Value;
    }

    /// <summary>
    /// Reads the value of an Axis action.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The value rule: the method gives the value of the last frame read. <see cref="UiContext.Update(float)"/> writes
    /// each value one time in each frame, through the slot <see cref="UiContext.OnInputRead"/>, and the value stays
    /// fixed until the next call. A name that the map does not hold gives 0. A map that is not active gives 0 (the
    /// switch rule). The map reads the game view of the input state, so the routing gate empties a value that a focus
    /// owner of the UI takes. The Axis finish clamps a clamped sum to -1 to 1; a free sum keeps its size.
    /// </para>
    /// <para>The read copies one small float: it allocates nothing, so it gives 0 bytes.</para>
    /// </remarks>
    /// <param name="action">The UTF-8 name of the action.</param>
    /// <returns>The value of the last frame read.</returns>
    /// <exception cref="ArgumentException">
    /// The map holds an action of the name <paramref name="action"/>, and the action is not a
    /// <see cref="UiActionType.Axis"/>. The parameter name is <c>action</c>.
    /// </exception>
    public float ReadAxis(ReadOnlySpan<byte> action)
    {
        int index = IndexOfAction(action);
        if (index < 0)
        {
            return 0f;
        }

        CheckActionType(index, UiActionType.Axis);
        if (!IsActive)
        {
            return 0f;
        }

        return _actions[index].Value.X;
    }

    /// <summary>
    /// Reads the pressed flag of a Button action.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The press rule: the method gives <see langword="true"/> when the last frame read saw one press edge of one
    /// binding of the action. The read takes the edge from the state of the same frame in which the slot path invokes
    /// the command of that press, so the queries and the commands agree on one press (A4). A repeat gives
    /// <see langword="false"/>, because the state holds no press edge for a repeat. A press and a release in one frame
    /// give <see langword="true"/>, together with <see cref="WasReleased"/>. The routing gate applies to the query, and
    /// the text guard applies to the slot path only (A1, the press rule of this class). A name that the map does not
    /// hold gives <see langword="false"/>. A map that is not active gives <see langword="false"/> (the switch rule).
    /// The flag stays fixed until the next <see cref="UiContext.Update(float)"/> call.
    /// </para>
    /// <para>The read copies one bool: it allocates nothing, so it gives 0 bytes.</para>
    /// </remarks>
    /// <param name="action">The UTF-8 name of the action.</param>
    /// <returns>
    /// <see langword="true"/> when the last frame read saw a press of the action. Else <see langword="false"/>.
    /// </returns>
    /// <exception cref="ArgumentException">
    /// The map holds an action of the name <paramref name="action"/>, and the action is not a
    /// <see cref="UiActionType.Button"/>. The parameter name is <c>action</c>.
    /// </exception>
    public bool WasPressed(ReadOnlySpan<byte> action)
    {
        int index = IndexOfAction(action);
        if (index < 0)
        {
            return false;
        }

        CheckActionType(index, UiActionType.Button);
        if (!IsActive)
        {
            return false;
        }

        return _actions[index].Pressed;
    }

    /// <summary>
    /// Reads the held flag of a Button action.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The press rule: the method gives <see langword="true"/> while one binding of the action holds the input down and
    /// the action stays held since a press. The latch: an action becomes held only through a press that the map reads,
    /// so a key that is already down when a map becomes active keeps the flag <see langword="false"/> until a new press
    /// (the switch rule of this class). Two bindings of one action hold it together: the action releases only when
    /// the last one stops holding it. A press and a release in one frame give <see langword="false"/>, because the
    /// input is not down at the end of the frame. The routing gate applies to the query, and the text guard applies to
    /// the slot path only (A1, the press rule of this class). A name that the map does not hold gives
    /// <see langword="false"/>. A map that is not active gives <see langword="false"/> (the switch rule). The flag
    /// stays fixed until the next <see cref="UiContext.Update(float)"/> call.
    /// </para>
    /// <para>The read copies one bool: it allocates nothing, so it gives 0 bytes.</para>
    /// </remarks>
    /// <param name="action">The UTF-8 name of the action.</param>
    /// <returns><see langword="true"/> when the action is held since its press. Else <see langword="false"/>.</returns>
    /// <exception cref="ArgumentException">
    /// The map holds an action of the name <paramref name="action"/>, and the action is not a
    /// <see cref="UiActionType.Button"/>. The parameter name is <c>action</c>.
    /// </exception>
    public bool IsHeld(ReadOnlySpan<byte> action)
    {
        int index = IndexOfAction(action);
        if (index < 0)
        {
            return false;
        }

        CheckActionType(index, UiActionType.Button);
        if (!IsActive)
        {
            return false;
        }

        return _actions[index].Held;
    }

    /// <summary>
    /// Reads the released flag of a Button action.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The press rule: the method gives <see langword="true"/> when the last frame read saw the action stop being held.
    /// The flag covers a release of every kind: the input went up, or a press and a release ran in one frame. The flag
    /// describes one frame, so the next <see cref="UiContext.Update(float)"/> call gives <see langword="false"/> for the
    /// same release. The gate applies to the query, and the text guard applies to the slot path only (A1, the press rule
    /// of this class). A name that the map does not hold gives <see langword="false"/>. A map that is not active gives
    /// <see langword="false"/> (the switch rule).
    /// </para>
    /// <para>The read copies one bool: it allocates nothing, so it gives 0 bytes.</para>
    /// </remarks>
    /// <param name="action">The UTF-8 name of the action.</param>
    /// <returns><see langword="true"/> when the action released in the last frame read. Else <see langword="false"/>.</returns>
    /// <exception cref="ArgumentException">
    /// The map holds an action of the name <paramref name="action"/>, and the action is not a
    /// <see cref="UiActionType.Button"/>. The parameter name is <c>action</c>.
    /// </exception>
    public bool WasReleased(ReadOnlySpan<byte> action)
    {
        int index = IndexOfAction(action);
        if (index < 0)
        {
            return false;
        }

        CheckActionType(index, UiActionType.Button);
        if (!IsActive)
        {
            return false;
        }

        return _actions[index].Released;
    }

    /// <summary>
    /// Makes this map the active map of a context.
    /// </summary>
    /// <remarks>
    /// The method writes the handler of the map into <see cref="UiContext.OnUnhandledInput"/> and the frame read of the
    /// map into <see cref="UiContext.OnInputRead"/>. Each write replaces each delegate in its slot, also a delegate of
    /// the game, so one map is active on a context (A8). A second <see cref="Activate"/> of another map on the same
    /// context replaces the first map: the first map then reads <see langword="false"/> and zero, because each query
    /// checks <see cref="IsActive"/>, and a fresh <see cref="Activate"/> of it starts with no value. A mode list of
    /// maps waits for panels-and-modes. Each handler is one delegate that the constructor creates, so a call allocates
    /// nothing. Call it at build time or at a change of mode. A map is active on one context at most, and the last
    /// <see cref="Activate"/> wins: when the map is active on another context, the method first clears the two slots of
    /// that context. It clears a slot only when the slot holds the handler of this map. The switch rule: the method
    /// resets each frame value when the map was not active on this context. A second <see cref="Activate"/> on the same
    /// context keeps each value, so the same map in two modes keeps its values.
    /// </remarks>
    /// <param name="context">The context.</param>
    /// <exception cref="ArgumentNullException"><paramref name="context"/> is <see langword="null"/>.</exception>
    public void Activate(UiContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        bool wasActive = ReferenceEquals(_context, context) && IsActive;
        if (_context is { } old && !ReferenceEquals(old, context))
        {
            if (old.OnUnhandledInput == _onInput)
            {
                old.OnUnhandledInput = null;
            }

            if (old.OnInputRead == _onRead)
            {
                old.OnInputRead = null;
            }
        }

        context.OnUnhandledInput = _onInput;
        context.OnInputRead = _onRead;
        _context = context;
        if (!wasActive)
        {
            ResetFrame();
        }
    }

    /// <summary>
    /// Gets a value that tells if this map is the active map of its context.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The value is <see langword="true"/> when the context of the last <see cref="Activate"/> holds the handler of this
    /// map in <see cref="UiContext.OnUnhandledInput"/> and the frame read of this map in
    /// <see cref="UiContext.OnInputRead"/>. The value is <see langword="false"/> when either slot holds another handler
    /// or <see langword="null"/>, so one hand-written slot makes this map inactive. A later map that replaced this map
    /// gives <see langword="false"/>.
    /// </para>
    /// <para>
    /// Each query checks this value (the switch rule): a map that is not active reads <see langword="false"/> or zero.
    /// The frame read starts with the same test, so no query of a map that is not active reads a frozen value.
    /// </para>
    /// </remarks>
    public bool IsActive => _context is { } c && c.OnUnhandledInput == _onInput && c.OnInputRead == _onRead;

    /// <summary>
    /// Clears the two slots of the context when this map holds them, and releases each action.
    /// </summary>
    /// <remarks>
    /// The method clears <see cref="UiContext.OnUnhandledInput"/> when the slot holds the handler of this map, and
    /// <see cref="UiContext.OnInputRead"/> when the slot holds the frame read of this map. The method clears the read
    /// slot also when another map replaced the unhandled slot: the map then gives up a slot that it still holds. A map
    /// that another map replaced on both slots clears nothing, so the active map stays active. A call on a map that was
    /// never active clears no slot. The switch rule: the method then releases each action (A6). It resets each frame
    /// value, also on a map that another map replaced. The method allocates nothing.
    /// </remarks>
    public void Deactivate()
    {
        if (_context is { } context)
        {
            if (context.OnUnhandledInput == _onInput)
            {
                context.OnUnhandledInput = null;
            }

            if (context.OnInputRead == _onRead)
            {
                context.OnInputRead = null;
            }
        }

        _context = null;
        ResetFrame();
    }

    /// <summary>
    /// Sets the value, each flag, and the frame scratch of each action to their defaults.
    /// </summary>
    /// <remarks>
    /// <see cref="Activate"/> and <see cref="Deactivate"/> call it (the switch rule). The method writes the entries of
    /// the existing array in place: it creates no array, and it allocates nothing. The method resets the scheme report
    /// to the seeded KeyboardMouse id too, so a fresh <see cref="Activate"/> starts the report at KeyboardMouse. It
    /// leaves the locked scheme alone: the lock is a build setting of the map, not a frame value.
    /// </remarks>
    private void ResetFrame()
    {
        ActionEntry[] actions = _actions;
        for (int i = 0; i < _actionCount; i++)
        {
            ref ActionEntry a = ref actions[i];
            a.Value = default;
            a.Pressed = false;
            a.Held = false;
            a.Released = false;
            a.Sum = default;
            a.Free = default;
            a.Down = false;
            a.Press = false;
        }

        _activeScheme = _keyboardScheme;
        _reportedScheme = _keyboardScheme;
    }

    /// <summary>The kind of an input of a binding.</summary>
    private enum InputKind : byte
    {
        /// <summary>A key of the keyboard.</summary>
        Key,

        /// <summary>A button of the mouse.</summary>
        MouseButton,

        /// <summary>A button of a gamepad.</summary>
        GamepadButton,

        /// <summary>
        /// Four keys that fill a Vector2 action: up, left, down, and right in <see cref="InputBinding.Code"/> to
        /// <see cref="InputBinding.Code3"/>.
        /// </summary>
        KeyVector,

        /// <summary>
        /// One stick of a gamepad that fills a Vector2 action: the <see cref="UiGamepadStick"/> value in
        /// <see cref="InputBinding.Code"/>.
        /// </summary>
        GamepadStick,

        /// <summary>
        /// One axis of a gamepad that fills an Axis action: the <see cref="UiGamepadAxis"/> value in
        /// <see cref="InputBinding.Code"/>, with the scale in <see cref="InputBinding.Scale"/>.
        /// </summary>
        GamepadAxis,

        /// <summary>
        /// Two keys that fill an Axis action: the negative key in <see cref="InputBinding.Code"/> and the positive key
        /// in <see cref="InputBinding.Code1"/>.
        /// </summary>
        KeyAxis,

        /// <summary>
        /// The pointer delta of the frame, scaled, into a Vector2 action. The binding holds no code, and the frame keeps
        /// the value free.
        /// </summary>
        MouseDelta,

        /// <summary>
        /// The vertical scroll of the frame, scaled, into an Axis action. The binding holds no code, and the frame keeps
        /// the value free.
        /// </summary>
        Scroll,
    }

    /// <summary>One action.</summary>
    private struct ActionEntry
    {
        /// <summary>The copy of the action name.</summary>
        public byte[] Name;

        /// <summary>The copy of the command name. An action with no command has an empty array.</summary>
        public byte[] Command;

        /// <summary>The kind of the value of the action.</summary>
        public UiActionType Type;

        /// <summary>The value of the last frame read. A Button action keeps zero.</summary>
        public float2 Value;

        /// <summary><see langword="true"/> when the action got a press in the last frame read.</summary>
        public bool Pressed;

        /// <summary><see langword="true"/> when the action is held since its press.</summary>
        public bool Held;

        /// <summary><see langword="true"/> when the action released in the last frame read.</summary>
        public bool Released;

        /// <summary>The frame scratch: the sum of each read that the frame clamps.</summary>
        public float2 Sum;

        /// <summary>The frame scratch: the sum of each read that the frame keeps free.</summary>
        public float2 Free;

        /// <summary>The frame scratch: <see langword="true"/> when any read found the input down.</summary>
        public bool Down;

        /// <summary>The frame scratch: <see langword="true"/> when any read found a press.</summary>
        public bool Press;
    }

    /// <summary>One binding of an input to an action.</summary>
    private struct InputBinding
    {
        /// <summary>The kind of the input.</summary>
        public InputKind Kind;

        /// <summary>
        /// The code of the input: a GLFW key code for a key, a GLFW mouse button value for a mouse button, the bit of a
        /// gamepad button, the first code of a composite, or the stick or the axis of a gamepad value. For a
        /// <see cref="InputKind.KeyVector"/> it is the up key, and for a <see cref="InputKind.KeyAxis"/> it is the
        /// negative key. A <see cref="InputKind.MouseDelta"/> binding and a <see cref="InputKind.Scroll"/> binding hold
        /// -1, because the pointer part of the frame needs no code.
        /// </summary>
        public int Code;

        /// <summary>
        /// The second code of a composite: the positive key of a key axis, or the left key of a key vector. An unused
        /// code is -1.
        /// </summary>
        public int Code1;

        /// <summary>
        /// The third code of a composite: the down key of a key vector. An unused code is -1.
        /// </summary>
        public int Code2;

        /// <summary>
        /// The fourth code of a composite: the right key of a key vector. An unused code is -1.
        /// </summary>
        public int Code3;

        /// <summary>The modifier keys of the bind, masked with <see cref="MatchMask"/>.</summary>
        public UiModifiers Modifiers;

        /// <summary>
        /// The scheme id of the bind. Id 0 is the default scheme, and a binding with it belongs to each scheme. Each
        /// bind body writes the id that it takes, and the eleven current signatures write 0.
        /// </summary>
        public int Scheme;

        /// <summary>The scale that the read multiplies into the value of the binding.</summary>
        public float Scale;

        /// <summary>The index of the action.</summary>
        public int Action;
    }

    /// <summary>
    /// The create-info of a <see cref="UiActionMap"/>.
    /// </summary>
    public struct CreateInfo
    {
        /// <summary>
        /// The registry that holds the commands of the actions. The map does not own it.
        /// </summary>
        public required CommandRegistry Commands;
    }
}
