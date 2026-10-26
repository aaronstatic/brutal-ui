# interactive-elements/button-and-toggle - Code Review

**Reviewed:** 2026-10-26
**Reviewer:** Agent (cross-phase review of phases 1 to 6)
**Rules Applied:** 1. A click is exact, 2. A key is exact, 3. One click, one call, 4. Disabled means no effect, 5. The release order, 6. Zero allocation after warm-up, 7. `Label` does not change, 8. The widget pattern, 9. Test quality, 10. `AGENTS.md` and the docs. The Vercel React rules do not apply to C#.
**Status:** Resolved - 13 findings, 13 checked (12 fixed, CR-1 deferred to a person with its reason, and its chord limit stated in the docs). The verifier of the fixes gave PASS (`review-verify.md`): 2332 tests, 0 failed, 2 skipped, and `scripts/test-linux.sh` PASS.

---

## Summary

| Severity | Count | Findings |
|----------|-------|----------|
| HIGH | 1 | CR-1 |
| MEDIUM | 2 | CR-2, CR-3 |
| LOW | 10 | CR-4 to CR-13 |
| **Total** | **13** | |

**Overview.** The feature is in good shape. The release order change is small, and the tests kill each mutation of it. The `Label` refactor is a pure move: the public API of `Label` is the same, and no existing test file has a diff. The widgets allocate nothing after warm-up, and each planted allocation fails a gate. The disabled rule lives in `WidgetInput` only.

The review found one false click. A pointer chord (two buttons down at the same time) can click a button after a left press on another element (CR-1). A probe test confirms two input orders. The fix changes the click rule, so a person must decide it. The review also confirms OQ2: a font change does not measure an auto-size `Button` again (CR-2). One rule has no test: a toggle click stops the release walk (CR-3). The other findings are small test gaps and doc drift.

**Follow-up question (no finding).** `Disabled` blocks a widget only when the widget holds the bit. A button inside a disabled panel still clicks. This is the same rule as keyboard-focus (`AcceptsFocus` reads the own bit). A person decides if a later feature makes `:disabled` on an ancestor block the widgets.

**Baseline (R.3).** `dotnet build brutal-ui.sln`: `0 Warning(s)`, `0 Error(s)`. `dotnet test --solution brutal-ui.sln`: total 2332, failed 0, succeeded 2330, skipped 2 (the two GLFW window tests, because the machine has no display). `BrutalUi.Core.Tests`: 1500 tests, 0 failed. `git diff main --diff-filter=M --name-only -- tests/` gives no line.

**Probe method (R.4).** The review copied the repository with `rsync` to the scratch folder `cr-button-and-toggle/repo`, and saved each original file in `cr-button-and-toggle/orig`. The copy of `Directory.Build.props` appends `$(Mut)` to `DefineConstants`. Each mutation sits behind its own symbol (`#if Mnn`). Each run builds `BrutalUi.Core.Tests` with `-p:Mut=Mnn -p:TreatWarningsAsErrors=false` and runs the test executable. The base copy gave 1500 tests and 0 failed. The symbol `M18` is not used. `M52` is a scratch-only class of six behaviour tests for the open risks. Two runs combine it with one mutation (`M16;M52` and `M35;M52`).

**Probe list.** 50 mutations, 1 behaviour probe class, and 2 combined runs (53 runs).

Release order (`UiContext.cs`):

1. M01, no `FinishRelease` after the walk step (the `finally` call stays): 3 failed. `ClickFocusTests.APressOnAFocusableElementFocusesIt`, `ClickFocusTests.APressOnAFocusableChildFocusesTheChild`, `WidgetAllocationTests.TheMeasuredBodyDoesRealWork`. No test of `ReleaseOrderTests` fails, because each of its tests reads the state after the drain. Tests of other classes kill the mutation, so it is not a finding.
2. M02, no `FinishRelease` in the `finally` block: 1 failed. `ReleaseOrderTests.AThrowingReleaseHandlerStillClearsActive`.
3. M03, `ReleaseButton` clears `Active` before the walk (the old order): 12 failed. `ReleaseOrderTests.AReleaseHandlerOfThePressedElementReadsActive`, `ReleaseOrderTests.AReleaseOverAnotherElementSeesActiveOnThePressedElement`, `ButtonClickTests.APressAndAReleaseClickTheButtonOnce`, `ButtonClickTests.AClickStopsTheReleaseWalk`, `ButtonClickTests.AButtonWithNoHandlerStopsTheWalkAndDoesNotThrow`, `ButtonClickTests.ARightButtonDoesNotClick`, `ToggleTests.AClickFlipsTheToggle`, `ToggleTests.ASecondClickClearsChecked`, `ToggleTests.AReleaseElsewhereOrARightButtonDoesNotFlip`, `ButtonTextTests.AClickOnTheTextClicksTheButton`, `WidgetDisabledTests.AnEnableAgainClicks`, `WidgetAllocationTests.TheMeasuredBodyDoesRealWork`.
4. M04, `_releasePending` never clears: 10 failed. `ReleaseOrderTests.TheDrainClearsActiveAfterTheReleaseWalk`, `ReleaseOrderTests.AThrowingReleaseHandlerStillClearsActive`, `ClickFocusTests.APressOnAFocusableElementFocusesIt`, `ClickFocusTests.APressOnAFocusableChildFocusesTheChild`, `ClickFocusTests.APressOnNoFocusableElementClearsTheFocus`, `ButtonClickTests.ARightButtonDoesNotClick`, `ToggleTests.ASecondClickClearsChecked`, `ToggleTests.AReleaseElsewhereOrARightButtonDoesNotFlip`, `WidgetDisabledTests.AnEnableAgainClicks`, `WidgetAllocationTests.TheMeasuredBodyDoesRealWork`.
5. M05, `FinishRelease` before the walk step: 12 failed, the same 12 tests as M03.

