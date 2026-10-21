# style-system/selector-matching - Task Checklist

**Last Updated:** 2026-10-21 22:06
**Progress:** 31/31 tasks complete (100%)
**Advanced phases:** Phases 1, 2, and 4 (ADVANCED, opus). Phase 3 is STANDARD (sonnet). Group B of phase 4 is plain documentation work.

Source paths: `Core/` means `src/BrutalUi.Core/`. Test paths: `tests/Core/` means `tests/BrutalUi.Core.Tests/`. "The plan" means `implementation.md` of this feature. Its sections "The Public API", "The specificity tiers", "The Match", and "The Subtree Mark" hold the rules that the tasks name. Each phase ends with `dotnet build brutal-ui.sln 2>&1 | tail -n 30` and `dotnet test --solution brutal-ui.sln 2>&1 | tail -n 30` (tool rule 5). Expect `0 Warning(s)`, `0 Error(s)`, and 0 failed. A demo uses `dotnet run --project tests/BrutalUi.Core.Tests/BrutalUi.Core.Tests.csproj -- --filter-method '*Name' --show-live-output on`. A test prints with `TestContext.Current.TestOutputHelper?.WriteLine(...)`. The class table, the type table, and the names are global, and xUnit runs classes in parallel, so each test interns names with the prefix `nameof(Test)`. A color prints as `#RRGGBBAA`. The projects have no implicit usings, so each new file holds its own `using` lines.

---

## Phase 1: A Hover Rule Restyles a Button (7/7 complete)

**Demo:** the developer runs `dotnet run --project tests/BrutalUi.Core.Tests/BrutalUi.Core.Tests.csproj -- --filter-method '*AHoverRuleRestylesTheButton' --show-live-output on` and reads `button #1E222CFF`, then `button hover #3060C0FF` after `States = Hover`, then `button #1E222CFF` after `States = None`.
**Needs:** none
**Agent tier:** ADVANCED (opus)

**Group A (the compound and the tiers, 6 files):**

- [x] ✅ **1.1 Record the baseline and read the code** - Completed 2026-10-21
  - Description: Record in `context.md`: the base commit (`git rev-parse HEAD`), the test total (expected 1192), the build result, and the `bytes per element` line of `dotnet run -c Release --project tests/BrutalUi.Benchmarks/BrutalUi.Benchmarks.csproj -- --facts 2>&1 | tail -n 30`. Read `Selector.cs`, `StyleSheet.cs`, `StyleRule.cs`, `StylePass.Restyle`, `ClassId.cs`, and `PseudoStates.cs`.
  - File(s): `context.md`
  - Estimate: 6 steps

- [x] ✅ **1.2 Write CompoundSelector.cs** - Completed 2026-10-21
  - Description: Write `src/BrutalUi.Core/Style/CompoundSelector.cs` per "The Public API", with `Type`, `Class`, and `States` (no `Name` yet), the internal `IsEmpty`, and the internal `Matches(Element, ElementTypeId)` per "The Match". The remarks state the parts, the exact type match (D9.8), and that a state-only compound matches each element in the state.
  - File(s): `src/BrutalUi.Core/Style/CompoundSelector.cs`
  - Estimate: 6 steps

- [x] ✅ **1.3 Move Selector.cs to the subject compound and the byte tiers** - Completed 2026-10-21
  - Description: Edit `src/BrutalUi.Core/Style/Selector.cs`: one private `CompoundSelector _subject` in place of the two parts. `Class` and `Type` read the subject. Add `Subject` and `For(in CompoundSelector)` with the factory rules. `ForClass`, `ForType`, and `ForTypeAndClass` build a compound and keep their throws. `Specificity` uses the byte tiers with private shift constants and `BitOperations.PopCount`. `Matches` calls the subject match. Replace the CR-3 remarks with the tier rule and the bound of "The specificity tiers".
  - File(s): `src/BrutalUi.Core/Style/Selector.cs`
  - Estimate: 8 steps

- [x] ✅ **1.4 Name Selector.For in the builder docs** - Completed 2026-10-21
  - Description: Edit `src/BrutalUi.Core/Style/StyleSheet.cs`: the `Add` parameter doc and the exception text name `Selector.For` next to the three shortcuts.
  - File(s): `src/BrutalUi.Core/Style/StyleSheet.cs`
  - Estimate: 3 steps

