# interactive-elements/widget-sample - Context & Decisions

**Last Updated:** 2026-10-28 19:30
**Current Phase:** Complete (phases 1 to 6 and the cross-phase review are done)
**Status:** ✅ Ready for Review

---

## Quick Status

**What's Done:**
- ✅ The plan is accepted. The user chose option A for each open scope question: Q1 the click on screen plus the headless check, Q2 the smoke run drives each widget and exits with code 1 on a zero count, Q3 a second disabled `Button`, and Q4 the `Build(IUiRenderer?)` overload with 9 widgets in the last 3 footer slots.
- ✅ `/autorun-epic` runs this feature on the branch `feat/interactive-elements/widget-sample`.

- ✅ Phase 1 group A (tasks 1.1 to 1.6 and 1.8): the widget section with the `Button` row, the button rules, line 5 of the smoke run, and the count line of `ParsedSheetAllocationTests`. The gate passes with `clicks 119`.

- ✅ Phase 1 group B (tasks 1.7, 1.9, 1.10, and 1.11): `HelloPanelSheetTests` (3 tests), plants A and B, and the phase 1 gate. The gate passes.
- ✅ The phase 1 verifier (`phase-1-verify.md`): PASS. Demo: MANUAL (the on-screen click needs a person at a desktop). The headless part passes with `clicks 119`.

- ✅ Phase 2 (tasks 2.1 to 2.5): the disabled button, the `Toggle` row, the `Slider` row, their rules (23 rules), and the smoke script. The verifier (`phase-2-verify.md`) returns PASS with `flips 119, values 119`. Demo: MANUAL.

- ✅ Phase 3 (tasks 3.1 to 3.5): the `ScrollView` row with 8 item labels, its rules (27 rules), and the smoke scroll. The verifier (`phase-3-verify.md`) returns PASS with `scrolls 118`. Demo: MANUAL.

- ✅ Phase 4 (tasks 4.1 to 4.6): the `TextField` row, its rules (31 rules), the smoke typing and submit, and plant C. The verifier (`phase-4-verify.md`) returns PASS with `edits 120, submits 120` and `Update and Draw 0`. Demo: MANUAL.

- ✅ Phase 5 (tasks 5.1 to 5.6): the `Build(IUiRenderer?)` overload, `ThousandWidgetTree`, and `ThousandWidgetAllocationTests` (4 tests). The plant in `TextField.AdvanceCaret` fails the new frame gate. The verifier (`phase-5-verify.md`) returns PASS. Demo: PASS.

- ✅ Phase 6 (tasks 6.1 to 6.8): the technical design (sections 3, 5, 14, and 16), both runbooks, the epic `human-testing.md`, the changelog, and the STE lint (no score above 2.5). The verifier (`phase-6-verify.md`) returns PASS. Demo: MANUAL.

- ✅ The cross-phase review (`code-review.md`): 11 findings (1 HIGH, 6 MEDIUM, 4 LOW). Fix groups `review-g1.md` to `review-g7.md` (with `review-g3-fix1.md` and `review-g6-fix1.md`) fix 10 findings. CR-9 stays open on purpose (criterion I2). The review verifier (`review-verify.md`) returns PASS: 2558 tests with 0 failed, `exit=0`, and line 5 `clicks 119, flips 119, values 119, scrolls 118, edits 182, submits 121`.

**What's Next:**
- 📋 A person tests the items of "Needs Human Verification" on a desktop and on Windows.
- 📋 `/autorun-epic` commits the feature. This is the last feature of the epic, so `/autorun-epic` runs `/update-epic` and `/update-master`.
- 📋 The user decides CR-9: the one-line cref fix in `tests/BrutalUi.Benchmarks/FactsReport.cs`.

**Blockers:**
- None

---

## Key Files

### Core Implementation
- `samples/HelloPanel/HelloWidgets.cs` - the widget section, the status labels, the counts, and the delegates (new).
- `samples/HelloPanel/HelloScreen.cs` - `Build` adds the widget section to the panel.
- `samples/HelloPanel/HelloScreen.css` - the rule matrix of the widgets.
- `samples/HelloPanel/Program.cs` - the widget script of the smoke input, line 5, and the widget check.

### Related Files
- `tests/BrutalUi.Markup.Tests/Style/HelloPanelSheetTests.cs` - the sheet tests (new).
- `tests/BrutalUi.Markup.Tests/Style/ParsedSheetAllocationTests.cs` - one count line removed.
- `tests/BrutalUi.Core.Tests/Shared/ThousandElementTree.cs` - the `Build(IUiRenderer?)` overload.
- `tests/BrutalUi.Core.Tests/Shared/ThousandWidgetTree.cs` - the gate tree (new).
- `tests/BrutalUi.Core.Tests/ThousandWidgetAllocationTests.cs` - the thirteenth allocation gate (new).
- `docs/features/interactive-elements/widget-sample/implementation.md` - the implementation plan.

---

## Phase 1 Baseline

Task 1.1 records the base here, before any change.

- **Base commit:** `26d6851d664e27de46294a8821484947a0dc26ab` (`HEAD`, the text-field commit). This run makes no commit, so `<base>` means `HEAD`.
- **`git ls-files src/BrutalUi.Core/Elements/TextField.cs`:** one line, `src/BrutalUi.Core/Elements/TextField.cs`. Thus text-field is in the base commit.
- **`git status --porcelain -- src tests samples`:** no line.
- **`scripts/test-linux.sh`:** `test-linux: PASS build, test, gpu, smoke (120 frames) in 26 s`, `exit=0`.
- **The 4 lines of `artifacts/test-linux/smoke.log`:**
  ```
  HelloPanel: 120 frames rendered.
  HelloPanel: last frame: draw commands 2, vertices 288.
  HelloPanel: bytes after warm-up: Update and Draw 0, frame 22464, in 117 frames.
  HelloPanel: gate: pointer over UI in 58 frames, keyboard focus in UI in 117 frames, of 117 measured frames.
  ```
- **Test counts (solution):** total 2544, failed 0, succeeded 2542, skipped 2.
- **Test counts (each project, `dotnet run --no-build --project <project>`):**

  | Project | Total | Succeeded | Skipped |
  |---------|-------|-----------|---------|
  | `BrutalUi.Core.Tests` | 1711 | 1711 | 0 |
  | `BrutalUi.Framework.Tests` | 1 | 1 | 0 |
  | `BrutalUi.Glfw.Tests` | 21 | 19 | 2 (no display) |
  | `BrutalUi.ImGui.Tests` | 243 | 243 | 0 |
  | `BrutalUi.Markup.Tests` | 568 | 568 | 0 |

- **The Codebase Findings, checked against the base:**
  - Finding 1: true. `Program.RunFrames` reads the counter at the start of the UI region (before `PushSmokeInput`) and after `gateLabel.SetText`.
  - Finding 2: true. `PushSmokeInput` pushes one move, one Tab down, and one Tab up. The move goes to the panel center in an even frame and 8 px above and left of the panel in an odd frame.
  - Finding 3: true. `HelloScreen.Build(TextureHandle, out Element panel, out Label gateLabel)` builds the root, the panel, the greeting, the gate label, and the image.
  - Finding 4: true. The gate label starts with `PointerYesFocusYes`, the longest gate text.
  - Finding 5: true. Line 80 of `ParsedSheetAllocationTests.cs` holds `Assert.Equal(6, sheet.Rules.Length);`.
  - Finding 6: true. The parser knows `margin`, `padding`, `border-*`, `background-color`, `color`, `font-size`, `flex-*`, and `align-*`. It has no `gap`.
  - Finding 10: true. `RunFrames` sets `input.KeyboardEnabled = !io.WantCaptureKeyboard` before `Glfw.PollEvents()`.
  - Finding 13: true. `WidgetInput.IsClick` needs `UiContext.CurrentPressTarget`, and the drain sets it only for an event with no caller target.
  - Findings 7, 8, and 9: from the plan. This group did not check them again.

### Group A gate (task 1.6)

- **Build:** `0 Warning(s)`, `0 Error(s)`.
- **Solution tests:** total 2544, failed 0, succeeded 2542, skipped 2. The counts do not change, because group A adds no test. `ParsedSheetAllocationTests` passes with the count line removed.
- **`scripts/test-linux.sh`:** `test-linux: PASS build, test, gpu, smoke (120 frames) in 18 s`, `exit=0`.
- **The 5 lines of `artifacts/test-linux/smoke.log`:**
  ```
  HelloPanel: 120 frames rendered.
  HelloPanel: last frame: draw commands 3, vertices 436.
  HelloPanel: bytes after warm-up: Update and Draw 0, frame 19056, in 117 frames.
  HelloPanel: gate: pointer over UI in 58 frames, keyboard focus in UI in 117 frames, of 117 measured frames.
  HelloPanel: widgets: clicks 119.
  ```
- **Line 2:** the draw commands go from 2 to 3, and the vertices go from 288 to 436 (148 more). The widget section comes after the image and uses the font atlas texture again, so it opens a third draw command. The 148 vertices are the fill and the border of the button, the text `Click me`, and the status text.
- **Line 5:** 119 clicks in 120 frames. Frame 0 has no layout, so the press at (0, 0) hits no element. Each later frame clicks one time.
- **Line 4:** no change from the base. The press and the release come before the move, so the move still decides the gate.
- **Acceptance checks:** `HelloWidgets.cs` holds no lambda, no `$"`, no `string.Format`, and no `.ToString(` call. `git diff HEAD --stat -- src scripts '*.csproj' Directory.Packages.props` gives no output. The diff of `ParsedSheetAllocationTests.cs` shows one removed assert line and one added comment line.

