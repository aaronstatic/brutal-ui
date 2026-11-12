using System;
using Brutal.Numerics;
using BrutalUi.Core;

namespace BrutalUi.Framework;

public sealed partial class UiActionMap
{
    /// <summary>
    /// Gives the buttons that are down now to the map, and invokes the action of each new press.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Call it one time in each frame, after the poll and before <see cref="UiContext.Update(float)"/>. A press is a
    /// bit that is set now and was clear at the last push. A held button invokes nothing again. Two new presses in one
    /// push invoke in the order of the bind. The method works with no <see cref="Activate"/>. A new map starts with no
    /// button, so a button that is held at the first push counts as a press. The method stores the buttons before it
    /// calls a command, so a command that throws does not fire the same press again at the next push. The method
    /// allocates nothing. An exception of a command leaves the method.
    /// </para>
    /// <para>
    /// The method works on each map. An active map also reads the pad of <see cref="GamepadSlot"/> in
    /// <see cref="UiContext.Update(float)"/>, so a game must not push the same pad into an active map (Decision 9 of
    /// the input epic): the push would invoke each press a second time.
    /// </para>
    /// <para>
    /// The lock rule: a locked map takes no binding of another scheme, so a push invokes no command of that binding. An
    /// unlocked map keeps the body of the input-actions feature: it takes every binding, whatever its tag. See
    /// <see cref="SetScheme(int)"/>.
    /// </para>
    /// <para>
    /// The listen rule: an open listen stores the buttons and returns 0, so the push runs no command and queues no edge
    /// (Q9). The method writes <c>_gamepad</c> before it returns, so an edge that a suppressed frame reads is lost and
    /// not queued: a button that the player held through the listen fires nothing when the listen closes. The frame
    /// read of the same frame still runs the device track and <see cref="OnSchemeChanged"/> (the keep-running rule). The
    /// push allocates nothing.
    /// </para>
    /// </remarks>
    /// <param name="buttons">The buttons that are down now.</param>
    /// <returns>The count of commands that the registry ran.</returns>
    public int PushGamepad(UiGamepadButtons buttons)
    {
        if (_rebindOpen)
        {
            _gamepad = buttons;
            return 0;
        }

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
            if (binding.Kind == InputKind.GamepadButton && SchemeAllows(binding)
                && ((int)pressed & binding.Code) != 0 && Invoke(binding.Action))
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
    /// An unknown action invokes nothing. An action with no command invokes nothing and gives
    /// <see langword="false"/>, and the method calls no registry. A command that the registry does not hold calls
    /// <see cref="CommandRegistry.UnknownCommand"/>. The method allocates nothing and throws nothing of its own. An
    /// exception of the command leaves the method.
    /// <para>
    /// The listen rule: an open listen returns <see langword="false"/> before the name read, so the call runs no command
    /// and reads no action (Q9). The values of the five queries keep the state of the last read frame, and a suppressed
    /// frame emits no release edge, because the frame read runs no finish pass. The device track and
    /// <see cref="OnSchemeChanged"/> keep running. The call allocates nothing.
    /// </para>
    /// </remarks>
    /// <param name="action">The UTF-8 name of the action.</param>
    /// <returns>
    /// <see langword="true"/> when the map holds the action, the action has a command, and the registry ran it. Else
    /// <see langword="false"/>.
    /// </returns>
    public bool InvokeAction(ReadOnlySpan<byte> action)
    {
        if (_rebindOpen)
        {
            return false;
        }

        int index = IndexOfAction(action);
        return index >= 0 && Invoke(index);
    }

    /// <summary>
    /// Resolves a key with modifiers and invokes the command of its action.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The method is the raw resolve: it works with no <see cref="Activate"/> and has no repeat rule. The first binding
    /// in the order of the bind with the same key and the same modifiers wins (the modifier rule). The method allocates
    /// nothing and throws nothing of its own. A binding whose action has no command gives
    /// <see langword="false"/>, and the method calls no registry. An exception of the command leaves the method.
    /// </para>
    /// <para>
    /// The lock rule: the method takes the lock of the map, so a binding of another scheme gives no match. With no lock,
    /// each binding can match, whatever its tag. See <see cref="SetScheme(int)"/>.
    /// </para>
    /// <para>
    /// The listen rule: an open listen returns <see langword="false"/> as its first step, so the resolve runs no command
    /// and reads no binding (Q9). The method queues nothing: the press is lost, not kept for a later frame. Each query
    /// keeps the value and the flags of the last read frame, and a suppressed frame emits no release edge. The device
    /// track and <see cref="OnSchemeChanged"/> keep running. The call allocates nothing.
    /// </para>
    /// </remarks>
    /// <param name="key">The GLFW key code.</param>
    /// <param name="modifiers">The modifier keys that are down. Caps Lock and Num Lock do not count.</param>
    /// <returns>
    /// <see langword="true"/> when a binding matches, its action has a command, and the registry ran it. Else
    /// <see langword="false"/>.
    /// </returns>
    public bool PressKey(int key, UiModifiers modifiers)
    {
        if (_rebindOpen)
        {
            return false;
        }

        int index = FindAllowed(InputKind.Key, key, modifiers);
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
    /// <para>
    /// The method is the raw resolve: it works with no <see cref="Activate"/>, and it does not check if the press hit
    /// an element. The first mouse binding in the order of the bind with the same button and the same modifiers wins
    /// (the modifier rule). A key binding never matches. The method allocates nothing and throws nothing of its own. A
    /// binding whose action has no command gives <see langword="false"/>, and the method calls no registry. An
    /// exception of the command leaves the method.
    /// </para>
    /// <para>
    /// The lock rule: the method takes the lock of the map, so a binding of another scheme gives no match. With no lock,
    /// each binding can match, whatever its tag. See <see cref="SetScheme(int)"/>.
    /// </para>
    /// <para>
    /// The listen rule: an open listen returns <see langword="false"/> as its first step, so the resolve runs no command
    /// and reads no binding (Q9). The method queues nothing: the press is lost, not kept for a later frame. Each query
    /// keeps the value and the flags of the last read frame, and a suppressed frame emits no release edge. The device
    /// track and <see cref="OnSchemeChanged"/> keep running. The call allocates nothing.
    /// </para>
    /// </remarks>
    /// <param name="button">The mouse button.</param>
    /// <param name="modifiers">The modifier keys that are down. Caps Lock and Num Lock do not count.</param>
    /// <returns>
    /// <see langword="true"/> when a binding matches, its action has a command, and the registry ran it. Else
    /// <see langword="false"/>.
    /// </returns>
    public bool PressMouseButton(PointerButton button, UiModifiers modifiers)
    {
        if (_rebindOpen)
        {
            return false;
        }

        int index = FindAllowed(InputKind.MouseButton, (int)button, modifiers);
        if (index < 0)
        {
            return false;
        }

        return Invoke(_bindings[index].Action);
    }

    /// <summary>The slot handler: the context calls it for an input that no element took.</summary>
    /// <remarks>
    /// The handler keeps the text guard and the repeat rule, then calls <see cref="PressKey"/> or
    /// <see cref="PressMouseButton"/>, so the lock of <see cref="SetScheme(int)"/> applies to this path too: a binding of
    /// another scheme runs no command.
    /// <para>
    /// The listen rule: an open listen keeps this body as it is, and the two calls it makes give
    /// <see langword="false"/>, so the drain runs no command in a listening frame (Q9). The guard of a focused
    /// <see cref="TextField"/> and the repeat rule still run first. The path queues nothing: a press that the context
    /// gives the map in a suppressed frame is lost. The device track and <see cref="OnSchemeChanged"/> keep running. The
    /// path allocates nothing.
    /// </para>
    /// </remarks>
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

    /// <summary>
    /// The read slot handler: <see cref="UiContext.Update(float)"/> calls it after the drain of each frame. The method
    /// reads <see cref="UiContext.GameInput"/>, so the routing gate applies to each key, mouse button, and pointer value
    /// (A1), and it reads the pad of <see cref="GamepadSlot"/>. It writes the value of each typed action and the pressed,
    /// held, and released flags of each Button action, and each value stays fixed until the next call. It invokes the
    /// command of each new gamepad button press, and never a key command or a mouse command.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The method starts with the test of <see cref="IsActive"/>: when the slot
    /// <see cref="UiContext.OnUnhandledInput"/> holds another handler or <see langword="null"/>, the pair is broken, and
    /// the method returns with no work and no invoke. The read pass follows the unhandled slot, so a game that takes that
    /// slot also stops the frame read.
    /// </para>
    /// <para>
    /// The method works in four passes: it clears the frame scratch of each action, it adds the read of each binding to
    /// its action in the order of the bind, it finishes each action, and it invokes the command of each new gamepad
    /// button press in the order of the bind (the gamepad rule). The finish of a Vector2 action clamps the length of
    /// the clamped sum to 1 and adds the free sum. The finish of an Axis action clamps the clamped sum to -1 to 1 and
    /// adds the free sum. A key binding and a mouse button binding read their flags only when the modifiers of the frame
    /// match the modifiers of the bind (the modifier rule). The finish of a Button action keeps the latch: it starts from
    /// the held flag of the last frame or from a new press, so a press and a release in one frame set both the pressed
    /// flag and the released flag, and leave the held flag <see langword="false"/>. The invoke pass stops when a command
    /// deactivates the map or replaces the read slot. The method copies the two arrays and the two counts to locals
    /// before it scans, so a command that adds an action or a binding takes effect at the next frame (the re-entry
    /// rule).
    /// </para>
    /// <para>
    /// The device track: the read starts one device local from <c>_activeScheme</c>, and each case that gave a value
    /// writes the id of its device. A key, a mouse button, a key composite, the pointer delta, and a scroll write
    /// KeyboardMouse. A gamepad button, a stick, and an axis write Gamepad. The test reads the input, so a gamepad axis
    /// of exactly 0 gives no value, because the poll baked the deadzone in (A2), and a scale multiplies the value and
    /// never the device test: a scroll with a scale of 0 still reports KeyboardMouse. The read stores the
    /// local back before the finish pass, so the report carries from frame to frame: a frame with no input keeps the
    /// last report. The tag of a binding never sets the device, so a custom scheme name groups bindings only (Q1).
    /// </para>
    /// <para>
    /// The filter rule: the read starts both loops with <see cref="SchemeAllows"/> on the binding, so a locked map reads
    /// no binding of another scheme and invokes no command of one. A binding of the default scheme reads under every
    /// lock. With no lock, the map reads each binding, whatever its tag, so the device track can still move. A filtered
    /// binding gives no value, so it writes no device. See <see cref="SetScheme(int)"/>.
    /// </para>
    /// <para>
    /// The change call: the method ends with one report of the scheme that it now stands on, after the finish pass and
    /// after the invoke pass. The report compares the value that the map stands on with the value that the map last
    /// reported, and calls <see cref="OnSchemeChanged"/> only when the two differ, so one change gives one call and a
    /// frame with the same device gives none. Two flips inside one read give one call with the last value. A locked map
    /// reports the locked id. An early return of the pair test at the top of the method calls nothing, because the map
    /// is no longer active on the context. The handler is code inside <see cref="UiContext.Update(float)"/>, so a bind
    /// or a scheme change in it takes effect at the next frame.
    /// </para>
    /// <para>
    /// The listen branch: when a listen is open, the method calls <see cref="RebindScan"/> right after the pair test and
    /// returns, before the four passes. The scan reads <see cref="UiContext.Input"/>, the raw view, so a rebind works
    /// while a widget holds focus or a pickable root covers the window (Q1). The frame then writes no value, sums no
    /// binding, and runs no command (Q9), because the four passes do not run. The return is what makes a commit frame
    /// safe: the value loop reads the copy of the binding array that the method takes at its top, so the frame that
    /// lands a new binding must not run that loop, and the press that landed the commit drives nothing. A suppressed
    /// frame also runs no finish pass, so each action keeps the value and the flags of the last read frame and emits no
    /// release edge; a key that the player released during the listen gives one release on the first read frame after
    /// it. The scan ends with the device store and <see cref="ReportSchemeChange"/>, so the device track,
    /// <see cref="ActiveScheme"/>, and <see cref="OnSchemeChanged"/> keep running in a listening frame. The branch costs
    /// one bool test and gives 0 bytes.
    /// </para>
    /// <para>
    /// The method allocates nothing and throws nothing of its own. An exception of a command of the invoke pass leaves
    /// the method, and the map keeps the values of the frame, because the finish runs before the pass. An exception of
    /// the handler of <see cref="OnSchemeChanged"/> leaves the method the same way, and the map keeps the values and the
    /// report of the frame.
    /// </para>
    /// </remarks>
    /// <param name="context">The context that calls the handler.</param>
    private void ReadFrame(UiContext context)
    {
        if (context.OnUnhandledInput != _onInput)
        {
            return;
        }

        if (_rebindOpen)
        {
            RebindScan(context);
            return;
        }

        ref readonly UiInputState input = ref context.GameInput;
        ref readonly UiGamepadState pad = ref input.Gamepad(_gamepadSlot);
        UiModifiers mods = input.Keys.Modifiers & MatchMask;
        UiGamepadButtons pressed = pad.Pressed;
        ActionEntry[] actions = _actions;
        int actionCount = _actionCount;
        InputBinding[] bindings = _bindings;
        int bindingCount = _bindingCount;
        int kb = _keyboardScheme;
        int gp = _gamepadScheme;
        int device = _activeScheme;

        for (int i = 0; i < actionCount; i++)
        {
            ref ActionEntry a = ref actions[i];
            a.Sum = default;
            a.Free = default;
            a.Down = false;
            a.Press = false;
        }

        for (int i = 0; i < bindingCount; i++)
        {
            ref readonly InputBinding binding = ref bindings[i];
            if (!SchemeAllows(binding))
            {
                continue;
            }

            switch (binding.Kind)
            {
                case InputKind.Key:
                {
                    if (mods == binding.Modifiers)
                    {
                        ref readonly UiKeyState keys = ref input.Keys;
                        ref ActionEntry a = ref actions[binding.Action];
                        bool down = keys.IsDown(binding.Code);
                        bool press = keys.WasPressed(binding.Code);
                        a.Down |= down;
                        a.Press |= press;
                        if (down || press)
                        {
                            device = kb;
                        }
                    }

                    break;
                }

                case InputKind.MouseButton:
                {
                    if (mods == binding.Modifiers)
                    {
                        ref readonly UiPointerButtonState buttons = ref input.Buttons;
                        PointerButton button = (PointerButton)binding.Code;
                        ref ActionEntry a = ref actions[binding.Action];
                        bool down = buttons.IsDown(button);
                        bool press = buttons.WasPressed(button);
                        a.Down |= down;
                        a.Press |= press;
                        if (down || press)
                        {
                            device = kb;
                        }
                    }

                    break;
                }

                case InputKind.GamepadButton:
                {
                    ref ActionEntry a = ref actions[binding.Action];
                    bool down = ((int)pad.Buttons & binding.Code) != 0;
                    bool press = ((int)pressed & binding.Code) != 0;
                    a.Down |= down;
                    a.Press |= press;
                    if (down || press)
                    {
                        device = gp;
                    }

                    break;
                }

                case InputKind.KeyVector:
                {
                    ref readonly UiKeyState keys = ref input.Keys;
                    float x = DownBit(in keys, binding.Code3) - DownBit(in keys, binding.Code1);
                    float y = DownBit(in keys, binding.Code) - DownBit(in keys, binding.Code2);
                    ref ActionEntry a = ref actions[binding.Action];
                    a.Sum = a.Sum + new float2(x, y);
                    if (x != 0f || y != 0f)
                    {
                        device = kb;
                    }

                    break;
                }

                case InputKind.KeyAxis:
                {
                    ref readonly UiKeyState keys = ref input.Keys;
                    float value = DownBit(in keys, binding.Code1) - DownBit(in keys, binding.Code);
                    ref ActionEntry a = ref actions[binding.Action];
                    a.Sum = a.Sum + new float2(value, 0f);
                    if (value != 0f)
                    {
                        device = kb;
                    }

                    break;
                }

                case InputKind.GamepadStick:
                {
                    float2 stick = binding.Code == (int)UiGamepadStick.Right ? pad.RightStick : pad.LeftStick;
                    ref ActionEntry a = ref actions[binding.Action];
                    a.Sum = a.Sum + new float2(stick.X, -stick.Y);
                    if (stick.X != 0f || stick.Y != 0f)
                    {
                        device = gp;
                    }

                    break;
                }

                case InputKind.GamepadAxis:
                {
                    float axis = pad.Axis(binding.Code);
                    float value = axis * binding.Scale;
                    if (binding.Code == (int)UiGamepadAxis.LeftStickY || binding.Code == (int)UiGamepadAxis.RightStickY)
                    {
                        value = -value;
                    }

                    ref ActionEntry a = ref actions[binding.Action];
                    a.Sum = a.Sum + new float2(value, 0f);
                    if (axis != 0f)
                    {
                        device = gp;
                    }

                    break;
                }

                case InputKind.MouseDelta:
                {
                    ref readonly UiPointerState pointer = ref input.Pointer;
                    float2 delta = pointer.Delta;
                    ref ActionEntry a = ref actions[binding.Action];
                    a.Free = a.Free + new float2(delta.X * binding.Scale, delta.Y * binding.Scale);
                    if (delta.X != 0f || delta.Y != 0f)
                    {
                        device = kb;
                    }

                    break;
                }

                case InputKind.Scroll:
                {
                    float delta = input.Pointer.ScrollDelta.Y;
                    ref ActionEntry a = ref actions[binding.Action];
                    a.Free = a.Free + new float2(delta * binding.Scale, 0f);
                    if (delta != 0f)
                    {
                        device = kb;
                    }

                    break;
                }
            }
        }

        _activeScheme = device;

        for (int i = 0; i < actionCount; i++)
        {
            ref ActionEntry a = ref actions[i];
            if (a.Type == UiActionType.Vector2)
            {
                a.Value = ClampLength(a.Sum) + a.Free;
            }
            else if (a.Type == UiActionType.Axis)
            {
                a.Value = new float2(ClampUnit(a.Sum.X) + a.Free.X, 0f);
            }
            else
            {
                bool oldHeld = a.Held;
                bool start = oldHeld || a.Press;
                a.Pressed = a.Press;
                a.Held = start && a.Down;
                a.Released = start && !a.Held;
            }
        }

        if (pressed != UiGamepadButtons.None)
        {
            for (int i = 0; i < bindingCount; i++)
            {
                ref readonly InputBinding binding = ref bindings[i];
                if (!SchemeAllows(binding) || binding.Kind != InputKind.GamepadButton
                    || ((int)pressed & binding.Code) == 0)
                {
                    continue;
                }

                if (context.OnInputRead != _onRead)
                {
                    break;
                }

                Invoke(binding.Action);
            }
        }

        ReportSchemeChange();
    }

    /// <summary>Turns one key of the frame into 1 or 0 for the sum of a composite.</summary>
    /// <param name="keys">The key part of the frame state.</param>
    /// <param name="code">The GLFW key code of the key.</param>
    /// <returns><c>1f</c> when the key is down. Else <c>0f</c>.</returns>
    private static float DownBit(in UiKeyState keys, int code) => keys.IsDown(code) ? 1f : 0f;

    /// <summary>Clamps the length of a vector to 1. A vector with a length at or below 1 keeps its parts.</summary>
    /// <param name="value">The sum to clamp.</param>
    /// <returns>The clamped vector.</returns>
    private static float2 ClampLength(float2 value)
    {
        float length = MathF.Sqrt(value.X * value.X + value.Y * value.Y);
        if (length > 1f)
        {
            float scale = 1f / length;
            return new float2(value.X * scale, value.Y * scale);
        }

        return value;
    }

    /// <summary>Clamps a float to -1 to 1. A value inside the range keeps its value.</summary>
    /// <param name="value">The sum to clamp.</param>
    /// <returns>The clamped value.</returns>
    private static float ClampUnit(float value) => value < -1f ? -1f : value > 1f ? 1f : value;

    /// <summary>
    /// Invokes the command of an action through the registry. The method copies the command name to a local before the
    /// call, so a command that adds an action does not break the call (the re-entry rule). An action with no command
    /// invokes nothing, and the method calls no registry.
    /// </summary>
    /// <param name="action">The index of the action.</param>
    /// <returns><see langword="true"/> when the action has a command and the registry holds it and ran it.</returns>
    private bool Invoke(int action)
    {
        byte[] command = _actions[action].Command;
        return command.Length != 0 && Commands.Invoke(command);
    }
}
