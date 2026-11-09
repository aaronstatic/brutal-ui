using System;

namespace BrutalUi.Framework;

/// <summary>
/// The names of the standard actions of a <see cref="UiActionMap"/>.
/// </summary>
/// <remarks>
/// <para>
/// An action name is UTF-8. The map compares two names by their bytes, so <c>Cancel</c> and <c>cancel</c> are two
/// names. A game can use other names, and each name needs an add with
/// <see cref="UiActionMap.AddAction(ReadOnlySpan{byte}, ReadOnlySpan{byte})"/> or
/// <see cref="UiActionMap.AddAction(ReadOnlySpan{byte}, UiActionType)"/>.
/// </para>
/// <para>
/// The names are the words of the requirement: confirm, cancel, navigate up, and navigate down. Each property gives a
/// span of constant data, so a read allocates nothing.
/// </para>
/// </remarks>
public static class UiActions
{
    /// <summary>Gets the name of the action that confirms a choice: <c>Confirm</c>.</summary>
    public static ReadOnlySpan<byte> Confirm => "Confirm"u8;

    /// <summary>Gets the name of the action that cancels or closes: <c>Cancel</c>.</summary>
    public static ReadOnlySpan<byte> Cancel => "Cancel"u8;

    /// <summary>Gets the name of the action that moves up: <c>NavigateUp</c>.</summary>
    public static ReadOnlySpan<byte> NavigateUp => "NavigateUp"u8;

    /// <summary>Gets the name of the action that moves down: <c>NavigateDown</c>.</summary>
    public static ReadOnlySpan<byte> NavigateDown => "NavigateDown"u8;
}
