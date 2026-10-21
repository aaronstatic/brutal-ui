# Selector Matching - Implementation Plan

**Epic:** style-system (feature 2 of 4)
**Status:** Planning
**Started:** 2026-10-21
**Target Completion:** TBD
**Last Updated:** 2026-10-21 17:45

---

## Quality Bar

**Target quality:** A core library feature. The matcher of this feature decides which rule reaches each element. The input-routing epic sets `Element.States` from events later, and each `:hover` and `:focus` rule of a game then runs through this code. style-parser writes the full selector grammar of this feature, so the public selector model must hold each form that the parser reads. A wrong match, a wrong order, or a missed dirty bit gives a wrong screen, and no pixel test catches it.

- **A correct match:** A compound matches only when each of its parts matches: the type, the class, the name, and each pseudo-state. A child chain reads the parent only. A descendant chain reads the parent and the grandparent only (open scope question 2). A test proves each part and each chain form, and a miss for each.
- **A correct specificity order:** The order is name, then class and pseudo-state, then type, as in CSS. A later rule wins at equal specificity. The order holds for the largest selector of the grammar: 12 units of the class tier lose to one name. A test proves it in each order of add.
- **Deterministic dirty bits:** A class change, a pseudo-state change, and a name change mark the element and its full subtree with `Style`, and each ancestor with `DescendantStyle`. A change to the same value marks nothing. The dirty-bit invariant holds after each mark.
- **Zero allocation:** `UiContext.Update` allocates 0 bytes after warm-up on the 1000-element tree with a pseudo-state rule, a child rule, a descendant rule, and a name rule in the sheet, and with a state change in the measured body. A planted allocation in the match path proves that the gate blocks.
- **A clean model for style-parser:** Each selector form of the grammar has one public way to build it. A wrong selector fails fast with a message that names the fix. `StylePass` does not change its call of `Selector.Matches`.
- **No new pin and no ImGui in Core:** `Directory.Packages.props` and each `.csproj` do not change. The match path uses no reflection and no `Enum.HasFlag`.

---

## Executive Summary

This feature completes the selector of the style system. A selector of stylesheet-model holds one type part and one class part. This feature turns that pair into a compound with four parts: the type, one class, the name, and a set of pseudo-states. A selector then holds one subject compound, an optional ancestor compound, and a combinator (`None`, `Child`, or `Descendant`). The depth is a constant of 2 (A9): the selector holds two compound slots and no list.

The match stays inside `Selector.Matches(Element, ElementTypeId)`, the call seam of the style pass (D9.6, D9.7). Thus `StylePass` does not change. A child chain reads `element.Parent`. A descendant chain reads the parent and then the grandparent, and it stops there. The specificity moves from the decimal sum of stylesheet-model to one byte for each tier, so the CSS order holds for each selector of the grammar (CR-3).

A class change, a pseudo-state change, and a name change now call `Element.MarkSubtreeStyle()` in place of `MarkDirty(DirtyFlags.Style)` (A2). The pass visits only elements with a style bit, so the subtree mark is the path that lets a chain rule see a change of an ancestor. The allocation gate of the 1000-element tree gets one rule of each new form and a state change in its style step.

---

## User Personas Alignment

No `docs/user-personas.md` file exists. The personas below come from `docs/mission-statement.md` and the epic docs.

**Target Users:**
- [x] **The game developer** (writes `.button:hover`, `.panel > .row`, `.hud .label`, and `#title` rules in code, and sets classes, names, and states on elements)
- [x] **The sibling feature agents** (style-parser maps each selector form of the text format to the public model. transitions start a tween when a state change restyles a subtree)
- [x] **The later epics** (input-routing sets `Element.States` from pointer and keyboard events. The framework layer sets classes from state)
- [ ] **The markup author** (not served. The text format comes in style-parser)

**User Subsets Served:**
- [x] **Simple** - `Selector.For(new CompoundSelector { Class = button, States = PseudoStates.Hover })` gives `.button:hover`. `button.States = PseudoStates.Hover;` restyles the button on the next `Update`.
- [x] **Complex** - A child chain and a descendant chain with a state on the ancestor, a name rule over a class rule, and a hover on a high panel that restyles each row and each cell below it.

**UX Approach:**
- One way to build each form: `Selector.For`, `Selector.Child`, and `Selector.Descendant` take `CompoundSelector` values. The shortcuts `ForClass`, `ForType`, `ForTypeAndClass`, and `ForName` stay for the common cases.
- A wrong selector fails at once: an empty compound, an unknown state bit, and an empty name throw `ArgumentException` with a message that names the fix.
- The game calls no `MarkDirty` for a class, a state, or a name. The setter marks the subtree.

---

## Goals

### Primary Goals
1. Match a selector on the type, one class, the name, and the pseudo-states of one compound.
2. Match a child chain and a descendant chain of depth 2, with a constant cost for each rule.
3. Order the rules by name, then class and pseudo-state, then type, for each selector of the grammar.
4. Mark the full subtree style-dirty on a class, state, or name change (A2), with no check of the sheet.
5. Keep `Update` free of managed allocation with the new selector forms in the sheet.

### Success Criteria
- [ ] The phase 1 demo prints `button #1E222CFF`, then `button hover #3060C0FF`, then `button #1E222CFF`.
- [ ] The phase 2 demo prints `row #00000000 cell #00000000`, then `row #3060C0FF cell #FFCC00FF` after the hover of the panel, then the first line again.
- [ ] The phase 3 demo prints `title #00FF00FF`, then `subtitle #3060C0FF` after the rename.
- [ ] `ThousandElementAllocationTests` measures 0 bytes with a sheet of 16 rules and a state change in the style step.
- [ ] One name beats a selector with 12 units of the class tier in each order of add.

---

## Architecture Overview

### File Tree (end state)

```
src/BrutalUi.Core/
├── Style/
│   ├── CompoundSelector.cs                   # new. Phase 1: type, class, states. Phase 2: the ancestor match. Phase 3: name
│   ├── SelectorCombinator.cs                 # new. Phase 2: None, Child, Descendant
│   ├── Selector.cs                           # phase 1: the subject compound and the byte tiers. Phase 2: the chain. Phase 3: ForName
│   ├── StyleSheet.cs                         # phase 1: the Add doc and the exception text name Selector.For
│   └── StylePass.cs                          # phase 4: the remarks only. The code does not change
├── Tree/Element.cs                           # phase 2: States marks the subtree. Phase 3: Name marks the subtree
├── Tree/ClassList.cs                         # phase 2: Add and Remove mark the subtree
└── Tree/PseudoStates.cs                      # phase 2: the remarks only
tests/BrutalUi.Core.Tests/
├── Style/PseudoStateSelectorTests.cs         # new. Phase 1
├── Style/ChainSelectorTests.cs               # new. Phase 2
├── Style/SubtreeMarkTests.cs                 # new. Phase 2
├── Style/NameSelectorTests.cs                # new. Phase 3
├── Style/StyleSheetTests.cs                  # phase 1: the specificity numbers
├── Style/TypeSelectorTests.cs                # phase 1: the specificity numbers
├── Shared/ThousandElementTree.cs             # phase 4: 4 new rules
└── ThousandElementAllocationTests.cs         # phase 4: the state change in the style step
docs/technical-design.md, CHANGELOG.md, docs/features/style-system/human-testing.md   # phase 4
```

