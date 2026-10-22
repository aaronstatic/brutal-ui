using System;
using System.Globalization;

namespace BrutalUi.Markup;

/// <summary>
/// The kind of a <see cref="StyleParseError"/>. Each kind names one cause.
/// </summary>
public enum StyleErrorKind : byte
{
    /// <summary>A byte that starts no token, a control byte, or a byte of 0x80 or more outside a comment.</summary>
    UnexpectedCharacter,

    /// <summary>A comment that starts with <c>/*</c> and has no <c>*/</c>.</summary>
    UnterminatedComment,

    /// <summary>A token in a place where the grammar wants another token.</summary>
    UnexpectedToken,

    /// <summary>
    /// The text ends inside a rule, a <c>:root</c> block, an at-rule, or a <c>var(</c>. For example, the <c>}</c> of the
    /// rule is missing. The error is at the position after the last byte.
    /// </summary>
    UnexpectedEnd,

    /// <summary>A selector with an empty compound, or <c>:root</c> in a chain or in a list.</summary>
    InvalidSelector,

    /// <summary>
    /// A CSS selector form that the format does not support: <c>.a.b</c>, <c>*</c>, <c>[x]</c>, <c>+</c>, <c>~</c>,
    /// <c>::before</c>, <c>:not</c>, or a third compound.
    /// </summary>
    UnsupportedSelector,

    /// <summary>A pseudo-state other than <c>:hover</c>, <c>:active</c>, <c>:focus</c>, <c>:disabled</c>, and <c>:checked</c>.</summary>
    UnknownPseudoState,

    /// <summary>A property name that the format does not know.</summary>
    UnknownProperty,

    /// <summary>
    /// A value with a wrong form, a wrong count, a unit that the format does not know (<c>12em</c>), or a number out
    /// of range (a negative <c>padding</c>, an <c>opacity</c> above 1, or a number that a float cannot hold).
    /// </summary>
    InvalidValue,

    /// <summary>A font name that the font map of the parser does not hold.</summary>
    UnknownFont,

    /// <summary>A <c>var(--name)</c> with no definition in a <c>:root</c> block of the same sheet.</summary>
    UnknownVariable,

    /// <summary>A wrong variable declaration or a wrong <c>var()</c> use.</summary>
    InvalidVariable,

    /// <summary>
    /// A CSS form outside the format: an at-rule such as <c>@import</c> or <c>@media</c> (at the <c>@</c>),
    /// <c>!important</c> (at the <c>!</c>), or a function other than <c>var</c> such as <c>calc(</c> or <c>rgb(</c> (at
    /// its name).
    /// </summary>
    UnsupportedSyntax,
}

/// <summary>
/// One error of a stylesheet parse: the kind, the position in the source, and a message.
/// </summary>
/// <remarks>
/// <para>
/// The line and the column start at 1. The column counts code points, not bytes. A tab counts 1.
/// </para>
/// <para>
/// The parser makes each error. <c>default(StyleParseError)</c> has the line 0, the column 0,
/// an empty message, and an empty source name.
/// </para>
/// </remarks>
public readonly struct StyleParseError
{
    private readonly string? _message;
    private readonly string? _sourceName;

    /// <summary>Creates an error.</summary>
    /// <param name="kind">The kind of the error.</param>
    /// <param name="line">The line of the error, from 1.</param>
    /// <param name="column">The column of the error, from 1, in code points.</param>
    /// <param name="message">The problem and the fix.</param>
    /// <param name="sourceName">The name of the source, for example a file name.</param>
    /// <exception cref="ArgumentNullException"><paramref name="message"/> or <paramref name="sourceName"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="line"/> or <paramref name="column"/> is less than 1.</exception>
    internal StyleParseError(StyleErrorKind kind, int line, int column, string message, string sourceName)
    {
        ArgumentNullException.ThrowIfNull(message);
        ArgumentNullException.ThrowIfNull(sourceName);
        ArgumentOutOfRangeException.ThrowIfLessThan(line, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(column, 1);

        Kind = kind;
        Line = line;
        Column = column;
        _message = message;
        _sourceName = sourceName;
    }

    /// <summary>Gets the kind of the error.</summary>
    public StyleErrorKind Kind { get; }

    /// <summary>Gets the line of the error, from 1. Each LF, each CRLF, and each lone CR starts a new line.</summary>
    public int Line { get; }

    /// <summary>
    /// Gets the column of the error, from 1. The column counts code points. A tab counts 1. Outside a comment, each
    /// byte sequence that is not valid UTF-8 (for example a stray continuation byte) counts 1, as the replacement
    /// character of an editor does.
    /// </summary>
    public int Column { get; }

    /// <summary>
    /// Gets the message. It quotes the source text at fault in single quotes, names the problem, and names the fix,
    /// in one or two short sentences. For the end of the text, the words <c>the end of the text</c> are the quote.
    /// </summary>
    /// <remarks>
    /// An example is <c>'colour' is not a property of the format. Check the spelling, for example 'color'.</c> The
    /// text of a message can change in a later version. Read <see cref="Kind"/>, <see cref="Line"/>, and
    /// <see cref="Column"/> in code.
    /// </remarks>
    public string Message => _message ?? string.Empty;

    /// <summary>Gets the name of the source, for example <c>HelloScreen.css</c>.</summary>
    public string SourceName => _sourceName ?? string.Empty;

    /// <summary>
    /// Gives the error as text in the form <c>&lt;source&gt;(&lt;line&gt;,&lt;column&gt;): &lt;kind&gt;: &lt;message&gt;</c>.
    /// </summary>
    /// <remarks>An example is <c>HelloScreen.css(3,10): InvalidValue: &lt;message&gt;</c>. The method allocates.</remarks>
    /// <returns>The text.</returns>
    public override string ToString()
    {
        return SourceName
            + "("
            + Line.ToString(CultureInfo.InvariantCulture)
            + ","
            + Column.ToString(CultureInfo.InvariantCulture)
            + "): "
            + GetKindName(Kind)
            + ": "
            + Message;
    }

    /// <summary>Gives the name of a kind through an explicit table, with no reflection.</summary>
    /// <param name="kind">The kind.</param>
    /// <returns>The name of the member, or the number for a value outside the enum.</returns>
    internal static string GetKindName(StyleErrorKind kind)
    {
        return kind switch
        {
            StyleErrorKind.UnexpectedCharacter => "UnexpectedCharacter",
            StyleErrorKind.UnterminatedComment => "UnterminatedComment",
            StyleErrorKind.UnexpectedToken => "UnexpectedToken",
            StyleErrorKind.UnexpectedEnd => "UnexpectedEnd",
            StyleErrorKind.InvalidSelector => "InvalidSelector",
            StyleErrorKind.UnsupportedSelector => "UnsupportedSelector",
            StyleErrorKind.UnknownPseudoState => "UnknownPseudoState",
            StyleErrorKind.UnknownProperty => "UnknownProperty",
            StyleErrorKind.InvalidValue => "InvalidValue",
            StyleErrorKind.UnknownFont => "UnknownFont",
            StyleErrorKind.UnknownVariable => "UnknownVariable",
            StyleErrorKind.InvalidVariable => "InvalidVariable",
            StyleErrorKind.UnsupportedSyntax => "UnsupportedSyntax",
            _ => ((byte)kind).ToString(CultureInfo.InvariantCulture),
        };
    }
}
