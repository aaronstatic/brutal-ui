# style-system/transitions - Task Checklist

**Last Updated:** 2026-10-22 05:18
**Progress:** 37/37 tasks complete (100%)
**Advanced phases:** Phases 1, 4, 5, and 6 (ADVANCED, opus). Phases 2 and 3 are STANDARD (sonnet). Group B of phase 6 is plain documentation work.

Source paths: `Core/` means `src/BrutalUi.Core/`. "The plan" means `implementation.md` of this feature. Its sections "The Frame Order", "The Record Model", "Track: the Restyle Hook", "Advance: the Transition Pass", "The Value of Each Member", and "The Curves" hold the rules that the tasks name. Each phase ends with `dotnet build brutal-ui.sln 2>&1 | tail -n 30` and `dotnet test --solution brutal-ui.sln 2>&1 | tail -n 30` (tool rule 5). Expect `0 Warning(s)`, `0 Error(s)`, and 0 failed. A demo uses `dotnet run --project tests/BrutalUi.Core.Tests/BrutalUi.Core.Tests.csproj -- --filter-method '*Name' --show-live-output on`. A test prints with `TestContext.Current.TestOutputHelper?.WriteLine(...)`. The class table and the type table are global, and xUnit runs classes in parallel, so each test interns its names with the prefix `nameof(Test) + "."`. A color prints as `#RRGGBBAA` (`Rgba8.ToString`). A time prints with `ToString("F1", CultureInfo.InvariantCulture)`, and an opacity with `"F3"`. The demos use durations of 0.2 s and steps of 0.1 s. The projects have no implicit usings, so each new file holds its own `using` lines.

---

## Phase 1: A Hover Fades the Fill of a Button (11/11 complete)

**Demo:** the developer runs `dotnet run --project tests/BrutalUi.Core.Tests/BrutalUi.Core.Tests.csproj -- --filter-method '*AHoverFadesTheFillOfAButton' --show-live-output on` and reads `t 0.0 #1E222CFF`, then `t 0.1 #274176FF` after `States = Hover` and `Update(0.1f)`, then `t 0.2 #3060C0FF` after one more `Update(0.1f)`.
**Needs:** none
**Agent tier:** ADVANCED (opus)

**Group A (the model and the pool, 4 files):**

- [x] ✅ **1.1 Record the baseline and read the code** - Completed 2026-10-22
  - Description: Record in `context.md`: the base commit (`git rev-parse HEAD`), the test total (expected 2044), the build result, and the `bytes per element` line of `dotnet run -c Release --project tests/BrutalUi.Benchmarks/BrutalUi.Benchmarks.csproj -- --facts 2>&1 | tail -n 30` (expected 1049.5). Read `StylePass.cs`, `StyleTransitions.cs`, `StyleRule.cs`, `ComputedStyle.cs`, `Element.cs`, `UiContext.cs`, `Rgba8.cs`, `Corners.cs`, and `PseudoStateSelectorTests.AHoverRuleRestylesTheButton`.
  - File(s): `context.md`
  - Estimate: 8 steps

- [x] ✅ **1.2 Add the internal slot helpers and MergeFrom to StyleTransitions.cs** - Completed 2026-10-22
  - Description: Edit `src/BrutalUi.Core/Style/StyleTransitions.cs`: an internal constant `SlotCount` (5), make `SlotOf` internal, and add the internal members `PropertyOf(int slot)`, `IsSlotSet(int slot)`, `GetSlot(int slot)`, and `MergeFrom(in StyleTransitions later)`. `MergeFrom` copies each slot that `later` holds and sets its bit, so a later rule wins for each slot. Add no public member. The size stays 44 bytes (`TheSetHoldsNoHeapObject`).
  - File(s): `src/BrutalUi.Core/Style/StyleTransitions.cs`
  - Estimate: 5 steps

- [x] ✅ **1.3 Write TransitionCurves.cs (linear only)** - Completed 2026-10-22
  - Description: Write `src/BrutalUi.Core/Style/TransitionCurves.cs` (internal static): `Evaluate(TransitionEasing easing, float progress)` clamps the progress to 0 to 1 and gives the linear value for each member. The remarks state that phase 3 gives the three other curves.
  - File(s): `src/BrutalUi.Core/Style/TransitionCurves.cs`
  - Estimate: 3 steps

