# Core Tree - Epic Requirements

**Created:** 2026-10-16
**Phase:** Phase 1 (skeleton and tree)

> Epic-level requirements for the whole epic. Each child feature has its own `requirements.md` that `/plan-feature core-tree/<feature-name>` consumes.

## Overview

This epic delivers the retained element tree and the flexbox layout engine in `BrutalUi.Core`. It owns the `Element` class, the child list, the class list, the dirty flags, the `LayoutNode` struct, and the layout pass. The features belong together because every later layer, from the style pass to the renderer to the framework, reads and writes this one tree.

## Requirements

- An `Element` holds a name, a class list, a parent, a child list, pseudo-states, a picking flag, and inline `ComputedStyle` and `LayoutNode` structs.
- A game adds, removes, and reorders children with no managed allocation after warm-up. The child list and the class list use pooled or inline storage.
- Each element carries dirty flags for style, layout, and paint. A change marks the element and its ancestors. A pass visits dirty subtrees only.
- The layout engine implements the Yoga subset from the technical design: display, position, flex direction, wrap, justify, align, grow, shrink, basis, size, min and max size, margin, padding, border width, offsets, and overflow. Length units are pixels, percent, and auto.
- The layout engine runs on `ref LayoutNode` and writes a `float4` rect for each element. It allocates nothing after warm-up.
- A measure callback slot on an element returns an intrinsic size. `Label` and `Image` use it later. The epic ships the slot, not a caller (A8).
- A minimal `UiContext` owns the roots and runs `Update(deltaTime)`, which runs the layout pass on dirty subtrees (Q2).
- Unit tests on the layout engine use fixtures from the Yoga test suite, ported by hand.
- An allocation test builds a warm 1000-element tree, runs the layout pass, and asserts zero bytes allocated. It is an xUnit test that reads `GC.GetAllocatedBytesForCurrentThread()` and runs under `dotnet test` in both scripts with no new step (A5).
- A benchmark in `BrutalUi.Benchmarks` measures the layout pass on the same tree. It uses the `BenchmarkDotNet` 0.15.8 pin and runs on demand, not in the test scripts (A6).
- All code uses `Brutal.Numerics` types, file-scoped namespaces, nullable on, and XML doc comments on public members.
- `BrutalUi.Core` references `Ahwoo.BRUTAL.Numerics` and `Ahwoo.BRUTAL.Localization` only. No ImGui reference. No reference change in this epic (A7).
- `ComputedStyle` holds the layout fields only in this epic. The style-system epic extends it (A2).
- Length units are pixels, percent, and auto. No em, rem, or viewport units (A3).

## Planned Features

1. **element-tree**. The `Element` class, the pooled child list, the inline class list, pseudo-states, the dirty flags, and a minimal `UiContext` with the roots and `Update`. It comes first because every other feature stores data on it (Q2, Q3).
2. **flexbox-layout**. The `LayoutNode` struct, the layout properties, the Yoga algorithm subset, the measure callback slot, and the hand-ported Yoga fixtures. It depends on feature 1.
3. **allocation-guard**. The 1000-element allocation test and the BenchmarkDotNet layout benchmark. It depends on feature 2 because it measures the layout pass (Q4).

## Dependencies

- The `dev-ops` epic. The `BrutalUi.Core` project, the `BrutalUi.Core.Tests` project, the `BrutalUi.Benchmarks` project, and both test scripts exist.
- The `Ahwoo.Brutal.Numerics` and `Ahwoo.Brutal.Localization` packages, pinned in `Directory.Packages.props`.
- The Yoga test suite as a reference for the layout fixtures. The agent ports fixtures by hand. No Yoga binary and no Yoga package (A4).

## Out of Scope

- The `IUiRenderer` interface and the ImGui renderer. A later `imgui-renderer` epic owns them.
- The style cascade, the stylesheet parser, selectors, and transitions. A later `style-system` epic owns them. This epic defines the `ComputedStyle` slot on the element and the layout fields in it only.
- Text measurement and font handling. The measure callback is a hook. The renderer epic plugs text into it.
- The struct event with capture, target, and bubble phases. The input epic owns it (Q1).
- Pointer and keyboard routing, hit testing, and focus. No GLFW subscription.
- The built-in element types beyond the base `Element`. `Label`, `Image`, `Button`, and the others come with the renderer and style epics.
- Grid layout. Flexbox and absolute position only.

---

*Consumed by `/plan-epic core-tree`. After planning, run `/plan-feature core-tree/<feature-name>` per feature in the recommended order.*
