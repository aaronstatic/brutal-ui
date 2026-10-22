# input-routing/event-model - Task Checklist

**Last Updated:** 2026-10-22 09:44
**Progress:** 43/43 tasks complete (100%)
**Advanced phases:** Phase 1 (ADVANCED, opus). Phases 2, 3, 4, and 5 are STANDARD (sonnet).

Source paths: `Core/` means `src/BrutalUi.Core/`. `Input/` means `src/BrutalUi.Core/Input/`. `Tests/` means `tests/BrutalUi.Core.Tests/`. "The plan" means `implementation.md` of this feature. Its sections "Public API (end state)", "Internal Types", "Data Flow", and "The Seams for the Siblings" hold the rules that the tasks name. Each phase ends with `dotnet build brutal-ui.sln 2>&1 | tail -n 30` and `dotnet test --solution brutal-ui.sln 2>&1 | tail -n 30` (tool rule 5). Expect `0 Warning(s)`, `0 Error(s)`, and 0 failed. The projects have no implicit usings, so each new file holds its own `using` lines. Each public member needs an XML doc comment (CS1591 is an error). Use the file-scoped `namespace BrutalUi.Core;` in `src/` and `namespace BrutalUi.Core.Tests;` in the tests. Prose in comments follows ASD-STE100.

---

## Phase 1: The Pointer Walk (12/12 complete)

**Demo:** a unit test pushes a pointer down at the leaf of a 3-deep tree (root, middle, leaf), drains the queue, and asserts capture (root, middle), target (leaf), and bubble (middle, root). The developer runs `dotnet run --project tests/BrutalUi.Core.Tests/BrutalUi.Core.Tests.csproj -- --filter-class '*EventWalkTests'` and reads a total of 5 with `failed: 0`.
**Needs:** none
**Agent tier:** ADVANCED (opus)

**Group A (the types, 6 new files in `Input/`):**

- [x] ✅ **1.1 Write UiEventEnums.cs** - Completed 2026-10-22
  - Description: Write `UiEventEnums.cs`: `UiEventType`, `UiEventPhase`, `PointerButton`, and `[Flags] UiModifiers` with the values of the Public API section. The doc comment of `PointerButton` and `UiModifiers` states that the values equal the GLFW values, so the adapter casts with no table. `UiEventType` has no enter and no leave member (A1).
  - File(s): `src/BrutalUi.Core/Input/UiEventEnums.cs`
  - Estimate: 3 steps

- [x] ✅ **1.2 Write UiEventHeader.cs** - Completed 2026-10-22
  - Description: Write `UiEventHeader.cs`: the struct with `Type`, `Timestamp` (`double`, seconds, A6), and `Target` as public fields, the constructor `(UiEventType type, double timestamp, Element? target)`, and `Phase` and `CurrentTarget` as properties with an `internal set`. The remarks state: the caller sets the target (A4), the walk writes `Phase` and `CurrentTarget` before each call, and a change of `Target` during the walk does not change the path (A3).
  - File(s): `src/BrutalUi.Core/Input/UiEventHeader.cs`
  - Estimate: 3 steps

- [x] ✅ **1.3 Write UiEventHandler.cs** - Completed 2026-10-22
  - Description: Write `UiEventHandler.cs`: the public delegate `UiEventHandler<TEvent>(ref TEvent e) where TEvent : struct`, and the internal interface `IUiEvent<TSelf>` with the five static abstract members of the section "Internal Types". The remarks of the delegate state: register at build time, not in the frame loop. A delegate with a closure allocates when the code creates it, not when the walk calls it.
  - File(s): `src/BrutalUi.Core/Input/UiEventHandler.cs`
  - Estimate: 4 steps

- [x] ✅ **1.4 Write PointerEvent.cs** - Completed 2026-10-22
  - Description: Write `PointerEvent.cs`: the public struct with `Header`, `Position` (`float2`), `Button`, and `Modifiers`. Implement `IUiEvent<PointerEvent>` explicitly. `Accepts` gives true for `PointerMove`, `PointerDown`, and `PointerUp`. `GetHandler` switches on the type and the capture flag to the six pointer slots, and gives null for another type. `Store` and `Load` copy the three fields through `UiEventPayload.Pointer`.
  - File(s): `src/BrutalUi.Core/Input/PointerEvent.cs`
  - Estimate: 6 steps

