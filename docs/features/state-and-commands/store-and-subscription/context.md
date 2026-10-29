# state-and-commands/store-and-subscription - Context & Decisions

**Last Updated:** 2026-10-29 06:48
**Current Phase:** Complete (phases 1 and 2 and the cross-phase review are done)
**Status:** ✅ Ready for Review

---

## Quick Status

**What's Done:**
- ✅ The plan (`implementation.md`) and the dev docs (`tasks.md`, `inbox.md`, this file)
- ✅ Phase 1 (tasks 1.1 to 1.6): `IBindingSource`, `Store<T>`, `StoreTests` (9)
- ✅ Phase 2 (tasks 2.1 to 2.8): `StoreSubscription<T, TValue>`, `StoreSubscriptionTests` (10), the copied `AllocationGate`, `StoreAllocationTests` (4), the design and changelog lines
- ✅ The cross-phase review (`code-review.md`): 3 LOW findings, 3 fixed. The review verifier returns PASS.
- ✅ The draft plan of the next feature: `docs/features/state-and-commands/element-bindings/implementation.md`

**What's Next:**
- 📋 A person tests the two lines of `## Needs Human Verification`.
- 📋 `/autorun-epic` commits the feature and goes on to `state-and-commands/element-bindings`.

**Blockers:**
- None

---

## Base and Counts

- Base commit: `2ac76be4d374398ba7f0422de0c720945caaa23b`.
- Counts before any code change: solution total 2558 (2556 passed, 2 skipped, 0 failed). `BrutalUi.Core.Tests` 1715. `BrutalUi.Framework.Tests` 1 (the placeholder).
- Counts after phase 1: solution total 2566 (2564 passed, 2 skipped, 0 failed). `BrutalUi.Core.Tests` 1715 (no edit). The phase 1 agent first wrote 1705. The phase 1 verifier ran the Core test project alone and read 1715, the same count as the base commit in `interactive-elements/widget-sample/code-review.md`. `BrutalUi.Framework.Tests` 9 (`StoreTests`; the placeholder is deleted).
- Build: `0 Warning(s)`, `0 Error(s)`.
- `StoreTests`: total 9, failed 0.
- `scripts/test-linux.sh`: `test-linux: PASS build, test, gpu, smoke (120 frames) in 24 s`, `exit=0`.
- Demo grep: `12:/home/aaronstatic/dev/brutal-ui/tests/BrutalUi.Framework.Tests/bin/Debug/net10.0/BrutalUi.Framework.Tests.dll (net10.0|x64) passed (1s 785ms)` (lines 3 and 29 also name the assembly).

- Counts after phase 2 group A (tasks 2.1 to 2.3): solution total 2576 (2574 passed, 2 skipped, 0 failed). Build: `0 Warning(s)`, `0 Error(s)`. `StoreSubscriptionTests`: total 10, failed 0. `StoreTests`: total 9, failed 0. Group B (tasks 2.4 to 2.8) is open.

---

## Key Files

### Core Implementation
- `src/BrutalUi.Core/Binding/IBindingSource.cs` - the one Core contract: `ulong Version { get; }` (phase 1)
- `src/BrutalUi.Framework/State/Store.cs` - `Store<T>`: one value, a version counter, `Set(in T)` (phase 1)
- `src/BrutalUi.Framework/State/StoreSubscription.cs` - `StoreSubscription<T, TValue>`: a store, a static selector, the applied version (phase 2)

### Tests
- `tests/BrutalUi.Framework.Tests/State/StoreTests.cs` - 9 tests (phase 1)
- `tests/BrutalUi.Framework.Tests/State/StoreSubscriptionTests.cs` - 10 tests (phase 2)
- `tests/BrutalUi.Framework.Tests/State/StoreAllocationTests.cs` - the 0-byte gate, 4 tests (phase 2)
- `tests/BrutalUi.Framework.Tests/Shared/AllocationGate.cs` - a copy of the Core.Tests helper (phase 2)

### Related Files
- `docs/features/state-and-commands/store-and-subscription/implementation.md` - Implementation plan
- `docs/features/state-and-commands/epic-overview.md` - the epic rollup and the build order

---

## Important Decisions

The plan holds Decisions 1 to 8 and the Confirmed Assumptions Q1 to Q4 and A1 to A8. This section records each decision that the run makes past the plan.

