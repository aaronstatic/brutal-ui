# Input Routing - Epic Overview

**Epic:** input-routing
**Status:** Complete
**Last Updated:** 2026-10-22 16:30

> **This file is the epic marker.** Its presence in `docs/features/input-routing/` tells every Beast Mode command that this folder is an **epic**, not a plain feature. Keep it present and keep the required sections below filled in. The master `docs/overview.md` tracks this epic as a **single row**. The per-feature detail lives **here**.

---

## Purpose

This epic routes pointer and keyboard events to the element tree. It owns the event model in `BrutalUi.Core`, the hit test, the capture, target, and bubble walk, the focus owner, and the gate that tells the game when the UI owns the pointer or the keyboard. A new `BrutalUi.Glfw` package converts the `GlfwWindow` events into the plain structs that Core accepts, so Core gets no GLFW reference.

These features belong together because each one feeds the next. An event needs a model and a walk. A pointer event needs a hit test. A keyboard event needs a focus owner. The gate reads the result of both. The epic closes the input half of Phase 2 and gives the interactive-elements epic the events that `Button`, `Toggle`, `ScrollView`, and `TextField` need.

---

## Features

The constituent features of this epic, in build order. Each feature is a subfolder under `docs/features/input-routing/`.

| # | Feature | Status | Tasks | Description |
|---|---------|--------|-------|-------------|
| 1 | event-model | Complete | 43/43 (100%) | The event structs, the handler delegates on `Element`, the event queue, and the capture, target, and bubble walk with stop propagation. |
| 2 | pointer-routing | Complete | 44/44 (100%) | The `BrutalUi.Glfw` adapter, the hit test with clip rects and `IsPickable`, the `Hover` and `Active` states, and the scroll event. |
| 3 | keyboard-focus | Complete | 42/42 (100%) | The focus owner, focus on click, Tab, and Shift+Tab, the key and char events to the focused element, and the `Focus` state. |
| 4 | routing-gate | Complete | 23/23 (100%) | `UiRoutingGate`, the input pass in `UiContext.Update`, the 0-byte gate on the input pass, and the HelloPanel demo. |

> Reference any feature with the slash form `input-routing/<feature-name>`. Task counts come from each feature's own `tasks.md`, and `/update-epic` refreshes them.

---

## Build Order / Dependencies

1. **event-model** - It comes first because every later feature dispatches through the walk. It has no GLFW code, so a unit test drives it with hand-made events.
2. **pointer-routing** - It depends on the walk and the queue of #1. It adds the `BrutalUi.Glfw` project and writes `Element.States`. The first hover rule becomes visible here.
3. **keyboard-focus** - It depends on the click of #2 for click focus and on the queue of #1 for the key and char events.
4. **routing-gate** - It reads the hit result of #2 and the focus owner of #3. It adds the allocation gate and the HelloPanel demo, and closes the epic.

**Dependencies between features:**
- event-model → pointer-routing (the event structs, the queue, and the walk)
- event-model → keyboard-focus (the queue and the walk)
- pointer-routing → keyboard-focus (the click target for click focus)
- pointer-routing → routing-gate (the last hit result)
- keyboard-focus → routing-gate (the focus owner)
- No pair builds in parallel. Each feature tests on the one before it.
- External: `Ahwoo.Brutal.Glfw` 2026.10.4, pinned in `Directory.Packages.props`.

**Confirmed interview items that shape the order:**
- Q3 Build order: event-model, pointer-routing, keyboard-focus, routing-gate.
- Q4 Four features. routing-gate closes the epic.

---

## Integration & Architecture

- **Within the epic:** As built, the hit test uses the padding-box clip of `PaintWalk`, not `LayoutNode.ContentInsets`. The adapter writes events into a fixed ring buffer on `UiContext`. The input pass drains the buffer in `UiContext.Update` before the style pass. The pointer walk and the keyboard walk share one dispatch path. The gate reads two fields that the walks write.
- **With other epics / features:** core-tree gives `Element`, `IsPickable`, `UiContext`, and the layout rects. style-system gives `PseudoStates` and `Element.States`, so a state change restyles the subtree. imgui-renderer gives `LayoutNode.ContentInsets` for the hit test. The interactive-elements epic registers handlers on the delegates of this epic. The Phase 3 framework epic adds the action map on top of the keyboard events.
- **Key architectural decisions for the epic as a whole:**
  - Q1 The GLFW adapter lives in a new `BrutalUi.Glfw` package. Core stays free of GLFW.
  - Q2 Focus moves on click, Tab, and Shift+Tab. Gamepad waits for the action map.
  - A1 `BrutalUi.Core` gets no GLFW reference. The input types in Core accept plain structs, and `BrutalUi.Glfw` converts the `GlfwWindow` events.
  - A2 A handler registers as a delegate per event kind on `Element` at build time, not in the frame loop. The walk calls the delegates with a `ref` event struct.
  - A3 The hit test uses the axis-aligned layout rects and the clip rects only. No transforms, no custom shapes.
  - A4 Drop and content-scale events go to `UiContext` callbacks. They get no per-element routing.
  - A5 The adapter queues events in a fixed ring buffer. The input pass drains it in `UiContext.Update` before the style pass.
  - A6 The gate reads the hit result of the last pointer position and the focus owner. The game polls it after `Update`.
  - A7 No existing feature in `docs/features/` belongs in this epic.

---

## Tech Debt / Findings

- (none yet - `/review-epic` adds cross-feature findings here)

---

## Master Overview Rollup

- **Rollup status:** Complete (4/4 features, 152/152 tasks)
- **One-line summary for master:** A zero-allocation event ring, a capture and bubble walk, a hit test, a focus owner with Tab, the `BrutalUi.Glfw` adapter, and `UiRoutingGate`. HelloPanel shows hover, focus, and the gate. 2281 tests pass.

---

*This is a required file. Do not delete it. It marks the folder as an epic. Update it with `/update-epic input-routing` after work on the features, and run `/review-epic input-routing` to refresh the Tech Debt / Findings section.*
