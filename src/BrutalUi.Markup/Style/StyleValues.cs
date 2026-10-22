using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text;
using BrutalUi.Core;

namespace BrutalUi.Markup;

/// <summary>
/// Reads the value forms of the stylesheet format from the tokens of a declaration.
/// </summary>
/// <remarks>
/// <para>
/// Each read takes the source span that the tokenizer read and a token of that span. A read gives the value, or a
/// message that names the problem and the fix. The caller makes the <see cref="StyleParseError"/> at the token, so
/// the error position is the first byte of the token at fault.
/// </para>
/// <para>
/// The forms: the color, the length, the number, the keyword, the edge set and the corner set (one to four values
/// in the CSS order), the font size, the font, and the transition list. The CSS order of an edge set differs from the field order of
/// <see cref="LengthEdges"/>, so <see cref="MapEdges"/> and <see cref="MapCorners"/> are the one place that maps it.
/// </para>
/// <para>
/// The range checks: <c>padding</c>, <c>border-width</c>, <c>border-radius</c>, <c>flex-grow</c>,
/// <c>flex-shrink</c>, and <c>font-size</c> take 0 or more. <c>opacity</c> takes 0 to 1. <c>padding</c> has no
/// <c>auto</c>. <c>border-width</c>, <c>border-radius</c>, and <c>font-size</c> take <c>px</c> or a bare <c>0</c>
/// only. Each check gives <see cref="StyleErrorKind.InvalidValue"/> at the token at fault.
/// </para>
/// <para>
/// A read allocates only for the message of an error and for the text of a keyword. The parser runs at load time
/// only.
/// </para>
/// </remarks>
internal static class StyleValues
{
    /// <summary>The units and the values that a length form accepts besides <c>px</c> values of 0 or more.</summary>
    [Flags]
    private enum Extra
    {
        None = 0,
        Percent = 1,
        Auto = 2,
        Negative = 4,
        Any = Percent | Auto | Negative,
    }

