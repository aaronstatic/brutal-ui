using System;
using System.Text;
using BrutalUi.Core;
using Xunit;

namespace BrutalUi.Markup.Tests.Generated;

/// <summary>
/// The tests of the two new reads on <see cref="AttributeContext"/>: <see cref="AttributeContext.TryReadInteger"/> and
/// <see cref="AttributeContext.TryReadKeyword"/>. The source generator takes no part in them.
/// </summary>
/// <remarks>
/// <para>
/// Each test registers its probe type by hand with <see cref="TemplateLoader.Register"/>, and one set row of the type
/// calls the read under test. The probe types carry no <see cref="TemplateElementAttribute"/>, so the generator
/// ignores them and writes no rows for them. The XML names <c>ProbeInt</c> and <c>ProbeKeyword</c> name no marked
/// type.
/// </para>
/// <para>
/// Each fault check reads the kind, the line, the column, and the quoted text of the message. The position of an
/// attribute fault sits at the first character of the attribute name.
/// </para>
/// </remarks>
public sealed class TemplateReadMethodTests
{
    /// <summary>The keywords of the <c>mode</c> row, in order.</summary>
    private static readonly string[] Modes = ["bar", "round-dial", "flat"];

    /// <summary><see cref="AttributeContext.TryReadInteger"/> reads 0, a sign, and whitespace at both ends.</summary>
    [Fact]
    public void AnIntegerTakesASignAndDigits()
    {
        TemplateLoader loader = NewProbeLoader();

        (string Text, int Expected)[] cases = [("0", 0), ("-3", -3), ("+3", 3), (" 42 ", 42)];
        foreach ((string text, int expected) in cases)
        {
            string template = $"""<ui:ProbeInt xmlns:ui="brutal-ui" count="{text}" />""";
            TemplateLoadResult result = loader.Load(Utf8(template), "reads.xml");
            ProbeInt probe = Assert.IsType<ProbeInt>(result.GetRootOrThrow());
            Assert.Empty(result.Errors);
            Assert.Equal(expected, probe.Count);
        }
    }

    /// <summary>A value that is not an integer gives one <see cref="TemplateErrorKind.InvalidValue"/> at the attribute.</summary>
    [Fact]
    public void AnIntegerFaultIsAnInvalidValue()
    {
        TemplateLoader loader = NewProbeLoader();

        string[] bad = ["1.5", "x", "-", "", "1e3", "2147483648"];
        foreach (string text in bad)
        {
            string template = $"""<ui:ProbeInt xmlns:ui="brutal-ui" count="{text}" />""";
            TemplateError error = Fault(loader, template);
            Assert.Equal(TemplateErrorKind.InvalidValue, error.Kind);
            AssertAttribute(error, template, "count");
            Assert.Contains($"'{text}'", error.Message);
            Assert.Contains("is not an integer", error.Message);
        }
    }

    /// <summary><see cref="AttributeContext.TryReadKeyword"/> gives the index of the keyword, after the trim.</summary>
    [Fact]
    public void AKeywordGivesItsIndex()
    {
        TemplateLoader loader = NewProbeLoader();

        (string Text, int Expected)[] cases = [("bar", 0), ("round-dial", 1), ("flat", 2), (" round-dial ", 1)];
        foreach ((string text, int expected) in cases)
        {
            string template = $"""<ui:ProbeKeyword xmlns:ui="brutal-ui" mode="{text}" />""";
            TemplateLoadResult result = loader.Load(Utf8(template), "reads.xml");
            ProbeKeyword probe = Assert.IsType<ProbeKeyword>(result.GetRootOrThrow());
            Assert.Empty(result.Errors);
            Assert.Equal(expected, probe.Mode);
        }
    }

    /// <summary>An unknown keyword gives one <see cref="TemplateErrorKind.InvalidValue"/> that lists each keyword.</summary>
    [Fact]
    public void AnUnknownKeywordListsTheKeywords()
    {
        TemplateLoader loader = NewProbeLoader();

        string template = """<ui:ProbeKeyword xmlns:ui="brutal-ui" mode="ghost" />""";
        TemplateError error = Fault(loader, template);
        Assert.Equal(TemplateErrorKind.InvalidValue, error.Kind);
        AssertAttribute(error, template, "mode");
        Assert.Contains("'ghost'", error.Message);
        Assert.Contains("is not a value of mode", error.Message);
        foreach (string keyword in Modes)
        {
            Assert.Contains($"'{keyword}'", error.Message);
        }
    }

    /// <summary>Creates a loader and registers the two probe types by hand.</summary>
    /// <returns>The loader with the built-in types and the two probe types.</returns>
    private static TemplateLoader NewProbeLoader()
    {
        TemplateLoader loader = new();
        loader.Register(ProbeIntInfo());
        loader.Register(ProbeKeywordInfo());
        return loader;
    }

    /// <summary>Gives the table entry of the <c>ProbeInt</c> type: one set row that calls <see cref="AttributeContext.TryReadInteger"/>.</summary>
    /// <returns>The entry.</returns>
    private static ElementTypeInfo ProbeIntInfo()
    {
        return new ElementTypeInfo
        {
            Name = "ProbeInt",
            Create = static (ElementCreateArgs _) => new ProbeInt(),
            Rows = [new AttributeRow("count", static (Element element, AttributeContext context) => SetCount(element, context))],
        };
    }

    /// <summary>Gives the table entry of the <c>ProbeKeyword</c> type: one set row that calls <see cref="AttributeContext.TryReadKeyword"/>.</summary>
    /// <returns>The entry.</returns>
    private static ElementTypeInfo ProbeKeywordInfo()
    {
        return new ElementTypeInfo
        {
            Name = "ProbeKeyword",
            Create = static (ElementCreateArgs _) => new ProbeKeyword(),
            Rows = [new AttributeRow("mode", static (Element element, AttributeContext context) => SetMode(element, context))],
        };
    }

    /// <summary>Writes the <c>count</c> value of a probe, and writes nothing on a fault.</summary>
    /// <param name="element">The element to change.</param>
    /// <param name="context">The <c>count</c> attribute.</param>
    private static void SetCount(Element element, AttributeContext context)
    {
        if (context.TryReadInteger(out int count))
        {
            ((ProbeInt)element).Count = count;
        }
    }

    /// <summary>Writes the index of the <c>mode</c> keyword of a probe, and writes nothing on a fault.</summary>
    /// <param name="element">The element to change.</param>
    /// <param name="context">The <c>mode</c> attribute.</param>
    private static void SetMode(Element element, AttributeContext context)
    {
        if (context.TryReadKeyword(Modes, out int index))
        {
            ((ProbeKeyword)element).Mode = index;
        }
    }

    /// <summary>Loads one bad template and gives its one error. The root is <see langword="null"/>.</summary>
    private static TemplateError Fault(TemplateLoader loader, string template)
    {
        TemplateLoadResult result = loader.Load(Utf8(template), "reads.xml");
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

    /// <summary>A probe element with one integer row.</summary>
    private sealed class ProbeInt : Element
    {
        /// <summary>Gets or sets the count that the <c>count</c> row writes.</summary>
        public int Count { get; set; }
    }

    /// <summary>A probe element with one keyword row.</summary>
    private sealed class ProbeKeyword : Element
    {
        /// <summary>Gets or sets the index of the <c>mode</c> keyword that the row writes.</summary>
        public int Mode { get; set; }
    }
}
