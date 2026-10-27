# interactive-elements/scroll-view - Task Checklist

**Last Updated:** 2026-10-27 07:26
**Progress:** 29/29 tasks complete (100%)
**Advanced phases:** Phases 1 and 2 (ADVANCED, opus). Phases 3, 4, 5, and 6 are STANDARD (sonnet).

Source paths: `Core/` means `src/BrutalUi.Core/`. `Tests/` means `tests/BrutalUi.Core.Tests/`. "The plan" means `implementation.md` of this feature. Its sections "Architecture Overview", "Internal Types", "The Offset Rule of the Walks", "The Drag Rule", and "Key Technical Decisions" hold the rules that the tasks name. Each phase ends with `dotnet build brutal-ui.sln 2>&1 | tail -n 30` and `dotnet test --solution brutal-ui.sln 2>&1 | tail -n 30` (tool rule 5). Expect `0 Warning(s)`, `0 Error(s)`, and 0 failed. The projects have no implicit usings, so each new file holds its own `using` lines. Each public member needs an XML doc comment (CS1591 is an error). Use `namespace BrutalUi.Core;` in Core and `namespace BrutalUi.Core.Tests;` in the tests. Prose in comments follows ASD-STE100. A test class that interns a class name uses a name prefix that is unique to the class, because xUnit runs the classes in parallel. Run one class with `dotnet run --project tests/BrutalUi.Core.Tests/BrutalUi.Core.Tests.csproj -- --filter-class '*<Class>' 2>&1 | tail -n 5`.

Each phase runs these classes with no edit (the regression set): the pointer and focus classes (`PointerPressTests`, `PointerStateEdgeTests`, `PointerHoverTests`, `PointerAllocationTests`, `ClickFocusTests`, `FocusOwnerTests`, `TabNavigationTests`, `FocusStateEdgeTests`, `FocusAllocationTests`, `RoutingGateTests`, `ScrollRoutingTests`), the walk classes (`HitTestTests`, `HitTestRuleTests`, `PaintWalkTests`, `PaintContentTests`, `RenderValueTests`), the button-and-toggle classes (`ReleaseOrderTests`, `ButtonClickTests`, `ButtonKeyTests`, `ToggleTests`, `ButtonTextTests`, `WidgetDisabledTests`, `WidgetAllocationTests`), and the slider classes (`ChordClickTests`, `SliderPressTests`, `SliderDrawTests`, `SliderDragTests`, `DragCaptureTests`, `SliderStepTests`, `SliderKeyTests`, `SliderDisabledTests`, `SliderAllocationTests`).

The test scene of each scroll view test is `ScrollViewTree` (task 1.5), unless a task names another scene. The view is 200 x 100 at (0, 0) with no padding. Its children in a column are `Row0` (300 x 40 at y 0), `Slider` (120 x 20 at y 40, the range 0 to 100), `Row2` (300 x 40 at y 60), `Row3` (300 x 40 at y 100, not pickable), and `Row4` (300 x 40 at y 140, not pickable). The content is 300 x 180, so the maximum offset is (100, 80). `Outside` is 100 x 100 at (0, 100). With the offset (ox, oy), the slider value `v` sits at the viewport point (10 + v - ox, 50 - oy).

---

## Phase 1: A Wheel Scroll Moves the Content (7/7 complete)

**Demo:** a unit test pushes a scroll with the delta (0, -1) over `Row0` through `UiContext.PushEvent`, and runs `Update` and `Draw` with a `RecordingRenderer`. It reads `ScrollOffset` (0, 40), one `ScrollChanged` call with the view as the sender, and the rect of `Row0` at (0, -40, 300, 40) between `PushClip(0, 0, 200, 100)` and its `PopClip`. The developer runs `dotnet run --project tests/BrutalUi.Core.Tests/BrutalUi.Core.Tests.csproj -- --filter-class '*ScrollViewScrollTests'` and reads a total of 10 with `failed: 0`.
**Needs:** none
**Agent tier:** ADVANCED (opus)
**Tier note:** This phase changes `PaintWalk`, the draw walk of each frame. Change only step 3 of `Enter` (add the child origin), the frame push (step 8), the loop line that passes the origin to a child, the `PaintFrame` field, and the doc comments. Do not change the hidden rule, the opacity rule, the rect step, the clip step, the `PaintContent` call, the pop, the stack growth, or the `finally` block. The view, its clip, and its `PaintContent` use the origin with no offset. Only a `ScrollView` changes the child origin, so each existing draw test passes with no edit.

