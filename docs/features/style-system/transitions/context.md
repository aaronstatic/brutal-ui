# style-system/transitions - Context & Decisions

**Last Updated:** 2026-10-22 05:18
**Current Phase:** Done (phases 1 to 6 and the cross-phase review)
**Status:** ✅ Ready for Review

---

## Quick Status

**What's Done:**
- ✅ The plan exists: `implementation.md` (6 phases, 37 tasks). The user accepted the recommended answer of each of the four open scope questions.
- ✅ `/start-feature` created `context.md`, `tasks.md`, and `inbox.md`.
- ✅ Phase 1, group 1 (tasks 1.1 to 1.6): the baseline, the slot helpers and `MergeFrom`, `TransitionCurves`, `TransitionRecord`, `TransitionPool`, and `Element.IsStyled`. The build has 0 warnings and 0 errors. The tests give 2044 passed and 0 failed. No code calls the pool yet.
- ✅ Phase 1, group 2 (tasks 1.7 to 1.11): the style pass merges the sets and calls `Track`, `UiContext` owns the pool and runs `UpdateTransitions` after the style pass, `TransitionPassTests` (12 tests) holds the demo, and the Markup gate runs a step time. The build has 0 warnings and 0 errors. The tests give 2056 passed and 0 failed.

- ✅ Phase 1 verified: the verifier (`phase-1-verify.md`) gave PASS and Demo: PASS. The orchestrator marked tasks 1.1 to 1.11 in `tasks.md`.
- ✅ Phase 2 (tasks 2.1 to 2.3): the remarks of each branch of `Track` and `TransitionRetargetTests` (8 tests). `Track` needed no code change. 2064 tests, 0 failed. The verifier (`phase-2-verify.md`) gave PASS and Demo: PASS.

- ✅ Phase 3 (tasks 3.1 to 3.4): the CSS Bezier curves with the Newton solver and the bisection fallback, the `deltaTime` rule of `Advance`, and `TransitionCurveTests` (28 tests). 2092 tests, 0 failed. The verifier (`phase-3-verify.md`) gave PASS and Demo: PASS.

- ✅ Phase 4 (tasks 4.1 to 4.5): `Element.InheritsColor`, the flag write in `Restyle`, the follow walk of `Color` in `TransitionPool.Advance`, and `TransitionInheritanceTests` (9 tests). 2101 tests, 0 failed. The verifier has not run yet.

- ✅ Phase 5 (tasks 5.1 to 5.5): the tree check of `Advance`, the drop of a record in `Track`, the join mark (`MarkSubtreeJoined`), and `TransitionLifetimeTests` (9 tests). 2110 tests, 0 failed. The verifier has not run yet.

- ✅ Phase 6, group 1 (tasks 6.1 to 6.5): the transition sheet of the 1000-element tree, the 2 new gate tests, the two plants, the facts line, and the smoke run. 2112 tests, 0 failed.
- ✅ Phase 6, group 2 (tasks 6.6 to 6.9): the design doc (sections 4, 7, and 14), the changelog, the Windows run line, the hand-offs, the lint, and the remarks of `StyleRule.cs`. 2112 tests, 0 failed.

- ✅ Phase 6 verified: the verifier (`phase-6-verify.md`) gave PASS and Demo: PASS. 37 of 37 tasks are done.

- ✅ The cross-phase review: 6 findings (1 HIGH, 5 LOW). Review groups 1 to 4 fixed all 6, with Decisions R2 and R3. 2121 tests pass. The verifier `review-verify.md` gave PASS, the six slice demos pass, and the smoke run passes.

**What's Next:**
- 📋 A person runs the items of "Needs Human Verification" (the 6 slice demos and the Windows run).
- 📋 A person checks Decisions R2 and R3 (the follow rule of a child with its own `Color` transition, and a bad duration as no transition).
- 📋 `/autorun-epic` commits the feature and runs `/update-epic` and `/update-master`.

## Phase 1 Baseline (task 1.1)

- Base commit: `11bd69af0f9faea2bd25e5a44afe87c92bb741db`.
- Build: `dotnet build brutal-ui.sln` gives `0 Warning(s)` and `0 Error(s)`.
- Tests: `dotnet test --solution brutal-ui.sln` gives total 2044, failed 0, succeeded 2044, skipped 0.
- Facts: `facts: tree elements 1000, bytes 1049520, bytes per element 1049.5`.
- After group 1, the facts line does not change (1049.5). The new `IsStyled` field fits in the padding of `Element`.

**Blockers:**
- None

---

## Key Files

### Core Implementation
- `src/BrutalUi.Core/Style/TransitionPool.cs` - the records, the index, `Track`, and `Advance` (new, internal)
- `src/BrutalUi.Core/Style/TransitionRecord.cs` - `TweenSlot`, `TweenSlots`, and `TransitionRecord` (new, internal)
- `src/BrutalUi.Core/Style/TransitionCurves.cs` - the four easing curves (new, internal)
- `src/BrutalUi.Core/Style/StyleTransitions.cs` - the internal slot helpers and `MergeFrom`
- `src/BrutalUi.Core/Style/StylePass.cs` - the merge of the matched sets, the `Track` call, and two flags
- `src/BrutalUi.Core/Tree/Element.cs` - `IsStyled`, `FollowsColor` (the name was `InheritsColor` before review group 1), and the join mark
- `src/BrutalUi.Core/Tree/UiContext.cs` - the pool, `UpdateTransitions`, and the frame order

### Related Files
- `docs/features/style-system/transitions/implementation.md` - the implementation plan
- `docs/features/style-system/stylesheet-model/context.md`, `docs/features/style-system/selector-matching/context.md`, and `docs/features/style-system/style-parser/context.md` - the hand-offs to this feature
- `tests/BrutalUi.Markup.Tests/Style/ParsedSheetAllocationTests.cs` - the gate with a transition list (task 1.10)

---

## Important Decisions

The plan holds Decisions 1 to 10 and the Confirmed Assumptions Q3, A2, and A8. This section records the decisions of the run.

### Decision R1: The user accepted the recommended answer of each open scope question
**Date:** 2026-10-22
**Context:** The draft plan lists four open scope questions. `/autorun-epic` passed the answers of the user.
**Decision:** Question 1: phase 1 uses the demo `AHoverFadesTheFillOfAButton`. Question 2: the transition sets of the matched rules merge for each slot, and the last matched rule in the cascade order wins. Question 3: the easings use the CSS cubic Bezier curves. Question 4: the transition pass writes the `Color` value into each inheriting descendant with no own `Color` tween, and it marks `Paint` only.
**Rationale:** Each answer is the recommended option of the plan, so the plan does not change.
**Impact:** None beyond the plan.

### Decision R2: A child with its own `Color` transition never follows its parent (CR-1)
**Date:** 2026-10-22
**Context:** CR-1 of `code-review.md` (HIGH): a child that inherits the color and declares a shorter `Color` transition of its own runs its own tween. When that tween ends, the follow walk writes the visible color of the parent over it, and the child jumps back (probe 13: red 150, then 50). The review offers two rules: (a) a child with its own `Color` transition never follows, or (b) the follow wins, and `Track` starts no own `Color` tween while an ancestor runs one. The run asks nothing, so the orchestrator takes the default that keeps the plan.
**Decision:** Rule (a). `StylePass.Restyle` writes the follow flag as "no rule and no inline value set `Color`, and the merged set holds no `Color` slot". The flag gets the name `FollowsColor`. The follow walk keeps its skip of a child with a running `Color` slot (CR-2 can keep a slot that the merged set does not hold).
**Rationale:** Decision 8 and F8 of the plan state that a child with its own `Color` transition runs its own tween. Rule (a) keeps that rule and each plan test, and the child ends on the same target as its parent, so no jump occurs (Risk 6). Rule (b) changes the plan test `AChildWithItsOwnColorTransitionRunsItsOwnTween` and needs a walk up the parents in `Track`.
**Impact:** The assert of `AChildTweenThatEndsFirstFollowsItsParent` changes to the own target of the child, and the test gets a new name. That test is new in phase 4 of this feature, so no test that existed before the feature changes. The report to `/autorun-epic` names the choice, so the user can change it.

