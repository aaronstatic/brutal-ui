# interactive-elements/widget-sample - Task Checklist

**Last Updated:** 2026-10-28 19:30
**Progress:** 41/41 tasks complete (100%)
**Advanced phases:** Phases 1, 4, and 5 (ADVANCED, opus). Phases 2, 3, and 6 are STANDARD (sonnet).

"The plan" means `implementation.md` of this feature. Its sections "Architecture Overview" (Codebase Findings, The Sample Tree, The Widget Status Rule, The Smoke Widget Script, The Rule Matrix, The Gate Tree) and "Key Technical Decisions" hold the rules that the tasks name.

Each phase ends with `dotnet build brutal-ui.sln 2>&1 | tail -n 30`, `dotnet test --solution brutal-ui.sln 2>&1 | tail -n 30`, and `scripts/test-linux.sh; echo "exit=$?"` (tool rule 5). Expect `0 Warning(s)`, `0 Error(s)`, 0 failed, the PASS line, and `exit=0`. After the script, read `artifacts/test-linux/smoke.log` and record its `HelloPanel:` lines in `context.md`. The projects have no implicit usings, so each new file holds its own `using` lines. Each public member needs an XML doc comment (CS1591 is an error). The sample uses `namespace HelloPanel;`. The Markup tests and the Core tests use the namespace of their existing files. Prose in comments follows ASD-STE100. Write each sample text as a `u8` literal (the analyzer `BRUTALCORE0011` rejects `SetText("x")`). Run one Markup class with `dotnet run --project tests/BrutalUi.Markup.Tests/BrutalUi.Markup.Tests.csproj -- --filter-class '*<Class>' 2>&1 | tail -n 5`.

**The stop rule of the plan.** No phase changes `src/`. When the smoke run or a gate reports an allocation that comes from `src/`, do not fix it in this feature. Record the bytes, the plant or the bisect that finds the path, and the file in `context.md`, and stop the phase.

**The regression set of each phase (no edit):** `ParsedSheetAllocationTests`, `WidgetAllocationTests`, `SliderAllocationTests`, `ScrollViewAllocationTests`, `TextFieldAllocationTests`, `ThousandElementAllocationTests`, `PointerAllocationTests`, `FocusAllocationTests`, and `ImGuiRendererAllocationTests`.

---

## Phase 1: A Click on the Button Shows in a Label (11/11 complete)

**Demo:** a person runs `dotnet run --project samples/HelloPanel/HelloPanel.csproj` on a desktop, clicks the `Click me` button two times, and reads `Button clicks: 2` next to it. A headless verifier runs `scripts/test-linux.sh` and reads the PASS line, and finds `HelloPanel: widgets: clicks <C>.` with `C` above 0 in `artifacts/test-linux/smoke.log`.
**Needs:** none
**Agent tier:** ADVANCED (opus)

*Group A (the sample):*

- [x] ✅ **1.1 Record the base and the counts** - Completed 2026-10-28
  - Description: Record in `docs/features/interactive-elements/widget-sample/context.md`, before any change: the base commit (`git rev-parse HEAD`), the result of `git ls-files src/BrutalUi.Core/Elements/TextField.cs` (expect one line), and `git status --porcelain -- src tests samples` (expect no line). When text-field is not in the base commit, stop and record it. Run `scripts/test-linux.sh` and record the 4 lines of `artifacts/test-linux/smoke.log`. Record the test counts of the solution and of each test project, and the Codebase Findings.
  - File(s): `docs/features/interactive-elements/widget-sample/context.md`
  - Estimate: 6 steps

- [x] ✅ **1.2 Write HelloWidgets.cs (the section and the Button row)** - Completed 2026-10-28
  - Description: Write `samples/HelloPanel/HelloWidgets.cs`: `internal sealed class HelloWidgets`. The constructor builds `Section` (class `hello-widgets`) and row 1 (class `hello-row`) with `ClickButton` (`new Button(new Button.CreateInfo { Text = "Click me" })`) and the status label (not pickable, longest text first). It creates `_scratch = new byte[64]`, sets `ClickButton.Clicked` from a cached field that holds `OnClicked`, and exposes `Section`, `ClickButton`, and `Clicks`. The class ids `hello-widgets` and `hello-row` are `static readonly ClassId` fields. `OnClicked(Button)` adds 1 to `Clicks` and writes `Button clicks: <n>` (The Widget Status Rule). The class remarks state: the status rule, the longest text rule, "one delegate object, created at build time", and "copy this file with `HelloScreen.cs` and `HelloScreen.css`".
  - File(s): `samples/HelloPanel/HelloWidgets.cs`
  - Estimate: 8 steps