- [x] ✅ **1.1 Record the base and the blast radius** - Completed 2026-10-26
  - Description: Record in `docs/features/interactive-elements/scroll-view/context.md`, before any change: the base commit (`git rev-parse HEAD`), and the result of `git ls-files src/BrutalUi.Core/Elements/Slider.cs src/BrutalUi.Core/Input/IDragCapture.cs` (expect two lines) and `git status --porcelain -- src tests` (expect no line). When slider is not in the base commit, stop and record it. Record the test counts of the solution and of `BrutalUi.Core.Tests`, and the counts of each class of the regression set. Record the Codebase Findings and the blast radius: each caller of `PaintWalk.Walk`, each use of `PaintFrame.Origin`, and each test that reads a paint frame.
  - File(s): `docs/features/interactive-elements/scroll-view/context.md`
  - Estimate: 8 steps

- [x] ✅ **1.2 Add IsScroll to WidgetInput** - Completed 2026-10-26
  - Description: Edit `src/BrutalUi.Core/Elements/WidgetInput.cs`: add `IsScroll(Element, in ScrollEvent)` (Internal Types). The doc comment states: the check reads the type and the disabled bit, it does not read the target, so a scroll on a child counts, and it reads no press target. Extend the class remarks: `IsScroll` serves each widget that takes the wheel.
  - File(s): `src/BrutalUi.Core/Elements/WidgetInput.cs`
  - Estimate: 4 steps

- [x] ✅ **1.3 Write ScrollView.cs (the scroll path)** - Completed 2026-10-26
  - Description: Write `src/BrutalUi.Core/Elements/ScrollView.cs` by the widget pattern: sealed, the type id of `ScrollView`, `DefaultScrollSpeed`, `public ScrollView(in CreateInfo info)`, `CreateInfo` with `ScrollSpeed`, the get-only `ScrollSpeed`, `ScrollOffset` (the get returns `AppliedOffset()`, the set calls `Apply`), `public Action<ScrollView>? ScrollChanged { get; set; }`, `MaxOffset()` (Decision 4), `AppliedOffset()` (returns `_offset` in this phase), `Clamp`, `Apply`, `UserSet`, `ValidateSpeed`, and the static `HandleScroll` (Decision 7). The constructor checks the speed first (Decision 8), writes 40 for a speed of 0, sets `Inline.Overflow = Overflow.Scroll` (Decision 6), and writes `OnScroll = s_scroll`. `HandleScroll` computes the next offset first. It returns with no stop when the next offset equals the applied offset. Else it stops the walk and calls `UserSet`. The remarks state: the offset rule of the walks, the clip, the content size, the clamp, the scroll rule and its sign, the stop rule, the sender, "read `ScrollChanged`, not the slot", "a write of `=` to `OnScroll` removes the wheel", a code set calls nothing, the view has no own size (Decision 13), and a set before the first `Update` gives (0, 0).
  - File(s): `src/BrutalUi.Core/Elements/ScrollView.cs`
  - Estimate: 12 steps

- [x] ✅ **1.4 Apply the offset in PaintWalk** - Completed 2026-10-26
  - Description: Edit `src/BrutalUi.Core/Rendering/PaintWalk.cs` by the Offset Rule of the Walks: rename `PaintFrame.Origin` to `ChildOrigin` and state its meaning in the doc comment. In `Enter`, compute the child origin after step 3 (the code block of the rule), and write it into the frame in step 8. The loop passes `top.ChildOrigin` to the child. Edit the class remarks and the remarks of `Enter`: a `ScrollView` moves its children by its offset, and its own rect, clip, and `PaintContent` do not move.
  - File(s): `src/BrutalUi.Core/Rendering/PaintWalk.cs`
  - Estimate: 6 steps

- [x] ✅ **1.5 Write ScrollViewTree.cs** - Completed 2026-10-26
  - Description: Write `tests/BrutalUi.Core.Tests/Elements/ScrollViewTree.cs`: an internal test tree. The context has a `RecordingRenderer` measurer and a viewport of 800 x 600. The root is 400 x 300 in a column and is pickable. Its children are `View` (a `ScrollView`, 200 x 100, column) and `Outside` (a plain element, 100 x 100). The children of `View` are `Row0`, `Slider` (120 x 20, the range 0 to 100, the value 0), `Row2`, `Row3`, and `Row4` (each row 300 x 40 with its own opaque inline background color). `Row3` and `Row4` have `IsPickable = false`. The constructor takes `in ScrollView.CreateInfo`, and a second constructor uses the default create-info. It holds the points `OverRow0` (100, 20), `OverGap` (160, 50, the view beside the slider at the offset 0), `OverOutside` (50, 150), `PastRight` (300, 50, the root), and `OverNothing` (600, 500), `static float2 SliderAt(float value, float2 offset)` (the point (10 + value - offset.X, 50 - offset.Y)), `RowFill(int)`, and `Settle()`. Use the factories of `PointerTree` for the events. The doc comment states the rects, the maximum offset (100, 80), and the map. It states that `PointerTree.ViewportRect` ignores the offset.
  - File(s): `tests/BrutalUi.Core.Tests/Elements/ScrollViewTree.cs`
  - Estimate: 7 steps