`WidgetInput.IsClick`:

6. M06, no `Left` check: 2 failed. `ButtonClickTests.ARightButtonDoesNotClick`, `ToggleTests.AReleaseElsewhereOrARightButtonDoesNotFlip`.
7. M07, no `Active` check: 2 failed. `ButtonClickTests.AReleaseAfterAPressElsewhereDoesNotClick`, `ToggleTests.AReleaseElsewhereOrARightButtonDoesNotFlip`.
8. M08, no `Disabled` check: 4 failed. `WidgetDisabledTests.ADisabledButtonDoesNotClick`, `WidgetDisabledTests.ADisableBetweenThePressAndTheReleaseGivesNoClick`, `WidgetDisabledTests.AnEnableAgainClicks`, `WidgetDisabledTests.ADisabledToggleDoesNotFlip`.

`WidgetInput.IsKeyActivation`:

9. M09, no target check (a key that bubbles from a child): 1 failed. `ButtonKeyTests.AKeyFromAFocusableChildDoesNotClick`.
10. M10, no repeat check: 2 failed. `ButtonKeyTests.ARepeatDoesNotClick`, `ToggleTests.ARepeatSpaceDoesNothing`.
11. M11, no `Control` in the blocked set: 1 failed. `ButtonKeyTests.AModifierStopsTheActivation`.
12. M12, no `Alt` in the blocked set: 1 failed. `ButtonKeyTests.AModifierStopsTheActivation`.
13. M13, no `Super` in the blocked set: 1 failed. `ButtonKeyTests.AModifierStopsTheActivation`.
14. M14, `Shift` in the blocked set: 1 failed. `ButtonKeyTests.AModifierStopsTheActivation`.
15. M15, no `Disabled` check: 2 failed. `WidgetDisabledTests.ADisabledButtonIgnoresEnterAndSpace`, `WidgetDisabledTests.ADisabledToggleDoesNotFlip`.
16. M16, no `KeyDown` type check: 0 failed (finding CR-4). This is not an equivalent mutant. The probe test `ACopiedKeyHandlerOnKeyUpDoesNotClick` copies `button.OnKeyDown` into `button.OnKeyUp`. With M16, an Enter key up then clicks.
17. M17, `EnterKey` set to 335: 2 failed. `ButtonKeyTests.AnotherKeyDoesNotClickAndWalksOn`, `WidgetAllocationTests.TheMeasuredBodyDoesRealWork`.
18. M51 (extra), `SpaceKey` set to 33: 1 failed. `WidgetAllocationTests.TheMeasuredBodyDoesRealWork` only (finding CR-5).

`Button`:

19. M19, no `StopPropagation` in `HandlePointerUp`: 3 failed. `ButtonClickTests.AClickStopsTheReleaseWalk`, `ButtonClickTests.AButtonWithNoHandlerStopsTheWalkAndDoesNotThrow`, `ButtonClickTests.ARightButtonDoesNotClick`.
20. M20, no `StopPropagation` in `HandleKeyDown`: 3 failed. `ButtonKeyTests.AKeyActivationStopsTheWalk`, `ButtonKeyTests.ARepeatDoesNotClick`, `ButtonKeyTests.AModifierStopsTheActivation`.
21. M21, `Clicked` called two times: 12 failed. `ButtonClickTests.APressAndAReleaseClickTheButtonOnce`, `ButtonClickTests.AClickStopsTheReleaseWalk`, `ButtonClickTests.ARightButtonDoesNotClick`, `ButtonKeyTests.AFocusedButtonClicksOnEnter`, `ButtonKeyTests.AFocusedButtonClicksOnSpace`, `ButtonKeyTests.ARepeatDoesNotClick`, `ButtonKeyTests.AModifierStopsTheActivation`, `ButtonKeyTests.AKeyActivationStopsTheWalk`, `ButtonKeyTests.TabThenEnterClicks`, `ButtonTextTests.AClickOnTheTextClicksTheButton`, `WidgetDisabledTests.AnEnableAgainClicks`, `WidgetAllocationTests.TheMeasuredBodyDoesRealWork`.
22. M22, `HandleKeyDown` with `ActivationKeys.Enter` only: 3 failed. `ButtonKeyTests.AFocusedButtonClicksOnSpace`, `ButtonKeyTests.AModifierStopsTheActivation`, `ButtonKeyTests.AKeyActivationStopsTheWalk`.
23. M23, `HandleKeyDown` with `ActivationKeys.Space` only: 7 failed. `ButtonKeyTests.AFocusedButtonClicksOnEnter`, `ButtonKeyTests.ARepeatDoesNotClick`, `ButtonKeyTests.AModifierStopsTheActivation`, `ButtonKeyTests.AKeyActivationStopsTheWalk`, `ButtonKeyTests.TabThenEnterClicks`, `WidgetDisabledTests.AnEnableAgainClicks`, `WidgetAllocationTests.TheMeasuredBodyDoesRealWork`.
24. M24, no `IsFocusable = true`: 11 failed. `ButtonClickTests.APressFocusesTheButton`, eight `ButtonKeyTests` (each test except `AKeyFromAFocusableChildDoesNotClick`), `ButtonTextTests.AClickOnTheTextClicksTheButton`, `WidgetAllocationTests.TheMeasuredBodyDoesRealWork`.

