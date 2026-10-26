# Button And Toggle - Implementation Plan

**Epic:** interactive-elements (feature 1 of 5)
**Status:** Ready for Review
**Started:** 2026-10-26
**Target Completion:** 2026-10-26
**Last Updated:** 2026-10-26 06:28

---

## Quality Bar

**Target quality:** A core library feature. It sets the widget pattern that slider, scroll-view, and text-field copy. A false click, a lost click, a stuck `Active` state, a key that acts on the wrong element, or a hidden allocation here breaks each game that uses a button, and each later widget copies the defect.

- **A click is exact:** A `Button` clicks only on a left release while it holds `Active`. A release over another element, a release after a press on another element, and a right or middle button give no click. A test asserts each case.
- **A key is exact:** Enter or Space acts on a `KeyDown` only when the widget is the target. A repeat, a key up, and a key with `Control`, `Alt`, or `Super` down do nothing. `Toggle` ignores Enter.
- **One click, one call:** Each activation calls `Clicked` or `CheckedChanged` one time and stops the walk. A code set of `Checked` calls nothing.
- **Disabled means no effect:** A widget with `PseudoStates.Disabled` raises nothing and flips nothing.
- **0 bytes after warm-up:** `Update`, `Draw`, a click, a key activation, a flip, and a `SetText` of a known length allocate 0 bytes. A planted allocation fails a gate.
- **Label does not change:** The shared text type is a pure refactor of `Label`. Each `Label` test passes with no edit, and the public API of `Label` stays the same.
- **API clarity:** Each public member has an XML doc comment in ASD-STE100 prose. The widget pattern is in this plan and in section 5 of the technical design, so a later sibling copies it with no guess.
- **No ImGui in Core:** `BrutalUi.Core` gets no package and no ImGui `using`.

---

## Executive Summary

This feature adds `Button` and `Toggle`, the first widgets of `BrutalUi.Core`. Each one is a sealed `Element` subclass in `src/BrutalUi.Core/Elements/`, with its own type id from `ElementType.Intern`. Each constructor sets `IsFocusable = true` and puts a cached static delegate into `OnPointerUp` and `OnKeyDown` one time. The static handler reads `e.Header.CurrentTarget` as the widget. A new internal static class, `WidgetInput`, holds the two shared checks: the click check (left button, the widget holds `Active`, not `Disabled`) and the key check (the widget is the target, Enter or Space as the widget asks, no repeat, no `Control`, `Alt`, or `Super`, not `Disabled`). Each widget runs its own step after the check: `Button` calls `Clicked`, and `Toggle` flips `Checked` and calls `CheckedChanged`. `Toggle.Checked` reads and writes `PseudoStates.Checked` in `States`, so a `:checked` rule restyles it. `Button` gets a text through `SetText`, with the overloads and the compare rules of `Label`. A new internal struct, `Utf8Text`, holds the buffer, the compare, the measure, and the draw code. `Label` and `Button` both use it.

The codebase check found one conflict. `RoutePointer` clears `Active` in `ReleaseButton` before the walk of the release (Decision 9 of pointer-routing). A `PointerUp` handler thus never sees `Active`, and the confirmed rule "click while it holds `Active`" cannot work. Phase 1 moves the clear of `Active` to the end of the release entry, after its walk. The states after the drain stay the same, so each existing pointer test passes with no edit (Decision 2).

---

## User Personas Alignment

No `docs/user-personas.md` file exists. The personas come from `docs/mission-statement.md` and the epic docs.

**Target Users:**
- [x] **The game developer** (creates a `Button` with a text, sets `Clicked`, creates a `Toggle`, sets `CheckedChanged`, writes `Button:hover`, `Button:active`, `Toggle:checked`, and `:disabled` rules, and sets `Disabled` through `States`)
- [x] **The sibling feature agents** (slider, scroll-view, and text-field copy the widget pattern of the Architecture Overview and call `WidgetInput` where it fits)
- [x] **The widget-sample feature** (puts each widget in HelloPanel, adds the stylesheet rules, and adds the 1000-element gate)
- [x] **The Phase 3 framework epic** (binds a command to `Clicked` and a store to `Checked`)
- [x] **The maintainer** (runs one more 0-byte gate)
- [ ] **The markup author** (not served. The markup epic registers `Button` and `Toggle` by name)

**User Subsets Served:**
- [x] **Simple** - `var play = new Button(new Button.CreateInfo { Text = "Play" }); play.Clicked = OnPlay;` and a `Button:hover` rule.
- [x] **Complex** - a `Toggle` with `CheckedChanged`, a code set of `Checked` from a save file with no call, `States |= PseudoStates.Disabled` on a locked option, Tab and Enter through a menu, and `SetText(LString)` with `RefreshLocalizedText` after a locale change.

**UX Approach:**
- The game reads one delegate for each widget. It never reads the raw slot.
- The widget gives the sender to the delegate (Q3), so one static method can serve many buttons.

---

## Goals

### Primary Goals
1. Add `Button`: a click on a left press and release over it, and on Enter or Space with focus. One `Clicked` call for each click.
2. Add `Toggle`: a flip on a click and on Space with focus. `Checked` sets and clears `PseudoStates.Checked`. One `CheckedChanged` call for each user flip.
3. Block the click and the flip on a widget that holds `Disabled`.
4. Give `Button` a text with the `SetText` overloads and the compare rules of `Label`, through one shared text type.
5. Allocate 0 bytes in `Update`, `Draw`, and the input path after warm-up.
6. Write the widget pattern down for the later siblings.

### Success Criteria
- [ ] The phase 1 demo passes: `scripts/test-linux.sh` prints its PASS line, and the click test in it counts one `Clicked` call.
- [ ] The click, key, toggle, text, and disabled test classes pass.
- [ ] Each `Label` test and each pointer and focus test passes with no edit.
- [ ] `WidgetAllocationTests` measures 0 bytes, and a planted allocation in a widget handler fails it.
- [ ] Section 5 of the technical design holds the widget pattern.

---

## Architecture Overview

### The Active Order Finding

`DrainEvents` calls `RoutePointer` in the target step, before the walk. For a `PointerUp`, `RoutePointer` calls `ReleaseButton`. When the last button goes up, `ReleaseButton` clears `PseudoStates.Active` on `_activeElement` and drops it. Then the walk runs. Thus a `PointerUp` handler today reads no `Active` on any element. No existing test reads `Active` inside a release handler. Each test reads the states after the drain (`PointerPressTests`, `PointerStateEdgeTests`, `GlfwUiInputTests`).

Phase 1 keeps the button mask step in `RoutePointer`, and it moves the clear of `Active` to the end of the release entry: after the walk step, and also in the `finally` block of `DrainEvents`. A release over nothing gets no walk, and the clear still runs in the same drain. The states after the drain do not change (Decision 2).

### File Tree (end state)

```
src/BrutalUi.Core/
├── Elements/WidgetInput.cs        # phase 1 (IsClick), phase 2 (IsKeyActivation, the keys), phase 5 (Disabled)
├── Elements/Button.cs             # phase 1 (click), phase 2 (keys), phase 4 (text)
├── Elements/Toggle.cs             # phase 3
├── Elements/Utf8Text.cs           # phase 4 (moved from Label)
├── Elements/Label.cs              # phase 4 (uses Utf8Text, no API change)
└── Tree/UiContext.cs              # phase 1 (Active clears after the release walk), phase 4 (RefreshSubtree knows Button)
tests/BrutalUi.Core.Tests/
├── Input/ReleaseOrderTests.cs     # phase 1
├── Elements/WidgetTree.cs         # phase 1, phase 3 (adds the toggle)
├── Elements/ButtonClickTests.cs   # phase 1
├── Elements/ButtonKeyTests.cs     # phase 2
├── Elements/ToggleTests.cs        # phase 3
├── Elements/ButtonTextTests.cs    # phase 4
├── Elements/WidgetDisabledTests.cs # phase 5
└── Elements/WidgetAllocationTests.cs # phase 6
docs/technical-design.md           # phase 6: sections 5, 9, 14
CHANGELOG.md                       # phase 6
docs/features/interactive-elements/button-and-toggle/context.md   # each phase
```

These files do not change: `Element.cs`, `PseudoStates.cs`, `ElementHandlers.cs`, `EventWalk.cs`, `HitWalk.cs`, `FocusWalk.cs`, `PaintWalk.cs`, `Image.cs`, each `.csproj`, `Directory.Packages.props`, `samples/`, `scripts/`, and each existing test file.

