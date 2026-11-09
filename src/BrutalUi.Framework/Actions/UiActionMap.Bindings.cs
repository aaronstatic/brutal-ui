using System;
using System.Numerics;
using BrutalUi.Core;

namespace BrutalUi.Framework;

public sealed partial class UiActionMap
{
    /// <summary>
    /// Adds a <see cref="UiActionType.Button"/> action that invokes a command. A literal <c>default</c> second argument
    /// matches this overload and the typed overload, so pass the command span or the <see cref="UiActionType"/> value.
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

        _actions[_actionCount] = new ActionEntry
        {
            Name = action.ToArray(),
            Command = command.ToArray(),
            Type = UiActionType.Button,
        };
        _actionCount++;
    }

    /// <summary>
    /// Adds an action with a type and no command. A literal <c>default</c> second argument matches this overload and the
    /// command overload, so pass the <see cref="UiActionType"/> value or the command span. To add a Button with no
    /// command, pass <see cref="UiActionType.Button"/>.
    /// </summary>
    /// <remarks>
    /// The map copies the name and gives the action an empty command. A query of the type then reads the value of the
    /// action, for example <c>ReadVector</c> on a Vector2 action. An action with no command invokes nothing: a bind of a
    /// Button action with no command resolves and calls no command, and <see cref="InvokeAction"/> gives
    /// <see langword="false"/>. The add can grow an array and allocate, so add at build time. A rejected call changes
    /// nothing.
    /// </remarks>
    /// <param name="action">The UTF-8 name of the action, for example <see cref="UiActions.Cancel"/>.</param>
    /// <param name="type">The type of the action.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="type"/> is not one of the values of <see cref="UiActionType"/>. The parameter name is
    /// <c>type</c>.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// <paramref name="action"/> is empty, or the map holds the action name.
    /// </exception>
    public void AddAction(ReadOnlySpan<byte> action, UiActionType type)
    {
        if (action.IsEmpty)
        {
            throw new ArgumentException("The name of an action must not be empty.", nameof(action));
        }

        if (!Enum.IsDefined(type))
        {
            throw new ArgumentOutOfRangeException(nameof(type), "The type of an action must be one of the values of UiActionType.");
        }

        if (IndexOfAction(action) >= 0)
        {
            throw new ArgumentException("The map already holds an action with this name.", nameof(action));
        }

        if (_actionCount == _actions.Length)
        {
            Array.Resize(ref _actions, _actions.Length == 0 ? InitialCapacity : _actions.Length * 2);
        }

        _actions[_actionCount] = new ActionEntry { Name = action.ToArray(), Command = [], Type = type };
        _actionCount++;
    }

    /// <summary>
    /// Binds a key with modifiers to an action.
    /// </summary>
    /// <remarks>
    /// The map stores the Shift, Control, Alt, and Super bits of <paramref name="modifiers"/> only (the modifier rule).
    /// The frame read applies the rule each frame: when a modifier changes while a plain-key bind is held, the binding
    /// stops matching, the latch releases the action, and a fresh press is needed to hold it again. The bind needs a
    /// <see cref="UiActionType.Button"/> action (the type rule). The bind can grow an array and
    /// allocate, so bind at build time.
    /// </remarks>
    /// <param name="action">The UTF-8 name of an action that the map holds.</param>
    /// <param name="key">The GLFW key code, for example 256 for Escape.</param>
    /// <param name="modifiers">The modifier keys that must be down.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="key"/> is outside 0 to <see cref="UiKeyState.KeyCount"/> - 1. The parameter name is <c>key</c>.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// The map holds no action of the name <paramref name="action"/>, the action is not a
    /// <see cref="UiActionType.Button"/>, or the map binds the key with these modifiers already.
    /// </exception>
    public void BindKey(ReadOnlySpan<byte> action, int key, UiModifiers modifiers = UiModifiers.None)
    {
        if ((uint)key >= UiKeyState.KeyCount)
        {
            throw new ArgumentOutOfRangeException(nameof(key), "The key code must be a GLFW key code from 0 to UiKeyState.KeyCount - 1.");
        }

        int index = IndexOfAction(action);
        if (index < 0)
        {
            throw new ArgumentException("The map holds no action with this name.", nameof(action));
        }

        CheckActionType(index, UiActionType.Button);
        if (Find(InputKind.Key, key, modifiers) >= 0)
        {
            throw new ArgumentException("The map already binds this key with these modifiers.", nameof(action));
        }

        Append(new InputBinding
        {
            Kind = InputKind.Key,
            Code = key,
            Code1 = -1,
            Code2 = -1,
            Code3 = -1,
            Modifiers = modifiers & MatchMask,
            Scale = 1f,
            Action = index,
        });
    }

    /// <summary>
    /// Binds a mouse button with modifiers to an action.
    /// </summary>
    /// <remarks>
    /// The map stores the GLFW value of <paramref name="button"/> as the code of the binding, and the Shift, Control,
    /// Alt, and Super bits of <paramref name="modifiers"/> only (the modifier rule). The bind needs a
    /// <see cref="UiActionType.Button"/> action (the type rule). A mouse binding never matches a
    /// key, also with the same code. The context gives the map a press over no element only (the mouse rule). The bind
    /// can grow an array and allocate, so bind at build time.
    /// </remarks>
    /// <param name="action">The UTF-8 name of an action that the map holds.</param>
    /// <param name="button">The mouse button.</param>
    /// <param name="modifiers">The modifier keys that must be down.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// The value of <paramref name="button"/> is outside 0 to <see cref="UiPointerButtonState.ButtonCount"/> - 1. The
    /// parameter name is <c>button</c>.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// The map holds no action of the name <paramref name="action"/>, the action is not a
    /// <see cref="UiActionType.Button"/>, or the map binds the button with these modifiers already.
    /// </exception>
    public void BindMouseButton(ReadOnlySpan<byte> action, PointerButton button, UiModifiers modifiers = UiModifiers.None)
    {
        if ((int)button < 0 || (int)button >= UiPointerButtonState.ButtonCount)
        {
            throw new ArgumentOutOfRangeException(nameof(button), "The mouse button must be a GLFW button value from 0 to UiPointerButtonState.ButtonCount - 1.");
        }

        int index = IndexOfAction(action);
        if (index < 0)
        {
            throw new ArgumentException("The map holds no action with this name.", nameof(action));
        }

        CheckActionType(index, UiActionType.Button);
        if (Find(InputKind.MouseButton, (int)button, modifiers) >= 0)
        {
            throw new ArgumentException("The map already binds this mouse button with these modifiers.", nameof(action));
        }

        Append(new InputBinding
        {
            Kind = InputKind.MouseButton,
            Code = (int)button,
            Code1 = -1,
            Code2 = -1,
            Code3 = -1,
            Modifiers = modifiers & MatchMask,
            Scale = 1f,
            Action = index,
        });
    }

    /// <summary>
    /// Binds a gamepad button to an action.
    /// </summary>
    /// <remarks>
    /// The bind can grow an array and allocate, so bind at build time. The bind needs a
    /// <see cref="UiActionType.Button"/> action (the type rule). The map stores the value of
    /// <paramref name="button"/> as the code of the binding, and a press of the button invokes the action (see
    /// <see cref="PushGamepad"/>). The button must be one button: <see cref="UiGamepadButtons.None"/> and a value with
    /// more than one bit fail.
    /// </remarks>
    /// <param name="action">The UTF-8 name of an action that the map holds.</param>
    /// <param name="button">The gamepad button.</param>
    /// <exception cref="ArgumentException">
    /// The map holds no action of the name <paramref name="action"/>, <paramref name="button"/> is not one button, the
    /// action is not a <see cref="UiActionType.Button"/>, or the map binds the button already.
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

        CheckActionType(index, UiActionType.Button);
        if (Find(InputKind.GamepadButton, (int)button, UiModifiers.None) >= 0)
        {
            throw new ArgumentException("The map already binds this gamepad button.", nameof(action));
        }

        Append(new InputBinding
        {
            Kind = InputKind.GamepadButton,
            Code = (int)button,
            Code1 = -1,
            Code2 = -1,
            Code3 = -1,
            Modifiers = UiModifiers.None,
            Scale = 1f,
            Action = index,
        });
    }

    /// <summary>
    /// Binds four keys to a Vector2 action: the up key, the left key, the down key, and the right key.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The bind needs a <see cref="UiActionType.Vector2"/> action (the type rule). Each key gives its own direction: the
    /// right key gives 1 on the X axis, the left key gives -1, the up key gives 1 on the Y axis, and the down key gives
    /// -1. Up is the positive Y axis (Decision 6 of the input epic), so the value of a held up key reads (0, 1). Two
    /// opposite keys give 0 on that axis: the frame read sums both.
    /// </para>
    /// <para>
    /// The composite reads the four keys with no modifier check (Decision 7 of the input epic), so Shift+W still reads
    /// (0, 1). The binding stores no modifiers. The frame read sums the value of each binding of the action, then clamps
    /// the length of the sum to 1. Thus W and D give (0.707107, 0.707107), not (1, 1). The read of one held key needs no
    /// clamp, so its length stays 1.
    /// </para>
    /// <para>
    /// The bind can grow an array and allocate, so bind at build time. The map reads the keys of an active map in
    /// <see cref="UiContext.Update(float)"/>, through the slot <see cref="UiContext.OnInputRead"/>. A game that reads
    /// the value calls <c>ReadVector</c>.
    /// </para>
    /// </remarks>
    /// <param name="action">The UTF-8 name of a Vector2 action that the map holds.</param>
    /// <param name="up">The GLFW key code of the up key.</param>
    /// <param name="left">The GLFW key code of the left key.</param>
    /// <param name="down">The GLFW key code of the down key.</param>
    /// <param name="right">The GLFW key code of the right key.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="up"/>, <paramref name="left"/>, <paramref name="down"/>, or <paramref name="right"/> is outside 0
    /// to <see cref="UiKeyState.KeyCount"/> - 1. The parameter name is the name of that key.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// The map holds no action of the name <paramref name="action"/>, the action is not a
    /// <see cref="UiActionType.Vector2"/>, two keys have the same code, or the map binds these four keys, in any order.
    /// </exception>
    public void BindKeyVector(ReadOnlySpan<byte> action, int up, int left, int down, int right)
    {
        if ((uint)up >= UiKeyState.KeyCount)
        {
            throw new ArgumentOutOfRangeException(nameof(up), "The up key must be a GLFW key code from 0 to UiKeyState.KeyCount - 1.");
        }

        if ((uint)left >= UiKeyState.KeyCount)
        {
            throw new ArgumentOutOfRangeException(nameof(left), "The left key must be a GLFW key code from 0 to UiKeyState.KeyCount - 1.");
        }

        if ((uint)down >= UiKeyState.KeyCount)
        {
            throw new ArgumentOutOfRangeException(nameof(down), "The down key must be a GLFW key code from 0 to UiKeyState.KeyCount - 1.");
        }

        if ((uint)right >= UiKeyState.KeyCount)
        {
            throw new ArgumentOutOfRangeException(nameof(right), "The right key must be a GLFW key code from 0 to UiKeyState.KeyCount - 1.");
        }

        int index = IndexOfAction(action);
        if (index < 0)
        {
            throw new ArgumentException("The map holds no action with this name.", nameof(action));
        }

        CheckActionType(index, UiActionType.Vector2);
        if (left == up)
        {
            throw new ArgumentException("The left key has the code of the up key.", nameof(left));
        }

        if (down == up || down == left)
        {
            throw new ArgumentException("The down key has the code of an earlier key.", nameof(down));
        }

        if (right == up || right == left || right == down)
        {
            throw new ArgumentException("The right key has the code of an earlier key.", nameof(right));
        }

        if (FindValue(InputKind.KeyVector, up, left, down, right) >= 0)
        {
            throw new ArgumentException("The map already binds these four keys.", nameof(action));
        }

        Append(new InputBinding
        {
            Kind = InputKind.KeyVector,
            Code = up,
            Code1 = left,
            Code2 = down,
            Code3 = right,
            Modifiers = UiModifiers.None,
            Scale = 1f,
            Action = index,
        });
    }

    /// <summary>
    /// Binds one stick of a gamepad to a Vector2 action.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The bind needs a <see cref="UiActionType.Vector2"/> action (the type rule). The frame read of an active map reads
    /// <see cref="BrutalUi.Core.UiGamepadState.LeftStick"/> or
    /// <see cref="BrutalUi.Core.UiGamepadState.RightStick"/> of the pad that <see cref="GamepadSlot"/> selects. The map
    /// reads the values after the deadzone of <c>PollGamepads</c> and adds no deadzone of its own.
    /// </para>
    /// <para>
    /// The read flips the Y component of the stick so up is +Y (Decision 6), because GLFW gives -1 for up. The frame
    /// read then adds the stick to the sum of the action, and the Vector2 finish clamps the length of the clamped sum
    /// to 1.
    /// </para>
    /// <para>
    /// The bind can grow an array and allocate, so bind at build time. A game that reads the value calls
    /// <see cref="ReadVector"/>.
    /// </para>
    /// </remarks>
    /// <param name="action">The UTF-8 name of a Vector2 action that the map holds.</param>
    /// <param name="stick">The stick to read.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="stick"/> is not a value of <see cref="UiGamepadStick"/>. The parameter name is <c>stick</c>.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// The map holds no action of the name <paramref name="action"/>, the action is not a
    /// <see cref="UiActionType.Vector2"/>, or the map binds the stick already.
    /// </exception>
    public void BindGamepadStick(ReadOnlySpan<byte> action, UiGamepadStick stick)
    {
        if (!Enum.IsDefined(stick))
        {
            throw new ArgumentOutOfRangeException(nameof(stick), "The stick must be Left or Right of UiGamepadStick.");
        }

        int index = IndexOfAction(action);
        if (index < 0)
        {
            throw new ArgumentException("The map holds no action with this name.", nameof(action));
        }

        CheckActionType(index, UiActionType.Vector2);
        if (FindValue(InputKind.GamepadStick, (int)stick, -1, -1, -1) >= 0)
        {
            throw new ArgumentException("The map already binds this stick.", nameof(action));
        }

        Append(new InputBinding
        {
            Kind = InputKind.GamepadStick,
            Code = (int)stick,
            Code1 = -1,
            Code2 = -1,
            Code3 = -1,
            Modifiers = UiModifiers.None,
            Scale = 1f,
            Action = index,
        });
    }

    /// <summary>
    /// Binds one axis of a gamepad to an Axis action.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The bind needs an <see cref="UiActionType.Axis"/> action (the type rule). The frame read of an active map reads
    /// <see cref="BrutalUi.Core.UiGamepadState.Axis(int)"/> of the pad that <see cref="GamepadSlot"/> selects. A trigger
    /// reads 0 to 1, and a stick axis reads -1 to 1. The values come from the deadzone of <c>PollGamepads</c>, and the
    /// map adds no deadzone of its own.
    /// </para>
    /// <para>
    /// The read flips a Y stick axis so up is positive (Decision 6), because GLFW gives -1 for up. The read multiplies
    /// <paramref name="scale"/> into the value, so a negative scale turns a trigger into the negative half of a zoom
    /// axis. The frame read sums the value of each binding of the action, and the Axis finish clamps the sum to -1
    /// to 1.
    /// </para>
    /// <para>
    /// The bind can grow an array and allocate, so bind at build time. A game that reads the value calls
    /// <see cref="ReadAxis"/>.
    /// </para>
    /// </remarks>
    /// <param name="action">The UTF-8 name of an Axis action that the map holds.</param>
    /// <param name="axis">The axis to read.</param>
    /// <param name="scale">The value that the read multiplies into the axis. The default is 1.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="axis"/> casts to a value outside 0 to 5, so it is not a value of
    /// <see cref="BrutalUi.Core.UiGamepadAxis"/>. The parameter name is <c>axis</c>. Or <paramref name="scale"/> is not
    /// a finite <see cref="float"/> value. The parameter name is <c>scale</c>.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// The map holds no action of the name <paramref name="action"/>, the action is not an
    /// <see cref="UiActionType.Axis"/>, or the map binds the axis already.
    /// </exception>
    public void BindGamepadAxis(ReadOnlySpan<byte> action, UiGamepadAxis axis, float scale = 1f)
    {
        if (!Enum.IsDefined(axis))
        {
            throw new ArgumentOutOfRangeException(nameof(axis), "The axis must be a UiGamepadAxis value from 0 to 5.");
        }

        if (!float.IsFinite(scale))
        {
            throw new ArgumentOutOfRangeException(nameof(scale), "The scale must be a finite float value.");
        }

        int index = IndexOfAction(action);
        if (index < 0)
        {
            throw new ArgumentException("The map holds no action with this name.", nameof(action));
        }

        CheckActionType(index, UiActionType.Axis);
        if (FindValue(InputKind.GamepadAxis, (int)axis, -1, -1, -1) >= 0)
        {
            throw new ArgumentException("The map already binds this axis.", nameof(action));
        }

        Append(new InputBinding
        {
            Kind = InputKind.GamepadAxis,
            Code = (int)axis,
            Code1 = -1,
            Code2 = -1,
            Code3 = -1,
            Modifiers = UiModifiers.None,
            Scale = scale,
            Action = index,
        });
    }

    /// <summary>
    /// Binds two keys to an Axis action: the negative key and the positive key.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The bind needs an <see cref="UiActionType.Axis"/> action (the type rule). Each key gives its own value: the
    /// positive key gives 1, and the negative key gives -1. Both keys give 0, because the frame read sums both (A2). No
    /// key gives 0. The frame read sums the value of each binding of the action, and the Axis finish clamps the sum to
    /// -1 to 1.
    /// </para>
    /// <para>
    /// The composite reads the two keys with no modifier check (Decision 7), so Shift+W still reads 1. The binding stores
    /// no modifiers.
    /// </para>
    /// <para>
    /// The bind can grow an array and allocate, so bind at build time. The map reads the keys of an active map in
    /// <see cref="UiContext.Update(float)"/>, through the slot <see cref="UiContext.OnInputRead"/>. A game that reads the
    /// value calls <see cref="ReadAxis"/>.
    /// </para>
    /// </remarks>
    /// <param name="action">The UTF-8 name of an Axis action that the map holds.</param>
    /// <param name="negative">The GLFW key code of the key that gives -1.</param>
    /// <param name="positive">The GLFW key code of the key that gives 1.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="negative"/> or <paramref name="positive"/> is outside 0 to
    /// <see cref="UiKeyState.KeyCount"/> - 1. The parameter name is the name of that key.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// The map holds no action of the name <paramref name="action"/>, the action is not an
    /// <see cref="UiActionType.Axis"/>, the two keys have the same code, or the map binds these two keys in either order.
    /// </exception>
    public void BindKeyAxis(ReadOnlySpan<byte> action, int negative, int positive)
    {
        if ((uint)negative >= UiKeyState.KeyCount)
        {
            throw new ArgumentOutOfRangeException(nameof(negative), "The negative key must be a GLFW key code from 0 to UiKeyState.KeyCount - 1.");
        }

        if ((uint)positive >= UiKeyState.KeyCount)
        {
            throw new ArgumentOutOfRangeException(nameof(positive), "The positive key must be a GLFW key code from 0 to UiKeyState.KeyCount - 1.");
        }

        int index = IndexOfAction(action);
        if (index < 0)
        {
            throw new ArgumentException("The map holds no action with this name.", nameof(action));
        }

        CheckActionType(index, UiActionType.Axis);
        if (positive == negative)
        {
            throw new ArgumentException("The positive key has the code of the negative key.", nameof(positive));
        }

        if (FindValue(InputKind.KeyAxis, negative, positive, -1, -1) >= 0)
        {
            throw new ArgumentException("The map already binds these two keys.", nameof(action));
        }

        Append(new InputBinding
        {
            Kind = InputKind.KeyAxis,
            Code = negative,
            Code1 = positive,
            Code2 = -1,
            Code3 = -1,
            Modifiers = UiModifiers.None,
            Scale = 1f,
            Action = index,
        });
    }

    /// <summary>
    /// Binds the pointer delta of the frame to a Vector2 action.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The bind needs a <see cref="UiActionType.Vector2"/> action (the type rule). The frame read of an active map reads
    /// <see cref="BrutalUi.Core.UiPointerState.Delta"/> of the game view and multiplies <paramref name="scale"/> into
    /// both components. The delta keeps the pointer coordinates of the context, so Y points down (Decision 6): a move to
    /// the lower right gives a positive value on both axes. The scroll and the delta read the same pointer part.
    /// </para>
    /// <para>
    /// The frame keeps the value free (Q3): the read adds the delta after the clamp of the action, so a mouse look
    /// action reads any length. Another binding of the same action still clamps, so a stick and a delta in one action
    /// clamp the stick part and add the delta whole.
    /// </para>
    /// <para>
    /// The routing gate empties the pointer part of the game view when the pointer is over the UI, so the read gives
    /// (0, 0) there. One map holds one mouse delta binding: a second bind throws, whatever the action and the scale.
    /// The bind can grow an array and allocate, so bind at build time. A game that reads the value calls
    /// <see cref="ReadVector"/>.
    /// </para>
    /// </remarks>
    /// <param name="action">The UTF-8 name of a Vector2 action that the map holds.</param>
    /// <param name="scale">The value that the read multiplies into the delta. The default is 1.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="scale"/> is not a finite <see cref="float"/> value. The parameter name is <c>scale</c>.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// The map holds no action of the name <paramref name="action"/>, the action is not a
    /// <see cref="UiActionType.Vector2"/>, or the map binds the pointer delta already.
    /// </exception>
    public void BindMouseDelta(ReadOnlySpan<byte> action, float scale = 1f)
    {
        if (!float.IsFinite(scale))
        {
            throw new ArgumentOutOfRangeException(nameof(scale), "The scale must be a finite float value.");
        }

        int index = IndexOfAction(action);
        if (index < 0)
        {
            throw new ArgumentException("The map holds no action with this name.", nameof(action));
        }

        CheckActionType(index, UiActionType.Vector2);
        if (FindValue(InputKind.MouseDelta, -1, -1, -1, -1) >= 0)
        {
            throw new ArgumentException("The map already binds the pointer delta.", nameof(action));
        }

        Append(new InputBinding
        {
            Kind = InputKind.MouseDelta,
            Code = -1,
            Code1 = -1,
            Code2 = -1,
            Code3 = -1,
            Modifiers = UiModifiers.None,
            Scale = scale,
            Action = index,
        });
    }

    /// <summary>
    /// Binds the vertical scroll of the frame to an Axis action.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The bind needs an <see cref="UiActionType.Axis"/> action (the type rule). The frame read of an active map reads
    /// the Y component of <see cref="BrutalUi.Core.UiPointerState.ScrollDelta"/> of the game view and multiplies
    /// <paramref name="scale"/> into it. The read ignores the horizontal part: a wheel gives the vertical value, and a
    /// game that needs the horizontal value reads the state itself.
    /// </para>
    /// <para>
    /// The frame keeps the value free (Q3): the read adds the scroll after the clamp of the action, so a zoom axis from
    /// a trigger and the scroll reads the clamped trigger plus the whole scroll.
    /// </para>
    /// <para>
    /// The routing gate empties the pointer part of the game view when the pointer is over the UI, so the read gives 0
    /// there. One map holds one scroll binding: a second bind throws, whatever the action and the scale. The bind can
    /// grow an array and allocate, so bind at build time. A game that reads the value calls <see cref="ReadAxis"/>.
    /// </para>
    /// </remarks>
    /// <param name="action">The UTF-8 name of an Axis action that the map holds.</param>
    /// <param name="scale">The value that the read multiplies into the scroll. The default is 1.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="scale"/> is not a finite <see cref="float"/> value. The parameter name is <c>scale</c>.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// The map holds no action of the name <paramref name="action"/>, the action is not an
    /// <see cref="UiActionType.Axis"/>, or the map binds the scroll already.
    /// </exception>
    public void BindScroll(ReadOnlySpan<byte> action, float scale = 1f)
    {
        if (!float.IsFinite(scale))
        {
            throw new ArgumentOutOfRangeException(nameof(scale), "The scale must be a finite float value.");
        }

        int index = IndexOfAction(action);
        if (index < 0)
        {
            throw new ArgumentException("The map holds no action with this name.", nameof(action));
        }

        CheckActionType(index, UiActionType.Axis);
        if (FindValue(InputKind.Scroll, -1, -1, -1, -1) >= 0)
        {
            throw new ArgumentException("The map already binds the scroll.", nameof(action));
        }

        Append(new InputBinding
        {
            Kind = InputKind.Scroll,
            Code = -1,
            Code1 = -1,
            Code2 = -1,
            Code3 = -1,
            Modifiers = UiModifiers.None,
            Scale = scale,
            Action = index,
        });
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
    /// Finds the first binding of a composite input in the order of the bind. The map compares the codes as a set, not as a
    /// position: a second bind of the same keys in another order is a duplicate. A <see cref="InputKind.KeyAxis"/> pair and
    /// a <see cref="InputKind.KeyVector"/> set match in any order. Every other kind keeps the exact compare.
    /// </summary>
    /// <param name="kind">The kind of the input.</param>
    /// <param name="code">The first code of the composite.</param>
    /// <param name="code1">The second code of the composite, or -1.</param>
    /// <param name="code2">The third code of the composite, or -1.</param>
    /// <param name="code3">The fourth code of the composite, or -1.</param>
    /// <returns>The index of the binding, or -1 when no binding matches.</returns>
    private int FindValue(InputKind kind, int code, int code1, int code2, int code3)
    {
        InputBinding[] bindings = _bindings;
        int count = _bindingCount;
        for (int i = 0; i < count; i++)
        {
            ref readonly InputBinding binding = ref bindings[i];
            if (binding.Kind != kind)
            {
                continue;
            }

            if (kind == InputKind.KeyAxis)
            {
                if (SamePair(in binding, code, code1))
                {
                    return i;
                }

                continue;
            }

            if (kind == InputKind.KeyVector)
            {
                if (SameSet(in binding, code, code1, code2, code3))
                {
                    return i;
                }

                continue;
            }

            if (binding.Code == code && binding.Code1 == code1
                && binding.Code2 == code2 && binding.Code3 == code3)
            {
                return i;
            }
        }

        return -1;
    }

    /// <summary>
    /// Reports whether a <see cref="InputKind.KeyAxis"/> binding holds the same two keys, in either order.
    /// </summary>
    /// <param name="binding">The binding to read.</param>
    /// <param name="negative">The negative key of the new bind.</param>
    /// <param name="positive">The positive key of the new bind.</param>
    /// <returns><see langword="true"/> when the two pairs hold the same two codes.</returns>
    private static bool SamePair(in InputBinding binding, int negative, int positive)
    {
        return (binding.Code == negative && binding.Code1 == positive)
            || (binding.Code == positive && binding.Code1 == negative);
    }

    /// <summary>
    /// Reports whether a <see cref="InputKind.KeyVector"/> binding holds the same four keys, in any order. Each bind checks
    /// that its four codes differ, so both sets hold four codes, and one code of each side in the other set is enough.
    /// </summary>
    /// <param name="binding">The binding to read.</param>
    /// <param name="up">The up key of the new bind.</param>
    /// <param name="left">The left key of the new bind.</param>
    /// <param name="down">The down key of the new bind.</param>
    /// <param name="right">The right key of the new bind.</param>
    /// <returns><see langword="true"/> when the two sets hold the same four codes.</returns>
    private static bool SameSet(in InputBinding binding, int up, int left, int down, int right)
    {
        return HasCode(binding.Code, up, left, down, right)
            && HasCode(binding.Code1, up, left, down, right)
            && HasCode(binding.Code2, up, left, down, right)
            && HasCode(binding.Code3, up, left, down, right);
    }

    /// <summary>Reports whether one code is one of the four codes.</summary>
    /// <param name="code">The code to look for.</param>
    /// <param name="a">The first code.</param>
    /// <param name="b">The second code.</param>
    /// <param name="c">The third code.</param>
    /// <param name="d">The fourth code.</param>
    /// <returns><see langword="true"/> when one code equals <paramref name="code"/>.</returns>
    private static bool HasCode(int code, int a, int b, int c, int d)
    {
        return code == a || code == b || code == c || code == d;
    }

    /// <summary>
    /// Checks the type of an action for a bind (the type rule).
    /// </summary>
    /// <param name="index">The index of the action.</param>
    /// <param name="type">The type that the bind needs.</param>
    /// <exception cref="ArgumentException">The action of <paramref name="index"/> is not of <paramref name="type"/>.</exception>
    private void CheckActionType(int index, UiActionType type)
    {
        if (_actions[index].Type != type)
        {
            throw new ArgumentException("The action is not the type that this bind needs.", ActionParameter);
        }
    }

    /// <summary>
    /// Writes one binding at the end of the array. The call can grow the array and allocate, so bind at build time.
    /// </summary>
    /// <param name="binding">The binding to write.</param>
    private void Append(in InputBinding binding)
    {
        if (_bindingCount == _bindings.Length)
        {
            Array.Resize(ref _bindings, _bindings.Length == 0 ? InitialCapacity : _bindings.Length * 2);
        }

        _bindings[_bindingCount] = binding;
        _bindingCount++;
    }
}
