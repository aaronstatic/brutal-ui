# input-routing/event-model - Context & Decisions

**Last Updated:** 2026-10-22 09:44
**Current Phase:** Done (phases 1 to 5 and the cross-phase review)
**Status:** ✅ Ready for Review

---

## Quick Status

**What's Done:**
- ✅ The plan is accepted. The feature docs exist (`tasks.md`, `context.md`, `inbox.md`).
- ✅ Phase 1, group A: the six type files in `src/BrutalUi.Core/Input/`.
- ✅ Phase 1, group B: the six pointer slots on `Element`, `EventWalk`, the ring and the drain on `UiContext`, `EventRecorder`, and `EventWalkTests` (5 tests pass). See "Phase 1 Results".

- ✅ Phase 2: stop propagation. `StopPropagation()` stops the walk, also inside a multicast slot (5 more tests pass). See "Phase 2 Results".
- ✅ Phase 3, group 2: the scroll, key, and char pushes, the full drain switch, the 14-slot recorder, and `EventKindTests` (12 tests pass). See "Phase 3 Results".
- ✅ Phase 4, group 2: `EventQueueTests` (8) and `EventWalkEdgeTests` (5) pass. The solution gives 2156 tests, 0 failed. See "Phase 4 Results".

- ✅ Phase 5, group 1: `EventAllocationTests` (3 tests) and the plant check. The solution gives 2159 tests, 0 failed.
- ✅ Phase 5, group 2: the technical design (sections 5, 9, 14), the changelog line, and the final checks (build, tests, `scripts/test-linux.sh` exit 0, lint under 2.5). See "Phase 5 Results".

- ✅ Each phase verifier gave PASS and Demo: PASS (phases 1 to 5).
- ✅ The cross-phase review (`code-review.md`) found 7 findings (3 MEDIUM, 4 LOW). All 7 are fixed, and the verifier of the fixes gave PASS: build 0 warnings, 2163 tests and 0 failed, `scripts/test-linux.sh` PASS. See "Review Fixes".
- ✅ The draft plan of the next feature exists: `docs/features/input-routing/pointer-routing/implementation.md`.

**What's Next:**
- 📋 A person runs the items of "Needs Human Verification", including the Windows run.
- 📋 `/autorun-epic` commits this feature and runs `input-routing/pointer-routing`.

**Blockers:**
- None

---

## Key Files

### Core Implementation
- `src/BrutalUi.Core/Input/` - The event enums, the header, the four event structs, the handler delegate and the internal interface, the side object, the ring, and the walk.
- `src/BrutalUi.Core/Tree/Element.cs` - The 14 handler slot properties over the side object.
- `src/BrutalUi.Core/Tree/UiContext.cs` - The ring, the path buffer, `PushEvent`, and the internal `DrainEvents`.

### Related Files
- `tests/BrutalUi.Core.Tests/Input/` - `EventRecorder`, `EventWalkTests`, `EventKindTests`, `EventQueueTests`, `EventWalkEdgeTests`, and `EventAllocationTests`.
- `docs/features/input-routing/event-model/implementation.md` - The implementation plan.

---

## Important Decisions

### Decision R1: The caller accepted the recommended answer of each open scope question
**Date:** 2026-10-22
**Context:** The Notes of the plan hold four open scope questions: the key type, a target that left the tree, the char modifiers, and a stop in the target capture slot.
**Decision:** The plan uses option A of each question. `/autorun-epic` states that the four answers are accepted.
**Rationale:** The caller accepted the plan with these answers.
**Impact:** `KeyEvent.Key` is an `int` with the GLFW value. The drain skips a target outside the tree. `CharEvent` has no modifiers. A stop in the target capture slot skips the target bubble slot.

---

## Phase 1 Results

**Test counts:**
- Before the phase: `BrutalUi.Core.Tests` total 1309. Solution total 2121. 0 failed.
- After the phase: `BrutalUi.Core.Tests` total 1314. Solution total 2126. 0 failed.
- The demo command (`--filter-class '*EventWalkTests'`) gives total 5, failed 0.
- `dotnet build brutal-ui.sln` gives 0 warnings and 0 errors.

**The size of a ring entry:** `Unsafe.SizeOf<UiQueuedEvent>()` is 48 bytes (header 32, payload 16) on x64. The default ring of 256 entries is 12 KB for each context. The plan estimated 64 bytes and 16 KB. Phase 3 adds the scroll payload (two `float2` and the modifiers), so the payload and the entry can grow. Measure the size again in phase 5. The scratch test is deleted.

