# Scroll View - Implementation Plan

**Epic:** interactive-elements (feature 3 of 5)
**Status:** Planning
**Started:** 2026-10-26
**Target Completion:** TBD
**Last Updated:** 2026-10-26 09:00

---

## Quality Bar

**Target quality:** A core library feature that changes two central walks. `HitWalk` and `PaintWalk` run for each element of each frame and for each pointer event. A wrong origin, a hit that does not match the draw, a lost clip, a scroll that does not stop, or a hidden allocation here breaks each game that uses a widget. text-field copies the clip rule, and the `ListView` of a later epic builds on the offset.

- **The hit matches the draw:** The hit walk, the draw walk, and `WidgetInput.ViewportOrigin` read one offset method (`ScrollView.AppliedOffset`). A test draws a scrolled frame and hit-tests the center of each drawn rect.
- **The offset is exact:** Each path that writes the offset uses one clamp: each axis goes from 0 to the content size minus the viewport size. A test asserts each case of the clamp.
- **The view clips:** A `ScrollView` clips its children to its padding box with no stylesheet rule. The clip of the view does not move with the offset.
- **Widgets inside a view work:** A slider press and a slider drag inside a scrolled view map through the scrolled origin.
- **One change, one call:** Each user change of the offset calls `ScrollChanged` one time, after the change. A user input that gives the same offset calls nothing. A code set and a clamp from a content change call nothing.
- **Disabled means no effect:** A view with `PseudoStates.Disabled` takes no scroll, no drag, and no capture, and it does not stop the walk. The walks still apply its offset.
- **No change for other elements:** The walks change only for a `ScrollView`. Each existing hit, draw, pointer, focus, widget, and slider test passes with no edit.
- **0 bytes after warm-up:** `Update`, `Draw`, a scroll, a drag, a captured drag, and a slider drag inside a scrolled view allocate 0 bytes. A planted allocation fails a gate.
- **API clarity:** Each public member has an XML doc comment in ASD-STE100 prose. Section 5, 8, and 9 of the technical design state the offset rule, so text-field and `ListView` copy it with no guess.
- **No ImGui in Core:** `BrutalUi.Core` gets no package and no ImGui `using`.

---

## Executive Summary

This feature adds `ScrollView`, a container that holds a scroll offset and clips its children. It is a sealed `Element` subclass in `src/BrutalUi.Core/Elements/`, with the type id `ScrollView` (epic A2). It copies the widget pattern of button-and-toggle and the drag rule of slider. The constructor writes cached static delegates into `OnScroll`, `OnPointerDown`, and `OnPointerMove`, and it sets the inline overflow to `Overflow.Scroll`. The existing clip of the two walks then clips the children to the padding box, and the existing layout gives the children no maximum on the main axis.

The layout engine does not change (epic A7). The walks add the offset. In the enter step of `PaintWalk` and of `HitWalk`, a `ScrollView` gives its children the origin of its border box minus its offset. The clip of the view, the draw of the view, and the hit test of the view itself use the border box with no offset. `WidgetInput.ViewportOrigin` subtracts the offset of each ancestor scroll view, so a slider inside a scrolled view maps the pointer through the same origin as the hit test.

The content size comes from the layout rects of the direct children. The maximum offset on each axis is the far edge of the children plus the trailing padding and border, minus the size of the view, and never below 0. A set of `ScrollOffset` clamps to that range. The walks also clamp the stored offset with the layout of the last `Update`, so a content change never shows space past the end (Notes Q3).

A scroll event on the view or on a child moves the offset by the delta times `ScrollSpeed` (default 40 pixels for one unit). A positive delta scrolls toward the start, the GLFW convention. The scroll stops the walk only when the offset changes, so an inner view at its end passes the wheel to an outer view. A left press on the view itself starts a drag. Each move then moves the content with the pointer. The view implements `IDragCapture`, so the drag goes on outside its rect. A press on a pickable child gives no drag, so a button inside the view still clicks (Notes Q2). `ScrollChanged` gets the view as the sender (epic Q3). There is no scrollbar (epic Q4) and no key (Notes Q4).

`UiContext.cs` does not change. The scroll route and the drag capture of input-routing and slider already serve the view.

The plan has 6 phases and 29 tasks.

---

## User Personas Alignment

No `docs/user-personas.md` file exists. The personas come from `docs/mission-statement.md` and the epic docs.

**Target Users:**
- [x] **The game developer** (creates a `ScrollView` with a size, adds rows, sets `ScrollChanged`, reads and sets `ScrollOffset`, writes `ScrollView` and `:disabled` rules, and puts a `Slider` or a `Button` inside the view)
- [x] **The sibling feature agents** (text-field reads the clip rule and the origin rule. widget-sample puts a view in HelloPanel and adds it to the 1000-element gate)
- [x] **The later `ListView` epic** (builds on the offset in the walks, the content size, and `ScrollChanged`, and adds the scrollbar)
- [x] **The maintainer** (runs one more 0-byte gate)
- [ ] **The markup author** (not served. The markup epic registers `ScrollView` by name)

**User Subsets Served:**
- [x] **Simple** - `var list = new ScrollView(new ScrollView.CreateInfo()); list.Inline.Height = 300; list.Children.Add(row);` and a mouse wheel over the list.
- [x] **Complex** - a view in a view, a slider inside a view, `ScrollSpeed = 20`, a restore of `ScrollOffset` after the first `Update`, `States |= PseudoStates.Disabled` during a cut scene, and one static method that serves many views through the sender.

**UX Approach:**
- The game reads one delegate. It never reads the raw slot.
- The content follows the pointer during a drag, as on a touch screen.
- A wheel at the end of an inner view scrolls the outer view.

---

## Goals

### Primary Goals
1. Add `ScrollView`: a wheel scroll moves the content, and the draw shows it inside the clip of the view.
2. Make a hit land on the element that the user sees, also inside a scrolled view and inside a view in a view.
3. Keep each widget inside a view correct: a slider press and a slider drag map through the scrolled origin.
4. Drag the content with a left press on the view, also when the pointer leaves the view.
5. Clamp the offset on each path, also after a content change.
6. Block each input on a view that holds `Disabled`.
7. Allocate 0 bytes in `Update`, `Draw`, and the input path after warm-up.
8. Write the offset rule down for text-field and `ListView`.

### Success Criteria
- [ ] The phase 1 demo passes: `ScrollViewScrollTests` reads a total of 10 with `failed: 0`, and the demo test reads `ScrollOffset` (0, 40), one `ScrollChanged` call, and the rect of `Row0` at (0, -40, 300, 40) inside the clip (0, 0, 200, 100).
- [ ] The hit, slider, drag, clamp, nested, and disabled test classes pass.
- [ ] Each hit, draw, pointer, focus, button-and-toggle, and slider test passes with no edit.
- [ ] `ScrollViewAllocationTests` measures 0 bytes, and a planted allocation in `ScrollView.HandleScroll` fails it.
- [ ] Section 5, 8, 9, and 14 of the technical design hold the scroll view rules and the offset rule of the walks.

---

## Architecture Overview

### Codebase Findings

1. **The clip exists.** `PaintWalk.Enter` pushes a clip of the padding box for each element with an overflow other than `Overflow.Visible` (step 6). `HitWalk.Enter` computes the same child clip with `PaintWalk.PaddingBounds` and `PaintWalk.Intersect` (step 3), and it enters no child when the point is outside it (step 4). Thus the view needs no new clip code. It sets `Inline.Overflow = Overflow.Scroll`.
2. **The layout already serves a scroll container.** `LayoutEngine.Lines.cs` gives the children of an `Overflow.Scroll` container no maximum on the main axis (the Yoga rule). `ComputedStyle.FlexShrink` defaults to 0, so the children do not shrink to fit. Thus the children overflow the view, and their rects give the content size with no layout change (A7).
3. **The origin is one sum in three places.** `HitWalk.Enter` and `PaintWalk.Enter` compute `origin = parentOrigin + rect.XY` (step 3), and pass the origin of the frame to each child. `WidgetInput.ViewportOrigin` sums the rect positions up the parent chain. Its remarks name this feature: "subtract here the scroll offset of each ancestor scroll view, not the offset of the element itself".
4. **The scroll route exists.** `UiContext.RoutePointer` hit-tests a scroll with no target, moves `Hover`, and writes the hit as the target. The walk goes from the hit up to the root, so a scroll over a child reaches the `OnScroll` slot of the view in the bubble phase (`ScrollRoutingTests.AScrollGoesToTheElementUnderThePointer`). A scroll leaves `Active`, and the drain gives it no press target.
5. **The drag capture exists.** `RoutePointer` sends each `PointerMove` with no target to `LeftPressElement` when it is `IDragCapture { CapturesMoves: true }`, in the roots, and shown (`FocusWalk.IsShown`). Thus the view drags past its rect when it implements the interface, with no change of `UiContext.cs`.
6. **`HitFrame.Origin` has two uses.** The element test (`Contains`) and the children both read `HitFrame.Origin`. The view needs the border box origin for its own test and the scrolled origin for its children, so the hit frame needs a second field. `PaintFrame.Origin` has one use (the children), because the draw of the element runs in `Enter`.
7. **The walks read the tree only.** `UiContext.Draw` states "The draw reads the tree only". Thus the draw cannot write a clamped offset back. The clamp at use computes the clamp and writes nothing (Decision 5).

### File Tree (end state)

```
src/BrutalUi.Core/
├── Elements/ScrollView.cs          # phase 1 (scroll, set, content size), 3 (drag, capture), 4 (clamp at use, NaN), 5 (remarks)
├── Elements/WidgetInput.cs         # phase 1 (IsScroll), 2 (ViewportOrigin subtracts the ancestor offsets)
├── Input/HitWalk.cs                # phase 2 (HitFrame.ChildOrigin, the scrolled child origin)
└── Rendering/PaintWalk.cs          # phase 1 (PaintFrame.ChildOrigin, the scrolled child origin)
tests/BrutalUi.Core.Tests/
├── Elements/ScrollViewTree.cs           # phase 1
├── Elements/ScrollViewScrollTests.cs    # phase 1
├── Elements/ScrollViewHitTests.cs       # phase 2
├── Elements/ScrollViewSliderTests.cs    # phase 2
├── Elements/ScrollViewDragTests.cs      # phase 3
├── Elements/ScrollViewClampTests.cs     # phase 4
├── Elements/ScrollViewNestedTests.cs    # phase 5
├── Elements/ScrollViewDisabledTests.cs  # phase 5
└── Elements/ScrollViewAllocationTests.cs # phase 6
docs/technical-design.md            # phase 6: sections 5, 8, 9, 14
CHANGELOG.md                        # phase 6
docs/features/interactive-elements/scroll-view/context.md   # each phase
```

