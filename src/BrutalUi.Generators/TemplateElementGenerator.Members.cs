// The member reads and the rule checks of the generator: the factory and its create rows (BRUTALUI0004), the
// constructor rule (BRUTALUI0001), the property walk with the row-name rules (BRUTALUI0003 and BRUTALUI0005),
// and the attribute lookups of The Rules. Each attribute matches by its full metadata name.
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace BrutalUi.Generators;

/// <summary>The member reads and the rule checks of <see cref="TemplateElementGenerator"/>.</summary>
public sealed partial class TemplateElementGenerator
{
    /// <summary>The row names that the written base rows take.</summary>
    private static readonly string[] BaseRowNames = { "name", "class", "pickable", "focusable" };

    /// <summary>
    /// Checks The factory and the constructor rule (Q3), and adds BRUTALUI0004 and BRUTALUI0001 faults. With no
    /// factory, the generator needs a no-parameter constructor of the element type: public, or internal when
    /// the element type sits in the compiling assembly. An abstract element type has no usable constructor.
    /// </summary>
    /// <param name="faults">The fault list to add to.</param>
    /// <param name="classSymbol">The marked class.</param>
    /// <param name="elementType">The element type: the target of the declaration form, or the marked class.</param>
    /// <param name="typeLocation">The location of the class name, for BRUTALUI0001.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <param name="createSources">The create-row name sources, for BRUTALUI0005.</param>
    /// <param name="factoryCall">The global factory call, or <see langword="null"/> for the constructor kind.</param>
    /// <param name="createRows">The create rows of the factory, in source order.</param>
    /// <returns><see langword="true"/> when a valid factory or a usable constructor gives the create kind.</returns>
    private static bool CheckCreate(
        ImmutableArray<DiagnosticInfo>.Builder faults,
        INamedTypeSymbol classSymbol,
        INamedTypeSymbol elementType,
        LocationInfo? typeLocation,
        CancellationToken ct,
        List<RowSource> createSources,
        out string? factoryCall,
        out EquatableArray<CreateRowModel> createRows)
    {
        factoryCall = null;
        createRows = default;

        var factories = new List<IMethodSymbol>();
        foreach (ISymbol member in classSymbol.GetMembers())
        {
            ct.ThrowIfCancellationRequested();
            if (member is not IMethodSymbol method)
            {
                continue;
            }

            if (FindAttribute(method, FactoryAttributeName) is not null)
            {
                factories.Add(method);
            }
            else if (FindAttribute(method, CreateRowAttributeName) is not null)
            {
                faults.Add(DiagnosticInfo.Create(
                    DiagnosticDescriptors.BadFactoryId,
                    LocationInfo.FromSymbol(method),
                    method.Name,
                    "it has [TemplateCreateRow] but no [TemplateFactory]"));
            }
        }

        if (factories.Count > 1)
        {
            foreach (IMethodSymbol extra in factories.GetRange(1, factories.Count - 1))
            {
                faults.Add(DiagnosticInfo.Create(
                    DiagnosticDescriptors.BadFactoryId,
                    LocationInfo.FromSymbol(extra),
                    extra.Name,
                    "the class holds more than one factory"));
            }

            return false;
        }

        if (factories.Count == 1)
        {
            int mark = faults.Count;
            CheckFactory(faults, factories[0]);
            if (faults.Count > mark)
            {
                return false;
            }

            factoryCall = GlobalFactoryCall(factories[0]);
            createRows = ReadCreateRows(factories[0], createSources, ct);
            return true;
        }

        if (!HasUsableConstructor(elementType, classSymbol))
        {
            faults.Add(DiagnosticInfo.Create(
                DiagnosticDescriptors.NoUsableConstructorAndNoFactoryId,
                typeLocation,
                elementType.ToDisplayString(MessageFormat)));
        }

        return true;
    }

    /// <summary>Builds the global factory call, with the args argument, for the generated create lambda.</summary>
    /// <param name="factory">The marked method.</param>
    /// <returns>A call text such as <c>global::Ns.Meter.Create(args)</c>.</returns>
    private static string GlobalFactoryCall(IMethodSymbol factory)
    {
        INamedTypeSymbol owner = factory.ContainingType;
        return owner.ToDisplayString(GlobalFormat) + "." + factory.Name + "(args)";
    }

