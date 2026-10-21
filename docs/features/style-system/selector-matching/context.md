# style-system/selector-matching - Context & Decisions

**Last Updated:** 2026-10-21 22:06
**Current Phase:** Done (phases 1 to 4 and the cross-phase review)
**Status:** ✅ Ready for Review

---

## Quick Status

**What's Done:**
- ✅ The plan exists: `implementation.md` (4 phases, 31 tasks). The user accepted the recommended answer of each of the four open scope questions.
- ✅ `/start-feature` created `context.md`, `tasks.md`, and `inbox.md`.
- ✅ Phase 1 (tasks 1.1 to 1.7): `CompoundSelector`, `Selector.For`, the byte tiers, and the hover demo. Tests 1192 to 1205, 0 failed. The verifier gave PASS and Demo: PASS.
- ✅ Phase 2 (tasks 2.1 to 2.9): `SelectorCombinator`, `Selector.Child` and `Descendant`, the bounded walk, and the subtree mark on a class or state change. Tests 1205 to 1225, 0 failed. No existing `Dirty` assert changed. The verifier gave PASS and Demo: PASS.
- ✅ Phase 3 (tasks 3.1 to 3.6): the name part, `Selector.ForName`, the name tier, the mark on a name change, and the builder text that names each factory. Tests 1225 to 1237, 0 failed. The verifier gave PASS and Demo: PASS.
- ✅ Phase 4 (tasks 4.1 to 4.9): the 16-rule sheet of the shared tree, the state change in the style step, the planted allocation (exit 2, then exit 0 after the restore), the design doc, the `StylePass.cs` remarks, `CHANGELOG.md`, the human testing list, the lint, and the hand-offs. Tests 1237 to 1238, 0 failed. Bytes per element 1046.6. The verifier gave PASS and Demo: PASS.
- ✅ The cross-phase review: 6 findings (1 MEDIUM, 5 LOW). Review groups 1 to 3 fixed CR-1, CR-2, CR-3, CR-5, and CR-6. CR-4 is deferred to style-parser. Tests stay at 1238, 0 failed. The verifier `review-verify.md` gave PASS, the four slice demos pass, and the smoke run passes.

**What's Next:**
- 📋 A person runs the items of "Needs Human Verification" (the 4 slice demos and the Windows run).
- 📋 `/autorun-epic` commits the feature, then builds style-system/style-parser after the user answers the open scope questions of its draft plan (CR-4 is question 2).

**Blockers:**
- None

---

## Key Files

### Core Implementation
- `src/BrutalUi.Core/Style/CompoundSelector.cs` - one compound: the type, the class, the name, and the pseudo-states (new)
- `src/BrutalUi.Core/Style/SelectorCombinator.cs` - `None`, `Child`, and `Descendant` (new)
- `src/BrutalUi.Core/Style/Selector.cs` - the subject compound, the ancestor compound, the combinator, the byte tiers, and the match
- `src/BrutalUi.Core/Tree/Element.cs` - the `States` and `Name` setters mark the subtree
- `src/BrutalUi.Core/Tree/ClassList.cs` - `Add` and `Remove` mark the subtree of the owner

### Related Files
- `docs/features/style-system/selector-matching/implementation.md` - the implementation plan
- `docs/features/style-system/stylesheet-model/context.md` - the decisions D1.x to D11.x, CR-1, CR-3, and the hand-offs
- `src/BrutalUi.Core/Style/StylePass.cs` - the call seam `Selector.Matches(Element, ElementTypeId)` (no code change)

---

## Important Decisions

The plan holds Decisions 1 to 7 and the Confirmed Assumptions A2, A8, and A9. This section records the decisions of the run.

### Decision R1: The user accepted the recommended answer of each open scope question
**Date:** 2026-10-21
**Context:** The draft plan lists four open scope questions. `/autorun-epic` passed the answers of the user.
**Decision:** Question 1: one byte for each specificity tier, `(names << 16) | ((classes + states) << 8) | types`. Question 2: a descendant chain walks to the grandparent only. Question 3: a name rule uses an ordinal string compare against `Element.Name`. Question 4: phase 1 uses the hover demo `AHoverRuleRestylesTheButton`.
**Rationale:** Each answer is the recommended option of the plan, so the plan does not change.
**Impact:** None beyond the plan.

---

## Phase 1: Decisions

### Group 1 (tasks 1.1 to 1.7): The compound, the pseudo-states, and the byte tiers (2026-10-21)
- **Task 1.1 The baseline.** The base commit is `ab41d74c6df38032f8b48b895225bdae9eb1bac0`. The build gives `0 Warning(s)` and `0 Error(s)`. The test total is 1192 (1192 passed, 0 failed, 0 skipped). The facts run gives `facts: tree elements 1000, bytes 1041488, bytes per element 1041.5`.
- **The result.** The build gives `0 Warning(s)` and `0 Error(s)`. The test total goes from 1192 to 1205 (13 new tests in `PseudoStateSelectorTests`: 8 facts and 5 theory rows, 0 failed). `StylePass.cs`, `StyleRule.cs`, `Directory.Packages.props`, and each `.csproj` do not change. `TheStylePassAllocatesNothingAfterWarmUp` and `ThousandElementAllocationTests` pass.
- **The demo prints:**
  - `button #1E222CFF`
  - `button hover #3060C0FF`
  - `button #1E222CFF`
