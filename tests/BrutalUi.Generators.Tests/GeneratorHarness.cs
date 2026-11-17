// The in-memory harness of the generator tests (Decision 13). It builds a CSharpCompilation from the
// trusted platform assemblies plus the real BrutalUi.Markup, BrutalUi.Core, and Brutal.Numerics
// assemblies, then runs TemplateElementGenerator with CSharpGeneratorDriver. No Roslyn testing package.
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using Brutal.Numerics;
using BrutalUi.Core;
using BrutalUi.Markup;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.Text;
using Xunit;

namespace BrutalUi.Generators.Tests;

/// <summary>
/// The result of one harness run: the generated texts by hint name, the generator diagnostics, the errors
/// and warnings of the output compilation, the input compilation, and the run driver. The driver keeps the
/// incremental cache, so a test can run it again on an edited compilation and read the tracked steps.
/// </summary>
/// <param name="GeneratedTexts">The generated texts, keyed by hint name. Empty when the run adds no source.</param>
/// <param name="Diagnostics">The diagnostics the generator reported, in report order.</param>
/// <param name="CompilationDiagnostics">The errors and warnings of the output compilation.</param>
/// <param name="Compilation">The input compilation, so a test can edit one tree and run the driver again.</param>
/// <param name="Driver">The driver after the run, with the incremental cache warm.</param>
internal sealed record GeneratorRun(
    ImmutableDictionary<string, string> GeneratedTexts,
    ImmutableArray<Diagnostic> Diagnostics,
    ImmutableArray<Diagnostic> CompilationDiagnostics,
    CSharpCompilation Compilation,
    GeneratorDriver Driver);

/// <summary>
/// Runs <see cref="TemplateElementGenerator"/> over sample source in memory. The samples parse with
/// <see cref="LanguageVersion.CSharp13"/> (Notes item 12: Roslyn 4.14 knows no C# 14).
/// </summary>
internal static class GeneratorHarness
{
    /// <summary>Sample source with one valid marked <c>Gauge</c> type. The assembly id is <c>Sample</c>.</summary>
    internal const string GaugeSource = """
        using BrutalUi.Core;
        using BrutalUi.Markup;

        namespace Sample;

        [TemplateElement("Gauge")]
        internal sealed class Gauge : Element
        {
            public float Level { get; set; }
        }
        """;

    /// <summary>The parse options of the samples and of the trees a test adds.</summary>
    internal static CSharpParseOptions ParseOptions { get; } = new(LanguageVersion.CSharp13);

