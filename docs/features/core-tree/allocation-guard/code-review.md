# core-tree/allocation-guard - Code Review

**Reviewed:** 2026-10-18
**Reviewer:** Agent (cross-phase review of phases 1 to 5)
**Rules Applied:** The gate is real, the numbers are honest, the facts report is correct, Decision 1 (the shared files), test quality and determinism, API clarity, the sibling seams, `AGENTS.md`, ASD-STE100. The Vercel React rules do not apply to C#.
**Status:** Resolved - 10 findings, 10 resolved (9 fixed in review groups 1 to 5, CR-10 routed to the epic as an engine hand-off). Verifier `review-verify.md` PASS, and `review-verify2.md` PASS after the verifier notes of review group 6 (2026-10-18).

---

## Summary

| Severity | Count |
|----------|-------|
| HIGH | 0 |
| MEDIUM | 2 |
| LOW | 8 |

The gate is real. No probe found a hidden allocation. All probes ran in a scratch copy of the repository and in a scratch console program. The console program links the files of `Shared/` and references a scratch copy of `src/BrutalUi.Core`. No file of the repository changed.

1. **Hook map.** The scratch engine got a counter at 12 sites. The sites are the pass, a cache hit, a compute, the zero-out, the wrap line break, an absolute child, the measure call, `ElementList.Move`, the cache reset, the align-content step, the shrink step, and the wrap of the cache ring. The map counts the hits of each kind over the 100 measured steps. Each kind reaches the path that its name gives. Kind 1 gives 129 line breaks in 13 steps. Kind 2 gives 26 absolute children. Kind 3 gives 1320 zero-outs, which is 220 for each of the 6 hide steps. Kind 4 gives 5 measure calls for each step. Kind 5 gives 1 move for each step. Kind 6 gives 946 computes for each step, and the grid goes from 10 to 12 lines. Kind 7 gives 1472 computes for each step.
2. **Mutation of the gate.** One build of the scratch copy has a planted `GC.KeepAlive(new object())` at each site. An environment variable selects one site for each run. The gate failed with `exit=2` for each of the 8 kinds of `Step` (288 or 312 bytes). It also failed for the measure callback of `TextMeasure` (102,120 bytes), the zero-out (63,360 bytes), the wrap line break (11,952 bytes), the absolute child (114,552 bytes), the engine measure call, `ElementList.Move`, the align-content step, the shrink step, and the cache ring wrap. With no planted site, the gate passed.
3. **Mutation of the other tests.** A kind 7 with no mark, or with a mark on the root only, fails `TheFullStepVisitsEachVisibleElement`. A kind 4 with no mark fails `TheMeasuredBodyCallsTheTextCallbacks`.
4. **Warm-up.** One step at a time from the build: the first `Update` and step 0 allocate a one-time 128 bytes. Steps 1 to 399 allocate 0 bytes, and 4000 more steps allocate 0 bytes. The two warm-up rounds hide no growth.
5. **Benchmark bodies.** `FullLayout` computes 996 times, calls the label callback 198 times and the paragraph callback 3 times, and visits each of the 780 visible elements in each iteration. `DirtySubtree` computes 9 times, gets 91 cache hits, calls the label callback 1 time, and toggles the value leaf between 94.8 and 126.4 pixels. Its rects equal a fresh layout. `CleanUpdate` makes 1 cache hit. Each body leaves no layout bit on the root. Each chain body gives the same count in each iteration (3031 and 5674).
6. **Stack probe.** In Release, depth 519 passes and depth 520 fails with exit code 134 (SIGABRT) and 2087 lines of trace on the standard error. A pass takes 0.05 s. A fail takes about 1.2 s. The depth 65536 check takes 23 s. `dotnet BrutalUi.Benchmarks.dll --facts` (the `dotnet` host branch) prints the same four lines and exits with 0 in 43 s.
7. **Decision 1.** The scratch benchmark project builds in Release with 0 warnings and no `InternalsVisibleTo`. The grep of Q4 finds no hit. The benchmark project has 1 `PackageReference` and 1 `ProjectReference`. No file of `src/`, `scripts/`, or `Directory.Packages.props` differs from `a8e7846`.