    /// <summary>
    /// Reads the value of one declaration and writes it through the setter of the member.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The read starts at <paramref name="index"/>, which is the first value token. A one-value form reads one token.
    /// An edge set or a corner set reads one to four values and stops at the first <c>;</c>, <c>}</c>, or end token.
    /// The read gives the index of the token after the last value, or after the whitespace that follows it.
    /// The caller checks the token at that index.
    /// </para>
    /// <para>The error holds the index of the first token at fault, so the caller makes the error at that token.</para>
    /// </remarks>
    /// <param name="source">The source span that the tokenizer read.</param>
    /// <param name="tokens">The token list.</param>
    /// <param name="index">The index of the first value token. The read moves it forward.</param>
    /// <param name="entry">The row of the property.</param>
    /// <param name="fonts">The font map of the parser.</param>
    /// <param name="properties">The set of the rule.</param>
    /// <param name="transitions">
    /// The transition set of the rule. Only the <see cref="ValueForm.Transition"/> form writes it, and only when the
    /// full value is correct.
    /// </param>
    /// <param name="error">The error when the read fails.</param>
    /// <returns><see langword="true"/> when the value is correct.</returns>
    public static bool TryRead(
        ReadOnlySpan<byte> source,
        List<StyleToken> tokens,
        ref int index,
        in PropertyEntry entry,
        Dictionary<string, FontHandle> fonts,
        ref StyleProperties properties,
        ref StyleTransitions transitions,
        out ValueError error)
    {
        error = default;
        StyleToken token = tokens[index];

        switch (entry.Form)
        {
            case ValueForm.Transition:
                return TryReadTransitions(source, tokens, ref index, ref transitions, out error);

            case ValueForm.Length:
            {
                if (!TryReadLength(source, in token, Extra.Any, entry.Name, out Length length, out string? message))
                {
                    error = new ValueError(StyleErrorKind.InvalidValue, index, message);
                    return false;
                }

                SetLength(ref properties, entry.Property, length);
                index++;
                return true;
            }

            case ValueForm.Margin:
            case ValueForm.Padding:
            case ValueForm.BorderWidth:
            {
                // A margin can be negative or auto. A padding has no auto and no negative value. A border width
                // takes px values of 0 or more only, because Yoga has no percent border.
                Extra extra = entry.Form switch
                {
                    ValueForm.Margin => Extra.Any,
                    ValueForm.Padding => Extra.Percent,
                    _ => Extra.None,
                };
                Span<Length> values = stackalloc Length[4];
                if (!TryReadList(source, tokens, ref index, extra, entry.Name, values, out int count, out error))
                {
                    return false;
                }

                LengthEdges edges = MapEdges(values, count);
                switch (entry.Form)
                {
                    case ValueForm.Margin:
                        properties.Margin = edges;
                        break;
                    case ValueForm.Padding:
                        properties.Padding = edges;
                        break;
                    default:
                        properties.BorderWidth = edges;
                        break;
                }

                return true;
            }

            case ValueForm.Corners:
            {
                Span<Length> values = stackalloc Length[4];
                if (!TryReadList(source, tokens, ref index, Extra.None, entry.Name, values, out int count, out error))
                {
                    return false;
                }

                properties.CornerRadius = MapCorners(values, count);
                return true;
            }

            case ValueForm.Number:
            case ValueForm.Opacity:
            {
                if (token.Kind != StyleTokenKind.Number)
                {
                    error = new ValueError(
                        StyleErrorKind.InvalidValue,
                        index,
                        Describe(source, in token) + " is not a number. Write a number for " + entry.Name + ", for example '1'.");
                    return false;
                }

                if (!TryReadNumber(source, in token, out float number, out string? message))
                {
                    error = new ValueError(StyleErrorKind.InvalidValue, index, message);
                    return false;
                }

                if (entry.Form == ValueForm.Opacity && (number < 0f || number > 1f))
                {
                    error = new ValueError(
                        StyleErrorKind.InvalidValue,
                        index,
                        Describe(source, in token) + " is outside the range 0 to 1. Write a number from 0 to 1 for " + entry.Name + ", for example '0.5'.");
                    return false;
                }

                if (number < 0f)
                {
                    error = new ValueError(
                        StyleErrorKind.InvalidValue,
                        index,
                        Describe(source, in token) + " is a negative number. Write a number of 0 or more for " + entry.Name + ", for example '1'.");
                    return false;
                }

                switch (entry.Property)
                {
                    case StyleProperty.FlexGrow:
                        properties.FlexGrow = number;
                        break;
                    case StyleProperty.FlexShrink:
                        properties.FlexShrink = number;
                        break;
                    default:
                        properties.Opacity = number;
                        break;
                }

                index++;
                return true;
            }

            case ValueForm.FontSize:
            {
                if (!TryReadLength(source, in token, Extra.None, entry.Name, out Length length, out string? message))
                {
                    error = new ValueError(StyleErrorKind.InvalidValue, index, message);
                    return false;
                }

                properties.FontSize = length.Value;
                index++;
                return true;
            }

            case ValueForm.Color:
            {
                if (!TryReadColor(source, in token, out Rgba8 color, out string? message))
                {
                    error = new ValueError(StyleErrorKind.InvalidValue, index, message);
                    return false;
                }

                switch (entry.Property)
                {
                    case StyleProperty.BackgroundColor:
                        properties.BackgroundColor = color;
                        break;
                    case StyleProperty.BorderColor:
                        properties.BorderColor = color;
                        break;
                    default:
                        properties.Color = color;
                        break;
                }

                index++;
                return true;
            }

            case ValueForm.Font:
            {
                if (!TryReadFont(source, in token, index, fonts, ref properties, out error))
                {
                    return false;
                }

                index++;
                return true;
            }

            case ValueForm.FlexDirection:
            {
                if (!TryReadKeyword(source, in token, PropertyTable.FlexDirectionKeywords, entry.Name, out FlexDirection value, out string? message))
                {
                    error = new ValueError(StyleErrorKind.InvalidValue, index, message);
                    return false;
                }

                properties.FlexDirection = value;
                index++;
                return true;
            }

            case ValueForm.Justify:
            {
                if (!TryReadKeyword(source, in token, PropertyTable.JustifyKeywords, entry.Name, out Justify value, out string? message))
                {
                    error = new ValueError(StyleErrorKind.InvalidValue, index, message);
                    return false;
                }

                properties.JustifyContent = value;
                index++;
                return true;
            }

            case ValueForm.Align:
            {
                if (!TryReadKeyword(source, in token, PropertyTable.AlignKeywords, entry.Name, out Align value, out string? message))
                {
                    error = new ValueError(StyleErrorKind.InvalidValue, index, message);
                    return false;
                }

                switch (entry.Property)
                {
                    case StyleProperty.AlignItems:
                        properties.AlignItems = value;
                        break;
                    case StyleProperty.AlignSelf:
                        properties.AlignSelf = value;
                        break;
                    default:
                        properties.AlignContent = value;
                        break;
                }

                index++;
                return true;
            }

            case ValueForm.FlexWrap:
            {
                if (!TryReadKeyword(source, in token, PropertyTable.FlexWrapKeywords, entry.Name, out FlexWrap value, out string? message))
                {
                    error = new ValueError(StyleErrorKind.InvalidValue, index, message);
                    return false;
                }

                properties.FlexWrap = value;
                index++;
                return true;
            }

            case ValueForm.Display:
            {
                if (!TryReadKeyword(source, in token, PropertyTable.DisplayKeywords, entry.Name, out Display value, out string? message))
                {
                    error = new ValueError(StyleErrorKind.InvalidValue, index, message);
                    return false;
                }

                properties.Display = value;
                index++;
                return true;
            }

            case ValueForm.Position:
            {
                if (!TryReadKeyword(source, in token, PropertyTable.PositionKeywords, entry.Name, out PositionType value, out string? message))
                {
                    error = new ValueError(StyleErrorKind.InvalidValue, index, message);
                    return false;
                }

                properties.Position = value;
                index++;
                return true;
            }

            case ValueForm.Overflow:
            {
                if (!TryReadKeyword(source, in token, PropertyTable.OverflowKeywords, entry.Name, out Overflow value, out string? message))
                {
                    error = new ValueError(StyleErrorKind.InvalidValue, index, message);
                    return false;
                }

                properties.Overflow = value;
                index++;
                return true;
            }

            default:
                throw new ArgumentOutOfRangeException(nameof(entry), entry.Form, "The value form has no read.");
        }
    }

