# interactive-elements/scroll-view - Context & Decisions

**Last Updated:** 2026-10-27 07:26
**Current Phase:** Complete (phases 1 to 6 and the cross-phase review are done)
**Status:** ✅ Ready for Review

---

## Quick Status

**What's Done:**
- ✅ The plan is accepted (6 phases, 29 tasks). The person confirmed Q1 to Q4 of the plan Notes on 2026-10-26, each with the recommended option: Q1 the unit test demo, Q2 a press on the view itself starts a drag, Q3 the clamp at use (on read), Q4 no focus and no key.
- ✅ The feature docs exist (`tasks.md`, `context.md`, `inbox.md`).
- ✅ The branch `feat/interactive-elements/scroll-view` holds this feature. slider is committed (`8137146`).
- ✅ Phase 1, tasks 1.1 to 1.7: the baseline, `IsScroll`, `ScrollView`, the child origin of `PaintWalk`, `ScrollViewTree`, and `ScrollViewScrollTests` (10 tests). The verifier gave PASS and Demo: PASS. See "Phase 1 Results".
- ✅ Phase 2, tasks 2.1 to 2.6 (group 1): the blast radius, the child origin of `HitWalk`, the ancestor offset in `ViewportOrigin`, `ScrollViewHitTests` (6 tests), and `ScrollViewSliderTests` (6 tests). See "Phase 2 Results". The verifier gave PASS and Demo: PASS.
- ✅ Phase 3, tasks 3.1 to 3.3: the drag, the capture, and `ScrollViewDragTests` (9 tests). The verifier gave PASS and Demo: PASS.
- ✅ Phase 4, tasks 4.1 to 4.3: the clamp at use, the NaN rule, and `ScrollViewClampTests` (10 tests). The verifier gave PASS and Demo: PASS.
- ✅ Phase 5, tasks 5.1 to 5.4: the nested rule and the disabled rule in the remarks, `ScrollViewNestedTests` (6 tests), and `ScrollViewDisabledTests` (5 tests). The verifier gave PASS and Demo: PASS.

- ✅ Phase 6, tasks 6.1 to 6.6: `ScrollViewAllocationTests` (3 tests), the plant, sections 5, 8, 9, and 14 of the technical design, and the changelog. The verifier gave PASS and Demo: PASS.

**What's Next:**
- ✅ The cross-phase review (`code-review.md`): 14 findings checked off, verifier PASS.
- ✅ The draft plan of `interactive-elements/text-field` exists (`docs/features/interactive-elements/text-field/implementation.md`, 6 phases, 34 tasks).
- 📋 `/autorun-epic` commits this feature and runs text-field next.

**Blockers:**
- None

---

## Key Files

### Core Implementation
- `src/BrutalUi.Core/Elements/ScrollView.cs` - The `ScrollView` widget (phases 1, 3, 4, and 5).
- `src/BrutalUi.Core/Elements/WidgetInput.cs` - `IsScroll` (phase 1) and the ancestor offset in `ViewportOrigin` (phase 2).
- `src/BrutalUi.Core/Rendering/PaintWalk.cs` - The child origin of the draw walk (phase 1).
- `src/BrutalUi.Core/Input/HitWalk.cs` - The child origin of the hit walk (phase 2).

### Related Files
- `tests/BrutalUi.Core.Tests/Elements/ScrollViewTree.cs` - The scene of each scroll view test (phase 1).
- `docs/features/interactive-elements/scroll-view/implementation.md` - Implementation plan.

---

## Phase 1 Baseline (task 1.1)

Recorded before any change of phase 1.

- Base commit: `81371461bf52264cb968545e84ceac47abe16b63` (`8137146`, `feat: interactive-elements/slider`). This run makes no commit, so `<base>` is `HEAD`.
- `git ls-files src/BrutalUi.Core/Elements/Slider.cs src/BrutalUi.Core/Input/IDragCapture.cs` gives two lines. slider is in the base commit.
- `git status --porcelain -- src tests` gives no line.
- `dotnet build brutal-ui.sln`: `0 Warning(s)`, `0 Error(s)`.
- `dotnet test --solution brutal-ui.sln`: total 2401, succeeded 2399, failed 0, skipped 2 (the two GLFW window tests, no display). `BrutalUi.Core.Tests`: 1569.
- The regression set at the base: `PointerPressTests` 8, `PointerStateEdgeTests` 8, `PointerHoverTests` 5, `PointerAllocationTests` 3, `ClickFocusTests` 8, `FocusOwnerTests` 8, `TabNavigationTests` 12, `FocusStateEdgeTests` 8, `FocusAllocationTests` 3, `RoutingGateTests` 8, `ScrollRoutingTests` 4, `HitTestTests` 6, `HitTestRuleTests` 11, `PaintWalkTests` 29, `PaintContentTests` 9, `RenderValueTests` 16, `ReleaseOrderTests` 4, `ButtonClickTests` 8, `ButtonKeyTests` 9, `ToggleTests` 10, `ButtonTextTests` 9, `WidgetDisabledTests` 7, `WidgetAllocationTests` 4, `ChordClickTests` 8, `SliderPressTests` 10, `SliderDrawTests` 6, `SliderDragTests` 9, `DragCaptureTests` 9, `SliderStepTests` 9, `SliderKeyTests` 9, `SliderDisabledTests` 6, `SliderAllocationTests` 3. Each class passes.

