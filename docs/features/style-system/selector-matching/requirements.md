# Selector Matching - Requirements

**Epic:** style-system
**Created:** 2026-10-21

## Overview

This feature completes the selector grammar of the style system. It adds the name match, the pseudo-state match, the depth-2 parent chain, the full specificity rule, and the dirty propagation when a class or a state changes. It builds on the model and the pass of stylesheet-model.

## Requirements

- A selector matches on class, name, element type, and pseudo-state, in any combination on one compound.
- The pseudo-states are `:hover`, `:active`, `:focus`, `:disabled`, and `:checked`. The match reads `Element.States`.
- A selector supports a parent-child chain of depth 2: `.a > .b` (child) and `.a .b` (descendant). The depth is a constant, not a list.
- Specificity follows the CSS order: name, then class and pseudo-state, then type. A later rule wins at equal specificity.
- A class or pseudo-state change marks the element and its subtree style-dirty (`DescendantStyle`), with no check of the sheet. A name change does the same.
- The match for a descendant selector walks at most two ancestors. The pass stays zero-allocation after warm-up.
- Unit tests cover each pseudo-state, each chain form, the specificity order, and the dirty propagation on a tree of depth 3 or more.

## Dependencies

- stylesheet-model must be complete. It gives the rule model, the pass, and the specificity slot.
- No parallel sibling. style-parser and transitions follow this feature.

## Out of Scope

- Setting `Element.States` from pointer or keyboard events. The input-routing epic does that.
- A chain deeper than 2, sibling selectors, attribute selectors, and `:not`.
- A check of the sheet before a subtree mark. That is a later optimization.
