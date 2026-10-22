# Event Model - Implementation Plan

**Epic:** input-routing (feature 1 of 4)
**Status:** Ready for Review
**Started:** 2026-10-22
**Target Completion:** TBD
**Last Updated:** 2026-10-22 09:44

---

## Quality Bar

**Target quality:** A core library feature. Each later feature of the epic, and the interactive-elements epic, sends its events through this walk. A wrong order, a missed stop, or a hidden allocation here breaks each consumer. The public API stays small, because a NuGet consumer and three sibling features build on it.

- **The order is exact:** Capture runs from the top of the path to the parent of the target. The target runs its capture slot, then its bubble slot. Bubble runs from the parent of the target to the top. A test asserts each call, in order, with its phase and its current target.
- **The stop is exact:** After a handler stops propagation, the walk calls no later handler. This holds also inside a multicast slot.
- **0 bytes after warm-up:** A push, a drain, and a walk of each kind allocate 0 bytes after warm-up. A gate measures them with a full queue, a stop, and a tree deeper than the start size of the path buffer. A planted allocation fails the gate.
- **No recursion:** The walk uses the path buffer on `UiContext`. No method of the walk calls itself.
- **Clean seams:** The caller sets the target. The drain is internal. pointer-routing, keyboard-focus, and routing-gate extend the drain and do not change the walk.
- **No GLFW:** `BrutalUi.Core` gets no package and no `using` of GLFW.

---

## Executive Summary

This feature adds the event model to `BrutalUi.Core`. Four public event structs (`PointerEvent`, `ScrollEvent`, `KeyEvent`, and `CharEvent`) each hold a `UiEventHeader` with the type, the timestamp, and the target. `Element` gets two handler slots for each of the seven kinds, for example `OnPointerDown` and `OnPointerDownCapture`. The slots live in a side object that the first registration creates. `UiContext` holds a fixed ring of tagged entries (default 256, from `CreateInfo.EventQueueCapacity`). A full ring drops the oldest entry and counts the drop. The internal `UiContext.DrainEvents()` pops each entry and sends it to one generic walk, `EventWalk.Dispatch<T>`. A static abstract member of an internal interface on each struct picks the slot for the kind and the phase. The walk copies the parent chain into a path buffer on `UiContext`, then runs capture, target, and bubble with a stop check after each handler. An allocation gate proves 0 bytes after warm-up. No GLFW code is in this feature. Unit tests drive the walk with hand-made events.

---

## User Personas Alignment

No `docs/user-personas.md` file exists. The personas below come from `docs/mission-statement.md` and the epic docs.

**Target Users:**
- [x] **The game developer** (registers a handler on an element at build time and gets a `ref` event in capture, target, or bubble order)
- [x] **The sibling feature agents** (pointer-routing pushes pointer and scroll events and sets the target from the hit test. keyboard-focus routes key and char events to the focus owner. routing-gate calls the drain from `Update`)
- [x] **The interactive-elements epic** (`Button`, `Toggle`, `ScrollView`, and `TextField` register on the slots)
- [x] **The maintainer** (runs one more 0-byte gate)
- [ ] **The markup author** (not served. The XML loader binds handlers in a later epic)

**User Subsets Served:**
- [x] **Simple** - One bubble slot per kind: `leaf.OnPointerDown = OnClick;`.
- [x] **Complex** - A capture slot per kind, stop propagation, and the phase and the current target in the header.

**UX Approach:**
- A handler is a `UiEventHandler<TEvent>`: `void Handler(ref PointerEvent e)`. It reads `e.Header.Phase` and `e.Header.CurrentTarget`, and it calls `e.Header.StopPropagation()`.
- A wrong push (a key type in a `PointerEvent`) throws `ArgumentException` with the type and the struct name in the message.

---

## Goals

### Primary Goals
1. Give Core the event structs, the handler slots, the ring queue, and one generic walk (the requirements).
2. Run capture, target, and bubble in exact order, with stop propagation.
3. Allocate 0 bytes in a push, a drain, and a walk after warm-up.
4. Leave the target, the drain, and the input pass to the siblings.

### Success Criteria
- [ ] The confirmed demo test passes: a pointer down at the leaf of a 3-deep tree walks capture, target, and bubble.
- [ ] A stop in any phase ends the walk, also inside a multicast slot.
- [ ] Each of the seven kinds reaches its own two slots only, and its payload survives the ring.
- [ ] A full ring drops the oldest entry and counts the drop.
- [ ] `EventAllocationTests` measures 0 bytes over 100 steps, and a planted allocation fails it.
- [ ] `BrutalUi.Core` has no GLFW reference.

---

## Architecture Overview

### File Tree (end state)

```
src/BrutalUi.Core/
├── Input/                                  # new folder, namespace BrutalUi.Core
│   ├── UiEventEnums.cs                     # phase 1: UiEventType, UiEventPhase, PointerButton, UiModifiers
│   ├── UiEventHeader.cs                    # phase 1, phase 2 adds the stop
│   ├── UiEventHandler.cs                   # phase 1: the public delegate and the internal IUiEvent<TSelf>
│   ├── PointerEvent.cs                     # phase 1
│   ├── ScrollEvent.cs                      # phase 3
│   ├── KeyEvent.cs                         # phase 3
│   ├── CharEvent.cs                        # phase 3
│   ├── ElementHandlers.cs                  # phase 1 (6 slots), phase 3 (14 slots)
│   ├── EventQueue.cs                       # phase 1: the payloads, UiQueuedEvent, and the ring struct
│   └── EventWalk.cs                        # phase 1: the generic walk over the path buffer
├── Tree/Element.cs                         # phase 1 and 3: the side object field and 14 slot properties
└── Tree/UiContext.cs                       # phase 1, 3, and 4: the ring, the path buffer, the push, the drain
tests/BrutalUi.Core.Tests/Input/
├── EventRecorder.cs                        # phase 1: a fixed-array recorder of handler calls
├── EventWalkTests.cs                       # phase 1 and 2
├── EventKindTests.cs                       # phase 3
├── EventQueueTests.cs                      # phase 4
├── EventWalkEdgeTests.cs                   # phase 4
└── EventAllocationTests.cs                 # phase 5
docs/technical-design.md                    # phase 5: sections 5 (Events), 9, and 14
CHANGELOG.md                                # phase 5: one line under Unreleased
```

### Public API (end state)

The siblings and the interactive-elements epic build on these signatures. Each public member gets an XML doc comment (CS1591 is an error).