- **The grep** `grep -nE 'System\.Reflection|GetType\(\)|typeof\(|HasFlag' src/BrutalUi.Core/Style/*.cs` prints nothing (exit code 1).
- **The blast radius.** `Selector` changes its private fields only: one `CompoundSelector _subject` (12 bytes, replaced by DS4.8) in place of two ids (8 bytes). The public members `Class`, `Type`, `Specificity`, and the three shortcuts keep their signatures. The callers: `StyleRule` (reads `Specificity` at creation), `StyleSheet.Builder.Add` (reads `IsEmpty`), `StyleSheet.Builder.Build` and `UiContext.RebuildCascade` (sort keys with the specificity in the high 32 bits), and `StylePass.Restyle` (calls `Matches(Element, ElementTypeId)`). No caller changes. Only `BrutalUi.Core.Tests` asserts specificity numbers. Framework, Markup, ImGui, the benchmarks, and the samples use no selector.
- **Task 1.5 The changed lines.**
  - `StyleSheetTests.cs` line 17: `ForClassGivesTheClassAndASpecificityOfTen` becomes `ForClassGivesTheClassAndTheSpecificityOfOneClass`. Line 19: the `nameof` follows the new name.
  - `StyleSheetTests.cs` line 24: `Assert.Equal(10, selector.Specificity)` becomes `256`.
  - `StyleSheetTests.cs` line 86: `Assert.Equal(10, rule.Specificity)` becomes `256`.
  - `TypeSelectorTests.cs` line 59: `TheSpecificityIsOneTenOrEleven` becomes `TheSpecificityPutsEachTierInItsOwnByte`. Line 61: the `nameof` follows the new name.
  - `TypeSelectorTests.cs` line 65: `10` becomes `256`. Line 66: `11` becomes `257`.
  - `TypeSelectorTests.cs` line 109: `10` becomes `256`. Line 110: `11` becomes `257`.
  - `TypeSelectorTests.cs` line 196: `10` becomes `256`.
- **DS1.1 The tier helper.** `Selector` holds a private static `Tiers(in CompoundSelector)` that gives the class tier and the type tier of one compound. `Specificity` calls it one time for the subject. Phase 2 adds the ancestor with `+`, because no tier count of the grammar reaches 256, so no carry crosses a byte. Phase 3 adds the name tier inside the helper.
- **DS1.2 `NameShift` exists now and has no use yet.** The three private shift constants (`NameShift` 16, `ClassShift` 8, `TypeShift` 0) keep each weight in one place. The compiler gives no warning for an unused constant, and the editorconfig does not raise IDE0051. Phase 3 uses `NameShift`.
- **DS1.3 The known state bits are a private constant of `Selector`** (`KnownStates`, the OR of the five members). `For` rejects a compound when `(States & ~KnownStates) != None`. Thus a negative value such as `(PseudoStates)(-1)` also throws. The test `ForThrowsForAWrongCompound` checks `32`, `Hover | 32`, and `-1`.
- **DS1.4 The order of the checks in `For`.** The empty check comes first, then the state check. Both throws use the parameter name `subject`. A compound with only an unknown bit (for example `32`) is not empty, so the state check rejects it.
- **DS1.5 `Subject` is an expression-bodied property over the private field** (`public CompoundSelector Subject => _subject;`). The public shape is a get-only property, as the plan states. The getter returns a copy of 12 bytes. The match path reads `_subject` directly, so it makes no copy.
- **DS1.6 `CompoundSelector.Matches` on an empty compound gives `true`.** `Selector.Matches` checks `IsEmpty` first, so `default(Selector)` still matches nothing. The doc comment of the compound states this.
- **DS1.7 No equality members on `CompoundSelector`.** The plan names none, so the struct has none. `Assert.Equal(selector, rule.Selector)` in `StyleSheetTests` still passes through the default `ValueType.Equals`. Phase 3 adds a `string?` name, so that default compare then uses reflection. The hot path never compares selectors, so this does not touch the allocation rule.
- **DS1.8 Extra tests beyond the brief:** `ForKeepsEachPartOfTheCompound` (the `Subject` getter, the shortcut subject, and the `default(Selector)` subject). `EachPseudoStateMatchesItsOwnBit` also checks an element in the state without the class. `ACompoundNeedsEachState` also checks `Focus` alone and the exact `Hover | Focus`. `AStateOnlyRuleMatchesEachElementInTheState` runs through the pass with a panel, a label, and a hovered panel. `AStateRuleWinsOverTheClassRuleInEachOrder` also checks the class value without the state. `TheSpecificityCountsEachStateInTheClassTier` also checks `.a:hover` (512) and `Label.a` with each state (1537).
- **Migration risk.** A user of the 0.1.0-alpha package who stores or compares a specificity number sees new values (256 in place of 10, 257 in place of 11). The order of the rules does not change. Task 4.7 records it under `### Changed` in `CHANGELOG.md`.
- **Open question:** none.

---

## Phase 2: Decisions

### Group 1 (tasks 2.1 to 2.4): The child chain and the descendant chain (2026-10-21)
- **The result.** The build gives `0 Warning(s)` and `0 Error(s)`. The test total goes from 1205 to 1216 (11 new facts in `ChainSelectorTests`, 0 failed). `StylePass.cs`, `Element.cs`, `ClassList.cs`, and `StyleSheet.cs` do not change in this group. `TheStylePassAllocatesNothingAfterWarmUp` and `ThousandElementAllocationTests` pass. The grep `grep -nE 'System\.Reflection|GetType\(\)|typeof\(|HasFlag' src/BrutalUi.Core/Style/*.cs` prints nothing (exit code 1).
- **The blast radius.** `Selector` gets two private fields: `CompoundSelector _ancestor` (12 bytes) and `SelectorCombinator _combinator` (1 byte). The struct grows from 12 bytes to about 28 bytes (replaced by DS4.8). `StyleRule` holds the selector by value, so each rule grows by the same amount. The callers `StyleRule`, `StyleSheet.Builder.Add`, `StyleSheet.Builder.Build`, `UiContext.RebuildCascade`, and `StylePass.Restyle` do not change. `IsEmpty` still reads only the subject, because each factory rejects an empty subject.
- **DS2.1 One check helper for each slot.** A private static `Selector.ThrowIfWrong(in CompoundSelector, string paramName)` holds the empty check and the state check of DS1.3 and DS1.4. `For`, `Child`, and `Descendant` call it. The message text of `For` does not change. The parameter name names the slot: `parent` for `Child`, `ancestor` for `Descendant`, and `subject` for both.
- **DS2.2 The order of the slot checks.** Each factory checks the ancestor slot first and the subject slot second, in the order of the parameters. Thus `Child(default, default)` throws with `parent`, and `Descendant(default, default)` throws with `ancestor`. The test `ChildAndDescendantThrowForAnEmptySlot` checks it.
- **DS2.3 The switch default.** `Selector.Matches` uses `default: return true` for `SelectorCombinator.None`. The constructor is private, so no other combinator value can reach the switch.
- **DS2.4 A second private constructor.** `Selector(in ancestor, combinator, in subject)` takes the slots in the CSS order. The phase 1 constructor `Selector(in subject)` stays for `For` and the shortcuts.
- **DS2.5 The `SelectorCombinator` values are explicit** (`None = 0`, `Child = 1`, `Descendant = 2`), so `default(Selector).Combinator` is `None`. The doc comments name the CSS form. The remarks name the type, the classes, and the pseudo-states as the parts that a chain reads. The name joins in phase 3.
- **DS2.6 The test helper `MatchesDirectAndThroughThePass`** checks each case with the direct `Selector.Matches(Element)` and with a one-rule sheet through `UiContext.Update`. It asserts that both agree. The rule sets `BackgroundColor`, which does not inherit, so only a match sets it.
- **DS2.7 Extra tests and checks beyond the brief.** `AnAncestorWithNoTypePartReadsNoTypeId` uses a subclass that counts each read of `TypeId`. A class ancestor gives 0 reads. A type ancestor that matches at the parent gives 1 read on the parent and 0 on the grandparent. `ARootMatchesNoChain` also puts `.a` on the root itself, so an element is not its own ancestor, and it checks `Selector.For(.b)` on the root as the control. `AnAncestorStateRuleMatchesOnlyInTheState` also checks that the hover of the subject does not satisfy the hover of the parent. `ATypeAncestorReadsTheTypeOfTheAncestor` also checks a subject of the type under a plain parent. `TheChainSpecificitySumsBothCompounds` also checks two types (`Label.a > Label.b` 514) and that `Child` and `Descendant` give the same weight. `TheBuilderAcceptsEachChainForm` also checks the sort order (256, 512, 768) and the `Ancestor` getter.
- **Follow-up.** The exception text of `StyleSheet.Builder.Add` for `default(Selector)` names `For`, `ForClass`, `ForType`, and `ForTypeAndClass`, but not `Child` and `Descendant`. `StyleSheet.cs` is outside the files of this group. A later task can add the two names.
- **Migration risk.** The public surface only grows. A user who stores a `Selector` sees a larger struct. No specificity value of phase 1 changes.
- **Open question:** none.

