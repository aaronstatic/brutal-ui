# panels-and-modes/slot-container - Task Checklist

**Last Updated:** 2026-10-30 07:35
**Progress:** 10/10 tasks complete (100%)
**Advanced phases:** none. Phase 1 and phase 2 are STANDARD (sonnet).

"The plan" means `implementation.md` of this feature. Its sections "Architecture Overview" (Public API, Internal Shape, Integration Points, Data Flow) and "Key Technical Decisions" hold the rules that the tasks name.

Each group ends with `MSBUILDDISABLENODEREUSE=1 dotnet build brutal-ui.sln -nodeReuse:false 2>&1 | tail -n 30` and `MSBUILDDISABLENODEREUSE=1 dotnet test --solution brutal-ui.sln 2>&1 | tail -n 30` (tool rule 5). Expect `0 Warning(s)`, `0 Error(s)`, and 0 failed. Do not run `dotnet build-server shutdown`. A warning fails the build (`TreatWarningsAsErrors`). Each public member of `src/` needs an XML doc comment (CS1591 is an error). The projects have no implicit usings, so each new file holds its own `using` lines (`System`, `System.Text`, `BrutalUi.Core`), also for each namespace that a `cref` names. Use file-scoped namespaces. The source file uses the namespace `BrutalUi.Framework`, and the test files use `BrutalUi.Framework.Tests`. Prose in comments follows ASD-STE100. Run one class with the built binary: `tests/BrutalUi.Framework.Tests/bin/Debug/net10.0/BrutalUi.Framework.Tests --filter-class '*<Class>' 2>&1 | tail -n 5`. The test files use unique class names for each test (`ClassName.Intern(nameof(TestName) + ".slot")`).

**No-change rule:** these files do not change: each file of `src/BrutalUi.Core/`, each other file of `src/BrutalUi.Framework/` (also the six panel-definition files), each `.csproj`, `Directory.Build.props`, `Directory.Packages.props`, `brutal-ui.sln`, `scripts/`, `samples/`, and each existing test file.

---

## Phase 1: A Slot That Holds Panel Roots in Order (4/4 complete)

**Demo:** a developer runs `tests/BrutalUi.Framework.Tests/bin/Debug/net10.0/BrutalUi.Framework.Tests --filter-class '*SlotContainerTests'` after a build and reads a total of 10 with `failed: 0`.
**Needs:** none
**Agent tier:** STANDARD (sonnet)

One group (tasks 1.1 to 1.4).

- [x] **1.1 Record the base, the counts, the Internal Shape, and the Integration Points in context.md**
  - Description: Check that the panel-definition files exist in `src/BrutalUi.Framework/Panels/` (six files). Record the base commit (`git rev-parse HEAD`), the test count of the solution (with the skipped count), and the count of `BrutalUi.Framework.Tests` in the section "Base and Counts". At planning time, the counts were 2750 (3 skipped) and 157. Record the Internal Shape and the Integration Points of the plan as a short summary.
  - File(s): `docs/features/panels-and-modes/slot-container/context.md`
  - Estimate: 4 steps

- [x] **1.2 Write SlotContainer.cs with the constructor, SlotName, TypeId, the element name, Mount, and Unmount**
  - Description: Write `src/BrutalUi.Framework/Panels/SlotContainer.cs` with the constructor, `SlotName`, `TypeId`, the element name, `Mount`, and `Unmount` of the Public API and the Internal Shape. Do not add `Find` yet. The class remarks: the slot holds the panel roots of one slot as its children, in the order of the mode definition (A4 of the epic). The style pass and the layout pass treat it as a plain `Element`, and a stylesheet styles it by the type name `SlotContainer`, by class, or by name. The constructor sets `Name` to the text of the slot name, and a later set of `Name` does not change `SlotName`. Change the children through `Mount` and `Unmount` only. The slot runs on the UI thread. The remarks of `Mount`: the index range (0 to `Count` for a new root, 0 to `Count` minus 1 for a held root), the move with no join, the parent rule, each exception, and "each throw comes before a change". The remarks of `Unmount`: the keep rule and the `false` result. Each remark names the allocation rule: 0 bytes after the child array of the slot holds the largest count.
  - File(s): `src/BrutalUi.Framework/Panels/SlotContainer.cs`
  - Estimate: 6 steps

