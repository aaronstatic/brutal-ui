# markup/definition-loader - Context & Decisions

**Epic:** markup (feature 5 of 6)
**Last Updated:** 2026-11-18 19:45
**Current Phase:** Phase 2 - Every Record Form, Equal to Code
**Status:** ✅ Phase 1 complete. ✅ Phase 2 complete (verifier PASS, Demo PASS).

---

## Quick Status

**What's Done:**
- ✅ The plan interview: Q1 to Q7 and A1 to A9 are confirmed. `implementation.md` holds them under `### Confirmed Assumptions`.
- ✅ The plan: 3 slices, 20 tasks, about 190 steps.
- ✅ Phase 1 group 1 (tasks 1.1 to 1.5): the four public types, `SlotContainerTemplate`, and the `DefinitionReader` placeholder exist. The build has 0 warnings. The test totals equal the baseline below.
- ✅ Phase 1 group 2 (tasks 1.6 to 1.8): the read pass (`DefinitionReader.cs` and `DefinitionReader.Elements.cs`), the resolve pass (`DefinitionReader.Resolve.cs`), the four demo tests, plants 1 and 2, and the section 10 and 11 edits in `technical-design.md`. Build `0 Warning(s)`, `0 Error(s)`. Solution 3171 (0 failed, 3 skipped), Framework 360. Phase 1 acceptance holds.
- ✅ Phase 2 group 1 (tasks 2.1 to 2.3): the `actions` attribute, the direct `PanelRef` under `Mode`, the action map lookup and the default-slot placement rule in the resolve pass, and five tests in `DefinitionRecordTests.cs`. Plants 3 and 4 ran. Build `0 Warning(s)`, `0 Error(s)`. Solution 3176 (0 failed, 3 skipped), Framework 365. Demo class `*DefinitionLoaderDemoTests` still reads 4.

- ✅ Phase 2 group 2 (tasks 2.4 to 2.6): `DefinitionSet.cs`, `DefinitionParityTests.cs` with 2 tests, `ModeDiffAllocationTests.Loaded.cs` with 1 test, the `partial` edit of `ModeDiffAllocationTests.cs`, plant 5, and the section 10, 11, and 14 doc edits. Build `0 Warning(s)`, `0 Error(s)`. Solution 3179 (0 failed, 3 skipped), Framework 368. Phase 2 acceptance holds.

**What's Next:**
- 📋 Phase 3 group 1 (tasks 3.1 to 3.3): the positioned definition errors, the duplicate and slot checks, the follow-on rule, and `DefinitionErrorTests`. Expect Framework +14 for the phase: 382 and solution 3193 at the close.

**Blockers:**
- None

---

## Key Files

### Core Implementation
- `src/BrutalUi.Framework/Templates/DefinitionLoader.cs` - The public loader. It holds the template table and the action map table, and it registers the `SlotContainer` type on the `TemplateLoader`.
- `src/BrutalUi.Framework/Templates/DefinitionReader.cs` - The read pass: the state, `Load`, `AddError`, the sort, and the entry structs.
- `src/BrutalUi.Framework/Templates/DefinitionReader.Elements.cs` - The walk of the read pass: one element, its place, and its attributes. Group 2 added this partial file, because one file passed the 400-line cap.
- `src/BrutalUi.Framework/Templates/DefinitionReader.Resolve.cs` - The resolve pass. It checks the names and builds the three records.
- `src/BrutalUi.Framework/Templates/DefinitionLoadResult.cs` - The records or the errors, and `RegisterTo(UiManager)`.
- `src/BrutalUi.Framework/Templates/DefinitionError.cs` - `DefinitionError` and `DefinitionErrorKind` (13 kinds).
- `src/BrutalUi.Framework/Templates/SlotContainerTemplate.cs` - The `SlotContainer` element type with the required `slot` attribute.

### Related Files
- `src/BrutalUi.Framework/Panels/PanelDefinition.cs`, `LayoutDefinition.cs`, `ModeDefinition.cs`, `UiManager.cs` - The records and the register calls. This feature does not change them.
- `src/BrutalUi.Markup/Template/TemplateLoader.cs`, `TemplateReader.cs` - The template loader and the read pattern. This feature does not change Markup (A8).
- `src/BrutalUi.Framework/Templates/TemplateBindings.cs` - The bindings that the game attaches to the `TemplateLoader`.
- `docs/features/markup/definition-loader/implementation.md` - The plan.

---

## Baseline Record (task 1.1)

- **Base commit:** `9f8f14e99daa35d27ff7d113bf8ca418f475c333` at the group start (planning-time reference: `9fa4b43`; the acceptance diff runs against `9fa4b43`). `git status --porcelain` was clean at the start.
- **Solution total, measured before any change:** 3167, failed 0, succeeded 3164, skipped 3. It equals Notes item 10.
- **`BrutalUi.Framework.Tests` total, measured before any change:** 356, failed 0, skipped 0.

## Group 2 Close Record (tasks 1.6 to 1.8)