`Toggle`:

25. M31, `Flip` calls `CheckedChanged` before the state change: 2 failed. `ToggleTests.AClickFlipsTheToggle`, `ToggleTests.ASecondClickClearsChecked`.
26. M32, the key mask `Enter | Space`: 1 failed. `ToggleTests.EnterDoesNothingOnAFocusedToggle`.
27. M33, the constructor calls `CheckedChanged` for a `true` start value: 0 failed. Equivalent mutant: `CheckedChanged` is always `null` inside the constructor. An object initializer and each later set run after the constructor, and the class is `sealed`.
28. M34, the `Checked` setter writes the bit only for `true` (a clear does nothing): 6 failed. `ToggleTests.ASecondClickClearsChecked`, `ToggleTests.SpaceFlipsAFocusedToggle`, `ToggleTests.ACodeSetCallsNoCheckedChanged`, `ToggleTests.CheckedReadsTheStateBit`, `WidgetDisabledTests.ACodeSetOfCheckedWorksOnADisabledToggle`, `WidgetAllocationTests.TheMeasuredBodyDoesRealWork`.
29. M35, no `StopPropagation` in `HandlePointerUp`: 0 failed (finding CR-3). The probe test `AToggleClickStopsTheReleaseWalk` passes on the code and fails with M35.
30. M36, no `StopPropagation` in `HandleKeyDown`: 2 failed. `ToggleTests.SpaceFlipsAFocusedToggle`, `ToggleTests.ARepeatSpaceDoesNothing`.

`Utf8Text` and `Label`:

31. M39, `Set(ReadOnlySpan<byte>)` returns `true` for the same text: 3 failed. `ButtonTextTests.SetTextFromTheSameTextMarksNothing`, `LabelTextTests.SetTextFromTheSameSpanMarksNothing`, `LabelTextTests.SetTextFromANullStringGivesAnEmptyText`.
32. M40, `Set(string)` returns `true` for the same text: 5 failed. `ButtonTextTests.SetTextFromTheSameTextMarksNothing`, `ButtonTextTests.SetTextFromAnLStringShowsTheLocalizedText`, `LabelTextTests.SetTextFromTheSameStringMarksNothing`, `LabelTextTests.SetTextFromAStringClearsTheLocalizedText`, `LabelTextTests.SetTextFromANullStringGivesAnEmptyText`.
33. M41, `Set(LString)` returns `true` for the same text: 2 failed. `ButtonTextTests.SetTextFromAnLStringShowsTheLocalizedText`, `LabelTextTests.SetTextFromAnLStringShowsTheLocalizedText`.
34. M42, `Set(string)` returns `false` for a new text: 5 failed. `ButtonTextTests.SetTextFromTheSameTextMarksNothing`, `LabelTextTests.SetTextFromAStringMarksLayoutAndPaint`, `LabelTextTests.SetTextFromANullStringGivesAnEmptyText`, `ElementAllocationTests.SetTextFromAStringAllocatesNothingAfterWarmUp`, `WidgetAllocationTests.TheMeasuredBodyDoesRealWork`.
35. M43, `Refresh` returns `false` for a new locale: 7 failed. `ButtonTextTests.RefreshLocalizedTextReachesAButton`, `LabelTextTests.ShowsTheNewLocaleAfterARefresh`, `LabelTextTests.RefreshShowsTheTextOfANewProvider`, `LabelTextTests.RefreshReachesADeepLabel`, `LabelTextTests.RefreshWorksWithNoMeasurer`, `LabelTextTests.RemoveLocalizationShowsTheTemplate`, `ElementAllocationTests.RefreshLocalizedTextAllocatesNothing`.
36. M44, the localized text wins over a new span set (no `ClearLocalized` in `Set(ReadOnlySpan<byte>)`): 1 failed. `LabelTextTests.SetTextFromASpanClearsTheLocalizedText`.
37. M46, `Label.SetText(ReadOnlySpan<byte>)` marks no `Paint`: 2 failed. `LabelTextTests.SetTextFromASpanMarksLayoutAndPaint`, `LabelTextTests.SetTextFromAPartOfTheTextCopiesTheBytes`.
38. M47, `Label.SetText(string)` marks no `Paint`: 2 failed. `LabelTextTests.SetTextFromAStringMarksLayoutAndPaint`, `LabelTextTests.SetTextFromANullStringGivesAnEmptyText`.

`Button` text:

39. M25, `SetText(ReadOnlySpan<byte>)` marks no `Layout`: 2 failed. `ButtonTextTests.SetTextFromASpanMarksLayoutAndPaint`, `ButtonTextTests.SetTextFromTheSameTextMarksNothing`.
40. M26, `SetText(string)` marks no `Layout`: 2 failed. `ButtonTextTests.SetTextFromTheSameTextMarksNothing`, `WidgetAllocationTests.TheMeasuredBodyDoesRealWork`.
41. M27, `SetText(LString)` marks no `Layout`: 1 failed. `ButtonTextTests.SetTextFromAnLStringShowsTheLocalizedText`.
42. M28, `MeasureButton` gives the size with no wrap: 1 failed. `ButtonTextTests.TheButtonTakesTheSizeOfItsText`.
43. M29, `PaintContent` draws at the border box corner: 2 failed. `ButtonTextTests.AButtonDrawsItsText`, `ButtonTextTests.TheButtonTakesTheSizeOfItsText`.
44. M48, `RefreshSubtree` with no `Button` branch: 1 failed. `ButtonTextTests.RefreshLocalizedTextReachesAButton`.

