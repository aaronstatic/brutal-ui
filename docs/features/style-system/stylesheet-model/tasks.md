# style-system/stylesheet-model - Task Checklist

**Last Updated:** 2026-10-21 17:34
**Progress:** 112/112 tasks complete (100%)
**Advanced phases:** Phases 1 to 8 and 11 (ADVANCED, opus). Phases 9 and 10 are STANDARD (sonnet). Group B of phase 11 is plain documentation work.

Source paths: `Core/` means `src/BrutalUi.Core/`. Test paths: `tests/Core/` means `tests/BrutalUi.Core.Tests/`, and `tests/ImGui/` means `tests/BrutalUi.ImGui.Tests/`. "The plan" means `implementation.md` of this feature. Its sections "The Public API", "The Style Pass", "The Transitional Gates", and "The Migration Recipe" hold the rules that the tasks name. Each phase ends with `dotnet build brutal-ui.sln 2>&1 | tail -n 30` and `dotnet test --solution brutal-ui.sln 2>&1 | tail -n 30` (tool rule 5). Expect `0 Warning(s)`, `0 Error(s)`, and 0 failed. A demo uses `dotnet run --project <test project> -- --filter-method '*Name' --show-live-output on`. A test prints with `TestContext.Current.TestOutputHelper?.WriteLine(...)`. The xUnit analyzers are errors. The projects have no implicit usings, so each new file holds its own `using` lines outside the namespace.

---

## Phase 1: A Class Rule Paints a Panel (12/12 complete)

**Demo:** the developer runs `dotnet run --project tests/BrutalUi.Core.Tests/BrutalUi.Core.Tests.csproj -- --filter-method '*PaintsAPanelFromAClassRule' --show-live-output on` and reads `panel BackgroundColor #1E222CFF` and `plain BackgroundColor #00000000`.
**Needs:** none
**Agent tier:** ADVANCED (opus)

**Group A (the model, 6 files):**

- [x] ✅ **1.1 Record the baseline and read the code** - Completed 2026-10-21
  - Description: Record in `context.md`: the test total of the solution, and the `bytes per element` line of `dotnet run -c Release --project tests/BrutalUi.Benchmarks/BrutalUi.Benchmarks.csproj -- --facts 2>&1 | tail -n 30`. Read `.editorconfig` (field names, types in one file), `ClassName.cs`, `ClassSet.cs`, `ClassList.cs`, and `DirtyWalk.cs`.
  - File(s): `context.md`
  - Estimate: 6 steps

- [x] ✅ **1.2 Write StyleProperties.cs** - Completed 2026-10-21
  - Description: Write `src/BrutalUi.Core/Style/StyleProperties.cs`: the `StyleProperty` enum with the 29 layout and paint members of "The Public API", an internal static `StyleMasks` class with `Layout` and `Paint` (each a `ulong`), and the `StyleProperties` struct. The struct holds one private field for each member, a `ulong` mask, `IsSet`, `IsEmpty`, `Clear`, the internal `SetMask`, and the internal `ApplyTo`. `ApplyTo` holds one `if` for each member. The remarks state the bit rule and that an unset getter gives the `ComputedStyle` default.
  - File(s): `src/BrutalUi.Core/Style/StyleProperties.cs`
  - Estimate: 6 steps

- [x] ✅ **1.3 Write Selector.cs** - Completed 2026-10-21
  - Description: Write `src/BrutalUi.Core/Style/Selector.cs`: `ForClass`, `Class`, `Specificity` (10), and the internal `Matches` (`element.ClassStorage.Contains(Class)`). `ForClass(ClassId.None)` throws `ArgumentException`. `default(Selector)` matches nothing. The remarks state the slot for selector-matching: the name, the pseudo-states, and the parent chain join this struct later, and `Specificity` is the one place of the weights.
  - File(s): `src/BrutalUi.Core/Style/Selector.cs`
  - Estimate: 6 steps

- [x] ✅ **1.4 Write StyleRule.cs** - Completed 2026-10-21
  - Description: Write `src/BrutalUi.Core/Style/StyleRule.cs` per "The Public API". The remarks state that style-parser adds the transition member later.
  - File(s): `src/BrutalUi.Core/Style/StyleRule.cs`
  - Estimate: 6 steps

- [x] ✅ **1.5 Write StyleSheet.cs and its Builder** - Completed 2026-10-21
  - Description: Write `src/BrutalUi.Core/Style/StyleSheet.cs`: the sheet and its `Builder`. `Build` copies the rules and sorts them one time with the unique key `(Specificity, index of Add)`. `Add` throws `ArgumentException` for `default(Selector)`. The sheet exposes the rules as a span and has no write member.
  - File(s): `src/BrutalUi.Core/Style/StyleSheet.cs`
  - Estimate: 6 steps

- [x] ✅ **1.6 Write the StyleProperties tests** - Completed 2026-10-21
  - Description: Write `tests/BrutalUi.Core.Tests/Style/StylePropertiesTests.cs`:
    - `ApplyToWritesEachMember`: set each of the 29 members to a value that is not the default, apply to `default(ComputedStyle)`, and assert each field.
    - `ApplyToSkipsTheMaskedMembers`, `AnUnsetGetterGivesTheDefault` (Opacity gives 1), `ClearRemovesTheBitAndTheValue`, and `IsEmptyForANewSet`.
    - `EachMemberHasOneBitInOneMask`: each enum value is below 64, and its bit is in `Layout` or `Paint` and not in both.
    - `TheEnumCoversEachComputedStyleMember`: count the public instance fields of `ComputedStyle` plus the `Opacity` property with reflection in the test, and compare with the count of `StyleProperty`. The test lists the field names on a failure.
  - File(s): `tests/BrutalUi.Core.Tests/Style/StylePropertiesTests.cs`
  - Estimate: 6 steps

- [x] ✅ **1.7 Write the StyleSheet tests** - Completed 2026-10-21
  - Description: Write `tests/BrutalUi.Core.Tests/Style/StyleSheetTests.cs`: the rules keep the order of `Add` at equal specificity, `Add` throws for `default(Selector)`, two `Build` calls give two sheets with equal rules, and an `Add` after `Build` does not change the built sheet.
  - File(s): `tests/BrutalUi.Core.Tests/Style/StyleSheetTests.cs`
  - Estimate: 6 steps

**Group B (the pass, 4 files):**

- [x] ✅ **1.8 Add the style storage and the subtree mark to Element** - Completed 2026-10-21
  - Description: Edit `src/BrutalUi.Core/Tree/Element.cs`: add `internal ref ComputedStyle StyleStorage` and `internal void MarkSubtreeStyle()` (recursion: `Style` on each element, `DescendantStyle` on each element with children, then `PropagateToAncestors` from the start element). Keep `public ref ComputedStyle Style` until phase 6.
  - File(s): `src/BrutalUi.Core/Tree/Element.cs`
  - Estimate: 6 steps

- [x] ✅ **1.9 Write StylePass.cs** - Completed 2026-10-21
  - Description: Write `src/BrutalUi.Core/Style/StylePass.cs`: an internal static class with `Run(ReadOnlySpan<Element> roots, ReadOnlySpan<StyleRule> cascade)`, `Visit`, `Restyle`, and `Compare` per "The Style Pass", with no inline set and no text members yet. `Restyle` has `[MethodImpl(MethodImplOptions.NoInlining)]`.
  - File(s): `src/BrutalUi.Core/Style/StylePass.cs`
  - Estimate: 6 steps

