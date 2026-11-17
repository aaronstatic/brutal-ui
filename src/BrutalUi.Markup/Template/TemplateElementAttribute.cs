using System;

namespace BrutalUi.Markup;

/// <summary>
/// Marks an element class so the brutal-ui source generator writes its template rows.
/// </summary>
/// <remarks>
/// <para>
/// <strong>The game form.</strong> Mark your element class, which derives from
/// <see cref="global::BrutalUi.Core.Element"/>, and give it a constructor with no parameters. The generator walks
/// the public settable properties of a supported type and writes one set row for each. See the element-generator
/// plan for the supported types and the row rules.
/// </para>
/// <para>
/// <strong>The register call.</strong> The generator writes one method for the assembly, for example
/// <c>loader.RegisterMyGameElements()</c>. Call it after you create the <see cref="TemplateLoader"/>. It registers
/// every marked type of the assembly with one call.
/// </para>
/// <para>
/// <strong>Run time.</strong> The attribute does nothing at run time. The generator reads it at build time. A
/// build that skips the generator loads no marked type.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// [TemplateElement("Gauge")]
/// internal sealed class Gauge : Element
/// {
///     public float Level { get; set; }
/// }
/// </code>
/// A template then uses <c>&lt;ui:Gauge level="0.5" /&gt;</c>.
/// </example>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class TemplateElementAttribute : Attribute
{
    /// <summary>Creates the attribute with the XML name of the element.</summary>
    /// <param name="name">
    /// The XML local name of the element, for example <c>Gauge</c>. The loader maps that name to the type. The
    /// compare is ordinal.
    /// </param>
    /// <exception cref="ArgumentNullException"><paramref name="name"/> is <see langword="null"/>.</exception>
    public TemplateElementAttribute(string name)
    {
        ArgumentNullException.ThrowIfNull(name);
        Name = name;
    }

    /// <summary>Gets the XML local name of the element.</summary>
    public string Name { get; }

    /// <summary>
    /// Gets or sets a value indicating that the element holds no child element. The default is
    /// <see langword="false"/>. A child of a leaf type gives a <c>UnexpectedChild</c> error at load time.
    /// </summary>
    public bool IsLeaf { get; set; }

    /// <summary>
    /// Gets or sets the element type that this attribute describes. The default is <see langword="null"/>, which
    /// means the game form: the marked class is the element type.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>The declaration form.</strong> Set this property to an element type. The marked class is then a
    /// <c>static</c> class, and it holds the factory of the target type. The generator walks the target type for its
    /// properties and reads the marked class for the factory and its create rows. Use this form for an element type
    /// that you do not own, or to keep the declaration apart from the type.
    /// </para>
    /// <para>
    /// The target type is <see cref="global::BrutalUi.Core.Element"/> or a type that derives from it, and it is not
    /// an open generic type. The marked class and each containing class have <c>internal</c> or <c>public</c> access,
    /// so the generated class can reach them.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// [TemplateElement("Dial", Target = typeof(Dial))]
    /// internal static class DialDeclaration
    /// {
    /// }
    /// </code>
    /// The template then uses <c>&lt;ui:Dial angle="90" /&gt;</c> for the <c>Dial</c> type.
    /// </example>
    public Type? Target { get; set; }
}
