# interactive-elements/scroll-view - Code Review

**Reviewed:** 2026-10-26
**Reviewer:** Agent (cross-phase review of phases 1 to 6)
**Rules Applied:** 1. The hit matches the draw, 2. The offset is exact, 3. The view clips, 4. Widgets inside a view work, 5. One change, one call, 6. The wheel and the drag route right, 7. Disabled means no effect, 8. Zero allocation after warm-up, 9. No change for other elements, 10. The widget pattern and the drag rule, 11. Test quality, 12. `AGENTS.md` and the docs. The Vercel React rules do not apply to C#.
**Status:** Resolved - 14 findings, 14 checked off. CR-1 has its doc part fixed. Its code part waits for a person (follow-up question 1).

---

## Summary

| Severity | Count | Findings |
|----------|-------|----------|
| HIGH | 0 | - |
| MEDIUM | 9 | CR-1, CR-2, CR-5, CR-6, CR-7, CR-8, CR-9, CR-10, CR-11 |
| LOW | 5 | CR-3, CR-4, CR-12, CR-13, CR-14 |

The feature is in good condition. The hit walk, the draw walk, and `WidgetInput.ViewportOrigin` read one offset method. Each probe of the offset rule, the clip, the stop rule, the capture, and the disabled rule failed at least one test. Each planted allocation failed a gate. No existing test changed, and no other element changes its hit or its draw.

The review found no HIGH defect. Two MEDIUM findings are code defects in less common cases: the content size ignores content that overflows the leading edge (CR-1), and a press that an ancestor stops gives a jump at the next move (CR-2). Seven MEDIUM findings are test gaps: a mutation of a planned rule passes each test (CR-5 to CR-11). The LOW findings are a small rule mismatch after a shrink (CR-3) and doc drift (CR-4, CR-12, CR-13, CR-14).

**Baseline (repository, no change).** `dotnet build brutal-ui.sln`: `0 Warning(s)`, `0 Error(s)`. `dotnet test --solution brutal-ui.sln`: total 2456, succeeded 2454, failed 0, skipped 2 (the two GLFW window tests, no display). `BrutalUi.Core.Tests` alone in the scratch copy: total 1624, failed 0.

**Probe method.** A copy of the repository went to the scratchpad (`rsync` with no `bin`, `obj`, `.git`, or `artifacts`). The six changed files went to `../orig/` first. `Directory.Build.props` of the copy appended `$(Mut)` to `DefineConstants`. Each mutation sat behind its own `#if Mnn` symbol. Each run built `BrutalUi.Core.Tests` with `-p:Mut=Mnn -p:TreatWarningsAsErrors=false` and ran the full Core suite (1624 tests). The scenario probes were a temporary test class (`CrScenarioTests`) that printed its values through `Assert.Fail`. After the last probe, each file came back from `../orig/`, the temporary class was deleted, and `diff -rq` found no difference in `src` and `tests`.

**Mutation probes.** 55 mutations: 44 caught, 2 equivalent (M09, M13), 9 not caught (each one is a finding).

`PaintWalk` (rule 1 of the brief):

1. M01, the child origin with no offset: 9 failed (`ScrollViewScrollTests.AScrollMovesTheContent`, `AHorizontalScrollMovesTheX`, `ScrollViewHitTests.TheHitAndTheDrawAgree`, `ScrollViewClampTests.AContentShrinkShowsTheEndAtOnce`, `TheOffsetComesBackWhenTheContentGrows`, `ThePaddingAndTheBorderCount`, `ScrollViewDragTests.ADragMovesTheContentWithThePointer`, `ScrollViewNestedTests.TheOffsetsAddUpInTheHitAndTheDraw`, `ScrollViewDisabledTests.ADisabledViewKeepsItsOffsetInTheWalks`).
2. M02, the offset added in place of subtracted: the same 9 failed.
3. M03, the offset also applied to the view itself (its rect, its clip, and its `PaintContent`): 3 failed (`AScrollMovesTheContent`, `ThePaddingAndTheBorderCount`, `TheOffsetsAddUpInTheHitAndTheDraw`).
4. M04, the offset applied to the clip of the view only: the same 3 failed.
5. M05, the offset applied to each element below a view (a grandchild gets the offset two times): 0 failed (finding CR-5).

`HitWalk` (rule 2 of the brief):

6. M06, the loop reads `top.Origin` in place of `top.ChildOrigin`: 11 failed (`ScrollViewHitTests` 4, `ScrollViewSliderTests` 3, `APressOnANonPickableChildDrags`, `TheOffsetsAddUpInTheHitAndTheDraw`, `ADisabledViewKeepsItsOffsetInTheWalks`, `ScrollViewAllocationTests.TheMeasuredBodyDoesRealWork`).
7. M07, the element test reads `ChildOrigin`: 2 failed (`TheGapAndANonPickableChildHitTheView`, `APressOnANonPickableChildDrags`).
8. M08, the offset added in place of subtracted: 11 failed (`ScrollViewHitTests` 3, `ScrollViewSliderTests` 5, `TheOffsetsAddUpInTheHitAndTheDraw`, `ADisabledViewKeepsItsOffsetInTheWalks`, `TheMeasuredBodyDoesRealWork`).
9. M09, the loop reads `top.ChildOrigin` after the call to `Enter` (the stale ref): equivalent mutant, not built. `Array.Resize` copies the frames into a new array and leaves the old array as it was. The walk writes `ChildOrigin` only at the push, and `Find` clears only the new array. A read of `ChildOrigin` through the stale ref thus gives the same value. Only a write through the stale ref (such as `NextChild--`) is a defect, and the loop keeps that write before the call.
10. M56, the hit walk version of M05 (a grandchild gets the offset two times): 0 failed (finding CR-5).