- [x] ✅ **1.3 Add the section to HelloScreen.Build** - Completed 2026-10-28
  - Description: Edit `samples/HelloPanel/HelloScreen.cs`: `Build` gets `out HelloWidgets widgets` and adds `widgets.Section` as the last child of the panel, after the image. Edit the class remarks and the `Build` remarks: the panel is no longer the only focusable element, the Tab order, the widget section, the child rules of the labels, and the class names of the sheet.
  - File(s): `samples/HelloPanel/HelloScreen.cs`
  - Estimate: 4 steps

- [x] ✅ **1.4 Add the layout and Button rules to HelloScreen.css** - Completed 2026-10-28
  - Description: Edit `samples/HelloPanel/HelloScreen.css`: add the variables of the widget colors to `:root`, and the rules `.hello-widgets`, `.hello-row`, `.hello-row > Label`, `Button`, `Button:hover`, `Button:active`, and `Button:focus` (The Rule Matrix). Do not change the 6 old rules. Extend the file comment: the widget rules, the fill rule for hover and active, and the border rule for focus. The sheet then holds 13 rules.
  - File(s): `samples/HelloPanel/HelloScreen.css`
  - Estimate: 4 steps

- [x] ✅ **1.5 Drive the button in the smoke run and print line 5** - Completed 2026-10-28
  - Description: Edit `samples/HelloPanel/Program.cs`: pass `widgets` from `Build` to `RunFrames` and to `PushSmokeInput`. Add the helper `ViewportCenter(Element)` (the walk of today, moved). Push the Left press and release at `ClickButton` before the move (The Smoke Widget Script). Add `Clicks` to `RunResult`. `Run` writes line 5 and runs the widget check after the input check. Edit the remarks of the class, `Run`, `RunFrames`, and `PushSmokeInput`: 5 lines, the widget script, the order rule, and the new exit-1 reason.
  - File(s): `samples/HelloPanel/Program.cs`
  - Estimate: 8 steps

- [x] ✅ **1.6 Run the group A gate** - Completed 2026-10-28
  - Description: Run the build, the tests, and `scripts/test-linux.sh`. Record line 5 and the new draw command and vertex counts of line 2 in `context.md`.
  - File(s): `docs/features/interactive-elements/widget-sample/context.md`
  - Estimate: 4 steps

*Group B (the tests and the plants):*

- [x] ✅ **1.7 Write HelloPanelSheetTests.cs (3 tests)** - Completed 2026-10-28
  - Description: Write `tests/BrutalUi.Markup.Tests/Style/HelloPanelSheetTests.cs` with 3 tests. Each test parses the linked `HelloScreen.css` with `StyleSheetParser` (the shape of `TheHelloPanelSheetGivesTheOldValues`). A private measurer gives 8 pixels for each byte and a line of 16 pixels. `TheSheetHoldsEachRule` (no error, 13 rules, and each selector text of the sheet occurs one time). `TheButtonRulesStyleTheButton` (a root `.hello-root`, a panel `.hello-panel`, a section `.hello-widgets`, a row `.hello-row`, and a `Button`. After `Update(0f)`: the base fill, the padding, and the color. With `States = Hover`: the hover fill and the base border color. With `Active`: the active fill. With `Focus`: the focus border color and the base fill. With `Hover | Focus`: the hover fill and the focus border color). `TheRowLabelRuleKeepsThePanelLabelRule` (a `Label` in the row has 16 px, and a `Label` that is a direct child of the panel keeps 26 px).
  - File(s): `tests/BrutalUi.Markup.Tests/Style/HelloPanelSheetTests.cs`
  - Estimate: 8 steps

