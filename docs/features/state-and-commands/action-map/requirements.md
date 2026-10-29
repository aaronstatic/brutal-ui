# Action Map - Requirements

**Epic:** state-and-commands
**Created:** 2026-10-29

## Overview

This feature adds `UiActionMap` to `BrutalUi.Framework`, the gamepad feed to `BrutalUi.Glfw`, and the Phase 3 changes to the HelloPanel sample. A map names an action, binds it to a key, a mouse button, or a gamepad button, and calls the same `CommandRegistry` as a button click. It closes the state-and-commands epic.

## Requirements

- `UiActionMap` names actions such as `Confirm`, `Cancel`, `NavigateUp`, and `NavigateDown`. Each action maps to a command name.
- A binding of an action to a key with modifiers, a mouse button, or a gamepad button lives in a struct list that grows at build time only.
- The map reads a key that no element handled through one slot on `UiContext`. A focused `TextField` keeps its edit keys. (A6)
- The Glfw adapter polls the gamepad once per frame through `Glfw.GetGamepadState` and pushes the state into the map. `BrutalUi.Framework` references no GLFW type. (A6)
- `NavigateUp` and `NavigateDown` call `UiContext.FocusPrevious` and `UiContext.FocusNext`.
- This epic sets one active map on the context. (A8)
- The resolve and invoke path allocates nothing after warm-up.
- HelloPanel shows one store that drives a `Label`, one command on a `Button`, and one action map binding on a key. The smoke run drives each one, prints a count line, and exits with code 1 on a count of 0. (A7)
- Tests cover the key, mouse, and gamepad resolve, the unhandled-key rule, the focus actions, and a zero-byte check.

## Dependencies

- commands-and-events must be complete: `CommandRegistry` and the command bind.
- element-bindings must be complete, for the bound label in the sample.
- The input-routing epic: the key events, `UiContext.FocusNext`, `UiContext.FocusPrevious`, and `GlfwUiInput`.
- The dev-ops epic: the GPU smoke contract and the test scripts.

## Out of Scope

- A mode that lists its active maps. The panels-and-modes epic owns it.
- A rebind screen or a saved binding file.
- Gamepad navigation beyond the focus actions, such as a spatial neighbor search.
- A gamepad axis or a deadzone rule beyond the button state.
