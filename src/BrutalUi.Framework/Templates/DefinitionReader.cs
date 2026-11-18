using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;

namespace BrutalUi.Framework;

/// <summary>
/// The two passes of one <see cref="DefinitionLoader.Load"/> call: the read pass walks the text with an
/// <see cref="XmlReader"/> and records each entry with the position of each value, and the resolve pass checks the
/// names and builds the records.
/// </summary>
/// <remarks>
/// <para>
/// One reader serves one load call. The walk reads one element at a time. It records an error and keeps reading, so
/// one text gives one error for each fault. XML that is not well formed stops the read with one
/// <see cref="DefinitionErrorKind.NotWellFormed"/> error, and the resolve pass stops for it. The load sorts the
/// errors by line, then column. This is the pattern of <c>TemplateReader</c>: the walk lives in
/// <c>DefinitionReader.Elements.cs</c> and the resolve pass in <c>DefinitionReader.Resolve.cs</c>.
/// </para>
/// <para>
/// <strong>The checks of one element,</strong> in this order: the namespace, the place under the parent, the
/// attributes (a prefixed attribute, an unknown attribute, an empty value), and the required attributes. A failed
/// element pushes a failed frame, so its children give no error. The frame stack tracks the parent.
/// </para>
/// <para>
/// <strong>Phase 1 scope.</strong> The walk reads <c>Definitions</c>, <c>Panel</c>, <c>Layout</c>, <c>Mode</c>,
/// <c>Slot</c>, and a <c>PanelRef</c> under a <c>Slot</c>. The <c>actions</c> attribute and a <c>PanelRef</c>
/// directly under <c>Mode</c> come in phase 2, so an <c>actions</c> attribute gives
/// <see cref="DefinitionErrorKind.UnknownAttribute"/> today. A text node gives no error until phase 3 reports it as
/// <see cref="DefinitionErrorKind.UnexpectedText"/>. The resolve pass gives the unknown-name kinds of phase 1 and
/// skips a faulty entry with no second error; phase 3 adds the duplicate check, the slot check, the check load of a
/// used layout, and the follow-on rule.
/// </para>
/// </remarks>
internal sealed partial class DefinitionReader
{
    /// <summary>The loader that owns the template table and the action map table.</summary>
    private readonly DefinitionLoader _loader;

    /// <summary>The name of the source for the error lines.</summary>
    private readonly string _sourceName;

    /// <summary>The stack of open elements. The top holds the parent of the element that the walk reads now.</summary>
    private readonly Stack<Frame> _frames = new();

    /// <summary>
    /// The attributes of the element that the walk reads now, in the order of the source. The walk fills this list
    /// before it makes an entry, and <see cref="ValueOf"/> reads it.
    /// </summary>
    private readonly List<(string Name, string Value, int Line, int Column)> _attributes = [];

    /// <summary>The panel entries, in the order of the text.</summary>
    private readonly List<PanelEntry> _panelEntries = [];

    /// <summary>The layout entries, in the order of the text.</summary>
    private readonly List<LayoutEntry> _layoutEntries = [];

    /// <summary>The mode entries, in the order of the text. Each entry holds its placements in the order of the text.</summary>
    private readonly List<ModeEntry> _modeEntries = [];

    /// <summary>Whether the walk saw the root element.</summary>
    private bool _sawRoot;

    /// <summary>Creates the reader for one load call.</summary>
    /// <param name="loader">The loader that owns the tables.</param>
    /// <param name="sourceName">The name of the source for the error lines.</param>
    internal DefinitionReader(DefinitionLoader loader, string sourceName)
    {
        _loader = loader;
        _sourceName = sourceName;
    }

    /// <summary>Gets the errors that the load records, in the order of the load.</summary>
    internal List<DefinitionError> Errors { get; } = [];

    /// <summary>Reads and resolves one definitions text.</summary>
    /// <param name="utf8">The definitions text, coded as UTF-8.</param>
    /// <returns>The records, or the errors.</returns>
    internal DefinitionLoadResult Load(ReadOnlySpan<byte> utf8)
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

