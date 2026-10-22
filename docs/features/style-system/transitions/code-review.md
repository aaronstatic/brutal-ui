# style-system/transitions - Code Review

**Reviewed:** 2026-10-22
**Reviewer:** Agent (cross-phase review of phases 1 to 6)
**Rules Applied:** 1. A correct value in each frame, 2. The dirty rule of Q3, 3. Stable tweens, 4. Zero allocation after warm-up, 5. API clarity and the internal design, 6. No regression, 7. Test quality, `AGENTS.md`, and the docs. The Vercel React rules do not apply to C#.
**Status:** Resolved - 6 findings, 6 resolved

---

## Summary

| Severity | Count |
|----------|-------|
| HIGH | 1 |
| MEDIUM | 0 |
| LOW | 5 |

The transition pass gives the right value in each frame for each probe of a single tween. The five members fade to their exact targets, the color step is premultiplied, and the curves give the CSS reference values. The pass and the follow walk mark `Paint` only. `Update` allocates 0 bytes with 1000 running records, and a plant in the follow walk or in the tree check fails a gate. A shipped test kills 15 of the 16 mutations of the keep, turn, dirty, color, curve, clock, follow, lifetime, and merge rules.

The HIGH finding is the open risk of phase 4. A child that inherits the color and declares a shorter `Color` transition of its own jumps back when its own tween ends, because the follow walk then writes the color of the parent over it (CR-1). The probe shows a jump from 150 to 50 on the red channel in one frame. The hand-offs say that the visible value stays continuous, and that statement is false. The LOW findings follow. The keep rule ignores the transition set of the new style (CR-2). No test proves the bisection fallback (CR-3) or the `ElementList.Move` rule (CR-4). The follow mark lives in bit 7 of the slot mask (CR-5). Some doc text does not match the code (CR-6).

All probes ran in the scratch copy `/tmp/claude-1000/-home-aaronstatic-dev-brutal-ui/1041e7b1-7770-4054-8b67-c6d8d936fcf3/scratchpad/cr-transitions/repo`. Each mutation was one edit of the copy, then one full `dotnet test --solution brutal-ui.sln`, then a restore from the saved original in `../orig/`. After the last restore, `diff -rq` of the `src` folders of the copy and of the repository gave no difference. The probe class `tests/BrutalUi.Core.Tests/ReviewProbeTests.cs` (5 tests, no assert) lived in the copy during the runs, so each mutation run has a total of 2117. No file of the repository changed, except this file.

