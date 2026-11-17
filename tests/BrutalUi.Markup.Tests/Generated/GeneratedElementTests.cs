using System;
using System.Collections.Generic;
using Brutal.Numerics;
using BrutalUi.Core;
using BrutalUi.Core.Tests;
using Xunit;

namespace BrutalUi.Markup.Tests.Generated;

/// <summary>
/// The phase 1 generated-element tests: the source generator writes the rows of the marked types below, the
/// generated register call adds them to a loader, and the loader loads them like a written table entry.
/// </summary>
/// <remarks>
/// <para>
/// The tests use the generated API <c>loader.RegisterBrutalUiMarkupTestsElements()</c>. It appears in this
/// assembly when <c>BrutalUi.Generators</c> runs as an analyzer. The XML names <c>Gauge</c>, <c>Readout</c>, and
/// <c>Lamp</c> are unique in this assembly.
/// </para>
/// </remarks>
public sealed class GeneratedElementTests
{
    private static readonly TextureHandle Icon = new(7, new int2(16, 16));

    /// <summary>The phase 1 demo (Q5): a marked <see cref="Gauge"/> loads with its <c>Level</c> 0.5.</summary>
    [Fact]
    public void AMarkedGaugeLoadsWithItsLevel()
    {
        TemplateLoader loader = NewLoaderWithGeneratedTypes();

        TemplateLoadResult result = loader.Load(
            """<ui:Gauge xmlns:ui="brutal-ui" level="0.5" />"""u8, "gauge.xml");
        Gauge gauge = Assert.IsType<Gauge>(result.GetRootOrThrow());
        Assert.Equal(0.5f, gauge.Level);
    }

    /// <summary>The generated type takes the base rows, and its tree equals the builder tree.</summary>
    [Fact]
    public void TheGaugeTakesTheBaseRowsAndEqualsTheBuilderTree()
    {
        TemplateLoader loader = NewLoaderWithGeneratedTypes();

        Element actual = loader.Load(
            """<ui:Gauge xmlns:ui="brutal-ui" name="g" class="hud" level="0.5" />"""u8, "gauge.xml")
            .GetRootOrThrow();
        Element expected = new Gauge { Level = 0.5f }.Name("g").Class("hud");
        Assert.Null(TreeCompare.FirstDifference(expected, actual));
    }

    /// <summary>A bad <c>level</c> value gives one <see cref="TemplateErrorKind.InvalidValue"/> at the attribute, and no root.</summary>
    [Fact]
    public void ABadLevelIsAnInvalidValueAtTheAttribute()
    {
        TemplateLoader loader = NewLoaderWithGeneratedTypes();

        string template = """<ui:Gauge xmlns:ui="brutal-ui" level="x" />""";
        TemplateLoadResult result = loader.Load(Utf8(template), "gauge.xml");
        Assert.Null(result.Root);
        TemplateError error = Assert.Single(result.Errors);
        Assert.Equal(TemplateErrorKind.InvalidValue, error.Kind);
        AssertAttribute(error, template, "level");
        Assert.Contains("'x'", error.Message);
    }

    /// <summary>Each of the six supported read kinds gives a working row on the marked <see cref="Readout"/>.</summary>
    [Fact]
    public void EachReadTypeGivesARow()
    {
        Dictionary<string, TextureHandle> map = new(StringComparer.Ordinal) { ["icon"] = Icon };
        TemplateLoader loader = new(new TemplateLoader.CreateInfo { Textures = map });
        loader.RegisterBrutalUiMarkupTestsElements();

        string template = """
            <ui:Readout xmlns:ui="brutal-ui" caption="Hello World" lit="true" offset="2 3" rect="0 0 1 1"
                        color="#ff0000" icon="icon" />
            """;
        Readout readout = Assert.IsType<Readout>(loader.Load(Utf8(template), "readout.xml").GetRootOrThrow());
        Assert.Equal("Hello World", readout.Caption);
        Assert.True(readout.Lit);
        Assert.Equal(new float2(2f, 3f), readout.Offset);
        Assert.Equal(new float4(0f, 0f, 1f, 1f), readout.Rect);
        Assert.Equal(new Rgba8(255, 0, 0), readout.Color);
        Assert.Equal(Icon, readout.Icon);
    }

    /// <summary>A property with no row (<c>Changed</c>, get-only <c>Count</c>) and the <c>states</c> name each give an <see cref="TemplateErrorKind.UnknownAttribute"/>.</summary>
    [Fact]
    public void APropertyWithNoRowIsAnUnknownAttribute()
    {
        TemplateLoader loader = NewLoaderWithGeneratedTypes();

        foreach (string name in new[] { "changed", "count", "states" })
        {
            string template = $"""<ui:Readout xmlns:ui="brutal-ui" {name}="1" />""";
            TemplateError error = Fault(loader, template);
            Assert.Equal(TemplateErrorKind.UnknownAttribute, error.Kind);
            AssertAttribute(error, template, name);
            Assert.Contains($"'{name}'", error.Message);
            Assert.Contains("name, class, pickable, focusable, caption, lit, offset, rect, color, icon", error.Message);
        }
    }

    /// <summary>A leaf type rejects a child element with <see cref="TemplateErrorKind.UnexpectedChild"/>.</summary>
    [Fact]
    public void ALeafTypeRejectsAChild()
    {
        TemplateLoader loader = NewLoaderWithGeneratedTypes();

        string template = """
            <ui:Lamp xmlns:ui="brutal-ui">
              <ui:Label text="x" />
            </ui:Lamp>
            """;
        TemplateError error = Fault(loader, template);
        Assert.Equal(TemplateErrorKind.UnexpectedChild, error.Kind);
        AssertElement(error, template, "ui:Label");
        Assert.Contains("'Label'", error.Message);
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
        TemplateLoadResult result = loader.Load(Utf8(template), "generated.xml");
        Assert.Null(result.Root);
        return Assert.Single(result.Errors);
    }

    /// <summary>Encodes one template text to UTF-8 bytes for the span overload.</summary>
    private static byte[] Utf8(string template) => System.Text.Encoding.UTF8.GetBytes(template);

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

/// <summary>A marked leaf type with one float property: the phase 1 demo type.</summary>
[TemplateElement("Gauge")]
internal sealed class Gauge : Element
{
    /// <summary>Gets or sets the level.</summary>
    public float Level { get; set; }
}

/// <summary>A marked type with one property per supported read kind, and two properties with no row.</summary>
[TemplateElement("Readout")]
internal sealed class Readout : Element
{
    /// <summary>Gets or sets the caption (text read).</summary>
    public string? Caption { get; set; }

    /// <summary>Gets or sets the lit flag (bool read).</summary>
    public bool Lit { get; set; }

    /// <summary>Gets or sets the offset (two-number read).</summary>
    public float2 Offset { get; set; }

    /// <summary>Gets or sets the rect (four-number read).</summary>
    public float4 Rect { get; set; }

    /// <summary>Gets or sets the color (color read).</summary>
    public Rgba8 Color { get; set; }

    /// <summary>Gets or sets the icon (texture read).</summary>
    public TextureHandle Icon { get; set; }

    /// <summary>Gets or sets a callback with no row: an unsupported type gives no row and no diagnostic.</summary>
    public Action<Readout>? Changed { get; set; }

    /// <summary>Gets a count with no row: a get-only property gives no row.</summary>
    public int Count => 0;
}

/// <summary>A marked leaf type with no own rows.</summary>
[TemplateElement("Lamp", IsLeaf = true)]
internal sealed class Lamp : Element
{
}
