using System;
using System.Collections.Generic;
using System.Xml;
using BrutalUi.Core;

namespace BrutalUi.Markup;

internal sealed partial class TemplateReader
{
    /// <summary>Reads one element node: the namespace, the leaf rule, the type, the attributes, the factory, the
    /// set rows, the hook, and the attach.</summary>
    /// <param name="xml">The reader, on the element node.</param>
    private void ReadElement(XmlReader xml)
    {
        IXmlLineInfo lineInfo = (IXmlLineInfo)xml;
        string qualifiedName = xml.Name;
        ElementName = xml.LocalName;
        ElementLine = lineInfo.LineNumber;
        ElementColumn = lineInfo.LinePosition;

        // XmlReader gives the correct value only on the element node, so read it before the attribute loop.
        bool isEmpty = xml.IsEmptyElement;

        Attributes.Clear();
        CurrentType = null;

        // Step 1. An element outside the namespace is an error. Read the subtree with a null parent.
        if (xml.NamespaceURI != TemplateLoader.Namespace)
        {
            AddError(
                TemplateErrorKind.UnknownElement,
                $"'{qualifiedName}' is not in the namespace '{TemplateLoader.Namespace}'. "
                    + $"Add xmlns:ui=\"{TemplateLoader.Namespace}\" and write <ui:{ElementName}>.",
                ElementLine,
                ElementColumn);
            PushFailedFrame(isEmpty);
            return;
        }

        // Step 2. A child of a leaf type is an error. Read the subtree with a null parent.
        if (_frames.Count > 0 && _frames.Peek().IsLeaf)
        {
            string parentName = _frames.Peek().TypeName;
            AddError(
                TemplateErrorKind.UnexpectedChild,
                $"'{ElementName}' cannot be a child of {parentName}. Remove it, or move it outside the {parentName}.",
                ElementLine,
                ElementColumn);
            PushFailedFrame(isEmpty);
            return;
        }

        // Step 3. No registered type for the local name is an error. Read the subtree with a null parent.
        if (!_loader.Types.TryGetValue(ElementName, out RegisteredType? type))
        {
            AddError(
                TemplateErrorKind.UnknownElement,
                $"'{ElementName}' is not an element type. The types are {_loader.TypeNames}.",
                ElementLine,
                ElementColumn);
            PushFailedFrame(isEmpty);
            return;
        }

        CurrentType = type;
        ReadAttributes(xml, type, lineInfo);

        Element? element = Create(type);
        if (element is null)
        {
            PushFailedFrame(isEmpty);
            return;
        }

        ApplyRows(element, type);
        RunHook(element, type);
        Attach(element);

        if (!isEmpty)
        {
            _frames.Push((element, type.Name, type.IsLeaf));
        }
    }

    /// <summary>Reads the attributes of the element node, in the source order, and finds the row of each one.</summary>
    /// <param name="xml">The reader, on the element node.</param>
    /// <param name="type">The table entry of the element.</param>
    /// <param name="lineInfo">The position reader.</param>
    private void ReadAttributes(XmlReader xml, RegisteredType type, IXmlLineInfo lineInfo)
    {
        if (!xml.MoveToFirstAttribute())
        {
            return;
        }

        do
        {
            if (IsNamespaceDeclaration(xml))
            {
                continue;
            }

            // Step 4. A prefixed attribute is an error, so each template uses one form.
            if (xml.Prefix.Length > 0)
            {
                AddError(
                    TemplateErrorKind.UnknownAttribute,
                    $"'{xml.Name}' is not an attribute of {type.Name}. Write the attribute with no prefix.",
                    lineInfo.LineNumber,
                    lineInfo.LinePosition);
                continue;
            }

            string name = xml.Name;
            Attributes.Add((name, xml.Value, lineInfo.LineNumber, lineInfo.LinePosition, type.FindRowIndex(name)));
        }
        while (xml.MoveToNextAttribute());

        _ = xml.MoveToElement();
    }

    /// <summary>Checks the required create rows, then gives the element of the factory, or
    /// <see langword="null"/> when a missing row or a factory error stopped the create.</summary>
    /// <param name="type">The table entry of the element.</param>
    /// <returns>The new element, or <see langword="null"/>.</returns>
    private Element? Create(RegisteredType type)
    {
        // Step 5. Each missing CreateRequired row gives MissingAttribute at the element, in table order, and no factory call.
        bool missing = false;
        foreach (int rowIndex in type.RequiredIndices)
        {
            if (FindAttributeOfRow(rowIndex) >= 0)
            {
                continue;
            }

            string rowName = type.Rows[rowIndex].Name;
            AddError(TemplateErrorKind.MissingAttribute, $"{ElementName} needs the attribute '{rowName}'.", ElementLine, ElementColumn);
            missing = true;
        }

        if (missing)
        {
            return null;
        }

        ElementCreateArgs args = new(this);
        int errorCount = Errors.Count;
        Element? element = type.Create(args);
        if (element is not null)
        {
            return element;
        }

        // Step 6. A null factory result with no new error is a bug of the factory.
        if (Errors.Count == errorCount)
        {
            throw new InvalidOperationException(
                $"The factory of '{type.Name}' returned null with no error. Call AddError before you return null.");
        }

        return null;
    }

