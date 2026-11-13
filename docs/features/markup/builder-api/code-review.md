# markup/builder-api - Code Review

**Reviewed:** 2026-11-12
**Reviewer:** Senior C# engine reviewer (cross-phase review of phases 1 to 4)
**Rules Applied:** 1. The 1 to 1 map (each value method writes one property or calls one setter, and nothing exists for `States`, `Capacity`, `ScrollSpeed`, `Inline`, `Layout`), 2. The shipped surface (`Class<T>(string)`, `Class<T>(ClassId)`, `Child<T>(Element)`, `BindCommand` text unchanged, `SlotContainer.Find` unchanged, no new public type), 3. The delegate name rule (each of the 20 names toggles the `On` prefix, no method shadows an invocable property), 4. The `+=` rule and the order (a handler set before stays, two calls add two handlers in call order, `Command` against `OnClicked` in both orders), 5. The query (root first, depth-first pre-order, first hit wins, ordinal UTF-8 compare, empty name gives `null`, no name never matches, no look above, 0 bytes, `Utf8Text.Utf8Equals` is the only compare), 6. The null rules (null receiver throws, null handler throws, `Name(null)` clears, `Name("")` sets, `Text` null gives empty), 7. The tree compare (the compared member list, the count-not-identity delegate rule, the not-compared list, no test framework type), 8. The 0-byte rule and the gate (1000 rounds of a hit, a miss, and a typed hit, with a real-work test), 9. Integration with the epic consumers (the `TreeCompare` link line, the `Range` before `Value` order, `command` over `Command` and `BindCommand`, `Find` in panel code, surfaces that force a change later), 10. API clarity and docs (XML docs in ASD-STE100 prose, sections 5, 11, and 14 match the code, the changelog names the feature once), 11. Test strength (a rule pinned by a test AND a plant, the nine plants, read-backs, same-instance asserts, null checks), 12. Small files and the builder's own allocation (each builder file under 300 lines, one build-time side object per delegate). The Vercel React rules do not apply to C#.
**Status:** Resolved - 6 findings, 6 resolved (2026-11-12, five fix groups: CR-1, CR-2, and CR-5 added test asserts inside existing tests; CR-4 reworded the `Find<T>` remarks; CR-3 and CR-6 changed doc lines. No production behavior changed, and no test count moved.)

---

## Summary

| Severity | Count | Findings |
|----------|-------|----------|
| CRITICAL | 0 | - |
| HIGH | 0 | - |
| MEDIUM | 0 | - |
| LOW-MEDIUM | 1 | CR-1 |
| LOW | 5 | CR-2, CR-3, CR-4, CR-5, CR-6 |

The feature does what the plan asks. The 43 builder methods, `Command`, `Find`, and `Find<T>` match the Public API of the plan name for name and body for body: every value method is null-check plus one property write or one `SetRange` call plus return, every delegate method is two null checks plus one `slot += handler` plus return, and no method exists for `States`, `Capacity`, `ScrollSpeed`, `Inline`, or `Layout`. The shipped surface is byte-identical: the diff of `ElementBuilder.cs` removes only the old class line and the old remarks line, and the diff of `CommandBindings.cs` removes only the old class summary line. The delegate name rule holds for all 20 names in both directions, and the task 4.5 grep prints nothing. The query implements the plan's pseudo-code exactly, with the length fast reject and `Utf8Text.Utf8Equals` as the only compare, and its gate measures 0 bytes over 1000 rounds of a hit, a miss, and a typed hit with a real-work guard. `TreeCompare` covers the planned member list, compares delegate counts and not identity, states the not-compared list, and names no test framework type. All nine plants failed exactly the named tests with `0 error CS`, and the final solution reads 3032 with 0 failed and 3 skipped, which I measured myself. Rules 1 to 9 have no finding; the six findings are one unpinned documented rule (CR-1) and five small test, doc, or hand-off gaps.

### Baseline (observed, not copied from context.md)