    /// <summary>Reads the <c>[TemplateCreateRow]</c> attributes of a factory, in source order.</summary>
    /// <param name="factory">The marked method.</param>
    /// <param name="createSources">The name sources, for BRUTALUI0005.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The create rows, in the order of the attributes in the source.</returns>
    private static EquatableArray<CreateRowModel> ReadCreateRows(
        IMethodSymbol factory,
        List<RowSource> createSources,
        CancellationToken ct)
    {
        var marked = new List<AttributeData>();
        foreach (AttributeData attribute in factory.GetAttributes())
        {
            if (FullName(attribute) == CreateRowAttributeName)
            {
                marked.Add(attribute);
            }
        }

        marked.Sort(static (a, b) => AttributeStart(a).CompareTo(AttributeStart(b)));

        var rows = ImmutableArray.CreateBuilder<CreateRowModel>(marked.Count);
        foreach (AttributeData attribute in marked)
        {
            if (attribute.ConstructorArguments.Length != 1
                || attribute.ConstructorArguments[0].Value is not string name)
            {
                continue;
            }

            bool isRequired = false;
            foreach (KeyValuePair<string, TypedConstant> named in attribute.NamedArguments)
            {
                if (named.Key == "IsRequired" && named.Value.Value is bool required)
                {
                    isRequired = required;
                }
            }

            rows.Add(new CreateRowModel(name, isRequired));
            createSources.Add(new RowSource(name, factory.Name, AttributeLocation(attribute, ct)));
        }

        return new EquatableArray<CreateRowModel>(rows.ToImmutable());
    }

    /// <summary>
    /// Walks the properties of the element type and each base type up to, but not including, <c>Element</c>. The
    /// base type nearest to <c>Element</c> comes first; each type keeps its declaration order. A property that
    /// overrides or hides a base property keeps the place of the base property, and the visible (the most
    /// derived) property decides the row: its read kind, its row name, and its ignore. <c>[TemplateIgnore]</c>
    /// drops a property, and <c>[TemplateRowName]</c> replaces the kebab-case name. The walk adds BRUTALUI0003
    /// for a <c>[TemplateRowName]</c> on a property that gives no row (A5).
    /// </summary>
    /// <param name="classSymbol">The element type.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <param name="faults">The fault list to add to.</param>
    /// <param name="setSources">The set-row name sources, for BRUTALUI0005.</param>
    /// <returns>The set rows, in output order.</returns>
    private static EquatableArray<SetRowModel> WalkProperties(
        INamedTypeSymbol classSymbol,
        CancellationToken ct,
        ImmutableArray<DiagnosticInfo>.Builder faults,
        List<RowSource> setSources)
    {
        var chain = new List<INamedTypeSymbol>();
        for (INamedTypeSymbol? type = classSymbol; type is not null && !IsCoreType(type, ElementName); type = type.BaseType)
        {
            chain.Add(type);
        }

        chain.Reverse();

        var rows = ImmutableArray.CreateBuilder<SetRowModel>();
        var rowSlots = new List<SetRowModel?>();
        var sourceSlots = new List<RowSource?>();
        var places = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (INamedTypeSymbol type in chain)
        {
            ct.ThrowIfCancellationRequested();
            foreach (ISymbol member in type.GetMembers())
            {
                ct.ThrowIfCancellationRequested();
                if (member is not IPropertySymbol property)
                {
                    continue;
                }

                AttributeData? renamed = FindAttribute(property, RowNameAttributeName);
                bool ignored = FindAttribute(property, IgnoreAttributeName) is not null;
                ReadKind? kind = KindOf(property.Type, out string enumType, out EquatableArray<KeywordModel> keywords);

                // A slot holds the row of the visible property, or null when the visible property gives no row.
                SetRowModel? row = null;
                RowSource? source = null;
                if (!ignored && GivesRow(property, kind))
                {
                    string rowName = NameRules.ToKebabCase(property.Name);
                    if (renamed is not null
                        && renamed.ConstructorArguments.Length == 1
                        && renamed.ConstructorArguments[0].Value is string customName)
                    {
                        rowName = customName;
                    }

                    row = new SetRowModel(rowName, property.Name, kind!.Value, enumType, keywords);
                    source = new RowSource(rowName, property.Name, LocationInfo.FromSymbol(property));
                }
                else if (!ignored && renamed is not null)
                {
                    faults.Add(DiagnosticInfo.Create(
                        DiagnosticDescriptors.RowNameOnNoRowId,
                        AttributeLocation(renamed, ct),
                        property.Name));
                }

                if (places.TryGetValue(property.Name, out int place))
                {
                    // The visible property takes over the place that the base property of the name took.
                    rowSlots[place] = row;
                    sourceSlots[place] = source;
                }
                else if (row is not null)
                {
                    places.Add(property.Name, rowSlots.Count);
                    rowSlots.Add(row);
                    sourceSlots.Add(source);
                }
            }
        }

        for (int index = 0; index < rowSlots.Count; index++)
        {
            if (rowSlots[index] is SetRowModel kept && sourceSlots[index] is RowSource keptSource)
            {
                rows.Add(kept);
                setSources.Add(keptSource);
            }
        }

        return new EquatableArray<SetRowModel>(rows.ToImmutable());
    }

