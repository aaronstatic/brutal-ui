# input-routing/pointer-routing - Task Checklist

**Last Updated:** 2026-10-22 12:50
**Progress:** 44/44 tasks complete (100%)
**Advanced phases:** Phases 1 and 4 (ADVANCED, opus). Phases 2, 3, 5, and 6 are STANDARD (sonnet).

Source paths: `Core/` means `src/BrutalUi.Core/`. `Tests/` means `tests/BrutalUi.Core.Tests/`. `Glfw/` means `src/BrutalUi.Glfw/`. "The plan" means `implementation.md` of this feature. Its section "Implementation Phases" holds the rules that the tasks name. Each phase ends with `dotnet build brutal-ui.sln 2>&1 | tail -n 30` and `dotnet test --solution brutal-ui.sln 2>&1 | tail -n 30` (tool rule 5). Expect `0 Warning(s)`, `0 Error(s)`, and 0 failed. The projects have no implicit usings, so each new file holds its own `using` lines. Each public member needs an XML doc comment. Use the file-scoped `namespace BrutalUi.Core;` in Core, `namespace BrutalUi.Glfw;` in the adapter, and `namespace BrutalUi.Core.Tests;` or `namespace BrutalUi.Glfw.Tests;` in the tests. Prose in comments follows ASD-STE100. At the start of phase 1, record the SHA-256 of `src/BrutalUi.Core/Input/EventWalk.cs` in `context.md`. Each phase checks that the hash stays the same.

---

## Phase 1: Hover Under the Pointer (10/10 complete)

**Demo:** a unit test lays out a root with two children, adds a `:hover` rule, pushes a pointer move with no target over the second child, drains the queue, and runs `Update`. It reads `PseudoStates.Hover` on the second child and the hover fill in its `Style.BackgroundColor`. The developer runs `dotnet run --project tests/BrutalUi.Core.Tests/BrutalUi.Core.Tests.csproj -- --filter-class '*PointerHoverTests'` and reads a total of 5 with `failed: 0`.
**Needs:** none
**Agent tier:** ADVANCED (opus)
**Tier note:** This phase changes the drain of `UiContext`, the central class, and adds a second walk with an explicit stack. Keep the change of `DrainEvents` to the target step only. Do not change `EventWalk.cs`. Copy the stack rules of `PaintWalk` (the growth by doubling, the stale `ref` gotcha after a growth, and the clear in `finally`). The early return of a hit must also clear the used frames.

**Group A (the walk and the context, 3 files):**

- [x] ✅ **1.1 Write HitWalk.cs** - Completed 2026-10-22
  - Description: Write `src/BrutalUi.Core/Input/HitWalk.cs`: the internal struct `HitFrame` (`Element`, `Origin`, `NextChild`) and `internal static class HitWalk` with `internal const int InitialStackSize = 32` and `internal static Element? Find(ReadOnlySpan<Element> roots, float2 point, ref HitFrame[] stack)`. Follow the section "HitWalk.Find" with no hidden rule, no clip, and no `IsPickable` check (phase 3 adds them). The origin of an element is the origin of its parent plus `Layout.Rect.XY`, as step 3 of `PaintWalk.Enter`. Use the half-open test on the border box. Grow the stack with `Array.Resize(ref stack, Math.Max(stack.Length * 2, InitialStackSize))`. Clear the used frames in a `finally`, also after the return of a hit. The class remarks state the order (the last root first, the last child first, a descendant before its ancestor) and the `PaintWalk` pattern. No method calls itself.
  - File(s): `src/BrutalUi.Core/Input/HitWalk.cs`
  - Estimate: 8 steps

- [x] ✅ **1.2 Add the hit test and the stack members to UiContext.cs** - Completed 2026-10-22
  - Description: Edit `src/BrutalUi.Core/Tree/UiContext.cs`: add `private HitFrame[] _hitStack = new HitFrame[HitWalk.InitialStackSize];`, `internal Element? HitTest(float2 position)` (calls `HitWalk.Find(_roots.AsSpan(), position, ref _hitStack)`), `internal int HitStackLength`, and `internal ReadOnlySpan<HitFrame> HitStackSlots`. The remarks state: the hit test reads `Layout.Rect` of the last `Update`.
  - File(s): `src/BrutalUi.Core/Tree/UiContext.cs`
  - Estimate: 6 steps

- [x] ✅ **1.3 Add the last-hit fields and getters** - Completed 2026-10-22
  - Description: Edit `UiContext.cs`: add the fields `_pointerHit`, `_pointerPosition`, and `_hasPointerPosition`, and the internal getters `PointerHit`, `PointerPosition`, and `HasPointerPosition`. The remarks state: routing-gate reads `PointerHit` (A6). The value follows the pointer events only.
  - File(s): `src/BrutalUi.Core/Tree/UiContext.cs`
  - Estimate: 5 steps