- [x] ✅ **1.4 Write TransitionRecord.cs** - Completed 2026-10-22
  - Description: Write `src/BrutalUi.Core/Style/TransitionRecord.cs`: `TweenSlot`, `TweenSlots` (`[InlineArray(5)]`), and `TransitionRecord` per "The Record Model". Each type and each member is `internal` (I2), with doc comments.
  - File(s): `src/BrutalUi.Core/Style/TransitionRecord.cs`
  - Estimate: 4 steps

- [x] ✅ **1.5 Write TransitionPool.cs** - Completed 2026-10-22
  - Description: Write `src/BrutalUi.Core/Style/TransitionPool.cs` (internal sealed) per "The Record Model", "Track", "Advance", and "The Value of Each Member". The later phases add five parts: the inline branch of `Track` (phase 2), the `deltaTime` rule of step 1 of "Advance" (phase 3), the follow walk of step 5 (phase 4), and the tree check of step 3 and the drop of step 2 of "Track" (phase 5). In this phase, step 2 of "Track" only returns, because a new element has no record. The duration guard of the start rule is in this phase, so a duration of 0 never divides. The array starts empty, grows to 4 at the first rent, and doubles. The dictionary uses `ReferenceEqualityComparer.Instance`. A release clears the freed array slot, so the pool holds no reference to an element after its release. `ActiveCount` and `Capacity` are internal. The remarks state the zero-allocation rule and the growth rule.
  - File(s): `src/BrutalUi.Core/Style/TransitionPool.cs`
  - Estimate: 12 steps

**Group B (the wiring and the demo, 5 files):**

- [x] ✅ **1.6 Add IsStyled to Element.cs** - Completed 2026-10-22
  - Description: Edit `src/BrutalUi.Core/Tree/Element.cs`: `internal bool IsStyled`. A new element holds `false`. The doc comment states the first-style rule.
  - File(s): `src/BrutalUi.Core/Tree/Element.cs`
  - Estimate: 3 steps

- [x] ✅ **1.7 Merge the sets and call Track in StylePass.cs** - Completed 2026-10-22
  - Description: Edit `src/BrutalUi.Core/Style/StylePass.cs`: `Run`, `Visit`, and `Restyle` take the `TransitionPool`. `Restyle` merges the set of each matched rule with a non-empty set into a local `StyleTransitions` (`MergeFrom`). Before the write `current = next`, it calls `Track` when the merged set is not empty or `ActiveCount > 0`. After the write, it sets `IsStyled`. `Compare` and the dirty rule table do not change. Update the remarks: the pass writes the target, and the transition pass writes the interpolated value over it.
  - File(s): `src/BrutalUi.Core/Style/StylePass.cs`
  - Estimate: 7 steps

- [x] ✅ **1.8 Wire the pool and UpdateTransitions into UiContext.cs** - Completed 2026-10-22
  - Description: Edit `src/BrutalUi.Core/Tree/UiContext.cs`: a `TransitionPool` field (both constructors), `UpdateStyles` passes it, a new internal `UpdateTransitions(float deltaTime)`, an internal `Transitions` property for the tests, and `Update` calls `UpdateStyles`, then `UpdateTransitions`, then the layout pass. Update the doc comment of `deltaTime` (the transition pass reads it) and the remarks of `Update` (the frame order).
  - File(s): `src/BrutalUi.Core/Tree/UiContext.cs`
  - Estimate: 5 steps

