# Flexbox Layout - Implementation Plan

**Epic:** core-tree (feature 2 of 3)
**Status:** Planning
**Started:** 2026-10-16
**Target Completion:** TBD
**Last Updated:** 2026-10-16 19:20

---

## Quality Bar

**Target quality:** Production-grade library code. The renderer, the input router, the style pass, the framework, and the debugger read the rects of this engine. A wrong rect shows on screen with no error. A stale rect after a change is the worst defect, because it shows only after a specific sequence of changes.

- **Correctness:** The engine gives the same rects as Yoga for each ported fixture, within 0.001 pixels. A change of one element, then one `Update`, gives the same rects as a fresh layout of the same tree (the differential test of phase 9).
- **Dirty bits:** After each `UiContext.Update`, no element under a root holds `Layout` or `DescendantLayout`. The dirty-bit invariant of element-tree holds after each pass.
- **Zero allocation:** After warm-up, the layout pass allocates no managed memory. No closure, no array, no boxing, no `params`, no LINQ, and no string in the pass. A small check in this feature proves it. The allocation-guard feature measures a 1000-element tree later.
- **Licence:** Each file that ports Yoga logic carries a header that names Yoga and `THIRD-PARTY-NOTICES.md`. That file holds the Yoga MIT text, and the NuGet package of `BrutalUi.Core` contains it.
- **API clarity:** Each public member has an XML doc comment (CS1591 is an error). The doc comment of each style field states its default and its units.

---

## Executive Summary

This feature delivers the C# flexbox engine of `BrutalUi.Core`. The engine is a structural port of the Yoga 3 algorithm (Decision 1): the same eleven steps, the same function split, and the same formulas, on the property subset of section 6 of the technical design. `ComputedStyle` holds the layout inputs (Q1). `LayoutNode` holds the output rect, the measure callback slot, and the engine cache. `UiContext` gets a `ViewportSize` (Q2). `UiContext.Update` lays out each root against that size, and then clears the layout bits with the dirty walk. A per-element layout cache, as in Yoga, makes a clean subtree cost one cache check (phase 9). The source is about 1800 lines in `src/BrutalUi.Core/Layout/`. The tests port about 120 Yoga fixtures by hand, one xUnit test for each fixture, in `tests/BrutalUi.Core.Tests/Layout/`.

---

## User Personas Alignment

No `docs/user-personas.md` file exists. The personas below come from `docs/mission-statement.md` and the epic docs.

**Target Users:**
- [x] **The game developer** (sets the layout fields on `ref element.Style`, sets `ViewportSize` on a resize, calls `Update`, and reads `element.Layout.Rect`)
- [x] **The sibling feature agents** (allocation-guard measures this pass; the style-system epic writes `ComputedStyle` from the cascade; the imgui-renderer epic plugs text measure into the callback slot and reads the rects)
- [ ] **The markup author** (not served yet. The markup and style epics map attributes and stylesheet properties to the same fields)

**User Subsets Served:**
- [x] **Simple** - Default values that match Yoga (column, no shrink, stretch). `element.Style.Width = 100;` works through an implicit conversion to pixels.
- [x] **Complex** - Percent and auto lengths, absolute children with offsets, wrap with `align-content`, and a measure callback for an intrinsic size.

**UX Approach:**
- The developer writes `e.Style.FlexDirection = FlexDirection.Row; e.Style.Padding = LengthEdges.All(8); e.MarkDirty(DirtyFlags.Layout);`.
- The developer sets `context.ViewportSize = new float2(1920, 1080);` at start and on each resize.
- After `context.Update(dt)`, the developer reads `e.Layout.Rect` (X and Y relative to the parent, Z and W the size).

---

## Goals

### Primary Goals
1. Ship the layout fields of `ComputedStyle`, the `Length` and `LengthEdges` types, the layout enums, the `MeasureCallback` slot, and the output rect of `LayoutNode`.
2. Port the Yoga algorithm structure: flex basis and measure, line break, main-axis distribution, cross-axis alignment, absolute children.
3. Run the layout pass in `UiContext.Update` on dirty subtrees only, with a correct clear of the layout bits (the CR-1 hand-off of element-tree).
4. Allocate nothing in the pass after warm-up.

### Success Criteria
- [ ] `dotnet test --solution brutal-ui.sln` passes, with 100 or more Yoga fixture tests, and each supported property set in one fixture test or more.
- [ ] The differential test of phase 9 finds no difference between an incremental layout and a fresh layout.
- [ ] `LayoutAllocationTests` measures 0 bytes over 100 warm passes.
- [ ] `dotnet build brutal-ui.sln` gives 0 warnings. `BrutalUi.Core.csproj` holds no new `PackageReference`, and `Directory.Packages.props` does not change.
- [ ] `THIRD-PARTY-NOTICES.md` holds the Yoga MIT text, and the `BrutalUi.Core` package contains it.

---

## Architecture Overview

All types go into the `BrutalUi.Core` namespace (A14). The new source goes into `src/BrutalUi.Core/Layout/`. `ComputedStyle`, `LayoutNode`, and `UiContext` stay in `Tree/`. The engine is an internal static class in partial files. It reads `ref readonly ComputedStyle` and writes `ref LayoutNode` of each element, and it reads the children through `Element.ChildStorage.AsSpan()`.

### File Tree (end state)

```
THIRD-PARTY-NOTICES.md              # phase 1: the Yoga MIT text, the tag, the commit, the derived files
src/BrutalUi.Core/
├── BrutalUi.Core.csproj            # phase 1: pack THIRD-PARTY-NOTICES.md
├── Tree/
│   ├── ComputedStyle.cs            # phase 1 (fields), edits in phases 2 to 8
│   ├── LayoutNode.cs               # phase 1 (Rect, scratch), edits in phases 7 to 9
│   └── UiContext.cs                # phase 1: ViewportSize, the layout pass
└── Layout/
    ├── Length.cs                   # phase 1: Length, LengthUnit
    ├── LengthEdges.cs              # phase 2
    ├── LayoutEnums.cs              # phase 1 (FlexDirection, MeasureMode), edits in phases 3 to 8
    ├── MeasureCallback.cs          # phase 8
    ├── LayoutMath.cs               # phase 1, edits in phases 2 to 6 (Yoga-derived)
    ├── LayoutEngine.cs             # phase 1, edits in all later phases (Yoga-derived)
    ├── LayoutEngine.Lines.cs       # phase 3, edits in phases 6 to 8 (Yoga-derived)
    ├── LayoutEngine.Absolute.cs    # phase 5, edit in phase 6 (Yoga-derived)
    └── LayoutCache.cs              # phase 9 (Yoga-derived)
tests/BrutalUi.Core.Tests/Layout/
├── LayoutFixture.cs                # phase 1: the test helper
├── LayoutPassTests.cs              # phase 1, phase 5 adds tests
├── FlexDirectionFixtureTests.cs    # phase 1  (YGFlexDirectionTest)
├── MarginFixtureTests.cs           # phase 2, phase 6 adds the auto margins (YGMarginTest)
├── PaddingFixtureTests.cs          # phase 2  (YGPaddingTest)
├── BorderFixtureTests.cs           # phase 2  (YGBorderTest)
├── FlexFixtureTests.cs             # phase 3  (YGFlexTest)
├── MinMaxDimensionFixtureTests.cs  # phase 3  (YGMinMaxDimensionTest)
├── JustifyContentFixtureTests.cs   # phase 3  (YGJustifyContentTest)
├── DimensionFixtureTests.cs        # phase 3  (YGDimensionTest)
├── AlignItemsFixtureTests.cs       # phase 4  (YGAlignItemsTest)
├── AlignSelfFixtureTests.cs        # phase 4  (YGAlignSelfTest)
├── AbsolutePositionFixtureTests.cs # phase 5  (YGAbsolutePositionTest)
├── DisplayFixtureTests.cs          # phase 5  (YGDisplayTest)
├── RelativePositionFixtureTests.cs # phase 5  (relative offsets, Notes item 8)
├── PercentageFixtureTests.cs       # phase 6  (YGPercentageTest)
├── FlexWrapFixtureTests.cs         # phase 7  (YGFlexWrapTest)
├── AlignContentFixtureTests.cs     # phase 7  (YGAlignContentTest)
├── MeasureStub.cs                  # phase 8: the test stub of the measure callback
├── MeasureFixtureTests.cs          # phase 8  (YGMeasureTest, hand-written in Yoga)
├── SizeOverflowFixtureTests.cs     # phase 8  (YGSizeOverflowTest)
├── MeasureCacheFixtureTests.cs     # phase 9  (YGMeasureCacheTest, hand-written in Yoga)
├── LayoutCacheTests.cs             # phase 9
├── LayoutDifferentialTests.cs      # phase 9
└── LayoutAllocationTests.cs        # phase 10
docs/technical-design.md            # phase 10: sections 5 and 6
```

### Public API (end state)

| Type | Kind | Members |
|------|------|---------|
| `Length` | public readonly struct, `IEquatable<Length>` | `float Value`, `LengthUnit Unit`, `static Length Undefined` (the default), `static Length Auto`, `static Length Px(float value)`, `static Length Percent(float value)`, `implicit operator Length(float pixels)`, `==`, `!=`, `Equals`, `GetHashCode`, `ToString()` |
| `LengthUnit` | public enum | `Undefined = 0`, `Pixel`, `Percent`, `Auto` |
| `LengthEdges` | public struct (public fields) | `Length Left`, `Length Top`, `Length Right`, `Length Bottom`, `LengthEdges(Length left, Length top, Length right, Length bottom)`, `static LengthEdges All(Length value)` |
| `Display` | public enum | `Flex = 0`, `None` |
| `PositionType` | public enum | `Relative = 0`, `Absolute` |
| `FlexDirection` | public enum | `Column = 0`, `ColumnReverse`, `Row`, `RowReverse` |
| `FlexWrap` | public enum | `NoWrap = 0`, `Wrap`, `WrapReverse` |
| `Justify` | public enum | `FlexStart = 0`, `Center`, `FlexEnd`, `SpaceBetween`, `SpaceAround`, `SpaceEvenly` |
| `Align` | public enum | `Auto = 0`, `FlexStart`, `Center`, `FlexEnd`, `Stretch`, `SpaceBetween`, `SpaceAround`, `SpaceEvenly` (no `Baseline`, A7) |
| `Overflow` | public enum | `Visible = 0`, `Hidden`, `Scroll` |
| `MeasureMode` | public enum | `Undefined = 0`, `Exactly`, `AtMost` |
| `MeasureCallback` | public delegate | `float2 MeasureCallback(Element element, float width, MeasureMode widthMode, float height, MeasureMode heightMode)` |
| `ComputedStyle` | public struct (public fields, A5) | `Display Display`, `PositionType Position`, `FlexDirection FlexDirection`, `FlexWrap FlexWrap`, `Justify JustifyContent`, `Align AlignItems`, `Align AlignSelf`, `Align AlignContent`, `float FlexGrow`, `float FlexShrink`, `Length FlexBasis`, `Length Width`, `Length Height`, `Length MinWidth`, `Length MinHeight`, `Length MaxWidth`, `Length MaxHeight`, `LengthEdges Margin`, `LengthEdges Padding`, `LengthEdges BorderWidth`, `Length Left`, `Length Top`, `Length Right`, `Length Bottom`, `Overflow Overflow` |
| `LayoutNode` | public struct | `float4 Rect { get; }` (X and Y: the position of the border box relative to the border box of the parent; Z: the width; W: the height; in pixels), `MeasureCallback? Measure { get; set; }` |
| `UiContext` | public sealed class (edit) | adds `float2 ViewportSize { get; set; }` |