- [x] ✅ **1.4 Add RoutePointer and SetHover** - Completed 2026-10-22
  - Description: Edit `UiContext.cs`: add `private Element? RoutePointer(ref UiQueuedEvent entry)` and `private void SetHover(Element? hit)`. In the target step of `DrainEvents`, when `entry.Header.Target` is null and the type is `PointerMove`, `PointerDown`, or `PointerUp`, call `target = RoutePointer(ref entry)` before the existing skip. `RoutePointer` reads `entry.Payload.Pointer.Position`, calls `HitTest`, writes `entry.Header.Target`, sets the three last-hit fields, and calls `SetHover`. `SetHover` returns at once for the same element. Else it clears `Hover` on the old element with `old.States &= ~PseudoStates.Hover`, stores the new element, and sets `Hover` on it. Leave a comment in `RoutePointer`: phase 2 adds `Active` and the scroll. keyboard-focus adds the click focus in the `PointerDown` branch.
  - File(s): `src/BrutalUi.Core/Tree/UiContext.cs`
  - Estimate: 8 steps

- [x] ✅ **1.5 Edit the doc comments of the target rule** - Completed 2026-10-22
  - Description: Edit the doc comments: the remarks of `PushEvent(in PointerEvent)` and of `DrainEvents` in `UiContext.cs` (a pointer event with no target gets the hit element as its target at the drain, against the layout of the last `Update`, and moves `Hover`). Edit `src/BrutalUi.Core/Input/UiEventHeader.cs`: the remarks of `Target` state the same rule. A target from the caller skips the hit test and the states (Decision 2).
  - File(s): `src/BrutalUi.Core/Tree/UiContext.cs`, `src/BrutalUi.Core/Input/UiEventHeader.cs`
  - Estimate: 4 steps

**Group B (the tests, 4 files):**

- [x] ✅ **1.6 Write PointerTree.cs** - Completed 2026-10-22
  - Description: Write `tests/BrutalUi.Core.Tests/Input/PointerTree.cs`: an `internal sealed class PointerTree` with a context (`Measurer = new RecordingRenderer()`, `ViewportSize = (800, 600)`), a root of 400 x 300 with `FlexDirection.Row`, two children `Left` and `Right` of 100 x 100, and a child `Inner` of 50 x 50 in `Right`. Set the sizes with `Inline.Width` and `Inline.Height`. The constructor takes a name prefix and interns a class `<prefix>.hoverable` on `Left` and `Right`, because xUnit runs the classes in parallel. `AddHoverSheet()` adds a rule `.hoverable:hover` with `BackgroundColor = HoverFill` (the pattern of `PseudoStateSelectorTests`). Add `Settle()` (`Update(0f)`), `static PointerEvent Move(float2 p)`, `Down(float2 p, PointerButton b)`, `Up(float2 p, PointerButton b)`, `static ScrollEvent Scroll(float2 p, float2 delta)` (each with a null target), and `static float4 ViewportRect(Element e)` (the sum of the parent rects). The class links into `BrutalUi.Glfw.Tests` in phase 4. Do not put it in `Shared/`: `BrutalUi.ImGui.Tests` links `Shared/*.cs` and has no access to the internals of Core.
  - File(s): `tests/BrutalUi.Core.Tests/Input/PointerTree.cs`
  - Estimate: 7 steps

- [x] ✅ **1.7 Write HitTestTests.cs** - Completed 2026-10-22
  - Description: Write `tests/BrutalUi.Core.Tests/Input/HitTestTests.cs` with 6 tests: `APointInAChildHitsTheChild` (also `Inner` in `Right`), `APointInTheGapHitsTheParent`, `APointOutsideEachRootHitsNothing`, `TheLaterSiblingWinsWhereTwoChildrenOverlap` (two absolute children), `TheLastRootWinsWhereTwoRootsOverlap`, and `ATreeDeeperThanTheStackHitsTheLeaf` (a chain of 40 levels, `HitStackLength` of 41 or more, and each slot of `HitStackSlots` holds a null element after the call).
  - File(s): `tests/BrutalUi.Core.Tests/Input/HitTestTests.cs`
  - Estimate: 6 steps

- [x] ✅ **1.8 Write PointerHoverTests.cs** - Completed 2026-10-22
  - Description: Write `tests/BrutalUi.Core.Tests/Input/PointerHoverTests.cs` with 5 tests: `AMoveOverAChildSetsHoverAndShowsTheHoverRule` (the demo), `AMoveToASiblingMovesHover` (`Left` loses the state, `Right` gets it), `AMoveOutsideClearsHover`, `TheMoveWalksToTheHitElement` (an `EventRecorder` on the root and `Right` reads root capture, `Right` target capture, `Right` target bubble, and root bubble), and `TheContextStoresTheLastHit` (`PointerHit`, `PointerPosition`, and `HasPointerPosition`).
  - File(s): `tests/BrutalUi.Core.Tests/Input/PointerHoverTests.cs`
  - Estimate: 6 steps

- [x] ✅ **1.9 Run the event-model classes** - Completed 2026-10-22
  - Description: Run the event-model classes with no change: `EventWalkTests` (13), `EventKindTests` (12), `EventQueueTests` (9), `EventWalkEdgeTests` (5), and `EventAllocationTests` (3). `EventQueueTests.ADrainSkipsAnEventWithNoTarget` pushes a pointer down with no target. Its root has an empty rect, so the hit test finds nothing and the test still passes. Do not edit the test.
  - File(s): `tests/BrutalUi.Core.Tests/BrutalUi.Core.Tests.csproj` (run only)
  - Estimate: 3 steps