`WidgetAllocationTests` plants and bodies:

45. M37, a plant at the start of `Toggle.HandleKeyDown`: 1 failed. `WidgetAllocationTests.TheWidgetInputPathAllocatesNothing`.
46. M30, a plant at the start of `Button.HandleKeyDown`: 1 failed. `WidgetAllocationTests.TheWidgetInputPathAllocatesNothing`.
47. M45, a plant at the start of `Utf8Text.Set(string)`: 2 failed. `WidgetAllocationTests.SetTextOnAButtonAllocatesNothing`, `ElementAllocationTests.SetTextFromAStringAllocatesNothingAfterWarmUp`.
48. M38, a plant at the start of `Toggle.Flip`: 1 failed. `WidgetAllocationTests.TheWidgetInputPathAllocatesNothing`.
49. M49, the measured body with no Space step: 1 failed. `WidgetAllocationTests.TheMeasuredBodyDoesRealWork`.
50. M50, the measured body with no press over the toggle: 1 failed. `WidgetAllocationTests.TheMeasuredBodyDoesRealWork`.

The review did not repeat the plant of task 6.2 (`Button.HandlePointerUp`).

Behaviour probe class (M52, scratch only, 6 tests, each asserts the rule of the plan):

51. M52 on the code: 4 failed. `ChordRightPressOnButtonLeftPressElsewhereGivesNoClick` and `ChordLeftPressElsewhereWhileRightHeldGivesNoClick` (CR-1), `ANewFontSizeMeasuresTheButtonAgain` (CR-2), `ACallerTargetReleaseAndTheRealReleaseGiveOneClick` (CR-6). `ACopiedKeyHandlerOnKeyUpDoesNotClick` and `AToggleClickStopsTheReleaseWalk` pass.
52. M16 with M52: 5 failed. The 4 tests of item 51 and `ACopiedKeyHandlerOnKeyUpDoesNotClick`.
53. M35 with M52: 5 failed. The 4 tests of item 51 and `AToggleClickStopsTheReleaseWalk`.

**Cleanup (R.6).** The review restored each file from `orig` and deleted the probe class. `diff -rq --exclude bin --exclude obj` on `src` and on `tests` of the copy and of the repository gave no output (exit 0 for each). `cmp` on `Directory.Build.props` gave exit 0. Then `rm -rf` removed the `cr-button-and-toggle` folder. `git status --short` shows only the work of the feature and this file as new.

## Findings

### CR-1: A pointer chord gives a click after a left press on another element
**Severity:** HIGH
**Rule:** 1. A click is exact
**File:** `src/BrutalUi.Core/Tree/UiContext.cs:1575-1611` (`PressButton`, `ReleaseButton`). The check is `src/BrutalUi.Core/Elements/WidgetInput.cs:50-58` (`IsClick`).
**Problem:** OQ1 of `context.md` is a real false click. `PressButton` sets `Active` on the first press of a gesture only. `Active` stays until the last button goes up. `IsClick` reads only `Left` and `Active`. Thus a left release clicks each widget that holds `Active`, also when the left press was on another element. The probe class confirms two orders on the code:

```text
Order 1: right down over the button, left down over Other, right up, left up over the button -> 1 click
Order 2: left down over the button, right down, left up over Other, left down over Other,
         left up over the button (the right button is still down)                      -> 1 click
```

In each order, the left press that ends with the release was on `Other`. This breaks rule 1 ("a release after a press on another element gives no click"), the remarks of `Button` (lines 13-14), and the click rule of section 5 of the technical design. The same defect flips a `Toggle`. Each later widget that calls `IsClick` copies it. No test covers a chord.

The widget cannot fix this alone. It holds no context reference, and `Active` alone cannot tell which press set it. A fix in `ReleaseButton` alone (clear `Active` before the walk when the release button differs from the press button) fixes order 1 only. In order 2 the left release is not the last release, so `Active` stays on the button through the walk.
**Fix:** The smallest complete fix keeps each public member. A person must decide it, because it changes the click rule of Decision 2 and rule 5 of the widget pattern, and it changes the result of a chord on the pressed button (that chord then gives no click).

```csharp
// UiContext: two private fields.
private PointerButton _activeButton;   // the button of the press that set Active
private bool _activeChord;             // another button went down while Active was held

// PressButton
if (_activeElement is null && hit is not null)
{
    hit.States |= PseudoStates.Active;
    _activeElement = hit;
    _activeButton = button;
    _activeChord = false;
}
else if (_activeElement is not null)
{
    _activeChord = true;
}

// DrainEvents, around the walk of a routed PointerUp: a thread-static click target, as t_measurer.
// It is the stored element only when the release ends a press of one left button.
t_clickTarget = _releasePending && _activeButton == PointerButton.Left && !_activeChord ? _activeElement : null;
// ... DispatchQueued ... then t_clickTarget = null (also in the finally block).

// WidgetInput.IsClick
return e.Button == PointerButton.Left && ReferenceEquals(UiContext.CurrentClickTarget, widget);
```

