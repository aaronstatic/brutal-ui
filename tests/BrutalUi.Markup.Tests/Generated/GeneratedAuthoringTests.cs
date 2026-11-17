using System;
using System.Text;
using BrutalUi.Core;
using Xunit;

namespace BrutalUi.Markup.Tests.Generated;

/// <summary>
/// The phase 3 authoring-surface tests: the source generator reads <c>Target</c>, the factory and its create
/// rows, the <c>int</c> and enum property kinds, <c>[TemplateRowName]</c>, and <c>[TemplateIgnore]</c>, and the
/// loader loads the result like a written table entry.
/// </summary>
/// <remarks>
/// <para>
/// The tests use the same generated API as <see cref="GeneratedElementTests"/>:
/// <c>loader.RegisterBrutalUiMarkupTestsElements()</c>. The XML names <c>Meter</c> and <c>Dial</c> are unique in
/// this assembly. <see cref="Meter"/> shows the game form with a factory; <c>DialDeclaration</c> shows the
/// declaration form with <c>Target</c> and no factory.
/// </para>
/// <para>
/// The create rows come before the set rows in the table: <c>segments, offset, style, lvl</c>. The
/// <see cref="TemplateErrorKind.UnknownAttribute"/> message pins that order.
/// </para>
/// </remarks>
public sealed class GeneratedAuthoringTests
{
    /// <summary>The factory type loads with its create rows: <c>segments</c> reaches the constructor, and the
    /// renamed <c>lvl</c> set row follows the factory, even when its attribute stands first in the source.</summary>
    [Fact]
    public void AFactoryTypeLoadsWithItsCreateRows()
    {
        TemplateLoader loader = NewLoaderWithGeneratedTypes();

        string template = """<ui:Meter xmlns:ui="brutal-ui" lvl="0.5" segments="4" />""";
        Meter meter = Assert.IsType<Meter>(loader.Load(Utf8(template), "authoring.xml").GetRootOrThrow());
        Assert.Equal(4, meter.Segments);
        Assert.Equal(0.5f, meter.Level);
    }

    /// <summary>A required create row with no attribute gives one <see cref="TemplateErrorKind.MissingAttribute"/>
    /// at the element name, and the factory never runs.</summary>
    [Fact]
    public void AMissingRequiredCreateRowIsAnError()
    {
        TemplateLoader loader = NewLoaderWithGeneratedTypes();

        string template = """<ui:Meter xmlns:ui="brutal-ui" lvl="0.5" />""";
        TemplateError error = Fault(loader, template);
        Assert.Equal(TemplateErrorKind.MissingAttribute, error.Kind);
        AssertElement(error, template, "ui:Meter");
        Assert.Contains("'segments'", error.Message);
    }

    /// <summary>An <c>int</c> property gives a row that reads 0 and a negative number.</summary>
    [Fact]
    public void AnIntRowTakesZeroAndANegativeNumber()
    {
        TemplateLoader loader = NewLoaderWithGeneratedTypes();

        (string Text, int Expected)[] cases = [("0", 0), ("-3", -3)];
        foreach ((string text, int expected) in cases)
        {
            string template = $"""<ui:Meter xmlns:ui="brutal-ui" segments="1" offset="{text}" />""";
            TemplateLoadResult result = loader.Load(Utf8(template), "authoring.xml");
            Meter meter = Assert.IsType<Meter>(result.GetRootOrThrow());
            Assert.Empty(result.Errors);
            Assert.Equal(expected, meter.Offset);
        }
    }

    /// <summary>An enum property gives a keyword row: the kebab name reads, and the member name does not.</summary>
    [Fact]
    public void AnEnumRowTakesAKebabKeyword()
    {
        TemplateLoader loader = NewLoaderWithGeneratedTypes();

        string good = """<ui:Meter xmlns:ui="brutal-ui" segments="1" style="round-dial" />""";
        Meter meter = Assert.IsType<Meter>(loader.Load(Utf8(good), "authoring.xml").GetRootOrThrow());
        Assert.Equal(MeterStyle.RoundDial, meter.Style);

        string bad = """<ui:Meter xmlns:ui="brutal-ui" segments="1" style="RoundDial" />""";
        TemplateError error = Fault(loader, bad);
        Assert.Equal(TemplateErrorKind.InvalidValue, error.Kind);
        AssertAttribute(error, bad, "style");
        Assert.Contains("'RoundDial'", error.Message);
        Assert.Contains("is not a value of style", error.Message);
    }