- [x] ✅ **1.9 Write TransitionPassTests.cs** - Completed 2026-10-22
  - Description: Write `tests/BrutalUi.Core.Tests/Style/TransitionPassTests.cs`:
    - `AHoverFadesTheFillOfAButton` (the demo): `.<p>button` sets `BackgroundColor` #1E222C with `BackgroundColor` 0.2 s `Linear`. `.<p>button:hover` sets #3060C0. A root with one button. `Update(0f)`, then `States = Hover` and `Update(0.1f)`, then `Update(0.1f)`. Print `t <time> <color>` after each update. Assert the three lines.
    - `EachMemberFadesToItsTarget` (a theory, 5 rows): the value of the member at half the duration and the exact target at the full duration. Example rows: `CornerRadius` 0 to 8 gives 4, `Opacity` 1 to 0.5 gives 0.75.
    - `TheFirstStyleStartsNoTween`: a rule with a transition gives its value at the first `Update(0f)`, and `ActiveCount` is 0.
    - `ATweenMarksPaintOnly`: after a full `Update` with a running tween, `UpdateTransitions(0.05f)` alone gives `Paint` and no other bit on the button, and `DescendantPaint` and no other bit on the root. A `Label` with a corner radius tween gets no `Layout` bit.
    - `AMemberWithNoTransitionChangesAtOnce`: the hover rule also sets `BorderColor` with no transition. The border holds the target after the first update.
    - `ALaterMatchedRuleWinsForEachSlot`: `.a` sets two slots, a later `.b` sets one of them with another duration. Each slot uses the duration of its last rule.
    - `AColorFadesInPremultipliedAlpha`: transparent to #FF0000FF gives #FF000080 at half the duration.
    - `ASheetWithNoTransitionRentsNoRecord`: a hover with no transition gives `ActiveCount` 0 and `Capacity` 0.
  - File(s): `tests/BrutalUi.Core.Tests/Style/TransitionPassTests.cs`
  - Estimate: 12 steps

- [x] ✅ **1.10 Give ParsedSheetAllocationTests a step time and a settle step** - Completed 2026-10-22
  - Description: Edit `tests/BrutalUi.Markup.Tests/Style/ParsedSheetAllocationTests.cs`: the gate text holds `transition: background-color 0.2s ease-out, opacity 150ms linear`, and `AssertTheRulesMatch` asserts the targets. With this feature, `Update(0f)` keeps those members at their start value. Change `Step` to `Context.Update(StepTime)` with `StepTime = 0.016f`, so the measured steps run the tweens. Add a `Settle()` method that runs `Context.Update(1f)` (more than each duration, outside the measure) and call it before each `AssertTheRulesMatch`. Update the remarks. Change no assert.
  - File(s): `tests/BrutalUi.Markup.Tests/Style/ParsedSheetAllocationTests.cs`
  - Estimate: 5 steps

- [x] ✅ **1.11 Run the gate, the demo, and the allocation gates** - Completed 2026-10-22
  - Description: Run the build, the tests, and the demo. Run `--filter-class '*ThousandElementAllocationTests'`, `--filter-method '*TheStylePassAllocatesNothingAfterWarmUp'`, and the Markup gate, and expect 0 failed. Record the printed lines and the test total in `context.md`.
  - File(s): `context.md`
  - Estimate: 5 steps

---

## Phase 2: A Hover Out During the Fade Turns Back from the Current Color (3/3 complete)

**Demo:** the developer runs `dotnet run --project tests/BrutalUi.Core.Tests/BrutalUi.Core.Tests.csproj -- --filter-method '*AHoverOutDuringTheFadeTurnsBack' --show-live-output on` and reads `hover 0.1 #274176FF`, then `leave 0.0 #274176FF`, `leave 0.1 #233251FF`, and `leave 0.2 #1E222CFF` after `States = None`.
**Needs:** Phase 1
**Agent tier:** STANDARD (sonnet)

**Group A (the retarget rules, 2 files):**

- [x] ✅ **2.1 Add the inline branch to Track** - Completed 2026-10-22
  - Description: Edit `src/BrutalUi.Core/Style/TransitionPool.cs`: `Track` reads the inline mask. A member that the inline set holds never starts a tween and ends its running slot, so the style pass writes the inline value at once. A member that leaves the inline set follows the start rule with the set of the rules (Decision 4). Update the remarks of `Track` with each branch.
  - File(s): `src/BrutalUi.Core/Style/TransitionPool.cs`
  - Estimate: 6 steps

