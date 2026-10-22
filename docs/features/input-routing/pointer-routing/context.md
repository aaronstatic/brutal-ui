# input-routing/pointer-routing - Context & Decisions

**Last Updated:** 2026-10-22 12:50
**Current Phase:** Done (phases 1 to 6 and the cross-phase review)
**Status:** ✅ Ready for Review

---

## Quick Status

**What's Done:**
- ✅ The plan is accepted. The feature docs exist (`tasks.md`, `context.md`, `inbox.md`).
- ✅ event-model is committed on `main` (commit `1647228`). The branch `feat/input-routing/pointer-routing` holds this feature.
- ✅ Phase 1: `HitWalk`, the pointer step of the drain, `Hover`, and the last hit. The verifier gave PASS and Demo: PASS. Solution 2174 tests, 0 failed. See "Phase 1 Results".
- ✅ Phase 2: `Active` with the button mask, and the scroll routing. The verifier gave PASS and Demo: PASS. Solution 2184 tests, 0 failed. See "Phase 2 Results".
- ✅ Phase 3: the clip, hidden, and pickable rules of the hit test. `PaintWalk` shares `PaddingBounds` and `Intersect`. The verifier gave PASS and Demo: PASS. Solution 2195 tests, 0 failed. See "Phase 3 Results".
- ✅ Phase 4: the window callbacks and the `BrutalUi.Glfw` package with `GlfwUiInput`. The verifier gave PASS and Demo: PASS. Solution 2209 tests, 0 failed, 1 skipped (the real-window test with no display). See "Phase 4 Results".
- ✅ Phase 5: the stale state rule at the start of the drain. The verifier gave PASS and Demo: PASS. Solution 2216 tests, 0 failed, 1 skipped. See "Phase 5 Results".
- ✅ Phase 6: the allocation gates (`PointerAllocationTests`, `GlfwUiInputAllocationTests`), the plant check, and the docs. The verifier gave PASS and Demo: PASS. Solution 2220 tests, 0 failed, 1 skipped. See "Phase 6 Results".
- ✅ The cross-phase review (`code-review.md`) found 9 findings (6 MEDIUM, 3 LOW). All 9 are resolved, and the verifier of the fixes gave PASS. See "Review Fixes".
- ✅ The draft plan of the next feature exists: `docs/features/input-routing/keyboard-focus/implementation.md` (6 phases, 42 tasks). It holds four open scope questions in its Notes.

**What's Next:**
- 📋 A person runs the items of "Needs Human Verification", including the Windows run.
- 📋 `/autorun-epic` commits this feature and runs `input-routing/keyboard-focus`.

**Blockers:**
- None

---

## Key Files

### Core Implementation
- `src/BrutalUi.Core/Input/HitWalk.cs` - The hit test walk with an explicit stack (phases 1 and 3).
- `src/BrutalUi.Core/Tree/UiContext.cs` - The hit stack, the pointer step of the drain, the states, and the window callbacks.
- `src/BrutalUi.Core/Input/UiWindowEvents.cs` - The two public callback delegates (phase 4).
- `src/BrutalUi.Glfw/GlfwUiInput.cs` - The GLFW adapter (phase 4).

### Related Files
- `src/BrutalUi.Core/Rendering/PaintWalk.cs` - The clip helpers and the hidden rule that the hit test shares.
- `tests/BrutalUi.Core.Tests/Input/` - The pointer test classes and the gate.
- `tests/BrutalUi.Glfw.Tests/` - The adapter tests.
- `docs/features/input-routing/pointer-routing/implementation.md` - The implementation plan.

---

## Important Decisions

### Decision R1: The caller accepted option A of each open scope question
**Date:** 2026-10-22
**Context:** The Notes of the plan hold four open scope questions: the phase 1 demo, the elements that get `Hover` and `Active`, the cursor leave, and the opacity rule of the hit test.
**Decision:** The plan uses option A of each question. `/autorun-epic` states that the user confirmed the four answers.
**Rationale:** The user accepted the plan with these answers.
**Impact:** The phase 1 demo is the hover unit test. `Hover` and `Active` go on the hit element only. The adapter clears the hover through `OnCursorEnter` with a move at `(NaN, NaN)`. The hit test skips `Display.None` and an opacity product of 0 or less.

### Decision R2: event-model is committed
**Date:** 2026-10-22
**Context:** Notes, Conflicts 9 of the plan says that event-model has no commit yet.
**Decision:** The conflict is closed. event-model is on `main`, and this branch starts from it.
**Impact:** Phase 1 starts with no wait.

### Decision R3: The hash of EventWalk.cs at the start
**Date:** 2026-10-22
**Context:** The plan asks each phase to check that `src/BrutalUi.Core/Input/EventWalk.cs` does not change.
**Decision:** The SHA-256 at the start of phase 1 is `e84ec520c8acc5b4db4fb8180d2bd4695d40f7547506c2e94ef795b9706307d8`.
**Impact:** Each verifier compares the hash with this value.

---

## Phase 1 Results

**Group A (the walk and the context):** `HitWalk.cs` is new. `UiContext.cs` and `UiEventHeader.cs` changed. Build 0 warnings and 0 errors. Solution 2163 tests, 0 failed. `EventWalk.cs` keeps the hash of Decision R3.

**Group A decisions (the brief did not settle them):**
- P1-1: `SetHover` reads `_pointerHit` as the old owner, then stores the hit in it (Decision 7 uses one field). `RoutePointer` does not write `_pointerHit` itself. It writes the target, the position, and `_hasPointerPosition`, and then it calls `SetHover`.
- P1-2: Any routed pointer entry sets `HasPointerPosition` to true, also for a NaN position (a cursor leave). `PointerPosition` then holds NaN.
- P1-3: A pointer entry that hits nothing gets no walk. It still clears `Hover` and sets `PointerHit` to null.
- P1-4: `HitFrame` keeps only the 3 fields of phase 1. The border-box test reads `Layout.Rect` again for the width and the height.
- P1-5: The remarks of `PushEvent(in PointerEvent)` no longer say "the caller sets the target". The new remarks state that the hit test applies to the pointer types only.

