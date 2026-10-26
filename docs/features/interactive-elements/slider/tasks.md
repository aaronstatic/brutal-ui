# interactive-elements/slider - Task Checklist

**Last Updated:** 2026-10-26 09:15
**Progress:** 41/41 tasks complete (100%)
**Advanced phases:** Phases 1 and 5 (ADVANCED, opus). Phases 2, 3, 4, 6, 7, and 8 are STANDARD (sonnet).

Source paths: `Core/` means `src/BrutalUi.Core/`. `Tests/` means `tests/BrutalUi.Core.Tests/`. "The plan" means `implementation.md` of this feature. Its sections "Architecture Overview", "Internal Types", "The Drag Rule", and "Key Technical Decisions" hold the rules that the tasks name. Each phase ends with `dotnet build brutal-ui.sln 2>&1 | tail -n 30` and `dotnet test --solution brutal-ui.sln 2>&1 | tail -n 30` (tool rule 5). Expect `0 Warning(s)`, `0 Error(s)`, and 0 failed. The projects have no implicit usings, so each new file holds its own `using` lines. Each public member needs an XML doc comment (CS1591 is an error). Use `namespace BrutalUi.Core;` in Core and `namespace BrutalUi.Core.Tests;` in the tests. Prose in comments follows ASD-STE100. A test class that interns a class name uses a name prefix that is unique to the class, because xUnit runs the classes in parallel. Run one class with `dotnet run --project tests/BrutalUi.Core.Tests/BrutalUi.Core.Tests.csproj -- --filter-class '*<Class>' 2>&1 | tail -n 5`. Each phase runs these classes with no edit: the pointer and focus classes (`PointerPressTests`, `PointerStateEdgeTests`, `PointerHoverTests`, `PointerAllocationTests`, `ClickFocusTests`, `FocusOwnerTests`, `TabNavigationTests`, `FocusStateEdgeTests`, `FocusAllocationTests`, `RoutingGateTests`) and the button-and-toggle classes (`ReleaseOrderTests`, `ButtonClickTests`, `ButtonKeyTests`, `ToggleTests`, `ButtonTextTests`, `WidgetDisabledTests`, `WidgetAllocationTests`). From phase 2 on, each phase also runs `ChordClickTests`. The test scene of each slider test is `SliderTree` (task 2.4): a slider of 120 x 20 at (40, 0) with no padding and a range of 0 to 100, so the value `v` sits at the x `50 + v`.

---

## Phase 1: The Press Target (CR-1) (6/6 complete)

**Demo:** a unit test pushes, through `UiContext.PushEvent` on `WidgetTree`, a right press over the `Button`, a left press over `Other`, a right release, and a left release over the `Button`, and runs `DrainEvents`. It reads 0 `Clicked` calls (the code of the base commit gives 1). The developer runs `dotnet run --project tests/BrutalUi.Core.Tests/BrutalUi.Core.Tests.csproj -- --filter-class '*ChordClickTests'` and reads a total of 8 with `failed: 0`.
**Needs:** none
**Agent tier:** ADVANCED (opus)
**Tier note:** This phase changes the press state of `UiContext` and the click rule of each widget. Change only `PressButton`, the walk step and the `finally` block of `DrainEvents`, and the new members of the Internal Types section. Do not change the hit test, `SetHover`, `PointerHit`, the button mask, `ReleaseButton`, `FinishRelease`, the life of the `Active` bit, the click focus, the Tab step, the stale rules, `RoutePointer`, `PointerEvent.cs`, or `EventWalk.cs`. When task 1.1 finds a test that asserts the old chord rule, stop and record it in `context.md`, because the brief allows no edit of an existing test.

- [x] ✅ **1.1 Record the base and the blast radius** - Completed 2026-10-26
  - Description: Record in `docs/features/interactive-elements/slider/context.md`, before any change: the base commit (`git rev-parse HEAD`), the test counts of the solution and of `BrutalUi.Core.Tests`, and the counts of each class that the phase intro names. Record the Chord Finding of the Architecture Overview and the blast radius: each caller of `PressButton`, `ReleaseButton`, `FinishRelease`, and `WidgetInput.IsClick`, each test that pushes two buttons down at one time, and each test that pushes a `PointerUp` with a caller target. Confirm that no test asserts a click for a chord or for a release with a caller target.
  - File(s): `docs/features/interactive-elements/slider/context.md`
  - Estimate: 8 steps

- [x] ✅ **1.2 Add the press button, the chord flag, and the press target to UiContext** - Completed 2026-10-26
  - Description: Edit `src/BrutalUi.Core/Tree/UiContext.cs` by Decision 13: add `_activeButton`, `_activeChord`, `LeftPressElement`, `t_pressTarget`, and `CurrentPressTarget` (Internal Types). In `PressButton`, read the other buttons of the mask before the bit of this press goes in. When the press sets `Active`, write `_activeButton` and write `_activeChord` (another button is down). When an element holds `Active` already, set `_activeChord`. In `DrainEvents`, save the old press target at the start. After the target step of each entry, set the press target (Internal Types). Write the old value back after the walk step and in the `finally` block. Edit the doc comments: the fields, `PressButton`, the remarks of `DrainEvents` (the press target), and the remarks of `ActiveElement` (`Active` marks the first press, and the press target holds the left press with no chord). Name Decision 2 of button-and-toggle and Decision 13 of this plan.
  - File(s): `src/BrutalUi.Core/Tree/UiContext.cs`
  - Estimate: 10 steps

