# Input - Epic Overview

**Epic:** input
**Status:** Planned
**Last Updated:** 2026-11-07 18:20

> **This file is the epic marker.** Its presence in `docs/features/input/` tells every Beast Mode command that this folder is an **epic**, not a plain feature. Keep it present and keep the required sections below filled in. The master `docs/overview.md` tracks this epic as a **single row**. The per-feature detail lives **here**.

---

## Purpose

This epic gives a game the input that the element tree does not take. Today a game gets a key down and a press over no element through one slot, and the action map turns a press into a command. A game gets no pointer delta, no button release, no scroll, no held key, and no gamepad axis. The first consumer, the spacegame map-view epic, blocks on this gap in its map-controls feature. The epic adds a per-frame input state on `UiContext` that the routing gate filters, the gamepad axes in `BrutalUi.Glfw`, typed actions with composites in `UiActionMap`, named control schemes with the active device, and a runtime remap with a text line format.

These features belong together because each one reads the state that the one before it writes. The input state is the one source of truth for a held input. The gamepad feature fills the gamepad part of that state. The typed actions read the state and give the game a name for each input. The schemes group the bindings of each device. The remap edits the bindings at run time and writes them as text.

---

## Features

The constituent features of this epic, in build order. Each feature is a subfolder under `docs/features/input/`.

| # | Feature | Status | Tasks | Description |
|---|---------|--------|-------|-------------|
| 1 | input-state | Planned | - | The per-frame `UiInputState` on `UiContext`: the pointer position and delta, each button down, pressed, and released, the scroll delta, and the held keys. A view that the routing gate filters. |
| 2 | gamepad-axes | Planned | - | The full gamepad state in `BrutalUi.Glfw`: the buttons, six axes with a radial deadzone, a connected flag, for each of the 16 GLFW slots, written into the input state. |
| 3 | input-actions | Planned | - | Typed actions in `UiActionMap`: Button, Axis, and Vector2, with key composites, sticks, triggers, and the mouse delta, and the queries `WasPressed`, `IsHeld`, `WasReleased`, `ReadAxis`, and `ReadVector`. |
| 4 | control-schemes | Planned | - | Named control schemes on the map, the active device from the last input, and a change slot for the prompt glyphs of a game. |
| 5 | binding-remap | Planned | - | A runtime rebind: listen for the next input, replace a binding, report a conflict, restore the defaults, and write or parse the bindings as text lines. |

> Reference any feature with the slash form `input/<feature-name>`. Task counts come from each feature's own `tasks.md`, and `/update-epic` refreshes them.

---

## Build Order / Dependencies

1. **input-state** - It comes first because each later feature reads it. It is the one Core change of the epic, and it closes the four gaps that map-controls names.
2. **gamepad-axes** - It depends on the gamepad fields of #1. It changes `BrutalUi.Glfw` and the Core enum only, so it is small and tests with a hand-made state.
3. **input-actions** - It depends on #1 for the held state and on #2 for the stick and trigger bindings. It is the hardest feature, and it unblocks spacegame map-controls (Q1).
4. **control-schemes** - It depends on the typed bindings of #3, because a scheme is a group of bindings by device.
5. **binding-remap** - It depends on #3 for the binding list and on #4 so a rebind stays inside one scheme. It closes the epic.

**Dependencies between features:**
- input-state → gamepad-axes (the gamepad fields of the state)
- input-state → input-actions (the held state for each query)
- gamepad-axes → input-actions (the axis values for a stick or a trigger binding)
- input-actions → control-schemes (the typed bindings)
- input-actions → binding-remap (the binding list)
- control-schemes → binding-remap (the scheme of a rebind)
- No pair builds in parallel. Each feature tests on the one before it.
- External: `Ahwoo.Brutal.Glfw` for the gamepad poll, pinned in `Directory.Packages.props`. spacegame moves its submodule after #3.

**Confirmed interview items that shape the order:**
- Q1 map-controls waits for the typed actions. Features 1 to 3 unblock it.
- Q7 Control schemes are a feature of their own, after the actions.
- A1 The epic is `input`, with five features in this order: input-state, gamepad-axes, input-actions, control-schemes, binding-remap.
- A5 No existing feature moves into this epic. input-routing and state-and-commands/action-map stay where they are.

---

## Integration & Architecture

- **Within the epic:** The input pass of `UiContext.Update` writes `UiInputState` after it drains the ring. `ui.Input` is the raw state, and `ui.Input.Game` is the view that the routing gate filters. `GlfwUiInput.PollGamepad` writes the gamepad state into the same struct. `UiActionMap` reads the state at `Update` and holds the value of each action for the frame. A scheme is a tag on each binding. The remap edits the binding list of the map and writes it as text.
- **With other epics / features:** input-routing gives the ring, the walk, the focus owner, and `UiRoutingGate`. state-and-commands gives `UiActionMap`, `CommandRegistry`, and the gamepad button poll. panels-and-modes/mode-set switches the map of a mode and keeps the one active map rule. The spacegame map-view epic reads the actions in map-controls.
- **Key architectural decisions for the epic as a whole:**
  - Q2 A per-frame input state on `UiContext`, with a view that the routing gate filters.
  - Q3 Gamepad axes with a deadzone, for all 16 GLFW slots, with a connected flag. No rumble.
  - Q4 Typed actions (Button, Axis, Vector2) with composites, and control schemes.
  - Q5 A text line format for the bindings, with no file IO in Framework.
  - Q6 `UiActionMap` changes in place. The old button methods stay.
  - A2 Core gets the input state and the gamepad state types. `BrutalUi.Glfw` fills them. Framework owns the actions, the schemes, and the remap. Core stays free of GLFW.
  - A3 The input state and each action query allocate nothing after warm-up. The allocation gate grows to cover them.
  - A4 The map of a mode and the map switch stay in panels-and-modes/mode-set. This epic keeps the one active map rule.
  - A6 The game polls the gamepad one time in each frame, as today. No background thread.
  - A9 Each feature updates HelloPanel and section 9 of `technical-design.md`.

---

## Tech Debt / Findings

- (none yet - `/review-epic` adds cross-feature findings here)

---

## Master Overview Rollup

- **Rollup status:** Planned (0/5 features)
- **One-line summary for master:** A gated per-frame input state, gamepad axes, typed actions with composites, control schemes, and a runtime remap with a text format. It unblocks the spacegame map controls.

---

*This is a required file. Do not delete it. It marks the folder as an epic. Update it with `/update-epic input` after work on the features, and run `/review-epic input` to refresh the Tech Debt / Findings section.*
