# style-system/stylesheet-model - Context & Decisions

**Last Updated:** 2026-10-21 17:34
**Current Phase:** Done (phases 1 to 11 and the cross-phase review)
**Status:** ✅ Ready for Review

---

## Quick Status

**What's Done:**
- ✅ The plan exists: `implementation.md` (11 phases, 112 tasks). The run uses the recommended option of each of the three open scope questions of the plan.
- ✅ Phase 1: `StyleProperty`, `StyleMasks`, `StyleProperties`, `Selector`, `StyleRule`, `StyleSheet`, `StylePass`, `Element.StyleStorage`, `Element.MarkSubtreeStyle`, `UiContext.AddSheet`, and `UiContext.UpdateStyles` with gate A. Tests 1013 to 1091, 0 failed. The verifier gave PASS and Demo: PASS.
- ✅ Phase 2: `InlineStyle`, `Element.Inline` (`ref readonly`, D2.1), gate B (D2.2), and `HelloScreen` on `Inline`. Tests 1091 to 1138, 0 failed. The smoke run passes. The verifier gave PASS and Demo: PASS.
- ✅ Phase 3 (Base): the shared trees, the allocation tests, and the render tests write through `Inline`. Tests stay at 1138, 0 failed. The write grep goes from 3758 to 3437. The verifier gave PASS.
- ✅ Phase 4 (Base): the element tests and the layout behavior tests write through `Inline`. `LayoutDifferentialTests` compares 200 trees before and after (6534 root comparisons). Tests stay at 1138, 0 failed. The write grep goes from 3437 to 3070. The verifier gave PASS.
- ✅ Phase 5 (Base): twelve Yoga fixture files write through `Inline`. Tests stay at 1138, 0 failed. The write grep goes from 3070 to 1013. The verifier gave PASS.
- ✅ Phase 6 (Base): the last nine fixture files write through `Inline`, `Element.Style` is `ref readonly`, and gate B and `HasInlineWrite` are gone. Tests 1138 to 1139, 0 failed. The write grep finds the 3 doc comment lines only. The smoke run passes. The verifier gave PASS.
- ✅ Phase 7: `Color`, `Font`, and `FontSize` in `ComputedStyle`, `StyleMasks.Text`, the context defaults of `CreateInfo`, the inheritance, and the A3 child marks. Tests 1139 to 1158, 0 failed. The verifier gave PASS and Demo: PASS.
- ✅ Phase 8: `Label` reads `Color`, `Font`, and `FontSize` from the style, `Label.CreateInfo` holds `Text` and `LocalizedText` only, and a `Font` or `FontSize` change on a `Label` adds `Layout`. Tests 1158 to 1160 (6 setter tests removed, 8 added), 0 failed. The smoke run passes. The verifier gave PASS and Demo: PASS.
- ✅ Phase 9: `ElementTypeId`, `ElementType`, the virtual `Element.TypeId` with the `Label` and `Image` overrides, and `Selector.ForType` and `ForTypeAndClass` with the specificities 1, 10, and 11. Tests 1160 to 1180, 0 failed. The verifier gave PASS and Demo: PASS.
- ✅ Phase 10: `UiContext.RemoveSheet`, and `AddRoot` marks the new root with `MarkSubtreeStyle`. One expectation of `DirtyTrackingTests` changed (task 10.2). Tests 1180 to 1188, 0 failed. The verifier gave PASS and Demo: PASS.
- ✅ Phase 11: the 12-rule sheet and the style step of `ThousandElementAllocationTests` (9 tests, 0 bytes in 5 runs), the planted allocation (exit 2, then exit 0 after the restore), `docs/technical-design.md`, `CHANGELOG.md`, `human-testing.md`, the doc comments of `LengthEdges` and `Corners`, and the hand-offs. Tests 1188 to 1189, 0 failed. Bytes per element 1041.5. The verifier gave PASS and Demo: PASS.
- ✅ The cross-phase review: 7 findings (4 MEDIUM, 3 LOW), 7 resolved by review groups 1 to 4. CR-3 keeps the A5 weights and writes the limit into the hand-off. Tests 1189 to 1192, 0 failed. The verifier `review-verify.md` gave PASS, the seven slice demos pass, and the smoke run passes.

**What's Next:**
- 📋 A person runs the items of "Needs Human Verification" (the 7 slice demos and the Windows run).
- 📋 `/autorun-epic` commits the feature, then builds style-system/selector-matching after the user answers its open scope questions.

**Blockers:**
- None

---

## Key Files

### Core Implementation
- `src/BrutalUi.Core/Style/StyleProperties.cs` - the `StyleProperty` enum, `StyleMasks`, and the sparse `StyleProperties` set (phase 1, phase 7)
- `src/BrutalUi.Core/Style/Selector.cs` - the selector (phase 1: class, phase 9: type and class on type)
- `src/BrutalUi.Core/Style/StyleRule.cs` - a selector, a property set, and a specificity (phase 1)
- `src/BrutalUi.Core/Style/StyleSheet.cs` - the immutable sheet and its builder (phase 1)
- `src/BrutalUi.Core/Style/StylePass.cs` - the style pass, the compare, and the inheritance (phases 1, 2, 6, 7, 8, 9)
- `src/BrutalUi.Core/Style/InlineStyle.cs` - the view of `el.Inline` (phase 2, phase 7)
- `src/BrutalUi.Core/Tree/ElementTypeId.cs` and `ElementType.cs` - the type id and its intern table (phase 9)
- `src/BrutalUi.Core/Tree/Element.cs` - `StyleStorage`, `MarkSubtreeStyle`, `Inline`, `ref readonly Style`, and `TypeId`
- `src/BrutalUi.Core/Tree/ComputedStyle.cs` - the text members `Color`, `Font`, and `FontSize` (phase 7)
- `src/BrutalUi.Core/Tree/UiContext.cs` - `AddSheet`, `RemoveSheet`, `UpdateStyles`, and the text defaults of `CreateInfo`
- `src/BrutalUi.Core/Elements/Label.cs` - reads the text values from the style (phase 8)

### Related Files
- `docs/features/style-system/stylesheet-model/implementation.md` - the implementation plan
- `docs/features/style-system/epic-overview.md` - the epic marker and the build order
- `src/BrutalUi.Core/Tree/DirtyWalk.cs` - the visitor rules and the own-recursion pattern

---

## Important Decisions

The plan holds Decisions 1 to 8 and the Confirmed Assumptions Q1 to Q4 and A1 to A9. This section records the decisions of the run.

### Decision R1: The run uses the recommended option of each open scope question
**Date:** 2026-10-21
**Context:** The plan lists three open scope questions. `/autorun-epic` runs the feature with no user question.
**Decision:** Question 1: `Font` and `FontSize` set `Layout` on a `Label`, and `Color` sets `Paint` only. Question 2: `AddRoot` calls `MarkSubtreeStyle`. Question 3: code changes one edge with a whole-value write.
**Rationale:** The plan uses the recommended option of each question.
**Impact:** None beyond the plan.

---

## Phase 1: Decisions

### Task 1.1: The baseline before the first edit (2026-10-21)
- The test total of the solution: 1013 (1013 passed, 0 failed, 0 skipped).
- The facts run: `facts: tree elements 1000, bytes 746528, bytes per element 746.5`.
- The build: `0 Warning(s)`, `0 Error(s)`.
- `.editorconfig` holds no naming rule. The code uses `_camelCase` for an instance field and `s_camelCase` for a static field (`ClassName.cs`, `ClassSet.cs`). The new files follow this form.

### Group 1 (tasks 1.2 to 1.7): The model (2026-10-21)
- The result: the build gives `0 Warning(s)` and `0 Error(s)`. The test total goes from 1013 to 1044 (31 new tests, 0 failed). No file of `src/` other than the 4 new files changed.
- **D1.1 The field order of `StyleProperties`.** The private fields are in the order of size (the mask, the edges, the corners, the lengths, the floats, the colors, the byte enums), not in the order of the enum. The struct then has little padding: about 240 bytes by the field sizes. The getters, the setters, `Clear`, and `ApplyTo` follow the enum order.
- **D1.2 `StyleMasks.All`.** `StyleMasks` has a third internal constant, `All = Layout | Paint`. `IsSet` and `Clear` use it to reject a value that is not a member. Phase 7 adds `Text` to `All`. The masks are `const ulong`, so each bit test folds to one constant.
- **D1.3 `IsSet` and `Clear` throw `ArgumentOutOfRangeException` for a value that is not a member**, for example `(StyleProperty)29` or `(StyleProperty)64`. Without the check, `1UL << 64` wraps to bit 0 and gives the answer of `FlexDirection`. The check costs one compare.
- **D1.4 A set to the default value sets the bit.** `properties.Opacity = 1` makes `IsSet(Opacity)` true, and `ApplyTo` writes the 1. Thus a rule can put a member back to its default over an earlier rule. `Clear` is the only way to remove a value.
- **D1.5 The unset `Opacity` getter tests the mask bit**, so the struct needs no extra flag field. `Clear(Opacity)` sets the field to 0, and the getter gives 1 again. Each other member has the `default` value of its type as its `ComputedStyle` default, so `Clear` writes `default`.
- **D1.6 `Selector.IsEmpty` (internal).** The builder calls it to reject `default(Selector)`. Phase 9 extends `IsEmpty` to the type part, so the builder does not change. `Matches` also checks `IsEmpty` first, so `default(Selector)` matches nothing even when an element holds `ClassId.None` through `ClassStorage`.
- **D1.7 `default(Selector).Specificity` gives 0.** A class selector gives 10. The weight is the private constant `ClassWeight`. Phase 9 adds the type weight next to it.
- **D1.8 The sort key of `Build`** is one `long`: the specificity in the high 32 bits and the index of `Add` in the low 32 bits. `Array.Sort(keys, rules)` then gives one order only. The specificity is never negative, so the shift keeps the order.
- **D1.9 The rule copy.** `StyleRule` copies the `StyleProperties` value, and `Build` copies the list to a new array. A change of the source set or an `Add` after `Build` does not change a built sheet. The tests prove both.
- **D1.10 Extra tests beyond the brief:** the set-to-default case, a clear of each member, a clear of an unset member, the throw for a value that is not a member, the type of each `StyleProperties` property against the `ComputedStyle` member, the selector match, the rule copy, a 200-rule order check, and an empty sheet.
- **Open question for group 2:** none. The pass can call `rule.Selector.Matches(element)` and `rule.Properties.ApplyTo(ref next, skip)` with no defensive copy, because both members are `readonly`.

### Group 2 (tasks 1.8 to 1.12): The pass, the context hooks, and the demo (2026-10-21)
- The result: the build gives `0 Warning(s)` and `0 Error(s)`. The test total goes from 1044 to 1091 (47 new tests in `StylePassTests`: 18 facts and 29 theory rows, 0 failed). No existing test changed.
- The demo prints:
  - `panel BackgroundColor #1E222CFF`
  - `plain BackgroundColor #00000000`
- `TheStylePassAllocatesNothingAfterWarmUp` measures 0 bytes over 100 steps (50 elements, 4 rules).
- The blast radius: `Element.cs` (2 internal members, no new field, so the element size does not change), `UiContext.cs` (2 fields, `AddSheet`, `UpdateStyles`, `RebuildCascade`, and the call at the seam), and the new `StylePass.cs`. Gate A keeps each existing test on the old path, because no existing test adds a sheet.
- **D1.11 The cascade key is one `long`.** The high 32 bits hold the specificity. The low 32 bits hold the index of the rule in the order of the sheets (sheet 0 first, each sheet in its own order). That index grows with the sheet index and then with the rule index, so the key gives the order `(Specificity, sheet index, rule index)` of Decision 2, and the key is unique. The form is the form of D1.8.
- **D1.12 `MarkSubtreeStyle` uses a private static recursion** (`MarkSubtreeStyleCore`) that writes `_dirty` directly, then one `PropagateToAncestors(DescendantStyle)` from the start element. A root has no parent, so the second step does nothing for a root. An element with no child gets `Style` only.
- **D1.13 `StylePass.Run` checks the style bits of each root** before `Visit`, as `DirtyWalk.Walk` does. `Visit` checks the style bits of each child before the call. Thus a clean subtree costs one flag check.
- **D1.14 `Restyle` takes a `ref` to `StyleStorage` one time**, compares, and then writes `next` through the same `ref`. The mark comes after the write.
- **D1.15 `Compare` is `internal static`** and uses two private helpers, `LayoutChanged` and `PaintChanged`, with a short-circuit `||` chain. `EdgesEqual` and `CornersEqual` compare each part with `Equals`, so `NaN` to `NaN` is equal. The theory `CompareGivesTheKindOfEachMember` sets each of the 29 members alone and checks the bit, so a member that `Compare` misses fails the test.
- **D1.16 Gate A tests `_cascade.Length`, not the sheet count.** A context with only empty sheets has an empty cascade, so the pass does not run and a direct write through `Element.Style` stays. `AnEmptyCascadeKeepsADirectWrite` proves it.
- **D1.17 The `AddSheet(null)` throw uses an explicit `ArgumentNullException`** with the message "Build a sheet with StyleSheet.Builder.Build", because `ThrowIfNull` gives no fix in its message. The parameter name is `sheet`.
- **D1.18 Extra tests beyond the brief:** an `Overflow` change gives `Layout` only (A9), a set `Opacity` of 1 gives no mark, a `NaN` to `NaN` restyle gives no mark, a rule of a later sheet wins at equal specificity (A4), `MarkSubtreeStyle` on a leaf sends `DescendantStyle` up, the compare of each edge and each corner, and the empty-cascade gate.
- **Follow-up for phase 10 (A8 and `AddRoot`):** `AddSheet` marks only the roots that the context holds at the call. A root that a later `AddRoot` adds keeps its own bits. A new element holds `Style`, so a new tree restyles. An old tree with clear bits does not restyle until phase 10 makes `AddRoot` call `MarkSubtreeStyle`. The doc comment of `AddSheet` states this.
- **Open question:** none.