    /// <summary>Checks that a property gives a row, before the name rules.</summary>
    /// <param name="property">The property to check.</param>
    /// <param name="kind">The read kind of the property type, or <see langword="null"/>.</param>
    /// <returns>
    /// <see langword="true"/> when the property is an instance property, not an indexer, public, with a public
    /// set accessor that is not <c>init</c>, and of a supported type.
    /// </returns>
    private static bool GivesRow(IPropertySymbol property, ReadKind? kind)
    {
        if (property.IsStatic || property.IsIndexer || property.DeclaredAccessibility != Accessibility.Public)
        {
            return false;
        }

        IMethodSymbol? setter = property.SetMethod;
        return setter is not null
            && setter.DeclaredAccessibility == Accessibility.Public
            && !IsInit(property)
            && kind is not null;
    }

    /// <summary>
    /// Checks the set accessor of a property for <c>init</c>. The method reads <c>IMethodSymbol.IsInitOnly</c>:
    /// the public flag of Roslyn 4.14 that reports an <c>init</c> accessor of a source property and of a property
    /// from metadata. The accessor syntax read stays as the second route for a source property. (CR-3: a
    /// metadata-only syntax read let an <c>init</c> property of a target type from another assembly give a row,
    /// and the generated assignment gave <c>CS8852</c>.)
    /// </summary>
    /// <param name="property">The property to check.</param>
    /// <returns><see langword="true"/> when the set accessor is <c>init</c>.</returns>
    private static bool IsInit(IPropertySymbol property)
    {
        if (property.SetMethod is not null && property.SetMethod.IsInitOnly)
        {
            return true;
        }

        foreach (SyntaxReference reference in property.DeclaringSyntaxReferences)
        {
            if (reference.GetSyntax() is PropertyDeclarationSyntax declaration
                && declaration.AccessorList is not null)
            {
                foreach (AccessorDeclarationSyntax accessor in declaration.AccessorList.Accessors)
                {
                    if (accessor.IsKind(SyntaxKind.InitAccessorDeclaration))
                    {
                        return true;
                    }
                }
            }
        }

        return false;
    }

    /// <summary>Maps a property type to the read kind of the generated row.</summary>
    /// <param name="type">The property type.</param>
    /// <param name="enumType">The global name of the enum type, or an empty string for another kind.</param>
    /// <param name="keywords">The keywords of an enum row in declaration order, or empty for another kind.</param>
    /// <returns>The kind, or <see langword="null"/> when no read supports the type.</returns>
    private static ReadKind? KindOf(ITypeSymbol type, out string enumType, out EquatableArray<KeywordModel> keywords)
    {
        enumType = string.Empty;
        keywords = default;

        switch (type.SpecialType)
        {
            case SpecialType.System_String:
                return ReadKind.Text;
            case SpecialType.System_Single:
                return ReadKind.Number;
            case SpecialType.System_Int32:
                return ReadKind.Integer;
            case SpecialType.System_Boolean:
                return ReadKind.Bool;
        }

        if (type is INamedTypeSymbol { TypeKind: TypeKind.Enum } enumSymbol)
        {
            enumType = enumSymbol.ToDisplayString(GlobalFormat);
            keywords = EnumKeywords(enumSymbol);
            return ReadKind.Keyword;
        }

        // The metadata names of float2 and float4 are confirmed against the Brutal.Numerics assembly.
        // See docs/features/markup/element-generator/context.md, task 1.8.
        switch (type.Name)
        {
            case "float2" when InNamespace(type, NumericsNamespace):
                return ReadKind.Numbers2;
            case "float4" when InNamespace(type, NumericsNamespace):
                return ReadKind.Numbers4;
            case "Rgba8" when InNamespace(type, ElementNamespace):
                return ReadKind.Color;
            case "TextureHandle" when InNamespace(type, ElementNamespace):
                return ReadKind.Texture;
            default:
                return null;
        }
    }

    /// <summary>Reads the keyword of each enum member, in declaration order.</summary>
    /// <param name="enumSymbol">The enum type.</param>
    /// <returns>The keywords, with the global member names.</returns>
    private static EquatableArray<KeywordModel> EnumKeywords(INamedTypeSymbol enumSymbol)
    {
        string typeName = enumSymbol.ToDisplayString(GlobalFormat);
        var keywords = ImmutableArray.CreateBuilder<KeywordModel>();
        foreach (ISymbol member in enumSymbol.GetMembers())
        {
            if (member is IFieldSymbol { IsConst: true })
            {
                keywords.Add(new KeywordModel(NameRules.ToKebabCase(member.Name), typeName + "." + member.Name));
            }
        }

        return new EquatableArray<KeywordModel>(keywords.ToImmutable());
    }
}
