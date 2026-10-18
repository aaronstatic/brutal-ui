# Allocation Guard - Implementation Plan

**Epic:** core-tree (feature 3 of 3)
**Status:** Ready for Review
**Started:** 2026-10-18
**Target Completion:** TBD
**Last Updated:** 2026-10-18 04:34

---

## Quality Bar

**Target quality:** A release gate. The test of this feature is the only guard of the first design pillar: "Layout, input, and draw allocate nothing after warm-up. A test fails when they do." A gate that passes with a hidden allocation is worse than no gate. A benchmark number that measures the wrong path misleads each later epic.

- **The gate is real:** The measured region of the allocation test runs each kind of layout work on the 1000-element tree: a leaf change, a wrap item, an absolute child, a hidden subtree, the text callbacks, a reorder, a new viewport, and a full layout. A second test proves that the body changes the rects and calls the callbacks. A third test proves that the probe API sees an allocation on this runtime. A fourth test proves that a full step visits each visible element.
- **The gate blocks:** An allocation in the measured region fails `dotnet test`, and both test scripts exit non-zero. The Verification Method proves it with a temporary allocation.
- **The numbers are honest:** Each benchmark names the path that it measures. A benchmark body allocates nothing (the `Allocated` column shows `-` or `0 B`). The recorded numbers carry the machine line of BenchmarkDotNet.
- **The hand-offs close:** The percent chain budget (CR-1), the Release stack limit (CR-10), and the memory of the 1000-element tree each have a measured number in `context.md` and in `CHANGELOG.md`.
- **No product change:** `src/` does not change, unless the gate finds an allocation in the engine (see Risks).

---

## Executive Summary

This feature adds two shared test builders, one xUnit test class, one budget test class, two BenchmarkDotNet classes, and a facts report. `ThousandElementTree` builds a game screen of exactly 1000 elements with rows, columns, a wrapped grid, grow children, absolute overlays, percent sizes, a hidden panel, and 109 text leaves. `ThousandElementAllocationTests` warms the tree and measures 0 bytes over 100 rotating steps, in the pattern of `LayoutAllocationTests` (Q1). The builders live in `tests/BrutalUi.Core.Tests/Shared/`, and `BrutalUi.Benchmarks` compiles the same files through a linked `Compile` item (Decision 1). `TreeLayoutBenchmark` measures a full layout, a one-subtree dirty pass, and a clean `Update` on the same tree. It replaces `PlaceholderBenchmark`. `PercentChainBenchmark` and `PercentChainBudgetTests` close the CR-1 hand-off (Q2). A `--facts` mode of the benchmark program prints the tree memory, the callback calls of a leaf mark in the percent chains, and the Release stack limit (Q3). The numbers go into `context.md` and a new `CHANGELOG.md`. No package, no script step, and no `src/` file changes.

---

## User Personas Alignment

No `docs/user-personas.md` file exists. The personas below come from `docs/mission-statement.md` and the epic docs.

**Target Users:**
- [x] **The game developer** (reads the changelog numbers to trust the frame budget, and gets a failed test when a change allocates)
- [x] **The maintainer and the dev agents** (run the gate in both scripts, and run the benchmark on demand before a release)
- [x] **The sibling epic agents** (the imgui-renderer epic adds a `Draw` step to the same builder and test, and the style-system epic adds a style change kind)
- [ ] **The markup author** (not served. The feature has no API)

**User Subsets Served:**
- [x] **Simple** - One command (`dotnet test`) runs the gate. One command (`dotnet run -c Release`) runs the benchmark.
- [x] **Complex** - The facts report gives the deep-tree limits: the call count of a percent chain and the stack depth limit.

**UX Approach:**
- A failed gate prints `Assert.Equal() Failure: Expected 0, Actual <N>` in `artifacts/test-linux/test.log`, and the script exits with the code of `dotnet test`.
- `dotnet run -c Release --project tests/BrutalUi.Benchmarks/BrutalUi.Benchmarks.csproj -- --filter '*'` runs all benchmarks. `-- --facts` prints four `facts:` lines.

---

## Goals

### Primary Goals
1. Ship a deterministic 1000-element tree that the test project and the benchmark project share.
2. Gate the zero-allocation rule of `UiContext.Update` on that tree under `dotnet test` (A5).
3. Measure the full layout pass and the one-subtree dirty pass with BenchmarkDotNet (A6), and remove the placeholder.
4. Close the three flexbox-layout hand-offs: the percent chain budget, the Release stack limit, and the tree memory.
5. Record the numbers in the feature docs and the changelog.

### Success Criteria
- [ ] `ThousandElementAllocationTests` passes with 0 bytes over 100 measured steps, and its 6 tests pass.
- [ ] A temporary allocation in the measured body makes `dotnet test --solution brutal-ui.sln` exit non-zero.
- [ ] `scripts/test-linux.sh` and `scripts/test-windows.ps1` do not change.
- [ ] The benchmark run shows 5 rows (`FullLayout`, `DirtySubtree`, `CleanUpdate`, `PaddingChainLeafMark`, `MaxWidthChainLeafMark`), each with no allocation.
- [ ] `--facts` prints the tree memory, two call counts inside their budgets, and the Release stack limit.
- [ ] `CHANGELOG.md` and `context.md` hold the same numbers.

---

## Architecture Overview

The feature adds test code only. It calls the public API of `BrutalUi.Core`: `Element`, `ElementList`, `ComputedStyle`, `LayoutNode.Measure`, `MarkDirty`, `UiContext.AddRoot`, `ViewportSize`, and `Update`. Only the test classes read the internal `LayoutNode.Generation` (through the existing `InternalsVisibleTo`). The shared builders read no internal member, because the benchmark assembly has no `InternalsVisibleTo`.

### File Tree (end state)