- [x] ✅ **1.3 IsClick reads the press target** - Completed 2026-10-26
  - Description: Edit `src/BrutalUi.Core/Elements/WidgetInput.cs`: `IsClick` returns `true` when the button is `Left`, `UiContext.CurrentPressTarget` is the widget, and the widget does not hold `Disabled`. Replace the chord-limit paragraph of its remarks with the chord rule: a click needs the left press that set `Active` with no other button down during the gesture, and a release with a caller target gives no click. Extend the class remarks: the drag checks of the later widgets read the same press target.
  - File(s): `src/BrutalUi.Core/Elements/WidgetInput.cs`
  - Estimate: 4 steps

- [x] ✅ **1.4 Edit the remarks of Button and Toggle** - Completed 2026-10-26
  - Description: Edit the remarks of `src/BrutalUi.Core/Elements/Button.cs` and `src/BrutalUi.Core/Elements/Toggle.cs` only. Replace "The limit of a chord" paragraph with the chord rule of task 1.3: a chord gives no click (no flip). Replace the paragraph on a `PointerUp` with a caller target: that event gives no click (no flip), and the real release clicks one time. Keep the advice to push a pointer event with no target. Change no code line.
  - File(s): `src/BrutalUi.Core/Elements/Button.cs`, `src/BrutalUi.Core/Elements/Toggle.cs`
  - Estimate: 4 steps

- [x] ✅ **1.5 Write ChordClickTests.cs** - Completed 2026-10-26
  - Description: Write `tests/BrutalUi.Core.Tests/Elements/ChordClickTests.cs` with 8 tests on `WidgetTree`: `ARightPressOnTheButtonThenALeftPressElsewhereGivesNoClick` (the demo, order 1 of CR-1: 0 clicks, and the root bubble handler gets the left release), `ALeftReleaseAfterALeftPressElsewhereWhileRightIsHeldGivesNoClick` (order 2 of CR-1: a left press over the `Button`, a right press, a left release over `Other`, a left press over `Other`, a left release over the `Button`, and a right release give 0 clicks), `AChordOnTheButtonGivesNoClick` (a left press over the `Button`, a right press, a right release, and a left release over the `Button` give 0 clicks), `AButtonDownBeforeTheLeftPressMakesAChord` (a right press at `OverNothing`, a left press over the `Button`, a right release, and a left release over the `Button` give 0 clicks), `AToggleChordGivesNoFlip` (order 1 on the `Toggle`: `Checked` stays `false`, and 0 `CheckedChanged` calls), `AReleaseWithACallerTargetGivesNoClick` (a left press over the `Button` with no target, then a left `PointerUp` with the `Button` as the caller target: 0 clicks. The real release over the `Button` then gives 1 click), `APlainClickAfterAChordClicksOnce` (after the chord of `AChordOnTheButtonGivesNoClick`, a left press and a left release over the `Button` give 1 click), and `ThePressTargetFollowsTheLeftPress` (a root capture handler reads `UiContext.CurrentPressTarget`: the `Button` for a left press, a move, and the release over the `Button`. `null` for a right press and its release. `null` after each drain, also after a release handler that throws).
  - File(s): `tests/BrutalUi.Core.Tests/Elements/ChordClickTests.cs`
  - Estimate: 10 steps

- [x] ✅ **1.6 Run the phase 1 gate** - Completed 2026-10-26
  - Description: Run the build, the tests, the demo class, the classes of the phase intro, and each class in `tests/BrutalUi.Core.Tests/Input/`. Run `git diff <base> --stat -- tests/` and expect no changed existing file. Run `scripts/test-linux.sh; echo "exit=$?"` and expect the PASS line and `exit=0`. Record the results in `context.md`.
  - File(s): `docs/features/interactive-elements/slider/context.md`
  - Estimate: 5 steps

---

## Phase 2: A Press Moves the Value (6/6 complete)

**Demo:** a person runs `scripts/test-linux.sh` and reads its PASS line. In that run, `SliderPressTests.APressMovesTheValueToThePointer` pushes a left press at x 75 over a `Slider` (range 0 to 100) through `UiContext.PushEvent`, runs `Update`, and reads `Value` 25 and one `ValueChanged` call with the slider as the sender.
**Needs:** Phase 1
**Agent tier:** STANDARD (sonnet)

- [x] ✅ **2.1 Record the Move Route Finding** - Completed 2026-10-26
  - Description: Record in `context.md`: the Move Route Finding of the Architecture Overview, and the test counts after phase 1.
  - File(s): `docs/features/interactive-elements/slider/context.md`
  - Estimate: 3 steps

- [x] ✅ **2.2 Add ViewportOrigin and IsDragStart to WidgetInput** - Completed 2026-10-26
  - Description: Edit `src/BrutalUi.Core/Elements/WidgetInput.cs`: add `ViewportOrigin(Element)` and `IsDragStart(Element, in PointerEvent)` (Internal Types). `IsDragStart` reads `UiContext.CurrentPressTarget`, as `IsClick` does. Extend the class remarks: the drag checks serve each widget with a drag rule. Keep `IsClick`, `IsKeyActivation`, and the key constants as they are.
  - File(s): `src/BrutalUi.Core/Elements/WidgetInput.cs`
  - Estimate: 5 steps