---

## Phase 2: Decisions

### Group 1 (tasks 2.1 to 2.7): The inline set and gate B (2026-10-21)
- The result: the build gives `0 Warning(s)` and `0 Error(s)`. The test total goes from 1091 to 1138 (47 new tests in `InlineStyleTests`: 14 facts and 33 theory rows, 0 failed). No existing test changed.
- The demo prints:
  - `BackgroundColor #3060C0FF width 200`
  - `BackgroundColor #1E222CFF width 200`
- The smoke run prints `test-linux: PASS build, test, gpu, smoke (120 frames) in 22 s` and `exit=0`.
- The facts line: `facts: tree elements 1000, bytes 994576, bytes per element 994.6`. The growth is 248.1 bytes per element (746.5 to 994.6): about 240 bytes for `StyleProperties` and 8 bytes for the view field of D2.1. Risk 5 expected about 300 bytes.
- `TheStylePassAllocatesNothingAfterWarmUp` still measures 0 bytes.
- The blast radius: `Element.cs` (3 new fields: `_inline`, `_inlineView`, and the `HasInlineWrite` flag, plus `Inline` and `InlineStorage`), `StylePass.cs` (gate B, the skip mask, the inline step, and 2 helpers from `private` to `internal`), `UiContext.cs` (gate A goes, 1 doc paragraph), the new `InlineStyle.cs`, and `HelloScreen.cs`. Each subclass of `Element` (`Label`, `Image`) gets the view through the base constructor, so no subclass changed.
- **D2.1 `Element.Inline` returns `ref readonly InlineStyle`, not `InlineStyle` (a change of the plan API shape).** The plan shape `public InlineStyle Inline { get; }` does not compile at the call site: `panel.Inline.Width = 200;` gives CS1612 for each set, also with a `readonly struct`, because C# accepts a property set on a variable only. The build of the sample proved it (10 errors). The fix: the element creates one view in its constructor (`private readonly InlineStyle _inlineView`), and `Inline` returns a `ref readonly` to it. A `ref` return is a variable, so each set compiles. The `readonly` stops `element.Inline = default;`. The call syntax of the plan does not change, and the view still allocates nothing. The cost is 8 bytes per element. The other options: a `class` view (one allocation and about 32 bytes per element), or set methods in place of properties (a new call syntax). The parent can review this shape. `technical-design.md` and `implementation.md` still show the plan shape.
- **D2.2 Gate B tests `HasInlineWrite`, not `InlineStorage.SetMask != 0`.** With the mask test of the plan, a clear of the last inline value with no sheet gives a mask of 0, gate B then skips the restyle, and the style keeps the old inline value (a stale style). `HasInlineWrite` is an internal flag that the first inline write sets, and no call clears it. A mask other than 0 always comes with the flag, so the flag covers each case of the mask test. `AClearOfTheLastInlineValueRestylesWithNoSheet` proves the case. Phase 6 removes gate B and the flag.
- **D2.3 The setter equality.** Each setter tests the bit of its member first, then the value. A float uses `float.Equals` (`NaN` equals `NaN`). `Length` and `Rgba8` use `Equals`. The enums use `==`. `LengthEdges` and `Corners` use `StylePass.EdgesEqual` and `StylePass.CornersEqual`, now `internal` (no second copy).
- **D2.4 The default view.** `InlineStyle` holds `Element? _owner`. A private `Owner` getter throws `InvalidOperationException` with the text "The InlineStyle has no owner. Use Element.Inline to get a view." through a `[DoesNotReturn]` helper, so the hot path stays small. `ClassList` has no default check (a default instance gives a `NullReferenceException`), so the form is new.
- **D2.5 `Clear` calls `IsSet` first.** Thus a clear of an unset member marks nothing, and a value that is not a member throws `ArgumentOutOfRangeException` (D1.3).
- **D2.6 `Restyle` reads the inline mask one time** and passes it as the skip mask of each rule. Then `InlineStorage.ApplyTo(ref next, 0UL)` writes the inline set last. The skip mask is not necessary for the result, because the inline step writes over each skipped member, but it saves the writes and it keeps the `applied` mask idea open for phase 7.
- **D2.7 Extra tests beyond the brief:** a set of the default on an unset member marks `Style` (D1.4), a clear of a set member marks `Style`, the clear of the last inline value with no sheet, a direct write with no inline write and no sheet stays (gate B), a paint change gives `Paint` and not `Layout`, a value that is not a member throws, the whole-value write of one edge, and the theory `EachMemberOfTheViewWritesItsOwnMember` (29 rows). The theory checks for each member: the set writes only the bit of that member, the same value marks nothing, a new value marks `Style`, the value reaches the style with the right kind, and a clear gives back the default.
- **Migration risk for phases 3 to 6:** an element with one inline write gets a full restyle from `default(ComputedStyle)`. Thus a test that writes some members through `Element.Style` and other members through `Element.Inline` on the same element loses the direct writes. Migrate each write of one element in one step.
- **Follow-up for phase 6:** remove gate B and `Element.HasInlineWrite`. Update the plan docs to the `ref readonly` shape of D2.1 when the parent accepts it.
- **Open question:** Q2.1 Does the parent accept `public ref readonly InlineStyle Inline` (D2.1) as the public shape? The call syntax is the plan syntax.

---

## Phase 3: Decisions

### Group 1 (tasks 3.1 to 3.8): The shared trees and the allocation tests (2026-10-21)
- **Task 3.1 The count before the first edit:** `grep -rnE '\.Style\.[A-Za-z]+(\.[A-Za-z]+)?\s*=[^=]' tests samples --include=*.cs | wc -l` gives 3758 (the plan expected about 3770). After this group, the count is 3582. The difference is 176 writes: 124 in `ThousandElementTree.cs`, 3 in `ChainTree.cs`, 5 in `ThousandElementAllocationTests.cs`, 5 in `ElementAllocationTests.cs`, 1 in `TreeLayoutBenchmark.cs`, and 38 in `LayoutAllocationTests.cs`.
- **The check of the shared-tree users:** `grep -rln 'ThousandElementTree\|ChainTree' tests --include=*.cs` gives 10 files. 4 of them are in this group: `ThousandElementTree.cs`, `ChainTree.cs`, `ThousandElementAllocationTests.cs`, and `TreeLayoutBenchmark.cs`. `PercentChainBudgetTests.cs`, `FactsReport.cs`, `PercentChainBenchmark.cs`, `StackProbe.cs`, and `TreeDrawBenchmark.cs` hold no `.Style` access. `ImGuiRendererAllocationTests.cs` (group 2) writes through `.Style.` only on the elements of its own label scene (a root, the labels, and the images that the file creates), not on an element of a shared tree. Thus no hit is outside this group or group 2.
- The result: the build gives `0 Warning(s)` and `0 Error(s)`. The test total stays at 1138 (1138 passed, 0 failed, 0 skipped). No test changed its expected value. No expected rect and no expected draw call changed. No file of `src/` changed.
- The facts line: `facts: tree elements 1000, bytes 994576, bytes per element 994.6`. The line does not change, because the inline setter allocates nothing. The chain lines stay at 3031 and 5674 calls, and the stack probe stays at 507 levels.
- The allocation gates still measure 0 bytes: `ThousandElementAllocationTests`, `ElementAllocationTests`, and `LayoutAllocationTests` pass with the step bodies that now write through `Inline`.
- **D3.1 No CS1612 fix was necessary.** The 6 files hold no nested write of an edge or a corner. Each edge write is already a whole value (`LengthEdges.All(...)`).
- **D3.2 The computed reads stay on `Style`.** In `ThousandElementAllocationTests.CountPainted`, the reads of `Display` and `Opacity` on each ancestor read the result of the pass, so they stay `current.Style.Display` and `current.Style.Opacity` after the `replace_all`. The two `ref ComputedStyle style = ref ... .Style;` locals only read, so they become `ref readonly`. The helpers `HasVisibleBorder`, `HasCornerRadius`, and `HasPercentSize` take `in ComputedStyle`, and the calls use `in style`.
- **D3.3 The reads of a value that the test wrote go to `Inline`.** The asserts and the loop guard on `tree.Settings.Inline.Display` (5 reads in `ThousandElementAllocationTests.cs`) and the asserts on `scene.Hidden.Inline.Display` (4 reads in `LayoutAllocationTests.cs`) read the value that the step body wrote. Each one runs after an `Update`, so the computed value is the same, and the meaning of the assert does not change.
- **D3.4 `TheTreeHoldsTheRequiredMix` runs one `Update` before it reads the style (recipe step 4).** Without it, each computed style holds the defaults, and the mix counts give 0. The doc comment of `ThousandElementTree.Build` stays true: the build runs no `Update`, and the test runs the `Update`.
- **D3.5 Each `MarkDirty(DirtyFlags.Layout)` call stays (recipe step 5).** The step bodies alternate two values, so each write gives a new value. The marks are still the contract of the tests.
- **Recipe step 6 changes:** none. No exact assert of `Dirty` failed, and no test asserts the old contract in these files.
- **Finding for `src/`:** none.
- **Open question:** none.

### Group 2 (tasks 3.9 to 3.15): The render tests of Core and ImGui (2026-10-21)
- **Task 3.15 The count:** `grep -rnE '\.Style\.[A-Za-z]+(\.[A-Za-z]+)?\s*=[^=]' tests samples --include=*.cs | wc -l` gives 3437 after this group (3582 before). The difference is 145 writes: 89 in `PaintWalkTests.cs`, 38 in `PaintContentTests.cs`, 2 in `RenderValueTests.cs`, 10 in `ImGuiRendererRectTests.cs`, 1 in `ImGuiImageElementTests.cs`, and 5 in `ImGuiRendererAllocationTests.cs`. Each of the 6 files gives 0.
- The result: the build gives `0 Warning(s)` and `0 Error(s)`. The test total stays at 1138 (1138 passed, 0 failed, 0 skipped). No expected rect and no expected draw call changed. No file of `src/` changed.
- The ImGui allocation gates (`Assert.Equal(0L, bytes)`, 3 asserts in `ImGuiRendererAllocationTests.cs`) still pass. The label scene now writes its root, its labels, and its images through `Inline`. `Label` and `Image` write no style in their constructors, so the restyle from `default(ComputedStyle)` loses no value.
- **D3.6 The CS1612 fixes (recipe step 2).** The 5 nested writes each set two edges (`Left` and `Top`), so each pair becomes one `with` expression: `x.Inline.Margin = x.Inline.Margin with { Left = 7, Top = 3 };`. The fixes are 4 in `PaintWalkTests.cs` (3 on `Margin`, 1 on `BorderWidth`) and 1 in `PaintContentTests.cs` (`Margin`). The `with` keeps the other two edges at the inline default, as the old write on `Style` did. A `new LengthEdges(...)` with four values does not apply, because the tests set only two edges.
- **D3.7 `DrawsAChainOf5000LevelsWithNoOverflow` runs `context.UpdateStyles()` before the draw (recipe step 4).** The test draws with no `Update`, because the layout recursion must not run on the chain of 5000 levels. After the migration, the inline fill reaches the computed style only in the style pass, so the draw gave 0 rects in place of 5000. The internal `UiContext.UpdateStyles` runs the style pass alone, so the layout recursion still does not run. The style pass runs on the test thread, and the draw still runs on the thread with the 64 KB stack. The doc comment of the test now tells this.
- **D3.8 `RenderValueTests.DefaultOpacityIsOne` keeps the read `new Element().Style.Opacity`.** The assert reads the computed default of a new element, not an inline value, so the read goes back to `Style` after the `replace_all`. The reads in `OpacityReadsBackTheSetValue` read the value that the test wrote, so they stay on `Inline` (the test runs no `Update`, and the view returns the set value).
- **Recipe step 3:** no file holds a `ref ComputedStyle` local or parameter. **Recipe step 5:** `PaintWalkTests.DrawChangesNoDirtyBit` keeps its `MarkDirty` calls.
- **Recipe step 6 changes:** none. No exact assert of `Dirty` failed, and no test asserts the old contract in these files.
- **Finding for `src/`:** none.
- **Open question:** none.

---

## Phase 4: Decisions

### Group 1 (tasks 4.1 to 4.7): The element tests (2026-10-21)
- **Task 4.7 The count:** `grep -rnE '\.Style\.[A-Za-z]+(\.[A-Za-z]+)?\s*=[^=]' tests samples --include=*.cs | wc -l` gives 3394 after this group (3437 before). The difference is 43 writes: 20 in `ContentBoxTests.cs`, 2 in `LabelTests.cs`, 14 in `ImageTests.cs`, 2 in `LabelTextTests.cs`, 3 in `LabelWrapTests.cs`, and 2 in `ImGuiLabelTests.cs`. Each of the 6 files gives 0.
- The result: the build gives `0 Warning(s)` and `0 Error(s)`. The test total stays at 1138 (1138 passed, 0 failed, 0 skipped). No expected rect, no expected draw call, and no vertex bound changed. No file of `src/` changed.
- **D4.1 Each `.Style.` access in the 6 files was a write.** The `replace_all` changed 43 lines, and each changed line is a write. No file reads a computed value through `.Style.`, so no read goes back to `Style`. The `cref` text `ComputedStyle.AlignItems` has no dot before `Style`, so the `replace_all` did not change it.
- **D4.2 No CS1612 fix was necessary (recipe step 2).** Each edge write is already a whole value (`LengthEdges.All(...)` or `new LengthEdges(...)`).
- **D4.3 No `Update` was added (recipe step 4).** Each test that writes through `Inline` runs `context.Update` before it draws or reads a rect. No file holds a `ref ComputedStyle` local or parameter (recipe step 3).
- **D4.4 `ImageTests.DrawsAZeroUvSpanWithTheStretchRule` keeps its `MarkDirty(DirtyFlags.Layout)` call (recipe step 5).** The write of `Width = 10` is a new value, and the mark stays the contract of the test.
- **D4.5 The writes of the label stay on the create info.** `Label.Color`, `Label.Font`, `Label.Size`, and their create-info members do not change (phase 8 moves them).
- **Recipe step 6 changes:** none. No exact assert of `Dirty` failed. The asserts of `LabelTextTests.cs` mask with `SelfBits` or run after an `Update`, and `RefreshReachesADeepLabel` writes the chain before its first `Update`, so the new `Style` bit clears before the assert.
- **Finding for `src/`:** none.
- **Open question:** none.

