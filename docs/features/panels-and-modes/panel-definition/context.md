# panels-and-modes/panel-definition - Context & Decisions

**Last Updated:** 2026-10-30 05:32
**Current Phase:** Complete (phase 1 and the cross-phase review are done)
**Status:** ✅ Ready for Review

---

## Quick Status

**What's Done:**
- ✅ The plan (`implementation.md`). The open questions use the recommended option (A) of each question.
- ✅ The dev docs (`tasks.md`, `inbox.md`, this file)
- ✅ Phase 1 group A (tasks 1.1 to 1.7): `PanelContext`, `PanelDefinition`, `LayoutDefinition`, `ModePanel`, `ModeDefinition`, and `PanelRecordTests` (14 passed).
- ✅ Phase 1 group B (tasks 1.8 to 1.13): `PanelInstance`, `PanelController`, `PanelControllerTests` (10 passed), the two plants (each `exit=2`), section 10 and section 14 of the technical design, and the changelog.
- ✅ The phase 1 verifier returns PASS, with Demo: PASS. The solution gives 2750 tests, 0 failed, 3 skipped.
- The shape of `PanelInstance` is set (task 1.8), so slot-container can start. `Root` is `null` until the first `EnsureRoot`, then the same `Element` for the life of the controller.

- ✅ The cross-phase review (`code-review.md`): 4 findings (1 MEDIUM, 3 LOW), 4 fixed in review groups 1 to 3. The code had no fault. CR-1 to CR-3 closed test gaps, and CR-4 corrected a remark. The review verifier returns PASS, with Demo: PASS.
- ✅ The draft plan of the next feature: `docs/features/panels-and-modes/slot-container/implementation.md` (2 phases, 10 tasks, STANDARD, with 4 open scope questions).

**What's Next:**
- 📋 `/autorun-epic` commits this feature and starts slot-container.
- 📋 mode-set decides the use of `PanelDefinition.DefaultSlot` (the follow-up question of `code-review.md`).

**Blockers:**
- None

---

## Base and Counts

- Base commit: `43d3ea073dd0d5bc075785b047ab4d97a0e2fa8e` (`docs: panels-and-modes epic plan`).
- Solution test count at the base (with the skipped count): 2726 total, 3 skipped, 0 failed. The first `dotnet test` run hung before the first code change (see Gotchas), so the agent computed the base as the group A total minus the 14 new tests.
- `BrutalUi.Framework.Tests` count at the base: 133.
- Group A result (task 1.7): `dotnet build brutal-ui.sln` gives 0 Warning(s) and 0 Error(s). `dotnet test --solution brutal-ui.sln` gives total 2740, failed 0, succeeded 2737, skipped 3. `BrutalUi.Framework.Tests` gives total 147, failed 0. `PanelRecordTests` gives total 14, failed 0. Each existing test passes with no edit.

### Group B results (tasks 1.11 and 1.13)

- `dotnet build brutal-ui.sln -nodeReuse:false` gives 0 Warning(s) and 0 Error(s).
- `dotnet test --solution brutal-ui.sln` gives total 2750, failed 0, succeeded 2747, skipped 3. `BrutalUi.Framework.Tests` gives total 157, failed 0. `PanelRecordTests` gives total 14, `PanelControllerTests` gives total 10, each with failed 0.
- Plant 1 (`GC.KeepAlive(new object());` as the first line of `EnsureRoot`): exit=2. Only `EnsureRootAndTheReadsAllocateNothingAfterTheBuild` failed (2400 bytes, 100 calls of 24 bytes).
- Plant 2 (empty body in place of the early `return`): exit=2. `EnsureRootBuildsTheRootOneTime`, `TheRootStaysTheSameAfterADetach`, and the 0-byte test failed.
- After each plant, `cmp` of `PanelController.cs` with its copy in `/tmp/claude-1000/panel-definition-plant/` gave 0. The tests then passed with 0 failed.
- `scripts/test-linux.sh`: `test-linux: PASS build, test, gpu, smoke (120 frames) in 22 s`, exit=0.
- Demo: `--filter-class '*PanelRecordTests'` reads total 14, and `--filter-class '*PanelControllerTests'` reads total 10, each with failed 0. The agent ran the built binary, because `dotnet run` hangs.
- Prose lint (`--fail-over 2.5`): `docs/technical-design.md` 1.52 (base 1.52), `CHANGELOG.md` 1.66, `implementation.md` 1.70, and `context.md` below 2.5 at the last run.

