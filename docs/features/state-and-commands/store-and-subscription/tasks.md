# state-and-commands/store-and-subscription - Task Checklist

**Last Updated:** 2026-10-29 06:47
**Progress:** 14/14 tasks complete (100%)
**Advanced phases:** none. Phases 1 and 2 are STANDARD (sonnet).

"The plan" means `implementation.md` of this feature. Its sections "Architecture Overview" (The Pull Model, Public API, Internal Shape, The Seam for element-bindings) and "Key Technical Decisions" hold the rules that the tasks name.

Each phase ends with `dotnet build brutal-ui.sln 2>&1 | tail -n 30` and `dotnet test --solution brutal-ui.sln 2>&1 | tail -n 30` (tool rule 5). Expect `0 Warning(s)`, `0 Error(s)`, and 0 failed. The projects have no implicit usings, so each new file holds its own `using` lines. A warning fails the build. Each public member of `src/` needs an XML doc comment (CS1591 is an error). Use file-scoped namespaces (IDE0161). Prose in comments follows ASD-STE100. Run one class with `dotnet run --project tests/BrutalUi.Framework.Tests/BrutalUi.Framework.Tests.csproj -- --filter-class '*<Class>' 2>&1 | tail -n 5`.

**No-change rule:** no `.csproj`, `Directory.Build.props`, `Directory.Packages.props`, `brutal-ui.sln`, `scripts/`, `samples/`, or file in `tests/BrutalUi.Core.Tests/` changes. No existing file of `src/BrutalUi.Core/` changes. When the Framework test project does not see `IBindingSource` through the transitive reference, record it in `context.md` and stop (Notes item 5 of the plan).

---

## Phase 1: The Store and Its Version (6/6 complete)

**Demo:** a person runs `scripts/test-linux.sh` and reads its PASS line. Then the person runs `grep -n 'BrutalUi.Framework.Tests' artifacts/test-linux/test.log` and reads the result of the Framework test assembly with 0 failed (Q4).
**Needs:** none
**Agent tier:** STANDARD (sonnet)

- [x] ✅ **1.1 Record the base and the counts in context.md** - Completed 2026-10-29
  - Description: In `docs/features/state-and-commands/store-and-subscription/context.md`, record the base commit (`git rev-parse HEAD`), the test counts of the solution, of `BrutalUi.Core.Tests`, and of `BrutalUi.Framework.Tests` (1, the placeholder). Record the Pull Model and the Seam of the Architecture Overview of the plan.
  - File(s): `docs/features/state-and-commands/store-and-subscription/context.md`
  - Estimate: 5 steps

- [x] ✅ **1.2 Write IBindingSource.cs** - Completed 2026-10-29
  - Description: Write `src/BrutalUi.Core/Binding/IBindingSource.cs`: `public interface IBindingSource` in `namespace BrutalUi.Core;` with `ulong Version { get; }` (A1). The summary states: a source of bound state with a version counter. The remarks state: a reader keeps the version that it applied last and reads the source again when the version differs. The version never goes down. A new source starts at 1, so a reader that starts at 0 applies at its first read (A2). The source raises no event. A read allocates nothing. The interface names no Framework type, so the Core binding pass reads it (Q1 of the epic). Use no `<see cref>` to a Framework type.
  - File(s): `src/BrutalUi.Core/Binding/IBindingSource.cs`
  - Estimate: 3 steps

- [x] ✅ **1.3 Write Store.cs** - Completed 2026-10-29
  - Description: Write `src/BrutalUi.Framework/State/Store.cs`: `public sealed class Store<T> : IBindingSource` in `namespace BrutalUi.Framework;`, with the members of the Public API and the Internal Shape of the plan. `CreateInfo` holds one field, `public required T Initial;` (A4). The constructor takes `in CreateInfo info`. The class remarks state: the equality rule (`EqualityComparer<T>.Default`, Decision 2), the advice to use a type with `IEquatable<T>` for a struct `T` (a `record struct` has it), the reference rule for a class with no `Equals` override, the version rule (A2), the UI thread rule with no lock (A3), the pull model (Decision 1), "the store keeps no reference to a reader", and "the store replaces `INotifyBindablePropertyChanged` of mosaic-ui". The `Set` remarks state: an equal value changes nothing, also not the stored reference.
  - File(s): `src/BrutalUi.Framework/State/Store.cs`
  - Estimate: 5 steps

- [x] ✅ **1.4 Delete PlaceholderTests.cs** - Completed 2026-10-29
  - Description: Delete `tests/BrutalUi.Framework.Tests/PlaceholderTests.cs` (A7).
  - File(s): `tests/BrutalUi.Framework.Tests/PlaceholderTests.cs`
  - Estimate: 1 step