### Group 2 (tasks 2.5 to 2.9): The subtree mark on a class change and on a state change (2026-10-21)
- **The result.** The build gives `0 Warning(s)` and `0 Error(s)`. The test total goes from 1216 to 1225 (9 new facts in `SubtreeMarkTests`, 1225 passed, 0 failed, 0 skipped). `Selector.cs`, `CompoundSelector.cs`, `StylePass.cs`, and the `Name` setter do not change. `DirtyTrackingTests.TenThousandSeededRandomCallsKeepTheInvariantAfterEachCall` passes with no change. `AChainMatchAndAStateChangeAllocateNothing`, `TheStylePassAllocatesNothingAfterWarmUp`, and `ThousandElementAllocationTests` pass with 0 bytes. The grep `git diff -U0 ab41d74 -- tests | grep -c '^[-+].*AssertRect'` prints `0`.
- **The demo prints:**
  - `row #00000000 cell #00000000`
  - `row #3060C0FF cell #FFCC00FF`
  - `row #00000000 cell #00000000`
- **Task 2.5 and task 2.6.** The `States` setter and `ClassList.Add` and `ClassList.Remove` call `MarkSubtreeStyle()` in place of `MarkDirty(DirtyFlags.Style)`. The check for the same value, the duplicate add, and the remove of an absent id stay before the mark. The remarks of `Element.States`, `ClassList`, and `PseudoStates` state A2 and Risk 2. The remarks of `Element.MarkSubtreeStyle` name the two new callers.
- **Task 2.7 The changed lines.** None. The full run after tasks 2.5 and 2.6 gave 1216 passed and 0 failed, so no exact `Dirty` assert changed and no test was renamed.
- **The blast radius.** The callers of the `States` setter in `src/`, `samples/`, and `benchmarks/`: none (only tests set it). The callers of `ClassList.Add` in `src/`: `ElementBuilder.Class` (two overloads). A builder chain usually calls it on an element with no child, and that element gets `Style` only. A call after `.Child(...)` now marks the children too. The next update gives the same styles, because the attach already marked the children. The mark adds no field and no allocation. A detached element has no parent, so the mark stops at the element itself. The next attach marks the subtree again (DR1.1).
- **DS2.8 The scene of `SubtreeMarkTests` has no sheet** for the mark tests. The mark reads no sheet, so a sheet gives no other bits. Only the demo adds a sheet with the two chain rules.
- **DS2.9 Extra checks beyond the brief.** The demo also checks that `side` keeps a transparent background. `AStateChangeMarksTheSubtree` also checks the change back to `None`. `TheSameStateAndADuplicateClassMarkNothing` also checks a held state that is not `None` (`Hover | Focus`). `ALeafChangeMarksTheLeafOnly` also checks a class add on the leaf. `TheMarkKeepsTheInvariant` marks the panel, the row, the cell, the side, and the root in turn, then a mark over a `Paint` bit, and checks the exact bits of the cell (`Style | Paint`) and the row (`Style | DescendantStyle | DescendantPaint`). `TheNextUpdateClearsEachStyleBit` marks three branches before `UpdateStyles()`. `AChainMatchAndAStateChangeAllocateNothing` also asserts the final styles of each panel and each item after the measured steps, so the steps reach the three rules.
- **DS2.10 The rules of the allocation gate.** The state rule is `.panel:hover` (background). The child rule is `.panel:hover > .item` (background). The descendant rule is `.root .mark` (opacity), and it matches at the grandparent of each item. Each step toggles the hover of panel `step % 7` and the class `.mark` of item `step % 42`.
- **Risk 2 (A2).** A hover or a class change on a high ancestor restyles the full subtree on the next `Update`. The mark costs one walk of the subtree. The style pass then visits each element of the subtree. Phase 4 can measure the cost on the 1000-element tree.
- **Follow-up.** The input layer does not set `States` yet. When it does, a hover on a root panel marks the full tree on each enter and each leave. A later feature can add a sheet-aware mark (out of scope by A2).
- **Open question:** none.

---

## Phase 3: Decisions

