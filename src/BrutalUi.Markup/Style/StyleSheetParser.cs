using System;
using System.Collections.Generic;
using System.Text;
using BrutalUi.Core;

namespace BrutalUi.Markup;

/// <summary>
/// Reads a stylesheet text and gives a <see cref="StyleSheet"/>, or a list of positioned errors.
/// </summary>
/// <remarks>
/// <para>
/// The parser runs at load time. It allocates: the UTF-8 copy of a <see cref="string"/> text, the token list, the
/// class names, the messages, and the sheet. The sheet that it gives allocates nothing in
/// <see cref="UiContext.Update(float)"/>. It is the same sheet type that <see cref="StyleSheet.Builder"/> makes.
/// </para>
/// <para>
/// The parser holds no state between calls. Each <c>Parse</c> call makes its own token list. Thus one parser can
/// parse more than one text, also on more than one thread at a time.
/// </para>
/// <para>
/// The format: a rule is a selector list and a block of declarations. A selector holds one compound, or two compounds
/// with <c>&gt;</c> (the child chain) or whitespace (the descendant chain) between them. A compound holds a type
/// (<c>Label</c>), a class (<c>.panel</c>), a name (<c>#title</c>), and states (<c>:hover</c>, <c>:active</c>,
/// <c>:focus</c>, <c>:disabled</c>, and <c>:checked</c>). Each part is optional, but a compound needs one part, and
/// the parts of a compound must touch. Each name is case-sensitive. A declaration sets one of the 32 properties
/// (the layout, paint, and text members of <see cref="StyleProperty"/>) to a length, a number, a keyword, an edge
/// set or a corner set of one to four values, a color, or a font. A later declaration of the same property wins.
/// A <c>transition</c> declaration (<c>transition: background-color 0.2s ease-out, opacity 150ms linear;</c>) goes
/// into <see cref="StyleRule.Transitions"/>. It names <c>background-color</c>, <c>border-color</c>, <c>color</c>,
/// <c>border-radius</c>, or <c>opacity</c>, a duration in <c>s</c> or <c>ms</c>, and the easing <c>linear</c>,
/// <c>ease-in</c>, <c>ease-out</c>, or <c>ease-in-out</c>. A later transition declaration replaces only the
/// transitions of its own members. The last <c>;</c> of a block is optional. Whitespace and comments can come between each two tokens outside a
/// compound. A list of values (the edges, the corners, and the parts of a transition) needs whitespace between two
/// values. A comment alone does not separate two values.
/// </para>
/// <para>
/// A <c>:root</c> block holds theme variables: <c>:root { --accent: #3060c0; }</c>. A value reads a variable with
/// <c>var(--accent)</c>. The parser resolves the variables at parse time, for each text. A block can come before or
/// after the rules that read it, and a later definition of the same name wins. A variable can hold more than one
/// value (<c>--pad: 4px 8px;</c>), and a value can mix variables with other values. A variable name is
/// case-sensitive. A second <c>Parse</c> call does not see the variables of the first. A variable cannot read another
/// variable, and <c>var()</c> takes no fallback value. The parser checks the value of a variable at each use, with the
/// form of the property that reads it. A variable that no rule reads gets no value check, as in CSS. A definition with
/// an error gives one error at the definition and no error at a use of the name.
/// </para>
/// <para>
/// The rules go into the sheet in the order of the source. A selector list gives one rule for each selector, with
/// the same values. A rule with no declaration gives no rule. The sheet sorts the rules by the specificity, so a later
/// rule wins over an earlier rule at equal specificity. An unknown type name is no error: the rule matches no element
/// until an element type with that name exists.
/// </para>
/// <para>
/// The error report: the parser records each error and continues. After an error in a declaration, it skips to the
/// next <c>;</c> or <c>}</c> of the same block. After an error in a selector, it skips the full block of the rule.
/// An at-rule (<c>@import</c>, <c>@media</c>) gives <see cref="StyleErrorKind.UnsupportedSyntax"/> at the <c>@</c>,
/// and the parser skips it to its <c>;</c> or to the end of its block. Each skip counts the nested braces. A missing
/// <c>}</c> at the end gives <see cref="StyleErrorKind.UnexpectedEnd"/>. <c>!important</c> and a function other than
/// <c>var</c> (<c>calc(</c>, <c>rgb(</c>) give <see cref="StyleErrorKind.UnsupportedSyntax"/>. A text with one error
/// or more gives no sheet and adds no name to the global name tables. Each message quotes the source text at fault
/// and names the fix.
/// </para>
/// </remarks>
public sealed class StyleSheetParser
{
    /// <summary>
    /// The settings of a <see cref="StyleSheetParser"/>.
    /// </summary>
    /// <remarks>
    /// <c>default(CreateInfo)</c> gives the default settings.
    /// </remarks>
    public struct CreateInfo
    {
        /// <summary>
        /// The font map of the <c>font</c> property: a name in the text and the handle that the name gives. The
        /// default is <see langword="null"/>, which allows only <c>font: default;</c>. The parser copies the map
        /// when it is created, so a later change of the map does not change the parser. The compare of a name is
        /// ordinal. A key <c>default</c> in the map wins over the built-in id 0.
        /// </summary>
        public IReadOnlyDictionary<string, FontHandle>? Fonts;
    }