    /// <summary><c>[TemplateRowName]</c> moves the row, and <c>[TemplateIgnore]</c> removes it: the old CLR names
    /// each give an <see cref="TemplateErrorKind.UnknownAttribute"/>.</summary>
    [Fact]
    public void ARenamedRowAndAnIgnoredPropertyFollowTheirAttributes()
    {
        TemplateLoader loader = NewLoaderWithGeneratedTypes();

        foreach (string name in new[] { "level", "hidden" })
        {
            string template = $"""<ui:Meter xmlns:ui="brutal-ui" segments="1" {name}="1" />""";
            TemplateError error = Fault(loader, template);
            Assert.Equal(TemplateErrorKind.UnknownAttribute, error.Kind);
            AssertAttribute(error, template, name);
            Assert.Contains($"'{name}'", error.Message);
            Assert.Contains("name, class, pickable, focusable, segments, offset, style, lvl", error.Message);
        }
    }

    /// <summary>A declaration class with <c>Target</c> and no factory gives the rows of its target type, and the
    /// generated create calls the target's no-parameter constructor.</summary>
    [Fact]
    public void ADeclarationClassGivesTheRowsOfItsTarget()
    {
        TemplateLoader loader = NewLoaderWithGeneratedTypes();

        string template = """<ui:Dial xmlns:ui="brutal-ui" angle="90" />""";
        Dial dial = Assert.IsType<Dial>(loader.Load(Utf8(template), "authoring.xml").GetRootOrThrow());
        Assert.Equal(90f, dial.Angle);
    }

    /// <summary>Creates a loader and registers the generated types of this assembly.</summary>
    /// <returns>The loader with the built-in types and the generated types.</returns>
    private static TemplateLoader NewLoaderWithGeneratedTypes()
    {
        TemplateLoader loader = new();
        loader.RegisterBrutalUiMarkupTestsElements();
        return loader;
    }

    /// <summary>Loads one bad template and gives its one error. The root is <see langword="null"/>.</summary>
    private static TemplateError Fault(TemplateLoader loader, string template)
    {
        TemplateLoadResult result = loader.Load(Utf8(template), "authoring.xml");
        Assert.Null(result.Root);
        return Assert.Single(result.Errors);
    }

    /// <summary>Encodes one template text to UTF-8 bytes for the span overload.</summary>
    private static byte[] Utf8(string template) => Encoding.UTF8.GetBytes(template);

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

/// <summary>The style of a <see cref="Meter"/>: the generator gives each member a kebab-case keyword.</summary>
internal enum MeterStyle
{
    /// <summary>A straight bar.</summary>
    Bar,

    /// <summary>A round dial.</summary>
    RoundDial,
}

/// <summary>A marked factory type: the constructor takes the segment count, and the factory reads the
/// <c>segments</c> create row.</summary>
[TemplateElement("Meter")]
internal sealed class Meter : Element
{
    /// <summary>Creates a meter with its segment count.</summary>
    /// <param name="segments">The number of segments.</param>
    public Meter(int segments)
    {
        Segments = segments;
    }

    /// <summary>Gets the segment count that the factory read. A get-only property gives no row.</summary>
    public int Segments { get; }

    /// <summary>Makes a meter from the create attributes.</summary>
    /// <param name="args">The create arguments. The factory reads <c>segments</c> through them.</param>
    /// <returns>The new meter.</returns>
    [TemplateFactory]
    [TemplateCreateRow("segments", IsRequired = true)]
    internal static Meter Create(ElementCreateArgs args)
    {
        int segments = 1;
        if (args.TryGet("segments", out AttributeContext context)
            && context.TryReadWholeNumber(out int read))
        {
            segments = read;
        }

        return new Meter(segments);
    }

    /// <summary>Gets or sets the offset (integer read).</summary>
    public int Offset { get; set; }

    /// <summary>Gets or sets the style (keyword read). The <c>new</c> hides the get-only <c>Element.Style</c>;
    /// the walk stops before <c>Element</c>, so only this property gives the <c>style</c> row.</summary>
    public new MeterStyle Style { get; set; }

    /// <summary>Gets or sets the level. The row name comes from the attribute, not from the kebab rule.</summary>
    [TemplateRowName("lvl")]
    public float Level { get; set; }

    /// <summary>Gets or sets a value with no row.</summary>
    [TemplateIgnore]
    public float Hidden { get; set; }
}

/// <summary>An unmarked element type: <c>DialDeclaration</c> declares its rows.</summary>
internal sealed class Dial : Element
{
    /// <summary>Gets or sets the angle.</summary>
    public float Angle { get; set; }
}

/// <summary>A declaration class with no factory: the generator writes the no-parameter constructor call of the
/// target and walks the target for its rows.</summary>
[TemplateElement("Dial", Target = typeof(Dial))]
internal static class DialDeclaration
{
}
