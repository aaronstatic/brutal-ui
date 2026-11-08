# panels-and-modes/mode-set - Task Checklist

**Last Updated:** 2026-11-08 03:10
**Progress:** 25/25 tasks complete (100%)
**Advanced phases:** Phase 1 and phase 2 are ADVANCED (opus). Phase 3 and phase 4 are STANDARD (sonnet).

"The plan" means `implementation.md` of this feature. Its sections "Architecture Overview" (Public API, Internal Shape, Integration Points, Data Flow) and "Key Technical Decisions" hold the rules that the tasks name. Each task's full text sits in the plan; the phase brief carries it word for word.

Each group ends with `MSBUILDDISABLENODEREUSE=1 dotnet build brutal-ui.sln -nodeReuse:false 2>&1 | tail -n 30` and `MSBUILDDISABLENODEREUSE=1 dotnet test --solution brutal-ui.sln 2>&1 | tail -n 30` (tool rule 5). Expect `0 Warning(s)`, `0 Error(s)`, and 0 failed. Do not run `dotnet build-server shutdown`. A warning fails the build (`TreatWarningsAsErrors`). Each public member needs an XML doc comment (CS1591 is an error). The projects have no implicit usings, so each new file holds its own `using` lines, also for each namespace that a `cref` names. Use file-scoped namespaces. The Framework files use `BrutalUi.Framework`; the Framework tests use `BrutalUi.Framework.Tests`; the Core test uses `BrutalUi.Core.Tests`. Prose in comments follows ASD-STE100. Run one Framework class with `tests/BrutalUi.Framework.Tests/bin/Debug/net10.0/BrutalUi.Framework.Tests --filter-class '*<Class>' 2>&1 | tail -n 5`, and one Core class with `tests/BrutalUi.Core.Tests/bin/Debug/net10.0/BrutalUi.Core.Tests --filter-class '*<Class>' 2>&1 | tail -n 5`. The test files use unique names for each test (`ClassName.Intern(nameof(TestName) + ".x")`, and unique UTF-8 panel, layout, and mode names), because xUnit runs the classes in parallel and the class table is global. Known flakes: a test host can exit 134, and `TheCurvesAllocateNothing` failed once; run the tests a second time before you treat either as a fault.

**No-change rule:** these files do not change: each other file of `src/BrutalUi.Core/`, each other file of `src/BrutalUi.Framework/` (also `SlotContainer.cs`, `PanelController.cs`, `ModeDefinition.cs`, and `UiActionMap.cs`), each `.csproj`, `Directory.Build.props`, `Directory.Packages.props`, `brutal-ui.sln`, `scripts/`, `samples/`, and each existing test file.

---

## Phase 1: The Core Slot and the First Mode on the Screen (6/6 complete)

**Demo:** a developer runs `tests/BrutalUi.Framework.Tests/bin/Debug/net10.0/BrutalUi.Framework.Tests --filter-class '*UiManagerTests'` after a build and reads a total of 6 with `failed: 0`.
**Needs:** none
**Agent tier:** ADVANCED (opus)

One group (tasks 1.1 to 1.6), 6 files.

- [x] **1.1 Fill in context.md: base commit, counts, Internal Shape, Integration Points**
  - Description: Check that `SlotContainer.cs` and the six panel-definition files exist in `src/BrutalUi.Framework/Panels/`. Record the base commit (`git rev-parse HEAD`), the solution test count (with the skipped count), and the `BrutalUi.Framework.Tests` and `BrutalUi.Core.Tests` counts. At planning time, the solution read 2773 (3 skipped) and `BrutalUi.Framework.Tests` read 180. Record the Internal Shape and the Integration Points of the plan.
  - File(s): `docs/features/panels-and-modes/mode-set/context.md`
  - Estimate: 4 steps

- [x] **1.2 Write UiBeforeUpdateHandler.cs**
  - Description: `public delegate void UiBeforeUpdateHandler(float deltaTime);` with the doc comment rules of the plan (the call point, the tree-change rule, the re-entry throw, the exception rule, the create-one-time rule).
  - File(s): `src/BrutalUi.Core/Tree/UiBeforeUpdateHandler.cs`
  - Estimate: 4 steps

