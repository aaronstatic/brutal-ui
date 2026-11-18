using System;
using System.Collections.Generic;
using System.Text;
using BrutalUi.Core;
using BrutalUi.Markup;

namespace BrutalUi.Framework;

/// <summary>
/// The helpers of the resolve pass: the check load of one layout, the slot walk, the duplicate report, the name
/// tables, the template factory, and the first-name test.
/// </summary>
internal sealed partial class DefinitionReader
{
    /// <summary>Loads one sound layout template, collects its slot names, and records each fault (Q3, Decision 15).
    /// The method drops the tree. Gives the slot names in pre-order for a sound layout, or <see langword="null"/>
    /// after a recorded fault.</summary>
    private string[]? CheckLayout(LayoutEntry entry)
    {
        string template = entry.Template.Text!;
        if (!_loader.TryGetTemplate(template, out byte[]? text))
        {
            return null;
        }

        TemplateLoadResult result = _loader.Templates.Load(text!, template);
        if (!result.Succeeded)
        {
            StringBuilder message = new($"The template '{template}' of the layout '{entry.Name.Text}' does not load.");
            for (int e = 0; e < result.Errors.Count; e++)
            {
                _ = message.Append('\n').Append(result.Errors[e].ToString());
            }

            AddError(DefinitionErrorKind.TemplateFault, message.ToString(), entry.Line, entry.Column);
            return null;
        }

        List<string> slots = [];
        string? duplicate = CollectSlots(result.Root!, slots, new HashSet<string>(StringComparer.Ordinal));
        if (duplicate is not null)
        {
            AddError(
                DefinitionErrorKind.DuplicateName,
                $"The template '{template}' holds two slots with the name '{duplicate}'. "
                    + "Give each SlotContainer one slot name.",
                entry.Line,
                entry.Column);
            return null;
        }

        return [.. slots];
    }

    /// <summary>Walks one template tree in pre-order and collects the name of each <see cref="SlotContainer"/>. The
    /// walk does not enter a slot, because the children of a slot are the mounted panels. Gives the name of the
    /// second slot of one name, or <see langword="null"/> when each name is unique.</summary>
    private static string? CollectSlots(Element element, List<string> slots, HashSet<string> seen)
    {
        if (element is SlotContainer container)
        {
            string name = Encoding.UTF8.GetString(container.SlotName);
            if (!seen.Add(name))
            {
                return name;
            }

            slots.Add(name);
            return null;
        }

        foreach (Element child in element.Children)
        {
            string? duplicate = CollectSlots(child, slots, seen);
            if (duplicate is not null)
            {
                return duplicate;
            }
        }

        return null;
    }

    /// <summary>Reports one <see cref="DefinitionErrorKind.DuplicateName"/> at the name attribute of each entry after
    /// the first entry of its name. The kind word of the message is <c>panel</c>, <c>layout</c>, or <c>mode</c>.</summary>
    private void ReportDuplicates<TEntry>(
        List<TEntry> entries,
        Dictionary<string, int> names,
        Func<TEntry, DefinitionValue> nameOf,
        string kindName)
    {
        for (int i = 0; i < entries.Count; i++)
        {
            DefinitionValue value = nameOf(entries[i]);
            if (value.HasName && names[value.Text!] != i)
            {
                AddError(
                    DefinitionErrorKind.DuplicateName,
                    $"'{value.Text}' names a {kindName} already. Give each {kindName} one name.",
                    value.Line,
                    value.Column);
            }
        }
    }

    /// <summary>Builds one name table from one entry list. The compare is ordinal. The first entry of a name wins,
    /// and an entry with a name stays in the table, also when another value of the entry has a fault. The table maps
    /// each name to the index of its first entry.</summary>
    private static Dictionary<string, int> BuildNames<TEntry>(List<TEntry> entries, Func<TEntry, DefinitionValue> nameOf)
    {
        Dictionary<string, int> names = new(StringComparer.Ordinal);
        for (int i = 0; i < entries.Count; i++)
        {
            DefinitionValue value = nameOf(entries[i]);
            if (value.HasName)
            {
                _ = names.TryAdd(value.Text!, i);
            }
        }

        return names;
    }

    /// <summary>Creates the factory of one template. The factory ignores its context and loads the text at each call
    /// (A4). The template name is the source name of each <see cref="TemplateError"/>.</summary>
    /// <param name="text">The UTF-8 text copy of the template.</param>
    /// <param name="templateName">The name that the definition value holds.</param>
    /// <returns>The factory.</returns>
    private Func<PanelContext, Element> CreateFactory(byte[] text, string templateName)
    {
        TemplateLoader templates = _loader.Templates;
        return _ => templates.Load(text, templateName).GetRootOrThrow();
    }

    /// <summary>Shows whether one entry is the first entry of its name. A later entry of one name gives
    /// <see langword="false"/>.</summary>
    /// <param name="names">The name table of the kind.</param>
    /// <param name="value">The name value of the entry.</param>
    /// <param name="index">The index of the entry in its list.</param>
    /// <returns><see langword="true"/> when the entry owns its name.</returns>
    private static bool IsFirstName(Dictionary<string, int> names, DefinitionValue value, int index)
    {
        return names[value.Text!] == index;
    }
}
