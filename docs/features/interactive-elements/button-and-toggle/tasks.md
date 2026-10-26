# interactive-elements/button-and-toggle - Task Checklist

**Last Updated:** 2026-10-26 06:28
**Progress:** 34/34 tasks complete (100%)
**Advanced phases:** Phases 1 and 4 (ADVANCED, opus). Phases 2, 3, 5, and 6 are STANDARD (sonnet).

Source paths: `Core/` means `src/BrutalUi.Core/`. `Tests/` means `tests/BrutalUi.Core.Tests/`. "The plan" means `implementation.md` of this feature. Its sections "Architecture Overview", "Internal Types", "The Widget Pattern", and "Key Technical Decisions" hold the rules that the tasks name. Each phase ends with `dotnet build brutal-ui.sln 2>&1 | tail -n 30` and `dotnet test --solution brutal-ui.sln 2>&1 | tail -n 30` (tool rule 5). Expect `0 Warning(s)`, `0 Error(s)`, and 0 failed. The projects have no implicit usings, so each new file holds its own `using` lines. Each public member needs an XML doc comment (CS1591 is an error). Use `namespace BrutalUi.Core;` in Core and `namespace BrutalUi.Core.Tests;` in the tests. Prose in comments follows ASD-STE100. Each test class interns its classes with a unique name prefix, because xUnit runs the classes in parallel. Run one class with `dotnet run --project tests/BrutalUi.Core.Tests/BrutalUi.Core.Tests.csproj -- --filter-class '*<Class>' 2>&1 | tail -n 5`. Each phase runs the pointer and focus classes (`PointerPressTests`, `PointerStateEdgeTests`, `PointerAllocationTests`, `ClickFocusTests`, `FocusOwnerTests`, `TabNavigationTests`, `FocusStateEdgeTests`, `FocusAllocationTests`, `RoutingGateTests`) with no edit.

---

## Phase 1: A Pointer Click on a Button (10/10 complete)

**Demo:** a person runs `scripts/test-linux.sh` and reads its PASS line. In that run, `ButtonClickTests.APressAndAReleaseClickTheButtonOnce` pushes a press and a release over a `Button` through `UiContext.PushEvent`, runs `Update`, and counts one `Clicked` call with the button as the sender.
**Needs:** none
**Agent tier:** ADVANCED (opus)
**Tier note:** Group A changes `DrainEvents` and `RoutePointer`, the central methods of input-routing. Move only the clear of `Active`. Do not change the hit test, `Hover`, the button mask, the click focus, the Tab step, the drop count, or `EventWalk.cs`. Run each pointer and focus class with no edit before group B starts.

### Group A: the release order

- [x] ✅ **1.1 Record the baseline counts** - Completed 2026-10-26
  - Description: Record the test counts of `BrutalUi.Core.Tests` and of each pointer and focus class in `docs/features/interactive-elements/button-and-toggle/context.md` before any change. Record the Active order finding of the Architecture Overview.
  - File(s): `docs/features/interactive-elements/button-and-toggle/context.md`
  - Estimate: 4 steps

- [x] ✅ **1.2 Move the clear of Active after the release walk** - Completed 2026-10-26
  - Description: Edit `src/BrutalUi.Core/Tree/UiContext.cs`: add `private bool _releasePending;`. Change `ReleaseButton` so it clears the mask bit only, and sets `_releasePending = true` when the mask is 0 and `_activeElement` is not null. Add `private void FinishRelease()`: when `_releasePending` is `true`, clear it, clear `PseudoStates.Active` on `_activeElement`, and set `_activeElement` to null. Call `FinishRelease()` in `DrainEvents` right after the walk step of each entry, before the Tab step, and one more time in the `finally` block. A release over nothing gets no walk, and the call after the walk step still clears `Active`.
  - File(s): `src/BrutalUi.Core/Tree/UiContext.cs`
  - Estimate: 7 steps

