# core-tree/allocation-guard - Context & Decisions

**Last Updated:** 2026-10-18 05:52
**Current Phase:** Complete (all five phases and the cross-phase review done)
**Status:** 🟢 Ready for Review

---

## Quick Status

**What's Done:**
- ✅ Implementation plan (`implementation.md`), accepted. The user chose option A for Q1 to Q4.
- ✅ Dev docs scaffolded (`context.md`, `tasks.md`, `inbox.md`)
- ✅ Phase 1 (tasks 1.1 to 1.10): `Shared/TextMeasure.cs`, `Shared/ThousandElementTree.cs`, and `ThousandElementAllocationTests.cs` (6 tests). The gate reads 0 bytes in 5 runs. A planted allocation fails the gate.
- ✅ Phase 2 (tasks 2.1 to 2.4): `TreeLayoutBenchmark` with 3 rows and no allocation.
- ✅ Phase 3 (tasks 3.1 to 3.4): `Shared/ChainTree.cs`, `PercentChainBudgetTests` (2 tests), and `PercentChainBenchmark` with 2 rows and no allocation.
- ✅ Phase 4 (tasks 4.1 to 4.4): `StackProbe`, `FactsReport`, and the `--facts` mode. Release: 519 levels on a 1 MB stack.
- ✅ Phase 5 (tasks 5.1 to 5.5): `CHANGELOG.md`, the full benchmark table, and the doc edits.
- ✅ Cross-phase review (`code-review.md`): 10 findings (0 HIGH, 2 MEDIUM, 8 LOW). Review groups 1 to 5 fixed CR-1 to CR-9. CR-10 (the cost of a deep percent chain) goes to the epic as an engine hand-off. Review group 6 fixed four verifier notes. Verifiers `review-verify.md` and `review-verify2.md`: PASS.

**What's Next:**
- 📋 A person runs the items of `## Needs Human Verification` (the two Windows runs first).
- 📋 The caller (`/autorun-epic`) commits the work and runs `/update-epic core-tree` and `/update-master`.
- 📋 `/update-epic core-tree` records CR-10 under "Tech Debt / Findings" of `epic-overview.md` as an engine follow-up.

**Blockers:**
- none

---

## Key Files

### Core Implementation
- `tests/BrutalUi.Core.Tests/Shared/` - The shared builders (`TextMeasure`, `ThousandElementTree`, `ChainTree`). The test project compiles them, and the benchmark project links them.
- `tests/BrutalUi.Core.Tests/ThousandElementAllocationTests.cs` - The gate on the 1000-element tree
- `tests/BrutalUi.Core.Tests/Layout/PercentChainBudgetTests.cs` - The CR-1 call budget
- `tests/BrutalUi.Benchmarks/` - `TreeLayoutBenchmark`, `PercentChainBenchmark`, `StackProbe`, `FactsReport`, and `Program`

### Related Files
- `tests/BrutalUi.Core.Tests/TreeAllocationTests.cs` and `tests/BrutalUi.Core.Tests/Layout/LayoutAllocationTests.cs` - The pattern of the gate
- `docs/features/core-tree/flexbox-layout/context.md` - The hand-offs (CR-1, CR-10, the tree memory)
- `docs/features/core-tree/allocation-guard/implementation.md` - Implementation plan

---

## Important Decisions

The plan holds decisions 1 to 7 and the assumptions A1 to A12. This section records the decisions made during the build.

- The run is the called mode of `/autorun-feature`, from `/autorun-epic`. The orchestrator makes no git write. The caller commits.
- The user chose the recommended option (A) of each of the four open scope questions (Q1 to Q4) of the plan Notes.
- Phase 1: the final section table of `ThousandElementTree` is the plan table with no move. The root holds the header, the body, the footer, the tooltip, the modal, and the settings panel. The body holds the sidebar, the grid, and the stats panel.

  | # | Section | Elements |
  |---|---------|----------|
  | 1 | root | 1 |
  | 2 | header | 14 |
  | 3 | body | 1 |
  | 4 | sidebar | 151 |
  | 5 | grid | 361 |
  | 6 | stats | 201 |
  | 7 | footer | 31 |
  | 8 | tooltip | 7 |
  | 9 | modal | 13 |
  | 10 | settings (hidden full-screen overlay, absolute, 100 percent by 100 percent) | 220 |
  | | **Total** | **1000** |

  The tree has 109 callback leaves, as the plan states. `Build()` throws `InvalidOperationException` when the count is not 1000. The check runs in `Collector.Make` for a tree of more than 1000 elements, and after the build for a tree of fewer elements (review-g1, CR-8).
