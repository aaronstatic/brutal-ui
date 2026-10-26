# interactive-elements/slider - Code Review

**Reviewed:** 2026-10-26
**Reviewer:** Agent (cross-phase review of phases 1 to 8)
**Rules Applied:** 1. A press belongs to one button, 2. The value is exact, 3. The pointer maps exactly, 4. A drag does not get lost, 5. One change, one call, 6. Disabled means no effect, 7. Zero allocation after warm-up, 8. No routing change for other elements, 9. The widget pattern and the drag rule, 10. Test quality, 11. `AGENTS.md` and the docs. The Vercel React rules do not apply to C#.
**Status:** Resolved - 15 findings, 15 fixed. Fix briefs `review-g1.md` to `review-g8.md` fixed CR-1 to CR-14, and the orchestrator fixed CR-15 in `context.md`. The verifier of the fixes gave PASS (`review-verify.md`): 2401 tests, 0 failed, 2 skipped, and `scripts/test-linux.sh` PASS.

---

## Summary

| Severity | Count | Findings |
|----------|-------|----------|
| HIGH | 1 | CR-1 |
| MEDIUM | 4 | CR-2, CR-3, CR-4, CR-5 |
| LOW | 10 | CR-6 to CR-15 |
| **Total** | **15** | |

The feature is in good shape. The chord rule, the press target, the opt-in capture, and the value rule follow the plan. The slider keeps each widget rule. The allocation gate catches each plant. 51 of the 58 mutations fail at least one test, and 4 of the 7 that pass are equivalent mutants.

The review found one defect with a wrong value. A captured drag goes on into a slider that a style change hides (CR-1). The layout gives the hidden slider a box of 0, so each move sets `Minimum` and calls `ValueChanged`. The other findings are a float map that is 1 ulp off for a pixel center (CR-2), three rules that no test guards (CR-3, CR-4, CR-5), and small API and doc items.

### Baseline (R.3)

- `dotnet build brutal-ui.sln`: `0 Warning(s)`, `0 Error(s)`.
- `dotnet test --solution brutal-ui.sln`: total 2401, succeeded 2399, skipped 2 (the two GLFW window tests, no display), failed 0.
- `BrutalUi.Core.Tests` in the scratch copy with no mutation: total 1569, failed 0. These totals match the record of phase 8.

### Probe method (R.4)

- The copy: `rsync -a --exclude bin --exclude obj --exclude .git --exclude artifacts` of the repository to `scratchpad/cr-slider/repo`. Before any change, `UiContext.cs`, `WidgetInput.cs`, `Slider.cs`, `SliderAllocationTests.cs`, and `Directory.Build.props` went to `scratchpad/cr-slider/orig/`.
- The `#if Mnn` method: `$(Mut)` goes at the end of `DefineConstants` in `Directory.Build.props` of the copy. Each mutation sits behind its own symbol. For each symbol, the probe ran `dotnet build tests/BrutalUi.Core.Tests -p:Mut=Mnn -p:TreatWarningsAsErrors=false` and then the `BrutalUi.Core.Tests` test host. Each build passed.
- Five scenario probes (P1 to P5) ran in a new test class `ReviewProbes` behind the symbol `PROBES`. They check edge cases that the plan does not name.
- After the last probe, each file came back from `orig/`, and `ReviewProbes.cs` was removed. `diff -rq --exclude bin --exclude obj` of `src` and of `tests` (copy against repository) gave no output and exit code 0. `cmp` of `Directory.Build.props` gave exit code 0. Then `rm -rf` removed the `cr-slider` folder.

### Probe list

The pointer route and the press target (`UiContext.cs`):

1. M01 `PressButton` ignores the other buttons at the press that sets `Active` (`_activeChord = false`): `ChordClickTests.AButtonDownBeforeTheLeftPressMakesAChord`.
2. M02 `_activeChord` never set: `ChordClickTests.AButtonDownBeforeTheLeftPressMakesAChord`, `AChordOnTheButtonGivesNoClick`, `APlainClickAfterAChordClicksOnce`, `SliderDragTests.ASecondButtonEndsTheDrag`.
3. M03 `LeftPressElement` ignores `_activeChord`: the same 4 tests as M02.
4. M04 `LeftPressElement` ignores `_activeButton`: `DragCaptureTests.ARightPressCapturesNoMove`, `ChordClickTests.ThePressTargetFollowsTheLeftPress`, `SliderDragTests.ARightPressThenAMoveDoesNotDrag`.
5. M05 the press target of a `PointerUp` ignores `_releasePending`: 0 failed. Equivalent mutant. A routed release that does not end the press needs another button down. With no chord, that is only a release of a button that is not down. `IsClick` then fails on `e.Button`, and no other check reads a release. Only the internal `CurrentPressTarget` shows the difference.
6. M06 no write-back of the press target after the walk step: 0 failed. Equivalent mutant. Each entry writes the press target before its walk, and the `finally` block writes the old value back. Between the walk step and the next entry, only `FinishRelease` and the Tab step run, and neither one reads the value or calls a handler.
7. M07 no write-back in the `finally` block: `ChordClickTests.ThePressTargetFollowsTheLeftPress`.
8. M08 an entry with a caller target gets `LeftPressElement`: 0 failed (finding CR-5).