**Group A results (phase decisions):**
- **P1-1:** `new EventQueue(capacity)` throws `ArgumentOutOfRangeException` for a capacity of 0 or less. `UiContext` maps 0 to `DefaultEventQueueCapacity` before it calls the constructor.
- **P1-2:** A `default(EventQueue)` has no array. `Count` and `Capacity` are 0, and `TryPop` returns false. Code must not push to it.
- **P1-3:** On a full ring, `Push` writes into the head slot, moves the head by one, and keeps the count.
- **P1-4:** `TryPop` on an empty ring sets `entry` to `default` and returns false.
- **P1-5:** The fields of `ElementHandlers`, `PointerPayload`, `UiEventPayload`, and `UiQueuedEvent` are `internal`. The members of `EventQueue` are `public` inside the internal struct, as in `ElementStorage`.
- **P1-6:** `UiEventHeader.Phase` and `CurrentTarget` use `{ readonly get; internal set; }`.
- **P1-7:** `PointerEvent.Store` writes only the pointer fields of the union. The push (`Enqueue<T>`) starts from a `default` payload.
- **P1-8:** The slot fields of `ElementHandlers` have bare names, for example `PointerMove` and `PointerMoveCapture`.

**Group B decisions (the brief did not settle them):**
- **P1-9:** The walk reads the event type one time at the start. A handler that changes `Header.Type` does not change the slots of the walk. This rule matches A3 for the path.
- **P1-10:** The walk reads `Element.Handlers` one time for each element of each phase, and reads a slot just before it calls the slot. At the target, the walk reads the bubble slot after the capture call. A capture handler of the target that sets or clears the bubble slot of the target thus changes the next call. This order matches the DOM, which reads the listener list at each invoke.
- **P1-11:** The path grows with `new Element[Math.Max(n, 2 x length)]` and no copy. The old buffer holds no reference that the walk needs. `PaintWalk` uses `Array.Resize` because its stack holds live frames.
- **P1-12:** `EventWalk.Dispatch` returns at once for a null target. The drain skips such an entry first (A4). The check guards a direct call of the walk.
- **P1-13:** `Enqueue<T>` copies the `in` event into a local, because `T.HeaderOf` takes a `ref`. The copy is on the stack and allocates nothing. The entry header comes from the public `UiEventHeader` constructor, so the phase is `None` and the current target is null. Phase 2 can reset the stop flag in the same step.
- **P1-14:** The drain skips an entry with a type that no struct of the ring carries (`None` now, and `Scroll`, `KeyDown`, `KeyUp`, and `Char` until phase 3). The skip does not count as a walk. Phase 4 adds the `Accepts` check at the push, so such an entry cannot enter the ring after phase 4.
- **P1-15:** When `TryPop` returns false inside the count loop, the drain stops. A nested drain from a handler can empty the ring before the outer drain ends. Phase 4 adds the guard against a nested drain.
- **P1-16:** An exception from a handler leaves `DrainEvents`. The drain lost the popped entry. The entries that it did not pop stay in the ring. The remarks of `DrainEvents` state this rule.
- **P1-17:** A negative `CreateInfo.EventQueueCapacity` throws `ArgumentOutOfRangeException` from `EventQueue` now, with the parameter name `capacity`. The constructor of `UiContext` has no `<exception>` tag for it yet. Phase 4 (task 4.1) adds the check with a message that names the member, and the tag.
- **P1-18:** `EventRecorder` exposes its two pointer delegates as `PointerCapture` and `PointerBubble`. `EachPointerTypeReachesItsOwnSlots` puts one recorder on the two slots of each pointer type. The struct `EventRecord` lives in `EventRecorder.cs`.

**Blast radius of the shared changes:**
- `Element`: one more reference field (8 bytes for each element on x64). The six slot properties are new members. No existing member changed. All tests of the four test projects pass.
- `UiContext`: both constructors now allocate the ring (12 KB for 256 entries) and the path buffer (32 references). The allocation is at construction, not in `Update` or `Draw`, so the allocation tests stay at 0 bytes. `CreateInfo` gets one optional member, so each existing object initializer still compiles. `Update` does not call `DrainEvents` (A5).

**Follow-up work:**
- Phase 4 clears the path buffer in a `finally`. Until then, the path buffer keeps the element references of the last walk until the next walk overwrites them. A removed panel can thus stay alive until the next event of the context.
- Phase 2 adds the stop check after each delegate of a slot. The walk now calls a multicast slot through one delegate call.

---

## Phase 2 Results

**Test counts:** `BrutalUi.Core.Tests` 1319 (was 1314). Solution 2131 (was 2126). 0 failed.

