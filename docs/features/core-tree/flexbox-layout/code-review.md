# core-tree/flexbox-layout - Code Review

**Reviewed:** 2026-10-17
**Reviewer:** Agent (cross-phase review of phases 1 to 10)
**Rules Applied:** Engine correctness against Yoga `v3.2.1` and the CSS decisions of `context.md`, the dirty-bit rule and the layout cache, zero allocation after warm-up, API clarity and sections 5 and 6 of the technical design, the sibling seams, `AGENTS.md`, the licence notice, test quality, ASD-STE100. The Vercel React rules do not apply to C#.
**Status:** Resolved - 13 findings, 13 resolved (verifier `review-verify.md` PASS on 2026-10-18). CR-1 is resolved with a caveat: the cost of a nested percent chain halves but still grows about 1.7 times for each level. The allocation-guard benchmark owns the rest.

---

## Summary

| Severity | Count |
|----------|-------|
| HIGH | 1 |
| MEDIUM | 4 |
| LOW | 8 |

The port is close to Yoga, and the cache is correct for each change that the repository test makes. A probe program in the scratch folder (outside the repository) ran about 228,000 compares of an incremental root with a fresh copy. It used 2 to 5 roots in one context and 9 change kinds: a reparent in one tree and across roots, a promote of a child to a root, a demote of a root into a tree, a new root order, a new viewport, a toggle of `Display`, `Position`, or `FlexWrap` with a mark, a new style, and a new callback. It found 0 stale rects except one case: a root with `Display.None` that moves into a tree keeps its old visible rect (CR-2). A scratch copy of the engine with no owner-size reset gave 162 failures in 200 trees, so that rule of phase 9 is necessary.

The probes found four engine faults and one gap in the allocation guard:

1. The cost of one dirty path grows by about 1.5 to 1.7 times for each level in a tree with percent sizes or percent paddings in alternating rows and columns. The owner-size reset multiplies it by 2 to 6 (CR-1).
2. A hidden root that moves into a tree keeps its visible rect (CR-2).
3. A finite tree can give an infinite width, because the grow step divides by a total factor of 0 (CR-3).
4. A `WrapReverse` container mirrors its padding, its border, and the cross margins of its items from the wrong edge (CR-4).
5. The measured region of `LayoutAllocationTests` runs the root and one row only. The other parts of the scene hit the cache (CR-5).

Six other warm paths measure 0 bytes over 100 steps in the probe: a hidden subtree that becomes visible, a wrap-reverse container, a viewport toggle on a percent tree, a reparent, a callback leaf with a mark, and an absolute child in a resized parent.

### Known items from the build

1. **The doc of `LayoutNode.Rect` says "For a root, X and Y are 0".** Confirmed. Probe P7 gives the rect (15, 4, 100, 100) for a root with `Margin.Left = 10`, `Margin.Top = 4`, and `Left = 5`. Section 6 of the technical design states the correct rule. LOW, see CR-8.
2. **The grep of Verification step 6 hits a doc comment in `LayoutEngine.cs`.** Confirmed as a fact: line 1298 holds `measureNodeWithMeasureFunc</c>`, and `Func<` matches it. The hit is not code. Rejected as a finding. A later verifier can add `--exclude` of `///` lines, or accept the one hit.
3. **`Display` and `Overflow` each appear in one fixture file only.** Confirmed. `Display` is only in `DisplayFixtureTests.cs`, and `Overflow` is only in `MeasureModeFixtureTests.cs` (`Overflow.Scroll`). No fixture sets `Overflow.Hidden`. F1 asks for one file or more, so the criterion holds. `BoxModelTests.cs` covers `Hidden` and `Scroll` with 5 hand-written tests. Rejected as a finding, because Yoga `v3.2.1` has few fixtures for these fields.
4. **`LayoutNode` is about 280 bytes.** Confirmed from the phase 9 note (`Unsafe.SizeOf`). `ComputedStyle` is 228 bytes by field count (9 enums of 4 bytes, 2 floats, 11 `Length` values of 8 bytes, 3 `LengthEdges` of 32 bytes). Each element holds about 508 bytes inline. Plan Notes item 12 estimated 600 bytes for each element, so the total is inside the plan. Rejected as a defect. CR-13 gives an optional saving.
5. **`MeasureStub.AvailableWidthBy20` breaks the callback contract.** Confirmed. No cache test and no differential test uses it. `LayoutDifferentialTests.cs:23` names it only in a doc comment. One single-pass test uses it (`BoxModelTests.cs:689`, `MeasureLeafStretchedWidthUsesAvailableWidth`). That use has no second pass, so the cache cannot give a stale size. `MeasureStub.YogaMax` also breaks the contract, and the Yoga ports of `MeasureCacheFixtureTests.cs` need it. Its doc says so. Rejected as a finding.
6. **The engine recurses several frames for each level.** Confirmed. With the Debug build of `BrutalUi.Core`, a plain column chain of 200 levels passes on a 1 MB stack, and 300 levels overflow. On an 8 MB stack, 1000 levels pass and 3000 overflow. Plan Notes item 13 says "about a thousand levels". On a 1 MB stack (the main thread on Windows), the limit is about 250. LOW, see CR-10.
7. **The tool-call stop rule.** Skipped as the brief says. It is not a code item.

