# State and Commands - Epic Requirements

**Created:** 2026-10-29
**Phase:** Phase 3, framework

> Epic-level requirements. `/plan-epic state-and-commands` reads this file to scope the epic. Each child feature gets its own `requirements.md` from `/plan-epic`.

## Overview

This epic opens the `BrutalUi.Framework` package with the data layer of the mosaic-ui model: stores, bindings, the event bus, the service registry, the command registry, and the action map. A panel binds an element to a store slice, and a button or a key binds to a named command. These types belong together because the panels-and-modes epic and the xml-markup epic both build on them, and because each one follows the same rule: a pooled struct list on the element, no managed allocation after warm-up.

## Requirements

- `Store<T>` holds one state value and a version counter. It exposes the value through `IBindingSource`, which lives in `BrutalUi.Core`. The store replaces `INotifyBindablePropertyChanged` from mosaic-ui. (Q1)
- A binding is a struct in a pooled list on the element. `Label.BindText(store, selector)` and a binding for each widget value subscribe to a store slice. A binding pass in `UiContext.Update` runs before the style pass, pulls each changed store version, applies the value, and marks the element dirty. (Q2)
- A text binding formats a number into the label buffer through a span formatter. No `ToString` runs in the frame loop. (A4)
- A binding runs with zero allocation after warm-up. A new gate `BoundTreeAllocationTests` puts a 1000-element tree with a changing store into the 0-byte check. (A3)
- `EventBus` publishes a typed event to each subscriber with no allocation in the publish path. `ServiceRegistry` resolves a service by type. Both copy the mosaic-ui design.
- `CommandRegistry` maps a UTF-8 command name to a static handler plus a target, like the event slots. (Q3) A command bind on a `Button` runs at build time and may allocate once. Only the invoke path is zero-alloc. (A5) The registry reports an unknown name without an exception in the frame loop.
- `UiActionMap` names actions such as `Confirm`, `Cancel`, and `NavigateUp`. It binds keys, mouse buttons, and gamepad buttons, and it calls the same `CommandRegistry` as a button click. `NavigateUp` and its siblings call `UiContext.FocusNext` and `UiContext.FocusPrevious`.
- The action map reads a key that no element handled through one slot on `UiContext`. The Glfw adapter polls the gamepad once per frame and pushes the state into the map. (A6)
- This epic sets one active map on the context. A mode list of maps waits for panels-and-modes. (A8)
- `BrutalUi.Framework` references `BrutalUi.Core` only. The gamepad feed from `Glfw.GetGamepadState` lives in `BrutalUi.Glfw`.
- Each type uses UTF-8 text, `Brutal.Numerics` types, file-scoped namespaces, and `required` create-info structs.
- The existing project `tests/BrutalUi.Framework.Tests/` covers each type. The epic makes no new project. (A2) `scripts/test-linux.sh` and `scripts/test-windows.ps1` run it.
- HelloPanel shows one store that drives a `Label`, one command on a `Button`, and one action map binding on a key. The smoke run drives each one and fails on a count of 0. The sample changes live in action-map. (A7)

## Planned Features

1. **store-and-subscription** - `IBindingSource` in Core, `Store<T>` and `StoreSubscription` in Framework with a version counter, and the first Framework tests - It comes first because each binding reads a store.
2. **element-bindings** - The pooled binding list on `Element`, the binding pass in `UiContext.Update`, `Label.BindText`, a value binding for each widget, and the 0-byte gate - It depends on the store and the version counter from #1.
3. **commands-and-events** - `EventBus`, `ServiceRegistry`, and `CommandRegistry` with a static handler plus target, and a command bind on `Button` - It depends on #1 for the test project and the registry pattern. It can be built in parallel with #2.
4. **action-map** - `UiActionMap`, the key, mouse, and gamepad bindings, the gamepad feed in `BrutalUi.Glfw`, the focus actions, and the HelloPanel sample - It depends on #3 because each action calls a command.

## Dependencies

- The interactive-elements epic: the delegate slot on each widget, and `TextField` for a two-way text binding.
- The input-routing epic: `UiContext.FocusNext`, `UiContext.FocusPrevious`, the key events, and `GlfwUiInput` in `BrutalUi.Glfw`.
- The core-tree epic: `Element`, the dirty flags, and the allocation gate.
- The dev-ops epic: the solution, the package pins, and the test scripts.

## Out of Scope

- Panel, layout, mode, and window definitions, `PanelController`, `UiManager`, `SlotContainer`, and `ModeHistory`. The panels-and-modes epic owns them. A mode lists its active action maps there.
- The XML element loader and the `command="..."` attribute. The xml-markup epic owns them.
- `WindowChrome`, `WindowManager`, persistence, `ListView`, and `DataList`. Phase 4 owns them.
- The debugger window. Phase 4 owns it.
- A gamepad navigation layer beyond the focus actions in the map.

---

*Consumed by `/plan-epic state-and-commands`. After planning, run `/plan-feature state-and-commands/<feature-name>` for each feature in the build order.*
