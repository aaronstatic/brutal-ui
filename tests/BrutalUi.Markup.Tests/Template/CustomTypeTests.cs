using System;
using System.Text;
using BrutalUi.Core;
using BrutalUi.Core.Tests;
using Xunit;

namespace BrutalUi.Markup.Tests.Template;

/// <summary>
/// The custom-type tests: a game registers a type with a factory and rows, <see cref="TemplateLoader.Register"/>
/// checks each entry, and each loader holds its own registry.
/// </summary>
/// <remarks>
/// <para>
/// Each test uses the public API of <see cref="TemplateLoader"/> and <see cref="TreeCompare"/> only. The line and
/// the column of an error come from the template text of the test: an attribute error sits at the first character
/// of the attribute name, and an element error at the first character of the qualified name after the
/// <c>&lt;</c>.
/// </para>
/// </remarks>
public sealed class CustomTypeTests
{
    /// <summary>A custom type loads with its factory and its rows, and the tree equals the builder tree.</summary>
    [Fact]
    public void ACustomTypeLoadsWithItsFactoryAndItsRows()
    {
        TemplateLoader loader = new();
        RegisterGauge(loader);

        string template = """<ui:Gauge xmlns:ui="brutal-ui" name="g" class="hud" segments="4" level="0.5" />""";
        Element actual = loader.Load(Bytes(template), "gauge.xml").GetRootOrThrow();
        Gauge gauge = Assert.IsType<Gauge>(actual);
        Assert.Equal(4, gauge.Segments);
        Assert.Equal(0.5f, gauge.Level);
        Assert.Equal("g", gauge.Name);
        Assert.Equal(1, gauge.Classes.Count);

        Element expected = new Gauge(4) { Level = 0.5f }.Name("g").Class("hud");
        Assert.Null(TreeCompare.FirstDifference(expected, actual));
    }

    /// <summary>Each fault of the type info throws, and a failed <see cref="TemplateLoader.Register"/> leaves no type.</summary>
    [Fact]
    public void RegisterChecksTheTypeInfo()
    {
        TemplateLoader loader = new();
        ElementFactory good = static (ElementCreateArgs _) => new Gauge(1);

        AssertFault(loader, new ElementTypeInfo { Name = "", Create = good }, "is not a valid XML name");
        AssertFault(loader, new ElementTypeInfo { Name = "a b", Create = good }, "'a b'");
        AssertFault(loader, new ElementTypeInfo { Name = "Probe", Create = null! }, "Create is null");
        AssertFault(
            loader,
            new ElementTypeInfo { Name = "Probe", Create = good, Rows = [new AttributeRow("ghost", (AttributeSetter)null!)] },
            "'ghost'");
        AssertFault(
            loader,
            new ElementTypeInfo { Name = "Probe", Create = good, Rows = [LevelRow(), LevelRow()] },
            "'level'");
        AssertFault(
            loader,
            new ElementTypeInfo
            {
                Name = "Probe",
                Create = good,
                Rows = [new AttributeRow("class", static (Element element, AttributeContext context) => { _ = element; _ = context; })],
            },
            "'class'");
        AssertFault(loader, new ElementTypeInfo { Name = "Label", Create = good }, "'Label'");

        ElementTypeInfo gauge = GaugeInfo();
        loader.Register(gauge);
        AssertFault(loader, gauge, "'Gauge'");

        // The failed calls registered nothing: Probe is still an unknown element.
        TemplateLoadResult probe = loader.Load("""<ui:Probe xmlns:ui="brutal-ui" />"""u8, "gauge.xml");
        Assert.Equal(TemplateErrorKind.UnknownElement, Assert.Single(probe.Errors).Kind);

        // The good Gauge registration landed: the gauge element loads.
        Element actual = loader.Load("""<ui:Gauge xmlns:ui="brutal-ui" segments="4" />"""u8, "gauge.xml").GetRootOrThrow();
        Assert.IsType<Gauge>(actual);
    }

    /// <summary>Each loader holds its own type registry and its own handlers.</summary>
    [Fact]
    public void EachLoaderHasItsOwnRegistry()
    {
        ReadOnlySpan<byte> template = """
            <ui:Element xmlns:ui="brutal-ui" probe="1">
              <ui:Gauge segments="4" />
            </ui:Element>
            """u8;

        TemplateLoader a = new();
        RegisterGauge(a);
        a.AddAttributeHandler(TakeProbe);
        Element root = a.Load(template, "gauges.xml").GetRootOrThrow();
        Assert.IsType<Gauge>(root.Children[0]);

        TemplateLoadResult b = new TemplateLoader().Load(template, "gauges.xml");
        Assert.Null(b.Root);
        Assert.Equal(2, b.Errors.Count);
        Assert.Equal(TemplateErrorKind.UnknownAttribute, b.Errors[0].Kind);
        Assert.Contains("'probe'", b.Errors[0].Message);
        Assert.Equal(TemplateErrorKind.UnknownElement, b.Errors[1].Kind);
        Assert.Contains("'Gauge'", b.Errors[1].Message);
    }