- [x] **1.3 Write SlotContainerTests.cs with 10 tests**
  - Description: Write `tests/BrutalUi.Framework.Tests/Panels/SlotContainerTests.cs` with the 10 tests of task 1.3 of the plan: `ANewSlotHoldsACopyOfItsNameAndNoChild`, `TheConstructorRejectsAnEmptyName`, `TheTypeIsSlotContainerAndTheNameIsTheTextOfTheSlotName`, `MountPutsEachRootAtItsIndex`, `AMountOfAHeldRootMovesItWithNoJoin`, `UnmountDetachesTheRootAndKeepsItsState`, `UnmountOfARootThatTheSlotDoesNotHoldReturnsFalse`, `AMountOfARootThatAnotherSlotHoldsThrows`, `AMountOfARootWithAnotherParentThrows`, and `TheMountRejectsANullRootABadIndexAContextRootAndACycle`. The plan states the asserts of each test.
  - File(s): `tests/BrutalUi.Framework.Tests/Panels/SlotContainerTests.cs`
  - Estimate: 8 steps

- [x] **1.4 Run the build, the tests, the demo, and plants 1 and 2**
  - Description: Run the build, the tests, and `SlotContainerTests` (total 10). Expect `failed: 0`, and each other test passes with no edit. Copy `SlotContainer.cs` to the scratchpad first. Plant 1: remove the `throw` of the parent rule. Expect `AMountOfARootThatAnotherSlotHoldsThrows` and `AMountOfARootWithAnotherParentThrows` to fail. Plant 2: replace `Children.Insert(index, root)` with `Children.Add(root)`. Expect `MountPutsEachRootAtItsIndex` to fail. For each plant, run `MSBUILDDISABLENODEREUSE=1 dotnet test --solution brutal-ui.sln > /dev/null 2>&1; echo "exit=$?"` and expect a value that is not 0. Restore the file and check it with `cmp`. Run the tests again and expect 0 failed. Record the demo and the results in `context.md`.
  - File(s): `src/BrutalUi.Framework/Panels/SlotContainer.cs` (plants only, restored), `docs/features/panels-and-modes/slot-container/context.md`
  - Estimate: 7 steps

---

## Phase 2: The Slot Lookup and the Plain Element Proof (6/6 complete)

**Demo:** a developer runs `MSBUILDDISABLENODEREUSE=1 scripts/test-linux.sh` and reads its PASS line, and the built binary reads a total of 8 for `--filter-class '*SlotLookupTests'` and 5 for `--filter-class '*SlotContainerTreeTests'`, each with `failed: 0`.
**Needs:** Phase 1
**Agent tier:** STANDARD (sonnet)

One group (tasks 2.1 to 2.6).

- [x] **2.1 Add Find to SlotContainer.cs**
  - Description: Add `Find` to `src/BrutalUi.Framework/Panels/SlotContainer.cs`: the public static method of the Public API and the private static walk of the Internal Shape. The remarks: the walk checks the root itself, then each child in depth-first pre-order. It does not enter the children of a slot, so the result depends on the layout tree only. The first match wins, and two slots with one name are a bug of the layout. The compare is by bytes, and `Find` reads `SlotName`, not `Name`. An empty name or a missing name gives `null`. The walk allocates nothing, so mode-set can call it at build time and at the first diff, and cache the result (Q4 of the epic). A `null` root throws `ArgumentNullException`.
  - File(s): `src/BrutalUi.Framework/Panels/SlotContainer.cs`
  - Estimate: 4 steps

- [x] **2.2 Write SlotLookupTests.cs with 8 tests**
  - Description: Write `tests/BrutalUi.Framework.Tests/Panels/SlotLookupTests.cs` with the 8 tests of task 2.2 of the plan: `FindReturnsTheSlotOfTheName`, `FindChecksTheRootItself`, `FindReturnsNullForAMissingName`, `FindReadsTheSlotNameAndNotTheElementName`, `FindReturnsTheFirstSlotInTreeOrder`, `FindDoesNotEnterTheChildrenOfASlot`, `FindRejectsANullRoot`, and `MountUnmountAndFindAllocateNothingAfterWarmUp`. The plan states the helper, the asserts, and the shape of the 0-byte test (2 warm-up rounds, `AllocationGate.ReleaseAllocationContext()`, 100 iterations, 0 bytes, a fixed sum).
  - File(s): `tests/BrutalUi.Framework.Tests/Panels/SlotLookupTests.cs`
  - Estimate: 8 steps

