using System;
using System.Buffers;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace BrutalUi.Markup;

/// <summary>
/// Splits a UTF-8 stylesheet text into a list of <see cref="StyleToken"/> values.
/// </summary>
/// <remarks>
/// <para>
/// The tokenizer reads the full span one time. It reads each byte at most one time and never throws for any input.
/// The last token of the list is always <see cref="StyleTokenKind.End"/>.
/// </para>
/// <para>
/// The position: the line starts at 1. LF ends a line. CRLF and a lone CR each count as one line break.
/// FF does not end a line. The column starts at 1 and counts code points: each byte that is not a UTF-8
/// continuation byte (<c>10xxxxxx</c>) adds 1. A tab adds 1. A UTF-8 BOM at offset 0 adds no column and gives no token.
/// Outside a comment, each rejected sequence of bytes of 0x80 or more adds 1 (see the rejected bytes below).
/// </para>
/// <para>
/// Whitespace and comments: one run of space, tab, CR, LF, FF, and comments gives one
/// <see cref="StyleTokenKind.Whitespace"/> token when the run holds one whitespace byte or more.
/// A run of comments only gives no token, as in CSS. Thus <c>./**/a</c> gives the same tokens as <c>.a</c>.
/// A comment can hold each UTF-8 text.
/// </para>
/// <para>
/// The rejected bytes: a byte of 0x80 or more outside a comment, and a control byte other than tab, LF, FF, and CR
/// (0x00 to 0x1F, and 0x7F), each give one <see cref="StyleErrorKind.UnexpectedCharacter"/> error at the position of the byte.
/// The tokenizer skips the full code point and gives no token for it. Each rejected sequence outside a comment counts
/// one column, as the replacement character (U+FFFD) of an editor does: a valid code point, a cut code point, and a
/// stray continuation byte each count 1. Thus <c>80 C3 A9 78</c> gives errors at columns 1 and 2, and <c>x</c> is at
/// column 3. In a comment, a continuation byte adds no column. Each other printable ASCII byte that starts
/// no other token gives a <see cref="StyleTokenKind.Delim"/> token.
/// </para>
/// <para>
/// A <c>/*</c> with no <c>*/</c> gives one <see cref="StyleErrorKind.UnterminatedComment"/> error at the <c>/*</c>,
/// and the token list ends with the <see cref="StyleTokenKind.End"/> token after the last byte.
/// </para>
/// <para>
/// The tokenizer allocates the token list. It runs at load time only.
/// </para>
/// </remarks>
internal static class StyleTokenizer
{
    /// <summary>Splits a UTF-8 text into tokens.</summary>
    /// <param name="utf8">The UTF-8 text. A BOM at offset 0 is skipped.</param>
    /// <param name="sourceName">The name of the source. Each error holds it.</param>
    /// <param name="errors">The list that gets each error, in the order of the source.</param>
    /// <returns>The tokens in the order of the source. The last token is <see cref="StyleTokenKind.End"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="sourceName"/> or <paramref name="errors"/> is <see langword="null"/>.</exception>
    public static List<StyleToken> Tokenize(ReadOnlySpan<byte> utf8, string sourceName, List<StyleParseError> errors)
    {
        ArgumentNullException.ThrowIfNull(sourceName);
        ArgumentNullException.ThrowIfNull(errors);

        List<StyleToken> tokens = [];
        Cursor cursor = new(utf8);

        if (utf8.Length >= 3 && utf8[0] == 0xEF && utf8[1] == 0xBB && utf8[2] == 0xBF)
        {
            cursor.Pos = 3;
        }

        while (cursor.Pos < utf8.Length)
        {
            int start = cursor.Pos;
            int line = cursor.Line;
            int column = cursor.Column;
            byte b = utf8[start];

            if (IsWhitespace(b) || IsCommentStart(utf8, start))
            {
                if (!ReadWhitespace(ref cursor, tokens, sourceName, errors))
                {
                    break;
                }

                continue;
            }

            if (IsIdentStart(utf8, start))
            {
                SkipIdent(ref cursor);
                tokens.Add(new StyleToken(StyleTokenKind.Ident, start, cursor.Pos - start, line, column));
                continue;
            }

            if (IsNumberStart(utf8, start))
            {
                tokens.Add(ReadNumber(ref cursor));
                continue;
            }

            StyleTokenKind kind;
            switch (b)
            {
                case (byte)'#':
                    cursor.Advance();
                    if (cursor.Pos < utf8.Length && IsNameByte(utf8[cursor.Pos]))
                    {
                        SkipNameBytes(ref cursor);
                        kind = StyleTokenKind.Hash;
                    }
                    else
                    {
                        kind = StyleTokenKind.Delim;
                    }

                    tokens.Add(new StyleToken(kind, start, cursor.Pos - start, line, column));
                    continue;

                case (byte)':': kind = StyleTokenKind.Colon; break;
                case (byte)';': kind = StyleTokenKind.Semicolon; break;
                case (byte)',': kind = StyleTokenKind.Comma; break;
                case (byte)'.': kind = StyleTokenKind.Dot; break;
                case (byte)'>': kind = StyleTokenKind.Greater; break;
                case (byte)'{': kind = StyleTokenKind.LeftBrace; break;
                case (byte)'}': kind = StyleTokenKind.RightBrace; break;
                case (byte)'(': kind = StyleTokenKind.LeftParen; break;
                case (byte)')': kind = StyleTokenKind.RightParen; break;

                default:
                    if (b >= 0x80)
                    {
                        SkipNonAscii(ref cursor, sourceName, errors);
                        continue;
                    }

                    if (b < 0x20 || b == 0x7F)
                    {
                        errors.Add(new StyleParseError(
                            StyleErrorKind.UnexpectedCharacter,
                            line,
                            column,
                            "The control character '0x" + b.ToString("X2", CultureInfo.InvariantCulture) + "' is not valid outside a comment. Remove it, or use a space.",
                            sourceName));
                        cursor.Advance();
                        continue;
                    }

                    kind = StyleTokenKind.Delim;
                    break;
            }

            cursor.Advance();
            tokens.Add(new StyleToken(kind, start, 1, line, column));
        }

        tokens.Add(new StyleToken(StyleTokenKind.End, cursor.Pos, 0, cursor.Line, cursor.Column));
        return tokens;
    }