The capture branch (`RoutePointer`):

9. M09 the branch also for a `PointerDown`: `SliderDragTests.ASecondButtonEndsTheDrag`.
10. M10 the branch with no `CapturesMoves` check: `SliderDisabledTests.ADisabledSliderCapturesNoMove`.
11. M11 the branch with no `IsInRootTree` check: `DragCaptureTests.ARemovedSliderEndsTheCapture`.
12. M12 the branch before `SetHover(hit)`: `DragCaptureTests.ADragPastTheRightEndGivesTheMaximum`, `ADragPastTheLeftEndGivesTheMinimum`, `ADragAboveOrBelowTheSliderReadsTheXOnly`, `ACapturedMoveKeepsHoverAndTheGateOnTheHit`, `ACapturedMoveWalksThePathOfTheSlider`.
13. M13 the branch reads `_activeElement` in place of `LeftPressElement`: `DragCaptureTests.ARightPressCapturesNoMove`.

`WidgetInput`:

14. M14 `IsClick` reads `Active` again: 7 of the 8 `ChordClickTests` (each one except `ThePressTargetFollowsTheLeftPress`).
15. M15 `IsDragStart` with no `Left` check: 0 failed. Equivalent mutant. The press target of a `PointerDown` is `LeftPressElement`, so it is `null` for each press that is not a left press with no chord. The `Left` check is defence in depth.
16. M16 `IsDragStart` with no press target check: 0 failed (finding CR-3).
17. M17 `IsDragMove` with no press target check: `SliderDragTests.AMoveWithNoPressDoesNothingAndWalksOn`, `AMoveAfterTheReleaseDoesNothing`, `APressElsewhereThenAMoveOverTheSliderDoesNothing`, `ARightPressThenAMoveDoesNotDrag`, `ASecondButtonEndsTheDrag`.
18. M18 `IsDragStart` with no `Disabled` check: `SliderDisabledTests.ADisabledSliderIgnoresAPress`.
19. M19 `IsDragMove` with no `Disabled` check: `SliderDisabledTests.ADisableDuringADragStopsTheDrag`.
20. M20 `IsArrowKey` with no `Disabled` check: `SliderDisabledTests.ADisabledSliderIgnoresTheArrows`.
21. M21 `IsArrowKey` with no target check: `SliderKeyTests.AKeyFromAFocusableChildDoesNothing`.
22. M22 `IsArrowKey` blocks a repeat: `SliderKeyTests.ARepeatMovesTheValue`.
23. M23 `IsArrowKey` blocks `Shift`: `SliderKeyTests.AModifierBlocksTheArrow`.
24. M24 `Control` removed from the mask: `SliderKeyTests.AModifierBlocksTheArrow`.
25. M25 `Alt` removed from the mask: `SliderKeyTests.AModifierBlocksTheArrow`.
26. M26 `Super` removed from the mask: `SliderKeyTests.AModifierBlocksTheArrow`.
27. M27 the two key constants swapped: `SliderKeyTests.ARightArrowMovesTheValueByOneStep` (it pins 262 and 263).
28. M28 `IsArrowKey` with no `KeyDown` type check: `SliderKeyTests.OtherKeysDoNothing`.

`Slider`:

