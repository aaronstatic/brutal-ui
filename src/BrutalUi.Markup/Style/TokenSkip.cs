using System;
using System.Collections.Generic;

namespace BrutalUi.Markup;

/// <summary>
/// The skip rules of the error recovery. Pass 1 and pass 2 use the same rules, so both passes find the same top-level
/// statements.
/// </summary>
/// <remarks>
/// <para>
/// Each skip honors the nesting of the braces: a <c>{</c> opens a level, and a <c>}</c> closes it. A skip never moves
/// past the <see cref="StyleTokenKind.End"/> token, and it moves at least one token when the start token is not the
/// End token and not a stop token. A skip reports no error. The caller reports one error for each fault.
/// </para>
/// <para>
/// A <c>:root</c> block holds no nested braces (DS4.8). Its skip ends at its first <c>}</c>.
/// </para>
/// </remarks>
internal static class TokenSkip
{
    /// <summary>
    /// Skips the full block of a rule: up to the <c>}</c> that closes the first level, or a <c>}</c> at the start
    /// level. The skip moves past that <c>}</c>.
    /// </summary>
    /// <param name="tokens">The token list. It ends with the <see cref="StyleTokenKind.End"/> token.</param>
    /// <param name="start">The index of the first token of the skip.</param>
    /// <param name="open"><see langword="true"/> when the skip stops at the End token with a level that is open.</param>
    /// <returns>The index after the <c>}</c>, or the index of the End token.</returns>
    public static int Block(List<StyleToken> tokens, int start, out bool open)
    {
        int depth = 0;
        int i = start;
        while (true)
        {
            switch (tokens[i].Kind)
            {
                case StyleTokenKind.End:
                    open = depth > 0;
                    return i;

                case StyleTokenKind.LeftBrace:
                    depth++;
                    break;

                case StyleTokenKind.RightBrace:
                    if (depth <= 1)
                    {
                        open = false;
                        return i + 1;
                    }

                    depth--;
                    break;
            }

            i++;
        }
    }

    /// <summary>
    /// Skips the rest of a declaration: up to the next <c>;</c> of the same block, or up to the <c>}</c> that ends
    /// the block. The skip moves past the <c>;</c> and stops at the <c>}</c>, so the block loop reads the <c>}</c>.
    /// </summary>
    /// <param name="tokens">The token list. It ends with the <see cref="StyleTokenKind.End"/> token.</param>
    /// <param name="start">The index of the first token of the skip.</param>
    /// <returns>The index after the <c>;</c>, the index of the <c>}</c>, or the index of the End token.</returns>
    public static int Declaration(List<StyleToken> tokens, int start)
    {
        int depth = 0;
        int i = start;
        while (true)
        {
            switch (tokens[i].Kind)
            {
                case StyleTokenKind.End:
                    return i;

                case StyleTokenKind.LeftBrace:
                    depth++;
                    break;

                case StyleTokenKind.RightBrace:
                    if (depth == 0)
                    {
                        return i;
                    }

                    depth--;
                    break;

                case StyleTokenKind.Semicolon:
                    if (depth == 0)
                    {
                        return i + 1;
                    }

                    break;
            }

            i++;
        }
    }

    /// <summary>
    /// Skips an at-rule such as <c>@import "a";</c> or <c>@media x { ... }</c>: up to the next <c>;</c> at the start
    /// level, or past the <c>}</c> that closes the first level, whichever comes first. A <c>}</c> at the start level
    /// also ends the skip, past the <c>}</c>.
    /// </summary>
    /// <param name="tokens">The token list. It ends with the <see cref="StyleTokenKind.End"/> token.</param>
    /// <param name="start">The index of the <c>@</c>.</param>
    /// <param name="open"><see langword="true"/> when the skip stops at the End token with a level that is open.</param>
    /// <returns>The index after the <c>;</c> or the <c>}</c>, or the index of the End token.</returns>
    public static int AtRule(List<StyleToken> tokens, int start, out bool open)
    {
        int depth = 0;
        int i = start;
        while (true)
        {
            switch (tokens[i].Kind)
            {
                case StyleTokenKind.End:
                    open = depth > 0;
                    return i;

                case StyleTokenKind.LeftBrace:
                    depth++;
                    break;

                case StyleTokenKind.RightBrace:
                    if (depth <= 1)
                    {
                        open = false;
                        return i + 1;
                    }

                    depth--;
                    break;

                case StyleTokenKind.Semicolon:
                    if (depth == 0)
                    {
                        open = false;
                        return i + 1;
                    }

                    break;
            }

            i++;
        }
    }

    /// <summary>Skips a <c>:root</c> block up to its first <c>}</c> (DS4.8), and past it.</summary>
    /// <param name="tokens">The token list. It ends with the <see cref="StyleTokenKind.End"/> token.</param>
    /// <param name="start">The index of the <c>:</c> of the block.</param>
    /// <returns>The index after the <c>}</c>, or the index of the End token.</returns>
    public static int RootBlock(List<StyleToken> tokens, int start)
    {
        int i = start;
        while (tokens[i].Kind is not (StyleTokenKind.RightBrace or StyleTokenKind.End))
        {
            i++;
        }

        return tokens[i].Kind == StyleTokenKind.End ? i : i + 1;
    }

    /// <summary>
    /// Skips one top-level statement: an at-rule, a <c>:root</c> block, or a rule. Pass 2 ends each rule at the same
    /// index, also after an error, so pass 1 finds each <c>:root</c> block that pass 2 finds.
    /// </summary>
    /// <param name="source">The source span that the tokenizer read.</param>
    /// <param name="tokens">The token list. It ends with the <see cref="StyleTokenKind.End"/> token.</param>
    /// <param name="start">The index of the first token of the statement. The token is not whitespace.</param>
    /// <returns>The index after the statement, or the index of the End token.</returns>
    public static int Statement(ReadOnlySpan<byte> source, List<StyleToken> tokens, int start)
    {
        if (IsAtSign(source, tokens[start]))
        {
            return AtRule(tokens, start, out _);
        }

        if (ThemeVariables.StartsRootBlock(source, tokens, start))
        {
            return RootBlock(tokens, start);
        }

        return Block(tokens, start, out _);
    }

    /// <summary>Gives a value that shows whether a token is the <c>@</c> of an at-rule.</summary>
    /// <param name="source">The source span that the tokenizer read.</param>
    /// <param name="token">The token.</param>
    /// <returns><see langword="true"/> for a <see cref="StyleTokenKind.Delim"/> token <c>@</c>.</returns>
    public static bool IsAtSign(ReadOnlySpan<byte> source, in StyleToken token)
    {
        return token.Kind == StyleTokenKind.Delim && source[token.Offset] == (byte)'@';
    }
}