### Codebase Findings (confirmed at the base)

- `PaintWalk.Enter` step 6 pushes a clip of the padding box for each element with an overflow other than `Overflow.Visible`.
- `PaintWalk.Enter` step 3 and `HitWalk.Enter` compute `origin = parentOrigin + rect.XY`.
- `LayoutEngine.Lines.cs` line 316 and `LayoutEngine.cs` line 1122 read `Overflow.Scroll` (no main-axis maximum for the children).
- `InlineStyle.Overflow` writes the inline value, and an inline value wins over each rule.
- `UiContext.RoutePointer` hit-tests a scroll with no target. The walk bubbles from the hit to the root, so a scroll over a child reaches the bubble slot `OnScroll` of the view (`ScrollRoutingTests.AScrollGoesToTheElementUnderThePointer`).

### Blast Radius of `PaintWalk`

- Callers of `PaintWalk.Walk`: `UiContext.Draw` only (`src/BrutalUi.Core/Tree/UiContext.cs` line 702). `UiContext` holds the stack `_paintStack` of type `PaintFrame[]` (line 100). It reads no field of a frame.
- Uses of `PaintFrame.Origin`: `PaintWalk.cs` only (the loop line that enters a child, and the frame push in step 8). `HitWalk.cs` reads `top.Origin` of its own `HitFrame` struct, which this phase does not change.
- `HitWalk.cs` calls `PaintWalk.PaddingBounds` and `PaintWalk.Intersect`. This phase does not change them.
- Tests that read a paint frame: none. No test file names `PaintFrame` or `PaintWalk`. Each draw test reaches the walk through `UiContext.Draw`. Only a `ScrollView` changes the child origin, so each draw test keeps its result.

### Blast Radius of `HitWalk` and `ViewportOrigin` (task 2.1)

Recorded before any change of phase 2.

- Callers of `HitWalk.Find`: `UiContext.HitTest` only (`src/BrutalUi.Core/Tree/UiContext.cs` line 1018). `UiContext` holds the stack `_hitStack` of type `HitFrame[]`.
- Callers of `UiContext.HitTest` in `src`: `UiContext.RoutePointer` only (line 1652). It hit-tests each pointer entry and each scroll with no caller target. It stores the hit in `PointerHit`, moves `Hover`, writes the hit as the target, and then applies the drag capture of slider. Thus the change reaches each press, release, move, and scroll, the click focus, the routing gate, and the drag capture.
- Callers of `UiContext.HitTest` in `tests`: `HitTestTests`, `HitTestRuleTests`, `PointerAllocationTests`, `FocusAllocationTests`, `WidgetAllocationTests`, and `SliderAllocationTests`. No scene of these tests holds a `ScrollView`, so the child origin equals the origin and each result stays.
- Tests that read `HitStackSlots`: `HitTestTests.AssertStackClean` only. It reads `HitFrame.Element` of each slot. The new field `ChildOrigin` keeps the test valid, and the `finally` block still clears each used frame.
- Callers of `WidgetInput.ViewportOrigin`: `Slider.ValueAtPointer` only (`src/BrutalUi.Core/Elements/Slider.cs` line 370). It maps the x of a press and of a drag move to a value. `Button` and `Toggle` do not call it.
- No other code reads `HitFrame`. `PaintWalk` has its own `PaintFrame`, which phase 1 changed.

## Important Decisions

The plan holds Decisions 1 to 13 and the confirmed items Q1 to Q4. This section records each decision that the build adds.

### Phase 1

- A NaN in the clamp gives 0 on its axis. Positive infinity gives the maximum, and negative infinity gives 0. `ClampAxis` uses `!(value > 0f)`, so a code set of NaN is safe. A NaN delta at an offset above 0 thus moves the offset to 0 in this phase. Phase 4 adds the non-finite delta rule and can change this.
- `MaxOffset` skips a NaN far edge (the `>` compare is false for NaN), and a NaN result gives 0. A hidden child has a zero rect, so it adds no far edge. An absolute child counts as each other child.
- A create-info speed of `-0f` gives `DefaultScrollSpeed`, because `-0f == 0f`. It does not throw.
- `_scrollSpeed` is `readonly`, because `ScrollSpeed` is get-only.
- `HandleScroll` and `Apply` compare the two axes with `==` on each component. The code uses no `float2` operator.
- The class remarks of `ScrollView` state the offset rule for the draw walk only. Phase 2 extends the remark to the hit walk.
- The loop of `PaintWalk.Walk` keeps the local name `origin` for the value that it reads from `top.ChildOrigin`. The name is the parameter `parentOrigin` of `Enter`, so the diff stays at one line.
- `ScrollViewTree.RowFill(int)` gives the color `(40 + 40n, 200 - 30n, 10 + 20n, 255)`. Each row color differs from the default white thumb of the slider.
- `ATypeRuleMatchesTheScrollView` puts the fill and `overflow: visible` in one rule. The fill proves that the rule matches, so the overflow check cannot pass for the wrong reason (the sibling gotcha).
- The tests add assertions that the brief did not list, with no extra test method: the demo also checks `Row2`, `Row4`, and a clip depth of 0. `TheSpeedScalesTheDelta` also checks the default constructor and `-0f`. `ACodeSetClampsAndCallsNothing` also checks a set before the first update and `MaxOffset()` (100, 80). `TheCreateInfoTheTypeAndTheOverflow` also checks negative infinity, the message, and the view rect.

