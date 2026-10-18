# core-tree/flexbox-layout - Task Checklist

**Last Updated:** 2026-10-18 04:25
**Progress:** 88/88 tasks complete (100%)
**Advanced phases:** Phases 1, 3, 4, 5, 6, 7, and 9 (ADVANCED, opus). Phases 2, 8, and 10 are STANDARD (sonnet).

Source paths: `src/` means `src/BrutalUi.Core/`. Test paths: `tests/` means `tests/BrutalUi.Core.Tests/Layout/`.

---

## Phase 1: The Layout Pass and a Fixed-Size Column and Row (13/13 complete)

**Demo:** the developer runs `dotnet run --project tests/BrutalUi.Core.Tests/BrutalUi.Core.Tests.csproj -- --filter-class '*FlexDirectionFixtureTests'` and reads a total of 6 or more with `failed: 0`.
**Needs:** none

Group A (src, 6 files):

- [x] ✅ **1.1 Write `Layout/Length.cs`**
  - Description: `LengthUnit` and `Length` per the Public API table. `Px(NaN)` gives `Undefined`. Add the internal `Resolve(float ownerSize)` (Key Components). `ToString()` gives `"auto"`, `"undefined"`, `"10px"`, or `"50%"` (test output only, not in the pass).
  - File(s): `src/BrutalUi.Core/Layout/Length.cs`
  - Estimate: 6 steps

- [x] ✅ **1.2 Write `Layout/LayoutEnums.cs`**
  - Description: `FlexDirection` and `MeasureMode` per the Public API table. The doc of `FlexDirection` says that `Column` is the default, as in Yoga.
  - File(s): `src/BrutalUi.Core/Layout/LayoutEnums.cs`
  - Estimate: 3 steps

- [x] ✅ **1.3 Edit `Tree/ComputedStyle.cs`**
  - Description: add the public fields `FlexDirection`, `Width`, and `Height`. Rewrite the struct doc: the layout inputs, `default` gives the Yoga defaults, a write sets no dirty bit (call `MarkDirty(DirtyFlags.Layout)`), the fields are public so that a nested write compiles (A5), and the style-system epic adds the paint fields.
  - File(s): `src/BrutalUi.Core/Tree/ComputedStyle.cs`
  - Estimate: 4 steps

- [x] ✅ **1.4 Edit `Tree/LayoutNode.cs`**
  - Description: add `internal float4 ComputedRect`, `public readonly float4 Rect => ComputedRect;` (the doc of A3), and the internal scratch fields `MeasuredWidth`, `MeasuredHeight`, and `ComputedFlexBasis`. Rewrite the struct doc.
  - File(s): `src/BrutalUi.Core/Tree/LayoutNode.cs`
  - Estimate: 4 steps

- [x] ✅ **1.5 Write `Layout/LayoutMath.cs`**
  - Description: with the Yoga header: `Epsilon = 0.0001f`, `IsDefined`, `MaxOrDefined`, `MinOrDefined`, `InexactEquals` (two `NaN` values are equal), `IsRow`, `IsReverse`, `CrossAxis(FlexDirection)` (row gives column, column gives row, A6), the main and cross dimension of a style (`Width` or `Height`), and `BoundAxis` (no min, max, padding, or border yet; phases 2 and 3 fill it).
  - File(s): `src/BrutalUi.Core/Layout/LayoutMath.cs`
  - Estimate: 6 steps

- [x] ✅ **1.6 Write `Layout/LayoutEngine.cs` (part 1)**
  - Description: with the Yoga header: `internal static partial class LayoutEngine`, the static generation counter, and `Calculate` per `calculateLayout` of Yoga. A defined style size gives `Exactly`. Else a defined available size gives `Exactly` with that size. Else `Undefined`. Then `LayoutInternal(... performLayout: true)`, the root position (0, 0), and the clear walk of the section "The Layout Pass". Write `LayoutInternal` per `calculateLayoutInternal` with no cache. Put the comment `// Phase 9: the cache lookup goes here.` at the place of the Yoga lookup. It calls `CalculateLayoutImpl`, writes `MeasuredWidth` and `MeasuredHeight`, and writes the size of `ComputedRect` when `performLayout` is true.
  - File(s): `src/BrutalUi.Core/Layout/LayoutEngine.cs`
  - Estimate: 8 steps

- [x] ✅ **1.7 Write `CalculateLayoutImpl` (part 2, same file)**
  - Description: with the eleven step headers. Fill only: step 2 (the available inner size), step 3 (the flex basis is the definite main size, or the result of a measure-only `LayoutInternal` call for an auto child), step 4 (one line with all children), step 6 (the main-axis position from the flex-start edge, and the line cross size), step 7 (stretch for a child with an auto cross size, else flex-start), step 9 (the final size: definite, or the content size), and step 10 (for a reverse axis, convert the offset from the flex-start edge into X or Y: `parentSize - offset - childSize`, Decision 7). Each empty step header names the phase that fills it.
  - File(s): `src/BrutalUi.Core/Layout/LayoutEngine.cs`
  - Estimate: 10 steps

Group B (context, notice, tests, 6 files):

- [x] ✅ **1.8 Edit `Tree/UiContext.cs`**
  - Description: add `float2 ViewportSize { get; set; }` (default `(0, 0)`). The setter throws `ArgumentOutOfRangeException` (`value`) for a negative or infinite component, and accepts `NaN` (A19). Replace the layout seam comment with the layout loop of the section "The Layout Pass". Keep the style seam comment and the clear pass. Update the doc of `Update` and of the class (set the size before the first `Update` and after each resize).
  - File(s): `src/BrutalUi.Core/Tree/UiContext.cs`
  - Estimate: 5 steps

