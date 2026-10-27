# interactive-elements/text-field - Code Review

**Reviewed:** 2026-10-27
**Reviewer:** Agent (cross-phase review of phases 1 to 7)
**Rules Applied:** 1. The buffer never grows, 2. The caret never splits a code point, 3. One change, one call, 4. The caret stays visible, 5. The walks do not change, 6. The keys and the pointer route right, 7. Disabled means no effect, 8. Zero allocation after warm-up, 9. No change for other elements, 10. The widget pattern, 11. Test quality, 12. `AGENTS.md` and the docs. The Vercel React rules do not apply to C#.
**Status:** Resolved - 16 findings, 16 resolved

---

## Summary

| Severity | Count |
|----------|-------|
| HIGH | 0 |
| MEDIUM | 8 |
| LOW | 8 |

**Overview.** The code of the field is sound. The buffer never grows, each path keeps the caret on a boundary, each edit calls `TextChanged` one time, and the gates measure 0 bytes. The walks have no diff, and `UiContext.cs` changes in the caret pass and one restart line only. The review found one code defect: a hide or a width of 0 on the focused field scrolls the text out of the view, and the field keeps that scroll after the show (CR-1). The other medium findings are rules that no test guards. The mutation probes found them (CR-2 to CR-8). The low findings are small test gaps, one small disabled-rule gap, and doc drift.

**Baseline (task R.3).** `dotnet build brutal-ui.sln`: 0 warnings and 0 errors. `dotnet test --solution brutal-ui.sln`: total 2544, succeeded 2542, skipped 2 (no display), failed 0. Core 1702 in the solution run. The first run had no flake. The scratch copy with no mutation gave Core 1711 passed and 0 failed in the project run (the known difference of 9 between the two runners).

**Probe method.** The copy lived in the scratchpad folder `cr-text-field/repo` (rsync with no `bin`, `obj`, `.git`, or `artifacts`). The originals went to `cr-text-field/orig/` first. Each mutation sat behind its own symbol (`#if Mnn` or `#if Ann`) in `TextField.cs`, `WidgetInput.cs`, or `UiContext.cs`. `Directory.Build.props` of the copy appended `$(Mut)` to `DefineConstants`. Each run built `BrutalUi.Core.Tests` with `-p:Mut=<symbol> -p:TreatWarningsAsErrors=false` and ran the full Core project with `dotnet run --no-build`. The failed test names below come from that run. A `TransitionCurveTests.TheCurvesAllocateNothing` failure in M04, M25, M38, and M44 is the known flake outside this feature. The list omits it.

**Mutation totals.** 66 probes: 53 caught, 1 equivalent, 12 not caught (findings).

### Mutation probes

Group 1: the buffer and the char.

1. M01 The fit check after the remove of the selection (also the "replace that removes the selection before the fit check" of group 6): `TextFieldSelectionTests.ADroppedCharKeepsTheSelection`.
2. M02 The fit check with `>=`: `TextFieldDeleteTests.ARemoveMakesRoomForAnInsert`, `TextFieldTypeTests.AnInsertPastTheCapacityDrops`, `TextFieldSelectionTests.ADroppedCharKeepsTheSelection`, `TextFieldTypeTests.ACodePointThatDoesNotFitDrops`.
3. M03 No `Rune.IsControl` check: `TextFieldTypeTests.AControlCharWalksOn`.
4. M04 No scalar check (an invalid code point becomes U+FFFD): `TextFieldTypeTests.AControlCharWalksOn`.
5. M05 No stop on a drop: `TextFieldSelectionTests.ADroppedCharKeepsTheSelection`, `TextFieldTypeTests.AnInsertPastTheCapacityDrops`, `TextFieldTypeTests.ACodePointThatDoesNotFitDrops`.
6. M06 A stop on a control char: `TextFieldTypeTests.AControlCharWalksOn`.
7. M07 `TextChanged` before the insert: 0 failed (finding CR-2).
8. M08 `TextChanged` on a dropped char: `TextFieldAllocationTests.TheMeasuredBodyDoesRealWork`, `TextFieldDeleteTests.ARemoveMakesRoomForAnInsert`, `TextFieldSelectionTests.ADroppedCharKeepsTheSelection`, `TextFieldTypeTests.AnInsertPastTheCapacityDrops`, `TextFieldTypeTests.ACodePointThatDoesNotFitDrops`.
9. M09 The caret not moved after an insert: 21 tests, among them `TextFieldTypeTests.ACharShowsInTheField`, `TextFieldCaretTests.ACharGoesInAtTheCaret`, and `TextFieldScrollTests.LongTextScrollsUnderTheCaret`.

Group 2: the boundary steps.

10. M10 `NextBoundary` steps one byte: `TextFieldPointerTests.APressStepsOverAWholeCodePoint`, `TextFieldDeleteTests.BackspaceRemovesAWholeCodePoint`, `TextFieldTextTests.GetTextIntoAShortSpanCutsAtACodePoint`, `TextFieldTextTests.ASetPastTheCapacityCutsAtACodePoint`, `TextFieldCaretTests.TheCaretStepsOverEachCodePoint`.
11. M11 `PreviousBoundary` steps one byte: `TextFieldDeleteTests.BackspaceRemovesAWholeCodePoint`, `TextFieldCaretTests.TheCaretStepsOverEachCodePoint`.
12. M12 A step of 0 for an invalid byte (both steps): `TextFieldTextTests.AnInvalidByteStepsAsOne`.
13. M13 `CutAtBoundary` cuts inside a code point: `TextFieldTextTests.GetTextIntoAShortSpanCutsAtACodePoint`, `TextFieldTextTests.ASetPastTheCapacityCutsAtACodePoint`.
14. M14 `GetText` copies a split code point: `TextFieldTextTests.GetTextIntoAShortSpanCutsAtACodePoint`.