`src/BrutalUi.ImGui/`, `src/BrutalUi.Markup/`, `Directory.Packages.props`, and each `.csproj` do not change. The size of `Element` does not change: the auto-property `Name` already holds a backing field.

### The Public API

All types live in `namespace BrutalUi.Core;`. Each new file holds its own `using` lines (no implicit usings).

```csharp
public enum SelectorCombinator : byte          // phase 2
{
    None,         // one compound
    Child,        // .a > .b : the parent matches the ancestor compound
    Descendant,   // .a .b   : the parent or the grandparent matches the ancestor compound (open scope question 2)
}

public readonly struct CompoundSelector        // phase 1. One compound of CSS: Type.class#name:state:state
{
    public ElementTypeId Type { get; init; }   // ElementTypeId.None, or the exact type id (D9.8)
    public ClassId Class { get; init; }        // ClassId.None, or one class
    public string? Name { get; init; }         // phase 3. null, or the exact name (ordinal compare)
    public PseudoStates States { get; init; }  // None, or each state that the element must hold
    internal bool IsEmpty { get; }             // no part
    internal bool Matches(Element element, ElementTypeId elementType);
    internal bool Matches(Element element);    // phase 2. Reads Element.TypeId only for a type part
}

public readonly struct Selector
{
    public static Selector For(in CompoundSelector subject);                                       // phase 1
    public static Selector Child(in CompoundSelector parent, in CompoundSelector subject);         // phase 2
    public static Selector Descendant(in CompoundSelector ancestor, in CompoundSelector subject);  // phase 2
    public static Selector ForClass(ClassId id);                                // stays
    public static Selector ForType(ElementTypeId type);                         // stays
    public static Selector ForTypeAndClass(ElementTypeId type, ClassId id);     // stays
    public static Selector ForName(string name);                                // phase 3: #name
    public CompoundSelector Subject { get; }                                    // phase 1
    public CompoundSelector Ancestor { get; }                                   // phase 2. default when Combinator is None
    public SelectorCombinator Combinator { get; }                               // phase 2
    public ClassId Class { get; }                                               // stays: Subject.Class
    public ElementTypeId Type { get; }                                          // stays: Subject.Type
    public int Specificity { get; }             // (names << 16) | ((classes + states) << 8) | types, over both compounds
    internal bool IsEmpty { get; }              // the subject is empty (D1.6)
    internal bool Matches(Element element, ElementTypeId elementType);         // the call seam of StylePass (no change)
}

public class Element
{
    public string? Name { get; set; }          // phase 3. A new value (ordinal) marks the subtree Style
    public PseudoStates States { get; set; }   // phase 2. A new value marks the subtree Style
}

public readonly struct ClassList
{
    public bool Add(ClassId id);               // phase 2. A change marks the subtree of the owner Style
    public bool Remove(ClassId id);            // phase 2. A change marks the subtree of the owner Style
}
```

**The factory rules.** `For`, `Child`, and `Descendant` throw `ArgumentException` for an empty compound (each slot), for a state bit outside the five members, and for an empty name (`""`). A `null` name means no name part. `ForName` throws for `null` and for `""`. `ForTypeAndClass` keeps its rule of D9.5. `default(Selector)` stays empty, matches nothing, and the builder rejects it.

**The specificity tiers.** Each tier holds one byte of the `int`. The name tier counts each compound with a name. The class tier counts each class and each set state bit (`BitOperations.PopCount`). The type tier counts each compound with a type. The largest selector of the grammar holds 2 names, 12 units of the class tier, and 2 types, so each count stays far below 256. `Selector.Specificity` stays the one place of the weights (the private shift constants). `StyleRule` stores the value at creation, and both sort keys (D1.8, D1.11) keep the value in the high 32 bits.

| Selector | Specificity |
|----------|-------------|
| `Label` | 1 |
| `.a` or `:hover` | 256 |
| `Label.a` | 257 |
| `.a:hover` or `.a > .b` | 512 |
| `.a:hover:active:focus:disabled:checked .b:hover:active:focus:disabled:checked` | 3072 |
| `#title` | 65536 |

### The Match (Chosen Approach: two fixed compound slots inside `Selector`)

```
StylePass.Restyle(element)                                   // no change
  type = element.TypeId                                      // one virtual call (D9.7)
  for each rule: if rule.Selector.Matches(element, type) ... // the call seam

Selector.Matches(element, elementType):
  if IsEmpty: return false
  if !_subject.Matches(element, elementType): return false   // the subject first: most rules miss here
  switch _combinator:
    None:       return true
    Child:      p = element.Parent
                return p is not null && _ancestor.Matches(p)
    Descendant: p = element.Parent
                if p is null: return false
                if _ancestor.Matches(p): return true
                g = p.Parent
                return g is not null && _ancestor.Matches(g) // the walk stops at the grandparent

CompoundSelector.Matches(element, elementType):              // the cheapest test first
  if (element.States & States) != States: return false      // bit math, no HasFlag
  if !Type.IsNone && Type != elementType: return false
  if !Class.IsNone && !element.ClassStorage.Contains(Class): return false
  if Name is not null && !string.Equals(element.Name, Name, StringComparison.Ordinal): return false   // phase 3
  return true

CompoundSelector.Matches(element):                           // an ancestor
  return Matches(element, Type.IsNone ? ElementTypeId.None : element.TypeId)   // no virtual call with no type part
```

- **No defensive copy.** `Selector` and `CompoundSelector` are `readonly struct` types, and the pass reads the rule through `ref readonly`. Each call on a field is thus a call on the field, with no copy.
- **The match reads the tree, not the computed style.** A chain reads the classes, the states, and the name of the ancestor. The order of the visit thus does not change the result.
- **A move restyles.** An attach marks the full subtree of the moved element (CR-1), so a new parent or a new grandparent restyles each descendant. A detached subtree restyles on its next attach.

### The Subtree Mark (A2)

```
ClassList.Add(id) / Remove(id)  ─► changed?  ─► _owner.MarkSubtreeStyle()     // phase 2 (was MarkDirty(Style))
Element.States = value          ─► changed?  ─► MarkSubtreeStyle()            // phase 2 (was MarkDirty(Style))
Element.Name = value            ─► changed?  ─► MarkSubtreeStyle()            // phase 3 (was no mark)

MarkSubtreeStyle():             Style on each element of the subtree, DescendantStyle on each element with a child,
                                then DescendantStyle up the parent chain (D1.12). No allocation.
```

The mark does not read the sheet (A2, out of scope). A change to the same value returns before the mark. A leaf gets `Style` only, so each existing leaf test keeps its expectation.

### Data Flow

