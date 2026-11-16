using System;
using System.Collections.Generic;
using System.Text;
using BrutalUi.Core;

namespace BrutalUi.Markup;

/// <summary>
/// Makes one element from the create attributes of one element node. The loader calls it after it reads the
/// attributes of the element.
/// </summary>
/// <param name="args">The create arguments. The factory reads each create row through <see cref="ElementCreateArgs.TryGet"/>.</param>
/// <returns>The new element. A null result needs one new error from <see cref="ElementCreateArgs.AddError"/>. A null
/// result with no new error is a bug of the factory: the loader throws <see cref="InvalidOperationException"/>.</returns>
public delegate Element? ElementFactory(ElementCreateArgs args);

/// <summary>
/// Writes one attribute value to one element. The loader calls it after the factory makes the element, in the order
/// of the table.
/// </summary>
/// <param name="element">The element that the factory made.</param>
/// <param name="context">The attribute: the name, the value, the position, and <see cref="AttributeContext.AddError"/>.</param>
public delegate void AttributeSetter(Element element, AttributeContext context);

/// <summary>
/// Takes one attribute that no row of the table holds. The loader calls each handler in the order of the add, after
/// the rows of the element.
/// </summary>
/// <param name="element">The element that the factory made.</param>
/// <param name="name">The attribute name.</param>
/// <param name="value">The attribute value, as <c>XmlReader</c> gives it.</param>
/// <param name="context">The position of the attribute and <see cref="AttributeContext.AddError"/>.</param>
/// <returns><see langword="true"/> when the handler takes the attribute. <see langword="false"/> gives the attribute
/// to the next handler. No handler takes it, so the loader records <see cref="TemplateErrorKind.UnknownAttribute"/>.</returns>
public delegate bool AttributeHandler(Element element, string name, string value, AttributeContext context);

/// <summary>
/// The kind of an <see cref="AttributeRow"/>. The kind says who reads the attribute.
/// </summary>
public enum AttributeRowKind : byte
{
    /// <summary>A static <see cref="AttributeRow.Setter"/> writes the value after the load of the element.</summary>
    Set,

    /// <summary>The <see cref="ElementTypeInfo.Create"/> factory reads the value through <see cref="ElementCreateArgs.TryGet"/>. The attribute is optional.</summary>
    Create,

    /// <summary>The factory reads the value, and the element needs the attribute. No attribute gives <see cref="TemplateErrorKind.MissingAttribute"/> at the element.</summary>
    CreateRequired,
}

/// <summary>
/// One row of the attribute table of an element type: the name, the kind, and, for a set row, the setter.
/// </summary>
/// <remarks>
/// A create row has no setter: the factory of the type reads its value. A <see cref="AttributeRowKind.CreateRequired"/>
/// row has no setter either, and the element needs the attribute.
/// </remarks>
public readonly struct AttributeRow
{
    /// <summary>Creates a create row or a required create row.</summary>
    /// <param name="name">The attribute name, for example <c>text</c>.</param>
    /// <param name="kind">The kind. <see cref="AttributeRowKind.Set"/> is not valid here: a set row needs a setter.</param>
    /// <exception cref="ArgumentNullException"><paramref name="name"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="kind"/> is <see cref="AttributeRowKind.Set"/>.</exception>
    public AttributeRow(string name, AttributeRowKind kind)
    {
        ArgumentNullException.ThrowIfNull(name);
        if (kind == AttributeRowKind.Set)
        {
            throw new ArgumentException(
                "A set row needs a setter. Use the AttributeRow(string, AttributeSetter) constructor.",
                nameof(kind));
        }

        Name = name;
        Kind = kind;
        Setter = null;
    }

    /// <summary>Creates a set row.</summary>
    /// <param name="name">The attribute name, for example <c>pickable</c>.</param>
    /// <param name="setter">The setter that writes the value to the element.</param>
    /// <remarks>
    /// The constructor does not check <paramref name="setter"/> for <see langword="null"/>. A set row with no setter is
    /// not valid, and <see cref="TemplateLoader.Register(in ElementTypeInfo)"/> rejects it (Decision: the check
    /// belongs to the table, not to the row).
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="name"/> is <see langword="null"/>.</exception>
    public AttributeRow(string name, AttributeSetter setter)
    {
        ArgumentNullException.ThrowIfNull(name);

        Name = name;
        Kind = AttributeRowKind.Set;
        Setter = setter;
    }

    /// <summary>Gets the attribute name, for example <c>scroll-offset</c>.</summary>
    public string Name { get; }

    /// <summary>Gets the kind of the row.</summary>
    public AttributeRowKind Kind { get; }

    /// <summary>Gets the setter. The value is <see langword="null"/> for a create row.</summary>
    public AttributeSetter? Setter { get; }
}

