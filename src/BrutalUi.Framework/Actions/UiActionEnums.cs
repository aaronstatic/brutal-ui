namespace BrutalUi.Framework;

/// <summary>
/// The kind of value that an action of a <see cref="UiActionMap"/> holds. The type decides the read of each frame and
/// the query that reads the value.
/// </summary>
public enum UiActionType : byte
{
    /// <summary>
    /// A press and a release, with no float value. The map keeps the <c>Pressed</c>, <c>Held</c>, and <c>Released</c>
    /// flags, and a query reads each flag with <see cref="UiActionMap.WasPressed"/>, <see cref="UiActionMap.IsHeld"/>,
    /// and <see cref="UiActionMap.WasReleased"/>.
    /// <see cref="UiActionMap.BindKey(System.ReadOnlySpan{System.Byte}, int, BrutalUi.Core.UiModifiers)"/>,
    /// <see cref="UiActionMap.BindMouseButton(System.ReadOnlySpan{System.Byte}, BrutalUi.Core.PointerButton, BrutalUi.Core.UiModifiers)"/>, and
    /// <see cref="UiActionMap.BindGamepadButton(System.ReadOnlySpan{System.Byte}, BrutalUi.Core.UiGamepadButtons)"/> need this type.
    /// </summary>
    Button = 0,

    /// <summary>
    /// One float value. A query reads it with <see cref="UiActionMap.ReadAxis"/>.
    /// <see cref="UiActionMap.BindKeyAxis(System.ReadOnlySpan{System.Byte}, int, int)"/>,
    /// <see cref="UiActionMap.BindGamepadAxis(System.ReadOnlySpan{System.Byte}, BrutalUi.Core.UiGamepadAxis, float)"/>, and
    /// <see cref="UiActionMap.BindScroll(System.ReadOnlySpan{System.Byte}, float)"/> need this type.
    /// </summary>
    Axis = 1,

    /// <summary>
    /// Two float values in one <c>float2</c>. A query reads it with <see cref="UiActionMap.ReadVector"/>.
    /// <see cref="UiActionMap.BindKeyVector(System.ReadOnlySpan{System.Byte}, int, int, int, int)"/>,
    /// <see cref="UiActionMap.BindGamepadStick(System.ReadOnlySpan{System.Byte}, BrutalUi.Framework.UiGamepadStick)"/>, and
    /// <see cref="UiActionMap.BindMouseDelta(System.ReadOnlySpan{System.Byte}, float)"/> need this type.
    /// </summary>
    Vector2 = 2,
}

/// <summary>
/// The two sticks of a gamepad.
/// <see cref="UiActionMap.BindGamepadStick(System.ReadOnlySpan{System.Byte}, BrutalUi.Framework.UiGamepadStick)"/> binds one stick to a
/// <see cref="UiActionType.Vector2"/> action.
/// </summary>
/// <remarks>
/// The sticks map to the six axes of <see cref="BrutalUi.Core.UiGamepadState"/>: <see cref="Left"/> is the pair
/// <see cref="BrutalUi.Core.UiGamepadAxis.LeftStickX"/> and <see cref="BrutalUi.Core.UiGamepadAxis.LeftStickY"/>, and
/// <see cref="Right"/> is the pair <see cref="BrutalUi.Core.UiGamepadAxis.RightStickX"/> and
/// <see cref="BrutalUi.Core.UiGamepadAxis.RightStickY"/>.
/// </remarks>
public enum UiGamepadStick : byte
{
    /// <summary>The left stick.</summary>
    Left = 0,

    /// <summary>The right stick.</summary>
    Right = 1,
}