- [x] ✅ **1.10 Add AddSheet, UpdateStyles, and gate A to UiContext** - Completed 2026-10-21
  - Description: Edit `src/BrutalUi.Core/Tree/UiContext.cs`: add `_sheets`, `_cascade`, `AddSheet` (the null check, the duplicate check, the cascade rebuild of "The Style Pass", and `MarkSubtreeStyle` on each root), and `internal void UpdateStyles()` with gate A. Call `UpdateStyles()` at the seam. The doc comment of `AddSheet` states that the call allocates and belongs in load code.
  - File(s): `src/BrutalUi.Core/Tree/UiContext.cs`
  - Estimate: 6 steps

- [x] ✅ **1.11 Write the style pass tests** - Completed 2026-10-21
  - Description: Write `tests/BrutalUi.Core.Tests/Style/StylePassTests.cs`:
    - `PaintsAPanelFromAClassRule` (the demo): a sheet with `.panel { BackgroundColor = (30, 34, 44, 255) }`. A root with two children: `panel` (class `panel`) and `plain`. `Update`, then print and assert both lines.
    - The dirty bits, read after `UpdateStyles()`: a new paint value gives `Paint` and no `Layout`. A new `Width` gives `Layout`. A second restyle with the same result (`MarkDirty(Style)`, then `UpdateStyles()`) gives no `Layout` and no `Paint`. The pass clears `Style` and `DescendantStyle` on each element.
    - `ALaterRuleWinsAtEqualSpecificity`: rules `.a { BackgroundColor = red; Width = 10 }` and `.b { BackgroundColor = blue }` on one element give blue and a width of 10.
    - `SkipsAnElementWithNoStyleBit`: after `Update`, add a class through `ClassStorage` (no mark), run `UpdateStyles()`, and the style stays the same.
    - `AddSheetAfterTheFirstUpdateRestylesEachElement` (A8), `AddSheetThrowsForNull`, and `AddSheetThrowsForTheSameSheetTwice`.
    - `TheStylePassAllocatesNothingAfterWarmUp`: a tree of 50 elements and a sheet of 4 rules. Each step toggles one class and calls `Update`. Run 16 warm-up steps, call `AllocationGate.ReleaseAllocationContext()`, measure 100 steps, and assert 0 bytes.
  - File(s): `tests/BrutalUi.Core.Tests/Style/StylePassTests.cs`
  - Estimate: 6 steps

- [x] ✅ **1.12 Run the build, the tests, and the demo** - Completed 2026-10-21
  - Description: Run the build, the tests, and the demo. Record the printed lines and the test total in `context.md`.
  - File(s): `context.md`
  - Estimate: 6 steps

---

## Phase 2: An Inline Value Wins Over a Rule (7/7 complete)

**Demo:** the developer runs `dotnet run --project tests/BrutalUi.Core.Tests/BrutalUi.Core.Tests.csproj -- --filter-method '*AnInlineValueWinsOverARule' --show-live-output on` and reads `BackgroundColor #3060C0FF width 200`, then `BackgroundColor #1E222CFF width 200` after the clear.
**Needs:** Phase 1
**Agent tier:** ADVANCED (opus)

**Group A (the inline set, 5 files):**

- [x] ✅ **2.1 Write InlineStyle.cs** - Completed 2026-10-21
  - Description: Write `src/BrutalUi.Core/Style/InlineStyle.cs`: a `readonly struct` view with an `Element` owner, in the form of `ClassList`. One get and set pair for each `StyleProperty` member. A setter returns at once when the bit is set and the value is equal. Otherwise it writes `_owner.InlineStorage` and calls `_owner.MarkDirty(DirtyFlags.Style)`. Add `IsSet` and `Clear`. The remarks state the nested-write gotcha and that a default instance throws.
  - File(s): `src/BrutalUi.Core/Style/InlineStyle.cs`
  - Estimate: 6 steps

- [x] ✅ **2.2 Add the inline set to Element** - Completed 2026-10-21
  - Description: Edit `Element.cs`: add the `_inline` field, `public InlineStyle Inline`, and `internal ref StyleProperties InlineStorage`. The class remarks state that the inline set lives in the element (A1).
  - File(s): `src/BrutalUi.Core/Tree/Element.cs`
  - Estimate: 6 steps

- [x] ✅ **2.3 Apply the inline set in the style pass and add gate B** - Completed 2026-10-21
  - Description: Edit `StylePass.cs`: apply the rules with `skip: inline`, then apply the inline set. Add gate B in `Visit`. Its comment names phase 6 as the end of the gate.
  - File(s): `src/BrutalUi.Core/Style/StylePass.cs`
  - Estimate: 6 steps

- [x] ✅ **2.4 Remove gate A from UiContext** - Completed 2026-10-21
  - Description: Edit `UiContext.cs`: remove gate A. `UpdateStyles` runs in each `Update`.
  - File(s): `src/BrutalUi.Core/Tree/UiContext.cs`
  - Estimate: 6 steps

- [x] ✅ **2.5 Write the inline style tests** - Completed 2026-10-21
  - Description: Write `tests/BrutalUi.Core.Tests/Style/InlineStyleTests.cs`:
    - `AnInlineValueWinsOverARule` (the demo): a sheet with `.panel { BackgroundColor = (30, 34, 44, 255); Width = 200 }`, and `panel.Inline.BackgroundColor = (48, 96, 192, 255)`. Print after `Update`. Then `panel.Inline.Clear(StyleProperty.BackgroundColor)`, `Update`, and print again. Assert both lines.
    - `ASetMarksStyleAndTheAncestors`, `ASetToTheSameValueMarksNothing`, `AClearOfAnUnsetMemberMarksNothing`, `AnInlineValueReachesTheStyleWithNoSheet`, `AnInlineValueWinsOverEachRuleOrder`, and `ADefaultViewThrows`.
    - `AnInlineLayoutChangeSetsLayoutAfterThePass`: after `UpdateStyles()`, the element holds `Layout`.
  - File(s): `tests/BrutalUi.Core.Tests/Style/InlineStyleTests.cs`
  - Estimate: 6 steps

**Group B (the sample, 1 file):**

- [x] ✅ **2.6 Move the HelloPanel writes to Inline** - Completed 2026-10-21
  - Description: Edit `samples/HelloPanel/HelloScreen.cs`: move the 10 writes to `Inline`. The nested `Margin.Top` write becomes a whole write. The label keeps its create-info until phase 8.
  - File(s): `samples/HelloPanel/HelloScreen.cs`
  - Estimate: 6 steps

- [x] ✅ **2.7 Run the checks and record the memory growth** - Completed 2026-10-21
  - Description: Run the build, the tests, the demo, and `scripts/test-linux.sh; echo "exit=$?"` (expect the PASS line and `exit=0`). Run the facts line of task 1.1 again, and record the growth of `bytes per element` in `context.md`.
  - File(s): `context.md`
  - Estimate: 6 steps

---

## Phase 3 (Base): Migrate the Shared Trees, the Allocation Gates, and the Render Tests (15/15 complete)

**Demo:** none (base phase: a pure refactor of test code to the inline API)
**Needs:** Phase 2
**Agent tier:** ADVANCED (opus)

**Group A (the shared trees and the gates, 6 files):**

- [x] ✅ **3.1 Record the baseline count of direct style writes** - Completed 2026-10-21
  - Description: Record in `context.md` the count of `grep -rnE '\.Style\.[A-Za-z]+(\.[A-Za-z]+)?\s*=[^=]' tests samples --include=*.cs | wc -l` (expected: about 3770).
  - File(s): `context.md`
  - Estimate: 4 steps

- [x] ✅ **3.2 Migrate ThousandElementTree.cs** - Completed 2026-10-21
  - Description: Apply the recipe to `tests/BrutalUi.Core.Tests/Shared/ThousandElementTree.cs` (124 writes). Use the public API only, because the linked projects have no internal access.
  - File(s): `tests/BrutalUi.Core.Tests/Shared/ThousandElementTree.cs`
  - Estimate: 4 steps

