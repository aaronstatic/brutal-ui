# Binding Attributes - Implementation Plan

**Epic:** markup (feature 4 of 6)
**Status:** Ready for Review
**Started:** 2026-11-17
**Target Completion:** 2026-11-17
**Last Updated:** 2026-11-17 07:30

---

## Quality Bar

**Target quality:** A public library API that connects an XML template to the Framework layer. definition-loader and markup-sample build on it, and a game calls it in each panel that loads a template. Four things carry the feature.

- **Reliability:** `Load` never throws for the text of a template with these attributes. Each `Add*` call and `AttachTo` check their arguments, so a bad value fails at the register call, not in the load. The handler calls the existing bind methods only, with no reflection.
- **Exact errors with positions:** Each fault of a binding attribute gives one `TemplateError` with the right kind, the line and the column of the attribute name, and a message that quotes the value and names the fix. Each of the four fault cases (A2, A3, `UnknownBinding`, `BindingTypeMismatch`) has a test that asserts the kind, the line, and the column.
- **Parity with the C# bind methods:** A template element and a C# element bind through the same method, the same handler, and the same target type. `TemplateParityTests` loads a tree and builds the same tree in C#, and both trees show the same values after each store version and after a click.
- **Zero allocation in `Update`:** A loaded tree with each attribute allocates 0 bytes in `Update` and `Draw` after warm-up. `TemplateBindingAllocationTests` measures it, and a plant fails the gate.

Each public member has an XML doc comment in ASD-STE100 prose (CS1591 is an error). Each new source file stays under 400 lines. Each phase plants a fault and proves that a named test fails.

---

## Executive Summary

This feature adds the binding attributes of a template to `BrutalUi.Framework`. Framework gets a project reference to `BrutalUi.Markup` (epic Q1). Markup stays Core only.

- A new public class `TemplateBindings` holds the `CommandRegistry` of the game and a table of typed selectors by string key. The panel code adds each selector with `AddText`, `AddChecked`, or `AddValue`, and then calls `AttachTo(loader)`.
- `AttachTo` adds one handler to the attribute hook of the `TemplateLoader`. The handler captures the `TemplateBindings` object, so the hook contract of Markup does not change (A7). This settles open point 9 of xml-element-loader.
- `command="name"` on a `Button` calls `BindCommand`. `bind-text="key"` on a `Label` or a `TextField`, `bind-checked="key"` on a `Toggle`, and `bind-value="key"` on a `Slider` call the matching bind method of `LabelBindings` or `WidgetBindings` with the selector of the key (Q1).
- The format and the provider of a number label come from the register call (Q2). The template holds no format.
- `TemplateErrorKind` gets `UnknownBinding` and `BindingTypeMismatch` at the end (Q3). That is the only Markup edit.

The plan has 3 phases and 18 tasks, about 155 steps. Phase 1 (6 tasks) loads the section 11 template with `command` and one `bind-text` label (Q4). Phase 2 (6 tasks) adds the string text, the checked state, and the slider value, the parity test, and the 0-byte gate. Phase 3 (6 tasks) adds the two error kinds, the four fault cases, the argument checks, and the changelog line. Phase 1 needs an advanced dev agent, because it fixes the public API and the new package edge. Phases 2 and 3 are STANDARD.

---

## User Personas Alignment

No `docs/user-personas.md` file exists. The personas come from `docs/mission-statement.md` and the epic docs.

**Target Users:**
- [ ] **The markup author** writes `command` and `bind-*` attributes in a template and gets an error with a line, a column, and a fix for each bad key.
- [ ] **The game developer** registers a typed selector for each key in panel code, attaches the bindings to a loader, and gets a bound tree with no allocation in a frame.
- [ ] **The framework contributor** of definition-loader and markup-sample loads a panel template through a loader with attached bindings.

**User Subsets Served:**
- [ ] **Simple** - a HUD with one number label and one pause button: one `AddText`, one registered command, one `AttachTo`, one `Load`.
- [ ] **Complex** - a settings panel with a string label, a text field and a label on one key, a number label with a format and a culture, a toggle, a slider, and two commands, plus a game handler for its own attribute on the same loader.

**UX Approach:**
- Load time: the handler binds each element during the load and allocates. Each fault gives an error and no root.
- Run time: a bound element from a template is the same as an element that the C# bind methods bind.
- No expression language, no two-way syntax, no reflection, no format in the template.

---

## Goals

### Primary Goals
1. Add the reference from Framework to Markup and keep Markup Core only (requirements line 12).
2. Bind `command` on a `Button` through `BindCommand` with the registry of the bindings (requirements line 13).
3. Bind `bind-text`, `bind-checked`, and `bind-value` through a typed selector that the panel code registers for each key (requirements lines 14 and 15, Q1).
4. Report each binding fault with a kind, a line, and a column (requirements line 16, Q3, A2, A3).
5. Prove parity with the C# bind methods and the 0-byte rule in `Update` (requirements lines 17 and 18).
6. Document the attributes in sections 10 and 11 of `technical-design.md` (requirements line 19).

### Success Criteria
- [ ] The phase 1 demo passes: `TemplateBindingDemoTests` reads a total of 4 with `failed: 0`, and `TheSectionElevenTemplateRunsTheCommandAndShowsTheStore` is one of them.
- [ ] The phase 2 demo passes: `TemplateParityTests` reads 2 and `TemplateBindingAllocationTests` reads 3, each with `failed: 0`.
- [ ] The phase 3 demo passes: `TemplateBindingErrorTests` reads 6 with `failed: 0`.
- [ ] Each existing test passes with no edit. The planned delta is Framework +23, so the solution grows from 3143 to 3166 with 3 skipped. Task 1.1 records the real baseline, and the deltas count from it.

---

## Architecture Overview

### The Place in the Epic

binding-attributes is feature 4 of 6. It uses the hook of xml-element-loader (`AddAttributeHandler`, `AttributeHandler`, `AttributeContext.AddError`, `TemplateError`) with no change to the contract. element-generator left that contract unchanged. builder-api gives `Find<T>` and `TreeCompare`. state-and-commands gives `BindCommand`, `Store<T>`, `LabelBindings`, and `WidgetBindings`.

```
BrutalUi.Framework (references BrutalUi.Core and, new, BrutalUi.Markup)
  Templates/                      (new folder, namespace BrutalUi.Framework)
    TemplateBindings.cs           public: the constructor, AddText (2 overloads), AddChecked, AddValue, AttachTo
    TemplateBindingEntry.cs       internal: the abstract entry and 4 sealed generic entries
        |
        +--> BrutalUi.Markup: TemplateLoader.AddAttributeHandler, AttributeContext.AddError, TemplateErrorKind
        +--> BrutalUi.Framework: CommandBindings.BindCommand, LabelBindings.BindText, WidgetBindings.Bind*
        +--> BrutalUi.Core: Element, Label, TextField, Toggle, Slider, Button

BrutalUi.Markup (references BrutalUi.Core only, unchanged but for 2 enum values)
  Template/TemplateError.cs       TemplateErrorKind + UnknownBinding, BindingTypeMismatch (and 2 GetKindName arms)
```

**What definition-loader inherits.** A panel template loads through a `TemplateLoader` with one attached `TemplateBindings`. Each `Load` call binds new elements to the same stores, so a panel factory can load its template for each instance. definition-loader can put its record loader in `src/BrutalUi.Framework/Templates/`. The reference from Framework to Markup exists. An error of a binding is a `TemplateError` with the line and the column of the attribute. Open point 8 of xml-element-loader (an error type for the record loader) stays open for definition-loader.

**What markup-sample inherits.** HelloPanel makes one `TemplateBindings` with its `CommandRegistry`, adds a selector for each key, calls `AttachTo(loader)` and `loader.RegisterHelloPanelElements()`, and loads its panel. A load error prints `TemplateError.ToString`, which names `UnknownBinding` and `BindingTypeMismatch`. The allocation gate of markup-sample can follow `TemplateBindingAllocationTests`.

