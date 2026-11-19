using System;
using BrutalUi.Markup;

namespace BrutalUi.Framework;

/// <summary>
/// The static element tables of the read pass: the kind resolution, the attribute name tables, and the messages
/// for an unknown place or an empty value. The walk in <c>DefinitionReader.Elements.cs</c> calls these members.
/// Each name table is a static array, so one element gives no new array in the read pass.
/// </summary>
internal sealed partial class DefinitionReader
{
    /// <summary>Gets the attribute names of Panel. Each name is required.</summary>
    private static readonly string[] PanelAttributeNames = ["name", "slot", "template"];

    /// <summary>Gets the attribute names of Layout. Each name is required.</summary>
    private static readonly string[] LayoutAttributeNames = ["name", "template"];

    /// <summary>Gets the allowed attribute names of Mode: the required names plus the optional <c>actions</c>
    /// name.</summary>
    private static readonly string[] ModeAllowedNames = ["name", "layout", "actions"];

    /// <summary>Gets the required attribute names of Mode.</summary>
    private static readonly string[] ModeRequiredNames = ["name", "layout"];

    /// <summary>Gets the attribute names of Slot and PanelRef: the one required <c>name</c>.</summary>
    private static readonly string[] NameOnlyAttributeNames = ["name"];

    /// <summary>
    /// Gives the kind of an element under one parent, or <see cref="Kind.Unknown"/> when the place holds no element
    /// of that name.
    /// </summary>
    /// <param name="parent">The kind of the parent element, or <see cref="Kind.Root"/> at the root position.</param>
    /// <param name="localName">The local name of the element.</param>
    /// <param name="atRoot">Whether the walk stands on the root element of the text.</param>
    /// <returns>The kind of the element.</returns>
    private static Kind ResolveKind(Kind parent, string localName, bool atRoot)
    {
        if (atRoot)
        {
            return localName == "Definitions" ? Kind.Definitions : Kind.Unknown;
        }

        return parent switch
        {
            Kind.Definitions => localName switch
            {
                "Panel" => Kind.Panel,
                "Layout" => Kind.Layout,
                "Mode" => Kind.Mode,
                _ => Kind.Unknown,
            },

            Kind.Mode => localName switch
            {
                "Slot" => Kind.Slot,
                "PanelRef" => Kind.PanelRef,
                _ => Kind.Unknown,
            },
            Kind.Slot => localName == "PanelRef" ? Kind.PanelRef : Kind.Unknown,
            _ => Kind.Unknown,
        };
    }

    /// <summary>Gives the message for an element that the place does not hold.</summary>
    /// <param name="parent">The kind of the parent element.</param>
    /// <param name="localName">The local name of the element.</param>
    /// <param name="atRoot">Whether the walk stands on the root element of the text.</param>
    /// <returns>The message.</returns>
    private static string PlaceMessage(Kind parent, string localName, bool atRoot)
    {
        if (atRoot)
        {
            return $"'{localName}' is not the root of a definition text. "
                + $"Write <ui:Definitions xmlns:ui=\"{TemplateLoader.Namespace}\"> as the root.";
        }

        if (parent == Kind.Definitions)
        {
            if (localName == "Window")
            {
                return "'Window' is not a definition. WindowDefinition comes in a later version. "
                    + "The definitions are Panel, Layout, and Mode.";
            }

            return $"'{localName}' is not a definition. The definitions are Panel, Layout, and Mode.";
        }

        if (parent == Kind.Mode)
        {
            return $"'{localName}' cannot be a child of Mode. Write Slot or PanelRef.";
        }

        if (parent == Kind.Slot)
        {
            return $"'{localName}' cannot be a child of Slot. Write PanelRef.";
        }

        return $"'{localName}' cannot be a child of {NameOf(parent)}. Remove it.";
    }

    /// <summary>Gives the <see cref="DefinitionErrorKind.InvalidValue"/> message of an empty attribute value. The
    /// second sentence names the fix for that attribute.</summary>
    /// <param name="name">The attribute name.</param>
    /// <returns>The message.</returns>
    private static string EmptyValueMessage(string name) => name switch
    {
        "slot" => "'slot' needs a value. Write the name of a slot, for example 'top'.",
        "template" => "'template' needs a value. Write the name of a registered template.",
        "layout" => "'layout' needs a value. Write the name of a layout of this text.",
        "actions" => "'actions' needs a value. Write the name of a registered action map.",
        _ => $"'{name}' needs a value. Write a name.",
    };

    /// <summary>Gives the attribute names that the table of one element kind holds, the required names and the
    /// optional ones. Check 3b reads this list, and check 4 reads <see cref="RequiredNames"/>. Keep the two tables
    /// in step when you add a name (Decision 7).</summary>
    /// <param name="kind">The kind of the element.</param>
    /// <returns>The names, in the order of the table.</returns>
    private static string[] AllowedNames(Kind kind) => kind switch
    {
        Kind.Panel => PanelAttributeNames,
        Kind.Layout => LayoutAttributeNames,
        Kind.Mode => ModeAllowedNames,
        Kind.Slot => NameOnlyAttributeNames,
        Kind.PanelRef => NameOnlyAttributeNames,
        _ => Array.Empty<string>(),
    };

    /// <summary>Gives the attribute names that one element kind needs. The <c>actions</c> name of <c>Mode</c> is
    /// optional, so it is absent here: a mode with no <c>actions</c> gets no map.</summary>
    /// <param name="kind">The kind of the element.</param>
    /// <returns>The required names, in the order of the table.</returns>
    private static string[] RequiredNames(Kind kind) => kind switch
    {
        Kind.Panel => PanelAttributeNames,
        Kind.Layout => LayoutAttributeNames,
        Kind.Mode => ModeRequiredNames,
        Kind.Slot => NameOnlyAttributeNames,
        Kind.PanelRef => NameOnlyAttributeNames,
        _ => Array.Empty<string>(),
    };

    /// <summary>Gives the element name of a kind.</summary>
    /// <param name="kind">The kind.</param>
    /// <returns>The name.</returns>
    private static string NameOf(Kind kind) => kind switch
    {
        Kind.Definitions => "Definitions",
        Kind.Panel => "Panel",
        Kind.Layout => "Layout",
        Kind.Mode => "Mode",
        Kind.Slot => "Slot",
        Kind.PanelRef => "PanelRef",
        _ => "the text",
    };
}