- [x] ✅ **3.3 Migrate ChainTree.cs** - Completed 2026-10-21
  - Description: Apply the recipe to `tests/BrutalUi.Core.Tests/Shared/ChainTree.cs`.
  - File(s): `tests/BrutalUi.Core.Tests/Shared/ChainTree.cs`
  - Estimate: 4 steps

- [x] ✅ **3.4 Migrate ThousandElementAllocationTests.cs** - Completed 2026-10-21
  - Description: Apply the recipe to `tests/BrutalUi.Core.Tests/ThousandElementAllocationTests.cs`. `TheTreeHoldsTheRequiredMix` reads the style before the first `Update`: add `tree.Context.Update(0.016f)` first (recipe step 4). The helpers take `in ComputedStyle`.
  - File(s): `tests/BrutalUi.Core.Tests/ThousandElementAllocationTests.cs`
  - Estimate: 4 steps

- [x] ✅ **3.5 Migrate ElementAllocationTests.cs** - Completed 2026-10-21
  - Description: Apply the recipe to `tests/BrutalUi.Core.Tests/ElementAllocationTests.cs`.
  - File(s): `tests/BrutalUi.Core.Tests/ElementAllocationTests.cs`
  - Estimate: 4 steps

- [x] ✅ **3.6 Migrate TreeLayoutBenchmark.cs** - Completed 2026-10-21
  - Description: Apply the recipe to `tests/BrutalUi.Benchmarks/TreeLayoutBenchmark.cs`.
  - File(s): `tests/BrutalUi.Benchmarks/TreeLayoutBenchmark.cs`
  - Estimate: 4 steps

- [x] ✅ **3.7 Migrate LayoutAllocationTests.cs** - Completed 2026-10-21
  - Description: Apply the recipe to `tests/BrutalUi.Core.Tests/Layout/LayoutAllocationTests.cs`.
  - File(s): `tests/BrutalUi.Core.Tests/Layout/LayoutAllocationTests.cs`
  - Estimate: 4 steps

- [x] ✅ **3.8 Run the build, the tests, and the facts line** - Completed 2026-10-21
  - Description: Run the build and the tests. Run `dotnet run -c Release --project tests/BrutalUi.Benchmarks/BrutalUi.Benchmarks.csproj -- --facts 2>&1 | tail -n 30` and expect a normal exit.
  - File(s): none
  - Estimate: 4 steps

**Group B (the render tests, 6 files):**

- [x] ✅ **3.9 Migrate PaintWalkTests.cs** - Completed 2026-10-21
  - Description: Apply the recipe to `tests/BrutalUi.Core.Tests/Rendering/PaintWalkTests.cs` (89 writes, 8 nested).
  - File(s): `tests/BrutalUi.Core.Tests/Rendering/PaintWalkTests.cs`
  - Estimate: 4 steps

- [x] ✅ **3.10 Migrate PaintContentTests.cs** - Completed 2026-10-21
  - Description: Apply the recipe to `tests/BrutalUi.Core.Tests/Rendering/PaintContentTests.cs`.
  - File(s): `tests/BrutalUi.Core.Tests/Rendering/PaintContentTests.cs`
  - Estimate: 4 steps

- [x] ✅ **3.11 Migrate RenderValueTests.cs** - Completed 2026-10-21
  - Description: Apply the recipe to `tests/BrutalUi.Core.Tests/Rendering/RenderValueTests.cs`.
  - File(s): `tests/BrutalUi.Core.Tests/Rendering/RenderValueTests.cs`
  - Estimate: 4 steps

- [x] ✅ **3.12 Migrate ImGuiRendererRectTests.cs** - Completed 2026-10-21
  - Description: Apply the recipe to `tests/BrutalUi.ImGui.Tests/ImGuiRendererRectTests.cs`.
  - File(s): `tests/BrutalUi.ImGui.Tests/ImGuiRendererRectTests.cs`
  - Estimate: 4 steps

- [x] ✅ **3.13 Migrate ImGuiImageElementTests.cs** - Completed 2026-10-21
  - Description: Apply the recipe to `tests/BrutalUi.ImGui.Tests/ImGuiImageElementTests.cs`.
  - File(s): `tests/BrutalUi.ImGui.Tests/ImGuiImageElementTests.cs`
  - Estimate: 4 steps

- [x] ✅ **3.14 Migrate ImGuiRendererAllocationTests.cs** - Completed 2026-10-21
  - Description: Apply the recipe to `tests/BrutalUi.ImGui.Tests/ImGuiRendererAllocationTests.cs`.
  - File(s): `tests/BrutalUi.ImGui.Tests/ImGuiRendererAllocationTests.cs`
  - Estimate: 4 steps

- [x] ✅ **3.15 Run the build and the tests and record the new count** - Completed 2026-10-21
  - Description: Run the build and the tests. Record the new grep count and each recipe step 6 change in `context.md`.
  - File(s): `context.md`
  - Estimate: 4 steps

---

## Phase 4 (Base): Migrate the Element Tests and the Layout Behavior Tests (13/13 complete)

**Demo:** none (base phase: a pure refactor of test code to the inline API)
**Needs:** Phase 3
**Agent tier:** ADVANCED (opus)

**Group A (the element tests, 6 files):**

- [x] ✅ **4.1 Migrate ContentBoxTests.cs** - Completed 2026-10-21
  - Description: Apply the recipe to `tests/BrutalUi.Core.Tests/Elements/ContentBoxTests.cs`.
  - File(s): `tests/BrutalUi.Core.Tests/Elements/ContentBoxTests.cs`
  - Estimate: 5 steps

- [x] ✅ **4.2 Migrate LabelTests.cs** - Completed 2026-10-21
  - Description: Apply the recipe to `tests/BrutalUi.Core.Tests/Elements/LabelTests.cs`.
  - File(s): `tests/BrutalUi.Core.Tests/Elements/LabelTests.cs`
  - Estimate: 5 steps

- [x] ✅ **4.3 Migrate ImageTests.cs** - Completed 2026-10-21
  - Description: Apply the recipe to `tests/BrutalUi.Core.Tests/Elements/ImageTests.cs`.
  - File(s): `tests/BrutalUi.Core.Tests/Elements/ImageTests.cs`
  - Estimate: 5 steps

- [x] ✅ **4.4 Migrate LabelTextTests.cs** - Completed 2026-10-21
  - Description: Apply the recipe to `tests/BrutalUi.Core.Tests/Elements/LabelTextTests.cs`.
  - File(s): `tests/BrutalUi.Core.Tests/Elements/LabelTextTests.cs`
  - Estimate: 5 steps

- [x] ✅ **4.5 Migrate LabelWrapTests.cs** - Completed 2026-10-21
  - Description: Apply the recipe to `tests/BrutalUi.Core.Tests/Elements/LabelWrapTests.cs`.
  - File(s): `tests/BrutalUi.Core.Tests/Elements/LabelWrapTests.cs`
  - Estimate: 5 steps

- [x] ✅ **4.6 Migrate ImGuiLabelTests.cs** - Completed 2026-10-21
  - Description: Apply the recipe to `tests/BrutalUi.ImGui.Tests/ImGuiLabelTests.cs`.
  - File(s): `tests/BrutalUi.ImGui.Tests/ImGuiLabelTests.cs`
  - Estimate: 5 steps

- [x] ✅ **4.7 Run the build and the tests** - Completed 2026-10-21
  - Description: Run the build and the tests.
  - File(s): none
  - Estimate: 5 steps

**Group B (the layout behavior tests, 6 files):**

