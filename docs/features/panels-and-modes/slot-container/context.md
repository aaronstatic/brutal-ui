# panels-and-modes/slot-container - Context & Decisions

**Last Updated:** 2026-10-30 07:35
**Current Phase:** Complete (phase 1, phase 2, and the cross-phase review are done)
**Status:** ✅ Ready for Review

---

## Quick Status

**What's Done:**
- ✅ The plan (`implementation.md`). The user accepted the recommended option (A) of each of the four open scope questions (see Open Questions).
- ✅ The dev docs (`tasks.md`, `inbox.md`, this file)
- ✅ Phase 1 (tasks 1.1 to 1.4): `SlotContainer.cs`, `SlotContainerTests.cs` (10 tests), and the two plants. See "Phase 1 Demo and Plants". The verifier returns PASS, with Demo: PASS.
- ✅ Phase 2 (tasks 2.1 to 2.6): `Find`, `SlotLookupTests` (8 tests), `SlotContainerTreeTests` (5 tests), plants 3 to 5, section 10 and section 14 of the technical design, and the changelog. The verifier returns PASS, with Demo: PASS.
- ✅ The cross-phase review (`code-review.md`): 4 LOW findings, 4 fixed in review groups 1 to 4. CR-4 fixed one fault of the code (the `ParamName` of a bad index). CR-1 and CR-2 closed test gaps, and CR-3 completed the keep rule in the remarks and the docs. The review verifier returns PASS, with Demo: PASS (2773 tests, 0 failed, 3 skipped, `scripts/test-linux.sh` PASS).

**What's Next:**
- 📋 `/autorun-epic` commits this feature and starts mode-set.
- 📋 mode-set caches each `Find` result, unmounts each leaving root before the first mount, and can reject a duplicate slot name at register time.

**Blockers:**
- None

---

## Base and Counts

- Base commit: `99823672ddf87d1428a590be58fcfa09ced508d9` (branch `feat/panels-and-modes/slot-container`).
- The six panel-definition files exist in `src/BrutalUi.Framework/Panels/`: `LayoutDefinition.cs`, `ModeDefinition.cs`, `PanelContext.cs`, `PanelController.cs`, `PanelDefinition.cs`, `PanelInstance.cs`.
- Base counts before any code: solution 2750 total, 3 skipped, 0 failed. `BrutalUi.Framework.Tests` 157, 0 failed. They match the counts at planning time.
- Counts after phase 1: solution 2760 total, 3 skipped, 0 failed. `BrutalUi.Framework.Tests` 167.

### Internal Shape (summary)
- `SlotContainer` is a sealed `Element` subtype. The type id is `ElementType.Intern("SlotContainer")`.
- The constructor rejects an empty name, copies the name into a `byte[]`, and sets `Element.Name` to the UTF-8 text. `SlotName` returns a span over the copy.
- `Mount` throws for a null root. A root of this slot moves with `Children.Move` (no join). A root with another parent throws `InvalidOperationException`. Any other root goes through `Children.Insert`, which checks a context root, a cycle, and the index before a change.
- `Unmount` returns `Children.Remove(root)`. The root keeps its state.
- `Find` comes in phase 2.

### Integration Points (summary)
- mode-set calls `PanelController.EnsureRoot()` and passes the root to `Mount`. The controller keeps the root alive after an `Unmount`.
- The style pass and the layout pass treat the slot as a plain `Element`. A stylesheet matches it by type name, class, or name.
- `BrutalUi.Core` gets no change.

---

## Phase 1 Demo and Plants

- Demo: the built binary with `--filter-class '*SlotContainerTests'` reads total 10, failed 0, succeeded 10.
- Build: `0 Warning(s)`, `0 Error(s)`. Solution tests: total 2760, failed 0, skipped 3.
- Plant 1 (the `throw` of the parent rule removed): `dotnet test --solution` gave `exit=2`, build `0 Error(s)`. Failed: `AMountOfARootThatAnotherSlotHoldsThrows` and `AMountOfARootWithAnotherParentThrows` (2 of 10).
- Plant 2 (`Children.Insert(index, root)` replaced with `Children.Add(root)`): `exit=2`, build `0 Error(s)`. Failed: `MountPutsEachRootAtItsIndex`, `UnmountDetachesTheRootAndKeepsItsState`, and `TheMountRejectsANullRootABadIndexAContextRootAndACycle` (3 of 10). The last two also use an insert index.
- After each plant, the file was restored from the scratchpad copy and `cmp` reported no difference. After the second restore, one comment word changed ("uses no lock" to "has no thread guard"). The tests ran again and read failed 0.

