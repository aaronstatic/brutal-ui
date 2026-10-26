# interactive-elements/button-and-toggle - Context & Decisions

**Last Updated:** 2026-10-26 06:28
**Current Phase:** Done (phases 1 to 6 and the cross-phase review)
**Status:** ✅ Ready for Review

---

## Quick Status

**What's Done:**
- ✅ The plan is accepted. The feature docs exist (`tasks.md`, `context.md`, `inbox.md`).
- ✅ The input-routing epic is committed on `main`. The branch `feat/interactive-elements/button-and-toggle` holds this feature.
- ✅ Phase 1, group A (tasks 1.1 to 1.5): `Active` stays on the pressed element through the release walk (Decision 2). `ReleaseOrderTests` (4) pass.
- ✅ Phase 1, group B (tasks 1.6 to 1.10): `WidgetInput.IsClick`, `Button`, `WidgetTree`, and `ButtonClickTests` (8) pass. `scripts/test-linux.sh` prints its PASS line.
- ✅ Phase 2 (tasks 2.1 to 2.4): Enter and Space on a focused button. `ButtonKeyTests` (9) pass. The verifier gave PASS and Demo: PASS.
- ✅ Phase 3 (tasks 3.1 to 3.4): `Toggle` with the `Checked` bit. `ToggleTests` (10) pass. The verifier gave PASS and Demo: PASS.
- ✅ Phase 4 (tasks 4.1 to 4.7): the shared `Utf8Text` struct and the button text. `ButtonTextTests` (9) pass, and each `Label` test passes with no edit. The verifier gave PASS and Demo: PASS.
- ✅ Phase 5 (tasks 5.1 to 5.3): the disabled rule in `WidgetInput`. `WidgetDisabledTests` (7) pass. The verifier gave PASS and Demo: PASS.
- ✅ Phase 6 (tasks 6.1 to 6.6): `WidgetAllocationTests` (4) pass, the plant fails the run (exit 2), and the docs and the changelog hold the widget pattern. The verifier gave PASS and Demo: PASS.
- ✅ The cross-phase review: 13 findings, 12 fixed, and CR-1 deferred to a person. The verifier of the fixes gave PASS. The solution gives 2332 tests (Core 1500, 51 more than the baseline), 0 failed, 2 skipped.
- ✅ The draft plan of the next feature exists: `docs/features/interactive-elements/slider/implementation.md`.

**What's Next:**
- 📋 A person decides the chord rule of CR-1 (`code-review.md`).
- 📋 A person runs the items of "Needs Human Verification".
- 📋 `/autorun-epic` commits the feature and runs the epic rollup.

**Blockers:**
- None

---

## Key Files

### Core Implementation
- `src/BrutalUi.Core/Elements/WidgetInput.cs` - The shared click check and key check of each widget (phases 1, 2, and 5).
- `src/BrutalUi.Core/Elements/Button.cs` - The `Button` widget: click, keys, and text (phases 1, 2, and 4).
- `src/BrutalUi.Core/Elements/Toggle.cs` - The `Toggle` widget: the `Checked` bit and the flip (phase 3).
- `src/BrutalUi.Core/Elements/Utf8Text.cs` - The shared UTF-8 text type of `Label` and `Button` (phase 4).
- `src/BrutalUi.Core/Tree/UiContext.cs` - The release order of `Active` (phase 1) and the locale refresh of a `Button` (phase 4).

### Related Files
- `src/BrutalUi.Core/Elements/Label.cs` - Uses `Utf8Text` after phase 4, with no API change.
- `tests/BrutalUi.Core.Tests/Elements/WidgetTree.cs` - The test tree of each widget test class.
- `tests/BrutalUi.Core.Tests/Input/PointerTree.cs` - The event factories that the widget tests use.
- `docs/features/interactive-elements/button-and-toggle/implementation.md` - Implementation plan.

---

## Important Decisions

The plan holds Decisions 1 to 10 and the confirmed items Q1 to Q6 and A1 to A7. This section records each decision that the build adds.