    /// <summary>A factory error on a create attribute goes to the report with the position of the attribute.</summary>
    [Fact]
    public void AFactoryErrorGoesToTheReport()
    {
        TemplateLoader loader = new();
        RegisterGauge(loader);

        string template = """<ui:Gauge xmlns:ui="brutal-ui" segments="0" />""";
        TemplateLoadResult result = loader.Load(Bytes(template), "gauge.xml");
        Assert.Null(result.Root);
        TemplateError error = Assert.Single(result.Errors);
        Assert.Equal(TemplateErrorKind.InvalidValue, error.Kind);
        AssertAttribute(error, template, "segments");
        Assert.Contains("'0'", error.Message);
    }

    /// <summary>A factory that returns <see langword="null"/> with no new error throws.</summary>
    [Fact]
    public void AFactoryThatReturnsNullWithNoErrorThrows()
    {
        TemplateLoader loader = new();
        loader.Register(new ElementTypeInfo { Name = "Broken", Create = static (ElementCreateArgs _) => null });

        Assert.Throws<InvalidOperationException>(
            () => loader.Load("""<ui:Broken xmlns:ui="brutal-ui" />"""u8, "gauge.xml"));
    }

    /// <summary>
    /// <see cref="ElementCreateArgs.TryGet"/> reads a create row of the type only: the name of a set row, and a name
    /// with no row at all, each throw out of <see cref="TemplateLoader.Load(ReadOnlySpan{byte}, string)"/>.
    /// </summary>
    [Fact]
    public void TryGetRejectsANameThatIsNotACreateRow()
    {
        TemplateLoader loader = new();
        loader.Register(ProbeRowInfo());

        // The "level" row of the type is a set row: TryGet throws for its name.
        ArgumentException setError = Assert.Throws<ArgumentException>(
            () => loader.Load("""<ui:ProbeRow xmlns:ui="brutal-ui" segments="1" level="0.5" />"""u8, "gauge.xml"));
        Assert.Equal("name", setError.ParamName);
        Assert.Contains("'level'", setError.Message);

        // The "ghost" name holds no row of the type: TryGet throws for it too.
        ArgumentException ghostError = Assert.Throws<ArgumentException>(
            () => loader.Load("""<ui:ProbeRow xmlns:ui="brutal-ui" segments="2" />"""u8, "gauge.xml"));
        Assert.Equal("name", ghostError.ParamName);
        Assert.Contains("'ghost'", ghostError.Message);
    }

    /// <summary>A custom leaf type rejects a child element with <see cref="TemplateErrorKind.UnexpectedChild"/>.</summary>
    [Fact]
    public void ACustomLeafTypeRejectsAChild()
    {
        TemplateLoader loader = new();
        RegisterGauge(loader, isLeaf: true);

        string template = """
            <ui:Gauge xmlns:ui="brutal-ui" segments="4">
              <ui:Label text="x" />
            </ui:Gauge>
            """;
        TemplateLoadResult result = loader.Load(Bytes(template), "gauge.xml");
        Assert.Null(result.Root);
        TemplateError error = Assert.Single(result.Errors);
        Assert.Equal(TemplateErrorKind.UnexpectedChild, error.Kind);
        AssertElement(error, template, "ui:Label");
        Assert.Contains("'Label'", error.Message);
    }

    /// <summary>A custom element type for the tests: a gauge with a segment count and a level.</summary>
    private sealed class Gauge : Element
    {
        /// <summary>Creates the gauge with its segment count.</summary>
        /// <param name="segments">The count of segments.</param>
        public Gauge(int segments)
        {
            Segments = segments;
        }

        /// <summary>Gets the count of segments.</summary>
        public int Segments { get; }

        /// <summary>Gets or sets the level.</summary>
        public float Level { get; set; }
    }

    /// <summary>Registers the gauge type on one loader.</summary>
    /// <param name="loader">The loader to fill.</param>
    /// <param name="isLeaf">When <see langword="true"/>, the type holds no child element.</param>
    private static void RegisterGauge(TemplateLoader loader, bool isLeaf = false)
    {
        ElementTypeInfo info = GaugeInfo();
        info.IsLeaf = isLeaf;
        loader.Register(info);
    }

    /// <summary>Gives the table entry of the gauge type.</summary>
    /// <returns>The entry, with the <c>segments</c> required create row and the <c>level</c> set row.</returns>
    private static ElementTypeInfo GaugeInfo()
    {
        return new ElementTypeInfo
        {
            Name = "Gauge",
            Create = static (ElementCreateArgs args) => CreateGauge(args),
            Rows =
            [
                new AttributeRow("segments", AttributeRowKind.CreateRequired),
                new AttributeRow("level", static (Element element, AttributeContext context) => SetLevel(element, context)),
            ],
        };
    }

