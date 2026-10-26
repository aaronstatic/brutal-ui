# Slider - Implementation Plan

**Epic:** interactive-elements (feature 2 of 5)
**Status:** Planning
**Started:** 2026-10-26
**Target Completion:** TBD
**Last Updated:** 2026-10-26 14:00

---

## Quality Bar

**Target quality:** A core library feature. It adds the first drag rule and the first continuous value of the widget set. scroll-view copies the drag rule and the drag capture. A wrong value, a lost drag, a drag that does not end, a key that acts on the wrong element, or a hidden allocation here breaks each game that uses a slider, and scroll-view copies the defect.

- **The value is exact:** Each path that changes `Value` uses one rule: a NaN gives `Minimum`, the value clamps to the range, rounds to the nearest step from `Minimum`, and clamps again. A test asserts each case of the rule.
- **The pointer maps exactly:** The center of the thumb follows the pointer. A press or a drag before the track gives `Minimum`, and past the track gives `Maximum`. The map uses the content box of the slider in viewport pixels, the same space as the hit test.
- **A drag does not get lost:** A drag that leaves the slider rect still moves the value (Decision 3). The capture ends with the release of the last button.
- **One change, one call:** Each user change of the value calls `ValueChanged` one time, after the change. A user input that gives the same value calls nothing. A code set calls nothing.
- **Disabled means no effect:** A slider with `PseudoStates.Disabled` takes no press, no drag, no key, and no capture, and it does not stop the walk.
- **0 bytes after warm-up:** `Update`, `Draw`, a press, a drag, a captured drag, and an arrow key allocate 0 bytes. A planted allocation fails a gate.
- **No routing change for other elements:** The capture applies only to an element that opts in. Each pointer, focus, and widget test passes with no edit.
- **API clarity:** Each public member has an XML doc comment in ASD-STE100 prose. The drag rule is in this plan and in section 5 of the technical design, so scroll-view copies it with no guess.
- **No ImGui in Core:** `BrutalUi.Core` gets no package and no ImGui `using`.

---

## Executive Summary

This feature adds `Slider`, a horizontal widget that holds a `float` value in a range. It is a sealed `Element` subclass in `src/BrutalUi.Core/Elements/`, with the type id `Slider`. It copies the widget pattern of button-and-toggle (its section "The Widget Pattern"). The constructor sets `IsFocusable = true` and writes cached static delegates into `OnPointerDown`, `OnPointerMove`, and `OnKeyDown`. Three new checks in `WidgetInput` hold the drag start (a left press while the widget holds `Active`), the drag move (a move while the widget holds `Active`), and the arrow key (a `KeyDown` of Left or Right with the widget as the target, repeat allowed). The slider maps the pointer x to a value through its content box. The arrow keys move the value by one step, or by one percent of the range when the step is 0. `ValueChanged` gets the slider as the sender. The paint walk draws the box of the slider as the track. `PaintContent` draws the thumb, a square of the content height, in the `Color` of the computed style. A rule such as `Slider:active { color: ... }` styles the thumb through the existing pseudo-states.

The codebase check found one gap. A pointer move with no target walks to the hit element only (`UiContext.RoutePointer`). Nothing routes a move to the element that holds `Active`. A drag that leaves the slider rect thus gives no move to the slider, and the value stops short of the end. Phase 4 adds an opt-in drag capture: an internal interface `IDragCapture`. When the element that holds `Active` implements it and asks for the moves, `RoutePointer` sends each move with no target to that element. `Hover` and `PointerHit` still follow the hit, so the routing gate and the hover rules do not change. No existing element implements the interface, so each existing test passes with no edit (Decision 3).

---

## User Personas Alignment

No `docs/user-personas.md` file exists. The personas come from `docs/mission-statement.md` and the epic docs.

**Target Users:**
- [x] **The game developer** (creates a `Slider` with a range and a step, sets `ValueChanged`, writes `Slider`, `Slider:hover`, `Slider:active`, `Slider:focus`, and `:disabled` rules, reads `Value`, and sets `Value` from a save file with no call)
- [x] **The sibling feature agents** (scroll-view copies the drag rule, calls `WidgetInput.IsDragStart` and `IsDragMove`, implements `IDragCapture`, and adds its scroll offset to `WidgetInput.ViewportOrigin`)
- [x] **The widget-sample feature** (puts a slider in HelloPanel, adds the stylesheet rules, and adds the slider to the 1000-element gate)
- [x] **The Phase 3 framework epic** (binds a store to `Value` and to `ValueChanged`)
- [x] **The maintainer** (runs one more 0-byte gate)
- [ ] **The markup author** (not served. The markup epic registers `Slider` by name)

**User Subsets Served:**
- [x] **Simple** - `var volume = new Slider(new Slider.CreateInfo { Maximum = 100, Value = 80 }); volume.ValueChanged = OnVolume;` and a `Slider:active { color: ... }` rule.
- [x] **Complex** - a slider with `Step = 5`, `SetRange` after a level change, a code set of `Value` with no call, `States |= PseudoStates.Disabled` on a locked option, Tab and the arrow keys through a menu, and one static method that serves many sliders through the sender.

**UX Approach:**
- The game reads one delegate. It never reads the raw slot.
- The thumb center follows the pointer, so a press on the track moves the thumb under the pointer.
- A held arrow key repeats, so the thumb moves while the key stays down.

---

## Goals

### Primary Goals
1. Add `Slider`: a press on the track moves the value to the pointer. One `ValueChanged` call for each change.
2. Drag the value while the slider holds `Active`, also outside the slider rect.
3. Round the value to the step, and keep it in the range for each path.
4. Move the value with the Left and Right arrow keys while the slider owns focus.
5. Draw the thumb from the computed style, and let the stylesheet style it through the pseudo-states.
6. Block each input on a slider that holds `Disabled`.
7. Allocate 0 bytes in `Update`, `Draw`, and the input path after warm-up.
8. Write the drag rule and the capture down for scroll-view.

### Success Criteria
- [ ] The phase 1 demo passes: `scripts/test-linux.sh` prints its PASS line, and the press test in it reads `Value` 25 and one `ValueChanged` call.
- [ ] The press, draw, drag, capture, step, key, and disabled test classes pass.
- [ ] Each pointer, focus, and button-and-toggle test passes with no edit.
- [ ] `SliderAllocationTests` measures 0 bytes, and a planted allocation in `Slider.HandlePointerMove` fails it.
- [ ] Section 5 of the technical design holds the slider rules, the drag rule, and the capture.

---

## Architecture Overview

### The Move Route Finding

`DrainEvents` calls `RoutePointer` for each pointer entry with no target. `RoutePointer` hit-tests the position, moves `Hover`, writes the hit into `entry.Header.Target`, and returns the hit. For a `PointerMove`, it changes no other state (Decision 8 of pointer-routing). The walk then goes from the hit up to the root. Thus a move reaches the slider only when the pointer is over the slider or over a descendant of it. A move over another element, or over an ancestor of the slider, does not reach the slider. No pointer capture exists in the code (`grep -rn Capture src/BrutalUi.Core` finds only the capture slots of the walk).

The requirement says "a pointer move while the slider holds `Active` drags the value". With the route of today, a fast drag that leaves the rect stops the value short of the end. Phase 3 delivers the drag inside the rect. Phase 4 adds the opt-in capture of Decision 3.

### File Tree (end state)

```
src/BrutalUi.Core/
├── Elements/Slider.cs             # phase 1 (press), 2 (thumb draw), 3 (drag), 4 (capture), 5 (step, keys), 6 (disabled)
├── Elements/WidgetInput.cs        # phase 1 (IsDragStart, ViewportOrigin), 3 (IsDragMove), 5 (IsArrowKey, keys), 6 (Disabled)
├── Input/IDragCapture.cs          # phase 4
└── Tree/UiContext.cs              # phase 4 (RoutePointer sends a captured move to the Active element)
tests/BrutalUi.Core.Tests/
├── Elements/SliderTree.cs         # phase 1
├── Elements/SliderPressTests.cs   # phase 1
├── Elements/SliderDrawTests.cs    # phase 2
├── Elements/SliderDragTests.cs    # phase 3
├── Input/DragCaptureTests.cs      # phase 4
├── Elements/SliderStepTests.cs    # phase 5
├── Elements/SliderKeyTests.cs     # phase 5
├── Elements/SliderDisabledTests.cs # phase 6
└── Elements/SliderAllocationTests.cs # phase 7
docs/technical-design.md           # phase 7: sections 5, 9, 14
CHANGELOG.md                       # phase 7
docs/features/interactive-elements/slider/context.md   # each phase
```

These files do not change: `Element.cs`, `PseudoStates.cs`, `ElementHandlers.cs`, `EventWalk.cs`, `HitWalk.cs`, `FocusWalk.cs`, `PaintWalk.cs`, `StylePass.cs`, `StyleProperties.cs`, `Button.cs`, `Toggle.cs`, `Label.cs`, `Utf8Text.cs`, each `.csproj`, `Directory.Packages.props`, `samples/`, `scripts/`, and each existing test file (also `WidgetTree.cs` and `WidgetAllocationTests.cs`).

### Public API (end state)