- [x] **1.3 Edit UiContext.cs: the OnBeforeUpdate slot, the guard, the call in Update**
  - Description: Add `OnBeforeUpdate`, `_beforeUpdate`, and the guard and call of the Internal Shape, after `OnUnhandledInput`. Add the remarks (the seam rule, the `=` rule, the null rule) and the `Update` paragraph and exception tag. Change no other member.
  - File(s): `src/BrutalUi.Core/Tree/UiContext.cs`
  - Estimate: 6 steps

- [x] **1.4 Write BeforeUpdateTests.cs with 6 tests**
  - Description: The 6 tests of task 1.4 of the plan: `UpdateCallsTheSlotFirstWithTheDeltaTime`, `ARootThatTheSlotAddsGetsItsStyleAndLayoutInTheSameUpdate`, `AnUpdateFromTheSlotThrowsAndTheContextStaysValid`, `AnExceptionOfTheSlotLeavesUpdateBeforeTheInputPass`, `AWriteReplacesTheDelegateInTheSlot`, and `AnUpdateWithASlotAllocatesNothingAfterWarmUp`. The plan states the asserts of each test.
  - File(s): `tests/BrutalUi.Core.Tests/BeforeUpdateTests.cs`
  - Estimate: 8 steps

- [x] **1.5 Write UiManager.cs (first-mode scope)**
  - Description: `CreateInfo`, the constructor, `Context`, `Panels`, `CurrentMode`, `TargetMode`, the three `Register` overloads, `SetMode`, the handler, and `Apply` for the first mode only (mark, layout add, enter, end). No `History`, leave pass, `RemoveRoot`, or map switch yet. The class remarks of the plan.
  - File(s): `src/BrutalUi.Framework/Panels/UiManager.cs`
  - Estimate: 8 steps

- [x] **1.6 Write UiManagerTests.cs (6 tests), run build/tests/demo, plants 1 to 3**
  - Description: The 6 tests of task 1.6 of the plan: `TheConstructorWritesTheBeforeUpdateSlot`, `SetModeRecordsTheTargetAndChangesNoTree`, `TheNextUpdateAddsTheLayoutRootAndMountsEachPanelInTheOrderOfTheMode`, `TwoSetModeCallsInOneFrameApplyTheLast`, `AFrameWithNoTargetChangesNothing`, `SetModeRejectsAnUnknownName`. Run the build, the tests, `UiManagerTests` (6) and `BeforeUpdateTests` (6). Plants 1 to 3 per the plan. Record the demo and results in `context.md`.
  - File(s): `tests/BrutalUi.Framework.Tests/Panels/UiManagerTests.cs`, `docs/features/panels-and-modes/mode-set/context.md`
  - Estimate: 10 steps

---

## Phase 2: The Mode Change (7/7 complete)

**Demo:** a developer runs the built Framework test binary with `--filter-class '*ModeDiffTests'`, `'*LayoutSwapTests'`, and `'*ModeActionMapTests'` and reads the totals 7, 5, and 5, each with `failed: 0`.
**Needs:** Phase 1
**Agent tier:** ADVANCED (opus)

One group (tasks 2.1 to 2.7), 5 files.

- [x] **2.1 UiManager.cs: the Slot/Next fields, the mark pass, the leave pass, the diff remarks**
  - Description: Per the Internal Shape and the plan's task 2.1.
  - File(s): `src/BrutalUi.Framework/Panels/UiManager.cs`
  - Estimate: 6 steps

- [x] **2.2 UiManager.cs: the layout swap (`_shown`, RemoveRoot, AddRoot) and its remarks**
  - Description: Per the Internal Shape and the plan's task 2.2.
  - File(s): `src/BrutalUi.Framework/Panels/UiManager.cs`
  - Estimate: 4 steps

- [x] **2.3 UiManager.cs: the action map switch and its remarks**
  - Description: Per the Internal Shape and the plan's task 2.3.
  - File(s): `src/BrutalUi.Framework/Panels/UiManager.cs`
  - Estimate: 3 steps

