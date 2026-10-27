# Widget Sample - Implementation Plan

**Epic:** interactive-elements (feature 5 of 5)
**Status:** Planning
**Started:** 2026-10-27
**Target Completion:** TBD
**Last Updated:** 2026-10-27 10:00

---

## Quality Bar

**Target quality:** A sample and a release gate. HelloPanel is the only sample of the epic (Q2), and the technical design tells a game developer to copy `HelloScreen.cs` and `HelloScreen.css` to start a screen. Thus the sample code is documentation. A sample that allocates per click, a label that grows its buffer, a sheet rule that a later rule hides, or a smoke run that does not touch the widgets teaches the wrong pattern and lets a GPU-only defect pass. The new 1000-element gate closes epic A9 and Phase 2.

- **The sample shows each widget:** HelloPanel shows a `Button`, a `Toggle`, a `Slider`, a `ScrollView` with content taller than the view, and a `TextField`. Each delegate slot writes to a `Label`.
- **The sheet holds each state rule:** `HelloScreen.css` has a rule for each pseudo-state bit that each widget gets (the rule matrix of Decision 3). A rule for a state does not hide a rule for another state on the same member.
- **0 bytes after warm-up on the GPU:** The smoke run drives each widget with synthetic input. The UI region allocates 0 bytes, also with the delegates and the label writes. A planted allocation in a delegate gives exit code 1.
- **0 bytes after warm-up on the 1000-element tree:** The new gate puts each widget type into the 1000-element tree and pushes pointer, scroll, key, and char events to each widget (A9). A planted allocation fails it.
- **No library change:** `src/`, `scripts/`, each `.csproj`, and `Directory.Packages.props` have no diff. The feature changes the sample, the tests, and the docs only.
- **Docs:** Section 5 of the technical design states the sheet rules of the widgets. The two runbooks and the epic `human-testing.md` hold a manual check for each widget. Prose follows ASD-STE100.

---

## Executive Summary

This feature closes the epic. It puts each widget of the epic into the HelloPanel sample, styles each one in `HelloScreen.css`, extends the GPU smoke run to drive each widget, and adds the thirteenth allocation gate: the 1000-element tree with each widget type in it.

The sample gets one new file, `HelloWidgets.cs`. A `HelloWidgets` object builds a widget section, a column of rows under the image of the panel. Each row holds one widget and its status label. The delegates of the widgets are instance methods of the object, created one time at build time. Each delegate writes its label through a fixed scratch buffer with no allocation. Each label starts with its longest text, so its buffer never grows after the build (the pattern of the gate label).

The smoke run (`--frames N`) pushes, in each frame, a short input script for each widget before the existing pointer move. The run prints a fifth line with the change count of each widget. It exits with code 1 when a count is 0, and it keeps the existing exit 1 for an allocation after warm-up. The scripts read the exit code only, so they do not change.

A new Markup test class, `HelloPanelSheetTests`, parses `HelloScreen.css` and checks that each widget rule reaches its widget. The existing `TheHelloPanelSheetGivesTheOldValues` asserts 6 rules, so this feature removes that one line.

The new gate, `ThousandWidgetAllocationTests`, builds the shared 1000-element tree with a measurer (one new overload of `ThousandElementTree.Build`). It swaps three footer slots (9 elements) for 9 widget elements, so the tree keeps exactly 1000 elements.

The plan has 6 phases and 41 tasks. No phase changes `src/`.

---

## User Personas Alignment

No `docs/user-personas.md` file exists. The personas come from `docs/mission-statement.md` and the epic docs.

**Target Users:**
- [x] **The game developer** (copies `HelloScreen.cs`, `HelloWidgets.cs`, and `HelloScreen.css` to start a screen, and reads how to size a widget, style each state, and write a label with no allocation)
- [x] **The player** (clicks, toggles, drags, scrolls, and types in the sample window)
- [x] **The maintainer** (runs `scripts/test-linux.sh`, reads one more smoke line, and runs one more 0-byte gate)
- [x] **The author on Windows** (runs `scripts/test-windows.ps1` and the widget checklist of `docs/testing-windows.md`)
- [x] **The framework epic** (gets a sample screen to bind to a store and a command later)
- [ ] **The markup author** (not served. The markup epic registers each widget type by name)

**User Subsets Served:**
- [x] **Simple** - one `Button` with a `Clicked` delegate that writes a count into a `Label`, and a `Button:hover` rule.
- [x] **Complex** - each widget with its state rules, a size rule for each widget with no own size, a disabled widget, a scroll view with clipped content, a text field with a capacity, and labels that never grow.

**UX Approach:**
- Each widget sits in one row with its status label to the right, so a person sees each change next to its cause.
- The hover and the active state change the fill. The focus changes the border color. Thus the hover and the focus show at the same time, as on the panel today.

---

## Goals

### Primary Goals
1. Show each widget of the epic in HelloPanel on Vulkan, with a status label for each delegate slot.
2. Style each widget in `HelloScreen.css`, with a rule for each pseudo-state bit that the widget gets.
3. Drive each widget in the smoke run, and fail the run when a widget gets no change or when `Update` or `Draw` allocates after warm-up.
4. Add the 1000-element allocation gate with each widget type (A9).
5. Record the widget sheet rules in section 5 of the technical design, and the manual checks in the runbooks and the epic `human-testing.md`.

### Success Criteria
- [ ] The phase 1 demo passes: a click on `Click me` shows `Button clicks: 1` on screen, and `artifacts/test-linux/smoke.log` holds `HelloPanel: widgets: clicks <C>.` with `C` above 0.
- [ ] `scripts/test-linux.sh` prints its PASS line with each widget in the sample.
- [ ] `HelloPanelSheetTests` (10 tests) and `ThousandWidgetAllocationTests` (4 tests) pass.
- [ ] A planted allocation in a sample delegate fails the smoke run with exit code 1. A planted allocation in `TextField.AdvanceCaret` fails the new gate.
- [ ] Each existing test passes. Of the existing test files, only one line of `ParsedSheetAllocationTests.cs` and the `Build` overload of `ThousandElementTree.cs` change.

---

## Architecture Overview

### Codebase Findings

1. **The sample frame loop.** `Program.RunFrames` measures the UI region with `GC.GetAllocatedBytesForCurrentThread`: the smoke input, `ViewportSize`, `Update`, `Draw`, the gate read, and the gate label write. After 3 warm-up frames, a UI sum above 0 gives exit code 1. A delegate of a widget runs in `Update`, so the gate measures each delegate and each label write.
2. **The smoke input.** `PushSmokeInput` pushes a pointer move and a Tab in each frame. The move goes to the panel center in an even frame and off the panel in an odd frame. The run fails when the gate read the pointer over the UI in each measured frame. `PointerOverUi` reads the hit of the last pointer event of the drain. Thus each widget event must come before the move.
3. **The tree.** `HelloScreen.Build` builds the root (not pickable), the panel (focusable, a column), the greeting label, the gate label, and the checker image. The sheet rules `.hello-panel > Label` (26 px) and `.hello-panel > Image` are child rules.
4. **The label buffer grows.** `Label` uses `Utf8Text`, which doubles its buffer. The gate label starts with its longest text, so the buffer never grows after the first frame. Each status label must do the same.
5. **The sheet test.** `ParsedSheetAllocationTests.TheHelloPanelSheetGivesTheOldValues` (Markup tests) reads the linked copy of `HelloScreen.css`. It asserts `Assert.Equal(6, sheet.Rules.Length)`. `FindRule` asserts that each selector text occurs one time. A new rule breaks the count line only. The `:root` block of variables gives no rule.
6. **The parser.** The stylesheet format has the five pseudo-classes (`:hover`, `:active`, `:focus`, `:disabled`, `:checked`) and the type selectors. It has no `gap`. Use `margin`.
7. **Hover is on the hit element only.** `PointerHoverTests` pins it. The panel loses `.hello-panel:hover` while the pointer is over a widget. `ScrollView:hover` matches only over the view itself: its padding, a gap, or a child with `IsPickable = false`.
8. **Sizes.** `Button` takes the size of its text plus the padding and the border. `Toggle`, `Slider`, `ScrollView`, and `TextField` have no own size. A widget with no size takes no space and catches no input. A slider narrower than tall has no travel.
9. **Specificity.** `Button:hover` (a type and a state) wins over `:disabled` (a state only). Thus a disabled widget under the pointer shows the hover fill unless the sheet has `Button:disabled` after `Button:hover`.
10. **The keyboard switch exists.** `RunFrames` already sets `input.KeyboardEnabled = !io.WantCaptureKeyboard` before `Glfw.PollEvents()`. The sample makes no ImGui widget, so ImGui never wants the keyboard. Item 3 of the text-field review needs no code change.
11. **The 1000-element tree has no measurer.** `ThousandElementTree.Build` creates `new UiContext()`. A `Button` needs a measurer, and the caret pass of a `TextField` maps a press and scrolls only with a measurer. The benchmark project and the ImGui tests link the file, and the file uses no internal member.
12. **The footer.** The 1000-element tree has a footer (a root child named `footer`, a centered row of height 64) with 10 slots of 3 elements each. No test handle points into it, no rule of the sheet names it, and no absolute overlay covers it. It is the safe place for the swap.
13. **A caller target.** A char or a key with the widget as the caller target reaches the widget with no focus (`TextFieldTypeTests.ACharWithACallerTargetInserts`). A pointer event with a caller target ends no press, so the smoke run pushes each pointer event with no target.

### File Tree (end state)