- [x] ✅ **1.5 Change the specificity numbers of the existing tests** - Completed 2026-10-21
  - Description: Edit `tests/BrutalUi.Core.Tests/Style/StyleSheetTests.cs` and `tests/BrutalUi.Core.Tests/Style/TypeSelectorTests.cs`: each specificity 10 becomes 256, and each 11 becomes 257. Rename `ForClassGivesTheClassAndASpecificityOfTen` to `ForClassGivesTheClassAndTheSpecificityOfOneClass`, and `TheSpecificityIsOneTenOrEleven` to `TheSpecificityPutsEachTierInItsOwnByte`. Change no other assert. Record each changed line in `context.md`.
  - File(s): `tests/BrutalUi.Core.Tests/Style/StyleSheetTests.cs`, `tests/BrutalUi.Core.Tests/Style/TypeSelectorTests.cs`, `context.md`
  - Estimate: 6 steps

- [x] ✅ **1.6 Write PseudoStateSelectorTests.cs** - Completed 2026-10-21
  - Description: Write `tests/BrutalUi.Core.Tests/Style/PseudoStateSelectorTests.cs`:
    - `AHoverRuleRestylesTheButton` (the demo): `.button { BackgroundColor = (30, 34, 44, 255) }` and `.button:hover { BackgroundColor = (48, 96, 192, 255) }`. Print after `Update`, after `States = Hover` and `Update`, and after `States = None` and `Update`. Assert the three lines.
    - `EachPseudoStateMatchesItsOwnBit` (a theory with 5 rows): `.x:<state>` matches with the state, misses without it, and misses with each other state alone.
    - `ACompoundNeedsEachState`: `:hover:focus` misses with `Hover` alone and matches with `Hover | Focus | Checked`.
    - `ATypeClassAndStateCompoundNeedsEachPart`: `Label.x:checked` misses when one part is absent.
    - `AStateOnlyRuleMatchesEachElementInTheState`: `:disabled` alone.
    - `AStateRuleWinsOverTheClassRuleInEachOrder`: two sheets with the two orders of add.
    - `TheSpecificityCountsEachStateInTheClassTier`: `:hover` 256, `.a:hover:focus` 768, `Label:checked` 257.
    - `ForThrowsForAWrongCompound`: `default(CompoundSelector)` and `(PseudoStates)32` throw `ArgumentException`.
  - File(s): `tests/BrutalUi.Core.Tests/Style/PseudoStateSelectorTests.cs`
  - Estimate: 10 steps

- [x] ✅ **1.7 Run the gate, the demo, and the grep** - Completed 2026-10-21
  - Description: Run the build, the tests, and the demo. Run `grep -nE 'System\.Reflection|GetType\(\)|typeof\(|HasFlag' src/BrutalUi.Core/Style/*.cs` and expect no output. Record the printed lines and the test total in `context.md`.
  - File(s): `context.md`
  - Estimate: 4 steps

---

## Phase 2: A Hover on a Panel Restyles Its Row and Its Cell (9/9 complete)

**Demo:** the developer runs `dotnet run --project tests/BrutalUi.Core.Tests/BrutalUi.Core.Tests.csproj -- --filter-method '*AHoverOnAPanelRestylesItsRowAndItsCell' --show-live-output on` and reads `row #00000000 cell #00000000`, then `row #3060C0FF cell #FFCC00FF` after the hover of the panel, then `row #00000000 cell #00000000` after the hover ends.
**Needs:** Phase 1
**Agent tier:** ADVANCED (opus)

**Group A (the chain, 4 files):**

- [x] ✅ **2.1 Write SelectorCombinator.cs** - Completed 2026-10-21
  - Description: Write `src/BrutalUi.Core/Style/SelectorCombinator.cs` per "The Public API". Each member has a doc comment with its CSS form.
  - File(s): `src/BrutalUi.Core/Style/SelectorCombinator.cs`
  - Estimate: 3 steps