- [x] ✅ **4.8 Migrate LayoutPassTests.cs and rewrite the old-contract tests** - Completed 2026-10-21
  - Description: Apply the recipe to `tests/BrutalUi.Core.Tests/Layout/LayoutPassTests.cs`. Search first for a test that writes a style with no mark and expects the old layout. Rewrite each such test for the new contract (the write marks `Style`, and the next `Update` lays out the new value). Record each one in `context.md`.
  - File(s): `tests/BrutalUi.Core.Tests/Layout/LayoutPassTests.cs`, `context.md`
  - Estimate: 5 steps

- [x] ✅ **4.9 Migrate LayoutCacheTests.cs** - Completed 2026-10-21
  - Description: Apply the recipe and the search of task 4.8 to `tests/BrutalUi.Core.Tests/Layout/LayoutCacheTests.cs`.
  - File(s): `tests/BrutalUi.Core.Tests/Layout/LayoutCacheTests.cs`, `context.md`
  - Estimate: 5 steps

- [x] ✅ **4.10 Migrate LayoutDifferentialTests.cs** - Completed 2026-10-21
  - Description: Migrate `tests/BrutalUi.Core.Tests/Layout/LayoutDifferentialTests.cs`: `RandomStyle` gives a `StyleProperties` and sets the same members with the same random calls in the same order. A whole copy becomes `copy.InlineStorage = source.InlineStorage; copy.MarkDirty(DirtyFlags.Style);`. `ChangeOneField` and `RemovePercents` take an `InlineStyle`. The seeds and the compare stay the same. Record the count of compared trees before and after in `context.md`. The counts must be equal.
  - File(s): `tests/BrutalUi.Core.Tests/Layout/LayoutDifferentialTests.cs`, `context.md`
  - Estimate: 10 steps

- [x] ✅ **4.11 Migrate ContentInsetsTests.cs** - Completed 2026-10-21
  - Description: Apply the recipe to `tests/BrutalUi.Core.Tests/Layout/ContentInsetsTests.cs` (22 nested writes).
  - File(s): `tests/BrutalUi.Core.Tests/Layout/ContentInsetsTests.cs`
  - Estimate: 5 steps

- [x] ✅ **4.12 Migrate the two measure fixture test files** - Completed 2026-10-21
  - Description: Apply the recipe to `tests/BrutalUi.Core.Tests/Layout/MeasureCacheFixtureTests.cs` and `tests/BrutalUi.Core.Tests/Layout/MeasureModeFixtureTests.cs`.
  - File(s): `tests/BrutalUi.Core.Tests/Layout/MeasureCacheFixtureTests.cs`, `tests/BrutalUi.Core.Tests/Layout/MeasureModeFixtureTests.cs`
  - Estimate: 5 steps

- [x] ✅ **4.13 Run the build and the tests and record the new count** - Completed 2026-10-21
  - Description: Run the build and the tests. Record the new grep count in `context.md`.
  - File(s): `context.md`
  - Estimate: 5 steps

---

## Phase 5 (Base): Migrate the First Half of the Layout Fixtures (14/14 complete)

**Demo:** none (base phase: a pure refactor of test code to the inline API)
**Needs:** Phase 4
**Agent tier:** ADVANCED (opus)

**Group A (6 files):**

- [x] ✅ **5.1 Migrate AlignContentFixtureTests.cs** - Completed 2026-10-21
  - Description: Apply the recipe to `tests/BrutalUi.Core.Tests/Layout/AlignContentFixtureTests.cs`.
  - File(s): `tests/BrutalUi.Core.Tests/Layout/AlignContentFixtureTests.cs`
  - Estimate: 4 steps

- [x] ✅ **5.2 Migrate AlignItemsFixtureTests.cs** - Completed 2026-10-21
  - Description: Apply the recipe to `tests/BrutalUi.Core.Tests/Layout/AlignItemsFixtureTests.cs`.
  - File(s): `tests/BrutalUi.Core.Tests/Layout/AlignItemsFixtureTests.cs`
  - Estimate: 4 steps

- [x] ✅ **5.3 Migrate AlignSelfFixtureTests.cs** - Completed 2026-10-21
  - Description: Apply the recipe to `tests/BrutalUi.Core.Tests/Layout/AlignSelfFixtureTests.cs`.
  - File(s): `tests/BrutalUi.Core.Tests/Layout/AlignSelfFixtureTests.cs`
  - Estimate: 4 steps

- [x] ✅ **5.4 Migrate AutoFixtureTests.cs** - Completed 2026-10-21
  - Description: Apply the recipe to `tests/BrutalUi.Core.Tests/Layout/AutoFixtureTests.cs`.
  - File(s): `tests/BrutalUi.Core.Tests/Layout/AutoFixtureTests.cs`
  - Estimate: 4 steps

- [x] ✅ **5.5 Migrate BorderFixtureTests.cs** - Completed 2026-10-21
  - Description: Apply the recipe to `tests/BrutalUi.Core.Tests/Layout/BorderFixtureTests.cs`.
  - File(s): `tests/BrutalUi.Core.Tests/Layout/BorderFixtureTests.cs`
  - Estimate: 4 steps

- [x] ✅ **5.6 Migrate BoxModelTests.cs** - Completed 2026-10-21
  - Description: Apply the recipe to `tests/BrutalUi.Core.Tests/Layout/BoxModelTests.cs`.
  - File(s): `tests/BrutalUi.Core.Tests/Layout/BoxModelTests.cs`
  - Estimate: 4 steps

- [x] ✅ **5.7 Run the build and the tests** - Completed 2026-10-21
  - Description: Run the build and the tests.
  - File(s): none
  - Estimate: 4 steps

**Group B (6 files):**

- [x] ✅ **5.8 Migrate DimensionFixtureTests.cs** - Completed 2026-10-21
  - Description: Apply the recipe to `tests/BrutalUi.Core.Tests/Layout/DimensionFixtureTests.cs`.
  - File(s): `tests/BrutalUi.Core.Tests/Layout/DimensionFixtureTests.cs`
  - Estimate: 4 steps

- [x] ✅ **5.9 Migrate DisplayFixtureTests.cs** - Completed 2026-10-21
  - Description: Apply the recipe to `tests/BrutalUi.Core.Tests/Layout/DisplayFixtureTests.cs`.
  - File(s): `tests/BrutalUi.Core.Tests/Layout/DisplayFixtureTests.cs`
  - Estimate: 4 steps

- [x] ✅ **5.10 Migrate FlexDirectionFixtureTests.cs** - Completed 2026-10-21
  - Description: Apply the recipe to `tests/BrutalUi.Core.Tests/Layout/FlexDirectionFixtureTests.cs`.
  - File(s): `tests/BrutalUi.Core.Tests/Layout/FlexDirectionFixtureTests.cs`
  - Estimate: 4 steps

- [x] ✅ **5.11 Migrate FlexFixtureTests.cs** - Completed 2026-10-21
  - Description: Apply the recipe to `tests/BrutalUi.Core.Tests/Layout/FlexFixtureTests.cs`.
  - File(s): `tests/BrutalUi.Core.Tests/Layout/FlexFixtureTests.cs`
  - Estimate: 4 steps

- [x] ✅ **5.12 Migrate FlexWrapFixtureTests.cs** - Completed 2026-10-21
  - Description: Apply the recipe to `tests/BrutalUi.Core.Tests/Layout/FlexWrapFixtureTests.cs`.
  - File(s): `tests/BrutalUi.Core.Tests/Layout/FlexWrapFixtureTests.cs`
  - Estimate: 4 steps

- [x] ✅ **5.13 Migrate JustifyContentFixtureTests.cs** - Completed 2026-10-21
  - Description: Apply the recipe to `tests/BrutalUi.Core.Tests/Layout/JustifyContentFixtureTests.cs`.
  - File(s): `tests/BrutalUi.Core.Tests/Layout/JustifyContentFixtureTests.cs`
  - Estimate: 4 steps