### Decision R3: A transition with a bad duration acts as no transition (RG1-Q1)
**Date:** 2026-10-22
**Context:** Review group 1 found an open question (RG1-Q1): after R2, a rule with `transition: color 0s` sets `FollowsColor` to false, but `Track` starts no tween for 0 s. A label under a fading button then jumps to the target of the button.
**Decision:** A transition with a duration that `Track` rejects (0, a negative value, `NaN`, or an infinity) acts as no transition for the member. It starts no tween, it keeps no tween, and it does not stop the follow of `Color`. One internal helper holds the duration check for the start rule, the keep rule, and the flag.
**Rationale:** The duration rule of the plan says that such a duration writes the target at once, as if no transition exists. In CSS, a child with `transition: color 0s` still shows the animated value that it inherits.
**Impact:** Review group 2 applies the rule and adds `AChildWithAZeroColorTransitionStillFollows`. No plan test changes.

---

## Phase 1: Decisions

These decisions of group 1 are not in the brief.

### P1-1: `Advance` takes the roots now
**Decision:** The signature is `Advance(float deltaTime, ReadOnlySpan<Element> roots)`. Phase 1 does not read `roots`. Phase 5 reads it for the tree check.
**Rationale:** The brief permits the parameter. The plan shows `pool.Advance(dt, roots)`. Thus group 2 and phase 5 do not change the call.

### P1-2: The inline mask is a `ulong`, and `Track` applies it now
**Decision:** The parameter is `ulong inlineMask`, the value of `StyleProperties.SetMask`. Bit `n` is `StyleProperty` `n`. `Track` applies the inline rule of phase 2 now: an inline member never keeps or starts a tween, and it ends its running slot.
**Rationale:** The brief permits it. Without the rule, an inline write fades when a rule declares a transition for the member. Phase 2 adds the tests of the branch, and it does not change the code of the branch.

### P1-3: `Track` checks `IsStyled` before the lookup
**Decision:** Step 2 runs before step 1. A `Debug.Assert` checks that an element with `IsStyled` false holds no record. Phase 5 moves the lookup before the check and drops the record.
**Rationale:** In phase 1, no code sets `IsStyled` back to false, so an element with no style holds no record. The order saves one dictionary lookup on each first style.

### P1-4: One `Equal` for each slot, with no switch
**Decision:** `Equal(in float4, in float4)` compares the four floats with `float.Equals`. It has no switch over the slot.
**Rationale:** A color holds whole numbers from 0 to 255, so `float.Equals` is an exact compare for it. For the corners and the opacity, `float.Equals` is the compare of `StylePass.Compare` (`NaN` equals `NaN`, and 0 equals -0). `Read`, `Write`, and `Interpolate` switch over the member.

### P1-5: The premultiplied step uses the alpha in bytes
**Decision:** A color channel steps as `Lerp(c0 * a0, c1 * a1, t) / a`, with each alpha from 0 to 255. With a new alpha of 0 or less, the RGB channels take a straight linear step.
**Rationale:** This is the rule of the plan with the factor 1/255 taken out of both sides. The product of two bytes is exact in a float. The check values of the brief hold: #1E222CFF to #3060C0FF at 0.5 gives #274176FF, and #00000000 to #FF0000FF at 0.5 gives #FF000080. The turn of phase 2 from (39, 65, 118) to (30, 34, 44) at 0.5 gives (34.5, 49.5, 81), which rounds to (35, 50, 81).

### P1-6: `TransitionCurves.Evaluate` maps `NaN` to 0
**Decision:** The clamp uses `!(progress > 0f)`, so a `NaN` progress gives 0. A progress of 1 or more gives 1. The easing parameter has no effect until phase 3.
**Rationale:** Phase 3 adds the `deltaTime` rule. Until then, a `NaN` progress must not reach the value of a member.

### P1-7: `ToByte` maps `NaN` to 0 and clamps to 0 to 255
**Decision:** A channel of 0 or less (also `NaN`) gives 0. A channel of 255 or more gives 255. Else `(byte)(value + 0.5f)`.
**Rationale:** The brief gives the rounding and the clamp. The order of the checks also covers `NaN`.

### P1-8: `TransitionRecord.Element` is a non-nullable field
**Decision:** The field type is `Element`, not `Element?`. A released record holds `null` through `default(TransitionRecord)`, and the pool never reads a released record.
**Rationale:** The compiler gives no nullable warning for a struct field, and the reads need no `!`.

### P1-9: An end of an opacity tween sets the opacity
**Decision:** The end of an opacity tween writes `To` through the setter, also when the target is the unset value 1. Then the style holds a set 1 and not an unset value.
**Rationale:** The getter gives 1 in both states, and `StylePass.Compare` compares the getters. The next restyle writes the unset value again. No reader sees the difference.

### Blast radius of group 1

- `StyleTransitions.cs`: `SlotOf` goes from private to internal. The new members are internal. No public member changes, and the size stays 44 bytes.
- `Element.cs`: one internal auto property. No code reads or writes it yet. The size of an element does not change (facts line 1049.5).
- The three new files are internal. No code calls them yet, so no existing behavior changes.

### P1-10: `Restyle` merges a set only when it holds a slot
**Decision:** `Restyle` calls `MergeFrom` only for a matched rule with `!rule.Transitions.IsEmpty`. The merge sits in the same `if` as `ApplyTo`, so the merge order is the order of the properties (R1).
**Rationale:** The brief names a non-empty set. `MergeFrom` returns early for an empty set, but the check saves the call.

### P1-11: `Track` runs before the write, after `Compare`
**Decision:** `Restyle` calls `Compare` first, then `Track` (when the merged set holds a slot or `ActiveCount > 0`), then writes `current = next`, then sets `IsStyled`. `Compare` reads `current` and `next`, and `Track` changes neither, so the order of these two calls gives the same result.
**Rationale:** The brief puts `Track` before the write. `Compare` stays as it was.

### P1-12: The pool is a field initializer, and `UpdateTransitions` checks the count
**Decision:** `private readonly TransitionPool _transitions = new();` serves both constructors. `UpdateTransitions` calls `Advance` only when `ActiveCount > 0`, and it passes `_roots.AsSpan()` (the `ElementStorage` span, no `CollectionsMarshal`).
**Rationale:** The initializer cannot miss a constructor. A context with no tween pays one count check in each update.

### P1-13: The corner row of the theory uses four different corners
**Decision:** The `CornerRadius` row of `EachMemberFadesToItsTarget` goes from (0, 0, 0, 0) to (8, 4, 2, 6) and expects "4 2 1 3", then "8 4 2 6". The top-left corner is the 0 to 8 example of the brief.
**Rationale:** Four different values also prove the corner order of `Read` and `Write` in the pool.

### P1-14: The Markup gate has a `SettleTime` constant
**Decision:** `Settle()` runs `Context.Update(SettleTime)` with `SettleTime = 1f`. The test calls `Settle()` before each of the two calls of `AssertTheRulesMatch`. `Settle` does not count as a step, so `StepCount` stays 116.
**Rationale:** The brief gives `Update(1f)`. A named constant documents the rule (more than each duration).

### Blast radius of group 2

- `StylePass.Run`, `Visit`, and `Restyle` take a `TransitionPool`. `UiContext.UpdateStyles` is the only caller of `Run`. No public member changes.
- `UiContext.Update` runs the transition pass between the style pass and the layout pass. With no running tween, the pass is one count check, so each existing test keeps its result.
- Each restyle now sets `Element.IsStyled`. Only `TransitionPool.Track` reads it.
- While a tween runs, `Track` runs for each restyled element (one dictionary lookup for an element with no record).
- A restyle of an element with a running tween compares the interpolated value with the target, so `Compare` gives `Paint`. The transition pass marks `Paint` in the same update anyway, so the extra mark costs nothing.

### Phase 1 results (task 1.11)

- Build: `dotnet build brutal-ui.sln` gives `0 Warning(s)` and `0 Error(s)`.
- Tests: `dotnet test --solution brutal-ui.sln` gives total 2056, failed 0, succeeded 2056, skipped 0 (2044 plus 12 new tests).
- Demo (`--filter-method '*AHoverFadesTheFillOfAButton' --show-live-output on`) prints:
  - `t 0.0 #1E222CFF`
  - `t 0.1 #274176FF`
  - `t 0.2 #3060C0FF`