    private readonly Dictionary<string, FontHandle> _fonts;

    /// <summary>Creates a parser with the default settings.</summary>
    public StyleSheetParser()
    {
        _fonts = new Dictionary<string, FontHandle>(StringComparer.Ordinal);
    }

    /// <summary>Creates a parser with the given settings.</summary>
    /// <param name="info">The settings. The parser keeps a copy of the values that it uses.</param>
    public StyleSheetParser(in CreateInfo info)
    {
        _fonts = new Dictionary<string, FontHandle>(StringComparer.Ordinal);
        if (info.Fonts is not null)
        {
            foreach (KeyValuePair<string, FontHandle> pair in info.Fonts)
            {
                _fonts[pair.Key] = pair.Value;
            }
        }
    }

    /// <summary>Parses a UTF-8 stylesheet text.</summary>
    /// <remarks>
    /// A UTF-8 BOM at the start is skipped. The method allocates. Call it at load time, not in the frame loop.
    /// </remarks>
    /// <param name="utf8">The UTF-8 text.</param>
    /// <param name="sourceName">The name of the source, for example a file name. Each error holds it.</param>
    /// <returns>The sheet, or the errors in the order of the source. The method never throws for the text.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="sourceName"/> is <see langword="null"/>.</exception>
    public StyleParseResult Parse(ReadOnlySpan<byte> utf8, string sourceName = "stylesheet")
    {
        ArgumentNullException.ThrowIfNull(sourceName);

        List<StyleParseError> tokenErrors = [];
        List<StyleToken> tokens = StyleTokenizer.Tokenize(utf8, sourceName, tokenErrors);

        // Pass 1 reads the variables of each ':root' block. Pass 2 reads the rules and puts the variables in place.
        // Both passes run also after a token error, so the report holds each fault of the text.
        ThemeVariables variables = ThemeVariables.Read(utf8, tokens, sourceName, tokenErrors);

        List<StyleParseError> ruleErrors = [];
        List<SelectorSyntax> selectors = [];
        List<PendingRule> rules = [];
        RuleReader reader = new(utf8, tokens, sourceName, ruleErrors, tokenErrors, _fonts, variables, selectors, rules);
        reader.ReadSheet();

        List<StyleParseError> errors = MergeErrors(tokenErrors, variables.Errors, ruleErrors);
        if (errors.Count > 0)
        {
            return StyleParseResult.Failure(errors);
        }

        // The names go into the global tables only for a text with no error. A selector list gives one rule for each
        // selector, in the order of the source, with the same values.
        StyleSheet.Builder builder = new();
        foreach (PendingRule rule in rules)
        {
            StyleProperties properties = rule.Properties;
            StyleTransitions transitions = rule.Transitions;
            for (int i = rule.SelectorStart; i < rule.SelectorStart + rule.SelectorCount; i++)
            {
                SelectorSyntax selector = selectors[i];
                builder.Add(SelectorReader.Build(utf8, tokens, in selector), in properties, in transitions);
            }
        }

        return StyleParseResult.Success(builder.Build());
    }