`WidgetInput` (rule 3 of the brief):

11. M10, `ViewportOrigin` with no ancestor subtraction: 6 failed (`ScrollViewSliderTests` 5, `TheOriginSubtractsEachAncestorOffset`).
12. M11, `ViewportOrigin` also subtracts the offset of the element itself: 2 failed (`TheOriginSubtractsTheAncestorOffsetOnly`, `TheOriginSubtractsEachAncestorOffset`).
13. M12, `IsScroll` with no `Disabled` check: 2 failed (`ADisabledViewIgnoresTheWheel`, `ADisabledInnerViewPassesTheWheelToTheOuterView`).
14. M13, `IsScroll` with no type check: 0 failed in the feature (the run also showed the known flake `TransitionCurveTests.TheCurvesAllocateNothing`). Equivalent mutant: `ScrollEvent` accepts only `UiEventType.Scroll` (`ScrollEvent.cs:31`), so the check can never be false. The remark about the check is wrong (finding CR-13).
15. M14, `IsScroll` needs the target to be the widget: 14 failed (`ScrollViewScrollTests` 6, `ScrollViewNestedTests` 3, `ScrollViewSliderTests` 2, `ScrollViewClampTests` 2, `AScrollWalksThePathOfTheShiftedChild`).

`ScrollView`, the scroll (rule 4 of the brief):

16. M15, `HandleScroll` stops the walk for each scroll: 5 failed (`TheScrollStopsAtTheEnd`, `AScrollAtTheStartWalksOn`, `AViewLargerThanTheContentDoesNotScroll`, `ANonFiniteDeltaCountsAsZero`, `AnInnerViewAtItsEndPassesTheWheelToTheOuterView`).
17. M16, `HandleScroll` never stops the walk: 4 failed (`TheScrollStopsAtTheEnd`, `AScrollThatMovesStopsTheWalk`, `ANonFiniteDeltaCountsAsZero`, `AnInnerViewThatMovesKeepsTheWheel`).
18. M17, the sign of the delta flipped: 16 failed.
19. M18, no speed: 15 failed.
20. M19, no non-finite rule: 0 failed (finding CR-7).
21. M20, `UserSet` calls `ScrollChanged` with no change: 1 failed (`ADragMoveStopsTheWalkAlsoWithNoChange`).
22. M21, `ScrollChanged` before `Apply`: 0 failed (finding CR-6).
23. M22, a code set calls `ScrollChanged`: 7 failed (`ACodeSetClampsAndCallsNothing`, `ACodeSetWorksOnADisabledView`, `ScrollViewClampTests` 4, `AnInnerViewAtItsEndPassesTheWheelToTheOuterView`).
24. M23, `Apply` marks no `Paint`: 1 failed (`ACodeSetClampsAndCallsNothing`).
25. M24, the speed check accepts a negative value: 1 failed (`TheCreateInfoTheTypeAndTheOverflow`).
26. M25, a speed of 0 stays 0: 15 failed.
27. M26, `IsFocusable` set: 1 failed (`TheCreateInfoTheTypeAndTheOverflow`).
28. M27, the constructor sets no `Overflow.Scroll`: 7 failed (`TheCreateInfoTheTypeAndTheOverflow`, `ATypeRuleMatchesTheScrollView`, `AScrollMovesTheContent`, `TheClipOfTheViewDoesNotMove`, `ThePaddingAndTheBorderCount`, `TheOffsetsAddUpInTheHitAndTheDraw`, `TheMeasuredBodyDoesRealWork`).
29. M52, `HandleScroll` starts from the stored offset in place of `AppliedOffset()`: 1 failed (`AUserScrollAfterAShrinkStartsFromTheShownOffset`).

`ScrollView`, the clamp (rule 5 of the brief):

30. M28, `AppliedOffset()` returns `_offset`: 5 failed (`ScrollViewClampTests` 5).
31. M29, `MaxOffset()` with no trailing inset: 1 failed (`ThePaddingAndTheBorderCount`).
32. M30, `MaxOffset()` with the axes swapped: 14 failed.
33. M31, `MaxOffset()` from the first child only: 40 or more failed (the run printed the first 40).
34. M32, `MaxOffset()` from the last child only: 4 failed (`AContentShrinkShowsTheEndAtOnce`, `TheOffsetComesBackWhenTheContentGrows`, `AnAbsoluteChildCountsAndAHiddenChildDoesNot`, `AUserScrollAfterAShrinkStartsFromTheShownOffset`).
35. M33, `MaxOffset()` with no floor at 0: 1 failed (`AViewLargerThanTheContentDoesNotScroll`).
36. M34, `ClampAxis` gives the maximum for NaN: 1 failed (`ANaNOrInfiniteSetFollowsTheRule`).
37. M35, `Apply` compares with `_offset` in place of `AppliedOffset()`: 0 failed (finding CR-9).
38. M55, `Apply` writes the stored offset only on a change of the shown offset: 0 failed (finding CR-3).
39. M59, `MaxOffset()` adds the leading inset in place of the trailing inset: 0 failed (finding CR-8).