29. M29 no `StopPropagation` in `HandlePointerDown`: `SliderPressTests.APressStopsTheWalk`.
30. M30 no `StopPropagation` in `HandlePointerMove`: `SliderDragTests.ADragMoveStopsTheWalk`, `AMoveToTheSameValueCallsNothing`, `SliderDisabledTests.ADisableDuringADragStopsTheDrag`.
31. M31 no `StopPropagation` in `HandleKeyDown`: `SliderKeyTests.AModifierBlocksTheArrow`, `TheArrowStopsAtTheEndOfTheRange`.
32. M32 `UserSet` calls `ValueChanged` with no change: `SliderKeyTests.TheArrowStopsAtTheEndOfTheRange`, `SliderStepTests.APressAndADragRoundToTheStep`, `SliderPressTests.APressAtTheSameValueCallsNothing`, `SliderDragTests.AMoveToTheSameValueCallsNothing`.
33. M33 `ValueChanged` before `Apply`: `SliderPressTests.APressMovesTheValueToThePointer`, `SliderDragTests.TheDelegateReadsTheNewValue`.
34. M34 a code set of `Value` calls `ValueChanged`: `SliderKeyTests.TheArrowStopsAtTheEndOfTheRange`, `SliderStepTests.SetRangeClampsAndRoundsAndCallsNothing`, `DragCaptureTests.ACapturedMoveKeepsHoverAndTheGateOnTheHit`, `SliderPressTests.ACodeSetClampsAndCallsNothing`, `SliderDisabledTests.ACodeSetWorksOnADisabledSlider`.
35. M35 `SetRange` calls `ValueChanged`: `SliderStepTests.SetRangeClampsAndRoundsAndCallsNothing`, `SliderDisabledTests.ACodeSetWorksOnADisabledSlider`.
36. M36 `Normalize` with no NaN rule: `SliderStepTests.ANaNValueGivesTheMinimum`.
37. M37 `Normalize` with no second clamp: `SliderStepTests.ARangeThatIsNoMultipleOfTheStep`.
38. M38 `MidpointRounding.ToEven`: `SliderStepTests.ASetRoundsToTheNearestStep`, `ARangeThatIsNoMultipleOfTheStep`.
39. M39 the grid from 0 in place of `Minimum`: `SliderStepTests.TheGridStartsAtTheMinimum`.
40. M40 `ValueAtPointer` with no half-thumb offset: 17 tests in `SliderPressTests`, `SliderDragTests`, `DragCaptureTests`, `SliderStepTests`, `SliderDisabledTests`, and `SliderDrawTests` (for example `SliderPressTests.APressMovesTheValueToThePointer` and `SliderDrawTests.TheThumbFitsTheContentBox`).
41. M41 `ValueAtPointer` with no clamp of the ratio: 0 failed. Equivalent mutant. Each result goes through `Apply`, and `Normalize` clamps to the range before it rounds. A ratio below 0 or above 1 thus gives the same value as the clamped ratio.
42. M42 `ThumbWidth` always the content height: `SliderDrawTests.ASmallSliderDrawsASafeThumb`.
43. M43 `Ratio` with no zero-range guard: `SliderDrawTests.ASmallSliderDrawsASafeThumb`.
44. M44 `PaintContent` with no alpha check: `SliderDrawTests.TheOpacityScalesTheThumb`.
45. M45 `PaintContent` with the border box in place of the content box: `SliderDrawTests.TheThumbFitsTheContentBox`.
46. M46 `SetRange` writes before the check: `SliderStepTests.ABadRangeThrowsAndKeepsTheState`.
47. M47 `SetRange` with no `Paint` mark: `SliderStepTests.SetRangeClampsAndRoundsAndCallsNothing`.
48. M48 `CapturesMoves` ignores `Disabled`: `SliderDisabledTests.ADisabledSliderCapturesNoMove`.
49. M49 the key delta is one percent also with a step: `SliderKeyTests.ARightArrowMovesTheValueByOneStep`, `ALeftArrowMovesTheValueBack`, `TabThenRightMovesTheValue`, `AModifierBlocksTheArrow`, `ARepeatMovesTheValue`.
50. M50 `IsFocusable` not set: 8 `SliderKeyTests`, `SliderPressTests.APressFocusesTheSlider`, `SliderAllocationTests.TheMeasuredBodyDoesRealWork`.
51. M51 `Apply` marks no `Paint`: `SliderStepTests.SetRangeClampsAndRoundsAndCallsNothing`, `SliderPressTests.ACodeSetClampsAndCallsNothing`.

The allocation gate (`SliderAllocationTests`):

52. M52 a plant at the start of `Slider.HandlePointerDown`: `SliderAllocationTests.TheSliderInputPathAllocatesNothing`.
53. M53 a plant at the start of `Slider.HandleKeyDown`: `SliderAllocationTests.TheSliderInputPathAllocatesNothing`.
54. M54 a plant at the start of `Slider.Apply`: `SliderAllocationTests.TheSliderInputPathAllocatesNothing`.
55. M55 a plant at the start of `Slider.PaintContent`: `SliderAllocationTests.TheSliderInputPathAllocatesNothing`, `UpdateAndDrawAllocateNothingWithSliders`.
56. M56 a plant in the capture branch of `UiContext.RoutePointer`: `SliderAllocationTests.TheSliderInputPathAllocatesNothing`.
57. M57 the measured body with no move past the root (no captured move): `SliderAllocationTests.TheMeasuredBodyDoesRealWork`.

An extra probe:

58. M58 `WidgetInput.ViewportOrigin` reads the rect of the element only, with no ancestor: 0 failed (finding CR-4).

The scenario probes (each one is a test that the copy held for the probe only):