### Group 1 (tasks 3.1 to 3.6): The name part, the name mark, and the full specificity order (2026-10-21)
- **The result.** The build gives `0 Warning(s)` and `0 Error(s)`. The test total goes from 1225 to 1237 (12 new facts in `NameSelectorTests`, 1237 passed, 0 failed, 0 skipped). `ElementStateTests`, `DirtyTrackingTests`, the allocation tests, and each tree that sets `Name` in an object initializer pass with no change. `StylePass.cs` does not change. The grep `grep -nE 'System\.Reflection|GetType\(\)|typeof\(|HasFlag' src/BrutalUi.Core/Style/*.cs` prints nothing (exit code 1).
- **The demo prints:**
  - `title #00FF00FF`
  - `subtitle #3060C0FF`
- **Task 3.1.** `Element.Name` has a private `_name` field. The setter returns at once for an ordinal-equal value. Otherwise it writes the value and calls `MarkSubtreeStyle()`. The doc comment and the remarks of `MarkSubtreeStyle` state it.
- **Task 3.2.** `CompoundSelector.Name` (`string?`, `init`). `IsEmpty` covers it. `Matches` tests it last, with `string.Equals(..., StringComparison.Ordinal)`. No equality members were added (DS1.7).
- **Task 3.3.** `Selector.ForName(string)` throws `ArgumentException` (param `name`) for `null` and `""`. `Tiers` adds the name tier at `NameShift`. `ThrowIfWrong` rejects an empty name in each slot.
- **Follow-up of phase 2.** The `Add` parameter doc and the exception text of `StyleSheet.Builder.Add` name `For`, `Child`, `Descendant`, `ForClass`, `ForName`, `ForType`, and `ForTypeAndClass`. No code and no test changed. No test asserts the old text.
- **DS3.1 The check order in `ThrowIfWrong`.** The empty check first, the empty-name check second, the state check third. `new CompoundSelector { Name = "" }` is not "empty" by `IsEmpty` (the name is not `null`), so the empty-name check rejects it with its own message. The message for the empty compound now lists `Name` too.
- **DS3.2 `ForName` throws plain `ArgumentException`** for `null`, not `ArgumentNullException`, as the brief states. `string.IsNullOrEmpty` does the check.
- **DS3.3 Doc updates.** The remarks of `Selector`, `SelectorCombinator` (DS2.5), `Selector.Specificity`, `Tiers`, and `NameShift` no longer say that the name joins in phase 3.
- **DS3.4 Extra tests beyond the brief.** `ANameWorksInACompound` (name with class and state, and the tier sum 65792 as `(1 << 16) | (2 << 8)`), `ANameChangeRestylesAChainRuleOfADescendant` (a name change on the grandparent restyles the cell through the pass), `ForNameGivesTheNameAndTheNameTier`. `ANameChangeMarksTheSubtree` also checks a leaf change. `TheSameNameMarksNothing` also checks an equal name in a new string instance, a `null` set on a `null` name, and that `Panel` differs from `panel`. The specificity test also checks the numbers (3072, 65536, 2, 256).
- **DS3.5 The test helper removes the root from the context** after each pass check (`RemoveRoot`), so one tree serves more than one selector check.
- **Open question:** none.

---

## Phase 4: Decisions

### Group 1 (tasks 4.1 to 4.4): The 16-rule sheet, the state change in the style step, and the planted allocation (2026-10-21)
- **The result.** The build gives `0 Warning(s)` and `0 Error(s)`. The test total goes from 1237 to 1238 (1 new fact `TheStyleStepMatchesEachSelectorForm`, 1238 passed, 0 failed, 0 skipped). No file of `src/` changes, except the temporary plant of task 4.3. The ImGui allocation gates and the benchmarks build and pass with the 16-rule sheet.
- **Task 4.1 The 4 new rules** (rules 13 to 16 of `ThousandElementTree.BuildSheet`). Each rule sets only `BorderColor`, and no element that it matches has a border width (D11.5). No rule sets a text color (D11.4).
  - 13. The state rule `.slot:hover` (`Selector.For`, specificity 512): `SlotHoverBorderColor`. It wins over the border color of rule 11 `Element.slot` (257).
  - 14. The child rule `.slot:hover > Element` (`Selector.Child`, 513): `SlotChildBorderColor` on the icon and the badge of a hovered slot.
  - 15. The descendant rule `.row:hover Element` (`Selector.Descendant`, 513): `RowDescendantBorderColor` on the children and the grandchildren of a hovered row.
  - 16. The name rule `#header` (`Selector.ForName(HeaderName)`, 65536): `HeaderBorderColor` on the header.
- **The new public members of the shared tree.** `HeaderName` (`"header"`), the 4 colors above, and the handles `StatRow`, `StatFill`, `GridIcon`, and `Header`. `BuildHeader` returns the header. `BuildStats` gives the fill leaf of stat row 0 as a second `out` value.
- **Task 4.2 The style step.** The first round sets `States = PseudoStates.Hover` on `GridSlot` and on `StatRow`, after the class adds. The second round sets `PseudoStates.None` on both. The loop that marks each element stays after the state sets. The kind table and the class remarks name the state change.
- **Task 4.3 The plant.** The copy `Selector.cs.orig` in the session scratch folder has the same SHA-256 as the file (`8b25f1ee...2920f`). `git diff --stat` of the file before the plant gives `253 insertions(+), 40 deletions(-)`. With the plant it gives `254 insertions(+), 40 deletions(-)`, and the grep for the plant gives 1 hit. The plant `GC.KeepAlive(new object());` at the start of `Selector.Matches(Element, ElementTypeId)` gives `exit=2` (4 failed of 1238):
  - `StylePassTests.TheStylePassAllocatesNothingAfterWarmUp`
  - `SubtreeMarkTests.AChainMatchAndAStateChangeAllocateNothing`
  - `ThousandElementAllocationTests.DrawAllocatesNothingAfterWarmUp`
  - `ThousandElementAllocationTests.UpdateAllocatesNothingAfterWarmUp`
  - The restore with `cp` gives `restore=0`. The `diff` against the copy prints nothing (`diff=0`), and the grep for the plant gives 0 hits. `git diff --stat` gives `253 insertions(+), 40 deletions(-)` again. The test run after the restore gives `exit=0`.