### Phase 1 Results (task 1.7)

- `dotnet build brutal-ui.sln`: `0 Warning(s)`, `0 Error(s)`.
- `dotnet test --solution brutal-ui.sln`: total 2411, succeeded 2409, failed 0, skipped 2 (GLFW, no display). `BrutalUi.Core.Tests`: 1579 (1569 + 10).
- `ScrollViewScrollTests`: 10 of 10 pass. Each class of the regression set passes with the count of the base.
- `git diff HEAD --stat -- tests/` gives no line: no existing test file changed.
- `HitWalk.cs`, `UiContext.cs`, `Element.cs`, and `EventWalk.cs` have no diff.
- The code diff of `PaintWalk.cs`: the `PaintFrame` field, the loop line, the child origin after step 3, and the frame push of step 8. The other lines are doc comments.
- `scripts/test-linux.sh`: `test-linux: PASS build, test, gpu, smoke (120 frames) in 15 s`, `exit=0`.

### Migration Risks and Follow-up (phase 1)

- In phase 1, the hit walk and `WidgetInput.ViewportOrigin` ignore the offset. A press on a child of a scrolled view hits the layout rect, not the drawn rect, and a slider in a scrolled view maps the press with no offset. Phase 2 fixes both.
- `ScrollOffset` keeps an offset that a later layout makes too large (the content shrinks). Phase 4 adds the clamp at use.

### Phase 2

- `HitWalk.Enter` computes the child origin after the clip of step 3, with the code of `PaintWalk`, and writes it in the frame push of step 4. The loop reads `top.ChildOrigin` into the local `origin`, next to the copy of the clip. The element test keeps `top.Origin`, so the border box and the clip of the view do not move.
- `ViewportOrigin` tests `!ReferenceEquals(current, element)` before `current is ScrollView`, so a view does not subtract its own offset. The other code of the loop stays.
- `TheHitAndTheDrawAgree` sets the slider value to 50 and an inline thumb color. At the value 0 and the offset (37, 23), the thumb draws at x -37 to x -17, outside the clip, so the test would have no visible thumb part. The helper asserts that each visible part is not empty, so the test cannot pass for the wrong reason. It also asserts the hand-computed rects of the draw.
- `AWheelOverTheSliderScrollsTheView` sets the value 25 from code and scrolls over the thumb, at `SliderAt(25, (20, 0))` = (15, 50). It asserts the hit test of that point first, so the scroll surely starts on the slider.
- `AScrollWalksThePathOfTheShiftedChild` reads the target in a capture handler of the root and counts the bubble calls of `Row2`. The delta (0, -1) moves the offset from (0, 40) to (0, 80).
- The tests add assertions that the brief did not list, with no extra test method: `PointerHit` in the demo, the hover of the slider and the scroll calls in the drag past the edge, and the origins of the root and of `Outside` in the origin test.
- A mutation check, restored after the run: the loop read of `top.Origin` in place of `top.ChildOrigin` fails 7 of the 12 new tests. `TheClipOfTheViewDoesNotMove`, `AnElementOutsideTheViewKeepsItsHit`, `AWheelOverTheSliderScrollsTheView`, `AScrollAndAPressInOneDrainUseTheNewOffset`, and `TheOriginSubtractsTheAncestorOffsetOnly` pass with it, because their points hit the same element with each rule (they guard the parts that must not move). A `ViewportOrigin` that also subtracts the own offset fails `TheOriginSubtractsTheAncestorOffsetOnly` only.

### Phase 2 Results (task 2.6)

- `dotnet build brutal-ui.sln`: `0 Warning(s)`, `0 Error(s)`.
- `dotnet test --solution brutal-ui.sln`: total 2423, succeeded 2421, failed 0, skipped 2 (GLFW, no display). That is 12 more than after phase 1. `BrutalUi.Core.Tests`: 1591 (1579 + 12).
- `ScrollViewHitTests` 6 of 6, `ScrollViewSliderTests` 6 of 6 (the demo passes), and `ScrollViewScrollTests` 10 of 10.
- Each class of the regression set passes with the count of the base. The other `Input/` classes pass: `EventAllocationTests` 3, `EventKindTests` 12, `EventQueueTests` 9, `EventWalkEdgeTests` 5, `EventWalkTests` 13, and `WindowCallbackTests` 4.
- `git diff HEAD --stat -- tests/` gives no line: no existing test file changed.
- `Slider.cs`, `UiContext.cs`, `IDragCapture.cs`, and `Element.cs` have no diff. The diff of `PaintWalk.cs` is the phase 1 change only.
- The code diff of `HitWalk.cs`: the `HitFrame.ChildOrigin` field, the loop line, the child origin after step 3, and the frame push of step 4. The other lines are doc comments.

