using System;
using System.Collections.Generic;
using System.Text;
using BrutalUi.Core;

namespace BrutalUi.Markup;

/// <summary>
/// One compound of a selector as the reader finds it: the token index of each name part and the state bits.
/// </summary>
/// <remarks>
/// The struct holds token indexes, not ids. Thus the read adds no name to a global table.
/// <see cref="SelectorReader.Build(ReadOnlySpan{byte}, List{StyleToken}, in SelectorSyntax)"/> interns the names later.
/// </remarks>
internal readonly struct CompoundSyntax
{
    /// <summary>Creates a compound.</summary>
    /// <param name="typeToken">The index of the <see cref="StyleTokenKind.Ident"/> token of the type, or -1.</param>
    /// <param name="classToken">The index of the <see cref="StyleTokenKind.Ident"/> token after the <c>.</c>, or -1.</param>
    /// <param name="nameToken">The index of the <see cref="StyleTokenKind.Hash"/> token of the name, or -1.</param>
    /// <param name="states">The state bits. Each bit is a member of <see cref="PseudoStates"/>.</param>
    public CompoundSyntax(int typeToken, int classToken, int nameToken, PseudoStates states)
    {
        TypeToken = typeToken;
        ClassToken = classToken;
        NameToken = nameToken;
        States = states;
    }

    /// <summary>Gets the index of the type token, or -1 when the compound has no type part.</summary>
    public int TypeToken { get; }

    /// <summary>Gets the index of the class name token (without the <c>.</c>), or -1 when the compound has no class part.</summary>
    public int ClassToken { get; }

    /// <summary>Gets the index of the <see cref="StyleTokenKind.Hash"/> token, or -1 when the compound has no name part.</summary>
    public int NameToken { get; }

    /// <summary>Gets the state bits. <see cref="PseudoStates.None"/> when the compound has no state part.</summary>
    public PseudoStates States { get; }
}

/// <summary>
/// One selector as the reader finds it: the subject, the ancestor, and the combinator.
/// </summary>
internal readonly struct SelectorSyntax
{
    /// <summary>Creates a selector.</summary>
    /// <param name="subject">The compound that the element must match.</param>
    /// <param name="ancestor">The compound of the parent or the grandparent. Ignored for <see cref="SelectorCombinator.None"/>.</param>
    /// <param name="combinator">The link between the two compounds.</param>
    public SelectorSyntax(in CompoundSyntax subject, in CompoundSyntax ancestor, SelectorCombinator combinator)
    {
        Subject = subject;
        Ancestor = ancestor;
        Combinator = combinator;
    }

    /// <summary>Gets the compound that the element must match.</summary>
    public CompoundSyntax Subject { get; }

    /// <summary>Gets the compound of the parent or the grandparent.</summary>
    public CompoundSyntax Ancestor { get; }

    /// <summary>Gets the link between <see cref="Ancestor"/> and <see cref="Subject"/>.</summary>
    public SelectorCombinator Combinator { get; }
}

