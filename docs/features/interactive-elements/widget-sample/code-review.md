# interactive-elements/widget-sample - Code Review

**Reviewed:** 2026-10-28
**Reviewer:** Agent (cross-phase review of phases 1 to 6)
**Rules Applied:** 1. The sample allocates nothing, 2. The smoke contract, 3. The sheet, 4. The gate, 5. The shared file, 6. No library change, 7. The sample is documentation, 8. Test quality, 9. `AGENTS.md` and the docs. The Vercel React rules do not apply to C#.
**Status:** Resolved - 11 findings, 10 fixed, 1 not fixed with a reason (CR-9)

---

## Summary

| Severity | Count | Findings |
|----------|-------|----------|
| HIGH | 1 | CR-1 |
| MEDIUM | 6 | CR-2, CR-3, CR-4, CR-5, CR-6, CR-7 |
| LOW | 4 | CR-8, CR-9, CR-10, CR-11 |

The feature does what the plan asks. The sample allocates nothing after warm-up, the smoke contract holds, each sheet mutation fails a test, and the shared tree keeps its result. `src/`, `scripts/`, each `.csproj`, `Directory.Packages.props`, and `samples/WorldSpace/` have no diff against `26d6851`. The findings are gaps in the guards and wrong remarks. The new gate does not measure the press map of the text field (CR-1). Two event groups of the gate step have no observable result (CR-2). The smoke run does not check that the caret pass runs (CR-3), and the longest-text rule of the two text labels has no guard (CR-6). Five remarks or docs state a wrong cause or a stale fact (CR-4, CR-5, CR-7, CR-8, CR-10).

### Baseline

- `dotnet build brutal-ui.sln`: `0 Warning(s)`, `0 Error(s)`.
- `dotnet test --solution brutal-ui.sln`: total 2558, failed 0, succeeded 2556, skipped 2 (no display). `TransitionCurveTests.TheCurvesAllocateNothing` did not fail.
- `scripts/test-linux.sh`: `exit=0`. The 5 lines of `artifacts/test-linux/smoke.log`:
  ```
  HelloPanel: 120 frames rendered.
  HelloPanel: last frame: draw commands 7, vertices 1100.
  HelloPanel: bytes after warm-up: Update and Draw 0, frame 20256, in 117 frames.
  HelloPanel: gate: pointer over UI in 58 frames, keyboard focus in UI in 117 frames, of 117 measured frames.
  HelloPanel: widgets: clicks 119, flips 119, values 119, scrolls 118, edits 120, submits 120.
  ```
- `git diff 26d6851 --stat -- src scripts Directory.Packages.props '*.csproj' samples/WorldSpace`: no output. Of the tracked test files, only `ParsedSheetAllocationTests.cs` and `ThousandElementTree.cs` changed.

### Probe method

The probes ran in a copy of the repository in the scratchpad (`cr-widget-sample/repo`, made with `rsync` with no `bin`, `obj`, `.git`, and `artifacts`). Each changed file had a saved original in `cr-widget-sample/orig`. The C# mutations used the `#if Mnn` method: `Directory.Build.props` of the copy appended `$(Mut)` to `DefineConstants`, and each run used `-p:Mut=<symbol> -p:TreatWarningsAsErrors=false`. A sample mutation ran the smoke step of `scripts/test-linux.sh` by hand in the copy (the same `env`, `timeout`, and `xvfb-run -a dotnet run ... --no-build -- --frames 120` command) after a build of the sample. The copy run with no mutation gave the 5 baseline lines and exit code 0. A sheet mutation changed `HelloScreen.css` of the copy with `sed`, ran `HelloPanelSheetTests` and `ParsedSheetAllocationTests`, and restored the file. A gate mutation ran the full `BrutalUi.Core.Tests` (1715 tests, 0 failed with no mutation). The scenarios ran as temporary test classes in the copy: one in `BrutalUi.Markup.Tests` that linked `HelloWidgets.cs` and `HelloScreen.cs` and built the real screen with `HelloScreen.LoadSheet` and `HelloScreen.Build`, and one in `BrutalUi.Core.Tests` with the base-commit `ThousandElementTree.cs` under a new class name.