```
samples/HelloPanel/
├── HelloWidgets.cs          # phase 1 (section, Button row), 2 (Toggle, Slider, disabled Button),
│                            # 3 (ScrollView row), 4 (TextField row)
├── HelloScreen.cs           # phase 1 (Build adds the section, the remarks)
├── HelloScreen.css          # phase 1 to 4 (the rule matrix)
└── Program.cs               # phase 1 to 4 (the widget script of the smoke input, line 5, the widget check)
tests/BrutalUi.Markup.Tests/Style/
├── HelloPanelSheetTests.cs        # phase 1 to 4 (3 + 3 + 2 + 2 tests)
└── ParsedSheetAllocationTests.cs  # phase 1: one line removed, one comment added
tests/BrutalUi.Core.Tests/
├── Shared/ThousandElementTree.cs          # phase 5: the Build(IUiRenderer?) overload
├── Shared/ThousandWidgetTree.cs           # phase 5: the swap, the widget sheet, the points
└── ThousandWidgetAllocationTests.cs       # phase 5 (4 tests)
docs/technical-design.md                   # phase 6: sections 3, 5, 14, 16
docs/testing-linux.md                      # phase 6
docs/testing-windows.md                    # phase 6
docs/features/interactive-elements/human-testing.md   # phase 6
CHANGELOG.md                               # phase 6
docs/features/interactive-elements/widget-sample/context.md   # each phase
```

These paths do not change: each file in `src/`, `scripts/`, each `.csproj`, `Directory.Packages.props`, `samples/WorldSpace/`, `samples/HelloPanel/VulkanContext.cs`, `samples/HelloPanel/SampledTexture.cs`, and each existing test file except the two above. Phase 5 plants a line in `src/BrutalUi.Core/Elements/TextField.cs` and restores it.

### The Sample Tree (end state)

```
root .hello-root (not pickable)
└── panel .hello-panel (focusable, column)
    ├── Label "Hello, brutal-ui"                  (not pickable)
    ├── Label gate text                           (not pickable)
    ├── Image checker                             (not pickable)
    └── Element .hello-widgets (column)           phase 1
        ├── Element .hello-row: Button "Click me", Label "Button clicks: 0",
        │                       Button "Disabled" (Disabled from the build, phase 2)
        ├── Element .hello-row: Toggle, Label "Toggle: off"                                 phase 2
        ├── Element .hello-row: Slider (0 to 100, step 1, value 50), Label "Slider value: 50" phase 2
        ├── Element .hello-row: ScrollView (8 Labels "Item 1" to "Item 8", not pickable),
        │                       Label "Scroll offset: 0 px"                                 phase 3
        └── Element .hello-row: TextField (capacity 32), Element .hello-status (column):
                                Label "Text: ", Label "Submitted: "                         phase 4
```

The section and the rows are plain elements. They are pickable by default, so a press in a gap focuses the panel (the nearest focusable ancestor). Each status label is not pickable. The Tab order is the panel, `Click me`, `Toggle`, `Slider`, and `TextField`. The disabled button and the scroll view take no focus.

### The Widget Status Rule

`HelloWidgets` owns one `byte[] _scratch` of 64 bytes, created in the constructor. Each delegate composes its text in the scratch buffer and calls `Label.SetText(ReadOnlySpan<byte>)`:

- A count: a `u8` prefix, then `int.TryFormat(Span<byte>, out int, default, CultureInfo.InvariantCulture)` (the `IUtf8SpanFormattable` form). No string and no boxing.
- A literal: one of two `u8` literals (`Toggle: on`, `Toggle: off`).
- A text: a `u8` prefix, then a copy of `TextField.Text` (at most 32 bytes).

Each label starts with its longest text and then gets its start text, so the buffer reaches its peak size at build time:

| Label | Longest text (build) | Start text |
|-------|----------------------|------------|
| Button | `Button clicks: 2147483647` | `Button clicks: 0` |
| Toggle | `Toggle: off` | `Toggle: off` |
| Slider | `Slider value: 100` | `Slider value: 50` |
| Scroll | `Scroll offset: 2147483647 px` | `Scroll offset: 0 px` |
| Text | `Text: ` plus 32 bytes of `W` | `Text: ` |
| Submitted | `Submitted: ` plus 32 bytes of `W` | `Submitted: ` |

The delegates also count the changes (`Clicks`, `Flips`, `Values`, `Scrolls`, `Edits`, `Submits`) for line 5 of the smoke run.

### The Smoke Widget Script

In each frame of a run with `--frames`, `PushSmokeInput` pushes the widget events first, then the existing pointer move, then the Tab down and up. Each pointer event has no target. Each point is the viewport center of the widget, from the layout of the last `Update` (the walk of the panel origin today, moved into one helper `ViewportCenter(Element)`).

| Phase | Events of one frame | Count that rises |
|-------|---------------------|------------------|
| 1 | Left press and release at `Click me` | `Clicks` |
| 2 | Left press and release at `Toggle`. Left press and release at the slider point of 25 % of its width (even frame) or 75 % (odd frame) | `Flips`, `Values` |
| 3 | Scroll at the view center with the delta (0, -1) (even frame) or (0, 1) (odd frame) | `Scrolls` |
| 4 | Left press and release at `TextField`. Char `a` with the field as the caller target (even frame), or a Backspace key down with the field as the caller target (odd frame). A main Enter key down with the field as the caller target | `Edits`, `Submits` |

Line 5: `HelloPanel: widgets: clicks <C>, flips <T>, values <V>, scrolls <S>, edits <E>, submits <U>.` Each phase adds its fields. The counts cover the full run. The widget check runs with the input check (a frame limit and 2 measured frames or more). A count of 0 writes `HelloPanel: the synthetic input did not reach each widget.` to standard error and returns exit code 1. The check runs after the allocation check, so an allocation still gives the allocation line.

### The Rule Matrix

`R` is a rule in `HelloScreen.css`. `-` means that the widget never gets the bit.

| Widget | base (size and paint) | `:hover` | `:active` | `:focus` | `:checked` | `:disabled` |
|--------|------|----------|-----------|----------|------------|-------------|
| `Button` | R (padding, fill, border, color, font size) | R (fill) | R (fill) | R (border color) | - | R as `Button:disabled` (fill, color), after `:hover` and `:active` |
| `Toggle` | R (20 x 20, fill, border, radius) | R (fill) | R (fill) | R (border color) | R (fill), after `:hover` and `:active` | none in the sample (Notes Q3) |
| `Slider` | R (200 x 20, track fill, thumb color, radius) | R (thumb color) | R (thumb color) | R (border color) | - | none in the sample |
| `ScrollView` | R (240 x 80, column, padding, fill, border) | R (border color) | R (fill) | - (no focus) | - | none in the sample |
| `TextField` | R (240 wide, padding, fill, border, color, font size) | R (fill) | - (no visual need) | R (border color) | - | none in the sample |

The layout rules: `.hello-widgets` (column, top margin 12), `.hello-row` (row, `align-items: center`, top margin 8), `.hello-row > Label` (text color, 16 px, left margin 12), `ScrollView > Label` (text color, 16 px), and `.hello-status > Label` (text color, 16 px). Each widget rule sets the font size of 16 px where the widget draws text, so the line height of the text field and the button follows the sheet. A hover or an active rule sets the fill, and a focus rule sets the border color. Thus the states combine, as `.hello-panel:hover` and `.hello-panel:focus` do today.

The rule count of the sheet after each phase: 6 (today), 13 (phase 1: 3 layout rules and 4 button rules), 23 (phase 2: 10 rules), 27 (phase 3: 4 rules), and 31 (phase 4: 4 rules). `HelloPanelSheetTests.TheSheetHoldsEachRule` pins each count. When a phase needs another rule, update the count and record the cause in `context.md`.

### The Gate Tree

```
ThousandWidgetTree.Build():
  renderer = new RecordingRenderer()
  tree     = ThousandElementTree.Build(renderer)            // the new overload, a context with a measurer
  footer   = the root child with Name "footer"
  remove the last 3 slots of the footer (3 x 3 = 9 elements)
  add 9 elements to the footer, in this order:
    Button "Play"                    (Clicked)
    Button "Off"                     (States |= Disabled, Clicked)
    Toggle                           (Inline 20 x 20, CheckedChanged)
    Slider 0..100, step 1, value 50  (Inline 120 x 20, ValueChanged)
    TextField capacity 32, "field"   (Inline 120 x 16, TextChanged, Submitted)
    ScrollView                       (Inline 80 x 48, column, ScrollChanged)
      3 rows                         (Inline 80 x 40 each, IsPickable = false)  -> content 120, maximum offset 72
  the subtree count of the root is 1000
  context.AddSheet(widget sheet of 7 rules): Button:hover, Button:active, Button:disabled, Toggle:checked,
                                             Slider:focus, ScrollView:hover, TextField:focus
  context.Update(0.016f)
  All = the 1000 live elements, by a walk in tree order
```

### Data Flow

```
Game: Glfw.PollEvents ──► GlfwUiInput ──► ui ring        (real input)
      PushSmokeInput: widget script ──► move ──► Tab      (smoke input, before Update)
UiContext.Update: drain ──► Button.HandlePointerUp ──► Clicked(button)
                               └─► HelloWidgets.OnClicked: Clicks++, compose "Button clicks: <n>" in _scratch
                                   ──► statusLabel.SetText(span) (no growth: the build set the peak size)
                  style pass (Button:hover, :active, :focus) ──► layout pass (the label measure) ──► caret pass
UiContext.Draw ──► ImGuiRenderer
Program: UI region bytes == 0 after warm-up? ──► line 3. Each count > 0? ──► line 5. Else exit 1.
```

---

## Implementation Phases

Each phase is a slice: one narrow path that works end to end, with a demo line that a person can check. Section 11 of `.beast-mode/phase-handoff.md` holds the rules. Notes Q1 names the demo of phase 1. No person answered it, so phase 1 uses the recommended line. Notes Q2, Q3, and Q4 also use the recommended option.