```csharp
namespace BrutalUi.Core;

public enum UiEventType { None, PointerMove, PointerDown, PointerUp, Scroll, KeyDown, KeyUp, Char }
public enum UiEventPhase { None, Capture, Target, Bubble }
public enum PointerButton { Left = 0, Right = 1, Middle = 2 }   // the GLFW values, so the adapter casts
[Flags] public enum UiModifiers { None = 0, Shift = 1, Control = 2, Alt = 4, Super = 8, CapsLock = 16, NumLock = 32 }

public struct UiEventHeader
{
    public UiEventHeader(UiEventType type, double timestamp, Element? target);
    public UiEventType Type;
    public double Timestamp;                    // seconds, from the caller (A6)
    public Element? Target;                     // set by the caller (A4)
    public UiEventPhase Phase { get; internal set; }          // the walk writes it before each call
    public Element? CurrentTarget { get; internal set; }      // the walk writes it before each call
    public bool IsPropagationStopped { get; private set; }    // phase 2
    public void StopPropagation();                            // phase 2
}

public struct PointerEvent { public UiEventHeader Header; public float2 Position; public PointerButton Button; public UiModifiers Modifiers; }
public struct ScrollEvent  { public UiEventHeader Header; public float2 Position; public float2 Delta; public UiModifiers Modifiers; }
public struct KeyEvent     { public UiEventHeader Header; public int Key; public int ScanCode; public UiModifiers Modifiers; public bool IsRepeat; }
public struct CharEvent    { public UiEventHeader Header; public uint CodePoint; }

public delegate void UiEventHandler<TEvent>(ref TEvent e) where TEvent : struct;

public class Element
{
    public UiEventHandler<PointerEvent>? OnPointerMove { get; set; }   // and OnPointerMoveCapture
    public UiEventHandler<PointerEvent>? OnPointerDown { get; set; }   // and OnPointerDownCapture
    public UiEventHandler<PointerEvent>? OnPointerUp { get; set; }     // and OnPointerUpCapture
    public UiEventHandler<ScrollEvent>? OnScroll { get; set; }         // and OnScrollCapture
    public UiEventHandler<KeyEvent>? OnKeyDown { get; set; }           // and OnKeyDownCapture
    public UiEventHandler<KeyEvent>? OnKeyUp { get; set; }             // and OnKeyUpCapture
    public UiEventHandler<CharEvent>? OnChar { get; set; }             // and OnCharCapture
}

public sealed class UiContext
{
    public struct CreateInfo { /* existing members */ public int EventQueueCapacity; }   // 0 gives 256
    public const int DefaultEventQueueCapacity = 256;
    public int EventQueueCapacity { get; }      // phase 4
    public int QueuedEventCount { get; }
    public long DroppedEventCount { get; }      // phase 4
    public void PushEvent(in PointerEvent e);
    public void PushEvent(in ScrollEvent e);    // phase 3
    public void PushEvent(in KeyEvent e);       // phase 3
    public void PushEvent(in CharEvent e);      // phase 3
    internal int DrainEvents();                 // A5: routing-gate calls it from Update
}
```

The positions are in viewport pixels, the space of the draw walk. pointer-routing converts the GLFW cursor position into this space. `KeyEvent.Key` holds the GLFW key code value (see Notes Q1). `CharEvent.CodePoint` holds a Unicode code point. The adapter checks it.

### Internal Types

- `IUiEvent<TSelf> where TSelf : struct, IUiEvent<TSelf>`: an internal interface with five static abstract members. Each event struct implements it explicitly, so the members do not show in the public API.
  - `static abstract ref UiEventHeader HeaderOf(ref TSelf e)`: gives the header by reference.
  - `static abstract bool Accepts(UiEventType type)`: `PointerEvent` accepts the three pointer types, `KeyEvent` the two key types, and the others one type each.
  - `static abstract UiEventHandler<TSelf>? GetHandler(ElementHandlers handlers, UiEventType type, bool capture)`: picks the slot. This member is the "static abstract interface member" of the chosen approach.
  - `static abstract void Store(in TSelf e, ref UiEventPayload payload)` and `static abstract TSelf Load(in UiEventHeader header, in UiEventPayload payload)`: copy the kind fields into and out of the ring entry.
- `ElementHandlers`: an internal sealed class with one field per slot (14 at the end). `Element` holds one `ElementHandlers?` field (A2).
- `UiEventPayload`: an internal struct with `[StructLayout(LayoutKind.Explicit)]`. It overlaps four internal unmanaged payload structs at offset 0: `PointerPayload`, `ScrollPayload`, `KeyPayload`, and `CharPayload`. It holds no reference, so the overlap is legal.
- `UiQueuedEvent`: an internal struct with `UiEventHeader Header` and `UiEventPayload Payload`. The header type is the tag. An entry is about 64 bytes, so the default ring is about 16 KB for each context.
- `EventQueue`: an internal mutable struct (the `ElementStorage` pattern: keep it in a field). It holds the `UiQueuedEvent[]` ring, the head, the count, and the drop count. `UiContext` creates the array at construction, so a push never allocates.
- `EventWalk`: an internal static class (the `PaintWalk` pattern). `InitialPathSize = 32`. `Dispatch<T>(ref T e, ref Element[] path) where T : struct, IUiEvent<T>`.

### Data Flow

```
caller (test now, BrutalUi.Glfw adapter in pointer-routing)
   │  UiContext.PushEvent(in PointerEvent | ScrollEvent | KeyEvent | CharEvent)
   │     ├─ check: T.Accepts(Header.Type), else ArgumentException          (phase 4)
   │     └─ T.Store ──► EventQueue ring (full: drop the oldest, count it)
   ▼
UiContext.DrainEvents()   (internal, A5. routing-gate calls it from Update before the style pass)
   │  guard: no nested drain (phase 4)
   │  n = count at the start. Events that a handler pushes wait for the next drain (phase 4)
   │  for each of n entries: pop (clear the slot) ──► skip when Target is null (A4)
   │                                             └─► skip when the top of the chain is not a root of this context (phase 4)
   │  switch on Header.Type ──► DispatchQueued<PointerEvent | ScrollEvent | KeyEvent | CharEvent>
   ▼
EventWalk.Dispatch<T>(ref T e, ref Element[] path)
   1. Count the parent chain of the target (n). Grow the path to max(n, 2 x length) when it is short (A7).
   2. Fill path[0] = top ... path[n-1] = target (A3: the path does not change during the walk).
   3. Capture: i = 0 .. n-2, the capture slot of path[i], phase Capture.
   4. Target: the capture slot, then the bubble slot of path[n-1], phase Target.
   5. Bubble: i = n-2 .. 0, the bubble slot of path[i], phase Bubble.
   Before each call: write Phase and CurrentTarget. After each delegate of the invocation list: check the stop.
   finally: clear path[0 .. n-1], so the buffer keeps no element reference (phase 4).
```

### The Seams for the Siblings

| Sibling | The seam | What the sibling adds |
|---------|----------|-----------------------|
| pointer-routing | `PushEvent(in PointerEvent)`, `PushEvent(in ScrollEvent)`, a public writable `Header.Target` | The adapter, and the hit test that sets the target before the null-target skip in `DrainEvents` |
| keyboard-focus | `PushEvent(in KeyEvent)`, `PushEvent(in CharEvent)`, the same skip step | The focus owner as the target of each key and char entry at drain time |
| routing-gate | `internal int DrainEvents()` | The call from `Update` before the style pass, and the full-queue case in `ThousandElementAllocationTests` |

The siblings change the target step of `DrainEvents` only. They do not change `EventWalk`.

---

## Implementation Phases

Each phase is a slice: one narrow path that works end to end, with a demo line that a person can check. Section 11 of `.beast-mode/phase-handoff.md` holds the rules. The Confirmed Assumptions name the demo of phase 1 (Q4).

Each phase ends with `dotnet build brutal-ui.sln 2>&1 | tail -n 30` and `dotnet test --solution brutal-ui.sln 2>&1 | tail -n 30` (tool rule 5). Expect `0 Warning(s)`, `0 Error(s)`, and 0 failed. The projects have no implicit usings, so each new file holds its own `using` lines. Each public member needs an XML doc comment. Use the file-scoped `namespace BrutalUi.Core;` in `src/` and `namespace BrutalUi.Core.Tests;` in the tests. Prose in comments follows ASD-STE100.

### Phase 1: The Pointer Walk
**Goal:** The smallest path that proves the idea: a pointer event with a target goes into the ring, the drain pops it, and the generic walk calls the pointer slots in capture, target, and bubble order.
**Demo:** a unit test pushes a pointer down at the leaf of a 3-deep tree (root, middle, leaf), drains the queue, and asserts capture (root, middle), target (leaf), and bubble (middle, root). The developer runs `dotnet run --project tests/BrutalUi.Core.Tests/BrutalUi.Core.Tests.csproj -- --filter-class '*EventWalkTests'` and reads a total of 5 with `failed: 0`.
**Needs:** none
**Agent tier:** ADVANCED (opus)