- **The Pull Model.** The store raises a `ulong` version on each change. A reader compares it with the version that it applied last. The store holds no event, no subscriber list, and no reference to a reader. Many writes before one read give one apply.
- **The Seam.** `Element` lives in Core, and Core does not reference Framework. So the binding list on `Element` cannot name `StoreSubscription<T, TValue>` or `Store<T>`. This feature gives element-bindings `IBindingSource.Version` (a version check with no Framework type) and, in phase 2, `StoreSubscription.Store` plus `AppliedVersion`. element-bindings chooses the shape. This feature adds nothing to `Element` or `Label`.
- **Phase 2 group A.** `StoreSubscription<T, TValue>` follows the brief with no change. The `Store` getter is `public readonly Store<T> Store => _store;` (an expression-bodied property). The constructor sets `_applied = 0` explicitly. Tests use a helper `CreateStore(score, lives)` and a `Decision 4` and `Decision 5` comment in two tests.
- **Phase 2 group B (tasks 2.4 to 2.8).**
  - Build: `0 Warning(s)`, `0 Error(s)`. Solution total 2580 (2578 passed, 2 skipped, 0 failed). That is 2576 plus the 4 new gates. `StoreAllocationTests` 4 of 4, `StoreSubscriptionTests` 10 of 10, `StoreTests` 9 of 9.
  - Plant: `GC.KeepAlive(new object());` as the first line of `Store<T>.Set`. `dotnet test --solution brutal-ui.sln` gave `exit=2`. Two gates failed: `SetAllocatesNothing` (14400 bytes, 100 iterations times 6 sets times 24 bytes) and `TryGetChangedAllocatesNothing` (2400 bytes, 100 sets times 24 bytes). `AVersionReadAllocatesNothing` passes, because its measured body never calls `Set`. The restore came from `Store.cs.orig` in the scratchpad folder, and `cmp` found no difference. After the restore the solution gave 0 failed.
  - Copy check (A6): the `diff` of the two `AllocationGate.cs` files without the `///` lines and the `namespace` line printed no line (exit 0). `tests/BrutalUi.Core.Tests/` has no diff. The Framework test project has no reference to `BrutalUi.Core.Tests`.
  - `scripts/test-linux.sh`: `test-linux: PASS build, test, gpu, smoke (120 frames) in 16 s`, `exit=0`.
  - Lint (`ste-lint.py --fail-over 2.5`): `docs/technical-design.md` 1.52 per 100 words, `CHANGELOG.md` 1.70 per 100 words. Both are under 2.5.
  - Decision past the plan: `AVersionReadAllocatesNothing` first raises the versions to 2, 3, and 2 (a helper `RaiseVersions`), so the sum check (7 per round, 714 after 102 rounds) tells a wrong store order or a missed store. The plan gave no values.
  - Decision past the plan: the scene keeps the subscription in a non-readonly field of a class (a `required` field), so no call works on a copy.
  - Decision past the plan: the section 14 bullet is the "fourteenth allocation gate" (after the thirteenth, in the order of the file) and it holds the plant result. Section 10 holds the store rules, the subscription rules, and the `IBindingSource` line as two lists after the data binding paragraph.
- **Phase 1 note.** `Store.cs` needs `using System;` for the `IEquatable<T>` cref (CS1574 otherwise). The Framework test project sees `IBindingSource` through the transitive reference to Core with no project change.

---

## Gotchas & Learnings

### 1. A doc comment cref needs its namespace
**Problem:** The first build of `Store.cs` failed with CS1574 on the `IEquatable<T>` cref.
**Solution:** Add `using System;` to the file. The projects have no implicit usings.
**Lesson:** Each new file of `src/` needs a `using` for each namespace that a cref names.

### 2. A stale count in a phase report
**Problem:** The phase 1 agent recorded 1705 Core tests. The real count is 1715.
**Solution:** The phase 1 verifier ran the Core test project alone and read 1715. The parent corrected the counts.
**Lesson:** Read a project count from a run of that project, not from the solution summary.

---

## API Integration

- `IBindingSource.Version` in Core is the seam for the binding pass of element-bindings. Notes item 1 of the plan lists the options for the binding list on `Element`.

---

## State Management

- `Store<T>` owns one value and a `ulong` version from 1. A reader pulls a change through the version. The store holds no event and no reference to a reader.

---

## Testing Approach

### Unit Tests
**Files:** `tests/BrutalUi.Framework.Tests/State/*.cs`
- `StoreTests` (9), `StoreSubscriptionTests` (10)

### Allocation Tests
- `StoreAllocationTests` (4): 0 bytes for `Set`, a version read, and `TryGetChanged`

---

## Needs Human Verification

`/autorun-feature` adds one line per item that the verifier cannot check, for a person to test.

