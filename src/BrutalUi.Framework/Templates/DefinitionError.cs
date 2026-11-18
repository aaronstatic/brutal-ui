using System;
using System.Globalization;

namespace BrutalUi.Framework;

/// <summary>
/// The kind of a <see cref="DefinitionError"/>. Each kind names one cause.
/// </summary>
public enum DefinitionErrorKind : byte
{
    /// <summary>
    /// XML that <c>XmlReader</c> rejects: no end tag, a wrong end tag, a control character, or a DTD. A text with no
    /// root element gives this kind too. The read pass stops and gives one error of this kind.
    /// </summary>
    NotWellFormed,

    /// <summary>An element name that the definition format does not hold at this place, or a name outside the
    /// <c>brutal-ui</c> namespace.</summary>
    UnknownElement,

    /// <summary>An attribute that the element does not hold. A prefixed attribute gives this kind, also with the
    /// <c>ui</c> prefix.</summary>
    UnknownAttribute,

    /// <summary>An element with no attribute for one of its required attributes: <c>name</c>, <c>slot</c>,
    /// <c>template</c>, or <c>layout</c>.</summary>
    MissingAttribute,

    /// <summary>An attribute value that is empty where the format needs a name.</summary>
    InvalidValue,

    /// <summary>Text or a CDATA section between definition elements. Whitespace there gives no error.</summary>
    UnexpectedText,

    /// <summary>A <c>template</c> value that names no template registered with
    /// <see cref="DefinitionLoader.AddTemplate"/>.</summary>
    UnknownTemplate,

    /// <summary>An <c>actions</c> value that names no map registered with
    /// <see cref="DefinitionLoader.AddActionMap"/>.</summary>
    UnknownActionMap,

    /// <summary>A <c>layout</c> value that names no <c>Layout</c> entry of the same text.</summary>
    UnknownLayout,

    /// <summary>A <c>PanelRef</c> value that names no <c>Panel</c> entry of the same text.</summary>
    UnknownPanel,

    /// <summary>Two entries of one text share one name, a layout template holds two slots of one name, or one mode
    /// places one panel two times.</summary>
    DuplicateName,

    /// <summary>A slot name of a placement that the mode layout does not hold.</summary>
    UnknownSlot,

    /// <summary>The template load of a layout failed. The message holds one line for each
    /// <see cref="BrutalUi.Markup.TemplateError"/>.</summary>
    TemplateFault,
}

/// <summary>
/// One error of a definition load: the kind, the position in the source text, and a message.
/// </summary>
/// <remarks>
/// <para>
/// The line and the column start at 1. <c>XmlReader</c> gives both through <see cref="System.Xml.IXmlLineInfo"/>.
/// The column counts UTF-16 units of the decoded text, as <see cref="BrutalUi.Markup.TemplateError.Column"/> does.
/// A tab counts 1. A character outside the Basic Multilingual Plane counts 2.
/// </para>
/// <para>
/// <strong>The position rule.</strong> An error at an element points at the first character of the element name,
/// after the <c>&lt;</c>. An error at a value points at the first character of the attribute name. A value has no
/// position of its own. <see cref="DefinitionErrorKind.MissingAttribute"/>,
/// <see cref="DefinitionErrorKind.UnknownElement"/>, <see cref="DefinitionErrorKind.TemplateFault"/>, and a duplicate
/// slot of a template sit at the element. Each other kind sits at its attribute.
/// </para>
/// <para>
/// The loader makes each error. <c>default(DefinitionError)</c> has the line 0, the column 0, an empty message, and
/// an empty source name.
/// </para>
/// </remarks>
public readonly struct DefinitionError
{
    private readonly string? _message;
    private readonly string? _sourceName;

    /// <summary>Creates an error.</summary>
    /// <param name="kind">The kind of the error.</param>
    /// <param name="line">The line of the error, from 1.</param>
    /// <param name="column">The column of the error, from 1, in UTF-16 units.</param>
    /// <param name="message">The problem and the fix.</param>
    /// <param name="sourceName">The name of the source, for example a file name.</param>
    /// <exception cref="ArgumentNullException"><paramref name="message"/> or <paramref name="sourceName"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="line"/> or <paramref name="column"/> is less than 1.</exception>
    internal DefinitionError(DefinitionErrorKind kind, int line, int column, string message, string sourceName)
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
    public DefinitionErrorKind Kind { get; }

    /// <summary>Gets the line of the error, from 1.</summary>
    public int Line { get; }

    /// <summary>
    /// Gets the column of the error, from 1. The column counts UTF-16 units of the decoded text, as
    /// <see cref="System.Xml.IXmlLineInfo.LinePosition"/> gives it. A tab counts 1. A character outside the Basic
    /// Multilingual Plane counts 2.
    /// </summary>
    public int Column { get; }

    /// <summary>
    /// Gets the message. It quotes the source text at fault in single quotes, names the problem, and names the fix,
    /// in one or two short sentences.
    /// </summary>
    /// <remarks>
    /// An example is <c>'hud.panel' is not a registered template. Call AddTemplate with this name before the
    /// load.</c> The text of a message can change in a later version. Read <see cref="Kind"/>,
    /// <see cref="Line"/>, and <see cref="Column"/> in code.
    /// </remarks>
    public string Message => _message ?? string.Empty;

    /// <summary>Gets the name of the source, for example <c>hud-definitions.xml</c>.</summary>
    public string SourceName => _sourceName ?? string.Empty;

    /// <summary>
    /// Gives the error as text in the form <c>&lt;source&gt;(&lt;line&gt;,&lt;column&gt;): &lt;kind&gt;: &lt;message&gt;</c>.
    /// </summary>
    /// <remarks>An example is <c>definitions(4,21): UnknownTemplate: &lt;message&gt;</c>. The method allocates.</remarks>
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

    /// <summary>Gives the name of a kind through an explicit switch, with no reflection.</summary>
    /// <param name="kind">The kind.</param>
    /// <returns>The name of the member, or the number for a value outside the enum.</returns>
    internal static string GetKindName(DefinitionErrorKind kind)
    {
        return kind switch
        {
            DefinitionErrorKind.NotWellFormed => "NotWellFormed",
            DefinitionErrorKind.UnknownElement => "UnknownElement",
            DefinitionErrorKind.UnknownAttribute => "UnknownAttribute",
            DefinitionErrorKind.MissingAttribute => "MissingAttribute",
            DefinitionErrorKind.InvalidValue => "InvalidValue",
            DefinitionErrorKind.UnexpectedText => "UnexpectedText",
            DefinitionErrorKind.UnknownTemplate => "UnknownTemplate",
            DefinitionErrorKind.UnknownActionMap => "UnknownActionMap",
            DefinitionErrorKind.UnknownLayout => "UnknownLayout",
            DefinitionErrorKind.UnknownPanel => "UnknownPanel",
            DefinitionErrorKind.DuplicateName => "DuplicateName",
            DefinitionErrorKind.UnknownSlot => "UnknownSlot",
            DefinitionErrorKind.TemplateFault => "TemplateFault",
            _ => ((byte)kind).ToString(CultureInfo.InvariantCulture),
        };
    }
}