```
CHANGELOG.md                                         # phase 5: new, Keep a Changelog form
docs/technical-design.md                             # phase 5: sections 6 (stack line) and 14 (the gate and the benchmark)
docs/features/core-tree/human-testing.md             # phase 5: the allocation-guard section
docs/features/core-tree/allocation-guard/context.md  # each phase records its results (scaffolded by /start-feature)
tests/BrutalUi.Core.Tests/
├── Shared/                                          # compiled by the test project, linked by the benchmark project
│   ├── TextMeasure.cs                               # phase 1: the counting text callback
│   ├── ThousandElementTree.cs                       # phase 1: the 1000-element game screen
│   └── ChainTree.cs                                 # phase 3: the percent chains and the plain column chain
├── ThousandElementAllocationTests.cs                # phase 1: the gate
└── Layout/PercentChainBudgetTests.cs                # phase 3: the CR-1 budget
tests/BrutalUi.Benchmarks/
├── BrutalUi.Benchmarks.csproj                       # phase 2: links ../BrutalUi.Core.Tests/Shared/*.cs
├── PlaceholderBenchmark.cs                          # phase 2: deleted
├── TreeLayoutBenchmark.cs                           # phase 2
├── PercentChainBenchmark.cs                         # phase 3
├── StackProbe.cs                                    # phase 4
├── FactsReport.cs                                   # phase 4
└── Program.cs                                       # phase 4: the --facts and --stack-child modes
```

### Data Flow

```
Shared/*.cs ──compiled──► BrutalUi.Core.Tests ──► ThousandElementAllocationTests, PercentChainBudgetTests ──► dotnet test (both scripts)
            └─linked────► BrutalUi.Benchmarks ──► TreeLayoutBenchmark, PercentChainBenchmark (BenchmarkDotNet)
                                              └─► --facts ──► FactsReport, StackProbe (child processes)
```

### The 1000-Element Tree

`ThousandElementTree.Build()` returns a sealed class with the context, the root, `All` (an `Element[]` of each element in build order), and the handles of the change kinds. It sets `ViewportSize` to (1280, 720) and adds the root. It runs no `Update`, so the facts report can measure the build alone. Each `Name` is a string literal (no concatenation), so the build allocates no name string. The sections:

| # | Section | Shape | Elements |
|---|---------|-------|----------|
| 1 | root | column, padding 8 | 1 |
| 2 | header | row, height 48, `SpaceBetween`. A title text leaf with grow 1. 6 buttons of 80 by 32, each with a text label leaf. | 14 |
| 3 | body | row, grow 1 | 1 |
| 4 | sidebar | column, width 20%, padding 1%. 50 items: a row (height 24), an icon (16 by 16), and a text label leaf with grow 1. | 151 |
| 5 | grid | row, `Wrap`, grow 1, `AlignContent.SpaceBetween`. 120 slots: a slot (48 by 48, margin 2), an icon (80% by 80%), and an absolute badge (right 2, bottom 2, 12 by 12). | 361 |
| 6 | stats | column, width 25%, max width 320. 40 rows: a row (height 20), a name text leaf with grow 1, a value leaf (width 30%), a bar (40 by 6), and a fill (width 50%, height 100%). | 201 |
| 7 | footer | row, height 64, `Justify.Center`. 10 slots: a slot (56 by 56), an icon (grow 1), and an absolute key label (left 2, top 2, 12 by 12). | 31 |
| 8 | tooltip | absolute, left 40, top 40, width 200, column. 6 text line leaves. | 7 |
| 9 | modal | absolute, width 50%, height 50%, left 25%, top 25%, column. A title row with a title text leaf and a close button, a body column with 3 wrapping paragraph leaves, and a button row with 2 buttons and 2 text labels. | 13 |
| 10 | settings | `Display.None`, column. 73 rows: a row, a label, and a toggle (24 by 12). | 220 |
| | **Total** | | **1000** |

The 109 text leaves use `TextMeasure`: the header title, 6 header button labels, 50 sidebar labels, 40 stat names, 6 tooltip lines, the modal title, 3 modal paragraphs, and 2 modal button labels. The settings labels have no callback. The labels share one `TextMeasure` with a content width of 60. The 3 paragraphs share a second one with a content width of 300. The developer can move one row between sections to reach exactly 1000, and records the final table in `context.md`.

### The Change Kinds of the Measured Body

`ThousandElementAllocationTests.Step` rotates through 8 kinds by the step number, as `LayoutAllocationTests.Step` does. The second round of a kind reverses the first.

| Kind | Change | The path that it runs |
|------|--------|-----------------------|
| 0 Leaf | the width of the value leaf of stat row 0 (30% and 40%), then a mark | a row with a grow sibling |
| 1 Wrap | the width of grid slot 0 (48 and 60), then a mark | the line break and step 8 |
| 2 Absolute | the width of the tooltip (200 and 240), then a mark | step 11 |
| 3 Hidden | `Display` of the settings panel (`Flex` and `None`), then a mark | the zero-out of 220 elements |
| 4 Text | a mark on sidebar label 0, stat name 0, and paragraph 0 | the measure callback |
| 5 Reorder | `Children.Move(0, Count - 1)` on the sidebar, then the reverse | the child list and a parent mark |
| 6 Viewport | `ViewportSize` (1280 by 720 and 1024 by 720) | each percent and the root constraints |
| 7 Full | a mark on each element of `All` | a full layout of the tree |

---

## Implementation Phases

Each phase is a slice: one narrow path that works end to end, with a demo line that a person can check. Section 11 of `.beast-mode/phase-handoff.md` holds the rules. The requirements leave the demo of phase 1 open, so Notes Q4 holds the options, and phase 1 uses the recommended line.

Each phase ends with `dotnet build brutal-ui.sln 2>&1 | tail -n 30` and `dotnet test --solution brutal-ui.sln 2>&1 | tail -n 30` (tool rule 5). Expect `0 Warning(s)`, `0 Error(s)`, and 0 failed. The test project has no implicit usings, so each new file holds its own `using` lines. Use `TestContext.Current.CancellationToken` where an API takes a token. The xUnit analyzers are errors.

### Phase 1: The Gate on the 1000-Element Tree
**Goal:** The smallest path that proves the idea: the shared tree, the text callback, and the xUnit test that measures 0 bytes over 100 warm steps.
**Demo:** the developer runs `dotnet run --project tests/BrutalUi.Core.Tests/BrutalUi.Core.Tests.csproj -- --filter-class '*ThousandElementAllocationTests'` and reads a total of 6 with `failed: 0`.
**Needs:** none
**Agent tier:** ADVANCED (opus)

**Tier note:** This phase is the release gate of the first pillar. The 1000-element tree runs paths that the 56-element tree of `LayoutAllocationTests` does not run (a 220-element zero-out, a reorder, a full layout of 1000 elements). When the gate measures bytes above 0, the cause can be in the engine. Find the allocating kind first: run the measured loop with one kind at a time in a scratch copy of the test. Then fix the cause in `src/BrutalUi.Core/` with no change of the public API, record the blast radius in `context.md`, and keep each existing test green. Do not weaken the gate: no extra warm-up round to hide a growth, and no removed kind.