/// <summary>
/// The table entry of one element type: the XML name, the factory, the own rows, and the leaf flag.
/// </summary>
/// <remarks>
/// <para>
/// Pass the info to <see cref="TemplateLoader.Register(in ElementTypeInfo)"/>. The loader copies the
/// <see cref="Rows"/> array into its own
/// table, so a later change of the array changes nothing.
/// </para>
/// <para>
/// The four base rows (<c>name</c>, <c>class</c>, <c>pickable</c>, <c>focusable</c>) apply to every type. The loader
/// places them before the own rows, and an own row may not take the name of a base row.
/// </para>
/// </remarks>
public struct ElementTypeInfo
{
    /// <summary>The XML local name of the type, for example <c>Slider</c>. The compare is ordinal.</summary>
    public required string Name;

    /// <summary>The factory that makes an element from the create attributes.</summary>
    public required ElementFactory Create;

    /// <summary>The own rows of the type, in the order in which the loader runs them. <see langword="null"/> gives none.</summary>
    public AttributeRow[]? Rows;

    /// <summary>
    /// <see langword="true"/> when the type holds no child element. A child of a leaf type gives
    /// <see cref="TemplateErrorKind.UnexpectedChild"/>.
    /// </summary>
    public bool IsLeaf;
}

/// <summary>
/// One <see cref="ElementTypeInfo"/> as the loader stores it: the own rows merged after the base rows, with the
/// name lookup and the message text ready.
/// </summary>
internal sealed class RegisteredType
{
    /// <summary>Builds the stored entry for one type.</summary>
    /// <param name="info">The type info. The method copies the own rows.</param>
    /// <param name="baseRows">The base rows that apply to every type.</param>
    internal RegisteredType(in ElementTypeInfo info, AttributeRow[] baseRows)
    {
        Name = info.Name;
        Create = info.Create;
        IsLeaf = info.IsLeaf;

        AttributeRow[] ownRows = info.Rows ?? Array.Empty<AttributeRow>();
        Rows = new AttributeRow[baseRows.Length + ownRows.Length];
        baseRows.CopyTo(Rows, 0);
        ownRows.CopyTo(Rows, baseRows.Length);

        RowIndices = new Dictionary<string, int>(Rows.Length, StringComparer.Ordinal);
        List<int>? required = null;
        StringBuilder names = new();
        for (int i = 0; i < Rows.Length; i++)
        {
            string rowName = Rows[i].Name;

            // The base rows win a duplicate name. Register rejects a duplicate (task 4.1).
            if (!RowIndices.ContainsKey(rowName))
            {
                RowIndices[rowName] = i;
            }

            if (Rows[i].Kind == AttributeRowKind.CreateRequired)
            {
                required ??= [];
                required.Add(i);
            }

            if (i > 0)
            {
                names.Append(", ");
            }

            names.Append(rowName);
        }

        RequiredIndices = required?.ToArray() ?? [];
        AttributeNames = names.ToString();
    }

    /// <summary>Gets the XML local name of the type.</summary>
    internal string Name { get; }

    /// <summary>Gets the factory of the type.</summary>
    internal ElementFactory Create { get; }

    /// <summary>Gets the leaf flag.</summary>
    internal bool IsLeaf { get; }

    /// <summary>Gets the rows: the base rows, then the own rows, in the order in which the loader runs them.</summary>
    internal AttributeRow[] Rows { get; }

    /// <summary>Gets the map from a row name to its index in <see cref="Rows"/>. The compare is ordinal.</summary>
    internal Dictionary<string, int> RowIndices { get; }

    /// <summary>Gets the indices of the <see cref="AttributeRowKind.CreateRequired"/> rows, in table order.</summary>
    internal int[] RequiredIndices { get; }

    /// <summary>Gets the row names, joined with <c>", "</c>, in table order, for an <see cref="TemplateErrorKind.UnknownAttribute"/> message.</summary>
    internal string AttributeNames { get; }

    /// <summary>Gives the index of the row with this name, or -1 when the type holds no such row.</summary>
    /// <param name="name">The attribute name.</param>
    /// <returns>The index in <see cref="Rows"/>, or -1.</returns>
    internal int FindRowIndex(string name)
    {
        return RowIndices.TryGetValue(name, out int index) ? index : -1;
    }
}
