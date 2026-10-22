# input-routing/pointer-routing - Code Review

**Reviewed:** 2026-10-22
**Reviewer:** Agent (cross-phase review of phases 1 to 6)
**Rules Applied:** 1. The exact hit, 2. The draw clip, 3. The exact states, 4. The walk does not change, 5. Zero allocation after warm-up, 6. No GLFW in Core, 7. No recursion, 8. Clean seams and a small public API, 9. No leak, 10. Test quality, 11. `AGENTS.md` and the docs. The Vercel React rules do not apply to C#.
**Status:** Resolved - 9 findings, 9 resolved (the verifier of the fixes gave PASS: `review-verify.md`)

---

## Summary

| Severity | Count |
|----------|-------|
| HIGH | 0 |
| MEDIUM | 6 |
| LOW | 3 |

The feature is sound. The hit order, the clip, the hidden rule, `IsPickable`, the hover and active states, the scroll routing, the callbacks, and the adapter conversions all hold, and a test catches each main mutation. The review found no wrong hit, no allocation after warm-up, no leak, and no public API break. `EventWalk.cs` keeps the SHA-256 `e84ec520c8acc5b4db4fb8180d2bd4695d40f7547506c2e94ef795b9706307d8`. `BrutalUi.Core` has no GLFW `using` and 2 `PackageReference` items.

The findings are test gaps and edge cases. Four rules have no test: the `Active` half of the stale rule, the kept button mask, the hover state that a handler reads, and a release over no element. Two edge cases can leave a state wrong: a full ring that drops a `PointerUp`, and an element that moves to another context. The adapter gate does not measure the real GLFW handler path. The step 3 clip cull of `HitWalk` is invisible to the tests, because the element test repeats it. Two demo lines do not match their tests.

**Baseline (repository):** `dotnet build brutal-ui.sln` gives 0 Warning(s) and 0 Error(s). `dotnet test --solution brutal-ui.sln` gives total 2220, failed 0, succeeded 2219, skipped 1 (`ARealWindowSubscribesAndUnsubscribes`, no display).

**Probes.** Each probe ran in the scratch copy `/tmp/claude-1000/-home-aaronstatic-dev-brutal-ui/9cde8ab4-a0c7-4298-9a82-daf2000f39aa/scratchpad/cr-pointer-routing/repo`, with one mutation, the tests of the touched project, and a restore from `../orig/` with a `cmp` check. 33 probes ran. 28 failed one or more tests. 5 gave 0 failed: 4 are findings, and 1 is an equivalent mutant.