- P1 a left press at 25, then `Display.None` on the slider, then `Update` and a move to the x of 60: the value is 0 (the minimum), `ValueChanged` runs 2 times, and the layout rect of the slider is (0, 0, 0, 0) (finding CR-1).
- P2 a left press at the x 110 (the value 60): the value is 60.000004 (`ToString` also gives `60.000004`). The same map in `double` gives 60 exactly. The press at the x 80 of `TheThumbFitsTheContentBox` gives 30 exactly in `double` (finding CR-2).
- P3 a move with the slider as the caller target during a left drag: the value stays 25. The code is correct, but no suite test holds this rule (finding CR-5).
- P4 a slider inside a wrapper with a padding of 10: a press maps through each ancestor and gives 25. The code is correct, but no suite test holds this rule (finding CR-4).
- P5 the range -3e38 to 3e38: `Ratio(0)` gives 0 (not 0.5), and a press at the center gives 3e38 (not 0) (finding CR-6).

Count: 58 mutations. 7 mutations gave 0 failed: 4 equivalent mutants (M05, M06, M15, M41) and 3 test gaps (M08, M16, M58).

## Findings

### CR-1: A captured drag goes on into a hidden slider and sets the minimum
**Severity:** HIGH
**Rule:** 4. A drag does not get lost (and 2. The value is exact, 5. One change, one call)
**File:** `src/BrutalUi.Core/Tree/UiContext.cs:1656-1665`
**Problem:** The capture branch checks the type, the left press element, `CapturesMoves`, and the roots. It does not check that the element is shown. A hidden element keeps `Active`, because the stale rule checks the roots only. The layout gives a `Display.None` subtree a rect of 0 (`LayoutEngine.Lines.cs`, `ZeroOutLayoutRecursively`). Each captured move then reaches the slider, `ValueAtPointer` finds a travel of 0, the ratio is 0, and the value goes to `Minimum`. `ValueChanged` runs for that change. Probe P1 shows the value 0 and 2 calls.

A common game case gives this: a key or a timer closes the settings panel while the person holds the mouse on the volume slider. Each move until the release then sets the volume to 0. Before the capture, a hidden element never got a move, because the hit test skips it. The capture opens this path, and scroll-view copies it.

```csharp
if (type == UiEventType.PointerMove
    && LeftPressElement is IDragCapture { CapturesMoves: true } and Element capture
    && IsInRootTree(capture))
{
    entry.Header.Target = capture;
    return capture;
}
```

**Fix:** Add the shown rule of the focus code to the branch. `FocusWalk.IsShown` reads the style of the last `Update`, walks the parents one time, and allocates nothing. The move then walks to the hit, as for a removed slider. Add `AHiddenSliderEndsTheCapture` to `DragCaptureTests`: a press at 25, `Display.None` on the slider (and a second case on an ancestor), `Update`, a move, and then the value 25, no new call, and the move target on the hit. Update the remarks of `RoutePointer`, `IDragCapture`, and section 9 item 4.

```csharp
if (type == UiEventType.PointerMove
    && LeftPressElement is IDragCapture { CapturesMoves: true } and Element capture
    && IsInRootTree(capture)
    && FocusWalk.IsShown(capture))
```

**Effort:** Low

### CR-2: The pointer map is 1 ulp off for a pixel center, and the tests hide it with a tolerance
**Severity:** MEDIUM
**Rule:** 3. The pointer maps exactly (and 2. The value is exact, 10. Test quality)
**File:** `src/BrutalUi.Core/Elements/Slider.cs:362-377`
**Problem:** `ValueAtPointer` divides first and multiplies second in `float`. The ratio 0.6f is 0.60000002, and the product with 100 rounds to 60.0000038. Probe P2 reads `60.000004`, also from `ToString()`. A step of 0 is the default, so a game label that shows `Value` with no format shows the error. Phase 4 added a tolerance of 0.001 to the tests (P4-1), so no test guards the exact map. The same map in `double` gives 60 and 30 exactly for the pixel centers of the tests.

```csharp
ratio = Math.Clamp((x - content.X - (thumb / 2f)) / travel, 0f, 1f);
...
return _minimum + (ratio * (_maximum - _minimum));
```

**Fix:** Compute the map in `double` and round one time to `float`. The product of two exact numbers and one divide give the nearest float of the exact result. `double` math allocates nothing. Then remove the tolerance of the mapped values in `SliderDragTests`, `DragCaptureTests`, `SliderDisabledTests`, and `TheThumbFitsTheContentBox`, and assert each value exactly.

```csharp
if (!(travel > 0f))
{
    return _minimum;
}

double offset = Math.Clamp((double)x - content.X - (thumb / 2d), 0d, travel);
return (float)(_minimum + (offset * ((double)_maximum - _minimum) / travel));
```