Internal types: `LayoutEngine` (static partial class), `LayoutMath` (static class), `LayoutCache`, `CachedMeasurement`, `CachedMeasurementBuffer` (`[InlineArray(8)]`). Internal fields of `LayoutNode`: `ComputedRect`, `MeasuredWidth`, `MeasuredHeight`, `ComputedFlexBasis`, `LineIndex`, `Generation`, `Cache`.

### Key Components
- **`Length`** - A value and a unit. `default` is `Undefined`, which means "the default of the property" (A2): auto for a size and for the basis, no limit for a min or max size, 0 for a margin, a padding, and a border, and "not set" for an offset. An implicit conversion turns a `float` into pixels. The internal `Resolve(float ownerSize)` gives pixels, or `NaN` for `Undefined`, `Auto`, and a percent of an undefined owner size.
- **`ComputedStyle`** - The layout inputs. `default(ComputedStyle)` gives the Yoga defaults (A1). The style-system epic adds the paint fields and writes the struct from the cascade.
- **`LayoutNode`** - The output rect, the measure callback slot, and the engine scratch and cache. The cache lives inline (phase 9), so an element needs no heap object for the layout.
- **`LayoutEngine`** - The structural port of Yoga (Decision 1). Entry: `internal static void Calculate(Element root, float availableWidth, float availableHeight)`. `NaN` means no limit, as `YGUndefined` in Yoga.
- **`LayoutMath`** - The float rules of Yoga (`NaN` as undefined, a compare with an epsilon of 0.0001), the axis helpers, the edge helpers, and `BoundAxis`.
- **`UiContext.ViewportSize`** - The available size of each root. A `NaN` component means no limit.

### The Layout Pass and Its Dirty-Bit Rule

This section is the answer to the CR-1 hand-off of element-tree (plan Notes item 4 of element-tree).

```csharp
public void Update(float deltaTime)
{
    ReadOnlySpan<Element> roots = _roots.AsSpan();

    // Seam: the style pass of the style-system epic goes here.

    // The layout pass. It lays out each root against the viewport size and clears the layout bits.
    float2 viewport = _viewportSize;
    for (int i = 0; i < roots.Length; i++)
    {
        LayoutEngine.Calculate(roots[i], viewport.X, viewport.Y);
    }

    // The clear pass. It clears the bits that no earlier pass cleared.
    // (unchanged)
}
```

**The rule (end state, after phase 9):**
1. `Calculate` takes a new generation number (`Interlocked.Increment` on a static `long`, A13). Then it runs `LayoutInternal` on the root, as `YGNodeCalculateLayout` does.
2. An element is "dirty for layout" when it holds `Layout` or `DescendantLayout`. The pass reads the bits and does not change them during the computation. Thus the test gives the same answer on each visit in one pass.
3. On the first visit of a dirty element in a generation, `LayoutInternal` resets the cache of the element and stores the generation. This is `needToVisitNode` of Yoga.
4. On each visit, `LayoutInternal` reads the cache first. A hit sets the measured size and does not enter the subtree. A miss runs the flex algorithm on the element and stores the result.
5. **The choice for a deep change.** The pass lays out again from the root, and the cache bounds the cost. Each ancestor on the dirty path holds `DescendantLayout`, so it runs the flex algorithm again. Thus a flex parent with only `DescendantLayout` distributes the space again (the CR-1 case). Each clean sibling subtree returns its cached size in one check, unless its constraints changed. Then it runs again, which is correct. The cost is about the length of the dirty path times the child count of each level on it.
6. **The clear.** After the computation of a root, `Calculate` runs `DirtyWalk.Walk(root, DirtyFlags.Layout, ref clear)` with `ClearVisitor`. The walk enters each element with a layout bit, also in a `display: none` subtree. It clears the self bit and the descendant bit after the subtree of each element, and it clears no other kind. This is the CR-1 rule. No stale descendant bit stays, because the walk, not the engine, decides where to clear.
7. A clean root with the same viewport: the cache hit at the root costs one check. A new viewport size: the root misses, and only the subtrees with new constraints run again. Thus `ViewportSize` and `AddRoot` mark nothing (A20).
8. **Phases 1 to 8.** Before the cache of phase 9, `LayoutInternal` always computes. The pass lays out each root in full on each `Update`, and the clear of step 6 is the same. Phase 9 adds the cache at one marked place in `LayoutInternal`.

**The rules for the pass:** the engine never changes a child list while it reads its span (the `ArrayPool` gotcha of element-tree). The engine marks no other kind. A measure callback must not change the tree, mark a bit, or allocate (A11).

### Data Flow

The game writes the layout fields through `ref element.Style` and calls `MarkDirty(DirtyFlags.Layout)`. The mark sends `DescendantLayout` up to the root. The game sets `UiContext.ViewportSize`. Each frame, `UiContext.Update` calls `LayoutEngine.Calculate` on each root. The engine runs the eleven Yoga steps on each element that it enters. It reads the cache for each clean element, calls the measure callback for a leaf with a callback, and writes `LayoutNode.ComputedRect`. Then the clear walk clears the layout bits. The game, and later the renderer, reads `element.Layout.Rect` and adds the parent positions on the walk down.

---

## Implementation Phases

Each phase is a slice: one narrow path that works end to end, with a demo line that a person can check. Section 11 of `.beast-mode/phase-handoff.md` holds the rules. The demo of phase 1 is the recommended option of Q4.

**Rules for each phase:**
- Each phase ends with `dotnet build brutal-ui.sln 2>&1 | tail -n 30` and `dotnet test --solution brutal-ui.sln 2>&1 | tail -n 30`. Expect 0 warnings and 0 failed. Record the results in `context.md`.
- Each new public member gets an XML doc comment in the same task. Each style field doc states its default and its units.
- **The source of the port.** Read the Yoga source and fixtures from one release tag, in a clone outside the repository (Notes item 9). Do not copy Yoga files into the repository.
- **The fixture rule.** Port a fixture in the first phase that supports all its properties. Port the LTR half of the generated test only (A15). Skip a fixture that uses an out-of-scope feature: `direction` or RTL, a start or end edge, `gap`, `aspect-ratio`, `baseline`, `position: static`, `box-sizing`, `display: contents`, an errata or experimental flag, a text measure, or a value that comes from the Yoga pixel rounding (Q3). Skip a fixture that Yoga marks as disabled. Port a fixture that a later phase unlocks in that phase, when the file limit of the group allows it.
- **The fixture ledger.** `context.md` holds a table with one row for each Yoga file: the fixture count of the file, the ported count, and the skipped count for each reason. Update it in each phase.
- **The test shape.** One `[Fact]` for each fixture. The method name is the Pascal case of the fixture id (`flex_direction_row` gives `FlexDirectionRow`). The first line of the method is `// Yoga fixture: <fixture id>`. Each test file starts with the header of Decision 1.
- **Each phase demo** uses `dotnet run --project tests/BrutalUi.Core.Tests/BrutalUi.Core.Tests.csproj -- --filter-class '*<ClassName>'` (Notes item 10). Read the total and `failed: 0`.

---

### Phase 1: The Layout Pass and a Fixed-Size Column and Row
**Goal:** The smallest path that proves the idea. The size and direction fields, the output rect, the engine skeleton in the Yoga step order (fixed-size children, the content size, the default stretch, four directions), `UiContext.ViewportSize`, and the layout pass in `Update` with the clear walk.
**Demo:** the developer runs `dotnet run --project tests/BrutalUi.Core.Tests/BrutalUi.Core.Tests.csproj -- --filter-class '*FlexDirectionFixtureTests'` and reads a total of 6 or more with `failed: 0`.
**Needs:** none
**Agent tier:** ADVANCED (opus)

**Tier note:** This phase sets the structure of each later phase. `LayoutInternal` and `CalculateLayoutImpl` keep the Yoga signatures (the available width and height, the two measure modes, the owner size, and `performLayout`), so phases 3 to 9 add code with no rewrite. Write the eleven step headers of `calculateLayoutImpl` as comments now. When a fixture fails after two fix attempts, stop and report the fixture, the expected rect, and the actual rect.

**Tasks:**

Group A (src, 6 files):
- [ ] 1.1 Write `Layout/Length.cs`: `LengthUnit` and `Length` per the Public API table. `Px(NaN)` gives `Undefined`. Add the internal `Resolve(float ownerSize)` (Key Components). `ToString()` gives `"auto"`, `"undefined"`, `"10px"`, or `"50%"` (test output only, not in the pass).
- [ ] 1.2 Write `Layout/LayoutEnums.cs`: `FlexDirection` and `MeasureMode` per the Public API table. The doc of `FlexDirection` says that `Column` is the default, as in Yoga.
- [ ] 1.3 Edit `Tree/ComputedStyle.cs`: add the public fields `FlexDirection`, `Width`, and `Height`. Rewrite the struct doc: the layout inputs, `default` gives the Yoga defaults, a write sets no dirty bit (call `MarkDirty(DirtyFlags.Layout)`), the fields are public so that a nested write compiles (A5), and the style-system epic adds the paint fields.
- [ ] 1.4 Edit `Tree/LayoutNode.cs`: add `internal float4 ComputedRect`, `public readonly float4 Rect => ComputedRect;` (the doc of A3), and the internal scratch fields `MeasuredWidth`, `MeasuredHeight`, and `ComputedFlexBasis`. Rewrite the struct doc.
- [ ] 1.5 Write `Layout/LayoutMath.cs` with the Yoga header: `Epsilon = 0.0001f`, `IsDefined`, `MaxOrDefined`, `MinOrDefined`, `InexactEquals` (two `NaN` values are equal), `IsRow`, `IsReverse`, `CrossAxis(FlexDirection)` (row gives column, column gives row, A6), the main and cross dimension of a style (`Width` or `Height`), and `BoundAxis` (no min, max, padding, or border yet; phases 2 and 3 fill it).
- [ ] 1.6 Write `Layout/LayoutEngine.cs` (part 1) with the Yoga header: `internal static partial class LayoutEngine`, the static generation counter, and `Calculate` per `calculateLayout` of Yoga. A defined style size gives `Exactly`. Else a defined available size gives `Exactly` with that size. Else `Undefined`. Then `LayoutInternal(... performLayout: true)`, the root position (0, 0), and the clear walk of the section "The Layout Pass". Write `LayoutInternal` per `calculateLayoutInternal` with no cache. Put the comment `// Phase 9: the cache lookup goes here.` at the place of the Yoga lookup. It calls `CalculateLayoutImpl`, writes `MeasuredWidth` and `MeasuredHeight`, and writes the size of `ComputedRect` when `performLayout` is true.
- [ ] 1.7 Write `CalculateLayoutImpl` (part 2, same file) with the eleven step headers. Fill only: step 2 (the available inner size), step 3 (the flex basis is the definite main size, or the result of a measure-only `LayoutInternal` call for an auto child), step 4 (one line with all children), step 6 (the main-axis position from the flex-start edge, and the line cross size), step 7 (stretch for a child with an auto cross size, else flex-start), step 9 (the final size: definite, or the content size), and step 10 (for a reverse axis, convert the offset from the flex-start edge into X or Y: `parentSize - offset - childSize`, Decision 7). Each empty step header names the phase that fills it.