- [x] ✅ **1.9 Write `THIRD-PARTY-NOTICES.md` and pack it**
  - Description: at the repository root: one section for Yoga with the repository URL, the tag, the commit, the list of derived files (update it in each phase), and the full text of the Yoga `LICENSE` file at that tag. Edit `src/BrutalUi.Core/BrutalUi.Core.csproj`: add `<None Include="../../THIRD-PARTY-NOTICES.md" Pack="true" PackagePath="/" />` in an item group with a comment. Add no `PackageReference`.
  - File(s): `THIRD-PARTY-NOTICES.md`, `src/BrutalUi.Core/BrutalUi.Core.csproj`
  - Estimate: 5 steps

- [x] ✅ **1.10 Write `Layout/LayoutFixture.cs` (test helper)**
  - Description: `Calculate(Element root)` adds the root to a new `UiContext` with `ViewportSize = new float2(float.NaN, float.NaN)`, calls `Update(0f)`, and removes the root. This matches `YGNodeCalculateLayout(root, YGUndefined, YGUndefined, LTR)`. `AssertRect(Element element, float left, float top, float width, float height)` compares each value with a tolerance of 0.001. `AssertNoLayoutBits(Element root)` checks each element of the tree.
  - File(s): `tests/BrutalUi.Core.Tests/Layout/LayoutFixture.cs`
  - Estimate: 5 steps

- [x] ✅ **1.11 Write `FlexDirectionFixtureTests.cs`**
  - Description: port `flex_direction_column_no_height`, `flex_direction_row_no_width`, `flex_direction_column`, `flex_direction_row`, `flex_direction_column_reverse`, and `flex_direction_row_reverse` from `YGFlexDirectionTest`. Check each child and the root.
  - File(s): `tests/BrutalUi.Core.Tests/Layout/FlexDirectionFixtureTests.cs`
  - Estimate: 6 steps

- [x] ✅ **1.12 Write `LayoutPassTests.cs`**
  - Description: an auto root gets the size of `ViewportSize` after `Update`. A height change of the first child of a column, with `MarkDirty(DirtyFlags.Layout)`, moves the second child on the next `Update` (the parent holds only `DescendantLayout`, the CR-1 case). A leaf at depth 4 grows, and each auto-size ancestor grows. After each `Update`, `AssertNoLayoutBits` passes. A second `Update` with no change gives the same rects. A new `ViewportSize` changes the size of a clean auto root. A negative and an infinite component throw with the `ParamName` `value`.
  - File(s): `tests/BrutalUi.Core.Tests/Layout/LayoutPassTests.cs`
  - Estimate: 6 steps

- [x] ✅ **1.13 Run the build, the tests, and the demo**
  - Description: Run the build and the tests (phase rules). Run the demo. Record the results, the Yoga tag and commit, and the first fixture ledger rows in `context.md`.
  - File(s): `docs/features/core-tree/flexbox-layout/context.md`
  - Estimate: 4 steps

---

## Phase 2: The Box: Margin, Padding, and Border Width (8/8 complete)

**Demo:** the developer runs `dotnet run --project tests/BrutalUi.Core.Tests/BrutalUi.Core.Tests.csproj -- --filter-class '*MarginFixtureTests' --filter-class '*PaddingFixtureTests' --filter-class '*BorderFixtureTests'` and reads a total of 10 or more with `failed: 0`.
**Needs:** Phase 1

Group A (src, 4 files):

- [x] ✅ **2.1 Write `Layout/LengthEdges.cs`**
  - Description: per the Public API table, with a doc comment for each field.
  - File(s): `src/BrutalUi.Core/Layout/LengthEdges.cs`
  - Estimate: 4 steps

- [x] ✅ **2.2 Edit `Tree/ComputedStyle.cs`**
  - Description: add `Margin`, `Padding`, and `BorderWidth`. The docs: `Undefined` counts as 0. Padding and border accept no auto (auto counts as 0). The border accepts pixels only (a percent counts as 0). Margin auto comes in phase 6 (until then it counts as 0).
  - File(s): `src/BrutalUi.Core/Tree/ComputedStyle.cs`
  - Estimate: 3 steps

- [x] ✅ **2.3 Edit `Layout/LayoutMath.cs`**
  - Description: the leading and trailing margin, padding, and border for an axis, `PaddingAndBorderForAxis`, and `MarginForAxis`. A margin and a padding resolve a percent against the owner width on all four edges (the Yoga rule, used in phase 6). `BoundAxis` now applies the padding-and-border floor.
  - File(s): `src/BrutalUi.Core/Layout/LayoutMath.cs`
  - Estimate: 6 steps

- [x] ✅ **2.4 Edit `Layout/LayoutEngine.cs`**
  - Description: apply the box in step 2 (the inner size), step 3 (the basis floor), step 6 (the leading padding and border, and the child margins on the main axis), step 7 (the cross position and the stretch size less the margins), step 9 (the final size adds padding and border), and `Calculate` (the root position adds the root margin).
  - File(s): `src/BrutalUi.Core/Layout/LayoutEngine.cs`
  - Estimate: 8 steps

Group B (tests, 3 files):

- [x] ✅ **2.5 Write `MarginFixtureTests.cs`**
  - Description: port 4 or more pixel fixtures of `YGMarginTest` (for example `margin_top`, `margin_bottom`, `margin_and_stretch_row`, `margin_and_stretch_column`, `margin_with_sibling_row`, `margin_with_sibling_column`). Skip each start, end, and auto fixture for now.
  - File(s): `tests/BrutalUi.Core.Tests/Layout/MarginFixtureTests.cs`
  - Estimate: 6 steps

- [x] ✅ **2.6 Write `PaddingFixtureTests.cs`**
  - Description: port 3 or more fixtures of `YGPaddingTest` (for example `padding_no_size`, `padding_container_match_child`, `padding_stretch_child`).
  - File(s): `tests/BrutalUi.Core.Tests/Layout/PaddingFixtureTests.cs`
  - Estimate: 5 steps