**Demo:** `dotnet run --project tests/BrutalUi.Core.Tests/BrutalUi.Core.Tests.csproj -- --filter-class '*EventWalkTests'` gives total 10, failed 0.

**Build:** `dotnet build brutal-ui.sln` gives 0 Warning(s) and 0 Error(s).

**What changed:**
- `UiEventHeader` has `IsPropagationStopped` (readonly getter, private setter), `StopPropagation()`, and the internal `ResetWalkState()`.
- `UiContext.Enqueue<T>` calls `ResetWalkState()` on the entry header, so a queued entry never arrives stopped.
- `EventWalk` calls each slot through the private `InvokeSlot<T>`. It enumerates with `Delegate.EnumerateInvocationList` and returns after the first delegate that sets the stop flag. The file holds no `GetInvocationList`. No method calls itself.
- `EventRecorder.StopAt(element, phase, captureSlot)` stops the event after the matching call is recorded. It allocates nothing in a call.
- 5 new tests in `EventWalkTests`: `AStopInCaptureEndsTheWalk`, `AStopInTheTargetCaptureSlotSkipsTheTargetBubbleSlot`, `AStopInBubbleSkipsTheAncestors`, `AStopInAMulticastSlotSkipsTheNextDelegate`, `AStopAppliesToOneEventOnly`.

**Decisions that the brief did not settle:**
- P2-1: `ResetWalkState()` sets `Phase` to `None`, `CurrentTarget` to null, and the stop flag to false. The call comes after the entry header constructor, so the reset is explicit although the constructor already gives these values.
- P2-2: `EventRecorder.StopAt` takes `(Element, UiEventPhase, bool captureSlot)`. The recorder stops the event after it records the call, so the stopping call is part of the record. `Reset()` keeps the `StopAt` setting. The setting has no clear method yet.
- P2-3: `AStopAppliesToOneEventOnly` uses one recorder for the down slots (with `StopAt`) and one for the up slots. It also pushes an up event whose caller stopped the struct before the push. The up event still walks six calls, which covers task 2.2.
- P2-4: `AStopInBubbleSkipsTheAncestors` stops at the bubble phase of the middle element, so the root bubble slot is the skipped ancestor.
- P2-5: `AStopInAMulticastSlotSkipsTheNextDelegate` combines two recorder delegates with `+` on the bubble slot of the target, and adds a third recorder on the root. The root records one call (capture), which shows that the walk also skips the later phases.

---

## Phase 3 Results

**Test counts:** Solution 2143 (was 2131). 0 failed. `EventKindTests` has 12 cases. `EventWalkTests` still has 10.

**Demo:**
- `dotnet run --project tests/BrutalUi.Core.Tests/BrutalUi.Core.Tests.csproj -- --filter-class '*EventKindTests'` gives total 12, failed 0.
- The same command with `'*EventWalkTests'` gives total 10, failed 0.

**Build:** `dotnet build brutal-ui.sln` gives 0 Warning(s) and 0 Error(s). `dotnet test --solution brutal-ui.sln` gives total 2143, failed 0.

**EventWalk.cs:** `sha256sum src/BrutalUi.Core/Input/EventWalk.cs` gives `15cbb05ca3d65b6e9318483901154c1cb39f78148ec2a87e4d7c9821b6172bc7`. This equals the hash after phase 2, so the file did not change.

**What changed (group 2):**
- `UiContext` has the public `PushEvent(in ScrollEvent)`, `PushEvent(in KeyEvent)`, and `PushEvent(in CharEvent)`. Each calls `Enqueue<T>`. There is no generic overload.
- `DrainEvents` sends `Scroll`, `KeyDown`/`KeyUp`, and `Char` to `DispatchQueued<T>`. Only `UiEventType.None` still gets no walk (P1-14).
- `EventRecorder.Register` sets all 14 slots. The recorder has the delegates `ScrollCapture`, `ScrollBubble`, `KeyCapture`, `KeyBubble`, `CharCapture`, and `CharBubble`, and the fields `LastPointer`, `LastScroll`, `LastKey`, and `LastChar`.
- `EventKindTests`: the theory `EachKindReachesOnlyItsOwnSlots` (7 cases), `PointerPayloadSurvivesTheQueue`, `ScrollPayloadSurvivesTheQueue`, `KeyPayloadSurvivesTheQueue`, `CharPayloadSurvivesTheQueue`, and `AKeyDownWalksCaptureTargetBubble`.

**Group 1 decisions:**
- P3-1: The `UiEventPayload` fields are `Scroll`, `Key`, and `Char`, as the existing `Pointer` field. The key code reads as `payload.Key.Key`.
- P3-2: `KeyPayload` stores `IsRepeat` as a `bool`, not as a flags byte.
- P3-3: `ScanCode` has the doc text "The platform scan code of the key."