- **B1 (phase 1, group A): the order in `FinishRelease`.** The method sets `_activeElement` to null before it clears the state bit. The `States` setter marks the subtree style. When that mark throws, the context holds no stale reference.
- **B2 (phase 1, group A): the order in the `finally` block.** The block clears `_draining` first and then calls `FinishRelease`. When `FinishRelease` throws, the context still accepts the next drain.
- **B3 (phase 1, group A): a reentrant drain does not finish a release.** The reentrant call throws before its `try` block, so its `finally` block does not run. The pending release of the outer drain stays pending until the outer walk ends.
- **B4 (phase 1, group A): a handler that removes the pressed element.** `FinishRelease` clears `Active` on the removed element at once. The stale rule (Decision 10) gives the same result at the start of the next drain.
- **B5 (phase 1, group A): extra cover in `ReleaseOrderTests`.** `TheDrainClearsActiveAfterTheReleaseWalk` also checks a release over no element (no walk, `Active` clears). The brief names this case in task 1.2.
- **B6 (phase 1, group B): the remark on the `+=` handlers of `OnPointerUp`.** `EventWalk.InvokeSlot` stops after the delegate that stops the event. Thus a handler that `+=` adds after the click handler gets no call for a click. It gets each release that is not a click. The remarks of `Button` state this rule. The first draft said that the handlers of the same slot still run, and the walk code shows the opposite.
- **B7 (phase 1, group B): `WidgetTree` takes no name prefix.** The tree interns no class, so the parallel runs share no class id. `ElementType.Intern("Button")` gives the same id at each call, so the type rule test needs no prefix. A test that needs a class interns it with its own prefix.
- **B8 (phase 1, group B): extra cover in `ButtonClickTests`.** The 8 test names stay as the plan names them. Some tests check more cases: a release over no element, a release with no press, a middle button, a left click after the other buttons, the order of the root capture call before `Clicked`, and the focus after the release.
- **B9 (phase 1, group B): no name conflict.** The type `Button` and the field `PointerEvent.Button` give no build error and no warning in each project of the solution. The `WidgetTree.Button` property of the type `Button` resolves by the "Color Color" rule, so `new Button(new Button.CreateInfo())` builds in the tree.
- **B10 (phase 1, group B): the unused `info` parameter.** The empty `CreateInfo` gives an unused parameter. The build shows no warning for it, so the constructor holds no discard line.

---

## Gotchas & Learnings

- A stop inside a slot also skips the later delegates of the same slot (`EventWalk.InvokeSlot`). The doc of a widget must not say that the other handlers of its slot still run.
- `UiContext.PushEvent` has one overload for each event struct. It is not generic, so a `cref` names `PushEvent(in PointerEvent)`.
- The click rule reads `Active`, and `PressButton` sets `Active` on the first press of a chord only. See open question OQ1.

---

## API Integration

- `Button.Clicked` (`Action<Button>?`) and `Toggle.CheckedChanged` (`Action<Toggle>?`): the delegate slots that the game sets at build time.
- The widgets read the event slots `OnPointerUp` and `OnKeyDown`, `PseudoStates.Active`, and the focus owner of input-routing.

---

## State Management

- `Toggle.Checked` reads and writes `PseudoStates.Checked` in `Element.States`. No field holds the state (Decision 4).
- `UiContext` holds `_releasePending`. `FinishRelease` clears `Active` after the release walk (Decision 2).

---

## Testing Approach

### Unit Tests
**Files:** `tests/BrutalUi.Core.Tests/Input/ReleaseOrderTests.cs`, `tests/BrutalUi.Core.Tests/Elements/*Tests.cs`
- `ReleaseOrderTests` (4), `ButtonClickTests` (8), `ButtonKeyTests` (9), `ToggleTests` (10), `ButtonTextTests` (9), `WidgetDisabledTests` (7).
- Each `Label` test and each pointer and focus test passes with no edit.

### Allocation Tests
**Files:** `tests/BrutalUi.Core.Tests/Elements/WidgetAllocationTests.cs`
- 4 tests: `Update` and `Draw`, the input path, `SetText`, and the real-work check.

---

## Needs Human Verification

`/autorun-feature` adds one line per item that the verifier cannot check, for a person to test.

- [ ] Phase 1 demo: a person runs `scripts/test-linux.sh` and reads its PASS line. In that run, `ButtonClickTests.APressAndAReleaseClickTheButtonOnce` pushes a press and a release over a `Button` through `UiContext.PushEvent`, runs `Update`, and counts one `Clicked` call with the button as the sender.
- [ ] Phase 2 demo: a unit test calls `SetFocus(button)`, pushes an Enter key down with no target, and runs `Update`. It counts one `Clicked` call. The developer runs `dotnet run --project tests/BrutalUi.Core.Tests/BrutalUi.Core.Tests.csproj -- --filter-class '*ButtonKeyTests'` and reads a total of 9 with `failed: 0`.
- [ ] Phase 3 demo: a unit test pushes a press and a release over a `Toggle` and runs `Update`. It reads `Checked` as `true`, `PseudoStates.Checked` in `States`, the fill of a `Toggle:checked` rule, and one `CheckedChanged` call that reads the new value. The developer runs `dotnet run --project tests/BrutalUi.Core.Tests/BrutalUi.Core.Tests.csproj -- --filter-class '*ToggleTests'` and reads a total of 10 with `failed: 0`.
- [ ] Phase 4 demo: a unit test creates a `Button` with `Text = "Play"` and a padding of 8, runs `Update` and `Draw` with a `RecordingRenderer`, and reads one `Text` call with the bytes of `Play` at the top-left corner of the content box. The button size is the measured text size plus the padding. The developer runs `dotnet run --project tests/BrutalUi.Core.Tests/BrutalUi.Core.Tests.csproj -- --filter-class '*ButtonTextTests'` and reads a total of 9 with `failed: 0`.
- [ ] Phase 5 demo: a unit test sets `Disabled` on the `Button`, pushes a press and a release over it, and runs `Update`. It counts 0 `Clicked` calls, and the root bubble handler gets the release. The developer runs `dotnet run --project tests/BrutalUi.Core.Tests/BrutalUi.Core.Tests.csproj -- --filter-class '*WidgetDisabledTests'` and reads a total of 7 with `failed: 0`.
- [ ] Phase 6 demo: the developer runs `dotnet run --project tests/BrutalUi.Core.Tests/BrutalUi.Core.Tests.csproj -- --filter-class '*WidgetAllocationTests'` and reads a total of 4 with `failed: 0`. A planted allocation at the start of `Button.HandlePointerUp` makes `dotnet test --solution brutal-ui.sln` exit with a value that is not 0.
- [ ] Flaky test outside this feature: `TransitionCurveTests.TheCurvesAllocateNothing` failed one time in a full run ("ease-out allocated 8056 bytes") and passed in each later run. A person decides whether the gate needs a fix.
- [ ] CR-1 of `code-review.md`: a person decides the chord rule. Today a right press on a button, a left press on another element, and a left release over the button give a click.

