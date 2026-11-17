using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using BrutalUi.Core;

namespace BrutalUi.Markup;

/// <summary>
/// Loads an XML element template. The result holds the root element, or one error or more.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Load time.</strong> The loader allocates: the UTF-8 copy of a <see cref="string"/> text, the reader, the
/// attribute list of each element, the elements, and the messages. Call <see cref="Load(ReadOnlySpan{byte}, string)"/>
/// before the frame loop, not in it. A loaded tree allocates nothing in <c>Update</c> and <c>Draw</c>.
/// </para>
/// <para>
/// <strong>The threads.</strong> One load holds no state that a later load reads. Each call makes its own reader, its
/// own frame stack, and its own error list. One loader can load more than one text, also on more than one thread,
/// when no <see cref="Register"/> call and no <see cref="AddAttributeHandler"/> call runs at the same time.
/// </para>
/// <para>
/// <strong>The namespace.</strong> Each element of a template is in the <see cref="Namespace"/> namespace. The loader
/// reads an attribute with no prefix as an attribute of its element. A prefixed attribute is an error, so each
/// template uses one form.
/// </para>
/// <para>
/// The loader maps each element name to a registered type, and each attribute to a row of the table or to the
/// attribute hook. It uses no reflection.
/// </para>
/// </remarks>
public sealed class TemplateLoader
{
    /// <summary>
    /// The XML namespace of each element of a template: <c>brutal-ui</c>. A template binds a prefix to it, for example
    /// <c>xmlns:ui="brutal-ui"</c>, and writes each element name with that prefix.
    /// </summary>
    public const string Namespace = "brutal-ui";

    /// <summary>The settings of a <see cref="TemplateLoader"/>.</summary>
    /// <remarks>
    /// <c>default(CreateInfo)</c> gives the default settings.
    /// </remarks>
    public struct CreateInfo
    {
        /// <summary>
        /// The map that a texture attribute reads: a name in the text and the handle that the name gives. The default
        /// is <see langword="null"/>, which makes each <c>texture</c> value an
        /// <see cref="TemplateErrorKind.UnknownTexture"/> error. The loader copies the map when it is created, so a
        /// later change of the map does not change the loader. The compare of a name is ordinal.
        /// </summary>
        public IReadOnlyDictionary<string, TextureHandle>? Textures;
    }

    private readonly Dictionary<string, TextureHandle> _textures;
    private readonly Dictionary<string, RegisteredType> _types = new(StringComparer.Ordinal);
    private readonly List<AttributeHandler> _handlers = [];
    private string _typeNames = string.Empty;

    /// <summary>Creates a loader with the default settings, and registers the built-in element types.</summary>
    public TemplateLoader()
        : this(new CreateInfo())
    {
    }

    /// <summary>Creates a loader with the given settings, and registers the built-in element types.</summary>
    /// <remarks>
    /// The constructor calls the generated <c>RegisterBrutalUiMarkupElements</c> method. The source generator writes
    /// that method from the declarations of <see cref="BuiltInElements"/>, with no reflection. A game adds its own
    /// types after the constructor, with the public <see cref="Register"/> call, or with the register call that the
    /// generator writes for the game assembly.
    /// </remarks>
    /// <param name="info">The settings. The loader keeps a copy of the values that it uses.</param>
    public TemplateLoader(in CreateInfo info)
    {
        _textures = new Dictionary<string, TextureHandle>(StringComparer.Ordinal);
        if (info.Textures is not null)
        {
            foreach (KeyValuePair<string, TextureHandle> pair in info.Textures)
            {
                _textures[pair.Key] = pair.Value;
            }
        }

        this.RegisterBrutalUiMarkupElements();
    }