- [x] ✅ **1.8 Remove the rule count line of ParsedSheetAllocationTests** - Completed 2026-10-28 (in group A, before the gate of task 1.6)
  - Description: Edit `tests/BrutalUi.Markup.Tests/Style/ParsedSheetAllocationTests.cs`: remove the line `Assert.Equal(6, sheet.Rules.Length);` only. Put a comment in its place: "`HelloPanelSheetTests.TheSheetHoldsEachRule` pins the rule count. The widget rules of widget-sample come after the 6 old rules." Change no other line.
  - File(s): `tests/BrutalUi.Markup.Tests/Style/ParsedSheetAllocationTests.cs`
  - Estimate: 2 steps

- [x] ✅ **1.9 Plant A (an allocation in OnClicked)** - Completed 2026-10-28
  - Description: Copy `samples/HelloPanel/HelloWidgets.cs` to the scratchpad. Add `GC.KeepAlive(new object());` as the first line of `OnClicked`. Run `scripts/test-linux.sh; echo "exit=$?"`. Expect `FAIL at smoke (exit 1)` and the line `HelloPanel: Update and Draw allocated <B> bytes after warm-up.` in the tail. In a later tool call, restore the file from the copy and check it with `cmp`.
  - File(s): `samples/HelloPanel/HelloWidgets.cs` (planted and restored)
  - Estimate: 4 steps

- [x] ✅ **1.10 Plant B (no button press in the smoke script)** - Completed 2026-10-28
  - Description: Copy `samples/HelloPanel/Program.cs` to the scratchpad. Remove the press and the release of `ClickButton` from `PushSmokeInput`. Run `scripts/test-linux.sh; echo "exit=$?"`. Expect `FAIL at smoke (exit 1)` and `HelloPanel: the synthetic input did not reach each widget.` In a later tool call, restore the file and check it with `cmp`. Record both plants in `context.md`.
  - File(s): `samples/HelloPanel/Program.cs` (planted and restored), `docs/features/interactive-elements/widget-sample/context.md`
  - Estimate: 5 steps

- [x] ✅ **1.11 Run the phase 1 gate** - Completed 2026-10-28
  - Description: Run the build, the tests, the new class, the regression set, and `scripts/test-linux.sh` again. Expect the PASS line. Run `git diff <base> --stat -- src scripts '*.csproj' Directory.Packages.props` and expect no output.
  - File(s): `docs/features/interactive-elements/widget-sample/context.md`
  - Estimate: 4 steps

---

## Phase 2: The Toggle, the Slider, and a Disabled Button (5/5 complete)

**Demo:** a person clicks the toggle and reads `Toggle: on` with the checked fill. The person drags the slider thumb and reads a value that follows the thumb, presses Tab until the slider shows the focus border, and presses Right to add 1. The person clicks the gray `Disabled` button and sees no change of any label. A headless verifier reads the PASS line of `scripts/test-linux.sh` and `flips <T>, values <V>` with both above 0 in line 5 of `smoke.log`, and runs `dotnet run --project tests/BrutalUi.Markup.Tests/BrutalUi.Markup.Tests.csproj -- --filter-class '*HelloPanelSheetTests'` and reads a total of 6 with `failed: 0`.
**Needs:** Phase 1
**Agent tier:** STANDARD (sonnet)

- [x] ✅ **2.1 Add the disabled button, the Toggle row, and the Slider row** - Completed 2026-10-28
  - Description: Edit `samples/HelloPanel/HelloWidgets.cs`: add the disabled button (`Text = "Disabled"`) as the last child of row 1, row 2 (`Toggle` and its label), and row 3 (`Slider` with `Minimum = 0`, `Maximum = 100`, `Step = 1`, `Value = 50`, and its label). Add `OnCheckedChanged(Toggle)` and `OnValueChanged(Slider)`, the counts `Flips` and `Values`, and the properties `Toggle`, `Slider`, and `DisabledButton`. Extend the remarks: the disabled widget shows `:disabled`, and a press on it changes nothing.
  - File(s): `samples/HelloPanel/HelloWidgets.cs`
  - Estimate: 6 steps

- [x] ✅ **2.2 Add the Toggle, Slider, and disabled rules** - Completed 2026-10-28
  - Description: Edit `samples/HelloPanel/HelloScreen.css`: add `Button:disabled`, `Toggle`, `Toggle:hover`, `Toggle:active`, `Toggle:focus`, `Toggle:checked`, `Slider`, `Slider:hover`, `Slider:active`, and `Slider:focus` (The Rule Matrix). The sheet then holds 23 rules. Note in the file comment: the four widgets with no own size get their size here.
  - File(s): `samples/HelloPanel/HelloScreen.css`
  - Estimate: 4 steps