---

## Performance Considerations

- Each widget shares one cached static delegate for each slot (Decision 3). The constructor allocates only the side object of the slots.
- `Utf8Text` is a struct that each owner holds inline, so it adds no heap object to an element (Decision 7).

---

## Next Steps

### Immediate (Today/This Session)
1. A person decides the chord rule of CR-1. The review proposes a click target that `UiContext` sets for the walk of a release that ends a press of one left button.
2. A person runs the items of "Needs Human Verification".
3. `/autorun-epic` commits the feature. The next feature is `interactive-elements/slider`, and its draft plan exists.

### Short Term
1. A person decides whether `:disabled` on an ancestor blocks the widgets (the follow-up question of the review).
2. A person decides whether the flaky gate `TransitionCurveTests.TheCurvesAllocateNothing` needs a fix (outside this feature).

---

## Phase 1 Results

### Baseline (task 1.1, before any change)

- The build of `brutal-ui.sln`: `0 Warning(s)`, `0 Error(s)`.
- The full test run: 2281 in total, 2279 passed, 2 skipped, 0 failed.
- `BrutalUi.Core.Tests`: 1449 in total, 1449 passed, 0 failed.
- The pointer and focus classes (each 0 failed): `PointerPressTests` 8, `PointerStateEdgeTests` 8, `PointerAllocationTests` 3, `ClickFocusTests` 8, `FocusOwnerTests` 8, `TabNavigationTests` 12, `FocusStateEdgeTests` 8, `FocusAllocationTests` 3, `RoutingGateTests` 8. `ThousandElementAllocationTests` 14.

### The Active order finding (task 1.1)

- `DrainEvents` calls `RoutePointer` in the target step, before the walk. For a `PointerUp`, `RoutePointer` calls `ReleaseButton`.
- When the last button goes up, `ReleaseButton` clears `PseudoStates.Active` on `_activeElement` and drops the reference. Then the walk runs.
- Thus a `PointerUp` handler before this phase reads no `Active` on any element.
- No existing test reads `Active` inside a release handler. Each test reads the states after the drain (`PointerPressTests`, `PointerStateEdgeTests`, `GlfwUiInputTests`).
- `_activeElement` has 3 write sites only: `PressButton`, `ReleaseButton`, and `DropStalePointerState`. No other path (cursor leave, root removal, gate) writes it.

### Blast radius of Decision 2 (task 1.2)

- `RoutePointer` has one caller: `DrainEvents`. `ReleaseButton` has one caller: `RoutePointer`. No code outside `UiContext.cs` reads `_activeElement` or sets `PseudoStates.Active`.
- The states after a drain do not change. Only a handler inside the walk of a release sees a difference. The style pass, `GlfwUiInput`, and the routing gate read the states after the drain only.

### After group A (task 1.5)

- The build of `brutal-ui.sln`: `0 Warning(s)`, `0 Error(s)`.
- The full test run: 2285 in total, 2283 passed, 2 skipped, 0 failed (4 more than the baseline).
- `BrutalUi.Core.Tests`: 1453 in total, 1453 passed, 0 failed.
- `ReleaseOrderTests`: 4 passed.
- The pointer and focus classes have no edit and give the baseline counts, each with 0 failed: `PointerPressTests` 8, `PointerStateEdgeTests` 8, `PointerAllocationTests` 3, `ClickFocusTests` 8, `FocusOwnerTests` 8, `TabNavigationTests` 12, `FocusStateEdgeTests` 8, `FocusAllocationTests` 3, `RoutingGateTests` 8. `ThousandElementAllocationTests` 14.
- `git diff --stat` shows `UiContext.cs` as the only changed tracked file. `EventWalk.cs` and `Element.cs` have no diff.

### After group B (task 1.10, the phase 1 gate)

