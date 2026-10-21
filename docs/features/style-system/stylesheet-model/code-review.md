# style-system/stylesheet-model - Code Review

**Reviewed:** 2026-10-21
**Reviewer:** Agent (cross-phase review of phases 1 to 11)
**Rules Applied:** 1. A correct cascade, 2. No missed member, 3. Deterministic dirty bits, 4. Zero allocation, 5. API clarity and the hand-off to selector-matching, 6. The layout stays exact, 7. Test quality, `AGENTS.md`, and the docs. The Vercel React rules do not apply to C#.
**Status:** Resolved - 7 findings, 7 resolved (review groups 1 to 4)

---

## Summary

| Severity | Count |
|----------|-------|
| HIGH | 0 |
| MEDIUM | 4 |
| LOW | 3 |

The cascade is right for each normal input. The pass applies the rules in the order of the cascade, the inline set wins, and the text members inherit. A removed class, a removed sheet, and a cleared inline value give the right result. The dirty bits come from the compare, and a shipped test kills each dirty-bit mutation of the probe set except one. The pass allocates nothing after warm-up, also with a class change, an inline set, a text change at the root, and a full restyle in the measured body. No expected rect changed in the migration.

The four MEDIUM findings are these. A subtree that moves to a root of another context through `Children.Add` keeps the rule values of the old context on each descendant (CR-1). No shipped test fails when the tie-break of the cascade key in `UiContext.RebuildCascade` goes (CR-2). The weights of `Selector.Specificity` (class 10, type 1) do not keep the CSS order for the grammar of selector-matching, and the hand-off tells the next feature to change the weights only (CR-3). The remarks of `UiContext.Update` state a gate-B rule that phase 6 removed (CR-4). The LOW findings are stale doc text, exception texts with no fix, and two missing changelog lines.

All probes ran in a scratch copy of the repository at `/tmp/claude-1000/-home-aaronstatic-dev-brutal-ui/1041e7b1-7770-4054-8b67-c6d8d936fcf3/scratchpad/cr-stylesheet-model/repo`. The copy has a new internal class `Mut` (`src/BrutalUi.Core/Mut.cs`). It reads the environment variable `CR_MUT`, so one build holds each mutation, and each test run switches on one mutation. The value 0 (or no variable) gives the original code. The scratch test class `ReviewProbeTests` holds the probes. No file of the repository changed, except this file.

1. **Baseline.** `dotnet build` gives 0 warnings and 0 errors. `dotnet test` gives total 1189, failed 0. The mutation runs use the Core test project only (944 tests with no probe).
2. **The mutations of `StylePass.cs` that a shipped test kills.**
   - M1, no second read of `element.Dirty` in `Visit`: 6 failures (`TextInheritanceTests` 5, `LabelTests.ALabelDrawsInTheInheritedTextStyle`).
   - M3, the rule masks do not stop the inheritance (`applied |= 0`): 9 failures.
   - M4, the inline mask does not stop the inheritance (`applied = 0`): 45 failures.
   - M5, M6, and M7, no inheritance of `Color`, `Font`, or `FontSize`: 16, 7, and 8 failures.
   - M8, `LayoutChanged` without `Overflow`: 3 failures. M22, `PaintChanged` without `Opacity`: 2 failures.
   - M9, `SizeTextChanged` without `Font`: 5 failures. M17, `textChanged` without `Color`: 8 failures.
   - M10, the `Label` check is always true: 3 failures (`ATextChangeGivesPaint` for `Font` and `FontSize`, `AFontSizeChangeDoesNotMeasureAnElementThatIsNotALabel`).
   - M16, no A3 mark of the children: 6 failures. M23, `Visit` clears `Style` only: 9 failures.
