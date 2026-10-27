# interactive-elements/text-field - Context & Decisions

**Last Updated:** 2026-10-27 09:46
**Current Phase:** Complete (phases 1 to 7 and the cross-phase review are done)
**Status:** ✅ Ready for Review

---

## Quick Status

**What's Done:**
- ✅ The plan is accepted. The user answered the scope questions: Q1 A, Q2 C (a press places the caret and a drag selects, phase 6), Q3 A, and Q4 A.
- ✅ `/autorun-epic` runs this feature on the branch `feat/interactive-elements/text-field`.
- ✅ Phase 1 (tasks 1.1 to 1.6): a typed char shows in the field. The gate passes.
- ✅ Phase 2 (tasks 2.1 to 2.6): the edit keys move the caret and remove one code point. The focused field draws the caret. The gate passes.
- ✅ Phase 3 (tasks 3.1 to 3.5): Enter calls `Submitted`, and code reads and writes the text. The gate passes.
- ✅ Phase 4 (tasks 4.1 to 4.6): long text scrolls under the caret, and the caret blinks. The gate passes.
- ✅ Phase 5 (tasks 5.1 to 5.4): `Shift` selects, and a char replaces the selection. The gate passes.
- ✅ Phase 6 (tasks 6.1 to 6.4): a press places the caret, and a drag selects. The gate passes.
- ✅ Phase 7 (tasks 7.1 to 7.7): the disabled tests, the allocation gate with the plant, the technical design, and the changelog. The gate passes.
- ✅ The cross-phase review (`code-review.md`): 16 findings (0 HIGH, 8 MEDIUM, 8 LOW). Fix groups `review-g1.md` to `review-g9.md` fix CR-1 to CR-15. This update fixes CR-16.

**What's Next:**
- 📋 A person tests the items of "Needs Human Verification".
- 📋 `/autorun-epic` commits the feature and starts `interactive-elements/widget-sample` (its draft plan exists).
- 📋 A person answers the five follow-up questions of `code-review.md`.

**Blockers:**
- None

---

## Key Files

### Core Implementation
- `src/BrutalUi.Core/Elements/TextField.cs` - the text field widget (new).
- `src/BrutalUi.Core/Elements/WidgetInput.cs` - the input checks `IsCharInput` and `IsEditKey`, and the key codes.
- `src/BrutalUi.Core/Tree/UiContext.cs` - the caret pass in `Update` and the blink restart in `SetFocusOwner` (phase 4).

### Related Files
- `tests/BrutalUi.Core.Tests/Elements/TextField*Tests.cs` - the test classes of each phase.
- `tests/BrutalUi.Glfw.Tests/TextFieldKeyCodeTests.cs` - the key code check against `GlfwKey`.
- `docs/features/interactive-elements/text-field/implementation.md` - the implementation plan.

---

## Phase 1 Baseline

Recorded before any change of phase 1 (task 1.1).

- The base commit: `4414826c8fbd755fe7d7d37549865d12b52f3e7d` (the scroll-view commit). This run makes no commit, so `<base>` means `HEAD`.
- `git ls-files src/BrutalUi.Core/Elements/ScrollView.cs` gives one line. Scroll-view is in the base commit.
- `git status --porcelain -- src tests` gives no line.
- `dotnet build brutal-ui.sln`: 0 warnings and 0 errors.
- `dotnet test --solution brutal-ui.sln`: total 2456, succeeded 2454, skipped 2, failed 0.
- `BrutalUi.Core.Tests`: 1615 passed (the test run). `--list-tests` gives 1624 lines.
- `BrutalUi.Glfw.Tests`: 20 (18 passed, 2 skipped because the machine has no display).
- The regression set (the counts of `--list-tests`): `FocusOwnerTests` 8, `ClickFocusTests` 8, `TabNavigationTests` 12, `FocusStateEdgeTests` 8, `FocusAllocationTests` 3, `RoutingGateTests` 8, `PaintWalkTests` 29, `PaintContentTests` 9, `HitTestTests` 6, `ButtonKeyTests` 9, `ButtonTextTests` 9, `ToggleTests` 10, `WidgetDisabledTests` 7, `WidgetAllocationTests` 4, `SliderKeyTests` 9, `SliderDisabledTests` 6, `SliderAllocationTests` 3, `ScrollViewScrollTests` 10, `ScrollViewSliderTests` 6, `ScrollViewAllocationTests` 3, and `ThousandElementAllocationTests` 14.

### Codebase Findings (checked against the base)

1. The key and char route exists. `UiContext.DrainEvents` gives each `Char` entry with no target to the focus owner. With no owner, the entry gets no walk. A target from the caller keeps its target (`UiContext.PushEvent(in CharEvent)` remarks).
3. The clip comes before the paint hook. `PaintWalk.Enter` pushes the clip of the padding box for an overflow other than `Visible`, and then calls `PaintContent`.
8. `RecordingRenderer.MeasureText` gives `(bytes * size * 0.5, size)` with no wrap. A font size of 16 gives 8 pixels for each byte.
9. `Utf8Text` doubles its buffer, so the field owns its own fixed `byte[]`.
10. `WidgetInput` holds the key codes and the target rule (`IsKeyActivation`, `IsArrowKey`).
11. The analyzer `BRUTALCORE0011` rejects a string literal when a span overload exists. Tests write `"..."u8`.
12. Each widget handler is a static method with no target. It casts `e.Header.CurrentTarget` with `is`.

---

### Phase 1 Gate (task 1.6)

- `dotnet build brutal-ui.sln`: 0 warnings and 0 errors.
- `dotnet test --solution brutal-ui.sln`: total 2466, succeeded 2464, skipped 2, failed 0 (the base plus 10).
- `BrutalUi.Core.Tests`: 1625 (the base 1615 plus the 10 tests of `TextFieldTypeTests`). `BrutalUi.Glfw.Tests`: 20 (18 passed, 2 skipped), no change.
- `TextFieldTypeTests`: 10 passed, 0 failed.
- The regression set: the 21 classes in one run give 181 passed and 0 failed. This count is the sum of the baseline counts.
- `git diff 4414826 --stat -- tests/` gives no line, so no existing test file changed. `UiContext.cs`, `HitWalk.cs`, `PaintWalk.cs`, and `Element.cs` have no diff.
- `scripts/test-linux.sh`: `test-linux: PASS build, test, gpu, smoke (120 frames) in 18 s`, `exit=0`.

### Blast Radius of Phase 1

- `WidgetInput.cs`: one new internal method (`IsCharInput`) and two remark edits. No existing check changed, so no widget changed.
- `TextField.cs`, `TextFieldTree.cs`, and `TextFieldTypeTests.cs` are new files. No shared file changed.

### Phase 2 Gate (task 2.6)

- `dotnet build brutal-ui.sln`: 0 warnings and 0 errors.
- `dotnet test --solution brutal-ui.sln`: total 2485, succeeded 2483, skipped 2, failed 0 (the phase 1 gate 2466 plus 19).
- `BrutalUi.Core.Tests`: 1643 (the phase 1 gate 1625 plus 10 and 8). `BrutalUi.Glfw.Tests`: 21 (19 passed, 2 skipped because the machine has no display).
- `TextFieldCaretTests`: 10 passed. `TextFieldDeleteTests`: 8 passed. `TextFieldKeyCodeTests`: 1 passed. All 19 passed in the first run.
- `TextFieldTypeTests`: 10 passed, with no edit.
- The regression set: the 21 classes in one run give 181 passed and 0 failed, the same as the phase 1 gate.
- `git diff --stat -- tests/` gives no line, so no tracked test file changed. `TextFieldTree.cs` and `TextFieldTypeTests.cs` have no edit.
- The `GlfwKey` names: `Backspace`, `Delete`, `Home`, `End`, `Enter`, `Left`, and `Right`. Each name is the name of the plan, and the test compiles against `Ahwoo.BRUTAL.Glfw` 2026.10.4.

### Blast Radius of Phase 2

- `WidgetInput.cs`: one new internal method (`IsEditKey`), four new internal constants, and one remark edit. No existing check changed, so no other widget changed.
- `TextField.cs`: the constructor now writes `OnKeyDown`. A game that wrote `OnKeyDown` on a field before phase 2 had no field handler to lose. The draw now measures the text before the caret when the field holds the focus. This adds one measure call to each focused frame (`RecordingRenderer.MeasureCount` counts it). No test of a phase 1 class reads that count.
- The new test files: `TextFieldCaretTests.cs` (it also holds the shared helper class `TextFieldDraws`), `TextFieldDeleteTests.cs`, and `TextFieldKeyCodeTests.cs`.

