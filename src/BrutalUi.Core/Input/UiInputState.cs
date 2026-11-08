using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Brutal.Numerics;

namespace BrutalUi.Core;

/// <summary>
/// The input state of the last <see cref="UiContext.Update(float)"/> call: the pointer position, the pointer delta,
/// the scroll sum, the mouse button flags, the key flags, and the gamepad slots. The drain writes it inside
/// <see cref="UiContext.Update(float)"/>, entry by entry.
/// </summary>
/// <remarks>
/// <para>
/// The game reads this state after <see cref="UiContext.Update(float)"/> through <see cref="UiContext.Input"/> or
/// <see cref="UiContext.GameInput"/>. The value is fixed after the call, so two reads in one frame agree.
/// </para>
/// <para>
/// A read through the <see langword="ref readonly"/> members copies nothing. A copy of the struct is a snapshot:
/// it keeps the values of that moment and changes no later state.
/// </para>
/// <para>
/// The struct holds the pointer part, the button part, the key part, and 16 gamepad slots. The game reads a slot
/// through <see cref="Gamepad(int)"/>; <see cref="UiContext.SetGamepad(int, in UiGamepadState)"/> writes a slot and
/// its edges.
/// </para>
/// </remarks>
public struct UiInputState
{
    /// <summary>The number of gamepad slots: the indexes 0 to 15.</summary>
    public const int GamepadCount = GamepadSlots.SlotCount;

    /// <summary>The pointer part. The writer fills it. The game reads it through <see cref="Pointer"/>.</summary>
    internal UiPointerState PointerState;

    /// <summary>The button part. The writer fills it. The game reads it through <see cref="Buttons"/>.</summary>
    internal UiPointerButtonState ButtonState;

    /// <summary>The key part. The writer fills it. The game reads it through <see cref="Keys"/>.</summary>
    internal UiKeyState KeyState;

    /// <summary>
    /// The gamepad slots. <see cref="UiContext.SetGamepad(int, in UiGamepadState)"/> fills them. The game reads
    /// each slot through <see cref="Gamepad(int)"/>.
    /// </summary>
    internal GamepadSlots GamepadState;

    /// <summary>Gets the pointer part: the position, the delta, and the scroll of the pointer.</summary>
    /// <remarks>The read copies nothing.</remarks>
    [UnscopedRef]
    public readonly ref readonly UiPointerState Pointer => ref PointerState;

    /// <summary>Gets the button part: the flags of each mouse button.</summary>
    /// <remarks>The read copies nothing.</remarks>
    [UnscopedRef]
    public readonly ref readonly UiPointerButtonState Buttons => ref ButtonState;

    /// <summary>Gets the key part: the flags of each key code and the modifiers of the last key event.</summary>
    /// <remarks>The read copies nothing.</remarks>
    [UnscopedRef]
    public readonly ref readonly UiKeyState Keys => ref KeyState;

    /// <summary>Gets the state of one gamepad slot.</summary>
    /// <param name="index">The slot index, 0 to 15.</param>
    /// <returns>The slot. The read copies nothing.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="index"/> is outside 0 to 15. The parameter name is <c>index</c>.
    /// </exception>
    /// <remarks>
    /// Each slot of a new context is disconnected. <see cref="UiContext.SetGamepad(int, in UiGamepadState)"/>
    /// writes a slot and its edges. The drain keeps the edges, and the game view copies the slots with no gate.
    /// </remarks>
    [UnscopedRef]
    public readonly ref readonly UiGamepadState Gamepad(int index)
    {
        if ((uint)index >= (uint)GamepadCount)
        {
            throw new ArgumentOutOfRangeException(nameof(index));
        }

        return ref Unsafe.Add(ref Unsafe.AsRef(in GamepadState._slot), index);
    }
}

/// <summary>
/// Sixteen <see cref="UiGamepadState"/> slots hold the gamepad part of <see cref="UiInputState"/>. Each slot of a
/// new state starts disconnected.
/// </summary>
[InlineArray(GamepadSlots.SlotCount)]
internal struct GamepadSlots
{
    /// <summary>The number of slots in one state.</summary>
    internal const int SlotCount = 16;

    /// <summary>The first slot. <see cref="UiInputState"/> indexes the slots with <c>Unsafe.Add</c>.</summary>
    internal UiGamepadState _slot;
}

/// <summary>
/// The pointer part of <see cref="UiInputState"/>: the pointer position of the last entry, the delta since the last
/// <see cref="UiContext.Update(float)"/> call, and the scroll sum of the frame.
/// </summary>
/// <remarks>
/// The drain writes the values. <c>InputStateWriter</c> calls <see cref="Move(float2)"/> for each pointer entry,
/// <see cref="Scroll(float2, float2)"/> for each scroll entry, <see cref="BeginFrame"/> at the start of the drain,
/// and <see cref="FinishFrame(float2)"/> at the end of the drain.
/// </remarks>
public struct UiPointerState
{
    private float2 _position;
    private float2 _delta;
    private float2 _scroll;