3. **The mutations of the model and the context that a shipped test kills.** M11, `ApplyTo` without `Opacity`: 14 failures. M18, `Clear` without the `Color` case: 1 failure (`ClearOfEachMemberGivesAnEmptySet`). M13, no tie-break in `StyleSheet.Builder.Build`: 1 failure (`TheOrderOfAddHoldsForManyRules`). M14, the `Inline.Font` setter without the same-value check: 1 failure. M15, `MarkSubtreeStyle` without `DescendantStyle`: 6 failures. M19, no type check in `Selector.Matches`: 6 failures. M20, `AddRoot` without `MarkSubtreeStyle`: 3 failures. M21, `RemoveSheet` without the mark of the roots: 4 failures.
4. **The mutations that no shipped test kills.** M2, the rules get the skip mask 0 in place of the inline mask: failed 0. The inline step writes over each skipped member, so the result does not change. D2.6 states that the skip only saves writes, so the mutant is equal to the code and is not reported. M12, no tie-break in the cascade key of `UiContext.RebuildCascade`: failed 0 (see CR-2).
5. **The member lists (rule 2).** `ApplyToWritesEachMember` asserts each of the 32 members. `CompareGivesTheKindOfEachMember` and `EachMemberOfTheViewWritesItsOwnMember` are theories over `Enum.GetValues<StyleProperty>()`, so each of the 32 members has its own row. `TheEnumCoversEachComputedStyleMember` and `EachMemberHasAPropertyOfTheComputedStyleType` compare the enum, `ComputedStyle`, and `StyleProperties` by reflection in the test. `ClearOfEachMemberGivesAnEmptySet` covers the `Clear` switch. M5 to M7 cover each of the 3 inherited members. Each sampled drop of a member fails a test.
6. **A deep chain.** A chain of 5000 levels, a new inline `Color` on the root, `UpdateStyles`, then `AddSheet` and `UpdateStyles` again. On a 1 MB thread: no failure, and the leaf gets the new color with `Layout, Paint`. On a 256 KB and a 128 KB thread: `Stack overflow.` (exit 134). The layout pass stops near 507 levels on 1 MB, so the style pass is not the depth limit. Not reported.
7. **The sheet and root orders.** `AddRoot` then `AddSheet`, and `AddSheet` then `AddRoot`: the root and the child get the rule value. One sheet in two contexts: both get the rule value, and a `RemoveSheet` in one context does not change the other. `RemoveRoot`, `RemoveSheet`, `AddRoot`: the default value. The same sheet after a removal: no throw, and the rule value comes back. A root that moves to another context with `AddRoot`: the rule value of the new context. A subtree that moves with `Children.Add` from a context with the sheet to a context with no sheet: `moved=#00000000 movedChild=#FF0000FF` (see CR-1).
8. **`NaN` and infinity.** `FlexGrow`, `FlexShrink`, `Opacity`, `FontSize`, and `CornerRadius` with `NaN`, `+Infinity`, and `-Infinity`, through a rule and through `Inline`. No call throws. A second restyle with the same rule value gives no `Layout` bit and no `Paint` bit. A second inline set of the same value gives `Dirty` `None`. The rect of the element is `0, 0, 100, 0` in each case. A `Length` member cannot hold `NaN` or infinity, because `Length.Px` and the implicit conversion give `Undefined` for them.
9. **The allocation gate.** A 65-element tree with a sheet of 2 rules. The measured body (200 steps) adds or removes a class, sets an inline `Height`, sets an inline `Color` on the root (A3 marks each child), calls `MarkSubtreeStyle` on the root, and runs `Update`: 0 bytes. Ten pairs of `AddSheet` and `RemoveSheet` with `Update`: 15048 bytes, as the doc comments of both methods state.
10. **Many rules of equal specificity in two sheets.** Two sheets of 20 rules each, each rule sets `Width` on one class. The result is the last rule of the second sheet (119 px). With M12 and with M13, the result is also 119 px, so this probe does not kill M12.
11. **Rule checks.** `Directory.Packages.props` and each `.csproj` do not change. No file of `src/BrutalUi.Core` references an ImGui package (the one ImGui word in `Label.cs` is a doc comment from `main`). The grep for `System.Reflection`, `GetType()`, `typeof(`, `HasFlag`, and LINQ in `src/BrutalUi.Core/Style` gives 0 hits. `git diff -U0 main -- tests | grep -c '^[-+].*AssertRect'` gives 0. Each changed line of the Layout fixtures (except `LayoutDifferentialTests.cs`) holds `Inline` or `Style`.

### Known items from the build