### Known items from the build

1. **The plan counts 566 tests as the solution total, but Verification step 2 names `BrutalUi.Core.Tests`.** Confirmed as a fact. Gotchas of `context.md` records the reading (the solution total is 574, and `BrutalUi.Core.Tests` alone gives 572). Rejected as a finding, because the record already explains the count and the code is correct.
2. **The builder adds `FlexShrink = 1` to the body and the grid.** Confirmed. This is an orchestrator decision, so it is not a finding. The shrink on the body also lets the visible settings panel collapse the body to 0 height. CR-1 reports that effect and keeps the decision.
3. **`ChainTree` names each level `"level"`.** Confirmed. No code reads `Name` as a key, and the literal keeps the build free of name strings. Rejected as a finding.
4. **The Release limit is 519 and the Debug limit is 292.** Confirmed. A scratch child run gives a pass at 519 and a fail at 520 in Release. The CR-10 numbers came from a scratch program with another frame layout. Sections 6 of the technical design and `CHANGELOG.md` give the measured numbers. Rejected as a finding.
5. **`StackProbe` can write core files.** Confirmed, with a correction. The search for 519 has 13 failed children: depth 65536, six halvings down to 1024, and six probes near the limit. The upper bound is 17, because depth 1 must pass. On this machine, `core_pattern` pipes each abort to apport, and each fail costs about 1.2 s. LOW, see CR-6.
6. **The tree build allocates 699,800 bytes.** Confirmed, with a correction. The number holds 2,856 bytes of one-time type set-up of the first build in the process. A second build allocates 696,944 bytes (696.9 bytes for each element). LOW, see CR-7.

---

## Findings

### CR-1: The visible settings panel collapses the body to 0 height in half of the measured steps
**Severity:** MEDIUM
**Rule:** 5. Test quality (and 7. The sibling seams)
**File:** `tests/BrutalUi.Core.Tests/Shared/ThousandElementTree.cs:422-443`
**Problem:** Kind 3 sets the settings panel to `Display.Flex`. The panel is an in-flow column of 73 rows, 876 pixels high, in a root with 704 pixels of inner height. The body has `FlexShrink = 1`, and the panel has shrink 0. So the body shrinks to 0. The probe gives these rects after step 3:

```
settings Flex (8.0,120.0,1264.0,876.0) body (8.0,56.0,1264.0,0.0) grid (252.8,0.0,695.2,0.0) sidebar (0.0,0.0,252.8,25.3)
```

The panel stays visible from step 3 to step 10 of each two rounds. Thus half of the measured steps of kinds 4 to 7 and 0 to 2 run on a body of 0 height, and the panel goes 276 pixels past the viewport. The grid has negative free space, so `AlignContent.SpaceBetween` falls back to flex-start in those steps. The gate still reaches each path (the hook map shows it), so no allocation hides. But the tree is not the game screen of the plan for half of the gate, and the imgui-renderer epic adds its `Draw` call to the same steps. `TheMeasuredBodyChangesEachPart` checks only `Settings.Layout.Rect.Z > 0`, so it does not see the collapse.
```csharp
Element settings = c.Add(root, "settings");
settings.Style.Display = Display.None;
settings.Style.FlexDirection = FlexDirection.Column;
```
**Fix:** Make the settings panel a full-screen overlay: `Position = PositionType.Absolute`, `Left = 0`, `Top = 0`, `Width = Length.Percent(100)`, `Height = Length.Percent(100)`. The count stays 1000, the hidden zero-out stays the same, and the benchmarks do not change (the panel is hidden there). In `TheMeasuredBodyChangesEachPart`, assert that the body keeps a height of 592 while the panel shows. Update the section table of `context.md` and the remarks of the class.
**Effort:** Low

