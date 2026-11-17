// The rule checks of the generator, moved out of TemplateElementGenerator.cs and Members.cs so each file
// stays under 400 lines (Risk 9). This file holds the attribute reads, the factory and constructor checks for
// BRUTALUI0001 and BRUTALUI0004, the row-name checks for BRUTALUI0005, and the XML name check shared with
// BRUTALUI0006.
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Xml;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace BrutalUi.Generators;

/// <summary>The rule checks of <see cref="TemplateElementGenerator"/>.</summary>
public sealed partial class TemplateElementGenerator
{
    /// <summary>Reads the <c>IsLeaf</c> and <c>Target</c> named arguments of the attribute.</summary>
    /// <param name="attribute">The attribute data.</param>
    /// <returns>The leaf flag, and the target type of the declaration form or <see langword="null"/>.</returns>
    private static (bool IsLeaf, INamedTypeSymbol? Target) ReadNamedArguments(AttributeData attribute)
    {
        bool isLeaf = false;
        INamedTypeSymbol? target = null;
        foreach (KeyValuePair<string, TypedConstant> named in attribute.NamedArguments)
        {
            if (named.Key == "IsLeaf" && named.Value.Value is bool leaf)
            {
                isLeaf = leaf;
            }
            else if (named.Key == "Target" && named.Value.Value is INamedTypeSymbol type)
            {
                target = type;
            }
        }

        return (isLeaf, target);
    }

    /// <summary>Locates the name argument of the attribute, for BRUTALUI0002.</summary>
    /// <param name="attribute">The attribute data.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The location of the first argument, or <see langword="null"/> when the syntax is not there.</returns>
    private static LocationInfo? NameArgumentLocation(AttributeData attribute, CancellationToken ct)
    {
        SyntaxNode? application = attribute.ApplicationSyntaxReference?.GetSyntax(ct);
        if (application is AttributeSyntax syntax
            && syntax.ArgumentList is not null
            && syntax.ArgumentList.Arguments.Count > 0)
        {
            return LocationInfo.From(syntax.ArgumentList.Arguments[0].GetLocation());
        }

        return null;
    }

    /// <summary>Gives the first attribute of a symbol with this full metadata name.</summary>
    /// <param name="symbol">The symbol to read.</param>
    /// <param name="fullName">The full metadata name, for example <c>BrutalUi.Markup.TemplateIgnoreAttribute</c>.</param>
    /// <returns>The attribute, or <see langword="null"/> when the symbol holds none.</returns>
    private static AttributeData? FindAttribute(ISymbol symbol, string fullName)
    {
        foreach (AttributeData attribute in symbol.GetAttributes())
        {
            if (FullName(attribute) == fullName)
            {
                return attribute;
            }
        }

        return null;
    }

    /// <summary>Gives the full metadata name of an attribute instance.</summary>
    /// <param name="attribute">The attribute to read.</param>
    /// <returns>The name, or an empty string for an error type.</returns>
    private static string FullName(AttributeData attribute)
    {
        return attribute.AttributeClass?.ToDisplayString() ?? string.Empty;
    }

    /// <summary>Gives the source start of an attribute application.</summary>
    /// <param name="attribute">The attribute to order.</param>
    /// <returns>The character offset, or 0 for a symbol from metadata.</returns>
    private static int AttributeStart(AttributeData attribute)
    {
        return attribute.ApplicationSyntaxReference?.Span.Start ?? 0;
    }

    /// <summary>Copies the source location of an attribute application.</summary>
    /// <param name="attribute">The attribute to locate.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The location, or <see langword="null"/> for an attribute from metadata.</returns>
    private static LocationInfo? AttributeLocation(AttributeData attribute, CancellationToken ct)
    {
        SyntaxNode? syntax = attribute.ApplicationSyntaxReference?.GetSyntax(ct);
        return syntax is null ? null : LocationInfo.From(syntax.GetLocation());
    }