Group 3: the keys.

15. M15 `IsEditKey` with no `Control` check: `TextFieldSubmitTests.TheModifiersAndEnter`, `TextFieldCaretTests.TheModifiersBlockTheKey`.
16. M16 `IsEditKey` with no `Disabled` check: `TextFieldDisabledTests.ADisabledFieldTakesNoKey`.
17. M17 `IsEditKey` with no target check: 0 failed (finding CR-7).
18. M18 `IsEditKey` rejects a repeat: `TextFieldDeleteTests.ARepeatRemovesAgain`, `TextFieldCaretTests.ARepeatMovesAgain`.
19. M19 Tab stopped: `TextFieldCaretTests.TheOtherKeysWalkOn`.
20. M20 Left at 0 walks on: `TextFieldCaretTests.TheCaretStopsAtEachEnd`.
21. M21 A repeat of Enter submits: `TextFieldSubmitTests.ARepeatOfEnterWalksOn`.
22. M22 The keypad Enter submits: `TextFieldSubmitTests.TheKeypadEnterWalksOn`, `TextFieldCaretTests.TheOtherKeysWalkOn`.
23. M23 `Submitted` with no stop: `TextFieldSubmitTests.EnterStopsTheWalk`, `TextFieldSubmitTests.TheModifiersAndEnter`, `TextFieldSubmitTests.EnterWithNoDelegateStillStops`.
24. M57 `IsCharInput` with no target check: 0 failed (finding CR-7).
25. M58 `IsCharInput` with no `Disabled` check: `TextFieldDisabledTests.ADisabledFieldTakesNoChar`.

Group 4: `SetText`.

26. M24 The same text marks `Paint`: `TextFieldTextTests.AnEmptySetClearsTheText`, `TextFieldTextTests.ASetPastTheCapacityCutsAtACodePoint`, `TextFieldTextTests.TheSameTextMarksNothing`.
27. M25 A set calls `TextChanged`: `TextFieldAllocationTests.TheMeasuredBodyDoesRealWork`, `TextFieldDisabledTests.ACodeSetStillWorks`, `TextFieldTextTests.SetTextCopiesTheBytes`, `TextFieldSelectionTests.SetTextClearsTheSelection`.
28. M26 The span overload with no code point cut (a byte cut): `TextFieldTextTests.ASetPastTheCapacityCutsAtACodePoint`.
29. M27 The string overload with no compare: `TextFieldTextTests.AnEmptySetClearsTheText`, `TextFieldTextTests.ASetPastTheCapacityCutsAtACodePoint`, `TextFieldTextTests.TheSameTextMarksNothing`.
30. M28 `EqualsText` replaced by `Utf8Text.Utf8Equals` (the whole string): `TextFieldTextTests.ASetPastTheCapacityCutsAtACodePoint`.
31. M29 No scroll reset: `TextFieldScrollTests.SetTextAndTheFocusOwnerRule`.
32. M30 No anchor collapse: `TextFieldAllocationTests.TheMeasuredBodyDoesRealWork`, `TextFieldTextTests.AnInvalidByteStepsAsOne`, `TextFieldTextTests.TheTextIsUsableAfterASet`, `TextFieldTextTests.TheSameTextMarksNothing`, `TextFieldSelectionTests.SetTextClearsTheSelection`.

Group 5: the caret pass.

33. M31 The caret pass after the clear pass: 0 failed (finding CR-10).
34. M32 The caret pass before the layout pass: `TextFieldScrollTests.AResizeKeepsTheCaretVisible`.
35. M33 No caret pass in `Update`: 22 tests of the blink, scroll, pointer, and selection classes.
36. M34 No restart in `SetFocusOwner`: `TextFieldBlinkTests.AFocusGainRestartsTheBlink`.
37. M35 The restart in the repair branch too: 0 failed (finding CR-11).
38. M36 The blink with no finite check: 0 failed (finding CR-9).
39. M37 The scroll rule with the wrong sign (`caretX + (width - CaretWidth)`): 0 failed (finding CR-3).
40. M38 No upper clamp: `TextFieldScrollTests.ARemoveAtTheEndPullsTheTextBack`.
41. M39 No lower clamp (`scroll < 0` removed from the final check): 0 failed. **Equivalent mutant.** The scroll starts at 0 or more. The right-edge rule writes `caretX - (width - CaretWidth)` only when that value is above the old scroll, the left-edge rule writes `caretX`, and the upper bound is at least 0. Thus the scroll is below 0 only for a measurer that gives a negative width, and neither measurer does.
42. M55 No left-edge rule (`caretX - scroll < 0`): `TextFieldScrollTests.HomeScrollsBackToTheStart`, `TextFieldScrollTests.AMovePastTheLeftEdgeScrolls`.
43. M40 The pass reads `t_measurer`: 16 tests of the pointer, scroll, and selection classes.
44. M41 The pass runs for each `TextField`: `TextFieldScrollTests.SetTextAndTheFocusOwnerRule`.

Group 6: the selection.

45. M42 Left and Right with a selection and no `Shift` step: `TextFieldSelectionTests.LeftAndRightCollapseTheSelection`.
46. M44 The selection rect after the text: `TextFieldSelectionTests.TheSelectionDrawsBehindTheTextOnlyWithFocus`, `TextFieldSelectionTests.TheSelectionFollowsTheScroll`, `TextFieldSelectionTests.ACharReplacesTheSelection`.
47. M45 The selection drawn with no focus: `TextFieldSelectionTests.TheSelectionDrawsBehindTheTextOnlyWithFocus`.
48. M46 Backspace with a selection removes one code point: `TextFieldSelectionTests.BackspaceAndDeleteRemoveTheSelection`.

Group 7: the press and the drag.

