// The second part of the phase 4 diagnostic tests: the row-name and element-type faults, and the clean
// valid sample. The class name still matches the '*DiagnosticTests' filter of the phase demo, so the filter
// reads a total of 8 across the two files. DiagnosticTests.cs holds the rest, because this file keeps the
// project rule of one file under 400 lines.
using Xunit;

namespace BrutalUi.Generators.Tests;

/// <summary>
/// Covers BRUTALUI0005, BRUTALUI0006, and the valid sample with every supported type.
/// </summary>
public sealed class RowAndTypeDiagnosticTests
{
    /// <summary>Two [TemplateRowName("a")], [TemplateRowName("class")], and [TemplateRowName("a b")] each give
    /// BRUTALUI0005 on the property name.</summary>
    [Fact]
    public void ABadRowNameGivesBRUTALUI0005()
    {
        const string fix = "Give each row a valid XML name that no other row and no base row takes.";

        GeneratorRun run = GeneratorHarness.Run("""
            using BrutalUi.Core;
            using BrutalUi.Markup;

            namespace Sample;

            [TemplateElement("Gauge")]
            internal sealed class Gauge : Element
            {
                [TemplateRowName("a")]
                public float First { get; set; }

                [TemplateRowName("a")]
                public float Second { get; set; }
            }
            """);
        Assert.Equal(2, run.Diagnostics.Length);
        GeneratorHarness.AssertDiagnostic(run.Diagnostics[0], "BRUTALUI0005", 10, 18, $"The row name 'a' of 'First' is not valid: another row takes it. {fix}");
        GeneratorHarness.AssertDiagnostic(run.Diagnostics[1], "BRUTALUI0005", 13, 18, $"The row name 'a' of 'Second' is not valid: another row takes it. {fix}");

        run = GeneratorHarness.Run("""
            using BrutalUi.Core;
            using BrutalUi.Markup;

            namespace Sample;

            [TemplateElement("Gauge")]
            internal sealed class Gauge : Element
            {
                [TemplateRowName("class")]
                public float Level { get; set; }
            }
            """);
        GeneratorHarness.AssertDiagnostic(Assert.Single(run.Diagnostics), "BRUTALUI0005", 10, 18, $"The row name 'class' of 'Level' is not valid: a base row takes it. {fix}");

        run = GeneratorHarness.Run("""
            using BrutalUi.Core;
            using BrutalUi.Markup;

            namespace Sample;

            [TemplateElement("Gauge")]
            internal sealed class Gauge : Element
            {
                [TemplateRowName("a b")]
                public float Level { get; set; }
            }
            """);
        GeneratorHarness.AssertDiagnostic(Assert.Single(run.Diagnostics), "BRUTALUI0005", 10, 18, $"The row name 'a b' of 'Level' is not valid: it is not a valid XML local name. {fix}");
    }

    /// <summary>A class that does not derive from <see cref="BrutalUi.Core.Element"/>, a generic class, a
    /// private nested class, <c>Target</c> on a class that is not static, a declaration class inside a
    /// generic class, and the XML name "1x" each give BRUTALUI0006 on the class name.</summary>
    [Fact]
    public void ABadElementTypeGivesBRUTALUI0006()
    {
        GeneratorRun run = GeneratorHarness.Run("""
            using BrutalUi.Core;
            using BrutalUi.Markup;

            namespace Sample;

            [TemplateElement("Gauge")]
            internal sealed class Plain
            {
            }

            [TemplateElement("Gauge")]
            internal sealed class Box<T> : Element
            {
            }

            internal static class Holder
            {
                [TemplateElement("Gauge")]
                private sealed class Inner : Element
                {
                }
            }

            internal class Dial : Element
            {
            }

            [TemplateElement("DialDecl", Target = typeof(Dial))]
            internal sealed class Declaration
            {
            }

            internal static class GenericHolder<T>
            {
                [TemplateElement("Gadget", Target = typeof(Dial))]
                internal static class Declared
                {
                }
            }

            [TemplateElement("1x")]
            internal sealed class Weird : Element
            {
            }
            """);
        Assert.Equal(6, run.Diagnostics.Length);
        GeneratorHarness.AssertDiagnostic(run.Diagnostics[0], "BRUTALUI0006", 7, 23, "'Sample.Plain' is not a valid element type: it does not derive from BrutalUi.Core.Element.");
        GeneratorHarness.AssertDiagnostic(run.Diagnostics[1], "BRUTALUI0006", 12, 23, "'Sample.Box<T>' is not a valid element type: it is a generic type.");
        GeneratorHarness.AssertDiagnostic(run.Diagnostics[2], "BRUTALUI0006", 19, 26, "'Sample.Holder.Inner' is not a valid element type: it or a containing class is not internal or public.");
        GeneratorHarness.AssertDiagnostic(run.Diagnostics[3], "BRUTALUI0006", 29, 23, "'Sample.Declaration' is not a valid element type: it is not a static class.");
        GeneratorHarness.AssertDiagnostic(run.Diagnostics[4], "BRUTALUI0006", 36, 27, "'Sample.GenericHolder<T>.Declared' is not a valid element type: it is a generic type.");
        GeneratorHarness.AssertDiagnostic(run.Diagnostics[5], "BRUTALUI0006", 42, 23, "'Sample.Weird' is not a valid element type: the XML name is not a valid XML local name.");
        Assert.Empty(run.GeneratedTexts);
    }

    /// <summary>A sample with each supported type, a factory, a declaration class, a renamed row, an ignored
    /// property, and an <see cref="System.Action"/> property gives no diagnostic (A5).</summary>
    [Fact]
    public void AValidSampleGivesNoDiagnostic()
    {
        GeneratorRun run = GeneratorHarness.Run("""
            using System;
            using Brutal.Numerics;
            using BrutalUi.Core;
            using BrutalUi.Markup;

            namespace Sample;

            public enum ReadoutStyle
            {
                Bar,
                RoundDial,
            }

            [TemplateElement("Readout")]
            internal sealed class Readout : Element
            {
                public string? Caption { get; set; }
                public float Level { get; set; }
                public int Count { get; set; }
                public bool Lit { get; set; }
                public ReadoutStyle Face { get; set; }
                public float2 Offset { get; set; }
                public float4 Rect { get; set; }
                public Rgba8 Color { get; set; }
                public TextureHandle Icon { get; set; }
                public Action<Readout>? Changed { get; set; }

                [TemplateRowName("lvl")]
                public float Renamed { get; set; }

                [TemplateIgnore]
                public float Hidden { get; set; }
            }

            internal sealed class Dial : Element
            {
                public float Angle { get; set; }
            }

            [TemplateElement("DialDecl", Target = typeof(Dial))]
            internal static class DialDeclaration
            {
            }

            [TemplateElement("Meter")]
            internal sealed class Meter : Element
            {
                public Meter(int segments)
                {
                }

                [TemplateFactory]
                [TemplateCreateRow("segments", IsRequired = true)]
                internal static Element? Create(ElementCreateArgs args)
                {
                    return new Meter(1);
                }

                public float Value { get; set; }
            }
            """);
        Assert.Empty(run.Diagnostics);
        Assert.Empty(run.CompilationDiagnostics);
        Assert.True(run.GeneratedTexts.ContainsKey("SampleTemplateElements.g.cs"));
    }
}
