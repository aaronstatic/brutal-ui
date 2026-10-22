# input-routing/event-model - Code Review

**Reviewed:** 2026-10-22
**Reviewer:** Agent (cross-phase review of phases 1 to 5)
**Rules Applied:** 1. The exact order, 2. The exact stop, 3. Zero allocation after warm-up, 4. No recursion, 5. Clean seams and a small public API, 6. No GLFW, 7. No regression and no leak, 8. Test quality, 9. `AGENTS.md` and the docs. The Vercel React rules do not apply to C#.
**Status:** Resolved - 7 findings, 7 resolved

---

## Summary

| Severity | Count | Findings |
|----------|-------|----------|
| HIGH | 0 | - |
| MEDIUM | 3 | CR-1, CR-2, CR-3 |
| LOW | 4 | CR-4, CR-5, CR-6, CR-7 |
| **Total** | **7** | |

The event model is in good shape. The walk order, the stop rule, the ring, and the 0-byte gate are correct. The tests catch each mutation of the brief, except one equivalent mutant (probe 6a). The review found no wrong order, no missed stop, no allocation after warm-up, no leak, and no break of the public API.

The review found one defect. When a handler pushes into a full ring during a drain, the drain walks an event that the handler pushed (CR-1). This breaks Decision 6. Two rules of the walk have no test: the walk reads the type one time (P1-9, CR-2), and the walk reads the target bubble slot after the target capture call (P1-10, CR-3). The other findings are small gaps in the gate, the tests, and the docs.

**The baseline (R.3):** `dotnet build brutal-ui.sln` gives 0 Warning(s) and 0 Error(s). `dotnet test --solution brutal-ui.sln` gives total 2159, failed 0, succeeded 2159.

**The probes (R.4).** Each probe ran in the scratch copy `/tmp/claude-1000/-home-aaronstatic-dev-brutal-ui/9cde8ab4-a0c7-4298-9a82-daf2000f39aa/scratchpad/cr-event-model/repo`. The originals are in `../orig/`. Each run is `dotnet test --solution brutal-ui.sln`, with total 2159. After each run, `cp -p` restored the file, and `cmp` reported no difference. The line numbers refer to the files before the mutation.

1. Capture order: the capture loop runs from `last - 1` down to 0 (`EventWalk.cs:114`). 15 failed: `EventWalkTests.PointerDownWalksCaptureTargetBubble`, `EventWalkTests.AStopInCaptureEndsTheWalk`, `EventWalkTests.AStopInTheTargetCaptureSlotSkipsTheTargetBubbleSlot`, `EventWalkTests.AStopAppliesToOneEventOnly`, `EventKindTests.EachKindReachesOnlyItsOwnSlots` (7 cases), `EventKindTests.AKeyDownWalksCaptureTargetBubble`, `EventWalkEdgeTests.ATreeChangeDuringTheWalkKeepsThePath`, `EventWalkEdgeTests.ATreeDeeperThanThePathBufferWalksInOrder`, `EventAllocationTests.TheMeasuredBodyDoesRealWork`.
2. Target order: the target phase calls the bubble slot first, then the capture slot (`EventWalk.cs:143` and `:155`). 16 failed: `EventWalkTests.PointerDownWalksCaptureTargetBubble`, `ARootTargetRunsTheTargetPhaseOnly`, `AnElementWithNoHandlerHasNoSideObject`, `EachPointerTypeReachesItsOwnSlots`, `AStopInTheTargetCaptureSlotSkipsTheTargetBubbleSlot`, `AStopInBubbleSkipsTheAncestors`, `AStopInAMulticastSlotSkipsTheNextDelegate`, `EventKindTests.EachKindReachesOnlyItsOwnSlots` (7 cases), `EventKindTests.AKeyDownWalksCaptureTargetBubble`, `EventWalkEdgeTests.ATreeDeeperThanThePathBufferWalksInOrder`.
3. Bubble order: the bubble loop runs from 0 up to `last - 1` (`EventWalk.cs:169`). 12 failed: `EventWalkTests.PointerDownWalksCaptureTargetBubble`, `EventWalkTests.AStopInBubbleSkipsTheAncestors`, `EventKindTests.EachKindReachesOnlyItsOwnSlots` (7 cases), `EventKindTests.AKeyDownWalksCaptureTargetBubble`, `EventWalkEdgeTests.ATreeChangeDuringTheWalkKeepsThePath`, `EventWalkEdgeTests.ATreeDeeperThanThePathBufferWalksInOrder`.
4. The stop check after each delegate: `InvokeSlot` calls `handler(ref e)` one time and checks the stop after the whole slot (`EventWalk.cs:209-218`). 1 failed: `EventWalkTests.AStopInAMulticastSlotSkipsTheNextDelegate`.
5. The stop between the two target slots: no `return` after a stop in the target capture slot (`EventWalk.cs:148-151`). 1 failed: `EventWalkTests.AStopInTheTargetCaptureSlotSkipsTheTargetBubbleSlot`.
6. The reset of the stop flag at the push.
   - 6a. Delete `entry.Header.ResetWalkState();` (`UiContext.cs:812`). 0 failures. This is an equivalent mutant, not a finding. The entry header comes from the constructor at line 809, which gives phase `None`, no current target, and no stop. The stop flag has a private setter, so no other path can copy it into the entry. No test can catch the deletion.
   - 6b. Copy the header of the caller (`entry.Header = header;`) and delete the reset. 1 failed: `EventWalkTests.AStopAppliesToOneEventOnly`.