### Phase 3 Gate (task 3.4)

- `dotnet build brutal-ui.sln`: 0 warnings and 0 errors.
- `dotnet test --solution brutal-ui.sln`: total 2501, succeeded 2499, skipped 2, failed 0 (the phase 2 gate 2485 plus 16).
- `BrutalUi.Core.Tests`: 1659 (the phase 2 gate 1643 plus 6 and 10). `BrutalUi.Glfw.Tests`: 21 (19 passed, 2 skipped because the machine has no display), no change.
- `TextFieldSubmitTests`: 6 passed, 0 failed. `TextFieldTextTests`: 10 passed, 0 failed. Both passed in the first run.
- The earlier text field classes, with no edit: `TextFieldTypeTests` 10, `TextFieldCaretTests` 10, and `TextFieldDeleteTests` 8. All passed.
- The regression set: the 21 classes in one run give 181 passed and 0 failed, the same as the phase 2 gate.
- No test file of phase 1 or phase 2 changed. `implementation.md` and `tasks.md` have no edit by this run.

### Phase 3 Rules (task 3.5)

- **The cut rule (Notes Q3).** Each `SetText` overload and the start text copy the longest prefix that fits `Capacity` and ends on a code point boundary. The span overload uses `CutAtBoundary`. The string overload uses `Utf8.FromUtf16`, which writes whole scalars only. `GetText(Span<byte>)` cuts at a boundary of the destination length the same way. No overload throws and none allocates.
- **The `LString` rule.** `SetText(LString?)` reads `Localized` one time and calls the string overload. The field keeps no `LString`, so a later locale change and `RefreshLocalizedText` do not change the field. The game calls `SetText` again to show a new locale.
- **The test counts.** `TextFieldSubmitTests` 6 and `TextFieldTextTests` 10.

### Blast Radius of Phase 3

- `TextField.cs` only. `WidgetInput.cs` has no new edit (the key code `EnterKey` and `IsEditKey` exist). The constructor now calls `EncodeUtf16` (the renamed `EncodeStartText`), and the start text result does not change.
- The Enter branch is new in `HandleKeyDown`. Before phase 3, the main Enter key walked on. A game that reads Enter on an ancestor of a field now gets no Enter from a focused field (the Enter rule, Decision 6).

### Phase 4 Gate (tasks 4.5 and 4.6)

- `dotnet build brutal-ui.sln`: 0 warnings and 0 errors.
- `dotnet test --solution brutal-ui.sln`: total 2516, succeeded 2514, skipped 2, failed 0 (the phase 3 gate 2501 plus 15).
- `BrutalUi.Core.Tests`: 1674 in the solution run (the phase 3 gate 1659 plus 9 and 6). `dotnet run` of the project gives 1683, 0 failed. The difference of 9 is the same as at the baseline (the two runners count some cases in a different way). `BrutalUi.Glfw.Tests`: 21 (19 passed, 2 skipped because the machine has no display), no change.
- `TextFieldScrollTests`: 9 passed. `TextFieldBlinkTests`: 6 passed. Both passed in the first run after one compile fix (a missing `using System;` for `SequenceEqual`).
- The earlier text field classes, with no edit: `TextFieldTypeTests` 10, `TextFieldCaretTests` 10, `TextFieldDeleteTests` 8, `TextFieldSubmitTests` 6, and `TextFieldTextTests` 10. All passed. The phase 2 caret tests run `Update(0)`, so the caret stays shown.
- The regression set: the 21 classes in one run give 181 passed and 0 failed, the same as the phase 3 gate.
- The `tests/BrutalUi.Core.Tests/Input/` folder: the 21 classes in one run give 151 passed and 0 failed.
- `git diff HEAD -- src/BrutalUi.Core/Tree/UiContext.cs`: 24 insertions and 1 deletion. The code lines are the caret pass in `Update` (a comment and the `if (_focusOwner is TextField field)` block after the `finally` block of the layout pass, before the clear pass) and the restart line in the change branch of `SetFocusOwner` (`if (next is TextField field) { field.RestartCaretBlink(); }`). Each other changed line is a doc comment: the frame order, a new paragraph on the caret pass, the `deltaTime` rule of the caret pass, and the restart sentence of `SetFocusOwner`. The one deleted line is the old frame order sentence. `DrainEvents`, `RoutePointer`, `DropStaleFocus`, the focus rules, the measure scope, and `Draw` have no change.
- `git diff HEAD --stat -- tests/` gives no line, so no tracked test file changed. No test file of phases 1 to 3 changed.
- `scripts/test-linux.sh`: `test-linux: PASS build, test, gpu, smoke (120 frames) in 21 s`, `exit=0`. The HelloPanel smoke run holds no field, so the caret pass does not run there.

### Blast Radius of Phase 4

- `UiContext.Update`: each context runs one type test of the focus owner in each frame. Only a focused `TextField` gets the caret pass: two measure calls on `_measurer` and at most one `Paint` mark. The pass reads `_measurer`, not `t_measurer`, so the measure scope of the layout pass does not change.
- `UiContext.SetFocusOwner`: one type test in the change branch. The repair branch has no change. Each focus path (`SetFocus`, `ClearFocus`, `FocusNext`, `FocusPrevious`, the click focus, Tab, and the stale drop) goes through this method, so each focus gain of a field restarts the blink.
- `TextField.PaintContent`: the text and the caret move left by `TextScroll`, and the caret draws only in the shown phase of the blink. A game that calls `Update` with a time step above 0 now sees a blinking caret.
- An `ImGuiRenderer` measurer with no current ImGui context throws in the caret pass, after the layout pass and before the clear pass (Risk 10 of the plan). The next `Update` clears the bits.

### Phase 5 Gate (task 5.3)

- `dotnet build brutal-ui.sln`: 0 warnings and 0 errors.
- `dotnet test --solution brutal-ui.sln`: total 2526, succeeded 2524, skipped 2, failed 0 (the phase 4 gate 2516 plus 10). The first run had 1 failed test, `TransitionCurveTests.TheCurvesAllocateNothing` (`ease-in-out allocated 7976 bytes`). Three more runs gave 0 failed. See the gotcha on this flake.
- `BrutalUi.Core.Tests`: 1684 in the solution run (the phase 4 gate 1674 plus 10). `dotnet run` of the project gives 1693, 0 failed. The difference of 9 is the same as at the baseline. `BrutalUi.Glfw.Tests`: 21, no change.
- `TextFieldSelectionTests`: 10 passed, 0 failed, in the first run.
- The earlier text field classes, with no edit: `TextFieldTypeTests` 10, `TextFieldCaretTests` 10, `TextFieldDeleteTests` 8, `TextFieldSubmitTests` 6, `TextFieldTextTests` 10, `TextFieldScrollTests` 9, and `TextFieldBlinkTests` 6. All passed.
- The regression set: the 21 classes in one run give 181 passed and 0 failed, the same as the phase 4 gate.
- `git diff HEAD --stat -- tests/` gives no line. No test file of phases 1 to 4 changed. `implementation.md` and `tasks.md` have no edit by this run.

### Blast Radius of Phase 5

- `TextField.cs` only. `WidgetInput.cs` and `UiContext.cs` have no new edit. `IsEditKey` already passes `Shift`.
- `MoveCaret` has the new parameter `extend`. Each caller is in `HandleKeyDown`. Phase 6 calls it from the caret pass.
- `HandleKeyDown`: Left, Right, Home, and End read `Shift` now. Before phase 5, `Shift` with Left moved the caret as Left did. Now it extends the selection. The phase 2 tests send no `Shift`, so their result does not change.
- `HandleChar`: the fit check now uses the length minus the selection. With no selection, the check is the same as before.
- `PaintContent`: a focused field with a selection makes two more measure calls and one more rect call in each frame. A field with no selection or with no focus draws as before. `RecordingRenderer.MeasureCount` counts the new measure calls. No earlier test reads that count for a field with a selection.
- `WriteLength`, `Insert`, `Remove`, and the constructor write the anchor. Each earlier path that wrote the caret now also writes the anchor, so the anchor never sits after `_length`.

### Phase 6 Gate (task 6.3)