| # | Rule of the brief | Mutation | Failed tests |
|---|-------------------|----------|--------------|
| 1a | 1 | `HitWalk.Find`: the first root first | `HitTestTests.TheLastRootWinsWhereTwoRootsOverlap` |
| 1b | 1 | `HitWalk.Find`: the first child first | `HitTestTests.TheLaterSiblingWinsWhereTwoChildrenOverlap`, `PointerAllocationTests.TheMeasuredBodyDoesRealWork` |
| 2 | 2 | `HitWalk.Find`: the element test before the children | 36 tests (all hit, hover, press, scroll, rule, and edge classes) |
| 3 | 3 | `Contains`: `<=` max on both axes | `HitTestTests.APointInAChildHitsTheChild`, `APointInTheGapHitsTheParent`, `APointOutsideEachRootHitsNothing`, `HitTestRuleTests.ThePaddingBoxClipKeepsTheBorderForTheParent`, `TheRightAndBottomEdgesAreOutside` |
| 4a | 4 | `Enter`: no parent-clip skip (step 3 removed) | 0 failed (finding CR-7) |
| 4b | 4 | `Enter`: the border box as the child clip | `HitTestRuleTests.ThePaddingBoxClipKeepsTheBorderForTheParent` |
| 4c | 4 | `Enter`: a clip for `Overflow.Visible` too | `HitTestRuleTests.AVisibleOverflowLetsAChildOutsideTheParentBeHit` |
| 5a | 5 | `Enter`: no `Display.None` skip | `HitTestRuleTests.ADisplayNoneSubtreeIsNotHit` |
| 5b | 5 | `Enter`: no opacity skip | `HitTestRuleTests.AZeroOpacitySubtreeIsNotHit` |
| 5c | 5 | `Enter`: NaN opacity counts as 0 | `HitTestRuleTests.AZeroOpacitySubtreeIsNotHit` |
| 6a | 6 | `Enter`: `IsPickable` false skips the subtree | `HitTestRuleTests.AnUnpickableParentKeepsItsChildrenPickable` |
| 6b | 6 | `Find`: `IsPickable` not read | `HitTestRuleTests.AnUnpickableParentKeepsItsChildrenPickable`, `AnUnpickableLeafPassesTheHitToItsParent` |
| 7 | 7 | `Find`: no `Array.Clear` in the `finally` | `HitTestTests.ATreeDeeperThanTheStackHitsTheLeaf` |
| 8 | 8 | `DrainEvents`: a caller target also runs `RoutePointer` (the target stays) | `PointerStateEdgeTests.ACallerTargetChangesNoState`, `ScrollRoutingTests.AScrollWithACallerTargetKeepsTheTarget` |
| 9 | 9 | `DrainEvents`: the walk first, then `RoutePointer` (all states after the walk) | `PointerPressTests.APressBubblesFromTheHitElement` |
| 9b | 9 (extra) | `DrainEvents`: only `SetHover` after the walk | 0 failed (finding CR-3) |
| 10a | 10 | `ReleaseButton`: each release clears `Active` | `PointerPressTests.ASecondButtonKeepsActiveUntilTheLastRelease` |
| 10b | 10 | `PressButton`: a second press moves `Active` | `PointerPressTests.ASecondButtonKeepsActiveUntilTheLastRelease` |
| 10c | 10 | `ReleaseButton`: no clear of the mask bit | `PointerPressTests.APressSetsActiveAndTheReleaseClearsIt`, `AReleaseOverAnotherElementClearsTheFirst`, `ASecondButtonKeepsActiveUntilTheLastRelease`, `AnActiveRuleShowsAfterUpdate`, `PointerAllocationTests.TheMeasuredBodyDoesRealWork` |
| 11a | 11 | `DrainEvents`: `Scroll` not in the type pattern | `ScrollRoutingTests.AScrollGoesToTheElementUnderThePointer`, `AScrollMovesHover`, `AScrollOverNothingWalksNothing`, `PointerAllocationTests.TheMeasuredBodyDoesRealWork` |
| 11b | 11 | `RoutePointer`: a scroll reads `Payload.Pointer.Position` | 0 failed (equivalent mutant, no finding: see "What's Already Good") |
| 12a | 12 | `DrainEvents`: no `DropStalePointerState` | `PointerStateEdgeTests.AHoveredElementThatLeavesTheTreeLosesHover`, `ARemovedRootLosesHover` |
| 12b | 12 | `DropStalePointerState`: the stale check clears the mask too | 0 failed (finding CR-2) |
| 12c | 12 (extra) | `DropStalePointerState`: no `Active` branch | 0 failed (finding CR-1) |
| 13a | 13 | `RaiseFileDrop`: queues a `PointerMove` | `WindowCallbackTests.ADropWithNoCallbackDoesNothing`, `ACallbackQueuesNoEventAndChangesNoState` |
| 13b | 13 | `RaiseContentScale`: sets `Hover` on the first root | `WindowCallbackTests.ACallbackQueuesNoEventAndChangesNoState` |
| 14a | 14 | `GlfwUiInput.PushScroll`: no `Dispose` guard | `GlfwUiInputTests.DisposeStopsThePushes` |
| 14b | 14 | `ButtonModifiers`: no `0x3F` mask | `GlfwUiInputTests.APressAndAReleaseGiveDownAndUp` |
| 14c | 14 | `PushScroll`: the position (0, 0) | `GlfwUiInputTests.AScrollUsesTheLastCursorPosition`, `ACursorLeaveClearsHover` |
| 14d | 14 | `LeavePosition`: (0, 0) in place of (NaN, NaN) | `GlfwUiInputTests.ACursorLeaveClearsHover` |
| 14e | 14 | `PushButton`: `(PointerButton)(button + 1)` | `GlfwUiInputTests.APressAndAReleaseGiveDownAndUp`, `EachButtonCastsWithNoTable` |
| 15a | 15 | `RoutePointer`: `GC.KeepAlive(new object())` | `PointerAllocationTests.PointerRoutingAllocatesNothingAfterWarmUp` |
| 15b | 15 | `PushCursorMove`: `GC.KeepAlive(new object())` | `GlfwUiInputAllocationTests.TheAdapterHandlersAllocateNothing` |