### Public API (end state)

```csharp
namespace BrutalUi.Core;

public sealed class Button : Element
{
    public Button(in CreateInfo info);                   // phase 1. Phase 4: throws ArgumentException for two texts
    public override ElementTypeId TypeId { get; }        // phase 1: the id of "Button" (A6)
    public Action<Button>? Clicked { get; set; }         // phase 1 (Q3)
    public ReadOnlySpan<byte> Text { get; }              // phase 4
    public LString? LocalizedText { get; }               // phase 4
    public void SetText(ReadOnlySpan<byte> utf8);        // phase 4: the rules of Label
    public void SetText(string? text);                   // phase 4
    public void SetText(LString? text);                  // phase 4

    public struct CreateInfo                             // phase 1: no member. Phase 4 adds the two members (A7)
    {
        public string? Text;
        public LString? LocalizedText;
    }
}

public sealed class Toggle : Element
{
    public Toggle(in CreateInfo info);                   // phase 3
    public override ElementTypeId TypeId { get; }        // phase 3: the id of "Toggle" (A6)
    public bool Checked { get; set; }                    // phase 3: reads and writes PseudoStates.Checked. A set calls nothing (Q4)
    public Action<Toggle>? CheckedChanged { get; set; }  // phase 3 (Q3)

    public struct CreateInfo
    {
        public bool Checked;                             // phase 3: the start value (A7)
    }
}
```

No create-info member is `required`: each one has a safe default. `Label` keeps its public API.

### Internal Types

- `WidgetInput` (`src/BrutalUi.Core/Elements/WidgetInput.cs`): an internal static class.
  - `internal const int SpaceKey = 32;` (GLFW `GLFW_KEY_SPACE`) and `internal const int EnterKey = 257;` (GLFW `GLFW_KEY_ENTER`). Phase 2.
  - `[Flags] internal enum ActivationKeys { None = 0, Space = 1, Enter = 2 }`. Phase 2.
  - `internal static bool IsClick(Element widget, in PointerEvent e)`: `e.Button == PointerButton.Left` and `widget.States` holds `Active`. Phase 5 adds: and no `Disabled`.
  - `internal static bool IsKeyActivation(Element widget, in KeyEvent e, ActivationKeys keys)`: `e.Header.Target` is `widget`, `e.IsRepeat` is `false`, no `Control`, `Alt`, or `Super` in `e.Modifiers`, and the key is in `keys`. Phase 5 adds: and no `Disabled`.
- `Utf8Text` (`src/BrutalUi.Core/Elements/Utf8Text.cs`): an internal mutable struct. Phase 4. It holds `_utf8` (`byte[]`, 32 bytes at the start), `_length`, `_lastText`, `_localized`, and `_lastLocalized`, the fields of `Label` today.
  - `internal Utf8Text(string? text, LString? localized)`: encodes one text. The element checks the "both set" rule first.
  - `internal readonly ReadOnlySpan<byte> Span` and `internal readonly LString? Localized`.
  - `internal bool Set(ReadOnlySpan<byte> utf8)`, `internal bool Set(string? text)`, `internal bool Set(LString? text)`, and `internal bool Refresh()`: each one returns `true` when the bytes changed. The element marks `Layout | Paint` on `true`. The struct sets no dirty bit.
  - `internal static bool Utf8Equals(...)`, `internal static float SnapWrapWidth(float)`, `internal static float DrawWrapWidth(float, float)`, `internal static float2 Measure(IUiRenderer measurer, in ComputedStyle style, ReadOnlySpan<byte> text, float width, MeasureMode widthMode)`, and `internal static void Paint(IUiRenderer renderer, in ComputedStyle style, in float4 content, float borderBoxWidth, float opacity, ReadOnlySpan<byte> text)`. The owner computes `content` with `Layout.ContentBox(in borderBox)`. The code and its doc comments move from `Label` with no rule change.
  - Each owner holds the struct in a non-`readonly` field and calls each method on the field. A copy of the struct loses the change.

### The Widget Pattern (for slider, scroll-view, and text-field)

Each later widget copies this shape. The sibling plans cite this section.

1. **The class.** `public sealed class <Name> : Element` in `src/BrutalUi.Core/Elements/`. A `private static readonly ElementTypeId s_typeId = ElementType.Intern("<Name>");` and `public override ElementTypeId TypeId => s_typeId;` (A6).
2. **The create-info.** A nested `public struct CreateInfo`. A member is `required` only when it has no safe default. The constructor takes `in CreateInfo info`.
3. **The handlers.** One `private static readonly UiEventHandler<T> s_<slot> = Handle<Slot>;` for each slot. The constructor writes each one into its slot with `=`, one time (A2). The delegate has no target, so each instance shares it, and the constructor allocates only the side object of the slots.
4. **The handler body.** `if (e.Header.CurrentTarget is not <Name> widget || !<check>) { return; }`. Then `e.Header.StopPropagation();` (A4), then the own step of the widget. The `is` pattern makes a copy of the delegate onto another element safe.
5. **The checks.** Use `WidgetInput.IsClick` for a click, and `WidgetInput.IsKeyActivation` with the keys of the widget. A widget with another rule (a drag, an arrow key, a char) writes its own check. That check also returns `false` for `Disabled`, and it never stops the walk for an event that it does not use.
6. **The state.** A pseudo-state bit lives in `States` only. A property reads the bit and writes it through `States`, so no field can disagree with the bit (A3 of the epic).
7. **The change.** A `public Action<<Name>>? <Change> { get; set; }` with the sender (Q3). Only user input calls it. A code set calls nothing (Q4). Call it after the state change, so the delegate reads the new value.
8. **Focus.** The constructor sets `IsFocusable = true` (A5).
9. **The draw.** A widget with content overrides `internal override void PaintContent(...)` and follows its rules: no tree change, no dirty bit, no allocation.
10. **The tests.** A test class for each slice, a disabled test, and a case in the allocation gate. The tests drive the widget through `UiContext.PushEvent` and `Update` or `DrainEvents`, with no caller target, so the hit test and the focus route run.

```csharp
public sealed class Button : Element
{
    private static readonly ElementTypeId s_typeId = ElementType.Intern("Button");
    private static readonly UiEventHandler<PointerEvent> s_pointerUp = HandlePointerUp;

    public Button(in CreateInfo info)
    {
        IsFocusable = true;
        OnPointerUp = s_pointerUp;
    }

    private static void HandlePointerUp(ref PointerEvent e)
    {
        if (e.Header.CurrentTarget is not Button button || !WidgetInput.IsClick(button, in e))
        {
            return;
        }

        e.Header.StopPropagation();
        button.Clicked?.Invoke(button);
    }
}
```

### Data Flow

```
Game: ui.PushEvent(PointerDown at p) ... ui.PushEvent(PointerUp at p) ... ui.Update(dt)
UiContext.DrainEvents
  PointerDown, no target:
    RoutePointer: hit = Button. Hover on Button. PressButton: Active on Button. FocusOnPress: Button owns focus
    walk: Button has no PointerDown handler
  PointerUp, no target:
    RoutePointer: hit test. Hover. ReleaseButton: the mask bit goes off. The last button sets the release flag   (phase 1)
    walk: Button.HandlePointerUp
          WidgetInput.IsClick: Left, Button holds Active, no Disabled ──► StopPropagation ──► Clicked(button)
    FinishRelease: Active goes off the stored element. The stored element is null                              (phase 1)
    (finally of DrainEvents: FinishRelease runs again when a handler throws)
  KeyDown Enter or Space, no target:
    target = FocusedElement (the Button)
    walk: Button.HandleKeyDown
          WidgetInput.IsKeyActivation(target is Button, no repeat, no Control, Alt, Super, key in mask, no Disabled)
          ──► StopPropagation ──► Clicked(button)            (Toggle: Space only ──► Checked = !Checked ──► CheckedChanged)
UiContext.Update: the style pass restyles the subtrees that the state changes marked (:hover, :active, :checked)
```

---

## Implementation Phases

Each phase is a slice: one narrow path that works end to end, with a demo line that a person can check. Section 11 of `.beast-mode/phase-handoff.md` holds the rules. Confirmed item Q6 names the demo of phase 1.

