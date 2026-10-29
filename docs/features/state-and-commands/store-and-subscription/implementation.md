# Store and Subscription - Implementation Plan

**Epic:** state-and-commands (feature 1 of 4)
**Status:** Ready for Review
**Started:** 2026-10-29
**Target Completion:** 2026-10-29
**Last Updated:** 2026-10-29 06:48

---

## Quality Bar

**Target quality:** A library feature of the data layer. It opens the `BrutalUi.Framework` package, and each later feature of the epic reads a store. element-bindings puts a subscription into a pooled list on `Element`, and its binding pass reads each store version in each frame. A wrong version, a lost change, a false change, or a hidden allocation here breaks each bound element of each game.

- **The version is exact:** A new store has the version 1. A `Set` that changes the value raises the version by exactly one. A `Set` of an equal value by `EqualityComparer<T>.Default` keeps the version and the old value. A test pins each rule.
- **A subscription sees each change one time:** The first `TryGetChanged` always applies. After that, it returns `true` one time for each version change, and the value is the slice of the newest value. Two sets before one read give one change.
- **0 bytes after warm-up:** `Set` on a store of an `int`, of a `record struct`, and of a `string`, a version read through `IBindingSource`, and `TryGetChanged` with a static selector allocate 0 bytes. A planted allocation in `Store<T>.Set` fails the gate.
- **No event in the frame loop:** The store holds no event, no subscriber list, and no reference to a subscriber. It implements no `INotifyPropertyChanged`.
- **The Core and Framework split:** `BrutalUi.Core` gets one interface, `IBindingSource`, with one member. It names no Framework type. `BrutalUi.Framework` references Core only.
- **API clarity:** Each public member has an XML doc comment in ASD-STE100 prose. The remarks state the equality rule, the struct copy rule of the subscription, the selector rule, and the thread rule.

---

## Executive Summary

This feature adds the state type of the mosaic-ui model to brutal-ui. `Store<T>` is a sealed class in `BrutalUi.Framework`. It holds one value of `T` and a `ulong` version. `Set(in T)` compares the new value with the old value through `EqualityComparer<T>.Default`. On a change, it writes the value and raises the version by one. On an equal value, it does nothing. A reader pulls a change: it compares the version with the version that it applied last. Thus the store needs no event and no callback, and the frame loop runs no `INotifyPropertyChanged` code. This replaces `INotifyBindablePropertyChanged` of mosaic-ui.

`IBindingSource` is the one Core contract. It holds `ulong Version { get; }`. `Store<T>` implements it, so the Core binding pass of element-bindings can read a version with no Framework type.

`StoreSubscription<T, TValue>` is a mutable struct. It holds a `Store<T>`, a static `Func<T, TValue>` selector, and the last version that it applied (0 at the start). `TryGetChanged(out TValue)` returns `false` when the store version equals the applied version. Else it stores the new version, runs the selector on the store value, and returns `true`.

The plan has 2 phases and 14 tasks. Phase 1 delivers the store and its tests. Phase 2 delivers the subscription, the copied allocation helper, the 0-byte gate, and the docs.

---

## User Personas Alignment

No `docs/user-personas.md` file exists. The personas come from `docs/mission-statement.md` and the epic docs.

**Target Users:**
- [x] **The game developer** (creates a `Store<GameState>` with a first value, calls `Set` from game code, and reads `Value` and `Version`)
- [x] **The sibling feature agents** (element-bindings reads `IBindingSource.Version` from Core and holds a `StoreSubscription` for each binding. commands-and-events copies the Framework test layout and the gate)
- [x] **The maintainer** (runs one more 0-byte gate in `scripts/test-linux.sh`)
- [ ] **The markup author** (not served. The xml-markup epic binds a store from XML)

**User Subsets Served:**
- [x] **Simple** - `var score = new Store<int>(new Store<int>.CreateInfo { Initial = 0 }); score.Set(10);`
- [x] **Complex** - a `record struct GameState` store, two subscriptions with static selectors on two slices, and a pull of each slice in a game loop with no allocation.

**UX Approach:**
- One write method. A set of an equal value is free and silent.
- The reader decides when to read. A change between two reads costs nothing extra.

---

## Goals

### Primary Goals
1. Add `IBindingSource` to `BrutalUi.Core` with `ulong Version { get; }` only.
2. Add `Store<T>` to `BrutalUi.Framework` with `CreateInfo`, `Value`, `Version`, and `Set(in T)`.
3. Add `StoreSubscription<T, TValue>` with `TryGetChanged(out TValue)`.
4. Prove 0 bytes after warm-up for `Set`, a version read, and `TryGetChanged`.
5. Replace the placeholder test of the Framework test project with real tests.
6. Leave a clear seam for element-bindings, and record it in Notes.

### Success Criteria
- [ ] The phase 1 demo passes: `scripts/test-linux.sh` prints its PASS line, and its test log shows the Framework test assembly with 0 failed.
- [ ] `StoreTests`, `StoreSubscriptionTests`, and `StoreAllocationTests` pass.
- [ ] `StoreAllocationTests` measures 0 bytes, and a planted allocation in `Store<T>.Set` fails it.
- [ ] `tests/BrutalUi.Framework.Tests/PlaceholderTests.cs` does not exist.
- [ ] Each existing test passes with no edit.

---

## Architecture Overview

### The Pull Model

mosaic-ui pushes a change: a store raises `INotifyBindablePropertyChanged`, and each binding runs in the handler. A push needs a subscriber list in the store and a delegate call for each change, and it runs at the time of the write, outside the frame order.