**Tier note:** This phase sets the public API of the epic and touches the two central classes, `Element` and `UiContext`. It uses a C# 11 feature that the codebase does not use yet: static abstract members of an internal interface, implemented explicitly on public structs. Check three points with the compiler: a public struct can implement the internal interface, `HeaderOf` returns a `ref` to a field of its `ref` parameter, and the generic walk calls `T.GetHandler` with no boxing. Do not make the interface public, and do not add a public generic `PushEvent<T>`.

**Tasks:**

Group A (the types, 6 new files in `src/BrutalUi.Core/Input/`):
- [ ] 1.1 Write `UiEventEnums.cs`: `UiEventType`, `UiEventPhase`, `PointerButton`, and `[Flags] UiModifiers` with the values of the Public API section. The doc comment of `PointerButton` and `UiModifiers` states that the values equal the GLFW values, so the adapter casts with no table. `UiEventType` has no enter and no leave member (A1).
- [ ] 1.2 Write `UiEventHeader.cs`: the struct with `Type`, `Timestamp` (`double`, seconds, A6), and `Target` as public fields, the constructor `(UiEventType type, double timestamp, Element? target)`, and `Phase` and `CurrentTarget` as properties with an `internal set`. The remarks state: the caller sets the target (A4), the walk writes `Phase` and `CurrentTarget` before each call, and a change of `Target` during the walk does not change the path (A3).
- [ ] 1.3 Write `UiEventHandler.cs`: the public delegate `UiEventHandler<TEvent>(ref TEvent e) where TEvent : struct`, and the internal interface `IUiEvent<TSelf>` with the five static abstract members of the section "Internal Types". The remarks of the delegate state: register at build time, not in the frame loop. A delegate with a closure allocates when the code creates it, not when the walk calls it.
- [ ] 1.4 Write `PointerEvent.cs`: the public struct with `Header`, `Position` (`float2`), `Button`, and `Modifiers`. Implement `IUiEvent<PointerEvent>` explicitly. `Accepts` gives true for `PointerMove`, `PointerDown`, and `PointerUp`. `GetHandler` switches on the type and the capture flag to the six pointer slots, and gives null for another type. `Store` and `Load` copy the three fields through `UiEventPayload.Pointer`.
- [ ] 1.5 Write `ElementHandlers.cs`: an `internal sealed class` with six fields: `PointerMove`, `PointerMoveCapture`, `PointerDown`, `PointerDownCapture`, `PointerUp`, and `PointerUpCapture`. The remarks cite A2.
- [ ] 1.6 Write `EventQueue.cs`: the internal `PointerPayload` struct, the explicit-layout `UiEventPayload` with `[FieldOffset(0)] PointerPayload Pointer`, the `UiQueuedEvent` struct, and the `EventQueue` struct. `EventQueue(int capacity)` creates the array. `Push(in UiQueuedEvent entry)` writes at `(head + count) % capacity`. A full ring moves the head by one, overwrites the oldest slot, and adds 1 to `Dropped` (a `long`). `TryPop(out UiQueuedEvent entry)` copies the head entry, writes `default` into the slot (no element reference stays), and moves the head. Members: `Count`, `Capacity`, `Dropped`. The remarks state the `ElementStorage` rule: keep the struct in a field, and change it through the field only.

Group B (the tree, the walk, and the first test, 6 files):
- [ ] 1.7 Edit `src/BrutalUi.Core/Tree/Element.cs`: add `private ElementHandlers? _handlers;`, an `internal ElementHandlers? Handlers => _handlers;`, and the six pointer properties. A getter reads `_handlers?.<Slot>`. A setter returns at once for a `null` value with no side object. Else it writes `(_handlers ??= new ElementHandlers()).<Slot> = value`. The remarks state: the first registration creates the side object (A2). An element with no handler holds one null reference. `+=` combines delegates into a multicast slot, and the walk calls each delegate in the order of add.
- [ ] 1.8 Write `src/BrutalUi.Core/Input/EventWalk.cs`: `internal static class EventWalk` with `internal const int InitialPathSize = 32` and `Dispatch<T>(ref T e, ref Element[] path) where T : struct, IUiEvent<T>`, per the steps of the section "Data Flow" (steps 1 to 5). Take `ref UiEventHeader header = ref T.HeaderOf(ref e)`. For each element, read `element.Handlers`. When it is null, go to the next element. Invoke the slot with `handler(ref e)`. No method of the class calls itself. The class remarks follow the `PaintWalk` remarks: no recursion, the caller owns the buffer, the buffer grows by doubling and never shrinks (A7).
- [ ] 1.9 Edit `src/BrutalUi.Core/Tree/UiContext.cs`: add `CreateInfo.EventQueueCapacity` (`int`, 0 gives the default), `public const int DefaultEventQueueCapacity = 256`, the fields `private EventQueue _events;` and `private Element[] _eventPath = new Element[EventWalk.InitialPathSize];`, the ring in both constructors, `public int QueuedEventCount`, `public void PushEvent(in PointerEvent e)`, and `internal int DrainEvents()`. The push builds a `UiQueuedEvent` with the header (with `Phase` `None` and `CurrentTarget` null) and `T.Store`, through a private generic `Enqueue<T>`. The drain reads `n = Count` at the start, pops `n` entries, skips an entry with a null target (A4), switches on the type to a private generic `DispatchQueued<T>(in UiQueuedEvent entry)` (`T e = T.Load(...)`, then `EventWalk.Dispatch(ref e, ref _eventPath)`), and returns the number of walks. The remarks of `DrainEvents` cite A5: routing-gate calls it from `Update`, and `Update` does not call it in this feature.
- [ ] 1.10 Write `tests/BrutalUi.Core.Tests/Input/EventRecorder.cs`: an `internal sealed class EventRecorder` with a fixed `EventRecord[]` of 256 entries (`Element? CurrentTarget`, `UiEventPhase Phase`, `UiEventType Type`, `bool CaptureSlot`), `Count`, `Reset()`, `this[int]`, and generic methods `Capture<T>(ref T e)` and `Bubble<T>(ref T e)` with `where T : struct, IUiEvent<T>` (the test project reads internals). A full array stops the record and sets an `Overflow` flag, so the recorder never allocates after its construction. Add a helper `Register(Element element)` that sets the six pointer slots of the element to the two methods, with the delegates created one time in the constructor.
- [ ] 1.11 Write `tests/BrutalUi.Core.Tests/Input/EventWalkTests.cs` with 5 tests: `PointerDownWalksCaptureTargetBubble` (the confirmed demo: 6 records, in order: root capture, middle capture, leaf target capture slot, leaf target bubble slot, middle bubble, root bubble, each with its current target), `ARootTargetRunsTheTargetPhaseOnly`, `AnElementWithNoHandlerHasNoSideObject` (reads `Handlers` before and after a null set), `EachPointerTypeReachesItsOwnSlots` (a move calls no down slot), and `DrainReturnsTheWalkCountAndEmptiesTheQueue`.
- [ ] 1.12 Run the build and the tests. Run the demo command. Record the test count before and after the phase, and the size of `UiQueuedEvent` (`Unsafe.SizeOf<UiQueuedEvent>()` in a scratch test, not committed), in `docs/features/input-routing/event-model/context.md`.

**Estimated Steps:** about 72 (12 tasks, about six steps for one task)

**Files (12):** Group A: the 6 new files in `Input/`. Group B: `Element.cs`, `EventWalk.cs`, `UiContext.cs`, `EventRecorder.cs`, `EventWalkTests.cs`, `context.md`.