- [x] ✅ **1.5 Write ElementHandlers.cs** - Completed 2026-10-22
  - Description: Write `ElementHandlers.cs`: an `internal sealed class` with six fields: `PointerMove`, `PointerMoveCapture`, `PointerDown`, `PointerDownCapture`, `PointerUp`, and `PointerUpCapture`. The remarks cite A2.
  - File(s): `src/BrutalUi.Core/Input/ElementHandlers.cs`
  - Estimate: 2 steps

- [x] ✅ **1.6 Write EventQueue.cs** - Completed 2026-10-22
  - Description: Write `EventQueue.cs`: the internal `PointerPayload` struct, the explicit-layout `UiEventPayload` with `[FieldOffset(0)] PointerPayload Pointer`, the `UiQueuedEvent` struct, and the `EventQueue` struct. `EventQueue(int capacity)` creates the array. `Push(in UiQueuedEvent entry)` writes at `(head + count) % capacity`. A full ring moves the head by one, overwrites the oldest slot, and adds 1 to `Dropped` (a `long`). `TryPop(out UiQueuedEvent entry)` copies the head entry, writes `default` into the slot (no element reference stays), and moves the head. Members: `Count`, `Capacity`, `Dropped`. The remarks state the `ElementStorage` rule: keep the struct in a field, and change it through the field only.
  - File(s): `src/BrutalUi.Core/Input/EventQueue.cs`
  - Estimate: 7 steps

**Group B (the tree, the walk, and the first test, 6 files):**

- [x] ✅ **1.7 Add the side object and the six pointer slots to Element.cs** - Completed 2026-10-22
  - Description: Edit `src/BrutalUi.Core/Tree/Element.cs`: add `private ElementHandlers? _handlers;`, an `internal ElementHandlers? Handlers => _handlers;`, and the six pointer properties. A getter reads `_handlers?.<Slot>`. A setter returns at once for a `null` value with no side object. Else it writes `(_handlers ??= new ElementHandlers()).<Slot> = value`. The remarks state: the first registration creates the side object (A2). An element with no handler holds one null reference. `+=` combines delegates into a multicast slot, and the walk calls each delegate in the order of add.
  - File(s): `src/BrutalUi.Core/Tree/Element.cs`
  - Estimate: 5 steps

- [x] ✅ **1.8 Write EventWalk.cs** - Completed 2026-10-22
  - Description: Write `src/BrutalUi.Core/Input/EventWalk.cs`: `internal static class EventWalk` with `internal const int InitialPathSize = 32` and `Dispatch<T>(ref T e, ref Element[] path) where T : struct, IUiEvent<T>`, per the steps of the section "Data Flow" (steps 1 to 5). Take `ref UiEventHeader header = ref T.HeaderOf(ref e)`. For each element, read `element.Handlers`. When it is null, go to the next element. Invoke the slot with `handler(ref e)`. No method of the class calls itself. The class remarks follow the `PaintWalk` remarks: no recursion, the caller owns the buffer, the buffer grows by doubling and never shrinks (A7).
  - File(s): `src/BrutalUi.Core/Input/EventWalk.cs`
  - Estimate: 7 steps

- [x] ✅ **1.9 Add the ring, the path buffer, the push, and the drain to UiContext.cs** - Completed 2026-10-22
  - Description: Edit `src/BrutalUi.Core/Tree/UiContext.cs`: add `CreateInfo.EventQueueCapacity` (`int`, 0 gives the default), `public const int DefaultEventQueueCapacity = 256`, the fields `private EventQueue _events;` and `private Element[] _eventPath = new Element[EventWalk.InitialPathSize];`, the ring in both constructors, `public int QueuedEventCount`, `public void PushEvent(in PointerEvent e)`, and `internal int DrainEvents()`. The push builds a `UiQueuedEvent` with the header (with `Phase` `None` and `CurrentTarget` null) and `T.Store`, through a private generic `Enqueue<T>`. The drain reads `n = Count` at the start, pops `n` entries, skips an entry with a null target (A4), switches on the type to a private generic `DispatchQueued<T>(in UiQueuedEvent entry)` (`T e = T.Load(...)`, then `EventWalk.Dispatch(ref e, ref _eventPath)`), and returns the number of walks. The remarks of `DrainEvents` cite A5: routing-gate calls it from `Update`, and `Update` does not call it in this feature.
  - File(s): `src/BrutalUi.Core/Tree/UiContext.cs`
  - Estimate: 8 steps