    /// <summary>
    /// Reads a length: <c>12px</c>, <c>50%</c>, <c>auto</c>, or a bare <c>0</c>.
    /// </summary>
    /// <remarks>
    /// A bare number other than 0 gives a message that names the fix. A unit other than <c>px</c> and <c>%</c> (for
    /// example <c>12em</c>) gives a message too. A form with no <see cref="Extra.Percent"/> rejects <c>%</c>, a form with
    /// no <see cref="Extra.Auto"/> rejects <c>auto</c>, and a form with no <see cref="Extra.Negative"/> rejects a
    /// negative value. A number that a float cannot hold gives a message.
    /// </remarks>
    /// <param name="source">The source span that the tokenizer read.</param>
    /// <param name="token">The value token.</param>
    /// <param name="extra">The units that the form accepts besides <c>px</c>.</param>
    /// <param name="propertyName">The CSS name, for the message.</param>
    /// <param name="length">The length, or <see cref="Length.Undefined"/> when the read fails.</param>
    /// <param name="message">The problem and the fix when the read fails.</param>
    /// <returns><see langword="true"/> when the token is a length of the form.</returns>
    private static bool TryReadLength(
        ReadOnlySpan<byte> source,
        in StyleToken token,
        Extra extra,
        string propertyName,
        out Length length,
        [NotNullWhen(false)] out string? message)
    {
        length = default;
        bool percent = (extra & Extra.Percent) != 0;
        string units = percent ? "px or %" : "px";

        switch (token.Kind)
        {
            case StyleTokenKind.Number:
            {
                if (!TryReadNumber(source, in token, out float number, out message))
                {
                    return false;
                }

                if (number == 0f)
                {
                    length = Length.Px(0f);
                    message = null;
                    return true;
                }

                message = Describe(source, in token) + " has no unit. Add " + units + ", for example '12px', or write 0.";
                return false;
            }

            case StyleTokenKind.Dimension:
            {
                if (!token.GetUnit(source).SequenceEqual("px"u8))
                {
                    message = Describe(source, in token) + " has a unit that the format does not know. Use " + units + " for "
                        + propertyName + ".";
                    return false;
                }

                if (!TryReadNumber(source, in token, out float number, out message)
                    || !CheckSign(source, in token, number, extra, propertyName, out message))
                {
                    return false;
                }

                length = Length.Px(number);
                message = null;
                return true;
            }

            case StyleTokenKind.Percentage:
            {
                if (!percent)
                {
                    message = Describe(source, in token) + " is not allowed for " + propertyName + ". Use px, for example '8px'.";
                    return false;
                }

                if (!TryReadNumber(source, in token, out float number, out message)
                    || !CheckSign(source, in token, number, extra, propertyName, out message))
                {
                    return false;
                }

                length = Length.Percent(number);
                message = null;
                return true;
            }

            case StyleTokenKind.Ident when token.GetText(source).SequenceEqual("auto"u8):
                if ((extra & Extra.Auto) == 0)
                {
                    message = Describe(source, in token) + " is not allowed for " + propertyName + ". Write a number with "
                        + units + ", for example '8px'.";
                    return false;
                }

                length = Length.Auto;
                message = null;
                return true;

            default:
                message = Describe(source, in token) + " is not a length. Write a number with " + units
                    + ((extra & Extra.Auto) != 0 ? ", 'auto'," : string.Empty) + " or write 0, for example '12px'.";
                return false;
        }
    }