`ScrollView`, the drag (rule 6 of the brief):

40. M36, `HandlePointerDown` with no stop: 0 failed (finding CR-10).
41. M37, `HandlePointerMove` with no stop when the offset does not change: 1 failed (`ADragMoveStopsTheWalkAlsoWithNoChange`).
42. M38, the drag direction flipped: 10 failed.
43. M39, `_dragPointer` not updated at the move: 3 failed (`ADragPastTheEndComesBackAtOnce`, `AWheelDuringADragAddsUp`, `TheMeasuredBodyDoesRealWork`).
44. M40, `_dragPointer` not written at the press: 8 failed.
45. M41, `CapturesMoves` ignores `Disabled`: 1 failed (`ADisableDuringADragStopsTheDragAndTheCapture`).
46. M42, `CapturesMoves` returns `false`: 5 failed (`ADragPastTheEndComesBackAtOnce`, `ADragOutsideTheViewKeepsDragging`, `ADragMoveStopsTheWalkAlsoWithNoChange`, `ADragOnTheInnerViewMovesTheInnerViewOnly`, `TheMeasuredBodyDoesRealWork`).
47. M53, `HandlePointerMove` starts from the stored offset in place of `AppliedOffset()`: 0 failed (finding CR-11).

`ScrollViewAllocationTests` (rule 7 of the brief). Each plant is `GC.KeepAlive(new object());`:

48. M43, at the start of `ScrollView.HandlePointerDown`: 1 failed (`TheScrollViewInputPathAllocatesNothing`).
49. M44, at the start of `ScrollView.HandlePointerMove`: 1 failed (`TheScrollViewInputPathAllocatesNothing`).
50. M45, at the start of `ScrollView.MaxOffset`: 2 failed (`UpdateAndDrawAllocateNothingWithScrollViews`, `TheScrollViewInputPathAllocatesNothing`).
51. M46, at the start of `ScrollView.Apply`: 1 failed (`TheScrollViewInputPathAllocatesNothing`).
52. M47, in the `ScrollView` branch of `PaintWalk.Enter`: 2 failed (both gates).
53. M48, in the `ScrollView` branch of `HitWalk.Enter`: 1 failed (`TheScrollViewInputPathAllocatesNothing`).
54. M49, in the ancestor branch of `WidgetInput.ViewportOrigin`: 1 failed (`TheScrollViewInputPathAllocatesNothing`).
55. M50, the measured body with no drag on the view: 1 failed (`TheMeasuredBodyDoesRealWork`).

**Scenario probes.**

- (a) A `ScrollChanged` handler removes the view from the tree during a drain. A scroll, two moves, and a release follow in the same drain. Result: no exception, one call, the offset stays (0, 10), the moves and the scroll walk to the root, and the view has no parent. After an add back and a settle, a scroll and a drag work again (offset (0, 60), 2 calls). No defect.
- (b) A `ScrollChanged` handler hides `Row4` during a drag at (0, 80). The next move in the same drain gives (0, 70) with the old layout. The update then shows (0, 40) with no call. The next moves give (0, 30) and (0, 10), and `Row0` draws at (0, -10). No jump. No defect.
- (c) Game code writes `Inline.Overflow = Overflow.Visible` after the constructor. Result: the view still scrolls to (0, 40), the draw pushes no clip, and the rows draw and hit outside the view (a point at (250, 30) hits `Row2`, a point at (50, -20) hits `Row0`). The hit and the draw agree. The remarks do not state this case (finding CR-4).
- (d) A view in the root of 400 x 300 with a content of 300 x 380. No size: the view is 400 x 380 and the maximum is (0, 0) (Decision 13, stated). A size of 50 percent: the view is 200 x 150, the maximum is (100, 230), and a scroll gives (0, 40). `flex-grow: 1` and no size: the view is 400 x 380 and never scrolls (finding CR-4). A maximum height of 150: the view is 400 x 150 and scrolls. 
- (e) A `Button` at the end of a view at the offset (0, 110). A press and a release over its drawn rect hit the button and give one click, no scroll, and no `ScrollChanged` call. A second press, a move off the button, and a release give no second click and no drag. No defect.
- (f) Risk 1. After a shrink (stored (0, 80), shown (0, 40)), a wheel at the end and a growth bring (0, 80) back. A drag move at the end and a growth give (0, 40) (finding CR-3).
- (g) Risk 8. A `ScrollChanged` handler reads the offset and sets (0, 0) from code during a drag. The handler reads (0, 20), then (0, 10). The next move adds its difference to (0, 0). No defect.
- (h) Risk 5. A drag ends at the pointer (160, 20). A root capture handler then stops a press on the view at (170, 25). A move of 1 pixel up gives the offset (0, 26) from (0, 30) (finding CR-2).
- (i) Content that overflows the leading edge. With `flex-direction: column-reverse`, `Row4` is at y -80 and the maximum is (100, 0). With `justify-content: center`, `Row0` is at y -40 and the maximum is (100, 40). The content above y 0 is out of reach (finding CR-1).
- (j) The gate step (a temporary `Assert.Fail` in `TheMeasuredBodyDoesRealWork`). Step 1 gives 4 `ScrollChanged` calls, 1 `ValueChanged` call, and the value 5. Step 2 gives 4 and 2 calls, the value 5, and the offset (0, 40) (finding CR-12).