- [x] ✅ **2.2 Write TransitionRetargetTests.cs** - Completed 2026-10-22
  - Description: Write `tests/BrutalUi.Core.Tests/Style/TransitionRetargetTests.cs`:
    - `AHoverOutDuringTheFadeTurnsBack` (the demo): the sheet of phase 1. `Update(0f)`, hover, `Update(0.1f)`, then `States = None` and `Update(0f)`, `Update(0.1f)`, and `Update(0.1f)`. Print and assert the four lines.
    - `ARestyleWithTheSameTargetKeepsTheTween`: hover, `Update(0.1f)`, then a state change on the root (a subtree mark, A2) and `Update(0.1f)`. The button holds the target, so the elapsed time stayed.
    - `ANewTargetTakesTheFullDuration`: a third color during the tween starts from the visible value and ends after the full duration.
    - `TheTransitionOfTheNewStyleApplies`: the base rule gives 0.4 s, the hover rule gives 0.1 s. The hover in ends after 0.1 s. The hover out runs 0.4 s.
    - `ANewTargetWithNoTransitionChangesAtOnce`: a class change that removes the only rule with the slot, during a tween, gives the new target at once and `ActiveCount` 0.
    - `AnInlineValueChangesAtOnce`, `AnInlineValueEndsARunningTween`, and `AClearedInlineValueFadesToTheRuleValue`.
  - File(s): `tests/BrutalUi.Core.Tests/Style/TransitionRetargetTests.cs`
  - Estimate: 12 steps

- [x] ✅ **2.3 Run the gate and the demo** - Completed 2026-10-22
  - Description: Run the build, the tests, and the demo. Record the printed lines and the test total in `context.md`.
  - File(s): `context.md`
  - Estimate: 4 steps

---

## Phase 3: Each Easing Shapes the Fade (4/4 complete)

**Demo:** the developer runs `dotnet run --project tests/BrutalUi.Core.Tests/BrutalUi.Core.Tests.csproj -- --filter-method '*EachEasingShapesTheFade' --show-live-output on` and reads `linear 0.500`, `ease-in 0.315`, `ease-out 0.685`, and `ease-in-out 0.500`.
**Needs:** Phase 2
**Agent tier:** STANDARD (sonnet)

**Group A (the curves and the clock, 3 files):**

- [x] ✅ **3.1 Add the CSS Bezier curves to TransitionCurves.cs** - Completed 2026-10-22
  - Description: Edit `src/BrutalUi.Core/Style/TransitionCurves.cs` per "The Curves": the three Bezier curves, the Newton steps with the bisection fallback, and the exact ends. A switch over the four members needs no fallback branch (DS6.3 of style-parser). The remarks name the control points and the reference values.
  - File(s): `src/BrutalUi.Core/Style/TransitionCurves.cs`
  - Estimate: 8 steps

- [x] ✅ **3.2 Add the deltaTime rule to Advance** - Completed 2026-10-22
  - Description: Edit `src/BrutalUi.Core/Style/TransitionPool.cs`: `Advance` adds a `deltaTime` that is more than 0 only (`NaN` and a negative value advance nothing). Infinity ends each tween. Check that the start rule of `Track` rejects each duration that is not finite or not more than 0. Update the remarks of `UiContext.Update` for `deltaTime`.
  - File(s): `src/BrutalUi.Core/Style/TransitionPool.cs`, `src/BrutalUi.Core/Tree/UiContext.cs`
  - Estimate: 5 steps

- [x] ✅ **3.3 Write TransitionCurveTests.cs** - Completed 2026-10-22
  - Description: Write `tests/BrutalUi.Core.Tests/Style/TransitionCurveTests.cs`:
    - `EachEasingShapesTheFade` (the demo): four buttons, each with `Opacity` 0, a hover rule with `Opacity` 1, and an `Opacity` transition of 0.2 s with one easing. Hover all four, `Update(0.1f)`, and print `<easing> <opacity>`. Assert the four lines.
    - `EachEasingAtZeroHalfAndOne` (a theory, 12 rows): `Evaluate` gives 0 at 0 and 1 at 1 exactly, and 0.5, 0.31536, 0.68464, and 0.5 at 0.5 (tolerance 1e-4).
    - `EachCurveRisesAndStaysInTheRange`: 1000 samples for each easing. Each value is from 0 to 1, and no value is less than the value before it.
    - `AProgressOutsideTheRangeIsClamped`: -1 gives 0, and 2 gives 1.
    - `EachBadDurationWritesTheTargetAtOnce` (a theory): 0, -0, -1, `NaN`, `PositiveInfinity`, and `NegativeInfinity`. After a hover and `Update(0f)`, the member holds the target, and `ActiveCount` is 0. `float.Epsilon` starts a tween (the control row).
    - `ADeltaTimeOfZeroOrLessAdvancesNothing` (a theory): 0, -0.1, and `NaN`. `AnInfiniteDeltaTimeEndsEachTween`.
    - `TheCurvesAllocateNothing`: 10000 calls of `Evaluate` for each easing after `AllocationGate.ReleaseAllocationContext()` give 0 bytes.
  - File(s): `tests/BrutalUi.Core.Tests/Style/TransitionCurveTests.cs`
  - Estimate: 12 steps