- `dotnet build brutal-ui.sln`: 0 warnings and 0 errors.
- `dotnet test --solution brutal-ui.sln`: total 2536, succeeded 2534, skipped 2, failed 0 (the phase 5 gate 2526 plus 10). The first run had no failure, so no flake run is on record for this phase.
- `BrutalUi.Core.Tests`: 1694 in the solution run (the phase 5 gate 1684 plus 10). `dotnet run` of the project gives 1703, 0 failed. The difference of 9 is the same as at the baseline. `BrutalUi.Glfw.Tests`: 21 (19 passed and 2 skipped with no display), no change.
- `TextFieldPointerTests`: 10 passed, 0 failed, in the first run. The demo test is `APressAndADragSelect`.
- The earlier text field classes, with no edit: `TextFieldTypeTests` 10, `TextFieldCaretTests` 10, `TextFieldDeleteTests` 8, `TextFieldTextTests` 10, `TextFieldBlinkTests` 6, `TextFieldScrollTests` 9, `TextFieldSelectionTests` 10, and `TextFieldSubmitTests` 6. The 8 classes in one run give 69 passed and 0 failed.
- The drag classes, with no edit: `SliderDragTests` 9, `ScrollViewDragTests` 9, and `DragCaptureTests` 9. All passed.
- The regression set: the 21 classes in one run give 181 passed and 0 failed, the same as the phase 5 gate.
- This run wrote one new test file only. It edited no test file of phases 1 to 5. `implementation.md` and `tasks.md` have no edit by this run.

### Blast Radius of Phase 6

- `TextField.cs` only. `UiContext.cs`, `WidgetInput.cs`, `IDragCapture.cs`, `HitWalk.cs`, `PaintWalk.cs`, `Slider.cs`, and `ScrollView.cs` have no edit.
- The constructor writes `OnPointerDown` and `OnPointerMove`. A left press on a field and each drag move of the field now stop the walk. Before phase 6, a press on a field bubbled to the ancestors. A bubble handler on an ancestor thus gets no left press on a field now. A capture handler still gets it. A right press and a chord still walk on.
- The click focus does not change. `FocusOnPress` runs in the route step, before the walk, so the field gets the focus before its press handler runs.
- `IDragCapture`: the drain now sends each move with no target of a left drag on a field to the field. `Hover`, `PointerHit`, and the routing gate still follow the hit (Known debt 9).
- `RestartCaretBlink` now also drops the pending x. Its callers are `MoveCaret`, `Insert`, `Remove`, `WriteLength`, and the focus gain in `UiContext.SetFocusOwner`. Each caller already restarted the blink, so no caller changes its result.
- `AdvanceCaret`: the scroll rule moved, with no change, into the private `ApplyScrollRule(IUiRenderer)`. The pending map runs before it. With no pending x, the pass makes the same measure calls as before. The scroll tests and the blink tests pass with no edit.

### Phase 7 Gate (tasks 7.1 to 7.7)

- `dotnet build brutal-ui.sln`: 0 warnings and 0 errors.
- `dotnet test --solution brutal-ui.sln`: total 2544, succeeded 2542, skipped 2, failed 0 (the phase 6 gate 2536 plus 8). The first run had no failure, so no flake run is on record for this phase.
- `BrutalUi.Core.Tests`: 1702 in the solution run (the phase 6 gate 1694 plus 8). The project run (`dotnet run`) gives 1711 (the same difference of 9). `BrutalUi.Glfw.Tests`: 21 (19 passed, 2 skipped with no display), no change. Against the baseline of task 1.1: total 2456 to 2544 (plus 88) and Core 1615 to 1702 (plus 87 in the solution run, the 87 test cases of the 11 Core text field classes: 10, 10, 8, 6, 10, 9, 6, 10, 10, 5, and 3 tests, which sum to 87), and Glfw 20 to 21 (plus `TextFieldKeyCodeTests`).
- `TextFieldDisabledTests`: 5 passed. `TextFieldAllocationTests`: 3 passed. Both passed in the first run, with `failed: 0`.
- The earlier text field classes, with no edit, one run for each class: `TextFieldTypeTests` 10, `TextFieldCaretTests` 10, `TextFieldDeleteTests` 8, `TextFieldSubmitTests` 6, `TextFieldTextTests` 10, `TextFieldScrollTests` 9, `TextFieldBlinkTests` 6, `TextFieldSelectionTests` 10, and `TextFieldPointerTests` 10. All have `failed: 0`.
- The plant (task 7.3): `GC.KeepAlive(new object());` as the first line of `TextField.HandleChar`. `dotnet test --solution brutal-ui.sln > /dev/null 2>&1; echo "exit=$?"` gave `exit=2`. A second run with the output read gave total 2544 and failed 1: `BrutalUi.Core.Tests.TextFieldAllocationTests.TheTextFieldInputPathAllocatesNothing`. The other two gates (`UpdateAndDrawAllocateNothingWithTextFields` and `TheMeasuredBodyDoesRealWork`) passed, because the char path runs in the input test only. The restore ran in a later tool call from `scratchpad/TextField.cs.orig`, and `cmp` found no difference. A search for `KeepAlive` in `TextField.cs` gives 0 lines.
- The STE lint (`ste-lint.py --fail-over 2.5`) exits 0 for each doc. The scores in matches per 100 words: `docs/technical-design.md` 1.53, `CHANGELOG.md` 1.62, and `implementation.md` 1.79. No sentence needed a fix, and `implementation.md` has no edit.
- `scripts/test-linux.sh`: `test-linux: PASS build, test, gpu, smoke (120 frames) in 12 s`, `exit=0`.
- The files of this phase: `TextFieldDisabledTests.cs` and `TextFieldAllocationTests.cs` (new), `docs/technical-design.md` (sections 4, 5, 8, 9, and 14), `CHANGELOG.md`, and this file. `TextField.cs` has no diff against its copy. `tasks.md`, `implementation.md`, and the test files of phases 1 to 6 have no edit by this run.

---

## Important Decisions

The plan holds Decisions 1 to 16. This section records each decision that the plan did not settle.

