# Scroll View — Requirements

**Epic:** interactive-elements
**Created:** 2026-10-23

## Overview

This feature delivers `ScrollView`, a container that holds a scroll offset and clips its content. It adds the offset to the hit test and the draw walk. Each later feature and the `ListView` of a later epic then scroll content. It does not change the layout engine.

## Requirements

- `ScrollView` lives in `src/BrutalUi.Core/Elements/`, is sealed, and has its own `ElementType` id. It accepts children.
- It holds `ScrollOffset` as a `float2`. A set clamps the offset to the content size minus the viewport size.
- A scroll event on the view or on a child moves the offset by the delta times a speed. A drag with the primary button moves the offset.
- `HitWalk` applies the offset to the children. A hit then lands on the element the user sees (A7).
- The draw walk applies the offset to the children and clips them to the padding box (A7).
- The layout engine stays as it is. The content size comes from the layout rects of the children (A7).
- A change goes to a delegate slot, for example `scrollView.ScrollChanged`, set at build time (Q3).
- `Update` and `Draw` allocate 0 bytes after warm-up with scroll views in the tree.
- Tests cover the clamp, the scroll event, and the drag.
- Tests cover the hit test with an offset, the draw with an offset, and the allocation.

## Dependencies

- button-and-toggle: the widget pattern and the delegate slot shape.
- input-routing: the scroll event, the pointer events, `Active`, and `HitWalk`.
- imgui-renderer: `PushClip` and `PopClip` in the draw walk.
- It can be built in parallel with slider after button-and-toggle.

## Out of Scope

- A scrollbar track and thumb. It comes with `ListView` in a later epic (Q4).
- Kinetic scrolling, smooth scrolling, and a scroll animation.
- Virtualization of the children. `ListView` adds it.
- A horizontal-only or a vertical-only mode. Both axes clamp by the content size.
