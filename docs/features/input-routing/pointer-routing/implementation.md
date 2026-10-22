# Pointer Routing - Implementation Plan

**Epic:** input-routing (feature 2 of 4)
**Status:** Ready for Review
**Started:** 2026-10-22
**Target Completion:** TBD
**Last Updated:** 2026-10-22 12:50

---

## Quality Bar

**Target quality:** A core library feature plus a thin platform adapter. keyboard-focus, routing-gate, and the interactive-elements epic build on the hit result, the states, and the adapter project. A wrong hit, a stuck state, or a hidden allocation here breaks each consumer.

- **The hit is exact:** The hit test finds the element that the draw shows on top at the point. The roots go from the last to the first. The children go from the last to the first. A descendant comes before its ancestor. A test asserts each rule with known rects.
- **The clip is the draw clip:** The hit test uses the same padding-box clip and the same hidden rule as `PaintWalk`. It shares the clip helpers with `PaintWalk`, so the two cannot drift.
- **The states are exact:** `Hover` is on one element at most, and it is on the last hit. `Active` is on the press target until the last button goes up. No state stays on an element that left the tree after the next drain.
- **The walk does not change:** The pointer events walk through the generic walk of event-model. `EventWalk.cs` does not change in this feature.
- **0 bytes after warm-up:** The hit test on a warm 1000-element tree allocates 0 bytes. The full pointer path (push, drain, hit test, state change, walk, and `Update`) allocates 0 bytes. The adapter handlers allocate 0 bytes. A planted allocation fails a gate.
- **No GLFW in Core:** `BrutalUi.Core` gets no package and no `using` of GLFW. Only `BrutalUi.Glfw` references `Ahwoo.BRUTAL.Glfw`.
- **No recursion:** The hit test uses an explicit stack on `UiContext`, as `PaintWalk` does.

---

## Executive Summary

This feature adds the hit test, the `Hover` and `Active` states, the scroll routing, the window callbacks, and the new `BrutalUi.Glfw` package. The internal `HitWalk` walks the tree with an explicit stack, from the top-most element down, with the clip rects, the hidden rule, and `IsPickable`. The drain of `UiContext` runs the hit test in its target step: a pointer or scroll entry with no target gets the element under its position as its target. The same step moves `Hover` to the hit element, sets `Active` on a press target, and stores the last hit result for routing-gate. The walk of event-model then sends the event through capture, target, and bubble. `UiContext` gets two public callback slots, `OnFileDrop` and `OnContentScale`, with no queue and no routing. `GlfwUiInput` in `BrutalUi.Glfw` subscribes to the cursor position, mouse button, scroll, cursor enter, drop, and content-scale events of `GlfwWindow`. It converts each one into a plain struct and pushes it, or calls a callback. Unit tests drive the Core path with hand-made events. A test of the adapter calls its internal handlers with no window. `UiContext.Update` does not call the drain yet. routing-gate wires it.

---

## User Personas Alignment

No `docs/user-personas.md` file exists. The personas below come from `docs/mission-statement.md` and the epic docs.

**Target Users:**
- [x] **The game developer** (creates one `GlfwUiInput` for the window and the context. Writes a `:hover` rule and an `:active` rule and sees them after routing-gate wires the drain)
- [x] **The sibling feature agents** (keyboard-focus adds the click focus in the pointer step of the drain and the key callbacks in the adapter. routing-gate reads the last hit result and wires the drain)
- [x] **The interactive-elements epic** (`Button` reads `Active`. `ScrollView` gets scroll events at the element under the pointer)
- [x] **The maintainer** (runs two more 0-byte gates)
- [ ] **The markup author** (not served. The state rules use the existing style syntax)

**User Subsets Served:**
- [x] **Simple** - `using GlfwUiInput input = new(new GlfwUiInput.CreateInfo { Window = window, Context = ui });` and a `.button:hover` rule.
- [x] **Complex** - `IsPickable = false` on a layout-only element, a capture handler on a panel, and the drop and content-scale callbacks.

**UX Approach:**
- A null target on a pointer or scroll event means "hit-test the position". A target from the caller skips the hit test and the states. A test or a synthetic event can still aim at one element.
- A wrong create-info (a null window or a null context) throws `ArgumentNullException` with the member name in the message.

---

## Goals

### Primary Goals
1. Find the element under the pointer with the clip rects, the hidden rule, and `IsPickable` (A3).
2. Set and clear `Hover` and `Active`, so the pseudo-state rules restyle the element.
3. Send pointer and scroll events to the hit element through the walk of event-model.
4. Store the last hit result for routing-gate (A6).
5. Add `BrutalUi.Glfw` with the adapter. Keep Core free of GLFW (Q1, A1).
6. Send the drop and content-scale events to `UiContext` callbacks (A4).
7. Allocate 0 bytes in the hit test on a warm 1000-element tree.

### Success Criteria
- [ ] The phase 1 demo test passes: a move over a child sets `Hover`, and the `:hover` fill shows after `Update`.
- [ ] A press sets `Active` on the hit element, and the last release clears it.
- [ ] A scroll walks to the element under the pointer.
- [ ] The clip, hidden, pickable, edge, and NaN rules hold (`HitTestRuleTests`).
- [ ] `GlfwUiInput` pushes the right structs and calls the callbacks. `dotnet pack` gives a `BrutalUi.Glfw` package with two dependencies.
- [ ] Two gates measure 0 bytes, and a planted allocation fails one of them.
- [ ] `BrutalUi.Core` has no GLFW reference.

---

## Architecture Overview

### File Tree (end state)

```
src/BrutalUi.Core/
├── Input/HitWalk.cs                        # phase 1 (rects, order), phase 3 (clip, hidden, pickable)
├── Input/UiWindowEvents.cs                 # phase 4: the two public callback delegates
├── Input/UiEventHeader.cs                  # phase 1: the remarks of Target (a null target is hit-tested)
├── Rendering/PaintWalk.cs                  # phase 3: PaddingBounds and Intersect become internal (no body change)
├── Tree/UiContext.cs                       # phases 1, 2, 4, 5: the hit stack, the pointer step, the states, the callbacks
└── BrutalUi.Core.csproj                    # phase 4: InternalsVisibleTo BrutalUi.Glfw.Tests
src/BrutalUi.Glfw/                          # phase 4: new package
├── BrutalUi.Glfw.csproj
└── GlfwUiInput.cs
tests/BrutalUi.Core.Tests/Input/
├── PointerTree.cs                          # phase 1: the shared test tree with known rects
├── HitTestTests.cs                         # phase 1
├── PointerHoverTests.cs                    # phase 1
├── PointerPressTests.cs                    # phase 2
├── ScrollRoutingTests.cs                   # phase 2
├── HitTestRuleTests.cs                     # phase 3
├── WindowCallbackTests.cs                  # phase 4
├── PointerStateEdgeTests.cs                # phase 5
└── PointerAllocationTests.cs               # phase 6
tests/BrutalUi.Glfw.Tests/                  # phase 4: new test project
├── BrutalUi.Glfw.Tests.csproj
├── GlfwUiInputTests.cs                     # phase 4
└── GlfwUiInputAllocationTests.cs           # phase 6
brutal-ui.sln                               # phase 4: two projects
docs/technical-design.md                    # phase 6: sections 3, 4, 5, 9, 14
README.md, CHANGELOG.md                     # phase 6
```

`EventWalk.cs`, `Directory.Packages.props`, `ThousandElementAllocationTests.cs`, and `scripts/` do not change.

### Public API (end state)

Each public member gets an XML doc comment (CS1591 is an error).

```csharp
namespace BrutalUi.Core;

public delegate void UiFileDropHandler(ReadOnlySpan<string> paths);   // phase 4
public delegate void UiContentScaleHandler(float2 scale);             // phase 4

public sealed class UiContext
{
    public UiFileDropHandler? OnFileDrop { get; set; }            // phase 4 (A4)
    public UiContentScaleHandler? OnContentScale { get; set; }    // phase 4 (A4)
    public void RaiseFileDrop(ReadOnlySpan<string> paths);        // phase 4: calls the slot at once
    public void RaiseContentScale(float2 scale);                  // phase 4: calls the slot at once

    internal Element? HitTest(float2 position);                   // phase 1
    internal Element? PointerHit { get; }                         // phase 1: the last hit result, read by routing-gate (A6)
    internal float2 PointerPosition { get; }                      // phase 1
    internal bool HasPointerPosition { get; }                     // phase 1
    internal Element? ActiveElement { get; }                      // phase 2
    internal int HitStackLength { get; }                          // phase 1, for the tests
    internal ReadOnlySpan<HitFrame> HitStackSlots { get; }        // phase 1, for the tests
}

namespace BrutalUi.Glfw;

public sealed class GlfwUiInput : IDisposable
{
    public struct CreateInfo
    {
        public required GlfwWindow Window;
        public required UiContext Context;
    }

    public GlfwUiInput(in CreateInfo info);
    public UiContext Context { get; }
    public void Dispose();

    // The test seam: an adapter with no window. It subscribes to no event.
    internal GlfwUiInput(UiContext context);

    // The GLFW handlers convert, then call these. The tests call them with no window.
    // After Dispose, each method returns at once.
    internal void PushCursorMove(float2 position, UiModifiers modifiers, double timestamp);
    internal void PushButton(int button, bool press, UiModifiers modifiers, double timestamp);
    internal void PushScroll(float2 delta, UiModifiers modifiers, double timestamp);
    internal void PushCursorLeave(double timestamp);
}
```

The adapter files use the alias `using GlfwApi = Brutal.GlfwApi.Glfw;`. Inside `namespace BrutalUi.Glfw`, the simple name `Glfw` finds the namespace, not the static class. `BrutalUi.ImGui` uses `using Gui = Brutal.ImGuiApi.ImGui;` for the same cause.

### Internal Types