- Phase 1 (changed in review-g1, CR-1): the settings panel is an absolute overlay (`Left = 0`, `Top = 0`, `Width` and `Height` of 100 percent). In the first build it was an in-flow column of 876 pixels. When it showed, the body shrank to 0 height in half of the measured steps. As an overlay, the panel takes no space in the flow, and the body keeps 592 pixels. The count stays 1000, and the hidden zero-out stays the same.
- Phase 1 (changed in review-g1, CR-2): `SlotWideWidth` is 100 (was 60). At 1280 pixels, line 1 holds 104 + 12 x 52 = 728 pixels, which is more than 695.2. So slot 12 moves to line 2. At 1024 pixels, line 1 holds 9 slots in place of 10.
- Phase 1: the body and the grid get `FlexShrink = 1`. The plan table names no shrink value. Without it, the auto basis of the wrapped grid is about 1248 pixels (one line at the `AtMost` width of the body), and the stats panel goes off the screen. The body also overflows the root by 112 pixels. With shrink, the body is 592 pixels high, the grid is 695.2 pixels wide with 13 slots on each of 10 lines, and `AlignContent.SpaceBetween` has free space to spread. The tree also runs the shrink path. A scratch probe checked these rects.
- Phase 1: the builder adds some detail fields that the table does not name. They make a more real screen and change no count: `AlignItems.Center` on the rows and buttons, `Justify.Center` on the buttons, a padding of 4 on the tooltip and 8 on the modal, a margin of 4 on each paragraph, a height of 16 on each stat value leaf, a height of 32 on the modal title row, a close button of 24 by 24, modal buttons of 80 by 32, `Justify.FlexEnd` on the modal button row, and grow 1 on each settings label and on the modal body.
- Phase 1: the change values are public constants of `ThousandElementTree` (`ValueNarrowPercent`, `SlotWideWidth`, `ViewportNarrowWidth`, and the others), so phase 2 can use the same values.
- Phase 1: `TheFullStepVisitsEachVisibleElement` runs two rounds. The last step is the kind 7 step of round 2, and the settings panel is hidden in round 2. The test also asserts that the settings subtree holds 220 elements.
- Phase 1: no `src/` change. The gate measured 0 bytes on the first run, so the tier note did not apply.

---

## Gotchas & Learnings

- Phase 1: the shared files compile in a scratch console project with only a project reference to `BrutalUi.Core` and no `InternalsVisibleTo`. Phase 2 can link them as planned.
- Phase 1: the planted `GC.KeepAlive(new object());` gives 2400 bytes over 100 steps (24 bytes for each step). The gate thus sees one small object in each step.
- Phase 1: the header title has grow 1, so `Justify.SpaceBetween` on the header has no free space to spread. This is as the plan states, and it changes no result.
- **The test count of the plan is the solution total (phase 1 verifier).** The 566 tests "before the feature" are the total of `dotnet test --solution brutal-ui.sln`: `BrutalUi.Core.Tests` 564, plus 1 each in the Markup and Framework test projects. Verification step 2 of the plan says "574 or more for `BrutalUi.Core.Tests`". Read it as the solution total: 572 after phase 1, 574 after phase 3. `BrutalUi.Core.Tests` alone gives 570 after phase 1. The plan stays as written.

---

## Testing Approach

### Unit Tests
**Files:** `tests/BrutalUi.Core.Tests/ThousandElementAllocationTests.cs`, `tests/BrutalUi.Core.Tests/Layout/PercentChainBudgetTests.cs`
- The gate, the probe self-check, the mix of the tree, the rect changes of each kind, the callback counts, and the full-step visit (6 tests)
- The call budget of one leaf mark for each percent chain, with a fresh layout check (2 tests)

### Integration Tests
- `scripts/test-linux.sh` runs the gate through `dotnet test`.
- (MANUAL) `scripts\test-windows.ps1` on Windows.

---

## Needs Human Verification

`/autorun-feature` adds one line per item that the verifier cannot check, for a person to test.

