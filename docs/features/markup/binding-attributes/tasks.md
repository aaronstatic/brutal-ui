# markup/binding-attributes - Task Checklist

**Epic:** markup (feature 4 of 6)
**Last Updated:** 2026-11-17 07:30
**Progress:** 18/18 tasks complete (100%) — all three phases built, gate-verified, and reviewed

Agent tiers: Phase 1 ADVANCED (opus), Phase 2 STANDARD (sonnet), Phase 3 STANDARD (sonnet).

---

## Phase 1: The Section 11 Demo (6/6 complete) — ADVANCED

**Demo:** the developer runs `BrutalUi.Framework.Tests --filter-class '*TemplateBindingDemoTests'` and reads a total of 4 with `failed: 0`. In `TheSectionElevenTemplateRunsTheCommandAndShowsTheStore`, a click on `pause` runs `game.pause` one time, and a store write of the score 1500 changes the `score` label to `1,500`.
**Needs:** none

**Group 1 (6 tasks, 6 files: `context.md`, `BrutalUi.Framework.csproj`, `TemplateBindingEntry.cs`, `TemplateBindings.cs`, `TemplateBindingDemoTests.cs`, `technical-design.md`):**

- [x] 1.1 Create `docs/features/markup/binding-attributes/context.md` when `/start-feature` did not. Run `git status --porcelain`. Record the base commit (`git rev-parse HEAD`, `5bbeec9` at planning time), the solution total with the skipped count (3143 with 3 skipped at planning time), and the total of `BrutalUi.Framework.Tests`. Record the Public API, The Entry Shape, The Handler, The Rules of the Object, and the Notes of this plan. Record the tool notes: `DOTNET_CLI_HOME`, the per-class binary, and the `error CS` check of a plant.
- [x] 1.2 Edit `src/BrutalUi.Framework/BrutalUi.Framework.csproj`. After the Core line, add `<ProjectReference Include="../BrutalUi.Markup/BrutalUi.Markup.csproj" />`. Build, and expect `0 Error(s)`. Run `dotnet msbuild src/BrutalUi.Framework/BrutalUi.Framework.csproj -getItem:Analyzer 2>&1 | grep -c 'BrutalUi.Generators'` and expect `0`: the generator does not flow through the project reference. Run `dotnet pack brutal-ui.sln --no-build --configuration Debug 2>&1 | tail -n 5`, then `unzip -p artifacts/packages/BrutalUi.Framework.0.1.0-alpha.nupkg BrutalUi.Framework.nuspec | grep -n 'BrutalUi.Markup'`, and expect one `dependency` line. Record the line and its `exclude` attribute in `context.md` (Notes item 7).
- [x] 1.3 Write `src/BrutalUi.Framework/Templates/TemplateBindingEntry.cs` with `internal abstract class TemplateBindingEntry` and its four virtual `TryBind` overloads (`Label`, `TextField`, `Toggle`, `Slider`), each `false` in the base class. Add `internal sealed class NumberTextEntry<T, TValue> where TValue : IUtf8SpanFormattable`. It holds the store, the selector, the format, and the provider in `readonly` fields, in the field style of `CommandBindings.ButtonCommand`, and overrides `TryBind(Label)` with `label.BindText(store, selector, format, provider)` and `return true`. The class remarks state the entry rule of The Entry Shape. The other three entries come in phase 2, and `SelectorKind` comes in phase 3.
- [x] 1.4 Write `src/BrutalUi.Framework/Templates/TemplateBindings.cs` with `public sealed class TemplateBindings`: the constructor (`ArgumentNullException` for a `null` registry), a `Dictionary<string, TemplateBindingEntry>` with `StringComparer.Ordinal`, `AddText<T, TValue>` (`ArgumentNullException.ThrowIfNull` for the key, the store, and the selector, then `_entries.Add`), `AttachTo` (`ArgumentNullException` for a `null` loader, then `loader.AddAttributeHandler(Handle)`), and the private `Handle`. In this phase, `Handle` binds `command` on a `Button` with a value that is not empty, and `bind-text` on a `Label` with a known key whose `TryBind` gives `true`. Each other case of the four names returns `false`, and another name returns `false`. The class remarks state the capture, the load-time rule, the one-set rule, and the rule for an add after `AttachTo`.
- [x] 1.5 Write `tests/BrutalUi.Framework.Tests/Templates/TemplateBindingDemoTests.cs` with 4 tests. `TheSectionElevenTemplateRunsTheCommandAndShowsTheStore` (the template of the new section 11 block of task 1.6, word for word with LF line ends. The test registers `game.pause` with a counter, adds `AddText("hud.score", store, static s => s.Score, "N0", CultureInfo.InvariantCulture)` on a `Store<BoundTree.HudState>`, attaches, loads, and adds the root to a context. After the first `Update`, the label shows `0`. A press and a release at the center of `pause` give one call. A `Set` with the score 1500 and one `Update` give the label text `1,500`). `TheFormatAndTheProviderComeFromTheRegisterCall` (the same key with `"N0"` and `de-DE` gives `1.500`). `ACommandThatTheGameRegistersAfterTheLoadRuns` (the load runs with no `game.pause` in the registry, the test registers it after the load, and a click runs it, A1). `AnAddAfterAttachToServesTheNextLoad` (`AttachTo` runs first, `AddText` second, and the load binds the label).
- [x] 1.6 Edit `docs/technical-design.md`. In section 3, change the Framework line to: it references Core and Markup. In section 11, after **The attribute hook.**, add a **The binding attributes.** block: the section 11 template with `bind-text="hud.score"` on the `score` label, the C# code of the demo (`new TemplateBindings(commands)`, `AddText`, `AttachTo`, `Load`), and the rules of phase 1. Change the last sentence of **The attribute hook.** and the sentence after the first template to name `TemplateBindings.AttachTo`. Keep the first template of section 11 unchanged (Notes item 2). Run the build and the tests, and expect Framework +4. Run two plants. Plant 1: the `command` branch returns `true` with no `BindCommand` call. Expect `TheSectionElevenTemplateRunsTheCommandAndShowsTheStore` and `ACommandThatTheGameRegistersAfterTheLoadRuns` to fail. Plant 2: `NumberTextEntry.TryBind(Label)` passes `null` for the format. Expect `TheFormatAndTheProviderComeFromTheRegisterCall` and the demo test to fail. Record the counts, the plants, and the demo state in `context.md`.