```
Load:   Selector.Descendant(new CompoundSelector { Class = panel, States = Hover }, new CompoundSelector { Class = cell })
          ─► StyleSheet.Builder.Add ─► StyleRule.Specificity (stored) ─► context.AddSheet ─► _cascade
Frame:  panel.States = PseudoStates.Hover ─► MarkSubtreeStyle (panel, row, cell: Style)
        UiContext.Update ─► UpdateStyles ─► Visit(panel, row, cell) ─► Restyle ─► Selector.Matches (parent, grandparent)
                         ─► Compare ─► MarkDirty(Paint) ─► layout pass ─► Draw
Tests:  context.UpdateStyles() alone, then read element.Dirty before the layout pass clears it.
```

---

## Implementation Phases

Each phase is a slice, per section 11 of `.beast-mode/phase-handoff.md`. No phase is a base phase: the weight change rides with phase 1, because a pseudo-state adds to the class tier. The requirements leave the demo of phase 1 open, so phase 1 uses the recommended line of open scope question 4 (rule 12).

Each phase ends with `dotnet build brutal-ui.sln 2>&1 | tail -n 30` and `dotnet test --solution brutal-ui.sln 2>&1 | tail -n 30` (tool rule 5). Expect `0 Warning(s)`, `0 Error(s)`, and 0 failed. A demo uses `dotnet run --project tests/BrutalUi.Core.Tests/BrutalUi.Core.Tests.csproj -- --filter-method '*Name' --show-live-output on`. A test prints with `TestContext.Current.TestOutputHelper?.WriteLine(...)`. The class table, the type table, and the names are global, and xUnit runs classes in parallel, so each test interns names with the prefix `nameof(Test)`. A color prints as `#RRGGBBAA`, as in the tests of stylesheet-model.

### Phase 1: A Hover Rule Restyles a Button
**Goal:** The smallest path that proves the idea: a compound with a class and a pseudo-state, the match on `Element.States`, and the byte tiers of the specificity, through the existing pass.
**Demo:** the developer runs `dotnet run --project tests/BrutalUi.Core.Tests/BrutalUi.Core.Tests.csproj -- --filter-method '*AHoverRuleRestylesTheButton' --show-live-output on` and reads `button #1E222CFF`, then `button hover #3060C0FF` after `States = Hover`, then `button #1E222CFF` after `States = None`.
**Needs:** none
**Agent tier:** ADVANCED (opus)

**Tier note:** This phase fixes the public selector model for style-parser and changes the specificity of each existing rule. A wrong tier, a lost check of `IsEmpty`, or a defensive copy in the match gives a silent cascade bug.

**Tasks:**

Group A (the compound and the tiers, 6 files):
- [ ] 1.1 Record in `context.md`: the base commit (`git rev-parse HEAD`), the test total (expected 1192), the build result, and the `bytes per element` line of `dotnet run -c Release --project tests/BrutalUi.Benchmarks/BrutalUi.Benchmarks.csproj -- --facts 2>&1 | tail -n 30`. Read `Selector.cs`, `StyleSheet.cs`, `StyleRule.cs`, `StylePass.Restyle`, `ClassId.cs`, and `PseudoStates.cs`.
- [ ] 1.2 Write `src/BrutalUi.Core/Style/CompoundSelector.cs` per "The Public API", with `Type`, `Class`, and `States` (no `Name` yet), the internal `IsEmpty`, and the internal `Matches(Element, ElementTypeId)` per "The Match". The remarks state the parts, the exact type match (D9.8), and that a state-only compound matches each element in the state.
- [ ] 1.3 Edit `src/BrutalUi.Core/Style/Selector.cs`: one private `CompoundSelector _subject` in place of the two parts. `Class` and `Type` read the subject. Add `Subject` and `For(in CompoundSelector)` with the factory rules. `ForClass`, `ForType`, and `ForTypeAndClass` build a compound and keep their throws. `Specificity` uses the byte tiers with private shift constants and `BitOperations.PopCount`. `Matches` calls the subject match. Replace the CR-3 remarks with the tier rule and the bound of "The specificity tiers".
- [ ] 1.4 Edit `src/BrutalUi.Core/Style/StyleSheet.cs`: the `Add` parameter doc and the exception text name `Selector.For` next to the three shortcuts.
- [ ] 1.5 Edit `tests/BrutalUi.Core.Tests/Style/StyleSheetTests.cs` and `tests/BrutalUi.Core.Tests/Style/TypeSelectorTests.cs`: each specificity 10 becomes 256, and each 11 becomes 257. Rename `ForClassGivesTheClassAndASpecificityOfTen` to `ForClassGivesTheClassAndTheSpecificityOfOneClass`, and `TheSpecificityIsOneTenOrEleven` to `TheSpecificityPutsEachTierInItsOwnByte`. Change no other assert. Record each changed line in `context.md`.
- [ ] 1.6 Write `tests/BrutalUi.Core.Tests/Style/PseudoStateSelectorTests.cs`:
  - `AHoverRuleRestylesTheButton` (the demo): `.button { BackgroundColor = (30, 34, 44, 255) }` and `.button:hover { BackgroundColor = (48, 96, 192, 255) }`. Print after `Update`, after `States = Hover` and `Update`, and after `States = None` and `Update`. Assert the three lines.
  - `EachPseudoStateMatchesItsOwnBit` (a theory with 5 rows): `.x:<state>` matches with the state, misses without it, and misses with each other state alone.
  - `ACompoundNeedsEachState`: `:hover:focus` misses with `Hover` alone and matches with `Hover | Focus | Checked`.
  - `ATypeClassAndStateCompoundNeedsEachPart`: `Label.x:checked` misses when one part is absent.
  - `AStateOnlyRuleMatchesEachElementInTheState`: `:disabled` alone.
  - `AStateRuleWinsOverTheClassRuleInEachOrder`: two sheets with the two orders of add.
  - `TheSpecificityCountsEachStateInTheClassTier`: `:hover` 256, `.a:hover:focus` 768, `Label:checked` 257.
  - `ForThrowsForAWrongCompound`: `default(CompoundSelector)` and `(PseudoStates)32` throw `ArgumentException`.
- [ ] 1.7 Run the build, the tests, and the demo. Run `grep -nE 'System\.Reflection|GetType\(\)|typeof\(|HasFlag' src/BrutalUi.Core/Style/*.cs` and expect no output. Record the printed lines and the test total in `context.md`.

**Estimated Steps:** about 45 (7 tasks, about six steps for one task)

**Files (6):** `CompoundSelector.cs`, `Selector.cs`, `StyleSheet.cs`, `StyleSheetTests.cs`, `TypeSelectorTests.cs`, `PseudoStateSelectorTests.cs` (and `context.md`).

**Acceptance Criteria:**
- [ ] The demo prints the three lines, and the test asserts them.
- [ ] Each existing test passes. Only the specificity numbers and the two test names of task 1.5 changed.
- [ ] `TheStylePassAllocatesNothingAfterWarmUp` and `ThousandElementAllocationTests` pass with 0 bytes.

---

