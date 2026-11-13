# markup/builder-api - Task Checklist

**Last Updated:** 2026-11-12 10:38
**Progress:** 33/33 tasks complete (100%)
**Agent tiers:** every phase is STANDARD (sonnet). No phase needs an advanced agent.

"The plan" means `implementation.md` of this feature. Its sections "Architecture Overview" (Public API, The Section 11 Example, The Query, The Delegate Method Names, The Tree Compare) and "Key Technical Decisions" hold the rules that the tasks name. Each task's full text sits in the plan; the phase brief carries it word for word.

Shared rules for every group. Each group ends with `MSBUILDDISABLENODEREUSE=1 dotnet build brutal-ui.sln -nodeReuse:false 2>&1 | tail -n 30` and `MSBUILDDISABLENODEREUSE=1 dotnet test --solution brutal-ui.sln 2>&1 | tail -n 30` (tool rule 5). Expect `0 Warning(s)`, `0 Error(s)`, and 0 failed. A warning fails the build. Run one class after a build with `tests/BrutalUi.Core.Tests/bin/Debug/net10.0/BrutalUi.Core.Tests --filter-class '*<Class>' 2>&1 | tail -n 5`, or the same path under `BrutalUi.Framework.Tests`. The projects have no implicit usings, so each new file holds its own `using` lines. Use file-scoped namespaces. Prose in comments follows ASD-STE100; each public member has an XML doc comment (CS1591 is an error). Plants: copy the file to `src/BrutalUi.Core/obj/plantscratch/`, change it, run the solution tests to a log, check `0 Error(s)` and a non-zero exit, restore with `cp`, check with `cmp`. A test host can exit 134 one time; run a second time before you treat it as a fault.

**No-change rule:** `src/BrutalUi.Markup/`, `src/BrutalUi.Glfw/`, `src/BrutalUi.ImGui/`, `src/BrutalUi.Debugger/`, `samples/`, `src/BrutalUi.Framework/Panels/SlotContainer.cs`, each widget file under `src/BrutalUi.Core/Elements/`, `Directory.Packages.props`, `brutal-ui.sln`, and `tests/BrutalUi.Markup.Tests/BrutalUi.Markup.Tests.csproj` do not change. `Class`, `Class`, `Child`, and `BindCommand` keep their exact text.

---

## Phase 1: The Section 11 Example (11/11 complete)

**Demo:** the developer runs the tests, and the test of the section 11 example passes: `BrutalUi.Framework.Tests --filter-class '*CommandBuilderTests'` reads a total of 5 with `failed: 0`, and `TheSectionElevenExampleBuildsTheSameTreeAsInitializers` is one of them.
**Needs:** none
**Agent tier:** STANDARD (sonnet)

Two groups. Group 1: tasks 1.1 to 1.6 (6 files). Group 2: tasks 1.7 to 1.11 (6 files).

- [x] **1.1 Create context.md: base commit, baseline counts, the recorded plan sections**
  - Description: Create `docs/features/markup/builder-api/context.md` when `/start-feature` did not. Run `git status --porcelain`. Record the base commit, the solution total with the skipped count (2988 with 3 skipped at planning time), and the Core (1801) and Framework (326) totals. Record the Public API, The Query, The Delegate Method Names, The Tree Compare, and the Notes of the plan.
  - File(s): `docs/features/markup/builder-api/context.md`
  - Estimate: 4 steps

- [x] **1.2 ElementBuilder.cs: partial, the class remarks, `Name<T>`**
  - Description: Per plan task 1.2. `public static partial class ElementBuilder`; the three replacement remark sentences; add `Name<T>(this T element, string? name)` with the null check and the doc rules of A5. Keep `Class`, `Class`, and `Child` word for word.
  - File(s): `src/BrutalUi.Core/Tree/ElementBuilder.cs`
  - Estimate: 6 steps

- [x] **1.3 Write ElementBuilder.Text.cs (the six Label and Button overloads)**
  - Description: Per plan task 1.3. `public static partial class ElementBuilder`; the six `Text` overloads of `Label` and `Button`; each checks the receiver, calls the matching `SetText`, returns the receiver; the doc rules (the `BRUTALCORE0011` note, the cast for a bare `null`, `null` gives an empty text).
  - File(s): `src/BrutalUi.Core/Tree/ElementBuilder.Text.cs`
  - Estimate: 6 steps