**Tasks:**

Group A (tests, 3 files):
- [ ] 1.1 Write `tests/BrutalUi.Core.Tests/Shared/TextMeasure.cs`: an `internal sealed class TextMeasure` in `namespace BrutalUi.Core.Tests;`. The constructor takes `float contentWidth` and `float lineHeight`, and creates the delegate one time (`Callback = Measure;`). Members: `MeasureCallback Callback { get; }`, `int Count { get; private set; }`, `Reset()`. The measure follows the `ClampedText` form of `MeasureStub`, with the content width in place of 60: the line width is the content width for `Undefined`, the width for `Exactly`, and the smaller of both for `AtMost`. The height is the line height times `ceil(contentWidth / max(lineWidth, 1))`, the height for `Exactly`, and not more than the height for `AtMost`. The doc comment states that the callback keeps the cache contract of section 6 of the technical design. The file uses no xUnit type, no `MeasureStub`, and no internal member of `BrutalUi.Core`.
- [ ] 1.2 Write `tests/BrutalUi.Core.Tests/Shared/ThousandElementTree.cs`: an `internal sealed class ThousandElementTree` with a `static ThousandElementTree Build()` per the section "The 1000-Element Tree". Properties (`required` with `init`, as the `Scene` classes): `Context`, `Root`, `All` (`Element[]`), `StatValueLeaf`, `GridSlot`, `Tooltip`, `Settings`, `Sidebar`, `SidebarLabel`, `StatName`, `Paragraph`, `LabelText` and `ParagraphText` (the two `TextMeasure` instances), and the constants of the change values (the widths and the viewport sizes). `All.Length` is exactly 1000. The rules of task 1.1 apply.
- [ ] 1.3 Write `tests/BrutalUi.Core.Tests/ThousandElementAllocationTests.cs`: a `public sealed class` with the remarks of the pattern (cite `TreeAllocationTests` and `LayoutAllocationTests`). A private `Scene` holds the tree and `StepCount`. A `static void Step(Scene)` applies the kind of the section "The Change Kinds of the Measured Body", then calls `Context.Update(0.016f)`. The body has no lambda, no boxing, no string, no `foreach` over an interface, and no `Assert`. Kind 7 marks each element of `All` with a `for` loop. Write `static void Run(Scene, int)`.
- [ ] 1.4 Add the gate `[Fact] UpdateAllocatesNothingAfterWarmUp`: build the tree, run one `Update` (the warm-up of A5), then `Run(scene, KindCount * 2)` two times (each kind in both directions, two times, as `LayoutAllocationTests`). Read `GC.GetAllocatedBytesForCurrentThread()`, run 100 steps, read it again, and `Assert.Equal(0L, after - before)`.
- [ ] 1.5 Add `[Fact] TheProbeSeesAnAllocation`: read the counter, allocate a `byte[64]` into a local, call `GC.KeepAlive` on it, read the counter, and assert a difference of 64 or more. It proves that the counter works on this runtime and thread.
- [ ] 1.6 Add `[Fact] TheTreeHoldsTheRequiredMix`: assert `All.Length == 1000`, and walk `All` to count a `Row` direction, a `Column` direction, a `Wrap`, a grow factor above 0, an `Absolute` position, a percent unit in a size field, a `Display.None`, and a measure callback. Assert one or more of each, and 109 callback leaves (or the recorded count of task 1.2).
- [ ] 1.7 Add `[Fact] TheMeasuredBodyChangesEachPart`: after one round, assert the new rect of the value leaf, of grid slot 0, and of the tooltip, a visible settings panel with a width above 0, the moved sidebar item at the last index, and the root width 1024. After the second round, assert each old value. After each round, assert that the root holds no `Layout` and no `DescendantLayout` bit.
- [ ] 1.8 Add `[Fact] TheMeasuredBodyCallsTheTextCallbacks` (both `TextMeasure` counts grow over 100 measured steps) and `[Fact] TheFullStepVisitsEachVisibleElement` (after a kind 7 step, each element outside the settings panel has `Layout.Generation` equal to the generation of the root, and each element inside it has generation 0).

Group B (verify, 1 file):
- [ ] 1.9 Run the build and the tests. Run the demo command and read the totals. Run `scripts/test-linux.sh; echo "exit=$?"` and expect the PASS line and `exit=0`. Run the class 5 times and record the byte result, the run time of the class, and the final section table in `context.md`.
- [ ] 1.10 Prove that the gate blocks. Copy `ThousandElementAllocationTests.cs` to the session scratch folder. Add `GC.KeepAlive(new object());` as the first line of `Step`. Run `dotnet test --solution brutal-ui.sln > /dev/null 2>&1; echo "exit=$?"` and expect a non-zero exit. Restore the file from the copy, run the tests again, and expect 0 failed. Record both exit codes in `context.md`.

**Estimated Steps:** about 60 (10 tasks, about six steps for one task)

**Files (4):** Group A: `Shared/TextMeasure.cs`, `Shared/ThousandElementTree.cs`, `ThousandElementAllocationTests.cs`. Group B: `context.md` (and a temporary edit of the test file, restored).

**Acceptance Criteria:**
- [ ] The 6 tests of `ThousandElementAllocationTests` pass, and the gate reads 0 bytes in 5 runs.
- [ ] The temporary allocation gives a non-zero exit code of `dotnet test`.
- [ ] `scripts/test-linux.sh` prints its PASS line, and `git diff main -- scripts/` is empty.
- [ ] No file of `Shared/` holds `Xunit`, `MeasureStub`, `LayoutFixture`, or `Generation`.

---

### Phase 2: The Layout Benchmark on the Same Tree
**Goal:** Measure the full layout pass, the one-subtree dirty pass, and a clean `Update` on the 1000-element tree. Replace the placeholder.
**Demo:** the developer runs `dotnet run -c Release --project tests/BrutalUi.Benchmarks/BrutalUi.Benchmarks.csproj -- --filter '*TreeLayoutBenchmark*' --job short` and sees a summary table with the rows `FullLayout`, `DirtySubtree`, and `CleanUpdate`, each with `-` or `0 B` in the `Allocated` column.
**Needs:** Phase 1
**Agent tier:** STANDARD (sonnet)

