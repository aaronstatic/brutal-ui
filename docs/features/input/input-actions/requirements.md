# Input Actions - Requirements

**Epic:** input
**Created:** 2026-11-07

## Overview

This feature turns `UiActionMap` into a typed action map. An action is a Button, an Axis, or a Vector2. A binding is a key, a mouse button, a gamepad button, a stick, a trigger, the mouse delta, or a key composite. The map reads `UiInputState` at `Update` and holds the value of each action for the frame. It is the hardest feature of the epic, and it unblocks the spacegame map-controls feature. (Q1)

## Requirements

- `AddAction` takes an action type: Button, Axis, or Vector2. A Button action keeps its command and invokes it on a press, as today. An Axis or Vector2 action has no command. (Q4, Q6)
- The bindings: `BindKey`, `BindMouseButton`, and `BindGamepadButton` stay. New binds are a stick to a Vector2, a trigger or a stick axis to an Axis, the mouse delta to a Vector2, the scroll to an Axis, and a key composite of two or four keys to an Axis or a Vector2.
- A Button binding on an Axis or Vector2 action, and an axis binding on a Button action, throw `ArgumentException` at build time.
- The queries by action name: `WasPressed`, `IsHeld`, `WasReleased`, `ReadAxis`, and `ReadVector`. A Button query on an Axis action throws. A name the map does not hold returns `false` or zero. (Q4)
- The map reads `UiContext.GameInput` in `Update`, so a gated input gives no value. A Vector2 from two bindings sums and clamps to length 1. A Button is held when one binding holds it.
- A press through the slot still invokes the command one time, with the repeat rule and the text guard of action-map. The queries and the commands agree on one press.
- `UiActionMap` changes in place. `HelloPanel`, the action-map tests, and the mode-set switch keep working. (Q6)
- `Activate` and `Deactivate` stay the one switch API of a map. When the typed map needs a per-frame read hook on `UiContext`, `Activate` writes it and `Deactivate` clears it. panels-and-modes/mode-set calls these two methods only and does not change.
- `Deactivate` releases each held action of the map. A mode switch mid-hold gives no stale held or released flag in the next map.
- Each query and the frame read allocate nothing after warm-up. `ActionMapAllocationTests` grows to cover them. (A3)
- Tests in `tests/BrutalUi.Framework.Tests/` cover each binding kind, the composite sum, the gate, the type checks, and the press agreement. HelloPanel shows a W, A, S, D vector and a stick in a label. Section 9 of `technical-design.md` gets the typed map. (A9)
- After this feature, spacegame moves its submodule and map-controls continues. (Q1)

## Dependencies

- input-state is complete: `UiContext.GameInput`.
- gamepad-axes is complete: the axis values.
- state-and-commands/action-map is complete: `UiActionMap` and `CommandRegistry`.

## Out of Scope

- Named control schemes and the active device. control-schemes adds them. (Q7)
- A rebind at run time and the text format. binding-remap adds them.
- A second active map or a map stack. (A4)
- A hold time, a tap count, or a chord of two actions.
