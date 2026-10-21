# Style System - Epic Requirements

**Created:** 2026-10-21
**Phase:** Phase 2 (style and input)

> Epic-level requirements for the whole epic. `/plan-epic style-system` reads this file to scope the epic. Each child feature gets its own `requirements.md` from `/plan-epic`.

## Overview

The style-system epic owns the path from a stylesheet to `ComputedStyle`. It adds the rule model, the selector match, the text format, and the per-frame transitions. These features belong together because each one feeds the next: a rule needs a selector, a parser writes rules, and a transition interpolates the values that the cascade writes. After this epic, code sets classes and pseudo-states instead of inline style values.

## Requirements

- A `StyleSheet` is a sorted list of `StyleRule`. Each rule has a `Selector`, a sparse `StyleProperties` set, and a specificity.
- A selector matches on class, name, element type, and pseudo-state. It supports a parent-child chain of depth 2 (`.a > .b` and `.a .b`). The depth is a constant.
- The pseudo-states are `:hover`, `:active`, `:focus`, `:disabled`, and `:checked`. The epic adds the matching rules and reads `Element.PseudoStates`. It does not set the states from input.
- The style pass runs inside `UiContext.Update`, before layout. It visits only dirty elements, merges the matched rules by specificity, and writes `ComputedStyle`. Inline style on an element wins over every rule.
- Each element holds a sparse inline `StyleProperties` set and a per-property mask. The cascade skips masked properties, so an inline value wins (Q1).
- `Color`, `Font`, and `FontSize` move into `ComputedStyle` and inherit down the tree. `Label` reads them (Q2).
- A type selector matches a type id that each element class declares. The match path uses no reflection (A3).
- A class or pseudo-state change marks the element and its subtree style-dirty, with no check of the sheet (A2).
- A style write sets the correct dirty bits. A layout property marks `Layout`. A paint property (`BackgroundColor`, `BorderColor`, `CornerRadius`, `Opacity`) marks `Paint` only.
- A text format describes a stylesheet. Theme variables (`--name`) resolve at parse time, for each stylesheet. The parser reports an error with a line and a column.
- A transition applies to colors, corner radius, and opacity only. A tween marks `Paint` only (Q3).
- A property transition has a duration and an easing. The style pass writes the target value. The transition system interpolates per frame into `ComputedStyle` and marks the right dirty bits.
- `Update` allocates no managed memory after warm-up with an active stylesheet and with running transitions. The allocation gate covers both.
- The HelloPanel sample moves its inline paint values to a stylesheet, so the GPU smoke run shows the cascade.

## Planned Features

1. **stylesheet-model** - `StyleRule`, `StyleProperties`, `StyleSheet`, specificity, and the style pass that writes `ComputedStyle` for a dirty element - It gives the data model and the cascade that every later feature fills or reads.
2. **selector-matching** - Class, name, type, and pseudo-state matching with the depth-2 parent chain, and the dirty propagation when a class or a state changes - It depends on the rule model of #1 and makes the cascade correct on a real tree.
3. **style-parser** - The text format, the tokenizer, the parser, theme variables, and the error report - It depends on #1 and #2 because it writes rules and selectors that already match.
4. **transitions** - The transition pool, easing, per-frame interpolation into `ComputedStyle`, and the dirty bits per property - It comes last because it interpolates the values that #1 writes, and it uses the format of #3 to declare a transition.

## Dependencies

- The parser lives in `BrutalUi.Markup` with its tests in `BrutalUi.Markup.Tests`. The model, the matcher, the pass, and the transitions live in `BrutalUi.Core` (A1).

- core-tree (epic): `Element`, `ClassList`, `PseudoStates`, `DirtyFlags`, `ComputedStyle`, and `UiContext.Update`.
- imgui-renderer (epic): the paint members of `ComputedStyle` and the draw walk that reads them.
- `ThousandElementAllocationTests` and `AllocationGate` for the zero-byte checks.

## Out of Scope

- Setting `:hover`, `:active`, and `:focus` from pointer or keyboard events. The input-routing epic does that.
- Selectors beyond class, name, type, and pseudo-state, and a chain deeper than 2.
- Runtime theme variables, a cascade across stylesheets, and stylesheet hot reload.
- An XML element loader. The markup epic of Phase 3 does that.
- Per-side border widths and an exact path for mixed corner radii.
- Transitions on layout lengths and on font size (Q3).

---

*Consumed by `/plan-epic style-system`. After planning, run `/plan-feature style-system/<feature-name>` for each feature in the build order.*