Totals: 31 mutations. 17 caught, 1 caught by an older gate only (CR-1), 5 equivalent, 8 not caught (3 with a recorded reason and no finding, 5 with a finding). 6 scenarios, each with the expected result.

### Probe list

**1. The status rule of the sample (`HelloWidgets.cs`, smoke run)**

| # | Mutation | Result |
|---|----------|--------|
| M01 | No `SetText(ClickStatusLongest)` | exit 0. Equivalent: the longest text is 25 bytes, and `Utf8Text` starts with a buffer of 32 bytes (`InitialTextCapacity`). The label never grows. |
| M02 | No `SetText(ScrollStatusLongest)` | exit 0. Equivalent: the longest text is 28 bytes, below the start buffer of 32 bytes. |
| M03 | No longest-text `WriteText` of the text label | exit 0. 0 failed (finding CR-6). The smoke run types one char only. Scenario (a) with M03 measures 88 bytes after warm-up. |
| M04 | `count.ToString(CultureInfo.InvariantCulture)` and `Encoding.UTF8.GetBytes` in place of `int.TryFormat` | exit 1. `HelloPanel: Update and Draw allocated 3560 bytes after warm-up. The limit is 0 bytes.` |
| M05 | `Clicked = b => OnClicked(b)` in place of `_onClicked` | exit 0. Equivalent: the constructor creates the lambda one time, as it creates the cached delegate. The frame loop creates nothing. |
| M06 | `CopyTo` with no check in place of `TryCopyTo` in `WriteText` | exit 0. Equivalent: the field holds at most 32 bytes, and the free room after the prefix is 58 or 53 bytes. |

**2. The smoke script (`Program.cs`, smoke run)**

| # | Mutation | Result |
|---|----------|--------|
| M07 | The widget check removed | exit 0. Not caught, reason recorded: the check fires on a zero count only, and the healthy run has no zero count. |
| M07+M11 | The widget check removed and no toggle click | exit 0 with `flips 0`. Not caught, reason recorded: the check is the only guard of a zero count, as planned. Plant B of phase 1 proves the check. |
| M08 | The button click after the pointer move | exit 1. `HelloPanel: the pointer read over the UI in each measured frame. The off-panel frames did not reach the game.` Line 2 reads `draw commands 5, vertices 1096`. |
| M09 | The Tab after the move (the plan order) | exit 0. 0 failed (finding CR-3). Line 2 reads `draw commands 5, vertices 1096`: the field loses the focus, and the caret pass and the caret draw do not run. |
| M10 | The field char with no caller target | exit 0 with `edits 118`. Not caught, reason recorded: the Tab and the field press give the field the focus, so the focus route carries the char. Only frames 0 and 1 lose an edit. |
| M11 | No toggle click | exit 1. `HelloPanel: the synthetic input did not reach each widget.` Line 5 reads `flips 0`. |
| M12 | No Tab at all | exit 0 with the same 5 lines as the baseline. 0 failed (finding CR-4). |

**3. The sheet (`HelloScreen.css`, `HelloPanelSheetTests`)**

| # | Mutation | Result |
|---|----------|--------|
| C1 | `Button:disabled` before `Button:hover` | `TheDisabledRuleWinsOverHover` fails. |
| C2 | `Toggle:checked` before `Toggle:active` | `TheToggleRulesStyleTheToggle` fails. |
| C3 | No `width` and no `height` in the `Slider` rule | `TheSliderRulesStyleTheSlider` fails. |
| C4 | `flex-grow: 1` in place of `height: 80px` in the `ScrollView` rule | `TheScrollViewRulesStyleTheView` and `TheScrollContentIsTallerThanTheView` fail. |