These files do not change: `UiContext.cs`, `Element.cs`, `PseudoStates.cs`, `ElementHandlers.cs`, `EventWalk.cs`, `FocusWalk.cs`, `PointerEvent.cs`, `ScrollEvent.cs`, `IDragCapture.cs`, `LayoutNode.cs`, each file in `src/BrutalUi.Core/Layout/`, `StylePass.cs`, `IUiRenderer.cs`, `Slider.cs`, `Button.cs`, `Toggle.cs`, `Label.cs`, `Image.cs`, each `.csproj`, `Directory.Packages.props`, `samples/`, `scripts/`, `src/BrutalUi.ImGui/`, `src/BrutalUi.Glfw/`, and each existing test file (also `PointerTree.cs`, `SliderTree.cs`, and `WidgetTree.cs`).

### Public API (end state)

```csharp
namespace BrutalUi.Core;

public sealed class ScrollView : Element               // phase 3 adds the internal IDragCapture (explicit)
{
    public const float DefaultScrollSpeed = 40f;        // phase 1: pixels for one unit of the scroll delta
    public ScrollView(in CreateInfo info);              // phase 1: throws ArgumentOutOfRangeException for a bad speed
    public override ElementTypeId TypeId { get; }       // phase 1: the id of "ScrollView"
    public float2 ScrollOffset { get; set; }            // phase 1: a set clamps and calls nothing. Phase 4: the get clamps by the last layout
    public float ScrollSpeed { get; }                   // phase 1
    public Action<ScrollView>? ScrollChanged { get; set; } // phase 1 (Q3 of the epic)

    public struct CreateInfo
    {
        public float ScrollSpeed;                       // phase 1. 0 gives DefaultScrollSpeed. No required member (Decision 8)
    }
}
```

### Internal Types

- `WidgetInput` (`src/BrutalUi.Core/Elements/WidgetInput.cs`), additions and changes:
  - `internal static bool IsScroll(Element widget, in ScrollEvent e)`: the type is `Scroll`, and the widget does not hold `Disabled`. It does not read the target, so a scroll on a child counts. Phase 1.
  - `ViewportOrigin(Element)`: for each ancestor (not the element itself) that is a `ScrollView`, subtract `AppliedOffset()`. Phase 2.
- `ScrollView` internal members:
  - `internal float2 MaxOffset()`: the content size minus the viewport size on each axis, never below 0 (Decision 4). It reads `Layout.Rect`, `Layout.ContentInsets`, and the rect of each direct child. Phase 1.
  - `internal float2 AppliedOffset()`: the offset that the walks apply. Phase 1: it returns `_offset`. Phase 4: it returns `_offset` clamped by `MaxOffset()` (Decision 5).
- `ScrollView` private members: `_offset` (`float2`), `_scrollSpeed` (`float`), `_dragPointer` (`float2`, phase 3), `Clamp(float2)`, `bool Apply(float2)` (clamps, compares with the applied offset, writes, marks `DirtyFlags.Paint`, returns `true` for a change), `UserSet(float2)` (calls `Apply`, then `ScrollChanged` on `true`), the static `HandleScroll`, `HandlePointerDown`, and `HandlePointerMove`, and the static `ValidateSpeed`.
- `HitFrame` (`HitWalk.cs`): a new field `public float2 ChildOrigin`. `Origin` stays the border box origin for the element test. Phase 2.
- `PaintFrame` (`PaintWalk.cs`): the field `Origin` becomes `ChildOrigin`, the origin that the children add their rect position to. Phase 1.

### The Offset Rule of the Walks

The three origin rules stay the same. Each one changes in the same way.

1. **The view itself.** The origin of the view is the origin of its parent plus its rect position. Its own rect, its clip, its `PaintContent`, and its own hit test use this origin with no offset.
2. **The children.** The child origin of a `ScrollView` is its origin minus `AppliedOffset()`. The child origin of each other element is its origin. A child adds its rect position to the child origin of its parent.
3. **A view in a view.** The offsets add up, because each level subtracts its own offset from the origin that it passes down. The clips intersect, as today.
4. **`ViewportOrigin`.** It sums the rect positions of the element and each ancestor, and it subtracts `AppliedOffset()` of each ancestor `ScrollView`. It does not subtract the offset of the element itself.

```csharp
// The enter step of PaintWalk and HitWalk, after step 3 (the origin and the clip).
float2 childOrigin = origin;
if (element is ScrollView view)
{
    float2 offset = view.AppliedOffset();
    childOrigin = new float2(origin.X - offset.X, origin.Y - offset.Y);
}
```

### The Drag Rule (copied from slider)

The view copies "The Drag Rule" of `slider/implementation.md`, items 1 to 6.

1. **The start.** `HandlePointerDown` runs `WidgetInput.IsDragStart`. On `true`, it stops the walk and writes `_dragPointer = e.Position`. The offset does not change at the press.
2. **The move.** `HandlePointerMove` runs `WidgetInput.IsDragMove`. On `true`, it stops the walk, calls `UserSet(AppliedOffset() + (_dragPointer - e.Position))`, and writes `_dragPointer = e.Position`. It stops the walk also when the offset does not change (Decision 9).
3. **The state.** The drag reads the press target of the context only. `_dragPointer` is data, not a drag flag.
4. **The capture.** The view implements `IDragCapture` explicitly. `CapturesMoves` returns `true` when the view does not hold `Disabled`.
5. **The position.** The drag uses the difference of two pointer positions, so it needs no origin.
6. **The press button.** Only a left press with no chord starts a drag. The press target is the hit element, so only a press on the view itself starts a drag (Notes Q2).

### Data Flow

```
Game: ui.PushEvent(Scroll at p, delta d) ... ui.PushEvent(PointerDown at q) ... PointerMove at r ... PointerUp ... ui.Update(dt) ... ui.Draw(r)
UiContext.DrainEvents
  Scroll, no target:
    RoutePointer: HitWalk.Find(p) ──► each ScrollView passes origin - AppliedOffset() to its children ──► hit = Row2
                  Hover on Row2. target = Row2. press target = null
    walk: Row2 ... bubble ──► ScrollView.HandleScroll
          WidgetInput.IsScroll: Scroll, no Disabled
          next = Clamp(AppliedOffset() - d * ScrollSpeed) ──► same offset: return (no stop, the wheel walks on to an outer view)
          ──► else StopPropagation ──► UserSet(next)
  PointerDown, no target, over the view itself:
    RoutePointer: hit = ScrollView. PressButton: Active on the view, left, no chord
    drain: press target = ScrollView
    walk: ScrollView.HandlePointerDown ──► IsDragStart ──► StopPropagation ──► _dragPointer = q
  PointerMove, no target:
    RoutePointer: hit = H. Hover on H. LeftPressElement is IDragCapture { CapturesMoves: true } ──► target = ScrollView
    walk: ScrollView.HandlePointerMove ──► IsDragMove ──► StopPropagation ──► UserSet(AppliedOffset() + (_dragPointer - r)) ──► _dragPointer = r
  PointerDown over a Slider inside the view:
    RoutePointer: the hit walk applies the offset ──► hit = Slider
    walk: Slider.HandlePointerDown ──► Slider.ValueAtPointer ──► WidgetInput.ViewportOrigin(slider) subtracts the offset of each ancestor view
UserSet: Apply(next) ──► on a change: _offset = next, MarkDirty(Paint) ──► ScrollChanged(view)
UiContext.Update: the style pass, the layout pass (no change: the children keep their rects)
UiContext.Draw: PaintWalk: the view draws its box ──► PushClip(padding box) ──► children at origin - AppliedOffset() ──► PopClip
```

---

## Implementation Phases

Each phase is a slice: one narrow path that works end to end, with a demo line that a person can check. Section 11 of `.beast-mode/phase-handoff.md` holds the rules. Notes Q1 names the demo of phase 1. No person answered it, so phase 1 uses the recommended line.

Each phase ends with `dotnet build brutal-ui.sln 2>&1 | tail -n 30` and `dotnet test --solution brutal-ui.sln 2>&1 | tail -n 30` (tool rule 5). Expect `0 Warning(s)`, `0 Error(s)`, and 0 failed. The projects have no implicit usings, so each new file holds its own `using` lines. Each public member needs an XML doc comment (CS1591 is an error). Use `namespace BrutalUi.Core;` in Core and `namespace BrutalUi.Core.Tests;` in the tests. Prose in comments follows ASD-STE100. A test class that interns a class name uses a name prefix that is unique to the class, because xUnit runs the classes in parallel. Run one class with `dotnet run --project tests/BrutalUi.Core.Tests/BrutalUi.Core.Tests.csproj -- --filter-class '*<Class>' 2>&1 | tail -n 5`.

Each phase runs these classes with no edit (the regression set): the pointer and focus classes (`PointerPressTests`, `PointerStateEdgeTests`, `PointerHoverTests`, `PointerAllocationTests`, `ClickFocusTests`, `FocusOwnerTests`, `TabNavigationTests`, `FocusStateEdgeTests`, `FocusAllocationTests`, `RoutingGateTests`, `ScrollRoutingTests`), the walk classes (`HitTestTests`, `HitTestRuleTests`, `PaintWalkTests`, `PaintContentTests`, `RenderValueTests`), the button-and-toggle classes (`ReleaseOrderTests`, `ButtonClickTests`, `ButtonKeyTests`, `ToggleTests`, `ButtonTextTests`, `WidgetDisabledTests`, `WidgetAllocationTests`), and the slider classes (`ChordClickTests`, `SliderPressTests`, `SliderDrawTests`, `SliderDragTests`, `DragCaptureTests`, `SliderStepTests`, `SliderKeyTests`, `SliderDisabledTests`, `SliderAllocationTests`).

The test scene of each scroll view test is `ScrollViewTree` (task 1.5), unless a task names another scene. The view is 200 x 100 at (0, 0) with no padding. Its children in a column are `Row0` (300 x 40 at y 0), `Slider` (120 x 20 at y 40, the range 0 to 100), `Row2` (300 x 40 at y 60), `Row3` (300 x 40 at y 100, not pickable), and `Row4` (300 x 40 at y 140, not pickable). The content is 300 x 180, so the maximum offset is (100, 80). `Outside` is 100 x 100 at (0, 100). With the offset (ox, oy), the slider value `v` sits at the viewport point (10 + v - ox, 50 - oy).

### Phase 1: A Wheel Scroll Moves the Content
**Goal:** The smallest path that proves the idea: a wheel scroll over a child of a `ScrollView` moves the offset, calls `ScrollChanged` one time, and the draw shows the children at the new offset inside the clip of the view.
**Demo:** a unit test pushes a scroll with the delta (0, -1) over `Row0` through `UiContext.PushEvent`, and runs `Update` and `Draw` with a `RecordingRenderer`. It reads `ScrollOffset` (0, 40), one `ScrollChanged` call with the view as the sender, and the rect of `Row0` at (0, -40, 300, 40) between `PushClip(0, 0, 200, 100)` and its `PopClip`. The developer runs `dotnet run --project tests/BrutalUi.Core.Tests/BrutalUi.Core.Tests.csproj -- --filter-class '*ScrollViewScrollTests'` and reads a total of 10 with `failed: 0`.
**Needs:** none
**Agent tier:** ADVANCED (opus)