Group B (context, notice, tests, 6 files):
- [ ] 1.8 Edit `Tree/UiContext.cs`: add `float2 ViewportSize { get; set; }` (default `(0, 0)`). The setter throws `ArgumentOutOfRangeException` (`value`) for a negative or infinite component, and accepts `NaN` (A19). Replace the layout seam comment with the layout loop of the section "The Layout Pass". Keep the style seam comment and the clear pass. Update the doc of `Update` and of the class (set the size before the first `Update` and after each resize).
- [ ] 1.9 Write `THIRD-PARTY-NOTICES.md` at the repository root: one section for Yoga with the repository URL, the tag, the commit, the list of derived files (update it in each phase), and the full text of the Yoga `LICENSE` file at that tag. Edit `src/BrutalUi.Core/BrutalUi.Core.csproj`: add `<None Include="../../THIRD-PARTY-NOTICES.md" Pack="true" PackagePath="/" />` in an item group with a comment. Add no `PackageReference`.
- [ ] 1.10 Write `tests/BrutalUi.Core.Tests/Layout/LayoutFixture.cs`: `Calculate(Element root)` adds the root to a new `UiContext` with `ViewportSize = new float2(float.NaN, float.NaN)`, calls `Update(0f)`, and removes the root. This matches `YGNodeCalculateLayout(root, YGUndefined, YGUndefined, LTR)`. `AssertRect(Element element, float left, float top, float width, float height)` compares each value with a tolerance of 0.001. `AssertNoLayoutBits(Element root)` checks each element of the tree.
- [ ] 1.11 Write `FlexDirectionFixtureTests.cs`: port `flex_direction_column_no_height`, `flex_direction_row_no_width`, `flex_direction_column`, `flex_direction_row`, `flex_direction_column_reverse`, and `flex_direction_row_reverse` from `YGFlexDirectionTest`. Check each child and the root.
- [ ] 1.12 Write `LayoutPassTests.cs`: an auto root gets the size of `ViewportSize` after `Update`. A height change of the first child of a column, with `MarkDirty(DirtyFlags.Layout)`, moves the second child on the next `Update` (the parent holds only `DescendantLayout`, the CR-1 case). A leaf at depth 4 grows, and each auto-size ancestor grows. After each `Update`, `AssertNoLayoutBits` passes. A second `Update` with no change gives the same rects. A new `ViewportSize` changes the size of a clean auto root. A negative and an infinite component throw with the `ParamName` `value`.
- [ ] 1.13 Run the build and the tests (phase rules). Run the demo. Record the results, the Yoga tag and commit, and the first fixture ledger rows in `context.md`.

**Estimated Steps:** about 78 (13 tasks, about six steps for one task)

**Files (12):** Group A: `Layout/Length.cs`, `Layout/LayoutEnums.cs`, `Tree/ComputedStyle.cs`, `Tree/LayoutNode.cs`, `Layout/LayoutMath.cs`, `Layout/LayoutEngine.cs`. Group B: `Tree/UiContext.cs`, `THIRD-PARTY-NOTICES.md`, `BrutalUi.Core.csproj`, `Layout/LayoutFixture.cs`, `Layout/FlexDirectionFixtureTests.cs`, `Layout/LayoutPassTests.cs`.

**Acceptance Criteria:**
- [ ] The six fixtures and each test of task 1.12 pass.
- [ ] `UiContext.Update` holds the style seam comment, the layout loop, and the clear pass, in that order.
- [ ] `LayoutEngine.cs` holds the eleven step headers and the phase 9 cache comment.
- [ ] `THIRD-PARTY-NOTICES.md` holds the Yoga MIT text, and the derived files carry the header.

---

### Phase 2: The Box: Margin, Padding, and Border Width
**Goal:** The box model in pixels on both axes and in all four directions: margins outside the child, padding and border inside the parent, and the padding-and-border floor of each size.
**Demo:** the developer runs `dotnet run --project tests/BrutalUi.Core.Tests/BrutalUi.Core.Tests.csproj -- --filter-class '*MarginFixtureTests' --filter-class '*PaddingFixtureTests' --filter-class '*BorderFixtureTests'` and reads a total of 10 or more with `failed: 0`.
**Needs:** Phase 1
**Agent tier:** STANDARD (sonnet)

**Tier note:** The edits go into the core engine. Follow the Yoga helpers for each edge (`computeFlexBasisForChild`, the leading and trailing margin, padding, and border). When a fixture fails after two fix attempts, stop and report.

**Tasks:**

Group A (src, 4 files):
- [ ] 2.1 Write `Layout/LengthEdges.cs` per the Public API table, with a doc comment for each field.
- [ ] 2.2 Edit `Tree/ComputedStyle.cs`: add `Margin`, `Padding`, and `BorderWidth`. The docs: `Undefined` counts as 0. Padding and border accept no auto (auto counts as 0). The border accepts pixels only (a percent counts as 0). Margin auto comes in phase 6 (until then it counts as 0).
- [ ] 2.3 Edit `Layout/LayoutMath.cs`: the leading and trailing margin, padding, and border for an axis, `PaddingAndBorderForAxis`, and `MarginForAxis`. A margin and a padding resolve a percent against the owner width on all four edges (the Yoga rule, used in phase 6). `BoundAxis` now applies the padding-and-border floor.
- [ ] 2.4 Edit `Layout/LayoutEngine.cs`: apply the box in step 2 (the inner size), step 3 (the basis floor), step 6 (the leading padding and border, and the child margins on the main axis), step 7 (the cross position and the stretch size less the margins), step 9 (the final size adds padding and border), and `Calculate` (the root position adds the root margin).

Group B (tests, 3 files):
- [ ] 2.5 Write `MarginFixtureTests.cs`: port 4 or more pixel fixtures of `YGMarginTest` (for example `margin_top`, `margin_bottom`, `margin_and_stretch_row`, `margin_and_stretch_column`, `margin_with_sibling_row`, `margin_with_sibling_column`). Skip each start, end, and auto fixture for now.
- [ ] 2.6 Write `PaddingFixtureTests.cs`: port 3 or more fixtures of `YGPaddingTest` (for example `padding_no_size`, `padding_container_match_child`, `padding_stretch_child`).
- [ ] 2.7 Write `BorderFixtureTests.cs`: port 3 or more fixtures of `YGBorderTest` (for example `border_no_size`, `border_container_match_child`, `border_stretch_child`).
- [ ] 2.8 Run the build, the tests, and the demo. Update the ledger in `context.md`.

**Estimated Steps:** about 48 (8 tasks, about six steps for one task)

**Files (7):** Group A: `Layout/LengthEdges.cs`, `Tree/ComputedStyle.cs`, `Layout/LayoutMath.cs`, `Layout/LayoutEngine.cs`. Group B: `MarginFixtureTests.cs`, `PaddingFixtureTests.cs`, `BorderFixtureTests.cs`.

**Acceptance Criteria:**
- [ ] `e.Style.Margin.Left = 4;` compiles and changes the layout after a mark and an `Update`.
- [ ] The fixtures of tasks 2.5 to 2.7 and all earlier tests pass.

---

### Phase 3: Main-Axis Distribution: Grow, Shrink, Basis, Min and Max, Justify
**Goal:** Steps 3 to 6 of Yoga in full for one line: the flex basis, the two-pass distribution of the free space with the min and max clamps, and `justify-content`.
**Demo:** the developer runs `dotnet run --project tests/BrutalUi.Core.Tests/BrutalUi.Core.Tests.csproj -- --filter-class '*FlexFixtureTests' --filter-class '*JustifyContentFixtureTests'` and reads a total of 18 or more with `failed: 0`.
**Needs:** Phase 2
**Agent tier:** ADVANCED (opus)

**Tier note:** The distribution is load-bearing. Port `computeFlexBasisForChildren`, `calculateFlexLine`, `resolveFlexibleLength`, `distributeFreeSpaceFirstPass`, `distributeFreeSpaceSecondPass`, and `justifyMainAxis` function by function. Keep the Yoga names in Pascal case. A line is an index range over the child span (Decision 7). Never store a list.

**Tasks:**

Group A (src, 5 files):
- [ ] 3.1 Edit `Layout/LayoutEnums.cs` and `Tree/ComputedStyle.cs`: add `Justify`, and the fields `FlexGrow`, `FlexShrink`, `FlexBasis`, `MinWidth`, `MinHeight`, `MaxWidth`, `MaxHeight`, and `JustifyContent`. The docs give the Yoga defaults (grow 0, shrink 0, basis auto, no min, no max, flex-start).
- [ ] 3.2 Edit `Layout/LayoutMath.cs`: `BoundAxisWithinMinAndMax` (the Yoga order: max, then min), and `BoundAxis` on top of it.
- [ ] 3.3 Write `Layout/LayoutEngine.Lines.cs` with the Yoga header: move step 3 out of `LayoutEngine.cs` into `ComputeFlexBasisForChildren` and `ComputeFlexBasisForChild`. The basis is the resolved `FlexBasis`, else the definite main size, else a measure-only `LayoutInternal` result. Always compute it again (Decision 1, the basis reuse of Yoga is dropped). Store it in `ComputedFlexBasis`.
- [ ] 3.4 In `LayoutEngine.Lines.cs`, write `CalculateFlexLine` for one line: the start and end index, the item count, `SizeConsumed`, `TotalFlexGrowFactors`, and `TotalFlexShrinkScaledFactors`, in a small struct on the stack. Phase 7 adds the break.
- [ ] 3.5 In `LayoutEngine.Lines.cs`, write `ResolveFlexibleLength` with `DistributeFreeSpaceFirstPass` and `DistributeFreeSpaceSecondPass`: the flex-shrink scaled by the basis, the clamp to min and max, the frozen items of the first pass, and the second layout call of each child with the final main size.
- [ ] 3.6 In `LayoutEngine.Lines.cs`, write `JustifyMainAxis`: the remaining free space, the leading and between space for each of the six values, and the line cross size.
- [ ] 3.7 Edit `Layout/LayoutEngine.cs`: call steps 3 to 6 through the new functions, apply min and max in step 9, and bound the root size in `Calculate`.

Group B (tests, 4 files):
- [ ] 3.8 Write `FlexFixtureTests.cs`: port 6 or more fixtures of `YGFlexTest` (for example `flex_basis_flex_grow_column`, `flex_basis_flex_grow_row`, `flex_basis_flex_shrink_column`, `flex_basis_flex_shrink_row`, `flex_shrink_to_zero`, `flex_basis_overrides_main_size`, `flex_grow_less_than_factor_one`).
- [ ] 3.9 Write `MinMaxDimensionFixtureTests.cs`: port 8 or more fixtures of `YGMinMaxDimensionTest` (for example `max_width`, `max_height`, `min_width`, `min_height`, `justify_content_min_max`, `flex_grow_to_min`, `flex_grow_within_max_width`, `min_width_overrides_max_width`). Skip the percent fixtures until phase 6.
- [ ] 3.10 Write `JustifyContentFixtureTests.cs`: port one fixture for each of the six values in a row and in a column (12 or more) from `YGJustifyContentTest`.
- [ ] 3.11 Write `DimensionFixtureTests.cs`: port `wrap_child` and `wrap_grandchild` from `YGDimensionTest`.
- [ ] 3.12 Run the build, the tests, and the demo. Update the ledger in `context.md`.

