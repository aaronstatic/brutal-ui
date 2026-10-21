# Stylesheet Model - Requirements

**Epic:** style-system
**Created:** 2026-10-21

## Overview

This feature gives the data model of the style system and the style pass that writes `ComputedStyle`. It adds `StyleProperties`, `StyleRule`, `StyleSheet`, the inline set on the element, and the text members with inheritance. It is the first feature of the style-system epic, and every later feature fills or reads its model.

## Requirements

- `StyleProperties` is a sparse set of property values with a set mask. It covers each layout member of `ComputedStyle`, the paint members, and the text members.
- `StyleRule` holds a `Selector`, a `StyleProperties` set, and a specificity. `StyleSheet` is a sorted list of rules.
- A selector of this feature matches a class, a type, or a class on a type, at depth 1. Names, pseudo-states, and the depth-2 chain come in selector-matching, and the model leaves a slot for them.
- A type selector matches a type id that each element class declares. The match path uses no reflection.
- `UiContext` holds zero or more sheets. The style pass runs at the seam in `Update`, before layout. It visits only elements with the `Style` or `DescendantStyle` bit, in tree order.
- The pass merges the matched rules by specificity, then by rule order, and writes `ComputedStyle`. An unset property gets its default.
- Each element holds a sparse inline `StyleProperties` set and a mask. The cascade skips masked properties, so the inline value wins.
- `Color`, `Font`, and `FontSize` move into `ComputedStyle`. They inherit from the parent when no rule and no inline value sets them. `Label` reads them. The old `Label.Color`, `Label.Font`, and `Label.Size` go.
- A cascade write that changes a layout property sets `Layout`. A write that changes a paint or text member sets `Paint`. A text member change also sets `Layout` on a `Label`. An unchanged value sets no bit.
- `Update` with an active sheet allocates no managed memory after warm-up. `ThousandElementAllocationTests` covers the pass.
- Unit tests cover the merge order, the inline mask, inheritance, the dirty bits, and the type match.

## Dependencies

- core-tree (epic): `Element`, `ClassList`, `DirtyFlags`, `ComputedStyle`, `DirtyWalk`, and `UiContext.Update`.
- imgui-renderer (epic): the paint members of `ComputedStyle` and `Label`.
- No sibling feature. This feature comes first.

## Out of Scope

- Name and pseudo-state selectors, and the depth-2 parent chain. See selector-matching.
- The text format and the parser. See style-parser.
- Transitions. See transitions.
- Dirty propagation on a class or state change beyond what core-tree does today. See selector-matching.