- [x] ✅ **2.3 Write Slider.cs (the press)** - Completed 2026-10-26
  - Description: Write `src/BrutalUi.Core/Elements/Slider.cs` by the widget pattern: sealed, the type id of `Slider`, `public Slider(in CreateInfo info)`, `CreateInfo` with `Minimum`, `required Maximum`, and `Value` (phase 6 adds `Step`), `IsFocusable = true`, `OnPointerDown = s_pointerDown`, the get-only `Minimum` and `Maximum`, `Value` (a set calls `Apply`: clamp, compare, write, mark `DirtyFlags.Paint`), `public Action<Slider>? ValueChanged { get; set; }`, `UserSet`, `ValueAtPointer` (Decision 6), the geometry helpers, and the static `HandlePointerDown`. The constructor writes the start value through `Apply` and calls nothing. The remarks state: the press rule (a left press with no chord, Decision 13), the map of the thumb center, the stop (A4 of button-and-toggle), the sender, "read `ValueChanged`, not the slot", "a write of `=` to `OnPointerDown` removes the press", a code set calls nothing (Decision 5), and the slider has no own size (Decision 10).
  - File(s): `src/BrutalUi.Core/Elements/Slider.cs`
  - Estimate: 10 steps

- [x] ✅ **2.4 Write SliderTree.cs** - Completed 2026-10-26
  - Description: Write `tests/BrutalUi.Core.Tests/Elements/SliderTree.cs`: an internal test tree. The context has a `RecordingRenderer` measurer and a viewport of 800 x 600. The root is 400 x 300 in a row and is pickable. The children are `Lead`, a plain `Element` (40 x 40 at 0, 0), `Slider` (120 x 20 at 40, 0, no padding), and `Other`, a plain `Element` (100 x 100 at 160, 0). The constructor takes `in Slider.CreateInfo`. A second constructor uses the range 0 to 100 and the value 0. It holds `static float2 At(float value)` (x `50 + value`, y 10), the points `OverLead` (20, 10), `OverOther` (210, 50), `PastRight` (300, 10, over the root), and `OverNothing` (600, 500), and a `Settle()` method. Use the factories of `PointerTree` for the events. The doc comment states the rects and the map.
  - File(s): `tests/BrutalUi.Core.Tests/Elements/SliderTree.cs`
  - Estimate: 6 steps

- [x] ✅ **2.5 Write SliderPressTests.cs** - Completed 2026-10-26
  - Description: Write `tests/BrutalUi.Core.Tests/Elements/SliderPressTests.cs` with 10 tests: `APressMovesTheValueToThePointer` (the demo, through `PushEvent` and `Update`), `APressBeforeTheTrackGivesTheMinimum` (start 50, press at x 45), `APressPastTheTrackGivesTheMaximum` (press at x 155), `APressAtTheSameValueCallsNothing` (no call and no `DirtyFlags.Paint` after a settle), `ARightButtonPressDoesNothing` (the value stays, and the root bubble handler gets the press), `APressStopsTheWalk` (the root capture handler runs, the root bubble handler does not), `APressFocusesTheSlider` (`IsFocusable` is `true`, and `FocusedElement` is the slider), `ACodeSetClampsAndCallsNothing` (150 gives 100, -5 gives 0, a change marks `Paint`, the same value marks nothing, the call count stays 0), `TheCreateInfoSetsTheRangeAndTheStartValue` (a start value outside the range clamps, no call, and the type id is `Slider`), and `ATypeRuleMatchesTheSlider` (a rule with `Type = ElementType.Intern("Slider")` sets a fill on the slider and not on `Other`).
  - File(s): `tests/BrutalUi.Core.Tests/Elements/SliderPressTests.cs`
  - Estimate: 10 steps

- [x] ✅ **2.6 Run the phase 2 gate** - Completed 2026-10-26
  - Description: Run the build, the tests, the demo class, the classes of the phase intro, and `scripts/test-linux.sh; echo "exit=$?"`. Expect the PASS line and `exit=0`. Record the counts and each decision that this plan did not settle in `context.md`.
  - File(s): `docs/features/interactive-elements/slider/context.md`
  - Estimate: 4 steps

---

## Phase 3: The Thumb Draw (3/3 complete)

**Demo:** a unit test sets a `Slider` rule with a background color and a color, sets the value 25, and runs `Update` and `Draw` with a `RecordingRenderer`. It reads the track rect (40, 0, 120, 20) in the background color and then the thumb rect (65, 0, 20, 20) in the color. The developer runs `dotnet run --project tests/BrutalUi.Core.Tests/BrutalUi.Core.Tests.csproj -- --filter-class '*SliderDrawTests'` and reads a total of 6 with `failed: 0`.
**Needs:** Phase 2
**Agent tier:** STANDARD (sonnet)