    /// <summary>Makes a gauge from the <c>segments</c> attribute, and reports a bad count with the position.</summary>
    /// <param name="args">The create args of the element.</param>
    /// <returns>The new gauge, or <see langword="null"/> after an error.</returns>
    private static Element? CreateGauge(ElementCreateArgs args)
    {
        if (!args.TryGet("segments", out AttributeContext context) || !context.TryReadWholeNumber(out int segments))
        {
            return null;
        }

        return new Gauge(segments);
    }

    /// <summary>Gives the table entry of the <c>ProbeRow</c> type: the <c>segments</c> required create row and the
    /// <c>level</c> set row, the gauge-like rows that the <see cref="ElementCreateArgs.TryGet"/> test reads.</summary>
    /// <returns>The entry.</returns>
    private static ElementTypeInfo ProbeRowInfo()
    {
        return new ElementTypeInfo
        {
            Name = "ProbeRow",
            Create = static (ElementCreateArgs args) => CreateProbeRow(args),
            Rows =
            [
                new AttributeRow("segments", AttributeRowKind.CreateRequired),
                new AttributeRow("level", static (Element element, AttributeContext context) => { _ = element; _ = context; }),
            ],
        };
    }

    /// <summary>Calls <see cref="ElementCreateArgs.TryGet"/> with a name that is not a create row of the type.</summary>
    /// <param name="args">The create args of the element.</param>
    /// <returns>Nothing: the call always throws.</returns>
    /// <remarks>
    /// The <c>segments</c> value picks the bad name: 1 gives the name of the <c>level</c> set row; any other value
    /// gives the <c>ghost</c> name, which holds no row of the type.
    /// </remarks>
    private static Element? CreateProbeRow(ElementCreateArgs args)
    {
        if (!args.TryGet("segments", out AttributeContext context) || !context.TryReadWholeNumber(out int segments))
        {
            return null;
        }

        string badName = segments == 1 ? "level" : "ghost";
        _ = args.TryGet(badName, out _);
        return new Gauge(segments);
    }

    /// <summary>Writes the <c>level</c> value of a gauge.</summary>
    /// <param name="element">The element to change.</param>
    /// <param name="context">The <c>level</c> attribute.</param>
    private static void SetLevel(Element element, AttributeContext context)
    {
        if (context.TryReadNumber(out float level))
        {
            ((Gauge)element).Level = level;
        }
    }

    /// <summary>Gives a set row named <c>level</c>, for the duplicate-row fault.</summary>
    /// <returns>The row.</returns>
    private static AttributeRow LevelRow()
    {
        return new AttributeRow("level", static (Element element, AttributeContext context) => { _ = element; _ = context; });
    }

    /// <summary>Takes the <c>probe</c> attribute, for the registry test.</summary>
    /// <param name="element">The element that the factory made.</param>
    /// <param name="name">The attribute name.</param>
    /// <param name="value">The attribute value.</param>
    /// <param name="context">The position of the attribute.</param>
    /// <returns><see langword="true"/> for <c>probe</c>.</returns>
    private static bool TakeProbe(Element element, string name, string value, AttributeContext context)
    {
        _ = element;
        _ = value;
        _ = context;
        return name == "probe";
    }

    /// <summary>Runs one bad <see cref="TemplateLoader.Register"/> call and checks the throw and the registry.</summary>
    /// <param name="loader">The loader.</param>
    /// <param name="info">The bad entry.</param>
    /// <param name="fault">The text that the message must hold.</param>
    /// <remarks>
    /// The probe load names the registered types in its <see cref="TemplateErrorKind.UnknownElement"/> message. The
    /// message stands unchanged around the bad call, so the failed <see cref="TemplateLoader.Register"/> added
    /// nothing. The compare holds no table knowledge: it reads the same message before and after the call.
    /// </remarks>
    private static void AssertFault(TemplateLoader loader, ElementTypeInfo info, string fault)
    {
        string namesBefore = RegistryNames(loader);
        ArgumentException error = Assert.ThrowsAny<ArgumentException>(() => loader.Register(info));
        Assert.Equal("info", error.ParamName);
        Assert.Contains(fault, error.Message);
        Assert.Equal(namesBefore, RegistryNames(loader));
    }

    /// <summary>Loads one unknown element and reads the name list of the registry from its error message.</summary>
    /// <param name="loader">The loader.</param>
    /// <returns>The message of the single <see cref="TemplateErrorKind.UnknownElement"/> error.</returns>
    private static string RegistryNames(TemplateLoader loader)
    {
        TemplateLoadResult result = loader.Load("""<ui:NoSuchType xmlns:ui="brutal-ui" />"""u8, "registry.xml");
        return Assert.Single(result.Errors).Message;
    }

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

    /// <summary>Encodes one template text to UTF-8 bytes for the span overload.</summary>
    private static byte[] Bytes(string template) => Encoding.UTF8.GetBytes(template);
}