- [x] ✅ **2.2 Add the ancestor match to CompoundSelector** - Completed 2026-10-21
  - Description: Edit `src/BrutalUi.Core/Style/CompoundSelector.cs`: add the internal `Matches(Element)` overload per "The Match". It reads `Element.TypeId` only when the compound has a type part.
  - File(s): `src/BrutalUi.Core/Style/CompoundSelector.cs`
  - Estimate: 3 steps

- [x] ✅ **2.3 Add the chain to Selector** - Completed 2026-10-21
  - Description: Edit `src/BrutalUi.Core/Style/Selector.cs`: add `_ancestor` and `_combinator`, `Ancestor`, `Combinator`, `Child`, and `Descendant` with the factory rules for each slot. `Specificity` sums the counts of both compounds. `Matches` walks per "The Match": a child chain reads the parent, and a descendant chain reads the parent and then the grandparent. The remarks state the bound of the walk and that the chain reads the tree, not the computed style.
  - File(s): `src/BrutalUi.Core/Style/Selector.cs`
  - Estimate: 8 steps

- [x] ✅ **2.4 Write ChainSelectorTests.cs** - Completed 2026-10-21
  - Description: Write `tests/BrutalUi.Core.Tests/Style/ChainSelectorTests.cs`. Each test sets the classes and the states before the first `Update`, so it needs no subtree mark:
    - `AChildRuleMatchesOnlyTheParent`: `.a > .b` matches with `.a` on the parent and misses with `.a` on the grandparent only.
    - `ADescendantRuleMatchesTheParentAndTheGrandparent`.
    - `ADescendantRuleStopsAtTheGrandparent`: `.a` on the great-grandparent only gives no match (open scope question 2).
    - `AChainNeedsTheSubject`: the ancestor matches and the subject misses.
    - `ARootMatchesNoChain`: a root with `.b` has no parent.
    - `AnAncestorStateRuleMatchesOnlyInTheState`: `.a:hover > .b`.
    - `ATypeAncestorReadsTheTypeOfTheAncestor`: `Label > .b` with a test subclass of `Element` that overrides `TypeId`, in the form of `TypeSelectorTests`.
    - `TheChainSpecificitySumsBothCompounds`: `.a > .b` 512, `.a:hover .b:focus` 1024, `Label .b` 257, and the 12-unit chain of "The specificity tiers" 3072.
    - `ChildAndDescendantThrowForAnEmptySlot`, and `TheBuilderAcceptsEachChainForm`.
  - File(s): `tests/BrutalUi.Core.Tests/Style/ChainSelectorTests.cs`
  - Estimate: 10 steps

**Group B (the subtree mark, 4 files and the test fixes):**

- [x] ✅ **2.5 The States setter marks the subtree** - Completed 2026-10-21
  - Description: Edit `src/BrutalUi.Core/Tree/Element.cs`: the `States` setter calls `MarkSubtreeStyle()` in place of `MarkDirty(DirtyFlags.Style)`. The remarks state A2: the element and its subtree get `Style`, and the mark reads no sheet.
  - File(s): `src/BrutalUi.Core/Tree/Element.cs`
  - Estimate: 3 steps

- [x] ✅ **2.6 ClassList.Add and Remove mark the subtree** - Completed 2026-10-21
  - Description: Edit `src/BrutalUi.Core/Tree/ClassList.cs`: `Add` and `Remove` call `_owner.MarkSubtreeStyle()` on a change. A duplicate add and a remove of an absent id still mark nothing. Edit the remarks of `ClassList` and of `src/BrutalUi.Core/Tree/PseudoStates.cs` to the same rule.
  - File(s): `src/BrutalUi.Core/Tree/ClassList.cs`, `src/BrutalUi.Core/Tree/PseudoStates.cs`
  - Estimate: 4 steps

- [x] ✅ **2.7 Fix the exact Dirty asserts of the subtree mark** - Completed 2026-10-21 (no assert failed, no fix)
  - Description: Run the tests. For each exact assert of `Dirty` that fails only because the subtree of the changed element now holds `Style` or `DescendantStyle`, add the new bits to the expected value. Change no other assert. Record each changed line in `context.md` (the form of task 10.2 of stylesheet-model). When a changed test asserts the old contract ("a class change marks only the owner"), rename it to the new contract and record the rename.
  - File(s): the failing test files (expectation lines only), `context.md`
  - Estimate: 6 steps