7. Drop the newest entry: a push into a full ring counts the drop and writes nothing (`EventQueue.cs:156-157`). 2 failed: `EventQueueTests.AFullQueueDropsTheOldestEvent`, `EventAllocationTests.TheMeasuredBodyDoesRealWork`.
8. The drop count: `_dropped++` becomes `_dropped += 0` (`EventQueue.cs:158`). A deletion of the line fails the build with CS0649, so the probe uses the no-op. 5 failed: `EventQueueTests.AFullQueueDropsTheOldestEvent`, `EventQueueTests.TheDropCountGrowsForEachDrop`, `EventQueueTests.TheDefaultCapacityIs256`, `EventAllocationTests.APushToAFullQueueAllocatesNothing`, `EventAllocationTests.TheMeasuredBodyDoesRealWork`.
9. The count read one time: `pending = int.MaxValue`, so the drain runs until the ring is empty (`UiContext.cs:713`). 1 failed: `EventQueueTests.AnEventFromAHandlerWaitsForTheNextDrain`.
10. The null-target skip: `target is not null && !IsInRootTree(target)` (`UiContext.cs:727`). A null target goes to the walk and counts as a walk. 3 failed: `EventWalkTests.DrainReturnsTheWalkCountAndEmptiesTheQueue`, `EventQueueTests.ADrainSkipsAnEventWithNoTarget`, `EventAllocationTests.TheMeasuredBodyDoesRealWork`.
11. The root check of Decision 7 (`UiContext.cs:785`).
    - 11a. `IsInRootTree` always returns true. 1 failed: `EventWalkEdgeTests.ADrainSkipsATargetOutsideTheTree`.
    - 11b. `IsInRootTree` reads `top.IsRoot` only, so a root of another context passes. 2 failed: `EventWalkEdgeTests.ADrainSkipsATargetOutsideTheTree`, and `TransitionCurveTests.TheCurvesAllocateNothing`. The second failure is not related to the mutation. It is a flaky allocation gate outside this feature. It passed in the 21 other runs.
