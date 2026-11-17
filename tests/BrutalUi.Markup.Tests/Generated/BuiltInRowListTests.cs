using System;
using System.Text;
using BrutalUi.Core;
using Xunit;

namespace BrutalUi.Markup.Tests.Generated;

/// <summary>
/// Pins the row list of each built-in element type: the names, the kinds, the order, and the leaf flag.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Why the list matters.</strong> The loader writes the row names of a type into one
/// <see cref="TemplateErrorKind.UnknownAttribute"/> message, in table order. One test reads that message and compares
/// the whole list, so a new row, a dropped row, and a moved row each fail the test. The element-generator feature
/// replaces the written table with a generated one, and these tests must pass before and after the replacement.
/// </para>
/// <para>
/// <strong>The list.</strong> Each type holds the four base rows first: <c>name</c>, <c>class</c>, <c>pickable</c>,
/// and <c>focusable</c>. Then the own rows follow: the create rows first, in the order of the factory attributes, and
/// then the set rows, from the base type nearest to <see cref="Element"/> down to the type itself.
/// </para>
/// <para>
/// <strong>The child rule.</strong> Each test also loads one <c>&lt;ui:Element /&gt;</c> child inside the type.
/// <c>Label</c> and <c>Image</c> are leaves, so the child gives <see cref="TemplateErrorKind.UnexpectedChild"/>. The
/// other six types take the child.
/// </para>
/// </remarks>
public sealed class BuiltInRowListTests
{
    /// <summary>The <see cref="Element"/> rows: the four base rows only, and the type takes a child.</summary>
    [Fact]
    public void TheRowsOfElementStayTheSame()
    {
        AssertRowList("Element", string.Empty, "name, class, pickable, focusable");
        AssertTakesAChild("Element", string.Empty);
    }

    /// <summary>The <see cref="Label"/> rows, and the leaf rule that rejects a child.</summary>
    [Fact]
    public void TheRowsOfLabelStayTheSame()
    {
        AssertRowList("Label", string.Empty, "name, class, pickable, focusable, text, text-key");
        AssertRejectsAChild("Label", string.Empty);
    }

    /// <summary>The <see cref="Button"/> rows, and the rule that a button takes a child.</summary>
    [Fact]
    public void TheRowsOfButtonStayTheSame()
    {
        AssertRowList("Button", string.Empty, "name, class, pickable, focusable, text, text-key");
        AssertTakesAChild("Button", string.Empty);
    }

    /// <summary>The <see cref="Toggle"/> rows: one set row, <c>checked</c>.</summary>
    [Fact]
    public void TheRowsOfToggleStayTheSame()
    {
        AssertRowList("Toggle", string.Empty, "name, class, pickable, focusable, checked");
        AssertTakesAChild("Toggle", string.Empty);
    }

    /// <summary>The <see cref="Slider"/> rows: three create rows, then the set row <c>value</c>.</summary>
    [Fact]
    public void TheRowsOfSliderStayTheSame()
    {
        AssertRowList("Slider", " max=\"10\"", "name, class, pickable, focusable, min, max, step, value");
        AssertTakesAChild("Slider", " max=\"10\"");
    }

    /// <summary>The <see cref="TextField"/> rows: one required create row and two text create rows.</summary>
    [Fact]
    public void TheRowsOfTextFieldStayTheSame()
    {
        AssertRowList("TextField", " capacity=\"8\"", "name, class, pickable, focusable, capacity, text, text-key");
        AssertTakesAChild("TextField", " capacity=\"8\"");
    }

    /// <summary>The <see cref="ScrollView"/> rows: one create row and one set row.</summary>
    [Fact]
    public void TheRowsOfScrollViewStayTheSame()
    {
        AssertRowList("ScrollView", string.Empty, "name, class, pickable, focusable, scroll-speed, scroll-offset");
        AssertTakesAChild("ScrollView", string.Empty);
    }

    /// <summary>The <see cref="Image"/> rows: four set rows, and the leaf rule that rejects a child.</summary>
    [Fact]
    public void TheRowsOfImageStayTheSame()
    {
        AssertRowList("Image", string.Empty, "name, class, pickable, focusable, texture, uv, tint, scale-mode");
        AssertRejectsAChild("Image", string.Empty);
    }

