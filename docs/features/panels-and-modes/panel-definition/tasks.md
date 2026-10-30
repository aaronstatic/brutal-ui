# panels-and-modes/panel-definition - Task Checklist

**Last Updated:** 2026-10-30 05:32
**Progress:** 13/13 tasks complete (100%)
**Advanced phases:** none. Phase 1 is STANDARD (sonnet). Escalate to ADVANCED only when the compiler rejects the record shape of Decision 2 and the fallback of the Risks table does not fix it.

"The plan" means `implementation.md` of this feature. Its sections "Architecture Overview" (Public API, Internal Shape, Integration Points, Data Flow) and "Key Technical Decisions" hold the rules that the tasks name.

Each group ends with `dotnet build brutal-ui.sln 2>&1 | tail -n 30` and `dotnet test --solution brutal-ui.sln 2>&1 | tail -n 30` (tool rule 5). Expect `0 Warning(s)`, `0 Error(s)`, and 0 failed. A warning fails the build (`TreatWarningsAsErrors`). Each public member of `src/` needs an XML doc comment (CS1591 is an error). The projects have no implicit usings, so each new file holds its own `using` lines (`System`, `System.Text`, `System.Diagnostics.CodeAnalysis`, `BrutalUi.Core`), also for each namespace that a `cref` names. Use file-scoped namespaces. The source files use the namespace `BrutalUi.Framework`, and the test files use `BrutalUi.Framework.Tests`. Prose in comments follows ASD-STE100. Run one class with `dotnet run --project tests/BrutalUi.Framework.Tests/BrutalUi.Framework.Tests.csproj -- --filter-class '*<Class>' 2>&1 | tail -n 5`.

**No-change rule:** these files do not change: each file of `src/BrutalUi.Core/`, each other file of `src/BrutalUi.Framework/`, each `.csproj`, `Directory.Build.props`, `Directory.Packages.props`, `brutal-ui.sln`, `scripts/`, `samples/`, and each existing test file.

---

## Phase 1: The Panel Data Model and the One-Time Root (13/13 complete)

**Demo:** a developer runs `scripts/test-linux.sh` and reads its PASS line, and the new Panels tests pass: `dotnet run --project tests/BrutalUi.Framework.Tests/BrutalUi.Framework.Tests.csproj -- --filter-class '*PanelRecordTests'` reads a total of 14 and `--filter-class '*PanelControllerTests'` reads a total of 10, each with `failed: 0`.
**Needs:** none
**Agent tier:** STANDARD (sonnet)

Group A: the records (tasks 1.1 to 1.7).

- [x] **1.1 Record the base, the counts, the Internal Shape, and the Integration Points in context.md**
  - Description: `/start-feature` created `context.md`. Check that the epic plan commit exists (`git log --oneline -3`, expect `43d3ea0` or later). Record the base commit (`git rev-parse HEAD`), the test count of the solution (with the skipped count), and the count of `BrutalUi.Framework.Tests` in the section "Base and Counts". Record the Internal Shape and the Integration Points of the plan.
  - File(s): `docs/features/panels-and-modes/panel-definition/context.md`
  - Estimate: 4 steps

- [x] **1.2 Write PanelContext.cs**
  - Description: Write `src/BrutalUi.Framework/Panels/PanelContext.cs`: the sealed class of the Public API with the three `required` init members and the null check of the Internal Shape. The class remarks: the game creates one context at build time and gives it to each controller (mode-set gives it to the manager). The factory of a panel or a layout reads the registries from it. The context holds no other state and owns no registry. It runs on the UI thread.
  - File(s): `src/BrutalUi.Framework/Panels/PanelContext.cs`
  - Estimate: 4 steps

- [x] **1.3 Write PanelDefinition.cs**
  - Description: Write `src/BrutalUi.Framework/Panels/PanelDefinition.cs`: the sealed record of the Public API, with the name copy, the validation, the explicit `PrintMembers`, and the remarks. The remarks state: the record copies each name at build time. The factory runs one time for each controller, at the first mount, so a closure costs one build-time allocation only. The factory can bind the root to a store (A7). `DefaultSlot` names the slot of the panel when a mode does not name one (mode-set uses it). The equality rule and the text rule of the Internal Shape. This task is the first compile of the record shape (Conflicts and Gaps item 8).
  - File(s): `src/BrutalUi.Framework/Panels/PanelDefinition.cs`
  - Estimate: 6 steps