Alternatives: an internal flag in the release payload in place of the thread-static (a change of `PointerEvent.cs` or of the queued payload, which I2 lists as no-diff), or a public `PointerEvent` member (a public API change). Add the two orders of the probe class to `ButtonClickTests` and `ToggleTests`. Update the `Button` remarks, the `IsClick` doc, and section 5 of the technical design with the chord rule. The fix also closes CR-6, because a caller target skips the routed release.
**Effort:** Medium

### CR-2: A font or font size change does not measure a `Button` again
**Severity:** MEDIUM
**Rule:** 8. The widget pattern (the text rules of `Label`), 9. Test quality
**File:** `src/BrutalUi.Core/Style/StylePass.cs:222-227`
**Problem:** OQ2 of `context.md` holds. The style pass adds `DirtyFlags.Layout` for a new font or font size only for a `Label`:

```csharp
if (sizeTextChanged && element is Label)
{
    bits |= DirtyFlags.Layout;
}
```

An auto-size `Button` then keeps its old size and draws the text at the new size. The probe test `ANewFontSizeMeasuresTheButtonAgain` sets `Inline.FontSize` from 16 to 32 on a `Play` button. The width stays 32 and does not become 64. A `Button:hover { font-size }` rule, an inline change, and a sheet change all hit this path. The text then overflows the box or leaves a gap. No test guards it. The first style pass runs before the first layout, so the start size is correct. Thus each test of the feature passes. The doc of `StyleProperties.cs:20` names `Label` only.
**Fix:** Change the type check and the comment, and add a test in `ButtonTextTests.cs` that copies `LabelTextTests.ANewFontSizeMeasuresTheLabelAgain` (line 178).

```csharp
// A label or a button measures its text with the font and the font size, so a change of either one measures it again.
if (sizeTextChanged && element is Label or Button)
{
    bits |= DirtyFlags.Layout;
}
```

Change the last sentence of `StyleProperties.cs:20` to "also changes the layout of a `Label` and of a `Button`". `TextField` adds a third type later. A follow-up can replace the type list with one internal flag on the element, but that change touches `Element.cs`.
**Effort:** Low

### CR-3: No test guards the stop of a toggle click
**Severity:** MEDIUM
**Rule:** 3. One click, one call, 9. Test quality
**File:** `tests/BrutalUi.Core.Tests/Elements/ToggleTests.cs:27-70`
**Problem:** Mutation M35 removes `e.Header.StopPropagation()` from `Toggle.HandlePointerUp`, and 0 tests fail. `AClickFlipsTheToggle` reads the state and the delegate, but no test of `ToggleTests` reads a bubble handler of an ancestor after a click. The key path has a guard (`SpaceFlipsAFocusedToggle` reads `rootDowns == 0`). The pointer path has none. A later edit can drop the stop, and a panel handler then gets each flip as a plain release.
**Fix:** Add a root bubble handler and a root capture handler to `AClickFlipsTheToggle`, or add one test:

```csharp
int rootUps = 0;
tree.Root.OnPointerUp = (ref PointerEvent e) => rootUps++;
tree.Context.PushEvent(PointerTree.Down(WidgetTree.OverToggle, PointerButton.Left));
tree.Context.PushEvent(PointerTree.Up(WidgetTree.OverToggle, PointerButton.Left));
Assert.Equal(2, tree.Context.DrainEvents());
Assert.True(tree.Toggle.Checked);
Assert.Equal(0, rootUps);
```

A new test changes the count of `ToggleTests` (10) in section 14 and in F5. An extra assert in `AClickFlipsTheToggle` keeps the count.
**Effort:** Low

### CR-4: No test guards the `KeyDown` type check of `IsKeyActivation`
**Severity:** LOW
**Rule:** 9. Test quality, 2. A key is exact
**File:** `tests/BrutalUi.Core.Tests/Elements/ButtonKeyTests.cs:102-118`
**Problem:** Mutation M16 removes `e.Header.Type != UiEventType.KeyDown` and 0 tests fail. `AKeyUpDoesNotClick` cannot catch it, because the button has no handler in `OnKeyUp`. The check is not dead code. `Element.OnKeyDown` has a public getter, and rule 4 of the widget pattern says that a copied delegate is safe. With `button.OnKeyUp = button.OnKeyDown`, M16 gives a click on an Enter key up (probe `ACopiedKeyHandlerOnKeyUpDoesNotClick`). The check is right to keep (open risk 7).
**Fix:** Extend `AKeyUpDoesNotClick`: copy the key handler into `OnKeyUp`, push an Enter key up and a Space key up, and assert 0 clicks. Name the check in the `IsKeyActivation` remarks: "The check also reads the event type, so a copy of the handler in another slot does nothing."
**Effort:** Low

### CR-5: The key tests take the key codes from the code under test
**Severity:** LOW
**Rule:** 9. Test quality
**File:** `tests/BrutalUi.Core.Tests/Elements/ButtonKeyTests.cs:40,62,83-86`
**Problem:** `ButtonKeyTests`, `ToggleTests`, and `WidgetDisabledTests` push `WidgetInput.EnterKey` and `WidgetInput.SpaceKey`. A wrong constant then passes each of these classes. Mutation M51 (`SpaceKey = 33`) fails one test only: `WidgetAllocationTests.TheMeasuredBodyDoesRealWork`, which uses the literals 257 and 32. M17 (`EnterKey = 335`) fails `AnotherKeyDoesNotClickAndWalksOn` only because that test also pushes the literal 335. A side test of the allocation gate should not be the only guard of the GLFW key codes.
**Fix:** Add one assert to `AFocusedButtonClicksOnEnter` (or a private constant pair at the top of the class):

