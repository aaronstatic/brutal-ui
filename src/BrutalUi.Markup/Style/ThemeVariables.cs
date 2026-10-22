using System;
using System.Collections.Generic;
using System.Text;

namespace BrutalUi.Markup;

/// <summary>
/// The token range of the value of one theme variable. The range has no leading and no trailing whitespace.
/// </summary>
/// <param name="Start">The index of the first token of the value.</param>
/// <param name="End">The index after the last token of the value.</param>
internal readonly record struct VariableValue(int Start, int End);

/// <summary>
/// Pass 1 of the parser: the theme variables of the <c>:root</c> blocks of one token list.
/// </summary>
/// <remarks>
/// <para>
/// The grammar is <c>root-block := ':root' '{' ( '--name' ':' value-tokens ';' )* '}'</c>. A <c>:root</c> starts a
/// block only at the start of a top-level statement, as the whole selector. Pass 1 finds the statements with
/// <see cref="TokenSkip.Statement"/>, which ends each statement at the index where pass 2 ends it, also after an
/// error. A later definition of the same name wins. The names are case-sensitive (ordinal). A block can come before
/// or after the rules that read its variables, because pass 1 reads the whole text before pass 2 reads a rule.
/// </para>
/// <para>
/// The map lives in one <c>Parse</c> call. A second sheet does not see it. The class allocates, because the parser
/// runs at load time.
/// </para>
/// <para>
/// Pass 1 records each error of the blocks and goes on. After an error in a declaration, it skips to the next
/// <c>;</c> or <c>}</c> of the block, so the map stays complete for the declarations that are correct. A block holds
/// no nested braces, so it ends at its first <c>}</c> (DS4.8). Pass 2 reports no error inside a block.
/// </para>
/// <para>
/// A definition with an error gives no variable, and its name goes into the rejected set. Pass 2 then reports no
/// <see cref="StyleErrorKind.UnknownVariable"/> for a use of that name, because the definition error names the fault
/// (CR-2). A token error inside a declaration of a block drops each pass 1 error of that declaration and rejects the
/// definition, as the declaration rule of DS5.5 does in pass 2. A rejected byte gives no token, so these errors are
/// results of the token error.
/// </para>
/// </remarks>
internal sealed class ThemeVariables
{
    private readonly Dictionary<string, VariableValue> _values = new(StringComparer.Ordinal);
    private readonly HashSet<string> _rejected = new(StringComparer.Ordinal);
    private readonly List<StyleParseError> _errors = [];

    /// <summary>The token errors of the text, in the order of the source.</summary>
    private readonly List<StyleParseError> _tokenErrors;

    /// <summary>
    /// The index of the first token error that can lie in the next declaration. The declarations come in the order
    /// of the source, so the index moves forward only, and the check of all declarations reads each token error once.
    /// </summary>
    private int _tokenErrorIndex;

    private ThemeVariables(List<StyleParseError> tokenErrors)
    {
        _tokenErrors = tokenErrors;
    }

    /// <summary>Gets the errors of pass 1 in the order of the source. The list is empty when the blocks are correct.</summary>
    public IReadOnlyList<StyleParseError> Errors => _errors;

    /// <summary>Gets the number of defined variables.</summary>
    public int Count => _values.Count;

    /// <summary>Gives the value range of a variable.</summary>
    /// <param name="name">The name with the leading <c>--</c>. The compare is ordinal.</param>
    /// <param name="value">The token range of the value.</param>
    /// <returns><see langword="true"/> when the name is defined.</returns>
    public bool TryGet(string name, out VariableValue value)
    {
        return _values.TryGetValue(name, out value);
    }

    /// <summary>
    /// Gives a value that shows whether a <c>:root</c> block holds a definition of a name with an error. The text then
    /// has an error at that definition, or a token error inside it.
    /// </summary>
    /// <remarks>
    /// A name can have a rejected definition and a correct definition. <see cref="TryGet"/> then gives the value of
    /// the correct definition, so check <see cref="TryGet"/> first.
    /// </remarks>
    /// <param name="name">The name with the leading <c>--</c>. The compare is ordinal.</param>
    /// <returns><see langword="true"/> when a definition of the name has an error.</returns>
    public bool IsRejected(string name)
    {
        return _rejected.Contains(name);
    }