- [x] ✅ **1.5 Write StoreTests.cs (9 tests)** - Completed 2026-10-29
  - Description: Write `tests/BrutalUi.Framework.Tests/State/StoreTests.cs` in `namespace BrutalUi.Framework.Tests;` with 9 tests: `ANewStoreHoldsTheInitialValueAtVersionOne`, `ASetOfANewValueRaisesTheVersionByOne` (0 to 10: `Value` 10, `Version` 2), `ASetOfAnEqualValueKeepsTheVersion` (10 then 10: `Version` stays), `EachChangeRaisesTheVersionByOne` (the values 1, 2, 2, 1 from the start 1: the versions 1, 2, 2, 3), `ARecordStructComparesByValue` (a private `record struct Pair(int A, int B)`), `AClassWithNoEqualsComparesByReference`, `AnEqualStringKeepsTheVersionAndTheOldReference` (`Assert.Same` shows the old reference), `ANullValueComparesSafely` (a `Store<string?>`), and `TheStoreIsABindingSource`. The plan task 1.5 holds the full text of each test.
  - File(s): `tests/BrutalUi.Framework.Tests/State/StoreTests.cs`
  - Estimate: 6 steps

- [x] ✅ **1.6 Run the gate and the demo, and record the results** - Completed 2026-10-29
  - Description: Run the build and the tests. Run the `StoreTests` class and expect a total of 9 with `failed: 0`. Run `scripts/test-linux.sh; echo "exit=$?"` and expect the PASS line and `exit=0`. Run `grep -n 'BrutalUi.Framework.Tests' artifacts/test-linux/test.log`. Record the counts and the grep line in `context.md`.
  - File(s): `docs/features/state-and-commands/store-and-subscription/context.md`
  - Estimate: 5 steps

---

## Phase 2: The Subscription and the 0-Byte Gate (8/8 complete)

**Demo:** a unit test creates a `Store<GameState>` and a subscription with the static selector `static s => s.Score`. The first `TryGetChanged` gives `true` and the start score, the second gives `false`, and a `Set` of a new score gives `true` and the new score one time. The developer runs `dotnet run --project tests/BrutalUi.Framework.Tests/BrutalUi.Framework.Tests.csproj -- --filter-class '*StoreSubscriptionTests'` and reads a total of 10 with `failed: 0`, and the same command with `'*StoreAllocationTests'` reads a total of 4 with `failed: 0`.
**Needs:** Phase 1
**Agent tier:** STANDARD (sonnet)

*Group A (the subscription):*

- [x] ✅ **2.1 Write StoreSubscription.cs** - Completed 2026-10-29
  - Description: Write `src/BrutalUi.Framework/State/StoreSubscription.cs`: `public struct StoreSubscription<T, TValue>` with the members of the Public API and the Internal Shape of the plan (Q3). The constructor calls `ArgumentNullException.ThrowIfNull` for the store and the selector. Mark the getters `readonly`. Put `[MaybeNullWhen(false)]` on the `out` parameter. The remarks state: the first read applies (A2). One `true` for each version change, with the slice of the newest value. A change of another slice of the same store also gives `true` (Decision 4). The struct is mutable, so call `TryGetChanged` on a variable, an array slot, or a `ref`, never on a copy or a `readonly` field (Decision 5). Pass a `static` lambda or a cached method group, and a selector must not allocate (Decision 6). A `default` value holds no store and throws (Decision 7). The UI thread rule (A3).
  - File(s): `src/BrutalUi.Framework/State/StoreSubscription.cs`
  - Estimate: 5 steps

- [x] ✅ **2.2 Write StoreSubscriptionTests.cs (10 tests)** - Completed 2026-10-29
  - Description: Write `tests/BrutalUi.Framework.Tests/State/StoreSubscriptionTests.cs` with 10 tests on a private `record struct GameState(int Score, int Lives)`: `TheFirstReadAppliesTheCurrentValue`, `ASecondReadWithNoChangeGivesNothing`, `ASetOfANewSliceGivesOneChange`, `AnEqualSetGivesNoChange`, `TwoSetsBeforeOneReadGiveOneChangeWithTheLastValue`, `AChangeOfAnotherSliceGivesAChangeWithTheSameSlice`, `TwoSubscriptionsOnOneStoreKeepTheirOwnVersion`, `ASubscriptionInAnArraySlotKeepsItsVersion`, `TheConstructorRejectsANullStoreOrSelector`, and `ADefaultSubscriptionThrows`. Each selector is a `static` lambda. The plan task 2.2 holds the full text of each test.
  - File(s): `tests/BrutalUi.Framework.Tests/State/StoreSubscriptionTests.cs`
  - Estimate: 6 steps