1. **The skip mask of the rule step has no effect on the result (M2).** D2.6 states it. Not reported.
2. **The `Label` check is a type check.** Gap 4 of the plan states it. Not reported.
3. **The two extra `Compare` overloads and `Selector.Matches(Element)`.** D7.1, D8.1, and D9.6 keep them for older tests. They are `internal`, and their remarks name the main form. The pass calls the main forms only. Not reported.
4. **`Element.Inline` is `public ref readonly InlineStyle` (D2.1).** The design doc shows the real shape. The plan keeps the old shape, by the decision of the run. Not reported.
5. **`LayoutDifferentialTests` copies the internal inline set (D4.8, D4.9).** The probe of D4.8 and the equal counts of task 4.10 hold. `CopyInline` no longer writes `Opacity` first (task 6.11). Not reported.
6. **The sheet of the 1000-element tree (D11.1 to D11.3).** The slot fill comes from the `.slot` rule, the `.selected` fill wins by the order of add, and the inline width wins over the rule width. `TheStyleStepRestylesTheTree` asserts each part. Not reported.
7. **`Overflow`, `Display`, and `BorderWidth` set `Layout` only (A9).** Conflict 5 of the plan states it. Not reported.
8. **The float members accept `NaN` and infinity with no check (probe 8).** The layout and the draw handle them, and the compare treats `NaN` as equal to `NaN`. D7.3 states that the context does not check `DefaultFontSize`. Not reported.
9. **The style pass is recursive (probe 6).** The layout pass is the depth limit. Not reported.

---

## Findings

### CR-1: A subtree that moves into another context keeps the rule values of the old context

**Severity:** MEDIUM
**Rule:** 1. A correct cascade (a removed sheet gives the right result)
**File:** `src/BrutalUi.Core/Tree/Element.cs` lines 268 to 277 (`AttachToParent`), `src/BrutalUi.Core/Tree/UiContext.cs` lines 205 to 220 (`AddRoot`), `docs/technical-design.md` section 7 ("The inheritance")

**Problem:** An attach marks only the attached element with `Style`:

```csharp
_parent = parent;
_dirty |= DirtyFlags.Style;
```

The sheets of a context do not depend on the parent in this feature, so a move inside one context is right. A move to an element of another context changes the sheets of each element of the subtree. The moved element restyles, but its descendants keep the rule values of the old context. Probe 7: a subtree with the class `x` moves from a context with the rule `.x { BackgroundColor: red }` to a context with no sheet. After `Update`, the moved element gives `#00000000`, and its child still gives `#FF0000FF`. `AddRoot` handles the same case with `MarkSubtreeStyle` (open scope question 2), but `Children.Add` does not. The design doc tells the reader that "a move to a new parent marks the element with `Style`" and gives no limit.

The same gap blocks selector-matching: a descendant selector (`.a .b`) makes the style of each descendant depend on its ancestors, so a move inside one context also needs a subtree mark.

**Fix:** In `AttachToParent`, mark the full subtree: call `MarkSubtreeStyleCore(this)` in place of `_dirty |= DirtyFlags.Style;`, before the existing `PropagateToAncestors` call. The propagation already sends `DescendantStyle` up from the new parent. Add a test to `MultiSheetTests`: a subtree moves with `Children.Add` from a context with a sheet to a context with no sheet, and each element of the subtree gets the default value. Run the suite, and update each exact `Dirty` assert (for example in `DirtyTrackingTests`) that reads a descendant after an add (the new bits are `Style` and `DescendantStyle` only, so no rect changes). Add one line to the `selector-matching` hand-off of `context.md` and one sentence to section 7.

**Effort:** Medium

### CR-2: No shipped test proves the tie-break of the cascade key

**Severity:** MEDIUM
**Rule:** 7. Test quality (and 1. A correct cascade)
**File:** `src/BrutalUi.Core/Tree/UiContext.cs` line 376, `tests/BrutalUi.Core.Tests/Style/MultiSheetTests.cs`

**Problem:** The cascade order of Decision 2 depends on the low 32 bits of the key:

```csharp
keys[index] = ((long)sheetRules[r].Specificity << 32) | (uint)index;
```

M12 removes `| (uint)index`, so `Array.Sort` sorts by the specificity only, and the sort is not stable. The 944 Core tests give failed 0. The multi-sheet tests use few rules, and `Array.Sort` uses an insertion sort for 16 items or fewer, which keeps the input order. The 12-rule sheet of the allocation tree is also below that size. Probe 10 (40 rules) also passes with M12. `StyleSheet.Builder.Build` has the same key, and `TheOrderOfAddHoldsForManyRules` (200 rules) kills its mutation M13. The cascade of the context has no such test, so a regression of the rule "a later sheet wins at equal specificity" (A4) stays green.

**Fix:** Add `internal ReadOnlySpan<StyleRule> Cascade => _cascade;` to `UiContext`, or read the order through the result. Add a test to `MultiSheetTests` that adds three sheets of 100 rules each, all with the same specificity, with the index of each rule in its `Width` value. Assert that the cascade holds the rules in the order (sheet index, rule index). Mix the specificities in a second sheet set (types, classes, and both), so the test also proves the high bits. Run M12 again, and expect a failure.