- [ ] Phase 1 demo: a person runs `scripts/test-linux.sh` and reads its PASS line. Then the person runs `grep -n 'BrutalUi.Framework.Tests' artifacts/test-linux/test.log` and reads the result of the Framework test assembly with 0 failed (Q4).
- [ ] Phase 2 demo: a unit test creates a `Store<GameState>` and a subscription with the static selector `static s => s.Score`. The first `TryGetChanged` gives `true` and the start score, the second gives `false`, and a `Set` of a new score gives `true` and the new score one time. The developer runs `dotnet run --project tests/BrutalUi.Framework.Tests/BrutalUi.Framework.Tests.csproj -- --filter-class '*StoreSubscriptionTests'` and reads a total of 10 with `failed: 0`, and the same command with `'*StoreAllocationTests'` reads a total of 4 with `failed: 0`.

---

## Next Steps

### Immediate (Next Session)
1. Test the two demo lines of `## Needs Human Verification`.
2. Commit the feature (the caller `/autorun-epic` owns each git action).
3. Build `state-and-commands/element-bindings` from its draft plan. Answer its open scope questions first (the seam, the leave rule, the phase 1 demo, the number culture).

### Future
1. element-bindings uses `IBindingSource.Version` from Core. Its draft keeps one applied version in a Core struct, and a Framework target holds the store and the selector. `StoreSubscription` stays as it is.

---

## Open Questions

- none

---

## Session Notes

### 2026-10-29 05:53
- `/autorun-feature` started the feature in the called mode of `/autorun-epic`. `/start-feature` created `tasks.md` (14 tasks in 2 phases), `inbox.md`, and this file.
- Phase 1 done (game-dev, STANDARD). Verifier: PASS. Build 0 warnings, 0 errors. Solution 2566 (2564 passed, 2 skipped, 0 failed). `StoreTests` 9 of 9. The diff check against `2ac76be` is clean, and `src/BrutalUi.Core/` holds only the new `Binding/` folder. Demo: PASS (`test-linux: PASS build, test, gpu, smoke (120 frames) in 20 s`, and the Framework test assembly passed in `artifacts/test-linux/test.log`).
- Phase 2 done (game-dev, STANDARD, two groups). Verifier: PASS. Build 0 warnings, 0 errors. Solution 2580 (2578 passed, 2 skipped, 0 failed). `StoreTests` 9, `StoreSubscriptionTests` 10, `StoreAllocationTests` 4, each with 0 failed. The plant in `Store<T>.Set` gave `exit=2` (`SetAllocatesNothing` 14400 bytes, `TryGetChangedAllocatesNothing` 2400 bytes), and the restore is byte-identical. The `AllocationGate` copy diff is empty. The design lint scores 1.52 and the changelog lint scores 1.70. Demo: PASS.
- The cross-phase review (game-dev-advanced, `review.md`) wrote `code-review.md`: 3 LOW findings, all on rule 5 (correct remarks). Its probes show that each gate and each rule test fails on a planted fault. Review group 1 (`review-g1.md`) fixed CR-2 (the `<returns>` tag of `TryGetChanged` names the first read) and CR-3 (the `Store` getter states `null` on a default value) in `StoreSubscription.cs`. Review group 2 (`review-g2.md`) fixed CR-1 (the `TextA` and `TextB` comments of the gate scene) in `StoreAllocationTests.cs`. Doc comments only. No code line changed.
- The follow-up questions of the review got the default answers, because the called mode asks nothing. Follow-up 1 (a): keep the order of `_applied` and the selector call, and add a remark that a selector that throws still applies the version. Follow-up 2 (a): no API change, so element-bindings rebuilds a subscription for a re-apply. Follow-up 3 (a): the `Store<T>` remarks and section 10 state that a change of a reference value in place, then a `Set` of the same reference, is no change.
- The review verifier (`review-verify.md`) returns PASS: build 0 warnings, 0 errors, 2580 tests with 0 failed, `exit=0` of `scripts/test-linux.sh`, the copy diff empty, no plant left, and each changed remark matches the code.

### 2026-10-29 06:48
- `/update-feature`: 14 of 14 tasks done. `implementation.md` status is Ready for Review. No advanced phase, so no agent fallback.
- The solution-architect wrote the draft plan of `state-and-commands/element-bindings` (4 phases, 30 tasks). The draft chooses a Core struct list with a public `Element.AddBinding` and Framework extension methods. It does not use `StoreSubscription` for each binding. It changes the Framework test project file (linked tree files). Its Notes hold four open scope questions.
- Inbox: 0 lines, so nothing to fold and no bug to promote.
- Next: a person tests the two demo lines. `/autorun-epic` commits the work.

---

*Update this file at the end of each work session. Run `/update-feature` before compacting conversations.*