- [x] ✅ **2.3 Run the gate for group A, and record the results** - Completed 2026-10-29
  - Description: Run the build, the tests, the class `StoreSubscriptionTests`, and `StoreTests`. Record the results in `context.md`.
  - File(s): `docs/features/state-and-commands/store-and-subscription/context.md`
  - Estimate: 4 steps

*Group B (the gate and the docs):*

- [x] ✅ **2.4 Copy AllocationGate.cs into the Framework tests** - Completed 2026-10-29
  - Description: Write `tests/BrutalUi.Framework.Tests/Shared/AllocationGate.cs`: a copy of `tests/BrutalUi.Core.Tests/Shared/AllocationGate.cs` with `namespace BrutalUi.Framework.Tests;` (A6). Keep the code and the first remarks paragraph. Replace the second paragraph: this file is a copy of the Core.Tests helper, because the Framework tests do not reference Core.Tests. Keep the two files the same in code.
  - File(s): `tests/BrutalUi.Framework.Tests/Shared/AllocationGate.cs`
  - Estimate: 3 steps

- [x] ✅ **2.5 Write StoreAllocationTests.cs (4 tests)** - Completed 2026-10-29
  - Description: Write `tests/BrutalUi.Framework.Tests/State/StoreAllocationTests.cs` with 4 tests in the shape of `tests/BrutalUi.Core.Tests/Elements/SliderAllocationTests.cs`: `SetAllocatesNothing`, `AVersionReadAllocatesNothing`, `TryGetChangedAllocatesNothing`, and `TheMeasuredBodyDoesRealWork`. The scene holds a `Store<int>`, a `Store<GameState>` (a `record struct`), a `Store<string>` with two strings that the scene built, a subscription with a static selector, and an `IBindingSource` reference to each store. The class remarks state the scene and the rule: the measured bodies create no lambda, no string, no boxed value, and no `Assert`. The plan task 2.5 holds the full text.
  - File(s): `tests/BrutalUi.Framework.Tests/State/StoreAllocationTests.cs`
  - Estimate: 6 steps

- [x] ✅ **2.6 Edit sections 10 and 14 of the technical design** - Completed 2026-10-29
  - Description: Edit section 10 of `docs/technical-design.md`, "Data binding": add the store rules (one value, the version from 1, the equality rule, `Set` only, no event) and the subscription rules (the applied version from 0, `TryGetChanged`, the static selector, the struct copy rule). State that `IBindingSource` in Core holds the version only. Edit section 14: add one bullet for `StoreAllocationTests` in `tests/BrutalUi.Framework.Tests/`.
  - File(s): `docs/technical-design.md`
  - Estimate: 4 steps

- [x] ✅ **2.7 Add the changelog line** - Completed 2026-10-29
  - Description: Edit `CHANGELOG.md`: under `## [Unreleased]`, `### Added`, add one line: `IBindingSource` in Core, `Store<T>` and `StoreSubscription<T, TValue>` in Framework, and the Framework allocation gate (state-and-commands/store-and-subscription).
  - File(s): `CHANGELOG.md`
  - Estimate: 2 steps

- [x] ✅ **2.8 Run the gate, the plant, the script, and the lint, and record the results** - Completed 2026-10-29
  - Description: Run the build, the tests, the classes `StoreSubscriptionTests` and `StoreAllocationTests`, and `StoreTests`. Plant `GC.KeepAlive(new object());` as the first line of `Store<T>.Set`, run `dotnet test --solution brutal-ui.sln > /dev/null 2>&1; echo "exit=$?"`, and expect a value that is not 0. Remove the plant and run the tests again. Run `scripts/test-linux.sh; echo "exit=$?"` and expect the PASS line and `exit=0`. Run `python3 /home/aaronstatic/.claude/skills/asd-ste100/scripts/ste-lint.py --fail-over 2.5` on the changed docs. Record the results and each decision that the plan did not settle in `context.md`.
  - File(s): `src/BrutalUi.Framework/State/Store.cs` (plant, then restore), `docs/features/state-and-commands/store-and-subscription/context.md`
  - Estimate: 8 steps

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

## Cross-Phase Review

- ✅ **CR-1, CR-2, CR-3 of `code-review.md`** - Completed 2026-10-29 (not counted in the 14 tasks)
  - Fix groups `review-g1.md` (CR-2, CR-3, follow-up 1) and `review-g2.md` (CR-1, follow-ups 2 and 3). Doc comments and section 10 only. The review verifier returns PASS.

---

## Progress Tracking

### Completed This Session (2026-10-29)
- ✅ Phase 1 (tasks 1.1 to 1.6) and phase 2 (tasks 2.1 to 2.8)
- ✅ The cross-phase review: 3 LOW findings, 3 fixed
- ✅ The draft plan of the next feature, `state-and-commands/element-bindings/implementation.md`

### Discovered New Tasks
- none

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