**Effort:** Low

### CR-3: The decimal specificity weights do not hold for the grammar of selector-matching

**Severity:** MEDIUM
**Rule:** 5. API clarity and the hand-off to selector-matching
**File:** `src/BrutalUi.Core/Style/Selector.cs` lines 22 to 31 and 50, `docs/features/style-system/stylesheet-model/context.md` line 433 (`## Hand-offs`), `docs/technical-design.md` section 7 (the cascade order), `implementation.md` Decision 5

**Problem:** The specificity is a weighted sum:

```csharp
public int Specificity => (Class.IsNone ? 0 : ClassWeight) + (Type.IsNone ? 0 : TypeWeight);
```

Decision 5 states that the sum keeps the CSS order "while each count stays below 10". The hand-off tells selector-matching to "change the weights in `Selector.Specificity` only". The selector-matching requirements put a class, a name, a type, and pseudo-states "in any combination on one compound", with five pseudo-states and a chain of two compounds. A pseudo-state counts in the class tier. Thus one selector can hold up to 12 units of the class tier (one class and five states in each compound). With the next decimal weight (100 for a name), 10 units of the class tier equal one name, and 12 units win over it. The CSS order then breaks, and the hand-off does not warn the next feature.

**Fix:** Pack the tiers in bit fields now, with one `int`: for example the name count in bits 16 to 23, the class tier in bits 8 to 15, and the type count in bits 0 to 7. Keep `StyleRule.Specificity` and the cascade key as they are (the value stays positive). Change `TheSpecificityIsOneTenOrEleven` to compare the order (type < class < type and class) and not the numbers, or to the new numbers. Update the remarks of `Selector`, section 7 of the design doc, and the hand-off: "the tiers are bit fields of 8 bits, so each count must stay below 256". If the team keeps the decimal weights, change the hand-off text to tell selector-matching to replace them before it adds the pseudo-states.

**Effort:** Low

### CR-4: The remarks of `UiContext.Update` state the removed gate B

**Severity:** MEDIUM
**Rule:** 7. The doc comments match the source
**File:** `src/BrutalUi.Core/Tree/UiContext.cs` lines 397 and 398

**Problem:** The public remarks of the main frame call end with:

```csharp
/// value wins over each rule. The pass marks the elements with a new style for the layout and the paint. In this
/// release, an element with no rule and no inline write keeps its style.
```

Phase 6 removed gate B. The pass now restyles each element with `Style` from the defaults, also with no sheet (`AnEmptyCascadeRestylesAnElementWithNoInlineWrite`). The sentence gives the opposite of the write rule of section 7 ("A direct write would not survive the next restyle"). A reader of the API doc, or the agent of the next feature, gets a wrong model of the pass.

**Fix:** Replace the last sentence with: "Each element with the style bit gets a new style from the defaults, the matching rules, the inline set, and the parent text. An element with no rule and no inline value gets the defaults."

**Effort:** Low

### CR-5: Other doc comments keep the text of earlier phases

**Severity:** LOW
**Rule:** 7. The doc comments match the source
**File:** `src/BrutalUi.Core/Style/StylePass.cs` line 13, `src/BrutalUi.Core/Style/StyleProperties.cs` line 20, `src/BrutalUi.Core/Tree/UiContext.cs` line 94, `src/BrutalUi.Core/Style/StyleSheet.cs` lines 43 and 52

**Problem:** Four texts describe an earlier phase:

- `StylePass.cs` line 13: "because a later phase marks the children of an element during the visit". Phase 7 added the A3 marks, so the pass marks them now.
- `StyleProperties.cs` line 20: "A later release also changes the layout of a label." Phase 8 made `Font` and `FontSize` mark `Layout` on a `Label`, and `Color` marks `Paint` only.
- `UiContext.cs` line 94: "`AddSheet(StyleSheet)` builds the array again." `RemoveSheet` also builds it.
- `StyleSheet.cs` lines 43 and 52: the parameter doc and the exception text of `Builder.Add` name `Selector.ForClass` only. `ForType` and `ForTypeAndClass` exist since phase 9.

**Fix:** Change each text: "because the pass marks the children of an element during the visit (A3)". "A change of `Font` or `FontSize` also changes the layout of a `Label`." "`AddSheet` and `RemoveSheet` build the array again." "Create it with `Selector.ForClass`, `Selector.ForType`, or `Selector.ForTypeAndClass`."

**Effort:** Low

### CR-6: Three exception texts do not name the fix