### Group 2 (tasks 4.8 to 4.13): The layout tests of the dirty bits, the cache, and the differential layout (2026-10-21)
- **Task 4.13 The count:** `grep -rnE '\.Style\.[A-Za-z]+(\.[A-Za-z]+)?\s*=[^=]' tests samples --include=*.cs | wc -l` gives 3070 after this group (3394 before). The difference is 324 writes: 125 in `LayoutPassTests.cs`, 69 in `LayoutCacheTests.cs`, 3 in `LayoutDifferentialTests.cs`, 82 in `ContentInsetsTests.cs`, 14 in `MeasureCacheFixtureTests.cs`, and 31 in `MeasureModeFixtureTests.cs`. Each of the 6 files gives 0. The grep does not count the writes through a `ref ComputedStyle` local. `LayoutDifferentialTests.cs` held 8 such locals and 2 such parameters (`ChangeOneField`, `RemovePercents`). Each local is now an `InlineStyle` view.
- The result: the build gives `0 Warning(s)` and `0 Error(s)`. The test total stays at 1138 (1138 passed, 0 failed, 0 skipped). `git diff -U0 -- tests/BrutalUi.Core.Tests/Layout | grep -c '^[-+].*AssertRect'` gives 0. No expected rect, no expected measure count, and no expected constraint changed. No file of `src/` changed.
- **Task 4.10 The count of compared trees.** Before the edit: `Seed 20261017: 200 trees, 4200 steps, 6534 root comparisons, 174 reparents, 260 promotes, 76 demotes, no difference, 0.78 s.` After the edit: `Seed 20261017: 200 trees, 4200 steps, 6534 root comparisons, 174 reparents, 260 promotes, 76 demotes, no difference, 1.25 s.` The counts are equal, and the test gives 0 failed. The run reads the line with `--xunit-diagnostics on`. The run time grows by about 0.5 s, because each element and each fresh copy now restyles from the inline set.
- **D4.6 Recipe step 6 (the old contract): no test needed a rewrite.** The search of tasks 4.8 and 4.9 found no test that writes a style with no mark and expects the old layout. Each "no mark" pass in `LayoutCacheTests.cs` (`SecondUpdateWithNoChangeCallsNoMeasureCallback`, the third pass of `MarkOnOneLeafCallsOnlyThatLeaf`) writes no style before the pass. Each style write after the first `Update` in the 6 files has a `MarkDirty(DirtyFlags.Layout)` call, and each mark stays (recipe step 5). The only "marks nothing" assert (`NewViewportSizeChangesSizeOfCleanAutoRoot`) tests a viewport change, not a style write. No exact assert of `Dirty` failed: the asserts mask with `Layout | DescendantLayout`, or they read an element that the step does not write (`child1` in `HeightChangeOfFirstChildMovesSecondChild`, `root` after an `Update`).
- **D4.7 The CS1612 fixes (recipe step 2): 59 nested writes in 35 groups.** `LayoutPassTests.cs` had 12 writes in 7 groups, `LayoutCacheTests.cs` had 11 in 7 groups, `ContentInsetsTests.cs` had 22 in 12 groups, and `LayoutDifferentialTests.cs` had 14 in 9 groups (through its `ref ComputedStyle` locals). Each group of writes to one edge set became one `with` expression, for example `root.Inline.Padding = root.Inline.Padding with { Top = 10, Bottom = 4 };`. Only `PercentPaddingOfARootResolvesAgainstTheViewportWidth` wrote four edges in a row, so it became `new LengthEdges(Length.Percent(10), Length.Percent(5), Length.Percent(2.5f), 3)`. A write of an edge to 0 (`Padding.Left = 0`, `Margin.Left = 0` in `LeafWithTopMarginGetsNewSizeWhenParentHeightBecomesAuto`) stays in the `with`, so the edge keeps the value 0 and not `Undefined`.
- **D4.8 The whole copy of `LayoutDifferentialTests` uses the helper `CopyInline(Element, in StyleProperties)`.** The helper writes `element.Inline.Opacity = 1f`, then runs `element.InlineStorage = source;` and `element.MarkDirty(DirtyFlags.Style);`. The first write sets `HasInlineWrite` (gate B, D2.2), and the copy then replaces that value and the full mask. `Clone`, `NewElement`, and the last case of `ChangeOneField` call it. The gap of the brief occurs: without the first write, a new element and each fresh copy hold no flag, and the pass keeps the default style. A mutation run proved it: with the first write removed, the test fails at tree 0, change 0 (`23 elements differ`). A temporary probe (removed after the run) checked 249130 elements of the incremental trees and the fresh copies after each step: the flag is set when the mask is not 0, and the computed `Width`, `Height`, `Display`, `FlexDirection`, `MinWidth`, `Margin.Left`, `Padding.Right`, and `Position` equal the inline values. Phase 6 removes gate B. Then the first write of `CopyInline` has no effect, and the helper can drop it.
- **D4.9 `ChangeOneField` takes the `Element`, not an `InlineStyle` (a change of the brief shape).** Its last case copies the whole set through `CopyInline`, and a view gives no access to its element. The method gets the view with `InlineStyle style = target.Inline;` and keeps the same random calls in the same order. `RemovePercents` takes an `InlineStyle` as the brief tells. The old `RemovePercents(ref LengthEdges)` became `WithoutPercent(LengthEdges)`, which gives a new value, because a view cannot give a `ref` to an edge set.
- **D4.10 `RandomStyle` gives a `StyleProperties`** with the same 25 members, the same random calls, and the same order. A whole copy thus replaces each member that the old whole copy of `ComputedStyle` changed. Each other member stays unset and gives its default, as the old copy did.
- **D4.11 `NudgeSize` has no `ref Length` local.** A view cannot give a `ref` to `Width` or `Height`, so the method reads the old value, computes the new value with the same random calls, and sets `Width` or `Height`. A nudge of a width of 0 down gives the same value, and the set then marks nothing. The `MarkDirty(DirtyFlags.Layout)` call stays, so the step still lays out the container.
- **D4.12 The reads stay on `Inline`.** The reads of `Display` in `Promote` and `Demote`, the reads in `ChangeContainer`, `NudgeSize`, `ShapeLimitedLeaves`, and `RemovePercents`, and the texts of `ChangeOneField` read the values that the test wrote. They run before the pass, so a read of the computed style would give an old value. Each such read also decides a random call or a text, so a read of `Inline` keeps the random order of the old code. No file of the group reads a computed value through `.Style.`.
- **D4.13 `Clone` now copies the inline set, not the computed style.** Its users are `LayoutDifferentialTests`, `LayoutCacheTests`, `ContentInsetsTests`, and `PercentChainBudgetTests` (through `ChainTree`, which phase 3 moved to `Inline`). Each of them writes through `Inline` after this group. A later test that calls `AssertMatchesFreshLayout` on a tree with a write through `Element.Style` gets a wrong fresh copy, so it must write through `Inline`.
- **Recipe step 3:** the `ref ComputedStyle` locals of `LayoutDifferentialTests.cs` each write, so each one became an `InlineStyle` view. The other 5 files hold no `ref ComputedStyle` local or parameter. **Recipe step 4:** no `Update` was added. Each test runs `context.Update` or `LayoutFixture.Calculate` before it reads a rect, an inset, or a count.
- **Finding for `src/`:** none.
- **Open question:** none. The parent can review D4.9.

---

## Phase 5: Decisions

### Group 1 (tasks 5.1 to 5.7): The first six Yoga fixture files (2026-10-21)
- **Task 5.7 The count:** `grep -rnE '\.Style\.[A-Za-z]+(\.[A-Za-z]+)?\s*=[^=]' tests samples --include=*.cs | wc -l` gives 1932 after this group (3070 before). The difference is 1138 writes: 753 in `AlignContentFixtureTests.cs`, 108 in `AlignItemsFixtureTests.cs`, 21 in `AlignSelfFixtureTests.cs`, 39 in `AutoFixtureTests.cs`, 13 in `BorderFixtureTests.cs`, and 204 in `BoxModelTests.cs`. Each of the 6 files gives 0, and no `.Style` text stays in the 6 files.
- The result: the build gives `0 Warning(s)` and `0 Error(s)`. The test total stays at 1138 (1138 passed, 0 failed, 0 skipped). `git diff -U0 -- tests/BrutalUi.Core.Tests/Layout | grep -c '^[-+].*AssertRect'` gives 0. No file of `src/` changed.
- **D5.1 Each `.Style.` access in the 6 files was a write.** No file reads a computed value through `.Style.`, so no read goes back to `Style`. The two `cref` texts `ComputedStyle.AlignContent` and `ComputedStyle.AlignItems` in `BoxModelTests.cs` have no dot before `Style`, so the `replace_all` did not change them.
- **D5.2 The CS1612 fixes (recipe step 2): 136 nested writes.** `AlignContentFixtureTests.cs` had 97, `AlignItemsFixtureTests.cs` had 10, `AutoFixtureTests.cs` had 1, and `BoxModelTests.cs` had 28. `AlignSelfFixtureTests.cs` and `BorderFixtureTests.cs` had none.
  - Each run of four edge writes of one element with four equal values became `LengthEdges.All(v)`: 19 runs in `AlignContentFixtureTests.cs` (8 `BorderWidth` 60, 2 `Margin` 10, 2 `Padding` 10, 2 `Padding` 2, 2 `BorderWidth` 5, 3 `Padding` 20) and 21 single writes (`Margin.Right` or `Margin.Left` 20 on 7 elements in 3 tests) and 2 runs in `BoxModelTests.cs` (`BorderWidth` 10, `Padding` 5). No run of four held different values, so no `new LengthEdges(l, t, r, b)` was necessary.
  - Each other edge write became one `x.Inline.Margin = x.Inline.Margin with { Edge = v };` line, one line for each write, in the order of the old writes (the brief rule "one at a time"). Phases 3 and 4 put two adjacent writes of one edge set in one `with`. The result is the same, because each `with` reads the inline value that the line before wrote.
  - An unset inline edge set gives `default(LengthEdges)` (four `Length.Undefined` values), as the old `ComputedStyle` default did. Thus the edges that a `with` does not name keep the old value.
- **D5.3 The syntax check.** A word diff of the 6 files, with the `Style` to `Inline` rename removed, shows only the `LengthEdges.All(...)` runs and the `with` lines. No number, no unit, and no enum value changed. The line count proves the collapse: 1138 lines removed and 1075 lines added. The 21 runs of four (19 in `AlignContentFixtureTests.cs`, 2 in `BoxModelTests.cs`) remove 3 lines each, which gives the difference of 63 lines.
- **Recipe step 3:** no file holds a `ref ComputedStyle` local or parameter. **Recipe step 4:** no `Update` was added. Each test calls `LayoutFixture.Calculate`, which runs `UiContext.Update`, before it reads a rect. **Recipe step 5:** no file calls `MarkDirty`.
- **Recipe step 6 changes:** none. No file asserts a `Dirty` value. `LayoutFixture.AssertNoLayoutBits` reads the layout bits only, after the `Update` clears the `Style` bit.
- **Finding for `src/`:** none.
- **Open question:** none.

### Group 2 (tasks 5.8 to 5.14): The next six Yoga fixture files (2026-10-21)
- **Task 5.14 The count:** `grep -rnE '\.Style\.[A-Za-z]+(\.[A-Za-z]+)?\s*=[^=]' tests samples --include=*.cs | wc -l` gives 1013 after this group (1932 before). The difference is 919 writes: 4 in `DimensionFixtureTests.cs`, 57 in `DisplayFixtureTests.cs`, 322 in `FlexDirectionFixtureTests.cs`, 68 in `FlexFixtureTests.cs`, 288 in `FlexWrapFixtureTests.cs`, and 180 in `JustifyContentFixtureTests.cs`. Each of the 6 files gives 0, and no `Style` text stays in the 6 files.
- The result: the build gives `0 Warning(s)` and `0 Error(s)`. The test total stays at 1138 (1138 passed, 0 failed, 0 skipped). `git diff -U0 -- tests/BrutalUi.Core.Tests/Layout | grep -c '^[-+].*AssertRect'` gives 0. No file of `src/` changed.
- **D5.4 Each `.Style.` access in the 6 files was a write.** No file reads a computed value through `.Style.`, so no read goes back to `Style`.
- **D5.5 The CS1612 fixes (recipe step 2): 43 nested writes.** `FlexDirectionFixtureTests.cs` had 26, `FlexWrapFixtureTests.cs` had 10, and `JustifyContentFixtureTests.cs` had 7. The other 3 files had none.
  - `FlexWrapFixtureTests.cs` had 2 runs of four `Margin` writes of 20 on `child1` (`WrappedColumnMaxHeight` and `WrappedColumnMaxHeightFlex`). Each run became `child1.Inline.Margin = LengthEdges.All(20);`. No run of four held different values, so no `new LengthEdges(l, t, r, b)` was necessary.
  - Each other edge write became one `x.Inline.Edges = x.Inline.Edges with { Edge = v };` line, one line for each write, in the order of the old writes (as D5.2). This includes the two adjacent `Padding` writes (`Left` and `Right`, 100) on `child0Child0` in `JustifyContentFixtureTests.cs`, and the write `Margin.Right = Length.Auto` on `child0`.