- [ ] Phase 1 demo: the developer runs `dotnet run --project tests/BrutalUi.Core.Tests/BrutalUi.Core.Tests.csproj -- --filter-class '*ThousandElementAllocationTests'` and reads a total of 6 with `failed: 0`.
- [ ] Phase 2 demo: the developer runs `dotnet run -c Release --project tests/BrutalUi.Benchmarks/BrutalUi.Benchmarks.csproj -- --filter '*TreeLayoutBenchmark*' --job short` and sees a summary table with the rows `FullLayout`, `DirtySubtree`, and `CleanUpdate`, each with `-` or `0 B` in the `Allocated` column.
- [ ] Phase 3 demo: the developer runs `dotnet run --project tests/BrutalUi.Core.Tests/BrutalUi.Core.Tests.csproj -- --filter-class '*PercentChainBudgetTests'` and reads a total of 2 with `failed: 0`.
- [ ] Phase 4 demo: the developer runs `dotnet run -c Release --project tests/BrutalUi.Benchmarks/BrutalUi.Benchmarks.csproj -- --facts` and reads four lines that start with `facts:`. The last line names the number of levels that pass on a 1 MB stack in a Release build.
- [ ] Phase 5 demo: the developer opens `CHANGELOG.md` and reads, under `## [Unreleased]`, a table of the 5 benchmarks with the mean and the allocated bytes, the machine line of BenchmarkDotNet, and the four facts lines.
- [ ] Full benchmark run: the developer runs `dotnet run -c Release --project tests/BrutalUi.Benchmarks/BrutalUi.Benchmarks.csproj -- --filter '*'` (several minutes) and sees a summary table of the 5 benchmarks, each with `-` in the `Allocated` column.
- [ ] Windows facts run (CR-5): on Windows, run `dotnet run -c Release --project tests/BrutalUi.Benchmarks/BrutalUi.Benchmarks.csproj -- --facts`. Expect four `facts:` lines, a stack line with a level count, no error dialog that blocks the run, and exit code 0.
- [ ] Windows run: on Windows, after `git pull --ff-only`, run `.\scripts\test-windows.ps1`. Expect the PASS line and `$LASTEXITCODE` 0.

---

## Performance Considerations

- **Current record (review-g5).** These are the numbers of the final code, after all review fixes. One run of `dotnet run -c Release --project tests/BrutalUi.Benchmarks/BrutalUi.Benchmarks.csproj -- --filter '*'` (the default job) and one run of `-- --facts` on 2026-10-18, at commit `a8e7846` with the uncommitted allocation-guard work. No test asserts a time. A `-` in the `Allocated` column means 0 bytes. The settings panel is hidden, so the pass of `FullLayout` lays out 780 elements and sets the 220 elements of the panel to zero. `CHANGELOG.md` holds the same table and the same facts lines. The older lines of this section (phases 1 to 4) are history.

  | Method                | Mean          | Error        | StdDev       | Allocated |
  |---------------------- |--------------:|-------------:|-------------:|----------:|
  | FullLayout            | 206,445.66 ns | 2,220.564 ns | 2,077.117 ns |         - |
  | DirtySubtree          |   8,573.94 ns |   105.449 ns |    98.637 ns |         - |
  | CleanUpdate           |      23.04 ns |     0.219 ns |     0.204 ns |         - |
  | PaddingChainLeafMark  |      4.893 ms |    0.0567 ms |    0.0530 ms |         - |
  | MaxWidthChainLeafMark |      3.722 ms |    0.0355 ms |    0.0314 ms |         - |

  Machine line of BenchmarkDotNet:

  ```
  BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04 (Resolute Raccoon)
  Intel Core i7-6700K CPU 4.00GHz (Max: 0.80GHz) (Skylake), 1 CPU, 8 logical and 4 physical cores
  .NET SDK 10.0.112
    DefaultJob : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ```

  Facts lines of the `--facts` mode (Release, `exit=0`):

  ```
  facts: tree elements 1000, bytes 696944, bytes per element 696.9
  facts: percent padding chain depth 20: 3031 callback calls for one leaf mark (budget 4000)
  facts: percent max width chain depth 20: 5674 callback calls for one leaf mark (budget 7400)
  facts: plain column chain on a 1 MB stack: 519 levels pass (Release build)
  ```
- Phase 5, the stack paragraph (task 5.3). The measured Release limit is 519, not 520. Section 6 of `docs/technical-design.md` now says 292 levels (Debug) and 519 levels (Release), with "(measured by `--facts`)". The 8 MB numbers stay as the review gave them, because `--facts` measures only the 1 MB stack. The advice of 200 levels stays.
- Phase 1, the gate in 5 runs of `--filter-class '*ThousandElementAllocationTests'` (Debug, `--no-build` after one build): 0 bytes in each run (the gate asserts `0L` and passed 5 of 5). Each run gave a total of 6 and `failed: 0`.
- Phase 1, the run time of the class: 667 ms, 704 ms, 671 ms, 666 ms, and 662 ms. This is far below the 5 s limit of the tier note, so the full kind stays at one in 8 steps.
- Phase 1, task 1.10: with `GC.KeepAlive(new object());` as the first line of `Step`, `dotnet test --solution brutal-ui.sln` gave `exit=2`, and the gate read `Actual: 2400`. After the restore from the scratch copy (`cmp` equal), `dotnet test` gave `exit=0` with 572 passed and 0 failed.
- Phase 1, the suite: 572 tests (566 before, plus 6), 0 failed. `scripts/test-linux.sh` printed `test-linux: PASS build, test, gpu, smoke (120 frames) in 9 s` and `exit=0`. `git diff main -- scripts/` is empty.
- Phase 1, the first `Update` of the tree calls the label callback 198 times and the paragraph callback 3 times.
- Phase 2, the short job (`--filter '*TreeLayoutBenchmark*' --job short`; BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04, Intel Core i7-6700K, .NET SDK 10.0.112). This is the short job, not the release numbers.

  | Method       | Mean          | Error         | StdDev       | Allocated |
  |------------- |--------------:|--------------:|-------------:|----------:|
  | FullLayout   | 205,179.30 ns | 66,449.893 ns | 3,642.343 ns |         - |
  | DirtySubtree |   8,462.72 ns |  1,299.442 ns |    71.227 ns |         - |
  | CleanUpdate  |      20.22 ns |      7.327 ns |     0.402 ns |         - |