- `--filter-class '*ThousandElementAllocationTests'`: 10 of 10 passed.
- `--filter-method '*TheStylePassAllocatesNothingAfterWarmUp'`: 1 of 1 passed.
- `ParsedSheetAllocationTests` (with `AParsedSheetAllocatesNothingInUpdate`): 2 of 2 passed, 0 bytes in 100 steps with the step time 0.016 s.
- The public grep on the diff and on the three new files prints nothing. The reflection grep on `src/BrutalUi.Core/Style/*.cs` prints nothing.

### Follow-up work and migration risks of phase 1

- `src/BrutalUi.Core/Style/StyleRule.cs`: the remarks state "The style pass does not read them" about `Transitions`. The statement is now false. Neither brief of phase 1 listed the file. A later phase (or phase 6, the docs) must change the remarks.
- Tasks 1.1 to 1.11 are not marked in `tasks.md`.
- An element that leaves its tree with a running tween keeps its record, and `Advance` still writes into it. Phase 5 adds the tree check.
- A `Color` tween on a parent does not reach the children: they inherit the target at the restyle. Phase 4 adds the follow walk.
- `Advance` with a `deltaTime` of 0 or less, or `NaN`, had no rule in phase 1. Phase 3 adds it (P3-3).

### Phase 2: Decisions and results (group 1)

- P2-1: The check of `Track` against Decision 4 found no gap. The inline branch of phase 1 holds, and `Advance` with a `deltaTime` of 0 writes the interpolated value at progress 0 (the visible value), so `leave 0.0` shows the visible value. Phase 2 changed only the remarks of `Track` (each branch, the turn, the merged set of the new style, and the inline rule).
- P2-2: `ARestyleWithTheSameTargetKeepsTheTween` sets `States = Focus` on the root. The button then holds `#3060C0FF` after the second step, so the elapsed time stayed. A restart would leave the button at the midpoint.
- P2-3: `ANewTargetTakesTheFullDuration` uses an `Active` rule (`#C82828FF`) with no transition of its own. The turn goes from (39, 65, 118) to (200, 40, 40). It gives `#78354FFF` after 0.1 s and the target after 0.2 s.
- P2-4: `TheTransitionOfTheNewStyleApplies` uses 0.4 s in the base rule and 0.1 s in the hover rule. The hover in ends after one step of 0.1 s. The hover out ends after four steps.
- Build: `dotnet build brutal-ui.sln` gives `0 Warning(s)` and `0 Error(s)`.
- Tests: `dotnet test --solution brutal-ui.sln` gives total 2064, failed 0, succeeded 2064, skipped 0 (2056 plus 8 new tests).
- Demo (`--filter-class '*TransitionRetargetTests' --show-live-output on`) prints:
  - `hover 0.1 #274176FF`
  - `leave 0.0 #274176FF`
  - `leave 0.1 #233251FF`
  - `leave 0.2 #1E222CFF`
- Files changed: `src/BrutalUi.Core/Style/TransitionPool.cs` (remarks only), `tests/BrutalUi.Core.Tests/Style/TransitionRetargetTests.cs` (new), and this file.
- `tasks.md` is not marked: the orchestrator marks tasks 2.1 to 2.3.

### Phase 3: Decisions and results (group 1)

- P3-1: `Evaluate` uses an `if` chain, not a switch. `Linear` returns the progress, `EaseIn` and `EaseOut` call the solver, and the last line is `EaseInOut` with no fallback branch (DS6.3). A switch statement over the enum would need a `return` or a `throw` after it, so the chain is shorter.
- P3-2: The solver starts Newton at `t = progress`. It stops at an x error below 1e-6, and it breaks to bisection when the slope is below 1e-6 or a step leaves 0 to 1. Bisection starts at `t = 0.5` and uses the same tolerance.
- P3-3: `Advance` computes `step = deltaTime > 0f ? deltaTime : 0f` once, then adds `step` to each elapsed time. `NaN`, 0, and a negative value add 0. Infinity gives an infinite elapsed time and ends each tween. A `deltaTime` of 0 still writes the visible value of each running slot. The `Track` start guard (`float.IsFinite(duration) && duration > 0f`) already rejects 0, -0, a negative value, `NaN`, and the infinities, so `Track` needed no change.
- P3-4: The bad-duration theory uses `MemberData` (`TheoryData<float>`), not `InlineData`. The xUnit analyzer (xUnit1025) treats the rows `0f` and `-0f` of `InlineData` as duplicates and fails the build. The control row (`float.Epsilon`) is the fact `AnEpsilonDurationStartsATween`.
- P3-5: The delta time theory has a fourth row, `NegativeInfinity`, beyond the three rows of the brief.
- P3-6: `EachCurveRisesAndStaysInTheRange` also asserts that the last sample (progress 1) is exactly 1. `AProgressOutsideTheRangeIsClamped` also covers `NaN` and positive infinity.
- Build: `dotnet build brutal-ui.sln` gives `0 Warning(s)` and `0 Error(s)`.
- Tests: `dotnet test --solution brutal-ui.sln` gives total 2092, failed 0, succeeded 2092, skipped 0 (2064 plus 28 new tests).
- Demo (`--filter-method '*EachEasingShapesTheFade' --show-live-output on`) prints:
  - `linear 0.500`
  - `ease-in 0.315`
  - `ease-out 0.685`
  - `ease-in-out 0.500`
- The public grep on `TransitionCurves.cs` and `TransitionPool.cs` prints nothing. The reflection grep on `src/BrutalUi.Core/Style/*.cs` prints nothing.
- Files changed: `src/BrutalUi.Core/Style/TransitionCurves.cs`, `src/BrutalUi.Core/Style/TransitionPool.cs` (`Advance` and the remarks), `src/BrutalUi.Core/Tree/UiContext.cs` (the remarks of `Update`), `tests/BrutalUi.Core.Tests/Style/TransitionCurveTests.cs` (new), and this file.
- `tasks.md` is not marked: the orchestrator marks tasks 3.1 to 3.4.

### Phase 4: Decisions and results (group 1)

- P4-1: `Element.InheritsColor` is an internal auto property, next to `IsStyled`. A new element holds `false` until its first style. `Restyle` writes it after `IsStyled`, with the condition of `Inherit`: `(applied & (1UL << (int)StyleProperty.Color)) == 0`. `applied` holds the inline mask, so an inline color also gives `false`. Review group 1 renames the flag to `FollowsColor` and adds the merged set to the condition (Decision R2). Review group 2 limits that part to a slot with a valid duration (Decision R3).
- P4-2: `Advance` runs in two loops. Loop 1 advances and writes each running slot, as before, and marks the record with a follow bit when its `Color` slot ran at the start of the step. Loop 2 runs `FollowColor` for each marked record and then releases each record with no running slot. Rationale: with one loop, the result depends on the order of the records. Example: a child with its own `Color` tween that ends in this step, under a parent with a longer tween. When the pool visits the parent first, the walk skips the child (its slot still runs), and the child shows its own target for one frame. When the pool visits the child first, the child follows the parent. Two loops give the second result in both orders. `AChildTweenThatEndsFirstFollowsItsParent` arranges the first order (the chip rents record 0 through a fill tween, so the pool visits the button first) and asserts the follow. Decision R2 replaces the follow of that chip: the test is now `AChildTweenThatEndsFirstKeepsItsOwnTarget` (RG1-3). The two loops stay.
- P4-3 (replaced by RG2-1 in review group 2): The follow mark was bit 7 of `TransitionRecord.Mask` (`private const int FollowBit = 1 << 7`). The slot bits are bits 0 to 4. Loop 2 clears bit 7 before `Advance` returns, so outside `Advance` the mask holds slot bits only, as the remarks of `TransitionRecord.cs` state. A `Debug.Assert` checks that `AllSlots` and `FollowBit` share no bit. Rationale: `TransitionRecord.cs` is not in the files of the brief, and a parallel flag array must move with each release. Phase 5 or 6 can move the mark into a named field of the record.
- P4-4: The walk runs after each step of a running `Color` slot, also when the bytes of the color did not change, and also at the last step. Rationale: the style pass can write the target into a child in the same update (a text change of the parent marks each child with `Style`), and the walk must write the visible value over it. The walk reads `element.StyleStorage.Color` in loop 2, not a copy from loop 1, so a walk that writes into a record element first gives the same result.
- P4-5: `FollowColor` visits each inheriting child also when the color of the child did not change, so a grandchild that the style pass restyled to the target in this update still gets the visible value. The cost is one walk of the inheriting part of the subtree for each running `Color` slot in each update, plus one dictionary lookup for each visited child. `HasRunningColor` is the lookup in `_index`.
- P4-6: `s_colorSlot` is a `static readonly int` from `StyleTransitions.SlotOf(StyleProperty.Color)`, so the slot order stays in one place.
- P4-7: The tests hold three facts beyond the brief: `AnInlineColorStopsTheWalk`, `AChildWithAFillTweenStillFollows` (the tier note: a record with no running `Color` slot still follows), and `AChildTweenThatEndsFirstFollowsItsParent` (P4-2).
- Build: `dotnet build brutal-ui.sln` gives `0 Warning(s)` and `0 Error(s)`.
- Tests: `dotnet test --solution brutal-ui.sln` gives total 2101, failed 0, succeeded 2101, skipped 0 (2092 plus 9 new tests). `TextInheritanceTests` gives 13 of 13 with no change.
- Demo (`--filter-method '*TheTextOfALabelFadesWithItsButton' --show-live-output on`) prints:
  - `button #FFFFFFFF label #FFFFFFFF`
  - `button #FFE680FF label #FFE680FF`
  - `button #FFCC00FF label #FFCC00FF`