- [x] **1.4 Write LayoutDefinition.cs**
  - Description: Write `src/BrutalUi.Framework/Panels/LayoutDefinition.cs`: the sealed record of the Public API, in the shape of task 1.3. The remarks: the root of the layout holds the `SlotContainer` elements of slot-container, and the manager finds each one by name (Q4 of the epic). The layout root is the one `UiContext` root (A4 of the epic). The factory gets the same `PanelContext` as the panels.
  - File(s): `src/BrutalUi.Framework/Panels/LayoutDefinition.cs`
  - Estimate: 4 steps

- [x] **1.5 Write ModeDefinition.cs (ModePanel and ModeDefinition)**
  - Description: Write `src/BrutalUi.Framework/Panels/ModeDefinition.cs`: the readonly struct `ModePanel` and the sealed record `ModeDefinition` of the Public API and the Internal Shape (the grouping, the duplicate check, the index checks, `PrintMembers`). The remarks of `ModeDefinition`: the slot order is the order of the first entry of each slot. The panel order of a slot is the order of the list (A4). A panel is in one slot of a mode at most. `ActionMap` is optional, and mode-set activates it (A6 of the epic). The record checks no name against a registered panel or layout. Each getter allocates nothing. The remarks of `ModePanel`: one placement of a panel in a slot, an input of the `ModeDefinition` constructor. A UTF-8 literal is a span and cannot sit in an array, so the struct copies the two names.
  - File(s): `src/BrutalUi.Framework/Panels/ModeDefinition.cs`
  - Estimate: 8 steps

- [x] **1.6 Write PanelRecordTests.cs (14 tests)**
  - Description: Write `tests/BrutalUi.Framework.Tests/Panels/PanelRecordTests.cs` with 14 tests. Use a private static factory method `static Element NewRoot(PanelContext context) => new Element();` in a `static readonly Func<PanelContext, Element>` field, and a private helper that builds a `PanelContext` with three new registries. Tests: `PanelContextHoldsTheThreeRegistries`, `PanelContextRejectsANullRegistry` (each member, `ArgumentNullException`), `APanelDefinitionHoldsItsNamesAndItsFactory` (`SequenceEqual` of both names, `Assert.Same` of the factory), `APanelDefinitionCopiesItsNames` (build from two `byte[]` sources, change a byte of each source, read the old names), `APanelDefinitionRejectsAnEmptyNameAnEmptySlotAndANullFactory` (`ArgumentException` with `ParamName` `name` and `defaultSlot`, and `ArgumentNullException` with `createRoot`), `ALayoutDefinitionHoldsACopyOfItsNameAndItsFactory`, `ALayoutDefinitionRejectsAnEmptyNameAndANullFactory`, `AModeDefinitionGroupsThePanelsBySlotInTheOrderOfTheList` (entries left:a, right:c, left:b give `SlotCount` 2, slot 0 `left` with `a`, `b`, and slot 1 `right` with `c`), `AModeDefinitionHoldsItsLayoutAndItsOptionalActionMap` (`Assert.Same` of a map, and `null` with no map), `AModeDefinitionCopiesItsNames`, `AModeDefinitionWithNoPanelHoldsNoSlot`, `AModeDefinitionRejectsAnEmptyNameAnEmptyLayoutAnEmptyEntryAndAPanelThatItListsTwoTimes` (also `default(ModePanel)`, and one panel in the same slot and in two slots), `TheModeGettersRejectAnIndexOutsideTheRange` (-1 and the count, for `slot` and for `index`), and `ToStringShowsTheNamesAsText` (`PanelDefinition { Name = inventory, DefaultSlot = left }` and the two other formats of the Internal Shape).
  - File(s): `tests/BrutalUi.Framework.Tests/Panels/PanelRecordTests.cs`
  - Estimate: 10 steps

- [x] **1.7 Run the build and the tests for group A**
  - Description: Run the build, the tests, and `PanelRecordTests` (total 14). Expect `failed: 0`, and each other test passes with no edit. Record the results in `context.md`.
  - File(s): `docs/features/panels-and-modes/panel-definition/context.md`
  - Estimate: 4 steps