**Effort:** Low

### CR-3: No test guards the press target check of `IsDragStart`
**Severity:** MEDIUM
**Rule:** 1. A press belongs to one button (and 10. Test quality)
**File:** `tests/BrutalUi.Core.Tests/Elements/SliderPressTests.cs`
**Problem:** Mutation M16 removes `ReferenceEquals(UiContext.CurrentPressTarget, widget)` from `IsDragStart`, and each test passes. Under the mutant, three press cases move the value: a left press on the slider after a right press on the slider (a chord press), a left press on a pickable child of the slider, and a left `PointerDown` with the slider as the caller target. The class remarks of `Slider` and section 5 state "a chord press and a press on a child give no move". `SliderPressTests.ARightButtonPressDoesNothing` fails on the `Left` check, not on the press target. scroll-view copies `IsDragStart`, so the gap repeats there.
**Fix:** Add `AChordPressOrAPressOnAChildDoesNotMove` to `SliderPressTests`: a right press and then a left press on the slider give no move and no call. A left press on a pickable child of the slider gives no move. A left press with the slider as the caller target gives no move. Each case also asserts that the walk goes on to the root.
**Effort:** Low

### CR-4: No test guards the ancestor walk of `ViewportOrigin`
**Severity:** MEDIUM
**Rule:** 3. The pointer maps exactly (and 10. Test quality)
**File:** `tests/BrutalUi.Core.Tests/Elements/SliderPressTests.cs`
**Problem:** Mutation M58 makes `WidgetInput.ViewportOrigin` read the rect of the slider only, and each test passes. Each test scene puts the slider directly under a root at (0, 0), so the ancestors add nothing. scroll-view changes this exact function to add its scroll offset (Conflicts and Gaps 6). With no test, a wrong origin inside a container or a scroll view passes the suite. Probe P4 shows that the current code maps correctly through a wrapper with a padding of 10.
**Fix:** Add `APressMapsThroughEachAncestor` to `SliderPressTests`: put the slider in a wrapper with a padding (and an offset from the root), press at the x of the value 25 in viewport pixels, and assert 25. Also assert the thumb rect of the draw in the same scene, so the hit test, the map, and the draw agree.
**Effort:** Low

### CR-5: No test guards the null press target of an entry with a caller target
**Severity:** MEDIUM
**Rule:** 1. A press belongs to one button (and 10. Test quality)
**File:** `tests/BrutalUi.Core.Tests/Elements/SliderDragTests.cs`
**Problem:** Mutation M08 removes `!routed` from the press target step of `DrainEvents`, and each test passes. Under the mutant, a `PointerMove` or a `PointerDown` with a caller target gets the left press element. A synthetic move with the slider as the target during a drag then drags the value. The `CurrentPressTarget` remarks, section 5, and the changelog state "an entry with a target from the caller gets null". Only the release case has a test (`AReleaseWithACallerTargetGivesNoClick`), and the `_releasePending` guard already gives `null` there, so that test cannot see the `!routed` rule. Probe P3 shows that the current code is correct.
**Fix:** Add `ACallerTargetMoveDuringADragDoesNotDrag` to `SliderDragTests`: a left press at 25 with no target, then a `PointerMove` at the x of 60 with the slider as the caller target, then `Update`. Assert the value 25 and 1 call.
**Effort:** Low

### CR-6: A range wider than the float range passes the check and breaks the map, the draw, and the keys
**Severity:** LOW
**Rule:** 2. The value is exact
**File:** `src/BrutalUi.Core/Elements/Slider.cs:384-405`
**Problem:** `ValidateRange` checks each bound and the order, but not the width. For the range -3e38 to 3e38, `_maximum - _minimum` is infinity. Probe P5 shows `Ratio(0)` as 0 (the thumb at the start for the value 0) and a press at the center as 3e38 (the maximum). The key delta is infinity, and the grid divide overflows too. No game needs such a range, but the constructor accepts it and the slider then misbehaves with no throw.
**Fix:** Add one check after the order check, and add the case to `ABadRangeThrowsAndKeepsTheState`.

```csharp
if (!float.IsFinite(maximum - minimum))
{
    throw new ArgumentOutOfRangeException(nameof(maximum), maximum, "The width of the range must be finite.");
}
```

**Effort:** Low