brutal-ui pulls a change. The store raises a counter. The reader keeps the counter value that it applied last. When the two values differ, the reader reads the store. The store holds no list of readers, so it keeps no reference to an element (a rule of element-bindings). Many writes before one read give one apply. The binding pass of element-bindings runs the pull once per frame, before the style pass (Q2 of the epic).

### File Tree (end state)

```
src/BrutalUi.Core/
└── Binding/IBindingSource.cs                    # phase 1 (new folder)
src/BrutalUi.Framework/
├── State/Store.cs                               # phase 1 (new folder)
└── State/StoreSubscription.cs                   # phase 2
tests/BrutalUi.Framework.Tests/
├── PlaceholderTests.cs                          # phase 1: deleted (A7)
├── State/StoreTests.cs                          # phase 1
├── State/StoreSubscriptionTests.cs              # phase 2
├── State/StoreAllocationTests.cs                # phase 2
└── Shared/AllocationGate.cs                     # phase 2: a copy of the Core.Tests file (A6)
docs/technical-design.md                         # phase 2: sections 10 and 14
CHANGELOG.md                                     # phase 2
docs/features/state-and-commands/store-and-subscription/context.md   # each phase
```

These files do not change: each `.csproj`, `Directory.Build.props`, `Directory.Packages.props`, `brutal-ui.sln`, `scripts/`, `samples/`, each file in `tests/BrutalUi.Core.Tests/` (also `Shared/AllocationGate.cs`), and each existing file in `src/BrutalUi.Core/`. The folder `State/` follows the source column of section 10 of the technical design. The namespaces stay flat: `BrutalUi.Core`, `BrutalUi.Framework`, and `BrutalUi.Framework.Tests`, as in the existing projects.

### Public API (end state)

```csharp
namespace BrutalUi.Core;

public interface IBindingSource
{
    ulong Version { get; }                          // phase 1 (A1)
}
```

```csharp
namespace BrutalUi.Framework;

public sealed class Store<T> : IBindingSource       // phase 1 (A5)
{
    public Store(in CreateInfo info);               // phase 1: Value = info.Initial, Version = 1 (A2)
    public T Value { get; }                         // phase 1
    public ulong Version { get; }                   // phase 1
    public void Set(in T value);                    // phase 1 (Q1, Q2)

    public struct CreateInfo
    {
        public required T Initial;                  // phase 1 (A4)
    }
}

public struct StoreSubscription<T, TValue>          // phase 2 (Q3, A5)
{
    public StoreSubscription(Store<T> store, Func<T, TValue> selector); // throws ArgumentNullException
    public readonly Store<T> Store { get; }
    public readonly ulong AppliedVersion { get; }   // 0 at the start (A2)
    public bool TryGetChanged([MaybeNullWhen(false)] out TValue value); // throws InvalidOperationException on a default value
}
```

### Internal Shape

- `Store<T>` fields: `private T _value;` and `private ulong _version;`. The constructor writes `_value = info.Initial` and `_version = 1`.
- `Store<T>.Set(in T value)`: `if (EqualityComparer<T>.Default.Equals(_value, value)) return; _value = value; _version++;`. The code calls `EqualityComparer<T>.Default` at the call site and does not cache it in a field, because the JIT devirtualizes the direct call for a value type (Decision 2).
- `StoreSubscription<T, TValue>` fields: `private readonly Store<T> _store;`, `private readonly Func<T, TValue> _selector;`, and `private ulong _applied;`. The fields hold a null reference only in a `default` value.
- `TryGetChanged`: when `_store` is `null`, throw `InvalidOperationException`. Read `ulong version = _store.Version`. When `version == _applied`, set `value = default` and return `false`. Else set `_applied = version`, set `value = _selector(_store.Value)`, and return `true`.

### Data Flow

```
Game code (UI thread): store.Set(in next)
  EqualityComparer<T>.Default.Equals(_value, next)
    true  ──► return (no write, no version change)
    false ──► _value = next ──► _version++

Reader (game loop now, the binding pass of element-bindings later):
  ref StoreSubscription<T, TValue> s = ref slots[i]
  s.TryGetChanged(out TValue v)
    _store.Version == _applied ──► false, v = default
    else                       ──► _applied = _store.Version ──► v = _selector(_store.Value) ──► true
```

### The Seam for element-bindings

`Element` lives in Core, and Core does not reference Framework. Thus the binding list on `Element` cannot name `StoreSubscription<T, TValue>` or `Store<T>`. This feature keeps the seam open and gives element-bindings two tools: `IBindingSource.Version` (a version check with no Framework type) and `StoreSubscription.Store` plus `AppliedVersion` (a Framework adapter can read both). Notes item 1 lists the options. This plan does not choose one.

---

## Implementation Phases

Each phase is a slice: one narrow path that works end to end, with a demo line that a person can check. Section 11 of `.beast-mode/phase-handoff.md` holds the rules. The Confirmed Assumptions name the demo of phase 1 (Q4).

Each phase ends with `dotnet build brutal-ui.sln 2>&1 | tail -n 30` and `dotnet test --solution brutal-ui.sln 2>&1 | tail -n 30` (tool rule 5). Expect `0 Warning(s)`, `0 Error(s)`, and 0 failed. The projects have no implicit usings, so each new file holds its own `using` lines. A warning fails the build. Each public member of `src/` needs an XML doc comment (CS1591 is an error). Use file-scoped namespaces (IDE0161). Prose in comments follows ASD-STE100. Run one class with `dotnet run --project tests/BrutalUi.Framework.Tests/BrutalUi.Framework.Tests.csproj -- --filter-class '*<Class>' 2>&1 | tail -n 5`. The test project references Framework only. It sees `IBindingSource` through the transitive reference to Core, so it needs no `.csproj` edit.