- **D5.6 The syntax check.** A copy of each file at `HEAD` with only a mechanical rename (each `.Style.` to `.Inline.`, and each single edge write to its `with` line) equals the new file for 5 of the 6 files. For `FlexWrapFixtureTests.cs`, the only difference is the 2 runs of four that became `LengthEdges.All(20)`. No number, no unit, and no enum value changed. The line count proves it: 919 lines removed and 913 lines added (2 runs, 3 lines each).
- **Recipe step 3:** no file holds a `ref ComputedStyle` local or parameter. **Recipe step 4:** no `Update` was added. Each test calls `LayoutFixture.Calculate` before it reads a rect. **Recipe step 5:** no file calls `MarkDirty`.
- **Recipe step 6 changes:** none. No file asserts a `Dirty` value.
- **Finding for `src/`:** none.
- **Open question:** none.

---

## Phase 6: Decisions

### Group 1 (tasks 6.1 to 6.7): The first six of the last nine Yoga fixture files (2026-10-21)
- **Task 6.7 The count:** `grep -rnE '\.Style\.[A-Za-z]+(\.[A-Za-z]+)?\s*=[^=]' tests samples --include=*.cs | wc -l` gives 280 after this group (1013 before). The difference is 733 writes: 207 in `MarginFixtureTests.cs`, 145 in `MeasureFixtureTests.cs`, 133 in `MinMaxDimensionFixtureTests.cs`, 20 in `PaddingFixtureTests.cs`, 188 in `PercentageFixtureTests.cs`, and 40 in `RelativePositionFixtureTests.cs`. Each of the 6 files gives 0, and no `Style` text stays in the 6 files.
- The result: the build gives `0 Warning(s)` and `0 Error(s)`. The test total stays at 1138 (1138 passed, 0 failed, 0 skipped). `git diff -U0 -- tests/BrutalUi.Core.Tests/Layout | grep -c '^[-+].*AssertRect'` gives 0. No file of `src/` changed in this group.
- **D6.1 Each `.Style.` access in the 6 files was a write.** Before the edit, the count of `Style` text in each file equals its count of writes. No file reads a computed value through `.Style.`, so no read goes back to `Style`.
- **D6.2 The CS1612 fixes (recipe step 2): 72 nested writes.** `MarginFixtureTests.cs` had 41, `MeasureFixtureTests.cs` had 23, and `PercentageFixtureTests.cs` had 8. The other 3 files had none.
  - `PercentageFixtureTests.cs` had 2 runs of four edge writes of `Length.Percent(10)` on `child0` (`Margin` in `PercentageMarginShouldCalculateBasedOnlyOnWidth`, `Padding` in `PercentagePaddingShouldCalculateBasedOnlyOnWidth`). Each run became `LengthEdges.All(Length.Percent(10))`. No run of four held different values, so no `new LengthEdges(l, t, r, b)` was necessary.
  - Each other edge write became one `x.Inline.Edges = x.Inline.Edges with { Edge = v };` line, one line for each write, in the order of the old writes (as D5.2). This includes the adjacent pairs of `Margin` writes on `child0` in `MarginFixtureTests.cs` and the pairs of `Padding.Top` and `Margin.Top` on `child2` and `child3` in `MeasureFixtureTests.cs`. A write of an edge to 0 (`Margin.Top = 0`, `Padding.Top = 0` in `MeasureFixtureTests.cs`) stays in the `with`, so the edge keeps the value 0 and not `Undefined`.
- **D6.3 The syntax check.** A copy of each file at `HEAD` with only a mechanical rename (each `.Style.` to `.Inline.`, and each single edge write to its `with` line) equals the new file for 5 of the 6 files. For `PercentageFixtureTests.cs`, the only difference is the 2 runs of four that became `LengthEdges.All(Length.Percent(10))`. No number, no unit, and no enum value changed. The line count proves it: 733 lines removed and 727 lines added (2 runs, 3 lines each).
- **Recipe step 3:** no file holds a `ref ComputedStyle` local or parameter. **Recipe step 4:** no `Update` was added. Each test calls `LayoutFixture.Calculate` before it reads a rect or a measure count. **Recipe step 5:** no file calls `MarkDirty`.
- **Recipe step 6 changes:** none. No file asserts a `Dirty` value.
- **Finding for `src/`:** none.
- **Open question:** none.

### Group 2 (tasks 6.8 to 6.13): The last three fixture files and the lock (2026-10-21)
- **The check before the lock:** `grep -rnE '\.Style\.[A-Za-z]+(\.[A-Za-z]+)?\s*=[^=]' src samples tests --include=*.cs -c` gave 280 writes in `tests` and `samples`: 252 in `AbsolutePositionFixtureTests.cs`, 18 in `SizeOverflowFixtureTests.cs`, 8 in `RoundingFixtureTests.cs`, 1 in `Style/InlineStyleTests.cs`, and 1 in `Style/StylePassTests.cs`. The 3 hits of `src` are the doc comment lines. A second grep for `ref ComputedStyle` locals and whole writes (`.Style = `) found only `ElementStateTests.cs` (task 6.9). Thus 2 test files outside this group wrote through `Style`.
- **Task 6.8:** each `.Style.` access in the 3 fixture files was a write. `AbsolutePositionFixtureTests.cs` had 12 nested writes (CS1612) and no run of four. Each one became one `x.Inline.Edges = x.Inline.Edges with { Edge = v };` line, in the order of the old writes (as D5.2). The 2 other files had none. The syntax check: a copy of each file at `HEAD` with only the mechanical rename (and the `with` form for the nested writes) equals the new file. No number, no unit, and no enum value changed. The 3 classes give 31 passed before the lock.
- **D6.4 The 2 extra files assert the old contract of gate B (recipe step 6), so each test is rewritten for the new contract.**
  - `StylePassTests.AnEmptyCascadeKeepsADirectWrite` became `AnEmptyCascadeRestylesAnElementWithNoInlineWrite`. The test runs one `Update`, puts a stale `BackgroundColor` in the style through `Unsafe.AsRef(in element.Style)`, marks `Style`, runs `Update` with an empty sheet, and expects the default. The file gets `using System.Runtime.CompilerServices;`.
  - `InlineStyleTests.AnElementWithNoInlineWriteAndNoSheetKeepsADirectWrite` became `AnInlineSetWithNoViewWriteRestylesWithNoSheet`. The test writes `InlineStorage` directly (no view write, thus no flag in the old code), marks `Style`, runs `Update` with no sheet, and expects the inline value.
  - A mutation run proved both: with the gate `!cascade.IsEmpty` put back in `StylePass.Visit`, both tests fail (and most of the layout fixtures fail, as the brief expects). The mutation is reverted.
- **Task 6.9:** `StyleReturnsARefToTheSameField` compares `Unsafe.AsRef(in first.Style)` refs with `Unsafe.AreSame`. `WriteThroughStyleOrLayoutMarksNothing` became `WriteThroughLayoutMarksNothing` (the `element.Style = default;` line goes). The new `StyleIsReadOnly` reads the attributes of the return parameter of the getter and matches the type name `IsReadOnlyAttribute`.
- **Task 6.10:** `Element.Style` is `public ref readonly ComputedStyle Style => ref _style;`. The remarks state that the style pass writes the style in `Update`, that the game writes `Inline`, and that the style holds the defaults before the first update. The remarks of `StyleStorage` lose the phase 6 note: only `StyleStorage` writes the style.
- **Task 6.11:** `StylePass.Visit` restyles each element with `Style`, also with an empty cascade. The field `Element.HasInlineWrite`, its write in `InlineStyle.MarkWritten`, and its `cref` in the remarks of `InlineStorage` go. `MarkWritten` now only marks `Style`. The D4.8 cleanup: `CopyInline` in `LayoutDifferentialTests.cs` loses its first line `element.Inline.Opacity = 1f;`, and its remarks lose the text about the flag.
- **The lock build:** no file of `src/` and no other project wrote through `Style`. The build gave `0 Warning(s)` and `0 Error(s)` at the first run after the lock.
- **Task 6.12 The grep output** (`src samples tests`):
  - `src/BrutalUi.Core/Layout/LengthEdges.cs:13:/// The fields are public, so a nested write such as <c>element.Style.Margin.Left = 4;</c> goes to the field and compiles.`
  - `src/BrutalUi.Core/Tree/ComputedStyle.cs:18:/// <c>element.Style.Width = 120;</c> goes to the field and compiles. A property gives a copy and error CS1612.`
  - `src/BrutalUi.Core/Rendering/Corners.cs:12:/// The fields are public, so a nested write such as <c>element.Style.CornerRadius.TopLeft = 4f;</c> goes to the field and compiles.`
  - The grep over `tests samples` with `wc -l` gives 0 (280 before).
- **Task 6.13 The result:** the build gives `0 Warning(s)` and `0 Error(s)`. The test total goes from 1138 to 1139 (the new `StyleIsReadOnly`): 1139 passed, 0 failed, 0 skipped. The smoke run prints `test-linux: PASS build, test, gpu, smoke (120 frames) in 9 s` and `exit=0`. `git diff -U0 main -- tests | grep -c '^[-+].*AssertRect'` gives 0, and the same check on `tests/BrutalUi.Core.Tests/Layout` gives 0.
- **Recipe step 3:** no file of the group holds a `ref ComputedStyle` local or parameter. **Recipe step 4:** no `Update` was added in the fixtures. Each test calls `LayoutFixture.Calculate`. **Recipe step 5:** no fixture file calls `MarkDirty`.
- **Finding for `src/` (follow-up, not changed):** the comment in `UiContext.UpdateStyles` (`src/BrutalUi.Core/Tree/UiContext.cs`, line 248) still tells that gate B in `StylePass.Visit` keeps the direct writes. The statement is now false. The brief allows no change of `UiContext.cs`, so the parent removes the second sentence of the comment.
- **Follow-up:** the doc comments of `ComputedStyle.cs`, `Corners.cs`, and `LengthEdges.cs` still show a write through `Element.Style` (task 11.8 and phase 7). Q2.1 (the `ref readonly InlineStyle` shape) stays open for the parent.
- **Open question:** none.

---

## Phase 7: Decisions

### Group 1 (tasks 7.1 to 7.7): The text members and the inheritance (2026-10-21)
- The result: the build gives `0 Warning(s)` and `0 Error(s)`. The test total goes from 1139 to 1158 (1158 passed, 0 failed, 0 skipped): 13 new tests in `TextInheritanceTests` (11 facts and 1 theory with 3 rows), and 6 new theory rows (3 in `StylePassTests.CompareGivesTheKindOfEachMember`, 3 in `InlineStyleTests.EachMemberOfTheViewWritesItsOwnMember`). Each label test passes with no change.
- The demo (`dotnet run --project tests/BrutalUi.Core.Tests/BrutalUi.Core.Tests.csproj -- --filter-method '*TextColorFlowsDownTheTree' --show-live-output on`) prints:
  - `root #FFFFFFFF`
  - `panel #FFCC00FF`
  - `child #FFCC00FF`
  - `badge #FF0000FF`
  - `child #FFFFFFFF` (after the removal of the class `panel` and one `Update`)