- `--filter-class '*ThousandElementAllocationTests'`: 10 of 10 passed. `--filter-method '*TheStylePassAllocatesNothingAfterWarmUp'`: 1 of 1 passed. `ParsedSheetAllocationTests`: 2 of 2 passed.
- Facts: `facts: tree elements 1000, bytes 1049648, bytes per element 1049.6`. The value was 1049.5. The new `bool` fits in the padding of `Element` (a new field slot gives 8 bytes for each element). The 128 extra bytes in the total are not for each element.
- The public grep on the diff and on `TransitionPool.cs` prints nothing. The reflection grep on `src/BrutalUi.Core/Style/*.cs` prints nothing.
- Files changed: `src/BrutalUi.Core/Tree/Element.cs`, `src/BrutalUi.Core/Style/StylePass.cs`, `src/BrutalUi.Core/Style/TransitionPool.cs`, `tests/BrutalUi.Core.Tests/Style/TransitionInheritanceTests.cs` (new), and this file.

### Blast radius of phase 4

- `StylePass.Restyle` writes one more flag for each restyled element. No other code reads it, except `TransitionPool.FollowColor`.
- `TransitionPool.Advance` releases a record in loop 2 and not in loop 1. The release rule and the result of each tween do not change. A pool with no `Color` slot pays one more loop over the records and no walk.
- A running `Color` tween now writes into the inheriting descendants and marks `Paint` on each changed descendant. The draw pass paints them again. No `Style` bit and no `Layout` bit come from the walk.

### Follow-up work and migration risks of phase 4

- An element that leaves the tree with a running `Color` tween still walks its subtree until phase 5 adds the tree check.
- Resolved by Decision R2 and Decision R3 (review groups 1 and 2). The phase 4 rule let a child with its own `Color` transition follow its parent after the end of its own tween, and the color jumped back (CR-1, probe 13). Now a child with its own `Color` transition (a valid duration) never follows. It runs its own tween and ends on the same target as its parent, so no jump occurs. A child with a `Color` transition of 0 s (or another duration that is not valid) follows. `AChildWithAShorterOwnColorTweenNeverJumpsBack` and `AChildWithAZeroColorTransitionStillFollows` cover the two rules.
- Resolved by RG2-1: the follow mark is now the field `TransitionRecord.Follow`, not a bit of `Mask`. A change of `StyleTransitions.SlotCount` does not affect it.
- `tasks.md` is not marked: the orchestrator marks tasks 4.1 to 4.5.

### Phase 5: Decisions and results (group 1)