    /// <summary>Loads one unknown <c>probe</c> attribute, and compares the whole row list of the type.</summary>
    /// <remarks>
    /// The loader reports an unknown attribute after the factory runs, so the template holds the required create
    /// attribute of the type. The message lists every row name of the type, in table order, and ends with a point.
    /// </remarks>
    /// <param name="xmlName">The XML name of the type.</param>
    /// <param name="required">The required create attribute of the type, with its leading space, or empty.</param>
    /// <param name="rows">The row names of the type, joined with <c>", "</c>, in table order.</param>
    private static void AssertRowList(string xmlName, string required, string rows)
    {
        TemplateLoader loader = new();
        string template = $"""<ui:{xmlName} xmlns:ui="brutal-ui"{required} probe="1" />""";

        TemplateError error = Fault(loader, template);
        Assert.Equal(TemplateErrorKind.UnknownAttribute, error.Kind);
        AssertAttribute(error, template, "probe");
        Assert.Contains($"The attributes of {xmlName} are {rows}.", error.Message);
    }

    /// <summary>Loads one <c>Label</c> or <c>Image</c> type with one <see cref="Element"/> child, and expects the
    /// <see cref="TemplateErrorKind.UnexpectedChild"/> fault.</summary>
    /// <param name="xmlName">The XML name of the leaf type.</param>
    /// <param name="required">The required create attribute of the type, with its leading space, or empty.</param>
    private static void AssertRejectsAChild(string xmlName, string required)
    {
        TemplateLoader loader = new();
        string template = $"""<ui:{xmlName} xmlns:ui="brutal-ui"{required}><ui:Element /></ui:{xmlName}>""";

        TemplateError error = Fault(loader, template);
        Assert.Equal(TemplateErrorKind.UnexpectedChild, error.Kind);
        AssertElement(error, template, "ui:Element");
        Assert.Contains($"cannot be a child of {xmlName}", error.Message);
    }

    /// <summary>Loads one type with one <see cref="Element"/> child, and expects the child on the tree.</summary>
    /// <param name="xmlName">The XML name of the type.</param>
    /// <param name="required">The required create attribute of the type, with its leading space, or empty.</param>
    private static void AssertTakesAChild(string xmlName, string required)
    {
        TemplateLoader loader = new();
        string template = $"""<ui:{xmlName} xmlns:ui="brutal-ui"{required}><ui:Element /></ui:{xmlName}>""";

        Element root = loader.Load(Bytes(template), "rows.xml").GetRootOrThrow();
        Assert.Equal(1, root.Children.Count);
    }

    /// <summary>Loads one bad template and gives its one error. The root is <see langword="null"/>.</summary>
    private static TemplateError Fault(TemplateLoader loader, string template)
    {
        TemplateLoadResult result = loader.Load(Bytes(template), "rows.xml");
        Assert.Null(result.Root);
        return Assert.Single(result.Errors);
    }

    /// <summary>Encodes one template text to UTF-8 bytes for the span overload.</summary>
    private static byte[] Bytes(string template) => Encoding.UTF8.GetBytes(template);

    /// <summary>Checks the line and the column of an attribute fault against the template text.</summary>
    private static void AssertAttribute(TemplateError error, string template, string attributeName)
    {
        int at = template.IndexOf($"{attributeName}=\"", StringComparison.Ordinal);
        Assert.True(at >= 0, $"The template holds no {attributeName} attribute.");
        AssertPosition(error, template, at);
    }

    /// <summary>Checks the line and the column of an element fault against the template text.</summary>
    private static void AssertElement(TemplateError error, string template, string qualifiedName)
    {
        int at = template.IndexOf(qualifiedName.AsSpan(), StringComparison.Ordinal);
        Assert.True(at >= 0, $"The template holds no {qualifiedName} element.");
        AssertPosition(error, template, at);
    }

    /// <summary>Checks one position against the index of the fault text in the template.</summary>
    private static void AssertPosition(TemplateError error, string template, int index)
    {
        int line = 1;
        int lineStart = 0;
        for (int i = 0; i < index; i++)
        {
            if (template[i] == '\n')
            {
                line++;
                lineStart = i + 1;
            }
        }

        Assert.Equal(line, error.Line);
        Assert.Equal(index - lineStart + 1, error.Column);
    }
}