- **P1-1. One renderer for each test tree.** The brief says that `Frame()` draws "into the tree renderer". `TextFieldTree` holds one `RecordingRenderer` (`Renderer`). It is the measurer of the context and the target of `Frame()`. Thus `Frame()` allocates no renderer, and the allocation tests of phase 7 can call it. The renderer holds the calls of the last frame only.
- **P1-2. The empty text and the alpha guard only the text call.** `PaintContent` returns early only for a content box of 0. The checks for an empty text and an alpha of 0 guard the `Text` call only, so phase 2 can add the caret rect after the text with no change of the early return.
- **P1-3. The start text is not filtered.** The constructor encodes the start text with `Utf8.FromUtf16` and `replaceInvalidSequences: true`, so an invalid surrogate becomes U+FFFD (as `Utf8Text.Utf8Equals` does). A control character or a line feed in the start text stays in the buffer. The plan filters the char path only. See the open question Q-P1-1.
- **P1-4. `Insert` returns `false` for an empty span.** No change and no call. Phase 1 has no path that sends an empty span.
- **P1-5. The scalar check uses `Rune.TryCreate(uint, out Rune)`.** It is the same rule as `Rune.IsValid(uint)`, and it gives the rune for `Rune.IsControl` and `EncodeToUtf8` in one step.
- **P1-6. The capacity message.** The constructor throws `ArgumentOutOfRangeException("info", capacity, "CreateInfo.Capacity must be above 0.")`, in the form of the slider messages.
- **P1-7. The old remark of `WidgetInput`.** The class remarks said that a widget with a char rule "writes its own check". That is false after `IsCharInput`, so the sentence now says that such a widget uses the check of its rule. The edit is a doc change only.
- **P2-1. `MoveCaret` has no `extend` parameter yet.** The plan gives `MoveCaret(int index, bool extend)`. Phase 2 has no anchor and reads no Shift, so an unused parameter has no use. Phase 2 writes `MoveCaret(int index)`. Phase 5 adds `extend` and the anchor.
- **P2-2. A move to the same index marks nothing.** `MoveCaret` returns at once when the index is the caret. Thus a Left at 0 and a Right at the end mark no `Paint`. The key still stops the walk.
- **P2-3. `Remove` moves the caret to the start of the range, and the handler calls `TextChanged`.** `Remove(int start, int end)` returns nothing. The handler checks `_caret > 0` (Backspace) or `_caret < _length` (Delete) first, so each `Remove` call changes the bytes, and the handler then calls `TextChanged` one time. Phase 5 must also set the anchor in `Remove`.
- **P2-4. The caret uses the alpha guard of the text.** Step 5 of The Draw names no alpha check. The draw skips the caret rect when the scaled color has an alpha of 0, as it skips the text. Phase 4 adds the blink check to the same condition.
- **P2-5. The caret measure uses the draw renderer.** `PaintContent` calls `IUiRenderer.MeasureText` on the renderer of the draw, not on the measurer of the context. In the ImGui path and in `TextFieldTree`, one object is both. The measure takes `Text[.._caret]`, so an empty text gives a caret of the line height (both measurers give the size for an empty span).
- **P2-6. The boundary steps clamp the index.** `NextBoundary` gives the length for an index at or after the end. `PreviousBoundary` gives 0 for an index at or below 0, and it clamps an index after the end to the length first. Each step is `Math.Max(1, bytesConsumed)`.
- **P2-7. The shared draw search helpers.** The brief forbids an edit of `TextFieldTree.cs` and `TextFieldTypeTests.cs`, and its file list has no new helper file. The caret and delete tests share the internal static class `TextFieldDraws`, which lives at the end of `TextFieldCaretTests.cs`. It copies the clip, text, and pop searches of `TextFieldTypeTests` and adds a rect count. `TextFieldTree` has no fill, so each rect call of a frame is a caret rect.
- **P2-8. Two extra checks in the tests.** `NothingToRemoveCallsNothing` also runs Backspace and Delete on an empty field. `BackspaceRemovesTheCodePointBeforeTheCaret` also checks the sender and the text that the delegate reads. `TheCaretDrawsOnlyWithFocus` also checks that `ClearFocus` removes the caret.
- **P2-9. The class remark of `WidgetInput`.** The paragraph on `IsCharInput` now names `IsEditKey` too. The edit is a doc change only.
- **P3-1. Q-P1-1 is answered: code does not filter.** `SetText` and the start text keep a control character or a line feed (Decision 3, alternative (c), and `Label`). Only the typed char path filters. The remarks of `SetText(ReadOnlySpan<byte>)`, `SetText(string?)`, and the class state the rule.
- **P3-2. The string overload does not use `Utf8Text.Utf8Equals`.** The plan says to reuse it. `Utf8Equals` compares the whole encoded string, so a string above the capacity never equals the cut `Text`, and the same long string would mark `Paint` at each call. `TextField.EqualsText` uses the same chunk idea (a stack chunk of 64 bytes, `Utf8.FromUtf16` with `replaceInvalidSequences: true`) but limits each chunk to the free room of the capacity. Thus the compare is on the cut form, and the same long text marks nothing. `ASetPastTheCapacityCutsAtACodePoint` checks it.
- **P3-3. `CutAtBoundary` steps from the start.** It walks with `NextBoundary` from index 0 to the last boundary at or below `maxBytes`. A look-back of 3 bytes could disagree with the caret steps after a stray continuation byte. The cost is at most `maxBytes` steps, so the capacity bounds it. It returns early for `maxBytes <= 0` (0) and for `maxBytes >= text.Length` (the length).
- **P3-4. The same text keeps the caret.** A set to the same bytes returns before any write, so the caret does not move to the end and nothing is marked (the `Button` shape). A set that changes the bytes moves the caret to the end. `WriteLength` is the one place for this write. Phase 4 adds the scroll 0 and the blink restart there. Phase 5 adds the anchor.
- **P3-5. The constructor and `SetText(string?)` share `EncodeUtf16`.** The helper is the renamed `EncodeStartText` with a `ReadOnlySpan<char>` parameter. The start text path is the same as before.
- **P3-6. Enter code.** The Enter branch returns at once for `IsRepeat`, so the repeat walks on. Else it stops the walk and calls `Submitted`. `IsEditKey` already rejects a Disabled field, a target other than the field, and a `Control`, `Alt`, or `Super` modifier. The keypad Enter key (335) is another code, so it takes the default branch.
- **P3-7. Two extra checks in the tests.** `SetTextEncodesAString` also checks the caret and the mark. `TheSameTextMarksNothing` also checks that the caret stays and that a changed text moves it to the end. `SetTextReadsTheLocalizedText` also checks `RefreshLocalizedText` and the null `LString`. `GetTextCopiesTheText` also checks an empty destination.
- **P4-1. `RestartCaretBlink` marks `Paint` only for a hidden caret.** The method sets `_blink` to 0. It marks `Paint` only when the caret was in the hidden phase, because then the shown caret changes (Edit Rule 5). Each edit path marks `Paint` for its own change, so a second mark adds nothing.
- **P4-2. A key that changes nothing restarts nothing.** Edit Rule 6 names "each change of the text or the caret". `MoveCaret` returns before the restart for the same index (P2-2), so Left at 0 and Right at the end keep the timer. A Backspace in an empty field and a Delete at the end also keep it. The class remarks state this.
- **P4-3. The same text keeps the scroll.** `SetText` with the same bytes returns before `WriteLength` (P3-4), so the scroll, the caret, and the timer stay. `WriteLength` writes `_scroll = 0` and restarts the blink. It is the only write of a new text from code.
- **P4-4. An infinite time step advances no blink.** The plan gives "finite and > 0" for the caret pass, and the transition pass ends each tween for an infinite step. The `deltaTime` doc of `Update` states both rules: the caret pass follows the transition pass for 0 or less and `NaN`, and an infinite step also advances no blink.
- **P4-5. The blink math.** `_blink = (_blink + deltaTime) % (2 * CaretBlinkInterval)` with the `float` remainder. The test steps are exact: 0.5 + 0.5 gives 1, and 1 % 1 gives 0. A result that is not finite or below 0 gives 0, as a guard. A step of exactly one cycle thus keeps the phase and marks nothing.
- **P4-6. The scroll clamp is written by hand.** `Math.Clamp` passes a `NaN` bound through. The pass applies the upper bound with `if (scroll > max)` and then maps a value that is not finite, or below 0, to 0. A measurer that gives `NaN` thus gives the scroll 0.
- **P4-7. A content width below `CaretWidth`.** The scroll rule returns early and keeps the scroll when the content width is below `CaretWidth` or is `NaN` (CR-1). The style pass gives a hidden field a rect of 0, and the stale focus rule drops the owner only at the next drain. Thus the caret pass of a hide frame sees a width of 0. Without the early return, the rule writes `_scroll = caretX`, and no later pass corrects it, because the field loses the focus. The draw returns early for a content width of 0, so the old scroll is the right state to keep. A show then draws the text with the old scroll.
- **P4-8. The marks of the caret pass.** The caret pass runs before the clear pass, so the clear pass of the same `Update` clears its `Paint` mark. The plan asks for the mark (Edit Rule 5), and `Draw` draws each frame, so no reader misses a change. A future dirty-paint cache must read the mark before the clear pass. See the open question Q-P4-1.
- **P4-9. The focus gain test.** `AFocusGainRestartsTheBlink` hides the caret, calls `ClearFocus` and `SetFocus(Field)`, and checks a caret rect. For the Tab part, it hides the caret again, calls `SetFocus(Other)`, runs one frame (the field keeps the hidden phase, because the pass does not run for it), and then pushes a Tab. The Tab wraps from `Other` to `Field`.
- **P4-10. Extra checks in the tests.** `SetTextAndTheFocusOwnerRule` also checks that a resize of a field that is not the focus owner keeps the old scroll (Risk "old scroll" of the plan), and that a `SetText` on such a field gives 0, which an `Update` keeps. `ABadDeltaAdvancesNothing` also checks that the timer runs after the bad steps. `AResizeKeepsTheCaretVisible` also checks the new width of 41. `AnEditRestartsTheBlink` and `ACaretMoveRestartsTheBlink` also check the caret rect.
- **P4-11. A private `Down` helper in `TextFieldScrollTests`.** The helper of `TextFieldCaretTests` is private, and the brief forbids an edit of that file. The new class holds a copy. `TextFieldBlinkTests` calls `PointerTree.Key` directly.
- **P5-1. The collapse rule.** Left with a selection and no `Shift` puts the caret at the start of the selection, and Right puts it at the end, with no further step. Home and End with no `Shift` put the caret at 0 or at the end. In each move with no `Shift`, the anchor follows the caret. `Shift` with a key keeps the anchor and steps the caret from the caret, not from the selection edge, so the selection can flip over the anchor.
- **P5-2. A selection change counts as a caret move.** `MoveCaret(index, extend)` returns at once only when neither the caret nor the anchor changes. Each other move restarts the blink and marks `Paint`. Thus a collapse with no caret step (Left when the caret is at the start of the selection) also restarts the blink, by the first sentence of the brief rule. `Shift` with Left at 0 and `Shift` with Right at the end change nothing and mark nothing (P2-2 still holds).
- **P5-3. The selection color.** The selection rect uses `Style.Color.ScaleAlpha(opacity * SelectionAlpha)`, with `SelectionAlpha = 0.35f` (Conflicts and Gaps item 6). The rect has no radius, no border, and a transparent border color. The selection has its own alpha guard: a scaled alpha of 0 draws no selection rect. A color with an alpha of 1 thus draws the text and the caret but no selection (1 x 0.35 rounds to 0).
- **P5-4. The char replace path.** `HandleChar` does the fit check with `_length - selectionLength + runeLength` before each change. A char that fits calls `Remove(start, end)` for a selection and then `Insert`, and it calls `TextChanged` one time. `Insert` keeps its own fit check as a guard, and that check always passes after the remove. Each of the two steps restarts the blink and marks `Paint`, so a replace marks two times. The second mark adds nothing.
- **P5-5. The same text keeps the selection.** `SetText` with the same bytes returns before `WriteLength` (P3-4), so the caret and the anchor stay, and a selection stays. A set that changes the bytes puts both at the end. The remarks of the class and of `SetText` state this.
- **P5-6. The selection does not blink.** The selection rect draws in each frame with the focus, also when the caret is in the hidden phase of the blink. Only the caret reads `CaretVisible`.
- **P5-7. The line height of the selection.** The draw measures `Text[..start]` for the x of the start and `Text[..end]` for the x of the end and the height. The height is the `Y` of the second measure, so both measurers give the font size for one line. A selection from 0 makes a measure of an empty span, which gives the x 0.
- **P5-8. `Shift` with Backspace, Delete, and Enter.** `IsEditKey` passes `Shift`, so `Shift` with Backspace or Delete acts as the key with no `Shift`. `Shift` with Enter still calls `Submitted`, and Enter does not change the selection.
- **P5-9. The test helpers of the selection tests.** `TextFieldDraws.IndexOfOnlyRect` fails for two rects, and a focused field with a selection draws two rects. `TextFieldSelectionTests` holds a private `Down(int key, UiModifiers modifiers)` and two private color searches (`IndexOfRectWithColor` and `CountRectsWithColor`). The searches tell the selection rect from the caret rect by the alpha. The class also calls the shared `TextFieldDraws` searches with no edit of that file.
- **P5-10. Extra checks in the tests.** `LeftAndRightCollapseTheSelection` also checks the `Paint` mark of a collapse with no caret step, in both directions of the selection. `ADroppedCharKeepsTheSelection` also checks that the root bubble handler gets no char. `TheSelectionDrawsBehindTheTextOnlyWithFocus` also checks that the selection stays after the focus loss and draws again at the next focus gain (Conflicts and Gaps item 8). `BackspaceAndDeleteRemoveTheSelection` puts the caret at the start of the selection for Backspace and at the end for Delete. `SetTextClearsTheSelection` also checks that the set calls nothing.
- **P6-1. The pending map uses two x values, not an x and a flag.** The plan names `_pendingX` and `_pendingExtend`. One x and one flag lose the press point when the press and a drag move come in one drain, and the demo pushes both before one `Update`. The field thus holds `_pendingX` (the caret x) and `_pendingAnchorX` (the anchor x). Both are in text space, and `float.NaN` means "nothing waits". A press with no `Shift` writes both. A press with `Shift` writes `_pendingX` only. A drag move writes `_pendingX` only. The caret pass maps `_pendingAnchorX` first with `MoveCaret(index, false)`, then `_pendingX` with `MoveCaret(index, true)`. A press, a release, and a `Shift` press in one drain thus select from the first press to the second press. The fields are private, so no public shape changes.
- **P6-2. The tie rule and the ends.** `IndexAtX` steps from 0 with `NextBoundary` and measures each prefix with no wrap. It stops at the first boundary with a width at or past `x` and compares it with the boundary before it: `x - width <= nextWidth - x` gives the lower index, so a tie goes to the lower index. An `x` at or below 0, and `NaN`, give 0. An `x` past the text gives the length. The cost is one measure for each code point up to `x`, so the capacity limits it.
- **P6-3. The Shift rule.** A left press with `Shift` keeps the anchor and moves the caret, so it extends the selection from the old anchor. The field reads `Shift` on the press only. A drag move always extends, so the field reads no modifier on a move. A `Shift` press on a field without the focus extends from the anchor that the field kept at the focus loss (P5-5 keeps the selection).
- **P6-4. `IndexAtX` takes a `FontHandle`.** The plan writes `string? font`. `IUiRenderer.MeasureText` and `ComputedStyle.Font` use `FontHandle`, so the signature is `IndexAtX(ReadOnlySpan<byte> text, float x, IUiRenderer measurer, FontHandle font, float fontSize)`.
- **P6-5. The drop rule: the later input wins.** `RestartCaretBlink` drops both pending values. Thus a char, an edit key, a caret move, a `SetText` that changes the text, and a focus gain drop a press of the same drain. A press x is in the text space of the old text, so a later edit would map it to a wrong place. The click focus runs before the walk of the press, so the focus gain of a press does not drop the x of that press. A press that loses the focus later in the same drain leaves a stale x. The next focus gain drops it, so a Tab into the field never maps an old press. A key that changes nothing (Left at 0) restarts nothing and keeps the pending x.
- **P6-6. The clear comes before the measure.** `AdvanceCaret` reads both pending values into locals and clears them after the blink step, before the null-measurer return and before the first measure. A context with no measurer thus drops them (the brief rule), and a measurer that throws does not map the same x in each later pass. The scroll rule moved into the private `ApplyScrollRule(IUiRenderer)` with no change of code.
- **P6-7. The text space x.** `TextSpaceX` gives the pointer x minus the x of the content box plus `_scroll`. The content box comes from `WidgetInput.ViewportOrigin(this)`, the size of `Layout.Rect`, and `Layout.ContentBox`, as in `Slider`. The handler reads the rect and the scroll of the last update, so it uses the frame that the player saw. A pointer x of `NaN` stores `NaN`, so the press places nothing. An infinite x gives an end.
- **P6-8. The press and each drag move stop the walk.** This copies the slider rule (A4 of button-and-toggle): the field uses the event. A move with no drag, a right press, and a chord walk on. The remarks name the effect on a bubble handler of an ancestor.
- **P6-9. A disable after the press in the same drain.** The caret pass reads `Disabled` (CR-12). It clears both pending values for each field, and it maps them only for a field with no `Disabled` state. A press that a field takes while it is enabled thus does not move the caret or the anchor when a later handler of the same drain disables the field. The scroll rule still runs. The next drain drops the focus by the stale focus rule. `IsDragStart`, `IsDragMove`, and `CapturesMoves` block each later press and move of a disabled field.
- **P6-10. The test helpers of the pointer tests.** The selection searches of `TextFieldSelectionTests` are private, so `TextFieldPointerTests` holds its own copy of `IndexOfRectWithColor` and `SelectionColor`. A `Shift` press is `PointerTree.Down(p, PointerButton.Left) with { Modifiers = UiModifiers.Shift }`, because `PointerTree.Pointer` sets no modifier. Each press has its release, so no test leaves a button down. `AContextWithNoMeasurerKeepsTheCaret` also pushes a move and also checks the focus and the scroll. `ARightPressAndAChordPlaceNothing` also pushes a move during the chord.