After the last probe, `diff -rq` of the `src` and `tests` folders of the copy and of the repository gives no output.

---

## Findings

### CR-1: The stale rule for `Active` has no test
**Severity:** MEDIUM
**Rule:** 3. The exact states, and 10. Test quality
**File:** `tests/BrutalUi.Core.Tests/Input/PointerStateEdgeTests.cs:68-97`
**Problem:** Probe 12c removes the `Active` branch of `DropStalePointerState`, and 0 tests fail. `AnActiveElementThatLeavesTheTreeLosesActive` removes `Right` and then drains a `PointerUp`. The release clears the mask, so `ReleaseButton` clears `Active` on the removed element in the same drain. The test passes with no stale rule for `Active`. F8 of the plan ("after a drain, an element that left the tree holds no `Active`") has no guard. With the mutant, a removed element keeps `Active`, and `_activeElement` keeps a reference to it until the last release.
```csharp
Assert.True(tree.Root.Children.Remove(tree.Right));
// No drain before the release: the release hides the stale rule.
tree.Context.PushEvent(PointerTree.Up(OverLeft, PointerButton.Left));
Assert.Equal(1, tree.Context.DrainEvents());
```
**Fix:** Drain an empty queue after the removal, and check the state before the release.
```csharp
Assert.True(tree.Root.Children.Remove(tree.Right));
Assert.Equal(0, tree.Context.DrainEvents());
Assert.Equal(PseudoStates.None, tree.Right.States);
Assert.Null(tree.Context.ActiveElement);
```
**Effort:** Low

### CR-2: The kept button mask of the stale rule has no test
**Severity:** MEDIUM
**Rule:** 3. The exact states, and 10. Test quality
**File:** `tests/BrutalUi.Core.Tests/Input/PointerStateEdgeTests.cs:92-96`
**Problem:** Probe 12b adds `_pressedButtons = 0;` to the `Active` branch of `DropStalePointerState`, and 0 tests fail. Decision 10 keeps the mask, so `Active` stays on a new element while the first button is still down. The last block of `AnActiveElementThatLeavesTheTreeLosesActive` passes with both rules, because the release of the only button clears its bit in each case.
**Fix:** Add a test with two buttons. Press Left over `Right`, remove `Right`, and drain an empty queue. Press the right button over `Left`. Release the right button. `Left` must keep `Active`, because the left button is still down. Release the left button. `Left` loses `Active`.
```csharp
tree.Context.PushEvent(PointerTree.Up(OverLeft, PointerButton.Right));
Assert.Equal(1, tree.Context.DrainEvents());
Assert.Same(tree.Left, tree.Context.ActiveElement); // the left bit stays in the mask
```
**Effort:** Low

### CR-3: No handler reads `Hover`, so Decision 9 has no test for the hover state
**Severity:** MEDIUM
**Rule:** 3. The exact states, and 10. Test quality
**File:** `tests/BrutalUi.Core.Tests/Input/PointerHoverTests.cs:105`
**Problem:** Probe 9b moves only `SetHover` to after the walk, and 0 tests fail. The only test that reads a state in a handler is `APressBubblesFromTheHitElement`, and it reads `Active`. The remarks of `DrainEvents` and `RoutePointer` promise that "a handler reads the new state". A regression would show the old hover owner to each handler of the move.
**Fix:** In `TheMoveWalksToTheHitElement`, hover `Left` first. Then move to `Right`, and read the states in the target handler of `Right`.
```csharp
PseudoStates rightInHandler = PseudoStates.None;
PseudoStates leftInHandler = PseudoStates.None;
tree.Right.OnPointerMove = (ref PointerEvent e) =>
{
    rightInHandler = tree.Right.States;
    leftInHandler = tree.Left.States;
};
// ... push the move over Right and drain ...
Assert.Equal(PseudoStates.Hover, rightInHandler);
Assert.Equal(PseudoStates.None, leftInHandler);
```
**Effort:** Low