### Phase 2: A Hover on a Panel Restyles Its Row and Its Cell
**Goal:** The child chain and the descendant chain, and the subtree mark on a class change and on a state change, so a change of an ancestor reaches each element that a chain rule matches.
**Demo:** the developer runs `dotnet run --project tests/BrutalUi.Core.Tests/BrutalUi.Core.Tests.csproj -- --filter-method '*AHoverOnAPanelRestylesItsRowAndItsCell' --show-live-output on` and reads `row #00000000 cell #00000000`, then `row #3060C0FF cell #FFCC00FF` after the hover of the panel, then `row #00000000 cell #00000000` after the hover ends.
**Needs:** Phase 1
**Agent tier:** ADVANCED (opus)

**Tier note:** This phase is the load-bearing part of the feature: the ancestor walk, its bound, and the dirty propagation. A missed mark gives a stale style that only a later, unrelated change repairs. A wrong walk gives a match at the wrong depth.

**Tasks:**

Group A (the chain, 4 files):
- [ ] 2.1 Write `src/BrutalUi.Core/Style/SelectorCombinator.cs` per "The Public API". Each member has a doc comment with its CSS form.
- [ ] 2.2 Edit `src/BrutalUi.Core/Style/CompoundSelector.cs`: add the internal `Matches(Element)` overload per "The Match". It reads `Element.TypeId` only when the compound has a type part.
- [ ] 2.3 Edit `src/BrutalUi.Core/Style/Selector.cs`: add `_ancestor` and `_combinator`, `Ancestor`, `Combinator`, `Child`, and `Descendant` with the factory rules for each slot. `Specificity` sums the counts of both compounds. `Matches` walks per "The Match": a child chain reads the parent, and a descendant chain reads the parent and then the grandparent. The remarks state the bound of the walk and that the chain reads the tree, not the computed style.
- [ ] 2.4 Write `tests/BrutalUi.Core.Tests/Style/ChainSelectorTests.cs`. Each test sets the classes and the states before the first `Update`, so it needs no subtree mark:
  - `AChildRuleMatchesOnlyTheParent`: `.a > .b` matches with `.a` on the parent and misses with `.a` on the grandparent only.
  - `ADescendantRuleMatchesTheParentAndTheGrandparent`.
  - `ADescendantRuleStopsAtTheGrandparent`: `.a` on the great-grandparent only gives no match (open scope question 2).
  - `AChainNeedsTheSubject`: the ancestor matches and the subject misses.
  - `ARootMatchesNoChain`: a root with `.b` has no parent.
  - `AnAncestorStateRuleMatchesOnlyInTheState`: `.a:hover > .b`.
  - `ATypeAncestorReadsTheTypeOfTheAncestor`: `Label > .b` with a test subclass of `Element` that overrides `TypeId`, in the form of `TypeSelectorTests`.
  - `TheChainSpecificitySumsBothCompounds`: `.a > .b` 512, `.a:hover .b:focus` 1024, `Label .b` 257, and the 12-unit chain of "The specificity tiers" 3072.
  - `ChildAndDescendantThrowForAnEmptySlot`, and `TheBuilderAcceptsEachChainForm`.

Group B (the subtree mark, 4 files and the test fixes):
- [ ] 2.5 Edit `src/BrutalUi.Core/Tree/Element.cs`: the `States` setter calls `MarkSubtreeStyle()` in place of `MarkDirty(DirtyFlags.Style)`. The remarks state A2: the element and its subtree get `Style`, and the mark reads no sheet.
- [ ] 2.6 Edit `src/BrutalUi.Core/Tree/ClassList.cs`: `Add` and `Remove` call `_owner.MarkSubtreeStyle()` on a change. A duplicate add and a remove of an absent id still mark nothing. Edit the remarks of `ClassList` and of `src/BrutalUi.Core/Tree/PseudoStates.cs` to the same rule.
- [ ] 2.7 Run the tests. For each exact assert of `Dirty` that fails only because the subtree of the changed element now holds `Style` or `DescendantStyle`, add the new bits to the expected value. Change no other assert. Record each changed line in `context.md` (the form of task 10.2 of stylesheet-model). When a changed test asserts the old contract ("a class change marks only the owner"), rename it to the new contract and record the rename.
- [ ] 2.8 Write `tests/BrutalUi.Core.Tests/Style/SubtreeMarkTests.cs` on a tree of depth 4 (root, panel, row, cell) with a second branch (root, side):
  - `AHoverOnAPanelRestylesItsRowAndItsCell` (the demo): `.panel:hover > .row { BackgroundColor = (48, 96, 192, 255) }` and `.panel:hover .cell { BackgroundColor = (255, 204, 0, 255) }`. Print after `Update`, after `panel.States = Hover` and `Update`, and after `States = None` and `Update`. Assert the three lines. The rules set a paint member that does not inherit, so the result comes from the match and not from the parent.
  - `AClassAddMarksTheSubtreeAndTheAncestors`: after a clean `Update`, `panel.Classes.Add`. `panel` and `row` hold `Style | DescendantStyle`, `cell` holds `Style`, `root` holds `DescendantStyle`, and `side` holds `None`.
  - `AClassRemoveMarksTheSubtree`, and `AStateChangeMarksTheSubtree` (the same bits).
  - `TheSameStateAndADuplicateClassMarkNothing`.
  - `ALeafChangeMarksTheLeafOnly`: a state change on `cell` gives `Style` on `cell` and `DescendantStyle` on `row`, `panel`, and `root`.
  - `TheMarkKeepsTheInvariant`: after each mark, each element with a bit has the descendant bit on each ancestor.
  - `TheNextUpdateClearsEachStyleBit`: after `UpdateStyles()`, no element holds `Style` or `DescendantStyle`.
  - `AChainMatchAndAStateChangeAllocateNothing`: a tree of 50 elements and a sheet with a state rule, a child rule, and a descendant rule. Each step toggles `States` on a panel with children and toggles one class, then calls `Update`. Run 16 warm-up steps, call `AllocationGate.ReleaseAllocationContext()`, measure 100 steps, and assert 0 bytes (the form of `TheStylePassAllocatesNothingAfterWarmUp`).
- [ ] 2.9 Run the build, the tests, and the demo. Record the printed lines, the test total, and the list of task 2.7 in `context.md`.

**Estimated Steps:** about 60 (9 tasks, about six to seven steps for one task)

**Files (8 and the fixes):** Group A: `SelectorCombinator.cs`, `CompoundSelector.cs`, `Selector.cs`, `ChainSelectorTests.cs`. Group B: `Element.cs`, `ClassList.cs`, `PseudoStates.cs`, `SubtreeMarkTests.cs`, and the test files of task 2.7 (expectation lines only. The search of the plan found leaf asserts only in `ClassListTests` and `ElementStateTests`, so it expects few or no fixes).

**Acceptance Criteria:**
- [ ] The demo prints the three lines, and the test asserts them.
- [ ] `DirtyTrackingTests.TenThousandSeededRandomCallsKeepTheInvariantAfterEachCall` passes with no change.
- [ ] `AChainMatchAndAStateChangeAllocateNothing`, `TheStylePassAllocatesNothingAfterWarmUp`, and `ThousandElementAllocationTests` pass with 0 bytes.
- [ ] No expected rect changed: `git diff -U0 <base commit> -- tests | grep -c '^[-+].*AssertRect'` prints `0`.

---