- [x] ✅ **1.6 Write ScrollViewScrollTests.cs (10 tests)** - Completed 2026-10-26
  - Description: Write `tests/BrutalUi.Core.Tests/Elements/ScrollViewScrollTests.cs` with 10 tests: `AScrollMovesTheContent` (the demo), `AScrollUpMovesTheContentBack` (a delta of (0, -1) and then (0, 1) give (0, 0) and two calls), `AHorizontalScrollMovesTheX` (the delta (-1, 0) gives (40, 0), and `Row0` draws at (-40, 0, 300, 40)), `TheScrollStopsAtTheEnd` (three scrolls of (0, -1) in one drain give (0, 80) and two calls, and the root bubble handler gets the third scroll only), `AScrollAtTheStartWalksOn` (the delta (0, 1) at the offset 0: no change, no call, and the root bubble handler gets it), `AScrollThatMovesStopsTheWalk` (the root capture handler runs, and the root bubble handler does not), `TheSpeedScalesTheDelta` (`ScrollSpeed` 10 and the delta (0, -2.5) give (0, 25). A create-info speed of 0 gives `ScrollSpeed` 40), `ACodeSetClampsAndCallsNothing` (a set of (500, 500) gives (100, 80) and marks `Paint`. A set of (-5, 10) gives (0, 10). The same value marks nothing after a settle. The call count stays 0), `TheCreateInfoTheTypeAndTheOverflow` (the type id is `ScrollView`, `Inline.Overflow` and `Style.Overflow` are `Scroll`, `IsFocusable` is `false`, and a speed of -1, NaN, or infinity throws `ArgumentOutOfRangeException` with `ParamName` `info`), and `ATypeRuleMatchesTheScrollView` (a rule with `Type = ElementType.Intern("ScrollView")` sets a fill on the view and not on `Outside`. A rule with `overflow: visible` on the type does not remove the clip).
  - File(s): `tests/BrutalUi.Core.Tests/Elements/ScrollViewScrollTests.cs`
  - Estimate: 10 steps

- [x] ✅ **1.7 Run the phase 1 gate and record it** - Completed 2026-10-26
  - Description: Run the build, the tests, the demo class, and the regression set. Run `git diff <base> --stat -- tests/` and expect no changed existing file. Run `scripts/test-linux.sh; echo "exit=$?"` and expect the PASS line and `exit=0`. Record the counts and each decision that this plan did not settle in `context.md`.
  - File(s): `docs/features/interactive-elements/scroll-view/context.md`
  - Estimate: 5 steps

---

## Phase 2: A Hit Lands on the Element That the User Sees (6/6 complete)

**Demo:** a unit test sets the offset (20, 20), pushes a left press at the viewport point of the slider value 25 (15, 30), and runs `Update`. It reads `Slider.Value` 25, one `ValueChanged` call, the offset (20, 20), and no `ScrollChanged` call. The developer runs `dotnet run --project tests/BrutalUi.Core.Tests/BrutalUi.Core.Tests.csproj -- --filter-class '*ScrollViewSliderTests'` and reads a total of 6 with `failed: 0`, and the same command with `'*ScrollViewHitTests'` reads a total of 6 with `failed: 0`.
**Needs:** Phase 1
**Agent tier:** ADVANCED (opus)
**Tier note:** This phase changes `HitWalk`, the hit test of each pointer event and each scroll, and the origin helper of each widget map. Add the `HitFrame.ChildOrigin` field, compute it in step 3 of `Enter` by the Offset Rule of the Walks, and pass `top.ChildOrigin` to each child. The element test keeps `top.Origin`, so the border box of the view does not move. Do not change the hidden rule, the clip rule, the cull of step 4, `Contains`, `InBounds`, the stack growth, or the `finally` block. `HitTestTests` reads `UiContext.HitStackSlots`. A new field keeps that test valid.

- [x] ✅ **2.1 Record the blast radius of the hit walk** - Completed 2026-10-26
  - Description: Record the blast radius in `context.md`: each caller of `HitWalk.Find` and of `UiContext.HitTest`, each test that reads `HitStackSlots`, and each caller of `WidgetInput.ViewportOrigin`.
  - File(s): `docs/features/interactive-elements/scroll-view/context.md`
  - Estimate: 5 steps