- [x] ✅ **1.10 Write EventRecorder.cs** - Completed 2026-10-22
  - Description: Write `tests/BrutalUi.Core.Tests/Input/EventRecorder.cs`: an `internal sealed class EventRecorder` with a fixed `EventRecord[]` of 256 entries (`Element? CurrentTarget`, `UiEventPhase Phase`, `UiEventType Type`, `bool CaptureSlot`), `Count`, `Reset()`, `this[int]`, and generic methods `Capture<T>(ref T e)` and `Bubble<T>(ref T e)` with `where T : struct, IUiEvent<T>` (the test project reads internals). A full array stops the record and sets an `Overflow` flag, so the recorder never allocates after its construction. Add a helper `Register(Element element)` that sets the six pointer slots of the element to the two methods, with the delegates created one time in the constructor.
  - File(s): `tests/BrutalUi.Core.Tests/Input/EventRecorder.cs`
  - Estimate: 5 steps

- [x] ✅ **1.11 Write EventWalkTests.cs (5 tests)** - Completed 2026-10-22
  - Description: Write `tests/BrutalUi.Core.Tests/Input/EventWalkTests.cs` with 5 tests: `PointerDownWalksCaptureTargetBubble` (the confirmed demo: 6 records, in order: root capture, middle capture, leaf target capture slot, leaf target bubble slot, middle bubble, root bubble, each with its current target), `ARootTargetRunsTheTargetPhaseOnly`, `AnElementWithNoHandlerHasNoSideObject` (reads `Handlers` before and after a null set), `EachPointerTypeReachesItsOwnSlots` (a move calls no down slot), and `DrainReturnsTheWalkCountAndEmptiesTheQueue`.
  - File(s): `tests/BrutalUi.Core.Tests/Input/EventWalkTests.cs`
  - Estimate: 6 steps

- [x] ✅ **1.12 Run the build, the tests, and the demo, and record the baseline** - Completed 2026-10-22
  - Description: Run the build and the tests. Run the demo command. Record the test count before and after the phase, and the size of `UiQueuedEvent` (`Unsafe.SizeOf<UiQueuedEvent>()` in a scratch test, not committed), in `docs/features/input-routing/event-model/context.md`.
  - File(s): `docs/features/input-routing/event-model/context.md`
  - Estimate: 5 steps

---

## Phase 2: Stop Propagation (5/5 complete)

**Demo:** a unit test registers a capture handler on the middle element that calls `e.Header.StopPropagation()`. After the drain, the recorder holds root capture and middle capture only, and a second queued event still walks all six calls. The developer runs `dotnet run --project tests/BrutalUi.Core.Tests/BrutalUi.Core.Tests.csproj -- --filter-class '*EventWalkTests'` and reads a total of 10 with `failed: 0`.
**Needs:** Phase 1
**Agent tier:** STANDARD (sonnet)

- [x] ✅ **2.1 Add the stop to UiEventHeader.cs** - Completed 2026-10-22
  - Description: Edit `UiEventHeader.cs`: add `public bool IsPropagationStopped { get; private set; }` and `public void StopPropagation()`. The remarks state: the walk calls no later handler, also no later delegate of the same multicast slot. A call through a copy of the event has no effect, so call it on the `ref` parameter.
  - File(s): `src/BrutalUi.Core/Input/UiEventHeader.cs`
  - Estimate: 3 steps