- **P7-1. The input step has a middle `Update`.** The plan lists the press, the drag, and the release after End. The press x maps in the caret pass, and the drain runs inside `Update`. An edit key after the press drops the stored x (P6-5), and the text is empty after the second Delete. A press at the end of the step would thus measure nothing. The step pushes the press, the drag move, and the release right after Delete (the text is `seeda`), and it runs one `Update(0.016f)` there. Home, Shift+End, Delete, End, Enter, and the char past the capacity follow. The order of the other steps is the plan order. The gate thus measures `IndexAtX` for the anchor and for the caret.
- **P7-2. The counts of one step.** `TextChanged` runs 7 times: the three chars, the char that replaces the selection, Backspace, Delete, and the Delete of the selection. `Submitted` runs 1 time. The first field ends empty, so the frame draws 19 text calls (the 20 fields minus the empty one). The disabled field draws its text in the `:disabled` color. The rect count is 20 for the field fills plus 1 when `CaretVisible` is `true`. After the edits the blink restarts, so the real-work test finds the caret shown, and it reads `CaretVisible` as the brief says.
- **P7-3. The char past the capacity goes to the second field.** The step calls `SetText` with 32 bytes on the second field, then it pushes the char with that field as the caller target. `SetText` with the same text marks nothing after the first step. The real-work test checks that the field keeps its 32 bytes and that the third field keeps `field`. All 20 fields share one `TextChanged` delegate, so a wrongly accepted char would change the count of 7.
- **P7-4. The docs describe the code as built.** Section 5 names `_pendingX` and `_pendingAnchorX` (P6-1), `IndexAtX` with a `FontHandle` (P6-4), and the private `EqualsText` (P3-2). The paragraph states that a press and each drag move stop the walk (P6-8) and that a text from code keeps a control character (P3-1). The sentence of rule 5 that said a widget with a char rule writes its own check stays in the form "a widget with another rule", because `IsCharInput` and `IsEditKey` now exist (P1-7).
- **P7-5. The `Changed` entry of the changelog.** The entry names the caret pass, the focus gain restart, the stop of a press and a drag move on a field, the capture of a move, and the Enter key that no longer reaches an ancestor. The last three are behavior changes of a field only, and no other element changes.
- **P7-6. The plant ran two times.** The first run is the exact command of the brief (exit code only). A second run printed the name of the failed test, to record the gate. The restore came after both runs, in a later tool call.