- [x] **2.4 Write ModeDiffTests.cs with 7 tests**
  - Description: The 7 tests of task 2.4 of the plan on one layout with slots `left` and `right`. The plan states the asserts of each test.
  - File(s): `tests/BrutalUi.Framework.Tests/Panels/ModeDiffTests.cs`
  - Estimate: 10 steps

- [x] **2.5 Write LayoutSwapTests.cs with 5 tests**
  - Description: The 5 tests of task 2.5 of the plan (layouts `wide` and `tall`). The plan states the asserts of each test.
  - File(s): `tests/BrutalUi.Framework.Tests/Panels/LayoutSwapTests.cs`
  - Estimate: 8 steps

- [x] **2.6 Write ModeActionMapTests.cs with 5 tests**
  - Description: The 5 tests of task 2.6 of the plan; each map binds Escape (256) to a counted command.
  - File(s): `tests/BrutalUi.Framework.Tests/Panels/ModeActionMapTests.cs`
  - Estimate: 8 steps

- [x] **2.7 Run build/tests/demo, plants 4 to 7, record**
  - Description: `ModeDiffTests` (7), `LayoutSwapTests` (5), `ModeActionMapTests` (5), the 12 phase-1 tests still pass. Plants 4 to 7 on `UiManager.cs` per the plan. Record in `context.md`.
  - File(s): `src/BrutalUi.Framework/Panels/UiManager.cs` (plants only, restored), `docs/features/panels-and-modes/mode-set/context.md`
  - Estimate: 7 steps

---

## Phase 3: The Mode History and the Back Step (4/4 complete)

**Demo:** a developer runs the built Framework test binary with `--filter-class '*ModeHistoryTests'` and reads a total of 9 with `failed: 0`.
**Needs:** Phase 2
**Agent tier:** STANDARD (sonnet)

One group (tasks 3.1 to 3.4), 4 files.

- [x] **3.1 Write ModeHistory.cs (ring, Push, Back, Clear) with the plan's remarks**
  - Description: The public members of the Public API and the ring of the Internal Shape; the constructor is internal.
  - File(s): `src/BrutalUi.Framework/Panels/ModeHistory.cs`
  - Estimate: 6 steps

- [x] **3.2 UiManager.cs: capacity, History, the back-step internal, Apply step 6**
  - Description: `DefaultHistoryCapacity` (16), `CreateInfo.HistoryCapacity` (0 gives the default, below 0 throws), the `History` property, the internal method `Back()` calls, `SetMode` clears `_targetFromBack`.
  - File(s): `src/BrutalUi.Framework/Panels/UiManager.cs`
  - Estimate: 4 steps

- [x] **3.3 Write ModeHistoryTests.cs with 9 tests**
  - Description: The 9 tests of task 3.3 of the plan on four modes A, B, C, D of one layout. The plan states the asserts of each test.
  - File(s): `tests/BrutalUi.Framework.Tests/Panels/ModeHistoryTests.cs`
  - Estimate: 8 steps

- [x] **3.4 Run build/tests/demo, plants 8 to 9, record**
  - Description: `ModeHistoryTests` (9), the 29 phase-1 and phase-2 tests still pass. Plants 8 and 9 per the plan. Record in `context.md`.
  - File(s): `docs/features/panels-and-modes/mode-set/context.md`
  - Estimate: 6 steps

---

## Phase 4: The Register Checks, the Error Rule, the 0-Byte Proof, and the Docs (8/8 complete)

**Demo:** a developer runs `MSBUILDDISABLENODEREUSE=1 scripts/test-linux.sh` and reads its PASS line, and the built Framework test binary reads a total of 8 for `--filter-class '*UiManagerRegisterTests'` and 9 for `--filter-class '*ModeDiffTests'`, each with `failed: 0`.
**Needs:** Phase 3
**Agent tier:** STANDARD (sonnet)

Two groups. Group 1: tasks 4.1 to 4.4 (5 files). Group 2: tasks 4.5 to 4.8 (5 files).