1. **Baseline (rule 6).** `dotnet build brutal-ui.sln` gives 0 warnings and 0 errors. `dotnet test --solution brutal-ui.sln` gives total 2112, failed 0.
2. **The keep rule (rule 3).** `Track` with no keep branch (each restyle with the same target restarts the slot from the visible value): 1 failure (`ARestyleWithTheSameTargetKeepsTheTween`). The two transition tests of `ThousandElementAllocationTests` pass with the mutation, so the 1000-element step of Risk 1 does not guard the rule. One test guards it.
3. **The turn rule (rule 3).** A turn that starts from `To` (the old target): 2 failures (`AHoverOutDuringTheFadeTurnsBack`, `ANewTargetTakesTheFullDuration`).
4. **The dirty rule (rule 2).** `Paint | Layout` in loop 1 of `Advance`: 2 failures (`ATweenMarksPaintOnly`, `AFollowMarksPaintOnly`). `Paint | Style` in the follow walk: 1 failure (`AFollowMarksPaintOnly`).
5. **The color step (rule 1).** A straight step on each channel for the three color members: 1 failure (`AColorFadesInPremultipliedAlpha`).
6. **The curves (rule 1).** `EaseIn` and `EaseOut` with swapped control points: 3 failures (`EachEasingShapesTheFade` and the two rows at 0.5 of `EachEasingAtZeroHalfAndOne`). A throw in place of the bisection: 1 failure (`TheCurvesAllocateNothing`, which fails on the exception, not on a value). With the throw, a scan of 999999 progress values reaches the bisection 352 times for `EaseIn`, 349 times for `EaseOut`, and 0 times for `EaseInOut`. The bisection dropped (the solver returns the last Newton parameter): **0 failures** (see CR-3).
7. **The clock (rule 1).** A negative `deltaTime` that moves a tween back: 2 failures (the rows -0.1 and negative infinity of `ADeltaTimeOfZeroOrLessAdvancesNothing`). A `deltaTime` of 0 that skips the write: 4 failures (`AnEpsilonDurationStartsATween`, `AHoverOutDuringTheFadeTurnsBack`, `AClearedInlineValueFadesToTheRuleValue`, `ANewTargetTakesTheFullDuration`).
8. **The follow walk (rule 3).** No skip of a child with a running `Color` slot: 1 failure (`AChildWithItsOwnColorTransitionRunsItsOwnTween`). No skip of a child with `InheritsColor` false: 2 failures (`AnInlineColorStopsTheWalk`, `AChildWithItsOwnColorDoesNotFollow`).
9. **The lifetime (rule 3).** No loop 0: 4 failures (`AMoveIntoAnotherContextStartsNoTween`, `ARemovedAncestorEndsTheTweensOfItsSubtree`, `ARemovedRootEndsItsTweens`, `ARemovedPanelEndsItsTweens`). No `IsStyled = false` in `MarkSubtreeJoinedCore`: 4 failures (`AMoveIntoAnotherContextStartsNoTween`, `AMoveInsideTheTreeEndsTheTween`, `AnElementThatBecomesARootStartsNoTween`, `ARemovedPanelEndsItsTweens`).
10. **The merge (rule 3).** `MergeFrom` that keeps the first matched rule for each slot: 2 failures (`TheTransitionOfTheNewStyleApplies`, `ALaterMatchedRuleWinsForEachSlot`).
11. **The planted allocations (rule 4).** `GC.KeepAlive(new object());` as the first line of the loop body of `FollowColor`: exit 2, 1 failure (`UpdateWithRunningTransitionsAllocatesNothing`). The same plant as the first line of `IsInTree`: exit 2, 3 failures (`AParsedSheetAllocatesNothingInUpdate`, `ThePoolReusesItsRecords`, `UpdateWithRunningTransitionsAllocatesNothing`).
12. **The cost (Risk 9 and Risk 11, a fact, not a gate).** A Release build, the 1000-element tree, 300 measured `Update(0.016f)` calls after 40 warm-up calls, and a second pass after a first pass for the JIT. Each tween has a duration of 1000 s, so each tween runs during the measure. The table gives the time for each `Update` of the second pass:

    | Scene | Root hover toggle in each `Update` | No restyle |
    |-------|------------------------------------|------------|
    | Baseline: no transition sheet | 135.9 us | 0.0 us |
    | A: 1000 records, each element runs 5 slots (`InheritsColor` 0) | 494.7 us | 219.9 us |
    | B: 1000 records, 4 slots on each element, and a root `Color` tween with a follow walk over 886 inheriting elements | 527.8 us | 212.3 us |

    The first pass gave 696.6 us (baseline), 816.5 us (A), and 509.8 us (B) with the toggle. Each measured region allocated 40 bytes, also in the baseline. The 40 bytes are the `Stopwatch` object of the probe. Thus the worst case costs about 0.36 ms more for each `Update` than the same restyle with no tween. About 0.22 ms of it is `Advance` alone.
13. **The open risk of phase 4.** A button fades `Color` from red 0 to red 200 in 0.4 s (linear). Its child inherits the color and declares its own `Color` transition. Steps of 0.025 s. The table gives the red channel of the child (the button is 13, 25, 38, 50, ... 200):

    | Case | t 0.025 | t 0.050 | t 0.075 | t 0.100 | t 0.225 | t 0.250 | t 0.275 | t 0.300 |
    |------|---------|---------|---------|---------|---------|---------|---------|---------|
    | Own 0.1 s | 50 | 100 | 150 | **50** | 113 | 125 | 138 | 150 |
    | Own 0.1 s, and `root.States = Focus` before the step to 0.225 | 50 | 100 | 150 | **50** | 125 | 150 | 175 | **150** |
    | Own 0.8 s | 6 | 13 | 19 | 25 | 56 | 63 | 69 | 75 |

    In the first two cases, the child runs its own tween, then it jumps back to the value of the button when its own tween ends. The ancestor restyle of the second case starts a second own tween, and the end of that tween gives a second jump (175 to 150). The third case is continuous: the child lags behind the button and ends at 0.8 s. See CR-1.