### CR-4: No test releases a button over no element
**Severity:** MEDIUM
**Rule:** 3. The exact states, and 10. Test quality
**File:** `tests/BrutalUi.Core.Tests/Input/PointerPressTests.cs:108-139`
**Problem:** Each `PointerUp` in the tests lands on an element (`OverRight` or `OverLeft`). The release outside the window (open risk 1) arrives at a position outside each root, or at (NaN, NaN) after a cursor leave. `RoutePointer` runs `ReleaseButton` for a null hit, so the code is correct today. But no test guards it. A change that returns early for a null hit (for example, a "no hit, no work" shortcut) leaves `Active` stuck after each drag that ends outside the window.
**Fix:** Add a test: press over `Right`, then release at `(500f, 500f)` and in a second case at `(float.NaN, float.NaN)`. Expect a walk count of 0, a null `ActiveElement`, and no `Active` bit on `Right`.
```csharp
tree.Context.PushEvent(PointerTree.Up(new float2(float.NaN, float.NaN), PointerButton.Left));
Assert.Equal(0, tree.Context.DrainEvents());
Assert.Null(tree.Context.ActiveElement);
Assert.Equal(PseudoStates.None, tree.Right.States & PseudoStates.Active);
```
**Effort:** Low

### CR-5: A full ring can drop a `PointerUp` and leave `Active` stuck
**Severity:** MEDIUM
**Rule:** 3. The exact states
**File:** `src/BrutalUi.Core/Tree/UiContext.cs:1099-1130` (with `Enqueue` at 1169-1190)
**Problem:** The ring drops its oldest entry when it is full (256 entries by default). The dropped entry can be a `PointerUp`. Then the bit stays in `_pressedButtons`, and `Active` stays on `_activeElement` until the next press and release of the same button. A load hitch of 300 ms with a 1000 Hz mouse gives about 300 moves, so the case is real after routing-gate drains once per frame. No test covers it. `DroppedEventCount` shows the drop, but no code acts on it.
**Fix:** Keep the button events when the ring is full. Option A (preferred): in `Enqueue`, when the ring is full and the new entry and the newest queued entry are both `PointerMove` with no target, overwrite the newest entry (a move merge at the limit only). This needs one internal peek method on `EventQueue`. `GlfwUiInputAllocationTests` then counts fewer drops for the moves, so update its expected drop count. Option B (minimum): state the limit in the remarks of `DrainEvents`, and give routing-gate the follow-up. Add a test with a capacity of 4: press, release, and 4 moves before the drain.
**Effort:** Medium

### CR-6: The real GLFW handler path has no allocation gate
**Severity:** MEDIUM
**Rule:** 5. Zero allocation after warm-up, and 10. Test quality
**File:** `tests/BrutalUi.Glfw.Tests/GlfwUiInputAllocationTests.cs:1-99`
**Problem:** The Quality Bar says "The adapter handlers allocate 0 bytes". The gate calls the `Push*` methods of the test seam only. The handlers `HandleCursorPos`, `HandleScroll`, and `HandleMouseButton` call `GlfwWindow.GetKey` up to 8 times and `Glfw.Time` one time for each event. Each BRUTAL P/Invoke wrapper calls `Log.Trace` (Phase 4 Results). If a wrapper builds a `string` for `Log.Trace`, each event allocates, also when the trace level is off. The review did not read the wrapper code. The code of this repository on that path is free of allocation (static methods, enum compares, and struct returns), but the gate cannot see a cost inside the wrappers. This is open risk 4.
**Fix:** Add `TheRealHandlersAllocateNothing`. Use the pattern of `ARealWindowSubscribesAndUnsubscribes`: a hidden window with `GLFW_NO_API`, a skip with the reason when `Glfw.Init` or the window fails, and the backing delegates of `OnCursorPos`, `OnScroll`, and `OnMouseButton` read by reflection before the measure. Warm up, then call each delegate 1000 times in the measured body, and expect 0 bytes. `xvfb-run -a` and `scripts/test-windows.ps1` then run the gate. If the wrappers allocate, record the fact for BRUTAL, and read the modifiers one time for each frame in place of each event.
**Effort:** Medium