```csharp
namespace BrutalUi.Core;

public sealed class Slider : Element
{
    public Slider(in CreateInfo info);                  // phase 1. Phase 5: throws ArgumentOutOfRangeException for a bad range
    public override ElementTypeId TypeId { get; }       // phase 1: the id of "Slider"
    public float Value { get; set; }                    // phase 1: clamps. Phase 5: rounds to the step, NaN gives Minimum. A set calls nothing
    public float Minimum { get; }                       // phase 1
    public float Maximum { get; }                       // phase 1
    public float Step { get; }                          // phase 5
    public void SetRange(float minimum, float maximum, float step); // phase 5: checks the three values as one. Calls nothing
    public Action<Slider>? ValueChanged { get; set; }   // phase 1 (Q3 of the epic)

    public struct CreateInfo
    {
        public float Minimum;                           // phase 1. The default is 0
        public required float Maximum;                  // phase 1. No safe default (Decision 9)
        public float Value;                             // phase 1. The start value. It clamps, and it calls nothing
        public float Step;                              // phase 5. The default is 0: no rounding
    }
}
```

`Slider` implements the internal `IDragCapture` explicitly (phase 4). The interface does not show in the public API.

### Internal Types

- `WidgetInput` (`src/BrutalUi.Core/Elements/WidgetInput.cs`), additions:
  - `internal static float2 ViewportOrigin(Element element)`: the sum of `Layout.Rect.X` and `Layout.Rect.Y` of the element and each ancestor. It is the origin rule of `HitWalk` and `PaintWalk`. The doc comment states: scroll-view adds its scroll offset here. Phase 1.
  - `internal static bool IsDragStart(Element widget, in PointerEvent e)`: the type is `PointerDown`, the button is `Left`, and `widget.States` holds `Active`. Phase 1. Phase 6 adds: and no `Disabled`.
  - `internal static bool IsDragMove(Element widget, in PointerEvent e)`: the type is `PointerMove`, and `widget.States` holds `Active`. The check does not read `e.Button`, because the walk does not read it for a move. Phase 3. Phase 6 adds: and no `Disabled`.
  - `internal const int RightKey = 262;` (`GLFW_KEY_RIGHT`) and `internal const int LeftKey = 263;` (`GLFW_KEY_LEFT`). Phase 5.
  - `internal static bool IsArrowKey(Element widget, in KeyEvent e, out int direction)`: the type is `KeyDown`, `e.Header.Target` is `widget`, no `Control`, `Alt`, or `Super`, and the key is `LeftKey` (direction -1) or `RightKey` (direction +1). A repeat passes. `Shift` does not block it. Phase 5. Phase 6 adds: and no `Disabled`.
- `IDragCapture` (`src/BrutalUi.Core/Input/IDragCapture.cs`): `internal interface IDragCapture { bool CapturesMoves { get; } }`. Phase 4. When the element that holds `Active` implements it and `CapturesMoves` is `true`, `RoutePointer` sends each `PointerMove` with no target to that element.
- `Slider` private members: `_value`, `_minimum`, `_maximum`, `_step` (floats). `Normalize(float)` (the value rule of Decision 4). `bool Apply(float)` (normalizes, compares, writes, marks `DirtyFlags.Paint`, returns `true` for a change). `UserSet(float)` (calls `Apply`, then `ValueChanged` on `true`). `ValueAtPointer(float x)` (the map of Decision 6). Two `internal static` geometry helpers that the press and the draw share: `ThumbWidth(in float4 content)` (the smaller of the content height and the content width) and `Ratio(float value, float minimum, float maximum)` (0 for a zero range).

### The Drag Rule (for scroll-view)

scroll-view copies this shape. Its plan cites this section.

1. **The start.** A static `HandlePointerDown` on `OnPointerDown`. It runs `WidgetInput.IsDragStart`. On `true`, it stops the walk and runs the own step (the slider moves the value to the pointer).
2. **The move.** A static `HandlePointerMove` on `OnPointerMove`. It runs `WidgetInput.IsDragMove`. On `true`, it stops the walk and runs the own step. It stops the walk also when the value does not change, because the widget uses the move.
3. **The state.** The drag reads `Active` only. The widget holds no drag flag (Decision 2). The drag ends when `FinishRelease` clears `Active`.
4. **The capture.** The widget implements `IDragCapture` explicitly. `CapturesMoves` returns `true` when the widget takes a drag now (the slider: no `Disabled`). Then each move with no target reaches the widget during the drag, also outside its rect.
5. **The position.** The widget maps `e.Position` through `WidgetInput.ViewportOrigin(widget)` and `Layout.ContentBox`. Both read the layout of the last `Update`, as the hit test does.
6. **The limit.** `Active` marks the first press of a gesture (CR-1 of button-and-toggle). A right press on the widget sets `Active` too, so a later move drags. The press itself needs the left button.

```csharp
private static void HandlePointerMove(ref PointerEvent e)
{
    if (e.Header.CurrentTarget is not Slider slider || !WidgetInput.IsDragMove(slider, in e))
    {
        return;
    }

    e.Header.StopPropagation();
    slider.UserSet(slider.ValueAtPointer(e.Position.X));
}
```

### Data Flow

```
Game: ui.PushEvent(PointerDown at p) ... ui.PushEvent(PointerMove at q) ... ui.PushEvent(PointerUp at r) ... ui.Update(dt)
UiContext.DrainEvents
  PointerDown, no target:
    RoutePointer: hit = Slider. Hover on Slider. PressButton: Active on Slider. FocusOnPress: Slider owns focus
    walk: Slider.HandlePointerDown
          WidgetInput.IsDragStart: PointerDown, Left, Active, no Disabled ──► StopPropagation ──► UserSet(ValueAtPointer(p.X))
  PointerMove, no target:
    RoutePointer: hit = H. Hover on H. PointerHit = H.
                  Active element is IDragCapture { CapturesMoves: true } ──► target = Slider            (phase 4)
    walk: Slider.HandlePointerMove
          WidgetInput.IsDragMove: PointerMove, Active, no Disabled ──► StopPropagation ──► UserSet(ValueAtPointer(q.X))
  PointerUp, no target:
    RoutePointer: ReleaseButton. walk to the hit (the slider has no PointerUp handler). FinishRelease: Active goes off. The capture ends
  KeyDown Left or Right, no target:
    target = FocusedElement (the Slider)
    walk: Slider.HandleKeyDown
          WidgetInput.IsArrowKey: KeyDown, target, no Control, Alt, Super, Left or Right, no Disabled (a repeat passes)
          ──► StopPropagation ──► UserSet(Value + direction * (Step > 0 ? Step : (Maximum - Minimum) / 100))
UserSet: Apply(Normalize(v)) ──► on a change: _value = v, MarkDirty(Paint) ──► ValueChanged(slider)
UiContext.Update: the style pass restyles the subtrees that the state changes marked (:hover, :active, :focus)
UiContext.Draw: the paint walk draws the box of the slider (the track) ──► Slider.PaintContent draws the thumb in style.Color
```

---

## Implementation Phases

Each phase is a slice: one narrow path that works end to end, with a demo line that a person can check. Section 11 of `.beast-mode/phase-handoff.md` holds the rules. No interview ran, so Notes Q1 names the demo of phase 1, and phase 1 uses the recommended line.

Each phase ends with `dotnet build brutal-ui.sln 2>&1 | tail -n 30` and `dotnet test --solution brutal-ui.sln 2>&1 | tail -n 30` (tool rule 5). Expect `0 Warning(s)`, `0 Error(s)`, and 0 failed. The projects have no implicit usings, so each new file holds its own `using` lines. Each public member needs an XML doc comment (CS1591 is an error). Use `namespace BrutalUi.Core;` in Core and `namespace BrutalUi.Core.Tests;` in the tests. Prose in comments follows ASD-STE100. A test class that interns a class name uses a name prefix that is unique to the class, because xUnit runs the classes in parallel. Run one class with `dotnet run --project tests/BrutalUi.Core.Tests/BrutalUi.Core.Tests.csproj -- --filter-class '*<Class>' 2>&1 | tail -n 5`. Each phase runs these classes with no edit: the pointer and focus classes (`PointerPressTests`, `PointerStateEdgeTests`, `PointerHoverTests`, `PointerAllocationTests`, `ClickFocusTests`, `FocusOwnerTests`, `TabNavigationTests`, `FocusStateEdgeTests`, `FocusAllocationTests`, `RoutingGateTests`) and the button-and-toggle classes (`ReleaseOrderTests`, `ButtonClickTests`, `ButtonKeyTests`, `ToggleTests`, `ButtonTextTests`, `WidgetDisabledTests`, `WidgetAllocationTests`).

The test scene of each slider test is `SliderTree` (task 1.4). Its slider is 120 x 20 at (40, 0) with no padding and a range of 0 to 100. The thumb is 20 x 20, so the thumb center moves over 100 pixels. The value `v` thus sits at the x `50 + v`.

### Phase 1: A Press Moves the Value
**Goal:** The smallest path that proves the idea: a left press on the track of a `Slider` moves `Value` to the pointer and calls `ValueChanged` one time, with the slider as the sender.
**Demo:** a person runs `scripts/test-linux.sh` and reads its PASS line. In that run, `SliderPressTests.APressMovesTheValueToThePointer` pushes a left press at x 75 over a `Slider` (range 0 to 100) through `UiContext.PushEvent`, runs `Update`, and reads `Value` 25 and one `ValueChanged` call with the slider as the sender.
**Needs:** none
**Agent tier:** STANDARD (sonnet)