- [x] ✅ **1.3 Edit the doc comments of the release order** - Completed 2026-10-26
  - Description: Edit the doc comments in `UiContext.cs`: the Active rule paragraph of `DrainEvents` (the release clears `Active` after its walk, so a `PointerUp` handler reads `Active` on the pressed element), the summary and remarks of `RoutePointer`, the summary of `ReleaseButton`, and the remarks of `ActiveElement`. Name Decision 9 of pointer-routing and Decision 2 of this plan.
  - File(s): `src/BrutalUi.Core/Tree/UiContext.cs`
  - Estimate: 5 steps

- [x] ✅ **1.4 Write ReleaseOrderTests.cs** - Completed 2026-10-26
  - Description: Write `tests/BrutalUi.Core.Tests/Input/ReleaseOrderTests.cs` with 4 tests on `PointerTree`: `AReleaseHandlerOfThePressedElementReadsActive`, `TheDrainClearsActiveAfterTheReleaseWalk` (no `Active` and a null `ActiveElement` after `DrainEvents`), `AReleaseOverAnotherElementSeesActiveOnThePressedElement` (a handler on `Left` reads `Active` on `Right` and not on `Left`), and `AThrowingReleaseHandlerStillClearsActive` (`DrainEvents` throws, then no element holds `Active`, and the next drain runs).
  - File(s): `tests/BrutalUi.Core.Tests/Input/ReleaseOrderTests.cs`
  - Estimate: 6 steps

- [x] ✅ **1.5 Run the group A gate** - Completed 2026-10-26
  - Description: Run the build and the tests. Run `ReleaseOrderTests` and each pointer and focus class with no edit. Record the results in `context.md`.
  - File(s): `docs/features/interactive-elements/button-and-toggle/context.md`
  - Estimate: 4 steps

### Group B: the button

- [x] ✅ **1.6 Write WidgetInput.cs** - Completed 2026-10-26
  - Description: Write `src/BrutalUi.Core/Elements/WidgetInput.cs`: the internal static class with `IsClick(Element widget, in PointerEvent e)` (left button and `Active` on the widget). The class summary states the role of the helper for each widget of the epic.
  - File(s): `src/BrutalUi.Core/Elements/WidgetInput.cs`
  - Estimate: 4 steps

- [x] ✅ **1.7 Write Button.cs** - Completed 2026-10-26
  - Description: Write `src/BrutalUi.Core/Elements/Button.cs` by the widget pattern: sealed, the type id of `Button`, `public Button(in CreateInfo info)`, an empty `public struct CreateInfo` (phase 4 adds the members), `IsFocusable = true`, `OnPointerUp = s_pointerUp`, `public Action<Button>? Clicked { get; set; }`, and the static `HandlePointerUp`. The remarks state: the click rule, the stop (A4), the sender (Q3), "read `Clicked`, not the slot", "a write of `=` to `OnPointerUp` removes the click", and "a pickable child of the button gets `Active` in place of the button, so set `IsPickable = false` on a child".
  - File(s): `src/BrutalUi.Core/Elements/Button.cs`
  - Estimate: 6 steps

- [x] ✅ **1.8 Write WidgetTree.cs** - Completed 2026-10-26
  - Description: Write `tests/BrutalUi.Core.Tests/Elements/WidgetTree.cs`: an internal test tree. The context has a `RecordingRenderer` measurer and a viewport of 800 x 600. The root is 400 x 300 in a row. The children are `Button` (120 x 40 at 0, 0) and `Other`, a plain `Element` (100 x 100 at 120, 0). It holds the points `OverButton`, `OverOther`, and `OverNothing`, and a `Settle()` method. Use the factories of `PointerTree` for the events. A later phase adds a child at the end of the row only, so these rects stay.
  - File(s): `tests/BrutalUi.Core.Tests/Elements/WidgetTree.cs`
  - Estimate: 5 steps

- [x] ✅ **1.9 Write ButtonClickTests.cs** - Completed 2026-10-26
  - Description: Write `tests/BrutalUi.Core.Tests/Elements/ButtonClickTests.cs` with 8 tests: `APressAndAReleaseClickTheButtonOnce` (the demo, through `PushEvent` and `Update`), `AReleaseOverAnotherElementDoesNotClick`, `AReleaseAfterAPressElsewhereDoesNotClick`, `ARightButtonDoesNotClick` (A1), `AClickStopsTheReleaseWalk` (the root capture handler runs, the root bubble handler does not), `AButtonWithNoHandlerStopsTheWalkAndDoesNotThrow`, `APressFocusesTheButton` (A5, `IsFocusable` is `true`, and `FocusedElement` is the button), and `ATypeRuleMatchesTheButton` (A6, a rule with `Type = ElementType.Intern("Button")` sets a fill on the button and not on `Other`).
  - File(s): `tests/BrutalUi.Core.Tests/Elements/ButtonClickTests.cs`
  - Estimate: 8 steps