---

## Findings

### CR-1: The content size ignores content that overflows the leading edge
**Severity:** MEDIUM
**Rule:** 2. The offset is exact
**File:** `src/BrutalUi.Core/Elements/ScrollView.cs:241-267`
**Problem:** `MaxOffset()` reads only the far edge of the children, and `ClampAxis` keeps the offset at 0 or more. A layout that puts content before the leading edge of the view thus has content that no offset shows. Scenario (i): with `flex-direction: column-reverse`, the children stack up from the bottom, `Row4` is at y -80, and the maximum offset is 0. The view cannot scroll, and two rows stay out of view. With `justify-content: center`, `Row0` is at y -40, and the top 40 pixels stay out of view at each offset. A chat log with `column-reverse` is a usual case.

```csharp
float maxX = farX + insets.Z - rect.Z;      // far edge only
float maxY = farY + insets.W - rect.W;
return new float2(maxX > 0f ? maxX : 0f, maxY > 0f ? maxY : 0f);
```

The plan (Decision 4) names the far edge only, and it does not name a reverse direction or a centered content.
**Fix:** A person decides the API (see Follow-up Questions). The small fix is a doc fix now: the class remarks and section 5 of the technical design state that the view shows content from the leading edge only, and that `*-reverse` directions and a `justify-content` other than `flex-start` on the scroll axis put content out of reach. The full fix computes a near edge (the smallest `X` or `Y` of the children minus the leading inset) and lets the offset go from that edge to the far edge, but that changes the meaning of a negative offset.
**Effort:** Low (doc), Medium (code)

### CR-2: A press that an ancestor stops gives a jump at the next move
**Severity:** MEDIUM
**Rule:** 10. The widget pattern and the drag rule
**File:** `src/BrutalUi.Core/Elements/ScrollView.cs:318-346`
**Problem:** This is item 9 of Conflicts and Gaps (the known debt of the epic), but the scroll view makes it worse than the slider. When a capture handler on an ancestor stops the press, `HandlePointerDown` does not run, so `_dragPointer` keeps the position of the last drag. The press target is still the view, so the next move is a drag move and adds `old _dragPointer - new position` to the offset. Scenario (h): the last drag ended at (160, 20), the stopped press is at (170, 25), and a move of 1 pixel up moves the offset from (0, 30) to (0, 26), 4 pixels the wrong way. The jump grows with the distance between the two gestures. The slider maps the absolute pointer position, so it shows no jump in the same case.
**Fix:** Preferred: the epic fixes the debt in `UiContext` (a press that a handler stops clears the press target), so no drag starts. Local option if the epic does not fix it soon: mark the anchor as not valid when a drag ends. For example, the move handler writes `_dragPointer = new float2(float.NaN, float.NaN)` for each move that is not a drag move of this view (a hover move bubbles to the view before each press), and a drag move with a NaN anchor stores the position and moves nothing. Record the choice in `context.md`, because item 3 of the Drag Rule says that `_dragPointer` is data only.
**Effort:** Medium

### CR-3: A no-change input after a shrink keeps or drops the stored offset by its path
**Severity:** LOW
**Rule:** 2. The offset is exact
**File:** `src/BrutalUi.Core/Elements/ScrollView.cs:286-305, 392-404`
**Problem:** Open risk 1. After a shrink, the stored offset is (0, 80) and the shown offset is (0, 40). A wheel at the end returns in `HandleScroll` before `Apply`, so the stored offset stays, and a growth brings (0, 80) back. A drag move at the end calls `UserSet`, and `Apply` writes the clamped (0, 40), so a growth keeps (0, 40) (scenario (f)). The class remarks say "A user input or a set from code starts from the shown offset and stores the clamped result, so the old offset is then gone", which is false for the wheel. No test pins either result: M55 (write only on a change) passes each test.
**Fix:** Make the two user paths agree, and keep the code set as it is (a set from code always stores its clamped value, so a game that sets the shown offset keeps it). In `HandlePointerMove`, compute `next = Clamp(...)` and call `UserSet` only when `next` differs from the applied offset, as `HandleScroll` does. Keep the stop. Then reword the remark: "A user input that changes the offset, or a set from code, stores the clamped result." Extend `ScrollViewClampTests.TheOffsetComesBackWhenTheContentGrows` with a wheel and a drag move at the end before the growth, and with a code set of the shown offset (it must store it).
**Effort:** Low