    /// <summary>Reads each <c>:root</c> block of the token list.</summary>
    /// <param name="source">The source span that the tokenizer read.</param>
    /// <param name="tokens">The token list. It ends with the <see cref="StyleTokenKind.End"/> token.</param>
    /// <param name="sourceName">The source name for the errors.</param>
    /// <param name="tokenErrors">The token errors of the text, in the order of the source. The method only reads them.</param>
    /// <returns>The variables and the errors.</returns>
    public static ThemeVariables Read(ReadOnlySpan<byte> source, List<StyleToken> tokens, string sourceName, List<StyleParseError> tokenErrors)
    {
        ThemeVariables variables = new(tokenErrors);
        int i = 0;

        while (true)
        {
            while (tokens[i].Kind == StyleTokenKind.Whitespace)
            {
                i++;
            }

            if (tokens[i].Kind == StyleTokenKind.End)
            {
                return variables;
            }

            // Each statement moves the index at least one token, because its first token is not the End token.
            i = StartsRootBlock(source, tokens, i)
                ? variables.ReadBlock(source, tokens, sourceName, i)
                : TokenSkip.Statement(source, tokens, i);
        }
    }

    /// <summary>
    /// Gives a value that shows whether the rule at an index is a <c>:root</c> block: <c>':' 'root'</c>, optional
    /// whitespace, and <c>'{'</c>. A <c>:root</c> in another place goes to <see cref="SelectorReader"/>, which
    /// rejects it.
    /// </summary>
    /// <param name="source">The source span that the tokenizer read.</param>
    /// <param name="tokens">The token list.</param>
    /// <param name="index">The index of the first token of the rule.</param>
    /// <returns><see langword="true"/> when the rule is a <c>:root</c> block.</returns>
    public static bool StartsRootBlock(ReadOnlySpan<byte> source, List<StyleToken> tokens, int index)
    {
        if (tokens[index].Kind != StyleTokenKind.Colon)
        {
            return false;
        }

        // The token after a ':' is never past the End token, because the ':' is not the End token.
        StyleToken name = tokens[index + 1];
        if (name.Kind != StyleTokenKind.Ident || !name.GetText(source).SequenceEqual("root"u8))
        {
            return false;
        }

        int next = index + 2;
        while (tokens[next].Kind == StyleTokenKind.Whitespace)
        {
            next++;
        }

        return tokens[next].Kind == StyleTokenKind.LeftBrace;
    }

    /// <summary>Gives a value that shows whether the token at an index starts a <c>var(</c> call.</summary>
    /// <param name="source">The source span that the tokenizer read.</param>
    /// <param name="tokens">The token list.</param>
    /// <param name="index">The token index.</param>
    /// <returns><see langword="true"/> for the identifier <c>var</c> and then <c>(</c> with no gap.</returns>
    public static bool IsVarCall(ReadOnlySpan<byte> source, List<StyleToken> tokens, int index)
    {
        StyleToken token = tokens[index];
        return token.Kind == StyleTokenKind.Ident
            && token.GetText(source).SequenceEqual("var"u8)
            && tokens[index + 1].Kind == StyleTokenKind.LeftParen;
    }

    /// <summary>Gives a value that shows whether an identifier is a variable name: <c>--</c> and at least one more byte.</summary>
    /// <param name="source">The source span that the tokenizer read.</param>
    /// <param name="token">The token.</param>
    /// <returns><see langword="true"/> for a variable name.</returns>
    public static bool IsVariableName(ReadOnlySpan<byte> source, in StyleToken token)
    {
        return token.Kind == StyleTokenKind.Ident && token.Length > 2 && token.GetText(source).StartsWith("--"u8);
    }

    /// <summary>Reads one block and gives the index after its <c>}</c>, or the index of the End token.</summary>
    private int ReadBlock(ReadOnlySpan<byte> source, List<StyleToken> tokens, string sourceName, int colonIndex)
    {
        int i = colonIndex + 2;
        while (tokens[i].Kind != StyleTokenKind.LeftBrace)
        {
            i++;
        }

        i++;

        while (true)
        {
            while (tokens[i].Kind == StyleTokenKind.Whitespace)
            {
                i++;
            }

            StyleToken token = tokens[i];
            if (token.Kind == StyleTokenKind.RightBrace)
            {
                return i + 1;
            }

            if (token.Kind == StyleTokenKind.End)
            {
                // A declaration that stops at the End token records its own UnexpectedEnd. The block adds no copy.
                if (_errors.Count == 0 || !IsErrorAt(_errors[^1], StyleErrorKind.UnexpectedEnd, in token))
                {
                    Record(StyleErrorKind.UnexpectedEnd, in token, sourceName, "The ':root' block has no '}' before the end of the text. Write '}' to end the block.");
                }

                return i;
            }

            int start = i;
            int errorMark = _errors.Count;
            i = ReadDeclaration(source, tokens, sourceName, start, out string? variableName, out VariableValue value, out bool correct);

            // A rejected byte gives no token, so each error of a declaration with a token error is a result of the
            // token error (the declaration rule of DS5.5). The definition is then not correct.
            if (HasTokenError(tokens, start, i))
            {
                _errors.RemoveRange(errorMark, _errors.Count - errorMark);
                correct = false;
            }

            if (variableName is null)
            {
                continue;
            }

            if (correct)
            {
                // A later definition of the same name wins.
                _values[variableName] = value;
            }
            else
            {
                // The definition error (or the token error) names the fault, so a use of the name gives no second error.
                _rejected.Add(variableName);
            }
        }
    }