- [x] ✅ **1.10 Run the phase 1 gate and the demo** - Completed 2026-10-26
  - Description: Run the build, the tests, the demo class, and `scripts/test-linux.sh; echo "exit=$?"`. Expect the PASS line and `exit=0`. Record the counts and each decision that this plan did not settle in `context.md`.
  - File(s): `docs/features/interactive-elements/button-and-toggle/context.md`
  - Estimate: 5 steps

---

## Phase 2: Enter and Space on a Focused Button (4/4 complete)

**Demo:** a unit test calls `SetFocus(button)`, pushes an Enter key down with no target, and runs `Update`. It counts one `Clicked` call. The developer runs `dotnet run --project tests/BrutalUi.Core.Tests/BrutalUi.Core.Tests.csproj -- --filter-class '*ButtonKeyTests'` and reads a total of 9 with `failed: 0`.
**Needs:** Phase 1
**Agent tier:** STANDARD (sonnet)

- [x] ✅ **2.1 Add the key check to WidgetInput** - Completed 2026-10-26
  - Description: Edit `src/BrutalUi.Core/Elements/WidgetInput.cs`: add `SpaceKey`, `EnterKey`, the `ActivationKeys` flags, and `IsKeyActivation(Element widget, in KeyEvent e, ActivationKeys keys)`. The check needs the widget as `e.Header.Target` (Decision 5), no repeat (Q2), and no `Control`, `Alt`, or `Super` (Decision 5). `Shift` does not block it.
  - File(s): `src/BrutalUi.Core/Elements/WidgetInput.cs`
  - Estimate: 5 steps

- [x] ✅ **2.2 Add the key handler to Button** - Completed 2026-10-26
  - Description: Edit `src/BrutalUi.Core/Elements/Button.cs`: add `s_keyDown`, write it into `OnKeyDown` in the constructor, and add the static `HandleKeyDown` with `ActivationKeys.Enter | ActivationKeys.Space`. Extract one private `Click()` step that the two handlers share: it calls `Clicked`. Extend the remarks with the key rule.
  - File(s): `src/BrutalUi.Core/Elements/Button.cs`
  - Estimate: 5 steps

- [x] ✅ **2.3 Write ButtonKeyTests.cs** - Completed 2026-10-26
  - Description: Write `tests/BrutalUi.Core.Tests/Elements/ButtonKeyTests.cs` with 9 tests on `WidgetTree`: `AFocusedButtonClicksOnEnter` (the demo), `AFocusedButtonClicksOnSpace`, `ARepeatDoesNotClick`, `AKeyUpDoesNotClick`, `AnotherKeyDoesNotClickAndWalksOn` (the key A reaches the root bubble handler), `AModifierStopsTheActivation` (`Control`+Enter, `Alt`+Enter, and `Super`+Space give no click. `Shift`+Enter clicks), `AKeyActivationStopsTheWalk`, `TabThenEnterClicks` (a Tab with no owner focuses the button, and the next Enter clicks), and `AKeyFromAFocusableChildDoesNotClick` (a focusable child of the button owns the focus, and Enter bubbles to the button with no click).
  - File(s): `tests/BrutalUi.Core.Tests/Elements/ButtonKeyTests.cs`
  - Estimate: 8 steps

- [x] ✅ **2.4 Run the phase 2 gate** - Completed 2026-10-26
  - Description: Run the build, the tests, the demo class, and the classes of phase 1. Record the results in `context.md`.
  - File(s): `docs/features/interactive-elements/button-and-toggle/context.md`
  - Estimate: 4 steps

---

## Phase 3: Toggle (4/4 complete)

