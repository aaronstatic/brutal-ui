// The phase 4 diagnostic tests. Each test compiles one sample in memory, runs the generator, and asserts the
// id, the severity, the line and the column, and the message text of each fault. The expected shapes match the
// implementation decisions of phase 4 group 1, recorded in docs/features/markup/element-generator/context.md.
// RowAndTypeDiagnosticTests.cs holds BRUTALUI0005, BRUTALUI0006, and the valid sample, so each file stays
// under 400 lines. The '*DiagnosticTests' filter reads both classes: a total of 8.
using Xunit;

namespace BrutalUi.Generators.Tests;

/// <summary>
/// Covers BRUTALUI0001 to BRUTALUI0004, and the no-code rule for a type with an error.
/// </summary>
public sealed class DiagnosticTests
{
    private const string NoFactoryMessage =
        "Add a public constructor with no parameters, or mark a static method with [TemplateFactory].";

    // The head and the tail are plain literals, not raw literals: a raw literal drops the newline
    // before its closing delimiter, and the joined sample would lose its line shape.
    private const string FactoryHead = "using BrutalUi.Core;\nusing BrutalUi.Markup;\n\nnamespace Sample;\n\n[TemplateElement(\"Meter\")]\ninternal sealed class Meter : Element\n{\n";

    private const string FactoryTail = "}\n";

    /// <summary>A constructor with a parameter, and an abstract class, each give BRUTALUI0001 on the class
    /// name. The same class with a factory gives none (Q3).</summary>
    [Fact]
    public void ATypeWithNoConstructorAndNoFactoryGivesBRUTALUI0001()
    {
        GeneratorRun run = GeneratorHarness.Run("""
            using BrutalUi.Core;
            using BrutalUi.Markup;

            namespace Sample;

            [TemplateElement("Gauge")]
            internal sealed class Gauge : Element
            {
                public Gauge(int x)
                {
                }
            }
            """);
        GeneratorHarness.AssertDiagnostic(
            Assert.Single(run.Diagnostics),
            "BRUTALUI0001",
            7,
            23,
            $"'Sample.Gauge' has no usable constructor and no factory. {NoFactoryMessage}");

        run = GeneratorHarness.Run("""
            using BrutalUi.Core;
            using BrutalUi.Markup;

            namespace Sample;

            [TemplateElement("Widget")]
            internal abstract class Widget : Element
            {
            }
            """);
        GeneratorHarness.AssertDiagnostic(
            Assert.Single(run.Diagnostics),
            "BRUTALUI0001",
            7,
            25,
            $"'Sample.Widget' has no usable constructor and no factory. {NoFactoryMessage}");

        run = GeneratorHarness.Run("""
            using BrutalUi.Core;
            using BrutalUi.Markup;

            namespace Sample;

            [TemplateElement("Gauge")]
            internal sealed class Gauge : Element
            {
                public Gauge(int x)
                {
                }

                [TemplateFactory]
                internal static Element? Create(ElementCreateArgs args)
                {
                    return new Gauge(0);
                }
            }
            """);
        Assert.Empty(run.Diagnostics);
        Assert.Empty(run.CompilationDiagnostics);
    }

    /// <summary>Two marked classes with one XML name give one BRUTALUI0002 on each name argument, and no
    /// code for either.</summary>
    [Fact]
    public void TwoTypesWithOneXmlNameGiveBRUTALUI0002()
    {
        GeneratorRun run = GeneratorHarness.Run("""
            using BrutalUi.Core;
            using BrutalUi.Markup;

            namespace Sample;

            [TemplateElement("Gauge")]
            internal sealed class Alpha : Element
            {
            }

            [TemplateElement("Gauge")]
            internal sealed class Beta : Element
            {
            }
            """);
        Assert.Equal(2, run.Diagnostics.Length);
        const string message = "The XML name 'Gauge' is on more than one element type: Sample.Alpha, Sample.Beta. Give each type its own name.";
        GeneratorHarness.AssertDiagnostic(run.Diagnostics[0], "BRUTALUI0002", 6, 18, message);
        GeneratorHarness.AssertDiagnostic(run.Diagnostics[1], "BRUTALUI0002", 11, 18, message);
        Assert.Empty(run.GeneratedTexts);
    }