- The build of `brutal-ui.sln`: `0 Warning(s)`, `0 Error(s)`.
- The full test run: 2293 in total, 2291 passed, 2 skipped, 0 failed (12 more than the baseline).
- `BrutalUi.Core.Tests`: 1461 in total, 1461 passed, 0 failed.
- `ReleaseOrderTests`: 4 passed. `ButtonClickTests`: 8 passed, with the demo `APressAndAReleaseClickTheButtonOnce`.
- The pointer and focus classes have no edit and give the baseline counts, each with 0 failed: `PointerPressTests` 8, `PointerStateEdgeTests` 8, `PointerAllocationTests` 3, `ClickFocusTests` 8, `FocusOwnerTests` 8, `TabNavigationTests` 12, `FocusStateEdgeTests` 8, `FocusAllocationTests` 3, `RoutingGateTests` 8. `ThousandElementAllocationTests` 14.
- `scripts/test-linux.sh; echo "exit=$?"`: `test-linux: PASS build, test, gpu, smoke (120 frames) in 16 s` and `exit=0`.
- `EventWalk.cs` and `Element.cs` have no diff. `UiContext.cs` is the only changed tracked file. The new files are `WidgetInput.cs`, `Button.cs`, `WidgetTree.cs`, and `ButtonClickTests.cs`.

### Verifier (brief `phase-1-verify.md`)

- PASS. Demo: PASS. The solution gives 2293 tests, 0 failed, 2 skipped (no display). `scripts/test-linux.sh` gives its PASS line and `exit=0`. The four facts of the `UiContext.cs` diff and the four facts of `Button.cs` and `WidgetInput.cs` hold.

---

## Phase 2 Results

- Build: `0 Warning(s)`, `0 Error(s)`.
- Full run (`dotnet test --solution brutal-ui.sln`): total 2302, failed 0, succeeded 2300, skipped 2.
- `ButtonKeyTests` 9 of 9 pass. `ButtonClickTests` 8, `ReleaseOrderTests` 4, `TabNavigationTests` 12, and `FocusOwnerTests` 8 pass with no edit.
- Decision: `IsKeyActivation` also checks that the event type is `KeyDown`, as a guard. The brief lists no such check, and the handler sits in `OnKeyDown`, so it does not change the result.
- Decision: `SpaceKey`, `EnterKey`, and `ActivationKeys` sit in `WidgetInput.cs`. The test file reads the key constants through `InternalsVisibleTo` (the tests already read `UiContext.TabKey` that way).
- No surprise. `Click()` is a private instance method that both handlers call.
- Verifier (brief `phase-2-verify.md`): PASS. Demo: PASS. The solution gives 2302 tests, 0 failed, 2 skipped. The five facts of `WidgetInput.cs` and `Button.cs` hold. The keypad Enter (335) does not click.

---

## Phase 3 Results

- Build: `0 Warning(s)`, `0 Error(s)`.
- Full run (`dotnet test --solution brutal-ui.sln`): total 2312, failed 0, succeeded 2310, skipped 2 (no display).
- `ToggleTests` 10 of 10 pass. `ReleaseOrderTests` 4, `ButtonClickTests` 8, and `ButtonKeyTests` 9 pass with no edit.
- Decision: `Toggle.Checked` reads and writes the `PseudoStates.Checked` bit of `States`. `Toggle.cs` holds no `bool` field.
- Decision: `Toggle` has no `Inline` size, no measure callback, and no `PaintContent`. `WidgetTree` gives the test toggle an inline size of 40 x 40.
- Decision: `WidgetTree` takes additions only: `Toggle`, `OverToggle` (240, 20), and the toggle as the third child of the root. The class summary of `WidgetTree` still lists two children. A later edit can update it.
- Demo: a type rule and a `Toggle:checked` rule set two fills. A click changes the fill from the plain fill to the checked fill.
- No surprise.
- Verifier (brief `phase-3-verify.md`): PASS. Demo: PASS. The solution gives 2312 tests (Core 1480), 0 failed, 2 skipped. The six facts of `Toggle.cs` hold, and `Toggle.cs` holds no `bool` field.

---

## Phase 4 Results

### Group A (tasks 4.1 to 4.3): the shared text type