**Tasks:**
- [ ] 2.1 Edit `tests/BrutalUi.Benchmarks/BrutalUi.Benchmarks.csproj`: add `<Compile Include="../BrutalUi.Core.Tests/Shared/*.cs" LinkBase="Shared" />` in a new item group with an XML comment that names Decision 1. Add no `PackageReference` and no `ProjectReference`.
- [ ] 2.2 Write `tests/BrutalUi.Benchmarks/TreeLayoutBenchmark.cs`: a `public class TreeLayoutBenchmark` (not sealed, as BenchmarkDotNet needs) with `[MemoryDiagnoser]` and an XML doc comment that names the path of each benchmark. `[GlobalSetup]` builds the tree, runs one `Update`, and runs each benchmark body two times. `FullLayout`: mark each element of `All`, then `Update` (the marks are part of the number, Decision 3). `DirtySubtree`: toggle the width of `StatValueLeaf` between its two values, mark it, then `Update`. `CleanUpdate`: `Update` only. Each body leaves the tree clean. Add `using BrutalUi.Core.Tests;`.
- [ ] 2.3 Delete `tests/BrutalUi.Benchmarks/PlaceholderBenchmark.cs`.
- [ ] 2.4 Run the build and the tests. Run the demo command. Print the summary table only (`grep -A 12 '| Method'`). Run `git status --porcelain | grep -c BenchmarkDotNet.Artifacts` and expect 0. Record the short-job table in `context.md` with the note "short job, not the release numbers".

**Estimated Steps:** about 24 (4 tasks, about six steps for one task)

**Files (4):** `BrutalUi.Benchmarks.csproj`, `TreeLayoutBenchmark.cs`, `PlaceholderBenchmark.cs` (delete), `context.md`

**Acceptance Criteria:**
- [ ] The benchmark project builds with 0 warnings and compiles the files of `Shared/`.
- [ ] The demo table holds three rows with no allocation.
- [ ] `PlaceholderBenchmark.cs` does not exist.
- [ ] `dotnet test` does not run a benchmark.

---

### Phase 3: The Percent Chain Budget
**Goal:** Close the CR-1 hand-off: a benchmark of one leaf mark in a 20-level percent chain, and a test that holds a budget of callback calls for both chain shapes.
**Demo:** the developer runs `dotnet run --project tests/BrutalUi.Core.Tests/BrutalUi.Core.Tests.csproj -- --filter-class '*PercentChainBudgetTests'` and reads a total of 2 with `failed: 0`.
**Needs:** Phase 2
**Agent tier:** STANDARD (sonnet)

**Tasks:**
- [ ] 3.1 Write `tests/BrutalUi.Core.Tests/Shared/ChainTree.cs`: an `internal enum ChainShape` (`PercentPadding`, `PercentMaxWidth`, `PlainColumn`) and an `internal sealed class ChainTree` with `static ChainTree Build(ChainShape shape, int depth)`. The chain has a root and `depth` levels below it. `PercentPadding` and `PercentMaxWidth` alternate `Row` and `Column` and set `Padding = LengthEdges.All(Length.Percent(1))` or `MaxWidth = Length.Percent(99)` on each level, as probe P2 and `LayoutCacheTests.LeafMarkInDeepPercentChainStaysInCallBudget`. `PlainColumn` sets nothing (the CR-10 chain). The last level is a text leaf with a `TextMeasure` of content width 60 and line height 16. Properties: `Context` (viewport 800 by 600), `Root`, `Leaf`, `Text`. Constants: `Depth = 20`, `PaddingCallBudget = 4000`, `MaxWidthCallBudget = 7400` (Assumption A8). Each level name is a literal, not a concatenation. The rules of task 1.1 apply.
- [ ] 3.2 Write `tests/BrutalUi.Core.Tests/Layout/PercentChainBudgetTests.cs`: two `[Fact]` tests, one for each percent shape at `Depth`. Each test runs one `Update`, resets the count, marks the leaf, runs one `Update`, and asserts a count above 0 and not above the budget, with the count and the budget in the message. Each test then calls `LayoutDifferentialTests.AssertMatchesFreshLayout` and `LayoutFixture.AssertNoLayoutBits`. The remarks name CR-1 and the counts of the review (3031 and 5674).
- [ ] 3.3 Write `tests/BrutalUi.Benchmarks/PercentChainBenchmark.cs`: a `public class` with `[MemoryDiagnoser]`. `[GlobalSetup]` builds both percent chains at `Depth` and runs one `Update` on each. `PaddingChainLeafMark` and `MaxWidthChainLeafMark` each mark the leaf and run `Update`.
- [ ] 3.4 Run the build and the tests. Run the demo. Run `-- --filter '*PercentChainBenchmark*' --job short` and print the summary table. Record the short-job table in `context.md`. Phase 4 prints and records the two call counts.

**Estimated Steps:** about 24 (4 tasks, about six steps for one task)

**Files (4):** `Shared/ChainTree.cs`, `Layout/PercentChainBudgetTests.cs`, `PercentChainBenchmark.cs`, `context.md`

**Acceptance Criteria:**
- [ ] Both budget tests pass, and each matches a fresh layout.
- [ ] The benchmark table holds both chain rows with no allocation.
- [ ] `LayoutCacheTests.cs` does not change.

---

### Phase 4: The Facts Report
**Goal:** Close the CR-10 hand-off and the memory hand-off: a `--facts` mode that prints the tree memory, the chain call counts, and the Release stack limit on a 1 MB stack.
**Demo:** the developer runs `dotnet run -c Release --project tests/BrutalUi.Benchmarks/BrutalUi.Benchmarks.csproj -- --facts` and reads four lines that start with `facts:`. The last line names the number of levels that pass on a 1 MB stack in a Release build.
**Needs:** Phase 3
**Agent tier:** STANDARD (sonnet)