**Tier note:** This phase changes `PaintWalk`, the draw walk of each frame. Change only step 3 of `Enter` (add the child origin), the frame push (step 8), the loop line that passes the origin to a child, the `PaintFrame` field, and the doc comments. Do not change the hidden rule, the opacity rule, the rect step, the clip step, the `PaintContent` call, the pop, the stack growth, or the `finally` block. The view, its clip, and its `PaintContent` use the origin with no offset. Only a `ScrollView` changes the child origin, so each existing draw test passes with no edit. Prove it with `PaintWalkTests`, `PaintContentTests`, `RenderValueTests`, and each slider and widget draw class.

**Tasks:**
- [ ] 1.1 Record in `docs/features/interactive-elements/scroll-view/context.md`, before any change: the base commit (`git rev-parse HEAD`), and the result of `git ls-files src/BrutalUi.Core/Elements/Slider.cs src/BrutalUi.Core/Input/IDragCapture.cs` (expect two lines) and `git status --porcelain -- src tests` (expect no line). When slider is not in the base commit, stop and record it. Record the test counts of the solution and of `BrutalUi.Core.Tests`, and the counts of each class of the regression set. Record the Codebase Findings and the blast radius: each caller of `PaintWalk.Walk`, each use of `PaintFrame.Origin`, and each test that reads a paint frame.
- [ ] 1.2 Edit `src/BrutalUi.Core/Elements/WidgetInput.cs`: add `IsScroll(Element, in ScrollEvent)` (Internal Types). The doc comment states: the check reads the type and the disabled bit, it does not read the target, so a scroll on a child counts, and it reads no press target. Extend the class remarks: `IsScroll` serves each widget that takes the wheel.
- [ ] 1.3 Write `src/BrutalUi.Core/Elements/ScrollView.cs` by the widget pattern: sealed, the type id of `ScrollView`, `DefaultScrollSpeed`, `public ScrollView(in CreateInfo info)`, `CreateInfo` with `ScrollSpeed`, the get-only `ScrollSpeed`, `ScrollOffset` (the get returns `AppliedOffset()`, the set calls `Apply`), `public Action<ScrollView>? ScrollChanged { get; set; }`, `MaxOffset()` (Decision 4), `AppliedOffset()` (returns `_offset` in this phase), `Clamp`, `Apply`, `UserSet`, `ValidateSpeed`, and the static `HandleScroll` (Decision 7). The constructor checks the speed first (Decision 8), writes 40 for a speed of 0, sets `Inline.Overflow = Overflow.Scroll` (Decision 6), and writes `OnScroll = s_scroll`. `HandleScroll` computes the next offset first. It returns with no stop when the next offset equals the applied offset. Else it stops the walk and calls `UserSet`. The remarks state: the offset rule of the walks, the clip, the content size, the clamp, the scroll rule and its sign, the stop rule, the sender, "read `ScrollChanged`, not the slot", "a write of `=` to `OnScroll` removes the wheel", a code set calls nothing, the view has no own size (Decision 13), and a set before the first `Update` gives (0, 0).
- [ ] 1.4 Edit `src/BrutalUi.Core/Rendering/PaintWalk.cs` by the Offset Rule of the Walks: rename `PaintFrame.Origin` to `ChildOrigin` and state its meaning in the doc comment. In `Enter`, compute the child origin after step 3 (the code block of the rule), and write it into the frame in step 8. The loop passes `top.ChildOrigin` to the child. Edit the class remarks and the remarks of `Enter`: a `ScrollView` moves its children by its offset, and its own rect, clip, and `PaintContent` do not move.
- [ ] 1.5 Write `tests/BrutalUi.Core.Tests/Elements/ScrollViewTree.cs`: an internal test tree. The context has a `RecordingRenderer` measurer and a viewport of 800 x 600. The root is 400 x 300 in a column and is pickable. Its children are `View` (a `ScrollView`, 200 x 100, column) and `Outside` (a plain element, 100 x 100). The children of `View` are `Row0`, `Slider` (120 x 20, the range 0 to 100, the value 0), `Row2`, `Row3`, and `Row4` (each row 300 x 40 with its own opaque inline background color). `Row3` and `Row4` have `IsPickable = false`. The constructor takes `in ScrollView.CreateInfo`, and a second constructor uses the default create-info. It holds the points `OverRow0` (100, 20), `OverGap` (160, 50, the view beside the slider at the offset 0), `OverOutside` (50, 150), `PastRight` (300, 50, the root), and `OverNothing` (600, 500), `static float2 SliderAt(float value, float2 offset)` (the point (10 + value - offset.X, 50 - offset.Y)), `RowFill(int)`, and `Settle()`. Use the factories of `PointerTree` for the events. The doc comment states the rects, the maximum offset (100, 80), and the map. It states that `PointerTree.ViewportRect` ignores the offset.
- [ ] 1.6 Write `tests/BrutalUi.Core.Tests/Elements/ScrollViewScrollTests.cs` with 10 tests: `AScrollMovesTheContent` (the demo), `AScrollUpMovesTheContentBack` (a delta of (0, -1) and then (0, 1) give (0, 0) and two calls), `AHorizontalScrollMovesTheX` (the delta (-1, 0) gives (40, 0), and `Row0` draws at (-40, 0, 300, 40)), `TheScrollStopsAtTheEnd` (three scrolls of (0, -1) in one drain give (0, 80) and two calls, and the root bubble handler gets the third scroll only), `AScrollAtTheStartWalksOn` (the delta (0, 1) at the offset 0: no change, no call, and the root bubble handler gets it), `AScrollThatMovesStopsTheWalk` (the root capture handler runs, and the root bubble handler does not), `TheSpeedScalesTheDelta` (`ScrollSpeed` 10 and the delta (0, -2.5) give (0, 25). A create-info speed of 0 gives `ScrollSpeed` 40), `ACodeSetClampsAndCallsNothing` (a set of (500, 500) gives (100, 80) and marks `Paint`. A set of (-5, 10) gives (0, 10). The same value marks nothing after a settle. The call count stays 0), `TheCreateInfoTheTypeAndTheOverflow` (the type id is `ScrollView`, `Inline.Overflow` and `Style.Overflow` are `Scroll`, `IsFocusable` is `false`, and a speed of -1, NaN, or infinity throws `ArgumentOutOfRangeException` with `ParamName` `info`), and `ATypeRuleMatchesTheScrollView` (a rule with `Type = ElementType.Intern("ScrollView")` sets a fill on the view and not on `Outside`. A rule with `overflow: visible` on the type does not remove the clip).
- [ ] 1.7 Run the build, the tests, the demo class, and the regression set. Run `git diff <base> --stat -- tests/` and expect no changed existing file. Run `scripts/test-linux.sh; echo "exit=$?"` and expect the PASS line and `exit=0`. Record the counts and each decision that this plan did not settle in `context.md`.

**Estimated Steps:** about 48 (7 tasks, about seven steps for one task)

**Files (6):** `WidgetInput.cs`, `ScrollView.cs`, `PaintWalk.cs`, `ScrollViewTree.cs`, `ScrollViewScrollTests.cs`, `context.md`

**Acceptance Criteria:**
- [ ] The 10 tests of `ScrollViewScrollTests` pass.
- [ ] Each class of the regression set passes with no edit.
- [ ] In `PaintWalk.cs`, only the `PaintFrame` field, step 3 and step 8 of `Enter`, the loop line that enters a child, and doc comments change.
- [ ] `HitWalk.cs`, `UiContext.cs`, `Element.cs`, and `EventWalk.cs` have no diff against the base commit.

---

### Phase 2: A Hit Lands on the Element That the User Sees
**Goal:** The hit walk applies the offset, so a pointer event inside a scrolled view hits the element under the pointer in the draw. `WidgetInput.ViewportOrigin` subtracts the offset of each ancestor view, so a slider inside a scrolled view takes a press and a drag at the right value.
**Demo:** a unit test sets the offset (20, 20), pushes a left press at the viewport point of the slider value 25 (15, 30), and runs `Update`. It reads `Slider.Value` 25, one `ValueChanged` call, the offset (20, 20), and no `ScrollChanged` call. The developer runs `dotnet run --project tests/BrutalUi.Core.Tests/BrutalUi.Core.Tests.csproj -- --filter-class '*ScrollViewSliderTests'` and reads a total of 6 with `failed: 0`, and the same command with `'*ScrollViewHitTests'` reads a total of 6 with `failed: 0`.
**Needs:** Phase 1
**Agent tier:** ADVANCED (opus)

**Tier note:** This phase changes `HitWalk`, the hit test of each pointer event and each scroll, and the origin helper of each widget map. Add the `HitFrame.ChildOrigin` field, compute it in step 3 of `Enter` by the Offset Rule of the Walks, and pass `top.ChildOrigin` to each child. The element test keeps `top.Origin`, so the border box of the view does not move. Do not change the hidden rule, the clip rule, the cull of step 4, `Contains`, `InBounds`, the stack growth, or the `finally` block. `HitTestTests` reads `UiContext.HitStackSlots`. A new field keeps that test valid. Prove the change with each `Input/` class, each slider class, and each widget class with no edit.