### Public API (end state)

```csharp
namespace BrutalUi.Framework;

public sealed class TemplateBindings
{
    public TemplateBindings(CommandRegistry commands);   // ArgumentNullException for null

    // bind-text on a Label or a TextField
    public void AddText<T>(string key, Store<T> store, Func<T, string?> selector);
    // bind-text on a Label only (A5). Q2: the format and the provider come from here
    public void AddText<T, TValue>(string key, Store<T> store, Func<T, TValue> selector,
        string? format = null, IFormatProvider? provider = null) where TValue : IUtf8SpanFormattable;
    // bind-checked on a Toggle
    public void AddChecked<T>(string key, Store<T> store, Func<T, bool> selector);
    // bind-value on a Slider
    public void AddValue<T>(string key, Store<T> store, Func<T, float> selector);

    public void AttachTo(TemplateLoader loader);         // adds one hook handler
}
```

```csharp
namespace BrutalUi.Markup;

public enum TemplateErrorKind : byte
{
    NotWellFormed, UnknownElement, UnknownAttribute, InvalidValue,
    MissingAttribute, UnknownTexture, UnexpectedText, UnexpectedChild,
    UnknownBinding,        // new: a bind key with no registered selector
    BindingTypeMismatch,   // new: a key whose selector does not fit the attribute or the element
}
```

The `Add*` methods return `void`, as `CommandRegistry.Register` does. The key is a `string`, because the attribute value is a `string`, and no span overload exists, so `BRUTALCORE0011` accepts a literal key. The two `AddText` overloads have the shape of the two `LabelBindings.BindText` overloads, so a `string` selector picks the first overload and a number selector picks the second.

### The Entry Shape

`TemplateBindingEntry` is an internal abstract class with four virtual methods, one for each target type. Each method returns `false` in the base class. A sealed generic entry overrides the methods of the targets that it fits. The generic types close at the `Add*` call, so the handler needs no reflection and no type test of the selector.

| Entry | `Add*` call | Overrides | Calls |
|---|---|---|---|
| `StringTextEntry<T>` | `AddText<T>` | `TryBind(Label)`, `TryBind(TextField)` | `label.BindText(store, selector)`, `field.BindText(store, selector)` |
| `NumberTextEntry<T, TValue>` | `AddText<T, TValue>` | `TryBind(Label)` | `label.BindText(store, selector, format, provider)` |
| `CheckedEntry<T>` | `AddChecked<T>` | `TryBind(Toggle)` | `toggle.BindChecked(store, selector)` |
| `ValueEntry<T>` | `AddValue<T>` | `TryBind(Slider)` | `slider.BindValue(store, selector)` |

Each entry also gives `SelectorKind` (phase 3): `a string`, `a number`, `a bool`, or `a float`. The mismatch message uses it. A `false` result of `TryBind` is the `BindingTypeMismatch` case. A number key on a `TextField` gives `false`, because `NumberTextEntry` does not override `TryBind(TextField)` (A5).

### The Handler

`AttachTo` adds the instance method `Handle` as one `AttributeHandler`. The method group captures the `TemplateBindings` object. `Handle` switches on the attribute name with an ordinal `switch`:

- Another name returns `false`, so the next handler of the loader sees the attribute.
- `command`, `bind-text`, `bind-checked`, and `bind-value` always return `true` in the end state. Each adds at most one error, in this order of checks:
  1. **The element type (A3).** `command` needs a `Button`, `bind-text` needs a `Label` or a `TextField`, `bind-checked` needs a `Toggle`, and `bind-value` needs a `Slider`. Another type adds `UnknownAttribute`.
  2. **An empty value (A2).** An empty value adds `InvalidValue`.
  3. **The key (bind attributes only).** A key that the table does not hold adds `UnknownBinding`. `command` has no such check (A1): the registry finds the command at each click.
  4. **The fit (bind attributes only).** A `false` result of `TryBind` adds `BindingTypeMismatch`.
  5. **The bind.** `command` calls `button.BindCommand(_commands, Encoding.UTF8.GetBytes(value))`. A bind attribute calls `TryBind`, which calls the bind method.

The value is used as is, with an ordinal compare and no trim (Notes item 6). The element type comes first, because an attribute on the wrong element is wrong for each value.

Phases 1 and 2 build the success path. In those phases, each fault case returns `false`, so the loader gives its own `UnknownAttribute`. Phase 3 replaces that with the order above.

### The Rules of the Object

- **One handler for each `AttachTo`.** `AttachTo` adds one handler for the four attribute names. A second `AttachTo` of the same object on the same loader throws `ArgumentException` and adds nothing. The object keeps a small list of its loaders for the check. One object can attach to more than one loader.
- **One bindings set for each loader.** The first attached handler takes each of the four names, so a second `TemplateBindings` on the same loader never sees them. The doc comment states it, and a game makes one loader for each bindings set (Chosen Approach).
- **An add after `AttachTo` is valid.** The handler reads the table at each load. A key that the panel code adds after `AttachTo` serves the next `Load`. An `Add*` call and a `Load` call must not run at the same time, as for `TemplateLoader.Register`.
- **One key holds one selector (A4).** A second `Add*` call for one key throws `ArgumentException`, also for another `Add*` method. A `null` key, store, or selector throws `ArgumentNullException`, and an empty key throws `ArgumentException`.
- **The handler order.** The handler goes to the end of the hook list. A game handler that the game adds before `AttachTo` sees each attribute first.
- **Load time and run time.** The bind and the UTF-8 copy of a command name allocate at load time (epic A10). The bound elements hold the same targets as a C# bind, so `Update` and `Draw` allocate nothing.

---

## Implementation Phases

The feature has 18 tasks, so it has three slices. Section 11 of `.beast-mode/phase-handoff.md` holds the rules. Rule 11 binds phase 1 to the demo of Q4: the section 11 template loads with `command` and one `bind-text` label, a click runs the command, and a store write changes the label text. The other widget bindings come second, because a game panel needs each of them, and the parity test and the gate prove them. The errors come third: the success path works, and phase 3 adds the validation (rule 2).

**The agent tier.** Phase 1 is ADVANCED (opus). It fixes the public API that definition-loader and markup-sample build on, it adds a new edge to the package graph, and it touches four integration points: the Markup hook, `CommandBindings`, `LabelBindings`, and the pack. Phases 2 and 3 are STANDARD (sonnet). They fill the entries, the cases, and the tests in the shape that phase 1 fixes.

**The commands.** Export `DOTNET_CLI_HOME=/tmp/dui-cli-home` and `MSBUILDDISABLENODEREUSE=1` in each command. Give `-nodeReuse:false` to `dotnet build` only. Each task group ends with `dotnet build brutal-ui.sln -nodeReuse:false 2>&1 | tail -n 30` and `dotnet test --solution brutal-ui.sln 2>&1 | tail -n 30` (tool rule 5). Expect `0 Warning(s)`, `0 Error(s)`, and 0 failed. A warning fails the build. Run one class after a build with `tests/BrutalUi.Framework.Tests/bin/Debug/net10.0/BrutalUi.Framework.Tests --filter-class '*<Class>' 2>&1 | tail -n 5`. The projects have no implicit usings, so each new file holds its own `using` lines. Use file-scoped namespaces. The source namespace is `BrutalUi.Framework`, and the test namespace is `BrutalUi.Framework.Tests`, as in the existing test folders.

**The plants.** For each plant, copy the file to `src/BrutalUi.Framework/obj/plantscratch/` (or `src/BrutalUi.Markup/obj/plantscratch/` for a Markup file, both git-ignored), make the change, and run the solution tests with `dotnet test --solution brutal-ui.sln > <scratch>/pN.log 2>&1; echo "exit=$?"`. Check the `error CS` count first. It must be 0, because a plant that does not compile leaves the old binary, and the old binary passes. Expect an exit code that is not 0. Read the failed names from the log. Restore with `cp`, check with `cmp`, and run again. A test host can exit with 134 one time. Run a second time before you treat it as a fault.