Each phase ends with `dotnet build brutal-ui.sln 2>&1 | tail -n 30` and `dotnet test --solution brutal-ui.sln 2>&1 | tail -n 30` (tool rule 5). Expect `0 Warning(s)`, `0 Error(s)`, and 0 failed. The projects have no implicit usings, so each new file holds its own `using` lines. Each public member needs an XML doc comment (CS1591 is an error). Use `namespace BrutalUi.Core;` in Core and `namespace BrutalUi.Core.Tests;` in the tests. Prose in comments follows ASD-STE100. Each test class interns its classes with a unique name prefix, because xUnit runs the classes in parallel. Run one class with `dotnet run --project tests/BrutalUi.Core.Tests/BrutalUi.Core.Tests.csproj -- --filter-class '*<Class>' 2>&1 | tail -n 5`. Each phase runs the pointer and focus classes (`PointerPressTests`, `PointerStateEdgeTests`, `PointerAllocationTests`, `ClickFocusTests`, `FocusOwnerTests`, `TabNavigationTests`, `FocusStateEdgeTests`, `FocusAllocationTests`, `RoutingGateTests`) with no edit.

### Phase 1: A Pointer Click on a Button
**Goal:** The smallest path that proves the idea: a left press and a release over a `Button` call `Clicked` one time, with the button as the sender.
**Demo:** a person runs `scripts/test-linux.sh` and reads its PASS line. In that run, `ButtonClickTests.APressAndAReleaseClickTheButtonOnce` pushes a press and a release over a `Button` through `UiContext.PushEvent`, runs `Update`, and counts one `Clicked` call with the button as the sender.
**Needs:** none
**Agent tier:** ADVANCED (opus)

**Tier note:** Group A changes `DrainEvents` and `RoutePointer`, the central methods of input-routing. Move only the clear of `Active`. Do not change the hit test, `Hover`, the button mask, the click focus, the Tab step, the drop count, or `EventWalk.cs`. Run each pointer and focus class with no edit before group B starts.

**Tasks (group A: the release order):**
- [ ] 1.1 Record the test counts of `BrutalUi.Core.Tests` and of each pointer and focus class in `docs/features/interactive-elements/button-and-toggle/context.md` before any change. Record the Active order finding of the Architecture Overview.
- [ ] 1.2 Edit `src/BrutalUi.Core/Tree/UiContext.cs`: add `private bool _releasePending;`. Change `ReleaseButton` so it clears the mask bit only, and sets `_releasePending = true` when the mask is 0 and `_activeElement` is not null. Add `private void FinishRelease()`: when `_releasePending` is `true`, clear it, clear `PseudoStates.Active` on `_activeElement`, and set `_activeElement` to null. Call `FinishRelease()` in `DrainEvents` right after the walk step of each entry, before the Tab step, and one more time in the `finally` block. A release over nothing gets no walk, and the call after the walk step still clears `Active`.
- [ ] 1.3 Edit the doc comments in `UiContext.cs`: the Active rule paragraph of `DrainEvents` (the release clears `Active` after its walk, so a `PointerUp` handler reads `Active` on the pressed element), the summary and remarks of `RoutePointer`, the summary of `ReleaseButton`, and the remarks of `ActiveElement`. Name Decision 9 of pointer-routing and Decision 2 of this plan.
- [ ] 1.4 Write `tests/BrutalUi.Core.Tests/Input/ReleaseOrderTests.cs` with 4 tests on `PointerTree`: `AReleaseHandlerOfThePressedElementReadsActive`, `TheDrainClearsActiveAfterTheReleaseWalk` (no `Active` and a null `ActiveElement` after `DrainEvents`), `AReleaseOverAnotherElementSeesActiveOnThePressedElement` (a handler on `Left` reads `Active` on `Right` and not on `Left`), and `AThrowingReleaseHandlerStillClearsActive` (`DrainEvents` throws, then no element holds `Active`, and the next drain runs).
- [ ] 1.5 Run the build and the tests. Run `ReleaseOrderTests` and each pointer and focus class with no edit. Record the results in `context.md`.

**Tasks (group B: the button):**
- [ ] 1.6 Write `src/BrutalUi.Core/Elements/WidgetInput.cs`: the internal static class with `IsClick(Element widget, in PointerEvent e)` (left button and `Active` on the widget). The class summary states the role of the helper for each widget of the epic.
- [ ] 1.7 Write `src/BrutalUi.Core/Elements/Button.cs` by the widget pattern: sealed, the type id of `Button`, `public Button(in CreateInfo info)`, an empty `public struct CreateInfo` (phase 4 adds the members), `IsFocusable = true`, `OnPointerUp = s_pointerUp`, `public Action<Button>? Clicked { get; set; }`, and the static `HandlePointerUp`. The remarks state: the click rule, the stop (A4), the sender (Q3), "read `Clicked`, not the slot", "a write of `=` to `OnPointerUp` removes the click", and "a pickable child of the button gets `Active` in place of the button, so set `IsPickable = false` on a child".
- [ ] 1.8 Write `tests/BrutalUi.Core.Tests/Elements/WidgetTree.cs`: an internal test tree. The context has a `RecordingRenderer` measurer and a viewport of 800 x 600. The root is 400 x 300 in a row. The children are `Button` (120 x 40 at 0, 0) and `Other`, a plain `Element` (100 x 100 at 120, 0). It holds the points `OverButton`, `OverOther`, and `OverNothing`, and a `Settle()` method. Use the factories of `PointerTree` for the events. A later phase adds a child at the end of the row only, so these rects stay.
- [ ] 1.9 Write `tests/BrutalUi.Core.Tests/Elements/ButtonClickTests.cs` with 8 tests: `APressAndAReleaseClickTheButtonOnce` (the demo, through `PushEvent` and `Update`), `AReleaseOverAnotherElementDoesNotClick`, `AReleaseAfterAPressElsewhereDoesNotClick`, `ARightButtonDoesNotClick` (A1), `AClickStopsTheReleaseWalk` (the root capture handler runs, the root bubble handler does not), `AButtonWithNoHandlerStopsTheWalkAndDoesNotThrow`, `APressFocusesTheButton` (A5, `IsFocusable` is `true`, and `FocusedElement` is the button), and `ATypeRuleMatchesTheButton` (A6, a rule with `Type = ElementType.Intern("Button")` sets a fill on the button and not on `Other`).
- [ ] 1.10 Run the build, the tests, the demo class, and `scripts/test-linux.sh; echo "exit=$?"`. Expect the PASS line and `exit=0`. Record the counts and each decision that this plan did not settle in `context.md`.

**Estimated Steps:** about 60 (10 tasks, about six steps for one task)

**Files (group A, 3):** `UiContext.cs`, `ReleaseOrderTests.cs`, `context.md`
**Files (group B, 5):** `WidgetInput.cs`, `Button.cs`, `WidgetTree.cs`, `ButtonClickTests.cs`, `context.md`

**Acceptance Criteria:**
- [ ] The 4 tests of `ReleaseOrderTests` and the 8 tests of `ButtonClickTests` pass.
- [ ] Each pointer and focus class passes with no edit.
- [ ] `scripts/test-linux.sh` prints its PASS line.
- [ ] `EventWalk.cs` and `Element.cs` have no diff.

---

### Phase 2: Enter and Space on a Focused Button
**Goal:** A focused `Button` clicks on an Enter or a Space key down. A repeat, a key up, another key, and a key with `Control`, `Alt`, or `Super` do nothing.
**Demo:** a unit test calls `SetFocus(button)`, pushes an Enter key down with no target, and runs `Update`. It counts one `Clicked` call. The developer runs `dotnet run --project tests/BrutalUi.Core.Tests/BrutalUi.Core.Tests.csproj -- --filter-class '*ButtonKeyTests'` and reads a total of 9 with `failed: 0`.
**Needs:** Phase 1
**Agent tier:** STANDARD (sonnet)