- [x] ✅ **5.14 Run the build and the tests and record the new count** - Completed 2026-10-21
  - Description: Run the build and the tests. Record the new grep count in `context.md`.
  - File(s): `context.md`
  - Estimate: 4 steps

---

## Phase 6 (Base): Migrate the Second Half of the Layout Fixtures and Lock the Style (13/13 complete)

**Demo:** none (base phase: a pure refactor that ends the migration)
**Needs:** Phase 5
**Agent tier:** ADVANCED (opus)

**Group A (6 files):**

- [x] ✅ **6.1 Migrate MarginFixtureTests.cs** - Completed 2026-10-21
  - Description: Apply the recipe to `tests/BrutalUi.Core.Tests/Layout/MarginFixtureTests.cs`.
  - File(s): `tests/BrutalUi.Core.Tests/Layout/MarginFixtureTests.cs`
  - Estimate: 4 steps

- [x] ✅ **6.2 Migrate MeasureFixtureTests.cs** - Completed 2026-10-21
  - Description: Apply the recipe to `tests/BrutalUi.Core.Tests/Layout/MeasureFixtureTests.cs`.
  - File(s): `tests/BrutalUi.Core.Tests/Layout/MeasureFixtureTests.cs`
  - Estimate: 4 steps

- [x] ✅ **6.3 Migrate MinMaxDimensionFixtureTests.cs** - Completed 2026-10-21
  - Description: Apply the recipe to `tests/BrutalUi.Core.Tests/Layout/MinMaxDimensionFixtureTests.cs`.
  - File(s): `tests/BrutalUi.Core.Tests/Layout/MinMaxDimensionFixtureTests.cs`
  - Estimate: 4 steps

- [x] ✅ **6.4 Migrate PaddingFixtureTests.cs** - Completed 2026-10-21
  - Description: Apply the recipe to `tests/BrutalUi.Core.Tests/Layout/PaddingFixtureTests.cs`.
  - File(s): `tests/BrutalUi.Core.Tests/Layout/PaddingFixtureTests.cs`
  - Estimate: 4 steps

- [x] ✅ **6.5 Migrate PercentageFixtureTests.cs** - Completed 2026-10-21
  - Description: Apply the recipe to `tests/BrutalUi.Core.Tests/Layout/PercentageFixtureTests.cs`.
  - File(s): `tests/BrutalUi.Core.Tests/Layout/PercentageFixtureTests.cs`
  - Estimate: 4 steps

- [x] ✅ **6.6 Migrate RelativePositionFixtureTests.cs** - Completed 2026-10-21
  - Description: Apply the recipe to `tests/BrutalUi.Core.Tests/Layout/RelativePositionFixtureTests.cs`.
  - File(s): `tests/BrutalUi.Core.Tests/Layout/RelativePositionFixtureTests.cs`
  - Estimate: 4 steps

- [x] ✅ **6.7 Run the build and the tests** - Completed 2026-10-21
  - Description: Run the build and the tests.
  - File(s): none
  - Estimate: 4 steps

**Group B (the lock, 6 files):**

- [x] ✅ **6.8 Migrate the last three fixture files** - Completed 2026-10-21
  - Description: Apply the recipe to `tests/BrutalUi.Core.Tests/Layout/RoundingFixtureTests.cs`, `SizeOverflowFixtureTests.cs`, and `AbsolutePositionFixtureTests.cs`.
  - File(s): `tests/BrutalUi.Core.Tests/Layout/RoundingFixtureTests.cs`, `tests/BrutalUi.Core.Tests/Layout/SizeOverflowFixtureTests.cs`, `tests/BrutalUi.Core.Tests/Layout/AbsolutePositionFixtureTests.cs`
  - Estimate: 4 steps

- [x] ✅ **6.9 Update ElementStateTests.cs for the read-only style** - Completed 2026-10-21
  - Description: Edit `tests/BrutalUi.Core.Tests/ElementStateTests.cs`: `StyleReturnsARefToTheSameField` uses `Unsafe.AsRef(in first.Style)`. `WriteThroughStyleOrLayoutMarksNothing` becomes `WriteThroughLayoutMarksNothing`. Add `StyleIsReadOnly`: the return parameter of the getter of `Element.Style` holds an attribute with the name `IsReadOnlyAttribute` (reflection in the test only).
  - File(s): `tests/BrutalUi.Core.Tests/ElementStateTests.cs`
  - Estimate: 7 steps

- [x] ✅ **6.10 Make Element.Style ref readonly** - Completed 2026-10-21
  - Description: Edit `Element.cs`: `public ref readonly ComputedStyle Style => ref _style;`. The doc comment states that the style pass writes the style and that the game writes `Inline`.
  - File(s): `src/BrutalUi.Core/Tree/Element.cs`
  - Estimate: 4 steps

- [x] ✅ **6.11 Remove gate B from the style pass** - Completed 2026-10-21
  - Description: Edit `StylePass.cs`: remove gate B. Each element with `Style` gets a restyle.
  - File(s): `src/BrutalUi.Core/Style/StylePass.cs`
  - Estimate: 4 steps

- [x] ✅ **6.12 Prove that no direct style write is left** - Completed 2026-10-21
  - Description: Run `grep -rnE '\.Style\.[A-Za-z]+(\.[A-Za-z]+)?\s*=[^=]' src samples tests --include=*.cs`. Expect the 3 doc comment lines of `ComputedStyle.cs`, `Corners.cs`, and `LengthEdges.cs` only. Record the output in `context.md`.
  - File(s): `context.md`
  - Estimate: 4 steps

- [x] ✅ **6.13 Run the build, the tests, and the Linux smoke script** - Completed 2026-10-21
  - Description: Run the build, the tests, and `scripts/test-linux.sh; echo "exit=$?"`. Record the test total in `context.md`.
  - File(s): `context.md`
  - Estimate: 4 steps

---

## Phase 7: Text Color Flows Down the Tree (7/7 complete)

**Demo:** the developer runs `dotnet run --project tests/BrutalUi.Core.Tests/BrutalUi.Core.Tests.csproj -- --filter-method '*TextColorFlowsDownTheTree' --show-live-output on` and reads `root #FFFFFFFF`, `panel #FFCC00FF`, `child #FFCC00FF`, `badge #FF0000FF`, then `child #FFFFFFFF` after the class removal.
**Needs:** Phase 6
**Agent tier:** ADVANCED (opus)

- [x] ✅ **7.1 Add the text members to ComputedStyle** - Completed 2026-10-21
  - Description: Edit `src/BrutalUi.Core/Tree/ComputedStyle.cs`: add `Color`, `Font`, and `FontSize` with doc comments (the units, the inheritance, and the font id rule that `Label` held). Rewrite the class remarks for the new contract: the style pass writes the fields, the game writes `Element.Inline`, and a write marks the dirty bits itself.
  - File(s): `src/BrutalUi.Core/Tree/ComputedStyle.cs`
  - Estimate: 6 steps

- [x] ✅ **7.2 Add the text members to StyleProperties** - Completed 2026-10-21
  - Description: Edit `StyleProperties.cs`: add the 3 text members to the enum and the struct, `StyleMasks.Text`, and the branches of `ApplyTo`. Update `EachMemberHasOneBitInOneMask` to three masks.
  - File(s): `src/BrutalUi.Core/Style/StyleProperties.cs`, `tests/BrutalUi.Core.Tests/Style/StylePropertiesTests.cs`
  - Estimate: 6 steps