### CR-7: The constructor throws with parameter names that the constructor does not have
**Severity:** LOW
**Rule:** 11. `AGENTS.md` and the docs (API clarity)
**File:** `src/BrutalUi.Core/Elements/Slider.cs:153-164`
**Problem:** The constructor and `SetRange` share `ValidateRange`, so `new Slider(in info)` throws `ArgumentOutOfRangeException` with `ParamName` `minimum`, `maximum`, or `step` (P6-1). The constructor has one parameter, `info`. The .NET rule for `ParamName` (CA2208) asks for the name of a real parameter. A developer who reads the exception looks for a parameter that does not exist. `SliderStepTests.ABadRangeThrowsAndKeepsTheState` pins the current names.
**Fix:** Give `ValidateRange` the parameter name to use. The constructor passes `nameof(info)`, and the message names the field (`CreateInfo.Minimum`, `CreateInfo.Maximum`, or `CreateInfo.Step`). `SetRange` keeps its own names. Update the constructor cases of the test.
**Effort:** Low

### CR-8: The remarks call the thumb square, but a narrow slider gets a tall thumb and a fixed value
**Severity:** LOW
**Rule:** 11. `AGENTS.md` and the docs
**File:** `src/BrutalUi.Core/Elements/Slider.cs:17-21`
**Problem:** The class remarks say "the thumb is square, and its width is the smaller of the height and the width". For a content box narrower than tall, the thumb is the content width by the content height, so it is not square (P3-2). The travel is then 0, so each press and each drag gives `Minimum`. The remarks do not state that result.
**Fix:** Write: "The thumb is as wide as the smaller of the content height and the content width, and as tall as the content box. It is square when the content box is at least as wide as tall. A slider narrower than tall has no travel, so a press and a drag give `Minimum`."
**Effort:** Low

### CR-9: `IsClick` does not read the event type
**Severity:** LOW
**Rule:** 9. The widget pattern and the drag rule (and 1. A press belongs to one button)
**File:** `src/BrutalUi.Core/Elements/WidgetInput.cs:67-75`
**Problem:** `IsDragStart`, `IsDragMove`, and `IsArrowKey` read the event type, so a copy of the handler in another slot does nothing (P2-5, P6-6). `IsClick` does not. The drain sets the press target for a press and a move too, so a copy of a click handler in `OnPointerDown` or `OnPointerMove` clicks during the press. The base code had the same behaviour with `Active`. The next widgets (text-field, scroll-view) copy the shape of `WidgetInput`, and one check that differs invites the mistake.
**Fix:** Add the type to the check, and add a case to `ChordClickTests` or `ButtonClickTests` with the click handler copied into `OnPointerDown`. Run `ButtonClickTests`, `ToggleTests`, and `WidgetAllocationTests` after the change.

```csharp
return e.Header.Type == UiEventType.PointerUp
    && e.Button == PointerButton.Left
    && ReferenceEquals(UiContext.CurrentPressTarget, widget);
```

**Effort:** Low

### CR-10: The doc comments of `WidgetInput` hold phase history and one false sentence
**Severity:** LOW
**Rule:** 11. `AGENTS.md` and the docs
**File:** `src/BrutalUi.Core/Elements/WidgetInput.cs:21-28, 200-204`
**Problem:** The class remarks say "The drag checks of the later phases serve the same widgets" and "The drag checks of the later widgets read the same press target". `IsDragMove` exists now, and a shipped doc comment does not name a phase. The remarks of `IsArrowKey` say "The check does not read `PseudoStates.Disabled`, and a disabled widget gets `false`". The first part is false: the check reads `Disabled` first.
**Fix:** Write "`IsDragStart` and `IsDragMove` serve each widget with a drag rule" and "The drag checks read the same press target". In `IsArrowKey`, write "A widget that holds `Disabled` gets `false` (Decision 11 of slider)".
**Effort:** Low

### CR-11: The `ViewportOrigin` comment does not tell scroll-view the rule for the scroll offset
**Severity:** LOW
**Rule:** 9. The widget pattern and the drag rule (and 3. The pointer maps exactly)
**File:** `src/BrutalUi.Core/Elements/WidgetInput.cs:126-149`
**Problem:** The remark says "A scroll view adds its scroll offset here". It does not say which offset or which sign. A scroll view moves the content of its children, not its own box. The origin of an element must thus subtract the offset of each ancestor scroll view, and not the offset of the element itself, the same rule that scroll-view adds to `HitWalk` and `PaintWalk`. `PaintContent` gets its border box from the paint walk, but `ValueAtPointer` gets it from `ViewportOrigin`. A wrong rule here moves the press away from the drawn thumb. CR-4 shows that no test guards the function.
**Fix:** Write: "This is the origin rule of the hit walk and of the paint walk. When scroll-view adds a scroll offset to those walks, subtract here the scroll offset of each ancestor scroll view, not the offset of the element itself. Keep the three rules the same, and add a test with a slider inside a scrolled view." Copy the sentence to section 5 ("The drag rule") of the technical design.
**Effort:** Low