### CR-4: The remarks do not state two ways to lose the clip or the scroll
**Severity:** LOW
**Rule:** 12. `AGENTS.md` and the docs
**File:** `src/BrutalUi.Core/Elements/ScrollView.cs:10-140`
**Problem:** Scenario (c): a write of `Inline.Overflow` from game code removes the clip. The content then draws and hits outside the view. The remarks state that a rule cannot remove the clip, and they warn about a write of `=` to `OnScroll`, but they do not warn about `Inline.Overflow`. Scenario (d): a view with `flex-grow: 1` and no size takes the size of its content, because the flex shrink is 0 by default, so it never scrolls. Decision 13 says "give it a width and a height, or a maximum size", but a game that fills a panel with `flex-grow` does not see that the grow is not enough. The first paragraph also ends with "The draw walk applies this rule", but the hit walk and `ViewportOrigin` apply it too.
**Fix:** Add to the clip paragraph: "A write of `Inline.Overflow` from code replaces the value of the constructor. The children then draw and hit outside the view." Add to the size paragraph: "`flex-grow` alone does not limit the view, because the view grows to its content. Use a size, a maximum size, or a percent size." Change the end of the first paragraph to "The draw walk, the hit walk, and `WidgetInput.ViewportOrigin` apply this rule."
**Effort:** Low

### CR-5: No test has a grandchild in a scrolled view
**Severity:** MEDIUM
**Rule:** 11. Test quality (guards rule 1, The hit matches the draw)
**File:** `tests/BrutalUi.Core.Tests/Elements/ScrollViewHitTests.cs:136`
**Problem:** Each child of a view in the tests has no child. A walk that applies the offset again below the view (M05 in `PaintWalk`, M56 in `HitWalk`) moves each grandchild two times, and each test passes. A list row with a label or a button in it is the usual case, and text-field and `ListView` copy this rule.
**Fix:** Extend `TheHitAndTheDrawAgree`. Before the settle, add to `Row2` a child of 50 x 20 with its own opaque fill. At the offset (37, 23), assert its drawn rect (-37, 37, 50, 20), and call `AssertCenterHits` for it. The center of the visible part of `Row2`, (100, 57), stays outside the new child, so the check of `Row2` does not change.
**Effort:** Low

### CR-6: No test checks that `ScrollChanged` comes after the change
**Severity:** MEDIUM
**Rule:** 5. One change, one call
**File:** `tests/BrutalUi.Core.Tests/Elements/ScrollViewScrollTests.cs:27`
**Problem:** M21 calls `ScrollChanged` before `Apply`, so the delegate reads the old offset. Each test passes, because no handler reads the offset.
**Fix:** Extend `AScrollMovesTheContent`: the handler also stores `view.ScrollOffset`, and the test asserts (0, 40) for that value.
**Effort:** Low

### CR-7: The non-finite delta test cannot fail
**Severity:** MEDIUM
**Rule:** 11. Test quality
**File:** `tests/BrutalUi.Core.Tests/Elements/ScrollViewClampTests.cs:146`
**Problem:** `ANonFiniteDeltaCountsAsZero` starts at the x offset 0. With no non-finite rule (M19), the NaN or infinite x gives NaN or negative infinity, and the clamp gives 0, which is the start value. Each test passes. The rule matters only at an x offset above 0: there, a NaN or an infinite x delta moves the x offset to 0.
**Fix:** Extend the test: set the offset (40, 40) from code, push the delta (NaN, -1), and assert (40, 80) with one more call. Then push (positive infinity, 0), and assert (40, 80) with no call and one more root call.
**Effort:** Low

### CR-8: No test tells the trailing inset from the leading inset
**Severity:** MEDIUM
**Rule:** 2. The offset is exact
**File:** `tests/BrutalUi.Core.Tests/Elements/ScrollViewClampTests.cs:175`
**Problem:** `ThePaddingAndTheBorderCount` uses the same padding and border on each side. A `MaxOffset()` that adds the leading inset (M59) gives the same values, and each test passes. Decision 4 needs the trailing inset.
**Fix:** Extend the test: at the end, set a padding with a left of 30 and a right of 0 (the top and the bottom stay 10), settle, and assert `MaxOffset().X` 134 (332 + 2 - 200). The leading inset gives 164.
**Effort:** Low

### CR-9: No test checks that a no-change input after a shrink calls nothing
**Severity:** MEDIUM
**Rule:** 5. One change, one call
**File:** `tests/BrutalUi.Core.Tests/Elements/ScrollViewClampTests.cs:23`
**Problem:** `Apply` compares the clamped value with `AppliedOffset()`. M35 compares it with the stored offset. After a shrink, the two differ, so a set of the shown offset marks `Paint` and a drag move at the end calls `ScrollChanged` with no visible change. Each test passes.
**Fix:** Extend `AContentShrinkShowsTheEndAtOnce`: after the draw, set the offset (0, 40) from code (the shown offset), and assert `DirtyFlags.None` for `Dirty & DirtyFlags.Paint`.
**Effort:** Low

