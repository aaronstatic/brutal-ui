using System;
using System.Globalization;

namespace BrutalUi.Markup;

/// <summary>
/// The kind of a <see cref="TemplateError"/>. Each kind names one cause.
/// </summary>
public enum TemplateErrorKind : byte
{
    /// <summary>
    /// XML that <c>XmlReader</c> rejects: no end tag, a wrong end tag, a control character, or a DTD. An empty template
    /// gives this kind too. The loader stops the read and gives one error of this kind.
    /// </summary>
    NotWellFormed,

    /// <summary>An element name that no registered type holds, or an element outside the <c>brutal-ui</c> namespace.</summary>
    UnknownElement,

    /// <summary>
    /// An attribute with no row of the table and no hook handler that takes it. A prefixed attribute gives this kind,
    /// also with the <c>brutal-ui</c> prefix.
    /// </summary>
    UnknownAttribute,

    /// <summary>An attribute value with a wrong form, a wrong count, or a value out of range.</summary>
    InvalidValue,

    /// <summary>An element with no attribute for one of its required create rows.</summary>
    MissingAttribute,

    /// <summary>A <c>texture</c> name that the texture map of the loader does not hold.</summary>
    UnknownTexture,

    /// <summary>Text or a CDATA section between elements. Whitespace there gives no error.</summary>
    UnexpectedText,

    /// <summary>A child element inside a type that the table marks as a leaf, for example <c>Label</c> or <c>Image</c>.</summary>
    UnexpectedChild,

    /// <summary>A key of a binding attribute that the attached bindings do not register.</summary>
    UnknownBinding,

    /// <summary>A key whose selector does not fit the binding attribute or the element type.</summary>
    BindingTypeMismatch,
}

/// <summary>
/// One error of a template load: the kind, the position in the source, and a message.
/// </summary>
/// <remarks>
/// <para>
/// The line and the column start at 1. <c>XmlReader</c> gives both through <see cref="System.Xml.IXmlLineInfo"/>.
/// The column counts UTF-16 units of the decoded text, so a tab counts 1, and a character outside the Basic
/// Multilingual Plane counts 2. This differs from <see cref="StyleParseError.Column"/>, which counts code points.
/// </para>
/// <para>
/// An error at an element points at the first character of its name, after the <c>&lt;</c>. An error at an attribute
/// points at the first character of the attribute name. A value has no position of its own, so a value error points
/// at the name of its attribute.
/// </para>
/// <para>
/// The loader makes each error. <c>default(TemplateError)</c> has the line 0, the column 0, an empty message, and an
/// empty source name.
/// </para>
/// </remarks>
public readonly struct TemplateError
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
    internal TemplateError(TemplateErrorKind kind, int line, int column, string message, string sourceName)
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
    public TemplateErrorKind Kind { get; }

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
    /// An example is <c>'colour' is not an attribute of Label. The attributes of Label are name, class, pickable,
    /// focusable, text, text-key.</c> The text of a message can change in a later version. Read <see cref="Kind"/>,
    /// <see cref="Line"/>, and <see cref="Column"/> in code.
    /// </remarks>
    public string Message => _message ?? string.Empty;

    /// <summary>Gets the name of the source, for example <c>hud.xml</c>.</summary>
    public string SourceName => _sourceName ?? string.Empty;

    /// <summary>
    /// Gives the error as text in the form <c>&lt;source&gt;(&lt;line&gt;,&lt;column&gt;): &lt;kind&gt;: &lt;message&gt;</c>.
    /// </summary>
    /// <remarks>An example is <c>hud.xml(3,59): UnknownAttribute: &lt;message&gt;</c>. The method allocates.</remarks>
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
    internal static string GetKindName(TemplateErrorKind kind)
    {
        return kind switch
        {
            TemplateErrorKind.NotWellFormed => "NotWellFormed",
            TemplateErrorKind.UnknownElement => "UnknownElement",
            TemplateErrorKind.UnknownAttribute => "UnknownAttribute",
            TemplateErrorKind.InvalidValue => "InvalidValue",
            TemplateErrorKind.MissingAttribute => "MissingAttribute",
            TemplateErrorKind.UnknownTexture => "UnknownTexture",
            TemplateErrorKind.UnexpectedText => "UnexpectedText",
            TemplateErrorKind.UnexpectedChild => "UnexpectedChild",
            TemplateErrorKind.UnknownBinding => "UnknownBinding",
            TemplateErrorKind.BindingTypeMismatch => "BindingTypeMismatch",
            _ => ((byte)kind).ToString(CultureInfo.InvariantCulture),
        };
    }
}