- `HitFrame`: an internal struct with `Element`, `Origin` (`float2`, the border-box position in viewport pixels), `Opacity` (`float`, the product from the root), `OwnClip` (`float4` bounds from the parent: min x, min y, max x, max y), `ChildClip` (`float4` bounds for the children), and `NextChild` (`int`, counts down). Phase 1 uses `Element`, `Origin`, and `NextChild`. Phase 3 adds the other fields.
- `HitWalk`: an internal static class (the `PaintWalk` pattern). `InitialStackSize = 32`. `Find(ReadOnlySpan<Element> roots, float2 point, ref HitFrame[] stack)` returns the hit element or null.

### Data Flow

```
Glfw.PollEvents() ──► GlfwUiInput (BrutalUi.Glfw)
   OnCursorPos          ──► PushEvent(PointerEvent PointerMove, Target null)
   OnMouseButton        ──► PushEvent(PointerEvent PointerDown | PointerUp, Target null, the last cursor position)
   OnScroll             ──► PushEvent(ScrollEvent, Target null, the last cursor position)
   OnCursorEnter(false) ──► PushEvent(PointerEvent PointerMove at (NaN, NaN))          (Notes Q3)
   OnDrop               ──► UiContext.RaiseFileDrop(paths)    ──► OnFileDrop           (A4, at once)
   OnContentScale       ──► UiContext.RaiseContentScale(scale) ──► OnContentScale      (A4, at once)
   ▼
UiContext.DrainEvents()   (internal. routing-gate calls it from Update before the style pass, A5)
   0. DropStalePointerState: Hover or Active on an element outside the roots clears (phase 5)
   for each entry (the count at the start):
     target step:
       Target is null and Type is PointerMove, PointerDown, PointerUp, or Scroll:
         RoutePointer(ref entry):
           hit = HitWalk.Find(roots, position, ref _hitStack)   (the layout of the last Update)
           entry.Header.Target = hit
           PointerHit = hit, PointerPosition = position
           SetHover(hit)                                        (phase 1)
           PointerDown: set the button bit. Active on hit when no element is active  (phase 2)
           PointerUp: clear the button bit. At no bit, clear Active                  (phase 2)
           [keyboard-focus adds the click focus here]
       skip a null target or a target outside the roots (event-model)
     EventWalk.Dispatch<T> (unchanged)
   ▼
UiContext.Update: the style pass restyles the subtrees that the state change marked (:hover, :active)
```

### HitWalk.Find (end state)

```
for r = roots.Length - 1 .. 0:
  Enter(roots[r], parent origin (0, 0), opacity 1, NoClip)
  while depth > 0:
    top = stack[depth - 1]
    if top.NextChild >= 0: child = children[top.NextChild--]; Enter(child, top.Origin, top.Opacity, top.ChildClip)
    else:  (all children done, the element itself comes after its descendants)
      if top.Element.IsPickable and point in border box (half-open) and point in top.OwnClip: return top.Element
      pop
Enter(element, parentOrigin, opacity, parentClip):
  Display.None or opacity product <= 0: skip the subtree         (the PaintWalk hidden rule, Notes Q4)
  point not in parentClip: skip the subtree                       (each clip below is inside parentClip)
  origin = parentOrigin + Rect.XY. childClip = Overflow != Visible ? Intersect(PaddingBounds(box), parentClip) : parentClip
  push frame. NextChild = point in childClip ? children.Length - 1 : -1
finally: clear the used frames (no element reference stays)
```

The point is in bounds when `x >= minX && x < maxX && y >= minY && y < maxY`. A NaN component fails each compare, so a NaN point hits nothing. Infinite clip bounds pass each finite point.

### The Seams for the Siblings

| Sibling | The seam | What the sibling adds |
|---------|----------|-----------------------|
| keyboard-focus | `RoutePointer` in `UiContext.cs`, the `PointerDown` branch | The focus move to the hit element on a press. |
| keyboard-focus | `GlfwUiInput` | The `OnKey` and `OnChar` subscriptions, with the same internal `Push*` pattern. |
| routing-gate | `internal Element? PointerHit` | `UiRoutingGate.PointerOverUi`. |
| routing-gate | `internal int DrainEvents()` (event-model) | The call from `Update`, and the queue case in `ThousandElementAllocationTests`. |

---

## Implementation Phases

Each phase is a slice: one narrow path that works end to end, with a demo line that a person can check. Section 11 of `.beast-mode/phase-handoff.md` holds the rules. The requirements leave the demo of phase 1 open, so Notes Q1 holds it, and phase 1 uses the recommended line.

Each phase ends with `dotnet build brutal-ui.sln 2>&1 | tail -n 30` and `dotnet test --solution brutal-ui.sln 2>&1 | tail -n 30` (tool rule 5). Expect `0 Warning(s)`, `0 Error(s)`, and 0 failed. The projects have no implicit usings, so each new file holds its own `using` lines. Each public member needs an XML doc comment. Use the file-scoped `namespace BrutalUi.Core;` in Core, `namespace BrutalUi.Glfw;` in the adapter, and `namespace BrutalUi.Core.Tests;` or `namespace BrutalUi.Glfw.Tests;` in the tests. Prose in comments follows ASD-STE100. At the start of phase 1, record the SHA-256 of `src/BrutalUi.Core/Input/EventWalk.cs` in `context.md`. Each phase checks that the hash stays the same.

### Phase 1: Hover Under the Pointer
**Goal:** The smallest path that proves the idea: a pointer move with no target finds the element under the pointer, sets `Hover` on it, stores the hit, and walks to it.
**Demo:** a unit test lays out a root with two children, adds a `:hover` rule, pushes a pointer move with no target over the second child, drains the queue, and runs `Update`. It reads `PseudoStates.Hover` on the second child and the hover fill in its `Style.BackgroundColor`. The developer runs `dotnet run --project tests/BrutalUi.Core.Tests/BrutalUi.Core.Tests.csproj -- --filter-class '*PointerHoverTests'` and reads a total of 5 with `failed: 0`.
**Needs:** none
**Agent tier:** ADVANCED (opus)

**Tier note:** This phase changes the drain of `UiContext`, the central class, and adds a second walk with an explicit stack. Keep the change of `DrainEvents` to the target step only. Do not change `EventWalk.cs`. Copy the stack rules of `PaintWalk` (the growth by doubling, the stale `ref` gotcha after a growth, and the clear in `finally`). The early return of a hit must also clear the used frames.

**Tasks:**

Group A (the walk and the context, 3 files):
- [ ] 1.1 Write `src/BrutalUi.Core/Input/HitWalk.cs`: the internal struct `HitFrame` (`Element`, `Origin`, `NextChild`) and `internal static class HitWalk` with `internal const int InitialStackSize = 32` and `internal static Element? Find(ReadOnlySpan<Element> roots, float2 point, ref HitFrame[] stack)`. Follow the section "HitWalk.Find" with no hidden rule, no clip, and no `IsPickable` check (phase 3 adds them). The origin of an element is the origin of its parent plus `Layout.Rect.XY`, as step 3 of `PaintWalk.Enter`. Use the half-open test on the border box. Grow the stack with `Array.Resize(ref stack, Math.Max(stack.Length * 2, InitialStackSize))`. Clear the used frames in a `finally`, also after the return of a hit. The class remarks state the order (the last root first, the last child first, a descendant before its ancestor) and the `PaintWalk` pattern. No method calls itself.
- [ ] 1.2 Edit `src/BrutalUi.Core/Tree/UiContext.cs`: add `private HitFrame[] _hitStack = new HitFrame[HitWalk.InitialStackSize];`, `internal Element? HitTest(float2 position)` (calls `HitWalk.Find(_roots.AsSpan(), position, ref _hitStack)`), `internal int HitStackLength`, and `internal ReadOnlySpan<HitFrame> HitStackSlots`. The remarks state: the hit test reads `Layout.Rect` of the last `Update`.
- [ ] 1.3 Edit `UiContext.cs`: add the fields `_pointerHit`, `_pointerPosition`, and `_hasPointerPosition`, and the internal getters `PointerHit`, `PointerPosition`, and `HasPointerPosition`. The remarks state: routing-gate reads `PointerHit` (A6). The value follows the pointer events only.
- [ ] 1.4 Edit `UiContext.cs`: add `private Element? RoutePointer(ref UiQueuedEvent entry)` and `private void SetHover(Element? hit)`. In the target step of `DrainEvents`, when `entry.Header.Target` is null and the type is `PointerMove`, `PointerDown`, or `PointerUp`, call `target = RoutePointer(ref entry)` before the existing skip. `RoutePointer` reads `entry.Payload.Pointer.Position`, calls `HitTest`, writes `entry.Header.Target`, sets the three last-hit fields, and calls `SetHover`. `SetHover` returns at once for the same element. Else it clears `Hover` on the old element with `old.States &= ~PseudoStates.Hover`, stores the new element, and sets `Hover` on it. Leave a comment in `RoutePointer`: phase 2 adds `Active` and the scroll. keyboard-focus adds the click focus in the `PointerDown` branch.
- [ ] 1.5 Edit the doc comments: the remarks of `PushEvent(in PointerEvent)` and of `DrainEvents` in `UiContext.cs` (a pointer event with no target gets the hit element as its target at the drain, against the layout of the last `Update`, and moves `Hover`). Edit `src/BrutalUi.Core/Input/UiEventHeader.cs`: the remarks of `Target` state the same rule. A target from the caller skips the hit test and the states (Decision 2).