```csharp
// GLFW_KEY_ENTER and GLFW_KEY_SPACE.
Assert.Equal(257, WidgetInput.EnterKey);
Assert.Equal(32, WidgetInput.SpaceKey);
```
**Effort:** Low

### CR-6: A caller-target release clicks a pressed button, and the real release clicks again
**Severity:** LOW
**Rule:** 1. A click is exact, 10. The docs
**File:** `src/BrutalUi.Core/Elements/Button.cs:12-15`
**Problem:** A `PointerUp` with a caller target skips `RoutePointer`, so it does not end the press. It walks to the button, and the button still holds `Active` from the real press. Thus it clicks. The real release then clicks again. The probe `ACallerTargetReleaseAndTheRealReleaseGiveOneClick` gives 2 clicks for one physical press. This follows the documented rules ("a target from the caller changes no state", and "the release walks to the button"), and only game code that pushes a targeted release meets it. Thus the severity is LOW. The remarks do not state it.
**Fix:** Add one paragraph to the `Button` remarks (and the same to `Toggle`): "A `PointerUp` with a caller target does not end a press. When the button holds `Active`, it clicks, and the real release clicks again. Push a pointer event with no target to click a button from code." The fix of CR-1 closes the case in the code.
**Effort:** Low

### CR-7: The `Toggle` remarks miss two rules that the `Button` remarks state
**Severity:** LOW
**Rule:** 8. The widget pattern, 10. The docs
**File:** `src/BrutalUi.Core/Elements/Toggle.cs:38-42`
**Problem:** Open risk 5 (B6) applies to `Toggle` too. `EventWalk.InvokeSlot` stops after the delegate that stops the event, so a handler that a game adds with `+=` to `OnPointerUp` or `OnKeyDown` of a toggle gets no call for a flip. The `Button` remarks state this rule (lines 24-27). The `Toggle` remarks say only "A write of `=` to a slot removes that input". The `Toggle` remarks also do not say that a pickable child takes `Active` in place of the toggle (Notes Q1, `Button` lines 30-31). Later widgets copy the remarks of the nearest widget.
**Fix:** Add the two paragraphs of `Button` to the `Toggle` remarks, with "flip" in place of "click". Read `CheckedChanged`, not the slot.
**Effort:** Low

### CR-8: The `WidgetInput` docs name the toggle as later work and omit `Disabled` in the summaries
**Severity:** LOW
**Rule:** 10. The docs
**File:** `src/BrutalUi.Core/Elements/WidgetInput.cs:16-19,32-36,60-64`
**Problem:** The class remarks say "`Button` now, and the toggle of a later phase". `Toggle` uses `IsClick` now. The summaries of `IsClick` and `IsKeyActivation` list each condition except `Disabled`. Only the class remarks state the disabled rule.
**Fix:** Write "`Button` and `Toggle` use it". Add "and the widget does not hold `PseudoStates.Disabled`" to each of the two summaries.
**Effort:** Low

### CR-9: The `Utf8Text` summary names `Label` only
**Severity:** LOW
**Rule:** 10. The docs
**File:** `src/BrutalUi.Core/Elements/Utf8Text.cs:10-13`
**Problem:** Open risk 6. The summary says "`Label` holds one". `Button` holds one too. The follow-up of phase 4 in `context.md` names this line.
**Fix:** Write "`Label` and `Button` each hold one."
**Effort:** Low

### CR-10: The `WidgetTree` summary lists two children, but the tree holds three
**Severity:** LOW
**Rule:** 9. Test quality, 10. The docs
**File:** `tests/BrutalUi.Core.Tests/Elements/WidgetTree.cs:5-17`
**Problem:** Open risk 6. The summary and the first remarks paragraph name `Root`, `Button`, and `Other` only. The tree also holds `Toggle` at (220, 0, 40, 40), and `OverToggle` points into it. The property docs at lines 75-82 are correct.
**Fix:** Add the toggle to the summary, to the tree order, and to the list of rects.
**Effort:** Low

### CR-11: The `WidgetAllocationTests` remarks give a wrong event count and a wrong string rule
**Severity:** LOW
**Rule:** 9. Test quality, 10. The docs
**File:** `tests/BrutalUi.Core.Tests/Elements/WidgetAllocationTests.cs:19-27`
**Problem:** The remarks say "Each step of the input test pushes 8 events". `InputStep` pushes 6, and `TheMeasuredBodyDoesRealWork` asserts 6 walks. The remarks also say "The measured bodies use no lambda, no string". `TextStep` holds a local of type `string` (open risk 9). The local picks a string that the scene built, so it allocates nothing. The doc of `TextStep` correctly leaves out "no string".
**Fix:** Write "pushes 6 events". Write "The measured bodies create no lambda, no string, no boxed value, and no Assert. `TextStep` reads a string that the scene built."
**Effort:** Low

