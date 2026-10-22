# Transitions - Implementation Plan

**Epic:** style-system (feature 4 of 4)
**Status:** Ready for Review
**Started:** 2026-10-22
**Target Completion:** TBD
**Last Updated:** 2026-10-22 05:18

---

## Quality Bar

**Target quality:** A library feature inside the zero-allocation frame loop. The transition pass runs in each `UiContext.Update`, after the style pass and before the layout pass. A defect here shows in each frame of each game: a fade that restarts, a fade that never ends, a wrong dirty bit, or a byte of garbage for each frame. The feature adds no public API. Each rule of the requirements has a test, and the zero-byte gates prove the frame loop.

- **A correct value in each frame:** After an `Update(deltaTime)`, each member with a running tween holds `From + (To - From) * curve(elapsed / duration)`. The last step writes the exact target. A test checks the value of each of the five members at the midpoint and at the end.
- **The dirty rule of Q3:** The transition pass marks `Paint` only. It never marks `Style` or `Layout`, also on a `Label`. A test reads the bits of the pass alone through the internal seam `UpdateTransitions`.
- **Stable tweens under the restyle of A2:** A state change on an ancestor restyles the full subtree. A restyle that gives the same target keeps the running tween. A restyle with a new target starts from the current visible value.
- **Zero allocation after warm-up:** `Update` with running tweens allocates 0 bytes. The pool grows during warm-up and never shrinks. Two planted allocations (one in the restyle hook, one in the pass) each fail at least one gate.
- **No regression:** Each existing test passes. The one exception is `ParsedSheetAllocationTests`: it asserts the target values of a sheet with a transition after `Update(0f)`, so task 1.10 gives it a step time and a settle step. The HelloPanel smoke run keeps its 3 lines.
- **No new package and no reflection:** `Directory.Packages.props`, `src/BrutalUi.ImGui`, `src/BrutalUi.Markup`, and `samples/` do not change.

---

## Executive Summary

This feature makes the five paint members of `ComputedStyle` (`BackgroundColor`, `BorderColor`, `CornerRadius`, `Opacity`, and `Color`) move to a new value over time. A rule declares a transition through the `StyleRule.Transitions` set of style-parser. The style pass computes the target value and writes it, as it does now. A new hook in the style pass compares the old value with the target and starts, keeps, turns, or ends a tween. A new transition pass then runs in `UiContext.Update`, after the style pass and before the layout pass. It adds `deltaTime` to each tween, writes the interpolated value into `ComputedStyle`, and marks `Paint`.

The tweens live in a pool that the context owns. The pool holds one record for each element with a running tween. Each record holds five slots, one for each member that can transition, and a mask. A dictionary keyed by the element reference finds the record of an element in the restyle hook. The array and the dictionary grow during warm-up and never shrink.

The feature uses `TransitionEasing`, `StyleTransition`, and `StyleTransitions` of style-parser as they are. It adds internal members only: no public type and no public member changes. An inherited `Color` follows the tween of its parent, as in CSS (question 4). An element that leaves the tree drops its tweens.

---

## User Personas Alignment

No `docs/user-personas.md` file exists. The personas below come from `docs/mission-statement.md` and the epic docs.

**Target Users:**
- [x] **The markup author** (writes `transition: background-color 0.2s ease-out` in a sheet and sees a hover fade in, not jump)
- [x] **The game developer** (sets classes and states in code, calls `Update(deltaTime)` with the frame time, and gets smooth paint changes with no garbage)
- [x] **The sibling feature agents** (input-routing sets `:hover` from events, and the fade runs with no extra code)
- [x] **The end player** (sees a button fade on hover in place of a hard switch)

**User Subsets Served:**
- [x] **Simple** - One rule with `transition: background-color 0.2s linear` and a `:hover` rule. The fill fades in and fades out.
- [x] **Complex** - A transition list on several members, a different duration in the hover rule, a hover out during the fade, and a label text that fades with its button.

**UX Approach:**
- The behavior follows CSS where CSS has a rule: no fade on the first style of an element, the transition of the new style decides, a new target starts from the current value, and an inherited color follows its parent.
- The easings follow the CSS curves of the four keywords (question 3).
- A bad duration (0, negative, `NaN`, or infinity) gives the target at once. It gives no error and no exception.

---

## Goals

### Primary Goals
1. Interpolate the five members of Q3 over the duration and the easing of the cascade, in each `Update`.
2. Keep the dirty rule of Q3: the transition pass marks `Paint` only.
3. Keep each tween stable under the subtree restyle of A2, and turn it from the current value on a new target.
4. Keep `Update` at 0 bytes after warm-up with running tweens, and prove it with a gate and two plants.

### Success Criteria
- [ ] The phase 1 demo prints `t 0.0 #1E222CFF`, `t 0.1 #274176FF`, and `t 0.2 #3060C0FF`.
- [ ] The phase 2 demo prints `hover 0.1 #274176FF`, `leave 0.0 #274176FF`, `leave 0.1 #233251FF`, and `leave 0.2 #1E222CFF`.
- [ ] The phase 3 demo prints `linear 0.500`, `ease-in 0.315`, `ease-out 0.685`, and `ease-in-out 0.500`.
- [ ] The phase 4 demo prints `button #FFFFFFFF label #FFFFFFFF`, `button #FFE680FF label #FFE680FF`, and `button #FFCC00FF label #FFCC00FF`.
- [ ] The phase 5 demo prints `active 1 panel #274176FF`, `active 0 panel #274176FF`, and `active 0 panel #3060C0FF`.
- [ ] `ThousandElementAllocationTests` holds 12 tests with 0 failed, and the smoke log holds `draw commands 2, vertices 128` and `Update and Draw 0`.

---

## Architecture Overview

### File Tree (end state)

```
src/BrutalUi.Core/
  Style/
    StyleTransitions.cs     (edit: internal MergeFrom, slot helpers)
    StylePass.cs            (edit: the merge of the matched sets, the Track call, two flags)
    TransitionCurves.cs     (new, internal static: the four easing curves)
    TransitionRecord.cs     (new, internal: TweenSlot, TweenSlots, TransitionRecord)
    TransitionPool.cs       (new, internal sealed: the records, the index, Track, Advance)
  Tree/
    Element.cs              (edit: internal IsStyled and InheritsColor, the join mark)
    UiContext.cs            (edit: the pool, UpdateTransitions, the frame order, the AddRoot join mark)
tests/BrutalUi.Core.Tests/
  Style/
    TransitionPassTests.cs         (new, phase 1)
    TransitionRetargetTests.cs     (new, phase 2)
    TransitionCurveTests.cs        (new, phase 3)
    TransitionInheritanceTests.cs  (new, phase 4)
    TransitionLifetimeTests.cs     (new, phase 5)
  Shared/ThousandElementTree.cs    (edit, phase 6: the transition sheet)
  ThousandElementAllocationTests.cs (edit, phase 6: 2 new tests)
tests/BrutalUi.Markup.Tests/Style/
  ParsedSheetAllocationTests.cs    (edit, phase 1: the step time and the settle step)
```

### The Frame Order

```
UiContext.Update(deltaTime)
  1. UpdateStyles()          the style pass. Restyle computes next (the target) from the cascade.
                             It merges the transition sets of the matched rules (per slot, cascade order).
                             It calls pool.Track(element, in current, in next, in merged, inlineMask).
                             It writes current = next (the target) and marks Layout or Paint, as now.
  2. UpdateTransitions(dt)   the transition pass. pool.Advance(dt, roots) writes the interpolated
                             value of each running slot over the target and marks Paint only.
  3. The layout pass         as now. It reads no member of Q3.
  4. The clear pass          as now. It clears Style, Layout, and Paint.
```

`UpdateTransitions` is `internal`, as `UpdateStyles` is. The tests call it alone to read the bits of the pass before the clear pass.

### The Record Model

```
internal struct TweenSlot            // 44 bytes
    float4 From                      // the visible value at the start, as four floats
    float4 To                        // the target of the cascade, as four floats
    float  Elapsed                   // seconds since the start
    float  Duration                  // seconds, finite and more than 0
    TransitionEasing Easing

[InlineArray(5)] internal struct TweenSlots   // slot order of StyleTransitions:
                                              // BackgroundColor 0, BorderColor 1, CornerRadius 2, Opacity 3, Color 4
internal struct TransitionRecord
    Element Element
    byte Mask                        // one bit for each running slot
    TweenSlots Slots

internal sealed class TransitionPool
    TransitionRecord[] _records      // starts empty, grows to 4, then doubles. Never shrinks.
    int _count                       // the records 0 to _count - 1 run. A release swaps the last record in.
    Dictionary<Element, int> _index  // ReferenceEqualityComparer. Never shrinks.
    int ActiveCount, int Capacity    // internal, for the tests
```