    /// <summary>Reads one run of whitespace bytes and comments.</summary>
    /// <param name="cursor">The cursor at the first byte of the run.</param>
    /// <param name="tokens">The token list.</param>
    /// <param name="sourceName">The name of the source.</param>
    /// <param name="errors">The error list.</param>
    /// <returns><see langword="false"/> when a comment has no end. The cursor is then at the end of the text.</returns>
    private static bool ReadWhitespace(ref Cursor cursor, List<StyleToken> tokens, string sourceName, List<StyleParseError> errors)
    {
        ReadOnlySpan<byte> source = cursor.Source;
        int start = cursor.Pos;
        int line = cursor.Line;
        int column = cursor.Column;
        bool hasWhitespace = false;
        bool terminated = true;

        while (cursor.Pos < source.Length)
        {
            if (IsWhitespace(source[cursor.Pos]))
            {
                hasWhitespace = true;
                cursor.Advance();
                continue;
            }

            if (!IsCommentStart(source, cursor.Pos))
            {
                break;
            }

            int commentLine = cursor.Line;
            int commentColumn = cursor.Column;
            cursor.Advance();
            cursor.Advance();
            terminated = false;

            while (cursor.Pos < source.Length)
            {
                if (source[cursor.Pos] == (byte)'*' && cursor.Pos + 1 < source.Length && source[cursor.Pos + 1] == (byte)'/')
                {
                    cursor.Advance();
                    cursor.Advance();
                    terminated = true;
                    break;
                }

                cursor.Advance();
            }

            if (!terminated)
            {
                errors.Add(new StyleParseError(
                    StyleErrorKind.UnterminatedComment,
                    commentLine,
                    commentColumn,
                    "The comment '/*' has no '*/' before the end of the text. Add '*/' to end the comment.",
                    sourceName));
                break;
            }
        }

        if (hasWhitespace)
        {
            tokens.Add(new StyleToken(StyleTokenKind.Whitespace, start, cursor.Pos - start, line, column));
        }

        return terminated;
    }

    /// <summary>Reads a number, and the unit or the <c>%</c> right after it.</summary>
    /// <param name="cursor">The cursor at the first byte of the number.</param>
    /// <returns>A <see cref="StyleTokenKind.Number"/>, a <see cref="StyleTokenKind.Dimension"/>, or a <see cref="StyleTokenKind.Percentage"/> token.</returns>
    private static StyleToken ReadNumber(ref Cursor cursor)
    {
        ReadOnlySpan<byte> source = cursor.Source;
        int start = cursor.Pos;
        int line = cursor.Line;
        int column = cursor.Column;

        if (source[cursor.Pos] == (byte)'-')
        {
            cursor.Advance();
        }

        SkipDigits(ref cursor);
        if (cursor.Pos + 1 < source.Length && source[cursor.Pos] == (byte)'.' && IsDigit(source[cursor.Pos + 1]))
        {
            cursor.Advance();
            SkipDigits(ref cursor);
        }

        int numberLength = cursor.Pos - start;
        StyleTokenKind kind = StyleTokenKind.Number;

        if (cursor.Pos < source.Length && source[cursor.Pos] == (byte)'%')
        {
            cursor.Advance();
            kind = StyleTokenKind.Percentage;
        }
        else if (IsIdentStart(source, cursor.Pos))
        {
            SkipIdent(ref cursor);
            kind = StyleTokenKind.Dimension;
        }

        return new StyleToken(kind, start, cursor.Pos - start, line, column, numberLength);
    }