- Build: `0 Warning(s)`, `0 Error(s)`.
- Full run (`dotnet test --solution brutal-ui.sln`): total 2312, failed 0, succeeded 2310, skipped 2 (no display). The count is the same as after phase 3, because group A adds no test.
- The `Label` gate, before and after the refactor, with no test edit: `LabelTests` 22, `LabelTextTests` 35, `LabelWrapTests` 22, `ContentBoxTests` 16, `PaintContentTests` 9, `ElementAllocationTests` 18. The ImGui classes that name `Label`: `ImGuiLabelTests` 7, `ImGuiRendererAllocationTests` 4. Each class gives 0 failed.
- `git diff main --stat -- tests/` gives no file. The new test files of phases 1 to 3 are untracked, and no existing test file changed.
- `grep -rn 'readonly Utf8Text' src/` gives no line.
- `git diff main -- src/BrutalUi.Core/Elements/Label.cs`: the only changed `public` lines are the bodies of `Text` (`_text.Span`) and `LocalizedText` (`_text.Localized`). Each public signature and each public doc comment stays the same.
- Blast radius: `Label.cs` and the new `Utf8Text.cs` only. `UiContext.RefreshSubtree` calls `Label.RefreshLocalized`, which keeps its signature. The tests call `Label.Utf8Equals`, `Label.SnapWrapWidth`, and `Label.DrawWrapWidth`, which stay as internal forwarders.
- Decision: the parameter lists of `Utf8Text.Measure` and `Utf8Text.Paint` match the sketch of the plan. No change was necessary. `Label.PaintContent` computes the content box, then calls `Utf8Text.Paint(renderer, in Style, in content, borderBox.Z, opacity, Text)`.
- Decision: the measure rule doc and the draw rule doc moved to `Utf8Text.Measure` and `Utf8Text.Paint`. The doc comments of the three forwarders and of `MeasureLabel` keep their summaries and point to the struct for the rule. `Label.PaintContent` keeps its full doc comment.
- Decision: the struct docs say "returns `true`" where the `Label` docs said "marks `Layout` and `Paint`". The rule is the same. The owner marks the bits.
- Decision (edge case that the plan does not name): a `default` instance of `Utf8Text` has a `null` buffer. `Span` gives an empty span for it. `EnsureCapacity` creates the first buffer from `InitialTextCapacity` for a `null` buffer. A `Label` always creates the struct with the constructor, so the rule of `Label` does not change. Group 2 can hold the struct in a `Button` with no risk from a missed constructor call.
- Decision: when both texts are set, the struct constructor uses the localized text. The owner checks the "both set" rule first, so this path does not occur through `Label`.

### Group B (tasks 4.4 to 4.7): the button text

- Build: `0 Warning(s)`, `0 Error(s)`.
- Full run (`dotnet test --solution brutal-ui.sln`): total 2321, failed 0, succeeded 2319, skipped 2 (no display). `BrutalUi.Core.Tests` gives 1489. The 9 new tests are the only change from group A.
- `ButtonTextTests` 9 of 9 pass, with the demo `AButtonDrawsItsText`: `rect 0,0,48,32` and `Text 8,8 font 0 size 16 #FFFFFFFF wrap 32 "Play"`.
- The `Label` gate, with no test edit, each with 0 failed: `LabelTests` 22, `LabelTextTests` 35, `LabelWrapTests` 22, `ContentBoxTests` 16, `PaintContentTests` 9, `ElementAllocationTests` 18.
- The classes of phases 1 to 3, with no edit, each with 0 failed: `ReleaseOrderTests` 4, `ButtonClickTests` 8, `ButtonKeyTests` 9, `ToggleTests` 10.
- `grep -rn 'readonly Utf8Text' src/` gives no line. `grep -n 'static readonly UiEventHandler' Button.cs` gives two lines. `s_measure` is a separate `static readonly MeasureCallback` field. `Button.cs` holds no lambda.
- `git diff main --stat -- tests/` gives no file. The public lines of `Label.cs` that differ from `main` are the bodies of `Text` and `LocalizedText` only (group A).
- `UiContext.cs`: this group changes `RefreshSubtree` (an `else if (element is Button button)` branch) and the doc comments of `RefreshLocalizedText` and `RefreshSubtree` only (I3).
- Blast radius: `Button.cs`, `UiContext.RefreshSubtree`, and the new test file. Each existing `Button` comes from `WidgetTree`, with a measurer and a fixed size of 120 x 40. Thus the new measure callback changes no existing test. No other project creates a `Button`.
- Decision: the constructor sets `Layout.Measure` before the phase 1 lines (`IsFocusable` and the two slots), in the order of `Label`. The order has no effect.
- Decision: `RefreshSubtree` uses `else if` for `Button`. One element is never both types, so the order has no effect. The walk goes on into the children of a button, so a `Label` inside a `Button` also refreshes. `RefreshLocalizedTextReachesAButton` checks it.
- Decision: the class remarks of `Button` state one more fact that the plan does not name. A button with an empty text and no children also needs the measurer. The engine skips the callback only when the layout fixes both the width and the height (`MeasureNodeWithMeasureFunc`). `AButtonNeedsAMeasurer` checks the throw for an empty text and the pass for a fixed size in a context with no measurer.
- Decision: the remarks state that a button with a child still draws a text that is not empty, under its children. `TheButtonTakesTheSizeOfItsText` checks that a button with a child takes the size of the child and calls no measure.
- Surprise (OQ2): `StylePass` marks `DirtyFlags.Layout` on a font or font size change for a `Label` only (`StylePass.cs`, `sizeTextChanged && element is Label`). A `Button` with an auto size keeps its old size after such a change, for example from a `Button:hover` rule with a new font size. The brief limits this group to `Button.cs` and `UiContext.cs`, so this group does not change `StylePass.cs`. The `Button` remarks do not claim the rule of `Label` for a font change.

### Verifier (brief `phase-4-verify.md`)