**Estimated Steps:** about 55 (6 tasks, about nine steps for one task)

**Acceptance (Phase 1):**
- [x] The 4 new Framework tests pass, and each existing test passes with no edit.
- [x] `src/BrutalUi.Framework/BrutalUi.Framework.csproj` holds one `ProjectReference` to `BrutalUi.Markup.csproj`, and `src/BrutalUi.Markup/BrutalUi.Markup.csproj` does not change.
- [x] The Analyzer item list of Framework holds no `BrutalUi.Generators` entry, and the packed Framework nuspec holds one dependency on `BrutalUi.Markup`.
- [x] Plants 1 and 2 each give a non-zero exit code with 0 `error CS` lines, and each restored file equals its copy under `cmp`.
- [x] The template of **The binding attributes.** equals the template text of `TheSectionElevenTemplateRunsTheCommandAndShowsTheStore`, character for character.

---

## Phase 2: Every Widget Binding, Equal to Code (6/6 complete) — STANDARD

**Demo:** the developer runs `BrutalUi.Framework.Tests --filter-class '*TemplateParityTests'` and reads a total of 2 with `failed: 0`. The template tree and the C# tree show the same text, checked state, and value after each of 10 store versions.
**Needs:** Phase 1

**Group 1 (the code and the widget tests, 4 files: `TemplateBindingEntry.cs`, `TemplateBindings.cs`, `TemplateWidgetBindingTests.cs`, `context.md`):**

- [x] 2.1 Edit `src/BrutalUi.Framework/Templates/TemplateBindingEntry.cs`. Add `StringTextEntry<T>` (overrides `TryBind(Label)` with the string `BindText` and `TryBind(TextField)` with `field.BindText`), `CheckedEntry<T>` (overrides `TryBind(Toggle)` with `BindChecked`), and `ValueEntry<T>` (overrides `TryBind(Slider)` with `BindValue`), each with the field style of task 1.3.
- [x] 2.2 Edit `src/BrutalUi.Framework/Templates/TemplateBindings.cs`. Add `AddText<T>(string, Store<T>, Func<T, string?>)`, `AddChecked<T>`, and `AddValue<T>` with the checks of task 1.4. In `Handle`, bind `bind-text` on a `TextField`, `bind-checked` on a `Toggle`, and `bind-value` on a `Slider` through `TryBind`. Each fault case still returns `false`. Confirm that `AddText("k", store, static s => s.Name)` picks the string overload and `static s => s.Score` picks the number overload, and record the result in `context.md` (Risk 3).
- [x] 2.3 Write `tests/BrutalUi.Framework.Tests/Templates/TemplateWidgetBindingTests.cs` with 4 tests. `AStringKeyBindsALabel` (a `Name` key shows the name after the first `Update`, and a new name after a `Set`). `OneStringKeyServesALabelAndATextField` (one `hud.name` key on a label and a field: both show the name, and each holds one binding, A5). `ACheckedKeyBindsAToggle` (a `Muted` key: a `Set` flips `Checked`, and `CheckedChanged` gets no call). `AValueKeyBindsASlider` (a `Volume` key on `<ui:Slider max="100" .../>`: a `Set` writes `Value`, and `ValueChanged` gets no call). Run the build and the tests, and expect Framework +4. Run plant 3: `StringTextEntry.TryBind(TextField)` returns `false`. Expect `OneStringKeyServesALabelAndATextField` to fail. Record the plant in `context.md`.

