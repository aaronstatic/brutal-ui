# markup/builder-api - Context and Decisions

**Last Updated:** 2026-11-12 10:35
**Current Phase:** Complete (phases 1 to 4 and the cross-phase review are done)
**Status:** ✅ Ready for Review

---

## Quick Status

**What's Done:**

- ✅ The plan (`implementation.md`) is complete: 4 phases, 33 tasks, all STANDARD (sonnet).
- ✅ The dev docs (`tasks.md`, `inbox.md`, this file) stand ready. Base branch `main` at `386793e`; the run works on `feat/markup/builder-api`.
- ✅ Phase 1 group 1 (tasks 1.1 to 1.6): done 2026-11-12. `ElementBuilder` is `public static partial` with `Name<T>`; `ElementBuilder.Text.cs` holds the six `Text` overloads of `Label` and `Button`; `CommandBindings.Command` calls `BindCommand`; `Shared/TreeCompare.cs` compiles in Core.Tests and Framework.Tests through the link line. Solution build: 0 warnings, 0 errors. Solution tests: 2988, 0 failed, 3 skipped (the baseline, unchanged — the group adds no test).
- ✅ Phase 1 group 2 (tasks 1.7 to 1.11): done 2026-11-12. `Builder/TreeCompareTests.cs` (5 tests), `Builder/ElementBuilderElementTests.cs` (3), `Builder/ElementBuilderTextTests.cs` (4), `Commands/CommandBuilderTests.cs` (5). Section 11 of `docs/technical-design.md` holds the compiling example; line 301 of section 5 names `Name(string?)`. Solution: 3005 tests, 0 failed, 3 skipped (Core 1813, Framework 331). Plants 1 and 2 each failed the named tests with a non-zero exit and `0 Error(s)`, and each restored file equals its copy under `cmp`.
- ✅ Phase 2 group 1 (tasks 2.1 to 2.6): done 2026-11-12. `ElementBuilder.cs` gains `Pickable<T>` and `Focusable<T>` (115 lines); `ElementBuilder.Text.cs` gains the three `TextField` overloads (143 lines); `ElementBuilder.Widgets.cs` (new, 143 lines) holds the eight value methods, no method for `Capacity` or `ScrollSpeed`. Tests: `ElementBuilderElementTests` +1, `ElementBuilderTextTests` +1, `ElementBuilderWidgetTests.cs` (new, 6). Solution: 3013 tests, 0 failed, 3 skipped (Core 1821 = 1813 + 8, Framework 331). Build: `0 Warning(s)`, `0 Error(s)`. `git diff --stat -- src/BrutalUi.Core/Elements/` is empty.

- ✅ Phase 2 group 2 (tasks 2.7 and 2.8): done 2026-11-12. `Builder/BuilderTreeTests.cs` (new, 2 tests): `AnEightTypeTreeFromTheFluentMethodsEqualsTheInitializerTree` (root `Element` with a name, two classes, `Pickable(false)`, `Focusable(true)`, and one `Label`, `Button`, `Toggle`, `Slider`, `TextField`, `Image`, and `ScrollView` with each value method), and `ValueBeforeRangeClampsToTheOldRange`. `docs/technical-design.md`: line 301 gains `Pickable(bool)` and `Focusable(bool)` plus the pointer sentence to section 11; section 11 gains the method lists for the element group, the text group, and the widget values. Solution: 3015 tests, 0 failed, 3 skipped (Core 1823 = 1813 + 10, Framework 331). Build: `0 Warning(s)`, `0 Error(s)`. Plants 3 and 4 each failed the named tests with exit 2 and `error CS` 0; both restores are `cmp` clean. Demo: `--filter-class '*BuilderTreeTests'` reads 2 with `failed: 0`.

- ✅ Phase 3 (tasks 3.1 to 3.6): done 2026-11-12. `Element.cs` gains the `partial` modifier and one remarks sentence, no member change. `Tree/Element.Find.cs` (new, 139 lines): `Find(ReadOnlySpan<byte>)`, `Find<T>(ReadOnlySpan<byte>) where T : Element`, and the two private walks with the length fast reject and `Utf8Text.Utf8Equals`. Tests: `Builder/ElementFindTests.cs` (new, 7) and `Builder/ElementFindAllocationTests.cs` (new, 2; tree of 218 named elements, three levels deep, target label last in tree order, 1000 rounds of a hit, a miss, and a typed hit at 0 bytes). Docs: section 5 `Element` block gains `Find` and `Find<T>`; section 11 gains the query bullet with the five rules of The Query; section 14 gains the twentieth allocation gate bullet with the three plants. Solution: 3024 tests, 0 failed, 3 skipped (Core 1832 = 1823 + 9, Framework 331). Build: `0 Warning(s)`, `0 Error(s)`. Demo: `--filter-class '*ElementFind*'` reads 9 with `failed: 0`. Plants 5, 6, and 7 each failed the named test with exit 2 and `error CS` 0; all restores `cmp` clean. `SlotContainer` (15 tests) and `UiManager` (14 tests) filters pass with no edit (risk 3 did not materialize). `git diff src/BrutalUi.Core/Tree/Element.cs` shows the modifier and the sentence only; `git diff --stat -- src/BrutalUi.Framework/Panels/` is empty.

- ✅ Phase 4 Group 1 (tasks 4.1 to 4.5): done 2026-11-12. `ElementBuilder.Widgets.cs` gains the six delegate methods (248 lines); new `ElementBuilder.Events.cs` holds the 14 generic event methods (287 lines). Each method toggles the `On` prefix, checks the receiver and the handler, adds with `+=`, and returns the receiver. Tests: `ElementBuilderWidgetTests` +2 (8 total), new `ElementBuilderEventTests` (4). Demo: `--filter-class '*ElementBuilderEventTests'` reads 4 with `failed: 0`; the routed test runs a `.PointerDown(h)` handler one time on a pushed pointer press. Name-rule grep: no output. `grep -cE '^    public static'` reads 6 + 9 + 14 + 14 = 43 across the four files. Solution: 3030 tests, 0 failed, 3 skipped (Core 1838 = 1832 + 6, Framework 331). Build: `0 Warning(s)`, `0 Error(s)`.

- ✅ Phase 4 Group 2 (tasks 4.6 to 4.8): done 2026-11-12. `CommandBuilderTests` gains `CommandAndOnClickedKeepBothDelegatesInEachOrder` (both chain orders run the handler and the command in order, and `Clicked` keeps two delegates); `BuilderTreeTests` gains `ATreeWithHandlersFromTheFluentMethodsEqualsTheInitializerTree` (the eight-type tree with one handler on each of the 6 widget slots and on 3 event slots, against a reference that sets the same static handlers with `=` and `+=`; `TreeCompare` gives `null`). Docs: section 11 gains the widget-delegate bullet, the event-group bullet, and the two-sentence delegate name rule; `CHANGELOG.md` gains the one `(markup/builder-api)` line. Solution: 3032 tests, 0 failed, 3 skipped (Core 1839 = 1832 + 7, Framework 332 = 331 + 1). Build: `0 Warning(s)`, `0 Error(s)`. Demos: `*ElementBuilder*` 21, `*BuilderTreeTests*` 3, `*ElementFind*` 9, `*TreeCompareTests*` 5, `*CommandBuilderTests*` 6, each with `failed: 0`. Plants 8 and 9 each failed exactly the named tests with exit 2 and `error CS` 0; both restores are `cmp` clean. All nine verification greps read as the plan expects (see the gate record below).