Group B (the tests, 4 files):
- [ ] 1.6 Write `tests/BrutalUi.Core.Tests/Input/PointerTree.cs`: an `internal sealed class PointerTree` with a context (`Measurer = new RecordingRenderer()`, `ViewportSize = (800, 600)`), a root of 400 x 300 with `FlexDirection.Row`, two children `Left` and `Right` of 100 x 100, and a child `Inner` of 50 x 50 in `Right`. Set the sizes with `Inline.Width` and `Inline.Height`. The constructor takes a name prefix and interns a class `<prefix>.hoverable` on `Left` and `Right`, because xUnit runs the classes in parallel. `AddHoverSheet()` adds a rule `.hoverable:hover` with `BackgroundColor = HoverFill` (the pattern of `PseudoStateSelectorTests`). Add `Settle()` (`Update(0f)`), `static PointerEvent Move(float2 p)`, `Down(float2 p, PointerButton b)`, `Up(float2 p, PointerButton b)`, `static ScrollEvent Scroll(float2 p, float2 delta)` (each with a null target), and `static float4 ViewportRect(Element e)` (the sum of the parent rects). The class links into `BrutalUi.Glfw.Tests` in phase 4. Do not put it in `Shared/`: `BrutalUi.ImGui.Tests` links `Shared/*.cs` and has no access to the internals of Core.
- [ ] 1.7 Write `tests/BrutalUi.Core.Tests/Input/HitTestTests.cs` with 6 tests: `APointInAChildHitsTheChild` (also `Inner` in `Right`), `APointInTheGapHitsTheParent`, `APointOutsideEachRootHitsNothing`, `TheLaterSiblingWinsWhereTwoChildrenOverlap` (two absolute children), `TheLastRootWinsWhereTwoRootsOverlap`, and `ATreeDeeperThanTheStackHitsTheLeaf` (a chain of 40 levels, `HitStackLength` of 41 or more, and each slot of `HitStackSlots` holds a null element after the call).
- [ ] 1.8 Write `tests/BrutalUi.Core.Tests/Input/PointerHoverTests.cs` with 5 tests: `AMoveOverAChildSetsHoverAndShowsTheHoverRule` (the demo), `AMoveToASiblingMovesHover` (`Left` loses the state, `Right` gets it), `AMoveOutsideClearsHover`, `TheMoveWalksToTheHitElement` (an `EventRecorder` on the root and `Right` reads root capture, `Right` target capture, `Right` target bubble, and root bubble), and `TheContextStoresTheLastHit` (`PointerHit`, `PointerPosition`, and `HasPointerPosition`).
- [ ] 1.9 Run the event-model classes with no change: `EventWalkTests` (13), `EventKindTests` (12), `EventQueueTests` (9), `EventWalkEdgeTests` (5), and `EventAllocationTests` (3). `EventQueueTests.ADrainSkipsAnEventWithNoTarget` pushes a pointer down with no target. Its root has an empty rect, so the hit test finds nothing and the test still passes. Do not edit the test.
- [ ] 1.10 Run the build and the tests. Run the demo command and `--filter-class '*HitTestTests'`. Record the test counts before and after, the hash of `EventWalk.cs`, and each decision that the brief did not settle in `docs/features/input-routing/pointer-routing/context.md`.

**Estimated Steps:** about 60 (10 tasks, about six steps for one task)

**Files (7):** Group A: `HitWalk.cs`, `UiContext.cs`, `UiEventHeader.cs`. Group B: `PointerTree.cs`, `HitTestTests.cs`, `PointerHoverTests.cs`, `context.md`.

**Acceptance Criteria:**
- [ ] The 5 tests of `PointerHoverTests` and the 6 tests of `HitTestTests` pass.
- [ ] The 42 tests of the five event-model classes pass with no edit.
- [ ] No method of `HitWalk` calls itself. `EventWalk.cs` has the hash of the start.

---

### Phase 2: Press, Release, and Scroll
**Goal:** A press sets `Active` on the hit element, the last release clears it, and a scroll walks to the element under the pointer.
**Demo:** a unit test pushes a pointer down with no target over `Right`, drains, and reads `PseudoStates.Active` on `Right`. A bubble handler on the root sees `Target` `Right` and `CurrentTarget` root. A pointer up clears the state. The developer runs `dotnet run --project tests/BrutalUi.Core.Tests/BrutalUi.Core.Tests.csproj -- --filter-class '*PointerPressTests'` and reads a total of 6 with `failed: 0`.
**Needs:** Phase 1
**Agent tier:** STANDARD (sonnet)

**Tasks:**
- [ ] 2.1 Edit `UiContext.cs`: add `_activeElement`, `_pressedButtons` (an `int` bit mask), and `internal Element? ActiveElement`. In `RoutePointer`, for `PointerDown`: set the bit `1 << (int)button` when `(uint)button < 32`. When `_activeElement` is null and the hit is not null, set `Active` on the hit and store it. For `PointerUp`: clear the bit. When the mask is 0 and `_activeElement` is not null, clear `Active` on it and set the field to null (Decision 8). The release walks to the element under the pointer, not to the pressed element. Pointer capture is out of scope.
- [ ] 2.2 Edit `UiContext.cs`: the target step also sends a `Scroll` entry with no target to `RoutePointer`. `RoutePointer` reads `entry.Payload.Scroll.Position` for a scroll. A scroll also moves `Hover` and the last hit (Decision 7).
- [ ] 2.3 Edit the doc comments: the remarks of `PushEvent(in ScrollEvent)` and of `DrainEvents` (the scroll rule and the `Active` rule).
- [ ] 2.4 Write `tests/BrutalUi.Core.Tests/Input/PointerPressTests.cs` with 6 tests: `APressSetsActiveAndTheReleaseClearsIt` (the demo), `APressBubblesFromTheHitElement` (the root bubble handler reads `Target` and `CurrentTarget`), `AReleaseOverAnotherElementClearsTheFirst` (the up walks to `Left`, and `Right` loses `Active`), `ASecondButtonKeepsActiveUntilTheLastRelease`, `APressOverNothingSetsNoActive`, and `AnActiveRuleShowsAfterUpdate` (a rule `.hoverable:active` and `Settle()`). Extend `PointerTree` with an active rule when the tests need it.
- [ ] 2.5 Write `tests/BrutalUi.Core.Tests/Input/ScrollRoutingTests.cs` with 4 tests: `AScrollGoesToTheElementUnderThePointer` (the scroll bubbles from `Inner` to `Right` and the root, and the delta and the position survive), `AScrollOverNothingWalksNothing`, `AScrollMovesHover`, and `AScrollWithACallerTargetKeepsTheTarget`.
- [ ] 2.6 Run the build and the tests. Run the demo command and `--filter-class '*ScrollRoutingTests'`. Record the results and the decisions in `context.md`.

**Estimated Steps:** about 36 (6 tasks, about six steps for one task)

**Files (5):** `UiContext.cs`, `PointerTree.cs`, `PointerPressTests.cs`, `ScrollRoutingTests.cs`, `context.md`

**Acceptance Criteria:**
- [ ] The 6 tests of `PointerPressTests` and the 4 tests of `ScrollRoutingTests` pass.
- [ ] `Active` stays on the press target until the last button goes up.
- [ ] `EventWalk.cs` has the hash of the start.

---

### Phase 3: The Hit Rules
**Goal:** The hit test follows the draw: the clip of each ancestor with an overflow other than `Visible`, the hidden rule, and `IsPickable`. The edge and NaN rules hold.
**Demo:** a unit test puts a child half outside a parent with `Overflow.Hidden`. A point in the visible half hits the child. A point in the clipped half hits the sibling under it. The developer runs `dotnet run --project tests/BrutalUi.Core.Tests/BrutalUi.Core.Tests.csproj -- --filter-class '*HitTestRuleTests'` and reads a total of 11 with `failed: 0`.
**Needs:** Phase 2
**Agent tier:** STANDARD (sonnet)

**Tasks:**
- [ ] 3.1 Edit `src/BrutalUi.Core/Rendering/PaintWalk.cs`: change `PaddingBounds` and `Intersect` from `private static` to `internal static`. Do not change a body. Add one remark to each: `HitWalk` shares the method, so the hit clip equals the draw clip.
- [ ] 3.2 Edit `HitWalk.cs`: add `Opacity`, `OwnClip`, and `ChildClip` to `HitFrame`. Follow the section "HitWalk.Find" in full. The hidden rule copies steps 1 and 2 of `PaintWalk.Enter`: `Display.None`, and an opacity product of 0 or less after the clamp (a value below 0 counts as 0, above 1 as 1, and NaN as 1), skip the subtree (Notes Q4). A point outside the clip of the parent skips the subtree. A point outside the child clip sets `NextChild` to -1, so the walk tests the element and skips its children. The self test reads `IsPickable` first. An element with `IsPickable` false passes the hit to the next element. Its children stay pickable (Decision 5).
- [ ] 3.3 Edit the remarks of `HitWalk`: list the rules in the order of the code. State the half-open edges, the NaN rule, and that the hit test does not read `LayoutNode.ContentInsets` (Notes, Conflicts 1).
- [ ] 3.4 Write `tests/BrutalUi.Core.Tests/Input/HitTestRuleTests.cs` with 11 tests: `AnOverflowHiddenParentClipsItsChild` (the demo), `AnOverflowScrollParentClipsItsChild`, `NestedClipsIntersect`, `AVisibleOverflowLetsAChildOutsideTheParentBeHit`, `ThePaddingBoxClipKeepsTheBorderForTheParent` (a point on the border of a clip parent, over a child that extends under the border, hits the parent), `AnUnpickableLeafPassesTheHitToItsParent`, `AnUnpickableParentKeepsItsChildrenPickable` (a point in the gap passes to the grandparent), `ADisplayNoneSubtreeIsNotHit`, `AZeroOpacitySubtreeIsNotHit`, `TheRightAndBottomEdgesAreOutside`, and `ANaNPointHitsNothing`.
- [ ] 3.5 Run the build and the tests. Check that the draw tests and the draw gates still pass (`--filter-class '*PaintWalk*'` and `*ThousandElementAllocationTests`). Run the demo command. Record the results in `context.md`.

**Estimated Steps:** about 30 (5 tasks, about six steps for one task)

**Files (4):** `PaintWalk.cs`, `HitWalk.cs`, `HitTestRuleTests.cs`, `context.md`

**Acceptance Criteria:**
- [ ] The 11 tests of `HitTestRuleTests` pass, and the tests of phases 1 and 2 still pass.
- [ ] `git diff` of `PaintWalk.cs` shows two changed modifiers and added remark lines only. No method body changes.
- [ ] `HitWalk.cs` calls `PaintWalk.PaddingBounds` and `PaintWalk.Intersect`, and holds no copy of them.

