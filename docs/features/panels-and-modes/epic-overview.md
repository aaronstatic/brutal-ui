# Panels and Modes - Epic Overview

**Epic:** panels-and-modes
**Status:** Ready for Review
**Last Updated:** 2026-11-08 11:44

> **This file is the epic marker.** Its presence in `docs/features/panels-and-modes/` tells every Beast Mode command that this folder is an **epic**, not a plain feature. Keep it present and keep the required sections below filled in. The master `docs/overview.md` tracks this epic as a **single row**. The per-feature detail lives **here**.

---

## Purpose

This epic brings the panel, slot, and mode model of mosaic-ui to the brutal-ui tree. A game screen is a set of named panels in named slots, and a mode names the set. `UiManager` diffs the current mode against the next one, detaches the panels that leave, and attaches the panels that enter. A panel that both modes hold stays in place with its state and its bindings. The epic closes the framework layer of Phase 3, with the xml-markup epic as the one open item after it.

These features belong together because the definitions, the slot container, and the mode diff share one data model and one hook in `UiContext.Update`. `BrutalUi.Core` gets one slot, `OnBeforeUpdate`. `BrutalUi.Framework` owns each other type.

---

## Features

| # | Feature | Status | Tasks | Description |
|---|---------|--------|-------|-------------|
| 1 | panel-definition | Complete | 13/13 (100%) | `PanelDefinition`, `LayoutDefinition`, `ModeDefinition`, `PanelController`, and `PanelInstance` in `BrutalUi.Framework`. |
| 2 | slot-container | Complete | 10/10 (100%) | `SlotContainer` as an `Element`, the mount and unmount of a panel root, and the slot lookup by name in a layout root. |
| 3 | mode-set | Complete | 25/25 (100%) | `UiManager`, `SetMode`, the deferred diff through `UiContext.OnBeforeUpdate`, `ModeHistory`, and the action map of a mode. |
| 4 | panel-sample | Complete | 25/25 (100%) | Two modes in HelloPanel, the mode line of the smoke run, and the `ModeDiffAllocationTests` gate. |

> Reference any feature with the slash form `panels-and-modes/<feature-name>`. `/update-epic` refreshes the task counts from each `tasks.md`.

---

## Build Order / Dependencies

1. **panel-definition** - It comes first because each later feature reads the definitions and the controller.
2. **slot-container** - It depends on `PanelInstance` from #1 for the root that it attaches. It can be built in parallel with the definitions of #1 after the record shapes are set.
3. **mode-set** - It depends on #1 and #2. It adds the one Core slot and the diff, and it is the center of the epic.
4. **panel-sample** - It depends on #1 to #3. It closes the epic with the sample and the allocation gate.

**Dependencies between features:**
- panel-definition → slot-container (`PanelInstance.Root`)
- panel-definition → mode-set (`ModeDefinition`, `LayoutDefinition`, `PanelController`)
- slot-container → mode-set (the mount and unmount calls)
- mode-set → panel-sample (`UiManager` and `SetMode`)
- External: state-and-commands (`Store<T>`, the binding pass, `CommandRegistry`, `EventBus`, `ServiceRegistry`, `UiActionMap`), core-tree (`Element`, `UiContext`, the allocation gate), interactive-elements (`Button` in the sample), dev-ops (the test scripts).

**Confirmed interview items:**
- Q1 Diff time: the diff is deferred. `SetMode` records the target, and `UiManager` applies it from a new `UiContext.OnBeforeUpdate` slot.
- Q2 Split: the action map list of a mode folds into mode-set. Four features.
- Q3 Lifetime: a panel root is created one time at the first mount and lives for the life of the manager.
- Q4 Layout: `LayoutDefinition` holds a factory for the layout root. `UiManager` finds each `SlotContainer` by name.
- A1 Build order: panel-definition, slot-container, mode-set, panel-sample.
- A2 `BrutalUi.Framework` references Core only, so the diff reaches `Update` through the one Core slot of Q1. The slot is set with `=`, as `OnUnhandledInput` is.
- A3 `ModeHistory` is in scope with a back step only. No forward step.
- A4 A slot holds an ordered list of panels, in the order of the mode definition. A mount attaches the root as a child of the `SlotContainer`. The layout root stays the one `UiContext` root.
- A5 A panel that both modes hold stays in place. A move to a new slot counts as a leave and an enter.
- A6 A mode change replaces the active `UiActionMap` through `Activate`. A mode with no map calls `Deactivate` on the old map.
- A7 The tests live in `tests/BrutalUi.Framework.Tests/`. A new `ModeDiffAllocationTests` gate measures `SetMode` plus one `Update` at 0 bytes after warm-up.
- A8 The HelloPanel sample changes live in panel-sample: two modes, a mode button and a key action, and the seventh smoke line.
- A9 No panel transitions beyond the style system, and no window chrome. Phase 4 owns windows.

---

## Integration & Architecture

- **Within the epic:** A `PanelDefinition` names a panel, a root factory, and a default slot. A `LayoutDefinition` names a layout and holds a factory for the layout root. A `ModeDefinition` names a layout, the panels in each slot, and an optional `UiActionMap`. A `PanelController` owns one `PanelInstance`, whose root is an `Element`. `SlotContainer` is an `Element` that holds the roots of one slot as children. `UiManager` holds the definitions, the controllers, the current mode, and the target mode. `SetMode` records the target. The `OnBeforeUpdate` slot of the context calls the manager, and the manager applies the diff before the input pass. `ModeHistory` records each applied mode.
- **With other epics / features:** `UiContext.OnBeforeUpdate` is the one Core change. It runs first in `Update`, so the input pass, the binding pass, the style pass, and the layout pass see the new tree in the same frame. A panel root binds to a store through the element bindings of state-and-commands. The mode diff calls `UiActionMap.Activate` and `Deactivate`. The xml-markup epic reads a panel, a layout, and a mode from XML into the same records, and gives a factory for each root.
- **Key architectural decisions for the epic as a whole:** Q1, Q3, Q4, A2, and A4 above. The diff allocates nothing after warm-up: the manager rents each list at build time, and a panel root is created one time.

---

## Tech Debt / Findings

- (none yet - `/review-epic` will surface cross-feature integration issues and per-feature tech debt here)

---

## Master Overview Rollup

- **Rollup status:** Ready for Review (4/4 features complete, 73/73 tasks on the built features)
- **One-line summary for master:** All four features are done. panel-definition, slot-container, and mode-set give the records, `SlotContainer`, `UiManager` with the deferred mode diff, `ModeHistory`, and `SetMode`. panel-sample closes the epic with no `src/` change. It adds two HelloPanel modes, the `Mode (F1)` button, F1 and F2, the seventh smoke line, and `ModeDiffAllocationTests`, the eighteenth allocation gate. 51 new tests over the epic base. The solution reads 2824 (3 skipped), 0 failed. The windowed smoke demos await a host with a GPU window. The xml-markup epic is next.

---

*This is a required file - do not delete it; it marks the folder as an epic. Update it with `/update-epic panels-and-modes` after working on the epic's features, and run `/review-epic panels-and-modes` to refresh the Tech Debt / Findings section.*