- [x] ✅ **3.4 Run the gate and the demo** - Completed 2026-10-22
  - Description: Run the build, the tests, and the demo. Record the printed lines and the test total in `context.md`.
  - File(s): `context.md`
  - Estimate: 4 steps

---

## Phase 4: The Text of a Label Fades with Its Button (5/5 complete)

**Demo:** the developer runs `dotnet run --project tests/BrutalUi.Core.Tests/BrutalUi.Core.Tests.csproj -- --filter-method '*TheTextOfALabelFadesWithItsButton' --show-live-output on` and reads `button #FFFFFFFF label #FFFFFFFF`, then `button #FFE680FF label #FFE680FF` after `States = Hover` and `Update(0.1f)`, then `button #FFCC00FF label #FFCC00FF` after one more `Update(0.1f)`.
**Needs:** Phase 3
**Agent tier:** ADVANCED (opus)

**Group A (the follow walk, 4 files):**

- [x] ✅ **4.1 Add InheritsColor to Element.cs** - Completed 2026-10-22
  - Description: Edit `src/BrutalUi.Core/Tree/Element.cs`: `internal bool InheritsColor`. The style pass writes it. The doc comment states the follow rule.
  - File(s): `src/BrutalUi.Core/Tree/Element.cs`
  - Estimate: 3 steps

- [x] ✅ **4.2 Write InheritsColor in StylePass.cs** - Completed 2026-10-22
  - Description: Edit `src/BrutalUi.Core/Style/StylePass.cs`: `Restyle` sets `InheritsColor` when no matching rule and no inline value set `Color` (the condition of `Inherit`).
  - File(s): `src/BrutalUi.Core/Style/StylePass.cs`
  - Estimate: 4 steps

- [x] ✅ **4.3 Add the follow walk to TransitionPool.cs** - Completed 2026-10-22
  - Description: Edit `src/BrutalUi.Core/Style/TransitionPool.cs`: after a write of `Color`, also at the end of the tween, `FollowColor(element, color)` visits each child. It skips a child with `InheritsColor` false and a child with a running `Color` slot (a lookup in `_index`), and their subtrees. It writes the color into each other child, marks `Paint` when the value changed, and visits the children of that child. The recursion depth is the depth of the tree, as in the style pass.
  - File(s): `src/BrutalUi.Core/Style/TransitionPool.cs`
  - Estimate: 7 steps

- [x] ✅ **4.4 Write TransitionInheritanceTests.cs** - Completed 2026-10-22
  - Description: Write `tests/BrutalUi.Core.Tests/Style/TransitionInheritanceTests.cs`:
    - `TheTextOfALabelFadesWithItsButton` (the demo): `.<p>button` sets `Color` #FFFFFF with a `Color` transition of 0.2 s `Linear`. `.<p>button:hover` sets #FFCC00. The button holds a `Label` (the context gets a `RecordingRenderer` as its measurer). Print `button <color> label <color>` after each update. Assert the three lines.
    - `AGrandchildFollowsThroughAnInheritingChild`.
    - `AChildWithItsOwnColorDoesNotFollow`: a child rule sets `Color`. The child and its subtree keep their values.
    - `AChildWithItsOwnColorTransitionRunsItsOwnTween`: a child rule declares a `Color` transition of 0.4 s and sets no color. The child follows its own tween, and the walk skips it.
    - `AFollowMarksPaintOnly`: after a full `Update` with a running tween, `UpdateTransitions(0.05f)` alone gives `Paint` and no other bit on the label.
    - `AChildAddedDuringTheFadeFollows`: a child added during the tween holds the visible color of the button after the next `Update`.
  - File(s): `tests/BrutalUi.Core.Tests/Style/TransitionInheritanceTests.cs`
  - Estimate: 12 steps