- [x] ✅ **7.3 Add the text members to InlineStyle** - Completed 2026-10-21
  - Description: Edit `InlineStyle.cs`: add the 3 text members.
  - File(s): `src/BrutalUi.Core/Style/InlineStyle.cs`
  - Estimate: 6 steps

- [x] ✅ **7.4 Add the context defaults to UiContext** - Completed 2026-10-21
  - Description: Edit `UiContext.cs`: add `DefaultColor`, `DefaultFont`, and `DefaultFontSize` to `CreateInfo` (A2). Store the resolved defaults: a null color gives opaque white. The parameterless constructor uses white, id 0, and size 0. `UpdateStyles` passes the defaults to each root.
  - File(s): `src/BrutalUi.Core/Tree/UiContext.cs`
  - Estimate: 6 steps

- [x] ✅ **7.5 Add the inheritance step to the style pass** - Completed 2026-10-21
  - Description: Edit `StylePass.cs`: the inheritance step, `textChanged` in `Compare` (a text change gives `Paint`), and the child marks of A3. Read `element.Dirty` again after `Restyle`.
  - File(s): `src/BrutalUi.Core/Style/StylePass.cs`
  - Estimate: 6 steps

- [x] ✅ **7.6 Write the text inheritance tests** - Completed 2026-10-21
  - Description: Write `tests/BrutalUi.Core.Tests/Style/TextInheritanceTests.cs`:
    - `TextColorFlowsDownTheTree` (the demo): a context with no default color, a sheet with `.panel { Color = (255, 204, 0, 255) }`, and the tree root > panel > child, and panel > badge with `badge.Inline.Color = (255, 0, 0, 255)`. Print the four lines. Remove the class `panel`, `Update`, and print `child` again. Assert all lines.
    - `ARootTakesTheContextDefaults` (each of the 3 members from `CreateInfo`), `TheParameterlessContextUsesWhite`, `ARuleWinsOverTheParent`, `AnInlineValueWinsOverARule`, and `AChainOfSixInherits`.
    - `AParentChangeReachesAGrandchild` (A3: the grandchild has no own bit before the change), `AChildWithItsOwnValueKeepsIt`, `ATextChangeGivesPaint` (read after `UpdateStyles()` on a plain element), and `TheSameTextValueGivesNoBit`.
  - File(s): `tests/BrutalUi.Core.Tests/Style/TextInheritanceTests.cs`
  - Estimate: 6 steps

- [x] ✅ **7.7 Run the build, the tests, and the demo** - Completed 2026-10-21
  - Description: Run the build, the tests, and the demo. Record the printed lines in `context.md`.
  - File(s): `context.md`
  - Estimate: 6 steps

---

## Phase 8: A Label Draws in the Inherited Text Style (9/9 complete)

**Demo:** the developer runs `dotnet run --project tests/BrutalUi.Core.Tests/BrutalUi.Core.Tests.csproj -- --filter-method '*ALabelDrawsInTheInheritedTextStyle' --show-live-output on` and reads `rect 0,0,40,16` and `Text 0,0 font 0 size 16 #FFCC00FF wrap 40 "Hello"`, then `rect 0,0,80,32` and `Text 0,0 font 0 size 32 #FFCC00FF wrap 80 "Hello"` after the new panel size.
**Needs:** Phase 7
**Agent tier:** ADVANCED (opus)

**Group A (the label, 6 files):**

- [x] ✅ **8.1 Move the label text values to the style** - Completed 2026-10-21
  - Description: Edit `src/BrutalUi.Core/Elements/Label.cs`: remove `_font`, `_size`, `_color`, the properties `Font`, `Size`, and `Color`, and the create-info members `Color`, `Font`, and `Size` (Q3). `PaintContent` and `MeasureLabel` read `Style.Color`, `Style.Font`, and `Style.FontSize`. The class remarks state that the text values come from a rule, the inline set, or the parent.
  - File(s): `src/BrutalUi.Core/Elements/Label.cs`
  - Estimate: 6 steps

- [x] ✅ **8.2 Add the Layout bit for a font change on a Label** - Completed 2026-10-21
  - Description: Edit `StylePass.cs`: a change of `Font` or `FontSize` on a `Label` adds `Layout` (`element is Label`, in the form of `UiContext.RefreshSubtree`).
  - File(s): `src/BrutalUi.Core/Style/StylePass.cs`
  - Estimate: 6 steps

- [x] ✅ **8.3 Update LabelTests.cs and add the demo test** - Completed 2026-10-21
  - Description: Edit `tests/BrutalUi.Core.Tests/Elements/LabelTests.cs`: each create-info color and size moves to `label.Inline.Color` and `label.Inline.FontSize`. Add `ALabelDrawsInTheInheritedTextStyle` (the demo): the tree of `DrawsAOneLineLabel`, a panel with the class `panel` between the root and the label, and a sheet with `.panel { Color = (255, 204, 0, 255); FontSize = 16 }`. Print and assert. Then `panel.Inline.FontSize = 32`, `Update`, `Draw`, print, and assert.
  - File(s): `tests/BrutalUi.Core.Tests/Elements/LabelTests.cs`
  - Estimate: 6 steps

- [x] ✅ **8.4 Update LabelTextTests.cs** - Completed 2026-10-21
  - Description: Edit `tests/BrutalUi.Core.Tests/Elements/LabelTextTests.cs`: remove the setter tests of `Font`, `Size`, and `Color`. Add: a new `FontSize` re-measures (`MeasureCount` goes up) and draws the new size. A new `Font` re-measures. A new `Color` does not re-measure and draws the new color. The same value does not re-measure. A label with no value takes the context defaults.
  - File(s): `tests/BrutalUi.Core.Tests/Elements/LabelTextTests.cs`
  - Estimate: 6 steps

- [x] ✅ **8.5 Update LabelWrapTests.cs and ContentBoxTests.cs** - Completed 2026-10-21
  - Description: Edit `tests/BrutalUi.Core.Tests/Elements/LabelWrapTests.cs` and `ContentBoxTests.cs`: move the create-info text values to `Inline`.
  - File(s): `tests/BrutalUi.Core.Tests/Elements/LabelWrapTests.cs`, `tests/BrutalUi.Core.Tests/Elements/ContentBoxTests.cs`
  - Estimate: 6 steps

**Group B (the other users, 4 files):**

- [x] ✅ **8.6 Update ElementAllocationTests.cs** - Completed 2026-10-21
  - Description: Edit `tests/BrutalUi.Core.Tests/ElementAllocationTests.cs`: the labels get their color and size from a parent `Inline` set. The test still measures 0 bytes.
  - File(s): `tests/BrutalUi.Core.Tests/ElementAllocationTests.cs`
  - Estimate: 6 steps

- [x] ✅ **8.7 Update the two ImGui test files** - Completed 2026-10-21
  - Description: Edit `tests/BrutalUi.ImGui.Tests/ImGuiLabelTests.cs` and `ImGuiRendererAllocationTests.cs`: move the text values to `Inline`. Each expected rect and vertex bound stays.
  - File(s): `tests/BrutalUi.ImGui.Tests/ImGuiLabelTests.cs`, `tests/BrutalUi.ImGui.Tests/ImGuiRendererAllocationTests.cs`
  - Estimate: 6 steps

- [x] ✅ **8.8 Update the HelloPanel sample** - Completed 2026-10-21
  - Description: Edit `samples/HelloPanel/HelloScreen.cs`: `greeting.Inline.Color = GreetingColor;` and `greeting.Inline.FontSize = GreetingFontSize;`. The remarks keep the font 0 rule.
  - File(s): `samples/HelloPanel/HelloScreen.cs`
  - Estimate: 6 steps

