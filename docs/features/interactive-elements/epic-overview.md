# Interactive Elements - Epic Overview

**Epic:** interactive-elements
**Status:** Planned
**Last Updated:** 2026-10-29 09:00

> **This file is the epic marker.** Its presence in `docs/features/interactive-elements/` tells every Beast Mode command that this folder is an **epic**, not a plain feature. Keep it present and keep the required sections below filled in. The master `docs/overview.md` tracks this epic as a **single row**. The per-feature detail lives **here**.

---

## Purpose

This epic delivers the built-in widgets of `BrutalUi.Core`: `Button`, `Toggle`, `Slider`, `ScrollView`, and `TextField`. Each widget is a sealed `Element` subclass with its own type id. It registers handlers on the event slots of input-routing at build time, sets a pseudo-state bit for the stylesheet, and reports a change through a delegate slot. The epic closes Phase 2. It gives the framework epic the widgets that commands, bindings, and the action map need.

---

## Features

| # | Feature | Status | Tasks | Description |
|---|---------|--------|-------|-------------|
| 1 | button-and-toggle | Complete | 34/34 (100%) | `Button` and `Toggle` with click, keyboard activation, the `Checked` bit, and the delegate slot. Sets the widget pattern. |
| 2 | slider | Complete | 41/41 (100%) | `Slider` with a float value, a range, a step, drag, and the arrow keys. Horizontal only. |
| 3 | scroll-view | Complete | 29/29 (100%) | `ScrollView` with a scroll offset in the hit test and the draw walk, clip, scroll events, and drag. No scrollbar. |
| 4 | text-field | Complete | 38/38 (100%) | `TextField` with a fixed UTF-8 buffer, a caret, a selection, key and char handling, and a caret draw. Single line. |
| 5 | widget-sample | Complete | 41/41 (100%) | Each widget in HelloPanel, the stylesheet rules, the smoke run, and the allocation gate with each widget. |

> Reference any feature with the slash form `interactive-elements/<feature-name>`. `/update-epic` refreshes the task counts from each `tasks.md`.

---

## Build Order / Dependencies

1. **button-and-toggle** — It sets the widget pattern: the sealed subclass, the type id, the build-time handlers, the pseudo-state bit, and the delegate slot. Each later feature copies it.
2. **slider** — It depends on the pattern from #1 and adds the first drag rule and the first continuous value.
3. **scroll-view** — It depends on the pattern from #1. It changes `HitWalk` and the draw walk, so it comes before the text field that clips its text.
4. **text-field** — It depends on the focus route from input-routing, the pattern from #1, and the clip rule from #3.
5. **widget-sample** — It depends on all four widgets.

**Dependencies between features:**
- button-and-toggle → slider, scroll-view, text-field (the widget pattern and the delegate slot shape)
- scroll-view → text-field (the clip of content that is wider than the element)
- slider and scroll-view have no dependency on each other. They can be built in parallel after #1.
- External: input-routing (slots, `Hover`, `Active`, hit test, focus owner, key and char route), style-system (`PseudoStates`, restyle on a state change), imgui-renderer (`IUiRenderer`), core-tree (`Element`, type id, dirty flags), dev-ops (test scripts, GPU smoke contract).

**Confirmed interview items:**
- A1 Build order: button-and-toggle, slider, scroll-view, text-field, widget-sample. Feature 1 sets the widget pattern.

---

## Integration & Architecture

- **Within the epic:** Each widget lives in `src/BrutalUi.Core/Elements/`. Each one is a sealed `Element` subclass with its own `ElementType` id. Each one registers its handlers one time in its constructor, as a static method plus a target object. A change goes to a delegate slot that the game sets at build time. No widget allocates in `Update` or `Draw` after warm-up.
- **With other epics / features:** The widgets read the event slots, `Hover`, `Active`, and the focus owner from input-routing. `Toggle` sets `PseudoStates.Checked`, and the style pass restyles it. scroll-view adds a scroll offset that `HitWalk` and the draw walk apply. The framework epic connects a widget to a command, a store, or the action map. The markup epic registers each widget type by name.
- **Key architectural decisions for the epic as a whole:** A2 Each widget is a sealed `Element` subclass in `src/BrutalUi.Core/Elements/`, with its own `ElementType` id. A3 `Toggle` sets the existing `PseudoStates.Checked` bit, and the epic adds no new pseudo-state. Q3 A widget reports a change through a delegate slot, set at build time. A7 scroll-view adds the scroll offset to `HitWalk` and to the draw walk, and it does not change the layout engine.
- **Patterns from the build:** `Slider`, `ScrollView`, and `TextField` take the moves of a drag through the internal `IDragCapture` interface. `WidgetInput` holds the shared input checks of each widget. `TextField` adds one step to `UiContext.Update`: the caret pass after the layout pass. It maps a pending press with the measurer, scrolls the text to the caret, and blinks the caret. `ThousandWidgetAllocationTests` puts each widget into the 1000-element gate.

---

## Tech Debt / Findings

- (none yet - `/review-epic` adds cross-feature findings here)

---

## Master Overview Rollup

- **Rollup status:** Complete (5/5 features, 183/183 tasks)
- **One-line summary for master:** `Button`, `Toggle`, `Slider`, `ScrollView`, and `TextField` on the event slots and the focus owner, with a delegate slot for each change. HelloPanel shows each widget, and the smoke run drives each one. 2558 tests pass. Closes Phase 2.

---

*This is a required file. Do not delete it. It marks the folder as an epic. Update it with `/update-epic interactive-elements` after work on the features, and run `/review-epic interactive-elements` to refresh the Tech Debt / Findings section.*