**The tests.** Follow the style of `CommandBindingTests` and `BoundTreeAllocationTests`. Pass a template as a `u8` literal to `Load(ReadOnlySpan<byte>, string)`, because `BRUTALCORE0011` rejects a string literal for `Load(string, string)`. Build each context with a `RecordingRenderer` measurer. Find an element with `root.Find<T>("name"u8)`, and click its center from `ThousandWidgetTree.ViewportCenter(element)` after the first `Update`. Each error assert checks `Kind`, `Line`, and `Column`, and checks the message with `Contains` on the quoted text only. Use a `static` lambda for each selector and a `static readonly CommandHandler` with a counter target for each command.

### Phase 1: The Section 11 Demo
**Goal:** Framework references Markup. `TemplateBindings` exists with the constructor, the number overload of `AddText`, and `AttachTo`. The handler binds `command` on a `Button` and `bind-text` on a `Label` with a number key. A test loads the section 11 template with one `bind-text` label, clicks the button, and writes the store.
**Demo:** the developer runs `BrutalUi.Framework.Tests --filter-class '*TemplateBindingDemoTests'` and reads a total of 4 with `failed: 0`. In `TheSectionElevenTemplateRunsTheCommandAndShowsTheStore`, a click on `pause` runs `game.pause` one time, and a store write of the score 1500 changes the `score` label to `1,500`.
**Needs:** none
**Agent tier:** ADVANCED (opus). This phase fixes the public API and the package edge.

**Tasks (one group, 6 files):**
- [ ] 1.1 Create `docs/features/markup/binding-attributes/context.md` when `/start-feature` did not. Run `git status --porcelain`. Record the base commit (`git rev-parse HEAD`, `5bbeec9` at planning time), the solution total with the skipped count (3143 with 3 skipped at planning time), and the total of `BrutalUi.Framework.Tests`. Record the Public API, The Entry Shape, The Handler, The Rules of the Object, and the Notes of this plan. Record the tool notes: `DOTNET_CLI_HOME`, the per-class binary, and the `error CS` check of a plant.
- [ ] 1.2 Edit `src/BrutalUi.Framework/BrutalUi.Framework.csproj`. After the Core line, add `<ProjectReference Include="../BrutalUi.Markup/BrutalUi.Markup.csproj" />`. Build, and expect `0 Error(s)`. Run `dotnet msbuild src/BrutalUi.Framework/BrutalUi.Framework.csproj -getItem:Analyzer 2>&1 | grep -c 'BrutalUi.Generators'` and expect `0`: the generator does not flow through the project reference. Run `dotnet pack brutal-ui.sln --no-build --configuration Debug 2>&1 | tail -n 5`, then `unzip -p artifacts/packages/BrutalUi.Framework.0.1.0-alpha.nupkg BrutalUi.Framework.nuspec | grep -n 'BrutalUi.Markup'`, and expect one `dependency` line. Record the line and its `exclude` attribute in `context.md` (Notes item 7).
- [ ] 1.3 Write `src/BrutalUi.Framework/Templates/TemplateBindingEntry.cs` with `internal abstract class TemplateBindingEntry` and its four virtual `TryBind` overloads (`Label`, `TextField`, `Toggle`, `Slider`), each `false` in the base class. Add `internal sealed class NumberTextEntry<T, TValue> where TValue : IUtf8SpanFormattable`. It holds the store, the selector, the format, and the provider in `readonly` fields, in the field style of `CommandBindings.ButtonCommand`, and overrides `TryBind(Label)` with `label.BindText(store, selector, format, provider)` and `return true`. The class remarks state the entry rule of The Entry Shape. The other three entries come in phase 2, and `SelectorKind` comes in phase 3.
- [ ] 1.4 Write `src/BrutalUi.Framework/Templates/TemplateBindings.cs` with `public sealed class TemplateBindings`: the constructor (`ArgumentNullException` for a `null` registry), a `Dictionary<string, TemplateBindingEntry>` with `StringComparer.Ordinal`, `AddText<T, TValue>` (`ArgumentNullException.ThrowIfNull` for the key, the store, and the selector, then `_entries.Add`), `AttachTo` (`ArgumentNullException` for a `null` loader, then `loader.AddAttributeHandler(Handle)`), and the private `Handle`. In this phase, `Handle` binds `command` on a `Button` with a value that is not empty, and `bind-text` on a `Label` with a known key whose `TryBind` gives `true`. Each other case of the four names returns `false`, and another name returns `false`. The class remarks state the capture, the load-time rule, the one-set rule, and the rule for an add after `AttachTo`.
- [ ] 1.5 Write `tests/BrutalUi.Framework.Tests/Templates/TemplateBindingDemoTests.cs` with 4 tests. `TheSectionElevenTemplateRunsTheCommandAndShowsTheStore` (the template of the new section 11 block of task 1.6, word for word with LF line ends. The test registers `game.pause` with a counter, adds `AddText("hud.score", store, static s => s.Score, "N0", CultureInfo.InvariantCulture)` on a `Store<BoundTree.HudState>`, attaches, loads, and adds the root to a context. After the first `Update`, the label shows `0`. A press and a release at the center of `pause` give one call. A `Set` with the score 1500 and one `Update` give the label text `1,500`). `TheFormatAndTheProviderComeFromTheRegisterCall` (the same key with `"N0"` and `de-DE` gives `1.500`). `ACommandThatTheGameRegistersAfterTheLoadRuns` (the load runs with no `game.pause` in the registry, the test registers it after the load, and a click runs it, A1). `AnAddAfterAttachToServesTheNextLoad` (`AttachTo` runs first, `AddText` second, and the load binds the label).
- [ ] 1.6 Edit `docs/technical-design.md`. In section 3, change the Framework line to: it references Core and Markup. In section 11, after **The attribute hook.**, add a **The binding attributes.** block: the section 11 template with `bind-text="hud.score"` on the `score` label, the C# code of the demo (`new TemplateBindings(commands)`, `AddText`, `AttachTo`, `Load`), and the rules of phase 1. Change the last sentence of **The attribute hook.** and the sentence after the first template to name `TemplateBindings.AttachTo`. Keep the first template of section 11 unchanged (Notes item 2). Run the build and the tests, and expect Framework +4. Run two plants. Plant 1: the `command` branch returns `true` with no `BindCommand` call. Expect `TheSectionElevenTemplateRunsTheCommandAndShowsTheStore` and `ACommandThatTheGameRegistersAfterTheLoadRuns` to fail. Plant 2: `NumberTextEntry.TryBind(Label)` passes `null` for the format. Expect `TheFormatAndTheProviderComeFromTheRegisterCall` and the demo test to fail. Record the counts, the plants, and the demo state in `context.md`.

**Estimated Steps:** about 55 (6 tasks, about nine steps for one task)

**Files (6):** `context.md`, `BrutalUi.Framework.csproj`, `TemplateBindingEntry.cs`, `TemplateBindings.cs`, `TemplateBindingDemoTests.cs`, `technical-design.md`

**Acceptance Criteria:**
- [ ] The 4 new Framework tests pass, and each existing test passes with no edit.
- [ ] `src/BrutalUi.Framework/BrutalUi.Framework.csproj` holds one `ProjectReference` to `BrutalUi.Markup.csproj`, and `src/BrutalUi.Markup/BrutalUi.Markup.csproj` does not change.
- [ ] The Analyzer item list of Framework holds no `BrutalUi.Generators` entry, and the packed Framework nuspec holds one dependency on `BrutalUi.Markup`.
- [ ] Plants 1 and 2 each give a non-zero exit code with 0 `error CS` lines, and each restored file equals its copy under `cmp`.
- [ ] The template of **The binding attributes.** equals the template text of `TheSectionElevenTemplateRunsTheCommandAndShowsTheStore`, character for character.