### Decisions that the plan did not settle (group B)

- `RootFactory` in the tests has two more fields than the brief names (`Reenter` and `Caught`) for the re-entry test, and three fields (`Commands`, `Events`, `Services`) for the context read test.
- The constructor of `PanelController` uses `ArgumentNullException.ThrowIfNull` with the argument expression, so `ParamName` is `definition` and `context`.
- The 0-byte test also asserts a fixed sum (each step adds the same positive sum), so an empty body cannot pass.
- The section 14 bullet says that plant 2 failed three tests, because a rebuilt root also allocates in the 0-byte test.

### Gotchas of group B

- `dotnet build-server shutdown` hung (more than 15 minutes), because of old MSBuild nodes of other days. The agent skipped it and used `MSBUILDDISABLENODEREUSE=1` with `-nodeReuse:false`. The agent could not stop the old processes (the permission system denied it). A `scripts/test-linux.sh` run without `MSBUILDDISABLENODEREUSE=1` hung in the build step and timed out (exit 124). A run with the variable passed.
- One run of `scripts/test-linux.sh` stopped in the test step with exit 134 (`Internal CLR error. (0x80131506)`). The next run passed. The same tests passed in four other `dotnet test` runs, so the agent treats it as a flake of the test host. Watch for it.

### Internal Shape and Integration Points (summary)

The plan holds the full text in "Architecture Overview" (Public API, Internal Shape, Integration Points, Data Flow).

- Records copy each name with `ToArray()` into a `byte[]` field. Name properties return a span over the copy (0 bytes per read).
- Validation (A5): an empty name throws `ArgumentException` with the parameter name. A `null` factory throws `ArgumentNullException`.
- `PanelContext` (Q2): three `required` init members with a null check. No other state.
- Records keep the generated equality (Decision 6) and an explicit `PrintMembers` for `ToString` (Decision 2).
- `ModeDefinition` (A4): the slots keep the order of their first entry. A panel is in one slot at most. The getters check the index.
- Integration: slot-container mounts `PanelInstance.Root`. mode-set registers the records, checks the names, and activates `ActionMap` (A6). Group B adds `PanelInstance` and `PanelController`.

---

## Key Files

### Core Implementation
- `src/BrutalUi.Framework/Panels/PanelContext.cs` - the three registries as `required` init members (group A)
- `src/BrutalUi.Framework/Panels/PanelDefinition.cs` - the panel record (group A)
- `src/BrutalUi.Framework/Panels/LayoutDefinition.cs` - the layout record (group A)
- `src/BrutalUi.Framework/Panels/ModeDefinition.cs` - `ModePanel` and the mode record (group A)
- `src/BrutalUi.Framework/Panels/PanelInstance.cs` - the instance with `Root` and `IsBuilt` (group B)
- `src/BrutalUi.Framework/Panels/PanelController.cs` - the controller with `EnsureRoot` (group B)

### Tests
- `tests/BrutalUi.Framework.Tests/Panels/PanelRecordTests.cs` - 14 tests (group A)
- `tests/BrutalUi.Framework.Tests/Panels/PanelControllerTests.cs` - 10 tests (group B)

### Related Files
- `docs/features/panels-and-modes/panel-definition/implementation.md` - the implementation plan
- `docs/technical-design.md` - section 10 and section 14 (group B)
- `CHANGELOG.md` - one line under Added (group B)

---

## Important Decisions

The plan holds Decisions 1 to 9 and the Confirmed Assumptions. Add a decision here only when the build changes or adds to the plan.

### Decision 10: The review fixes keep the class counts
**Date:** 2026-10-30
**Context:** CR-1, CR-2, and CR-3 of the review named test gaps. A new test changes the counts 14 and 10 that the plan, the demo, and section 14 name.
**Decision:** The fixes extend existing tests. `NewMode` lists `right:c`, `left:b`, `right:a`, so a sort fails the grouping test. The three "holds" tests assert the reference equality and the `with` copy. `AModeDefinitionCopiesItsNames` reads `ModePanel.Slot` and `ModePanel.Panel`. The 0-byte test reads both names and `ActionMap`, and asserts a floor on the sum.
**Rationale:** The counts in the plan and in the docs stay true.
**Impact:** `PanelRecordTests` stays 14, and `PanelControllerTests` stays 10.

