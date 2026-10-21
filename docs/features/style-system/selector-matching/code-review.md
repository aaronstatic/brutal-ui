# style-system/selector-matching - Code Review

**Reviewed:** 2026-10-21
**Reviewer:** Agent (cross-phase review of phases 1 to 4)
**Rules Applied:** 1. A correct match, 2. A correct specificity order, 3. Deterministic dirty bits, 4. Zero allocation, 5. API clarity and the hand-off to style-parser, 6. No regression, 7. Test quality, `AGENTS.md`, and the docs. The Vercel React rules do not apply to C#.
**Status:** Resolved - 6 findings, 5 fixed (review groups 1 to 3), 1 deferred to style-parser (CR-4)

---

## Summary

| Severity | Count |
|----------|-------|
| HIGH | 0 |
| MEDIUM | 1 |
| LOW | 5 |

The match is right for each probe. A compound needs each part, the name compare is ordinal, a child chain reads the parent only, and a descendant chain reads the parent and the grandparent only. The byte tiers keep the CSS order, and a later rule wins at equal specificity in one sheet and across two sheets. A class change, a state change, and a name change mark the full subtree, and a change to the same value marks nothing. A shipped test kills each of the 21 mutations of the match, the tiers, the slot checks, and the marks. The update allocates nothing with a name, a state, and a class change in the measured body.

The MEDIUM finding is a gap of the allocation gates. No shipped gate changes a name in its measured body, so a planted allocation in the `Name` setter fails no test (CR-1). The LOW findings are two stale sections of the design doc (CR-2, CR-3), the missing equality members and text form of the selector types (CR-4), two facts that the hand-offs do not state (CR-5), and stale doc text and size records (CR-6).

All probes ran in scratch copies of the repository under `/tmp/claude-1000/-home-aaronstatic-dev-brutal-ui/1041e7b1-7770-4054-8b67-c6d8d936fcf3/scratchpad/cr-selector-matching/`. The copy `repo` holds the probe class `tests/BrutalUi.Core.Tests/ReviewProbes.cs`, and the probes write their lines to `probes.txt`. Each copy `m01` to `m23` holds one mutation. The mutation runs build with `-p:TreatWarningsAsErrors=false`, because some mutations give a warning (for example unreachable code). The mutation runs use the Core test project only (993 tests), except M22 and M23, which use the full solution. No file of the repository changed, except this file.

1. **Baseline.** `dotnet build brutal-ui.sln` gives 0 warnings and 0 errors. `dotnet test --solution brutal-ui.sln` gives total 1238, failed 0.
2. **The mutations of the match (rule 1). A shipped test kills each one.**
   - M1, the descendant walk with no grandparent step: 7 failures (for example `ADescendantRuleMatchesTheParentAndTheGrandparent`, `TheStyleStepMatchesEachSelectorForm`).
   - M2, the descendant walk to the great-grandparent: 1 failure (`ADescendantRuleStopsAtTheGrandparent`).
   - M3, the child chain also reads the grandparent: 2 failures (`AChildRuleMatchesOnlyTheParent`, `ANameInAChain`).
   - M4, a compound with no state test: 15 failures. M5, with no class test: 30 failures. M6, with no name test: 6 failures.
   - M7, a case-insensitive name compare: 2 failures (`ANameRuleMatchesOnlyThatName`, `ANameInAChain`).
3. **The mutations of the tiers and the slot checks (rules 2 and 5). A shipped test kills each one.**
   - M8, the wrong shift `NameShift = 8`: 5 failures. M9, the states not counted: 8 failures. M10, the ancestor not counted: 4 failures.
   - M11, no empty check on the ancestor slot: 1 failure (`ChildAndDescendantThrowForAnEmptySlot`). M12, no state check: 2 failures. M13, no empty-name check: 1 failure (`ForThrowsForAnEmptyName`).
4. **The mutations of the marks (rule 3). A shipped test kills each one.**
   - M14, `ClassList.Add` with `MarkDirty(DirtyFlags.Style)`: 1 failure. M15, `ClassList.Remove` with it: 2 failures. M16, the `States` setter with it: 3 failures. M17, the `Name` setter with it: 3 failures.
   - M18 and M19, `ClassList.Add` and `ClassList.Remove` with no same-value return: 2 failures each. M20, the `States` setter with no same-value return: 2 failures (one of them is `ElementStateTests.SetToTheSameValueMarksNothing`). M21, the `Name` setter with no same-value return: 1 failure (`TheSameNameMarksNothing`).
