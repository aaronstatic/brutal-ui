# Core Tree - Epic Overview

**Epic:** core-tree
**Status:** Planned
**Last Updated:** 2026-10-16 19:10

> **This file is the epic marker.** Its presence in `docs/features/core-tree/` tells every Beast Mode command that this folder is an **epic**, not a plain feature. Keep it present and keep the required sections below filled in. The master `docs/overview.md` tracks this epic as a **single row**. The per-feature detail lives **here**.

---

## Purpose

This epic delivers the retained element tree, the flexbox layout engine, and a minimal `UiContext` in `BrutalUi.Core`. It owns the `Element` class, the pooled child list, the inline class list, the dirty flags, the `LayoutNode` struct, the layout pass, and the allocation test that guards the frame loop. The features belong together because every later layer, from the style pass to the renderer to the framework, reads and writes this one tree.

The epic covers the "tree" half of Phase 1 in the technical design. The dev-ops epic covers the "skeleton" half. The imgui-renderer epic completes Phase 1 with pixels on screen.

---

## Features

The constituent features of this epic, in build order. Each feature is a subfolder under `docs/features/core-tree/`.

| # | Feature | Status | Tasks | Description |
|---|---------|--------|-------|-------------|
| 1 | element-tree | Planned | — | The `Element` class, the pooled child list, the inline class list, pseudo-states, the dirty flags, and a minimal `UiContext` with the roots and `Update`. |
| 2 | flexbox-layout | Planned | — | The `LayoutNode` struct, the layout properties, the Yoga algorithm subset, the measure callback slot, and the hand-ported Yoga fixtures. |
| 3 | allocation-guard | Planned | — | The 1000-element allocation test under `dotnet test` and the BenchmarkDotNet layout benchmark. |

> Reference any feature with the slash form `core-tree/<feature-name>`. Task counts come from each feature's own `tasks.md` and `/update-epic` refreshes them.

---

## Build Order / Dependencies

1. **element-tree**. Every other feature stores data on the element and runs through the `UiContext`. It comes first. Dirty tracking lives here, because the flags sit on the element and the code is about 200 lines (Q3).
2. **flexbox-layout**. The layout pass reads the tree, the dirty flags, and the `LayoutNode` slot from feature 1. It is the hardest part of the epic and the largest, about 2000 lines.
3. **allocation-guard**. The test and the benchmark measure the layout pass from feature 2 on a 1000-element tree. It comes last so it measures the complete pass (Q4).

**Dependencies between features:**
- element-tree → flexbox-layout (the `Element`, the dirty flags, the `LayoutNode` slot, and `UiContext.Update`)
- flexbox-layout → allocation-guard (the layout pass to measure)
- The three features build in series (A1). No pair runs in parallel.
- External: the dev-ops epic. The `BrutalUi.Core`, `BrutalUi.Core.Tests`, and `BrutalUi.Benchmarks` projects and both test scripts exist.
- External: the Yoga repository on GitHub as the source of the layout fixtures. No Yoga package and no Yoga binary (A4).

**Confirmed interview items that shape the build order:**
- A1 The feature set is element-tree, flexbox-layout, allocation-guard, in that build order, in series.
- Q3 dirty-tracking merges into element-tree.
- Q4 allocation-guard stays a separate last feature.

---

## Integration & Architecture

- **Within the epic:** `Element` holds inline `ComputedStyle` and `LayoutNode` structs. The layout engine reads them through `ref`. `UiContext` owns the root list and runs `Update(deltaTime)`, which walks dirty subtrees only. The allocation test and the benchmark call `UiContext.Update` on a warm tree and nothing else.
- **With other epics / features:** The imgui-renderer epic adds `IUiRenderer`, `UiContext.Draw`, `Label`, and `Image`, and plugs text measure into the measure callback slot. The style-system epic extends `ComputedStyle` with paint fields and writes it from the cascade. The input epic adds the struct event, hit testing, and focus on the element (Q1).
- **Key architectural decisions for the epic as a whole:**
  - `BrutalUi.Core` references `Ahwoo.BRUTAL.Numerics` and `Ahwoo.BRUTAL.Localization` only. No ImGui reference. No reference change in this epic (A7).
  - A C# flexbox engine that follows the Yoga algorithm structure. No native layout library.
  - No managed allocation in `UiContext.Update` after warm-up. The child list and the class list use pooled or inline storage.
  - `ComputedStyle` holds the layout fields only in this epic (A2). Length units are pixels, percent, and auto (A3).
  - The measure callback is a delegate slot on the element with no caller in this epic (A8).

---

## Tech Debt / Findings

- (none yet: `/review-epic` will surface cross-feature integration issues and per-feature tech debt here)

---

## Master Overview Rollup

- **Rollup status:** Planned (0/3 features)
- **One-line summary for master:** The retained element tree, the C# flexbox engine, and the zero-allocation test on a 1000-element tree.

---

*This is a required file. Do not delete it. It marks the folder as an epic. Update it with `/update-epic core-tree` after work on the features, and run `/review-epic core-tree` to refresh the Tech Debt / Findings section.*