### Migration Risks and Follow-up (phase 2)

- The class remarks of `ScrollView` still state the offset rule for the draw walk only. Phase 3 changes `ScrollView.cs` and can extend the remark to the hit walk and to `ViewportOrigin`.
- Phase 4 changes `AppliedOffset()` to clamp by the last layout. The hit walk, the draw walk, and `ViewportOrigin` call `AppliedOffset()` and never read `_offset`, so they get the clamp with no change.
- Phase 5 (nested views) can reuse the rule with no change: each level subtracts its own offset, and `ViewportOrigin` subtracts the offset of each ancestor view.
- `HitWalk.Find` now runs a type test (`element is ScrollView`) for each entered element. The cost is one type check per element, with no allocation.

### Phase 4

- `AppliedOffset()` returns `Clamp(_offset)`. The walks, `ViewportOrigin`, the drag, and the get of `ScrollOffset` read it, so the hit walk, the draw walk, and the map follow the clamp with no change to `HitWalk.cs`, `PaintWalk.cs`, or `WidgetInput.cs`. The get writes nothing, so a clamp from a content change calls nothing.
- `Apply` writes the clamped value also when it equals the applied offset. A set or a user input to the shown offset thus replaces a stored offset that the last layout clamped. It marks the paint only when the shown offset changes. The brief did not settle this case.
- `HandleScroll` counts a non-finite delta component as 0 before the speed. A delta of (NaN, -1) at the offset 0 gives (0, 40). The phase 1 behavior (a NaN delta moves an offset above 0 to 0) is gone.
- `Clamp` and `ClampAxis` stay as the only clamp code. `ClampAxis` already gave 0 for NaN and negative infinity and the maximum for positive infinity, so the code needed no change.
- The class remarks now state the content size rule, the clamp at use (shrink shows the end, growth brings the old offset back), the NaN rule, and the set before the first update.
- `MaxOffset()` (internal) is visible to the tests. The tests assert it in the shrink, padding, absolute child, large view, and resize cases.
- The tests add assertions that the brief did not list, with no extra test method: `Row2` and `Row3` in the demo, a set of infinity at the end of the padding test, and the mixed NaN and infinity sets in `ANaNOrInfiniteSetFollowsTheRule`.

### Phase 4 Results (task 4.3)

- `dotnet build brutal-ui.sln`: `0 Warning(s)`, `0 Error(s)`.
- `dotnet test --solution brutal-ui.sln`: total 2442, succeeded 2440, failed 0, skipped 2 (GLFW, no display). That is 10 more than after phase 3. Core: 1610.
- `ScrollViewClampTests` 10 of 10 (the demo passes). `ScrollViewScrollTests` 10, `ScrollViewHitTests` 6, `ScrollViewSliderTests` 6, `ScrollViewDragTests` 9: each passes. Each class of the regression set passes with the count of the base.
- `HitWalk.cs`, `PaintWalk.cs`, and `WidgetInput.cs` have no change in this phase.

### Phase 5

- Task 5.1: the remarks of `ScrollView.cs` got three paragraphs: the nested rule (the offsets add up, the clips intersect, `ViewportOrigin` subtracts the offset of each ancestor view), the wheel in a nested view (the stop rule passes the wheel at the end of an inner view to the outer view), and the disabled rule (Decision 11). No code line changed.
- `ScrollViewNestedTests` builds the scene in a private nested class `Scene` that a private static method `BuildScene()` creates. The outer view is the only root (no plain root element), at (0, 0). `Inner` is a child of the outer view at (0, 40). The brief did not settle the root, so this choice keeps the scene small.
- `TheOffsetsAddUpInTheHitAndTheDraw` also asserts `InnerRow0` at (10, -10, 200, 40), the outer clip (0, 0, 200, 100), `PointerHit`, and a clip depth of 0. `TheOriginSubtractsEachAncestorOffset` also asserts the outer origin (0, 0). `ADragOnTheInnerViewMovesTheInnerViewOnly` also asserts that the press point hits `Inner`. `ADisabledViewKeepsItsOffsetInTheWalks` also asserts `PointerHit` and the offset after the move. `ACodeSetWorksOnADisabledView` also sets (500, 500) and reads the maximum (100, 80). No test method was added.
- `ADisableDuringADragStopsTheDragAndTheCapture` is discriminating: with the capture, the move to `PastRight` would move the offset from (0, 30) to (0, 0).
- All 11 new tests passed on the first run. No test failed on the code, so no code change was needed.

### Phase 5 Results (task 5.4)