- [x] ✅ **1.10 Run the build, the tests, and the demo, and record** - Completed 2026-10-22
  - Description: Run the build and the tests. Run the demo command and `--filter-class '*HitTestTests'`. Record the test counts before and after, the hash of `EventWalk.cs`, and each decision that the brief did not settle in `docs/features/input-routing/pointer-routing/context.md`.
  - File(s): `docs/features/input-routing/pointer-routing/context.md`
  - Estimate: 4 steps

---

## Phase 2: Press, Release, and Scroll (6/6 complete)

**Demo:** a unit test pushes a pointer down with no target over `Right`, drains, and reads `PseudoStates.Active` on `Right`. A bubble handler on the root sees `Target` `Right` and `CurrentTarget` root. A pointer up clears the state. The developer runs `dotnet run --project tests/BrutalUi.Core.Tests/BrutalUi.Core.Tests.csproj -- --filter-class '*PointerPressTests'` and reads a total of 6 with `failed: 0`.
**Needs:** Phase 1
**Agent tier:** STANDARD (sonnet)

- [x] ✅ **2.1 Add Active and the pressed-button mask** - Completed 2026-10-22
  - Description: Edit `UiContext.cs`: add `_activeElement`, `_pressedButtons` (an `int` bit mask), and `internal Element? ActiveElement`. In `RoutePointer`, for `PointerDown`: set the bit `1 << (int)button` when `(uint)button < 32`. When `_activeElement` is null and the hit is not null, set `Active` on the hit and store it. For `PointerUp`: clear the bit. When the mask is 0 and `_activeElement` is not null, clear `Active` on it and set the field to null (Decision 8). The release walks to the element under the pointer, not to the pressed element. Pointer capture is out of scope.
  - File(s): `src/BrutalUi.Core/Tree/UiContext.cs`
  - Estimate: 7 steps

- [x] ✅ **2.2 Route the scroll** - Completed 2026-10-22
  - Description: Edit `UiContext.cs`: the target step also sends a `Scroll` entry with no target to `RoutePointer`. `RoutePointer` reads `entry.Payload.Scroll.Position` for a scroll. A scroll also moves `Hover` and the last hit (Decision 7).
  - File(s): `src/BrutalUi.Core/Tree/UiContext.cs`
  - Estimate: 5 steps

- [x] ✅ **2.3 Edit the doc comments of the scroll and Active rules** - Completed 2026-10-22
  - Description: Edit the doc comments: the remarks of `PushEvent(in ScrollEvent)` and of `DrainEvents` (the scroll rule and the `Active` rule).
  - File(s): `src/BrutalUi.Core/Tree/UiContext.cs`
  - Estimate: 3 steps

- [x] ✅ **2.4 Write PointerPressTests.cs** - Completed 2026-10-22
  - Description: Write `tests/BrutalUi.Core.Tests/Input/PointerPressTests.cs` with 6 tests: `APressSetsActiveAndTheReleaseClearsIt` (the demo), `APressBubblesFromTheHitElement` (the root bubble handler reads `Target` and `CurrentTarget`), `AReleaseOverAnotherElementClearsTheFirst` (the up walks to `Left`, and `Right` loses `Active`), `ASecondButtonKeepsActiveUntilTheLastRelease`, `APressOverNothingSetsNoActive`, and `AnActiveRuleShowsAfterUpdate` (a rule `.hoverable:active` and `Settle()`). Extend `PointerTree` with an active rule when the tests need it.
  - File(s): `tests/BrutalUi.Core.Tests/Input/PointerPressTests.cs`, `tests/BrutalUi.Core.Tests/Input/PointerTree.cs`
  - Estimate: 8 steps

- [x] ✅ **2.5 Write ScrollRoutingTests.cs** - Completed 2026-10-22
  - Description: Write `tests/BrutalUi.Core.Tests/Input/ScrollRoutingTests.cs` with 4 tests: `AScrollGoesToTheElementUnderThePointer` (the scroll bubbles from `Inner` to `Right` and the root, and the delta and the position survive), `AScrollOverNothingWalksNothing`, `AScrollMovesHover`, and `AScrollWithACallerTargetKeepsTheTarget`.
  - File(s): `tests/BrutalUi.Core.Tests/Input/ScrollRoutingTests.cs`
  - Estimate: 6 steps

- [x] ✅ **2.6 Run the build, the tests, and the demo, and record** - Completed 2026-10-22
  - Description: Run the build and the tests. Run the demo command and `--filter-class '*ScrollRoutingTests'`. Record the results and the decisions in `context.md`.
  - File(s): `docs/features/input-routing/pointer-routing/context.md`
  - Estimate: 4 steps

---

## Phase 3: The Hit Rules (5/5 complete)

**Demo:** a unit test puts a child half outside a parent with `Overflow.Hidden`. A point in the visible half hits the child. A point in the clipped half hits the sibling under it. The developer runs `dotnet run --project tests/BrutalUi.Core.Tests/BrutalUi.Core.Tests.csproj -- --filter-class '*HitTestRuleTests'` and reads a total of 11 with `failed: 0`.
**Needs:** Phase 2
**Agent tier:** STANDARD (sonnet)

