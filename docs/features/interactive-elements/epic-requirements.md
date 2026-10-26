# Interactive Elements - Epic Requirements

**Created:** 2026-10-23
**Phase:** Phase 2, style and input. This epic closes the phase.

> Epic-level requirements. `/plan-epic interactive-elements` reads this file to scope the epic. Each child feature gets its own `requirements.md` from `/plan-epic`.

## Overview

This epic delivers the built-in widgets of `BrutalUi.Core`: `Button`, `Toggle`, `Slider`, `ScrollView`, and `TextField`. Each widget is an `Element` subclass that registers handlers on the event slots of input-routing and reads the focus owner. The widgets belong together because each one uses the same pattern: a handler on a slot, a pseudo-state for the stylesheet, and a change notification for the game. The epic ends Phase 2 and gives the framework epic the widgets that commands, bindings, and the action map need.

## Requirements

- Each widget lives in `src/BrutalUi.Core/Elements/` and references no ImGui package.
- Each widget has its own `ElementType` id, so a type rule in a stylesheet matches it.
- Each widget registers its handlers one time at build time. A handler is a static method plus a target object.
- `Update` and `Draw` allocate 0 bytes after warm-up with each widget in the 1000-element tree. A new allocation gate guards it.
- A widget reports a change through a struct event or a delegate that the game sets at build time, with no allocation on the change.
- `Button` raises a click on a pointer up over the pressed element, and on Enter or Space when it owns focus.
- `Toggle` holds a checked state, exposes it as a pseudo-state for the stylesheet, and flips it on click and on Space.
- `Slider` holds a float value in a range, moves on drag and on the arrow keys, and clamps and steps the value. It is horizontal only (A8).
- `ScrollView` holds a scroll offset, clips its content, moves on the scroll event and on drag, and reports its offset to the hit test and the draw walk. It does not change the layout engine (A7).
- `TextField` holds UTF-8 text in a fixed buffer, moves a caret on the key events, inserts on the char event, and draws a caret and a selection.
- `TextField` grows no managed buffer during typing. The capacity is a create-info value (A5).
- Each widget is keyboard reachable through the focus owner and Tab.
- The HelloPanel sample shows each widget on Vulkan, and the GPU smoke run exits with code 0. No new sample project (Q2).
- The widget-sample feature adds one allocation gate with each widget in the 1000-element tree (A9).
- `Toggle` sets the existing `PseudoStates.Checked` bit. The epic adds no new pseudo-state (A3).
- `Button` activates on Enter and Space. `Toggle` and `Slider` activate on Space and the arrow keys (A4).
- `scripts/test-linux.sh` and `scripts/test-windows.ps1` pass.
- The docs update `docs/technical-design.md` section 5 with the widget rules.

## Planned Features

1. **button-and-toggle** — `Button` and `Toggle` with click, keyboard activation, the `Checked` pseudo-state, and the change delegate — It comes first because it sets the widget pattern that each later feature copies.
2. **slider** — `Slider` with a float value, a range, a step, drag, and the arrow keys — It depends on the drag rule from #1 and adds the first continuous value.
3. **scroll-view** — `ScrollView` with a scroll offset, clip, scroll events, drag, and the offset in the hit test and the draw walk — It depends on the pattern from #1. It touches `HitWalk` and the draw walk, so it comes before the text field that scrolls its text.
4. **text-field** — `TextField` with a fixed UTF-8 buffer, a caret, a selection, key and char handling, and a caret draw — It depends on the focus route from input-routing and the clip from #3.
5. **widget-sample** — A sample screen with each widget, the stylesheet for each pseudo-state, the smoke run, and the allocation gate for the full set — It depends on all four widgets.

## Dependencies

- input-routing: the event slots, `Hover` and `Active`, the hit test, the focus owner, Tab, and the key and char route.
- style-system: the pseudo-state bits in `Element.States` and the restyle on a state change.
- imgui-renderer: `IUiRenderer` for the rect, the text, the clip, and the caret.
- core-tree: `Element`, the type id, and the dirty flags.
- dev-ops: the test scripts and the GPU smoke contract.

## Out of Scope

- `ListView` and `DataList`. They belong to the windows-and-lists epic.
- `WindowChrome` and the window manager.
- Commands, stores, and bindings. The framework epic connects a widget to them.
- The action map and gamepad navigation.
- The XML loader and the attribute table.
- IME composition, clipboard access, and undo in `TextField` (A6).
- Multi-line text editing (A5).
- A scrollbar track and thumb on `ScrollView`. It comes with `ListView` in a later epic (Q4).
- A vertical `Slider` (A8).

---

*Consumed by `/plan-epic interactive-elements`. After planning, run `/plan-feature interactive-elements/<feature-name>` per feature in the recommended order.*