Each phase ends with `dotnet build brutal-ui.sln 2>&1 | tail -n 30`, `dotnet test --solution brutal-ui.sln 2>&1 | tail -n 30`, and `scripts/test-linux.sh; echo "exit=$?"` (tool rule 5). Expect `0 Warning(s)`, `0 Error(s)`, 0 failed, the PASS line, and `exit=0`. After the script, read `artifacts/test-linux/smoke.log` and record its `HelloPanel:` lines in `context.md`. The projects have no implicit usings, so each new file holds its own `using` lines. Each public member needs an XML doc comment (CS1591 is an error). The sample uses `namespace HelloPanel;`. The Markup tests and the Core tests use the namespace of their existing files (read `ParsedSheetAllocationTests.cs` and `ScrollViewAllocationTests.cs` first). Prose in comments follows ASD-STE100. Write each sample text as a `u8` literal (the analyzer `BRUTALCORE0011` rejects `SetText("x")`). Run one Markup class with `dotnet run --project tests/BrutalUi.Markup.Tests/BrutalUi.Markup.Tests.csproj -- --filter-class '*<Class>' 2>&1 | tail -n 5`.

**GPU and display.** `scripts/test-linux.sh` runs the smoke step on the NVIDIA GPU inside Xvfb on this machine, so a headless verifier runs it. The on-screen part of each demo needs a person at a desktop: `dotnet run --project samples/HelloPanel/HelloPanel.csproj` with no argument. A headless verifier checks the PASS line, line 5 of `smoke.log`, and the unit tests, and it returns `Demo: MANUAL` for the on-screen part.

**The stop rule.** No phase changes `src/`. When the smoke run or a gate reports an allocation that comes from `src/` (a widget, `ImGuiRenderer`, or the caret pass under the real measurer), do not fix it in this feature. Record the bytes, the plant or the bisect that finds the path, and the file in `context.md` and in `inbox.md`, and stop the phase.

The regression set of each phase (no edit): `ParsedSheetAllocationTests`, `WidgetAllocationTests`, `SliderAllocationTests`, `ScrollViewAllocationTests`, `TextFieldAllocationTests`, `ThousandElementAllocationTests`, `PointerAllocationTests`, `FocusAllocationTests`, and `ImGuiRendererAllocationTests`.

### Phase 1: A Click on the Button Shows in a Label
**Goal:** The smallest path that proves the idea: HelloPanel shows a `Button` in a new widget section. A click calls `Clicked`, and the delegate writes the click count into a status label with no allocation. `HelloScreen.css` styles the button and its `:hover`, `:active`, and `:focus` states. The smoke run clicks the button in each frame, prints line 5, and fails when no click arrives.
**Demo:** a person runs `dotnet run --project samples/HelloPanel/HelloPanel.csproj` on a desktop, clicks the `Click me` button two times, and reads `Button clicks: 2` next to it. A headless verifier runs `scripts/test-linux.sh` and reads the PASS line, and finds `HelloPanel: widgets: clicks <C>.` with `C` above 0 in `artifacts/test-linux/smoke.log`.
**Needs:** none
**Agent tier:** ADVANCED (opus)

**Tier note:** This phase sets the sample structure that each later phase copies, changes the output of the smoke run (line 5 and one more exit-1 reason), and edits an existing test. Keep the widget events before the pointer move in `PushSmokeInput`, or the off-panel frames read the pointer over the UI, and the existing input check fails (Finding 2). Create each delegate one time in the `HelloWidgets` constructor from an instance method. Do not use a lambda. Do not format with `$"`, `string.Format`, or `ToString` in a delegate. The status label must start with its longest text (The Widget Status Rule).

**Tasks:**

*Group A (the sample):*
- [ ] 1.1 Record in `docs/features/interactive-elements/widget-sample/context.md`, before any change: the base commit (`git rev-parse HEAD`), the result of `git ls-files src/BrutalUi.Core/Elements/TextField.cs` (expect one line), and `git status --porcelain -- src tests samples` (expect no line). When text-field is not in the base commit, stop and record it. Run `scripts/test-linux.sh` and record the 4 lines of `artifacts/test-linux/smoke.log`. Record the test counts of the solution and of each test project, and the Codebase Findings.
- [ ] 1.2 Write `samples/HelloPanel/HelloWidgets.cs`: `internal sealed class HelloWidgets`. The constructor builds `Section` (class `hello-widgets`) and row 1 (class `hello-row`) with `ClickButton` (`new Button(new Button.CreateInfo { Text = "Click me" })`) and the status label (not pickable, longest text first). It creates `_scratch = new byte[64]`, sets `ClickButton.Clicked` from a cached field that holds `OnClicked`, and exposes `Section`, `ClickButton`, and `Clicks`. The class ids `hello-widgets` and `hello-row` are `static readonly ClassId` fields. `OnClicked(Button)` adds 1 to `Clicks` and writes `Button clicks: <n>` (The Widget Status Rule). The class remarks state: the status rule, the longest text rule, "one delegate object, created at build time", and "copy this file with `HelloScreen.cs` and `HelloScreen.css`".
- [ ] 1.3 Edit `samples/HelloPanel/HelloScreen.cs`: `Build` gets `out HelloWidgets widgets` and adds `widgets.Section` as the last child of the panel, after the image. Edit the class remarks and the `Build` remarks: the panel is no longer the only focusable element, the Tab order, the widget section, the child rules of the labels, and the class names of the sheet.
- [ ] 1.4 Edit `samples/HelloPanel/HelloScreen.css`: add the variables of the widget colors to `:root`, and the rules `.hello-widgets`, `.hello-row`, `.hello-row > Label`, `Button`, `Button:hover`, `Button:active`, and `Button:focus` (The Rule Matrix). Do not change the 6 old rules. Extend the file comment: the widget rules, the fill rule for hover and active, and the border rule for focus. The sheet then holds 13 rules.
- [ ] 1.5 Edit `samples/HelloPanel/Program.cs`: pass `widgets` from `Build` to `RunFrames` and to `PushSmokeInput`. Add the helper `ViewportCenter(Element)` (the walk of today, moved). Push the Left press and release at `ClickButton` before the move (The Smoke Widget Script). Add `Clicks` to `RunResult`. `Run` writes line 5 and runs the widget check after the input check. Edit the remarks of the class, `Run`, `RunFrames`, and `PushSmokeInput`: 5 lines, the widget script, the order rule, and the new exit-1 reason.
- [ ] 1.6 Run the build, the tests, and `scripts/test-linux.sh`. Record line 5 and the new draw command and vertex counts of line 2 in `context.md`.

*Group B (the tests and the plants):*
- [ ] 1.7 Write `tests/BrutalUi.Markup.Tests/Style/HelloPanelSheetTests.cs` with 3 tests. Each test parses the linked `HelloScreen.css` with `StyleSheetParser` (the shape of `TheHelloPanelSheetGivesTheOldValues`). A private measurer gives 8 pixels for each byte and a line of 16 pixels. `TheSheetHoldsEachRule` (no error, 13 rules, and each selector text of the sheet occurs one time). `TheButtonRulesStyleTheButton` (a root `.hello-root`, a panel `.hello-panel`, a section `.hello-widgets`, a row `.hello-row`, and a `Button`. After `Update(0f)`: the base fill, the padding, and the color. With `States = Hover`: the hover fill and the base border color. With `Active`: the active fill. With `Focus`: the focus border color and the base fill. With `Hover | Focus`: the hover fill and the focus border color). `TheRowLabelRuleKeepsThePanelLabelRule` (a `Label` in the row has 16 px, and a `Label` that is a direct child of the panel keeps 26 px).
- [ ] 1.8 Edit `tests/BrutalUi.Markup.Tests/Style/ParsedSheetAllocationTests.cs`: remove the line `Assert.Equal(6, sheet.Rules.Length);` only. Put a comment in its place: "`HelloPanelSheetTests.TheSheetHoldsEachRule` pins the rule count. The widget rules of widget-sample come after the 6 old rules." Change no other line.
- [ ] 1.9 Plant A: copy `samples/HelloPanel/HelloWidgets.cs` to the scratchpad. Add `GC.KeepAlive(new object());` as the first line of `OnClicked`. Run `scripts/test-linux.sh; echo "exit=$?"`. Expect `FAIL at smoke (exit 1)` and the line `HelloPanel: Update and Draw allocated <B> bytes after warm-up.` in the tail. In a later tool call, restore the file from the copy and check it with `cmp`.
- [ ] 1.10 Plant B: copy `samples/HelloPanel/Program.cs` to the scratchpad. Remove the press and the release of `ClickButton` from `PushSmokeInput`. Run `scripts/test-linux.sh; echo "exit=$?"`. Expect `FAIL at smoke (exit 1)` and `HelloPanel: the synthetic input did not reach each widget.` In a later tool call, restore the file and check it with `cmp`. Record both plants in `context.md`.
- [ ] 1.11 Run the build, the tests, the new class, the regression set, and `scripts/test-linux.sh` again. Expect the PASS line. Run `git diff <base> --stat -- src scripts '*.csproj' Directory.Packages.props` and expect no output.

**Estimated Steps:** about 55 (11 tasks, about five steps for one task)

**Files (7, two groups):** Group A: `HelloWidgets.cs`, `HelloScreen.cs`, `HelloScreen.css`, `Program.cs`, `context.md`. Group B: `HelloPanelSheetTests.cs`, `ParsedSheetAllocationTests.cs`, and the two restored plants in `HelloWidgets.cs` and `Program.cs`.

**Acceptance Criteria:**
- [ ] The 3 tests of `HelloPanelSheetTests` and each test of `ParsedSheetAllocationTests` pass.
- [ ] `scripts/test-linux.sh` prints its PASS line, and `smoke.log` holds 5 `HelloPanel:` lines, line 5 with `clicks` above 0.
- [ ] Plant A and plant B each give `exit=1` with the expected line, and `cmp` finds no difference after each restore.

---