**Group 2 (the parity, the gate, and the docs, 5 files: `BoundTemplate.cs`, `TemplateParityTests.cs`, `TemplateBindingAllocationTests.cs`, `technical-design.md`, `context.md`):**

- [x] 2.4 Write `tests/BrutalUi.Framework.Tests/Shared/BoundTemplate.cs` as `internal static class BoundTemplate`. It holds the template text (a root `hud`, the labels `score` (`hud.score`, `N0`, invariant), `volume` (`hud.volume-text`, `F1`, invariant), and `player` (`hud.name`), the field `name-field` (`capacity="16"`, `hud.name`), the toggle `mute` (`hud.muted`), the slider `volume-slider` (`max="100"`, `hud.volume`), and the button `pause` (`game.pause`)). `Load(Store<BoundTree.HudState>, CommandRegistry)` makes the bindings, attaches them to a new loader, and returns the root. `BuildCode(Store<BoundTree.HudState>, CommandRegistry)` builds the same tree with the builder methods, the same create values as the factories of `BuiltInElements.cs`, and the C# bind methods. Then write `tests/BrutalUi.Framework.Tests/Templates/TemplateParityTests.cs` with 2 tests. `TheTemplateTreeEqualsTheCodeTreeAfterEachStoreVersion` (two contexts, one for each tree. For rounds 1 to 10, `Set(BoundTree.Next(round))`, `Update` both, and assert `Assert.Null(TreeCompare.FirstDifference(code, loaded))` and the same `BindingCount` on each bound element. `TreeCompare` does not compare `BindingCount`). `AClickOnEachTreeRunsTheCommandOneTime` (one click on each `pause` gives two calls of one counter).
- [x] 2.5 Write `tests/BrutalUi.Framework.Tests/Templates/TemplateBindingAllocationTests.cs` with 3 tests, in the pattern of `BoundTreeAllocationTests` and with `AllocationGate.ReleaseAllocationContext()` before each measure. The scene loads `BoundTemplate` into a context with a `RecordingRenderer`, caches the center of `pause` after the first `Update`, and builds the two pointer events. `UpdateAndDrawOnALoadedBoundTreeAllocateNothing` (each frame sets `BoundTree.Next(round)`, then runs `Update(0.016f)` and `Draw`: 2 warm-up rounds of 9 frames, then 100 measured frames, 0 bytes). `AClickOnALoadedCommandButtonAllocatesNothing` (each frame pushes a press and a release at `pause`, then runs `Update` and `Draw`, 0 bytes). `TheMeasuredBodyDoesRealWork` (after one changing frame, each bound element shows the state of the round. After one click frame, the counter grows by 1).
- [x] 2.6 Edit `docs/technical-design.md`. In section 11, extend **The binding attributes.** with the table of the four attributes (attribute, element types, `Add*` method, bind method), The Entry Shape in prose, and the rules of the object except the errors. In section 10, **Data binding**, add one sentence: a template binds through `TemplateBindings` (section 11) with the same bind methods, and `TemplateParityTests` guards the parity. In section 14, after the twenty-first gate, add **A twenty-second allocation gate.** for `TemplateBindingAllocationTests` with its plant. Run the build and the tests, and expect Framework +9 in this phase. Run two plants. Plant 4: `CheckedEntry.TryBind(Toggle)` binds `s => !selector(s)`. Expect `ACheckedKeyBindsAToggle` and `TheTemplateTreeEqualsTheCodeTreeAfterEachStoreVersion` to fail. Plant 5: `GC.KeepAlive(new object());` as the first line of `WidgetBindings.ValueHandler<T>.Run`. Expect `UpdateAndDrawOnALoadedBoundTreeAllocateNothing` to fail (the gates of `BoundTreeAllocationTests` fail with it). Record the counts and the plants in `context.md`.