### CR-12: The ring-limit remark of `IDragCapture` names the wrong end of the stuck capture
**Severity:** LOW
**Rule:** 4. A drag does not get lost (and 11. The docs)
**File:** `src/BrutalUi.Core/Input/IDragCapture.cs:30-34`
**Problem:** The remark says "the capture goes on with no button down until the next press and release". The next press makes a chord (`PressButton` sets `_activeChord` while an element holds `Active`), so the capture ends at that press, not at the release. The remark also does not say what a person sees: each hover move drags the value until the next press.
**Fix:** Write: "A dropped `PointerUp` keeps `Active` and the button bit, so the capture goes on with no button down. Each move then drags the element. The next press makes a chord and ends the capture, and the release after it clears `Active`."
**Effort:** Low

### CR-13: Section 5 of the technical design keeps the old click sentence and the square thumb
**Severity:** LOW
**Rule:** 11. `AGENTS.md` and the docs
**File:** `docs/technical-design.md:320, 327`
**Problem:** The click rule starts with "a click is a left release while the widget holds `Active`". The next sentences replace that rule with the press target. A reader who stops after the first sentence gets the rule of the base code. The slider paragraph says "the thumb is a square" (see CR-8).
**Fix:** Write "The click rule: a click is a left release that ends the press, while the widget is the press target (`UiContext.CurrentPressTarget`)." Then keep the chord rule. In the slider paragraph, use the thumb sentence of CR-8.
**Effort:** Low

### CR-14: The class remarks of `SliderDragTests` say that the capture comes later
**Severity:** LOW
**Rule:** 10. Test quality (and 11. The docs)
**File:** `tests/BrutalUi.Core.Tests/Elements/SliderDragTests.cs:11-15`
**Problem:** The remarks say "Each move goes over the slider, because the capture comes in a later phase". Phase 5 added the capture, so the sentence is stale (open item of phase 5).
**Fix:** Write "Each move goes over the slider. `DragCaptureTests` holds the moves outside the slider."
**Effort:** Low

### CR-15: `context.md` holds three notes about a tool hook that do not belong to the feature record
**Severity:** LOW
**Rule:** 11. `AGENTS.md` and the docs
**File:** `docs/features/interactive-elements/slider/context.md:336, 370, 401`
**Problem:** Phases 6, 7, and 8 each add a note about a style reminder in the tool results. The notes record the agent environment, not a decision, a risk, or a result of the slider. They also state that the reminder "did not come from the user". The reminder is a hook of the local tool harness, and its rule is the ASD-STE100 rule of `AGENTS.md`. The feature record cannot prove the source either way. Three copies of one note make the phase results harder to read.
**Fix:** Remove the three notes. When a person wants a record, add one line under "Session Notes": "A tool hook reminds the agent of the ASD-STE100 reply style. The briefs set the reply style."
**Effort:** Low

## What's Already Good

- The chord rule is correct in each order that the plan names. `PressButton` reads the other buttons before it sets the bit of the press, and each press while an element holds `Active` sets the chord flag. M01 to M04 and M14 each fail tests.
- The press target follows the shape of `t_measurer`. The drain saves the old value before `_draining` goes on, writes it before each walk, writes it back after each walk step and in the `finally` block. A throw and a nested drain of another context leave no value (M07 fails a test, and M06 is equivalent).
- The capture is opt-in and explicit. No other element changes its route, and each pointer, focus, and widget test passes with no edit. The root check of P5-1 closes the removed-slider case (M11 fails a test).
- One `Normalize` serves each path. M36 to M39 each fail a test. `Apply` compares, writes, and marks `Paint`, and `UserSet` calls `ValueChanged` after the change only (M32 to M35).
- The disabled rule lives in `WidgetInput` and in `CapturesMoves` only, and each check has a test (M18 to M20, M48).
- The allocation gate catches each plant: the press, the key, `Apply`, `PaintContent`, and the capture branch (M52 to M56). `TheMeasuredBodyDoesRealWork` catches a body with no captured move (M57).
- `Slider.cs` holds three cached `static readonly` handlers, no lambda, no closure, and no boxing. The interface call on a class allocates nothing.
- The judgement of the open risks that are not findings:
  - Risk 1 (P1-1): a second press of the same button after a dropped release is a chord. The result is no click, the safe side. It is the ring limit of `Active`, and `Update` each frame keeps the ring below 256 entries.
  - Risk 2 (P1-2): the stale `_activeButton` and `_activeChord` never reach a walk. `LeftPressElement` reads `_activeElement` first, `PressButton` writes both fields each time it sets `Active`, and the stale rule clears `_activeElement`.
  - Risk 4: `Active` keeps its life by Decision 13, and the person accepted it. `Slider:active` during a right press follows the documented pseudo-state, and the slider remarks state that a right press sets `Active` and gives no drag. A game handler that reads `Active` reads a documented state, not a click rule.
  - Risk 5 (P3-1): the track rect of size 0 comes from `PaintWalk`, which is out of scope, and the renderer skips it. P3-2 becomes the doc finding CR-8.
  - Risk 7 (P5-1): the root check walks the parents one time and calls `List<Element>.IndexOf` on the roots. It allocates nothing (the M56 plant proves that the gate measures the branch). The walk step repeats the same check, so a captured move costs two short parent walks.
  - Risk 8 (P5-5): a captured move walks the path of the slider by the choice of Q2. `IDragCapture`, `RoutePointer`, and `DrainEvents` state it, and `ACapturedMoveWalksThePathOfTheSlider` pins it.
  - Risk 10 (P6-2): a new range moves the thumb, so the `Paint` mark is correct. `SetRangeClampsAndRoundsAndCallsNothing` pins it (M47).
  - Risk 11: a dropped release keeps the capture. A move carries no button mask, a dropped `PointerUp` also leaves its bit in the mask, and `PointerEvent.cs` is out of scope, so the context cannot see the lost release. It is the ring limit of `Active`. Only the remark is wrong (CR-12).
  - Risk 13: the gate measures each path of rule 7. The release of a step has no walk, but its drain part runs under the counter: the press target step for a `PointerUp`, `ReleaseButton`, and `FinishRelease`. The walk of a release is generic `EventWalk` code, and `WidgetAllocationTests` measures it with `IsClick` on the press target. The slider has no `PointerUp` handler.