### Phase 2: Every Widget Binding, Equal to Code
**Goal:** `AddText<T>` (string), `AddChecked`, and `AddValue` exist. `bind-text` binds a `Label` and a `TextField` with a string key, `bind-checked` binds a `Toggle`, and `bind-value` binds a `Slider`. A template tree equals the C# bound tree after each store version and after a click. A loaded bound tree allocates 0 bytes in `Update` and `Draw`.
**Demo:** the developer runs `BrutalUi.Framework.Tests --filter-class '*TemplateParityTests'` and reads a total of 2 with `failed: 0`. The template tree and the C# tree show the same text, checked state, and value after each of 10 store versions.
**Needs:** Phase 1
**Agent tier:** STANDARD (sonnet).

**Tasks (group 1, the code and the widget tests, 4 files):**
- [ ] 2.1 Edit `src/BrutalUi.Framework/Templates/TemplateBindingEntry.cs`. Add `StringTextEntry<T>` (overrides `TryBind(Label)` with the string `BindText` and `TryBind(TextField)` with `field.BindText`), `CheckedEntry<T>` (overrides `TryBind(Toggle)` with `BindChecked`), and `ValueEntry<T>` (overrides `TryBind(Slider)` with `BindValue`), each with the field style of task 1.3.
- [ ] 2.2 Edit `src/BrutalUi.Framework/Templates/TemplateBindings.cs`. Add `AddText<T>(string, Store<T>, Func<T, string?>)`, `AddChecked<T>`, and `AddValue<T>` with the checks of task 1.4. In `Handle`, bind `bind-text` on a `TextField`, `bind-checked` on a `Toggle`, and `bind-value` on a `Slider` through `TryBind`. Each fault case still returns `false`. Confirm that `AddText("k", store, static s => s.Name)` picks the string overload and `static s => s.Score` picks the number overload, and record the result in `context.md` (Risk 3).
- [ ] 2.3 Write `tests/BrutalUi.Framework.Tests/Templates/TemplateWidgetBindingTests.cs` with 4 tests. `AStringKeyBindsALabel` (a `Name` key shows the name after the first `Update`, and a new name after a `Set`). `OneStringKeyServesALabelAndATextField` (one `hud.name` key on a label and a field: both show the name, and each holds one binding, A5). `ACheckedKeyBindsAToggle` (a `Muted` key: a `Set` flips `Checked`, and `CheckedChanged` gets no call). `AValueKeyBindsASlider` (a `Volume` key on `<ui:Slider max="100" .../>`: a `Set` writes `Value`, and `ValueChanged` gets no call). Run the build and the tests, and expect Framework +4. Run plant 3: `StringTextEntry.TryBind(TextField)` returns `false`. Expect `OneStringKeyServesALabelAndATextField` to fail. Record the plant in `context.md`.

**Tasks (group 2, the parity, the gate, and the docs, 5 files):**
- [ ] 2.4 Write `tests/BrutalUi.Framework.Tests/Shared/BoundTemplate.cs` as `internal static class BoundTemplate`. It holds the template text (a root `hud`, the labels `score` (`hud.score`, `N0`, invariant), `volume` (`hud.volume-text`, `F1`, invariant), and `player` (`hud.name`), the field `name-field` (`capacity="16"`, `hud.name`), the toggle `mute` (`hud.muted`), the slider `volume-slider` (`max="100"`, `hud.volume`), and the button `pause` (`game.pause`)). `Load(Store<BoundTree.HudState>, CommandRegistry)` makes the bindings, attaches them to a new loader, and returns the root. `BuildCode(Store<BoundTree.HudState>, CommandRegistry)` builds the same tree with the builder methods, the same create values as the factories of `BuiltInElements.cs`, and the C# bind methods. Then write `tests/BrutalUi.Framework.Tests/Templates/TemplateParityTests.cs` with 2 tests. `TheTemplateTreeEqualsTheCodeTreeAfterEachStoreVersion` (two contexts, one for each tree. For rounds 1 to 10, `Set(BoundTree.Next(round))`, `Update` both, and assert `Assert.Null(TreeCompare.FirstDifference(code, loaded))` and the same `BindingCount` on each bound element. `TreeCompare` does not compare `BindingCount`). `AClickOnEachTreeRunsTheCommandOneTime` (one click on each `pause` gives two calls of one counter).
- [ ] 2.5 Write `tests/BrutalUi.Framework.Tests/Templates/TemplateBindingAllocationTests.cs` with 3 tests, in the pattern of `BoundTreeAllocationTests` and with `AllocationGate.ReleaseAllocationContext()` before each measure. The scene loads `BoundTemplate` into a context with a `RecordingRenderer`, caches the center of `pause` after the first `Update`, and builds the two pointer events. `UpdateAndDrawOnALoadedBoundTreeAllocateNothing` (each frame sets `BoundTree.Next(round)`, then runs `Update(0.016f)` and `Draw`: 2 warm-up rounds of 9 frames, then 100 measured frames, 0 bytes). `AClickOnALoadedCommandButtonAllocatesNothing` (each frame pushes a press and a release at `pause`, then runs `Update` and `Draw`, 0 bytes). `TheMeasuredBodyDoesRealWork` (after one changing frame, each bound element shows the state of the round. After one click frame, the counter grows by 1).
- [ ] 2.6 Edit `docs/technical-design.md`. In section 11, extend **The binding attributes.** with the table of the four attributes (attribute, element types, `Add*` method, bind method), The Entry Shape in prose, and the rules of the object except the errors. In section 10, **Data binding**, add one sentence: a template binds through `TemplateBindings` (section 11) with the same bind methods, and `TemplateParityTests` guards the parity. In section 14, after the twenty-first gate, add **A twenty-second allocation gate.** for `TemplateBindingAllocationTests` with its plant. Run the build and the tests, and expect Framework +9 in this phase. Run two plants. Plant 4: `CheckedEntry.TryBind(Toggle)` binds `s => !selector(s)`. Expect `ACheckedKeyBindsAToggle` and `TheTemplateTreeEqualsTheCodeTreeAfterEachStoreVersion` to fail. Plant 5: `GC.KeepAlive(new object());` as the first line of `WidgetBindings.ValueHandler<T>.Run`. Expect `UpdateAndDrawOnALoadedBoundTreeAllocateNothing` to fail (the gates of `BoundTreeAllocationTests` fail with it). Record the counts and the plants in `context.md`.

**Estimated Steps:** about 50 (6 tasks, about eight steps for one task)

**Files (group 1, 4):** `TemplateBindingEntry.cs`, `TemplateBindings.cs`, `TemplateWidgetBindingTests.cs`, `context.md`
**Files (group 2, 5):** `BoundTemplate.cs`, `TemplateParityTests.cs`, `TemplateBindingAllocationTests.cs`, `technical-design.md`, `context.md`

**Acceptance Criteria:**
- [ ] The 9 new Framework tests pass, and each existing test passes with no edit.
- [ ] `UpdateAndDrawOnALoadedBoundTreeAllocateNothing` and `AClickOnALoadedCommandButtonAllocatesNothing` each measure 0 bytes over 100 frames.
- [ ] Plants 3, 4, and 5 each give a non-zero exit code with 0 `error CS` lines, and each restored file equals its copy under `cmp`.