**Size of the ring entry:** `Unsafe.SizeOf<UiQueuedEvent>()` is 64 bytes (header 40, payload 20, plus padding). It was 48 after phase 1. A scratch test measured it. The test is deleted.

**Decisions that the brief did not settle:**
- P3-4: Each `Last<Kind>` field holds a copy of the whole event struct (header and fields), not only the payload struct. The recorder writes it when `Header.Phase` is `Target`, in both the capture slot and the bubble slot of the target. The bubble slot call is the last write, and it sees the same values.
- P3-5: The `Last<Kind>` fields are public fields of the test helper. They are not properties, so the struct stays assignable with no extra copy.
- P3-6: The recorder uses private typed methods for the eight delegates. Each method sets the `Last<Kind>` copy, then calls the generic `Capture<T>` or `Bubble<T>`. The constructor creates each delegate one time.
- P3-7: `Register` sets the same bubble delegate on both `KeyDown` and `KeyUp`, and the same capture delegate on both capture slots. The record `Type` tells which event came.
- P3-8: `EventKindTests` uses a private nested `Tree` class that builds root, middle, and leaf, adds the root to a context, and registers a new recorder. The class holds no static state.
- P3-9: `KeyPayloadSurvivesTheQueue` also pushes a `KeyUp` with `IsRepeat` false, to show that the flag does not stay from the earlier event. This adds no test case.
- P3-10: The doc comment of `DrainEvents` now says that only `None` has no walk.

---

## Phase 4 Results

**Test counts:** Solution 2156 (was 2143). 0 failed. `EventQueueTests` has 8 cases. `EventWalkEdgeTests` has 5 cases.

**Demo:**
- `dotnet run --project tests/BrutalUi.Core.Tests/BrutalUi.Core.Tests.csproj -- --filter-class '*EventQueueTests'` gives total 8, failed 0.
- The same command with `'*EventWalkEdgeTests'` gives total 5, failed 0.

**Build:** `dotnet build brutal-ui.sln` gives 0 Warning(s) and 0 Error(s). `dotnet test --solution brutal-ui.sln` gives total 2156, failed 0.

**What changed (group 1, the rules):**
- P4-1: `UiContext` gets the public get-only `EventQueueCapacity` and `DroppedEventCount`. They read the ring. `CreateInfo.EventQueueCapacity` and `DefaultEventQueueCapacity` existed already.
- P4-2: A negative capacity throws `ArgumentOutOfRangeException` with the parameter name `info` and the actual value, and the message "The CreateInfo.EventQueueCapacity must not be negative. Use 0 for the default of 256." The check runs after the null check of the measurer.
- P4-3: A wrong type throws `ArgumentException` with the parameter name `e` and the message "A {T} cannot carry the type {type}. {hint}". The hint is "Use a PointerEvent.", "Use a ScrollEvent.", "Use a KeyEvent.", or "Use a CharEvent.". For `None` or an undefined value, the hint is "Set a valid type in the event header.". The throw lives in the helper `ThrowTypeMismatch<T>`, marked `[DoesNotReturn]`.
- P4-4: A nested drain throws `InvalidOperationException` with "A drain of this UiContext runs already. Do not call Update or the drain from an event handler."
- P4-5: P1-14: the default case of the drain switch stays as a guard, with a comment.
- P4-6: The parent walk and the root test live in the private `IsInRootTree`. A skipped entry does not count as a walk.
- P4-7: In `EventWalk`, steps 3 to 5 moved into a private method `Walk`. `Dispatch` calls it inside a `try`, and the `finally` runs `Array.Clear(path, 0, count)`. No method calls itself.
- P4-8: The four `PushEvent` methods and `DrainEvents` got `<exception>` tags.
- P4-9: No existing test pushed a mismatched type. `EventQueue.cs` did not change.
- Solution total before group 1: 2143, 0 failed.