### Phase 2: The Toggle, the Slider, and a Disabled Button
**Goal:** HelloPanel shows a `Toggle` and a `Slider` in two new rows, and a disabled `Button` after the button status label (Notes Q3). `CheckedChanged` writes `Toggle: on` or `Toggle: off`. `ValueChanged` writes `Slider value: <n>`. The sheet styles each state of both widgets and the disabled button. The smoke run flips the toggle and moves the slider in each frame.
**Demo:** a person clicks the toggle and reads `Toggle: on` with the checked fill. The person drags the slider thumb and reads a value that follows the thumb, presses Tab until the slider shows the focus border, and presses Right to add 1. The person clicks the gray `Disabled` button and sees no change of any label. A headless verifier reads the PASS line of `scripts/test-linux.sh` and `flips <T>, values <V>` with both above 0 in line 5 of `smoke.log`, and runs `dotnet run --project tests/BrutalUi.Markup.Tests/BrutalUi.Markup.Tests.csproj -- --filter-class '*HelloPanelSheetTests'` and reads a total of 6 with `failed: 0`.
**Needs:** Phase 1
**Agent tier:** STANDARD (sonnet)

**Tier note:** Copy the shape of phase 1. The slider value is a whole number from 0 to 100 (`Step = 1`), so write `(int)MathF.Round(slider.Value)`. The disabled button gets `States |= PseudoStates.Disabled` at build time and no `Clicked` delegate. `Button:disabled` must come after `Button:hover` and `Button:active` in the sheet (Finding 9). `Toggle:checked` must come after `Toggle:hover` and `Toggle:active`. The slider press point is in the content box at 25 % or 75 % of the width. Push the press and the release at the same point, so the press sets the value and no drag follows.

**Tasks:**
- [ ] 2.1 Edit `samples/HelloPanel/HelloWidgets.cs`: add the disabled button (`Text = "Disabled"`) as the last child of row 1, row 2 (`Toggle` and its label), and row 3 (`Slider` with `Minimum = 0`, `Maximum = 100`, `Step = 1`, `Value = 50`, and its label). Add `OnCheckedChanged(Toggle)` and `OnValueChanged(Slider)`, the counts `Flips` and `Values`, and the properties `Toggle`, `Slider`, and `DisabledButton`. Extend the remarks: the disabled widget shows `:disabled`, and a press on it changes nothing.
- [ ] 2.2 Edit `samples/HelloPanel/HelloScreen.css`: add `Button:disabled`, `Toggle`, `Toggle:hover`, `Toggle:active`, `Toggle:focus`, `Toggle:checked`, `Slider`, `Slider:hover`, `Slider:active`, and `Slider:focus` (The Rule Matrix). The sheet then holds 23 rules. Note in the file comment: the four widgets with no own size get their size here.
- [ ] 2.3 Edit `samples/HelloPanel/Program.cs`: push the toggle press and release and the slider press and release before the move (The Smoke Widget Script). Add `Flips` and `Values` to `RunResult`, to line 5, and to the widget check. Edit the remarks.
- [ ] 2.4 Edit `tests/BrutalUi.Markup.Tests/Style/HelloPanelSheetTests.cs`: the count 23 in `TheSheetHoldsEachRule`, and 3 tests. `TheToggleRulesStyleTheToggle` (the size 20 x 20 in `Layout.Rect` after `Update`, the base fill, `Checked = true` gives the checked fill, `Checked` with `Hover` keeps the checked fill, and `Focus` gives the focus border color). `TheSliderRulesStyleTheSlider` (the size 200 x 20, the track fill, the thumb `Color`, the hover and active thumb colors, and the focus border color). `TheDisabledRuleWinsOverHover` (a `Button` with `Disabled | Hover` and with `Disabled | Active` has the disabled fill and the disabled color).
- [ ] 2.5 Run the build, the tests, the class, the regression set, and `scripts/test-linux.sh`. Record line 5 in `context.md`.

**Estimated Steps:** about 30 (5 tasks, about six steps for one task)

**Files (5):** `HelloWidgets.cs`, `HelloScreen.css`, `Program.cs`, `HelloPanelSheetTests.cs`, `context.md`

**Acceptance Criteria:**
- [ ] `HelloPanelSheetTests` passes with a total of 6.
- [ ] `scripts/test-linux.sh` prints its PASS line, and line 5 shows `flips` and `values` above 0.

---

### Phase 3: The Scroll View Moves Content Taller than the View
**Goal:** HelloPanel shows a `ScrollView` of 240 x 80 with 8 labels (`Item 1` to `Item 8`, not pickable), so its content is taller than the view. `ScrollChanged` writes `Scroll offset: <n> px`. The sheet styles the view, its hover and active states, and its labels. The smoke run scrolls the view in each frame.
**Demo:** a person turns the mouse wheel over the list and sees the items move up inside the clip and `Scroll offset: 40 px` in the label. The person drags the list with the left button and sees the items follow the pointer. A headless verifier reads the PASS line of `scripts/test-linux.sh` and `scrolls <S>` above 0 in line 5 of `smoke.log`, and the class filter `'*HelloPanelSheetTests'` reads a total of 8 with `failed: 0`.
**Needs:** Phase 2
**Agent tier:** STANDARD (sonnet)

**Tier note:** The labels inside the view are not pickable, so each press on the view starts a drag and each wheel turn over it hits the view (Finding 7). The offset is a whole number of pixels with the default speed of 40. Write `(int)view.ScrollOffset.Y`. Give the view a fixed size in the sheet. Do not use `flex-grow` for its height (CR-4 of the scroll-view review). The view takes no focus, so Tab skips it.

**Tasks:**
- [ ] 3.1 Edit `samples/HelloPanel/HelloWidgets.cs`: add row 4 with the `ScrollView` (`default(ScrollView.CreateInfo)`), its 8 item labels, and the status label. Add `OnScrollChanged(ScrollView)`, the count `Scrolls`, and the property `ScrollView`. Extend the remarks: the content is taller than the view, and the items are not pickable, so a press drags the list.
- [ ] 3.2 Edit `samples/HelloPanel/HelloScreen.css`: add `ScrollView`, `ScrollView:hover`, `ScrollView:active`, and `ScrollView > Label` (The Rule Matrix). The sheet then holds 27 rules.
- [ ] 3.3 Edit `samples/HelloPanel/Program.cs`: push the scroll at the view center before the move (The Smoke Widget Script). Add `Scrolls` to `RunResult`, to line 5, and to the widget check. Edit the remarks.
- [ ] 3.4 Edit `tests/BrutalUi.Markup.Tests/Style/HelloPanelSheetTests.cs`: the count 27, and 2 tests. `TheScrollViewRulesStyleTheView` (the size 240 x 80, the base fill, the hover border color, the active fill, and a `Label` child with 16 px). `TheScrollContentIsTallerThanTheView` (a view with 8 item labels under the sheet: after `Update`, a set of `ScrollOffset = (0, 1000)` gives an offset Y above 0 and below 1000, so the content is taller than the view).
- [ ] 3.5 Run the build, the tests, the class, the regression set, and `scripts/test-linux.sh`. Record line 5 in `context.md`.

**Estimated Steps:** about 25 (5 tasks, about five steps for one task)

**Files (5):** `HelloWidgets.cs`, `HelloScreen.css`, `Program.cs`, `HelloPanelSheetTests.cs`, `context.md`

**Acceptance Criteria:**
- [ ] `HelloPanelSheetTests` passes with a total of 8.
- [ ] `scripts/test-linux.sh` prints its PASS line, and line 5 shows `scrolls` above 0.

---

### Phase 4: Typed Text Shows in a Label, and Enter Submits
**Goal:** HelloPanel shows a `TextField` (capacity 32) with two status labels. `TextChanged` writes `Text: <text>`, and `Submitted` writes `Submitted: <text>`. The sheet styles the field and its hover and focus states, with a height of at least one line. The smoke run presses the field, types or removes a char, and submits in each frame, so the caret pass and the press map run with `ImGuiRenderer` on the GPU.
**Demo:** a person clicks the field, types `Hello`, and reads `Text: Hello`. The caret blinks right after the `o`, with a gap of 1 pixel or less. The person presses Enter and reads `Submitted: Hello`, then types until the text is wider than the field and sees the text scroll under the caret. A headless verifier reads the PASS line of `scripts/test-linux.sh` and `edits <E>, submits <U>` with both above 0 in line 5 of `smoke.log`, and the class filter `'*HelloPanelSheetTests'` reads a total of 10 with `failed: 0`.
**Needs:** Phase 3
**Agent tier:** ADVANCED (opus)

**Tier note:** This is the first GPU run of the caret pass, the blink, and `IndexAtX` through the `ImGuiRenderer` measure. An allocation there comes from `src/`, so apply the stop rule. The two status labels copy `TextField.Text` (at most 32 bytes) after a `u8` prefix into the scratch buffer. They must start with the prefix plus 32 bytes. The char and the keys of the smoke script use the field as the caller target (Finding 13), so the Tab of the same frame does not move them. Push the press and the release with no target. The sample already sets `input.KeyboardEnabled = !io.WantCaptureKeyboard` (Finding 10). Keep that line, and name it in the remarks of `HelloWidgets`.