12. The nested-drain guard: `if (_draining && _events.Capacity < 0)`, so the guard never throws (`UiContext.cs:704`). 1 failed: `EventWalkEdgeTests.ANestedDrainThrows`.
13. The clear of the path in the `finally` (`EventWalk.cs:91-98`).
    - 13a. No `Array.Clear`. 2 failed: `EventWalkEdgeTests.ATreeDeeperThanThePathBufferWalksInOrder`, `EventWalkEdgeTests.AHandlerExceptionLeavesTheQueueAndThePathClean`.
    - 13b. The clear runs after a normal walk only, outside a `finally`. 1 failed: `EventWalkEdgeTests.AHandlerExceptionLeavesTheQueueAndThePathClean`.
14. The clear of the popped ring slot: no `items[_head] = default;` (`EventQueue.cs:181`). 1 failed: `EventWalkEdgeTests.AHandlerExceptionLeavesTheQueueAndThePathClean`.
15. The type check at the push: `if (!T.Accepts(header.Type) && _events.Capacity < 0)`, so no push throws (`UiContext.cs:802`). 1 failed: `EventQueueTests.APushWithAWrongTypeThrows`.
16. A planted allocation in the drain: `GC.KeepAlive(new object());` before the target step in the drain loop (`UiContext.cs:726`). 1 failed: `EventAllocationTests.PushAndDrainAllocateNothingAfterWarmUp`.
17. Extra, P1-9: the bubble loop reads `header.Type` in place of the type from the start of the walk (`EventWalk.cs:178`). **0 failures.** Finding CR-2.
18. Extra, P1-10: the target phase reads the bubble slot before the target capture call (`EventWalk.cs:143-155`). **0 failures.** Finding CR-3.
19. Extra: a planted allocation on the skip path of a target outside the tree (`IsInRootTree` returns false, `UiContext.cs:785`). **0 failures.** Finding CR-4.
20. Extra, a behavior probe on the unchanged source: a scratch test pushes 4 events into a ring of 4. The handler of event 1 pushes events 10 and 11. Decision 6 expects the walks `1, 3, 4` and 2 queued events. The actual result is `1, 3, 4, 10`, with 4 walks and 1 queued event. **The probe fails.** Finding CR-1. The scratch test file is deleted.

Probe count: 20 probes in 23 runs (16 rules of the brief, with 6, 11, and 13 in two variants, plus 4 extra probes). One more run of probe 8 stopped at the build (CS0649) and is not counted.

**The diff (R.5):** after the last probe, `diff -rq --exclude bin --exclude obj` on `src` and on `tests` of the copy and of the repository gives no output and exit code 0 for both. The copy matches the repository.

---

## Findings

### CR-1: A handler push into a full ring walks in the same drain
**Severity:** MEDIUM
**Rule:** 7. No regression and no leak (Decision 6), and 8. Test quality
**File:** `src/BrutalUi.Core/Tree/UiContext.cs:712-761`
**Problem:** Decision 6 says: "An event that a handler pushes waits for the next drain." The drain reads the count one time and pops that number of entries. When a handler pushes into a full ring, the push drops the oldest entry. The oldest entry is an entry of the current drain that did not walk yet. The ring then holds fewer entries of the current drain, but the loop still pops `pending` entries. The last pops take the events that the handler pushed.

```csharp
int pending = _events.Count;            // 4 in probe 20
for (int i = 0; i < pending; i++)
{
    if (!_events.TryPop(out UiQueuedEvent entry)) { break; }
    // The handler of event 1 pushes 10 and 11. The push of 11 drops event 2.
    // The ring holds 3, 4, 10, 11. The loop pops 3, 4, and 10.
    ...
}
```

Probe 20 shows the result: the walks are `1, 3, 4, 10`, and event 11 waits. The drain stays bounded, so no endless loop occurs. But the rule of Decision 6 does not hold, and the walk count includes a handler event. A game that queues a follow-up event from a handler (for example a click from a pointer up) gets it in the same drain only when the ring is full. No test covers a handler push into a full ring.

