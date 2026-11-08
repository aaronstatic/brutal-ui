# Control Schemes - Requirements

**Epic:** input
**Created:** 2026-11-07

## Overview

This feature groups the bindings of `UiActionMap` in named control schemes, such as KeyboardMouse and Gamepad. The map reports the active device from the last input and calls a slot on a change. A game uses it for the prompt glyphs and for a rebind screen that shows one scheme at a time. (Q4, Q7)

## Requirements

- `AddScheme` names a scheme at build time. Each bind takes an optional scheme name. A binding with no scheme belongs to each scheme.
- `UiScheme` holds the standard names `KeyboardMouse` and `Gamepad`. A game can add other names. The names compare by their bytes.
- The map reports `ActiveScheme`: the scheme of the last binding that gave a value. A key or a mouse input sets KeyboardMouse. A gamepad input above the deadzone sets Gamepad.
- `OnSchemeChanged` is a slot on the map. The map calls it in `Update`, after the read, on a change only. The game writes it with `=`.
- A game can lock the map to one scheme with `SetScheme`. Then a binding of another scheme gives no value.
- The read and the change check allocate nothing after warm-up. (A3)
- Tests in `tests/BrutalUi.Framework.Tests/` cover the group, the active scheme, the slot, and the lock. HelloPanel shows the active scheme in a label. (A9)

## Dependencies

- input-actions is complete: the typed bindings.
- gamepad-axes is complete: the gamepad input that sets the Gamepad scheme.

## Out of Scope

- A glyph table or a sprite for a prompt. The game maps a scheme to its art.
- The persistence of the active scheme. binding-remap writes the bindings only.
- A scheme for each player in a local multiplayer. One map serves one player.
