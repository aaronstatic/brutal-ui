using System;
using System.Numerics;
using Brutal.Numerics;
using BrutalUi.Core;

namespace BrutalUi.Framework;

// The remap part of the map: the listen, the capture scan, the commit, and the two report slots.
// A rebind takes one action in one scheme and replaces its whole group with the next fitting input.
// BeginRebind opens the listen, and the scan reads UiContext.Input, the raw view, so a rebind works while a
// widget holds focus or a pickable root covers the window. The first scan frame settles and reads no input, so
// the key or the click that opened the listen stays out of the new binding. The scan walks the seven capturable
// shapes of InputKind in one fixed order: a key, a mouse button, a gamepad button, a stick, a gamepad axis, a
// pointer delta, and a scroll. The stick sits before the axis, so a stick move never gives an axis capture. A
// digital shape needs a fresh press edge; an analog shape is live when its stored value is not exactly 0, and a
// pointer delta needs a length above RebindDeltaThreshold. A scroll reads the Y component only, as the frame read
// does. The cancel key (Escape by default) closes the listen first, and the fit test of the action type then
// accepts or rejects the candidate. A mismatch lands no binding, keeps the listen open, and reports RebindRejected
// once in each frame that the candidate stays live. The commit removes the group, appends one binding at the end of
// the array, and reports through OnRebindResult, past the throwing duplicate check of a bind. A frame that listens
// writes no value and runs no command, and the device track and OnSchemeChanged keep running. The map holds one
// handler in each slot, not a list. The first Activate copies the bindings of the build, and ResetBindings restores
// them with one in-place copy, so a restore allocates nothing.
public sealed partial class UiActionMap
{
    /// <summary>The status of a listen that landed its new binding: <c>0</c>.</summary>
    /// <remarks>
    /// The map calls <see cref="OnRebindResult"/> with this status and the code of the captured input.
    /// </remarks>
    public const int RebindBound = 0;

    /// <summary>The status of a listen that closed with no change: <c>1</c>.</summary>
    /// <remarks>
    /// The map calls <see cref="OnRebindResult"/> with this status and the code of the cancel key, or -1 for a
    /// <see cref="CancelRebind"/> call.
    /// </remarks>
    public const int RebindCancelled = 1;

    /// <summary>The status of a captured input that does not fit the action: <c>2</c>.</summary>
    /// <remarks>
    /// The map calls <see cref="OnRebindResult"/> with this status and the code of the rejected input. The listen
    /// stays open.
    /// </remarks>
    public const int RebindRejected = 2;

    /// <summary>The default cancel key of a listen: GLFW_KEY_ESCAPE (256).</summary>
    private const int RebindCancelKeyCode = 256;

    /// <summary>The value of <see cref="RebindCancelKey"/> that turns the cancel key off: <c>-1</c>.</summary>
    private const int RebindNoKey = -1;

    /// <summary>
    /// The least pointer-delta length that a capture takes, in device-independent pixels. A raw delta is
    /// unfiltered screen movement, so a zero test is too weak for it.
    /// </summary>
    private const float RebindDeltaThreshold = 8f;

    /// <summary><see langword="true"/> when a listen is open. The frame read scans, and five paths go silent.</summary>
    private bool _rebindOpen;

    /// <summary>The index of the target action, or -1 when no listen is open.</summary>
    private int _rebindAction = -1;

    /// <summary>The scheme id of the listen. It tags the new binding and names the group that the commit replaces.</summary>
    private int _rebindScheme;

    /// <summary>The count of frames that the scan leaves alone after a fresh listen. <see cref="BeginRebind"/> sets 1.</summary>
    private int _rebindSettle;

    /// <summary>The key code that closes a listen, or -1 for none. <see cref="RebindCancelKey"/> reads and writes it.</summary>
    private int _rebindCancelKey = RebindCancelKeyCode;

    /// <summary>The bindings of the build, copied on the first <see cref="Activate"/>. <see langword="null"/> before it.</summary>
    private InputBinding[]? _defaults;

    /// <summary>The count of bindings in <see cref="_defaults"/>. Valid when <see cref="_defaultsCaptured"/> is set.</summary>
    private int _defaultsCount;

    /// <summary><see langword="true"/> when <see cref="CaptureDefaults"/> copied the bindings of the build.</summary>
    private bool _defaultsCaptured;