- **Task 4.4 The demo class, 5 runs.** Each run gives `total: 10, failed: 0, succeeded: 10`. The durations are 966 ms, 934 ms, 934 ms, 959 ms, and 965 ms. Both gates assert 0 bytes, so both gates measure 0 bytes in each run.
- **Task 4.4 The facts line.** `facts: tree elements 1000, bytes 1046640, bytes per element 1046.6`. The baseline of task 1.1 is 1041.5, so the build allocates 5152 more bytes. The cause is in the test file, not in `src/`: the build adds 4 more rules to the sheet and to the cascade of the context, and the tree object holds 4 more handles.
- **Task 4.4 The smoke run.** `test-linux: PASS build, test, gpu, smoke (120 frames) in 13 s` and `exit=0`.
- **DS4.1 The child rule also reads the hover of the slot.** The brief asks that the next step gives the old child value. Thus the child rule needs a part that the step toggles. The parent part `.slot:hover` gives it, so one state change drives the state rule and the child rule.
- **DS4.2 The subject of the child rule and of the descendant rule is the type `Element`.** Each of the 1000 elements matches the subject, so each one reads its parent in the measured body. The descendant rule also reads the grandparent when the parent part misses. This is the worst case of the bounded walk. The descendant rule matches `StatName` at the parent and `StatFill` at the grandparent.
- **DS4.3 The name rule uses a name that the build sets already.** The collector gives each element a name. `header` is unique in the tree. The new constant `HeaderName` holds the literal, and `BuildHeader` uses it, so the build creates no name string. No element got a new name.
- **DS4.4 The new test reads the old values before the step.** `SlotBorderColor` and the other old border colors stay private. The test also checks two misses: the hovered row itself (an element is not its own ancestor) and the name leaf of stat row 1 (its row is not in the state). It checks the rects of the slot, the icon, the name leaf, the fill leaf, and the header, and the `Rect` call count of the recording renderer before the step, after the hover, and after the hover ends.
- **DS4.5 Extra checks in `TheTreeHoldsTheRequiredMix`.** The literal 16, 1 child rule, 1 descendant rule, 1 rule with a name, 1 rule with a state, no element in a state after the build, exactly 1 element with the name `header`, and the relations of the 4 new handles.
- **DS4.6 Why the plant fails 4 tests and not 7.** The plant of stylesheet-model was in `Restyle`, which runs for each restyled element with or without a sheet. `Selector.Matches` runs only for a rule of a sheet. The ImGui tree gate measures `Draw` only, and the ImGui label scene adds no sheet. Thus no ImGui gate calls `Selector.Matches` inside its measure.
- **The blast radius.** The users of the shared tree are `ThousandElementAllocationTests.cs`, `FactsReport.cs`, `ImGuiRendererAllocationTests.cs`, `TreeLayoutBenchmark.cs`, and `TreeDrawBenchmark.cs`. No one of them changes. Each one passes or builds with the 16-rule sheet. The new rules change no rect and no draw call, and the draw count checks of `TheDrawStepEmitsEachPaintedElement` and the ImGui gates pass.
- **Follow-up.** The ImGui gates do not cover the match loop. A later feature can add a sheet to the ImGui label scene, so a plant in `Selector.Matches` also fails an ImGui gate.
- **Open question:** none.

### Group 2 (tasks 4.5 to 4.9): The design doc, the changelog, the human testing list, and the hand-offs (2026-10-21)
- **The result.** The build gives `0 Warning(s)` and `0 Error(s)`. The test total stays at 1238 (1238 passed, 0 failed, 0 skipped). No code line of `src/` changes. The check `git diff -U0 ab41d74 -- src/BrutalUi.Core/Style/StylePass.cs | grep '^[-+]' | grep -v '^[-+][-+]' | grep -vE '^[-+]\s*///'` prints nothing.
- **Task 4.5 `docs/technical-design.md`.**
  - Section 2: the "Full CSS" line names the type, class, name, and pseudo-state selectors, the child chain and the descendant chain of depth 2, and the walk bound. It names the sibling, attribute, universal, and `:not` selectors as out of the first release (Conflict 1).
  - Section 5: the shape comments of `Name`, `States`, `ClassList.Add`, and `ClassList.Remove` name the subtree mark. A new paragraph "The name, the states, and the classes" states the mark (Conflict 5).
  - Section 7: the model shows `CompoundSelector`, `SelectorCombinator`, and the real public members of `Selector`. Step 1 of the cascade order no longer holds the weights 10 and 1. New paragraphs: "The selectors", "The specificity tiers" with the table of the brief, "The match" with the walk bound, and "The dirty rule for a class, a state, and a name". The design decisions name the bit math of the states, the depth constant, the walk bound, one class for each compound, the ordinal name compare, the byte tiers, the full subtree mark, and DS1.7.
- **Task 4.6.** The last remarks paragraph of `StylePass.cs` names the compound parts, the child chain, the descendant chain, and the read of the tree. The text "(phase 9)" goes.
- **Task 4.7.** One line under `### Added` and one line under `### Changed`, each with `(style-system/selector-matching)`.
- **Task 4.8.** The phase 4 demo line and the line `Windows run (selector-matching)` go into the `## selector-matching` section. The `Last Updated` stamp is `2026-10-21 21:18`, the clock of the run.
- **Task 4.9 The lint.** `docs/technical-design.md` 1.36, `CHANGELOG.md` 1.54, `docs/features/style-system/human-testing.md` 0.49, `implementation.md` 1.75 (not edited), and `context.md` 1.59. No file is above 2.5. The one hit of `human-testing.md` is the em dash of the epic title, which the parent wrote. This group keeps it.
- **The follow-up of phase 3.** The type summary of `Selector` names "the type, the class, the name, and the pseudo-states". The chain remarks name "the type, the classes, the name, and the pseudo-states of the ancestor". Only `///` lines change.
- **DS4.7 The cost line of section 7 follows the facts of group 1.** The brief does not name it. The line said 1041.5 bytes and a 12-rule sheet. It now says 1046.6 bytes and a 16-rule sheet, with the four new rule forms and the hover step.
- **DS4.8 The design doc names no internal member.** `Selector.Matches`, `CompoundSelector.Matches`, and `IsEmpty` are `internal`, so the model block omits them (D11.8 of stylesheet-model). The doc gives no byte size of `Selector`. A compound holds three 4-byte ids and a reference, so the old size "about 28 bytes" of DS2 is out of date. Probe 10 of `code-review.md` measures the real sizes: 24 bytes for a `CompoundSelector`, 56 bytes for a `Selector`, and 320 bytes for a `StyleRule`. These values replace the 12-byte and 28-byte lines of the blast radius of phase 1 and phase 2.
- **DS4.9 The match paragraph states two edge cases that the brief does not name.** An element is not its own ancestor (DS2.7), and the factories check the ancestor slot first (DS2.2).
- **Follow-up.** Section 4 ("Dirty tracking") says "A change marks the element and its ancestors". Section 14 lists the style test classes of stylesheet-model only. This group keeps both sections, because the brief names sections 2, 5, and 7. A later doc task can name the subtree mark in section 4 and the four new test classes in section 14.
- **Open question:** none.