---

## Phase 2 Plants

- Group 1 (tasks 2.1 to 2.4) added `SlotContainer.Find`, `SlotLookupTests` (8 tests), and `SlotContainerTreeTests` (5 tests).
- Build: `0 Warning(s)`, `0 Error(s)`. Solution tests: total 2773, failed 0, skipped 3. `SlotLookupTests` 8, `SlotContainerTreeTests` 5, and `SlotContainerTests` 10, each with failed 0. `BrutalUi.Framework.Tests` gained 13 tests.
- `MountUnmountAndFindAllocateNothingAfterWarmUp` measures 0 bytes in Debug (100 iterations, fixed sum 17 for each step).
- Plant 3 (`GC.KeepAlive(new object());` as the first line of `Find`): `exit=2`, build `0 Error(s)`. Failed: `MountUnmountAndFindAllocateNothingAfterWarmUp` (1 test). Measured 7200 bytes (300 calls of 24 bytes).
- Plant 4 (the same line as the first line of `Mount`): `exit=2`, build `0 Error(s)`. Failed: `MountUnmountAndFindAllocateNothingAfterWarmUp` (1 test). Measured 16800 bytes (700 calls of 24 bytes).
- Plant 5 (the early `return` of the slot branch removed, so the walk enters a slot when the name does not match): `exit=2`, build `0 Error(s)`. Failed: `FindDoesNotEnterTheChildrenOfASlot` (1 test). It expected `null` and got the inner slot.
- The scratchpad copy was made after task 2.1 and before the plants. After each plant, the file was restored with an edit. `cmp` of the restored file against the copy reported no difference (`cmp=0`). The tests ran again and read failed 0.
- `foreach` over `element.Children` allocates 0 bytes (the enumerator is a struct), so the walk needs no index loop.

## Phase 2 Docs, Script Run, and Lint

- Group 2 (tasks 2.5 and 2.6) added the sub-section `### Slot containers` to section 10 of `docs/technical-design.md`, the bullet `**Slot tests.**` to section 14, and one line to `CHANGELOG.md`. No file of `src/`, `tests/`, `samples/`, or `scripts/` changed.
- `MSBUILDDISABLENODEREUSE=1 scripts/test-linux.sh; echo "exit=$?"` printed `test-linux: PASS build, test, gpu, smoke (120 frames) in 16 s` and `exit=0`. The first run passed, so no second run was needed (no exit 134).
- Demos: `SlotContainerTests` total 10, `SlotLookupTests` total 8, and `SlotContainerTreeTests` total 5. Each reads `failed: 0`.
- Lint (`--fail-over 2.5`): `technical-design.md` 1.51, `CHANGELOG.md` 1.66, `context.md` 1.69, and `implementation.md` 1.97. Each passes with no prose fix, so `implementation.md` has no edit in group 2.
- Decisions that the plan did not settle: the sub-section holds 9 bullets: type, name copy, mount rule, parent rule, keep rule, lookup rule, child list rule, thread rule, and 0-byte rule. The throws of the context root and of an ancestor sit in the parent bullet. The `Slot tests.` bullet lists the 3 plants of group 1 by name and the 3 plants of group 2 with the measured bytes. The thread bullet says "no thread guard" (the wording of the class remarks) in place of "no lock".

**Phase 2 build decisions (no change to the plan):**
- `SlotContainerTreeTests` puts the slot first under the layout root. The slot starts at Y 0, so the expected Y values are the heights 0, 20, and 50 (and 0, 30, 50). The style tests put a plain sibling element after the slot.
- The class rule sets `BackgroundColor` and the name rule sets `BorderColor`, so the two rules do not hide each other. The move test uses a descendant selector (`Selector.Descendant`) from the slot class to the `Label` type, with the text member `Color`.
- The 0-byte test uses `Step` with 6 unmounts, 9 child counts, and 2 hits, so the sum is 17 for each step.
- The class remarks and the `SlotName` remarks held no text that said `Find` comes later, so no remark needed an edit.

---

## Review Fixes

Group 1 (`review-g1.md`). All edits are in `src/BrutalUi.Framework/Panels/SlotContainer.cs`.

