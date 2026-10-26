# interactive-elements/slider - Context & Decisions

**Last Updated:** 2026-10-26 09:15
**Current Phase:** Done (phases 1 to 8 and the cross-phase review)
**Status:** ✅ Ready for Review

---

## Quick Status

**What's Done:**
- ✅ The plan is accepted (8 phases, 41 tasks). The person confirmed Q2 to Q5 on 2026-10-26. Q1 (the demo of phase 2) uses the recommended option.
- ✅ The feature docs exist (`tasks.md`, `context.md`, `inbox.md`).
- ✅ The branch `feat/interactive-elements/slider` holds this feature. button-and-toggle is committed (`21abb3c`).
- ✅ Phase 1 (tasks 1.1 to 1.6): the press target in `UiContext`, `IsClick`, the `Button` and `Toggle` remarks, and `ChordClickTests` (8 pass). The verifier gave PASS and Demo: PASS. See "Phase 1 Results".
- ✅ Phase 2 (tasks 2.1 to 2.6): `Slider`, `SliderTree`, `SliderPressTests` (10 pass). The verifier gave PASS and Demo: PASS. See "Phase 2 Results".
- ✅ Phase 3 (tasks 3.1 to 3.3): the thumb draw in `PaintContent`, `SliderDrawTests` (6 pass). The verifier gave PASS and Demo: PASS. See "Phase 3 Results".
- ✅ Phase 4 (tasks 4.1 to 4.4): `IsDragMove` and the drag move of `Slider`, `SliderDragTests` (9 pass). The verifier gave PASS and Demo: PASS. See "Phase 4 Results".
- ✅ Phase 5 (tasks 5.1 to 5.6): `IDragCapture`, the capture branch of `RoutePointer`, and `DragCaptureTests` (9 pass). The verifier gave PASS and Demo: PASS. See "Phase 5 Results".
- ✅ Phase 6 (tasks 6.1 to 6.7): the step, `Normalize`, `SetRange`, `IsArrowKey`, and the key handler. `SliderStepTests` (9) and `SliderKeyTests` (9) pass. The verifier gave PASS and Demo: PASS. See "Phase 6 Results".
- ✅ Phase 7 (tasks 7.1 to 7.3): the disabled rule in `WidgetInput` and `CapturesMoves`, `SliderDisabledTests` (6 pass). The verifier gave PASS and Demo: PASS. See "Phase 7 Results".
- ✅ Phase 8 (tasks 8.1 to 8.6): `SliderAllocationTests` (3 pass), the plant (exit 2, restored with `cmp` 0), section 5, 9, and 14 of the technical design, and the changelog. The verifier gave PASS and Demo: PASS. See "Phase 8 Results".
- ✅ The cross-phase review: 15 findings (1 HIGH, 4 MEDIUM, 10 LOW), 15 fixed. The verifier of the fixes gave PASS. The solution gives 2401 tests (Core 1569, 69 more than the base), 0 failed, 2 skipped. See "Review Fixes".
- ✅ The draft plan of the next feature exists: `docs/features/interactive-elements/scroll-view/implementation.md` (6 phases, 29 tasks).

**What's Next:**
- 📋 A person runs the items of "Needs Human Verification".
- 📋 A person answers the follow-up questions under "Open Questions".
- 📋 `/autorun-epic` commits the feature and runs the epic rollup.

**Blockers:**
- None

---

## Key Files

### Core Implementation
- `src/BrutalUi.Core/Elements/Slider.cs` - The `Slider` widget (phases 2 to 7).
- `src/BrutalUi.Core/Elements/WidgetInput.cs` - The press target check of `IsClick`, the drag checks, the arrow key check, and `ViewportOrigin`.
- `src/BrutalUi.Core/Input/IDragCapture.cs` - The opt-in drag capture (phase 5).
- `src/BrutalUi.Core/Tree/UiContext.cs` - The press button, the chord flag, the press target (phase 1), and the capture branch of `RoutePointer` (phase 5).

### Related Files
- `src/BrutalUi.Core/Elements/Button.cs`, `src/BrutalUi.Core/Elements/Toggle.cs` - The remarks of the chord rule (phase 1).
- `tests/BrutalUi.Core.Tests/Elements/WidgetTree.cs` - The scene of `ChordClickTests` (no edit).
- `tests/BrutalUi.Core.Tests/Elements/SliderTree.cs` - The scene of each slider test (phase 2).
- `docs/features/interactive-elements/slider/implementation.md` - Implementation plan.

---

## Important Decisions

The plan holds Decisions 1 to 13 and the confirmed items Q2 to Q5. This section records each decision that the build adds. The "Phase N Results" sections hold the small decisions of each phase (P1-1 to P8).

- **S1 (phase 5, P5-1): the capture branch checks the roots.** A handler can remove the slider during a drain. The stale rule runs only at the start of a drain, so the branch also needs `IsInRootTree(capture)`. Else each later move of that drain targets the removed slider and gets lost.
- **S2 (review CR-1): the capture branch checks that the element is shown.** A slider that a style change hides keeps `Active`, and its layout box is 0. With no check, each captured move sets `Minimum`. The branch now also needs `FocusWalk.IsShown(capture)`, and the move walks to the hit.
- **S3 (review CR-2): the map computes in `double`.** The `float` map gave 60.0000038 for a pixel center. The `double` map rounds one time to `float`, so a pixel center gives the exact value. The tests assert each mapped value with no tolerance.
- **S4 (review CR-7): the constructor throws with `ParamName` `info`.** `ValidateRange` takes a flag that picks the parameter name. `SetRange` keeps `minimum`, `maximum`, and `step`.
- **S5 (review CR-9): `IsClick` reads the event type.** Each check of `WidgetInput` now reads the event type, so a copy of a handler in another slot does nothing.
- **S6 (review fixes): no new test method.** The review fixes extend existing test methods, so the class totals of the Definition of Done stay (the convention of button-and-toggle).

---

## Gotchas & Learnings

- From button-and-toggle: a stop inside a slot also skips the later delegates of the same slot (`EventWalk.InvokeSlot`).
- From button-and-toggle: do not run a planted allocation and its restore in one parallel tool block.
- A `float` map of a pixel center can be 1 ulp off (60.0000038 for 60). Compute a ratio map in `double` and round one time, or the tests need a tolerance that hides the error.
- A capture that routes moves past the hit test must repeat each rule that the hit test gives for free: the element is in the roots and is shown.
- A test of a rule that the scene cannot show passes for the wrong reason. `ARightButtonPressDoesNothing` failed on the `Left` check, not on the press target, and the scene put the slider directly under the root, so `ViewportOrigin` had no ancestor to add. The review added the cases.

---

## API Integration