- [x] ✅ **8.9 Run the checks and search for old label members** - Completed 2026-10-21
  - Description: Run the build, the tests, the demo, and `scripts/test-linux.sh; echo "exit=$?"`. Run `grep -rnE 'label\w*\.(Color|Font|Size)\b' src samples tests --include=*.cs` and expect no hit. Record the results in `context.md`.
  - File(s): `context.md`
  - Estimate: 6 steps

---

## Phase 9: A Type Selector Styles Each Label (9/9 complete)

**Demo:** the developer runs `dotnet run --project tests/BrutalUi.Core.Tests/BrutalUi.Core.Tests.csproj -- --filter-method '*ATypeSelectorStylesEachLabel' --show-live-output on` and reads `label #FFCC00FF size 0`, `title label #00FF00FF size 24`, and `title panel #00FF00FF size 0`.
**Needs:** Phase 8
**Agent tier:** STANDARD (sonnet)

**Group A (the type id, 6 files):**

- [x] ✅ **9.1 Write ElementTypeId.cs** - Completed 2026-10-21
  - Description: Write `src/BrutalUi.Core/Tree/ElementTypeId.cs` in the form of `ClassId.cs`.
  - File(s): `src/BrutalUi.Core/Tree/ElementTypeId.cs`
  - Estimate: 5 steps

- [x] ✅ **9.2 Write ElementType.cs** - Completed 2026-10-21
  - Description: Write `src/BrutalUi.Core/Tree/ElementType.cs` in the form of `ClassName.cs`: a global table with one lock, `Intern`, and `GetName`. The comparison is ordinal.
  - File(s): `src/BrutalUi.Core/Tree/ElementType.cs`
  - Estimate: 5 steps

- [x] ✅ **9.3 Add TypeId to Element** - Completed 2026-10-21
  - Description: Edit `Element.cs`: `public virtual ElementTypeId TypeId => s_typeId;` with `private static readonly ElementTypeId s_typeId = ElementType.Intern("Element");` (Q2). The doc states: a subclass overrides the property with its own name, and a subclass with no override has the id of its base class.
  - File(s): `src/BrutalUi.Core/Tree/Element.cs`
  - Estimate: 5 steps

- [x] ✅ **9.4 Override TypeId in Label and Image** - Completed 2026-10-21
  - Description: Edit `Label.cs` and `Image.cs`: override `TypeId` with `"Label"` and `"Image"`.
  - File(s): `src/BrutalUi.Core/Elements/Label.cs`, `src/BrutalUi.Core/Elements/Image.cs`
  - Estimate: 5 steps

- [x] ✅ **9.5 Write the element type tests** - Completed 2026-10-21
  - Description: Write `tests/BrutalUi.Core.Tests/Style/ElementTypeTests.cs`: the same name gives the same id, `Element`, `Label`, and `Image` have three ids, `ElementType.Intern("Label")` equals `label.TypeId`, a test subclass with no override has the id of `Element`, a test subclass with an override has its own id, `GetName` gives the name back, and `Intern` throws for null and for an empty name.
  - File(s): `tests/BrutalUi.Core.Tests/Style/ElementTypeTests.cs`
  - Estimate: 5 steps

**Group B (the selector, 3 files):**

- [x] ✅ **9.6 Add the type forms to Selector** - Completed 2026-10-21
  - Description: Edit `Selector.cs`: add `Type`, `ForType`, and `ForTypeAndClass`. `Specificity` gives 1, 10, or 11 (A5). `Matches` compares `Type` with the type id of the element when `Type` is not none, and checks the class when `Class` is not none. Both none throws at creation.
  - File(s): `src/BrutalUi.Core/Style/Selector.cs`
  - Estimate: 5 steps

- [x] ✅ **9.7 Pass the type id to Matches in the style pass** - Completed 2026-10-21
  - Description: Edit `StylePass.cs`: read `element.TypeId` one time in `Restyle` and pass it to `Matches`.
  - File(s): `src/BrutalUi.Core/Style/StylePass.cs`
  - Estimate: 5 steps

- [x] ✅ **9.8 Write the type selector tests** - Completed 2026-10-21
  - Description: Write `tests/BrutalUi.Core.Tests/Style/TypeSelectorTests.cs`:
    - `ATypeSelectorStylesEachLabel` (the demo): a sheet with `Label { Color = (255, 204, 0, 255) }`, `.title { Color = (0, 255, 0, 255) }`, and `Label.title { FontSize = 24 }`, added in that order. A label, a label with `.title`, and a plain element with `.title`. Print and assert the three lines.
    - `TheSpecificityIsOneTenOrEleven`, `AClassRuleBeatsATypeRuleInEachOrder`, `TheBuilderSortsATypeRuleFirst`, `ATypeRuleDoesNotMatchASubclassWithItsOwnId`, and `ForTypeAndClassThrowsForTwoNones`.
  - File(s): `tests/BrutalUi.Core.Tests/Style/TypeSelectorTests.cs`
  - Estimate: 5 steps

- [x] ✅ **9.9 Run the checks and search for reflection** - Completed 2026-10-21
  - Description: Run the build, the tests, and the demo. Run `grep -nE 'System\.Reflection|GetType\(\)|typeof\(' src/BrutalUi.Core/Style/*.cs src/BrutalUi.Core/Tree/ElementType*.cs` and expect no hit. Record the results in `context.md`.
  - File(s): `context.md`
  - Estimate: 5 steps

---

## Phase 10: Two Sheets and a Sheet Change (4/4 complete)

**Demo:** the developer runs `dotnet run --project tests/BrutalUi.Core.Tests/BrutalUi.Core.Tests.csproj -- --filter-method '*ALaterSheetWinsAndARemoveRestyles' --show-live-output on` and reads `panel #3060C0FF`, then `panel #1E222CFF` after the removal of the theme sheet.
**Needs:** Phase 9
**Agent tier:** STANDARD (sonnet)

- [x] ✅ **10.1 Add RemoveSheet and the root restyle to UiContext** - Completed 2026-10-21
  - Description: Edit `UiContext.cs`: add `RemoveSheet` (null throws, an absent sheet gives `false`, the cascade rebuild, and `MarkSubtreeStyle` on each root). `AddRoot` calls `MarkSubtreeStyle` on the new root, so a root from a context with other sheets gets a full restyle (open scope question 2). Update the doc comments of `AddRoot` and `RemoveRoot`.
  - File(s): `src/BrutalUi.Core/Tree/UiContext.cs`
  - Estimate: 6 steps

- [x] ✅ **10.2 Search the tests for a Dirty assert after AddRoot** - Completed 2026-10-21
  - Description: Before the edit of task 10.1, search the tests for an assert of `Dirty` after `AddRoot`. Record each hit in `context.md`, and update each expectation after task 10.1.
  - File(s): `context.md`
  - Estimate: 6 steps

- [x] ✅ **10.3 Write the multi-sheet tests** - Completed 2026-10-21
  - Description: Write `tests/BrutalUi.Core.Tests/Style/MultiSheetTests.cs`:
    - `ALaterSheetWinsAndARemoveRestyles` (the demo): a base sheet with `.panel { BackgroundColor = (30, 34, 44, 255) }` and a theme sheet with `.panel { BackgroundColor = (48, 96, 192, 255) }`. Print after `Update`. `RemoveSheet(theme)`, `Update`, and print again.
    - `AHigherSpecificityInTheFirstSheetWins`, `ThreeSheetsMergeInTheOrderOfAdd`, `RemoveSheetGivesFalseForAnAbsentSheet`, `RemoveTheLastSheetKeepsTheInlineValues`, `AddSheetAfterARemoveRestyles`, and `ARootFromAnotherContextGetsTheNewSheets`.
  - File(s): `tests/BrutalUi.Core.Tests/Style/MultiSheetTests.cs`
  - Estimate: 6 steps