**4. The gate (`ThousandWidgetAllocationTests`, `ThousandWidgetTree`, full `BrutalUi.Core.Tests`)**

| # | Mutation | Result |
|---|----------|--------|
| P01 | Plant at the start of `Button.HandlePointerUp` | `TheWidgetInputPathAllocatesNothing` fails (also `WidgetAllocationTests.TheWidgetInputPathAllocatesNothing`). |
| P02 | Plant at the start of `Toggle.HandlePointerUp` | `TheWidgetInputPathAllocatesNothing` fails (also `WidgetAllocationTests.TheWidgetInputPathAllocatesNothing`). |
| P03 | Plant at the start of `Slider.HandlePointerMove` | `TheWidgetInputPathAllocatesNothing` fails (also `SliderAllocationTests` and `ScrollViewAllocationTests` input gates). |
| P04 | Plant at the start of `ScrollView.HandleScroll` | `TheWidgetInputPathAllocatesNothing` fails (also `ScrollViewAllocationTests.TheScrollViewInputPathAllocatesNothing`). |
| P05 | Plant at the start of `TextField.HandleChar` | `TheWidgetInputPathAllocatesNothing` fails (also `TextFieldAllocationTests.TheTextFieldInputPathAllocatesNothing`). |
| P06 | Plant at the start of `TextField.HandleKeyDown` | `TheWidgetInputPathAllocatesNothing` fails (also `TextFieldAllocationTests.TheTextFieldInputPathAllocatesNothing`). |
| P07 | Plant in `Counter.OnClicked` of the test | `TheWidgetInputPathAllocatesNothing` fails. |
| P08 | Plant at the start of `TextField.IndexAtX` (the press map of the caret pass) | 0 failed in the new gate (finding CR-1). Only `TextFieldAllocationTests.TheTextFieldInputPathAllocatesNothing` fails. |
| T01 | No `AllocationGate.ReleaseAllocationContext()` before the two measures | 0 failed. Equivalent: the release removes a false failure (a retired allocation context). It cannot hide an allocation. |
| T02 | Group 2 (the disabled button) removed from the step | 0 failed (finding CR-2). |
| T03 | Group 3 (the toggle) removed from the step | `TheMeasuredBodyDoesRealWork` fails. |
| T04 | Group 7 (the Tab) removed from the step | 0 failed (finding CR-2). |
| T05 | The footer swap leaves 999 elements (2 view rows) | Each of the 4 tests fails: `Build` throws. |
| T06 | The footer swap leaves 1001 elements (4 view rows) | Each of the 4 tests fails: `Build` throws. |

**5. The scenarios**

| # | Scenario | Result |
|---|----------|--------|
| (a) | The real screen. After 3 warm-up rounds, type 9 chars of U+1F600 (the field keeps 8, 32 bytes) and Enter, then clear and type 33 `W` (the field keeps 32) and Enter | Pass. 0 bytes for each measure, no throw. The labels read `Text: ` and `Submitted: ` with the 32 bytes. With M03 the same scenario measures 88 bytes. |
| (b) | `Clicks` set to `int.MaxValue - 1` by reflection, then 3 calls of `Clicked` | Pass. `Clicks` stays at `int.MaxValue`, the label reads `Button clicks: 2147483647`, and the last 2 calls allocate 0 bytes. |
| (c) | The focus on the slider, then a click on the not-pickable toggle status label (the row takes the hit), then a click in the top margin of row 2 | Pass. The panel takes the focus each time. Each count stays at 0, the toggle stays unchecked, and the slider stays at 50. |
| (d) | 6 Tab keys with no focus owner | Pass. The order is the panel, `Click me`, the toggle, the slider, the text field, and the panel again. The disabled button and the view take no focus. |
| (e) | A press on item 2 of the view and a move of 20 pixels up | Pass. The view holds `Active`, the offset is 20, `Scrolls` rises, and the label reads `Scroll offset: 20 px`. |
| (f) | `ThousandElementTree.Build()`, `Build(null)`, and the base-commit `Build()` | Pass. Each tree holds 1000 elements with the same types, names, pickable and focusable flags, child counts, and inline widths. Each context has no measurer and the same viewport. |