**Acceptance Criteria:**
- [ ] The 5 tests of `EventWalkTests` pass. The demo test asserts six records with the phase, the slot, and the current target of each.
- [ ] `IUiEvent<TSelf>` and `ElementHandlers` are internal. No public member names them.
- [ ] No method of `EventWalk` calls itself.
- [ ] `grep -rn 'using .*Glfw' src/BrutalUi.Core` gives no output.

---

### Phase 2: Stop Propagation
**Goal:** A handler stops the walk. The walk calls no later handler, also inside a multicast slot.
**Demo:** a unit test registers a capture handler on the middle element that calls `e.Header.StopPropagation()`. After the drain, the recorder holds root capture and middle capture only, and a second queued event still walks all six calls. The developer runs `dotnet run --project tests/BrutalUi.Core.Tests/BrutalUi.Core.Tests.csproj -- --filter-class '*EventWalkTests'` and reads a total of 10 with `failed: 0`.
**Needs:** Phase 1
**Agent tier:** STANDARD (sonnet)

**Tasks:**
- [ ] 2.1 Edit `UiEventHeader.cs`: add `public bool IsPropagationStopped { get; private set; }` and `public void StopPropagation()`. The remarks state: the walk calls no later handler, also no later delegate of the same multicast slot. A call through a copy of the event has no effect, so call it on the `ref` parameter.
- [ ] 2.2 Edit `EventQueue.cs` or the `Enqueue<T>` step of `UiContext.cs`: a push writes the stop flag as false, so a reused struct of the caller does not arrive stopped. Add an internal `ResetWalkState()` on the header for this, and call it in the push.
- [ ] 2.3 Edit `EventWalk.cs`: invoke each slot through `foreach (UiEventHandler<T> one in Delegate.EnumerateInvocationList(handler))`. Check `header.IsPropagationStopped` after each delegate, and return at once when it is true. The enumerator is a struct and allocates nothing (.NET 9 and later, see Decision 5). Keep one private generic `InvokeSlot<T>` helper that returns the stop flag, so the three phases share it.
- [ ] 2.4 Extend `EventRecorder` with a `StopAt` setting (an element, a phase, and a slot) and add 5 tests to `EventWalkTests.cs`: `AStopInCaptureEndsTheWalk`, `AStopInTheTargetCaptureSlotSkipsTheTargetBubbleSlot`, `AStopInBubbleSkipsTheAncestors`, `AStopInAMulticastSlotSkipsTheNextDelegate` (two delegates on one slot, the first stops), and `AStopAppliesToOneEventOnly` (two queued events, the first stops, the second walks six calls).
- [ ] 2.5 Run the build and the tests. Run the demo command. Record the result in `context.md`.

**Estimated Steps:** about 30 (5 tasks, about six steps for one task)

**Files (6):** `UiEventHeader.cs`, `EventQueue.cs` or `UiContext.cs`, `EventWalk.cs`, `EventRecorder.cs`, `EventWalkTests.cs`, `context.md`

**Acceptance Criteria:**
- [ ] The 10 tests of `EventWalkTests` pass.
- [ ] `EventWalk.cs` calls no `GetInvocationList`.
- [ ] A stop inside a multicast slot skips the next delegate of that slot.

---

### Phase 3: The Scroll, Key, and Char Kinds
**Goal:** The other three structs reach their own slots through the same walk, and each payload survives the ring.
**Demo:** a unit test pushes a key down at the leaf of the 3-deep tree and reads the six calls of `OnKeyDownCapture` and `OnKeyDown` in capture, target, and bubble order. The developer runs `dotnet run --project tests/BrutalUi.Core.Tests/BrutalUi.Core.Tests.csproj -- --filter-class '*EventKindTests'` and reads a total of 12 with `failed: 0`.
**Needs:** Phase 2
**Agent tier:** STANDARD (sonnet)

**Tasks:**

Group A (the types and the slots, 6 files):
- [ ] 3.1 Write `src/BrutalUi.Core/Input/ScrollEvent.cs`: `Header`, `Position` (`float2`, for the hit test of pointer-routing), `Delta` (`float2`), and `Modifiers`. `Accepts` gives true for `Scroll` only. Implement the interface as task 1.4 does.
- [ ] 3.2 Write `KeyEvent.cs`: `Header`, `Key` (`int`, the GLFW key code value, Notes Q1), `ScanCode` (`int`), `Modifiers`, and `IsRepeat` (`bool`). `Accepts` gives true for `KeyDown` and `KeyUp`. The doc comment states that the adapter sends a GLFW repeat action as a `KeyDown` with `IsRepeat` true.
- [ ] 3.3 Write `CharEvent.cs`: `Header` and `CodePoint` (`uint`, a Unicode scalar value). `Accepts` gives true for `Char` only. The remarks state: GLFW gives no modifiers with a char, so the struct has none (Notes Q3).
- [ ] 3.4 Edit `ElementHandlers.cs`: add the 8 fields `Scroll`, `ScrollCapture`, `KeyDown`, `KeyDownCapture`, `KeyUp`, `KeyUpCapture`, `Char`, and `CharCapture`.
- [ ] 3.5 Edit `Element.cs`: add the 8 properties `OnScroll`, `OnScrollCapture`, `OnKeyDown`, `OnKeyDownCapture`, `OnKeyUp`, `OnKeyUpCapture`, `OnChar`, and `OnCharCapture`, with the setter rule of task 1.7.
- [ ] 3.6 Edit `EventQueue.cs`: add the internal unmanaged `ScrollPayload`, `KeyPayload`, and `CharPayload`, each at `[FieldOffset(0)]` in `UiEventPayload`.

Group B (the push, the drain, and the tests, 4 files):
- [ ] 3.7 Edit `UiContext.cs`: add `PushEvent(in ScrollEvent e)`, `PushEvent(in KeyEvent e)`, and `PushEvent(in CharEvent e)` through `Enqueue<T>`. Extend the switch of `DrainEvents`: `Scroll` goes to `DispatchQueued<ScrollEvent>`, `KeyDown` and `KeyUp` go to `DispatchQueued<KeyEvent>`, and `Char` goes to `DispatchQueued<CharEvent>`.
- [ ] 3.8 Edit `EventRecorder.cs`: `Register(Element)` sets all 14 slots. Add a copy of the last payload of each kind (`LastPointer`, `LastScroll`, `LastKey`, `LastChar`), written in the target phase.
- [ ] 3.9 Write `tests/BrutalUi.Core.Tests/Input/EventKindTests.cs` with 12 test cases: the theory `EachKindReachesOnlyItsOwnSlots` over the 7 types (7 cases: 6 records, each of the pushed type and of its two slots), `PointerPayloadSurvivesTheQueue`, `ScrollPayloadSurvivesTheQueue`, `KeyPayloadSurvivesTheQueue`, `CharPayloadSurvivesTheQueue` (each field, the timestamp, and the target), and `AKeyDownWalksCaptureTargetBubble` (the demo).
- [ ] 3.10 Run the build and the tests. Run the demo command. Record the result in `context.md`.

**Estimated Steps:** about 60 (10 tasks, about six steps for one task)

**Files (10):** Group A: `ScrollEvent.cs`, `KeyEvent.cs`, `CharEvent.cs`, `ElementHandlers.cs`, `Element.cs`, `EventQueue.cs`. Group B: `UiContext.cs`, `EventRecorder.cs`, `EventKindTests.cs`, `context.md`.

**Acceptance Criteria:**
- [ ] The 12 test cases of `EventKindTests` pass, and the 10 tests of `EventWalkTests` still pass.
- [ ] `EventWalk.cs` does not change in this phase (`git diff` of the phase shows no line of it).
- [ ] Each payload struct holds no reference type.

---