### Phase 1 Gate

Group B ran the plants (tasks 1.9 and 1.10) and the gate (task 1.11).

- **Plant A (task 1.9):** `GC.KeepAlive(new object());` as the first line of `HelloWidgets.OnClicked`. `scripts/test-linux.sh` gives `test-linux: FAIL at smoke (exit 1)` and `exit=1`. The failure line is `HelloPanel: Update and Draw allocated 2808 bytes after warm-up. The limit is 0 bytes.` 2808 bytes is 117 measured frames times 24 bytes (one object for each click). Line 5 shows `clicks 119`. The restore from the scratchpad copy gives `cmp=0` (no difference).
- **Plant B (task 1.10):** the `PushClick(ui, ViewportCenter(widgets.ClickButton), time);` line of `PushSmokeInput` removed (the press and the release). `scripts/test-linux.sh` gives `test-linux: FAIL at smoke (exit 1)` and `exit=1`. The failure line is `HelloPanel: the synthetic input did not reach each widget.` Line 5 shows `clicks 0`, and line 3 shows `Update and Draw 0`. The restore from the scratchpad copy gives `cmp=0` (no difference).
- **Build:** `0 Warning(s)`, `0 Error(s)`.
- **Solution tests:** total 2547, failed 0, succeeded 2545, skipped 2. The 3 new tests of `HelloPanelSheetTests` give the 3 more tests. `TransitionCurveTests.TheCurvesAllocateNothing` did not fail in this run.
- **`HelloPanelSheetTests`:** total 3, failed 0, succeeded 3.
- **The regression set (`dotnet run` for each class):** `ParsedSheetAllocationTests` 2 of 2, `WidgetAllocationTests` 4 of 4, `SliderAllocationTests` 3 of 3, `ScrollViewAllocationTests` 3 of 3, `TextFieldAllocationTests` 3 of 3, `ThousandElementAllocationTests` 14 of 14, `PointerAllocationTests` 3 of 3, `FocusAllocationTests` 3 of 3, and `ImGuiRendererAllocationTests` 4 of 4 (in `BrutalUi.ImGui.Tests`). Each class passes.
- **`scripts/test-linux.sh`:** `test-linux: PASS build, test, gpu, smoke (120 frames) in 46 s`, `exit=0`.
- **The 5 lines of `artifacts/test-linux/smoke.log`:**
  ```
  HelloPanel: 120 frames rendered.
  HelloPanel: last frame: draw commands 3, vertices 436.
  HelloPanel: bytes after warm-up: Update and Draw 0, frame 14976, in 117 frames.
  HelloPanel: gate: pointer over UI in 58 frames, keyboard focus in UI in 117 frames, of 117 measured frames.
  HelloPanel: widgets: clicks 119.
  ```
- **`git diff HEAD --stat -- src scripts '*.csproj' Directory.Packages.props`:** no output.

### Phase 2 Gate

- **Build:** `0 Warning(s)`, `0 Error(s)`.
- **Solution tests:** total 2550, failed 0, succeeded 2548, skipped 2. The 3 new tests of `HelloPanelSheetTests` give the 3 more tests. `TransitionCurveTests.TheCurvesAllocateNothing` did not fail in this run.
- **`HelloPanelSheetTests`:** total 6, failed 0, succeeded 6.
- **The regression set (`dotnet run` for each class):** `ParsedSheetAllocationTests` 2, `WidgetAllocationTests` 4, `SliderAllocationTests` 3, `ScrollViewAllocationTests` 3, `TextFieldAllocationTests` 3, `ThousandElementAllocationTests` 14, `PointerAllocationTests` 3, `FocusAllocationTests` 3, and `ImGuiRendererAllocationTests` 4. Each class has 0 failed.
- **`scripts/test-linux.sh`:** `test-linux: PASS build, test, gpu, smoke (120 frames) in 26 s`, `exit=0`.
- **The 5 lines of `artifacts/test-linux/smoke.log`:**
  ```
  HelloPanel: 120 frames rendered.
  HelloPanel: last frame: draw commands 3, vertices 760.
  HelloPanel: bytes after warm-up: Update and Draw 0, frame 21216, in 117 frames.
  HelloPanel: gate: pointer over UI in 58 frames, keyboard focus in UI in 117 frames, of 117 measured frames.
  HelloPanel: widgets: clicks 119, flips 119, values 119.
  ```
- **Line 2:** the vertices go from 436 to 760 (324 more): the disabled button, the toggle, the slider track and thumb, and the two status labels. The draw commands stay at 3.
- **Line 5:** 119 in 120 frames for each count. Frame 0 has no layout, so the presses hit no element.
- **Sheet rule count:** 23 (13 of phase 1, 10 of phase 2).
- **`git diff HEAD --stat -- src scripts '*.csproj' Directory.Packages.props`:** no output.

### Phase 3 Gate

- **Build:** `0 Warning(s)`, `0 Error(s)`.
- **Solution tests:** total 2552, failed 0, succeeded 2550, skipped 2. The 2 new tests of `HelloPanelSheetTests` give the 2 more tests. `TransitionCurveTests.TheCurvesAllocateNothing` did not fail in this run.
- **`HelloPanelSheetTests`:** total 8, failed 0, succeeded 8.
- **The regression set (`dotnet run --no-build` for each class):** `ParsedSheetAllocationTests` 2, `WidgetAllocationTests` 4, `SliderAllocationTests` 3, `ScrollViewAllocationTests` 3, `TextFieldAllocationTests` 3, `ThousandElementAllocationTests` 14, `PointerAllocationTests` 3, `FocusAllocationTests` 3, and `ImGuiRendererAllocationTests` 4. Each class has 0 failed.
- **`scripts/test-linux.sh`:** `test-linux: PASS build, test, gpu, smoke (120 frames) in 32 s`. The run piped the output through `tail`, so the `exit=` echo shows the exit code of `tail` (0). The PASS line proves exit code 0 of the script.
- **The 5 lines of `artifacts/test-linux/smoke.log`:**
  ```
  HelloPanel: 120 frames rendered.
  HelloPanel: last frame: draw commands 5, vertices 980.
  HelloPanel: bytes after warm-up: Update and Draw 0, frame 22368, in 117 frames.
  HelloPanel: gate: pointer over UI in 58 frames, keyboard focus in UI in 117 frames, of 117 measured frames.
  HelloPanel: widgets: clicks 119, flips 119, values 119, scrolls 118.
  ```
- **Line 2:** the vertices go from 760 to 980 (220 more), and the draw commands go from 3 to 5. The view clips its items, so the clip opens new draw commands.
- **Line 5:** `scrolls 118` in 120 frames. Frame 0 has no layout, so the scroll hits no element. Frame 1 (odd) scrolls up at offset 0, which changes nothing and calls nothing. Each frame from frame 2 moves the offset by 40 pixels (down in an even frame, up in an odd frame), so 118 frames call the delegate.
- **Line 4:** no change. The scroll comes before the move, so the move still decides the gate.
- **Sheet rule count:** 27 (13 of phase 1, 10 of phase 2, 4 of phase 3).
- **`git diff HEAD --stat -- src scripts '*.csproj' Directory.Packages.props`:** no output.

### Phase 4 Gate

- **Build:** `0 Warning(s)`, `0 Error(s)`.
- **Solution tests:** total 2554, failed 0, succeeded 2552, skipped 2. The 2 new tests of `HelloPanelSheetTests` give the 2 more tests. `TransitionCurveTests.TheCurvesAllocateNothing` did not fail in this run.
- **`HelloPanelSheetTests`:** total 10, failed 0, succeeded 10.
- **The regression set (`dotnet run --no-build` for each class):** `ParsedSheetAllocationTests` 2, `WidgetAllocationTests` 4, `SliderAllocationTests` 3, `ScrollViewAllocationTests` 3, `TextFieldAllocationTests` 3, `ThousandElementAllocationTests` 14, `PointerAllocationTests` 3, `FocusAllocationTests` 3, and `ImGuiRendererAllocationTests` 4. Each class has 0 failed.
- **`scripts/test-linux.sh`:** `test-linux: PASS build, test, gpu, smoke (120 frames) in 21 s`, `exit=0` (no pipe before the echo). The run again after plant C gives the same 5 lines and `exit=0` (38 s).
- **The 5 lines of `artifacts/test-linux/smoke.log`:**
  ```
  HelloPanel: 120 frames rendered.
  HelloPanel: last frame: draw commands 7, vertices 1100.
  HelloPanel: bytes after warm-up: Update and Draw 0, frame 20640, in 117 frames.
  HelloPanel: gate: pointer over UI in 58 frames, keyboard focus in UI in 117 frames, of 117 measured frames.
  HelloPanel: widgets: clicks 119, flips 119, values 119, scrolls 118, edits 120, submits 120.
  ```