- The tests are strong. Each class drives the real drain with no caller target, and each demo test matches its demo line in `context.md`.

## Recommended Fix Order

| Order | Finding | Severity | Effort | Fix group (one file) |
|-------|---------|----------|--------|----------------------|
| 1 | CR-1 | HIGH | Low | G1 `UiContext.cs` (and the new test in `DragCaptureTests`) |
| 2 | CR-3, CR-4 | MEDIUM | Low | G2 `SliderPressTests.cs` |
| 3 | CR-5, CR-14 | MEDIUM, LOW | Low | G3 `SliderDragTests.cs` |
| 4 | CR-2 | MEDIUM | Low | G4 `Slider.cs` (map) and the tolerance edits of the tests |
| 5 | CR-9, CR-10, CR-11 | LOW | Low | G5 `WidgetInput.cs` |
| 6 | CR-6, CR-7, CR-8 | LOW | Low | G6 `Slider.cs` (range check, names, remarks) |
| 7 | CR-12 | LOW | Low | G7 `IDragCapture.cs` |
| 8 | CR-13 | LOW | Low | G8 `docs/technical-design.md` |
| 9 | CR-15 | LOW | Low | G9 `context.md` |

Do G4 before G2 and G3 when they run in one session, because CR-2 removes the tolerance that the new tests then use. G6 touches the same file as G4, so run G4 first.

### Follow-up questions (not findings)

- The routing gate during a captured drag (risk 15, Conflicts and Gaps 5): `PointerOverUi` reads `false` while a person drags a slider over the game world. A person or the framework epic decides a "pointer captured by UI" rule.
- A capture handler on an ancestor that stops the press does not stop the drag. The press target is still the slider, so the later moves drag it. The click has the same rule today (a stopped press still gives a click at the release). A person decides whether a drag needs the walk of the press to reach the widget.

## Progress

- [x] CR-1: A captured drag goes on into a hidden slider and sets the minimum
- [x] CR-2: The pointer map is 1 ulp off for a pixel center, and the tests hide it with a tolerance
- [x] CR-3: No test guards the press target check of `IsDragStart`
- [x] CR-4: No test guards the ancestor walk of `ViewportOrigin`
- [x] CR-5: No test guards the null press target of an entry with a caller target
- [x] CR-6: A range wider than the float range passes the check and breaks the map, the draw, and the keys
- [x] CR-7: The constructor throws with parameter names that the constructor does not have
- [x] CR-8: The remarks call the thumb square, but a narrow slider gets a tall thumb and a fixed value
- [x] CR-9: `IsClick` does not read the event type
- [x] CR-10: The doc comments of `WidgetInput` hold phase history and one false sentence
- [x] CR-11: The `ViewportOrigin` comment does not tell scroll-view the rule for the scroll offset
- [x] CR-12: The ring-limit remark of `IDragCapture` names the wrong end of the stuck capture
- [x] CR-13: Section 5 of the technical design keeps the old click sentence and the square thumb
- [x] CR-14: The class remarks of `SliderDragTests` say that the capture comes later
- [x] CR-15: `context.md` holds three notes about a tool hook that do not belong to the feature record