### CR-12: Section 5 of the technical design misses the measurer rule of `Button` and states the click rule with no chord limit
**Severity:** LOW
**Rule:** 10. The docs
**File:** `docs/technical-design.md:303,320`
**Problem:** "The measurer of the context" (line 303) names `Label` only. A `Button` also needs a measurer during `Update`, also with an empty text, unless the layout fixes both its width and its height. The widget paragraph (line 320) says that a press on another element gives no click. CR-1 shows that this is not true for a chord.
**Fix:** Line 303: write "A `Label` or a `Button` measures its text", and add "A `Button` with an empty text also needs the measurer, unless its width and its height are fixed." Line 320: write the chord rule that CR-1 decides.
**Effort:** Low

### CR-13: The `Utf8Text` line of the changelog has no feature tag
**Severity:** LOW
**Rule:** 10. The docs
**File:** `CHANGELOG.md:29`
**Problem:** Each other line of `## [Unreleased]` ends with the feature path in brackets. The `Utf8Text` line has none. The `Button` line under "Added" does not say that a `Button` needs a context with a measurer, which is a new rule for a game.
**Fix:** Append "(interactive-elements/button-and-toggle)" to line 29. Add to the `Button` line: "A `Button` measures its text, so its context needs a measurer unless its size is fixed."
**Effort:** Low

## What's Already Good

- **The release order (rule 5).** `FinishRelease` clears the flag first and then drops the reference before it clears the bit (B1). The `finally` block clears `_draining` first (B2). Each of the five release-order mutations fails at least one test, and M03 and M05 each fail 12. The states after the drain are the same as before the feature: no existing pointer or focus test changed.
- **The exact click and key (rules 1 and 2).** Each check of `IsClick` and `IsKeyActivation` has a guard: M06 to M15 and M17 each fail a test. Each modifier has its own push in `AModifierStopsTheActivation`. The GLFW codes are right: 32 (`GLFW_KEY_SPACE`), 257 (`GLFW_KEY_ENTER`), and 335 (`GLFW_KEY_KP_ENTER`).
- **One click, one call (rule 3).** M21 (two calls) fails 12 tests. M31 (the delegate before the flip) fails 2. The delegate runs after the stop and after the state change (Decision 10).
- **The disabled rule (rule 4).** The check lives in `WidgetInput` only. M08 and M15 fail 4 and 2 tests. Each disabled input test also reads a root handler, so the "no stop" rule has a guard.
- **Zero allocation (rule 6).** The handlers are `static readonly` delegates with no target, the cast is an `is` pattern, and the code holds no closure, no boxing, and no string on the happy path. Each plant fails a gate: M30, M37, and M38 fail `TheWidgetInputPathAllocatesNothing`, and M45 fails `SetTextOnAButtonAllocatesNothing`. `TheMeasuredBodyDoesRealWork` catches a body with no Space step (M49) and with no press over the toggle (M50).
- **`Label` does not change (rule 7).** The diff of `Label.cs` changes the bodies of `Text` and `LocalizedText` only, and each public signature and doc stays. The three internal forwarders keep each `Label` test with no edit. The `Utf8Text` mutations M39 to M44 each fail a `LabelTextTests` test.
- **The widget pattern (rule 8).** `Button` and `Toggle` follow the ten rules. `Toggle.Checked` reads the bit, so no field can disagree with `States`. Each `Utf8Text` field has no `readonly` modifier.
- **Open risk 3 (a `Button` with no text needs a measurer).** Not a finding. Task 4.4 accepted this rule. The `Button` remarks state it, and `AButtonNeedsAMeasurer` guards it. No release shipped the button of phases 1 to 3, so no game lost a working path. The missing doc line in section 5 is part of CR-12 and CR-13.
- **Open risk 4 (a `Button` with a child draws its text under the children).** Not a finding. The `Button` remarks state the limit, and the plan (Gap 6) gives the icon rule to a later feature. `TheButtonTakesTheSizeOfItsText` checks that a button with a child calls no measure.
- **Open risk 5 (B6).** Not a finding for `Button`: the remarks state the rule. The same gap in `Toggle` is CR-7.
- **Open risk 6.** The two doc lines are CR-9 and CR-10.
- **Open risk 7 (the `KeyDown` check).** The check is right: it keeps a copied handler safe, as rule 4 of the pattern asks. The missing test is CR-4.
- **Open risk 8 (a `default` `Utf8Text`).** Not a finding. `Span` gives an empty span for a `null` buffer with a length of 0. `EnsureCapacity` creates the first buffer for a `null` buffer. `Label` and `Button` each throw for two texts before they call the struct constructor, so the "localized wins" path cannot occur through an owner.
- **Open risk 9 (the `string` local of `TextStep`).** Not an allocation: the local reads a reference that the scene built. The wrong remark is part of CR-11.
- **Open risk 10 (the flaky `TransitionCurveTests` gate).** A widget gate cannot fail the same way. Each widget gate calls `AllocationGate.ReleaseAllocationContext` right before its one measured region, and the region allocates nothing, so the allocation context stays empty. `TransitionCurveTests.TheCurvesAllocateNothing` releases the context one time and then measures one region for each easing. After each region, it builds the message string of `Assert.True` with `+`, also when the assert passes. That string refills the allocation context between the regions. A background GC in a later region then counts the free part of the context (less than 8 KB, here 8056 bytes). The class is outside this feature. A follow-up can call the release before each region, or build the message only on a failure.
- **The phase 6 record.** The overlap of the first plant run of task 6.2 has no effect on the result: the second run in sequence gave the same failure, and this review repeated four other plants with the same result.
- **`AGENTS.md` (rule 10).** .NET 10, C# 14, nullable on, file-scoped namespaces, XML docs on each public member, `Brutal.Numerics` types, and no ImGui or GLFW `using` in Core.