### Review Fixes

- **CR-1 (fixed).** `ApplyScrollRule` returns early for a content width below `CaretWidth` or `NaN`, so a hide or a width of 0 keeps the scroll. `AResizeKeepsTheCaretVisible` hides the field, drops the focus, shows it, and checks the scroll 56. Mutation: no early return gives the scroll 96, and the test fails.
- **CR-12 (fixed).** `AdvanceCaret` maps the pending values only for a field with no `Disabled` state. `APressOnADisabledFieldGivesNoFocus` disables the field in a root release handler of the press drain and checks the caret and the anchor 5. Mutation: no `Disabled` check gives the caret 3, and the test fails.
- **CR-4 (fixed).** `APressFocusesTheFieldAndShowsTheCaret` pushes a press, its release, and a Home key in one drain and checks the caret 0, then clicks x 16 two times, sets `Inline.FontSize = 32`, and checks the caret 2. Mutation M56 (no `DropPending()` in `RestartCaretBlink`) gives the caret 3, and M54 (no clear in `AdvanceCaret`) gives the caret 1. The test fails for both.
- **CR-5 (fixed).** `APressInAScrolledFieldReadsTheScroll` adds a padded step (width 91, height 26, padding 4, border 1, text `hello`) and clicks at x 30, not x 26 of the finding: x 26 gives the caret 3 for the content box and for the border box, so it cannot tell them apart. x 30 gives 3 for the content box and 4 for the border box. Mutation M49 (the border box in `TextSpaceX`) gives 4, and the test fails.
- **CR-8 (fixed).** `InputStep` of `TextFieldAllocationTests` sets two static strings and one `LString` (built in the `Scene`) on `Fields[3]` in the order A, A, B, L, L, so the compare and the encode of both overloads run. The test adds no method, so the class total stays 3. Mutation A06 (a 16-byte array at the start of `SetText(string?)`) gives 20000 bytes, and `TheTextFieldInputPathAllocatesNothing` fails.
- **CR-2 (fixed).** `ACharShowsInTheField` records `Encoding.UTF8.GetString(f.Text)` in the delegate and asserts `"Hi"` for the last call. The test adds no method. Mutation M07 (`TextChanged` before `Insert` in `HandleChar`) fails `ACharShowsInTheField`.
- **CR-7 (fixed).** `ACharWithACallerTargetInserts` adds a focusable child to `Field`, pushes the char `z` and a Left key with the child as the target, and asserts that the text, the caret, and the `TextChanged` count stay and that a root bubble handler gets both entries. The test adds no method. Mutations M17 (`IsEditKey` with no target check) and M57 (`IsCharInput` with no target check) both fail `ACharWithACallerTargetInserts`.
- **CR-3 (fixed).** `AMovePastTheLeftEdgeScrolls` sets 14 chars `a` with `SetText`, checks the scroll 32, runs a frame after Home, pushes 11 Right keys, and asserts the caret 11, the scroll 8, and the caret rect (80, 0, 1, 16). The brief names one frame for Home and the 11 Right keys, but then the caret at x 88 stays inside the view of the scroll 32 and the scroll stays 32. The extra frame after Home avoids this. The test adds no method. Mutation M37 (`caretX + (width - CaretWidth)`) gives the scroll 32, and `AMovePastTheLeftEdgeScrolls` fails.
- **CR-6 (fixed).** `APressOnADisabledFieldGivesNoFocus` counts the `OnPointerMove` calls of `Other`, adds a move to `TextFieldTree.OverOther` before the release, and asserts 1 call. The test adds no method. Mutation M52 (`CapturesMoves` always `true`) gives 0 calls, and the test fails.
- **CR-9 (fixed).** `ABadDeltaAdvancesNothing` applies `float.PositiveInfinity` after the last step (the caret hidden) and asserts that `CaretVisible` stays `false` and that the frame draws no rect. The test adds no method. Mutation M36 (no `float.IsFinite(deltaTime)` term in `AdvanceCaret`) fails `ABadDeltaAdvancesNothing`.
- **CR-10 (fixed).** `TheCaretBlinks` asserts `tree.Field.Dirty & DirtyFlags.Paint` is `DirtyFlags.None` after the frame with `0.5f` (the flip). The test adds no method. Mutation M31 (the caret pass after the clear pass in `UiContext.Update`) fails `TheCaretBlinks`.
- **CR-11 (fixed).** `AFocusGainRestartsTheBlink` hides the caret again, calls `SetFocus(Field)` while the field holds the focus, runs a frame with `0f`, and asserts that `CaretVisible` stays `false`. The test adds no method. Mutation M35 (`RestartCaretBlink` in the repair branch of `SetFocusOwner`) fails `AFocusGainRestartsTheBlink`.
- **CR-14 (fixed).** `BackspaceRemovesAWholeCodePoint` starts with a field `aé` (3 bytes), presses Backspace, and asserts the text `a` and the caret 1. The test adds no method. Mutation (`PreviousBoundary` steps `end - 1`) fails `BackspaceRemovesAWholeCodePoint`.
- **CR-13 (fixed).** The remarks of the class and of `SetText(ReadOnlySpan<byte>)` now state that a line feed or another control character in a text from code breaks the caret position and the scroll, and that the game removes it before the call. Doc comments only, so no mutation applies. The word diff of the copy and the file shows comment lines only.
- **CR-15 (fixed).** The long doc lines of the class remarks, the `SetText(string?)` remarks, and `Insert` wrap at 120 characters. The constructor remark now reads "...writes the four handlers (char, key down, press, and move). It calls no `TextChanged`." The empty first line of `ApplyScrollRule` was already gone, so no edit was needed. The word diff shows comment lines only.

---

## Gotchas & Learnings

- From button-and-toggle: a stop inside a slot also skips the later delegates of the same slot (`EventWalk.InvokeSlot`).
- From button-and-toggle: do not run a planted allocation and its restore in one parallel tool block.
- From slider: a `float` map of a pixel center can be 1 ulp off. Compute a map in `double` when a test asserts an exact value.
- From scroll-view: `dotnet test --solution brutal-ui.sln --filter-class '*<Class>'` prints `error: 4` and exit code 8 with 0 failed tests. Use `dotnet run --project tests/BrutalUi.Core.Tests/BrutalUi.Core.Tests.csproj -- --filter-class '*<Class>'` for one class.
- From text-field phase 5: `TransitionCurveTests.TheCurvesAllocateNothing` can fail one time in a solution run (`ease-in-out allocated 7976 bytes`). The test reads `GC.GetAllocatedBytesForCurrentThread` around a loop of 10000 curve calls, and no text field path runs in it. A run again passes. The likely cause is a runtime allocation on the test thread during the loop (for example a JIT tier step). Phase 7 runs a plant that must fail the suite, so read the name of each failed test there.

---

## API Integration

- `TextField.TextChanged` and `TextField.Submitted` (`Action<TextField>?`): the delegate slots that the game sets at build time.
- The field reads the event slots `OnChar`, `OnKeyDown`, `OnPointerDown`, and `OnPointerMove`, the focus owner, and the drag capture of slider.

---

## State Management

- `TextField` holds one fixed `byte[]` buffer, the length, the caret, the anchor, the text scroll, the blink timer, and the pending press x.

---

## Testing Approach

### Unit Tests
**Files:** `tests/BrutalUi.Core.Tests/Elements/TextField*Tests.cs`
- The classes and counts of the plan: Type 10, Caret 10, Delete 8, Submit 6, Text 10, Scroll 9, Blink 6, Selection 10, Pointer 10, and Disabled 5.