**Estimated Steps:** about 72 (12 tasks, about six steps for one task)

**Files (9):** Group A: `Layout/LayoutEnums.cs`, `Tree/ComputedStyle.cs`, `Layout/LayoutMath.cs`, `Layout/LayoutEngine.Lines.cs`, `Layout/LayoutEngine.cs`. Group B: `FlexFixtureTests.cs`, `MinMaxDimensionFixtureTests.cs`, `JustifyContentFixtureTests.cs`, `DimensionFixtureTests.cs`.

**Acceptance Criteria:**
- [ ] `LayoutEngine.Lines.cs` holds no collection type and no `new` of an array.
- [ ] The fixtures of tasks 3.8 to 3.11 and all earlier tests pass.

---

### Phase 4: Cross-Axis Alignment: Align Items and Align Self
**Goal:** Step 7 of Yoga in full: the alignment of each child on the cross axis, the stretch with a second exact layout, and the auto value of `align-self`.
**Demo:** the developer runs `dotnet run --project tests/BrutalUi.Core.Tests/BrutalUi.Core.Tests.csproj -- --filter-class '*AlignItemsFixtureTests' --filter-class '*AlignSelfFixtureTests'` and reads a total of 10 or more with `failed: 0`.
**Needs:** Phase 3
**Agent tier:** ADVANCED (opus)

**Tasks:**
- [ ] 4.1 Edit `Layout/LayoutEnums.cs` and `Tree/ComputedStyle.cs`: add `Align` (no `Baseline`, A7), and the fields `AlignItems` and `AlignSelf`. The docs: `AlignItems` auto means stretch. `AlignSelf` auto means the `AlignItems` of the parent. The three space values act as flex-start on these two fields (Yoga).
- [ ] 4.2 Edit `Layout/LayoutMath.cs`: `ResolveChildAlignment(ref readonly ComputedStyle parent, ref readonly ComputedStyle child)` per `resolveChildAlignment` of Yoga, with the auto rules of task 4.1.
- [ ] 4.3 Edit `Layout/LayoutEngine.cs`, step 7: flex-start, center, and flex-end with the cross margins. Stretch: when the child has an auto cross size and no auto cross margin, run `LayoutInternal` again with `Exactly` on the cross axis and `performLayout` true, as Yoga does.
- [ ] 4.4 Edit `Layout/LayoutEngine.cs`, steps 6 and 9: the cross size of a single line when the parent has a definite cross size (Yoga uses the available inner cross size), and the cross content size when it has none.
- [ ] 4.5 Write `AlignItemsFixtureTests.cs`: port 6 or more fixtures of `YGAlignItemsTest` (for example `align_items_stretch`, `align_items_center`, `align_items_flex_start`, `align_items_flex_end`, `align_items_center_child_with_margin_bigger_than_parent`, `align_items_flex_end_child_with_margin_bigger_than_parent`). Skip each baseline fixture.
- [ ] 4.6 Write `AlignSelfFixtureTests.cs`: port 4 or more fixtures of `YGAlignSelfTest` (for example `align_self_center`, `align_self_flex_end`, `align_self_flex_start`, `align_self_flex_end_override_flex_start`).
- [ ] 4.7 Run the build, the tests, and the demo. Update the ledger in `context.md`.

**Estimated Steps:** about 42 (7 tasks, about six steps for one task)

**Files (6):** `Layout/LayoutEnums.cs`, `Tree/ComputedStyle.cs`, `Layout/LayoutMath.cs`, `Layout/LayoutEngine.cs`, `AlignItemsFixtureTests.cs`, `AlignSelfFixtureTests.cs`

**Acceptance Criteria:**
- [ ] `default(ComputedStyle)` gives stretch, so the phase 1 fixtures still pass with no change.
- [ ] The fixtures of tasks 4.5 and 4.6 and all earlier tests pass.

---

### Phase 5: Absolute Children, Offsets, and Display None
**Goal:** Step 11 of Yoga for each absolute child of an element, the offsets of a relative child, and `display: none` with the zero-out of a hidden subtree.
**Demo:** the developer runs `dotnet run --project tests/BrutalUi.Core.Tests/BrutalUi.Core.Tests.csproj -- --filter-class '*AbsolutePositionFixtureTests' --filter-class '*DisplayFixtureTests'` and reads a total of 13 or more with `failed: 0`.
**Needs:** Phase 4
**Agent tier:** ADVANCED (opus)

**Tier note:** An absolute child lays out against its parent (A12). Port `layoutAbsoluteChild` with the `justifyAbsoluteChild` and `alignAbsoluteChild` parts of Yoga 3 for a child with no offset. Do not port the containing-block search of `layoutAbsoluteDescendants`, because no element is static. Port the errata-free path only.

**Tasks:**

Group A (src, 5 files):
- [ ] 5.1 Edit `Layout/LayoutEnums.cs` and `Tree/ComputedStyle.cs`: add `Display` and `PositionType`, and the fields `Display`, `Position`, `Left`, `Top`, `Right`, `Bottom`. The docs: an offset moves a relative child from its flex position. On an absolute child, the offsets place it in the padding box of the parent. `Undefined` means "not set".
- [ ] 5.2 Edit `Layout/LayoutMath.cs`: `RelativePosition(axis)` per Yoga (the leading offset when it is set, else the negative trailing offset), and the leading and trailing offset helpers.
- [ ] 5.3 Write `Layout/LayoutEngine.Absolute.cs` with the Yoga header: `LayoutAbsoluteChild`, the size part. The width from the style, else from `Left` and `Right`. The height likewise. A measure-only `LayoutInternal` call for an undefined size, then the final layout with `Exactly`.
- [ ] 5.4 In `LayoutEngine.Absolute.cs`, write the position part: from the leading offset, else from the trailing offset, else from the `JustifyContent` and the alignment of the parent.
- [ ] 5.5 Edit `Layout/LayoutEngine.cs`: skip an absolute child and a `display: none` child in steps 3 to 8. Add the relative offsets in steps 6 and 7. Call step 11 when `performLayout` is true. Write `ZeroOutLayoutRecursively` per Yoga (the rect, the scratch, and the cache to zero). Run it on a `display: none` child only when the child holds a layout bit (Decision 9).

Group B (tests, 4 files):
- [ ] 5.6 Write `AbsolutePositionFixtureTests.cs`: port 8 or more fixtures of `YGAbsolutePositionTest` that use `left`, `top`, `right`, or `bottom` and no start or end edge (for example `absolute_layout_start_top_end_bottom` only when it maps to left and right, `absolute_layout_within_border`, `absolute_layout_align_items_and_justify_content_center`, `absolute_layout_align_items_and_justify_content_flex_end`, `absolute_layout_align_items_and_justify_content_center_and_top_position`, `do_not_clamp_height_of_absolute_node_to_height_of_its_overflow_hidden_parent` after phase 8).
- [ ] 5.7 Write `DisplayFixtureTests.cs`: port 5 or more fixtures of `YGDisplayTest` (for example `display_none`, `display_none_fixed_size`, `display_none_with_margin`, `display_none_with_child`, `display_none_with_position`, `display_none_with_position_absolute`).
- [ ] 5.8 Write `RelativePositionFixtureTests.cs`: port 4 or more fixtures that set `left`, `top`, `right`, and `bottom` on a relative child (each offset one time or more). The header names each Yoga source file (Notes item 8).
- [ ] 5.9 Edit `LayoutPassTests.cs`: a hidden subtree with a marked leaf holds no layout bit after `Update`. A hidden element that becomes visible again, with a mark, gets its full layout. A clean hidden subtree is not entered (a leaf rect that a test sets by hand through `ComputedRect` stays as it is).
- [ ] 5.10 Run the build, the tests, and the demo. Update the ledger and the list of derived files in `THIRD-PARTY-NOTICES.md` and `context.md`.

**Estimated Steps:** about 60 (10 tasks, about six steps for one task)

**Files (9):** Group A: `Layout/LayoutEnums.cs`, `Tree/ComputedStyle.cs`, `Layout/LayoutMath.cs`, `Layout/LayoutEngine.Absolute.cs`, `Layout/LayoutEngine.cs`. Group B: `AbsolutePositionFixtureTests.cs`, `DisplayFixtureTests.cs`, `RelativePositionFixtureTests.cs`, `LayoutPassTests.cs`. (`THIRD-PARTY-NOTICES.md` gets one line in task 5.10.)

**Acceptance Criteria:**
- [ ] An absolute child takes no space in the flex line of its parent.
- [ ] A `display: none` subtree has zero rects and no layout bit after `Update`.
- [ ] The tests of tasks 5.6 to 5.9 and all earlier tests pass.

---

### Phase 6: Percent Lengths and Auto Margins
**Goal:** A percent value on each length field, with the Yoga owner size for each field, and auto margins on the main axis and the cross axis.
**Demo:** the developer runs `dotnet run --project tests/BrutalUi.Core.Tests/BrutalUi.Core.Tests.csproj -- --filter-class '*PercentageFixtureTests'` and reads a total of 10 or more with `failed: 0`.
**Needs:** Phase 5
**Agent tier:** ADVANCED (opus)

**Tier note:** The auto margins change `JustifyMainAxis` and step 7. On the main axis, the auto margins take the free space and `justify-content` gives no space. On the cross axis, two auto margins center the child, and one auto margin pushes it to the other edge.

**Tasks:**
- [ ] 6.1 Audit each `Resolve` call in `Layout/LayoutMath.cs`, `Layout/LayoutEngine.cs`, and `Layout/LayoutEngine.Lines.cs`. The owner size is the owner width for a width, a min width, a max width, and each margin and padding edge. It is the owner height for a height, a min height, and a max height. It is the owner main size for the basis. A percent basis with an undefined owner main size acts as auto. Fix each wrong owner size.
- [ ] 6.2 Edit `Layout/LayoutEngine.Absolute.cs`: a percent size and a percent offset of an absolute child resolve against the padding box of the parent (the errata-free Yoga path).
- [ ] 6.3 Edit `Layout/LayoutEngine.Lines.cs`: auto margins on the main axis in `JustifyMainAxis` (count the auto margins of the line and give each one an equal part of the free space).
- [ ] 6.4 Edit `Layout/LayoutEngine.cs`: auto margins on the cross axis in step 7. A child with an auto cross margin does not stretch. Update the doc of `ComputedStyle.Margin` (auto now works).
- [ ] 6.5 Write `PercentageFixtureTests.cs`: port 10 or more fixtures of `YGPercentageTest` (for example `percentage_width_height`, `percentage_position_left_top`, `percentage_position_bottom_right`, `percentage_flex_basis`, `percentage_flex_basis_cross`, `percentage_margin_should_calculate_based_only_on_width`, `percentage_padding_should_calculate_based_only_on_width`, `percentage_absolute_position`, `percentage_width_height_undefined_parent_size`, `percent_within_flex_grow`). Cover a min or a max percent one time or more.
- [ ] 6.6 Edit `MarginFixtureTests.cs`: port 5 or more auto fixtures of `YGMarginTest` (for example `margin_auto_bottom`, `margin_auto_top`, `margin_auto_bottom_and_top`, `margin_auto_left_and_right_column`, `margin_auto_left`, `margin_auto_right`).
- [ ] 6.7 Run the build, the tests, and the demo. Update the ledger in `context.md`.

