using System;
using System.Text;
using BrutalUi.Core;
using BrutalUi.Core.Tests;
using Xunit;

namespace BrutalUi.Markup.Tests.Template;

/// <summary>
/// The positioned errors of A6, A7, and A8: the namespace rejection, the text node, the leaf child, the full
/// valid-names messages, the stable error sort, and the <see cref="System.Xml.XmlException"/> rules.
/// </summary>
/// <remarks>
/// <para>
/// Each test uses the public API of <see cref="TemplateLoader"/> and <see cref="TreeCompare"/> only. Each message
/// check uses <c>Contains</c> on the quoted text, because the plan fixes the words of some messages only.
/// </para>
/// <para>
/// The line and the column come from the template text of the test: an element error sits at the first character of
/// the qualified name, after the <c>&lt;</c>, and an attribute error at the first character of the attribute name.
/// </para>
/// </remarks>
public sealed class TemplateErrorTests
{
    /// <summary>An unknown element gives <see cref="TemplateErrorKind.UnknownElement"/> at its name, and the message lists the types.</summary>
    [Fact]
    public void AnUnknownElementIsAnErrorAtItsName()
    {
        TemplateLoader loader = new();
        string template = """<ui:Panel xmlns:ui="brutal-ui" />""";
        TemplateError error = Fault(loader, template);

        AssertElement(error, template, "ui:Panel");
        Assert.Equal(TemplateErrorKind.UnknownElement, error.Kind);
        Assert.Contains("Element", error.Message);
    }

    /// <summary>An element outside the <c>brutal-ui</c> namespace gives <see cref="TemplateErrorKind.UnknownElement"/>.</summary>
    [Fact]
    public void AnElementOutsideTheNamespaceIsAnError()
    {
        TemplateLoader loader = new();

        string none = """<Label />""";
        TemplateError noNamespace = Fault(loader, none);
        AssertElement(noNamespace, none, "Label");
        Assert.Equal(TemplateErrorKind.UnknownElement, noNamespace.Kind);
        Assert.Contains("brutal-ui", noNamespace.Message);

        string other = """<x:Label xmlns:x="other" />""";
        TemplateError otherNamespace = Fault(loader, other);
        AssertElement(otherNamespace, other, "x:Label");
        Assert.Equal(TemplateErrorKind.UnknownElement, otherNamespace.Kind);
        Assert.Contains("brutal-ui", otherNamespace.Message);
    }

    /// <summary>A prefixed attribute gives <see cref="TemplateErrorKind.UnknownAttribute"/>, also with the <c>ui:</c> prefix.</summary>
    [Fact]
    public void AnAttributeOutsideTheNamespaceIsAnError()
    {
        TemplateLoader loader = new();

        string other = """<ui:Element xmlns:ui="brutal-ui" xmlns:x="other" x:name="a" />""";
        TemplateError prefixedOther = Fault(loader, other);
        AssertPrefixedAttribute(prefixedOther, other, "x:name");
        Assert.Equal(TemplateErrorKind.UnknownAttribute, prefixedOther.Kind);

        string brutal = """<ui:Element xmlns:ui="brutal-ui" ui:name="a" />""";
        TemplateError prefixedBrutal = Fault(loader, brutal);
        AssertPrefixedAttribute(prefixedBrutal, brutal, "ui:name");
        Assert.Equal(TemplateErrorKind.UnknownAttribute, prefixedBrutal.Kind);
    }

    /// <summary>An attribute that belongs to another type gives <see cref="TemplateErrorKind.UnknownAttribute"/> and lists the
    /// valid names. A character outside the Basic Multilingual Plane before the attribute shows the column unit: UTF-16.</summary>
    [Fact]
    public void AnAttributeOfAnotherTypeIsAnUnknownAttribute()
    {
        TemplateLoader loader = new();
        string template = """<ui:Label xmlns:ui="brutal-ui" checked="true" />""";
        TemplateError error = Fault(loader, template);

        AssertAttribute(error, template, "checked");
        Assert.Equal(TemplateErrorKind.UnknownAttribute, error.Kind);
        Assert.Contains("'checked'", error.Message);
        Assert.Contains("focusable", error.Message);

        // The emoji takes 2 UTF-16 units. `nope` starts at UTF-16 index 41, so the reader must give column 42.
        // A rune-based count would give 41, so these asserts pin the UTF-16 rule of `TemplateError.Column`.
        string astral = """<ui:Label xmlns:ui="brutal-ui" text="😀" nope="1" />""";
        TemplateError utf16Fault = Fault(loader, astral);

        Assert.Equal(TemplateErrorKind.UnknownAttribute, utf16Fault.Kind);
        AssertAttribute(utf16Fault, astral, "nope");
        Assert.Equal(42, utf16Fault.Column);
    }