**Demo:** a unit test pushes a press and a release over a `Toggle` and runs `Update`. It reads `Checked` as `true`, `PseudoStates.Checked` in `States`, the fill of a `Toggle:checked` rule, and one `CheckedChanged` call that reads the new value. The developer runs `dotnet run --project tests/BrutalUi.Core.Tests/BrutalUi.Core.Tests.csproj -- --filter-class '*ToggleTests'` and reads a total of 10 with `failed: 0`.
**Needs:** Phase 2
**Agent tier:** STANDARD (sonnet)

- [x] ✅ **3.1 Write Toggle.cs** - Completed 2026-10-26
  - Description: Write `src/BrutalUi.Core/Elements/Toggle.cs` by the widget pattern: sealed, the type id of `Toggle`, `public Toggle(in CreateInfo info)`, `public struct CreateInfo { public bool Checked; }`, `IsFocusable = true`, `OnPointerUp` and `OnKeyDown` from cached static delegates, `public bool Checked` (Decision 4), and `public Action<Toggle>? CheckedChanged { get; set; }`. The key handler passes `ActivationKeys.Space` only (A3). A private `Flip()` step sets `Checked = !Checked` and then calls `CheckedChanged`. The constructor writes the start value through `States` and calls nothing. The remarks state: no text (Q5), the size comes from the style (Decision 9), and a code set calls nothing (Q4).
  - File(s): `src/BrutalUi.Core/Elements/Toggle.cs`
  - Estimate: 7 steps

- [x] ✅ **3.2 Add the toggle to WidgetTree** - Completed 2026-10-26
  - Description: Edit `tests/BrutalUi.Core.Tests/Elements/WidgetTree.cs` with additions only: a `Toggle` child (40 x 40) at the end of the row, at (220, 0), and the point `OverToggle`.
  - File(s): `tests/BrutalUi.Core.Tests/Elements/WidgetTree.cs`
  - Estimate: 3 steps

- [x] ✅ **3.3 Write ToggleTests.cs** - Completed 2026-10-26
  - Description: Write `tests/BrutalUi.Core.Tests/Elements/ToggleTests.cs` with 10 tests: `AClickFlipsTheToggle` (the demo), `ASecondClickClearsChecked`, `SpaceFlipsAFocusedToggle`, `EnterDoesNothingOnAFocusedToggle` (A3, and the root bubble handler gets the key), `ARepeatSpaceDoesNothing`, `ACodeSetCallsNoCheckedChanged` (Q4, the bit changes and the delegate count stays 0), `ASetToTheSameValueMarksNothing` (no `DirtyFlags.Style` after a settle and a set to the same value), `TheCreateInfoSetsTheStartValue` (A7, no call), `AReleaseElsewhereOrARightButtonDoesNotFlip` (A1), and `CheckedReadsTheStateBit` (a clear of the bit through `States` reads back as `false`. The type id is `Toggle`, and `IsFocusable` is `true`).
  - File(s): `tests/BrutalUi.Core.Tests/Elements/ToggleTests.cs`
  - Estimate: 9 steps

- [x] ✅ **3.4 Run the phase 3 gate** - Completed 2026-10-26
  - Description: Run the build, the tests, the demo class, and the classes of phases 1 and 2. Record the results in `context.md`.
  - File(s): `docs/features/interactive-elements/button-and-toggle/context.md`
  - Estimate: 4 steps

---

## Phase 4: Button Text (7/7 complete)

**Demo:** a unit test creates a `Button` with `Text = "Play"` and a padding of 8, runs `Update` and `Draw` with a `RecordingRenderer`, and reads one `Text` call with the bytes of `Play` at the top-left corner of the content box. The button size is the measured text size plus the padding. The developer runs `dotnet run --project tests/BrutalUi.Core.Tests/BrutalUi.Core.Tests.csproj -- --filter-class '*ButtonTextTests'` and reads a total of 9 with `failed: 0`.
**Needs:** Phase 3
**Agent tier:** ADVANCED (opus)
**Tier note:** Group A is a pure refactor of `Label`. It moves code. It changes no rule, no doc rule, and no public signature. Keep `Label.DrawWrapWidth`, `Label.SnapWrapWidth`, and `Label.Utf8Equals` as internal forwarders, because `ContentBoxTests`, `LabelWrapTests`, and `LabelTextTests` call them. Keep the exception message of `MeasureLabel` in `Label`, because `LabelTests.ThrowsWithNoMeasurer` reads it. Hold `Utf8Text` in a field with no `readonly` modifier. Run each `Label` test with no edit before group B starts.

