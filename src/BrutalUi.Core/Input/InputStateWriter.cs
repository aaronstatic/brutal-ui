using System.Runtime.CompilerServices;
using Brutal.Numerics;

namespace BrutalUi.Core;

/// <summary>
/// The writer of <see cref="UiInputState"/>. <see cref="UiContext"/> holds one value in a field and calls it from
/// the three hooks of <see cref="UiContext.DrainEvents"/>.
/// </summary>
/// <remarks>
/// <para>
/// This is a mutable struct, in the pattern of <see cref="EventQueue"/>. Keep it in a field, and change it only
/// through the field or a <see langword="ref"/> to the field. A change on a copy is lost.
/// </para>
/// <para>
/// The frame rule. Hook 1 (<see cref="BeginFrame"/>) clears the frame values and keeps the start position. Hook 2
/// (<see cref="Record"/>) writes each entry of the drain into the raw state. Hook 3
/// (<see cref="EndFrame(UiRoutingGate)"/>) writes the delta and the game view. The game reads the state after
/// <see cref="UiContext.Update(float)"/> only, so it always reads a finished frame.
/// </para>
/// <para>The allocation rule. No field holds a reference, and no method allocates.</para>
/// </remarks>
internal struct InputStateWriter
{
    /// <summary>The raw state: the values that the drain recorded, before the gate.</summary>
    internal UiInputState Raw;

    /// <summary>The game view: the copy of <see cref="Raw"/> that the routing gate filters.</summary>
    internal UiInputState Game;

    /// <summary>
    /// The pointer position at the start of the drain. <see cref="EndFrame(UiRoutingGate)"/> uses it for the delta.
    /// </summary>
    private float2 _startPosition;

    /// <summary>
    /// Bit <c>i</c>: the last down of button <c>i</c> went to the UI. <see cref="Record"/> writes it, and
    /// <see cref="EndFrame(UiRoutingGate)"/> hides those buttons in the game view (A3, Decision 8). The value
    /// survives across frames, so a press that the pointer drags outside the UI stays a UI press.
    /// </summary>
    private byte _uiPresses;

    /// <summary>
    /// Bit <c>i</c>: this frame holds an up of button <c>i</c> of a press that went to the UI. The game view drops
    /// those released flags (Decision 8). <see cref="BeginFrame"/> clears this mask; <see cref="_uiPresses"/> keeps
    /// its value.
    /// </summary>
    private byte _uiReleases;

    /// <summary>
    /// <see langword="true"/> when <see cref="RequestKeyClear"/> asked for a key clear.
    /// <see cref="UiContext.ClearHeldKeys"/> sets it through that method, and
    /// <see cref="EndFrame(UiRoutingGate)"/> applies it after the last entry and clears the flag (Decision 10).
    /// </summary>
    private bool _clearKeys;

    /// <summary>Creates a writer whose both views start with no pointer position.</summary>
    /// <returns>The writer. Both <see cref="UiContext"/> constructors call it.</returns>
    internal static InputStateWriter Create()
    {
        InputStateWriter writer = new();
        float2 noPosition = new(float.NaN);
        writer.Raw.PointerState.Move(noPosition);
        writer.Game.PointerState.Move(noPosition);
        writer._startPosition = noPosition;
        return writer;
    }

    /// <summary>
    /// Hook 1: remembers the start position, clears the delta, the scroll, the pressed and released button masks,
    /// the pressed and released key bits, and the release ownership mask. Keeps each down mask, the modifiers, the
    /// press ownership mask, and each gamepad slot with its edges.
    /// </summary>
    internal void BeginFrame()
    {
        _startPosition = Raw.PointerState.Position;
        Raw.PointerState.BeginFrame();
        Raw.ButtonState.BeginFrame();
        Raw.KeyState.BeginFrame();
        _uiReleases = 0;
    }

    /// <summary>Hook 2: writes one entry of the drain into the raw state.</summary>
    /// <param name="entry">The entry that left the ring.</param>
    /// <param name="hitUi">
    /// <see langword="true"/> when the entry has a target after the target step. A press with this value set belongs
    /// to the UI, and a press with it clear belongs to the game (A3).
    /// </param>
    /// <remarks>
    /// The switch reads the payload that the type names, because the payloads overlap
    /// (<see cref="UiEventPayload"/>). A button outside 0 to 7 changes no mask. A key code outside 0 to 348 changes
    /// no bit and still sets the modifiers. The position takes each value, also (NaN, NaN).
    /// </remarks>
    internal void Record(in UiQueuedEvent entry, bool hitUi)
    {
        switch (entry.Header.Type)
        {
            case UiEventType.PointerMove:
                Raw.PointerState.Move(entry.Payload.Pointer.Position);
                break;

            case UiEventType.PointerDown:
            {
                PointerButton button = entry.Payload.Pointer.Button;
                Raw.PointerState.Move(entry.Payload.Pointer.Position);
                Raw.ButtonState.Press(button);
                byte bit = UiPointerButtonState.BitOf(button);
                _uiPresses = hitUi ? (byte)(_uiPresses | bit) : (byte)(_uiPresses & ~bit);
                break;
            }

            case UiEventType.PointerUp:
            {
                PointerButton button = entry.Payload.Pointer.Button;
                Raw.PointerState.Move(entry.Payload.Pointer.Position);
                if (Raw.ButtonState.IsDown(button))
                {
                    _uiReleases |= (byte)(_uiPresses & UiPointerButtonState.BitOf(button));
                    Raw.ButtonState.Release(button);
                }

                break;
            }

            case UiEventType.Scroll:
                Raw.PointerState.Scroll(entry.Payload.Scroll.Position, entry.Payload.Scroll.Delta);
                break;

            case UiEventType.KeyDown:
                Raw.KeyState.KeyDown(
                    entry.Payload.Key.Key,
                    entry.Payload.Key.IsRepeat,
                    entry.Payload.Key.Modifiers);
                break;

            case UiEventType.KeyUp:
                Raw.KeyState.KeyUp(entry.Payload.Key.Key, entry.Payload.Key.Modifiers);
                break;

            default:
                // A char and a default entry change no value.
                break;
        }
    }