**Tasks:**
- [ ] 2.1 Record the blast radius in `context.md`: each caller of `HitWalk.Find` and of `UiContext.HitTest`, each test that reads `HitStackSlots`, and each caller of `WidgetInput.ViewportOrigin`.
- [ ] 2.2 Edit `src/BrutalUi.Core/Input/HitWalk.cs`: add `public float2 ChildOrigin` to `HitFrame` with a doc comment. In `Enter`, compute the child origin after step 3, and write it into the frame in step 4. The loop copies `top.ChildOrigin` before the call to `Enter`, next to the copy of the clip (the gotcha of the stale ref). Edit the class remarks (the origin rule: a `ScrollView` moves its children by its offset, and the hit walk and the draw walk read the same `AppliedOffset`) and the doc comments of `Enter` and `HitFrame.Origin`.
- [ ] 2.3 Edit `src/BrutalUi.Core/Elements/WidgetInput.cs`: `ViewportOrigin` subtracts `AppliedOffset()` of each ancestor `ScrollView`, and not of the element itself (rule 4 of the Offset Rule of the Walks). Replace the remark "When scroll-view adds a scroll offset to those walks ..." with the rule and a reference to `ScrollViewSliderTests`.
- [ ] 2.4 Write `tests/BrutalUi.Core.Tests/Elements/ScrollViewHitTests.cs` with 6 tests: `AHitLandsOnTheElementThatTheUserSees` (the offset (0, 40) and a move at (100, 30): `PointerHit` and `Hover` are on `Row2`. The base code gives `Row0`), `TheClipOfTheViewDoesNotMove` (the offset (60, 0): a move at (220, 20) hits the root, and a move at (190, 20) hits `Row0`), `TheGapAndANonPickableChildHitTheView` (the offset 0 at `OverGap` hits the view. The offset (0, 80) at (100, 50) falls through `Row3` to the view), `AScrollWalksThePathOfTheShiftedChild` (the offset (0, 40) and a scroll at (100, 30): the target of the walk is `Row2`, and the view moves), `AnElementOutsideTheViewKeepsItsHit` (the offset (100, 80): `OverOutside` hits `Outside`, and `PastRight` hits the root), and `TheHitAndTheDrawAgree` (the offset (37, 23): for each rect that the draw gives to `Row0`, `Row2`, and the slider thumb, a move at the center of the visible part of the rect inside the clip hits that element).
- [ ] 2.5 Write `tests/BrutalUi.Core.Tests/Elements/ScrollViewSliderTests.cs` with 6 tests: `APressOnASliderInAScrolledViewMapsThroughTheOffset` (the demo), `ADragOfASliderInAScrolledViewFollowsThePointer` (a press at 25 and a move to `SliderAt(60, (20, 20))` give 60 and two calls), `ASliderDragPastTheViewEdgeStaysOnTheSlider` (a press at 25 and a move to (300, 30) give 100. The offset stays (20, 20), and `Hover` is on the root), `AWheelOverTheSliderScrollsTheView` (the offset (20, 0) and the delta (0, -0.5) over the slider give the offset (20, 20), and the value stays), `AScrollAndAPressInOneDrainUseTheNewOffset` (the offset 0, then a scroll of (-0.5, 0) over `Row0` and a left press at (15, 50) in one `Update` give the offset (20, 0) and the value 25), and `TheOriginSubtractsTheAncestorOffsetOnly` (the offset (20, 20): `ViewportOrigin` gives (-20, 20) for the slider, (0, 0) for the view, and (-20, -20) for `Row0`).
- [ ] 2.6 Run the build, the tests, the demo class, the class of task 2.4, the class of phase 1, the regression set, and each class in `tests/BrutalUi.Core.Tests/Input/`. Run `git diff <base> --stat -- tests/` and expect no changed existing file. Record the results in `context.md`.

**Estimated Steps:** about 42 (6 tasks, about seven steps for one task)

**Files (5):** `HitWalk.cs`, `WidgetInput.cs`, `ScrollViewHitTests.cs`, `ScrollViewSliderTests.cs`, `context.md`

**Acceptance Criteria:**
- [ ] The 6 tests of `ScrollViewHitTests` and the 6 tests of `ScrollViewSliderTests` pass, and the tests of phase 1 still pass.
- [ ] Each class of the regression set and each existing `Input/` class passes with no edit.
- [ ] In `HitWalk.cs`, only the `HitFrame` field, step 3 and step 4 of `Enter`, the copy before the call to `Enter`, and doc comments change.
- [ ] `Slider.cs`, `UiContext.cs`, and `IDragCapture.cs` have no diff against the base commit.

---

### Phase 3: A Drag Moves the Content
**Goal:** A left press on the view itself and a move drag the content with the pointer. The drag goes on outside the view through the capture. A press on a pickable child, a right press, and a chord give no drag.
**Demo:** a unit test pushes a left press at `OverGap` (160, 50) and a move to (160, 20), and runs `Update`. It reads `ScrollOffset` (0, 30) and one `ScrollChanged` call. The developer runs `dotnet run --project tests/BrutalUi.Core.Tests/BrutalUi.Core.Tests.csproj -- --filter-class '*ScrollViewDragTests'` and reads a total of 9 with `failed: 0`.
**Needs:** Phase 2
**Agent tier:** STANDARD (sonnet)

**Tasks:**
- [ ] 3.1 Edit `src/BrutalUi.Core/Elements/ScrollView.cs`: add `IDragCapture` to the class line, `_dragPointer`, `s_pointerDown`, `s_pointerMove`, the writes of `OnPointerDown` and `OnPointerMove` in the constructor, the static `HandlePointerDown` and `HandlePointerMove` of the Drag Rule, and the explicit `IDragCapture.CapturesMoves` (no `Disabled`). Extend the remarks: the drag rule (Decision 9), the press on the view itself (Notes Q2: a press on a pickable child gives no drag, so set `IsPickable = false` on a decorative child to let a press fall through), the stop for each drag move, the press button rule, the capture, and "a write of `=` to `OnPointerDown` removes the drag".
- [ ] 3.2 Write `tests/BrutalUi.Core.Tests/Elements/ScrollViewDragTests.cs` with 9 tests: `ADragMovesTheContentWithThePointer` (the demo, and the draw gives `Row0` at (0, -30, 300, 40)), `ADragOutsideTheViewKeepsDragging` (a press at `OverGap` and a move to (300, 10) over the root give (0, 40). `Hover` and `PointerHit` are on the root), `ADragPastTheEndComesBackAtOnce` (a move to (160, -100) gives (0, 80). A move to (160, -90) gives (0, 70)), `ADragMoveStopsTheWalkAlsoWithNoChange` (at the end, one more move gives no call, and the root bubble handler gets no move), `APressOnAPickableChildGivesNoDrag` (a press at `OverRow0` and a move to (100, 0): no change, no call, and the root bubble handler gets the move), `APressOnANonPickableChildDrags` (a code set of (0, 80), a press at (100, 50) over `Row3`, and a move to (100, 70) give (0, 60)), `ARightPressGivesNoDrag` (a right press at `OverGap` and a move: no change, and the root bubble handler gets the move), `TheCaptureEndsAtTheRelease` (after a release at `PastRight`, a move over the view changes nothing), and `AWheelDuringADragAddsUp` (a press at `OverGap`, a move to (160, 40) gives (0, 10), a scroll of (0, -1) gives (0, 50), and a move to (160, 30) gives (0, 60)).
- [ ] 3.3 Run the build, the tests, the demo class, the classes of phases 1 and 2, the regression set, and each class in `tests/BrutalUi.Core.Tests/Input/`. Record the results in `context.md`.

**Estimated Steps:** about 22 (3 tasks, about seven steps for one task)

**Files (3):** `ScrollView.cs`, `ScrollViewDragTests.cs`, `context.md`

**Acceptance Criteria:**
- [ ] The 9 tests of `ScrollViewDragTests` pass, and the tests of phases 1 and 2 still pass.
- [ ] `DragCaptureTests` and `SliderDragTests` pass with no edit.
- [ ] `ScrollView.cs` holds no `bool` field for the drag state. `UiContext.cs` has no diff against the base commit.

---

### Phase 4: The Clamp After a Content Change
**Goal:** The shown offset always fits the content. The walks and the get of `ScrollOffset` clamp the stored offset with the layout of the last `Update` (Notes Q3). A NaN and an infinite value follow one rule. The content size counts the padding, the border, an absolute child, and no hidden child.
**Demo:** a unit test sets the offset (0, 80), hides `Row4` (`Inline.Display = Display.None`), and runs `Update` and `Draw`. It reads `ScrollOffset` (0, 40), the rect of `Row0` at (0, -40, 300, 40), and no `ScrollChanged` call. The developer runs `dotnet run --project tests/BrutalUi.Core.Tests/BrutalUi.Core.Tests.csproj -- --filter-class '*ScrollViewClampTests'` and reads a total of 10 with `failed: 0`.
**Needs:** Phase 3
**Agent tier:** STANDARD (sonnet)

**Tasks:**
- [ ] 4.1 Edit `src/BrutalUi.Core/Elements/ScrollView.cs`: `AppliedOffset()` returns `Clamp(_offset)` with `MaxOffset()` of the last layout (Decision 5). `Apply` compares the clamped value with `AppliedOffset()`, and it writes the clamped value. `Clamp` gives 0 for a NaN component, the maximum for positive infinity, and 0 for negative infinity. `HandleScroll` counts a delta component that is not finite as 0. Extend the remarks: the clamp at use, the stored offset (a content shrink shows the end, and a content growth before the next input brings the old offset back), the content size rule (the direct children, their border boxes, the trailing padding and border, no margin, an absolute child counts, a hidden child gives a rect of 0), a set before the first `Update`, and the NaN rule.
- [ ] 4.2 Write `tests/BrutalUi.Core.Tests/Elements/ScrollViewClampTests.cs` with 10 tests: `AContentShrinkShowsTheEndAtOnce` (the demo), `TheOffsetComesBackWhenTheContentGrows` (after the demo, a show of `Row4` gives (0, 80) and no call), `AUserScrollAfterAShrinkStartsFromTheShownOffset` (after the shrink, a scroll of (0, 1) gives (0, 0) and one call. A show of `Row4` then gives (0, 0)), `ASetBeforeTheFirstUpdateGivesZero` (a new tree with no settle: a set of (50, 50) gives (0, 0), also after a settle), `ANaNOrInfiniteSetFollowsTheRule` ((NaN, 50) gives (0, 50). (+infinity, -infinity) gives (100, 0)), `ANonFiniteDeltaCountsAsZero` ((NaN, -1) gives (0, 40). (+infinity, 0) changes nothing and walks on), `ThePaddingAndTheBorderCount` (a padding of 10 and a border of 2 on the view give the maximum (124, 104), the clip (2, 2, 196, 96), and `Row0` at (12 - ox, 12 - oy)), `AnAbsoluteChildCountsAndAHiddenChildDoesNot` (an absolute child at the left 500 with a width of 50 gives the maximum x 350. A hide of that child gives 100 again), `AViewLargerThanTheContentDoesNotScroll` (a view of 400 x 300 gives the maximum (0, 0), and a scroll walks on), and `AViewResizeClampsTheShownOffset` (the offset (0, 80) and a view height of 150 give (0, 30)).
- [ ] 4.3 Run the build, the tests, the demo class, the classes of phases 1 to 3, and the regression set. Record the results in `context.md`.

**Estimated Steps:** about 22 (3 tasks, about seven steps for one task)

**Files (3):** `ScrollView.cs`, `ScrollViewClampTests.cs`, `context.md`

**Acceptance Criteria:**
- [ ] The 10 tests of `ScrollViewClampTests` pass, and the tests of phases 1 to 3 still pass.
- [ ] `ScrollView.cs` holds one `Clamp` method, and each path that writes `_offset` calls `Apply`.
- [ ] `HitWalk.cs`, `PaintWalk.cs`, and `WidgetInput.cs` have no change in this phase.

---