- [x] ✅ **3.1 Make PaddingBounds and Intersect internal** - Completed 2026-10-22
  - Description: Edit `src/BrutalUi.Core/Rendering/PaintWalk.cs`: change `PaddingBounds` and `Intersect` from `private static` to `internal static`. Do not change a body. Add one remark to each: `HitWalk` shares the method, so the hit clip equals the draw clip.
  - File(s): `src/BrutalUi.Core/Rendering/PaintWalk.cs`
  - Estimate: 4 steps

- [x] ✅ **3.2 Add the clip, hidden, and IsPickable rules to HitWalk.cs** - Completed 2026-10-22
  - Description: Edit `HitWalk.cs`: add `Opacity`, `OwnClip`, and `ChildClip` to `HitFrame`. Follow the section "HitWalk.Find" in full. The hidden rule copies steps 1 and 2 of `PaintWalk.Enter`: `Display.None`, and an opacity product of 0 or less after the clamp (a value below 0 counts as 0, above 1 as 1, and NaN as 1), skip the subtree (Notes Q4). A point outside the clip of the parent skips the subtree. A point outside the child clip sets `NextChild` to -1, so the walk tests the element and skips its children. The self test reads `IsPickable` first. An element with `IsPickable` false passes the hit to the next element. Its children stay pickable (Decision 5).
  - File(s): `src/BrutalUi.Core/Input/HitWalk.cs`
  - Estimate: 8 steps

- [x] ✅ **3.3 Edit the remarks of HitWalk** - Completed 2026-10-22
  - Description: Edit the remarks of `HitWalk`: list the rules in the order of the code. State the half-open edges, the NaN rule, and that the hit test does not read `LayoutNode.ContentInsets` (Notes, Conflicts 1).
  - File(s): `src/BrutalUi.Core/Input/HitWalk.cs`
  - Estimate: 3 steps

- [x] ✅ **3.4 Write HitTestRuleTests.cs** - Completed 2026-10-22
  - Description: Write `tests/BrutalUi.Core.Tests/Input/HitTestRuleTests.cs` with 11 tests: `AnOverflowHiddenParentClipsItsChild` (the demo), `AnOverflowScrollParentClipsItsChild`, `NestedClipsIntersect`, `AVisibleOverflowLetsAChildOutsideTheParentBeHit`, `ThePaddingBoxClipKeepsTheBorderForTheParent` (a point on the border of a clip parent, over a child that extends under the border, hits the parent), `AnUnpickableLeafPassesTheHitToItsParent`, `AnUnpickableParentKeepsItsChildrenPickable` (a point in the gap passes to the grandparent), `ADisplayNoneSubtreeIsNotHit`, `AZeroOpacitySubtreeIsNotHit`, `TheRightAndBottomEdgesAreOutside`, and `ANaNPointHitsNothing`.
  - File(s): `tests/BrutalUi.Core.Tests/Input/HitTestRuleTests.cs`
  - Estimate: 8 steps

- [x] ✅ **3.5 Run the build, the tests, the draw gates, and the demo, and record** - Completed 2026-10-22
  - Description: Run the build and the tests. Check that the draw tests and the draw gates still pass (`--filter-class '*PaintWalk*'` and `*ThousandElementAllocationTests`). Run the demo command. Record the results in `context.md`.
  - File(s): `docs/features/input-routing/pointer-routing/context.md`
  - Estimate: 4 steps

---

## Phase 4: The GLFW Adapter (11/11 complete)

**Demo:** a unit test in `BrutalUi.Glfw.Tests` creates a `GlfwUiInput` with the test seam (no window), calls its internal `PushCursorMove` and `PushButton` over `Right` of the pointer tree, drains, and reads `Hover` and `Active` on `Right`. The developer runs `dotnet run --project tests/BrutalUi.Glfw.Tests/BrutalUi.Glfw.Tests.csproj -- --filter-class '*GlfwUiInputTests'` and reads a total of 10 with `failed: 0`.
**Needs:** Phase 3
**Agent tier:** ADVANCED (opus)
**Tier note:** This phase creates a package project and a test project, changes the public API of `UiContext`, and binds to an API that the planner could not read in full. Do task 4.5 before any code of group B. The XML doc of `Brutal.Glfw` names the events but gives no parameter types. Find the delegate signatures and the way that `GlfwWindow` installs its native callbacks. When `GlfwWindow` sets the native callback at the first subscription, a later subscription can replace the callback of the ImGui `GlfwBackend`. Record the safe creation order. Do not change `Directory.Packages.props`: it pins `Ahwoo.BRUTAL.Glfw` 2026.10.4 already. When the real API cannot carry a requirement (for example, no drop paths as strings), stop and report to the main thread.

**Group A (the Core callbacks, 4 files):**

- [x] ✅ **4.1 Write UiWindowEvents.cs** - Completed 2026-10-22
  - Description: Write `src/BrutalUi.Core/Input/UiWindowEvents.cs`: the public delegates `UiFileDropHandler(ReadOnlySpan<string> paths)` and `UiContentScaleHandler(float2 scale)`. The remarks cite A4: the window events get no per-element routing.
  - File(s): `src/BrutalUi.Core/Input/UiWindowEvents.cs`
  - Estimate: 4 steps