    /// <summary>Checks that a name is a valid XML local name.</summary>
    /// <param name="name">The name to check.</param>
    /// <returns><see langword="true"/> when <paramref name="name"/> is a valid XML local name.</returns>
    /// <remarks>
    /// <see cref="XmlConvert.IsStartNCNameChar(char)"/> and <see cref="XmlConvert.IsNCNameChar(char)"/> exist in
    /// <c>netstandard2.0</c>. A character outside the basic multilingual plane fails. The XML names of the game
    /// stay in that plane.
    /// </remarks>
    private static bool IsValidXmlName(string name)
    {
        if (name.Length == 0 || !XmlConvert.IsStartNCNameChar(name[0]))
        {
            return false;
        }

        for (int index = 1; index < name.Length; index++)
        {
            if (!XmlConvert.IsNCNameChar(name[index]))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>Checks one factory method against The Rules, and adds BRUTALUI0004 on the method name for each fault.</summary>
    /// <param name="faults">The fault list to add to.</param>
    /// <param name="factory">The marked method.</param>
    private static void CheckFactory(ImmutableArray<DiagnosticInfo>.Builder faults, IMethodSymbol factory)
    {
        LocationInfo? location = LocationInfo.FromSymbol(factory);
        string name = factory.Name;

        if (!factory.IsStatic)
        {
            faults.Add(DiagnosticInfo.Create(DiagnosticDescriptors.BadFactoryId, location, name, "it is not static"));
        }

        if (factory.TypeParameters.Length > 0)
        {
            faults.Add(DiagnosticInfo.Create(DiagnosticDescriptors.BadFactoryId, location, name, "it is a generic method"));
        }

        if (factory.DeclaredAccessibility is not (Accessibility.Internal or Accessibility.Public))
        {
            faults.Add(DiagnosticInfo.Create(
                DiagnosticDescriptors.BadFactoryId, location, name, "it is not internal or public"));
        }

        if (!HasCreateArgsParameter(factory))
        {
            faults.Add(DiagnosticInfo.Create(
                DiagnosticDescriptors.BadFactoryId, location, name, "it does not have one parameter of type ElementCreateArgs"));
        }

        if (factory.ReturnsVoid
            || factory.ReturnType is not INamedTypeSymbol returnType
            || !DerivesFromElement(returnType))
        {
            faults.Add(DiagnosticInfo.Create(
                DiagnosticDescriptors.BadFactoryId, location, name, "it does not return Element or a type derived from Element"));
        }
    }

    /// <summary>Checks that the method has one by-value parameter of type <c>BrutalUi.Markup.ElementCreateArgs</c>.</summary>
    /// <param name="factory">The method to check.</param>
    /// <returns><see langword="true"/> when the parameter shape is valid.</returns>
    private static bool HasCreateArgsParameter(IMethodSymbol factory)
    {
        return factory.Parameters.Length == 1
            && factory.Parameters[0].RefKind == RefKind.None
            && factory.Parameters[0].Type.Name == "ElementCreateArgs"
            && InNamespace(factory.Parameters[0].Type, MarkupNamespace);
    }

    /// <summary>Checks the no-parameter constructor rule of Q3.</summary>
    /// <param name="elementType">The element type to create.</param>
    /// <param name="classSymbol">The marked class, in the compiling assembly.</param>
    /// <returns>
    /// <see langword="true"/> when the element type is not abstract and has a no-parameter constructor that is
    /// public, or internal with the element type in the compiling assembly.
    /// </returns>
    private static bool HasUsableConstructor(INamedTypeSymbol elementType, INamedTypeSymbol classSymbol)
    {
        if (elementType.IsAbstract)
        {
            return false;
        }

        bool sameAssembly = SymbolEqualityComparer.Default.Equals(
            elementType.ContainingAssembly, classSymbol.ContainingAssembly);
        foreach (IMethodSymbol constructor in elementType.InstanceConstructors)
        {
            if (constructor.Parameters.Length != 0)
            {
                continue;
            }

            if (constructor.DeclaredAccessibility == Accessibility.Public
                || (constructor.DeclaredAccessibility == Accessibility.Internal && sameAssembly))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Checks every row name of one type against The Rules, and adds BRUTALUI0005 on the property or the
    /// create-row attribute for each fault: not a valid XML local name, taken by a base row, or shared by two
    /// rows of the type (ordinal).
    /// </summary>
    /// <param name="faults">The fault list to add to.</param>
    /// <param name="createSources">The create-row name sources.</param>
    /// <param name="setSources">The set-row name sources.</param>
    private static void CheckRowNames(
        ImmutableArray<DiagnosticInfo>.Builder faults,
        List<RowSource> createSources,
        List<RowSource> setSources)
    {
        var all = new List<RowSource>(createSources.Count + setSources.Count);
        all.AddRange(createSources);
        all.AddRange(setSources);

        for (int index = 0; index < all.Count; index++)
        {
            RowSource row = all[index];
            string? cause = null;
            if (!IsValidXmlName(row.Name))
            {
                cause = "it is not a valid XML local name";
            }
            else if (IsBaseRowName(row.Name))
            {
                cause = "a base row takes it";
            }
            else
            {
                for (int other = 0; other < all.Count; other++)
                {
                    if (other != index && string.Equals(all[other].Name, row.Name, StringComparison.Ordinal))
                    {
                        cause = "another row takes it";
                        break;
                    }
                }
            }

            if (cause is not null)
            {
                faults.Add(DiagnosticInfo.Create(
                    DiagnosticDescriptors.BadRowNameId, row.Location, row.Name, row.Owner, cause));
            }
        }
    }

    /// <summary>Checks that a row name does not clash with a base row.</summary>
    /// <param name="rowName">The row name to check.</param>
    /// <returns><see langword="true"/> when a base row takes the name.</returns>
    private static bool IsBaseRowName(string rowName)
    {
        foreach (string baseName in BaseRowNames)
        {
            if (string.Equals(baseName, rowName, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>One row name with the place and the owner that BRUTALUI0005 reports.</summary>
    private readonly struct RowSource
    {
        /// <summary>Creates one source.</summary>
        /// <param name="name">The row name.</param>
        /// <param name="owner">The property name or the factory method name.</param>
        /// <param name="location">The location of the property or the create-row attribute.</param>
        public RowSource(string name, string owner, LocationInfo? location)
        {
            Name = name;
            Owner = owner;
            Location = location;
        }

        /// <summary>The row name.</summary>
        public string Name { get; }

        /// <summary>The property name or the factory method name, for the message.</summary>
        public string Owner { get; }

        /// <summary>The location, or <see langword="null"/> when the row comes from metadata.</summary>
        public LocationInfo? Location { get; }
    }
}