### Phase 5: Views in Views and Disabled Views
**Goal:** A view in a view scrolls, hits, and draws with the sum of the offsets, and an inner view at its end passes the wheel to the outer view. A view that holds `Disabled` takes no scroll, no drag, and no capture, does not stop the walk, and keeps its offset in the walks.
**Demo:** a unit test puts an inner view at its end (0, 60) inside an outer view at (0, 0), pushes a scroll of (0, -1) over the inner view, and runs `Update`. It reads the inner offset (0, 60), the outer offset (0, 40), one outer `ScrollChanged` call, and no inner call. The developer runs `dotnet run --project tests/BrutalUi.Core.Tests/BrutalUi.Core.Tests.csproj -- --filter-class '*ScrollViewNestedTests'` and reads a total of 6 with `failed: 0`, and the same command with `'*ScrollViewDisabledTests'` reads a total of 5 with `failed: 0`.
**Needs:** Phase 4
**Agent tier:** STANDARD (sonnet)

**Tasks:**
- [ ] 5.1 Edit the remarks of `src/BrutalUi.Core/Elements/ScrollView.cs` only: the nested rule (the offsets add up, the clips intersect, and the stop rule passes the wheel at the end of an inner view to the outer view) and the disabled rule (Decision 11). Change no code line. When a test of tasks 5.2 or 5.3 fails on the code, stop and record the failure in `context.md`. Do not change the walks in this phase.
- [ ] 5.2 Write `tests/BrutalUi.Core.Tests/Elements/ScrollViewNestedTests.cs` with 6 tests on a scene that a private static method of the class builds: an outer view of 200 x 100 in a column with `Top` (300 x 40), `Inner` (a view of 150 x 60 with a left padding of 10, holding `InnerRow0` to `InnerRow2`, each 200 x 40), and `Bottom` (300 x 80). The outer maximum is (100, 80), and the inner maximum is (60, 60). The tests: `AnInnerViewAtItsEndPassesTheWheelToTheOuterView` (the demo), `AnInnerViewThatMovesKeepsTheWheel` (the inner offset 0: the inner view goes to (0, 40), and the outer view stays), `TheOffsetsAddUpInTheHitAndTheDraw` (the outer offset (0, 20) and the inner offset (0, 30): a move at (50, 35) hits `InnerRow1`, the draw gives `InnerRow1` at (10, 30, 200, 40), and the inner clip is (0, 20, 150, 60)), `TheOriginSubtractsEachAncestorOffset` (the same offsets: `ViewportOrigin` gives (10, 30) for `InnerRow1` and (0, 20) for `Inner`), `ADragOnTheInnerViewMovesTheInnerViewOnly` (a press at (5, 60) in the inner padding and a move to (5, 30) give the inner offset (0, 30), and the outer view stays), and `ADisabledInnerViewPassesTheWheelToTheOuterView`.
- [ ] 5.3 Write `tests/BrutalUi.Core.Tests/Elements/ScrollViewDisabledTests.cs` with 5 tests on `ScrollViewTree`: `ADisabledViewIgnoresTheWheel` (no change, no call, and the root bubble handler gets the scroll), `ADisabledViewIgnoresADrag` (a press at `OverGap` reaches the root bubble handler, and a move changes nothing), `ADisableDuringADragStopsTheDragAndTheCapture` (a press and a move give (0, 30). After a disable, a move to `PastRight` walks to the root, and the offset stays), `ADisabledViewKeepsItsOffsetInTheWalks` (the offset (0, 40) and a disable: the draw gives `Row0` at (0, -40, 300, 40), and a move at (100, 30) hits `Row2`), and `ACodeSetWorksOnADisabledView`.
- [ ] 5.4 Run the build, the tests, the two classes of this phase, the classes of phases 1 to 4, and the regression set. Record the results in `context.md`.

**Estimated Steps:** about 26 (4 tasks, about six steps for one task)

**Files (4):** `ScrollView.cs` (remarks only), `ScrollViewNestedTests.cs`, `ScrollViewDisabledTests.cs`, `context.md`

**Acceptance Criteria:**
- [ ] The 6 tests of `ScrollViewNestedTests` and the 5 tests of `ScrollViewDisabledTests` pass, and the tests of phases 1 to 4 still pass.
- [ ] `ScrollView.cs` changes in `///` lines only in this phase.
- [ ] `WidgetDisabledTests` and `SliderDisabledTests` pass with no edit.

---

### Phase 6: The Allocation Gate and the Record
**Goal:** Prove 0 bytes for `Update`, `Draw`, and the full scroll view input path after warm-up. Record the scroll view rules and the offset rule of the walks in the technical design and the changelog.
**Demo:** the developer runs `dotnet run --project tests/BrutalUi.Core.Tests/BrutalUi.Core.Tests.csproj -- --filter-class '*ScrollViewAllocationTests'` and reads a total of 3 with `failed: 0`. A planted allocation at the start of `ScrollView.HandleScroll` makes `dotnet test --solution brutal-ui.sln` exit with a value that is not 0.
**Needs:** Phase 5
**Agent tier:** STANDARD (sonnet)

**Tier note:** Copy the gate shape of `SliderAllocationTests` and `WidgetAllocationTests`: a `Scene` class that the test builds before the measure, delegates from instance methods that the scene creates one time, `AllocationGate.ReleaseAllocationContext()` before the measured region, and a measured body with no lambda, no `$"`, no new `string`, and no `Assert`. Do not add warm-up rounds to hide an allocation. Do not run a plant and its restore in one parallel tool block (a gotcha of button-and-toggle).

**Tasks:**
- [ ] 6.1 Write `tests/BrutalUi.Core.Tests/Elements/ScrollViewAllocationTests.cs` with 3 tests. The scene: a column root with 10 views (200 x 100), each with 20 rows (300 x 40, the odd rows not pickable), a slider in the first view, the last view in `Disabled`, and a sheet with `ScrollView`, `ScrollView:hover`, `ScrollView:active`, and `:disabled` rules. `UpdateAndDrawAllocateNothingWithScrollViews` (100 rounds of `Update` and `Draw`). `TheScrollViewInputPathAllocatesNothing` (each step pushes, on the first view, a scroll down, a scroll up, a press on a row that is not pickable, a move, a move outside the view, a release, a press and a move on the slider at a scrolled origin, and a release, then runs `Update` and `Draw`). `TheMeasuredBodyDoesRealWork` (after one step: at least one `ScrollChanged` call, at least one `ValueChanged` call, an offset that is not 0 on the first view, and 10 `PushClip` calls in the frame).
- [ ] 6.2 Copy `src/BrutalUi.Core/Elements/ScrollView.cs` to the scratchpad. Plant `GC.KeepAlive(new object());` as the first line of `ScrollView.HandleScroll`. Run `dotnet test --solution brutal-ui.sln > /dev/null 2>&1; echo "exit=$?"` and expect a value that is not 0. Restore the file from the copy, in a later tool call, and check it with `cmp`. Record the result and the gates that failed in `context.md`.
- [ ] 6.3 Edit `docs/technical-design.md` section 5: a "ScrollView" paragraph after the slider paragraph (the offset, the clip by the inline overflow, the content size, the clamp at use, the scroll rule and its sign, the stop rule, the drag, the capture, the disabled rule, and `ScrollChanged`). Edit rule 5 of the widget rules (`WidgetInput` also holds `IsScroll`, and `ViewportOrigin` subtracts the offset of each ancestor view). State the Offset Rule of the Walks.
- [ ] 6.4 Edit `docs/technical-design.md` section 8, "The draw walk" (the rect form: a `ScrollView` moves its children by its offset, and its clip does not move), section 9, item 3 of the flow (the hit rules: the same offset), and section 14 (the scroll view test classes and the eleventh gate with the plant). Edit `CHANGELOG.md` under `## [Unreleased]`: "Added" (`ScrollView`) and "Changed" (the hit walk and the draw walk move the children of a `ScrollView` by its offset).
- [ ] 6.5 Run `python3 /home/aaronstatic/.claude/skills/asd-ste100/scripts/ste-lint.py --fail-over 2.5` on each changed doc and on this plan. Fix each finding above 2.5.
- [ ] 6.6 Run the build, the tests, and `scripts/test-linux.sh; echo "exit=$?"`. Record the final test counts against the counts of task 1.1 in `context.md`.

**Estimated Steps:** about 36 (6 tasks, about six steps for one task)

**Files (5):** `ScrollViewAllocationTests.cs`, `ScrollView.cs` (planted and restored), `technical-design.md`, `CHANGELOG.md`, `context.md`

**Acceptance Criteria:**
- [ ] The 3 tests of `ScrollViewAllocationTests` pass.
- [ ] The planted allocation fails the run, and `cmp` finds no difference after the restore.
- [ ] `scripts/test-linux.sh` prints its PASS line.

---

## Key Technical Decisions

### Confirmed Assumptions

None. The draft had no interview. The open scope questions in Notes hold the open choices.

### Decision 1: A sealed `ScrollView` on the widget pattern
**Choice:** `ScrollView` derives from `Element` directly and follows the ten rules of the widget pattern of button-and-toggle. The new check `IsScroll` goes into `WidgetInput`, next to `IsDragStart` and `IsDragMove`. The view reuses `IsDragStart`, `IsDragMove`, and `IDragCapture` of slider.
**Rationale:** Epic A2 and Decision 1 of slider. One place holds the disabled rule of each check. The public API grows by `ScrollView` only.
**Alternatives:** (a) Private checks in `ScrollView`: the disabled rule drifts. (b) A shared base class for the drag widgets: a second class level, against Decision 1 of button-and-toggle.

### Decision 2: The offset goes into the walks, not into the layout (A7)
**Choice:** The enter step of `PaintWalk` and of `HitWalk` gives the children of a `ScrollView` the origin of the view minus its offset. `WidgetInput.ViewportOrigin` subtracts the offset of each ancestor view. The layout rects do not change.
**Rationale:** The chosen approach, and epic A7. A scroll changes no layout rect, so it marks `Paint` only, and the layout cache stays valid. The content size comes from the rects that the scroll does not change. The three origin rules change in the same way, so the hit, the draw, and the widget maps agree.
**Alternatives:** (a) A layout offset: the engine moves the child rects by the offset. A7 rules it out. Each scroll marks the layout, the cache misses, and the content size must subtract the offset again. (b) A hidden content child with a relative `Top` of minus the offset: a layout pass on each scroll, one more element for each view, and the children of the view go to the wrapper. (c) A translate in `IUiRenderer`: a change of the renderer interface, and the hit walk and `ViewportOrigin` still need the offset.

### Decision 3: The walks find a view with a type test
**Choice:** The walks and `ViewportOrigin` test `element is ScrollView view` and call the internal `AppliedOffset()`.
**Rationale:** `ScrollView` is sealed, so the test is one compare of the type handle. It needs no change of `Element.cs` and adds no member to each element. Only one type scrolls in this epic.
**Alternatives:** (a) An internal virtual member on `Element`: a virtual call for each element of each walk, and a change of `Element.cs`. (b) An internal interface: a slower cast test. (c) An offset field on `Element`: memory on each element for one type. The `ListView` epic can move to (c) when a second type scrolls.