### Phase 3: The Positioned Binding Errors
**Goal:** `TemplateErrorKind` holds `UnknownBinding` and `BindingTypeMismatch`. The handler reports each fault case in the order of The Handler, with the line and the column of the attribute. The `Add*` methods and `AttachTo` check each argument, a duplicate key, and a second attach. The docs and the changelog name the feature.
**Demo:** the developer runs `BrutalUi.Framework.Tests --filter-class '*TemplateBindingErrorTests'` and reads a total of 6 with `failed: 0`. A template with `bind-text="hud.nme"` gives one `UnknownBinding` error at the line and the column of `bind-text`, and no root.
**Needs:** Phase 1, Phase 2
**Agent tier:** STANDARD (sonnet).

**Tasks (group 1, the kinds, the handler, and the error tests, 5 files):**
- [ ] 3.1 Edit `src/BrutalUi.Markup/Template/TemplateError.cs`. Add `UnknownBinding` and `BindingTypeMismatch` after `UnexpectedChild`, each with a doc line that names its cause. Add the two arms to `GetKindName` (Notes item 1). Run `grep -rn 'TemplateErrorKind' src tests --include=*.cs | grep -v 'TemplateErrorKind\.'` and confirm that no code counts the members or switches on the kind with no default. Build, and expect `0 Error(s)` and each Markup test green.
- [ ] 3.2 Edit `src/BrutalUi.Framework/Templates/TemplateBindingEntry.cs`. Add `public abstract string SelectorKind { get; }` to the base class and its value to each entry: `a string`, `a number`, `a bool`, `a float`.
- [ ] 3.3 Edit `src/BrutalUi.Framework/Templates/TemplateBindings.cs`. Write `Handle` in the order of The Handler. Each of the four names now returns `true`. The messages: `'command' is not an attribute of this element. Write command on a Button only.` (and `on a Label or a TextField only`, `on a Toggle only`, `on a Slider only`). `'command' needs a command name. Write the name of a registered command, for example 'game.pause'.` and `'bind-text' needs a key. Write a key that the panel code registers, for example 'hud.score'.` `'hud.nme' is not a bound key. Register it with TemplateBindings.AddText before the load.` (with `AddChecked` or `AddValue` for the other attributes). `'hud.muted' holds a bool selector. bind-text on a Label needs a string or a number selector. Register the key with AddText.` (a `TextField` needs a string selector, a `Toggle` a bool selector with `AddChecked`, a `Slider` a float selector with `AddValue`).
- [ ] 3.4 Write `tests/BrutalUi.Framework.Tests/Templates/TemplateBindingErrorTests.cs` with 6 tests. `AnUnknownKeyIsAnUnknownBindingError` (one template with an unknown key on each bind attribute gives 3 errors in source order, each `UnknownBinding` with its line and column, and no root). `ASelectorOfTheWrongTypeIsABindingTypeMismatchError` (a bool key on a `bind-text` label, a number key on a `bind-text` field (A5), a string key on `bind-value`, and a float key on `bind-checked` give 4 errors of `BindingTypeMismatch`). `AnAttributeOnTheWrongElementIsAnUnknownAttributeError` (`command` on a `Label`, `bind-text` on a `Button`, `bind-checked` on a `Slider`, and `bind-value` on a `Toggle`, each with a key that no `Add*` call registered, give 4 errors of `UnknownAttribute`, so the element check comes before the key check). `AnEmptyValueIsAnInvalidValueError` (`command=""` and `bind-text=""` give 2 errors of `InvalidValue`, A2). `ALoaderWithNoBindingsRejectsEachAttribute` (a loader with no `AttachTo` gives `UnknownAttribute` for each of the four names, A6). `TheErrorTextNamesTheNewKinds` (`ToString` of each new kind contains `UnknownBinding: ` or `BindingTypeMismatch: `). Compute each column from the template text, as `AttributeHookTests.AssertAttribute` does. Run the build and the tests, and expect Framework +6. Run plants 6 and 7. Plant 6: the `bind-text` branch checks the key before the element type. Expect `AnAttributeOnTheWrongElementIsAnUnknownAttributeError` to fail. Plant 7: `GetKindName` loses the `UnknownBinding` arm. Expect `TheErrorTextNamesTheNewKinds` to fail. Record the plants in `context.md`.

**Tasks (group 2, the argument checks, the API tests, and the docs, 5 files):**
- [ ] 3.5 Edit `src/BrutalUi.Framework/Templates/TemplateBindings.cs`. Each `Add*` method throws `ArgumentException` for an empty key and, before the add, for a key that the table holds, with a constant message that names the rule (A4). `AttachTo` keeps a `List<TemplateLoader>` and throws `ArgumentException` (parameter `loader`) for a loader that it holds, before it adds a handler. Then write `tests/BrutalUi.Framework.Tests/Templates/TemplateBindingsApiTests.cs` with 4 tests. `ASecondAddForOneKeyThrows` (`AddText` then `AddText`, and `AddText` then `AddChecked`, on one key each throw `ArgumentException`, and the first selector still binds). `TheMethodsCheckTheirArguments` (a `null` registry, a `null` loader, and a `null` key, store, or selector throw `ArgumentNullException` with the parameter name. An empty key throws `ArgumentException`). `ASecondAttachToOnOneLoaderThrows` (the second call throws, and a load still binds one time: the label holds one binding). `TheHandlerPassesAnotherAttributeToTheNextHandler` (a game handler added after `AttachTo` takes `probe`, and the load succeeds).
- [ ] 3.6 Edit `docs/technical-design.md`. In section 11, add the two members to the `TemplateErrorKind` block, add the two rows to the table of **The error report.**, and add the order of checks, the four messages, and the rules of A4 and `AttachTo` to **The binding attributes.** In section 10, **The button bind rules**, add one line: the `command` attribute calls `BindCommand` (section 11). Add one line to `CHANGELOG.md` under `### Added`, tagged `(markup/binding-attributes)`. Run the build and the tests, and expect Framework +10 in this phase and +23 in total. Run plant 8: `AttachTo` skips the check of its loader list. Expect `ASecondAttachToOnOneLoaderThrows` to fail. Run `pack`, `package-restore`, and `package` of `scripts/test-linux.sh` by hand, and expect `package-test: PASS Gauge level 0.5`. Record the counts, the plants, and the close state in `context.md`.

**Estimated Steps:** about 50 (6 tasks, about eight steps for one task)

**Files (group 1, 5):** `TemplateError.cs`, `TemplateBindingEntry.cs`, `TemplateBindings.cs`, `TemplateBindingErrorTests.cs`, `context.md`
**Files (group 2, 5):** `TemplateBindings.cs`, `TemplateBindingsApiTests.cs`, `technical-design.md`, `CHANGELOG.md`, `context.md`

**Acceptance Criteria:**
- [ ] The 10 new Framework tests pass, and each existing test passes with no edit, also in `BrutalUi.Markup.Tests`.
- [ ] `git diff 5bbeec9 -- src/BrutalUi.Markup` touches `Template/TemplateError.cs` only.
- [ ] Plants 6, 7, and 8 each give a non-zero exit code with 0 `error CS` lines, and each restored file equals its copy under `cmp`.
- [ ] The package step prints `package-test: PASS Gauge level 0.5`.

---

## Key Technical Decisions

### Confirmed Assumptions

- Q1 The attribute names for the store bindings: The attributes are `bind-text`, `bind-checked`, and `bind-value`.
- Q2 The number format of a Label for a `bind-text` key: The C# register call gives the selector, the format, and the provider. The template holds no format.
- Q3 The error kind of a binding error: `TemplateErrorKind` gets `UnknownBinding` and `BindingTypeMismatch` at the end of the enum.
- Q4 The demo of phase 1: The phase 1 demo is a test that loads the section 11 template with `command` and one `bind-text` Label. A click runs the command, and a store write changes the label text.
- A1 `command` does not check the name at load. `BindCommand` finds the command at each click (`src/BrutalUi.Framework/Commands/CommandBindings.cs`).
- A2 An empty `command` value and an empty bind key each give an `InvalidValue` error.
- A3 An attribute on the wrong element type gives an `UnknownAttribute` error from the handler, with the line and the column.
- A4 A second register call for one key throws `ArgumentException`. A key holds one selector.
- A5 One string key serves a Label and a TextField. A number key serves a Label only.
- A6 A loader with no attached bindings gives `UnknownAttribute` for `command` and for each bind attribute, as today.
- A7 The hook contract of Markup does not change. The only Markup edit is the two enum values of Q3.
- A8 The tests are in `tests/BrutalUi.Framework.Tests/`. They use the linked `TreeCompare.cs` and the allocation gate pattern of that project.
- A9 The feature does not change `PanelContext`, `PanelDefinition`, or HelloPanel. definition-loader and markup-sample own those edits.
- A10 The test handler for `command` in `tests/BrutalUi.Markup.Tests/` stays, because Markup does not reference Framework.