- **Build:** `dotnet build brutal-ui.sln -nodeReuse:false` → `0 Warning(s)`, `0 Error(s)`.
- **Solution tests:** 3171 total, 0 failed, 3168 succeeded, 3 skipped. The baseline plus the 4 new tests.
- **`BrutalUi.Framework.Tests`:** 360 total, 0 failed, 0 skipped. The baseline plus 4.
- **Demo state:** `BrutalUi.Framework.Tests --filter-class '*DefinitionLoaderDemoTests'` → total 4, `failed: 0`. The four tests are `OneLoadedPanelMountsInItsSlot`, `ThePanelFactoryLoadsItsTemplateAtEachCall`, `TheLayoutTemplateBuildsASlotContainer`, and `TheEntryOrderDoesNotMatter`.
- **Plant 1** (`DefinitionReader.Resolve.cs`, the panel and layout factory caches the first root and returns it at each call): exit 2, 0 `error CS` lines, 1 failed — `ThePanelFactoryLoadsItsTemplateAtEachCall`. Restore: `cmp` reports the file identical to its copy; the re-run gives 3171, 0 failed.
- **Plant 2** (`SlotContainerTemplate.cs`, the factory ignores the value and uses the name `slot`): exit 2, 0 `error CS` lines, 3 failed — `OneLoadedPanelMountsInItsSlot`, `TheLayoutTemplateBuildsASlotContainer`, and `TheEntryOrderDoesNotMatter`. The brief named the first two. The third shares the layout template, so its `RegisterTo` throws `ArgumentException` for the unknown slot `top`. The extra kill is a stronger guard, not a fault of the plant. Restore: `cmp` reports the file identical to its copy.
- **Docs:** section 10, **Panel definitions**, holds the new `DefinitionLoader` bullet. Section 11 holds **### Definition format** before **### Stylesheet format**: the definitions text, the panel template, the layout template, the C# demo, the element table without `actions`, the entry-order rule, and the rules of The Factories. A byte compare of the definitions text against `DefinitionsText` of the demo test gives equal.
- **Protected paths:** `git diff 9fa4b43 -- src/BrutalUi.Markup src/BrutalUi.Core src/BrutalUi.Framework/Panels` gives no output.
- **File sizes:** `DefinitionReader.cs` 361 lines, `DefinitionReader.Elements.cs` 349, `DefinitionReader.Resolve.cs` 228, `DefinitionLoaderDemoTests.cs` 222. Each new source file stays under 400.
- **Verifier (post-phase, round 1):** PASS. Build `0 Warning(s)`, `0 Error(s)`. Solution 3171, 0 failed, 3 skipped; Framework 360, 0 failed. Protected-path diff empty. Section 10 bullet at line 1144; section 11 **### Definition format** at line 1635. Demo command: `*DefinitionLoaderDemoTests` total 4, `failed: 0`. Demo: PASS. UI check: none (headless test proof).

## Phase 2 Group 1 Close Record (tasks 2.1 to 2.3)

- **Build:** `dotnet build brutal-ui.sln -nodeReuse:false` → `0 Warning(s)`, `0 Error(s)`.
- **Solution tests:** 3176 total, 0 failed, 3173 succeeded, 3 skipped. The Phase 1 close plus the 5 new tests.
- **`BrutalUi.Framework.Tests`:** 365 total, 0 failed, 0 skipped. The Phase 1 close (360) plus 5.
- **Demo state:** `*DefinitionRecordTests` → total 5, `failed: 0`. `*DefinitionLoaderDemoTests` still reads total 4, `failed: 0`. The five tests are `AModeNamesItsActionMap`, `AModeWithNoActionsHasNoMap`, `APanelRefWithNoSlotUsesTheDefaultSlot`, `TheSlotOrderAndThePanelOrderFollowTheText`, and `ABoundPanelTemplateBindsThroughTheAttachedBindings`.
- **Plant 3** (`DefinitionReader.Resolve.cs`, a placement with no slot value takes the first slot of the mode instead of the panel default slot): exit 2, 0 `error CS` lines, 2 failed — `APanelRefWithNoSlotUsesTheDefaultSlot` and `TheSlotOrderAndThePanelOrderFollowTheText`. The brief named the first. The second also places one panel with a direct `PanelRef`, so it fails with the plant: a stronger guard, not a fault of the plant. Restore: `cmp` reports the file identical to its copy; the re-run gives 3176, 0 failed.
- **Plant 4** (`DefinitionReader.Resolve.cs`, `modes.Add(...)` passes `null` as the map): exit 2, 0 `error CS` lines, 1 failed — `AModeNamesItsActionMap`, as the brief expects. Restore: `cmp` reports the file identical to its copy.
- **Protected paths:** `git diff 9fa4b43 -- src/BrutalUi.Markup src/BrutalUi.Core src/BrutalUi.Framework/Panels` gives no output.
- **File sizes:** `DefinitionReader.cs` 366 lines, `DefinitionReader.Elements.cs` 373, `DefinitionReader.Resolve.cs` 263, `DefinitionRecordTests.cs` 365. Each new source file stays under 400.
- **Group 1 changed no doc of `technical-design.md`.** Task 2.6 owns the `actions` row, the direct `PanelRef` rule, and the parity rule in section 11.

## Phase 2 Group 2 Close Record (tasks 2.4 to 2.6)

- **Build:** `dotnet build brutal-ui.sln -nodeReuse:false` → `0 Warning(s)`, `0 Error(s)`.
- **Solution tests:** 3179 total, 0 failed, 3176 succeeded, 3 skipped. The group 1 close plus the 3 new tests.
- **`BrutalUi.Framework.Tests`:** 368 total, 0 failed, 0 skipped. The group 1 close (365) plus 3.
- **Demo state:** `*DefinitionParityTests` → total 2, `failed: 0`: `TheLoadedRecordsGiveTheSameModeDiffAsTheCodeRecords` and `TheLoadedRecordsHoldTheSameDataAsTheCodeRecords`. `*ModeDiffAllocationTests` → total 4, `failed: 0`. `TheToggleCycleOnLoadedRecordsAllocatesNothing` measures 0 bytes over 100 toggle steps with the loaded records, asserts the sum 1200, and reads the loaded score label equal to the store value.
- **Plant 5** (`UiManager.cs`, `GC.KeepAlive(new object());` as the first line of `UiManager.Apply`): exit 2, 0 `error CS` lines, 3 failed — both toggle gates, `TheToggleCycleOnLoadedRecordsAllocatesNothing` and `TheToggleCycleAllocatesNothingAfterWarmUp` (9600 bytes each), and `ModeDiffTests.SetModeAndTheDiffAllocateNothingAfterWarmUp`. The brief names the first two. The third shares `Apply`, so its kill is a stronger guard, not a fault of the plant. Restore: `cmp` reports the file identical to its copy; the re-run gives 3179, 0 failed.
- **Docs:** section 10, **Mode set**, holds the loaded-record bullet. Section 11, **### Definition format**, holds the `actions` and map row of the `Mode` table, the direct `PanelRef` row with the Q5 rule, the `actions` and Q5 prose, and the **Parity** paragraph. Section 14, the eighteenth-gate bullet, reads 4 tests and names `TheToggleCycleOnLoadedRecordsAllocatesNothing` and plant 5.
- **Protected paths:** `git diff 9fa4b43 -- src/BrutalUi.Markup src/BrutalUi.Core src/BrutalUi.Framework/Panels` gives no output. The diff of `ModeDiffAllocationTests.cs` shows one changed line: the `partial` keyword.
- **File sizes:** `DefinitionSet.cs` 256 lines, `DefinitionParityTests.cs` 178, `ModeDiffAllocationTests.Loaded.cs` 177. Each new file stays under 400.
- **Verifier (post-phase, round 1):** PASS, no fix round. Build `0 Warning(s)`, `0 Error(s)`. Solution 3179, 0 failed, 3 skipped; `BrutalUi.Framework.Tests` 368, 0 failed. `*DefinitionRecordTests` total 5, `*ModeDiffAllocationTests` total 4, each `failed: 0`. Protected-path diff empty; the `ModeDiffAllocationTests.cs` diff shows the one `partial` line; every listed file stays under 400 lines; no reflection use in `Definition*.cs` or `SlotContainerTemplate.cs`; plant 5 left no residue. Section 11 holds the `actions` row and the direct `PanelRef` Q5 rule, section 10 **Mode set** names `DefinitionParityTests` (line 1185), and section 14 names `TheToggleCycleOnLoadedRecordsAllocatesNothing` (line 1980). Demo: PASS. UI check: none (headless test proof). Faults: none.