49. M47 `IndexAtX` with a tie to the upper index: `TextFieldPointerTests.APressPlacesTheCaretAtTheNearestBoundary`.
50. M48 No scroll in the text-space x: `TextFieldPointerTests.APressInAScrolledFieldReadsTheScroll`.
51. M49 The border box in place of the content box in the text-space x: 0 failed (finding CR-5).
52. M50 A drag move that does not extend (the move writes the pending anchor): `TextFieldPointerTests.ADragPastTheRightEdgeScrolls`, `TextFieldPointerTests.APressAndADragSelect`.
53. M51 A `Shift` press that collapses: `TextFieldPointerTests.ShiftWithAPressExtendsTheSelection`.
54. M52 `CapturesMoves` that ignores `Disabled`: 0 failed (finding CR-6).
55. M53 A right press places the caret (no button and no press target check): `TextFieldPointerTests.ARightPressAndAChordPlaceNothing`.
56. M54 The pending x not cleared in `AdvanceCaret`: 0 failed (finding CR-4).
57. M56 The pending x not dropped in `RestartCaretBlink`: 0 failed (finding CR-4).

Group 8: the allocation plants (`GC.KeepAlive(new object());` at the start of each member).

58. A01 `HandleKeyDown`: `TextFieldAllocationTests.TheTextFieldInputPathAllocatesNothing`.
59. A02 `HandlePointerDown`: `TextFieldAllocationTests.TheTextFieldInputPathAllocatesNothing`.
60. A03 `HandlePointerMove`: `TextFieldAllocationTests.TheTextFieldInputPathAllocatesNothing`.
61. A04 `AdvanceCaret`: `TheTextFieldInputPathAllocatesNothing`, `UpdateAndDrawAllocateNothingWithTextFields`.
62. A05 `PaintContent`: `TheTextFieldInputPathAllocatesNothing`, `UpdateAndDrawAllocateNothingWithTextFields`.
63. A06 `SetText(string?)`: 0 failed (finding CR-8).
64. A07 `GetText`: `TextFieldAllocationTests.TheTextFieldInputPathAllocatesNothing`.
65. A08 `IndexAtX`: `TextFieldAllocationTests.TheTextFieldInputPathAllocatesNothing`.
66. A09 The caret pass line of `UiContext.Update`: 22 gates, among them both text field gates, `ThousandElementAllocationTests.UpdateAllocatesNothingAfterWarmUp`, `SliderAllocationTests.TheSliderInputPathAllocatesNothing`, and `ScrollViewAllocationTests.UpdateAndDrawAllocateNothingWithScrollViews`.

The plant of task 7.3 (`HandleChar`) did not run again.

### Scenario probes

Each probe was a temporary test class in the copy. The copy deleted it after the run.

- (a) A `TextChanged` handler sets `X` on the same field when the text is `ab`, and the drain holds `abc`. Result: the text `Xc`, the caret and the anchor at 2, 3 calls, and no exception. No defect.
- (b) A `TextChanged` handler removes the field from the tree, and the drain holds `abc`. Result: the text `a` and 1 call. The chars `b` and `c` get no walk, and the root gets no char. The first `Update` keeps the removed field as the focus owner, and its caret pass advances the blink of the removed field (the stale focus rule drops an owner at the start of the next drain). The second `Update` drops the focus and clears the bit. No exception. No defect (the documented one-frame limit of `DropStaleFocus`).
- (c) A `Submitted` handler calls `ClearFocus`, and a char follows in the same drain. Result: 1 submit, no text change, no `TextChanged` call, no walk for the char, no owner, and no caret rect. No defect.
- (d) A field of 81 x 16 at (0, 40) in a `ScrollView` of 200 x 100 with the offset (20, 20). A press at the origin plus 21 and a move to the origin plus 37 give the hit `field`, the focus, the anchor 3, and the caret 5. No defect.
- (e) Game code writes `Inline.Overflow = Overflow.Visible` after the constructor. Result: no clip push, and the text draws at x -16, outside the field. The class remarks state this effect. No defect.
- (f) A field with the width 50 % in a parent of 100. The parent widths 100, 1, 0.5, 0, and 100 give the field widths 50, 0.5, 0.25, 0, and 50, and the scrolls 31, 80, 80, 80, and 31. The scroll stays finite and 0 or more. At a width below `CaretWidth`, the left-edge rule wins, and the clip cuts the caret rect. The field holds the focus, so the last pass corrects the scroll. No defect while the field holds the focus. Probes (n) and (o) show the defect when the focus goes (CR-1).
- (g) A `Label` `abc` and a `TextField` `abc` with the font size 16 in one column. Result: the measure 24, the label width 24, both texts at x 0, and the caret rect at x 24. No defect.
- (h) Extra: 20 000 random byte strings of 0 to 11 bytes from a pool of lead, continuation, invalid, and surrogate bytes. Each `PreviousBoundary` result and each `CutAtBoundary` result is a `NextBoundary` boundary. 0 mismatches.
- (i) Extra (risk 2): `EqualsText` at the chunk edge. A surrogate pair across the 64-byte chunk edge, a pair cut at the capacity (65), a capacity of exactly 64 with a longer text, a lone surrogate, and a capacity of 1 with a 4-byte code point each compare as the cut form. A text that differs in the cut form compares as a change. No defect.
- (j) Extra: a press on a field with a padding of 4 and a border of 1 maps to the caret 3. The code is right. CR-5 covers the test gap.
- (k) Extra: a press and a Home key in one drain give the caret 0, so the later input wins. The code is right. CR-4 covers the test gap.
- (l) Extra: an infinite time step in the hidden phase keeps the caret hidden. The code is right. CR-9 covers the test gap.
- (m) Extra: a press on a disabled field and a move over `Other` give `Other` one move. The code is right. CR-6 covers the test gap.
- (n) Extra: a focused field `hello` (scroll 0) gets `Display.None`. The next `Update` writes the scroll 40. The next drain drops the focus. After `Display.Flex`, the field draws its text at x -40, and the clip hides all of it. Defect (CR-1).
- (o) Extra: a focused field `hello` gets the width 0, then the focus goes to `Other`, then the width 81 comes back. The scroll stays 40, and the text draws at x -40. Defect (CR-1).