**Risks that group A found:**
- Phase 2: the scroll and pointer payloads overlap in memory. `RoutePointer` must branch on the type before it reads `Payload.Scroll.Position`. Add `Scroll` to the type pattern of the target step.
- Phase 3: `Enter` in the walk loop must copy the opacity and the clip before the call, because a growth of the stack makes the `ref` stale.
- Phase 5: `SetHover` clears `Hover` only on `_pointerHit`. A `Hover` that a caller sets by hand on another element stays.

**Group B (the test tree and the tests):** `PointerTree.cs`, `HitTestTests.cs`, and `PointerHoverTests.cs` are new in `tests/BrutalUi.Core.Tests/Input/`. No `src/` file changed in group B.

| Check | Result |
|-------|--------|
| `dotnet build brutal-ui.sln` | 0 Warning(s), 0 Error(s) |
| `dotnet test --solution brutal-ui.sln` before phase 1 tests | total 2163, failed 0 (the group A run) |
| `dotnet test --solution brutal-ui.sln` after | total 2174, failed 0, skipped 0 |
| Demo: `--filter-class '*PointerHoverTests'` | total 5, failed 0 |
| `--filter-class '*HitTestTests'` | total 6, failed 0 |
| Event-model classes, no edit | `EventWalkTests` 13, `EventKindTests` 12, `EventQueueTests` 9, `EventWalkEdgeTests` 5, `EventAllocationTests` 3: 42 pass, 0 failed |
| SHA-256 of `EventWalk.cs` | `e84ec520c8acc5b4db4fb8180d2bd4695d40f7547506c2e94ef795b9706307d8` (the value of Decision R3) |
| Recursion in `HitWalk` | None. `Find` calls `Enter` and `Contains`. `Enter` and `Contains` call no method of `HitWalk`. |

**Group B decisions (the brief did not settle them):**
- P1-6: `PointerTree` uses the public API only (the constructor of `UiContext`, `ClassName.Intern`, `Selector`, and `StyleSheet.Builder`). `HoverFill` is `(48, 96, 192, 255)`, the `Hovered` color of `PseudoStateSelectorTests`. Each event has the timestamp 0 and `UiModifiers.None`. `Move` sets `PointerButton.Left`, because a move has no button parameter.
- P1-7: `ViewportRect` adds the rect position of the element and of each ancestor, the root included. The rect of a root holds its margin and its offsets.
- P1-8: Each test of `HitTestTests` first checks the viewport rects of the layout, so a layout fault shows as a layout fault and not as a hit fault. `APointInAChildHitsTheChild` also checks the half-open edges (Decision 6). `(100, 0)` hits `Inner`, and `(100, 75)`, `(150, 25)`, and `(125, 50)` hit `Right`.
- P1-9: `APointOutsideEachRootHitsNothing` also checks a NaN point and a point with one NaN component. `ATreeDeeperThanTheStackHitsTheLeaf` gives each level 10 x 10, because a leaf with an empty rect hits nothing. It also checks that a miss leaves no element in the stack.
- P1-10: `TheLaterSiblingWinsWhereTwoChildrenOverlap` puts two absolute children at `(10, 10)` and `(50, 50)`, each 100 x 100, in a root of 300 x 300. It checks the overlap, each part outside the overlap, and the root outside both children.
- P1-11: The hover tests call `DrainEvents()` and check its walk count: 1 for a hit and 0 for a miss (P1-3). `AMoveOutsideClearsHover` also checks a move to `(NaN, NaN)`, the cursor leave of Decision R1. The demo test writes the name, the states, and the fill of `Right` to the test output.

**Risks that group B found:**
- Phase 2: the drain skips `PointerTree.Scroll(...)` now, because the type pattern of the target step has no `Scroll`. The scroll tests fail until phase 2 adds it.
- Phase 2: `PointerTree.Move` sets `PointerButton.Left`. The `Active` step must branch on the event type, not on the button.
- Phase 3: `Element.IsPickable` exists (default `true`), and `HitWalk` does not read it yet. The plan adds it in phase 3.
- Phase 4: `PointerTree` needs `Shared/RecordingRenderer.cs` in `BrutalUi.Glfw.Tests`. A Glfw test that calls `DrainEvents()` or reads `PointerHit` needs `InternalsVisibleTo` from Core.

---

## Phase 2 Results

**Files:** `UiContext.cs` changed. `PointerTree.cs` has `ActiveFill` and `AddActiveSheet`. `PointerPressTests.cs` (6 tests) and `ScrollRoutingTests.cs` (4 tests) are new.

| Check | Result |
|-------|--------|
| `dotnet build brutal-ui.sln` | 0 Warning(s), 0 Error(s) |
| `dotnet test --solution brutal-ui.sln` | total 2184, failed 0, skipped 0 (2174 + 10 new) |
| Demo: `--filter-class '*PointerPressTests'` | total 6, failed 0 |
| `--filter-class '*ScrollRoutingTests'` | total 4, failed 0 |
| SHA-256 of `EventWalk.cs` | `e84ec520c8acc5b4db4fb8180d2bd4695d40f7547506c2e94ef795b9706307d8` (the value of Decision R3) |

**Decisions (the brief did not settle them):**
- P2-1: `RoutePointer` reads the position by type: `Payload.Scroll.Position` for `Scroll`, `Payload.Pointer.Position` for the pointer types. The type pattern of the target step has `Scroll`.
- P2-2: The `Active` logic lives in two private methods, `PressButton` and `ReleaseButton`. `RoutePointer` calls them after `SetHover`, so a handler sees both states (Decision 9). Neither method allocates.
- P2-3: A `PointerUp` with the mask at 0 and a stored active element clears the state. This happens for an up of a button that was never down in this context. It matches the rule of the brief (mask 0 and element not null).
- P2-4: A press over nothing sets the mask bit and no `Active`. The element hit by a later press, while the mask is not 0 and `_activeElement` is null, gets `Active`.
- P2-5: `ActiveElement` is `internal` and read-only. It has an XML comment. The tests read it, and routing-gate can read it.
- P2-6: A button value of 32 or more, or a negative value, sets and clears no mask bit. A release of it still clears `Active` when the mask is 0.
- P2-7: `PointerTree.ActiveFill` is `(200, 64, 64, 255)`. `AddActiveSheet` adds `.hoverable:active`.
- P2-8: The demo test `APressSetsActiveAndTheReleaseClearsIt` writes the state and the active element name to the test output after the press and after the release.