**What's Next:**

- ✅ The feature is complete: 33/33 tasks, all four phase verifiers PASS, the cross-phase review resolved (6 findings, 6 fixed), and the post-review gate PASS.
- ⏳ A person tests the four demo lines under "Needs Human Verification", then commits, pushes, and opens the PR for `feat/markup/builder-api`. `/autorun-feature` makes no commit and no push.
- ⏳ The next feature of the epic is `markup/xml-element-loader`. It reads the property surface of this feature row by row, links `tests/BrutalUi.Core.Tests/Shared/TreeCompare.cs` into `BrutalUi.Markup.Tests`, applies `Range` before `Value`, and now also carries the CR-6 hand-off line for `Capacity` and `ScrollSpeed`.

**Blockers:**

- None.

---

## Base and Counts

Task 1.1 recorded the real baseline before any code, on 2026-11-12.

- Base commit: `386793ef733863fd8f82e2e59d1d0b5f299beb94` (`386793e`), the same as at planning time. `git status --porcelain` showed only the four untracked feature docs.
- Solution: 2988 tests, 0 failed, 3 skipped (the three GLFW display tests). Measured with `MSBUILDDISABLENODEREUSE=1 dotnet test --solution brutal-ui.sln`.
- `BrutalUi.Core.Tests`: 1801, 0 failed, 0 skipped.
- `BrutalUi.Framework.Tests`: 326, 0 failed, 0 skipped.
- The planned end totals count from these: Core 1839, Framework 332, solution 3032 with 3 skipped.

## Recorded Plan Sections

Task 1.1 records these sections of `implementation.md` here. The plan stays the source of truth; this is the working copy.

### Public API (end state)

43 `public static` methods in the four `ElementBuilder` partial files (3 shipped, 40 new), 2 `Find` methods on `Element`, and 1 `Command` method in `CommandBindings`.

- Element group, `ElementBuilder.cs`: `Class<T>(string)`, `Class<T>(ClassId)`, `Child<T>(Element)` (shipped, text unchanged); new `Name<T>(this T element, string? name)`, `Pickable<T>(this T element, bool value)` (writes `IsPickable`), `Focusable<T>(this T element, bool value)` (writes `IsFocusable`). Each generic with `where T : Element`.
- Text group, `ElementBuilder.Text.cs`: `Text` with `(ReadOnlySpan<byte>)`, `(string?)`, `(LString?)` for `Label`, `Button`, and `TextField`. Each calls the matching `SetText` and returns the receiver.
- Widget group, `ElementBuilder.Widgets.cs`: `Toggle.Checked(bool)`, `Slider.Value(float)`, `Slider.Range(float, float, float)` (calls `SetRange`), `Image.Texture(TextureHandle)`, `Image.Uv(float4)`, `Image.Tint(Rgba8)`, `Image.ScaleMode(ImageScaleMode)`, `ScrollView.ScrollOffset(float2)`. No methods for `Capacity` and `ScrollSpeed` (A6). Phase 4 adds `OnClicked`, `OnCheckedChanged`, `OnValueChanged`, `OnTextChanged`, `OnSubmitted`, `OnScrollChanged`.
- Event group, `ElementBuilder.Events.cs` (phase 4): 14 generic methods `PointerMove`, `PointerMoveCapture`, `PointerDown`, `PointerDownCapture`, `PointerUp`, `PointerUpCapture`, `Scroll`, `ScrollCapture`, `KeyDown`, `KeyDownCapture`, `KeyUp`, `KeyUpCapture`, `Char`, `CharCapture`. Each adds its handler to the matching `On*` slot with `+=`.
- Framework: `CommandBindings.Command(this Button, CommandRegistry, ReadOnlySpan<byte>)` calls `BindCommand`.
- Query: `Element.Find(ReadOnlySpan<byte>)` and `Element.Find<T>(ReadOnlySpan<byte>) where T : Element` on `public partial class Element`.
- No new public type appears. Each method returns the receiver with its own static type. Each method throws `ArgumentNullException` for a `null` receiver (parameter name `element`, or the receiver name).

### The Query

`Find(name)`: an empty name gives `null`; else a recursive walk. The walk checks the element itself first, then the children in order (depth-first pre-order, the order of `SlotContainer.Walk`). A match is `e.Name is string s` with the fast reject `s.Length <= name.Length && name.Length <= 3 * s.Length` and `Utf8Text.Utf8Equals(name, s)`. The compare is ordinal on the UTF-8 form, so `Score` and `score` are two names. `Find<T>` skips an element of another type and walks on. An element with a `null` name never matches. The call allocates 0 bytes, so a game can call it in a frame.

### The Delegate Method Names

C# binds `button.Clicked(h)` to a call of the delegate in the invocable property, not to an extension method. So a delegate method toggles the `On` prefix of its slot: `Clicked` gives `OnClicked`; `OnPointerDown` gives `PointerDown`. A value property is not invocable, so value methods keep the property name. Each delegate method throws `ArgumentNullException` for a `null` handler, adds with `+=`, and returns the receiver.

### The Tree Compare

`tests/BrutalUi.Core.Tests/Shared/TreeCompare.cs`: `internal static class TreeCompare` in namespace `BrutalUi.Core.Tests`, one method `FirstDifference(Element expected, Element actual)`, null when equal. A message names the path and the member, for example `root/1/0: Name differs (expected 'score', actual 'scor')`. The path is `root` plus child indexes, for example `root/1/0`.

- Per element: runtime type, `TypeId`, `Name` (ordinal), `IsPickable`, `IsFocusable`, `States`, the class count and each `ClassId` in order, the child count, the invocation count of the 14 event slots (0 for `null`).
- Per widget: `Label` and `Button` text bytes and `LocalizedText` (reference); `Button.Clicked` count; `Toggle.CheckedChanged` count; `Slider` `Minimum`, `Maximum`, `Step`, `Value` (float.Equals) and `ValueChanged` count; `TextField` `Capacity`, text bytes, `TextChanged` and `Submitted` counts; `Image` `Texture.Id`, `Texture.Size`, `Uv`, `Tint`, `ScaleMode`; `ScrollView` `ScrollOffset`, `ScrollSpeed`, `ScrollChanged` count.
- The helper compares delegate invocation counts, not delegate identity. It uses `System`, `System.Text`, and Core types only, and no test framework type. It does not compare `Inline`, `Layout`, `Style`, `Dirty`, `BindingCount`.
- Phase 1 writes the whole method (the eight-type list), because phase 2 reuses the file.

