# Text Field — Requirements

**Epic:** interactive-elements
**Created:** 2026-10-23

## Overview

This feature delivers `TextField`, a single-line text editor on a fixed UTF-8 buffer. It uses the focus route of input-routing for the key and char events, and the clip rule of scroll-view for text wider than the field. It is the largest widget of the epic.

## Requirements

- `TextField` lives in `src/BrutalUi.Core/Elements/`, is sealed, has its own `ElementType` id, and is focusable by default.
- It holds the text in a fixed UTF-8 buffer. The capacity is a create-info value. The buffer never grows during typing (A5).
- An insert past the capacity drops the input and raises nothing.
- A char event inserts a code point at the caret. The key events move the caret with the arrow keys, Home, and End. Backspace and Delete remove a code point.
- Shift with a caret move extends a selection. An insert replaces the selection.
- The field draws the text, the caret, and the selection through `IUiRenderer`. The caret blinks on a timer that `Update` advances.
- The field clips the text to its padding box and scrolls the text so the caret stays visible.
- A change goes to a delegate slot, for example `textField.TextChanged`, set at build time (Q3). A submit on Enter goes to a second slot.
- `GetText(Span<byte>)` and `SetText` with the `Label` overloads read and write the buffer with no allocation.
- `Update` and `Draw` allocate 0 bytes after warm-up with text fields in the tree.
- Tests cover the insert, the delete, the caret moves, the selection, the capacity limit, the submit, and the allocation.

## Dependencies

- button-and-toggle: the widget pattern and the delegate slot shape.
- scroll-view: the clip of content that is wider than the element.
- input-routing: the focus owner, the key route, and the char route.
- imgui-renderer: `MeasureText` for the caret position.

## Out of Scope

- IME composition, clipboard access, and undo (A6).
- Multi-line text editing (A5).
- A placeholder text and a password mask.
- Input validation and a numeric mode.