**Risks:**
- Phase 3: a hit element that becomes `IsPickable == false` or leaves the tree while it holds `Active` keeps the state until a release. The release clears it on the stored element, so no stale state stays, but the element with the state can differ from the element under the pointer.
- Phase 5: `Active` and `_pressedButtons` have no reset on `RemoveRoot`. A removed active element keeps `Active` until the last release. Phase 5 (stale state rules) must clear `_activeElement` when its root leaves, and must clear the mask on a cursor leave or on a lost focus.
- Phase 5: a lost release (the pointer released outside the window) leaves the mask above 0. `Active` then stays. Pointer capture is out of scope.

---

## Phase 3 Results

**Files:** `PaintWalk.cs` (two modifiers and two remarks), `HitWalk.cs` (rules, 3 new `HitFrame` fields), and `HitTestRuleTests.cs` (11 tests, new).

| Check | Result |
|-------|--------|
| `dotnet build brutal-ui.sln` | 0 Warning(s), 0 Error(s) |
| `dotnet test --solution brutal-ui.sln` | total 2195, failed 0, skipped 0 (2184 + 11 new) |
| Demo: `dotnet run --project tests/BrutalUi.Core.Tests/BrutalUi.Core.Tests.csproj -- --filter-class '*HitTestRuleTests'` | total 11, failed 0 |
| `--filter-class '*PaintWalk*'` | total 29, failed 0 |
| `--filter-class '*ThousandElementAllocationTests'` | total 12, failed 0 |
| `git diff --stat` of `PaintWalk.cs` | 4 insertions, 2 deletions (2 modifiers, 2 remark lines) |
| SHA-256 of `EventWalk.cs` | `e84ec520c8acc5b4db4fb8180d2bd4695d40f7547506c2e94ef795b9706307d8` (unchanged) |

**Decisions (the brief did not settle them):**
- P3-1: `HitFrame.OwnClip` is the child clip of the parent, and `ChildClip` is the clip for the children. `Opacity` is the product after the clamp. `Find` copies `Origin`, `Opacity`, and `ChildClip` into locals before `Enter`.
- P3-2: A private `InBounds(in float4, float2)` does the half-open test for a clip and for the border box (`Contains` calls it for `OwnClip`). The root clip `NoClip` is a private copy of the infinite bounds of `PaintWalk` (the brief shares only `PaddingBounds` and `Intersect`).
- P3-3: The opacity clamp copies steps 1 and 2 of `PaintWalk.Enter`, so the hit rule and the draw rule agree. `Enter` tests the point against the parent clip after the hidden rule, as the algorithm lists it.
- P3-4: `ADisplayNoneSubtreeIsNotHit` and `AZeroOpacitySubtreeIsNotHit` write `StyleStorage` after `Settle`. The layout engine zeroes the rect of a hidden element, so only a style write after the layout tests the hidden rule alone. The tests need no `Update` after the write.
- P3-5: The demo hit in the clipped half is the root (the element under the parent), not a sibling. The test tree has no sibling there. The brief names the root.
- P3-6: The remarks of `HitWalk` use `<c>PaintWalk.PaddingBounds</c>` and `<c>PaintWalk.Intersect</c>`, because a cref to a member of an internal class is fine, but the plain form avoids a doc warning if a member changes later.

**Risks:**
- Phase 5: a hidden (`Display.None`), zero-opacity, or unpickable element that holds `Hover` or `Active` keeps the state until the next move or release. Phase 5 owns the stale rules. `SetHover` runs before the walk with the last hit, so it sees the new hit on the next move.
- Phase 6: the allocation gate must warm up `HitFrame` growth. `HitFrame` is now 56 bytes (was 24), so a deep tree grows the stack to a larger array than before.

---

## Phase 4 Results

**Group A (the Core callbacks):** `UiWindowEvents.cs` (2 delegates) and `WindowCallbackTests.cs` (4 tests) are new. `UiContext.cs` has `OnFileDrop`, `OnContentScale`, `RaiseFileDrop`, and `RaiseContentScale`. `BrutalUi.Core.csproj` has `InternalsVisibleTo` for `BrutalUi.Glfw.Tests`. Decisions P4-1 to P4-4 are in the brief of group B.

**Group B (the adapter):** `src/BrutalUi.Glfw/BrutalUi.Glfw.csproj`, `src/BrutalUi.Glfw/GlfwUiInput.cs`, `tests/BrutalUi.Glfw.Tests/BrutalUi.Glfw.Tests.csproj`, and `tests/BrutalUi.Glfw.Tests/GlfwUiInputTests.cs` are new. `brutal-ui.sln` holds `BrutalUi.Glfw` under `src` and `BrutalUi.Glfw.Tests` under `tests`.

### The API of Ahwoo.BRUTAL.Glfw 2026.10.4 (task 4.5)

Source: `ilspycmd` 11.1.0 output of `Brutal.Glfw.dll` and `Brutal.ImGui.Backends.dll` in the session scratch folder. The repository holds no decompiled file. The namespace is `Brutal.GlfwApi`. The static class is `Brutal.GlfwApi.Glfw`, so the adapter uses the alias `GlfwApi`.