- [x] ✅ **2.2 Reset the walk state in the push** - Completed 2026-10-22
  - Description: Edit `EventQueue.cs` or the `Enqueue<T>` step of `UiContext.cs`: a push writes the stop flag as false, so a reused struct of the caller does not arrive stopped. Add an internal `ResetWalkState()` on the header for this, and call it in the push.
  - File(s): `src/BrutalUi.Core/Input/UiEventHeader.cs`, `src/BrutalUi.Core/Tree/UiContext.cs` (or `src/BrutalUi.Core/Input/EventQueue.cs`)
  - Estimate: 3 steps

- [x] ✅ **2.3 Check the stop after each delegate in EventWalk.cs** - Completed 2026-10-22
  - Description: Edit `EventWalk.cs`: invoke each slot through `foreach (UiEventHandler<T> one in Delegate.EnumerateInvocationList(handler))`. Check `header.IsPropagationStopped` after each delegate, and return at once when it is true. The enumerator is a struct and allocates nothing (.NET 9 and later, see Decision 5). Keep one private generic `InvokeSlot<T>` helper that returns the stop flag, so the three phases share it.
  - File(s): `src/BrutalUi.Core/Input/EventWalk.cs`
  - Estimate: 5 steps

- [x] ✅ **2.4 Add StopAt to EventRecorder and 5 stop tests to EventWalkTests.cs** - Completed 2026-10-22
  - Description: Extend `EventRecorder` with a `StopAt` setting (an element, a phase, and a slot) and add 5 tests to `EventWalkTests.cs`: `AStopInCaptureEndsTheWalk`, `AStopInTheTargetCaptureSlotSkipsTheTargetBubbleSlot`, `AStopInBubbleSkipsTheAncestors`, `AStopInAMulticastSlotSkipsTheNextDelegate` (two delegates on one slot, the first stops), and `AStopAppliesToOneEventOnly` (two queued events, the first stops, the second walks six calls).
  - File(s): `tests/BrutalUi.Core.Tests/Input/EventRecorder.cs`, `tests/BrutalUi.Core.Tests/Input/EventWalkTests.cs`
  - Estimate: 8 steps

- [x] ✅ **2.5 Run the build, the tests, and the demo, and record the result** - Completed 2026-10-22
  - Description: Run the build and the tests. Run the demo command. Record the result in `context.md`.
  - File(s): `docs/features/input-routing/event-model/context.md`
  - Estimate: 3 steps

---

## Phase 3: The Scroll, Key, and Char Kinds (10/10 complete)

**Demo:** a unit test pushes a key down at the leaf of the 3-deep tree and reads the six calls of `OnKeyDownCapture` and `OnKeyDown` in capture, target, and bubble order. The developer runs `dotnet run --project tests/BrutalUi.Core.Tests/BrutalUi.Core.Tests.csproj -- --filter-class '*EventKindTests'` and reads a total of 12 with `failed: 0`.
**Needs:** Phase 2
**Agent tier:** STANDARD (sonnet)

**Group A (the types and the slots, 6 files):**

- [x] ✅ **3.1 Write ScrollEvent.cs** - Completed 2026-10-22
  - Description: Write `src/BrutalUi.Core/Input/ScrollEvent.cs`: `Header`, `Position` (`float2`, for the hit test of pointer-routing), `Delta` (`float2`), and `Modifiers`. `Accepts` gives true for `Scroll` only. Implement the interface as task 1.4 does.
  - File(s): `src/BrutalUi.Core/Input/ScrollEvent.cs`
  - Estimate: 4 steps

- [x] ✅ **3.2 Write KeyEvent.cs** - Completed 2026-10-22
  - Description: Write `KeyEvent.cs`: `Header`, `Key` (`int`, the GLFW key code value, Notes Q1), `ScanCode` (`int`), `Modifiers`, and `IsRepeat` (`bool`). `Accepts` gives true for `KeyDown` and `KeyUp`. The doc comment states that the adapter sends a GLFW repeat action as a `KeyDown` with `IsRepeat` true.
  - File(s): `src/BrutalUi.Core/Input/KeyEvent.cs`
  - Estimate: 4 steps