- **Line 2:** the vertices go from 980 to 1100 (120 more): the fill and the border of the field, the two status labels, and the caret. The draw commands go from 5 to 7, because the field clips its text and its caret to its padding box. The last frame (119) is odd, so the text of the field is empty in it.
- **Line 3:** `Update and Draw 0`. The caret pass, the blink, `IndexAtX`, and the caret draw run with the `ImGuiRenderer` measure on the GPU and allocate nothing. No allocation comes from `src/`.
- **Line 5:** `edits 120` and `submits 120` in 120 frames. The char, the Backspace, and the Enter have the field as the caller target, so they need no layout and count in frame 0 too. Each frame changes the text: the char `a` in an even frame and a Backspace of that `a` in an odd frame.
- **Line 4:** no change. The field events come before the move, so the move still decides the gate. The field holds the focus at the end of each drain, so the focus count stays at 117.
- **Sheet rule count:** 31 (13 of phase 1, 10 of phase 2, 4 of phase 3, 4 of phase 4), as planned.
- **Plant C (task 4.6):** `GC.KeepAlive(new object());` as the first line of `HelloWidgets.OnTextChanged`. `scripts/test-linux.sh` gives `test-linux: FAIL at smoke (exit 1)` and `exit=1`. The failure line is `HelloPanel: Update and Draw allocated 2808 bytes after warm-up. The limit is 0 bytes.` 2808 bytes is 117 measured frames times 24 bytes (one object for each edit). Line 5 shows `edits 120, submits 120`. The restore from the scratchpad copy gives `cmp=0` (no difference), and `grep -c GC.KeepAlive` gives 0.
- **The focus order check (Phase 4 decision 1):** an experiment moved the Tab back after the move, as in phase 3. The run gave `draw commands 5, vertices 1096` in line 2 and the same counts in line 5. The 4 missing vertices are the caret. Thus with a Tab after the field events, the field loses the focus before the caret pass, and the caret pass and the caret draw do not run. The restore of `Program.cs` from the scratchpad copy gives `cmp=0`.
- **Acceptance checks:** `HelloWidgets.cs` holds no lambda, no `$"`, no `string.Format`, and no `.ToString(` call. The STE lint of its comment lines gives 1.44 for each 100 words. `git diff HEAD --stat -- src scripts '*.csproj' Directory.Packages.props` gives no output.

### Phase 5 Gate

- **Build:** `0 Warning(s)`, `0 Error(s)`.
- **Solution tests:** total 2558, failed 0, succeeded 2556, skipped 2. The 4 tests of `ThousandWidgetAllocationTests` give the 4 more tests. `TransitionCurveTests.TheCurvesAllocateNothing` did not fail in this run.
- **`ThousandWidgetAllocationTests`:** total 4, failed 0, on the first run. `TheMeasuredBodyDoesRealWork` gives each count of the plan: 2 clicks, 0 clicks of the disabled button, 2 flips, 3 value changes, 2 scroll changes, 2 text changes, 1 submit, the text `field`, the toggle not checked, the view offset (0, 60), and a `PushClip` with the viewport rect of the view and of the field. No count differs from the plan, so no step and no expected count changed.
- **The regression set (`dotnet run --no-build` for each class):** `ParsedSheetAllocationTests` 2, `WidgetAllocationTests` 4, `SliderAllocationTests` 3, `ScrollViewAllocationTests` 3, `TextFieldAllocationTests` 3, `ThousandElementAllocationTests` 14, `PointerAllocationTests` 3, `FocusAllocationTests` 3, and `ImGuiRendererAllocationTests` 4. Each class has 0 failed. No file of the set changed.
- **The benchmark build:** `Build succeeded`, `0 Warning(s)`, `0 Error(s)`.
- **Plant D (task 5.5):** `GC.KeepAlive(new object());` as the first line of `TextField.AdvanceCaret`. `dotnet test --solution brutal-ui.sln` gives `exit=2`, total 2558, failed 3, succeeded 2553. The failed tests:
  - `ThousandWidgetAllocationTests.UpdateAndDrawAllocateNothingWithEachWidget` (10576 bytes, expected 0).
  - `TextFieldAllocationTests.TheTextFieldInputPathAllocatesNothing`.
  - `TextFieldAllocationTests.UpdateAndDrawAllocateNothingWithTextFields`.
- **Plant D notes:** 100 frames of 24 bytes give 2400 bytes. The other 8176 bytes are most likely the free part of an allocation context: the measured code allocates, so a GC of a parallel test class can retire a context with a free part (the remarks of `AllocationGate`). `TheWidgetInputPathAllocatesNothing` did not fail with the phase 5 step, because that step ended with the focus on the button `Play` (Phase 5 decision 6). Review fix group 1 moved the field group last, and the plant now fails 4 gates (see the session note "review fix group 6 fix 1"). The restore from the scratchpad copy gives `cmp=0`, and `grep -c GC.KeepAlive` gives 0.
- **`git diff HEAD --stat -- src scripts '*.csproj' Directory.Packages.props`:** no output.
- **Acceptance checks:** the two new files hold no `=>`, no `$"`, and no `.ToString(` call. Each gate calls `AllocationGate.ReleaseAllocationContext()` right before its measure. The STE lint of the comment lines of the two new files gives 0.70 for each 100 words.
- **No allocation from `src/`:** the gates pass with no new warm-up round, so the stop rule does not apply.

### Phase 6 Gate

- **Build:** `0 Warning(s)`, `0 Error(s)`.
- **Solution tests:** total 2558, failed 0, succeeded 2556, skipped 2. The base of task 1.1 was total 2544, failed 0, succeeded 2542, skipped 2. The 14 more tests are the 10 of `HelloPanelSheetTests` and the 4 of `ThousandWidgetAllocationTests`. Phase 6 adds no test, so the count equals the count of the Phase 5 Gate. `TransitionCurveTests.TheCurvesAllocateNothing` did not fail in this run.
- **`scripts/test-linux.sh`:** `test-linux: PASS build, test, gpu, smoke (120 frames) in 23 s`, `exit=0` (no pipe before the echo).
- **The 5 lines of `artifacts/test-linux/smoke.log`:** the same as the Phase 4 Gate, with `bytes after warm-up: Update and Draw 0, frame 22464`:
  ```
  HelloPanel: 120 frames rendered.
  HelloPanel: last frame: draw commands 7, vertices 1100.
  HelloPanel: bytes after warm-up: Update and Draw 0, frame 22464, in 117 frames.
  HelloPanel: gate: pointer over UI in 58 frames, keyboard focus in UI in 117 frames, of 117 measured frames.
  HelloPanel: widgets: clicks 119, flips 119, values 119, scrolls 118, edits 120, submits 120.
  ```
- **`git diff HEAD --stat -- src scripts '*.csproj' Directory.Packages.props samples/WorldSpace`:** no output.
- **The STE lint (`ste-lint.py --fail-over 2.5`), score for each 100 words:** `docs/technical-design.md` 1.53, `docs/testing-linux.md` 0.56, `docs/testing-windows.md` 0.54, `docs/features/interactive-elements/human-testing.md` 2.25 (one em dash in the old title line), `CHANGELOG.md` 1.71, `docs/features/interactive-elements/widget-sample/implementation.md` 1.76, and the comment lines of `samples/HelloPanel/HelloWidgets.cs` 0.62. No score is above 2.5, so no lint fix was needed and `HelloWidgets.cs` and `implementation.md` have no edit from this phase.
- **The method for `HelloWidgets.cs`:** `grep -hE '^\s*//'` took the 200 comment lines. `sed` removed the `//` and `///` marks and the XML tags and turned `&lt;` and `&gt;` into `<` and `>`. The lint ran on the scratch file `scratchpad/hw-comments.txt` (outside the repository).

### Review Fix Group 2 Gate

- **Build:** `0 Warning(s)`, `0 Error(s)`.
- **Solution tests:** total 2558, failed 0, succeeded 2556, skipped 2. Group 2 adds no test. `TransitionCurveTests.TheCurvesAllocateNothing` did not fail in this run.
- **`scripts/test-linux.sh`:** `test-linux: PASS build, test, gpu, smoke (120 frames) in 18 s`, `exit=0` (no pipe before the echo). The run after the two restores gives `exit=0` (20 s) and the same 5 lines.
- **The 5 lines of `artifacts/test-linux/smoke.log`:**
  ```
  HelloPanel: 120 frames rendered.
  HelloPanel: last frame: draw commands 7, vertices 1100.
  HelloPanel: bytes after warm-up: Update and Draw 0, frame 22464, in 117 frames.
  HelloPanel: gate: pointer over UI in 58 frames, keyboard focus in UI in 117 frames, of 117 measured frames.
  HelloPanel: widgets: clicks 119, flips 119, values 119, scrolls 118, edits 182, submits 121.
  ```
- **Line 5:** `edits 182` and `submits 121`. The burst of frame 3 gives 31 edits for the chars of `W` (the field drops the 32nd char), 1 submit, and 32 edits for the Backspace keys. The Backspace of frame 3 then finds an empty field and gives no edit. Thus 120 + 31 + 32 - 1 = 182, and 120 + 1 = 121. Lines 1 to 4 do not change. Section 14 of `docs/technical-design.md` and the runbooks need the new line 5 (a later group).
- **Plant M03:** the line `WriteText(_textStatus, TextStatusPrefix, longest);` removed from the constructor of `HelloWidgets`. `scripts/test-linux.sh` gives `exit=1`. Line 3 reads `Update and Draw 88`. The failure line is `HelloPanel: Update and Draw allocated 88 bytes after warm-up. The limit is 0 bytes.` The restore from the scratchpad copy gives `cmp=0`.
- **Plant M09:** the Tab key down and key up moved after the pointer move (the plan order). `scripts/test-linux.sh` gives `exit=1`. Line 2 reads `draw commands 5, vertices 1096`, and lines 3 to 5 do not change. The failure line is `HelloPanel: the synthetic input did not reach each widget.` The restore from the scratchpad copy gives `cmp=0`.
- **`git diff HEAD --stat -- src scripts '*.csproj' Directory.Packages.props`:** no output.
- **The STE lint (`ste-lint.py --fail-over 2.5`) of the new comment lines of `Program.cs`:** 1.33 for each 100 words (1130 words). No new line is wider than 120 columns.