    /// <summary>Parses a stylesheet text. The method encodes the text to UTF-8 and then parses the bytes.</summary>
    /// <remarks>
    /// A <c>U+FEFF</c> at the start is a BOM and is skipped. The method allocates. Call it at load time, not in the
    /// frame loop.
    /// </remarks>
    /// <param name="text">The text.</param>
    /// <param name="sourceName">The name of the source, for example a file name. Each error holds it.</param>
    /// <returns>The sheet, or the errors in the order of the source. The method never throws for the text.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="text"/> or <paramref name="sourceName"/> is <see langword="null"/>.</exception>
    public StyleParseResult Parse(string text, string sourceName = "stylesheet")
    {
        ArgumentNullException.ThrowIfNull(text);
        ArgumentNullException.ThrowIfNull(sourceName);

        return Parse(Encoding.UTF8.GetBytes(text), sourceName);
    }

    /// <summary>
    /// Merges the errors of the tokenizer, of pass 1, and of pass 2 in the order of the source (line, then column).
    /// </summary>
    /// <remarks>
    /// <para>
    /// Errors at the same position keep the order of the three lists. A second error with the same kind at the same
    /// position is a copy, and the merge drops it. Thus the End token gives one <see cref="StyleErrorKind.UnexpectedEnd"/>.
    /// </para>
    /// <para>
    /// An unterminated comment runs to the end of the text. Each <see cref="StyleErrorKind.UnexpectedEnd"/> of pass 1
    /// and of pass 2 is then a result of the comment, and the merge drops it.
    /// </para>
    /// </remarks>
    private static List<StyleParseError> MergeErrors(
        List<StyleParseError> tokenErrors,
        IReadOnlyList<StyleParseError> variableErrors,
        List<StyleParseError> ruleErrors)
    {
        bool openComment = false;
        foreach (StyleParseError error in tokenErrors)
        {
            openComment |= error.Kind == StyleErrorKind.UnterminatedComment;
        }

        List<StyleParseError> all = new(tokenErrors.Count + variableErrors.Count + ruleErrors.Count);
        all.AddRange(tokenErrors);
        foreach (StyleParseError error in variableErrors)
        {
            if (!(openComment && error.Kind == StyleErrorKind.UnexpectedEnd))
            {
                all.Add(error);
            }
        }

        foreach (StyleParseError error in ruleErrors)
        {
            if (!(openComment && error.Kind == StyleErrorKind.UnexpectedEnd))
            {
                all.Add(error);
            }
        }

        // A stable sort: the index in the list breaks a tie of the position.
        int[] order = new int[all.Count];
        for (int i = 0; i < order.Length; i++)
        {
            order[i] = i;
        }

        Array.Sort(order, (a, b) =>
        {
            int compare = ComparePosition(all[a].Line, all[a].Column, all[b].Line, all[b].Column);
            return compare != 0 ? compare : a.CompareTo(b);
        });

        List<StyleParseError> merged = new(all.Count);
        foreach (int i in order)
        {
            StyleParseError error = all[i];
            if (merged.Count > 0)
            {
                StyleParseError last = merged[^1];
                if (last.Line == error.Line && last.Column == error.Column && last.Kind == error.Kind)
                {
                    continue;
                }
            }

            merged.Add(error);
        }

        return merged;
    }

    /// <summary>Compares two positions of the source: the line first, then the column.</summary>
    /// <returns>A negative value, 0, or a positive value.</returns>
    internal static int ComparePosition(int line1, int column1, int line2, int column2)
    {
        return line1 != line2 ? line1.CompareTo(line2) : column1.CompareTo(column2);
    }

    /// <summary>One rule with no error: its range in the selector list and its values.</summary>
    /// <param name="SelectorStart">The index of the first selector of the rule.</param>
    /// <param name="SelectorCount">The number of selectors of the rule.</param>
    /// <param name="Properties">The values of the rule.</param>
    /// <param name="Transitions">The transitions of the rule.</param>
    private readonly record struct PendingRule(int SelectorStart, int SelectorCount, StyleProperties Properties, StyleTransitions Transitions);