---

## Findings

### CR-1: The new gate does not measure the press map of the text field
**Severity:** HIGH
**Rule:** 4. The gate
**File:** `tests/BrutalUi.Core.Tests/ThousandWidgetAllocationTests.cs:211-267`
**Problem:** A press on a `TextField` stores the pointer x. The caret pass of the next `Update` maps it with `IndexAtX`, and the caret pass runs for the focus owner only. The input step ends with the view press (the focus clears) and the Tab (the focus goes to `Play`). Thus the caret pass of the step never runs for the field. The frame gate gives the field the focus with `SetFocus`, but it pushes no press, so the pending x is empty. Plant P08 at the start of `TextField.IndexAtX` gives 0 failed tests in `ThousandWidgetAllocationTests`. Only `TextFieldAllocationTests` catches it. Plant D of task 5.5 (`AdvanceCaret`) fails the frame gate only, for the same cause (open risk 4). The thirteenth gate thus misses one path of one widget on the 1000-element tree.
**Fix:** Move group 5 (the field) to the end of the step, after group 7. Add an Enter after the Tab of group 7, so that the Tab is observable (see CR-2). At the end of group 5, after the edit keys, push a second press and release at a point inside the text. The field then holds the focus and a pending x at the caret pass of the step:
```csharp
// 7. A Tab with no owner moves the focus to Play. The Enter clicks Play.
context.PushEvent(in scene.TabDown);
context.PushEvent(in scene.EnterDown);

// 5. The field, last: ... the edit keys ..., then a press inside the text. The caret pass maps it.
context.PushEvent(in scene.FieldPress);
// ... scroll, char, Backspace, Enter ...
context.PushEvent(in scene.FieldTextPress);
context.PushEvent(in scene.FieldTextRelease);
```
Update `TheMeasuredBodyDoesRealWork`: 3 clicks, the field as the focus owner, and a caret index from the press map. Run plant P08 again and expect `TheWidgetInputPathAllocatesNothing` in the failed list.
**Effort:** Low

### CR-2: The real-work test does not see the disabled group or the Tab group
**Severity:** MEDIUM
**Rule:** 8. Test quality
**File:** `tests/BrutalUi.Core.Tests/ThousandWidgetAllocationTests.cs:390-428`
**Problem:** Mutation T02 removes group 2 (the 4 events at the disabled button), and mutation T04 removes group 7 (the Tab). Each run gives 0 failed tests. Group 2 has no counter: `DisabledClicks` is 0 with the group and without it. Group 7 has no observable result: no assert reads the focus owner after the step. Thus the gate can lose the disabled path or the Tab walk on the 1000-element tree with no signal.
**Fix:** Extend `TheMeasuredBodyDoesRealWork` and `BuildScene`, and add no test method. For group 7, push an Enter after the Tab (CR-1), so that the Tab gives a third click of `Play`, and assert `Clicks == 3`. For group 2, give the footer a pointer-down handler slot that calls a new `Counter` method. A press on the disabled button walks on to the footer (the disabled rule of `Button`), so the count of the handler changes when group 2 goes. Assert the exact count.
**Effort:** Low