    /// <summary>
    /// Sets the key clear request of <see cref="UiContext.ClearHeldKeys"/> (Decision 10). The method sets a flag
    /// only; <see cref="EndFrame(UiRoutingGate)"/> applies it at the end of the drain, after the last entry.
    /// </summary>
    internal void RequestKeyClear()
    {
        _clearKeys = true;
    }

    /// <summary>
    /// Writes one raw gamepad slot and its edges: <c>Pressed = new &amp; ~old</c> and
    /// <c>Released = old &amp; ~new</c>, over the buttons of the old raw slot and of
    /// <paramref name="state"/> (Decision 11). The writer copies the connected flag, the buttons, and the six
    /// axes, and leaves the game view to the next <see cref="EndFrame(UiRoutingGate)"/>.
    /// </summary>
    /// <param name="index">The slot index. <see cref="UiContext.SetGamepad(int, in UiGamepadState)"/> checks the range 0 to 15.</param>
    /// <param name="state">The new state of the slot.</param>
    internal void SetGamepad(int index, in UiGamepadState state)
    {
        ref UiGamepadState slot = ref Unsafe.Add(ref Unsafe.AsRef(in Raw.GamepadState._slot), index);
        state.WriteEdgesInto(ref slot);
    }

    /// <summary>Hook 3: writes the delta of the frame and the game view at the end of the drain.</summary>
    /// <param name="gate">The routing gate at the end of the drain (A4).</param>
    /// <remarks>
    /// <para>
    /// The key clear runs first. When <see cref="_clearKeys"/> is set, <c>UiKeyState.ReleaseAll</c> releases each
    /// held key of the raw state, and the flag clears (Decision 10). The clear then covers a key down of the same
    /// frame, also one that came after the request.
    /// </para>
    /// <para>
    /// <see cref="Game"/> gets one copy of <see cref="Raw"/> (Decision 12), then the gate empties the parts that
    /// the gate closes. When <see cref="UiRoutingGate.PointerOverUi"/> is <see langword="true"/>, the pointer and
    /// button parts of the game view are empty: the position (NaN, NaN), the delta (0, 0), the scroll (0, 0), and
    /// zero button masks.
    /// </para>
    /// <para>
    /// With an open pointer gate, the ownership rule runs instead (Decision 8): the game view drops the down and
    /// pressed flags of <see cref="_uiPresses"/> and the released flags of <see cref="_uiReleases"/>. A press
    /// belongs to the side that got the down.
    /// </para>
    /// <para>
    /// When <see cref="UiRoutingGate.KeyboardFocusInUi"/> is <see langword="true"/>, the key part of the game view
    /// is <c>default</c>: no bit and the modifiers
    /// <see cref="UiModifiers.None"/>.
    /// </para>
    /// <para>
    /// The gamepad slots pass to the game view with no gate (Decision 11). The routing gate covers the pointer and
    /// the keyboard only, so the copy of <see cref="Raw"/> carries each slot and its edges.
    /// </para>
    /// <para>
    /// The drain calls this method in a <c>finally</c> block, so an exception of a handler still writes the state
    /// of the entries that ran.
    /// </para>
    /// </remarks>
    internal void EndFrame(UiRoutingGate gate)
    {
        if (_clearKeys)
        {
            Raw.KeyState.ReleaseAll();
            _clearKeys = false;
        }

        Raw.PointerState.FinishFrame(_startPosition);
        Game = Raw;
        if (gate.PointerOverUi)
        {
            float2 noPosition = new(float.NaN);
            Game.PointerState = default;
            Game.PointerState.Move(noPosition);
            Game.ButtonState = default;
        }
        else
        {
            Game.ButtonState.Mask(_uiPresses, _uiReleases);
        }

        if (gate.KeyboardFocusInUi)
        {
            Game.KeyState = default;
        }
    }
}