- [x] ✅ **2.2 Apply the offset in HitWalk** - Completed 2026-10-26
  - Description: Edit `src/BrutalUi.Core/Input/HitWalk.cs`: add `public float2 ChildOrigin` to `HitFrame` with a doc comment. In `Enter`, compute the child origin after step 3, and write it into the frame in step 4. The loop copies `top.ChildOrigin` before the call to `Enter`, next to the copy of the clip (the gotcha of the stale ref). Edit the class remarks (the origin rule: a `ScrollView` moves its children by its offset, and the hit walk and the draw walk read the same `AppliedOffset`) and the doc comments of `Enter` and `HitFrame.Origin`.
  - File(s): `src/BrutalUi.Core/Input/HitWalk.cs`
  - Estimate: 6 steps

- [x] ✅ **2.3 ViewportOrigin subtracts the ancestor offsets** - Completed 2026-10-26
  - Description: Edit `src/BrutalUi.Core/Elements/WidgetInput.cs`: `ViewportOrigin` subtracts `AppliedOffset()` of each ancestor `ScrollView`, and not of the element itself (rule 4 of the Offset Rule of the Walks). Replace the remark "When scroll-view adds a scroll offset to those walks ..." with the rule and a reference to `ScrollViewSliderTests`.
  - File(s): `src/BrutalUi.Core/Elements/WidgetInput.cs`
  - Estimate: 4 steps

- [x] ✅ **2.4 Write ScrollViewHitTests.cs (6 tests)** - Completed 2026-10-26
  - Description: Write `tests/BrutalUi.Core.Tests/Elements/ScrollViewHitTests.cs` with 6 tests: `AHitLandsOnTheElementThatTheUserSees` (the offset (0, 40) and a move at (100, 30): `PointerHit` and `Hover` are on `Row2`. The base code gives `Row0`), `TheClipOfTheViewDoesNotMove` (the offset (60, 0): a move at (220, 20) hits the root, and a move at (190, 20) hits `Row0`), `TheGapAndANonPickableChildHitTheView` (the offset 0 at `OverGap` hits the view. The offset (0, 80) at (100, 50) falls through `Row3` to the view), `AScrollWalksThePathOfTheShiftedChild` (the offset (0, 40) and a scroll at (100, 30): the target of the walk is `Row2`, and the view moves), `AnElementOutsideTheViewKeepsItsHit` (the offset (100, 80): `OverOutside` hits `Outside`, and `PastRight` hits the root), and `TheHitAndTheDrawAgree` (the offset (37, 23): for each rect that the draw gives to `Row0`, `Row2`, and the slider thumb, a move at the center of the visible part of the rect inside the clip hits that element).
  - File(s): `tests/BrutalUi.Core.Tests/Elements/ScrollViewHitTests.cs`
  - Estimate: 8 steps

- [x] ✅ **2.5 Write ScrollViewSliderTests.cs (6 tests)** - Completed 2026-10-26
  - Description: Write `tests/BrutalUi.Core.Tests/Elements/ScrollViewSliderTests.cs` with 6 tests: `APressOnASliderInAScrolledViewMapsThroughTheOffset` (the demo), `ADragOfASliderInAScrolledViewFollowsThePointer` (a press at 25 and a move to `SliderAt(60, (20, 20))` give 60 and two calls), `ASliderDragPastTheViewEdgeStaysOnTheSlider` (a press at 25 and a move to (300, 30) give 100. The offset stays (20, 20), and `Hover` is on the root), `AWheelOverTheSliderScrollsTheView` (the offset (20, 0) and the delta (0, -0.5) over the slider give the offset (20, 20), and the value stays), `AScrollAndAPressInOneDrainUseTheNewOffset` (the offset 0, then a scroll of (-0.5, 0) over `Row0` and a left press at (15, 50) in one `Update` give the offset (20, 0) and the value 25), and `TheOriginSubtractsTheAncestorOffsetOnly` (the offset (20, 20): `ViewportOrigin` gives (-20, 20) for the slider, (0, 0) for the view, and (-20, -20) for `Row0`).
  - File(s): `tests/BrutalUi.Core.Tests/Elements/ScrollViewSliderTests.cs`
  - Estimate: 8 steps

- [x] ✅ **2.6 Run the phase 2 gate and record it** - Completed 2026-10-26
  - Description: Run the build, the tests, the demo class, the class of task 2.4, the class of phase 1, the regression set, and each class in `tests/BrutalUi.Core.Tests/Input/`. Run `git diff <base> --stat -- tests/` and expect no changed existing file. Record the results in `context.md`.
  - File(s): `docs/features/interactive-elements/scroll-view/context.md`
  - Estimate: 5 steps