**Tasks:**
- [ ] 4.1 Edit `samples/HelloPanel/HelloWidgets.cs`: add row 5 with the `TextField` (`Capacity = 32`) and a column (class `hello-status`) with the text label and the submitted label. Add `OnTextChanged(TextField)` and `OnSubmitted(TextField)`, the counts `Edits` and `Submits`, the property `TextField`, and `internal const int TextCapacity = 32`. Extend the remarks: the capacity, the copy of the text with no allocation, and the keyboard switch in `Program.RunFrames`.
- [ ] 4.2 Edit `samples/HelloPanel/HelloScreen.css`: add `TextField`, `TextField:hover`, `TextField:focus`, and `.hello-status > Label` (The Rule Matrix). The sheet then holds 31 rules.
- [ ] 4.3 Edit `samples/HelloPanel/Program.cs`: push the field press and release, the char or the Backspace by frame parity, and the main Enter before the move (The Smoke Widget Script). Add `Edits` and `Submits` to `RunResult`, to line 5, and to the widget check. Edit the remarks.
- [ ] 4.4 Edit `tests/BrutalUi.Markup.Tests/Style/HelloPanelSheetTests.cs`: the count 31, and 2 tests. `TheTextFieldRulesStyleTheField` (the width 240, the base fill, the text color, the hover fill, and the focus border color). `TheTextFieldHasRoomForOneLine` (after `Update`, the content box height of the field is at least the measured line height of its font size).
- [ ] 4.5 Run the build, the tests, the class, the regression set, and `scripts/test-linux.sh`. Record line 2, line 3, and line 5 in `context.md`.
- [ ] 4.6 Plant C: copy `samples/HelloPanel/HelloWidgets.cs` to the scratchpad. Add `GC.KeepAlive(new object());` as the first line of `OnTextChanged`. Run `scripts/test-linux.sh; echo "exit=$?"` and expect `FAIL at smoke (exit 1)` with the allocation line. In a later tool call, restore the file and check it with `cmp`. Record it.

**Estimated Steps:** about 35 (6 tasks, about six steps for one task)

**Files (5):** `HelloWidgets.cs`, `HelloScreen.css`, `Program.cs`, `HelloPanelSheetTests.cs`, `context.md`

**Acceptance Criteria:**
- [ ] `HelloPanelSheetTests` passes with a total of 10.
- [ ] `scripts/test-linux.sh` prints its PASS line. Line 3 shows `Update and Draw 0`. Line 5 shows each of the six counts above 0.
- [ ] Plant C gives `exit=1` with the allocation line, and `cmp` finds no difference after the restore.

---

### Phase 5: The 1000-Element Gate with Each Widget
**Goal:** Epic A9. A new gate builds the 1000-element tree with each widget type in it, pushes pointer, scroll, key, and char events to each widget, and asserts 0 bytes after warm-up for `Update` and `Draw`.
**Demo:** the developer runs `dotnet run --project tests/BrutalUi.Core.Tests/BrutalUi.Core.Tests.csproj -- --filter-class '*ThousandWidgetAllocationTests'` and reads a total of 4 with `failed: 0`. A planted allocation at the start of `TextField.AdvanceCaret` makes `dotnet test --solution brutal-ui.sln` exit with a value that is not 0, and the failed list holds `ThousandWidgetAllocationTests.UpdateAndDrawAllocateNothingWithEachWidget`.
**Needs:** Phase 4
**Agent tier:** ADVANCED (opus)

**Tier note:** The benchmark project and the ImGui tests link `ThousandElementTree.cs`. Add one overload only, and keep the result of `Build()`: `Build()` calls `Build(null)`, and `null` gives `new UiContext()`. Use no internal member of `BrutalUi.Core` in `ThousandElementTree.cs`. `ThousandWidgetTree.cs` lives in `BrutalUi.Core.Tests` only. Copy the gate shape of `ScrollViewAllocationTests`: a `Scene` class, a `Counter` class with instance methods as delegate targets, events built before the measure, `AllocationGate.ReleaseAllocationContext()` right before the measured region, and a measured body with no lambda, no `$"`, no new string, no boxing, and no `Assert`. Do not add warm-up rounds to hide an allocation. Do not run a plant and its restore in one parallel tool block.

**Tasks:**
- [ ] 5.1 Edit `tests/BrutalUi.Core.Tests/Shared/ThousandElementTree.cs`: add `public static ThousandElementTree Build(IUiRenderer? measurer)`. With a measurer, the context is `new UiContext(new UiContext.CreateInfo { Measurer = measurer })`. `Build()` calls `Build(null)`. Edit the doc comments of both overloads and the class remarks (a tree with a widget needs a measurer).
- [ ] 5.2 Write `tests/BrutalUi.Core.Tests/Shared/ThousandWidgetTree.cs`: `internal sealed class ThousandWidgetTree` with `Build()` (The Gate Tree), and the properties `Context`, `Renderer`, `Root`, `All` (the 1000 live elements in tree order), `Button`, `DisabledButton`, `Toggle`, `Slider`, `TextField`, `ScrollView`, and the viewport center of each widget. `Build` throws `InvalidOperationException` when the subtree count of the root is not 1000. The type selectors of the widget sheet use the real type names (`Button`, `Toggle`, and so on). Each other interned name (a class or an element name) uses a prefix that is unique to the class, because xUnit runs the classes in parallel.
- [ ] 5.3 Write `tests/BrutalUi.Core.Tests/ThousandWidgetAllocationTests.cs` with the step below. The step sets `ScrollView.ScrollOffset = default`, then pushes in this order: (1) a Left press and release, a scroll (0, -1), and a char `a` at the `Button` center, and an Enter key down. (2) The same four events at the disabled button. (3) The same at the `Toggle`, with a Space key down in place of Enter. (4) A Left press at 25 % of the slider width, a move to 75 %, a release, a scroll, a char `a`, and a Right key down. (5) A Left press and release at the `TextField`, a scroll, a char `a`, a Backspace key down, and an Enter key down. (6) A scroll (0, -1) at the view center, a Left press there, a move 20 pixels up, and a release. (7) A Tab key down. Each event has no target, so each key and each char goes to the focus owner that the press of its group set. The step then runs `Update(0.016f)` and `Draw`.
- [ ] 5.4 Write the 4 tests of the class: `TheTreeHoldsEachWidget` (1000 elements, one element of each widget type and one disabled button, `HitTest` of each widget center gives the widget, and the view content is taller than the view). `UpdateAndDrawAllocateNothingWithEachWidget` (the field holds the focus. Each frame marks `Style` on each element of `All` and runs `Update(0.016f)` and `Draw`. 100 frames after 2 warm-up rounds of 9 frames, so the caret pass, the blink, and the restyle of 1000 elements with both sheets run). `TheWidgetInputPathAllocatesNothing` (100 steps after 2 warm-up steps). `TheMeasuredBodyDoesRealWork` (after one step: 2 clicks, no click from the disabled button, 2 flips, 3 value changes, 2 scroll changes, 2 text changes, 1 submit, the text `field`, the view offset (0, 60), and a `PushClip` for the view and for the field in the frame). When a count differs from the list, read the widget rule, fix the step or the expected count, and record the cause in `context.md`.
- [ ] 5.5 Copy `src/BrutalUi.Core/Elements/TextField.cs` to the scratchpad. Add `GC.KeepAlive(new object());` as the first line of `AdvanceCaret`. Run `dotnet test --solution brutal-ui.sln > <scratch>/plant.log 2>&1; echo "exit=$?"`, then search `<scratch>/plant.log` for `failed` and `Failed` (tool rule 5). Expect a value that is not 0 and `UpdateAndDrawAllocateNothingWithEachWidget` in the failed list. In a later tool call, restore the file from the copy and check it with `cmp`. Record the failed gates in `context.md`.
- [ ] 5.6 Run the build, the tests, the regression set, and the benchmark build (`dotnet build tests/BrutalUi.Benchmarks/BrutalUi.Benchmarks.csproj 2>&1 | tail -n 5`). Run `git diff <base> --stat -- src` and expect no output. Record the counts.

**Estimated Steps:** about 45 (6 tasks, about eight steps for one task)

**Files (5):** `ThousandElementTree.cs`, `ThousandWidgetTree.cs`, `ThousandWidgetAllocationTests.cs`, `TextField.cs` (planted and restored), `context.md`

**Acceptance Criteria:**
- [ ] The 4 tests of `ThousandWidgetAllocationTests` pass.
- [ ] `ThousandElementAllocationTests`, `PointerAllocationTests`, `FocusAllocationTests`, and `ImGuiRendererAllocationTests` pass with no edit, and the benchmark project builds.
- [ ] The plant fails the new gate, and `cmp` finds no difference after the restore.

---

### Phase 6: The Manual Checks and the Record
**Goal:** Record the widget sheet rules, the sample, the smoke line 5, and the thirteenth gate in the technical design and the changelog. Give a person one manual check for each widget in both runbooks and in the epic `human-testing.md`.
**Demo:** a person opens the widget checklist of `docs/testing-linux.md`, runs `dotnet run --project samples/HelloPanel/HelloPanel.csproj` on a desktop, and does each item in order: the button, the disabled button, the toggle, the slider, the scroll view, the text field with the caret check, and Tab. Each item states one action and one result, and the person checks each one.
**Needs:** Phase 5
**Agent tier:** STANDARD (sonnet)

**Tier note:** Read each section before the edit, by range (tool rule 2). Copy the numbers from `context.md`. Do not run the sample again to get them. Keep each runbook step to one action and one result.