- [x] **1.4 CommandBindings.cs: `Command`**
  - Description: Per plan task 1.4. Add `Command(this Button, CommandRegistry, ReadOnlySpan<byte>)` as one call to `BindCommand`; copy the three exception lines; change the class summary to "The bind methods"; keep `BindCommand` word for word.
  - File(s): `src/BrutalUi.Framework/Commands/CommandBindings.cs`
  - Estimate: 4 steps

- [x] **1.5 Write Shared/TreeCompare.cs**
  - Description: Per plan task 1.5 and "The Tree Compare". `internal static class TreeCompare` in `BrutalUi.Core.Tests`, one method `FirstDifference(Element, Element)`; the element list, the widget list, the path message, the delegate-count rule, the not-compared list, no test framework type.
  - File(s): `tests/BrutalUi.Core.Tests/Shared/TreeCompare.cs`
  - Estimate: 8 steps

- [x] **1.6 Framework tests csproj: the TreeCompare link**
  - Description: Per plan task 1.6. Add `<Compile Include="../BrutalUi.Core.Tests/Shared/TreeCompare.cs" LinkBase="Shared" />` after the `TextMeasure.cs` line. Build, and expect `0 Error(s)`.
  - File(s): `tests/BrutalUi.Framework.Tests/BrutalUi.Framework.Tests.csproj`
  - Estimate: 3 steps

- [x] **1.7 Write TreeCompareTests.cs with 5 tests**
  - Description: Per plan task 1.7: `TwoEqualTreesGiveNoDifference`, `ANameOrAClassDifferenceGivesThePath`, `ATypeOrAChildCountDifferenceGivesThePath`, `AWidgetValueDifferenceGivesThePath`, `ADelegateCountDifferenceGivesThePath`.
  - File(s): `tests/BrutalUi.Core.Tests/Builder/TreeCompareTests.cs`
  - Estimate: 8 steps

- [x] **1.8 Write ElementBuilderElementTests.cs with 3 tests**
  - Description: Per plan task 1.8: the name round trip with the literal of risk 1, the null-clears and same-name-marks-nothing test, and `TheElementMethodsThrowForANullElement`.
  - File(s): `tests/BrutalUi.Core.Tests/Builder/ElementBuilderElementTests.cs`
  - Estimate: 6 steps

- [x] **1.9 Write ElementBuilderTextTests.cs with 4 tests**
  - Description: Per plan task 1.9: the Label and Button byte overloads, `TextReturnsTheSameElementWithItsType`, and `TheTextMethodsThrowForANullElement` for the six overloads.
  - File(s): `tests/BrutalUi.Core.Tests/Builder/ElementBuilderTextTests.cs`
  - Estimate: 7 steps

- [x] **1.10 Write CommandBuilderTests.cs with 5 tests (Framework)**
  - Description: Per plan task 1.10: `CommandInvokesTheCommandOnAClick`, `CommandKeepsAHandlerThatTheGameSetBefore`, `CommandChecksTheButtonTheRegistryAndTheName`, `CommandReturnsTheSameButton`, and `TheSectionElevenExampleBuildsTheSameTreeAsInitializers`.
  - File(s): `tests/BrutalUi.Framework.Tests/Commands/CommandBuilderTests.cs`
  - Estimate: 9 steps

- [x] **1.11 Update technical-design.md, run build/tests/demo, plants 1 and 2, record**
  - Description: Per plan task 1.11. Section 11 code block plus the `registry` and `u8` sentences; line 301 of section 5 gains `Name(string?)`. Expect Core +12 and Framework +5. Plants 1 and 2. Record counts, plants, and the demo state in `context.md`.
  - File(s): `docs/technical-design.md`, `docs/features/markup/builder-api/context.md`
  - Estimate: 8 steps

---

## Phase 2: The Other Value Methods (8/8 complete)

**Demo:** the developer runs `BrutalUi.Core.Tests --filter-class '*BuilderTreeTests'` and reads a total of 2 with `failed: 0`. The fluent eight-type tree equals the initializer tree.
**Needs:** Phase 1
**Agent tier:** STANDARD (sonnet)