**Tasks:**
- [ ] 1.1 Record in `docs/features/interactive-elements/slider/context.md`, before any change: the base commit (`git rev-parse HEAD`), the test counts of the solution and of `BrutalUi.Core.Tests`, and the counts of each class that the phase intro names. Record the Move Route Finding of the Architecture Overview.
- [ ] 1.2 Edit `src/BrutalUi.Core/Elements/WidgetInput.cs`: add `ViewportOrigin(Element)` and `IsDragStart(Element, in PointerEvent)` (Internal Types). Extend the class remarks: the drag checks serve each widget with a drag rule. Keep `IsClick`, `IsKeyActivation`, and the key constants as they are.
- [ ] 1.3 Write `src/BrutalUi.Core/Elements/Slider.cs` by the widget pattern: sealed, the type id of `Slider`, `public Slider(in CreateInfo info)`, `CreateInfo` with `Minimum`, `required Maximum`, and `Value` (phase 5 adds `Step`), `IsFocusable = true`, `OnPointerDown = s_pointerDown`, the get-only `Minimum` and `Maximum`, `Value` (a set calls `Apply`: clamp, compare, write, mark `DirtyFlags.Paint`), `public Action<Slider>? ValueChanged { get; set; }`, `UserSet`, `ValueAtPointer` (Decision 6), the geometry helpers, and the static `HandlePointerDown`. The constructor writes the start value through `Apply` and calls nothing. The remarks state: the press rule, the map of the thumb center, the stop (A4 of button-and-toggle), the sender, "read `ValueChanged`, not the slot", "a write of `=` to `OnPointerDown` removes the press", a code set calls nothing (Decision 5), and the slider has no own size (Decision 10).
- [ ] 1.4 Write `tests/BrutalUi.Core.Tests/Elements/SliderTree.cs`: an internal test tree. The context has a `RecordingRenderer` measurer and a viewport of 800 x 600. The root is 400 x 300 in a row and is pickable. The children are `Lead`, a plain `Element` (40 x 40 at 0, 0), `Slider` (120 x 20 at 40, 0, no padding), and `Other`, a plain `Element` (100 x 100 at 160, 0). The constructor takes `in Slider.CreateInfo`. A second constructor uses the range 0 to 100 and the value 0. It holds `static float2 At(float value)` (x `50 + value`, y 10), the points `OverLead` (20, 10), `OverOther` (210, 50), `PastRight` (300, 10, over the root), and `OverNothing` (600, 500), and a `Settle()` method. Use the factories of `PointerTree` for the events. The doc comment states the rects and the map.
- [ ] 1.5 Write `tests/BrutalUi.Core.Tests/Elements/SliderPressTests.cs` with 10 tests: `APressMovesTheValueToThePointer` (the demo, through `PushEvent` and `Update`), `APressBeforeTheTrackGivesTheMinimum` (start 50, press at x 45), `APressPastTheTrackGivesTheMaximum` (press at x 155), `APressAtTheSameValueCallsNothing` (no call and no `DirtyFlags.Paint` after a settle), `ARightButtonPressDoesNothing` (the value stays, and the root bubble handler gets the press), `APressStopsTheWalk` (the root capture handler runs, the root bubble handler does not), `APressFocusesTheSlider` (`IsFocusable` is `true`, and `FocusedElement` is the slider), `ACodeSetClampsAndCallsNothing` (150 gives 100, -5 gives 0, a change marks `Paint`, the same value marks nothing, the call count stays 0), `TheCreateInfoSetsTheRangeAndTheStartValue` (a start value outside the range clamps, no call, and the type id is `Slider`), and `ATypeRuleMatchesTheSlider` (a rule with `Type = ElementType.Intern("Slider")` sets a fill on the slider and not on `Other`).
- [ ] 1.6 Run the build, the tests, the demo class, the classes of the phase intro, and `scripts/test-linux.sh; echo "exit=$?"`. Expect the PASS line and `exit=0`. Record the counts and each decision that this plan did not settle in `context.md`.

**Estimated Steps:** about 40 (6 tasks, about seven steps for one task)

**Files (5):** `WidgetInput.cs`, `Slider.cs`, `SliderTree.cs`, `SliderPressTests.cs`, `context.md`

**Acceptance Criteria:**
- [ ] The 10 tests of `SliderPressTests` pass.
- [ ] Each class of the phase intro passes with no edit.
- [ ] `scripts/test-linux.sh` prints its PASS line.
- [ ] `UiContext.cs`, `Element.cs`, and `EventWalk.cs` have no diff against the base commit.

---

### Phase 2: The Thumb Draw
**Goal:** The slider shows its value. The paint walk draws the box of the slider as the track. `PaintContent` draws the thumb in the `Color` of the computed style, and a pseudo-state rule restyles it.
**Demo:** a unit test sets a `Slider` rule with a background color and a color, sets the value 25, and runs `Update` and `Draw` with a `RecordingRenderer`. It reads the track rect (40, 0, 120, 20) in the background color and then the thumb rect (65, 0, 20, 20) in the color. The developer runs `dotnet run --project tests/BrutalUi.Core.Tests/BrutalUi.Core.Tests.csproj -- --filter-class '*SliderDrawTests'` and reads a total of 6 with `failed: 0`.
**Needs:** Phase 1
**Agent tier:** STANDARD (sonnet)

**Tasks:**
- [ ] 2.1 Edit `src/BrutalUi.Core/Elements/Slider.cs`: override `internal override void PaintContent(IUiRenderer renderer, in float4 borderBox, float opacity)` (Decision 7). Compute the content box with `Layout.ContentBox(in borderBox)`. Return with no call when the content width or height is 0 or less, or when `Style.Color.ScaleAlpha(opacity)` has an alpha of 0. Else draw one `renderer.Rect` with the x `content.X + Ratio * (content.Z - ThumbWidth)`, the y `content.Y`, the width `ThumbWidth`, the height `content.W`, the scaled color, `Style.CornerRadius`, a border width of 0, and `Rgba8.Transparent`. `ValueAtPointer` uses the same helpers. Extend the remarks: the track is the box of the slider, the thumb uses `color`, and `Slider:hover`, `Slider:active`, `Slider:focus`, and `:disabled` rules style the thumb.
- [ ] 2.2 Write `tests/BrutalUi.Core.Tests/Elements/SliderDrawTests.cs` with 6 tests: `TheSliderDrawsTheTrackAndTheThumb` (the demo, the order of the two rects), `TheThumbFollowsTheValue` (the values 0, 50, and 100 give the x 40, 90, and 140), `TheThumbFitsTheContentBox` (a padding of 2 and a border of 1 give a thumb of 14 x 14 inside the content box, and a press maps through the same box), `AnActiveRuleRestylesTheThumb` (a `Slider:active` color shows on the thumb after a press and an `Update`, and the old color after the release), `TheOpacityScalesTheThumb` (an opacity of 0.5 halves the alpha, and a transparent color draws no thumb rect), and `ASmallSliderDrawsASafeThumb` (a zero range draws the thumb at the start, a slider narrower than tall draws a thumb of the content width, and a slider of 0 x 0 draws no thumb and gives no NaN).
- [ ] 2.3 Run the build, the tests, the demo class, the class of phase 1, and the classes of the phase intro. Record the results in `context.md`.

**Estimated Steps:** about 20 (3 tasks, about seven steps for one task)

**Files (3):** `Slider.cs`, `SliderDrawTests.cs`, `context.md`

**Acceptance Criteria:**
- [ ] The 6 tests of `SliderDrawTests` pass, and the tests of phase 1 still pass.
- [ ] `PaintWalk.cs` and `StylePass.cs` have no diff against the base commit.

---

### Phase 3: The Drag
**Goal:** A pointer move while the slider holds `Active` drags the value. A move with no press, a move after the release, and a move after a press on another element do nothing.
**Demo:** a unit test pushes a left press at the value 25 and a move to the value 60 over the slider, and runs `Update`. It reads `Value` 60 and two `ValueChanged` calls. The developer runs `dotnet run --project tests/BrutalUi.Core.Tests/BrutalUi.Core.Tests.csproj -- --filter-class '*SliderDragTests'` and reads a total of 7 with `failed: 0`.
**Needs:** Phase 2
**Agent tier:** STANDARD (sonnet)

**Tasks:**
- [ ] 3.1 Edit `src/BrutalUi.Core/Elements/WidgetInput.cs`: add `IsDragMove(Element, in PointerEvent)` (Internal Types). The doc comment states the CR-1 limit: a right press on the widget also sets `Active`, so a later move drags (Decision 2).
- [ ] 3.2 Edit `src/BrutalUi.Core/Elements/Slider.cs`: add `s_pointerMove`, write it into `OnPointerMove` in the constructor, and add the static `HandlePointerMove` of the Drag Rule. Extend the remarks: the drag rule, the stop for each drag move (also with no change), the CR-1 limit, and "a drag that leaves the rect stops at the edge" (phase 4 removes this sentence).
- [ ] 3.3 Write `tests/BrutalUi.Core.Tests/Elements/SliderDragTests.cs` with 7 tests: `AMoveAfterAPressDragsTheValue` (the demo), `AMoveToTheSameValueCallsNothing` (the call count stays, and the walk still stops), `AMoveWithNoPressDoesNothingAndWalksOn` (a hover move: no change, and the root bubble handler gets it), `AMoveAfterTheReleaseDoesNothing`, `APressElsewhereThenAMoveOverTheSliderDoesNothing` (a press on `Lead`, a move over the slider), `ADragMoveStopsTheWalk`, and `TheDelegateReadsTheNewValue`.
- [ ] 3.4 Run the build, the tests, the demo class, the classes of phases 1 and 2, and the classes of the phase intro. Record the results in `context.md`.

**Estimated Steps:** about 24 (4 tasks, about six steps for one task)

**Files (4):** `WidgetInput.cs`, `Slider.cs`, `SliderDragTests.cs`, `context.md`

**Acceptance Criteria:**
- [ ] The 7 tests of `SliderDragTests` pass, and the tests of phases 1 and 2 still pass.
- [ ] `Slider.cs` holds no `bool` field for the drag state.

---