---

## Review Group 1: Decisions

### CR-1: A name change in the measured body (2026-10-21)
- **The result.** The build gives `0 Warning(s)` and `0 Error(s)`. The test total stays at 1238 (1238 passed, 0 failed, 0 skipped). The class `ThousandElementAllocationTests` gives `total: 10, failed: 0, succeeded: 10`, so both gates measure 0 bytes. No file of `src/` changes, except the temporary plant.
- **The fix.** The shared tree has the new constant `OtherHeaderName` (`"header-renamed"`). The first round of the style step sets `tree.Header.Name = ThousandElementTree.OtherHeaderName`, after the state sets. The second round sets `ThousandElementTree.HeaderName`. Both values are `const string`, so the step creates no string. The `Name` setter marks the subtree of the header (14 elements), and the loop after it marks each element.
- **The test.** `TheStyleStepMatchesEachSelectorForm` checks the name before the step, after the first round, and after the second round. The header loses `HeaderBorderColor` in the first round (`Assert.NotEqual`) and gets it back in the second round. The existing checks of the header rect and of the `Rect` call count show that the name changes no rect and no draw call.
- **The facts.** `TheTreeHoldsTheRequiredMix` checks that the two names are not equal with an ordinal compare, that 0 elements have the name `OtherHeaderName` after the build, and that the name of `Header` is `HeaderName`.
- **The doc text.** The class remarks of the test file, the kind table, and the comment of the `StyleKind` case name the name change. The remarks now say that the name rule misses in the first round and matches in the second round. The remarks of the shared tree and the summary of `Header` say that the header has the name `HeaderName` after the build.
- **The M23 run (for review group 2).** The copy `Element.cs.orig` in the session scratch folder has the same SHA-256 as the file (`0653fa31...17e3fb8`). `git diff --stat` of the file gives `41 insertions(+), 6 deletions(-)` before the plant and `42 insertions(+), 6 deletions(-)` with the plant. The plant `GC.KeepAlive(new object());` goes after the same-value return of the `Name` setter. The run gives `exit=2` (2 failed of 1238):
  - `ThousandElementAllocationTests.UpdateAllocatesNothingAfterWarmUp`
  - `ThousandElementAllocationTests.DrawAllocatesNothingAfterWarmUp`
  - The restore with `cp` gives `restore=0`. The `diff` against the copy prints nothing (`diff=0`), the grep for the plant gives 0 hits, and the SHA-256 is the same as the copy. `git diff --stat` gives `41 insertions(+), 6 deletions(-)` again. The test run after the restore gives `exit=0`.
- **DR1.1 The run of the plant writes a log.** The brief sends the output to `/dev/null`. This group sends it to `m23.log` in the scratch folder, so the record can name the failed tests. The exit code is the same.
- **DR1.2 Why the plant fails 2 tests and not 4.** `StylePassTests` and `SubtreeMarkTests.ChainScene` set no name in the measured body. Only the 1000-element step does. The draw gate runs the same step before each `Draw`, so it also fails. The ImGui gates use no step of this class.
- **DR1.3 The header has no other border color rule.** The inline set of the header sets no border color. Thus the first round gives the header the default border color. The test asserts only that the color is not `HeaderBorderColor`, so it does not depend on the default value of `BorderColor`.
- **The blast radius.** `Step` is private to `ThousandElementAllocationTests`, so only its tests run the rename. The ImGui tests, `FactsReport.cs`, and the benchmarks link the shared tree, but they do not rename the header. The new constant is the only new member, and the three projects build with 0 warnings.
- **Follow-up.** Review group 2 adds the name change and the M23 result above to the style-step bullet of section 14 of `docs/technical-design.md`. `ChainScene.Step` of `SubtreeMarkTests` still sets no name. The 1000-element gate is enough for rule 4, so this group keeps it.
- **Open question:** none.

---

## Review Group 2: Decisions

### CR-2: Section 4 states the subtree mark (2026-10-21)
- **The fix.** The second sentence of "Dirty tracking" in `docs/technical-design.md` keeps the ancestor mark. A new sentence says that a class, state, or name change, an attach, and a sheet change also mark the full subtree of the element with `Style` (section 7). The text follows the brief word for word.

### CR-3: Section 14 names the new tests and the real gate facts (2026-10-21)
- **The fix.** The style-test bullet names `PseudoStateSelectorTests`, `ChainSelectorTests`, `NameSelectorTests`, and `SubtreeMarkTests`. The style-step bullet gives the 16 rules and the four new forms, the state change, the name change, and the 10 tests of `ThousandElementAllocationTests` (counted in the file: 10 `[Fact]`). It also names `TheStyleStepMatchesEachSelectorForm` and `SubtreeMarkTests.AChainMatchAndAStateChangeAllocateNothing`.
- **The plants.** The bullet keeps the `Restyle` plant (7 gates) as history of stylesheet-model, without a new run. It gives the plant in `Selector.Matches` (4 Core gates, no ImGui gate, DS4.6) and the plant in the `Name` setter (2 gates, CR-1).
- **DG2.1 The bullet names the three plants in one paragraph.** The brief asks for the `Selector.Matches` result and the `Name` result. The `Restyle` text now says "failed ... in stylesheet-model", so a reader sees that it is history.
- **DG2.2 The bullet adds the chain-scene gate.** The brief does not name `SubtreeMarkTests.AChainMatchAndAStateChangeAllocateNothing` in the bullet. The gate is one of the 4 that the plant fails, so the bullet names it.
- **The lint.** `docs/technical-design.md` is checked at the end of the group (see the hand-back report). No code changes.
- **Open question:** none.

---

## Review Group 3: Decisions