### Decision 1: A `TemplateBindings` object that captures its state in the hook handler
**Choice:** One public class in Framework holds the `CommandRegistry` and the key table. `AttachTo(loader)` adds its instance method as an `AttributeHandler`, so the handler captures the object.
**Rationale:** This is the Chosen Approach. The key table is one visible object, so the duplicate check (A4) and the fit check (Q3) have one place for the code and the test. The capture needs no state argument, so the hook contract stays as it is (A7). This settles open point 9 of `docs/features/markup/xml-element-loader/implementation.md` ("the registry of the load"): the registry of the load is the registry of the attached `TemplateBindings`.
**Alternatives:** Extension methods on `TemplateLoader` (the key table is hidden, and the duplicate check has no owner). A state argument on `Load` (it changes the hook contract, against A7).

### Decision 2: An abstract entry with one virtual `TryBind` for each target type
**Choice:** `TemplateBindingEntry` has `TryBind(Label)`, `TryBind(TextField)`, `TryBind(Toggle)`, and `TryBind(Slider)`, each `false` in the base class. Four sealed generic entries override the targets that they fit and call the public bind method.
**Rationale:** The generic types close at the `Add*` call, so the handler calls a virtual method and needs no reflection and no type test of a selector. A `false` result is the mismatch case, so A5 and Q3 are data of the entry. The entry calls the public bind method, so the template element gets the same handler and the same target as a C# element, and the 0-byte proof of `BoundTreeAllocationTests` applies.
**Alternatives:** A delegate for each target type in one entry struct (four nullable fields, and the null check is the mismatch test. It works, but it allocates up to four closures for each key and hides the kind for the message). A `Type` field and a cast (a run-time type test, near to reflection).

### Decision 3: One handler for the four names, with a fixed order of checks
**Choice:** `AttachTo` adds one handler. The handler checks the element type, then an empty value, then the key, then the fit, and adds at most one error for each attribute.
**Rationale:** One handler is one entry in the hook list, so one `AttachTo` check covers it. The element type comes first, because an attribute on the wrong element is wrong for each value (A3). The empty check comes before the lookup, so an empty key gives `InvalidValue` and not `UnknownBinding` (A2). One error for each attribute keeps the error list short and exact.
**Alternatives:** Four handlers, one for each name (four entries for each attach, and the same check four times).

### Decision 4: A second `AttachTo` on the same loader throws, and an add after `AttachTo` is valid
**Choice:** `AttachTo` throws `ArgumentException` for a loader that it holds already. An `Add*` call after `AttachTo` serves the next load.
**Rationale:** A second handler of the same object never runs, because the first takes each attribute. A silent second add hides a setup fault, and the throw follows the duplicate rule of `CommandRegistry.Register` and A4. The handler reads the table at each load, so the order of the add and the attach does not matter to the panel code.
**Alternatives:** A no-op for a second attach (it hides the fault). A rule that forbids an add after `AttachTo` (it needs a flag and a throw, and it gives the panel code no value).

### Decision 5: The key is a `string` with an ordinal compare, and the value is used as is
**Choice:** The `Add*` methods take a `string` key. The table uses `StringComparer.Ordinal`. The handler trims nothing.
**Rationale:** The attribute value is a `string`, so a `string` key needs no conversion at load. A key has no span overload, so `BRUTALCORE0011` accepts a literal key in panel code. The ordinal compare follows the type names, the row names, and the texture names of the loader. The `string` row of the generator also uses the value as is.
**Alternatives:** A UTF-8 key (a conversion at each lookup, and a `u8` literal in each register call). A trim of XML whitespace (a key with a space then matches a key with no space, and the text of the author and the table differ).

### Decision 6: The `command` handler encodes the name and calls `BindCommand`
**Choice:** `button.BindCommand(_commands, Encoding.UTF8.GetBytes(value))`.
**Rationale:** `BindCommand` copies the name, so the encoded array is a load-time allocation only (epic A10). The click path is the path of a C# bind, which `CommandAndEventAllocationTests` guards at 0 bytes. The empty check of the handler runs first, so `BindCommand` never throws in a load.

### Decision 7: The tests reuse `BoundTree.HudState` and a shared `BoundTemplate`
**Choice:** The new tests use the `HudState` record struct and `BoundTree.Next` of `tests/BrutalUi.Framework.Tests/Shared/BoundTree.cs`. A new `Shared/BoundTemplate.cs` holds the template and its C# twin for the parity test and the gate.
**Rationale:** `Next` gives values of a fixed width, so no buffer grows after warm-up, and the gate needs no new value scheme. One template for the parity test and the gate proves the parity and the cost on the same tree.

---

## Risks & Mitigation

| # | Risk | Impact | Mitigation |
|---|------|--------|------------|
| 1 | The new reference brings the generator into Framework, or the pack writes a wrong dependency. | Framework gets generated code, or a package consumer gets no Markup. | Task 1.2 reads the Analyzer item list (expect 0 hits) and the packed nuspec (expect one dependency). The Markup csproj marks the generator reference `PrivateAssets="all"`, and an analyzer does not flow through a project reference (element-generator Notes item 8). Task 3.6 runs the package step. |
| 2 | A type name of `BrutalUi.Markup` clashes with a name of `BrutalUi.Framework` in a file with both `using` lines. | `CS0104` in a new file. | Only the new files hold both `using` lines. The build shows a clash at once. Qualify the name in that file. |
| 3 | The two `AddText` overloads give an ambiguous call for a `string` selector. | The panel code does not compile. | The overloads copy the shape of the two `LabelBindings.BindText` overloads, which `BoundTree` calls with both selector kinds. `string` does not implement `IUtf8SpanFormattable`, so the generic overload drops out. Task 2.2 confirms both calls. |
| 4 | Two `TemplateBindings` sets on one loader. | The second set never sees an attribute, and a key of the second set gives `UnknownBinding`. | The doc comment states the one-set rule. A game makes one loader for each set (Chosen Approach). Notes item 5 records it. |
| 5 | A plant in a shared bind method fails more than the new gate. | A confusing plant log. | Plant 5 lists the expected extra failures (`BoundTreeAllocationTests`). The plant check reads the named test, not the count. |
| 6 | The button of a loaded tree has no size with no stylesheet, so a click misses. | The demo and the click gate fail. | The context has a `RecordingRenderer` measurer, so the text gives the button a size. Each click test reads `ViewportCenter` after the first `Update` and asserts a width above 0. |
| 7 | A test that changes the culture runs in parallel with another test. | Another test reads a wrong culture. | The tests pass the provider to `AddText` and never change `CultureInfo.CurrentCulture`. |

---

## Dependencies

- xml-element-loader (complete): `TemplateLoader`, `AddAttributeHandler`, `AttributeHandler`, `AttributeContext.AddError`, `TemplateError`, `TemplateErrorKind`.
- element-generator (complete): the hook contract is unchanged. The analyzer stays inside the Markup package.
- builder-api (complete): `Find<T>`, the builder methods for the C# twin, and `TreeCompare.cs` (linked in the Framework tests already).
- state-and-commands (complete): `CommandRegistry`, `CommandBindings.BindCommand`, `Store<T>`, `LabelBindings`, `WidgetBindings`.
- No new package, no native dependency, and no change to `Directory.Packages.props`. The one new edge is the project reference from Framework to Markup.
- definition-loader and markup-sample depend on this feature.