### Group A: the shared text type

- [x] ✅ **4.1 Write Utf8Text.cs** - Completed 2026-10-26
  - Description: Write `src/BrutalUi.Core/Elements/Utf8Text.cs`: the internal struct of the Internal Types section. Move from `Label` the five text fields, `InitialTextCapacity`, `CompareChunkLength`, `WrapSnapTolerance`, `EnsureCapacity`, `EncodeText`, `ApplyText`, `ClearLocalized`, `Utf8Equals`, `SnapWrapWidth`, `DrawWrapWidth`, `FiniteSize`, `FiniteLength`, the measure rule of `MeasureLabel` (after the measurer check), and the body of `PaintContent`. Move each doc comment with its code. Each `Set` method and `Refresh` return `true` for a change and set no dirty bit.
  - File(s): `src/BrutalUi.Core/Elements/Utf8Text.cs`
  - Estimate: 8 steps

- [x] ✅ **4.2 Make Label use Utf8Text** - Completed 2026-10-26
  - Description: Edit `src/BrutalUi.Core/Elements/Label.cs`: hold `private Utf8Text _text;`. The constructor checks the "both set" rule, then creates the struct. `Text`, `LocalizedText`, the three `SetText` overloads, and `RefreshLocalized` call the struct and mark `DirtyFlags.Layout | DirtyFlags.Paint` on `true`. `MeasureLabel` keeps its measurer check and its message, then calls `Utf8Text.Measure`. `PaintContent` calls `Utf8Text.Paint`. Keep the three internal static forwarders. Keep the class remarks and each public doc comment.
  - File(s): `src/BrutalUi.Core/Elements/Label.cs`
  - Estimate: 7 steps

- [x] ✅ **4.3 Run the Label regression gate** - Completed 2026-10-26
  - Description: Run the build and the tests. Run `LabelTests`, `LabelTextTests`, `LabelWrapTests`, `ContentBoxTests`, `PaintContentTests`, `ElementAllocationTests`, and the ImGui label tests with no edit. Run `git diff main --stat -- tests/` and expect no changed file. Record the counts in `context.md`.
  - File(s): `docs/features/interactive-elements/button-and-toggle/context.md`
  - Estimate: 4 steps

### Group B: the button text

- [x] ✅ **4.4 Add the text to Button** - Completed 2026-10-26
  - Description: Edit `src/BrutalUi.Core/Elements/Button.cs`: add `Text` and `LocalizedText` to `CreateInfo` (A7). The constructor throws `ArgumentException` when both are set, with the message of `Label` for `Button.CreateInfo`. Add `private Utf8Text _text;`, the `Text` and `LocalizedText` properties, the three `SetText` overloads, and `internal void RefreshLocalized()`, each with the doc comments of `Label`. Set `Layout.Measure = s_measure` with a static `MeasureButton` that throws `InvalidOperationException` with the message "A Button needs a UiContext with a measurer. Create the context with UiContext.CreateInfo." when no measurer exists. Override `PaintContent` with `Utf8Text.Paint`. The remarks state: the text draws at the top-left corner of the content box (Decision 8), a button with a child does not measure its text, and the style gives the color, the font, and the size.
  - File(s): `src/BrutalUi.Core/Elements/Button.cs`
  - Estimate: 8 steps

- [x] ✅ **4.5 Make the locale refresh reach a Button** - Completed 2026-10-26
  - Description: Edit `src/BrutalUi.Core/Tree/UiContext.cs`: `RefreshSubtree` also calls `RefreshLocalized` on a `Button` (Decision 7). Update the doc comments of `RefreshLocalizedText` and `RefreshSubtree`.
  - File(s): `src/BrutalUi.Core/Tree/UiContext.cs`
  - Estimate: 3 steps