- `export DOTNET_CLI_HOME=/tmp/dui-cli-home` set first; the build and test runs used it.
- `MSBUILDDISABLENODEREUSE=1 dotnet build brutal-ui.sln -nodeReuse:false`: `Build succeeded.`, `0 Warning(s)`, `0 Error(s)` (10.9s).
- `MSBUILDDISABLENODEREUSE=1 dotnet test --solution brutal-ui.sln`: `total: 3032`, `failed: 0`, `succeeded: 3029`, `skipped: 3` (the three GLFW display tests), duration 5.3s. No `error CS` line, no exit-134 host death; one run was enough.
- `git diff --stat 386793e`: six changed files, +101/−8: `CHANGELOG.md` +1, `docs/technical-design.md` +22/−4, `Element.cs` +6, `ElementBuilder.cs` +61, `CommandBindings.cs` +18, `BrutalUi.Framework.Tests.csproj` +1. `git status --porcelain`: those six ` M` files plus the untracked `Element.Find.cs`, the three new `ElementBuilder.*` files, `tests/BrutalUi.Core.Tests/Builder/`, `Shared/TreeCompare.cs`, `CommandBuilderTests.cs`, and the four feature docs. Branch `feat/markup/builder-api` at `386793e`; the whole feature is uncommitted working-tree work.
- `grep -c 'public static' src/BrutalUi.Core/Tree/ElementBuilder*.cs`: 7 + 15 + 10 + 15 = 47, that is 43 methods plus the four `public static partial class ElementBuilder` lines. `grep -c 'public static.*\bthis\b'` confirms 6 + 14 + 9 + 14 = 43.
- `wc -l`: `ElementBuilder.cs` 115, `ElementBuilder.Text.cs` 143, `ElementBuilder.Widgets.cs` 248, `ElementBuilder.Events.cs` 287, `Element.Find.cs` 139, `TreeCompare.cs` 319. Each builder file is under 300.
- Verification Method item 5: the two readings above (43 methods, each file under 300).
- Item 6: `git diff 386793e -- src/BrutalUi.Core/Tree/ElementBuilder.cs | grep '^-[^-]'` prints exactly two lines, the old remarks line and the old `public static class ElementBuilder` line.
- Item 7: `git diff --stat 386793e -- src/BrutalUi.Markup src/BrutalUi.Core/Elements src/BrutalUi.Framework/Panels samples` prints no output.
- Item 8: `git diff 386793e -- src | grep -E '^\+.*public ... class|struct|interface|enum|record|delegate '` prints only `+public partial class Element` and `+public static partial class ElementBuilder`. No new public type.
- Item 9 (the task 4.5 grep over the four builder files): no output. No delegate method carries its slot name.
- `CHANGELOG.md`: exactly one `markup/builder-api` line (53), under `## [Unreleased]` → `### Added`.

---

## Findings

### CR-1: The `Text` null-gives-empty-text rule has no test and no plant

**Severity:** LOW-MEDIUM
**Rule:** 6 (The null rules) and 11 (Test strength)
**File:** `tests/BrutalUi.Core.Tests/Builder/ElementBuilderTextTests.cs:99-116`; the rule sits in the doc at `src/BrutalUi.Core/Tree/ElementBuilder.Text.cs:17`

**Problem:** Decision 5 of the plan states, and the file remarks repeat, that `Text` with a `null` `string` or `null` `LString` gives an empty text. No test runs that rule at the method level. The only test that passes a null text argument is the null-receiver test, and the receiver throw ends the call before the rule can run:

```csharp
Assert.Equal("label", Assert.Throws<ArgumentNullException>(() => nullLabel.Text((string?)null)).ParamName);
```

The shipped `Label.SetText(string?)` behavior covers the property, so the map holds today (`SetText(null)` writes the empty text, `src/BrutalUi.Core/Elements/Label.cs:149`). But a method-level regression that turns a null text into a throw, or into a clear of the wrong member, keeps all 3032 tests green. Six of the 43 methods carry the documented rule with no read-back. Rule 11 counts the rule unpinned: no test asserts it at the method, and none of the nine plants covers it.

**Fix:** Add the null read-back to each of the three `TextOfA...SetsTheBytesOfEachOverload` tests, after the LString case, on the live receiver:

```csharp
label.Text((string?)null);
Assert.Empty(label.Text);
Assert.Null(label.LocalizedText);
label.Text(hello);          // restore for the next assert when needed
label.Text((LString?)null);
Assert.Empty(label.Text);
Assert.Null(label.LocalizedText);
```

