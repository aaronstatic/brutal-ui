// The CR-1 walk-rule pins and the CR-3 metadata-init pin: the visible property of a name decides the row, and
// an init accessor that only the symbol semantics show gives no row.
using System;
using System.Linq;
using Microsoft.CodeAnalysis;
using Xunit;

namespace BrutalUi.Generators.Tests;

/// <summary>
/// Covers the walk of a hiding or overriding property, and the walk of a property that comes from metadata.
/// </summary>
/// <remarks>
/// <para>
/// Review finding CR-1: the walk read the base property of a name and the generated cast named the target type,
/// so C# member lookup bound the assignment to the derived property. The pins below run the driver and assert an
/// empty <see cref="GeneratorRun.CompilationDiagnostics"/>: a base row that keeps the base read kind for a
/// derived property of another type gives a <c>CS0266</c> error in the generated file.
/// </para>
/// <para>
/// Review finding CR-3: <c>IsInit</c> read the accessor syntax, so an <c>init</c> property of a target type from
/// another assembly counted as settable and the generated assignment gave <c>CS8852</c>. The walk now reads
/// <c>IMethodSymbol.IsInitOnly</c>, the public Roslyn 4.14 flag that reports the accessor of a metadata symbol.
/// </para>
/// </remarks>
public sealed class WalkRulesTests
{
    /// <summary>A <c>new</c> hide of another type gives the derived read kind, and the generated file compiles.</summary>
    [Fact]
    public void AHidingPropertyTakesTheDerivedReadKind()
    {
        GeneratorRun run = GeneratorHarness.Run("""
            using BrutalUi.Core;
            using BrutalUi.Markup;

            namespace Sample;

            internal class Base : Element
            {
                public float Amount { get; set; }
            }

            [TemplateElement("Hide")]
            internal sealed class Derived : Base
            {
                public new int Amount { get; set; }
            }
            """);
        Assert.Empty(run.Diagnostics);
        Assert.Empty(run.CompilationDiagnostics);
        string text = Assert.Single(run.GeneratedTexts).Value;
        Assert.Contains("context.TryReadInteger(out int value)", text);
        Assert.Contains("((global::Sample.Derived)element).Amount = value;", text);
        Assert.DoesNotContain("TryReadNumber", text);
    }

    /// <summary><c>[TemplateIgnore]</c> on the hider removes the row of the base property of the same name.</summary>
    [Fact]
    public void AnIgnoredHidingPropertyRemovesTheBaseRow()
    {
        GeneratorRun run = GeneratorHarness.Run("""
            using BrutalUi.Core;
            using BrutalUi.Markup;

            namespace Sample;

            internal class Base : Element
            {
                public float Level { get; set; }

                public float Zoom { get; set; }
            }

            [TemplateElement("Hide")]
            internal sealed class Derived : Base
            {
                [TemplateIgnore]
                public new float Level { get; set; }
            }
            """);
        Assert.Empty(run.Diagnostics);
        Assert.Empty(run.CompilationDiagnostics);
        string text = Assert.Single(run.GeneratedTexts).Value;
        Assert.DoesNotContain("\"level\"", text);
        Assert.Contains("\"zoom\"", text);
    }

    /// <summary><c>[TemplateRowName]</c> on the hider names the row that the base property opened.</summary>
    [Fact]
    public void ARenamedHidingPropertyTakesTheDerivedRowName()
    {
        GeneratorRun run = GeneratorHarness.Run("""
            using BrutalUi.Core;
            using BrutalUi.Markup;

            namespace Sample;

            internal class Base : Element
            {
                [TemplateRowName("base-name")]
                public string? Text { get; set; }
            }

            [TemplateElement("Hide")]
            internal sealed class Derived : Base
            {
                [TemplateRowName("derived-name")]
                public new string? Text { get; set; }
            }
            """);
        Assert.Empty(run.Diagnostics);
        Assert.Empty(run.CompilationDiagnostics);
        string text = Assert.Single(run.GeneratedTexts).Value;
        Assert.Contains("\"derived-name\"", text);
        Assert.DoesNotContain("\"base-name\"", text);
    }

    /// <summary>A hider that cannot be written gives no row at all: the generated file names no setter.</summary>
    [Fact]
    public void AHidingPropertyWithNoSetterRemovesTheBaseRow()
    {
        GeneratorRun run = GeneratorHarness.Run("""
            using BrutalUi.Core;
            using BrutalUi.Markup;

            namespace Sample;

            internal class Base : Element
            {
                public float Level { get; set; }

                public float Zoom { get; set; }
            }

            [TemplateElement("Hide")]
            internal sealed class Derived : Base
            {
                public new float Level => 0f;
            }
            """);
        Assert.Empty(run.Diagnostics);
        Assert.Empty(run.CompilationDiagnostics);
        string text = Assert.Single(run.GeneratedTexts).Value;
        Assert.DoesNotContain("\"level\"", text);
        Assert.Contains("\"zoom\"", text);
    }

    /// <summary>An <c>override</c> keeps the place of the base property and gives one row.</summary>
    [Fact]
    public void AnOverrideKeepsTheBasePlaceAndGivesOneRow()
    {
        GeneratorRun run = GeneratorHarness.Run("""
            using BrutalUi.Core;
            using BrutalUi.Markup;

            namespace Sample;

            internal class Base : Element
            {
                public float Alpha { get; set; }

                public virtual float Level { get; set; }
            }

            [TemplateElement("Hide")]
            internal sealed class Derived : Base
            {
                public override float Level { get; set; }

                public float Beta { get; set; }
            }
            """);
        Assert.Empty(run.Diagnostics);
        Assert.Empty(run.CompilationDiagnostics);
        string text = Assert.Single(run.GeneratedTexts).Value;

        // One row for the name, and the order is alpha, level, beta: the override keeps the place of the base.
        Assert.Equal(3, text.Split("new global::BrutalUi.Markup.AttributeRow(").Length - 1);
        int alpha = text.IndexOf("\"alpha\"", StringComparison.Ordinal);
        int level = text.IndexOf("\"level\"", StringComparison.Ordinal);
        int beta = text.IndexOf("\"beta\"", StringComparison.Ordinal);
        Assert.True(alpha >= 0, "alpha missing");
        Assert.True(level > alpha, $"level ({level}) must follow alpha ({alpha}).");
        Assert.True(beta > level, $"beta ({beta}) must follow level ({level}).");
    }

    /// <summary>An <c>init</c> property of a target type from metadata gives no row, and no <c>CS8852</c> error.</summary>
    [Fact]
    public void AnInitPropertyFromMetadataGivesNoRow()
    {
        // The target type sits in another assembly, so its property has no declaring syntax: the read of the
        // accessor syntax alone would call it settable.
        MetadataReference metadata = GeneratorHarness.EmitMetadata("""
            using BrutalUi.Core;

            namespace Library;

            public class Widget : Element
            {
                public float Level { get; set; }

                public int Count { get; init; }
            }
            """);
        GeneratorRun run = GeneratorHarness.Run(
            """
            using BrutalUi.Core;
            using BrutalUi.Markup;
            using Library;

            namespace Sample;

            [TemplateElement("Widget", Target = typeof(Widget))]
            internal static class WidgetDeclaration
            {
            }
            """,
            extraReferences: [metadata]);
        Assert.Empty(run.Diagnostics);
        Assert.Empty(run.CompilationDiagnostics);
        string text = Assert.Single(run.GeneratedTexts).Value;
        Assert.Contains("\"level\"", text);
        Assert.DoesNotContain("\"count\"", text);
    }
}