**Estimated Steps:** about 50 (6 tasks, about eight steps for one task)

**Acceptance (Phase 2):**
- [x] The 9 new Framework tests pass, and each existing test passes with no edit.
- [x] `UpdateAndDrawOnALoadedBoundTreeAllocateNothing` and `AClickOnALoadedCommandButtonAllocatesNothing` each measure 0 bytes over 100 frames.
- [x] Plants 3, 4, and 5 each give a non-zero exit code with 0 `error CS` lines, and each restored file equals its copy under `cmp`.

---

## Phase 3: The Positioned Binding Errors (6/6 complete) — STANDARD

**Demo:** the developer runs `BrutalUi.Framework.Tests --filter-class '*TemplateBindingErrorTests'` and reads a total of 6 with `failed: 0`. A template with `bind-text="hud.nme"` gives one `UnknownBinding` error at the line and the column of `bind-text`, and no root.
**Needs:** Phase 1, Phase 2

**Group 1 (the kinds, the handler, and the error tests, 5 files: `TemplateError.cs`, `TemplateBindingEntry.cs`, `TemplateBindings.cs`, `TemplateBindingErrorTests.cs`, `context.md`):**

- [x] 3.1 Edit `src/BrutalUi.Markup/Template/TemplateError.cs`. Add `UnknownBinding` and `BindingTypeMismatch` after `UnexpectedChild`, each with a doc line that names its cause. Add the two arms to `GetKindName` (Notes item 1). Run `grep -rn 'TemplateErrorKind' src tests --include=*.cs | grep -v 'TemplateErrorKind\.'` and confirm that no code counts the members or switches on the kind with no default. Build, and expect `0 Error(s)` and each Markup test green.
- [x] 3.2 Edit `src/BrutalUi.Framework/Templates/TemplateBindingEntry.cs`. Add `public abstract string SelectorKind { get; }` to the base class and its value to each entry: `a string`, `a number`, `a bool`, `a float`.
- [x] 3.3 Edit `src/BrutalUi.Framework/Templates/TemplateBindings.cs`. Write `Handle` in the order of The Handler. Each of the four names now returns `true`. The messages: `'command' is not an attribute of this element. Write command on a Button only.` (and `on a Label or a TextField only`, `on a Toggle only`, `on a Slider only`). `'command' needs a command name. Write the name of a registered command, for example 'game.pause'.` and `'bind-text' needs a key. Write a key that the panel code registers, for example 'hud.score'.` `'hud.nme' is not a bound key. Register it with TemplateBindings.AddText before the load.` (with `AddChecked` or `AddValue` for the other attributes). `'hud.muted' holds a bool selector. bind-text on a Label needs a string or a number selector. Register the key with AddText.` (a `TextField` needs a string selector, a `Toggle` a bool selector with `AddChecked`, a `Slider` a float selector with `AddValue`).
- [x] 3.4 Write `tests/BrutalUi.Framework.Tests/Templates/TemplateBindingErrorTests.cs` with 6 tests. `AnUnknownKeyIsAnUnknownBindingError` (one template with an unknown key on each bind attribute gives 3 errors in source order, each `UnknownBinding` with its line and column, and no root). `ASelectorOfTheWrongTypeIsABindingTypeMismatchError` (a bool key on a `bind-text` label, a number key on a `bind-text` field (A5), a string key on `bind-value`, and a float key on `bind-checked` give 4 errors of `BindingTypeMismatch`). `AnAttributeOnTheWrongElementIsAnUnknownAttributeError` (`command` on a `Label`, `bind-text` on a `Button`, `bind-checked` on a `Slider`, and `bind-value` on a `Toggle`, each with a key that no `Add*` call registered, give 4 errors of `UnknownAttribute`, so the element check comes before the key check). `AnEmptyValueIsAnInvalidValueError` (`command=""` and `bind-text=""` give 2 errors of `InvalidValue`, A2). `ALoaderWithNoBindingsRejectsEachAttribute` (a loader with no `AttachTo` gives `UnknownAttribute` for each of the four names, A6). `TheErrorTextNamesTheNewKinds` (`ToString` of each new kind contains `UnknownBinding: ` or `BindingTypeMismatch: `). Compute each column from the template text, as `AttributeHookTests.AssertAttribute` does. Run the build and the tests, and expect Framework +6. Run plants 6 and 7. Plant 6: the `bind-text` branch checks the key before the element type. Expect `AnAttributeOnTheWrongElementIsAnUnknownAttributeError` to fail. Plant 7: `GetKindName` loses the `UnknownBinding` arm. Expect `TheErrorTextNamesTheNewKinds` to fail. Record the plants in `context.md`.