**Tasks:**
- [ ] 2.1 Edit `src/BrutalUi.Core/Elements/WidgetInput.cs`: add `SpaceKey`, `EnterKey`, the `ActivationKeys` flags, and `IsKeyActivation(Element widget, in KeyEvent e, ActivationKeys keys)`. The check needs the widget as `e.Header.Target` (Decision 5), no repeat (Q2), and no `Control`, `Alt`, or `Super` (Decision 5). `Shift` does not block it.
- [ ] 2.2 Edit `src/BrutalUi.Core/Elements/Button.cs`: add `s_keyDown`, write it into `OnKeyDown` in the constructor, and add the static `HandleKeyDown` with `ActivationKeys.Enter | ActivationKeys.Space`. Extract one private `Click()` step that the two handlers share: it calls `Clicked`. Extend the remarks with the key rule.
- [ ] 2.3 Write `tests/BrutalUi.Core.Tests/Elements/ButtonKeyTests.cs` with 9 tests on `WidgetTree`: `AFocusedButtonClicksOnEnter` (the demo), `AFocusedButtonClicksOnSpace`, `ARepeatDoesNotClick`, `AKeyUpDoesNotClick`, `AnotherKeyDoesNotClickAndWalksOn` (the key A reaches the root bubble handler), `AModifierStopsTheActivation` (`Control`+Enter, `Alt`+Enter, and `Super`+Space give no click. `Shift`+Enter clicks), `AKeyActivationStopsTheWalk`, `TabThenEnterClicks` (a Tab with no owner focuses the button, and the next Enter clicks), and `AKeyFromAFocusableChildDoesNotClick` (a focusable child of the button owns the focus, and Enter bubbles to the button with no click).
- [ ] 2.4 Run the build, the tests, the demo class, and the classes of phase 1. Record the results in `context.md`.

**Estimated Steps:** about 24 (4 tasks, about six steps for one task)

**Files (4):** `WidgetInput.cs`, `Button.cs`, `ButtonKeyTests.cs`, `context.md`

**Acceptance Criteria:**
- [ ] The 9 tests of `ButtonKeyTests` pass, and the tests of phase 1 still pass.
- [ ] `TabNavigationTests` and `FocusOwnerTests` pass with no edit.

---

### Phase 3: Toggle
**Goal:** A `Toggle` flips on a click and on Space with focus. `Checked` sets and clears `PseudoStates.Checked`, and a `:checked` rule restyles it. Only user input calls `CheckedChanged`.
**Demo:** a unit test pushes a press and a release over a `Toggle` and runs `Update`. It reads `Checked` as `true`, `PseudoStates.Checked` in `States`, the fill of a `Toggle:checked` rule, and one `CheckedChanged` call that reads the new value. The developer runs `dotnet run --project tests/BrutalUi.Core.Tests/BrutalUi.Core.Tests.csproj -- --filter-class '*ToggleTests'` and reads a total of 10 with `failed: 0`.
**Needs:** Phase 2
**Agent tier:** STANDARD (sonnet)

**Tasks:**
- [ ] 3.1 Write `src/BrutalUi.Core/Elements/Toggle.cs` by the widget pattern: sealed, the type id of `Toggle`, `public Toggle(in CreateInfo info)`, `public struct CreateInfo { public bool Checked; }`, `IsFocusable = true`, `OnPointerUp` and `OnKeyDown` from cached static delegates, `public bool Checked` (Decision 4), and `public Action<Toggle>? CheckedChanged { get; set; }`. The key handler passes `ActivationKeys.Space` only (A3). A private `Flip()` step sets `Checked = !Checked` and then calls `CheckedChanged`. The constructor writes the start value through `States` and calls nothing. The remarks state: no text (Q5), the size comes from the style (Decision 9), and a code set calls nothing (Q4).
- [ ] 3.2 Edit `tests/BrutalUi.Core.Tests/Elements/WidgetTree.cs` with additions only: a `Toggle` child (40 x 40) at the end of the row, at (220, 0), and the point `OverToggle`.
- [ ] 3.3 Write `tests/BrutalUi.Core.Tests/Elements/ToggleTests.cs` with 10 tests: `AClickFlipsTheToggle` (the demo), `ASecondClickClearsChecked`, `SpaceFlipsAFocusedToggle`, `EnterDoesNothingOnAFocusedToggle` (A3, and the root bubble handler gets the key), `ARepeatSpaceDoesNothing`, `ACodeSetCallsNoCheckedChanged` (Q4, the bit changes and the delegate count stays 0), `ASetToTheSameValueMarksNothing` (no `DirtyFlags.Style` after a settle and a set to the same value), `TheCreateInfoSetsTheStartValue` (A7, no call), `AReleaseElsewhereOrARightButtonDoesNotFlip` (A1), and `CheckedReadsTheStateBit` (a clear of the bit through `States` reads back as `false`. The type id is `Toggle`, and `IsFocusable` is `true`).
- [ ] 3.4 Run the build, the tests, the demo class, and the classes of phases 1 and 2. Record the results in `context.md`.

**Estimated Steps:** about 30 (4 tasks, about seven steps for one task)

**Files (4):** `Toggle.cs`, `WidgetTree.cs`, `ToggleTests.cs`, `context.md`

**Acceptance Criteria:**
- [ ] The 10 tests of `ToggleTests` pass, and the tests of phases 1 and 2 still pass.
- [ ] `Toggle.cs` holds no `bool` field for the checked state.

---

### Phase 4: Button Text
**Goal:** A `Button` shows a text. It has the `SetText` overloads and the compare rules of `Label`, through one shared internal text type. `Label` does not change its behaviour.
**Demo:** a unit test creates a `Button` with `Text = "Play"` and a padding of 8, runs `Update` and `Draw` with a `RecordingRenderer`, and reads one `Text` call with the bytes of `Play` at the top-left corner of the content box. The button size is the measured text size plus the padding. The developer runs `dotnet run --project tests/BrutalUi.Core.Tests/BrutalUi.Core.Tests.csproj -- --filter-class '*ButtonTextTests'` and reads a total of 9 with `failed: 0`.
**Needs:** Phase 3
**Agent tier:** ADVANCED (opus)

**Tier note:** Group A is a pure refactor of `Label`. It moves code. It changes no rule, no doc rule, and no public signature. Keep `Label.DrawWrapWidth`, `Label.SnapWrapWidth`, and `Label.Utf8Equals` as internal forwarders, because `ContentBoxTests`, `LabelWrapTests`, and `LabelTextTests` call them. Keep the exception message of `MeasureLabel` in `Label`, because `LabelTests.ThrowsWithNoMeasurer` reads it. Hold `Utf8Text` in a field with no `readonly` modifier. Run each `Label` test with no edit before group B starts.

**Tasks (group A: the shared text type):**
- [ ] 4.1 Write `src/BrutalUi.Core/Elements/Utf8Text.cs`: the internal struct of the Internal Types section. Move from `Label` the five text fields, `InitialTextCapacity`, `CompareChunkLength`, `WrapSnapTolerance`, `EnsureCapacity`, `EncodeText`, `ApplyText`, `ClearLocalized`, `Utf8Equals`, `SnapWrapWidth`, `DrawWrapWidth`, `FiniteSize`, `FiniteLength`, the measure rule of `MeasureLabel` (after the measurer check), and the body of `PaintContent`. Move each doc comment with its code. Each `Set` method and `Refresh` return `true` for a change and set no dirty bit.
- [ ] 4.2 Edit `src/BrutalUi.Core/Elements/Label.cs`: hold `private Utf8Text _text;`. The constructor checks the "both set" rule, then creates the struct. `Text`, `LocalizedText`, the three `SetText` overloads, and `RefreshLocalized` call the struct and mark `DirtyFlags.Layout | DirtyFlags.Paint` on `true`. `MeasureLabel` keeps its measurer check and its message, then calls `Utf8Text.Measure`. `PaintContent` calls `Utf8Text.Paint`. Keep the three internal static forwarders. Keep the class remarks and each public doc comment.
- [ ] 4.3 Run the build and the tests. Run `LabelTests`, `LabelTextTests`, `LabelWrapTests`, `ContentBoxTests`, `PaintContentTests`, `ElementAllocationTests`, and the ImGui label tests with no edit. Run `git diff main --stat -- tests/` and expect no changed file. Record the counts in `context.md`.

