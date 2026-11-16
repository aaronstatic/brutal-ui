using System;
using System.Globalization;
using BrutalUi.Core;

namespace BrutalUi.Markup;

/// <summary>
/// The value forms of a template attribute: bool, whole number, number, number list, color, and class list name.
/// </summary>
/// <remarks>
/// <para>
/// Each parse method reads a <see cref="ReadOnlySpan{T}"/> of characters and allocates nothing. The methods follow
/// The Value Forms of the plan: a typed value ignores XML whitespace at both ends, and a fault gives
/// <see langword="false"/>. <see cref="AttributeContext"/> turns a fault into an error with a message.
/// </para>
/// <para>
/// <see cref="TryParseColor"/> copies the digit rules of <c>StyleValues.TryReadColor</c> (Decision 7), because the
/// stylesheet reader works on UTF-8 tokens. A later refactor can share one span reader.
/// </para>
/// </remarks>
internal static class TemplateValues
{
    /// <summary>Tells whether a character is XML whitespace: space, tab, CR, or LF.</summary>
    /// <param name="character">The character.</param>
    /// <returns><see langword="true"/> for XML whitespace.</returns>
    internal static bool IsXmlWhitespace(char character)
    {
        return character is ' ' or '\t' or '\r' or '\n';
    }

    /// <summary>Cuts XML whitespace from both ends of the text.</summary>
    /// <param name="text">The text.</param>
    /// <returns>The text without XML whitespace at either end.</returns>
    internal static ReadOnlySpan<char> Trim(ReadOnlySpan<char> text)
    {
        int start = 0;
        int end = text.Length;

        while (start < end && IsXmlWhitespace(text[start]))
        {
            start++;
        }

        while (end > start && IsXmlWhitespace(text[end - 1]))
        {
            end--;
        }

        return text[start..end];
    }

    /// <summary>Reads a bool: <c>true</c> or <c>false</c>, case-sensitive.</summary>
    /// <param name="text">The value without XML whitespace at both ends.</param>
    /// <param name="value">The value, or <see langword="false"/> on a fault.</param>
    /// <returns><see langword="true"/> when the text is <c>true</c> or <c>false</c>.</returns>
    internal static bool TryParseBool(ReadOnlySpan<char> text, out bool value)
    {
        if (text.Equals("true".AsSpan(), StringComparison.Ordinal))
        {
            value = true;
            return true;
        }

        if (text.Equals("false".AsSpan(), StringComparison.Ordinal))
        {
            value = false;
            return true;
        }

        value = false;
        return false;
    }

    /// <summary>Reads a whole number: ASCII digits only, from 1 to <see cref="int.MaxValue"/>.</summary>
    /// <param name="text">The value without XML whitespace at both ends.</param>
    /// <param name="value">The value, or 0 on a fault.</param>
    /// <returns><see langword="true"/> when the text is a whole number above 0.</returns>
    internal static bool TryParseWholeNumber(ReadOnlySpan<char> text, out int value)
    {
        value = 0;
        long total = 0;

        if (text.IsEmpty)
        {
            return false;
        }

        for (int i = 0; i < text.Length; i++)
        {
            char character = text[i];
            if (character is < '0' or > '9')
            {
                return false;
            }

            total = total * 10 + (character - '0');
            if (total > int.MaxValue)
            {
                return false;
            }
        }

        if (total < 1)
        {
            return false;
        }

        value = (int)total;
        return true;
    }

    /// <summary>The largest <c>capacity</c> that the <c>TextField</c> factory accepts, in bytes.</summary>
    /// <remarks>
    /// A real single-line field takes far fewer bytes. The number stays under the CLR array limit, so the field
    /// constructor allocates its buffer with no <see cref="OutOfMemoryException"/>.
    /// </remarks>
    internal const int MaxTextCapacity = 65536;

    /// <summary>Checks a text capacity against the limit, and reports the fault on the attribute.</summary>
    /// <remarks>
    /// The <c>TextField</c> factory calls this before the constructor, so no exception leaves the load for a big
    /// <c>capacity</c>. A capacity of <see cref="MaxTextCapacity"/> or less passes. The check is a rule of the
    /// <c>TextField</c> row, not of the whole-number form.
    /// </remarks>
    /// <param name="context">The <c>capacity</c> attribute.</param>
    /// <param name="capacity">The capacity from the whole-number read.</param>
    /// <returns><see langword="true"/> when the capacity is not above the limit.</returns>
    internal static bool CheckCapacity(AttributeContext context, int capacity)
    {
        if (capacity <= MaxTextCapacity)
        {
            return true;
        }

        context.AddError(
            TemplateErrorKind.InvalidValue,
            $"'{context.Value}' is above the capacity limit {MaxTextCapacity}. Write a smaller number.");
        return false;
    }

