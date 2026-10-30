# Panels and Modes - Epic Requirements

**Created:** 2026-10-30
**Phase:** Phase 3, framework

> Epic-level requirements. `/plan-epic panels-and-modes` reads this file to scope the epic. Each child feature gets its own `requirements.md` from the plan.

## Overview

This epic brings the panel, slot, and mode model of mosaic-ui to the brutal-ui tree. A game screen is a set of named panels in named slots, and a mode names the set. The framework diffs the current mode against the next one and moves the panels. These features belong together because the slot container, the panel controller, and the mode diff share one data model and one pass in `UiContext.Update`.

## Requirements

- A `PanelDefinition` record names a panel, its root element factory, and its default slot. A `LayoutDefinition` names the slots of a layout. A `ModeDefinition` names a layout and the panels in each slot.
- A `PanelController` owns one `PanelInstance`. The instance root is an `Element`. The controller binds the root to a store and gets the `CommandRegistry`, the `EventBus`, and the `ServiceRegistry` through the framework.
- A `SlotContainer` is an `Element` that holds the panel roots of one slot. The slot names are UTF-8.
- `UiManager` holds the registered definitions and the current mode. `SetMode` diffs the current mode against the next one. A panel that both modes share stays alive and keeps its state. A panel that only the old mode holds leaves its slot. A panel that only the new mode holds enters its slot.
- `ModeHistory` records each applied mode and supports a back step only. No forward step (A3).
- A mode holds one optional `UiActionMap`. A mode change calls `Activate` on the map of the new mode, or `Deactivate` on the old map when the new mode has none (A6).
- The diff is deferred (Q1). `SetMode` records the target mode. `UiContext.Update` calls a new slot `OnBeforeUpdate` first, and `UiManager` applies the diff there, before the input pass. The slot is set with `=`, as `OnUnhandledInput` is (A2).
- A panel root is created one time at the first mount and lives for the life of the manager (Q3). A leave detaches the root and keeps its state and bindings.
- `LayoutDefinition` holds a factory for the layout root (Q4). `UiManager` finds each `SlotContainer` by name in the built root. The layout root stays the one `UiContext` root (A4).
- A slot holds an ordered list of panels, in the order of the mode definition (A4). A panel that both modes hold stays in place. A move to a new slot counts as a leave and an enter (A5).
- `SetMode`, the diff, and each frame with no mode change allocate 0 bytes after warm-up. `ModeDiffAllocationTests` in `tests/BrutalUi.Framework.Tests/` guards it (A7).
- The HelloPanel sample registers two modes and switches between them with a command and a key action. The smoke run prints a seventh line with the mode counts and exits with code 1 when a count is 0.
- `scripts/test-linux.sh` and `scripts/test-windows.ps1` pass.

## Planned Features

1. **panel-definition** - `PanelDefinition`, `LayoutDefinition`, `ModeDefinition`, `PanelController`, and `PanelInstance` as records and classes in `BrutalUi.Framework`. - It sets the data model that each later feature reads.
2. **slot-container** - `SlotContainer` as an `Element`, the mount and unmount of a panel root in a slot, and the slot lookup by name in a layout root. - It depends on `PanelInstance` from #1.
3. **mode-set** - `UiManager`, `SetMode`, the deferred diff through `UiContext.OnBeforeUpdate`, `ModeHistory`, and the action map of a mode (Q2). - It depends on #1 and #2 and is the center of the epic.
4. **panel-sample** - Two modes in HelloPanel, a mode button and a key action, the seventh smoke line, and the allocation gate for the mode diff (A8). - It closes the epic and needs #1 to #3.

## Dependencies

- `core-tree` for `Element` and `UiContext`.
- `state-and-commands` for `Store<T>`, the binding pass, `CommandRegistry`, `EventBus`, `ServiceRegistry`, and `UiActionMap`.
- `interactive-elements` for `Button` in the sample.
- The frame order in section 4 and the component table in section 10 of `docs/technical-design.md`.

## Out of Scope

- The XML loader for panel and mode definitions. The xml-markup epic owns it.
- `WindowChrome`, `WindowManager`, and `WindowPersistence`. The windows-and-lists epic owns them in Phase 4.
- `WorldPanel` and the offscreen render target. Phase 5 owns them.
- Panel transitions beyond the paint transitions of the style system (A9).
- A forward step in `ModeHistory` (A3).

---

*Consumed by `/plan-epic panels-and-modes`. After planning, run `/plan-feature panels-and-modes/<feature-name>` for each feature in the build order.*