    /// <summary>Runs the set rows of the type that have an attribute, in table order.</summary>
    /// <param name="element">The element that the factory made.</param>
    /// <param name="type">The table entry of the element.</param>
    private void ApplyRows(Element element, RegisteredType type)
    {
        for (int rowIndex = 0; rowIndex < type.Rows.Length; rowIndex++)
        {
            AttributeRow row = type.Rows[rowIndex];
            if (row.Kind != AttributeRowKind.Set || row.Setter is null)
            {
                continue;
            }

            int index = FindAttributeOfRow(rowIndex);
            if (index >= 0)
            {
                row.Setter(element, new AttributeContext(this, index));
            }
        }
    }

    /// <summary>Calls the hook for each attribute that no row holds, in the source order.</summary>
    /// <param name="element">The element that the factory made.</param>
    /// <param name="type">The table entry of the element.</param>
    private void RunHook(Element element, RegisteredType type)
    {
        IReadOnlyList<AttributeHandler> handlers = _loader.Handlers;
        for (int i = 0; i < Attributes.Count; i++)
        {
            (string name, string value, int line, int column, int row) = Attributes[i];
            if (row >= 0)
            {
                continue;
            }

            AttributeContext context = new(this, i);
            bool handled = false;
            for (int h = 0; h < handlers.Count; h++)
            {
                if (handlers[h](element, name, value, context))
                {
                    handled = true;
                    break;
                }
            }

            if (!handled)
            {
                AddError(
                    TemplateErrorKind.UnknownAttribute,
                    $"'{name}' is not an attribute of {type.Name}. The attributes of {type.Name} are {type.AttributeNames}.",
                    line,
                    column);
            }
        }
    }

    /// <summary>Adds the element to the parent of the frame, or keeps it as the root.</summary>
    /// <param name="element">The element to attach.</param>
    private void Attach(Element element)
    {
        if (_frames.Count == 0)
        {
            _root ??= element;
            return;
        }

        if (_frames.Peek().Parent is Element parent)
        {
            parent.Children.Add(element);
        }
    }

    /// <summary>Pushes the frame of an element that gave an error, so its children report their own errors.</summary>
    /// <param name="isEmpty">Whether the element node has no children.</param>
    private void PushFailedFrame(bool isEmpty)
    {
        if (!isEmpty)
        {
            _frames.Push((null, string.Empty, false));
        }
    }

    /// <summary>Gives the index of the attribute that holds this row index, or -1.</summary>
    /// <param name="rowIndex">The index of the row in the table of the type.</param>
    /// <returns>The index into <see cref="Attributes"/>, or -1.</returns>
    private int FindAttributeOfRow(int rowIndex)
    {
        for (int i = 0; i < Attributes.Count; i++)
        {
            if (Attributes[i].Row == rowIndex)
            {
                return i;
            }
        }

        return -1;
    }

    /// <summary>Tells whether the reader stands on a namespace declaration: <c>xmlns</c> or <c>xmlns:prefix</c>.</summary>
    /// <param name="xml">The reader, on an attribute node.</param>
    /// <returns><see langword="true"/> for a namespace declaration.</returns>
    private static bool IsNamespaceDeclaration(XmlReader xml)
    {
        return xml.Prefix == "xmlns" || (xml.Prefix.Length == 0 && xml.LocalName == "xmlns");
    }

    /// <summary>Records one <see cref="TemplateErrorKind.UnexpectedText"/> error at a text or CDATA node.</summary>
    /// <param name="xml">The reader, on the text or CDATA node.</param>
    private void AddUnexpectedText(XmlReader xml)
    {
        IXmlLineInfo lineInfo = (IXmlLineInfo)xml;
        string text = (xml.Value ?? string.Empty).Trim();
        AddError(
            TemplateErrorKind.UnexpectedText,
            $"'{text}' is text between elements. Remove it, or write it as the 'text' attribute of a Label.",
            lineInfo.LineNumber,
            lineInfo.LinePosition);
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

        Array.Sort(order, (a, b) =>
        {
            TemplateError first = Errors[a];
            TemplateError second = Errors[b];
            int byLine = first.Line.CompareTo(second.Line);
            if (byLine != 0)
            {
                return byLine;
            }

            int byColumn = first.Column.CompareTo(second.Column);
            return byColumn != 0 ? byColumn : a.CompareTo(b);
        });

        List<TemplateError> sorted = new(count);
        for (int i = 0; i < count; i++)
        {
            sorted.Add(Errors[order[i]]);
        }

        Errors.Clear();
        Errors.AddRange(sorted);
    }
}