---

## Findings

### CR-1: A hide or a width of 0 on the focused field scrolls the text out of the view for good
**Severity:** MEDIUM
**Rule:** 4. The caret stays visible
**File:** `src/BrutalUi.Core/Elements/TextField.cs:547-583`
**Problem:** The style pass gives a hidden element a rect of 0, and the stale focus rule drops the owner only at the next drain. Thus the caret pass of the hide frame runs with a content width of 0. The right-edge rule then writes `caretX + CaretWidth`, the left-edge rule pulls it back to `caretX`, and the field keeps `_scroll = caretX`. The next drain drops the focus, so no later pass corrects the scroll. When the game shows the field again, the text draws at `content.X - caretX`. With the caret at the end, the clip hides each glyph, and the field looks empty until the player focuses it again. Probes (n) and (o) show it: a field `hello` with the scroll 0 gets the scroll 40 and draws its text at x -40. A game that closes a chat panel with a focused field, or a layout that shrinks a field to 0, hits this case. P4-7 says that "a later pass with a real width corrects the scroll", but no later pass runs for a field that lost the focus.
```csharp
float width = Layout.ContentBox(in size).Z;
float caretX = measurer.MeasureText(Style.Font, Style.FontSize, Text[.._caret], 0f).X;
...
if (caretX - scroll > width - CaretWidth)
{
    scroll = caretX - (width - CaretWidth);
}
```
**Fix:** Keep the scroll when the content box has no room for the caret. The draw also shows nothing for a content width of 0, so the old scroll is the right state to keep.
```csharp
float width = Layout.ContentBox(in size).Z;
if (!(width >= CaretWidth))
{
    // A hidden field or a field with no room for the caret keeps its scroll, so the text shows again as before.
    return;
}
```
Extend `TextFieldScrollTests.AResizeKeepsTheCaretVisible`: after its checks, set `Inline.Display = Display.None`, run two frames (the focus drops), set `Display.Flex`, run one frame, and assert that `TextScroll` stays 56. Update P4-7 in `context.md` and the remark "A field that is not the focus owner keeps its scroll".
**Effort:** Low

### CR-2: No test checks that `TextChanged` comes after the insert of a char
**Severity:** MEDIUM
**Rule:** 3. One change, one call, and 11. Test quality
**File:** `tests/BrutalUi.Core.Tests/Elements/TextFieldTypeTests.cs:31`
**Problem:** Mutation M07 calls `TextChanged` before `Insert` in `HandleChar`, and 0 tests fail. Each char test counts the calls and reads `Text` after the drain only. Only the Backspace path (`BackspaceRemovesTheCodePointBeforeTheCaret`) reads `Text` inside the delegate. A store binding reads `field.Text` in the delegate, so a call before the change gives the store the old text. The code is right today.
**Fix:** Extend the demo test `ACharShowsInTheField`: the delegate also records `Encoding.UTF8.GetString(f.Text)` into a local, and the test asserts `"Hi"` for the last call. Add no test method.
```csharp
string? seen = null;
tree.Field.TextChanged = f =>
{
    calls++;
    sender = f;
    seen = Encoding.UTF8.GetString(f.Text);
};
...
Assert.Equal("Hi", seen);
```
**Effort:** Low

### CR-3: No test guards the smallest scroll for a move right inside a long text
**Severity:** MEDIUM
**Rule:** 4. The caret stays visible, and 11. Test quality
**File:** `tests/BrutalUi.Core.Tests/Elements/TextFieldScrollTests.cs:87`
**Problem:** Mutation M37 writes `scroll = caretX + (width - CaretWidth)` in the right-edge rule, and 0 tests fail. Each test that passes the right edge has the caret at the end of the text. There the upper clamp gives the right value for each sign. With text after the caret, the mutant scrolls the caret to the left edge (the left-edge rule) or to the upper bound, not by the smallest distance. Decision 12 names the smallest distance.
**Fix:** Extend `AMovePastTheLeftEdgeScrolls` with a right step: set the text to 14 chars `a` with `SetText`, run a frame, push Home and 11 Right keys, and run a frame. Assert `TextScroll` 8 and the caret rect at (80, 0, 1, 16). The mutant gives 32.
**Effort:** Low

### CR-4: No test guards the drop rule of the pending press x
**Severity:** MEDIUM
**Rule:** 6. The keys and the pointer route right, and 11. Test quality
**File:** `tests/BrutalUi.Core.Tests/Elements/TextFieldPointerTests.cs:188`
**Problem:** Two mutations survive. M56 removes `DropPending()` from `RestartCaretBlink`. Then a press and a later Home key in one drain end with the caret at the press point, so the earlier input wins (probe (k) shows the right result, 0, on the real code). A focus gain also no longer drops an old press. M54 removes the clear in `AdvanceCaret`. Then a press that maps to the current caret stays pending. Each later caret pass runs `IndexAtX` again (one measure for each code point in each frame), and a font change moves the caret with no input. P6-5 and P6-6 state both rules, and the class remarks promise "the later input wins".
**Fix:** Extend `APressFocusesTheFieldAndShowsTheCaret`. (1) Push a press at x 21, its release, and a Home key in one drain, then settle: assert the caret 0 (M56). (2) Click at x 16 two times (the caret 2, and the second press maps to the same index), set `Inline.FontSize = 32`, and settle: assert the caret 2 (M54 gives 1).
**Effort:** Low