A value of a member goes into four floats: a color as R, G, B, and A (0 to 255), the corners as top-left, top-right, bottom-right, and bottom-left, and the opacity in the first float. One `Read`, one `Write`, and one `Equal` switch over the slot hold the map.

### Track: the Restyle Hook

`StylePass.Restyle` calls `Track` before the write `current = next`, when the merged set is not empty or the pool holds a record. `Track` reads `current` and `next`. It never changes them.

1. Find the record of the element through `_index` (only when `_count > 0`).
2. When `element.IsStyled` is false (the first style in this tree), drop the record and return. The style pass writes the target.
3. For each slot, read the visible value from `current` and the target from `next`:
   - **Keep:** the slot runs, the target equals its `To`, and the inline set does not hold the member. The tween keeps its `From`, `Elapsed`, and duration.
   - **No change:** the target equals the visible value. End the slot when it runs.
   - **Start or turn:** the merged set holds the slot, the duration is finite and more than 0, both values are finite, and the inline set does not hold the member. Write `From` = visible, `To` = target, `Elapsed` = 0, and the duration and easing of the merged set. Rent a record at the first start.
   - **Else:** end the slot when it runs. The style pass writes the target at once.
4. Release the record when no slot runs.

### Advance: the Transition Pass

1. Read `deltaTime`. A value that is not more than 0 (also `NaN`) advances nothing. Infinity ends each tween.
2. Visit the records from the last to the first, so a release (a swap with the last record) skips no record.
3. Drop the record of an element that is not in the tree of this context: the walk up the parents ends at an element that is not a root of this context. The pass writes nothing and marks nothing (phase 5).
4. For each running slot: add `deltaTime` to `Elapsed`. When `Elapsed / Duration` is 1 or more, the value is `To`, and the slot ends. Else the value is `From + (To - From) * curve(Easing, Elapsed / Duration)`.
5. Write each value that differs from the member. When the `Color` member changes, write the same color to each descendant that inherits it and holds no own `Color` tween (phase 4).
6. When a member changed, mark the element `Paint`. Release the record when no slot runs.

### The Value of Each Member

| Member | Read | Interpolation | Write |
|--------|------|---------------|-------|
| `BackgroundColor`, `BorderColor`, `Color` | R, G, B, A as floats | Premultiplied: each RGB channel times A / 255, then a linear step, then a divide by the new A. With a new A of 0, the RGB channels take a straight linear step. | Each channel `(byte)(value + 0.5f)`, clamped to 0 to 255 |
| `CornerRadius` | four corners | linear for each corner | four corners, exact |
| `Opacity` | the getter (1 when unset) | linear | the setter |

At the end, the pass writes the exact target. Thus each member ends on the value that the cascade gives.

### The Curves (phase 3)

`TransitionCurves.Evaluate(TransitionEasing, float progress)` gives 0 for a progress of 0 or less and 1 for a progress of 1 or more. `Linear` gives the progress. The other three members use the CSS cubic Bezier curves: `EaseIn` (0.42, 0, 1, 1), `EaseOut` (0, 0, 0.58, 1), and `EaseInOut` (0.42, 0, 0.58, 1). The solver finds the curve parameter for the progress with up to 8 Newton steps (stop at an error below 1e-6). Then it uses up to 32 bisection steps on 0 to 1 when Newton fails. It allocates nothing and uses no table. Reference values at 0.5: `EaseIn` 0.31536, `EaseOut` 0.68464, `EaseInOut` 0.5.

### Data Flow

```
Load:   StyleSheet (parser or Builder) ─► StyleRule.Transitions ─► context.AddSheet ─► the cascade
Frame:  game sets States / Classes / Name ─► the subtree mark (A2)
        Update(dt) ─► StylePass.Restyle ─► next (target) + merged set ─► pool.Track ─► current = next
                   ─► pool.Advance(dt) ─► interpolated value into StyleStorage ─► MarkDirty(Paint)
                                         └─► Color: the follow walk into each inheriting descendant
                   ─► LayoutEngine (no member of Q3) ─► the clear pass
        Draw ─► PaintWalk reads Element.Style (the interpolated value)
```

---

## Implementation Phases

Each phase is a slice, per section 11 of `.beast-mode/phase-handoff.md`. No phase is a base phase. The requirements leave the demo of phase 1 open, so phase 1 uses the recommended line of open scope question 1 (rule 12).

Each phase ends with `dotnet build brutal-ui.sln 2>&1 | tail -n 30` and `dotnet test --solution brutal-ui.sln 2>&1 | tail -n 30` (tool rule 5). Expect `0 Warning(s)`, `0 Error(s)`, and 0 failed. A demo uses `dotnet run --project tests/BrutalUi.Core.Tests/BrutalUi.Core.Tests.csproj -- --filter-method '*Name' --show-live-output on`. A test prints with `TestContext.Current.TestOutputHelper?.WriteLine(...)`. The class table and the type table are global, and xUnit runs classes in parallel. Thus each test interns its names with the prefix `nameof(Test) + "."`, as `PseudoStateSelectorTests` does. A color prints as `#RRGGBBAA` (`Rgba8.ToString`). A time prints with `ToString("F1", CultureInfo.InvariantCulture)`, and an opacity with `"F3"`. The demos use the durations 0.2 s and the steps 0.1 s: `0.1f + 0.1f` equals `0.2f` exactly, so a tween ends on the second step with no float drift.

### Phase 1: A Hover Fades the Fill of a Button
**Goal:** The smallest path that proves the idea: the pool, the restyle hook, the transition pass in `Update`, the linear step for each of the five members, the merge of the matched sets, the first-style rule, and the `Paint` mark.
**Demo:** the developer runs `dotnet run --project tests/BrutalUi.Core.Tests/BrutalUi.Core.Tests.csproj -- --filter-method '*AHoverFadesTheFillOfAButton' --show-live-output on` and reads `t 0.0 #1E222CFF`, then `t 0.1 #274176FF` after `States = Hover` and `Update(0.1f)`, then `t 0.2 #3060C0FF` after one more `Update(0.1f)`.
**Needs:** none
**Agent tier:** ADVANCED (opus)

**Tier note:** This phase changes the style pass and the frame order of `UiContext.Update`, and it adds a hot path to the restyle of each element. A wrong keep rule restarts each tween on each subtree mark. A write of the visible value before the compare hides a `Paint` bit. The pool and the dictionary must not allocate after warm-up.

**Tasks:**

Group A (the model and the pool, 4 files):
- [ ] 1.1 Record in `context.md`: the base commit (`git rev-parse HEAD`), the test total (expected 2044), the build result, and the `bytes per element` line of `dotnet run -c Release --project tests/BrutalUi.Benchmarks/BrutalUi.Benchmarks.csproj -- --facts 2>&1 | tail -n 30` (expected 1049.5). Read `StylePass.cs`, `StyleTransitions.cs`, `StyleRule.cs`, `ComputedStyle.cs`, `Element.cs`, `UiContext.cs`, `Rgba8.cs`, `Corners.cs`, and `PseudoStateSelectorTests.AHoverRuleRestylesTheButton`.
- [ ] 1.2 Edit `src/BrutalUi.Core/Style/StyleTransitions.cs`: an internal constant `SlotCount` (5), make `SlotOf` internal, and add the internal members `PropertyOf(int slot)`, `IsSlotSet(int slot)`, `GetSlot(int slot)`, and `MergeFrom(in StyleTransitions later)`. `MergeFrom` copies each slot that `later` holds and sets its bit, so a later rule wins for each slot. Add no public member. The size stays 44 bytes (`TheSetHoldsNoHeapObject`).
- [ ] 1.3 Write `src/BrutalUi.Core/Style/TransitionCurves.cs` (internal static): `Evaluate(TransitionEasing easing, float progress)` clamps the progress to 0 to 1 and gives the linear value for each member. The remarks state that phase 3 gives the three other curves.
- [ ] 1.4 Write `src/BrutalUi.Core/Style/TransitionRecord.cs`: `TweenSlot`, `TweenSlots` (`[InlineArray(5)]`), and `TransitionRecord` per "The Record Model". Each type and each member is `internal` (I2), with doc comments.
- [ ] 1.5 Write `src/BrutalUi.Core/Style/TransitionPool.cs` (internal sealed) per "The Record Model", "Track", "Advance", and "The Value of Each Member". The later phases add five parts: the inline branch of `Track` (phase 2), the `deltaTime` rule of step 1 of "Advance" (phase 3), the follow walk of step 5 (phase 4), and the tree check of step 3 and the drop of step 2 of "Track" (phase 5). In this phase, step 2 of "Track" only returns, because a new element has no record. The duration guard of the start rule is in this phase, so a duration of 0 never divides. The array starts empty, grows to 4 at the first rent, and doubles. The dictionary uses `ReferenceEqualityComparer.Instance`. A release clears the freed array slot, so the pool holds no reference to an element after its release. `ActiveCount` and `Capacity` are internal. The remarks state the zero-allocation rule and the growth rule.