### Phase 4: The Full Queue and the Edge Rules
**Goal:** The ring drops the oldest entry and counts it, a bad push throws, and the drain stays correct when a handler pushes, removes, throws, or calls the drain again.
**Demo:** a unit test creates a context with `EventQueueCapacity = 4`, pushes 6 pointer events, and reads `QueuedEventCount` 4 and `DroppedEventCount` 2. The drain walks events 3 to 6 in order. The developer runs `dotnet run --project tests/BrutalUi.Core.Tests/BrutalUi.Core.Tests.csproj -- --filter-class '*EventQueueTests'` and reads a total of 8 with `failed: 0`.
**Needs:** Phase 3
**Agent tier:** STANDARD (sonnet)

**Tasks:**

Group A (the rules, 3 files):
- [ ] 4.1 Edit `UiContext.cs`: add `public int EventQueueCapacity` and `public long DroppedEventCount`. In `UiContext(in CreateInfo)`, throw `ArgumentOutOfRangeException` for a negative `EventQueueCapacity`, with a message that names the member and the default. 0 gives `DefaultEventQueueCapacity`.
- [ ] 4.2 Edit `Enqueue<T>` in `UiContext.cs`: when `T.Accepts(e.Header.Type)` is false, throw `ArgumentException` that names the type and the struct, for example "A PointerEvent cannot carry the type KeyDown. Use a KeyEvent." `UiEventType.None` also throws. The throw allocates on the error path only.
- [ ] 4.3 Edit `DrainEvents` in `UiContext.cs`: add a `_draining` flag. A nested call throws `InvalidOperationException` ("A drain of this UiContext runs already. Do not call Update or the drain from an event handler."), as `Draw` does. Set the flag in a `try` and clear it in the `finally`. Keep the count at the start: an event that a handler pushes waits for the next drain. Skip an entry when the top of the parent chain of its target is not a root of this context (`top.IsRoot` and `_roots.IndexOf(top) >= 0`, Decision 7). Keep the target step in one place, with a comment that pointer-routing and keyboard-focus set the target there.
- [ ] 4.4 Edit `EventWalk.cs`: clear `path[0 .. n-1]` in a `finally`, so the buffer keeps no element reference after a walk or an exception.

Group B (the tests, 3 files):
- [ ] 4.5 Write `tests/BrutalUi.Core.Tests/Input/EventQueueTests.cs`, part 1 (the ring): `AFullQueueDropsTheOldestEvent` (the demo), `TheDropCountGrowsForEachDrop`, `TheDefaultCapacityIs256` (both constructors), and `ACapacityFromTheCreateInfoSizesTheRing`. Use `new UiContext(new UiContext.CreateInfo { Measurer = new RecordingRenderer(), EventQueueCapacity = 4 })`.
- [ ] 4.6 Add part 2 (the checks) to `EventQueueTests.cs`: `ANegativeCapacityThrows`, `APushWithAWrongTypeThrows` (each struct with a type of another struct, and `None`), `ADrainSkipsAnEventWithNoTarget`, and `AnEventFromAHandlerWaitsForTheNextDrain`. The class holds 8 tests.
- [ ] 4.7 Write `tests/BrutalUi.Core.Tests/Input/EventWalkEdgeTests.cs` with 5 tests: `ADrainSkipsATargetOutsideTheTree` (a removed subtree and a target in another context), `ATreeChangeDuringTheWalkKeepsThePath` (the root capture handler removes the middle element, and the middle and leaf handlers still run, A3), `ATreeDeeperThanThePathBufferWalksInOrder` (a chain of 40 levels, 80 capture and bubble records plus 2 target records, in order), `ANestedDrainThrows`, and `AHandlerExceptionLeavesTheQueueAndThePathClean` (the exception leaves the drain, the rest of the queue stays, the next drain runs, and each slot of the path buffer is null).
- [ ] 4.8 Run the build and the tests. Run the demo command and the class `*EventWalkEdgeTests`. Record the results in `context.md`.

**Estimated Steps:** about 48 (8 tasks, about six steps for one task)

**Files (6):** Group A: `UiContext.cs`, `EventWalk.cs` (and `EventQueue.cs` when the counters need a member). Group B: `EventQueueTests.cs`, `EventWalkEdgeTests.cs`, `context.md`.

**Acceptance Criteria:**
- [ ] The 8 tests of `EventQueueTests` and the 5 tests of `EventWalkEdgeTests` pass.
- [ ] After a walk and after a handler exception, the path buffer and the popped ring slots hold no element reference.
- [ ] A nested drain throws, and the outer drain still clears its flag.

---

### Phase 5: The Allocation Gate and the Record
**Goal:** Prove 0 bytes for a push, a drain, and a walk of each kind after warm-up. Record the model in the technical design and the changelog.
**Demo:** the developer runs `dotnet run --project tests/BrutalUi.Core.Tests/BrutalUi.Core.Tests.csproj -- --filter-class '*EventAllocationTests'` and reads a total of 3 with `failed: 0`. A planted allocation at the start of `EventWalk.Dispatch` makes `dotnet test --solution brutal-ui.sln` exit non-zero.
**Needs:** Phase 4
**Agent tier:** STANDARD (sonnet)

**Tier note:** When the gate measures bytes above 0, find the cause before a change: run the body with one kind at a time in a scratch copy of the test. Expected suspects are a boxing call through an interface in place of the constraint, a delegate created in the body, or a `foreach` over an interface. Fix the cause in `src/` with no public API change. Do not add warm-up rounds to hide a growth. When the cause is in the runtime, stop and report to the main thread.

**Tasks:**

Group A (the gate, 2 files):
- [ ] 5.1 Write `tests/BrutalUi.Core.Tests/Input/EventAllocationTests.cs` in the pattern of `TreeAllocationTests`: a private `Scene` with a context of `EventQueueCapacity = 8` (and a `RecordingRenderer` measurer), a chain of 40 levels under one root (deeper than `InitialPathSize`, so the warm-up grows the path, A7), a side branch, and a counter object. The scene registers all 14 slots on each chain element with delegates that the build creates one time, puts a second delegate on one slot (a multicast slot), and puts a stop handler on the `KeyUp` capture slot of level 20. Build the 10 events of the body before the measure: each of the 7 types at the deepest leaf, one pointer down at the side branch, one char with a null target, and one more pointer move, so the ring drops 2 entries in each step. `Step(Scene)` pushes the 10 events and calls `DrainEvents()`. It holds no lambda, no boxing, no string, and no `Assert`.
- [ ] 5.2 Add `[Fact] PushAndDrainAllocateNothingAfterWarmUp`: build the scene, run 2 steps (the warm-up), call `AllocationGate.ReleaseAllocationContext()`, read `GC.GetAllocatedBytesForCurrentThread()`, run 100 steps, read it again, and `Assert.Equal(0L, after - before)`.
- [ ] 5.3 Add `[Fact] APushToAFullQueueAllocatesNothing`: warm up, release, then measure 1000 pushes of one prepared pointer event into a full ring, and assert 0 bytes and 1000 more drops.
- [ ] 5.4 Add `[Fact] TheMeasuredBodyDoesRealWork`: after 10 steps, assert the counter calls for each step (the exact count from the scene: the full walks, the stopped `KeyUp` walk, the skip of the null target), `DroppedEventCount` of 20, and the path buffer length of 64 or more (internal read of a new `internal int EventPathLength` on `UiContext`, added for this test).
- [ ] 5.5 Prove that the gate blocks. Copy `src/BrutalUi.Core/Input/EventWalk.cs` to the session scratch folder. Add `GC.KeepAlive(new object());` as the first line of `Dispatch`. Run `dotnet test --solution brutal-ui.sln > /dev/null 2>&1; echo "exit=$?"` and expect a non-zero exit. Restore the file from the copy, run the tests again, and expect 0 failed. Record both exit codes and the names of the failed gates in `context.md`.