Repeat on the `Button` and `TextField` tests (`Assert.Empty` on the `Text` span). No production change; the class stays under its current test count.

**Effort:** Low

### CR-2: `Name("")` setting an empty name has no test

**Severity:** LOW
**Rule:** 6 (The null rules) and 11 (Test strength)
**File:** `tests/BrutalUi.Core.Tests/Builder/ElementBuilderElementTests.cs:24-43`; the rule sits in the doc at `src/BrutalUi.Core/Tree/ElementBuilder.cs:62-63`

**Problem:** The `Name` doc states: "A `null` name clears the name. An empty name is valid, and no name selector matches it." The test pins the null half (`root.Name((string?)null)` then `Assert.Null(root.Name)` at lines 39-41) and the same-name-marks-nothing rule. No test calls `Name("")` and reads the name back. The property write itself is safe (`Element.Name` accepts any string), so this is a test-strength gap, not a behavior bug: a future method-level special case that rejected or normalized an empty name would pass.

**Fix:** In `NameWithNullClearsTheNameAndTheSameNameMarksNothing`, after the null clear, add:

```csharp
root.Name("");
Assert.Equal("", root.Name);
```

The "matches no name selector" half already belongs to `NameSelectorTests` and `FindGivesNullForAMissAnEmptyNameAndAnUnnamedTree`; do not repeat it here.

**Effort:** Low

### CR-3: Section 5 of the design doc still shows `public class Element`

**Severity:** LOW
**Rule:** 10 (API clarity and docs)
**File:** `docs/technical-design.md:184`

**Problem:** Phase 3 changed the declaration (`src/BrutalUi.Core/Tree/Element.cs:31`):

```csharp
public partial class Element
```

The section 5 Shape block, which the same phase updated with the two `Find` lines at 200-201, still opens with:

```csharp
public class Element
```

The block now reads as an API map of `Element`, `Element.Find.cs`, and `InlineStyle` members, so the modifier mismatch is the kind of drift a later feature copies. Rule 10 asks section 5 to match the code.

**Fix:** Change the line to `public partial class Element`. One-word doc change, no re-flow.

**Effort:** Low

### CR-4: The `Find<T>` doc ends in an unreadable clause

**Severity:** LOW
**Rule:** 10 (API clarity and docs)
**File:** `src/BrutalUi.Core/Tree/Element.Find.cs:54-56`

**Problem:** The third paragraph of the `Find<T>` remarks:

```csharp
/// An element with the name but another type does not match, and the walk walks on. The method gives the first
/// element with the name and the type, not the first element with the name that is a <typeparamref name="T"/>
/// of a missed hit.
```

The final clause fails ASD-STE100 (project rule: short sentences, no ambiguous reference) and obscures the Q3 rule. Read literally, "not the first element with the name that is a T" says the method does *not* give what it actually gives. The plan words it plainly: "It does not stop at the first name hit."

**Fix:** Replace the paragraph with two short sentences:

```csharp
/// An element with the name but another type does not match, and the walk does not stop there. The method
/// gives the first element that has the name and the type <typeparamref name="T"/>.
```

**Effort:** Low

### CR-5: `OnSubmitted` has no same-instance assert in the `+=` test

**Severity:** LOW
**Rule:** 11 (Test strength)
**File:** `tests/BrutalUi.Core.Tests/Builder/ElementBuilderWidgetTests.cs:168-171`

**Problem:** `EachWidgetSlotMethodAddsTheHandlerWithPlusEquals` captures the chain result and asserts `ReferenceEquals` for five of the six widget delegate methods. The `Submitted` block drops the result:

```csharp
field.Submitted = _ => order.Add("submit game");
field.OnSubmitted(_ => order.Add("submit one")).OnSubmitted(_ => order.Add("submit two"));
Assert.Equal(3, field.Submitted!.GetInvocationList().Length);
```

The Quality Bar states "each fluent method returns the element of the call with its own type", and the other five methods in the same test pin it. `OnSubmitted` is the only widget delegate with no same-instance read-back (the null receiver, null handler, and add-order asserts do cover it at lines 208 and 214).

**Fix:** Mirror the siblings:

```csharp
TextField sameField2 = field.OnSubmitted(_ => order.Add("submit one")).OnSubmitted(_ => order.Add("submit two"));
Assert.True(ReferenceEquals(field, sameField2));
```

**Effort:** Low

### CR-6: `Capacity` and `ScrollSpeed` are get-only, so a template attribute for either forces a change here later

**Severity:** LOW
**Rule:** 9 (Integration with the epic consumers)
**File:** `src/BrutalUi.Core/Elements/TextField.cs:336` (`public int Capacity => _buffer.Length;`) and `src/BrutalUi.Core/Elements/ScrollView.cs:228` (`public float ScrollSpeed => _scrollSpeed;`)

**Problem:** A6 gives these create-time values no builder method, and the review confirms nothing exists for them in `ElementBuilder.Widgets.cs`. They also have no public setter at all, yet `TreeCompare` compares them (`tests/BrutalUi.Core.Tests/Shared/TreeCompare.cs:199-201` and `:235`), and Notes 3 of the plan states the loader maps XML "row by row" onto the builder surface. Neither consumer requirement asks for a `capacity` or `scroll-speed` attribute today, so the plan is consistent. But when a template wants either, there is no table row to write and no property to call: the only path is a create-info built inside the loader's own factory, or a reopened A6 with a new method here. Rule 9 asks me to name the surface that will force a change here.

**Fix:** No code change in this feature. Record the consequence once, at the hand-off that already carries Notes 3 and 4: add to `docs/features/markup/xml-element-loader/requirements.md` (Dependencies or Out of Scope) one line: "`Capacity` of `TextField` and `ScrollSpeed` of `ScrollView` exist only in the create info (A6); they have no builder method and no public setter, so they have no table row, and a template attribute for either needs a decision first." Then a later loader plan cannot assume a row and bounce the change back here.

**Effort:** Low

---

## What's Already Good

- The 1 to 1 map holds everywhere: each value method body is `ArgumentNullException.ThrowIfNull`, one property write or `SetRange` call, and `return` — no rule added (`Range` passes the throw of `SetRange` through, and `RangeCallsSetRangeAndThrowsForABadRange` proves the old range stays). No method exists for `States`, `Capacity`, `ScrollSpeed`, `Inline`, or `Layout`, and the file remarks of `ElementBuilder.Widgets.cs:12-13` state the A6 exclusions where a reader looks for them.
- The shipped surface is provably untouched: the `ElementBuilder.cs` diff removes two lines, `CommandBindings.cs` removes one doc line and keeps `BindCommand` word for word, `SlotContainer.cs` and `samples/` show no diff, and item 8 prints only the two `partial` lines — no new public type, exactly Decision 1.
- The delegate name rule is mechanical and verified both ways: all six widget names add the `On` prefix, all fourteen event names remove it, the task 4.5 grep prints nothing, and `ADelegateCountDifferenceGivesThePath` plus the eight-type handler tree prove the slots fill. The invocable-property argument (why `Clicked(h)` cannot reach a method) sits in the file remarks of both delegate files, where a user of the API reads it.
- The `+=` rule and the order are pinned hard: `EachWidgetSlotMethodAddsTheHandlerWithPlusEquals` runs all six widget slots through `=`, two builder calls, count 3, and the exact run order; `CommandAndOnClickedKeepBothDelegatesInEachOrder` checks `Command` against `OnClicked` in both chain orders with the invocation-list length; plant 8 (the `=` write) fails exactly those two.
- `Element.Find.cs` implements The Query literally: root first, index loop over `Children` (no enumerator — the shipped `SlotContainer.Walk` uses `foreach` and its gate tolerates that; this one needs no tolerance), the 1-to-3-bytes length reject, and `Utf8Text.Utf8Equals` as the only compare, so the case-sensitive ordinal rule falls out of the shared helper instead of a second compare. All seven query tests plus the two gate tests hit every clause of rule 5 and rule 8, and plants 5, 6, and 7 cover root-first, 0 bytes, and the typed skip.
- `TreeCompare` matches the planned member list line for line, compares delegate counts and not identity (the one form that lets a bound tree and a builder tree test equal, per Decision 7), states the not-compared list in its own remarks, and keeps to `System`, `System.Text`, and Core types with the `object`-typed `LString` dodge recorded in `context.md`. The Framework link line sits where `AllocationGate` style links sit in the csproj, and the remarks already hold the exact link line for `BrutalUi.Markup.Tests` (rule 9, Notes 3).
- Docs and names: the section 11 example block equals the test expression after the body indent, the compiling `new Label(default)` form keeps its promise (Decision 8 never had to fall back), line 301 of section 5 names the element-group methods and points at section 11, the delegate-name paragraph in section 11 states the rule in two sentences, section 14 carries the twentieth gate with its three plant readings, and the changelog names the feature exactly once. Every public member has a doc comment (CS1591 is an error, and the build reads 0 warnings).
- The nine plants each failed exactly the named tests with a non-zero exit and `error CS` 0, with `cmp`-clean restores, and the three deviations (plants 2, 4, and 7) are recorded as decisions in `context.md` rather than silently accepted. The final counts read Core 1839, Framework 332, solution 3032 with 3 skipped — the planned totals from the recorded baseline.

