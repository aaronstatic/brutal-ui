using System;
using Brutal.Localization;

namespace BrutalUi.Core;

/// <summary>
/// The text group of the fluent builder: the <c>Text</c> methods of the widgets that own a text.
/// </summary>
/// <remarks>
/// <para>
/// Each overload calls the <see cref="Label.SetText(ReadOnlySpan{byte})"/> overload of the widget with the same
/// parameter type, then returns the receiver. A set to the same text marks nothing, as <c>SetText</c> does.
/// </para>
/// <para>
/// The analyzer <c>BRUTALCORE0011</c> rejects a string literal when a span overload exists, so write
/// <c>"..."u8</c>. A bare <c>null</c> is ambiguous between the overloads, so cast it, for example
/// <c>Text((string?)null)</c>. A <see langword="null"/> of <c>string?</c> or <c>LString?</c> gives an empty text.
/// </para>
/// </remarks>
public static partial class ElementBuilder
{
    /// <summary>Copies a UTF-8 text into the label and returns the label.</summary>
    /// <param name="label">The label to set.</param>
    /// <param name="utf8">The UTF-8 text. An empty span gives an empty text.</param>
    /// <returns>The same label.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="label"/> is <see langword="null"/>.</exception>
    public static Label Text(this Label label, ReadOnlySpan<byte> utf8)
    {
        ArgumentNullException.ThrowIfNull(label);
        label.SetText(utf8);
        return label;
    }

    /// <summary>Encodes a text to UTF-8 into the label and returns the label.</summary>
    /// <param name="label">The label to set.</param>
    /// <param name="text">The text. <see langword="null"/> gives an empty text.</param>
    /// <returns>The same label.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="label"/> is <see langword="null"/>.</exception>
    public static Label Text(this Label label, string? text)
    {
        ArgumentNullException.ThrowIfNull(label);
        label.SetText(text);
        return label;
    }

    /// <summary>Sets the localizable text of the label and returns the label.</summary>
    /// <param name="label">The label to set.</param>
    /// <param name="text">The localizable text. <see langword="null"/> gives an empty text.</param>
    /// <returns>The same label.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="label"/> is <see langword="null"/>.</exception>
    public static Label Text(this Label label, LString? text)
    {
        ArgumentNullException.ThrowIfNull(label);
        label.SetText(text);
        return label;
    }

    /// <summary>Copies a UTF-8 text into the button and returns the button.</summary>
    /// <param name="button">The button to set.</param>
    /// <param name="utf8">The UTF-8 text. An empty span gives an empty text.</param>
    /// <returns>The same button.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="button"/> is <see langword="null"/>.</exception>
    public static Button Text(this Button button, ReadOnlySpan<byte> utf8)
    {
        ArgumentNullException.ThrowIfNull(button);
        button.SetText(utf8);
        return button;
    }

    /// <summary>Encodes a text to UTF-8 into the button and returns the button.</summary>
    /// <param name="button">The button to set.</param>
    /// <param name="text">The text. <see langword="null"/> gives an empty text.</param>
    /// <returns>The same button.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="button"/> is <see langword="null"/>.</exception>
    public static Button Text(this Button button, string? text)
    {
        ArgumentNullException.ThrowIfNull(button);
        button.SetText(text);
        return button;
    }

    /// <summary>Sets the localizable text of the button and returns the button.</summary>
    /// <param name="button">The button to set.</param>
    /// <param name="text">The localizable text. <see langword="null"/> gives an empty text.</param>
    /// <returns>The same button.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="button"/> is <see langword="null"/>.</exception>
    public static Button Text(this Button button, LString? text)
    {
        ArgumentNullException.ThrowIfNull(button);
        button.SetText(text);
        return button;
    }

    /// <summary>Copies a UTF-8 text into the field and returns the field.</summary>
    /// <remarks>
    /// <see cref="TextField.SetText(ReadOnlySpan{byte})"/> applies the capacity rule of the field, so a long text gets
    /// the rule of <c>SetText</c>: the field keeps the longest prefix that fits its capacity and ends on a code point
    /// boundary.
    /// </remarks>
    /// <param name="field">The field to set.</param>
    /// <param name="utf8">The UTF-8 text. An empty span gives an empty text.</param>
    /// <returns>The same field.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="field"/> is <see langword="null"/>.</exception>
    public static TextField Text(this TextField field, ReadOnlySpan<byte> utf8)
    {
        ArgumentNullException.ThrowIfNull(field);
        field.SetText(utf8);
        return field;
    }

    /// <summary>Encodes a text to UTF-8 into the field and returns the field.</summary>
    /// <remarks>
    /// <see cref="TextField.SetText(string?)"/> applies the capacity rule of the field, so a long text gets the rule of
    /// <c>SetText</c>: the field keeps the longest prefix that fits its capacity and ends on a code point boundary.
    /// </remarks>
    /// <param name="field">The field to set.</param>
    /// <param name="text">The text. <see langword="null"/> gives an empty text.</param>
    /// <returns>The same field.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="field"/> is <see langword="null"/>.</exception>
    public static TextField Text(this TextField field, string? text)
    {
        ArgumentNullException.ThrowIfNull(field);
        field.SetText(text);
        return field;
    }

    /// <summary>Sets the text to the localized form of a localizable text and returns the field.</summary>
    /// <remarks>
    /// <see cref="TextField.SetText(LString?)"/> reads <see cref="LString.Localized"/> one time, keeps no
    /// <see cref="LString"/>, and applies the capacity rule of the field, so a long text gets the rule of
    /// <c>SetText</c>.
    /// </remarks>
    /// <param name="field">The field to set.</param>
    /// <param name="text">The localizable text. <see langword="null"/> gives an empty text.</param>
    /// <returns>The same field.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="field"/> is <see langword="null"/>.</exception>
    public static TextField Text(this TextField field, LString? text)
    {
        ArgumentNullException.ThrowIfNull(field);
        field.SetText(text);
        return field;
    }
}