- PASS. Demo: PASS. The solution gives 2321 tests (Core 1489), 0 failed, 2 skipped. Each `Label` class and the ImGui label classes (`ImGuiLabelTests` 7, `ImGuiRendererAllocationTests` 4) pass with no edit. No existing test file has a diff.
- One grep of step 7 of the Verification Method finds a doc comment line in `Button.cs` with the word `required` ("The create-info has no required member"). The phase 5 brief rewords it, so the literal grep gives no line.

## Phase 5 Results

- Tasks 5.1 to 5.3 are done. `WidgetInput.IsClick` and `WidgetInput.IsKeyActivation` return `false` for a widget that holds `PseudoStates.Disabled` (Decision 6). `Button.cs` and `Toggle.cs` hold no `Disabled` check of their own. Only their doc comments changed.
- The remarks of `Button` and `Toggle` state that a disabled widget raises nothing and does not stop the walk. The remarks of `Toggle` state that a code set of `Checked` still works (Q4).
- The doc comment of `Button.CreateInfo` now reads "Each create-info member has a safe default". `grep -n 'required' src/BrutalUi.Core/Elements/Button.cs src/BrutalUi.Core/Elements/Toggle.cs` gives no line.
- Build: `0 Warning(s)`, `0 Error(s)`.
- Full test run: total 2328, failed 0, succeeded 2326, skipped 2 (the two GLFW tests skip, because the machine has no display). The count of Core was 1489 after phase 4, so Core now holds 1496 (1489 plus 7 new tests).
- Classes: `WidgetDisabledTests` 7, `ReleaseOrderTests` 4, `ButtonClickTests` 8, `ButtonKeyTests` 9, `ToggleTests` 10, `ButtonTextTests` 9, `ClickFocusTests` 8, `FocusStateEdgeTests` 8. Each has `failed: 0`.
- The 7 new tests also pass the rule of the keyboard-focus drain: the key tests push the key with the widget as the caller target. A press on a disabled button clears the focus of another owner and gives no focus to the button.
- Verifier (brief `phase-5-verify.md`): PASS. Demo: PASS. The solution gives 2328 tests, 0 failed, 2 skipped. The disabled check lives in `WidgetInput` only. A `false` check returns before `StopPropagation` in each of the four handlers. `grep -n 'required'` on `Button.cs` and `Toggle.cs` gives no line.

---

## Phase 6 Results

- Task 6.1: `tests/BrutalUi.Core.Tests/Elements/WidgetAllocationTests.cs` has 4 tests, and all pass at the first run. The scene: a column root with 20 buttons (text "Open it", padding 4, auto size) and 20 toggles (20 x 20), a sheet with `Button`, `Button:hover`, `Button:active`, `Toggle`, `Toggle:checked`, and `:disabled` rules, and the last button and toggle in `Disabled`. The sender delegates (`Counter.OnClicked`, `Counter.OnChanged`) are created once in the scene. One input step pushes 6 events (press, release, Enter over the button, and press, release, Space over the toggle) and gives 6 walks, 2 clicks, and 2 flips. `TheMeasuredBodyDoesRealWork` also checks 20 `Text` calls in the frame, and the measure of all 20 buttons after a text step. The warm-up is 2 rounds, as in `FocusAllocationTests`.
- Task 6.2 (the plant): `Button.cs` is untracked, so the check uses a copy and `cmp`, not `git diff`. The copy lives at `/tmp/claude-1000/-home-aaronstatic-dev-brutal-ui/71406a31-5c0c-4ee7-bb2b-5b51e0230847/scratchpad/Button.cs.orig`. With `GC.KeepAlive(new object());` as the first line of `Button.HandlePointerUp`, `dotnet test --solution brutal-ui.sln` gave `exit=2` and failed one gate: `WidgetAllocationTests.TheWidgetInputPathAllocatesNothing`. After the restore, `cmp` gave `cmp=0` (no difference). Note: a first plant run overlapped with the restore edit (the two tool calls ran in parallel), and it also gave `exit=2`. I ran the plant a second time in sequence, and the result above is from that run.
- Task 6.3 and 6.4: `docs/technical-design.md` now has the "Widgets" paragraph in section 5 (the ten rules, the click rule, the key rule, the `Checked` rule, the disabled rule, and the sender delegates), the `Button` note in "The text of a label", `Label` and `Button` in "The locale refresh", the release order in "The pointer step of the drain" (the first sentence and item 3), the release order in section 9 item 4, and two bullets in section 14 (the widget tests and the ninth allocation gate with the plant). `CHANGELOG.md` has the `Button` and `Toggle` entry under "Added", and the `PointerUp` and `Utf8Text` entries under "Changed". The docs do not say that a font change measures a `Button` again (OQ2 stays open).
- Task 6.5: `ste-lint.py --fail-over 2.5` gave 1.51 for `docs/technical-design.md`, 1.67 for `CHANGELOG.md`, and 1.58 for `implementation.md`. No score is above 2.5.
- Task 6.6: build `0 Warning(s)`, `0 Error(s)`. Full test run: total 2332, failed 0, succeeded 2330, skipped 2 (the baseline of task 1.1 was 2281, so the solution grew by 51). Core: total 1500, failed 0 (the baseline was 1449, so Core grew by 51: 4 + 8 + 9 + 10 + 9 + 7 + 4). `scripts/test-linux.sh` gave `test-linux: PASS build, test, gpu, smoke (120 frames) in 13 s` and `exit=0`.
- Verifier (brief `phase-6-verify.md`): PASS. Demo: PASS. The solution gives 2332 tests (Core 1500), 0 failed, 2 skipped. Each grep of the Verification Method gives the expected result. The lint gives 1.51 for `technical-design.md` and 1.67 for `CHANGELOG.md`.
- The verifier saw one flaky failure in its first full run: `TransitionCurveTests.TheCurvesAllocateNothing` ("ease-out allocated 8056 bytes", `tests/BrutalUi.Core.Tests/Style/TransitionCurveTests.cs:267`). The class is not part of this feature. It passed alone (31 of 31), and three more full runs gave 0 failed.
- The verifier notes that `TextStep` of `WidgetAllocationTests` holds a local of type `string`. It picks a string that the scene built, so it allocates nothing. The tier note asks for no `string` in a measured body.