- [x] ✅ **2.3 Drive the toggle and the slider in the smoke run** - Completed 2026-10-28
  - Description: Edit `samples/HelloPanel/Program.cs`: push the toggle press and release and the slider press and release before the move (The Smoke Widget Script). Add `Flips` and `Values` to `RunResult`, to line 5, and to the widget check. Edit the remarks.
  - File(s): `samples/HelloPanel/Program.cs`
  - Estimate: 5 steps

- [x] ✅ **2.4 Add the toggle, slider, and disabled sheet tests** - Completed 2026-10-28
  - Description: Edit `tests/BrutalUi.Markup.Tests/Style/HelloPanelSheetTests.cs`: the count 23 in `TheSheetHoldsEachRule`, and 3 tests. `TheToggleRulesStyleTheToggle` (the size 20 x 20 in `Layout.Rect` after `Update`, the base fill, `Checked = true` gives the checked fill, `Checked` with `Hover` keeps the checked fill, and `Focus` gives the focus border color). `TheSliderRulesStyleTheSlider` (the size 200 x 20, the track fill, the thumb `Color`, the hover and active thumb colors, and the focus border color). `TheDisabledRuleWinsOverHover` (a `Button` with `Disabled | Hover` and with `Disabled | Active` has the disabled fill and the disabled color).
  - File(s): `tests/BrutalUi.Markup.Tests/Style/HelloPanelSheetTests.cs`
  - Estimate: 7 steps

- [x] ✅ **2.5 Run the phase 2 gate** - Completed 2026-10-28
  - Description: Run the build, the tests, the class, the regression set, and `scripts/test-linux.sh`. Record line 5 in `context.md`.
  - File(s): `docs/features/interactive-elements/widget-sample/context.md`
  - Estimate: 4 steps

---

## Phase 3: The Scroll View Moves Content Taller than the View (5/5 complete)

**Demo:** a person turns the mouse wheel over the list and sees the items move up inside the clip and `Scroll offset: 40 px` in the label. The person drags the list with the left button and sees the items follow the pointer. A headless verifier reads the PASS line of `scripts/test-linux.sh` and `scrolls <S>` above 0 in line 5 of `smoke.log`, and the class filter `'*HelloPanelSheetTests'` reads a total of 8 with `failed: 0`.
**Needs:** Phase 2
**Agent tier:** STANDARD (sonnet)

- [x] ✅ **3.1 Add the ScrollView row** - Completed 2026-10-28
  - Description: Edit `samples/HelloPanel/HelloWidgets.cs`: add row 4 with the `ScrollView` (`default(ScrollView.CreateInfo)`), its 8 item labels, and the status label. Add `OnScrollChanged(ScrollView)`, the count `Scrolls`, and the property `ScrollView`. Extend the remarks: the content is taller than the view, and the items are not pickable, so a press drags the list.
  - File(s): `samples/HelloPanel/HelloWidgets.cs`
  - Estimate: 5 steps

- [x] ✅ **3.2 Add the ScrollView rules** - Completed 2026-10-28
  - Description: Edit `samples/HelloPanel/HelloScreen.css`: add `ScrollView`, `ScrollView:hover`, `ScrollView:active`, and `ScrollView > Label` (The Rule Matrix). The sheet then holds 27 rules.
  - File(s): `samples/HelloPanel/HelloScreen.css`
  - Estimate: 3 steps

- [x] ✅ **3.3 Drive the scroll view in the smoke run** - Completed 2026-10-28
  - Description: Edit `samples/HelloPanel/Program.cs`: push the scroll at the view center before the move (The Smoke Widget Script). Add `Scrolls` to `RunResult`, to line 5, and to the widget check. Edit the remarks.
  - File(s): `samples/HelloPanel/Program.cs`
  - Estimate: 4 steps