**Tasks (group B: the button text):**
- [ ] 4.4 Edit `src/BrutalUi.Core/Elements/Button.cs`: add `Text` and `LocalizedText` to `CreateInfo` (A7). The constructor throws `ArgumentException` when both are set, with the message of `Label` for `Button.CreateInfo`. Add `private Utf8Text _text;`, the `Text` and `LocalizedText` properties, the three `SetText` overloads, and `internal void RefreshLocalized()`, each with the doc comments of `Label`. Set `Layout.Measure = s_measure` with a static `MeasureButton` that throws `InvalidOperationException` with the message "A Button needs a UiContext with a measurer. Create the context with UiContext.CreateInfo." when no measurer exists. Override `PaintContent` with `Utf8Text.Paint`. The remarks state: the text draws at the top-left corner of the content box (Decision 8), a button with a child does not measure its text, and the style gives the color, the font, and the size.
- [ ] 4.5 Edit `src/BrutalUi.Core/Tree/UiContext.cs`: `RefreshSubtree` also calls `RefreshLocalized` on a `Button` (Decision 7). Update the doc comments of `RefreshLocalizedText` and `RefreshSubtree`.
- [ ] 4.6 Write `tests/BrutalUi.Core.Tests/Elements/ButtonTextTests.cs` with 9 tests: `AButtonDrawsItsText` (the demo), `TheButtonTakesTheSizeOfItsText`, `SetTextFromASpanMarksLayoutAndPaint`, `SetTextFromTheSameTextMarksNothing` (the span, the string, and a string after a span), `SetTextFromAnLStringShowsTheLocalizedText`, `RefreshLocalizedTextReachesAButton`, `ThrowsForTwoTexts`, `AButtonNeedsAMeasurer` (the message of task 4.4), and `AClickOnTheTextClicksTheButton`.
- [ ] 4.7 Run the build, the tests, the demo class, the `Label` classes of task 4.3, and the classes of phases 1 to 3. Record the results in `context.md`.

**Estimated Steps:** about 48 (7 tasks, about seven steps for one task)

**Files (group A, 3):** `Utf8Text.cs`, `Label.cs`, `context.md`
**Files (group B, 4):** `Button.cs`, `UiContext.cs`, `ButtonTextTests.cs`, `context.md`

**Acceptance Criteria:**
- [ ] The 9 tests of `ButtonTextTests` pass.
- [ ] Each `Label` test passes with no edit, and no existing test file has a diff.
- [ ] The public members of `Label` are the same as on `main`.
- [ ] `grep -rn 'readonly Utf8Text' src/` gives no line.

---

### Phase 5: Disabled Widgets
**Goal:** A widget that holds `PseudoStates.Disabled` raises no click and no flip, and it does not stop the walk.
**Demo:** a unit test sets `Disabled` on the `Button`, pushes a press and a release over it, and runs `Update`. It counts 0 `Clicked` calls, and the root bubble handler gets the release. The developer runs `dotnet run --project tests/BrutalUi.Core.Tests/BrutalUi.Core.Tests.csproj -- --filter-class '*WidgetDisabledTests'` and reads a total of 7 with `failed: 0`.
**Needs:** Phase 4
**Agent tier:** STANDARD (sonnet)

**Tasks:**
- [ ] 5.1 Edit `src/BrutalUi.Core/Elements/WidgetInput.cs`: `IsClick` and `IsKeyActivation` return `false` when `widget.States` holds `Disabled` (Decision 6). Edit the remarks of `Button` and `Toggle`: a disabled widget raises nothing and does not stop the walk. A code set of `Checked` still works on a disabled toggle.
- [ ] 5.2 Write `tests/BrutalUi.Core.Tests/Elements/WidgetDisabledTests.cs` with 7 tests: `ADisabledButtonDoesNotClick` (the demo), `ADisabledButtonIgnoresEnterAndSpace` (a key with the button as the caller target, because the drain drops a disabled owner), `ADisableBetweenThePressAndTheReleaseGivesNoClick`, `AnEnableAgainClicks`, `ADisabledToggleDoesNotFlip` (a click and a Space with a caller target. `Checked` stays and the count stays 0), `ACodeSetOfCheckedWorksOnADisabledToggle`, and `APressOnADisabledButtonTakesNoFocus` (the rule of keyboard-focus still holds for a widget).
- [ ] 5.3 Run the build, the tests, the demo class, and the classes of phases 1 to 4. Record the results in `context.md`.

**Estimated Steps:** about 18 (3 tasks, about six steps for one task)

**Files (3):** `WidgetInput.cs`, `WidgetDisabledTests.cs`, `context.md`

**Acceptance Criteria:**
- [ ] The 7 tests of `WidgetDisabledTests` pass, and the tests of phases 1 to 4 still pass.
- [ ] `Button.cs` and `Toggle.cs` hold no `Disabled` check of their own. The check lives in `WidgetInput` only.

---

### Phase 6: The Allocation Gate and the Record
**Goal:** Prove 0 bytes for `Update`, `Draw`, and the full widget input path after warm-up. Record the widget pattern and the release order in the technical design and the changelog.
**Demo:** the developer runs `dotnet run --project tests/BrutalUi.Core.Tests/BrutalUi.Core.Tests.csproj -- --filter-class '*WidgetAllocationTests'` and reads a total of 4 with `failed: 0`. A planted allocation at the start of `Button.HandlePointerUp` makes `dotnet test --solution brutal-ui.sln` exit with a value that is not 0.
**Needs:** Phase 5
**Agent tier:** STANDARD (sonnet)

**Tier note:** Copy the gate shape of `FocusAllocationTests` and `ElementAllocationTests`: a `Scene` class that the test builds before the measure, delegates from instance methods that the scene creates one time, `AllocationGate.ReleaseAllocationContext()` before the measured region, and a measured body with no lambda, no `$"`, no `string`, and no `Assert`. Do not add warm-up rounds to hide an allocation.

**Tasks:**
- [ ] 6.1 Write `tests/BrutalUi.Core.Tests/Elements/WidgetAllocationTests.cs` with 4 tests. The scene: a root with 20 buttons with a text, 20 toggles, and a `Button:hover`, `Button:active`, `Toggle:checked`, and `:disabled` sheet. `UpdateAndDrawAllocateNothingWithWidgets` (100 rounds of `Update` and `Draw`). `TheWidgetInputPathAllocatesNothing` (each step pushes a press and a release over a button, an Enter to the focused button, a press and a release over a toggle, and a Space to the focused toggle, then runs `Update` and `Draw`). `SetTextOnAButtonAllocatesNothing` (two strings of the same length in turn). `TheMeasuredBodyDoesRealWork` (the counts of `Clicked`, `CheckedChanged`, and the `Text` calls after one step).
- [ ] 6.2 Plant `GC.KeepAlive(new object());` as the first line of `Button.HandlePointerUp`. Run `dotnet test --solution brutal-ui.sln > /dev/null 2>&1; echo "exit=$?"` and expect a value that is not 0. Restore the file and check it with `git diff --exit-code -- src/BrutalUi.Core/Elements/Button.cs`. Record the result in `context.md`.
- [ ] 6.3 Edit `docs/technical-design.md` section 5: a new "Widgets" paragraph after "Label and image" with the widget pattern (the ten rules in short), the click rule, the key rule, the `Checked` rule, the disabled rule, and the sender delegates. Edit "The text of a label" (a `Button` has the same `SetText` rules) and "The locale refresh" (it knows `Label` and `Button`). Edit item 3 of "The pointer step of the drain" and its first sentence: the release clears `Active` after its walk.
- [ ] 6.4 Edit `docs/technical-design.md` section 9, item 4 (the release order), and section 14 (the new gate and the widget test classes). Edit `CHANGELOG.md` under `## [Unreleased]`: "Added" (`Button`, `Toggle`), and "Changed" (a `PointerUp` handler reads `Active` on the pressed element, and `Label` uses an internal shared text type with no behaviour change).
- [ ] 6.5 Run `python3 /home/aaronstatic/.claude/skills/asd-ste100/scripts/ste-lint.py --fail-over 2.5` on each changed doc and on this plan. Fix each finding above 2.5.
- [ ] 6.6 Run the build, the tests, and `scripts/test-linux.sh; echo "exit=$?"`. Record the final test counts against the counts of task 1.1 in `context.md`.

**Estimated Steps:** about 36 (6 tasks, about six steps for one task)

**Files (5):** `WidgetAllocationTests.cs`, `Button.cs` (planted and restored), `technical-design.md`, `CHANGELOG.md`, `context.md`

**Acceptance Criteria:**
- [ ] The 4 tests of `WidgetAllocationTests` pass.
- [ ] The planted allocation fails the run, and the file has no diff after the restore.
- [ ] `scripts/test-linux.sh` prints its PASS line.

---

## Key Technical Decisions

### Confirmed Assumptions

