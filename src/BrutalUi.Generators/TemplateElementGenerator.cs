// The incremental pipeline of "The Generator Pipeline": one attribute step, one
// compilation step, and one combine step, each with a tracking name. The models are
// value-equatable and hold no ISymbol, SyntaxNode, Location, or Compilation. The
// transform returns an ElementResult: one model, or the faults of The Diagnostics.
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace BrutalUi.Generators;

/// <summary>
/// Writes the element table of the brutal-ui XML loader for each class marked with
/// <c>BrutalUi.Markup.TemplateElementAttribute</c>.
/// </summary>
/// <remarks>
/// <para>
/// The generator runs at build time only. It reads the marked class, its name, its leaf flag, its factory and
/// create rows, and the public settable properties of the target type, and writes one register method for the
/// assembly. See the element-generator plan, "The Generator Pipeline" and "The Generated Source".
/// </para>
/// <para>
/// Phase 4 adds The Diagnostics. The transform checks The Rules and reports BRUTALUI0001, BRUTALUI0003,
/// BRUTALUI0004, BRUTALUI0005, and BRUTALUI0006. The output step reports them, adds BRUTALUI0002 for a shared
/// XML name, and writes no code for a type with a fault. The other types of the assembly keep their code, so
/// the register call still compiles. The member reads and the check helpers sit in
/// <c>TemplateElementGenerator.Members.cs</c>.
/// </para>
/// </remarks>
[Generator(LanguageNames.CSharp)]
public sealed partial class TemplateElementGenerator : IIncrementalGenerator
{
    private const string AttributeFullName = "BrutalUi.Markup.TemplateElementAttribute";
    private const string FactoryAttributeName = "BrutalUi.Markup.TemplateFactoryAttribute";
    private const string CreateRowAttributeName = "BrutalUi.Markup.TemplateCreateRowAttribute";
    private const string RowNameAttributeName = "BrutalUi.Markup.TemplateRowNameAttribute";
    private const string IgnoreAttributeName = "BrutalUi.Markup.TemplateIgnoreAttribute";
    private const string ElementNamespace = "BrutalUi.Core";
    private const string ElementName = "Element";
    private const string NumericsNamespace = "Brutal.Numerics";
    private const string MarkupNamespace = "BrutalUi.Markup";

    /// <summary>Display format for a type name in a diagnostic message.</summary>
    private static readonly SymbolDisplayFormat MessageFormat = SymbolDisplayFormat.CSharpErrorMessageFormat;

    /// <summary>Global name format for a type reference in the generated source.</summary>
    private static readonly SymbolDisplayFormat GlobalFormat = SymbolDisplayFormat.FullyQualifiedFormat;