    /// <summary>Registers one element type.</summary>
    /// <remarks>
    /// <para>
    /// The loader checks the entry before it registers anything. Each fault throws
    /// <see cref="ArgumentException"/> with a message that names the fault, and registers nothing. The checks:
    /// <see cref="ElementTypeInfo.Name"/> is a valid XML local name and the name of no registered type;
    /// <see cref="ElementTypeInfo.Create"/> is not <see langword="null"/>; each own row has a valid name; a
    /// <see cref="AttributeRowKind.Set"/> row has a setter; no two rows share one name; and no own row takes the name
    /// of a base row.
    /// </para>
    /// <para>
    /// The loader copies the <see cref="ElementTypeInfo.Rows"/> array, and places the base rows before the own rows.
    /// A later change of the array changes nothing.
    /// </para>
    /// <para>
    /// The loader calls a registered factory for each element with that name. A game calls this method for its own
    /// types, after the constructor.
    /// </para>
    /// </remarks>
    /// <param name="info">The table entry of the type.</param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="info"/> holds a <see langword="null"/> <see cref="ElementTypeInfo.Name"/> or a
    /// <see langword="null"/> <see cref="ElementTypeInfo.Create"/>.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// <paramref name="info"/> holds another fault of the list above. The method registers nothing.
    /// </exception>
    public void Register(in ElementTypeInfo info)
    {
        if (info.Name is null)
        {
            throw new ArgumentNullException(nameof(info), "ElementTypeInfo.Name is null. Set the XML name of the type.");
        }

        if (!IsValidXmlName(info.Name))
        {
            throw new ArgumentException(
                $"'{info.Name}' is not a valid XML name. Write letters, digits, '-', '.', and '_', and start with a letter or '_'.",
                nameof(info));
        }

        if (info.Create is null)
        {
            throw new ArgumentNullException(
                nameof(info),
                "ElementTypeInfo.Create is null. Set the factory of the type.");
        }

        AttributeRow[] rows = info.Rows ?? [];
        foreach (AttributeRow row in rows)
        {
            if (!IsValidXmlName(row.Name))
            {
                throw new ArgumentException(
                    $"'{row.Name}' is not a valid XML name for a row. Write letters, digits, '-', '.', and '_', and start with a letter or '_'.",
                    nameof(info));
            }

            if (row.Kind == AttributeRowKind.Set && row.Setter is null)
            {
                throw new ArgumentException(
                    $"The row '{row.Name}' is a set row with no setter. Pass the setter to the AttributeRow constructor.",
                    nameof(info));
            }

            foreach (AttributeRow baseRow in BaseRows.All)
            {
                if (string.Equals(baseRow.Name, row.Name, StringComparison.Ordinal))
                {
                    throw new ArgumentException(
                        $"The row '{row.Name}' takes the name of a base row. Each type holds the base rows already. Remove the row.",
                        nameof(info));
                }
            }
        }

        for (int i = 1; i < rows.Length; i++)
        {
            for (int j = 0; j < i; j++)
            {
                if (string.Equals(rows[i].Name, rows[j].Name, StringComparison.Ordinal))
                {
                    throw new ArgumentException(
                        $"Two rows share the name '{rows[i].Name}'. Give each row one name.",
                        nameof(info));
                }
            }
        }

        if (_types.ContainsKey(info.Name))
        {
            throw new ArgumentException(
                $"The type '{info.Name}' is registered already. Use another name.",
                nameof(info));
        }

        _types.Add(info.Name, new RegisteredType(in info, BaseRows.All));
        _typeNames = BuildTypeNames();
    }

    /// <summary>Tells whether the text is a valid XML local name (a non-empty NCName).</summary>
    /// <param name="name">The name to check.</param>
    /// <returns><see langword="true"/> when the name is a valid XML local name.</returns>
    /// <remarks>
    /// <see cref="XmlConvert.VerifyNCName"/> throws <see cref="XmlException"/> for a bad character, and
    /// <see cref="ArgumentException"/> for an empty name. The method turns both into <see langword="false"/>, so
    /// <see cref="Register"/> gives one <see cref="ArgumentException"/> that names the fault.
    /// </remarks>
    private static bool IsValidXmlName(string name)
    {
        try
        {
            _ = XmlConvert.VerifyNCName(name);
            return true;
        }
        catch (Exception ex) when (ex is XmlException or ArgumentException)
        {
            return false;
        }
    }