- [x] ✅ **3.3 Write CharEvent.cs** - Completed 2026-10-22
  - Description: Write `CharEvent.cs`: `Header` and `CodePoint` (`uint`, a Unicode scalar value). `Accepts` gives true for `Char` only. The remarks state: GLFW gives no modifiers with a char, so the struct has none (Notes Q3).
  - File(s): `src/BrutalUi.Core/Input/CharEvent.cs`
  - Estimate: 3 steps

- [x] ✅ **3.4 Add the 8 slot fields to ElementHandlers.cs** - Completed 2026-10-22
  - Description: Edit `ElementHandlers.cs`: add the 8 fields `Scroll`, `ScrollCapture`, `KeyDown`, `KeyDownCapture`, `KeyUp`, `KeyUpCapture`, `Char`, and `CharCapture`.
  - File(s): `src/BrutalUi.Core/Input/ElementHandlers.cs`
  - Estimate: 2 steps

- [x] ✅ **3.5 Add the 8 slot properties to Element.cs** - Completed 2026-10-22
  - Description: Edit `Element.cs`: add the 8 properties `OnScroll`, `OnScrollCapture`, `OnKeyDown`, `OnKeyDownCapture`, `OnKeyUp`, `OnKeyUpCapture`, `OnChar`, and `OnCharCapture`, with the setter rule of task 1.7.
  - File(s): `src/BrutalUi.Core/Tree/Element.cs`
  - Estimate: 4 steps

- [x] ✅ **3.6 Add the scroll, key, and char payloads to EventQueue.cs** - Completed 2026-10-22
  - Description: Edit `EventQueue.cs`: add the internal unmanaged `ScrollPayload`, `KeyPayload`, and `CharPayload`, each at `[FieldOffset(0)]` in `UiEventPayload`.
  - File(s): `src/BrutalUi.Core/Input/EventQueue.cs`
  - Estimate: 3 steps

**Group B (the push, the drain, and the tests, 4 files):**

- [x] ✅ **3.7 Add the three pushes and extend the drain switch in UiContext.cs** - Completed 2026-10-22
  - Description: Edit `UiContext.cs`: add `PushEvent(in ScrollEvent e)`, `PushEvent(in KeyEvent e)`, and `PushEvent(in CharEvent e)` through `Enqueue<T>`. Extend the switch of `DrainEvents`: `Scroll` goes to `DispatchQueued<ScrollEvent>`, `KeyDown` and `KeyUp` go to `DispatchQueued<KeyEvent>`, and `Char` goes to `DispatchQueued<CharEvent>`.
  - File(s): `src/BrutalUi.Core/Tree/UiContext.cs`
  - Estimate: 4 steps

- [x] ✅ **3.8 Register all 14 slots and keep the last payloads in EventRecorder.cs** - Completed 2026-10-22
  - Description: Edit `EventRecorder.cs`: `Register(Element)` sets all 14 slots. Add a copy of the last payload of each kind (`LastPointer`, `LastScroll`, `LastKey`, `LastChar`), written in the target phase.
  - File(s): `tests/BrutalUi.Core.Tests/Input/EventRecorder.cs`
  - Estimate: 4 steps

- [x] ✅ **3.9 Write EventKindTests.cs (12 cases)** - Completed 2026-10-22
  - Description: Write `tests/BrutalUi.Core.Tests/Input/EventKindTests.cs` with 12 test cases: the theory `EachKindReachesOnlyItsOwnSlots` over the 7 types (7 cases: 6 records, each of the pushed type and of its two slots), `PointerPayloadSurvivesTheQueue`, `ScrollPayloadSurvivesTheQueue`, `KeyPayloadSurvivesTheQueue`, `CharPayloadSurvivesTheQueue` (each field, the timestamp, and the target), and `AKeyDownWalksCaptureTargetBubble` (the demo).
  - File(s): `tests/BrutalUi.Core.Tests/Input/EventKindTests.cs`
  - Estimate: 7 steps

- [x] ✅ **3.10 Run the build, the tests, and the demo, and record the result** - Completed 2026-10-22
  - Description: Run the build and the tests. Run the demo command. Record the result in `context.md`.
  - File(s): `docs/features/input-routing/event-model/context.md`
  - Estimate: 3 steps