---

## Recommended Fix Order

| Order | Finding | Impact | Effort | Reason |
|-------|---------|--------|--------|--------|
| 1 | CR-1 | Medium-low | Low | Six methods carry a documented null rule with no method-level assert; the suite cannot see a method-level regression. Do it inside the three existing tests. |
| 2 | CR-2 | Low | Low | One line completes the null-rule set for `Name`; same file, same test. |
| 3 | CR-5 | Low | Low | One line closes the last same-instance gap in the delegate tests. |
| 4 | CR-4 | Low | Low | The doc reword keeps the only public-facing sentence about Q3 readable; pure doc, no test change. |
| 5 | CR-3 | Low | Low | One word in `technical-design.md`; do it with CR-4 so the doc pass is one commit. |
| 6 | CR-6 | Low | Low | Cross-feature hand-off line; cheapest now, expensive when the loader plan invents a capacity row. |

---

## Progress

- [x] CR-1: The `Text` null-gives-empty-text rule has no test and no plant (ElementBuilderTextTests.cs) — fixed 2026-11-12: the null read-back now sits in all three `TextOfA...SetsTheBytesOfEachOverload` tests. `Assert.Empty` does not compile on a `ReadOnlySpan<byte>` in xunit.v3 4.0.2, so the house form `Assert.True(<x>.Text.IsEmpty)` carries the assert at the six sites. Class stays at 5 tests, `failed: 0`.
- [x] CR-2: `Name("")` setting an empty name has no test (ElementBuilderElementTests.cs) — fixed 2026-11-12: `root.Name("")` and `Assert.Equal("", root.Name)` now follow the null clear in `NameWithNullClearsTheNameAndTheSameNameMarksNothing`. Class stays at 4 tests, `failed: 0`.
- [x] CR-3: Section 5 of the design doc still shows `public class Element` (technical-design.md:184) — fixed 2026-11-12: the line reads `public partial class Element`. One word, no re-flow; the `Find` lines at 200-201 and line 301 stay as phases 1 and 3 wrote them.
- [x] CR-4: The `Find<T>` doc ends in an unreadable clause (Element.Find.cs:54-56) — fixed 2026-11-12: the paragraph now reads the two short sentences of the report, and keeps `<typeparamref name="T"/>`. No code line changed; the build reads `0 Warning(s)` and `0 Error(s)`, and `*ElementFind*` reads 9 with `failed: 0`.
- [x] CR-5: `OnSubmitted` has no same-instance assert in the `+=` test (ElementBuilderWidgetTests.cs:168-171) — fixed 2026-11-12: the `Submitted` block now captures the chain result in a typed `TextField` local and asserts `ReferenceEquals`, as the five siblings do. All six widget delegate methods now pin the same-instance rule. Class stays at 8 tests, `failed: 0`.
- [x] CR-6: `Capacity` and `ScrollSpeed` are get-only, so a template attribute for either forces a change here later (TextField.cs:336, ScrollView.cs:228) — recorded 2026-11-12: one hand-off line added under `## Out of Scope` of `docs/features/markup/xml-element-loader/requirements.md`, in the fix form above. No code change in this feature, and no plan written in the loader docs.