### CR-10: No test checks that the press of a drag stops the walk
**Severity:** MEDIUM
**Rule:** 6. The wheel and the drag route right
**File:** `tests/BrutalUi.Core.Tests/Elements/ScrollViewDragTests.cs:94`
**Problem:** Item 1 of the Drag Rule says that the press stops the walk. M36 removes the stop, and each test passes. A root bubble handler then gets each press that starts a drag.
**Fix:** Extend `ADragMoveStopsTheWalkAlsoWithNoChange`: add a counter on `Root.OnPointerDown` and assert 0 after the first drain.
**Effort:** Low

### CR-11: No test checks that a drag starts from the shown offset
**Severity:** MEDIUM
**Rule:** 2. The offset is exact
**File:** `tests/BrutalUi.Core.Tests/Elements/ScrollViewDragTests.cs:145`
**Problem:** Item 2 of the Drag Rule adds the pointer difference to `AppliedOffset()`. M53 adds it to the stored offset, and each test passes. After a shrink, such a drag sticks: a move of 20 pixels from the stored (0, 80) gives (0, 60), and the clamp shows (0, 40) again.
**Fix:** Extend `APressOnANonPickableChildDrags`: release the press, set (0, 80) from code, hide `Row4`, and settle (the shown offset is (0, 40)). Press at (100, 80), assert with `HitTest` that it hits the view (it is over `Row3`), move to (100, 100), and assert (0, 20).
**Effort:** Low

### CR-12: The gate comment gives a wrong offset and a wrong slider place
**Severity:** LOW
**Rule:** 11. Test quality
**File:** `tests/BrutalUi.Core.Tests/Elements/ScrollViewAllocationTests.cs:62-64`
**Problem:** The comment says "After the drag the offset is (100, 40). The slider is at x -100 to 20". The move to (300, 40) moves the content right, so the x offset is clamped to 0. Scenario (j) reads the offset (0, 40), the value 5, and 4 and 2 calls in each step. The slider press thus has a scrolled origin on the y axis only, and the slider map reads the x axis only. The gate still measures the ancestor branch of `ViewportOrigin` (M49 failed it). The Phase 6 bullet of `context.md` holds the same wrong values (the offset (100, 40) and the values 95 and 100).
**Fix:** Change the comment to "After the drag the offset is (0, 40). The slider is at x 0 to 120 and y 0 to 20." Extend `TheMeasuredBodyDoesRealWork` with `Assert.Equal(new float2(0f, 40f), scene.Views[0].ScrollOffset)` and `Assert.Equal(4, scene.Counter.Scrolls)`. Optional: move the captured move left of the view (for example (-100, 40)) so that the x offset is above 0 and the slider map reads a scrolled x. Correct the Phase 6 bullet of `context.md` in the same fix group.
**Effort:** Low

### CR-13: The `IsScroll` remark claims a slot guard that does not exist
**Severity:** LOW
**Rule:** 12. `AGENTS.md` and the docs
**File:** `src/BrutalUi.Core/Elements/WidgetInput.cs:149`
**Problem:** The remark says "The check also reads the event type, so a copy of the handler in another slot does nothing." A `ScrollEvent` carries only `UiEventType.Scroll` (`ScrollEvent.cs:31`), and `OnScroll` and `OnScrollCapture` both carry it. A copy of the handler in `OnScrollCapture` thus runs in full. M13 (no type check) is an equivalent mutant. Rule 5 of section 5 says "`IsScroll` also reads the event type (`Scroll`)", which is true but suggests the same guard.
**Fix:** Change the remark to "The check also reads the event type, as the other checks do. Each `ScrollEvent` has the type `Scroll`, so the check does not tell `OnScroll` from `OnScrollCapture`." Keep the code.
**Effort:** Low

### CR-14: Section 14 of the technical design states a check that the slider tests do not make
**Severity:** LOW
**Rule:** 12. `AGENTS.md` and the docs
**File:** `docs/technical-design.md:1140-1141`
**Problem:** Line 1140 says "`ScrollViewSliderTests` (6 tests) checks a slider in a scrolled view: the map, the hit, and the draw agree." No test of that class draws. `ScrollViewHitTests.TheHitAndTheDrawAgree` checks the draw. Line 1141 says "a press and a move on the slider at a scrolled origin", but the x offset is 0 at that time (CR-12).
**Fix:** Change line 1140 to "checks a slider in a scrolled view: the press, the drag, the capture, and the wheel map through the offset." Change line 1141 to "a press and a move on the slider at the offset (0, 40)".
**Effort:** Low

---

## What's Already Good