Two groups. Group 1: tasks 2.1 to 2.6 (6 files). Group 2: tasks 2.7 and 2.8 (3 files).

- [x] **2.1 ElementBuilder.cs: `Pickable<T>` and `Focusable<T>`**
  - Description: Per plan task 2.1. Each with the null check, one write, and the doc of the defaults and the no-dirty-bit rule.
  - File(s): `src/BrutalUi.Core/Tree/ElementBuilder.cs`
  - Estimate: 4 steps

- [x] **2.2 ElementBuilder.Text.cs: the three TextField overloads**
  - Description: Per plan task 2.2, with the capacity-rule doc sentence.
  - File(s): `src/BrutalUi.Core/Tree/ElementBuilder.Text.cs`
  - Estimate: 4 steps

- [x] **2.3 Write ElementBuilder.Widgets.cs (the 8 widget value methods)**
  - Description: Per plan task 2.3: `Checked`, `Value`, `Range`, `Texture`, `Uv`, `Tint`, `ScaleMode`, `ScrollOffset`. No method for `Capacity` or `ScrollSpeed` (A6). The docs of the value rule, the no-delegate rule, the `SetRange` throw rule, and the range-before-value order. File remarks point at phase 4.
  - File(s): `src/BrutalUi.Core/Tree/ElementBuilder.Widgets.cs`
  - Estimate: 8 steps

- [x] **2.4 ElementBuilderElementTests.cs: +1 test and the new null checks**
  - Description: Per plan task 2.4. Add `PickableAndFocusableSetTheFlagsAndReturnTheSameElement`; extend `TheElementMethodsThrowForANullElement`.
  - File(s): `tests/BrutalUi.Core.Tests/Builder/ElementBuilderElementTests.cs`
  - Estimate: 4 steps

- [x] **2.5 ElementBuilderTextTests.cs: +1 test and the new null checks**
  - Description: Per plan task 2.5. Add `TextOfATextFieldSetsTheBytesOfEachOverload`; extend `TheTextMethodsThrowForANullElement`.
  - File(s): `tests/BrutalUi.Core.Tests/Builder/ElementBuilderTextTests.cs`
  - Estimate: 4 steps

- [x] **2.6 Write ElementBuilderWidgetTests.cs with 6 tests**
  - Description: Per plan task 2.6: `CheckedSetsTheStateAndCallsNoDelegate`, `ValueAppliesTheValueRuleAndCallsNoDelegate`, `RangeCallsSetRangeAndThrowsForABadRange`, `TheImageMethodsSetTextureUvTintAndScaleMode`, `ScrollOffsetSetsTheOffset`, `EachWidgetValueMethodThrowsForANullElement`.
  - File(s): `tests/BrutalUi.Core.Tests/Builder/ElementBuilderWidgetTests.cs`
  - Estimate: 8 steps

- [x] **2.7 Write BuilderTreeTests.cs with 2 tests**
  - Description: Per plan task 2.7: `AnEightTypeTreeFromTheFluentMethodsEqualsTheInitializerTree` and `ValueBeforeRangeClampsToTheOldRange`.
  - File(s): `tests/BrutalUi.Core.Tests/Builder/BuilderTreeTests.cs`
  - Estimate: 8 steps

- [x] **2.8 Update technical-design.md, run build/tests/demo, plants 3 and 4, record**
  - Description: Per plan task 2.8. The section 11 method lists (element, text, widget values); line 301 gains `Pickable(bool)` and `Focusable(bool)` plus the pointer sentence. Expect Core +10. Plants 3 and 4. Record counts and plants in `context.md`. Check each `ElementBuilder*.cs` under 300 lines.
  - File(s): `docs/technical-design.md`, `docs/features/markup/builder-api/context.md`
  - Estimate: 8 steps

---

## Phase 3: The Query by Name (6/6 complete)

**Demo:** the developer runs `BrutalUi.Core.Tests --filter-class '*ElementFind*'` and reads a total of 9 with `failed: 0`. The allocation test measures 0 bytes for 1000 rounds of a hit, a miss, and a typed hit.
**Needs:** Phase 1
**Agent tier:** STANDARD (sonnet)

One group: tasks 3.1 to 3.6 (6 files).

