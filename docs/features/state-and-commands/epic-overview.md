# State and Commands - Epic Overview

**Epic:** state-and-commands
**Status:** Planned
**Last Updated:** 2026-10-29 10:00

> **This file is the epic marker.** Its presence in `docs/features/state-and-commands/` tells every Beast Mode command that this folder is an **epic**, not a plain feature. Keep it present and keep the required sections below filled in. The master `docs/overview.md` tracks this epic as a **single row**. The per-feature detail lives **here**.

---

## Purpose

This epic opens the `BrutalUi.Framework` package with the data layer of the mosaic-ui model: stores, bindings, the event bus, the service registry, the command registry, and the action map. A panel binds an element to a store slice, and a button or a key binds to a named command. The epic starts Phase 3. The panels-and-modes epic and the xml-markup epic both build on these types.

These features belong together because each one follows the same rule: a struct in a pooled list, set at build time, with no managed allocation in `Update` or `Draw` after warm-up. `BrutalUi.Core` gets one small contract, `IBindingSource`, and the binding pass. `BrutalUi.Framework` owns each other type.

---

## Features

| # | Feature | Status | Tasks | Description |
|---|---------|--------|-------|-------------|
| 1 | store-and-subscription | Planned | — | `IBindingSource` in Core, `Store<T>` and `StoreSubscription` in Framework, with a version counter and the first Framework tests. |
| 2 | element-bindings | Planned | — | The pooled binding list on `Element`, the binding pass in `UiContext.Update`, `Label.BindText`, a value binding for each widget, and the 0-byte gate. |
| 3 | commands-and-events | Planned | — | `EventBus`, `ServiceRegistry`, and `CommandRegistry` with a static handler plus target, and a command bind on `Button`. |
| 4 | action-map | Planned | — | `UiActionMap` with key, mouse, and gamepad bindings, the focus actions, the gamepad feed in `BrutalUi.Glfw`, and the HelloPanel sample. |

> Reference any feature with the slash form `state-and-commands/<feature-name>`. `/update-epic` refreshes the task counts from each `tasks.md`.

---

## Build Order / Dependencies

1. **store-and-subscription** - It comes first because each binding reads a store, and it sets the Core and Framework split with `IBindingSource`.
2. **element-bindings** - It depends on the store and the version counter from #1. It adds the binding pass to `UiContext.Update`, which is the one change to the frame order in this epic.
3. **commands-and-events** - It depends on #1 only for the service registry pattern. It can be built in parallel with #2.
4. **action-map** - It depends on #3 because each action calls a command, and on #2 for the sample that shows a bound label.

**Dependencies between features:**
- store-and-subscription → element-bindings (the store version and `IBindingSource`)
- store-and-subscription → commands-and-events (the Framework test project and the registry pattern)
- commands-and-events → action-map (each action invokes a command)
- element-bindings and commands-and-events have no dependency on each other. They can be built in parallel after #1.
- External: interactive-elements (the delegate slot on each widget), input-routing (`FocusNext`, `FocusPrevious`, the key events, `GlfwUiInput`), core-tree (`Element`, the dirty flags, the allocation gate), dev-ops (the solution, `tests/BrutalUi.Framework.Tests/`, the test scripts).

**Confirmed interview items:**
- Q4 Feature split: one feature `commands-and-events` holds `EventBus`, `ServiceRegistry`, and `CommandRegistry`.
- A1 Build order: store-and-subscription, element-bindings, commands-and-events, action-map. Four features.
- A2 `tests/BrutalUi.Framework.Tests/` exists in the solution. The epic adds tests there and makes no new project.
- A7 The HelloPanel sample changes live in action-map. No separate sample feature.

---

## Integration & Architecture

- **Within the epic:** `Store<T>` keeps a version counter. A binding is a struct in a pooled list on `Element`, with the store, a selector, and the last version it applied. The binding pass in `UiContext.Update` runs before the style pass, reads each store version, applies a changed value, and marks the element dirty. `CommandRegistry` maps a UTF-8 command name to a static handler plus a target. `UiActionMap` resolves a key, a mouse button, or a gamepad button to a command name and calls the same registry as a button click.
- **With other epics / features:** `Button.Clicked` and the other delegate slots from interactive-elements are the hook for a command bind. `UiContext.FocusNext` and `UiContext.FocusPrevious` from input-routing are the targets of the navigation actions. The panels-and-modes epic lists active maps on a mode and binds a panel root to a store. The xml-markup epic reads the `command="..."` attribute and calls the same bind.
- **Key architectural decisions for the epic as a whole:** Q1 `IBindingSource` lives in Core, and `Store<T>` lives in Framework. Q2 A binding pass in `UiContext.Update` pulls the store version before the style pass. Q3 A command handler is a static method plus a target, like the event slots. A4 A text binding formats a number into the label buffer through a span formatter, with no `ToString` in the frame loop. A5 A command bind on a `Button` runs at build time and may allocate once. Only the invoke path is zero-alloc. A6 The action map reads a key that no element handled through one slot on `UiContext`. The Glfw adapter polls the gamepad once per frame and pushes the state into the map. A8 This epic sets one active map on the context. A mode list of maps waits for panels-and-modes.

---

## Tech Debt / Findings

Cross-feature tech debt and review findings. `/review-epic` writes this section. Start empty.

- (none yet)

---

## Master Overview Rollup

- **Rollup status:** Planned (0/4 features)
- **One-line summary for master:** Stores, bindings, the event bus, the service and command registries, and the action map in `BrutalUi.Framework`. Opens Phase 3.

---

*This is a required file. Do not delete it. It marks the folder as an epic. Update it with `/update-epic state-and-commands` after work on the features, and run `/review-epic state-and-commands` to refresh the Tech Debt / Findings section.*