## Tool Notes

- Export `DOTNET_CLI_HOME=/tmp/dui-cli-home` and `MSBUILDDISABLENODEREUSE=1` in each command. Give `-nodeReuse:false` to `dotnet build` only.
- The end-of-group pair: `dotnet build brutal-ui.sln -nodeReuse:false 2>&1 | tail -n 30` and `dotnet test --solution brutal-ui.sln 2>&1 | tail -n 30`. Expect `0 Warning(s)`, `0 Error(s)`, 0 failed, and the totals above unchanged. A warning fails the build.
- Per-class run after a build: `tests/BrutalUi.Framework.Tests/bin/Debug/net10.0/BrutalUi.Framework.Tests --filter-class '*<Class>' 2>&1 | tail -n 5`.
- Plants: copy the file to `src/BrutalUi.Framework/obj/plantscratch/` (git-ignored), change it, run the solution tests into a log, and check the `error CS` count first: it must be 0, because a plant that does not compile leaves the old binary, and the old binary passes. Expect a non-zero exit. Read the failed names from the log. Restore with `cp`, check with `cmp`, and run again. A test host can exit with 134 one time; run a second time before you treat it as a fault.
- Harness note (dsh sandbox): `/tmp` is private to each shell call. Write a test log and read its totals inside one chained command, or write the log under `obj/`.

## Plan Record (recorded from `implementation.md` by task 1.1)

### Public API (end state)

```csharp
namespace BrutalUi.Framework;

public sealed class DefinitionLoader
{
    public DefinitionLoader(TemplateLoader templates);              // registers the SlotContainer type on templates
    public void AddTemplate(string name, ReadOnlySpan<byte> utf8);  // copies the text
    public void AddActionMap(string name, UiActionMap map);
    public DefinitionLoadResult Load(ReadOnlySpan<byte> utf8, string sourceName = "definitions");
}

public sealed class DefinitionLoadResult
{
    public IReadOnlyList<PanelDefinition> Panels { get; }   // text order. Empty when Errors holds an error
    public IReadOnlyList<LayoutDefinition> Layouts { get; }
    public IReadOnlyList<ModeDefinition> Modes { get; }
    public IReadOnlyList<DefinitionError> Errors { get; }   // sorted by line, then column
    public bool Succeeded { get; }
    public void RegisterTo(UiManager ui);                   // panels, then layouts, then modes (Q6)
}

public enum DefinitionErrorKind : byte
{
    NotWellFormed, UnknownElement, UnknownAttribute, MissingAttribute, InvalidValue, UnexpectedText,
    UnknownTemplate, UnknownActionMap, UnknownLayout, UnknownPanel, DuplicateName, UnknownSlot, TemplateFault,
}

public readonly struct DefinitionError
{
    public DefinitionErrorKind Kind { get; }
    public int Line { get; }
    public int Column { get; }
    public string Message { get; }
    public string SourceName { get; }
    public override string ToString();   // "<source>(<line>,<column>): <kind>: <message>"
}
```

The names of templates and maps are `string` keys with an ordinal compare, because `XmlReader` gives each value as a `string`. The name has no span overload, so `BRUTALCORE0011` accepts a literal name. The text of `AddTemplate` and of `Load` is a span, so a test passes a `u8` literal. `DefinitionError` has an internal constructor, as `TemplateError` has.

### The Format

```xml
<ui:Definitions xmlns:ui="brutal-ui">
  <ui:Panel name="hud" slot="top" template="hud.panel" />
  <ui:Layout name="game" template="game.layout" />
  <ui:Mode name="play" layout="game" actions="play.actions">
    <ui:Slot name="top">
      <ui:PanelRef name="hud" />
    </ui:Slot>
    <ui:PanelRef name="chat" />
  </ui:Mode>
</ui:Definitions>
```

```xml
<ui:Element pickable="false" xmlns:ui="brutal-ui">
  <ui:SlotContainer slot="top" />
  <ui:SlotContainer slot="bottom" />
</ui:Element>
```

| Element | Parent | Attributes | Record |
|---|---|---|---|
| `Definitions` | none (the root) | none | the result |
| `Panel` | `Definitions` | `name`, `slot`, `template` (all required) | `PanelDefinition(name, slot, factory)` |
| `Layout` | `Definitions` | `name`, `template` (both required) | `LayoutDefinition(name, factory)` |
| `Mode` | `Definitions` | `name`, `layout` (required), `actions` (optional) | `ModeDefinition(name, layout, placements, map)` |
| `Slot` | `Mode` | `name` (required) | a group of placements |
| `PanelRef` | `Slot` or `Mode` | `name` (required) | one `ModePanel`. A direct child of `Mode` takes the `DefaultSlot` of the panel (Q5) |
| `SlotContainer` | a layout template | `slot` (required) | `new SlotContainer(slot)`, a leaf |