### Phase 4: The Drag Past the Edge
**Goal:** A drag that leaves the slider rect still moves the value. Each move with no target walks to the slider while it holds `Active`. `Hover`, `PointerHit`, and the routing gate still follow the hit. Elements with no capture keep the route of today.
**Demo:** a unit test pushes a left press at the value 25 and a move to (300, 10), over the root and outside the slider, and runs `Update`. It reads `Value` 100, `Hover` on the root, and no `Hover` on the slider. The developer runs `dotnet run --project tests/BrutalUi.Core.Tests/BrutalUi.Core.Tests.csproj -- --filter-class '*DragCaptureTests'` and reads a total of 8 with `failed: 0`.
**Needs:** Phase 3
**Agent tier:** ADVANCED (opus)

**Tier note:** This phase changes `RoutePointer`, a central method of the drain. Add one branch for a `PointerMove` only. Do not change the hit test, `SetHover`, `PointerHit`, the button mask, `PressButton`, `ReleaseButton`, `FinishRelease`, the click focus, the Tab step, the stale rules, or `EventWalk.cs`. No existing element implements `IDragCapture`, so the route of each existing test stays the same. Prove it with the classes of the phase intro and each `Input/` class with no edit.

**Tasks:**
- [ ] 4.1 Record the blast radius in `context.md`: list each caller of `RoutePointer` and each existing test that pushes a `PointerMove` while a button is down. Confirm that no element type of `src/` implements `IDragCapture` before this phase.
- [ ] 4.2 Write `src/BrutalUi.Core/Input/IDragCapture.cs`: the internal interface of the Internal Types section. The doc comment states the rule, the end of the capture (the release of the last button clears `Active`), and the limits: `Hover`, `PointerHit`, and the routing gate follow the hit. A move with a caller target keeps its target.
- [ ] 4.3 Edit `src/BrutalUi.Core/Tree/UiContext.cs`: in `RoutePointer`, after `SetHover(hit)`, for a `PointerMove` only: when `_activeElement is IDragCapture { CapturesMoves: true }`, write `_activeElement` into `entry.Header.Target` and return it. Else keep the code of today. Edit the doc comments: the summary and the remarks of `RoutePointer` (the return value is the hit or the capture element), the remarks of `DrainEvents` (the move rule), and the remarks of `ActiveElement`. Name Decision 8 of pointer-routing and Decision 3 of this plan.
- [ ] 4.4 Edit `src/BrutalUi.Core/Elements/Slider.cs`: implement `IDragCapture` explicitly. `CapturesMoves` returns `true` (phase 6 adds the `Disabled` rule). Replace the remark "a drag that leaves the rect stops at the edge" with the capture rule.
- [ ] 4.5 Write `tests/BrutalUi.Core.Tests/Input/DragCaptureTests.cs` with 8 tests on `SliderTree`: `ADragPastTheRightEndGivesTheMaximum` (the demo), `ADragPastTheLeftEndGivesTheMinimum` (a move over `Lead`), `ADragAboveOrBelowTheSliderReadsTheXOnly` (a move to (100, 60) gives 50), `ACapturedMoveKeepsHoverAndTheGateOnTheHit` (`Hover` on `Other`, `PointerHit` is `Other`, and `RoutingGate.PointerOverUi` is `true`. A move to `OverNothing` gives a null `PointerHit`, `PointerOverUi` `false`, and still a drag), `ACapturedMoveWalksThePathOfTheSlider` (the target of the move is the slider, a root capture handler runs, and a bubble handler on `Other` gets no call), `TheCaptureEndsAtTheRelease` (after a release over `Other`, a move over `Other` reaches `Other` and does not change the value), `AnElementWithNoCaptureKeepsTheHitRule` (a press on `Lead`, then a move over `Other`, walks to `Other`), and `ARemovedSliderEndsTheCapture` (a remove of the slider during a drag, then a move: no throw, and the move walks to the hit).
- [ ] 4.6 Run the build, the tests, the demo class, the classes of phases 1 to 3, the classes of the phase intro, and each class in `tests/BrutalUi.Core.Tests/Input/`. Run `git diff <base> --stat -- tests/` and expect no changed existing file. Record the results in `context.md`.

**Estimated Steps:** about 42 (6 tasks, about seven steps for one task)

**Files (5):** `IDragCapture.cs`, `UiContext.cs`, `Slider.cs`, `DragCaptureTests.cs`, `context.md`

**Acceptance Criteria:**
- [ ] The 8 tests of `DragCaptureTests` pass, and the tests of phases 1 to 3 still pass.
- [ ] Each class of the phase intro and each existing `Input/` class passes with no edit.
- [ ] In `UiContext.cs`, only `RoutePointer` and doc comments change.
- [ ] `HitWalk.cs`, `EventWalk.cs`, and `Element.cs` have no diff against the base commit.

---

### Phase 5: The Step and the Arrow Keys
**Goal:** The value rounds to the step on each path, and `SetRange` changes the range. A focused slider moves by one step, or by one percent of the range for a step of 0, on the Left and Right arrow keys.
**Demo:** a unit test calls `SetFocus(slider)` on a slider with the range 0 to 100, the step 10, and the value 50. It pushes a Right arrow key down with no target and runs `Update`. It reads `Value` 60 and one `ValueChanged` call. The developer runs `dotnet run --project tests/BrutalUi.Core.Tests/BrutalUi.Core.Tests.csproj -- --filter-class '*SliderKeyTests'` and reads a total of 9 with `failed: 0`, and the same command with `'*SliderStepTests'` reads a total of 9 with `failed: 0`.
**Needs:** Phase 4
**Agent tier:** STANDARD (sonnet)

**Tasks (group A: the step):**
- [ ] 5.1 Edit `src/BrutalUi.Core/Elements/Slider.cs`: add `Step` to `CreateInfo`, the get-only `Step`, and `SetRange(float minimum, float maximum, float step)`. Write `Normalize` by Decision 4, and use it in `Apply`. The constructor and `SetRange` check the range first (Decision 9). On a bad value, they throw `ArgumentOutOfRangeException` with the name of the bad parameter, and `SetRange` changes nothing. `SetRange` writes the range and then calls `Apply` with the old value: it marks `Paint` on a change of the range or of the value, and it calls nothing. Extend the remarks: the value rule, the grid from `Minimum`, a range that is not a multiple of the step, and the NaN rule.
- [ ] 5.2 Write `tests/BrutalUi.Core.Tests/Elements/SliderStepTests.cs` with 9 tests: `ASetRoundsToTheNearestStep` (the step 10: 23 gives 20, 25 gives 30, 27 gives 30), `TheGridStartsAtTheMinimum` (the range 5 to 105 with the step 10: 12 gives 15, 9 gives 5), `AStepOfZeroKeepsTheValue` (33.3 stays), `ANaNValueGivesTheMinimum`, `ARangeThatIsNoMultipleOfTheStep` (0 to 10 with the step 3: 10 gives 9. With the step 4: 10 gives 10), `AFractionalStepGivesTheGrid` (0 to 1 with the step 0.1: 0.34 gives 0.3 within 1e-6, and 1 gives 1), `APressAndADragRoundToTheStep` (a press at 23 gives 20 and one call. A move to 24 gives no call), `SetRangeClampsAndRoundsAndCallsNothing` (the value 73, then `SetRange(0, 50, 5)` gives 50, marks `Paint`, and calls nothing. The same range again marks nothing), and `ABadRangeThrowsAndKeepsTheState` (the constructor and `SetRange` throw for a minimum above the maximum, a NaN or infinite bound, a negative step, and a NaN or infinite step. After a throw of `SetRange`, the range and the value stay).
- [ ] 5.3 Run the build, the tests, the class of task 5.2, and the classes of phases 1 to 4. Record the results in `context.md`.

**Tasks (group B: the arrow keys):**
- [ ] 5.4 Edit `src/BrutalUi.Core/Elements/WidgetInput.cs`: add `RightKey`, `LeftKey`, and `IsArrowKey(Element, in KeyEvent, out int direction)` (Decision 8). The doc comments name the GLFW values.
- [ ] 5.5 Edit `src/BrutalUi.Core/Elements/Slider.cs`: add `s_keyDown`, write it into `OnKeyDown` in the constructor, and add the static `HandleKeyDown`. After the check and the stop, it calls `UserSet(Value + direction * delta)`. The delta is `Step` when `Step` is above 0. Else it is `(Maximum - Minimum) / 100`. Extend the remarks: the key rule, the repeat, the stop at the end of the range, and "a write of `=` to `OnKeyDown` removes the keys".
- [ ] 5.6 Write `tests/BrutalUi.Core.Tests/Elements/SliderKeyTests.cs` with 9 tests: `ARightArrowMovesTheValueByOneStep` (the demo), `ALeftArrowMovesTheValueBack`, `AStepOfZeroMovesOnePercentOfTheRange` (the range 0 to 200: the value moves by 2), `TheArrowStopsAtTheEndOfTheRange` (the value 100 and a Right key: no call, and the walk stops), `ARepeatMovesTheValue`, `AModifierBlocksTheArrow` (`Control`, `Alt`, and `Super` give no move, and the key walks on. `Shift` moves), `OtherKeysDoNothing` (Up 265, Down 264, Space, Enter, and a key up of Right give no move, and the root bubble handler gets each one), `AKeyFromAFocusableChildDoesNothing` (a focusable child of the slider owns the focus, and Right bubbles to the slider with no move), and `TabThenRightMovesTheValue` (a Tab with no owner reaches the slider, after `Lead`, because `Lead` is not focusable).
- [ ] 5.7 Run the build, the tests, the demo class, the classes of phases 1 to 4 and of task 5.2, and the classes of the phase intro. Record the results in `context.md`.

**Estimated Steps:** about 45 (7 tasks, about six steps for one task)