- `dotnet build brutal-ui.sln`: `0 Warning(s)`, `0 Error(s)`.
- `dotnet test --solution brutal-ui.sln`: total 2453, succeeded 2451, failed 0, skipped 2 (GLFW, no display). That is 11 more than after phase 4.
- `ScrollViewNestedTests` 6 of 6 (the demo passes), `ScrollViewDisabledTests` 5 of 5. `ScrollViewScrollTests` 10, `ScrollViewHitTests` 6, `ScrollViewSliderTests` 6, `ScrollViewDragTests` 9, `ScrollViewClampTests` 10. Each class of the regression set passes with the count of the base (`WidgetDisabledTests` 7 and `SliderDisabledTests` 6 with no edit).
- The diff of `ScrollView.cs` in this phase is `///` lines only. `HitWalk.cs`, `PaintWalk.cs`, and `WidgetInput.cs` have no change in this phase.

### Phase 6

- `ScrollViewAllocationTests` copies the gate shape of `SliderAllocationTests` (2 warm-up rounds, a `Scene` class, `AllocationGate.ReleaseAllocationContext()` before the measure, a measured body with no lambda, no string, and no Assert). All 3 tests passed on the first run.
- The viewport of the gate is 800 x 1200, because the root is 400 x 1000 (10 views of 100). The first view holds the slider as its second child (after row 0), so the rows start at y 0, 60, 100. Row 1 (y 60 to 100) is the first row that is not pickable.
- Each input step first sets `ScrollOffset = default` on the first view from code. A step then starts from the same state, and the fixed points hit the same elements in each step. The brief did not list this reset. The set allocates nothing.
- The step moves the pointer from (100, 80) to (100, 60) and then to (300, 40). The move to (300, 40) moves the content right, so the x offset clamps to 0 and the offset then is (0, 40). The slider sits at x 0 to 120, y 0 to 20. The scrolled origin is on the y axis only, and the slider map reads the x axis only. The slider ends each step at the value 5. A step calls `ScrollChanged` 4 times and `ValueChanged` 2 times (the press differs from the value of the last step). The first step after the build calls `ValueChanged` 1 time.
- `TheMeasuredBodyDoesRealWork` asserts 4 `ScrollChanged` calls, at least one `ValueChanged` call, the offset (0, 40) on the first view, an offset of 0 on the second view, a walk count above 0, and 10 `PushClip` calls. It asserts no exact walk count.
- Task 6.3 also replaced the future-tense sentence in the slider drag bullet of section 5 ("When scroll-view adds a scroll offset ...") with the present rule, because the sentence was now false. Task 6.4 added a bullet to the hit rules of section 9 item 3, and a sentence to the rect form rule of section 8.
- The `CHANGELOG.md` "Changed" entry also names the capture of a dragged `ScrollView`, because it is a behavior change of the pointer step for a second type.

### Phase 6 Plant Result (task 6.2)

- The copy: `ScrollView.cs.orig` in the scratchpad. The plant: `GC.KeepAlive(new object());` as the first line of `ScrollView.HandleScroll`.
- `dotnet test --solution brutal-ui.sln > /dev/null 2>&1; echo "exit=$?"` gave `exit=2`. The full run gave total 2456, failed 1: `ScrollViewAllocationTests.TheScrollViewInputPathAllocatesNothing`. No other gate failed (the flake did not show).
- The first plant attempt used a `sed` edit with a wrong brace, so I restored the file at once and made the plant with the Edit tool. The failed attempt was never built.
- The restore ran in a later tool call. `cmp` of the copy and `ScrollView.cs` found no difference.

### Phase 6 Results (task 6.6)

- `dotnet build brutal-ui.sln`: `0 Warning(s)`, `0 Error(s)`.
- `dotnet test --solution brutal-ui.sln`: total 2456, succeeded 2454, failed 0, skipped 2 (GLFW, no display). The base was 2401, so the growth is 55.
- `BrutalUi.Core.Tests`: 1624 (base 1569 + 55: 10 + 6 + 6 + 9 + 10 + 6 + 5 + 3).
- `scripts/test-linux.sh`: `test-linux: PASS build, test, gpu, smoke (120 frames) in 12 s`, `exit=0`.
- Lint scores (`--fail-over 2.5`): `technical-design.md` 1.55, `CHANGELOG.md` 1.78, `implementation.md` 1.91. No finding above 2.5.
- Files changed in this phase: `ScrollViewAllocationTests.cs` (new), `technical-design.md`, `CHANGELOG.md`, and this file. `ScrollView.cs` is planted and restored: no diff.

### Review Fixes

- CR-2, the anchor rule: `_dragPointer` holds the pointer position of the last drag press or drag move, or NaN (no anchor). The constructor writes NaN. A move on the view that is not a drag move writes NaN and walks on. A hover move bubbles to the view before each press, so a stale anchor does not live to the next gesture. A drag move with a NaN anchor stops the walk, stores the position, changes no offset, and calls nothing. A press that an ancestor capture handler stops thus gives no jump: the first move sets the anchor, and the later moves drag. The preferred epic fix (`UiContext` clears the press target of a stopped press) breaks criterion I2, so the fix stays local. The NaN anchor is data, so the view has no `bool` field. Mutation check: with no NaN write on the hover move, `TheCaptureEndsAtTheRelease` fails at the first assertion after the stopped press (expected (0, 20), actual (0, 21)).
- CR-3, the store rule: a user input stores only a change. `HandlePointerMove` clamps the next offset and calls `UserSet` only when the next offset differs from the applied offset, as `HandleScroll` does. The move still stops the walk. After a shrink, a wheel and a drag move at the end thus both keep the stored offset, and a growth brings it back. A set from code still stores the clamped value also when it equals the shown offset (phase 4). Mutation check: with an unconditional `UserSet`, `TheOffsetComesBackWhenTheContentGrows` fails at the first assertion after the growth (expected (0, 80), actual (0, 40)).