### Phase 1: The Store and Its Version
**Goal:** The smallest path that proves the idea: a game creates a `Store<int>`, calls `Set`, and reads the value and a version that rises by one for each change and stays for an equal value. The store is an `IBindingSource` of Core.
**Demo:** a person runs `scripts/test-linux.sh` and reads its PASS line. Then the person runs `grep -n 'BrutalUi.Framework.Tests' artifacts/test-linux/test.log` and reads the result of the Framework test assembly with 0 failed (Q4).
**Needs:** none
**Agent tier:** STANDARD (sonnet)

**Tasks:**
- [ ] 1.1 Create `docs/features/state-and-commands/store-and-subscription/context.md`. Record the base commit (`git rev-parse HEAD`), the test counts of the solution, of `BrutalUi.Core.Tests`, and of `BrutalUi.Framework.Tests` (1, the placeholder). Record the Pull Model and the Seam of the Architecture Overview.
- [ ] 1.2 Write `src/BrutalUi.Core/Binding/IBindingSource.cs`: `public interface IBindingSource` in `namespace BrutalUi.Core;` with `ulong Version { get; }` (A1). The summary states: a source of bound state with a version counter. The remarks state: a reader keeps the version that it applied last and reads the source again when the version differs. The version never goes down. A new source starts at 1, so a reader that starts at 0 applies at its first read (A2). The source raises no event. A read allocates nothing. The interface names no Framework type, so the Core binding pass reads it (Q1 of the epic). Use no `<see cref>` to a Framework type.
- [ ] 1.3 Write `src/BrutalUi.Framework/State/Store.cs`: `public sealed class Store<T> : IBindingSource` in `namespace BrutalUi.Framework;`, with the members of the Public API and the Internal Shape. `CreateInfo` holds one field, `public required T Initial;` (A4). The constructor takes `in CreateInfo info`. The class remarks state: the equality rule (`EqualityComparer<T>.Default`, Decision 2), the advice to use a type with `IEquatable<T>` for a struct `T` (a `record struct` has it), the reference rule for a class with no `Equals` override, the version rule (A2), the UI thread rule with no lock (A3), the pull model (Decision 1), "the store keeps no reference to a reader", and "the store replaces `INotifyBindablePropertyChanged` of mosaic-ui". The `Set` remarks state: an equal value changes nothing, also not the stored reference.
- [ ] 1.4 Delete `tests/BrutalUi.Framework.Tests/PlaceholderTests.cs` (A7).
- [ ] 1.5 Write `tests/BrutalUi.Framework.Tests/State/StoreTests.cs` in `namespace BrutalUi.Framework.Tests;` with 9 tests: `ANewStoreHoldsTheInitialValueAtVersionOne`, `ASetOfANewValueRaisesTheVersionByOne` (0 to 10: `Value` 10, `Version` 2), `ASetOfAnEqualValueKeepsTheVersion` (10 then 10: `Version` stays), `EachChangeRaisesTheVersionByOne` (the values 1, 2, 2, 1 from the start 1: the versions 1, 2, 2, 3. A return to an old value is a change), `ARecordStructComparesByValue` (a private `record struct Pair(int A, int B)`: an equal new instance keeps the version, a new `B` raises it), `AClassWithNoEqualsComparesByReference` (a private sealed class with one field: a new instance with the same field raises the version, the same instance keeps it), `AnEqualStringKeepsTheVersionAndTheOldReference` (the start `"abc"`, then `new string('a', 1) + "bc"`: the version stays, and `Assert.Same` shows the old reference), `ANullValueComparesSafely` (a `Store<string?>` with the start `null`: `Set(null)` keeps the version, `Set("a")` raises it, `Set(null)` raises it), and `TheStoreIsABindingSource` (an `IBindingSource` reference to the store reads the same version after each `Set`).
- [ ] 1.6 Run the build and the tests. Run `dotnet run --project tests/BrutalUi.Framework.Tests/BrutalUi.Framework.Tests.csproj -- --filter-class '*StoreTests' 2>&1 | tail -n 5` and expect a total of 9 with `failed: 0`. Run `scripts/test-linux.sh; echo "exit=$?"` and expect the PASS line and `exit=0`. Run the grep of the demo. Record the counts and the grep line in `context.md`.

**Estimated Steps:** about 30 (6 tasks, about five steps for one task)

**Files (5):** `IBindingSource.cs`, `Store.cs`, `PlaceholderTests.cs` (deleted), `StoreTests.cs`, `context.md`

**Acceptance Criteria:**
- [ ] The 9 tests of `StoreTests` pass.
- [ ] `PlaceholderTests.cs` does not exist, and the Framework test project still builds and runs.
- [ ] `BrutalUi.Core.Tests` keeps its count of task 1.1, with no edit.
- [ ] `scripts/test-linux.sh` prints its PASS line.
- [ ] `IBindingSource.cs` names no Framework type and holds one member.

---

### Phase 2: The Subscription and the 0-Byte Gate
**Goal:** A reader pulls each store change one time through a `StoreSubscription<T, TValue>` and a static selector. `Set`, a version read, and `TryGetChanged` allocate 0 bytes after warm-up. The technical design and the changelog name the new types.
**Demo:** a unit test creates a `Store<GameState>` and a subscription with the static selector `static s => s.Score`. The first `TryGetChanged` gives `true` and the start score, the second gives `false`, and a `Set` of a new score gives `true` and the new score one time. The developer runs `dotnet run --project tests/BrutalUi.Framework.Tests/BrutalUi.Framework.Tests.csproj -- --filter-class '*StoreSubscriptionTests'` and reads a total of 10 with `failed: 0`, and the same command with `'*StoreAllocationTests'` reads a total of 4 with `failed: 0`.
**Needs:** Phase 1
**Agent tier:** STANDARD (sonnet)

