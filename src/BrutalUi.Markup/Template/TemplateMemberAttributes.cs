using System;

namespace BrutalUi.Markup;

/// <summary>
/// Marks the static factory method of a marked element type. The generator calls it in place of the constructor.
/// </summary>
/// <remarks>
/// <para>
/// <strong>The factory rule.</strong> Put the attribute on one method of the marked class. The method is
/// <c>static</c>, not generic, and <c>internal</c> or <c>public</c>. It takes one by-value
/// <see cref="ElementCreateArgs"/> parameter and returns <see cref="global::BrutalUi.Core.Element"/> or a type that
/// derives from it. A class holds one factory at most.
/// </para>
/// <para>
/// The generator calls the factory before it runs the set rows. Read each create row inside the call with
/// <see cref="ElementCreateArgs.TryGet"/>. With no factory, the generator calls the constructor with no parameters of
/// the target type.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// [TemplateElement("Meter")]
/// internal sealed class Meter : Element
/// {
///     [TemplateFactory]
///     internal static Meter? Create(ElementCreateArgs args)
///     {
///         ...
///     }
/// }
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
public sealed class TemplateFactoryAttribute : Attribute
{
}

/// <summary>
/// Names one create attribute that the factory of a marked element type reads. Put the attribute on the factory
/// method.
/// </summary>
/// <remarks>
/// <para>
/// <strong>The create-row rule.</strong> The attribute sits only on a method that holds
/// <see cref="TemplateFactoryAttribute"/>. Each create row comes before every set row, in the order of the attributes
/// in the source. The loader runs the factory first, so the factory reads these attributes and the set rows write the
/// properties after it.
/// </para>
/// <para>
/// The generator marks the row <see cref="AttributeRowKind.Create"/>, or
/// <see cref="AttributeRowKind.CreateRequired"/> when <see cref="IsRequired"/> is <see langword="true"/>. A required
/// row with no attribute gives <see cref="TemplateErrorKind.MissingAttribute"/> at the element name, and the loader
/// does not call the factory.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// [TemplateFactory]
/// [TemplateCreateRow("segments", IsRequired = true)]
/// internal static Meter? Create(ElementCreateArgs args)
/// {
///     ...
/// }
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = true, Inherited = false)]
public sealed class TemplateCreateRowAttribute : Attribute
{
    /// <summary>Creates the attribute with the name of one create row.</summary>
    /// <param name="name">
    /// The XML local name of the create attribute, for example <c>segments</c>. The factory reads it with
    /// <see cref="ElementCreateArgs.TryGet"/>. The compare is ordinal.
    /// </param>
    /// <exception cref="ArgumentNullException"><paramref name="name"/> is <see langword="null"/>.</exception>
    public TemplateCreateRowAttribute(string name)
    {
        ArgumentNullException.ThrowIfNull(name);
        Name = name;
    }

    /// <summary>Gets the XML local name of the create attribute.</summary>
    public string Name { get; }

    /// <summary>
    /// Gets or sets a value indicating that the element needs the attribute. The default is
    /// <see langword="false"/>. <see langword="true"/> gives a required create row.
    /// </summary>
    public bool IsRequired { get; set; }
}

/// <summary>
/// Gives one property of a marked element type another row name. The kebab rule of the property name no longer
/// applies.
/// </summary>
/// <remarks>
/// <para>
/// <strong>The row-name rule.</strong> Put the attribute on a property that gives a row: an instance property, not an
/// indexer, <c>public</c>, with a <c>public</c> set accessor that is not <c>init</c>, of a supported type. A name that
/// is not a valid XML local name, a name that another row of the type holds, and a base row name
/// (<c>name</c>, <c>class</c>, <c>pickable</c>, <c>focusable</c>) each give a build error.
/// </para>
/// <para>
/// The attribute on a property that gives no row gives a build error. The property keeps its place in the row order.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// [TemplateRowName("lvl")]
/// public float Level { get; set; }
/// </code>
/// The template then uses <c>lvl="0.5"</c>, and <c>level</c> is an unknown attribute.
/// </example>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
public sealed class TemplateRowNameAttribute : Attribute
{
    /// <summary>Creates the attribute with the row name of the property.</summary>
    /// <param name="name">
    /// The XML local name of the attribute, for example <c>lvl</c>. The loader maps that name to the property. The
    /// compare is ordinal.
    /// </param>
    /// <exception cref="ArgumentNullException"><paramref name="name"/> is <see langword="null"/>.</exception>
    public TemplateRowNameAttribute(string name)
    {
        ArgumentNullException.ThrowIfNull(name);
        Name = name;
    }

    /// <summary>Gets the XML local name of the row of the property.</summary>
    public string Name { get; }
}

/// <summary>
/// Keeps one property of a marked element type out of the attribute table. The property gives no row.
/// </summary>
/// <remarks>
/// <para>
/// <strong>The ignore rule.</strong> Put the attribute on a <c>public</c> settable property of a supported type that
/// the template must not write, for example a value that the game sets in code only. The loader reports
/// <see cref="TemplateErrorKind.UnknownAttribute"/> for its row name.
/// </para>
/// <para>
/// The generator skips the property before it applies the kebab rule, so no attribute name comes from it.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// [TemplateIgnore]
/// public float Hidden { get; set; }
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
public sealed class TemplateIgnoreAttribute : Attribute
{
}