Group B (the wiring and the demo, 5 files):
- [ ] 1.6 Edit `src/BrutalUi.Core/Tree/Element.cs`: `internal bool IsStyled`. A new element holds `false`. The doc comment states the first-style rule.
- [ ] 1.7 Edit `src/BrutalUi.Core/Style/StylePass.cs`: `Run`, `Visit`, and `Restyle` take the `TransitionPool`. `Restyle` merges the set of each matched rule with a non-empty set into a local `StyleTransitions` (`MergeFrom`). Before the write `current = next`, it calls `Track` when the merged set is not empty or `ActiveCount > 0`. After the write, it sets `IsStyled`. `Compare` and the dirty rule table do not change. Update the remarks: the pass writes the target, and the transition pass writes the interpolated value over it.
- [ ] 1.8 Edit `src/BrutalUi.Core/Tree/UiContext.cs`: a `TransitionPool` field (both constructors), `UpdateStyles` passes it, a new internal `UpdateTransitions(float deltaTime)`, an internal `Transitions` property for the tests, and `Update` calls `UpdateStyles`, then `UpdateTransitions`, then the layout pass. Update the doc comment of `deltaTime` (the transition pass reads it) and the remarks of `Update` (the frame order).
- [ ] 1.9 Write `tests/BrutalUi.Core.Tests/Style/TransitionPassTests.cs`:
  - `AHoverFadesTheFillOfAButton` (the demo): `.<p>button` sets `BackgroundColor` #1E222C with `BackgroundColor` 0.2 s `Linear`. `.<p>button:hover` sets #3060C0. A root with one button. `Update(0f)`, then `States = Hover` and `Update(0.1f)`, then `Update(0.1f)`. Print `t <time> <color>` after each update. Assert the three lines.
  - `EachMemberFadesToItsTarget` (a theory, 5 rows): the value of the member at half the duration and the exact target at the full duration. Example rows: `CornerRadius` 0 to 8 gives 4, `Opacity` 1 to 0.5 gives 0.75.
  - `TheFirstStyleStartsNoTween`: a rule with a transition gives its value at the first `Update(0f)`, and `ActiveCount` is 0.
  - `ATweenMarksPaintOnly`: after a full `Update` with a running tween, `UpdateTransitions(0.05f)` alone gives `Paint` and no other bit on the button, and `DescendantPaint` and no other bit on the root. A `Label` with a corner radius tween gets no `Layout` bit.
  - `AMemberWithNoTransitionChangesAtOnce`: the hover rule also sets `BorderColor` with no transition. The border holds the target after the first update.
  - `ALaterMatchedRuleWinsForEachSlot`: `.a` sets two slots, a later `.b` sets one of them with another duration. Each slot uses the duration of its last rule.
  - `AColorFadesInPremultipliedAlpha`: transparent to #FF0000FF gives #FF000080 at half the duration.
  - `ASheetWithNoTransitionRentsNoRecord`: a hover with no transition gives `ActiveCount` 0 and `Capacity` 0.
- [ ] 1.10 Edit `tests/BrutalUi.Markup.Tests/Style/ParsedSheetAllocationTests.cs`: the gate text holds `transition: background-color 0.2s ease-out, opacity 150ms linear`, and `AssertTheRulesMatch` asserts the targets. With this feature, `Update(0f)` keeps those members at their start value. Change `Step` to `Context.Update(StepTime)` with `StepTime = 0.016f`, so the measured steps run the tweens. Add a `Settle()` method that runs `Context.Update(1f)` (more than each duration, outside the measure) and call it before each `AssertTheRulesMatch`. Update the remarks. Change no assert.
- [ ] 1.11 Run the build, the tests, and the demo. Run `--filter-class '*ThousandElementAllocationTests'`, `--filter-method '*TheStylePassAllocatesNothingAfterWarmUp'`, and the Markup gate, and expect 0 failed. Record the printed lines and the test total in `context.md`.

**Estimated Steps:** about 75 (11 tasks, about seven steps for one task)

**Files (9):** Group A: `StyleTransitions.cs`, `TransitionCurves.cs`, `TransitionRecord.cs`, `TransitionPool.cs`. Group B: `Element.cs`, `StylePass.cs`, `UiContext.cs`, `TransitionPassTests.cs`, `ParsedSheetAllocationTests.cs` (and `context.md`).

**Acceptance Criteria:**
- [ ] The demo prints the three lines, and the test asserts them.
- [ ] Each existing test passes with no change of an assert. The total is 2044 plus the new tests.
- [ ] `ThousandElementAllocationTests` (10 of 10) and `AParsedSheetAllocatesNothingInUpdate` pass with 0 bytes.
- [ ] `git diff -U0 <base commit> -- src/BrutalUi.Core | grep -E '^\+\s*public '` prints nothing.

---

### Phase 2: A Hover Out During the Fade Turns Back from the Current Color
**Goal:** The retarget path: a new target during a tween starts from the current visible value, a restyle with the same target keeps the tween, the transition of the new style decides, and an inline value never fades.
**Demo:** the developer runs `dotnet run --project tests/BrutalUi.Core.Tests/BrutalUi.Core.Tests.csproj -- --filter-method '*AHoverOutDuringTheFadeTurnsBack' --show-live-output on` and reads `hover 0.1 #274176FF`, then `leave 0.0 #274176FF`, `leave 0.1 #233251FF`, and `leave 0.2 #1E222CFF` after `States = None`.
**Needs:** Phase 1
**Agent tier:** STANDARD (sonnet)

**Tier note:** `Track` of phase 1 holds the keep branch and the turn branch. This phase adds the inline branch and proves each branch with a test. The demo values are exact: the turn starts from the stored `Rgba8` (39, 65, 118), and half of the step gives (34.5, 49.5, 81), which rounds to (35, 50, 81).

**Tasks:**

Group A (the retarget rules, 2 files):
- [ ] 2.1 Edit `src/BrutalUi.Core/Style/TransitionPool.cs`: `Track` reads the inline mask. A member that the inline set holds never starts a tween and ends its running slot, so the style pass writes the inline value at once. A member that leaves the inline set follows the start rule with the set of the rules (Decision 4). Update the remarks of `Track` with each branch.
- [ ] 2.2 Write `tests/BrutalUi.Core.Tests/Style/TransitionRetargetTests.cs`:
  - `AHoverOutDuringTheFadeTurnsBack` (the demo): the sheet of phase 1. `Update(0f)`, hover, `Update(0.1f)`, then `States = None` and `Update(0f)`, `Update(0.1f)`, and `Update(0.1f)`. Print and assert the four lines.
  - `ARestyleWithTheSameTargetKeepsTheTween`: hover, `Update(0.1f)`, then a state change on the root (a subtree mark, A2) and `Update(0.1f)`. The button holds the target, so the elapsed time stayed.
  - `ANewTargetTakesTheFullDuration`: a third color during the tween starts from the visible value and ends after the full duration.
  - `TheTransitionOfTheNewStyleApplies`: the base rule gives 0.4 s, the hover rule gives 0.1 s. The hover in ends after 0.1 s. The hover out runs 0.4 s.
  - `ANewTargetWithNoTransitionChangesAtOnce`: a class change that removes the only rule with the slot, during a tween, gives the new target at once and `ActiveCount` 0.
  - `AnInlineValueChangesAtOnce`, `AnInlineValueEndsARunningTween`, and `AClearedInlineValueFadesToTheRuleValue`.