**Tasks:**
- [ ] 6.1 Edit `docs/technical-design.md` section 5: after the TextField paragraph, add a paragraph "**The widget sheet.**" with the rule matrix in prose: the bits that each widget gets, the four widgets that need a size rule, the fill rule for hover and active and the border rule for focus, the order rule for `Button:disabled` and `Toggle:checked`, the hover rule of the hit element (Finding 7), and the status label rule (the longest text first, a `u8` prefix, and `int.TryFormat` into a fixed buffer). Name `samples/HelloPanel/HelloWidgets.cs` as the example. Edit the comment of `HelloPanel/` in section 3 ("Phase 2: each widget of the epic").
- [ ] 6.2 Edit `docs/technical-design.md` section 14: add a "**A thirteenth allocation gate.**" item (`ThousandWidgetAllocationTests`, the swap of the footer, the widget sheet, the step, and the plant of task 5.5 with its failed gates). Name `HelloPanelSheetTests` in the stylesheet parser item. Edit the sample item: 5 lines, the widget script, the new numbers of line 2, line 4, and line 5, the new exit-1 line, and plants A, B, and C. Edit section 16: the widget-sample feature closes Phase 2.
- [ ] 6.3 Edit `docs/testing-linux.md`: add a section "8. The widget checklist" (a desktop session with a display, not `xvfb-run`). The items: the `Click me` click and its label, the hover fill and the focus border, the disabled button, the toggle and `:checked`, the slider drag and the Right key, the wheel and the drag of the list, the text field (type, the caret gap of 1 pixel or less, Enter, and the scroll under the caret), and Tab through the four focusable widgets. Add two rows to the table of section 6: `FAIL at smoke (exit 1)` with `allocated <B> bytes after warm-up`, and with `did not reach each widget`.
- [ ] 6.4 Edit `docs/testing-windows.md`: add the widget items to the look checklist of section 4 after item 2 and renumber. Add one line for each new item to the report block of section 5. Add the two smoke rows to the table of section 8.
- [ ] 6.5 Edit `docs/features/interactive-elements/human-testing.md`: in the `## widget-sample` section (create it when `/autorun-epic` did not), add one manual check for each widget (`Button`, `Toggle`, `Slider`, `ScrollView`, `TextField`), one for the disabled button, one for the caret gap with `ImGuiRenderer` (open risk 13 of the text-field review), and one Windows run of `scripts/test-windows.ps1` with the widget checklist.
- [ ] 6.6 Edit `CHANGELOG.md` under `## [Unreleased]`: "Added" (each widget in HelloPanel, the widget rules of `HelloScreen.css`, `HelloPanelSheetTests`, and the thirteenth gate) and "Changed" (the smoke run drives each widget, prints line 5, and exits with code 1 when a widget gets no change).
- [ ] 6.7 Run `python3 /home/aaronstatic/.claude/skills/asd-ste100/scripts/ste-lint.py --fail-over 2.5` on each changed doc, on `HelloWidgets.cs`, and on this plan. Fix each finding above 2.5.
- [ ] 6.8 Run the build, the tests, and `scripts/test-linux.sh; echo "exit=$?"`. Record the final test counts against the counts of task 1.1 in `context.md`.

**Estimated Steps:** about 35 (8 tasks, about four steps for one task)

**Files (6):** `technical-design.md`, `testing-linux.md`, `testing-windows.md`, `human-testing.md`, `CHANGELOG.md`, `context.md`

**Acceptance Criteria:**
- [ ] Each runbook holds the widget checklist, with one action and one result for each item.
- [ ] Section 5 holds the widget sheet paragraph, and section 14 holds the thirteenth gate and the 5 smoke lines.
- [ ] The lint gives no score above 2.5, and `scripts/test-linux.sh` prints its PASS line.

---

## Key Technical Decisions

### Confirmed Assumptions

None (a draft with no interview). `/autorun-epic` drafts this plan, and the person answers its open scope questions before the build.

### Decision 1: One new sample file for the widgets
**Choice:** `samples/HelloPanel/HelloWidgets.cs` builds the widget section and holds the status labels, the counts, and the delegates. `HelloScreen.Build` adds the section to the panel and returns the object.
**Rationale:** `HelloScreen.cs` holds the checker code, the sheet load, and the gate texts today. One more file keeps each file to one concern, and a game developer copies the widget part alone. Q2 of the epic: no new sample project.
**Alternatives:** (a) All in `HelloScreen.cs`: a file of more than 500 lines with three concerns. (b) A second sample: against Q2.

### Decision 2: The status rule: one delegate object and labels at their peak size
**Choice:** The delegates are instance methods of one `HelloWidgets` object, cached in fields at build time. Each one writes its label through `_scratch` with a `u8` prefix and `int.TryFormat` into the span, or with a copy of `TextField.Text`. Each label starts with its longest text.
**Rationale:** The smoke run measures each delegate (Finding 1). A delegate from an instance method allocates one time at build. `Utf8Text` doubles its buffer (Finding 4), so a longer count after warm-up would allocate. The longest-text rule is the rule of the gate label. The `IUtf8SpanFormattable` form writes UTF-8 with no string.
**Alternatives:** (a) `$"..."` and `SetText(string)`: one string for each change. (b) A static class with static fields: it works, but the gates use the counter object shape, and an object is easier to copy for two screens.

### Decision 3: The rule matrix of the sheet
**Choice:** The Rule Matrix. A hover or an active rule sets the fill (the thumb color on the slider), a focus rule sets the border color, `:checked` sets the fill after hover and active, and `Button:disabled` comes after hover and active. Each of the four widgets with no own size gets its size in its base rule.
**Rationale:** The requirement asks for a rule for each bit that each widget gets. The existing panel rules set hover and focus on different members, so the two states show at the same time. The specificity of a type with a state ties, so the order decides (Finding 9). The size lives in the sheet, as each size of the sample does.
**Alternatives:** (a) One `:disabled` rule with no type: a hovered disabled button shows the hover fill. (b) `:hover:checked` compound rules: more rules for no visible gain.

### Decision 4: The smoke run drives each widget (Notes Q2)
**Choice:** `PushSmokeInput` pushes a short script for each widget before the pointer move (The Smoke Widget Script). Line 5 prints the six counts. A count of 0 gives exit code 1 with one new line.
**Rationale:** The smoke run is the only GPU check that a headless verifier runs. With no widget input, no delegate, no label write, no caret pass, and no press map with `ImGuiRenderer` runs under the 0-byte check. The order keeps the off-panel frames off the UI (Finding 2). The exit codes keep the dev-ops contract, because the scripts read the exit code only.
**Alternatives:** Notes Q2, options B and C.

### Decision 5: The disabled widget is a second button (Notes Q3)
**Choice:** A `Button` with the text `Disabled` holds `PseudoStates.Disabled` from the build, in row 1 after the button status label. It has no `Clicked` delegate.
**Rationale:** The requirement asks for a `:disabled` rule where the widget gets the bit. The game sets that bit, not the widget. A fixed disabled button shows the rule with no new behavior and no focus drop in the smoke run.
**Alternatives:** Notes Q3, options B and C.

### Decision 6: The panel stays focusable
**Choice:** The panel keeps `IsFocusable = true` and its two state rules. Tab walks the panel and the four focusable widgets in tree order.
**Rationale:** The input check of the smoke run counts the focus in the UI, and the Tab of each frame keeps a focus owner. The panel rules and `TheHelloPanelSheetGivesTheOldValues` stay as they are. A press in a gap of the section focuses the panel, the nearest focusable ancestor.
**Alternatives:** The panel loses the focus: a change of an existing sample rule for no requirement.

### Decision 7: The sheet tests move the rule count to a new class
**Choice:** `HelloPanelSheetTests` (Markup tests) parses the sheet and checks each widget rule on a hand-built tree. `TheHelloPanelSheetGivesTheOldValues` loses its count line only.
**Rationale:** The old test pins the 6 old rules by their selector texts and values, and those stay. The count belongs with the widget rules that change it. A parse test needs no GPU, so a headless verifier checks the sheet. The Markup test project already links `HelloScreen.css`.
**Alternatives:** (a) Edit the count in the old test in each phase: the old test then changes four times. (b) No sheet test: only a person checks the sheet, on screen.

### Decision 8: The gate swaps footer slots for widgets (Notes Q4)
**Choice:** `ThousandElementTree.Build(IUiRenderer?)` gives the tree a measurer. `ThousandWidgetTree` removes the last 3 footer slots (9 elements) and adds 9 widget elements, so the tree keeps 1000 elements. A second sheet of 7 widget state rules goes in with `AddSheet`.
**Rationale:** A9 names the 1000-element tree. A `Button` and the caret pass need a measurer (Finding 11). The footer has no test handle and no overlay (Finding 12), so the existing handles stay valid and each widget center hits its widget. The overload keeps `Build()` and each existing gate the same.
**Alternatives:** Notes Q4, options B and C.

### Decision 9: The gate step gives each widget each event kind
**Choice:** The step of task 5.3. Each widget gets a press, a release, a scroll, and a char. Each focusable widget gets its key. The step leaves the text and the toggle as they were, and it resets the view offset from code.
**Rationale:** The requirement says "pushes pointer, scroll, key, and char events to each widget". An event that a widget does not take walks on, so the gate also measures that walk. A step that returns to the same state keeps each step the same, as in `ScrollViewAllocationTests`.
**Alternatives:** Only the events that each widget takes: a smaller step, but not the requirement text.

### Decision 10: No library change
**Choice:** `src/` has no diff. An allocation or a defect from `src/` stops the phase (the stop rule).
**Rationale:** The epic built and gated each widget. This feature shows and gates the set. A library fix needs its own review and its own plant.
**Alternatives:** A fix in this feature: a library change with no plan.

---

## Risks & Mitigation