- [x] ✅ **3.4 Add the scroll view sheet tests** - Completed 2026-10-28
  - Description: Edit `tests/BrutalUi.Markup.Tests/Style/HelloPanelSheetTests.cs`: the count 27, and 2 tests. `TheScrollViewRulesStyleTheView` (the size 240 x 80, the base fill, the hover border color, the active fill, and a `Label` child with 16 px). `TheScrollContentIsTallerThanTheView` (a view with 8 item labels under the sheet: after `Update`, a set of `ScrollOffset = (0, 1000)` gives an offset Y above 0 and below 1000, so the content is taller than the view).
  - File(s): `tests/BrutalUi.Markup.Tests/Style/HelloPanelSheetTests.cs`
  - Estimate: 6 steps

- [x] ✅ **3.5 Run the phase 3 gate** - Completed 2026-10-28
  - Description: Run the build, the tests, the class, the regression set, and `scripts/test-linux.sh`. Record line 5 in `context.md`.
  - File(s): `docs/features/interactive-elements/widget-sample/context.md`
  - Estimate: 4 steps

---

## Phase 4: Typed Text Shows in a Label, and Enter Submits (6/6 complete)

**Demo:** a person clicks the field, types `Hello`, and reads `Text: Hello`. The caret blinks right after the `o`, with a gap of 1 pixel or less. The person presses Enter and reads `Submitted: Hello`, then types until the text is wider than the field and sees the text scroll under the caret. A headless verifier reads the PASS line of `scripts/test-linux.sh` and `edits <E>, submits <U>` with both above 0 in line 5 of `smoke.log`, and the class filter `'*HelloPanelSheetTests'` reads a total of 10 with `failed: 0`.
**Needs:** Phase 3
**Agent tier:** ADVANCED (opus)

- [x] ✅ **4.1 Add the TextField row** - Completed 2026-10-28
  - Description: Edit `samples/HelloPanel/HelloWidgets.cs`: add row 5 with the `TextField` (`Capacity = 32`) and a column (class `hello-status`) with the text label and the submitted label. Add `OnTextChanged(TextField)` and `OnSubmitted(TextField)`, the counts `Edits` and `Submits`, the property `TextField`, and `internal const int TextCapacity = 32`. Extend the remarks: the capacity, the copy of the text with no allocation, and the keyboard switch in `Program.RunFrames`.
  - File(s): `samples/HelloPanel/HelloWidgets.cs`
  - Estimate: 6 steps

- [x] ✅ **4.2 Add the TextField rules** - Completed 2026-10-28
  - Description: Edit `samples/HelloPanel/HelloScreen.css`: add `TextField`, `TextField:hover`, `TextField:focus`, and `.hello-status > Label` (The Rule Matrix). The sheet then holds 31 rules.
  - File(s): `samples/HelloPanel/HelloScreen.css`
  - Estimate: 3 steps

- [x] ✅ **4.3 Drive the text field in the smoke run** - Completed 2026-10-28 (the Tab moves before the field events, decision 1 of `### Phase 4` in `context.md`)
  - Description: Edit `samples/HelloPanel/Program.cs`: push the field press and release, the char or the Backspace by frame parity, and the main Enter before the move (The Smoke Widget Script). Add `Edits` and `Submits` to `RunResult`, to line 5, and to the widget check. Edit the remarks.
  - File(s): `samples/HelloPanel/Program.cs`
  - Estimate: 5 steps

- [x] ✅ **4.4 Add the text field sheet tests** - Completed 2026-10-28
  - Description: Edit `tests/BrutalUi.Markup.Tests/Style/HelloPanelSheetTests.cs`: the count 31, and 2 tests. `TheTextFieldRulesStyleTheField` (the width 240, the base fill, the text color, the hover fill, and the focus border color). `TheTextFieldHasRoomForOneLine` (after `Update`, the content box height of the field is at least the measured line height of its font size).
  - File(s): `tests/BrutalUi.Markup.Tests/Style/HelloPanelSheetTests.cs`
  - Estimate: 5 steps

- [x] ✅ **4.5 Run the phase 4 gate** - Completed 2026-10-28
  - Description: Run the build, the tests, the class, the regression set, and `scripts/test-linux.sh`. Record line 2, line 3, and line 5 in `context.md`.
  - File(s): `docs/features/interactive-elements/widget-sample/context.md`
  - Estimate: 4 steps