**Fix:** Reduce the remaining count by each drop that a walk causes. While entries of the current drain wait, each drop removes one of them, because they are the oldest. A skipped entry runs no handler, so it causes no drop.

```csharp
int remaining = _events.Count;
int walks = 0;
while (remaining > 0 && _events.TryPop(out UiQueuedEvent entry))
{
    remaining--;
    Element? target = entry.Header.Target;
    if (target is null || !IsInRootTree(target))
    {
        continue;
    }

    long droppedBefore = _events.Dropped;
    switch (entry.Header.Type) { /* the same cases */ }

    // A push from a handler into a full ring drops the oldest entry. That entry belongs to this drain.
    remaining -= (int)Math.Min(remaining, _events.Dropped - droppedBefore);
}
```

Add `AHandlerPushIntoAFullRingWaitsForTheNextDrain` to `EventQueueTests`: a ring of 4, 4 pushes, and the handler of event 1 pushes events 10 and 11. Expect the walks `1, 3, 4`, 3 walks, 2 queued events, and 1 drop. The next drain walks `10, 11`. Update the remarks of `DrainEvents` and Decision 6 with the rule.
**Effort:** Low

### CR-2: No test guards the rule that the walk reads the type one time
**Severity:** MEDIUM
**Rule:** 8. Test quality (rule 1 depends on it)
**File:** `tests/BrutalUi.Core.Tests/Input/EventWalkTests.cs`
**Problem:** P1-9 and the remarks of `EventWalk.Dispatch` say: "A handler that changes `Header.Type` does not change the slots of the walk." `UiEventHeader.Type` is a public writable field, so a handler can change it. Probe 17 makes the bubble loop read `header.Type` again. No test fails.

```csharp
// EventWalk.cs:64 reads the type one time. No test changes the type in a handler.
UiEventType type = header.Type;
```

**Fix:** Add a test. The capture handler of the root sets `e.Header.Type = UiEventType.PointerUp` on a pointer down. Use one recorder on the down slots and one recorder on the up slots. Expect 6 calls on the down recorder and 0 calls on the up recorder.

```csharp
root.OnPointerDownCapture = (ref PointerEvent e) =>
{
    downs.PointerCapture(ref e);
    e.Header.Type = UiEventType.PointerUp;
};
// After the drain: Assert.Equal(6, downs.Count); Assert.Equal(0, ups.Count);
```
**Effort:** Low

### CR-3: No test guards the late read of the target bubble slot
**Severity:** MEDIUM
**Rule:** 8. Test quality (rule 1 depends on it)
**File:** `tests/BrutalUi.Core.Tests/Input/EventWalkTests.cs`
**Problem:** P1-10 and the comment at `EventWalk.cs:154` say that the walk reads the bubble slot of the target after the capture call. A capture handler of the target that sets the bubble slot thus changes the next call. The DOM has the same rule. Probe 18 reads the bubble slot before the capture call. No test fails.
**Fix:** Add a test. The capture handler of the leaf sets `leaf.OnPointerDown = second.PointerBubble`. Expect one target-phase call on `second`. Add the opposite case: the capture handler sets the bubble slot to `null`, and the bubble slot of the leaf gets no call.

```csharp
leaf.OnPointerDownCapture = (ref PointerEvent e) =>
{
    first.PointerCapture(ref e);
    leaf.OnPointerDown = second.PointerBubble;
};
// After the drain: Assert.Equal(1, second.Count); Assert.Equal(UiEventPhase.Target, second[0].Phase);
```
**Effort:** Low