| Risk | Likelihood | Impact | Mitigation |
|------|------------|--------|------------|
| A widget event after the pointer move puts the pointer over the UI in the off-panel frames, and the input check fails. | Medium | High | The widget script comes before the move (Finding 2, tier note of phase 1). The smoke run in each phase shows it. |
| A status label grows its buffer after warm-up, and the smoke run fails. | Medium | High | Each label starts with its longest text (The Widget Status Rule). Plants A and C prove that the smoke run sees a delegate allocation. |
| The caret pass or the press map allocates with `ImGuiRenderer` on the GPU. | Low | High | Phase 4 runs both in the smoke run. The stop rule records the path and does not fix `src/` here. |
| A new sheet rule breaks `TheHelloPanelSheetGivesTheOldValues`. | High | Low | Task 1.8 removes the count line only. `FindRule` still pins each old rule. |
| A state rule hides another state (a hovered disabled button, a hovered checked toggle). | Medium | Medium | The order of Decision 3. `TheDisabledRuleWinsOverHover` and `TheToggleRulesStyleTheToggle` pin it. |
| The panel loses its hover fill over a widget, and a person reads it as a defect. | High | Low | Finding 7. The runbook item states it. Conflicts and Gaps item 3. |
| The overload of `ThousandElementTree.Build` breaks the benchmark project or the ImGui tests. | Low | Medium | `Build()` keeps its result. Task 5.6 builds the benchmark project and runs `ImGuiRendererAllocationTests`. |
| An exact count of `TheMeasuredBodyDoesRealWork` differs from the plan. | Medium | Low | Task 5.4 tells the agent to read the widget rule, fix the step or the count, and record the cause. |
| The widgets do not fit the window of 1280 x 720. | Low | Medium | The section adds about 330 pixels under the image. The panel starts at 24 pixels. The demos of phases 1 to 4 check it on screen. |
| A person on Linux runs the widget checklist under Xvfb and sees nothing. | Medium | Low | Task 6.3 states a desktop session with a display. |
| The GPU on this machine needs a reboot after a driver update. | Low | Medium | `scripts/check-gpu.sh` fails first with a fix line. The verifier returns `Demo: MANUAL` for the GPU part. |
| `TransitionCurveTests.TheCurvesAllocateNothing` fails one time in a full run. | Low | Low | Known debt outside the epic. Run the suite again and record it (Conflicts and Gaps item 13). |

---

## Dependencies

### Internal Dependencies
- `interactive-elements/text-field`: `TextField`, the caret pass, and `TextFieldAllocationTests`. It is not in a commit at plan time. Task 1.1 checks that the caller committed it.
- `interactive-elements/scroll-view`, `slider`, and `button-and-toggle`: each widget, each delegate slot, each gate shape, and `WidgetInput`.
- `input-routing`: the event ring, `PushEvent`, the caller target rule, `Hover`, `Active`, the focus owner, Tab, and `RoutingGate`.
- `style-system` and `style-parser`: the five pseudo-classes, the type selectors, `StyleSheetParser`, and `AddSheet`.
- `imgui-renderer`: `ImGuiRenderer` as the measurer and the renderer of the sample, and `ImGuiRendererAllocationTests`.
- `dev-ops`: `scripts/test-linux.sh`, `scripts/test-windows.ps1`, the smoke contract (the arguments and the exit codes), `AllocationGate`, and `ThousandElementTree`.
- `hello-panel-sample` and `routing-gate`: the frame loop, the allocation region, the smoke input, and the input check of `Program.cs`.

### External Dependencies
- `int.TryFormat(Span<byte>, out int, ReadOnlySpan<char>, IFormatProvider?)` of .NET 10 (`IUtf8SpanFormattable`). No new package and no native dependency.
- The NVIDIA GPU and Xvfb of this machine for the smoke step, and a desktop session or Windows for the on-screen checks.

---

## Definition of Done

All criteria below must pass. An independent evaluator runs the Verification Method from `/home/aaronstatic/dev/brutal-ui` on Linux. `<base>` is the base commit that task 1.1 records in `context.md`.

### Functional Criteria
- [ ] F1. HelloPanel builds a widget section with a `Button`, a disabled `Button`, a `Toggle`, a `Slider`, a `ScrollView` with 8 item labels, and a `TextField` with a capacity of 32 (`HelloWidgets.cs`).
- [ ] F2. Each delegate slot of the sample (`Clicked`, `CheckedChanged`, `ValueChanged`, `ScrollChanged`, `TextChanged`, and `Submitted`) writes a status label through the scratch buffer with no string, and each status label starts with its longest text.
- [ ] F3. `HelloScreen.css` holds 31 rules: the 6 old rules and the 25 rules of The Rule Matrix. `HelloPanelSheetTests` (10 tests) checks that each widget rule reaches its widget, that `Button:disabled` and `Toggle:checked` win over hover, that the scroll content is taller than the view, and that the field has room for one line.
- [ ] F4. The smoke run prints 5 `HelloPanel:` lines. Line 5 shows six counts above 0. The run exits with code 0. A planted allocation in a sample delegate (plants A and C) gives exit code 1 with the allocation line. A smoke script with no button press (plant B) gives exit code 1 with `HelloPanel: the synthetic input did not reach each widget.`
- [ ] F5. `ThousandWidgetAllocationTests` (4 tests) builds the 1000-element tree with each widget type. It measures 0 bytes for `Update` and `Draw` with the focused field and a full restyle, and 0 bytes for the input step with pointer, scroll, key, and char events to each widget. `TheMeasuredBodyDoesRealWork` checks the change count of each widget.
- [ ] F6. A planted allocation in `TextField.AdvanceCaret` gives a non-zero exit code of `dotnet test`, and `UpdateAndDrawAllocateNothingWithEachWidget` is in the failed list.
- [ ] F7. `docs/testing-linux.md` and `docs/testing-windows.md` hold a widget checklist, and the `## widget-sample` section of the epic `human-testing.md` holds one manual check for each of the five widgets.

### Quality Criteria
- [ ] Q1. `dotnet build brutal-ui.sln` gives `0 Warning(s)` and `0 Error(s)`.
- [ ] Q2. `dotnet test --solution brutal-ui.sln` gives 0 failed. `BrutalUi.Markup.Tests` has 10 more tests than the count of task 1.1, and `BrutalUi.Core.Tests` has 4 more.
- [ ] Q3. `HelloWidgets.cs` holds no lambda, no `$"`, no `string.Format`, and no `.ToString(` call.
- [ ] Q4. The measured bodies of `ThousandWidgetAllocationTests.cs` hold no lambda, and each gate calls `AllocationGate.ReleaseAllocationContext()` right before its measure.
- [ ] Q5. The changed docs and the new doc comments use ASD-STE100 prose. The lint gives no score above 2.5.

### Integration Criteria
- [ ] I1. `src/`, `scripts/`, each `.csproj`, `Directory.Packages.props`, and `samples/WorldSpace/` have no diff against `<base>`.
- [ ] I2. Of the existing test files, only `ParsedSheetAllocationTests.cs` (one line removed and one comment) and `ThousandElementTree.cs` (the overload and its doc comments) have a diff.
- [ ] I3. `scripts/test-linux.sh` prints its PASS line.
- [ ] I4. Section 5 of `docs/technical-design.md` holds the widget sheet paragraph. Section 14 holds the thirteenth gate, `HelloPanelSheetTests`, and the 5 smoke lines. Section 16 states that the feature closes Phase 2. `CHANGELOG.md` names the sample widgets and the gate.

### Verification Method

Run each step from `/home/aaronstatic/dev/brutal-ui` on Linux. `<scratch>` is any empty folder outside the repository. Read `<base>` from `docs/features/interactive-elements/widget-sample/context.md`.

1. `dotnet build brutal-ui.sln 2>&1 | tail -n 30`. Expect `0 Warning(s)` and `0 Error(s)` (Q1).
2. `dotnet test --solution brutal-ui.sln 2>&1 | tail -n 30`. Expect 0 failed. Compare the Markup total and the Core total with the counts of task 1.1 in `context.md` (Q2).
3. `dotnet run --project tests/BrutalUi.Markup.Tests/BrutalUi.Markup.Tests.csproj -- --filter-class '*HelloPanelSheetTests' 2>&1 | tail -n 5`. Expect a total of 10 with `failed: 0` (F3). `dotnet run --project tests/BrutalUi.Core.Tests/BrutalUi.Core.Tests.csproj -- --filter-class '*ThousandWidgetAllocationTests' 2>&1 | tail -n 5`. Expect a total of 4 with `failed: 0` (F5). Read the test names and match them to the criteria.
4. `scripts/test-linux.sh; echo "exit=$?"`. Expect the PASS line and `exit=0`. Read `artifacts/test-linux/smoke.log`. Expect 5 `HelloPanel:` lines, line 3 with `Update and Draw 0`, and line 5 with each of `clicks`, `flips`, `values`, `scrolls`, `edits`, and `submits` above 0 (F4, I3).
5. Copy `samples/HelloPanel/HelloWidgets.cs` to `<scratch>`. Add `GC.KeepAlive(new object());` as the first line of `OnClicked`. Run `scripts/test-linux.sh; echo "exit=$?"`. Expect `FAIL at smoke (exit 1)`, the allocation line, and `exit=1`. Restore the file from the copy, and check it with `cmp` (F4).
6. Copy `src/BrutalUi.Core/Elements/TextField.cs` to `<scratch>`. Add `GC.KeepAlive(new object());` as the first line of `AdvanceCaret`. Run `dotnet test --solution brutal-ui.sln > <scratch>/plant.log 2>&1; echo "exit=$?"`. Expect a value that is not 0, and search `<scratch>/plant.log` for `UpdateAndDrawAllocateNothingWithEachWidget`. Restore the file, check it with `cmp`, and run step 2 again (F6).
7. `git diff <base> --stat -- src scripts Directory.Packages.props '*.csproj' samples/WorldSpace`. Expect no output (I1).
8. `git diff <base> --diff-filter=M --name-only -- tests/`. Expect only `tests/BrutalUi.Markup.Tests/Style/ParsedSheetAllocationTests.cs` and `tests/BrutalUi.Core.Tests/Shared/ThousandElementTree.cs`. Read `git diff <base> -- tests/BrutalUi.Markup.Tests/Style/ParsedSheetAllocationTests.cs` and expect one removed assert line and one comment (I2).
9. `grep -nE '\$"|string\.Format|\.ToString\(|\) =>' samples/HelloPanel/HelloWidgets.cs`. Expect no line (Q3). Read the measured bodies of `ThousandWidgetAllocationTests.cs` (Q4).
10. Read section 5 (the widget sheet paragraph), section 14 (the thirteenth gate, `HelloPanelSheetTests`, and the sample item), and section 16 of `docs/technical-design.md`, the widget checklists of `docs/testing-linux.md` and `docs/testing-windows.md`, the `## widget-sample` section of `docs/features/interactive-elements/human-testing.md`, and the `## [Unreleased]` part of `CHANGELOG.md` (F7, I4). Run `python3 /home/aaronstatic/.claude/skills/asd-ste100/scripts/ste-lint.py --fail-over 2.5` on each changed doc (Q5).
11. The on-screen part needs a person at a desktop, not a headless evaluator. Run `dotnet run --project samples/HelloPanel/HelloPanel.csproj` and do the widget checklist of `docs/testing-linux.md`. A headless evaluator returns `Demo: MANUAL` for this step.