- [x] ✅ **3.1 Draw the thumb in PaintContent** - Completed 2026-10-26
  - Description: Edit `src/BrutalUi.Core/Elements/Slider.cs`: override `internal override void PaintContent(IUiRenderer renderer, in float4 borderBox, float opacity)` (Decision 7). Compute the content box with `Layout.ContentBox(in borderBox)`. Return with no call when the content width or height is 0 or less, or when `Style.Color.ScaleAlpha(opacity)` has an alpha of 0. Else draw one `renderer.Rect` with the x `content.X + Ratio * (content.Z - ThumbWidth)`, the y `content.Y`, the width `ThumbWidth`, the height `content.W`, the scaled color, `Style.CornerRadius`, a border width of 0, and `Rgba8.Transparent`. `ValueAtPointer` uses the same helpers. Extend the remarks: the track is the box of the slider, the thumb uses `color`, and `Slider:hover`, `Slider:active`, `Slider:focus`, and `:disabled` rules style the thumb.
  - File(s): `src/BrutalUi.Core/Elements/Slider.cs`
  - Estimate: 6 steps

- [x] ✅ **3.2 Write SliderDrawTests.cs** - Completed 2026-10-26
  - Description: Write `tests/BrutalUi.Core.Tests/Elements/SliderDrawTests.cs` with 6 tests: `TheSliderDrawsTheTrackAndTheThumb` (the demo, the order of the two rects), `TheThumbFollowsTheValue` (the values 0, 50, and 100 give the x 40, 90, and 140), `TheThumbFitsTheContentBox` (a padding of 2 and a border of 1 give a thumb of 14 x 14 inside the content box, and a press maps through the same box), `AnActiveRuleRestylesTheThumb` (a `Slider:active` color shows on the thumb after a press and an `Update`, and the old color after the release), `TheOpacityScalesTheThumb` (an opacity of 0.5 halves the alpha, and a transparent color draws no thumb rect), and `ASmallSliderDrawsASafeThumb` (a zero range draws the thumb at the start, a slider narrower than tall draws a thumb of the content width, and a slider of 0 x 0 draws no thumb and gives no NaN).
  - File(s): `tests/BrutalUi.Core.Tests/Elements/SliderDrawTests.cs`
  - Estimate: 8 steps

- [x] ✅ **3.3 Run the phase 3 gate** - Completed 2026-10-26
  - Description: Run the build, the tests, the demo class, the classes of phases 1 and 2, and the classes of the phase intro. Record the results in `context.md`.
  - File(s): `docs/features/interactive-elements/slider/context.md`
  - Estimate: 4 steps

---

## Phase 4: The Drag (4/4 complete)

**Demo:** a unit test pushes a left press at the value 25 and a move to the value 60 over the slider, and runs `Update`. It reads `Value` 60 and two `ValueChanged` calls. The developer runs `dotnet run --project tests/BrutalUi.Core.Tests/BrutalUi.Core.Tests.csproj -- --filter-class '*SliderDragTests'` and reads a total of 9 with `failed: 0`.
**Needs:** Phase 3
**Agent tier:** STANDARD (sonnet)

- [x] ✅ **4.1 Add IsDragMove to WidgetInput** - Completed 2026-10-26
  - Description: Edit `src/BrutalUi.Core/Elements/WidgetInput.cs`: add `IsDragMove(Element, in PointerEvent)` (Internal Types). The doc comment states the press target rule: a right press or a chord gives no drag (Decisions 2 and 13).
  - File(s): `src/BrutalUi.Core/Elements/WidgetInput.cs`
  - Estimate: 3 steps

- [x] ✅ **4.2 Add the drag move to Slider** - Completed 2026-10-26
  - Description: Edit `src/BrutalUi.Core/Elements/Slider.cs`: add `s_pointerMove`, write it into `OnPointerMove` in the constructor, and add the static `HandlePointerMove` of the Drag Rule. Extend the remarks: the drag rule, the stop for each drag move (also with no change), the press button rule (a right press gives no drag, and a second button ends the drag until the last release), and "a drag that leaves the rect stops at the edge" (phase 5 removes this sentence).
  - File(s): `src/BrutalUi.Core/Elements/Slider.cs`
  - Estimate: 5 steps

- [x] ✅ **4.3 Write SliderDragTests.cs** - Completed 2026-10-26
  - Description: Write `tests/BrutalUi.Core.Tests/Elements/SliderDragTests.cs` with 9 tests: `AMoveAfterAPressDragsTheValue` (the demo), `AMoveToTheSameValueCallsNothing` (the call count stays, and the walk still stops), `AMoveWithNoPressDoesNothingAndWalksOn` (a hover move: no change, and the root bubble handler gets it), `AMoveAfterTheReleaseDoesNothing`, `APressElsewhereThenAMoveOverTheSliderDoesNothing` (a press on `Lead`, a move over the slider), `ARightPressThenAMoveDoesNotDrag` (a right press over the slider and a move over the slider: no change, and the root bubble handler gets the move), `ASecondButtonEndsTheDrag` (a left press at 25, a right press, and a move to 60: the value stays 25. After the release of both buttons, a new left press and a move drag again), `ADragMoveStopsTheWalk`, and `TheDelegateReadsTheNewValue`.
  - File(s): `tests/BrutalUi.Core.Tests/Elements/SliderDragTests.cs`
  - Estimate: 10 steps

- [x] ✅ **4.4 Run the phase 4 gate** - Completed 2026-10-26
  - Description: Run the build, the tests, the demo class, the classes of phases 1 to 3, and the classes of the phase intro. Record the results in `context.md`.
  - File(s): `docs/features/interactive-elements/slider/context.md`
  - Estimate: 4 steps