### Notes (from the plan)

1. The old section 11 form `new Label { Name = "score" }` does not compile: `Label` and `Button` have only the `(in CreateInfo)` constructor. The example uses `new Label(default)`.
2. Q2 has no slot-name methods: C# does not reach an extension method named like an invocable property. Decision 2 gives the names.
3. xml-element-loader links `Shared/TreeCompare.cs` into `BrutalUi.Markup.Tests` and extends it when a template sets a member the helper does not compare. The 12 value names of A2 are its table rows.
4. For xml-element-loader: `Value` clamps to the current range, so a `value` row must run after the range rows.
5. For binding-attributes: `Command` and `BindCommand` share one body; a bound tree and a builder tree compare equal under `TreeCompare`.
6. Requirements line 15 tags the query "(Q4)"; the plan follows A8 and Q3.
7. "Settable" excludes `States`, inline style, `Capacity`, `ScrollSpeed`, and `Element.Layout`.
8. A1 covers the Core fluent methods; `Command` (A9) and `Find` (A8) follow their own items.
9. The planned totals come from the binding-remap close; the real baseline above equals them.

---

## Key Files

### Core Implementation

- `src/BrutalUi.Core/Tree/ElementBuilder.cs` - the element group: `Class`, `Child` (shipped), `Name`, `Pickable`, `Focusable`.
- `src/BrutalUi.Core/Tree/ElementBuilder.Text.cs` - the text group: the nine `Text` overloads of `Label`, `Button`, and `TextField`.
- `src/BrutalUi.Core/Tree/ElementBuilder.Widgets.cs` - the widget group: 8 value methods (phase 2), 6 delegate methods (phase 4).
- `src/BrutalUi.Core/Tree/ElementBuilder.Events.cs` - the event group: the 14 generic event methods (phase 4).
- `src/BrutalUi.Core/Tree/Element.cs` - the `partial` modifier only (phase 3).
- `src/BrutalUi.Core/Tree/Element.Find.cs` - `Find`, `Find<T>`, and the two private walks (phase 3).
- `src/BrutalUi.Framework/Commands/CommandBindings.cs` - `Command` next to the shipped `BindCommand`.

### Related Files

- `tests/BrutalUi.Core.Tests/Shared/TreeCompare.cs` - the tree compare helper, linked by Framework tests now and by Markup tests later.
- `tests/BrutalUi.Core.Tests/Builder/` - the new test folder for this feature.
- `docs/technical-design.md` - section 5 line 301, the section 5 `Element` block, section 11, and section 14.
- `docs/features/markup/builder-api/implementation.md` - the plan and the source of truth.

---

## Important Decisions

The plan holds eight decisions. This section records only what a phase adds or changes.

### Decision (from the plan): Four partial files, explicit overloads, no new public type

See Decision 1 of `implementation.md`.

### Decision (from the plan): A delegate method toggles the `On` prefix

See Decision 2 of `implementation.md`. A delegate property is invocable, so C# never reaches an extension method with the slot name.

### Decision (group 1): TreeCompare message grammar and the `LString` avoidance

`TreeCompare.FirstDifference` returns messages in the form `<path>: <Member> differs (expected <e>, actual <a>)`. The member names the tests see: `Type`, `TypeId`, `Name`, `IsPickable`, `IsFocusable`, `States`, `Class count`, `Class <i>`, `Child count`, `<Slot> count` for each invocation count (for example `OnKeyDown count`, `Clicked count`), `Text`, `LocalizedText reference`, `Capacity`, `Minimum`, `Maximum`, `Step`, `Value`, `Texture.Id`, `Texture.Size`, `Uv`, `Tint`, `ScaleMode`, `ScrollOffset`, `ScrollSpeed`. `null` shows as `null`, a string as `'text'`, and a set `LocalizedText` reference as `set`.

Task 1.5 allows only `System`, `System.Text`, and Core types. `Label.LocalizedText` has the type `LString` from `Brutal.Localization`, so the helper compares it with `ReferenceEquals` through an `object?` parameter and never names the type. No `using` outside `System`, `System.Text`, and `BrutalUi.Core` appears in the file.

The text-group rules (the `SetText` map, the `BRUTALCORE0011` note, the cast for a bare `null`, `null` gives an empty text) sit once in the `ElementBuilder.Text.cs` class remarks, and each method doc repeats the receiver null rule in its `<exception>` line.

### Decision (group 2): Risk 1 did not materialize

`Label label = new Label(default(Label.CreateInfo)).Name("score");` compiles with the literal: `Name` has no span overload, so `BRUTALCORE0011` stays quiet. The doc example keeps the literal, and the test keeps it too. The analyzer still rejects a literal at `Text(string?)`, so every test that needs a string text passes a local, for example `string text = "World"; label.Text(text);`.

### Decision (group 2): The scroll offset case needs a settled layout

`ScrollView.ScrollOffset` gives the stored offset clamped with the layout of the last update, so a set on a view with no layout reads back `(0, 0)` and `TreeCompare` sees no difference. `AWidgetValueDifferenceGivesThePath` therefore reuses `ScrollViewTree`: it calls `Settle()` on two trees, sets `(0, 40)` on one, and leaves the other at `(0, 0)`. The path of the difference is `root/0`, the same shape as the other six cases.

### Decision (group 2): The doc block and the test expression match after the body indent

The acceptance line "the section 11 code block equals the fluent expression of the test, character for character" holds for the text of the expression: the three doc lines and the three test lines are equal once the test removes its 8-space method-body indent. C# formatting needs that indent, so the absolute leading spaces differ. A `diff` of the two blocks extracted with `grep -A2` reads no difference.

### Decision (group 2): Plant 2 also fails two checks that the brief did not name

The planted `Command` body `=> button;` drops the bind and the argument checks, so `CommandChecksTheButtonTheRegistryAndTheName` and `CommandKeepsAHandlerThatTheGameSetBefore` fail next to the two tests the brief names. The named failures hold, so the plant proves what it must. No test change.

### Decision (group 2): How a plant checks `0 Error(s)`

`dotnet test --solution` prints no build summary, so the plant log check reads `grep -c "error CS"` for 0 instead of the `0 Error(s)` line. A failing assertion also proves the planted binary ran, because an uncompilable plant leaves the old binary and passes.

### Decision (phase 2, group 1): What the value tests pin

The eight value methods map 1 to 1 to their properties (Quality Bar), so the tests pin the property rules the docs name, not new rules:

- `ValueAppliesTheValueRuleAndCallsNoDelegate` uses a slider with the range 0 to 10 and step 2: 20 clamps to 10, NaN gives the minimum 0, and the off-grid 3.4 rounds to 4 (no midpoint, so the away-from-zero tie rule does not bind).
- `RangeCallsSetRangeAndThrowsForABadRange` asserts `Range(5, 1, 0)` throws `ArgumentOutOfRangeException` and leaves the old 0 to 10 step 1 range, the pass-through of `SetRange` (Decision 5). The order rule (range before value, Notes item 4) stays for the group 2 tree test `ValueBeforeRangeClampsToTheOldRange`.
- `ScrollOffsetSetsTheOffset` reuses `ScrollViewTree`, calls `Settle()` first (the layout of the last update clamps the get), sets (0, 40), and checks the clamp with a set of (500, 500) against the tree maximum (100, 80).
- The null checks use the receiver names of the Public API: `toggle`, `slider`, `image`, `view`. The `ParamName` of the `SetRange` throw is not asserted, because `Slider.SetRange` owns that choice.

### Decision (phase 2, group 2): The eight-type tree plants through the slider

The slider of the tree uses `.Range(0f, 10f, 1f).Value(5f)` against a reference `CreateInfo` with `Minimum = 0f, Maximum = 10f, Step = 1f, Value = 5f`. The choice carries both plants: plant 3 forces the step to 0, so `Step` differs, and plant 4 drops the value write, so `Value` differs. The `TextField` gets `Capacity` from the create info (A6 gives no method) and `Text` from the `u8` overload. The two text trees compare equal because `Label.CreateInfo.Text` and `Text(ReadOnlySpan<byte>)` store the same bytes, and both `LocalizedText` members are `null`. The view sets `ScrollOffset(0, 40)` on both paths; neither tree has a layout, so each get clamps to `(0, 0)` and the compare holds. The tree needs no settled layout and calls no `UiContext`.

### Decision (phase 2, group 2): Plant 4 also fails the order test

The planted `Value` body returns the slider and writes nothing, so `ValueBeforeRangeClampsToTheOldRange` fails next to the two tests the brief names: its first chain reads `0` where the rule gives `1`. The named failures hold, so the plant proves what it must. No test change. Plant 3 failed exactly the two named tests.

### Decision (phase 3): The walk names and the cref form in `Element.Find.cs`

`Find` calls the private static `Walk(Element, ReadOnlySpan<byte>)`; `Find<T>` calls the private static generic `Walk<T>(Element, ReadOnlySpan<byte>)`. The two `Walk` members differ in their parameter lists, so each call has one match, and `dotnet build` stays at 0 warnings. The class remarks of `Element.cs` name the members with `<see cref="Find(ReadOnlySpan{byte})"/>` and `<see cref="Find{T}(ReadOnlySpan{byte})"/>`; the crefs resolve, because both partial parts compile as one type. No cref names a Framework type: the doc says "depth-first pre-order", and no sentence points at `SlotContainer`.

### Decision (phase 3): Plant 7 needs the stop at the sibling level

The brief words plant 7 as "the typed walk returns `null` at the first name hit of another type". The one-line form `if (Matches(element, name)) { return element is T hit ? hit : null; }` only ends the branch: the parent loop walks on to the next sibling, so `FindTSkipsAnElementOfAnotherType` still passes. The plant therefore also checks each child with `Matches` before the recursion, so the first name hit ends the whole walk and gives `null`. That plant failed only the named test, with exit 2 and `error CS` 0. No production code and no test changed.

### Decision (phase 4, group 1): Compare handlers with `Assert.Same` on a typed local

A method-group conversion to `Delegate` (no target delegate type) uses the C# natural-type rule and synthesizes a new delegate type (`<>A{...}`1`). Such an instance never equals the `UiEventHandler<T>` instance in the slot, even for the same static method. The tests therefore convert each handler once to a typed local (`UiEventHandler<KeyEvent> added = KeyHandler;`) and compare with `Assert.Same`, which also pins that the slot holds the exact instance.

### Decision (phase 4, group 1): The test facts behind the three first-run failures

Three new tests failed on the first run and the production code needed no second change. (1) `TextField` rejects `default(CreateInfo)`: `Capacity must be above 0`, so the new tests pass `Capacity = 16`, the pattern of `ElementBuilderTextTests`. (2) and (3) are the `Delegate` natural-type case above. `button.OnClicked(h)` and `element.PointerDown(h)` compile as written (the widget and event tests), so Decision 2 holds: no method carries the name of its slot and the grep of task 4.5 gives no output. The routed test `ABuilderHandlerRunsOnARoutedEvent` pushes one `PointerDown` with the element as the header target (the `EventWalkTests` pattern) and `DrainEvents` runs the builder-added handler one time in the target phase.

### Decision (phase 4, group 2): The tree test picks three event slots that plant 9 does not touch

`ATreeWithHandlersFromTheFluentMethodsEqualsTheInitializerTree` sets the event handlers on the root slots `OnPointerDown`, `OnKeyDown`, and `OnChar` (indexes 2, 8, and 12), and the six widget slots on their own widgets. The reference writes three slots with `=` and six with `+=`; `TreeCompare` counts the delegates in each slot, not the identity, so the trees compare equal. The choice keeps plant 9 (`KeyUpCapture` writes `OnKeyUp`) inside one failure: the tree test uses no `KeyUp` slot, so only `EachEventMethodAddsTheHandlerToItsSlot` fails.

### Decision (phase 4, group 2): One context per order in the Framework both-order test

`CommandAndOnClickedKeepBothDelegatesInEachOrder` builds one button per chain order, each in its own context and root, and clicks it with the new private `Click` helper (update, a left press and release at (40, 12), update). Each order asserts the run order of the names, and asserts `Clicked.GetInvocationList().Length == 2`, so the test pins the `+=` rule next to the order rule. Plant 8 (`OnClicked` writes `button.Clicked = handler`) fails both named tests: with `=` the command-first chain loses the command delegate, so the run order reads `handler` only, and `EachWidgetSlotMethodAddsTheHandlerWithPlusEquals` reads one delegate where it expects three.

### Decision (phase 4, group 2): The name rule holds two sentences in section 11

Section 11 gains the widget-delegate bullet, the event-group bullet, and the paragraph **The delegate name rule** in exactly two sentences: the invocable slot explains why no method carries the slot name, and the prefix toggle names both directions. The third sentence states the `+=` effect of `Command`, because the Framework test of 4.6 depends on it. `CHANGELOG.md` holds one `(markup/builder-api)` line under `## [Unreleased]` → `### Added`, after the binding-remap line.

---

## Open Questions

- None. The public API shape followed the plan of `implementation.md` in all four phases.

---

## Gotchas & Learnings

- Risk 1: `BRUTALCORE0011` and the literal argument of `Name`. Task 1.8 compiles a literal; when the analyzer rejects it, record it here and pass a local.
- A bare `Text(null)` is ambiguous between the three overloads; cast it: `Text((string?)null)`.
- A test that needs a string text passes a local, because the analyzer rejects a literal argument of `Text(string?)`.
- A plant that does not compile leaves the old binary, and the old binary passes: check `0 Error(s)` before the exit code.
- The build sandbox has a read-only `~/.dotnet`, so the first `dotnet` call fails on the first-time-use sentinel. Export `DOTNET_CLI_HOME=/tmp/dui-cli-home` (the directory must exist) before `dotnet build` and `dotnet test`.
- A method group assigned to `Delegate` gets a synthesized natural delegate type, so delegate equality against a typed slot fails; convert to the delegate type first and use `Assert.Same`.
- `new TextField(default(TextField.CreateInfo))` throws: `Capacity` must be above 0. Pass a capacity in the create info.