### CR-5: No test guards the padding and the border in the text-space x
**Severity:** MEDIUM
**Rule:** 6. The keys and the pointer route right, and 11. Test quality
**File:** `tests/BrutalUi.Core.Tests/Elements/TextFieldPointerTests.cs:142`
**Problem:** Mutation M49 maps the pointer x from the border box in place of the content box, and 0 tests fail. `TextFieldTree` gives the field no padding and no border, so both boxes start at x 0. A styled field nearly always has a padding, so the mutant places the caret a few pixels off in the common case. Probe (j) shows that the code is right: a padding of 4 and a border of 1 map x 26 to the caret 3.
**Fix:** Extend `APressInAScrolledFieldReadsTheScroll` with a padded step: give the field the width 91, the height 26, `Padding = LengthEdges.All(4)`, and `BorderWidth = LengthEdges.All(1)`, set the text `hello`, and click at x 26 (y 8). Assert the caret 3.
**Effort:** Low

### CR-6: No test guards the capture rule of a disabled field
**Severity:** MEDIUM
**Rule:** 7. Disabled means no effect, and 11. Test quality
**File:** `tests/BrutalUi.Core.Tests/Elements/TextFieldDisabledTests.cs:109`
**Problem:** Mutation M52 makes `IDragCapture.CapturesMoves` always `true`, and 0 tests fail. A press on a disabled field sets `Active` on it, so the mutant sends each later move to the field, and the element under the pointer gets no move. `APressOnADisabledFieldGivesNoFocus` moves to (37, 8), which is over the field, so the hit and the capture are the same element. Probe (m) shows the right result on the real code: `Other` gets the move.
**Fix:** Extend `APressOnADisabledFieldGivesNoFocus`: count `Other.OnPointerMove` calls, move to `TextFieldTree.OverOther` before the release, and assert 1 call.
**Effort:** Low

### CR-7: No test guards the target rule of `IsEditKey` and `IsCharInput`
**Severity:** MEDIUM
**Rule:** 6. The keys and the pointer route right, and 10. The widget pattern
**File:** `tests/BrutalUi.Core.Tests/Elements/TextFieldTypeTests.cs:222`
**Problem:** Mutations M17 and M57 remove the `ReferenceEquals(e.Header.Target, widget)` term of `IsEditKey` and `IsCharInput` in `src/BrutalUi.Core/Elements/WidgetInput.cs`, and 0 tests fail. The term matters when a focusable child of a field holds the focus: the key or the char bubbles to the field, and the field must not edit. The `WidgetInput` remarks and section 5 state the rule. The sibling widgets pin the same rule for `IsArrowKey` and `IsKeyActivation`.
**Fix:** Extend `ACharWithACallerTargetInserts`: add a focusable child element to `Field`, push a char `z` and a Left key with the child as the caller target, and drain. Assert that the text and the caret do not change, that `TextChanged` gets no call, and that a root bubble handler gets both entries.
**Effort:** Low

### CR-8: No gate covers `SetText(string?)` and `SetText(LString?)`
**Severity:** MEDIUM
**Rule:** 8. Zero allocation after warm-up
**File:** `tests/BrutalUi.Core.Tests/Elements/TextFieldAllocationTests.cs:209`
**Problem:** Plant A06 at the start of `SetText(string?)` fails no gate. The input step calls only the span overload (`SetText("seed"u8)` and `SetText(FullText)`). The string overload runs `EqualsText` (a stack chunk loop) and `Utf8.FromUtf16`, and the `LString` overload reads `Localized`. The Quality Bar and section 14 claim 0 bytes for `SetText`. The code allocates nothing today, but no gate holds it.
**Fix:** Extend `InputStep`: keep two `static readonly string` values and one `LString` in the scene, and set them in turn on `Fields[3]` (a set of the same text and a set of a new text, so both the compare and the encode run). Keep fields 0 to 2 as they are, so `TheMeasuredBodyDoesRealWork` does not change. Run plant A06 again and expect `TheTextFieldInputPathAllocatesNothing` to fail.
**Effort:** Low

### CR-9: No test applies an infinite time step in the hidden phase
**Severity:** LOW
**Rule:** 11. Test quality
**File:** `tests/BrutalUi.Core.Tests/Elements/TextFieldBlinkTests.cs:110`
**Problem:** Mutation M36 removes the `float.IsFinite` term of the blink step, and 0 tests fail. The mutant computes `NaN`, and the guard maps it to 0, so the caret shows. `ABadDeltaAdvancesNothing` applies the infinite step in the shown phase only, where the guard gives the same result. In the hidden phase, the mutant shows the caret and marks `Paint`. Probe (l) shows the right result on the real code (the caret stays hidden). P4-4 states the rule.
**Fix:** Extend `ABadDeltaAdvancesNothing`: after the last step (the caret hidden), apply `float.PositiveInfinity` again and assert that `CaretVisible` stays `false`.
**Effort:** Low

### CR-10: No test pins the caret pass before the clear pass
**Severity:** LOW
**Rule:** 9. No change for other elements, and 11. Test quality
**File:** `tests/BrutalUi.Core.Tests/Elements/TextFieldBlinkTests.cs:21`
**Problem:** Mutation M31 moves the caret pass after the clear pass in `UiContext.Update`, and 0 tests fail. The `Paint` mark of a blink flip then stays after `Update`, against the frame contract that the clear pass leaves no bit. Open risk 3 (P4-8 and Q-P4-1): the clear pass of the same `Update` clears the mark, and no code reads it now. That is acceptable, because `Draw` draws each frame. But the order of section 4 has no guard, and a future paint cache depends on it.
**Fix:** Extend `TheCaretBlinks`: after the frame with `0.5f` (the flip), assert `tree.Field.Dirty & DirtyFlags.Paint` is `DirtyFlags.None`.
**Effort:** Low