---

### Phase 4: The GLFW Adapter
**Goal:** The new `BrutalUi.Glfw` package converts the `GlfwWindow` events into the structs of Core and pushes them. Drop and content-scale events reach the `UiContext` callbacks.
**Demo:** a unit test in `BrutalUi.Glfw.Tests` creates a `GlfwUiInput` with the test seam (no window), calls its internal `PushCursorMove` and `PushButton` over `Right` of the pointer tree, drains, and reads `Hover` and `Active` on `Right`. The developer runs `dotnet run --project tests/BrutalUi.Glfw.Tests/BrutalUi.Glfw.Tests.csproj -- --filter-class '*GlfwUiInputTests'` and reads a total of 10 with `failed: 0`.
**Needs:** Phase 3
**Agent tier:** ADVANCED (opus)

**Tier note:** This phase creates a package project and a test project, changes the public API of `UiContext`, and binds to an API that the planner could not read in full. Do task 4.5 before any code of group B. The XML doc of `Brutal.Glfw` names the events but gives no parameter types. Find the delegate signatures and the way that `GlfwWindow` installs its native callbacks. When `GlfwWindow` sets the native callback at the first subscription, a later subscription can replace the callback of the ImGui `GlfwBackend`. Record the safe creation order. Do not change `Directory.Packages.props`: it pins `Ahwoo.BRUTAL.Glfw` 2026.10.4 already. When the real API cannot carry a requirement (for example, no drop paths as strings), stop and report to the main thread.

**Tasks:**

Group A (the Core callbacks, 4 files):
- [ ] 4.1 Write `src/BrutalUi.Core/Input/UiWindowEvents.cs`: the public delegates `UiFileDropHandler(ReadOnlySpan<string> paths)` and `UiContentScaleHandler(float2 scale)`. The remarks cite A4: the window events get no per-element routing.
- [ ] 4.2 Edit `UiContext.cs`: add the public slots `OnFileDrop` and `OnContentScale`, and the public methods `RaiseFileDrop(ReadOnlySpan<string> paths)` and `RaiseContentScale(float2 scale)`. Each method calls its slot at once when the slot is not null. It queues no entry, walks no element, and changes no state. The remarks state: call the methods on the main thread. The span is valid during the call only. The input adapter calls them. A test or another platform layer can call them too.
- [ ] 4.3 Edit `src/BrutalUi.Core/BrutalUi.Core.csproj`: add `<InternalsVisibleTo Include="BrutalUi.Glfw.Tests" />` with a comment: the adapter tests call the drain and read the last hit. Add no `PackageReference`.
- [ ] 4.4 Write `tests/BrutalUi.Core.Tests/Input/WindowCallbackTests.cs` with 4 tests: `ADropWithNoCallbackDoesNothing`, `ADropPassesEachPathInOrder`, `AContentScalePassesTheScale`, and `ACallbackQueuesNoEventAndChangesNoState` (`QueuedEventCount` 0, `PointerHit` null, and no new dirty bit).

Group B (the package, 6 files):
- [ ] 4.5 Read the real API of `Ahwoo.BRUTAL.Glfw` 2026.10.4. Decompile `Brutal.Glfw.dll` with `ilspycmd` in the session scratch folder, as `docs/features/dev-ops/solution-skeleton/context.md` did. Commit no decompiled file. Record a table in `context.md`: the delegate type and the parameter types of `OnCursorPos`, `OnMouseButton`, `OnScroll`, `OnCursorEnter`, `OnDrop`, and `OnContentScale`, the type of `GlfwWindow` (class or struct), `GetKey`, `CursorPos`, `Glfw.Time`, and the install rule of the native callbacks with the safe order next to `GlfwBackend`.
- [ ] 4.6 Write `src/BrutalUi.Glfw/BrutalUi.Glfw.csproj` in the shape of `BrutalUi.ImGui.csproj`: a `Description`, `<InternalsVisibleTo Include="BrutalUi.Glfw.Tests" />`, `<PackageReference Include="Ahwoo.BRUTAL.Glfw" />`, and `<ProjectReference Include="../BrutalUi.Core/BrutalUi.Core.csproj" />`. `src/Directory.Build.props` makes it packable. Add the project and the test project of task 4.9 to the solution: `dotnet sln brutal-ui.sln add --solution-folder src src/BrutalUi.Glfw/BrutalUi.Glfw.csproj` and the same with `--solution-folder tests` for the test project.
- [ ] 4.7 Write `src/BrutalUi.Glfw/GlfwUiInput.cs`: the `public sealed class GlfwUiInput : IDisposable` of the Public API section. The constructor throws `ArgumentNullException` for a null window or context. It creates each GLFW handler delegate one time in a field and subscribes it. It seeds the last cursor position from `window.CursorPos`. Add the test seam `internal GlfwUiInput(UiContext context)`: it throws for a null context, subscribes to no event, and starts at the position (0, 0). `Dispose` unsubscribes each handler (none for the seam) and sets a flag. After `Dispose`, each `Push*` method returns at once. A second `Dispose` does nothing. Each GLFW handler converts and calls an internal `Push*` method. A cursor position becomes a `float2` with no scale (Decision 13). A button is cast to `PointerButton` with no table, also for the buttons 3 to 7 (Decision 14). A press gives `PointerDown` and a release gives `PointerUp`, with `(UiModifiers)((int)mods & 0x3F)`. A scroll gives `Delta` as a `float2` and the last cursor position as `Position`. The timestamp comes from `GlfwApi.Time`. A drop calls `Context.RaiseFileDrop`, and a content-scale event calls `Context.RaiseContentScale`. Each push uses a null target.
- [ ] 4.8 Add the cursor leave (Notes Q3) and the modifiers of a move and a scroll to `GlfwUiInput.cs`. `OnCursorEnter` with "left" calls `PushCursorLeave`, which pushes a `PointerMove` at `(float.NaN, float.NaN)` and sets the last cursor position to it. "Entered" does nothing. A move and a scroll read the modifiers from `GetKey` of the left and right Shift, Control, Alt, and Super keys (Decision 14). Write the class remarks: create the adapter on the main thread, the creation order with `GlfwBackend` from task 4.5, dispose the adapter before the window, one context for each adapter, the position space (Decision 13), and the fact that `Update` does not drain until routing-gate.
- [ ] 4.9 Write `tests/BrutalUi.Glfw.Tests/BrutalUi.Glfw.Tests.csproj` in the shape of `BrutalUi.ImGui.Tests.csproj`: `OutputType` `Exe`, `xunit.v3`, the linked `../BrutalUi.Core.Tests/Shared/*.cs` with `LinkBase="Shared"`, the linked `../BrutalUi.Core.Tests/Input/PointerTree.cs` and `../BrutalUi.Core.Tests/Input/EventRecorder.cs` with `LinkBase="Input"`, and a `ProjectReference` to `BrutalUi.Glfw`.
- [ ] 4.10 Write `tests/BrutalUi.Glfw.Tests/GlfwUiInputTests.cs` with 10 tests. Each test except the last creates the adapter with the test seam `new GlfwUiInput(context)` and calls the `Push*` methods. Tests: `ACursorMoveAndAPressHoverAndActivateTheChild` (the demo), `AMoveCarriesThePositionTheModifiersAndTheTime`, `EachButtonCastsWithNoTable` (the values 0 to 7), `APressAndAReleaseGiveDownAndUp`, `AScrollUsesTheLastCursorPosition`, `ACursorLeaveClearsHover`, `ADropReachesTheContextCallback`, `AContentScaleReachesTheContextCallback`, `DisposeStopsThePushes`, and `ARealWindowSubscribesAndUnsubscribes`. The last test calls `GlfwApi.Init()` and creates a hidden window with `GLFW_NO_API`. When the init or the window fails (no display), it calls `Assert.Skip` with the reason. In a `finally`, it disposes the adapter, then the window, then calls `Terminate`.
- [ ] 4.11 Run the build and the tests. Run the demo command. Run `dotnet pack src/BrutalUi.Glfw/BrutalUi.Glfw.csproj -o <scratch>/pack` and read the dependencies of the `.nuspec` in the package: `Ahwoo.BRUTAL.Glfw` 2026.10.4 and `BrutalUi.Core` 0.1.0-alpha. Run `grep -rn 'using .*Glfw' src/BrutalUi.Core` (no output). Write the native dependency note in `context.md` (AGENTS.md rule): `BrutalUi.Glfw` brings the native GLFW library (`libglfw.so` on Linux, `glfw3.dll` on Windows) through `Ahwoo.BRUTAL.Glfw`. The samples use the same package already. The table of `docs/features/dev-ops/solution-skeleton/context.md`, section "Native Dependencies", lists it. `BrutalUi.Core` stays free of native code. Record the results.

**Estimated Steps:** about 66 (11 tasks, about six steps for one task)

**Files (10):** Group A: `UiWindowEvents.cs`, `UiContext.cs`, `BrutalUi.Core.csproj`, `WindowCallbackTests.cs`. Group B: `BrutalUi.Glfw.csproj`, `GlfwUiInput.cs`, `BrutalUi.Glfw.Tests.csproj`, `GlfwUiInputTests.cs`, `brutal-ui.sln`, `context.md`.

**Acceptance Criteria:**
- [ ] The 4 tests of `WindowCallbackTests` and the 10 tests of `GlfwUiInputTests` pass. `ARealWindowSubscribesAndUnsubscribes` passes or reports a skip with a reason.
- [ ] The package of `BrutalUi.Glfw` lists exactly two dependencies: `Ahwoo.BRUTAL.Glfw` and `BrutalUi.Core`.
- [ ] `BrutalUi.Core.csproj` holds 2 `PackageReference` items. `Directory.Packages.props` does not change.
- [ ] `context.md` holds the API table of task 4.5 and the native dependency note.