**Files (group A, 3):** `Slider.cs`, `SliderStepTests.cs`, `context.md`
**Files (group B, 4):** `WidgetInput.cs`, `Slider.cs`, `SliderKeyTests.cs`, `context.md`

**Acceptance Criteria:**
- [ ] The 9 tests of `SliderStepTests` and the 9 tests of `SliderKeyTests` pass, and the tests of phases 1 to 4 still pass.
- [ ] `ButtonKeyTests`, `ToggleTests`, and `TabNavigationTests` pass with no edit.
- [ ] `Slider.cs` holds one `Normalize` method, and each path that writes `_value` calls `Apply`.

---

### Phase 6: Disabled Sliders
**Goal:** A slider that holds `PseudoStates.Disabled` takes no press, no drag, no key, and no capture, and it does not stop the walk. A code set still works.
**Demo:** a unit test sets `Disabled` on the slider, pushes a left press at the value 25, and runs `Update`. It reads the old value, 0 `ValueChanged` calls, and one call of the root bubble handler for the press. The developer runs `dotnet run --project tests/BrutalUi.Core.Tests/BrutalUi.Core.Tests.csproj -- --filter-class '*SliderDisabledTests'` and reads a total of 6 with `failed: 0`.
**Needs:** Phase 5
**Agent tier:** STANDARD (sonnet)

**Tasks:**
- [ ] 6.1 Edit `src/BrutalUi.Core/Elements/WidgetInput.cs`: `IsDragStart`, `IsDragMove`, and `IsArrowKey` return `false` when `widget.States` holds `Disabled` (Decision 11). Edit `src/BrutalUi.Core/Elements/Slider.cs`: `CapturesMoves` returns `false` when the slider holds `Disabled`. Extend the remarks of `Slider`: a disabled slider takes no input, does not stop the walk, and captures no move. A code set of `Value` and `SetRange` still work.
- [ ] 6.2 Write `tests/BrutalUi.Core.Tests/Elements/SliderDisabledTests.cs` with 6 tests: `ADisabledSliderIgnoresAPress` (the demo), `ADisableDuringADragStopsTheDrag` (a move over the slider changes nothing and walks on. An enable during the same press drags again), `ADisabledSliderIgnoresTheArrows` (a key with the slider as the caller target, because the drain drops a disabled owner), `ADisabledSliderCapturesNoMove` (a disable during a drag, then a move to `PastRight`: the move walks to the root, and the value stays), `ACodeSetWorksOnADisabledSlider` (`Value` and `SetRange`), and `APressOnADisabledSliderTakesNoFocus`.
- [ ] 6.3 Run the build, the tests, the demo class, the classes of phases 1 to 5, and the classes of the phase intro. Record the results in `context.md`.

**Estimated Steps:** about 18 (3 tasks, about six steps for one task)

**Files (4):** `WidgetInput.cs`, `Slider.cs`, `SliderDisabledTests.cs`, `context.md`

**Acceptance Criteria:**
- [ ] The 6 tests of `SliderDisabledTests` pass, and the tests of phases 1 to 5 still pass.
- [ ] The handlers of `Slider.cs` hold no `Disabled` check of their own. The checks live in `WidgetInput`, and `CapturesMoves` holds the only check in `Slider.cs`.
- [ ] `WidgetDisabledTests` passes with no edit.

---

### Phase 7: The Allocation Gate and the Record
**Goal:** Prove 0 bytes for `Update`, `Draw`, and the full slider input path after warm-up. Record the slider rules, the drag rule, and the capture in the technical design and the changelog.
**Demo:** the developer runs `dotnet run --project tests/BrutalUi.Core.Tests/BrutalUi.Core.Tests.csproj -- --filter-class '*SliderAllocationTests'` and reads a total of 3 with `failed: 0`. A planted allocation at the start of `Slider.HandlePointerMove` makes `dotnet test --solution brutal-ui.sln` exit with a value that is not 0.
**Needs:** Phase 6
**Agent tier:** STANDARD (sonnet)

**Tier note:** Copy the gate shape of `WidgetAllocationTests` and `FocusAllocationTests`: a `Scene` class that the test builds before the measure, delegates from instance methods that the scene creates one time, `AllocationGate.ReleaseAllocationContext()` before the measured region, and a measured body with no lambda, no `$"`, no new `string`, and no `Assert`. Do not add warm-up rounds to hide an allocation. Do not run a plant and its restore in one parallel tool block (a gotcha of button-and-toggle).

**Tasks:**
- [ ] 7.1 Write `tests/BrutalUi.Core.Tests/Elements/SliderAllocationTests.cs` with 3 tests. The scene: a column root with 20 sliders (200 x 20, the range 0 to 100, the step 0), the last one in `Disabled`, and a sheet with `Slider`, `Slider:hover`, `Slider:active`, `Slider:focus`, and `:disabled` rules. `UpdateAndDrawAllocateNothingWithSliders` (100 rounds of `Update` and `Draw`). `TheSliderInputPathAllocatesNothing` (each step pushes, on the first slider, a press at 25, a move to 60, a move past the right end of the root, a release, a Left key, and a Right key, then runs `Update` and `Draw`). `TheMeasuredBodyDoesRealWork` (after one step: 5 `ValueChanged` calls, the value 100, and 20 thumb rects in the frame).
- [ ] 7.2 Copy `src/BrutalUi.Core/Elements/Slider.cs` to the scratchpad. Plant `GC.KeepAlive(new object());` as the first line of `Slider.HandlePointerMove`. Run `dotnet test --solution brutal-ui.sln > /dev/null 2>&1; echo "exit=$?"` and expect a value that is not 0. Restore the file from the copy, in a later tool call, and check it with `cmp`. Record the result and the gates that failed in `context.md`.
- [ ] 7.3 Edit `docs/technical-design.md` section 5: a "Slider" paragraph after the widget paragraphs (the value rule, the map of the thumb center, the press, the drag, the arrow keys, the draw of the track and the thumb, the disabled rule, and `ValueChanged`). Edit rule 5 of the ten widget rules (`WidgetInput` also holds `IsDragStart`, `IsDragMove`, `IsArrowKey`, and `ViewportOrigin`). Add the drag rule and the capture of `IDragCapture`. Edit item 4 of "The pointer step of the drain": a move with no target walks to the `Active` element when it captures the moves.
- [ ] 7.4 Edit `docs/technical-design.md` section 9, item 4 (the capture of a move), and section 14 (the slider test classes, `DragCaptureTests`, and the tenth gate with the plant). Edit `CHANGELOG.md` under `## [Unreleased]`: "Added" (`Slider`), and "Changed" (a pointer move with no target walks to a dragged slider while it holds `Active`).
- [ ] 7.5 Run `python3 /home/aaronstatic/.claude/skills/asd-ste100/scripts/ste-lint.py --fail-over 2.5` on each changed doc and on this plan. Fix each finding above 2.5.
- [ ] 7.6 Run the build, the tests, and `scripts/test-linux.sh; echo "exit=$?"`. Record the final test counts against the counts of task 1.1 in `context.md`.

**Estimated Steps:** about 36 (6 tasks, about six steps for one task)

**Files (5):** `SliderAllocationTests.cs`, `Slider.cs` (planted and restored), `technical-design.md`, `CHANGELOG.md`, `context.md`

**Acceptance Criteria:**
- [ ] The 3 tests of `SliderAllocationTests` pass.
- [ ] The planted allocation fails the run, and `cmp` finds no difference after the restore.
- [ ] `scripts/test-linux.sh` prints its PASS line.

---

## Key Technical Decisions

### Confirmed Assumptions

None (draft with no interview).

### Decision 1: A sealed `Slider` on the widget pattern, with the drag and arrow checks in `WidgetInput`
**Choice:** `Slider` derives from `Element` directly and follows the ten rules of the widget pattern. The new checks (`IsDragStart`, `IsDragMove`, `IsArrowKey`) and the origin helper (`ViewportOrigin`) go into `WidgetInput`, next to `IsClick` and `IsKeyActivation`.
**Rationale:** The chosen approach: the widget pattern with the smallest new surface. It matches epic decision A2 and Decision 1 of button-and-toggle. scroll-view needs the same drag start, drag move, and origin, so one place holds the drag rule and the disabled rule. The checks are internal, so the public API grows by `Slider` only.
**Alternatives:** (a) Private checks in `Slider`: scroll-view copies the code, and the disabled rule drifts. (b) An abstract `DraggableElement` base class: a second class level, against Decision 1 of button-and-toggle.

### Decision 2: The drag reads `Active` only
**Choice:** The press needs the left button and `Active` on the slider. The move needs `Active` on the slider. The slider holds no drag flag.
**Rationale:** The requirement says "a pointer move while the slider holds `Active`". `Active` marks the element of the first press of the gesture and clears after the last release (Decision 2 of button-and-toggle), so the drag ends with no own state. The brief asks for no dependence on a fix of CR-1. The limit: a right press on the slider also sets `Active`, so a later move drags the value. A chord gives the same limit as the click of a button. A fix of CR-1 in `UiContext` (the button of the press that set `Active`) can close both cases later.
**Alternatives:** (a) A private drag flag that a left press sets: it fixes the right-button drag, but the flag goes stale when an ancestor capture handler stops a later press, and the slider cannot see a release outside its rect. (b) A wait for the CR-1 fix: it blocks this feature on a decision of a person.