---

## Phase 5: The Drag Past the Edge (6/6 complete)

**Demo:** a unit test pushes a left press at the value 25 and a move to (300, 10), over the root and outside the slider, and runs `Update`. It reads `Value` 100, `Hover` on the root, and no `Hover` on the slider. The developer runs `dotnet run --project tests/BrutalUi.Core.Tests/BrutalUi.Core.Tests.csproj -- --filter-class '*DragCaptureTests'` and reads a total of 9 with `failed: 0`.
**Needs:** Phase 4
**Agent tier:** ADVANCED (opus)
**Tier note:** This phase changes `RoutePointer`, a central method of the drain. Add one branch for a `PointerMove` only. It reads `LeftPressElement` of phase 1, so a right press or a chord captures no move. Do not change the hit test, `SetHover`, `PointerHit`, the button mask, `PressButton`, `ReleaseButton`, `FinishRelease`, the press target, the click focus, the Tab step, the stale rules, or `EventWalk.cs`. No existing element implements `IDragCapture`, so the route of each existing test stays the same. Prove it with the classes of the phase intro and each `Input/` class with no edit.

- [x] ✅ **5.1 Record the blast radius of the capture** - Completed 2026-10-26
  - Description: Record the blast radius in `context.md`: list each caller of `RoutePointer` and each existing test that pushes a `PointerMove` while a button is down. Confirm that no element type of `src/` implements `IDragCapture` before this phase.
  - File(s): `docs/features/interactive-elements/slider/context.md`
  - Estimate: 4 steps

- [x] ✅ **5.2 Write IDragCapture.cs** - Completed 2026-10-26
  - Description: Write `src/BrutalUi.Core/Input/IDragCapture.cs`: the internal interface of the Internal Types section. The doc comment states the rule, the press button rule (a left press with no chord, Decision 13), the end of the capture (the release of the last button clears `Active`), and the limits: `Hover`, `PointerHit`, and the routing gate follow the hit. A move with a caller target keeps its target.
  - File(s): `src/BrutalUi.Core/Input/IDragCapture.cs`
  - Estimate: 3 steps

- [x] ✅ **5.3 Add the capture branch to RoutePointer** - Completed 2026-10-26
  - Description: Edit `src/BrutalUi.Core/Tree/UiContext.cs`: in `RoutePointer`, after `SetHover(hit)`, for a `PointerMove` only: when `LeftPressElement is IDragCapture { CapturesMoves: true }`, write that element into `entry.Header.Target` and return it. Else keep the code of today. Edit the doc comments: the summary and the remarks of `RoutePointer` (the return value is the hit or the capture element), the remarks of `DrainEvents` (the move rule), and the remarks of `ActiveElement`. Name Decision 8 of pointer-routing and Decisions 3 and 13 of this plan.
  - File(s): `src/BrutalUi.Core/Tree/UiContext.cs`
  - Estimate: 6 steps

- [x] ✅ **5.4 Slider implements IDragCapture** - Completed 2026-10-26
  - Description: Edit `src/BrutalUi.Core/Elements/Slider.cs`: implement `IDragCapture` explicitly. `CapturesMoves` returns `true` (phase 7 adds the `Disabled` rule). Replace the remark "a drag that leaves the rect stops at the edge" with the capture rule.
  - File(s): `src/BrutalUi.Core/Elements/Slider.cs`
  - Estimate: 3 steps

- [x] ✅ **5.5 Write DragCaptureTests.cs** - Completed 2026-10-26
  - Description: Write `tests/BrutalUi.Core.Tests/Input/DragCaptureTests.cs` with 9 tests on `SliderTree`: `ADragPastTheRightEndGivesTheMaximum` (the demo), `ADragPastTheLeftEndGivesTheMinimum` (a move over `Lead`), `ADragAboveOrBelowTheSliderReadsTheXOnly` (a move to (100, 60) gives 50), `ACapturedMoveKeepsHoverAndTheGateOnTheHit` (`Hover` on `Other`, `PointerHit` is `Other`, and `RoutingGate.PointerOverUi` is `true`. A move to `OverNothing` gives a null `PointerHit`, `PointerOverUi` `false`, and still a drag), `ACapturedMoveWalksThePathOfTheSlider` (the target of the move is the slider, a root capture handler runs, and a bubble handler on `Other` gets no call), `TheCaptureEndsAtTheRelease` (after a release over `Other`, a move over `Other` reaches `Other` and does not change the value), `AnElementWithNoCaptureKeepsTheHitRule` (a press on `Lead`, then a move over `Other`, walks to `Other`), `ARightPressCapturesNoMove` (a right press on the slider, then a move over `Other`: the move walks to `Other`, and the value stays), and `ARemovedSliderEndsTheCapture` (a remove of the slider during a drag, then a move: no throw, and the move walks to the hit).
  - File(s): `tests/BrutalUi.Core.Tests/Input/DragCaptureTests.cs`
  - Estimate: 10 steps