- [x] ✅ **4.2 Add the window callbacks to UiContext.cs** - Completed 2026-10-22
  - Description: Edit `UiContext.cs`: add the public slots `OnFileDrop` and `OnContentScale`, and the public methods `RaiseFileDrop(ReadOnlySpan<string> paths)` and `RaiseContentScale(float2 scale)`. Each method calls its slot at once when the slot is not null. It queues no entry, walks no element, and changes no state. The remarks state: call the methods on the main thread. The span is valid during the call only. The input adapter calls them. A test or another platform layer can call them too.
  - File(s): `src/BrutalUi.Core/Tree/UiContext.cs`
  - Estimate: 6 steps

- [x] ✅ **4.3 Add InternalsVisibleTo for the adapter tests** - Completed 2026-10-22
  - Description: Edit `src/BrutalUi.Core/BrutalUi.Core.csproj`: add `<InternalsVisibleTo Include="BrutalUi.Glfw.Tests" />` with a comment: the adapter tests call the drain and read the last hit. Add no `PackageReference`.
  - File(s): `src/BrutalUi.Core/BrutalUi.Core.csproj`
  - Estimate: 3 steps

- [x] ✅ **4.4 Write WindowCallbackTests.cs** - Completed 2026-10-22
  - Description: Write `tests/BrutalUi.Core.Tests/Input/WindowCallbackTests.cs` with 4 tests: `ADropWithNoCallbackDoesNothing`, `ADropPassesEachPathInOrder`, `AContentScalePassesTheScale`, and `ACallbackQueuesNoEventAndChangesNoState` (`QueuedEventCount` 0, `PointerHit` null, and no new dirty bit).
  - File(s): `tests/BrutalUi.Core.Tests/Input/WindowCallbackTests.cs`
  - Estimate: 5 steps

**Group B (the package, 6 files):**

- [x] ✅ **4.5 Read the real API of Ahwoo.BRUTAL.Glfw** - Completed 2026-10-22
  - Description: Read the real API of `Ahwoo.BRUTAL.Glfw` 2026.10.4. Decompile `Brutal.Glfw.dll` with `ilspycmd` in the session scratch folder, as `docs/features/dev-ops/solution-skeleton/context.md` did. Commit no decompiled file. Record a table in `context.md`: the delegate type and the parameter types of `OnCursorPos`, `OnMouseButton`, `OnScroll`, `OnCursorEnter`, `OnDrop`, and `OnContentScale`, the type of `GlfwWindow` (class or struct), `GetKey`, `CursorPos`, `Glfw.Time`, and the install rule of the native callbacks with the safe order next to `GlfwBackend`.
  - File(s): `docs/features/input-routing/pointer-routing/context.md`
  - Estimate: 9 steps

- [x] ✅ **4.6 Write BrutalUi.Glfw.csproj and add the projects to the solution** - Completed 2026-10-22
  - Description: Write `src/BrutalUi.Glfw/BrutalUi.Glfw.csproj` in the shape of `BrutalUi.ImGui.csproj`: a `Description`, `<InternalsVisibleTo Include="BrutalUi.Glfw.Tests" />`, `<PackageReference Include="Ahwoo.BRUTAL.Glfw" />`, and `<ProjectReference Include="../BrutalUi.Core/BrutalUi.Core.csproj" />`. `src/Directory.Build.props` makes it packable. Add the project and the test project of task 4.9 to the solution: `dotnet sln brutal-ui.sln add --solution-folder src src/BrutalUi.Glfw/BrutalUi.Glfw.csproj` and the same with `--solution-folder tests` for the test project.
  - File(s): `src/BrutalUi.Glfw/BrutalUi.Glfw.csproj`, `brutal-ui.sln`
  - Estimate: 6 steps

- [x] ✅ **4.7 Write GlfwUiInput.cs** - Completed 2026-10-22
  - Description: Write `src/BrutalUi.Glfw/GlfwUiInput.cs`: the `public sealed class GlfwUiInput : IDisposable` of the Public API section. The constructor throws `ArgumentNullException` for a null window or context. It creates each GLFW handler delegate one time in a field and subscribes it. It seeds the last cursor position from `window.CursorPos`. Add the test seam `internal GlfwUiInput(UiContext context)`: it throws for a null context, subscribes to no event, and starts at the position (0, 0). `Dispose` unsubscribes each handler (none for the seam) and sets a flag. After `Dispose`, each `Push*` method returns at once. A second `Dispose` does nothing. Each GLFW handler converts and calls an internal `Push*` method. A cursor position becomes a `float2` with no scale (Decision 13). A button is cast to `PointerButton` with no table, also for the buttons 3 to 7 (Decision 14). A press gives `PointerDown` and a release gives `PointerUp`, with `(UiModifiers)((int)mods & 0x3F)`. A scroll gives `Delta` as a `float2` and the last cursor position as `Position`. The timestamp comes from `GlfwApi.Time`. A drop calls `Context.RaiseFileDrop`, and a content-scale event calls `Context.RaiseContentScale`. Each push uses a null target.
  - File(s): `src/BrutalUi.Glfw/GlfwUiInput.cs`
  - Estimate: 9 steps