---

## Testing Approach

- Unit tests in Core, one class per builder file, plus the tree compare, the query, and the query allocation gate.
- Tree tests compare a fluent build with an initializer build through `TreeCompare.FirstDifference`.
- Framework tests cover `Command`, its checks, the order with `OnClicked`, and the section 11 example.
- Nine plants across the four phases prove each named test fails.
- No manual check: this feature adds no window, no sample, and no UI.

---

## Needs Human Verification

`/autorun-feature` adds one line per item that the verifier cannot check, for a person to test.

- [ ] Phase 1 demo: the developer runs the tests, and the test of the section 11 example passes: `BrutalUi.Framework.Tests --filter-class '*CommandBuilderTests'` reads a total of 5 with `failed: 0`, and `TheSectionElevenExampleBuildsTheSameTreeAsInitializers` is one of them.
- [ ] Phase 2 demo: the developer runs the tests, and the eight-type tree test passes: `BrutalUi.Core.Tests --filter-class '*BuilderTreeTests'` reads a total of 2 with `failed: 0`.
- [ ] Phase 3 demo: the developer runs the tests, and the query passes: `BrutalUi.Core.Tests --filter-class '*ElementFind*'` reads a total of 9 with `failed: 0`, and the allocation test measures 0 bytes.
- [ ] Phase 4 demo: the developer runs the tests, and the delegate methods pass: `BrutalUi.Core.Tests --filter-class '*ElementBuilderEventTests'` reads a total of 4 with `failed: 0`, `--filter-class '*ElementBuilder*'` reads 21 with `failed: 0`, and `BrutalUi.Framework.Tests --filter-class '*CommandBuilderTests'` reads 6 with `failed: 0`.

---

## Session Notes

### 2026-11-12 07:11

- `/autorun-feature markup/builder-api` started the run. Created the branch `feat/markup/builder-api` from `main` (`386793e`), and scaffolded `tasks.md`, `inbox.md`, and this file.
- Next session: run phase 1 group 1.

### 2026-11-12 07:26 (phase 1 group 1)

- Ran tasks 1.1 to 1.6. The baseline reads equal the planning numbers: solution 2988 with 3 skipped, Core 1801, Framework 326. The base commit is `386793ef733863fd8f82e2e59d1d0b5f299beb94`.
- Added: `Name<T>` and the partial modifier in `ElementBuilder.cs`; the six `Text` overloads in `ElementBuilder.Text.cs` (93 lines); `Command` in `CommandBindings.cs`; `Shared/TreeCompare.cs` (319 lines); the link line in `BrutalUi.Framework.Tests.csproj`.
- Verification: build 0 warnings, 0 errors. Solution tests 2988, 0 failed, 3 skipped. `git diff -U0 src/BrutalUi.Core/Tree/ElementBuilder.cs` shows only the old remarks line and the old class line. `git diff --stat` over `src/BrutalUi.Markup`, `src/BrutalUi.Core/Elements`, `src/BrutalUi.Framework/Panels`, and `samples` is empty.
- Open questions: none. The API shape followed the plan exactly.
- Next session: run phase 1 group 2 (tasks 1.7 to 1.11).

### 2026-11-12 07:42 (phase 1 group 2)

- Ran tasks 1.7 to 1.11. Added `Builder/TreeCompareTests.cs` (5), `Builder/ElementBuilderElementTests.cs` (3), `Builder/ElementBuilderTextTests.cs` (4), and `Commands/CommandBuilderTests.cs` (5). No source file changed in this group; the code of group 1 needed no edit.
- Counts: Core 1813 (1801 + 12), Framework 331 (326 + 5), solution 3005 with 0 failed and 3 skipped. Build: `0 Warning(s)`, `0 Error(s)`.
- Demo: `BrutalUi.Framework.Tests --filter-class '*CommandBuilderTests'` reads a total of 5 with `failed: 0`, and `TheSectionElevenExampleBuildsTheSameTreeAsInitializers` is one of them. The test builds the section 11 example, compares it with the initializer tree through `TreeCompare`, then clicks the fluent `pause` button and counts one `game.pause` call.
- Docs: section 11 holds the compiling example, the `registry` sentence, and the `u8` sentence. Section 5 line 301 names `Name(string?)` next to `Class(string)`, `Class(ClassId)`, and `Child(Element)`.
- Plant 1 (`TreeCompare` skips the `Name` check): exit 2, one failure, `ANameOrAClassDifferenceGivesThePath`, and `error CS` count 0. Restore: `cmp` clean, then `TreeCompareTests` reads 5 with 0 failed.
- Plant 2 (`Command` returns `button` and calls nothing): exit 2, four failures — the named `CommandInvokesTheCommandOnAClick` and `TheSectionElevenExampleBuildsTheSameTreeAsInitializers`, plus `CommandKeepsAHandlerThatTheGameSetBefore` and `CommandChecksTheButtonTheRegistryAndTheName`. `error CS` count 0. Restore: `cmp` clean, then `CommandBuilderTests` reads 5 with 0 failed.
- Guard: `git diff -U0 src/BrutalUi.Core/Tree/ElementBuilder.cs` removes only the old remarks line and the old class line, both from group 1. `git status --porcelain` shows no change outside the six files of this group and the files of group 1.
- Open questions: none. Deviation from the brief: plant 2 failed two extra tests (see the decision above).
- Next session: run phase 2 group 1 (tasks 2.1 to 2.6).

### 2026-11-12 07:55 (phase 2 group 1)

- Ran tasks 2.1 to 2.6. Added `Pickable<T>` and `Focusable<T>` to `ElementBuilder.cs` (115 lines); the three `TextField` `Text` overloads to `ElementBuilder.Text.cs` (143 lines); the new `ElementBuilder.Widgets.cs` (143 lines) with the eight value methods and the file remarks that point at the phase 4 delegate methods. No method for `Capacity` or `ScrollSpeed` (A6).
- Tests: `ElementBuilderElementTests` gains `PickableAndFocusableSetTheFlagsAndReturnTheSameElement` and the two new null checks (4 total); `ElementBuilderTextTests` gains `TextOfATextFieldSetsTheBytesOfEachOverload` and the three new null checks (5 total); the new `ElementBuilderWidgetTests.cs` holds the 6 tests of the brief (6 total). `--filter-class '*ElementBuilder*'` reads 15 with `failed: 0`.
- Counts: Core 1821 (1813 + 8), Framework 331, solution 3013 with 0 failed and 3 skipped. Build: `0 Warning(s)`, `0 Error(s)`.
- Guards: `git diff --stat -- src/BrutalUi.Core/Elements/` is empty. `wc -l` reads each `ElementBuilder*.cs` under 300. `grep -c 'public static'` reads 6 methods in `ElementBuilder.cs`, 9 in `ElementBuilder.Text.cs`, 8 in `ElementBuilder.Widgets.cs`. `git status --porcelain` shows no change outside the six files of this group and the files of phase 1.
- Open questions: none. Deviation from the brief: none. The API shape followed the Public API of the plan exactly.
- Next session: run phase 2 group 2 (tasks 2.7 and 2.8): `BuilderTreeTests` (2), the section 11 and line 301 doc updates, plants 3 and 4, the phase demo run.