- [x] ✅ **2.8 Write SubtreeMarkTests.cs** - Completed 2026-10-21
  - Description: Write `tests/BrutalUi.Core.Tests/Style/SubtreeMarkTests.cs` on a tree of depth 4 (root, panel, row, cell) with a second branch (root, side):
    - `AHoverOnAPanelRestylesItsRowAndItsCell` (the demo): `.panel:hover > .row { BackgroundColor = (48, 96, 192, 255) }` and `.panel:hover .cell { BackgroundColor = (255, 204, 0, 255) }`. Print after `Update`, after `panel.States = Hover` and `Update`, and after `States = None` and `Update`. Assert the three lines. The rules set a paint member that does not inherit, so the result comes from the match and not from the parent.
    - `AClassAddMarksTheSubtreeAndTheAncestors`: after a clean `Update`, `panel.Classes.Add`. `panel` and `row` hold `Style | DescendantStyle`, `cell` holds `Style`, `root` holds `DescendantStyle`, and `side` holds `None`.
    - `AClassRemoveMarksTheSubtree`, and `AStateChangeMarksTheSubtree` (the same bits).
    - `TheSameStateAndADuplicateClassMarkNothing`.
    - `ALeafChangeMarksTheLeafOnly`: a state change on `cell` gives `Style` on `cell` and `DescendantStyle` on `row`, `panel`, and `root`.
    - `TheMarkKeepsTheInvariant`: after each mark, each element with a bit has the descendant bit on each ancestor.
    - `TheNextUpdateClearsEachStyleBit`: after `UpdateStyles()`, no element holds `Style` or `DescendantStyle`.
    - `AChainMatchAndAStateChangeAllocateNothing`: a tree of 50 elements and a sheet with a state rule, a child rule, and a descendant rule. Each step toggles `States` on a panel with children and toggles one class, then calls `Update`. Run 16 warm-up steps, call `AllocationGate.ReleaseAllocationContext()`, measure 100 steps, and assert 0 bytes (the form of `TheStylePassAllocatesNothingAfterWarmUp`).
  - File(s): `tests/BrutalUi.Core.Tests/Style/SubtreeMarkTests.cs`
  - Estimate: 12 steps

- [x] ✅ **2.9 Run the gate and the demo** - Completed 2026-10-21
  - Description: Run the build, the tests, and the demo. Record the printed lines, the test total, and the list of task 2.7 in `context.md`.
  - File(s): `context.md`
  - Estimate: 4 steps

---

## Phase 3: A Name Rule Wins Over a Class and a State (6/6 complete)

**Demo:** the developer runs `dotnet run --project tests/BrutalUi.Core.Tests/BrutalUi.Core.Tests.csproj -- --filter-method '*ANameRuleWinsOverAClassAndAState' --show-live-output on` and reads `title #00FF00FF`, then `subtitle #3060C0FF` after `Name = "subtitle"`.
**Needs:** Phase 2
**Agent tier:** STANDARD (sonnet)

- [x] ✅ **3.1 The Name setter marks the subtree** - Completed 2026-10-21
  - Description: Edit `src/BrutalUi.Core/Tree/Element.cs`: `Name` gets a private `_name` field. The setter returns at once when `string.Equals(_name, value, StringComparison.Ordinal)` is true. Otherwise it writes the value and calls `MarkSubtreeStyle()`. The doc comment states the mark and the ordinal compare, in place of "A change of the name sets no dirty bit".
  - File(s): `src/BrutalUi.Core/Tree/Element.cs`
  - Estimate: 4 steps

- [x] ✅ **3.2 Add the name part to CompoundSelector** - Completed 2026-10-21
  - Description: Edit `src/BrutalUi.Core/Style/CompoundSelector.cs`: add `Name` (`string?`, `init`). `IsEmpty` covers it. `Matches` tests it last with `string.Equals(element.Name, Name, StringComparison.Ordinal)`. The remarks state that `null` means no name part and that the compare is case-sensitive.
  - File(s): `src/BrutalUi.Core/Style/CompoundSelector.cs`
  - Estimate: 4 steps