**Estimated Steps:** about 42 (7 tasks, about six steps for one task)

**Files (6):** `Layout/LayoutMath.cs`, `Layout/LayoutEngine.cs`, `Layout/LayoutEngine.Lines.cs`, `Layout/LayoutEngine.Absolute.cs`, `PercentageFixtureTests.cs`, `MarginFixtureTests.cs`

**Acceptance Criteria:**
- [ ] A percent margin on the top edge resolves against the owner width.
- [ ] The fixtures of tasks 6.5 and 6.6 and all earlier tests pass.

---

### Phase 7: Line Break: Flex Wrap and Align Content
**Goal:** Step 4 with the line break, the loop of steps 4 to 7 over each line, step 8 (`align-content`), and `wrap-reverse`.
**Demo:** the developer runs `dotnet run --project tests/BrutalUi.Core.Tests/BrutalUi.Core.Tests.csproj -- --filter-class '*FlexWrapFixtureTests' --filter-class '*AlignContentFixtureTests'` and reads a total of 16 or more with `failed: 0`.
**Needs:** Phase 4
**Agent tier:** ADVANCED (opus)

**Tier note:** The line break is load-bearing. Each line is an index range over the child span, and `LineIndex` on each child records its line for step 8. Port the break test of `calculateFlexLine` exactly (the size with the min clamp, the margin, and "the line holds one item or more").

**Tasks:**

Group A (src, 5 files):
- [ ] 7.1 Edit `Layout/LayoutEnums.cs` and `Tree/ComputedStyle.cs`: add `FlexWrap`, and the fields `FlexWrap` and `AlignContent`. The doc of `AlignContent`: auto means flex-start (the Yoga default), and `AlignContent` applies to a wrapped container only.
- [ ] 7.2 Edit `Tree/LayoutNode.cs`: add the internal `int LineIndex`.
- [ ] 7.3 Edit `Layout/LayoutEngine.Lines.cs`: the break in `CalculateFlexLine`. It returns the end index of the line, and the caller starts the next line at that index.
- [ ] 7.4 Edit `Layout/LayoutEngine.cs`: the loop of steps 4 to 7 for each line, the cross size of each line (the largest child cross size with margins), and the total cross size for step 9.
- [ ] 7.5 Edit `Layout/LayoutEngine.cs`, step 8: `align-content` for flex-start, center, flex-end, stretch, space-between, space-around, and space-evenly. With stretch, a child with stretch alignment gets a second exact layout with the line cross size, as Yoga does.
- [ ] 7.6 Edit `Layout/LayoutEngine.cs`: `wrap-reverse` flips the cross position of each child against the cross size of the element (the Yoga step after step 9).

Group B (tests, 2 files):
- [ ] 7.7 Write `FlexWrapFixtureTests.cs`: port 8 or more fixtures of `YGFlexWrapTest`, with wrap and wrap-reverse in a row and in a column (for example `wrap_column`, `wrap_row`, `wrap_row_align_items_flex_end`, `wrap_row_align_items_center`, `flex_wrap_children_with_min_main_overriding_flex_basis`, `flex_wrap_wrap_to_child_height`, `wrap_reverse_row_align_content_flex_start`, `wrap_reverse_column_fixed_size`).
- [ ] 7.8 Write `AlignContentFixtureTests.cs`: port one fixture or more for each of the seven values (8 or more) from `YGAlignContentTest` (for example `align_content_flex_start`, `align_content_flex_end`, `align_content_center`, `align_content_stretch`, `align_content_spacebetween`, `align_content_spacearound`, `align_content_stretch_row`).
- [ ] 7.9 Run the build, the tests, and the demo. Update the ledger in `context.md`.

**Estimated Steps:** about 54 (9 tasks, about six steps for one task)

**Files (7):** Group A: `Layout/LayoutEnums.cs`, `Tree/ComputedStyle.cs`, `Tree/LayoutNode.cs`, `Layout/LayoutEngine.Lines.cs`, `Layout/LayoutEngine.cs`. Group B: `FlexWrapFixtureTests.cs`, `AlignContentFixtureTests.cs`.

**Acceptance Criteria:**
- [ ] A row that does not fit breaks into lines, and each line keeps the order of the children.
- [ ] The fixtures of tasks 7.7 and 7.8 and all earlier tests pass.

---

### Phase 8: The Measure Callback Slot and Overflow
**Goal:** The measure callback slot with the Yoga measure rules, a test stub, and the layout effects of `overflow: scroll`.
**Demo:** the developer runs `dotnet run --project tests/BrutalUi.Core.Tests/BrutalUi.Core.Tests.csproj -- --filter-class '*MeasureFixtureTests'` and reads a total of 6 or more with `failed: 0`.
**Needs:** Phase 3
**Agent tier:** STANDARD (sonnet)

**Tasks:**

Group A (src, 6 files):
- [ ] 8.1 Write `Layout/MeasureCallback.cs`: the public delegate of the Public API table. The doc gives the rules of A10 and A11: the engine calls it for an element with no children only. It gets the inner size (the available size less margin, padding, and border) and the two modes. It must not change the tree, mark a bit, or allocate. Assign a `static` lambda or a method group one time (for example in a constructor), not in the frame loop.
- [ ] 8.2 Edit `Tree/LayoutNode.cs`: add `public MeasureCallback? Measure { readonly get; set; }`. The doc: a set marks nothing, so call `MarkDirty(DirtyFlags.Layout)`.
- [ ] 8.3 Edit `Layout/LayoutEngine.cs`: `MeasureNodeWithMeasureFunc`, `MeasureNodeWithoutChildren`, and `MeasureNodeWithFixedSize` per Yoga, called at the top of `CalculateLayoutImpl` in the Yoga order. A callback on an element with children has no effect.
- [ ] 8.4 Edit `Layout/LayoutEnums.cs` and `Tree/ComputedStyle.cs`: add `Overflow` and the field `Overflow`. The doc: hidden and visible give the same layout (A9). Scroll changes the measure constraints of the children and the final size. The renderer epic clips.
- [ ] 8.5 Edit `Layout/LayoutEngine.Lines.cs` and `Layout/LayoutEngine.cs`: the scroll rules of `computeFlexBasisForChild` (the `AtMost` constraint of a child on the axes of Yoga) and of step 9 (the final size of a scroll container with an `AtMost` mode).

Group B (tests, 3 files):
- [ ] 8.6 Write `MeasureStub.cs` (test code): a class with a call counter and a set of static measure functions (a fixed size, the available width with a fixed height, and the Yoga test functions that task 8.7 needs). A test creates the delegate one time in its setup.
- [ ] 8.7 Write `MeasureFixtureTests.cs`: port 6 or more tests of the hand-written `YGMeasureTest.cpp` of Yoga with the stub (Notes item 8, for example `dont_measure_single_grow_shrink_child`, `measure_absolute_child_with_no_constraints`, `dont_measure_when_min_equals_max`, `measure_flex_direction_row_and_padding`, `measure_enough_size_should_be_in_single_line`, `measure_zero_space_should_grow`). Add one test: a callback on an element with children is not called.
- [ ] 8.8 Write `SizeOverflowFixtureTests.cs`: port the fixtures of `YGSizeOverflowTest` (for example `nested_overflowing_child`, `nested_overflowing_child_in_constraint_parent`, `parent_wrap_child_size_overflowing_parent`), and 2 or more fixtures that set `overflow: scroll` (search the fixture HTML files for `overflow: scroll`; when none fits, write a test from the Yoga rule and say so in the header).
- [ ] 8.9 Run the build, the tests, and the demo. Port the overflow fixture of task 5.6 now, if it waits. Update the ledger in `context.md`.

**Estimated Steps:** about 54 (9 tasks, about six steps for one task)

**Files (9):** Group A: `Layout/MeasureCallback.cs`, `Tree/LayoutNode.cs`, `Layout/LayoutEngine.cs`, `Layout/LayoutEnums.cs`, `Tree/ComputedStyle.cs`, `Layout/LayoutEngine.Lines.cs`. Group B: `MeasureStub.cs`, `MeasureFixtureTests.cs`, `SizeOverflowFixtureTests.cs`.

**Acceptance Criteria:**
- [ ] A leaf with the stub gets the size that the stub returns, bounded by its min and max.
- [ ] `src/BrutalUi.Core/` holds no caller that sets `Measure` (A8 of the epic).
- [ ] The tests of tasks 8.7 and 8.8 and all earlier tests pass.

---

### Phase 9: The Layout Cache: Dirty Subtrees Only
**Goal:** The Yoga layout cache with the dirty-bit rule of the section "The Layout Pass", so a clean subtree costs one check, and a differential test that proves the incremental result.
**Demo:** the developer runs `dotnet run --project tests/BrutalUi.Core.Tests/BrutalUi.Core.Tests.csproj -- --filter-class '*LayoutCacheTests'` and reads `failed: 0`, with the test that runs a second `Update` with no change and counts 0 measure calls.
**Needs:** Phase 8
**Agent tier:** ADVANCED (opus)

**Tier note:** This phase is load-bearing. A stale cache gives a stale rect with no error. Follow the section "The Layout Pass" exactly. The bits stay read-only during the computation. The generation makes the reset happen one time in each pass. When a rule of that section looks wrong, stop and report.

**Tasks:**

Group A (src, 3 files):
- [ ] 9.1 Write `Layout/LayoutCache.cs` with the Yoga header: `CachedMeasurement` (the available width and height, the two modes, the computed width and height), `CachedMeasurementBuffer` (`[InlineArray(8)]`), and `LayoutCache` (one layout entry, the buffer, the next index). Port `canUseCachedMeasurement` of Yoga `Cache.cpp` (the exact match, the "old size is max content and still fits" rule, and the "new size is stricter and still valid" rule) with no pixel rounding (Q3).
- [ ] 9.2 Edit `Tree/LayoutNode.cs`: add the internal `long Generation` and `LayoutCache Cache`.
- [ ] 9.3 Edit `Layout/LayoutEngine.cs`, `LayoutInternal`: the dirty test and the reset on the first visit of a generation (steps 2 and 3 of the rule), the lookup in the Yoga order (an element with a callback uses `CanUseCachedMeasurement` with its margins; a layout call uses the layout entry; a measure call uses the buffer), the hit path (set the measured size, do not enter the subtree), and the store (a layout call writes the layout entry; a measure call writes the buffer and wraps the index at 8). Remove the phase 9 comment.
- [ ] 9.4 Edit `Layout/LayoutEngine.cs`: check that `ZeroOutLayoutRecursively` resets the cache, and that `Calculate` reads the root through the same lookup (a clean root with the same viewport returns at once).

