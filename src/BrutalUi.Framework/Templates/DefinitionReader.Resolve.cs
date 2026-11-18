using System;
using System.Collections.Generic;
using System.Text;
using BrutalUi.Core;
using BrutalUi.Markup;

namespace BrutalUi.Framework;

/// <summary>
/// The resolve pass of one definitions load: the three name tables, the duplicate check, the check load of each
/// sound layout, the slot check, and the build of the three records.
/// </summary>
/// <remarks>
/// <para>
/// The pass runs after the read pass, also when the read pass found an error, so one load gives each error. It stops
/// only for <see cref="DefinitionErrorKind.NotWellFormed"/>. <strong>The follow-on rule (one fault, one error):</strong>
/// an entry with a name stays in its name table, also when another value of the entry has a fault; such an entry is
/// faulty. A reference to a faulty entry gives no error. A mode on a faulty layout skips the slot check. A placement
/// with no panel record skips the slot check.
/// </para>
/// <para>
/// A fault hides nothing in its own entry: the pass reads each other value of the entry and each placement of a
/// mode, so one load gives each fault. The check load covers each layout that is not faulty, also one that no mode
/// names (Decision 15), so a successful result holds no factory that throws inside <c>RegisterTo</c>.
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
        // A faulty entry keeps a null element and gives its reference no second error (the follow-on rule). The pass
        // looks up a present template also when another value of the entry has a fault, so one load gives each fault.
        PanelDefinition?[] panelRecords = new PanelDefinition?[_panelEntries.Count];
        for (int i = 0; i < _panelEntries.Count; i++)
        {
            PanelEntry entry = _panelEntries[i];
            bool faulty = !entry.Name.HasName || !entry.Slot.HasName || !IsFirstName(panelNames, entry.Name, i);
            byte[]? text = null;
            if (!entry.Template.HasName)
            {
                faulty = true;
            }
            else if (!_loader.TryGetTemplate(entry.Template.Text!, out text))
            {
                AddError(
                    DefinitionErrorKind.UnknownTemplate,
                    $"'{entry.Template.Text}' is not a registered template. Add it with DefinitionLoader.AddTemplate before the load.",
                    entry.Template.Line,
                    entry.Template.Column);
                faulty = true;
            }

            if (faulty)
            {
                continue;
            }

            byte[] name = Encoding.UTF8.GetBytes(entry.Name.Text!);
            PanelDefinition record = new(
                name,
                Encoding.UTF8.GetBytes(entry.Slot.Text!),
                CreateFactory(text!, entry.Template.Text!));
            panelRecords[i] = record;
            panels.Add(record);
        }

        // Step 3. The layouts. A faulty layout is faulty for the slot check of each mode on it (the follow-on rule).
        // The table keeps the slot names of each checked layout, in pre-order, first entry first. The pass looks up
        // a present template also when another value of the entry has a fault.
        bool[] layoutFaulty = new bool[_layoutEntries.Count];
        Dictionary<int, string[]> layoutSlots = [];
        for (int i = 0; i < _layoutEntries.Count; i++)
        {
            LayoutEntry entry = _layoutEntries[i];
            bool faulty = !entry.Name.HasName || !IsFirstName(layoutNames, entry.Name, i);
            byte[]? text = null;
            if (!entry.Template.HasName)
            {
                faulty = true;
            }
            else if (!_loader.TryGetTemplate(entry.Template.Text!, out text))
            {
                AddError(
                    DefinitionErrorKind.UnknownTemplate,
                    $"'{entry.Template.Text}' is not a registered template. Add it with DefinitionLoader.AddTemplate before the load.",
                    entry.Template.Line,
                    entry.Template.Column);
                faulty = true;
            }

            layoutFaulty[i] = faulty;
            if (faulty)
            {
                continue;
            }

            byte[] name = Encoding.UTF8.GetBytes(entry.Name.Text!);
            layouts.Add(new LayoutDefinition(name, CreateFactory(text!, entry.Template.Text!)));
        }

        // Step 3b. The check load (Q3, A5, Decision 15). Each layout that is not faulty loads its template one time,
        // also a layout that no mode names, so a fault of an unused layout lands at load time and RegisterTo can
        // only throw the failed-result rule. The extra load costs build time only (epic A10).
        for (int i = 0; i < _layoutEntries.Count; i++)
        {
            if (layoutFaulty[i])
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
        // A faulty mode keeps its placement loop, so a fault of the layout or the map hides no placement fault. The
        // record lands only when the mode itself is sound.
        for (int i = 0; i < _modeEntries.Count; i++)
        {
            ModeEntry entry = _modeEntries[i];
            bool faulty = !entry.Name.HasName || !IsFirstName(modeNames, entry.Name, i);
            int layoutIndex = -1;
            if (!entry.Layout.HasName)
            {
                faulty = true;
            }
            else if (!layoutNames.TryGetValue(entry.Layout.Text!, out layoutIndex))
            {
                AddError(
                    DefinitionErrorKind.UnknownLayout,
                    $"'{entry.Layout.Text}' is not a layout of this text. Add a Layout entry with this name.",
                    entry.Layout.Line,
                    entry.Layout.Column);
                layoutIndex = -1;
                faulty = true;
            }

            // The actions value names a registered map. No value gives no map, because DefinitionValue.HasName is the
            // single test for a value (an empty value already has its InvalidValue error from the read pass). A map
            // fault marks the mode faulty and gives no continue.
            UiActionMap? actionMap = null;
            if (entry.Actions.HasName && !_loader.TryGetActionMap(entry.Actions.Text!, out actionMap))
            {
                AddError(
                    DefinitionErrorKind.UnknownActionMap,
                    $"'{entry.Actions.Text}' is not a registered action map. Add it with DefinitionLoader.AddActionMap before the load.",
                    entry.Actions.Line,
                    entry.Actions.Column);
                faulty = true;
            }

            // A mode on a faulty layout, and a mode with no resolved layout, skip the slot check (the follow-on
            // rule). The UnknownLayout error of an unresolved layout stands on its own.
            bool checkSlots = layoutIndex >= 0 && !layoutFaulty[layoutIndex];
            string[] slots = layoutSlots.TryGetValue(layoutIndex, out string[]? known) ? known : [];
            string layout = entry.Layout.Text!;

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

            if (faulty)
            {
                continue;
            }

            byte[] modeName = Encoding.UTF8.GetBytes(entry.Name.Text!);
            byte[] layoutName = Encoding.UTF8.GetBytes(layout);
            modes.Add(new ModeDefinition(modeName, layoutName, placements.ToArray(), actionMap));
        }
    }
}