| Member | Kind | Type and parameters |
|---|---|---|
| `GlfwWindow` | type | `public sealed class GlfwWindow : IDisposable, INullable` (a class, not a struct) |
| `OnCursorPos` | field-like event | `GlfwCursorPosCallback(GlfwWindow window, double2 pos)` |
| `OnMouseButton` | field-like event | `GlfwMouseButtonCallback(GlfwWindow window, GlfwMouseButton button, GlfwButtonAction action, GlfwModifier mods)` |
| `OnScroll` | field-like event | `GlfwScrollCallback(GlfwWindow window, double2 offset)` |
| `OnCursorEnter` | field-like event | `GlfwCursorEnterCallback(GlfwWindow window, bool entered)` |
| `OnDrop` | field-like event | `GlfwDropCallback(GlfwWindow window, int count, string[] paths)` |
| `OnContentScale` | field-like event | `GlfwWindowContentScaleCallback(GlfwWindow window, float2 scale)` |
| `GetKey` | method | `GlfwKeyAction GetKey(GlfwKey key)`. `GlfwKeyAction` is `Release` 0, `Press` 1, `Repeat` 2. |
| `CursorPos` | property | `double2 CursorPos { get; set; }`. The getter returns a cached value. The constructor and the cursor callback refresh it. |
| `Glfw.Time` | static property | `double Time { get; set; }`, in seconds, through `glfwGetTime`. |
| `GlfwMouseButton` | enum | `Number1` 0 to `Number8` 7. `Left` 0, `Right` 1, `Middle` 2, `Last` 7. |
| `GlfwButtonAction` | enum | `Release` 0, `Press` 1. |
| `GlfwModifier` | flags | `Shift` 1, `Control` 2, `Alt` 4, `Super` 8, `CapsLock` 0x10, `NumLock` 0x20. These equal `UiModifiers`. |
| Modifier keys | `GlfwKey` | `LeftShift` 340, `LeftControl` 341, `LeftAlt` 342, `LeftSuper` 343, `RightShift` 344, `RightControl` 345, `RightAlt` 346, `RightSuper` 347. |

**The install rule of the native callbacks:** the constructor of `GlfwWindow` sets all 20 native GLFW callbacks one time, with `glfwSet*Callback`, and pins each delegate with a `GCHandle`. Each native callback raises the managed event. The events are field-like multicast events. A subscription adds a managed handler and sets no native callback. `GlfwWindow.Dispose` destroys the window and frees the handles. It does not clear the events. Each native callback catches an exception from a handler and gives it to `Glfw.ExceptionAggregator`. The exception stops the later handlers of the same event.

**The ImGui `GlfwBackend`:** it subscribes with `+=` to `OnMouseButton`, `OnScroll`, `OnCursorPos`, `OnCursorEnter`, `OnKey`, `OnChar`, and `OnFocus`, and removes them with `-=` in `Dispose`. It sets no native callback. It does not subscribe to `OnDrop` or `OnContentScale`.

**The safe creation order:** each order is safe, because neither `GlfwBackend` nor `GlfwUiInput` replaces a native callback. Both get each event. The handlers run in the order of the subscriptions. The recommended order: create the window, then `GlfwBackend`, then `GlfwUiInput`. Then ImGui gets each event first, and an exception in a brutal-ui handler cannot stop the ImGui handler. Dispose `GlfwUiInput`, then `GlfwBackend`, then the window. Call `Glfw.Terminate` last.

### Native dependency note (AGENTS.md rule)

`BrutalUi.Glfw` brings the native GLFW library (`libglfw.so` on Linux, `glfw3.dll` on Windows, `libglfw.dylib` on macOS) through `Ahwoo.BRUTAL.Glfw` 2026.10.4. The samples use the same package already. The table of `docs/features/dev-ops/solution-skeleton/context.md`, section "Native Dependencies", lists it. `BrutalUi.Glfw` adds no other native library. `BrutalUi.Core` stays free of native code: `grep -rn 'using .*Glfw' src/BrutalUi.Core --include=*.cs` gives no output.

### Results

| Check | Result |
|-------|--------|
| `dotnet build brutal-ui.sln` | 0 Warning(s), 0 Error(s) |
| `dotnet test --solution brutal-ui.sln` | total 2209, failed 0, succeeded 2208, skipped 1 (2199 + 10 new) |
| Demo: `dotnet run --project tests/BrutalUi.Glfw.Tests/BrutalUi.Glfw.Tests.csproj -- --filter-class '*GlfwUiInputTests'` | total 10, failed 0, succeeded 9, skipped 1 (no display) |
| The same demo under `xvfb-run -a` | total 10, failed 0, succeeded 10, skipped 0 |
| Skip reason with no display | `GLFW init failed: PlatformUnavailable (Failed to detect any supported platform). The machine has no display.` |
| `dotnet pack src/BrutalUi.Glfw/BrutalUi.Glfw.csproj` | `BrutalUi.Glfw.0.1.0-alpha.nupkg`. The nuspec lists 2 dependencies: `BrutalUi.Core` 0.1.0-alpha and `Ahwoo.BRUTAL.Glfw` 2026.10.4. |
| `grep -rn 'using .*Glfw' src/BrutalUi.Core` | no output |
| `PackageReference` items in `BrutalUi.Core.csproj` | 2. `Directory.Packages.props` has no change. |
| SHA-256 of `EventWalk.cs` | `e84ec520c8acc5b4db4fb8180d2bd4695d40f7547506c2e94ef795b9706307d8` (unchanged) |

**Decisions of group B (the brief did not settle them):**
- P4-5: The adapter has two more internal push methods, `PushFileDrop(ReadOnlySpan<string>)` and `PushContentScale(float2)`, so the drop and scale tests use the test seam. Each has the guard of `Dispose`. The internal static `ButtonModifiers(GlfwModifier)` holds the `0x3F` mask, so a test checks the mask with no window.
- P4-6: The public constructor chains to the seam constructor through `RequireContext`. A null window or a null context throws `ArgumentNullException` with `ParamName` `info`. The seam creates the six delegates too, so the delegate fields are never null.
- P4-7: A move and a cursor leave carry `PointerButton.Left`, as `PointerTree.Move` does. A cursor leave carries `UiModifiers.None`. A button event also uses the last cursor position, because GLFW gives a button event no position.
- P4-8: `HandleDrop` clamps the count to the array length and passes a span of the array. The binding creates a new `string[]` for each drop, so the adapter keeps no array, and the reuse risk of group A does not apply.
- P4-9: The modifiers of a move and a scroll never hold Caps Lock or Num Lock, because `GetKey` has no lock state. A button event holds these bits only when the application turns on the GLFW input mode `LockKeyMods`.
- P4-10: A handler reads the modifiers from the `window` parameter of the event, not from the stored field. Both are the same window.
- P4-11: The scroll delta keeps the GLFW sign (a positive y is a scroll away from the user). `ScrollEvent.Delta` states no sign rule.
- P4-12: The real-window test reads the backing field of each field-like event with reflection. It counts the handlers with the adapter as the target, before and after `Dispose`. It calls the `OnCursorPos` delegate to drive one conversion through `GetKey` and `Glfw.Time`. The null checks of both constructors run before `Glfw.Init`, so they also run with no display.