    /// <summary>
    /// Reads the number part of a numeric token with the invariant culture.
    /// </summary>
    /// <param name="source">The source span that the tokenizer read.</param>
    /// <param name="token">A number, a dimension, or a percentage token.</param>
    /// <param name="number">The value, or 0 when the read fails.</param>
    /// <param name="message">The problem and the fix when the read fails.</param>
    /// <returns><see langword="true"/> when the number is finite.</returns>
    private static bool TryReadNumber(
        ReadOnlySpan<byte> source,
        in StyleToken token,
        out float number,
        [NotNullWhen(false)] out string? message)
    {
        if (!float.TryParse(token.GetNumber(source), NumberStyles.Float, CultureInfo.InvariantCulture, out number) || !float.IsFinite(number))
        {
            number = 0f;
            message = Describe(source, in token) + " is not a number that a float holds. Write a smaller number.";
            return false;
        }

        message = null;
        return true;
    }

    /// <summary>Checks the sign of a length: a form with no <see cref="Extra.Negative"/> takes 0 or more only.</summary>
    /// <param name="source">The source span that the tokenizer read.</param>
    /// <param name="token">The value token.</param>
    /// <param name="number">The number of the token.</param>
    /// <param name="extra">The values that the form accepts.</param>
    /// <param name="propertyName">The CSS name, for the message.</param>
    /// <param name="message">The problem and the fix when the check fails.</param>
    /// <returns><see langword="true"/> when the form accepts the sign of the number.</returns>
    private static bool CheckSign(
        ReadOnlySpan<byte> source,
        in StyleToken token,
        float number,
        Extra extra,
        string propertyName,
        [NotNullWhen(false)] out string? message)
    {
        // -0 is not less than 0, so '-0px' is correct.
        if (number < 0f && (extra & Extra.Negative) == 0)
        {
            message = Describe(source, in token) + " is a negative value. Write a value of 0 or more for " + propertyName + ", for example '4px'.";
            return false;
        }

        message = null;
        return true;
    }

