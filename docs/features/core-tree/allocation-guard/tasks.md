# core-tree/allocation-guard - Task Checklist

**Last Updated:** 2026-10-18 05:52
**Progress:** 27/27 tasks complete (100%)
**Advanced phases:** Phase 1 (ADVANCED, opus). Phases 2, 3, 4, and 5 are STANDARD (sonnet).

Test paths: `Shared/` means `tests/BrutalUi.Core.Tests/Shared/`. Benchmark paths: `bench/` means `tests/BrutalUi.Benchmarks/`.

Each phase ends with `dotnet build brutal-ui.sln 2>&1 | tail -n 30` and `dotnet test --solution brutal-ui.sln 2>&1 | tail -n 30`. Expect `0 Warning(s)`, `0 Error(s)`, and 0 failed. The test project has no implicit usings, so each new file holds its own `using` lines. Use `TestContext.Current.CancellationToken` where an API takes a token. The xUnit analyzers are errors.

---

## Phase 1: The Gate on the 1000-Element Tree (10/10 complete)

**Demo:** the developer runs `dotnet run --project tests/BrutalUi.Core.Tests/BrutalUi.Core.Tests.csproj -- --filter-class '*ThousandElementAllocationTests'` and reads a total of 6 with `failed: 0`.
**Needs:** none

Group A (tests, 3 files):

- [x] ✅ **1.1 Write `Shared/TextMeasure.cs`**
  - Description: an `internal sealed class TextMeasure` in `namespace BrutalUi.Core.Tests;`. The constructor takes `float contentWidth` and `float lineHeight`, and creates the delegate one time (`Callback = Measure;`). Members: `MeasureCallback Callback { get; }`, `int Count { get; private set; }`, `Reset()`. The measure follows the `ClampedText` form of `MeasureStub`, with the content width in place of 60: the line width is the content width for `Undefined`, the width for `Exactly`, and the smaller of both for `AtMost`. The height is the line height times `ceil(contentWidth / max(lineWidth, 1))`, the height for `Exactly`, and not more than the height for `AtMost`. The doc comment states that the callback keeps the cache contract of section 6 of the technical design. The file uses no xUnit type, no `MeasureStub`, and no internal member of `BrutalUi.Core`.
  - File(s): `tests/BrutalUi.Core.Tests/Shared/TextMeasure.cs`
  - Estimate: 5 steps

- [x] ✅ **1.2 Write `Shared/ThousandElementTree.cs`**
  - Description: an `internal sealed class ThousandElementTree` with a `static ThousandElementTree Build()` per the section "The 1000-Element Tree" of `implementation.md`. Properties (`required` with `init`, as the `Scene` classes): `Context`, `Root`, `All` (`Element[]`), `StatValueLeaf`, `GridSlot`, `Tooltip`, `Settings`, `Sidebar`, `SidebarLabel`, `StatName`, `Paragraph`, `LabelText` and `ParagraphText` (the two `TextMeasure` instances), and the constants of the change values (the widths and the viewport sizes). `All.Length` is exactly 1000. The rules of task 1.1 apply.
  - File(s): `tests/BrutalUi.Core.Tests/Shared/ThousandElementTree.cs`
  - Estimate: 10 steps

- [x] ✅ **1.3 Write `ThousandElementAllocationTests.cs` (the scene and the step)**
  - Description: a `public sealed class` with the remarks of the pattern (cite `TreeAllocationTests` and `LayoutAllocationTests`). A private `Scene` holds the tree and `StepCount`. A `static void Step(Scene)` applies the kind of the section "The Change Kinds of the Measured Body", then calls `Context.Update(0.016f)`. The body has no lambda, no boxing, no string, no `foreach` over an interface, and no `Assert`. Kind 7 marks each element of `All` with a `for` loop. Write `static void Run(Scene, int)`.
  - File(s): `tests/BrutalUi.Core.Tests/ThousandElementAllocationTests.cs`
  - Estimate: 8 steps