### CR-11: No test pins the repair branch of `SetFocusOwner` (no restart)
**Severity:** LOW
**Rule:** 11. Test quality
**File:** `tests/BrutalUi.Core.Tests/Elements/TextFieldBlinkTests.cs:80`
**Problem:** Mutation M35 restarts the blink in the repair branch too, and 0 tests fail. The doc comment of `SetFocusOwner` states "The repair branch restarts nothing". With the mutant, `SetFocus` on the focused field and a press on the focused field show a hidden caret.
**Fix:** Extend `AFocusGainRestartsTheBlink`: hide the caret, call `SetFocus(Field)` while the field holds the focus, run a frame with `0f`, and assert that `CaretVisible` stays `false`. See also follow-up question 3.
**Effort:** Low

### CR-12: A disable after a press in the same drain still moves the caret
**Severity:** LOW
**Rule:** 7. Disabled means no effect
**File:** `src/BrutalUi.Core/Elements/TextField.cs:532-540`
**Problem:** Open risk 10 (P6-9). A handler that disables the field after the press of the same drain does not stop the map. The caret pass of that `Update` moves the caret and the anchor of a disabled field, and the draw of that frame still shows the caret and the selection, because the focus drops at the next drain. The text does not change, and no delegate gets a call, so the effect is small.
**Fix:** Skip the two caret moves for a disabled field. The values stay cleared, and the scroll rule still runs.
```csharp
if (measurer is null)
{
    return;
}

// A handler of the same drain can disable the field after the press. A disabled field maps no press.
if ((States & PseudoStates.Disabled) == 0)
{
    if (!float.IsNaN(anchorX))
    {
        MoveCaret(IndexAtX(Text, anchorX, measurer, Style.Font, Style.FontSize), false);
    }

    if (!float.IsNaN(caretX))
    {
        MoveCaret(IndexAtX(Text, caretX, measurer, Style.Font, Style.FontSize), true);
    }
}

ApplyScrollRule(measurer);
```
Extend `TextFieldDisabledTests.APressOnADisabledFieldGivesNoFocus` with an enabled step: a root `OnPointerUp` handler sets `Disabled` on the field (the field has no release handler, so the release bubbles to the root). Push a press at x 21 and its release in one drain, and settle. Assert that the caret and the anchor stay at 5.
**Effort:** Low

### CR-13: The remarks do not state the effect of a line feed in a text from code
**Severity:** LOW
**Rule:** 12. `AGENTS.md` and the docs
**File:** `src/BrutalUi.Core/Elements/TextField.cs:107-115`
**Problem:** Open risk 1 (P1-3 and P3-1). A text from code keeps a control character, and the draw sends each byte to the renderer. `ImGuiRenderer.MeasureText` calls `ImFont_CalcTextSizeA`, which breaks a line at `\n`. Thus a line feed gives a text of two lines in a one-line field, a caret x of the longest line, a caret height of two lines, and a wrong `IndexAtX` map. The remarks say only that a set does not filter. A typed control char walks on, and that rule is right.
**Fix:** Add one sentence to the remarks of the class and of `SetText(ReadOnlySpan<byte>)`: "A line feed or another control character in a text from code breaks the caret position and the scroll. Remove it before the call." Follow-up question 1 asks if the field must filter.
**Effort:** Low

### CR-14: No test removes a 2-byte code point
**Severity:** LOW
**Rule:** 2. The caret never splits a code point, and 11. Test quality
**File:** `tests/BrutalUi.Core.Tests/Elements/TextFieldDeleteTests.cs:66`
**Problem:** The Quality Bar says "A test types and removes 1-, 2-, 3-, and 4-byte code points". The delete tests remove 1-byte, 3-byte (`€`), and 4-byte (`😀`) code points. No test removes a 2-byte code point. Section 14 says that `TextFieldDeleteTests` "checks Backspace and Delete on code points of 1 to 4 bytes".
**Fix:** Extend `BackspaceRemovesAWholeCodePoint` with a field `aé`: Backspace gives `a` and the caret 1.
**Effort:** Low

### CR-15: Format drift in `TextField.cs` from the scripted edits
**Severity:** LOW
**Rule:** 12. `AGENTS.md` and the docs
**File:** `src/BrutalUi.Core/Elements/TextField.cs:112`
**Problem:** Open risk 15. The scripted edits of phase 4 left stray format: an empty first line in the body of `ApplyScrollRule` (line 549); doc lines of 149 to 156 characters where a sentence was appended to a wrapped line (lines 112, 411, and 1097); and a broken wrap in the constructor remarks (lines 300 to 304: "The constructor sets" on its own line and "It calls `TextChanged` never."). No code changed by error. `UiContext.cs` has no stray change, and the hunks of `git diff 4414826` are the caret pass, the restart line, and doc comments only.
**Fix:** Remove the empty line. Wrap the three long lines at 120 characters. Write the constructor remark as "The constructor sets `IsFocusable` to `true`, sets the inline overflow to `Overflow.Hidden`, and writes the four handlers. It calls no `TextChanged`."
**Effort:** Low

### CR-16: The quick status of `context.md` is old
**Severity:** LOW
**Rule:** 12. `AGENTS.md` and the docs
**File:** `docs/features/interactive-elements/text-field/context.md:9-28`
**Problem:** "What's Done" lists phases 1 to 5 only, and "What's Next" says "Phase 6". The header says "Current Phase: Cross-phase review", and the gates of phases 6 and 7 are on record.
**Fix:** Add phases 6 and 7 to "What's Done", and set "What's Next" to the fixes of this review.
**Effort:** Low

---

## What's Already Good