    /// <summary>
    /// Finds a CSS form that the format does not support at a token of a value: a function other than <c>var</c>
    /// (<c>calc(</c>, <c>rgb(</c>), or <c>!important</c>.
    /// </summary>
    /// <param name="source">The source span that the tokenizer read.</param>
    /// <param name="tokens">The token list. It ends with the <see cref="StyleTokenKind.End"/> token.</param>
    /// <param name="index">The index of the token.</param>
    /// <param name="message">The problem and the fix when the token starts such a form.</param>
    /// <returns>
    /// <see langword="true"/> when the token is the name of such a function or the <c>!</c> of <c>!important</c>.
    /// The error position is the token at the index.
    /// </returns>
    public static bool IsUnsupportedSyntax(
        ReadOnlySpan<byte> source,
        List<StyleToken> tokens,
        int index,
        [NotNullWhen(true)] out string? message)
    {
        message = null;
        StyleToken token = tokens[index];

        // The token after an identifier or a '!' is never past the End token, because neither is the End token.
        if (token.Kind == StyleTokenKind.Ident
            && tokens[index + 1].Kind == StyleTokenKind.LeftParen
            && !token.GetText(source).SequenceEqual("var"u8))
        {
            message = Describe(source, in token) + " is a function. The format has no function other than 'var'. "
                + "Write the value itself, for example '#3060c0' or '12px'.";
            return true;
        }

        if (token.Kind == StyleTokenKind.Delim && source[token.Offset] == (byte)'!')
        {
            int next = index + 1;
            while (tokens[next].Kind == StyleTokenKind.Whitespace)
            {
                next++;
            }

            StyleToken word = tokens[next];
            if (word.Kind == StyleTokenKind.Ident && word.GetText(source).SequenceEqual("important"u8))
            {
                message = Describe(source, in token) + " starts '!important'. The format has no '!important'. "
                    + "Remove it, and use a selector with a higher specificity.";
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Reads one to four lengths that are separated by whitespace. The read stops at a <c>;</c>, a <c>}</c>, or the
    /// end token.
    /// </summary>
    /// <param name="source">The source span that the tokenizer read.</param>
    /// <param name="tokens">The token list.</param>
    /// <param name="index">The index of the first value. The method moves it to the token after the last value and its whitespace.</param>
    /// <param name="extra">The units that the form accepts besides <c>px</c>.</param>
    /// <param name="propertyName">The CSS name, for the message.</param>
    /// <param name="values">A span of four lengths that gets the values.</param>
    /// <param name="count">The number of values.</param>
    /// <param name="error">The error at the first token at fault.</param>
    /// <returns><see langword="true"/> when the list is correct.</returns>
    private static bool TryReadList(
        ReadOnlySpan<byte> source,
        List<StyleToken> tokens,
        ref int index,
        Extra extra,
        string propertyName,
        Span<Length> values,
        out int count,
        out ValueError error)
    {
        count = 0;
        error = default;

        while (true)
        {
            StyleToken token = tokens[index];
            if (count == values.Length)
            {
                error = new ValueError(
                    StyleErrorKind.InvalidValue,
                    index,
                    Describe(source, in token) + " is one value too many. " + propertyName + " takes 1 to 4 values.");
                return false;
            }

            if (!TryReadLength(source, in token, extra, propertyName, out Length length, out string? message))
            {
                error = new ValueError(StyleErrorKind.InvalidValue, index, message);
                return false;
            }

            values[count++] = length;
            index++;

            int next = index;
            while (tokens[next].Kind == StyleTokenKind.Whitespace)
            {
                next++;
            }

            StyleTokenKind kind = tokens[next].Kind;
            if (kind is StyleTokenKind.Semicolon or StyleTokenKind.RightBrace or StyleTokenKind.End)
            {
                index = next;
                return true;
            }

            if (next == index)
            {
                StyleToken stuck = tokens[next];
                error = new ValueError(
                    StyleErrorKind.InvalidValue,
                    next,
                    Describe(source, in stuck) + " touches the value before it. Separate the values of " + propertyName + " with whitespace.");
                return false;
            }

            index = next;
        }
    }

    /// <summary>
    /// Reads the value of a <c>transition</c> declaration: one or more items
    /// <c>&lt;property&gt; &lt;duration&gt; &lt;easing&gt;</c>, separated by commas. The read stops at a <c>;</c>, a
    /// <c>}</c>, or the end token.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Whitespace separates the three parts of an item. Whitespace around a comma is optional. The property is one of
    /// <see cref="PropertyTable.TransitionMemberKeywords"/>. The duration is <c>&lt;number&gt;s</c> or
    /// <c>&lt;number&gt;ms</c> of 0 or more, or a bare <c>0</c>. The easing is one of
    /// <see cref="PropertyTable.EasingKeywords"/>. All three parts are required.
    /// </para>
    /// <para>
    /// A later item for the same member wins. The read writes <paramref name="transitions"/> only when the full value
    /// is correct, so a wrong value changes no slot. The slots of the other members stay as they are.
    /// </para>
    /// </remarks>
    /// <param name="source">The source span that the tokenizer read.</param>
    /// <param name="tokens">The token list.</param>
    /// <param name="index">The index of the first value token. The method moves it to the <c>;</c>, the <c>}</c>, or the end token.</param>
    /// <param name="transitions">The transition set of the rule.</param>
    /// <param name="error">The error at the first token at fault.</param>
    /// <returns><see langword="true"/> when the value is correct.</returns>
    private static bool TryReadTransitions(
        ReadOnlySpan<byte> source,
        List<StyleToken> tokens,
        ref int index,
        ref StyleTransitions transitions,
        out ValueError error)
    {
        error = default;
        StyleTransitions read = transitions;
        int i = index;

        while (true)
        {
            StyleToken name = tokens[i];
            if (name.Kind != StyleTokenKind.Ident
                || !PropertyTable.TryFind(PropertyTable.TransitionMemberKeywords, name.GetText(source), out StyleProperty member))
            {
                error = new ValueError(
                    StyleErrorKind.InvalidValue,
                    i,
                    Describe(source, in name) + " is not a property that can transition. Use one of "
                        + PropertyTable.List(PropertyTable.TransitionMemberKeywords) + ".");
                return false;
            }

            i++;
            if (!TryNextTransitionPart(source, tokens, ref i, "duration", out error))
            {
                return false;
            }

            StyleToken durationToken = tokens[i];
            if (!TryReadDuration(source, in durationToken, out float seconds, out string? message))
            {
                error = new ValueError(StyleErrorKind.InvalidValue, i, message);
                return false;
            }

            i++;
            if (!TryNextTransitionPart(source, tokens, ref i, "easing", out error))
            {
                return false;
            }

            StyleToken easingToken = tokens[i];
            if (easingToken.Kind != StyleTokenKind.Ident
                || !PropertyTable.TryFind(PropertyTable.EasingKeywords, easingToken.GetText(source), out TransitionEasing easing))
            {
                error = new ValueError(
                    StyleErrorKind.InvalidValue,
                    i,
                    Describe(source, in easingToken) + " is not an easing of the format. Use one of "
                        + PropertyTable.List(PropertyTable.EasingKeywords) + ".");
                return false;
            }

            i++;

            // A later item for the same member replaces the slot.
            read.Set(member, new StyleTransition(seconds, easing));

            int next = SkipWhitespace(tokens, i);
            StyleToken after = tokens[next];
            if (after.Kind is StyleTokenKind.Semicolon or StyleTokenKind.RightBrace or StyleTokenKind.End)
            {
                index = next;
                transitions = read;
                return true;
            }

            if (after.Kind == StyleTokenKind.Comma)
            {
                i = SkipWhitespace(tokens, next + 1);
                StyleToken start = tokens[i];
                if (start.Kind is StyleTokenKind.Semicolon or StyleTokenKind.RightBrace or StyleTokenKind.End or StyleTokenKind.Comma)
                {
                    error = new ValueError(
                        StyleErrorKind.InvalidValue,
                        i,
                        "The transition after ',' is missing before " + Describe(source, in start)
                            + ". Write a property, a duration, and an easing after ',', for example 'opacity 0.2s ease-out'.");
                    return false;
                }

                continue;
            }

            error = new ValueError(
                StyleErrorKind.InvalidValue,
                next,
                next == i
                    ? Describe(source, in after) + " touches the easing before it. Write ',' between two transitions."
                    : Describe(source, in after) + " is one part too many. A transition takes a property, a duration, and an easing only. "
                        + "Write ',' between two transitions.");
            return false;
        }
    }

    /// <summary>
    /// Moves from the token after one part of a transition to the next part. Whitespace must come between the parts.
    /// </summary>
    /// <param name="source">The source span that the tokenizer read.</param>
    /// <param name="tokens">The token list.</param>
    /// <param name="index">The index of the token after the part. The method moves it to the next part.</param>
    /// <param name="part">The name of the next part for the message: <c>duration</c> or <c>easing</c>.</param>
    /// <param name="error">The error at the token at fault.</param>
    /// <returns><see langword="true"/> when the next part is there.</returns>
    private static bool TryNextTransitionPart(
        ReadOnlySpan<byte> source,
        List<StyleToken> tokens,
        ref int index,
        string part,
        out ValueError error)
    {
        int next = SkipWhitespace(tokens, index);
        StyleToken token = tokens[next];
        if (token.Kind is StyleTokenKind.Semicolon or StyleTokenKind.RightBrace or StyleTokenKind.End or StyleTokenKind.Comma)
        {
            error = new ValueError(
                StyleErrorKind.InvalidValue,
                next,
                "The transition has no " + part + " before " + Describe(source, in token)
                    + ". Write a property, a duration, and an easing, for example 'opacity 0.2s ease-out'.");
            return false;
        }

        if (next == index)
        {
            error = new ValueError(
                StyleErrorKind.InvalidValue,
                next,
                Describe(source, in token) + " touches the value before it. Write whitespace before the " + part + ".");
            return false;
        }

        error = default;
        index = next;
        return true;
    }

    /// <summary>
    /// Reads a duration: <c>&lt;number&gt;s</c>, <c>&lt;number&gt;ms</c>, or a bare <c>0</c>. A value in
    /// <c>ms</c> is divided by 1000.
    /// </summary>
    /// <remarks>
    /// The unit is case-sensitive, as <c>px</c> is. A negative value, another unit, a bare number other than 0, and a
    /// number that a float cannot hold each give a message. <c>-0s</c> and <c>-0</c> give 0.
    /// </remarks>
    /// <param name="source">The source span that the tokenizer read.</param>
    /// <param name="token">The value token.</param>
    /// <param name="seconds">The duration in seconds, or 0 when the read fails.</param>
    /// <param name="message">The problem and the fix when the read fails.</param>
    /// <returns><see langword="true"/> when the token is a duration of 0 or more.</returns>
    private static bool TryReadDuration(
        ReadOnlySpan<byte> source,
        in StyleToken token,
        out float seconds,
        [NotNullWhen(false)] out string? message)
    {
        seconds = 0f;
        switch (token.Kind)
        {
            case StyleTokenKind.Number:
            {
                if (!TryReadNumber(source, in token, out float number, out message))
                {
                    return false;
                }

                if (number == 0f)
                {
                    message = null;
                    return true;
                }

                message = Describe(source, in token) + " has no unit. Add s or ms, for example '0.2s', or write 0.";
                return false;
            }

            case StyleTokenKind.Dimension:
            {
                ReadOnlySpan<byte> unit = token.GetUnit(source);
                bool milliseconds = unit.SequenceEqual("ms"u8);
                if (!milliseconds && !unit.SequenceEqual("s"u8))
                {
                    message = Describe(source, in token) + " has a unit that a duration does not take. Use s or ms, for example '0.2s'.";
                    return false;
                }

                if (!TryReadNumber(source, in token, out float number, out message))
                {
                    return false;
                }

                if (number < 0f)
                {
                    message = Describe(source, in token) + " is a negative duration. Write a duration of 0 or more, for example '0.2s'.";
                    return false;
                }

                // -0 is not less than 0, so '-0s' is correct. The slot gets +0, so the value prints as 0.
                seconds = milliseconds ? number / 1000f : number;
                if (seconds == 0f)
                {
                    seconds = 0f;
                }

                message = null;
                return true;
            }

            default:
                message = Describe(source, in token) + " is not a duration. Write a number with s or ms, for example '0.2s'.";
                return false;
        }
    }

    /// <summary>Gives the index of the first token at or after an index that is not whitespace.</summary>
    private static int SkipWhitespace(List<StyleToken> tokens, int index)
    {
        while (tokens[index].Kind == StyleTokenKind.Whitespace)
        {
            index++;
        }

        return index;
    }

    /// <summary>
    /// Maps one to four values in the CSS order (top, right, bottom, left) to the fields of <see cref="LengthEdges"/>.
    /// </summary>
    /// <param name="values">The values in the CSS order.</param>
    /// <param name="count">The number of values, from 1 to 4.</param>
    /// <returns>The edges.</returns>
    private static LengthEdges MapEdges(ReadOnlySpan<Length> values, int count)
    {
        Length top = values[0];
        Length right = count > 1 ? values[1] : top;
        Length bottom = count > 2 ? values[2] : top;
        Length left = count > 3 ? values[3] : right;

        // The constructor of LengthEdges takes left, top, right, and bottom.
        return new LengthEdges(left, top, right, bottom);
    }

    /// <summary>
    /// Maps one to four values in the CSS order (top-left, top-right, bottom-right, bottom-left) to <see cref="Corners"/>.
    /// </summary>
    /// <param name="values">The values in the CSS order. Each value is in pixels.</param>
    /// <param name="count">The number of values, from 1 to 4.</param>
    /// <returns>The corners.</returns>
    private static Corners MapCorners(ReadOnlySpan<Length> values, int count)
    {
        float topLeft = values[0].Value;
        float topRight = count > 1 ? values[1].Value : topLeft;
        float bottomRight = count > 2 ? values[2].Value : topLeft;
        float bottomLeft = count > 3 ? values[3].Value : topRight;

        // The constructor of Corners takes top-left, top-right, bottom-right, and bottom-left.
        return new Corners(topLeft, topRight, bottomRight, bottomLeft);
    }

    /// <summary>Reads a keyword of an explicit table.</summary>
    /// <typeparam name="T">The enum type.</typeparam>
    /// <param name="source">The source span that the tokenizer read.</param>
    /// <param name="token">The value token.</param>
    /// <param name="table">The table.</param>
    /// <param name="propertyName">The CSS name, for the message.</param>
    /// <param name="value">The member, or <c>default</c> when the read fails.</param>
    /// <param name="message">The problem and the fix when the read fails.</param>
    /// <returns><see langword="true"/> when the token is a word of the table.</returns>
    private static bool TryReadKeyword<T>(
        ReadOnlySpan<byte> source,
        in StyleToken token,
        Keyword<T>[] table,
        string propertyName,
        out T value,
        [NotNullWhen(false)] out string? message)
    {
        if (token.Kind == StyleTokenKind.Ident && PropertyTable.TryFind(table, token.GetText(source), out value))
        {
            message = null;
            return true;
        }

        value = default!;
        message = Describe(source, in token) + " is not a value of " + propertyName + ". Use one of " + PropertyTable.List(table) + ".";
        return false;
    }

    /// <summary>Reads the name of a font.</summary>
    /// <param name="source">The source span that the tokenizer read.</param>
    /// <param name="token">The value token.</param>
    /// <param name="index">The index of the token, for the error.</param>
    /// <param name="fonts">The font map of the parser.</param>
    /// <param name="properties">The set that gets the handle.</param>
    /// <param name="error">The error when the read fails.</param>
    /// <returns><see langword="true"/> when the name is <c>default</c> or a key of the map.</returns>
    private static bool TryReadFont(
        ReadOnlySpan<byte> source,
        in StyleToken token,
        int index,
        Dictionary<string, FontHandle> fonts,
        ref StyleProperties properties,
        out ValueError error)
    {
        error = default;
        if (token.Kind != StyleTokenKind.Ident)
        {
            error = new ValueError(
                StyleErrorKind.InvalidValue,
                index,
                Describe(source, in token) + " is not a font name. Write 'default', or a name of the font map of the parser.");
            return false;
        }

        string name = Encoding.UTF8.GetString(token.GetText(source));
        if (fonts.TryGetValue(name, out FontHandle handle))
        {
            properties.Font = handle;
            return true;
        }

        if (string.Equals(name, "default", StringComparison.Ordinal))
        {
            properties.Font = new FontHandle(0);
            return true;
        }

        error = new ValueError(
            StyleErrorKind.UnknownFont,
            index,
            "'" + name + "' is not in the font map of the parser. Write 'default', or add the name to StyleSheetParser.CreateInfo.Fonts.");
        return false;
    }

    /// <summary>Writes a length to the member of one of the eleven length properties.</summary>
    private static void SetLength(ref StyleProperties properties, StyleProperty property, Length length)
    {
        switch (property)
        {
            case StyleProperty.Width:
                properties.Width = length;
                break;
            case StyleProperty.Height:
                properties.Height = length;
                break;
            case StyleProperty.FlexBasis:
                properties.FlexBasis = length;
                break;
            case StyleProperty.MinWidth:
                properties.MinWidth = length;
                break;
            case StyleProperty.MinHeight:
                properties.MinHeight = length;
                break;
            case StyleProperty.MaxWidth:
                properties.MaxWidth = length;
                break;
            case StyleProperty.MaxHeight:
                properties.MaxHeight = length;
                break;
            case StyleProperty.Left:
                properties.Left = length;
                break;
            case StyleProperty.Top:
                properties.Top = length;
                break;
            case StyleProperty.Right:
                properties.Right = length;
                break;
            case StyleProperty.Bottom:
                properties.Bottom = length;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(property), property, "The member does not take a length.");
        }
    }

    /// <summary>
    /// Reads a color: a <see cref="StyleTokenKind.Hash"/> token of 3, 6, or 8 hex digits in any case.
    /// </summary>
    /// <remarks>
    /// <para><c>#rgb</c> doubles each digit and sets the alpha to 255. <c>#rrggbb</c> sets the alpha to 255.
    /// <c>#rrggbbaa</c> holds the alpha.</para>
    /// <para>Another digit count, a digit that is not hex, and another token kind each give a message.</para>
    /// </remarks>
    /// <param name="source">The source span that the tokenizer read.</param>
    /// <param name="token">The value token.</param>
    /// <param name="color">The color, or <see cref="Rgba8.Transparent"/> when the read fails.</param>
    /// <param name="message">The problem and the fix when the read fails. <see langword="null"/> when the read succeeds.</param>
    /// <returns><see langword="true"/> when the token is a color.</returns>
    public static bool TryReadColor(
        ReadOnlySpan<byte> source,
        in StyleToken token,
        out Rgba8 color,
        [NotNullWhen(false)] out string? message)
    {
        color = Rgba8.Transparent;

        if (token.Kind != StyleTokenKind.Hash)
        {
            message = Describe(source, in token) + " is not a color. Write '#' and 3, 6, or 8 hex digits, for example '#1e222c'.";
            return false;
        }

        // The text of a Hash token is '#' and one or more bytes of [A-Za-z0-9_-].
        ReadOnlySpan<byte> digits = token.GetText(source)[1..];
        if (digits.Length != 3 && digits.Length != 6 && digits.Length != 8)
        {
            message = Describe(source, in token) + " is not a color. Write 3, 6, or 8 hex digits after '#', for example '#1e222c'.";
            return false;
        }

        for (int i = 0; i < digits.Length; i++)
        {
            if (HexValue(digits[i]) < 0)
            {
                message = Describe(source, in token) + " is not a color. Use only the hex digits 0 to 9, a to f, and A to F after '#'.";
                return false;
            }
        }

        if (digits.Length == 3)
        {
            color = new Rgba8(Short(digits[0]), Short(digits[1]), Short(digits[2]), 255);
        }
        else
        {
            byte a = digits.Length == 8 ? Pair(digits[6], digits[7]) : (byte)255;
            color = new Rgba8(Pair(digits[0], digits[1]), Pair(digits[2], digits[3]), Pair(digits[4], digits[5]), a);
        }

        message = null;
        return true;
    }

    /// <summary>Gives the text of a token for a message.</summary>
    /// <param name="source">The source span that the tokenizer read.</param>
    /// <param name="token">The token.</param>
    /// <returns>
    /// The token text in single quotes, <c>whitespace</c> for a <see cref="StyleTokenKind.Whitespace"/> token, or
    /// <c>the end of the text</c> for the <see cref="StyleTokenKind.End"/> token. The method allocates.
    /// </returns>
    public static string Describe(ReadOnlySpan<byte> source, in StyleToken token)
    {
        return token.Kind switch
        {
            StyleTokenKind.End => "the end of the text",
            StyleTokenKind.Whitespace => "whitespace",
            _ => "'" + Encoding.UTF8.GetString(token.GetText(source)) + "'",
        };
    }

    /// <summary>Gives the value of one hex digit.</summary>
    /// <param name="b">The byte.</param>
    /// <returns>The value from 0 to 15, or -1 for a byte that is not a hex digit.</returns>
    private static int HexValue(byte b)
    {
        return b switch
        {
            >= (byte)'0' and <= (byte)'9' => b - '0',
            >= (byte)'a' and <= (byte)'f' => b - 'a' + 10,
            >= (byte)'A' and <= (byte)'F' => b - 'A' + 10,
            _ => -1,
        };
    }

    /// <summary>Gives the channel of one short digit: the digit two times, for example <c>f</c> gives 0xFF.</summary>
    private static byte Short(byte digit)
    {
        return (byte)(HexValue(digit) * 17);
    }

    /// <summary>Gives the channel of two digits, for example <c>1e</c> gives 0x1E.</summary>
    private static byte Pair(byte high, byte low)
    {
        return (byte)((HexValue(high) << 4) | HexValue(low));
    }
}