**Decisions that the brief did not settle (group 2, the tests):**
- P4-10: The path buffer and the ring slots are read with reflection in the test only (`_eventPath`, `_events`, `_items`), because no internal member exists yet. Phase 5 adds `internal int EventPathLength`. The reflection helpers `ReadPath` and `ReadRingItems` sit at the end of `EventWalkEdgeTests`. They run outside any allocation gate.
- P4-11: "A chain of 40 levels" means 40 ancestors and one leaf (41 elements). This gives 40 capture, 2 target, and 40 bubble records (82), and it is deeper than `EventWalk.InitialPathSize` (32). The test also checks that the path buffer grew to 41 or more and holds no reference after the walk.
- P4-12: `ANestedDrainThrows` covers both ways in one test: the handler that catches the exception, and the handler that lets it leave the outer drain. A third drain after each way runs.
- P4-13: `AHandlerExceptionLeavesTheQueueAndThePathClean` pushes 3 events into a ring of 256. The first throws. It checks ring slot 0 (popped, null target), slots 1 and 2 (waiting, target kept), and the path buffer (all null) after the exception. After the second drain, it checks that every ring slot and every path slot is null.
- P4-14: `APushWithAWrongTypeThrows` tries every other type for each struct, plus `None`, plus `(UiEventType)99` for the pointer struct. It checks the parameter name `e`, the "A {T} cannot carry the type" text, and two hints ("Use a ScrollEvent." and the `None` hint). It also checks that the ring stays empty.
- P4-15: `ADrainSkipsATargetOutsideTheTree` uses four skipped targets: a leaf and a top element of a removed subtree, a leaf of a removed root (`RemoveRoot`), and a leaf in another context. One valid event (5 pushes) gives 1 walk.
- P4-16: The tests tell the pushed events apart by `Header.Timestamp`, with a lambda handler that adds the value to a list. The `EventRecorder` did not change.

---

## Phase 5 Results

**Test counts:** Solution 2159 (was 2156). 0 failed. `EventAllocationTests` has 3 cases.

**Gate results (class `*EventAllocationTests`, five runs, Debug):** each run gives total 3, failed 0, succeeded 3, exit 0.
- `PushAndDrainAllocateNothingAfterWarmUp`: 0 bytes over 100 steps (1000 pushes, 100 drains, 700 walks).
- `APushToAFullQueueAllocatesNothing`: 0 bytes over 1000 pushes into a full ring, and 1000 more drops.
- `TheMeasuredBodyDoesRealWork`: passes. Each step gives 438 handler calls and 7 walks. After 10 steps, `DroppedEventCount` is 20 and `EventPathLength` is 64 or more.

**Plant check (task 5.5):**
- Planted line: `GC.KeepAlive(new object());` as the first line of `EventWalk.Dispatch`.
- `dotnet test --solution brutal-ui.sln` with the plant: exit 2, total 2159, failed 1. The failed gate: `BrutalUi.Core.Tests.EventAllocationTests.PushAndDrainAllocateNothingAfterWarmUp`.
- Restore from the scratch copy with `cp`. `cmp` reports no difference. SHA-256 before the plant and after the restore: `e84ec520c8acc5b4db4fb8180d2bd4695d40f7547506c2e94ef795b9706307d8`.
- `dotnet test --solution brutal-ui.sln` after the restore: exit 0, total 2159, failed 0.

**Build:** `dotnet build brutal-ui.sln` gives 0 Warning(s) and 0 Error(s).

**What changed:**
- `UiContext` gets `internal int EventPathLength => _eventPath.Length;` for the tests.
- `tests/BrutalUi.Core.Tests/Input/EventAllocationTests.cs` is new. `ThousandElementAllocationTests.cs` did not change. `EventWalk.cs` is equal to its state before the plant.
- The gate found no allocation. No change in `src/` was needed, and the `Delegate.EnumerateInvocationList` risk of Decision 5 did not occur (the multicast slot and the single-target slots both measure 0 bytes).

**Decisions that the brief did not settle:**
- P5-1: The order of the 10 pushes is: extra pointer move, pointer up, pointer move, pointer down, scroll, key down, key up, char (all at the deepest leaf), pointer down at the side leaf, char with a null target. The ring drops the first two (the extra move and the pointer up), so a full walk of each struct survives: pointer (move and down), scroll, key (down and the stopped up), and char. The pointer up walk has no measure, but it uses the same code as the pointer down walk with another slot.
- P5-2: "Level 20" is the 20th element under the root (the root is level 0, the leaf is level 40). The stop handler counts as a key call and stops the key up capture walk after the root and levels 1 to 19, so the walk makes 21 calls.
- P5-3: The multicast slot is the bubble slot of level 10 for a pointer down. It holds `CountPointer` and `CountPointerAgain`. The second delegate counts in `ExtraCalls`, so the pointer down at the leaf makes 83 calls.
- P5-4: The root, the 40 levels, the side element, and the side leaf register all 14 slots. The expected calls for each step: pointer 170, extra 1, scroll 82, key 103, char 82 (total 438). The comment of the class states the order and the count.
- P5-5: In `APushToAFullQueueAllocatesNothing`, 8 pushes fill the ring after the warm-up and before the release. The test then measures the 1000 pushes of one move event and checks that the queued count stays at 8.
- P5-6: The scene holds the 10 events as `required` fields, so `Step` passes them with `in` and copies nothing extra.