### 2026-11-12 08:05 (phase 2 group 2)

- Ran tasks 2.7 and 2.8. Wrote `Builder/BuilderTreeTests.cs` (2 tests, 88 lines): the eight-type fluent tree equals the initializer tree through `TreeCompare.FirstDifference`, and `ValueBeforeRangeClampsToTheOldRange` pins the order rule of task 2.3. Both tests passed on the first run.
- Docs: `docs/technical-design.md` line 301 names `Pickable(bool)` and `Focusable(bool)` next to the four older methods, with the sentence "Section 11 names the full list." Section 11 gains the method list after the example: the element group, the text group, and the widget values, in the format of the Public API of the plan. No source file changed in this group.
- Counts: Core 1823 (1813 + 10 from phase 1, as the brief expects), Framework 331, solution 3015 with 0 failed and 3 skipped. Build: `0 Warning(s)`, `0 Error(s)`. Demo: `--filter-class '*BuilderTreeTests'` reads 2 with `failed: 0`.
- Plant 3 (`Range` calls `SetRange(minimum, maximum, 0f)`): exit 2, `error CS` 0, 2 failures — the named `RangeCallsSetRangeAndThrowsForABadRange` and `AnEightTypeTreeFromTheFluentMethodsEqualsTheInitializerTree`. Restore `cmp` clean; `BuilderTreeTests` + `ElementBuilderWidgetTests` read 8 with 0 failed.
- Plant 4 (`Value` returns the slider and writes nothing): exit 2, `error CS` 0, 3 failures — the named `ValueAppliesTheValueRuleAndCallsNoDelegate` and `AnEightTypeTreeFromTheFluentMethodsEqualsTheInitializerTree`, plus `ValueBeforeRangeClampsToTheOldRange` (see the decision above). Restore `cmp` clean; the two classes again read 8 with 0 failed.
- One final solution run hit a test-host `Fatal error. Internal CLR error. (0x80131506)` (the exit-134 case of the brief). The second run read `total: 3015, failed: 0, skipped: 3`.
- Guards: `wc -l` reads `ElementBuilder.cs` 115, `ElementBuilder.Text.cs` 143, `ElementBuilder.Widgets.cs` 143 — each under 300. `git diff --stat -- src/BrutalUi.Core/Elements/` is empty. `grep -cE '^    public static'` reads 8 methods in `ElementBuilder.Widgets.cs` after the plant restores.
- Open questions: none. Deviation from the brief: plant 4 failed one extra test (see the decision above). Tooling note: `/tmp` is not shared between shell calls in this session, so the plant logs sit under `src/BrutalUi.Core/obj/plantscratch/`.
- Next session: run phase 3 (tasks 3.1 to 3.6).

### 2026-11-12 08:32 (phase 3)

- Ran tasks 3.1 to 3.6. `Element.cs`: the `partial` modifier and one remarks sentence; `git diff` shows nothing else. New `Tree/Element.Find.cs` (139 lines): `Find`, `Find<T>`, the two private static walks, and `Matches` with the length fast reject and `Utf8Text.Utf8Equals`. New `Builder/ElementFindTests.cs` (7 tests) and `Builder/ElementFindAllocationTests.cs` (2 tests, tree of 218 named elements: 6 branches, 5 children each, 6 leaves each, plus root and the target label last in tree order).
- Docs: section 5 `Element` block gains `Find` and `Find<T>` with one comment each; section 11 gains the query bullet with the five rules of The Query; section 14 gains the twentieth allocation gate bullet with the plant results.
- Counts: Core 1832 (1823 + 9), Framework 331, solution 3024 with 0 failed and 3 skipped. Build: `0 Warning(s)`, `0 Error(s)`. Demo: `--filter-class '*ElementFind*'` reads 9 with `failed: 0`; the gate measures 0 bytes over 1000 rounds of a hit, a miss, and a typed hit.
- Risk 3 check: `--filter-class '*SlotContainer*'` reads 15 with `failed: 0`, and `--filter-class '*UiManager*'` reads 14 with `failed: 0`, both with no test edit.
- Plant 5 (`Walk` checks the children before the root): exit 2, `error CS` 0, one failure — `FindChecksTheRootFirst`. Restore `cmp` clean.
- Plant 6 (`GC.KeepAlive(new object());` as the first line of the untyped walk): exit 2, `error CS` 0, one failure — `FindAndFindTAllocateNothingAfterWarmUp` with 10464000 bytes (218 elements walked by the hit and the miss, 1000 rounds, 24 bytes per element). Restore `cmp` clean.
- Plant 7 (the typed walk returns `null` at the first name hit of another type): exit 2, `error CS` 0, one failure — `FindTSkipsAnElementOfAnotherType`. The one-line form of the plant does not stop the sibling loop, so the plant also checks `Matches` at the child level (see the decision above). Restore `cmp` clean.
- Guards: `git diff src/BrutalUi.Core/Tree/Element.cs` shows the modifier and the sentence only. `git diff --stat -- src/BrutalUi.Framework/Panels/` is empty. `git status --porcelain` shows no change outside the phase files and the files of phases 1 and 2.
- Open questions: none. Deviation from the brief: none, except the plant 7 form above, which the brief's expected failure forced. No test-host exit 134 hit this session; every run read its result on the first call.
- Next session: run phase 4 (tasks 4.1 to 4.8).

### 2026-11-12 08:45 (phase 4 group 1)