---

## Definition of Done

### Functional Criteria
- [ ] `src/BrutalUi.Framework/Templates/` holds `TemplateBindings.cs` and `TemplateBindingEntry.cs`. `TemplateBindings` is the one new public type, with the members of the Public API: the constructor, `AddText<T>`, `AddText<T, TValue>`, `AddChecked<T>`, `AddValue<T>`, and `AttachTo`.
- [ ] A loader with attached bindings loads `<ui:Button ... command="game.pause" />`. A click on the button invokes `game.pause` of the registry one time. A command that the game registers after the load runs at the click.
- [ ] `bind-text="k"` on a `Label` with a number key shows the slice with the format and the provider of the `AddText` call (`N0` and the invariant culture give `1,500` for 1500).
- [ ] One string key binds a `Label` and a `TextField`. `bind-checked` binds `Toggle.Checked`, and `bind-value` binds `Slider.Value`. A store change calls no widget slot.
- [ ] A template tree and the C# tree with the same bind calls compare equal under `TreeCompare.FirstDifference` after each of 10 store versions, and each bound element has the same `BindingCount` in both trees.
- [ ] An `Add*` call after `AttachTo` serves the next load. A game handler added after `AttachTo` still takes its own attribute.

### Quality Criteria
- [ ] An unknown key gives `UnknownBinding`. A key of the wrong selector type gives `BindingTypeMismatch`, also a number key on a `TextField`. An attribute on the wrong element type gives `UnknownAttribute`. An empty value gives `InvalidValue`. Each error has the line and the column of the attribute name, and the result has no root.
- [ ] A loader with no attached bindings gives `UnknownAttribute` for `command`, `bind-text`, `bind-checked`, and `bind-value`.
- [ ] A second `Add*` call for one key throws `ArgumentException`. A second `AttachTo` on one loader throws `ArgumentException`. A `null` argument throws `ArgumentNullException` with the parameter name. `Load` throws no exception for any template text in the tests.
- [ ] `UpdateAndDrawOnALoadedBoundTreeAllocateNothing` and `AClickOnALoadedCommandButtonAllocatesNothing` measure 0 bytes over 100 frames, and plant 5 fails the first one.
- [ ] Each existing test passes with no edit. The Framework total grows by 23 from the baseline of `context.md`, and the other test projects keep their totals.
- [ ] The build gives `0 Warning(s)` and `0 Error(s)`. Each new public member has an XML doc comment. Each new file has fewer than 400 lines.

### Integration Criteria
- [ ] `src/BrutalUi.Framework/BrutalUi.Framework.csproj` references `BrutalUi.Core` and `BrutalUi.Markup`. `src/BrutalUi.Markup/BrutalUi.Markup.csproj` does not change and references no Framework project.
- [ ] In `src/BrutalUi.Markup/`, only `Template/TemplateError.cs` changes: two new members after `UnexpectedChild` and two `GetKindName` arms.
- [ ] `PanelContext`, `PanelDefinition`, the rest of `src/BrutalUi.Framework/Panels/`, `samples/`, `src/BrutalUi.Core/`, `tests/BrutalUi.Markup.Tests/`, and `Directory.Packages.props` do not change.
- [ ] `src/BrutalUi.Framework/Templates/` uses no `System.Reflection`, no `Activator`, no `MakeGenericType`, and no `GetType()`.
- [ ] The packed `BrutalUi.Framework` nuspec holds a dependency on `BrutalUi.Markup`. The package test prints `package-test: PASS Gauge level 0.5`.
- [ ] Section 3 of `technical-design.md` says that Framework references Core and Markup. Section 10 names `TemplateBindings` under **Data binding** and the `command` attribute under the button bind rules. Section 11 holds **The binding attributes.**, the two new kinds in the enum block and in the error table. Section 14 names `TemplateBindingAllocationTests`. `CHANGELOG.md` has one line tagged `(markup/binding-attributes)`.

### Verification Method
1. Run `export DOTNET_CLI_HOME=/tmp/dui-cli-home MSBUILDDISABLENODEREUSE=1; dotnet build brutal-ui.sln -nodeReuse:false 2>&1 | tail -n 30`. Expect `0 Warning(s)` and `0 Error(s)`.
2. Run `dotnet test --solution brutal-ui.sln 2>&1 | tail -n 30`. Expect 0 failed and 3 skipped, and a total 23 above the baseline of `context.md` (3166 at planning time).
3. Run `tests/BrutalUi.Framework.Tests/bin/Debug/net10.0/BrutalUi.Framework.Tests --filter-class '<Class>' 2>&1 | tail -n 5` for each class. Expect `failed: 0` and these totals: `*TemplateBindingDemoTests` 4, `*TemplateWidgetBindingTests` 4, `*TemplateParityTests` 2, `*TemplateBindingAllocationTests` 3, `*TemplateBindingErrorTests` 6, `*TemplateBindingsApiTests` 4.
4. Run `grep -c 'ProjectReference' src/BrutalUi.Framework/BrutalUi.Framework.csproj`. Expect 2. Run `grep -n 'BrutalUi.Markup.csproj' src/BrutalUi.Framework/BrutalUi.Framework.csproj`. Expect one line.
5. Run `git diff --stat 5bbeec9 -- src/BrutalUi.Markup`. Expect one file: `src/BrutalUi.Markup/Template/TemplateError.cs`. Run `grep -n 'UnknownBinding\|BindingTypeMismatch' src/BrutalUi.Markup/Template/TemplateError.cs`. Expect the two members after `UnexpectedChild` and two `GetKindName` arms.
6. Run `git diff --stat 5bbeec9 -- src/BrutalUi.Core src/BrutalUi.Framework/Panels samples tests/BrutalUi.Markup.Tests src/BrutalUi.Markup/BrutalUi.Markup.csproj Directory.Packages.props`. Expect no output.
7. Run `grep -rnE 'System\.Reflection|Activator|MakeGenericType|GetType\(\)' src/BrutalUi.Framework/Templates`. Expect no output.
8. Run `git diff 5bbeec9 -- src | grep -E '^\+.*public (sealed |static |readonly |ref |abstract )*(partial )*(class|struct|interface|enum|record|delegate) '`. Expect one line: `public sealed class TemplateBindings`.
9. Run `dotnet msbuild src/BrutalUi.Framework/BrutalUi.Framework.csproj -getItem:Analyzer 2>&1 | grep -c 'BrutalUi.Generators'`. Expect `0`.
10. Run `dotnet pack brutal-ui.sln --no-build --configuration Debug 2>&1 | tail -n 5`, then `unzip -p artifacts/packages/BrutalUi.Framework.0.1.0-alpha.nupkg BrutalUi.Framework.nuspec | grep -n 'BrutalUi.Markup'`. Expect one `dependency` line. Run `rm -rf artifacts/package-test && dotnet restore tests/BrutalUi.PackageTest/BrutalUi.PackageTest.csproj --force 2>&1 | tail -n 3 && dotnet build-server shutdown && dotnet run --project tests/BrutalUi.PackageTest/BrutalUi.PackageTest.csproj --no-restore 2>&1 | tail -n 3`. Expect `package-test: PASS Gauge level 0.5`.
11. Run `wc -l src/BrutalUi.Framework/Templates/*.cs`. Expect 2 files, each under 400 lines.
12. Run `grep -n 'bind-checked' docs/technical-design.md`, `grep -n 'TemplateBindingAllocationTests' docs/technical-design.md`, and `grep -n 'references Core and Markup' docs/technical-design.md`. Expect at least one line each. Run `grep -n 'markup/binding-attributes' CHANGELOG.md`. Expect one line.
13. For each kind in `UnknownBinding BindingTypeMismatch UnknownAttribute InvalidValue`, run `grep -l "TemplateErrorKind.<Kind>" tests/BrutalUi.Framework.Tests/Templates/*.cs`. Expect at least one file for each kind.
14. Compare the template of **The binding attributes.** in section 11 with the template text of `TheSectionElevenTemplateRunsTheCommandAndShowsTheStore`. Expect the same text.
15. Read `context.md`. Expect plants 1 to 8, each with a non-zero exit code, 0 `error CS` lines, and the expected failed test names.