- [x] ✅ **5.6 Run the phase 5 gate** - Completed 2026-10-26
  - Description: Run the build, the tests, the demo class, the classes of phases 1 to 4, the classes of the phase intro, and each class in `tests/BrutalUi.Core.Tests/Input/`. Run `git diff <base> --stat -- tests/` and expect no changed existing file. Record the results in `context.md`.
  - File(s): `docs/features/interactive-elements/slider/context.md`
  - Estimate: 5 steps

---

## Phase 6: The Step and the Arrow Keys (7/7 complete)

**Demo:** a unit test calls `SetFocus(slider)` on a slider with the range 0 to 100, the step 10, and the value 50. It pushes a Right arrow key down with no target and runs `Update`. It reads `Value` 60 and one `ValueChanged` call. The developer runs `dotnet run --project tests/BrutalUi.Core.Tests/BrutalUi.Core.Tests.csproj -- --filter-class '*SliderKeyTests'` and reads a total of 9 with `failed: 0`, and the same command with `'*SliderStepTests'` reads a total of 9 with `failed: 0`.
**Needs:** Phase 5
**Agent tier:** STANDARD (sonnet)

### Group A: the step

- [x] ✅ **6.1 Add the step, Normalize, and SetRange to Slider** - Completed 2026-10-26
  - Description: Edit `src/BrutalUi.Core/Elements/Slider.cs`: add `Step` to `CreateInfo`, the get-only `Step`, and `SetRange(float minimum, float maximum, float step)`. Write `Normalize` by Decision 4, and use it in `Apply`. The constructor and `SetRange` check the range first (Decision 9). On a bad value, they throw `ArgumentOutOfRangeException` with the name of the bad parameter, and `SetRange` changes nothing. `SetRange` writes the range and then calls `Apply` with the old value: it marks `Paint` on a change of the range or of the value, and it calls nothing. Extend the remarks: the value rule, the grid from `Minimum`, a range that is not a multiple of the step, and the NaN rule.
  - File(s): `src/BrutalUi.Core/Elements/Slider.cs`
  - Estimate: 8 steps

- [x] ✅ **6.2 Write SliderStepTests.cs** - Completed 2026-10-26
  - Description: Write `tests/BrutalUi.Core.Tests/Elements/SliderStepTests.cs` with 9 tests: `ASetRoundsToTheNearestStep` (the step 10: 23 gives 20, 25 gives 30, 27 gives 30), `TheGridStartsAtTheMinimum` (the range 5 to 105 with the step 10: 12 gives 15, 9 gives 5), `AStepOfZeroKeepsTheValue` (33.3 stays), `ANaNValueGivesTheMinimum`, `ARangeThatIsNoMultipleOfTheStep` (0 to 10 with the step 3: 10 gives 9. With the step 4: 10 gives 10), `AFractionalStepGivesTheGrid` (0 to 1 with the step 0.1: 0.34 gives 0.3 within 1e-6, and 1 gives 1), `APressAndADragRoundToTheStep` (a press at 23 gives 20 and one call. A move to 24 gives no call), `SetRangeClampsAndRoundsAndCallsNothing` (the value 73, then `SetRange(0, 50, 5)` gives 50, marks `Paint`, and calls nothing. The same range again marks nothing), and `ABadRangeThrowsAndKeepsTheState` (the constructor and `SetRange` throw for a minimum above the maximum, a NaN or infinite bound, a negative step, and a NaN or infinite step. After a throw of `SetRange`, the range and the value stay).
  - File(s): `tests/BrutalUi.Core.Tests/Elements/SliderStepTests.cs`
  - Estimate: 8 steps

- [x] ✅ **6.3 Run the group A gate** - Completed 2026-10-26
  - Description: Run the build, the tests, the class of task 6.2, and the classes of phases 1 to 5. Record the results in `context.md`.
  - File(s): `docs/features/interactive-elements/slider/context.md`
  - Estimate: 4 steps

### Group B: the arrow keys

- [x] ✅ **6.4 Add the arrow keys to WidgetInput** - Completed 2026-10-26
  - Description: Edit `src/BrutalUi.Core/Elements/WidgetInput.cs`: add `RightKey`, `LeftKey`, and `IsArrowKey(Element, in KeyEvent, out int direction)` (Decision 8). The doc comments name the GLFW values.
  - File(s): `src/BrutalUi.Core/Elements/WidgetInput.cs`
  - Estimate: 4 steps

- [x] ✅ **6.5 Add the key handler to Slider** - Completed 2026-10-26
  - Description: Edit `src/BrutalUi.Core/Elements/Slider.cs`: add `s_keyDown`, write it into `OnKeyDown` in the constructor, and add the static `HandleKeyDown`. After the check and the stop, it calls `UserSet(Value + direction * delta)`. The delta is `Step` when `Step` is above 0. Else it is `(Maximum - Minimum) / 100`. Extend the remarks: the key rule, the repeat, the stop at the end of the range, and "a write of `=` to `OnKeyDown` removes the keys".
  - File(s): `src/BrutalUi.Core/Elements/Slider.cs`
  - Estimate: 4 steps