- [x] **3.1 Element.cs: the partial modifier and one remarks sentence**
  - Description: Per plan task 3.1. `public partial class Element`, one remarks sentence about `Element.Find.cs`. Change no member.
  - File(s): `src/BrutalUi.Core/Tree/Element.cs`
  - Estimate: 3 steps

- [x] **3.2 Write Element.Find.cs (`Find`, `Find<T>`, the two private walks)**
  - Description: Per plan task 3.2 and "The Query": the empty-name return, the root-first depth-first pre-order index walk, the length fast reject, `Utf8Text.Utf8Equals`, the typed skip. The doc rules of the task. No cref to a Framework type.
  - File(s): `src/BrutalUi.Core/Tree/Element.Find.cs`
  - Estimate: 7 steps

- [x] **3.3 Write ElementFindTests.cs with 7 tests**
  - Description: Per plan task 3.3: `FindChecksTheRootFirst`, `FindWalksInTreeOrderAndGivesTheFirstHit`, `FindComparesOrdinal`, `FindGivesNullForAMissAnEmptyNameAndAnUnnamedTree`, `FindDoesNotLookAboveTheElement`, `FindTGivesTheElementWithItsType`, `FindTSkipsAnElementOfAnotherType`.
  - File(s): `tests/BrutalUi.Core.Tests/Builder/ElementFindTests.cs`
  - Estimate: 8 steps

- [x] **3.4 Write ElementFindAllocationTests.cs with 2 tests**
  - Description: Per plan task 3.4, in the style of `ElementAllocationTests`: the ~200-element three-level tree, `FindAndFindTAllocateNothingAfterWarmUp` with `AllocationGate.ReleaseAllocationContext()` and a 0 difference, and `TheMeasuredFindDoesRealWork`.
  - File(s): `tests/BrutalUi.Core.Tests/Builder/ElementFindAllocationTests.cs`
  - Estimate: 8 steps

- [x] **3.5 Update technical-design.md sections 5, 11, and 14**
  - Description: Per plan task 3.5. `Find` and `Find<T>` in the section 5 `Element` block; the five rules under the section 11 method list; one section 14 bullet for the new gate.
  - File(s): `docs/technical-design.md`
  - Estimate: 5 steps

- [x] **3.6 Run build/tests/demo, the SlotContainer and UiManager check, plants 5 to 7, record**
  - Description: Per plan task 3.6. Expect Core +9. Check each `SlotContainer` and `UiManager` test passes with no edit (risk 3). Plants 5, 6, and 7. Record counts and plants in `context.md`.
  - File(s): `docs/features/markup/builder-api/context.md`
  - Estimate: 8 steps

---

## Phase 4: The Delegate Methods (8/8 complete)

**Demo:** the developer runs `BrutalUi.Core.Tests --filter-class '*ElementBuilderEventTests'` and reads a total of 4 with `failed: 0`. A `PointerDown` handler that the builder adds runs on a routed pointer press.
**Needs:** Phase 1, Phase 2
**Agent tier:** STANDARD (sonnet)

Two groups. Group 1: tasks 4.1 to 4.5 (5 files). Group 2: tasks 4.6 to 4.8 (5 files).

- [x] **4.1 ElementBuilder.Widgets.cs: the 6 widget delegate methods**
  - Description: Per plan task 4.1: `OnClicked`, `OnCheckedChanged`, `OnValueChanged`, `OnTextChanged`, `OnSubmitted`, `OnScrollChanged`, each with the two null checks, `slot += handler`, and the name, `+=`, and build-time docs.
  - File(s): `src/BrutalUi.Core/Tree/ElementBuilder.Widgets.cs`
  - Estimate: 7 steps

- [x] **4.2 Write ElementBuilder.Events.cs (the 14 generic event methods)**
  - Description: Per plan task 4.2. Each `where T : Element`, two null checks, `element.OnX += handler`, `return element`; the name rule, the `+=` rule, the side-object rule, and the slot phase in the docs; file remarks one time; the file stays under 300 lines.
  - File(s): `src/BrutalUi.Core/Tree/ElementBuilder.Events.cs`
  - Estimate: 9 steps