- [ ] 2.3 Run the build, the tests, and the demo. Record the printed lines and the test total in `context.md`.

**Estimated Steps:** about 30 (3 tasks, about ten steps for one task)

**Files (2):** `TransitionPool.cs`, `TransitionRetargetTests.cs` (and `context.md`).

**Acceptance Criteria:**
- [ ] The demo prints the four lines, and the test asserts them.
- [ ] Each test of phase 1 passes with no change.

---

### Phase 3: Each Easing Shapes the Fade
**Goal:** The CSS curves of the four easings, the duration rule (0, negative, `NaN`, and infinity give the target at once), and the rule of `deltaTime`.
**Demo:** the developer runs `dotnet run --project tests/BrutalUi.Core.Tests/BrutalUi.Core.Tests.csproj -- --filter-method '*EachEasingShapesTheFade' --show-live-output on` and reads `linear 0.500`, `ease-in 0.315`, `ease-out 0.685`, and `ease-in-out 0.500`.
**Needs:** Phase 2
**Agent tier:** STANDARD (sonnet)

**Tier note:** The plan gives the control points, the solver, and the reference values. The demo prints three decimals, so a solver error below 1e-4 prints the same line. The duration guard of phase 1 exists already, and this phase proves it.

**Tasks:**

Group A (the curves and the clock, 3 files):
- [ ] 3.1 Edit `src/BrutalUi.Core/Style/TransitionCurves.cs` per "The Curves": the three Bezier curves, the Newton steps with the bisection fallback, and the exact ends. A switch over the four members needs no fallback branch (DS6.3 of style-parser). The remarks name the control points and the reference values.
- [ ] 3.2 Edit `src/BrutalUi.Core/Style/TransitionPool.cs`: `Advance` adds a `deltaTime` that is more than 0 only (`NaN` and a negative value advance nothing). Infinity ends each tween. Check that the start rule of `Track` rejects each duration that is not finite or not more than 0. Update the remarks of `UiContext.Update` for `deltaTime`.
- [ ] 3.3 Write `tests/BrutalUi.Core.Tests/Style/TransitionCurveTests.cs`:
  - `EachEasingShapesTheFade` (the demo): four buttons, each with `Opacity` 0, a hover rule with `Opacity` 1, and an `Opacity` transition of 0.2 s with one easing. Hover all four, `Update(0.1f)`, and print `<easing> <opacity>`. Assert the four lines.
  - `EachEasingAtZeroHalfAndOne` (a theory, 12 rows): `Evaluate` gives 0 at 0 and 1 at 1 exactly, and 0.5, 0.31536, 0.68464, and 0.5 at 0.5 (tolerance 1e-4).
  - `EachCurveRisesAndStaysInTheRange`: 1000 samples for each easing. Each value is from 0 to 1, and no value is less than the value before it.
  - `AProgressOutsideTheRangeIsClamped`: -1 gives 0, and 2 gives 1.
  - `EachBadDurationWritesTheTargetAtOnce` (a theory): 0, -0, -1, `NaN`, `PositiveInfinity`, and `NegativeInfinity`. After a hover and `Update(0f)`, the member holds the target, and `ActiveCount` is 0. `float.Epsilon` starts a tween (the control row).
  - `ADeltaTimeOfZeroOrLessAdvancesNothing` (a theory): 0, -0.1, and `NaN`. `AnInfiniteDeltaTimeEndsEachTween`.
  - `TheCurvesAllocateNothing`: 10000 calls of `Evaluate` for each easing after `AllocationGate.ReleaseAllocationContext()` give 0 bytes.
- [ ] 3.4 Run the build, the tests, and the demo. Record the printed lines and the test total in `context.md`.

**Estimated Steps:** about 35 (4 tasks, about nine steps for one task)

**Files (3):** `TransitionCurves.cs`, `TransitionPool.cs`, `TransitionCurveTests.cs` (and `context.md`, and the doc comment of `UiContext.Update`).

**Acceptance Criteria:**
- [ ] The demo prints the four lines, and the test asserts them.
- [ ] Each easing has a test at 0, 0.5, and 1. Each bad duration has a test row.

---

### Phase 4: The Text of a Label Fades with Its Button
**Goal:** An inherited `Color` follows the tween of its parent (question 4). The transition pass writes the color into each descendant that inherits it, with `Paint` only.
**Demo:** the developer runs `dotnet run --project tests/BrutalUi.Core.Tests/BrutalUi.Core.Tests.csproj -- --filter-method '*TheTextOfALabelFadesWithItsButton' --show-live-output on` and reads `button #FFFFFFFF label #FFFFFFFF`, then `button #FFE680FF label #FFE680FF` after `States = Hover` and `Update(0.1f)`, then `button #FFCC00FF label #FFCC00FF` after one more `Update(0.1f)`.
**Needs:** Phase 3
**Agent tier:** ADVANCED (opus)

**Tier note:** The phase touches the inheritance of the style pass, a new flag on each element, and a recursive walk in the transition pass. In the style pass, the children inherit the target of the parent. The walk then writes the visible value over it in the same `Update`. A child with its own `Color` tween must keep it, and the walk must skip its subtree.

**Tasks:**

Group A (the follow walk, 4 files):
- [ ] 4.1 Edit `src/BrutalUi.Core/Tree/Element.cs`: `internal bool InheritsColor`. The style pass writes it. The doc comment states the follow rule.
- [ ] 4.2 Edit `src/BrutalUi.Core/Style/StylePass.cs`: `Restyle` sets `InheritsColor` when no matching rule and no inline value set `Color` (the condition of `Inherit`).
- [ ] 4.3 Edit `src/BrutalUi.Core/Style/TransitionPool.cs`: after a write of `Color`, also at the end of the tween, `FollowColor(element, color)` visits each child. It skips a child with `InheritsColor` false and a child with a running `Color` slot (a lookup in `_index`), and their subtrees. It writes the color into each other child, marks `Paint` when the value changed, and visits the children of that child. The recursion depth is the depth of the tree, as in the style pass.
- [ ] 4.4 Write `tests/BrutalUi.Core.Tests/Style/TransitionInheritanceTests.cs`:
  - `TheTextOfALabelFadesWithItsButton` (the demo): `.<p>button` sets `Color` #FFFFFF with a `Color` transition of 0.2 s `Linear`. `.<p>button:hover` sets #FFCC00. The button holds a `Label` (the context gets a `RecordingRenderer` as its measurer). Print `button <color> label <color>` after each update. Assert the three lines.
  - `AGrandchildFollowsThroughAnInheritingChild`.
  - `AChildWithItsOwnColorDoesNotFollow`: a child rule sets `Color`. The child and its subtree keep their values.
  - `AChildWithItsOwnColorTransitionRunsItsOwnTween`: a child rule declares a `Color` transition of 0.4 s and sets no color. The child follows its own tween, and the walk skips it.
  - `AFollowMarksPaintOnly`: after a full `Update` with a running tween, `UpdateTransitions(0.05f)` alone gives `Paint` and no other bit on the label.
  - `AChildAddedDuringTheFadeFollows`: a child added during the tween holds the visible color of the button after the next `Update`.
- [ ] 4.5 Run the build, the tests, the demo, and the allocation gates of task 1.11. Record the printed lines and the test total in `context.md`.

**Estimated Steps:** about 40 (5 tasks, about eight steps for one task)

**Files (4):** `Element.cs`, `StylePass.cs`, `TransitionPool.cs`, `TransitionInheritanceTests.cs` (and `context.md`).

**Acceptance Criteria:**
- [ ] The demo prints the three lines, and the test asserts them.
- [ ] `TextInheritanceTests` passes with no change.
- [ ] The allocation gates of task 1.11 pass with 0 bytes.

---

### Phase 5: A Removed Panel Ends Its Tweens
**Goal:** An element that leaves the tree drops its tweens. An element that joins a tree starts no tween on its first style. The pool reuses its records and never shrinks.
**Demo:** the developer runs `dotnet run --project tests/BrutalUi.Core.Tests/BrutalUi.Core.Tests.csproj -- --filter-method '*ARemovedPanelEndsItsTweens' --show-live-output on` and reads `active 1 panel #274176FF`, then `active 0 panel #274176FF` after `root.Children.Remove(panel)` and `Update(0.1f)`, then `active 0 panel #3060C0FF` after `root.Children.Add(panel)` and `Update(0f)`.
**Needs:** Phase 4
**Agent tier:** ADVANCED (opus)