- [x] ✅ **4.6 Plant C (an allocation in OnTextChanged)** - Completed 2026-10-28
  - Description: Copy `samples/HelloPanel/HelloWidgets.cs` to the scratchpad. Add `GC.KeepAlive(new object());` as the first line of `OnTextChanged`. Run `scripts/test-linux.sh; echo "exit=$?"` and expect `FAIL at smoke (exit 1)` with the allocation line. In a later tool call, restore the file and check it with `cmp`. Record it.
  - File(s): `samples/HelloPanel/HelloWidgets.cs` (planted and restored), `docs/features/interactive-elements/widget-sample/context.md`
  - Estimate: 4 steps

---

## Phase 5: The 1000-Element Gate with Each Widget (6/6 complete)

**Demo:** the developer runs `dotnet run --project tests/BrutalUi.Core.Tests/BrutalUi.Core.Tests.csproj -- --filter-class '*ThousandWidgetAllocationTests'` and reads a total of 4 with `failed: 0`. A planted allocation at the start of `TextField.AdvanceCaret` makes `dotnet test --solution brutal-ui.sln` exit with a value that is not 0, and the failed list holds `ThousandWidgetAllocationTests.UpdateAndDrawAllocateNothingWithEachWidget`.
**Needs:** Phase 4
**Agent tier:** ADVANCED (opus)

- [x] ✅ **5.1 Add the Build(IUiRenderer?) overload** - Completed 2026-10-28
  - Description: Edit `tests/BrutalUi.Core.Tests/Shared/ThousandElementTree.cs`: add `public static ThousandElementTree Build(IUiRenderer? measurer)`. With a measurer, the context is `new UiContext(new UiContext.CreateInfo { Measurer = measurer })`. `Build()` calls `Build(null)`. Edit the doc comments of both overloads and the class remarks (a tree with a widget needs a measurer).
  - File(s): `tests/BrutalUi.Core.Tests/Shared/ThousandElementTree.cs`
  - Estimate: 4 steps

- [x] ✅ **5.2 Write ThousandWidgetTree.cs** - Completed 2026-10-28
  - Description: Write `tests/BrutalUi.Core.Tests/Shared/ThousandWidgetTree.cs`: `internal sealed class ThousandWidgetTree` with `Build()` (The Gate Tree), and the properties `Context`, `Renderer`, `Root`, `All` (the 1000 live elements in tree order), `Button`, `DisabledButton`, `Toggle`, `Slider`, `TextField`, `ScrollView`, and the viewport center of each widget. `Build` throws `InvalidOperationException` when the subtree count of the root is not 1000. The type selectors of the widget sheet use the real type names (`Button`, `Toggle`, and so on). Each other interned name (a class or an element name) uses a prefix that is unique to the class, because xUnit runs the classes in parallel.
  - File(s): `tests/BrutalUi.Core.Tests/Shared/ThousandWidgetTree.cs`
  - Estimate: 8 steps

- [x] ✅ **5.3 Write the input step of ThousandWidgetAllocationTests** - Completed 2026-10-28
  - Description: Write `tests/BrutalUi.Core.Tests/ThousandWidgetAllocationTests.cs` with the step below. The step sets `ScrollView.ScrollOffset = default`, then pushes in this order: (1) a Left press and release, a scroll (0, -1), and a char `a` at the `Button` center, and an Enter key down. (2) The same four events at the disabled button. (3) The same at the `Toggle`, with a Space key down in place of Enter. (4) A Left press at 25 % of the slider width, a move to 75 %, a release, a scroll, a char `a`, and a Right key down. (5) A Left press and release at the `TextField`, a scroll, a char `a`, a Backspace key down, and an Enter key down. (6) A scroll (0, -1) at the view center, a Left press there, a move 20 pixels up, and a release. (7) A Tab key down. Each event has no target, so each key and each char goes to the focus owner that the press of its group set. The step then runs `Update(0.016f)` and `Draw`.
  - File(s): `tests/BrutalUi.Core.Tests/ThousandWidgetAllocationTests.cs`
  - Estimate: 8 steps

