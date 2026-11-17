using System;
using System.Text;
using Brutal.Numerics;
using BrutalUi.Core;

namespace BrutalUi.Markup;

/// <summary>
/// The attribute that a setter or a hook handler reads now: the name, the value, the position, and the way to report
/// a fault of this attribute.
/// </summary>
/// <remarks>
/// <para>
/// The type is a <c>readonly ref struct</c> on purpose. It holds a reference to the reader of one load and an index
/// into its attribute list, so it is valid during one call of a setter or a handler only. The compiler rejects the
/// store of a context in a field, an array, or a closure: a handler cannot keep a context after the load, and cannot
/// use one from a later frame.
/// </para>
/// <para>
/// <c>default(AttributeContext)</c> has no reader. Do not use it. The loader makes each context and passes it to one
/// call.
/// </para>
/// </remarks>
public readonly ref struct AttributeContext
{
    private readonly TemplateReader? _reader;
    private readonly int _index;

    /// <summary>Creates the context of one attribute. The loader calls this.</summary>
    /// <param name="reader">The reader of the load.</param>
    /// <param name="index">The index of the attribute in the list of the element.</param>
    internal AttributeContext(TemplateReader reader, int index)
    {
        _reader = reader;
        _index = index;
    }

    /// <summary>Gets the attribute name, as the source writes it.</summary>
    public string Name => Reader.Attributes[_index].Name;

    /// <summary>Gets the attribute value, as <c>XmlReader</c> gives it. XML normalizes a tab or a line break inside the value to a space.</summary>
    public string Value => Reader.Attributes[_index].Value;

    /// <summary>Gets the line of the attribute name, from 1.</summary>
    public int Line => Reader.Attributes[_index].Line;

    /// <summary>Gets the column of the attribute name, from 1, in UTF-16 units.</summary>
    public int Column => Reader.Attributes[_index].Column;

    /// <summary>Gets the name of the source, for example <c>hud.xml</c>.</summary>
    public string SourceName => Reader.SourceName;

    /// <summary>Records one error at the position of this attribute.</summary>
    /// <param name="kind">The kind of the error.</param>
    /// <param name="message">The problem and the fix.</param>
    /// <remarks>
    /// The loader keeps reading after the error. One error or more gives no root in
    /// <see cref="TemplateLoadResult"/>.
    /// </remarks>
    public void AddError(TemplateErrorKind kind, string message)
    {
        Reader.AddError(kind, message, Line, Column);
    }

    /// <summary>Reads the value as a bool.</summary>
    /// <remarks>
    /// A bool is <c>true</c> or <c>false</c>, case-sensitive. The value ignores XML whitespace at both ends. A fault
    /// adds <see cref="TemplateErrorKind.InvalidValue"/> and gives <see langword="false"/>.
    /// </remarks>
    /// <param name="value">The value on a success, or <see langword="false"/> on a fault.</param>
    /// <returns><see langword="true"/> when the value is a bool.</returns>
    public bool TryReadBool(out bool value)
    {
        if (TemplateValues.TryParseBool(TemplateValues.Trim(Value), out value))
        {
            return true;
        }

        value = false;
        AddError(TemplateErrorKind.InvalidValue, $"'{Value}' is not a boolean. Write 'true' or 'false'.");
        return false;
    }

    /// <summary>Reads the value as a whole number.</summary>
    /// <remarks>
    /// A whole number holds ASCII digits only, from 1 to <see cref="int.MaxValue"/>. The value ignores XML whitespace
    /// at both ends. A fault adds <see cref="TemplateErrorKind.InvalidValue"/> and gives 0.
    /// </remarks>
    /// <param name="value">The value on a success, or 0 on a fault.</param>
    /// <returns><see langword="true"/> when the value is a whole number.</returns>
    public bool TryReadWholeNumber(out int value)
    {
        if (TemplateValues.TryParseWholeNumber(TemplateValues.Trim(Value), out value))
        {
            return true;
        }

        value = 0;
        AddError(TemplateErrorKind.InvalidValue, $"'{Value}' is not a whole number. Write digits from 1, for example '8'.");
        return false;
    }

    /// <summary>Reads the value as an integer.</summary>
    /// <remarks>
    /// An integer holds an optional <c>+</c> or <c>-</c> sign, then one or more ASCII digits, and it stays in the
    /// <see cref="int"/> range. It differs from <see cref="TryReadWholeNumber"/> in that it takes 0 and a negative
    /// value. The value ignores XML whitespace at both ends. A fault adds
    /// <see cref="TemplateErrorKind.InvalidValue"/> and gives 0. The read allocates nothing on a success.
    /// </remarks>
    /// <param name="value">The value on a success, or 0 on a fault.</param>
    /// <returns><see langword="true"/> when the value is an integer.</returns>
    public bool TryReadInteger(out int value)
    {
        if (TemplateValues.TryParseInteger(TemplateValues.Trim(Value), out value))
        {
            return true;
        }

        value = 0;
        AddError(
            TemplateErrorKind.InvalidValue,
            $"'{Value}' is not an integer. Write digits with an optional sign, for example '-3'.");
        return false;
    }

    /// <summary>Reads the value as one keyword from a list, and gives the index of the keyword.</summary>
    /// <remarks>
    /// <para>
    /// The read trims XML whitespace from both ends of the value and compares it with each keyword, ordinal, in the
    /// order of the list. It stops at the first match, so a repeated keyword in the list gives its first index. The
    /// read allocates nothing on a success.
    /// </para>
    /// <para>
    /// A fault adds <see cref="TemplateErrorKind.InvalidValue"/> with the keywords in the message, and gives -1. The
    /// generated code for an enum property passes the kebab-case name of each member, in the order of the
    /// declarations, and writes the value from the index in a <c>switch</c>.
    /// </para>
    /// </remarks>
    /// <param name="keywords">The accepted keywords, in order. An empty list matches nothing.</param>
    /// <param name="index">The index of the keyword in <paramref name="keywords"/> on a success, or -1 on a fault.</param>
    /// <returns><see langword="true"/> when the value is one of the keywords.</returns>
    public bool TryReadKeyword(ReadOnlySpan<string> keywords, out int index)
    {
        ReadOnlySpan<char> text = TemplateValues.Trim(Value);
        for (int i = 0; i < keywords.Length; i++)
        {
            if (text.Equals(keywords[i], StringComparison.Ordinal))
            {
                index = i;
                return true;
            }
        }

        index = -1;
        AddError(
            TemplateErrorKind.InvalidValue,
            $"'{Value}' is not a value of {Name}. Write {JoinKeywords(keywords)}.");
        return false;
    }

    /// <summary>Joins the keywords of a fault message with quotes: <c>'a', 'b', or 'c'</c>.</summary>
    /// <param name="keywords">The accepted keywords, in order.</param>
    /// <returns>The joined list. An empty list gives an empty text.</returns>
    private static string JoinKeywords(ReadOnlySpan<string> keywords)
    {
        StringBuilder text = new(keywords.Length * 8);
        for (int i = 0; i < keywords.Length; i++)
        {
            if (i > 0)
            {
                text.Append(i == keywords.Length - 1 ? ", or " : ", ");
            }

            text.Append('\'').Append(keywords[i]).Append('\'');
        }

        return text.ToString();
    }

    /// <summary>Reads the value as a number.</summary>
    /// <remarks>
    /// A number reads in the invariant culture: a sign, digits, and an optional point with digits. It has no
    /// exponent and no group separator, and it must be finite. The value ignores XML whitespace at both ends. A fault
    /// adds <see cref="TemplateErrorKind.InvalidValue"/> and gives 0.
    /// </remarks>
    /// <param name="value">The value on a success, or 0 on a fault.</param>
    /// <returns><see langword="true"/> when the value is a finite number.</returns>
    public bool TryReadNumber(out float value)
    {
        if (TemplateValues.TryParseNumber(TemplateValues.Trim(Value), out value))
        {
            return true;
        }

        value = 0f;
        AddError(TemplateErrorKind.InvalidValue, $"'{Value}' is not a number. Write a number such as '0.5'.");
        return false;
    }

    /// <summary>Reads the value as two numbers with XML whitespace between them.</summary>
    /// <param name="value">The numbers on a success, or (0, 0) on a fault.</param>
    /// <returns><see langword="true"/> when the value holds exactly two finite numbers.</returns>
    public bool TryReadNumbers(out float2 value)
    {
        value = default;
        Span<float> numbers = stackalloc float[2];
        if (TemplateValues.TryParseNumbers(TemplateValues.Trim(Value), numbers))
        {
            value = new float2(numbers[0], numbers[1]);
            return true;
        }

        AddError(
            TemplateErrorKind.InvalidValue,
            $"'{Value}' is not two numbers. Write two numbers with a space between them, for example '0 30'.");
        return false;
    }

    /// <summary>Reads the value as four numbers with XML whitespace between them.</summary>
    /// <param name="value">The numbers on a success, or (0, 0, 0, 0) on a fault.</param>
    /// <returns><see langword="true"/> when the value holds exactly four finite numbers.</returns>
    public bool TryReadNumbers(out float4 value)
    {
        value = default;
        Span<float> numbers = stackalloc float[4];
        if (TemplateValues.TryParseNumbers(TemplateValues.Trim(Value), numbers))
        {
            value = new float4(numbers[0], numbers[1], numbers[2], numbers[3]);
            return true;
        }

        AddError(
            TemplateErrorKind.InvalidValue,
            $"'{Value}' is not four numbers. Write four numbers with a space between them, for example '0 0 1 1'.");
        return false;
    }

    /// <summary>Reads the value as a color.</summary>
    /// <remarks>
    /// A color is <c>#</c> and 3, 6, or 8 hex digits in each case, as in the stylesheet. The value ignores XML
    /// whitespace at both ends. A fault adds <see cref="TemplateErrorKind.InvalidValue"/> and gives
    /// <see cref="Rgba8.Transparent"/>.
    /// </remarks>
    /// <param name="value">The color on a success, or <see cref="Rgba8.Transparent"/> on a fault.</param>
    /// <returns><see langword="true"/> when the value is a color.</returns>
    public bool TryReadColor(out Rgba8 value)
    {
        if (TemplateValues.TryParseColor(TemplateValues.Trim(Value), out value))
        {
            return true;
        }

        value = Rgba8.Transparent;
        AddError(
            TemplateErrorKind.InvalidValue,
            $"'{Value}' is not a color. Write '#' and 3, 6, or 8 hex digits, for example '#1e222c'.");
        return false;
    }

    /// <summary>Reads the value as a texture name from the map of <see cref="TemplateLoader.CreateInfo.Textures"/>.</summary>
    /// <remarks>
    /// The compare is ordinal. The value ignores XML whitespace at both ends. A name that the map does not hold adds
    /// <see cref="TemplateErrorKind.UnknownTexture"/> and gives the none handle.
    /// </remarks>
    /// <param name="value">The handle on a success, or the none handle on a fault.</param>
    /// <returns><see langword="true"/> when the map holds the name.</returns>
    public bool TryReadTexture(out TextureHandle value)
    {
        string name = TemplateValues.Trim(Value).ToString();
        if (Reader.TryGetTexture(name, out value))
        {
            return true;
        }

        value = default;
        AddError(
            TemplateErrorKind.UnknownTexture,
            $"'{name}' is not a texture name. Add it to TemplateLoader.CreateInfo.Textures.");
        return false;
    }

    private TemplateReader Reader => _reader ?? throw new InvalidOperationException(
        "An AttributeContext with no reader is not valid. Use the context that the loader passes to the call.");
}