/// <summary>
/// Reads a selector list from the token list: <c>selector ( ',' selector )*</c>, up to the <c>{</c> of the rule.
/// </summary>
/// <remarks>
/// <para>
/// The grammar: <c>selector := compound ( combinator compound )?</c>, <c>combinator := Whitespace | '&gt;'</c>, and
/// <c>compound := Ident? ( '.' Ident | Hash | ':' Ident )*</c> with at least one part. A leading identifier is the
/// type, <c>.name</c> is the class, a <c>#name</c> is the name, and <c>:hover</c>, <c>:active</c>, <c>:focus</c>,
/// <c>:disabled</c>, and <c>:checked</c> are the states. The parts of one compound must touch. Whitespace around
/// <c>&gt;</c>, after <c>,</c>, and before <c>{</c> is not a combinator.
/// </para>
/// <para>
/// The reader checks each case of the reject list before a factory runs, so no <see cref="ArgumentException"/>
/// leaves the parse. It stops at the first error and adds one <see cref="StyleParseError"/> at the first byte of the
/// token at fault. The message quotes the token text. The parser then skips the full block of the rule. The read adds no name to a global table: <see cref="Build"/> does that, after the parser knows that
/// the rule is complete and sets a value.
/// </para>
/// </remarks>
internal ref struct SelectorReader
{
    private readonly ReadOnlySpan<byte> _source;
    private readonly List<StyleToken> _tokens;
    private readonly string _sourceName;
    private readonly List<StyleParseError> _errors;
    private int _index;

    /// <summary>Creates a reader at a token index.</summary>
    /// <param name="source">The source span that the tokenizer read.</param>
    /// <param name="tokens">The token list. It ends with a <see cref="StyleTokenKind.End"/> token.</param>
    /// <param name="sourceName">The source name of each error.</param>
    /// <param name="errors">The list that gets the error.</param>
    /// <param name="index">The index of the first token of the list. The token is not whitespace.</param>
    public SelectorReader(ReadOnlySpan<byte> source, List<StyleToken> tokens, string sourceName, List<StyleParseError> errors, int index)
    {
        _source = source;
        _tokens = tokens;
        _sourceName = sourceName;
        _errors = errors;
        _index = index;
    }

    /// <summary>Gets the read position. After a successful read, it is the index of the <c>{</c> token.</summary>
    public readonly int Index => _index;

    /// <summary>Reads a selector list up to the <c>{</c> of the rule. The method does not read the <c>{</c>.</summary>
    /// <param name="selectors">The list that gets each selector in the order of the source.</param>
    /// <returns><see langword="true"/> when the list has no error. Else the error list holds one new error.</returns>
    public bool ReadList(List<SelectorSyntax> selectors)
    {
        while (true)
        {
            if (!ReadSelector(out SelectorSyntax selector))
            {
                return false;
            }

            selectors.Add(selector);

            // ReadSelector stops at '{' or at ','.
            if (Peek().Kind == StyleTokenKind.LeftBrace)
            {
                return true;
            }

            _index++;
            SkipWhitespace();
        }
    }

    /// <summary>
    /// Makes the <see cref="Selector"/> of a selector that <see cref="ReadList"/> gives. The method interns the type
    /// names and the class names, so call it only for a complete rule with a value.
    /// </summary>
    /// <param name="source">The source span that the tokenizer read.</param>
    /// <param name="tokens">The token list of the read.</param>
    /// <param name="syntax">The selector.</param>
    /// <returns>The selector of the factories. The factories do not throw, because the read checked each part.</returns>
    public static Selector Build(ReadOnlySpan<byte> source, List<StyleToken> tokens, in SelectorSyntax syntax)
    {
        if (syntax.Combinator == SelectorCombinator.None)
        {
            return Selector.For(BuildCompound(source, tokens, syntax.Subject));
        }

        // The ancestor comes first in the source, so its names go into the tables first.
        CompoundSelector ancestor = BuildCompound(source, tokens, syntax.Ancestor);
        CompoundSelector subject = BuildCompound(source, tokens, syntax.Subject);
        return syntax.Combinator == SelectorCombinator.Child
            ? Selector.Child(in ancestor, in subject)
            : Selector.Descendant(in ancestor, in subject);
    }

    /// <summary>Gives the state bit of a state name, for example <c>hover</c>. The compare is ordinal.</summary>
    /// <param name="name">The bytes of the name, without the <c>:</c>.</param>
    /// <returns>The bit, or <see cref="PseudoStates.None"/> for an unknown name.</returns>
    internal static PseudoStates GetState(ReadOnlySpan<byte> name)
    {
        if (name.SequenceEqual("hover"u8))
        {
            return PseudoStates.Hover;
        }

        if (name.SequenceEqual("active"u8))
        {
            return PseudoStates.Active;
        }

        if (name.SequenceEqual("focus"u8))
        {
            return PseudoStates.Focus;
        }

        if (name.SequenceEqual("disabled"u8))
        {
            return PseudoStates.Disabled;
        }

        if (name.SequenceEqual("checked"u8))
        {
            return PseudoStates.Checked;
        }

        return PseudoStates.None;
    }

    private static CompoundSelector BuildCompound(ReadOnlySpan<byte> source, List<StyleToken> tokens, in CompoundSyntax syntax)
    {
        ElementTypeId type = syntax.TypeToken < 0
            ? ElementTypeId.None
            : ElementType.Intern(Encoding.UTF8.GetString(tokens[syntax.TypeToken].GetText(source)));
        ClassId id = syntax.ClassToken < 0
            ? ClassId.None
            : ClassName.Intern(Encoding.UTF8.GetString(tokens[syntax.ClassToken].GetText(source)));

        // A Hash token holds one byte or more after the '#', so the name is never empty.
        string? name = syntax.NameToken < 0
            ? null
            : Encoding.UTF8.GetString(tokens[syntax.NameToken].GetText(source)[1..]);

        return new CompoundSelector { Type = type, Class = id, States = syntax.States, Name = name };
    }

    /// <summary>Reads one selector of one or two compounds. The read stops at the <c>{</c> or the <c>,</c> after it.</summary>
    private bool ReadSelector(out SelectorSyntax selector)
    {
        selector = default;
        if (!ReadCompound(out CompoundSyntax first) || !ReadCombinator(out SelectorCombinator combinator))
        {
            return false;
        }

        if (combinator == SelectorCombinator.None)
        {
            selector = new SelectorSyntax(in first, default, SelectorCombinator.None);
            return true;
        }

        if (!ReadCompound(out CompoundSyntax second) || !ReadCombinator(out SelectorCombinator third))
        {
            return false;
        }

        if (third != SelectorCombinator.None)
        {
            // ReadCombinator stops at the first token of the third compound.
            StyleToken start = Peek();
            return Fail(
                StyleErrorKind.UnsupportedSelector,
                in start,
                "The compound that starts at " + Describe(in start) + " is a third compound. A selector holds at most two compounds. Remove it, or give the element a class.");
        }

        selector = new SelectorSyntax(in second, in first, combinator);
        return true;
    }

    /// <summary>
    /// Reads one compound. The first token is not whitespace. The read stops at the first token after the compound.
    /// </summary>
    private bool ReadCompound(out CompoundSyntax compound)
    {
        compound = default;
        int type = -1;
        int id = -1;
        int name = -1;
        PseudoStates states = PseudoStates.None;

        StyleToken first = Peek();
        if (first.Kind == StyleTokenKind.Ident)
        {
            type = _index;
            _index++;
        }
        else if (!CanStartCompound(in first))
        {
            return StartError(in first);
        }

        while (true)
        {
            StyleToken token = Peek();
            if (token.Kind == StyleTokenKind.Dot)
            {
                if (id >= 0)
                {
                    return Fail(
                        StyleErrorKind.UnsupportedSelector,
                        in token,
                        Describe(in token) + " starts a second class. A compound holds at most one class. Remove the second class, or use a chain such as '.a .b'.");
                }

                _index++;

                // The '.' and the class name must touch, so no whitespace skip comes here.
                StyleToken className = Peek();
                if (className.Kind != StyleTokenKind.Ident)
                {
                    return Unexpected(in className, "Write the class name directly after '.', for example '.panel'.");
                }

                id = _index;
                _index++;
            }
            else if (token.Kind == StyleTokenKind.Hash)
            {
                if (name >= 0)
                {
                    return Fail(
                        StyleErrorKind.UnsupportedSelector,
                        in token,
                        "A compound holds at most one name. Remove the second name " + Describe(in token) + ".");
                }

                name = _index;
                _index++;
            }
            else if (token.Kind == StyleTokenKind.Colon)
            {
                if (!ReadState(ref states))
                {
                    return false;
                }
            }
            else
            {
                break;
            }
        }

        compound = new CompoundSyntax(type, id, name, states);
        return true;
    }

    /// <summary>Reads <c>':' Ident</c> and adds the state bit. A repeat of a state adds nothing.</summary>
    private bool ReadState(ref PseudoStates states)
    {
        StyleToken colon = Peek();
        _index++;

        // The ':' and the state must touch, so no whitespace skip comes here.
        StyleToken state = Peek();
        if (state.Kind == StyleTokenKind.Colon)
        {
            return Fail(
                StyleErrorKind.UnsupportedSelector,
                in state,
                "The second " + Describe(in state) + " starts a pseudo-element such as '::before'. The format has no pseudo-element. Remove it, or use a state such as ':hover'.");
        }

        if (state.Kind != StyleTokenKind.Ident)
        {
            return Unexpected(in state, "Write the state directly after ':', for example ':hover'.");
        }

        // The token after an identifier is never past the End token, because an identifier is not the End token.
        if (_tokens[_index + 1].Kind == StyleTokenKind.LeftParen)
        {
            return Fail(
                StyleErrorKind.UnsupportedSelector,
                in state,
                "The format has no function selector such as " + Describe(in state) + "(). Remove it, or give the element a class.");
        }

        ReadOnlySpan<byte> text = state.GetText(_source);
        if (text.SequenceEqual("root"u8))
        {
            return Fail(
                StyleErrorKind.InvalidSelector,
                in colon,
                Describe(in colon) + " starts ':root', and ':root' starts a block of its own. Remove ':root' from this selector, or write ':root {' alone.");
        }

        PseudoStates bit = GetState(text);
        if (bit == PseudoStates.None)
        {
            return Fail(
                StyleErrorKind.UnknownPseudoState,
                in state,
                Describe(in state) + " is not a state of the format. Use 'hover', 'active', 'focus', 'disabled', or 'checked'.");
        }

        states |= bit;
        _index++;
        return true;
    }

    /// <summary>
    /// Reads the part after a compound. <see cref="SelectorCombinator.None"/> when the selector ends: the read stops at
    /// the <c>{</c> or the <c>,</c>. Else the read stops at the first token of the next compound.
    /// </summary>
    private bool ReadCombinator(out SelectorCombinator combinator)
    {
        combinator = SelectorCombinator.None;
        int start = _index;
        SkipWhitespace();
        bool space = _index > start;

        StyleToken token = Peek();
        if (token.Kind is StyleTokenKind.LeftBrace or StyleTokenKind.Comma)
        {
            return true;
        }

        if (token.Kind == StyleTokenKind.Greater)
        {
            _index++;
            SkipWhitespace();
            StyleToken next = Peek();
            if (!CanStartCompound(in next))
            {
                return StartError(in next);
            }

            combinator = SelectorCombinator.Child;
            return true;
        }

        if (IsUnsupportedDelim(in token))
        {
            return Unsupported(in token);
        }

        if (space && CanStartCompound(in token))
        {
            combinator = SelectorCombinator.Descendant;
            return true;
        }

        return Unexpected(in token, "Write '{' after the selector, or ',' before the next selector.");
    }

    /// <summary>Gives a value that shows whether a token can start a compound: a type, a class, a name, or a state.</summary>
    private static bool CanStartCompound(in StyleToken token)
    {
        return token.Kind is StyleTokenKind.Ident or StyleTokenKind.Dot or StyleTokenKind.Hash or StyleTokenKind.Colon;
    }

    /// <summary>Gives a value that shows whether a token is <c>*</c>, <c>[</c>, <c>+</c>, or <c>~</c>.</summary>
    private readonly bool IsUnsupportedDelim(in StyleToken token)
    {
        if (token.Kind != StyleTokenKind.Delim)
        {
            return false;
        }

        byte b = _source[token.Offset];
        return b is (byte)'*' or (byte)'[' or (byte)'+' or (byte)'~';
    }

    /// <summary>Records the error of a token that cannot start a compound, at the place where a compound must start.</summary>
    private readonly bool StartError(in StyleToken token)
    {
        if (token.Kind is StyleTokenKind.LeftBrace or StyleTokenKind.Comma or StyleTokenKind.Greater)
        {
            return Fail(
                StyleErrorKind.InvalidSelector,
                in token,
                "The selector is empty before " + Describe(in token) + ". Write a type, a class, a name, or a state, for example '.panel'.");
        }

        if (IsUnsupportedDelim(in token))
        {
            return Unsupported(in token);
        }

        return Unexpected(in token, "Start the selector with a type, a class, a name, or a state, for example '.panel'.");
    }

    /// <summary>Records the error of <c>*</c>, <c>[</c>, <c>+</c>, or <c>~</c>.</summary>
    private readonly bool Unsupported(in StyleToken token)
    {
        string form = _source[token.Offset] switch
        {
            (byte)'*' => "The format has no universal selector '*'. Use a class, or a state alone such as ':hover'.",
            (byte)'[' => "The format has no attribute selector '['. Use a class or a name.",
            _ => "The format has no sibling combinator " + Describe(in token) + ". Use '>' or whitespace.",
        };

        return Fail(StyleErrorKind.UnsupportedSelector, in token, form);
    }

    /// <summary>
    /// Records a token in a wrong place: <see cref="StyleErrorKind.UnexpectedEnd"/> for the
    /// <see cref="StyleTokenKind.End"/> token, else <see cref="StyleErrorKind.UnexpectedToken"/>.
    /// </summary>
    private readonly bool Unexpected(in StyleToken token, string fix)
    {
        if (token.Kind == StyleTokenKind.End)
        {
            return Fail(StyleErrorKind.UnexpectedEnd, in token, "The selector has no '{' before the end of the text. " + fix);
        }

        return Fail(StyleErrorKind.UnexpectedToken, in token, Describe(in token) + " is not correct here. " + fix);
    }

    /// <summary>Records an error at the first byte of a token.</summary>
    /// <returns>Always <see langword="false"/>.</returns>
    private readonly bool Fail(StyleErrorKind kind, in StyleToken token, string message)
    {
        _errors.Add(new StyleParseError(kind, token.Line, token.Column, message, _sourceName));
        return false;
    }

    private readonly string Describe(in StyleToken token)
    {
        return StyleValues.Describe(_source, in token);
    }

    private void SkipWhitespace()
    {
        while (_tokens[_index].Kind == StyleTokenKind.Whitespace)
        {
            _index++;
        }
    }

    private readonly StyleToken Peek()
    {
        return _tokens[_index];
    }
}