**Tier note:** The phase changes the attach path of `Element` and `UiContext.AddRoot`, which mark each subtree. The tree check runs for each record in each frame. A write to a dropped element can overwrite the style that another context gave it (Decision 9).

**Tasks:**

Group A (the lifetime, 4 files):
- [ ] 5.1 Edit `src/BrutalUi.Core/Style/TransitionPool.cs`: step 3 of "Advance" (the tree check: walk up the parents, then compare the top element with each root of this context by reference). A record outside the tree drops with no write and no mark. Step 2 of "Track" drops the record of an element with `IsStyled` false. Update the remarks.
- [ ] 5.2 Edit `src/BrutalUi.Core/Tree/Element.cs`: a private static `MarkSubtreeJoinedCore` that sets the style bits as `MarkSubtreeStyleCore` does and sets `IsStyled` to false on each element of the subtree. `AttachToParent` calls it. Add an internal `MarkSubtreeJoined()` (the core walk and the ancestor bits). `MarkSubtreeStyle` does not change, so a class, state, name, or sheet change keeps `IsStyled`.
- [ ] 5.3 Edit `src/BrutalUi.Core/Tree/UiContext.cs`: `AddRoot` calls `MarkSubtreeJoined()` in place of `MarkSubtreeStyle()`. Update the remarks of `AddRoot`.
- [ ] 5.4 Write `tests/BrutalUi.Core.Tests/Style/TransitionLifetimeTests.cs`:
  - `ARemovedPanelEndsItsTweens` (the demo): the sheet of phase 1 on a panel under a root. Print `active <ActiveCount> panel <color>` after each step. Assert the three lines.
  - `ARemovedAncestorEndsTheTweensOfItsSubtree` and `ARemovedRootEndsItsTweens`.
  - `AMoveInsideTheTreeEndsTheTween`: a remove and an add between two updates. The panel holds the target after the next `Update(0f)`.
  - `AMoveIntoAnotherContextStartsNoTween`: the second context gives the target at once, and a later `Update` of the first context does not write the element.
  - `ThePoolGrowsAndNeverShrinks`: 40 tweens give a `Capacity` of 40 or more. After each tween ends, `ActiveCount` is 0 and `Capacity` holds its peak.
  - `ThePoolReusesItsRecords`: 40 panels hover in and out. After two rounds of warm-up, `AllocationGate.ReleaseAllocationContext()`, then two measured rounds (each with a state change on each panel, `Update(0.016f)` steps, and an `Update(1f)` to end each tween) give 0 bytes, and `Capacity` does not change.
- [ ] 5.5 Run the build, the tests, and the demo. Record the printed lines and the test total in `context.md`.

**Estimated Steps:** about 40 (5 tasks, about eight steps for one task)

**Files (4):** `TransitionPool.cs`, `Element.cs`, `UiContext.cs`, `TransitionLifetimeTests.cs` (and `context.md`).

**Acceptance Criteria:**
- [ ] The demo prints the three lines, and the test asserts them.
- [ ] `ElementListTests`, `TreeGuardTests`, `MultiSheetTests`, and `SubtreeMarkTests` pass with no change.
- [ ] `ThePoolReusesItsRecords` asserts 0 bytes.

---

### Phase 6: A Thousand Elements with Running Tweens Allocate Nothing
**Goal:** The 1000-element gate runs a transition sheet in its measured body, two plants prove the new hot paths, and the design doc, the changelog, the human test list, and the hand-offs hold the feature.
**Demo:** the developer runs `dotnet run --project tests/BrutalUi.Core.Tests/BrutalUi.Core.Tests.csproj -- --filter-class '*ThousandElementAllocationTests'` and reads a total of 12 with `failed: 0`.
**Needs:** Phase 5
**Agent tier:** ADVANCED (opus)

**Tier note:** The allocation gate is the release gate of the project. When a gate measures bytes above 0, find the cause in `src/` before a change of the test. The plants must restore each file exactly. Group B is plain documentation work.

**Tasks:**

Group A (the gate and the plants, 3 files):
- [ ] 6.1 Edit `tests/BrutalUi.Core.Tests/Shared/ThousandElementTree.cs`: a public static `BuildTransitionSheet()` with three transition-only rules. `.slot`: `BackgroundColor` 0.1 s `EaseOut` and `BorderColor` 0.5 s `EaseInOut`. `.muted`: `Color` 0.1 s `Linear`. `#header`: `BorderColor` 0.3 s `EaseIn`. The base sheet keeps its 16 rules. The doc comment states what each rule covers: the `selected` toggle ends the fill tween before the next style step (9 steps of 0.016 s), the hover toggle turns the slot border tween, the `muted` toggle runs the follow walk into the sidebar, and the name toggle turns the header border tween.
- [ ] 6.2 Edit `tests/BrutalUi.Core.Tests/ThousandElementAllocationTests.cs`:
  - `UpdateWithRunningTransitionsAllocatesNothing`: the warm scene, `AddSheet(BuildTransitionSheet())`, one `Update`, the warm-up of `UpdateAllocatesNothingAfterWarmUp`, the release, 100 measured steps, and 0 bytes. Assert also that `Capacity` is the same before and after the measure.
  - `TheTransitionStepRunsTweens`: after one style step, `ActiveCount` is 3 or more, the slot fill and the sidebar label color lie between their two values (each channel), and the label color equals the sidebar color. After 9 more steps, the fill and the label color hold their targets, and the slot border still runs.
  - Update the class remarks. The class holds 12 tests.
- [ ] 6.3 Plant 1: copy `src/BrutalUi.Core/Style/TransitionPool.cs` to the session scratch folder. Add `GC.KeepAlive(new object());` as the first line of the record loop of `Advance`. Run `dotnet test --solution brutal-ui.sln > <scratch>/plant-advance.log 2>&1; echo "exit=$?"` and expect a value that is not 0, with `UpdateWithRunningTransitionsAllocatesNothing` in the failed list. Restore the file from the copy, and run `diff` on the two files (no output). Record the exit code and each failed test in `context.md`.
- [ ] 6.4 Plant 2: the same steps with the plant as the first line of `Track`. Expect `UpdateWithRunningTransitionsAllocatesNothing` and `AParsedSheetAllocatesNothingInUpdate` in the failed list. Restore, `diff`, and record.
- [ ] 6.5 Run the facts line of task 1.1 and record `bytes per element`. Run `scripts/test-linux.sh; echo "exit=$?"` and expect the PASS line and `exit=0`. Read the 3 lines of `artifacts/test-linux/smoke.log`: `120 frames rendered`, `draw commands 2, vertices 128`, and `Update and Draw 0`. Record each result in `context.md`.

Group B (the record, 4 files):
- [ ] 6.6 Edit `docs/technical-design.md`. Section 4: the transition pass in the frame order, after the style pass. Section 7, `### Transitions`: the record model, the merge rule, the start, keep, turn, and end rules, the first-style rule, the inline rule, the duration and `deltaTime` rules, the curves with their control points, the premultiplied color step, the follow rule of `Color`, the leave and join rules, the pool growth, and the cost. Update the sentence "The style pass does not read the set". Add design decision lines for the pool and the follow walk. Section 14: the five new test classes, the 2 new tests of the gate, the change of `ParsedSheetAllocationTests`, and the results of the plants. Update the cost line with the facts of task 6.5 when the value changed.
- [ ] 6.7 Edit `CHANGELOG.md`: one line under `### Added` (the transitions of the five paint members in `UiContext.Update`, with `deltaTime`) and one line under `### Changed` (`Update` now reads `deltaTime`, and a rule with a transition no longer gives its target in the same `Update`). Name the feature `style-system/transitions`.
- [ ] 6.8 Edit `docs/features/style-system/human-testing.md`: add a `## transitions` section with the 6 phase demos and the Windows run of `.\scripts\test-windows.ps1`.
- [ ] 6.9 Run `python3 /home/aaronstatic/.claude/skills/asd-ste100/scripts/ste-lint.py --fail-over 2.5` on each changed doc and on this plan, and fix each hit. Write the hand-offs of Notes into `context.md`. Add a "Native Dependencies" note: none. Run the build and the tests.