- `Slider.ValueChanged` (`Action<Slider>?`): the delegate slot that the game sets at build time.
- The slider reads the event slots `OnPointerDown`, `OnPointerMove`, and `OnKeyDown`, the press target of the context, and the focus owner of input-routing.

---

## State Management

- `Slider` holds `_value`, `_minimum`, `_maximum`, and `_step`. It holds no drag flag (Decision 2).
- `UiContext` holds `_activeButton`, `_activeChord`, and the thread-static press target for one walk (Decision 13).

---

## Testing Approach

### Unit Tests
**Files:** `tests/BrutalUi.Core.Tests/Elements/*Tests.cs`, `tests/BrutalUi.Core.Tests/Input/DragCaptureTests.cs`
- `ChordClickTests` (8), `SliderPressTests` (10), `SliderDrawTests` (6), `SliderDragTests` (9), `DragCaptureTests` (9), `SliderStepTests` (9), `SliderKeyTests` (9), `SliderDisabledTests` (6).

### Allocation Tests
**Files:** `tests/BrutalUi.Core.Tests/Elements/SliderAllocationTests.cs`
- 3 tests: `Update` and `Draw`, the input path with a captured move, and the real-work check.

---

## Needs Human Verification

`/autorun-feature` adds one line per item that the verifier cannot check, for a person to test.

- [ ] Phase 1 demo: a unit test pushes, through `UiContext.PushEvent` on `WidgetTree`, a right press over the `Button`, a left press over `Other`, a right release, and a left release over the `Button`, and runs `DrainEvents`. It reads 0 `Clicked` calls (the code of the base commit gives 1). The developer runs `dotnet run --project tests/BrutalUi.Core.Tests/BrutalUi.Core.Tests.csproj -- --filter-class '*ChordClickTests'` and reads a total of 8 with `failed: 0`.
- [ ] Phase 2 demo: a person runs `scripts/test-linux.sh` and reads its PASS line. In that run, `SliderPressTests.APressMovesTheValueToThePointer` pushes a left press at x 75 over a `Slider` (range 0 to 100) through `UiContext.PushEvent`, runs `Update`, and reads `Value` 25 and one `ValueChanged` call with the slider as the sender.
- [ ] Phase 3 demo: a unit test sets a `Slider` rule with a background color and a color, sets the value 25, and runs `Update` and `Draw` with a `RecordingRenderer`. It reads the track rect (40, 0, 120, 20) in the background color and then the thumb rect (65, 0, 20, 20) in the color. The developer runs `dotnet run --project tests/BrutalUi.Core.Tests/BrutalUi.Core.Tests.csproj -- --filter-class '*SliderDrawTests'` and reads a total of 6 with `failed: 0`.
- [ ] Phase 4 demo: a unit test pushes a left press at the value 25 and a move to the value 60 over the slider, and runs `Update`. It reads `Value` 60 and two `ValueChanged` calls. The developer runs `dotnet run --project tests/BrutalUi.Core.Tests/BrutalUi.Core.Tests.csproj -- --filter-class '*SliderDragTests'` and reads a total of 9 with `failed: 0`.
- [ ] Phase 5 demo: a unit test pushes a left press at the value 25 and a move to (300, 10), over the root and outside the slider, and runs `Update`. It reads `Value` 100, `Hover` on the root, and no `Hover` on the slider. The developer runs `dotnet run --project tests/BrutalUi.Core.Tests/BrutalUi.Core.Tests.csproj -- --filter-class '*DragCaptureTests'` and reads a total of 9 with `failed: 0`.
- [ ] Phase 6 demo: a unit test calls `SetFocus(slider)` on a slider with the range 0 to 100, the step 10, and the value 50. It pushes a Right arrow key down with no target and runs `Update`. It reads `Value` 60 and one `ValueChanged` call. The developer runs `dotnet run --project tests/BrutalUi.Core.Tests/BrutalUi.Core.Tests.csproj -- --filter-class '*SliderKeyTests'` and reads a total of 9 with `failed: 0`, and the same command with `'*SliderStepTests'` reads a total of 9 with `failed: 0`.
- [ ] Phase 7 demo: a unit test sets `Disabled` on the slider, pushes a left press at the value 25, and runs `Update`. It reads the old value, 0 `ValueChanged` calls, and one call of the root bubble handler for the press. The developer runs `dotnet run --project tests/BrutalUi.Core.Tests/BrutalUi.Core.Tests.csproj -- --filter-class '*SliderDisabledTests'` and reads a total of 6 with `failed: 0`.
- [ ] Phase 8 demo: the developer runs `dotnet run --project tests/BrutalUi.Core.Tests/BrutalUi.Core.Tests.csproj -- --filter-class '*SliderAllocationTests'` and reads a total of 3 with `failed: 0`. A planted allocation at the start of `Slider.HandlePointerMove` makes `dotnet test --solution brutal-ui.sln` exit with a value that is not 0.
- [ ] Decide whether a captured drag needs a "pointer captured by UI" rule: `RoutingGate.PointerOverUi` reads `false` while a person drags a slider over the game world (follow-up of the slider review).
- [ ] Decide whether a drag needs the walk of the press to reach the widget: a capture handler on an ancestor that stops the press does not stop the drag, and the click has the same rule (follow-up of the slider review).

---

## Performance Considerations

- Each slot of `Slider` holds a cached static delegate. The press target is one reference store, so it allocates nothing.

---

## Next Steps

### Immediate (Today/This Session)
1. A person runs the items of "Needs Human Verification".
2. `/autorun-epic` commits the feature. The next feature is `interactive-elements/scroll-view`, and its draft plan exists.

### Short Term
1. A person answers the follow-up questions under "Open Questions".
2. scroll-view keeps the origin rule of `WidgetInput.ViewportOrigin` and adds a test with a slider inside a scrolled view.

---

## Phase 1 Results

### Task 1.1: the base record (before any change)

- Base commit: `21abb3cfa92bbdaed22971e289150037ac87224b`.
- Base build: `0 Warning(s)`, `0 Error(s)`.
- Base solution tests: total 2332, succeeded 2330, skipped 2, failed 0.
- Base `BrutalUi.Core.Tests`: total 1500, succeeded 1500, failed 0.
- Base counts of the classes of the phase intro (each with failed 0): `PointerPressTests` 8, `PointerStateEdgeTests` 8, `PointerHoverTests` 5, `PointerAllocationTests` 3, `ClickFocusTests` 8, `FocusOwnerTests` 8, `TabNavigationTests` 12, `FocusStateEdgeTests` 8, `FocusAllocationTests` 3, `RoutingGateTests` 8, `ReleaseOrderTests` 4, `ButtonClickTests` 8, `ButtonKeyTests` 9, `ToggleTests` 10, `ButtonTextTests` 9, `WidgetDisabledTests` 7, `WidgetAllocationTests` 4.

