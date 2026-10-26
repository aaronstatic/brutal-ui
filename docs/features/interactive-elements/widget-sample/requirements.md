# Widget Sample — Requirements

**Epic:** interactive-elements
**Created:** 2026-10-23

## Overview

This feature shows each widget of the epic in the HelloPanel sample on Vulkan. It adds the stylesheet rules for each pseudo-state, keeps the GPU smoke contract, and adds the allocation gate with each widget in the 1000-element tree. It closes the epic and Phase 2.

## Requirements

- HelloPanel shows a `Button`, a `Toggle`, a `Slider`, a `ScrollView` with content taller than the view, and a `TextField` (Q2).
- `HelloScreen.css` styles each widget, with a rule for `:hover`, `:active`, `:focus`, `:checked`, and `:disabled` where the widget sets the bit.
- Each delegate slot of the sample writes to a `Label`, so a person sees the change on screen.
- The smoke run with `--frames N` exits with code 0, and it exits with code 1 when `Update` or `Draw` allocates after warm-up.
- A new allocation gate builds the 1000-element tree with each widget type in it. It pushes pointer, scroll, key, and char events to each widget, and it asserts 0 bytes after warm-up (A9).
- `scripts/test-linux.sh` and `scripts/test-windows.ps1` pass.
- `docs/technical-design.md` section 5 gets the widget rules, and `docs/testing-linux.md` and `docs/testing-windows.md` get the manual checks.
- The epic `human-testing.md` lists one manual check for each widget.

## Dependencies

- button-and-toggle, slider, scroll-view, and text-field. All four must be complete.
- dev-ops: the test scripts and the GPU smoke contract.

## Out of Scope

- A new sample project. HelloPanel is the only sample of this epic (Q2).
- A widget gallery with each style variant.
- A performance benchmark for each widget. The allocation gate is the only gate.