Group B: the controller and the docs (tasks 1.8 to 1.13).

- [x] **1.8 Write PanelInstance.cs**
  - Description: Write `src/BrutalUi.Framework/Panels/PanelInstance.cs`: the sealed class of the Public API and the Internal Shape. The remarks: the controller creates the one instance and writes `Root` one time. `Root` is `null` until the first `EnsureRoot`. After that, it is the same `Element` for the life of the controller, also while the root is detached from each slot (Q3). slot-container mounts `Root`. After this task, record in `context.md` that the shape of `PanelInstance` is set, so slot-container can start (requirements, Dependencies).
  - File(s): `src/BrutalUi.Framework/Panels/PanelInstance.cs`, `docs/features/panels-and-modes/panel-definition/context.md`
  - Estimate: 4 steps

- [x] **1.9 Write PanelController.cs**
  - Description: Write `src/BrutalUi.Framework/Panels/PanelController.cs`: the sealed class of the Public API with `EnsureRoot` of the Internal Shape. The remarks state: the controller owns one `PanelInstance` and reaches the registries through `Context`. `EnsureRoot` calls the factory one time at the first call and gives the same root after that, with 0 bytes. A factory exception leaves the call, the controller stays unbuilt, and the next call tries again (Q3). A `null` root throws `InvalidOperationException`, and the controller stays unbuilt. A call from inside the factory throws `InvalidOperationException`. The controller keeps the root for its life and never detaches it. slot-container and mode-set call `EnsureRoot` at the mount (A2). The controller does not bind the root to a store. The factory does that (A7).
  - File(s): `src/BrutalUi.Framework/Panels/PanelController.cs`
  - Estimate: 5 steps

- [x] **1.10 Write PanelControllerTests.cs (10 tests)**
  - Description: Write `tests/BrutalUi.Framework.Tests/Panels/PanelControllerTests.cs` with 10 tests. Use a private sealed `RootFactory` class with an `int Calls` field, a `PanelContext? Seen` field, a `bool Throw` field, and a `bool ReturnNull` field, and an instance method `Element Create(PanelContext context)` that the test passes as a method group at build time. Tests: `EnsureRootBuildsTheRootOneTime` (the demo test: the first call gives a root and `Calls` 1, `IsBuilt` is `true`, and `Instance.Root` is the same element. A second call gives the same element and `Calls` stays 1), `ANewControllerHoldsAnUnbuiltInstance` (`IsBuilt` `false`, `Root` `null`, `Definition` and `Instance.Definition` are the definition, and `Context` is the context), `TheFactoryGetsTheContextOfTheController` (`Seen` is the same context, and the factory reads `Commands`, `Events`, and `Services`), `AFactoryThatThrowsLeavesTheControllerUnbuiltAndTheNextCallTriesAgain` (the first call throws the exception of the factory with no wrap, and `IsBuilt` stays `false`. With `Throw` set to `false`, the next call builds, and `Calls` is 2), `AFactoryThatReturnsNullThrowsAndLeavesTheControllerUnbuilt` (`InvalidOperationException`), `ACallFromInsideTheFactoryThrows` (a factory that calls `EnsureRoot` of its own controller gets `InvalidOperationException`, stores it, and returns a root. The outer call builds), `TwoControllersOfOneDefinitionBuildTwoRoots`, `TheRootStaysTheSameAfterADetach` (`UiContext.AddRoot` of the root, then `RemoveRoot`. `EnsureRoot` gives the same element, and `Calls` stays 1), `TheConstructorRejectsANullDefinitionOrContext`, and `EnsureRootAndTheReadsAllocateNothingAfterTheBuild`. The last test builds a controller, a mode with three entries in two slots, and a layout before the measure. It runs 2 warm-up rounds, calls `AllocationGate.ReleaseAllocationContext()` right before the measure, and runs 100 iterations of: `EnsureRoot`, `IsBuilt`, `Instance.Root`, the length of each name of the panel and the layout, and each getter of the mode over each slot and panel. It adds each length and count to a local `int` and asserts `Assert.Equal(0L, after - before)` and the sum after the measure. The measured body creates no lambda, no string, no array, and no `Assert`, as the rule of `CommandAndEventAllocationTests`.
  - File(s): `tests/BrutalUi.Framework.Tests/Panels/PanelControllerTests.cs`
  - Estimate: 10 steps