**The Chord Finding.** `PressButton` sets `Active` on the first press of a gesture only. `Active` stays until the last button goes up. `IsClick` reads `Left` and `Active` only. Thus a left release clicks each widget that holds `Active`, also when the left press of that release was on another element (orders 1 and 2 of CR-1). A widget holds no context reference, and `Active` alone cannot tell which press set it. A drag that reads `Active` has the same defect.

**The blast radius.**
- `PressButton`: one caller, `RoutePointer` (`UiContext.cs`, the `PointerDown` branch).
- `ReleaseButton`: one caller, `RoutePointer` (the `PointerUp` branch). No change in this phase.
- `FinishRelease`: two callers, the step after the walk in `DrainEvents` and the `finally` block of `DrainEvents`. No change in this phase.
- `WidgetInput.IsClick`: two callers, `Button.HandlePointerUp` and `Toggle.HandlePointerUp`. No other `src/` file and no test calls it.
- The tests that push two buttons down at one time: `PointerPressTests.ASecondButtonKeepsActiveUntilTheLastRelease`, `PointerStateEdgeTests` (the test that removes `Right` and then presses the right button while the left button is down), and `ClickFocusTests.ARightButtonPressMovesTheFocus` (a right press, a middle press, and a second right press with no release). Each one uses `PointerTree` with plain elements. They assert `Active`, the mask, and the focus only. They assert no click, so the new rule does not touch them.
- The tests that push a `PointerUp` with a caller target: none. The caller targets in `tests/` are on a `PointerDown` (`EventKindTests`, `PointerStateEdgeTests`), a `PointerMove` (`RoutingGateTests`), a scroll, a key, or a char, or they go through generic helpers of the `Input/` walk tests on trees with no widget. No such test has a `Button` or a `Toggle`.
- Confirmed: no test asserts a click (or a flip) for a chord, or for a release with a caller target. `ButtonClickTests.ARightButtonDoesNotClick` and `ToggleTests.AReleaseElsewhereOrARightButtonDoesNotFlip` push a right (and a middle) press and release with no chord. They give no press target, so they pass with no edit.

### Tasks 1.2 to 1.5: the changes

- `UiContext.cs`: the fields `_activeButton`, `_activeChord`, and `t_pressTarget`, the private property `LeftPressElement`, and `internal static CurrentPressTarget`. `PressButton` computes the bit of this press, reads `otherDown` from the mask without that bit, and then writes the bit. With an element in `Active`, it sets `_activeChord`. Else, when the press hits an element, it sets `Active` and writes `_activeButton` and `_activeChord = otherDown`. `DrainEvents` saves `t_pressTarget` before `_draining` goes on, sets it after the target step, writes it back after the walk step (before `FinishRelease`), and writes it back in the `finally` block (before `FinishRelease`). The doc comments of the fields, `_activeElement`, `PressButton`, `DrainEvents`, and `ActiveElement` name Decision 2 of button-and-toggle and Decision 13 of slider.
- `WidgetInput.cs`: `IsClick` returns `e.Button == Left && ReferenceEquals(UiContext.CurrentPressTarget, widget)` after the `Disabled` check. The remarks hold the chord rule. The class remarks name the press target and the later drag checks.
- `Button.cs`, `Toggle.cs`: `///` lines only (checked with `git diff -U0`).
- `tests/BrutalUi.Core.Tests/Elements/ChordClickTests.cs`: the 8 tests of task 1.5.

### Task 1.6: the results

- Build: `0 Warning(s)`, `0 Error(s)`.
- Solution tests: total 2340, succeeded 2338, skipped 2, failed 0 (base 2332 + 8).
- `BrutalUi.Core.Tests`: total 1508, failed 0 (base 1500 + 8).
- `ChordClickTests`: total 8, failed 0.
- Each class of the phase intro: the same counts as the base, each with failed 0.
- Each class in `tests/BrutalUi.Core.Tests/Input/` (the 20 classes, also `EventAllocationTests`, `EventKindTests`, `EventQueueTests`, `EventWalkEdgeTests`, `EventWalkTests`, `HitTestRuleTests`, `HitTestTests`, `ScrollRoutingTests`, and `WindowCallbackTests`): failed 0.
- `git diff 21abb3c --stat -- tests/`: no changed tracked file. The only new test file is `ChordClickTests.cs`.
- `PointerEvent.cs`, `Element.cs`, and `EventWalk.cs`: no diff. `RoutePointer`, `ReleaseButton`, `FinishRelease`, and `DropStalePointerState`: no code change.
- `scripts/test-linux.sh`: `test-linux: PASS build, test, gpu, smoke (120 frames) in 15 s`, `exit=0`.
- A planted check: with the old `IsClick` rule (`Active` in place of the press target), 7 of the 8 tests fail, and the demo reads 1 click. `ThePressTargetFollowsTheLeftPress` passes on both rules, because it tests the context only. The restore came after the run, in a separate step.

### Decisions that the brief does not settle

- **P1-1. A second press of the same button is a chord.** The brief says: "When an element holds `Active` already, set `_activeChord`." The code applies it to each press, also a press over no element and a second press of the same button after a dropped `PointerUp` (the ring limit). After a drop, the next release then gives no click, and the release after it ends the gesture. The ring limit of `ActiveElement` already covers this case.
- **P1-2. The release does not clear `_activeButton` and `_activeChord`.** `FinishRelease` must not change. `LeftPressElement` reads `_activeElement` first, and `PressButton` writes both fields each time it sets `Active`. A stale value thus never reaches a walk. The stale rule (`DropStalePointerState`) also leaves them, for the same reason.
- **P1-3. The write-back runs before `FinishRelease`.** The press target goes back right after the walk step, so the Tab step and `FinishRelease` run with the old value. Neither one reads it.
- **P1-4. The save runs before `_draining` goes on.** A nested drain of the same context throws before it reads or writes `t_pressTarget`. A nested drain of another context saves the target of the outer walk and writes it back.
- **P1-5. The class remarks of `WidgetInput`.** The sentence "A check reads the event and the states of the widget only" now names the press target too, because `IsClick` reads it.
- **P1-6. `tasks.md` is not in the `## Files` list of the brief.** The phase did not mark its tasks there. The caller marks tasks 1.1 to 1.6.

### Follow-up and migration risks

- `IsClick` does not read the event type. A copy of the click handler in `OnPointerDown` or `OnPointerMove` clicks during the press, because the press target is set for those walks. The base code has the same behavior (`Active` is set before those walks). No widget does this.
- A game handler that read `Active` to find a click keeps the old chord behavior. Only `Button` and `Toggle` use `IsClick` now. Phase 2 and later read `CurrentPressTarget` for the drag checks.

## Phase 2 Results

### Task 2.1: the Move Route Finding and the counts after phase 1