- [x] ✅ **1.4 Add the gate `[Fact] UpdateAllocatesNothingAfterWarmUp`**
  - Description: build the tree, run one `Update` (the warm-up of A5), then `Run(scene, KindCount * 2)` two times (each kind in both directions, two times, as `LayoutAllocationTests`). Read `GC.GetAllocatedBytesForCurrentThread()`, run 100 steps, read it again, and `Assert.Equal(0L, after - before)`.
  - File(s): `tests/BrutalUi.Core.Tests/ThousandElementAllocationTests.cs`
  - Estimate: 4 steps

- [x] ✅ **1.5 Add `[Fact] TheProbeSeesAnAllocation`**
  - Description: read the counter, allocate a `byte[64]` into a local, call `GC.KeepAlive` on it, read the counter, and assert a difference of 64 or more. It proves that the counter works on this runtime and thread.
  - File(s): `tests/BrutalUi.Core.Tests/ThousandElementAllocationTests.cs`
  - Estimate: 2 steps

- [x] ✅ **1.6 Add `[Fact] TheTreeHoldsTheRequiredMix`**
  - Description: assert `All.Length == 1000`, and walk `All` to count a `Row` direction, a `Column` direction, a `Wrap`, a grow factor above 0, an `Absolute` position, a percent unit in a size field, a `Display.None`, and a measure callback. Assert one or more of each, and 109 callback leaves (or the recorded count of task 1.2).
  - File(s): `tests/BrutalUi.Core.Tests/ThousandElementAllocationTests.cs`
  - Estimate: 4 steps

- [x] ✅ **1.7 Add `[Fact] TheMeasuredBodyChangesEachPart`**
  - Description: after one round, assert the new rect of the value leaf, of grid slot 0, and of the tooltip, a visible settings panel with a width above 0, the moved sidebar item at the last index, and the root width 1024. After the second round, assert each old value. After each round, assert that the root holds no `Layout` and no `DescendantLayout` bit.
  - File(s): `tests/BrutalUi.Core.Tests/ThousandElementAllocationTests.cs`
  - Estimate: 5 steps

- [x] ✅ **1.8 Add `[Fact] TheMeasuredBodyCallsTheTextCallbacks` and `[Fact] TheFullStepVisitsEachVisibleElement`**
  - Description: both `TextMeasure` counts grow over 100 measured steps. After a kind 7 step, each element outside the settings panel has `Layout.Generation` equal to the generation of the root, and each element inside it has generation 0.
  - File(s): `tests/BrutalUi.Core.Tests/ThousandElementAllocationTests.cs`
  - Estimate: 5 steps

Group B (verify, 1 file):

- [x] ✅ **1.9 Run the build, the tests, the demo, and the Linux script**
  - Description: run the build and the tests. Run the demo command and read the totals. Run `scripts/test-linux.sh; echo "exit=$?"` and expect the PASS line and `exit=0`. Run the class 5 times and record the byte result, the run time of the class, and the final section table in `context.md`.
  - File(s): `docs/features/core-tree/allocation-guard/context.md`
  - Estimate: 6 steps

- [x] ✅ **1.10 Prove that the gate blocks**
  - Description: copy `ThousandElementAllocationTests.cs` to the session scratch folder. Add `GC.KeepAlive(new object());` as the first line of `Step`. Run `dotnet test --solution brutal-ui.sln > /dev/null 2>&1; echo "exit=$?"` and expect a non-zero exit. Restore the file from the copy, run the tests again, and expect 0 failed. Record both exit codes in `context.md`.
  - File(s): `tests/BrutalUi.Core.Tests/ThousandElementAllocationTests.cs` (temporary edit, restored), `docs/features/core-tree/allocation-guard/context.md`
  - Estimate: 5 steps

---

## Phase 2: The Layout Benchmark on the Same Tree (4/4 complete)

**Demo:** the developer runs `dotnet run -c Release --project tests/BrutalUi.Benchmarks/BrutalUi.Benchmarks.csproj -- --filter '*TreeLayoutBenchmark*' --job short` and sees a summary table with the rows `FullLayout`, `DirtySubtree`, and `CleanUpdate`, each with `-` or `0 B` in the `Allocated` column.
**Needs:** Phase 1