**Estimated Steps:** about 70 (9 tasks, about eight steps for one task)

**Files (7):** Group A: `ThousandElementTree.cs`, `ThousandElementAllocationTests.cs` (and a temporary edit of `TransitionPool.cs`, restored, and `context.md`). Group B: `docs/technical-design.md`, `CHANGELOG.md`, `docs/features/style-system/human-testing.md`, and `context.md`.

**Acceptance Criteria:**
- [ ] The demo reads a total of 12 with `failed: 0`.
- [ ] Each plant gives a non-zero exit code with the expected gates in the failed list, and each restored file is the same as its copy.
- [ ] The smoke log holds the 3 lines, and `test-linux.sh` exits with 0.
- [ ] The lint gives no hit above 2.5 for each changed doc.

**Total estimate:** about 290 steps over 6 phases and 37 tasks.

---

## Key Technical Decisions

### Confirmed Assumptions

- Q3 Transitions: colors, corner radius, and opacity only. A tween marks `Paint` only.
- A2 A class or pseudo-state change marks the element and its subtree style-dirty (`DescendantStyle`), with no check of the sheet.
- A8 Only code sets `Element.States` in this epic.
- `Update` allocates no managed memory after warm-up with an active sheet and with running transitions.

The epic interview confirmed these items (`epic-overview.md`). The decisions below cite them.

### Decision 1: A context pool of records for each element, found through a reference dictionary (the chosen approach)
**Choice:** `TransitionPool` belongs to `UiContext`. It holds one `TransitionRecord` for each element with a running tween, with five inline slots and a mask, in a dense array. A `Dictionary<Element, int>` with `ReferenceEqualityComparer` gives the record of an element. The style pass calls `Track`, and `Update` calls `Advance` after the style pass.
**Rationale:** Only the elements with a running tween cost memory. The element class does not grow by a slot set or an index. The dense array gives the transition pass a loop over the running records only, with no tree walk. The dictionary gives the restyle hook a constant-time lookup, also during the restyle of 1000 elements in the gate. Each context owns its own records, so a move into another context cannot mix two pools. An array and a dictionary that never shrink allocate nothing after warm-up, and `_count == 0` skips the lookup in a frame with no tween.
**Alternatives:** (a) Tween slots stored on the element: no lookup, but each element of each tree pays about 230 bytes (about 22 % of 1049.5), and the pass needs a tree walk or a list anyway. (b) One record for each element and slot, found by a linear scan: a small record, but each restyled element costs a scan of each running tween. (c) An index field on the element: no hash, but the element grows, and a move into another context needs an owner check to stop the old pool from writing the index of the new pool.

### Decision 2: The style pass writes the target, and the transition pass writes over it
**Choice:** `Track` reads the old value (`current`) and the target (`next`) before the write `current = next`, and it changes neither. The style pass writes the target and marks the bits as now. The transition pass then writes the interpolated value over the target in the same `Update`.
**Rationale:** The requirement says that the style pass writes the target. `Compare` and the dirty rule table stay the same. The children of a `Color` change inherit the target in the same pass, so no child keeps an old color when no follow runs. The turn reads the visible value of the last `Update` from `current`, which is the value on the screen. `UpdateStyles` gives the same values as before, so each existing style test stays true.
**Alternatives:** `Track` writes the visible value back into `next` for each running slot: `Compare` marks no `Paint` for the slot, but the children inherit the old value and keep it when the tween ends, because the pass marks no `Style`.

### Decision 3: The merge of the transition sets (question 2)
**Choice:** For each slot, the last matched rule in the order of the cascade that holds the slot wins. The set of the new style decides the duration and the easing.
**Rationale:** `StyleProperties` merges in the same way, and the parser merges the slots inside one rule in the same way (DS6.13). A hover rule can change the duration of one member and keep the transition of the base rule for another member. The new style decides, as in CSS: the hover in uses the hover rule, and the hover out uses the base rule.
**Alternatives:** The full set of the last matched rule with a non-empty set, as the CSS `transition` property does: the CSS result, but a hover rule that names one member drops the transitions of the other members, and the parser rule DS6.13 then differs from the cascade rule.

### Decision 4: The start, keep, turn, and end rules
**Choice:** Per "Track". A tween starts on a change of the value when the merged set holds the slot, the duration is finite and more than 0, both values are finite, the element has a style in this tree (`IsStyled`), and the inline set does not hold the member. A restyle with the same target keeps the tween. A new target turns the tween from the visible value, with the full duration. An inline member never fades and ends its slot. A member that leaves the inline set fades from the inline value to the rule value, when the set of the rules holds the slot.
**Rationale:** A2 restyles a full subtree on each state change, so a restart on each restyle would stop each fade under a hover of an ancestor. The first-style rule follows CSS: a new screen does not fade in from transparent. "A transition on an inline value" is out of scope, so an inline value goes in at once. After a clear, the target comes from the rules, so the transition of the rules applies.
**Alternatives:** (a) A restart on each restyle: simpler, but each subtree mark restarts each tween. (b) The CSS reversing factor, which shortens a tween that turns back to its start value: the CSS result, but more state, and the requirement asks for a start from the current value only (Conflicts 1).

### Decision 5: The CSS cubic Bezier curves (question 3)
**Choice:** `Linear`, and the curves `EaseIn` (0.42, 0, 1, 1), `EaseOut` (0, 0, 0.58, 1), and `EaseInOut` (0.42, 0, 0.58, 1), with a Newton solver and a bisection fallback.
**Rationale:** The enum doc and the format name the CSS keywords. A CSS author expects the CSS curve. The solver is about 40 lines, allocates nothing, and runs only for a running slot.
**Alternatives:** (a) The quadratic curves (0.25, 0.75, and 0.5 at the midpoint): exact values and less code, but another curve than CSS for the same keyword. (b) The cubic curves (0.125, 0.875, and 0.5): the same trade-off with a stronger curve.

### Decision 6: The clock of a tween
**Choice:** A tween advances by `deltaTime` in each `Update`, also in the `Update` that starts it. A progress of 1 or more writes the exact target and ends the slot. A `deltaTime` that is not more than 0 (also `NaN`) advances nothing. Infinity ends each tween.
**Rationale:** The total time of a tween is its duration in `Update` time. The style change happened during the last frame, so the first drawn frame shows the progress of one frame, as in a browser. A bad `deltaTime` from a game cannot move a tween back or give `NaN` in a color.
**Alternatives:** A new tween waits one `Update`: the first frame shows the start value, but the tween then takes one frame longer than its duration, and the record needs a flag.

### Decision 7: A color step in premultiplied alpha
**Choice:** The color slots step on premultiplied channels and divide by the new alpha. Each channel rounds half up. The first and the last value are exact.
**Rationale:** CSS Color 4 interpolates in premultiplied space. A fade from `default(Rgba8)` (transparent black) to a color gives the color with a growing alpha, not a dark color. For two opaque colors, the result is the same as a straight step, so the demos hold exact values.
**Alternatives:** A straight step on each byte: less code, but a fade from transparent shows a dark fringe, and the default fill of each element is transparent.

### Decision 8: An inherited `Color` follows its parent (question 4)
**Choice:** The style pass writes `InheritsColor` on each element. After a write of `Color`, the transition pass writes the same color into each descendant with `InheritsColor` and no own `Color` slot, and marks `Paint`. A child with its own `Color` transition runs its own tween.
**Rationale:** In CSS, a child inherits the animated value of its parent. A `Label` draws the text color, and a hover rule sets the color on the button, so a fade on the button must reach the label. The walk marks `Paint` only (Q3), and it needs no restyle and no record for each child.
**Alternatives:** (a) Each inheriting child copies the tween of its parent in the style pass: the same result, but one record for each child, and a restyle that must read the record of the parent. (b) No follow: the child takes the target at once, and a fade of a label needs a rule on the label itself.