### CR-2: The wrap kind does not move a slot to another line
**Severity:** LOW
**Rule:** 1. The gate is real (the named path)
**File:** `tests/BrutalUi.Core.Tests/Shared/ThousandElementTree.cs:50-54`
**Problem:** The plan names "the line break and step 8" for kind 1. The break test runs (129 hits in 13 steps), but no slot changes its line. At a viewport of 1280, the grid is 695.2 pixels wide. With slot 0 at 60 pixels, line 1 holds 64 + 12 × 52 = 688 pixels, so 13 slots stay on line 1 for both widths. At 1024, the grid is 554.4 pixels wide, and line 1 holds 10 slots for both widths. Only kind 6 changes the line count (10 and 12 lines).
```csharp
public const float SlotWideWidth = 60f;
```
**Fix:** Set `SlotWideWidth` to 100. Then line 1 holds 104 + 12 × 52 = 728 pixels at 1280, which is more than 695.2, so slot 12 moves to line 2. At 1024, line 1 holds 104 + 8 × 52 = 520 pixels and keeps 9 slots in place of 10. In `TheMeasuredBodyChangesEachPart`, assert that the Y of slot 12 changes in round 1 and returns in round 2.
**Effort:** Low

### CR-3: `TheMeasuredBodyChangesEachPart` checks one value for some kinds
**Severity:** LOW
**Rule:** 5. Test quality
**File:** `tests/BrutalUi.Core.Tests/ThousandElementAllocationTests.cs:217-258`
**Problem:** The test proves that each kind changes one rect. For kind 3, it checks the panel and its first row only, not the 220 rects. For kind 6, it checks only the root width, which is the viewport itself. No percent rect is in the test, but the plan names "each percent" for this kind. The probe shows that the sidebar goes from 252.8 to 201.6 pixels, so the engine is correct. The test does not guard it.
```csharp
Assert.True(tree.Settings.Layout.Rect.Z > 0f);
Assert.True(tree.Settings.Children[0].Layout.Rect.Z > 0f);
...
Assert.Equal(ThousandElementTree.ViewportNarrowWidth, tree.Root.Layout.Rect.Z, Tolerance);
```
**Fix:** After round 1, assert that the last element of `All` (a settings toggle) has a width of 24, and that the sidebar width is 20 percent of the body width. After round 2, assert the old sidebar width.
**Effort:** Low

### CR-4: The full kind has no constant, and the visit test depends on its place in the order
**Severity:** LOW
**Rule:** 7. The sibling seams
**File:** `tests/BrutalUi.Core.Tests/ThousandElementAllocationTests.cs:28-38`, `:110-117`, and `:293-295`
**Problem:** The full kind is the `default` branch. The visit test assumes that the last step of two rounds is the full step. The style-system epic adds a style change kind. If it adds kind 8 and sets `KindCount` to 9, the last step of two rounds is the style step. Then `TheFullStepVisitsEachVisibleElement` reads the generation of another pass with no error in the code.
```csharp
// Two rounds end with the full step of the second round. The settings panel is hidden again in that round.
Run(scene, KindCount * 2);
```
**Fix:** Add `private const int FullKind = 7;` and use `case FullKind:`. Make `default` an empty branch or a `Debug.Fail`. In the visit test, run steps until `scene.StepCount % KindCount == FullKind` and the panel is hidden, then run one step. Add a remark that a new kind goes after `FullKind` or that each test finds its kind by the constant.
**Effort:** Low

### CR-5: The stack probe reads each non-zero exit as an overflow and waits with no limit
**Severity:** LOW
**Rule:** 3. The facts report is correct (the exit codes)
**File:** `tests/BrutalUi.Benchmarks/StackProbe.cs:89-126`
**Problem:** `Passes` returns `false` for each non-zero exit code. An overflow gives 134 on Linux and `0xC00000FD` on Windows. An unhandled exception, a failed thread start, or a runtime fault also gives a non-zero code. Then the binary search reads the fault as an overflow, prints a wrong limit, and the report exits with 0. Also, `WaitForExit` has no time limit. A child that hangs (for example, a Windows Error Reporting dialog after the overflow) stops the report with no message.
```csharp
process.WaitForExit();

return process.ExitCode == 0;
```
**Fix:** Keep the standard error text. Return `false` only when the exit code is not 0 and the text starts with `Stack overflow.`. Throw `InvalidOperationException` with the code and the first line of the text for each other non-zero code. Use `WaitForExit(TimeSpan.FromMinutes(1))`, and kill the child and throw on a timeout. Add the Windows `--facts` run to the human tests.
**Effort:** Low