**Risks:**
- Phase 6: the handler path calls `GetKey` up to 8 times for each move and scroll, and `Glfw.Time` one time. Each BRUTAL P/Invoke wrapper calls `Log.Trace(...)`. The seam path skips these calls. The allocation gate must measure the handler path: a hidden window under `xvfb-run`, with the events raised through the backing delegate, as `ARealWindowSubscribesAndUnsubscribes` does.
- Phase 6: a drop allocates in the binding (a `string[]` and one string for each path). Keep the drop out of the gate. A subscription `+=` allocates a combined delegate, at construction only.
- keyboard-focus: the native char callback of `GlfwWindow` raises `OnChar` and `OnCharMods` with `(ushort)point`, although the delegates take `uint`. A code point above U+FFFF (for example an emoji) arrives truncated. The adapter cannot restore it. Report it to BRUTAL, or accept the Basic Multilingual Plane only.
- keyboard-focus: `OnKey` is `GlfwKeyCallback(GlfwWindow window, GlfwKey key, int scancode, GlfwKeyAction action, GlfwModifier mods)`. A repeat is the action `Repeat` (2). `GlfwBackend` also subscribes to `OnKey` and `OnChar`. Both handlers get each event, and the binding has no consume rule. The arbitration between ImGui and the UI focus belongs to keyboard-focus.
- Phase 5: after a cursor leave, the last position is NaN. A press or a scroll before the next move hits no element. On X11, GLFW sends a cursor position event with each enter.
- `Glfw.Terminate` destroys the native windows, but a managed `GlfwWindow` keeps its pointer. Dispose each window before `Terminate`.

---

## Phase 5 Results

**Changes:** `UiContext.cs` has the new `DropStalePointerState`. `DrainEvents` calls it first, inside the `_draining` guard and before the count read. The remarks of `DrainEvents` and `PointerHit` state the stale rule, the rule that the states follow the pointer events only, and the rule that a target from the caller changes no state. `PointerStateEdgeTests.cs` is new (7 tests). `EventWalk.cs` keeps its SHA-256 `e84ec520...6307d8`.

**Results:** `dotnet build brutal-ui.sln` gives 0 Warning(s) and 0 Error(s). `dotnet test --solution brutal-ui.sln` gives total 2216, failed 0, succeeded 2215, skipped 1 (`ARealWindowSubscribesAndUnsubscribes`, no display). The demo command `dotnet run --project tests/BrutalUi.Core.Tests/BrutalUi.Core.Tests.csproj -- --filter-class '*PointerStateEdgeTests'` gives total 7, failed 0.

**Decisions:**
- P5-1: `DropStalePointerState` clears `Hover` through `SetHover(null)`, so the old owner loses the bit and `_pointerHit` becomes null in one step. It clears `Active` directly and sets `_activeElement` to null. `_pressedButtons` stays (Decision 10).
- P5-2: the check runs before the count read, so it also runs for an empty drain. A test drain with no entry drops the state.
- P5-3: after a removal, a release still routes to the element under the pointer, and a press after that release sets `Active` again, because the release cleared the mask bit.
- P5-4: `AnElementAddedAfterUpdateIsNotHitUntilTheNextUpdate` adds a 100 x 100 child at x 200. Before `Update`, the hit test does not return it (its layout rect is empty).

**Risks:**
- Phase 6: the stale check adds two `IsInRootTree` parent walks to each drain, with no allocation. The allocation gate needs no extra warm-up for it.
- A stored element that moves to another root of the same context keeps its state, because it is still in the roots.

---

## Phase 6 Results

**Group A (the gates, tasks 6.1 to 6.5):**
- New files: `tests/BrutalUi.Core.Tests/Input/PointerAllocationTests.cs` (3 tests) and `tests/BrutalUi.Glfw.Tests/GlfwUiInputAllocationTests.cs` (1 test). `src/` needed no fix.
- Build 0 warnings. Solution 2220 tests, 0 failed, 1 skipped (`ARealWindowSubscribesAndUnsubscribes` with no display).
- `*PointerAllocationTests`, 5 runs: total 3 and failed 0 in each run (0 bytes). `GlfwUiInputAllocationTests`: 0 bytes, and `DroppedEventCount` grows by 4000.
- The plant (`GC.KeepAlive(new object());` as the first line of `HitWalk.Find`): exit 2. The failed gates: `PointerAllocationTests.TheHitTestAllocatesNothingOnAWarmTree` and `PointerAllocationTests.PointerRoutingAllocatesNothingAfterWarmUp`. The restore with `cp` gave equal files (`cmp`), exit 0, and 0 failed. SHA-256 of `HitWalk.cs` before and after: `4645af8e7251408a5f5a92e458bae85c5d5057197f19bba2dce415e50c9ef51b`.

**Group A decisions:**
- P6-1: no grid slot overflows the grid in this layout (the grid is 592 high, the last slot ends at y 646, inside the clip at y 648). The "grid point under the clip" is a point right of the last slot of the last row. The walk tests all 120 slots and hits the grid.
- P6-2: the "settings point" is the center of stat row 0. The settings panel has `Display.None`, so the walk skips it, and the hit lies in the stat row.
- P6-3: the move outside the root gets no walk. A step has 5 walks from 6 events.
- P6-4: `HitRound` stores a hit count in `Scene.HitSum`, so the compiler cannot drop the hit tests.
- P6-5: the adapter gate uses a second context with `RecordingRenderer` and capacity 8. The warm-up is 2 rounds of 8 pushes of each kind. The remarks name the real-handler limit (`GetKey`, `Glfw.Time`, `Log.Trace`) and the drop and content-scale exclusion.