---

## Findings

### CR-1: A dirty path in a nested percent tree costs about 1.6 times more for each level
**Severity:** HIGH
**Rule:** 2. The layout cache (the cost bound of phase 9)
**File:** `src/BrutalUi.Core/Layout/LayoutEngine.cs:171-189` and `:281-284`
**Problem:** The plan says that the cache bounds the cost of a deep change: "The cost is about the length of the dirty path times the child count of each level on it." Probe P2 shows exponential growth when the levels hold percents. Each probe tree is one chain with a text leaf. The levels alternate `Row` and `Column`. A mark on the leaf dirties each level.

| Shape of each level | Depth 10 | Depth 15 | Depth 20 | Depth 25 |
|---------------------|----------|----------|----------|----------|
| `Padding = LengthEdges.All(Length.Percent(1))` | 70 calls | 891 calls, 7.7 ms | 5951 calls, 68 ms | 98488 calls, 834 ms |
| `MaxWidth = Length.Percent(99)` | 231 calls (depth 10) | 4179 calls (depth 16), 20 ms | 28655 calls, 159 ms | not run |
| pixel padding, no percent | 1 call | 1 call | 1 call | 1 call |

The leaf mark pass repeats the full count, so each text change in such a tree costs the same. One cause is the owner-size reset. It clears all entries of the element when the owner size changes, also between two visits in one pass (a measurement with one inner size, then a layout with another inner size):

```csharp
bool needToVisitNode = (isDirty && layout.Generation != generation)
    || layout.Generation == 0
    || (cache.UsesOwnerSize
        && (!LayoutCache.IsSameSize(cache.LastOwnerWidth, ownerWidth)
            || !LayoutCache.IsSameSize(cache.LastOwnerHeight, ownerHeight)));
```

A scratch copy with the reset turned off gives 2542 calls in place of 5951 (percent padding, depth 20), and 5013 in place of 28655 (percent maximum, depth 20). Thus the reset multiplies the cost by 2 to 6. The rest of the growth comes from the cache shape (8 measurement entries, an exact key) and is in the Yoga form too. The reset is necessary for correctness (the same scratch copy gives 162 stale trees in 200), so the fix must keep the rule and change its form. No test measures the cost, so a regression shows only as a slow frame.
**Fix:**
1. Put the owner size into the key of each entry. Add `OwnerWidth` and `OwnerHeight` to `CachedMeasurement`. Store them in each entry. When `UsesOwnerSize` is true, a hit needs the same owner size. Remove the owner test from `needToVisitNode`. Then entries for two owner sizes live together, and a second visit in the same pass hits.
2. Run probe P2 again (the shapes above at depths 10 to 25) and the differential test. Record the call counts in `context.md`.
3. Hand the remaining growth to the allocation-guard feature: add a benchmark with a 20-level percent chain and a budget of callback calls for a leaf mark.
**Effort:** Medium
**Resolved:** `CachedMeasurement` holds `OwnerWidth` and `OwnerHeight`, a hit needs the same owner size when `UsesOwnerSize` is true, and `needToVisitNode` has no owner test. Probe P2 at depth 20 gives 3031 calls in place of 5951 (percent padding) and 5674 in place of 28655 (percent maximum). The test `LeafMarkInDeepPercentChainStaysInCallBudget` holds a budget of 4000. Step 3 goes to the allocation-guard feature (`context.md`, Follow-up).

### CR-2: A hidden root that moves into a tree keeps its visible rect
**Severity:** MEDIUM
**Rule:** 1. Engine correctness (Decision 9), 2. No stale rect
**File:** `src/BrutalUi.Core/Layout/LayoutEngine.Lines.cs:137-147`
**Problem:** Decision 9 zeroes a hidden child only when the child holds a layout bit:

```csharp
if (childStyle.Display == Display.None)
{
    if ((child.Dirty & (DirtyFlags.Layout | DirtyFlags.DescendantLayout)) != DirtyFlags.None)
    {
        ZeroOutLayoutRecursively(child);
    }
```

The rule assumes that a clean hidden child got its zero-out in an earlier pass. A root breaks the assumption. `Display` "has no effect on a root element", so a root with `Display.None` gets a visible layout, and the clear walk removes its bits. `RemoveRoot` and `Children.Add` then mark the new parent only. The hidden element is clean, so the parent skips the zero-out. Probe P1b: a panel with `Display.None` as a root gets (0, 0, 300, 200). After the move into a tree, it keeps (0, 0, 300, 200), and its child keeps (0, 0, 300, 20). A fresh layout gives zeros. The random probe found the same fault in each failed demote step (419 trees over 4 runs). A docked window or a hidden panel that the framework moves from a root into a tree gives this case. The input router and the debugger read the stale rect.
**Fix:** Zero out a hidden child also when it holds a visible layout. `ZeroOutLayoutRecursively` sets `Generation = 0`, and a hidden child gets no visit, so `Generation != 0` on a hidden child means a stale visible layout:

```csharp
bool isDirty = (child.Dirty & (DirtyFlags.Layout | DirtyFlags.DescendantLayout)) != DirtyFlags.None;
if (isDirty || child.Layout.Generation != 0)
{
    ZeroOutLayoutRecursively(child);
}
```

Update the remark of `ZeroOutLayoutRecursively` and Decision 9 in `context.md`. Add a `LayoutPassTests` test: a root with `Display.None`, one `Update`, `RemoveRoot`, `Children.Add` into a second root, one `Update`, then zeros on the moved subtree and a match with a fresh layout.
**Effort:** Low
**Resolved:** `ComputeFlexBasisForChildren` zeroes a hidden child when it holds a layout bit or a generation that is not 0. The remark of `ZeroOutLayoutRecursively` and Decision 9 say so. Test: `LayoutPassTests.HiddenRootThatMovesIntoTreeGetsZeros`.

### CR-3: The grow step can divide by a total factor of 0 and give an infinite width
**Severity:** MEDIUM
**Rule:** 1. Engine correctness (a wrong rect with no error)
**File:** `src/BrutalUi.Core/Layout/LayoutEngine.Lines.cs:589-602` (the second pass) and `:762-787` (the first pass)
**Problem:** The first pass removes the factor of each item that its minimum or maximum freezes. When it freezes each growing item, `TotalFlexGrowFactors` becomes 0, but `RemainingFreeSpace` can stay positive. The second pass then divides by 0:

```csharp
updatedMainSize = LayoutMath.BoundAxis(
    in childStyle,
    mainAxis,
    childFlexBasis + (flexLine.RemainingFreeSpace / flexLine.TotalFlexGrowFactors * flexGrowFactor),
```

An item with no maximum gets an infinite width, and each later sibling gets an infinite X. The shrink branch at line 570 has a guard for a total of 0. The grow branch has none. Yoga `v3.2.1` has the same unguarded division (`CalculateLayout.cpp:689`), so the fault is inherited. The random probe found it one time in about 228,000 root compares: `dotnet out/ReviewProbe.dll diff 90206 1` (scratch folder `review-probe`), change 16. A row with an inner width of 185.4 and an owner width of about 120.2 holds item A (`Width = 194`, `MinWidth = 75%`, `FlexBasis = 10`, `FlexGrow = 0.5`, `FlexShrink = 0`, `Margin.Left = 1%`) and item B (`Width = 134`, `MaxWidth = 37`, `FlexGrow = 1`, `FlexShrink = 0`, `Margin.Left = Auto`). The basis of A clamps to 75% of the owner width (90.2). The first pass bounds A with 75% of the inner width (139.1), so A freezes at its minimum. B freezes at its maximum. The total goes to 0, and 7.5 pixels of free space stay. A gets the width infinity, and B gets X infinity. The fresh layout gives the same values, so the cache is not the cause.