The entry order in the text does not matter. A `Mode` can come before its `Panel` and its `Layout`. The placements of a mode keep the text order, and `ModeDefinition` groups them by slot in the order of the first entry of each slot.

### The Two Passes

**The read pass (`DefinitionReader.cs`).** One forward `XmlReader` walk with the settings of `TemplateReader.Load` (DTD prohibited, no resolver, comments, processing instructions, and whitespace ignored, `ConformanceLevel.Document`). Each element goes through these checks, in order: the namespace, the place under its parent, the attributes (a prefixed attribute, an unknown attribute, an empty value), and the required attributes. A frame stack tracks the parent, as the `_frames` stack of `TemplateReader` does. A failed element pushes a failed frame, so its children give no error. Each `Panel`, `Layout`, and `Mode` becomes an entry. Each value is a `DefinitionValue(string Text, int Line, int Column)` with the position of its attribute name. `default` means no value.

**The resolve pass (`DefinitionReader.Resolve.cs`).** It runs after the read pass, also when the read pass found an error, so one load gives each error. It stops only for `NotWellFormed`.
1. **The name tables.** One ordinal table for the panels, one for the layouts, and one for the modes. The first entry of a name wins. A later entry gives `DuplicateName`. The three kinds have three tables, as in `UiManager`.
2. **The panels.** The `template` value must be a registered template, or `UnknownTemplate`. The record gets a factory that loads the template at each call (A4).
3. **The layouts.** The `template` value must be a registered template, or `UnknownTemplate`. A layout that a mode names loads its template one time (Q3). A template error gives `TemplateFault` at the `Layout` element, with each `TemplateError.ToString` line in the message (A5). The walk collects the slot names of each `SlotContainer` in pre-order and does not enter a slot. Two slots of one name give `DuplicateName` at the `Layout` element, because `UiManager.Register(LayoutDefinition)` throws for them. The pass drops the tree and keeps the slot names.
4. **The modes.** The `layout` value must be a `Layout` entry of the text, or `UnknownLayout`. The `actions` value must be a registered map, or `UnknownActionMap`. Each placement names a `Panel` entry of the text, or `UnknownPanel`. A panel two times in one mode gives `DuplicateName` at the second `PanelRef`. The slot of a placement is the `Slot` name, or the `DefaultSlot` of the panel for a direct `PanelRef`. A slot that the layout does not hold gives `UnknownSlot`.
5. **The result.** One error or more gives a sorted error list and no record. No error gives the three lists in text order.

**The position rule.** An error at an element points at the first character of the element name. An error at a value points at the first character of its attribute name. This is the rule of `TemplateError`. `MissingAttribute`, `UnknownElement`, `TemplateFault`, and the duplicate slot of a template sit at the element. Each other kind sits at its attribute.

**The follow-on rule (one fault, one error).** An entry with a name stays in its name table, also when another value of the entry has a fault. Such an entry is faulty. A reference to a faulty entry gives no error. A mode on a faulty layout skips the slot check. A placement with no panel record skips the slot check. So a `Panel` with no `template` gives one `MissingAttribute`, and a `Mode` that names it gives nothing more.

### The Factories

- **A panel factory** captures the `TemplateLoader`, the template text copy, and the template name. Each call runs `templates.Load(text, templateName).GetRootOrThrow()`. The controller calls it one time, at the first mount. A fault of the template throws `InvalidOperationException` at that call, and the error rule of `UiManager` applies: the target stays, and the next `Update` tries again (A4).
- **A layout factory** is the same. `UiManager.Register(LayoutDefinition)` calls it one time. The check of Q3 found no fault in a used layout, so that call does not throw for the text.
- The template name is the source name of each `TemplateError`, so an error line reads `hud.panel(3,5): UnknownBinding: ...`.
- The factory ignores its `PanelContext`. The attached `TemplateBindings` gives the commands and the stores.

### The Rules of the Object

- **One `DefinitionLoader` for each `TemplateLoader`.** The constructor registers the type `SlotContainer`. A second `DefinitionLoader` on the same loader, or a game type with that name, makes `TemplateLoader.Register` throw `ArgumentException` in the constructor (Notes item 4).
- **One name holds one template, and one name holds one map.** A second `AddTemplate` or `AddActionMap` for one name throws `ArgumentException` before the add. `AddTemplate` copies the text, so a later change of the source changes nothing.
- **An add after a load is valid.** The next load reads the tables. An add call and a load call must not run at the same time, as for `TemplateLoader.Register`.
- **The checks of A6.** The load checks duplicates inside one text only. A name that the manager holds makes `UiManager.Register` throw `ArgumentException` in `RegisterTo`, as today. `RegisterTo` is not atomic: the records before the throw stay registered (Notes item 5).
- **A failed result.** `RegisterTo` on a result with an error throws `InvalidOperationException` with one `ToString` line for each error, separated by LF, and registers nothing. This is the form of `GetRootOrThrow`.
- **Load time and run time.** The load, the template copy, the check loads, and each factory call allocate at build time (epic A10). A loaded record holds the same data as a C# record, so the mode diff allocates nothing after warm-up.

### Notes (of the plan)