### Decision 3: An opt-in drag capture in `RoutePointer`
**Choice:** A new internal interface `IDragCapture { bool CapturesMoves { get; } }`. In `RoutePointer`, for a `PointerMove` with no target, when `_activeElement is IDragCapture { CapturesMoves: true }`, the entry walks to `_activeElement`. The hit test, `Hover`, and `PointerHit` stay as today. `Slider` implements the interface explicitly. This amends Decision 8 of pointer-routing for a move during a captured drag only.
**Rationale:** The Move Route Finding: with the route of today, a drag that leaves the rect stops short of the end, and a fast drag to the end of the track misses the maximum. The opt-in keeps each other element on the route of today, so no existing test changes, and the press, the release, the click, and the hover rules stay the same. The interface is internal and needs no change of `Element.cs`. The pattern check allocates nothing. scroll-view implements the same interface for its drag. Notes Q2 holds the open choice.
**Alternatives:** (a) No capture: the drag stops at the edge of the rect, and scroll-view meets the same gap. (b) A capture for each element that holds `Active`: a move during a press walks to the pressed element for each button, toggle, and plain element, a routing change for each game handler on `OnPointerMove`. (c) A flag on `Element`: a change of `Element.cs` and a public or internal member on each element for two widget types.

### Decision 4: One value rule for each path
**Choice:** `Normalize(v)`: a NaN gives `Minimum`. Clamp to `[Minimum, Maximum]`. When `Step` is above 0, round to `Minimum + k * Step` with `MathF.Round((v - Minimum) / Step, MidpointRounding.AwayFromZero)` for `k`. Clamp again. The code set, the create-info value, `SetRange`, the press, the drag, and the keys all call `Apply`, which calls `Normalize`.
**Rationale:** One rule gives one result for each path. The grid starts at `Minimum`, as in an HTML range input. The second clamp keeps a rounded value in the range. A range that is not a multiple of the step reaches `Maximum` only when the nearest grid point is at or past it (0 to 10 with the step 3 stops at 9). The remarks advise a range that is a multiple of the step. The midpoint rule gives a fixed result for a test.
**Alternatives:** (a) A floor to the grid: a press near the next grid point snaps back. (b) Steps that end at `Maximum` (a grid from both ends): two grids, and a key from `Maximum` lands off the grid. (c) A throw for a NaN: a code set from a bad calculation throws in the game loop.

### Decision 5: Only user input calls `ValueChanged`
**Choice:** A press, a drag move, and an arrow key call `ValueChanged` when the normalized value differs from the old value. A code set of `Value`, the create-info value, and `SetRange` call nothing. A user input that gives the same value calls nothing.
**Rationale:** Rule 7 of the widget pattern and Q4 of button-and-toggle: the same rule as `Toggle.Checked`. A game that loads a value from a save file gets no echo call. "A set to the same value raises nothing" of the requirement holds for each path. Notes Q4 holds the open choice.
**Alternatives:** A code set that changes the value calls the delegate: it differs from `Toggle`, and a store binding of the framework epic gets an echo.

### Decision 6: The thumb center follows the pointer
**Choice:** `ValueAtPointer(x)`: the content box comes from `Layout.ContentBox` of the border box at `WidgetInput.ViewportOrigin(this)`. The thumb width is the smaller of the content height and the content width. The ratio is `(x - content.X - thumbWidth / 2) / (content.Z - thumbWidth)`, clamped to 0 and 1, and 0 when the travel is 0 or less. The value is `Minimum + ratio * (Maximum - Minimum)`, through `Apply`. A press on the thumb also maps, with no grab offset.
**Rationale:** The draw and the map use the same helpers, so the thumb center sits under the pointer after a press. The origin uses the layout of the last `Update`, the same layout as the hit test. One helper holds the origin, so scroll-view adds its scroll offset in one place.
**Alternatives:** (a) A grab offset for a press on the thumb: more state, and the requirement asks for "the pointer position". (b) A box that the draw stores in the element: no value before the first `Draw`, and a test with `Update` only fails.

### Decision 7: The track is the box of the slider, and the thumb uses `Color`
**Choice:** The paint walk draws the background and the border of the slider as the track. `PaintContent` draws the thumb as one rect: the content height square, the `Color` of the computed style scaled by the opacity product, and the `CornerRadius` of the slider. The stylesheet styles the thumb through the pseudo-states of the slider: `Slider:hover`, `Slider:active`, `Slider:focus`, and `:disabled` with `color`.
**Rationale:** The requirement says "through a pseudo-state or a child class", and epic A3 adds no new pseudo-state. The existing states and an existing style member are enough. No new style property, no child element, and no layout change on a value change. `Color` inherits and defaults to opaque white, so a thumb shows with no rule. Notes Q3 holds the open choice.
**Alternatives:** (a) A child element with a class `thumb`: a second element for each slider, a layout mark for each value change, and a pickable child takes `Active` from the slider. (b) A new style property for the thumb color: a change of `ComputedStyle`, `StyleProperties`, the parser, and each style test theory. (c) A new pseudo-state: against epic A3.

### Decision 8: The arrow key check allows a repeat
**Choice:** `IsArrowKey` needs a `KeyDown` with the widget as `e.Header.Target`, no `Control`, `Alt`, or `Super`, and the key `LeftKey` (263) or `RightKey` (262). A repeat passes. `Shift` does not block it. A key that the slider uses stops the walk, also at the end of the range.
**Rationale:** The requirement and epic A4 name the arrow keys. A held arrow key moves the thumb, as in each common slider. The target rule and the modifier rule copy Decision 5 of button-and-toggle. The values come from the GLFW 3 header (`GLFW_KEY_RIGHT` 262, `GLFW_KEY_LEFT` 263), and they fit the codes of the repository (`EnterKey` 257, `TabKey` 258). The adapter sends the GLFW value with no table (`GlfwUiInput.cs`).
**Alternatives:** (a) `IsKeyActivation` with new flags: it rejects a repeat. (b) No stop at the end of the range: an ancestor gets a key that the slider owns.

### Decision 9: The range is read-only, and `SetRange` changes it as one
**Choice:** `Minimum`, `Maximum`, and `Step` have no setter. `SetRange(minimum, maximum, step)` checks the three values: finite bounds, `minimum <= maximum`, and a finite step of 0 or more. It throws `ArgumentOutOfRangeException` and changes nothing on a bad value. The constructor runs the same check. `CreateInfo.Maximum` is `required`.
**Rationale:** Three setters give a bad state between two calls (a new minimum above the old maximum). One call checks the full range. A maximum of 0 with the minimum 0 gives a slider that cannot move, so `Maximum` has no safe default, and rule 2 of the widget pattern asks for `required`. A zero range (`minimum == maximum`) stays valid. The value is `Minimum`.
**Alternatives:** (a) Three setters with a clamp: the order of the calls changes the result. (b) No way to change the range: the game creates a new slider for each level.

### Decision 10: `Slider` has no measure and no own size
**Choice:** No measure callback and no inline size. The size comes from a rule or from `Inline`.
**Rationale:** The rule of `Toggle` (Decision 9 of button-and-toggle). An inline size wins over each rule. widget-sample adds the size rules.
**Alternatives:** A default inline size: a stylesheet can then not size the slider.

### Decision 11: A disabled slider does nothing, does not stop the walk, and captures no move
**Choice:** Each slider check of `WidgetInput` returns `false` for `Disabled`. `CapturesMoves` returns `false` for `Disabled`. A code set and `SetRange` still work.
**Rationale:** Decision 6 of button-and-toggle. A disable during a drag ends the drag at once, and the next move walks to the hit.
**Alternatives:** A stop for a disabled slider: an ancestor never sees an event that the slider does not use.

### Decision 12: The delegate runs after the stop and the change
**Choice:** The handler calls `StopPropagation`, writes the value, marks `Paint`, and then calls `ValueChanged`.
**Rationale:** Decision 10 of button-and-toggle. The delegate reads the new value. A throw from the delegate leaves the drain, and Decision 2 of button-and-toggle still clears `Active`.
**Alternatives:** The delegate first: it reads the old value.

---

## Risks & Mitigation

| Risk | Likelihood | Impact | Mitigation |
|------|------------|--------|------------|
| The capture changes the route of an existing element. | Low | High | Decision 3 is opt-in, and no existing type implements `IDragCapture`. Task 4.1 records the blast radius. The tier note of phase 4. Each pointer, focus, and widget class runs with no edit. |
| A captured drag over the game world reads `PointerOverUi` as `false`, and the game acts on the pointer. | Medium | Medium | The gate follows the hit by design (Decision 3). `ACapturedMoveKeepsHoverAndTheGateOnTheHit` pins it. Conflicts and Gaps item 5 names it for the framework epic. |
| A right press or a chord drags the value (CR-1). | Medium | Low | Decision 2. The remarks of `Slider` and `IsDragMove` state the limit. A fix of CR-1 closes it. |
| A dropped release in a full ring keeps `Active`, so the capture goes on with no button down. | Low | Medium | The limit of the ring exists today for `Active`. The remarks of `IDragCapture` name it. `Update` each frame keeps the ring below its capacity. |
| Float rounding gives a value off the grid. | Medium | Low | One `Normalize` with a second clamp (Decision 4). `AFractionalStepGivesTheGrid` checks 0.1 within 1e-6. |
| scroll-view adds a scroll offset to the hit test, and the slider map inside a scroll view goes wrong. | Medium | Medium | `WidgetInput.ViewportOrigin` is the one place. Its doc comment and Conflicts and Gaps item 6 tell scroll-view to add the offset there. |
| A game writes `slider.OnPointerDown = handler` and removes the press. | Medium | Medium | The remarks of `Slider` state it. The game reads `ValueChanged`. |
| A widget allocates in the input path. | Low | High | Cached static delegates. `SliderAllocationTests` and the plant of task 7.2. |
| A slider with no size rule takes no space. | Medium | Low | Decision 10. The remarks state it. widget-sample adds the rules. |

---

## Dependencies