5. **The planted allocations (rule 4).** M22, `GC.KeepAlive(new object());` at the start of `Selector.Matches(Element, ElementTypeId)`: 4 failures of 1238, the same 4 tests as DS4.6. M23, the same plant in the `Name` setter after the same-value check: 0 failures of 1238 (see CR-1).
6. **A deep chain.** A chain of 5000 levels on a 1 MB thread. `States` on the detached root, `AddRoot`, `UpdateStyles`, then `States`, a class, and a name on the root, and `UpdateStyles` again: each step passes, with no exception. A chain of 40000 levels on a 1 MB thread gives `Stack overflow.` in `AddRoot`, before the first setter. `AddRoot` and the setters use the same recursion (`MarkSubtreeStyleCore`), so the setters add no new depth limit. Not reported.
7. **The cost of a high hover (Release build, 200 rounds, the 1000-element tree).** A hover on the root restyles 1000 elements: `Update` best 118.0 us, mean 765.4 us. The old mark (`Style` on the root only) restyles 1 element: best 0.3 us. A hover on grid slot 0 restyles 3 elements: best 1.9 us. A hover on a leaf restyles 1 element: best 0.4 us. A2 and Risk 2 accept this cost (see CR-5 for the hand-off).
8. **The allocation gate with each change.** The 16-rule sheet of the shared tree. The measured body (100 steps) renames the header between `header` and `header-renamed`, sets `Hover` on grid slot 0, sets `Hover | Focus` on stat row 0, sets `Active` on the root, adds or removes `muted` on the sidebar, and runs `Update`: 0 bytes. The renamed header misses the name rule, the hovered slot matches the state rule, and the header matches again after the name comes back.
9. **The tie order.** Two rules of equal specificity, in both orders of add, in one sheet and in two sheets: `.a > .b` and `.a .b` (512), `.b:hover` and `.a > .b` (512), `.a .b` and `.b:hover` (512), `:hover > :hover` and `.a .b` (512), and `ForName("tie")` and `For(new CompoundSelector { Name = "tie" })` (65536). In each case the later rule wins.
10. **The sizes and the equality.** `CompoundSelector` is 24 bytes, `Selector` is 56 bytes, and `StyleRule` is 320 bytes. The default `Equals` of two selectors with equal names in two string instances gives `true`, and the hash codes agree. One `Equals` call allocates 880 bytes. `ToString` gives `BrutalUi.Core.Selector` (see CR-4).
11. **Rule checks.** `Directory.Packages.props`, each `.csproj`, `DirtyTrackingTests.cs`, and `ElementStateTests.cs` do not change against `ab41d74`. The grep for `System.Reflection`, `GetType()`, `typeof(`, `HasFlag`, `System.Linq`, and a nested lambda in `Selector.cs`, `CompoundSelector.cs`, `SelectorCombinator.cs`, and `ClassList.cs` gives 0 hits. The ImGui hits in `src/BrutalUi.Core` are in the unchanged `.csproj` and in two doc comments from `main`. `git diff -U0 ab41d74 -- tests | grep -c '^[-+].*AssertRect'` gives 0. The test diffs of `StyleSheetTests.cs` and `TypeSelectorTests.cs` hold only the specificity numbers and the two names of task 1.5. `StylePass.cs` changes doc comment lines only. Both sort keys put the specificity in the high 32 bits, and the largest value (2 names, 12 class units, 2 types) is 134146, so the key stays positive.

### Known items from the build

1. **Section 4 of `docs/technical-design.md` states the old mark** ("A change marks the element and its ancestors"). The item still holds. See CR-2.
2. **Section 14 of `docs/technical-design.md` does not list the four new test classes.** The item still holds, and the style-step bullet of section 14 also gives the old sheet and the old plant result. See CR-3.
3. **DS4.6: the ImGui gates do not run the match loop.** The item still holds. The match runs only in `UiContext.Update`, and the ImGui gates measure `Draw` only. The Core gates measure `Update` on the same 16-rule tree, and the plant fails 4 of them (probe 5). Thus the gap is not a gap of rule 4. The doc must state which gates guard the match. See CR-3.
4. **DS4.8: the size of `Selector` grew with the `string?` name.** The item still holds. `Selector` is 56 bytes (probe 10), and `context.md` still gives 12 bytes for one compound and about 28 bytes for the selector. See CR-6.
5. **The descendant walk stops at the grandparent (R1, question 2).** This is the user answer. Section 2 and section 7 of the design doc, the remarks of `Selector`, `Selector.Descendant`, `SelectorCombinator.Descendant`, `CHANGELOG.md`, and the style-parser hand-off state it. Not reported.
6. **DS1.7: `CompoundSelector` has no equality members.** The default `Equals` gives the right result, so no test is wrong. The cost and the text form matter for style-parser. See CR-4.
7. **DS2.3: the switch default of `Selector.Matches` returns `true`.** Only the private constructors set `_combinator`, and each factory gives a known value. Not reported.