Also in this file: the doc of `FlexLine.TotalFlexShrinkScaledFactors` (lines 1063-1066) says "The value is 0 or less. A value of more than 0 and less than 1 gets a floor of 1." The two sentences disagree. A negative shrink factor gives a positive value.
**Fix:** In the second pass, add the guard of the shrink branch to the grow branch. When `TotalFlexGrowFactors` is 0, use `BoundAxis` of the flex basis, so a frozen item gets its bound size. Add the same guard to the first pass. Add a `BoxModelTests` test from the tree above with finite expected rects. Record the difference from Yoga in `context.md` and in section 6 of the technical design. Fix the doc of `TotalFlexShrinkScaledFactors`: "The value is 0 or less for factors of 0 or more."
**Effort:** Low
**Resolved:** The second pass gives a growing item `BoundAxis` of its flex basis when the total grow factor is 0, and the first pass freezes no item at a total of 0. A total under 1e-5 of the original total also becomes 0 after the first pass, because the float subtractions leave a residual. The doc of `TotalFlexShrinkScaledFactors` is fixed. Tests: `BoxModelTests.GrowWithEachItemFrozenGivesFiniteRects` and `BoxModelTests.GrowWithEachItemFrozenAndRoundingResidualGivesBoundBasis`.

### CR-4: A wrap-reverse container mirrors its padding and the cross margins from the wrong edge
**Severity:** MEDIUM
**Rule:** 1. Engine correctness (the step 8 decision "Each case puts the margin box of the item in the line, as CSS does")
**File:** `src/BrutalUi.Core/Layout/LayoutEngine.cs:1158-1186` (the mirror) and `:880` (the start of `currentLead`)
**Problem:** Step 8 puts the lines from the leading padding and border of the cross axis, and it puts the leading margin before each item. Then the mirror reflects the border box against the full cross size:

```csharp
position = crossDim
    - (position - relativePosition)
    - LayoutMath.MeasuredDimension(in childLayout, crossAxis)
    + relativePosition;
```

After the mirror, the gap at the bottom of a `WrapReverse` row is the top padding and the top margin. CSS puts the bottom padding and the bottom margin there. Probe P6: a row of 100 by 100 with one item of 20 by 20 and `AlignItems = FlexStart`.

| Case | Engine Y | CSS Y |
|------|----------|-------|
| no padding, no margin | 80 | 80 |
| `Padding.Top = 10` | 70 | 80 |
| `Padding.Bottom = 10` | 80 | 70 |
| item `Margin.Top = 5` | 75 | 80 |
| item `Margin.Bottom = 5` | 80 | 75 |

Yoga `v3.2.1` has the same formula (`CalculateLayout.cpp:2077-2087`). No Yoga fixture sets a padding, a border, or a margin with `wrap-reverse` (a search of `tests/generated/` finds none), so no fixture shows it. The CSS rule is clear, and the run rule for step 8 ("the engine follows CSS") applies. A chat log or a stack of notifications from the bottom uses this layout.
**Fix:** Mirror the margin box inside the content box. For each item, with `leadPB` and `trailPB` the leading and trailing padding and border of the container on the cross axis, and `mStart` and `mEnd` the leading and trailing cross margins of the item:

```csharp
float offset = position - relativePosition - leadPB - mStart; // margin-box offset from the content start
position = crossDim - trailPB - offset - mEnd - childCrossSize + relativePosition;
```

The formula gives the CSS column of the table. Add the five cases as `LayoutPassTests` tests. Add the case to the list of CSS differences in section 6 of the technical design and in `context.md`. Run the `p7check` self-check before and after, as phase 7 did.
**Effort:** Low
**Resolved:** The wrap-reverse loop after step 9 mirrors the margin box inside the content box with the formula above, and it keeps the physical relative offset. `p7check` gives the same output before and after. Tests: `LayoutPassTests.WrapReverseRowPutsItemInsideBottomPaddingAndMargin` (the five cases of the table), `LayoutPassTests.WrapReverseColumnPutsItemInsideRightPaddingAndMargin`, `LayoutPassTests.WrapReverseRowMirrorsMarginBoxForEachAlignment` (four cases), and `LayoutPassTests.LinesOfWrapReverseRowWithPaddingAndOffsetStartInsideBottomPadding`.

### CR-5: The measured region of the allocation test runs one row and the cache hits only
**Severity:** MEDIUM
**Rule:** 3. Zero allocation after warm-up, 8. Test quality
**File:** `tests/BrutalUi.Core.Tests/Layout/LayoutAllocationTests.cs:11-14`, `:183-189`, and `:239-248`
**Problem:** The class doc says that the tree holds a row, a column, a wrapped row, an absolute child, percent sizes, a hidden element, and two callback leaves. The measured step changes one leaf of the first row and marks it. Probe P8 copies the scene and reads `LayoutNode.Generation` before and after one measured step:

```
root, row, grow, leaf, percent, stub-leaf: visited
column, wrap, absolute, section: visited (one cache hit each)
column-item 0 of 8, wrap-item 0 of 12, absolute-item 0 of 2, hidden 0 of 1, cell 0 of 12, cell-content 0 of 12
callbacks in the step: first stub 0, second stub 0
```

Thus the measured region never runs step 8, step 11, the zero-out, the percent basis of the section, or a measure callback. The test `TheTreeHoldsTheStubsAndTheHiddenElement` checks the counts after the build, not in the measured region. A future allocation in these paths passes the test. Probe P5 measured 0 bytes on these paths, so the engine is clean today.
**Fix:** Make the measured body rotate through changes that dirty each part: the leaf width (now), a width of one wrap item, a toggle of `Display` on the hidden element (with a mark), a size of the absolute element, a mark on each stub leaf, and a viewport toggle between two sizes. Assert in a separate test that each stub count grows over the measured steps. Keep the test free of a lambda and an `Assert` in the measured body.
**Effort:** Low
**Resolved:** `Step` in `LayoutAllocationTests` rotates through six kinds by the step number (the leaf width, the width of wrap item 0, `Display` of the hidden element with a mark, the size of the absolute element, a mark on both stub leaves, and the viewport width 800/700). The second round of each kind reverses the first. The root has no fixed size now, so the viewport toggle reaches the layout. Warm-up runs two rounds of both directions, two times. The measured body has no lambda, string, boxing, or `Assert`. New tests: `TheMeasuredBodyCallsEachStub` (both stub counts grow over the measured steps) and `TheHiddenElementShowsAndHidesItsChildren`. `TheMeasuredBodyChangesARect` now checks every kind. Results: 0 bytes over 100 measured steps in 5 runs of the class (4 tests each, all passed). Full suite: 556 passed, 0 failed, 0 warnings.

### CR-6: The differential test makes no reparent of a laid-out subtree and uses one root
**Severity:** LOW
**Rule:** 8. Test quality (gaps in the coverage)
**File:** `tests/BrutalUi.Core.Tests/Layout/LayoutDifferentialTests.cs:664-736`
**Problem:** The change step adds new elements, removes elements, and moves a child inside one parent. It never moves a laid-out subtree with a warm cache to another parent, and each context holds one root. The probe added these change kinds and found CR-2 in its first run. The demote case and the cross-root reparent are the paths of the framework layer (window docking, list recycling).
**Fix:** Add change kinds to `ApplyRandomChange`: a reparent of an existing subtree to an element outside its subtree, a promote of a child to a second root, and a demote of a root into the tree. Let `CheckStep` compare each root of the context. Keep the seed and the tree count. Run the mutation check of phase 9 group 3 again.
**Effort:** Low
**Resolved:** `ApplyRandomChange` adds three change kinds: a reparent of a laid-out subtree to an element outside the subtree (also into the tree of another root), a promote of a child to a new root (one in three also hides the root), and a demote of a root into the tree of another root. A context holds up to 4 roots, and `CheckStep` compares each root (6534 root compares in 4200 steps: 174 reparents, 260 promotes, 76 demotes). The seed, the 200 trees, and the five kinds stay. Mutation check in the refreshed scratch copy: each mutation fails the test. Mutation 1 fails 14 trees, 2 fails 6, 3 fails 111, 4 fails 12, 5 fails 11, and the CR-2 revert fails 16 (each at a demote of a hidden root). Run time: 0.70 s, 0.71 s, and 0.73 s in 3 runs.

### CR-7: The style docs do not state the valid range of the grow and shrink factors
**Severity:** LOW
**Rule:** 4. API clarity (each style field doc states its default and its units)
**File:** `src/BrutalUi.Core/Tree/ComputedStyle.cs:71-87` (also `src/BrutalUi.Core/Layout/MeasureCallback.cs:15-19`)
**Problem:** Probe P3 sets each float input to `NaN`, an infinity, a negative value, and `float.MaxValue`. `NaN` acts as 0, which is correct. Other values give wrong rects with no error. In a row of 100 with item A (width 20) and item B (width 20, `FlexGrow = 1`):