- G1.1 CR-4: done. The held branch of `Mount` calls `ThrowIfNegative(index)` and `ThrowIfGreaterThanOrEqual(index, Children.Count)` before `Children.Move`. A bad index gives `ParamName` `index`. The `ArgumentOutOfRangeException` doc names `index` and the two ranges. Core has no change.
- G1.2 CR-3 (remark part): done. The remarks of `Unmount` get the sentence about the hover, the active state, the focus, and the running tweens. Core matches: `Update` calls `DrainEvents` (which calls `DropStalePointerState` and `DropStaleFocus`) and the transition pass (loop 0 of `TransitionPool.Advance`, in `src/BrutalUi.Core/Style/TransitionPool.cs`). The class remarks and the `Mount` remarks make no such claim, so they have no change.
- G1.3 follow-up 1, default (C): done. The constructor remarks tell the U+FFFD rule. No code change.
- G1.4 checks: done. Build: 0 Warning(s), 0 Error(s). Tests: total 2773, failed 0, succeeded 2770, skipped 3 (GLFW tests, no display). Slot classes: `SlotContainerTests` 10, `SlotLookupTests` 8, `SlotContainerTreeTests` 5. The grep for `lock`, `IDisposable`, `event`, `ToString(`, `(object)`, `new List`, `Stack<`, and `Queue<` gives no line.

Group 2 (`review-g2.md`). All edits are in `tests/BrutalUi.Framework.Tests/Panels/SlotContainerTests.cs`.

- G2.1 CR-2: done. `AMountOfARootThatAnotherSlotHoldsThrows` asserts `Assert.Contains("another slot", e.Message)`. `AMountOfARootWithAnotherParentThrows` asserts `Assert.Contains("another parent", e.Message)`. Both keep the caught exception.
- G2.2 CR-4 (test part): done. `ParamName` is `index` for the held root (`Mount(a, 3)`) and for the new root with -1 and `Count + 1`. No test added.
- G2.3 checks: done. Build: 0 Warning(s), 0 Error(s). Tests: total 2773, failed 0, succeeded 2770, skipped 3. `SlotContainerTests` 10.
- G2.3 plant R1 (`OtherParentMessage` for both parent cases): exit 2. Failed: `AMountOfARootThatAnotherSlotHoldsThrows`.
- G2.3 plant R4 (the two `ThrowIf` lines of the held branch removed): exit 2. Failed: `AMountOfAHeldRootMovesItWithNoJoin`.
- G2.3 restore: `cmp` of `SlotContainer.cs` against the copy gives 0. The rerun gives total 2773, failed 0.

Group 3 (`review-g3.md`). The edit is in `tests/BrutalUi.Framework.Tests/Panels/SlotLookupTests.cs`.

- G3.1 CR-1: done. `FindChecksTheRootItself` mounts a panel root with a slot `inner` into the root slot. It asserts `Assert.Null(SlotContainer.Find(slot, "inner"u8))` and keeps the old asserts. No test added.
- G3.2 checks: done. Build: 0 Warning(s), 0 Error(s). Tests: total 2773, failed 0, succeeded 2770, skipped 3. `SlotLookupTests` 8.
- G3.2 plant R2 (a slot that does not match walks its children): exit 2. Failed: `FindChecksTheRootItself` (`Assert.Null() Failure: Value is not null`) and `FindDoesNotEnterTheChildrenOfASlot`.
- G3.2 restore: `cmp` of `SlotContainer.cs` against the copy gives 0. The rerun gives total 2773, failed 0.

Group 4 (`review-g4.md`). All edits are in `docs/technical-design.md`.

- G4.1 CR-3 (doc part): done. The keep rule (line 1035) gets the sentence of the `Unmount` remarks about the hover, the active state, the focus, and the running tweens.
- G4.2 CR-4 (doc part): done. The mount rule (line 1033) tells the `ArgumentOutOfRangeException` with `ParamName` `index` for a held root and for a new root. The bullet of the constructor (line 1032) tells the U+FFFD rule.
- G4.3 `**Slot tests.**` bullet (line 1309): done. It names the plants R1, R2, and R4 and their results. The counts stay 10, 8, and 5.
- G4.4 lint: `ste-lint.py --fail-over 2.5 docs/technical-design.md` gives 1.51 per 100 words (440 hits, 29057 words). It passes with no prose fix.
- The risk row of `implementation.md` that names the join rule for the tween drop keeps its text, because the called mode keeps the plan. The leave rule of the transition pool drops the record at the next `Update` (CR-3).

## Key Files

### Core Implementation
- `src/BrutalUi.Framework/Panels/SlotContainer.cs` - the slot element with `Mount`, `Unmount`, and `Find` (phase 1 and phase 2)