- [x] ✅ **4.8 Add the cursor leave, the modifiers, and the class remarks** - Completed 2026-10-22
  - Description: Add the cursor leave (Notes Q3) and the modifiers of a move and a scroll to `GlfwUiInput.cs`. `OnCursorEnter` with "left" calls `PushCursorLeave`, which pushes a `PointerMove` at `(float.NaN, float.NaN)` and sets the last cursor position to it. "Entered" does nothing. A move and a scroll read the modifiers from `GetKey` of the left and right Shift, Control, Alt, and Super keys (Decision 14). Write the class remarks: create the adapter on the main thread, the creation order with `GlfwBackend` from task 4.5, dispose the adapter before the window, one context for each adapter, the position space (Decision 13), and the fact that `Update` does not drain until routing-gate.
  - File(s): `src/BrutalUi.Glfw/GlfwUiInput.cs`
  - Estimate: 7 steps

- [x] ✅ **4.9 Write BrutalUi.Glfw.Tests.csproj** - Completed 2026-10-22
  - Description: Write `tests/BrutalUi.Glfw.Tests/BrutalUi.Glfw.Tests.csproj` in the shape of `BrutalUi.ImGui.Tests.csproj`: `OutputType` `Exe`, `xunit.v3`, the linked `../BrutalUi.Core.Tests/Shared/*.cs` with `LinkBase="Shared"`, the linked `../BrutalUi.Core.Tests/Input/PointerTree.cs` and `../BrutalUi.Core.Tests/Input/EventRecorder.cs` with `LinkBase="Input"`, and a `ProjectReference` to `BrutalUi.Glfw`.
  - File(s): `tests/BrutalUi.Glfw.Tests/BrutalUi.Glfw.Tests.csproj`
  - Estimate: 5 steps

- [x] ✅ **4.10 Write GlfwUiInputTests.cs** - Completed 2026-10-22
  - Description: Write `tests/BrutalUi.Glfw.Tests/GlfwUiInputTests.cs` with 10 tests. Each test except the last creates the adapter with the test seam `new GlfwUiInput(context)` and calls the `Push*` methods. Tests: `ACursorMoveAndAPressHoverAndActivateTheChild` (the demo), `AMoveCarriesThePositionTheModifiersAndTheTime`, `EachButtonCastsWithNoTable` (the values 0 to 7), `APressAndAReleaseGiveDownAndUp`, `AScrollUsesTheLastCursorPosition`, `ACursorLeaveClearsHover`, `ADropReachesTheContextCallback`, `AContentScaleReachesTheContextCallback`, `DisposeStopsThePushes`, and `ARealWindowSubscribesAndUnsubscribes`. The last test calls `GlfwApi.Init()` and creates a hidden window with `GLFW_NO_API`. When the init or the window fails (no display), it calls `Assert.Skip` with the reason. In a `finally`, it disposes the adapter, then the window, then calls `Terminate`.
  - File(s): `tests/BrutalUi.Glfw.Tests/GlfwUiInputTests.cs`
  - Estimate: 8 steps

- [x] ✅ **4.11 Run the build, the tests, the pack, and the checks, and record** - Completed 2026-10-22
  - Description: Run the build and the tests. Run the demo command. Run `dotnet pack src/BrutalUi.Glfw/BrutalUi.Glfw.csproj -o <scratch>/pack` and read the dependencies of the `.nuspec` in the package: `Ahwoo.BRUTAL.Glfw` 2026.10.4 and `BrutalUi.Core` 0.1.0-alpha. Run `grep -rn 'using .*Glfw' src/BrutalUi.Core` (no output). Write the native dependency note in `context.md` (AGENTS.md rule): `BrutalUi.Glfw` brings the native GLFW library (`libglfw.so` on Linux, `glfw3.dll` on Windows) through `Ahwoo.BRUTAL.Glfw`. The samples use the same package already. The table of `docs/features/dev-ops/solution-skeleton/context.md`, section "Native Dependencies", lists it. `BrutalUi.Core` stays free of native code. Record the results.
  - File(s): `docs/features/input-routing/pointer-routing/context.md`
  - Estimate: 7 steps

---

## Phase 5: Stale State Rules (4/4 complete)

**Demo:** a unit test hovers `Right`, removes it from the root, and drains an empty queue. It reads no `Hover` on `Right` and a null `PointerHit`. The developer runs `dotnet run --project tests/BrutalUi.Core.Tests/BrutalUi.Core.Tests.csproj -- --filter-class '*PointerStateEdgeTests'` and reads a total of 7 with `failed: 0`.
**Needs:** Phase 4
**Agent tier:** STANDARD (sonnet)

- [x] ✅ **5.1 Add DropStalePointerState** - Completed 2026-10-22
  - Description: Edit `UiContext.cs`: add `private void DropStalePointerState()` and call it at the start of `DrainEvents`, inside the `_draining` guard. When `_pointerHit` is not null and `IsInRootTree(_pointerHit)` is false, call `SetHover(null)` and set `_pointerHit` to null. When `_activeElement` is not null and is not in the tree, clear `Active` on it and set the field to null. Keep `_pressedButtons`, so a later release still clears the bit (Decision 10).
  - File(s): `src/BrutalUi.Core/Tree/UiContext.cs`
  - Estimate: 6 steps