- The facts line: `facts: tree elements 1000, bytes 1026832, bytes per element 1026.8` (994.6 before, +32.2). The 3 members grow `ComputedStyle` and `StyleProperties` by 12 bytes each, plus the padding. The chain lines stay at 3031 and 5674 calls, and the stack probe stays at 507 levels. The 5000-level chain test of `PaintWalkTests` passes.
- `TheStylePassAllocatesNothingAfterWarmUp` and each allocation class still measure 0 bytes (each one passes in the full run).
- The I2 grep over `src` now finds 2 lines only: `LengthEdges.cs:13` and `Corners.cs:12` (task 11.8). The `ComputedStyle.cs` line is gone.
- **The blast radius.** `ComputedStyle.cs` (3 public fields and the remarks), `StyleProperties.cs` (3 enum members, `StyleMasks.Text`, 3 fields and properties, `Clear`, `ApplyTo`), `InlineStyle.cs` (3 view properties), `UiContext.cs` (3 create-info members, the field `_textDefaults`, both constructors, `UpdateStyles`, and the helper `CreateTextDefaults`), and `StylePass.cs` (`Run`, `Visit`, `Restyle`, the new `Inherit` and `TextChanged`, and a `Compare` overload). `StylePass.Run` has one caller (`UiContext.UpdateStyles`). No other file of `src` changed. `Element` gets no new field: the growth is inside the two structs.
- **D7.1 `Compare` has two overloads.** The plan form `Compare(in current, in next, out bool textChanged)` is the main form, and `Restyle` calls it. The 2-argument form stays as an internal overload that discards `textChanged`, so the other 6 test calls of phases 1 and 2 compile with no change (the theory `CompareGivesTheKindOfEachMember` now uses the 3-argument form). A text change gives `Paint`. `Rgba8` and `FontHandle` use `Equals`, and `FontSize` uses `float.Equals` (`NaN` to `NaN` is no change, proved in `TheSameTextValueGivesNoBit`).
- **D7.2 The inheritance step is a private helper `Inherit(ref next, in parentText, applied)`.** `Restyle` calls it only when `applied` does not hold all 3 text bits. `applied` starts with the inline mask and adds `rule.Properties.SetMask` of each matching rule. Thus a rule value or an inline value wins over the parent, and an inline value wins over a rule.
- **D7.3 The context defaults are one `private readonly ComputedStyle _textDefaults`**, with only the 3 text members set. Both constructors call `CreateTextDefaults(color, font, size)`. `null` gives `new Rgba8(255, 255, 255, 255)`, the pattern of `Image.CreateInfo.Tint`. `UpdateStyles` passes the field with `in`, so the call makes no copy. The cost is about 300 bytes for each context, not for each element. The create-info does not check `DefaultFontSize` (a negative value or `NaN`), as `Label.CreateInfo.Size` does not.
- **D7.4 The stack.** `Visit` gets one `in` parameter (a pointer). Before the child loop, it takes `ref readonly ComputedStyle style = ref element.StyleStorage` one time, and passes `in style` to each child. The 300-byte `next` local stays in `Restyle` (`NoInlining`). The `Inherit` helper takes refs only.
- **D7.5 The A3 marks.** After the write of the new style, `Restyle` calls `MarkDirty(DirtyFlags.Style)` on each child when `textChanged` is true. The first mark sends `DescendantStyle` to the element. `PropagateToAncestors` stops at the first ancestor that holds the bit, so each later mark costs one flag check. `Visit` reads `element.Dirty` again after `Restyle`. A mutation run proved the second read: without it, 5 of the 13 tests of `TextInheritanceTests` fail (`AParentChangeReachesAGrandchild`, `AnInlineValueWinsOverARule`, `AChildWithItsOwnValueKeepsIt`, `TextColorFlowsDownTheTree`, `AChainOfSixInherits`). The mutation is reverted.
- **D7.6 The first update.** A new element holds `default(ComputedStyle)`, which has transparent black text. The first restyle gives the inherited white, so `textChanged` is true, the element gets `Paint`, and each child gets `Style`. A new element holds `Style` and `Paint` already, so the first update does no extra restyle.
- **D7.7 Test changes outside the file list of the brief.** The theories of phases 1 and 2 read `Enum.GetValues<StyleProperty>()`, so they got the 3 new rows and needed a case for each. `StylePassTests`: `SetToANonDefaultValue` has 3 new cases, and `CompareGivesTheKindOfEachMember` also asserts `textChanged` for each member. `InlineStyleTests`: `SetThroughTheView` has 3 new cases. `EachMemberOfTheViewWritesItsOwnMember` compares against the warm style of the leaf, not `default(ComputedStyle)`, because the warm leaf now holds the inherited white (the old compare gave `Paint` for each member). `AValueThatIsNotAMemberThrows` uses `(StyleProperty)32`. `StylePropertiesTests.IsSetAndClearThrowForAValueThatIsNotAMember` uses the row 32 in place of 29, because 29 is now `Color`.
- **D7.8 The field remarks of `ComputedStyle`.** The remarks of `Display`, `BackgroundColor`, `BorderColor`, `CornerRadius`, and `Opacity` told the caller to call `MarkDirty` after a write. That text is false under the new contract, so each one now tells that a change marks `Layout` or `Paint`. The class remarks lose the CS1612 example of a write through `Element.Style`, and the example writes through `Inline`. The field docs of the 3 text members tell that `Label` reads its own values in this release (phase 8 removes the sentence).
- **D7.9 The field order.** `StyleProperties` puts `_fontSize` with the floats, `_color` with the colors, and `_font` (4 bytes) after the colors (D1.1). `ComputedStyle` puts the 3 public fields after `CornerRadius`, before the private opacity fields.
- **Task 7.4 follow-up of phase 6:** the false sentence about gate B in the comment of `UiContext.UpdateStyles` is gone. The comment now tells that each root gets the text defaults.
- **Edge cases that the plan does not name:**
  - A move of an element to a new parent: `AttachToParent` marks the element with `Style`, so the restyle gives the text of the new parent, and A3 sends a change down. `ANewChildTakesTheTextOfItsParent` proves the add and the move. It also proves the path through `DescendantStyle` only: the root, the middle, and the leaf give their current style to the next level with no restyle.
  - A child that sets only `Color` still inherits `Font` and `FontSize`, so a font change of the parent reaches the grandchild. `AChildWithItsOwnValueKeepsIt` proves it.
- **Follow-up for phase 10:** an element that leaves its parent and becomes a root through `AddRoot` gets no `Style` bit, so it keeps the text of the old parent until phase 10 makes `AddRoot` call `MarkSubtreeStyle`.
- **Follow-up for phase 8:** the 3 sentences "`Label` reads its own ... in this release" in `ComputedStyle.cs` go when the label reads the style. `Compare` gets the second out value `sizeTextChanged`.
- **Migration risk:** in this phase, an inline `Color` on a `Label` changes `Style.Color` but not the drawn text, because the label draws `Label.Color`. Phase 8 closes the gap.
- **Open question:** none.

---

## Phase 8: Decisions

### Group 1 (tasks 8.1 to 8.9): The label reads the text style (2026-10-21)
- **The users before task 8.1.** The grep of the brief found users in `Label.cs` and in the 9 files of the file list only. The first build after tasks 8.1 and 8.2 named 8 files, each in the file list: `HelloScreen.cs` (2 errors), `ElementAllocationTests.cs` (7), `ContentBoxTests.cs` (4), `LabelTests.cs` (14), `LabelTextTests.cs` (35), `LabelWrapTests.cs` (2), `ImGuiLabelTests.cs` (2), and `ImGuiRendererAllocationTests.cs` (8). No user in `src/BrutalUi.Framework`, `src/BrutalUi.Markup`, the benchmarks, or a shared tree. Thus no extra user moved.
- **The blast radius.** `Label.cs` (3 fields, 3 properties, 3 create-info members, the constructor, `PaintContent`, `MeasureLabel`, and the remarks), `StylePass.cs` (`Restyle`, the `Compare` overloads, `TextChanged` became `SizeTextChanged`, and the class remarks), and `ComputedStyle.cs` (3 doc sentences). `StylePass.Run` keeps its one caller. No other file of `src` changed.
- **The result.** The build gives `0 Warning(s)` and `0 Error(s)`. The test total goes from 1158 to 1160 (1160 passed, 0 failed, 0 skipped): 6 removed, 8 added (1 in `LabelTests`, 7 in `LabelTextTests`). The smoke run prints `test-linux: PASS build, test, gpu, smoke (120 frames) in 11 s` and `exit=0`. The grep `label\w*\.(Color|Font|Size)\b` over `src samples tests` gives 0 hits. The text "reads its own" in `src` gives 0 hits.
- The demo (`dotnet run --project tests/BrutalUi.Core.Tests/BrutalUi.Core.Tests.csproj -- --filter-method '*ALabelDrawsInTheInheritedTextStyle' --show-live-output on`) prints:
  - `rect 0,0,40,16`
  - `Text 0,0 font 0 size 16 #FFCC00FF wrap 40 "Hello"`
  - `rect 0,0,80,32` (after `panel.Inline.FontSize = 32`, one `Update`, and one `Draw`)
  - `Text 0,0 font 0 size 32 #FFCC00FF wrap 80 "Hello"`
- **The removed label tests (Q2 count: 6).** All 6 are in `LabelTextTests`: `SettingTheFontMarksLayoutAndPaint`, `SettingTheSameFontMarksNothing`, `SettingTheSizeMarksLayoutAndPaint`, `SettingTheSameSizeMarksNothing`, `SettingTheColorMarksPaint`, and `SettingTheSameColorMarksNothing`.
- **The added label tests.** `LabelTests.ALabelDrawsInTheInheritedTextStyle` (the demo). `LabelTextTests`: `ANewFontSizeMeasuresTheLabelAgain`, `ANewFontMeasuresTheLabelAgain`, `ANewColorDrawsTheNewColorWithNoMeasure`, `TheSameTextValueDoesNotMeasureTheLabelAgain`, `ALabelWithNoValueTakesTheContextDefaults`, `AMoveToANewParentMeasuresTheLabelInTheNewSize`, and `AFontSizeChangeDoesNotMeasureAnElementThatIsNotALabel`.
- **D8.1 `Compare` has three overloads.** The main form is now `Compare(in current, in next, out bool textChanged, out bool sizeTextChanged)`, and `Restyle` calls it. The 3-argument form stays as an internal overload that discards `sizeTextChanged`, so `StylePassTests.CompareGivesTheKindOfEachMember` (not in the file list) compiles with no change. The 2-argument form stays (D7.1). `sizeTextChanged` is true for a change of `Font` or `FontSize`. `textChanged` is `sizeTextChanged` or a change of `Color`. `Compare` itself gives no `Layout` for a text change, because only a label measures a text.
- **D8.2 The `Layout` bit.** `Restyle` adds `DirtyFlags.Layout` when `sizeTextChanged` is true and `element is Label` (the form of `UiContext.RefreshSubtree`). A plain element or an `Image` with a new font size gets `Paint` only. A mutation run proved the check: with `element is Image` in place of `element is Label`, 5 tests fail (`ALabelDrawsInTheInheritedTextStyle`, `ANewFontSizeMeasuresTheLabelAgain`, `ANewFontMeasuresTheLabelAgain`, `AMoveToANewParentMeasuresTheLabelInTheNewSize`, `TheSameTextValueDoesNotMeasureTheLabelAgain`). The mutation is reverted.
- **D8.3 The label reads the style through one `ref readonly` local.** `PaintContent` and `MeasureLabel` take `ref readonly ComputedStyle style = ref Style` one time, so the read makes no copy of the 300-byte struct. The draw keeps the old rules: `style.Color.ScaleAlpha(opacity)`, and no draw for an empty text or an alpha of 0.
- **D8.4 `Label.CreateInfo` holds `Text` and `LocalizedText` only, with no `required` member (Q3).** The struct remarks tell where the text values come from. `new Label(default(Label.CreateInfo))` and `new Label(new Label.CreateInfo())` both give an empty label with no inline value.
- **D8.5 The test moves.** In `LabelTests`, `LabelTextTests`, `LabelWrapTests`, `ContentBoxTests`, and `ImGuiLabelTests`, each create-info color and size became `label.Inline.Color` and `label.Inline.FontSize` on the same label (the `AddLabel` and `CreateLabel` helpers set them). In `ElementAllocationTests` and `ImGuiRendererAllocationTests`, the root sets `Inline.Color` and `Inline.FontSize`, and the labels inherit them. The frame body writes `label.Inline.Color`, `label.Inline.FontSize`, and (Core only) `label.Inline.Font`. Both tests still measure 0 bytes. `AssertLabelFrame` reads `label.Style.Color` and `label.Style.FontSize`. Each expected rect and vertex bound of the ImGui tests stays.
- **D8.6 `EncodesTheTextToUtf8`.** The 3 asserts of `defaults.Font`, `defaults.Size`, and `defaults.Color` went with the members. The test now asserts `defaults.InlineStorage.SetMask == 0`: a new label sets no inline value. `ALabelWithNoValueTakesTheContextDefaults` covers the computed defaults after an update.
- **D8.7 The sample.** `HelloScreen.Build` writes `greeting.Inline.Color = GreetingColor;` and `greeting.Inline.FontSize = GreetingFontSize;`. The label keeps font 0, and the class remarks keep the font 0 rule.
- **Edge cases that the plan does not name:**
  - A label that moves to a parent with another font size measures again: `AttachToParent` marks `Style`, the restyle gives a new size, and D8.2 adds `Layout`.
  - The same value on the parent gives no restyle of the label. An inline value on the label that equals the inherited value restyles the label, but the style does not change, so no measure runs.
  - `NaN` to `NaN` is no change (`float.Equals`). A change from 16 to `NaN` measures again, and the label gets a 0 by 0 rect (`FiniteSize`).
  - A context with no text defaults gives font 0, size 0, and opaque white. `RecordingRenderer` omits `wrap 0` from its dump.
- **Follow-up (outside the file list, not changed):** `docs/technical-design.md` line 504 still tells that `Label.Color`, `Label.Font`, and `Label.Size` are properties of the element, not style fields. The statement is now false. The parent removes or rewrites the sentence. The older feature docs under `docs/features/imgui-renderer/` are history and stay.
- **Migration risk:** a game that sets `Label.CreateInfo.Color`, `Font`, or `Size`, or the label properties, does not compile. The fix is a write to `label.Inline` or a rule. The old create-info required `Color`. A label with no value now draws in the context default color (opaque white).
- **Open question:** none.

---

## Phase 9: Decisions

### Group 1 (tasks 9.1 to 9.5): The element type id (2026-10-21)
- **D9.1 The type id mirrors the class id.** `ElementTypeId` copies `ClassId` (a `readonly struct` with `None`, `Value`, `IsNone`, the operators, and `ToString`). `ElementType` copies `ClassName`: one `Lock`, an ordinal `Dictionary`, and a `List` with a placeholder at index 0. `Intern` throws for null and for an empty name. `GetName` throws `ArgumentOutOfRangeException` for `None` and for an unknown id. The two tables are separate, so a class named `Label` and the type `Label` have unrelated ids.
- **D9.2 `Element.TypeId`.** It is `public virtual ElementTypeId TypeId => s_typeId;` with `Element` as the name. The doc states the override rule and the base-class rule. `Label` and `Image` (both `sealed`) override it with their own `private static readonly ElementTypeId` field, named `Label` and `Image`. The read is one virtual call and allocates nothing.
- **D9.3 The tests.** `ElementTypeTests` has 9 tests: same name, ordinal compare, three ids, built-in names, no override, own override, `GetName` round trip, `None` and unknown id, and null or empty `Intern`. The test names carry the prefix `ett-` because the table is global and xUnit runs classes in parallel. The total goes from 1160 to 1169.
- **Result.** The build gives `0 Warning(s)` and `0 Error(s)`. The tests give 1169 passed, 0 failed. The grep for reflection in `ElementType*.cs` prints nothing.
- **Open question:** none.