**Group 2 results (the docs and the final checks):**
- `docs/technical-design.md`: section 5 "Events" holds the model (structs, 14 slots, build-time delegates, ring of 64-byte entries and 16 KB, internal drain, walk order, stop rule). Section 9 "Flow" has a new step 2 for the push and the drain. Section 14 has two new bullets: the event tests and the gate `EventAllocationTests` with the plant result of task 5.5. The `UiInput`, `PickingEnabled`, and `IsPickable` lines did not change.
- `CHANGELOG.md`: one line under `## [Unreleased]`, `### Added`.
- `dotnet build brutal-ui.sln`: 0 Warning(s), 0 Error(s).
- `dotnet test --solution brutal-ui.sln`: total 2159, failed 0, succeeded 2159.
- `scripts/test-linux.sh; echo "exit=$?"`: `test-linux: PASS build, test, gpu, smoke (120 frames) in 15 s`, then `exit=0`. The script did not change.
- Prose lint (`ste-lint.py --fail-over 2.5`, per100w): `technical-design.md` 1.51, `CHANGELOG.md` 1.60, `implementation.md` 1.73. No hit above 2.5. `implementation.md` needed no change.
- Decisions the brief did not settle: P5-7 The new step in section 9 is step 2, so the old steps 2 to 5 became 3 to 6. The text says that the routing-gate feature adds the drain call, as the model says. P5-8 The section 5 "Shape" block did not change, because the brief names the "Events" paragraph only. The block lists no event member yet.

---

## Review Fixes

- CR-1: `DrainEvents` reduces the remaining count by each drop that a walk causes, so a handler push into a full ring waits for the next drain (Decision 6). Test: `AHandlerPushIntoAFullRingWaitsForTheNextDrain`. `EventQueueTests` has 9 cases (the phase 4 demo line "total 8" is now 9). Solution total 2160.
- CR-2: Test `AHandlerThatChangesTheTypeDoesNotChangeTheSlots` guards the single read of `Header.Type` (P1-9). The root capture handler sets the type to `PointerUp`. Expect 6 down calls and 0 up calls. Probe 17 (bubble loop reads `header.Type`) now fails this test.
- CR-3: Tests `ATargetCaptureHandlerThatSetsTheBubbleSlotGetsTheNewSlot` and `ATargetCaptureHandlerThatClearsTheBubbleSlotSkipsIt` guard the late read of the target bubble slot (P1-10). Two separate tests, one for each case. Probe 18 (early read of the bubble slot) now fails both. `EventWalkTests` has 13 cases (the phase 1 and phase 2 demo lines "5" and "10" are now 13). Solution total 2163.
- CR-4: The gate now measures the skip of a target outside the tree (the false path of `IsInRootTree`, Decision 7). `BuildScene` makes a detached `orphan` with a child `orphanLeaf`, and the scene holds `OrphanDown`. `QueueCapacity` is 9. The 11 pushes of a step: extra move, pointer up (both dropped), move, down, scroll, key down, key up, char, side down, orphan down, null char. The ring still drops 2 entries, the walks stay at 7, and the calls stay at 438. Gate: 0 bytes in 3 runs of the class. Plant `GC.KeepAlive(new object());` on the false path of `IsInRootTree` fails `PushAndDrainAllocateNothingAfterWarmUp` (total 3, failed 1). `UiContext.cs` restored with `cp`, `cmp` reports no difference. Solution total stays 2163. Doc follow-up: the "fifth allocation gate" bullet in section 14 of `docs/technical-design.md` now states 11 pushes in a ring of 9, the target outside the tree, 1100 pushes, 100 drains, and 700 walks.
- CR-5: `EventWalkEdgeTests` no longer uses reflection. New internal members: `UiContext.EventPathSlots` (`ReadOnlySpan<Element?>` over `_eventPath`, no warning), `UiContext.EventRingSlots` (`_events.Slots`), and `EventQueue.Slots` (public inside the internal struct, a `readonly` span property on the field, so no struct copy). The helpers `ReadPath` and `ReadRingItems` are gone. Solution total stays 2163.
- CR-6: Step 2 of section 9 of `docs/technical-design.md` now states the facts in order: the input layer pushes with `UiContext.PushEvent`, the internal `DrainEvents` walks each entry, `Update` does not call the drain yet, and the routing-gate feature adds the call before the style pass. Docs only.
- CR-7: The remarks of `UiEventPhase` now state the walk order: capture slots from the root down to the parent of the target, then the capture and bubble slots of the target with the phase `Target`, then bubble slots from the parent of the target up to the root. Only `UiEventEnums.cs` changed. Docs only.