- [x] ✅ **4.5 Run the gate, the demo, and the allocation gates** - Completed 2026-10-22
  - Description: Run the build, the tests, the demo, and the allocation gates of task 1.11. Record the printed lines and the test total in `context.md`.
  - File(s): `context.md`
  - Estimate: 5 steps

---

## Phase 5: A Removed Panel Ends Its Tweens (5/5 complete)

**Demo:** the developer runs `dotnet run --project tests/BrutalUi.Core.Tests/BrutalUi.Core.Tests.csproj -- --filter-method '*ARemovedPanelEndsItsTweens' --show-live-output on` and reads `active 1 panel #274176FF`, then `active 0 panel #274176FF` after `root.Children.Remove(panel)` and `Update(0.1f)`, then `active 0 panel #3060C0FF` after `root.Children.Add(panel)` and `Update(0f)`.
**Needs:** Phase 4
**Agent tier:** ADVANCED (opus)

**Group A (the lifetime, 4 files):**

- [x] ✅ **5.1 Add the tree check and the drop to TransitionPool.cs** - Completed 2026-10-22
  - Description: Edit `src/BrutalUi.Core/Style/TransitionPool.cs`: step 3 of "Advance" (the tree check: walk up the parents, then compare the top element with each root of this context by reference). A record outside the tree drops with no write and no mark. Step 2 of "Track" drops the record of an element with `IsStyled` false. Update the remarks.
  - File(s): `src/BrutalUi.Core/Style/TransitionPool.cs`
  - Estimate: 6 steps

- [x] ✅ **5.2 Add the join mark to Element.cs** - Completed 2026-10-22
  - Description: Edit `src/BrutalUi.Core/Tree/Element.cs`: a private static `MarkSubtreeJoinedCore` that sets the style bits as `MarkSubtreeStyleCore` does and sets `IsStyled` to false on each element of the subtree. `AttachToParent` calls it. Add an internal `MarkSubtreeJoined()` (the core walk and the ancestor bits). `MarkSubtreeStyle` does not change, so a class, state, name, or sheet change keeps `IsStyled`.
  - File(s): `src/BrutalUi.Core/Tree/Element.cs`
  - Estimate: 5 steps

- [x] ✅ **5.3 Call MarkSubtreeJoined in UiContext.AddRoot** - Completed 2026-10-22
  - Description: Edit `src/BrutalUi.Core/Tree/UiContext.cs`: `AddRoot` calls `MarkSubtreeJoined()` in place of `MarkSubtreeStyle()`. Update the remarks of `AddRoot`.
  - File(s): `src/BrutalUi.Core/Tree/UiContext.cs`
  - Estimate: 3 steps

- [x] ✅ **5.4 Write TransitionLifetimeTests.cs** - Completed 2026-10-22
  - Description: Write `tests/BrutalUi.Core.Tests/Style/TransitionLifetimeTests.cs`:
    - `ARemovedPanelEndsItsTweens` (the demo): the sheet of phase 1 on a panel under a root. Print `active <ActiveCount> panel <color>` after each step. Assert the three lines.
    - `ARemovedAncestorEndsTheTweensOfItsSubtree` and `ARemovedRootEndsItsTweens`.
    - `AMoveInsideTheTreeEndsTheTween`: a remove and an add between two updates. The panel holds the target after the next `Update(0f)`.
    - `AMoveIntoAnotherContextStartsNoTween`: the second context gives the target at once, and a later `Update` of the first context does not write the element.
    - `ThePoolGrowsAndNeverShrinks`: 40 tweens give a `Capacity` of 40 or more. After each tween ends, `ActiveCount` is 0 and `Capacity` holds its peak.
    - `ThePoolReusesItsRecords`: 40 panels hover in and out. After two rounds of warm-up, `AllocationGate.ReleaseAllocationContext()`, then two measured rounds (each with a state change on each panel, `Update(0.016f)` steps, and an `Update(1f)` to end each tween) give 0 bytes, and `Capacity` does not change.
  - File(s): `tests/BrutalUi.Core.Tests/Style/TransitionLifetimeTests.cs`
  - Estimate: 12 steps