Group B (the record, 4 files):
- [ ] 5.6 Edit `docs/technical-design.md`. Section 5, "Events": replace the paragraph with the model of this feature: the header, the four structs, the 14 slots in a side object, the delegate at build time (a delegate is a method plus a target, so it fits the old rule), the ring, the internal drain, and the walk order with the stop rule. Section 9, "Flow": add the queue and the drain between steps 1 and 2. Section 14: add a bullet for the event tests and the gate `EventAllocationTests`, with the plant result of task 5.5.
- [ ] 5.7 Edit `CHANGELOG.md`: add one line under `## [Unreleased]`, `### Added`: the event structs, the handler slots, the event queue, and the capture, target, and bubble walk of `BrutalUi.Core`.
- [ ] 5.8 Run the build, the tests, and `scripts/test-linux.sh; echo "exit=$?"`. Run `python3 /home/aaronstatic/.claude/skills/asd-ste100/scripts/ste-lint.py --fail-over 2.5` on each changed doc and on this plan, and fix each hit. Record the results and the final test count in `context.md`.

**Estimated Steps:** about 48 (8 tasks, about six steps for one task)

**Files (6):** Group A: `EventAllocationTests.cs`, `UiContext.cs` (one internal member), and a temporary edit of `EventWalk.cs` (restored). Group B: `docs/technical-design.md`, `CHANGELOG.md`, `context.md`.

**Acceptance Criteria:**
- [ ] The 3 tests of `EventAllocationTests` pass with 0 bytes in 5 runs of the class.
- [ ] The plant gives a non-zero exit code. The restored file gives 0 failed.
- [ ] Sections 5, 9, and 14 of the technical design match the code.

---

**Total Estimated Steps:** about 258 (43 tasks over 5 phases)

---

## Key Technical Decisions

### Confirmed Assumptions

- Q1 Event struct shape: a `UiEventHeader` struct inside `PointerEvent`, `ScrollEvent`, `KeyEvent`, and `CharEvent`. The ring stores a tagged union.
- Q2 Capture versus bubble registration: each kind gets two slots on `Element`, for example `OnPointerDown` and `OnPointerDownCapture`.
- Q3 Ring size: the ring size comes from `UiContext.CreateInfo.EventQueueCapacity`, with a default of 256.
- Q4 Phase 1 demo: a unit test pushes a pointer event at a leaf of a 3-deep tree. It asserts capture, target, bubble.
- A1 The kinds are pointer move, pointer down, pointer up, scroll, key down, key up, and char. There is no enter or leave event. pointer-routing uses `PseudoStates.Hover` for that.
- A2 The handler slots live in a side object on `Element`. The side object allocates at the first registration, at build time. An element with no handler adds one null reference.
- A3 The walk copies the path into the path buffer at the start. A tree change during the walk does not change that path.
- A4 A queued event carries its target. The drain skips an event with no target. pointer-routing sets the target from the hit test.
- A5 The drain is an internal method on `UiContext`. routing-gate calls it from `Update`. Tests call it through `InternalsVisibleTo`.
- A6 The caller gives the timestamp as a `double` in seconds.
- A7 The path buffer grows to the deepest tree at warm-up. The 0-byte rule holds only after warm-up.

### Decision 1: One generic walk with a static abstract slot picker
**Choice:** `EventWalk.Dispatch<T>(ref T e, ref Element[] path) where T : struct, IUiEvent<T>`. The internal interface `IUiEvent<TSelf>` has static abstract members. `GetHandler` picks the slot from the type and the capture flag. `HeaderOf` gives the header by `ref`. Each struct implements the members explicitly.
**Rationale:** One loop holds the order and the stop rule, so one test set covers each kind. The JIT compiles one copy for each struct type, so a call through the constraint does not box. The explicit implementation keeps the interface out of the public API. The three pointer types share one struct, so `GetHandler` reads the type of the header.
**Alternatives:** (a) A walk for each kind: four copies of the same loop (rejected by the chosen approach). (b) A public interface: it exposes `Store` and `Load` to the consumer. (c) An instance interface method: the call on a struct in a generic context does not box either, but a static member needs no instance for `Accepts`.

### Decision 2: The ring entry is a header plus an explicit-layout payload
**Choice:** `UiQueuedEvent` holds `UiEventHeader` (with the target reference) and `UiEventPayload`. The payload overlaps four unmanaged payload structs at offset 0. The header type is the tag. `T.Store` and `T.Load` copy the fields.
**Rationale:** The CLR rejects an overlap of a reference field and a value field, so the reference stays in the header and outside the overlap. The entry stays about 64 bytes. The public structs keep flat fields, so a handler reads `e.Position` and not `e.Payload.Pointer.Position`.
**Alternatives:** (a) Four rings, one for each struct: the drain loses the order between kinds, for example a click and the next key. (b) A flat entry with each field of each kind: about 30 percent larger, and still a tagged entry. (c) A boxed `object` entry: it allocates.

### Decision 3: The slots are properties over a side object
**Choice:** 14 public properties on `Element` read and write fields of the internal `ElementHandlers`. A non-null set creates the side object. A null set with no side object does nothing.
**Rationale:** A2 asks for one null reference on an element with no handler. A property accepts `+=`, so a game can combine handlers at build time. A C# `event` hides the getter, and the walk needs the getter. The walk reads `Handlers` one time for each element and skips an element with no side object.
**Alternatives:** (a) 14 fields on `Element`: 112 bytes on each element, against A2. (b) A dictionary from the kind to the handler: a lookup in the walk.

### Decision 4: The walk order at the target
**Choice:** At the target, the walk calls the capture slot, then the bubble slot, both with the phase `Target`. The capture phase covers the ancestors only. The bubble phase covers the ancestors only.
**Rationale:** The requirement says "capture from the root to the target, then the target, then bubble from the target to the root". The current DOM order at the target is the same: the capture listeners first. A handler that reads `Phase` sees `Target` in both slots.
**Alternatives:** The bubble slot only at the target: a capture handler on a leaf never runs, and that surprises a consumer.