---

## Gotchas & Learnings

### 1. A push from a handler into a full ring broke the drain count
**Problem:** The drain read the count one time. A handler push into a full ring dropped an entry of the current drain, so the drain walked an event from the handler in the same drain (CR-1, probe 20).
**Solution:** The drain reduces the remaining count by each drop that a walk causes.
**Lesson:** A count from the start of a loop is only safe when nothing in the loop can remove an entry that the count covers.

### 2. A rule with no test is not a rule
**Problem:** The single read of `Header.Type` (P1-9) and the late read of the target bubble slot (P1-10) had no test. A mutation of each gave 0 failures (CR-2, CR-3).
**Solution:** Three tests now guard both rules.
**Lesson:** Each decision that the code records needs a test that fails when the decision changes. Mutation probes find the gaps.

### 3. A test change can leave a doc number stale
**Problem:** CR-4 raised the pushes of the gate from 10 to 11. Section 14 of the technical design still said 10.
**Solution:** A doc follow-up (`review-g7.md`) fixed the bullet.
**Lesson:** When a fix changes a number of a gate, search the docs for the old number.

---

## API Integration

- `UiContext.PushEvent` (four public overloads) takes the events. The internal `UiContext.DrainEvents()` walks them. routing-gate calls the drain from `Update`. This feature does not.

---

## State Management

- `UiContext` owns the event ring and the path buffer. `Element` owns one nullable side object with the handler slots.

---

## Testing Approach

### Unit Tests
**Files:** `tests/BrutalUi.Core.Tests/Input/*.cs`
- The walk order, the stop rule, the slot of each kind, the payload through the ring, the full ring, and the edge rules of the drain.

### Allocation Tests
**Files:** `tests/BrutalUi.Core.Tests/Input/EventAllocationTests.cs`
- 0 bytes for a push, a drain, and a walk of each kind after warm-up.

---

## Needs Human Verification

`/autorun-feature` adds one line per item that the verifier cannot check, for a person to test.

- [ ] Phase 1 demo: a unit test pushes a pointer down at the leaf of a 3-deep tree (root, middle, leaf), drains the queue, and asserts capture (root, middle), target (leaf), and bubble (middle, root). The developer runs `dotnet run --project tests/BrutalUi.Core.Tests/BrutalUi.Core.Tests.csproj -- --filter-class '*EventWalkTests'` and reads a total of 5 with `failed: 0`.
- [ ] Phase 2 demo: a unit test registers a capture handler on the middle element that calls `e.Header.StopPropagation()`. After the drain, the recorder holds root capture and middle capture only, and a second queued event still walks all six calls. The developer runs `dotnet run --project tests/BrutalUi.Core.Tests/BrutalUi.Core.Tests.csproj -- --filter-class '*EventWalkTests'` and reads a total of 10 with `failed: 0`.
- [ ] Phase 3 demo: a unit test pushes a key down at the leaf of the 3-deep tree and reads the six calls of `OnKeyDownCapture` and `OnKeyDown` in capture, target, and bubble order. The developer runs `dotnet run --project tests/BrutalUi.Core.Tests/BrutalUi.Core.Tests.csproj -- --filter-class '*EventKindTests'` and reads a total of 12 with `failed: 0`.
- [ ] Phase 4 demo: a unit test creates a context with `EventQueueCapacity = 4`, pushes 6 pointer events, and reads `QueuedEventCount` 4 and `DroppedEventCount` 2. The drain walks events 3 to 6 in order. The developer runs `dotnet run --project tests/BrutalUi.Core.Tests/BrutalUi.Core.Tests.csproj -- --filter-class '*EventQueueTests'` and reads a total of 8 with `failed: 0`.
- [ ] Phase 5 demo: the developer runs `dotnet run --project tests/BrutalUi.Core.Tests/BrutalUi.Core.Tests.csproj -- --filter-class '*EventAllocationTests'` and reads a total of 3 with `failed: 0`. A planted allocation at the start of `EventWalk.Dispatch` makes `dotnet test --solution brutal-ui.sln` exit non-zero.
- Note: the cross-phase review added tests. `EventWalkTests` now gives a total of 13 (the Phase 1 and Phase 2 lines say 5 and 10), and `EventQueueTests` gives 9 (the Phase 4 line says 8).
- [ ] Windows run (event-model, Q7 of the plan): on Windows, after `git pull --ff-only`, run `.\scripts\test-windows.ps1`. Expect the PASS line and `$LASTEXITCODE` 0.

---