---

## Gotchas & Learnings

- From button-and-toggle: a stop inside a slot also skips the later delegates of the same slot (`EventWalk.InvokeSlot`).
- From button-and-toggle: do not run a planted allocation and its restore in one parallel tool block.
- From slider: a `float` map of a pixel center can be 1 ulp off. The slider map computes in `double`, so the tests assert each mapped value with no tolerance.
- From slider: a capture that routes moves past the hit test must repeat each rule that the hit test gives for free: the element is in the roots and is shown.
- From slider: the review fixes extend existing test methods and add no test method, so the class totals of the Definition of Done stay.
- `dotnet test --solution brutal-ui.sln --filter-class '*<Class>'` prints `error: 4` and the exit code 8 with 0 failed tests. The other test projects run zero tests under the filter. Use `dotnet run --project tests/BrutalUi.Core.Tests/BrutalUi.Core.Tests.csproj -- --filter-class '*<Class>'` for one class.
- A resumed review group can find a file of an interrupted agent. Compare each file with the `.g<n>` scratch copy of that agent first, so that no temporary mutation stays in the tree.

---

## API Integration

- `ScrollView.ScrollChanged` (`Action<ScrollView>?`): the delegate slot that the game sets at build time.
- The view reads the event slots `OnScroll`, `OnPointerDown`, and `OnPointerMove`, the press target of the context, and the drag capture of slider.

---

## State Management

- `ScrollView` holds `_offset`, `_scrollSpeed`, and `_dragPointer`. It holds no drag flag (Drag Rule item 3).

---

## Testing Approach

### Unit Tests
**Files:** `tests/BrutalUi.Core.Tests/Elements/ScrollView*Tests.cs`
- `ScrollViewScrollTests` (10), `ScrollViewHitTests` (6), `ScrollViewSliderTests` (6), `ScrollViewDragTests` (9), `ScrollViewClampTests` (10), `ScrollViewNestedTests` (6), `ScrollViewDisabledTests` (5).

### Allocation Tests
**Files:** `tests/BrutalUi.Core.Tests/Elements/ScrollViewAllocationTests.cs`
- 3 tests: `Update` and `Draw`, the input path with a captured drag and a slider drag in a scrolled view, and the real-work check.

---

## Needs Human Verification

`/autorun-feature` adds one line per item that the verifier cannot check, for a person to test.

- [ ] Phase 1 demo: a unit test pushes a scroll with the delta (0, -1) over `Row0` through `UiContext.PushEvent`, and runs `Update` and `Draw` with a `RecordingRenderer`. It reads `ScrollOffset` (0, 40), one `ScrollChanged` call with the view as the sender, and the rect of `Row0` at (0, -40, 300, 40) between `PushClip(0, 0, 200, 100)` and its `PopClip`. The developer runs `dotnet run --project tests/BrutalUi.Core.Tests/BrutalUi.Core.Tests.csproj -- --filter-class '*ScrollViewScrollTests'` and reads a total of 10 with `failed: 0`.
- [ ] Phase 2 demo: a unit test sets the offset (20, 20), pushes a left press at the viewport point of the slider value 25 (15, 30), and runs `Update`. It reads `Slider.Value` 25, one `ValueChanged` call, the offset (20, 20), and no `ScrollChanged` call. The developer runs `dotnet run --project tests/BrutalUi.Core.Tests/BrutalUi.Core.Tests.csproj -- --filter-class '*ScrollViewSliderTests'` and reads a total of 6 with `failed: 0`, and the same command with `'*ScrollViewHitTests'` reads a total of 6 with `failed: 0`.
- [ ] Phase 3 demo: a unit test pushes a left press at `OverGap` (160, 50) and a move to (160, 20), and runs `Update`. It reads `ScrollOffset` (0, 30) and one `ScrollChanged` call. The developer runs `dotnet run --project tests/BrutalUi.Core.Tests/BrutalUi.Core.Tests.csproj -- --filter-class '*ScrollViewDragTests'` and reads a total of 9 with `failed: 0`.
- [ ] Phase 4 demo: a unit test sets the offset (0, 80), hides `Row4` (`Inline.Display = Display.None`), and runs `Update` and `Draw`. It reads `ScrollOffset` (0, 40), the rect of `Row0` at (0, -40, 300, 40), and no `ScrollChanged` call. The developer runs `dotnet run --project tests/BrutalUi.Core.Tests/BrutalUi.Core.Tests.csproj -- --filter-class '*ScrollViewClampTests'` and reads a total of 10 with `failed: 0`.
- [ ] Phase 5 demo: a unit test puts an inner view at its end (0, 60) inside an outer view at (0, 0), pushes a scroll of (0, -1) over the inner view, and runs `Update`. It reads the inner offset (0, 60), the outer offset (0, 40), one outer `ScrollChanged` call, and no inner call. The developer runs `dotnet run --project tests/BrutalUi.Core.Tests/BrutalUi.Core.Tests.csproj -- --filter-class '*ScrollViewNestedTests'` and reads a total of 6 with `failed: 0`, and the same command with `'*ScrollViewDisabledTests'` reads a total of 5 with `failed: 0`.
- [ ] Phase 6 demo: the developer runs `dotnet run --project tests/BrutalUi.Core.Tests/BrutalUi.Core.Tests.csproj -- --filter-class '*ScrollViewAllocationTests'` and reads a total of 3 with `failed: 0`. A planted allocation at the start of `ScrollView.HandleScroll` makes `dotnet test --solution brutal-ui.sln` exit with a value that is not 0.