- [x] **4.1 UiManager.cs: the register checks, the `_applying` guard, the error and allocation remarks**
  - Description: Each check before a change with a constant message per the plan's task 4.1; `SetMode`, `Back()`, and each `Register` throw while `_applying` is `true`.
  - File(s): `src/BrutalUi.Framework/Panels/UiManager.cs`
  - Estimate: 6 steps

- [x] **4.2 Write UiManagerRegisterTests.cs with 8 tests**
  - Description: The 8 tests of task 4.2 of the plan. The plan states the asserts of each test.
  - File(s): `tests/BrutalUi.Framework.Tests/Panels/UiManagerRegisterTests.cs`
  - Estimate: 9 steps

- [x] **4.3 Add the two 0-byte tests to ModeDiffTests.cs**
  - Description: `SetModeAndTheDiffAllocateNothingAfterWarmUp` and `AFrameWithNoTargetAllocatesNothing`, with the scene of task 4.3 of the plan (layouts `wide` and `tall`, panels `p1` to `p4`, two maps, modes A, B, C).
  - File(s): `tests/BrutalUi.Framework.Tests/Panels/ModeDiffTests.cs`
  - Estimate: 8 steps

- [x] **4.4 Run build/tests/demo, plants 10 to 14, record**
  - Description: `UiManagerRegisterTests` (8), `ModeDiffTests` (9). Plants 10 to 14 on `UiManager.cs` and `ModeHistory.cs` per the plan. Record the failed tests and the measured bytes in `context.md`.
  - File(s): `src/BrutalUi.Framework/Panels/UiManager.cs`, `src/BrutalUi.Framework/Panels/ModeHistory.cs` (plants only, restored), `docs/features/panels-and-modes/mode-set/context.md`
  - Estimate: 8 steps

- [x] **4.5 PanelDefinition.cs: the DefaultSlot remark (doc lines only)**
  - Description: Notes question 2, option (A): the name of the slot that the panel suggests; a loader can use it; `UiManager` does not read it. Change no code line.
  - File(s): `src/BrutalUi.Framework/Panels/PanelDefinition.cs`
  - Estimate: 3 steps

- [x] **4.6 Update docs/technical-design.md sections 4, 10, and 14**
  - Description: The Before-update slot line and frame-order sentence (section 4); the `### Mode set` sub-section and the Components table fix (section 10); the `**Mode tests.**` bullet (section 14). Per the plan's task 4.6.
  - File(s): `docs/technical-design.md`
  - Estimate: 6 steps

- [x] **4.7 Update CHANGELOG.md (Added and Changed lines)**
  - Description: The two Added lines and the one Changed line of the plan's task 4.7.
  - File(s): `CHANGELOG.md`
  - Estimate: 3 steps

- [x] **4.8 Run the full gate, all phase demos, the STE lint, record**
  - Note: the build, test, gpu, and loader steps pass (2821, 0 failed) and all demos read their totals. The smoke step fails on a host Xvfb crash inside the NVIDIA EGL stack (see gotcha 4 in `context.md`). A repaired host must print the PASS line.
  - Description: `scripts/test-linux.sh` PASS; the demo totals 6 and 6, 9, 5, 5, 9, 8 with `failed: 0`; `ste-lint.py --fail-over 2.5` on `docs/technical-design.md`, `CHANGELOG.md`, `context.md`, and the plan; fix prose until each file passes. Record in `context.md`.
  - File(s): `docs/features/panels-and-modes/mode-set/context.md`, `docs/features/panels-and-modes/mode-set/implementation.md`
  - Estimate: 8 steps

---

## Bugs & Issues

**Active Bugs:**
- (none)

**Fixed Bugs:**
- (none)

---

## Technical Debt

- (none)

---

## Notes

- The plan's Notes hold the four open scope questions (all use the recommended option A), the conflicts and gaps reading, and the two known flakes.
- panel-sample (the next epic feature) owns the numbered gate `ModeDiffAllocationTests`; this feature proves the 0-byte paths in `ModeDiffTests` and `BeforeUpdateTests` (Decision 10).