- Phase 3, the short job (`--filter '*PercentChainBenchmark*' --job short`; same machine). This is the short job, not the release numbers.

  | Method                | Mean     | Error     | StdDev    | Allocated |
  |---------------------- |---------:|----------:|----------:|----------:|
  | PaddingChainLeafMark  | 4.856 ms | 1.0598 ms | 0.0581 ms |         - |
  | MaxWidthChainLeafMark | 3.735 ms | 0.7312 ms | 0.0401 ms |         - |

- Phase 3, the call counts, read from the failure message with a budget of 1 (the budget was restored): the padding chain gave 3031 calls (budget 4000) and the max width chain gave 5674 calls (budget 7400). Both counts equal the counts of the review. Phase 4 prints and records them with the facts report.
- Phase 3, the max width chain has more calls but a lower time than the padding chain. A call of the stub is cheap, so the time depends on the rest of the layout work.

- Phase 4, two Release runs of `dotnet run -c Release --project tests/BrutalUi.Benchmarks/BrutalUi.Benchmarks.csproj -- --facts`. Both runs gave the same four lines and `exit=0`:
  - `facts: tree elements 1000, bytes 699800, bytes per element 699.8`
  - `facts: percent padding chain depth 20: 3031 callback calls for one leaf mark (budget 4000)`
  - `facts: percent max width chain depth 20: 5674 callback calls for one leaf mark (budget 7400)`
  - `facts: plain column chain on a 1 MB stack: 519 levels pass (Release build)`
- Phase 4, the run time of the report: 45.28 s and 45.13 s (wall clock of `dotnet run`, build already done). The time was the binary search: 18 child processes (two checks, then 16 halvings of the range up to 65536), each about 2.5 s. Review-g3 (CR-6) replaced the search. The run time is now 10.16 s in Release (7 failed children), see the session note of review-g3.
- Phase 4, the Debug run (`-- --facts`, no `-c`): the same first three lines, `exit=0`, 53.10 s (review-g3: 10.09 s). The stack line reads `facts: plain column chain on a 1 MB stack: 292 levels pass (Debug build)`.
- Phase 4, the difference from the CR-10 numbers: Release gives 519 (CR-10: 520) and Debug gives 292 (CR-10: 293). Each is one level lower. The probe of CR-10 was a scratch program, so the count of the root or the thread start-up frames probably differs by one. The result is the same within one level, so section 6 of the technical design ("under 200 levels on a 1 MB stack") still holds. Phase 5 decides the text of the stack paragraph.
- Phase 4, what the 699,800 build bytes hold: the 1000 `Element` objects, their style and layout state, the child lists and the pool arrays, the `Element[]` named `All`, the `UiContext`, and the two `TextMeasure` objects with their delegates. The number is the whole allocation of `ThousandElementTree.Build()` on the current thread, so it is not "bytes in the elements". The 699.8 bytes per element is an upper bound for one element.
- Phase 4, an overflow of a child (`--stack-child 2000 1048576`, run by hand with the output sent to files): exit code 134 (SIGABRT), nothing on the standard output, and 2109 lines of trace on the standard error that start with `Stack overflow.`. The parent of the probe reads both streams, so no trace reaches the console in a `--facts` run. The shell printed "Aborted (core dumped)", but `ulimit -c` is 0, `core_pattern` pipes to apport, and no core file appeared in the working folder. No new report appeared in `/var/crash` (the only dotnet report is from 2026-10-17, from the CR-10 probe). The runtime writes no mini dump, because `DOTNET_DbgEnableMiniDump` is not set. On a machine with `ulimit -c unlimited`, each overflow can write a core file, so 18 files are possible.

---

## Open Questions

- none

---

## Next Steps

1. A person runs the items of `## Needs Human Verification`. The two Windows runs (`--facts` and `scripts\test-windows.ps1`) are the checks that no agent can run.
2. The caller commits the work as `feat: core-tree/allocation-guard`.
3. `/update-epic core-tree` closes the three hand-offs of flexbox-layout (CR-1 budget, CR-10 stack limit, tree memory) and records CR-10 of this feature as an engine follow-up: one leaf mark in a 20-level percent chain costs about 3.7 to 4.9 ms.

---

## Session Notes

### 2026-10-18 04:34
- Dev docs scaffolded from the accepted plan. 27 tasks across 5 phases.