## Recommended Fix Order

| Order | Finding | Severity | Effort | File | Fix group |
|-------|---------|----------|--------|------|-----------|
| 1 | CR-1 (a person decides first) | HIGH | Medium | `src/BrutalUi.Core/Tree/UiContext.cs` | A |
| 2 | CR-2 | MEDIUM | Low | `src/BrutalUi.Core/Style/StylePass.cs` | B |
| 3 | CR-3 | MEDIUM | Low | `tests/BrutalUi.Core.Tests/Elements/ToggleTests.cs` | C |
| 4 | CR-4 | LOW | Low | `tests/BrutalUi.Core.Tests/Elements/ButtonKeyTests.cs` | D |
| 5 | CR-5 | LOW | Low | `tests/BrutalUi.Core.Tests/Elements/ButtonKeyTests.cs` | D |
| 6 | CR-6 | LOW | Low | `src/BrutalUi.Core/Elements/Button.cs` | E |
| 7 | CR-7 | LOW | Low | `src/BrutalUi.Core/Elements/Toggle.cs` | F |
| 8 | CR-12 | LOW | Low | `docs/technical-design.md` | G |
| 9 | CR-8 | LOW | Low | `src/BrutalUi.Core/Elements/WidgetInput.cs` | H |
| 10 | CR-9 | LOW | Low | `src/BrutalUi.Core/Elements/Utf8Text.cs` | I |
| 11 | CR-10 | LOW | Low | `tests/BrutalUi.Core.Tests/Elements/WidgetTree.cs` | J |
| 12 | CR-11 | LOW | Low | `tests/BrutalUi.Core.Tests/Elements/WidgetAllocationTests.cs` | K |
| 13 | CR-13 | LOW | Low | `CHANGELOG.md` | L |

Do CR-1 after a person decides the chord rule. Its fix also touches `WidgetInput.cs` (CR-8) and the docs of CR-6 and CR-12, so do those three after CR-1. Each other fix group is independent.

## Progress

- [x] CR-1: A pointer chord gives a click after a left press on another element - **Deferred, not fixed in code.** Reason: the fix of this review is the rejected alternative (b) of Decision 2 (a context value that the handler reads, here a thread-static), and it changes the click rule of the widget pattern. The called mode of `/autorun-feature` asks nothing and edits no plan decision, so `/autorun-epic` asks a person (an open scope question of the report). The docs now state the chord limit: the `Button`, `Toggle`, and `IsClick` remarks (`review-g4.md`, `review-g5.md`, `review-g6.md`) and section 5 of the technical design (`review-g10.md`).
- [x] CR-2: A font or font size change does not measure a `Button` again - Fixed (`review-g1.md`): `StylePass` checks `element is Label or Button`, `StyleProperties.cs` names both types, and `TheButtonTakesTheSizeOfItsText` holds one more step (16 to 32, the width 32 to 64). The step failed before the fix. `ButtonTextTests` stays at 9.
- [x] CR-3: No test guards the stop of a toggle click - Fixed (`review-g2.md`): `AClickFlipsTheToggle` reads a root capture handler (1 call) and a root bubble handler (0 calls). The test fails with no stop. `ToggleTests` stays at 10.
- [x] CR-4: No test guards the `KeyDown` type check of `IsKeyActivation` - Fixed (`review-g3.md` and `review-g6.md`): `AKeyUpDoesNotClick` copies the key handler into `OnKeyUp` and asserts 0 clicks for Enter and Space key ups. The remarks of `IsKeyActivation` name the type check.
- [x] CR-5: The key tests take the key codes from the code under test - Fixed (`review-g3.md`): `AFocusedButtonClicksOnEnter` asserts 257 and 32.
- [x] CR-6: A caller-target release clicks a pressed button, and the real release clicks again - Fixed in the docs (`review-g4.md`): the `Button` remarks state the rule and tell the game to push a pointer event with no target.
- [x] CR-7: The `Toggle` remarks miss two rules that the `Button` remarks state - Fixed (`review-g5.md`): the `Toggle` remarks state the `+=` rule, the pickable child rule, the caller-target rule, and the chord limit.
- [x] CR-8: The `WidgetInput` docs name the toggle as later work and omit `Disabled` in the summaries - Fixed (`review-g6.md`).
- [x] CR-9: The `Utf8Text` summary names `Label` only - Fixed (`review-g7.md`).
- [x] CR-10: The `WidgetTree` summary lists two children, but the tree holds three - Fixed (`review-g8.md`).
- [x] CR-11: The `WidgetAllocationTests` remarks give a wrong event count and a wrong string rule - Fixed (`review-g9.md`).
- [x] CR-12: Section 5 of the technical design misses the measurer rule of `Button` and states the click rule with no chord limit - Fixed (`review-g10.md`): the measurer paragraph names `Button`, the click rule states the chord limit, and the font change rule names `Button`.
- [x] CR-13: The `Utf8Text` line of the changelog has no feature tag - Fixed (`review-g11.md`): the `Utf8Text` line has the tag, the `Button` line names the measurer rule, and a new "Changed" line names the font change rule.