    /// <summary>Gives an error for one code point of 0x80 or more, and skips the code point.</summary>
    /// <param name="cursor">The cursor at the first byte of the code point.</param>
    /// <param name="sourceName">The name of the source.</param>
    /// <param name="errors">The error list.</param>
    private static void SkipNonAscii(ref Cursor cursor, string sourceName, List<StyleParseError> errors)
    {
        ReadOnlySpan<byte> rest = cursor.Source[cursor.Pos..];
        OperationStatus status = Rune.DecodeFromUtf8(rest, out Rune rune, out int consumed);

        string message = status == OperationStatus.Done
            ? "The character '" + rune.ToString() + "' (U+" + rune.Value.ToString("X4", CultureInfo.InvariantCulture) + ") is not valid outside a comment. Use ASCII text."
            : "The byte '0x" + rest[0].ToString("X2", CultureInfo.InvariantCulture) + "' is not valid UTF-8 text. Use ASCII text outside a comment.";

        int column = cursor.Column;
        errors.Add(new StyleParseError(StyleErrorKind.UnexpectedCharacter, cursor.Line, column, message, sourceName));

        // The decoder consumes 1 byte or more, and never an ASCII byte. The guard keeps the loop finite for each input.
        int end = cursor.Pos + Math.Max(consumed, 1);
        while (cursor.Pos < end)
        {
            cursor.Advance();
        }

        // Each rejected sequence counts one column, as the replacement character of an editor does (CR-3). A valid
        // code point already counts one column. A stray continuation byte counts no column in Advance, so without
        // this line the next character gets the same column, and the merge drops its error as a copy. The skipped
        // bytes are 0x80 or more, so they hold no line break, and the line does not change.
        cursor.Column = column + 1;
    }

    private static void SkipIdent(ref Cursor cursor)
    {
        // The first byte is a letter, '_', or '-'. Each of them is also a name byte.
        cursor.Advance();
        SkipNameBytes(ref cursor);
    }

    private static void SkipNameBytes(ref Cursor cursor)
    {
        while (cursor.Pos < cursor.Source.Length && IsNameByte(cursor.Source[cursor.Pos]))
        {
            cursor.Advance();
        }
    }

    private static void SkipDigits(ref Cursor cursor)
    {
        while (cursor.Pos < cursor.Source.Length && IsDigit(cursor.Source[cursor.Pos]))
        {
            cursor.Advance();
        }
    }

    private static bool IsWhitespace(byte b)
    {
        return b is (byte)' ' or (byte)'\t' or (byte)'\r' or (byte)'\n' or 0x0C;
    }

    private static bool IsCommentStart(ReadOnlySpan<byte> source, int pos)
    {
        return source[pos] == (byte)'/' && pos + 1 < source.Length && source[pos + 1] == (byte)'*';
    }

    private static bool IsDigit(byte b)
    {
        return b is >= (byte)'0' and <= (byte)'9';
    }

    private static bool IsLetterOrUnderscore(byte b)
    {
        return b is (>= (byte)'a' and <= (byte)'z') or (>= (byte)'A' and <= (byte)'Z') or (byte)'_';
    }

    private static bool IsNameByte(byte b)
    {
        return IsLetterOrUnderscore(b) || IsDigit(b) || b == (byte)'-';
    }

    /// <summary>Finds out if an identifier starts at a position: <c>[A-Za-z_]</c>, or <c>-</c> and then <c>[A-Za-z_-]</c>.</summary>
    private static bool IsIdentStart(ReadOnlySpan<byte> source, int pos)
    {
        if (pos >= source.Length)
        {
            return false;
        }

        byte b = source[pos];
        if (IsLetterOrUnderscore(b))
        {
            return true;
        }

        return b == (byte)'-' && pos + 1 < source.Length && (IsLetterOrUnderscore(source[pos + 1]) || source[pos + 1] == (byte)'-');
    }

    /// <summary>Finds out if a number starts at a position: a digit, <c>.digit</c>, <c>-digit</c>, or <c>-.digit</c>.</summary>
    private static bool IsNumberStart(ReadOnlySpan<byte> source, int pos)
    {
        int i = pos;
        if (source[i] == (byte)'-')
        {
            i++;
        }

        if (i < source.Length && source[i] == (byte)'.')
        {
            i++;
        }

        return i < source.Length && IsDigit(source[i]);
    }

    /// <summary>The read position and the line and column of the byte at that position.</summary>
    private ref struct Cursor
    {
        public readonly ReadOnlySpan<byte> Source;
        public int Pos;
        public int Line;
        public int Column;

        public Cursor(ReadOnlySpan<byte> source)
        {
            Source = source;
            Pos = 0;
            Line = 1;
            Column = 1;
        }

        /// <summary>Moves past one byte. A CRLF pair moves past two bytes and counts as one line break.</summary>
        public void Advance()
        {
            byte b = Source[Pos];
            Pos++;

            if (b == (byte)'\r')
            {
                if (Pos < Source.Length && Source[Pos] == (byte)'\n')
                {
                    Pos++;
                }

                Line++;
                Column = 1;
            }
            else if (b == (byte)'\n')
            {
                Line++;
                Column = 1;
            }
            else if ((b & 0xC0) != 0x80)
            {
                Column++;
            }
        }
    }
}