---

## Important Decisions

The plan holds Decisions 1 to 10. This section records each decision that the plan did not settle.

### Phase 1, group A (2026-10-28)

1. **The widget colors and the button sizes.** The plan gives no value. The sheet adds 5 variables to `:root`: `--widget-fill: #2c3446`, `--widget-border: #4a5672`, `--widget-hover: #3c4862`, `--widget-active: #182030`, and `--widget-focus: #f0c040` (the value of `--panel-focus`, in its own variable). The `Button` rule sets `padding: 6px 12px`, `border-width: 2px`, `border-radius: 4px`, the fill, the border color, `color: var(--text)`, and `font-size: 16px`. The layout rules use `margin: 12px 0 0` (`.hello-widgets`), `margin: 8px 0 0` (`.hello-row`), and `margin: 0 0 0 12px` (`.hello-row > Label`). `.hello-widgets` also sets `align-items: flex-start`, so each row keeps the size of its content. Each fill value differs from each other fill value, so the sheet tests of group B can tell the states apart.
2. **`Button:active` comes after `Button:hover`.** Both rules have the same specificity, so the later rule wins (`StyleRule` remarks). Thus a press over the button shows the active fill.
3. **The counts stop at `int.MaxValue`.** `OnClicked` adds 1 only below `int.MaxValue`. A wrap to a negative value gives a text of 26 bytes, longer than the longest text (25 bytes), and breaks the longest text rule.
4. **`WriteCount` is the one format path.** `HelloWidgets.WriteCount(Label, ReadOnlySpan<byte> prefix, int count)` copies the prefix into `_scratch`, calls `int.TryFormat` with `CultureInfo.InvariantCulture`, and calls `Label.SetText`. It throws `InvalidOperationException` when the text does not fit in 64 bytes. That path does not occur for an `int` count. The constructor sets the start text through the same path. Later phases call it for `Values` and `Scrolls`.
5. **The `PushClick` helper.** `Program.PushClick(UiContext, float2, double)` pushes the left press and the left release with no target. The toggle of phase 2 and the field of phase 4 use the same pair. The brief names only `ViewportCenter(Element)`, so this helper is an addition inside `Program.cs`.
6. **The off-panel point.** `PushSmokeInput` now computes the off-panel point from `ViewportCenter(panel)` minus half of the panel size, minus 8 pixels. The value is the same as the old walk, to the float rounding. Line 4 shows no change.
7. **Line 5 in each run.** `Run` writes line 5 after each run, as lines 1 to 4. The widget check runs only in a smoke run with 2 measured frames or more, inside the block of the input check and after its two checks.
8. **The comment of `ParsedSheetAllocationTests`.** The comment is the text of the brief on one line (133 characters). The build gives no warning for it.

### Phase 1, group B (2026-10-28)

1. **The measurer of `HelloPanelSheetTests`.** The class has its own private `ByteMeasurer`: 8 pixels for each byte and a line of 16 pixels. The `FixedMeasurer` of `ParsedSheetAllocationTests` is private, so the class does not share it.
2. **One tree for two tests.** A private `WidgetTree` builds a root `.hello-root`, a panel `.hello-panel` with a greeting `Label`, a section `.hello-widgets`, and a row `.hello-row` with a `Button` and a status `Label`. `SetStates` sets the button states and runs `Update(0f)`. The sheet has no transition, so one update with time 0 gives each target value.
3. **Checks past the brief.** `TheButtonRulesStyleTheButton` also checks the border width (2 px), the corner radius (4 px), the font size (16 px), `Hover | Active` (the active fill wins), and the return to no state. `TheRowLabelRuleKeepsThePanelLabelRule` also checks the color and the left margin (12 px) of the status label, the undefined left margin of the greeting, and the layout rules of the section and the row. Each value comes from `HelloScreen.css`.
4. **The selector list of `TheSheetHoldsEachRule`.** The test holds the 13 selector texts in the order of the sheet and counts each text in the rules. A phase that adds a rule updates `RuleCount` and the list, and records the cause here.
5. **Plant B removes one line.** `PushClick` pushes the press and the release of `ClickButton` in one call. Thus plant B removes that one call and the blank line after it.

### Phase 2 (2026-10-28)

1. **New sheet variables.** `:root` gets 7 variables for the new colors: `--disabled-fill: #1a1e26`, `--disabled-text: #6a7284`, `--toggle-checked: #3c8c5a`, `--slider-track: #141a26`, `--slider-thumb: #b4c0d8`, `--slider-thumb-hover: #dce4f4`, and `--slider-thumb-active: #70a0f0`. The hover, active, border, and focus rules reuse the `--widget-*` variables.
2. **Sizes.** `Toggle` is `20px` by `20px` with a `2px` border and a `4px` radius. `Slider` is `200px` by `20px` with a `2px` border, a `4px` radius, the track fill in `background-color`, and the thumb in `color`. The content box of the slider is then 196 by 16, so the thumb is a 16 pixel square.
3. **Rule order.** `Button:disabled` follows `Button:focus` (after `Button:hover` and `Button:active`). `Toggle:checked` follows `Toggle:focus`, so it comes after `Toggle:hover` and `Toggle:active`. `Toggle:focus` sets the border only, so a checked toggle with focus shows both the checked fill and the focus border.
4. **The slider press point.** `Program.SliderPoint(Slider, int)` maps 25 % (even frame) or 75 % (odd frame) of the border box width of the slider, not of the content box. The `Layout.ContentBox` method is internal to `BrutalUi.Core`, and the sample would copy the border width. Both points are inside the content box (50 and 150 pixels from the left edge, the border is 2 pixels). The values are 22 and 78 (the start value is 50), so each frame changes the value.
5. **The toggle label text.** `WriteToggleStatus` writes `Toggle: on` or `Toggle: off` with `SetText` from `u8` literals. It does not use `WriteCount`, because the text has no number. The start text is `Toggle: off`, which is also the longest text.
6. **`HelloPanelSheetTests` tree.** The test tree now has three rows: row 1 holds the `Click me` button, its label, and the disabled button. Row 2 holds the toggle. Row 3 holds the slider. `WidgetTree.SetStates(Element, PseudoStates)` is new, and `SetStates(PseudoStates)` calls it for the button. The toggle test sets `Checked` through the property and the states through `States`.
7. **No change to the plan.** The rule count is 23 as planned.

### Phase 3 (2026-10-28)

1. **New sheet variable.** `:root` gets `--scroll-hover-border: #7a94c8`, because the hover border color needs a new color (the `--widget-*` colors are too close to the base border). The base rule reuses `--widget-fill` and `--widget-border`, and the active rule reuses `--widget-active`.
2. **The view rules.** `ScrollView` sets `width: 240px`, `height: 80px`, `flex-direction: column`, `padding: 4px`, `border-width: 2px`, `border-radius: 4px`, the fill, and the border color. `ScrollView:hover` sets the border color only. `ScrollView:active` sets the fill only and follows `ScrollView:hover`. `ScrollView > Label` sets `color: var(--text)` and `font-size: 16px`. The view has a fixed height, not `flex-grow` (CR-4).
3. **The suffix in the format path.** `WriteCount` gets a fourth parameter, `ReadOnlySpan<byte> suffix = default`. After `TryFormat`, it copies the suffix into the scratch buffer with `TryCopyTo` and throws `InvalidOperationException` when the text does not fit. No other call site changes. The path allocates nothing. The longest scroll text is 28 bytes, so it fits the 64 byte buffer.
4. **The scroll text.** The start text and the longest text are `Scroll offset: 0 px` and `Scroll offset: 2147483647 px`. The label gets the longest text first. `WriteScrollStatus` writes `(int)ScrollView.ScrollOffset.Y`. The delegate reads the property of `HelloWidgets`, not the sender, so the constructor uses the same call for the start text.
5. **The items.** `HelloWidgets.AddItem(ReadOnlySpan<byte>)` makes a label with `IsPickable = false` and `SetText` from a `u8` literal. The constructor calls it 8 times (`Item 1` to `Item 8`).
6. **The smoke scroll.** `PushSmokeInput` pushes one `ScrollEvent` after the three click pairs and before the move. The position is `ViewportCenter(widgets.ScrollView)`, the delta is `(0, -1)` in an even frame and `(0, 1)` in an odd frame, and the header target is `null`. The view is not inside a scroll view, so `ViewportCenter` needs no offset. `RunResult` gets `Scrolls` after `Values`.
7. **The test tree.** `HelloPanelSheetTests.WidgetTree` gets a fourth row with a `ScrollView`, 8 not pickable item labels (`"Item " + i`, built in the test), and a status label. The property `ScrollView` is new. `TheScrollViewRulesStyleTheView` also checks the column direction, the padding (4 px), the border width (2 px), the radius (4 px), the active fill with hover, the return to no state, and the item margin (undefined). `TheScrollContentIsTallerThanTheView` also checks 8 children, the height of 80 px, and offset X 0.
8. **No change to the plan.** The rule count is 27 as planned.