/// <summary>
/// The element that a factory makes now: the element name, the position, the source name, the read of one create
/// attribute, and the way to report a fault of this element.
/// </summary>
/// <remarks>
/// <para>
/// The type is a <c>readonly ref struct</c> on purpose. It holds a reference to the reader of one load, so it is
/// valid during one factory call only. The compiler rejects the store of the args in a field, an array, or a
/// closure: a factory cannot keep them after the load.
/// </para>
/// <para>
/// <c>default(ElementCreateArgs)</c> has no reader. Do not use it. The loader makes each args value and passes it to
/// one factory call.
/// </para>
/// </remarks>
public readonly ref struct ElementCreateArgs
{
    private readonly TemplateReader? _reader;

    /// <summary>Creates the args of one element. The loader calls this.</summary>
    /// <param name="reader">The reader of the load.</param>
    internal ElementCreateArgs(TemplateReader reader)
    {
        _reader = reader;
    }

    /// <summary>Gets the XML local name of the element, for example <c>Slider</c>.</summary>
    public string ElementName => Reader.ElementName;

    /// <summary>Gets the line of the element name, from 1.</summary>
    public int Line => Reader.ElementLine;

    /// <summary>Gets the column of the element name, from 1, in UTF-16 units.</summary>
    public int Column => Reader.ElementColumn;

    /// <summary>Gets the name of the source, for example <c>hud.xml</c>.</summary>
    public string SourceName => Reader.SourceName;

    /// <summary>
    /// Gives the context of one create attribute of this element. Read the context inside this factory call only.
    /// </summary>
    /// <param name="name">The name of a create row of the type.</param>
    /// <param name="context">The context of the attribute, when the method gives <see langword="true"/>.</param>
    /// <returns>
    /// <see langword="true"/> when the element holds the attribute. <see langword="false"/> when the element has no
    /// such attribute, so the factory uses its default. A required row with no attribute never reaches the factory:
    /// the loader records <see cref="TemplateErrorKind.MissingAttribute"/> first.
    /// </returns>
    /// <remarks>
    /// The name must be a create row of the type. The method throws <see cref="ArgumentException"/> for any other
    /// name, so a factory cannot read a set attribute here.
    /// </remarks>
    /// <exception cref="ArgumentException"><paramref name="name"/> is not a create row of the type.</exception>
    public bool TryGet(string name, out AttributeContext context)
    {
        if (Reader.TryGetCreateAttribute(name, out int index))
        {
            context = new AttributeContext(Reader, index);
            return true;
        }

        RegisteredType? type = Reader.CurrentType;
        int rowIndex = type is null ? -1 : type.FindRowIndex(name);
        if (rowIndex < 0 || type!.Rows[rowIndex].Kind == AttributeRowKind.Set)
        {
            throw new ArgumentException(
                $"'{name}' is not a create attribute of '{Reader.ElementName}'. TryGet reads a create row of the type only.",
                nameof(name));
        }

        context = default;
        return false;
    }

    /// <summary>Records one error at the position of this element.</summary>
    /// <param name="kind">The kind of the error.</param>
    /// <param name="message">The problem and the fix.</param>
    /// <remarks>
    /// The loader keeps reading after the error. One error or more gives no root in
    /// <see cref="TemplateLoadResult"/>.
    /// </remarks>
    public void AddError(TemplateErrorKind kind, string message)
    {
        Reader.AddError(kind, message, Line, Column);
    }

    private TemplateReader Reader => _reader ?? throw new InvalidOperationException(
        "An ElementCreateArgs with no reader is not valid. Use the args that the loader passes to the factory.");
}