### CR-5: The hand-offs state the name form and the cost of a high hover (2026-10-21)
- **The fix.** The style-parser hand-off tells the parser to give `Name` and `ClassName.Intern` the identifier without the `#` or the `.`. The input hand-off gives the cost of a hover on the root (about 0.1 to 0.8 ms in `Update`, probe 7) and asks the input feature to decide on `:hover` for each ancestor.

### CR-6: The doc comments and the size records match the code (2026-10-21)
- **The fix.** The summaries of `Selector.Ancestor` and of the field `_ancestor` say that the parent matches a child chain, and that the parent or the grandparent matches a descendant chain. The summary of `ThrowIfWrong` names the empty name. Only `///` lines of `Selector.cs` change. DS4.8 gives the measured sizes (24, 56, and 320 bytes). The two old size lines carry the mark "replaced by DS4.8".

### The deferral of CR-4 (2026-10-21)
- The parent defers CR-4 because the change adds public API that the accepted plan does not name. The style-parser hand-off holds the open item. The parent writes the resolution in `code-review.md`.
- **Open question:** none.

---

## Hand-offs

The plan Notes hold the first form of these lines. This section checks each line against the code at the end of phase 4.

### style-parser
- Map each compound of the format to `CompoundSelector`: a type name through `ElementType.Intern`, `.class` through `ClassName.Intern`, `#name` to `Name`, and each of `:hover`, `:active`, `:focus`, `:disabled`, and `:checked` to a bit of `States`. Map `>` to `Selector.Child(parent, subject)` and whitespace to `Selector.Descendant(ancestor, subject)`. Map a selector with one compound to `Selector.For(subject)`.
- Keep the case of a name. The name compare is ordinal and case-sensitive, so `#Title` does not match `title`.
- Reject with a positioned error: a chain of more than two compounds, a second class in one compound, an unknown pseudo-state, a sibling or attribute selector, and `:not`. A compound holds one `Class` slot only.
- The factories throw `ArgumentException` for an empty compound, for an empty name, and for a state bit that is not a member of `PseudoStates`. Each check runs in this order. `Child` and `Descendant` check the ancestor slot first, and the parameter name (`parent`, `ancestor`, or `subject`) tells the slot. `ForName` throws `ArgumentException`, not `ArgumentNullException`, for `null`. Check each case first and report the position.
- No universal selector exists (Gap 2). A compound with only states acts as `*:hover`. A bare `*` gives an empty compound, and the factory throws.
- A selector list (`.a, .b`) gives one rule for each selector. `Selector.Specificity` computes the value, and `StyleRule` stores it at creation, so the parser computes nothing. Rules with the same specificity keep the order of add, so add the rules in the order of the source.
- `CompoundSelector` has no equality members (DS1.7). Its default `ValueType.Equals` uses reflection for the `string?` name. Do not compare selectors in a hot path.
- Give `Name` and `ClassName.Intern` the identifier without the `#` or the `.`. The factories do not check the characters of a name. They accept each string that is not empty, also `"#title"` and `"a b"`. Such a name matches only an element with that exact name, so a rule with a `#` in the name matches no element, and no error shows it.
- CR-4 of `code-review.md` is open for style-parser: `CompoundSelector` and `Selector` have no `IEquatable<T>`, no `==`, and no readable `ToString`. The default `Equals` gives the right result but allocates (about 880 bytes for each call), and a failed `Assert.Equal` shows `BrutalUi.Core.Selector` for both values. The plan of style-parser decides the shape.
- The format doc states the walk bound of `.a .b` (question 2): the parent and the grandparent only. This differs from CSS.

### transitions
- A state change restyles the full subtree on the next `Update`. A tween starts on each element whose value changes, also below the changed element. Many elements of the subtree get a restyle with no change of a value (A2, Risk 2), so `Compare` gives no bit for them.
- `StylePass` does not change in this feature (doc comment lines only). Thus the start point of the stylesheet-model hand-off holds: `StylePass.Restyle` holds `ref ComputedStyle current = ref element.StyleStorage` and `next` before the write `current = next`.
- The match reads the tree, not the computed style. A tween value in `Element.Style` does not change which rule matches.

### input and later features
- The input layer does not set `States` yet. When it does, a hover on a high panel marks the full subtree on each enter and each leave. A sheet-aware mark is out of scope (A2).
- A hover on the root of the 1000-element tree costs about 0.1 to 0.8 ms in `Update` (probe 7 of `code-review.md`). Decide whether `:hover` goes to each ancestor, as in CSS. If it does, measure the cost of a pointer move before the input feature ends.
- The ImGui allocation gates do not call `Selector.Matches` inside the measure (DS4.6). A later feature can add a sheet to the ImGui label scene.
- `.a.b` (Gap 1), a rule index (Gap 3), and a `Name` helper on `ElementBuilder` (Gap 5) are not in this feature.

---

## Gotchas & Learnings

- A planted allocation proves a gate only for the code path that the measured body runs. The plant in `Selector.Matches` failed 4 Core gates and no ImGui gate, because the ImGui gates run no sheet in the measure (DS4.6). The plant in the `Name` setter failed 0 gates until CR-1 put a name change into the style step. Plant each new hot path once, and read the list of failed gates.
- A `readonly struct` with a `string?` field gets a default `Equals` that uses reflection and allocates. Keep such a compare out of a hot path (DS1.7, CR-4).

---

## Testing Approach

### Unit Tests
**Files:** `tests/BrutalUi.Core.Tests/Style/*.cs`
- `PseudoStateSelectorTests`, `ChainSelectorTests`, `SubtreeMarkTests`, and `NameSelectorTests`: each part and each chain form has a match and a miss.
- The specificity numbers of the tier table, and the order of each pair in both orders of add.

### Integration Tests
- `ThousandElementAllocationTests` with the 16-rule sheet and a state change in the style step.
- `AChainMatchAndAStateChangeAllocateNothing` and `TheStylePassAllocatesNothingAfterWarmUp`.

---

## Needs Human Verification

`/autorun-feature` adds one line per item that the verifier cannot check, for a person to test.