- `DrainEvents` calls `RoutePointer` for each pointer entry with no target. `RoutePointer` hit-tests the position, moves `Hover`, writes the hit into `entry.Header.Target`, and returns the hit. For a `PointerMove`, it changes no other state. The walk goes from the hit up to the root. Thus a move reaches the slider only when the pointer is over the slider or over a descendant. No pointer capture exists. A fast drag that leaves the rect stops the value short of the end. Phase 4 delivers the drag inside the rect. Phase 5 adds the opt-in capture.
- Counts after phase 1: solution 2340 (2338 succeeded, 2 skipped, 0 failed), Core 1508.

### Tasks 2.2 to 2.5: the changes

- `WidgetInput.cs`: `ViewportOrigin(Element)` and `IsDragStart(Element, in PointerEvent)`. The class remarks name the drag checks. `IsClick`, `IsKeyActivation`, and the key constants have no change. The file now has `using Brutal.Numerics;`.
- `Slider.cs`: new. No lambda, and the only delegate is the `static readonly` field `s_pointerDown`.
- `SliderTree.cs`, `SliderPressTests.cs`: new (10 tests).
- `UiContext.cs`, `Element.cs`, and `EventWalk.cs`: no change in this phase (`UiContext.cs` shows in `git status` from phase 1 only).

### Task 2.6: the results

- Build: `0 Warning(s)`, `0 Error(s)`.
- Solution tests: total 2350, succeeded 2348, skipped 2, failed 0 (phase 1: 2340 + 10).
- `BrutalUi.Core.Tests`: 1518 (1508 + 10), by the sum of the solution totals. Failed 0.
- `SliderPressTests`: total 10, failed 0.
- Each class of the phase intro (18 classes, with `ChordClickTests`): the same counts as the base of phase 1, each with failed 0.
- `scripts/test-linux.sh`: `test-linux: PASS build, test, gpu, smoke (120 frames) in 13 s`, `exit=0`.

### Decisions that the brief does not settle

- **P2-1. `ValueAtPointer` and `Apply` are `private`.** The brief lists them as private members. The tests reach them through a press and `Value`.
- **P2-2. `Apply` uses `Math.Clamp(value, _minimum, _maximum)`.** A range with a minimum above the maximum throws `ArgumentException` from the constructor. A NaN value passes the clamp and compares as a change. Phase 6 adds the range check and the NaN rule (`Normalize`).
- **P2-3. `Ratio` clamps to 0 and 1.** The helper is `internal static` and has no caller until phase 3. The press path clamps the pointer ratio on its own, in `ValueAtPointer`.
- **P2-4. `ViewportOrigin` has no early-out for a null parent chain.** It loops from the element up through `Parent`, as `PointerTree.ViewportRect` does. A scroll view adds its offset here later (doc comment).
- **P2-5. `IsDragStart` also reads the event type.** The brief says "the type is `PointerDown`". The check reads `e.Header.Type`, so a copy of the handler in another slot does nothing.
- **P2-6. `APressAtTheSameValueCallsNothing` checks `Paint` after `Update`.** The press sets `Active` and `Focus`, and the style pass does not mark `Paint` with no rule. The test passes, so this holds in the current scene.

## Phase 3 Results

### Task 3.1: the changes

- `Slider.cs`: `PaintContent` override (Decision 7) and the class remarks of the draw. The method computes the content box, returns on a content width or height of 0 or less, returns on a scaled color with alpha 0, and else sends one `Rect` call (x `content.X + Ratio * (content.Z - ThumbWidth)`, y `content.Y`, width `ThumbWidth`, height `content.W`, border width 0, border color transparent). It allocates nothing.
- `PaintWalk.cs` and `StylePass.cs`: no diff against the base commit (`git diff --stat` is empty).

### Task 3.2: `SliderDrawTests` (6 tests)

`TheSliderDrawsTheTrackAndTheThumb`, `TheThumbFollowsTheValue`, `TheThumbFitsTheContentBox`, `AnActiveRuleRestylesTheThumb`, `TheOpacityScalesTheThumb`, and `ASmallSliderDrawsASafeThumb`. The tests intern no class name.

### Task 3.3: the results

- Build: `0 Warning(s)`, `0 Error(s)`.
- Solution tests: total 2356, succeeded 2354, skipped 2, failed 0 (phase 2: 2350 + 6). `BrutalUi.Core.Tests` is 1524 (1518 + 6).
- `SliderDrawTests`: total 6, failed 0.
- `ChordClickTests` 8, `SliderPressTests` 10, and each class of the phase intro (17 classes): the same counts as the base of phase 1, each with failed 0.

### Decisions that the brief does not settle

- **P3-1. The 0 x 0 slider still sends the track rect.** `PaintWalk` sends a rect for a visible fill with no check of the area (the renderer skips it). So `ASmallSliderDrawsASafeThumb` expects one rect (the track, width 0 and height 0) and no thumb rect. The test checks that no rect has a NaN component.
- **P3-2. A narrow slider has a thumb that is not square.** The brief sets the width to `ThumbWidth` and the height to `content.W`. A slider of 10 x 20 gives a thumb of 10 x 20. Decision 7 says "square", which holds only when the content is as wide as tall or wider.
- **P3-3. The guard uses `!(x > 0f)`.** A NaN content size draws no thumb, in the same way as a size of 0 or less.
- **P3-4. The test of opacity reads the track alpha too.** An opacity of 0.5 gives alpha 128 on the track and on the thumb (`ScaleAlpha` rounds 127.5 up).

## Phase 4 Results

### Tasks 4.1 and 4.2: the changes

- `WidgetInput.cs`: `IsDragMove(Element, in PointerEvent)`. The check reads the event type and `UiContext.CurrentPressTarget` only. The doc comment states the press target rule (a right press or a chord gives no drag).
- `Slider.cs`: `s_pointerMove`, the write `OnPointerMove = s_pointerMove` in the constructor, and the static `HandlePointerMove`. The remarks now state the drag rule, the stop for each drag move (also with no change), the press button rule, and the sentence "a drag that leaves the rect stops at the edge" (phase 5 removes it). The file has no lambda and no `bool` field.
- `UiContext.cs`, `Element.cs`, and `EventWalk.cs`: no change in this phase.

### Task 4.3: `SliderDragTests` (9 tests)

`AMoveAfterAPressDragsTheValue`, `AMoveToTheSameValueCallsNothing`, `AMoveWithNoPressDoesNothingAndWalksOn`, `AMoveAfterTheReleaseDoesNothing`, `APressElsewhereThenAMoveOverTheSliderDoesNothing`, `ARightPressThenAMoveDoesNotDrag`, `ASecondButtonEndsTheDrag`, `ADragMoveStopsTheWalk`, and `TheDelegateReadsTheNewValue`.

### Task 4.4: the results