- [x] ✅ **6.6 Write SliderKeyTests.cs** - Completed 2026-10-26
  - Description: Write `tests/BrutalUi.Core.Tests/Elements/SliderKeyTests.cs` with 9 tests: `ARightArrowMovesTheValueByOneStep` (the demo), `ALeftArrowMovesTheValueBack`, `AStepOfZeroMovesOnePercentOfTheRange` (the range 0 to 200: the value moves by 2), `TheArrowStopsAtTheEndOfTheRange` (the value 100 and a Right key: no call, and the walk stops), `ARepeatMovesTheValue`, `AModifierBlocksTheArrow` (`Control`, `Alt`, and `Super` give no move, and the key walks on. `Shift` moves), `OtherKeysDoNothing` (Up 265, Down 264, Space, Enter, and a key up of Right give no move, and the root bubble handler gets each one), `AKeyFromAFocusableChildDoesNothing` (a focusable child of the slider owns the focus, and Right bubbles to the slider with no move), and `TabThenRightMovesTheValue` (a Tab with no owner reaches the slider, after `Lead`, because `Lead` is not focusable).
  - File(s): `tests/BrutalUi.Core.Tests/Elements/SliderKeyTests.cs`
  - Estimate: 8 steps

- [x] ✅ **6.7 Run the phase 6 gate** - Completed 2026-10-26
  - Description: Run the build, the tests, the demo class, the classes of phases 1 to 5 and of task 6.2, and the classes of the phase intro. Record the results in `context.md`.
  - File(s): `docs/features/interactive-elements/slider/context.md`
  - Estimate: 4 steps

---

## Phase 7: Disabled Sliders (3/3 complete)

**Demo:** a unit test sets `Disabled` on the slider, pushes a left press at the value 25, and runs `Update`. It reads the old value, 0 `ValueChanged` calls, and one call of the root bubble handler for the press. The developer runs `dotnet run --project tests/BrutalUi.Core.Tests/BrutalUi.Core.Tests.csproj -- --filter-class '*SliderDisabledTests'` and reads a total of 6 with `failed: 0`.
**Needs:** Phase 6
**Agent tier:** STANDARD (sonnet)

- [x] ✅ **7.1 Add the disabled rule** - Completed 2026-10-26
  - Description: Edit `src/BrutalUi.Core/Elements/WidgetInput.cs`: `IsDragStart`, `IsDragMove`, and `IsArrowKey` return `false` when `widget.States` holds `Disabled` (Decision 11). Edit `src/BrutalUi.Core/Elements/Slider.cs`: `CapturesMoves` returns `false` when the slider holds `Disabled`. Extend the remarks of `Slider`: a disabled slider takes no input, does not stop the walk, and captures no move. A code set of `Value` and `SetRange` still work.
  - File(s): `src/BrutalUi.Core/Elements/WidgetInput.cs`, `src/BrutalUi.Core/Elements/Slider.cs`
  - Estimate: 5 steps

- [x] ✅ **7.2 Write SliderDisabledTests.cs** - Completed 2026-10-26
  - Description: Write `tests/BrutalUi.Core.Tests/Elements/SliderDisabledTests.cs` with 6 tests: `ADisabledSliderIgnoresAPress` (the demo), `ADisableDuringADragStopsTheDrag` (a move over the slider changes nothing and walks on. An enable during the same press drags again), `ADisabledSliderIgnoresTheArrows` (a key with the slider as the caller target, because the drain drops a disabled owner), `ADisabledSliderCapturesNoMove` (a disable during a drag, then a move to `PastRight`: the move walks to the root, and the value stays), `ACodeSetWorksOnADisabledSlider` (`Value` and `SetRange`), and `APressOnADisabledSliderTakesNoFocus`.
  - File(s): `tests/BrutalUi.Core.Tests/Elements/SliderDisabledTests.cs`
  - Estimate: 7 steps

- [x] ✅ **7.3 Run the phase 7 gate** - Completed 2026-10-26
  - Description: Run the build, the tests, the demo class, the classes of phases 1 to 6, and the classes of the phase intro. Record the results in `context.md`.
  - File(s): `docs/features/interactive-elements/slider/context.md`
  - Estimate: 4 steps

---

## Phase 8: The Allocation Gate and the Record (6/6 complete)

**Demo:** the developer runs `dotnet run --project tests/BrutalUi.Core.Tests/BrutalUi.Core.Tests.csproj -- --filter-class '*SliderAllocationTests'` and reads a total of 3 with `failed: 0`. A planted allocation at the start of `Slider.HandlePointerMove` makes `dotnet test --solution brutal-ui.sln` exit with a value that is not 0.
**Needs:** Phase 7
**Agent tier:** STANDARD (sonnet)
**Tier note:** Copy the gate shape of `WidgetAllocationTests` and `FocusAllocationTests`: a `Scene` class that the test builds before the measure, delegates from instance methods that the scene creates one time, `AllocationGate.ReleaseAllocationContext()` before the measured region, and a measured body with no lambda, no `$"`, no new `string`, and no `Assert`. Do not add warm-up rounds to hide an allocation. Do not run a plant and its restore in one parallel tool block (a gotcha of button-and-toggle).