- **The buffer and the boundaries (rules 1 and 2).** The constructor holds the only `new byte[`. The fit check comes before each change, so a dropped char keeps the selection (M01, M02, M05, M08 caught). Probe (h) found 0 mismatches between the forward step, the backward step, and the cut in 20 000 random invalid byte strings.
- **One change, one call (rule 3).** A move, a dropped char, a set from code, and the start text call nothing (M08, M25 caught). Probe (a) shows that a `SetText` inside `TextChanged` leaves a valid caret for the next char.
- **The walks and the other elements (rules 5 and 9).** `HitWalk.cs`, `PaintWalk.cs`, each `.csproj`, `Slider.cs`, `ScrollView.cs`, and each existing test file have no diff. Plant A09 fails 22 gates, so the existing gates measure the caret pass line.
- **The key and pointer routes (rule 6).** Each edit key stops the walk also at an end, and Tab, the keypad Enter key, and a repeat of Enter walk on (M19 to M23 caught). The press handlers follow the slider pattern: a static handler with no target, `IsDragStart` and `IsDragMove`, a stop, and `CapturesMoves` that reads `Disabled`. Probe (d) shows the map through a scrolled `ScrollView`.
- **Open risk 2: `EqualsText`.** Probe (i) compares the cut form right at the 64-byte chunk edge, for a surrogate pair that crosses the edge, for a pair that the capacity cuts, for a lone surrogate, and for a capacity of 1. M28 shows that the whole-string compare of `Utf8Text.Utf8Equals` breaks the same long text, so P3-2 is right.
- **Open risk 3: the cleared `Paint` mark.** No code reads the mark today, and `Draw` draws each frame, so no reader misses a change. CR-10 adds only a guard of the order.
- **Open risk 4: an infinite time step.** The code keeps the phase (probe (l)), as P4-4 and the `Update` doc state. CR-9 adds only a test.
- **Open risk 5: two `Paint` marks for a replace.** A second mark of the same bit adds no state and no allocation. The cost is one more parent walk for each replaced char.
- **Open risk 6: two pending x values (P6-1).** One x and one flag lose the press point when the press and the move come in one drain. The demo and the gate push both before one `Update`, and M50 shows that the tests pin the split.
- **Open risk 7: the drop on a focus loss (P6-5).** A stale x waits only while the field has no focus, and the next focus gain drops it before the press of that gain writes its own x. The caret pass never runs for a field with no focus. CR-4 adds only a test.
- **Open risk 8: the clear before the measure (P6-6).** A context with no measurer drops the x (`AContextWithNoMeasurerKeepsTheCaret`), and a measurer that throws does not map the same x again.
- **Open risk 9: the stops of the press, the drag, and Enter.** They copy rule A4 of button-and-toggle and the slider rule. The changelog and section 9 state the effect on an ancestor. A capture handler on an ancestor still gets each entry, and `Submitted` replaces an Enter handler of an ancestor.
- **Open risk 11: the cost of `IndexAtX`.** The cost grows as the square of the bytes before the x. The capacity bounds it, the walk stops at the x, and only a press or a drag move runs it. A short field costs little. Follow-up question 2 covers a large capacity.
- **Open risk 12: the measures of each frame.** A focused field makes 1 measure for the caret, 2 for a selection, and 2 in the caret pass. Each other field makes none. Each measure takes a span and allocates nothing (A04, A05 caught).
- **Open risk 13: the rounding of `ImGuiRenderer`.** Probe (g) shows that the caret x equals the measured width of a `Label` with the same font, because one measurer serves both. The rounding up to a whole pixel can put the caret up to 1 pixel off the glyph edge on screen. The plan accepts this risk, and widget-sample checks it on screen.
- **Open risk 14: the extra checks in the tests.** Each test still tests the rule of its name, and each demo test matches its demo line. `APressAndADragSelect` uses `FieldY = 8`, the press at 21, and the move to 37. The middle `Update` of the gate step (P7-1) makes the gate run `IndexAtX` for the anchor and the caret, and plant A08 fails it.
- **The docs (rule 12).** Sections 4, 5, 8, 9, and 14 and the changelog match the code, with the exceptions of CR-13 and CR-14. Each public member has an XML doc comment. Core has no ImGui or GLFW `using`.

---

## Follow-up Questions

1. **A filter in `SetText` (open risk 1).** Must `SetText` and the start text replace a C0 control character with U+FFFD, or remove a line feed, as the typed char path does? CR-13 only documents the effect. `Label` does not filter either, so a filter is a choice for the person.
2. **A large capacity and `IndexAtX` (open risk 11).** A chat field with a capacity of some kilobytes makes one measure for each code point before the x in each drag frame. Does widget-sample measure the cost with `ImGuiRenderer`? A sum of the widths of single code points is cheaper, but it ignores the kerning.
3. **A press at the caret position shows no hidden caret.** A press on the focused field that maps to the current caret changes nothing, so the blink does not restart (M35 shows that the repair branch is the only other place). Desktop text boxes show the caret at each press. Is this a wanted change for a later feature?
4. **Known debt of the epic (Conflicts and Gaps 9).** (a) A press that a capture handler on an ancestor stops keeps the press target, so the field still captures the later moves of that press. The field has the same rule as the slider. (b) `:disabled` on an ancestor does not block the field: the field still accepts the focus and the typing. This is the same rule as for the slider keys, but the effect is larger, because a disabled form still takes text. (c) `RoutingGate.PointerOverUi` follows the hit during a captured drag, so a text selection drag past the field edge can give the pointer to the game. Each item stays debt of the epic. Item (b) is a question for the person: must the stale focus rule read the disabled ancestors?
5. **A removed focus owner in the caret pass.** Probe (b) shows that the caret pass of one more `Update` runs on a field that a handler removed in the drain. The pass changes only the field, so no other element sees it. Must the pass check `IsInRootTree`, or does the one-frame limit of `DropStaleFocus` stay?

---

## Recommended Fix Order