14. **Extra probes.** A class swap during a fade to a rule with the same target and no transition keeps the tween (`#274176FF` after the swap, 1 record). See CR-2. A `root.Children.Move(0, 1)` during a fade keeps the tween: the panel stays at `#274176FF` after `Update(0f)` and ends at `#3060C0FF` after 0.1 s more. See CR-4.
15. **Rule checks (rule 6 and rule 7).** `git diff --quiet 11bd69a -- Directory.Packages.props src/BrutalUi.ImGui src/BrutalUi.Markup samples` gives exit 0. The public grep of the Core diff and of `Transition*.cs`, the reflection grep, and the style write grep (I4) give no hit. No `.csproj`, `.props`, or `.targets` file changed. The test diff removes no `Assert` line. In the copy, `scripts/test-linux.sh` gives `test-linux: PASS build, test, gpu, smoke (120 frames) in 23 s` and exit 0. The smoke log holds `120 frames rendered`, `draw commands 2, vertices 128`, and `Update and Draw 0`.

### Known items from the build

1. **P6-1 and P6-2: `TheTransitionStepRunsTweens` asserts `ActiveCount == 2`.** The item holds, and the value is right. The pool holds one record for each element, and the first style step starts three tweens in two records. The test proves the third tween (the slot border) by its value after 9 more steps. Not reported.
2. **P6-4 and P6-5: the `#header` tween never turns.** The item holds. The doc comment of `BuildTransitionSheet` states it, and the hover toggle turns the slot border tween at each style step. `UpdateWithRunningTransitionsAllocatesNothing` asserts a running tween after the measure. Not reported.
3. **The open risk of phase 4.** The item holds, and the risk is larger than the context states. The child also jumps back with no ancestor restyle (probe 13). See CR-1.
4. **The follow-up risks of phase 5.** A `Remove` and an `Insert` end a tween: `AMoveInsideTheTreeEndsTheTween` proves it. A detached element keeps its last tween value until a join: `ARemovedPanelEndsItsTweens` proves it, and section 7 and the hand-offs state it. `ElementList.Move` keeps a tween: true (probe 14), but no test proves it. See CR-4.

---

## Findings

### CR-1: An inheriting child with a shorter own `Color` transition jumps back when its own tween ends

**Severity:** HIGH
**Rule:** 1. A correct value in each frame, and 3. Stable tweens (the follow rule and the own tween of a child)
**File:** `src/BrutalUi.Core/Style/TransitionPool.cs` lines 328 to 344 (loop 2 of `Advance`) and lines 369 to 389 (`FollowColor`), `src/BrutalUi.Core/Style/StylePass.cs` line 210 (the write of `InheritsColor`), `tests/BrutalUi.Core.Tests/Style/TransitionInheritanceTests.cs` lines 251 to 292 (`AChildTweenThatEndsFirstFollowsItsParent`), and `docs/features/style-system/transitions/context.md` lines 237 and 346

**Problem:** A hover on a button restyles its subtree (A2). The child inherits the target of the button, and its own `Color` transition starts its own tween. The walk skips the child while its own slot runs:

```csharp
if (!child.InheritsColor || HasRunningColor(child))
{
    continue;
}
```

When the own tween of the child ends, loop 1 writes the target, and the slot bit goes to 0. Then loop 2 runs the walk of the button, and the walk writes the visible color of the button over the child. The button is still at the start of its own fade, so the child jumps back. Probe 13 shows red 150, then 50, in one step of 0.025 s. An ancestor restyle during the fade starts a second own tween from the visible value, and its end gives a second jump (175, then 150). A theme rule such as `Label { transition: color 0.1s }` under a button with `transition: color 0.3s` gives this jump on each hover.