---

## Testing Strategy

- **Demo and path tests (Framework, 8):** `TemplateBindingDemoTests` and `TemplateWidgetBindingTests` load a template, run `Update`, click, and write the store. They cover each attribute and each `Add*` method.
- **Parity tests (Framework, 2):** `TemplateParityTests` compares a loaded tree with the C# bound tree through `TreeCompare` and `BindingCount` over 10 store versions and a click. They prove requirements line 17.
- **Allocation gate (Framework, 3):** `TemplateBindingAllocationTests` measures `Update` and `Draw` with a changing store and with a click on the loaded tree, with a real-work test so the measured body cannot be empty. It proves requirements line 18.
- **Error and API tests (Framework, 10):** `TemplateBindingErrorTests` covers each fault case with its kind, line, and column, A6, and the text form of the new kinds. `TemplateBindingsApiTests` covers A4, the argument checks, the second attach, and the hand-on to the next handler.
- **Plants (8):** the command bind, the format pass, the string field bind, the toggle parity, the gate, the order of checks, the kind name, and the attach check.
- **No regressions:** each existing test passes with no edit. `SectionElevenTemplateTests` of Markup keeps its test handler for `command` (A10). The package test guards the pack.
- **No manual check:** no window, no sample change, and no Needs Human Verification item.

| Class | Project | Phase | Tests |
|---|---|---|---|
| `TemplateBindingDemoTests` | Framework | 1 | 4 |
| `TemplateWidgetBindingTests` | Framework | 2 | 4 |
| `TemplateParityTests` | Framework | 2 | 2 |
| `TemplateBindingAllocationTests` | Framework | 2 | 3 |
| `TemplateBindingErrorTests` | Framework | 3 | 6 |
| `TemplateBindingsApiTests` | Framework | 3 | 4 |

---

## Related Documentation

- `docs/features/markup/binding-attributes/requirements.md`
- `docs/features/markup/epic-overview.md` and `docs/features/markup/epic-requirements.md`
- `docs/features/markup/xml-element-loader/implementation.md` (the hook, Decision 4, and Notes item 9)
- `docs/features/markup/element-generator/implementation.md` (Notes item 8: the analyzer does not flow through a project reference)
- `docs/features/markup/definition-loader/requirements.md` and `docs/features/markup/markup-sample/requirements.md` (the consumers)
- `docs/technical-design.md` section 3 (line 125), section 10 (**Data binding** and **The button bind rules**), section 11 (the first template, **The error report.**, **The attribute hook.**), and section 14 (the gates)
- `src/BrutalUi.Framework/Commands/CommandBindings.cs`, `src/BrutalUi.Framework/Binding/LabelBindings.cs`, `src/BrutalUi.Framework/Binding/WidgetBindings.cs`
- `src/BrutalUi.Markup/Template/TemplateLoader.cs`, `ElementTypeInfo.cs`, `AttributeContext.cs`, `TemplateError.cs`
- `tests/BrutalUi.Framework.Tests/Binding/BoundTreeAllocationTests.cs` and `Shared/BoundTree.cs` (the gate model and the state)
- `tests/BrutalUi.Markup.Tests/Template/AttributeHookTests.cs` (the column helper)
- `.beast-mode/phase-handoff.md` section 11 (the slice rules)

No harness skill in `.claude/skills/` covers a C# pattern of this feature. The plan references the project files above.

---

## Notes

1. **Conflict: A7 and the name switch of the kinds.** A7 says that the only Markup edit is the two enum values. `TemplateError.GetKindName` is an explicit switch, so `ToString` prints `8` and `9` for the new values with no new arms. The plan adds the two arms and the two doc lines in the same file (task 3.1) and reads them as a part of the Q3 edit, not as a change of the hook contract. A7 stays as it is.
2. **Conflict: Q4 and the text of the section 11 template.** Q4 names "the section 11 template with `command` and one `bind-text` Label". The first template of section 11 has no `bind-text`, and `SectionElevenTemplateTests` of Markup copies it word for word (an acceptance criterion of xml-element-loader). A Markup test cannot load `bind-text` (A10). The plan keeps the first template unchanged and adds the same template with `bind-text="hud.score"` on the `score` label in the new block **The binding attributes.** The demo test copies that block.
3. **Conflict: section 3 of `technical-design.md`.** Line 125 says that Framework references Core only. The requirements name sections 10 and 11 only. The plan also edits line 125 (task 1.6), because epic Q1 makes the line wrong.
4. **A reading of A3.** The handler gets the `Element`, not the registered XML name, so its message cannot name the element type of the source. The message names the valid element types of the attribute: `'command' is not an attribute of this element. Write command on a Button only.` The kind is `UnknownAttribute`, as A3 says.
5. **Open point: two bindings sets on one loader.** The plan does not detect a second `TemplateBindings` on one loader, because the loader has no public list of its handlers and a static table breaks the no-static-state rule. Options: (a) document the one-set rule (recommended, the plan). (b) Return `false` for an unknown key, so a second set can take it (the loader then gives `UnknownAttribute`, not `UnknownBinding`, and Q3 loses its kind). (c) Add a merge method to `TemplateBindings`.
6. **Open point: the whitespace of a value.** The plan uses the value as is (Decision 5). `command=" game.pause "` binds the name with the spaces, and a click then calls `UnknownCommand`. `bind-text=" k "` gives `UnknownBinding` with the spaces in the quote. Options: (a) as is (recommended, the plan). (b) Trim XML whitespace, as the `TryRead*` reads do.
7. **Open point for markup-sample: the analyzer of a package consumer.** The default pack of a project reference can mark the dependency with `exclude="Build,Analyzers"`. A game that references only the `BrutalUi.Framework` package then gets the Markup library with no generator, and a `[TemplateElement]` type of the game gets no rows. Task 1.2 records the real attribute. When it holds `Analyzers`, markup-sample or the README states that a game with its own element types references `BrutalUi.Markup` too. HelloPanel references Markup as a project already. This feature does not change the pack.
8. **Open point: a `null` registry.** The constructor throws for a `null` `CommandRegistry`, so a panel with only bind attributes passes a registry too. Options: (a) required (recommended, the plan, the sketch of the Chosen Approach). (b) Optional, with `command` as `UnknownAttribute` when the registry is `null`.
9. **Out of scope: `bind-text` on a `Button`.** No `BindText` for a `Button` exists in `LabelBindings` or `WidgetBindings`, and the requirements name a `Label` and a `TextField`. `bind-text` on a `Button` gives `UnknownAttribute` (A3).
10. **A stale remark in Markup.** The remarks of `TemplateLoader.AddAttributeHandler` say that binding-attributes registers `command` and `bind-text`. The handler also takes `bind-checked` and `bind-value`. A7 keeps the file unchanged, so the remark stays. A later doc pass can widen it.
11. **`text-key` and `bind-text` on one label.** The binding writes through `Label.SetText`, which clears `LocalizedText`. The label shows the store after the first `Update`, as a C# label with `Text` and `BindText` does.
12. **The planned totals** come from the element-generator close: the solution 3143 with 3 skipped at `5bbeec9`. Task 1.1 records the real baseline and the Framework total, and the acceptance criteria count the deltas from it.