### Decision 4: The content size comes from the direct children
**Choice:** `MaxOffset()` gives, on each axis, `max(0, farEdge + trailingInset - size)`. `farEdge` is the largest `X + Z` (or `Y + W`) of the layout rects of the direct children, or 0 with no child. `trailingInset` is `Layout.ContentInsets.Z` (or `.W`): the trailing padding plus the trailing border. `size` is the width (or the height) of the view. The method reads the layout of the last `Update`.
**Rationale:** Epic A7: "the content size comes from the layout rects of the children". At the maximum offset, the far edge of the content plus the trailing padding meets the trailing edge of the padding box (the clip). A hidden child has a rect of 0, so it adds nothing. An absolute child counts, because its rect is the place where it draws. The loop reads one span and allocates nothing.
**Alternatives:** (a) Each descendant: a subtree walk for each walk of the view. (b) The trailing margin of the last child: the walks must resolve a margin length at draw time. (c) A content size from the layout engine: a change of the engine (A7).

### Decision 5: The walks clamp the stored offset with the last layout (Notes Q3)
**Choice:** `AppliedOffset()` clamps `_offset` with `MaxOffset()`. The walks, `ViewportOrigin`, and the get of `ScrollOffset` read it. A set and a user input write the clamped value.
**Rationale:** The draw reads the tree only (`UiContext.Draw`), so it cannot write a clamp back. A clamp at use shows no space past the end after a content shrink or a resize, with no change of `Update`. The hit and the draw read the same value from the same layout, so they agree. A clamp from a content change is not user input, so it calls nothing. The cost is one loop over the direct children of each view for each walk.
**Alternatives:** (b) A clamp at the set and at user input only: the view shows space past the end until the next input. (c) A clamp pass after the layout pass of `Update`: a tree walk or a list of views in the context, and a change of `UiContext.cs`.

### Decision 6: The view clips through the inline overflow
**Choice:** The constructor sets `Inline.Overflow = Overflow.Scroll`.
**Rationale:** The existing clip of both walks applies, so no clip code changes. `Overflow.Scroll` also tells the layout to give the children no maximum on the main axis (the Yoga rule), so the children overflow. An inline value wins over each rule, so a stylesheet cannot remove the clip by mistake.
**Alternatives:** (a) The walks clip each `ScrollView` with no style: a second clip rule in two walks. (b) The game writes `overflow: scroll`: a view with no rule draws its content outside its box.

### Decision 7: A scroll moves by the delta times the speed, and stops only on a change
**Choice:** `HandleScroll` runs `IsScroll`. The next offset is `Clamp(AppliedOffset() - delta * ScrollSpeed)`, with a non-finite delta component as 0. With no change, the handler returns with no stop. With a change, it stops the walk, writes the offset, and calls `ScrollChanged`. The handler is on the bubble slot `OnScroll`.
**Rationale:** A positive delta scrolls toward the start. The GLFW adapter passes the GLFW offset with no change (`GlfwUiInput.PushScroll`). A wheel away from the user gives a positive y, and the content moves down, as in the GLFW backend of Dear ImGui. The stop on a change only passes the wheel of an inner view at its end to the outer view, and the game gets a wheel that no view uses. The bubble slot lets the innermost view take the wheel first.
**Alternatives:** (a) A stop for each scroll, as the slider stops a drag move with no change: an inner view at its end blocks the outer view. (b) The capture slot: the outer view takes the wheel before the inner view.

### Decision 8: `ScrollSpeed` comes from the create-info
**Choice:** `CreateInfo.ScrollSpeed` in pixels for one unit of the delta. A value of 0 gives `DefaultScrollSpeed` (40). A negative, NaN, or infinite value throws `ArgumentOutOfRangeException` with `ParamName` `info`. `ScrollSpeed` is get-only. The create-info has no `required` member, as the create-info of `Label`.
**Rationale:** The requirement names "a speed". The default struct value is 0, so 0 must give a safe speed. The throw copies CR-7 of slider. A get-only member keeps the API small (YAGNI).
**Alternatives:** (a) A constant: a game cannot tune a list of large rows. (b) A settable member: no requirement asks for it. (c) A speed from the font size: the view has no font.

### Decision 9: The drag copies the slider rule and moves by the pointer difference
**Choice:** The Drag Rule of the Architecture Overview. Each move adds the difference of the last pointer position and the new position to the applied offset. The press writes no offset. Only a press on the view itself starts a drag (Notes Q2).
**Rationale:** The drag rule of slider, as the epic asks. A move by the difference composes with a wheel and with a code set during the drag. At the end of the content, a move back moves the content at once. The capture of slider sends each move to the view outside its rect, with no change of `UiContext.cs`.
**Alternatives:** (a) An anchor: the offset is the press offset plus the full pointer distance. A wheel during the drag snaps back at the next move. (b) A press on each descendant starts the drag: Notes Q2, option B.

### Decision 10: Only user input calls `ScrollChanged`
**Choice:** A scroll and a drag move call `ScrollChanged` when the offset changes, after the stop and the write. A code set, a clamp at use, and a content change call nothing.
**Rationale:** Rule 7 of the widget pattern, Decision 5 and Decision 12 of slider, and epic Q3.
**Alternatives:** A call for each change of the applied offset: a call from a draw or a hit walk, and an echo for a store binding.

### Decision 11: A disabled view takes no input, does not stop the walk, and keeps its offset
**Choice:** `IsScroll`, `IsDragStart`, and `IsDragMove` return `false` for `Disabled`. `CapturesMoves` returns `false` for `Disabled`. The walks still apply the offset. A code set still works.
**Rationale:** Decision 6 of button-and-toggle and Decision 11 of slider. The content does not jump when the game disables the view. A disabled inner view passes the wheel to the outer view.
**Alternatives:** A disabled view that shows the offset 0: the content jumps.

### Decision 12: The view takes no focus and no key (Notes Q4)
**Choice:** `IsFocusable` stays `false`. The view has no key handler.
**Rationale:** The feature requirement names no key. A focusable view takes the click focus for each press on a plain child (`FocusOnPress` walks to the nearest focusable ancestor). A focusable child inside the view stays reachable by Tab.
**Alternatives:** Notes Q4, options B and C.

### Decision 13: `ScrollView` has no own size
**Choice:** No measure callback and no inline size. The remarks advise a width and a height, or a maximum size.
**Rationale:** Decision 10 of slider. A view with no size grows to its content in the layout and never scrolls. `Overflow.Scroll` takes the available size under a maximum size (`LayoutEngine.cs`, the scroll rule of the measured size). widget-sample adds the size rules.
**Alternatives:** A default inline size: a stylesheet can then not size the view.

---

## Risks & Mitigation

| Risk | Likelihood | Impact | Mitigation |
|------|------------|--------|------------|
| The walk change breaks an existing hit or draw test. | Low | High | Only a `ScrollView` changes the child origin (Decision 3). The tier notes of phases 1 and 2 limit the change. Each walk, pointer, widget, and slider class runs with no edit. Task 1.1 and task 2.1 record the blast radius. |
| The hit and the draw disagree. | Low | High | One method, `AppliedOffset`, serves both walks and `ViewportOrigin` (Decision 5). `TheHitAndTheDrawAgree` and `TheOffsetsAddUpInTheHitAndTheDraw` pin it. |
| A widget inside a view maps the pointer with no offset. | Medium | High | `ViewportOrigin` subtracts the offset of each ancestor view (task 2.3). `ScrollViewSliderTests` and `TheOriginSubtractsEachAncestorOffset` pin it. |
| The stale ref gotcha: `Enter` grows the stack, and the loop reads a stale frame. | Medium | High | Task 2.2 copies `top.ChildOrigin` before the call, next to the existing copies. Phase 1 renames the field that the loop already copies. |
| The clamp at use costs a loop over the children for each walk. | Medium | Low | The loop reads one span and allocates nothing. It runs for a `ScrollView` only. The `ListView` epic adds virtualization. The gate of phase 6 measures 0 bytes. |
| `Hover` stays on the old element after a wheel scroll under a still pointer. | High | Low | The next pointer event moves `Hover`. The remarks state it. Conflicts and Gaps item 5. |
| A press on a pickable child gives no drag, so a list of plain rows does not drag. | Medium | Medium | Notes Q2. The remarks advise `IsPickable = false` on a decorative child. `APressOnANonPickableChildDrags` pins it. |
| The x sign of the wheel differs on a platform. | Low | Low | The view follows the GLFW convention of the adapter (Decision 7). widget-sample checks a horizontal wheel on screen. Conflicts and Gaps item 7. |
| A set before the first `Update` gives (0, 0). | Medium | Low | The remarks state it. `ASetBeforeTheFirstUpdateGivesZero` pins it. |
| A view with no size takes the size of its content and never scrolls. | Medium | Low | Decision 13. The remarks state it. widget-sample adds the size rules. |
| An ancestor capture handler stops the press, and the drag reads an old `_dragPointer`. | Low | Medium | The known debt of the epic (a stop of the press does not stop a drag). The first move jumps by the old distance. Conflicts and Gaps item 9. |
| A view allocates in the input path. | Low | High | Cached static delegates. `ScrollViewAllocationTests` and the plant of task 6.2. |

---

## Dependencies

### Internal Dependencies
- `interactive-elements/slider`: `IDragCapture`, the capture branch of `RoutePointer` (with `IsInRootTree` and `FocusWalk.IsShown`), `UiContext.CurrentPressTarget`, `WidgetInput.IsDragStart`, `IsDragMove`, and `ViewportOrigin`, and the Drag Rule. The caller commits slider before this feature starts. Task 1.1 checks it.
- `interactive-elements/button-and-toggle`: the widget pattern, `WidgetInput`, and the delegate slot shape (`21abb3c`).
- `input-routing`: `ScrollEvent`, `OnScroll`, the scroll route of `RoutePointer`, `Hover`, `Active`, `HitWalk`, `PointerTree.Scroll`, and `ScrollRoutingTests`.
- `core-tree` and the layout engine: `Element`, `ElementType.Intern`, `MarkDirty`, `LayoutNode.Rect`, `LayoutNode.ContentInsets`, `InlineStyle.Overflow`, and the `Overflow.Scroll` rule of the engine.
- `imgui-renderer`: `IUiRenderer.PushClip`, `PopClip`, `Rect`, and `RecordingRenderer`.
- `style-system`: the type selector and `PseudoStates.Disabled`.