### CR-6: The stack search spends half of its time on depth 65536 and aborts 13 children
**Severity:** LOW
**Rule:** 3. The facts report is correct (the binary search)
**File:** `tests/BrutalUi.Benchmarks/StackProbe.cs:30-31`, `:58-87`, and `:17-18`
**Problem:** The search is correct: the invariant holds, and the result is the largest depth that passes. But the first fail check builds a chain of 65536 levels. `ElementList.Add` checks each ancestor for a cycle, so the build is quadratic. The probe gives 23 s for this child, of 43 s for the full report. A pass at 65536 also returns 65536 as a "limit", which is only a lower bound. The search aborts 13 children for a limit of 519. Each abort runs the core handler (apport on this machine, about 1.2 s), and each can write a core file with `ulimit -c unlimited`. The remarks also say "The layout pass recurses once for each level". Section 6 of the technical design says several frames for each level.
```csharp
public const int MaxDepth = 65536;
...
if (Passes(MaxDepth, stackBytes))
{
    return MaxDepth;
}
```
**Fix:** Use an exponential search. Start at 64, double the depth until a child fails, then halve the range between the last pass and the first fail. Cap the depth at 8192 (16 MB at 2 KB for each level), and print "8192 or more" when the cap passes. For a limit of 519, this gives 7 failed children in place of 13, and about 9 s in place of 43 s. Change the remark to "The layout pass uses several stack frames for each level". Update the run time line of `context.md`.
**Effort:** Low

### CR-7: The memory line holds the one-time type set-up of the first build
**Severity:** LOW
**Rule:** 3. The facts report is correct (the memory line)
**File:** `tests/BrutalUi.Benchmarks/FactsReport.cs:162-170`
**Problem:** The report measures the first `ThousandElementTree.Build()` of the process. That build also runs the one-time set-up of the types that it touches. The probe gives 699,800 bytes for the first build and 696,944 bytes for each later build. The remarks name the pool arrays, the context, and the two text measure objects, but not the one-time part. The difference is small (0.4 percent), but a later epic compares the number with the next release.
```csharp
long before = GC.GetAllocatedBytesForCurrentThread();
ThousandElementTree tree = ThousandElementTree.Build();
```
**Fix:** Build one tree and drop it before the measure. Then measure a second build. Update the facts line in `CHANGELOG.md` and in the section "Performance Considerations" of `context.md` (696944 bytes, 696.9 bytes for each element).
**Effort:** Low

### CR-8: The builder throws `IndexOutOfRangeException` for a tree of more than 1000 elements
**Severity:** LOW
**Rule:** 6. API clarity (the docs match the code)
**File:** `tests/BrutalUi.Core.Tests/Shared/ThousandElementTree.cs:114`, `:142-145`, and `:457-463`
**Problem:** The doc says that `Build` throws `InvalidOperationException` when the tree does not hold 1000 elements. The check runs after the build. A builder change that adds an element writes past the end of the array in `Collector.Make`, so the caller gets `IndexOutOfRangeException` with no message about the count.
```csharp
Element element = new() { Name = name };
All[Count] = element;
```
**Fix:** In `Make`, throw `InvalidOperationException("The tree must hold exactly 1000 elements.")` when `Count` equals `All.Length`. Keep the check after the build for a tree with too few elements.
**Effort:** Low

