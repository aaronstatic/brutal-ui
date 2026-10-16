# Element Tree - Requirements

**Epic:** core-tree
**Created:** 2026-10-16

## Overview

This feature delivers the `Element` class, its child and class lists, its dirty flags, and a minimal `UiContext` that owns the roots. It is the first feature of the core-tree epic. The flexbox-layout feature stores its `LayoutNode` on this element, and the allocation-guard feature calls `UiContext.Update` on a tree of these elements.

## Requirements

- An `Element` holds a name, a class list, a parent, a child list, pseudo-states, a picking flag, and inline `ComputedStyle` and `LayoutNode` structs exposed through `ref`.
- The child list is a pooled list. Add, insert, remove, and reorder allocate nothing after warm-up.
- The class list is a small inline set. Add and remove allocate nothing. A class is an interned id, not a string, in the hot path.
- Pseudo-states are a flags enum: Hover, Active, Focus, Disabled, Checked.
- Each element carries dirty flags for style, layout, and paint. A change marks the element and its ancestors. A walk visits dirty subtrees only.
- `UiContext` owns a root list and exposes `Update(float deltaTime)`. `Update` runs the dirty-subtree walk. In this feature the walk clears the flags. The flexbox-layout feature adds the layout pass to it (Q2).
- `ComputedStyle` holds the layout fields only. The style-system epic extends it (A2).
- Unit tests cover add, remove, reorder, reparent, the dirty mark up to the root, and the walk that skips clean subtrees.
- Public members carry XML doc comments. Code uses file-scoped namespaces and `Brutal.Numerics` types.

## Dependencies

- The dev-ops epic. The `BrutalUi.Core` and `BrutalUi.Core.Tests` projects exist and build.
- No sibling feature. This feature comes first and runs alone.

## Out of Scope

- The struct event with capture, target, and bubble phases. The input epic owns it (Q1).
- Hit testing, focus, and any GLFW subscription.
- The layout algorithm and the `LayoutNode` fields. The flexbox-layout feature owns them. This feature reserves the slot.
- The style cascade, selectors, and paint fields in `ComputedStyle`.
- `Draw`, `IUiRenderer`, and the built-in element types beyond the base `Element`.