- [x] ✅ **2.1 Link the shared builders into the benchmark project**
  - Description: edit `tests/BrutalUi.Benchmarks/BrutalUi.Benchmarks.csproj`: add `<Compile Include="../BrutalUi.Core.Tests/Shared/*.cs" LinkBase="Shared" />` in a new item group with an XML comment that names Decision 1. Add no `PackageReference` and no `ProjectReference`.
  - File(s): `tests/BrutalUi.Benchmarks/BrutalUi.Benchmarks.csproj`
  - Estimate: 3 steps

- [x] ✅ **2.2 Write `bench/TreeLayoutBenchmark.cs`**
  - Description: a `public class TreeLayoutBenchmark` (not sealed, as BenchmarkDotNet needs) with `[MemoryDiagnoser]` and an XML doc comment that names the path of each benchmark. `[GlobalSetup]` builds the tree, runs one `Update`, and runs each benchmark body two times. `FullLayout`: mark each element of `All`, then `Update` (the marks are part of the number, Decision 3). `DirtySubtree`: toggle the width of `StatValueLeaf` between its two values, mark it, then `Update`. `CleanUpdate`: `Update` only. Each body leaves the tree clean. Add `using BrutalUi.Core.Tests;`.
  - File(s): `tests/BrutalUi.Benchmarks/TreeLayoutBenchmark.cs`
  - Estimate: 6 steps

- [x] ✅ **2.3 Delete `bench/PlaceholderBenchmark.cs`**
  - Description: delete the placeholder benchmark.
  - File(s): `tests/BrutalUi.Benchmarks/PlaceholderBenchmark.cs`
  - Estimate: 1 step

- [x] ✅ **2.4 Run the build, the tests, and the short benchmark**
  - Description: run the build and the tests. Run the demo command. Print the summary table only (`grep -A 12 '| Method'`). Run `git status --porcelain | grep -c BenchmarkDotNet.Artifacts` and expect 0. Record the short-job table in `context.md` with the note "short job, not the release numbers".
  - File(s): `docs/features/core-tree/allocation-guard/context.md`
  - Estimate: 6 steps

---

## Phase 3: The Percent Chain Budget (4/4 complete)

**Demo:** the developer runs `dotnet run --project tests/BrutalUi.Core.Tests/BrutalUi.Core.Tests.csproj -- --filter-class '*PercentChainBudgetTests'` and reads a total of 2 with `failed: 0`.
**Needs:** Phase 2

- [x] ✅ **3.1 Write `Shared/ChainTree.cs`**
  - Description: an `internal enum ChainShape` (`PercentPadding`, `PercentMaxWidth`, `PlainColumn`) and an `internal sealed class ChainTree` with `static ChainTree Build(ChainShape shape, int depth)`. The chain has a root and `depth` levels below it. `PercentPadding` and `PercentMaxWidth` alternate `Row` and `Column` and set `Padding = LengthEdges.All(Length.Percent(1))` or `MaxWidth = Length.Percent(99)` on each level, as probe P2 and `LayoutCacheTests.LeafMarkInDeepPercentChainStaysInCallBudget`. `PlainColumn` sets nothing (the CR-10 chain). The last level is a text leaf with a `TextMeasure` of content width 60 and line height 16. Properties: `Context` (viewport 800 by 600), `Root`, `Leaf`, `Text`. Constants: `Depth = 20`, `PaddingCallBudget = 4000`, `MaxWidthCallBudget = 7400` (Assumption A8). Each level name is a literal, not a concatenation. The rules of task 1.1 apply.
  - File(s): `tests/BrutalUi.Core.Tests/Shared/ChainTree.cs`
  - Estimate: 6 steps

- [x] ✅ **3.2 Write `Layout/PercentChainBudgetTests.cs`**
  - Description: two `[Fact]` tests, one for each percent shape at `Depth`. Each test runs one `Update`, resets the count, marks the leaf, runs one `Update`, and asserts a count above 0 and not above the budget, with the count and the budget in the message. Each test then calls `LayoutDifferentialTests.AssertMatchesFreshLayout` and `LayoutFixture.AssertNoLayoutBits`. The remarks name CR-1 and the counts of the review (3031 and 5674).
  - File(s): `tests/BrutalUi.Core.Tests/Layout/PercentChainBudgetTests.cs`
  - Estimate: 6 steps