- [x] ✅ **5.4 Write the 4 tests of ThousandWidgetAllocationTests** - Completed 2026-10-28
  - Description: Write the 4 tests of the class: `TheTreeHoldsEachWidget` (1000 elements, one element of each widget type and one disabled button, `HitTest` of each widget center gives the widget, and the view content is taller than the view). `UpdateAndDrawAllocateNothingWithEachWidget` (the field holds the focus. Each frame marks `Style` on each element of `All` and runs `Update(0.016f)` and `Draw`. 100 frames after 2 warm-up rounds of 9 frames, so the caret pass, the blink, and the restyle of 1000 elements with both sheets run). `TheWidgetInputPathAllocatesNothing` (100 steps after 2 warm-up steps). `TheMeasuredBodyDoesRealWork` (after one step: 2 clicks, no click from the disabled button, 2 flips, 3 value changes, 2 scroll changes, 2 text changes, 1 submit, the text `field`, the view offset (0, 60), and a `PushClip` for the view and for the field in the frame). When a count differs from the list, read the widget rule, fix the step or the expected count, and record the cause in `context.md`.
  - File(s): `tests/BrutalUi.Core.Tests/ThousandWidgetAllocationTests.cs`
  - Estimate: 10 steps

- [x] ✅ **5.5 Plant an allocation in TextField.AdvanceCaret** - Completed 2026-10-28
  - Description: Copy `src/BrutalUi.Core/Elements/TextField.cs` to the scratchpad. Add `GC.KeepAlive(new object());` as the first line of `AdvanceCaret`. Run `dotnet test --solution brutal-ui.sln > <scratch>/plant.log 2>&1; echo "exit=$?"`, then search `<scratch>/plant.log` for `failed` and `Failed` (tool rule 5). Expect a value that is not 0 and `UpdateAndDrawAllocateNothingWithEachWidget` in the failed list. In a later tool call, restore the file from the copy and check it with `cmp`. Record the failed gates in `context.md`.
  - File(s): `src/BrutalUi.Core/Elements/TextField.cs` (planted and restored), `docs/features/interactive-elements/widget-sample/context.md`
  - Estimate: 5 steps

- [x] ✅ **5.6 Run the phase 5 gate** - Completed 2026-10-28
  - Description: Run the build, the tests, the regression set, and the benchmark build (`dotnet build tests/BrutalUi.Benchmarks/BrutalUi.Benchmarks.csproj 2>&1 | tail -n 5`). Run `git diff <base> --stat -- src` and expect no output. Record the counts.
  - File(s): `docs/features/interactive-elements/widget-sample/context.md`
  - Estimate: 4 steps

---

## Phase 6: The Manual Checks and the Record (8/8 complete)

**Demo:** a person opens the widget checklist of `docs/testing-linux.md`, runs `dotnet run --project samples/HelloPanel/HelloPanel.csproj` on a desktop, and does each item in order: the button, the disabled button, the toggle, the slider, the scroll view, the text field with the caret check, and Tab. Each item states one action and one result, and the person checks each one.
**Needs:** Phase 5
**Agent tier:** STANDARD (sonnet)

- [x] ✅ **6.1 Add the widget sheet paragraph to technical design section 5** - Completed 2026-10-28
  - Description: Edit `docs/technical-design.md` section 5: after the TextField paragraph, add a paragraph "**The widget sheet.**" with the rule matrix in prose: the bits that each widget gets, the four widgets that need a size rule, the fill rule for hover and active and the border rule for focus, the order rule for `Button:disabled` and `Toggle:checked`, the hover rule of the hit element (Finding 7), and the status label rule (the longest text first, a `u8` prefix, and `int.TryFormat` into a fixed buffer). Name `samples/HelloPanel/HelloWidgets.cs` as the example. Edit the comment of `HelloPanel/` in section 3 ("Phase 2: each widget of the epic").
  - File(s): `docs/technical-design.md`
  - Estimate: 5 steps

- [x] ✅ **6.2 Record the gate and the smoke lines in sections 14 and 16** - Completed 2026-10-28
  - Description: Edit `docs/technical-design.md` section 14: add a "**A thirteenth allocation gate.**" item (`ThousandWidgetAllocationTests`, the swap of the footer, the widget sheet, the step, and the plant of task 5.5 with its failed gates). Name `HelloPanelSheetTests` in the stylesheet parser item. Edit the sample item: 5 lines, the widget script, the new numbers of line 2, line 4, and line 5, the new exit-1 line, and plants A, B, and C. Edit section 16: the widget-sample feature closes Phase 2.
  - File(s): `docs/technical-design.md`
  - Estimate: 5 steps