- [x] ✅ **3.3 Add ForName and the name tier to Selector** - Completed 2026-10-21
  - Description: Edit `src/BrutalUi.Core/Style/Selector.cs`: add `ForName(string)` (`ArgumentException` for `null` and `""`). The factory rules reject an empty name in each slot. `Specificity` counts the name tier.
  - File(s): `src/BrutalUi.Core/Style/Selector.cs`
  - Estimate: 5 steps

- [x] ✅ **3.4 Write NameSelectorTests.cs** - Completed 2026-10-21
  - Description: Write `tests/BrutalUi.Core.Tests/Style/NameSelectorTests.cs`:
    - `ANameRuleWinsOverAClassAndAState` (the demo): an element with the name `title`, the class `panel`, and `States = Hover`. The rule `#title { BackgroundColor = (0, 255, 0, 255) }` comes first, and `.panel:hover { BackgroundColor = (48, 96, 192, 255) }` comes later. Print after `Update`, then after `Name = "subtitle"` and `Update`. Assert both lines.
    - `ANameRuleMatchesOnlyThatName`: `Title` and `title` differ, and a `null` name matches no name rule.
    - `ANameInAChain`: `#hud > .row` and `#hud .row`.
    - `ANameChangeMarksTheSubtree` on a tree of depth 3, and `TheSameNameMarksNothing`.
    - `TheSpecificityOrderIsNameThenClassThenType`: one name beats the 12-unit chain, a class beats `Label > Label` (2 types), and each pair holds in both orders of add.
    - `ALaterRuleWinsAtEqualSpecificity`: `.a` and `:hover` (256 each) on one element, in both orders.
    - `ForNameThrowsForNullOrEmpty`, and `ForThrowsForAnEmptyName`.
  - File(s): `tests/BrutalUi.Core.Tests/Style/NameSelectorTests.cs`
  - Estimate: 10 steps

- [x] ✅ **3.5 Run the build and the tests** - Completed 2026-10-21
  - Description: Run the build and the tests. `ElementStateTests`, `DirtyTrackingTests`, and the trees that set `Name` in an object initializer pass with no change: a new element holds `Style` already, and it has no child at the set.
  - File(s): none
  - Estimate: 3 steps

- [x] ✅ **3.6 Run the demo and record it** - Completed 2026-10-21
  - Description: Run the demo. Record the printed lines and the test total in `context.md`.
  - File(s): `context.md`
  - Estimate: 3 steps

---

## Phase 4: The Allocation Gate and the Record (9/9 complete)

**Demo:** the developer runs `dotnet run --project tests/BrutalUi.Core.Tests/BrutalUi.Core.Tests.csproj -- --filter-class '*ThousandElementAllocationTests'` and reads a total of 10 with `failed: 0`.
**Needs:** Phase 3
**Agent tier:** ADVANCED (opus)

**Group A (the gate, 2 files and a temporary edit):**

- [x] ✅ **4.1 Add 4 rules to the shared 1000-element tree** - Completed 2026-10-21
  - Description: Edit `tests/BrutalUi.Core.Tests/Shared/ThousandElementTree.cs`: add 4 rules at the end (`RuleCount` 16): a state rule (`.slot:hover`), a child rule, a descendant rule with a state on the ancestor, and a name rule on an element that the build names already. Each rule sets a value that changes no rect and no draw call, for example `BorderColor` on an element with no border width (D11.5). No rule sets a text color on the sidebar or below it (D11.4). Expose the values and the elements that the tests read. Update the class remarks and the remarks of the sheet build.
  - File(s): `tests/BrutalUi.Core.Tests/Shared/ThousandElementTree.cs`
  - Estimate: 8 steps

- [x] ✅ **4.2 Put the state change into the style step** - Completed 2026-10-21
  - Description: Edit `tests/BrutalUi.Core.Tests/ThousandElementAllocationTests.cs`: the style step also sets `States = PseudoStates.Hover` on the grid slot and on the ancestor of the descendant rule in the first round, and `PseudoStates.None` in the second round. The subtree mark thus runs in the measured body. `TheTreeHoldsTheRequiredMix` asserts 16 rules. Add `TheStyleStepMatchesEachSelectorForm`: one style step gives the hover value, the child value, and the descendant value, and the name value holds in each step. The next step gives the old values. No rect changes. Update the kind table of `StyleKind`.
  - File(s): `tests/BrutalUi.Core.Tests/ThousandElementAllocationTests.cs`
  - Estimate: 8 steps