    /// <summary>Runs the generator over one source text in one in-memory compilation.</summary>
    /// <param name="source">The sample source.</param>
    /// <param name="assemblyName">The assembly name of the compilation, for the register call id.</param>
    /// <param name="extraReferences">References beyond the platform and the brutal-ui assemblies, for a sample
    /// that reads a target type from metadata. Empty by default.</param>
    /// <returns>The run result.</returns>
    internal static GeneratorRun Run(string source, string assemblyName = "Sample", MetadataReference[]? extraReferences = null)
    {
        CSharpCompilation compilation = CreateCompilation(source, assemblyName, extraReferences);
        // Roslyn 4.14 has no Create(IEnumerable<IIncrementalGenerator>, ...) overload with driver options.
        // The public wrapper AsSourceGenerator() carries the incremental generator through the
        // Create(IEnumerable<ISourceGenerator>, ...) overload, and the driver unwraps it again.
        GeneratorDriver driver = CSharpGeneratorDriver.Create(
            generators: new IIncrementalGenerator[] { new TemplateElementGenerator() }.Select(static g => g.AsSourceGenerator()),
            parseOptions: ParseOptions,
            driverOptions: new GeneratorDriverOptions(default, trackIncrementalGeneratorSteps: true));

        driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out Compilation output, out _);
        return new GeneratorRun(
            Texts(driver),
            driver.GetRunResult().Diagnostics,
            CompileIssues(output),
            compilation,
            driver);
    }

    /// <summary>Runs the cached driver again on an edited compilation and returns the new run result.</summary>
    /// <param name="run">The previous run, whose driver keeps the cache.</param>
    /// <param name="compilation">The edited compilation.</param>
    /// <returns>The new <see cref="GeneratorRunResult"/> of the generator.</returns>
    internal static GeneratorRunResult Rerun(GeneratorRun run, CSharpCompilation compilation)
    {
        return run.Driver.RunGenerators(compilation).GetRunResult().Results.Single();
    }

    /// <summary>Compiles one source text to an in-memory assembly and gives its metadata reference. A sample
    /// type that derives from the assembly reads its properties from metadata, with no declaring syntax.</summary>
    /// <param name="source">The source text of the assembly.</param>
    /// <param name="assemblyName">The assembly name.</param>
    /// <returns>The reference to the emitted assembly.</returns>
    internal static MetadataReference EmitMetadata(string source, string assemblyName = "Metadata")
    {
        var references = BuildReferences(null);
        SyntaxTree tree = CSharpSyntaxTree.ParseText(source, ParseOptions, path: assemblyName + ".cs");
        CSharpCompilation compilation = CSharpCompilation.Create(
            assemblyName,
            new[] { tree },
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, nullableContextOptions: NullableContextOptions.Enable));
        var peStream = new MemoryStream();
        EmitResult result = compilation.Emit(peStream);
        Assert.True(result.Success, "The metadata assembly did not build: " + string.Join(" ",
            result.Diagnostics.Where(static d => d.Severity == DiagnosticSeverity.Error).Select(static d => d.ToString())));
        peStream.Position = 0;

        // The stream stays open: the reference reads it during the run.
        return MetadataReference.CreateFromStream(peStream);
    }

    /// <summary>Adds one tree to a compilation and returns it.</summary>
    /// <param name="compilation">The compilation to edit.</param>
    /// <param name="source">The source text of the new tree.</param>
    /// <param name="path">The file path of the new tree.</param>
    /// <returns>The edited compilation.</returns>
    internal static CSharpCompilation WithTree(CSharpCompilation compilation, string source, string path)
    {
        SyntaxTree tree = CSharpSyntaxTree.ParseText(source, ParseOptions, path: path);
        return compilation.AddSyntaxTrees(tree);
    }

    /// <summary>Gives the reasons of the outputs of one tracked step.</summary>
    /// <param name="result">The run result to read.</param>
    /// <param name="trackingName">The tracking name set in the pipeline.</param>
    /// <returns>One reason per output of each step with that name. Empty when the name is not tracked.</returns>
    internal static IReadOnlyList<IncrementalStepRunReason> StepReasons(GeneratorRunResult result, string trackingName)
    {
        var reasons = new List<IncrementalStepRunReason>();
        if (result.TrackedSteps.TryGetValue(trackingName, out ImmutableArray<IncrementalGeneratorRunStep> steps))
        {
            foreach (IncrementalGeneratorRunStep step in steps)
            {
                foreach (object output in step.Outputs)
                {
                    reasons.Add(OutputReason(output));
                }
            }
        }

        return reasons;
    }

    /// <summary>Gives the reasons of the outputs of the tracked source output step.</summary>
    /// <param name="result">The run result to read.</param>
    /// <returns>One reason per output of each source output step. Empty when none.</returns>
    internal static IReadOnlyList<IncrementalStepRunReason> SourceOutputReasons(GeneratorRunResult result)
    {
        var reasons = new List<IncrementalStepRunReason>();
        foreach (ImmutableArray<IncrementalGeneratorRunStep> steps in result.TrackedOutputSteps.Values)
        {
            foreach (IncrementalGeneratorRunStep step in steps)
            {
                foreach (object output in step.Outputs)
                {
                    reasons.Add(OutputReason(output));
                }
            }
        }

        return reasons;
    }

    /// <summary>Gives the one-based line and column of a diagnostic.</summary>
    /// <param name="diagnostic">The diagnostic to read.</param>
    /// <returns>The line and the column, each counted from 1.</returns>
    internal static (int Line, int Column) Position(Diagnostic diagnostic)
    {
        LinePosition start = diagnostic.Location.GetLineSpan().StartLinePosition;
        return (start.Line + 1, start.Character + 1);
    }

    /// <summary>Asserts the id, the severity, the line and column, and the full message of one diagnostic.</summary>
    /// <param name="diagnostic">The diagnostic to check.</param>
    /// <param name="id">The expected id.</param>
    /// <param name="line">The expected one-based line.</param>
    /// <param name="column">The expected one-based column.</param>
    /// <param name="message">The expected message text.</param>
    internal static void AssertDiagnostic(Diagnostic diagnostic, string id, int line, int column, string message)
    {
        Assert.Equal(id, diagnostic.Id);
        Assert.Equal(DiagnosticSeverity.Error, diagnostic.Severity);
        (int actualLine, int actualColumn) = Position(diagnostic);
        Assert.Equal(line, actualLine);
        Assert.Equal(column, actualColumn);
        Assert.Equal(message, diagnostic.GetMessage());
    }

    /// <summary>Reads the reason of one step output. The driver boxes each output as a
    /// <c>ValueTuple&lt;object, IncrementalStepRunReason&gt;</c>; the reason is the second item.</summary>
    /// <param name="output">The boxed output object.</param>
    /// <returns>The reason value.</returns>
    private static IncrementalStepRunReason OutputReason(object output)
    {
        if (output is ValueTuple<object, IncrementalStepRunReason> pair)
        {
            return pair.Item2;
        }

        throw new InvalidOperationException("A tracked step output has an unexpected shape: " + output.GetType().FullName);
    }

    /// <summary>Collects the generated texts by hint name.</summary>
    /// <param name="driver">The run driver.</param>
    /// <returns>The texts, keyed by hint name.</returns>
    private static ImmutableDictionary<string, string> Texts(GeneratorDriver driver)
    {
        var texts = ImmutableDictionary<string, string>.Empty;
        foreach (GeneratedSourceResult generated in driver.GetRunResult().Results.Single().GeneratedSources)
        {
            texts = texts.Add(generated.HintName, generated.SourceText.ToString());
        }

        return texts;
    }

    /// <summary>Collects the errors and warnings of one compilation.</summary>
    /// <param name="compilation">The compilation to read.</param>
    /// <returns>The errors and warnings.</returns>
    private static ImmutableArray<Diagnostic> CompileIssues(Compilation compilation)
    {
        return compilation.GetDiagnostics()
            .Where(static d => d.Severity is DiagnosticSeverity.Error or DiagnosticSeverity.Warning)
            .ToImmutableArray();
    }

    /// <summary>Builds one in-memory compilation with the sample source.</summary>
    /// <param name="source">The sample source text.</param>
    /// <param name="assemblyName">The assembly name of the compilation.</param>
    /// <param name="extraReferences">References beyond the platform and the brutal-ui assemblies, or <see langword="null"/>.</param>
    /// <returns>The compilation, ready for the driver.</returns>
    private static CSharpCompilation CreateCompilation(string source, string assemblyName, MetadataReference[]? extraReferences)
    {
        List<MetadataReference> references = BuildReferences(extraReferences);
        SyntaxTree tree = CSharpSyntaxTree.ParseText(source, ParseOptions, path: assemblyName + ".xml.cs");
        return CSharpCompilation.Create(
            assemblyName,
            new[] { tree },
            references,
            new CSharpCompilationOptions(
                OutputKind.DynamicallyLinkedLibrary,
                nullableContextOptions: NullableContextOptions.Enable));
    }

    /// <summary>Collects the reference list: the platform assemblies, the three brutal-ui assemblies, and the
    /// extra references of one test.</summary>
    /// <param name="extraReferences">The extra references, or <see langword="null"/>.</param>
    /// <returns>The reference list.</returns>
    private static List<MetadataReference> BuildReferences(MetadataReference[]? extraReferences)
    {
        var references = new List<MetadataReference>();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        string tpa = (string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!;
        foreach (string path in tpa.Split(Path.PathSeparator))
        {
            AddReference(references, seen, path);
        }

        // The samples compile against the real BrutalUi.Markup, BrutalUi.Core, and Brutal.Numerics assemblies.
        AddReference(references, seen, typeof(TemplateLoader).Assembly.Location);
        AddReference(references, seen, typeof(Element).Assembly.Location);
        AddReference(references, seen, typeof(float2).Assembly.Location);
        if (extraReferences is not null)
        {
            references.AddRange(extraReferences);
        }

        return references;
    }

    /// <summary>Adds one assembly file to the reference list, once.</summary>
    /// <param name="references">The list to add to.</param>
    /// <param name="seen">The paths already added.</param>
    /// <param name="path">The assembly file path.</param>
    private static void AddReference(List<MetadataReference> references, HashSet<string> seen, string path)
    {
        if (path.Length > 0 && seen.Add(Path.GetFullPath(path)))
        {
            references.Add(MetadataReference.CreateFromFile(path));
        }
    }
}