    /// <summary>[TemplateRowName] on an <see cref="System.Action"/> property and on a get-only property each
    /// give BRUTALUI0003 on the attribute.</summary>
    [Fact]
    public void ARowNameOnAPropertyWithNoRowGivesBRUTALUI0003()
    {
        GeneratorRun run = GeneratorHarness.Run("""
            using System;
            using BrutalUi.Core;
            using BrutalUi.Markup;

            namespace Sample;

            [TemplateElement("Readout")]
            internal sealed class Readout : Element
            {
                [TemplateRowName("changed")]
                public Action<Readout>? Changed { get; set; }

                [TemplateRowName("count")]
                public int Count => 0;
            }
            """);
        Assert.Equal(2, run.Diagnostics.Length);
        GeneratorHarness.AssertDiagnostic(
            run.Diagnostics[0],
            "BRUTALUI0003",
            10,
            6,
            "[TemplateRowName] is on 'Changed', but 'Changed' gives no row. Use a public settable property of a supported type, or remove the attribute.");
        GeneratorHarness.AssertDiagnostic(
            run.Diagnostics[1],
            "BRUTALUI0003",
            13,
            6,
            "[TemplateRowName] is on 'Count', but 'Count' gives no row. Use a public settable property of a supported type, or remove the attribute.");
    }

    /// <summary>An instance method, a private method, two parameters, a <see cref="string"/> return, two
    /// factories, and [TemplateCreateRow] with no factory each give BRUTALUI0004 on the method name.</summary>
    [Fact]
    public void ABadFactoryGivesBRUTALUI0004()
    {
        const string write = "Write 'static Element? Create(ElementCreateArgs args)' with internal or public access.";

        AssertFactoryFault(
            """
                [TemplateFactory]
                internal Element? Create(ElementCreateArgs args)
                {
                    return null;
                }

            """,
            1,
            (10, 23),
            $"'Create' is not a valid factory: it is not static. {write}");
        AssertFactoryFault(
            """
                [TemplateFactory]
                private static Element? Create(ElementCreateArgs args)
                {
                    return null;
                }

            """,
            1,
            (10, 29),
            $"'Create' is not a valid factory: it is not internal or public. {write}");
        AssertFactoryFault(
            """
                [TemplateFactory]
                internal static Element? Create(ElementCreateArgs args, int extra)
                {
                    return null;
                }

            """,
            1,
            (10, 30),
            $"'Create' is not a valid factory: it does not have one parameter of type ElementCreateArgs. {write}");
        AssertFactoryFault(
            """
                [TemplateFactory]
                internal static string Create(ElementCreateArgs args)
                {
                    return string.Empty;
                }

            """,
            1,
            (10, 28),
            $"'Create' is not a valid factory: it does not return Element or a type derived from Element. {write}");
        AssertFactoryFault(
            """
                [TemplateFactory]
                internal static Element? Create(ElementCreateArgs args)
                {
                    return null;
                }

                [TemplateFactory]
                internal static Element? Make(ElementCreateArgs args)
                {
                    return null;
                }

            """,
            1,
            (16, 30),
            $"'Make' is not a valid factory: the class holds more than one factory. {write}");
        AssertFactoryFault(
            """
                [TemplateCreateRow("segments")]
                internal static Element? Build(ElementCreateArgs args)
                {
                    return null;
                }

            """,
            1,
            (10, 30),
            $"'Build' is not a valid factory: it has [TemplateCreateRow] but no [TemplateFactory]. {write}");
    }

    /// <summary>One bad type and one good type: the output holds the good type only, and the output
    /// compilation has no error.</summary>
    [Fact]
    public void ATypeWithAnErrorGetsNoCodeAndTheOtherTypesStay()
    {
        GeneratorRun run = GeneratorHarness.Run("""
            using BrutalUi.Core;
            using BrutalUi.Markup;

            namespace Sample;

            [TemplateElement("Gauge")]
            internal sealed class Gauge : Element
            {
                public Gauge(int x)
                {
                }
            }

            [TemplateElement("Widget")]
            internal sealed class Widget : Element
            {
                public int Power { get; set; }
            }
            """);
        GeneratorHarness.AssertDiagnostic(
            Assert.Single(run.Diagnostics),
            "BRUTALUI0001",
            7,
            23,
            $"'Sample.Gauge' has no usable constructor and no factory. {NoFactoryMessage}");
        string text = Assert.Single(run.GeneratedTexts).Value;
        Assert.Contains("Name = \"Widget\",", text);
        Assert.Contains("((global::Sample.Widget)element).Power = value;", text);
        Assert.DoesNotContain("Gauge", text);
        Assert.Empty(run.CompilationDiagnostics);
    }

    /// <summary>Runs one factory sample and asserts the count and the faults of BRUTALUI0004.</summary>
    /// <param name="member">The member text, with its closing line kept in the head and the tail.</param>
    /// <param name="count">The number of expected faults.</param>
    /// <param name="position">The expected line and column of the first fault.</param>
    /// <param name="message">The expected message of the first fault.</param>
    private static void AssertFactoryFault(string member, int count, (int Line, int Column) position, string message)
    {
        GeneratorRun run = GeneratorHarness.Run(FactoryHead + member + "\n" + FactoryTail);
        Assert.Equal(count, run.Diagnostics.Length);
        GeneratorHarness.AssertDiagnostic(run.Diagnostics[0], "BRUTALUI0004", position.Line, position.Column, message);
        Assert.Empty(run.GeneratedTexts);
    }
}