- [x] ✅ **4.3 Prove that the gate blocks** - Completed 2026-10-21
  - Description: Copy `src/BrutalUi.Core/Style/Selector.cs` to the session scratch folder. Add `GC.KeepAlive(new object());` at the start of `Selector.Matches(Element, ElementTypeId)`. Run `dotnet test --solution brutal-ui.sln > /dev/null 2>&1; echo "exit=$?"` and expect a value that is not 0. Restore the file from the copy. Run `diff` on the two files and expect no output. Record both exit codes and the failed tests in `context.md`.
  - File(s): `src/BrutalUi.Core/Style/Selector.cs` (temporary, restored), `context.md`
  - Estimate: 5 steps

- [x] ✅ **4.4 Run the demo class 5 times, the facts line, and the smoke run** - Completed 2026-10-21
  - Description: Run the demo class 5 times and record the results. Run the facts line of task 1.1 and record `bytes per element`. Run `scripts/test-linux.sh; echo "exit=$?"` and expect the PASS line and `exit=0`.
  - File(s): `context.md`
  - Estimate: 5 steps

**Group B (the record, 5 files):**

- [x] ✅ **4.5 Update docs/technical-design.md** - Completed 2026-10-21
  - Description: Edit `docs/technical-design.md`. Section 2: the "Full CSS" line now names the child and descendant chains of depth 2. Section 5: `Name` and `States` mark the subtree. Section 7: the model of "The Public API" (replace the sketch of `Selector`), the specificity tiers and their table, the match rules with the bound of the descendant walk, the dirty rule for a class, a state, and a name change, and the design decisions.
  - File(s): `docs/technical-design.md`
  - Estimate: 8 steps

- [x] ✅ **4.6 Update the remarks of StylePass.cs** - Completed 2026-10-21
  - Description: Edit the remarks of `src/BrutalUi.Core/Style/StylePass.cs`: the match sentence names the compound parts and the chain. The code does not change.
  - File(s): `src/BrutalUi.Core/Style/StylePass.cs`
  - Estimate: 3 steps

- [x] ✅ **4.7 Update CHANGELOG.md** - Completed 2026-10-21
  - Description: Edit `CHANGELOG.md`: one line under `### Added` (`CompoundSelector`, `SelectorCombinator`, `Selector.For`, `Child`, `Descendant`, `ForName`, and the pseudo-state, name, and chain match) and one line under `### Changed` (the specificity numbers, the subtree mark on a class or state change, and the mark on a name change). Name the feature `style-system/selector-matching`.
  - File(s): `CHANGELOG.md`
  - Estimate: 3 steps

- [x] ✅ **4.8 Update the human-testing list of the epic** - Completed 2026-10-21
  - Description: Edit `docs/features/style-system/human-testing.md`: add a `## selector-matching` section with the 4 phase demos and the Windows run of `.\scripts\test-windows.ps1`.
  - File(s): `docs/features/style-system/human-testing.md`
  - Estimate: 3 steps

- [x] ✅ **4.9 Lint the docs and write the hand-offs** - Completed 2026-10-21
  - Description: Run `python3 /home/aaronstatic/.claude/skills/asd-ste100/scripts/ste-lint.py --fail-over 2.5` on each changed doc and on this plan, and fix each hit. Write the hand-offs of Notes into `context.md`. Add a "Native Dependencies" note: none. Run the build and the tests.
  - File(s): `context.md`, each changed doc
  - Estimate: 6 steps

---

## Bugs & Issues

**Active Bugs:**
- None

**Fixed Bugs:**
- None

---

## Technical Debt

- [ ] 💳 **CR-4: equality members and a text form for the selector types** - Priority: Low
  - Description: `CompoundSelector` and `Selector` have no `IEquatable<T>`, no `==`, and no readable `ToString`.
  - Reason: Deferred to style-parser, because the fix adds public API that this plan does not name.
  - Effort: Medium (question 2 of the style-parser draft plan)

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