**Tasks:**
- [ ] 4.1 Write `tests/BrutalUi.Benchmarks/StackProbe.cs`: an `internal static class StackProbe`. `RunChild(int depth, int stackBytes)` builds a `PlainColumn` chain of `depth` levels, runs one `Update` on a `new Thread(start, stackBytes)`, joins the thread, and returns 0. A stack overflow ends the child process with a non-zero code. `FindLimit(int stackBytes)` runs a binary search between 1 (a pass) and 65536 (confirm a fail first). Each probe starts the program again with `--stack-child <depth> <stackBytes>`: use `Environment.ProcessPath`, and when its file name is `dotnet` or `dotnet.exe`, pass the entry assembly path as the first argument. Redirect the standard output and the standard error of the child, and read both to the end before `WaitForExit`, so the overflow trace does not reach the console. Return the largest depth that passes.
- [ ] 4.2 Write `tests/BrutalUi.Benchmarks/FactsReport.cs`: an `internal static class FactsReport` with `int Run()`. It prints exactly these four lines, in this order:
  - `facts: tree elements <N>, bytes <B>, bytes per element <B/N>` (the counter of `GC.GetAllocatedBytesForCurrentThread()` around `ThousandElementTree.Build()`)
  - `facts: percent padding chain depth 20: <C> callback calls for one leaf mark (budget 4000)`
  - `facts: percent max width chain depth 20: <C> callback calls for one leaf mark (budget 7400)`
  - `facts: plain column chain on a 1 MB stack: <L> levels pass (<Debug|Release> build)` (from `#if DEBUG`)

  It returns 1 when a call count is above its budget, else 0. The report runs outside the frame loop, so it can allocate (Assumption A7).
- [ ] 4.3 Edit `tests/BrutalUi.Benchmarks/Program.cs`: when `args[0]` is `--facts`, return `FactsReport.Run()`. When `args[0]` is `--stack-child`, parse the two numbers with `int.Parse(..., CultureInfo.InvariantCulture)` and return `StackProbe.RunChild`. Else run the switcher as today. Add an XML summary to `Program` that lists the three modes.
- [ ] 4.4 Run the build and the tests. Run the demo two times and record the four lines, the run time, and the difference from the CR-10 numbers (520 levels in Release, 293 in Debug) in `context.md`. Run `-- --facts` in a Debug build one time (`dotnet run --project tests/BrutalUi.Benchmarks/BrutalUi.Benchmarks.csproj -- --facts`) and record the Debug stack line.

**Estimated Steps:** about 24 (4 tasks, about six steps for one task)

**Files (4):** `StackProbe.cs`, `FactsReport.cs`, `Program.cs`, `context.md`

**Acceptance Criteria:**
- [ ] The demo prints four `facts:` lines and exits with 0.
- [ ] The stack line of a Release run reads `Release build`.
- [ ] No overflow trace reaches the console.

---

### Phase 5: The Record
**Goal:** Run the full benchmark one time, and put the numbers into the feature docs, the changelog, and the technical design.
**Demo:** the developer opens `CHANGELOG.md` and reads, under `## [Unreleased]`, a table of the 5 benchmarks with the mean and the allocated bytes, the machine line of BenchmarkDotNet, and the four facts lines.
**Needs:** Phase 4
**Agent tier:** STANDARD (sonnet)

**Tasks:**
- [ ] 5.1 Run `dotnet run -c Release --project tests/BrutalUi.Benchmarks/BrutalUi.Benchmarks.csproj -- --filter '*'` (the default job) and `-- --facts`. Save the summary table and the environment lines of BenchmarkDotNet (the OS, the CPU, the .NET runtime). Write them into the section "Performance Considerations" of `context.md` with the date and the commit.
- [ ] 5.2 Write `CHANGELOG.md` in the Keep a Changelog 1.1.0 form: a short header, `## [Unreleased]`, `### Added` (one line for each core-tree feature: the element tree, the flexbox layout engine, and the allocation gate with the benchmarks), and `### Benchmarks` (the table of task 5.1, the machine line, the facts lines, and the command that makes them).
- [ ] 5.3 Edit `docs/technical-design.md`. Section 14: name `ThousandElementAllocationTests` (update only in this epic, draw in the imgui-renderer epic), `TreeLayoutBenchmark`, `PercentChainBenchmark`, the `--facts` mode, and the two commands. Section 6: when the measured Release stack limit differs from 520, change the numbers of the stack paragraph and add "(measured by `--facts`)".
- [ ] 5.4 Edit `docs/features/core-tree/human-testing.md`: add a `## allocation-guard` section in the form of the sibling sections, with the 5 phase demos, one line for the full benchmark run, and the Windows run.
- [ ] 5.5 Run the build, the tests, and `scripts/test-linux.sh; echo "exit=$?"`. Run `python3 /home/aaronstatic/.claude/skills/asd-ste100/scripts/ste-lint.py --fail-over 2.5` on each changed doc and fix each hit. Record the results in `context.md`.

**Estimated Steps:** about 30 (5 tasks, about six steps for one task)

**Files (4):** `CHANGELOG.md`, `docs/technical-design.md`, `docs/features/core-tree/human-testing.md`, `context.md`

**Acceptance Criteria:**
- [ ] `CHANGELOG.md` and `context.md` hold the same benchmark table and facts lines.
- [ ] Section 14 names the gate and the benchmark commands. Section 6 matches the measured stack limit.
- [ ] The lint gives no hit above the limit on the changed docs.

---

## Key Technical Decisions

### Assumptions

No interview ran for this draft. The plan makes these assumptions. `/autorun-epic` can confirm or change each one.

- A1. The two projects share the builders through a linked source file (Decision 1). A linked `Compile` item is not a reference, so the benchmark project still references `BrutalUi.Core` and BenchmarkDotNet only.
- A2. The measured region follows the sibling pattern: one warm `Update`, warm-up rounds of each kind, then 100 rotating steps (Q1). The literal A5 form ("one `Update`, one mark, one measured `Update`") is the first part of this form.
- A3. "A full layout pass" means a mark on each element, then one `Update`. The first visit of a dirty element resets its cache, so each element runs the flex steps (Decision 3).
- A4. "A one-subtree dirty pass" means a width change of one stat value leaf with a mark, then one `Update`.
- A5. `CHANGELOG.md` does not exist. This feature creates it at the root in the Keep a Changelog 1.1.0 form, with an `## [Unreleased]` section that starts at the core-tree epic. The dev-ops epic gets no retroactive entry.
- A6. The recorded numbers come from one run on the Linux machine of the author, with the machine line of BenchmarkDotNet. No test asserts a time.
- A7. The zero-allocation rule covers the benchmark bodies, not the setup and not the facts report.
- A8. The call budgets have a margin of about 30 percent: 4000 for the padding chain (the existing budget, 3031 calls) and 7400 for the max width chain (5674 calls).
- A9. `src/` does not change. The exception: the gate finds an allocation in the engine (see Risks).
- A10. `dotnet test` builds Debug, so the gate measures a Debug build. The `Allocated` column of the Release benchmark is the Release check.
- A11. The tree holds exactly 1000 elements. A test asserts the count.
- A12. The text callbacks keep the cache contract. No test of this feature uses `MeasureStub.AvailableWidthBy20`.