    /// <summary>
    /// Gets the position of the last pointer or scroll entry, in the coordinates of the context.
    /// </summary>
    /// <remarks>
    /// The value is (NaN, NaN) before the first pointer entry of the context and after a cursor leave. The adapter
    /// pushes a leave as a move at (NaN, NaN).
    /// </remarks>
    public readonly float2 Position => _position;

    /// <summary>
    /// Gets the position of this <see cref="UiContext.Update(float)"/> call minus the position of the last one.
    /// </summary>
    /// <remarks>
    /// The value is (0, 0) when the start position or the end position holds NaN (A5). A frame with no pointer
    /// entry also gives (0, 0) and keeps the position.
    /// </remarks>
    public readonly float2 Delta => _delta;

    /// <summary>
    /// Gets the scroll sum of this <see cref="UiContext.Update(float)"/> call: the sum of the delta of each scroll
    /// entry of the frame.
    /// </summary>
    /// <remarks>
    /// The value is (0, 0) in a frame with no scroll entry. The game view holds (0, 0) when the pointer is over the
    /// UI, because the gate empties the pointer part.
    /// </remarks>
    public readonly float2 ScrollDelta => _scroll;

    /// <summary>Writes the position of one entry.</summary>
    /// <param name="position">
    /// The position in the coordinates of the context, or (NaN, NaN) for a cursor leave.
    /// </param>
    internal void Move(float2 position)
    {
        _position = position;
    }

    /// <summary>Writes the position of one scroll entry and adds its delta to the scroll sum of the frame.</summary>
    /// <param name="position">The position in the coordinates of the context.</param>
    /// <param name="delta">The scroll amount of the entry.</param>
    internal void Scroll(float2 position, float2 delta)
    {
        _position = position;
        _scroll += delta;
    }

    /// <summary>Clears the delta and the scroll sum at the start of the drain, and keeps the position.</summary>
    internal void BeginFrame()
    {
        _delta = default;
        _scroll = default;
    }

    /// <summary>Writes the delta of the frame.</summary>
    /// <param name="start">
    /// The position at the start of the drain: the position of the last <see cref="UiContext.Update(float)"/> call.
    /// </param>
    internal void FinishFrame(float2 start)
    {
        _delta = IsFinite(start) && IsFinite(_position) ? _position - start : default;
    }

    private static bool IsFinite(float2 value)
    {
        return float.IsFinite(value.X) && float.IsFinite(value.Y);
    }
}

/// <summary>
/// The button part of <see cref="UiInputState"/>: the down, pressed, and released flags of each GLFW mouse button.
/// </summary>
/// <remarks>
/// <para>
/// Three masks of eight bits cover the GLFW buttons 0 to 7. Bit <c>i</c> is the GLFW button <c>i</c>. A query with
/// a value outside that range returns <see langword="false"/> and throws no exception. A write with such a value
/// changes no mask (Decision 7).
/// </para>
/// <para>
/// The frame rule. <c>InputStateWriter</c> calls <see cref="Press(PointerButton)"/> for each down entry and
/// <see cref="Release(PointerButton)"/> for each up entry. <see cref="BeginFrame"/> clears the pressed mask and the
/// released mask at the start of the drain, and keeps the down mask. An up sets the released flag only for a button
/// that is down (Decision 7). A press and a release in one frame set both flags and clear the down flag.
/// </para>
/// <para>
/// The ownership rule (A3 and Decision 8). A press belongs to the side that got the down. The game view drops the
/// down and pressed flags of a button that the UI owns, and the released flag of each up of such a press in the
/// frame. The raw state keeps every flag.
/// </para>
/// </remarks>
public struct UiPointerButtonState
{
    /// <summary>The number of buttons that the masks cover: the GLFW buttons 0 to 7.</summary>
    public const int ButtonCount = 8;

    private byte _down;
    private byte _pressed;
    private byte _released;

    /// <summary>Gets a value that shows whether the button is down at the end of the frame.</summary>
    /// <param name="button">The button. A value outside 0 to 7 gives <see langword="false"/>.</param>
    /// <returns>
    /// <see langword="true"/> when a down of the button ran and no up of it ran after that down.
    /// </returns>
    public readonly bool IsDown(PointerButton button) => Holds(_down, button);