- **One offset method.** The hit walk, the draw walk, `ViewportOrigin`, the get of `ScrollOffset`, and each input path read `AppliedOffset()`. Each walk mutation (M01 to M04, M06 to M08, M10, M11) failed at least two tests, and `TheHitAndTheDrawAgree` checks the center of each drawn rect.
- **The stale ref.** The hit walk copies `ChildOrigin` into a local before the call to `Enter`, next to the other copies. M09 is an equivalent mutant (see the probe list).
- **Allocation.** Each plant of the brief failed a gate (M43 to M49), and the plant of task 6.2 failed it too. The handlers are three `static readonly` delegates. The hot path has no closure, no boxing, and no string. `IDragCapture { CapturesMoves: true }` is a type test on a class, with no box.
- **The stop rule and the capture.** M15, M16, M37, M41, and M42 each failed a test. Scenario (a) shows that a view that a handler removes during a drain gets no more moves, and that it works again after an add back.
- **Widgets inside a view.** Scenario (e) clicks a button inside a scrolled view one time, and a press on the button gives no drag. The slider tests map a press and a drag through the offset.
- **Risk 2 (a set before the first `Update`).** The class remarks state that the set gives (0, 0) and that a later layout does not bring the value back, and `ASetBeforeTheFirstUpdateGivesZero` pins it. That is enough for this feature. A pending set is a follow-up question.
- **Risk 3 (the cost of the clamp at use).** For one view with N children, one scroll runs the child loop 5 times: 1 in the hit walk and 4 in `HandleScroll` (`AppliedOffset`, `Clamp`, and `Clamp` and `AppliedOffset` in `Apply`). One drag move runs it 4 times. One slider press inside a view runs it 1 time in the hit walk and 1 time for each ancestor view in `ViewportOrigin`. One frame runs it 1 time for each shown view in the draw. The hit walk also runs it for a view when the point is outside the clip of the view. Each loop reads one rect for each child. For 1000 children, that is about 5000 rect reads for each event, a few microseconds. This is acceptable until the `ListView` epic adds virtualization (follow-up).
- **Risk 6 (a press on a view that cannot scroll stops the walk).** The slider press stops the walk in the same way, so the view makes nothing worse. Follow-up question.
- **Risk 7 (`-0f`).** `-0f == 0f`, so the speed is `DefaultScrollSpeed`, and `TheSpeedScalesTheDelta` pins it. A negative zero is not a negative speed. No finding.
- **Risk 8 (`_dragPointer` before `UserSet`).** Scenario (g): a `ScrollChanged` handler reads the new offset and can set an offset from code. The next move adds its difference to the offset of the code set. The raw pointer position is the right anchor (Decision 9).
- **Risk 9 (the code set in each gate step).** The set makes each step hit the same elements, and it adds the `Apply` path to the measure. It hides no path of rule 8: the two scrolls change the offset, the drag runs the press, a move, a captured move, and the release, and the slider runs its press and move at a scrolled y. The no-change paths (a wheel at an end, a drag move at an end) are early returns with no call that can allocate.
- **Risk 10 (the type test for each element).** `ScrollView` is sealed, so `element is ScrollView` is one compare of the method table. It runs after the hidden checks in both walks.
- **Risk 11 (extra assertions).** Each extra assertion checks the rule of the test name or a value that the rule needs (for example, `MaxOffset()` in the code set test and the hand-computed rects in `TheHitAndTheDrawAgree`). No test lost its main check.
- **Risk 12 (text-field).** Section 8 says that the walk calls `PaintContent` after the clip push of the element, so "the clip of an element also clips its own content" (line 780). Section 5 says that the view itself, its clip, and its `PaintContent` use the origin with no offset. text-field can thus clip with `Overflow.Hidden` and move its text in `PaintContent` with no change of the walks.
- **Rules 7 and 9.** The disabled mutations M12 and M41 failed a test. No existing test file changed, and the full suite passes.
- **Docs.** Sections 5, 8, and 9 of the technical design state the Offset Rule of the Walks in the same words as the code comments. The changelog names `ScrollView` and the change of the walks.

---

## Follow-up Questions

1. **Leading overflow (CR-1).** Must the view reach content before its leading edge (`column-reverse`, `justify-content: center`)? Option A: a doc rule (start-aligned content only). Option B: an offset range from the near edge to the far edge, with a possible negative offset. Option B changes the meaning of `ScrollOffset`.
2. **A pending set (risk 2).** Must a set before the first `Update` keep its value until the first layout, so that a game can restore a saved offset at build time?
3. **`Hover` after a wheel (risk 4).** `Hover` and `PointerHit` stay on the old element until the next pointer event. A press after a wheel hit-tests again, so a click is correct. Only the hover style is old. Must the drain run a hit after a scroll that moves a view?
4. **The known debt of the epic (risk 5).** `RoutingGate.PointerOverUi` follows the hit during a captured view drag, as for the slider. `:disabled` on an ancestor does not block the view, as for the slider. The stopped press is CR-2, because the view makes it worse.
5. **A press on a view that cannot scroll (risk 6).** The press stops the walk, and a root bubble handler does not get it. Must the press stop the walk only when `MaxOffset()` is above 0 on an axis?
6. **The cost of the clamp at use (risk 3).** The `ListView` epic can store the content extent once for each layout pass, call `MaxOffset()` one time in `Apply` and in `HandleScroll`, and skip `AppliedOffset()` in `HitWalk.Enter` when the point is outside the child clip.
7. **`flex-grow` sizing (CR-4).** Must the layout give a scroll container an automatic minimum size of 0 (the CSS rule), so that `flex-grow: 1` with `flex-shrink: 1` limits the view? widget-sample owns the size rules.

---