- [ ] Phase 1 demo: the developer runs `dotnet run --project tests/BrutalUi.Core.Tests/BrutalUi.Core.Tests.csproj -- --filter-method '*AHoverRuleRestylesTheButton' --show-live-output on` and reads `button #1E222CFF`, then `button hover #3060C0FF` after `States = Hover`, then `button #1E222CFF` after `States = None`.
- [ ] Phase 2 demo: the developer runs `dotnet run --project tests/BrutalUi.Core.Tests/BrutalUi.Core.Tests.csproj -- --filter-method '*AHoverOnAPanelRestylesItsRowAndItsCell' --show-live-output on` and reads `row #00000000 cell #00000000`, then `row #3060C0FF cell #FFCC00FF` after the hover of the panel, then `row #00000000 cell #00000000` after the hover ends.
- [ ] Phase 3 demo: the developer runs `dotnet run --project tests/BrutalUi.Core.Tests/BrutalUi.Core.Tests.csproj -- --filter-method '*ANameRuleWinsOverAClassAndAState' --show-live-output on` and reads `title #00FF00FF`, then `subtitle #3060C0FF` after `Name = "subtitle"`.
- [ ] Phase 4 demo: the developer runs `dotnet run --project tests/BrutalUi.Core.Tests/BrutalUi.Core.Tests.csproj -- --filter-class '*ThousandElementAllocationTests'` and reads a total of 10 with `failed: 0`.
- [ ] Windows run (selector-matching): on Windows, after `git pull --ff-only`, run `.\scripts\test-windows.ps1`. Expect the PASS line and `$LASTEXITCODE` 0.

---

## Performance Considerations

- A hover on a high ancestor restyles the full subtree on the next `Update` (A2, Risk 2 of the plan).
- The descendant walk stops at the grandparent, so each chain rule has a constant cost.

---

## Native Dependencies

- None.

---

## Session Notes

### 2026-10-21 20:43
- `/autorun-epic` started the feature through `/autorun-feature` in the called mode. `/start-feature` created `context.md`, `tasks.md`, and `inbox.md`.
- The next session starts with phase 1, task 1.1.

### 2026-10-21 20:58
- Phase 1 is done in one group (`phase-1.md`) at the ADVANCED tier. The verifier (`phase-1-verify.md`) gave PASS: 0 warnings, 1205 tests with 0 failed, 19 allocation tests, no reflection in `Style/`, no change of `StylePass.cs`, and an `AssertRect` diff count of 0. Demo: PASS.
- The orchestrator accepts DS1.1 to DS1.8. DS1.7 is a note for phase 3: the `string?` name makes the default `ValueType.Equals` of `CompoundSelector` use reflection. Only a test compares selectors.
- The inbox holds no line.

### 2026-10-21 21:12
- Phase 2 is done in two groups (`phase-2.md`, `phase-2-g2.md`) at the ADVANCED tier. The verifier (`phase-2-verify.md`) gave PASS: 0 warnings, 1225 tests with 0 failed, 19 allocation tests, `SubtreeMarkTests` and `ChainSelectorTests` passed, the seeded invariant test passed with no change, no change of `StylePass.cs`, and an `AssertRect` diff count of 0. Demo: PASS.
- The task 2.7 list is empty: no exact `Dirty` assert failed after the subtree mark.
- The follow-up of group 1 (the `StyleSheet.Builder.Add` text does not name `Child` and `Descendant`) goes into the brief of phase 3 (`phase-3.md`).
- The inbox holds no line.

### 2026-10-21 21:22
- Phase 3 is done in one group (`phase-3.md`) at the STANDARD tier. The verifier (`phase-3-verify.md`) gave PASS: 0 warnings, 1237 tests with 0 failed, 19 allocation tests, `NameSelectorTests` and `SubtreeMarkTests` passed, no change of `DirtyTrackingTests.cs` and `ElementStateTests.cs`, and no change of `StylePass.cs`. Demo: PASS.
- The verifier notes two stale doc lines in `Selector.cs`: the type summary and the chain remarks do not name the name part. The fix goes into the brief of phase 4, group 2 (`phase-4-g2.md`).
- The inbox holds no line.

### 2026-10-21 21:30
- Phase 4 is done in two groups (`phase-4.md`, `phase-4-g2.md`) at the ADVANCED tier. The verifier (`phase-4-verify.md`) gave PASS: 0 warnings, 1238 tests with 0 failed, 5 runs of `ThousandElementAllocationTests` with 10 of 10, 20 Core and 4 ImGui allocation tests, no plant left, the code of `StylePass.cs` unchanged, the lint exit 0 for each changed doc, and the smoke run PASS. Demo: PASS.
- The orchestrator accepts DS4.1 to DS4.9. The open doc items of group 2 (section 4 and section 14 of `docs/technical-design.md`) and DS4.6 go into the brief of the cross-phase review as known items.
- All 4 phases are done. The inbox holds no line.

### 2026-10-21 22:05
- The cross-phase review (`review.md`) wrote `code-review.md` with 6 findings (1 MEDIUM, 5 LOW). A shipped test kills each of the 21 mutations of the match, the tiers, the slot checks, and the marks. Review groups 1 to 3 (`review-g1.md` to `review-g3.md`) fixed CR-1, CR-2, CR-3, CR-5, and CR-6. The verifier (`review-verify.md`) gave PASS: 0 warnings, 1238 tests with 0 failed, 20 Core and 4 ImGui allocation tests, the four slice demos, the lint, and the smoke run.
- CR-1 changed a gate: the style step of `ThousandElementAllocationTests` renames the header between two constants, so a plant in the `Name` setter fails 2 gates.
- The orchestrator defers CR-4 (no equality members and no text form on the selector types) to the plan of style-parser, because it adds public API that the accepted plan does not name. The style-parser hand-off holds it.
- The inbox holds no line.

### 2026-10-21 22:06
- `/update-feature`: 31 of 31 tasks are done. `implementation.md` reads `Ready for Review`.
- The draft plan of style-system/style-parser exists at `docs/features/style-system/style-parser/implementation.md` (7 phases, 54 tasks, 4 open scope questions). Question 2 of the draft decides the shape of CR-4. The architect had no shell, so the lint of the draft did not run. Task 7.12 of the draft runs it.
- The inbox holds no line, so no line folds and no bug goes to `docs/bugs/`.
- Final state: 1238 tests with 0 failed, 0 build warnings, 0 bytes in each allocation gate, bytes per element 1046.6, and the smoke run PASS. The base commit is `ab41d74`. The work is not committed (the caller owns each git action).

---

*Update this file at the end of each work session. Run `/update-feature` before compacting conversations.*