---

### Phase 5: Stale State Rules
**Goal:** No `Hover` and no `Active` stays on an element that left the tree. A target from the caller changes no state. A move over the same element marks nothing.
**Demo:** a unit test hovers `Right`, removes it from the root, and drains an empty queue. It reads no `Hover` on `Right` and a null `PointerHit`. The developer runs `dotnet run --project tests/BrutalUi.Core.Tests/BrutalUi.Core.Tests.csproj -- --filter-class '*PointerStateEdgeTests'` and reads a total of 7 with `failed: 0`.
**Needs:** Phase 4
**Agent tier:** STANDARD (sonnet)

**Tasks:**
- [ ] 5.1 Edit `UiContext.cs`: add `private void DropStalePointerState()` and call it at the start of `DrainEvents`, inside the `_draining` guard. When `_pointerHit` is not null and `IsInRootTree(_pointerHit)` is false, call `SetHover(null)` and set `_pointerHit` to null. When `_activeElement` is not null and is not in the tree, clear `Active` on it and set the field to null. Keep `_pressedButtons`, so a later release still clears the bit (Decision 10).
- [ ] 5.2 Edit the remarks of `DrainEvents` and of `PointerHit`: the stale rule, the rule that the states follow the pointer events only (a layout change under a still pointer waits for the next pointer event), and the rule that a target from the caller changes no state.
- [ ] 5.3 Write `tests/BrutalUi.Core.Tests/Input/PointerStateEdgeTests.cs` with 7 tests: `AHoveredElementThatLeavesTheTreeLosesHover` (the demo), `ARemovedRootLosesHover` (`RemoveRoot`), `AnActiveElementThatLeavesTheTreeLosesActive` (the release still routes to the hit element), `ACallerTargetChangesNoState`, `AMoveOverTheSameElementMarksNothing` (after `Settle()`, a second move leaves no `Style` bit in `Dirty`), `AnElementAddedAfterUpdateIsNotHitUntilTheNextUpdate`, and `ADrainWithNoPointerEventKeepsAValidHover`.
- [ ] 5.4 Run the build and the tests. Run the demo command. Record the results in `context.md`.

**Estimated Steps:** about 24 (4 tasks, about six steps for one task)

**Files (3):** `UiContext.cs`, `PointerStateEdgeTests.cs`, `context.md`

**Acceptance Criteria:**
- [ ] The 7 tests of `PointerStateEdgeTests` pass, and the tests of phases 1 to 4 still pass.
- [ ] After a drain, no element outside the roots holds `Hover` or `Active` from this context.

---

### Phase 6: The Allocation Gates and the Record
**Goal:** Prove 0 bytes for the hit test on a warm 1000-element tree, for the full pointer path, and for the adapter handlers. Record the feature in the technical design, the README, and the changelog.
**Demo:** the developer runs `dotnet run --project tests/BrutalUi.Core.Tests/BrutalUi.Core.Tests.csproj -- --filter-class '*PointerAllocationTests'` and reads a total of 3 with `failed: 0`. A planted allocation at the start of `HitWalk.Find` makes `dotnet test --solution brutal-ui.sln` exit non-zero.
**Needs:** Phase 5
**Agent tier:** STANDARD (sonnet)

**Tier note:** When a gate measures bytes above 0, find the cause before a change. Run the body with one event kind at a time in a scratch copy of the test. Expected suspects: a `ReadOnlySpan` over a field that copies a struct, a boxed `PseudoStates`, a delegate created in the body, or a growth of the hit stack after the warm-up. Fix the cause in `src/` with no public API change. Do not add warm-up rounds to hide a growth.

**Tasks:**

Group A (the gates, 3 files. Task 6.8 records the results):
- [ ] 6.1 Write `tests/BrutalUi.Core.Tests/Input/PointerAllocationTests.cs` in the pattern of `EventAllocationTests`: a private `Scene` from `ThousandElementTree.Build()` with one `Update(0f)` for the layout. Compute 8 points from `PointerTree.ViewportRect` outside the measure: a grid slot point outside its icon and its badge (hits the slot), an icon point, a badge point, a modal point, a grid point under the clip of the grid, a tooltip point, a point over the hidden settings panel, and a point outside the root. Hold the prepared events as `required` fields (event-model P5-6). Add `[Fact] TheHitTestAllocatesNothingOnAWarmTree`: 2 warm-up rounds, `AllocationGate.ReleaseAllocationContext()`, then 100 rounds of `HitTest` over the 8 points, and `Assert.Equal(0L, after - before)`.
- [ ] 6.2 Add `[Fact] PointerRoutingAllocatesNothingAfterWarmUp`: each step pushes a move to the slot point, a down, a move to the modal point, an up, a scroll at the slot point, and a move outside the root. Then it calls `DrainEvents()` and `Update(0.016f)`. Measure 100 steps after 2 warm-up steps. The body holds no lambda, no boxing, no string, and no `Assert`.
- [ ] 6.3 Add `[Fact] TheMeasuredBodyDoesRealWork`: assert the hit of each of the 8 points. After a move to the slot point and `Update`, the slot holds `Hover` and the border color `ThousandElementTree.SlotHoverBorderColor`. After one full step, no element holds `Hover` or `Active`, and counter handlers on the slot and the modal read the expected walks.
- [ ] 6.4 Write `tests/BrutalUi.Glfw.Tests/GlfwUiInputAllocationTests.cs` with `[Fact] TheAdapterHandlersAllocateNothing`: fill the ring of a context with capacity 8, warm up, release the allocation context, and measure 1000 calls of each of `PushCursorMove`, `PushButton`, `PushScroll`, and `PushCursorLeave`. Assert 0 bytes and the growth of `DroppedEventCount`.
- [ ] 6.5 Prove that the gate blocks. Copy `src/BrutalUi.Core/Input/HitWalk.cs` to the session scratch folder. Add `GC.KeepAlive(new object());` as the first line of `Find`. Run `dotnet test --solution brutal-ui.sln > /dev/null 2>&1; echo "exit=$?"` and expect a non-zero exit. Restore the file with `cp`, check it with `cmp`, run the tests again, and expect 0 failed. Report both exit codes and the names of the failed gates in the hand-back.

Group B (the record, 4 files):
- [ ] 6.6 Edit `docs/technical-design.md`. Section 3: add `BrutalUi.Glfw/` and `BrutalUi.Glfw.Tests/` to the tree, and a paragraph for `BrutalUi.Glfw` under "Directory Responsibilities". Section 4: in the overview, replace `UiInput` with `GlfwUiInput ──► UiContext.PushEvent`. Section 5, "Events": add the pointer step of the drain (the hit test, `Hover`, `Active`, and the last hit). Section 9, "Flow": replace `UiInput` with `GlfwUiInput` and `PickingEnabled` with `IsPickable`, and state the hit rules, the state rules, the rule that a layout root sets `IsPickable` to false, and the two callbacks. Section 14: add the pointer test classes and the gates `PointerAllocationTests` and `GlfwUiInputAllocationTests`, with the plant result of task 6.5.
- [ ] 6.7 Edit `README.md`: add the row `BrutalUi.Glfw` (the GLFW input adapter, references `Brutal.Glfw` and `BrutalUi.Core`) to the packages table, and change "the five packages" to "the six packages". Edit `CHANGELOG.md`: add one line under `## [Unreleased]`, `### Added`: the hit test, the `Hover` and `Active` states, the scroll routing, the window callbacks, and the `BrutalUi.Glfw` package.
- [ ] 6.8 Run the build, the tests, and `scripts/test-linux.sh; echo "exit=$?"`. Run `python3 /home/aaronstatic/.claude/skills/asd-ste100/scripts/ste-lint.py --fail-over 2.5` on each changed doc and on this plan, and fix each hit. Record the gate results of group A, the plant result, and the final test counts in `context.md`.

**Estimated Steps:** about 48 (8 tasks, about six steps for one task)

**Files (7):** Group A: `PointerAllocationTests.cs`, `GlfwUiInputAllocationTests.cs`, and a temporary edit of `HitWalk.cs` (restored). Group B: `docs/technical-design.md`, `README.md`, `CHANGELOG.md`, `context.md`.

**Acceptance Criteria:**
- [ ] The 3 tests of `PointerAllocationTests` pass with 0 bytes in 5 runs of the class. `GlfwUiInputAllocationTests` passes.
- [ ] The plant gives a non-zero exit code. The restored file gives 0 failed.
- [ ] Sections 3, 4, 5, 9, and 14 of the technical design match the code. Section 9 names `GlfwUiInput` and `IsPickable`, and no longer names `UiInput` or `PickingEnabled`.

---

**Total Estimated Steps:** about 264 (44 tasks over 6 phases)

---

## Key Technical Decisions

### Confirmed Assumptions

No feature-level interview ran: `/autorun-epic` drafts this plan in its called mode, and no person can answer. The items below are the epic-level items that the user confirmed in the epic interview (`docs/features/input-routing/epic-overview.md`):
- Q1 The GLFW adapter lives in a new `BrutalUi.Glfw` package. Core stays free of GLFW.
- Q3 Build order: event-model, pointer-routing, keyboard-focus, routing-gate.
- A1 `BrutalUi.Core` gets no GLFW reference. The input types in Core accept plain structs, and `BrutalUi.Glfw` converts the `GlfwWindow` events.
- A3 The hit test uses the axis-aligned layout rects and the clip rects only. No transforms, no custom shapes.
- A4 Drop and content-scale events go to `UiContext` callbacks. They get no per-element routing.
- A5 The adapter queues events in a fixed ring buffer. The input pass drains it in `UiContext.Update` before the style pass.
- A6 The gate reads the hit result of the last pointer position and the focus owner. The game polls it after `Update`.