- [x] ✅ **4.6 Write ButtonTextTests.cs** - Completed 2026-10-26
  - Description: Write `tests/BrutalUi.Core.Tests/Elements/ButtonTextTests.cs` with 9 tests: `AButtonDrawsItsText` (the demo), `TheButtonTakesTheSizeOfItsText`, `SetTextFromASpanMarksLayoutAndPaint`, `SetTextFromTheSameTextMarksNothing` (the span, the string, and a string after a span), `SetTextFromAnLStringShowsTheLocalizedText`, `RefreshLocalizedTextReachesAButton`, `ThrowsForTwoTexts`, `AButtonNeedsAMeasurer` (the message of task 4.4), and `AClickOnTheTextClicksTheButton`.
  - File(s): `tests/BrutalUi.Core.Tests/Elements/ButtonTextTests.cs`
  - Estimate: 8 steps

- [x] ✅ **4.7 Run the phase 4 gate** - Completed 2026-10-26
  - Description: Run the build, the tests, the demo class, the `Label` classes of task 4.3, and the classes of phases 1 to 3. Record the results in `context.md`.
  - File(s): `docs/features/interactive-elements/button-and-toggle/context.md`
  - Estimate: 4 steps

---

## Phase 5: Disabled Widgets (3/3 complete)

**Demo:** a unit test sets `Disabled` on the `Button`, pushes a press and a release over it, and runs `Update`. It counts 0 `Clicked` calls, and the root bubble handler gets the release. The developer runs `dotnet run --project tests/BrutalUi.Core.Tests/BrutalUi.Core.Tests.csproj -- --filter-class '*WidgetDisabledTests'` and reads a total of 7 with `failed: 0`.
**Needs:** Phase 4
**Agent tier:** STANDARD (sonnet)

- [x] ✅ **5.1 Add the disabled rule to WidgetInput** - Completed 2026-10-26
  - Description: Edit `src/BrutalUi.Core/Elements/WidgetInput.cs`: `IsClick` and `IsKeyActivation` return `false` when `widget.States` holds `Disabled` (Decision 6). Edit the remarks of `Button` and `Toggle`: a disabled widget raises nothing and does not stop the walk. A code set of `Checked` still works on a disabled toggle.
  - File(s): `src/BrutalUi.Core/Elements/WidgetInput.cs`, `src/BrutalUi.Core/Elements/Button.cs`, `src/BrutalUi.Core/Elements/Toggle.cs`
  - Estimate: 5 steps

- [x] ✅ **5.2 Write WidgetDisabledTests.cs** - Completed 2026-10-26
  - Description: Write `tests/BrutalUi.Core.Tests/Elements/WidgetDisabledTests.cs` with 7 tests: `ADisabledButtonDoesNotClick` (the demo), `ADisabledButtonIgnoresEnterAndSpace` (a key with the button as the caller target, because the drain drops a disabled owner), `ADisableBetweenThePressAndTheReleaseGivesNoClick`, `AnEnableAgainClicks`, `ADisabledToggleDoesNotFlip` (a click and a Space with a caller target. `Checked` stays and the count stays 0), `ACodeSetOfCheckedWorksOnADisabledToggle`, and `APressOnADisabledButtonTakesNoFocus` (the rule of keyboard-focus still holds for a widget).
  - File(s): `tests/BrutalUi.Core.Tests/Elements/WidgetDisabledTests.cs`
  - Estimate: 8 steps

- [x] ✅ **5.3 Run the phase 5 gate** - Completed 2026-10-26
  - Description: Run the build, the tests, the demo class, and the classes of phases 1 to 4. Record the results in `context.md`.
  - File(s): `docs/features/interactive-elements/button-and-toggle/context.md`
  - Estimate: 4 steps

---

## Phase 6: The Allocation Gate and the Record (6/6 complete)