- [x] ✅ **3.3 Write `bench/PercentChainBenchmark.cs`**
  - Description: a `public class` with `[MemoryDiagnoser]`. `[GlobalSetup]` builds both percent chains at `Depth` and runs one `Update` on each. `PaddingChainLeafMark` and `MaxWidthChainLeafMark` each mark the leaf and run `Update`.
  - File(s): `tests/BrutalUi.Benchmarks/PercentChainBenchmark.cs`
  - Estimate: 5 steps

- [x] ✅ **3.4 Run the build, the tests, the demo, and the short benchmark**
  - Description: run the build and the tests. Run the demo. Run `-- --filter '*PercentChainBenchmark*' --job short` and print the summary table. Record the short-job table in `context.md`. Phase 4 prints and records the two call counts.
  - File(s): `docs/features/core-tree/allocation-guard/context.md`
  - Estimate: 6 steps

---

## Phase 4: The Facts Report (4/4 complete)

**Demo:** the developer runs `dotnet run -c Release --project tests/BrutalUi.Benchmarks/BrutalUi.Benchmarks.csproj -- --facts` and reads four lines that start with `facts:`. The last line names the number of levels that pass on a 1 MB stack in a Release build.
**Needs:** Phase 3

- [x] ✅ **4.1 Write `bench/StackProbe.cs`**
  - Description: an `internal static class StackProbe`. `RunChild(int depth, int stackBytes)` builds a `PlainColumn` chain of `depth` levels, runs one `Update` on a `new Thread(start, stackBytes)`, joins the thread, and returns 0. A stack overflow ends the child process with a non-zero code. `FindLimit(int stackBytes)` runs a binary search between 1 (a pass) and 65536 (confirm a fail first). Each probe starts the program again with `--stack-child <depth> <stackBytes>`: use `Environment.ProcessPath`, and when its file name is `dotnet` or `dotnet.exe`, pass the entry assembly path as the first argument. Redirect the standard output and the standard error of the child, and read both to the end before `WaitForExit`, so the overflow trace does not reach the console. Return the largest depth that passes.
  - File(s): `tests/BrutalUi.Benchmarks/StackProbe.cs`
  - Estimate: 7 steps

- [x] ✅ **4.2 Write `bench/FactsReport.cs`**
  - Description: an `internal static class FactsReport` with `int Run()`. It prints exactly four lines, in this order: `facts: tree elements <N>, bytes <B>, bytes per element <B/N>` (the counter of `GC.GetAllocatedBytesForCurrentThread()` around `ThousandElementTree.Build()`), `facts: percent padding chain depth 20: <C> callback calls for one leaf mark (budget 4000)`, `facts: percent max width chain depth 20: <C> callback calls for one leaf mark (budget 7400)`, and `facts: plain column chain on a 1 MB stack: <L> levels pass (<Debug|Release> build)` (from `#if DEBUG`). It returns 1 when a call count is above its budget, else 0. The report runs outside the frame loop, so it can allocate (Assumption A7).
  - File(s): `tests/BrutalUi.Benchmarks/FactsReport.cs`
  - Estimate: 5 steps

- [x] ✅ **4.3 Edit `bench/Program.cs`**
  - Description: when `args[0]` is `--facts`, return `FactsReport.Run()`. When `args[0]` is `--stack-child`, parse the two numbers with `int.Parse(..., CultureInfo.InvariantCulture)` and return `StackProbe.RunChild`. Else run the switcher as today. Add an XML summary to `Program` that lists the three modes.
  - File(s): `tests/BrutalUi.Benchmarks/Program.cs`
  - Estimate: 3 steps

- [x] ✅ **4.4 Run the build, the tests, and the facts report**
  - Description: run the build and the tests. Run the demo two times and record the four lines, the run time, and the difference from the CR-10 numbers (520 levels in Release, 293 in Debug) in `context.md`. Run `-- --facts` in a Debug build one time (`dotnet run --project tests/BrutalUi.Benchmarks/BrutalUi.Benchmarks.csproj -- --facts`) and record the Debug stack line.
  - File(s): `docs/features/core-tree/allocation-guard/context.md`
  - Estimate: 6 steps

---

## Phase 5: The Record (5/5 complete)

**Demo:** the developer opens `CHANGELOG.md` and reads, under `## [Unreleased]`, a table of the 5 benchmarks with the mean and the allocated bytes, the machine line of BenchmarkDotNet, and the four facts lines.
**Needs:** Phase 4