---

## Phase 4: The Full Queue and the Edge Rules (8/8 complete)

**Demo:** a unit test creates a context with `EventQueueCapacity = 4`, pushes 6 pointer events, and reads `QueuedEventCount` 4 and `DroppedEventCount` 2. The drain walks events 3 to 6 in order. The developer runs `dotnet run --project tests/BrutalUi.Core.Tests/BrutalUi.Core.Tests.csproj -- --filter-class '*EventQueueTests'` and reads a total of 8 with `failed: 0`.
**Needs:** Phase 3
**Agent tier:** STANDARD (sonnet)

**Group A (the rules, 3 files):**

- [x] ✅ **4.1 Add EventQueueCapacity, DroppedEventCount, and the capacity check to UiContext.cs** - Completed 2026-10-22
  - Description: Edit `UiContext.cs`: add `public int EventQueueCapacity` and `public long DroppedEventCount`. In `UiContext(in CreateInfo)`, throw `ArgumentOutOfRangeException` for a negative `EventQueueCapacity`, with a message that names the member and the default. 0 gives `DefaultEventQueueCapacity`.
  - File(s): `src/BrutalUi.Core/Tree/UiContext.cs`
  - Estimate: 4 steps

- [x] ✅ **4.2 Check the type in Enqueue<T>** - Completed 2026-10-22
  - Description: Edit `Enqueue<T>` in `UiContext.cs`: when `T.Accepts(e.Header.Type)` is false, throw `ArgumentException` that names the type and the struct, for example "A PointerEvent cannot carry the type KeyDown. Use a KeyEvent." `UiEventType.None` also throws. The throw allocates on the error path only.
  - File(s): `src/BrutalUi.Core/Tree/UiContext.cs`
  - Estimate: 4 steps

- [x] ✅ **4.3 Add the drain guard and the root check to DrainEvents** - Completed 2026-10-22
  - Description: Edit `DrainEvents` in `UiContext.cs`: add a `_draining` flag. A nested call throws `InvalidOperationException` ("A drain of this UiContext runs already. Do not call Update or the drain from an event handler."), as `Draw` does. Set the flag in a `try` and clear it in the `finally`. Keep the count at the start: an event that a handler pushes waits for the next drain. Skip an entry when the top of the parent chain of its target is not a root of this context (`top.IsRoot` and `_roots.IndexOf(top) >= 0`, Decision 7). Keep the target step in one place, with a comment that pointer-routing and keyboard-focus set the target there.
  - File(s): `src/BrutalUi.Core/Tree/UiContext.cs`
  - Estimate: 6 steps

- [x] ✅ **4.4 Clear the path buffer in a finally in EventWalk.cs** - Completed 2026-10-22
  - Description: Edit `EventWalk.cs`: clear `path[0 .. n-1]` in a `finally`, so the buffer keeps no element reference after a walk or an exception.
  - File(s): `src/BrutalUi.Core/Input/EventWalk.cs`
  - Estimate: 3 steps

**Group B (the tests, 3 files):**

- [x] ✅ **4.5 Write EventQueueTests.cs part 1 (the ring)** - Completed 2026-10-22
  - Description: Write `tests/BrutalUi.Core.Tests/Input/EventQueueTests.cs`, part 1 (the ring): `AFullQueueDropsTheOldestEvent` (the demo), `TheDropCountGrowsForEachDrop`, `TheDefaultCapacityIs256` (both constructors), and `ACapacityFromTheCreateInfoSizesTheRing`. Use `new UiContext(new UiContext.CreateInfo { Measurer = new RecordingRenderer(), EventQueueCapacity = 4 })`.
  - File(s): `tests/BrutalUi.Core.Tests/Input/EventQueueTests.cs`
  - Estimate: 6 steps