    /// <summary>
    /// Reads one declaration of a block from its first token, and records each error of the declaration. A correct
    /// declaration is <c>'--name' ':' value-tokens</c> and then <c>;</c>, or nothing before the <c>}</c>.
    /// </summary>
    /// <param name="source">The source span that the tokenizer read.</param>
    /// <param name="tokens">The token list.</param>
    /// <param name="sourceName">The source name for the errors.</param>
    /// <param name="start">The index of the first token. It is not whitespace, a <c>}</c>, or the End token.</param>
    /// <param name="variableName">The variable name of the declaration, or <see langword="null"/> when the first token is not a variable name.</param>
    /// <param name="value">The token range of the value. Use it only when <paramref name="correct"/> is <see langword="true"/>.</param>
    /// <param name="correct"><see langword="true"/> when the declaration has no error.</param>
    /// <returns>The index after the <c>;</c>, or the index of the <c>}</c> or the End token that ends the declaration.</returns>
    private int ReadDeclaration(
        ReadOnlySpan<byte> source,
        List<StyleToken> tokens,
        string sourceName,
        int start,
        out string? variableName,
        out VariableValue value,
        out bool correct)
    {
        variableName = null;
        value = default;
        correct = false;

        int i = start;
        StyleToken name = tokens[i];
        if (TokenSkip.IsAtSign(source, in name))
        {
            Record(
                StyleErrorKind.UnsupportedSyntax,
                in name,
                sourceName,
                StyleValues.Describe(source, in name) + " starts an at-rule. The format has no at-rule. Remove it from the ':root' block.");
            return SkipDeclaration(tokens, i);
        }

        if (!IsVariableName(source, in name))
        {
            string message = name.Kind == StyleTokenKind.Ident
                ? StyleValues.Describe(source, in name) + " is not a variable. A ':root' block holds variables only. Write a name that starts with '--', for example '--accent: #3060c0;'."
                : StyleValues.Describe(source, in name) + " is not correct in a ':root' block. Write a variable such as '--accent: #3060c0;', or write '}' to end the block.";
            Record(StyleErrorKind.InvalidVariable, in name, sourceName, message);
            return SkipDeclaration(tokens, i);
        }

        // From here, each error rejects the name, so a use of the name gives no second error.
        variableName = Encoding.UTF8.GetString(name.GetText(source));

        i++;
        while (tokens[i].Kind == StyleTokenKind.Whitespace)
        {
            i++;
        }

        StyleToken colon = tokens[i];
        if (colon.Kind != StyleTokenKind.Colon)
        {
            if (colon.Kind == StyleTokenKind.End)
            {
                Record(StyleErrorKind.UnexpectedEnd, in colon, sourceName, "The ':root' block has no '}' before the end of the text. Write ':' and a value after the variable name, and '}' to end the block.");
                return i;
            }

            Record(
                StyleErrorKind.UnexpectedToken,
                in colon,
                sourceName,
                StyleValues.Describe(source, in colon) + " is not correct here. Write ':' after the variable name, for example '--accent: #3060c0;'.");
            return SkipDeclaration(tokens, i);
        }

        i++;
        int valueStart = i;
        while (tokens[i].Kind is not (StyleTokenKind.Semicolon or StyleTokenKind.RightBrace or StyleTokenKind.End))
        {
            i++;
        }

        StyleToken end = tokens[i];
        if (end.Kind == StyleTokenKind.End)
        {
            Record(StyleErrorKind.UnexpectedEnd, in end, sourceName, "The ':root' block has no '}' before the end of the text. Write ';' after the value and '}' to end the block.");
            return i;
        }

        int valueEnd = i;
        while (valueStart < valueEnd && tokens[valueStart].Kind == StyleTokenKind.Whitespace)
        {
            valueStart++;
        }

        while (valueEnd > valueStart && tokens[valueEnd - 1].Kind == StyleTokenKind.Whitespace)
        {
            valueEnd--;
        }

        if (valueStart == valueEnd)
        {
            Record(
                StyleErrorKind.InvalidVariable,
                in end,
                sourceName,
                "The value of " + StyleValues.Describe(source, in name) + " is missing before " + StyleValues.Describe(source, in end) + ". Write a value after ':', for example '#3060c0'.");
        }
        else if (CheckValue(source, tokens, sourceName, in name, valueStart, valueEnd))
        {
            value = new VariableValue(valueStart, valueEnd);
            correct = true;
        }

        // The block loop reads the '}'.
        return end.Kind == StyleTokenKind.Semicolon ? i + 1 : i;
    }