### Decision 1: A linked source file shares the builders
**Choice:** The builders live in `tests/BrutalUi.Core.Tests/Shared/` in `namespace BrutalUi.Core.Tests;`. The test project compiles them through its default glob. The benchmark project links them with `<Compile Include="../BrutalUi.Core.Tests/Shared/*.cs" LinkBase="Shared" />`.
**Rationale:** The requirement asks for one tree in the test and in the benchmark. A link keeps the reference rule of the benchmark project and adds no project.
**Alternatives:** (a) A public builder in `BrutalUi.Core`: test code in the shipped package, and new public API with CS1591 docs. (b) A `ProjectReference` from the benchmark project to the test project: the test project is a Microsoft.Testing.Platform executable with xUnit, and the brief forbids the reference. (c) A copy of the builder in each project: two trees that drift apart.
**Constraint:** The shared files read no internal member and use no xUnit type. The benchmark build fails when they do, so the build enforces the rule.

### Decision 2: The measured region rotates through 8 kinds
**Choice:** The sibling pattern of `LayoutAllocationTests` with 8 kinds (Q1).
**Rationale:** CR-5 showed that a single change on a warm tree runs one path and hits the cache for the rest. A gate on one path passes with an allocation in each other path. The full kind (7) makes each visible element run the flex steps in the measured region.
**Alternatives:** The literal single dirty `Update` of A5. It is a subset of this form, so it adds no gate.

### Decision 3: A full layout is a mark on each element
**Choice:** `FullLayout` and kind 7 call `MarkDirty(DirtyFlags.Layout)` on each element of `All`, then `Update`.
**Rationale:** The public API has no "lay out all" call. A mark on each element resets each cache on the first visit, so the pass is a full layout with no allocation. The 1000 marks cost little next to the layout, and the doc comment of the benchmark says that the number holds them.
**Alternatives:** (a) A new tree for each operation: it measures the construction and allocates. (b) A viewport toggle: the fixed-size subtrees hit the cache, so it is not a full pass (kind 6 measures it). (c) `[IterationSetup]` marks: BenchmarkDotNet warns that it is not accurate for short operations.

### Decision 4: The call budget lives in a test
**Choice:** `PercentChainBudgetTests` holds both budgets on the shared chain. The benchmark measures the time of the same chain (Q2).
**Rationale:** A call count is deterministic, so a test can gate it. A time is not deterministic, so it goes to the record only. The existing CR-1 test stays as it is.
**Alternatives:** (a) A budget in the benchmark output only: no gate reads it. (b) A new shape in `LayoutCacheTests`: it edits a file of the finished sibling, and the benchmark cannot share its chain.

### Decision 5: A `--facts` mode in the benchmark program
**Choice:** `Program.Main` dispatches `--facts` and `--stack-child` before the switcher (Q3).
**Rationale:** BenchmarkDotNet has no column for a call count, a memory total, or a stack limit. A custom column needs a column provider for one number each. A stack overflow ends the process, so the limit needs child processes, as the CR-10 probe did. The hand-off asks for the measure in the benchmark project.
**Alternatives:** (a) A scratch probe outside the repository: no one can run it again. (b) Keep the CR-10 numbers: the hand-off stays open.

### Decision 6: An own text callback in place of `MeasureStub`
**Choice:** `Shared/TextMeasure.cs` with the `ClampedText` form and a content width parameter.
**Rationale:** `MeasureStub` uses `Assert` and holds Yoga-derived code with a notice header. The benchmark project needs neither. The `ClampedText` form keeps the cache contract.
**Alternatives:** Link `MeasureStub.cs` too: it pulls xUnit into the benchmark build.

### Decision 7: No script change and no time gate
**Choice:** The gate is the exit code of `dotnet test`, which both scripts already stop on. The benchmark is not in a script (A6).
**Rationale:** The requirements forbid a new step. A time gate on a shared machine gives false failures.
**Alternatives:** A new script step that runs the benchmark: the requirements forbid it, and it adds minutes to each run.

---

## Risks & Mitigation

| Risk | Likelihood | Impact | Mitigation |
|------|------------|--------|------------|
| The gate finds an allocation in the engine on a path that the 56-element tree does not run (the 220-element zero-out, a reorder, a full layout). | Medium | High | Phase 1 is ADVANCED. Find the kind with a one-kind loop in a scratch copy. Fix the cause in `src/` with no public API change, record the blast radius, and run the full suite. |
| A one-time growth (an `ArrayPool` bucket, a JIT tier) shows in the measured region, and the agent hides it with more warm-up. | Low | High | The tier note forbids it. Warm-up is fixed at one `Update` plus two double rounds, as the sibling. |
| BenchmarkDotNet builds a generated project under `bin/`, where `Directory.Build.props` (warnings as errors) and central package management apply. The generated build can fail. | Medium | Medium | Task 2.4 runs the benchmark. On a failure, read the generated build log, and fix it with a project setting in `BrutalUi.Benchmarks.csproj`. As a last resort, use the in-process toolchain (`--inProcess`) and record the reason in `context.md`. |
| A stack overflow on Linux prints a long trace and can write a core dump. | Medium | Low | The parent redirects and drains both streams. A core dump needs `ulimit -c` above 0 or `DOTNET_DbgEnableMiniDump`. Record the behavior in `context.md`. |
| The Debug gate passes and a Release build allocates (an inlining difference). | Low | Medium | The `Allocated` column of the Release benchmark reads 0 for each row (DoD Q3). |
| The test class runs too long (about 17 full layouts of 1000 elements in Debug). | Low | Low | Task 1.9 records the run time. Over 5 s, lower the full kind to one in 16 steps, and record it. |
| A later engine change moves the chain call counts, and the budget test fails. | Medium | Low | Intended: the test is the gate of CR-1. The fix raises the budget only with a note and a new count in `context.md`. |
| The memory number of the tree holds pool arrays and the two `TextMeasure` objects, so it is not "bytes in the elements". | High | Low | The facts line says "bytes" of the build. `context.md` names what the build holds. |