**Group B (the record, tasks 6.6 to 6.8):**
- `docs/technical-design.md`: section 3 (tree and the `BrutalUi.Glfw` paragraph), section 4 (overview line), section 5 (the drain paragraph and the new pointer step), section 9 (the new flow), and section 14 (two new bullets). `README.md` has the `BrutalUi.Glfw` row and "the six packages". `CHANGELOG.md` has one new line under `### Added`.
- Search of `docs/technical-design.md` for `UiInput` and `PickingEnabled`: the only hits are the substring in `GlfwUiInput` (section 3, 4, and 9). No whole-word `UiInput` and no `PickingEnabled` stays. Section 9 names `GlfwUiInput` and `IsPickable`.
- Lint (`ste-lint.py --fail-over 2.5`, per 100 words): `docs/technical-design.md` 1.51, `README.md` 0.51, `CHANGELOG.md` 1.68, `implementation.md` 1.62. No score is above 2.5, so `implementation.md` has no change.
- `dotnet build brutal-ui.sln`: 0 Warning(s), 0 Error(s).
- `dotnet test --solution brutal-ui.sln`: total 2220, failed 0, succeeded 2219, skipped 1. By project: Core.Tests 1397, Markup.Tests 568, Glfw.Tests 11 (10 passed, 1 skipped), Framework.Tests 1, ImGui.Tests 243 (by difference).
- `scripts/test-linux.sh`: `test-linux: PASS build, test, gpu, smoke (120 frames) in 18 s`, `exit=0`.
- SHA-256 of `EventWalk.cs`: `e84ec520c8acc5b4db4fb8180d2bd4695d40f7547506c2e94ef795b9706307d8` (unchanged).

**Group B decisions (the brief did not settle them):**
- P6-6: the section 9 flow has nine items. The old step 1 named key and char events. The new step 1 names the six events that the adapter handles, and says that the key and char events belong to keyboard-focus.
- P6-7: the section 5 sentence "The drain skips an entry with a null target" now says "with a null target after the pointer step", because a pointer or scroll entry gets its target in that step.
- P6-8: no library code sets `IsPickable` to false on a root. Section 9 states the layout-root rule as a rule for the game or for the framework layer. The routing-gate feature reads `PointerHit` for it.
- P6-9: the section 14 text uses "sixth and seventh allocation gate" for `PointerAllocationTests` and `GlfwUiInputAllocationTests`, after the "fifth" gate of the event model.
- P6-10: the ImGui.Tests count comes from the total minus the other four projects, because the runner prints no final count for each project.

**Risks:**
- The one real-window test skipped in the run of `dotnet test` on this machine (no display). Only `xvfb-run -a` runs it.
- `GlfwUiInputAllocationTests` does not measure the real GLFW handler path (see P6-5 and the Phase 4 risks).

---

## Review Fixes

The cross-phase review (`code-review.md`) ran 33 mutation probes and found 9 findings (6 MEDIUM, 3 LOW, 0 HIGH). All 9 are resolved. The verifier of the fixes gave PASS: build 0 warnings, 2224 tests, 0 failed, 2 skipped with no display, the Glfw tests 12 of 12 under `xvfb-run -a`, and `scripts/test-linux.sh` PASS.

- CR-1, CR-2: `PointerStateEdgeTests` checks `Active` after an empty drain, and `TheStaleRuleKeepsTheButtonMask` guards the kept mask. The class has 8 tests (the phase 5 demo line says 7).
- CR-3: the target handler of `Right` in `TheMoveWalksToTheHitElement` reads `Hover` (Decision 9).
- CR-4: the theory `AReleaseOverNothingClearsActive` releases at (500, 500) and at (NaN, NaN). `PointerPressTests` has 8 tests (the phase 2 demo line says 6).
- CR-5 (option B): the remarks of `DrainEvents` and `ActiveElement` state that a full ring can drop a `PointerUp`, so `Active` stays until the next press and release. Follow-up for routing-gate below.
- CR-6: `GlfwUiInputAllocationTests.TheRealHandlersAllocateNothing` measures the real GLFW handlers with a hidden window: 0 bytes, 3000 drops. It skips with no display. The class runs in a collection with `DisableParallelization`, because GLFW state is global. The BRUTAL wrappers (`GetKey`, `Glfw.Time`, `Log.Trace`) allocate nothing after warm-up.
- CR-7: `HitWalk` has one clip test. A parent sets `NextChild` to -1 when the point is outside its child clip, so a child enters only when the point is inside every clip above it. The old step 3 of `Enter` was dead code, and `Contains` repeated the clip test. `HitFrame.OwnClip` is gone (56 to 40 bytes). The first fix kept step 3 and dropped the early skip, so `review-g6-fix1.md` restored the early skip. The new SHA-256 of `HitWalk.cs` is `c61eac92e85482fbc651ed4cfd7fae275aac994c642e2911c7753a0bfb10cc63`.
- CR-8: the remarks of `DrainEvents` and `DropStalePointerState` state the rule: drain the old context after you remove an element and before you add it to another context.
- CR-9: the tests now match the demo lines. `APressBubblesFromTheHitElement` presses over `Right` outside `Inner`. `AnOverflowHiddenParentClipsItsChild` puts a sibling under the clipped half.
- Doc follow-up (`review-g8.md`): section 14 of `docs/technical-design.md` states the new test counts and the real-handler gate.

**Follow-ups for later features:**
- routing-gate (CR-5): drain the queue in each `Update`, and size the ring for a hitch. A move merge at the full limit (option A of CR-5) changes the drop-oldest rule of the event-model ring, so it needs its own decision.
- A later feature (CR-8): an owner context on the element would make the stale rule exact across contexts.
- keyboard-focus (Phase 4 facts): `GlfwWindow` raises `OnChar` with `(ushort)point`, so a code point above U+FFFF arrives cut. `GlfwBackend` also subscribes to `OnKey` and `OnChar`, and the binding has no consume rule.

---

## Gotchas & Learnings

