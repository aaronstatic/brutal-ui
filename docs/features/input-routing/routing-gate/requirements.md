# Routing Gate - Requirements

**Epic:** input-routing
**Created:** 2026-10-22

## Overview

This feature adds `UiRoutingGate`, wires the input pass into `UiContext.Update`, guards the pass with the allocation gate, and shows the result in HelloPanel. It is the last feature of the input-routing epic and closes it.

## Requirements

- `UiRoutingGate.PointerOverUi` is true when the last hit result of pointer-routing is an element. `UiRoutingGate.KeyboardFocusInUi` is true when a focus owner exists.
- The game reads the gate after `UiContext.Update`. The gate is a struct or a pair of properties on `UiContext`, with no event.
- `UiContext.Update` runs the input pass before the style pass, then the transition pass, then layout.
- `ThousandElementAllocationTests` gains a case that drains a full queue on a warm 1000-element tree and measures 0 bytes.
- HelloPanel adds a `:hover` rule and a `:focus` rule to `HelloScreen.css`, and a label that shows the two gate values.
- The smoke run of HelloPanel pushes a synthetic pointer move and a Tab, and exits with code 1 when the input pass allocates.
- `scripts/test-linux.sh` and `scripts/test-windows.ps1` pass.

## Dependencies

- pointer-routing must be complete: the last hit result.
- keyboard-focus must be complete: the focus owner.

## Out of Scope

- `Button`, `Toggle`, `ScrollView`, and `TextField`. The interactive-elements epic adds them.
- The action map. The Phase 3 framework epic adds it.