- [x] ✅ **2.7 Write `BorderFixtureTests.cs`**
  - Description: port 3 or more fixtures of `YGBorderTest` (for example `border_no_size`, `border_container_match_child`, `border_stretch_child`).
  - File(s): `tests/BrutalUi.Core.Tests/Layout/BorderFixtureTests.cs`
  - Estimate: 5 steps

- [x] ✅ **2.8 Run the build, the tests, and the demo**
  - Description: Run the build, the tests, and the demo. Update the ledger in `context.md`.
  - File(s): `docs/features/core-tree/flexbox-layout/context.md`
  - Estimate: 3 steps

---

## Phase 3: Main-Axis Distribution: Grow, Shrink, Basis, Min and Max, Justify (12/12 complete)

**Demo:** the developer runs `dotnet run --project tests/BrutalUi.Core.Tests/BrutalUi.Core.Tests.csproj -- --filter-class '*FlexFixtureTests' --filter-class '*JustifyContentFixtureTests'` and reads a total of 18 or more with `failed: 0`.
**Needs:** Phase 2

Group A (src, 5 files):

- [x] ✅ **3.1 Edit `Layout/LayoutEnums.cs` and `Tree/ComputedStyle.cs`**
  - Description: add `Justify`, and the fields `FlexGrow`, `FlexShrink`, `FlexBasis`, `MinWidth`, `MinHeight`, `MaxWidth`, `MaxHeight`, and `JustifyContent`. The docs give the Yoga defaults (grow 0, shrink 0, basis auto, no min, no max, flex-start).
  - File(s): `src/BrutalUi.Core/Layout/LayoutEnums.cs`, `src/BrutalUi.Core/Tree/ComputedStyle.cs`
  - Estimate: 4 steps

- [x] ✅ **3.2 Edit `Layout/LayoutMath.cs`**
  - Description: `BoundAxisWithinMinAndMax` (the Yoga order: max, then min), and `BoundAxis` on top of it.
  - File(s): `src/BrutalUi.Core/Layout/LayoutMath.cs`
  - Estimate: 4 steps

- [x] ✅ **3.3 Write `Layout/LayoutEngine.Lines.cs` (the flex basis)**
  - Description: with the Yoga header: move step 3 out of `LayoutEngine.cs` into `ComputeFlexBasisForChildren` and `ComputeFlexBasisForChild`. The basis is the resolved `FlexBasis`, else the definite main size, else a measure-only `LayoutInternal` result. Always compute it again (Decision 1, the basis reuse of Yoga is dropped). Store it in `ComputedFlexBasis`.
  - File(s): `src/BrutalUi.Core/Layout/LayoutEngine.Lines.cs`, `src/BrutalUi.Core/Layout/LayoutEngine.cs`
  - Estimate: 8 steps

- [x] ✅ **3.4 Write `CalculateFlexLine` in `LayoutEngine.Lines.cs`**
  - Description: for one line: the start and end index, the item count, `SizeConsumed`, `TotalFlexGrowFactors`, and `TotalFlexShrinkScaledFactors`, in a small struct on the stack. Phase 7 adds the break.
  - File(s): `src/BrutalUi.Core/Layout/LayoutEngine.Lines.cs`
  - Estimate: 6 steps

- [x] ✅ **3.5 Write `ResolveFlexibleLength` in `LayoutEngine.Lines.cs`**
  - Description: with `DistributeFreeSpaceFirstPass` and `DistributeFreeSpaceSecondPass`: the flex-shrink scaled by the basis, the clamp to min and max, the frozen items of the first pass, and the second layout call of each child with the final main size.
  - File(s): `src/BrutalUi.Core/Layout/LayoutEngine.Lines.cs`
  - Estimate: 10 steps

- [x] ✅ **3.6 Write `JustifyMainAxis` in `LayoutEngine.Lines.cs`**
  - Description: the remaining free space, the leading and between space for each of the six values, and the line cross size.
  - File(s): `src/BrutalUi.Core/Layout/LayoutEngine.Lines.cs`
  - Estimate: 6 steps

- [x] ✅ **3.7 Edit `Layout/LayoutEngine.cs`**
  - Description: call steps 3 to 6 through the new functions, apply min and max in step 9, and bound the root size in `Calculate`.
  - File(s): `src/BrutalUi.Core/Layout/LayoutEngine.cs`
  - Estimate: 6 steps

Group B (tests, 4 files):

- [x] ✅ **3.8 Write `FlexFixtureTests.cs`**
  - Description: port 6 or more fixtures of `YGFlexTest` (for example `flex_basis_flex_grow_column`, `flex_basis_flex_grow_row`, `flex_basis_flex_shrink_column`, `flex_basis_flex_shrink_row`, `flex_shrink_to_zero`, `flex_basis_overrides_main_size`, `flex_grow_less_than_factor_one`).
  - File(s): `tests/BrutalUi.Core.Tests/Layout/FlexFixtureTests.cs`
  - Estimate: 6 steps

- [x] ✅ **3.9 Write `MinMaxDimensionFixtureTests.cs`**
  - Description: port 8 or more fixtures of `YGMinMaxDimensionTest` (for example `max_width`, `max_height`, `min_width`, `min_height`, `justify_content_min_max`, `flex_grow_to_min`, `flex_grow_within_max_width`, `min_width_overrides_max_width`). Skip the percent fixtures until phase 6.
  - File(s): `tests/BrutalUi.Core.Tests/Layout/MinMaxDimensionFixtureTests.cs`
  - Estimate: 6 steps

- [x] ✅ **3.10 Write `JustifyContentFixtureTests.cs`**
  - Description: port one fixture for each of the six values in a row and in a column (12 or more) from `YGJustifyContentTest`.
  - File(s): `tests/BrutalUi.Core.Tests/Layout/JustifyContentFixtureTests.cs`
  - Estimate: 6 steps

- [x] ✅ **3.11 Write `DimensionFixtureTests.cs`**
  - Description: port `wrap_child` and `wrap_grandchild` from `YGDimensionTest`.
  - File(s): `tests/BrutalUi.Core.Tests/Layout/DimensionFixtureTests.cs`
  - Estimate: 4 steps