### CR-3: The smoke run does not check that the caret pass runs
**Severity:** MEDIUM
**Rule:** 2. The smoke contract
**File:** `samples/HelloPanel/Program.cs:660-730`
**Problem:** Phase 4 put the field events into the smoke run so that the caret pass, `IndexAtX`, and the caret draw run with `ImGuiRenderer` under the 0-byte check. No check reads that the field holds the focus at the caret pass. Mutation M09 (the Tab after the move, the plan order) gives exit 0 and the same line 5. Only line 2 changes (`draw commands 5, vertices 1096`), and no script reads line 2. Thus a change of the push order or of the focus rule removes the caret pass from the GPU check with no signal.
**Fix:** In `RunFrames`, count the measured frames in which the field holds the focus after `Update`. `UiContext.FocusedElement` is public:
```csharp
ui.Update(io.DeltaTime);
if (measured && ui.FocusedElement == widgets.TextField)
{
    caretFrames++;
}
```
Add the count to `RunResult`, and make the widget check fail with the widget line when the count is 0. Record the rule in the remarks of `Run`.
**Effort:** Low

### CR-4: The remarks give the Tab the job of the field press, and two remarks are stale
**Severity:** MEDIUM
**Rule:** 7. The sample is documentation
**File:** `samples/HelloPanel/Program.cs:272-277, 763-767`
**Problem:** Three facts in the remarks do not match the code.
1. The focus rule paragraph of `PushSmokeInput` says: "Thus the Tab comes before the text field events. The Tab moves the focus from the slider to the field, and the field keeps the focus to the end of the drain." Mutation M12 removes the Tab and gives the same 5 lines, with the caret (1100 vertices). The field press gives the field the focus. The real rule is: no focus change comes after the field press. A Tab after the field press moves the focus to the panel.
2. The Tab has no observable result in the smoke run now. The presses give the focus to the UI, so the keyboard count of line 4 (117 frames) no longer proves the Tab. The remarks do not say this.
3. The widget check paragraph of `Run` names the press and the release on three widgets and the scroll, and "a click count, a flip count, a value count, or a scroll count of 0". The check reads six counts. The paragraph has no text for the edits and the submits since phase 4.
**Fix:** Write the real rule: "The field press is the last focus change of the drain, so the field holds the focus at the caret pass. A Tab after the field press moves the focus to the panel." State that the presses also give the focus, so the keyboard count of line 4 does not prove the Tab, and that `FocusAllocationTests` and the queue case of `ThousandElementAllocationTests` guard the Tab. Add the char, the Backspace, the Enter, the edit count, and the submit count to the widget check paragraph.
**Effort:** Low

### CR-5: The remarks of `HelloScreen.cs` say that each widget is focusable
**Severity:** MEDIUM
**Rule:** 7. The sample is documentation
**File:** `samples/HelloPanel/HelloScreen.cs:20-21, 52-53, 171-172, 221`
**Problem:** The class remarks say "each widget of the widget section is focusable too". The `Build` remarks say "each widget of the widget section is focusable too", and the comment in `Build` says "The panel and each widget are focusable". The scroll view takes no focus, and the disabled button does not accept the focus (`Element.AcceptsFocus`). Scenario (d) confirms the real order. A game developer copies this file and reads a wrong focus model. The list of class names (lines 52-53) also has no `hello-status`, but `HelloWidgets` uses that class and the sheet has the rule `.hello-status > Label`.
**Fix:** Write "each enabled widget of the section is focusable, except the scroll view" in the three places, or name the four widgets that take the focus (`Click me`, the toggle, the slider, and the text field). Add `hello-status` to the list of class names.
**Effort:** Low

### CR-6: No test and no smoke run guards the longest-text rule of the text labels
**Severity:** MEDIUM
**Rule:** 8. Test quality
**File:** `samples/HelloPanel/Program.cs:829-847`
**Problem:** The text label and the submit label are the only status labels whose longest text (38 and 43 bytes) is longer than the start buffer of `Utf8Text` (32 bytes). For the other labels, the longest-text write is an equivalent mutant (M01, M02). The smoke run types one char only, so the label texts stay at 7 and 12 bytes. Mutation M03 (no longest-text write of the text label) gives exit 0. Scenario (a) with M03 shows the defect: a 32-byte text after warm-up allocates 88 bytes (a new 64-byte label buffer). No test compiles `HelloWidgets.cs`, so no test can see it.
**Fix:** In one measured frame of the smoke run (for example `frameIndex == WarmUpFrames`), push `HelloWidgets.TextCapacity` chars of `W` with the field as the caller target, then the Enter, then the same number of Backspace keys, before the char of the frame. The text then returns to the state of the frame, and both labels get their longest text in a measured frame. Update the line 5 counts in section 14 and in the runbooks. Plant M03 must then give exit 1 with the allocation line.
**Effort:** Medium

