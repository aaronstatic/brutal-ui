using System;
using System.Numerics;
using BrutalUi.Core;

namespace BrutalUi.Framework;

/// <summary>
/// A map from an input to a named action, and from an action to a command of a <see cref="CommandRegistry"/>.
/// </summary>
/// <remarks>
/// <para>
/// An action maps a UTF-8 name to a command name. A binding maps a key with modifiers, a mouse button with modifiers,
/// or a gamepad button to an action. The names compare
/// by their bytes. <see cref="UiActions"/> holds the standard names, and a game can use other names.
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
/// The slot rule: <see cref="Activate"/> writes the slot <see cref="UiContext.OnUnhandledInput"/>. The context holds
/// one slot, so one map is active on a context (A8). A map is active on one context at most: <see cref="Activate"/> on
/// a second context clears the slot of the first context. The context then gives the map each key down that no element
/// took. A key repeat invokes nothing. <see cref="PressKey"/>, <see cref="PressMouseButton"/>, and
/// <see cref="InvokeAction"/> work with no <see cref="Activate"/>.
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
/// The build rules: <see cref="AddAction"/> rejects an empty name and an action name that the map holds. Each bind
/// rejects an action that the map does not hold, and an input that the map binds already (the same kind, code, and
/// modifiers). <see cref="BindKey"/> rejects a key below 0, <see cref="BindMouseButton"/> rejects a button below 0,
/// and <see cref="BindGamepadButton"/> rejects <see cref="UiGamepadButtons.None"/> and a value with more than one bit.
/// A rejected call changes no count.
/// </para>
/// <para>
/// The re-entry rule: each resolve copies the binding array and the count before it scans, and each invoke copies the
/// command name before the call. A command that adds an action or a binding thus takes effect at the next input.
/// </para>
/// <para>
/// The error rule: the resolve methods throw nothing of their own. An exception of a command leaves
/// <see cref="PressKey"/>, <see cref="PressMouseButton"/>, <see cref="PushGamepad"/>, or
/// <see cref="UiContext.Update(float)"/>, and the map stays usable. <see cref="PushGamepad"/> stores the buttons
/// before it calls a command, so a held button does not fire again after an exception.
/// </para>
/// <para>
/// The thread rule: the map runs on the UI thread. It uses no lock.
/// </para>
/// </remarks>
public sealed class UiActionMap
{
    /// <summary>The first size of each array.</summary>
    private const int InitialCapacity = 8;

    /// <summary>The modifier keys that a binding compares. Caps Lock and Num Lock do not count.</summary>
    private const UiModifiers MatchMask = UiModifiers.Shift | UiModifiers.Control | UiModifiers.Alt | UiModifiers.Super;

    /// <summary>The first key code that is not a text key: GLFW_KEY_ESCAPE (256). Each code below it is a text key.</summary>
    private const int FirstNonTextKey = 256;

    /// <summary>The first keypad key: GLFW_KEY_KP_0 (320).</summary>
    private const int KeypadFirstKey = 320;

    /// <summary>The last keypad key: GLFW_KEY_KP_EQUAL (336).</summary>
    private const int KeypadLastKey = 336;