- Build: `0 Warning(s)`, `0 Error(s)`.
- Solution tests: total 2365, succeeded 2363, skipped 2, failed 0 (phase 3: 2356 + 9).
- `SliderDragTests`: total 9, failed 0. Demo: press at 25, move to 60, `Value` 60, 2 calls.
- `ChordClickTests` 8, `SliderPressTests` 10, `SliderDrawTests` 6, and each class of the phase intro (17 classes): failed 0, the same counts as in phase 3.

### Decisions that the brief does not settle

- **P4-1. The tests use a tolerance for a mapped value of 60 and 80.** The map gives `60.0000038` for the x 110 (float rounding in the divide and the multiply). The tests compare with a tolerance of 0.001. The value 25 maps exactly, so the phase 2 tests need no tolerance. The slider code has no change for this. Phase 5 and later tests should use the same tolerance for any value other than 25.
- **P4-2. `ASecondButtonEndsTheDrag` counts three calls.** The first left press gives call 1, the move after the right press gives none, and the second left press (at the value 10) and the move to 60 give calls 2 and 3.
- Review fix CR-2: the map uses `double`, and the tests assert each mapped value exactly.

## Phase 5 Results

### Task 5.1: the blast radius (before any change)

- `RoutePointer` has one caller: the target step of `DrainEvents` (`UiContext.cs`), for a `PointerMove`, `PointerDown`, `PointerUp`, or `Scroll` entry with no target. No other `src/` file and no test calls it. The change touches the `PointerMove` path of that one caller only.
- `LeftPressElement` has one reader before this phase: the press target step of `DrainEvents`. The capture branch is the second reader.
- `IDragCapture`: no type of `src/` declares or implements it before this phase (`grep -rn "IDragCapture\|CapturesMoves" src` is empty).
- The existing tests that push a `PointerMove` with no target while a button is down:
  - `PointerAllocationTests` (the scene `DownSlot`, `MoveModal`, `UpModal`): a left press on a plain slot element, then a move over the modal. Plain elements only.
  - `ClickFocusTests.AMoveAReleaseAndAScrollKeepTheFocus`: no press goes first, so no button is down. Listed because it pushes a move next to a release.
  - `ChordClickTests.ThePressTargetFollowsTheLeftPress`: a left press on the `Button`, then a move over the `Button`. `Button` has no capture.
  - `SliderDragTests`: `AMoveAfterAPressDragsTheValue`, `AMoveToTheSameValueCallsNothing`, `ADragMoveStopsTheWalk`, `TheDelegateReadsTheNewValue` (each move is over the slider, so the hit and the capture target are the same element), `APressElsewhereThenAMoveOverTheSliderDoesNothing` (the press is on `Lead`, a plain element), `ARightPressThenAMoveDoesNotDrag` (a right press gives no `LeftPressElement`), and `ASecondButtonEndsTheDrag` (a chord gives no `LeftPressElement`). Each one keeps its route.
  - `GlfwUiInputAllocationTests` (`Push` and `CallHandlers`): the rounds push left presses with no release, so the moves of a later round run with the left button down. The scene has plain elements only.
- Other pointer tests (`PointerPressTests`, `PointerStateEdgeTests`, `PointerHoverTests`, `RoutingGateTests`, `ScrollRoutingTests`, `GlfwUiInputTests`, `GlfwUiInputKeyTests`) push no move while a button is down, or they push a move with a caller target. A caller target skips `RoutePointer`.
- Conclusion: before this phase, no element can be a capture element, so each existing route stays the same.

### Tasks 5.2 to 5.5: the changes

- `src/BrutalUi.Core/Input/IDragCapture.cs`: new. `internal interface IDragCapture { bool CapturesMoves { get; } }` in `namespace BrutalUi.Core;`. The remarks state the capture rule, the press button rule (Decision 13), the end of the capture (the release of the last button, a chord, the stale rule), the limits (`Hover`, `PointerHit`, and the routing gate follow the hit, a caller target keeps its target), and the ring limit (a dropped release keeps the capture).
- `UiContext.cs`: one branch in `RoutePointer`, after `SetHover(hit)` and before the `Active` step: for a `PointerMove`, when `LeftPressElement is IDragCapture { CapturesMoves: true } and Element capture` and `IsInRootTree(capture)`, the method writes `capture` into `entry.Header.Target` and returns it. Doc comments: the summary, the remarks, and the `<returns>` of `RoutePointer`, a new "move rule" paragraph in the remarks of `DrainEvents`, and the remarks of `ActiveElement`. No other code of `UiContext.cs` changes in this phase.
- `Slider.cs`: `Slider : Element, IDragCapture`, and `bool IDragCapture.CapturesMoves => true;` (explicit). The remark "a drag that leaves the rect stops at the edge" is now the capture rule.
- `tests/BrutalUi.Core.Tests/Input/DragCaptureTests.cs`: new, the 9 tests of task 5.5.

### Task 5.6: the results

- Build: `0 Warning(s)`, `0 Error(s)`.
- Solution tests: total 2374, succeeded 2372, skipped 2, failed 0 (phase 4: 2365 + 9). `BrutalUi.Core.Tests` is 1542 (1533 + 9).
- `DragCaptureTests`: total 9, failed 0. Demo: press at 25, move to (300, 10), `Value` 100, `Hover` on the root, no `Hover` on the slider.
- Phases 1 to 4: `ChordClickTests` 8, `SliderPressTests` 10, `SliderDrawTests` 6, `SliderDragTests` 9, each with failed 0.
- The classes of the phase intro (17 classes): the same counts as the base of phase 1, each with failed 0.
- Each class in `tests/BrutalUi.Core.Tests/Input/`: failed 0 (`EventAllocationTests` 3, `EventKindTests` 12, `EventQueueTests` 9, `EventWalkEdgeTests` 5, `EventWalkTests` 13, `HitTestRuleTests` 11, `HitTestTests` 6, `ScrollRoutingTests` 4, `WindowCallbackTests` 4, and the classes of the phase intro).
- `git diff 21abb3c --stat -- tests/`: empty. No tracked test file changed. The new test files are untracked.
- `HitWalk.cs`, `EventWalk.cs`, and `Element.cs`: no diff against `21abb3c`.
- Planted checks (each one restored in a separate step before the next run): with `CapturesMoves => false`, 6 of the 9 tests fail (the 3 tests of the hit rule pass, as expected). With no root check in the capture branch, only `ARemovedSliderEndsTheCapture` fails (its in-drain part).

### Decisions that the brief does not settle