- [x] ✅ **3.12 Run the build, the tests, and the demo**
  - Description: Run the build, the tests, and the demo. Update the ledger in `context.md`.
  - File(s): `docs/features/core-tree/flexbox-layout/context.md`
  - Estimate: 3 steps

---

## Phase 4: Cross-Axis Alignment: Align Items and Align Self (7/7 complete)

**Demo:** the developer runs `dotnet run --project tests/BrutalUi.Core.Tests/BrutalUi.Core.Tests.csproj -- --filter-class '*AlignItemsFixtureTests' --filter-class '*AlignSelfFixtureTests'` and reads a total of 10 or more with `failed: 0`.
**Needs:** Phase 3

- [x] ✅ **4.1 Edit `Layout/LayoutEnums.cs` and `Tree/ComputedStyle.cs`**
  - Description: add `Align` (no `Baseline`, A7), and the fields `AlignItems` and `AlignSelf`. The docs: `AlignItems` auto means stretch. `AlignSelf` auto means the `AlignItems` of the parent. The three space values act as flex-start on these two fields (Yoga).
  - File(s): `src/BrutalUi.Core/Layout/LayoutEnums.cs`, `src/BrutalUi.Core/Tree/ComputedStyle.cs`
  - Estimate: 4 steps

- [x] ✅ **4.2 Edit `Layout/LayoutMath.cs`**
  - Description: `ResolveChildAlignment(ref readonly ComputedStyle parent, ref readonly ComputedStyle child)` per `resolveChildAlignment` of Yoga, with the auto rules of task 4.1.
  - File(s): `src/BrutalUi.Core/Layout/LayoutMath.cs`
  - Estimate: 3 steps

- [x] ✅ **4.3 Edit `Layout/LayoutEngine.cs`, step 7**
  - Description: flex-start, center, and flex-end with the cross margins. Stretch: when the child has an auto cross size and no auto cross margin, run `LayoutInternal` again with `Exactly` on the cross axis and `performLayout` true, as Yoga does.
  - File(s): `src/BrutalUi.Core/Layout/LayoutEngine.cs`
  - Estimate: 8 steps

- [x] ✅ **4.4 Edit `Layout/LayoutEngine.cs`, steps 6 and 9**
  - Description: the cross size of a single line when the parent has a definite cross size (Yoga uses the available inner cross size), and the cross content size when it has none.
  - File(s): `src/BrutalUi.Core/Layout/LayoutEngine.cs`
  - Estimate: 6 steps

- [x] ✅ **4.5 Write `AlignItemsFixtureTests.cs`**
  - Description: port 6 or more fixtures of `YGAlignItemsTest` (for example `align_items_stretch`, `align_items_center`, `align_items_flex_start`, `align_items_flex_end`, `align_items_center_child_with_margin_bigger_than_parent`, `align_items_flex_end_child_with_margin_bigger_than_parent`). Skip each baseline fixture.
  - File(s): `tests/BrutalUi.Core.Tests/Layout/AlignItemsFixtureTests.cs`
  - Estimate: 6 steps

- [x] ✅ **4.6 Write `AlignSelfFixtureTests.cs`**
  - Description: port 4 or more fixtures of `YGAlignSelfTest` (for example `align_self_center`, `align_self_flex_end`, `align_self_flex_start`, `align_self_flex_end_override_flex_start`).
  - File(s): `tests/BrutalUi.Core.Tests/Layout/AlignSelfFixtureTests.cs`
  - Estimate: 5 steps

- [x] ✅ **4.7 Run the build, the tests, and the demo**
  - Description: Run the build, the tests, and the demo. Update the ledger in `context.md`.
  - File(s): `docs/features/core-tree/flexbox-layout/context.md`
  - Estimate: 3 steps

---

## Phase 5: Absolute Children, Offsets, and Display None (10/10 complete)

**Demo:** the developer runs `dotnet run --project tests/BrutalUi.Core.Tests/BrutalUi.Core.Tests.csproj -- --filter-class '*AbsolutePositionFixtureTests' --filter-class '*DisplayFixtureTests'` and reads a total of 13 or more with `failed: 0`.
**Needs:** Phase 4

Group A (src, 5 files):

- [x] ✅ **5.1 Edit `Layout/LayoutEnums.cs` and `Tree/ComputedStyle.cs`**
  - Description: add `Display` and `PositionType`, and the fields `Display`, `Position`, `Left`, `Top`, `Right`, `Bottom`. The docs: an offset moves a relative child from its flex position. On an absolute child, the offsets place it in the padding box of the parent. `Undefined` means "not set".
  - File(s): `src/BrutalUi.Core/Layout/LayoutEnums.cs`, `src/BrutalUi.Core/Tree/ComputedStyle.cs`
  - Estimate: 4 steps

- [x] ✅ **5.2 Edit `Layout/LayoutMath.cs`**
  - Description: `RelativePosition(axis)` per Yoga (the leading offset when it is set, else the negative trailing offset), and the leading and trailing offset helpers.
  - File(s): `src/BrutalUi.Core/Layout/LayoutMath.cs`
  - Estimate: 4 steps

- [x] ✅ **5.3 Write `Layout/LayoutEngine.Absolute.cs` (the size part)**
  - Description: with the Yoga header: `LayoutAbsoluteChild`, the size part. The width from the style, else from `Left` and `Right`. The height likewise. A measure-only `LayoutInternal` call for an undefined size, then the final layout with `Exactly`.
  - File(s): `src/BrutalUi.Core/Layout/LayoutEngine.Absolute.cs`
  - Estimate: 8 steps

- [x] ✅ **5.4 Write the position part in `LayoutEngine.Absolute.cs`**
  - Description: the position part: from the leading offset, else from the trailing offset, else from the `JustifyContent` and the alignment of the parent.
  - File(s): `src/BrutalUi.Core/Layout/LayoutEngine.Absolute.cs`
  - Estimate: 6 steps