**Tasks (group A: the subscription):**
- [ ] 2.1 Write `src/BrutalUi.Framework/State/StoreSubscription.cs`: `public struct StoreSubscription<T, TValue>` with the members of the Public API and the Internal Shape (Q3). The constructor calls `ArgumentNullException.ThrowIfNull` for the store and the selector. Mark the getters `readonly`. Put `[MaybeNullWhen(false)]` on the `out` parameter. The remarks state: the first read applies (A2). One `true` for each version change, with the slice of the newest value. A change of another slice of the same store also gives `true` (Decision 4). The struct is mutable, so call `TryGetChanged` on a variable, an array slot, or a `ref`, never on a copy or a `readonly` field (Decision 5). Pass a `static` lambda or a cached method group, and a selector must not allocate (Decision 6). A `default` value holds no store and throws (Decision 7). The UI thread rule (A3).
- [ ] 2.2 Write `tests/BrutalUi.Framework.Tests/State/StoreSubscriptionTests.cs` with 10 tests on a private `record struct GameState(int Score, int Lives)`: `TheFirstReadAppliesTheCurrentValue` (the demo: `true`, the start score, and `AppliedVersion` 1), `ASecondReadWithNoChangeGivesNothing` (`false`, and the value is `default`), `ASetOfANewSliceGivesOneChange` (`true` with the new score, then `false`), `AnEqualSetGivesNoChange`, `TwoSetsBeforeOneReadGiveOneChangeWithTheLastValue`, `AChangeOfAnotherSliceGivesAChangeWithTheSameSlice` (a change of `Lives` only: `true` with the old score, Decision 4), `TwoSubscriptionsOnOneStoreKeepTheirOwnVersion`, `ASubscriptionInAnArraySlotKeepsItsVersion` (a call on `slots[0]` stores the version. A copy taken before the call still reads `AppliedVersion` 0, Decision 5), `TheConstructorRejectsANullStoreOrSelector` (`ArgumentNullException` with the parameter name for each), and `ADefaultSubscriptionThrows` (`InvalidOperationException`). Each selector is a `static` lambda.
- [ ] 2.3 Run the build, the tests, the class of task 2.2, and `StoreTests`. Record the results in `context.md`.

**Tasks (group B: the gate and the docs):**
- [ ] 2.4 Write `tests/BrutalUi.Framework.Tests/Shared/AllocationGate.cs`: a copy of `tests/BrutalUi.Core.Tests/Shared/AllocationGate.cs` with `namespace BrutalUi.Framework.Tests;` (A6). Keep the code and the first remarks paragraph. Replace the second paragraph: this file is a copy of the Core.Tests helper, because the Framework tests do not reference Core.Tests. Keep the two files the same in code.
- [ ] 2.5 Write `tests/BrutalUi.Framework.Tests/State/StoreAllocationTests.cs` with 4 tests. Use the shape of `tests/BrutalUi.Core.Tests/Elements/SliderAllocationTests.cs`: build the scene, run 2 warm-up rounds, call `AllocationGate.ReleaseAllocationContext()`, read `GC.GetAllocatedBytesForCurrentThread()`, run 100 iterations, read the counter again, and assert `0L`. The scene holds a `Store<int>`, a `Store<GameState>` (a `record struct`), a `Store<string>` with two strings that the scene built, a subscription with a static selector, and an `IBindingSource` reference to each store. The tests: `SetAllocatesNothing` (each iteration sets a new value and an equal value on each of the three stores), `AVersionReadAllocatesNothing` (each iteration reads `Version` of each store through `IBindingSource` and adds it to a `ulong` sum. Assert the sum after the measure), `TryGetChangedAllocatesNothing` (each iteration sets a new `GameState` and calls `TryGetChanged`, then calls it again with no change), and `TheMeasuredBodyDoesRealWork` (after a run of the bodies of the three gates, the version of each store and the count of `true` results match the expected counts, so an empty body cannot pass). The class remarks state the scene and the rule: the measured bodies create no lambda, no string, no boxed value, and no `Assert`.
- [ ] 2.6 Edit section 10 of `docs/technical-design.md`, "Data binding": add the store rules (one value, the version from 1, the equality rule, `Set` only, no event) and the subscription rules (the applied version from 0, `TryGetChanged`, the static selector, the struct copy rule). State that `IBindingSource` in Core holds the version only. Edit section 14: add one bullet for `StoreAllocationTests` in `tests/BrutalUi.Framework.Tests/`.
- [ ] 2.7 Edit `CHANGELOG.md`: under `## [Unreleased]`, `### Added`, add one line: `IBindingSource` in Core, `Store<T>` and `StoreSubscription<T, TValue>` in Framework, and the Framework allocation gate (state-and-commands/store-and-subscription).
- [ ] 2.8 Run the build, the tests, the classes of tasks 2.2 and 2.5, and `StoreTests`. Plant `GC.KeepAlive(new object());` as the first line of `Store<T>.Set`, run `dotnet test --solution brutal-ui.sln > /dev/null 2>&1; echo "exit=$?"`, and expect a value that is not 0. Remove the plant and run the tests again. Run `scripts/test-linux.sh; echo "exit=$?"` and expect the PASS line and `exit=0`. Run `python3 /home/aaronstatic/.claude/skills/asd-ste100/scripts/ste-lint.py --fail-over 2.5` on the changed docs. Record the results and each decision that this plan did not settle in `context.md`.

**Estimated Steps:** about 45 (8 tasks, about five to six steps for one task)