- **P5-1. The capture branch checks the roots (`IsInRootTree(capture)`).** The brief names the type pattern only. The stale rule (`DropStalePointerState`) runs at the start of a drain only. A handler that removes the slider during a drain (for example `ValueChanged`) leaves `_activeElement` on the removed slider until the release or the next drain. With no root check, each later move of that drain gets the removed slider as its target, and the walk step skips it, so the move is lost. With the check, the move walks to the hit. The check costs one parent walk for each captured move (the walk step does the same check). It allocates nothing. `ARemovedSliderEndsTheCapture` covers both cases: a remove between two updates (the stale rule) and a remove from `ValueChanged` in the same drain (the root check).
- **P5-2. The pattern binds `and Element capture`.** `LeftPressElement` is an `Element?`, so the pattern `is IDragCapture { CapturesMoves: true } and Element capture` gives a typed local with no cast. The interface is internal, so an outside type cannot implement it.
- **P5-3. The capture branch returns before the `Active` step.** A `PointerMove` never enters the press or the release branch, so the early return skips no state change.
- **P5-4. The press target of a captured move is the slider.** The press target step of `DrainEvents` reads `LeftPressElement` for each routed move, so `IsDragMove` gives `true` on the slider walk. No change of `DrainEvents` code is necessary.
- **P5-5. A captured move walks the path of the slider only.** The bubble handlers of the hit element get no call (`ACapturedMoveWalksThePathOfTheSlider`). A game handler on another element that reads moves during a slider drag gets none. The hover rule still moves `Hover` to the hit.
- **P5-6. The tests show a drag with no value change through a set from code.** A move over `Other` or over nothing maps to 100. `ACapturedMoveKeepsHoverAndTheGateOnTheHit` and `TheCaptureEndsAtTheRelease` set `Value = 25` from code (no `ValueChanged` call) before the move, so a captured move shows as a change to 100.

### Follow-up and migration risks

- The class remarks of `SliderDragTests` say "Each move goes over the slider, because the capture comes in a later phase". The sentence is now history. The file is not in the `## Files` list of this phase, so it has no edit.
- Phase 7 adds the `Disabled` rule to `CapturesMoves`. A slider that a handler disables during a drag then gives the moves back to the hit at the next move.
- scroll-view implements the same interface for its drag. The root check of P5-1 sits in `RoutePointer`, so it covers that drag too.
- A dropped release in a full ring keeps the capture with no button down (the ring limit of `Active`). The remarks of `IDragCapture` name it.

---

## Phase 6 Results

### Tasks done (6.1 to 6.7)

- 6.1: `Slider.cs` has `CreateInfo.Step`, the get-only `Step`, `SetRange(minimum, maximum, step)`, `ValidateRange`, and one `Normalize`. `Apply` calls `Normalize`. Each write of `_value` goes through `Apply`. The remarks hold the value rule, the grid from `Minimum`, the range that is not a multiple of the step, the NaN rule, the range check, and the key rule.
- 6.2: `SliderStepTests` has 9 tests.
- 6.4: `WidgetInput` has `RightKey` (262), `LeftKey` (263), and `IsArrowKey`.
- 6.5: `Slider.cs` has `s_keyDown`, the write in the constructor, and `HandleKeyDown`. The remarks hold the key rule, the repeat, the stop at the end of the range, and the `=` rule for `OnKeyDown`.
- 6.6: `SliderKeyTests` has 9 tests.

### Results of tasks 6.3 and 6.7

- `dotnet build brutal-ui.sln`: 0 Warning(s), 0 Error(s).
- `dotnet test --solution brutal-ui.sln`: total 2392, failed 0, succeeded 2390, skipped 2 (the two GLFW window tests: the machine has no display).
- `SliderStepTests`: total 9, failed 0. `SliderKeyTests`: total 9, failed 0 (the demo class).
- Phases 1 to 5: `ChordClickTests` 8, `SliderPressTests` 10, `SliderDrawTests` 6, `SliderDragTests` 9, `DragCaptureTests` 9. Each has failed 0.
- Phase intro classes, each with failed 0: `PointerPressTests` 8, `PointerStateEdgeTests` 8, `PointerHoverTests` 5, `PointerAllocationTests` 3, `ClickFocusTests` 8, `FocusOwnerTests` 8, `TabNavigationTests` 12, `FocusStateEdgeTests` 8, `FocusAllocationTests` 3, `RoutingGateTests` 8, `ReleaseOrderTests` 4, `ButtonClickTests` 8, `ButtonKeyTests` 9, `ToggleTests` 10, `ButtonTextTests` 9, `WidgetDisabledTests` 7, `WidgetAllocationTests` 4.
- `Slider.cs` holds no lambda. `required` stays on `Maximum` only. `ButtonKeyTests`, `ToggleTests`, and `TabNavigationTests` pass with no edit.

### Decisions that the brief does not settle

- P6-1: the parameter name of a bad range. The constructor and `SetRange` share `ValidateRange(minimum, maximum, step)`, so the exception names `minimum`, `maximum`, or `step` (the `SetRange` parameter names), also when the constructor throws for a create-info field. A minimum above the maximum names `minimum`. A NaN or infinite minimum names `minimum`. The check order is: minimum finite, maximum finite, minimum not above maximum, step finite and 0 or more.
- P6-2: `SetRange` marks `Paint` for a changed range even when the value stays the same, because the thumb position changes with the range. `Apply` marks the paint for a changed value. The method marks it a second time only when the value is the same and the range differs.
- P6-3: `_minimum`, `_maximum`, and `_step` are no longer `readonly`, because `SetRange` writes them. The public properties stay get-only.
- P6-4: the key handler stops the walk before the call of `UserSet`, also when `Apply` gives `false` (end of the range). This follows Decision 8.
- P6-5: the key delta for a step of 0 is `(Maximum - Minimum) / 100`, and the value then goes through `Normalize` (clamp only). A zero range gives the delta 0 and no move.
- P6-6: `OtherKeysDoNothing` also checks that a copy of the handler in `OnKeyUp` ignores a key up, because `IsArrowKey` reads the event type.

### Surprises

- None in the code. Both new test classes passed on the first run after a missing `using System;` in `SliderStepTests`.

### Follow-up

- Phase 7 adds the `Disabled` rule to `IsDragStart`, `IsDragMove`, `CapturesMoves`, and `IsArrowKey`. The doc comment of `IsArrowKey` names it.

---

## Phase 7 Results

### Done

- 7.1: `IsDragStart`, `IsDragMove`, and `IsArrowKey` in `WidgetInput.cs` return `false` for `Disabled` (Decision 11). `IDragCapture.CapturesMoves` in `Slider.cs` returns `(States & Disabled) == 0`. The class remarks of `Slider` and the doc comments of the three checks name the rule. The slider handlers hold no `Disabled` check.
- 7.2: `SliderDisabledTests` holds 6 tests: `ADisabledSliderIgnoresAPress` (the demo), `ADisableDuringADragStopsTheDrag`, `ADisabledSliderIgnoresTheArrows`, `ADisabledSliderCapturesNoMove`, `ACodeSetWorksOnADisabledSlider`, and `APressOnADisabledSliderTakesNoFocus`.
- 7.3: build, tests, and class runs done.