### Decision 1: The hit test and the states run in the target step of the drain
**Choice:** The drain runs the hit test, the state change, and the store of the last hit for each pointer or scroll entry with no target. The push only copies the event.
**Rationale:** A5 puts the input pass in `Update` before the style pass, so a state change and its restyle land in the same `Update`. At the drain, `Layout.Rect` holds the layout of the last frame, which is the frame on the screen. The event-model seam names this step. Until routing-gate wires the drain, no game sees a state change. The demo of each phase is a test, so the build order holds.
**Alternatives:** (a) The hit test at the push: the state changes outside `Update`, and `PushEvent`, a public method, gets a second job for each caller. (b) A separate public pass, for example `UpdatePointer()`: one more call for the game, and routing-gate owns the order of the passes.

### Decision 2: A null target means "hit-test the position"
**Choice:** A `PointerEvent` or `ScrollEvent` with a null target gets the hit element as its target at the drain. A target from the caller skips the hit test, the states, and the last hit.
**Rationale:** A4 of event-model lets the caller set the target. The adapter has no tree knowledge, so it pushes a null target. A test or a synthetic event can still aim at one element, and the event-model tests keep their meaning. A key or char entry with no target stays a skip, and keyboard-focus changes that rule.
**Alternatives:** A flag in the header for "hit-test me": one more public member for the same rule.

### Decision 3: HitWalk copies the PaintWalk pattern
**Choice:** An internal static class with an explicit stack of `HitFrame` values on `UiContext`. The stack starts at 32, grows by doubling, and never shrinks. The walk clears the used frames in `finally`, also after an early return.
**Rationale:** The rule "no recursion" of the technical design, and the 0-byte rule after warm-up. The pattern is known and tested in `PaintWalk`. The name `HitWalk` avoids a clash with the method `UiContext.HitTest`.
**Alternatives:** Recursion: simple, but a deep tree can overflow the stack, and the project forbids it in the walks.

### Decision 4: The hit test mirrors the draw
**Choice:** The hit area is the border box. The clip of an element with an overflow other than `Visible` is its padding box, intersected with the clip of its parent. The hit test calls `PaintWalk.PaddingBounds` and `PaintWalk.Intersect`. It skips what the draw hides: `Display.None` and an opacity product of 0 or less (Notes Q4). It does not read `LayoutNode.ContentInsets`.
**Rationale:** A3. A person clicks what the screen shows. One copy of the clip math keeps the two walks equal. The technical design (section 8, A7) states that `ContentInsets` holds the padding plus the border, so it is not the clip.
**Alternatives:** (a) A copy of the clip math in `HitWalk`: the two can drift. (b) `ContentInsets` as the clip: it cuts the padding, so a click on the padding of a scroll panel misses its children.

### Decision 5: IsPickable false skips the element only
**Choice:** An element with `IsPickable` false is not a hit. Its children stay pickable. A point in its gap passes to the element under it.
**Rationale:** This is the `PickingMode.Ignore` rule of Unity UI Toolkit, which mosaic-ui used. A full-screen layout root with `IsPickable` false lets the game get the pointer in the empty areas (routing-gate). A label inside a button with `IsPickable` false passes the hit to the button (Notes Q2).
**Alternatives:** Skip the full subtree: then a layout container cannot be transparent and keep its children.

### Decision 6: Half-open bounds and NaN
**Choice:** A point is inside when `min <= p < max` on each axis. A NaN component is never inside.
**Rationale:** Two adjacent elements then never share an edge pixel. The cursor leave of the adapter pushes `(NaN, NaN)`, and it hits nothing with no special case.
**Alternatives:** Closed bounds: the shared edge goes to the top-most element. That is also correct, but the right and bottom edges of the root then count as inside.

### Decision 7: Hover follows each hit-tested entry, on the hit element only
**Choice:** Each hit-tested pointer or scroll entry moves `Hover` to the hit element and stores it as the last hit. One field, `_pointerHit`, is both the hover owner and the last hit. `Hover` is on the hit element only (Notes Q2).
**Rationale:** The requirement says that a move sets `Hover` on the element under the pointer and clears it on the last one. A press or a scroll also gives a position. With this rule, a press after a window focus, with no move before it, still gives the right state. One element at a time matches the `Focus` rule of keyboard-focus. The state set is one write on each of two elements, and each write marks one subtree.
**Alternatives:** (a) Moves only: a press with no move leaves a stale hover. (b) The CSS rule, with `Hover` on each ancestor too: see Notes Q2.

### Decision 8: Active belongs to the first press until the last release
**Choice:** A press sets `Active` on its hit element when no element holds `Active`. A bit mask counts the buttons that are down. The release of the last button clears `Active` on the stored element. The release walks to the element under the pointer.
**Rationale:** The requirement: a press sets `Active` on the target and a release clears it. The mask keeps the state through a second button. Pointer capture is out of scope, so the release does not walk to the pressed element.
**Alternatives:** Each release clears: a right click during a left press then clears the left press state.

### Decision 9: The state changes before the walk
**Choice:** `RoutePointer` writes the states before `EventWalk.Dispatch` runs.
**Rationale:** A handler reads the current state of its element. The style pass after the drain sees each change, also when a handler stops the walk.
**Alternatives:** After the walk: a handler then reads the old state.

### Decision 10: The drain drops stale state at its start
**Choice:** At the start of each drain, the context clears `Hover` and `Active` on a stored element that left its roots, and drops the reference.
**Rationale:** A closed panel must not keep a hover look when it comes back. The context then holds no reference to a removed panel after the next drain. The cost is one parent walk for each of two fields. routing-gate drains in each `Update`, so the rule runs in each frame.
**Alternatives:** A hook in `RemoveRoot` and in the child list: more code paths, and a detach deep in the tree still needs the walk.

### Decision 11: The window callbacks are delegate slots on UiContext, called at once
**Choice:** `OnFileDrop` and `OnContentScale` are public slots. `RaiseFileDrop` and `RaiseContentScale` call them at once, with no queue.
**Rationale:** A4. The paths of a drop are strings, and the ring holds no reference besides the target. A drop and a content-scale change are rare, and no element routes them. The slot properties follow the `On...` names of the handler slots of `Element`.
**Alternatives:** (a) Queue them for the drain: a second ring type for two rare events. (b) Callbacks on the adapter: against A4, and a second platform layer would repeat them.

### Decision 12: The adapter shape
**Choice:** `GlfwUiInput` is a sealed `IDisposable` class with a `required` create-info. The constructor subscribes the handlers. `Dispose` unsubscribes them. Each GLFW handler converts its arguments and calls an internal `Push*` method with plain types. An internal constructor with no window is the test seam.
**Rationale:** The project rules ask for `required` create-info structs. GLFW cannot inject an OS event, so the conversion must be testable without one. One delegate for each handler, created at construction, keeps each event free of allocation.
**Alternatives:** A static helper with no state: it cannot hold the last cursor position for a press and a scroll.

### Decision 13: The position stays in GLFW screen coordinates
**Choice:** The adapter casts the cursor position to `float2` with no scale.
**Rationale:** The viewport space of the draw is the ImGui display space. HelloPanel sets `ViewportSize` from `io.DisplaySize`, which the ImGui GLFW backend sets to the window size in screen coordinates. `Draw` calls `Begin(ViewportSize, 1f)`. The cursor position of GLFW is in the same screen coordinates, so the two match on each platform.
**Alternatives:** (a) A `PositionScale` member in the create-info: no consumer needs it yet (YAGNI). (b) A scale from `ViewportSize / window.Size` for each event: it breaks for a `NaN` viewport.

### Decision 14: The conversions
**Choice:** A button is cast to `PointerButton` for the GLFW values 0 to 7, with no filter. The modifiers of a button event are the GLFW bits masked with `0x3F`. A move and a scroll read the modifiers from `GetKey` of the eight modifier keys (no caps lock and no num lock). The timestamp is `Glfw.Time`, in seconds. A scroll takes the last cursor position.
**Rationale:** event-model gave `PointerButton` and `UiModifiers` the GLFW values. The cursor and scroll callbacks of GLFW give no modifiers, and `GetKey` reads a cached key state with no OS call. `Glfw.Time` is the monotonic clock of the window layer.
**Alternatives:** (a) The modifiers of the last button event: a release of Shift then shows late. (b) `UiModifiers.None` for a move and a scroll: a Ctrl+scroll zoom cannot work.

### Decision 15: The adapter tests reach the internals of Core
**Choice:** `BrutalUi.Core.csproj` adds `InternalsVisibleTo` for `BrutalUi.Glfw.Tests`.
**Rationale:** The adapter tests must drain the queue and read `PointerHit`. `DrainEvents` stays internal until routing-gate (A5 of event-model).
**Alternatives:** Test the adapter through `QueuedEventCount` only: the demo cannot show a state.

---

## Risks & Mitigation

| Risk | Likelihood | Impact | Mitigation |
|------|------------|--------|------------|
| A subscription of `GlfwUiInput` replaces the native callback of the ImGui `GlfwBackend`, so ImGui loses its input, or the other way. | Medium | High | Task 4.5 reads the install rule. The remarks of `GlfwUiInput` state the safe order. routing-gate checks it in HelloPanel. |
| The real signatures of the `GlfwWindow` events differ from the plan, for example the drop gives UTF-8 pointers. | Medium | Low | Task 4.5 runs first. The internal `Push*` shape stays. A UTF-8 drop converts to strings at drop time only (Notes, Conflicts 4). |
| The native trampoline of `Brutal.Glfw` allocates for each event. | Low | Medium | Outside this code. `GlfwUiInputAllocationTests` measures the adapter code. The HelloPanel gate of routing-gate measures the full frame. |
| A full-screen pickable root makes `PointerOverUi` true everywhere. | High | Medium | Decision 5. The technical design states the rule: a layout root sets `IsPickable` to false. Notes Conflicts 7 tells routing-gate. |
| The hover stays when the layout moves under a still pointer. | High | Low | The next pointer event fixes it. The remarks state the rule. routing-gate can add a test of the last position at each `Update`. |
| `Hover` on the hit element only surprises an author who expects the CSS rule. | Medium | Medium | Notes Q2. `IsPickable = false` on a label inside a button keeps the state on the button. |
| The visibility change in `PaintWalk` changes the draw. | Low | High | Two modifiers change, and no body. The draw tests and the draw gates run in phase 3. |
| Many moves in one frame cost many hit tests. | Low | Low | The cull of the clip skips clipped subtrees. A 1000-element walk costs microseconds. A move merge waits for a measured need. |
| The window test fails on a machine with a display and no GPU. | Low | Low | The window uses `GLFW_NO_API`, so it needs no GPU. Any init or window failure gives a skip with a reason. |
| Until routing-gate, the ring of a game with the adapter fills and drops events. | High | Low | Intended by the build order. `DroppedEventCount` shows it. |