- [x] ✅ **5.5 Edit `Layout/LayoutEngine.cs`**
  - Description: skip an absolute child and a `display: none` child in steps 3 to 8. Add the relative offsets in steps 6 and 7. Call step 11 when `performLayout` is true. Write `ZeroOutLayoutRecursively` per Yoga (the rect, the scratch, and the cache to zero). Run it on a `display: none` child only when the child holds a layout bit (Decision 9).
  - File(s): `src/BrutalUi.Core/Layout/LayoutEngine.cs`
  - Estimate: 8 steps

Group B (tests, 4 files):

- [x] ✅ **5.6 Write `AbsolutePositionFixtureTests.cs`**
  - Description: port 8 or more fixtures of `YGAbsolutePositionTest` that use `left`, `top`, `right`, or `bottom` and no start or end edge (for example `absolute_layout_start_top_end_bottom` only when it maps to left and right, `absolute_layout_within_border`, `absolute_layout_align_items_and_justify_content_center`, `absolute_layout_align_items_and_justify_content_flex_end`, `absolute_layout_align_items_and_justify_content_center_and_top_position`, `do_not_clamp_height_of_absolute_node_to_height_of_its_overflow_hidden_parent` after phase 8).
  - File(s): `tests/BrutalUi.Core.Tests/Layout/AbsolutePositionFixtureTests.cs`
  - Estimate: 6 steps

- [x] ✅ **5.7 Write `DisplayFixtureTests.cs`**
  - Description: port 5 or more fixtures of `YGDisplayTest` (for example `display_none`, `display_none_fixed_size`, `display_none_with_margin`, `display_none_with_child`, `display_none_with_position`, `display_none_with_position_absolute`).
  - File(s): `tests/BrutalUi.Core.Tests/Layout/DisplayFixtureTests.cs`
  - Estimate: 6 steps

- [x] ✅ **5.8 Write `RelativePositionFixtureTests.cs`**
  - Description: port 4 or more fixtures that set `left`, `top`, `right`, and `bottom` on a relative child (each offset one time or more). The header names each Yoga source file (Notes item 8).
  - File(s): `tests/BrutalUi.Core.Tests/Layout/RelativePositionFixtureTests.cs`
  - Estimate: 6 steps

- [x] ✅ **5.9 Edit `LayoutPassTests.cs`**
  - Description: a hidden subtree with a marked leaf holds no layout bit after `Update`. A hidden element that becomes visible again, with a mark, gets its full layout. A clean hidden subtree is not entered (a leaf rect that a test sets by hand through `ComputedRect` stays as it is).
  - File(s): `tests/BrutalUi.Core.Tests/Layout/LayoutPassTests.cs`
  - Estimate: 5 steps

- [x] ✅ **5.10 Run the build, the tests, and the demo**
  - Description: Run the build, the tests, and the demo. Update the ledger and the list of derived files in `THIRD-PARTY-NOTICES.md` and `context.md`.
  - File(s): `THIRD-PARTY-NOTICES.md`, `docs/features/core-tree/flexbox-layout/context.md`
  - Estimate: 3 steps

---

## Phase 6: Percent Lengths and Auto Margins (7/7 complete)

**Demo:** the developer runs `dotnet run --project tests/BrutalUi.Core.Tests/BrutalUi.Core.Tests.csproj -- --filter-class '*PercentageFixtureTests'` and reads a total of 10 or more with `failed: 0`.
**Needs:** Phase 5

- [x] ✅ **6.1 Audit each `Resolve` call**
  - Description: Audit each `Resolve` call in `Layout/LayoutMath.cs`, `Layout/LayoutEngine.cs`, and `Layout/LayoutEngine.Lines.cs`. The owner size is the owner width for a width, a min width, a max width, and each margin and padding edge. It is the owner height for a height, a min height, and a max height. It is the owner main size for the basis. A percent basis with an undefined owner main size acts as auto. Fix each wrong owner size.
  - File(s): `src/BrutalUi.Core/Layout/LayoutMath.cs`, `src/BrutalUi.Core/Layout/LayoutEngine.cs`, `src/BrutalUi.Core/Layout/LayoutEngine.Lines.cs`
  - Estimate: 8 steps

- [x] ✅ **6.2 Edit `Layout/LayoutEngine.Absolute.cs`**
  - Description: a percent size and a percent offset of an absolute child resolve against the padding box of the parent (the errata-free Yoga path).
  - File(s): `src/BrutalUi.Core/Layout/LayoutEngine.Absolute.cs`
  - Estimate: 5 steps

- [x] ✅ **6.3 Edit `Layout/LayoutEngine.Lines.cs`**
  - Description: auto margins on the main axis in `JustifyMainAxis` (count the auto margins of the line and give each one an equal part of the free space).
  - File(s): `src/BrutalUi.Core/Layout/LayoutEngine.Lines.cs`
  - Estimate: 6 steps

- [x] ✅ **6.4 Edit `Layout/LayoutEngine.cs`**
  - Description: auto margins on the cross axis in step 7. A child with an auto cross margin does not stretch. Update the doc of `ComputedStyle.Margin` (auto now works).
  - File(s): `src/BrutalUi.Core/Layout/LayoutEngine.cs`, `src/BrutalUi.Core/Tree/ComputedStyle.cs`
  - Estimate: 6 steps

- [x] ✅ **6.5 Write `PercentageFixtureTests.cs`**
  - Description: port 10 or more fixtures of `YGPercentageTest` (for example `percentage_width_height`, `percentage_position_left_top`, `percentage_position_bottom_right`, `percentage_flex_basis`, `percentage_flex_basis_cross`, `percentage_margin_should_calculate_based_only_on_width`, `percentage_padding_should_calculate_based_only_on_width`, `percentage_absolute_position`, `percentage_width_height_undefined_parent_size`, `percent_within_flex_grow`). Cover a min or a max percent one time or more.
  - File(s): `tests/BrutalUi.Core.Tests/Layout/PercentageFixtureTests.cs`
  - Estimate: 8 steps