### Results

- `dotnet build brutal-ui.sln`: 0 Warning(s), 0 Error(s).
- `dotnet test --solution brutal-ui.sln`: total 2398, failed 0, succeeded 2396, skipped 2 (the two GLFW tests skip on a machine with no display).
- `SliderDisabledTests`: total 6, failed 0.
- Classes of phases 1 to 6, each with failed 0: `ChordClickTests` 8, `SliderPressTests` 10, `SliderDrawTests` 6, `SliderDragTests` 9, `DragCaptureTests` 9, `SliderStepTests` 9, `SliderKeyTests` 9.
- Classes of the phase intro, each with failed 0: `PointerPressTests` 8, `PointerStateEdgeTests` 8, `PointerHoverTests` 5, `PointerAllocationTests` 3, `ClickFocusTests` 8, `FocusOwnerTests` 8, `TabNavigationTests` 12, `FocusStateEdgeTests` 8, `FocusAllocationTests` 3, `RoutingGateTests` 8, `ReleaseOrderTests` 4, `ButtonClickTests` 8, `ButtonKeyTests` 9, `ToggleTests` 10, `ButtonTextTests` 9, `WidgetDisabledTests` 7 (no edit), and `WidgetAllocationTests` 4.

### Decisions the brief does not settle

- The demo test starts the slider at the value 50 (not 0) and presses at the value 25. A press that moved the value would then show in the result.
- `ADisabledSliderCapturesNoMove` also records `e.Header.Target` in the root move handler and asserts that it is the root. A walk to the slider (a capture that did not stop) would also bubble to the root, so the count alone would not show the capture rule. The target shows it.
- `ADisableDuringADragStopsTheDrag` pushes the events with `DrainEvents`, so each step has a known count of one entry.
- `APressOnADisabledSliderTakesNoFocus` sets `IsFocusable` on `Other` and uses it as the first focus owner, because `SliderTree` has no second focusable element.

### Notes

- A disabled slider keeps the left press element of the drain, so the press target stays the slider during the press. An enable during the same press drags again (tested).

---

## Phase 8 Results

### Done

- 8.1: `SliderAllocationTests.cs` holds 3 tests: `UpdateAndDrawAllocateNothingWithSliders`, `TheSliderInputPathAllocatesNothing`, and `TheMeasuredBodyDoesRealWork`. The scene is a column root of 400 x 400 (pickable) with 20 sliders of 200 x 20, the last one in `Disabled`, and a sheet with the five rules. The gate uses 2 warm-up rounds, as in `WidgetAllocationTests`.
- 8.2: the plant `GC.KeepAlive(new object());` as the first line of `Slider.HandlePointerMove` gave `exit=2`. One gate failed: `SliderAllocationTests.TheSliderInputPathAllocatesNothing` (solution: total 2401, failed 1). The restore ran in a later tool call. `cmp` of the copy (`Slider.cs.orig` in the scratchpad) and `Slider.cs` gave 0 (no difference), and `grep -c KeepAlive` gave 0.
- 8.3 and 8.4: `docs/technical-design.md` section 5 has the new "Slider" paragraph (value rule, thumb map, press, drag rule, capture, keys, draw, disabled rule, `ValueChanged`), the chord rule in place of the chord limit, the edited rule 5 of the ten widget rules, and the amended item 4 of "The pointer step of the drain". Section 9 item 4 states the capture. Section 14 names the slider test classes, `ChordClickTests`, `DragCaptureTests`, and the tenth gate with the plant. `CHANGELOG.md` has an "Added" line for `Slider` and two "Changed" lines (the chord rule and the capture).
- 8.5: lint (`--fail-over 2.5`) scores per 100 words: `technical-design.md` 1.53, `CHANGELOG.md` 1.84, `context.md` 1.99, `implementation.md` 1.82. No fix was needed, and `implementation.md` has no edit.
- 8.6: build, tests, and `scripts/test-linux.sh` done.

### Results

- `dotnet build brutal-ui.sln`: 0 Warning(s), 0 Error(s).
- `dotnet test --solution brutal-ui.sln`: total 2401, failed 0, succeeded 2399, skipped 2 (the two GLFW tests skip on a machine with no display). Base of task 1.1: 2332, so the solution gains 69 tests.
- Core: total 1569, failed 0. Base 1500 + 69 = 1569.
- `SliderAllocationTests`: total 3, failed 0 (`dotnet run --project tests/BrutalUi.Core.Tests/BrutalUi.Core.Tests.csproj -- --filter-class '*SliderAllocationTests'`).
- `scripts/test-linux.sh`: `exit=0`, `test-linux: PASS build, test, gpu, smoke (120 frames) in 11 s`.

### Decisions the brief does not settle

- The release of a step goes to the point past the root. A release with no hit and no capture has no target and no walk, so one step gives 5 walks (the press, 2 moves, 2 keys), not 6. The release still ends the press (the next step starts with a fresh press). `TheMeasuredBodyDoesRealWork` asserts 5 walks.
- The root has a fixed size of 400 x 400 and `IsPickable = true`, so the press hits the root area and a point at x 500 is outside it.
- The thumb count in the frame counts the rect calls of 20 x 20 (the slider height). The track rects are 200 wide, so they do not count.
- The step gives 5 `ValueChanged` calls in each step, because each press starts from the value 100 of the step before.

---

## Review Fixes

- The cross-phase review (`code-review.md`, brief `review.md`) ran 58 mutation probes and 5 scenario probes. It found 15 findings: 1 HIGH, 4 MEDIUM, and 10 LOW. 51 of the 58 mutations failed a test. 4 of the other 7 are equivalent mutants, and 3 were test gaps (CR-3, CR-4, CR-5).
- 15 findings are fixed. Fix briefs `review-g1.md` to `review-g8.md` fixed CR-1 to CR-14. The orchestrator fixed CR-15 (three notes about a tool hook) in this file.
  - CR-1 (HIGH): the capture branch of `RoutePointer` also needs `FocusWalk.IsShown(capture)`. A slider that a style change hides captures no move, so its value does not go to `Minimum`.
  - CR-2: `ValueAtPointer` computes in `double` and rounds one time to `float`. A pixel center now gives the exact value, and the tests assert each mapped value with no tolerance.
  - CR-3, CR-4, CR-5: the press target check of `IsDragStart`, the ancestor walk of `ViewportOrigin`, and the null press target of a caller-target move now have a test.
  - CR-6, CR-7: `ValidateRange` rejects a range with an infinite width. The constructor throws with `ParamName` `info`.
  - CR-9: `IsClick` also reads the event type (`PointerUp`).
  - CR-8, CR-10 to CR-14: doc comments, section 5 and section 9 of the technical design, and the changelog.