### Tests
- `tests/BrutalUi.Framework.Tests/Panels/SlotContainerTests.cs` - 10 tests (phase 1)
- `tests/BrutalUi.Framework.Tests/Panels/SlotLookupTests.cs` - 8 tests, with the 0-byte test (phase 2)
- `tests/BrutalUi.Framework.Tests/Panels/SlotContainerTreeTests.cs` - 5 tests (phase 2)

### Related Files
- `docs/features/panels-and-modes/slot-container/implementation.md` - the implementation plan
- `docs/technical-design.md` - section 10 and section 14 (phase 2)
- `CHANGELOG.md` - one line under Added (phase 2)

---

## Important Decisions

The plan holds Decisions 1 to 9. Add a decision here only when the build changes or adds to the plan.

**Phase 1 build notes (no change to the plan):**
- Core check confirmed in `ElementList.cs`: `Move` checks both indexes before a change and a move to the same index marks nothing. `Insert` runs `ValidateChild` (null, context root, cycle) and then the index check (0 to `Count`) before a change. `Mount` needs no extra check, and Core has no change.
- A root that is an ancestor of the slot and has its own parent gives `InvalidOperationException` from the parent rule of `Mount`. A root with no parent gives it from the cycle check of Core. Both are `InvalidOperationException`, so the contract holds.
- The test file declares the class name as `ClassId className = ClassName.Intern(...)`, because `ClassName.Intern` returns `ClassId`.
- The empty-name message and the two parent messages are `private const string` fields. The class remarks say "has no thread guard" in place of the word "lock", so a grep for `lock` in the file finds nothing.

### Decision 10: The held branch of `Mount` checks the index itself (CR-4)
**Date:** 2026-10-30
**Context:** `ElementList.Move` names its index `toIndex`, so a bad index of a held root gave `ParamName` `toIndex`, and a bad index of a new root gave `index`.
**Decision:** The held branch calls `ArgumentOutOfRangeException.ThrowIfNegative(index)` and `ThrowIfGreaterThanOrEqual(index, Children.Count)` before `Children.Move`.
**Rationale:** `Mount` has one parameter for the index, so one fault gives one `ParamName`. The check comes before a change, and it allocates nothing on the good path.
**Impact:** Three asserts in `SlotContainerTests` read `ParamName` `index`. The class counts stay 10, 8, and 5, because the review fixes extend existing tests.

### Decision 11: A slot name that is not valid UTF-8 stays legal (follow-up question 1, default C)
**Date:** 2026-10-30
**Context:** `Encoding.UTF8.GetString` puts U+FFFD in place of each bad sequence, so `Name` can differ from the bytes of `SlotName`.
**Decision:** Keep the code. The constructor remarks and section 10 state the rule: a name rule does not match such a slot, but `Find` still finds it by `SlotName`.
**Rationale:** The UTF-8 literals and the later xml-markup loader give valid UTF-8. A sentence costs nothing and tells the limit. Option (B), a reject with `Utf8.IsValid`, stays open for a later change.

### Correction to the plan: the cause of the tween drop (CR-3)
The risk row of `implementation.md` names the join rule of Core for the tween drop at a remove. The real cause is the leave rule of the transition pool: loop 0 of `TransitionPool.Advance` releases the record of a running tween outside the roots at the next `Update`, with no write. The called mode keeps the plan, so the row keeps its text.

---

## Gotchas & Learnings

- Old MSBuild nodes can hang `dotnet` commands on this machine. Use `MSBUILDDISABLENODEREUSE=1` with `-nodeReuse:false`. Do not run `dotnet build-server shutdown`. Run one test class with the built binary `tests/BrutalUi.Framework.Tests/bin/Debug/net10.0/BrutalUi.Framework.Tests --filter-class '*<Class>'`.
- A plant that does not compile leaves the old test binary, and the old binary passes. Check `0 Error(s)` before you read the result of a plant (panel-definition).
- The Core test `TheCurvesAllocateNothing` failed one time with "ease-in allocated 8128 bytes" and passed on the next run. This feature does not touch that code. Run a failed allocation test of another area one more time before you treat it as a fault.
- A plant can fail more tests than the plan names (plant 2, plant R2). Record each failed test, not only the expected one.

---

## Testing Approach

### Unit Tests
**Files:** `tests/BrutalUi.Framework.Tests/Panels/Slot*.cs`
- `SlotContainerTests` (10), `SlotLookupTests` (8), and `SlotContainerTreeTests` (5), per the Testing Strategy of the plan.