- [x] ✅ **6.6 Edit `MarginFixtureTests.cs`**
  - Description: port 5 or more auto fixtures of `YGMarginTest` (for example `margin_auto_bottom`, `margin_auto_top`, `margin_auto_bottom_and_top`, `margin_auto_left_and_right_column`, `margin_auto_left`, `margin_auto_right`).
  - File(s): `tests/BrutalUi.Core.Tests/Layout/MarginFixtureTests.cs`
  - Estimate: 6 steps

- [x] ✅ **6.7 Run the build, the tests, and the demo**
  - Description: Run the build, the tests, and the demo. Update the ledger in `context.md`.
  - File(s): `docs/features/core-tree/flexbox-layout/context.md`
  - Estimate: 3 steps

---

## Phase 7: Line Break: Flex Wrap and Align Content (9/9 complete)

**Demo:** the developer runs `dotnet run --project tests/BrutalUi.Core.Tests/BrutalUi.Core.Tests.csproj -- --filter-class '*FlexWrapFixtureTests' --filter-class '*AlignContentFixtureTests'` and reads a total of 16 or more with `failed: 0`.
**Needs:** Phase 4

Group A (src, 5 files):

- [x] ✅ **7.1 Edit `Layout/LayoutEnums.cs` and `Tree/ComputedStyle.cs`**
  - Description: add `FlexWrap`, and the fields `FlexWrap` and `AlignContent`. The doc of `AlignContent`: auto means flex-start (the Yoga default), and `AlignContent` applies to a wrapped container only.
  - File(s): `src/BrutalUi.Core/Layout/LayoutEnums.cs`, `src/BrutalUi.Core/Tree/ComputedStyle.cs`
  - Estimate: 4 steps

- [x] ✅ **7.2 Edit `Tree/LayoutNode.cs`**
  - Description: add the internal `int LineIndex`.
  - File(s): `src/BrutalUi.Core/Tree/LayoutNode.cs`
  - Estimate: 2 steps

- [x] ✅ **7.3 Edit `Layout/LayoutEngine.Lines.cs`**
  - Description: the break in `CalculateFlexLine`. It returns the end index of the line, and the caller starts the next line at that index.
  - File(s): `src/BrutalUi.Core/Layout/LayoutEngine.Lines.cs`
  - Estimate: 6 steps

- [x] ✅ **7.4 Edit `Layout/LayoutEngine.cs` (the line loop)**
  - Description: the loop of steps 4 to 7 for each line, the cross size of each line (the largest child cross size with margins), and the total cross size for step 9.
  - File(s): `src/BrutalUi.Core/Layout/LayoutEngine.cs`
  - Estimate: 8 steps

- [x] ✅ **7.5 Edit `Layout/LayoutEngine.cs`, step 8**
  - Description: `align-content` for flex-start, center, flex-end, stretch, space-between, space-around, and space-evenly. With stretch, a child with stretch alignment gets a second exact layout with the line cross size, as Yoga does.
  - File(s): `src/BrutalUi.Core/Layout/LayoutEngine.cs`
  - Estimate: 8 steps

- [x] ✅ **7.6 Edit `Layout/LayoutEngine.cs` (wrap-reverse)**
  - Description: `wrap-reverse` flips the cross position of each child against the cross size of the element (the Yoga step after step 9).
  - File(s): `src/BrutalUi.Core/Layout/LayoutEngine.cs`
  - Estimate: 4 steps

Group B (tests, 2 files):

- [x] ✅ **7.7 Write `FlexWrapFixtureTests.cs`**
  - Description: port 8 or more fixtures of `YGFlexWrapTest`, with wrap and wrap-reverse in a row and in a column (for example `wrap_column`, `wrap_row`, `wrap_row_align_items_flex_end`, `wrap_row_align_items_center`, `flex_wrap_children_with_min_main_overriding_flex_basis`, `flex_wrap_wrap_to_child_height`, `wrap_reverse_row_align_content_flex_start`, `wrap_reverse_column_fixed_size`).
  - File(s): `tests/BrutalUi.Core.Tests/Layout/FlexWrapFixtureTests.cs`
  - Estimate: 8 steps

- [x] ✅ **7.8 Write `AlignContentFixtureTests.cs`**
  - Description: port one fixture or more for each of the seven values (8 or more) from `YGAlignContentTest` (for example `align_content_flex_start`, `align_content_flex_end`, `align_content_center`, `align_content_stretch`, `align_content_spacebetween`, `align_content_spacearound`, `align_content_stretch_row`).
  - File(s): `tests/BrutalUi.Core.Tests/Layout/AlignContentFixtureTests.cs`
  - Estimate: 8 steps

- [x] ✅ **7.9 Run the build, the tests, and the demo**
  - Description: Run the build, the tests, and the demo. Update the ledger in `context.md`.
  - File(s): `docs/features/core-tree/flexbox-layout/context.md`
  - Estimate: 3 steps

---

## Phase 8: The Measure Callback Slot and Overflow (9/9 complete)

**Demo:** the developer runs `dotnet run --project tests/BrutalUi.Core.Tests/BrutalUi.Core.Tests.csproj -- --filter-class '*MeasureFixtureTests'` and reads a total of 6 or more with `failed: 0`.
**Needs:** Phase 3

Group A (src, 6 files):

- [x] ✅ **8.1 Write `Layout/MeasureCallback.cs`**
  - Description: the public delegate of the Public API table. The doc gives the rules of A10 and A11: the engine calls it for an element with no children only. It gets the inner size (the available size less margin, padding, and border) and the two modes. It must not change the tree, mark a bit, or allocate. Assign a `static` lambda or a method group one time (for example in a constructor), not in the frame loop.
  - File(s): `src/BrutalUi.Core/Layout/MeasureCallback.cs`
  - Estimate: 3 steps