- Q1 Button text: a shared internal text type holds the UTF-8 buffer, compare, and measure code. Label and Button both use it.
- Q2 Keys: Enter or Space acts on KeyDown. A repeat KeyDown does nothing.
- Q3 Delegate: `Action<Button>? Clicked` and `Action<Toggle>? CheckedChanged`, with the sender.
- Q4 Set rule: only user input calls `CheckedChanged`. A code set of `Checked` calls nothing.
- Q5 Toggle: no text. The game puts a `Label` next to it.
- Q6 Demo: a person runs `scripts/test-linux.sh`. A test clicks a `Button` through `UiContext` and gets one `Clicked` call.
- A1 Only `PointerButton.Left` clicks a button or flips a toggle.
- A2 A handler is a static method that reads `e.Header.CurrentTarget` as the widget. The handler slots are the public properties on `Element`.
- A3 Enter on a focused `Toggle` does nothing. Only Space flips it.
- A4 A click or key activation marks the event handled with `StopPropagation`.
- A5 `Button` and `Toggle` set `IsFocusable = true` in the constructor. Code can change it later.
- A6 The type names are `Button` and `Toggle`, interned through `ElementType.Intern`.
- A7 `Toggle.CreateInfo` takes a start `Checked` value. `Button.CreateInfo` takes `Text` or `LocalizedText`, like `Label`.

### Decision 1: One static helper for the shared checks
**Choice:** `Button` and `Toggle` derive from `Element` directly. The internal static class `WidgetInput` holds the click check and the key check. Each widget calls a check and then runs its own step.
**Rationale:** The chosen approach. It matches epic decision A2 (a sealed `Element` subclass) and keeps one class level. The disabled rule lives in one place. slider, scroll-view, and text-field use the helper only where it fits: a slider uses no click, and a text field uses no Space.
**Alternatives:** (a) An abstract `ActivatableElement` base class: a second class level, and a sealed sibling cannot share it with a drag widget. (b) A copy of the checks in each widget: the disabled rule drifts.

### Decision 2: `Active` stays on the pressed element through the release walk
**Choice:** `ReleaseButton` clears the mask bit before the walk, as today. The clear of `PseudoStates.Active` moves to `FinishRelease`, after the walk of the release entry and in the `finally` block of `DrainEvents`. This amends Decision 9 of pointer-routing for the release only.
**Rationale:** The confirmed rule says that a button clicks on a release while it holds `Active`. The code clears `Active` before the walk, so no handler can read it (the Active order finding). With the change, `Active` on the button means that the press hit the button, and the walk to the handler means that the release is over the button or its subtree. That is the epic rule "a pointer up over the pressed element". The states after the drain are the same as before, so the style pass and each existing test see no change. The `finally` call keeps `Active` from a stuck state when a handler throws.
**Alternatives:** (a) A press flag in the widget: a press on the button and a release elsewhere leave the flag set, and a later press elsewhere with a release on the button gives a false click. (b) A context field "the released element" that the handler reads: an element holds no context reference, and a static would add global state. (c) `Active` on each ancestor: it breaks Decision R1 of pointer-routing.

### Decision 3: Cached static delegates, and an `is` cast of the current target
**Choice:** Each widget holds one `private static readonly UiEventHandler<T>` for each slot. The constructor writes it into the slot with `=`. The handler starts with `if (e.Header.CurrentTarget is not Button button ...) return;`.
**Rationale:** A2. The delegate has no target, so all widgets share it and the constructor allocates only the side object of the slots. The `is` pattern allocates nothing, and a copy of the delegate onto another element does nothing in place of a throw.
**Alternatives:** An instance method delegate: one more allocation for each widget, at build time.

### Decision 4: `Toggle.Checked` reads the state bit
**Choice:** `Checked => (States & PseudoStates.Checked) != 0`. The setter writes `States` with the bit set or cleared. The `States` setter returns at once for the same value, so a set to the same value marks nothing.
**Rationale:** One source of truth. A code write through `States` and a write through `Checked` always agree, and the stylesheet reads the same bit (epic A3).
**Alternatives:** A `bool` field plus a bit write: two values that a `States` write can split.

### Decision 5: The key acts on the target only, with no `Control`, `Alt`, or `Super`
**Choice:** `IsKeyActivation` needs `e.Header.Target` to be the widget. It rejects a repeat (Q2) and a key with `Control`, `Alt`, or `Super` down. `Shift` does not block it.
**Rationale:** The requirement says "when it owns focus". The key route gives each key with no target to the focus owner, so the owner is the target. A key that bubbles from a focusable child does not activate the parent widget. The modifier rule copies the Tab rule of keyboard-focus, so a game shortcut such as `Alt`+Enter does not click the focused button.
**Alternatives:** (a) A check of the `Focus` bit: code can clear the bit while the element keeps the keys, and a key with a caller target would fail. (b) No modifier rule: `Alt`+Enter clicks the focused button. See Notes Q2.

### Decision 6: A disabled widget does nothing, and it does not stop the walk
**Choice:** Each check of `WidgetInput` returns `false` for `Disabled`. The handler then returns with no stop.
**Rationale:** A disabled widget does not use the event, so A4 does not apply. An ancestor handler can still see the event. A code set of `Checked` on a disabled toggle still works, because `Disabled` blocks user input only (Q4).
**Alternatives:** A stop for a disabled widget: a panel handler never sees a click on a disabled button, and the widget hides an event that it does not use.

### Decision 7: `Utf8Text` is an internal struct that each owner holds inline
**Choice:** The text fields and the static text rules move from `Label` into `Utf8Text`. `Label` keeps its public API, its exception message, and three internal static forwarders. `RefreshSubtree` in `UiContext` checks `Button` next to `Label`.
**Rationale:** Q1. A struct adds no heap object to each element, as section 5 of the technical design asks. The `bool` result keeps the dirty marks in the element, so the struct needs no element reference. The forwarders keep each `Label` test with no edit. A type check for two types is the smallest change to the refresh.
**Alternatives:** (a) A sealed class: one more object for each label and button. (b) A shared base class for `Label` and `Button`: a second class level, and `Button` is not a label. (c) An internal virtual `RefreshLocalized` on `Element`: a change of `Element.cs` for two types.

### Decision 8: The button text draws at the top-left corner of the content box
**Choice:** `Button.PaintContent` uses the draw rule of `Label`. A button with an auto size takes the size of its text plus the padding, so the text shows in the middle of the box.
**Rationale:** The requirement asks for the rules of `Label`. The style system has no text alignment yet. YAGNI.
**Alternatives:** A centre rule inside `Button`: a second draw rule, and the wrap rule of the measure no longer matches the draw. See Notes Q3.

### Decision 9: `Toggle` has no measure and no own draw
**Choice:** A `Toggle` has no measure callback and no `PaintContent` override. The paint walk draws its fill and border from the style. The size comes from a rule or from `Inline`.
**Rationale:** Q5: no text. The box and a `:checked` fill show the state. widget-sample adds the size rules. The constructor sets no `Inline` size, because an inline value wins over each rule.
**Alternatives:** A default inline size of 20 x 20: a stylesheet can then not size the toggle.

### Decision 10: The delegate runs after the stop
**Choice:** The handler calls `StopPropagation`, runs the state change, and then calls the delegate. An exception from the delegate leaves `DrainEvents`, and Decision 2 still clears `Active`.
**Rationale:** The walk state is final before game code runs. The delegate reads the new `Checked` value.
**Alternatives:** The delegate first: a throw leaves the event not stopped, with no effect, because the drain ends.

---

## Risks & Mitigation

| Risk | Likelihood | Impact | Mitigation |
|------|------------|--------|------------|
| The release order change breaks a pointer or focus rule. | Medium | High | Decision 2 moves one clear only. The tier note of phase 1. Each phase runs each pointer and focus class with no edit. `ReleaseOrderTests` covers the throw path. |
| The `Label` refactor changes a measure or a draw result. | Medium | High | The tier note of phase 4. Group A moves code with no rule change. Each `Label` test, `ElementAllocationTests`, and the ImGui label tests run with no edit before group B. |
| A copy of `Utf8Text` loses a text change. | Low | Medium | The owner holds the field with no `readonly` modifier. The acceptance check of phase 4 greps for `readonly Utf8Text`. The doc comment of the struct states the rule. |
| A game writes `button.OnPointerUp = handler` and removes the click. | Medium | Medium | The remarks of `Button` and `Toggle` state it. The game reads `Clicked` and `CheckedChanged`. |
| A game handler that it adds with `+=` to the same slot never runs on a click, because the widget stops the walk. | Medium | Low | A4 asks for the stop. The remarks state: read the delegate, or use a capture handler on an ancestor. |
| A pickable child of a button gets `Active`, and the button never clicks. | Medium | Medium | `Button` holds its own text, so a text needs no child. The remarks state: set `IsPickable = false` on a child. Notes Q1. |
| The type name `Button` hides the field `PointerEvent.Button` in some code. | Low | Low | A member lookup finds the field first inside `PointerEvent`. The build of each project in phase 1 shows a conflict at once. |
| A widget allocates in the input path. | Low | High | Decision 3. `WidgetAllocationTests` and the planted check of task 6.2. |
| A toggle with no size rule takes no space and catches no click. | Medium | Low | Decision 9. The remarks state it. widget-sample adds the rules. |