### Allocation Tests
- `SlotLookupTests.MountUnmountAndFindAllocateNothingAfterWarmUp` measures 0 bytes in Debug. Plants 3 and 4 of task 2.4 fail it, and the review plant R3 in `Unmount` fails it too (14400 bytes).

---

## Needs Human Verification

`/autorun-feature` adds one line per item that the verifier cannot check, for a person to test.

- [ ] Phase 1 demo: a developer runs `tests/BrutalUi.Framework.Tests/bin/Debug/net10.0/BrutalUi.Framework.Tests --filter-class '*SlotContainerTests'` after a build and reads a total of 10 with `failed: 0`.
- [ ] Phase 2 demo: a developer runs `MSBUILDDISABLENODEREUSE=1 scripts/test-linux.sh` and reads its PASS line, and the built binary reads a total of 8 for `--filter-class '*SlotLookupTests'` and 5 for `--filter-class '*SlotContainerTreeTests'`, each with `failed: 0`.

---

## Open Questions

- [x] **Q:** Which demo does phase 1 use?
      **A:** (A) The built binary with `--filter-class '*SlotContainerTests'` reads a total of 10 with `failed: 0`. The user accepted it.
- [x] **Q:** Does the constructor set `Element.Name` to the text of the slot name?
      **A:** (A) Yes. `Find` reads `SlotName` only (Decision 3). The user accepted it.
- [x] **Q:** Which argument does `Mount` take?
      **A:** (A) An `Element` root. mode-set calls `PanelController.EnsureRoot()` and passes the result (Decision 4). The user accepted it.
- [x] **Q:** What does a mount do with a root that has a parent?
      **A:** (A) A root that the slot holds moves to the index with no join. A root with any other parent throws `InvalidOperationException` (Decision 5). The user accepted it.

---

## Session Notes

### 2026-10-30 05:34
- `/autorun-epic` runs this feature in the called mode through the `feature-orchestrator` agent, on the branch `feat/panels-and-modes/slot-container`.
- `/start-feature` created `tasks.md`, `inbox.md`, and this file. `implementation.md` reads In Progress.

### 2026-10-30 05:50
- Phase 1 is done in one task group (STANDARD, `game-dev`). The verifier returns PASS and Demo: PASS: 2760 tests, 0 failed, 3 skipped. `BrutalUi.Framework.Tests` reads 167, and `SlotContainerTests` reads 10.
- Plant 2 failed three tests, not one. Two more tests also depend on the insert index. The plan expected one.
- The inbox holds no line.
- Phase 2 splits into two groups at a file boundary: group 1 (tasks 2.1 to 2.4, the code and the plants) and group 2 (tasks 2.5 and 2.6, the docs, the script run, and the lint). Group 2 also lints `implementation.md`, so one group would touch 7 files.

### 2026-10-30 06:10
- Phase 2 is done in two task groups (STANDARD, `game-dev`). The verifier returns PASS and Demo: PASS: 2773 tests, 0 failed, 3 skipped. `BrutalUi.Framework.Tests` reads 180. `scripts/test-linux.sh` prints its PASS line.
- In the first verifier test run, the Core test `TheCurvesAllocateNothing` (`tests/BrutalUi.Core.Tests/Style/TransitionCurveTests.cs:267`) failed once with "ease-in allocated 8128 bytes". This feature does not touch that file or the transition code. The second run passed. Treat it as a flake of an existing allocation test, and watch for it.
- The inbox holds no line.

### 2026-10-30 07:35
- The cross-phase review (`game-dev-advanced`) found 4 findings: 0 HIGH, 0 MEDIUM, 4 LOW. Review groups 1 to 4 (`game-dev`) fixed each finding. Each test fix has a plant that fails it (R1, R2, R4). The review verifier returns PASS, with Demo: PASS (2773 tests, 0 failed, 3 skipped, `scripts/test-linux.sh` PASS).
- Follow-up question 1 of the review uses the default (C): a remark sentence on the U+FFFD rule (Decision 11).
- The `solution-architect` agent wrote the draft plan of mode-set: `docs/features/panels-and-modes/mode-set/implementation.md` (4 phases, 25 tasks, phases 1 and 2 ADVANCED, with 4 open scope questions). It uses the recommended option of each question.
- The inbox holds no line. No bug to promote.
- The feature is ready for review. `/autorun-epic` owns the commit.

---

*Update this file at the end of each work session. Run `/update-feature` before compacting conversations.*
