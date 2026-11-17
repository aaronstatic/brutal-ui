// The phase 4 diagnostics. This file holds the six DiagnosticDescriptor values of The Diagnostics, and the
// value-equal records the pipeline caches: LocationInfo, DiagnosticInfo, and ElementResult.
// Cache rule. These records hold no ISymbol, no SyntaxNode, no Location, and no Compilation. The output step
// rebuilds the Location from the stored file path, text span, and line span. See implementation.md,
// "The Diagnostics" and "The Generator Pipeline".
using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace BrutalUi.Generators;

/// <summary>
/// A source location, stored as values: the file path, the text span, and the line span. The pipeline caches
/// this record between the transform and the output step, because it holds no <c>Location</c>.
/// </summary>
/// <param name="FilePath">The path of the file that holds the span.</param>
/// <param name="Span">The span of the text.</param>
/// <param name="LineSpan">The span of the lines.</param>
internal sealed record LocationInfo(string FilePath, TextSpan Span, LinePositionSpan LineSpan)
{
    /// <summary>Rebuilds the <c>Location</c> for one diagnostic.</summary>
    /// <returns>The created location.</returns>
    internal Location ToLocation()
    {
        return Location.Create(FilePath, Span, LineSpan);
    }

    /// <summary>Copies a source location into this value record.</summary>
    /// <param name="location">The location to copy, or <see langword="null"/>.</param>
    /// <returns>The copy, or <see langword="null"/> for a <see langword="null"/> or non-source location.</returns>
    internal static LocationInfo? From(Location? location)
    {
        if (location is null || !location.IsInSource)
        {
            return null;
        }

        FileLinePositionSpan line = location.GetLineSpan();
        return new LocationInfo(line.Path, location.SourceSpan, line.Span);
    }

    /// <summary>Copies the source location of a symbol.</summary>
    /// <param name="symbol">The symbol to read.</param>
    /// <returns>The copy, or <see langword="null"/> when the symbol has no source location.</returns>
    internal static LocationInfo? FromSymbol(ISymbol symbol)
    {
        foreach (Location location in symbol.Locations)
        {
            LocationInfo? info = From(location);
            if (info is not null)
            {
                return info;
            }
        }

        return null;
    }
}

/// <summary>
/// One reported fault, stored as values. The transform builds these records, and the output step turns each one
/// into a <c>Diagnostic</c> with <see cref="ToDiagnostic"/>.
/// </summary>
/// <param name="Id">The diagnostic id, one of the <see cref="DiagnosticDescriptors"/> ids.</param>
/// <param name="Location">The location of the fault, or <see langword="null"/> when it has none.</param>
/// <param name="Arguments">The message arguments, in order.</param>
internal sealed record DiagnosticInfo(string Id, LocationInfo? Location, EquatableArray<string> Arguments)
{
    /// <summary>Creates one fault.</summary>
    /// <param name="id">The diagnostic id.</param>
    /// <param name="location">The location, or <see langword="null"/>.</param>
    /// <param name="arguments">The message arguments, in order.</param>
    /// <returns>The fault.</returns>
    internal static DiagnosticInfo Create(string id, LocationInfo? location, params string[] arguments)
    {
        return new DiagnosticInfo(id, location, new EquatableArray<string>(ImmutableArray.Create(arguments)));
    }

    /// <summary>Makes the diagnostic for the output step. The method looks the descriptor up by <see cref="Id"/>.</summary>
    /// <returns>The diagnostic, with the stored location or no location.</returns>
    internal Diagnostic ToDiagnostic()
    {
        object?[] messageArgs = new object?[Arguments.Length];
        for (int index = 0; index < Arguments.Length; index++)
        {
            messageArgs[index] = Arguments[index];
        }

        LocationInfo? stored = Location;
        Microsoft.CodeAnalysis.Location location = stored is null
            ? Microsoft.CodeAnalysis.Location.None
            : stored.ToLocation();
        return Diagnostic.Create(DiagnosticDescriptors.For(Id), location, messageArgs);
    }
}

/// <summary>
/// The transform result for one marked class. The pipeline caches this record. A type with a fault keeps a
/// <see langword="null"/> <see cref="Model"/>, so it gets no generated code.
/// </summary>
/// <param name="Model">The model, or <see langword="null"/> when the type has a fault.</param>
/// <param name="Diagnostics">The faults of this type. Empty when the type is valid.</param>
/// <param name="NameLocation">The location of the name argument of the attribute, for BRUTALUI0002.</param>
/// <param name="TypeName">The display name of the marked class, for the duplicate-name message.</param>
internal sealed record ElementResult(
    ElementModel? Model,
    EquatableArray<DiagnosticInfo> Diagnostics,
    LocationInfo? NameLocation,
    string TypeName);