- [x] **2.3 Write SlotContainerTreeTests.cs with 5 tests**
  - Description: Write `tests/BrutalUi.Framework.Tests/Panels/SlotContainerTreeTests.cs` with the 5 tests of task 2.3 of the plan: `AStylesheetStylesASlotByTypeName`, `AStylesheetStylesASlotByClassAndByName`, `TheLayoutPassPlacesTheMountedRootsInTheirOrder`, `APanelThatMovesToAnotherSlotTakesTheStyleOfTheNewSlot`, and `AnUnmountedPanelKeepsItsBindingAndAppliesTheNewValueAfterTheMount`. Each test builds a `UiContext` with a `RecordingRenderer`, a viewport of 400 by 300, and a layout root of 400 by 300 with `FlexDirection.Column`.
  - File(s): `tests/BrutalUi.Framework.Tests/Panels/SlotContainerTreeTests.cs`
  - Estimate: 8 steps

- [x] **2.4 Run the build, the tests, and plants 3 to 5**
  - Description: Run the build, the tests, `SlotLookupTests` (total 8), and `SlotContainerTreeTests` (total 5). Expect `failed: 0`. Copy `SlotContainer.cs` to the scratchpad first. Plant 3: `GC.KeepAlive(new object());` as the first line of `Find`. Expect `MountUnmountAndFindAllocateNothingAfterWarmUp` to fail. Plant 4: the same line as the first line of `Mount`. Expect the same test to fail. Plant 5: remove the early `return` of the slot branch in the walk, so the walk enters the children of a slot when the name does not match. Expect `FindDoesNotEnterTheChildrenOfASlot` to fail. For each plant, run `MSBUILDDISABLENODEREUSE=1 dotnet test --solution brutal-ui.sln > /dev/null 2>&1; echo "exit=$?"` and expect a value that is not 0. Record the failed tests and the measured bytes. Restore the file and check it with `cmp`. Run the tests again and expect 0 failed. Record the results in `context.md`.
  - File(s): `src/BrutalUi.Framework/Panels/SlotContainer.cs` (plants only, restored), `docs/features/panels-and-modes/slot-container/context.md`
  - Estimate: 8 steps

- [x] **2.5 Edit the technical design and the changelog**
  - Description: Edit `docs/technical-design.md`. Section 10: add a sub-section `### Slot containers` after "Panel definitions" and before the `---` line, with the rules of task 2.5 of the plan. End with one sentence: `SlotContainerTests`, `SlotLookupTests`, and `SlotContainerTreeTests` guard the rules (section 14). Do not change the Components table. Section 14: add a bullet `**Slot tests.**` after the bullet "Panel tests" and before "Sample projects". Name the three classes, their counts, the 0-byte test, and the plants of tasks 1.4 and 2.4 with their results. Do not call the 0-byte test a numbered gate (Decision 8). Edit `CHANGELOG.md`: under `## [Unreleased]`, `### Added`, add one line after the panel-definition line: `SlotContainer` with `Mount`, `Unmount`, and `SlotContainer.Find` (panels-and-modes/slot-container).
  - File(s): `docs/technical-design.md`, `CHANGELOG.md`
  - Estimate: 6 steps

- [x] **2.6 Run test-linux.sh, the demos, and the prose lint**
  - Description: Run `MSBUILDDISABLENODEREUSE=1 scripts/test-linux.sh; echo "exit=$?"` and expect the PASS line and `exit=0`. Run the demo commands of both phases and expect the totals 10, 8, and 5 with `failed: 0`. Run `python3 /home/aaronstatic/.claude/skills/asd-ste100/scripts/ste-lint.py --fail-over 2.5` on `docs/technical-design.md`, `CHANGELOG.md`, `context.md`, and `implementation.md`, and fix the prose until each file passes. Record the demo, the results, and each decision that the plan did not settle in `context.md`.
  - File(s): `docs/technical-design.md`, `CHANGELOG.md`, `docs/features/panels-and-modes/slot-container/context.md`, `docs/features/panels-and-modes/slot-container/implementation.md` (prose only)
  - Estimate: 6 steps

---

## Bugs & Issues

**Active Bugs:**
- None

**Fixed Bugs:**
- None

---

## Technical Debt

- None

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