---

## Performance Considerations

- Each slot of `ScrollView` holds a cached static delegate.
- The clamp at use loops over the direct children of each view for each walk. It allocates nothing.

---

## Next Steps

### Immediate (Today/This Session)
1. A person tests the items of "Needs Human Verification".
2. `/autorun-epic` commits the feature and starts `interactive-elements/text-field`.

### Short Term
1. A person answers follow-up question 1 of `code-review.md` (CR-1, the leading-edge reach).
2. widget-sample shows a `ScrollView` in HelloPanel with a size, and puts it in the gate with each widget.

---

## Open Questions

- CR-1, the code part: must the view reach content before its leading edge (`column-reverse`, `justify-content: center`)? The docs state the leading-edge rule now. An offset range from the near edge changes the meaning of `ScrollOffset`.
- The follow-up questions 2 to 7 of `code-review.md`: a pending set, `Hover` after a wheel, a press on a view that cannot scroll, the cost of the clamp at use, and the `flex-grow` sizing.

---

## Session Notes

### 2026-10-26 09:19
- `/autorun-feature` started the feature in the called mode of `/autorun-epic`. It created `tasks.md` (29 tasks in 6 phases), `context.md`, and `inbox.md`.

### 2026-10-26 09:45 (phase 1)
- Tasks 1.1 to 1.7 are done (brief `phase-1.md`, advanced agent, one group). A wheel scroll over a child moves the offset, and the draw walk moves the children of the view by its offset inside the clip.
- Verifier (brief `phase-1-verify.md`): PASS. Demo: PASS. The solution gives 2411 tests (Core 1579, 10 more than the base), 0 failed, 2 skipped. `scripts/test-linux.sh` gives its PASS line.
- The inbox holds no line. Phase 2 can start.
- Phase 1 group 1 is done. The new files are `ScrollView.cs`, `ScrollViewTree.cs`, and `ScrollViewScrollTests.cs`. The changed files are `WidgetInput.cs` (`IsScroll`) and `PaintWalk.cs` (`ChildOrigin`). The 10 new tests passed on the first run.

### 2026-10-26 (phase 2)
- Tasks 2.1 to 2.6 are done (brief `phase-2.md`, advanced agent, one group). A hit inside a scrolled view lands on the element that the draw shows, and a slider in a scrolled view maps a press and a drag through the offset.
- The changed files are `HitWalk.cs` (`HitFrame.ChildOrigin`) and `WidgetInput.cs` (`ViewportOrigin`). The new files are `ScrollViewHitTests.cs` and `ScrollViewSliderTests.cs`. The 12 new tests passed on the first run.
- The solution gives 2423 tests (Core 1591), 0 failed, 2 skipped. `tasks.md` is for the caller to mark.
- Verifier (brief `phase-2-verify.md`): PASS. Demo: PASS. The first full run gave 1 failed test outside this feature: `TransitionCurveTests.TheCurvesAllocateNothing` ("ease-in-out allocated 7976 bytes"). The class alone gave 31 of 31, and a second full run gave 0 failed. button-and-toggle saw the same flake (8056 bytes). This feature does not touch `Style/`.
- The inbox holds no line. Phase 3 can start.

### 2026-10-26 (phase 3)
- Tasks 3.1 to 3.3 are done (brief `phase-3.md`, standard agent, one group). A left press on the view itself and a move drag the content with the pointer. The capture keeps the drag outside the view.
- The changed file is `ScrollView.cs` (`IDragCapture`, `_dragPointer`, `s_pointerDown`, `s_pointerMove`, `HandlePointerDown`, `HandlePointerMove`, explicit `CapturesMoves`, extended remarks). The new file is `ScrollViewDragTests.cs` (9 tests, all passed on the first run). `UiContext.cs`, `HitWalk.cs`, `PaintWalk.cs`, `WidgetInput.cs`, `Slider.cs`, and `IDragCapture.cs` have no change in this phase.
- Demo: press at (160, 50), move to (160, 20): offset (0, 30), one `ScrollChanged` call, `Row0` drawn at (0, -30, 300, 40). PASS.
- Build: `0 Warning(s)`, `0 Error(s)`. Solution: total 2432, succeeded 2430, failed 0, skipped 2 (the GLFW window tests, no display). Core: 1600 (9 more than after phase 2).
- Each class of phases 1 and 2 and of the regression set gives its base count with 0 failed (ScrollViewScrollTests 10, ScrollViewHitTests 6, ScrollViewSliderTests 6, and the 32 regression classes at the counts of the Phase 1 Baseline).
- Decisions: the move handler writes `_dragPointer` before `UserSet`, so the stored position is the raw pointer and never the clamped one (Decision 9: a move back from past the end acts at once). The press stops the walk as the slider does. The hit walk and `ViewportOrigin` remarks are in the class remarks (follow-up of phase 1).
- Verifier (brief `phase-3-verify.md`): PASS. Demo: PASS. The solution gives 2432 tests, 0 failed, 2 skipped. Each `Input/` class passes. The class remarks name the offset rule of the three readers, the drag rule, and the capture. `scripts/test-linux.sh` gives `exit=0`.
- The inbox holds no line. Phase 4 can start.