### Allocation Tests
**Files:** `tests/BrutalUi.Core.Tests/Elements/TextFieldAllocationTests.cs`
- 3 tests: `Update` and `Draw`, the full input path, and the real-work check.

---

## Needs Human Verification

`/autorun-feature` adds one line per item that the verifier cannot check, for a person to test.

- [ ] Phase 1 demo: a unit test settles the tree, focuses `Field`, pushes the chars `H` and `i` through `UiContext.PushEvent`, and runs `Update` and `Draw` with a `RecordingRenderer`. It reads `Text` as `"Hi"u8`, two `TextChanged` calls with the field as the sender, and a `Text` call `"Hi"` at (0, 0) with the size 16 after `PushClip(0, 0, 81, 16)` and before its `PopClip`. The developer runs `dotnet run --project tests/BrutalUi.Core.Tests/BrutalUi.Core.Tests.csproj -- --filter-class '*TextFieldTypeTests'` and reads a total of 10 with `failed: 0`.
- [ ] Phase 2 demo: a unit test creates the field with the start text `abc`, focuses it, pushes Left, Left, and the char `X`, and runs `Update` and `Draw`. It reads `Text` as `"aXbc"u8`, one `TextChanged` call, and a caret rect at (16, 0, 1, 16) in the text color after the `Text` call and before the `PopClip`. The developer runs `dotnet run --project tests/BrutalUi.Core.Tests/BrutalUi.Core.Tests.csproj -- --filter-class '*TextFieldCaretTests'` and reads a total of 10 with `failed: 0`, and the same command with `'*TextFieldDeleteTests'` reads a total of 8 with `failed: 0`.
- [ ] Phase 3 demo: a unit test focuses `Field`, types `ok`, pushes Enter, and runs `Update`. It reads one `Submitted` call with the field as the sender, two `TextChanged` calls, and `GetText` into a stack span of 16 bytes that returns 2 and holds `"ok"u8`. The developer runs `dotnet run --project tests/BrutalUi.Core.Tests/BrutalUi.Core.Tests.csproj -- --filter-class '*TextFieldSubmitTests'` and reads a total of 6 with `failed: 0`, and the same command with `'*TextFieldTextTests'` reads a total of 10 with `failed: 0`.
- [ ] Phase 4 demo: a unit test focuses `Field`, types 12 chars `a`, and runs `Update(0)` and `Draw`. It reads a `Text` call at (-16, 0) with 12 bytes and a caret rect at (80, 0, 1, 16), both after `PushClip(0, 0, 81, 16)` and before its `PopClip`. The developer runs `dotnet run --project tests/BrutalUi.Core.Tests/BrutalUi.Core.Tests.csproj -- --filter-class '*TextFieldScrollTests'` and reads a total of 9 with `failed: 0`, and the same command with `'*TextFieldBlinkTests'` reads a total of 6 with `failed: 0`.
- [ ] Phase 5 demo: a unit test creates the field with the start text `hello`, focuses it, pushes Shift with Left two times, and runs `Update` and `Draw`. It reads a selection rect at (24, 0, 16, 16) in `Style.Color.ScaleAlpha(SelectionAlpha)` before the `Text` call. It then pushes the char `p` and reads `Text` as `"help"u8`, one `TextChanged` call, the caret at 4, and no selection rect. The developer runs `dotnet run --project tests/BrutalUi.Core.Tests/BrutalUi.Core.Tests.csproj -- --filter-class '*TextFieldSelectionTests'` and reads a total of 10 with `failed: 0`.
- [ ] Phase 6 demo: a unit test creates the field with the start text `hello`, pushes a left press at (21, 8) and a move to (37, 8), and runs `Update` and `Draw`. It reads `FocusedElement` as the field, the anchor at 3, the caret at 5, a selection rect at (24, 0, 16, 16), and no `TextChanged` call. The developer runs `dotnet run --project tests/BrutalUi.Core.Tests/BrutalUi.Core.Tests.csproj -- --filter-class '*TextFieldPointerTests'` and reads a total of 10 with `failed: 0`.
- [ ] Phase 7 demo: the developer runs `dotnet run --project tests/BrutalUi.Core.Tests/BrutalUi.Core.Tests.csproj -- --filter-class '*TextFieldAllocationTests'` and reads a total of 3 with `failed: 0`, and the same command with `'*TextFieldDisabledTests'` reads a total of 5 with `failed: 0`. A planted allocation at the start of `TextField.HandleChar` makes `dotnet test --solution brutal-ui.sln` exit with a value that is not 0.
- [ ] Answer the five follow-up questions of `text-field/code-review.md`: a control character filter in `SetText`, the cost of `IndexAtX` with a large capacity, a blink restart on a press at the caret, a `:disabled` ancestor that does not block the field, and a removed focus owner in the caret pass.

---

## Performance Considerations

- Each slot of `TextField` holds a cached static delegate.
- The caret pass measures the text of the focus owner only.

---

## Next Steps

### Immediate (Today/This Session)
1. A person tests the items of "Needs Human Verification".
2. `/autorun-epic` commits the feature and starts `interactive-elements/widget-sample`.

### Short Term
1. A person answers the follow-up questions of `code-review.md`: a control character filter in `SetText`, the cost of `IndexAtX` with a large capacity, a blink restart on a press at the caret, a `:disabled` ancestor, and a removed focus owner in the caret pass.
2. widget-sample shows a `TextField` in HelloPanel with a size, checks the caret position on screen with `ImGuiRenderer`, and puts the field in the 1000-element gate.

---

## Open Questions

- **Follow-up from phase 2 (done in phase 3).** `AnInvalidByteStepsAsOne` tests the boundary steps on invalid bytes through `SetText(ReadOnlySpan<byte>)`.
- **Follow-up from phase 2 (done in phase 3).** The main Enter key stops the walk and calls `Submitted`.
- **Q-P1-1. A control character in a text from code. ANSWERED in phase 3 (P3-1).** The code path does not filter. The draw sends a control byte of such a text to the renderer as it is.
- **Follow-up from phase 5.** `TransitionCurveTests.TheCurvesAllocateNothing` is a flaky test outside this feature (see Gotchas). A later feature can add a warm-up of the measured loop or a retry. This feature does not edit it.
- **Q-P4-1. The paint mark of the caret pass has no reader.** The clear pass of the same `Update` clears it (P4-8). The mark is harmless now. A future paint cache that skips a clean frame must move the clear or read the mark first, else the blink does not show. No action in this feature.

---

## Session Notes

### 2026-10-27 07:35
- `/autorun-epic` started the feature. `/start-feature` created `context.md`, `tasks.md` (38 tasks in 7 phases), and `inbox.md`.

### 2026-10-27 07:50 (phase 1, group 1)
- Tasks 1.1 to 1.6 are done. The baseline and the gate are in `## Phase 1 Baseline`.
- `WidgetInput.IsCharInput` is new. `TextField.cs` holds the buffer, the char path, and the text draw (step 4 of The Draw, with `x0 = content.X`).
- `TextFieldTree.cs` and `TextFieldTypeTests.cs` (10 tests) are new. All 10 tests passed in the first run.
- The decisions P1-1 to P1-7 and the open question Q-P1-1 are new.
- The parent marks the tasks in `tasks.md`. This run made no git write.
- Verifier (brief `phase-1-verify.md`): PASS. Demo: PASS. The solution gives 2466 tests (Core 1625), 0 failed, 2 skipped. `scripts/test-linux.sh` gives `exit=0`.

### 2026-10-27 (phase 2, group 1)
- Tasks 2.1 to 2.6 are done. The gate is in `### Phase 2 Gate`.
- `WidgetInput.IsEditKey` and the constants `BackspaceKey`, `DeleteKey`, `HomeKey`, and `EndKey` are new.
- `TextField.cs` holds `HandleKeyDown`, `NextBoundary`, `PreviousBoundary`, `Remove`, `MoveCaret`, `CaretWidth`, and step 5 of The Draw.
- `TextFieldCaretTests.cs` (10 tests and the helper `TextFieldDraws`), `TextFieldDeleteTests.cs` (8 tests), and `TextFieldKeyCodeTests.cs` (1 test) are new.
- The decisions P2-1 to P2-9 and two follow-ups are new. No `GlfwKey` name differs from the plan.
- The parent marks the tasks in `tasks.md`. This run made no git write.