- [x] ✅ **5.1 Run the full benchmark and the facts report**
  - Description: run `dotnet run -c Release --project tests/BrutalUi.Benchmarks/BrutalUi.Benchmarks.csproj -- --filter '*'` (the default job) and `-- --facts`. Save the summary table and the environment lines of BenchmarkDotNet (the OS, the CPU, the .NET runtime). Write them into the section "Performance Considerations" of `context.md` with the date and the commit.
  - File(s): `docs/features/core-tree/allocation-guard/context.md`
  - Estimate: 6 steps

- [x] ✅ **5.2 Write `CHANGELOG.md`**
  - Description: the Keep a Changelog 1.1.0 form: a short header, `## [Unreleased]`, `### Added` (one line for each core-tree feature: the element tree, the flexbox layout engine, and the allocation gate with the benchmarks), and `### Benchmarks` (the table of task 5.1, the machine line, the facts lines, and the command that makes them).
  - File(s): `CHANGELOG.md`
  - Estimate: 4 steps

- [x] ✅ **5.3 Edit `docs/technical-design.md`**
  - Description: section 14: name `ThousandElementAllocationTests` (update only in this epic, draw in the imgui-renderer epic), `TreeLayoutBenchmark`, `PercentChainBenchmark`, the `--facts` mode, and the two commands. Section 6: when the measured Release stack limit differs from 520, change the numbers of the stack paragraph and add "(measured by `--facts`)".
  - File(s): `docs/technical-design.md`
  - Estimate: 5 steps

- [x] ✅ **5.4 Edit `docs/features/core-tree/human-testing.md`**
  - Description: add a `## allocation-guard` section in the form of the sibling sections, with the 5 phase demos, one line for the full benchmark run, and the Windows run.
  - File(s): `docs/features/core-tree/human-testing.md`
  - Estimate: 3 steps

- [x] ✅ **5.5 Run the build, the tests, the Linux script, and the prose lint**
  - Description: run the build, the tests, and `scripts/test-linux.sh; echo "exit=$?"`. Run `python3 /home/aaronstatic/.claude/skills/asd-ste100/scripts/ste-lint.py --fail-over 2.5` on each changed doc and fix each hit. Record the results in `context.md`.
  - File(s): `docs/features/core-tree/allocation-guard/context.md`
  - Estimate: 6 steps

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

## Task Status Legend

- [ ] Not started
- [ ] 🔄 In progress
- [ ] ⏸️ Blocked (waiting on something)
- [x] ✅ Completed
- [x] ❌ Cancelled/Won't do

---

## Progress Tracking

### Completed This Session (2026-10-18)
- Phase 5, tasks 5.1 to 5.5: the full benchmark run, `CHANGELOG.md`, sections 6 and 14 of the technical design, the human-testing lines, and the checks.
- Phase 4, tasks 4.1 to 4.4: `StackProbe`, `FactsReport`, the `--facts` mode, and the two Release runs.
- Phase 1, tasks 1.1 to 1.10: the shared builders, the gate class with 6 tests, the 5 runs, and the proof that the gate blocks.
- Phase 2, tasks 2.1 to 2.4: the linked builders, `TreeLayoutBenchmark`, and the short-job table.
- Phase 3, tasks 3.1 to 3.4: `ChainTree`, `PercentChainBudgetTests` (2 tests), `PercentChainBenchmark`, and the short-job table.
- Cross-phase review: CR-1 to CR-9 fixed in review groups 1 to 5, CR-10 routed to the epic, and four verifier notes fixed in review group 6.

### Discovered New Tasks
- none (CR-10, an engine follow-up, goes to the epic rollup, not to this feature)

### Blocked Items
- none

---

## Notes

### Task Estimation
One step is one tool call of an agent. A typical task costs about six steps (see `.beast-mode/phase-handoff.md` section 1).
- 🟢 Small (up to 6 steps)
- 🟡 Medium (7 to 15 steps)
- 🔴 Large (more than 15 steps, so split the task)

---

*Update this file as tasks are completed. Mark tasks with ✅ immediately when done. Add new tasks as they're discovered.*