P4-2 chose this order so that the result does not depend on the order of the records. `AChildTweenThatEndsFirstFollowsItsParent` asserts the follow value with one step of 0.1 s, so the test cannot see the jump. Risk 6 of the plan names the defect ("The child color jumps between two values"). The hand-off line of `## Hand-offs` ("The visible value stays continuous") and line 237 of `context.md` are false.

**Fix:** A child with its own `Color` transition runs its own tween and never follows. This rule keeps F8 ("a child with its own `Color` transition runs its own tween"), and the child ends on the same target as its parent, so no jump occurs.
1. In `StylePass.Restyle`, write the flag with the merged set: `element.InheritsColor = (applied & (1UL << (int)StyleProperty.Color)) == 0 && !merged.IsSlotSet(StyleTransitions.SlotOf(StyleProperty.Color));`. Rename the property to `FollowsColor`, because `FollowColor` is its only reader, and update its remarks and section 7.
2. Keep the `HasRunningColor` skip of `FollowColor` (CR-2 can keep a slot that the merged set does not hold).
3. Change the assert of `AChildTweenThatEndsFirstFollowsItsParent` to the own target of the chip (`#FFCC00FF`) and rename the test. This is a change of an assert, so the user must accept the rule first. Add a test with steps of 0.025 s that asserts that the red channel of the child never falls during the fade, for the two cases of probe 13.
4. Update the phase 4 risk in `context.md`, the epic review line of `## Hand-offs`, and the follow rule paragraph of section 7.

The other option is "the follow wins": `Track` starts no own `Color` tween while an ancestor runs a `Color` tween. That option changes `AChildWithItsOwnColorTransitionRunsItsOwnTween` and needs a walk up the parents in `Track`. Write the choice in `context.md` as an open question.

**Effort:** Medium

---

### CR-2: The keep rule ignores the transition set of the new style

**Severity:** LOW
**Rule:** 3. Stable tweens (the keep rule and "the set of the new style decides")
**File:** `src/BrutalUi.Core/Style/TransitionPool.cs` lines 157 to 161 (the keep branch of `Track`) and lines 78 to 82 (its remarks)

**Problem:** The keep branch checks the target and the inline set. It does not check the merged set:

```csharp
if (running && !isInline && Equal(in target, in _records[index].Slots[slot].To))
{
    continue;
}
```

Probe 14 swaps the class of a fading element to a rule with the same target and no transition. The tween keeps running for its full duration (`#274176FF` after the swap, 1 record). Decision 3 says that the set of the new style decides, and step 4 of the CSS start algorithm cancels a running transition when the new style holds no transition for the property. A new target with no transition ends at once (`ANewTargetWithNoTransitionChangesAtOnce`), so the same target behaves in another way. The case is rare, and the visible result is a slow end in place of a jump.

**Fix:** Add `merged.IsSlotSet(slot)` to the condition of the keep branch. A slot that the new style does not hold then goes to the "no change" branch or to the "else" branch, and it ends. The A2 case does not change, because the subtree restyle keeps the matched rules. Add `ASameTargetWithNoTransitionEndsTheTween` to `TransitionRetargetTests` with the scene of probe 14. Update the keep text of the remarks, of section 7, and of Decision 4.

**Effort:** Low

---

### CR-3: No test proves the bisection fallback of the curve solver

**Severity:** LOW
**Rule:** 7. Test quality
**File:** `src/BrutalUi.Core/Style/TransitionCurves.cs` lines 101 to 151 (`SolveParameter`), and `tests/BrutalUi.Core.Tests/Style/TransitionCurveTests.cs`

**Problem:** The scan of probe 6 reaches the bisection for 352 of 999999 progress values of `EaseIn` and for 349 of `EaseOut`. Newton starts at `t = progress`, and near the flat end of these two curves it leaves 0 to 1 or does not reach the tolerance in 8 steps. With the bisection removed, no shipped test fails. Only `TheCurvesAllocateNothing` reaches the code, and it checks the bytes, not the value. Thus a later change can break the fallback with no failed test.