**Files (group A, 3):** `StoreSubscription.cs`, `StoreSubscriptionTests.cs`, `context.md`
**Files (group B, 5):** `AllocationGate.cs` (copy), `StoreAllocationTests.cs`, `technical-design.md`, `CHANGELOG.md`, `context.md`

**Acceptance Criteria:**
- [ ] The 10 tests of `StoreSubscriptionTests` and the 4 tests of `StoreAllocationTests` pass, and `StoreTests` still passes.
- [ ] The planted allocation in `Store<T>.Set` gives a non-zero exit code of `dotnet test`.
- [ ] `tests/BrutalUi.Core.Tests/Shared/AllocationGate.cs` has no diff, and the Framework test project has no reference to `BrutalUi.Core.Tests`.
- [ ] `scripts/test-linux.sh` prints its PASS line.

---

## Key Technical Decisions

### Confirmed Assumptions

- Q1 Equality in Set: `EqualityComparer<T>.Default`, with no constraint on `T`.
- Q2 Write API: `Set(in T)` only.
- Q3 Subscription shape: a generic struct `StoreSubscription<T, TValue>` that holds `Store<T>`, a static `Func<T, TValue>` selector, and the last version. It exposes `TryGetChanged(out TValue)`.
- Q4 Phase 1 demo: the output of `scripts/test-linux.sh` shows that the Framework tests pass.
- A1 `IBindingSource` in Core holds one member: `ulong Version { get; }`.
- A2 A new store starts at version 1. A new subscription starts at version 0, so the first read always applies.
- A3 The store runs on the UI thread only. It has no lock and no volatile read.
- A4 `Store<T>.CreateInfo` holds one `required` field, `Initial`.
- A5 `Store<T>` is a sealed class. `StoreSubscription` is a struct.
- A6 The tests copy `AllocationGate` from `tests/BrutalUi.Core.Tests/Shared/AllocationGate.cs` into the Framework test project. The Framework tests do not reference Core.Tests.
- A7 The feature deletes `tests/BrutalUi.Framework.Tests/PlaceholderTests.cs`.
- A8 `scripts/test-linux.sh` runs `dotnet test --solution brutal-ui.sln`, so it already runs the Framework tests. No script change.

### Decision 1: A version counter and a pull, not an event
**Choice:** The store raises a `ulong` version on each change. A reader compares the version with the version that it applied last. The store holds no event, no subscriber list, and no reference to a reader.
**Rationale:** Q1 and Q2 of the epic. A pull fits the frame order: the binding pass reads each version one time per frame, before the style pass. Many writes in one frame give one apply. No delegate runs at the time of the write, so a write from game code cannot reach the tree in the middle of a frame. The store keeps no reference to an element, so an element that leaves the tree needs no unsubscribe from the store (a rule of element-bindings). A `ulong` does not wrap in practice: at one change per microsecond, it wraps after more than 500000 years. The code uses a plain unchecked `++`.
**Alternatives:** (a) An `Action` event on the store (the mosaic-ui shape): a subscriber list, a delegate call for each write, and a reference from the store to each element. (b) A dirty flag on the store: one flag cannot serve two readers. (c) An `int` version: a wrap is possible in a long session with a fast store.

### Decision 2: `EqualityComparer<T>.Default` at the call site
**Choice:** `Set` calls `EqualityComparer<T>.Default.Equals(_value, value)` directly (Q1). On `true`, it returns and keeps the old value. `T` has no constraint.
**Rationale:** The JIT treats `EqualityComparer<T>.Default` as an intrinsic. For a value type `T`, it devirtualizes the direct call, so an `int` or a `record struct` compares with no boxing. A cached comparer in a static field blocks the devirtualization. An equal value keeps the old value, so an equal string or record keeps the old reference, and a reader that compares by reference sees no change.
**Alternatives:** (a) A `where T : IEquatable<T>` constraint: it blocks a class with no `IEquatable<T>` (Q1 rejects it). (b) An `IEqualityComparer<T>` member in `CreateInfo`: YAGNI. A later feature can add it. (c) A `Set` that always raises the version: each frame of a game that writes each frame applies each binding.

### Decision 3: A struct `T` with no `IEquatable<T>` boxes in `Set`
**Choice:** The store does not check for it. The remarks of `Store<T>` advise a `record struct` or a struct with `IEquatable<T>`. The gate uses an `int`, a `record struct`, and a `string`.
**Rationale:** For a struct with no `IEquatable<T>`, `EqualityComparer<T>.Default` falls back to `ValueType.Equals(object)`, which boxes both values. A runtime check costs code and gives no fix. A `record struct`, the common shape of game state, has `IEquatable<T>`. Notes item 3 records it.
**Alternatives:** (a) A throw in the constructor for such a `T`: a reflection check and a surprise for a correct but slow type. (b) A constraint: rejected by Q1.

### Decision 4: The subscription tracks the store version, not the slice value
**Choice:** `TryGetChanged` returns `true` for each new store version, also when the selected slice is equal to the last slice. It does not store the last slice.
**Rationale:** The requirement: "records a store, a selector, and the last version it applied". A slice compare needs a stored `TValue` and a second comparer call for each read, and the binding knows its own cheap compare (a label compares its UTF-8 text, a slider compares its float). element-bindings decides a slice compare when it needs one. A test pins the rule (`AChangeOfAnotherSliceGivesAChangeWithTheSameSlice`).
**Alternatives:** A stored last slice and an `EqualityComparer<TValue>` compare: more state and a box risk for each `TValue`, with no reader that needs it yet.