**Demo:** the developer runs `dotnet run --project tests/BrutalUi.Core.Tests/BrutalUi.Core.Tests.csproj -- --filter-class '*WidgetAllocationTests'` and reads a total of 4 with `failed: 0`. A planted allocation at the start of `Button.HandlePointerUp` makes `dotnet test --solution brutal-ui.sln` exit with a value that is not 0.
**Needs:** Phase 5
**Agent tier:** STANDARD (sonnet)
**Tier note:** Copy the gate shape of `FocusAllocationTests` and `ElementAllocationTests`: a `Scene` class that the test builds before the measure, delegates from instance methods that the scene creates one time, `AllocationGate.ReleaseAllocationContext()` before the measured region, and a measured body with no lambda, no `$"`, no `string`, and no `Assert`. Do not add warm-up rounds to hide an allocation.

- [x] ✅ **6.1 Write WidgetAllocationTests.cs** - Completed 2026-10-26
  - Description: Write `tests/BrutalUi.Core.Tests/Elements/WidgetAllocationTests.cs` with 4 tests. The scene: a root with 20 buttons with a text, 20 toggles, and a `Button:hover`, `Button:active`, `Toggle:checked`, and `:disabled` sheet. `UpdateAndDrawAllocateNothingWithWidgets` (100 rounds of `Update` and `Draw`). `TheWidgetInputPathAllocatesNothing` (each step pushes a press and a release over a button, an Enter to the focused button, a press and a release over a toggle, and a Space to the focused toggle, then runs `Update` and `Draw`). `SetTextOnAButtonAllocatesNothing` (two strings of the same length in turn). `TheMeasuredBodyDoesRealWork` (the counts of `Clicked`, `CheckedChanged`, and the `Text` calls after one step).
  - File(s): `tests/BrutalUi.Core.Tests/Elements/WidgetAllocationTests.cs`
  - Estimate: 9 steps

- [x] ✅ **6.2 Prove the gate with a planted allocation** - Completed 2026-10-26
  - Description: Plant `GC.KeepAlive(new object());` as the first line of `Button.HandlePointerUp`. Run `dotnet test --solution brutal-ui.sln > /dev/null 2>&1; echo "exit=$?"` and expect a value that is not 0. Restore the file and check it with `git diff --exit-code -- src/BrutalUi.Core/Elements/Button.cs`. Record the result in `context.md`.
  - File(s): `src/BrutalUi.Core/Elements/Button.cs` (planted and restored), `docs/features/interactive-elements/button-and-toggle/context.md`
  - Estimate: 4 steps

- [x] ✅ **6.3 Edit section 5 of the technical design** - Completed 2026-10-26
  - Description: Edit `docs/technical-design.md` section 5: a new "Widgets" paragraph after "Label and image" with the widget pattern (the ten rules in short), the click rule, the key rule, the `Checked` rule, the disabled rule, and the sender delegates. Edit "The text of a label" (a `Button` has the same `SetText` rules) and "The locale refresh" (it knows `Label` and `Button`). Edit item 3 of "The pointer step of the drain" and its first sentence: the release clears `Active` after its walk.
  - File(s): `docs/technical-design.md`
  - Estimate: 6 steps

- [x] ✅ **6.4 Edit section 9, section 14, and the changelog** - Completed 2026-10-26
  - Description: Edit `docs/technical-design.md` section 9, item 4 (the release order), and section 14 (the new gate and the widget test classes). Edit `CHANGELOG.md` under `## [Unreleased]`: "Added" (`Button`, `Toggle`), and "Changed" (a `PointerUp` handler reads `Active` on the pressed element, and `Label` uses an internal shared text type with no behaviour change).
  - File(s): `docs/technical-design.md`, `CHANGELOG.md`
  - Estimate: 5 steps

- [x] ✅ **6.5 Lint the changed docs** - Completed 2026-10-26
  - Description: Run `python3 /home/aaronstatic/.claude/skills/asd-ste100/scripts/ste-lint.py --fail-over 2.5` on each changed doc and on this plan. Fix each finding above 2.5.
  - File(s): `docs/technical-design.md`, `CHANGELOG.md`, `docs/features/interactive-elements/button-and-toggle/implementation.md`
  - Estimate: 5 steps

- [x] ✅ **6.6 Run the final gate** - Completed 2026-10-26
  - Description: Run the build, the tests, and `scripts/test-linux.sh; echo "exit=$?"`. Record the final test counts against the counts of task 1.1 in `context.md`.
  - File(s): `docs/features/interactive-elements/button-and-toggle/context.md`
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