    /// <summary>
    /// Gives a value that shows whether a token error lies inside one declaration: from its first token up to its
    /// <c>;</c>, or up to the <c>}</c> or the End token that ends it. This is the range of the declaration rule of
    /// DS5.5 in pass 2.
    /// </summary>
    /// <param name="tokens">The token list.</param>
    /// <param name="start">The index of the first token of the declaration.</param>
    /// <param name="next">The index that <see cref="ReadDeclaration"/> gives.</param>
    /// <returns><see langword="true"/> when a token error lies in the range.</returns>
    private bool HasTokenError(List<StyleToken> tokens, int start, int next)
    {
        StyleToken first = tokens[start];

        // The token errors before this declaration cannot lie in a later declaration, so the index skips them once.
        while (_tokenErrorIndex < _tokenErrors.Count
            && StyleSheetParser.ComparePosition(_tokenErrors[_tokenErrorIndex].Line, _tokenErrors[_tokenErrorIndex].Column, first.Line, first.Column) < 0)
        {
            _tokenErrorIndex++;
        }

        if (_tokenErrorIndex == _tokenErrors.Count)
        {
            return false;
        }

        StyleToken stop = tokens[next];
        int upperLine = stop.Line;
        int upperColumn = stop.Column;
        if (next > start && tokens[next - 1].Kind == StyleTokenKind.Semicolon)
        {
            StyleToken semicolon = tokens[next - 1];
            upperLine = semicolon.Line;
            upperColumn = semicolon.Column + 1;
        }

        StyleParseError tokenError = _tokenErrors[_tokenErrorIndex];
        return StyleSheetParser.ComparePosition(tokenError.Line, tokenError.Column, upperLine, upperColumn) < 0;
    }

    /// <summary>Gives a value that shows whether an error has a kind and the position of a token.</summary>
    private static bool IsErrorAt(StyleParseError error, StyleErrorKind kind, in StyleToken token)
    {
        return error.Kind == kind && error.Line == token.Line && error.Column == token.Column;
    }

    /// <summary>
    /// Checks the tokens of a variable value: a <c>var(</c> gives <see cref="StyleErrorKind.InvalidVariable"/>, and a
    /// function other than <c>var</c> or <c>!important</c> gives <see cref="StyleErrorKind.UnsupportedSyntax"/>. The
    /// first such token of the range gives the error.
    /// </summary>
    /// <returns><see langword="true"/> when the range has no such token.</returns>
    private bool CheckValue(ReadOnlySpan<byte> source, List<StyleToken> tokens, string sourceName, in StyleToken name, int start, int end)
    {
        for (int i = start; i < end; i++)
        {
            if (IsVarCall(source, tokens, i))
            {
                StyleToken use = tokens[i];
                Record(
                    StyleErrorKind.InvalidVariable,
                    in use,
                    sourceName,
                    StyleValues.Describe(source, in use) + " in the value of " + StyleValues.Describe(source, in name) + " reads another variable. A variable cannot read another variable. Write the value itself.");
                return false;
            }

            if (StyleValues.IsUnsupportedSyntax(source, tokens, i, out string? message))
            {
                StyleToken token = tokens[i];
                Record(StyleErrorKind.UnsupportedSyntax, in token, sourceName, message);
                return false;
            }
        }

        return true;
    }

    /// <summary>Skips to after the next <c>;</c>, or to the <c>}</c> or the End token, which the caller reads.</summary>
    /// <remarks>A <c>:root</c> block holds no nested braces (DS4.8), so this skip does not count the braces.</remarks>
    private static int SkipDeclaration(List<StyleToken> tokens, int index)
    {
        int i = index;
        while (tokens[i].Kind is not (StyleTokenKind.Semicolon or StyleTokenKind.RightBrace or StyleTokenKind.End))
        {
            i++;
        }

        return tokens[i].Kind == StyleTokenKind.Semicolon ? i + 1 : i;
    }

    /// <summary>Records an error. Pass 1 records each error and goes on.</summary>
    private void Record(StyleErrorKind kind, in StyleToken token, string sourceName, string message)
    {
        _errors.Add(new StyleParseError(kind, token.Line, token.Column, message, sourceName));
    }
}