```
FlexGrow=∞ on A:  a=(0,0,0,50)  b=(0,0,20,50)
FlexGrow=-1 on A: a=(0,0,0,50)  b=(0,0,100,50)
FlexShrink=-1 on A (widths 80): a=(0,0,80,50) b=(80,0,80,50)
```

CSS rejects a negative factor. Yoga gives the same results. The docs of `FlexGrow` and `FlexShrink` give no range. The doc of `MeasureCallback` says that `NaN` and a negative result count as 0, but a callback result of infinity gives an infinite rect. A `Length.Px(float.MaxValue)` width and a viewport of `float.MaxValue` with a child of 200% also give infinite rects. The `Length` factories are correct: `Px`, `Percent`, the implicit conversion, and `LengthEdges.All` give `Undefined` for `NaN` and each infinity, and `-0` equals `0` with the same hash.
**Fix:** In `ResolveFlexGrow` and `ResolveFlexShrink` (`LayoutEngine.Lines.cs:17-45`), give 0 for a value that is negative or not finite. State the rule in the docs of `FlexGrow` and `FlexShrink`: "A negative, infinite, or `NaN` value counts as 0." In the doc of `MeasureCallback`, add "An infinite component gives an infinite size. Return a finite size." Add one `BoxModelTests` test for each factor.
**Effort:** Low
**Resolved:** `ResolveFlexGrow` and `ResolveFlexShrink` give 0 for a negative, infinite, or `NaN` value; the docs of `FlexGrow`, `FlexShrink`, and `MeasureCallback` state the rules; the tests are `InvalidGrowFactorCountsAsZero` and `InvalidShrinkFactorCountsAsZero`. The same group applied the residual rule to the shrink total (`ShrinkWithEachItemFrozenAndRoundingResidualGivesBoundBasis`).

### CR-8: The doc of `LayoutNode.Rect` says that X and Y of a root are 0
**Severity:** LOW
**Rule:** 4. API clarity (the docs match the code)
**File:** `src/BrutalUi.Core/Tree/LayoutNode.cs:80-85`
**Problem:**

```csharp
/// and W is the height. All values are in pixels. For a root, X and Y are 0.
```

`Calculate` writes the left and top margin plus the relative offsets of the root (`LayoutEngine.cs:127-131`). Probe P7 gives X 15 and Y 4. Section 6 of the technical design says "The X and Y of a root hold its margin and its offsets", so the two docs disagree. The renderer epic reads this doc.
**Fix:** Replace the sentence with "For a root, X and Y hold its left and top margin plus its offsets, resolved against `UiContext.ViewportSize`."
**Effort:** Low
**Resolved:** The remarks of `LayoutNode.Rect` now say that X and Y of a root hold its left and top margin plus its offsets, resolved against `UiContext.ViewportSize`. The other docs of `LayoutNode.cs` match the code (the cache holds one layout entry and 8 measurement entries).

### CR-9: The remarks of `UiContext.Update` still describe the pass before the cache
**Severity:** LOW
**Rule:** 4. API clarity (the docs match the code)
**File:** `src/BrutalUi.Core/Tree/UiContext.cs:128-131`
**Problem:**

```csharp
/// Until the layout cache comes, the layout pass lays out each root in full on each call.
/// Thus a new <see cref="ViewportSize"/> takes effect on the next call, with no dirty bit.
```

Phase 9 added the cache. A clean root with the same viewport now costs one check, and a new viewport takes effect through the cache key of the root.
**Fix:** Replace the paragraph with "The layout pass enters only the dirty elements and the elements with new constraints. A clean root with the same viewport costs one cache check. A new `ViewportSize` takes effect on the next call with no dirty bit, because the root sees new constraints."
**Effort:** Low
**Resolved:** The remarks of `UiContext.Update` now describe the layout cache: the pass enters only dirty elements and elements with new constraints, and a new `ViewportSize` takes effect through the new constraints of the root.

### CR-10: The plan overstates the safe tree depth
**Severity:** LOW
**Rule:** 4. Docs match the code, 5. The sibling seams (allocation-guard)
**File:** `docs/technical-design.md` section 6 (the plan Notes item 13 holds the same claim)
**Problem:** Notes item 13 says that a tree deeper than "about a thousand levels" can overflow the stack. Probe P2 (Debug build) overflows a 1 MB stack between 200 and 300 levels of a plain column chain. The main thread on Windows has a 1 MB stack by default. A Release build uses smaller frames, so its limit is higher, but no test measures it. A stack overflow ends the process with no exception that code can catch. Section 6 of the technical design has no statement of the limit.
**Fix:** Add one line to section 6: "The engine recurses about 4 KB of stack for each level in a Debug build. Keep the tree under 200 levels on a 1 MB stack." Ask the allocation-guard feature to measure the Release limit and record it. Correct Notes item 13.
**Effort:** Low