**Fix:** Make `SolveParameter` `internal`. Add a test to `TransitionCurveTests` that scans 100000 progress values for each of the three curves and asserts that the x value of the curve at the result (`Sample(x1, x2, t)`) differs from the progress by less than 1e-6. Make `Sample` `internal` for the test, or compute the cubic in the test. The test fails when the bisection goes, because Newton gives up only when its error stays above the tolerance.

**Effort:** Low

---

### CR-4: No test proves that `ElementList.Move` keeps a running tween

**Severity:** LOW
**Rule:** 7. Test quality (and 3. Stable tweens, the leave and join rules)
**File:** `tests/BrutalUi.Core.Tests/Style/TransitionLifetimeTests.cs` (no test), `docs/technical-design.md` section 7 (**The leave and join rules.**), and `docs/features/style-system/transitions/context.md` line 340 (`## Hand-offs`)

**Problem:** Section 7 and the hand-off tell a game to "Use `Move` for a reorder inside one child list", because `Move` keeps the tweens. Probe 14 shows that the rule holds today. No test covers it. `AMoveInsideTheTreeEndsTheTween` uses `Remove` and `Add`, which is the other rule. A later change of `ElementList.Move` (for example, a call of `AttachToParent` or of `MarkSubtreeJoined`) breaks the hand-off with no failed test.

**Fix:** Add `AMoveInsideOneChildListKeepsTheTween` to `TransitionLifetimeTests`: a root with the panel and one more child, a hover and `Update(0.1f)` (`#274176FF`), `root.Children.Move(0, 1)` and `Update(0f)` (still `#274176FF`, 1 record), then `Update(0.1f)` (`#3060C0FF`, 0 records).

**Effort:** Low

---

### CR-5: The follow mark lives in bit 7 of the slot mask

**Severity:** LOW
**Rule:** 5. API clarity and the internal design (P4-3)
**File:** `src/BrutalUi.Core/Style/TransitionPool.cs` lines 49 to 53 (`FollowBit`), 264, 271, 290 to 293, and 333 to 335, and `src/BrutalUi.Core/Style/TransitionRecord.cs` lines 55 to 68

**Problem:** `TransitionRecord.Mask` has the doc "One bit for each running slot. Bit `n` is slot `n`." During `Advance`, bit 7 holds a second meaning:

```csharp
private const int FollowBit = 1 << 7;
...
record.Mask &= unchecked((byte)~FollowBit);
```

Two `Debug.Assert` lines and an `unchecked` cast protect the second meaning. P4-3 chose the bit because `TransitionRecord.cs` was not a file of the brief of phase 4. That reason does not hold now. A reader of `Mask` outside `Advance` must know the rule of loop 2. A `SlotCount` of 8 or more breaks the bit in a Release build with no error.

**Fix:** Add `internal bool Follow;` to `TransitionRecord`, after `Mask`. The field fits in the padding before `Slots`, so the record does not grow. Set it in loop 1, and read and clear it in loop 2. Remove `FollowBit`, the two asserts, and the `unchecked` cast. Update the remarks of `Advance`, the remarks of `TransitionRecord`, section 7 ("the follow mark in bit 7"), and the epic review line of `## Hand-offs`.

**Effort:** Low

---

### CR-6: Doc text that does not match the code

**Severity:** LOW
**Rule:** 7. The docs
**File:** `src/BrutalUi.Core/Style/TransitionPool.cs` lines 105 to 108 (the remarks of `Track`), `docs/features/style-system/transitions/context.md` lines 237, 346, and 384 to 386, and `docs/technical-design.md` section 7 (**The cost.**)

**Problem:**
1. The remarks of `Track` say "Thus an element that moves inside its tree, or that leaves and joins again before the next update, ends each tween". `ElementList.Move` moves an element inside its tree and keeps each tween (probe 14). Section 7 states the right rule.
2. `context.md` (the phase 4 risk and the epic review line) says "The visible value stays continuous". Probe 13 shows two jumps. CR-1 changes the rule, and the text must follow the fix.
3. `## Performance Considerations` holds one line, and Gap 6 of the plan waits for a benchmark of a hover on a high ancestor with running tweens. Probe 12 gives the first numbers.