### 2026-10-18 04:43
- Phase 1 done: tasks 1.1 to 1.10. New files: `tests/BrutalUi.Core.Tests/Shared/TextMeasure.cs`, `tests/BrutalUi.Core.Tests/Shared/ThousandElementTree.cs`, and `tests/BrutalUi.Core.Tests/ThousandElementAllocationTests.cs`. No `src/` change.
- Blast radius: none. The new files are test code only. No existing file of `src/` or `tests/` changed.

### 2026-10-18 04:48 (orchestrator)
- Phase 1 verifier (`phase-1-verify.md`): PASS. Demo: PASS (`total: 6`, `failed: 0`). Build 0 warnings and 0 errors, also with `--no-incremental`. Solution total 572, 0 failed. The planted allocation gave `exit=2` (`Actual: 2400`), and the restore gave 0 failed. `scripts/test-linux.sh` PASS in 12 s. No `src/`, `scripts/`, or `Directory.Packages.props` change.
- The orchestrator accepts `FlexShrink = 1` on the body and the grid. The plan table names no shrink value, and the screen fits the viewport with it.
- Inbox: no line.

### 2026-10-18 (phase 2)
- Phase 2 done: tasks 2.1 to 2.4. `BrutalUi.Benchmarks.csproj` links `Shared/*.cs` (Decision 1) and has 1 `PackageReference` and 1 `ProjectReference`. New `TreeLayoutBenchmark.cs`. `PlaceholderBenchmark.cs` deleted. No file of `Shared/` changed.
- The generated BenchmarkDotNet build needed no project setting. The in-process toolchain was not needed.
- The Release run showed `-` in the `Allocated` column of all three rows. `git status --porcelain | grep -c BenchmarkDotNet.Artifacts` gave 0.
- Build: 0 warnings, 0 errors. Tests: 572 passed, 0 failed. `dotnet test` runs no benchmark.
- `TreeLayoutBenchmark` uses a `using BrutalUi.Core;` line in addition to `using BrutalUi.Core.Tests;`, because the namespace `BrutalUi.Benchmarks` does not resolve the Core types.

### 2026-10-18 04:52 (orchestrator)
- Phase 2 verifier (`phase-2-verify.md`): PASS. Demo: PASS. The short job gave `FullLayout` 202,740.99 ns, `DirtySubtree` 8,234.75 ns, and `CleanUpdate` 20.17 ns, each with `-` under `Allocated`. Build 0 warnings and 0 errors (Debug and Release). Solution total 572, 0 failed. No `BenchmarkDotNet.Artifacts` entry in `git status`.
- Inbox: no line.

### 2026-10-18 (phase 3)
- Phase 3 done: tasks 3.1 to 3.4. New files: `Shared/ChainTree.cs`, `Layout/PercentChainBudgetTests.cs`, and `PercentChainBenchmark.cs`. `LayoutCacheTests.cs` did not change. No `src/` change.
- `ChainTree` names each level with the literal `"level"`, so the build creates no name string. The properties are `required` with `init`, as `ThousandElementTree`.
- The benchmark build accepts `ChainTree.cs` with no `InternalsVisibleTo`.
- Build: 0 warnings, 0 errors. Tests: 574 passed, 0 failed. Demo: `total: 2`, `failed: 0`. `git status --porcelain | grep -c BenchmarkDotNet.Artifacts` gave 0.

### 2026-10-18 04:56 (orchestrator)
- Phase 3 verifier (`phase-3-verify.md`): PASS. Demo: PASS (`total: 2`, `failed: 0`). Build 0 warnings and 0 errors (Debug and Release). Solution total 574, 0 failed. The call counts equal the review counts: 3031 (budget 4000) and 5674 (budget 7400). The short job gave `PaddingChainLeafMark` 4.873 ms and `MaxWidthChainLeafMark` 3.717 ms, each with `-` under `Allocated`. `LayoutCacheTests.cs`, `src/`, `scripts/`, and `Directory.Packages.props` did not change.
- Inbox: no line.

### 2026-10-18 (phase 4)
- Phase 4 done: tasks 4.1 to 4.4. New files: `tests/BrutalUi.Benchmarks/StackProbe.cs` and `tests/BrutalUi.Benchmarks/FactsReport.cs`. `Program.cs` now dispatches `--facts` and `--stack-child` before the switcher (Decision 5). No `Shared/` change and no `src/` change.
- `FactsReport.cs` needs `using BrutalUi.Core;` for `DirtyFlags`, as the other benchmark files.
- `StackProbe` checks depth 1 and depth 65536 first, then halves the range. If 65536 passes, it returns 65536. The child runs `Update` on a `new Thread(..., stackBytes)`.
- Build: 0 warnings, 0 errors. Tests: 574 passed, 0 failed (no new test in this phase). `git status --porcelain | grep -c BenchmarkDotNet.Artifacts` gave 0.