### Decision 5: The subscription is a mutable struct with a copy rule
**Choice:** `TryGetChanged` writes `_applied`, so the method is not `readonly`. The remarks tell the caller to call it on a variable, an array slot, or a `ref`. The getters are `readonly`.
**Rationale:** A5 and the epic rule: a binding is a struct in a pooled list. A struct in an array slot needs no allocation for each binding. A call on a copy loses the version, and the next read applies again. The test `ASubscriptionInAnArraySlotKeepsItsVersion` pins the rule for element-bindings.
**Alternatives:** A class: one allocation for each binding and a pointer chase in the binding pass.

### Decision 6: The selector is a static lambda, and the subscription does not check it
**Choice:** The constructor takes a `Func<T, TValue>` and checks it for `null` only. The remarks advise a `static` lambda or a method group in a `static readonly` field.
**Rationale:** Q3. The compiler caches a `static` lambda in a static field, so the call site allocates the delegate one time. The subscription runs the delegate only in `TryGetChanged`, and a delegate call allocates nothing. A runtime check for a static lambda does not work: the compiler emits a static lambda as an instance method on a singleton, so `Target` is not `null`. A capturing lambda allocates at build time only, and the epic allows a build-time allocation (A5 of the epic).
**Alternatives:** A function pointer `delegate*<T, TValue>`: no lambda syntax, and it needs `unsafe` at each call site.

### Decision 7: A default subscription throws
**Choice:** `TryGetChanged` throws `InvalidOperationException` when the subscription holds no store (a `default` value).
**Rationale:** A pooled array holds `default` slots past its count. A read of such a slot is a bug in the caller. A clear exception names the bug, and a `NullReferenceException` does not. The check is one null compare.
**Alternatives:** (a) Return `false`: the bug stays hidden. (b) No check: a `NullReferenceException` with no message.

### Decision 8: `Value` returns a copy of `T`
**Choice:** `public T Value { get; }`.
**Rationale:** YAGNI. The selector takes `T` by value, and the common state is a small `record struct`. A `ref readonly T` return lets a caller hold a reference into the store, against the write rule of `Set`.
**Alternatives:** `ref readonly T Value`: less copy for a large struct. A later feature can add it with no break of a caller.

---

## Risks & Mitigation

| Risk | Likelihood | Impact | Mitigation |
|------|------------|--------|------------|
| A struct `T` with no `IEquatable<T>` boxes in each `Set`. | Medium | Medium | Decision 3. The remarks of `Store<T>` state the rule. The gate uses a `record struct`. Notes item 3. |
| A caller calls `TryGetChanged` on a copy, and the version gets lost. | Medium | Medium | Decision 5. The remarks state the rule. `ASubscriptionInAnArraySlotKeepsItsVersion` pins it. Notes item 1 names it for element-bindings. |
| A selector allocates (a boxing slice, a `ToString`). | Medium | High | Decision 6. The remarks state the rule. element-bindings gates the full binding pass with `BoundTreeAllocationTests`. |
| The JIT does not devirtualize the comparer in Debug, and the gate sees an allocation. | Low | Medium | The existing gates run in Debug through `dotnet test`. The comparer call allocates nothing also with no devirtualization for `int`, `string`, and a type with `IEquatable<T>`. The warm-up rounds cover the first access of `EqualityComparer<T>.Default`. |
| A background GC adds bytes to the counter of the gate. | Low | Medium | The copied `AllocationGate.ReleaseAllocationContext` runs right before each measure (A6). |
| The two copies of `AllocationGate` drift. | Low | Low | The remarks of the copy name the source. Task 2.4 keeps the code the same. Step 6 of the Verification Method compares the code. |
| element-bindings cannot hold the generic subscription in a Core list. | High | Medium | The Seam of the Architecture Overview and Notes item 1. `IBindingSource.Version`, `Store`, and `AppliedVersion` give an adapter what it needs. |
| A store used from two threads gives a lost version. | Low | High | A3. The remarks state the UI thread rule. |

---

## Dependencies

### Internal Dependencies
- `dev-ops`: `brutal-ui.sln`, `src/BrutalUi.Framework/` (references Core only), `tests/BrutalUi.Framework.Tests/` (references Framework only), `Directory.Build.props` (warnings as errors, no implicit usings), `src/Directory.Build.props` (the doc file), and `scripts/test-linux.sh`.
- `core-tree/allocation-guard`: the gate pattern and `tests/BrutalUi.Core.Tests/Shared/AllocationGate.cs` (copied, A6).
- No sibling feature. This feature starts the epic.

### External Dependencies
- None. No new package and no native dependency. `System.Collections.Generic.EqualityComparer<T>` and `System.Diagnostics.CodeAnalysis.MaybeNullWhenAttribute` come from the .NET 10 base library.

---

## Definition of Done

All criteria below must pass. An independent evaluator runs the Verification Method from `/home/aaronstatic/dev/brutal-ui` on Linux. `<base>` is the base commit that task 1.1 records in `context.md`.

### Functional Criteria
- [ ] F1. A new `Store<int>` with `Initial = 0` reads `Value` 0 and `Version` 1. `Set(10)` gives `Value` 10 and `Version` 2. `Set(10)` again keeps `Version` 2 (`StoreTests`).
- [ ] F2. Each change raises the version by exactly one, also a return to an old value. A `record struct` compares by value, a class with no `Equals` override compares by reference, an equal string keeps the old reference, and `null` compares safely. An `IBindingSource` reference reads the same version (`StoreTests`, 9 tests).
- [ ] F3. A new subscription applies at its first read with the start slice and `AppliedVersion` 1. A read with no change gives `false`. Each version change gives one `true` with the slice of the newest value. Two sets before one read give one `true`. A change of another slice gives `true`. Two subscriptions keep their own versions. A copy keeps its own version. A null store or selector throws `ArgumentNullException`, and a default subscription throws `InvalidOperationException` (`StoreSubscriptionTests`, 10 tests).
- [ ] F4. `StoreAllocationTests` measures 0 bytes for `Set` on three store types, for a version read through `IBindingSource`, and for `TryGetChanged`, and its real-work test passes (4 tests). A planted allocation in `Store<T>.Set` gives a non-zero exit code of `dotnet test`.

