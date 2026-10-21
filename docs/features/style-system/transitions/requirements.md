# Transitions - Requirements

**Epic:** style-system
**Created:** 2026-10-21

## Overview

This feature gives per-frame transitions on the paint members of `ComputedStyle`. The style pass writes the target value, and the transition pass interpolates from the old value to the target over a duration with an easing. It runs after the style pass in `UiContext.Update` and reads `deltaTime`.

## Requirements

- A transition applies to `BackgroundColor`, `BorderColor`, `Color`, `CornerRadius`, and `Opacity`. No other property transitions in this epic.
- A rule declares a transition with a property, a duration in seconds, and an easing. The easing set is linear, ease-in, ease-out, and ease-in-out.
- When the style pass changes a property with a transition, the transition pass starts a tween from the current value to the new target. A new target during a tween starts from the current interpolated value.
- The transition pass runs after the style pass, before layout. It writes the interpolated value into `ComputedStyle` and marks `Paint` only.
- A duration of 0 or less, `NaN`, or infinity writes the target at once.
- The tweens live in a pool. The pool grows during warm-up and never shrinks. `Update` with running tweens allocates no managed memory after warm-up. The allocation gate covers it.
- An element that leaves the tree ends its tweens.
- Unit tests cover each easing at 0, 0.5, and 1, the retarget rule, the zero duration, the dirty bits, and the pool reuse.

## Dependencies

- stylesheet-model must be complete. It gives the paint members, the dirty rules, and the pass order.
- style-parser gives the `transition` declaration. The feature can build with a sheet made in code and can run in parallel with style-parser.

## Out of Scope

- Transitions on layout lengths and on font size.
- Keyframe animations, delays, and a transition event.
- A transition on an inline value.
