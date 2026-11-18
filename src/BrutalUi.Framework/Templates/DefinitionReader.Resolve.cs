using System;
using System.Collections.Generic;
using System.Text;
using BrutalUi.Core;
using BrutalUi.Markup;

namespace BrutalUi.Framework;

/// <summary>
/// The resolve pass of one definitions load: the three name tables, the lookup of each name, and the build of the
/// three records.
/// </summary>
/// <remarks>
/// <para>
/// The pass runs after the read pass, also when the read pass found an error, so one load gives each error. It stops
/// only for <see cref="DefinitionErrorKind.NotWellFormed"/>.
/// </para>
/// <para>
/// <strong>Phase 1 scope.</strong> The pass looks up each panel template and each layout template in the table of
/// the loader, and each layout and each panel of a mode in the entries of the same text. It skips an entry whose
/// value gave a read fault, so one fault gives one error. The duplicate check, the slot check, the check load of a
/// used layout, and the follow-on rule of the plan come in phase 3. The <c>actions</c> value comes in phase 2, so
/// each mode record gets no map in this phase.
/// </para>
/// </remarks>
internal sealed partial class DefinitionReader
{
    /// <summary>Resolves the entries and builds the records. The method adds to the error list, so a fault of one
    /// entry gives no error in another.</summary>
    /// <param name="panels">The list that takes each panel record, in the order of the text.</param>
    /// <param name="layouts">The list that takes each layout record, in the order of the text.</param>
    /// <param name="modes">The list that takes each mode record, in the order of the text.</param>
    private void Resolve(List<PanelDefinition> panels, List<LayoutDefinition> layouts, List<ModeDefinition> modes)
    {
        if (HoldsNotWellFormed())
        {
            return;
        }

        // Step 1. The three name tables. The first entry of a name wins, so a later entry gives no record. Phase 3
        // reports DuplicateName for it. The three kinds have three tables, as in UiManager.
        Dictionary<string, int> panelNames = BuildNames(
            _panelEntries,
            static entry => entry.Name);
        Dictionary<string, int> layoutNames = BuildNames(
            _layoutEntries,
            static entry => entry.Name);
        Dictionary<string, int> modeNames = BuildNames(
            _modeEntries,
            static entry => entry.Name);

        // Step 2. The panels. Each record gets a factory that loads its template at each call (A4).
        for (int i = 0; i < _panelEntries.Count; i++)
        {
            PanelEntry entry = _panelEntries[i];
            if (!entry.Name.HasName || !entry.Slot.HasName)
            {
                continue;
            }

            if (!IsFirstName(panelNames, entry.Name, i))
            {
                continue;
            }

            if (!entry.Template.HasName)
            {
                continue;
            }

            string template = entry.Template.Text!;
            if (!_loader.TryGetTemplate(template, out byte[]? text))
            {
                AddError(
                    DefinitionErrorKind.UnknownTemplate,
                    $"'{template}' is not a registered template. Add it with AddTemplate before the load.",
                    entry.Template.Line,
                    entry.Template.Column);
                continue;
            }

            byte[] name = Encoding.UTF8.GetBytes(entry.Name.Text!);
            byte[] slot = Encoding.UTF8.GetBytes(entry.Slot.Text!);
            panels.Add(new PanelDefinition(name, slot, CreateFactory(text!, template)));
        }

        // Step 3. The layouts.
        for (int i = 0; i < _layoutEntries.Count; i++)
        {
            LayoutEntry entry = _layoutEntries[i];
            if (!entry.Name.HasName)
            {
                continue;
            }

            if (!IsFirstName(layoutNames, entry.Name, i))
            {
                continue;
            }

            if (!entry.Template.HasName)
            {
                continue;
            }

            string template = entry.Template.Text!;
            if (!_loader.TryGetTemplate(template, out byte[]? text))
            {
                AddError(
                    DefinitionErrorKind.UnknownTemplate,
                    $"'{template}' is not a registered template. Add it with AddTemplate before the load.",
                    entry.Template.Line,
                    entry.Template.Column);
                continue;
            }

            byte[] name = Encoding.UTF8.GetBytes(entry.Name.Text!);
            layouts.Add(new LayoutDefinition(name, CreateFactory(text!, template)));
        }

        // Step 4. The modes. The layout and each panel name resolve against the entries of the same text (Decision 6).
        for (int i = 0; i < _modeEntries.Count; i++)
        {
            ModeEntry entry = _modeEntries[i];
            if (!entry.Name.HasName || !entry.Layout.HasName)
            {
                continue;
            }

            if (!IsFirstName(modeNames, entry.Name, i))
            {
                continue;
            }

            string layout = entry.Layout.Text!;
            if (!layoutNames.ContainsKey(layout))
            {
                AddError(
                    DefinitionErrorKind.UnknownLayout,
                    $"'{layout}' is not a layout of this text. Add a Layout entry with this name.",
                    entry.Layout.Line,
                    entry.Layout.Column);
                continue;
            }

            List<ModePanel> placements = [];
            HashSet<string> placed = new(StringComparer.Ordinal);
            for (int p = 0; p < entry.Placements.Count; p++)
            {
                PlacementEntry placement = entry.Placements[p];

                // A placement with no name, or with no slot name, already has its error from the read pass.
                if (!placement.Name.HasName || !placement.Slot.HasName)
                {
                    continue;
                }

                string panel = placement.Name.Text!;
                if (!panelNames.ContainsKey(panel))
                {
                    AddError(
                        DefinitionErrorKind.UnknownPanel,
                        $"'{panel}' is not a panel of this text. Add a Panel entry with this name.",
                        placement.Name.Line,
                        placement.Name.Column);
                    continue;
                }

                // A mode lists one panel one time. The record enforces the rule, so the pass keeps the first
                // placement. Phase 3 reports DuplicateName for a later one.
                if (!placed.Add(panel))
                {
                    continue;
                }

                placements.Add(new ModePanel(
                    Encoding.UTF8.GetBytes(placement.Slot.Text!),
                    Encoding.UTF8.GetBytes(panel)));
            }

            byte[] name = Encoding.UTF8.GetBytes(entry.Name.Text!);
            byte[] layoutName = Encoding.UTF8.GetBytes(layout);
            modes.Add(new ModeDefinition(name, layoutName, placements.ToArray()));
        }
    }

    /// <summary>Builds one name table from one entry list. The compare is ordinal. The first entry of a name wins.</summary>
    /// <typeparam name="TEntry">The entry type of the list.</typeparam>
    /// <param name="entries">The entries, in the order of the text.</param>
    /// <param name="nameOf">The name value of one entry.</param>
    /// <returns>The table: the name of each usable entry to the index of its first entry.</returns>
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
