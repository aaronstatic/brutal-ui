using System;
using System.Runtime.CompilerServices;

namespace BrutalUi.Core;

/// <summary>
/// Six <see cref="float"/> values hold the axes of one gamepad slot for <see cref="UiGamepadState"/>.
/// </summary>
[InlineArray(GamepadAxes.AxisCount)]
internal struct GamepadAxes
{
    /// <summary>The number of axes in one slot.</summary>
    internal const int AxisCount = 6;

    private float _axis;
}

/// <summary>
/// The state of one gamepad slot: the connected flag, the buttons, the pressed and released edges, and six axes.
/// </summary>
/// <remarks>
/// <para>
/// A2 of this feature and of the input epic: the type holds the full gamepad shape, and this feature writes no
/// gamepad value itself. Core names no GLFW type.
/// </para>
/// <para>
/// Bit <c>i</c> of <see cref="Buttons"/> is the GLFW gamepad button <c>i</c>, so the adapter converts the GLFW
/// button values with no table.
/// </para>
/// <para>
/// <see cref="Axis(int)"/> gives the GLFW axis <c>i</c> with no deadzone; the gamepad-axes feature applies the
/// deadzone.
/// </para>
/// <para>
/// <c>default(UiGamepadState)</c> is a disconnected slot with no button and no axis.
/// <see cref="UiContext.SetGamepad(int, in UiGamepadState)"/> writes <see cref="Pressed"/> and
/// <see cref="Released"/> from the old buttons of the slot; the constructor leaves both
/// <see cref="UiGamepadButtons.None"/>.
/// </para>
/// <para>The struct is 40 bytes, and the slots of <see cref="UiInputState"/> start disconnected.</para>
/// </remarks>
public struct UiGamepadState
{
    /// <summary>The number of axes in one slot.</summary>
    public const int AxisCount = 6;

    private bool _connected;
    private UiGamepadButtons _buttons;
    private UiGamepadButtons _pressed;
    private UiGamepadButtons _released;
    private GamepadAxes _axes;

    /// <summary>
    /// Creates a connected state with the given buttons and no edges. <see cref="UiContext.SetGamepad"/> computes
    /// the edges.
    /// </summary>
    /// <param name="buttons">The buttons that are down.</param>
    /// <param name="axes">
    /// The axis values, in the GLFW axis order. The constructor copies at most six values and fills the rest
    /// with 0.
    /// </param>
    public UiGamepadState(UiGamepadButtons buttons, ReadOnlySpan<float> axes)
    {
        _connected = true;
        _buttons = buttons;
        _pressed = UiGamepadButtons.None;
        _released = UiGamepadButtons.None;
        _axes = default;
        for (int index = 0; index < Math.Min(axes.Length, AxisCount); index++)
        {
            _axes[index] = axes[index];
        }
    }

    /// <summary>Gets a value that shows whether a gamepad occupies the slot.</summary>
    /// <remarks>
    /// <c>default(UiGamepadState)</c> and every slot of a new context give <see langword="false"/>.
    /// The constructor gives <see langword="true"/>.
    /// </remarks>
    public readonly bool Connected => _connected;

    /// <summary>Gets the buttons that are down.</summary>
    public readonly UiGamepadButtons Buttons => _buttons;

    /// <summary>Gets the buttons that went down since the last <see cref="UiContext.SetGamepad"/> call.</summary>
    /// <remarks><see cref="UiContext.SetGamepad"/> writes the edges; the constructor leaves them <see cref="UiGamepadButtons.None"/>.</remarks>
    public readonly UiGamepadButtons Pressed => _pressed;

    /// <summary>Gets the buttons that went up since the last <see cref="UiContext.SetGamepad"/> call.</summary>
    /// <remarks><see cref="UiContext.SetGamepad"/> writes the edges; the constructor leaves them <see cref="UiGamepadButtons.None"/>.</remarks>
    public readonly UiGamepadButtons Released => _released;

    /// <summary>Gets the value of one axis, with no deadzone applied.</summary>
    /// <param name="axis">The GLFW axis index, 0 to 5.</param>
    /// <returns>The axis value that the last write stored.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="axis"/> is outside 0 to 5. The parameter name is <c>axis</c>.
    /// </exception>
    /// <remarks>The gamepad-axes feature applies the deadzone; this query returns the stored value.</remarks>
    public readonly float Axis(int axis)
    {
        if ((uint)axis >= (uint)AxisCount)
        {
            throw new ArgumentOutOfRangeException(nameof(axis));
        }

        return _axes[axis];
    }

    /// <summary>
    /// Writes the buttons, the axes, and the edges into the given slot:
    /// <c>Pressed = new &amp; ~old</c> and <c>Released = old &amp; ~new</c>, over the buttons of this value and of
    /// the old slot. <see cref="UiContext.SetGamepad"/> calls it through <c>InputStateWriter.SetGamepad</c>.
    /// </summary>
    internal readonly void WriteEdgesInto(ref UiGamepadState slot)
    {
        ref GamepadAxes target = ref slot._axes;
        ref readonly GamepadAxes source = ref _axes;
        target = source;
        slot._connected = _connected;
        slot._pressed = _buttons & ~slot._buttons;
        slot._released = slot._buttons & ~_buttons;
        slot._buttons = _buttons;
    }
}
