# Input State - Requirements

**Epic:** input
**Created:** 2026-11-07

## Overview

This feature adds `UiInputState`, a per-frame struct on `UiContext` that holds the device state after the input pass. A game reads it after `Update`. A second view applies the routing gate, so the game ignores an input that the UI owns. It is the one Core change of the epic and the base of each later feature.

## Requirements

- `UiContext.Input` returns the raw `UiInputState` of the last `Update`. The input pass writes it after it drains the ring. (Q2)
- The pointer part holds the position, the delta since the last `Update`, and the scroll delta of the frame. A pointer at (NaN, NaN) gives a zero delta.
- The button part holds, for each `PointerButton`, three flags: down, pressed this frame, and released this frame. (A7)
- The key part holds the same three flags for each GLFW key code, with the modifiers of the last key event. A key repeat sets no pressed flag.
- The gamepad part holds a slot for each of the 16 GLFW gamepads. This feature defines the fields. gamepad-axes fills them.
- `UiContext.GameInput` returns the view that the routing gate filters. The pointer, button, and scroll parts are empty when `PointerOverUi` is `true`. The key part is empty when `KeyboardFocusInUi` is `true`. (A7)
- A button that goes down over the UI and comes up over the game gives no released flag in the game view. A press belongs to the side that got the down.
- A focus loss and a `KeyboardEnabled` switch to `false` clear each held key, so no key stays down with no key up.
- The state is a struct of fixed size. The write and each read allocate nothing after warm-up. The allocation gate covers the input pass with the state. (A3)
- Tests in `tests/BrutalUi.Core.Tests/` cover the flags, the delta, the gate view, the press ownership, and the clear rules. HelloPanel shows the pointer delta and a held key in a label. Section 9 of `technical-design.md` gets the state. (A9)

## Dependencies

- input-routing is complete: the ring, the input pass, and `UiRoutingGate`.
- No sibling feature. This is the first feature of the epic.

## Out of Scope

- The gamepad values. gamepad-axes fills the slots.
- A named action or a binding. input-actions reads this state.
- A new event type or a wider `OnUnhandledInput` slot. The slot stays as it is. (Q2)
- Touch input. (A8)