### 2026-10-18 05:03 (orchestrator)
- Phase 4 verifier (`phase-4-verify.md`): PASS. Demo: PASS. The Release report printed the four `facts:` lines (1000 elements, 699800 bytes, 3031 of 4000 calls, 5674 of 7400 calls, 519 levels in a Release build) and exited with 0 in 44.7 s. The Debug report gave 292 levels in 52.9 s. No overflow trace reached the console, and no core file appeared. Build 0 warnings and 0 errors (Debug and Release). Solution total 574, 0 failed.
- The measured Release limit (519) differs from the CR-10 number (520) by one level. Task 5.3 therefore changes the numbers of the stack paragraph in section 6 of `docs/technical-design.md`.
- Inbox: no line.

### 2026-10-18 05:15 (phase 5)
- Phase 5 done: tasks 5.1 to 5.5. New file: `CHANGELOG.md`. Changed: `docs/technical-design.md` (sections 6 and 14), `docs/features/core-tree/human-testing.md`, and this file. No `src/`, `scripts/`, or test change.
- Build: 0 warnings, 0 errors. Tests: 574 passed, 0 failed. `scripts/test-linux.sh`: `test-linux: PASS build, test, gpu, smoke (120 frames) in 9 s`, `exit=0`.
- Lint (`ste-lint.py --fail-over 2.5`, exit 0 on each): `CHANGELOG.md` 1.44, `docs/technical-design.md` 1.12, `docs/features/core-tree/human-testing.md` 1.80, this file 1.70.
- The facts run of task 5.1 gave the same four lines as phase 4 (519 levels in Release). `git status --porcelain | grep -c BenchmarkDotNet.Artifacts` gave 0.

### 2026-10-18 05:12 (orchestrator)
- Phase 5 verifier (`phase-5-verify.md`): PASS. It ran steps 1 to 11 of the Verification Method. Demo: PASS. Build 0 warnings and 0 errors. Solution total 574 (Core 572), 0 failed. The planted allocation gave `exit=2`, and the restore gave 0 failed. The short job gave 5 rows with `-` under `Allocated`. `--facts` gave `exit=0`. `CHANGELOG.md` and "Performance Considerations" match. Sections 6 and 14 match the numbers. Each changed doc lints under 2.5. `scripts/test-linux.sh` PASS. No `src/`, `scripts/`, or `Directory.Packages.props` change.
- All 27 tasks are done. Next: the cross-phase review (`review.md`).
- Inbox: no line.

### 2026-10-18 (review-g1)
- Fixed CR-1, CR-2, and CR-8. Files: `tests/BrutalUi.Core.Tests/Shared/ThousandElementTree.cs`, `tests/BrutalUi.Core.Tests/ThousandElementAllocationTests.cs`, `code-review.md`, and this file. No `src/` change.
- CR-1: the settings panel is an absolute overlay. `TheMeasuredBodyChangesEachPart` asserts a body height of 592 before the rotation and after round 1 (the panel shows).
- CR-2: `SlotWideWidth` is 100. The test asserts that the Y of grid slot 12 (122 pixels at the start) changes when the wrap kind runs alone, returns after the reverse step, differs after round 1, and equals the start value after round 2. The isolated check runs before the rotation, because kind 6 also moves slot 12 in round 1 (the viewport goes to 1024).
- CR-8: `Collector.Make` throws `InvalidOperationException("The tree must hold exactly 1000 elements.")` when `Count` equals `All.Length`.
- Results: `dotnet build brutal-ui.sln` gave 0 warnings and 0 errors. The Release build of the benchmark project gave 0 warnings and 0 errors. The gate class gave total 6 and failed 0 in each of 3 runs. The planted `GC.KeepAlive(new object());` in `Step` gave `exit=2`, and `cmp` confirmed the restore. The solution gave 574 total, 0 failed.
- Open: CR-3 and CR-4 (review-g2) change the same test. CR-5 to CR-7 and CR-9 belong to other groups.

### 2026-10-18 (review-g2)
- Fixed CR-3 and CR-4. Files: `tests/BrutalUi.Core.Tests/ThousandElementAllocationTests.cs`, `code-review.md`, and this file. No `src/` change. The wrap-kind check of review-g1 stays.
- CR-3: `TheMeasuredBodyChangesEachPart` records the sidebar width at the start and asserts it is 20 percent of the body width. After round 1, it asserts that the last element of `All` (a settings toggle) has a width of 24, that the sidebar width is 20 percent of the narrower body, and that it is smaller than the start width. After round 2, it asserts the start width again. The class has the new constants `SidebarPercent` and `SettingsToggleWidth`.
- CR-4: `FullKind = 7` is a constant, and `Step` uses `case FullKind:`. The `default` branch is empty (no kind reaches it). `TheFullStepVisitsEachVisibleElement` runs steps until `StepCount % KindCount == FullKind` and the panel is hidden (with a guard of `KindCount * 4` steps), then runs one step. A remark names the rule: a new kind gets a constant after `FullKind`, and each test finds its kind by the constant.
- Results: `dotnet build brutal-ui.sln` gave 0 warnings and 0 errors. The gate class gave total 6 and failed 0 in each of 3 runs. The planted `GC.KeepAlive(new object());` in `Step` gave `exit=2`, and `cmp` confirmed the restore. The solution gave 574 total, 0 failed.
- Open: CR-5 to CR-7 and CR-9 belong to other groups.