- P5-1 (review group 2 moves the follow mark into `TransitionRecord.Follow`, so the text about bit 7 and the `Debug.Assert` is history): The tree check is a separate loop 0 in `Advance`, before loop 1. Rationale: at loop 0, no record holds the follow bit, so a release (a swap with the last record) moves no bit 7. A check inside loop 1 can move a record with bit 7 into a lower index. Loop 2 clears it there, but the separate loop needs no such argument. A `Debug.Assert` in loop 0 checks that no record holds the follow bit. Loop 0 visits from the last record to the first, as loop 2 does. The cost is one walk to the top and one compare with each root, for each record and each update with a running tween.
- P5-2: `IsInTree` is a private static method of `TransitionPool`. It walks `Element.Parent` (public) to the top element, then compares with `ReferenceEquals`. It reads no `IsRoot` flag, because the flag does not name the context: a root of another context also has `IsRoot` true.
- P5-3: `Track` releases the record at step 2 and returns. The style pass then writes the target. The record of an element that joins with no update between the leave and the join thus drops in `Track`, and not in loop 0 of `Advance`, because the element is in the tree again at the next update.
- P5-4: `MarkSubtreeJoinedCore` is a copy of `MarkSubtreeStyleCore` with one more write (`IsStyled = false`). A shared core with a `bool` parameter was the other option. Two methods keep the hot path of a state change free of the extra branch, and `MarkSubtreeStyle` does not change.
- P5-5: The tests hold two facts beyond the brief. `AnElementThatBecomesARootStartsNoTween` covers `AddRoot` (task 5.3): `AddRoot` takes a hovered panel and its hovered child from the parent, and the next `Update(0f)` gives the target with 0 records. `AStateChangeAfterAJoinStillFades` checks that a join does not stop a later fade, because the restyle sets `IsStyled` again and a state change keeps it.
- P5-6: `ARemovedAncestorEndsTheTweensOfItsSubtree` and `AMoveIntoAnotherContextStartsNoTween` call `UpdateTransitions` alone after the leave, so they read the paint bit before the clear pass. Both assert no `Paint` bit on the dropped element ("no mark"). The second context uses a sheet with another hover color (#C80000), so a write of the first context (#3060C0) fails the test.
- P5-7: `ThePoolReusesItsRecords` runs a round as a hover in and a hover out on 40 panels. Each change runs three `Update(0.016f)` steps and one `Update(1f)`. The measured region holds no assert and no string. The capacity is 64 after the warm-up, and it does not change.
- Build: `dotnet build brutal-ui.sln` gives `0 Warning(s)` and `0 Error(s)`.
- Tests: `dotnet test --solution brutal-ui.sln` gives total 2110, failed 0, succeeded 2110, skipped 0 (2101 plus 9 new tests).
- Demo (`--filter-method '*ARemovedPanelEndsItsTweens' --show-live-output on`) prints:
  - `active 1 panel #274176FF`
  - `active 0 panel #274176FF`
  - `active 0 panel #3060C0FF`
- `ThePoolReusesItsRecords`: 1 of 1 passed, 0 bytes. `ThousandElementAllocationTests`: 10 of 10 passed. `TheStylePassAllocatesNothingAfterWarmUp`: 1 of 1 passed. `ParsedSheetAllocationTests`: 2 of 2 passed.
- With no change: `ElementListTests` 30 of 30, `TreeGuardTests` 26 of 26, `MultiSheetTests` 11 of 11, `SubtreeMarkTests` 9 of 9, `TransitionPassTests` 12 of 12, `TransitionRetargetTests` 8 of 8, `TransitionCurveTests` 28 of 28, and `TransitionInheritanceTests` 9 of 9.
- The public grep on the diff and on the three new source files prints nothing. The reflection grep on `src/BrutalUi.Core/Style/*.cs` prints nothing.
- Files changed: `src/BrutalUi.Core/Style/TransitionPool.cs`, `src/BrutalUi.Core/Tree/Element.cs`, `src/BrutalUi.Core/Tree/UiContext.cs`, `tests/BrutalUi.Core.Tests/Style/TransitionLifetimeTests.cs` (new), and this file.

### Blast radius of phase 5

- `Element.AttachToParent` (the path of `ElementList.Add` and `ElementList.Insert`) now also sets `IsStyled` to false on the subtree. The dirty bits do not change, so the tree tests and the subtree mark tests pass with no change. Only `TransitionPool.Track` reads `IsStyled`.
- `UiContext.AddRoot` calls `MarkSubtreeJoined`. The dirty bits are the same as with `MarkSubtreeStyle`.
- `ElementList.Move` does not attach, so a move inside one child list keeps each running tween. A remove and an add end it.
- `TransitionPool.Advance` runs loop 0 in each update with a running tween. With no running tween, `UpdateTransitions` does not call `Advance`.

### Follow-up work and migration risks of phase 5

- The cost of loop 0 is the depth of the element times the count of the records, plus the count of the roots for each record. A context with many roots and many tweens pays more. A context reference on `Element` would make the check O(1), but it is a larger change.
- A tween of an element that a game removes and adds back in the same child list (a reorder with `Remove` and `Insert`) ends at once. `ElementList.Move` keeps the tween. The docs of phase 6 can name this.
- An element outside each tree keeps the style that the last tween step wrote (for example, the midpoint). The first restyle after a join writes the target.
- `tasks.md` is not marked: the orchestrator marks tasks 5.1 to 5.5.

### Phase 6: Decisions and results (group 1)

- P6-1: The three selectors of `BuildTransitionSheet` exist in the tree: each grid slot has the class `slot`, the style step puts the sidebar in the class `muted`, and the header has the name `header` (`HeaderName`). No rule needed another class.
- P6-2: The plan states "ActiveCount is 3 or more" after one style step. The value is 2. The pool holds one record for each element, not one for each tween. The first style step starts three tweens in two records: the fill and the border color of grid slot 0 in one record, and the text color of the sidebar in the second record. The first style step renames the header, so the `#header` rule does not match, and the header border changes at once. `TheTransitionStepRunsTweens` asserts `ActiveCount == 2`, and it proves the third tween (the slot border) through its value after the 9 more steps.
- P6-3: "After 9 more steps" is the 8 steps of the rotation (kinds 0 to 7 of the second round) plus one `Update(0.016f)` with no change. The ninth step of the rotation is the reverse style step, and it starts a new fill tween back to `SlotColor`, so the fill does not hold its target after it. The total time since the start of the tweens is 0.16 s. The fill (0.1 s) and the text color (0.1 s) hold their targets, `ActiveCount` is 1, and each RGB channel of the slot border lies strictly between `SlotBorderColor` and `SlotHoverBorderColor` (0.5 s `EaseInOut`). The test asserts `scene.StepCount % KindCount == StyleKind` before the extra update, so the extra update does not replace a style step.
- P6-4: After one style step, the test asserts the fill, the sidebar color, and the label color strictly between the two values on each RGB channel, and the alpha equal to 255 in all three. The alpha of both values is 255, so no alpha lies strictly between. The test also asserts that the sidebar item (the parent of the label) holds the color of the sidebar.
- P6-5: The plan says "the name toggle turns the header border tween". The tween does not turn. The rename back to `header` starts a tween of 0.3 s from the unset border color to `HeaderBorderColor`. The next rename comes 0.144 s later, the rule then does not match, and the style step ends the tween at once. The doc comment of `BuildTransitionSheet` states this. The hover toggle is the one that turns a tween (the slot border).
- P6-6: `UpdateWithRunningTransitionsAllocatesNothing` reads `Capacity` after the release and before the measure, and again after the measure. It also asserts `ActiveCount >= 1` after the measure, so a tween ran in the measured body (the slot border turns at each style step and never ends). The warm-up is two calls of `Run(scene, KindCount * 2)`, so four full rounds of the toggles.
- P6-7: The gate measured 0 bytes at the first run. No change in `src/` was necessary, so the copy of task 6.3 is the file of phase 5.
- P6-8: Each restore copied `TransitionPool.cs.orig` from the scratch folder over the file with `cp`. No git command ran.

Plant 1 (task 6.3): `GC.KeepAlive(new object());` as the first line inside the body of loop 1 of `Advance`.
- `dotnet test --solution brutal-ui.sln > <scratch>/plant-advance.log 2>&1; echo "exit=$?"` printed `exit=2`. Total 2112, failed 3, succeeded 2109.
- The failed list, word for word:
  - `failed BrutalUi.Markup.Tests.Style.ParsedSheetAllocationTests.AParsedSheetAllocatesNothingInUpdate (379ms)`
  - `failed BrutalUi.Core.Tests.TransitionLifetimeTests.ThePoolReusesItsRecords (19ms)`
  - `failed BrutalUi.Core.Tests.ThousandElementAllocationTests.UpdateWithRunningTransitionsAllocatesNothing (178ms)`
- The restore and `diff <scratch>/TransitionPool.cs.orig src/BrutalUi.Core/Style/TransitionPool.cs` printed nothing (`diff=0`).

Plant 2 (task 6.4): `GC.KeepAlive(new object());` as the first line of `Track`.
- `dotnet test --solution brutal-ui.sln > <scratch>/plant-track.log 2>&1; echo "exit=$?"` printed `exit=2`. Total 2112, failed 3, succeeded 2109.
- The failed list, word for word:
  - `failed BrutalUi.Markup.Tests.Style.ParsedSheetAllocationTests.AParsedSheetAllocatesNothingInUpdate (517ms)`
  - `failed BrutalUi.Core.Tests.TransitionLifetimeTests.ThePoolReusesItsRecords (16ms)`
  - `failed BrutalUi.Core.Tests.ThousandElementAllocationTests.UpdateWithRunningTransitionsAllocatesNothing (203ms)`
- The restore and `diff` printed nothing (`diff=0`).
- Both plants also fail the gate of phase 5 (`ThePoolReusesItsRecords`) and the Markup gate, because their measured bodies run tweens. The gates with no transition sheet (`UpdateAllocatesNothingAfterWarmUp`, `DrawAllocatesNothingAfterWarmUp`, and `TheStylePassAllocatesNothingAfterWarmUp`) pass with both plants, because their measured bodies call neither `Track` nor `Advance`.

Results (task 6.5, after the restore):
- Build: `dotnet build brutal-ui.sln` gives `0 Warning(s)` and `0 Error(s)`.
- Tests: `dotnet test --solution brutal-ui.sln` gives total 2112, failed 0, succeeded 2112, skipped 0 (2110 plus 2 new tests).
- `--filter-class '*ThousandElementAllocationTests'`: total 12, failed 0, succeeded 12.
- Facts: `facts: tree elements 1000, bytes 1049648, bytes per element 1049.6`. The value is the value of phase 4 (1049.6), and the base was 1049.5.
- `scripts/test-linux.sh; echo "exit=$?"` printed `test-linux: PASS build, test, gpu, smoke (120 frames) in 16 s` and `exit=0`.
- `artifacts/test-linux/smoke.log`:
  - `HelloPanel: 120 frames rendered.`
  - `HelloPanel: last frame: draw commands 2, vertices 128.`
  - `HelloPanel: bytes after warm-up: Update and Draw 0, frame 22464, in 117 frames.`
- Files changed: `tests/BrutalUi.Core.Tests/Shared/ThousandElementTree.cs`, `tests/BrutalUi.Core.Tests/ThousandElementAllocationTests.cs`, and this file. `src/BrutalUi.Core/Style/TransitionPool.cs` is the same as before the plants.
- `tasks.md` is not marked: the orchestrator marks tasks 6.1 to 6.5.

### Phase 6: Decisions and results (group 2)

- P6-9: Section 7 of `docs/technical-design.md` states the real code, not the first form of the plan: the three loops of `Advance` (loop 0 the tree check, loop 1 the step and the write, loop 2 the follow walks and the releases), the follow mark (then in bit 7 of `TransitionRecord.Mask`, now in `TransitionRecord.Follow` after RG2-1), and the call of `Advance` only when a tween runs.
- P6-10: The cost paragraph of section 7 (the style model) now gives the facts line of task 6.5: 1049.6 bytes for each element (`bytes 1049648`). It names the base 1049.5, and it states that the two flags fit in the padding of `Element`.
- P6-11: Section 7 adds two design decision lines: the pool and the follow walk. The turn rule and its difference from CSS live in the `### Transitions` text ("The turn"), not in a decision line.
- P6-12: Section 14 adds one bullet for the five transition test classes (66 tests). It updates the count of `ThousandElementAllocationTests` from 10 to 12, and it adds the 2 new tests, the transition sheet, and the results of both plants. The bullet of `ParsedSheetAllocationTests` gets the step time and the settle step.
- P6-13: The changelog line under `### Changed` also tells a test author to pass a time of at least the duration when the test reads a target on a sheet with transitions.
- P6-14: The remarks of `StyleRule.cs` now state the merge for each slot (the last matched rule in the cascade order wins) and the write of the transition pass over the target. The change is in `///` lines only.
- P6-15: The lint of the plan gave 1.93 for each 100 words, below 2.5. Thus group 2 did not edit `implementation.md`.

### Review Group 1: Decisions

- RG1-1 (CR-1): `Restyle` writes `Element.FollowsColor` (the new name of `InheritsColor`) with the expression of the brief. A color slot in the merged set gives `false`. The rename touches `Element.cs`, `StylePass.cs`, `TransitionPool.cs`, and `TransitionInheritanceTests.cs` only. No markup, framework, or ImGui file names the flag. The doc text of section 7 and the phase 4 lines of this file still say `InheritsColor`. Review group 2 updates them with CR-6.
- RG1-2 (CR-1): `AChildWithItsOwnColorTransitionRunsItsOwnTween` keeps each color assert. Its flag assert changes from `Assert.True(icon.InheritsColor)` to `Assert.False(icon.FollowsColor)`, because the icon holds its own color transition. That is the definition of the new flag, not a new rule. The test also asserts that the glyph follows.
- RG1-3 (CR-1): `AChildTweenThatEndsFirstFollowsItsParent` is now `AChildTweenThatEndsFirstKeepsItsOwnTarget`. The chip and the label hold `#FFCC00FF` after the step, and the chip asserts `FollowsColor` false.
- RG1-4 (CR-1): The new theory `AChildWithAShorterOwnColorTweenNeverJumpsBack` uses the colors of probe 13: the button goes from red 0 to red 200 in 0.4 s, and the chip has its own color transition of 0.1 s. It runs 20 steps of 0.025 s and asserts that the red channel of the chip and of the label never falls. Case `true` sets a state on the root at step 2 (during the tween of the chip, the keep branch) and clears it at step 8 (after the tween, the second jump of probe 13). On the old rule, both cases fail at step 3 with the trace `13/50/50 25/100/100 38/150/150 50/50/50` (button/chip/label). A scratch copy outside the repository showed it.
- RG1-5 (CR-2): The keep branch of `Track` also needs `merged.IsSlotSet(slot)`. A running slot that the new style does not hold ends in the "no change" branch or the "else" branch. `ASameTargetWithNoTransitionEndsTheTween` fails on the old rule (`#274176FF` in place of `#3060C0FF`).
- RG1-6: With CR-2, a running `Color` slot always comes with `FollowsColor` false. `Track` starts and keeps a slot only when the merged set holds it, and `Restyle` writes the flag from the same merged set. Thus the `HasRunningColor` skip of `FollowColor` is now a guard. The brief keeps it, and the remarks of `FollowColor` call it a guard.
- RG1-7: The remarks of loop 2 of `Advance` said that a descendant with a color tween that ends in the step takes the color of its ancestor. Decision R2 makes that false, so the remarks now state the new rule.
- Results: 2115 tests (2112 plus 3), 0 failed. The build gives 0 warnings and 0 errors in Debug and in Release. The 0-byte gates pass: `ThousandElementAllocationTests` (12), `ThePoolReusesItsRecords`, `TheTransitionStepRunsTweens`, and `ParsedSheetAllocationTests` (2). The `git diff` check of the brief prints no new public member. A Release build refreshed the stale XML doc files, so `grep -rn InheritsColor src tests` prints nothing.

**Open question RG1-Q1 (a duration of 0).** The parser accepts `transition: color 0s`. The merged set then holds the color slot, so `FollowsColor` is `false`, but `Track` starts no tween for a duration of 0. A label with `transition: color 0s` under a fading button thus changes to the target of the button at once, and it does not follow the fade. Before this group, the label followed. Rule (a) and the brief give this result, so the group keeps it. A possible fix: write the flag with "the merged set holds the slot with a finite duration of more than 0" (the start condition of `Track`), and put the same condition in the keep branch, so a running color slot still comes with `FollowsColor` false. The user decides. **Resolved:** Decision R3 takes the possible fix, and review group 2 applies it (RG2-2).

### Review Group 2: Decisions

- RG2-1 (CR-5): The follow mark is the field `internal bool Follow` of `TransitionRecord`, after `Mask`. Loop 1 of `Advance` writes `record.Follow = (record.Mask & colorBit) != 0` before the step of the slots, because the last step clears the slot bit. Loop 2 reads the field, clears it, and runs the walk. `FollowBit`, the two `Debug.Assert` lines, the `unchecked` cast, and `using System.Diagnostics` are gone. `Unsafe.SizeOf<TransitionRecord>()` is 232 bytes before and after the change (`TweenSlot` 44, `TweenSlots` 220). A scratch console program outside the repository read the three sizes through reflection from the Debug build of the tests. The assignment in loop 1 writes `false` also for a record with no color slot. A released record holds `default`, and `Rent` writes `default`, so no stale mark can reach loop 2.
- RG2-2 (RG1-Q1, Decision R3): The helper is `internal static bool TransitionPool.CanRun(in StyleTransitions merged, int slot)`. It returns `true` when the merged set holds the slot and the duration is finite and more than 0. The keep branch and the start branch of `Track` and the `FollowsColor` write of `StylePass.Restyle` call it. The start branch keeps the finite check of the visible value and of the target. The helper lives in `TransitionPool.cs`, because `StyleTransitions.cs` is not a file of the brief. A mutation of the flag write back to `merged.IsSlotSet` fails `AChildWithAZeroColorTransitionStillFollows` only (1 of 1304 Core tests).
- RG2-3 (Decision R3, the keep rule): A running slot that a restyle sends to a rule with the same target and a duration of 0 now ends at once, the same as a rule with no transition (CR-2). No test of this group covers this path, because `TransitionRetargetTests.cs` is not a file of the brief. A later group can add a row with a duration of 0 to `ASameTargetWithNoTransitionEndsTheTween`.
- RG2-4 (CR-6): The remarks of `Track` now state the `Remove` and `Add` rule and the `ElementList.Move` rule. The phase 4 risk lines and the epic review lines state Decision R2 and Decision R3. The phase 4, phase 5, and phase 6 decision lines about bit 7 stay as history, with a note.
- RG2-5 (CR-6): The cost line of section 7 gives two values, 0.36 ms (scene A) and 0.39 ms (scene B). The review states 0.36 ms for "the worst case", but scene B with the toggle is 527.8 us, 0.39 ms above the baseline of 135.9 us.
- RG2-6: Section 14 of `docs/technical-design.md` gave stale test counts after review group 1. It now gives 70 transition tests: `TransitionRetargetTests` 9 and `TransitionInheritanceTests` 12.
- Results: 2116 tests (2115 plus 1), 0 failed. The build gives 0 warnings and 0 errors. The 0-byte gates pass: `ThousandElementAllocationTests` (12), `ThePoolReusesItsRecords`, and `ParsedSheetAllocationTests` (2). The lint gives 1.51 for `docs/technical-design.md` and 2.16 for this file, for each 100 words. No new public member.

### Review Group 3: Decisions

- RG3-1 (CR-3): `SolveParameter` and `Sample` of `TransitionCurves` are now `internal`. The class stays `internal static`, so no public member is new. The test project reads them through `InternalsVisibleTo`.
- RG3-2 (CR-3): The theory `TheSolverFindsTheParameterForEachProgress` scans the progress values `i / 100000` for i from 1 to 99999 for each of the three curves. It asserts that the parameter is in 0 to 1 and that the x value of the curve at the parameter differs from the progress by less than 1e-6. The test computes the x value with `Sample`. It allocates nothing that a gate measures.
- RG3-3 (CR-3): The bound is 1e-6, the tolerance of the solver. The correct solver meets it for all 299997 values, so no wider bound was needed.
- RG3-4 (CR-3): The check without the bisection used a scratch change of `TransitionCurves.cs`: a `return t` in front of the bisection (a copy of the file was in the scratch folder). The test failed for `EaseIn` (x error 4.17e-6 at 0.99999) and for `EaseOut` (4.14e-6 at 0.00001), and it passed for `EaseInOut`, as the review predicted (0 bisection calls). The restored file equals its copy (`diff` printed nothing). A first try with an unreachable `return` failed to build (warning CS0162 as error), so the second try used a guard on `high`.
- Results: 2119 tests (2116 plus 3 theory rows), 0 failed. The build gives 0 warnings and 0 errors.

### Review Group 4: Decisions

- RG4-1 (CR-4): `AMoveInsideOneChildListKeepsTheTween` in `TransitionLifetimeTests` uses a root with the panel at index 0 and one more child. It moves the panel with `Move(0, 1)`. It also asserts the new index of the panel, so the test proves that the move happened.
- RG4-2 (RG2-6, Decision R3): The 0 s row is a sibling test, `ASameTargetWithAZeroDurationEndsTheTween` in `TransitionRetargetTests`. The `instant` class holds the hover target and a transition of 0 s. The class swap ends the running tween at once. The member holds the target and `ActiveCount` is 0, also after one more step. The sibling leaves `ASameTargetWithNoTransitionEndsTheTween` unchanged.

---

## Hand-offs

Group 2 of phase 6 checked each line against the code of `TransitionPool.cs`, `StylePass.cs`, `Element.cs`, and `UiContext.cs`.

**input-routing**
- A hover now fades when a rule declares a transition. Pass the real frame time to `Update(deltaTime)`. A `deltaTime` of 0 or less, also `NaN`, advances no tween. An infinite `deltaTime` ends each tween at its target.
- A fast pointer turns each tween from its visible value, with the full duration. A turn costs a restyle and no allocation after warm-up.
- A hover on a high ancestor restyles the subtree (A2). The keep rule holds each running tween below it.

**framework and markup (Phase 3)**
- A mode change that swaps classes restyles the subtree. A swap that changes a target turns the tween. A swap that keeps the target keeps it.
- A new screen (an attach or `AddRoot`) starts no tween on its first style. Use a class change after the first `Update` for an intro fade.
- A `Remove` and then an `Insert` (or an `Add`) end each tween of the moved subtree, and the element gets its target at once. `ElementList.Move` keeps the tweens. Use `Move` for a reorder inside one child list.
- An element outside each tree keeps the last value of its tween (for example, the midpoint) until a join restyles it to the target. A screen that a game keeps outside the tree thus holds a stale color in `Element.Style` until it joins again.

**epic review**
- Candidates for tech debt: the reversing factor, the cost of the follow walk for a deep inheriting subtree, and the dictionary lookup for each restyled element while a tween runs.
- More candidates: the tree check of loop 0 costs the depth of the element plus the count of the roots, for each record in each update with a running tween. A context reference on `Element` would make it constant.
- A child with its own `Color` transition never follows the color tween of its parent (Decision R2). It runs its own tween with its own duration, and it ends on the same target as its parent, so its color does not jump back. Thus a theme rule such as `Label { transition: color 0.1s }` under a button with a slower fade gives a label that leads the button. A label with no `Color` transition, or with a duration of 0 s, follows the button (Decision R3).
- The follow mark of `Advance` lives in the field `TransitionRecord.Follow`. The record stays at 232 bytes.
- `UiContext.Transitions` (internal) gives `ActiveCount` and `Capacity` for a later debug overlay or benchmark.

---

## Gotchas & Learnings

- The acceptance grep `git diff -U0 11bd69af... -- src/BrutalUi.Core | grep -E '^\+\s*public '` does not see an untracked file. Group 1 checked the three new files with `grep -nE '^\s*public '` on each file, and the grep printed nothing. Run the diff grep again after the commit of the phase.
- `float4` from `Brutal.Numerics` has the fields `X`, `Y`, `Z`, and `W`. The pool uses no operator of `float4`.
- The pool holds one record for each element, not one record for each tween. A count of tweens in a plan gives a lower record count (P6-1).
- A follow walk and an own tween on the same child give a jump when the own tween ends first (CR-1). A child with its own `Color` transition must never follow (Decision R2).
- A planted allocation proves a gate only for the path that the measured body runs. The review planted the follow walk and the tree check too, and each plant failed at least one gate.

---

## Testing Approach

### Unit Tests
**Files:** `tests/BrutalUi.Core.Tests/Style/Transition*Tests.cs`
- The pass, the retarget rules, the curves, the follow walk of `Color`, and the lifetime of a tween.

### Integration Tests
- `ThousandElementAllocationTests` with a transition sheet, and `ParsedSheetAllocationTests` with a step time.

---

## Needs Human Verification

`/autorun-feature` adds one line per item that the verifier cannot check, for a person to test.

- [ ] Phase 1 demo: the developer runs `dotnet run --project tests/BrutalUi.Core.Tests/BrutalUi.Core.Tests.csproj -- --filter-method '*AHoverFadesTheFillOfAButton' --show-live-output on` and reads `t 0.0 #1E222CFF`, then `t 0.1 #274176FF` after `States = Hover` and `Update(0.1f)`, then `t 0.2 #3060C0FF` after one more `Update(0.1f)`.
- [ ] Phase 2 demo: the developer runs `dotnet run --project tests/BrutalUi.Core.Tests/BrutalUi.Core.Tests.csproj -- --filter-method '*AHoverOutDuringTheFadeTurnsBack' --show-live-output on` and reads `hover 0.1 #274176FF`, then `leave 0.0 #274176FF`, `leave 0.1 #233251FF`, and `leave 0.2 #1E222CFF` after `States = None`.
- [ ] Phase 3 demo: the developer runs `dotnet run --project tests/BrutalUi.Core.Tests/BrutalUi.Core.Tests.csproj -- --filter-method '*EachEasingShapesTheFade' --show-live-output on` and reads `linear 0.500`, `ease-in 0.315`, `ease-out 0.685`, and `ease-in-out 0.500`.
- [ ] Phase 4 demo: the developer runs `dotnet run --project tests/BrutalUi.Core.Tests/BrutalUi.Core.Tests.csproj -- --filter-method '*TheTextOfALabelFadesWithItsButton' --show-live-output on` and reads `button #FFFFFFFF label #FFFFFFFF`, then `button #FFE680FF label #FFE680FF` after `States = Hover` and `Update(0.1f)`, then `button #FFCC00FF label #FFCC00FF` after one more `Update(0.1f)`.
- [ ] Phase 5 demo: the developer runs `dotnet run --project tests/BrutalUi.Core.Tests/BrutalUi.Core.Tests.csproj -- --filter-method '*ARemovedPanelEndsItsTweens' --show-live-output on` and reads `active 1 panel #274176FF`, then `active 0 panel #274176FF` after `root.Children.Remove(panel)` and `Update(0.1f)`, then `active 0 panel #3060C0FF` after `root.Children.Add(panel)` and `Update(0f)`.
- [ ] Phase 6 demo: the developer runs `dotnet run --project tests/BrutalUi.Core.Tests/BrutalUi.Core.Tests.csproj -- --filter-class '*ThousandElementAllocationTests'` and reads a total of 12 with `failed: 0`.
- [ ] Windows run (transitions): on Windows, after `git pull --ff-only`, run `.\scripts\test-windows.ps1`. Expect the PASS line and `$LASTEXITCODE` 0.

---

## Performance Considerations

- The pool holds one record for each element with a running tween. The array and the dictionary grow during warm-up and never shrink.
- Probe 12 of the review (`code-review.md`) gives the first numbers for Gap 6 of the plan. Release build, 1000 elements, 1000 records. Each tween has a duration of 1000 s, so each tween runs during the measure. The time is for each `Update(0.016f)`, in the second pass of 300 measured calls after 40 warm-up calls:

  | Scene | Root hover toggle in each `Update` | No restyle |
  |-------|------------------------------------|------------|
  | Baseline: no transition sheet | 135.9 us | 0.0 us |
  | A: 1000 records, 5 running slots on each element | 494.7 us | 219.9 us |
  | B: 1000 records, 4 slots on each element, and a root `Color` tween that 886 elements follow | 527.8 us | 212.3 us |

- Thus the toggle with a tween on each element adds about 0.36 ms (scene A) to 0.39 ms (scene B) to each `Update`. `Advance` alone takes about 0.22 ms. `Update` allocates 0 bytes. The probe ran before Decision R2 and Decision R3. In scene B, the 4 slots of each element do not include `Color`, so the two decisions probably do not change the 886 followers. A new run after the review can confirm the numbers.

---

## Native Dependencies

- None.

---

## Session Notes

### 2026-10-22 02:26
- `/autorun-epic` started the feature through `/autorun-feature` in the called mode. `/start-feature` created `context.md`, `tasks.md`, and `inbox.md`.
- The next session starts with phase 1, task 1.1.

### 2026-10-22 (phase 1, group 1)
- Tasks 1.1 to 1.6 are done. The build has 0 warnings and 0 errors. The tests give 2044 passed and 0 failed. `TheSetHoldsNoHeapObject` passes (44 bytes).
- The reflection grep and the public grep print nothing.
- An agent ran `git add -N` on the three new files by mistake, and then ran `git restore --staged` on them. The index is the same as before the run, and the three files are untracked.
- Open for group 2: tasks 1.7 to 1.11 and the marks of tasks 1.1 to 1.6 in `tasks.md`.

### 2026-10-22 (phase 1, group 2)
- Tasks 1.7 to 1.11 are done. The build has 0 warnings and 0 errors. The tests give 2056 passed and 0 failed.
- The demo prints the three lines of the brief. The three allocation gates pass with 0 bytes.
- No test showed a defect in a group 1 file, so group 2 changed no group 1 file.
- Open: the marks of tasks 1.1 to 1.11 in `tasks.md`, and the stale remarks of `StyleRule.cs`.

### 2026-10-22 02:58 (phase 1 verified)
- The verifier `phase-1-verify.md` gave PASS: 2056 tests with 0 failed, the three allocation classes with 0 failed, no public member, no reflection, and no change in ImGui, Markup, the samples, or the pins. Demo: PASS.
- The orchestrator marked tasks 1.1 to 1.11 in `tasks.md` and added the phase 1 demo to "Needs Human Verification" and to `docs/features/style-system/human-testing.md`.
- The brief of task 6.6 carries the remark fix of `StyleRule.cs`.

### 2026-10-22 03:10 (phase 2 verified)
- Phase 2 is done in one group (`phase-2.md`). The verifier `phase-2-verify.md` gave PASS: 2064 tests with 0 failed, `TransitionPassTests` 12 of 12 with no change, the allocation classes with 0 failed, and no public member. Demo: PASS.
- The orchestrator marked tasks 2.1 to 2.3 in `tasks.md` and added the phase 2 demo to "Needs Human Verification" and to `docs/features/style-system/human-testing.md`.

### 2026-10-22 03:22 (phase 3 verified)
- Phase 3 is done in one group (`phase-3.md`). The verifier `phase-3-verify.md` gave PASS: 2092 tests with 0 failed, `TransitionCurveTests` 28 of 28, the tests of phases 1 and 2 with no change, and the allocation classes with 0 failed. Demo: PASS.
- The orchestrator marked tasks 3.1 to 3.4 in `tasks.md` and added the phase 3 demo to "Needs Human Verification" and to `docs/features/style-system/human-testing.md`.

### 2026-10-22 (phase 4, group 1)
- Tasks 4.1 to 4.5 are done. The build has 0 warnings and 0 errors. The tests give 2101 passed and 0 failed.
- The demo prints the three lines of the brief. The three allocation gates pass.
- Open: the verifier of phase 4 and the marks of tasks 4.1 to 4.5 in `tasks.md`.

### 2026-10-22 03:40 (phase 4 verified)
- Phase 4 is done in one group (`phase-4.md`). The verifier `phase-4-verify.md` gave PASS: 2101 tests with 0 failed, `TransitionInheritanceTests` 9 of 9, `TextInheritanceTests` 13 of 13 with no change, and the allocation gates with 0 failed. Demo: PASS.
- The orchestrator marked tasks 4.1 to 4.5 in `tasks.md` and added the phase 4 demo to "Needs Human Verification" and to `docs/features/style-system/human-testing.md`.
- The brief of phase 5 states the two loops of `Advance` (P4-2) and the follow mark in bit 7 (P4-3), so the tree check runs before each write.

### 2026-10-22 (phase 5, group 1)
- Tasks 5.1 to 5.5 are done. The build has 0 warnings and 0 errors. The tests give 2110 passed and 0 failed.
- The demo prints the three lines of the brief. `ThePoolReusesItsRecords` and the allocation gates pass with 0 bytes.
- Open: the verifier of phase 5 and the marks of tasks 5.1 to 5.5 in `tasks.md`.

### 2026-10-22 03:58 (phase 5 verified)
- Phase 5 is done in one group (`phase-5.md`). The verifier `phase-5-verify.md` gave PASS: 2110 tests with 0 failed, `TransitionLifetimeTests` 9 of 9, `ElementListTests`, `TreeGuardTests`, `MultiSheetTests`, and `SubtreeMarkTests` with no change, and the allocation gates with 0 failed. Demo: PASS.
- The orchestrator marked tasks 5.1 to 5.5 in `tasks.md` and added the phase 5 demo to "Needs Human Verification" and to `docs/features/style-system/human-testing.md`.

### 2026-10-22 (phase 6, group 1)
- Tasks 6.1 to 6.5 are done. The build has 0 warnings and 0 errors. The tests give 2112 passed and 0 failed. `ThousandElementAllocationTests` gives 12 of 12.
- Both plants gave `exit=2` with 3 failed gates, and each restore gave an empty `diff`. The facts line gives 1049.6. `test-linux.sh` gave PASS and `exit=0`.
- Open: group 2 of phase 6 (task 6.6 and the docs) and the marks of tasks 6.1 to 6.5 in `tasks.md`.

### 2026-10-22 (phase 6, group 2)
- Tasks 6.6 to 6.9 and the remark fix of `StyleRule.cs` are done. The lint gives no score above 2.5 on the changed docs and on the plan.
- Open: the verifier of phase 6, the phase 6 demo line, and the marks of tasks 6.1 to 6.9 in `tasks.md`.

### 2026-10-22 04:30 (phase 6 verified)
- Phase 6 is done in two groups (`phase-6.md`, `phase-6-g2.md`). The verifier `phase-6-verify.md` gave PASS: 2112 tests with 0 failed, `ThousandElementAllocationTests` 12 of 12, the restored `TransitionPool.cs` equal to its copy, no change in ImGui, Markup, the samples, or the pins, a comment-only change of `StyleRule.cs`, the smoke run with `exit=0` and its 3 lines, and each changed doc below 2.5 in the lint. Demo: PASS.
- The orchestrator marked tasks 6.1 to 6.9 in `tasks.md` (37 of 37) and added the phase 6 demo and the Windows run to "Needs Human Verification". It added the phase 6 demo to `docs/features/style-system/human-testing.md`.
- Next: the cross-phase review (`review.md`).

### 2026-10-22 05:18 (the review and /update-feature)
- The cross-phase review (`review.md`) wrote `code-review.md` with 6 findings and 15 probes. Review groups 1 to 4 (`review-g1.md` to `review-g4.md`) fixed each finding. The verifier `review-verify.md` gave PASS: 2121 tests with 0 failed, the 0-byte gates, the six demos, the smoke run, and the lint.
- Decisions R2 and R3 settle CR-1 and its open question RG1-Q1 with the default that keeps the plan. The report to `/autorun-epic` names both decisions.
- The inbox holds no line, so no line folds and no bug goes to `docs/bugs/`.
- `/update-feature` set the status to Ready for Review. `/autorun-epic` runs `/update-epic` and `/update-master` at the end of the epic.
