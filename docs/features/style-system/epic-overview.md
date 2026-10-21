# Style System - Epic Overview

**Epic:** style-system
**Status:** 🔵 Planned
**Last Updated:** 2026-10-21 09:35

> **This file is the epic marker.** Its presence in `docs/features/style-system/` tells every Beast Mode command that this folder is an **epic**, not a plain feature. Keep it present and keep the required sections filled in. The master `docs/overview.md` tracks this epic as **one row**. The per-feature detail lives **here**.

---

## Purpose

The style-system epic owns the path from a stylesheet to `ComputedStyle`. It adds the rule model, the sparse property set, the selector match, the style pass in `UiContext.Update`, the text format with its parser, and the per-frame transitions. After this epic, code sets classes, names, and pseudo-states instead of inline style values. It also moves the text properties (color, font, font size) into `ComputedStyle`, where they inherit down the tree.

These features belong together because each one feeds the next. A rule needs a selector. A parser writes rules and selectors. A transition interpolates the values that the cascade writes. The epic opens Phase 2 of the technical design and gives the input-routing epic the pseudo-state rules that `:hover` and `:focus` need.

---

## Features

The constituent features of this epic, in build order. Each feature is a subfolder under `docs/features/style-system/`.

| # | Feature | Status | Tasks | Description |
|---|---------|--------|-------|-------------|
| 1 | stylesheet-model | Planned | — | `StyleProperties`, `StyleRule`, `StyleSheet`, the inline set with its mask, the text members of `ComputedStyle` with inheritance, and the style pass with class and type match. |
| 2 | selector-matching | Planned | — | Name and pseudo-state match, the depth-2 parent chain, specificity, and the dirty propagation on a class or state change. |
| 3 | style-parser | Planned | — | The text format, the tokenizer, the parser, theme variables, the error report, and the HelloPanel sample on a stylesheet. |
| 4 | transitions | Planned | — | The transition pool, easing, and the per-frame interpolation of colors, corner radius, and opacity into `ComputedStyle`. |

> Reference a feature with the slash form `style-system/<feature-name>`. Task counts come from each feature's own `tasks.md` and `/update-epic` refreshes them.

---

## Build Order / Dependencies

1. **stylesheet-model** - The hardest part comes first: the zero-allocation cascade over dirty elements. Every later feature fills or reads its model.
2. **selector-matching** - It extends the matcher of #1 with names, pseudo-states, and the depth-2 chain. It needs the rule model and the pass.
3. **style-parser** - It writes rules and selectors that #1 and #2 already match, so a parsed sheet is testable end to end. It moves the HelloPanel sample to a stylesheet.
4. **transitions** - It interpolates the values that #1 writes. It declares a transition through the format of #3, but it can build with a sheet made in code.

**Dependencies between features:**
- stylesheet-model → selector-matching (the rule model, the pass, the specificity slot)
- stylesheet-model, selector-matching → style-parser (the types that the parser writes, the full selector grammar)
- stylesheet-model → transitions (the paint members and the dirty rules)
- transitions can run in parallel with style-parser.
- Outside the epic: the core-tree epic (`Element`, `ClassList`, `PseudoStates`, `DirtyFlags`, `ComputedStyle`, `UiContext.Update`) and the imgui-renderer epic (the paint members and the draw walk).

---

## Integration & Architecture

- **Within the epic:** `BrutalUi.Core` holds the model, the matcher, the style pass, and the transitions. `BrutalUi.Markup` holds the parser and references Core only. The style pass runs at the seam in `UiContext.Update`, before layout. The transition pass runs after the style pass and reads `deltaTime`. A property write from the cascade sets `Layout` for a layout property and `Paint` for a paint property.
- **With other epics / features:** The draw walk of imgui-renderer reads the paint members that the cascade writes. `Label` reads `Color`, `Font`, and `FontSize` from `ComputedStyle`. The input-routing epic sets `Element.States` from events. The markup epic of Phase 3 loads a sheet next to an XML tree.
- **Key architectural decisions for the epic as a whole:**
  - Q1 Inline style: a sparse inline `StyleProperties` set and a per-property mask on the element. The cascade skips masked properties.
  - Q2 Text properties: `Color`, `Font`, and `FontSize` move into `ComputedStyle` and inherit down the tree. `Label` reads them.
  - Q3 Transitions: colors, corner radius, and opacity only. A tween marks `Paint` only.
  - A2 A class or pseudo-state change marks the element and its subtree style-dirty (`DescendantStyle`), with no check of the sheet.
  - A3 A type selector matches a type id that each element class declares. The match path uses no reflection.
  - A8 Only code sets `Element.States` in this epic.
  - A9 Theme variables resolve at parse time, for each sheet. The selector depth is a constant of 2.
  - `Update` allocates no managed memory after warm-up with an active sheet and with running transitions.

---

## Tech Debt / Findings

Cross-feature tech debt and review findings. `/review-epic` fills this section. Start empty.

- (none yet)

---

## Master Overview Rollup

- **Rollup status:** Planned (0/4 features)
- **One-line summary for master:** Stylesheets, selectors with pseudo-states, a zero-allocation cascade into `ComputedStyle`, a text format with a parser, and paint transitions.

---

*This is a required file. Do not delete it. It marks the folder as an epic. Update it with `/update-epic style-system` after work on the features, and run `/review-epic style-system` to refresh the Tech Debt / Findings section.*