### 1. A redundant check hides a dead cull
**Problem:** The hit walk had three clip tests for one rule: the cull of the children in the parent, a cull at the entry of a child, and a test in the element test. No mutation of one test failed a test (CR-7).
**Solution:** Keep one clip test, the early skip in the parent, and remove the others.
**Lesson:** Run a mutation probe on each guard. When no probe fails, the guard is dead or a second guard hides it. Keep the cheaper one.

### 2. A fix that follows the letter of a finding can lose the intent
**Problem:** The first CR-7 fix kept the dead step and removed the live early skip, so a clipped panel entered each child.
**Solution:** A second fix brief named the intent (the early skip of the plan) and the cost.
**Lesson:** A fix brief states the intent and the cost, not only the code change.

### 3. A test can drift from its demo line
**Problem:** Two tests proved the rule of their phase with another scene than the demo line named (CR-9).
**Solution:** The tests changed to match the demo lines, so the plan and the human test list keep their text.
**Lesson:** The verifier reads the demo test against the demo line in each phase. A difference is a fix, not a note.

---

## API Integration

- (see the section "Public API (end state)" of the plan)

---

## Testing Approach

### Unit Tests
**Files:** `tests/BrutalUi.Core.Tests/Input/*.cs`
- The hit order, the hit rules, the hover and active states, the scroll, the window callbacks, and the stale state rules.

### Adapter Tests
**Files:** `tests/BrutalUi.Glfw.Tests/*.cs`
- The conversions through the test seam, the callbacks, `Dispose`, and one real-window test that skips with no display.

### Allocation Tests
- `PointerAllocationTests` and `GlfwUiInputAllocationTests` (phase 6).

---

## Needs Human Verification

`/autorun-feature` adds one line per item that the verifier cannot check, for a person to test.

- [ ] Phase 1 demo: a unit test lays out a root with two children, adds a `:hover` rule, pushes a pointer move with no target over the second child, drains the queue, and runs `Update`. It reads `PseudoStates.Hover` on the second child and the hover fill in its `Style.BackgroundColor`. The developer runs `dotnet run --project tests/BrutalUi.Core.Tests/BrutalUi.Core.Tests.csproj -- --filter-class '*PointerHoverTests'` and reads a total of 5 with `failed: 0`.
- [ ] Phase 2 demo: a unit test pushes a pointer down with no target over `Right`, drains, and reads `PseudoStates.Active` on `Right`. A bubble handler on the root sees `Target` `Right` and `CurrentTarget` root. A pointer up clears the state. The developer runs `dotnet run --project tests/BrutalUi.Core.Tests/BrutalUi.Core.Tests.csproj -- --filter-class '*PointerPressTests'` and reads a total of 6 with `failed: 0`.
- [ ] Phase 3 demo: a unit test puts a child half outside a parent with `Overflow.Hidden`. A point in the visible half hits the child. A point in the clipped half hits the sibling under it. The developer runs `dotnet run --project tests/BrutalUi.Core.Tests/BrutalUi.Core.Tests.csproj -- --filter-class '*HitTestRuleTests'` and reads a total of 11 with `failed: 0`.
- [ ] Phase 4 demo: a unit test in `BrutalUi.Glfw.Tests` creates a `GlfwUiInput` with the test seam (no window), calls its internal `PushCursorMove` and `PushButton` over `Right` of the pointer tree, drains, and reads `Hover` and `Active` on `Right`. The developer runs `dotnet run --project tests/BrutalUi.Glfw.Tests/BrutalUi.Glfw.Tests.csproj -- --filter-class '*GlfwUiInputTests'` and reads a total of 10 with `failed: 0`.
- [ ] Phase 5 demo: a unit test hovers `Right`, removes it from the root, and drains an empty queue. It reads no `Hover` on `Right` and a null `PointerHit`. The developer runs `dotnet run --project tests/BrutalUi.Core.Tests/BrutalUi.Core.Tests.csproj -- --filter-class '*PointerStateEdgeTests'` and reads a total of 7 with `failed: 0`.
- [ ] Phase 6 demo: the developer runs `dotnet run --project tests/BrutalUi.Core.Tests/BrutalUi.Core.Tests.csproj -- --filter-class '*PointerAllocationTests'` and reads a total of 3 with `failed: 0`. A planted allocation at the start of `HitWalk.Find` makes `dotnet test --solution brutal-ui.sln` exit non-zero.
- Note: the cross-phase review added tests. `PointerPressTests` now gives a total of 8 (the Phase 2 line says 6), and `PointerStateEdgeTests` gives 8 (the Phase 5 line says 7). Expect `failed: 0` in each run.
- [ ] Real-window tests (pointer-routing): on a machine with a display, or under `xvfb-run -a`, run `dotnet run --project tests/BrutalUi.Glfw.Tests/BrutalUi.Glfw.Tests.csproj`. Expect a total of 12 with `failed: 0` and no skip. `ARealWindowSubscribesAndUnsubscribes` and `TheRealHandlersAllocateNothing` skip with no display.
- [ ] Windows run (pointer-routing, Q8 of the plan): on Windows, after `git pull --ff-only`, run `.\scripts\test-windows.ps1`. Expect the PASS line and `$LASTEXITCODE` 0. Then run the Glfw test project, and expect no skip.

---

## Next Steps

### Immediate (Today/This Session)
1. A person runs the items of "Needs Human Verification".
2. `/autorun-epic` commits the feature and merges it into `main`.

### Short Term (This Week)
1. `input-routing/keyboard-focus` adds the click focus in the `PointerDown` branch of `RoutePointer` (or `PressButton`), and the `OnKey` and `OnChar` subscriptions in `GlfwUiInput` with the internal `Push*` pattern.
2. `input-routing/routing-gate` reads `PointerHit`, calls `DrainEvents` from `Update`, and drains each frame (CR-5 follow-up).

---

## Open Questions

- (none open. See Decision R1.)

---

## Session Notes

### 2026-10-22 11:03
- `/autorun-feature` started the feature in the called mode of `/autorun-epic`.
- The run created `tasks.md` (44 tasks over 6 phases), `context.md`, and `inbox.md`.