---

## Dependencies

### Internal Dependencies
- `core-tree/flexbox-layout` (done): the layout pass, the cache, `LayoutNode.Measure`, `ViewportSize`, `LayoutDifferentialTests.AssertMatchesFreshLayout`, and `LayoutFixture.AssertNoLayoutBits`.
- `core-tree/element-tree` (done): `Element`, `ElementList.Move`, `MarkDirty`, `UiContext`, and `InternalsVisibleTo("BrutalUi.Core.Tests")`.
- The dev-ops epic: `BrutalUi.Benchmarks`, both scripts, and the `.gitignore` rule for `BenchmarkDotNet.Artifacts/`.

### External Dependencies
- `BenchmarkDotNet` 0.15.8 and `xunit.v3` 4.0.2, both pinned. No new package. No native dependency.

---

## Definition of Done

All criteria below must pass. An independent evaluator runs the Verification Method from `/home/aaronstatic/dev/brutal-ui` on Linux. "MANUAL" marks a criterion that needs the author on Windows.

### Functional Criteria
- [ ] F1. `ThousandElementTree.Build()` gives exactly 1000 elements with rows, columns, a wrap, grow, absolute children, percent sizes, a hidden panel, and text callbacks (`TheTreeHoldsTheRequiredMix`).
- [ ] F2. `UpdateAllocatesNothingAfterWarmUp` measures 0 bytes over 100 steps of the 8 kinds.
- [ ] F3. The measured body changes each part, calls each text callback, and visits each visible element in a full step (three tests).
- [ ] F4. A temporary allocation in `Step` gives a non-zero exit code of `dotnet test`.
- [ ] F5. `PercentChainBudgetTests` holds both budgets, and both chains match a fresh layout.
- [ ] F6. The benchmark run shows `FullLayout`, `DirtySubtree`, `CleanUpdate`, `PaddingChainLeafMark`, and `MaxWidthChainLeafMark`.
- [ ] F7. `--facts` prints four `facts:` lines and exits with 0.

### Quality Criteria
- [ ] Q1. `dotnet build brutal-ui.sln` gives `0 Warning(s)` and `0 Error(s)`.
- [ ] Q2. `dotnet test --solution brutal-ui.sln` gives 0 failed, with 8 more tests than before the feature (566 before).
- [ ] Q3. Each benchmark row shows `-` or `0 B` in the `Allocated` column.
- [ ] Q4. No file of `tests/BrutalUi.Core.Tests/Shared/` holds `Xunit`, `Assert`, `MeasureStub`, `LayoutFixture`, or `Generation`.
- [ ] Q5. The measured body `Step` holds no lambda, no `$"`, no `string`, and no `Assert`.
- [ ] Q6. The changed docs use ASD-STE100 prose. The lint gives no hit above 2.5.
- [ ] Q7. (MANUAL) `scripts\test-windows.ps1` prints its PASS line on Windows.

### Integration Criteria
- [ ] I1. `scripts/test-linux.sh` and `scripts/test-windows.ps1` do not change, and `scripts/test-linux.sh` prints its PASS line.
- [ ] I2. `src/` does not change, or `context.md` names each change and its reason (A9).
- [ ] I3. `Directory.Packages.props` does not change. `BrutalUi.Core.csproj` holds 2 `PackageReference` items. `BrutalUi.Benchmarks.csproj` holds 1 `PackageReference` (BenchmarkDotNet) and 1 `ProjectReference` (`BrutalUi.Core`).
- [ ] I4. `PlaceholderBenchmark.cs` does not exist.
- [ ] I5. `CHANGELOG.md` and the section "Performance Considerations" of `context.md` hold the same table and facts lines. Sections 6 and 14 of `docs/technical-design.md` match them.

### Verification Method

Run steps 1 to 11 from `/home/aaronstatic/dev/brutal-ui` on Linux. Step 12 is MANUAL.

1. `dotnet build brutal-ui.sln 2>&1 | tail -n 30`. Expect `0 Warning(s)` and `0 Error(s)` (Q1).
2. `dotnet test --solution brutal-ui.sln 2>&1 | tail -n 30`. Expect 0 failed and a total of 574 or more for `BrutalUi.Core.Tests` (Q2).
3. `dotnet run --project tests/BrutalUi.Core.Tests/BrutalUi.Core.Tests.csproj -- --filter-class '*ThousandElementAllocationTests' 2>&1 | tail -n 5`. Expect a total of 6 and `failed: 0` (F1 to F3). Read the test names in the file, and match them to F1 to F3.
4. `dotnet run --project tests/BrutalUi.Core.Tests/BrutalUi.Core.Tests.csproj -- --filter-class '*PercentChainBudgetTests' 2>&1 | tail -n 5`. Expect a total of 2 and `failed: 0` (F5).
5. Copy `tests/BrutalUi.Core.Tests/ThousandElementAllocationTests.cs` to the scratch folder. Add `GC.KeepAlive(new object());` as the first line of `Step`. Run `dotnet test --solution brutal-ui.sln > /dev/null 2>&1; echo "exit=$?"`. Expect a value that is not 0 (F4). Restore the file from the copy. Run step 2 again and expect 0 failed.
6. `scripts/test-linux.sh; echo "exit=$?"`. Expect the PASS line and `exit=0`. `git diff main -- scripts/`. Expect no output (I1).
7. `dotnet run -c Release --project tests/BrutalUi.Benchmarks/BrutalUi.Benchmarks.csproj -- --filter '*' --job short 2>&1 | grep -A 12 '| Method'`. Expect the 5 rows of F6, each with `-` or `0 B` under `Allocated` (Q3). `git status --porcelain | grep -c BenchmarkDotNet.Artifacts`. Expect 0.
8. `dotnet run -c Release --project tests/BrutalUi.Benchmarks/BrutalUi.Benchmarks.csproj -- --facts; echo "exit=$?"`. Expect four `facts:` lines, `Release build` in the last one, each call count not above its budget, and `exit=0` (F7).
9. `grep -nE 'Xunit|Assert|MeasureStub|LayoutFixture|Generation' tests/BrutalUi.Core.Tests/Shared/*.cs`. Expect no output (Q4). Read `Step` in `ThousandElementAllocationTests.cs` for Q5.
10. `git diff main --stat -- src/ Directory.Packages.props; grep -c PackageReference src/BrutalUi.Core/BrutalUi.Core.csproj; grep -cE 'PackageReference|ProjectReference' tests/BrutalUi.Benchmarks/BrutalUi.Benchmarks.csproj; test ! -e tests/BrutalUi.Benchmarks/PlaceholderBenchmark.cs && echo ok`. Expect no `src/` change outside the changes that `context.md` names, `2`, `2`, and `ok` (I2 to I4). At plan time, the flexbox-layout changes are not on `main`. Compare against the flexbox-layout commit in place of `main` when it exists.
11. Read `CHANGELOG.md`, the section "Performance Considerations" of `context.md`, and sections 6 and 14 of `docs/technical-design.md`. Expect the same table and facts lines (I5). Run `python3 /home/aaronstatic/.claude/skills/asd-ste100/scripts/ste-lint.py --fail-over 2.5` on each changed doc (Q6).
12. (MANUAL) On Windows, after `git pull --ff-only`, run `.\scripts\test-windows.ps1`. Expect the PASS line and `$LASTEXITCODE` 0 (Q7).