- [x] ✅ **8.2 Edit `Tree/LayoutNode.cs`**
  - Description: add `public MeasureCallback? Measure { readonly get; set; }`. The doc: a set marks nothing, so call `MarkDirty(DirtyFlags.Layout)`.
  - File(s): `src/BrutalUi.Core/Tree/LayoutNode.cs`
  - Estimate: 2 steps

- [x] ✅ **8.3 Edit `Layout/LayoutEngine.cs` (the measure functions)**
  - Description: `MeasureNodeWithMeasureFunc`, `MeasureNodeWithoutChildren`, and `MeasureNodeWithFixedSize` per Yoga, called at the top of `CalculateLayoutImpl` in the Yoga order. A callback on an element with children has no effect.
  - File(s): `src/BrutalUi.Core/Layout/LayoutEngine.cs`
  - Estimate: 8 steps

- [x] ✅ **8.4 Edit `Layout/LayoutEnums.cs` and `Tree/ComputedStyle.cs`**
  - Description: add `Overflow` and the field `Overflow`. The doc: hidden and visible give the same layout (A9). Scroll changes the measure constraints of the children and the final size. The renderer epic clips.
  - File(s): `src/BrutalUi.Core/Layout/LayoutEnums.cs`, `src/BrutalUi.Core/Tree/ComputedStyle.cs`
  - Estimate: 3 steps

- [x] ✅ **8.5 Edit `Layout/LayoutEngine.Lines.cs` and `Layout/LayoutEngine.cs` (scroll)**
  - Description: the scroll rules of `computeFlexBasisForChild` (the `AtMost` constraint of a child on the axes of Yoga) and of step 9 (the final size of a scroll container with an `AtMost` mode).
  - File(s): `src/BrutalUi.Core/Layout/LayoutEngine.Lines.cs`, `src/BrutalUi.Core/Layout/LayoutEngine.cs`
  - Estimate: 6 steps

Group B (tests, 3 files):

- [x] ✅ **8.6 Write `MeasureStub.cs` (test code)**
  - Description: a class with a call counter and a set of static measure functions (a fixed size, the available width with a fixed height, and the Yoga test functions that task 8.7 needs). A test creates the delegate one time in its setup.
  - File(s): `tests/BrutalUi.Core.Tests/Layout/MeasureStub.cs`
  - Estimate: 4 steps

- [x] ✅ **8.7 Write `MeasureFixtureTests.cs`**
  - Description: port 6 or more tests of the hand-written `YGMeasureTest.cpp` of Yoga with the stub (Notes item 8, for example `dont_measure_single_grow_shrink_child`, `measure_absolute_child_with_no_constraints`, `dont_measure_when_min_equals_max`, `measure_flex_direction_row_and_padding`, `measure_enough_size_should_be_in_single_line`, `measure_zero_space_should_grow`). Add one test: a callback on an element with children is not called.
  - File(s): `tests/BrutalUi.Core.Tests/Layout/MeasureFixtureTests.cs`
  - Estimate: 8 steps

- [x] ✅ **8.8 Write `SizeOverflowFixtureTests.cs`**
  - Description: port the fixtures of `YGSizeOverflowTest` (for example `nested_overflowing_child`, `nested_overflowing_child_in_constraint_parent`, `parent_wrap_child_size_overflowing_parent`), and 2 or more fixtures that set `overflow: scroll` (search the fixture HTML files for `overflow: scroll`; when none fits, write a test from the Yoga rule and say so in the header).
  - File(s): `tests/BrutalUi.Core.Tests/Layout/SizeOverflowFixtureTests.cs`
  - Estimate: 6 steps

- [x] ✅ **8.9 Run the build, the tests, and the demo**
  - Description: Run the build, the tests, and the demo. Port the overflow fixture of task 5.6 now, if it waits. Update the ledger in `context.md`.
  - File(s): `docs/features/core-tree/flexbox-layout/context.md`, `tests/BrutalUi.Core.Tests/Layout/AbsolutePositionFixtureTests.cs`
  - Estimate: 4 steps

---

## Phase 9: The Layout Cache: Dirty Subtrees Only (8/8 complete)

**Demo:** the developer runs `dotnet run --project tests/BrutalUi.Core.Tests/BrutalUi.Core.Tests.csproj -- --filter-class '*LayoutCacheTests'` and reads `failed: 0`, with the test that runs a second `Update` with no change and counts 0 measure calls.
**Needs:** Phase 8

Group A (src, 3 files):

- [x] ✅ **9.1 Write `Layout/LayoutCache.cs`**
  - Description: with the Yoga header: `CachedMeasurement` (the available width and height, the two modes, the computed width and height), `CachedMeasurementBuffer` (`[InlineArray(8)]`), and `LayoutCache` (one layout entry, the buffer, the next index). Port `canUseCachedMeasurement` of Yoga `Cache.cpp` (the exact match, the "old size is max content and still fits" rule, and the "new size is stricter and still valid" rule) with no pixel rounding (Q3).
  - File(s): `src/BrutalUi.Core/Layout/LayoutCache.cs`
  - Estimate: 8 steps

- [x] ✅ **9.2 Edit `Tree/LayoutNode.cs`**
  - Description: add the internal `long Generation` and `LayoutCache Cache`.
  - File(s): `src/BrutalUi.Core/Tree/LayoutNode.cs`
  - Estimate: 2 steps

- [x] ✅ **9.3 Edit `Layout/LayoutEngine.cs`, `LayoutInternal`**
  - Description: the dirty test and the reset on the first visit of a generation (steps 2 and 3 of the rule), the lookup in the Yoga order (an element with a callback uses `CanUseCachedMeasurement` with its margins; a layout call uses the layout entry; a measure call uses the buffer), the hit path (set the measured size, do not enter the subtree), and the store (a layout call writes the layout entry; a measure call writes the buffer and wraps the index at 8). Remove the phase 9 comment.
  - File(s): `src/BrutalUi.Core/Layout/LayoutEngine.cs`
  - Estimate: 10 steps