- Ran tasks 4.1 to 4.5. `ElementBuilder.Widgets.cs` gains `OnClicked`, `OnCheckedChanged`, `OnValueChanged`, `OnTextChanged`, `OnSubmitted`, and `OnScrollChanged` (248 lines); the file remarks now hold the name rule, the `+=` rule, and the build-time rule once. New `ElementBuilder.Events.cs` (287 lines) holds the 14 generic methods with `where T : Element`, the two `ThrowIfNull` checks, `slot += handler`, and `return element`; the file remarks hold the name rule, the `+=` rule, the side-object rule, the build-time rule, and the two phases.
- Tests: `ElementBuilderWidgetTests` gains `EachWidgetSlotMethodAddsTheHandlerWithPlusEquals` (each of the 6 slots: the `=` handler stays first, two builder calls, an invocation count of 3, the add order read back) and `AWidgetSlotMethodThrowsForANullReceiverOrANullHandler` (8 total). New `ElementBuilderEventTests.cs` (4): the per-slot check on a `Delegate?[]` of all 14 slots, the keep-before case (count 2, order pinned with `Assert.Same`), the 14×2 null checks (`element` and `handler`), and the routed press (handler runs 1 time, `DrainEvents` gives 1 walk).
- Task 4.5 record: the solution build reads `0 Warning(s)` and `0 Error(s)`; `button.OnClicked(h)` and `element.PointerDown(h)` compile in the tests; the name-rule grep of task 4.5 gives no output. Counts: `grep -cE '^    public static'` reads 6 + 9 + 14 + 14 = 43; each `ElementBuilder*.cs` file is under 300 lines (115, 143, 248, 287).
- Three new tests failed on the first run (the `TextField` capacity and the `Delegate` natural-type case; see the decisions). After the test fixes: `--filter-class '*ElementBuilderEventTests'` reads 4 with `failed: 0`, `--filter-class '*ElementBuilderWidgetTests'` reads 8 with `failed: 0`, and the Core suite reads 1838 with 0 failed. Solution: 3030, 0 failed, 3 skipped (the three GLFW display tests). No test-host exit 134 this session.
- Guards: `git status --porcelain` shows no change outside the files of phases 1 to 3 and the five files of this group. No existing test needed an edit; no production file outside the two builder files changed.
- Open questions: none. Deviation from the brief: none for group 1; tasks 4.6 to 4.8 (group 2) stay open: the Framework both-order test, the tree test with handlers, section 11 and `CHANGELOG.md`, plants 8 and 9, and the final full gate (expect Core 1839, Framework 332, solution 3032 with 3 skipped).
- Next session: run phase 4 group 2 (tasks 4.6 to 4.8).

---

### 2026-11-12 09:20 (phase 4 group 2, the final gate of the feature)

- Ran tasks 4.6 to 4.8. `CommandBuilderTests.cs` gains `CommandAndOnClickedKeepBothDelegatesInEachOrder` and the private `Click` helper. `BuilderTreeTests.cs` gains `ATreeWithHandlersFromTheFluentMethodsEqualsTheInitializerTree` and nine private static handlers. No production file changed in this group.
- Docs: section 11 gains the widget-delegate bullet, the event-group bullet, and the **The delegate name rule** paragraph (two sentences, plus the `Command` `+=` sentence). `CHANGELOG.md` gains one `(markup/builder-api)` line under `## [Unreleased]` → `### Added`.
- Counts, final: solution 3032 tests, 0 failed, 3 skipped (the three GLFW display tests), duration 5s. `BrutalUi.Core.Tests` 1839, 0 failed (1832 + 7). `BrutalUi.Framework.Tests` 332, 0 failed (331 + 1). The reading equals the plan baseline expectation (Core 1839, Framework 332, solution 3032 with 3 skipped).
- Demos: `*ElementBuilder*` 21 / failed 0, `*BuilderTreeTests*` 3 / failed 0, `*ElementFind*` 9 / failed 0, `*TreeCompareTests*` 5 / failed 0, `*ElementBuilderEventTests*` 4 / failed 0, Framework `*CommandBuilderTests*` 6 / failed 0.
- Plant 8 (`ElementBuilder.Widgets.cs`: `OnClicked` writes `button.Clicked = handler`): exit 2, `error CS` 0, two failures — `ElementBuilderWidgetTests.EachWidgetSlotMethodAddsTheHandlerWithPlusEquals` and `CommandBuilderTests.CommandAndOnClickedKeepBothDelegatesInEachOrder`, exactly the two named tests, with 3032 total and 3 skipped. Restore: `cmp` clean, then the two classes read 8 and 6 with 0 failed.
- Plant 9 (`ElementBuilder.Events.cs`: `KeyUpCapture` writes `element.OnKeyUp`): exit 2, `error CS` 0, one failure — `ElementBuilderEventTests.EachEventMethodAddsTheHandlerToItsSlot`, exactly the named test. Restore: `cmp` clean, then `*ElementBuilderEventTests` reads 4 and `*BuilderTreeTests` reads 3 with 0 failed. No test-host exit 134 in this session; every run read its result on the first call.
- Final gate grep readings (the Verification Method of the plan):
  1. `dotnet build brutal-ui.sln -nodeReuse:false`: `Build succeeded.`, `0 Warning(s)`, `0 Error(s)`.
  2. `dotnet test --solution brutal-ui.sln`: `total: 3032`, `failed: 0`, `succeeded: 3029`, `skipped: 3`.
  3. `grep -c 'public static' src/BrutalUi.Core/Tree/ElementBuilder*.cs`: 7 + 15 + 10 + 15 = 47, that is 43 methods plus the four `public static partial class ElementBuilder` lines. `grep -c 'public static.*\bthis\b'` confirms the methods: 6 + 14 + 9 + 14 = 43.
  4. `wc -l src/BrutalUi.Core/Tree/ElementBuilder*.cs`: 115, 287, 143, 248 — each under 300.
  5. `git diff 386793e -- src/BrutalUi.Core/Tree/ElementBuilder.cs | grep '^-[^-]'`: two lines only, the old remarks line and the old `public static class ElementBuilder` line.
  6. `git diff --stat 386793e -- src/BrutalUi.Markup src/BrutalUi.Core/Elements src/BrutalUi.Framework/Panels samples`: no output.
  7. `git diff 386793e -- src | grep -E '^\+.*public (sealed |static |abstract )*(partial )*(class|struct|interface|enum|record|delegate) '`: `+public partial class Element` and `+public static partial class ElementBuilder` only.
  8. The name-rule grep of task 4.5 over the four builder files: no output.
  9. The section 11 code block against the fluent expression of `TheSectionElevenExampleBuildsTheSameTreeAsInitializers`, after the removal of the 8-space method-body indent: `diff` reports no difference.
- Guards: `git status --porcelain` shows no change outside the files of phases 1 to 4. No existing test needed an edit in this group; the new Core tree test is the only test that touched the reference shape (it needed the two `Classes.Add` lines of its own reference, found on the first run).
- Open questions: none. Deviation from the brief: none, beyond the tool notes of this host — the plant check reads `error CS` 0 in place of the `0 Error(s)` line, because `dotnet test --solution` prints no build summary.
- Next: `/evaluate-feature markup/builder-api`, then `/update-feature` and `/update-master`.

### 2026-11-12 09:30 (review group 1: CR-1)

