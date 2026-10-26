# Button And Toggle — Requirements

**Epic:** interactive-elements
**Created:** 2026-10-23

## Overview

This feature delivers `Button` and `Toggle`, the first two widgets of `BrutalUi.Core`. It sets the widget pattern that each later feature of the interactive-elements epic copies: a sealed `Element` subclass with its own type id, build-time handlers on the event slots, a pseudo-state bit for the stylesheet, and a delegate slot for a change.

## Requirements

- `Button` and `Toggle` live in `src/BrutalUi.Core/Elements/` and reference no ImGui package.
- Each class is sealed, has its own `ElementType` id, and is focusable by default.
- Each class registers its handlers one time in its constructor, as a static method plus a target object (A2).
- `Button` raises a click on a pointer up over the element while it holds `Active`, and on Enter or Space when it owns focus (A4).
- `Toggle` holds a `Checked` state. It sets and clears `PseudoStates.Checked`, so the style pass restyles it (A3).
- `Toggle` flips on click and on Space when it owns focus (A4).
- A change goes to a delegate slot that the game sets at build time, for example `button.Clicked` and `toggle.CheckedChanged`. The call allocates nothing (Q3).
- A disabled widget raises nothing. The `Disabled` bit blocks the click and the flip.
- A `Button` holds a label through `SetText`, with the same overloads and the same compare rules as `Label`.
- `Update` and `Draw` allocate 0 bytes after warm-up with the widgets in the tree.
- Tests cover the click, the keyboard activation, the disabled case, the `Checked` bit, and the allocation.

## Dependencies

- input-routing: the event slots, `Hover` and `Active`, the focus owner, and the key route.
- style-system: `PseudoStates.Checked` and `PseudoStates.Disabled`.
- It comes first in the epic. No sibling feature must exist before it.

## Out of Scope

- A command binding on a button. The framework epic adds it.
- A radio group or an exclusive toggle set.
- An icon on a button. Compose an `Image` child.
- `Slider`, `ScrollView`, and `TextField`. Each one is its own feature.