### 2026-10-18 (review-g3)
- Fixed CR-5 and CR-6. Files: `tests/BrutalUi.Benchmarks/StackProbe.cs`, `tests/BrutalUi.Benchmarks/FactsReport.cs` (the stack line only), `code-review.md`, and this file. No `src/` change.
- CR-5: `Passes` reads the standard error text. The new method `Interpret(depth, exitCode, errorText)` returns `true` for code 0, `false` for a non-zero code with text that starts with `Stack overflow.`, and throws `InvalidOperationException` (code and first line) for each other non-zero code. A child that does not end in one minute (the wait for both streams and `WaitForExit(TimeSpan)`) gets `Kill(entireProcessTree: true)`, and the probe throws with the depth.
- CR-6: `FindLimit` is exponential. It starts at `StartDepth` 64, doubles up to `MaxDepth` 8192, then halves the range between the last pass and the first fail. Depth 1 runs only when depth 64 fails, and it throws when it fails. `FindLimit` still returns an `int`. The value 8192 means "8192 or more", and `FactsReport` prints `8192 or more` in that case. The remark now says "The layout pass uses several stack frames for each level".
- How the throw paths were tested: a scratch console program in the session scratch folder links `StackProbe.cs` (with the timeout set to 5 s) and a stub `ChainTree`. The stub child, selected by an environment variable, throws an unhandled exception (exit 134, text `Unhandled exception.`), hangs, or writes `Stack overflow.` and exits with 134 above depth 300. Results: the throw gave `InvalidOperationException` with "exit code 134" and the first line `Unhandled exception. System.InvalidOperationException: boom from child`. The hang ended after 5.2 s with "did not end in 5 s. The probe killed it.", and no child stayed alive. The fake overflow gave the limit 300. A stub with no fault passed to the cap and gave 8192. Direct calls of `Interpret` gave `true` for code 0, `false` for 134 and for -1073741571 (with the text), and a throw for code 1 with another text.
- Real runs, Release (`dotnet run -c Release ... -- --facts`): 10.161 s and 10.162 s, four `facts:` lines, `exit=0`, stack line `519 levels pass (Release build)`, 7 failed children (1024, 768, 640, 576, 544, 528, 520). Debug: 10.085 s, `292 levels pass (Debug build)`, `exit=0`, 7 failed children (512, 384, 320, 304, 296, 294, 293). No overflow trace reached the console. Before: 13 failed children and about 45 s.
- Results: `dotnet build brutal-ui.sln` gave 0 warnings and 0 errors. The Release build of the benchmark project gave 0 warnings and 0 errors. The solution gave 574 total, 0 failed. `git status --porcelain | grep -c BenchmarkDotNet.Artifacts` gave 0.
- Open: CR-7 and CR-9 belong to other groups. The memory line of the facts report is unchanged (699800 bytes).

### 2026-10-18 (review-g4)
- Fixed CR-7. Files: `tests/BrutalUi.Benchmarks/FactsReport.cs`, `code-review.md`, and this file. No `src/` change. The stack line change of review-g3 stays.
- `FactsReport.Run` calls the new method `WarmUpBuild` (marked `NoInlining`), which builds one tree and drops it. Then the report measures a second `ThousandElementTree.Build()`. The remarks of the class name what the build holds (the 1000 `Element` objects, the child lists and pool arrays, the `All` array, the `UiContext`, the two `TextMeasure` objects with their delegates) and say that the number holds no one-time type set-up.
- New memory line (Release): `facts: tree elements 1000, bytes 696944, bytes per element 696.9`. It matches the number of the review. The old number was 699800.
- Real run, Release: four `facts:` lines, `exit=0`, about 10.2 s. Padding chain 3031 calls (budget 4000), max width chain 5674 calls (budget 7400), stack line `519 levels pass (Release build)`.
- Results: `dotnet build brutal-ui.sln` gave 0 warnings and 0 errors. The Release build of the benchmark project gave 0 warnings and 0 errors. The solution gave 574 total, 0 failed.
- Not done on purpose (review-g5): `CHANGELOG.md`, the section "Performance Considerations" of this file, and section 14 of `docs/technical-design.md` still show 699800 bytes. Review group 5 updates them with 696944 bytes and 696.9 bytes for each element.