---

## Phase 3: A Drag Moves the Content (3/3 complete)

**Demo:** a unit test pushes a left press at `OverGap` (160, 50) and a move to (160, 20), and runs `Update`. It reads `ScrollOffset` (0, 30) and one `ScrollChanged` call. The developer runs `dotnet run --project tests/BrutalUi.Core.Tests/BrutalUi.Core.Tests.csproj -- --filter-class '*ScrollViewDragTests'` and reads a total of 9 with `failed: 0`.
**Needs:** Phase 2
**Agent tier:** STANDARD (sonnet)

- [x] ✅ **3.1 Add the drag and the capture to ScrollView** - Completed 2026-10-26
  - Description: Edit `src/BrutalUi.Core/Elements/ScrollView.cs`: add `IDragCapture` to the class line, `_dragPointer`, `s_pointerDown`, `s_pointerMove`, the writes of `OnPointerDown` and `OnPointerMove` in the constructor, the static `HandlePointerDown` and `HandlePointerMove` of the Drag Rule, and the explicit `IDragCapture.CapturesMoves` (no `Disabled`). Extend the remarks: the drag rule (Decision 9), the press on the view itself (Notes Q2: a press on a pickable child gives no drag, so set `IsPickable = false` on a decorative child to let a press fall through), the stop for each drag move, the press button rule, the capture, and "a write of `=` to `OnPointerDown` removes the drag".
  - File(s): `src/BrutalUi.Core/Elements/ScrollView.cs`
  - Estimate: 8 steps

- [x] ✅ **3.2 Write ScrollViewDragTests.cs (9 tests)** - Completed 2026-10-26
  - Description: Write `tests/BrutalUi.Core.Tests/Elements/ScrollViewDragTests.cs` with 9 tests: `ADragMovesTheContentWithThePointer` (the demo, and the draw gives `Row0` at (0, -30, 300, 40)), `ADragOutsideTheViewKeepsDragging` (a press at `OverGap` and a move to (300, 10) over the root give (0, 40). `Hover` and `PointerHit` are on the root), `ADragPastTheEndComesBackAtOnce` (a move to (160, -100) gives (0, 80). A move to (160, -90) gives (0, 70)), `ADragMoveStopsTheWalkAlsoWithNoChange` (at the end, one more move gives no call, and the root bubble handler gets no move), `APressOnAPickableChildGivesNoDrag` (a press at `OverRow0` and a move to (100, 0): no change, no call, and the root bubble handler gets the move), `APressOnANonPickableChildDrags` (a code set of (0, 80), a press at (100, 50) over `Row3`, and a move to (100, 70) give (0, 60)), `ARightPressGivesNoDrag` (a right press at `OverGap` and a move: no change, and the root bubble handler gets the move), `TheCaptureEndsAtTheRelease` (after a release at `PastRight`, a move over the view changes nothing), and `AWheelDuringADragAddsUp` (a press at `OverGap`, a move to (160, 40) gives (0, 10), a scroll of (0, -1) gives (0, 50), and a move to (160, 30) gives (0, 60)).
  - File(s): `tests/BrutalUi.Core.Tests/Elements/ScrollViewDragTests.cs`
  - Estimate: 10 steps

- [x] ✅ **3.3 Run the phase 3 gate and record it** - Completed 2026-10-26
  - Description: Run the build, the tests, the demo class, the classes of phases 1 and 2, the regression set, and each class in `tests/BrutalUi.Core.Tests/Input/`. Record the results in `context.md`.
  - File(s): `docs/features/interactive-elements/scroll-view/context.md`
  - Estimate: 4 steps

---

## Phase 4: The Clamp After a Content Change (3/3 complete)

**Demo:** a unit test sets the offset (0, 80), hides `Row4` (`Inline.Display = Display.None`), and runs `Update` and `Draw`. It reads `ScrollOffset` (0, 40), the rect of `Row0` at (0, -40, 300, 40), and no `ScrollChanged` call. The developer runs `dotnet run --project tests/BrutalUi.Core.Tests/BrutalUi.Core.Tests.csproj -- --filter-class '*ScrollViewClampTests'` and reads a total of 10 with `failed: 0`.
**Needs:** Phase 3
**Agent tier:** STANDARD (sonnet)