### 2026-10-26 10:35 (phase 4)
- Tasks 4.1 to 4.3 are done (brief `phase-4.md`, standard agent, one group). `AppliedOffset()` clamps the stored offset with the last layout, so a content shrink shows the end at once with no call. A non-finite delta component counts as 0.
- Decision of the phase agent: `Apply` writes the clamped value also when it equals the shown offset, and it marks the paint only on a change of the shown offset. A set or a user input to the shown offset thus replaces a stored offset that a shrink clamped. See "Phase 4" under "Important Decisions".
- Verifier (brief `phase-4-verify.md`): PASS. Demo: PASS. The solution gives 2442 tests (Core 1610), 0 failed, 2 skipped. `HitWalk.cs`, `PaintWalk.cs`, and `WidgetInput.cs` have no change in this phase (the diff fingerprint holds). `scripts/test-linux.sh` gives `exit=0`.
- The inbox holds no line. Phase 5 can start.

### 2026-10-26 10:50 (phase 5)
- Tasks 5.1 to 5.4 are done (brief `phase-5.md`, standard agent, one group). A view in a view scrolls, hits, and draws with the sum of the offsets, and an inner view at its end passes the wheel to the outer view. A disabled view takes no input and keeps its offset in the walks.
- Demo: inner view at (0, 60), scroll (0, -1): inner (0, 60), outer (0, 40), one outer call, no inner call. PASS.
- The solution gives 2453 tests, 0 failed, 2 skipped. See "Phase 5 Results".
- Verifier (brief `phase-5-verify.md`): PASS. Demo: PASS. The solution gives 2453 tests (11 more than after phase 4), 0 failed, 2 skipped. `ScrollView.cs` changes in `///` lines only, and the walks have no change (both fingerprints hold). `scripts/test-linux.sh` gives `exit=0`.
- The inbox holds no line. Phase 6 can start.

### 2026-10-26 11:10 (phase 6)
- Tasks 6.1 to 6.6 are done (brief `phase-6.md`, standard agent, one group). The eleventh gate measures 0 bytes. The plant failed one gate (`TheScrollViewInputPathAllocatesNothing`, exit 2), and the restore passed `cmp`. Sections 5, 8, 9, and 14 of the technical design and the changelog hold the scroll view rules.
- Verifier (brief `phase-6-verify.md`, the full Verification Method of the plan): PASS. Demo: PASS. The solution gives 2456 tests (Core 1624, 55 more than the base), 0 failed, 2 skipped. The verifier plant failed the same gate (exit 2), and the restore passed `cmp`. The lint gives 1.55 for `technical-design.md`, 1.78 for `CHANGELOG.md`, 2.17 for `context.md`, and 1.91 for `implementation.md`. `scripts/test-linux.sh` gives its PASS line.
- Each phase is complete. The inbox holds no line. The cross-phase review can start.

### 2026-10-27 (cross-phase review)
- The review (`code-review.md`) found 14 findings: 9 MEDIUM and 5 LOW, no HIGH. Groups `review-g1.md` to `review-g4.md` fixed the test gaps CR-5 to CR-11. Group `review-g5.md` (advanced agent, resumed after an interrupt) fixed the drag defects CR-2 and CR-3. Groups `review-g6.md` to `review-g9.md` (standard agent) fixed the doc findings CR-1 (doc part), CR-4, CR-12, CR-13, and CR-14.
- CR-1 keeps its code part open on purpose. An offset range from the near edge changes the meaning of `ScrollOffset`, so a person decides it.
- Verifier (brief `review-verify.md`): PASS. Demo: none. The solution gives 2456 tests, 0 failed, 2 skipped. Each scroll view class and each regression class keeps its count. `scripts/test-linux.sh` gives its PASS line and `exit=0`.
- The inbox holds no line, so `/update-feature` folds and promotes nothing.
- The draft plan of `interactive-elements/text-field` exists (solution-architect, 6 phases, 34 tasks, 4 open scope questions in its Notes).
- Next: `/autorun-epic` commits this feature. A person tests the items of "Needs Human Verification".

---

*Update this file at the end of each work session. Run `/update-feature` before compacting conversations.*