- [x] ✅ **4.6 Add EventQueueTests.cs part 2 (the checks)** - Completed 2026-10-22
  - Description: Add part 2 (the checks) to `EventQueueTests.cs`: `ANegativeCapacityThrows`, `APushWithAWrongTypeThrows` (each struct with a type of another struct, and `None`), `ADrainSkipsAnEventWithNoTarget`, and `AnEventFromAHandlerWaitsForTheNextDrain`. The class holds 8 tests.
  - File(s): `tests/BrutalUi.Core.Tests/Input/EventQueueTests.cs`
  - Estimate: 6 steps

- [x] ✅ **4.7 Write EventWalkEdgeTests.cs (5 tests)** - Completed 2026-10-22
  - Description: Write `tests/BrutalUi.Core.Tests/Input/EventWalkEdgeTests.cs` with 5 tests: `ADrainSkipsATargetOutsideTheTree` (a removed subtree and a target in another context), `ATreeChangeDuringTheWalkKeepsThePath` (the root capture handler removes the middle element, and the middle and leaf handlers still run, A3), `ATreeDeeperThanThePathBufferWalksInOrder` (a chain of 40 levels, 80 capture and bubble records plus 2 target records, in order), `ANestedDrainThrows`, and `AHandlerExceptionLeavesTheQueueAndThePathClean` (the exception leaves the drain, the rest of the queue stays, the next drain runs, and each slot of the path buffer is null).
  - File(s): `tests/BrutalUi.Core.Tests/Input/EventWalkEdgeTests.cs`
  - Estimate: 8 steps

- [x] ✅ **4.8 Run the build, the tests, the demo, and the edge class, and record the results** - Completed 2026-10-22
  - Description: Run the build and the tests. Run the demo command and the class `*EventWalkEdgeTests`. Record the results in `context.md`.
  - File(s): `docs/features/input-routing/event-model/context.md`
  - Estimate: 3 steps

---

## Phase 5: The Allocation Gate and the Record (8/8 complete)

**Demo:** the developer runs `dotnet run --project tests/BrutalUi.Core.Tests/BrutalUi.Core.Tests.csproj -- --filter-class '*EventAllocationTests'` and reads a total of 3 with `failed: 0`. A planted allocation at the start of `EventWalk.Dispatch` makes `dotnet test --solution brutal-ui.sln` exit non-zero.
**Needs:** Phase 4
**Agent tier:** STANDARD (sonnet)

**Group A (the gate, 2 files):**

- [x] ✅ **5.1 Write the EventAllocationTests scene and Step** - Completed 2026-10-22
  - Description: Write `tests/BrutalUi.Core.Tests/Input/EventAllocationTests.cs` in the pattern of `TreeAllocationTests`: a private `Scene` with a context of `EventQueueCapacity = 8` (and a `RecordingRenderer` measurer), a chain of 40 levels under one root (deeper than `InitialPathSize`, so the warm-up grows the path, A7), a side branch, and a counter object. The scene registers all 14 slots on each chain element with delegates that the build creates one time, puts a second delegate on one slot (a multicast slot), and puts a stop handler on the `KeyUp` capture slot of level 20. Build the 10 events of the body before the measure: each of the 7 types at the deepest leaf, one pointer down at the side branch, one char with a null target, and one more pointer move, so the ring drops 2 entries in each step. `Step(Scene)` pushes the 10 events and calls `DrainEvents()`. It holds no lambda, no boxing, no string, and no `Assert`.
  - File(s): `tests/BrutalUi.Core.Tests/Input/EventAllocationTests.cs`
  - Estimate: 8 steps

- [x] ✅ **5.2 Add PushAndDrainAllocateNothingAfterWarmUp** - Completed 2026-10-22
  - Description: Add `[Fact] PushAndDrainAllocateNothingAfterWarmUp`: build the scene, run 2 steps (the warm-up), call `AllocationGate.ReleaseAllocationContext()`, read `GC.GetAllocatedBytesForCurrentThread()`, run 100 steps, read it again, and `Assert.Equal(0L, after - before)`.
  - File(s): `tests/BrutalUi.Core.Tests/Input/EventAllocationTests.cs`
  - Estimate: 3 steps