    /// <summary>Text and CDATA between elements give <see cref="TemplateErrorKind.UnexpectedText"/>. Whitespace, a comment, and a processing instruction give none.</summary>
    [Fact]
    public void TextBetweenElementsIsAnError()
    {
        TemplateLoader loader = new();

        string text = """<ui:Element xmlns:ui="brutal-ui">Hello</ui:Element>""";
        TemplateError textFault = Fault(loader, text);
        AssertPosition(textFault, text, text.IndexOf("Hello", StringComparison.Ordinal));
        Assert.Equal(TemplateErrorKind.UnexpectedText, textFault.Kind);
        Assert.Contains("'Hello'", textFault.Message);

        string cdata = """<ui:Element xmlns:ui="brutal-ui"><![CDATA[Hi]]></ui:Element>""";
        TemplateError cdataFault = Fault(loader, cdata);
        Assert.Equal(TemplateErrorKind.UnexpectedText, cdataFault.Kind);
        Assert.Contains("'Hi'", cdataFault.Message);

        string clean = """<?pi go?>""" + Environment.NewLine
            + """<ui:Element xmlns:ui="brutal-ui">""" + "\n"
            + "  <!-- a comment -->\n"
            + "  <ui:Label text=\"a\" />\n"
            + "</ui:Element>";
        TemplateLoadResult ok = loader.Load(Bytes(clean), "text.xml");
        Assert.True(ok.Succeeded);
        Assert.NotNull(ok.Root);
    }

    /// <summary>A child element inside a leaf type gives <see cref="TemplateErrorKind.UnexpectedChild"/> at the child name.</summary>
    [Fact]
    public void AChildInsideALabelOrAnImageIsAnError()
    {
        TemplateLoader loader = new();

        string inLabel = """<ui:Label xmlns:ui="brutal-ui"><ui:Element /></ui:Label>""";
        TemplateError labelFault = Fault(loader, inLabel);
        AssertElement(labelFault, inLabel, "ui:Element");
        Assert.Equal(TemplateErrorKind.UnexpectedChild, labelFault.Kind);

        string inImage = """<ui:Image xmlns:ui="brutal-ui"><ui:Element /></ui:Image>""";
        TemplateError imageFault = Fault(loader, inImage);
        AssertElement(imageFault, inImage, "ui:Element");
        Assert.Equal(TemplateErrorKind.UnexpectedChild, imageFault.Kind);
    }

    /// <summary>The reader records each error and keeps reading. The errors come in the source order, and the root is null.</summary>
    [Fact]
    public void TheLoaderRecordsEachErrorAndContinues()
    {
        TemplateLoader loader = new();
        string template =
            """<ui:Element xmlns:ui="brutal-ui">""" + "\n"
            + """  <ui:Label text="a" bad="1" />""" + "\n"
            + """  <ui:Slider max="5" value="x" />""" + "\n"
            + """  <ui:Panel>""" + "\n"
            + """    <ui:Label nope="1" />""" + "\n"
            + """  </ui:Panel>""" + "\n"
            + "</ui:Element>";

        TemplateLoadResult result = loader.Load(Bytes(template), "many.xml");
        Assert.Null(result.Root);
        Assert.Equal(4, result.Errors.Count);

        Assert.Equal(TemplateErrorKind.UnknownAttribute, result.Errors[0].Kind);
        Assert.Equal(2, result.Errors[0].Line);
        Assert.Equal(TemplateErrorKind.InvalidValue, result.Errors[1].Kind);
        Assert.Equal(3, result.Errors[1].Line);
        Assert.Equal(TemplateErrorKind.UnknownElement, result.Errors[2].Kind);
        Assert.Equal(4, result.Errors[2].Line);
        Assert.Equal(TemplateErrorKind.UnknownAttribute, result.Errors[3].Kind);
        Assert.Equal(5, result.Errors[3].Line);
    }

    /// <summary>XML that is not well formed stops the read. The loader keeps the earlier errors and adds no error after the stop.</summary>
    [Fact]
    public void XmlThatIsNotWellFormedStopsTheRead()
    {
        TemplateLoader loader = new();
        string template =
            """<ui:Element xmlns:ui="brutal-ui">""" + "\n"
            + """  <ui:Label bad="1" text="a" />""" + "\n"
            + "</ui:Wrongs>";

        TemplateLoadResult result = loader.Load(Bytes(template), "stop.xml");
        Assert.Null(result.Root);
        Assert.Equal(2, result.Errors.Count);
        Assert.Equal(TemplateErrorKind.UnknownAttribute, result.Errors[0].Kind);
        Assert.Equal(2, result.Errors[0].Line);
        Assert.Equal(TemplateErrorKind.NotWellFormed, result.Errors[1].Kind);
        Assert.Contains("not well-formed", result.Errors[1].Message);

        // The wrong end tag sits on line 3, and the reader reports its name start: line 3, column 3.
        Assert.Equal(3, result.Errors[1].Line);
        Assert.Equal(3, result.Errors[1].Column);
    }