### CR-9: The full layout benchmark lays out 780 elements, and the docs say 1000
**Severity:** LOW
**Rule:** 2. The numbers are honest
**File:** `tests/BrutalUi.Benchmarks/TreeLayoutBenchmark.cs:88-94`
**Problem:** The settings panel is hidden during the benchmark. `FullLayout` lays out the 780 visible elements and gives the zero-out to the 220 hidden elements. The probe shows 780 elements with the generation of the pass. The remarks, section 14 of `docs/technical-design.md` (line 473), and the record say "the full layout ... on the 1000-element tree". A reader can divide 207 µs by 1000 elements and get a wrong cost for each element.
```csharp
/// The benchmark has three paths. <see cref="FullLayout"/> marks each element of the tree for layout, then runs one
/// <c>Update</c>.
```
**Fix:** Add one sentence to the remarks: "The settings panel is hidden, so the pass lays out 780 elements and sets the 220 elements of the panel to zero." Add the same note to section 14 and to the table text of `CHANGELOG.md`.
**Effort:** Low

### CR-10: One leaf mark in a 20-level percent chain costs 3.7 to 4.9 ms (engine, hand-off)
**Severity:** MEDIUM
**Rule:** 2. The numbers are honest (a cost fault of the engine)
**File:** `src/BrutalUi.Core/Layout/LayoutEngine.cs:157-300` (`LayoutInternal`, the cache reset and the owner-size key)
**Problem:** The new benchmark gives 4.874 ms for `PaddingChainLeafMark` and 3.739 ms for `MaxWidthChainLeafMark`. That is 29 and 22 percent of a 60 Hz frame for one text change. The count grows about 1.7 times for each level. At that rate, a depth of 25 costs about 70 ms (an estimate, not a measurement). The flexbox-layout review resolved CR-1 "with a caveat" and gave the rest to this feature. This feature measures the cost and holds a call budget (4000 and 7400). No item owns a fix after this feature.
**Fix:** Route to an engine follow-up. Do not change the engine in this feature (no allocation is in the path). Record the hand-off in `docs/features/core-tree/epic-overview.md` under the open items. The follow-up can try a cache entry that keeps the result of a measure pass for a later layout pass with the same owner size, and must keep `LayoutDifferentialTests` green. When the follow-up lowers the counts, lower the budgets in `ChainTree` with a note in `context.md`.
**Effort:** High (engine)

### Verifier notes (review-verify)
- V1 (LOW): `ThousandElementAllocationTests.cs:295` had no space after the comma in `Assert.Equal(wrapBefore,wrapSlot...)`. (fixed in review-g6)
- V2 (LOW): `ThousandElementAllocationTests.cs:258` and `:263` held the same assert on `SlotNarrowWidth` with no step between them. The test keeps the first assert and drops the second. (fixed in review-g6)
- V3 (LOW): the comment line `FactsReport.cs:19` had 153 characters. The comment now wraps at 120. (fixed in review-g6)
- V4 (LOW): `StackProbe.Passes` could wait 2 minutes while the comment said one minute for each child. One `Stopwatch` deadline of 1 minute now covers the stream wait and the exit wait, and the comment says so. (fixed in review-g6)
- V5 (LOW, from `review-verify2.md`): the comment line `StackProbe.cs:27` has 154 characters, and the other comment lines wrap at 120. Not fixed (orchestrator): the line is a comment, `.editorconfig` sets no `max_line_length`, and the build does not flag it. A later edit of the file can wrap it.

---

## What's Already Good