Group B (tests, 3 files):
- [ ] 9.5 Write `MeasureCacheFixtureTests.cs`: port 4 or more tests of the hand-written `YGMeasureCacheTest.cpp` of Yoga with the counting stub (Notes item 8, for example `measure_once_single_flexible_child`, `remeasure_with_same_exact_width_larger_than_needed_height`, `remeasure_with_same_atmost_width_larger_than_needed_height`, `remeasure_with_computed_width_larger_than_needed_height`, `remeasure_with_atmost_computed_width_undefined_height`).
- [ ] 9.6 Write `LayoutCacheTests.cs` with the counting stub on each leaf: a second `Update` with no change calls no callback. A mark on one leaf calls the callback of that leaf only, and not of a clean sibling with the same constraints. A grow of one child calls the callback of a sibling whose free space changes. A new `ViewportSize` calls the callbacks of the subtrees with new constraints only. `AssertNoLayoutBits` passes after each `Update`.
- [ ] 9.7 Write `LayoutDifferentialTests.cs`: one seeded test (seed in the test name) builds 200 random trees of 20 to 40 elements with random supported fields (each enum value, pixel and percent lengths, grow, shrink, wrap, absolute, hidden elements, stub leaves). For each tree: lay out, then make 20 random changes (one field and a `MarkDirty`, or a child add, remove, or move), each followed by `Update`. After each `Update`, build a fresh copy of the tree (copy `Style` and `Measure` of each element), lay it out, and compare each rect with a tolerance of 0.001. On a difference, the message names the seed, the tree, the change, and the element path.
- [ ] 9.8 Run the build, the tests, and the demo. Run the differential test 3 times. Record the results in `context.md`.

**Estimated Steps:** about 48 (8 tasks, about six steps for one task)

**Files (6):** Group A: `Layout/LayoutCache.cs`, `Tree/LayoutNode.cs`, `Layout/LayoutEngine.cs`. Group B: `MeasureCacheFixtureTests.cs`, `LayoutCacheTests.cs`, `LayoutDifferentialTests.cs`.

**Acceptance Criteria:**
- [ ] The differential test finds no difference.
- [ ] A second `Update` with no change calls no measure callback.
- [ ] Each earlier fixture test passes with the cache on.

---

### Phase 10: The Allocation Check and the Design Doc
**Goal:** Prove that the warm pass allocates nothing, bring sections 5 and 6 of the technical design in line with the code, and check the notice.
**Demo:** the developer runs `dotnet run --project tests/BrutalUi.Core.Tests/BrutalUi.Core.Tests.csproj -- --filter-class '*LayoutAllocationTests'` and reads `failed: 0`.
**Needs:** Phase 9
**Agent tier:** STANDARD (sonnet)

**Tasks:**
- [ ] 10.1 Write `LayoutAllocationTests.cs` (A18): build a tree of about 60 elements with a row, a column, a wrapped row, grow, an absolute child, percent sizes, a hidden element, and two stub leaves. Create each delegate before the warm-up. Set `ViewportSize`, and run `Update(0.016f)` two times. Measure 100 steps with `GC.GetAllocatedBytesForCurrentThread()`. One step changes the width of one leaf (two values in turn), calls `MarkDirty(DirtyFlags.Layout)`, and calls `Update`. Assert `0L` outside the measured region. Keep the test synchronous. Add a second test that checks that the body changes a rect.
- [ ] 10.2 Edit `docs/technical-design.md`, section 6: the inputs on `ComputedStyle`, the output `LayoutNode.Rect` (relative to the parent), the units, the Yoga defaults, `ViewportSize`, the dirty-bit rule and the cache, and the list of Yoga parts that the port drops (Decision 1). Section 5: add `ViewportSize` to the shape of `UiContext`, and change the comment of `Layout` to "the output rect, the measure callback, and the layout cache". Use ASD-STE100 prose.
- [ ] 10.3 Check the notice: each Yoga-derived file of the file tree has the header, and `THIRD-PARTY-NOTICES.md` lists each one. Fix a missing item.
- [ ] 10.4 Check the source rules: run the greps of Verification steps 6 and 7, and fix each real hit.
- [ ] 10.5 Run each non-MANUAL step of the Verification Method, and `scripts/test-linux.sh`. Add the Windows run (Verification step 13) to "Needs Human Verification" in `context.md`.

**Estimated Steps:** about 30 (5 tasks, about six steps for one task)

**Files (3 and fixes):** `LayoutAllocationTests.cs`, `docs/technical-design.md`, `THIRD-PARTY-NOTICES.md`, and a source file only when a check of task 10.3 or 10.4 finds a fault.

**Acceptance Criteria:**
- [ ] The allocation test measures 0 bytes.
- [ ] Sections 5 and 6 of the technical design match the Public API table.

---

## Key Technical Decisions

### Assumptions

- A1 The defaults are the Yoga defaults: column, grow 0, shrink 0, basis auto, stretch, flex-start content, relative position. `default(ComputedStyle)` gives them.
- A2 `Length.Undefined` (the default) means the default of the property: auto, no limit, 0, or "not set".
- A3 `Rect` holds X and Y relative to the border box of the parent, and Z and W as the width and height, in pixels.
- A4 A write through `ref Style` or `ref Layout` marks nothing. The caller calls `MarkDirty(DirtyFlags.Layout)`. The style-system epic owns the write path later.
- A5 `ComputedStyle` and `LengthEdges` use public fields, so `e.Style.Margin.Left = 4;` compiles (a property gives CS1612).
- A6 The edges are left, top, right, and bottom only. No start or end edge, because `direction` is out of scope.
- A7 `align-items: baseline` is out. It needs a text baseline, and text measure comes later.
- A8 `margin: auto` works as in Yoga. Auto and percent on a border, and auto on a padding, count as 0.
- A9 `overflow: hidden` gives the same layout as visible. `scroll` follows the Yoga rules. The renderer epic clips.
- A10 The engine calls the measure callback only for an element with no children, as Yoga does.
- A11 A measure callback must not change the tree, mark a bit, or allocate.
- A12 An absolute child lays out against its parent. No static position and no containing-block search.
- A13 The generation counter is a static `long` with `Interlocked.Increment`. One tree is on one thread, but tests run in parallel.
- A14 One namespace, `BrutalUi.Core`. The engine goes into `src/BrutalUi.Core/Layout/`. `ComputedStyle`, `LayoutNode`, and `UiContext` stay in `Tree/`.
- A15 The tests go into `tests/BrutalUi.Core.Tests/Layout/`, one class for each Yoga fixture file, the LTR half of each generated test only.
- A16 The port is a sample of about 120 fixtures, not the full Yoga suite of several hundred.
- A17 The Yoga source and fixtures come from one release tag (the latest `v3.*`), cloned outside the repository.
- A18 This feature adds one small allocation check. The allocation-guard feature adds the 1000-element test and the benchmark.
- A19 `ViewportSize` starts at `(0, 0)`. A `NaN` component means no limit. A negative or infinite component throws.
- A20 `AddRoot` and a new `ViewportSize` mark nothing. The cache at the root sees the new constraints.

### Decision 1: A structural port of Yoga 3, with the MIT notice
**Context:** The requirements ask the plan to decide how close the port stays. A direct port of Yoga source keeps the Yoga MIT notice.
**Decision:** A structural port of `yoga/algorithm/` at one release tag (`CalculateLayout.cpp`, `FlexLine.cpp`, `AbsoluteLayout.cpp`, `Cache.cpp`, `BoundAxis.h`, `FlexDirection.h`, `Align.h`).
- **Kept:** the eleven steps of `calculateLayoutImpl` in order, the function split and names (in Pascal case), the formulas, `NaN` as undefined, the 0.0001 compare, the cache shape (one layout entry and 8 measurements), and the generation counter.
- **Dropped:** `direction` and RTL, start and end edges, `gap`, `aspect-ratio`, baseline, `position: static` and the containing-block search, `box-sizing`, `display: contents`, errata and experimental features, the config object, the pixel-grid rounding (Q3), node cloning and owners, `hasNewLayout`, the dirtied callback, logging, the edge shorthands, the `flex` shorthand, and the reuse of a flex basis across passes.
- **Changed:** a flex line is an index range over the child span (Decision 7). The dirty state is `DirtyFlags`, read-only during the pass (Decision 2). The rect stores X and Y only (Decision 7).
- **The notice rule:** the engine is a derived work of Yoga source, so it keeps the notice. `THIRD-PARTY-NOTICES.md` at the root holds the full Yoga `LICENSE` text of the tag ("Copyright (c) Meta Platforms, Inc. and affiliates."), the tag, the commit, and the list of derived files. Each derived file starts with this header:
  ```csharp
  // Ported from Yoga (https://github.com/facebook/yoga), <tag>, <file>. Copyright (c) Meta Platforms, Inc. and affiliates.
  // MIT License. See THIRD-PARTY-NOTICES.md at the repository root.
  ```
  The derived files are `LayoutMath.cs`, `LayoutEngine*.cs`, `LayoutCache.cs`, and each fixture test file (the expected values come from Yoga). `BrutalUi.Core.csproj` packs the notice file, because the package is a copy of the derived code. The `LICENSE` of the repository does not change.
**Rationale:** The Yoga structure is the source of truth for the fixtures. A port that keeps the function split lets a dev agent compare one C# function with one Yoga function when a fixture fails. A clean-room engine from the CSS specification gives different edge results, and each one costs a fixture investigation.
**Alternatives Considered:** A line-by-line port of all of Yoga. Rejected: it brings the dropped features and their branches, about two times the code. A clean-room engine with Yoga only as a test source. Rejected: more fixture failures with no reference to compare, and the expected values still come from Yoga, so the notice stays.

### Decision 2: The pass lays out from the root with a Yoga cache, and the walk clears the bits
**Context:** The CR-1 hand-off of element-tree. The walk calls a visitor only for an element with a self bit, and a flex parent with only `DescendantLayout` must lay out its children again.
**Decision:** The rule of the section "The Layout Pass". The engine has its own recursion. A layout bit, self or descendant, invalidates the cache of an element one time in each generation. A clean element uses its cache. The bits stay read-only during the computation. Then `DirtyWalk.Walk` with `ClearVisitor` clears the layout bits of the root.
**Rationale:** It is the Yoga model, so the fixtures and the cache rules port with no change. The clear walk enters each element with a bit, so no stale descendant bit can stay, also in a subtree that the engine skips. The engine needs no clear logic.
**Alternatives Considered:** Relayout boundaries (lay out again from the nearest ancestor whose size does not depend on the child). Rejected: a second set of rules for each property, and the cache already bounds the cost. A clear inside the engine recursion. Rejected: each skip path then needs its own clear, which is the risk that CR-1 names.

### Decision 3: The inputs live on `ComputedStyle` (Q1)
**Context:** The requirements say that `LayoutNode` holds the inputs and the output. Epic A2, the doc comment of `ComputedStyle.cs`, and section 7 of the technical design put the layout fields on `ComputedStyle`.
**Decision:** `ComputedStyle` holds the layout inputs. `LayoutNode` holds the output rect, the measure callback, and the engine scratch and cache. The engine reads `ref readonly ComputedStyle` and writes `ref LayoutNode`.
**Rationale:** The style pass of the style-system epic writes one struct. The split matches `Style` and `LayoutResults` of Yoga. A cascade change of a layout field and a mark are one path.
**Alternatives Considered:** See Q1.

### Decision 4: `UiContext.ViewportSize` (Q2)
**Context:** `UiContext` holds no available size for a root (CR-1).
**Decision:** A settable `float2 ViewportSize`. `Update` reads it for each root.
**Rationale:** A window resize changes the size at run time, so a setter is necessary in each option. The size is run-time state, not a setting, so Decision 9 of element-tree (a create-info for the first setting) does not apply.
**Alternatives Considered:** See Q2.