1. **A reading of A2 and Q5: a direct `PanelRef`.** The plan keeps the A2 shape and the A2 names, and adds one form: a `PanelRef` directly under `Mode` has no slot and takes the `DefaultSlot` of its panel.
2. **Conflict: A9 and the size of `ModeDiffAllocationTests.cs`.** The plan makes the class `partial` and adds `ModeDiffAllocationTests.Loaded.cs` (Decision 9).
3. **Open point: a mode that names a record of C# code.** The plan resolves each layout and panel of a mode against the same text (Decision 6). Options: (a) the same text only (recommended, the plan). (b) Accept an unknown name and leave the fault to `UiManager.Register`. (c) A list of known C# names before the load.
4. **Open point: one `DefinitionLoader` for each `TemplateLoader`.** A second `DefinitionLoader` on one loader throws `ArgumentException`. Options: (a) document and pin the throw (recommended, the plan). (b) Catch it (it hides a game type with the same name). (c) A public `TemplateLoader.IsRegistered` (changes Markup, against A8).
5. **Open point: `RegisterTo` is not atomic.** The records before the throw stay registered. Options: (a) document it (recommended, the plan). (b) A name query on `UiManager` in a later feature.
6. **A reading of Q3: an unused layout.** A bad template of a layout that no mode names gives no load error; `RegisterTo` throws from its factory. The plan follows Q3.
7. **Conflict: sections of `technical-design.md`.** Task 2.6 also edits the eighteenth-gate bullet in section 14.
8. **The whitespace of a value.** Each name value is used as is. Only an empty value gives `InvalidValue`.
9. **A Slot with no PanelRef.** An empty `Slot` adds no placement; the slot check still runs on its name.
10. **The planned totals** come from the binding-attributes close: 3167 with 3 skipped at `9fa4b43`. Task 1.1 records the real baseline above.

### The commands (of Implementation Phases)

Export `DOTNET_CLI_HOME=/tmp/dui-cli-home` and `MSBUILDDISABLENODEREUSE=1` in each command. Give `-nodeReuse:false` to `dotnet build` only. Each task group ends with `dotnet build brutal-ui.sln -nodeReuse:false 2>&1 | tail -n 30` and `dotnet test --solution brutal-ui.sln 2>&1 | tail -n 30`. Expect `0 Warning(s)`, `0 Error(s)`, and 0 failed. A warning fails the build. Run one class after a build with `tests/BrutalUi.Framework.Tests/bin/Debug/net10.0/BrutalUi.Framework.Tests --filter-class '*<Class>' 2>&1 | tail -n 5`. The projects have no implicit usings, so each new file holds its own `using` lines. Use file-scoped namespaces. The source namespace is `BrutalUi.Framework`, and the test namespace is `BrutalUi.Framework.Tests`.

### The plants (of Implementation Phases)

For each plant, copy the file to `src/BrutalUi.Framework/obj/plantscratch/` (git-ignored), make the change, and run the solution tests with `dotnet test --solution brutal-ui.sln > <scratch>/pN.log 2>&1; echo "exit=$?"`. Check the `error CS` count first. It must be 0, because a plant that does not compile leaves the old binary, and the old binary passes. Expect an exit code that is not 0. Read the failed names from the log. Restore with `cp`, check with `cmp`, and run again. A test host can exit with 134 one time. Run a second time before you treat it as a fault.

---

## Important Decisions

### Decision 1: The confirmed interview items
**Date:** 2026-11-18
**Context:** Open points 7 and 8 of xml-element-loader left the template registry and the error type to this feature.
**Decision:** A template is the UTF-8 XML text, added by name with `AddTemplate` (Q1, A3). The errors use a new `DefinitionError` struct in Framework (Q4). The full list is in `implementation.md` under `### Confirmed Assumptions`.
**Rationale:** The coordinator chose each recommended option, and the stop check came back "all hold".
**Impact:** Markup stays unchanged. Each factory loads its template through the shared `TemplateLoader`.

### Decision 2: The approach
**Date:** 2026-11-18
**Context:** The loader must give exact positions, and a mode can name a layout or a panel that comes later in the text.
**Decision:** Two passes, in the style of `TemplateReader`: a read pass into an entry list with positions, then a resolve pass.
**Rationale:** The order of entries does not matter, and each error keeps its position.
**Impact:** The plan splits the reader into `DefinitionReader.cs` and `DefinitionReader.Resolve.cs`.

### Decision 3: The SlotContainer factory keeps a defensive error path
**Date:** 2026-11-18
**Context:** The `slot` row is `CreateRequired`, so the loader records `MissingAttribute` before it calls the factory. `args.TryGet("slot", ...)` therefore never gives `false` in a load. A null factory result with no new error, however, makes the loader throw `InvalidOperationException`.
**Decision:** If `TryGet` gives `false`, the factory calls `args.AddError(TemplateErrorKind.MissingAttribute, ...)` and returns `null`. The guard keeps the factory contract in every path.
**Rationale:** The path is unreachable in a load; the cost is three lines.
**Impact:** `SlotContainerTemplate.cs` only.

### Decision 4: The duplicate add guards follow in a later group
**Date:** 2026-11-18
**Context:** The Rules of the Object say a second `AddTemplate` or `AddActionMap` for one name throws `ArgumentException` before the add. Task 1.5 names only the `ArgumentNullException` checks for this group ("Each throws `ArgumentNullException` for a `null` argument in this phase").
**Decision:** Group 1 stores with the dictionary indexer and documents the end-state rule in the class remarks. Task 3.4 owns the guard: it edits `DefinitionLoader.cs` so `AddTemplate` and `AddActionMap` throw `ArgumentException` for a duplicate name.
**Rationale:** The brief pins the checks of this phase, and the plan gives task 3.4 as the owner.
**Impact:** See the record in Gotchas. `AddTemplate` and `AddActionMap` overwrite a name until task 3.4 runs.

### Decision 5: The read pass spans two partial files
**Date:** 2026-11-18
**Context:** Task 1.6 with the entry structs, `Load`, the walk, and the BRUTAL doc style reached 703 lines in one file. The rule for a new source file is 400 lines.
**Decision:** Keep `DefinitionReader.cs` for the state, `Load`, `AddError`, the sort, and the entry structs, and move the walk to `DefinitionReader.Elements.cs`. This follows the existing split of `TemplateReader.cs` and `TemplateReader.Elements.cs`. `DefinitionReader.Resolve.cs` stays one file at 228 lines.
**Rationale:** A split on the same seam as Markup keeps each file readable and adds no new kind of file. The plan already notes a partial split for `ModeDiffAllocationTests` (Notes item 2).
**Impact:** Three files instead of two for group 2. Tasks 2.1 and 3.1 edit `DefinitionReader.Elements.cs` for the walk, and `DefinitionReader.cs` for the entries and the value rules. See the record in Gotchas.