    /// <inheritdoc/>
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        IncrementalValuesProvider<ElementResult> results = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                AttributeFullName,
                static (node, _) => node is ClassDeclarationSyntax,
                static (ctx, ct) => ReadElement(ctx, ct))
            .WithTrackingName("TemplateElementModels");

        IncrementalValueProvider<string> assemblyId = context.CompilationProvider
            .Select(static (c, _) => NameRules.ToIdentifier(c.AssemblyName))
            .WithTrackingName("TemplateElementAssemblyId");

        IncrementalValueProvider<(ImmutableArray<ElementResult> Results, string Id)> all =
            results.Collect().Combine(assemblyId).WithTrackingName("TemplateElementSet");

        context.RegisterSourceOutput(all, static (spc, source) => Emit(spc, source));
    }

    /// <summary>Reports each fault, drops the types that share an XML name, and writes one source file for the rest.</summary>
    /// <param name="spc">The source production context.</param>
    /// <param name="source">The collected results and the assembly id.</param>
    private static void Emit(
        SourceProductionContext spc,
        (ImmutableArray<ElementResult> Results, string Id) source)
    {
        var valid = new List<ElementResult>();
        foreach (ElementResult result in source.Results)
        {
            foreach (DiagnosticInfo info in result.Diagnostics)
            {
                spc.ReportDiagnostic(info.ToDiagnostic());
            }

            if (result.Model is not null)
            {
                valid.Add(result);
            }
        }

        // Sort by XML name, then by type name, so a duplicate group and its type list are deterministic.
        valid.Sort(static (a, b) =>
        {
            int byName = string.CompareOrdinal(a.Model!.XmlName, b.Model!.XmlName);
            return byName != 0 ? byName : string.CompareOrdinal(a.TypeName, b.TypeName);
        });

        var models = new List<ElementModel>();
        int start = 0;
        while (start < valid.Count)
        {
            string xmlName = valid[start].Model!.XmlName;
            int end = start + 1;
            while (end < valid.Count && string.Equals(valid[end].Model!.XmlName, xmlName, StringComparison.Ordinal))
            {
                end++;
            }

            if (end - start > 1)
            {
                ReportDuplicateNames(spc, valid, start, end, xmlName);
            }
            else
            {
                models.Add(valid[start].Model!);
            }

            start = end;
        }

        if (models.Count == 0)
        {
            return;
        }

        string text = ElementSourceWriter.Write(source.Id, models);
        spc.AddSource(source.Id + "TemplateElements.g.cs", SourceText.From(text, Encoding.UTF8));
    }

    /// <summary>Reports BRUTALUI0002 on the name argument of each member of a duplicate-name group, and writes
    /// no code for them.</summary>
    /// <param name="spc">The source production context.</param>
    /// <param name="sorted">The valid results, sorted by XML name then type name.</param>
    /// <param name="start">The index of the first member of the group.</param>
    /// <param name="end">The index past the last member of the group.</param>
    /// <param name="xmlName">The shared XML name.</param>
    private static void ReportDuplicateNames(SourceProductionContext spc, List<ElementResult> sorted, int start, int end, string xmlName)
    {
        var names = new StringBuilder();
        for (int index = start; index < end; index++)
        {
            if (index > start)
            {
                names.Append(", ");
            }

            names.Append(sorted[index].TypeName);
        }

        string typeList = names.ToString();
        for (int index = start; index < end; index++)
        {
            DiagnosticInfo info = DiagnosticInfo.Create(
                DiagnosticDescriptors.DuplicateXmlNameId, sorted[index].NameLocation, xmlName, typeList);
            spc.ReportDiagnostic(info.ToDiagnostic());
        }
    }

    /// <summary>Reads one marked class, checks The Rules, and builds its result.</summary>
    /// <param name="ctx">The attribute context: the class symbol, its syntax, and its attributes.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>
    /// The result. <see cref="ElementResult.Model"/> is <see langword="null"/> when a check added a fault, so
    /// the type gets no code. The transform never throws: each read checks the shape before it casts.
    /// </returns>
    private static ElementResult ReadElement(GeneratorAttributeSyntaxContext ctx, CancellationToken ct)
    {
        if (ctx.Attributes.IsDefaultOrEmpty
            || ctx.TargetSymbol is not INamedTypeSymbol classSymbol
            || ctx.TargetNode is not ClassDeclarationSyntax classSyntax)
        {
            return new ElementResult(null, default, null, string.Empty);
        }

        AttributeData attribute = ctx.Attributes[0];
        string xmlName = string.Empty;
        LocationInfo? nameLocation = null;
        if (attribute.ConstructorArguments.Length == 1
            && attribute.ConstructorArguments[0].Value is string argument)
        {
            xmlName = argument;
            nameLocation = NameArgumentLocation(attribute, ct);
        }

        string typeName = classSymbol.ToDisplayString(MessageFormat);
        LocationInfo? typeLocation = LocationInfo.From(classSyntax.Identifier.GetLocation());
        var faults = ImmutableArray.CreateBuilder<DiagnosticInfo>();

        (bool isLeaf, INamedTypeSymbol? target) = ReadNamedArguments(attribute);

        // A type that breaks The element type gets BRUTALUI0006 and no further checks.
        if (!CheckElementType(faults, classSymbol, target, xmlName, typeLocation))
        {
            return new ElementResult(null, new EquatableArray<DiagnosticInfo>(faults.ToImmutable()), nameLocation, typeName);
        }

        INamedTypeSymbol elementType = target ?? classSymbol;
        var createSources = new List<RowSource>();
        if (!CheckCreate(faults, classSymbol, elementType, typeLocation, ct, createSources,
            out string? factoryCall, out EquatableArray<CreateRowModel> createRows))
        {
            return new ElementResult(null, new EquatableArray<DiagnosticInfo>(faults.ToImmutable()), nameLocation, typeName);
        }

        var setSources = new List<RowSource>();
        EquatableArray<SetRowModel> rows = WalkProperties(elementType, ct, faults, setSources);
        CheckRowNames(faults, createSources, setSources);

        if (faults.Count > 0)
        {
            return new ElementResult(null, new EquatableArray<DiagnosticInfo>(faults.ToImmutable()), nameLocation, typeName);
        }

        ElementModel model = new(
            xmlName,
            isLeaf,
            elementType.ToDisplayString(GlobalFormat),
            factoryCall,
            createRows,
            rows);
        return new ElementResult(model, default, nameLocation, typeName);
    }

    /// <summary>
    /// Checks The element type and the XML name, and adds BRUTALUI0006 on the class name for each fault.
    /// </summary>
    /// <param name="faults">The fault list to add to.</param>
    /// <param name="classSymbol">The marked class.</param>
    /// <param name="target">The target type of the declaration form, or <see langword="null"/>.</param>
    /// <param name="xmlName">The XML name from the attribute.</param>
    /// <param name="location">The location of the class name.</param>
    /// <returns><see langword="true"/> when the type passed every check.</returns>
    private static bool CheckElementType(
        ImmutableArray<DiagnosticInfo>.Builder faults,
        INamedTypeSymbol classSymbol,
        INamedTypeSymbol? target,
        string xmlName,
        LocationInfo? location)
    {
        string name = classSymbol.ToDisplayString(MessageFormat);

        if (!IsValidXmlName(xmlName))
        {
            faults.Add(DiagnosticInfo.Create(
                DiagnosticDescriptors.BadElementTypeId, location, name, "the XML name is not a valid XML local name"));
        }

        if (target is null)
        {
            if (classSymbol.IsStatic)
            {
                faults.Add(DiagnosticInfo.Create(
                    DiagnosticDescriptors.BadElementTypeId, location, name, "it is a static class"));
            }

            if (!DerivesFromElement(classSymbol))
            {
                faults.Add(DiagnosticInfo.Create(
                    DiagnosticDescriptors.BadElementTypeId, location, name, "it does not derive from BrutalUi.Core.Element"));
            }
        }
        else
        {
            if (!classSymbol.IsStatic)
            {
                faults.Add(DiagnosticInfo.Create(
                    DiagnosticDescriptors.BadElementTypeId, location, name, "it is not a static class"));
            }

            if (target.IsUnboundGenericType)
            {
                faults.Add(DiagnosticInfo.Create(
                    DiagnosticDescriptors.BadElementTypeId, location, name, "the Target type is an open generic type"));
            }
            else if (!DerivesFromElement(target))
            {
                faults.Add(DiagnosticInfo.Create(
                    DiagnosticDescriptors.BadElementTypeId, location, name, "the Target type does not derive from BrutalUi.Core.Element"));
            }
        }

        // Both forms run the generic check. The generated file names the class with its global display
        // string, and an open type parameter of the class or of a containing class cannot compile there.
        if (classSymbol.IsGenericType || IsNestedInGenericType(classSymbol))
        {
            faults.Add(DiagnosticInfo.Create(
                DiagnosticDescriptors.BadElementTypeId, location, name, "it is a generic type"));
        }

        if (!HasReachableAccess(classSymbol))
        {
            faults.Add(DiagnosticInfo.Create(
                DiagnosticDescriptors.BadElementTypeId, location, name, "it or a containing class is not internal or public"));
        }

        return faults.Count == 0;
    }

    /// <summary>Checks that the marked class and each containing class have internal or public access.</summary>
    /// <param name="classSymbol">The marked class.</param>
    /// <returns><see langword="true"/> when the generated class can reach the marked class.</returns>
    private static bool HasReachableAccess(INamedTypeSymbol classSymbol)
    {
        if (classSymbol.DeclaredAccessibility is not (Accessibility.Internal or Accessibility.Public))
        {
            return false;
        }

        for (INamedTypeSymbol? outer = classSymbol.ContainingType; outer is not null; outer = outer.ContainingType)
        {
            if (outer.DeclaredAccessibility is not (Accessibility.Internal or Accessibility.Public))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>Checks that no containing type is generic.</summary>
    /// <param name="classSymbol">The marked class.</param>
    /// <returns><see langword="true"/> when a containing type has type parameters.</returns>
    private static bool IsNestedInGenericType(INamedTypeSymbol classSymbol)
    {
        for (INamedTypeSymbol? outer = classSymbol.ContainingType; outer is not null; outer = outer.ContainingType)
        {
            if (outer.IsGenericType)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>Checks that the type is <c>BrutalUi.Core.Element</c> or derives from it.</summary>
    /// <param name="classSymbol">The element type to check.</param>
    /// <returns><see langword="true"/> when the type is <c>Element</c> or a base type is <c>Element</c>.</returns>
    private static bool DerivesFromElement(INamedTypeSymbol classSymbol)
    {
        for (INamedTypeSymbol? type = classSymbol; type is not null; type = type.BaseType)
        {
            if (IsCoreType(type, ElementName))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>Checks the namespace of a type by its display name.</summary>
    /// <param name="type">The type to check.</param>
    /// <param name="namespaceName">The expected namespace, for example <c>Brutal.Numerics</c>.</param>
    /// <returns><see langword="true"/> when the type sits in that namespace.</returns>
    private static bool InNamespace(ITypeSymbol type, string namespaceName)
    {
        return type.ContainingNamespace is { IsGlobalNamespace: false } ns
            && string.Equals(ns.ToDisplayString(), namespaceName, StringComparison.Ordinal);
    }

    /// <summary>Checks a type of the <c>BrutalUi.Core</c> namespace by name.</summary>
    /// <param name="type">The type to check.</param>
    /// <param name="name">The expected type name, for example <c>Element</c>.</param>
    /// <returns><see langword="true"/> when the type is <c>BrutalUi.Core.{name}</c>.</returns>
    private static bool IsCoreType(ITypeSymbol type, string name)
    {
        return string.Equals(type.Name, name, StringComparison.Ordinal) && InNamespace(type, ElementNamespace);
    }
}
