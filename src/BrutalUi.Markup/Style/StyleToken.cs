using System;

namespace BrutalUi.Markup;

/// <summary>
/// The kind of a <see cref="StyleToken"/>.
/// </summary>
internal enum StyleTokenKind : byte
{
    /// <summary>An identifier: <c>[A-Za-z_]</c>, or <c>-</c> and then <c>[A-Za-z_-]</c>, and then <c>[A-Za-z0-9_-]*</c>.</summary>
    Ident,

    /// <summary>A <c>#</c> and then one or more of <c>[A-Za-z0-9_-]</c>. A name selector or a color, by context.</summary>
    Hash,

    /// <summary>A number with no unit, for example <c>0</c>, <c>-4</c>, or <c>.5</c>.</summary>
    Number,

    /// <summary>A number and then an identifier, for example <c>12px</c> or <c>0.2s</c>.</summary>
    Dimension,

    /// <summary>A number and then <c>%</c>, for example <c>50%</c>.</summary>
    Percentage,

    /// <summary>The byte <c>:</c>.</summary>
    Colon,

    /// <summary>The byte <c>;</c>.</summary>
    Semicolon,

    /// <summary>The byte <c>,</c>.</summary>
    Comma,

    /// <summary>The byte <c>.</c> when no digit follows it.</summary>
    Dot,

    /// <summary>The byte <c>&gt;</c>.</summary>
    Greater,

    /// <summary>The byte <c>{</c>.</summary>
    LeftBrace,

    /// <summary>The byte <c>}</c>.</summary>
    RightBrace,

    /// <summary>The byte <c>(</c>.</summary>
    LeftParen,

    /// <summary>The byte <c>)</c>.</summary>
    RightParen,

    /// <summary>One run of space, tab, CR, LF, FF, and comments. The run holds one whitespace byte or more.</summary>
    Whitespace,

    /// <summary>Each other printable ASCII byte, for example <c>*</c>, <c>[</c>, <c>+</c>, <c>~</c>, <c>!</c>, or <c>@</c>.</summary>
    Delim,

    /// <summary>The end of the text. The token holds the position after the last byte, and its length is 0.</summary>
    End,
}

/// <summary>
/// One token of a stylesheet text: the kind, the byte range in the source, and the position of the first byte.
/// </summary>
/// <remarks>
/// The token holds no text. <see cref="GetText(ReadOnlySpan{byte})"/> slices the source span, so a read allocates nothing.
/// The source span must be the span that the tokenizer read.
/// </remarks>
internal readonly struct StyleToken
{
    /// <summary>Creates a token.</summary>
    /// <param name="kind">The kind.</param>
    /// <param name="offset">The byte offset of the first byte in the source.</param>
    /// <param name="length">The byte length.</param>
    /// <param name="line">The line of the first byte, from 1.</param>
    /// <param name="column">The column of the first byte, from 1, in code points.</param>
    /// <param name="numberLength">The byte length of the number part of a numeric token. 0 for each other kind.</param>
    public StyleToken(StyleTokenKind kind, int offset, int length, int line, int column, int numberLength = 0)
    {
        Kind = kind;
        Offset = offset;
        Length = length;
        Line = line;
        Column = column;
        NumberLength = numberLength;
    }

    /// <summary>Gets the kind.</summary>
    public StyleTokenKind Kind { get; }

    /// <summary>Gets the byte offset of the first byte in the source.</summary>
    public int Offset { get; }

    /// <summary>Gets the byte length. The length of <see cref="StyleTokenKind.End"/> is 0.</summary>
    public int Length { get; }

    /// <summary>Gets the line of the first byte, from 1.</summary>
    public int Line { get; }

    /// <summary>Gets the column of the first byte, from 1. The column counts code points. A tab counts 1.</summary>
    public int Column { get; }

    /// <summary>
    /// Gets the byte length of the number part of a <see cref="StyleTokenKind.Number"/>, a <see cref="StyleTokenKind.Dimension"/>,
    /// or a <see cref="StyleTokenKind.Percentage"/>. The value is 0 for each other kind.
    /// </summary>
    public int NumberLength { get; }

    /// <summary>Gets the bytes of the token.</summary>
    /// <param name="source">The source span that the tokenizer read.</param>
    /// <returns>A slice of <paramref name="source"/>.</returns>
    public ReadOnlySpan<byte> GetText(ReadOnlySpan<byte> source)
    {
        return source.Slice(Offset, Length);
    }

    /// <summary>Gets the number part of a numeric token, for example <c>12</c> of <c>12px</c> or <c>50</c> of <c>50%</c>.</summary>
    /// <param name="source">The source span that the tokenizer read.</param>
    /// <returns>A slice of <paramref name="source"/>. The slice is empty for a token that is not numeric.</returns>
    public ReadOnlySpan<byte> GetNumber(ReadOnlySpan<byte> source)
    {
        return source.Slice(Offset, NumberLength);
    }

    /// <summary>Gets the unit of a <see cref="StyleTokenKind.Dimension"/>, for example <c>px</c> of <c>12px</c>.</summary>
    /// <param name="source">The source span that the tokenizer read.</param>
    /// <returns>A slice of <paramref name="source"/>. The slice is <c>%</c> for a percentage and empty for each other kind.</returns>
    public ReadOnlySpan<byte> GetUnit(ReadOnlySpan<byte> source)
    {
        if (NumberLength == 0)
        {
            return [];
        }

        return source.Slice(Offset + NumberLength, Length - NumberLength);
    }
}