### CR-4: The gate does not measure the skip of a target outside the tree
**Severity:** LOW
**Rule:** 3. Zero allocation after warm-up, and 8. Test quality
**File:** `tests/BrutalUi.Core.Tests/Input/EventAllocationTests.cs:12-41, 108-183, 232-248`
**Problem:** The gate pushes a char with a null target, so it measures the null skip. It does not push an event at a target outside the tree, so it does not measure the false path of `IsInRootTree` (Decision 7). Probe 19 plants an allocation on that path. No test fails. The path is a loop with no allocation now, so the risk is small. But routing-gate and pointer-routing make this path common: a click on a panel that a handler closed.
**Fix:** In `BuildScene`, create a detached element with a child (`orphan` and `orphanLeaf`) that is not in the context. Add a field `OrphanDown` and push it in `Step`. Raise `QueueCapacity` to 9, so the ring still drops the 2 oldest entries. The walks of each step stay at 7 and the calls stay at 438. Update the push list in the remarks of the class.
**Effort:** Low

### CR-5: The edge tests read private fields by reflection
**Severity:** LOW
**Rule:** 8. Test quality (P4-10)
**File:** `tests/BrutalUi.Core.Tests/Input/EventWalkEdgeTests.cs:287-304`
**Problem:** `ReadPath` and `ReadRingItems` read `_eventPath`, `_events`, and `_items` by reflection. A rename of a field makes `GetField` return null, and the tests fail with a `NullReferenceException` that does not name the cause. The doc comment of `ReadPath` says: "The test project has no internal member for it until phase 5 adds `EventPathLength`." Phase 5 added `EventPathLength`, but it gives the length only. The comment is now out of date. Rule 7 depends on these two helpers: they are the only check that the path and the ring keep no element reference.
**Fix:** Add two internal members for the tests, in the pattern of `Transitions` and `Cascade`: `internal ReadOnlySpan<Element?> EventPathSlots => _eventPath;` on `UiContext`, and `internal ReadOnlySpan<UiQueuedEvent> Slots => _items;` on `EventQueue`, with `internal ReadOnlySpan<UiQueuedEvent> EventRingSlots => _events.Slots;` on `UiContext`. Replace the two helpers with these members, and remove `using System.Reflection;`. The fix touches `UiContext.cs` and `EventQueue.cs` with one line each.
**Effort:** Low

### CR-6: Section 9 states the drain call in `Update` as a fact
**Severity:** LOW
**Rule:** 9. `AGENTS.md` and the docs
**File:** `docs/technical-design.md:768`
**Problem:** Step 2 of section 9 says: "The drain empties the ring at the start of `UiContext.Update`, before the style pass." Criterion I2 says that `Update` does not call `DrainEvents`, and the code agrees. The next sentences of the step say that the routing-gate feature adds the call. A reader who stops after the first sentence gets the wrong state.
**Fix:** Write the step in the order of the facts: "The input layer pushes each event to the ring of the context with `UiContext.PushEvent`. The internal `DrainEvents` walks each entry (section 5, "Events"). The routing-gate feature calls the drain at the start of `UiContext.Update`, before the style pass. Until then, no code of the library calls the drain."
**Effort:** Low

### CR-7: The remarks of `UiEventPhase` put the target in the capture phase
**Severity:** LOW
**Rule:** 9. `AGENTS.md` and the docs (rule 1 in the docs)
**File:** `src/BrutalUi.Core/Input/UiEventEnums.cs:42-45`
**Problem:** The remarks say: "The walk calls the capture slots from the root down to the target, then the slots of the target, then the bubble slots from the parent of the target up to the root." The capture phase stops at the parent of the target. The capture slot of the target runs in the phase `Target`. The member doc of `Capture` is correct, so the remarks contradict it.
**Fix:** "The walk calls the capture slots from the root down to the parent of the target. Then it calls the capture slot and the bubble slot of the target, both with the phase `Target`. Then it calls the bubble slots from the parent of the target up to the root."
**Effort:** Low

---

## What's Already Good