**Fix:** (1) Change the sentence to "Thus an element that a `Remove` and an `Add` (or an `Insert`) move inside its tree, or that leaves and joins again before the next update, ends each tween. `ElementList.Move` does not attach, so it keeps each tween." (2) Rewrite the two lines after CR-1. (3) Add the table of probe 12 to `## Performance Considerations`, with the note "Release build, 1000 elements, 1000 records". Add one sentence to **The cost.** of section 7: the worst case adds about 0.36 ms for each `Update`.

**Effort:** Low

---

## What's Already Good

- **The values are exact at the ends.** Each member ends on the exact target of the cascade, the `>= 1` end rule and the 0.1 s demo steps add up exactly, and the corner order and the opacity getter map round trip (`EachMemberFadesToItsTarget`).
- **The dirty rule holds.** `Advance` and the follow walk mark `Paint` only, `Compare` and the dirty rule table of `StylePass.cs` did not change, and two tests read the bits of the pass alone through `UpdateTransitions`.
- **The clock rejects each bad `deltaTime`.** One expression (`deltaTime > 0f ? deltaTime : 0f`) handles 0, negative values, and `NaN`. Infinity ends each tween. The start guard rejects each bad duration, so the progress never divides by 0. Each branch has a test row.
- **The lifetime is safe across contexts.** Loop 0 drops a record with no write and no mark, and the join mark drops it in `Track`. A second context with another hover color proves that the first context writes nothing (`AMoveIntoAnotherContextStartsNoTween`).
- **The allocation gates are strong.** Each plant in `Track`, loop 1, the follow walk, and the tree check fails at least one gate. The 1000-element gate asserts the same `Capacity` before and after the measure, and a running tween after it.
- **The internal design reads well.** P1-3 and P5-3 give the right order: the lookup comes before the `IsStyled` check, so a join drops the record. The three loops of `Advance` each have one job, and the separate loop 0 keeps the release rule simple (P5-1). The `if` chain of `Evaluate` needs no fallback throw, because the `StyleTransition` constructor rejects each other value and `default` is `Linear` (DS6.3).
- **No public API and no regression.** No new public member, no reflection, no new package, no change in ImGui, Markup, the samples, or the pins. The test diff removes no assert. The smoke run keeps its 3 lines.
- **The docs match the code in most places.** Sections 4, 7, and 14 give the real three loops, the record model, the merge rule, and the test counts (66 transition tests, 12 tests in `ThousandElementAllocationTests`).

---

## Recommended Fix Order

| Order | Finding | Reason |
|-------|---------|--------|
| 1 | CR-1 | The only visible defect. It needs a user decision on the rule, and it changes an assert and the `InheritsColor` write. |
| 2 | CR-2 | A one-line change of the keep branch. Do it after CR-1, because CR-1 keeps the `HasRunningColor` skip for this case. |
| 3 | CR-5 | A small internal change of `Advance` and `TransitionRecord`. Do it after CR-1, so the walk code changes one time. |
| 4 | CR-3 | A test only, with two `internal` members. |
| 5 | CR-4 | A test only. |
| 6 | CR-6 | Doc text. Do it last, so it states the rules of CR-1, CR-2, and CR-5. |

---

## Progress

- [x] CR-1: An inheriting child with a shorter own `Color` transition jumps back when its own tween ends (review group 1, Decision R2, and Decision R3)
- [x] CR-2: The keep rule ignores the transition set of the new style (review group 1)
- [x] CR-3: No test proves the bisection fallback of the curve solver (review group 3)
- [x] CR-4: No test proves that `ElementList.Move` keeps a running tween (review group 4)
- [x] CR-5: The follow mark lives in bit 7 of the slot mask (review group 2)
- [x] CR-6: Doc text that does not match the code (review group 2)