- [x] ✅ **8.1 Write SliderAllocationTests.cs** - Completed 2026-10-26
  - Description: Write `tests/BrutalUi.Core.Tests/Elements/SliderAllocationTests.cs` with 3 tests. The scene: a column root with 20 sliders (200 x 20, the range 0 to 100, the step 0), the last one in `Disabled`, and a sheet with `Slider`, `Slider:hover`, `Slider:active`, `Slider:focus`, and `:disabled` rules. `UpdateAndDrawAllocateNothingWithSliders` (100 rounds of `Update` and `Draw`). `TheSliderInputPathAllocatesNothing` (each step pushes, on the first slider, a press at 25, a move to 60, a move past the right end of the root, a release, a Left key, and a Right key, then runs `Update` and `Draw`). `TheMeasuredBodyDoesRealWork` (after one step: 5 `ValueChanged` calls, the value 100, and 20 thumb rects in the frame).
  - File(s): `tests/BrutalUi.Core.Tests/Elements/SliderAllocationTests.cs`
  - Estimate: 8 steps

- [x] ✅ **8.2 Plant an allocation and restore it** - Completed 2026-10-26
  - Description: Copy `src/BrutalUi.Core/Elements/Slider.cs` to the scratchpad. Plant `GC.KeepAlive(new object());` as the first line of `Slider.HandlePointerMove`. Run `dotnet test --solution brutal-ui.sln > /dev/null 2>&1; echo "exit=$?"` and expect a value that is not 0. Restore the file from the copy, in a later tool call, and check it with `cmp`. Record the result and the gates that failed in `context.md`.
  - File(s): `src/BrutalUi.Core/Elements/Slider.cs` (planted and restored), `docs/features/interactive-elements/slider/context.md`
  - Estimate: 5 steps

- [x] ✅ **8.3 Record the slider rules in technical-design section 5** - Completed 2026-10-26
  - Description: Edit `docs/technical-design.md` section 5: a "Slider" paragraph after the widget paragraphs (the value rule, the map of the thumb center, the press, the drag, the arrow keys, the draw of the track and the thumb, the disabled rule, and `ValueChanged`). Replace the chord limit of the click rule with the chord rule of Decision 13: a click and a drag need the left press that set `Active` with no chord, and a release with a caller target gives no click. Edit rule 5 of the ten widget rules (`WidgetInput` also holds `IsDragStart`, `IsDragMove`, `IsArrowKey`, and `ViewportOrigin`, and each pointer check reads `UiContext.CurrentPressTarget`). Add the drag rule and the capture of `IDragCapture`. Edit item 4 of "The pointer step of the drain": a move with no target walks to the element of the left press when it captures the moves.
  - File(s): `docs/technical-design.md`
  - Estimate: 7 steps

- [x] ✅ **8.4 Record section 9, section 14, and the changelog** - Completed 2026-10-26
  - Description: Edit `docs/technical-design.md` section 9, item 4 (the capture of a move), and section 14 (`ChordClickTests`, the slider test classes, `DragCaptureTests`, and the tenth gate with the plant). Edit `CHANGELOG.md` under `## [Unreleased]`: "Added" (`Slider`), and "Changed" (a chord gives no click, and a `PointerUp` with a caller target gives no click (CR-1, CR-6). A pointer move with no target walks to a dragged slider while it holds the left press).
  - File(s): `docs/technical-design.md`, `CHANGELOG.md`
  - Estimate: 5 steps

- [x] ✅ **8.5 Lint the changed docs** - Completed 2026-10-26
  - Description: Run `python3 /home/aaronstatic/.claude/skills/asd-ste100/scripts/ste-lint.py --fail-over 2.5` on each changed doc and on this plan. Fix each finding above 2.5.
  - File(s): `docs/technical-design.md`, `CHANGELOG.md`, `docs/features/interactive-elements/slider/implementation.md`
  - Estimate: 4 steps

- [x] ✅ **8.6 Run the final gate** - Completed 2026-10-26
  - Description: Run the build, the tests, and `scripts/test-linux.sh; echo "exit=$?"`. Record the final test counts against the counts of task 1.1 in `context.md`.
  - File(s): `docs/features/interactive-elements/slider/context.md`
  - Estimate: 4 steps

---

## Bugs & Issues

**Active Bugs:**
- None

**Fixed Bugs:**
- None

---

## Technical Debt

- [x] ✅ 💳 **The class remarks of `SliderDragTests.cs` name the capture as later work** - Resolved 2026-10-26 (review fix CR-14, `review-g4.md`)
  - Description: The remarks say "the capture comes in a later phase". Phase 5 added the capture. The file was outside the file list of phase 5.
  - Reason: Found by the phase 5 agent.
  - Effort: one doc edit (the cross-phase review fixes it).

---

## Task Status Legend

- [ ] Not started
- [ ] 🔄 In progress
- [ ] ⏸️ Blocked (waiting on something)
- [x] ✅ Completed
- [x] ❌ Cancelled/Won't do

---

## Progress Tracking

### Completed This Session (2026-10-26)
- ✅ Phases 1 to 8 (41 tasks). Each verifier gave PASS and Demo: PASS.
- ✅ The cross-phase review: 15 findings, 15 fixed (`code-review.md`).

### Discovered New Tasks
- None. The review fixes live in `code-review.md`.

---

## Notes

### Task Estimation
One step is one tool call of an agent. A typical task costs about six steps (see `.beast-mode/phase-handoff.md` section 1).
- 🟢 Small (up to 6 steps)
- 🟡 Medium (7 to 15 steps)
- 🔴 Large (more than 15 steps, so split the task)

---

*Update this file as tasks are completed. Mark tasks with ✅ immediately when done. Add new tasks as they're discovered.*