- [x] **4.3 ElementBuilderWidgetTests.cs: +2 tests**
  - Description: Per plan task 4.3: `EachWidgetSlotMethodAddsTheHandlerWithPlusEquals` and `AWidgetSlotMethodThrowsForANullReceiverOrANullHandler`.
  - File(s): `tests/BrutalUi.Core.Tests/Builder/ElementBuilderWidgetTests.cs`
  - Estimate: 6 steps

- [x] **4.4 Write ElementBuilderEventTests.cs with 4 tests**
  - Description: Per plan task 4.4: `EachEventMethodAddsTheHandlerToItsSlot`, `AnEventMethodKeepsAHandlerThatTheGameSetBefore`, `AnEventMethodThrowsForANullElementOrANullHandler`, `ABuilderHandlerRunsOnARoutedEvent` (the `EventWalkTests` pattern).
  - File(s): `tests/BrutalUi.Core.Tests/Builder/ElementBuilderEventTests.cs`
  - Estimate: 8 steps

- [x] **4.5 Run the build and the Core tests, and check the name rule**
  - Description: Per plan task 4.5. Compile checks that `button.OnClicked(h)` and `element.PointerDown(h)` build; run the grep of the task and expect no output; record the result in `context.md`.
  - File(s): `docs/features/markup/builder-api/context.md`
  - Estimate: 5 steps

- [x] **4.6 CommandBuilderTests.cs: +1 test (both orders)**
  - Description: Per plan task 4.6: `CommandAndOnClickedKeepBothDelegatesInEachOrder` (A7).
  - File(s): `tests/BrutalUi.Framework.Tests/Commands/CommandBuilderTests.cs`
  - Estimate: 5 steps

- [x] **4.7 BuilderTreeTests.cs: +1 tree test with handlers**
  - Description: Per plan task 4.7: `ATreeWithHandlersFromTheFluentMethodsEqualsTheInitializerTree` with one handler on each of the 6 widget slots and on 3 event slots.
  - File(s): `tests/BrutalUi.Core.Tests/Builder/BuilderTreeTests.cs`
  - Estimate: 6 steps

- [x] **4.8 Update technical-design.md and CHANGELOG.md, run the full gate, plants 8 and 9, record**
  - Description: Per plan task 4.8. Section 11 gains the 20 delegate methods and the name rule; `CHANGELOG.md` gains one `(markup/builder-api)` line under `## [Unreleased]`. Expect Core +7, Framework +1, and the solution total with 3 skipped. Plants 8 and 9. Run the greps of the Verification Method. Record counts, plants, and greps in `context.md`.
  - File(s): `docs/technical-design.md`, `CHANGELOG.md`, `docs/features/markup/builder-api/context.md`
  - Estimate: 9 steps

---

## Completed This Session (2026-11-12)

- ✅ All 33 tasks, phases 1 to 4, each group verified by its own gate: build `0 Warning(s)` and `0 Error(s)`, solution 3032 tests with 0 failed and 3 skipped (Core 1839, Framework 332).
- ✅ Cross-phase review (`code-review.md`): 6 findings, 6 fixed. CR-1, CR-2, and CR-5 added asserts inside existing tests, so no test count moved; CR-4 reworded the `Find<T>` remarks; CR-3 fixed one design-doc line; CR-6 added one hand-off line to the xml-element-loader requirements. The post-review gate reads PASS on all 15 items.
- ✅ Docs: `implementation.md` reads `Ready for Review`; `context.md` holds the baseline, the nine plants, the gate record, and the four demo lines under Needs Human Verification.

## Bugs & Issues

**Active Bugs:**
- (none)

**Fixed Bugs:**
- (none)

---

## Technical Debt

- (none)

---

## Notes

- This feature adds no UI and no window. No browser check applies; every demo is a test command. The plan states "No manual check" in its Testing Strategy.
- Notes 3 and 4 of the plan are hand-offs to xml-element-loader (the `TreeCompare` link line, the `Range` before `Value` order); note 5 is the hand-off to binding-attributes.
- Planned totals at the end: Core 1839, Framework 332, solution 3032 with 3 skipped, counted from the baseline that task 1.1 records. The final gate reads exactly these: Core 1839, Framework 332, solution 3032, 0 failed, 3 skipped.

---

*Update this file as tasks are completed. Mark tasks with ✅ immediately when done.*
