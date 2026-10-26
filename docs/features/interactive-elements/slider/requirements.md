# Slider — Requirements

**Epic:** interactive-elements
**Created:** 2026-10-23

## Overview

This feature delivers `Slider`, a horizontal widget that holds a float value in a range. It adds the first drag rule and the first continuous value to the widget set. It copies the widget pattern of button-and-toggle.

## Requirements

- `Slider` lives in `src/BrutalUi.Core/Elements/`, is sealed, has its own `ElementType` id, and is focusable by default.
- It holds `Value`, `Minimum`, `Maximum`, and `Step`. A set clamps the value to the range and rounds it to the step. A step of 0 means no rounding.
- A pointer down on the track moves the value to the pointer position. A pointer move while the slider holds `Active` drags the value.
- The left and right arrow keys move the value by one step, or by one percent of the range when the step is 0, while the slider owns focus (A4).
- A change goes to a delegate slot, for example `slider.ValueChanged`, set at build time. A set to the same value raises nothing (Q3).
- The slider draws a track and a thumb from `ComputedStyle` through `IUiRenderer`. The stylesheet styles the thumb through a pseudo-state or a child class.
- A disabled slider ignores the pointer and the keys.
- `Update` and `Draw` allocate 0 bytes after warm-up with sliders in the tree.
- Tests cover the clamp, the step, the drag, the arrow keys, the disabled case, and the allocation.

## Dependencies

- button-and-toggle: the widget pattern and the delegate slot shape.
- input-routing: the pointer events, `Active`, the focus owner, and the key route.
- It can be built in parallel with scroll-view after button-and-toggle.

## Out of Scope

- A vertical slider (A8).
- A range slider with two thumbs.
- A value label. Compose a `Label` and read the delegate slot.
- A binding to a store. The framework epic adds it.