- [x] ✅ **4.1 Clamp at use and the NaN rule in ScrollView** - Completed 2026-10-26
  - Description: Edit `src/BrutalUi.Core/Elements/ScrollView.cs`: `AppliedOffset()` returns `Clamp(_offset)` with `MaxOffset()` of the last layout (Decision 5). `Apply` compares the clamped value with `AppliedOffset()`, and it writes the clamped value. `Clamp` gives 0 for a NaN component, the maximum for positive infinity, and 0 for negative infinity. `HandleScroll` counts a delta component that is not finite as 0. Extend the remarks: the clamp at use, the stored offset (a content shrink shows the end, and a content growth before the next input brings the old offset back), the content size rule (the direct children, their border boxes, the trailing padding and border, no margin, an absolute child counts, a hidden child gives a rect of 0), a set before the first `Update`, and the NaN rule.
  - File(s): `src/BrutalUi.Core/Elements/ScrollView.cs`
  - Estimate: 7 steps

- [x] ✅ **4.2 Write ScrollViewClampTests.cs (10 tests)** - Completed 2026-10-26
  - Description: Write `tests/BrutalUi.Core.Tests/Elements/ScrollViewClampTests.cs` with 10 tests: `AContentShrinkShowsTheEndAtOnce` (the demo), `TheOffsetComesBackWhenTheContentGrows` (after the demo, a show of `Row4` gives (0, 80) and no call), `AUserScrollAfterAShrinkStartsFromTheShownOffset` (after the shrink, a scroll of (0, 1) gives (0, 0) and one call. A show of `Row4` then gives (0, 0)), `ASetBeforeTheFirstUpdateGivesZero` (a new tree with no settle: a set of (50, 50) gives (0, 0), also after a settle), `ANaNOrInfiniteSetFollowsTheRule` ((NaN, 50) gives (0, 50). (+infinity, -infinity) gives (100, 0)), `ANonFiniteDeltaCountsAsZero` ((NaN, -1) gives (0, 40). (+infinity, 0) changes nothing and walks on), `ThePaddingAndTheBorderCount` (a padding of 10 and a border of 2 on the view give the maximum (124, 104), the clip (2, 2, 196, 96), and `Row0` at (12 - ox, 12 - oy)), `AnAbsoluteChildCountsAndAHiddenChildDoesNot` (an absolute child at the left 500 with a width of 50 gives the maximum x 350. A hide of that child gives 100 again), `AViewLargerThanTheContentDoesNotScroll` (a view of 400 x 300 gives the maximum (0, 0), and a scroll walks on), and `AViewResizeClampsTheShownOffset` (the offset (0, 80) and a view height of 150 give (0, 30)).
  - File(s): `tests/BrutalUi.Core.Tests/Elements/ScrollViewClampTests.cs`
  - Estimate: 10 steps

- [x] ✅ **4.3 Run the phase 4 gate and record it** - Completed 2026-10-26
  - Description: Run the build, the tests, the demo class, the classes of phases 1 to 3, and the regression set. Record the results in `context.md`.
  - File(s): `docs/features/interactive-elements/scroll-view/context.md`
  - Estimate: 4 steps

---

## Phase 5: Views in Views and Disabled Views (4/4 complete)

**Demo:** a unit test puts an inner view at its end (0, 60) inside an outer view at (0, 0), pushes a scroll of (0, -1) over the inner view, and runs `Update`. It reads the inner offset (0, 60), the outer offset (0, 40), one outer `ScrollChanged` call, and no inner call. The developer runs `dotnet run --project tests/BrutalUi.Core.Tests/BrutalUi.Core.Tests.csproj -- --filter-class '*ScrollViewNestedTests'` and reads a total of 6 with `failed: 0`, and the same command with `'*ScrollViewDisabledTests'` reads a total of 5 with `failed: 0`.
**Needs:** Phase 4
**Agent tier:** STANDARD (sonnet)

- [x] ✅ **5.1 Add the nested rule and the disabled rule to the remarks** - Completed 2026-10-26
  - Description: Edit the remarks of `src/BrutalUi.Core/Elements/ScrollView.cs` only: the nested rule (the offsets add up, the clips intersect, and the stop rule passes the wheel at the end of an inner view to the outer view) and the disabled rule (Decision 11). Change no code line. When a test of tasks 5.2 or 5.3 fails on the code, stop and record the failure in `context.md`. Do not change the walks in this phase.
  - File(s): `src/BrutalUi.Core/Elements/ScrollView.cs`
  - Estimate: 3 steps