### CR-7: The sheet comment of `.hello-panel:focus` is stale
**Severity:** MEDIUM
**Rule:** 7. The sample is documentation
**File:** `samples/HelloPanel/HelloScreen.css:67`
**Problem:** The comment says "Tab gives the focus to the panel, the only focusable element." The panel is now the first of five focusable elements (open risk 12, phase 6 decision 4). Also, the new header paragraph (lines 18-21) wraps at about 117 columns, and the rest of the header wraps at 80 columns.
**Fix:** Write "Tab gives the focus to the panel first, then to each focusable widget." Wrap the header paragraph at 80 columns.
**Effort:** Low

### CR-8: Two remarks of `HelloWidgets.cs` state a wrong behavior
**Severity:** LOW
**Rule:** 7. The sample is documentation
**File:** `samples/HelloPanel/HelloWidgets.cs:43, 277, 485-497 (prefix.CopyTo at 497)`
**Problem:** The class remarks and the remarks of `DisabledButton` say "A press on it changes nothing". A press on a disabled element clears the focus (`Element.IsFocusable` remarks). The gate relies on that rule: the Enter of group 2 gives no click of `Play`. Also, the `<exception>` tag of `WriteCount` names `InvalidOperationException` for a text that does not fit, but `prefix.CopyTo(scratch)` throws `ArgumentException` for a long prefix. `WriteText` uses `TryCopyTo` for the same case.
**Fix:** Write "A press on it raises no click and clears the keyboard focus." Use `TryCopyTo` for the prefix in `WriteCount`, as `WriteText` does.
**Effort:** Low

### CR-9: The cref of `ThousandElementTree.Build` in the facts report is ambiguous
**Severity:** LOW
**Rule:** 5. The shared file
**File:** `tests/BrutalUi.Benchmarks/FactsReport.cs:15`
**Problem:** `<see cref="ThousandElementTree.Build"/>` now names two overloads. The test projects make no documentation file, so the build gives no CS0419 today (open risk 3). A project that turns on `GenerateDocumentationFile` gets CS0419, which is an error with `TreatWarningsAsErrors`.
**Fix:** Write `<see cref="ThousandElementTree.Build()"/>`.
**Effort:** Low
**Resolution (2026-10-28):** Not fixed in this feature. Criterion I2 of the Definition of Done allows a diff in two existing test files only (`ParsedSheetAllocationTests.cs` and `ThousandElementTree.cs`), and the build gives no CS0419 today. The one-line fix in `FactsReport.cs` is a follow-up for the user (open scope question of the run report).

### CR-10: Section 14 states the wrong cause for the Tab order
**Severity:** LOW
**Rule:** 9. `AGENTS.md` and the docs
**File:** `docs/technical-design.md:1176-1177`
**Problem:** The sample item says "The Tab comes before the field events, so the field keeps the focus to the end of the drain and the caret pass runs." The field press gives the focus (CR-4, mutation M12). The thirteenth gate item lists the 7 widget rules in an order that differs from `BuildWidgetSheet` (`ScrollView:hover` and `Slider:focus` change places).
**Fix:** Write "No focus change comes after the field press, so the field holds the focus at the caret pass." State that the presses also give the focus, so the keyboard count of line 4 does not prove the Tab. List the 7 rules in the order of the code.
**Effort:** Low