The plan Notes item 13 stays as written. Section 6 of the technical design holds the measured limit.

**Resolved:** A column chain on a 1 MB stack passes 293 levels in a Debug build and 520 levels in a Release build. On an 8 MB stack, it passes 2378 levels (Debug) and 4219 levels (Release). Section 6 of `docs/technical-design.md` now holds the line "Keep the tree under 200 levels on a 1 MB stack" with these numbers, and its cache sentence names the owner size key. The allocation-guard feature measures the Release limit again in its benchmark.

### CR-11: `MeasureStub.cs` copies Yoga test functions with no notice header
**Severity:** LOW
**Rule:** 7. The licence notice
**File:** `THIRD-PARTY-NOTICES.md:21-52` (also `tests/BrutalUi.Core.Tests/Layout/MeasureStub.cs:1`)
**Problem:** `MeasureStub.cs` holds direct copies of Yoga test functions: `_measureMax`, `_measureMin`, `_measure_84_49` (`YGMeasureCacheTest.cpp`), `_measure` (`YGMeasureModeTest.cpp`), `_simulate_wrapping_text`, `_measure_assert_negative`, `_measure_90_10`, and `_measure_100_100` (`YGMeasureTest.cpp`). Its doc names them as Yoga functions. The file has no Yoga header, and the list of derived files does not name it. The notice also holds a process sentence that does not belong in a shipped notice: "Each phase of the flexbox-layout feature updates this list."
**Fix:** Add the two-line Yoga header to `MeasureStub.cs` with the three test files. Add the file to the list. Remove the process sentence.
**Effort:** Low

**Resolved:** `MeasureStub.cs` starts with the two-line Yoga header (v3.2.1, `YGMeasureTest.cpp`, `YGMeasureCacheTest.cpp`, `YGMeasureModeTest.cpp`). `THIRD-PARTY-NOTICES.md` lists the file and holds no process sentence.

### CR-12: The remarks of `DisplayFixtureTests` name a skip that no longer applies
**Severity:** LOW
**Rule:** 8. Test quality (each doc matches the test)
**File:** `tests/BrutalUi.Core.Tests/Layout/DisplayFixtureTests.cs:11-15`
**Problem:** The remarks say "The ports skip the fixtures with `display: contents` and with a percent value." Phase 6 group 2 ported `display_none_with_child`, which holds `flex-basis: 0%`. The ledger shows 6 ports and 8 skips for `display: contents` only.
**Fix:** Change the sentence to "The ports skip the fixtures with `display: contents`."
**Effort:** Low

**Resolved:** The remarks of `DisplayFixtureTests` say "The ports skip the fixtures with `display: contents`." The remarks of nine other `*FixtureTests` classes (`AlignItems`, `AlignSelf`, `Border`, `Dimension`, `FlexDirection`, `Flex`, `JustifyContent`, `MinMaxDimension`, `Padding`) now say "The ports of this file were written before the engine had a position type" in place of "The engine has no position type until phase 5". The `JustifyContent` remarks say "The ports added later" in place of "The ports of phase 6 and later".

### CR-13: The layout enums use 4 bytes each
**Severity:** LOW
**Rule:** 5. The sibling seams (allocation-guard measures the memory of a 1000-element tree)
**File:** `src/BrutalUi.Core/Layout/LayoutEnums.cs:10-227`
**Problem:** The 9 enum fields of `ComputedStyle` use the default `int` storage, 36 bytes of the 228. Each enum has 8 values or fewer. A `byte` storage gives 9 bytes. The saving is up to 27 bytes for each element, less the alignment padding next to the `Length` fields. The storage type is part of the public API, so a change after the first package release breaks binary compatibility.
**Fix:** Decide before the first package release. Option A: declare each layout enum as `: byte`. Option B: keep `int` and record the reason in `context.md`. The orchestrator owns this public API choice.
**Effort:** Low
**Resolved:** Option A. Each layout enum (`FlexDirection`, `FlexWrap`, `Justify`, `Align`, `Display`, `PositionType`, `Overflow`, `MeasureMode`, `LengthUnit`) has `: byte` storage. `ComputedStyle` went from 228 to 208 bytes and `LayoutNode` from 344 to 312 bytes. `p7check` gives the same result (pass 319, fail 21, skip 208). The build has 0 warnings, and 566 tests pass.