### Quality Criteria
- [ ] Q1. `dotnet build brutal-ui.sln` gives `0 Warning(s)` and `0 Error(s)`.
- [ ] Q2. `dotnet test --solution brutal-ui.sln` gives 0 failed. `BrutalUi.Framework.Tests` has 23 tests (9 + 10 + 4). `BrutalUi.Core.Tests` keeps the count of task 1.1.
- [ ] Q3. `Store<T>` is `public sealed class` and implements `IBindingSource`. `StoreSubscription<T, TValue>` is `public struct`. `Store<T>.CreateInfo` holds one field, and it is `required`. The store has one public write method, `Set(in T)`.
- [ ] Q4. `src/BrutalUi.Framework/State/` holds no `event`, no `INotifyPropertyChanged`, no `lock`, and no `Volatile` in code.
- [ ] Q5. The new doc comments and the changed docs use ASD-STE100 prose. The lint gives no score above 2.5.

### Integration Criteria
- [ ] I1. `IBindingSource` lives in `src/BrutalUi.Core/Binding/IBindingSource.cs`, holds one member `ulong Version { get; }`, and names no Framework type. No other file of `src/BrutalUi.Core/` changes.
- [ ] I2. No `.csproj`, `Directory.Build.props`, `Directory.Packages.props`, `brutal-ui.sln`, `scripts/`, `samples/`, or file in `tests/BrutalUi.Core.Tests/` has a diff against `<base>`.
- [ ] I3. `tests/BrutalUi.Framework.Tests/PlaceholderTests.cs` does not exist. `tests/BrutalUi.Framework.Tests/Shared/AllocationGate.cs` exists, and its code matches the Core.Tests file except the namespace and the doc comments.
- [ ] I4. Section 10 of `docs/technical-design.md` states the store, version, and subscription rules. Section 14 names `StoreAllocationTests`. `CHANGELOG.md` names `IBindingSource`, `Store<T>`, and `StoreSubscription<T, TValue>`.
- [ ] I5. `scripts/test-linux.sh` prints its PASS line, and `artifacts/test-linux/test.log` shows the Framework test assembly with 0 failed (Q4).

### Verification Method

Run each step from `/home/aaronstatic/dev/brutal-ui` on Linux. `<scratch>` is any empty folder outside the repository. Read `<base>` from `docs/features/state-and-commands/store-and-subscription/context.md`.

1. `dotnet build brutal-ui.sln 2>&1 | tail -n 30`. Expect `0 Warning(s)` and `0 Error(s)` (Q1).
2. `dotnet test --solution brutal-ui.sln 2>&1 | tail -n 30`. Expect 0 failed. Compare the Core total with the count of task 1.1 (Q2).
3. For each class, run `dotnet run --project tests/BrutalUi.Framework.Tests/BrutalUi.Framework.Tests.csproj -- --filter-class '*<Class>' 2>&1 | tail -n 5`. Expect `failed: 0` and these totals: `StoreTests` 9 (F1, F2), `StoreSubscriptionTests` 10 (F3), and `StoreAllocationTests` 4 (F4). Read the test names in each file and match them to the criteria.
4. Copy `src/BrutalUi.Framework/State/Store.cs` to `<scratch>`. Add `GC.KeepAlive(new object());` as the first line of `Set`. Run `dotnet test --solution brutal-ui.sln > /dev/null 2>&1; echo "exit=$?"`. Expect a value that is not 0. Restore the file from the copy, check it with `cmp`, and run step 2 again (F4).
5. `git diff <base> --stat -- '*.csproj' Directory.Build.props Directory.Packages.props brutal-ui.sln scripts/ samples/ tests/BrutalUi.Core.Tests/`. Expect no output (I2). `git status --porcelain -- src/BrutalUi.Core/` and `git diff <base> --name-status -- src/BrutalUi.Core/`. Expect the new file `src/BrutalUi.Core/Binding/IBindingSource.cs` only (I1).
6. `test ! -e tests/BrutalUi.Framework.Tests/PlaceholderTests.cs && echo gone`. Expect `gone`. `diff <(grep -vE '^\s*///|^namespace' tests/BrutalUi.Core.Tests/Shared/AllocationGate.cs) <(grep -vE '^\s*///|^namespace' tests/BrutalUi.Framework.Tests/Shared/AllocationGate.cs)`. Expect no output (I3).
7. `grep -v '///' src/BrutalUi.Core/Binding/IBindingSource.cs`. Read the code lines: one `public interface IBindingSource`, one member `ulong Version { get; }`, and no word `Framework` (I1).
8. `grep -n 'public sealed class Store<T> : IBindingSource' src/BrutalUi.Framework/State/Store.cs` gives one line. `grep -n 'public struct StoreSubscription<T, TValue>' src/BrutalUi.Framework/State/StoreSubscription.cs` gives one line. `grep -v '///' src/BrutalUi.Framework/State/Store.cs | grep -n 'required'` gives one line, the field `Initial`. `grep -nE 'public (void|bool) ' src/BrutalUi.Framework/State/Store.cs` gives the line of `Set` only (Q3).
9. `grep -rnE '\bevent\b|INotifyPropertyChanged|\block\b|Volatile' src/BrutalUi.Framework/State/ | grep -v '///'`. Expect no line (Q4).
10. `scripts/test-linux.sh; echo "exit=$?"`. Expect the PASS line and `exit=0`. Then `grep -n 'BrutalUi.Framework.Tests' artifacts/test-linux/test.log`. Expect the result of the Framework test assembly with 0 failed (I5).
11. Read section 10 ("Data binding") and section 14 of `docs/technical-design.md` and the `## [Unreleased]` part of `CHANGELOG.md` (I4). Run `python3 /home/aaronstatic/.claude/skills/asd-ste100/scripts/ste-lint.py --fail-over 2.5` on each changed doc (Q5).