### Internal Dependencies
- `interactive-elements/button-and-toggle`: the widget pattern, `WidgetInput`, the release order (Decision 2), and the CR-1 limit. It must be committed before phase 1 starts.
- `input-routing/event-model`: `UiEventHandler<T>`, `PointerEvent`, `KeyEvent`, `UiEventHeader`, `StopPropagation`, `PushEvent`, `DrainEvents`, and `EventWalk`.
- `input-routing/pointer-routing`: `RoutePointer`, `PressButton`, `ActiveElement`, `HitTest`, `PointerHit`, `PointerTree`, and Decisions 8 and R1.
- `input-routing/keyboard-focus`: `IsFocusable`, the click focus, the key route, Tab, `SetFocus`, and the disabled rule.
- `input-routing/routing-gate`: `RoutingGate.PointerOverUi`.
- `style-system`: `PseudoStates`, `Element.States`, the type selector, `ComputedStyle.Color`, `BackgroundColor`, and `CornerRadius`.
- `core-tree`: `Element`, `ElementType.Intern`, `MarkDirty`, `PaintContent`, `LayoutNode.Rect`, and `LayoutNode.ContentBox`.
- `imgui-renderer`: `IUiRenderer.Rect` and `RecordingRenderer`.

### External Dependencies
- `Brutal.Numerics`. It is pinned already. No new package and no native dependency.
- GLFW key values: `GLFW_KEY_RIGHT` is 262 and `GLFW_KEY_LEFT` is 263 ([GLFW 3.3 header](https://www.glfw.org/docs/3.3/glfw3_8h_source.html), [GLFW 3.4 reference](https://glfw.org/docs/3.4/glfw3_8h.html)). The adapter sends the GLFW value as `KeyEvent.Key`.

---

## Definition of Done

All criteria below must pass. An independent evaluator runs the Verification Method from `/home/aaronstatic/dev/brutal-ui` on Linux. `<base>` is the base commit that task 1.1 records in `context.md`.

### Functional Criteria
- [ ] F1. A left press at x 75 over a `Slider` with the range 0 to 100, pushed with no target and followed by `Update`, gives `Value` 25 and one `ValueChanged` call with the slider as the sender (`SliderPressTests.APressMovesTheValueToThePointer`).
- [ ] F2. A press before or past the track gives the minimum or the maximum. A press at the same value and a right press call nothing. A press stops the walk and focuses the slider. A code set clamps and calls nothing. A type rule for `Slider` matches it (`SliderPressTests`, 10 tests).
- [ ] F3. The draw gives the track rect and then the thumb rect, in the background color and in `Color`. The thumb follows the value, fits the content box, takes a `Slider:active` color, scales with the opacity, and stays safe for a zero range and a zero size (`SliderDrawTests`, 6 tests).
- [ ] F4. A move after a press drags the value. A move with no press, after the release, or after a press elsewhere does nothing. A drag move stops the walk (`SliderDragTests`, 7 tests).
- [ ] F5. A drag past either end of the slider gives the maximum or the minimum. `Hover`, `PointerHit`, and the routing gate follow the hit. The capture ends at the release. An element with no capture keeps the hit rule. A removed slider ends the capture (`DragCaptureTests`, 8 tests).
- [ ] F6. Each path rounds to the nearest step from the minimum, with a NaN to the minimum. `SetRange` clamps and rounds and calls nothing. A bad range throws and changes nothing (`SliderStepTests`, 9 tests).
- [ ] F7. Left and Right move a focused slider by one step, or one percent of the range for a step of 0. A repeat moves. `Control`, `Alt`, `Super`, other keys, a key up, and a key from a child do nothing (`SliderKeyTests`, 9 tests).
- [ ] F8. A disabled slider takes no press, no drag, no key, and no capture, does not stop the walk, and takes no focus. A code set works (`SliderDisabledTests`, 6 tests).
- [ ] F9. `SliderAllocationTests` measures 0 bytes for `Update` and `Draw` and for the full input path (3 tests). A planted allocation in `Slider.HandlePointerMove` gives a non-zero exit code of `dotnet test`.

### Quality Criteria
- [ ] Q1. `dotnet build brutal-ui.sln` gives `0 Warning(s)` and `0 Error(s)`.
- [ ] Q2. `dotnet test --solution brutal-ui.sln` gives 0 failed. `BrutalUi.Core.Tests` has 58 more tests than the count of task 1.1 (10 + 6 + 7 + 8 + 9 + 9 + 6 + 3).
- [ ] Q3. `Slider` is `public sealed`. `IDragCapture` and `WidgetInput` are internal. The only `required` member of `Slider.CreateInfo` is `Maximum`.
- [ ] Q4. `Slider.cs` creates no delegate outside a `static readonly` field initializer and holds no lambda. It holds three `static readonly UiEventHandler` fields.
- [ ] Q5. No existing test file has a diff against `<base>`.
- [ ] Q6. The changed docs and the new doc comments use ASD-STE100 prose. The lint gives no score above 2.5.

### Integration Criteria
- [ ] I1. `BrutalUi.Core` has no ImGui or GLFW `using`, and no `.csproj` has a diff.
- [ ] I2. `Element.cs`, `PseudoStates.cs`, `ElementHandlers.cs`, `EventWalk.cs`, `HitWalk.cs`, `FocusWalk.cs`, `PaintWalk.cs`, `StylePass.cs`, `Button.cs`, `Toggle.cs`, `Label.cs`, `Directory.Packages.props`, `samples/`, and `scripts/` have no diff against `<base>`.
- [ ] I3. In `UiContext.cs`, only `RoutePointer` (the capture branch) and doc comments change.
- [ ] I4. Section 5 of `docs/technical-design.md` holds the slider rules, the drag rule, the capture, and the amended pointer step. Section 9 item 4 states the capture. Section 14 names the slider classes and the tenth gate. `CHANGELOG.md` names `Slider` and the capture.
- [ ] I5. `scripts/test-linux.sh` prints its PASS line.

### Verification Method

Run each step from `/home/aaronstatic/dev/brutal-ui` on Linux. `<scratch>` is any empty folder outside the repository. Read `<base>` from `docs/features/interactive-elements/slider/context.md`.

1. `dotnet build brutal-ui.sln 2>&1 | tail -n 30`. Expect `0 Warning(s)` and `0 Error(s)` (Q1).
2. `dotnet test --solution brutal-ui.sln 2>&1 | tail -n 30`. Expect 0 failed. Compare the Core total with the count of task 1.1 in `context.md` (Q2).
3. For each class, run `dotnet run --project tests/BrutalUi.Core.Tests/BrutalUi.Core.Tests.csproj -- --filter-class '*<Class>' 2>&1 | tail -n 5`. Expect `failed: 0` and these totals: `SliderPressTests` 10 (F1, F2), `SliderDrawTests` 6 (F3), `SliderDragTests` 7 (F4), `DragCaptureTests` 8 (F5), `SliderStepTests` 9 (F6), `SliderKeyTests` 9 (F7), `SliderDisabledTests` 6 (F8), and `SliderAllocationTests` 3 (F9). Read the test names in each file and match them to the criteria.
4. Copy `src/BrutalUi.Core/Elements/Slider.cs` to `<scratch>`. Add `GC.KeepAlive(new object());` as the first line of `HandlePointerMove`. Run `dotnet test --solution brutal-ui.sln > /dev/null 2>&1; echo "exit=$?"`. Expect a value that is not 0. Restore the file from the copy, and check it with `cmp`. Run step 2 again (F9).
5. `git diff <base> --diff-filter=M --name-only -- tests/`. Expect no line (Q5). `git diff <base> --stat -- src/BrutalUi.Core/Tree/Element.cs src/BrutalUi.Core/Tree/PseudoStates.cs src/BrutalUi.Core/Input/ElementHandlers.cs src/BrutalUi.Core/Input/EventWalk.cs src/BrutalUi.Core/Input/HitWalk.cs src/BrutalUi.Core/Input/FocusWalk.cs src/BrutalUi.Core/Rendering/PaintWalk.cs src/BrutalUi.Core/Style/StylePass.cs src/BrutalUi.Core/Elements/Button.cs src/BrutalUi.Core/Elements/Toggle.cs src/BrutalUi.Core/Elements/Label.cs Directory.Packages.props '*.csproj' samples/ scripts/`. Expect no output (I1, I2).
6. `git diff <base> -- src/BrutalUi.Core/Tree/UiContext.cs`. Read the diff. Expect one new branch in `RoutePointer` for a `PointerMove` and doc comments only (I3).
7. `grep -n '^public sealed class Slider' src/BrutalUi.Core/Elements/Slider.cs` gives one line. `grep -rnE '^internal (interface IDragCapture|static class WidgetInput)' src/BrutalUi.Core` gives two lines. `grep -n 'required' src/BrutalUi.Core/Elements/Slider.cs` gives one code line, the `Maximum` field. `grep -nE '\(ref [A-Za-z]+ e\) =>|new UiEventHandler' src/BrutalUi.Core/Elements/Slider.cs` gives no line, and `grep -c 'static readonly UiEventHandler' src/BrutalUi.Core/Elements/Slider.cs` gives 3 (Q3, Q4).
8. `grep -rnE 'using .*(ImGui|Glfw)' src/BrutalUi.Core` gives no line (I1).
9. `scripts/test-linux.sh; echo "exit=$?"`. Expect the PASS line and `exit=0` (I5, the demo of phase 1).
10. Read section 5 ("Widgets", the slider paragraph, "The pointer step of the drain"), section 9 item 4, and section 14 of `docs/technical-design.md`, and the `## [Unreleased]` part of `CHANGELOG.md` (I4). Run `python3 /home/aaronstatic/.claude/skills/asd-ste100/scripts/ste-lint.py --fail-over 2.5` on each changed doc (Q6).

---

## Testing Strategy

### Unit Tests
- `Elements/SliderPressTests` (10): the press map, the ends, the same value, the right button, the stop, the focus, the code set, the create-info, and the type rule.
- `Elements/SliderDrawTests` (6): the track and the thumb, the thumb position, the content box, the `:active` rule, the opacity, and the small sizes.
- `Elements/SliderDragTests` (7): the drag, the same value, the hover move, the move after the release, the press elsewhere, the stop, and the delegate value.
- `Input/DragCaptureTests` (8): the drag past each end, the x only, the hover and the gate, the walk path, the end of the capture, the hit rule of other elements, and a removed slider.
- `Elements/SliderStepTests` (9): the rounding, the grid start, the step of 0, NaN, a range off the grid, a fractional step, the step on the pointer path, `SetRange`, and a bad range.
- `Elements/SliderKeyTests` (9): Right, Left, one percent, the end, the repeat, the modifiers, other keys, a key from a child, and Tab then Right.
- `Elements/SliderDisabledTests` (6): the press, the drag, the keys, the capture, the code set, and the focus.
- Each slider test uses `SliderTree`, pushes events with no target through `UiContext.PushEvent`, and calls `Update`. A key test with a disabled slider uses a caller target, because the drain drops a disabled owner.

### Allocation Tests
- `Elements/SliderAllocationTests` (3): `Update` and `Draw`, the input path with a captured move, and the real-work check. widget-sample adds the 1000-element gate with each widget (epic A9).

### Regression Tests
- Each pointer, focus, and routing-gate class with no edit.
- Each button-and-toggle class with no edit, also `WidgetAllocationTests`.

### Integration Tests
- `scripts/test-linux.sh` runs the full suite and the HelloPanel smoke run. HelloPanel does not change. widget-sample adds the slider to it.

### Manual Testing
- None in this feature. widget-sample adds the visible demo and the manual check of a drag on screen.

---

## Related Documentation

- **Requirements:** `docs/features/interactive-elements/slider/requirements.md`
- **Epic:** `docs/features/interactive-elements/epic-overview.md`, `docs/features/interactive-elements/epic-requirements.md`
- **Pattern source:** `docs/features/interactive-elements/button-and-toggle/implementation.md` ("The Widget Pattern", Decisions 1 to 10), its `context.md` (Important Decisions, Open Questions OQ1 and OQ2, Review Fixes), and its `code-review.md` (CR-1)
- **Siblings:** `docs/features/interactive-elements/scroll-view/requirements.md` (copies the drag rule and the capture), `text-field/requirements.md`, `widget-sample/requirements.md`
- **Input routing:** `docs/features/input-routing/pointer-routing/implementation.md` (Decisions 8, 9, R1), `docs/features/input-routing/keyboard-focus/implementation.md` (Decisions 4, 5, 7)
- **Design:** `docs/technical-design.md` sections 5 (Element Tree, Events, Widgets), 7 (pseudo-states), 9 (Input), and 14 (Testing)
- **Rules:** `AGENTS.md`, `.beast-mode/phase-handoff.md` sections 1 and 11, `.beast-mode/tool-rules.md`
- **Code patterns:** `src/BrutalUi.Core/Elements/Toggle.cs` (the widget shape with two slots), `src/BrutalUi.Core/Elements/WidgetInput.cs` (the checks), `src/BrutalUi.Core/Elements/Image.cs` (a `PaintContent` with the content box), `src/BrutalUi.Core/Tree/UiContext.cs` (`DrainEvents`, `RoutePointer`, `PressButton`, `FinishRelease`), `src/BrutalUi.Core/Input/HitWalk.cs` (the origin rule), `tests/BrutalUi.Core.Tests/Input/PointerTree.cs` (the event factories and `ViewportRect`), `tests/BrutalUi.Core.Tests/Elements/WidgetTree.cs` (the tree shape), and `tests/BrutalUi.Core.Tests/Elements/WidgetAllocationTests.cs` (the gate)
- **Skills:** no implementation skill in `.claude/skills/` applies. The skills there are workflow commands.

---

## Notes

### Open Scope Questions

No interview ran. The plan uses the recommended option (the first one) of each question.

**Q1. What is the demo of phase 1?**
- **A (recommended):** a person runs `scripts/test-linux.sh` and reads its PASS line. In that run, `SliderPressTests.APressMovesTheValueToThePointer` pushes a left press at x 75 over a `Slider` (range 0 to 100) through `UiContext.PushEvent`, runs `Update`, and reads `Value` 25 and one `ValueChanged` call with the slider as the sender. Reason: the same form as the confirmed demo of button-and-toggle, and the script runs the full suite and the smoke run.
- B: the developer runs `dotnet run --project tests/BrutalUi.Core.Tests/BrutalUi.Core.Tests.csproj -- --filter-class '*SliderPressTests'` and reads a total of 10 with `failed: 0`.
- C: a person drags a slider in HelloPanel on screen. It moves widget-sample work into this feature and needs a GPU.

**Q2. What does a drag do when the pointer leaves the slider rect?**
- **A (recommended):** The opt-in capture of Decision 3. Each move with no target walks to the slider while it holds `Active`. `Hover` and the routing gate follow the hit. Reason: a drag to the end of the track reaches the maximum, no other element changes its route, and scroll-view reuses the capture.
- B: No capture. The drag stops at the edge of the rect. The remarks state the limit, and scroll-view decides the capture. Phase 4 goes away.
- C: A capture for each element that holds `Active`. A routing change for each element with an `OnPointerMove` handler.

**Q3. How does the stylesheet style the thumb?**
- **A (recommended):** The thumb draws in the `Color` of the slider. Rules on the pseudo-states of the slider (`Slider:hover`, `Slider:active`, `Slider:focus`, `:disabled`) restyle it (Decision 7). Reason: no new pseudo-state (epic A3), no new style property, and no child element.
- B: A child element with a class `thumb`, not pickable, placed through `Inline.Left`. The rule `Slider > .thumb` styles it. Each value change marks a layout.
- C: A new style property for the thumb color. It changes `ComputedStyle`, `StyleProperties`, the parser, and the style tests.

**Q4. Does a code set of `Value` call `ValueChanged`?**
- **A (recommended):** No. Only user input calls it (Decision 5, rule 7 of the widget pattern, Q4 of button-and-toggle).
- B: Yes, when the normalized value changes. A set to the same value calls nothing.

### Conflicts and Gaps

1. **The move route (main finding).** The requirement "a pointer move while the slider holds `Active` drags the value" conflicts with the route of a move to the hit only (`src/BrutalUi.Core/Tree/UiContext.cs`, `RoutePointer`). Decision 3 and phase 4 add an opt-in capture. This amends Decision 8 of pointer-routing ("a move and a scroll leave `Active`" stays true, but a captured move walks to the `Active` element). No existing test changes. Task 7.3 updates section 5 of the technical design.
2. **CR-1 of button-and-toggle.** A drag that reads `Active` has the chord limit of the click, and a right press on the slider also starts a drag (Decision 2). The slider does not depend on a fix. A fix in `UiContext` closes both cases.
3. **Epic A4 and Space.** `epic-requirements.md` says "Toggle and Slider activate on Space and the arrow keys". The feature requirement names the arrow keys only, and a slider has no meaning for Space. The slider ignores Space and Enter, and they walk on (`OtherKeysDoNothing`).
4. **"A set to the same value raises nothing."** The requirement can read as "a set to a new value raises the delegate". The plan follows rule 7 of the widget pattern: only user input calls it (Decision 5, Notes Q4).
5. **The routing gate during a captured drag.** `PointerOverUi` follows the hit, so a drag over the game world reads `false`. A game that reads the gate can act on the pointer during the drag. The same holds today for a press on a button and a move off the UI. The framework epic or a person decides a "pointer captured by UI" rule. This feature does not change the gate.
6. **scroll-view and the origin.** scroll-view adds a scroll offset to `HitWalk` and to the draw walk (epic A7). `WidgetInput.ViewportOrigin` then misses the offset for a slider inside a scroll view. The scroll-view plan must add the offset in `ViewportOrigin`. scroll-view also implements `IDragCapture` for its drag.
7. **The pseudo-state or child class wording.** The requirement offers both. Epic A3 adds no new pseudo-state. The plan uses the existing pseudo-states of the slider (Decision 7, Notes Q3).
8. **The 1000-element gate.** Epic A9 gives that gate to widget-sample. This feature adds a small gate with 20 sliders.
9. **No change of `WidgetTree`.** The brief asks for no change of an existing test file. `SliderTree` is a new scene, and `WidgetTree` and `WidgetAllocationTests` stay as they are.
10. **More keys.** Home, End, Page Up, Page Down, Up, and Down are out of scope. Up and Down walk on (A8: horizontal only). A later feature or the action map can add them.
11. **The size.** A `Slider` has no measure and no inline size (Decision 10). With no size rule, it takes no space. widget-sample adds the size rules to HelloPanel.
12. **The key values.** The repository has no named arrow constant. The values come from the GLFW 3 header and fit the codes of the repository (`EnterKey` 257, `TabKey` 258). The adapter sends the GLFW value with no table.
13. **The base commit.** At the time of this plan, button-and-toggle is complete but not committed on `feat/interactive-elements/button-and-toggle`. Task 1.1 records the base commit after that commit, and each "no diff" check of this plan uses it.
14. **The prose lint.** The planner had no shell, so it did not run `ste-lint.py` on this file. Task 7.5 runs it.

---

*This plan will be updated as implementation progresses. See context.md and tasks.md for current status.*