### 2026-10-22 11:20 Phase 1
- Group A wrote `HitWalk.cs` and the pointer step of `DrainEvents`. Group B wrote `PointerTree`, `HitTestTests` (6), and `PointerHoverTests` (5).
- The phase 1 verifier gave PASS and Demo: PASS (`phase-1-verify.md`). Build 0 warnings. Solution 2174 tests, 0 failed. The 42 event-model tests pass with no edit. `EventWalk.cs` keeps its hash.
- The inbox holds no line.

### 2026-10-22 11:30 Phase 2
- The group added `Active` with the button mask (`PressButton`, `ReleaseButton`), the scroll routing, `PointerPressTests` (6), and `ScrollRoutingTests` (4).
- The phase 2 verifier gave PASS and Demo: PASS (`phase-2-verify.md`). Build 0 warnings. Solution 2184 tests, 0 failed. `EventWalk.cs` keeps its hash.
- Verifier note: `APressBubblesFromTheHitElement` presses over `Inner`, so the root handler sees `Target` `Inner`, not `Right` as the demo line says. The behavior is the same.
- Open risk for the review: a release outside the window can leave the button mask above 0, so `Active` stays until the next release.
- The inbox holds no line.

### 2026-10-22 11:38 Phase 3
- The group made `PaintWalk.PaddingBounds` and `PaintWalk.Intersect` internal, added the clip, hidden, and pickable rules to `HitWalk`, and wrote `HitTestRuleTests` (11).
- The phase 3 verifier gave PASS and Demo: PASS (`phase-3-verify.md`). Build 0 warnings. Solution 2195 tests, 0 failed. `*PaintWalk*` 29 and `ThousandElementAllocationTests` 12 pass. The `PaintWalk.cs` diff holds two modifiers and two remark lines.
- Verifier note: in `AnOverflowHiddenParentClipsItsChild`, the clipped point hits the root under the parent, not a sibling as the demo line says. The child is three quarters outside the parent. The clip rule is the same.
- Open risk for the review: an element that holds `Hover` or `Active` and then turns hidden, transparent, or unpickable keeps the state until the next pointer event.
- The inbox holds no line.

### 2026-10-22 11:50 Phase 4
- Group A added the window callbacks (`UiWindowEvents.cs`, the two slots and the two Raise methods) and `WindowCallbackTests` (4). Group B added the `BrutalUi.Glfw` package, `GlfwUiInput`, the test project, and `GlfwUiInputTests` (10). See "Phase 4 Results" for the API table, the native dependency note, and P4-1 to P4-12.
- The phase 4 verifier gave PASS and Demo: PASS (`phase-4-verify.md`). Build 0 warnings. Solution 2209 tests, 0 failed, 1 skipped. `ARealWindowSubscribesAndUnsubscribes` skips with no display and passes under `xvfb-run -a` (10 of 10). The package lists exactly two dependencies.
- Safe order next to `GlfwBackend`: `GlfwWindow` installs its native callbacks one time, in its constructor. The events are managed multicast events, so no subscriber replaces another. Create the window, then `GlfwBackend`, then `GlfwUiInput`. Dispose in the opposite order.
- Open risk for phase 6 and the review: the real GLFW handler path calls up to 8 `GetKey` and 1 `Glfw.Time` for each move or scroll, and each BRUTAL P/Invoke wrapper calls `Log.Trace`. The adapter gate of the plan measures the `Push*` methods only.
- Open facts for keyboard-focus: `GlfwWindow` raises `OnChar` with `(ushort)point`, so a code point above U+FFFF arrives cut. `GlfwBackend` also subscribes to `OnKey` and `OnChar`, and the binding has no consume rule.
- The inbox holds no line.

### 2026-10-22 11:55 Phase 5
- The group added `DropStalePointerState` at the start of `DrainEvents`, the remarks of the stale rule, and `PointerStateEdgeTests` (7).
- The phase 5 verifier gave PASS and Demo: PASS (`phase-5-verify.md`). Build 0 warnings. Solution 2216 tests, 0 failed, 1 skipped. `EventWalk.cs` keeps its hash.
- Open fact for the review: an element that moves to another root of the same context keeps its state, because it is still in the roots.
- The inbox holds no line.

### 2026-10-22 12:00 Phase 6
- Group A wrote `PointerAllocationTests` (3) and `GlfwUiInputAllocationTests` (1), and proved the plant. `src/` needed no fix. Group B wrote sections 3, 4, 5, 9, and 14 of the technical design, the README row, and the changelog line.
- The phase 6 verifier gave PASS and Demo: PASS (`phase-6-verify.md`). Build 0 warnings. Solution 2220 tests, 0 failed, 1 skipped. Five runs of `PointerAllocationTests` give 3 of 3. `scripts/test-linux.sh` gives PASS and exit 0. The lint gives 1.51, 0.51, 1.68, and 1.62.
- All 44 tasks are done. Next: the cross-phase review.
- The inbox holds no line.

### 2026-10-22 12:50 Cross-phase review
- The review ran 33 mutation probes in a scratch copy and wrote `code-review.md` with 9 findings (CR-1 to CR-9). Nine fix briefs (`review-g1.md` to `review-g8.md` and `review-g6-fix1.md`) resolved all 9. CR-5 and CR-8 use the remark option. Their follow-ups are in "Review Fixes".
- The verifier of the fixes gave PASS (`review-verify.md`): build 0 warnings, 2224 tests, 0 failed, 2 skipped with no display. The Glfw tests give 12 of 12 under `xvfb-run -a`. `scripts/test-linux.sh` gives PASS.
- The inbox holds no line: 0 done and 0 open. No bug to promote.
- The `solution-architect` agent wrote the draft plan of `input-routing/keyboard-focus`: 6 phases and 42 tasks, with four open scope questions (the phase 1 demo, the ImGui and UI keyboard rule, the click target, and a Tab that a handler keeps).
- Next: a person runs "Needs Human Verification". `/autorun-epic` owns the commit and the next feature.


---

*Update this file at the end of each work session. Run `/update-feature` before compacting conversations.*