### Group 2 (tasks 9.6 to 9.9): The type selector (2026-10-21)
- **Result.** The build gives `0 Warning(s)` and `0 Error(s)`. The test total goes from 1169 to 1180 (1180 passed, 0 failed, 0 skipped): 11 new tests in `TypeSelectorTests`. `TheStylePassAllocatesNothingAfterWarmUp` still passes (0 bytes).
- **The demo** (`dotnet run --project tests/BrutalUi.Core.Tests/BrutalUi.Core.Tests.csproj -- --filter-method '*ATypeSelectorStylesEachLabel' --show-live-output on`) prints:
  - `label #FFCC00FF size 0`
  - `title label #00FF00FF size 24`
  - `title panel #00FF00FF size 0`
- **The grep** `grep -nE 'System\.Reflection|GetType\(\)|typeof\(' src/BrutalUi.Core/Style/*.cs src/BrutalUi.Core/Tree/ElementType*.cs` prints nothing (exit 1).
- **D9.4 `Selector` holds a type part and a class part.** The private constructor takes both. `Specificity` is `(class ? ClassWeight : 0) + (type ? TypeWeight : 0)` with the private constants 10 and 1, so it gives 1, 10, or 11. `IsEmpty` is true when both parts are none (D1.6), so the builder needs no change and accepts a type selector.
- **D9.5 `ForTypeAndClass` with one none part.** A none type with a real class gives the class selector. A real type with a none class gives the type selector. Both none throw `ArgumentException` (parameter name `type`). The doc comment states the rule. `ForType(None)` throws with the parameter name `type`.
- **D9.6 Two `Matches` overloads.** `Matches(Element, ElementTypeId)` does the work: it returns false for an empty selector, false when the type part differs from the given id, and then checks the class part. `Matches(Element)` stays as an internal overload that reads `element.TypeId` and calls the first one. Thus `StyleSheetTests` (not in the file list) compiles with no change. The pass calls the two-argument form only.
- **D9.7 `Restyle` reads `element.TypeId` one time** into a local before the rule loop. The pass allocates nothing.
- **D9.8 The match is exact.** A subclass with no `TypeId` override has the id of its base class and matches the base type rule. A subclass with its own id does not match it. `ATypeRuleDoesNotMatchASubclassWithItsOwnId` proves both, also through a sheet. That test asserts `BackgroundColor`, because `Color` inherits from the parent and hides the miss.
- **D9.9 Extra tests beyond the brief:** `ATypeRuleDoesNotMatchAnotherType`, `ATypeAndClassRuleNeedsBothParts`, `ForTypeThrowsForNone`, `ForTypeAndClassWithOneNoneGivesTheOtherSelector`, and `TheBuilderAcceptsATypeSelectorAndRejectsADefaultSelector`.
- **Files changed outside the list:** none. The `Selector` and `StylePass` doc comments lost the sentences "in this release a selector picks each element with one class" and "this is the phase 7 form".
- **Open question:** none.

---

## Phase 10: Decisions

### Group 1 (tasks 10.1 to 10.4): Multiple sheets and the AddRoot mark (2026-10-21)
- **D10.1 `RemoveSheet`.** `public bool RemoveSheet(StyleSheet sheet)` throws `ArgumentNullException` for null, gives `false` for an absent sheet (no cascade rebuild, no mark), and otherwise removes the sheet from `_sheets`, calls `RebuildCascade` (the method of `AddSheet`), and calls `MarkSubtreeStyle` on each root (A8). The doc comment states that the call allocates and belongs in load code. With no sheet left, the pass still restyles each element from the defaults and the inline set.
- **D10.2 `AddRoot` marks the new root.** `AddRoot` calls `root.MarkSubtreeStyle()` after the root flag is set (open scope question 2, the recommended option). The doc comments of `AddRoot` and `AddSheet` are updated: the `AddSheet` gap sentence is gone. The phase 7 follow-up is closed: `ADetachedChildThatBecomesARootTakesTheContextDefaults` proves that a child with a gold parent takes the green default of the new context.
- **Task 10.2: tests that assert `Dirty` after `AddRoot`.** The search found 18 test files that use both `AddRoot` and `Dirty`. After the edit, one test failed, and the other tests pass with no change (they assert `Dirty` after an `Update`, or on elements that the `AddRoot` mark leaves with the same bits, or on elements that hold the style bits already):
  - `DirtyTrackingTests.AddRootOfAChildMarksTheOldParentForLayout`: `tree.BTop.Dirty` was `DirtyFlags.None`. It is now `DirtyFlags.Style | DirtyFlags.DescendantStyle` (`BTop` has two children). Two lines are new: `tree.BLeaf.Dirty` and `tree.BOther.Dirty` are `DirtyFlags.Style`. The expectations for `tree.Root` (`Layout`) and `tree.ATop` (`None`) are unchanged. No expected rect changed.
- **Tests (8 new, file `tests/BrutalUi.Core.Tests/Style/MultiSheetTests.cs`):** `ALaterSheetWinsAndARemoveRestyles`, `AHigherSpecificityInTheFirstSheetWins` (checks both orders), `ThreeSheetsMergeInTheOrderOfAdd` (also removes the middle sheet), `RemoveSheetGivesFalseForAnAbsentSheet` (also null and a double remove), `RemoveTheLastSheetKeepsTheInlineValues`, `AddSheetAfterARemoveRestyles`, `ARootFromAnotherContextGetsTheNewSheets`, and `ADetachedChildThatBecomesARootTakesTheContextDefaults`.
- **The demo** (`dotnet run --project tests/BrutalUi.Core.Tests/BrutalUi.Core.Tests.csproj -- --filter-method '*ALaterSheetWinsAndARemoveRestyles' --show-live-output on`) prints:
  - `panel #3060C0FF`
  - `panel #1E222CFF` (after `RemoveSheet(theme)` and one `Update`)
- **The result:** the build gives `0 Warning(s)` and `0 Error(s)`. The test total goes from 1180 to 1188 (1188 passed, 0 failed, 0 skipped). The allocation tests still pass.
- **Files changed:** `src/BrutalUi.Core/Tree/UiContext.cs`, `tests/BrutalUi.Core.Tests/Style/MultiSheetTests.cs` (new), `tests/BrutalUi.Core.Tests/DirtyTrackingTests.cs` (expectation lines only).
- **Open question:** none.

---

## Phase 11: Decisions

### Group 1 (tasks 11.1 to 11.4): The allocation gate with an active sheet (2026-10-21)
- **The users of the shared tree.** `grep -rln 'ThousandElementTree' tests --include=*.cs` gives 6 files: `ThousandElementTree.cs`, `ThousandElementAllocationTests.cs`, `FactsReport.cs`, `ImGuiRendererAllocationTests.cs`, `TreeLayoutBenchmark.cs`, and `TreeDrawBenchmark.cs`. No file outside the group reads `GridSlot`, `Sidebar`, or `SidebarLabel`. No existing assert broke, so the sheet did not change and no assert changed.
- **The sheet (12 rules, in the order of add).** The build adds it with `context.AddSheet(sheet)` after `AddRoot`.
  1. Type `Element`: `FontSize` 14.
  2. Type `Label`: `Color` and `FontSize` 16. It matches no element of the tree.
  3. Class `.root`: `Color` (the inherited text color).
  4. Class `.item`: `BorderColor` with no border width.
  5. Class `.row`: `Color`.
  6. Class `.slot`: `BackgroundColor` (the slot fill, moved from the inline set).
  7. Class `.selected`: `BackgroundColor`, `Width` 100, and `MinWidth` 48.
  8. Class `.muted`: `Color`.
  9. `Element.item`: `FontSize` 13.
  10. `Element.row`: `FontSize` 12 and `BorderColor`.
  11. `Element.slot`: `BorderColor`.
  12. `Label.selected`: `Color`. It matches no element of the tree.
- **The classes.** The root has `root`, the 50 sidebar items have `item`, the 40 stat rows and the 73 settings rows have `row`, and the 120 grid slots have `slot`. The build puts no element in `selected` or `muted`. The tree exposes `Sheet`, `SelectedClass`, `MutedClass`, `RuleCount`, `ElementFontSize`, `SelectedMinWidth`, and the colors `RootTextColor`, `SlotColor`, `SelectedSlotColor`, and `MutedTextColor`.
- **D11.1 The slot fill moves from the inline set to the `.slot` rule.** An inline value wins over each rule. With the inline fill, the `.selected` fill does not reach the slot, and `TheStyleStepRestylesTheTree` cannot see a flip. The computed fill of each slot stays the same, so no rect, no draw count, and no fill count changes.
- **D11.2 The `.selected` fill wins by the order of add.** Rules 6 and 7 both have the specificity 10, and rule 7 comes later. Rule 11 (`Element.slot`, specificity 11) sets no fill, so it does not hide the `.selected` fill.
- **D11.3 The width values of `.selected`.** The inline width of grid slot 0 wins over the `Width` of the rule, so `Width` 100 reaches no computed style. The test asserts that the computed width stays 48. The rule also sets `MinWidth` 48 (equal to `SlotNarrowWidth`). That value reaches the computed style, so `Compare` gives `Layout` and the layout pass runs on the slot, but no rect changes (the slot width is 48 or 100). `TheMeasuredBodyChangesEachPart` asserts the slot width after the first round, with `.selected` on, and it passes with no change.
- **D11.4 No always-on rule sets a text color on the sidebar or below it.** A text color rule on an element stops the inheritance. Thus the type rule of `Element` sets `FontSize` only, and the `.item` rules set no `Color`. The sidebar label inherits the root color, or the `.muted` color when the sidebar holds `muted`.
- **D11.5 The always-on values change no rect and no draw call.** The tree holds no `Label`, so a font size or a text color reaches no measure and no text draw (D8.2). A border color with no border width gives no visible border (`PaintWalk` step 4). The ImGui gates and `TheMeasuredDrawDoesRealWork` pass with no change.
- **D11.6 "The context holds the sheet" uses the public API.** `UiContext` has no public read of its sheets. `TheTreeHoldsTheRequiredMix` asserts that a second `AddSheet` of the same sheet throws `InvalidOperationException`. The throw comes before each change, so the context does not change. The test also asserts `Rules.Length` 12, the slot fill, the inherited root color, and the font size 14, and that no element holds `selected` or `muted`.
- **D11.7 The style step.** `StyleKind = 8` and `KindCount = 9`. The first round adds `selected` to grid slot 0 and `muted` to the sidebar. The second round removes them. Then the step marks `DirtyFlags.Style` on each element of `tree.All`. The class doc and a kind table at the constants name the new kind.
- **`TheStyleStepRestylesTheTree` (new).** After one style step, the slot fill is `SelectedSlotColor`, the sidebar label color is `MutedTextColor`, the tooltip keeps `RootTextColor`, the computed width stays 48, the computed `MinWidth` is 48, and the slot rect does not change. The next style step gives the old values back.
- **Task 11.3: the gate blocks.** The plant `GC.KeepAlive(new object());` at the start of `Restyle` gives `exit=2` (7 failed of 1189: both gates of `ThousandElementAllocationTests`, `StylePassTests.TheStylePassAllocatesNothingAfterWarmUp`, `LayoutAllocationTests.LayoutStepAllocatesNothingAfterWarmUp`, `TreeAllocationTests.TreeChangesAndUpdateAllocateNothingAfterWarmUp`, `ElementAllocationTests.UpdateAndDrawAllocateNothingAfterWarmUp`, and `ImGuiRendererAllocationTests.LabelUpdateAndDrawAllocateNothingAfterWarmUp`). The restore with `cp` gives an empty `diff` (`diff=0`), and the grep for the plant gives 0 hits. The test run after the restore gives `exit=0`. `StylePass.cs` is an untracked new file of the feature, so `git diff --stat` prints nothing before and after the plant.
- **Task 11.4: the demo class runs.** `--filter-class '*ThousandElementAllocationTests'` gives `total: 9`, `failed: 0`, `succeeded: 9` in each of the 5 runs. Both gates measure 0 bytes in each run.
- **The facts line:** `facts: tree elements 1000, bytes 1041488, bytes per element 1041.5` (1026.8 before, +14.7). The growth is the build of the sheet and the cascade (the rule arrays, the sort keys, and the builder list) and the class storage writes. The chain lines stay at 3031 and 5674 calls, and the stack probe stays at 507 levels.
- **The smoke run:** `test-linux: PASS build, test, gpu, smoke (120 frames) in 17 s` and `exit=0`.
- **The result:** the build gives `0 Warning(s)` and `0 Error(s)`. The test total goes from 1188 to 1189 (1189 passed, 0 failed, 0 skipped).
- **Finding for `src/`:** none. The style pass allocates nothing on a full restyle of 1000 elements with 12 rules.
- **Files changed:** `tests/BrutalUi.Core.Tests/Shared/ThousandElementTree.cs`, `tests/BrutalUi.Core.Tests/ThousandElementAllocationTests.cs`, and this file. `src/BrutalUi.Core/Style/StylePass.cs` is the same as before the plant.
- **Open question:** none.