                    default:
                        // Text, CDATA, whitespace, and the declaration give no work in this phase. See the remarks.
                        break;
                }
            }
        }
        catch (XmlException ex)
        {
            AddError(
                DefinitionErrorKind.NotWellFormed,
                "The definition text is not well-formed XML. " + ex.Message,
                Math.Max(1, ex.LineNumber),
                Math.Max(1, ex.LinePosition));
        }

        if (Errors.Count == 0 && !_sawRoot)
        {
            AddError(DefinitionErrorKind.NotWellFormed, "The definition text holds no root element.", 1, 1);
        }

        List<PanelDefinition> panels = [];
        List<LayoutDefinition> layouts = [];
        List<ModeDefinition> modes = [];
        Resolve(panels, layouts, modes);

        if (Errors.Count > 0)
        {
            SortErrors();
            return DefinitionLoadResult.Failure(Errors);
        }

        return DefinitionLoadResult.Success(panels, layouts, modes);
    }

    /// <summary>Records one error.</summary>
    /// <param name="kind">The kind of the error.</param>
    /// <param name="message">The problem and the fix.</param>
    /// <param name="line">The line of the error, from 1.</param>
    /// <param name="column">The column of the error, from 1.</param>
    internal void AddError(DefinitionErrorKind kind, string message, int line, int column)
    {
        Errors.Add(new DefinitionError(kind, line, column, message, _sourceName));
    }

    /// <summary>Checks whether the read pass recorded one <see cref="DefinitionErrorKind.NotWellFormed"/> error. The
    /// resolve pass stops for it.</summary>
    /// <returns><see langword="true"/> when the walk stopped.</returns>
    private bool HoldsNotWellFormed()
    {
        for (int i = 0; i < Errors.Count; i++)
        {
            if (Errors[i].Kind == DefinitionErrorKind.NotWellFormed)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>Sorts the errors by line, then column, stable. The original index is the tie-break.</summary>
    private void SortErrors()
    {
        int count = Errors.Count;
        if (count < 2)
        {
            return;
        }

        int[] order = new int[count];
        for (int i = 0; i < count; i++)
        {
            order[i] = i;
        }

        Array.Sort(order, (first, second) =>
        {
            DefinitionError left = Errors[first];
            DefinitionError right = Errors[second];
            int byLine = left.Line.CompareTo(right.Line);
            if (byLine != 0)
            {
                return byLine;
            }

            int byColumn = left.Column.CompareTo(right.Column);
            return byColumn != 0 ? byColumn : first.CompareTo(second);
        });

        List<DefinitionError> sorted = new(count);
        for (int i = 0; i < count; i++)
        {
            sorted.Add(Errors[order[i]]);
        }

        Errors.Clear();
        Errors.AddRange(sorted);
    }

    /// <summary>One attribute value with the position of its attribute name. A default value means that the element
    /// holds no such attribute.</summary>
    private readonly struct DefinitionValue
    {
        /// <summary>The text of the value, or <see langword="null"/> for no value.</summary>
        private readonly string? _text;

        /// <summary>Creates a value.</summary>
        /// <param name="text">The text of the value.</param>
        /// <param name="line">The line of the attribute name, from 1.</param>
        /// <param name="column">The column of the attribute name, from 1.</param>
        internal DefinitionValue(string text, int line, int column)
        {
            _text = text;
            Line = line;
            Column = column;
        }

        /// <summary>Gets the text of the value, or <see langword="null"/> for no value.</summary>
        internal string? Text => _text;

        /// <summary>Gets the line of the attribute name, from 1.</summary>
        internal int Line { get; }

        /// <summary>Gets the column of the attribute name, from 1.</summary>
        internal int Column { get; }

        /// <summary>Gets a value that shows whether the value holds a name. No value and an empty value give
        /// <see langword="false"/>, because each already has its error.</summary>
        internal bool HasName => _text is { Length: > 0 };
    }

    /// <summary>One <c>Panel</c> entry.</summary>
    private struct PanelEntry
    {
        /// <summary>The <c>name</c> value.</summary>
        internal DefinitionValue Name;

        /// <summary>The <c>slot</c> value.</summary>
        internal DefinitionValue Slot;

        /// <summary>The <c>template</c> value.</summary>
        internal DefinitionValue Template;

        /// <summary>The line of the element name, for an error at the element.</summary>
        internal int Line;

        /// <summary>The column of the element name, for an error at the element.</summary>
        internal int Column;
    }

    /// <summary>One <c>Layout</c> entry.</summary>
    private struct LayoutEntry
    {
        /// <summary>The <c>name</c> value.</summary>
        internal DefinitionValue Name;

        /// <summary>The <c>template</c> value.</summary>
        internal DefinitionValue Template;

        /// <summary>The line of the element name, for an error at the element.</summary>
        internal int Line;

        /// <summary>The column of the element name, for an error at the element. Phase 3 places a layout template
        /// fault here.</summary>
        internal int Column;
    }

    /// <summary>One <c>Mode</c> entry, with its placements in the order of the text.</summary>
    private struct ModeEntry
    {
        /// <summary>The <c>name</c> value.</summary>
        internal DefinitionValue Name;

        /// <summary>The <c>layout</c> value.</summary>
        internal DefinitionValue Layout;

        /// <summary>The line of the element name, for an error at the element.</summary>
        internal int Line;

        /// <summary>The column of the element name, for an error at the element.</summary>
        internal int Column;

        /// <summary>The placements of the mode. The list is a reference, so a placement joins the stored entry.</summary>
        internal List<PlacementEntry> Placements;
    }

    /// <summary>One <c>PanelRef</c> placement.</summary>
    private struct PlacementEntry
    {
        /// <summary>The <c>name</c> value of the <c>PanelRef</c>: the panel of the placement.</summary>
        internal DefinitionValue Name;

        /// <summary>The <c>name</c> value of the enclosing <c>Slot</c>. Phase 2 gives a default value for a
        /// <c>PanelRef</c> directly under a <c>Mode</c>.</summary>
        internal DefinitionValue Slot;

        /// <summary>The line of the element name, for an error at the element.</summary>
        internal int Line;

        /// <summary>The column of the element name, for an error at the element.</summary>
        internal int Column;
    }

    /// <summary>The state of one open element.</summary>
    private struct Frame
    {
        /// <summary>The kind of the element.</summary>
        internal Kind Kind;

        /// <summary>The index of the entry in its own list: the enclosing mode for a Slot frame.</summary>
        internal int Index;

        /// <summary>The <c>name</c> value of a Slot frame, for each placement under it.</summary>
        internal DefinitionValue SlotName;
    }

    /// <summary>The kinds of the walk: the elements of the format, the root position, a failed element, and no valid
    /// element.</summary>
    private enum Kind : byte
    {
        /// <summary>The position before the root element.</summary>
        Root,

        /// <summary>The <c>Definitions</c> element.</summary>
        Definitions,

        /// <summary>The <c>Panel</c> element.</summary>
        Panel,

        /// <summary>The <c>Layout</c> element.</summary>
        Layout,

        /// <summary>The <c>Mode</c> element.</summary>
        Mode,

        /// <summary>The <c>Slot</c> element.</summary>
        Slot,

        /// <summary>The <c>PanelRef</c> element.</summary>
        PanelRef,

        /// <summary>An element that gave an error. Its children give no error.</summary>
        Failed,

        /// <summary>No element of this name at this place.</summary>
        Unknown,
    }
}