---

## Dependencies

### Internal Dependencies
- `input-routing/event-model`: the structs, the ring, `PushEvent`, the internal `DrainEvents` and its target step, `IsInRootTree`, `EventWalk`, `EventRecorder`, and the gate pattern of `EventAllocationTests`. The work tree holds event-model with no commit yet. Start this feature after the event-model commit.
- `core-tree`: `Element`, `Parent`, `ChildStorage`, `IsPickable`, `Layout.Rect`, `UiContext`, `ElementStorage`, and `AllocationGate`.
- `style-system`: `PseudoStates`, `Element.States` (a change marks the subtree), and the `:hover` and `:active` selectors.
- `imgui-renderer`: `PaintWalk` (the clip helpers and the hidden rule), `RecordingRenderer`, and `ThousandElementTree`.

### External Dependencies
- `Ahwoo.BRUTAL.Glfw` 2026.10.4, pinned in `Directory.Packages.props`. It brings the native GLFW library. The feature `context.md` records the note (task 4.11).
- `ilspycmd` in the session scratch folder for task 4.5 only. No repository change.
- .NET 10 and `Brutal.Numerics` (`float2`, `float4`).

---

## Definition of Done

All criteria below must pass. An independent evaluator runs the Verification Method from `/home/aaronstatic/dev/brutal-ui` on Linux. "MANUAL" marks a criterion that needs the author on Windows.

### Functional Criteria
- [ ] F1. A pointer move with no target over a child sets `Hover` on the child. After `Update`, the child shows the fill of its `:hover` rule (`PointerHoverTests.AMoveOverAChildSetsHoverAndShowsTheHoverRule`).
- [ ] F2. A move to another element moves `Hover`. A move outside each root clears it and sets `PointerHit` to null.
- [ ] F3. A move walks to the hit element: root capture, target capture slot, target bubble slot, root bubble (`TheMoveWalksToTheHitElement`).
- [ ] F4. A press sets `Active` on the hit element. The release of the last button clears it, also over another element. A press over nothing sets no `Active` (`PointerPressTests`, 6 tests).
- [ ] F5. A scroll with no target walks to the element under the pointer and bubbles to its ancestors (`ScrollRoutingTests`, 4 tests).
- [ ] F6. The hit test takes the last root first and the last child first. It honors the padding-box clip of `Hidden` and `Scroll`, nested clips, `Display.None`, an opacity of 0, and `IsPickable` (self only). The right and bottom edges are outside. A NaN point hits nothing (`HitTestTests` 6, `HitTestRuleTests` 11).
- [ ] F7. A target from the caller skips the hit test and changes no state.
- [ ] F8. After a drain, an element that left the tree holds no `Hover` and no `Active` from the context (`PointerStateEdgeTests`, 7 tests).
- [ ] F9. `RaiseFileDrop` and `RaiseContentScale` call their slots at once, queue no event, and change no state (`WindowCallbackTests`, 4 tests).
- [ ] F10. `GlfwUiInput` pushes a move, a down, an up, and a scroll with the right type, position, button, modifiers, and timestamp. A cursor leave clears `Hover`. A drop and a content-scale change reach the callbacks. `Dispose` stops the pushes (`GlfwUiInputTests`, 10 tests).
- [ ] F11. `PointerAllocationTests` measures 0 bytes for 100 rounds of the hit test on the warm 1000-element tree, and for 100 steps of the full pointer path with `Update`. `GlfwUiInputAllocationTests` measures 0 bytes for the adapter handlers.
- [ ] F12. A planted allocation at the start of `HitWalk.Find` gives a non-zero exit code of `dotnet test`.

### Quality Criteria
- [ ] Q1. `dotnet build brutal-ui.sln` gives `0 Warning(s)` and `0 Error(s)`.
- [ ] Q2. `dotnet test --solution brutal-ui.sln` gives 0 failed. `BrutalUi.Core.Tests` has 46 more tests than the count in `context.md` before phase 1 (6 + 5 + 6 + 4 + 11 + 4 + 7 + 3). `BrutalUi.Glfw.Tests` has 11 tests.
- [ ] Q3. No method of `HitWalk` calls itself. `HitWalk.cs` holds no copy of the clip math and calls `PaintWalk.PaddingBounds` and `PaintWalk.Intersect`.
- [ ] Q4. `HitWalk` and `HitFrame` are internal. `HitTest`, `PointerHit`, `PointerPosition`, `HasPointerPosition`, `ActiveElement`, `HitStackLength`, and `HitStackSlots` are internal. The test seam constructor and the `Push*` methods of `GlfwUiInput` are internal.
- [ ] Q5. The measured bodies of the two new gate classes hold no lambda, no `$"`, no `string`, and no `Assert`.
- [ ] Q6. The changed docs use ASD-STE100 prose. The lint gives no hit above 2.5.
- [ ] Q7. `src/BrutalUi.Core/Input/EventWalk.cs` has the same SHA-256 as at the start of phase 1 (the value in `context.md`).
- [ ] Q8. (MANUAL) `scripts\test-windows.ps1` prints its PASS line on Windows, and `ARealWindowSubscribesAndUnsubscribes` passes there with no skip.

### Integration Criteria
- [ ] I1. `BrutalUi.Core` has no `using` of a GLFW namespace and holds 2 `PackageReference` items (`Ahwoo.BRUTAL.Numerics` and `Ahwoo.BRUTAL.Localization`).
- [ ] I2. `src/BrutalUi.Glfw/BrutalUi.Glfw.csproj` references `Ahwoo.BRUTAL.Glfw` and `BrutalUi.Core`. Its package lists exactly these two dependencies. `Directory.Packages.props` does not change.
- [ ] I3. `brutal-ui.sln` holds `BrutalUi.Glfw` under `src` and `BrutalUi.Glfw.Tests` under `tests`.
- [ ] I4. `UiContext.Update` does not call `DrainEvents`, and `ThousandElementAllocationTests.cs` does not change (routing-gate owns both).
- [ ] I5. The drain changes only its target step and adds the stale check at its start. `PushEvent` keeps four public overloads.
- [ ] I6. `docs/features/input-routing/pointer-routing/context.md` holds the native dependency note and the API table of task 4.5.
- [ ] I7. Sections 3, 4, 5, 9, and 14 of `docs/technical-design.md`, the packages table of `README.md`, and `CHANGELOG.md` name the feature. Section 9 names `GlfwUiInput` and `IsPickable`.
- [ ] I8. `scripts/test-linux.sh` prints its PASS line, and `git diff main -- scripts/` is empty.

### Verification Method

Run steps 1 to 10 from `/home/aaronstatic/dev/brutal-ui` on Linux. Step 11 is MANUAL. `<scratch>` is any empty folder outside the repository.

1. `dotnet build brutal-ui.sln 2>&1 | tail -n 30`. Expect `0 Warning(s)` and `0 Error(s)` (Q1).
2. `dotnet test --solution brutal-ui.sln 2>&1 | tail -n 30`. Expect 0 failed. Compare the totals with the counts in `context.md` (Q2).
3. For each class, run `dotnet run --project tests/BrutalUi.Core.Tests/BrutalUi.Core.Tests.csproj -- --filter-class '*<Class>' 2>&1 | tail -n 5`. Expect `failed: 0` and these totals: `HitTestTests` 6, `PointerHoverTests` 5 (F1 to F3), `PointerPressTests` 6 (F4), `ScrollRoutingTests` 4 (F5), `HitTestRuleTests` 11 (F6), `WindowCallbackTests` 4 (F9), `PointerStateEdgeTests` 7 (F7, F8), and `PointerAllocationTests` 3 (F11). Read the test names in each file and match them to the criteria.
4. Run `dotnet run --project tests/BrutalUi.Glfw.Tests/BrutalUi.Glfw.Tests.csproj -- --filter-class '*GlfwUiInputTests' 2>&1 | tail -n 5`. Expect total 10 and `failed: 0`. One skip is allowed for `ARealWindowSubscribesAndUnsubscribes` with no display (F10). Run the same with `'*GlfwUiInputAllocationTests'`. Expect total 1 and `failed: 0` (F11).
5. Copy `src/BrutalUi.Core/Input/HitWalk.cs` to `<scratch>`. Add `GC.KeepAlive(new object());` as the first line of `Find`. Run `dotnet test --solution brutal-ui.sln > /dev/null 2>&1; echo "exit=$?"`. Expect a value that is not 0 (F12). Restore the file from the copy. Run step 2 again and expect 0 failed.
6. `grep -rn 'using .*Glfw' src/BrutalUi.Core; grep -n PackageReference src/BrutalUi.Core/BrutalUi.Core.csproj; git diff main --stat -- Directory.Packages.props tests/BrutalUi.Core.Tests/ThousandElementAllocationTests.cs scripts/`. Expect no line from the first grep, 2 lines (Numerics and Localization) from the second, and no diff (I1, I2, I4, I8).
7. `dotnet pack src/BrutalUi.Glfw/BrutalUi.Glfw.csproj -o <scratch>/pack 2>&1 | tail -n 5`, then `unzip -p <scratch>/pack/BrutalUi.Glfw.0.1.0-alpha.nupkg BrutalUi.Glfw.nuspec | grep -n 'dependency id'`. Expect exactly two lines: `Ahwoo.BRUTAL.Glfw` with version `2026.10.4` and `BrutalUi.Core` with version `0.1.0-alpha` (I2). `grep -n 'Glfw' brutal-ui.sln` shows both new projects (I3).
8. `grep -n 'DrainEvents' src/BrutalUi.Core/Tree/UiContext.cs`. Expect the declaration with `internal` and no call inside `Update` (I4). `grep -n 'public void PushEvent' src/BrutalUi.Core/Tree/UiContext.cs`. Expect 4 lines (I5). `grep -nE '^(public|internal).*(class|struct) ' src/BrutalUi.Core/Input/HitWalk.cs` gives two `internal` lines (Q4). `grep -n 'PaintWalk\.' src/BrutalUi.Core/Input/HitWalk.cs` shows the calls of Q3. `sha256sum src/BrutalUi.Core/Input/EventWalk.cs` equals the value in `context.md` (Q7).
9. `scripts/test-linux.sh; echo "exit=$?"`. Expect the PASS line and `exit=0` (I8).
10. Read sections 3, 4, 5, 9, and 14 of `docs/technical-design.md`, the packages table of `README.md`, the `## [Unreleased]` part of `CHANGELOG.md`, and the native dependency note and the GLFW API table of the feature `context.md` (I6, I7). Run `python3 /home/aaronstatic/.claude/skills/asd-ste100/scripts/ste-lint.py --fail-over 2.5` on each changed doc (Q6).
11. (MANUAL) On Windows, after `git pull --ff-only`, run `.\scripts\test-windows.ps1`. Expect the PASS line and `$LASTEXITCODE` 0. Run step 4 and expect no skip (Q8).