    /// <summary>Gets a value that shows whether the button went down in this frame.</summary>
    /// <param name="button">The button. A value outside 0 to 7 gives <see langword="false"/>.</param>
    /// <returns>
    /// <see langword="true"/> when the drain wrote a down entry of the button in this frame.
    /// </returns>
    public readonly bool WasPressed(PointerButton button) => Holds(_pressed, button);

    /// <summary>Gets a value that shows whether the button went up in this frame.</summary>
    /// <param name="button">The button. A value outside 0 to 7 gives <see langword="false"/>.</param>
    /// <returns>
    /// <see langword="true"/> when the drain wrote an up entry of a down button in this frame.
    /// </returns>
    public readonly bool WasReleased(PointerButton button) => Holds(_released, button);

    /// <summary>Sets the down flag and the pressed flag of one button.</summary>
    /// <param name="button">The button. A value outside 0 to 7 changes no mask.</param>
    internal void Press(PointerButton button)
    {
        byte bit = BitOf(button);
        _down |= bit;
        _pressed |= bit;
    }

    /// <summary>
    /// Clears the down flag of one button. The method sets the released flag only when the down flag is set.
    /// </summary>
    /// <param name="button">The button. A value outside 0 to 7 changes no mask.</param>
    internal void Release(PointerButton button)
    {
        byte bit = BitOf(button);
        if ((_down & bit) != 0)
        {
            _released |= bit;
            _down &= (byte)~bit;
        }
    }

    /// <summary>Clears the pressed mask and the released mask at the start of the drain, and keeps the down mask.</summary>
    internal void BeginFrame()
    {
        _pressed = 0;
        _released = 0;
    }

    /// <summary>
    /// Drops the flags that the ownership rule hides in the game view (Decision 8).
    /// </summary>
    /// <param name="presses">The bits of the buttons whose last down went to the UI.</param>
    /// <param name="releases">The bits of the buttons whose up in this frame belongs to a down that went to the UI.</param>
    internal void Mask(byte presses, byte releases)
    {
        _down &= (byte)~presses;
        _pressed &= (byte)~presses;
        _released &= (byte)~releases;
    }

    /// <summary>
    /// Returns the mask bit of one button, or 0 for a value outside 0 to 7. The writer uses it for the two
    /// ownership masks of <c>InputStateWriter</c>.
    /// </summary>
    internal static byte BitOf(PointerButton button)
    {
        int index = (int)button;
        return (uint)index < (uint)ButtonCount ? (byte)(1 << index) : (byte)0;
    }

    /// <summary>Returns <see langword="true"/> when the mask holds the bit of one button.</summary>
    private static bool Holds(byte mask, PointerButton button) => (mask & BitOf(button)) != 0;
}

/// <summary>
/// Eleven <see cref="uint"/> words hold 352 key bits for <see cref="UiKeyState"/>. The codes 0 to 348 use 349 of
/// them.
/// </summary>
[InlineArray(KeyBits.WordCount)]
internal struct KeyBits
{
    /// <summary>The number of words in the bitset.</summary>
    internal const int WordCount = 11;

    private uint _word;
}

/// <summary>
/// The key part of <see cref="UiInputState"/>: the down, pressed, and released flags of each GLFW key code, and the
/// modifiers of the last key event.
/// </summary>
/// <remarks>
/// <para>
/// Three bitsets cover the GLFW key codes 0 to 348 (A1). <see cref="KeyCount"/> is 349. A code outside that range,
/// also -1, reads <see langword="false"/> with no exception and changes no bit (Decision 7). A key event with such
/// a code still sets the modifiers.
/// </para>
/// <para>
/// The frame rule. <c>InputStateWriter</c> calls <see cref="KeyDown(int, bool, UiModifiers)"/> for each down entry
/// and <see cref="KeyUp(int, UiModifiers)"/> for each up entry. <see cref="BeginFrame"/> clears the pressed bits and
/// the released bits at the start of the drain, and keeps the down bits and the modifiers. An up sets the released
/// flag only for a key that is down.
/// </para>
/// <para>
/// The repeat rule (Decision 9). A repeat sets the down flag and no pressed flag. A repeat of a key that is not
/// down sets the down flag and no pressed flag.
/// </para>
/// <para>
/// The modifier rule (A6). <see cref="Modifiers"/> comes from the last key event and stays until the next key
/// event.
/// </para>
/// <para>
/// The clear rule. <see cref="ReleaseAll"/> sets the released flag of each held key, clears each down flag, and
/// sets the modifiers to <see cref="UiModifiers.None"/>. <see cref="UiContext.ClearHeldKeys"/> asks for it at the
/// end of the next drain (Decision 10).
/// </para>
/// </remarks>
public struct UiKeyState
{
    /// <summary>The number of key codes that the bitsets cover: the GLFW key codes 0 to 348.</summary>
    public const int KeyCount = 349;