### Open follow-up for mode-set: `PanelDefinition.DefaultSlot`
`ModePanel` rejects an empty slot, so no mode can omit the slot of a panel. The remark "names the slot of the panel when a mode does not name one" thus names a case that cannot occur now. The review recommends (A): mode-set keeps the API and corrects the remark to the real use, or no use. `code-review.md` holds the options.

---

## Gotchas & Learnings

- `dotnet build` and `dotnet test` hung for more than 5 minutes at the start. Old MSBuild nodes (node reuse) blocked the run. `-nodeReuse:false` on the build, or `MSBUILDDISABLENODEREUSE=1` on the test, finishes in under 15 seconds. `dotnet run` of the test project hung the same way, so run the built `tests/BrutalUi.Framework.Tests/bin/Debug/net10.0/BrutalUi.Framework.Tests --filter-class '*<Class>'` instead.
- `UiActionMap` has no default constructor. A test builds it with `new UiActionMap(new UiActionMap.CreateInfo { Commands = registry })`.
- The compiler accepted the records with a `ReadOnlySpan<byte>` property and the explicit `PrintMembers`. The class fallback was not needed.

---

## Testing Approach

### Unit Tests
**Files:** `tests/BrutalUi.Framework.Tests/Panels/*.cs`
- `PanelRecordTests` (14) and `PanelControllerTests` (10), per the Testing Strategy of the plan.

### Allocation Tests
- `PanelControllerTests.EnsureRootAndTheReadsAllocateNothingAfterTheBuild` measures 0 bytes in Debug. Plant 1 of task 1.11 fails it.

---

## Needs Human Verification

`/autorun-feature` adds one line per item that the verifier cannot check, for a person to test.

- [ ] Phase 1 demo: a developer runs `scripts/test-linux.sh` and reads its PASS line, and the new Panels tests pass: `dotnet run --project tests/BrutalUi.Framework.Tests/BrutalUi.Framework.Tests.csproj -- --filter-class '*PanelRecordTests'` reads a total of 14 and `--filter-class '*PanelControllerTests'` reads a total of 10, each with `failed: 0`.

---

## Open Questions

- [x] **Q:** The equality of the records.
      **A:** (A) Keep the generated equality (Decision 6).
- [x] **Q:** The input shape of the mode panels.
      **A:** (A) A flat list of `ModePanel` values (Decision 3).

---

## Session Notes

### 2026-10-30 04:36
- `/autorun-epic` runs this feature in the called mode through the `feature-orchestrator` agent.
- `/start-feature` created `tasks.md`, `inbox.md`, and this file. `implementation.md` reads In Progress.

### 2026-10-30 05:10
- Phase 1 is done in two task groups (STANDARD, `game-dev`). The verifier returns PASS and Demo: PASS. `scripts/test-linux.sh` passed on the first verifier run.
- Stale MSBuild nodes of earlier sessions still run on this machine. `MSBUILDDISABLENODEREUSE=1` with `-nodeReuse:false` avoids the hang. A person can stop the old `dotnet` processes (for example the hung `dotnet build-server shutdown`) by hand.
- The inbox holds no line.

### 2026-10-30 05:32
- The cross-phase review (`game-dev-advanced`) found 4 findings: 0 HIGH, 1 MEDIUM, 3 LOW. The code had no fault. Review groups 1 to 3 (`game-dev`) fixed each finding, and each fix has a plant that fails a test. The review verifier returns PASS, with Demo: PASS (2750 tests, 0 failed, 3 skipped, `scripts/test-linux.sh` PASS).
- The `solution-architect` agent wrote the draft plan of slot-container. It has 4 open scope questions, and it uses the recommended option of each.
- A plant that does not compile leaves the old test binary, and the old binary passes. Check `0 Error(s)` before you read the result of a plant.
- The inbox holds no line. No bug to promote.
- The feature is ready for review. `/autorun-epic` owns the commit.

---

*Update this file at the end of each work session. Run `/update-feature` before compacting conversations.*