### Group 2 (tasks 11.5 to 11.9): The design docs, the changelog, and the hand-offs (2026-10-21)
- **Files changed:** `docs/technical-design.md`, `CHANGELOG.md`, `docs/features/style-system/human-testing.md`, `src/BrutalUi.Core/Layout/LengthEdges.cs` (doc comment), `src/BrutalUi.Core/Rendering/Corners.cs` (doc comment), and this file. No other file of `src/` changed. `tasks.md` is not ticked (the parent ticks).
- **Task 11.5 sections of `docs/technical-design.md`:** section 4 (the diagram line, the frame order, and the new paragraph "The style pass at the seam"), section 5 (the shape block with `TypeId`, `ref readonly Style`, `Inline`, `ElementTypeId`, `ElementType`, the new `UiContext.CreateInfo` members, `AddSheet`, `RemoveSheet`, the `Label` block; and the new paragraphs "The style members" and "The type id"; the design decision on `required`), section 6 (one sentence of "Inputs", which told the reader to write `element.Style.Width = 120;`), section 7 (the full "Model", the cascade order, the inheritance, the context defaults, the dirty rule table, the write rule, the sheets, the cost, and the design decisions), section 8 (the label draw rule and the new paragraph "The text style", which replaces the false sentence near line 504), and section 14 (the style tests, the style step of the allocation gate, and the third gate sentence).
- **D11.8 The doc shape is the real code, not the plan sketch.** `StyleRule` is a `readonly struct` with `Properties` (A6). `Element.Inline` is `public ref readonly InlineStyle` (D2.1). `StyleProperties.SetMask` and `Selector.Matches` are `internal`, so the design doc names neither as public API. `ComputedStyle` has 32 members (25 layout, 4 paint, 3 text), not the 29 of the first phases.
- **D11.9 Section 6 changed beyond the brief.** The sentence "The fields are public so that `element.Style.Width = 120;` compiles. A write sets no dirty bit" is false since phase 6, so section 6 now tells that game code writes `Inline`.
- **D11.10 The byte figures in section 7 are rounded.** The doc says "about 250 bytes" for `StyleProperties` and 1041.5 bytes for each element (the facts line of group 1). The first figure comes from D1.1 plus 12 bytes for each text member, not from a new measure.
- **Task 11.8:** both doc comments now show the local-variable form (`LengthEdges m = el.Inline.Margin; m.Top = Length.Px(12); el.Inline.Margin = m;` and the same for `Corners`) and tell that a nested write through a property gives CS1612, and that the whole-value write goes through `Element.Inline`.
- **Task 11.6:** one line under `### Added` and one under `### Changed` in `CHANGELOG.md`, with the name `style-system/stylesheet-model`.
- **Task 11.7:** `human-testing.md` keeps the 6 demo lines of phases 1, 2, 7, 8, 9, and 10, and gets the phase 11 demo line and the Windows line. The `Last Updated` stamp changed.
- **The lint (`ste-lint.py --fail-over 2.5`), words per 100:** `docs/technical-design.md` 1.38, `CHANGELOG.md` 1.69, `docs/features/style-system/human-testing.md` 0.73, and `docs/features/style-system/stylesheet-model/implementation.md` 1.56 (score only, not edited). No file is above 2.5.
- **The grep of task 6.12** (`grep -rnE '\.Style\.[A-Za-z]+(\.[A-Za-z]+)?\s*=[^=]' src samples tests --include=*.cs`) prints nothing (exit 1).
- **The result:** the build gives `0 Warning(s)` and `0 Error(s)`. The test total stays at 1189 (1189 passed, 0 failed, 0 skipped).
- **Open question:** Q2.1 is closed by the parent (`public ref readonly InlineStyle Inline`, see the session note of 14:19). `implementation.md` still shows the old shape. The plan is history, and the design doc shows the real shape.

## Review Group 1: Decisions

### CR-1: An attach marks the full subtree (2026-10-21)
- **Files changed:** `src/BrutalUi.Core/Tree/Element.cs` (`AttachToParent` and its remarks), `src/BrutalUi.Core/Tree/ElementList.cs` (one sentence of the `Add` remarks), `tests/BrutalUi.Core.Tests/Style/MultiSheetTests.cs` (one new test and one private constant), `tests/BrutalUi.Core.Tests/DirtyTrackingTests.cs` (two expectation lines), `docs/technical-design.md` (one sentence of section 7), `code-review.md` (the tick), and this file.
- **DR1.1 The fix.** `AttachToParent` calls `MarkSubtreeStyleCore(this)` in place of `_dirty |= DirtyFlags.Style;`, before the existing `PropagateToAncestors` call. Each element of the moved subtree gets `Style`. Each element of the subtree with a child gets `DescendantStyle`. The propagation then sends the bits of the moved element up from the new parent. `Children.Add` and `Children.Insert` both call `AttachToParent`, so both get the fix. `AddRoot` keeps its own `MarkSubtreeStyle` call. A detach and `RemoveRoot` do not change: a detached subtree restyles on its next attach.
- **DR1.2 The blast radius.** The callers of `AttachToParent` are `ElementList.Add` and `ElementList.Insert` only. The new bits are `Style` and `DescendantStyle` only, so no layout bit and no rect changes. The cost is one walk of the moved subtree on each attach. The walk is a recursion over `ReadOnlySpan<Element>` and allocates nothing. The attach runs in game code, not in the pass. The allocation gates (19 tests, with the reparent of `ThousandElementAllocationTests`) measure 0 bytes.
- **DR1.3 The `ElementList.Add` remarks changed beyond the brief.** The sentence "The child gets the `Style` bit" is false after the fix. It now tells that the child and each descendant of the child get the `Style` bit.
- **DR1.4 The new test.** `MultiSheetTests.ASubtreeThatMovesIntoAContextWithNoSheetTakesTheDefaults` moves a subtree of three levels (each element has the class `x`) with `Children.Add` from a context with the rule `.x { BackgroundColor: red }` to a context with no sheet. It asserts the style bits of each element after the add, and the default background (`#00000000`) on each element after `Update`. With the old one-element mark, the test fails (`Expected: Style | DescendantStyle`, `Actual: Style` on the moved element).
- **DR1.5 The changed `Dirty` expectations.** One test changed: `DirtyTrackingTests.AddMarksTheParentForLayoutTheChildForStyleAndSendsTheBitsOfTheChildUp`. The grandchild expectation changed from `Paint` to `Style | Paint`. The child expectation changed from `Style | DescendantPaint` to `Style | DescendantStyle | DescendantPaint`. The parent and root expectations did not change, because both already held `DescendantStyle`. The test name stays. No other exact `Dirty` assert failed.
- **The result:** the build gives `0 Warning(s)` and `0 Error(s)`. The test total is 1190 (1190 passed, 0 failed, 0 skipped). `git diff -U0 main -- tests | grep -c '^[-+].*AssertRect'` prints `0`.
- **Open question:** none.

## Review Group 2: Decisions

### CR-2: A test proves the tie-break of the cascade key (2026-10-21)
- **Files changed:** `src/BrutalUi.Core/Tree/UiContext.cs` (the new `Cascade` member), `tests/BrutalUi.Core.Tests/Style/MultiSheetTests.cs` (two tests, two constants, one helper), `code-review.md` (the ticks), and this file.
- **DR2.1 The reader.** `UiContext.Cascade` is `internal ReadOnlySpan<StyleRule>` over `_cascade`. Its doc comment names the tests of `BrutalUi.Core.Tests` as its only reader. The public API does not change.
- **DR2.2 The tests.** `TheCascadeKeepsTheOrderOfSheetAndRuleAtEqualSpecificity` adds three sheets of 100 rules with the same class selector. The `Width` of a rule is its index in the order of add. The test asserts that the cascade holds the widths 0 to 299 in order. `TheCascadeSortsBySpecificityThenBySheetThenByRule` uses the same three sheets with a mix of selectors (type, class, and both, by rule index modulo 3). It asserts the order (specificity group, sheet index, rule index), so it also proves the high bits of the key. Each test has 300 rules, well above the 16 items of the insertion sort.
- **DR2.3 The mutation M12.** Mutation: remove `| (uint)index` from the key line in `RebuildCascade`. Result: both new tests fail (2 failed, 0 succeeded). With the line restored, both pass. The mutation is not in the tree.
- **DR2.4 The helper.** `MultiSheetTests` got a private `WidthOf(int)` helper. It is a copy of the helper in `StyleSheetTests`, because that one is private to its class.

### CR-4: The remarks of `UiContext.Update` state the pass rule (2026-10-21)
- **DR2.5 The fix.** The last sentence "an element with no rule and no inline write keeps its style" is gone. The remarks now say: each element with the style bit gets a new style from the defaults, the matching rules, the inline set, and the parent text. An element with no rule and no inline value gets the defaults.
- **Open question:** none.

## Review Group 3: Decisions

### CR-5: The doc comments match the code (2026-10-21)
- **Files changed:** `Style/StylePass.cs`, `Style/StyleProperties.cs`, `Style/StyleSheet.cs`, and `Tree/UiContext.cs` (all in `src/BrutalUi.Core`).
- **DR3.1 The texts.** `StylePass` says the pass marks the children during the visit (A3). `StyleProperties` says a change of `Font` or `FontSize` also changes the layout of a `Label`. The `_cascade` comment names `AddSheet` and `RemoveSheet`. The `Builder.Add` parameter doc and exception text name `ForClass`, `ForType`, and `ForTypeAndClass`.
- **DR3.2 The crefs.** The `StyleProperties` text uses `see cref` for `StyleProperty.Font`, `StyleProperty.FontSize`, and `Label`. The build has 0 warnings, so the crefs resolve.

### CR-6: Three exception texts name the fix (2026-10-21)
- **Files changed:** `Tree/UiContext.cs` (`RemoveSheet` null text), `Style/StyleProperties.cs` (the mask bit text), and `Tree/ElementType.cs` (the id text).
- **DR3.3 The tests.** No test asserts the old texts (`grep` found none). The exception types do not change.
- **Open question:** none.

## Review Group 4: Decisions

### CR-3: The decimal specificity weights stay, and the docs state the limit (2026-10-21)
- **Files changed:** `src/BrutalUi.Core/Style/Selector.cs` (remarks only), `docs/technical-design.md` (section 7, the cascade order), the selector-matching part of `## Hand-offs` in this file, `code-review.md` (the tick and the resolution line), and this file.
- **DR4.1 The decision.** The run keeps the weights, because the user confirmed A5 in the interview ("class = 10, type = 1") and this run asks the user nothing. `Selector.Specificity`, the weights, and the tests do not change.
- **DR4.2 The limit.** The decimal sum keeps the CSS order only while each tier count stays below 10. One selector can hold 12 units of the class tier (one class and five pseudo-states in each of two compounds). selector-matching must replace the decimal weights with bit fields, or cap the counts, before it adds the name and the pseudo-states. The hand-off also names `TheSpecificityIsOneTenOrEleven` as a test that changes with the weights.
- **Open question:** none.

### CR-7: The changelog names the changes of behavior (2026-10-21)
- **Files changed:** `CHANGELOG.md` (two new lines under `### Changed`), `code-review.md` (the tick and the status), and this file.
- **DR4.3 The lines.** One line names the `AddRoot` mark (`Style` and `DescendantStyle` on the root and each descendant) and the attach mark of review group 1 (CR-1). One line names the default color of a label with no text value. Both lines carry the name `style-system/stylesheet-model`.
- **Open question:** none.

---

## Hand-offs

The lists below follow the real code. `Selector.Matches` and `StyleProperties.SetMask` are `internal` in `BrutalUi.Core`, so a feature inside the assembly reads them. A feature outside the assembly builds sheets through the public API.

### selector-matching
- Extend `Selector` with the name, the pseudo-states, the parent compound, and the combinator. The type part and the class part exist (`Selector.Type`, `Selector.Class`, `ForClass`, `ForType`, `ForTypeAndClass`). The private constructor takes both parts. The weights live in `Selector.Specificity` (the private constants `ClassWeight` 10 and `TypeWeight` 1). The decimal sum keeps the CSS order only while each tier count stays below 10. Replace the decimal weights with bit fields (for example 8 bits for each tier, so each count stays below 256), or cap the counts, before you add the name and the pseudo-states. One selector can hold 12 units of the class tier (one class and five pseudo-states in each of two compounds), and 12 units would win over one name at the weight 100. `TheSpecificityIsOneTenOrEleven` holds the numbers 1, 10, and 11, so change it with the weights. `StyleRule` stores the value at creation (`StyleRule.Specificity`), and `StyleSheet.Builder.Build` sorts by it.
- Extend `Selector.IsEmpty` with each new part (D1.6). The builder rejects `default(Selector)` through it, and `Matches` returns false for it.
- The pass calls `rule.Selector.Matches(element, elementType)` one time for each rule and element. `Restyle` reads `element.TypeId` one time before the rule loop (D9.7). A descendant match walks at most two ancestors inside `Matches`.
- A change of a class marks the element with `Style` (`ClassList.Add` and `Remove`). A change of the pseudo-states (`Element.States`) marks `Style` too. A descendant selector needs `DescendantStyle` or `Element.MarkSubtreeStyle()` on the subtree, because the pass visits only the elements that hold a style bit. `MarkSubtreeStyle` is `internal` and exists for a full subtree mark.
- The match of a type is exact (D9.8). A subclass without its own `TypeId` override matches the rule of its base class.
- An attach (`Children.Add`, `Children.Insert`) marks the full subtree of the moved element with `Style` (CR-1), so a descendant selector sees a move. A new pseudo-state or class change on an ancestor still needs its own subtree mark.