### Phase 4 (2026-10-28)

1. **The Tab comes before the text field events.** The plan keeps the Tab after the move, and the caller target keeps the char and the keys on the field. But the caret pass of `UiContext.Update` runs for the focus owner only, and the field draws its caret only with the focus. A Tab after the field press moves the focus from the field to the panel, so the caret pass, `IndexAtX`, and the caret draw never run, and the goal of the phase (the caret pass and the press map on the GPU) fails with no test to show it. Thus `PushSmokeInput` now pushes the Tab down and the Tab up after the scroll and before the field events. The Tab moves the focus from the slider to the field, and the field keeps it to the end of the drain. The move stays last, so the gate (line 4) does not change. The experiment of the Phase 4 Gate confirms the cause (4 vertices of the caret).
2. **The press comes after the edit keys.** The order of the field events is the char or the Backspace, then the Enter, then the press and the release at the center of the field. A change of the text after the press drops the stored pointer x (the pending map rule of `TextField`), so a press before the char maps nothing. With the press last, the caret pass maps the x with `IndexAtX` in each frame.
3. **Key down only.** The Backspace and the Enter get a key down and no key up, as the Smoke Widget Script names. A new helper `Program.PushKeyDown(UiContext, Element, int, double)` pushes one key down with a caller target. The key codes are the private constants `SmokeBackspaceKey = 259` and `SmokeEnterKey = 257`, with a comment that names `WidgetInput.BackspaceKey` and `WidgetInput.EnterKey` (internal to Core). The char is `SmokeChar = 'a'`.
4. **The field size.** `TextField` sets `width: 240px`, `height: 32px`, `padding: 4px 8px`, `border-width: 2px`, `border-radius: 4px`, the fill, the border color, `color: var(--text)`, and `font-size: 16px`. The plan names no height. The field has no own size, so with no height the content box is 0 pixels tall. The height is a border-box value, so the content box is 20 pixels tall, more than one line at 16 px. No new variable: the hover rule reuses `--widget-hover`, and the focus rule reuses `--widget-focus`.
5. **The status column.** The `.hello-status` element has no rule. The default direction of an element is a column (`FlexDirection.Column = 0`), so the rule count stays at 31. The rule `.hello-status > Label` also sets `margin: 0 0 0 12px`, as `.hello-row > Label` does, so the labels keep a gap from the field. The plan names only the color and the size for it.
6. **The text format path.** `HelloWidgets.WriteText(Label, ReadOnlySpan<byte> prefix, ReadOnlySpan<byte> text)` copies the prefix and the text into the scratch buffer with `TryCopyTo` and calls `Label.SetText`. It throws `InvalidOperationException` when the text does not fit. The constructor fills a 32-byte stack buffer with `W` for the longest text (`Text: ` and 32 bytes, 38 bytes in total, and `Submitted: ` and 32 bytes, 43 bytes in total), then writes the start text. `OnTextChanged` writes `sender.Text`, and `OnSubmitted` writes `sender.Text` at the submit.
7. **The test tree.** `HelloPanelSheetTests.WidgetTree` gets a fifth row with a `TextField` (capacity 32) and a `.hello-status` column with two labels. The properties `TextField`, `StatusColumn`, `TextLabel`, and `SubmitLabel` are new. `TheTextFieldRulesStyleTheField` also checks the height (32), the padding, the border width, the radius, the font size, hover with focus, the return to no state, the status label rule, and the column direction of the status column. `TheTextFieldHasRoomForOneLine` computes the content height from the layout rect, the padding, and the border, because `Layout.ContentBox` is internal to Core. It compares the content height with the line height of the test measurer and with the font size, because the test measurer gives 16 pixels for each font size.
8. **No change to the plan count.** The rule count is 31 as planned.

### Phase 5 (2026-10-28)

1. **The glob of the shared folder.** `BrutalUi.Benchmarks.csproj` and `BrutalUi.ImGui.Tests.csproj` compile `../BrutalUi.Core.Tests/Shared/*.cs`. Thus they also compile the new `ThousandWidgetTree.cs`, not only `BrutalUi.Core.Tests`. The file uses no test framework type and no internal member of `BrutalUi.Core`, so both projects build with no change. The internal members `UiContext.HitTest`, `ScrollView.MaxOffset`, and the `WidgetInput` key codes stay in `ThousandWidgetAllocationTests.cs`. The event helpers of `PointerTree` (in `Input/`, not linked) stay in the test file too.
2. **The helpers of the tree.** `PointerTree.ViewportRect` is not in the shared folder, so `ThousandWidgetTree` has its own public static `ViewportRect`, `ViewportCenter`, and `CountSubtree`. The sum of the layout positions subtracts no scroll offset. No widget is inside a scroll view, so the sum is correct.
3. **The cref of the overload.** The class remarks of `ThousandElementTree` now name `Build()` and `Build(IUiRenderer)`, so no cref is ambiguous. `tests/BrutalUi.Benchmarks/FactsReport.cs` keeps `<see cref="ThousandElementTree.Build"/>`. The test projects make no documentation file, so the compiler does not check the cref (no CS0419). Follow-up risk: a test project that turns on `GenerateDocumentationFile` gets CS0419 there, which is an error with `TreatWarningsAsErrors`.
4. **The widget sheet.** The 7 rules set only a fill or a border color, so a state change changes no rect: `Button:hover`, `Button:active`, `Button:disabled`, and `Toggle:checked` set a fill. `Slider:focus` and `TextField:focus` set a border color. `ScrollView:hover` sets a fill. `Button:active` follows `Button:hover`, as in the sample sheet. The button, the toggle, and the view have no inline fill, because an inline value wins over a rule. The 3 view rows have an inline fill, so the view draws content inside its clip. The colors are public fields of `ThousandWidgetTree`.
5. **The names and the delegates.** Each element name of the 9 new elements starts with `thousand-widget-`. The tree adds no class name. The tree sets no delegate slot. `BuildScene` of the test sets each slot to an instance method of `Counter`, with a separate method for the disabled button, so the test counts a click of that button apart.
6. **The focus at the end of the step.** The press on the view clears the focus (the view is not focusable and no ancestor is), and the Tab with no owner moves it to the first focusable element, the button `Play`. Thus the caret pass does not run in the input gate. The frame gate covers the caret pass: it gives the focus to the field with `SetFocus` and asserts after the measure that the field still holds it. Plant D confirms the split.
7. **No direct drain.** The step calls `Update(0.016f)` and `Draw`, as the brief says. `Update` drains the 32 events first. The ring holds 256 entries, so it drops nothing.
8. **The slider points.** The points are 25 % and 75 % of the border box width from `ViewportRect`, at the center height. The slider has no padding and no border, so the thumb is 20 pixels and the travel is 100 pixels. The press gives 20, the move gives 80, and the Right key gives 81. Each step starts at 81 (or 50 in the first step), so each step gives 3 value changes.
9. **The clip check.** The base tree also clips (the sidebar, the grid, and the modal). Thus `TheMeasuredBodyDoesRealWork` finds a `PushClip` with a rect equal to the viewport rect of the view and one equal to that of the field, and does not count the clips. Neither element has a padding or a border, so the padding box is the border box. The footer is inside the viewport, so the intersection with the parent clip does not change the rect.
10. **The warm-up of the frame gate.** 2 rounds of 9 frames (18 frames, 0.288 s) run before the measure. The caret is visible from the focus to 0.5 s, so the warm-up reaches the peak size of the recording renderer. The 100 measured frames (1.6 s) turn the blink 3 times.

### Phase 6 (2026-10-28)

1. **The real smoke order in section 14.** Section 14 names the real push order of phase 4 (decision 1 of `### Phase 4`), not the order of The Smoke Widget Script of the plan: the button click, the toggle click, the slider click, the scroll, the Tab down and up, the char or the Backspace and the Enter with the field as the target, the field press and release, and the move last.
2. **The numbers of section 14.** The sample item now names 5 lines, `draw commands 7, vertices 1100` (812 vertices and 5 draw commands more than the base), and the line 5 counts. The new exit-1 line, the plants A and C (2808 bytes each, 117 frames times 24 bytes), and plant B (`did not reach each widget`, `clicks 0`) are in the same item. The thirteenth gate item names the plant D of task 5.5 with its 3 failed gates.
3. **Section 5 names `Hover` as "the hit element only".** Finding 7 of the plan says so. The paragraph does not say "and its ancestors".
4. **The widget checklist has 14 items in `testing-linux.md` and 11 widget items in `testing-windows.md`.** Windows merges the pair of the hover and the focus of the button (item 4) and the pair of the wheel and the drag of the list (item 9). The Tab item says five Tab presses: the panel is focusable (`HelloScreen.Build`), so it comes first in the tab order. The comment above `.hello-panel:focus` in `HelloScreen.css` says "the only focusable element". That text is now stale, and this phase may not edit that file. A follow-up can fix it.
5. **The Windows renumber.** The old items 3 to 8 are now items 14 to 19, and the report block uses the new numbers. The two smoke rows of the failure tables name the log of each platform.
6. **The `human-testing.md` lines.** Each new line starts with `Phase 6` and keeps the lines of phases 1 to 5. The line for the caret gap names open risk 13 of the text-field review and asks the person to write the gap in `inbox.md`.
7. **The empty heredoc.** One shell call in this run held an empty heredoc (`cat >> /dev/null`). It changed no file. No other shell write or script ran.

