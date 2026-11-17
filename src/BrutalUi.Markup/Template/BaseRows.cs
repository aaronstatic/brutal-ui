using System;
using BrutalUi.Core;

namespace BrutalUi.Markup;

/// <summary>
/// The four base rows of the element table: <c>name</c>, <c>class</c>, <c>pickable</c>, and <c>focusable</c>.
/// </summary>
/// <remarks>
/// <para>
/// The loader places these rows before the own rows of each registered type, and runs them in this order. Every
/// type holds them, so a custom type and a built-in type read the same four attributes with the same code.
/// </para>
/// <para>
/// The generator writes no base row. These rows stay written code, because they read <see cref="Element"/> members
/// that every type shares. See the element-generator plan, Note 3.
/// </para>
/// </remarks>
internal static class BaseRows
{
    /// <summary>Gets the base rows. The loader places them before the own rows of each registered type.</summary>
    internal static readonly AttributeRow[] All =
    [
        new AttributeRow("name", static (Element element, AttributeContext context) => element.Name = context.Value),
        new AttributeRow("class", static (Element element, AttributeContext context) => AddClasses(element, context)),
        new AttributeRow("pickable", static (Element element, AttributeContext context) => SetPickable(element, context)),
        new AttributeRow("focusable", static (Element element, AttributeContext context) => SetFocusable(element, context)),
    ];

    /// <summary>Writes the <c>pickable</c> value of an element.</summary>
    /// <param name="element">The element to change.</param>
    /// <param name="context">The <c>pickable</c> attribute.</param>
    private static void SetPickable(Element element, AttributeContext context)
    {
        if (context.TryReadBool(out bool value))
        {
            element.IsPickable = value;
        }
    }

    /// <summary>Writes the <c>focusable</c> value of an element.</summary>
    /// <param name="element">The element to change.</param>
    /// <param name="context">The <c>focusable</c> attribute.</param>
    private static void SetFocusable(Element element, AttributeContext context)
    {
        if (context.TryReadBool(out bool value))
        {
            element.IsFocusable = value;
        }
    }

    /// <summary>Adds each class name of a class list, in the order of the value.</summary>
    /// <param name="element">The element to change.</param>
    /// <param name="context">The <c>class</c> attribute.</param>
    /// <remarks>
    /// The value holds names with XML whitespace between them. An empty value adds no class. A repeated name adds
    /// nothing, as <see cref="ClassList.Add"/> does.
    /// </remarks>
    private static void AddClasses(Element element, AttributeContext context)
    {
        ReadOnlySpan<char> rest = context.Value;
        while (TemplateValues.NextName(ref rest, out ReadOnlySpan<char> name))
        {
            element.Classes.Add(ClassName.Intern(name.ToString()));
        }
    }
}