### 2026-10-27 (parent, after phase 2)
- Phase 2 verifier (brief `phase-2-verify.md`): PASS. Demo: PASS. The solution gives 2485 tests (Core 1643, Glfw 21), 0 failed, 2 skipped. `scripts/test-linux.sh` gives `exit=0`.
- The parent settles Q-P1-1 by the plan: `SetText` and the start text do not filter a control character (Decision 3, alternative (c)). The phase 3 brief carries the rule.

### 2026-10-27 (phase 3, group 1)
- Tasks 3.1 to 3.5 are done. The gate is in `### Phase 3 Gate`, and the rules are in `### Phase 3 Rules`.
- `TextField.cs` holds `Submitted`, the Enter branch, `GetText`, the three `SetText` overloads, `CutAtBoundary`, `EqualsText`, `EncodeUtf16`, and `WriteLength`.
- `TextFieldSubmitTests.cs` (6 tests) and `TextFieldTextTests.cs` (10 tests) are new. All 16 passed in the first run.
- The decisions P3-1 to P3-7 are new. Q-P1-1 is answered.
- The parent marks the tasks in `tasks.md`. This run made no git write.

### 2026-10-27 (parent, after phase 3)
- Phase 3 verifier (brief `phase-3-verify.md`): PASS. Demo: PASS. The solution gives 2501 tests (Core 1659, Glfw 21), 0 failed, 2 skipped. `scripts/test-linux.sh` gives `exit=0`.

### 2026-10-27 (phase 4, group 1)
- Tasks 4.1 to 4.6 are done. The gate is in `### Phase 4 Gate`, and the blast radius is in `### Blast Radius of Phase 4`.
- `TextField.cs` holds `_scroll`, `_blink`, `CaretBlinkInterval`, `TextScroll`, `CaretVisible`, `AdvanceCaret`, and `RestartCaretBlink`. `Insert`, `Remove`, `MoveCaret`, and `WriteLength` restart the blink. `WriteLength` writes the scroll 0. The draw uses `x0 = content.X - _scroll` and the blink test.
- `UiContext.cs` holds the caret pass in `Update` and the restart line in `SetFocusOwner`, with doc comments.
- `TextFieldScrollTests.cs` (9 tests) and `TextFieldBlinkTests.cs` (6 tests) are new.
- The decisions P4-1 to P4-11 and the open question Q-P4-1 are new.
- The parent marks the tasks in `tasks.md`. This run made no git write.

### 2026-10-27 (parent, after phase 4)
- Phase 4 verifier (brief `phase-4-verify.md`): PASS. Demo: PASS. The solution gives 2516 tests (Core 1674, Glfw 21), 0 failed, 2 skipped. The `UiContext.cs` diff holds the caret pass, one restart line, and doc comments only. `scripts/test-linux.sh` gives `exit=0`.
- The phase 4 agent edited `TextField.cs`, `UiContext.cs`, and `context.md` with short scratchpad scripts, not the edit tool, because a session note asked for Bash. The verifier found no stray change.

### 2026-10-27 (phase 5, group 1)
- Tasks 5.1 to 5.4 are done. The gate is in `### Phase 5 Gate`, and the blast radius is in `### Blast Radius of Phase 5`.
- `TextField.cs` holds `_anchor`, `AnchorIndex`, `SelectionAlpha`, `MoveCaret(int index, bool extend)`, the `Shift` branch and the collapse rule of Left, Right, Home, and End, the selection remove of Backspace and Delete, the selection replace of `HandleChar`, and step 3 of The Draw. `WriteLength`, `Insert`, `Remove`, and the constructor write the anchor.
- `TextFieldSelectionTests.cs` (10 tests) is new. All 10 passed in the first run.
- The decisions P5-1 to P5-10, one gotcha, and one follow-up (the flaky curve test) are new.
- This run used the edit tool for each file change, by the tool rules of the brief. A hook message asked for Bash edits. This run did not follow it.
- The parent marks the tasks in `tasks.md`. This run made no git write.

### 2026-10-27 (parent, after phase 5)
- Phase 5 verifier (brief `phase-5-verify.md`): PASS. Demo: PASS. The solution gives 2526 tests (Core 1684, Glfw 21), 0 failed, 2 skipped. `UiContext.cs` did not change in phase 5. `scripts/test-linux.sh` gives `exit=0`.
- `TransitionCurveTests.TheCurvesAllocateNothing` failed one time in the first solution run of the phase 5 agent and passed in three later runs. This test is outside the feature. The button-and-toggle section of `human-testing.md` already names this flake.

### 2026-10-27 (phase 6, group 1)
- Tasks 6.1 to 6.4 are done. The gate is in `### Phase 6 Gate`, and the blast radius is in `### Blast Radius of Phase 6`.
- `TextField.cs` implements `IDragCapture` explicitly. It holds `_pendingX`, `_pendingAnchorX`, `IndexAtX`, `TextSpaceX`, `DropPending`, `ApplyScrollRule`, the static `HandlePointerDown` and `HandlePointerMove`, and their cached handler fields. The constructor writes `OnPointerDown` and `OnPointerMove`. The class remarks state the press rule, the drag rule, the `Shift` rule, the pending map, the capture, the `=` write of `OnPointerDown`, and the no-measurer rule.
- `TextFieldPointerTests.cs` (10 tests) is new. All 10 passed in the first run. The solution run had no flake.
- The decisions P6-1 to P6-10 are new. P6-1 (two pending x values) and P6-4 (`FontHandle`) differ from the plan text.
- Follow-up: `IndexAtX` makes one measure for each code point up to the x, and each measure reads the prefix. A drag in a field with a large capacity thus costs a square of the length in each frame with a move. The capacity limits it. A later phase can cache the prefix widths.
- This run used the edit tool for each file change, by the tool rules of the brief. An auto mode note asked for Bash edits. This run did not follow it.
- The parent marks the tasks in `tasks.md`. This run made no git write.

### 2026-10-27 (parent, after phase 6)
- Phase 6 verifier (brief `phase-6-verify.md`): PASS. Demo: PASS. The solution gives 2536 tests (Core 1694, Glfw 21), 0 failed, 2 skipped. `SliderDragTests`, `ScrollViewDragTests`, and `DragCaptureTests` pass with no edit. `UiContext.cs` did not change in phase 6. `scripts/test-linux.sh` gives `exit=0`.

### 2026-10-27 (phase 7, group 1)
- Tasks 7.1 to 7.7 are done. The gate is in `### Phase 7 Gate`, and the decisions are P7-1 to P7-6.
- `TextFieldDisabledTests.cs` (5 tests) and `TextFieldAllocationTests.cs` (3 tests) are new. All 8 passed in the first run. The plant failed one gate (`TheTextFieldInputPathAllocatesNothing`), and `cmp` found no difference after the restore.
- `docs/technical-design.md` holds the text field in sections 4, 5, 8, 9, and 14. `CHANGELOG.md` holds the `Added` and `Changed` entries.
- This run used the edit and write tools for each file change, by the tool rules of the brief. An auto mode note asked for Bash edits. This run did not follow it.
- The parent marks the tasks in `tasks.md`. This run made no git write.

### 2026-10-27 (parent, after phase 7)
- Phase 7 verifier (brief `phase-7-verify.md`): PASS. Demo: PASS. The solution gives 2544 tests (Core 1702, Glfw 21), 0 failed, 2 skipped. Against task 1.1: Core plus 87, Glfw plus 1. The plant gave `exit=2`, and `cmp` found no difference after the restore. The lint gives 1.53, 1.62, and 1.79. `scripts/test-linux.sh` gives `exit=0`.
- Each phase is done (38/38). The cross-phase review comes next.

### 2026-10-27 09:46 (update-feature)
- Review fixes verifier (brief `review-verify.md`): PASS. The solution gives 2544 tests (Core 1702, Glfw 21), 0 failed, 2 skipped. Against task 1.1: Core plus 87, Glfw plus 1. `scripts/test-linux.sh` gives `exit=0`.
- `code-review.md` is resolved: 16 findings (0 HIGH, 8 MEDIUM, 8 LOW), each one checked off. CR-1 and CR-12 changed code in `TextField.cs`. CR-13 and CR-15 changed doc comments only. The other findings extended existing test methods, so the class totals stay.
- The draft plan of `interactive-elements/widget-sample` exists (6 phases, 41 tasks, four open scope questions).
- The inbox holds no line. No bug is promoted.
- `implementation.md` status: Ready for Review. The built code differs from the plan text in three private details: two pending x values (P6-1), `IndexAtX` takes a `FontHandle` (P6-4), and the string `SetText` compares through `EqualsText` (P3-2). The docs follow the code (P7-4).

---

*Update this file at the end of each work session. Run `/update-feature` before compacting conversations.*