### Decision 6: Phase 1 reports the faults that a record constructor would hit
**Date:** 2026-11-18
**Context:** Task 1.6 names `UnknownElement`, `UnknownAttribute`, and `MissingAttribute` for this phase and leaves the other faults to phase 3. An empty `name` or `slot` value, however, reaches a record constructor, which throws `ArgumentException`, and `Load` must never throw for the text. One panel two times in one mode has the same shape: `ModeDefinition` throws.
**Decision:** The read pass reports an empty required value as `InvalidValue` with a short message, and the resolve pass skips an entry or a placement whose required value is absent or empty, so one fault gives one error. The resolve pass also keeps the first placement of one panel in one mode. Phase 3 replaces the message wording and adds `DuplicateName` at the later entry; nothing else changes.
**Rationale:** The public contract of `Load` ("the method never throws for the text") holds from phase 1, and no input gives a silent success with a missing record. Each check is three lines.
**Impact:** `DefinitionReader.Elements.cs` (check 3c) and `DefinitionReader.Resolve.cs` (the skip and the first-placement rule).

### Decision 7: The read split keeps the required names in their own table
**Date:** 2026-11-18
**Context:** Task 2.1 makes `actions` an optional attribute of `Mode`. Phase 1 reads one list per element kind, and check 4 reports `MissingAttribute` for every name in it, so the list cannot hold both a required name and an optional one.
**Decision:** `AllowedNames(kind)` holds the required names plus `actions` for `Mode`, and a new `RequiredNames(kind)` holds only the required ones. Check 3b and the unknown-attribute message read the allowed list; check 4 reads the required list. A mode with no `actions` gives no error, so the value stays absent and the resolve pass gives no map.
**Rationale:** Two small tables keep the order of the checks of `The Two Passes` and need no flag inside the loop. The Phase 3 messages of task 3.1 name both lists already: they list the attributes of the element, `actions` included.
**Impact:** `DefinitionReader.Elements.cs` only.

### Decision 8: The resolve pass keeps the panel records by entry index
**Date:** 2026-11-18
**Context:** Task 2.2 needs the `DefaultSlot` of the panel record for a `PanelRef` directly under `Mode`. The `panelNames` table gives the entry index of the first entry of one name, but a faulty entry (no `template`, an unknown template) is in that table and has no record, and `Load` must not throw for the text.
**Decision:** Step 2 fills `PanelDefinition?[] panelRecords` beside the `panels` list. Step 4 reads the record at the entry index of its name: with a record it builds the `ModePanel` from `panelRecord.DefaultSlot`; with no record it skips the placement, because the fault of that panel entry already has its error.
**Rationale:** One array of the entry count costs no search, keeps the follow-on shape of Decision 6, and gives one fault one error. Phase 3 replaces the skip with the follow-on rule for the same entries.
**Impact:** `DefinitionReader.Resolve.cs` only. The skip of a direct `PanelRef` on a faulty panel is the shape that task 3.2 keeps.

### Decision 11: DefinitionSet returns a nested Records type
**Date:** 2026-11-18
**Context:** Task 2.4 needs the three record lists of both paths in text order. `DefinitionLoadResult.Success` is internal, and `BrutalUi.Framework` keeps no `InternalsVisibleTo` for the test assembly (A10), so `RegisterCode` cannot wrap the C# records in a `DefinitionLoadResult`.
**Decision:** `DefinitionSet.Load` and `DefinitionSet.RegisterCode` each return the nested `internal sealed class DefinitionSet.Records`, which holds the three arrays. `Load` copies the lists of the result into it, and `RegisterCode` builds it from the records it registers.
**Rationale:** One small internal carrier gives both tests one loop shape, adds no public API, and keeps the rule "do not guess on a public API shape". The change needs no new production type.
**Impact:** `DefinitionSet.cs` and `DefinitionParityTests.cs` only.

### Decision 12: The loaded gate scene carries no inline size and no field focus
**Date:** 2026-11-18
**Context:** The C# `BuildScene` sets `Inline` sizes on the field, the add button, and the layout root, and gives the focus to the field. The template format has no style row, so a template cannot set an `Inline` member, and the task 2.5 brief lists no size and no focus step.
**Decision:** `BuildLoadedScene` builds the same trees with no size and no focus, and reuses the existing `ToggleStep` as the measured body. `TreeCompare` reads no size, and the step sum and the 0-byte rule do not depend on the focus. The loaded score label keeps no name: the test reads it by child index, state root child 1, so the two shapes stay equal.
**Rationale:** The overview of task 2.5 pins the build steps, and the parity rule of A9 needs the step body and the tree shape, not the size.
**Impact:** `ModeDiffAllocationTests.Loaded.cs` only.

---

## Gotchas & Learnings