---

## Testing Strategy

### Unit Tests
- `BrutalUi.Markup.Tests/Style/HelloPanelSheetTests` (10): the rule count and the unique selectors, the button rules, the row label rule, the toggle rules, the slider rules, the disabled rule over hover, the scroll view rules, the scroll content height, the text field rules, and the line height of the field. Each test parses the real `HelloScreen.css` and builds the sample tree by hand with the same classes.

### Allocation Tests
- `BrutalUi.Core.Tests/ThousandWidgetAllocationTests` (4): the tree check, `Update` and `Draw` with the focused field and a full restyle, the input step to each widget, and the real-work check. The plant of task 5.5 proves that the gate sees an allocation in the caret pass.
- The smoke run is the GPU gate. Plants A and C prove that it sees an allocation in a sample delegate.

### Regression Tests
- The regression set of the phase intro with no edit in each phase, and the benchmark build in phase 5.

### Integration Tests
- `scripts/test-linux.sh` runs the build, the tests, the GPU check, and the HelloPanel smoke run with each widget driven in each frame. Plant B proves that the widget check fails a run with no widget input.
- `scripts/test-windows.ps1` runs the same smoke step on Windows. A person runs it (Conflicts and Gaps item 8).

### Manual Testing
- The widget checklist of `docs/testing-linux.md` and `docs/testing-windows.md`: one action and one result for each widget, the disabled button, the caret gap with `ImGuiRenderer`, and Tab.
- The `## widget-sample` section of the epic `human-testing.md` lists the same checks for the epic sign-off.

---

## Related Documentation

- **Requirements:** `docs/features/interactive-elements/widget-sample/requirements.md`
- **Epic:** `docs/features/interactive-elements/epic-overview.md`, `docs/features/interactive-elements/epic-requirements.md`, `docs/features/interactive-elements/human-testing.md`
- **Siblings:** `docs/features/interactive-elements/button-and-toggle/implementation.md`, `slider/implementation.md`, `scroll-view/implementation.md`, `text-field/implementation.md`, and the open items of `scroll-view/code-review.md` (CR-4) and `text-field/code-review.md` (open risks 11 and 13, follow-up 2)
- **Design:** `docs/technical-design.md` sections 3, 5 (Widgets and the sibling paragraphs), 9 (item 7 of the flow), 11 (Stylesheet format), 14 (Testing Strategy), and 16
- **Runbooks:** `docs/testing-linux.md`, `docs/testing-windows.md`, `docs/features/dev-ops/solution-skeleton/implementation.md` (the smoke contract)
- **Rules:** `AGENTS.md`, `.beast-mode/phase-handoff.md` sections 1 and 11, `.beast-mode/tool-rules.md`
- **Code patterns:** `samples/HelloPanel/Program.cs` (`RunFrames`, `PushSmokeInput`, the input check), `samples/HelloPanel/HelloScreen.cs` (the gate label and its longest text), `samples/HelloPanel/HelloScreen.css`, `tests/BrutalUi.Markup.Tests/Style/ParsedSheetAllocationTests.cs` (the sheet test and its measurer), `tests/BrutalUi.Core.Tests/Shared/ThousandElementTree.cs`, `tests/BrutalUi.Core.Tests/ThousandElementAllocationTests.cs`, `tests/BrutalUi.Core.Tests/Elements/ScrollViewAllocationTests.cs` (the gate shape), `tests/BrutalUi.Core.Tests/Input/PointerTree.cs` (the event helpers), and `tests/BrutalUi.Core.Tests/Shared/AllocationGate.cs`
- **Skills:** no implementation skill in `.claude/skills/` applies. The skills there are workflow commands.

---

## Notes

### Open Scope Questions

No person answered these questions. Each one uses the recommended option.

**Q1. What is the demo of phase 1?**
- **A (recommended):** a person runs `dotnet run --project samples/HelloPanel/HelloPanel.csproj` on a desktop, clicks the `Click me` button two times, and reads `Button clicks: 2` next to it. A headless verifier reads the PASS line of `scripts/test-linux.sh` and `clicks <C>` above 0 in line 5 of `smoke.log`. Reason: it shows the widget, the delegate, the label, and the smoke contract in one path, and the headless part runs on this machine.
- B: a person runs `scripts/test-linux.sh` and reads its PASS line and line 5 only. Reason: fully headless, but no person sees the button.
- C: the developer runs the class filter `'*HelloPanelSheetTests'` and reads a total of 3 with `failed: 0`. Reason: a fast unit demo, but it shows the sheet and not the sample.

**Q2. Does the smoke run drive each widget?**
- **A (recommended):** yes. The run pushes a widget script in each frame, prints line 5 with the six counts, and exits with code 1 when a count is 0 (Decision 4). Reason: the only headless GPU check of the delegates, the label writes, the caret pass, and the press map under the 0-byte check.
- B: yes, but line 5 is a report only, with no new exit-1 reason. Reason: a smaller contract change, but a broken smoke script passes with no signal.
- C: no. The run keeps the move and the Tab only. Reason: no `Program.cs` change for the widgets, but the GPU run measures no widget delegate.

**Q3. How does the sample show `:disabled`?**
- **A (recommended):** a second `Button` with the text `Disabled` holds `Disabled` from the build (Decision 5). Reason: one static element, no new behavior, and no focus drop in the smoke run.
- B: the `Toggle` sets `Disabled` on the slider and the text field. Reason: a live switch that shows the focus drop, but the toggle then does two jobs, and the smoke run flips it in each frame.
- C: no disabled widget. The sheet holds the rule only. Reason: no extra element, but no person sees the rule.

**Q4. How does the gate put each widget into the 1000-element tree?**
- **A (recommended):** a `Build(IUiRenderer?)` overload of `ThousandElementTree` gives a measurer, and `ThousandWidgetTree` swaps the last 3 footer slots (9 elements) for 9 widget elements, so the tree keeps exactly 1000 elements (Decision 8). Reason: the shared builder keeps its result for each existing gate, and the count stays 1000.
- B: add the 9 widget elements to the tree, so it holds 1009 elements. Reason: no swap, but the name of the gate no longer matches the count.
- C: a widget variant inside `ThousandElementTree.Build`. Reason: one builder, but a change of the shared tree for the benchmark and the ImGui tests.

### Conflicts and Gaps

1. **An existing test changes.** `TheHelloPanelSheetGivesTheOldValues` asserts 6 rules. Each new rule breaks that line, so task 1.8 removes it. The other checks of the test stay.
2. **A shared test file changes.** `ThousandElementTree.cs` gets one overload (Q4). The benchmark project and the ImGui tests link it. `Build()` keeps its result.
3. **Hover is on the hit element only.** The panel loses `.hello-panel:hover` while the pointer is over a widget, and `ScrollView:hover` matches only over the view itself or an item label that is not pickable. The input-routing rule stays. The runbook item states it.
4. **Section 5 already holds the widget rules.** The siblings wrote a paragraph for each widget. This feature adds the sheet paragraph only (the rule matrix, the size rule, the order rule, and the status label rule).
5. **The cost of `IndexAtX` with a large capacity** (text-field review, follow-up 2). The requirement puts a widget benchmark out of scope. The sample uses a capacity of 32, so the cost stays small. The question stays open for a chat field.
6. **The caret gap with `ImGuiRenderer`** (text-field open risk 13). No pixel compare exists, so a person checks the gap on screen (task 6.5).
7. **The keyboard switch.** The text-field review asks the sample to show `input.KeyboardEnabled = !io.WantCaptureKeyboard`. `RunFrames` already has the line. Phase 4 names it in the remarks of `HelloWidgets`.
8. **`scripts/test-windows.ps1` must pass.** The script needs no change, because it reads the exit code only. No agent can run it on this machine. A person runs it with the widget checklist (task 6.5).
9. **The flex-grow size of a scroll view** (scroll-view CR-4). The sample gives the view a fixed size. The question of an automatic minimum size of 0 stays open.
10. **The smoke output grows to 5 lines.** The dev-ops contract is the arguments and the exit codes, and the scripts read the exit code only. Line 5 and the new exit-1 reason keep the contract. Section 14 records them.
11. **text-field is not in a commit at plan time.** Task 1.1 stops the feature when `TextField.cs` is not in the base commit.
12. **A scroll or a char to a widget that does not take it.** The requirement asks for each event kind to each widget. The gate pushes them, and they walk on. The gate thus measures the walk, not a widget step.
13. **Known debt of the epic.** A press that a capture handler on an ancestor stops keeps the press target. `:disabled` on an ancestor does not block a widget. `RoutingGate.PointerOverUi` follows the hit during a captured drag. `TransitionCurveTests.TheCurvesAllocateNothing` fails one time in some full runs. This plan changes none of these. A flaky full run is not a failure of this feature. Run it again and record it.
14. **Draw commands and vertices change.** Line 2 of the smoke run gets new numbers with the widgets and the clips. No script reads them. Section 14 records the new values from `context.md`.
15. **No edit tool for this plan.** The planner had no edit tool, so it wrote the plan file two times in full. The second write fixed the rule counts and the task total.