### 2026-10-18 (review-g5)
- Fixed CR-9 and wrote the final record. Files: `tests/BrutalUi.Benchmarks/TreeLayoutBenchmark.cs` (one sentence in the remarks), `CHANGELOG.md`, `docs/technical-design.md` (section 14), `code-review.md` (CR-9 checked), and this file. No `src/` change.
- CR-9: the remarks, section 14, and the table text of `CHANGELOG.md` say that the settings panel is hidden, so the pass lays out 780 elements and sets the 220 elements of the panel to zero.
- R5.1: one default-job run (the benchmark step ran 47.29 s for `PercentChainBenchmark` and 59.6 s for `TreeLayoutBenchmark`) and one `--facts` run on the final code. The new numbers are in "Performance Considerations" under "Current record (review-g5)": `FullLayout` 206,445.66 ns, `DirtySubtree` 8,573.94 ns, `CleanUpdate` 23.04 ns, `PaddingChainLeafMark` 4.893 ms, `MaxWidthChainLeafMark` 3.722 ms, each with `-` under `Allocated`. The machine line now reads `Max: 0.80GHz` (was `Max: 4.40GHz`), because the CPU reported a low clock at the run start. The facts lines: 696944 bytes (696.9 for each element), 3031 and 5674 calls, 519 levels (Release).
- R5.3: the stack limits equal the old numbers (519 in Release, and the Debug limit stays at 292 from the review-g3 run). Section 6 did not change.
- Results: `dotnet build brutal-ui.sln` gave 0 warnings and 0 errors. The solution gave 574 total, 0 failed. `scripts/test-linux.sh` printed `test-linux: PASS build, test, gpu, smoke (120 frames) in 9 s` and `exit=0`.

### 2026-10-18 (review-g6)
- Fixed the four LOW notes V1 to V4 of the review verifier. Files: `tests/BrutalUi.Core.Tests/ThousandElementAllocationTests.cs`, `tests/BrutalUi.Benchmarks/FactsReport.cs`, `tests/BrutalUi.Benchmarks/StackProbe.cs`, `code-review.md`, and this file. No `src/` change. `Step`, the warm-up, and the measured region are unchanged.
- V1: added the space after the comma. V2: removed the second `SlotNarrowWidth` assert (the former line 263). No step ran between the two, so both checked the same moment. V3: wrapped the comment at 120 characters.
- V4: `Passes` starts one `Stopwatch` before the stream wait. The stream wait uses `ChildTimeout`, and the exit wait uses the time that remains (new helper `Remaining`). So the child gets 1 minute in total. The kill of the process tree, the throw, and the drain of both streams before the exit wait stay. The remark of the class says "for the streams and the exit together". The CR-5 rules of `Interpret` are unchanged.
- Results: `dotnet build brutal-ui.sln` gave 0 warnings and 0 errors. The Release build of the benchmark project gave 0 warnings and 0 errors. The solution gave 574 total, 0 failed. The gate class gave 6 of 6, 0 failed. The Release facts run gave the four record lines (696944 bytes, 3031 calls, 5674 calls, 519 levels) and `exit=0`.

### 2026-10-18 05:52 (orchestrator, `/update-feature`)
- Cross-phase review (`code-review.md`): 10 findings (0 HIGH, 2 MEDIUM, 8 LOW). Review groups 1 to 5 (`review-g1.md` to `review-g5.md`) fixed CR-1 to CR-9. CR-10 (one leaf mark in a 20-level percent chain costs 3.7 to 4.9 ms) is an engine hand-off: the plan forbids a `src/` change with no allocation, so the epic rollup owns it. Verifier `review-verify.md`: PASS (the full Verification Method and each finding). Review group 6 fixed four verifier notes (V1 to V4). Verifier `review-verify2.md`: PASS. V5 (one long comment line in `StackProbe.cs`) stays, with a reason in `code-review.md`.
- The orchestrator accepts two small changes of the agents: `StackProbe` checks depth 1 only when depth 64 fails (the throw rule is the same), and `TheMeasuredBodyChangesEachPart` checks the wrap kind on its own, because kind 6 also moves slot 12.
- The machine line of the current record reads `Max: 0.80GHz`, a value that BenchmarkDotNet read at the start of the run. The means match the earlier runs at `Max: 4.40GHz`, so the numbers stand.
- The final state: 574 tests in the solution (8 new), 0 failed. Build 0 warnings and 0 errors. `scripts/test-linux.sh` PASS. No `src/`, `scripts/`, or `Directory.Packages.props` change. The release facts report runs in about 10 s.
- Inbox: no line in the run. Nothing to promote.
- All 27 tasks are done. Status: Ready for Review.

---

*Update this file at the end of each work session. Run `/update-feature` before compacting conversations.*
