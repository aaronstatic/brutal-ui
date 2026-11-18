using System;
using System.Xml;
using BrutalUi.Markup;

namespace BrutalUi.Framework;

internal sealed partial class DefinitionReader
{
    /// <summary>Reads one element node: the failed parent, the namespace, the place, the attributes, the required
    /// attributes, and the entry.</summary>
    /// <param name="xml">The reader, on the element node.</param>
    private void ReadElement(XmlReader xml)
    {
        IXmlLineInfo lineInfo = (IXmlLineInfo)xml;
        string qualifiedName = xml.Name;
        string localName = xml.LocalName;
        int line = lineInfo.LineNumber;
        int column = lineInfo.LinePosition;

        // XmlReader gives the correct value only on the element node, so read it before the attribute walk.
        bool isEmpty = xml.IsEmptyElement;

        Kind parent = _frames.Count == 0 ? Kind.Root : _frames.Peek().Kind;

        // A child of a failed element gives no error. The fault of the parent names the place. Read this before the
        // namespace, so a child outside the namespace under a failed element gives no follow-on error.
        if (parent == Kind.Failed)
        {
            PushFrame(Kind.Failed, -1, default, isEmpty);
            return;
        }

        // Check 1. An element outside the namespace is an error. Read its subtree with no error.
        if (xml.NamespaceURI != TemplateLoader.Namespace)
        {
            AddError(
                DefinitionErrorKind.UnknownElement,
                $"'{qualifiedName}' is not in the namespace '{TemplateLoader.Namespace}'. "
                    + $"Add xmlns:ui=\"{TemplateLoader.Namespace}\" and write <ui:{localName}>.",
                line,
                column);
            PushFrame(Kind.Failed, -1, default, isEmpty);
            return;
        }

        // Check 2. The place under the parent.
        bool atRoot = !_sawRoot && parent == Kind.Root;
        Kind kind = ResolveKind(parent, localName, atRoot);
        if (kind == Kind.Unknown)
        {
            AddError(DefinitionErrorKind.UnknownElement, PlaceMessage(parent, localName, atRoot), line, column);
            PushFrame(Kind.Failed, -1, default, isEmpty);
            return;
        }

        if (atRoot)
        {
            _sawRoot = true;
        }

        // Checks 3 and 4. The attributes, then the required attributes.
        ReadAttributes(xml, kind, localName, line, column);

        switch (kind)
        {
            case Kind.Definitions:
                PushFrame(Kind.Definitions, -1, default, isEmpty);
                break;

            case Kind.Panel:
                _panelEntries.Add(new PanelEntry
                {
                    Name = ValueOf("name"),
                    Slot = ValueOf("slot"),
                    Template = ValueOf("template"),
                    Line = line,
                    Column = column,
                });
                PushFrame(Kind.Panel, _panelEntries.Count - 1, default, isEmpty);
                break;

            case Kind.Layout:
                _layoutEntries.Add(new LayoutEntry
                {
                    Name = ValueOf("name"),
                    Template = ValueOf("template"),
                    Line = line,
                    Column = column,
                });
                PushFrame(Kind.Layout, _layoutEntries.Count - 1, default, isEmpty);
                break;

            case Kind.Mode:
                _modeEntries.Add(new ModeEntry
                {
                    Name = ValueOf("name"),
                    Layout = ValueOf("layout"),
                    Actions = ValueOf("actions"),
                    Line = line,
                    Column = column,
                    Placements = [],
                });
                PushFrame(Kind.Mode, _modeEntries.Count - 1, default, isEmpty);
                break;

            case Kind.Slot:
                // ResolveKind proved that the parent frame is the enclosing Mode frame.
                PushFrame(Kind.Slot, _frames.Peek().Index, ValueOf("name"), isEmpty);
                break;

            case Kind.PanelRef:
                // The placement joins the placements of its mode, in the order of the text. The list is a reference,
                // so the add reaches the stored entry. A PanelRef under a Slot takes the slot name of that frame. A
                // PanelRef directly under a Mode takes no slot value, so the resolve pass uses the default slot of the
                // panel record (Q5).
                Frame parentFrame = _frames.Peek();
                bool underSlot = parentFrame.Kind == Kind.Slot;
                DefinitionValue slotValue = underSlot ? parentFrame.SlotName : default;
                ModeEntry placementMode = _modeEntries[parentFrame.Index];
                placementMode.Placements.Add(new PlacementEntry
                {
                    Name = ValueOf("name"),
                    Slot = slotValue,
                    UnderSlot = underSlot,
                    Line = line,
                    Column = column,
                });
                PushFrame(Kind.PanelRef, -1, default, isEmpty);
                break;
        }
    }