- Read the Notes of `implementation.md` before phase 1. Item 4: a second `DefinitionLoader` on one `TemplateLoader` throws from `TemplateLoader.Register`. Item 5: `RegisterTo` is not atomic. Item 6: a bad layout that no mode names gives no error at load time.
- The reader lives in three partial files: `DefinitionReader.cs` (state, `Load`, `AddError`, `HoldsNotWellFormed`, `SortErrors`, and the entry structs), `DefinitionReader.Elements.cs` (the walk: `ReadElement`, `ReadAttributes`, the place and attribute tables, and the frame helpers), and `DefinitionReader.Resolve.cs` (the resolve pass). Tasks 2.1, 2.2, 3.1, and 3.2 name only `DefinitionReader.cs` and `DefinitionReader.Resolve.cs`. The walk edits of tasks 2.1 and 3.1 belong in `DefinitionReader.Elements.cs`; the `DefinitionReader.cs` edits of those tasks are the entries and the value rules.
- `Load` gives one `NotWellFormed` error and stops the resolve pass for it, so a broken text gives no follow-on name error. A text with no root gives `NotWellFormed` at 1,1.
- The resolve pass skips an entry whose required value is absent or empty. The read pass already named that fault, so one fault gives one error. Phase 3 keeps this shape and adds the follow-on rule for the kinds that a faulty entry causes elsewhere.
- A mode with one panel two times: the resolve pass keeps the first placement and drops the second, because `ModeDefinition` rejects a repeated panel and `Load` must not throw. Phase 3 reports `DuplicateName` at the second `PanelRef` (task 3.2).
- An `actions` attribute and a `PanelRef` directly under `Mode` read as the real forms since task 2.1 and 2.2. A mode with no `actions` gets no map, and a placement with no slot value takes the `DefaultSlot` of its panel record (Q5). The Phase 1 demo text and the section 11 block of `docs/technical-design.md` hold neither form, so no existing test changed. Task 3.1 owns the wording of the unknown-attribute message, which now lists `name, layout, actions` for `Mode`.
- Check 3b and the unknown-attribute message read `AllowedNames(kind)`; check 4 reads `RequiredNames(kind)`. Keep the two tables in step when you add a name (Decision 7). A `Mode` table holds `actions` in the allowed list only.
- `actions=""` gives `InvalidValue` in the read pass and no map in the resolve pass, because `HasName` is false. A direct `PanelRef` whose panel entry has no record gives no placement and no second error (Decision 8). Phase 3 replaces both skips with the follow-on rule wording and keeps the one-fault-one-error shape.
- `DefinitionValue.HasName` is the single test that the resolve pass uses for a value. It is false for no value and for an empty value, so no record constructor sees an empty name and `Load` never throws for the text.
- The internal members that group 2 consumes: `DefinitionLoader.Templates`, `TryGetTemplate(string, out byte[]?)`, `TryGetActionMap(string, out UiActionMap?)`, `DefinitionLoadResult.Success`, and `DefinitionLoadResult.Failure`. The `out byte[]?` and `out UiActionMap?` parameters are nullable; do not read them after a false return.
- Group 2 of phase 2 keeps to the public loader API: the test project holds no `InternalsVisibleTo` for `BrutalUi.Framework`, so `DefinitionSet` uses `DefinitionLoadResult.Success` nowhere (Decision 11).
- The diff parity test needs two map instances: one map is active on one context at most, and `Activate` on the second manager's context would clear the slots of the first (the switch rule of `UiActionMap`). The data parity test needs one map instance: it asserts the `ActionMap` reference equality of the two records.
- `DefinitionSet.Load` makes one `TemplateLoader` and one `DefinitionLoader` in each call, so each manager gets a fresh `SlotContainer` registration and no second-loader throw (Notes item 4).

---

## Testing Approach

- Framework tests in `tests/BrutalUi.Framework.Tests/Templates/` and `Panels/`. The plan adds 26 tests and 9 plants. The planned solution total is 3193 with 3 skipped, from 3167 at `9fa4b43`.
- `ModeDiffAllocationTests` gets one case on loaded records at 0 bytes after warm-up (A9).

---

## Needs Human Verification

- [ ] Phase 1 demo: the developer runs `BrutalUi.Framework.Tests --filter-class '*DefinitionLoaderDemoTests'` and reads a total of 4 with `failed: 0`. In `OneLoadedPanelMountsInItsSlot`, a test loads one text with one panel, one layout, and one mode, registers them, sets the mode, runs one `Update`, and finds the loaded panel root, with its `score` label, as the one child of the slot `top`.
- [ ] Phase 2 demo: the developer runs `BrutalUi.Framework.Tests --filter-class '*DefinitionParityTests'` and reads a total of 2 with `failed: 0`. After each of five mode steps, with a layout swap and a back step, the layout root of the loaded manager and the layout root of the C# manager compare equal under `TreeCompare`, with the same panels in the same slots.

---

## Next Steps

### Immediate
1. Phase 3 group 1 (tasks 3.1 to 3.3): the positioned definition errors, the duplicate and slot checks, the check load of a used layout, the follow-on rule, the message wording, and `DefinitionErrorTests` with 10 tests.
2. Phase 3 group 2 (tasks 3.4 to 3.6): the argument checks, `DefinitionLoaderApiTests`, the docs, the changelog line, and the package run.

### Future
1. markup-sample loads the HelloPanel records through this API.

---

## Open Questions

- (none. The Notes of `implementation.md` hold the open points for review.)

---

## Session Notes

### 2026-11-18 16:40
- The plan interview ran through the Orca coordinator. Each recommended option was chosen, and the assumptions hold.
- The solution-architect agent wrote `implementation.md`. `/start-feature` wrote `tasks.md`, this file, and `inbox.md`.
- The next session starts phase 1.

### 2026-11-18 17:56 (phase 1, group 1)
- Tasks 1.1 to 1.5 done. New files: `DefinitionError.cs`, `SlotContainerTemplate.cs`, `DefinitionLoadResult.cs`, `DefinitionLoader.cs`, and the `DefinitionReader.cs` placeholder. No shared file changed.
- Baseline measured at the group start: solution 3167 (0 failed, 3 skipped), Framework.Tests 356 (0 failed, 0 skipped). Base commit `9f8f14e`, planning reference `9fa4b43`. The recorded plan sections and the tool notes are above.
- End-of-group run: build `0 Warning(s)`, `0 Error(s)`. Solution tests 3167, 0 failed, 3 skipped, unchanged. `git diff 9fa4b43 -- src/BrutalUi.Markup src/BrutalUi.Core src/BrutalUi.Framework/Panels` is empty.
- Blast radius: the `DefinitionLoader` constructor calls `TemplateLoader.Register` (documented throw). `RegisterTo` calls the three `UiManager.Register` overloads. The record constructors and `TemplateLoader.Load` run in group 2 through the reader. Nothing existing reads the new types yet, so the risk is confined to the new files.
- Migration risks and follow-up: the placeholder throws `NotImplementedException`; group 2 replaces it and adds the demo tests. Task 3.4 adds the duplicate-add guards. The `RegisterTo` throw stays non-atomic (Notes item 5).