### style-parser
- Build a sheet with `StyleSheet.Builder.Add(in Selector, in StyleProperties)` and `Build()`. Map a type name with `ElementType.Intern(name)` and a class with `ClassName.Intern(name)`. The built-in type names are `Element`, `Label`, and `Image`.
- Each `StyleProperty` member (32) needs one property name of the format. Write a value with the `StyleProperties` setter. A set to the default value still sets the bit (D1.4). The edge sets (`Margin`, `Padding`, `BorderWidth`, `CornerRadius`) take one to four values. A rule sets all four edges of an edge set at once: there is no per-edge bit (Gaps 5 of the plan). A longhand such as `margin-top` needs a per-edge bit or a merge rule.
- The text members (`Color`, `Font`, `FontSize`) inherit, and the format must say so. A rule that sets only `color` still lets `font` and `font-size` inherit.
- Add the transition member to `StyleRule` with a new constructor overload. The existing constructor `StyleRule(in Selector, in StyleProperties)` stays.
- `StyleSheet` has no name and no source reference. The parser keeps its own map for error messages.

### transitions
- `StylePass.Restyle` holds the old style (`ref ComputedStyle current = ref element.StyleStorage`) and the new style (`next`) before the write `current = next`. A tween starts there, or right after `UiContext.UpdateStyles`, before the layout pass. `Compare` gives the kind of bits for the change.
- An inline value has no transition (out of scope of transitions). The pass writes the target value into `Element.Style`, and a transition system interpolates into the computed style.
- `Element.Style` is `ref readonly`, so a transition system writes through the internal `Element.StyleStorage`.
- A text change marks each child with `Style` (A3), so a transition on `Color` runs on each child that inherits it.

---

## Gotchas & Learnings

- A property set on a struct that a property or a method returns does not compile (CS1612), also when the struct is a `readonly struct`. A `ref` or `ref readonly` return is a variable, so the set compiles. See D2.1.
- A nested write through the view, `element.Inline.Margin.Top = 12;`, still gives CS1612. Write the whole value: `element.Inline.Margin = element.Inline.Margin with { Top = 12 };`.

---

## Testing Approach

### Unit Tests
**Files:** `tests/BrutalUi.Core.Tests/Style/*.cs`
- The model, the sort, the pass, the inline set, the inheritance, the type match, and the multi-sheet merge.

### Integration Tests
- The layout fixtures and `LayoutDifferentialTests` on the inline API, with no change of an expected rect.
- The allocation gates: `TheStylePassAllocatesNothingAfterWarmUp` and the style step of `ThousandElementAllocationTests`.

---

## Needs Human Verification

`/autorun-feature` adds one line per item that the verifier cannot check, for a person to test.

- [ ] Phase 1 demo: the developer runs `dotnet run --project tests/BrutalUi.Core.Tests/BrutalUi.Core.Tests.csproj -- --filter-method '*PaintsAPanelFromAClassRule' --show-live-output on` and reads `panel BackgroundColor #1E222CFF` and `plain BackgroundColor #00000000`.
- [ ] Phase 2 demo: the developer runs `dotnet run --project tests/BrutalUi.Core.Tests/BrutalUi.Core.Tests.csproj -- --filter-method '*AnInlineValueWinsOverARule' --show-live-output on` and reads `BackgroundColor #3060C0FF width 200`, then `BackgroundColor #1E222CFF width 200` after the clear.
- [ ] Phase 7 demo: the developer runs `dotnet run --project tests/BrutalUi.Core.Tests/BrutalUi.Core.Tests.csproj -- --filter-method '*TextColorFlowsDownTheTree' --show-live-output on` and reads `root #FFFFFFFF`, `panel #FFCC00FF`, `child #FFCC00FF`, `badge #FF0000FF`, then `child #FFFFFFFF` after the class removal.
- [ ] Phase 8 demo: the developer runs `dotnet run --project tests/BrutalUi.Core.Tests/BrutalUi.Core.Tests.csproj -- --filter-method '*ALabelDrawsInTheInheritedTextStyle' --show-live-output on` and reads `rect 0,0,40,16` and `Text 0,0 font 0 size 16 #FFCC00FF wrap 40 "Hello"`, then `rect 0,0,80,32` and `Text 0,0 font 0 size 32 #FFCC00FF wrap 80 "Hello"` after the new panel size.
- [ ] Phase 9 demo: the developer runs `dotnet run --project tests/BrutalUi.Core.Tests/BrutalUi.Core.Tests.csproj -- --filter-method '*ATypeSelectorStylesEachLabel' --show-live-output on` and reads `label #FFCC00FF size 0`, `title label #00FF00FF size 24`, and `title panel #00FF00FF size 0`.
- [ ] Phase 10 demo: the developer runs `dotnet run --project tests/BrutalUi.Core.Tests/BrutalUi.Core.Tests.csproj -- --filter-method '*ALaterSheetWinsAndARemoveRestyles' --show-live-output on` and reads `panel #3060C0FF`, then `panel #1E222CFF` after the removal of the theme sheet.
- [ ] Phase 11 demo: the developer runs `dotnet run --project tests/BrutalUi.Core.Tests/BrutalUi.Core.Tests.csproj -- --filter-class '*ThousandElementAllocationTests'` and reads a total of 9 with `failed: 0`.
- [ ] Windows run (stylesheet-model): on Windows, after `git pull --ff-only`, run `.\scripts\test-windows.ps1`. Expect the PASS line and `$LASTEXITCODE` 0.

---

## Performance Considerations

- The element grows by the size of the inline set (about 300 bytes). Tasks 1.1, 2.7, and 11.4 record the `bytes per element` line.
- `Restyle` has `NoInlining`, so the 300-byte `ComputedStyle` local stays out of the recursion frame.

---

## Native Dependencies

- None.

---

## Session Notes

### 2026-10-21 13:51
- `/autorun-epic` started the feature through `/autorun-feature` in the called mode. `/start-feature` created `context.md`, `tasks.md`, and `inbox.md`.
- The next session starts with phase 1, task 1.1.

### 2026-10-21 14:08
- Phase 1 is done in two groups (`phase-1.md`, `phase-1-g2.md`). The verifier (`phase-1-verify.md`) gave PASS: 0 warnings, 1091 tests with 0 failed, no reflection in `Style/`, and 0 bytes in `TheStylePassAllocatesNothingAfterWarmUp`. Demo: PASS.
- The inbox holds no line.

### 2026-10-21 14:19
- Phase 2 is done in one group (`phase-2.md`, tasks 2.1 to 2.7). The verifier (`phase-2-verify.md`) gave PASS: 0 warnings, 1138 tests with 0 failed, the smoke run PASS, and 0 bytes in the style pass gate. Demo: PASS.
- The orchestrator accepts D2.1: `Element.Inline` is `public ref readonly InlineStyle`, because a property that returns a struct by value gives CS1612 on `el.Inline.Width = 200`. The call syntax of Q1 stays. The plan text keeps the old shape. Task 11.5 writes the D2.1 shape into `docs/technical-design.md`.
- The orchestrator accepts D2.2: gate B reads the internal flag `HasInlineWrite`. Phase 6 removes the gate and the flag.
- Bytes per element: 746.5 before the feature, 994.6 after phase 2 (+248.1).
- The inbox holds no line.

### 2026-10-21 14:27
- Phase 3 (Base) is done in two groups (`phase-3.md`, `phase-3-g2.md`). The verifier (`phase-3-verify.md`) gave PASS: 0 warnings, 1138 tests with 0 failed, no write through `Style` in the 12 files, an `AssertRect` diff count of 0, no changed render value, 18 allocation tests passed, and the facts run exit 0. Demo: none (base phase).
- The write grep went from 3758 to 3437. D3.7: the 5000-level chain test calls `context.UpdateStyles()` before the draw.
- The inbox holds no line.

### 2026-10-21 14:40
- Phase 4 (Base) is done in two groups (`phase-4.md`, `phase-4-g2.md`). The verifier (`phase-4-verify.md`) gave PASS: 0 warnings, 1138 tests with 0 failed, no write through `Style` in the 12 files, an `AssertRect` diff count of 0, and an equal count of compared trees. Demo: none (base phase).
- No test of the old contract needed a rewrite (D4.6). D4.8: the helper `CopyInline` of `LayoutDifferentialTests` sets the flag of gate B with one inline write. The lock brief (`phase-6-g2.md`) removes that line. D4.9: `ChangeOneField` takes the element, not an `InlineStyle`.
- The inbox holds no line.

### 2026-10-21 14:48
- Phase 5 (Base) is done in two groups (`phase-5.md`, `phase-5-g2.md`). The verifier (`phase-5-verify.md`) gave PASS: 0 warnings, 1138 tests with 0 failed, no write through `Style` in the 12 files, an `AssertRect` diff count of 0, and no changed fixture number. Demo: none (base phase).
- D5.2 and D5.5: 179 nested writes became `LengthEdges.All(v)` or one `with` line for each edge.
- The inbox holds no line.

### 2026-10-21 15:56
- Phase 6 (Base) is done in two groups (`phase-6.md`, `phase-6-g2.md`). A session limit stopped the agent of group 2 after its last record. The orchestrator checked the tree and the docs, and the coordinator told the run to go on. The verifier (`phase-6-verify.md`) gave PASS: 0 warnings, 1139 tests with 0 failed, an `AssertRect` diff count of 0 against `main`, the 3 doc comment lines only in the write grep, `ref readonly` on `Element.Style`, no `HasInlineWrite`, and the smoke run PASS. Demo: none (base phase).
- D6.4: two tests of the old gate B contract are rewritten (`AnEmptyCascadeRestylesAnElementWithNoInlineWrite`, `AnInlineSetWithNoViewWriteRestylesWithNoSheet`).
- The stale gate B sentence in the comment of `UiContext.UpdateStyles` goes to the brief of phase 7.
- The inbox holds no line.

### 2026-10-21 16:07
- Phase 7 is done in one group (`phase-7.md`). The verifier (`phase-7-verify.md`) gave PASS: 0 warnings, 1158 tests with 0 failed, 18 allocation tests passed, the 5000-level chain test passed, and no `.Style.` write example in `ComputedStyle.cs`. Demo: PASS.
- Bytes per element: 1026.8 after phase 7 (+32.2 over phase 2).
- The follow-ups of D7 go to the briefs of phase 8 (the label doc sentences of `ComputedStyle.cs`, `sizeTextChanged`) and phase 10 (a detached child that becomes a root).
- The inbox holds no line.

### 2026-10-21 16:20
- Phase 8 is done in one group (`phase-8.md`). The parent merged the two groups of the plan, because the removal of the label members breaks the build of each file of group B until it moves.
- The first verifier run gave FAIL on one check of the verify brief: the grep for `required` matched the word in a doc comment of `Label.cs`. The parent corrected the check. The second run gave PASS: 0 warnings, 1160 tests with 0 failed, 18 allocation tests, the smoke run PASS, and no label member grep hit. Demo: PASS.
- Removed label tests (Definition of Done Q2): `SettingTheFontMarksLayoutAndPaint`, `SettingTheSameFontMarksNothing`, `SettingTheSizeMarksLayoutAndPaint`, `SettingTheSameSizeMarksNothing`, `SettingTheColorMarksPaint`, and `SettingTheSameColorMarksNothing`.
- The inbox holds one done line (the verifier FAIL of the check).

### 2026-10-21 16:35
- Phase 9 is done in two groups (`phase-9.md`, `phase-9-g2.md`) at the STANDARD tier. The verifier (`phase-9-verify.md`) gave PASS: 0 warnings, 1180 tests with 0 failed, 18 allocation tests, and no reflection in the match path. Demo: PASS.
- The inbox holds no open line.

### 2026-10-21 16:40
- Phase 10 is done in one group (`phase-10.md`) at the STANDARD tier. The verifier (`phase-10-verify.md`) gave PASS: 0 warnings, 1188 tests with 0 failed, and 18 allocation tests. Demo: PASS.
- Task 10.2: `DirtyTrackingTests.AddRootOfAChildMarksTheOldParentForLayout` expects `Style | DescendantStyle` on `BTop` and `Style` on its two children after `AddRoot`.
- The inbox holds no open line.

### 2026-10-21 16:54
- Phase 11 is done in two groups (`phase-11.md` at the ADVANCED tier, `phase-11-g2.md` at the STANDARD tier for the documentation work). The verifier (`phase-11-verify.md`) gave PASS: 0 warnings, 1189 tests with 0 failed, 13 element allocation tests, no write grep hit, no reflection, no planted line, the lint exit 0 for each changed doc, and the smoke run PASS. Demo: PASS.
- The orchestrator accepts D11.3: `.selected` also sets `MinWidth` 48, so the style step runs a real layout on the slot with no rect change.
- All 11 phases are done. The inbox holds no open line.

### 2026-10-21 17:21
- The cross-phase review (`review.md`) wrote `code-review.md` with 7 findings. Review groups 1 to 4 (`review-g1.md` to `review-g4.md`) resolved each one. The verifier (`review-verify.md`) gave PASS: 0 warnings, 1192 tests with 0 failed, the allocation classes, the seven slice demos, the lint, and the smoke run.
- CR-1 changed behavior: an attach marks the full subtree with style bits. CR-3 is resolved by text: the run keeps the confirmed A5 weights, and the hand-off tells selector-matching to replace them before it adds the name and the pseudo-states.
- The inbox holds no open line.

### 2026-10-21 17:34
- `/update-feature`: 112 of 112 tasks are done. `implementation.md` reads `Ready for Review`. The draft plan of style-system/selector-matching exists at `docs/features/style-system/selector-matching/implementation.md` (4 phases, 31 tasks, 4 open scope questions).
- Inbox bug: Verifier FAIL: the phase 8 grep for `required` in `Label.cs` matches the word in a doc comment (line 547), not a member modifier. (done: phase-8-verify.md, the check of the brief is corrected, no code change, the second verifier run gave PASS)
- The inbox holds no open line, so no bug goes to `docs/bugs/`.
- Final state: 1192 tests with 0 failed, 0 build warnings, 0 bytes in each allocation gate, bytes per element 1041.5, and the smoke run PASS.

---

*Update this file at the end of each work session. Run `/update-feature` before compacting conversations.*