- **The order and the stop.** One generic loop holds the order for all four structs. Probes 1 to 5 each fail between 1 and 16 tests. The stop check after each delegate (`Delegate.EnumerateInvocationList`) and the stop between the two target slots each have a direct test.
- **No recursion.** `Dispatch` fills the path with two loops, and `Walk` and `InvokeSlot` use loops. No method calls itself. The growth `Math.Max(count, 2 x length)` handles a deep chain in one step.
- **No leak.** The `finally` clears the path, and `TryPop` clears the popped slot before the walk. Probes 13a, 13b, and 14 show that the tests guard both, also on the exception path.
- **Zero allocation.** The gate covers each kind, a multicast slot, a stop, a full ring, a null target, and a tree deeper than the start path. Probe 16 shows that the gate also catches a plant in the drain, in addition to the plant of phase 5 in `Dispatch`. `ThrowTypeMismatch` keeps the string on the throw path only.
- **A small public API.** The public surface matches "Public API (end state)". `IUiEvent<TSelf>`, `ElementHandlers`, the payloads, `UiQueuedEvent`, `EventQueue`, and `EventWalk` are internal. The structs implement the internal interface explicitly, so no member leaks. `DrainEvents` and `EventPathLength` are internal. `PushEvent` has four overloads and no generic overload.
- **Clean seams.** The target step is one block in `DrainEvents`, with a comment for pointer-routing and keyboard-focus. A sibling sets `entry.Header.Target` there and does not touch `EventWalk`.
- **No GLFW.** `BrutalUi.Core` has no GLFW `using` and no GLFW package. The GLFW values appear in doc comments only.
- **Strong checks at the push.** The type check names the struct, the type, and a hint. The reset of the walk state means that a stopped struct from a caller walks in full (probe 6b).
- **The docs.** The XML docs cover each public member, the prose follows ASD-STE100, and `context.md` records each decision that the briefs did not settle.

---

## Recommended Fix Order

| Order | Finding | Severity | Effort | Impact |
|-------|---------|----------|--------|--------|
| 1 | CR-1: A handler push into a full ring walks in the same drain | MEDIUM | Low | Restores Decision 6 before routing-gate wires the drain into `Update`. |
| 2 | CR-2: No test for the type read one time | MEDIUM | Low | Guards a rule of the walk that a public writable field can break. |
| 3 | CR-3: No test for the late read of the target bubble slot | MEDIUM | Low | Guards the DOM-like rule at the target. Same file and same fix group as CR-2. |
| 4 | CR-4: The gate does not measure the skip of a target outside the tree | LOW | Low | Covers a path that pointer-routing makes common. |
| 5 | CR-5: The edge tests read private fields by reflection | LOW | Low | Makes the leak checks of rule 7 robust against a rename. |
| 6 | CR-6: Section 9 states the drain call in `Update` as a fact | LOW | Low | Doc accuracy. |
| 7 | CR-7: The remarks of `UiEventPhase` put the target in the capture phase | LOW | Low | Doc accuracy. |

The fix groups: CR-1 (`UiContext.cs`, with a test in `EventQueueTests.cs`), CR-2 and CR-3 (`EventWalkTests.cs`), CR-4 (`EventAllocationTests.cs`), CR-5 (`EventWalkEdgeTests.cs`), CR-6 (`technical-design.md`), and CR-7 (`UiEventEnums.cs`).

---

## Progress

- [x] CR-1: A handler push into a full ring walks in the same drain
- [x] CR-2: No test guards the rule that the walk reads the type one time
- [x] CR-3: No test guards the late read of the target bubble slot
- [x] CR-4: The gate does not measure the skip of a target outside the tree
- [x] CR-5: The edge tests read private fields by reflection
- [x] CR-6: Section 9 states the drain call in `Update` as a fact
- [x] CR-7: The remarks of `UiEventPhase` put the target in the capture phase

Fix briefs: `.beast-mode/briefs/input-routing/event-model/review-g1.md` to `review-g7.md` (group 7 updates the gate bullet of section 14 after CR-4). The verifier of the fixes gave PASS: build 0 warnings, 2163 tests and 0 failed, `scripts/test-linux.sh` PASS, and the event classes 13, 12, 9, 5, and 3.