### Decision 5: No pixel rounding (Q3)
**Context:** Yoga rounds each rect to the pixel grid with absolute positions over the full tree on each pass.
**Decision:** The rects keep float values. The port skips a fixture whose expected value comes from the rounding.
**Rationale:** The rounding needs the absolute positions of the full tree on each pass, which a dirty-subtree pass does not keep. The renderer epic can snap at draw time with the display scale.
**Alternatives Considered:** See Q3.

### Decision 6: `Length` with an `Undefined` default and an implicit conversion
**Context:** `new Element()` zero-fills `ComputedStyle`. A size, a basis, a min, a max, a margin, and an offset each have a different default.
**Decision:** `default(Length)` is `Undefined`, and the engine maps it to the default of each property (A2). `Auto` is a separate value. An implicit conversion turns a `float` into pixels.
**Rationale:** The zero-filled struct is correct with no constructor and no initial-value table. It is the `YGValue` model of Yoga. The conversion keeps the test code and the game code short.
**Alternatives Considered:** A `NaN` float for each field and a separate unit field. Rejected: two fields for each length, and an easy mismatch.

### Decision 7: Index ranges for lines, and X and Y only in the rect
**Context:** Yoga stores the items of a line in a `std::vector`, and it stores a position for each physical edge.
**Decision:** A line is a start and an end index over `ChildStorage.AsSpan()`, in a struct on the stack. The engine skips an absolute child and a hidden child in each loop. `LineIndex` on each child records its line for step 8. For a reverse axis, the engine writes the offset from the flex-start edge into X or Y, and step 10 converts it in place.
**Rationale:** No allocation, and no copy of the child list. Step 10 of Yoga does the same conversion.
**Alternatives Considered:** A pooled scratch array of items. Rejected: a rent and a return in the pass for no gain.

### Decision 8: An inline cache of one layout entry and 8 measurements
**Context:** A flex parent lays out a child more than one time in one pass (the basis measure, the final layout, the stretch). With no cache, the cost grows with the depth of auto sizes.
**Decision:** `LayoutCache` holds one layout entry and an `[InlineArray(8)]` buffer of measurements, in `LayoutNode`, as `LayoutResults` of Yoga does.
**Rationale:** No heap object, and the same hit rules as Yoga, so `YGMeasureCacheTest` ports. The cost is about 220 bytes for each element.
**Alternatives Considered:** A single entry. Rejected: the basis measure and the final layout use different constraints, so a single entry misses each time.

### Decision 9: Zero out a hidden child only when it holds a layout bit
**Context:** Yoga zeroes each `display: none` subtree on each layout of its parent.
**Decision:** The engine runs `ZeroOutLayoutRecursively` on a hidden child only when the child holds a layout bit. A clean hidden child had its zero-out in an earlier pass (a change to `display: none` comes with a mark).
**Rationale:** A static hidden panel costs one check, not one visit for each of its elements. The clear walk still clears each bit in the subtree.

### Decision 10: A sample port with a ledger, plus a differential test
**Context:** The Yoga suite holds several hundred generated tests. The requirements ask for each property one time or more.
**Decision:** Each phase ports a named minimum (about 120 tests in total). The ledger in `context.md` counts each Yoga file. Phase 9 adds a differential test that compares the incremental result with a fresh layout.
**Rationale:** The sample covers each property and each enum value. The differential test covers the risk that the fixtures do not reach: a stale cache after a sequence of changes.

---

## Risks & Mitigation

| Risk | Likelihood | Impact | Mitigation |
|------|-----------|--------|------------|
| A port defect gives a wrong rect in a case that no fixture covers. | Medium | High | The function-by-function port (Decision 1), the ADVANCED tier on the algorithm phases, about 120 fixtures, and the stop rule after two failed fixes. |
| The cache gives a stale rect after a sequence of changes. | Medium | High | The read-only bits and the generation (Decision 2), the clear walk, `LayoutCacheTests`, and the differential test (task 9.7). |
| A stale descendant bit hides a later mark. | Low | High | The clear walk enters each element with a bit (Decision 2). `AssertNoLayoutBits` after each `Update` in the pass, cache, and differential tests. |
| The source and the fixtures come from different Yoga versions, so expected values disagree with the ported code. | Medium | Medium | One tag for both (A17), recorded in `THIRD-PARTY-NOTICES.md` and in each header. |
| The dev agent cannot reach the Yoga repository. | Medium | High | Clone the tag before phase 1 (Notes item 9). Without a clone, the agent stops and reports. |
| A fixture depends on the Yoga pixel rounding. | Medium | Low | Skip it with the reason "rounding" in the ledger (Q3). The tolerance is 0.001, so a skip shows as a clear failure first. |
| Phases 1 to 8 lay out each root in full on each `Update`, and nested auto sizes cost more for each level. | High | Low | The test trees are small. Phase 9 adds the cache. Section "The Layout Pass" step 8 records it. |
| A struct copy of `ComputedStyle` or `LayoutNode` (about 200 to 400 bytes) in a hot loop. | Medium | Low | The engine uses `ref readonly` and `ref` locals only. A copy is not an allocation, but phase 10 reads the loops. |
| The static generation counter races across parallel test classes. | Low | Medium | `Interlocked.Increment` (A13). |
| A measure callback allocates or changes the tree in the pass. | Low | Medium | The doc rule (A11). The allocation check uses stub delegates made before the warm-up. |
| The recursion of the engine is deeper than the walk (several frames for each level). | Low | Medium | UI trees are under 100 levels. Notes item 13. |
| `--filter-class` with more than one flag does not work in the test runner. | Low | Low | Run one command for each class (Notes item 10). |

---

## Dependencies

### Internal Dependencies
- core-tree/element-tree (complete): `Element`, `ChildStorage`, `DirtyFlags`, `DirtyWalk`, `ClearVisitor`, the empty `ComputedStyle` and `LayoutNode` slots, `UiContext.Update` with the seam, and `InternalsVisibleTo` for `BrutalUi.Core.Tests`.
- The dev-ops epic: the solution, the test project (xUnit v3 on Microsoft.Testing.Platform), `Directory.Build.props` (warnings as errors, code style in the build), and `scripts/test-linux.sh`.

### External Dependencies
- The Yoga repository on GitHub (`https://github.com/facebook/yoga`), one release tag, read only, outside the repository. No Yoga package and no Yoga binary.
- `Ahwoo.BRUTAL.Numerics` 2026.10.1 (pinned): `float2` and `float4`. The XML docs of the package show the fields `X`, `Y`, `Z`, `W` on `float4`.
- The base class library: `Interlocked`, `InlineArrayAttribute`, `MathF`.
- No new package and no native dependency.

---

## Definition of Done

All criteria below must pass. An independent evaluator runs the Verification Method from `/home/aaronstatic/dev/brutal-ui` on Linux. "MANUAL" marks a criterion that needs the author on Windows.

### Functional Criteria
- [ ] F1. Each of the 25 supported properties is set in one fixture test or more, and each fixture test passes (Verification steps 2 to 4).
- [ ] F2. Each fixture test file starts with the Yoga header, and each test method starts with `// Yoga fixture: <id>` (step 5).
- [ ] F3. `UiContext.Update` lays out each root against `ViewportSize`. An auto root gets the viewport size. A negative or infinite component throws (`LayoutPassTests`).
- [ ] F4. After each `Update`, no element under a root holds `Layout` or `DescendantLayout` (`LayoutPassTests`, `LayoutCacheTests`, `LayoutDifferentialTests`).
- [ ] F5. An incremental layout after random changes gives the same rects as a fresh layout (`LayoutDifferentialTests`).
- [ ] F6. A second `Update` with no change calls no measure callback. A mark on one leaf calls the callback of that leaf only (`LayoutCacheTests`).
- [ ] F7. A `display: none` subtree has zero rects and no layout bit (`DisplayFixtureTests`, `LayoutPassTests`).
- [ ] F8. A leaf with the stub callback gets the stub size. A callback on an element with children is not called (`MeasureFixtureTests`).

### Quality Criteria
- [ ] Q1. `dotnet build brutal-ui.sln` gives `0 Warning(s)` and `0 Error(s)`.
- [ ] Q2. `LayoutAllocationTests` measures 0 bytes over 100 warm steps.
- [ ] Q3. No file under `src/BrutalUi.Core/Layout/` holds a lambda, a collection type, an array `new`, `params`, `HasFlag`, or `System.Linq` (step 6).
- [ ] Q4. Each file under `src/BrutalUi.Core/Layout/` holds `namespace BrutalUi.Core;` (step 7).
- [ ] Q5. The doc comments and the changed docs use ASD-STE100 prose.
- [ ] Q6. (MANUAL) `scripts\test-windows.ps1` prints its PASS line on Windows.

### Integration Criteria
- [ ] I1. `UiContext.Update` holds the style seam comment, the layout loop over the roots, and the clear pass, in that order.
- [ ] I2. `ComputedStyle` holds the 25 fields of the Public API table. `LayoutNode` holds `Rect` and `Measure` as its public members.
- [ ] I3. `BrutalUi.Core.csproj` holds the same two `PackageReference` items. `Directory.Packages.props` does not change.
- [ ] I4. `THIRD-PARTY-NOTICES.md` holds the Yoga MIT text, a `v3` tag, a commit, and the list of derived files. Each derived file has the header. The `BrutalUi.Core` package contains the notice file.
- [ ] I5. Sections 5 and 6 of `docs/technical-design.md` match the Public API table.
- [ ] I6. `context.md` holds the fixture ledger with one row for each Yoga file in the file tree.

### Verification Method

Run steps 1 to 12 from `/home/aaronstatic/dev/brutal-ui` on Linux.

1. `dotnet build brutal-ui.sln 2>&1 | tail -n 30`. Expect `0 Warning(s)` and `0 Error(s)` (Q1).
2. `dotnet test --solution brutal-ui.sln 2>&1 | tail -n 30`. Expect 0 failed for each test project.
3. `cat tests/BrutalUi.Core.Tests/Layout/*FixtureTests.cs | grep -c '\[Fact\]'`. Expect 100 or more.
4. Run this loop. Expect a count of 1 or more on each line (F1).
   ```bash
   for p in Display Position FlexDirection FlexWrap JustifyContent AlignItems AlignSelf AlignContent FlexGrow FlexShrink FlexBasis Width Height MinWidth MinHeight MaxWidth MaxHeight Margin Padding BorderWidth Left Top Right Bottom Overflow; do printf '%s %s\n' "$p" "$(grep -lE "Style\.$p\b" tests/BrutalUi.Core.Tests/Layout/*FixtureTests.cs | wc -l)"; done
   ```