---

## Testing Strategy

### Unit Tests
- `ThousandElementAllocationTests` (6): the gate, the probe self-check, the mix of the tree, the rect changes of each kind, the callback counts, and the full-step visit.
- `PercentChainBudgetTests` (2): the call budget of one leaf mark for each percent chain, with a fresh layout check.

### Allocation Test
- The gate of phase 1 is the allocation test of the epic (A5). The imgui-renderer epic adds a `Draw` call to the same `Step` and the same builder. The style-system epic adds a style change kind.

### Benchmarks
- `TreeLayoutBenchmark` (3) and `PercentChainBenchmark` (2), on demand with `dotnet run -c Release` (A6). Not in a script.
- The `--facts` mode prints the numbers that BenchmarkDotNet cannot give.

### Integration Tests
- `scripts/test-linux.sh` runs the gate through `dotnet test`. The temporary allocation of Verification step 5 proves the exit code.
- (MANUAL) `scripts\test-windows.ps1` on Windows.

### Manual Testing
- The full benchmark run (task 5.1) on the release machine. Each release runs it again and adds the table to the changelog.

---

## Related Documentation

- **Requirements:** `docs/features/core-tree/allocation-guard/requirements.md`
- **Epic:** `docs/features/core-tree/epic-overview.md`, `docs/features/core-tree/epic-requirements.md`
- **Siblings:** `docs/features/core-tree/element-tree/implementation.md` (task 5.4, `TreeAllocationTests`), `docs/features/core-tree/flexbox-layout/context.md` (Gotchas, the follow-up of CR-1), `docs/features/core-tree/flexbox-layout/code-review.md` (CR-1, CR-5, CR-10, CR-13)
- **Design:** `docs/mission-statement.md` ("Quality through measurement"), `docs/technical-design.md` sections 4, 6, 14, and 15
- **Rules:** `AGENTS.md`, `.beast-mode/phase-handoff.md` sections 1 and 11, `.beast-mode/tool-rules.md`
- **Code patterns:** `tests/BrutalUi.Core.Tests/TreeAllocationTests.cs`, `tests/BrutalUi.Core.Tests/Layout/LayoutAllocationTests.cs`, `tests/BrutalUi.Core.Tests/Layout/MeasureStub.cs`, `LayoutCacheTests.LeafMarkInDeepPercentChainStaysInCallBudget`

---

## Notes

### Open Scope Questions

`/autorun-epic` asks these questions. The plan uses the recommended option (the first one) of each.

**Answered on 2026-10-18.** The user chose the recommended option of each question. The plan uses these answers as written.

**Q1. What does the gate measure?**
- **A (recommended):** One warm `Update`, two double rounds of the 8 kinds, then 100 rotating steps. Reason: CR-5 showed that a single change runs one path and hits the cache for the rest.
- B: The literal A5 form: one warm `Update`, one subtree mark, one measured `Update`.
- C: Both forms, as two `[Fact]` tests.

**Q2. Where does the call budget of the percent chains live?**
- **A (recommended):** A new test class with both chain shapes on the shared chain, and a benchmark of the time. Reason: a call count is deterministic, so only a test can gate it.
- B: The benchmark and the facts output only, with the numbers in the docs.
- C: A second shape in `LayoutCacheTests`, with no shared chain.

**Q3. How does the feature measure the Release stack limit?**
- **A (recommended):** A `--facts` mode in the benchmark program with child processes and a binary search. Reason: anyone can run it again before a release, in the project that the hand-off names.
- B: A scratch probe outside the repository, with the numbers in the docs only.
- C: No new measure. Keep the CR-10 numbers (520 levels in Release).

**Q4. What is the demo of phase 1?**
- **A (recommended):** `**Demo:** the developer runs dotnet run --project tests/BrutalUi.Core.Tests/BrutalUi.Core.Tests.csproj -- --filter-class '*ThousandElementAllocationTests' and reads a total of 6 with failed: 0`. Reason: Microsoft.Testing.Platform prints no name of a passed test, so the class filter is the only way to see the class.
- B: `**Demo:** the developer runs scripts/test-linux.sh and sees the PASS line, then breaks the gate with one allocation and sees the script exit non-zero`.

### Conflicts and Gaps

1. **No changelog exists.** The mission statement and the requirements name "the changelog". Phase 5 creates `CHANGELOG.md` (A5).
2. **The section counts.** The table of the tree adds up to 1000, with 109 text leaves. The developer can move one row between sections, and then records the final table in `context.md` and in the mix test.
3. **The base for the diffs.** At plan time, the flexbox-layout changes are not committed on `main`. Verification steps 6 and 10 compare against `main`. Use the flexbox-layout commit when the caller has made it.
4. **The epic overview.** After this feature, `/update-epic core-tree` records the gate and the benchmark commands under Integration & Architecture, and closes the three hand-offs of flexbox-layout.
5. **The Draw step.** Out of scope. The imgui-renderer epic adds a `Draw` call to `Step`, and a `Draw` benchmark to `TreeLayoutBenchmark` or a new class.
6. **The prose lint.** The planner had no shell, so it did not run `ste-lint.py` on this file. Task 5.5 runs it on the changed docs.

---

*This plan will be updated as implementation progresses. See context.md and tasks.md for current status.*