- Ran CR-1 from `code-review.md`. The three `TextOfA...SetsTheBytesOfEachOverload` tests in `Builder/ElementBuilderTextTests.cs` gain the null read-back after their `LString` case, on the live receiver: `Text((string?)null)`, read the empty text and the cleared `LocalizedText`, restore with the existing `LString` local, then `Text((LString?)null)` and read again. The `TextField` test reads the empty text only: `TextField` has no `LocalizedText` property, and its `SetText(LString?)` keeps no reference.
- Deviation from the reviewer's fix form: `Assert.Empty(span)` does not compile with xunit.v3 4.0.2 (`CS1503`: no `ReadOnlySpan<T>` overload). The tests use the house form of the element tests instead: `Assert.True(label.Text.IsEmpty)`, `Assert.True(button.Text.IsEmpty)`, `Assert.True(field.Text.IsEmpty)`. Same rule, same strength.
- Gate readings: build `Build succeeded.`, `0 Warning(s)`, `0 Error(s)`; `--filter-class '*ElementBuilderTextTests'` reads total 5, `failed: 0`, 5 succeeded. No test count changed, and no file under `src/` was touched by this change. The CR-1 box in `code-review.md` stays unticked; the parent ticks it.

### 2026-11-12 09:40 (review group 2: CR-2)

- Ran CR-2 from `code-review.md`. `NameWithNullClearsTheNameAndTheSameNameMarksNothing` in `Builder/ElementBuilderElementTests.cs` gains the empty-name read-back after the null clear: `root.Name("")`, then `Assert.Equal("", root.Name)`. The two lines of the fix form, no more; the "matches no name selector" half stays in `NameSelectorTests` and the `Find` miss test.
- Gate readings: build `Build succeeded.`, `0 Warning(s)`, `0 Error(s)`; `--filter-class '*ElementBuilderElementTests'` reads total 4, `failed: 0`. No test count moved, and no file under `src/` shows a diff. No span assert touched, so the `Assert.Empty` overload gap did not bind. The CR-2 box in `code-review.md` stays unticked; the parent ticks it.

### 2026-11-12 09:55 (review group 3: CR-5)

- Ran CR-5 from `code-review.md`. The `Submitted` block in `EachWidgetSlotMethodAddsTheHandlerWithPlusEquals` (`Builder/ElementBuilderWidgetTests.cs`) now captures the chain result into the typed local `sameField2` and asserts `ReferenceEquals(field, sameField2)`, on the fix form of the report. The test asserts the same instance for all six widget delegate methods. No production file changed.
- Gate readings: build `Build succeeded.`, `0 Warning(s)`, `0 Error(s)`; `--filter-class '*ElementBuilderWidgetTests'` reads total 8, `failed: 0`, 8 succeeded. No test count moved. `find src -name '*.cs' -mmin -15` reads no file, so this group touched no file under `src/`; the `src/` lines in `git status` are the uncommitted feature build of phases 1 to 4. The CR-5 box in `code-review.md` stays unticked; the parent ticks it. No test-host exit 134 this session; both runs read their result on the first call.

### 2026-11-12 10:05 (review group 4: CR-4)

- Ran CR-4 from `code-review.md`. The third `Find<T>` remarks paragraph in `src/BrutalUi.Core/Tree/Element.Find.cs` (lines 54-56) now reads the two fix sentences: "An element with the name but another type does not match, and the walk does not stop there. The method gives the first element that has the name and the type `<typeparamref name="T"/>`." Comment lines only; no code line and no other remarks changed.
- Gate readings: build `Build succeeded.`, `0 Warning(s)`, `0 Error(s)` (a bad `typeparamref` would fail it); `--filter-class '*ElementFind*'` reads total 9, `failed: 0`. No test count moved. `git diff` shows nothing for the file because `Element.Find.cs` is untracked (the phase 3 build is not committed); the check read the file lines instead, and only the two comment lines differ.
- Prose lint: this repo holds no `scripts/ste-lint.py` or any lint script under `scripts/`. The only artifact is the stale `artifacts/ste-lint.txt` from an older feature. The prose-lint step of the brief has no script to run, so this group ran none.
- Tooling: `dotnet test <project>` positional form fails on this SDK (10.0.115: "should be via '--project'"), and the `--project` form then died on MSBuild project evaluation; the direct test binary (`tests/BrutalUi.Core.Tests/bin/Debug/net10.0/BrutalUi.Core.Tests --filter-class '*ElementFind*'`) read the result after a fresh rebuild. The CR-4 box in `code-review.md` stays unticked; the parent ticks it.

### 2026-11-12 10:20 (review group 5: CR-3 and CR-6)

- Ran CR-3 and CR-6 from `code-review.md`. Both are doc lines; no code and no test changed.
- CR-3: `docs/technical-design.md:184` now reads `public partial class Element`, one word added, no re-flow. The two `Find` lines at 200-201 and line 301 stay as phase 3 and phase 2 left them. The `git diff` of the file shows this one new change line; the other hunks are the uncommitted doc edits of phases 1 to 4.
- CR-6: added one hand-off line to `docs/features/markup/xml-element-loader/requirements.md` under `## Out of Scope`, in the fix form of the report: `Capacity` of `TextField` and `ScrollSpeed` of `ScrollView` exist only in the create info (A6), have no builder method and no public setter, so they have no table row, and a template attribute for either needs a decision first. The rest of the file is unchanged. The loader feature is not planned yet, so this group wrote no plan there.
- Gate readings: build `Build succeeded.`, `0 Warning(s)`, `0 Error(s)` (9.9s). A doc-only change touches no file that the tests read, so no test run. Prose lint: this repo holds no `ste-lint` script (carried from the group 3 and group 4 notes), so this group ran none.
- The CR-3 and CR-6 boxes in `code-review.md` stay unticked; the parent ticks them.

### 2026-11-12 10:38 (autorun close: review gate, docs, hand-off)

- The post-review gate ran on all 15 items of the review verification brief and returned PASS, with Demo: PASS. Readings: build `0 Warning(s)` and `0 Error(s)`; solution total 3032, `failed: 0`, `skipped: 3`; `*ElementBuilder*` 21, `*BuilderTreeTests*` 3, `*ElementFind*` 9, `*TreeCompareTests*` 5, `*CommandBuilderTests*` 6, each `failed: 0`; `public static` 47 lines = 43 methods plus 4 class lines; the two shipped-surface greps, the untouched-area diff, the new-public-type grep, and the slot-name grep all read as the plan states.
- The review stands at Resolved: 6 findings, 6 fixed. CR-1, CR-2, and CR-5 added asserts inside existing tests, so no test count moved. CR-4 reworded the `Find<T>` remarks. CR-3 changed one doc line, and CR-6 added one hand-off line to the loader requirements.
- Inbox: the file holds no item line. No user message reached the run, so nothing to fold and no bug to promote.
- Docs: `implementation.md` now reads `Status: Ready for Review`; `tasks.md` reads 33/33 (100%); this file carries the four demo lines under "Needs Human Verification".
- Hand-off to the epic: the property surface, `TreeCompare` with its `BrutalUi.Markup.Tests` link line, the `Range` before `Value` order, `Command` and `BindCommand` sharing one body, `Find` for panel code, and the CR-6 note that `Capacity` and `ScrollSpeed` have no table row.
- Next session: the person tests the four demos, then commits, pushes, and opens the PR. `/autorun-feature` made no commit and no push.

---

*Update this file at the end of each work session. Run `/update-feature` before compacting conversations.*