### CR-7: The element test repeats the parent-clip test, so the step 3 cull is invisible
**Severity:** LOW
**Rule:** 2. The draw clip, and 10. Test quality
**File:** `src/BrutalUi.Core/Input/HitWalk.cs:213-217` and `268-283`
**Problem:** Probe 4a removes step 3 of `Enter` (the point outside the parent clip skips the subtree), and 0 tests fail. `Contains` tests `top.OwnClip` again, and `OwnClip` is the same `parentClip` that step 3 tests. Each child clip is inside the parent clip, so the two checks are always equal. The cull is the plan mitigation for "many moves cost many hit tests", but no test can see it. `HitFrame` also carries 16 bytes for `OwnClip` only for this second check.
```csharp
// Step 3 in Enter:
if (!InBounds(in parentClip, point)) { return; }
// Contains, for the same frame later:
if (!InBounds(in ownClip, point)) { return false; }
```
**Fix:** Keep the cull in step 3 and remove the clip test from `Contains`. Remove `HitFrame.OwnClip` too (the frame goes from 56 to 40 bytes). Then probe 4a fails `AnOverflowHiddenParentClipsItsChild`.
```csharp
// Enter tested the clip of the parent (step 3), so only the border box stays.
private static bool Contains(Element element, float2 origin, float2 point)
{
    float4 r = element.Layout.Rect;
    return point.X >= origin.X && point.X < origin.X + r.Z && point.Y >= origin.Y && point.Y < origin.Y + r.W;
}
```
**Effort:** Low

### CR-8: The stale check of one context clears a state that another context set
**Severity:** LOW
**Rule:** 3. The exact states
**File:** `src/BrutalUi.Core/Tree/UiContext.cs:1013-1025`
**Problem:** The states are bits on the element, and each context keeps its own `_pointerHit` and `_activeElement`. Context A hovers `E`. The game moves `E` to a root of context B (for example, a panel that goes from the screen UI to a world-space quad). B drains and sets `Hover` on `E`. Then A drains. `E` is not in the roots of A, so `SetHover(null)` clears the `Hover` bit that B set. `E` shows no hover until the pointer of B leaves `E` and comes back. The same holds for `Active`. If A drains first, the clear is correct. The result depends on the order of the drains.
**Fix:** State the rule in the remarks of `DropStalePointerState` and of `DrainEvents`: "Drain the old context after you remove an element and before you add it to another context." A full fix needs the owner context on the element, which is out of scope. Add a follow-up line in `context.md`.
**Effort:** Low

### CR-9: Two demo lines do not match their tests
**Severity:** LOW
**Rule:** 10. Test quality, and 11. The docs
**File:** `docs/features/input-routing/human-testing.md:21-22` (the same lines are in `implementation.md:274` and `:299`, and in `context.md:328-329`)
**Problem:** The phase 2 line says that the root handler sees `Target` `Right`. `APressBubblesFromTheHitElement` presses over `Inner`, so the root sees `Target` `Inner`. The phase 3 line says that the clipped point hits "the sibling under it". `AnOverflowHiddenParentClipsItsChild` has no sibling there, and the point hits the root. The behavior is correct in both tests. A person who reads the demo line and the test output finds a different element name. This is open risk 5.
**Fix:** Change the two demo lines to match the tests: "A bubble handler on the root sees `Target` `Inner` (the hit element under `Right`) and `CurrentTarget` root." and "A point in the clipped half hits the element under the parent (the root)." Change all three copies.
**Effort:** Low

---

## What's Already Good