    /// <summary>The handler that <see cref="Activate"/> writes into the slot. The constructor creates it one time.</summary>
    private readonly UiUnhandledInputHandler _onInput;

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
    }

    /// <summary>Gets the registry that holds the commands of the actions.</summary>
    public CommandRegistry Commands { get; }

    /// <summary>Gets the count of actions.</summary>
    public int ActionCount => _actionCount;

    /// <summary>Gets the count of bindings.</summary>
    public int BindingCount => _bindingCount;

    /// <summary>
    /// Adds an action that invokes a command.
    /// </summary>
    /// <remarks>
    /// The map copies both names. The add can grow an array and allocate, so add at build time. The registry needs no
    /// command of the name at the time of the add: the map gives the name to the registry at each invoke. A rejected
    /// call changes nothing.
    /// </remarks>
    /// <param name="action">The UTF-8 name of the action, for example <see cref="UiActions.Cancel"/>.</param>
    /// <param name="command">The UTF-8 name of the command in <see cref="Commands"/>.</param>
    /// <exception cref="ArgumentException">
    /// <paramref name="action"/> or <paramref name="command"/> is empty, or the map holds the action name.
    /// </exception>
    public void AddAction(ReadOnlySpan<byte> action, ReadOnlySpan<byte> command)
    {
        if (action.IsEmpty)
        {
            throw new ArgumentException("The name of an action must not be empty.", nameof(action));
        }

        if (command.IsEmpty)
        {
            throw new ArgumentException("The command name of an action must not be empty.", nameof(command));
        }

        if (IndexOfAction(action) >= 0)
        {
            throw new ArgumentException("The map already holds an action with this name.", nameof(action));
        }

        if (_actionCount == _actions.Length)
        {
            Array.Resize(ref _actions, _actions.Length == 0 ? InitialCapacity : _actions.Length * 2);
        }

        _actions[_actionCount] = new ActionEntry { Name = action.ToArray(), Command = command.ToArray() };
        _actionCount++;
    }

    /// <summary>
    /// Binds a key with modifiers to an action.
    /// </summary>
    /// <remarks>
    /// The map stores the Shift, Control, Alt, and Super bits of <paramref name="modifiers"/> only (the modifier rule).
    /// The bind can grow an array and allocate, so bind at build time.
    /// </remarks>
    /// <param name="action">The UTF-8 name of an action that the map holds.</param>
    /// <param name="key">The GLFW key code, for example 256 for Escape.</param>
    /// <param name="modifiers">The modifier keys that must be down.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="key"/> is below 0.</exception>
    /// <exception cref="ArgumentException">
    /// The map holds no action of the name <paramref name="action"/>, or the map binds the key with these modifiers
    /// already.
    /// </exception>
    public void BindKey(ReadOnlySpan<byte> action, int key, UiModifiers modifiers = UiModifiers.None)
    {
        if (key < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(key), "The key code must not be below 0.");
        }

        int index = IndexOfAction(action);
        if (index < 0)
        {
            throw new ArgumentException("The map holds no action with this name.", nameof(action));
        }

        if (Find(InputKind.Key, key, modifiers) >= 0)
        {
            throw new ArgumentException("The map already binds this key with these modifiers.", nameof(action));
        }

        if (_bindingCount == _bindings.Length)
        {
            Array.Resize(ref _bindings, _bindings.Length == 0 ? InitialCapacity : _bindings.Length * 2);
        }

        _bindings[_bindingCount] = new InputBinding
        {
            Kind = InputKind.Key,
            Code = key,
            Modifiers = modifiers & MatchMask,
            Action = index,
        };
        _bindingCount++;
    }

    /// <summary>
    /// Binds a mouse button with modifiers to an action.
    /// </summary>
    /// <remarks>
    /// The map stores the GLFW value of <paramref name="button"/> as the code of the binding, and the Shift, Control,
    /// Alt, and Super bits of <paramref name="modifiers"/> only (the modifier rule). A mouse binding never matches a
    /// key, also with the same code. The context gives the map a press over no element only (the mouse rule). The bind
    /// can grow an array and allocate, so bind at build time.
    /// </remarks>
    /// <param name="action">The UTF-8 name of an action that the map holds.</param>
    /// <param name="button">The mouse button.</param>
    /// <param name="modifiers">The modifier keys that must be down.</param>
    /// <exception cref="ArgumentOutOfRangeException">The value of <paramref name="button"/> is below 0.</exception>
    /// <exception cref="ArgumentException">
    /// The map holds no action of the name <paramref name="action"/>, or the map binds the button with these modifiers
    /// already.
    /// </exception>
    public void BindMouseButton(ReadOnlySpan<byte> action, PointerButton button, UiModifiers modifiers = UiModifiers.None)
    {
        if ((int)button < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(button), "The mouse button must not be below 0.");
        }

        int index = IndexOfAction(action);
        if (index < 0)
        {
            throw new ArgumentException("The map holds no action with this name.", nameof(action));
        }

        if (Find(InputKind.MouseButton, (int)button, modifiers) >= 0)
        {
            throw new ArgumentException("The map already binds this mouse button with these modifiers.", nameof(action));
        }

        if (_bindingCount == _bindings.Length)
        {
            Array.Resize(ref _bindings, _bindings.Length == 0 ? InitialCapacity : _bindings.Length * 2);
        }

        _bindings[_bindingCount] = new InputBinding
        {
            Kind = InputKind.MouseButton,
            Code = (int)button,
            Modifiers = modifiers & MatchMask,
            Action = index,
        };
        _bindingCount++;
    }

    /// <summary>
    /// Binds a gamepad button to an action.
    /// </summary>
    /// <remarks>
    /// The bind can grow an array and allocate, so bind at build time. The map stores the value of
    /// <paramref name="button"/> as the code of the binding, and a press of the button invokes the action (see
    /// <see cref="PushGamepad"/>). The button must be one button: <see cref="UiGamepadButtons.None"/> and a value with
    /// more than one bit fail.
    /// </remarks>
    /// <param name="action">The UTF-8 name of an action that the map holds.</param>
    /// <param name="button">The gamepad button.</param>
    /// <exception cref="ArgumentException">
    /// The map holds no action of the name <paramref name="action"/>, <paramref name="button"/> is not one button, or
    /// the map binds the button already.
    /// </exception>
    public void BindGamepadButton(ReadOnlySpan<byte> action, UiGamepadButtons button)
    {
        if (BitOperations.PopCount((uint)button) != 1)
        {
            throw new ArgumentException("The gamepad button must be exactly one button.", nameof(button));
        }

        int index = IndexOfAction(action);
        if (index < 0)
        {
            throw new ArgumentException("The map holds no action with this name.", nameof(action));
        }

        if (Find(InputKind.GamepadButton, (int)button, UiModifiers.None) >= 0)
        {
            throw new ArgumentException("The map already binds this gamepad button.", nameof(action));
        }

        if (_bindingCount == _bindings.Length)
        {
            Array.Resize(ref _bindings, _bindings.Length == 0 ? InitialCapacity : _bindings.Length * 2);
        }

        _bindings[_bindingCount] = new InputBinding
        {
            Kind = InputKind.GamepadButton,
            Code = (int)button,
            Modifiers = UiModifiers.None,
            Action = index,
        };
        _bindingCount++;
    }

    /// <summary>
    /// Gives the buttons that are down now to the map, and invokes the action of each new press.
    /// </summary>
    /// <remarks>
    /// Call it one time in each frame, after the poll and before <see cref="UiContext.Update(float)"/>. A press is a
    /// bit that is set now and was clear at the last push. A held button invokes nothing again. Two new presses in one
    /// push invoke in the order of the bind. The method works with no <see cref="Activate"/>. A new map starts with no
    /// button, so a button that is held at the first push counts as a press. The method stores the buttons before it
    /// calls a command, so a command that throws does not fire the same press again at the next push. The method
    /// allocates nothing. An exception of a command leaves the method.
    /// </remarks>
    /// <param name="buttons">The buttons that are down now.</param>
    /// <returns>The count of commands that the registry ran.</returns>
    public int PushGamepad(UiGamepadButtons buttons)
    {
        UiGamepadButtons pressed = buttons & ~_gamepad;
        _gamepad = buttons;
        if (pressed == UiGamepadButtons.None)
        {
            return 0;
        }

        InputBinding[] bindings = _bindings;
        int count = _bindingCount;
        int invoked = 0;
        for (int i = 0; i < count; i++)
        {
            ref readonly InputBinding binding = ref bindings[i];
            if (binding.Kind == InputKind.GamepadButton && ((int)pressed & binding.Code) != 0 && Invoke(binding.Action))
            {
                invoked++;
            }
        }

        return invoked;
    }

    /// <summary>
    /// Invokes the command of an action.
    /// </summary>
    /// <remarks>
    /// An unknown action invokes nothing. A command that the registry does not hold calls
    /// <see cref="CommandRegistry.UnknownCommand"/>. The method allocates nothing and throws nothing of its own. An
    /// exception of the command leaves the method.
    /// </remarks>
    /// <param name="action">The UTF-8 name of the action.</param>
    /// <returns>
    /// <see langword="true"/> when the map holds the action and the registry ran its command. Else
    /// <see langword="false"/>.
    /// </returns>
    public bool InvokeAction(ReadOnlySpan<byte> action)
    {
        int index = IndexOfAction(action);
        return index >= 0 && Invoke(index);
    }

    /// <summary>
    /// Resolves a key with modifiers and invokes the command of its action.
    /// </summary>
    /// <remarks>
    /// The method is the raw resolve: it works with no <see cref="Activate"/> and has no repeat rule. The first binding
    /// in the order of the bind with the same key and the same modifiers wins (the modifier rule). The method allocates
    /// nothing and throws nothing of its own. An exception of the command leaves the method.
    /// </remarks>
    /// <param name="key">The GLFW key code.</param>
    /// <param name="modifiers">The modifier keys that are down. Caps Lock and Num Lock do not count.</param>
    /// <returns>
    /// <see langword="true"/> when a binding matches and the registry ran the command of its action. Else
    /// <see langword="false"/>.
    /// </returns>
    public bool PressKey(int key, UiModifiers modifiers)
    {
        int index = Find(InputKind.Key, key, modifiers);
        if (index < 0)
        {
            return false;
        }

        return Invoke(_bindings[index].Action);
    }

    /// <summary>
    /// Resolves a mouse button with modifiers and invokes the command of its action.
    /// </summary>
    /// <remarks>
    /// The method is the raw resolve: it works with no <see cref="Activate"/>, and it does not check if the press hit
    /// an element. The first mouse binding in the order of the bind with the same button and the same modifiers wins
    /// (the modifier rule). A key binding never matches. The method allocates nothing and throws nothing of its own. An
    /// exception of the command leaves the method.
    /// </remarks>
    /// <param name="button">The mouse button.</param>
    /// <param name="modifiers">The modifier keys that are down. Caps Lock and Num Lock do not count.</param>
    /// <returns>
    /// <see langword="true"/> when a binding matches and the registry ran the command of its action. Else
    /// <see langword="false"/>.
    /// </returns>
    public bool PressMouseButton(PointerButton button, UiModifiers modifiers)
    {
        int index = Find(InputKind.MouseButton, (int)button, modifiers);
        if (index < 0)
        {
            return false;
        }

        return Invoke(_bindings[index].Action);
    }

    /// <summary>
    /// Makes this map the active map of a context.
    /// </summary>
    /// <remarks>
    /// The method writes the handler of the map into <see cref="UiContext.OnUnhandledInput"/>. The write replaces each
    /// delegate in the slot, also a delegate of the game, so one map is active on a context (A8). A second
    /// <see cref="Activate"/> on the same context replaces the first map. The first map then keeps its state and
    /// <see cref="IsActive"/> gives <see langword="false"/> for it. A mode list of maps waits for panels-and-modes.
    /// The handler is one delegate that the constructor creates, so a call allocates nothing. Call it at build time or
    /// at a change of mode. A map is active on one context at most, and the last <see cref="Activate"/> wins: when the
    /// map is active on another context, the method first clears the slot of that context. It clears the slot only when
    /// the slot holds the handler of this map. A second <see cref="Activate"/> on the same context changes nothing else.
    /// </remarks>
    /// <param name="context">The context.</param>
    /// <exception cref="ArgumentNullException"><paramref name="context"/> is <see langword="null"/>.</exception>
    public void Activate(UiContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (_context is { } old && !ReferenceEquals(old, context) && old.OnUnhandledInput == _onInput)
        {
            old.OnUnhandledInput = null;
        }

        context.OnUnhandledInput = _onInput;
        _context = context;
    }

    /// <summary>
    /// Gets a value that tells if this map is the active map of its context.
    /// </summary>
    /// <remarks>
    /// The value is <see langword="true"/> when the slot <see cref="UiContext.OnUnhandledInput"/> of the context of the
    /// last <see cref="Activate"/> holds the handler of this map. A later map that replaced this map gives
    /// <see langword="false"/>.
    /// </remarks>
    public bool IsActive => _context is { } c && c.OnUnhandledInput == _onInput;

    /// <summary>
    /// Clears the slot of the context when this map is the active map.
    /// </summary>
    /// <remarks>
    /// A map that another map replaced clears nothing, so the active map stays active. A call on a map that was never
    /// active does nothing. The method allocates nothing.
    /// </remarks>
    public void Deactivate()
    {
        if (IsActive)
        {
            _context!.OnUnhandledInput = null;
        }

        _context = null;
    }

    /// <summary>The slot handler: the context calls it for an input that no element took.</summary>
    /// <param name="type">The type of the input.</param>
    /// <param name="code">The GLFW key code of a key down, or the GLFW mouse button value of a press.</param>
    /// <param name="modifiers">The modifier keys that are down.</param>
    /// <param name="isRepeat"><see langword="true"/> for a key repeat.</param>
    private void HandleInput(UiEventType type, int code, UiModifiers modifiers, bool isRepeat)
    {
        if (isRepeat)
        {
            return;
        }

        if (type == UiEventType.KeyDown)
        {
            if (IsTypedByTextField(code, modifiers))
            {
                return;
            }

            PressKey(code, modifiers);
        }
        else if (type == UiEventType.PointerDown)
        {
            PressMouseButton((PointerButton)code, modifiers);
        }
    }

    /// <summary>
    /// Tests the text guard: a focused <see cref="TextField"/> takes the char of a text key, so the map skips the key.
    /// </summary>
    /// <param name="key">The GLFW key code.</param>
    /// <param name="modifiers">The modifier keys that are down.</param>
    /// <returns>
    /// <see langword="true"/> when a text field holds the focus, no Control, Alt, or Super key is down, and the key is a
    /// text key.
    /// </returns>
    private bool IsTypedByTextField(int key, UiModifiers modifiers)
    {
        if (_context?.FocusedElement is not TextField)
        {
            return false;
        }

        if ((modifiers & (UiModifiers.Control | UiModifiers.Alt | UiModifiers.Super)) != UiModifiers.None)
        {
            return false;
        }

        return key < FirstNonTextKey || (key >= KeypadFirstKey && key <= KeypadLastKey);
    }

    /// <summary>Finds the first binding of an input in the order of the bind.</summary>
    /// <param name="kind">The kind of the input.</param>
    /// <param name="code">The code of the input.</param>
    /// <param name="modifiers">The modifier keys that are down.</param>
    /// <returns>The index of the binding, or -1 when no binding matches.</returns>
    private int Find(InputKind kind, int code, UiModifiers modifiers)
    {
        InputBinding[] bindings = _bindings;
        int count = _bindingCount;
        UiModifiers match = modifiers & MatchMask;
        for (int i = 0; i < count; i++)
        {
            ref readonly InputBinding binding = ref bindings[i];
            if (binding.Kind == kind && binding.Code == code && binding.Modifiers == match)
            {
                return i;
            }
        }

        return -1;
    }

    /// <summary>Finds an action by its name.</summary>
    /// <param name="name">The UTF-8 name.</param>
    /// <returns>The index of the action, or -1 when no action matches.</returns>
    private int IndexOfAction(ReadOnlySpan<byte> name)
    {
        ActionEntry[] actions = _actions;
        int count = _actionCount;
        for (int i = 0; i < count; i++)
        {
            if (actions[i].Name.AsSpan().SequenceEqual(name))
            {
                return i;
            }
        }

        return -1;
    }

    /// <summary>
    /// Invokes the command of an action through the registry. The method copies the command name to a local before the
    /// call, so a command that adds an action does not break the call (the re-entry rule).
    /// </summary>
    /// <param name="action">The index of the action.</param>
    /// <returns><see langword="true"/> when the registry holds the command and ran it.</returns>
    private bool Invoke(int action)
    {
        byte[] command = _actions[action].Command;
        return Commands.Invoke(command);
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
    }

    /// <summary>One action.</summary>
    private struct ActionEntry
    {
        /// <summary>The copy of the action name.</summary>
        public byte[] Name;

        /// <summary>The copy of the command name.</summary>
        public byte[] Command;
    }

    /// <summary>One binding of an input to an action.</summary>
    private struct InputBinding
    {
        /// <summary>The kind of the input.</summary>
        public InputKind Kind;

        /// <summary>
        /// The code of the input: a GLFW key code for a key, a GLFW mouse button value for a mouse button, and the bit
        /// of a gamepad button.
        /// </summary>
        public int Code;

        /// <summary>The modifier keys of the bind, masked with <see cref="MatchMask"/>.</summary>
        public UiModifiers Modifiers;

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