    /// <summary>
    /// A handler that the map calls when a listen closes or rejects an input. The map calls the slot
    /// <see cref="OnRebindResult"/> with the map, the action name, the status, and the code.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The place rule: the frame read calls the result slot for a cancel key, a reject, and a commit, and calls the
    /// conflict slot after a commit, in the frame that closes or rejects the listen. A <see cref="CancelRebind"/>
    /// call reports through the result slot at the point of the call, which may sit outside
    /// <see cref="UiContext.Update(float)"/>.
    /// </para>
    /// <para>
    /// The slot rule: a game writes the slot with <c>=</c> on <see cref="OnRebindResult"/>. The map holds one handler,
    /// not a list, so a write with <c>+=</c> is a mistake. A null slot calls nothing.
    /// </para>
    /// <para>
    /// The span rule: the action span is valid only during the call. Copy the bytes that the game wants to keep.
    /// </para>
    /// <para>
    /// The re-entry rule: inside the frame read, the handler is code in the read. A bind, a
    /// <see cref="BeginRebind"/>, or a <see cref="ResetBindings"/> call in it takes effect at the next frame. A call
    /// of <see cref="UiContext.Update(float)"/> from it throws <see cref="InvalidOperationException"/>. Outside the
    /// read, the re-entry rule does not apply, and a call of <see cref="UiContext.Update(float)"/> from the handler
    /// does not throw. A handler that must know the place reads <see cref="IsActive"/> or keeps its own flag.
    /// </para>
    /// <para>
    /// The exception rule: an exception of the handler leaves <see cref="UiContext.Update(float)"/>. A commit that
    /// already landed stays committed, because the map closes the listen before the call.
    /// </para>
    /// <para>
    /// The build-time rule: create the delegate one time and set the slot at build time, not in the frame loop. A new
    /// delegate in each frame allocates.
    /// </para>
    /// </remarks>
    /// <param name="map">The map that ran the listen.</param>
    /// <param name="action">The UTF-8 name of the target action. The span is valid only during the call.</param>
    /// <param name="status">
    /// One of <see cref="RebindBound"/>, <see cref="RebindCancelled"/>, or <see cref="RebindRejected"/>.
    /// </param>
    /// <param name="code">
    /// The code that comes with the status. A bound or a rejected status gives the captured key code. A cancelled
    /// status gives the cancel key, or -1 for a <see cref="CancelRebind"/> call.
    /// </param>
    public delegate void RebindResultHandler(UiActionMap map, ReadOnlySpan<byte> action, int status, int code);

    /// <summary>
    /// A handler that the map calls when a committed input already belongs to another action. The map calls the slot
    /// <see cref="OnRebindConflict"/> in the frame read, after the result slot, with the map and the two action
    /// names.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The place rule: the frame read calls the result slot for a cancel key, a reject, and a commit, and calls
    /// this handler after a commit and after <see cref="OnRebindResult"/> of the same frame. A
    /// <see cref="CancelRebind"/> call reports through the result slot at the point of the call, and never calls
    /// this handler. The map keeps both bindings.
    /// </para>
    /// <para>
    /// The slot rule: a game writes the slot with <c>=</c> on <see cref="OnRebindConflict"/>. The map holds one handler,
    /// not a list, so a write with <c>+=</c> is a mistake. A null slot calls nothing.
    /// </para>
    /// <para>
    /// The span rule: both name spans are valid only during the call. Copy the bytes that the game wants to keep.
    /// </para>
    /// <para>
    /// The re-entry rule: the handler is code inside the frame read. A bind, a <see cref="BeginRebind"/>, or a
    /// <see cref="ResetBindings"/> call in it takes effect at the next frame. A call of
    /// <see cref="UiContext.Update(float)"/> from it throws <see cref="InvalidOperationException"/>.
    /// </para>
    /// <para>
    /// The exception rule: an exception of the handler leaves <see cref="UiContext.Update(float)"/>. The commit already
    /// landed, so both bindings stay.
    /// </para>
    /// <para>
    /// The build-time rule: create the delegate one time and set the slot at build time, not in the frame loop.
    /// </para>
    /// </remarks>
    /// <param name="map">The map that ran the listen.</param>
    /// <param name="action">The UTF-8 name of the rebound action. The span is valid only during the call.</param>
    /// <param name="otherAction">
    /// The UTF-8 name of the other action that holds the same input. The span is valid only during the call.
    /// </param>
    public delegate void RebindConflictHandler(UiActionMap map, ReadOnlySpan<byte> action, ReadOnlySpan<byte> otherAction);

    /// <summary>
    /// Gets a value that tells if a listen is open.
    /// </summary>
    /// <remarks>
    /// The read copies one bool: it allocates nothing, so it gives 0 bytes.
    /// </remarks>
    public bool IsRebinding => _rebindOpen;

