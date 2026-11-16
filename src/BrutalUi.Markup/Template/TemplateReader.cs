using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using BrutalUi.Core;

namespace BrutalUi.Markup;

/// <summary>
/// The walk of one <see cref="TemplateLoader"/> load: the <see cref="XmlReader"/> read, the element and attribute
/// state of the node that the walk reads now, and the errors.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="AttributeContext"/> and <see cref="ElementCreateArgs"/> read this state, so they hold a reference to it.
/// One reader serves one load call.
/// </para>
/// <para>
/// The walk reads one element at a time. It records an error and keeps reading, so one text gives one error for each
/// fault. XML that is not well formed stops the read with one error. The result sorts the errors by line, then column.
/// </para>
/// </remarks>
internal sealed partial class TemplateReader
{
    private readonly TemplateLoader _loader;
    private readonly Stack<(Element? Parent, string TypeName, bool IsLeaf)> _frames = new();
    private Element? _root;

    /// <summary>Creates the state of one load call.</summary>
    /// <param name="loader">The loader that owns the type registry and the attribute hook.</param>
    /// <param name="sourceName">The name of the source, for a message and for an error.</param>
    internal TemplateReader(TemplateLoader loader, string sourceName)
    {
        _loader = loader;
        SourceName = sourceName;
    }

    /// <summary>Gets the name of the source, for example <c>hud.xml</c>.</summary>
    internal string SourceName { get; }

    /// <summary>Gets the errors that the load records, in the order of the load.</summary>
    internal List<TemplateError> Errors { get; } = [];

    /// <summary>
    /// Gets the attributes of the element that the walk reads now, in the order of the source. <c>Row</c> is the
    /// index of the table row of the attribute, or -1 when no row holds it.
    /// </summary>
    internal List<(string Name, string Value, int Line, int Column, int Row)> Attributes { get; } = [];

    /// <summary>Gets or sets the XML local name of the element that the walk reads now.</summary>
    internal string ElementName { get; set; } = string.Empty;

    /// <summary>Gets or sets the line of the name of the element that the walk reads now, from 1.</summary>
    internal int ElementLine { get; set; } = 1;

    /// <summary>Gets or sets the column of the name of the element that the walk reads now, from 1.</summary>
    internal int ElementColumn { get; set; } = 1;

    /// <summary>Gets or sets the table entry of the element that the walk reads now.</summary>
    internal RegisteredType? CurrentType { get; set; }

    /// <summary>Loads one UTF-8 template text and gives the result.</summary>
    /// <param name="utf8">The UTF-8 text.</param>
    /// <returns>The root, or the errors.</returns>
    internal TemplateLoadResult Load(ReadOnlySpan<byte> utf8)
    {
        XmlReaderSettings settings = new()
        {
            DtdProcessing = DtdProcessing.Prohibit,
            XmlResolver = null,
            IgnoreComments = true,
            IgnoreProcessingInstructions = true,
            IgnoreWhitespace = true,
            ConformanceLevel = ConformanceLevel.Document,
            CloseInput = true,
        };

        byte[] bytes = utf8.ToArray();
        try
        {
            using MemoryStream stream = new(bytes, 0, bytes.Length, writable: false);
            using XmlReader xml = XmlReader.Create(stream, settings);
            while (xml.Read())
            {
                switch (xml.NodeType)
                {
                    case XmlNodeType.Element:
                        ReadElement(xml);
                        break;

                    case XmlNodeType.EndElement:
                        if (_frames.Count > 0)
                        {
                            _ = _frames.Pop();
                        }
                        break;

                    case XmlNodeType.Text:
                    case XmlNodeType.CDATA:
                        AddUnexpectedText(xml);
                        break;

                    default:
                        // Whitespace, SignificantWhitespace, the declaration, and any skipped node give no work.
                        break;
                }
            }
        }
        catch (XmlException ex)
        {
            AddError(
                TemplateErrorKind.NotWellFormed,
                "The template is not well-formed XML. " + ex.Message,
                Math.Max(1, ex.LineNumber),
                Math.Max(1, ex.LinePosition));
        }

        if (Errors.Count == 0 && _root is null)
        {
            AddError(TemplateErrorKind.NotWellFormed, "The template holds no root element.", 1, 1);
        }

        if (Errors.Count > 0)
        {
            SortErrors();
            return TemplateLoadResult.Failure(Errors);
        }

        return TemplateLoadResult.Success(_root!);
    }

    /// <summary>Records one error.</summary>
    /// <param name="kind">The kind of the error.</param>
    /// <param name="message">The problem and the fix.</param>
    /// <param name="line">The line of the error, from 1.</param>
    /// <param name="column">The column of the error, from 1.</param>
    internal void AddError(TemplateErrorKind kind, string message, int line, int column)
    {
        Errors.Add(new TemplateError(kind, line, column, message, SourceName));
    }

    /// <summary>
    /// Finds the attribute of the create row with this name. A set row gives <see langword="false"/>, and no
    /// attribute with that row index gives <see langword="false"/>.
    /// </summary>
    /// <param name="name">The attribute name.</param>
    /// <param name="index">The index into <see cref="Attributes"/>, or -1.</param>
    /// <returns><see langword="true"/> when the list holds the attribute of a create row.</returns>
    internal bool TryGetCreateAttribute(string name, out int index)
    {
        RegisteredType? type = CurrentType;
        if (type is not null)
        {
            int rowIndex = type.FindRowIndex(name);
            if (rowIndex >= 0 && type.Rows[rowIndex].Kind != AttributeRowKind.Set)
            {
                for (int i = 0; i < Attributes.Count; i++)
                {
                    if (Attributes[i].Row == rowIndex)
                    {
                        index = i;
                        return true;
                    }
                }
            }
        }

        index = -1;
        return false;
    }

    /// <summary>Looks one texture name up in the copied map of the loader.</summary>
    /// <param name="name">The texture name.</param>
    /// <param name="texture">The handle when the map holds the name, or the none handle.</param>
    /// <returns><see langword="true"/> when the map holds the name.</returns>
    internal bool TryGetTexture(string name, out TextureHandle texture) => _loader.TryGetTexture(name, out texture);
}