---

## Dependencies

### Internal Dependencies
- `input-routing/event-model`: `UiEventHandler<T>`, `PointerEvent`, `KeyEvent`, `UiEventHeader.CurrentTarget`, `Target`, `StopPropagation`, the ring, `PushEvent`, `DrainEvents`, and `EventWalk`.
- `input-routing/pointer-routing`: `RoutePointer`, `PressButton`, `ReleaseButton`, `ActiveElement`, `HitTest`, `PointerTree`, and Decisions 8, 9, and R1.
- `input-routing/keyboard-focus`: `IsFocusable`, `AcceptsFocus`, the click focus, the key route to the owner, Tab, `SetFocus`, and the disabled rule.
- `input-routing/routing-gate`: `Update` calls the drain, so a test can use `Update`.
- `style-system`: `PseudoStates.Checked` and `Disabled`, `Element.States` (a change marks the subtree), and the type selector (`CompoundSelector.Type`).
- `core-tree`: `Element`, `ElementType.Intern`, `TypeId`, `MarkDirty`, `PaintContent`, and `Label`.
- `imgui-renderer`: `IUiRenderer`, `RecordingRenderer`, and the ImGui label tests.

### External Dependencies
- `Brutal.Localization` (`LString`), `Brutal.Numerics`. Both are pinned already. No new package and no native dependency.
- GLFW key values: `GLFW_KEY_SPACE` is 32, and `GLFW_KEY_ENTER` is 257. The adapter sends the GLFW value as `KeyEvent.Key` (keyboard-focus).

---

## Definition of Done

All criteria below must pass. An independent evaluator runs the Verification Method from `/home/aaronstatic/dev/brutal-ui` on Linux.

### Functional Criteria
- [ ] F1. A left press and a release over a `Button`, pushed with no target and followed by `Update`, call `Clicked` one time with the button as the sender (`ButtonClickTests.APressAndAReleaseClickTheButtonOnce`).
- [ ] F2. A release over another element, a release after a press elsewhere, and a right button give no click. A click stops the release walk. A press focuses the button. A type rule for `Button` matches it (`ButtonClickTests`, 8 tests).
- [ ] F3. A `PointerUp` handler reads `Active` on the pressed element. After the drain, no element holds `Active`, also after a handler throws (`ReleaseOrderTests`, 4 tests).
- [ ] F4. A focused button clicks on an Enter or a Space key down. A repeat, a key up, another key, `Control`, `Alt`, `Super`, and a key from a focusable child give no click. Tab and then Enter clicks (`ButtonKeyTests`, 9 tests).
- [ ] F5. A click and a Space flip a focused `Toggle`. `Checked` matches `PseudoStates.Checked`, and a `Toggle:checked` rule shows after `Update`. Enter and a repeat do nothing. A code set and the create-info value call no `CheckedChanged` (`ToggleTests`, 10 tests).
- [ ] F6. A `Button` with a text draws one `Text` call with its bytes at the top-left corner of its content box, and it takes the size of its text plus the padding. The `SetText` overloads mark `Layout` and `Paint` for a new text only. `RefreshLocalizedText` reaches a button (`ButtonTextTests`, 9 tests).
- [ ] F7. A disabled button and a disabled toggle raise nothing, flip nothing, and do not stop the walk. A code set of `Checked` works on a disabled toggle (`WidgetDisabledTests`, 7 tests).
- [ ] F8. `WidgetAllocationTests` measures 0 bytes for `Update` and `Draw`, for the full input path, and for `SetText` (4 tests). A planted allocation in `Button.HandlePointerUp` gives a non-zero exit code of `dotnet test`.

### Quality Criteria
- [ ] Q1. `dotnet build brutal-ui.sln` gives `0 Warning(s)` and `0 Error(s)`.
- [ ] Q2. `dotnet test --solution brutal-ui.sln` gives 0 failed. `BrutalUi.Core.Tests` has 51 more tests than the count of task 1.1 (4 + 8 + 9 + 10 + 9 + 7 + 4).
- [ ] Q3. `Button` and `Toggle` are `public sealed`. `WidgetInput` and `Utf8Text` are internal. No create-info member is `required`.
- [ ] Q4. `Button.cs` and `Toggle.cs` create no delegate outside a `static readonly` field initializer, and hold no lambda.
- [ ] Q5. No existing test file has a diff. Each `Label` test passes with no edit.
- [ ] Q6. The changed docs and the new doc comments use ASD-STE100 prose. The lint gives no score above 2.5.

### Integration Criteria
- [ ] I1. `BrutalUi.Core` has no ImGui or GLFW `using`, and its `.csproj` has no diff.
- [ ] I2. `Element.cs`, `PseudoStates.cs`, `ElementHandlers.cs`, `EventWalk.cs`, `HitWalk.cs`, `FocusWalk.cs`, `PaintWalk.cs`, `Directory.Packages.props`, `samples/`, and `scripts/` have no diff.
- [ ] I3. In `UiContext.cs`, only `ReleaseButton`, the new `FinishRelease`, its two calls in `DrainEvents`, the new field, `RefreshSubtree`, and doc comments change.
- [ ] I4. Section 5 of `docs/technical-design.md` holds the widget pattern and the amended pointer step. Section 9 item 4 states the release order. `CHANGELOG.md` names `Button`, `Toggle`, and the release order.
- [ ] I5. `scripts/test-linux.sh` prints its PASS line.

### Verification Method

Run each step from `/home/aaronstatic/dev/brutal-ui` on Linux. `<scratch>` is any empty folder outside the repository.

1. `dotnet build brutal-ui.sln 2>&1 | tail -n 30`. Expect `0 Warning(s)` and `0 Error(s)` (Q1).
2. `dotnet test --solution brutal-ui.sln 2>&1 | tail -n 30`. Expect 0 failed. Compare the Core total with the count of task 1.1 in `context.md` (Q2).
3. For each class, run `dotnet run --project tests/BrutalUi.Core.Tests/BrutalUi.Core.Tests.csproj -- --filter-class '*<Class>' 2>&1 | tail -n 5`. Expect `failed: 0` and these totals: `ReleaseOrderTests` 4 (F3), `ButtonClickTests` 8 (F1, F2), `ButtonKeyTests` 9 (F4), `ToggleTests` 10 (F5), `ButtonTextTests` 9 (F6), `WidgetDisabledTests` 7 (F7), and `WidgetAllocationTests` 4 (F8). Read the test names in each file and match them to the criteria.
4. Copy `src/BrutalUi.Core/Elements/Button.cs` to `<scratch>`. Add `GC.KeepAlive(new object());` as the first line of `HandlePointerUp`. Run `dotnet test --solution brutal-ui.sln > /dev/null 2>&1; echo "exit=$?"`. Expect a value that is not 0. Restore the file from the copy, and check it with `cmp`. Run step 2 again (F8).
5. `git diff main --diff-filter=M --name-only -- tests/`. Expect no line (Q5). `git diff main --stat -- src/BrutalUi.Core/Tree/Element.cs src/BrutalUi.Core/Tree/PseudoStates.cs src/BrutalUi.Core/Input/ src/BrutalUi.Core/Rendering/PaintWalk.cs Directory.Packages.props '*.csproj' samples/ scripts/`. Expect no output (I1, I2).
6. `git diff main -- src/BrutalUi.Core/Tree/UiContext.cs`. Read the diff. Expect changes in `ReleaseButton`, `FinishRelease`, `DrainEvents` (two calls), `RefreshSubtree`, the new field, and doc comments only (I3).
7. `grep -nE '^public sealed class (Button|Toggle)' src/BrutalUi.Core/Elements/*.cs` gives two lines. `grep -nE '^internal (static class WidgetInput|struct Utf8Text)' src/BrutalUi.Core/Elements/*.cs` gives two lines. `grep -n 'required' src/BrutalUi.Core/Elements/Button.cs src/BrutalUi.Core/Elements/Toggle.cs` gives no line. `grep -nE '\(ref [A-Za-z]+ e\) =>|new UiEventHandler' src/BrutalUi.Core/Elements/Button.cs src/BrutalUi.Core/Elements/Toggle.cs` gives no line, and `grep -n 'static readonly UiEventHandler' src/BrutalUi.Core/Elements/Button.cs src/BrutalUi.Core/Elements/Toggle.cs` gives four lines (Q3, Q4). `grep -rn 'readonly Utf8Text' src/` gives no line.
8. `grep -rnE 'using .*(ImGui|Glfw)' src/BrutalUi.Core` gives no line (I1).
9. `scripts/test-linux.sh; echo "exit=$?"`. Expect the PASS line and `exit=0` (I5, the demo of phase 1).
10. Read section 5 ("Widgets", "The text of a label", "The locale refresh", "The pointer step of the drain"), section 9 item 4, and section 14 of `docs/technical-design.md`, and the `## [Unreleased]` part of `CHANGELOG.md` (I4). Run `python3 /home/aaronstatic/.claude/skills/asd-ste100/scripts/ste-lint.py --fail-over 2.5` on each changed doc (Q6).