### CR-11: The Windows checklist has items with two actions
**Severity:** LOW
**Rule:** 9. `AGENTS.md` and the docs
**File:** `docs/testing-windows.md:63, 68, 72`
**Problem:** Item 4 holds a hover and a Tab. Item 9 holds a wheel turn and a drag (open risk 13). A person who sees one result fail cannot tell which action failed. Item 13 also has no text for the disabled button and the list, but item 14 of the Linux checklist says "The disabled button and the list take no focus."
**Fix:** Split items 4 and 9 into two items each, as the Linux checklist does, and renumber the items and the report block. Add "The disabled button and the list take no focus." to the Tab item.
**Effort:** Low

---

## What's Already Good

- **Rule 1.** The sample allocates nothing after warm-up. Each delegate is a cached instance method, and each write goes through the fixed scratch buffer. M04 (`ToString`) gives exit 1 with 3560 bytes. Scenario (a) measures 0 bytes for 32-byte texts of 4-byte code points and of `W`, and scenario (b) measures 0 bytes at `int.MaxValue`. `HelloWidgets.cs` holds no lambda, no `$"`, no `string.Format`, and no `.ToString(`.
- **Rule 2.** The arguments and the exit codes stay. The allocation check runs first, line 5 prints six counts, and a zero count gives the widget line (M11). The widget events come before the move (M08 fails with the pointer line). `scripts/` has no diff.
- **Rule 3.** Each sheet mutation fails a test (C1 to C4). The matrix holds a rule for each bit, `Button:disabled` and `Toggle:checked` come last, and each widget with no own size gets a size.
- **Rule 4.** Each handler plant of the brief (P01 to P07) fails `TheWidgetInputPathAllocatesNothing`. A footer swap to 999 or 1001 elements fails each of the 4 tests. Each measured body holds no lambda, no string, no boxing, and no `Assert`, and each measure follows a release.
- **Rule 5 and open risk 2.** Scenario (f) shows that `Build()` and `Build(null)` give the base tree and a context with no measurer. `ThousandWidgetTree.cs` uses no internal member and no test framework type, so the glob of the benchmark project and of the ImGui tests compiles it with no change. The build of the solution gives 0 warnings. The file costs those projects compile time only.
- **Rule 6.** No library change.
- **Scenarios (c), (d), and (e).** A gap click focuses the panel and runs no delegate. The Tab order matches the plan. A press on an item label drags the view.
- **Open risk 5.** The slider points are 50 and 150 pixels from the left edge of a 200-pixel border box with a 2-pixel border. Both are inside the content box with a margin of 48 pixels, and both values differ from 50 and from each other.
- **Open risk 6.** The counts stop at `int.MaxValue` (scenario b). The longest count text is 29 bytes with the suffix, so the throw of `WriteCount` cannot occur for an `int` count. The throw only guards a later edit of a prefix.
- **Open risk 7.** The fixed `height: 32px` leaves a content box of 20 pixels. That holds one line of the ImGui default font at 16 pixels. An automatic minimum height needs a change of `src/`, which is out of scope.
- **Open risk 9.** `scrolls 118` and `edits 120, submits 120` follow from frame 0 (no layout) and frame 1 (a scroll up at offset 0). Section 14 and `context.md` record the cause.
- **Open risk 10.** 10576 bytes of plant D is 2400 bytes of the plant plus a retired allocation context. The measured code allocates, so a parallel GC can retire the context. The gate fails in each case, so the extra bytes do not hide a defect.
- **Open risk 11.** The diffs of the four phase 4 files hold no stray change of logic. The only drift is the wrap width of some comment lines (CR-7, and `Program.cs` line 28 at 138 columns).
- **Open risk 14.** The second assert of `TheTextFieldHasRoomForOneLine` compares the content height with the font size. A real font has a line of at least its size, so the assert covers the case that the 16-pixel test line hides.

## Follow-up Questions