- [x] ✅ **5.5 Run the gate and the demo** - Completed 2026-10-22
  - Description: Run the build, the tests, and the demo. Record the printed lines and the test total in `context.md`.
  - File(s): `context.md`
  - Estimate: 4 steps

---

## Phase 6: A Thousand Elements with Running Tweens Allocate Nothing (9/9 complete)

**Demo:** the developer runs `dotnet run --project tests/BrutalUi.Core.Tests/BrutalUi.Core.Tests.csproj -- --filter-class '*ThousandElementAllocationTests'` and reads a total of 12 with `failed: 0`.
**Needs:** Phase 5
**Agent tier:** ADVANCED (opus)

**Group A (the gate and the plants, 3 files):**

- [x] ✅ **6.1 Add BuildTransitionSheet to ThousandElementTree.cs** - Completed 2026-10-22
  - Description: Edit `tests/BrutalUi.Core.Tests/Shared/ThousandElementTree.cs`: a public static `BuildTransitionSheet()` with three transition-only rules. `.slot`: `BackgroundColor` 0.1 s `EaseOut` and `BorderColor` 0.5 s `EaseInOut`. `.muted`: `Color` 0.1 s `Linear`. `#header`: `BorderColor` 0.3 s `EaseIn`. The base sheet keeps its 16 rules. The doc comment states what each rule covers: the `selected` toggle ends the fill tween before the next style step (9 steps of 0.016 s), the hover toggle turns the slot border tween, the `muted` toggle runs the follow walk into the sidebar, and the name toggle turns the header border tween.
  - File(s): `tests/BrutalUi.Core.Tests/Shared/ThousandElementTree.cs`
  - Estimate: 5 steps

- [x] ✅ **6.2 Add the two transition gates to ThousandElementAllocationTests.cs** - Completed 2026-10-22
  - Description: Edit `tests/BrutalUi.Core.Tests/ThousandElementAllocationTests.cs`:
    - `UpdateWithRunningTransitionsAllocatesNothing`: the warm scene, `AddSheet(BuildTransitionSheet())`, one `Update`, the warm-up of `UpdateAllocatesNothingAfterWarmUp`, the release, 100 measured steps, and 0 bytes. Assert also that `Capacity` is the same before and after the measure.
    - `TheTransitionStepRunsTweens`: after one style step, `ActiveCount` is 3 or more, the slot fill and the sidebar label color lie between their two values (each channel), and the label color equals the sidebar color. After 9 more steps, the fill and the label color hold their targets, and the slot border still runs.
    - Update the class remarks. The class holds 12 tests.
  - File(s): `tests/BrutalUi.Core.Tests/ThousandElementAllocationTests.cs`
  - Estimate: 10 steps

- [x] ✅ **6.3 Plant 1 in Advance** - Completed 2026-10-22
  - Description: Copy `src/BrutalUi.Core/Style/TransitionPool.cs` to the session scratch folder. Add `GC.KeepAlive(new object());` as the first line of the record loop of `Advance`. Run `dotnet test --solution brutal-ui.sln > <scratch>/plant-advance.log 2>&1; echo "exit=$?"` and expect a value that is not 0, with `UpdateWithRunningTransitionsAllocatesNothing` in the failed list. Restore the file from the copy, and run `diff` on the two files (no output). Record the exit code and each failed test in `context.md`.
  - File(s): `src/BrutalUi.Core/Style/TransitionPool.cs` (temporary, restored), `context.md`
  - Estimate: 5 steps

- [x] ✅ **6.4 Plant 2 in Track** - Completed 2026-10-22
  - Description: The same steps with the plant as the first line of `Track`. Expect `UpdateWithRunningTransitionsAllocatesNothing` and `AParsedSheetAllocatesNothingInUpdate` in the failed list. Restore, `diff`, and record.
  - File(s): `src/BrutalUi.Core/Style/TransitionPool.cs` (temporary, restored), `context.md`
  - Estimate: 5 steps