- [x] ✅ **5.2 Edit the remarks of the stale rules** - Completed 2026-10-22
  - Description: Edit the remarks of `DrainEvents` and of `PointerHit`: the stale rule, the rule that the states follow the pointer events only (a layout change under a still pointer waits for the next pointer event), and the rule that a target from the caller changes no state.
  - File(s): `src/BrutalUi.Core/Tree/UiContext.cs`
  - Estimate: 3 steps

- [x] ✅ **5.3 Write PointerStateEdgeTests.cs** - Completed 2026-10-22
  - Description: Write `tests/BrutalUi.Core.Tests/Input/PointerStateEdgeTests.cs` with 7 tests: `AHoveredElementThatLeavesTheTreeLosesHover` (the demo), `ARemovedRootLosesHover` (`RemoveRoot`), `AnActiveElementThatLeavesTheTreeLosesActive` (the release still routes to the hit element), `ACallerTargetChangesNoState`, `AMoveOverTheSameElementMarksNothing` (after `Settle()`, a second move leaves no `Style` bit in `Dirty`), `AnElementAddedAfterUpdateIsNotHitUntilTheNextUpdate`, and `ADrainWithNoPointerEventKeepsAValidHover`.
  - File(s): `tests/BrutalUi.Core.Tests/Input/PointerStateEdgeTests.cs`
  - Estimate: 8 steps

- [x] ✅ **5.4 Run the build, the tests, and the demo, and record** - Completed 2026-10-22
  - Description: Run the build and the tests. Run the demo command. Record the results in `context.md`.
  - File(s): `docs/features/input-routing/pointer-routing/context.md`
  - Estimate: 4 steps

---

## Phase 6: The Allocation Gates and the Record (8/8 complete)

**Demo:** the developer runs `dotnet run --project tests/BrutalUi.Core.Tests/BrutalUi.Core.Tests.csproj -- --filter-class '*PointerAllocationTests'` and reads a total of 3 with `failed: 0`. A planted allocation at the start of `HitWalk.Find` makes `dotnet test --solution brutal-ui.sln` exit non-zero.
**Needs:** Phase 5
**Agent tier:** STANDARD (sonnet)
**Tier note:** When a gate measures bytes above 0, find the cause before a change. Run the body with one event kind at a time in a scratch copy of the test. Expected suspects: a `ReadOnlySpan` over a field that copies a struct, a boxed `PseudoStates`, a delegate created in the body, or a growth of the hit stack after the warm-up. Fix the cause in `src/` with no public API change. Do not add warm-up rounds to hide a growth.

**Group A (the gates, 3 files. Task 6.8 records the results):**

- [x] ✅ **6.1 Write PointerAllocationTests.cs with the hit-test gate** - Completed 2026-10-22
  - Description: Write `tests/BrutalUi.Core.Tests/Input/PointerAllocationTests.cs` in the pattern of `EventAllocationTests`: a private `Scene` from `ThousandElementTree.Build()` with one `Update(0f)` for the layout. Compute 8 points from `PointerTree.ViewportRect` outside the measure: a grid slot point outside its icon and its badge (hits the slot), an icon point, a badge point, a modal point, a grid point under the clip of the grid, a tooltip point, a point over the hidden settings panel, and a point outside the root. Hold the prepared events as `required` fields (event-model P5-6). Add `[Fact] TheHitTestAllocatesNothingOnAWarmTree`: 2 warm-up rounds, `AllocationGate.ReleaseAllocationContext()`, then 100 rounds of `HitTest` over the 8 points, and `Assert.Equal(0L, after - before)`.
  - File(s): `tests/BrutalUi.Core.Tests/Input/PointerAllocationTests.cs`
  - Estimate: 8 steps

- [x] ✅ **6.2 Add the full pointer path gate** - Completed 2026-10-22
  - Description: Add `[Fact] PointerRoutingAllocatesNothingAfterWarmUp`: each step pushes a move to the slot point, a down, a move to the modal point, an up, a scroll at the slot point, and a move outside the root. Then it calls `DrainEvents()` and `Update(0.016f)`. Measure 100 steps after 2 warm-up steps. The body holds no lambda, no boxing, no string, and no `Assert`.
  - File(s): `tests/BrutalUi.Core.Tests/Input/PointerAllocationTests.cs`
  - Estimate: 5 steps

- [x] ✅ **6.3 Add the real-work check** - Completed 2026-10-22
  - Description: Add `[Fact] TheMeasuredBodyDoesRealWork`: assert the hit of each of the 8 points. After a move to the slot point and `Update`, the slot holds `Hover` and the border color `ThousandElementTree.SlotHoverBorderColor`. After one full step, no element holds `Hover` or `Active`, and counter handlers on the slot and the modal read the expected walks.
  - File(s): `tests/BrutalUi.Core.Tests/Input/PointerAllocationTests.cs`
  - Estimate: 6 steps