- **The exact hit (rule 1).** `HitWalk.Find` walks the last root first and the last child first, and it tests an element after its descendants. Probes 1a, 1b, and 2 fail the order tests. The walk copies `Origin`, `Opacity`, and `ChildClip` into locals before `Enter`, so a stack growth never leaves a stale `ref`.
- **The draw clip (rule 2).** `HitWalk` calls `PaintWalk.PaddingBounds` and `PaintWalk.Intersect`, and it copies steps 1 and 2 of the hidden rule with the same clamp. Probes 4b, 4c, 5a, 5b, and 5c each fail a rule test. `PaintWalk.cs` changed two modifiers and two remark lines only.
- **The walk does not change (rule 4).** The SHA-256 of `EventWalk.cs` equals the value of Decision R3. The drain changes only its target step and adds the stale call at its start.
- **No GLFW in Core (rule 6).** `grep -rn 'using .*Glfw' src/BrutalUi.Core` gives no output. `BrutalUi.Core.csproj` has 2 `PackageReference` items and one new `InternalsVisibleTo`. Only `BrutalUi.Glfw.csproj` references `Ahwoo.BRUTAL.Glfw`. The native dependency note is in `context.md` (Phase 4 Results).
- **No recursion and no leak (rules 7 and 9).** No method of `HitWalk` calls itself. The `finally` block clears the used frames after a hit, and each pop clears its frame. Probe 7 fails `ATreeDeeperThanTheStackHitsTheLeaf`. The stale rule drops the references to a removed element at the next drain.
- **Zero allocation (rule 5).** The plants in `RoutePointer` (probe 15a) and in `PushCursorMove` (probe 15b) each fail a gate. Phase 6 proved the plant in `HitWalk.Find`. The adapter creates its six delegates one time. `RoutePointer`, `PressButton`, `ReleaseButton`, and `SetHover` use no closure, no boxing, and no string.
- **The public API (rule 8).** The public surface equals the section "Public API (end state)". `HitWalk`, `HitFrame`, the pointer members of `UiContext`, the seam constructor, and the `Push*` methods are internal. The extra internal `PushFileDrop`, `PushContentScale`, and `ButtonModifiers` follow P4-5. The keyboard-focus seam (the `PointerDown` branch of `RoutePointer`) and the routing-gate seams (`PointerHit`, `DrainEvents`) need no change of the walk.
- **The adapter conversions.** Probes 14a to 14e each fail an adapter test. `RequireContext` gives `ArgumentNullException` with `ParamName` `info`. `Dispose` is idempotent.
- **`AGENTS.md` (rule 11).** .NET 10, nullable on, `AllowUnsafeBlocks` on, file-scoped namespaces, a `required` create-info, and XML docs on each public member. The build gives 0 warnings with CS1591 as an error. Sections 3, 4, 5, 9, and 14 of the technical design, the README row, and the changelog line name the feature.
- **Probe 11b is an equivalent mutant.** `PointerPayload` and `ScrollPayload` both sit at `FieldOffset(0)` of `UiEventPayload`, and `Position` is the first field of each. A scroll that reads `Payload.Pointer.Position` gets the same value, so no test can catch the mutant. The type branch in `RoutePointer` stays correct, and it protects against a future change of the payload layout. No finding.
- **Open risk 1 (a release outside the window), the GLFW part: not a finding.** GLFW delivers the release. Windows captures the mouse during a press, X11 has an implicit grab, and Cocoa sends `mouseUp` to the window of the `mouseDown`. When a window loses the focus, GLFW makes a synthetic release for each pressed button (the GLFW input guide, `glfwSetWindowFocusCallback`). The adapter pushes each release, also at a NaN position, and `RoutePointer` runs `ReleaseButton` for a null hit. The residual cases are CR-4 (no test) and CR-5 (a ring drop).
- **Open risk 2 (a hidden, transparent, or unpickable element keeps its state): not a finding.** The remarks of `DrainEvents` and `PointerHit` state the rule. The plan accepts it in its risk table (likelihood High, impact Low). The draw does not show a hidden or transparent element, so its hover look does not show. The next pointer event moves `Hover`, the last release clears `Active`, and a press re-tests the position before the game reads `PointerHit`. routing-gate can add a test of the last position at each `Update`.
- **Open risk 3 (an element moves to another root of the same context): not a finding.** The element stays in the tree and on the screen. This case equals a layout move under a still pointer: the next pointer event moves `Hover`. `Active` belongs to the press until the last release (Decision 8), also after a move. The move to another context is a different case, and CR-8 records it.
- **Open risk 6 (no grid slot overflows the grid): not a finding.** A clipped slot takes the `NextChild = -1` branch or the return of step 3. Both branches hold only struct copies and compares, so no allocation path can hide there. Each point of the gate that is outside a clip runs one of these branches. The clip logic has its own guard in `HitTestRuleTests` (probes 4b and 4c).
- **Open risks 4 and 5** are findings CR-6 and CR-9.