**Group 2 (the argument checks, the API tests, and the docs, 5 files: `TemplateBindings.cs`, `TemplateBindingsApiTests.cs`, `technical-design.md`, `CHANGELOG.md`, `context.md`):**

- [x] 3.5 Edit `src/BrutalUi.Framework/Templates/TemplateBindings.cs`. Each `Add*` method throws `ArgumentException` for an empty key and, before the add, for a key that the table holds, with a constant message that names the rule (A4). `AttachTo` keeps a `List<TemplateLoader>` and throws `ArgumentException` (parameter `loader`) for a loader that it holds, before it adds a handler. Then write `tests/BrutalUi.Framework.Tests/Templates/TemplateBindingsApiTests.cs` with 4 tests. `ASecondAddForOneKeyThrows` (`AddText` then `AddText`, and `AddText` then `AddChecked`, on one key each throw `ArgumentException`, and the first selector still binds). `TheMethodsCheckTheirArguments` (a `null` registry, a `null` loader, and a `null` key, store, or selector throw `ArgumentNullException` with the parameter name. An empty key throws `ArgumentException`). `ASecondAttachToOnOneLoaderThrows` (the second call throws, and a load still binds one time: the label holds one binding). `TheHandlerPassesAnotherAttributeToTheNextHandler` (a game handler added after `AttachTo` takes `probe`, and the load succeeds).
- [x] 3.6 Edit `docs/technical-design.md`. In section 11, add the two members to the `TemplateErrorKind` block, add the two rows to the table of **The error report.**, and add the order of checks, the four messages, and the rules of A4 and `AttachTo` to **The binding attributes.** In section 10, **The button bind rules**, add one line: the `command` attribute calls `BindCommand` (section 11). Add one line to `CHANGELOG.md` under `### Added`, tagged `(markup/binding-attributes)`. Run the build and the tests, and expect Framework +10 in this phase and +23 in total. Run plant 8: `AttachTo` skips the check of its loader list. Expect `ASecondAttachToOnOneLoaderThrows` to fail. Run `pack`, `package-restore`, and `package` of `scripts/test-linux.sh` by hand, and expect `package-test: PASS Gauge level 0.5`. Record the counts, the plants, and the close state in `context.md`.

**Estimated Steps:** about 50 (6 tasks, about eight steps for one task)

**Acceptance (Phase 3):**
- [x] The 10 new Framework tests pass, and each existing test passes with no edit, also in `BrutalUi.Markup.Tests`.
- [x] `git diff 5bbeec9 -- src/BrutalUi.Markup` touches `Template/TemplateError.cs` only.
- [x] Plants 6, 7, and 8 each give a non-zero exit code with 0 `error CS` lines, and each restored file equals its copy under `cmp`.
- [x] The package step prints `package-test: PASS Gauge level 0.5`.

---

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

### Completed in the 2026-11-17 autorun
- Phase 1 (6 tasks, ADVANCED), Phase 2 (6 tasks, 2 groups, STANDARD), Phase 3 (6 tasks, 2 groups, STANDARD): all 18 built and gate-verified by independent verifier agents.
- Cross-phase review: 10 findings, all fixed in five groups. Plants 1 to 11 each fail their named test.
- Final counts: solution 3167 / failed 0 / skipped 3; Framework.Tests 356 (+24 over the 332 baseline); Markup.Tests 666; build `0 Warning(s)` / `0 Error(s)`; `package-test: PASS Gauge level 0.5`.
- `code-review.md` reads `Resolved - 10 findings, 10 resolved`. `inbox.md` holds two closed bug lines.
- No commit and no push. The branch `feat/markup/binding-attributes` is the human's to ship.

- The plan (`implementation.md`) holds the Public API, The Entry Shape, The Handler, The Rules of the Object, the commands, the plant method, the tests style, the Verification Method, and the Notes. Read it per phase.
- The base commit for diffs is `5bbeec9` (element-generator close). Task 1.1 records the real baseline.
- `/tmp` is not shared between the tool calls of one agent: write plant logs inside the workspace (xml-element-loader note).