/// <summary>
/// The six descriptors of The Diagnostics. Each has the category <c>BrutalUi.Markup</c> and the severity
/// <c>Error</c>. The messages follow the table of implementation.md, word for word.
/// </summary>
internal static class DiagnosticDescriptors
{
    /// <summary>The diagnostic category.</summary>
    internal const string Category = "BrutalUi.Markup";

    /// <summary>The id of the missing-constructor-and-factory fault.</summary>
    internal const string NoUsableConstructorAndNoFactoryId = "BRUTALUI0001";

    /// <summary>The id of the duplicate-XML-name fault.</summary>
    internal const string DuplicateXmlNameId = "BRUTALUI0002";

    /// <summary>The id of the row-name-on-no-row fault.</summary>
    internal const string RowNameOnNoRowId = "BRUTALUI0003";

    /// <summary>The id of the bad-factory fault.</summary>
    internal const string BadFactoryId = "BRUTALUI0004";

    /// <summary>The id of the bad-row-name fault.</summary>
    internal const string BadRowNameId = "BRUTALUI0005";

    /// <summary>The id of the bad-element-type fault.</summary>
    internal const string BadElementTypeId = "BRUTALUI0006";

    /// <summary>BRUTALUI0001: the type has no factory and no usable constructor with no parameters.</summary>
    internal static readonly DiagnosticDescriptor NoUsableConstructorAndNoFactory = new(
        NoUsableConstructorAndNoFactoryId,
        "No usable constructor and no factory",
        "'{0}' has no usable constructor and no factory. Add a public constructor with no parameters, or mark a static method with [TemplateFactory].",
        Category,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    /// <summary>BRUTALUI0002: two marked classes of one compilation share one XML name.</summary>
    internal static readonly DiagnosticDescriptor DuplicateXmlName = new(
        DuplicateXmlNameId,
        "Duplicate XML name",
        "The XML name '{0}' is on more than one element type: {1}. Give each type its own name.",
        Category,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    /// <summary>BRUTALUI0003: [TemplateRowName] sits on a property that gives no row.</summary>
    internal static readonly DiagnosticDescriptor RowNameOnNoRow = new(
        RowNameOnNoRowId,
        "TemplateRowName on a property that gives no row",
        "[TemplateRowName] is on '{0}', but '{0}' gives no row. Use a public settable property of a supported type, or remove the attribute.",
        Category,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    /// <summary>BRUTALUI0004: a factory breaks a rule of The Rules, or a create row sits with no factory.</summary>
    internal static readonly DiagnosticDescriptor BadFactory = new(
        BadFactoryId,
        "Invalid factory",
        "'{0}' is not a valid factory: {1}. Write 'static Element? Create(ElementCreateArgs args)' with internal or public access.",
        Category,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    /// <summary>BRUTALUI0005: a row name is not a valid XML local name, two rows share it, or a base row takes it.</summary>
    internal static readonly DiagnosticDescriptor BadRowName = new(
        BadRowNameId,
        "Invalid row name",
        "The row name '{0}' of '{1}' is not valid: {2}. Give each row a valid XML name that no other row and no base row takes.",
        Category,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    // The RS1032 rule wants one sentence with no trailing period. The plan message of BRUTALUI0006 is one
    // sentence with a period, because ASD-STE100 ends every sentence with a period. Keep the plan text.
#pragma warning disable RS1032
    /// <summary>BRUTALUI0006: a marked class breaks a rule of The element type, or its XML name is not valid.</summary>
    internal static readonly DiagnosticDescriptor BadElementType = new(
        BadElementTypeId,
        "Invalid element type",
        "'{0}' is not a valid element type: {1}.",
        Category,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);
#pragma warning restore RS1032

    /// <summary>Maps one id to its descriptor.</summary>
    /// <param name="id">The id of one fault.</param>
    /// <returns>The descriptor. The method gives <see cref="BadElementType"/> for an unknown id.</returns>
    internal static DiagnosticDescriptor For(string id)
    {
        if (id == NoUsableConstructorAndNoFactoryId)
        {
            return NoUsableConstructorAndNoFactory;
        }

        if (id == DuplicateXmlNameId)
        {
            return DuplicateXmlName;
        }

        if (id == RowNameOnNoRowId)
        {
            return RowNameOnNoRow;
        }

        if (id == BadFactoryId)
        {
            return BadFactory;
        }

        if (id == BadRowNameId)
        {
            return BadRowName;
        }

        return BadElementType;
    }
}