- **The gate is real.** The hook map shows that the 100 measured steps reach each path that the plan names. A planted allocation at each of 17 sites fails the gate. Each of the 8 kinds of `Step`, the measure callback, the zero-out, and the wrap line break are among them.
- **The warm-up is honest.** Only step 0 allocates (a one-time 128 bytes). Steps 1 to 399 and 4000 more steps allocate 0 bytes. The two rounds of warm-up hide no growth.
- **The supporting tests prove their names.** The mutations show that `TheFullStepVisitsEachVisibleElement` catches a full kind with no mark or with a root mark only. `TheMeasuredBodyCallsTheTextCallbacks` catches a text kind with no mark. `TheProbeSeesAnAllocation` proves the counter on the runtime.
- **The benchmarks measure their names.** `FullLayout` visits each visible element in each iteration. `DirtySubtree` computes 9 elements and matches a fresh layout. `CleanUpdate` costs one check. Each body leaves the tree clean and does the same work in each iteration.
- **The facts report uses the same steps as the budget test.** `CountLeafMarkCalls` builds the chain, runs one `Update`, resets the count, marks the leaf, and runs one `Update`. It gives 3031 and 5674, the counts of the test and of the review.
- **The process start of the stack probe is correct.** The apphost branch and the `dotnet` host branch both work on Linux. On Windows, `dotnet run` starts the apphost `.exe`, and `dotnet.exe` gets the entry assembly path as its first argument. The parent reads both streams before `WaitForExit`, so no trace reaches the console and no pipe blocks.
- **Decision 1 holds.** The shared files use no xUnit type, no `MeasureStub`, and no internal member. The benchmark build with no `InternalsVisibleTo` proves it. `TextMeasure` is the `ClampedText` form with a parameter, and it keeps the cache contract.
- **Determinism.** No test reads a time. The generation counter is a static, but each test compares values of its own tree. Each `TextMeasure` count is an instance field.
- **The record matches.** `CHANGELOG.md` and the section "Performance Considerations" of `context.md` hold the same table, the same machine line, and the same four facts lines. Section 6 of the technical design gives 292 and 519 levels, and section 14 names the gate, the two benchmarks, and the facts mode.
- **`AGENTS.md`.** .NET 10, C# 14, nullable, file-scoped namespaces, `Brutal.Numerics` types, no native dependency, no new package, and no change of `src/`.

---

## Recommended Fix Order

| Order | Finding | Reason |
|-------|---------|--------|
| 1 | CR-1 | The tree shape of the gate. Fix it before the imgui-renderer epic adds `Draw` to the same steps. |
| 2 | CR-2, CR-3 | They make the wrap kind and the viewport kind prove their paths. They use the tree of CR-1. |
| 3 | CR-4 | The seam for the style-system epic. A small change to the test class. |
| 4 | CR-8 | A guard and a doc in the same builder file as CR-1 and CR-2. |
| 5 | CR-5, CR-6 | The facts report. One file, and the run gets about five times faster. |
| 6 | CR-7, CR-9 | The record. Run `--facts` again after CR-5 to CR-7, and update `CHANGELOG.md`, `context.md`, and section 14 one time. |
| 7 | CR-10 | An engine hand-off for the orchestrator to route. No change in this feature. |

---

## Progress

- [x] CR-1: The visible settings panel collapses the body to 0 height in half of the measured steps (fixed in review-g1)
- [x] CR-2: The wrap kind does not move a slot to another line (fixed in review-g1)
- [x] CR-3: `TheMeasuredBodyChangesEachPart` checks one value for some kinds (fixed in review-g2)
- [x] CR-4: The full kind has no constant, and the visit test depends on its place in the order (fixed in review-g2)
- [x] CR-5: The stack probe reads each non-zero exit as an overflow and waits with no limit (fixed in review-g3)
- [x] CR-6: The stack search spends half of its time on depth 65536 and aborts 13 children (fixed in review-g3)
- [x] CR-7: The memory line holds the one-time type set-up of the first build (fixed in review-g4, the record in review-g5)
- [x] CR-8: The builder throws `IndexOutOfRangeException` for a tree of more than 1000 elements (fixed in review-g1)
- [x] CR-9: The full layout benchmark lays out 780 elements, and the docs say 1000 (fixed in review-g5)
- [x] CR-10: One leaf mark in a 20-level percent chain costs 3.7 to 4.9 ms (engine, hand-off). Not fixed in this feature (out of scope, orchestrator). The plan forbids a `src/` change unless the gate finds an allocation, and no allocation is in the path. The budget test (`PercentChainBudgetTests`) and the benchmark (`PercentChainBenchmark`) now hold the cost. The hand-off goes to the epic rollup: `/update-epic core-tree` records it under "Tech Debt / Findings" of `epic-overview.md` as an engine follow-up (fewer measure passes for each level, or a cache entry that keeps a measure result for a later layout pass with the same owner size, with `LayoutDifferentialTests` green, then lower budgets in `ChainTree`).