---

## Findings

### CR-1: No allocation gate changes a name in its measured body

**Severity:** MEDIUM
**Rule:** 4. Zero allocation (and 7. Test quality)
**File:** `tests/BrutalUi.Core.Tests/ThousandElementAllocationTests.cs` lines 160 to 177 (`StyleKind`), `tests/BrutalUi.Core.Tests/Style/SubtreeMarkTests.cs` lines 418 to 431 (`ChainScene.Step`), `src/BrutalUi.Core/Tree/Element.cs` lines 53 to 79 (the `Name` setter)

**Problem:** Rule 4 asks for a state, a class, and a name change in the measured body. The style step of the 1000-element gate changes two classes and two states:

```csharp
tree.GridSlot.Classes.Add(tree.SelectedClass);
tree.Sidebar.Classes.Add(tree.MutedClass);
tree.GridSlot.States = PseudoStates.Hover;
tree.StatRow.States = PseudoStates.Hover;
```

`ChainScene.Step` changes one state and one class. No gate sets `Element.Name` after the warm-up. Probe M23 puts `GC.KeepAlive(new object());` in the `Name` setter, and the full suite gives 0 failures of 1238. Thus a later change of the setter, for example the intern table of option B of question 3, can allocate in each frame with no failed test. Probe 8 shows that the current code allocates 0 bytes for a name change, so the code is right. Only the guard is missing.

**Fix:** In the `StyleKind` case of `ThousandElementAllocationTests`, set `tree.Header.Name` to a second constant (for example `ThousandElementTree.OtherHeaderName = "header-renamed"`) in one round and back to `ThousandElementTree.HeaderName` in the other round. Use two `const string` values, so the step creates no string. In `TheStyleStepMatchesEachSelectorForm`, assert that the header loses `HeaderBorderColor` in the first round and gets it back in the second round. Update the class remarks, the kind table, the facts of `TheTreeHoldsTheRequiredMix` (no element has the second name after the build), and the style-step bullet of section 14. Run M23 again, and expect a failure of `UpdateAllocatesNothingAfterWarmUp`.

**Resolution:** The style step sets the header name to `OtherHeaderName` and back to `HeaderName`, and the M23 plant now fails 2 of 1238 tests (`UpdateAllocatesNothingAfterWarmUp` and `DrawAllocatesNothingAfterWarmUp`).

**Effort:** Low

### CR-2: Section 4 of the design doc states the old mark

**Severity:** LOW
**Rule:** 7. The docs match the source
**File:** `docs/technical-design.md` line 157 (section 4, "Dirty tracking")

**Problem:** The section says:

```
Each element carries dirty flags for style, layout, and paint. A change marks the element and its ancestors.
```

A class change, a state change, and a name change now mark the element, its full subtree, and its ancestors. An attach, `AddRoot`, `AddSheet`, and `RemoveSheet` also mark a full subtree. Section 4 is the overview that a new reader reads first, and it gives a smaller mark than the code.

**Fix:** Replace the second sentence with: "A change marks the element and its ancestors. A class, state, or name change, an attach, and a sheet change also mark the full subtree of the element with `Style` (section 7)."

**Resolution:** Section 4 now says that a class, state, or name change, an attach, and a sheet change also mark the full subtree of the element with `Style`.

**Effort:** Low

### CR-3: Section 14 of the design doc gives the old test set and the old gate facts

**Severity:** LOW
**Rule:** 7. The docs match the source (and 4. Zero allocation, DS4.6)
**File:** `docs/technical-design.md` lines 757 and 759 (section 14, "Test types")

**Problem:** The style-test bullet lists the test classes of stylesheet-model only. It does not name `PseudoStateSelectorTests`, `ChainSelectorTests`, `SubtreeMarkTests`, and `NameSelectorTests`. The style-step bullet gives the facts of stylesheet-model:

```
The 1000-element tree holds a sheet of 12 rules, added with `AddSheet`: type rules, class rules, and rules with a type and a class. The frame body adds and removes two classes ...
A planted `GC.KeepAlive(new object())` in `Restyle` fails 7 gates ... The class has 9 tests.
```

The sheet now holds 16 rules with a state rule, a child rule, a descendant rule, and a name rule. The frame body also sets and clears `Hover` on two elements. The class has 10 tests, with `TheStyleStepMatchesEachSelectorForm`. A plant in `Selector.Matches` fails 4 gates and no ImGui gate (DS4.6). The bullet does not tell a reader which gates guard the match loop.