1. **Open risk 1.** Do you accept that the smoke run no longer proves the Tab (CR-4, M12)? Option A: keep the Tab and correct the remarks (the fix of CR-4). Option B: also push a key after the Tab that only the Tab target takes, so that a lost Tab changes a count.
2. **Open risk 4.** CR-1 moves the field group to the end of the gate step. Do you want the frame gate to keep `SetFocus` on the field, or to drop it when the input gate runs the caret pass?
3. **Open risk 8.** `Program.ViewportCenter` subtracts no scroll offset. No smoke point needs it today. Do you want a remark on the helper for a game developer who aims at a widget inside a view?
4. **M07.** No unit test pins the exit logic of `Run`. Only a plant in the smoke run proves each exit line. Do you want a small pure method for the checks, so that a test can pin them?
5. **M05.** The grep of Q3 in the Verification Method (`\) =>`) does not match a lambda with one parameter and no brackets (`b => OnClicked(b)`). Do you want the pattern ` => ` in place of it?
6. **Open risk 13.** CR-11 splits the merged Windows items. Do you want the same rule ("one action and one result") stated at the top of the Windows checklist too?

## Recommended Fix Order

| Order | Finding | File | Severity | Effort |
|-------|---------|------|----------|--------|
| 1 | CR-1 | `tests/BrutalUi.Core.Tests/ThousandWidgetAllocationTests.cs` | HIGH | Low |
| 2 | CR-2 | `tests/BrutalUi.Core.Tests/ThousandWidgetAllocationTests.cs` | MEDIUM | Low |
| 3 | CR-3 | `samples/HelloPanel/Program.cs` | MEDIUM | Low |
| 4 | CR-4 | `samples/HelloPanel/Program.cs` | MEDIUM | Low |
| 5 | CR-5 | `samples/HelloPanel/HelloScreen.cs` | MEDIUM | Low |
| 6 | CR-7 | `samples/HelloPanel/HelloScreen.css` | MEDIUM | Low |
| 7 | CR-6 | `samples/HelloPanel/Program.cs` | MEDIUM | Medium |
| 8 | CR-8 | `samples/HelloPanel/HelloWidgets.cs` | LOW | Low |
| 9 | CR-9 | `tests/BrutalUi.Benchmarks/FactsReport.cs` | LOW | Low |
| 10 | CR-10 | `docs/technical-design.md` | LOW | Low |
| 11 | CR-11 | `docs/testing-windows.md` | LOW | Low |

The fix groups: CR-1 and CR-2 (`ThousandWidgetAllocationTests.cs`), CR-3, CR-4, and CR-6 (`Program.cs`), and one group for each other file. CR-10 follows CR-4, and the line 5 counts of CR-6 also change section 14 and the runbooks.

## Progress

- [x] CR-1: The new gate does not measure the press map of the text field (fixed in review-g1.md)
- [x] CR-2: The real-work test does not see the disabled group or the Tab group (fixed in review-g1.md)
- [x] CR-3: The smoke run does not check that the caret pass runs (fixed in review-g2.md)
- [x] CR-4: The remarks give the Tab the job of the field press, and two remarks are stale (fixed in review-g2.md)
- [x] CR-5: The remarks of `HelloScreen.cs` say that each widget is focusable (fixed in review-g3.md and review-g3-fix1.md)
- [x] CR-6: No test and no smoke run guards the longest-text rule of the text labels (fixed in review-g2.md)
- [x] CR-7: The sheet comment of `.hello-panel:focus` is stale (fixed in review-g4.md)
- [x] CR-8: Two remarks of `HelloWidgets.cs` state a wrong behavior (fixed in review-g5.md)
- [x] CR-9: The cref of `ThousandElementTree.Build` in the facts report is ambiguous (not fixed: out of scope by criterion I2, follow-up for the user)
- [x] CR-10: Section 14 states the wrong cause for the Tab order (fixed in review-g6.md)
- [x] CR-11: The Windows checklist has items with two actions (fixed in review-g7.md)