---

## Testing Strategy

### Unit Tests
- `State/StoreTests` (9): the start value and version, a change, an equal value, the version sequence, a `record struct`, a class by reference, an equal string, `null`, and the `IBindingSource` read.
- `State/StoreSubscriptionTests` (10): the first read, no change, one change, an equal set, two sets before a read, another slice, two subscriptions, the array slot and the copy, the null checks, and a default value.

### Allocation Tests
- `State/StoreAllocationTests` (4): `Set` on three store types, the version read through `IBindingSource`, `TryGetChanged`, and the real-work check. element-bindings adds `BoundTreeAllocationTests` for the full binding pass.

### Regression Tests
- Each test of `BrutalUi.Core.Tests`, the ImGui tests, and the Glfw tests run with no edit. This feature changes no existing source file.

### Integration Tests
- `scripts/test-linux.sh` runs the full suite, with the Framework tests, and the HelloPanel smoke run. HelloPanel does not change. action-map adds the store to the sample.

### Manual Testing
- None in this feature. action-map adds the visible demo of a store in HelloPanel.

---

## Related Documentation

- **Requirements:** `docs/features/state-and-commands/store-and-subscription/requirements.md`
- **Epic:** `docs/features/state-and-commands/epic-overview.md`, `docs/features/state-and-commands/epic-requirements.md`
- **Siblings:** `docs/features/state-and-commands/element-bindings/requirements.md` (the binding list and the binding pass read this feature), `commands-and-events/requirements.md`, `action-map/requirements.md`
- **Design:** `docs/technical-design.md` sections 3 (project structure), 10 (Framework Layer, Data binding), and 14 (Testing)
- **Rules:** `AGENTS.md`, `.beast-mode/phase-handoff.md` sections 1 and 11, `.beast-mode/tool-rules.md`
- **Code patterns:** `src/BrutalUi.Core/Elements/Slider.cs` (a nested `CreateInfo` with one `required` field, a constructor with `in CreateInfo`), `src/BrutalUi.Core/Tree/UiContext.cs` (the `CreateInfo` of the context), `tests/BrutalUi.Core.Tests/Shared/AllocationGate.cs` (the source of the copy), and `tests/BrutalUi.Core.Tests/Elements/SliderAllocationTests.cs` (the gate shape: warm-up rounds, the release, the measure, and the real-work check)
- **Skills:** no implementation skill applies. The ASD-STE100 lint script is at `/home/aaronstatic/.claude/skills/asd-ste100/scripts/ste-lint.py`.

---

## Notes

### Open Scope Questions

None. The interview of `.beast-mode/interview.md` confirmed Q1 to Q4 and A1 to A8, and Q4 names the demo of phase 1.

### Conflicts and Gaps

1. **The seam for element-bindings (open).** `Element` lives in Core, and the binding list on `Element` cannot name `StoreSubscription<T, TValue>` or `Store<T>`. Also `Label` lives in Core, so `Label.BindText(store, selector)` cannot take a `Store<T>` as a member method. element-bindings decides the shape. Options: (a) A non-generic Core struct in the pooled list on `Element`: an `IBindingSource`, the applied version, and a static apply handler plus a target object, like the event slots. A Framework adapter class holds the `StoreSubscription` and is the target. The pass checks the version in Core and calls the handler only on a change. The adapter allocates one time at build time. (b) A Core interface for a binding (for example `bool Apply(Element)`), and a sealed generic Framework class that implements it. The list holds references, not structs, against the struct rule of the epic. (c) `BindText` as an extension method in `BrutalUi.Framework` that builds the adapter of (a) or (b). This feature gives `IBindingSource.Version`, `StoreSubscription.Store`, and `AppliedVersion` for each option, and it adds nothing to `Element` or `Label`.
2. **The demo of phase 1 and the script output (Q4).** `scripts/test-linux.sh` prints one PASS line on success and writes the test output to `artifacts/test-linux/test.log`. The PASS line proves that each test passed, also the Framework tests. The demo adds a `grep` of the log, so a person sees the Framework assembly by name. The script also runs the GPU check and the HelloPanel smoke run. On a machine with no NVIDIA GPU, the script fails after the test step. Then the person reads the test step in `artifacts/test-linux/test.log`. No script change (A8).
3. **A struct with no `IEquatable<T>`.** Q1 allows each `T`. Such a struct boxes in each `Set` (Decision 3). The remarks state it. A later feature can add a comparer to `CreateInfo` when a real store needs it.
4. **The version only, not the slice.** The requirement records the last version, so a change of another slice gives `true` (Decision 4). element-bindings decides a slice compare when a binding needs one.
5. **The transitive reference.** The Framework test project references Framework only. It sees `IBindingSource` through the transitive project reference to Core. When the build does not find the type, record it in `context.md` and stop. Do not add a project reference without a note, because I2 keeps each `.csproj` the same.
6. **The base commit.** HEAD at the time of the plan is `2ac76be` (docs: state-and-commands epic plan). Task 1.1 records the base commit, and each "no diff" check uses it.
7. **The prose lint.** The planner had no shell, so it did not run `ste-lint.py` on this file. Task 2.8 runs it on the changed docs.

---

*This plan will be updated as implementation progresses. See context.md and tasks.md for current status.*