| Order | Finding | Severity | Effort | Fix group (one file) |
|-------|---------|----------|--------|----------------------|
| 1 | CR-1: a hide scrolls the text out of the view | MEDIUM | Low | G1 `TextField.cs` (code), test in `TextFieldScrollTests.cs` |
| 2 | CR-8: no gate for the string and `LString` sets | MEDIUM | Low | G2 `TextFieldAllocationTests.cs` |
| 3 | CR-4: the drop rule of the pending x | MEDIUM | Low | G3 `TextFieldPointerTests.cs` |
| 4 | CR-5: the padding in the text-space x | MEDIUM | Low | G3 `TextFieldPointerTests.cs` |
| 5 | CR-2: `TextChanged` after the insert | MEDIUM | Low | G4 `TextFieldTypeTests.cs` |
| 6 | CR-7: the target rule of the text checks | MEDIUM | Low | G4 `TextFieldTypeTests.cs` |
| 7 | CR-3: the smallest scroll to the right | MEDIUM | Low | G5 `TextFieldScrollTests.cs` |
| 8 | CR-6: the capture of a disabled field | MEDIUM | Low | G6 `TextFieldDisabledTests.cs` |
| 9 | CR-12: a disable after a press | LOW | Low | G1 `TextField.cs` (code), test in `TextFieldDisabledTests.cs` |
| 10 | CR-9: an infinite step in the hidden phase | LOW | Low | G7 `TextFieldBlinkTests.cs` |
| 11 | CR-10: the caret pass before the clear pass | LOW | Low | G7 `TextFieldBlinkTests.cs` |
| 12 | CR-11: the repair branch | LOW | Low | G7 `TextFieldBlinkTests.cs` |
| 13 | CR-14: a 2-byte remove | LOW | Low | G8 `TextFieldDeleteTests.cs` |
| 14 | CR-13: the line feed remark | LOW | Low | G9 `TextField.cs` (docs) |
| 15 | CR-15: the format drift | LOW | Low | G9 `TextField.cs` (docs) |
| 16 | CR-16: the quick status | LOW | Low | G10 `context.md` |

Each test fix extends an existing test method and adds no test method, so the class totals stay. Run the mutation of each finding again after its fix, and expect a failed test.

---

## Progress

- [x] CR-1: A hide or a width of 0 on the focused field scrolls the text out of the view for good (fixed by `review-g1.md`: the scroll rule returns early for a content width below `CaretWidth` or NaN, and `AResizeKeepsTheCaretVisible` hides and shows the field. The mutation fails it (expected 56, actual 96))
- [x] CR-2: No test checks that `TextChanged` comes after the insert of a char (fixed by `review-g4.md`: the delegate of `ACharShowsInTheField` records the text at each call, and the last call reads `Hi`. M07 fails it)
- [x] CR-3: No test guards the smallest scroll for a move right inside a long text (fixed by `review-g5.md`: `AMovePastTheLeftEdgeScrolls` has a right step from Home with 11 Right keys, and reads `TextScroll` 8. M37 fails it (expected 8, actual 32))
- [x] CR-4: No test guards the drop rule of the pending press x (fixed by `review-g3.md`: `APressFocusesTheFieldAndShowsTheCaret` checks a Home key after a press in one drain and a font change after two clicks. M54 and M56 fail it)
- [x] CR-5: No test guards the padding and the border in the text-space x (fixed by `review-g3.md`: `APressInAScrolledFieldReadsTheScroll` has a padded step with a click at x 30 (x 26 maps the same with both boxes). M49 fails it)
- [x] CR-6: No test guards the capture rule of a disabled field (fixed by `review-g6.md`: `APressOnADisabledFieldGivesNoFocus` counts the moves of `Other` after a press on the disabled field. M52 fails it (expected 1, actual 0))
- [x] CR-7: No test guards the target rule of `IsEditKey` and `IsCharInput` (fixed by `review-g4.md`: `ACharWithACallerTargetInserts` pushes a char and a Left key with a focusable child as the target. M17 and M57 fail it)
- [x] CR-8: No gate covers `SetText(string?)` and `SetText(LString?)` (fixed by `review-g2.md`: the input step sets two static strings and one `LString` on a third field. A plant in `SetText(string?)` fails `TheTextFieldInputPathAllocatesNothing` (20000 bytes))
- [x] CR-9: No test applies an infinite time step in the hidden phase (fixed by `review-g7.md`: `ABadDeltaAdvancesNothing` applies an infinite step in the hidden phase. M36 fails it)
- [x] CR-10: No test pins the caret pass before the clear pass (fixed by `review-g7.md`: `TheCaretBlinks` asserts no `Paint` mark after the flip frame. M31 fails it)
- [x] CR-11: No test pins the repair branch of `SetFocusOwner` (no restart) (fixed by `review-g7.md`: `AFocusGainRestartsTheBlink` sets the focus on the focused field and keeps the caret hidden. M35 fails it)
- [x] CR-12: A disable after a press in the same drain still moves the caret (fixed by `review-g1.md`: `AdvanceCaret` moves no caret for a disabled field, and `APressOnADisabledFieldGivesNoFocus` disables the field in the drain of the press. The mutation fails it (expected 5, actual 3))
- [x] CR-13: The remarks do not state the effect of a line feed in a text from code (fixed by `review-g9.md`: the class remarks and the remarks of `SetText(ReadOnlySpan<byte>)` state that a line feed or another control character in a text from code breaks the caret position and the scroll)
- [x] CR-14: No test removes a 2-byte code point (fixed by `review-g8.md`: `BackspaceRemovesAWholeCodePoint` starts with `aé`, and Backspace gives `a` and the caret 1. A one-byte step fails it)
- [x] CR-15: Format drift in `TextField.cs` from the scripted edits (fixed by `review-g9.md`: three long doc lines wrapped and the constructor remark rewritten. Only `///` lines changed. The empty line was already gone after `review-g1.md`)
- [x] CR-16: The quick status of `context.md` is old (fixed by the parent: the quick status of `context.md` names phases 6 and 7, the review, and the next steps)
