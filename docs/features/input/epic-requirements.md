# Input - Epic Requirements

**Created:** 2026-11-07
**Phase:** Phase 3

> Epic-level requirements. `/plan-epic input` reads this file. Each child feature below has its own `requirements.md` that `/plan-feature input/<feature-name>` consumes.

## Overview

The input epic gives a game the input that no element takes, in a form that a game can name, remap, and extend to a gamepad. It owns the per-frame input state in `BrutalUi.Core`, the gamepad state in `BrutalUi.Glfw`, and the typed actions, the control schemes, and the remap in `BrutalUi.Framework`. The spacegame map-view epic blocks on it: the map-controls feature needs the pointer delta, the button release, the scroll, and the held keys, each gated by the routing gate.

## Requirements

- The game reads a per-frame input state from `UiContext` after `Update`: the pointer position and delta, each button down, pressed, and released, the scroll delta, the held keys, and the gamepad state. (Q2)
- A view of the state applies the routing gate. The pointer part is empty when the pointer is over the UI. The key part is empty when the keyboard focus is in the UI. (A7)
- The gamepad state holds the buttons, six axes with a radial deadzone, and a connected flag, for each of the 16 GLFW slots. The game polls one time in each frame. No rumble. (Q3, A6)
- `UiActionMap` holds typed actions: Button, Axis, and Vector2. A binding is a key, a mouse button, a gamepad button, a stick, a trigger, the mouse delta, or a key composite such as W, A, S, D to Vector2. (Q4)
- The game queries an action by name: `WasPressed`, `IsHeld`, `WasReleased`, `ReadAxis`, and `ReadVector`. A Button action still invokes its command on a press. (Q4)
- `UiActionMap` changes in place. The old button methods stay or map to the new ones. (Q6)
- A map groups its bindings in named control schemes. The map reports the active device from the last input and calls a slot on a change. (Q4, Q7)
- A game rebinds an action at run time: it listens for the next input, replaces the binding, gets a conflict report, and restores the defaults. (Q5)
- Framework writes and parses the bindings as text lines, one binding per line, into a span or a string. The game owns the file. (Q5)
- The input state, each action query, and the gamepad poll allocate nothing after warm-up. The allocation gate grows to cover them. (A3)
- Core gets the input state and the gamepad state types. `BrutalUi.Glfw` fills them. Framework owns the actions, the schemes, and the remap. Core stays free of GLFW. (A2)
- Each feature updates HelloPanel and section 9 of `technical-design.md`. (A9)

## Planned Features

1. **input-state** - The per-frame `UiInputState` on `UiContext` and the gated view. It comes first because each later feature reads it, and it closes the four gaps that map-controls names.
2. **gamepad-axes** - The full gamepad state in `BrutalUi.Glfw`, written into the input state. It depends on the gamepad fields of #1.
3. **input-actions** - The typed actions, the composites, and the queries in `UiActionMap`. It depends on #1 and #2. It unblocks spacegame map-controls. (Q1)
4. **control-schemes** - The named schemes, the active device, and the change slot. It depends on the typed bindings of #3. (Q7)
5. **binding-remap** - The runtime rebind and the text line format. It depends on #3 and #4 and closes the epic.

## Dependencies

- input-routing: the event ring, the walk, the focus owner, `UiRoutingGate`, and `GlfwUiInput`.
- state-and-commands: `UiActionMap`, `CommandRegistry`, `UiGamepadButtons`, and `PollGamepad`.
- core-tree: `UiContext.Update` and the allocation gate.
- panels-and-modes/mode-set switches the map of a mode. It is not a blocker, and this epic keeps the one active map rule. (A4)
- The spacegame map-view epic consumes this epic. map-controls waits for features 1 to 3. (Q1)

## Out of Scope

- Touch input, an SDL adapter, gamepad rumble, and IME text input. (A8)
- A map stack or more than one active map. The map of a mode stays in panels-and-modes/mode-set. (A4)
- A background thread for the gamepad poll. (A6)
- File IO for the bindings. The game owns the file. (Q5)
- A hot-plug event for a gamepad. The connected flag covers it. (Q3)
- A rebind screen. The epic gives the API, and a game builds the screen.
- A move of input-routing or state-and-commands/action-map into this epic. (A5)

---

*Consumed by `/plan-epic input`. After the planning, run `/plan-feature input/<feature-name>` for each feature in the build order.*