## Performance Considerations

- The ring is created at construction, so a push never allocates.
- The path buffer grows by doubling at warm-up and never shrinks (A7).

---

## Next Steps

### Immediate (Today/This Session)
1. A person runs the demo lines and the Windows run of "Needs Human Verification".
2. `/autorun-epic` commits the feature and merges it into `main`.

### Short Term (This Week)
1. `input-routing/pointer-routing` sets the target in the target step of `DrainEvents` from the hit test. It does not change `EventWalk`.
2. `input-routing/routing-gate` calls `DrainEvents` from `Update` before the style pass, and adds the queue case to `ThousandElementAllocationTests`.

---

## Open Questions

- (none open. See Decision R1.)

---

## Session Notes

### 2026-10-22 08:06
- `/autorun-feature` started the feature in the called mode of `/autorun-epic`.
- The run created `tasks.md` (43 tasks over 5 phases), `context.md`, and `inbox.md`.

### 2026-10-22 Phase 1, group B
- The group wired the six pointer slots into `Element`, wrote `EventWalk`, added the ring and the drain to `UiContext`, and wrote `EventRecorder` and `EventWalkTests`.
- The build gives 0 warnings. The solution gives 2126 tests and 0 failed. See "Phase 1 Results" for the decisions P1-9 to P1-18.
- The phase 1 verifier gave PASS and Demo: PASS (`phase-1-verify.md`). The inbox holds no line.

### 2026-10-22 Phase 2, group 1
- The group added `StopPropagation`, the stop check after each delegate of a slot, the reset in `Enqueue<T>`, `EventRecorder.StopAt`, and 5 tests.
- The build gives 0 warnings. The solution gives 2131 tests and 0 failed. See "Phase 2 Results" for P2-1 to P2-5.
- The phase 2 verifier gave PASS and Demo: PASS (`phase-2-verify.md`). The inbox holds no line.
- The SHA-256 of `EventWalk.cs` after phase 2 is `15cbb05ca3d65b6e9318483901154c1cb39f78148ec2a87e4d7c9821b6172bc7`. Phase 3 must keep it.

### 2026-10-22 Phase 3, group 2
- The group added the three `PushEvent` overloads, the full drain switch, the 14-slot `EventRecorder`, and `EventKindTests` (12 tests).
- The build gives 0 warnings. The solution gives 2143 tests and 0 failed. See "Phase 3 Results" for P3-1 to P3-10.
- `EventWalk.cs` has the same SHA-256 as after phase 2. The ring entry measures 64 bytes.
- The phase 3 verifier gave PASS and Demo: PASS (`phase-3-verify.md`). The inbox holds no line.

### 2026-10-22 Phase 4, group 2
- The group wrote `EventQueueTests` (8 tests) and `EventWalkEdgeTests` (5 tests). It changed no source file.
- The build gives 0 warnings. The solution gives 2156 tests and 0 failed. See "Phase 4 Results" for P4-1 to P4-16.
- The phase 4 verifier gave PASS and Demo: PASS (`phase-4-verify.md`). The inbox holds no line.
- The group 1 agent used a shell heredoc two times (one edit script and one empty scratch file), against tool rule 1. The result is correct, and the scratch file is outside the repository.

### 2026-10-22 Phase 5, group 2
- The group wrote the event model into `docs/technical-design.md` (sections 5, 9, and 14) and added the changelog line.
- The build gives 0 warnings. The solution gives 2159 tests and 0 failed. `scripts/test-linux.sh` gives PASS and exit 0. The lint gives 1.51, 1.60, and 1.73. See "Phase 5 Results".
- The phase 5 verifier gave PASS and Demo: PASS (`phase-5-verify.md`): five runs of `EventAllocationTests` give 3 of 3, and the plant result is recorded. The inbox holds no line.

### 2026-10-22 09:44 Cross-phase review and close
- The review ran 20 mutation probes in a scratch copy and wrote `code-review.md` with 7 findings (CR-1 to CR-7). Seven fix groups (`review-g1.md` to `review-g7.md`) fixed all 7. The verifier of the fixes gave PASS.
- The final state: build 0 warnings and 0 errors, 2163 tests and 0 failed, `scripts/test-linux.sh` PASS. The event classes give 13, 12, 9, 5, and 3 tests.
- The draft plan of `input-routing/pointer-routing` exists. It holds four open scope questions in its Notes.
- The inbox holds no line: 0 done and 0 open. No bug to promote.
- Next: a person runs "Needs Human Verification". `/autorun-epic` owns the commit and the next feature.

---

*Update this file at the end of each work session. Run `/update-feature` before compacting conversations.*