- [x] ✅ **5.2 Write ScrollViewNestedTests.cs (6 tests)** - Completed 2026-10-26
  - Description: Write `tests/BrutalUi.Core.Tests/Elements/ScrollViewNestedTests.cs` with 6 tests on a scene that a private static method of the class builds: an outer view of 200 x 100 in a column with `Top` (300 x 40), `Inner` (a view of 150 x 60 with a left padding of 10, holding `InnerRow0` to `InnerRow2`, each 200 x 40), and `Bottom` (300 x 80). The outer maximum is (100, 80), and the inner maximum is (60, 60). The tests: `AnInnerViewAtItsEndPassesTheWheelToTheOuterView` (the demo), `AnInnerViewThatMovesKeepsTheWheel` (the inner offset 0: the inner view goes to (0, 40), and the outer view stays), `TheOffsetsAddUpInTheHitAndTheDraw` (the outer offset (0, 20) and the inner offset (0, 30): a move at (50, 35) hits `InnerRow1`, the draw gives `InnerRow1` at (10, 30, 200, 40), and the inner clip is (0, 20, 150, 60)), `TheOriginSubtractsEachAncestorOffset` (the same offsets: `ViewportOrigin` gives (10, 30) for `InnerRow1` and (0, 20) for `Inner`), `ADragOnTheInnerViewMovesTheInnerViewOnly` (a press at (5, 60) in the inner padding and a move to (5, 30) give the inner offset (0, 30), and the outer view stays), and `ADisabledInnerViewPassesTheWheelToTheOuterView`.
  - File(s): `tests/BrutalUi.Core.Tests/Elements/ScrollViewNestedTests.cs`
  - Estimate: 9 steps

- [x] ✅ **5.3 Write ScrollViewDisabledTests.cs (5 tests)** - Completed 2026-10-26
  - Description: Write `tests/BrutalUi.Core.Tests/Elements/ScrollViewDisabledTests.cs` with 5 tests on `ScrollViewTree`: `ADisabledViewIgnoresTheWheel` (no change, no call, and the root bubble handler gets the scroll), `ADisabledViewIgnoresADrag` (a press at `OverGap` reaches the root bubble handler, and a move changes nothing), `ADisableDuringADragStopsTheDragAndTheCapture` (a press and a move give (0, 30). After a disable, a move to `PastRight` walks to the root, and the offset stays), `ADisabledViewKeepsItsOffsetInTheWalks` (the offset (0, 40) and a disable: the draw gives `Row0` at (0, -40, 300, 40), and a move at (100, 30) hits `Row2`), and `ACodeSetWorksOnADisabledView`.
  - File(s): `tests/BrutalUi.Core.Tests/Elements/ScrollViewDisabledTests.cs`
  - Estimate: 7 steps

- [x] ✅ **5.4 Run the phase 5 gate and record it** - Completed 2026-10-26
  - Description: Run the build, the tests, the two classes of this phase, the classes of phases 1 to 4, and the regression set. Record the results in `context.md`.
  - File(s): `docs/features/interactive-elements/scroll-view/context.md`
  - Estimate: 4 steps

---

## Phase 6: The Allocation Gate and the Record (6/6 complete)

**Demo:** the developer runs `dotnet run --project tests/BrutalUi.Core.Tests/BrutalUi.Core.Tests.csproj -- --filter-class '*ScrollViewAllocationTests'` and reads a total of 3 with `failed: 0`. A planted allocation at the start of `ScrollView.HandleScroll` makes `dotnet test --solution brutal-ui.sln` exit with a value that is not 0.
**Needs:** Phase 5
**Agent tier:** STANDARD (sonnet)
**Tier note:** Copy the gate shape of `SliderAllocationTests` and `WidgetAllocationTests`: a `Scene` class that the test builds before the measure, delegates from instance methods that the scene creates one time, `AllocationGate.ReleaseAllocationContext()` before the measured region, and a measured body with no lambda, no `$"`, no new `string`, and no `Assert`. Do not add warm-up rounds to hide an allocation. Do not run a plant and its restore in one parallel tool block (a gotcha of button-and-toggle).

- [x] ✅ **6.1 Write ScrollViewAllocationTests.cs (3 tests)** - Completed 2026-10-26
  - Description: Write `tests/BrutalUi.Core.Tests/Elements/ScrollViewAllocationTests.cs` with 3 tests. The scene: a column root with 10 views (200 x 100), each with 20 rows (300 x 40, the odd rows not pickable), a slider in the first view, the last view in `Disabled`, and a sheet with `ScrollView`, `ScrollView:hover`, `ScrollView:active`, and `:disabled` rules. `UpdateAndDrawAllocateNothingWithScrollViews` (100 rounds of `Update` and `Draw`). `TheScrollViewInputPathAllocatesNothing` (each step pushes, on the first view, a scroll down, a scroll up, a press on a row that is not pickable, a move, a move outside the view, a release, a press and a move on the slider at a scrolled origin, and a release, then runs `Update` and `Draw`). `TheMeasuredBodyDoesRealWork` (after one step: at least one `ScrollChanged` call, at least one `ValueChanged` call, an offset that is not 0 on the first view, and 10 `PushClip` calls in the frame).
  - File(s): `tests/BrutalUi.Core.Tests/Elements/ScrollViewAllocationTests.cs`
  - Estimate: 10 steps