5. `grep -L 'THIRD-PARTY-NOTICES.md' tests/BrutalUi.Core.Tests/Layout/*FixtureTests.cs src/BrutalUi.Core/Layout/LayoutMath.cs src/BrutalUi.Core/Layout/LayoutEngine*.cs src/BrutalUi.Core/Layout/LayoutCache.cs`. Expect no output (F2, I4). `grep -c '// Yoga fixture:' tests/BrutalUi.Core.Tests/Layout/*FixtureTests.cs`. Expect each count equal to the `[Fact]` count of the file.
6. `grep -nE 'new [A-Za-z0-9_<>]+\[|List<|Dictionary<|Func<|Action<|=>\s*\{|\) =>|params |HasFlag|System\.Linq' src/BrutalUi.Core/Layout/*.cs`. Expect no output, or read each hit: an expression-bodied member is not a lambda (Q3).
7. `grep -L 'namespace BrutalUi.Core;' src/BrutalUi.Core/Layout/*.cs`. Expect no output (Q4).
8. For each class, run `dotnet run --project tests/BrutalUi.Core.Tests/BrutalUi.Core.Tests.csproj -- --filter-class '*<ClassName>'` and expect a total above 0 and `failed: 0`: `LayoutPassTests`, `LayoutCacheTests`, `LayoutDifferentialTests`, `MeasureFixtureTests`, `DisplayFixtureTests`, `LayoutAllocationTests` (F3 to F8, Q2).
9. `grep -c PackageReference src/BrutalUi.Core/BrutalUi.Core.csproj; git diff main -- Directory.Packages.props`. Expect `2` and no diff (I3).
10. `grep -n 'Meta Platforms' THIRD-PARTY-NOTICES.md; grep -nE 'v3\.[0-9]+' THIRD-PARTY-NOTICES.md`. Expect a hit for each. Then `rm -rf artifacts/verify-pack && dotnet pack src/BrutalUi.Core/BrutalUi.Core.csproj -c Release -o artifacts/verify-pack 2>&1 | tail -n 5 && unzip -l artifacts/verify-pack/BrutalUi.Core.*.nupkg | grep THIRD-PARTY-NOTICES.md`. Expect one line (I4).
11. Read `src/BrutalUi.Core/Tree/UiContext.cs` for I1, `ComputedStyle.cs` and `LayoutNode.cs` for I2, sections 5 and 6 of `docs/technical-design.md` for I5, and the ledger in `docs/features/core-tree/flexbox-layout/context.md` for I6.
12. `scripts/test-linux.sh; echo "exit=$?"`. Expect the PASS line and `exit=0`.
13. (MANUAL) On Windows, after `git pull --ff-only`, run `.\scripts\test-windows.ps1`. Expect the PASS line and `$LASTEXITCODE` 0 (Q6).

---

## Testing Strategy

### Unit Tests
- **Fixture tests** (`*FixtureTests`): one `[Fact]` for each ported Yoga fixture, through `LayoutFixture.Calculate`, which uses the public `UiContext` path with a `NaN` viewport. Each test checks the rect of each element of the fixture.
- **`LayoutPassTests`**: the viewport, the guards, the incremental move of a sibling, the deep change, the clear of the bits, and the hidden subtree.
- **`MeasureFixtureTests` and `MeasureCacheFixtureTests`**: hand ports of the hand-written Yoga measure tests, with `MeasureStub`.
- **`LayoutCacheTests`**: the callback counts for no change, one leaf, a sibling with new free space, and a new viewport.

### Differential Test
- `LayoutDifferentialTests` compares the incremental result with a fresh layout over 200 seeded random trees and 20 changes each. It covers the cache and the dirty bits together.

### Allocation Test
- `LayoutAllocationTests` (phase 10) measures 100 warm steps on a 60-element tree. The allocation-guard feature adds the 1000-element test and the benchmark.

### Integration Tests
- `scripts/test-linux.sh` runs the full build and the tests.
- (MANUAL) `scripts\test-windows.ps1` on Windows.

### Manual Testing
- None on Linux. The feature has no visible output. The imgui-renderer epic draws the rects.

---

## Related Documentation

- **Requirements:** `docs/features/core-tree/flexbox-layout/requirements.md`
- **Epic:** `docs/features/core-tree/epic-overview.md`, `docs/features/core-tree/epic-requirements.md`
- **Siblings:** `docs/features/core-tree/element-tree/implementation.md`, `context.md`, and `code-review.md` (CR-1); `docs/features/core-tree/allocation-guard/requirements.md`
- **Design:** `docs/technical-design.md` sections 4 (frame order), 5 (element tree), 6 (layout engine), 7 (style system), 13 (configuration), 15 (principles)
- **Rules:** `AGENTS.md`, `.beast-mode/phase-handoff.md` sections 1 and 11, `.beast-mode/tool-rules.md`
- **Yoga:** `https://github.com/facebook/yoga`: `yoga/algorithm/`, `gentest/fixtures/*.html`, `tests/generated/*.cpp`, `tests/YGMeasureTest.cpp`, `tests/YGMeasureCacheTest.cpp`, `LICENSE`
- **Format reference:** `docs/features/core-tree/element-tree/implementation.md`

---

## Notes

### Open Scope Questions

**Q1.** Where do the layout input properties live?
- A. `ComputedStyle` holds the inputs, and `LayoutNode` holds the output rect, the measure callback, and the engine cache. **(Recommended)**
- B. `LayoutNode` holds the inputs and the output, and `ComputedStyle` stays empty in this epic.
- C. A `LayoutStyle` struct inside `LayoutNode` holds the inputs, and the style-system epic copies the cascade result into it.
- Reason: A matches epic A2, the current doc comments, section 7 of the technical design, and the Yoga split of style and results, so the style pass writes one struct.

**Q2.** How does a root get its available size?
- A. A settable `float2 ViewportSize` property on `UiContext`, which each `Update` reads. **(Recommended)**
- B. A `UiContext.CreateInfo` with a required `ViewportSize`, plus the same setter for a resize.
- C. A size argument on `Update(float deltaTime, float2 viewportSize)`.
- Reason: a resize changes the size at run time, so each option needs a setter, and the constructor stays as element-tree left it.

**Q3.** Does the engine round the output rects to the pixel grid as Yoga does?
- A. No rounding in this feature: the rects keep float values, and the port skips a fixture whose expected value comes from the rounding. **(Recommended)**
- B. Port the Yoga pixel-grid rounding with a scale of 1 as the last step of each pass.
- Reason: the Yoga rounding needs the absolute positions of the full tree on each pass, which a dirty-subtree pass does not keep, and the renderer can snap at draw time.

**Q4.** What is the demo of phase 1?
- A. **Demo:** the developer runs `dotnet run --project tests/BrutalUi.Core.Tests/BrutalUi.Core.Tests.csproj -- --filter-class '*FlexDirectionFixtureTests'` and reads a total of 6 or more with `failed: 0`. **(Recommended)**
- B. **Demo:** the developer runs `scripts/test-linux.sh` and sees the line `test-linux: PASS build, test, gpu, smoke (120 frames) in <N> s`.
- C. **Demo:** the developer runs `dotnet run --project tests/BrutalUi.Core.Tests/BrutalUi.Core.Tests.csproj -- --filter-class '*LayoutPassTests'` and reads `failed: 0`, with the test that grows a child and moves its sibling.
- Reason: A shows Yoga fixtures pass through the new engine and the public pass, and the runner prints the count for one class.

### Conflicts and Gaps

1. **Where the inputs live.** The requirements (line 12) say that `LayoutNode` holds the input properties and the output rect. Epic A2, the doc comment of `ComputedStyle.cs` ("The core-tree/flexbox-layout feature adds the layout fields"), and section 7 of the technical design put the layout fields on `ComputedStyle`. Section 5 of the technical design calls `LayoutNode` "flexbox input and output". The plan follows Q1 option A. Task 10.2 updates section 5. When the user picks B, tasks 1.3 and each "Edit `Tree/ComputedStyle.cs`" task move to `Tree/LayoutNode.cs`, and Decision 3 changes.
2. **The root size (CR-1).** `UiContext` holds no available size. The plan adds `ViewportSize` (Q2). Decision 9 of element-tree says that the epic with the first setting adds `UiContext.CreateInfo`. The plan treats the viewport as run-time state, not a setting, so no create-info comes yet.
3. **The seam comment.** The seam says "It walks DirtyFlags.Layout and clears the layout bits." The pass runs its own recursion and then the walk with `ClearVisitor`, so the comment stays true. Task 1.8 replaces the comment with the code.
4. **The CR-1 choice.** The plan lays out from the root with the Yoga cache, and it clears with the walk after the computation (Decision 2). This is the answer to plan Notes item 4 of element-tree.
5. **The allocation test.** The requirements put the allocation test out of scope. The plan adds one small check (A18, task 10.1), as element-tree did with `TreeAllocationTests`. The 1000-element test and the benchmark stay with allocation-guard.
6. **The RTL half of the Yoga tests.** Each generated Yoga test checks LTR, then sets RTL on the root and checks again. The port keeps the LTR half only (A15).
7. **The pixel rounding.** The generated Yoga tests run with a point scale factor of 1, so Yoga rounds the expected values. Most fixtures give whole values with no rounding. Q3 decides the rest.
8. **Names to confirm.** The planner did not read the Yoga repository. The fixture ids in the tasks are examples from memory. The dev agent confirms each id in the clone and picks another fixture of the same file when an id does not exist. Unknown items: (a) the Yoga file that holds fixtures for `left`, `top`, `right`, and `bottom` on a relative child (task 5.8; candidates are `YGFlexDirectionTest`, `YGPercentageTest`, and any `YG*PositionTest` file); (b) a fixture with `overflow: scroll` (task 8.8); (c) the test names of the hand-written `YGMeasureTest.cpp` and `YGMeasureCacheTest.cpp` (tasks 8.7 and 9.5); (d) whether `start` and `end` edges appear in the HTML as `margin-start` or as `margin-inline-start`; (e) the exact Yoga `LICENSE` text at the tag.
9. **The Yoga clone.** The dev agent needs the Yoga source and fixtures. Before phase 1, run `git clone --depth 1 --branch <tag> https://github.com/facebook/yoga <session scratch folder>/yoga-src`, outside the repository, and treat it as read-only data (run no script from it). Record the tag and the commit in `context.md`. When no network is available, stop and report.
10. **The test filter.** Microsoft.Testing.Platform prints no name of a passed test. The demos use `--filter-class`. The demos of phases 2 to 7 pass `--filter-class` more than one time. When the runner accepts one flag only, run one command for each class and add the totals.
11. **The paint kind.** The layout pass marks no `Paint` bit when a rect changes. The renderer draws the full tree each frame (section 4 of the technical design). The imgui-renderer epic can add a paint mark later, through the visitor rule "a pass can mark another kind".
12. **Element size.** `ComputedStyle` (about 200 bytes) and `LayoutNode` with the cache (about 300 bytes) make each element about 600 bytes. A 1000-element tree uses about 0.6 MB. The allocation-guard benchmark can report it.
13. **The recursion depth.** The engine recurses several frames for each level. A tree deeper than about a thousand levels can overflow the stack. No game UI comes near it.
14. **No user personas file.** `docs/user-personas.md` does not exist. The personas of this plan come from the mission statement and the epic docs.
15. **The epic overview.** After this feature, `/update-epic core-tree` records `ViewportSize`, the dirty-bit rule of Decision 2, and the notice file under Integration & Architecture.
16. **The prose lint.** The planner had no shell, so it did not run `ste-lint.py` on this file. The dev agent can run `python3 /home/aaronstatic/.claude/skills/asd-ste100/scripts/ste-lint.py --fail-over 2.5` on the changed docs in task 10.5.

---

*This plan will be updated as implementation progresses. See context.md and tasks.md for current status.*