### Phase 3: A Name Rule Wins Over a Class and a State
**Goal:** The name part of a compound, the subtree mark on a name change, and the proof of the full specificity order.
**Demo:** the developer runs `dotnet run --project tests/BrutalUi.Core.Tests/BrutalUi.Core.Tests.csproj -- --filter-method '*ANameRuleWinsOverAClassAndAState' --show-live-output on` and reads `title #00FF00FF`, then `subtitle #3060C0FF` after `Name = "subtitle"`.
**Needs:** Phase 2
**Agent tier:** STANDARD (sonnet)

**Tier note:** Phases 1 and 2 fix the match path, the tiers, and the subtree mark. This phase adds one part in the same shape and calls the existing mark. The order proof is a test.

**Tasks:**
- [ ] 3.1 Edit `src/BrutalUi.Core/Tree/Element.cs`: `Name` gets a private `_name` field. The setter returns at once when `string.Equals(_name, value, StringComparison.Ordinal)` is true. Otherwise it writes the value and calls `MarkSubtreeStyle()`. The doc comment states the mark and the ordinal compare, in place of "A change of the name sets no dirty bit".
- [ ] 3.2 Edit `src/BrutalUi.Core/Style/CompoundSelector.cs`: add `Name` (`string?`, `init`). `IsEmpty` covers it. `Matches` tests it last with `string.Equals(element.Name, Name, StringComparison.Ordinal)`. The remarks state that `null` means no name part and that the compare is case-sensitive.
- [ ] 3.3 Edit `src/BrutalUi.Core/Style/Selector.cs`: add `ForName(string)` (`ArgumentException` for `null` and `""`). The factory rules reject an empty name in each slot. `Specificity` counts the name tier.
- [ ] 3.4 Write `tests/BrutalUi.Core.Tests/Style/NameSelectorTests.cs`:
  - `ANameRuleWinsOverAClassAndAState` (the demo): an element with the name `title`, the class `panel`, and `States = Hover`. The rule `#title { BackgroundColor = (0, 255, 0, 255) }` comes first, and `.panel:hover { BackgroundColor = (48, 96, 192, 255) }` comes later. Print after `Update`, then after `Name = "subtitle"` and `Update`. Assert both lines.
  - `ANameRuleMatchesOnlyThatName`: `Title` and `title` differ, and a `null` name matches no name rule.
  - `ANameInAChain`: `#hud > .row` and `#hud .row`.
  - `ANameChangeMarksTheSubtree` on a tree of depth 3, and `TheSameNameMarksNothing`.
  - `TheSpecificityOrderIsNameThenClassThenType`: one name beats the 12-unit chain, a class beats `Label > Label` (2 types), and each pair holds in both orders of add.
  - `ALaterRuleWinsAtEqualSpecificity`: `.a` and `:hover` (256 each) on one element, in both orders.
  - `ForNameThrowsForNullOrEmpty`, and `ForThrowsForAnEmptyName`.
- [ ] 3.5 Run the build and the tests. `ElementStateTests`, `DirtyTrackingTests`, and the trees that set `Name` in an object initializer pass with no change: a new element holds `Style` already, and it has no child at the set.
- [ ] 3.6 Run the demo. Record the printed lines and the test total in `context.md`.

**Estimated Steps:** about 35 (6 tasks, about six steps for one task)

**Files (4):** `Element.cs`, `CompoundSelector.cs`, `Selector.cs`, `NameSelectorTests.cs` (and `context.md`).

**Acceptance Criteria:**
- [ ] The demo prints the two lines, and the test asserts them.
- [ ] `TheSpecificityOrderIsNameThenClassThenType` passes in each order of add.
- [ ] The allocation tests pass with 0 bytes.

---

### Phase 4: The Allocation Gate and the Record
**Goal:** Prove that `Update` allocates nothing with each new selector form on the 1000-element tree and with a state change in the measured body. Prove that the gate blocks. Put the selector model into the design docs, the changelog, and the hand-offs.
**Demo:** the developer runs `dotnet run --project tests/BrutalUi.Core.Tests/BrutalUi.Core.Tests.csproj -- --filter-class '*ThousandElementAllocationTests'` and reads a total of 10 with `failed: 0`.
**Needs:** Phase 3
**Agent tier:** ADVANCED (opus)

**Tier note:** The allocation gate is load-bearing. When the gate measures bytes above 0, find the cause in `src/` before a change of the test. The shared tree compiles into three projects, so it uses the public API only. Group B is plain documentation work.

**Tasks:**

Group A (the gate, 2 files and a temporary edit):
- [ ] 4.1 Edit `tests/BrutalUi.Core.Tests/Shared/ThousandElementTree.cs`: add 4 rules at the end (`RuleCount` 16): a state rule (`.slot:hover`), a child rule, a descendant rule with a state on the ancestor, and a name rule on an element that the build names already. Each rule sets a value that changes no rect and no draw call, for example `BorderColor` on an element with no border width (D11.5). No rule sets a text color on the sidebar or below it (D11.4). Expose the values and the elements that the tests read. Update the class remarks and the remarks of the sheet build.
- [ ] 4.2 Edit `tests/BrutalUi.Core.Tests/ThousandElementAllocationTests.cs`: the style step also sets `States = PseudoStates.Hover` on the grid slot and on the ancestor of the descendant rule in the first round, and `PseudoStates.None` in the second round. The subtree mark thus runs in the measured body. `TheTreeHoldsTheRequiredMix` asserts 16 rules. Add `TheStyleStepMatchesEachSelectorForm`: one style step gives the hover value, the child value, and the descendant value, and the name value holds in each step. The next step gives the old values. No rect changes. Update the kind table of `StyleKind`.
- [ ] 4.3 Prove that the gate blocks. Copy `src/BrutalUi.Core/Style/Selector.cs` to the session scratch folder. Add `GC.KeepAlive(new object());` at the start of `Selector.Matches(Element, ElementTypeId)`. Run `dotnet test --solution brutal-ui.sln > /dev/null 2>&1; echo "exit=$?"` and expect a value that is not 0. Restore the file from the copy. Run `diff` on the two files and expect no output. Record both exit codes and the failed tests in `context.md`.
- [ ] 4.4 Run the demo class 5 times and record the results. Run the facts line of task 1.1 and record `bytes per element`. Run `scripts/test-linux.sh; echo "exit=$?"` and expect the PASS line and `exit=0`.

Group B (the record, 5 files):
- [ ] 4.5 Edit `docs/technical-design.md`. Section 2: the "Full CSS" line now names the child and descendant chains of depth 2. Section 5: `Name` and `States` mark the subtree. Section 7: the model of "The Public API" (replace the sketch of `Selector`), the specificity tiers and their table, the match rules with the bound of the descendant walk, the dirty rule for a class, a state, and a name change, and the design decisions.
- [ ] 4.6 Edit the remarks of `src/BrutalUi.Core/Style/StylePass.cs`: the match sentence names the compound parts and the chain. The code does not change.
- [ ] 4.7 Edit `CHANGELOG.md`: one line under `### Added` (`CompoundSelector`, `SelectorCombinator`, `Selector.For`, `Child`, `Descendant`, `ForName`, and the pseudo-state, name, and chain match) and one line under `### Changed` (the specificity numbers, the subtree mark on a class or state change, and the mark on a name change). Name the feature `style-system/selector-matching`.
- [ ] 4.8 Edit `docs/features/style-system/human-testing.md`: add a `## selector-matching` section with the 4 phase demos and the Windows run of `.\scripts\test-windows.ps1`.
- [ ] 4.9 Run `python3 /home/aaronstatic/.claude/skills/asd-ste100/scripts/ste-lint.py --fail-over 2.5` on each changed doc and on this plan, and fix each hit. Write the hand-offs of Notes into `context.md`. Add a "Native Dependencies" note: none. Run the build and the tests.