### Decision 9: The leave and join rules
**Choice:** The pass drops the record of an element outside the tree of its context with no write and no mark. An attach and `AddRoot` set `IsStyled` to false on the full subtree, so the next restyle drops each record of the subtree and starts no tween.
**Rationale:** An element has no context reference, so the pass checks the tree from the element up. A write at the drop could overwrite the style that a second context gave the element after a move. A join restyles the full subtree already (CR-1 of stylesheet-model), so the element gets its target from the cascade.
**Alternatives:** (a) The drop writes the target: a defined value outside the tree, but a stale write after a move into another context. (b) A notice from `DetachFromParent` to the pool: no walk, but the element needs a reference to its context.

### Decision 10: No public API
**Choice:** Each new type and each new member is `internal`. The tests use `UpdateStyles`, `UpdateTransitions`, and `Transitions` through `InternalsVisibleTo`.
**Rationale:** The rule and the format of style-parser give the public surface. A game needs no handle on a tween in this epic. A later need for a count or an event can add a public member then (Gaps).
**Alternatives:** A public `UiContext.ActiveTransitionCount` for debug output: small, but a public member with no user yet.

---

## Risks & Mitigation

| # | Risk | Impact | Mitigation |
|---|------|--------|------------|
| 1 | The keep rule fails, and each restyle restarts a tween. | A hover on an ancestor (A2) stops each fade below it. | `ARestyleWithTheSameTargetKeepsTheTween`. The 1000-element step marks each element with `Style` during running tweens. |
| 2 | The first `Update` fades each element in from its defaults. | Each new screen fades in from transparent. | `IsStyled` and `TheFirstStyleStartsNoTween`. The join rule of phase 5. |
| 3 | `ParsedSheetAllocationTests` asserts targets after `Update(0f)` on a sheet with transitions. | The gate fails in phase 1. | Task 1.10 adds the step time and the settle step, with no change of an assert. |
| 4 | The pool or the dictionary grows inside a measured region. | A gate fails with a few hundred bytes. | The warm-up runs two full rounds. The gate asserts the same `Capacity` before and after the measure. |
| 5 | A float sum of the steps misses the end by one ulp. | A tween ends one frame late, and a demo line differs. | The end rule is `>= 1`. The demos use 0.1 s steps on 0.2 s, which add exactly. |
| 6 | The follow walk and an own `Color` tween write the same child. | The child color jumps between two values. | The walk skips a child with a running `Color` slot and its subtree. `AChildWithItsOwnColorTransitionRunsItsOwnTween`. |
| 7 | A drop after a move writes into the style of the new context. | A moved element shows a stale color. | Decision 9: the drop writes nothing. `AMoveIntoAnotherContextStartsNoTween`. |
| 8 | The Bezier solver gives a value off by more than 1e-4. | A demo line or a theory row fails. | Newton with a bisection fallback. The demo prints three decimals. The reference values come from the control points. |
| 9 | The dictionary lookup costs time in a restyle of 1000 elements with a running tween. | A slower hover on a high ancestor. | The lookup runs only when the pool holds a record. The 1000-element gate measures the worst case. A benchmark waits for a need (Gaps). |
| 10 | The two new flags grow the element. | The facts line grows. | Two `bool` fields fit into the padding or add 8 bytes. Tasks 1.1 and 6.5 record the line. |
| 11 | The tree check walks up the parents for each record in each frame. | Cost with many deep tweens. | The cost is the depth for each running record. A running record is rare outside a hover. |

---

## Dependencies

**Internal (done):**
- stylesheet-model: `StylePass` (`Restyle`, `Compare`, `Inherit`), `UiContext.Update` and `UpdateStyles`, `ComputedStyle`, the inline set and its mask, and `Element.StyleStorage`.
- selector-matching: the subtree mark of a class, state, and name change (A2), and the match that reads the tree, not the computed style.
- style-parser: `TransitionEasing`, `StyleTransition`, `StyleTransitions`, `StyleRule.Transitions`, the new `Add` overload, and the `transition` declaration.
- core-tree and imgui-renderer: `Element`, `ElementList`, `DirtyFlags`, `Rgba8`, `Corners`, `float4`, `RecordingRenderer`, `AllocationGate`, and `ThousandElementTree`.

**External (pinned, no change):**
- `Ahwoo.BRUTAL.Numerics` (`float4`) and `xunit.v3`. No new package and no native dependency.

**Downstream:**
- input-routing sets `Element.States` from events, and each hover rule with a transition then fades.
- The framework of Phase 3 swaps classes on a mode change, and the keep rule holds the running tweens.

---

## Definition of Done

### Functional
- [ ] F1. Each phase demo prints its lines, and its test asserts them.
- [ ] F2. Each of `BackgroundColor`, `BorderColor`, `CornerRadius`, `Opacity`, and `Color` moves from its old value to its target over the duration of the merged set, and holds the exact target after the full duration (`EachMemberFadesToItsTarget`).
- [ ] F3. The first style of an element in a tree starts no tween. A member with no transition and an inline member change at once.
- [ ] F4. A restyle with the same target keeps the tween. A new target starts from the visible value of the last `Update` (`AHoverOutDuringTheFadeTurnsBack`, `ARestyleWithTheSameTargetKeepsTheTween`).
- [ ] F5. For each slot, the last matched rule in the cascade order wins, and the set of the new style decides (`ALaterMatchedRuleWinsForEachSlot`, `TheTransitionOfTheNewStyleApplies`).
- [ ] F6. Each easing gives 0 at 0, 1 at 1, and 0.5, 0.31536, 0.68464, or 0.5 at 0.5 (tolerance 1e-4).
- [ ] F7. A duration of 0, -0, -1, `NaN`, or an infinity gives the target at once and no record. A `deltaTime` of 0, a negative value, or `NaN` advances nothing. An infinite `deltaTime` ends each tween.
- [ ] F8. An inherited `Color` follows its parent, a child with its own color does not, and a child with its own `Color` transition runs its own tween.
- [ ] F9. An element that leaves the tree drops its tweens with no write. An element that joins starts no tween on its first style.
- [ ] F10. The pool grows during warm-up and never shrinks. A second round of tweens allocates 0 bytes and keeps `Capacity`.

### Quality
- [ ] Q1. `dotnet build brutal-ui.sln 2>&1 | tail -n 30` prints `0 Warning(s)` and `0 Error(s)`.
- [ ] Q2. `dotnet test --solution brutal-ui.sln 2>&1 | tail -n 30` prints 0 failed. The total is 2044 plus the new tests.
- [ ] Q3. `UpdateWithRunningTransitionsAllocatesNothing`, `ThePoolReusesItsRecords`, `TheCurvesAllocateNothing`, `AParsedSheetAllocatesNothingInUpdate`, `TheStylePassAllocatesNothingAfterWarmUp`, and each test of `ThousandElementAllocationTests` pass with 0 bytes. Both plants of tasks 6.3 and 6.4 give a non-zero exit code.
- [ ] Q4. The transition pass marks `Paint` only: `ATweenMarksPaintOnly` and `AFollowMarksPaintOnly` pass.
- [ ] Q5. `ste-lint.py --fail-over 2.5` exits with 0 for each changed doc.

### Integration
- [ ] I1. `git diff --quiet <base commit> -- Directory.Packages.props src/BrutalUi.ImGui src/BrutalUi.Markup samples; echo "exit=$?"` prints `exit=0`.
- [ ] I2. `git diff -U0 <base commit> -- src/BrutalUi.Core | grep -E '^\+\s*public '` prints nothing. `grep -nE '^\s*public ' src/BrutalUi.Core/Style/Transition*.cs` prints nothing.
- [ ] I3. `grep -nE 'System\.Reflection|GetType\(\)|typeof\(|HasFlag' src/BrutalUi.Core/Style/*.cs` prints nothing.
- [ ] I4. `grep -rnE '\.Style\.[A-Za-z]+(\.[A-Za-z]+)?\s*=[^=]' src samples tests --include=*.cs` prints nothing.
- [ ] I5. `UiContext.Update` calls `UpdateStyles`, then `UpdateTransitions`, then the layout pass (read the method).
- [ ] I6. `scripts/test-linux.sh; echo "exit=$?"` prints the PASS line and `exit=0`, and `artifacts/test-linux/smoke.log` holds `draw commands 2, vertices 128` and `Update and Draw 0`.