    /// <summary>
    /// Gets or sets the key code that closes a listen. The default is 256 (Escape), and -1 turns the key off.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The listen rule: the scan reads one press edge of this code first, before the fit test, so the cancel key wins
    /// over a candidate. The listen consumes the edge, so the map runs no command for it.
    /// </para>
    /// <para>
    /// The disable rule: a value of <see cref="RebindNoKey"/> (-1) turns the cancel key off. The code then becomes an
    /// ordinary candidate.
    /// </para>
    /// <para>
    /// The cost rule: the property allocates nothing. A set takes effect in the next frame read.
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">
    /// A set outside -1 to <see cref="UiKeyState.KeyCount"/> - 1. The property keeps the old value. The parameter name
    /// is <c>value</c>.
    /// </exception>
    public int RebindCancelKey
    {
        get => _rebindCancelKey;
        set
        {
            if (value != RebindNoKey && (uint)value >= UiKeyState.KeyCount)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(value),
                    "The cancel key must be -1 or a GLFW key code from 0 to UiKeyState.KeyCount - 1.");
            }

            _rebindCancelKey = value;
        }
    }

    /// <summary>
    /// Gets or sets the handler that the map calls when a listen closes or rejects an input.
    /// </summary>
    /// <remarks>
    /// Write the property with <c>=</c>. The map holds one handler, not a list. See
    /// <see cref="RebindResultHandler"/> for the place rule, the span rule, the re-entry rule, the exception rule, and
    /// the build-time rule.
    /// </remarks>
    public RebindResultHandler? OnRebindResult { get; set; }

    /// <summary>
    /// Gets or sets the handler that the map calls when a committed input already belongs to another action.
    /// </summary>
    /// <remarks>
    /// Write the property with <c>=</c>. The map holds one handler, not a list. See
    /// <see cref="RebindConflictHandler"/> for the place rule, the span rule, the re-entry rule, the exception rule,
    /// and the build-time rule.
    /// </remarks>
    public RebindConflictHandler? OnRebindConflict { get; set; }

    /// <summary>
    /// Opens a listen for one action in one scheme. The next fitting input becomes the new binding of that group.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The target rule: the listen takes the whole action inside one scheme, and the captured input replaces it (Q5).
    /// A group can hold more than one binding, and the commit removes each of them.
    /// </para>
    /// <para>
    /// The scheme argument rule: <paramref name="scheme"/> names the tag that the new binding gets, and the set of
    /// bindings that it replaces. It filters no input, because any fitting input is captured (Q11).
    /// </para>
    /// <para>
    /// The raw rule: the scan reads <see cref="UiContext.Input"/>, the raw view, so a rebind works while a widget holds
    /// focus or a pickable root covers the window (Q1).
    /// </para>
    /// <para>
    /// The capture rule: the scan walks the seven capturable shapes of <see cref="InputKind"/> in one fixed order: a
    /// key, a mouse button, a gamepad button, a stick, a gamepad axis, a pointer delta, and a scroll. The first live
    /// shape is the candidate. <see cref="InputKind.KeyVector"/> and <see cref="InputKind.KeyAxis"/> hold two or four
    /// keys, so no rebind captures them.
    /// </para>
    /// <para>
    /// The fit rule: the candidate must fill the type of the target action. A Button action takes a key, a mouse button,
    /// or a gamepad button; an Axis action takes a gamepad axis or a scroll; a Vector2 action takes a stick or a pointer
    /// delta. A candidate that does not fit lands no binding and keeps the listen open.
    /// </para>
    /// <para>
    /// The settle rule: the first frame read of a fresh listen reads no input. The key or the click that opened the
    /// listen therefore stays out of the new binding.
    /// </para>
    /// <para>
    /// The give-false-for-unknown rule: a name that the map does not hold gives <see langword="false"/>, because a
    /// runtime call from a menu must not throw on data. A rejected call changes no count.
    /// </para>
    /// <para>
    /// The twice rule: a second call moves the listen to the new target, and the first target keeps its bindings.
    /// </para>
    /// <para>
    /// The inactive rule: a listen on a map that no <see cref="Activate"/> reached waits, because only the frame read
    /// scans. <see cref="Activate"/> and <see cref="Deactivate"/> close a listen with no commit.
    /// </para>
    /// <para>
    /// The work rule: the method allocates nothing.
    /// </para>
    /// </remarks>
    /// <param name="action">The UTF-8 name of an action that the map holds.</param>
    /// <param name="scheme">The id of the scheme that the new binding lands in. See <see cref="UiScheme.Default"/>.</param>
    /// <returns>
    /// <see langword="true"/> when the listen opened on the pair. <see langword="false"/> when the map holds no action
    /// of the name.
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="scheme"/> is negative, or it is at or above the count of valid ids. The method opens nothing.
    /// The parameter name is <c>scheme</c>.
    /// </exception>
    public bool BeginRebind(ReadOnlySpan<byte> action, int scheme)
    {
        if ((uint)scheme >= (uint)_schemeCount)
        {
            throw new ArgumentOutOfRangeException(
                SchemeParameter,
                "The scheme id must be 0 or the id of a name that AddScheme gave.");
        }

        int index = IndexOfAction(action);
        if (index < 0)
        {
            return false;
        }

        _rebindAction = index;
        _rebindScheme = scheme;
        _rebindSettle = 1;
        _rebindOpen = true;
        return true;
    }

    /// <summary>
    /// Closes an open listen with no change, and reports the cancel through <see cref="OnRebindResult"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The close rule: a call on an open listen clears the listen and keeps every binding. The map reports
    /// <see cref="RebindCancelled"/> with the code -1.
    /// </para>
    /// <para>
    /// The report rule: the method calls <see cref="OnRebindResult"/> at the point of the call, not from the frame
    /// read. A game may call the method between frames, outside <see cref="UiContext.Update(float)"/>. A handler
    /// that must know the place reads <see cref="IsActive"/> or keeps its own flag.
    /// </para>
    /// <para>
    /// The idle rule: a call with no open listen gives <see langword="false"/>, calls nothing, and changes no count.
    /// </para>
    /// <para>
    /// The work rule: the method allocates nothing.
    /// </para>
    /// </remarks>
    /// <returns>
    /// <see langword="true"/> when the method closed an open listen. Else <see langword="false"/>.
    /// </returns>
    public bool CancelRebind()
    {
        if (!_rebindOpen)
        {
            return false;
        }

        byte[] name = _actions[_rebindAction].Name;
        CloseRebind();
        OnRebindResult?.Invoke(this, name, RebindCancelled, RebindNoKey);
        return true;
    }

    /// <summary>
    /// Clears the listen state: the open flag, the settle count, and the target action.
    /// </summary>
    /// <remarks>
    /// Every close path calls it: a commit, a cancel, and a reject of the cancel key. The method keeps the scheme id
    /// of the last listen, because a fresh <see cref="BeginRebind"/> writes it again. The method allocates nothing.
    /// </remarks>
    private void CloseRebind()
    {
        _rebindOpen = false;
        _rebindSettle = 0;
        _rebindAction = -1;
    }

    /// <summary>One captured input, ready for the fit test and the commit.</summary>
    private struct CapturedInput
    {
        /// <summary>The kind of the captured input.</summary>
        public InputKind Kind;

        /// <summary>The code of the captured input, or -1 for a shape that holds no code.</summary>
        public int Code;

        /// <summary>The second code of a composite. Every capture writes -1.</summary>
        public int Code1;

        /// <summary>The third code of a composite. Every capture writes -1.</summary>
        public int Code2;

        /// <summary>The fourth code of a composite. Every capture writes -1.</summary>
        public int Code3;

        /// <summary>The modifier keys of the capture, masked with <see cref="MatchMask"/>.</summary>
        public UiModifiers Modifiers;

        /// <summary><see langword="true"/> when the capture holds a live input.</summary>
        public bool Found;
    }

    /// <summary>
    /// Reads the first live shape of the seven capturable shapes, in the <see cref="InputKind"/> order.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The order rule: the scan walks a key, a mouse button, a gamepad button, a stick, a gamepad axis, a pointer
    /// delta, and a scroll, and gives the first live shape. A frame with two live shapes gives the earlier one, even
    /// when the later one would fit the target action.
    /// </para>
    /// <para>
    /// The press-edge rule: a digital shape is live only through a fresh press edge, never through a held mask. A key
    /// reads <see cref="UiKeyState.WasPressed(int)"/>, a mouse button reads
    /// <see cref="UiPointerButtonState.WasPressed(PointerButton)"/>, and a gamepad button reads one bit of
    /// <see cref="UiGamepadState.Pressed"/>, where the lowest set bit wins.
    /// </para>
    /// <para>
    /// The analog rule: an analog shape is live when its stored value is not exactly 0, because the poll baked the
    /// deadzone into it (A5). The stick sits before the axis, so a stick move gives a stick capture and never an axis
    /// capture of axes 0 to 3. The pointer delta needs a length above <see cref="RebindDeltaThreshold"/>: the method
    /// compares the squared length with the squared threshold, so it runs no square root, and a non-finite length gives
    /// no candidate. A scroll reads <see cref="UiPointerState.ScrollDelta"/>.Y only, because the frame read does the
    /// same.
    /// </para>
    /// <para>
    /// The modifier rule: a key and a mouse button carry <see cref="UiKeyState.Modifiers"/> masked with
    /// <see cref="MatchMask"/>. Every other shape carries <see cref="UiModifiers.None"/>.
    /// </para>
    /// <para>
    /// The cancel rule: the code of <see cref="RebindCancelKey"/> is never a candidate, because the cancel path takes it.
    /// </para>
    /// <para>
    /// The work rule: the method allocates nothing.
    /// </para>
    /// </remarks>
    /// <param name="input">The raw input state of the frame.</param>
    /// <param name="pad">The pad slot of <see cref="GamepadSlot"/>.</param>
    /// <param name="c">The capture, with <see cref="CapturedInput.Found"/> set when the scan found a live shape.</param>
    /// <returns>
    /// <see langword="true"/> when the scan found a candidate. Else <see langword="false"/>.
    /// </returns>
    private bool TryCaptureCandidate(in UiInputState input, ref readonly UiGamepadState pad, out CapturedInput c)
    {
        ref readonly UiKeyState keys = ref input.Keys;
        for (int code = 0; code < UiKeyState.KeyCount; code++)
        {
            if (keys.WasPressed(code) && code != _rebindCancelKey)
            {
                c = new CapturedInput
                {
                    Kind = InputKind.Key,
                    Code = code,
                    Code1 = -1,
                    Code2 = -1,
                    Code3 = -1,
                    Modifiers = keys.Modifiers & MatchMask,
                    Found = true,
                };
                return true;
            }
        }

        ref readonly UiPointerButtonState buttons = ref input.Buttons;
        for (int code = 0; code < UiPointerButtonState.ButtonCount; code++)
        {
            if (buttons.WasPressed((PointerButton)code))
            {
                c = new CapturedInput
                {
                    Kind = InputKind.MouseButton,
                    Code = code,
                    Code1 = -1,
                    Code2 = -1,
                    Code3 = -1,
                    Modifiers = keys.Modifiers & MatchMask,
                    Found = true,
                };
                return true;
            }
        }

        UiGamepadButtons pressed = pad.Pressed;
        if (pressed != UiGamepadButtons.None)
        {
            c = new CapturedInput
            {
                Kind = InputKind.GamepadButton,
                Code = 1 << BitOperations.TrailingZeroCount((uint)pressed),
                Code1 = -1,
                Code2 = -1,
                Code3 = -1,
                Modifiers = UiModifiers.None,
                Found = true,
            };
            return true;
        }

        float2 leftStick = pad.LeftStick;
        if (leftStick.X != 0f || leftStick.Y != 0f)
        {
            c = new CapturedInput
            {
                Kind = InputKind.GamepadStick,
                Code = (int)UiGamepadStick.Left,
                Code1 = -1,
                Code2 = -1,
                Code3 = -1,
                Modifiers = UiModifiers.None,
                Found = true,
            };
            return true;
        }

        float2 rightStick = pad.RightStick;
        if (rightStick.X != 0f || rightStick.Y != 0f)
        {
            c = new CapturedInput
            {
                Kind = InputKind.GamepadStick,
                Code = (int)UiGamepadStick.Right,
                Code1 = -1,
                Code2 = -1,
                Code3 = -1,
                Modifiers = UiModifiers.None,
                Found = true,
            };
            return true;
        }

        for (int axis = 0; axis < UiGamepadState.AxisCount; axis++)
        {
            if (pad.Axis(axis) != 0f)
            {
                c = new CapturedInput
                {
                    Kind = InputKind.GamepadAxis,
                    Code = axis,
                    Code1 = -1,
                    Code2 = -1,
                    Code3 = -1,
                    Modifiers = UiModifiers.None,
                    Found = true,
                };
                return true;
            }
        }

        ref readonly UiPointerState pointer = ref input.Pointer;
        float deltaX = pointer.Delta.X;
        float deltaY = pointer.Delta.Y;
        float squaredLength = deltaX * deltaX + deltaY * deltaY;
        if (float.IsFinite(squaredLength) && squaredLength > RebindDeltaThreshold * RebindDeltaThreshold)
        {
            c = new CapturedInput
            {
                Kind = InputKind.MouseDelta,
                Code = RebindNoKey,
                Code1 = -1,
                Code2 = -1,
                Code3 = -1,
                Modifiers = UiModifiers.None,
                Found = true,
            };
            return true;
        }

        if (pointer.ScrollDelta.Y != 0f)
        {
            c = new CapturedInput
            {
                Kind = InputKind.Scroll,
                Code = RebindNoKey,
                Code1 = -1,
                Code2 = -1,
                Code3 = -1,
                Modifiers = UiModifiers.None,
                Found = true,
            };
            return true;
        }

        c = default;
        return false;
    }

    /// <summary>
    /// Gives the scheme id of the device that one shape reports.
    /// </summary>
    /// <remarks>
    /// The table is the device track of the frame read: a key, a mouse button, a key composite, the pointer delta, and
    /// a scroll report <see cref="UiScheme.KeyboardMouse"/>; a gamepad button, a stick, and an axis report
    /// <see cref="UiScheme.Gamepad"/>. The two composites never come from a capture, so the default case gives the
    /// keyboard id. The method allocates nothing.
    /// </remarks>
    /// <param name="kind">The kind of the capture.</param>
    /// <returns>The id of the device of the kind.</returns>
    private int DeviceOf(InputKind kind)
    {
        return kind switch
        {
            InputKind.GamepadButton => _gamepadScheme,
            InputKind.GamepadStick => _gamepadScheme,
            InputKind.GamepadAxis => _gamepadScheme,
            _ => _keyboardScheme,
        };
    }

    /// <summary>
    /// Reports whether one capture fills one action type.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The fit table: a <see cref="UiActionType.Button"/> action takes a key, a mouse button, or a gamepad button; an
    /// <see cref="UiActionType.Axis"/> action takes a gamepad axis or a scroll; a
    /// <see cref="UiActionType.Vector2"/> action takes a stick or a pointer delta.
    /// </para>
    /// <para>
    /// The composite rule: <see cref="InputKind.KeyVector"/> and <see cref="InputKind.KeyAxis"/> are never capturable,
    /// because a rebind captures one input and a composite needs two or four keys. A composite action rebinds as one
    /// binding only through the text format, which is Out of Scope of the listen. A rebind of its action removes the
    /// composite binding of the target scheme.
    /// </para>
    /// </remarks>
    /// <param name="c">The capture to test.</param>
    /// <param name="type">The type of the target action.</param>
    /// <returns>
    /// <see langword="true"/> when the capture fills the type. Else <see langword="false"/>.
    /// </returns>
    private static bool FitsCapture(in CapturedInput c, UiActionType type)
    {
        return type switch
        {
            UiActionType.Button => c.Kind is InputKind.Key or InputKind.MouseButton or InputKind.GamepadButton,
            UiActionType.Axis => c.Kind is InputKind.GamepadAxis or InputKind.Scroll,
            UiActionType.Vector2 => c.Kind is InputKind.GamepadStick or InputKind.MouseDelta,
            _ => false,
        };
    }

    /// <summary>
    /// The listen scan: the frame read calls it in place of the four passes, and the frame then writes no value and
    /// runs no command.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The settle rule: a fresh listen reads no input in its first frame, and the scan clears the count.
    /// </para>
    /// <para>
    /// The one-candidate-per-frame rule: the scan takes the first live shape of the frame and stops. A frame with two
    /// live shapes commits the first one, even when the second would fit.
    /// </para>
    /// <para>
    /// The press-edge rule: a digital shape is live only through a fresh press edge, never through a held mask.
    /// </para>
    /// <para>
    /// The cancel-beats-fit rule: the scan reads the cancel-key edge first, so the cancel key closes a listen even
    /// when its code does not fit the target action.
    /// </para>
    /// <para>
    /// The reject-stays-open rule: a candidate that does not fit the action type lands no binding, moves no settle
    /// count, touches no binding, and keeps the listen open. The map reports <see cref="RebindRejected"/> with its
    /// code, which is -1 for a pointer delta and a scroll, in every frame that the candidate stays live. The check
    /// runs before <see cref="FindConflictAction"/>, <see cref="RemoveBindings"/>, and <see cref="Append"/>, so no
    /// dead binding can land (Q4).
    /// </para>
    /// <para>
    /// The consumed-edge rule: a commit closes the listen and the frame read returns, so the value loop never reads the
    /// new binding in that frame. The player presses the new key a second time to drive the action.
    /// </para>
    /// <para>
    /// The device rule: the scan ends with one device store and <see cref="ReportSchemeChange"/>, so the device track
    /// and <see cref="OnSchemeChanged"/> keep running in a suppressed frame (Q9). The scan gives the device of the
    /// candidate through <see cref="DeviceOf(InputKind)"/>, the same table as the frame read, for all seven shapes. A
    /// frame with no live shape keeps the last report (the carry rule).
    /// </para>
    /// <para>
    /// The work rule: the method allocates nothing.
    /// </para>
    /// </remarks>
    /// <param name="context">The context that calls the frame read.</param>
    private void RebindScan(UiContext context)
    {
        int device = _activeScheme;
        if (_rebindSettle > 0)
        {
            _rebindSettle--;
        }
        else
        {
            ref readonly UiInputState input = ref context.Input;
            ref readonly UiGamepadState pad = ref input.Gamepad(_gamepadSlot);
            if (_rebindCancelKey >= 0 && input.Keys.WasPressed(_rebindCancelKey))
            {
                int code = _rebindCancelKey;
                byte[] name = _actions[_rebindAction].Name;
                CloseRebind();
                OnRebindResult?.Invoke(this, name, RebindCancelled, code);
            }
            else if (TryCaptureCandidate(input, in pad, out CapturedInput c))
            {
                device = DeviceOf(c.Kind);
                if (FitsCapture(c, _actions[_rebindAction].Type))
                {
                    CommitRebind(c);
                }
                else
                {
                    OnRebindResult?.Invoke(this, _actions[_rebindAction].Name, RebindRejected, c.Code);
                }
            }
        }

        _activeScheme = device;
        ReportSchemeChange();
    }

    /// <summary>
    /// Finds the action of another binding that clashes with one capture in one scheme.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The before-the-commit rule: the map calls the method before <see cref="RemoveBindings"/>, because a compaction
    /// moves the indices of the array.
    /// </para>
    /// <para>
    /// The clash rule: the method reuses <see cref="SchemeClashes"/>, so a clash against <see cref="UiScheme.Default"/>
    /// reports in either order (A4). The target action is not a clash: its group is the set that the commit replaces.
    /// The compare covers all seven capture kinds: a key, a mouse button, and a gamepad button carry a code and a
    /// modifier set, a stick and an axis carry a code and no modifier, and a pointer delta and a scroll carry neither.
    /// </para>
    /// <para>
    /// The work rule: the method reads the array and the count in place, allocates nothing, and throws nothing.
    /// </para>
    /// </remarks>
    /// <param name="c">The capture to look for.</param>
    /// <param name="scheme">The scheme id of the listen.</param>
    /// <returns>The index of the other action, or -1 when no binding clashes.</returns>
    private int FindConflictAction(in CapturedInput c, int scheme)
    {
        InputBinding[] bindings = _bindings;
        int count = _bindingCount;
        UiModifiers match = c.Modifiers & MatchMask;
        for (int i = 0; i < count; i++)
        {
            ref readonly InputBinding binding = ref bindings[i];
            if (binding.Kind == c.Kind && binding.Code == c.Code
                && binding.Modifiers == match && SchemeClashes(binding.Scheme, scheme)
                && binding.Action != _rebindAction)
            {
                return binding.Action;
            }
        }

        return -1;
    }

    /// <summary>
    /// Lands one capture as the new binding of the target group, closes the listen, and reports the result.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The order rule: the method finds the clash first, then removes the group, then appends one binding at the end of
    /// the array, then closes the listen, then calls <see cref="OnRebindResult"/>, then calls
    /// <see cref="OnRebindConflict"/> for a clash. The listen is closed before the slots run, so a handler that throws
    /// leaves a committed binding and no open listen.
    /// </para>
    /// <para>
    /// The bypass rule: the commit calls <see cref="Append"/>, not <see cref="FindClash"/>, so a conflicting rebind
    /// lands and both bindings stay (Q8, A4). The bind-time duplicate check keeps its throw for a build.
    /// </para>
    /// <para>
    /// The free-room rule: the commit has room for the append in two parts. The <see cref="RemoveBindings"/> call frees a
    /// slot when the target group holds one, and the build-time slack of <see cref="CaptureDefaults"/> covers the group
    /// that holds none. So a commit cannot resize the array in a frame while the scheme table stands still (A1). The
    /// method allocates nothing after warm-up.
    /// </para>
    /// <para>
    /// The field rule: the commit covers all seven capture kinds. It writes <c>Kind</c>, <c>Code</c>, and
    /// <c>Code1</c> to <c>Code3 = -1</c>, <c>Modifiers</c> masked with <see cref="MatchMask"/>, <c>Scheme</c> of the
    /// listen, <c>Scale = 1f</c>, and the target <c>Action</c>: the same field defaults as the nine bind bodies.
    /// </para>
    /// </remarks>
    /// <param name="c">The capture to land.</param>
    private void CommitRebind(in CapturedInput c)
    {
        int clashAction = FindConflictAction(c, _rebindScheme);
        int actionIndex = _rebindAction;
        byte[] name = _actions[actionIndex].Name;
        RemoveBindings(actionIndex, _rebindScheme);
        Append(new InputBinding
        {
            Kind = c.Kind,
            Code = c.Code,
            Code1 = c.Code1,
            Code2 = c.Code2,
            Code3 = c.Code3,
            Modifiers = c.Modifiers & MatchMask,
            Scheme = _rebindScheme,
            Scale = 1f,
            Action = actionIndex,
        });
        CloseRebind();
        OnRebindResult?.Invoke(this, name, RebindBound, c.Code);
        if (clashAction >= 0)
        {
            OnRebindConflict?.Invoke(this, name, _actions[clashAction].Name);
        }
    }

    /// <summary>
    /// Removes every binding of one action in one scheme, and gives the count of the removals.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The group rule: the method takes a binding when its action index and its scheme id both equal the target (Q5,
    /// Q11). A binding of another scheme of the same action stays.
    /// </para>
    /// <para>
    /// The order rule: the method compacts the array in place, so the survivors keep their order, and a later
    /// <see cref="Append"/> lands the new binding at the end.
    /// </para>
    /// <para>
    /// The work rule: one walk with one survivor copy, no new array, and no <see cref="Array.Resize"/> call. Every read
    /// bounds itself with the count, so the method needs no clear of the tail. The method allocates nothing.
    /// </para>
    /// </remarks>
    /// <param name="actionIndex">The index of the target action.</param>
    /// <param name="scheme">The id of the target scheme.</param>
    /// <returns>The count of bindings that the method removed.</returns>
    private int RemoveBindings(int actionIndex, int scheme)
    {
        InputBinding[] bindings = _bindings;
        int count = _bindingCount;
        int j = 0;
        for (int i = 0; i < count; i++)
        {
            if (bindings[i].Action == actionIndex && bindings[i].Scheme == scheme)
            {
                continue;
            }

            if (j != i)
            {
                bindings[j] = bindings[i];
            }

            j++;
        }

        _bindingCount = j;
        return count - j;
    }

    /// <summary>
    /// Copies the bindings of the build one time, so a later <see cref="ResetBindings"/> can restore them.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The once rule: the method acts only while <see cref="_defaultsCaptured"/> is <see langword="false"/>. The first
    /// <see cref="Activate"/> calls it, before any rebind lands (A6). A later call does nothing, so a mode switch keeps
    /// the same snapshot.
    /// </para>
    /// <para>
    /// The load-time rule: the copy creates one array, and the slack grow may create one more. The first activation is
    /// therefore a build-time step, in the pattern of <see cref="AddAction(ReadOnlySpan{byte}, UiActionType)"/> and each
    /// bind. It is the one place the feature allocates a binding array.
    /// </para>
    /// <para>
    /// The slack rule: the method grows the live array to <c>_bindingCount + _actionCount * _schemeCount</c> when it is
    /// smaller. One net add for every action-and-scheme pair the table can name then fits without a resize, so a rebind
    /// of an action that holds no binding in the target scheme cannot grow the array in a frame. A real map pays a few
    /// bytes at its first <see cref="Activate"/>, which is a build-time step.
    /// </para>
    /// <para>
    /// The reopen rule: a scheme that a game adds after the first <see cref="Activate"/> reopens the grow, the same way a
    /// bind after the first <see cref="Activate"/> reopens the snapshot loss. The slack covers the pairs the table could
    /// name at the first <see cref="Activate"/>, not the pairs it gains later.
    /// </para>
    /// <para>
    /// The not-a-lock rule: the snapshot holds bindings only. It holds no action, no action name, no scheme table, and no
    /// lock, so <see cref="ResetBindings"/> restores the bindings and nothing else. The snapshot array stays separate
    /// from the live array, so a grow of the live array does not move the snapshot.
    /// </para>
    /// </remarks>
    private void CaptureDefaults()
    {
        if (_defaultsCaptured)
        {
            return;
        }

        int count = _bindingCount;
        InputBinding[] defaults = new InputBinding[count];
        Array.Copy(_bindings, 0, defaults, 0, count);
        _defaults = defaults;
        _defaultsCount = count;

        int needed = count + _actionCount * _schemeCount;
        if (_bindings.Length < needed)
        {
            InputBinding[] grown = new InputBinding[needed];
            Array.Copy(_bindings, 0, grown, 0, count);
            _bindings = grown;
        }

        _defaultsCaptured = true;
    }

    /// <summary>
    /// Restores the bindings that the map held on its first <see cref="Activate"/>, and closes an open listen.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The restore rule: the method copies the snapshot into the existing binding array and restores the count. The array
    /// instance does not change, so the method allocates nothing. The bindings of the build come back.
    /// </para>
    /// <para>
    /// The scope rule: the method restores bindings only, because requirements line 15 names the bindings. It leaves
    /// <see cref="ActionCount"/>, each action type, each command name, the scheme table, <see cref="GamepadSlot"/>, and
    /// the lock of <see cref="SetScheme(int)"/> alone. It also leaves <see cref="OnRebindResult"/>,
    /// <see cref="OnRebindConflict"/>, and <see cref="RebindCancelKey"/> alone.
    /// </para>
    /// <para>
    /// The bind-after-activate rule: a bind that a game adds after the first <see cref="Activate"/> is gone after a
    /// restore. The snapshot came before it (A6).
    /// </para>
    /// <para>
    /// The <see cref="ReadBindings"/> rule: a text read does not move the snapshot, so a restore after a read gives the
    /// bindings of the build (A6).
    /// </para>
    /// <para>
    /// The close rule: the method closes an open listen with no commit and no slot call. No binding lands, and the map
    /// reports nothing.
    /// </para>
    /// <para>
    /// The re-entry rule: a restore that a slot handler or a command calls takes effect at the next frame, because the
    /// frame read copied the array and the count before it scanned.
    /// </para>
    /// </remarks>
    /// <returns>
    /// <see langword="true"/> when the method restored the snapshot. <see langword="false"/> when the map holds no
    /// snapshot, which one <see cref="Activate"/> creates. A rejected call changes nothing.
    /// </returns>
    public bool ResetBindings()
    {
        if (!_defaultsCaptured || _defaults is not { } defaults)
        {
            return false;
        }

        CloseRebind();
        Array.Copy(defaults, 0, _bindings, 0, _defaultsCount);
        _bindingCount = _defaultsCount;
        return true;
    }
}