- [x] ✅ **6.5 Run the facts line and the smoke run** - Completed 2026-10-22
  - Description: Run the facts line of task 1.1 and record `bytes per element`. Run `scripts/test-linux.sh; echo "exit=$?"` and expect the PASS line and `exit=0`. Read the 3 lines of `artifacts/test-linux/smoke.log`: `120 frames rendered`, `draw commands 2, vertices 128`, and `Update and Draw 0`. Record each result in `context.md`.
  - File(s): `context.md`
  - Estimate: 4 steps

**Group B (the record, 4 files):**

- [x] ✅ **6.6 Update docs/technical-design.md** - Completed 2026-10-22
  - Description: Edit `docs/technical-design.md`. Section 4: the transition pass in the frame order, after the style pass. Section 7, `### Transitions`: the record model, the merge rule, the start, keep, turn, and end rules, the first-style rule, the inline rule, the duration and `deltaTime` rules, the curves with their control points, the premultiplied color step, the follow rule of `Color`, the leave and join rules, the pool growth, and the cost. Update the sentence "The style pass does not read the set". Add design decision lines for the pool and the follow walk. Section 14: the five new test classes, the 2 new tests of the gate, the change of `ParsedSheetAllocationTests`, and the results of the plants. Update the cost line with the facts of task 6.5 when the value changed.
  - File(s): `docs/technical-design.md`
  - Estimate: 10 steps

- [x] ✅ **6.7 Update CHANGELOG.md** - Completed 2026-10-22
  - Description: Edit `CHANGELOG.md`: one line under `### Added` (the transitions of the five paint members in `UiContext.Update`, with `deltaTime`) and one line under `### Changed` (`Update` now reads `deltaTime`, and a rule with a transition no longer gives its target in the same `Update`). Name the feature `style-system/transitions`.
  - File(s): `CHANGELOG.md`
  - Estimate: 3 steps

- [x] ✅ **6.8 Update the human-testing list of the epic** - Completed 2026-10-22
  - Description: Edit `docs/features/style-system/human-testing.md`: add a `## transitions` section with the 6 phase demos and the Windows run of `.\scripts\test-windows.ps1`.
  - File(s): `docs/features/style-system/human-testing.md`
  - Estimate: 3 steps

- [x] ✅ **6.9 Lint the docs and write the hand-offs** - Completed 2026-10-22
  - Description: Run `python3 /home/aaronstatic/.claude/skills/asd-ste100/scripts/ste-lint.py --fail-over 2.5` on each changed doc and on this plan, and fix each hit. Write the hand-offs of Notes into `context.md`. Add a "Native Dependencies" note: none. Run the build and the tests.
  - File(s): `context.md`, each changed doc
  - Estimate: 6 steps

---

## Bugs & Issues

**Active Bugs:**
- None

**Fixed Bugs:**
- [x] ✅ 🐛 **CR-1: an inheriting child with a shorter own `Color` transition jumped back when its own tween ended** - Fixed 2026-10-22
  - Fix: review group 1. A child with its own `Color` transition never follows (Decision R2, `FollowsColor`). Review group 2 made a bad duration act as no transition (Decision R3).
- [x] ✅ 🐛 **CR-2: the keep rule ignored the transition set of the new style** - Fixed 2026-10-22
  - Fix: review group 1. The keep branch of `Track` reads the merged set.
- [x] ✅ **CR-3 to CR-6 (tests, the `Follow` field, and the doc text)** - Fixed 2026-10-22
  - Fix: review groups 2 to 4. See `code-review.md`.

---

## Technical Debt

- None

---

## Task Status Legend

- [ ] Not started
- [ ] 🔄 In progress
- [ ] ⏸️ Blocked (waiting on something)
- [x] ✅ Completed
- [x] ❌ Cancelled/Won't do

---

## Notes

### Task Estimation
One step is one tool call of an agent. A typical task costs about six steps (see `.beast-mode/phase-handoff.md` section 1).
- 🟢 Small (up to 6 steps)
- 🟡 Medium (7 to 15 steps)
- 🔴 Large (more than 15 steps, so split the task)

---

*Update this file as tasks are completed. Mark tasks with ✅ immediately when done. Add new tasks as they're discovered.*