### External Dependencies
- `Brutal.Numerics`. It is pinned already. No new package and no native dependency.
- The GLFW scroll convention: the adapter passes the GLFW x and y offsets as `ScrollEvent.Delta`. The Dear ImGui GLFW backend passes the same values to `io.MouseWheelH` and `io.MouseWheel` ([imgui_impl_glfw.cpp](https://skia.googlesource.com/external/github.com/ocornut/imgui/+/923bd2fd217c1dc1e75fa92b0284d3817904988b/backends/imgui_impl_glfw.cpp), [horizontal wheel support, #1463](https://skia.googlesource.com/external/github.com/ocornut/imgui/+/7dea158175615dc8939c57a06641bac911e33e8c%5E%21)).

---

## Definition of Done

All criteria below must pass. An independent evaluator runs the Verification Method from `/home/aaronstatic/dev/brutal-ui` on Linux. `<base>` is the base commit that task 1.1 records in `context.md`.

### Functional Criteria
- [ ] F1. A scroll with the delta (0, -1) over `Row0` of `ScrollViewTree`, pushed with no target and followed by `Update` and `Draw`, gives `ScrollOffset` (0, 40), one `ScrollChanged` call with the view as the sender, and the rect of `Row0` at (0, -40, 300, 40) between `PushClip(0, 0, 200, 100)` and its `PopClip` (`ScrollViewScrollTests.AScrollMovesTheContent`).
- [ ] F2. A scroll moves back, moves the x, stops at each end, walks on with no change, stops the walk on a change, and scales with the speed. A code set clamps and calls nothing. The type id, the inline overflow, the focus rule, and the speed check hold. A type rule matches the view, and a rule cannot remove the clip (`ScrollViewScrollTests`, 10 tests).
- [ ] F3. With an offset, a pointer event hits the element under the pointer in the draw. The clip does not move. The gap and a child that is not pickable hit the view. A scroll walks the path of the shifted child. An element outside the view keeps its hit. The center of each drawn rect hits its element (`ScrollViewHitTests`, 6 tests).
- [ ] F4. A press and a drag of a slider inside a view at the offset (20, 20) give the value under the pointer. A slider drag past the view stays on the slider. A wheel over the slider scrolls the view. A scroll and a press in one drain use the new offset. `ViewportOrigin` subtracts the offset of each ancestor view and not of the element itself (`ScrollViewSliderTests`, 6 tests).
- [ ] F5. A left press on the view and a move drag the content with the pointer, also outside the view. A drag at the end comes back at once. A drag move stops the walk also with no change. A press on a pickable child and a right press give no drag. A press on a child that is not pickable drags. The capture ends at the release. A wheel during a drag adds up (`ScrollViewDragTests`, 9 tests).
- [ ] F6. After a content shrink, the walks and `ScrollOffset` show the end at once, with no call. The stored offset comes back when the content grows. A user scroll starts from the shown offset. A set before the first update gives 0. NaN and infinity follow the rule. The padding, the border, and an absolute child count, and a hidden child does not. A view larger than its content does not scroll. A view resize clamps (`ScrollViewClampTests`, 10 tests).
- [ ] F7. An inner view at its end passes the wheel to the outer view, and an inner view that moves keeps it. The offsets add up in the hit, the draw, the clip, and `ViewportOrigin`. A drag on the inner view moves the inner view only. A disabled inner view passes the wheel (`ScrollViewNestedTests`, 6 tests).
- [ ] F8. A disabled view takes no wheel, no drag, and no capture, does not stop the walk, and keeps its offset in the walks. A code set works (`ScrollViewDisabledTests`, 5 tests).
- [ ] F9. `ScrollViewAllocationTests` measures 0 bytes for `Update` and `Draw` and for the full input path (3 tests). A planted allocation in `ScrollView.HandleScroll` gives a non-zero exit code of `dotnet test`.

### Quality Criteria
- [ ] Q1. `dotnet build brutal-ui.sln` gives `0 Warning(s)` and `0 Error(s)`.
- [ ] Q2. `dotnet test --solution brutal-ui.sln` gives 0 failed. `BrutalUi.Core.Tests` has 55 more tests than the count of task 1.1 (10 + 6 + 6 + 9 + 10 + 6 + 5 + 3).
- [ ] Q3. `ScrollView` is `public sealed`. `IDragCapture` and `WidgetInput` stay internal. `MaxOffset` and `AppliedOffset` are internal. `ScrollView.CreateInfo` has no `required` member. `ScrollChanged` is `Action<ScrollView>?`.
- [ ] Q4. `ScrollView.cs` creates no delegate outside a `static readonly` field initializer and holds no lambda. It holds three `static readonly UiEventHandler` fields.
- [ ] Q5. No existing test file has a diff against `<base>`.
- [ ] Q6. The changed docs and the new doc comments use ASD-STE100 prose. The lint gives no score above 2.5.

### Integration Criteria
- [ ] I1. `BrutalUi.Core` has no ImGui or GLFW `using`, and no `.csproj` has a diff.
- [ ] I2. These files have no diff against `<base>`: `UiContext.cs`, `Element.cs`, `PseudoStates.cs`, `ElementHandlers.cs`, `EventWalk.cs`, `FocusWalk.cs`, `PointerEvent.cs`, `ScrollEvent.cs`, `IDragCapture.cs`, `LayoutNode.cs`, `src/BrutalUi.Core/Layout/`, `StylePass.cs`, `IUiRenderer.cs`, `Slider.cs`, `Button.cs`, `Toggle.cs`, `Label.cs`, `Image.cs`, `Directory.Packages.props`, `src/BrutalUi.ImGui/`, `src/BrutalUi.Glfw/`, `samples/`, and `scripts/`.
- [ ] I3. In `src/`, the feature adds `ScrollView.cs` and changes `WidgetInput.cs`, `HitWalk.cs`, and `PaintWalk.cs` only. In the two walks, only the frame fields, step 3 and the frame push of `Enter`, the copy of the child origin in the loop, and doc comments change.
- [ ] I4. Section 5 of `docs/technical-design.md` holds the scroll view paragraph and the Offset Rule of the Walks. Section 8 ("The draw walk") and section 9 (item 3 of the flow) state the offset. Section 14 names the scroll view classes and the eleventh gate. `CHANGELOG.md` names `ScrollView` and the change of the walks.
- [ ] I5. `scripts/test-linux.sh` prints its PASS line.

### Verification Method

Run each step from `/home/aaronstatic/dev/brutal-ui` on Linux. `<scratch>` is any empty folder outside the repository. Read `<base>` from `docs/features/interactive-elements/scroll-view/context.md`.

1. `dotnet build brutal-ui.sln 2>&1 | tail -n 30`. Expect `0 Warning(s)` and `0 Error(s)` (Q1).
2. `dotnet test --solution brutal-ui.sln 2>&1 | tail -n 30`. Expect 0 failed. Compare the Core total with the count of task 1.1 in `context.md` (Q2).
3. For each class, run `dotnet run --project tests/BrutalUi.Core.Tests/BrutalUi.Core.Tests.csproj -- --filter-class '*<Class>' 2>&1 | tail -n 5`. Expect `failed: 0` and these totals: `ScrollViewScrollTests` 10 (F1, F2), `ScrollViewHitTests` 6 (F3), `ScrollViewSliderTests` 6 (F4), `ScrollViewDragTests` 9 (F5), `ScrollViewClampTests` 10 (F6), `ScrollViewNestedTests` 6 (F7), `ScrollViewDisabledTests` 5 (F8), and `ScrollViewAllocationTests` 3 (F9). Read the test names in each file and match them to the criteria.
4. Copy `src/BrutalUi.Core/Elements/ScrollView.cs` to `<scratch>`. Add `GC.KeepAlive(new object());` as the first line of `HandleScroll`. Run `dotnet test --solution brutal-ui.sln > /dev/null 2>&1; echo "exit=$?"`. Expect a value that is not 0. Restore the file from the copy, and check it with `cmp`. Run step 2 again (F9).
5. `git diff <base> --diff-filter=M --name-only -- tests/`. Expect no line (Q5).
6. `git diff <base> --stat -- src/BrutalUi.Core/Tree/UiContext.cs src/BrutalUi.Core/Tree/Element.cs src/BrutalUi.Core/Tree/PseudoStates.cs src/BrutalUi.Core/Tree/LayoutNode.cs src/BrutalUi.Core/Input/ElementHandlers.cs src/BrutalUi.Core/Input/EventWalk.cs src/BrutalUi.Core/Input/FocusWalk.cs src/BrutalUi.Core/Input/PointerEvent.cs src/BrutalUi.Core/Input/ScrollEvent.cs src/BrutalUi.Core/Input/IDragCapture.cs src/BrutalUi.Core/Layout/ src/BrutalUi.Core/Style/StylePass.cs src/BrutalUi.Core/Rendering/IUiRenderer.cs src/BrutalUi.Core/Elements/Slider.cs src/BrutalUi.Core/Elements/Button.cs src/BrutalUi.Core/Elements/Toggle.cs src/BrutalUi.Core/Elements/Label.cs src/BrutalUi.Core/Elements/Image.cs src/BrutalUi.ImGui/ src/BrutalUi.Glfw/ Directory.Packages.props '*.csproj' samples/ scripts/`. Expect no output (I1, I2). When a path of this list does not exist, find the file with `git ls-files` and use its path.
7. `git diff <base> --name-status -- src/` and `git status --porcelain -- src/`. Together they list only `src/BrutalUi.Core/Elements/ScrollView.cs` (added or untracked) and `WidgetInput.cs`, `HitWalk.cs`, and `PaintWalk.cs` (modified). Read `git diff <base> -- src/BrutalUi.Core/Input/HitWalk.cs src/BrutalUi.Core/Rendering/PaintWalk.cs`, and expect the parts of I3 only.
8. `grep -n '^public sealed class ScrollView : Element, IDragCapture' src/BrutalUi.Core/Elements/ScrollView.cs` gives one line. `grep -c 'static readonly UiEventHandler' src/BrutalUi.Core/Elements/ScrollView.cs` gives 3. `grep -nE '\(ref [A-Za-z]+ e\) =>|new UiEventHandler' src/BrutalUi.Core/Elements/ScrollView.cs` gives no line. `grep -nE '^\s*public required' src/BrutalUi.Core/Elements/ScrollView.cs` gives no line. `grep -n 'Inline.Overflow = Overflow.Scroll' src/BrutalUi.Core/Elements/ScrollView.cs` gives one line. `grep -nE 'internal float2 (MaxOffset|AppliedOffset)\(\)' src/BrutalUi.Core/Elements/ScrollView.cs` gives two lines. `grep -c 'is ScrollView' src/BrutalUi.Core/Input/HitWalk.cs src/BrutalUi.Core/Rendering/PaintWalk.cs src/BrutalUi.Core/Elements/WidgetInput.cs` gives a count of 1 or more for each file (Q3, Q4).
9. `grep -rnE 'using .*(ImGui|Glfw)' src/BrutalUi.Core` gives no line (I1).
10. `scripts/test-linux.sh; echo "exit=$?"`. Expect the PASS line and `exit=0` (I5).
11. Read section 5 (the scroll view paragraph, rule 5 of the widget rules, and the Offset Rule of the Walks), section 8 ("The draw walk"), section 9 (item 3 of the flow), and section 14 of `docs/technical-design.md`, and the `## [Unreleased]` part of `CHANGELOG.md` (I4). Run `python3 /home/aaronstatic/.claude/skills/asd-ste100/scripts/ste-lint.py --fail-over 2.5` on each changed doc (Q6).

---

## Testing Strategy

### Unit Tests
- `Elements/ScrollViewScrollTests` (10): the demo, back, the x axis, the end, the start, the stop, the speed, the code set, the create-info and the overflow, and the type rule.
- `Elements/ScrollViewHitTests` (6): the hit with an offset, the fixed clip, the gap and a child that is not pickable, the path of a scroll, an element outside, and the hit-draw agreement.
- `Elements/ScrollViewSliderTests` (6): the slider press and the slider drag through a scrolled origin, a slider drag past the view, a wheel over the slider, a scroll and a press in one drain, and `ViewportOrigin`.
- `Elements/ScrollViewDragTests` (9): the drag, the capture, the end, the stop, a pickable child, a child that is not pickable, the right press, the release, and a wheel during a drag.
- `Elements/ScrollViewClampTests` (10): the content shrink, the growth, a user scroll after a shrink, a set before the update, NaN and infinity, a non-finite delta, the padding and the border, an absolute and a hidden child, a large view, and a resize.
- `Elements/ScrollViewNestedTests` (6): the wheel at the end of an inner view, an inner view that moves, the sum in the hit and the draw, `ViewportOrigin`, a drag on the inner view, and a disabled inner view.
- `Elements/ScrollViewDisabledTests` (5): the wheel, the drag, a disable during a drag, the walks, and a code set.
- Each test except the nested class uses `ScrollViewTree`. Each test pushes events with no target through `UiContext.PushEvent` and calls `Update` or `DrainEvents`. A test computes the expected points by hand, because `PointerTree.ViewportRect` ignores the offset.

### Allocation Tests
- `Elements/ScrollViewAllocationTests` (3): `Update` and `Draw`, the input path with a captured drag and a slider drag inside a scrolled view, and the real-work check. widget-sample adds the 1000-element gate with each widget (epic A9).

### Regression Tests
- The regression set of the phase intro with no edit in each phase, and each class in `tests/BrutalUi.Core.Tests/Input/` in phases 2 and 3.

### Integration Tests
- `scripts/test-linux.sh` runs the full suite and the HelloPanel smoke run. HelloPanel does not change. widget-sample adds the view to it.

### Manual Testing
- None in this feature. widget-sample adds the visible demo and the manual check of a wheel, a horizontal wheel, and a drag on screen.

---

## Related Documentation

- **Requirements:** `docs/features/interactive-elements/scroll-view/requirements.md`
- **Epic:** `docs/features/interactive-elements/epic-overview.md`, `docs/features/interactive-elements/epic-requirements.md`
- **Pattern sources:** `docs/features/interactive-elements/button-and-toggle/implementation.md` ("The Widget Pattern"), `docs/features/interactive-elements/slider/implementation.md` ("The Drag Rule", "Internal Types", Decisions 2, 3, 5, 10, 11, 12, and 13), the slider `context.md` ("Review Fixes", CR-1 and CR-7), and the slider `code-review.md`
- **Siblings:** `docs/features/interactive-elements/text-field/requirements.md` (reads the clip rule), `widget-sample/requirements.md`
- **Design:** `docs/technical-design.md` sections 5 (Element Tree, Events, Widgets, "The pointer step of the drain"), 7 (pseudo-states), 8 ("The draw walk"), 9 (Input), and 14 (Testing)
- **Rules:** `AGENTS.md`, `.beast-mode/phase-handoff.md` sections 1 and 11, `.beast-mode/tool-rules.md`
- **Code patterns:** `src/BrutalUi.Core/Elements/Slider.cs` (the drag, the capture, `ValidateRange`), `src/BrutalUi.Core/Elements/WidgetInput.cs` (the checks and `ViewportOrigin`), `src/BrutalUi.Core/Input/HitWalk.cs` and `src/BrutalUi.Core/Rendering/PaintWalk.cs` (the enter step and the stale ref gotcha), `src/BrutalUi.Core/Tree/UiContext.cs` (`RoutePointer`, `DrainEvents`, `Draw`), `src/BrutalUi.Core/Layout/LayoutEngine.Lines.cs` (the scroll rule of the child limits), `tests/BrutalUi.Core.Tests/Input/ScrollRoutingTests.cs` (the scroll route), `tests/BrutalUi.Core.Tests/Input/PointerTree.cs` (the event factories), `tests/BrutalUi.Core.Tests/Elements/SliderTree.cs` (the tree shape), and `tests/BrutalUi.Core.Tests/Elements/SliderAllocationTests.cs` (the gate)
- **Skills:** no implementation skill in `.claude/skills/` applies. The skills there are workflow commands.

---

## Notes

### Open Scope Questions

No person answered these questions. The plan uses the recommended option of each one.

**Q1. What is the demo of phase 1?**
- **A (recommended):** a unit test pushes a scroll with the delta (0, -1) over `Row0` through `UiContext.PushEvent`, and runs `Update` and `Draw` with a `RecordingRenderer`. It reads `ScrollOffset` (0, 40), one `ScrollChanged` call, and the rect of `Row0` at (0, -40, 300, 40) inside the clip (0, 0, 200, 100). The developer runs the class filter `'*ScrollViewScrollTests'` and reads a total of 10 with `failed: 0`. Reason: it shows the scroll and the draw in one test, the form of the slider demos.
- B: a person runs `scripts/test-linux.sh` and reads its PASS line, with the demo test in the run. Reason: the full suite and the smoke run, but the line does not show the scroll.
- C: a person scrolls a view in HelloPanel on screen. Reason: it moves widget-sample work into this feature and needs a GPU.

**Q2. Which press starts a drag?**
- **A (recommended):** a left press on the view itself: its padding, a gap, or a child with `IsPickable = false`. A press on a pickable child gives no drag. Reason: it reuses the press target and the capture of slider with no change of `UiContext.cs`, and a button inside the view still clicks.
- B: a press on each descendant that does not stop the press. Reason: a list of plain rows drags, but `RoutePointer` needs a search for the nearest capture ancestor, and a child button needs a drag threshold and a click cancel.
- C: no drag. Only the wheel scrolls. Reason: the smallest code, but it drops a requirement.

**Q3. What happens to the offset after a content change?**
- **A (recommended):** the walks and the get of `ScrollOffset` clamp the stored offset with the last layout (Decision 5). A shrink shows the end at once. When the content grows before the next input, the old offset comes back. Reason: no space past the end, and no change of `Update` or of the read-only draw.
- B: only a set and a user input clamp. Reason: the simplest code, but a shrink shows space past the end until the next input.
- C: a clamp pass after the layout pass of `Update` writes the clamp back. Reason: no jump back, but `UiContext.cs` changes and needs a tree walk or a list of views.

**Q4. Does the view take the keyboard focus?**
- **A (recommended):** no. The view is not focusable and has no key (Decision 12). Reason: the feature requirement names no key, and a focusable view takes the click focus from each plain child.
- B: the view is focusable, and Up, Down, Page Up, Page Down, Home, and End move the offset. Reason: it meets "each widget is keyboard reachable" of the epic, but it adds a phase and a key rule that the action map epic can own.
- C: the view is focusable with no key. Reason: a Tab stop that does nothing.

### Conflicts and Gaps

1. **The build order and the slider code.** The epic overview says that slider and scroll-view have no dependency and can be built in parallel. This plan uses the slider code: `IDragCapture`, the capture branch of `RoutePointer`, `CurrentPressTarget`, `IsDragStart`, `IsDragMove`, and `ViewportOrigin`. The brief states that the caller commits slider before this feature starts. Task 1.1 checks it and stops when slider is not in the base commit.
2. **Epic: "each widget is keyboard reachable".** `epic-requirements.md` says each widget is keyboard reachable through the focus owner and Tab. The feature requirement names no key. The plan makes the view not focusable (Notes Q4). A focusable child inside the view stays reachable. A person decides.
3. **The drag on a child (main gap).** The requirement says "a drag with the primary button moves the offset" and "a scroll event on the view or on a child". It does not name a drag on a child. The press target of slider is the hit element, so only a press on the view itself drags (Notes Q2). The remarks advise `IsPickable = false` on decorative children.
4. **The clamp after a content change.** The requirement says "a set clamps". It does not name a content change. Decision 5 clamps at use (Notes Q3).
5. **`Hover` after a wheel.** A wheel moves the content under a still pointer. `Hover` and `PointerHit` stay on the old element until the next pointer event. The plan adds no hit after a scroll.
6. **No scroll into view.** A Tab to a focusable child outside the visible part does not scroll the view. The `ListView` epic or the action map can add it.
7. **The sign of the horizontal wheel.** The view uses the GLFW convention of the adapter (a positive delta scrolls toward the start, Decision 7). Shift with a vertical wheel does not scroll the x axis. widget-sample checks a horizontal wheel on screen.
8. **The content size.** It counts the direct children only, and no margin (Decision 4). A grandchild that overflows its row does not add to the content. Add a padding to the view to keep space after the last child.
9. **Known debt of the epic.** `RoutingGate.PointerOverUi` follows the hit during a captured view drag. A capture handler on an ancestor that stops the press does not stop the drag, and the first move then reads an old `_dragPointer`. `:disabled` on an ancestor does not block the view. This plan changes none of these rules.
10. **The press stops the walk with no content to scroll.** A press on a view that cannot scroll still stops the walk (item 1 of the Drag Rule). A root bubble handler then does not get it.
11. **text-field.** text-field reads the clip rule. It can clip its text with `Overflow.Hidden` and move the text in its own `PaintContent`. It does not need a `ScrollView`, and it needs no change of the walks.
12. **No change of an existing test file.** `ScrollViewTree` is a new scene. `PointerTree`, `SliderTree`, and `WidgetTree` stay as they are. A review fix extends an existing test method and adds no test method, so the class totals of the Definition of Done stay.
13. **The create-info rule.** `AGENTS.md` asks for `required` create-info structs. `ScrollView.CreateInfo` has no member with no safe default, so it has no `required` member, as `Label.CreateInfo` (Decision 8).
14. **The prose lint.** The planner had no shell, so it did not run `ste-lint.py` on this file. Task 6.5 runs it.

---

*This plan will be updated as implementation progresses. See context.md and tasks.md for current status.*