### 2026-11-18 18:36 (phase 1, group 2)
- Tasks 1.6 to 1.8 done. New: `DefinitionReader.Elements.cs` and `DefinitionReader.Resolve.cs`; replaced: `DefinitionReader.cs`; new test file `DefinitionLoaderDemoTests.cs`; edited `technical-design.md` (section 10 bullet, section 11 **### Definition format**) and these docs.
- Counts, both plants, and the demo state are in the Group 2 Close Record above. Phase 1 acceptance holds, and no git write ran.
- Blast radius, the four integration points that group 2 reached: `TemplateLoader.Load` and `TemplateLoadResult.GetRootOrThrow` inside each record factory; `PanelDefinition`, `LayoutDefinition`, and `ModeDefinition` with `ModePanel`, built by the resolve pass; `UiManager.Register` for the three kinds through `RegisterTo`; `SlotContainer` built by the registered `SlotContainerTemplate` type. Nothing shared changed: the protected-path diff is empty, and no existing test needed an edit.
- Migration risks and follow-up: the reader is now three partial files (Decision 5), so tasks 2.1 and 3.1 must open `DefinitionReader.Elements.cs`. Phase 1 gives `UnknownAttribute` for `actions` and `UnknownElement` for a direct `PanelRef`, and no duplicate, slot, check-load, or follow-on rule yet; tasks 2.1, 2.2, 3.1, and 3.2 own those. `RegisterTo` stays non-atomic (Notes item 5). Task 3.4 still owns the duplicate-add guards.

### 2026-11-18 18:50 (phase 1 close, Orca worker)
- Phase 1 is done: group 1 (tasks 1.1 to 1.5, the public types) and group 2 (tasks 1.6 to 1.8, the reader, the demo, and the docs), both by a `game-dev-advanced` agent. The verifier passed on round 1 with Demo: PASS. No fix round ran. No user message reached the inbox; no inbox line is open.
- The Phase 1 demo line went to Needs Human Verification above. The coordinator copies it to `docs/features/markup/human-testing.md` after the merge into the epic branch, so this run wrote no line there.
- No git write ran in this phase. The verdict file is `.beast-mode/briefs/markup/definition-loader/orca-phase-1.verdict.md`.

### 2026-11-18 19:08 (phase 2, group 1)
- Tasks 2.1 to 2.3 done. Edits: `DefinitionReader.cs` (the `Actions` value of the mode entry, the `PlacementEntry.Slot` note, the scope remarks), `DefinitionReader.Elements.cs` (the `actions` read, the `AllowedNames` and `RequiredNames` split, the `PanelRef` under `Mode` in the place table and the placement read), `DefinitionReader.Resolve.cs` (the `panelRecords` array, the `TryGetActionMap` lookup with `UnknownActionMap` at the attribute, the map pass-through, the default-slot placement rule). New test file: `tests/BrutalUi.Framework.Tests/Templates/DefinitionRecordTests.cs` with 5 tests.
- Counts, both plants, and the demo state are in the Phase 2 Group 1 Close Record above. `git diff 9fa4b43 -- src/BrutalUi.Markup src/BrutalUi.Core src/BrutalUi.Framework/Panels` stays empty: the group changed no shared file, and no existing test needed an edit.
- Blast radius: the group touched the reader only. The new resolve path calls `DefinitionLoader.TryGetActionMap`, `ModeDefinition(name, layout, panels, map)` with the map argument, and reads `PanelDefinition.DefaultSlot`. No record, manager, or Markup type changed.
- Migration risks and follow-up: group 2 (tasks 2.4 to 2.6) owns the parity test, `DefinitionSet.cs`, the loaded allocation case, plant 5, and the section 10, 11, and 14 doc edits. Expect Framework 368 and solution 3179 after it: group 2 adds 3 tests. Phase 3 still owns the duplicate check, the slot check, the check load of a used layout, the follow-on rule, and the message wording.
- No inbox line was open at the group start, and no user message reached this run. No git write ran: no commit, branch, merge, stash, reset, or push.

### 2026-11-18 19:37 (phase 2, group 2)
- Tasks 2.4 to 2.6 done. New files: `tests/BrutalUi.Framework.Tests/Shared/DefinitionSet.cs`, `tests/BrutalUi.Framework.Tests/Templates/DefinitionParityTests.cs` (2 tests), and `tests/BrutalUi.Framework.Tests/Panels/ModeDiffAllocationTests.Loaded.cs` (1 test). Edits: the class line of `ModeDiffAllocationTests.cs` (`partial`, one changed line against `9fa4b43`), sections 10, 11, and 14 of `technical-design.md`, and these docs.
- Counts, plant 5, the demo state, and the protected paths are in the Phase 2 Group 2 Close Record above. Phase 2 acceptance holds: the loaded toggle gate measured 0 bytes on the first run, and the parity classes read 2 and 4 with `failed: 0`.
- Blast radius: tests and docs only. The group changed no `src/` file; the plant restore left `cmp`-equal files and the protected-path diff is empty. The new tests consume the public `DefinitionLoader`, `DefinitionLoadResult.RegisterTo`, `TemplateBindings`, `UiManager`, and the record types. `ModeDiffAllocationTests` became `partial`; no existing test needed an edit.
- Migration risks and follow-up: phase 3 owns the duplicate check, the slot check, the check load of a used layout, the follow-on rule, the message wording, the duplicate-add guards (Decision 4), and `DefinitionLoaderApiTests`. Expect Framework 382 and solution 3193 at the phase 3 close.
- No inbox line was open at the group start, and no user message reached this run. No git write ran: no commit, branch, merge, stash, reset, or push.

### 2026-11-18 19:45 (phase 2 close, Orca worker)
- Phase 2 is done: group 1 (tasks 2.1 to 2.3, the record forms) and group 2 (tasks 2.4 to 2.6, the parity, the loaded gate, and the docs), each by one `game-dev` agent at the STANDARD tier, one agent at a time. The verifier passed on round 1 with Demo: PASS; no fix round ran, and no `bug` line was open, so no fix brief was needed.
- The Phase 2 demo line went to Needs Human Verification above, and only there. This run wrote no line to `docs/features/markup/human-testing.md`: the coordinator copies the open human tests here after it merges the feature into the epic branch.
- `tasks.md`: Phase 2 6/6, progress 14/20, and the four Phase 2 acceptance boxes checked on the verifier evidence.
- No git write ran in this phase. The verdict file is `.beast-mode/briefs/markup/definition-loader/orca-phase-2.verdict.md`.

---

*Update this file at the end of each work session. Run `/update-feature` before compacting conversations.*
