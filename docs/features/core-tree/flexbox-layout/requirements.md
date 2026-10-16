# Flexbox Layout - Requirements

**Epic:** core-tree
**Created:** 2026-10-16

## Overview

This feature delivers the C# flexbox engine of `BrutalUi.Core`. It owns the `LayoutNode` struct, the layout properties, the layout pass inside `UiContext.Update`, and the measure callback slot. It is the second feature of the core-tree epic and its hardest part. It follows the Yoga algorithm structure. Yoga is a pattern and a test source here, not a dependency.

## Requirements

- `LayoutNode` holds the input properties and the output rect as a struct inline on the element. The engine reads and writes it through `ref`.
- Supported properties: `display`, `position` (relative, absolute), `flex-direction`, `flex-wrap`, `justify-content`, `align-items`, `align-self`, `align-content`, `flex-grow`, `flex-shrink`, `flex-basis`, `width`, `height`, `min-width`, `min-height`, `max-width`, `max-height`, `margin`, `padding`, `border-width`, `left`, `top`, `right`, `bottom`, `overflow`.
- Length units are pixels, percent, and auto. No em, rem, or viewport units (A3).
- The algorithm follows the Yoga structure: measure, main-axis line break, main-axis distribution, cross-axis alignment, absolute children.
- The layout pass runs inside `UiContext.Update` on dirty subtrees only and writes a `float4` rect for each element.
- The pass allocates nothing after warm-up. No closures, no arrays, no boxing in the pass.
- A measure callback slot on the element returns an intrinsic size. This feature ships the slot and a test stub. The imgui-renderer epic plugs text measure into it (A8).
- Unit tests port fixtures from the Yoga generated test suite by hand, one xUnit test per fixture. The port covers each supported property at least one time (A4).
- A direct port of Yoga source keeps the Yoga MIT notice in the repository. The plan decides how close the port stays.

## Dependencies

- `core-tree/element-tree` must be complete. This feature needs the `Element`, the dirty flags, the `LayoutNode` slot, and `UiContext.Update`.
- The Yoga repository on GitHub as the fixture source. No Yoga package and no Yoga binary.

## Out of Scope

- Grid layout, `gap`, `aspect-ratio`, and `direction` (right to left).
- Text measure and image measure. The slot exists, the callers come later.
- Scroll offsets. `overflow` clips in the renderer epic, and `ScrollView` comes in the style-and-input phase.
- The allocation test and the benchmark. The allocation-guard feature owns them.