- [x] ✅ **6.2 Plant an allocation and restore it** - Completed 2026-10-26
  - Description: Copy `src/BrutalUi.Core/Elements/ScrollView.cs` to the scratchpad. Plant `GC.KeepAlive(new object());` as the first line of `ScrollView.HandleScroll`. Run `dotnet test --solution brutal-ui.sln > /dev/null 2>&1; echo "exit=$?"` and expect a value that is not 0. Restore the file from the copy, in a later tool call, and check it with `cmp`. Record the result and the gates that failed in `context.md`.
  - File(s): `src/BrutalUi.Core/Elements/ScrollView.cs` (planted and restored), `docs/features/interactive-elements/scroll-view/context.md`
  - Estimate: 5 steps

- [x] ✅ **6.3 Technical design section 5** - Completed 2026-10-26
  - Description: Edit `docs/technical-design.md` section 5: a "ScrollView" paragraph after the slider paragraph (the offset, the clip by the inline overflow, the content size, the clamp at use, the scroll rule and its sign, the stop rule, the drag, the capture, the disabled rule, and `ScrollChanged`). Edit rule 5 of the widget rules (`WidgetInput` also holds `IsScroll`, and `ViewportOrigin` subtracts the offset of each ancestor view). State the Offset Rule of the Walks.
  - File(s): `docs/technical-design.md`
  - Estimate: 5 steps

- [x] ✅ **6.4 Technical design sections 8, 9, 14 and the changelog** - Completed 2026-10-26
  - Description: Edit `docs/technical-design.md` section 8, "The draw walk" (the rect form: a `ScrollView` moves its children by its offset, and its clip does not move), section 9, item 3 of the flow (the hit rules: the same offset), and section 14 (the scroll view test classes and the eleventh gate with the plant). Edit `CHANGELOG.md` under `## [Unreleased]`: "Added" (`ScrollView`) and "Changed" (the hit walk and the draw walk move the children of a `ScrollView` by its offset).
  - File(s): `docs/technical-design.md`, `CHANGELOG.md`
  - Estimate: 6 steps

- [x] ✅ **6.5 Lint the prose** - Completed 2026-10-26
  - Description: Run `python3 /home/aaronstatic/.claude/skills/asd-ste100/scripts/ste-lint.py --fail-over 2.5` on each changed doc and on this plan. Fix each finding above 2.5.
  - File(s): `docs/technical-design.md`, `CHANGELOG.md`, `docs/features/interactive-elements/scroll-view/context.md`
  - Estimate: 4 steps

- [x] ✅ **6.6 Run the final gate and record it** - Completed 2026-10-26
  - Description: Run the build, the tests, and `scripts/test-linux.sh; echo "exit=$?"`. Record the final test counts against the counts of task 1.1 in `context.md`.
  - File(s): `docs/features/interactive-elements/scroll-view/context.md`
  - Estimate: 4 steps

---

## Bugs & Issues

**Active Bugs:**
- None

**Fixed Bugs:**
- None

---

## Technical Debt

- CR-1, the code part: content before the leading edge of the view (`*-reverse`, `justify-content` other than `flex-start` on the scroll axis) is out of reach. The docs state the rule. An offset range from the near edge waits for a person (follow-up question 1 of `code-review.md`).
- The follow-up questions 2 to 7 of `code-review.md`: a pending set before the first `Update`, `Hover` after a wheel, a press on a view that cannot scroll, the cost of the clamp at use (for the `ListView` epic), and the `flex-grow` sizing (for widget-sample).

---

## Task Status Legend

- [ ] Not started
- [ ] 🔄 In progress
- [ ] ⏸️ Blocked (waiting on something)
- [x] ✅ Completed
- [x] ❌ Cancelled/Won't do

---

## Progress Tracking

### Completed This Session (2026-10-26 to 2026-10-27)
- Phases 1 to 6 (29 tasks), each with verifier PASS and Demo: PASS.
- The cross-phase review: 14 findings checked off in `code-review.md` (groups `review-g1.md` to `review-g9.md`), verifier PASS.

### Discovered New Tasks
- None yet

### Blocked Items
- None

---

## Notes

### Task Estimation
One step is one tool call of an agent. A typical task costs about six steps (see `.beast-mode/phase-handoff.md` section 1).
- 🟢 Small (up to 6 steps)
- 🟡 Medium (7 to 15 steps)
- 🔴 Large (more than 15 steps, so split the task)

---

*Update this file as tasks are completed. Mark tasks with ✅ immediately when done. Add new tasks as they're discovered.*