**Severity:** LOW
**Rule:** 5. Each exception names the fix
**File:** `src/BrutalUi.Core/Tree/UiContext.cs` line 305, `src/BrutalUi.Core/Style/StyleProperties.cs` line 860, `src/BrutalUi.Core/Tree/ElementType.cs` line 61

**Problem:** The other new exceptions name the fix (for example "Build a sheet with StyleSheet.Builder.Build" in `AddSheet`). These three give the fault only:

```csharp
throw new ArgumentNullException(nameof(sheet), "The sheet is null.");
throw new ArgumentOutOfRangeException(nameof(property), property, "The value is not a member of StyleProperty.");
throw new ArgumentOutOfRangeException(nameof(id), id.Value, "The element type table does not hold this id.");
```

**Fix:** Add one sentence to each text: "Pass a sheet that AddSheet added." "Use a named member of StyleProperty, from FlexDirection to FontSize." "Get an id from ElementType.Intern or Element.TypeId."

**Effort:** Low

### CR-7: The changelog does not name two changes of behavior

**Severity:** LOW
**Rule:** 7. `CHANGELOG.md` matches the source
**File:** `CHANGELOG.md` line 20 (`### Changed`)

**Problem:** The `### Changed` line names the `ref readonly` style and the removed label members. It does not name two changes that a game can see:

- `AddRoot` now marks the new root and each descendant with `Style` and `DescendantStyle` (D10.2). The expected `Dirty` of `DirtyTrackingTests.AddRootOfAChildMarksTheOldParentForLayout` changed for this reason.
- A label with no text value now draws in the default color of the context (opaque white). The old `Label.CreateInfo.Color` was `required`, and a game had to give a color.

**Fix:** Add both items to the `### Changed` line or as two new lines, with the name `style-system/stylesheet-model`.

**Effort:** Low

---

## What's Already Good

- The full rebuild of Decision 1 has no reset path, so a removed class, a removed sheet, and a cleared inline value give the right result with no special case. Probe 7 proves each order of `AddSheet`, `RemoveSheet`, `AddRoot`, and `RemoveRoot` in one context and a sheet in two contexts.
- The second read of `element.Dirty` in `Visit` is real and proved: M1 fails 6 tests. The pass clears both style bits after the subtree, and M23 fails 9 tests.
- Each member list covers the 32 members, and each list has a theory or an assert for each member (probe 5). The reflection tests that count the members live in the test project, not in the match path.
- The compare gives the bits from the values, so an unchanged value, `NaN` to `NaN`, and an unset `Opacity` against a set 1 give no bit (probe 8).
- The pass allocates nothing with a class change, an inline set, a text change at the root, and a full restyle in the measured body (probe 9). The doc comments of `AddSheet`, `RemoveSheet`, and `StyleSheet.Builder` state that they allocate.
- `Element.Style` is `ref readonly`, and the only write path of a game is `el.Inline.<Member> = value`. The `ref readonly InlineStyle` shape of D2.1 keeps the call syntax of the plan and costs 8 bytes for each element.
- The migration changed no expected rect. `LayoutDifferentialTests` keeps its counts (6534 root comparisons for the seed 20261017).
- The hand-off section of `context.md` follows the real code: the internal members, the type id names, the missing per-edge bit, and the place of a tween in `Restyle`.

---

## Recommended Fix Order

| Order | Finding | Reason |
|-------|---------|--------|
| 1 | CR-2 | A small test that guards the core cascade order. Do it first, so the later fixes run against it. |
| 2 | CR-1 | A real stale style. It touches the attach path of the tree and some exact `Dirty` asserts. |
| 3 | CR-3 | The specificity slot of the next feature. Fix it before selector-matching starts. |
| 4 | CR-4 | One sentence in the public doc of `Update`. |
| 5 | CR-5 | Doc text only. |
| 6 | CR-6 | Exception text only. |
| 7 | CR-7 | Changelog text only. |

---

## Progress

- [x] CR-1: A subtree that moves into another context keeps the rule values of the old context
- [x] CR-2: No shipped test proves the tie-break of the cascade key
- [x] CR-3: The decimal specificity weights do not hold for the grammar of selector-matching
  **Resolution:** kept A5. The hand-off and the docs state the limit. selector-matching decides the weights.
- [x] CR-4: The remarks of `UiContext.Update` state the removed gate B
- [x] CR-5: Other doc comments keep the text of earlier phases
- [x] CR-6: Three exception texts do not name the fix
- [x] CR-7: The changelog does not name two changes of behavior