## Recommended Fix Order

| Order | Finding | Impact | Effort | Fix group (one file) |
|-------|---------|--------|--------|----------------------|
| 1 | CR-5 | MEDIUM | Low | `ScrollViewHitTests.cs` |
| 2 | CR-6 | MEDIUM | Low | `ScrollViewScrollTests.cs` |
| 3 | CR-7 | MEDIUM | Low | `ScrollViewClampTests.cs` |
| 4 | CR-8 | MEDIUM | Low | `ScrollViewClampTests.cs` |
| 5 | CR-9 | MEDIUM | Low | `ScrollViewClampTests.cs` |
| 6 | CR-10 | MEDIUM | Low | `ScrollViewDragTests.cs` |
| 7 | CR-11 | MEDIUM | Low | `ScrollViewDragTests.cs` |
| 8 | CR-1 | MEDIUM | Low (doc) | `ScrollView.cs` (with section 5 of `technical-design.md`) |
| 9 | CR-2 | MEDIUM | Medium | `ScrollView.cs` (or the epic fix in `UiContext.cs`) |
| 10 | CR-3 | LOW | Low | `ScrollView.cs` (test in `ScrollViewClampTests.cs`) |
| 11 | CR-4 | LOW | Low | `ScrollView.cs` (remarks) |
| 12 | CR-12 | LOW | Low | `ScrollViewAllocationTests.cs` (with `context.md`) |
| 13 | CR-13 | LOW | Low | `WidgetInput.cs` |
| 14 | CR-14 | LOW | Low | `technical-design.md` |

The `ScrollView.cs` group holds four findings. Split it into two fix groups: CR-1, CR-2, and CR-3 (code), and CR-4 (remarks). The test of CR-3 makes `ScrollViewClampTests.cs` a fourth item in that file, so run CR-3 after the group of CR-7, CR-8, and CR-9.

---

## Progress

- [x] CR-1: The content size ignores content that overflows the leading edge (doc part fixed by `review-g6.md` and `review-g9.md`: the class remarks and section 5 of the technical design state that the view shows content from the leading edge only. The code part is left open on purpose: an offset range from the near edge changes the meaning of `ScrollOffset`, so a person decides it. Follow-up question 1 holds it)
- [x] CR-2: A press that an ancestor stops gives a jump at the next move (fixed by `review-g5.md`: a move that is not a drag move writes a NaN anchor, and a drag move with a NaN anchor sets the anchor and moves nothing. `TheCaptureEndsAtTheRelease` pins it. The old anchor rule fails it with (0, 21))
- [x] CR-3: A no-change input after a shrink keeps or drops the stored offset by its path (fixed by `review-g5.md`: a drag move calls `UserSet` only on a change of the shown offset, as the wheel does. `TheOffsetComesBackWhenTheContentGrows` pins it. A call with no change check fails it with (0, 40))
- [x] CR-4: The remarks do not state two ways to lose the clip or the scroll (fixed by `review-g6.md`: the class remarks state the `Inline.Overflow` write, the `flex-grow` case, and the three readers of the offset rule. The non-`///` lines keep their fingerprint)
- [x] CR-5: No test has a grandchild in a scrolled view (fixed by `review-g1.md`: `TheHitAndTheDrawAgree` draws and hits a grandchild. A double offset fails it)
- [x] CR-6: No test checks that `ScrollChanged` comes after the change (fixed by `review-g2.md`: the delegate of `AScrollMovesTheContent` reads the new offset. A call before `Apply` fails it)
- [x] CR-7: The non-finite delta test cannot fail (fixed by `review-g3.md`: the test starts at (40, 40). No non-finite rule fails it)
- [x] CR-8: No test tells the trailing inset from the leading inset (fixed by `review-g3.md`: a left padding of 30 and a right padding of 0 give the maximum x 134. The leading inset fails it)
- [x] CR-9: No test checks that a no-change input after a shrink calls nothing (fixed by `review-g3.md`: a set of the shown offset after a shrink marks no `Paint`. A compare with the stored offset fails it)
- [x] CR-10: No test checks that the press of a drag stops the walk (fixed by `review-g4.md`: the root press counter reads 0. A press with no stop fails it)
- [x] CR-11: No test checks that a drag starts from the shown offset (fixed by `review-g4.md`: a drag after a shrink starts from (0, 40). A drag from the stored offset fails it)
- [x] CR-12: The gate comment gives a wrong offset and a wrong slider place (fixed by `review-g7.md`: the comment gives the offset (0, 40) and the slider at x 0 to 120. `TheMeasuredBodyDoesRealWork` asserts the offset (0, 40) and 4 scroll calls. The phase 6 bullets of `context.md` give the real values)
- [x] CR-13: The `IsScroll` remark claims a slot guard that does not exist (fixed by `review-g8.md`: the remark says that the check does not tell `OnScroll` from `OnScrollCapture`. The non-`///` lines keep their fingerprint)
- [x] CR-14: Section 14 of the technical design states a check that the slider tests do not make (fixed by `review-g9.md`: section 14 states the press, the drag, the capture, and the wheel through the offset, and the gate press at the offset (0, 40))