- [x] ✅ **6.3 Add the widget checklist to testing-linux.md** - Completed 2026-10-28
  - Description: Edit `docs/testing-linux.md`: add a section "8. The widget checklist" (a desktop session with a display, not `xvfb-run`). The items: the `Click me` click and its label, the hover fill and the focus border, the disabled button, the toggle and `:checked`, the slider drag and the Right key, the wheel and the drag of the list, the text field (type, the caret gap of 1 pixel or less, Enter, and the scroll under the caret), and Tab through the four focusable widgets. Add two rows to the table of section 6: `FAIL at smoke (exit 1)` with `allocated <B> bytes after warm-up`, and with `did not reach each widget`.
  - File(s): `docs/testing-linux.md`
  - Estimate: 4 steps

- [x] ✅ **6.4 Add the widget items to testing-windows.md** - Completed 2026-10-28
  - Description: Edit `docs/testing-windows.md`: add the widget items to the look checklist of section 4 after item 2 and renumber. Add one line for each new item to the report block of section 5. Add the two smoke rows to the table of section 8.
  - File(s): `docs/testing-windows.md`
  - Estimate: 4 steps

- [x] ✅ **6.5 Add the manual checks to the epic human-testing.md** - Completed 2026-10-28
  - Description: Edit `docs/features/interactive-elements/human-testing.md`: in the `## widget-sample` section (create it when `/autorun-epic` did not), add one manual check for each widget (`Button`, `Toggle`, `Slider`, `ScrollView`, `TextField`), one for the disabled button, one for the caret gap with `ImGuiRenderer` (open risk 13 of the text-field review), and one Windows run of `scripts/test-windows.ps1` with the widget checklist.
  - File(s): `docs/features/interactive-elements/human-testing.md`
  - Estimate: 3 steps

- [x] ✅ **6.6 Update CHANGELOG.md** - Completed 2026-10-28
  - Description: Edit `CHANGELOG.md` under `## [Unreleased]`: "Added" (each widget in HelloPanel, the widget rules of `HelloScreen.css`, `HelloPanelSheetTests`, and the thirteenth gate) and "Changed" (the smoke run drives each widget, prints line 5, and exits with code 1 when a widget gets no change).
  - File(s): `CHANGELOG.md`
  - Estimate: 3 steps

- [x] ✅ **6.7 Run the STE lint** - Completed 2026-10-28
  - Description: Run `python3 /home/aaronstatic/.claude/skills/asd-ste100/scripts/ste-lint.py --fail-over 2.5` on each changed doc, on `HelloWidgets.cs`, and on this plan. Fix each finding above 2.5.
  - File(s): each changed doc, `samples/HelloPanel/HelloWidgets.cs`, `docs/features/interactive-elements/widget-sample/implementation.md`
  - Estimate: 5 steps

- [x] ✅ **6.8 Run the final gate** - Completed 2026-10-28
  - Description: Run the build, the tests, and `scripts/test-linux.sh; echo "exit=$?"`. Record the final test counts against the counts of task 1.1 in `context.md`.
  - File(s): `docs/features/interactive-elements/widget-sample/context.md`
  - Estimate: 4 steps

---

## Bugs & Issues

**Active Bugs:**
- None

**Fixed Bugs:**
- None

---

## Technical Debt

- None yet

---

## Progress Tracking

### Completed This Session (2026-10-28)
- ✅ Phases 1 to 6 (tasks 1.1 to 6.8), each with a verifier PASS.
- ✅ The cross-phase review: CR-1 to CR-8, CR-10, and CR-11 fixed. CR-9 not fixed by criterion I2.

### Discovered New Tasks
- [x] The user decides CR-9 (the ambiguous cref in `tests/BrutalUi.Benchmarks/FactsReport.cs`). The user chose the fix, and the cref now reads `Build()`.

---

## Task Status Legend

- [ ] Not started
- [ ] 🔄 In progress
- [ ] ⏸️ Blocked (waiting on something)
- [x] ✅ Completed
- [x] ❌ Cancelled/Won't do

---

## Notes

### Task Estimation
One step is one tool call of an agent. A typical task costs about six steps (see `.beast-mode/phase-handoff.md` section 1).
- 🟢 Small (up to 6 steps)
- 🟡 Medium (7 to 15 steps)
- 🔴 Large (more than 15 steps, so split the task)

---

*Update this file as tasks are completed. Mark tasks with ✅ immediately when done. Add new tasks as they're discovered.*