---

## Testing Strategy

### Unit Tests (in `tests/BrutalUi.Core.Tests/Input/`)
- `HitTestTests` (6): the order of roots and children, the gap, the miss, the overlap, and a 40-level chain with the stack growth and the clear.
- `PointerHoverTests` (5): the demo, the hover move, the clear, the walk to the hit, and the last hit.
- `PointerPressTests` (6): `Active` with one and two buttons, the bubble from the hit, the release elsewhere, the press over nothing, and the `:active` rule.
- `ScrollRoutingTests` (4): the scroll target, the miss, the hover move, and the caller target.
- `HitTestRuleTests` (11): the clip rules, the hidden rules, `IsPickable`, the edges, and NaN.
- `WindowCallbackTests` (4): the two callbacks with and with no slot.
- `PointerStateEdgeTests` (7): the stale rules, the caller target, the same-element move, and the layout of the last `Update`.
- Each test uses `PointerTree` or its own small tree, pushes hand-made events with a null target, and calls the internal `DrainEvents()`. No Core test uses GLFW.

### Adapter Tests (in `tests/BrutalUi.Glfw.Tests/`)
- `GlfwUiInputTests` (10): the conversions through the test seam and the internal `Push*` methods, the callbacks, `Dispose`, and one real-window lifecycle test that skips with no display.

### Allocation Tests
- `PointerAllocationTests` (3): the hit test gate on the 1000-element tree, the full pointer path gate with `Update`, and the real-work check.
- `GlfwUiInputAllocationTests` (1): the adapter handlers into a full ring.
- Each gate calls `AllocationGate.ReleaseAllocationContext()` before its measured region. routing-gate adds the queue case to `ThousandElementAllocationTests`.

### Integration Tests
- The pack check of task 4.11 and step 7 of the Verification Method.
- No HelloPanel change. routing-gate adds the visible demo.

### Manual Testing
- The Windows run of Q8. No visible output exists until routing-gate wires the drain.

---

## Related Documentation

- **Requirements:** `docs/features/input-routing/pointer-routing/requirements.md`
- **Epic:** `docs/features/input-routing/epic-overview.md`, `docs/features/input-routing/epic-requirements.md`
- **Siblings:** `docs/features/input-routing/event-model/implementation.md` and `context.md` (the seams and the decisions R1, P1 to P5, and CR-1 to CR-7), `docs/features/input-routing/keyboard-focus/requirements.md`, `docs/features/input-routing/routing-gate/requirements.md`
- **Design:** `docs/technical-design.md` sections 3, 4, 5 (Events), 8 (The draw walk), 9 (Input), and 14 (Testing)
- **Rules:** `AGENTS.md`, `.beast-mode/phase-handoff.md` sections 1 and 11, `.beast-mode/tool-rules.md`
- **Code patterns:** `src/BrutalUi.Core/Rendering/PaintWalk.cs` (the stack, the hidden rule, the clip), `src/BrutalUi.Core/Tree/UiContext.cs` (`DrainEvents`, `IsInRootTree`), `src/BrutalUi.ImGui/BrutalUi.ImGui.csproj` and `tests/BrutalUi.ImGui.Tests/BrutalUi.ImGui.Tests.csproj` (the project shape), `samples/HelloPanel/Program.cs` (the window, `GlfwBackend`, and `io.DisplaySize`), `tests/BrutalUi.Core.Tests/Input/EventAllocationTests.cs` (the gate)
- **Native dependency table:** `docs/features/dev-ops/solution-skeleton/context.md`, section "Native Dependencies"
- **External:** the GLFW input guide, [cursor position](https://www.glfw.org/docs/latest/input_guide.html#cursor_pos), [cursor enter and leave](https://www.glfw.org/docs/latest/input_guide.html#cursor_enter), [mouse buttons](https://www.glfw.org/docs/latest/input_guide.html#input_mouse_button), [scroll](https://www.glfw.org/docs/latest/input_guide.html#scrolling), [path drop](https://www.glfw.org/docs/latest/input_guide.html#path_drop), [content scale](https://www.glfw.org/docs/latest/window_guide.html#window_scale)

---

## Notes

### Open Scope Questions

The plan uses the recommended option (the first one) of each question.

**Q1. What is the demo of phase 1?**
- **A (recommended):** A unit test lays out a root with two children, adds a `:hover` rule, pushes a pointer move with no target over the second child, drains, and runs `Update`. It reads `Hover` and the hover fill on the second child (`--filter-class '*PointerHoverTests'`, total 5). Reason: it proves the hit test, the state, and the restyle with no window, before the adapter exists.
- B: A unit test calls the hit test only and reads the hit element. It proves less: no state and no restyle.
- C: A person moves the mouse over HelloPanel and sees a hover color. It needs the drain in `Update`, which belongs to routing-gate.

**Q2. Which elements get `Hover` and `Active`?**
- **A (recommended):** The hit element only. A child that must not take the state, for example the label of a button, sets `IsPickable = false`. Reason: the requirement names one element, the rule matches `Focus`, and it stays simple and exact.
- B: The hit element and each ancestor, as in CSS and Unity UI Toolkit. A rule such as `.card:hover` then holds over the children of the card. The context then stores the hover chain in a buffer and changes only the elements that leave or join the chain.

**Q3. Does the adapter clear the hover when the cursor leaves the window?**
- **A (recommended):** Yes. The adapter also subscribes to `OnCursorEnter` and pushes a move at `(NaN, NaN)` on a leave, which hits nothing. Reason: else `Hover` and the gate stay on after the cursor leaves the window.
- B: No. The adapter subscribes to the five events of the requirements only.

**Q4. Does the hit test skip an element that the draw hides with an opacity of 0?**
- **A (recommended):** Yes. The hit test skips what the draw skips: `Display.None` and an opacity product of 0 or less. Reason: a panel that faded out catches no click.
- B: `Display.None` only, as in CSS. An invisible panel then still blocks the pointer.

### Conflicts and Gaps

1. **`ContentInsets` and the hit test.** The epic says that imgui-renderer gives `LayoutNode.ContentInsets` for the hit test. The draw clip uses the padding box from the border widths, and section 8 (A7) of the technical design states that `ContentInsets` holds the padding plus the border. The hit test follows the draw (Decision 4) and does not read `ContentInsets`. A3 names "the clip rects", so no confirmed item conflicts.
2. **Section 9 of the technical design** names `UiInput` and `PickingEnabled`. The code has `IsPickable`. Task 6.6 updates the section with `GlfwUiInput` and `IsPickable`.
3. **A5 and the visible hover.** The requirements call this feature "the first one that a hover rule makes visible". The drain runs in `Update` only after routing-gate. In this feature, the hover shows in a test (phase 1 demo), not in a game. No confirmed item conflicts: A5 puts the drain in `Update`, and routing-gate wires it.
4. **The drop paths allocate.** A drop gives strings, so `RaiseFileDrop` and a UTF-8 conversion in the adapter can allocate at drop time. A drop is rare and comes from the user, so the 0-byte gates do not cover it. AGENTS.md asks for UTF-8 text. A path goes to a file API that takes a `string`, so the plan keeps strings.
5. **An event-model test changes its meaning.** `EventQueueTests.ADrainSkipsAnEventWithNoTarget` pushes a pointer down with no target. After phase 1, that entry runs a hit test. The root has an empty rect, so the hit test finds nothing and the test passes with no edit.
6. **"No native dependency" of the mission statement.** `BrutalUi.Glfw` brings GLFW through `Ahwoo.BRUTAL.Glfw`. A BRUTAL game with a window has the same package already, and the samples use it. `BrutalUi.Core` stays free of native code. Task 4.11 writes the note in `context.md`.
7. **For routing-gate.** A full-screen root with `IsPickable` true makes `PointerHit` an element at each point, so `PointerOverUi` stays true. The technical design (task 6.6) states that a layout root sets `IsPickable` to false. `PseudoStates.Disabled` is not read by this feature.
8. **Hover and moves.** The requirement names a move. Decision 7 also moves `Hover` on a press and a scroll, because each one gives a position. This extends the rule and does not contradict it.
9. **event-model has no commit yet.** The git status shows the event-model files as uncommitted on `feat/input-routing/event-model`. Phase 1 starts after that commit.
10. **The prose lint.** The planner had no shell, so it did not run `ste-lint.py` on this file. Task 6.8 runs it.

---

*This plan will be updated as implementation progresses. See context.md and tasks.md for current status.*
