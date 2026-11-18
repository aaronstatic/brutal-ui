using System;
using System.Collections.Generic;
using System.Text;
using BrutalUi.Core;
using BrutalUi.Markup;

namespace BrutalUi.Framework;

/// <summary>
/// The resolve pass of one definitions load: the three name tables, the duplicate check, the check load of each used
/// layout, the slot check, and the build of the three records.
/// </summary>
/// <remarks>
/// <para>
/// The pass runs after the read pass, also when the read pass found an error, so one load gives each error. It stops
/// only for <see cref="DefinitionErrorKind.NotWellFormed"/>. <strong>The follow-on rule (one fault, one error):</strong>
/// an entry with a name stays in its name table, also when another value of the entry has a fault; such an entry is
/// faulty. A reference to a faulty entry gives no error. A mode on a faulty layout skips the slot check. A placement
/// with no panel record skips the slot check.
/// </para>
/// </remarks>
internal sealed partial class DefinitionReader
{
    /// <summary>Resolves the entries and builds the three record lists. A fault of one entry gives no error in
    /// another (the follow-on rule).</summary>
    private void Resolve(List<PanelDefinition> panels, List<LayoutDefinition> layouts, List<ModeDefinition> modes)
    {
        if (HoldsNotWellFormed())
        {
            return;
        }

        // Step 1. The three name tables, with three tables for the three kinds, as in UiManager. The first entry of
        // a name wins. A later entry gives DuplicateName at its name attribute.
        Dictionary<string, int> panelNames = BuildNames(_panelEntries, static entry => entry.Name);
        Dictionary<string, int> layoutNames = BuildNames(_layoutEntries, static entry => entry.Name);
        Dictionary<string, int> modeNames = BuildNames(_modeEntries, static entry => entry.Name);
        ReportDuplicates(_panelEntries, panelNames, static entry => entry.Name, "panel");
        ReportDuplicates(_layoutEntries, layoutNames, static entry => entry.Name, "layout");
        ReportDuplicates(_modeEntries, modeNames, static entry => entry.Name, "mode");

        // Step 2. The panels. Each record gets a factory that loads its template at each call (A4). The array keeps
        // each record by entry index, so a placement with no slot value can read the default slot of its panel (Q5).
        // A faulty entry keeps a null element and gives its reference no second error (the follow-on rule).
        PanelDefinition?[] panelRecords = new PanelDefinition?[_panelEntries.Count];
        for (int i = 0; i < _panelEntries.Count; i++)
        {
            PanelEntry entry = _panelEntries[i];
            if (!entry.Name.HasName || !entry.Slot.HasName || !entry.Template.HasName)
            {
                continue;
            }

            if (!IsFirstName(panelNames, entry.Name, i))
            {
                continue;
            }

            string template = entry.Template.Text!;
            if (!_loader.TryGetTemplate(template, out byte[]? text))
            {
                AddError(
                    DefinitionErrorKind.UnknownTemplate,
                    $"'{template}' is not a registered template. Add it with DefinitionLoader.AddTemplate before the load.",
                    entry.Template.Line,
                    entry.Template.Column);
                continue;
            }

            byte[] name = Encoding.UTF8.GetBytes(entry.Name.Text!);
            PanelDefinition record = new(
                name,
                Encoding.UTF8.GetBytes(entry.Slot.Text!),
                CreateFactory(text!, template));
            panelRecords[i] = record;
            panels.Add(record);
        }

        // Step 3. The layouts. A faulty layout is faulty for the slot check of each mode on it (the follow-on rule).
        // The table keeps the slot names of each checked layout, in pre-order, first entry first.
        bool[] layoutFaulty = new bool[_layoutEntries.Count];
        Dictionary<int, string[]> layoutSlots = [];
        for (int i = 0; i < _layoutEntries.Count; i++)
        {
            LayoutEntry entry = _layoutEntries[i];
            if (!entry.Name.HasName || !entry.Template.HasName || !IsFirstName(layoutNames, entry.Name, i))
            {
                layoutFaulty[i] = true;
                continue;
            }

            string template = entry.Template.Text!;
            if (!_loader.TryGetTemplate(template, out byte[]? text))
            {
                AddError(
                    DefinitionErrorKind.UnknownTemplate,
                    $"'{template}' is not a registered template. Add it with DefinitionLoader.AddTemplate before the load.",
                    entry.Template.Line,
                    entry.Template.Column);
                layoutFaulty[i] = true;
                continue;
            }

            byte[] name = Encoding.UTF8.GetBytes(entry.Name.Text!);
            layouts.Add(new LayoutDefinition(name, CreateFactory(text!, template)));
        }

        // Step 3b. The check load (Q3, A5). Each layout that a mode names loads its template one time. A layout that
        // no mode names gives no load error: its factory throws at RegisterTo (Notes item 6 of the plan).
        HashSet<int> usedLayouts = [];
        for (int i = 0; i < _modeEntries.Count; i++)
        {
            if (_modeEntries[i].Layout is { HasName: true, Text: { } layoutText }
                && layoutNames.TryGetValue(layoutText, out int layoutIndex))
            {
                _ = usedLayouts.Add(layoutIndex);
            }
        }

        for (int i = 0; i < _layoutEntries.Count; i++)
        {
            if (!usedLayouts.Contains(i) || layoutFaulty[i])
            {
                continue;
            }

            if (CheckLayout(_layoutEntries[i]) is string[] slots)
            {
                layoutSlots[i] = slots;
            }
            else
            {
                layoutFaulty[i] = true;
            }
        }

        // Step 4. The modes. The layout and each panel name resolve against the entries of the same text (Decision 6).
        for (int i = 0; i < _modeEntries.Count; i++)
        {
            ModeEntry entry = _modeEntries[i];
            if (!entry.Name.HasName || !entry.Layout.HasName || !IsFirstName(modeNames, entry.Name, i))
            {
                continue;
            }

            string layout = entry.Layout.Text!;
            if (!layoutNames.TryGetValue(layout, out int layoutIndex))
            {
                AddError(
                    DefinitionErrorKind.UnknownLayout,
                    $"'{layout}' is not a layout of this text. Add a Layout entry with this name.",
                    entry.Layout.Line,
                    entry.Layout.Column);
                continue;
            }

            // The actions value names a registered map. No value gives no map, because DefinitionValue.HasName is the
            // single test for a value (an empty value already has its InvalidValue error from the read pass).
            UiActionMap? actionMap = null;
            if (entry.Actions.HasName)
            {
                string map = entry.Actions.Text!;
                if (!_loader.TryGetActionMap(map, out actionMap))
                {
                    AddError(
                        DefinitionErrorKind.UnknownActionMap,
                        $"'{map}' is not a registered action map. Add it with DefinitionLoader.AddActionMap before the load.",
                        entry.Actions.Line,
                        entry.Actions.Column);
                    continue;
                }
            }

            // A mode on a faulty layout skips the slot check (the follow-on rule).
            bool checkSlots = !layoutFaulty[layoutIndex];
            string[] slots = layoutSlots.TryGetValue(layoutIndex, out string[]? known) ? known : [];

            List<ModePanel> placements = [];
            HashSet<string> placed = new(StringComparer.Ordinal);
            for (int p = 0; p < entry.Placements.Count; p++)
            {
                PlacementEntry placement = entry.Placements[p];

                // A placement with no panel name already has its error from the read pass.
                if (!placement.Name.HasName)
                {
                    continue;
                }

                string panel = placement.Name.Text!;
                if (!panelNames.TryGetValue(panel, out int panelIndex))
                {
                    AddError(
                        DefinitionErrorKind.UnknownPanel,
                        $"'{panel}' is not a panel of this text. Add a Panel entry with this name.",
                        placement.Name.Line,
                        placement.Name.Column);
                    continue;
                }

                // A mode lists one panel one time. The record enforces the rule, so the pass reports the later
                // placement and keeps the first.
                if (!placed.Add(panel))
                {
                    AddError(
                        DefinitionErrorKind.DuplicateName,
                        $"'{panel}' is in this mode already. A mode holds a panel one time.",
                        placement.Name.Line,
                        placement.Name.Column);
                    continue;
                }

                // A placement of a faulty panel entry has no record and gives no error (the follow-on rule).
                if (panelRecords[panelIndex] is not { } panelRecord)
                {
                    continue;
                }

                byte[] panelBytes = Encoding.UTF8.GetBytes(panel);
                if (placement.UnderSlot)
                {
                    // The slot name of the enclosing Slot: an empty name already has its InvalidValue error, so the
                    // placement gives no second error.
                    if (!placement.Slot.HasName)
                    {
                        continue;
                    }

                    string slotName = placement.Slot.Text!;
                    if (checkSlots && Array.IndexOf(slots, slotName) < 0)
                    {
                        AddError(
                            DefinitionErrorKind.UnknownSlot,
                            $"'{slotName}' is not a slot of the layout '{layout}'. The slots are {string.Join(", ", slots)}.",
                            placement.Slot.Line,
                            placement.Slot.Column);
                        continue;
                    }

                    placements.Add(new ModePanel(Encoding.UTF8.GetBytes(slotName), panelBytes));
                    continue;
                }

                // A PanelRef directly under the mode takes the default slot of the panel record (Q5).
                string defaultSlot = Encoding.UTF8.GetString(panelRecord.DefaultSlot);
                if (checkSlots && Array.IndexOf(slots, defaultSlot) < 0)
                {
                    AddError(
                        DefinitionErrorKind.UnknownSlot,
                        $"The panel '{panel}' has the default slot '{defaultSlot}', and the layout '{layout}' holds "
                            + "no such slot. Put the PanelRef in a Slot, or change the slot of the panel.",
                        placement.Name.Line,
                        placement.Name.Column);
                    continue;
                }

                placements.Add(new ModePanel(panelRecord.DefaultSlot, panelBytes));
            }

            byte[] modeName = Encoding.UTF8.GetBytes(entry.Name.Text!);
            byte[] layoutName = Encoding.UTF8.GetBytes(layout);
            modes.Add(new ModeDefinition(modeName, layoutName, placements.ToArray(), actionMap));
        }
    }

    /// <summary>Loads one used layout template, collects its slot names, and records each fault. The method drops the
    /// tree (Q3). Gives the slot names in pre-order for a sound layout, or <see langword="null"/> after a recorded
    /// fault.</summary>
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