**Fix:** Add one sentence to the style-test bullet: "`PseudoStateSelectorTests`, `ChainSelectorTests`, and `NameSelectorTests` check each part of a compound, each chain form, the walk bound, the tiers, and the factory checks. `SubtreeMarkTests` checks the subtree mark of a class, state, and name change." In the style-step bullet, give the 16 rules and the four new forms, the state change (and the name change of CR-1), the 10 tests, and the result of the plant in `Selector.Matches`: 4 Core gates fail, and no ImGui gate fails, because the ImGui gates measure `Draw` only and the match runs in `Update`. Keep the result of the plant in `Restyle`, or run it again and give the new count.

**Resolution:** Section 14 names the four new test classes and gives the 16 rules, the state and name changes, the 10 tests, and the plants in `Restyle`, `Selector.Matches` (4 Core gates, no ImGui gate), and the `Name` setter (2 gates).

**Effort:** Low

### CR-4: The selector types have no equality members and no text form

**Severity:** LOW
**Rule:** 5. API clarity and the hand-off to style-parser (DS1.7)
**File:** `src/BrutalUi.Core/Style/CompoundSelector.cs` line 31, `src/BrutalUi.Core/Style/Selector.cs` line 43, `docs/features/style-system/selector-matching/context.md` line 202 (the style-parser hand-off)

**Problem:** `CompoundSelector` and `Selector` use the default `ValueType.Equals`, `GetHashCode`, and `ToString`. The `string?` name makes `Equals` use reflection. Probe 10 gives the right result for two equal names in two string instances, but one call allocates 880 bytes. `ToString` gives `BrutalUi.Core.Selector`. The tests of style-parser compare a parsed selector with an expected selector, for example `Assert.Equal(Selector.Child(a, b), sheet.Rules[0].Selector)`. A wrong parse then fails with this text for both values:

```
Expected: BrutalUi.Core.Selector
Actual:   BrutalUi.Core.Selector
```

The test author then cannot see which part is wrong. Also, `==` does not compile for the two types. The hot path compares no selector, so the allocation rule holds.

**Fix:** Decide the API shape before style-parser starts (see the open questions). The recommended shape: `CompoundSelector` and `Selector` implement `IEquatable<T>` with `==` and `!=`. The compare uses the ids, the state bits, the combinator, and `string.Equals(..., StringComparison.Ordinal)` for the name. `GetHashCode` uses `HashCode.Combine`. `ToString` gives a CSS-like form, for example `Element.slot:hover > #title`, with the ids in the text if no name lookup exists for an id. Add a test for each member, and update DS1.7, the hand-off, and the design decision of section 7.

**Effort:** Medium

**Resolution:** Deferred to style-parser (out of scope). The fix adds public API (`IEquatable<T>`, `==`, `!=`, and a text form) that the accepted plan of this feature does not name, and the hot path compares no selector. The style-parser hand-off of `context.md` holds the finding, and the draft plan of style-parser decides the shape.

### CR-5: The hand-offs do not state two facts that the next features need

**Severity:** LOW
**Rule:** 5. The hand-off to style-parser (and 7. The hand-offs of `context.md`)
**File:** `docs/features/style-system/selector-matching/context.md` lines 195 to 213 (`## Hand-offs`)

**Problem:** Two facts are not in the hand-offs:

- **style-parser.** The line "`#name` to `Name`" does not say that `Name` holds the name without the `#`. The factories accept each string that is not empty, also `"#title"` and `"a b"`. Such a name matches only an element with that exact name, so a parser that keeps the `#` gives a rule that matches no element, and no error shows it. The same holds for `.class` and `ClassName.Intern`.
- **input and later features.** The line says that a hover on a high panel marks the full subtree. It gives no cost. Probe 7 measures a hover on the root of the 1000-element tree: 1000 restyled elements, `Update` best 118 us and mean 765 us in Release, against 0.3 us with the old mark. CSS also puts `:hover` on each ancestor of the hovered element. If the input layer does the same, each pointer move between two elements changes the state of a high ancestor, and each move restyles the full tree.

**Fix:** Add to the style-parser hand-off: "Give `Name` and `ClassName.Intern` the identifier without the `#` or the `.`. The factories do not check the characters of a name." Add to the input hand-off: "A hover on the root of the 1000-element tree costs about 0.1 to 0.8 ms in `Update` (probe 7 of `code-review.md`). Decide whether `:hover` goes to each ancestor, as in CSS. If it does, measure the cost of a pointer move before the input feature ends."