---

## What's Already Good

- **The cache rule of the plan holds.** The pass reads the bits and does not change them. The generation resets each dirty element one time in each pass. The clear walk then removes each layout bit, also in a skipped subtree. No probe step left a layout bit under a root.
- **The stricter cache works.** About 228,000 probe compares with reparents, promotes, root order changes, and toggles of `Display`, `Position`, and `FlexWrap` gave 0 stale rects outside CR-2. The scratch mutation proves that the owner-size rule is necessary.
- **Zero allocation.** No lambda, collection, array `new`, `params`, `HasFlag`, LINQ, or string is in `src/BrutalUi.Core/Layout/`. The only grep hit is a doc comment. `FlexLine` is a struct of index ranges. The cache is an `[InlineArray(8)]`. Six warm probe paths and the repository test measure 0 bytes.
- **A faithful port.** Each function names its Yoga source. The formulas match `CalculateLayout.cpp`, `FlexLine.cpp`, `AbsoluteLayout.cpp`, and `Cache.cpp` at `v3.2.1`. Each difference from Yoga has a "CSS difference from Yoga" comment and a row in `context.md`.
- **The API docs.** Each public member has an XML doc. Each style field states its default and its unit. The docs state the CSS differences, for example the physical offsets on a reverse axis.
- **The `Length` model.** The `Undefined` default maps to the default of each property with no table. The factories reject `NaN` and infinity. `Equals` and `GetHashCode` agree, also for `-0`.
- **The seams.** `BrutalUi.Core` has two `PackageReference` items and no ImGui reference. `MeasureCallback` states the contract that the imgui-renderer text measure must keep. `UiContext.Update` keeps the style seam, the layout loop, and the clear pass in frame order.
- **The tests.** 337 fixture ports, each with a `// Yoga fixture:` line and the Yoga header. The fixture rule holds: the hand-written tests live in `BoxModelTests.cs`. The differential test has five tree kinds, and a mutation check shows that 200 trees find each of the five cache faults of phase 9.
- **`AGENTS.md`.** .NET 10, nullable, file-scoped namespaces, `Brutal.Numerics` types, no native dependency, and no new package.

---

## Recommended Fix Order

| Order | Finding | Reason |
|-------|---------|--------|
| 1 | CR-2 | A stale rect with no error. One-line fix and one test. |
| 2 | CR-3 | An infinite rect from finite input. A small guard and one test. |
| 3 | CR-4 | A wrong rect in a common game layout. A small formula change and five tests. |
| 4 | CR-6 | The new change kinds guard CR-2 and the cache rule for the framework layer. |
| 5 | CR-1 | The largest performance risk. It needs a cache key change and the differential test of CR-6 as a guard. |
| 6 | CR-5 | The allocation guard must cover the paths that CR-1 to CR-4 change. |
| 7 | CR-7 | Input range guards and docs. |
| 8 | CR-8, CR-9, CR-12 | Doc corrections. |
| 9 | CR-10, CR-11 | Doc and notice corrections. |
| 10 | CR-13 | A public API choice for the orchestrator. |

---

## Progress

- [x] CR-1: A dirty path in a nested percent tree costs about 1.6 times more for each level
- [x] CR-2: A hidden root that moves into a tree keeps its visible rect
- [x] CR-3: The grow step can divide by a total factor of 0 and give an infinite width
- [x] CR-4: A wrap-reverse container mirrors its padding and the cross margins from the wrong edge
- [x] CR-5: The measured region of the allocation test runs one row and the cache hits only
- [x] CR-6: The differential test makes no reparent of a laid-out subtree and uses one root
- [x] CR-7: The style docs do not state the valid range of the grow and shrink factors
- [x] CR-8: The doc of `LayoutNode.Rect` says that X and Y of a root are 0
- [x] CR-9: The remarks of `UiContext.Update` still describe the pass before the cache
- [x] CR-10: The plan overstates the safe tree depth
- [x] CR-11: `MeasureStub.cs` copies Yoga test functions with no notice header
- [x] CR-12: The remarks of `DisplayFixtureTests` name a skip that no longer applies
- [x] CR-13: The layout enums use 4 bytes each