- [x] ✅ **5.3 Add APushToAFullQueueAllocatesNothing** - Completed 2026-10-22
  - Description: Add `[Fact] APushToAFullQueueAllocatesNothing`: warm up, release, then measure 1000 pushes of one prepared pointer event into a full ring, and assert 0 bytes and 1000 more drops.
  - File(s): `tests/BrutalUi.Core.Tests/Input/EventAllocationTests.cs`
  - Estimate: 3 steps

- [x] ✅ **5.4 Add TheMeasuredBodyDoesRealWork and EventPathLength** - Completed 2026-10-22
  - Description: Add `[Fact] TheMeasuredBodyDoesRealWork`: after 10 steps, assert the counter calls for each step (the exact count from the scene: the full walks, the stopped `KeyUp` walk, the skip of the null target), `DroppedEventCount` of 20, and the path buffer length of 64 or more (internal read of a new `internal int EventPathLength` on `UiContext`, added for this test).
  - File(s): `tests/BrutalUi.Core.Tests/Input/EventAllocationTests.cs`, `src/BrutalUi.Core/Tree/UiContext.cs`
  - Estimate: 5 steps

- [x] ✅ **5.5 Prove that the gate blocks a planted allocation** - Completed 2026-10-22
  - Description: Prove that the gate blocks. Copy `src/BrutalUi.Core/Input/EventWalk.cs` to the session scratch folder. Add `GC.KeepAlive(new object());` as the first line of `Dispatch`. Run `dotnet test --solution brutal-ui.sln > /dev/null 2>&1; echo "exit=$?"` and expect a non-zero exit. Restore the file from the copy, run the tests again, and expect 0 failed. Record both exit codes and the names of the failed gates in `context.md`.
  - File(s): `src/BrutalUi.Core/Input/EventWalk.cs` (temporary, restored), `docs/features/input-routing/event-model/context.md`
  - Estimate: 5 steps

**Group B (the record, 4 files):**

- [x] ✅ **5.6 Update the technical design, sections 5, 9, and 14** - Completed 2026-10-22
  - Description: Edit `docs/technical-design.md`. Section 5, "Events": replace the paragraph with the model of this feature: the header, the four structs, the 14 slots in a side object, the delegate at build time (a delegate is a method plus a target, so it fits the old rule), the ring, the internal drain, and the walk order with the stop rule. Section 9, "Flow": add the queue and the drain between steps 1 and 2. Section 14: add a bullet for the event tests and the gate `EventAllocationTests`, with the plant result of task 5.5.
  - File(s): `docs/technical-design.md`
  - Estimate: 6 steps

- [x] ✅ **5.7 Add the changelog line** - Completed 2026-10-22
  - Description: Edit `CHANGELOG.md`: add one line under `## [Unreleased]`, `### Added`: the event structs, the handler slots, the event queue, and the capture, target, and bubble walk of `BrutalUi.Core`.
  - File(s): `CHANGELOG.md`
  - Estimate: 2 steps

- [x] ✅ **5.8 Run the build, the tests, the Linux script, and the prose lint, and record the results** - Completed 2026-10-22
  - Description: Run the build, the tests, and `scripts/test-linux.sh; echo "exit=$?"`. Run `python3 /home/aaronstatic/.claude/skills/asd-ste100/scripts/ste-lint.py --fail-over 2.5` on each changed doc and on this plan, and fix each hit. Record the results and the final test count in `context.md`.
  - File(s): `docs/technical-design.md`, `CHANGELOG.md`, `docs/features/input-routing/event-model/implementation.md`, `docs/features/input-routing/event-model/context.md`
  - Estimate: 6 steps

---

## Bugs & Issues

**Active Bugs:**
- none

**Fixed Bugs:**
- none

---

## Technical Debt

- none

---

## Progress Tracking

### Completed This Session (2026-10-22)
- ✅ Phases 1 to 5 (43 tasks). Each phase verifier gave PASS and Demo: PASS.
- ✅ The cross-phase review fixes CR-1 to CR-7 (see `code-review.md`).

### Discovered New Tasks
- CR-1 to CR-7 of the cross-phase review, and the doc follow-up of CR-4: done on 2026-10-22. They live in `code-review.md`, outside the task count.

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