    /// <summary>Adds one handler to the attribute hook.</summary>
    /// <remarks>
    /// <para>
    /// The loader calls a handler for each attribute that no row of the table holds, after the rows of the element.
    /// The handlers run in the order of the add, and the first handler that returns
    /// <see langword="true"/> takes the attribute. An attribute that no handler takes gives an
    /// <see cref="TemplateErrorKind.UnknownAttribute"/> error.
    /// </para>
    /// <para>
    /// The binding-attributes feature registers <c>command</c> and <c>bind-text</c> here. A game can add its own
    /// attribute the same way.
    /// </para>
    /// </remarks>
    /// <param name="handler">The handler.</param>
    /// <exception cref="ArgumentNullException"><paramref name="handler"/> is <see langword="null"/>.</exception>
    public void AddAttributeHandler(AttributeHandler handler)
    {
        ArgumentNullException.ThrowIfNull(handler);
        _handlers.Add(handler);
    }

    /// <summary>Loads a UTF-8 template text.</summary>
    /// <remarks>
    /// <para>
    /// The method reads the text with <see cref="System.Xml.XmlReader"/>. It prohibits a document type definition, so
    /// no entity expansion and no external read can run. The method allocates. Call it at load time, not in the frame
    /// loop.
    /// </para>
    /// <para>
    /// The method records each error of the text and keeps reading. One error or more gives no root. XML that is not
    /// well formed stops the read with one error.
    /// </para>
    /// </remarks>
    /// <param name="utf8">The UTF-8 text.</param>
    /// <param name="sourceName">The name of the source, for example a file name. Each error holds it.</param>
    /// <returns>The root, or the errors. The method never throws for the text.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="sourceName"/> is <see langword="null"/>.</exception>
    public TemplateLoadResult Load(ReadOnlySpan<byte> utf8, string sourceName = "template")
    {
        ArgumentNullException.ThrowIfNull(sourceName);

        TemplateReader reader = new(this, sourceName);
        return reader.Load(utf8);
    }

    /// <summary>Loads a template text. The method encodes the text to UTF-8 and then loads the bytes.</summary>
    /// <remarks>
    /// <para>
    /// The method allocates. Call it at load time, not in the frame loop. Write a constant text as a <c>u8</c> literal
    /// and call <see cref="Load(ReadOnlySpan{byte}, string)"/>, because the BRUTAL analyzer rejects a string literal
    /// here.
    /// </para>
    /// </remarks>
    /// <param name="text">The text.</param>
    /// <param name="sourceName">The name of the source, for example a file name. Each error holds it.</param>
    /// <returns>The root, or the errors. The method never throws for the text.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="text"/> or <paramref name="sourceName"/> is <see langword="null"/>.</exception>
    public TemplateLoadResult Load(string text, string sourceName = "template")
    {
        ArgumentNullException.ThrowIfNull(text);
        ArgumentNullException.ThrowIfNull(sourceName);

        return Load(Encoding.UTF8.GetBytes(text), sourceName);
    }

    /// <summary>Looks one texture name up in the copied map. The compare is ordinal.</summary>
    /// <param name="name">The texture name.</param>
    /// <param name="texture">The handle when the map holds the name, or the none handle.</param>
    /// <returns><see langword="true"/> when the map holds the name.</returns>
    internal bool TryGetTexture(string name, out TextureHandle texture)
    {
        if (_textures.TryGetValue(name, out texture))
        {
            return true;
        }

        texture = default;
        return false;
    }

    /// <summary>Gets the type registry. The reader of one load looks a name up here.</summary>
    internal IReadOnlyDictionary<string, RegisteredType> Types => _types;

    /// <summary>Gets the handlers of the attribute hook, in the order of the add. The reader of one load calls them.</summary>
    internal IReadOnlyList<AttributeHandler> Handlers => _handlers;

    /// <summary>
    /// Gets the registered type names, sorted ordinal and joined with <c>", "</c>. The reader uses the text in an
    /// <see cref="TemplateErrorKind.UnknownElement"/> message. The loader builds it again after each
    /// <see cref="Register"/> call.
    /// </summary>
    internal string TypeNames => _typeNames;

    /// <summary>Builds <see cref="TypeNames"/> from the current registry: the names, sorted ordinal, joined with <c>", "</c>.</summary>
    /// <returns>The joined names.</returns>
    private string BuildTypeNames()
    {
        string[] names = new string[_types.Count];
        _types.Keys.CopyTo(names, 0);
        Array.Sort(names, StringComparer.Ordinal);

        StringBuilder text = new();
        for (int i = 0; i < names.Length; i++)
        {
            if (i > 0)
            {
                text.Append(", ");
            }

            text.Append(names[i]);
        }

        return text.ToString();
    }
}