**Estimated Steps:** about 50 (9 tasks, about five to six steps for one task)

**Files (7):** Group A: `ThousandElementTree.cs`, `ThousandElementAllocationTests.cs` (and a temporary edit of `Selector.cs`, restored). Group B: `docs/technical-design.md`, `StylePass.cs`, `CHANGELOG.md`, `docs/features/style-system/human-testing.md` (and `context.md`).

**Acceptance Criteria:**
- [ ] The 10 tests of `ThousandElementAllocationTests` pass in 5 runs, and both gates measure 0 bytes.
- [ ] The planted allocation gives a non-zero exit code, and the restored file is the same as the copy.
- [ ] The ImGui allocation gates and the benchmarks build and pass with the 16-rule sheet.
- [ ] The lint gives no hit above 2.5 for each changed doc.

**Total estimate:** about 190 steps over 4 phases and 31 tasks.

---

## Key Technical Decisions

### Confirmed Assumptions

- A2 A class or pseudo-state change marks the element and its subtree style-dirty (`DescendantStyle`), with no check of the sheet.
- A8 Only code sets `Element.States` in this epic.
- A9 Theme variables resolve at parse time, for each sheet. The selector depth is a constant of 2.

The epic interview confirmed these items (`epic-overview.md`). The decisions below cite them.

### Decision 1: Two fixed compound slots inside `Selector` (the chosen approach)
**Choice:** `Selector` holds one subject `CompoundSelector`, one ancestor `CompoundSelector`, and a `SelectorCombinator`. The whole match runs in `Selector.Matches(Element, ElementTypeId)`. `StylePass` does not change.
**Rationale:** A9 fixes the depth at 2, so two slots hold each selector of the grammar with no list and no allocation. The call seam of D9.6 and D9.7 stays: the pass reads `TypeId` one time and calls `Matches` for each rule. `IsEmpty` (D1.6) and `Specificity` (one place of the weights) keep their roles, so the builder and both sort keys do not change. The match reads the parent links of the tree, which exist already.
**Alternatives:** (a) A list of compounds in each selector: a variable depth against A9, and a heap array for each selector. (b) A matcher object or a delegate for each rule: an allocation at load and a virtual call for each rule and element. (c) A cache of the ancestor matches for each element: memory in each element and a second invalidation path.

### Decision 2: `CompoundSelector` is a public value with `init` parts, and the factories check it
**Choice:** A `readonly struct` with four `init` properties. `Selector.For`, `Child`, and `Descendant` take compounds and check them. The shortcuts `ForClass`, `ForType`, `ForTypeAndClass`, and `ForName` stay or join.
**Rationale:** style-parser lives outside the assembly and needs one public way to build each form. An object initializer names each part at the call site, as the `StyleProperties` initializers do. The checks run at load time only. The existing shortcuts keep each call site of stylesheet-model, of the tests, and of the shared tree.
**Alternatives:** (a) A fluent builder (`Selector.ForClass(a).WithState(Hover).Under(...)`): more types and a chain of copies for the same result. (b) One factory with optional parameters: an unreadable call for a chain with two compounds.

### Decision 3: One byte for each specificity tier
**Choice:** `Specificity = (names << 16) | ((classes + states) << 8) | types`, summed over both compounds. A pseudo-state counts in the class tier, as in CSS. See open scope question 1.
**Rationale:** The decimal sum of A5 keeps the CSS order only while each tier count stays below 10, and one selector of this feature holds 12 units of the class tier (CR-3). A byte keeps the order up to 255 for each tier, so a later grammar change (for example `.a.b`) needs no new weights. The value stays an `int`, so `StyleRule` and both sort keys do not change.
**Alternatives:** (a) Keep the decimal weights for class and type, and give the name the weight 1000: the numbers 1, 10, and 11 stay, but the proof depends on the bound of the grammar. (b) Cap each count at 9: the decimal numbers stay, but two selectors above the cap tie.

### Decision 4: The descendant walk stops at the grandparent
**Choice:** `.a .b` matches when the parent or the grandparent matches `.a`. See open scope question 2.
**Rationale:** The requirement states that the match walks at most two ancestors. The cost of each rule then stays constant, also on a deep tree. The full subtree mark of A2 covers each element that the walk can reach.
**Alternatives:** A walk to the root, as in CSS: each descendant rule then costs the depth of the element for each restyled element.

### Decision 5: The name match is an ordinal string compare
**Choice:** `CompoundSelector.Name` is a `string?`. The match uses `string.Equals(element.Name, Name, StringComparison.Ordinal)`. See open scope question 3.
**Rationale:** `Element.Name` is a public `string?` already, and no code reads it for a match. The compare allocates nothing and checks the reference first. A name rule is rare in a sheet, and the compare runs last in the compound, after the cheaper tests.
**Alternatives:** An intern table (`ElementName.Intern`, in the form of `ClassName`) and an id field on `Element`: an `int` compare in the match, but a lock and a dictionary lookup in the setter, a first-use allocation, and a new type.

### Decision 6: The setters call `MarkSubtreeStyle`
**Choice:** `ClassList.Add`, `ClassList.Remove`, the `States` setter, and the `Name` setter call `MarkSubtreeStyle()` on a change, in place of `MarkDirty(DirtyFlags.Style)`.
**Rationale:** A2 asks for the subtree mark with no check of the sheet, and the requirements add the name. The pass enters only elements with a style bit, so a chain rule on a descendant needs the mark. `MarkSubtreeStyle` exists, keeps the invariant (D1.12), and allocates nothing. A leaf gets `Style` only, so the leaf tests keep their expectations.
**Alternatives:** (a) A mark of the children and the grandchildren only, which matches the walk bound: less work, but against A2. (b) A check of the sheet before the mark: out of scope (a later optimization).

### Decision 7: The allocation gate extends the shared 1000-element tree
**Choice:** Four rules join the 12-rule sheet of `ThousandElementTree`, and the existing style step gets the state change.
**Rationale:** The epic names `ThousandElementAllocationTests` as the zero-byte check, and phase 11 of stylesheet-model put the sheet there. One new test proves that the rules match. The values change no rect and no draw call (D11.5), so the ImGui gates and the benchmarks keep their numbers.
**Alternatives:** A new tree for the gate: no risk for the shared tree, but a second large fixture and no proof on the real mix.

---

## Risks & Mitigation