    /// <summary>
    /// Reads the rules of one token list. The reader moves forward only. It records each error and continues after
    /// the skip of the recovery.
    /// </summary>
    /// <remarks>
    /// A token error marks its statement. A token error inside a declaration drops each other error of that
    /// declaration, and a token error inside a selector, up to the token after the error, drops the selector error.
    /// A rejected byte gives no token, so these errors are results of the token error, not second faults.
    /// </remarks>
    private ref struct RuleReader
    {
        private readonly ReadOnlySpan<byte> _source;
        private readonly List<StyleToken> _tokens;
        private readonly string _sourceName;
        private readonly List<StyleParseError> _errors;
        private readonly List<StyleParseError> _tokenErrors;

        /// <summary>The selectors of each rule with no error, in the order of the source.</summary>
        private readonly List<SelectorSyntax> _selectors;

        /// <summary>Each rule with a value, in the order of the source.</summary>
        private readonly List<PendingRule> _rules;
        private int _index;

        private readonly Dictionary<string, FontHandle> _fonts;
        private readonly ThemeVariables _variables;

        /// <summary>The tokens of the current value, with the variables in place. Each declaration clears and reuses the list.</summary>
        private readonly List<StyleToken> _value;

        /// <summary>For each entry of <see cref="_value"/>, the index of the token in the token list that an error points at.</summary>
        private readonly List<int> _map;

        /// <summary>For each entry of <see cref="_value"/>, the index of the <c>--name</c> token of the use, or -1.</summary>
        private readonly List<int> _useName;

        public RuleReader(
            ReadOnlySpan<byte> source,
            List<StyleToken> tokens,
            string sourceName,
            List<StyleParseError> errors,
            List<StyleParseError> tokenErrors,
            Dictionary<string, FontHandle> fonts,
            ThemeVariables variables,
            List<SelectorSyntax> selectors,
            List<PendingRule> rules)
        {
            _source = source;
            _tokens = tokens;
            _sourceName = sourceName;
            _errors = errors;
            _tokenErrors = tokenErrors;
            _fonts = fonts;
            _variables = variables;
            _selectors = selectors;
            _rules = rules;
            _value = [];
            _map = [];
            _useName = [];
            _index = 0;
        }

        /// <summary>Reads each top-level statement up to the <see cref="StyleTokenKind.End"/> token.</summary>
        public void ReadSheet()
        {
            while (true)
            {
                SkipWhitespace();
                if (Peek().Kind == StyleTokenKind.End)
                {
                    return;
                }

                // Each statement moves the read position at least one token, because its first token is not End.
                ReadStatement();
            }
        }

        /// <summary>
        /// Reads one top-level statement: an at-rule, a <c>:root</c> block, or a rule
        /// <c>selector ( ',' selector )* '{' declaration* '}'</c>. The statement ends at the index that
        /// <see cref="TokenSkip.Statement"/> gives, so pass 1 finds the same statements.
        /// </summary>
        private void ReadStatement()
        {
            int start = _index;
            StyleToken first = Peek();

            if (TokenSkip.IsAtSign(_source, in first))
            {
                AtRuleError(start);
                _index = TokenSkip.AtRule(_tokens, start, out bool atRuleOpen);
                if (atRuleOpen)
                {
                    StyleToken end = Peek();
                    Fail(StyleErrorKind.UnexpectedEnd, in end, "The at-rule has no '}' before the end of the text. Remove the at-rule.");
                }

                return;
            }

            if (ThemeVariables.StartsRootBlock(_source, _tokens, start))
            {
                // Pass 1 read the variables of the block and reported each error of the block.
                _index = TokenSkip.RootBlock(_tokens, start);
                return;
            }

            int errorMark = _errors.Count;
            int selectorStart = _selectors.Count;
            SelectorReader selectorReader = new(_source, _tokens, _sourceName, _errors, start);
            if (!selectorReader.ReadList(_selectors))
            {
                _selectors.RemoveRange(selectorStart, _selectors.Count - selectorStart);
                DropSelectorErrorAtTokenError(start, errorMark);

                // After an error in a selector, the parser skips the full block of the rule.
                _index = TokenSkip.Block(_tokens, start, out bool blockOpen);
                if (blockOpen)
                {
                    StyleToken end = Peek();
                    Fail(StyleErrorKind.UnexpectedEnd, in end, "The rule has no '}' before the end of the text. Write '}' to end the rule.");
                }

                return;
            }

            // A successful read stops at the '{'.
            _index = selectorReader.Index + 1;

            StyleProperties properties = default;
            StyleTransitions transitions = default;
            while (true)
            {
                SkipWhitespace();
                StyleToken token = Peek();
                if (token.Kind == StyleTokenKind.RightBrace)
                {
                    _index++;
                    break;
                }

                if (token.Kind == StyleTokenKind.End)
                {
                    Fail(StyleErrorKind.UnexpectedEnd, in token, "The rule has no '}' before the end of the text. Write '}' to end the rule.");
                    break;
                }

                int declarationStart = _index;
                int declarationMark = _errors.Count;
                bool ok;
                if (TokenSkip.IsAtSign(_source, in token))
                {
                    ok = AtRuleError(declarationStart);
                }
                else if (token.Kind != StyleTokenKind.Ident)
                {
                    ok = Unexpected(in token, "Write a declaration such as 'color: #ffffff;', or write '}' to end the rule.");
                }
                else
                {
                    ok = ReadDeclaration(ref properties, ref transitions);
                }

                if (!ok)
                {
                    // After an error in a declaration, the parser skips to the next ';' or '}' of the same block.
                    _index = TokenSkip.Declaration(_tokens, declarationStart);
                }

                DropDeclarationErrorsAtTokenError(declarationStart, declarationMark);
            }

            // A transition declaration is a value too, so a rule with only a transition gives a rule.
            if (!properties.IsEmpty || !transitions.IsEmpty)
            {
                _rules.Add(new PendingRule(selectorStart, _selectors.Count - selectorStart, properties, transitions));
            }
        }

        /// <summary>Records the error of an at-rule at its <c>@</c>.</summary>
        /// <returns>Always <see langword="false"/>.</returns>
        private readonly bool AtRuleError(int atIndex)
        {
            StyleToken at = _tokens[atIndex];
            StyleToken name = _tokens[atIndex + 1];
            string form = name.Kind == StyleTokenKind.Ident && name.Offset == at.Offset + 1
                ? "the at-rule '@" + Encoding.UTF8.GetString(name.GetText(_source)) + "'"
                : "an at-rule";
            return Fail(
                StyleErrorKind.UnsupportedSyntax,
                in at,
                StyleValues.Describe(_source, in at) + " starts " + form + ". The format has no at-rule. Remove it, and parse each other sheet with its own Parse call.");
        }

        /// <summary>Reads one declaration: <c>Ident ':' value</c> and then <c>';'</c>, or nothing before <c>'}'</c>.</summary>
        /// <returns><see langword="false"/> after an error. The caller then skips the rest of the declaration.</returns>
        private bool ReadDeclaration(ref StyleProperties properties, ref StyleTransitions transitions)
        {
            StyleToken name = Peek();
            if (ThemeVariables.IsVariableName(_source, in name))
            {
                return Fail(
                    StyleErrorKind.InvalidVariable,
                    in name,
                    StyleValues.Describe(_source, in name) + " is a variable name. A variable is valid in a ':root' block only. Move the line into a ':root' block, and read it with 'var(" + Encoding.UTF8.GetString(name.GetText(_source)) + ")'.");
            }

            if (!PropertyTable.TryGet(name.GetText(_source), out PropertyEntry entry))
            {
                return Fail(
                    StyleErrorKind.UnknownProperty,
                    in name,
                    StyleValues.Describe(_source, in name) + " is not a property of the format. Check the spelling, for example 'color'.");
            }

            _index++;
            SkipWhitespace();

            StyleToken colon = Peek();
            if (colon.Kind != StyleTokenKind.Colon)
            {
                return Unexpected(in colon, "Write ':' after the property name, for example 'color: #ffffff;'.");
            }

            _index++;
            SkipWhitespace();

            StyleToken value = Peek();
            if (value.Kind == StyleTokenKind.End)
            {
                return Unexpected(in value, "Write a value after ':', and then write ';' and '}'.");
            }

            if (value.Kind is StyleTokenKind.Semicolon or StyleTokenKind.RightBrace)
            {
                return Fail(
                    StyleErrorKind.InvalidValue,
                    in value,
                    "The value of " + StyleValues.Describe(_source, in name) + " is missing before " + StyleValues.Describe(_source, in value) + ". Write a value after ':', for example '#ffffff'.");
            }

            // The value list holds the tokens of the value, each var(--name) replaced by the tokens of the variable,
            // and then the terminator and the End token. The value readers read this list, not the token list.
            if (!BuildValue())
            {
                return false;
            }

            int valueIndex = 0;

            // A later declaration of the same property writes the member again, so it wins as in CSS. A later
            // transition declaration writes only the slots of its members.
            if (!StyleValues.TryRead(_source, _value, ref valueIndex, in entry, _fonts, ref properties, ref transitions, out ValueError error))
            {
                return FailValue(error.Kind, error.TokenIndex, error.Message);
            }

            while (_value[valueIndex].Kind == StyleTokenKind.Whitespace)
            {
                valueIndex++;
            }

            StyleToken end = _value[valueIndex];
            if (end.Kind == StyleTokenKind.Semicolon)
            {
                _index = _map[valueIndex] + 1;
            }
            else if (end.Kind == StyleTokenKind.End)
            {
                _index = _map[valueIndex];
                return Unexpected(in end, "Write ';' after the value and '}' at the end of the rule.");
            }
            else if (end.Kind == StyleTokenKind.RightBrace)
            {
                // The '}' ends the last declaration, so the last ';' is optional. The block loop reads the '}'.
                _index = _map[valueIndex];
            }
            else
            {
                // Each other token is a second value.
                return FailValue(
                    StyleErrorKind.InvalidValue,
                    valueIndex,
                    StyleValues.Describe(_source, in end) + " is one value too many. Write one value for "
                        + StyleValues.Describe(_source, in name) + ", and then write ';'.");
            }

            return true;
        }

        /// <summary>
        /// Fills the value list from the token list at the read position, up to the <c>;</c>, the <c>}</c>, or the End
        /// token. A <c>var(--name)</c> becomes the tokens of the variable. The list ends with the terminator and the
        /// End token. The map holds, for each entry, the index of the token that an error points at: the token itself,
        /// or the <c>var</c> token for a token that comes from a variable.
        /// </summary>
        /// <remarks>
        /// A function other than <c>var</c> and <c>!important</c> give <see cref="StyleErrorKind.UnsupportedSyntax"/>
        /// at their first token, before the value read.
        /// </remarks>
        /// <returns>
        /// <see langword="false"/> after an error. The error is in the error list, or it is the pass 1 error of a
        /// rejected definition (<see cref="ThemeVariables.IsRejected"/>).
        /// </returns>
        private bool BuildValue()
        {
            _value.Clear();
            _map.Clear();
            _useName.Clear();

            int i = _index;
            while (true)
            {
                StyleToken token = _tokens[i];
                if (token.Kind is StyleTokenKind.Semicolon or StyleTokenKind.RightBrace or StyleTokenKind.End)
                {
                    break;
                }

                if (ThemeVariables.IsVarCall(_source, _tokens, i))
                {
                    if (!ExpandVariable(ref i))
                    {
                        return false;
                    }

                    continue;
                }

                if (StyleValues.IsUnsupportedSyntax(_source, _tokens, i, out string? message))
                {
                    return Fail(StyleErrorKind.UnsupportedSyntax, in token, message);
                }

                AddValueToken(in token, i, -1);
                i++;
            }

            StyleToken terminator = _tokens[i];
            AddValueToken(in terminator, i, -1);
            if (terminator.Kind != StyleTokenKind.End)
            {
                int endIndex = _tokens.Count - 1;
                StyleToken end = _tokens[endIndex];
                AddValueToken(in end, endIndex, -1);
            }

            return true;
        }

        /// <summary>
        /// Reads <c>var(--name)</c> at an index and adds the tokens of the variable to the value list. The index moves
        /// to the token after the <c>)</c>.
        /// </summary>
        private bool ExpandVariable(ref int index)
        {
            int varIndex = index;
            int j = index + 2;
            while (_tokens[j].Kind == StyleTokenKind.Whitespace)
            {
                j++;
            }

            StyleToken nameToken = _tokens[j];
            if (nameToken.Kind == StyleTokenKind.End)
            {
                return Fail(StyleErrorKind.UnexpectedEnd, in nameToken, "'var(' has no ')' before the end of the text. Write a variable name and ')', for example 'var(--accent)'.");
            }

            if (!ThemeVariables.IsVariableName(_source, in nameToken))
            {
                return Fail(
                    StyleErrorKind.InvalidVariable,
                    in nameToken,
                    StyleValues.Describe(_source, in nameToken) + " is not a variable name. Write a name that starts with '--', for example 'var(--accent)'.");
            }

            int nameIndex = j;
            j++;
            while (_tokens[j].Kind == StyleTokenKind.Whitespace)
            {
                j++;
            }

            StyleToken close = _tokens[j];
            if (close.Kind == StyleTokenKind.Comma)
            {
                return Fail(
                    StyleErrorKind.InvalidVariable,
                    in close,
                    StyleValues.Describe(_source, in close) + " starts a fallback value. The format has no fallback value. Remove the fallback, and define the variable in a ':root' block.");
            }

            if (close.Kind == StyleTokenKind.End)
            {
                return Fail(StyleErrorKind.UnexpectedEnd, in close, "'var(' has no ')' before the end of the text. Write ')' after the variable name.");
            }

            if (close.Kind != StyleTokenKind.RightParen)
            {
                return Fail(
                    StyleErrorKind.InvalidVariable,
                    in close,
                    StyleValues.Describe(_source, in close) + " is not correct here. Write ')' after the variable name, for example 'var(--accent)'.");
            }

            string name = Encoding.UTF8.GetString(nameToken.GetText(_source));
            if (!_variables.TryGet(name, out VariableValue variable))
            {
                // A definition of the name has an error. That error names the fault, so the use adds no second error
                // (CR-2). The text has an error, so it gives no sheet.
                if (_variables.IsRejected(name))
                {
                    return false;
                }

                return Fail(
                    StyleErrorKind.UnknownVariable,
                    in nameToken,
                    "The variable '" + name + "' is not defined. Define it in a ':root' block of this text, for example ':root { " + name + ": #3060c0; }'. The name is case-sensitive.");
            }

            for (int k = variable.Start; k < variable.End; k++)
            {
                StyleToken token = _tokens[k];
                AddValueToken(in token, varIndex, nameIndex);
            }

            index = j + 1;
            return true;
        }

        private readonly void AddValueToken(in StyleToken token, int errorIndex, int useNameIndex)
        {
            _value.Add(token);
            _map.Add(errorIndex);
            _useName.Add(useNameIndex);
        }

        /// <summary>
        /// Records an error at an entry of the value list. The error points at the token that the map names. For a
        /// token that comes from a variable, the message names the variable use.
        /// </summary>
        private readonly bool FailValue(StyleErrorKind kind, int valueIndex, string message)
        {
            StyleToken atFault = _tokens[_map[valueIndex]];
            int useName = _useName[valueIndex];
            if (useName >= 0)
            {
                StyleToken nameToken = _tokens[useName];
                message += " The value comes from 'var(" + Encoding.UTF8.GetString(nameToken.GetText(_source)) + ")'.";
            }

            return Fail(kind, in atFault, message);
        }

        /// <summary>
        /// Drops the errors of one declaration after <paramref name="errorMark"/> when a token error lies inside the
        /// declaration: from its first token up to its <c>;</c>, or up to the <c>}</c> or the End token that ends it.
        /// </summary>
        private readonly void DropDeclarationErrorsAtTokenError(int start, int errorMark)
        {
            if (_errors.Count == errorMark || _tokenErrors.Count == 0)
            {
                return;
            }

            StyleToken first = _tokens[start];
            StyleToken stop = _tokens[_index];
            int upperLine = stop.Line;
            int upperColumn = stop.Column;
            if (_index > start && _tokens[_index - 1].Kind == StyleTokenKind.Semicolon)
            {
                StyleToken semicolon = _tokens[_index - 1];
                upperLine = semicolon.Line;
                upperColumn = semicolon.Column + 1;
            }

            DropAtTokenError(first.Line, first.Column, upperLine, upperColumn, errorMark);
        }

        /// <summary>
        /// Drops the selector error after <paramref name="errorMark"/> when a token error lies between the first token
        /// of the rule and the token after the error.
        /// </summary>
        private readonly void DropSelectorErrorAtTokenError(int start, int errorMark)
        {
            if (_errors.Count == errorMark || _tokenErrors.Count == 0)
            {
                return;
            }

            StyleParseError error = _errors[errorMark];
            int upperLine = int.MaxValue;
            int upperColumn = int.MaxValue;
            for (int i = start; i < _tokens.Count; i++)
            {
                StyleToken token = _tokens[i];
                if (ComparePosition(token.Line, token.Column, error.Line, error.Column) > 0)
                {
                    upperLine = token.Line;
                    upperColumn = token.Column;
                    break;
                }
            }

            StyleToken first = _tokens[start];
            DropAtTokenError(first.Line, first.Column, upperLine, upperColumn, errorMark);
        }

        /// <summary>
        /// Drops each error after <paramref name="errorMark"/> when a token error lies in the range from the lower
        /// position (included) to the upper position (not included).
        /// </summary>
        /// <remarks>
        /// The tokenizer adds the token errors in the order of the source, so the list is sorted by the line and the
        /// column. A binary search finds the first token error at or after the lower position. Only that error can be
        /// the first in the range, so one compare with the upper position gives the result. Thus each call takes
        /// a time that grows with the logarithm of the token errors, not with their count (CR-1).
        /// </remarks>
        private readonly void DropAtTokenError(int lowerLine, int lowerColumn, int upperLine, int upperColumn, int errorMark)
        {
            int index = FirstTokenErrorAtOrAfter(_tokenErrors, lowerLine, lowerColumn);
            if (index == _tokenErrors.Count)
            {
                return;
            }

            StyleParseError tokenError = _tokenErrors[index];
            if (ComparePosition(tokenError.Line, tokenError.Column, upperLine, upperColumn) < 0)
            {
                _errors.RemoveRange(errorMark, _errors.Count - errorMark);
            }
        }

        /// <summary>Finds the first error of a sorted list at or after a position, with a binary search.</summary>
        /// <param name="sorted">The errors, sorted by the line and then the column.</param>
        /// <param name="line">The line of the position.</param>
        /// <param name="column">The column of the position.</param>
        /// <returns>The index of the first error at or after the position, or the count of the list when no error is there.</returns>
        private static int FirstTokenErrorAtOrAfter(List<StyleParseError> sorted, int line, int column)
        {
            int low = 0;
            int high = sorted.Count;
            while (low < high)
            {
                int middle = low + ((high - low) >> 1);
                StyleParseError error = sorted[middle];
                if (ComparePosition(error.Line, error.Column, line, column) < 0)
                {
                    low = middle + 1;
                }
                else
                {
                    high = middle;
                }
            }

            return low;
        }

        /// <summary>
        /// Skips the whitespace tokens. A comment is part of the whitespace. The rule reader skips whitespace only
        /// between rules and in a block. <see cref="SelectorReader"/> reads the whitespace of a selector, because there
        /// whitespace can be the descendant combinator.
        /// </summary>
        private void SkipWhitespace()
        {
            while (_tokens[_index].Kind == StyleTokenKind.Whitespace)
            {
                _index++;
            }
        }

        /// <summary>Gives the token at the read position. The reader never moves past the <see cref="StyleTokenKind.End"/> token.</summary>
        private readonly StyleToken Peek()
        {
            return _tokens[_index];
        }

        /// <summary>
        /// Records a token in a wrong place: <see cref="StyleErrorKind.UnexpectedEnd"/> for the
        /// <see cref="StyleTokenKind.End"/> token, else <see cref="StyleErrorKind.UnexpectedToken"/>.
        /// </summary>
        /// <param name="token">The token at fault.</param>
        /// <param name="fix">The sentence that names the fix.</param>
        /// <returns>Always <see langword="false"/>.</returns>
        private readonly bool Unexpected(in StyleToken token, string fix)
        {
            if (token.Kind == StyleTokenKind.End)
            {
                return Fail(StyleErrorKind.UnexpectedEnd, in token, "The rule has no '}' before the end of the text. " + fix);
            }

            return Fail(StyleErrorKind.UnexpectedToken, in token, StyleValues.Describe(_source, in token) + " is not correct here. " + fix);
        }

        /// <summary>Records an error at the first byte of a token.</summary>
        /// <param name="kind">The kind.</param>
        /// <param name="token">The token at fault.</param>
        /// <param name="message">The problem and the fix.</param>
        /// <returns>Always <see langword="false"/>.</returns>
        private readonly bool Fail(StyleErrorKind kind, in StyleToken token, string message)
        {
            _errors.Add(new StyleParseError(kind, token.Line, token.Column, message, _sourceName));
            return false;
        }
    }
}