**Resolution:** The style-parser hand-off and the input hand-off of `context.md` now state both facts, and the cost line of the input hand-off cites probe 7.

**Effort:** Low

### CR-6: Doc comments and size records that do not match the code

**Severity:** LOW
**Rule:** 7. The doc comments and `context.md` match the source
**File:** `src/BrutalUi.Core/Style/Selector.cs` lines 61, 85 to 89, and 313, `docs/features/style-system/selector-matching/context.md` lines 66 and 92

**Problem:**

- The public summary of `Selector.Ancestor` and the doc of the field `_ancestor` say "the compound that the parent or the grandparent must match". For a child chain, only the parent must match it.
- The summary of `ThrowIfWrong` says "Throws when a compound holds no part or holds a state bit that is not a member". The method also throws for an empty name, and its remarks say so.
- The blast radius of phase 1 gives `CompoundSelector _subject (12 bytes)`, and the blast radius of phase 2 gives "The struct grows from 12 bytes to about 28 bytes". Probe 10 gives 24 bytes for a compound, 56 bytes for a selector, and 320 bytes for a rule. DS4.8 states that the old size is out of date, but the two lines still hold the old values.

**Fix:** Change the summary of `Selector.Ancestor` to: "Gets the compound that an ancestor must match: the parent for <see cref="SelectorCombinator.Child"/>, and the parent or the grandparent for <see cref="SelectorCombinator.Descendant"/>." Change the field doc the same way. Change the summary of `ThrowIfWrong` to name the empty name. In `context.md`, add the measured sizes to DS4.8 (24, 56, and 320 bytes), and mark the two old lines as replaced by DS4.8.

**Resolution:** The doc comments of `Selector.Ancestor`, `_ancestor`, and `ThrowIfWrong` now match the code, DS4.8 gives the measured sizes (24, 56, and 320 bytes), and the two old size lines carry the mark "replaced by DS4.8".

**Effort:** Low

---

## What's Already Good

- **The match path is small and clear.** `Selector.Matches` tests the subject first, and the two chain forms are two short cases with no loop. `CompoundSelector.Matches` tests the cheapest part first, uses bit math for the states, and reads `TypeId` of an ancestor only for a type part.
- **No defensive copy.** `Selector`, `CompoundSelector`, and `StyleRule` are `readonly struct` types, `StyleRule.Selector` is a `readonly` field, and the pass reads the rule through `ref readonly`. The larger selector (56 bytes) thus costs no copy in the rule loop.
- **The tiers have one home.** The three shift constants and `Tiers` hold each weight, and `StyleRule` stores the value at creation. No other code changed for the new weights.
- **Strong tests.** A shipped test kills each of the 21 mutations of the match, the tiers, the slot checks, and the marks. The chain tests check each case with the direct match and through the style pass, and both must agree. `AnAncestorWithNoTypePartReadsNoTypeId` proves the cost rule of the ancestor match with a counting subclass.
- **The factory checks name the slot.** The parameter name tells `parent`, `ancestor`, or `subject`, and each message names the fix.
- **The marks keep the invariant.** The setters reuse `MarkSubtreeStyle`, the same-value checks stay before the mark, and `DirtyTrackingTests` and `ElementStateTests` pass with no change.
- **The docs state the walk bound.** Section 2, section 7, the remarks of `Selector`, the changelog, and the hand-off each state that `.a .b` reads the parent and the grandparent only, and that this differs from CSS.
- **The shared tree proves each form on the real mix.** The four new rules change no rect and no draw call, and `TheStyleStepMatchesEachSelectorForm` checks each match, two misses, the rects, and the draw count.

---

## Recommended Fix Order

| Order | Finding | Reason |
|-------|---------|--------|
| 1 | CR-1 | The only MEDIUM. It closes a gap of rule 4, and CR-3 then records the new step. |
| 2 | CR-4 | It needs a decision on the API shape before style-parser starts. |
| 3 | CR-3 | Design doc text. Do it after CR-1, so the bullet gives the final step. |
| 4 | CR-2 | Design doc text only. |
| 5 | CR-5 | Hand-off text only. |
| 6 | CR-6 | Doc comment and `context.md` text only. |

---

## Progress

- [x] CR-1: No allocation gate changes a name in its measured body
- [x] CR-2: Section 4 of the design doc states the old mark
- [x] CR-3: Section 14 of the design doc gives the old test set and the old gate facts
- [x] CR-4: The selector types have no equality members and no text form (deferred to style-parser, see the resolution)
- [x] CR-5: The hand-offs do not state two facts that the next features need
- [x] CR-6: Doc comments and size records that do not match the code