| # | Risk | Impact | Mitigation |
|---|------|--------|------------|
| 1 | The subtree mark changes an exact `Dirty` assert of an existing test. | A tree test fails. | Task 2.7 changes the expectation lines only and records each one. The plan search found leaf asserts only. |
| 2 | A hover on a high ancestor restyles a large subtree (A2). | One state change costs a full restyle of the subtree on the next `Update`. | A2 accepts the cost. The remarks of the setters state it. The sheet check is a later optimization (out of scope). The 1000-element gate records the time of the test run. |
| 3 | The specificity numbers change for each existing rule. | A test or a game that reads the number breaks. | Task 1.5 changes the 2 test files. The order of each existing pair does not change. `CHANGELOG.md` names the change (task 4.7). |
| 4 | The walk bound surprises a CSS author (`.hud .label` misses a label three levels down). | A rule does not apply, with no error. | `ADescendantRuleStopsAtTheGrandparent` fixes the rule. Section 7 of the design doc states it. The style-parser hand-off asks for a note in the format doc. Open scope question 2 lets the user choose. |
| 5 | The match makes a defensive copy or allocates (a culture compare, `HasFlag`, a boxed enum). | The gate fails, or the pass slows down. | Both structs are `readonly`. The grep of task 1.7 finds `HasFlag` and reflection. The plant of task 4.3 proves that the gate sees an allocation in `Matches`. |
| 6 | The four new rules of the shared tree change a rect or a draw count of a linked project. | An ImGui test or a benchmark changes its numbers. | Only values that change no rect and no draw call (D11.5). The tree uses the public API only. Acceptance criterion of phase 4. |
| 7 | The ancestor test reads `TypeId` for each rule. | One virtual call for each chain rule and element. | `CompoundSelector.Matches(Element)` reads `TypeId` only for a type part. |
| 8 | The `Name` setter runs in each object initializer of the tests and the trees. | Extra marks at build time. | A new element holds `Style` already and has no child at the set, so the mark costs one flag write. Task 3.5 checks the existing tests. |

---

## Dependencies

**Internal (done):**
- stylesheet-model: `Selector`, `StyleRule`, `StyleSheet.Builder`, `StylePass`, `Element.MarkSubtreeStyle`, `UiContext.AddSheet` and `RemoveSheet`, the attach mark of CR-1, `ElementTypeId`, and the style step of `ThousandElementAllocationTests`.
- core-tree: `Element.Parent`, `Element.States`, `Element.Name`, `ClassList`, `ClassSet`, `PseudoStates`, `DirtyFlags`, and `AllocationGate`.

**External (pinned, no change):**
- `Ahwoo.BRUTAL.Numerics`, `Ahwoo.BRUTAL.Localization`, `Ahwoo.BRUTAL.ImGui` (the ImGui tests only), and `xunit.v3`. No new package and no native dependency.

**Downstream:**
- style-parser maps each selector form of the format to `CompoundSelector` and the factories.
- transitions start a tween when a state change restyles an element of the subtree.
- input-routing sets `Element.States` from events. Each set marks the subtree.

---

## Definition of Done

### Functional
- [ ] F1. A compound matches only when each part matches: the type, the class, the name (ordinal), and each pseudo-state. Extra states on the element do not stop the match.
- [ ] F2. Each of `:hover`, `:active`, `:focus`, `:disabled`, and `:checked` matches its own bit of `Element.States` and no other bit.
- [ ] F3. `.a > .b` matches when the parent matches `.a`. `.a .b` matches when the parent or the grandparent matches `.a`, and it misses at the great-grandparent. A root matches no chain.
- [ ] F4. The specificity is `(names << 16) | ((classes + states) << 8) | types` over both compounds. One name beats 12 units of the class tier, and one class beats 2 types, in each order of add. A later rule wins at equal specificity.
- [ ] F5. A class change, a state change, and a name change mark the element and each descendant `Style`, each element of the subtree with a child `DescendantStyle`, and each ancestor `DescendantStyle`. A change to the same value marks nothing.
- [ ] F6. `Selector.For`, `Child`, `Descendant`, and `ForName` throw `ArgumentException` for an empty compound, an unknown state bit, and an empty name. `default(Selector)` matches nothing, and the builder rejects it.
- [ ] F7. `ForClass`, `ForType`, and `ForTypeAndClass` keep their behavior, with the specificities 256, 1, and 257.

### Quality
- [ ] Q1. `dotnet build brutal-ui.sln 2>&1 | tail -n 30` prints `0 Warning(s)` and `0 Error(s)`.
- [ ] Q2. `dotnet test --solution brutal-ui.sln 2>&1 | tail -n 30` prints 0 failed. The total is the total of task 1.1 plus the new tests.
- [ ] Q3. `ThousandElementAllocationTests` (10 tests), `TheStylePassAllocatesNothingAfterWarmUp`, and `AChainMatchAndAStateChangeAllocateNothing` pass with 0 bytes. The plant of task 4.3 gives a non-zero exit code.
- [ ] Q4. No expected rect changed: `git diff -U0 <base commit> -- tests | grep -c '^[-+].*AssertRect'` prints `0`.
- [ ] Q5. Each public member of the new and the changed types has an XML doc comment (the build enforces it).
- [ ] Q6. `ste-lint.py --fail-over 2.5` exits with 0 for each changed doc.

### Integration
- [ ] I1. `git diff --quiet <base commit> -- Directory.Packages.props src/BrutalUi.ImGui src/BrutalUi.Markup '*.csproj'; echo "exit=$?"` prints `exit=0`.
- [ ] I2. `grep -nE 'System\.Reflection|GetType\(\)|typeof\(|HasFlag' src/BrutalUi.Core/Style/*.cs` prints nothing.
- [ ] I3. The code of `StylePass.cs` does not change, only its doc comments: `git diff -U0 <base commit> -- src/BrutalUi.Core/Style/StylePass.cs | grep '^[-+]' | grep -v '^[-+][-+]' | grep -vE '^[-+]\s*///'` prints nothing.
- [ ] I4. `grep -rnE '\.Style\.[A-Za-z]+(\.[A-Za-z]+)?\s*=[^=]' src samples tests --include=*.cs` prints nothing.
- [ ] I5. `scripts/test-linux.sh; echo "exit=$?"` prints the PASS line and `exit=0`.

### Verification Method
1. Run Q1 and Q2.
2. Run the demo command of each phase and compare the printed lines with the Demo line.
3. Run `dotnet run --project tests/BrutalUi.Core.Tests/BrutalUi.Core.Tests.csproj -- --filter-class '*ThousandElementAllocationTests'` and expect a total of 10 with `failed: 0`. Run `dotnet run --project tests/BrutalUi.Core.Tests/BrutalUi.Core.Tests.csproj -- --filter-class '*SubtreeMarkTests'` and expect `failed: 0`.
4. Run Q4, I1, I2, I3, I4, and I5 with the base commit of task 1.1.
5. Read `context.md` for the exit codes of task 4.3, the facts lines of tasks 1.1 and 4.4, and the changed lines of tasks 1.5 and 2.7.

---

## Testing Strategy