- [x] ✅ **6.4 Write GlfwUiInputAllocationTests.cs** - Completed 2026-10-22
  - Description: Write `tests/BrutalUi.Glfw.Tests/GlfwUiInputAllocationTests.cs` with `[Fact] TheAdapterHandlersAllocateNothing`: fill the ring of a context with capacity 8, warm up, release the allocation context, and measure 1000 calls of each of `PushCursorMove`, `PushButton`, `PushScroll`, and `PushCursorLeave`. Assert 0 bytes and the growth of `DroppedEventCount`.
  - File(s): `tests/BrutalUi.Glfw.Tests/GlfwUiInputAllocationTests.cs`
  - Estimate: 6 steps

- [x] ✅ **6.5 Prove that the gate blocks** - Completed 2026-10-22
  - Description: Prove that the gate blocks. Copy `src/BrutalUi.Core/Input/HitWalk.cs` to the session scratch folder. Add `GC.KeepAlive(new object());` as the first line of `Find`. Run `dotnet test --solution brutal-ui.sln > /dev/null 2>&1; echo "exit=$?"` and expect a non-zero exit. Restore the file with `cp`, check it with `cmp`, run the tests again, and expect 0 failed. Report both exit codes and the names of the failed gates in the hand-back.
  - File(s): `src/BrutalUi.Core/Input/HitWalk.cs` (temporary edit, restored)
  - Estimate: 7 steps

**Group B (the record, 4 files):**

- [x] ✅ **6.6 Edit the technical design** - Completed 2026-10-22
  - Description: Edit `docs/technical-design.md`. Section 3: add `BrutalUi.Glfw/` and `BrutalUi.Glfw.Tests/` to the tree, and a paragraph for `BrutalUi.Glfw` under "Directory Responsibilities". Section 4: in the overview, replace `UiInput` with `GlfwUiInput ──► UiContext.PushEvent`. Section 5, "Events": add the pointer step of the drain (the hit test, `Hover`, `Active`, and the last hit). Section 9, "Flow": replace `UiInput` with `GlfwUiInput` and `PickingEnabled` with `IsPickable`, and state the hit rules, the state rules, the rule that a layout root sets `IsPickable` to false, and the two callbacks. Section 14: add the pointer test classes and the gates `PointerAllocationTests` and `GlfwUiInputAllocationTests`, with the plant result of task 6.5.
  - File(s): `docs/technical-design.md`
  - Estimate: 8 steps

- [x] ✅ **6.7 Edit the README and the changelog** - Completed 2026-10-22
  - Description: Edit `README.md`: add the row `BrutalUi.Glfw` (the GLFW input adapter, references `Brutal.Glfw` and `BrutalUi.Core`) to the packages table, and change "the five packages" to "the six packages". Edit `CHANGELOG.md`: add one line under `## [Unreleased]`, `### Added`: the hit test, the `Hover` and `Active` states, the scroll routing, the window callbacks, and the `BrutalUi.Glfw` package.
  - File(s): `README.md`, `CHANGELOG.md`
  - Estimate: 5 steps

- [x] ✅ **6.8 Run the build, the tests, the Linux script, and the STE lint, and record** - Completed 2026-10-22
  - Description: Run the build, the tests, and `scripts/test-linux.sh; echo "exit=$?"`. Run `python3 /home/aaronstatic/.claude/skills/asd-ste100/scripts/ste-lint.py --fail-over 2.5` on each changed doc and on this plan, and fix each hit. Record the gate results of group A, the plant result, and the final test counts in `context.md`.
  - File(s): `docs/features/input-routing/pointer-routing/context.md`
  - Estimate: 7 steps

---

## Bugs & Issues

**Active Bugs:**
- none

---

## Technical Debt

- [ ] 💳 **A full ring can drop a `PointerUp` (CR-5)** - Priority: Medium
  - Description: `Active` stays until the next press and release of the button. The remarks state the limit.
  - Reason: a move merge at the full limit changes the drop-oldest rule of the event-model ring.
  - Effort: routing-gate drains each frame and sizes the ring. A move merge needs its own decision.
- [ ] 💳 **The stale rule across two contexts (CR-8)** - Priority: Low
  - Description: the stale check of one context can clear a state that another context set, when an element moves between contexts.
  - Reason: a full fix needs the owner context on the element.
  - Effort: Medium. The remarks state the rule: drain the old context first.

---

## Progress Tracking

### Completed This Session (2026-10-22)
- ✅ Phases 1 to 6 (44 tasks). Each phase verifier gave PASS and Demo: PASS.
- ✅ The cross-phase review: 9 findings (CR-1 to CR-9), all resolved. The verifier of the fixes gave PASS.

### Discovered New Tasks
- The review fixes (`code-review.md`): 4 test gaps (CR-1 to CR-4), the real-handler gate (CR-6), the single clip cull of `HitWalk` (CR-7), two remark limits (CR-5, CR-8), and the demo tests (CR-9). All done.

---

## Task Status Legend

- `[ ]` Not started
- `[ ]` 🔄 In progress
- `[ ]` ⏸️ Blocked (waiting on something)
- `[x]` ✅ Completed
- `[x]` ❌ Cancelled/Won't do