    /// <summary>Reads a number: a sign, digits, and an optional point with digits, in the invariant culture.</summary>
    /// <remarks>
    /// The form has no exponent and no group separator. A read that gives NaN or an infinite value is a fault.
    /// </remarks>
    /// <param name="text">The value without XML whitespace at both ends.</param>
    /// <param name="value">The value, or 0 on a fault.</param>
    /// <returns><see langword="true"/> when the text is a finite number.</returns>
    internal static bool TryParseNumber(ReadOnlySpan<char> text, out float value)
    {
        const NumberStyles Style = NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint;
        if (!float.TryParse(text, Style, CultureInfo.InvariantCulture, out value))
        {
            value = 0f;
            return false;
        }

        if (!float.IsFinite(value))
        {
            value = 0f;
            return false;
        }

        return true;
    }

    /// <summary>Reads a list of numbers with XML whitespace between them.</summary>
    /// <param name="text">The value without XML whitespace at both ends.</param>
    /// <param name="numbers">The span to fill. The text must hold exactly this many numbers.</param>
    /// <returns><see langword="true"/> when the text holds exactly <paramref name="numbers"/>.Length finite numbers.</returns>
    internal static bool TryParseNumbers(ReadOnlySpan<char> text, Span<float> numbers)
    {
        int count = 0;
        ReadOnlySpan<char> rest = text;

        while (true)
        {
            while (!rest.IsEmpty && IsXmlWhitespace(rest[0]))
            {
                rest = rest[1..];
            }

            if (rest.IsEmpty)
            {
                break;
            }

            int end = 0;
            while (end < rest.Length && !IsXmlWhitespace(rest[end]))
            {
                end++;
            }

            if (count == numbers.Length || !TryParseNumber(rest[..end], out numbers[count]))
            {
                return false;
            }

            count++;
            rest = rest[end..];
        }

        return count == numbers.Length;
    }

    /// <summary>Reads a color: <c>#</c> and 3, 6, or 8 hex digits in each case.</summary>
    /// <remarks>
    /// <c>#rgb</c> doubles each digit and sets the alpha to 255. <c>#rrggbb</c> sets the alpha to 255.
    /// <c>#rrggbbaa</c> holds the alpha.
    /// </remarks>
    /// <param name="text">The value without XML whitespace at both ends.</param>
    /// <param name="color">The color, or <see cref="Rgba8.Transparent"/> on a fault.</param>
    /// <returns><see langword="true"/> when the text is a color.</returns>
    internal static bool TryParseColor(ReadOnlySpan<char> text, out Rgba8 color)
    {
        color = Rgba8.Transparent;

        if (text.IsEmpty || text[0] != '#')
        {
            return false;
        }

        ReadOnlySpan<char> digits = text[1..];
        if (digits.Length is not (3 or 6 or 8))
        {
            return false;
        }

        for (int i = 0; i < digits.Length; i++)
        {
            if (HexValue(digits[i]) < 0)
            {
                return false;
            }
        }

        if (digits.Length == 3)
        {
            color = new Rgba8(Short(digits[0]), Short(digits[1]), Short(digits[2]), 255);
            return true;
        }

        byte alpha = digits.Length == 8 ? Pair(digits[6], digits[7]) : (byte)255;
        color = new Rgba8(Pair(digits[0], digits[1]), Pair(digits[2], digits[3]), Pair(digits[4], digits[5]), alpha);
        return true;
    }

    /// <summary>Gives the next name of a class list, and moves the rest past it.</summary>
    /// <remarks>
    /// The rest holds names with XML whitespace between them. An empty rest gives <see langword="false"/>.
    /// </remarks>
    /// <param name="rest">The rest of the value. The method writes the part after the name.</param>
    /// <param name="name">The name, or an empty span when the rest holds none.</param>
    /// <returns><see langword="true"/> when the rest held a name.</returns>
    internal static bool NextName(ref ReadOnlySpan<char> rest, out ReadOnlySpan<char> name)
    {
        while (!rest.IsEmpty && IsXmlWhitespace(rest[0]))
        {
            rest = rest[1..];
        }

        int end = 0;
        while (end < rest.Length && !IsXmlWhitespace(rest[end]))
        {
            end++;
        }

        name = rest[..end];
        rest = rest[end..];
        return end > 0;
    }

    /// <summary>Gives the value of one hex digit.</summary>
    /// <param name="digit">The character.</param>
    /// <returns>The value from 0 to 15, or -1 for a character that is not a hex digit.</returns>
    private static int HexValue(char digit)
    {
        return digit switch
        {
            >= '0' and <= '9' => digit - '0',
            >= 'a' and <= 'f' => digit - 'a' + 10,
            >= 'A' and <= 'F' => digit - 'A' + 10,
            _ => -1,
        };
    }

    /// <summary>Gives the channel of one short digit: the digit two times, for example <c>f</c> gives 0xFF.</summary>
    /// <param name="digit">The hex digit.</param>
    /// <returns>The channel byte.</returns>
    private static byte Short(char digit)
    {
        return (byte)(HexValue(digit) * 17);
    }

    /// <summary>Gives the channel of two hex digits, high first.</summary>
    /// <param name="high">The high digit.</param>
    /// <param name="low">The low digit.</param>
    /// <returns>The channel byte.</returns>
    private static byte Pair(char high, char low)
    {
        return (byte)((HexValue(high) << 4) | HexValue(low));
    }
}