- [x] **1.11 Run the group B tests and the two plants**
  - Description: Run the build, the tests, and `PanelControllerTests` (total 10). Expect `failed: 0`. Run two plants. Copy `PanelController.cs` to the scratchpad first. Plant 1: `GC.KeepAlive(new object());` as the first line of `EnsureRoot`. Expect `EnsureRootAndTheReadsAllocateNothingAfterTheBuild` to fail. Plant 2: remove the early `return` of the warm path. Expect `EnsureRootBuildsTheRootOneTime` to fail. For each plant, run `dotnet test --solution brutal-ui.sln > /dev/null 2>&1; echo "exit=$?"` and expect a value that is not 0. Restore the file and check it with `cmp`. Run the tests again and expect 0 failed. Record the results in `context.md`.
  - File(s): `src/BrutalUi.Framework/Panels/PanelController.cs` (plant and restore), `docs/features/panels-and-modes/panel-definition/context.md`
  - Estimate: 8 steps

- [x] **1.12 Edit the technical design and the changelog**
  - Description: Edit `docs/technical-design.md`. Section 10: add a sub-section `### Panel definitions` after "Commands, events, and services" and before the `---` line. List the rules of this feature: the records and their names (Q4), the validation (A5), the equality rule and the text rule, the mode slot order (A4), the context (Q2), the controller and `EnsureRoot` (A2, Q3), the error rule, the thread rule, and the 0-byte rule. End with one sentence: `PanelRecordTests` and `PanelControllerTests` guard the rules (section 14). In the Components table, change the `ModeHistory` cell text only when it names a wrong feature (expect no change). Section 14: add a bullet `**Panel tests.**` after the bullet "A seventeenth allocation gate" and before "Sample projects". Name the two classes, their counts, the 0-byte test, and the two plants of task 1.11. Do not call the 0-byte test a numbered gate (Decision 9). Edit `CHANGELOG.md`: under `## [Unreleased]`, `### Added`, add one line: `PanelDefinition`, `LayoutDefinition`, `ModeDefinition`, `ModePanel`, `PanelContext`, `PanelInstance`, and `PanelController` (panels-and-modes/panel-definition).
  - File(s): `docs/technical-design.md`, `CHANGELOG.md`
  - Estimate: 6 steps

- [x] **1.13 Run the test script, the demo, and the prose lint**
  - Description: Run `scripts/test-linux.sh; echo "exit=$?"` and expect the PASS line and `exit=0`. Run the two demo commands of the phase and expect the totals 14 and 10 with `failed: 0`. Run `python3 /home/aaronstatic/.claude/skills/asd-ste100/scripts/ste-lint.py --fail-over 2.5` on `docs/technical-design.md`, `CHANGELOG.md`, `context.md`, and the plan, and fix the prose until each file passes. Record the demo, the results, and each decision that the plan did not settle in `context.md`.
  - File(s): `docs/features/panels-and-modes/panel-definition/context.md`, `docs/features/panels-and-modes/panel-definition/implementation.md` (prose only)
  - Estimate: 6 steps

---

## Bugs & Issues

**Active Bugs:**
- (none)

---

## Technical Debt

- (none)

---

## Task Status Legend

- [ ] Not started
- [x] ✅ Completed

---

## Progress Tracking

### Completed This Session (2026-10-30)
- ✅ Phase 1 group A (tasks 1.1 to 1.7): the records, `PanelContext`, and `PanelRecordTests` (14).
- ✅ Phase 1 group B (tasks 1.8 to 1.13): `PanelInstance`, `PanelController`, `PanelControllerTests` (10), the two plants, the technical design, and the changelog.
- ✅ The phase 1 verifier: PASS, Demo: PASS.
- ✅ The cross-phase review (`code-review.md`): 4 findings (1 MEDIUM, 3 LOW), 4 fixed in review groups 1 to 3. The review verifier: PASS, Demo: PASS.

### Discovered New Tasks
- (none)

---

## Notes

### Task Estimation
One step is one tool call of an agent. A typical task costs about six steps (see `.beast-mode/phase-handoff.md` section 1).

---

*Update this file as tasks are completed. Mark tasks with ✅ immediately when done. Add new tasks as they're discovered.*