    /// <summary>The loader prohibits a DTD. A document type with an entity gives one error, and no entity text reaches a label.</summary>
    [Fact]
    public void ADtdIsAnError()
    {
        TemplateLoader loader = new();
        string template =
            """<!DOCTYPE root [<!ENTITY e "boom">]>""" + "\n"
            + """<ui:Element xmlns:ui="brutal-ui"><ui:Label text="&e;" /></ui:Element>""";

        TemplateLoadResult result = loader.Load(Bytes(template), "dtd.xml");
        Assert.Null(result.Root);
        TemplateError error = Assert.Single(result.Errors);
        Assert.Equal(TemplateErrorKind.NotWellFormed, error.Kind);

        // The DTD itself is the fault: the error sits on line 1, before the reader reaches the element on line 2.
        Assert.Equal(1, error.Line);
        Assert.True(error.Column >= 1);
    }

    /// <summary>An empty template gives one <see cref="TemplateErrorKind.NotWellFormed"/> error and no root.</summary>
    [Fact]
    public void AnEmptyTemplateIsAnError()
    {
        TemplateLoadResult result = new TemplateLoader().Load(""u8, "empty.xml");

        Assert.Null(result.Root);
        TemplateError error = Assert.Single(result.Errors);
        Assert.Equal(TemplateErrorKind.NotWellFormed, error.Kind);
        Assert.True(error.Line >= 1);
        Assert.True(error.Column >= 1);
    }

    /// <summary>The default-namespace form loads and equals the prefixed form.</summary>
    [Fact]
    public void TheDefaultNamespaceFormLoads()
    {
        TemplateLoader loader = new();
        string plain = """<Element xmlns="brutal-ui"><Label text="a" /></Element>""";
        string prefixed = """<ui:Element xmlns:ui="brutal-ui"><ui:Label text="a" /></ui:Element>""";

        Element plainRoot = loader.Load(Bytes(plain), "ns.xml").GetRootOrThrow();
        Element prefixedRoot = loader.Load(Bytes(prefixed), "ns.xml").GetRootOrThrow();

        Assert.IsType<Element>(plainRoot);
        Assert.IsType<Element>(prefixedRoot);
        Assert.Null(TreeCompare.FirstDifference(plainRoot, prefixedRoot));
    }

    /// <summary><see cref="TemplateError.ToString"/> gives the source, the position, and the kind. <see cref="TemplateLoadResult.GetRootOrThrow"/> lists each error on its own line.</summary>
    [Fact]
    public void AnErrorGivesItsTextAndGetRootOrThrowListsEachError()
    {
        TemplateLoader loader = new();
        string template =
            """<ui:Element xmlns:ui="brutal-ui">""" + "\n"
            + """   <ui:Label nope="1" />""" + "\n"
            + """   <ui:Label bad="2" />""" + "\n"
            + "</ui:Element>";

        TemplateLoadResult result = loader.Load(Bytes(template), "hud.xml");
        Assert.Equal(2, result.Errors.Count);
        Assert.StartsWith("hud.xml(2,14): UnknownAttribute: ", result.Errors[0].ToString());
        Assert.Equal(3, result.Errors[1].Line);
        Assert.Equal(14, result.Errors[1].Column);

        InvalidOperationException error = Assert.Throws<InvalidOperationException>(() => result.GetRootOrThrow());
        string[] lines = error.Message.Split('\n');
        Assert.Equal(2, lines.Length);
        Assert.Equal(result.Errors[0].ToString(), lines[0]);
        Assert.Equal(result.Errors[1].ToString(), lines[1]);
    }

    /// <summary>Loads one bad template and gives its one error. The root is <see langword="null"/>.</summary>
    private static TemplateError Fault(TemplateLoader loader, string template)
    {
        TemplateLoadResult result = loader.Load(Bytes(template), "errors.xml");
        Assert.Null(result.Root);
        return Assert.Single(result.Errors);
    }

    /// <summary>Checks the line and the column of an attribute fault against the template text.</summary>
    private static void AssertAttribute(TemplateError error, string template, string attributeName)
    {
        int at = template.IndexOf($"{attributeName}=\"", StringComparison.Ordinal);
        Assert.True(at >= 0, $"The template holds no {attributeName} attribute.");
        AssertPosition(error, template, at);
    }

    /// <summary>Checks the line and the column of a prefixed-attribute fault against the template text.</summary>
    private static void AssertPrefixedAttribute(TemplateError error, string template, string qualifiedName)
    {
        int at = template.IndexOf($"{qualifiedName}=\"", StringComparison.Ordinal);
        Assert.True(at >= 0, $"The template holds no {qualifiedName} attribute.");
        AssertPosition(error, template, at);
    }

    /// <summary>Checks the line and the column of an element fault against the template text.</summary>
    /// <remarks>The element position is the first character of the qualified name, after the <c>&lt;</c>.</remarks>
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

    /// <summary>Encodes one template text to UTF-8 bytes for the span overload.</summary>
    private static byte[] Bytes(string template) => Encoding.UTF8.GetBytes(template);
}