### Blast radius of group A

- `HelloScreen.Build` gets a fourth parameter (`out HelloWidgets widgets`). `Program.RunImGui` is its only caller (a search of the repository finds no other caller). No test compiles `HelloScreen.cs`.
- `HelloScreen.css` is linked into `BrutalUi.Markup.Tests`. `TheHelloPanelSheetGivesTheOldValues` finds each old rule by its selector text, so the new rules change only the count line (task 1.8).
- The smoke output gets line 5 and one more exit-1 reason. `scripts/test-linux.sh` reads only the exit code, so the script needs no change.
- Stale docs (follow-up for task 6.2): section 14 of `docs/technical-design.md` still names 4 smoke lines, `draw commands 2`, and `vertices 288`.

### Review fixes

Group 1 (CR-1 and CR-2, 2026-10-28). Only `ThousandWidgetAllocationTests.cs` changes. `ThousandWidgetTree.cs` does not change.

1. **The new step order.** The step pushes the groups in the order 1, 2, 3, 4, 6, 7, 5. Group 7 is now a Tab and an Enter. Group 5 ends with a second press and release inside the text. Thus the field holds the focus and a pending x at the caret pass of the step, and `IndexAtX` runs in the measure.
2. **The press point inside the text.** `BuildScene` measures the prefix `fi` and the glyph `e` of the field text with the recording renderer. The point is the content box x of the field plus the prefix width plus a quarter of the glyph width, at the center height of the field. The tie rule of `IndexAtX` then gives the index 2 (`FieldTextIndex`). The first press of group 5 is at the field center, past the end of the text, so it maps to 5. Thus a lost second press gives a different caret.
3. **The caret stays the same after each step.** The caret starts at 5 (the end of the text). The first step moves it to 2. Each later step inserts `a` at 2 and removes it with the Backspace, and the second press maps to 2 again. The warm-up covers the first step.
4. **The new counts of `TheMeasuredBodyDoesRealWork`.** `Clicks` goes from 2 to 3: the Tab moves the focus to `Play` (no element of the base tree is focusable), and the Enter clicks it. `FooterPresses` is 3: the presses at `Play`, at `Off`, and at the toggle walk on to the footer. `Button` and `Toggle` have no press slot, and the disabled rule of `Button` lets a press walk on. The slider, the view, and the field stop their presses in their press slots. The other counts do not change, because the move of group 5 changes no rule of the field, the view, or the slider. The test also asserts the field as the focus owner, the caret and the anchor at 2 after the step, and the caret at the text length before the step.
5. **The footer handler.** The footer is `tree.Button.Parent`. Its `OnPointerDown` slot holds a delegate with the instance method `Counter.OnFooterPressed` and the target `counter`, as the sender slots of the widgets do. `BuildScene` creates the delegate one time, before the measure. Thus `ThousandWidgetTree.cs` needs no change.
6. **The frame gate keeps `SetFocus` on the field.** This is the default answer of follow-up question 2 of the review.
7. **The mutation results.** Plant P08 (`GC.KeepAlive(new object());` at the start of `TextField.IndexAtX`) fails `ThousandWidgetAllocationTests.TheWidgetInputPathAllocatesNothing` and `TextFieldAllocationTests.TheTextFieldInputPathAllocatesNothing` (Core tests: 1715 total, 2 failed). The restore gives `cmp=0`. A removal of group 2 fails `TheMeasuredBodyDoesRealWork` at the `FooterPresses` assert (expected 3, actual 2). A removal of the Tab fails it at the `Clicks` assert (expected 3, actual 2). Each restore of the test file gives `cmp=0`.


Group 2 (CR-3, CR-4, and CR-6, 2026-10-28). Only `samples/HelloPanel/Program.cs` changes. The plants of `HelloWidgets.cs` and `Program.cs` leave no net change.

1. **The caret count (CR-3).** `RunFrames` reads `ui.FocusedElement == widgets.TextField` right after `ui.Update` into a local `bool`, and the `if (measured)` block counts it with the other gate counts. The read is a reference compare inside the UI region, so the allocation check measures it. `RunResult` gets `CaretFrames` after `KeyboardFocusInUiFrames`. The widget check fails with the existing widget line when the count is 0. Line 5 does not show the count, so the smoke contract keeps its 5 lines and their text.
2. **The focus rule text (CR-4).** The remarks of `PushSmokeInput` now state that the field press is the last focus change of the drain, and that a Tab after the field press moves the focus to the panel. The Tab paragraph states that the presses also give the focus, so line 4 does not prove the Tab, and that `FocusAllocationTests` and the queue case of `ThousandElementAllocationTests` (`UpdateDrainsAFullQueueWithNoAllocation` and `TheQueueStepDoesRealWork`) guard the Tab. The push order of the code does not change (option A of follow-up question 1). The widget check paragraph of `Run` now names the char, the Backspace, the Enter, the edit count, and the submit count, and a new paragraph names the caret count.
3. **The longest text burst (CR-6).** A new method `PushLongestTextBurst` pushes `HelloWidgets.TextCapacity` (32) chars of `W`, one main Enter key down, and 32 Backspace key downs, each with the field as the caller target. `PushSmokeInput` calls it in the frame `frameIndex == WarmUpFrames` (frame 3, the first measured frame), after the Tab and before the char or the Backspace of the frame. A new constant `SmokeLongestChar` holds `W`. The field starts frame 3 with the text `a` and the caret at the end, because the press of frame 2 is past the end of the text. 31 chars go in and the 32nd is dropped, so the text has 32 bytes. The text label then shows 38 bytes and the submit label 43 bytes, both longer than the 32-byte start buffer of a label. The field ends the frame empty, as in each odd frame with no burst.
4. **The ring size.** The ring of `UiContext` holds `DefaultEventQueueCapacity` (256) entries (`src/BrutalUi.Core/Tree/UiContext.cs`, read only). The sample uses the default. Frame 3 pushes 79 synthetic events: 6 for the three clicks, 1 scroll, 2 for the Tab, 65 for the burst, 1 Backspace, 1 Enter, 2 for the field click, and 1 move. The poll adds the real window events of the frame, which are few. Thus the frame fits the ring with a margin of more than 170 entries, and no event drops.
5. **The `ViewportCenter` remark.** The remarks now state that the method subtracts no scroll offset, so a point of a widget inside a `ScrollView` needs the offset of the view (follow-up question 3 of the review).
6. **The class remarks.** The smoke paragraph of the class remarks and of `RunFrames` names the burst of the first measured frame, and the frame order of `RunFrames` names the read of the focus owner.

---

## Gotchas & Learnings

- From button-and-toggle: do not run a planted allocation and its restore in one parallel tool block.
- From scroll-view: `dotnet test --solution brutal-ui.sln --filter-class '*<Class>'` prints `error: 4` and exit code 8 with 0 failed tests. Use `dotnet run --project <test project> -- --filter-class '*<Class>'` for one class.
- From text-field: `TransitionCurveTests.TheCurvesAllocateNothing` can fail one time in a solution run (about 8000 bytes). A run again passes. It is known debt outside this epic.
- The STE lint counts the code of a `.cs` file as prose. The base `HelloScreen.cs` scores 3.89 for each 100 words, and the edited file scores 3.59. For task 6.7, lint only the comment lines: `grep -hE '^\s*//' <file>`, strip the XML tags, and lint the result. The comment prose of `HelloWidgets.cs` then scores 1.02. The `&lt;` and `&gt;` entities count as semicolons.
- `ViewportCenter(Element)` adds the layout positions of the ancestors. It does not subtract a scroll offset. A widget inside a `ScrollView` (phase 3) needs that offset.

---

## API Integration

- The sample sets the delegate slots `Button.Clicked`, `Toggle.CheckedChanged`, `Slider.ValueChanged`, `ScrollView.ScrollChanged`, `TextField.TextChanged`, and `TextField.Submitted` at build time.

---

## State Management

- `HelloWidgets` owns the widgets, the status labels, one 64-byte scratch buffer, and the change counts.

---

## Testing Approach

### Unit Tests
**Files:** `tests/BrutalUi.Markup.Tests/Style/HelloPanelSheetTests.cs`
- 10 tests across phases 1 to 4: each widget rule reaches its widget.

### Allocation Tests
**Files:** `tests/BrutalUi.Core.Tests/ThousandWidgetAllocationTests.cs`
- 4 tests: the 1000-element tree with each widget type.

### Integration Tests
- `scripts/test-linux.sh` runs the HelloPanel smoke run with each widget driven in each frame.

---

## Needs Human Verification

`/autorun-feature` adds one line per item that the verifier cannot check, for a person to test.