- **Match unit tests (`PseudoStateSelectorTests`, `ChainSelectorTests`, `NameSelectorTests`):** a small tree in a context and a sheet in code. Each part and each chain form has a match and a miss. The values are paint members that do not inherit, so a result comes from the match and not from the parent.
- **Specificity tests:** the numbers of the tier table, and the order of each pair in both orders of add (two sheets or two builders).
- **Dirty tests (`SubtreeMarkTests`):** a tree of depth 4 with a second branch. A test reads `element.Dirty` after the change and after `UpdateStyles()`, before the layout pass clears the bits. One test checks the invariant on each element.
- **Regression:** `DirtyTrackingTests` (the seeded random calls), `ClassListTests`, `ElementStateTests`, `TypeSelectorTests`, `MultiSheetTests`, and each layout fixture pass. Task 2.7 records each changed expectation.
- **Allocation gates:** `AChainMatchAndAStateChangeAllocateNothing` (phase 2, small tree) and the style step of `ThousandElementAllocationTests` (phase 4). The plant in `Selector.Matches` proves the gate.
- **Manual:** the smoke run of HelloPanel through `scripts/test-linux.sh`. The sample sets no state in this feature. style-parser moves the sample to a sheet.

---

## Related Documentation

- `docs/features/style-system/selector-matching/requirements.md`
- `docs/features/style-system/epic-overview.md` and `epic-requirements.md`
- `docs/features/style-system/stylesheet-model/implementation.md`, `context.md` (D1.6, D1.8, D1.11, D1.12, D9.6 to D9.8, D11.4, D11.5, CR-1, CR-3, and `## Hand-offs`), and `code-review.md`
- `docs/features/style-system/style-parser/requirements.md` and `transitions/requirements.md`
- `docs/technical-design.md` sections 2, 4, 5, 7, and 14
- `.beast-mode/phase-handoff.md` section 11 (the slice rules)

---

## Notes

### Open Scope Questions

No interview ran for this draft. The plan uses the recommended option of each question.

**Question 1: Which specificity weights replace the decimal sum (CR-3)?**
- **Recommended:** one byte for each tier: `(names << 16) | ((classes + states) << 8) | types`. Reason: the CSS order holds for each count below 256, with no proof that depends on the grammar.
- Option B: keep class 10 and type 1 (A5 of stylesheet-model), and give the name 1000. Reason: the numbers 1, 10, and 11 stay, and the bound of the grammar (12 class units, 2 types) keeps the order today, but a later `.a.b` can break it.
- Option C: cap each tier count at 9 and keep the decimal weights. Reason: the smallest change, but two selectors above the cap tie, and the order then falls back to the order of add.

**Question 2: How far does a descendant chain (`.a .b`) walk?**
- **Recommended:** the parent and the grandparent only. Reason: the requirement says "walks at most two ancestors", and the cost of each rule stays constant.
- Option B: each ancestor up to the root, as in CSS. Reason: a CSS author gets the expected result at each depth, but each descendant rule then costs the depth of the element. The subtree mark already covers each depth.

**Question 3: How does a name rule compare the name?**
- **Recommended:** an ordinal string compare against `Element.Name`. Reason: no new type and no lock in the setter, and the compare allocates nothing.
- Option B: an intern table and a name id on `Element`, in the form of `ClassName`. Reason: an `int` compare in the match, but a lock and a dictionary lookup in each name set, and a new public type.

**Question 4: Which demo does phase 1 use (slice rule 12)?**
- **Recommended:** `**Demo:** the developer runs dotnet run --project tests/BrutalUi.Core.Tests/BrutalUi.Core.Tests.csproj -- --filter-method '*AHoverRuleRestylesTheButton' --show-live-output on and reads button #1E222CFF, then button hover #3060C0FF after States = Hover, then button #1E222CFF after States = None`. Reason: the smallest path that proves the state match, and it shows the change and the way back.
- Option B: `**Demo:** the developer runs the filter '*ADisabledRuleDimsTheButton' and reads opacity 1, then opacity 0.5 after States = Disabled`. Reason: the same path, but an opacity line is less clear than a color.
- Option C: `**Demo:** the developer runs the filter '*AHoverOnAPanelRestylesItsRowAndItsCell' and reads the row and cell lines`. Reason: a stronger demo, but phase 1 then holds the chain and the subtree mark, which is too large for a first slice.

### Conflicts

1. **The technical design and the descendant chain.** Section 2 of `docs/technical-design.md` says "No descendant selectors in the first release". The requirements and A9 ask for `.a .b`. The plan follows the requirements, and task 4.5 changes the line.
2. **A5 of stylesheet-model and the byte tiers.** A5 ("class = 10, type = 1") is not in the Confirmed Assumptions of this feature. The CR-3 hand-off of stylesheet-model asks this feature to replace the weights. The plan uses the byte tiers (question 1) and records the change in `CHANGELOG.md`.
3. **The walk bound and CSS.** The requirement "walks at most two ancestors" gives `.a .b` a meaning that differs from CSS (question 2). The plan keeps the requirement and documents the rule.
4. **A2 and the walk bound.** A2 marks the full subtree, but with the walk bound only the children and the grandchildren can change their match. The plan keeps A2. The deeper marks cost extra restyles with no change of the result (Risk 2).
5. **The doc of `Element.Name`.** The doc says "A change of the name sets no dirty bit". The requirements ask for the subtree mark. Task 3.1 changes the setter and the doc.
6. **The epic requirements and the member name.** `epic-requirements.md` says the match reads `Element.PseudoStates`. The member is `Element.States` of the type `PseudoStates`. The plan reads `Element.States`, as the feature requirements say.

### Gaps (not in this feature)

1. One class for each compound. `.a.b` is not in the requirements. style-parser rejects it with a positioned error, or a later feature adds a second class slot (the byte tiers already hold the count).
2. No universal selector (`*`). A compound with only states acts as `*:hover`.
3. No rule index. A7 of stylesheet-model waits for a benchmark. Each restyled element still scans each rule.
4. No check of the sheet before a subtree mark (out of scope), and no mark limited to two levels (Decision 6).
5. `ElementBuilder` gets no `Name` helper. The object initializer `new Element { Name = "title" }` covers it.

### Hand-offs (for `context.md` at the end of phase 4)

**style-parser**
- Map each compound of the format to `CompoundSelector`: a type name through `ElementType.Intern`, `.class` through `ClassName.Intern`, `#name` to `Name`, and each of `:hover`, `:active`, `:focus`, `:disabled`, and `:checked` to a bit of `States`. Map `>` to `Selector.Child` and whitespace to `Selector.Descendant`.
- Reject with a positioned error: a chain of more than two compounds, a second class in one compound, an unknown pseudo-state, a sibling or attribute selector, and `:not`. The factories throw `ArgumentException` for an empty compound and an empty name, so check first and report the position.
- A selector list (`.a, .b`) gives one rule for each selector. `Selector.Specificity` computes the value, so the parser computes nothing.
- The format doc states the walk bound of `.a .b` (question 2).

**transitions**
- A state change restyles the full subtree on the next `Update`. A tween starts on each element whose value changes, also below the changed element.
- `StylePass` does not change in this feature, so the start point of the stylesheet-model hand-off holds.
