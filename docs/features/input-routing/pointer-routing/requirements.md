# Pointer Routing - Requirements

**Epic:** input-routing
**Created:** 2026-10-22

## Overview

This feature adds the `BrutalUi.Glfw` package, the hit test, and the pointer states. It converts the `GlfwWindow` pointer events into the event structs of event-model and routes them to the element under the pointer. It is the second feature of the input-routing epic and the first one that a hover rule makes visible.

## Requirements

- A new `BrutalUi.Glfw` project and pack references `Ahwoo.Brutal.Glfw` and `BrutalUi.Core`. `BrutalUi.Core` gets no GLFW reference.
- The adapter subscribes to the cursor position, mouse button, scroll, drop, and content-scale events of `GlfwWindow` and pushes them into the queue of `UiContext`.
- Drop and content-scale events go to `UiContext` callbacks with no per-element routing.
- The hit test walks the roots from the top-most down and the children from the last to the first. It honors `IsPickable` and the clip rects of the layout. It uses the axis-aligned layout rects only.
- A pointer move sets `PseudoStates.Hover` on the element under the pointer and clears it on the last one. A press sets `PseudoStates.Active` on the target and a release clears it.
- A pointer event walks capture, target, and bubble through event-model. Scroll goes to the element under the pointer.
- `UiContext` stores the last hit result for routing-gate.
- The hit test on a warm 1000-element tree allocates 0 bytes.

## Dependencies

- event-model must be complete: the event structs, the queue, and the walk.
- style-system: `Element.States` and the pseudo-state rules.
- `Ahwoo.Brutal.Glfw` 2026.10.4 from `Directory.Packages.props`.

## Out of Scope

- Keyboard and char events. keyboard-focus adds them.
- Pointer capture across elements, such as a drag that leaves the element. The interactive-elements epic adds it with `ScrollView`.
- Transforms, custom hit shapes, and world-space quads.