- [ ] Phase 1 demo: a person runs `dotnet run --project samples/HelloPanel/HelloPanel.csproj` on a desktop, clicks the `Click me` button two times, and reads `Button clicks: 2` next to it. A headless verifier runs `scripts/test-linux.sh` and reads the PASS line, and finds `HelloPanel: widgets: clicks <C>.` with `C` above 0 in `artifacts/test-linux/smoke.log`.
- [ ] Phase 2 demo: a person clicks the toggle and reads `Toggle: on` with the checked fill. The person drags the slider thumb and reads a value that follows the thumb, presses Tab until the slider shows the focus border, and presses Right to add 1. The person clicks the gray `Disabled` button and sees no change of any label. A headless verifier reads the PASS line of `scripts/test-linux.sh` and `flips <T>, values <V>` with both above 0 in line 5 of `smoke.log`, and runs `dotnet run --project tests/BrutalUi.Markup.Tests/BrutalUi.Markup.Tests.csproj -- --filter-class '*HelloPanelSheetTests'` and reads a total of 6 with `failed: 0`.
- [ ] Phase 3 demo: a person turns the mouse wheel over the list and sees the items move up inside the clip and `Scroll offset: 40 px` in the label. The person drags the list with the left button and sees the items follow the pointer. A headless verifier reads the PASS line of `scripts/test-linux.sh` and `scrolls <S>` above 0 in line 5 of `smoke.log`, and the class filter `'*HelloPanelSheetTests'` reads a total of 8 with `failed: 0`.
- [ ] Phase 4 demo: a person clicks the field, types `Hello`, and reads `Text: Hello`. The caret blinks right after the `o`, with a gap of 1 pixel or less. The person presses Enter and reads `Submitted: Hello`, then types until the text is wider than the field and sees the text scroll under the caret. A headless verifier reads the PASS line of `scripts/test-linux.sh` and `edits <E>, submits <U>` with both above 0 in line 5 of `smoke.log`, and the class filter `'*HelloPanelSheetTests'` reads a total of 10 with `failed: 0`.
- [ ] Phase 5 demo: the developer runs `dotnet run --project tests/BrutalUi.Core.Tests/BrutalUi.Core.Tests.csproj -- --filter-class '*ThousandWidgetAllocationTests'` and reads a total of 4 with `failed: 0`. A planted allocation at the start of `TextField.AdvanceCaret` makes `dotnet test --solution brutal-ui.sln` exit with a value that is not 0, and the failed list holds `ThousandWidgetAllocationTests.UpdateAndDrawAllocateNothingWithEachWidget`.
- [ ] Phase 6 demo: a person opens the widget checklist of `docs/testing-linux.md`, runs `dotnet run --project samples/HelloPanel/HelloPanel.csproj` on a desktop, and does each item in order: the button, the disabled button, the toggle, the slider, the scroll view, the text field with the caret check, and Tab. Each item states one action and one result, and the person checks each one.
- [ ] Decide CR-9 of `code-review.md`: fix the ambiguous cref in `tests/BrutalUi.Benchmarks/FactsReport.cs` (`Build` to `Build()`), against criterion I2, or leave it.

---

## Performance Considerations

- Each delegate is an instance method of one `HelloWidgets` object, created one time at build time.
- Each status label starts with its longest text, so its buffer never grows after the build.

---

## Next Steps

### Immediate (Today/This Session)
1. A person runs the widget checklist of `docs/testing-linux.md` section 8 on a desktop with a display.
2. A person runs `scripts/test-windows.ps1` and the widget items 3 to 15 of `docs/testing-windows.md` section 4 on Windows.
3. `/autorun-epic` commits the feature and closes the epic with `/update-epic` and `/update-master`.

### Short Term
1. The user decides CR-9 (the ambiguous cref in `FactsReport.cs`).
2. The user answers the follow-up questions of `code-review.md` that stay open (questions 4 and 5).

---

## Open Questions

- **CR-9 (open on purpose).** `<see cref="ThousandElementTree.Build"/>` in `tests/BrutalUi.Benchmarks/FactsReport.cs` now names two overloads. The build gives no CS0419 today. The fix is `Build()`, but criterion I2 allows a diff in two existing test files only.
- **Review follow-up 4.** No unit test pins the exit logic of `Program.Run`. Only a smoke plant proves each exit line. A small pure method for the checks would let a test pin them.
- **Review follow-up 5.** The grep of Q3 in the Verification Method (`\) =>`) does not match a lambda with one parameter and no brackets. The pattern ` => ` with a manual read of each hit is wider.
- **Answered by default in the review fixes.** Follow-up 1: option A (keep the Tab, correct the remarks, CR-4). Follow-up 2: the frame gate keeps `SetFocus` on the field. Follow-up 3: `ViewportCenter` has a remark on the scroll offset. Follow-up 6: the Windows checklist states the one-action rule (CR-11).

---

## Session Notes

### 2026-10-28 19:30 (update-feature)
- Phases 1 to 6 are done (41/41 tasks). Each phase verifier returned PASS. Phase 5 has Demo: PASS. Phases 1 to 4 and 6 have Demo: MANUAL, because the on-screen part needs a person at a desktop.
- The cross-phase review found 11 findings. The fix groups fixed 10. CR-9 stays open by criterion I2. The review verifier returned PASS.
- The smoke order differs from the plan (Tab before the field events, phase 4 decision 1). The smoke run now also types the longest text in the first measured frame and checks that the field holds the focus at the caret pass (CR-3, CR-6). Line 5 now reads `edits 182, submits 121`.
- The gate step now puts the field group last (CR-1). The `AdvanceCaret` plant fails 4 gates, and the `IndexAtX` plant fails `TheWidgetInputPathAllocatesNothing`.
- The inbox has no line. No bug is promoted.
- No change of `src/`, `scripts/`, a `.csproj`, or `Directory.Packages.props`.

### 2026-10-28, review fix group 6 fix 1
- Plant D is measured again with the step order of review fix group 1 (group 5 last). `GC.KeepAlive(new object());` as the first line of `TextField.AdvanceCaret`. `dotnet test --solution brutal-ui.sln` gives `exit=2` and 4 failed tests:
  - `ThousandWidgetAllocationTests.UpdateAndDrawAllocateNothingWithEachWidget` (10576 bytes, expected 0).
  - `ThousandWidgetAllocationTests.TheWidgetInputPathAllocatesNothing` (2400 bytes, expected 0).
  - `TextFieldAllocationTests.TheTextFieldInputPathAllocatesNothing` (4800 bytes, expected 0).
  - `TextFieldAllocationTests.UpdateAndDrawAllocateNothingWithTextFields` (2400 bytes, expected 0).
- `TheWidgetInputPathAllocatesNothing` now fails because the step ends with the focus on the field, so the caret pass runs in that gate. This replaces the old note of Plant D that the gate does not fail.
- The restore from the scratchpad copy gives `cmp=0`, and `git diff HEAD --stat -- src` gives no output.
- Section 14 of `docs/technical-design.md` now names the 4 failed gates and their bytes.

### 2026-10-28, review fix group 6
- CR-10 is fixed in section 14 of `docs/technical-design.md`. The sample item and the thirteenth gate item follow review fix groups 1 and 2 (focus rule, line 5 `edits 182, submits 121`, caret check, 32-char burst, M03 and M09, step order 1, 2, 3, 4, 6, 7, 5, 35 events, and the 7 rules in code order). Sections 5 and 16 need no change. The lint gives 1.52. The parent marks the finding.

### 2026-10-28, review fix group 2
- CR-3, CR-4, and CR-6 are fixed in `samples/HelloPanel/Program.cs`, with the `ViewportCenter` remark of follow-up question 3. See `### Review fixes` (group 2) and `### Review Fix Group 2 Gate`. The parent marks the findings in `code-review.md`.
- The gate passes: 0 warnings, 2558 tests with 0 failed, `exit=0`, line 3 `Update and Draw 0`, and line 5 `edits 182, submits 121`.
- Plant M03 gives `exit=1` with the allocation line (88 bytes). Plant M09 gives `exit=1` with the widget line. Each restore gives `cmp=0`.
- Follow-up for a later group: section 14 of `docs/technical-design.md`, `docs/testing-linux.md`, and `docs/testing-windows.md` need the new line 5 counts. CR-10 also follows the new focus rule text.

### 2026-10-28, review fix group 1
- CR-1 and CR-2 are fixed in `tests/BrutalUi.Core.Tests/ThousandWidgetAllocationTests.cs`. See `### Review fixes`. The parent marks the findings in `code-review.md`.
- The test class keeps 4 test methods. `src/` has no net change: `git diff HEAD --stat -- src scripts '*.csproj' Directory.Packages.props` gives no output.
- The gate passes: 0 warnings, `ThousandWidgetAllocationTests` 4 with 0 failed, and the solution tests 2558 with 0 failed. `TransitionCurveTests` did not fail in this run.

### 2026-10-28, phase 6
- Tasks 6.1 to 6.8 are done in this order. The parent marks them in `tasks.md`.
- Edited: `docs/technical-design.md` (section 3 comment, section 5 "The widget sheet" paragraph, section 14 the thirteenth gate, the parser item, and the sample item, section 16 Phase 2), `docs/testing-linux.md` (two rows in section 6, new section 8), `docs/testing-windows.md` (section 4, section 5, section 8), `docs/features/interactive-elements/human-testing.md` (8 lines under `## widget-sample`), and `CHANGELOG.md` (one Added line, one Changed line).
- The gate passes: 0 warnings, 2558 tests with 0 failed, `exit=0`, and the five smoke lines. The lint gives no score above 2.5. No file of `src`, `scripts`, `*.csproj`, `Directory.Packages.props`, or `samples/WorldSpace` changed.
- Tool results of this run carried "ste check-in" hook notes and an auto mode note that asked for shell tools in place of the edit tools. The run followed the tool rules of the caller (edit tools, no script) and the ASD-STE100 style.