---

## Testing Strategy

### Unit Tests
- `Input/ReleaseOrderTests` (4): the release order of Decision 2 on `PointerTree`.
- `Elements/ButtonClickTests` (8): the click, the misses, the right button, the stop, the null delegate, the focus, and the type rule.
- `Elements/ButtonKeyTests` (9): Enter, Space, the repeat, the key up, another key, the modifiers, the stop, Tab then Enter, and a key from a child.
- `Elements/ToggleTests` (10): the flip by click and by Space, Enter, the repeat, the code set, the same value, the start value, the misses, and the bit.
- `Elements/ButtonTextTests` (9): the draw, the size, the three overloads and their compares, the refresh, the two-text throw, the measurer message, and a click on the text.
- `Elements/WidgetDisabledTests` (7): the disabled click, keys, and flip, the disable during a press, the enable, the code set, and the focus.
- Each widget test uses `WidgetTree`, pushes events with no target through `UiContext.PushEvent`, and calls `Update` or `DrainEvents`. A key test with a disabled widget uses a caller target, because the drain drops a disabled owner.

### Allocation Tests
- `Elements/WidgetAllocationTests` (4): `Update` and `Draw`, the input path, `SetText`, and the real-work check. widget-sample adds the 1000-element gate with each widget (epic A9).

### Regression Tests
- Each `Label` test (`LabelTests`, `LabelTextTests`, `LabelWrapTests`, `ContentBoxTests`, `PaintContentTests`, `ElementAllocationTests`, and the ImGui label tests) with no edit.
- Each pointer and focus class with no edit.

### Integration Tests
- `scripts/test-linux.sh` runs the full suite and the HelloPanel smoke run. HelloPanel does not change. widget-sample adds the widgets to it.

### Manual Testing
- None in this feature. widget-sample adds the visible demo.

---

## Related Documentation

- **Requirements:** `docs/features/interactive-elements/button-and-toggle/requirements.md`
- **Epic:** `docs/features/interactive-elements/epic-overview.md`, `docs/features/interactive-elements/epic-requirements.md`
- **Siblings:** `docs/features/interactive-elements/slider/requirements.md`, `scroll-view/requirements.md`, `text-field/requirements.md`, `widget-sample/requirements.md`
- **Input routing:** `docs/features/input-routing/pointer-routing/implementation.md` (Decisions 8, 9, R1), `docs/features/input-routing/keyboard-focus/implementation.md` (Decisions 4, 5, 7, and the plan format)
- **Design:** `docs/technical-design.md` sections 5 (Element Tree, Events), 7 (pseudo-states), 9 (Input), and 14 (Testing)
- **Rules:** `AGENTS.md`, `.beast-mode/phase-handoff.md` sections 1 and 11, `.beast-mode/tool-rules.md`
- **Code patterns:** `src/BrutalUi.Core/Elements/Label.cs` (the text rules, the cached measure callback), `src/BrutalUi.Core/Elements/Image.cs` (the sealed element shape), `src/BrutalUi.Core/Tree/UiContext.cs` (`DrainEvents`, `RoutePointer`, `ReleaseButton`, `RefreshSubtree`), `tests/BrutalUi.Core.Tests/Input/PointerTree.cs` (the event factories), `tests/BrutalUi.Core.Tests/Input/FocusAllocationTests.cs` and `tests/BrutalUi.Core.Tests/ElementAllocationTests.cs` (the gates)
- **Skills:** no implementation skill in `.claude/skills/` applies. The skills there are workflow commands.

---

## Notes

### Open Scope Questions

The plan uses the recommended option (the first one) of each question. The demo of phase 1 comes from the confirmed item Q6.

**Q1. What does a pickable child of a `Button` do?**
- **A (recommended):** Nothing new. `Active` goes to the hit only (Decision R1 of pointer-routing). A press on a pickable child gives the child `Active`, and the button does not click. The remarks tell the game to set `IsPickable = false` on a child. Reason: the confirmed rule says "while it holds `Active`", and the button holds its own text, so the common case needs no child.
- B: Click when the release target holds `Active`. A press and a release on the same child click the button. It reads "it holds `Active`" as "its subtree holds `Active`", which differs from the confirmed wording.

**Q2. Do `Control`, `Alt`, and `Super` block Enter and Space?**
- **A (recommended):** Yes (Decision 5). Reason: `Alt`+Enter is a common game shortcut, and the Tab rule of keyboard-focus uses the same three modifiers.
- B: No. Any Enter or Space with no repeat activates.

**Q3. Where does the button text draw?**
- **A (recommended):** At the top-left corner of the content box, as `Label` (Decision 8). An auto-size button shows its text in the middle.
- B: In the middle of the content box. It needs a second draw rule and a wrap rule of its own. A text alignment style property is a better home, in a later feature.

**Q4. Does the keypad Enter key (GLFW 335) click?**
- **A (recommended):** No. Q2 names Enter and Space only. A later feature or the action map can add it with one more flag in `ActivationKeys`.
- B: Yes. Add `KeypadEnterKey = 335` to the Enter flag.

### Conflicts and Gaps

1. **The Active order (main finding).** The confirmed rule "a click on a pointer up while it holds `Active`" and the requirement A4 conflict with the code. `RoutePointer` calls `ReleaseButton` before the walk, and `ReleaseButton` clears `Active` (`src/BrutalUi.Core/Tree/UiContext.cs`, `RoutePointer` and `ReleaseButton`). A `PointerUp` handler therefore never reads `Active`. The plan keeps the confirmed item. Decision 2 moves the clear to the end of the release entry. This amends Decision 9 of pointer-routing ("The state changes before the walk") for the release. No existing test reads `Active` inside a release handler, so no test changes. Task 6.3 updates section 5 of the technical design, which states "so a handler reads the new state".
2. **The slot names.** The brief names the slots `PointerUp` and `KeyDown`. On `Element`, the public slots are `OnPointerUp` and `OnKeyDown`. `PointerUp` and `KeyDown` are the internal fields of `ElementHandlers`. The plan uses the public properties, as A2 states.
3. **"A static method plus a target object".** The requirement and section 5 of the technical design use this wording. A2 makes the handler a static method that reads `e.Header.CurrentTarget`. The delegate then has no target object, and the widget comes from the event. No confirmed item conflicts: A2 settles the reading.
4. **The epic A4 and the toggle keys.** `epic-requirements.md` says "Toggle and Slider activate on Space and the arrow keys". The feature requirement and the confirmed A3 say that only Space flips a toggle. The plan follows A3. The arrow keys belong to the slider.
5. **The 1000-element gate.** The epic asks for a gate with each widget in the 1000-element tree. Epic item A9 gives that gate to widget-sample. This feature adds a smaller gate with buttons and toggles only.
6. **A button with children.** The engine calls a measure callback for an element with no children only. A `Button` with an `Image` child (the icon of the out-of-scope list) does not measure its text. The remarks of `Button` state the limit. A later icon feature decides the rule.
7. **The toggle size.** A `Toggle` has no text and no measure (Q5, Decision 9). With no size rule, it takes no space. widget-sample adds the size rules to HelloPanel.
8. **The name `Button`.** `PointerEvent` has a public field `Button` of type `PointerButton`. A new type `BrutalUi.Core.Button` shares the name. Inside `PointerEvent`, the member lookup finds the field first, and each other use is a member access. The build of phase 1 confirms it.
9. **The prose lint.** The planner had no shell, so it did not run `ste-lint.py` on this file. Task 6.5 runs it.

---

*This plan will be updated as implementation progresses. See context.md and tasks.md for current status.*