- Decision: the review fixes extend existing test methods and add no test method, so the class totals of the Definition of Done stay (the convention of button-and-toggle). The solution stays at 2401 tests (Core 1569).
- Verifier (brief `review-verify.md`): PASS. Demo: none. The solution gives 2401 tests, 0 failed, 2 skipped. `scripts/test-linux.sh` gives its PASS line. The lint gives 1.54 for `technical-design.md` and 1.81 for `CHANGELOG.md`.
- Follow-up questions of the review (no finding, a person decides): `RoutingGate.PointerOverUi` reads `false` while a person drags a slider over the game world. A capture handler on an ancestor that stops the press does not stop the drag (the click has the same rule).

---

## Open Questions

- Follow-up of the review: does a captured drag need a "pointer captured by UI" rule for `RoutingGate.PointerOverUi`? A person or the framework epic decides.
- Follow-up of the review: must a drag need the walk of the press to reach the widget, when an ancestor capture handler stops the press? The click has the same rule today.
- Follow-up of button-and-toggle: `:disabled` on an ancestor does not block a widget.

---

## Session Notes

### 2026-10-26 06:46
- `/autorun-feature` started the feature in the called mode of `/autorun-epic`. It created `tasks.md` (41 tasks in 8 phases), `context.md`, and `inbox.md`.

### 2026-10-26 07:10 (phase 1)
- Tasks 1.1 to 1.6 are done (brief `phase-1.md`, advanced agent). The click reads the press target of the context. A chord and a release with a caller target give no click (CR-1 and CR-6 of button-and-toggle).
- Verifier (brief `phase-1-verify.md`): PASS. Demo: PASS. The solution gives 2340 tests (Core 1508), 0 failed, 2 skipped. `scripts/test-linux.sh` gives its PASS line.
- The inbox holds no line. Phase 2 can start.

### 2026-10-26 07:25 (phase 2)
- Tasks 2.1 to 2.6 are done (brief `phase-2.md`, standard agent). A left press on the track moves `Value` to the pointer and calls `ValueChanged` one time.
- Verifier (brief `phase-2-verify.md`): PASS. Demo: PASS. The solution gives 2350 tests, 0 failed, 2 skipped. `scripts/test-linux.sh` gives its PASS line.
- The inbox holds no line. Phase 3 can start.

### 2026-10-26 07:29 (phase 3)
- Tasks 3.1 to 3.3 are done (brief `phase-3.md`, standard agent). `PaintContent` draws the thumb in the `Color` of the computed style. The box of the slider is the track.
- Verifier (brief `phase-3-verify.md`): PASS. Demo: PASS. The solution gives 2356 tests (Core 1524), 0 failed, 2 skipped. `PaintWalk.cs` and `StylePass.cs` have no diff.
- The inbox holds no line. Phase 4 can start.

### 2026-10-26 07:35 (phase 4)
- Tasks 4.1 to 4.4 are done (brief `phase-4.md`, standard agent). A move while the slider holds the left press drags the value. The slider holds no drag flag.
- Surprise: the map gives 60.0000038 for the x 110 (float rounding). The tests compare a mapped value with a tolerance of 0.001 (P4-1). The briefs of phases 5 and 6 carry the note.
- Verifier (brief `phase-4-verify.md`): PASS. Demo: PASS. The solution gives 2365 tests (Core 1533), 0 failed, 2 skipped.
- The inbox holds no line. Phase 5 can start.

### 2026-10-26 07:44 (phase 5)
- Tasks 5.1 to 5.6 are done (brief `phase-5.md`, advanced agent). A move with no target walks to the slider while it holds the left press, also outside its rect. `Hover`, `PointerHit`, and the routing gate follow the hit.
- Decision P5-1: the capture branch also checks that the slider is in the root tree, so a slider that a handler removes during a drain captures no later move of that drain.
- Open item: the class remarks of `SliderDragTests.cs` still name the capture as later work (a tech debt line in `tasks.md`).
- Verifier (brief `phase-5-verify.md`): PASS. Demo: PASS. The solution gives 2374 tests, 0 failed, 2 skipped. Each `Input/` class passes with no edit.
- The inbox holds no line. Phase 6 can start.

### 2026-10-26 07:59 (phase 6)
- Tasks 6.1 to 6.7 are done (brief `phase-6.md`, standard agent, one group: 7 tasks and 5 files). Each path that writes the value calls `Apply`, and `Apply` calls the one `Normalize`. The Left and Right keys move a focused slider.
- Verifier (brief `phase-6-verify.md`): PASS. Demo: PASS. The solution gives 2392 tests, 0 failed, 2 skipped.
- The inbox holds no line. Phase 7 can start.

### 2026-10-26 08:04 (phase 7)
- Tasks 7.1 to 7.3 are done (brief `phase-7.md`, standard agent). A disabled slider takes no press, no drag, no key, and no capture, and it does not stop the walk. The checks live in `WidgetInput`, and `CapturesMoves` holds the only check in `Slider.cs`.
- Verifier (brief `phase-7-verify.md`): PASS. Demo: PASS. The solution gives 2398 tests, 0 failed, 2 skipped. `WidgetDisabledTests` passes with no edit.
- The inbox holds no line. Phase 8 can start.

### 2026-10-26 08:11 (phase 8)
- Tasks 8.1 to 8.6 are done (brief `phase-8.md`, standard agent). The tenth gate measures 0 bytes. The plant failed one gate (`TheSliderInputPathAllocatesNothing`, exit 2), and the restore passed `cmp`. The lint gives 1.53 for `technical-design.md` and 1.84 for `CHANGELOG.md`.
- Verifier (brief `phase-8-verify.md`, the full Verification Method): PASS. Demo: PASS. The solution gives 2401 tests (Core 1569, 69 more than the base), 0 failed, 2 skipped. `scripts/test-linux.sh` gives its PASS line.
- Each phase is complete. The inbox holds no line. The cross-phase review can start.

### 2026-10-26 09:15 (the cross-phase review and the feature end)
- The cross-phase review found 15 findings (1 HIGH, 4 MEDIUM, 10 LOW) with 58 mutation probes. 8 fix briefs (`review-g1.md` to `review-g8.md`) fixed CR-1 to CR-14, and the orchestrator fixed CR-15. The verifier of the fixes gave PASS: 2401 tests, 0 failed, 2 skipped, and `scripts/test-linux.sh` PASS. See "Review Fixes".
- The draft plan of `interactive-elements/scroll-view` exists (6 phases, 29 tasks, ADVANCED phases 1 and 2). It needs the slider code, so `/autorun-epic` commits this feature first.
- The inbox holds no line. No bug goes to `docs/bugs/`.

---

*Update this file at the end of each work session. Run `/update-feature` before compacting conversations.*