    private KeyBits _down;
    private KeyBits _pressed;
    private KeyBits _released;
    private UiModifiers _modifiers;

    /// <summary>Gets the modifier keys of the last key event.</summary>
    /// <remarks>
    /// The value comes from the last key event, also a key event with a code outside 0 to 348 (A6), and stays until
    /// the next key event. <see cref="ReleaseAll"/> and the game-view gate give <see cref="UiModifiers.None"/>.
    /// </remarks>
    public readonly UiModifiers Modifiers => _modifiers;

    /// <summary>Gets a value that shows whether the key is down at the end of the frame.</summary>
    /// <param name="key">The GLFW key code. A code outside 0 to 348, also -1, gives <see langword="false"/>.</param>
    /// <returns>
    /// <see langword="true"/> when a down of the key ran and no up of it ran after that down.
    /// </returns>
    public readonly bool IsDown(int key) => Holds(in _down, key);

    /// <summary>Gets a value that shows whether the key went down in this frame.</summary>
    /// <param name="key">The GLFW key code. A code outside 0 to 348, also -1, gives <see langword="false"/>.</param>
    /// <returns>
    /// <see langword="true"/> when the drain wrote a down entry of the key in this frame with no repeat.
    /// </returns>
    public readonly bool WasPressed(int key) => Holds(in _pressed, key);

    /// <summary>Gets a value that shows whether the key went up in this frame.</summary>
    /// <param name="key">The GLFW key code. A code outside 0 to 348, also -1, gives <see langword="false"/>.</param>
    /// <returns>
    /// <see langword="true"/> when the drain wrote an up entry of a down key in this frame, or when
    /// <see cref="ReleaseAll"/> cleared a down key.
    /// </returns>
    public readonly bool WasReleased(int key) => Holds(in _released, key);

    /// <summary>Sets the down flag of one key, and the pressed flag when the event is no repeat.</summary>
    /// <param name="key">The GLFW key code. A code outside 0 to 348 changes no bit.</param>
    /// <param name="repeat"><see langword="true"/> when the event comes from a key repeat.</param>
    /// <param name="modifiers">The modifier keys of the event. The method keeps them for every code.</param>
    internal void KeyDown(int key, bool repeat, UiModifiers modifiers)
    {
        if ((uint)key < (uint)KeyCount)
        {
            Set(ref _down, key);
            if (!repeat)
            {
                Set(ref _pressed, key);
            }
        }

        _modifiers = modifiers;
    }

    /// <summary>
    /// Clears the down flag of one key. The method sets the released flag only when the down flag is set.
    /// </summary>
    /// <param name="key">The GLFW key code. A code outside 0 to 348 changes no bit.</param>
    /// <param name="modifiers">The modifier keys of the event. The method keeps them for every code.</param>
    internal void KeyUp(int key, UiModifiers modifiers)
    {
        if ((uint)key < (uint)KeyCount && Holds(in _down, key))
        {
            Set(ref _released, key);
            Clear(ref _down, key);
        }

        _modifiers = modifiers;
    }

    /// <summary>
    /// Clears the pressed bits and the released bits at the start of the drain, and keeps the down bits and the
    /// modifiers.
    /// </summary>
    internal void BeginFrame()
    {
        _pressed = default;
        _released = default;
    }

    /// <summary>
    /// Sets the released flag of each held key, clears each down flag, and sets the modifiers to
    /// <see cref="UiModifiers.None"/> (Decision 10). <c>InputStateWriter.EndFrame</c> calls it when
    /// <see cref="UiContext.ClearHeldKeys"/> asked for the clear.
    /// </summary>
    internal void ReleaseAll()
    {
        for (int word = 0; word < KeyBits.WordCount; word++)
        {
            uint held = _down[word];
            _released[word] |= held;
            _down[word] = 0;
        }

        _modifiers = UiModifiers.None;
    }

    /// <summary>Returns <see langword="true"/> when the bitset holds the bit of one code.</summary>
    private static bool Holds(in KeyBits bits, int key)
    {
        return (uint)key < (uint)KeyCount && (bits[key >> 5] & (1u << (key & 31))) != 0;
    }

    /// <summary>Sets the bit of one code. The caller checks the range first.</summary>
    private static void Set(ref KeyBits bits, int key)
    {
        bits[key >> 5] |= 1u << (key & 31);
    }

    /// <summary>Clears the bit of one code. The caller checks the range first.</summary>
    private static void Clear(ref KeyBits bits, int key)
    {
        bits[key >> 5] &= ~(1u << (key & 31));
    }
}