### Decision 5: A stop check after each delegate of a slot
**Choice:** The walk enumerates each slot with `Delegate.EnumerateInvocationList` and checks the stop flag after each delegate.
**Rationale:** The requirement says "The walk then calls no later handler." A plain `handler(ref e)` call of a multicast slot runs each delegate, also after a stop. `Delegate.EnumerateInvocationList` (.NET 9 and later) gives a struct enumerator and allocates nothing ([Microsoft Learn](https://learn.microsoft.com/en-us/dotnet/api/system.delegate.enumerateinvocationlist)). For a single delegate, it gives the delegate itself.
**Alternatives:** (a) `GetInvocationList`: it allocates an array for each call. (b) The DOM rule, in which the other listeners of the current element still run: it breaks the literal requirement.

### Decision 6: The drain works on a count from its start, and pops before the walk
**Choice:** `DrainEvents` reads the count one time. It pops each entry into a local and clears the slot before the walk. An event that a handler pushes waits for the next drain. A nested drain throws.
**Rationale:** A handler that pushes an event in each call cannot make a drain run forever. The pop before the walk keeps the ring correct when a handler pushes into a full ring. The throw protects the one path buffer, as the `_drawing` flag protects the paint stack.
**Alternatives:** (a) Drain until empty: an endless loop is possible. (b) A path buffer for each depth of nesting: no consumer needs a nested drain.

### Decision 7: The drain skips a target outside the tree of the context
**Choice:** The drain skips an entry when the top of the parent chain of the target is not a root of this context. The check reads `IsRoot` and then `_roots.IndexOf`.
**Rationale:** A handler of one event can remove a panel, and a queued event can target a child of that panel. A game expects no click on a closed panel. The roots are few, so the check is cheap. A4 names the null-target skip. This rule adds a second skip and does not contradict A4. Notes Q2 holds the open question.
**Alternatives:** Walk the detached chain, as the DOM does. The walk is then correct, but a closed dialog can still act on a click.

### Decision 8: The ring is created at construction
**Choice:** Both constructors create the `UiQueuedEvent[]` of the capacity. A push never allocates.
**Rationale:** "A push after warm-up allocates 0 bytes" then holds from the first push. The default ring is about 16 KB for each context, which is small next to a tree.
**Alternatives:** Create the ring at the first push: one branch in each push for a small saving.

### Decision 9: The siblings own the target
**Choice:** The push takes the target from the caller. The drain has one target step: the null skip and the root check. pointer-routing and keyboard-focus extend that step. They do not change `EventWalk`.
**Rationale:** A4 and the epic build order. The walk stays generic and tested.
**Alternatives:** A target resolver delegate on `UiContext` now: no consumer exists in this feature (YAGNI).

---

## Risks & Mitigation

| Risk | Likelihood | Impact | Mitigation |
|------|------------|--------|------------|
| A public struct with an explicit static implementation of an internal interface does not compile, or a member shows in the public API. | Low | High | Phase 1 is ADVANCED and checks it first. Fallback: make `IUiEvent<TSelf>` public with `[EditorBrowsable(Never)]` and record the reason in `context.md`. |
| A generic call boxes in a Debug build, so the gate measures bytes. | Low | High | The calls go through the constraint (`T.GetHandler`), not through a cast to the interface. Phase 5 has a tier note for the search. |
| `Delegate.EnumerateInvocationList` allocates in a corner case. | Low | Medium | The gate of phase 5 holds a multicast slot and single slots. On a failure, use `handler(ref e)` for a single-target delegate and record the result. |
| A handler holds a `ref` to the event after the call, or calls `StopPropagation` on a copy. | Medium | Low | The compiler stops a `ref` escape. The doc comment of `StopPropagation` names the copy trap. A test (task 2.4) stops through the `ref`. |
| Until routing-gate, nothing calls `DrainEvents` in a game, so the ring fills and drops events. | High | Low | Intended by the build order. The drop count shows it. pointer-routing and routing-gate wire the drain. |
| `KeyEvent.Key` as a GLFW key code ties Core to a GLFW table. | Medium | Medium | The value is a plain `int`. Notes Q1 offers a `UiKey` enum. keyboard-focus adds the named keys that it reads. |
| The detached-target check (Decision 7) costs too much with many roots. | Low | Low | The check reads `IsRoot` first, and a game has few roots. The gate measures it with one root. |
| A re-entrant `Update` from a handler, after routing-gate wires the drain, throws and surprises a game. | Low | Medium | The message names the cause and the fix. routing-gate documents the rule on `Update`. |

---

## Dependencies

### Internal Dependencies
- `core-tree/element-tree` (done): `Element`, `Element.Parent`, `IsRoot`, `ElementStorage.IndexOf`, `UiContext`, `UiContext.CreateInfo`, and `InternalsVisibleTo("BrutalUi.Core.Tests")`.
- `core-tree/allocation-guard` (done): `AllocationGate.ReleaseAllocationContext()` and the gate pattern of `TreeAllocationTests`.
- `imgui-renderer` (done): `RecordingRenderer` as the measurer of a test context with a set capacity.
- The `PaintWalk` pattern for the path buffer and the `_drawing` guard.

### External Dependencies
- .NET 10: `Delegate.EnumerateInvocationList` (.NET 9 and later) and static abstract interface members (C# 11 and later).
- `Brutal.Numerics` (`float2`), already pinned. No new package. No native dependency.

---

## Definition of Done

All criteria below must pass. An independent evaluator runs the Verification Method from `/home/aaronstatic/dev/brutal-ui` on Linux. "MANUAL" marks a criterion that needs the author on Windows.

### Functional Criteria
- [ ] F1. A pointer down at the leaf of a 3-deep tree calls, in order: root capture, middle capture, leaf capture slot, leaf bubble slot, middle bubble, root bubble. Each call sees the right `Phase` and `CurrentTarget` (`EventWalkTests.PointerDownWalksCaptureTargetBubble`).
- [ ] F2. A stop in capture, at the target, in bubble, or inside a multicast slot calls no later handler. The next event walks in full (5 stop tests).
- [ ] F3. Each of the 7 types reaches only its own two slots, and each payload field, the timestamp, and the target survive the ring (`EventKindTests`, 12 cases).
- [ ] F4. A ring of capacity 4 with 6 pushes holds the last 4 events in order and counts 2 drops. The default capacity is 256 for both constructors.
- [ ] F5. The drain skips a null target and a target outside the tree. An event from a handler waits for the next drain. A nested drain throws. A handler exception leaves the rest of the queue and an empty path buffer.
- [ ] F6. A tree change during the walk does not change the path. A 40-level chain walks in order.
- [ ] F7. A push with a type of another struct, or with `None`, throws `ArgumentException`. A negative capacity throws `ArgumentOutOfRangeException`.
- [ ] F8. `EventAllocationTests` measures 0 bytes over 100 steps of push and drain, and 0 bytes over 1000 pushes into a full ring.
- [ ] F9. A planted allocation in `EventWalk.Dispatch` gives a non-zero exit code of `dotnet test`.

### Quality Criteria
- [ ] Q1. `dotnet build brutal-ui.sln` gives `0 Warning(s)` and `0 Error(s)`.
- [ ] Q2. `dotnet test --solution brutal-ui.sln` gives 0 failed, with 38 more test cases in `BrutalUi.Core.Tests` than the count of task 1.12 (5 + 5 + 12 + 13 + 3).
- [ ] Q3. No method of `EventWalk` calls itself, and `EventWalk.cs` holds no `GetInvocationList`.
- [ ] Q4. `IUiEvent`, `ElementHandlers`, `UiEventPayload`, `UiQueuedEvent`, `EventQueue`, and `EventWalk` are internal.
- [ ] Q5. The measured `Step` of `EventAllocationTests` holds no lambda, no `$"`, no `string`, and no `Assert`.
- [ ] Q6. The changed docs use ASD-STE100 prose. The lint gives no hit above 2.5.
- [ ] Q7. (MANUAL) `scripts\test-windows.ps1` prints its PASS line on Windows.

### Integration Criteria
- [ ] I1. `BrutalUi.Core` has no GLFW `using` and holds 2 `PackageReference` items. `Directory.Packages.props` does not change.
- [ ] I2. `UiContext.Update` does not call `DrainEvents` (routing-gate wires it).
- [ ] I3. `ThousandElementAllocationTests.cs` does not change (routing-gate adds the queue case).
- [ ] I4. `DrainEvents` is `internal`, and `PushEvent` has four public overloads and no generic overload.
- [ ] I5. Sections 5, 9, and 14 of `docs/technical-design.md` and `CHANGELOG.md` name the model and the gate.
- [ ] I6. `scripts/test-linux.sh` prints its PASS line, and `git diff main -- scripts/` is empty.

### Verification Method

Run steps 1 to 9 from `/home/aaronstatic/dev/brutal-ui` on Linux. Step 10 is MANUAL.

1. `dotnet build brutal-ui.sln 2>&1 | tail -n 30`. Expect `0 Warning(s)` and `0 Error(s)` (Q1).
2. `dotnet test --solution brutal-ui.sln 2>&1 | tail -n 30`. Expect 0 failed, and a `BrutalUi.Core.Tests` total 38 above the count in `context.md` (Q2).
3. For each class, run `dotnet run --project tests/BrutalUi.Core.Tests/BrutalUi.Core.Tests.csproj -- --filter-class '*<Class>' 2>&1 | tail -n 5`. Expect `failed: 0` and these totals: `EventWalkTests` 10 (F1, F2), `EventKindTests` 12 (F3), `EventQueueTests` 8 (F4, F5, F7), `EventWalkEdgeTests` 5 (F5, F6), `EventAllocationTests` 3 (F8). Read the test names in each file and match them to the criteria.
4. Copy `src/BrutalUi.Core/Input/EventWalk.cs` to the scratch folder. Add `GC.KeepAlive(new object());` as the first line of `Dispatch`. Run `dotnet test --solution brutal-ui.sln > /dev/null 2>&1; echo "exit=$?"`. Expect a value that is not 0 (F9). Restore the file from the copy. Run step 2 again and expect 0 failed.
5. `grep -rn 'using .*Glfw' src/BrutalUi.Core; grep -c PackageReference src/BrutalUi.Core/BrutalUi.Core.csproj; git diff main --stat -- Directory.Packages.props tests/BrutalUi.Core.Tests/ThousandElementAllocationTests.cs scripts/`. Expect no grep line, `2`, and no diff (I1, I3, I6).
6. `grep -n 'DrainEvents' src/BrutalUi.Core/Tree/UiContext.cs`. Expect the declaration with `internal` and no call inside `Update` (I2, I4). `grep -n 'public void PushEvent' src/BrutalUi.Core/Tree/UiContext.cs`. Expect 4 lines, none generic (I4).
7. `grep -nE 'GetInvocationList|Dispatch<' src/BrutalUi.Core/Input/EventWalk.cs`. Expect the declaration of `Dispatch<T>` and no other `Dispatch` call inside the class, and no `GetInvocationList` (Q3). `grep -nE '^(public|internal).*(interface|class|struct)' src/BrutalUi.Core/Input/*.cs`. Expect the types of Q4 with `internal` (Q4). Read `Step` in `EventAllocationTests.cs` for Q5.
8. `scripts/test-linux.sh; echo "exit=$?"`. Expect the PASS line and `exit=0` (I6).
9. Read sections 5, 9, and 14 of `docs/technical-design.md` and the `## [Unreleased]` part of `CHANGELOG.md` (I5). Run `python3 /home/aaronstatic/.claude/skills/asd-ste100/scripts/ste-lint.py --fail-over 2.5` on each changed doc (Q6).
10. (MANUAL) On Windows, after `git pull --ff-only`, run `.\scripts\test-windows.ps1`. Expect the PASS line and `$LASTEXITCODE` 0 (Q7).

---

## Testing Strategy

### Unit Tests (in `tests/BrutalUi.Core.Tests/Input/`)
- `EventWalkTests` (10): the walk order, the root target, the side object rule, the slot choice of each pointer type, the drain count, and the 5 stop cases.
- `EventKindTests` (12 cases): the slot isolation of each of the 7 types, the payload of each struct through the ring, and the key walk.
- `EventQueueTests` (8): the drop of the oldest entry, the drop count, the capacity rules, the type check, the null-target skip, and the push from a handler.
- `EventWalkEdgeTests` (5): the detached target, the tree change during the walk, the deep chain, the nested drain, and the handler exception.
- Each test builds its tree with `new Element { Name = ... }` and `Children.Add`, pushes hand-made events, and calls the internal `DrainEvents()`. No test uses GLFW. `EventRecorder` stores the calls in a fixed array.

### Allocation Test
- `EventAllocationTests` (3): the push and drain gate, the full-ring push gate, and the real-work check. Each gate calls `AllocationGate.ReleaseAllocationContext()` before its measured region. routing-gate adds the queue case to `ThousandElementAllocationTests` later.

### Integration Tests
- None in this feature. pointer-routing tests the adapter, and routing-gate tests the drain in `Update`.

### Manual Testing
- None. The feature has no visible output. The HelloPanel demo comes with routing-gate.

---

## Related Documentation

- **Requirements:** `docs/features/input-routing/event-model/requirements.md`
- **Epic:** `docs/features/input-routing/epic-overview.md`, `docs/features/input-routing/epic-requirements.md`
- **Siblings:** `docs/features/input-routing/pointer-routing/requirements.md`, `docs/features/input-routing/keyboard-focus/requirements.md`, `docs/features/input-routing/routing-gate/requirements.md`
- **Design:** `docs/technical-design.md` sections 5 (Events), 9 (Input), 14 (Testing), and 15 (Principles)
- **Rules:** `AGENTS.md`, `.beast-mode/phase-handoff.md` sections 1 and 11, `.beast-mode/tool-rules.md`
- **Code patterns:** `src/BrutalUi.Core/Rendering/PaintWalk.cs` (the explicit stack and its growth), `src/BrutalUi.Core/Tree/ElementStorage.cs` (the mutable struct in a field), `UiContext.Draw` (the re-entry guard), `tests/BrutalUi.Core.Tests/TreeAllocationTests.cs` and `tests/BrutalUi.Core.Tests/Shared/AllocationGate.cs` (the gate)
- **Format references:** `docs/features/core-tree/element-tree/implementation.md`, `docs/features/core-tree/allocation-guard/implementation.md`
- **External:** [Delegate.EnumerateInvocationList](https://learn.microsoft.com/en-us/dotnet/api/system.delegate.enumerateinvocationlist), [Delegate.InvocationListEnumerator](https://learn.microsoft.com/sk-sk/dotnet/api/system.delegate.invocationlistenumerator-1?view=net-10.0)

---

## Notes

### Open Scope Questions

The plan uses the recommended option (the first one) of each question.

**Q1. What type holds the key of a `KeyEvent`?**
- **A (recommended):** An `int` with the value of the GLFW key code table. Reason: the adapter casts with no table, and Core reads no key in this feature. keyboard-focus adds the named constants that it reads (Tab).
- B: A `UiKey` enum with each key now, with the GLFW values. More API and docs before a consumer needs it.
- C: A `UiKey` enum with its own values and a map table in `BrutalUi.Glfw`.

**Q2. What does the drain do with a target that left the tree of the context?**
- **A (recommended):** Skip the entry (Decision 7). Reason: a closed panel gets no click from a queued event.
- B: Walk the detached chain, as the DOM does.

**Q3. Does a `CharEvent` carry modifiers?**
- **A (recommended):** No. Reason: the GLFW char callback gives no modifiers, and the char-with-modifiers callback is deprecated.
- B: Yes, from the last key event of the adapter.

**Q4. Does the walk end at the target capture slot when a stop happens there?**
- **A (recommended):** Yes. The target bubble slot does not run (Decision 4, Decision 5). Reason: the literal requirement "calls no later handler".
- B: The DOM rule: both target slots run, and the stop applies to the ancestors only.

### Conflicts and Gaps

1. **The technical design, section 5.** It says "No closures in the hot path. A handler is a static method plus a target object, or a command id." The slots hold delegates. A delegate is a method plus a target. A closure allocates at build time only, and the walk only calls it. The command id belongs to the framework layer. No confirmed assumption conflicts with the code. Task 5.6 updates the paragraph.
2. **The technical design, section 9.** It names `UiInput` and `PickingEnabled`. The code has `IsPickable`. pointer-routing names the adapter and updates the section. This feature changes only the flow lines of the queue and the drain.
3. **No consumer of the drain in a game.** Until routing-gate, `Update` does not drain, so the ring fills in a game with an adapter. Only pointer-routing adds an adapter, so no game sees it before then.
4. **The size of an entry.** The plan estimates 64 bytes. Task 1.12 measures it and records it.
5. **The epic overview.** After this feature, `/update-epic input-routing` records the public API of the section "Public API" under Integration & Architecture.
6. **The prose lint.** The planner had no shell, so it did not run `ste-lint.py` on this file. Task 5.8 runs it.

---

*This plan will be updated as implementation progresses. See context.md and tasks.md for current status.*