### Verification Method
1. Run Q1 and Q2.
2. Run the demo command of each phase and compare the printed lines with the Demo line.
3. Run `dotnet run --project tests/BrutalUi.Core.Tests/BrutalUi.Core.Tests.csproj -- --filter-class '*ThousandElementAllocationTests'` (12 tests) and `--filter-class '*TransitionLifetimeTests'`, and `dotnet run --project tests/BrutalUi.Markup.Tests/BrutalUi.Markup.Tests.csproj -- --filter-class '*ParsedSheetAllocationTests'`. Expect `failed: 0`.
4. Run I1 to I6 with the base commit of task 1.1.
5. Read `context.md` for the exit codes and the failed lists of tasks 6.3 and 6.4, and the facts lines of tasks 1.1 and 6.5.

---

## Testing Strategy

- **Pass tests (`TransitionPassTests`):** the five members at the midpoint and the end, the first-style rule, the merge of the slots, the premultiplied color, and the dirty bits of the pass alone through `UpdateTransitions`.
- **Retarget tests (`TransitionRetargetTests`):** the turn from the visible value, the keep rule under a subtree mark, the full duration of a turn, the set of the new style, and the inline rules.
- **Curve tests (`TransitionCurveTests`):** each easing at 0, 0.5, and 1, the range and the rise of each curve, the clamp, each bad duration, each bad `deltaTime`, and a 0-byte check of `Evaluate`.
- **Inheritance tests (`TransitionInheritanceTests`):** the follow walk, the stop at an own color, the own tween of a child, a child added during the fade, and the `Paint` bit of a follower.
- **Lifetime tests (`TransitionLifetimeTests`):** the drop on a remove of the element, of an ancestor, and of a root, a move inside the tree, a move into another context, the growth rule, and a 0-byte reuse gate.
- **Allocation gate (`ThousandElementAllocationTests`):** the 1000-element step with a transition sheet that covers each easing, an end, a turn, and the follow walk. Two plants prove the gate for `Track` and for `Advance`.
- **Regression:** each existing test passes. `ParsedSheetAllocationTests` gets a step time and a settle step (task 1.10). The HelloPanel smoke run keeps its 3 lines.
- **Manual:** none beyond the demos and the Windows run. The sample has no input that sets `:hover` yet.

---

## Related Documentation

- `docs/features/style-system/transitions/requirements.md`
- `docs/features/style-system/epic-overview.md` and `epic-requirements.md`
- `docs/features/style-system/stylesheet-model/context.md` (`## Hand-offs`, the transitions part)
- `docs/features/style-system/selector-matching/context.md` (`## Hand-offs`, the transitions part)
- `docs/features/style-system/style-parser/context.md` (`## Hand-offs`, the transitions part, and DS6.1 to DS6.17)
- `docs/technical-design.md` sections 4, 7, and 14
- `.beast-mode/phase-handoff.md` section 11 (the slice rules)

---

## Notes

### Open Scope Questions

No interview ran for this draft. The plan uses the recommended option of each question.

**Question 1: Which demo does phase 1 use (slice rule 12)?**
- **Recommended:** `**Demo:** the developer runs dotnet run --project tests/BrutalUi.Core.Tests/BrutalUi.Core.Tests.csproj -- --filter-method '*AHoverFadesTheFillOfAButton' --show-live-output on and reads t 0.0 #1E222CFF, t 0.1 #274176FF, and t 0.2 #3060C0FF`. Reason: the colors of the first demo of selector-matching, one hover, and an exact midpoint.
- Option B: `**Demo:** the developer runs the filter '*AHoverFadesTheOpacityOfAPanel' and reads opacity 1.000, opacity 0.750, and opacity 0.500`. Reason: a simple number, but no color step, and the opacity hides the premultiplied rule.
- Option C: `**Demo:** the developer runs scripts/test-linux.sh and the HelloPanel panel fades in`. Reason: a visible result, but the sample has no input that sets `:hover`, so the sample needs a timer that sets a state, and the run needs a GPU.

**Question 2: How do the transition sets of the matched rules merge?**
- **Recommended:** For each slot, the last matched rule in the cascade order that holds the slot wins. Reason: the same rule as `StyleProperties` and as the parser inside one rule (DS6.13). A hover rule can change one duration and keep the others.
- Option B: The full set of the last matched rule with a non-empty set, as the CSS `transition` property. Reason: the CSS result, but a hover rule that names one member drops the transitions of each other member.

**Question 3: Which curves do the easings use?**
- **Recommended:** The CSS cubic Bezier curves: ease-in (0.42, 0, 1, 1), ease-out (0, 0, 0.58, 1), and ease-in-out (0.42, 0, 0.58, 1). Reason: the format and the enum name the CSS keywords, so a CSS author gets the CSS curve.
- Option B: The quadratic curves: t², 1 - (1 - t)², and the two-part quadratic (0.25, 0.75, and 0.5 at the midpoint). Reason: exact values and less code, but another curve than CSS.
- Option C: The cubic curves (0.125, 0.875, and 0.5 at the midpoint). Reason: a stronger curve with exact values, but also another curve than CSS.

**Question 4: How does a `Color` tween reach the children that inherit the color?**
- **Recommended:** The transition pass writes the interpolated color into each descendant that inherits it and holds no own `Color` tween, and it marks `Paint` only. Reason: the CSS result. The label of a button fades with the button, with no record and no restyle for each child.
- Option B: Each inheriting child copies the tween of its parent in the style pass. Reason: the same result, but one record for each child, and the restyle of a child reads the record of its parent.
- Option C: No follow. A child takes the target at once, and a fade of a label needs a rule with a transition on the label. Reason: no new code, but a hover color on a button makes its label jump.

### Conflicts

1. **The turn of a tween and CSS.** CSS shortens a transition that turns back to its start value (the reversing shortening factor). The requirement asks for a start from the current value, so this plan uses the full duration for each turn. Section 7 states the difference.
2. **The visible value between the passes.** The requirement says that the style pass writes the target. It does (Decision 2). The transition pass writes the interpolated value over it in the same `Update`. Only the internal seam `UpdateStyles` shows the target before the transition pass.
3. **A cleared inline value.** "A transition on an inline value" is out of scope, so an inline value never fades. A clear gives the member back to the rules, and the transition of the rules applies (Decision 4).
4. **`ParsedSheetAllocationTests` of style-parser.** Its gate text holds a transition list, and its asserts read the targets after `Update(0f)`. With this feature, those members stay at their start value. Task 1.10 gives the steps a time of 0.016 s and adds a settle step before the asserts. No assert changes.
5. **The hand-off line "a transition on `Color` runs on each child that inherits it".** With question 4, a child with no `Color` transition follows the value of its parent through the pass and holds no record. A child with its own `Color` transition runs its own tween.
6. **`StylePass.cs` changes.** The hand-offs of the three earlier features state that `StylePass.cs` did not change. This feature changes it: the merge of the sets, the `Track` call, and two flags. `Compare` and the dirty rule table do not change.
7. **The Confirmed Assumptions and the code.** No item conflicts with the code. A2 restyles many elements during a tween, and the keep rule stops a restart. A8 holds: the demos set `States` in code.

### Gaps (not in this feature)

1. A public API to read or stop a tween, and a transition event (out of scope).
2. Delays, keyframes, `transition: none`, `transition: all`, and the CSS default easing `ease`.
3. Transitions on layout lengths and on the font size (Q3).
4. The CSS reversing shortening factor (Conflicts 1).
5. A color step in another color space, for example Oklab.
6. A benchmark of a hover on a high ancestor with running tweens.
7. A transition in the HelloPanel sample. No input sets `:hover` until the input-routing epic.

### Hand-offs (for `context.md` at the end of phase 6)

**input-routing**
- A hover now fades when a rule declares a transition. Pass the real frame time to `Update(deltaTime)`. A `deltaTime` of 0 or less advances no tween.
- A fast pointer turns each tween from its visible value. A turn costs a restyle and no allocation.
- A hover on a high ancestor restyles the subtree (A2). The keep rule holds each running tween below it.

**framework and markup (Phase 3)**
- A mode change that swaps classes restyles the subtree. A swap that changes a target turns the tween. A swap that keeps the target keeps it.
- A new screen (an attach or `AddRoot`) starts no tween on its first style. Use a class change after the first `Update` for an intro fade.

**epic review**
- Candidates for tech debt: the reversing factor, the cost of the follow walk for a deep inheriting subtree, and the dictionary lookup for each restyled element while a tween runs.
- `UiContext.Transitions` (internal) gives `ActiveCount` and `Capacity` for a later debug overlay or benchmark.