    /// <summary>Reads the attributes of the element node, in the order of the source, and checks each one against the
    /// table of the element.</summary>
    /// <param name="xml">The reader, on the element node.</param>
    /// <param name="kind">The kind of the element.</param>
    /// <param name="elementName">The local name of the element, for each message.</param>
    /// <param name="elementLine">The line of the element name.</param>
    /// <param name="elementColumn">The column of the element name.</param>
    private void ReadAttributes(XmlReader xml, Kind kind, string elementName, int elementLine, int elementColumn)
    {
        _attributes.Clear();
        string[] allowed = AllowedNames(kind);

        if (xml.MoveToFirstAttribute())
        {
            do
            {
                if (IsNamespaceDeclaration(xml))
                {
                    continue;
                }

                IXmlLineInfo lineInfo = (IXmlLineInfo)xml;

                // Check 3a. A prefixed attribute is an error, so each definition uses one form.
                if (xml.Prefix.Length > 0)
                {
                    AddError(
                        DefinitionErrorKind.UnknownAttribute,
                        $"'{xml.Name}' is not an attribute of {elementName}. Write the attribute with no prefix.",
                        lineInfo.LineNumber,
                        lineInfo.LinePosition);
                    continue;
                }

                string name = xml.Name;

                // Check 3b. An attribute that the table does not hold is an error. Build the joined list only here,
                // so a clean read allocates nothing.
                if (Array.IndexOf(allowed, name) < 0)
                {
                    string names = string.Join(", ", allowed);
                    AddError(
                        DefinitionErrorKind.UnknownAttribute,
                        $"'{name}' is not an attribute of {elementName}. The attributes of {elementName} are {names}.",
                        lineInfo.LineNumber,
                        lineInfo.LinePosition);
                    continue;
                }

                // Check 3c. An empty value is an error. The value stays in the list, so check 4 gives no second error.
                if (xml.Value.Length == 0)
                {
                    AddError(
                        DefinitionErrorKind.InvalidValue,
                        EmptyValueMessage(name),
                        lineInfo.LineNumber,
                        lineInfo.LinePosition);
                }

                _attributes.Add((name, xml.Value, lineInfo.LineNumber, lineInfo.LinePosition));
            }
            while (xml.MoveToNextAttribute());

            _ = xml.MoveToElement();
        }

        // Check 4. Each required name with no attribute gives MissingAttribute at the element. An optional name, the
        // actions name of Mode, gives no error.
        string[] required = RequiredNames(kind);
        for (int i = 0; i < required.Length; i++)
        {
            if (IndexOfName(required[i]) < 0)
            {
                AddError(
                    DefinitionErrorKind.MissingAttribute,
                    $"{elementName} needs the attribute '{required[i]}'.",
                    elementLine,
                    elementColumn);
            }
        }
    }

    /// <summary>Gives the value of one attribute name, or a default value when the element holds no such
    /// attribute.</summary>
    /// <param name="name">The attribute name. The compare is ordinal.</param>
    /// <returns>The value.</returns>
    private DefinitionValue ValueOf(string name)
    {
        int index = IndexOfName(name);
        if (index < 0)
        {
            return default;
        }

        (string attributeName, string value, int line, int column) = _attributes[index];
        return new DefinitionValue(value, line, column);
    }

    /// <summary>Gives the index of one attribute name in the list of the element, or -1.</summary>
    /// <param name="name">The attribute name. The compare is ordinal.</param>
    /// <returns>The index, or -1.</returns>
    private int IndexOfName(string name)
    {
        for (int i = 0; i < _attributes.Count; i++)
        {
            if (_attributes[i].Name == name)
            {
                return i;
            }
        }

        return -1;
    }

    /// <summary>Tells whether the reader stands on a namespace declaration: <c>xmlns</c> or <c>xmlns:prefix</c>. The
    /// walk gives such a declaration no error.</summary>
    /// <param name="xml">The reader, on an attribute node.</param>
    /// <returns><see langword="true"/> for a namespace declaration.</returns>
    private static bool IsNamespaceDeclaration(XmlReader xml)
    {
        return xml.Prefix == "xmlns" || (xml.Prefix.Length == 0 && xml.LocalName == "xmlns");
    }

    /// <summary>Pushes the frame of one element, when the element can still hold children.</summary>
    /// <param name="kind">The kind of the element.</param>
    /// <param name="index">The index of the entry in its own list: the enclosing mode for a Slot frame.</param>
    /// <param name="slotName">The <c>name</c> value for a Slot frame, or a default value.</param>
    /// <param name="isEmpty">Whether the element node has no children.</param>
    private void PushFrame(Kind kind, int index, DefinitionValue slotName, bool isEmpty)
    {
        if (!isEmpty)
        {
            _frames.Push(new Frame { Kind = kind, Index = index, SlotName = slotName });
        }
    }
}
