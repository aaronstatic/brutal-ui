# Input Routing - Epic Requirements

**Created:** 2026-10-22
**Phase:** Phase 2, style and input

## Overview

This epic routes GLFW pointer and keyboard events to the element tree in `BrutalUi.Core`. It owns the event model, hit testing, the capture, target, and bubble walk, focus, and the gate that tells the game when the UI owns the pointer or the keyboard. These features belong together because each one feeds the next: an event needs a model, a pointer event needs a hit test, a keyboard event needs a focus owner, and the gate reads the result of both.

## Requirements

- The adapter subscribes to the `GlfwWindow` events for cursor position, mouse button, scroll, key, char, drop, and content scale.
- Drop and content-scale events go to `UiContext` callbacks. They get no per-element routing (A4).
- A pointer event hit-tests the tree from the top-most root down. The test honors `PickingEnabled` and the clip rects of the layout.
- An event walks capture, target, and bubble. A handler can stop propagation.
- `UiInput` sets `Element.States` for `:hover`, `:active`, and `:focus`. The pseudo-state rules of the style system restyle the subtree.
- Focus moves on click, Tab, and Shift+Tab in tree order (Q2). Gamepad navigation has a slot for a later feature.
- `UiRoutingGate.PointerOverUi` and `UiRoutingGate.KeyboardFocusInUi` tell the game to stand down.
- `UiContext.Update` runs the input pass before the style pass.
- The input pass allocates 0 bytes on a warm 1000-element tree. The allocation gate guards it.
- `BrutalUi.Core` keeps no GLFW reference. A new `BrutalUi.Glfw` package converts the `GlfwWindow` events into plain structs and feeds them in (Q1, A1).
- The adapter queues events in a fixed ring buffer. The input pass drains it in `UiContext.Update` before the style pass (A5).
- A handler registers as a delegate per event kind on `Element` at build time. The walk calls it with a `ref` event struct (A2).
- The hit test uses the axis-aligned layout rects and the clip rects only (A3).
- The gate reads the hit result of the last pointer position and the focus owner. The game polls it after `Update` (A6).
- The HelloPanel sample shows a `:hover` rule and the gate state on screen.

## Planned Features

1. **event-model** - The event structs, the handler registration on `Element`, and the capture, target, and bubble walk with stop propagation - It comes first because every later feature dispatches through it.
2. **pointer-routing** - The GLFW adapter, the hit test with clip rects and `PickingEnabled`, the hover and active states, and the scroll event - It depends on the walk of #1 and writes `Element.States`.
3. **keyboard-focus** - The focus owner, focus on click and Tab, the key and char events to the focused element, and the `:focus` state - It depends on the pointer click of #2 for click focus.
4. **routing-gate** - `UiRoutingGate`, the input pass in `UiContext.Update`, the 0-byte gate, and the HelloPanel demo - It reads the results of #2 and #3 and closes the epic.

## Dependencies

- core-tree: `Element`, `UiContext`, `LayoutNode`, and the layout rects.
- style-system: `PseudoStates`, `Element.States`, and the pseudo-state rules.
- imgui-renderer: the layout rects and `LayoutNode.ContentInsets` for the hit test.
- `Ahwoo.Brutal.Glfw`: the `GlfwWindow` events.

## Out of Scope

- `Button`, `Toggle`, `ScrollView`, and `TextField`. The interactive-elements epic adds them on top of this one.
- The action map `UiActionMap`. It calls the command registry, so it belongs to the Phase 3 framework epic.
- Gamepad navigation. This epic leaves a slot for it and adds no binding.
- SDL input. The feasibility doc lists it as an open question.
- World-space pointer events on a quad. The Phase 5 epic adds them.
- Transforms and custom hit shapes. The hit test is axis-aligned (A3).
- Arrow-key and spatial navigation. The Phase 3 action map adds it (Q2).

---

*Consumed by `/plan-epic input-routing`. After planning, run `/plan-feature input-routing/[feature-name]` per feature in the recommended order.*