---

## Open Questions

- The plan uses the recommended option of each open scope question (Notes Q1 to Q4 of the plan).
- **OQ1 (phase 1, group B): a click from a button chord.** `PressButton` sets `Active` on the first press only, and `IsClick` reads `Active` and the button of the release. Case 1: a right press on the button, a left press on another element, and a left release over the button give a click. The left press was not on the button. Case 2: a left press on the button, a right press, and a left release over the button give a click while the right button stays down. The plan fixes the rule of `IsClick` for phase 1, so this group changes nothing. A fix can store the button of the press that set `Active` in `UiContext` and let `IsClick` compare it. That fix changes the public state of the context, so a person must decide it. No test covers the two cases now.
- **OQ2 (phase 4, group B): a font change does not measure a `Button` again.** `StylePass.cs` (about line 224) adds `DirtyFlags.Layout` for a font or font size change only when `element is Label`. A `Button` gets `Paint` only, so the layout keeps the old text size and the draw uses the new size. The first style pass runs before the first layout, so the start size is correct. A later change is wrong: an inline change, a rule change, or a pseudo-state rule such as `Button:hover { font-size }`. The proposed fix is one line: `element is Label or Button`. The doc of `StyleProperties.cs` (line 20) names `Label` only and needs the same edit. A test can follow `LabelTextTests.ANewFontSizeMeasuresTheLabelAgain`. The fix is outside the files of this brief, so a person or a later brief must decide it.
- **Follow-up (phase 4, group B):** the summary of `Utf8Text` says "`Label` holds one". It can name `Button` too. `Utf8Text.cs` is outside the files of this brief.
- **Status after the review:** OQ1 is CR-1 of `code-review.md`. It stays open in the code: a person decides the chord rule (an open scope question of the `/autorun-epic` report). The docs state the chord limit. OQ2 is CR-2, and it is fixed. The `Utf8Text` follow-up is CR-9, and it is fixed.
- **Follow-up question of the review (no finding):** `Disabled` blocks a widget only when the widget holds the bit. A button inside a disabled panel still clicks. Keyboard-focus uses the same rule (`AcceptsFocus` reads the own bit). A person decides whether a later feature makes `:disabled` on an ancestor block the widgets.

---

## Review Fixes

- The cross-phase review (`code-review.md`) ran 50 mutation probes and one class of 6 behaviour probes. It found 13 findings: 1 HIGH, 2 MEDIUM, and 10 LOW.
- 12 findings are fixed, in 11 fix briefs (`review-g1.md` to `review-g11.md`). CR-2 changes code: `StylePass` measures a `Button` again after a font or font size change (`element is Label or Button`). CR-3, CR-4, and CR-5 extend four tests with no new test method, so the class totals of the plan stay. The other fixes are doc comments, section 5 of the technical design, and the changelog.
- CR-1 (HIGH, a click from a pointer chord) is deferred to a person. Its fix is the rejected alternative (b) of Decision 2, and it changes the click rule of the widget pattern. The docs of `Button`, `Toggle`, `WidgetInput.IsClick`, and section 5 state the chord limit.
- The first agent of `review-g11.md` stopped when the session ended, before its edits applied. A second agent did the group.
- Verifier (brief `review-verify.md`): PASS. Demo: none. The solution gives 2332 tests (Core 1500), 0 failed, 2 skipped. `scripts/test-linux.sh` gives its PASS line and `exit=0`. The lint gives 1.52 for `technical-design.md` and 1.72 for `CHANGELOG.md`.

---

## Session Notes

### 2026-10-26 04:41
- `/autorun-feature` started the feature in the called mode of `/autorun-epic`. It created `tasks.md` (34 tasks in 6 phases), `context.md`, and `inbox.md`.