- [x] ✅ **9.4 Check the zero-out and the root lookup**
  - Description: Edit `Layout/LayoutEngine.cs`: check that `ZeroOutLayoutRecursively` resets the cache, and that `Calculate` reads the root through the same lookup (a clean root with the same viewport returns at once).
  - File(s): `src/BrutalUi.Core/Layout/LayoutEngine.cs`
  - Estimate: 4 steps

Group B (tests, 3 files):

- [x] ✅ **9.5 Write `MeasureCacheFixtureTests.cs`**
  - Description: port 4 or more tests of the hand-written `YGMeasureCacheTest.cpp` of Yoga with the counting stub (Notes item 8, for example `measure_once_single_flexible_child`, `remeasure_with_same_exact_width_larger_than_needed_height`, `remeasure_with_same_atmost_width_larger_than_needed_height`, `remeasure_with_computed_width_larger_than_needed_height`, `remeasure_with_atmost_computed_width_undefined_height`).
  - File(s): `tests/BrutalUi.Core.Tests/Layout/MeasureCacheFixtureTests.cs`
  - Estimate: 6 steps

- [x] ✅ **9.6 Write `LayoutCacheTests.cs`**
  - Description: with the counting stub on each leaf: a second `Update` with no change calls no callback. A mark on one leaf calls the callback of that leaf only, and not of a clean sibling with the same constraints. A grow of one child calls the callback of a sibling whose free space changes. A new `ViewportSize` calls the callbacks of the subtrees with new constraints only. `AssertNoLayoutBits` passes after each `Update`.
  - File(s): `tests/BrutalUi.Core.Tests/Layout/LayoutCacheTests.cs`
  - Estimate: 6 steps

- [x] ✅ **9.7 Write `LayoutDifferentialTests.cs`**
  - Description: one seeded test (seed in the test name) builds 200 random trees of 20 to 40 elements with random supported fields (each enum value, pixel and percent lengths, grow, shrink, wrap, absolute, hidden elements, stub leaves). For each tree: lay out, then make 20 random changes (one field and a `MarkDirty`, or a child add, remove, or move), each followed by `Update`. After each `Update`, build a fresh copy of the tree (copy `Style` and `Measure` of each element), lay it out, and compare each rect with a tolerance of 0.001. On a difference, the message names the seed, the tree, the change, and the element path.
  - File(s): `tests/BrutalUi.Core.Tests/Layout/LayoutDifferentialTests.cs`
  - Estimate: 10 steps

- [x] ✅ **9.8 Run the build, the tests, and the demo**
  - Description: Run the build, the tests, and the demo. Run the differential test 3 times. Record the results in `context.md`.
  - File(s): `docs/features/core-tree/flexbox-layout/context.md`
  - Estimate: 4 steps

---

## Phase 10: The Allocation Check and the Design Doc (5/5 complete)

**Demo:** the developer runs `dotnet run --project tests/BrutalUi.Core.Tests/BrutalUi.Core.Tests.csproj -- --filter-class '*LayoutAllocationTests'` and reads `failed: 0`.
**Needs:** Phase 9

- [x] ✅ **10.1 Write `LayoutAllocationTests.cs` (A18)**
  - Description: build a tree of about 60 elements with a row, a column, a wrapped row, grow, an absolute child, percent sizes, a hidden element, and two stub leaves. Create each delegate before the warm-up. Set `ViewportSize`, and run `Update(0.016f)` two times. Measure 100 steps with `GC.GetAllocatedBytesForCurrentThread()`. One step changes the width of one leaf (two values in turn), calls `MarkDirty(DirtyFlags.Layout)`, and calls `Update`. Assert `0L` outside the measured region. Keep the test synchronous. Add a second test that checks that the body changes a rect.
  - File(s): `tests/BrutalUi.Core.Tests/Layout/LayoutAllocationTests.cs`
  - Estimate: 6 steps

- [x] ✅ **10.2 Edit `docs/technical-design.md`, sections 5 and 6**
  - Description: Section 6: the inputs on `ComputedStyle`, the output `LayoutNode.Rect` (relative to the parent), the units, the Yoga defaults, `ViewportSize`, the dirty-bit rule and the cache, and the list of Yoga parts that the port drops (Decision 1). Section 5: add `ViewportSize` to the shape of `UiContext`, and change the comment of `Layout` to "the output rect, the measure callback, and the layout cache". Use ASD-STE100 prose.
  - File(s): `docs/technical-design.md`
  - Estimate: 6 steps

- [x] ✅ **10.3 Check the notice**
  - Description: each Yoga-derived file of the file tree has the header, and `THIRD-PARTY-NOTICES.md` lists each one. Fix a missing item.
  - File(s): `THIRD-PARTY-NOTICES.md`
  - Estimate: 3 steps

- [x] ✅ **10.4 Check the source rules**
  - Description: run the greps of Verification steps 6 and 7, and fix each real hit.
  - File(s): `src/BrutalUi.Core/Layout/*.cs` (only on a real hit)
  - Estimate: 3 steps

- [x] ✅ **10.5 Run the Verification Method**
  - Description: Run each non-MANUAL step of the Verification Method, and `scripts/test-linux.sh`. Add the Windows run (Verification step 13) to "Needs Human Verification" in `context.md`.
  - File(s): `docs/features/core-tree/flexbox-layout/context.md`
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

## Notes

### Task Estimation
One step is one tool call of an agent. A typical task costs about six steps (see `.beast-mode/phase-handoff.md` section 1).
- 🟢 Small (up to 6 steps)
- 🟡 Medium (7 to 15 steps)
- 🔴 Large (more than 15 steps, so split the task)

---

*Update this file as tasks are completed. Mark tasks with ✅ immediately when done. Add new tasks as they're discovered.*