- [x] ✅ **10.4 Run the build, the tests, and the demo** - Completed 2026-10-21
  - Description: Run the build, the tests, and the demo. Record the printed lines in `context.md`.
  - File(s): `context.md`
  - Estimate: 6 steps

---

## Phase 11: The Allocation Gate and the Record (9/9 complete)

**Demo:** the developer runs `dotnet run --project tests/BrutalUi.Core.Tests/BrutalUi.Core.Tests.csproj -- --filter-class '*ThousandElementAllocationTests'` and reads a total of 9 with `failed: 0`.
**Needs:** Phase 10
**Agent tier:** ADVANCED (opus)

**Group A (the gate, 3 files):**

- [x] ✅ **11.1 Add a style sheet to ThousandElementTree.cs** - Completed 2026-10-21
  - Description: Edit `tests/BrutalUi.Core.Tests/Shared/ThousandElementTree.cs`: build a sheet of 12 rules (class, type, and class-on-type rules, among them `.selected` with a paint and a width value and `.muted` with a color) and add it to the context. Put classes on the rows and the sidebar items. Expose the sheet and the two class ids. Use the public API only.
  - File(s): `tests/BrutalUi.Core.Tests/Shared/ThousandElementTree.cs`
  - Estimate: 6 steps

- [x] ✅ **11.2 Add the style step to ThousandElementAllocationTests.cs** - Completed 2026-10-21
  - Description: Edit `tests/BrutalUi.Core.Tests/ThousandElementAllocationTests.cs`: add `StyleKind = 8` and set `KindCount = 9`. The step toggles `.selected` on the grid slot and `.muted` on the sidebar, then marks `Style` on each element of `tree.All`. Add `TheStyleStepRestylesTheTree`: one style step flips the background of the grid slot and the inherited color of a sidebar label, and the next round flips them back. `TheTreeHoldsTheRequiredMix` asserts that the context holds the sheet.
  - File(s): `tests/BrutalUi.Core.Tests/ThousandElementAllocationTests.cs`
  - Estimate: 6 steps

- [x] ✅ **11.3 Prove that the gate blocks** - Completed 2026-10-21
  - Description: Prove that the gate blocks. Copy `src/BrutalUi.Core/Style/StylePass.cs` to the session scratch folder. Add `GC.KeepAlive(new object());` at the start of `Restyle`. Run `dotnet test --solution brutal-ui.sln > /dev/null 2>&1; echo "exit=$?"` and expect a value that is not 0. Restore the file from the copy. Run `diff` on the two files and expect no output. Record both exit codes in `context.md`.
  - File(s): `src/BrutalUi.Core/Style/StylePass.cs` (temporary edit, restored), `context.md`
  - Estimate: 6 steps

- [x] ✅ **11.4 Run the demo class 5 times and the final checks** - Completed 2026-10-21
  - Description: Run the demo class 5 times and record the results in `context.md`. Run the facts line of task 1.1 and record the final `bytes per element`. Run `scripts/test-linux.sh; echo "exit=$?"` and expect the PASS line and `exit=0`.
  - File(s): `context.md`
  - Estimate: 6 steps

**Group B (the record, 5 files):**

- [x] ✅ **11.5 Update technical-design.md** - Completed 2026-10-21
  - Description: Edit `docs/technical-design.md`. Section 4: the style pass at the seam and its own recursion. Section 5: `Inline`, `ref readonly Style`, and `TypeId`. Section 7: the model of "The Public API" (replace the sketch of `StyleRule`), the cascade order, the inline rule, the inheritance, the dirty rule table, and the defaults of `UiContext.CreateInfo`. Section 8: `Label` reads the text values from the style. Section 14: the style tests and the style step of the allocation gate.
  - File(s): `docs/technical-design.md`
  - Estimate: 6 steps

- [x] ✅ **11.6 Update CHANGELOG.md** - Completed 2026-10-21
  - Description: Edit `CHANGELOG.md`: add one line under `### Added` (`StyleProperties`, `Selector`, `StyleRule`, `StyleSheet`, `InlineStyle`, `ElementTypeId`, the style pass, and the text members of `ComputedStyle`) and one line under `### Changed` (`Element.Style` is `ref readonly`, and `Label.Color`, `Label.Font`, and `Label.Size` go). Name the feature `style-system/stylesheet-model`.
  - File(s): `CHANGELOG.md`
  - Estimate: 5 steps

- [x] ✅ **11.7 Write the human testing file** - Completed 2026-10-21
  - Description: Write `docs/features/style-system/human-testing.md` with a `## stylesheet-model` section in the form of `docs/features/imgui-renderer/human-testing.md`: the 7 phase demos and the Windows run of `scripts\test-windows.ps1`.
  - File(s): `docs/features/style-system/human-testing.md`
  - Estimate: 5 steps

- [x] ✅ **11.8 Update the nested-write doc comments** - Completed 2026-10-21
  - Description: Edit the doc comments of `src/BrutalUi.Core/Layout/LengthEdges.cs` and `src/BrutalUi.Core/Rendering/Corners.cs`: the nested-write example uses a local variable, and the remarks name the whole-value write through `Inline`.
  - File(s): `src/BrutalUi.Core/Layout/LengthEdges.cs`, `src/BrutalUi.Core/Rendering/Corners.cs`
  - Estimate: 5 steps

- [x] ✅ **11.9 Lint the docs and write the hand-offs** - Completed 2026-10-21
  - Description: Run `python3 /home/aaronstatic/.claude/skills/asd-ste100/scripts/ste-lint.py --fail-over 2.5` on each changed doc and on this plan, and fix each hit. Write the hand-offs of Notes into `context.md`. Add a "Native Dependencies" note: none. Run the build, the tests, and the grep of task 6.12 (expect no hit).
  - File(s): `context.md`
  - Estimate: 6 steps

---

## Bugs & Issues

**Active Bugs:**
- None

**Fixed Bugs:**
- [x] ✅ 🐛 **Verifier FAIL of the phase 8 check** - Fixed 2026-10-21
  - Fix: the grep of `phase-8-verify.md` matched the word `required` in a doc comment. The check is corrected. No code changed.

---

## Technical Debt

- [ ] 💳 **The decimal specificity weights (CR-3)** - Priority: High
  - Description: class 10 and type 1 hold the CSS order only while each tier count stays below 10.
  - Reason: the user confirmed A5. selector-matching decides the new weights (its open scope question 1).
  - Effort: Low
- [ ] 💳 **No rule index (A7)** - Priority: Low
  - Description: the pass scans each rule of each sheet for each restyled element.
  - Reason: A7 waits for a benchmark. The 12-rule sheet of the allocation tree records the cost.
  - Effort: Medium
- [ ] 💳 **The inline set lives in each element** - Priority: Low
  - Description: each element holds about 250 bytes of `StyleProperties`, also when it is empty. Bytes per element went from 746.5 to 1041.5.
  - Reason: A1 accepts the inline set. A side table can follow a benchmark.
  - Effort: Medium
- [ ] 💳 **No per-edge mask bit** - Priority: Medium
  - Description: a rule that sets `Margin` sets all four edges. A longhand such as `margin-top` in style-parser needs a per-edge bit or a merge rule.
  - Reason: Gaps 5 of the plan.
  - Effort: Medium
- [ ] 💳 **The `Label` check of the pass is a type check** - Priority: Low
  - Description: a later text element (for example `Button`) needs a virtual hook on `Element` or a second check.
  - Reason: Gaps 4 of the plan.
  - Effort: Low

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

### Priority Guidelines
- **High:** Critical for feature to work
- **Medium:** Important but not blocking
- **Low:** Nice to have, can defer

---

*Update this file as tasks are completed. Mark tasks with ✅ immediately when done. Add new tasks as they're discovered.*