### 2026-10-26 (phase 1, group A)
- Tasks 1.1 to 1.5 are done. `ReleaseButton` clears the button bit only and marks the release as pending. `FinishRelease` clears `Active` after the walk step of each entry and in the `finally` block of `DrainEvents`.
- The doc comments of `DrainEvents`, `RoutePointer`, `ReleaseButton`, `ActiveElement`, and `_activeElement` name Decision 9 of pointer-routing and Decision 2 of this plan.
- No surprise. The pointer and focus classes pass with no edit. Group B (tasks 1.6 to 1.10) can start.

### 2026-10-26 (phase 1, group B)
- Tasks 1.6 to 1.10 are done. `WidgetInput.IsClick` holds the click rule. `Button` follows the widget pattern: a cached static delegate in `OnPointerUp`, an `is` cast of the current target, the stop, and then `Clicked` with the sender.
- `WidgetTree` holds the root, the button, `Other`, and the three points. `ButtonClickTests` drives the button through `PushEvent` with no caller target.
- One surprise: a stop also skips the later delegates of the same slot (B6). The remarks of `Button` state it.
- One open question: a click from a button chord (OQ1). Phase 1 keeps the rule of the plan.
- Phase 1 is complete. Phase 2 can start.

### 2026-10-26 (phase 2)
- Tasks 2.1 to 2.4 are done. One test line had a wrong count (`capturesAtClick` is 2 after two keys). The test was fixed, not the code.

### 2026-10-26 (phase 3)
- Tasks 3.1 to 3.4 are done. `Toggle` follows the widget pattern, with Space only (A3). `ToggleTests` has 10 tests, and all pass. Phase 4 can start.

### 2026-10-26 (phase 4, group A)
- Tasks 4.1 to 4.3 are done. `Utf8Text` holds the text fields, the encode and compare code, the measure rule, and the draw rule of `Label`. `Label` holds `private Utf8Text _text;` and marks `Layout | Paint` on each `true` result.
- Each `Label` test and each ImGui label test passes with no edit. The full run gives 2312 tests and 0 failed.
- No surprise. Group B (tasks 4.4 to 4.7) can start.

### 2026-10-26 (phase 4, group B)
- Tasks 4.4 to 4.7 are done. `Button` holds `private Utf8Text _text;`, the `Text` and `LocalizedText` properties, the three `SetText` overloads, `RefreshLocalized`, a cached static measure callback, and `PaintContent`. `RefreshSubtree` refreshes a `Button`.
- `ButtonTextTests` has 9 tests, and all pass. The full run gives 2321 tests and 0 failed. Each `Label` class and each class of phases 1 to 3 passes with no edit.
- One surprise: the style pass measures a `Label` again after a font change, but not a `Button` (OQ2). This group does not fix it, because the fix is outside the files of the brief.
- Phase 4 is complete. Phase 5 can start.

### 2026-10-26 (phase 5)
- Tasks 5.1 to 5.3 are done. The disabled check lives in `WidgetInput` only. `WidgetDisabledTests` has 7 tests, and all pass. The full run gives 2328 tests and 0 failed. Phase 6 can start.
- No surprise. A disable between the press and the release gives no click, and the release still clears `Active` and `ActiveElement`.

### 2026-10-26 (phase 6)
- Tasks 6.1 to 6.6 are done. The 4 gate tests pass, the plant failed the input-path gate with `exit=2`, and the restore passed `cmp`. The docs lint below 2.5. The solution gives 2332 tests with 0 failed, and `scripts/test-linux.sh` passes. The feature has no open task. OQ1 and OQ2 stay open for the caller.
- Surprise: two tool calls that ran in parallel (the planted test run and the restore edit) overlapped. I ran the plant again in sequence. Do not run a plant and its restore in one parallel block.

### 2026-10-26 06:28 (the cross-phase review and the feature end)
- The cross-phase review found 13 findings (1 HIGH, 2 MEDIUM, 10 LOW) with 50 mutation probes. 11 fix briefs fixed 12 findings. The verifier of the fixes gave PASS: 2332 tests, 0 failed, 2 skipped, and `scripts/test-linux.sh` PASS. See "Review Fixes".
- Decision: CR-2 changes `StylePass.cs` and `StyleProperties.cs`, two files outside the file tree of the plan. A font change rule of `Label` is a text rule, and the requirement gives `Button` the text rules of `Label`. Neither file is on the no-diff list (I2).
- Decision: the review fixes extend four tests and add no test method, so the class totals of the Definition of Done stay (F1 to F8, Q2).
- Decision: CR-1 stays open in the code. Its fix is the rejected alternative (b) of Decision 2. The called mode edits no plan decision, so `/autorun-epic` asks a person. The docs state the chord limit.
- Gotcha: the session ended while the first agent of `review-g11.md` ran, and its edits did not apply. A second agent did the group and checked the file first.
- The draft plan of `interactive-elements/slider` exists (7 phases, 35 tasks). It adds an opt-in drag capture in `RoutePointer` (its phase 4, ADVANCED).
- The inbox holds no line. No bug goes to `docs/bugs/`.

---

*Update this file at the end of each work session. Run `/update-feature` before compacting conversations.*