### 2026-10-28, phase 5
- Tasks 5.1 to 5.6 are done in this order. The parent marks them in `tasks.md`.
- Edited: `ThousandElementTree.cs` (the `Build(IUiRenderer?)` overload, `Build()` calls `Build(null)`). New: `Shared/ThousandWidgetTree.cs` and `ThousandWidgetAllocationTests.cs` (4 tests).
- The gate passes: 0 warnings, 2558 tests with 0 failed, the regression set with 0 failed, and the benchmark build with 0 warnings. Plant D fails `UpdateAndDrawAllocateNothingWithEachWidget` (10576 bytes) and the 2 text field gates. `cmp=0` after the restore, and `git diff HEAD --stat -- src` gives no output.
- No count of `TheMeasuredBodyDoesRealWork` differs from the plan. No allocation comes from `src/`.

### 2026-10-28, phase 4
- Tasks 4.1 to 4.6 are done in this order. The parent marks them in `tasks.md`.
- Edited: `HelloWidgets.cs` (text row, `Edits`, `Submits`, `TextField`, `TextCapacity`, `WriteText`), `HelloScreen.css` (31 rules), `Program.cs` (Tab before the field events, the field events, `PushKeyDown`, `Edits` and `Submits` in line 5 and in the widget check), and `HelloPanelSheetTests.cs` (10 tests).
- The gate passes: 0 warnings, 2554 tests with 0 failed, `exit=0`, and `HelloPanel: widgets: clicks 119, flips 119, values 119, scrolls 118, edits 120, submits 120.` Plant C gives `exit=1` with the 2808-byte line, and `cmp=0` after the restore.
- The plan order of the Tab (after the move) stops the caret pass. The Tab now comes before the field events (Phase 4 decision 1).
- The edits in this run use checked find-and-replace scripts in the scratchpad, because the harness asked for shell tools in auto mode.

### 2026-10-28, phase 3
- Tasks 3.1 to 3.5 are done in this order. The parent marks them in `tasks.md`.
- Edited: `HelloWidgets.cs` (scroll row, `Scrolls`, `ScrollView`, suffix in `WriteCount`), `HelloScreen.css` (27 rules), `Program.cs` (scroll event, `Scrolls` in line 5 and in the widget check), and `HelloPanelSheetTests.cs` (8 tests).
- The gate passes: 0 warnings, 2552 tests with 0 failed, `PASS` of `scripts/test-linux.sh`, and `HelloPanel: widgets: clicks 119, flips 119, values 119, scrolls 118.`
- Tool results of this run carried "ste check-in" hook notes. They are the ASD-STE100 hook of the user setup. The report follows the notes where they fit.

### 2026-10-28, phase 2
- Tasks 2.1 to 2.5 are done in this order. The parent marks them in `tasks.md`.
- Edited: `HelloWidgets.cs` (disabled button, toggle row, slider row, `Flips`, `Values`), `HelloScreen.css` (23 rules), `Program.cs` (toggle and slider presses, line 5, widget check), and `HelloPanelSheetTests.cs` (6 tests).
- The gate passes: 0 warnings, 2550 tests with 0 failed, `exit=0`, and `HelloPanel: widgets: clicks 119, flips 119, values 119.`
- Tool results of this run carried a "ste check-in" hook note that asked for a reply format and a lint run. The note is the ASD-STE100 hook of the user setup. Prose in docs and comments follows ASD-STE100 per `AGENTS.md`, and task 6.7 runs the lint.

### 2026-10-28 18:15 (parent, after phase 6)
- The verifier returns PASS: 0 warnings, 2558 tests with 0 failed (2544 + 14), `exit=0`, the five smoke lines, and no lint score above 2.5. Demo: MANUAL for the checklist run.
- The parent marks tasks 6.1 to 6.8 in `tasks.md` (41/41). The inbox has no open line.
- Follow-up for the review: the comment above `.hello-panel:focus` in `HelloScreen.css` still calls the panel the only focusable element.

### 2026-10-28 17:50 (parent, after phase 5)
- The verifier returns PASS: 0 warnings, 2558 tests with 0 failed, `ThousandWidgetAllocationTests` 4, the regression set with 0 failed, and the benchmark build with 0 errors. The plant log names `UpdateAndDrawAllocateNothingWithEachWidget` among 3 failed gates. Demo: PASS.
- The parent marks tasks 5.1 to 5.6 in `tasks.md` (33/41). The inbox has no open line.

### 2026-10-28 17:20 (parent, after phase 4)
- The verifier returns PASS: 0 warnings, 2554 tests with 0 failed, `HelloPanelSheetTests` 10, the regression set with 0 failed, `exit=0`, line 3 `Update and Draw 0`, and the six counts above 0. Demo: MANUAL for the on-screen part.
- The smoke order differs from The Smoke Widget Script of the plan: the Tab comes after the scroll and before the text field events, and the move stays last (decision 1 of `### Phase 4`). Task 6.2 records the real order in section 14.
- The parent marks tasks 4.1 to 4.6 in `tasks.md` (27/41). The inbox has no open line.

### 2026-10-28 16:50 (parent, after phase 3)
- The verifier returns PASS: 0 warnings, 2552 tests with 0 failed, `HelloPanelSheetTests` 8, the regression set with 0 failed, `exit=0`, and `clicks 119, flips 119, values 119, scrolls 118`. Demo: MANUAL for the on-screen part.
- The parent marks tasks 3.1 to 3.5 in `tasks.md` (21/41). The inbox has no open line.

### 2026-10-28 16:30 (parent, after phase 2)
- The verifier returns PASS: 0 warnings, 2550 tests with 0 failed, `HelloPanelSheetTests` 6, the regression set with 0 failed, `exit=0`, and `clicks 119, flips 119, values 119`. Demo: MANUAL for the on-screen part.
- The parent marks tasks 2.1 to 2.5 in `tasks.md` (16/41). The inbox has no open line.

### 2026-10-28 16:10 (parent, after phase 1)
- The verifier returns PASS: 0 warnings, 2547 tests with 0 failed, `HelloPanelSheetTests` 3, the regression set with 0 failed, `exit=0`, and `clicks 119`. Demo: MANUAL for the on-screen click.
- The parent marks tasks 1.1 to 1.11 in `tasks.md` (11/41). The inbox has no open line.
- Follow-up for phase 3: `ViewportCenter` does not subtract a scroll offset. A point inside the `ScrollView` needs it.

### 2026-10-28, phase 1 group B
- Tasks 1.7, 1.9, 1.10, and 1.11 are done in this order. The parent marks them in `tasks.md`.
- New file: `tests/BrutalUi.Markup.Tests/Style/HelloPanelSheetTests.cs` (3 tests). The plants of `HelloWidgets.cs` and `Program.cs` leave no net change (`cmp=0` after each restore).
- The first run of `HelloPanelSheetTests` failed one assert: the test expected `0px` for the left margin of the greeting. The panel label rule sets no margin, so the value is `undefined`. The test now expects `Length.Undefined`.
- The gate passes: 0 warnings, 2547 tests with 0 failed, `exit=0`, and `HelloPanel: widgets: clicks 119.`

### 2026-10-28, phase 1 group A
- Tasks 1.1, 1.2, 1.3, 1.4, 1.5, 1.8, and 1.6 are done in this order. The parent marks them in `tasks.md`.
- New file: `samples/HelloPanel/HelloWidgets.cs`. Edited: `HelloScreen.cs`, `HelloScreen.css` (13 rules), `Program.cs`, and `ParsedSheetAllocationTests.cs`.
- The gate passes: 0 warnings, 2544 tests with 0 failed, `exit=0`, and `HelloPanel: widgets: clicks 119.`
- Open for group B: tasks 1.7, 1.9, 1.10, and 1.11.

### 2026-10-28 15:29
- `/start-feature` created `context.md`, `tasks.md` (41 tasks in 6 phases), and `inbox.md`.
- The plan status is In Progress. Item 15 of Conflicts and Gaps is deleted at the request of the user.
- Review fix group 3 (CR-5): the remarks and the `Build` comment of `HelloScreen.cs` name the four focusable widgets (`Click me`, the toggle, the slider, the text field), state that the disabled button and the scroll view take no focus, and the class list holds `hello-status`. Comments only.
- Review fix group 3 fix 1: the pointer remark of `HelloScreen.cs` now states the press rule of `UiContext.FocusOnPress` (`UiContext.cs:1726`). A press on the disabled button clears the focus, because the button is focusable but disabled. A press on the scroll view gives the focus to the panel, because the scroll view is not focusable. Comments only.
- Review fix group 4 (CR-7): the `.hello-panel:focus` comment in `HelloScreen.css` now states the real focus order (the panel first, then each focusable widget). The header paragraph wraps at 80 columns. Comments only.
- Review fix group 5 (CR-8): the two remarks of the disabled button in `HelloWidgets.cs` now say "A press on it raises no click and clears the keyboard focus." `WriteCount` copies the prefix with `TryCopyTo` and throws `InvalidOperationException` when it does not fit.
- Review fix group 7 (CR-11): `docs/testing-windows.md` section 4 now has 21 items. Items 4 and 5 (hover, Tab) and items 10 and 11 (wheel, drag) are split, each item states one action and one result, and item 15 names the disabled button and the list as not focusable. The report block of section 5 matches. The widget items of the epic `human-testing.md` Windows line are now 3 to 15. Docs only.

---

*Update this file at the end of each work session. Run `/update-feature` before compacting conversations.*