## Recommended Fix Order

| Order | Finding | Severity | Effort | Fix group (one file) | Impact |
|-------|---------|----------|--------|----------------------|--------|
| 1 | CR-1 | MEDIUM | Low | A: `PointerStateEdgeTests.cs` | Guards F8 for `Active`. |
| 2 | CR-2 | MEDIUM | Low | A: `PointerStateEdgeTests.cs` | Guards the kept mask of Decision 10. |
| 3 | CR-4 | MEDIUM | Low | B: `PointerPressTests.cs` | Guards the release path of open risk 1. |
| 4 | CR-3 | MEDIUM | Low | C: `PointerHoverTests.cs` | Guards Decision 9 for `Hover`. |
| 5 | CR-7 | LOW | Low | D: `HitWalk.cs` | Makes the clip cull testable, and makes the frame 16 bytes smaller. |
| 6 | CR-8 | LOW | Low | E: `UiContext.cs` | States the drain order for a move between contexts. |
| 7 | CR-9 | LOW | Low | F: the three demo docs | The demo lines match the tests. |
| 8 | CR-5 | MEDIUM | Medium | E: `UiContext.cs` | Removes a stuck `Active` after a ring overflow. |
| 9 | CR-6 | MEDIUM | Medium | G: `GlfwUiInputAllocationTests.cs` | Measures the real handler path on a machine with a display. |

## Progress

- [x] CR-1: The stale rule for `Active` has no test (fixed in `review-g1.md`: the test drains an empty queue after the removal and checks `Active` before the release. Probe 12c now fails 2 tests.)
- [x] CR-2: The kept button mask of the stale rule has no test (fixed in `review-g1.md`: new test `TheStaleRuleKeepsTheButtonMask`. Probe 12b now fails it.)
- [x] CR-3: No handler reads `Hover`, so Decision 9 has no test for the hover state (fixed in `review-g2.md`: the target handler of `Right` reads `Hover`. Probe 9b now fails it.)
- [x] CR-4: No test releases a button over no element (fixed in `review-g3.md`: theory `AReleaseOverNothingClearsActive` with (500, 500) and (NaN, NaN).)
- [x] CR-5: A full ring can drop a `PointerUp` and leave `Active` stuck (option B, `review-g4.md`: the remarks of `DrainEvents` and `ActiveElement` state the limit. Option A is out of scope: a move merge changes the drop-oldest rule of the event-model ring and its tests. routing-gate owns the follow-up: it drains each frame and owns the queue size.)
- [x] CR-6: The real GLFW handler path has no allocation gate (fixed in `review-g5.md`: `TheRealHandlersAllocateNothing` measures 0 bytes under `xvfb-run -a`, and it skips with no display. The BRUTAL wrappers allocate nothing after warm-up.)
- [x] CR-7: The element test repeats the parent-clip test, so the step 3 cull is invisible (fixed in `review-g6.md` and `review-g6-fix1.md`: step 3 of `Enter` was dead code, because a child enters only when the point is in the child clip of its parent. The fix removes step 3, `HitFrame.OwnClip`, and the clip test of `Contains`, and keeps the early skip of step 5. The no-cull probe now fails 5 clip tests.)
- [x] CR-8: The stale check of one context clears a state that another context set (option of the finding, `review-g4.md`: the remarks of `DrainEvents` and `DropStalePointerState` state the rule. A full fix needs the owner context on the element, which is out of scope. Follow-up in `context.md`.)
- [x] CR-9: Two demo lines do not match their tests (fixed in `review-g7.md`: the tests now match the demo lines, so the plan and `human-testing.md` keep their text. `APressBubblesFromTheHitElement` presses over `Right` outside `Inner`. `AnOverflowHiddenParentClipsItsChild` puts a sibling under the clipped half.)
