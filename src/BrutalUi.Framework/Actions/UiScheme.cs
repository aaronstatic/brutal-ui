using System;

namespace BrutalUi.Framework;

/// <summary>
/// The names of the standard control schemes of a <see cref="UiActionMap"/>.
/// </summary>
/// <remarks>
/// <para>
/// A scheme name is UTF-8. The map compares two names by their bytes, so <c>Gamepad</c> and <c>gamepad</c> are two
/// names. The map holds both names in its table from construction, so
/// <see cref="UiActionMap.AddScheme(ReadOnlySpan{byte})"/> gives their id with no add. A game can add other names. A
/// custom name groups bindings only, because <see cref="UiActionMap.ActiveScheme"/> reports the device of the input.
/// </para>
/// <para>
/// Each property gives a span of constant data, so a read allocates nothing.
/// </para>
/// </remarks>
public static class UiScheme
{
    /// <summary>
    /// Gets the id of the default scheme: <c>0</c>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Id 0 is the default scheme. <see cref="UiActionMap.SchemeName(int)"/> gives an empty span for it, because the
    /// default scheme holds no name. A binding with id 0 belongs to each scheme. It reads under every lock, and it
    /// clashes with a tagged bind of the same input.
    /// </para>
    /// <para>
    /// A game writes <see cref="Default"/> in place of a hard-coded 0. The control-schemes review CR-8 deferred this
    /// member, and the binding-remap feature ships it.
    /// </para>
    /// </remarks>
    public const int Default = 0;

    /// <summary>Gets the name of the keyboard and mouse scheme: <c>KeyboardMouse</c>.</summary>
    public static ReadOnlySpan<byte> KeyboardMouse => "KeyboardMouse"u8;

    /// <summary>Gets the name of the gamepad scheme: <c>Gamepad</c>.</summary>
    public static ReadOnlySpan<byte> Gamepad => "Gamepad"u8;
}
