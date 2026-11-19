using BrutalUi.Core;
using BrutalUi.Markup;

namespace HelloPanel;

/// <summary>
/// The state row of the XML state panel: a custom element type with no own property. The type adds one class, and
/// the sheet rules style the row and its children.
/// </summary>
/// <remarks>
/// <para>
/// <strong>The generator writes the rows.</strong> The attribute name gives the XML element name <c>ui:HelloRow</c>.
/// <c>BrutalUi.Generators</c> reads the marked types of this assembly and writes the register method
/// <c>RegisterHelloPanelElements</c>: its name follows the assembly name. The load code calls it on the template
/// loader before the load. The class has no own property, so the generator writes no rows for it (Q3).
/// </para>
/// <para>
/// <strong>The CSS rules match by class.</strong> The constructor adds the <c>hello-row</c> class, so the rules
/// <c>.hello-row</c> and <c>.hello-row &gt; Label</c> of <c>HelloScreen.css</c> match this type by class. The old
/// C# row carried the same class, so the draw does not change.
/// </para>
/// <para>
/// <strong>A game with its own marked types.</strong> Such a game references <c>BrutalUi.Markup</c> and adds the
/// generator analyzer line to its project file, because the packed Framework package excludes the Markup
/// analyzers (A7, binding-attributes Notes item 7).
/// </para>
/// </remarks>
[TemplateElement("HelloRow")]
internal sealed class HelloRow : Element
{
    /// <summary>The class of the row: the class of the <c>.hello-row</c> rules of the sheet.</summary>
    private static readonly ClassId RowClass = ClassName.Intern("hello-row");

    /// <summary>Creates the row and adds the <c>hello-row</c> class.</summary>
    public HelloRow()
    {
        Classes.Add(RowClass);
    }
}
