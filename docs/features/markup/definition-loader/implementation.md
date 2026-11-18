# Definition Loader - Implementation Plan

**Epic:** markup (feature 5 of 6)
**Status:** In Progress
**Started:** 2026-11-18
**Target Completion:** TBD
**Last Updated:** 2026-11-18 16:40

---

## Quality Bar

**Target quality:** A public library API that reads the panel, layout, and mode records from one XML text. markup-sample builds on it, and a game calls it one time at build time for each definition text. Four things carry the feature.

- **Reliability:** `DefinitionLoader.Load` never throws for the text. Each fault of the text gives an error, and a result with an error holds no record. A result with no error registers in `UiManager` with no throw from a fault of the text: the loader checks each name, each duplicate, each slot, and each used layout template before it gives the records. Each `Add*` call and the constructor check their arguments, so a bad setup fails at the call, not in the load.
- **Exact errors with positions:** Each fault gives one `DefinitionError` with the right kind, the line and the column of `IXmlLineInfo`, the source name, and a message that quotes the text at fault and names the fix. One fault gives one error and no follow-on error. Each of the 13 kinds has a test that asserts the kind, the line, and the column.
- **Parity with the C# records:** A record from XML and a record from C# give the same mode diff in `UiManager`. `DefinitionParityTests` registers both forms in two managers, runs the same mode steps, and compares the layout root with its mounted panels under `TreeCompare` after each step.
- **Zero allocation in a mode switch:** A mode switch on loaded records allocates 0 bytes after warm-up. `ModeDiffAllocationTests` gets one case on loaded records, and a plant fails it.

Each public member has an XML doc comment in ASD-STE100 prose (CS1591 is an error). Each new source file stays under 400 lines. Each phase plants a fault and proves that a named test fails.

---

## Executive Summary

This feature adds the record loader to `src/BrutalUi.Framework/Templates/`. It uses the `TemplateLoader` of xml-element-loader and the `TemplateBindings` of binding-attributes. Markup, Core, and `src/BrutalUi.Framework/Panels/` do not change.

- A new public class `DefinitionLoader` takes one `TemplateLoader` with `TemplateBindings` attached (A3). Its constructor registers a `SlotContainer` element type with a required `slot` attribute on that loader (Q2, A8). The game adds each template text with `AddTemplate(name, utf8)` and each action map with `AddActionMap(name, map)`, and then calls `Load(utf8, sourceName)`.
- The load runs two passes (the Chosen Approach). `DefinitionReader` reads each entry of the text with `XmlReader` into an entry list with the line and the column of each value. The resolve pass then checks the names, loads each used layout template one time, walks it for the slots, drops the tree (Q3), and builds the three records.
- Each panel record and each layout record gets a factory that calls `TemplateLoader.Load` on the template text and `GetRootOrThrow` at each call (Q1, A4).
- The result is a new public class `DefinitionLoadResult` with the three record lists and the errors. `RegisterTo(UiManager)` registers the panels, then the layouts, then the modes (Q6).
- A new public struct `DefinitionError` and enum `DefinitionErrorKind` give the errors in the shape of `TemplateError` (Q4).

The plan has 3 phases and 20 tasks, about 190 steps. Phase 1 (8 tasks) loads one panel, one layout, and one mode, registers them, sets the mode, and finds the panel root in the slot (Q7). Phase 2 (6 tasks) adds the action map, the default slot, the parity test, and the 0-byte case. Phase 3 (6 tasks) adds the duplicate check, the slot check, the template fault, each structural error, the argument checks, and the changelog line. Phase 1 needs an advanced dev agent, because it fixes four public types and touches four integration points. Phases 2 and 3 are STANDARD.

---

## User Personas Alignment

No `docs/user-personas.md` file exists. The personas come from `docs/mission-statement.md` and the epic docs.

**Target Users:**
- [ ] **The markup author** writes the panels, the layouts, and the modes of a game in one XML text, and gets an error with a line, a column, and a fix for each bad name or slot.
- [ ] **The game developer** adds each template and each action map by name in C#, loads the text one time, and calls `RegisterTo` with no other wiring code.
- [ ] **The framework contributor** of markup-sample loads the HelloPanel records from one XML file through this API.

**User Subsets Served:**
- [ ] **Simple** - one panel, one layout with one slot, and one mode: three `AddTemplate` calls, one `Load`, and one `RegisterTo`.
- [ ] **Complex** - two layouts, several panels with bound templates, three modes with action maps, a panel that takes its default slot, and a layout swap between two modes.

**UX Approach:**
- Load time: the loader reads, checks, and builds at build time, and it allocates (epic A10). Each fault gives an error and no record.
- Run time: a loaded record is the same as a C# record. `UiManager` sees no difference.
- No file IO, no search path, no hot reload, no action map in XML, and no `WindowDefinition` (requirements Out of Scope).

---

## Goals

### Primary Goals
1. Read the panel, layout, and mode records from one XML text in `BrutalUi.Framework`, or give a list of errors (requirements line 12).
2. Build each panel root and each layout root from a template that the game registers by name (requirements lines 13, 14, and 16, Q1).
3. Give each mode its layout, its slots with their panels, and its action map by name (requirements line 15, Q5).
4. Report each unknown name, each duplicate name, and each slot that the layout does not hold with a line and a column (requirements line 17, Q3, Q4).
5. Prove the parity with C# records and the 0-byte mode switch (requirements lines 18 and 19).
6. Document the format in sections 10 and 11 of `technical-design.md` (requirements line 20).

### Success Criteria
- [ ] The phase 1 demo passes: `DefinitionLoaderDemoTests` reads a total of 4 with `failed: 0`.
- [ ] The phase 2 demo passes: `DefinitionParityTests` reads 2 and `ModeDiffAllocationTests` reads 4, each with `failed: 0`.
- [ ] The phase 3 demo passes: `DefinitionErrorTests` reads 10 and `DefinitionLoaderApiTests` reads 4, each with `failed: 0`.
- [ ] Each existing test passes with no edit. The planned delta is Framework +26, so the solution grows from 3167 to 3193 with 3 skipped. Task 1.1 records the real baseline, and the deltas count from it.

---

## Architecture Overview

### The Place in the Epic

definition-loader is feature 5 of 6. It uses `TemplateLoader.Register`, `TemplateLoader.Load`, `TemplateLoadResult.GetRootOrThrow`, `ElementCreateArgs`, and `TemplateError.ToString` of xml-element-loader with no change. It uses `TemplateBindings` of binding-attributes through the loader that the game gives. It uses the records, `SlotContainer`, and `UiManager.Register` of panels-and-modes with no change. It settles open points 7 (the template registry) and 8 (the error type) of `docs/features/markup/xml-element-loader/implementation.md`.

```
BrutalUi.Framework (references BrutalUi.Core and BrutalUi.Markup, no new edge)
  Templates/
    DefinitionLoader.cs           public: the constructor, AddTemplate, AddActionMap, Load
    DefinitionLoadResult.cs       public: Panels, Layouts, Modes, Errors, Succeeded, RegisterTo
    DefinitionError.cs            public: DefinitionErrorKind, DefinitionError
    DefinitionReader.cs           internal: the read pass and the entry structs
    DefinitionReader.Resolve.cs   internal: the resolve pass and the record build
    SlotContainerTemplate.cs      internal: the ElementTypeInfo of SlotContainer
        |
        +--> BrutalUi.Markup: TemplateLoader.Register, TemplateLoader.Load, GetRootOrThrow, ElementCreateArgs
        +--> BrutalUi.Framework: PanelDefinition, LayoutDefinition, ModeDefinition, ModePanel,
        |                        SlotContainer, UiManager.Register, UiActionMap
        +--> System.Xml: XmlReader with the settings of TemplateReader
```

**What markup-sample inherits.** HelloPanel makes one `TemplateLoader`, attaches its `TemplateBindings`, calls its generated register method, and gives the loader to a `DefinitionLoader`. It adds each template file and each action map by name, loads the definition file as bytes, prints `DefinitionError.ToString` for each error and exits with code 1, and calls `RegisterTo`. A layout template writes `pickable="false"` on a root that fills the window (the layout-root rule of `UiManager`).

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

---

## Implementation Phases

The feature has 20 tasks, so it has three slices. Section 11 of `.beast-mode/phase-handoff.md` holds the rules. Rule 11 binds phase 1 to the demo of Q7. The other record forms come second, because markup-sample needs the action map and two modes, and the parity test and the 0-byte case prove them. The errors come third: the success path works, and phase 3 adds the validation (rule 2).

**The agent tier.** Phase 1 is ADVANCED (opus). It fixes four public types that markup-sample builds on, and it touches four integration points: `TemplateLoader.Register` for the slot type, `TemplateLoader.Load` in the factories, the three record constructors, and `UiManager.Register` in `RegisterTo`. Phases 2 and 3 are STANDARD (sonnet). They fill the format, the checks, and the tests in the shape that phase 1 fixes.

**The commands.** Export `DOTNET_CLI_HOME=/tmp/dui-cli-home` and `MSBUILDDISABLENODEREUSE=1` in each command. Give `-nodeReuse:false` to `dotnet build` only. Each task group ends with `dotnet build brutal-ui.sln -nodeReuse:false 2>&1 | tail -n 30` and `dotnet test --solution brutal-ui.sln 2>&1 | tail -n 30` (tool rule 5). Expect `0 Warning(s)`, `0 Error(s)`, and 0 failed. A warning fails the build. Run one class after a build with `tests/BrutalUi.Framework.Tests/bin/Debug/net10.0/BrutalUi.Framework.Tests --filter-class '*<Class>' 2>&1 | tail -n 5`. The projects have no implicit usings, so each new file holds its own `using` lines. Use file-scoped namespaces. The source namespace is `BrutalUi.Framework`, and the test namespace is `BrutalUi.Framework.Tests`.

**The plants.** For each plant, copy the file to `src/BrutalUi.Framework/obj/plantscratch/` (git-ignored), make the change, and run the solution tests with `dotnet test --solution brutal-ui.sln > <scratch>/pN.log 2>&1; echo "exit=$?"`. Check the `error CS` count first. It must be 0, because a plant that does not compile leaves the old binary, and the old binary passes. Expect an exit code that is not 0. Read the failed names from the log. Restore with `cp`, check with `cmp`, and run again. A test host can exit with 134 one time. Run a second time before you treat it as a fault.

**The tests.** Follow the style of `TemplateBindingDemoTests` and `ModeDiffAllocationTests`. Pass each text as a `u8` literal. Build each context with a `RecordingRenderer` measurer and a viewport of 800 by 600, and each manager with a `PanelContext` of a new `CommandRegistry`, `EventBus`, and `ServiceRegistry`. Find a slot with `SlotContainer.Find(context.Roots[0], "top"u8)` and an element with `root.Find<T>("name"u8)`. Each error assert checks `Kind`, `Line`, and `Column`, and checks the message with `Contains` on the quoted text only. Compute each column from the text, as `AttributeHookTests.AssertAttribute` of the Markup tests does.

### Phase 1: One Panel, One Layout, One Mode
**Goal:** `DefinitionLoader`, `DefinitionLoadResult`, `DefinitionError`, and `DefinitionErrorKind` exist with the API of the end state. The constructor registers the `SlotContainer` type. The read pass reads `Definitions`, `Panel`, `Layout`, `Mode`, `Slot`, and `PanelRef`. The resolve pass builds the three records with template factories. `RegisterTo` registers them in the order of Q6.
**Demo:** the developer runs `BrutalUi.Framework.Tests --filter-class '*DefinitionLoaderDemoTests'` and reads a total of 4 with `failed: 0`. In `OneLoadedPanelMountsInItsSlot`, a test loads one text with one panel, one layout, and one mode, registers them, sets the mode, runs one `Update`, and finds the loaded panel root, with its `score` label, as the one child of the slot `top`.
**Needs:** none
**Agent tier:** ADVANCED (opus). This phase fixes the public API and four integration points.

**Tasks (group 1, the public types, 5 files):**
- [ ] 1.1 Create `docs/features/markup/definition-loader/context.md` when `/start-feature` did not. Run `git status --porcelain`. Record the base commit (`git rev-parse HEAD`, `9fa4b43` at planning time), the solution total with the skipped count (3167 with 3 skipped at planning time), and the total of `BrutalUi.Framework.Tests`. Record the Public API, The Format, The Two Passes, The Factories, The Rules of the Object, and the Notes of this plan. Record the tool notes: `DOTNET_CLI_HOME`, the per-class binary, and the `error CS` check of a plant.
- [ ] 1.2 Write `src/BrutalUi.Framework/Templates/DefinitionError.cs`. Add `public enum DefinitionErrorKind : byte` with the 13 members of the Public API in that order, each with a doc line that names its cause. Add `public readonly struct DefinitionError` in the shape of `TemplateError`: an internal constructor with the same checks (`ArgumentNullException` for a `null` message or source name, `ArgumentOutOfRangeException` for a line or a column below 1), `Kind`, `Line`, `Column`, `Message`, `SourceName`, `ToString` in the form `<source>(<line>,<column>): <kind>: <message>`, and an internal static `GetKindName` with an explicit `switch` and a number arm for a value outside the enum. The struct remarks state the position rule of The Two Passes and the column rule of `TemplateError` (UTF-16 units).
- [ ] 1.3 Write `src/BrutalUi.Framework/Templates/SlotContainerTemplate.cs` with `internal static class SlotContainerTemplate` and `internal static ElementTypeInfo Create()`. It returns `Name = "SlotContainer"`, `Rows = [new AttributeRow("slot", AttributeRowKind.CreateRequired)]`, `IsLeaf = true`, and a static factory. The factory reads `slot` with `args.TryGet`. An empty value calls `context.AddError(TemplateErrorKind.InvalidValue, "'slot' needs a value. Write the name of the slot, for example 'top'.")` and returns `null`. Another value returns `new SlotContainer(Encoding.UTF8.GetBytes(value))`. The value is used as is, with no trim. The remarks state the leaf rule (a template child of a slot gives `UnexpectedChild`, because the children of a slot are the mounted panels) and the base row rule (`name` changes `Element.Name` and not `SlotName`).
- [ ] 1.4 Write `src/BrutalUi.Framework/Templates/DefinitionLoadResult.cs` with `public sealed class DefinitionLoadResult`: a private constructor, the four lists as `ReadOnlyCollection` copies, `Succeeded` (`Errors.Count == 0`), internal static `Success(List<PanelDefinition>, List<LayoutDefinition>, List<ModeDefinition>)` and `Failure(List<DefinitionError>)` (an empty list throws `ArgumentException`), and `RegisterTo(UiManager ui)`. `RegisterTo` throws `ArgumentNullException` for `null`, throws `InvalidOperationException` with the LF-joined `ToString` lines for a failed result, and otherwise calls `ui.Register` for each panel, then each layout, then each mode. The class remarks state the all-or-nothing rule, the order of Q6, and the A6 rule.
- [ ] 1.5 Write `src/BrutalUi.Framework/Templates/DefinitionLoader.cs` with `public sealed class DefinitionLoader`. The constructor throws `ArgumentNullException` for a `null` loader, calls `templates.Register(SlotContainerTemplate.Create())`, and keeps the loader. `AddTemplate` stores `utf8.ToArray()` in a `Dictionary<string, byte[]>` with `StringComparer.Ordinal`. `AddActionMap` stores the map in a `Dictionary<string, UiActionMap>` with `StringComparer.Ordinal`. Each throws `ArgumentNullException` for a `null` argument in this phase. `Load` throws `ArgumentNullException` for a `null` source name and returns `new DefinitionReader(this, sourceName).Load(utf8)`. Add the internal members `Templates`, `TryGetTemplate(string, out byte[])`, and `TryGetActionMap(string, out UiActionMap)`. The class remarks state the rules of The Rules of the Object.

**Tasks (group 2, the reader, the demo, and the docs, 5 files):**
- [ ] 1.6 Write `src/BrutalUi.Framework/Templates/DefinitionReader.cs` with `internal sealed partial class DefinitionReader`. It holds the `DefinitionValue`, panel entry, layout entry, mode entry, and placement entry structs of The Two Passes, the error list, the frame stack, and `AddError`. `Load(ReadOnlySpan<byte>)` copies the settings block of `TemplateReader.Load`, walks the text, catches `XmlException` as one `NotWellFormed` error, adds `NotWellFormed` at 1,1 for a text with no root, calls `Resolve`, sorts the errors by line, then column, and gives the result. In this phase, the walk reads the elements and attributes of the table of The Format except `actions` and a `PanelRef` directly under `Mode`. An element or an attribute outside that table gives `UnknownElement` or `UnknownAttribute`, and a missing required attribute gives `MissingAttribute`, each with a short message. Phase 3 completes the messages and the other faults.
- [ ] 1.7 Write `src/BrutalUi.Framework/Templates/DefinitionReader.Resolve.cs`. `Resolve` builds the three name tables, looks up each panel template and each layout template (`UnknownTemplate` at the `template` attribute), each mode layout (`UnknownLayout` at the `layout` attribute), and each placement panel (`UnknownPanel` at the `name` attribute of the `PanelRef`). It builds each `PanelDefinition` and `LayoutDefinition` with the factory of The Factories, and each `ModeDefinition` with one `ModePanel` for each placement in text order. With no error, it gives `DefinitionLoadResult.Success` with the lists in text order. The duplicate check, the slot check, and the follow-on rule come in phase 3.
- [ ] 1.8 Write `tests/BrutalUi.Framework.Tests/Templates/DefinitionLoaderDemoTests.cs` with 4 tests. `OneLoadedPanelMountsInItsSlot` (the definition text of the new section 11 block of this task, word for word with LF line ends. The panel template holds an `Element` named `hud` with a `Label` named `score`. The layout template holds a root with `pickable="false"` and one `SlotContainer slot="top"`. The test loads, asserts `Succeeded` and one record of each kind, calls `RegisterTo`, sets the mode, runs one `Update`, and asserts that `SlotContainer.Find(context.Roots[0], "top"u8)` holds one child with the name `hud` and a `score` label). `ThePanelFactoryLoadsItsTemplateAtEachCall` (two `CreateRoot` calls give two different roots that compare equal under `TreeCompare.FirstDifference`, A4). `TheLayoutTemplateBuildsASlotContainer` (`CreateRoot` of the layout gives a root, `SlotContainer.Find` finds `top`, and its `SlotName` and `Name` are `top`, Q2). `TheEntryOrderDoesNotMatter` (a text with the `Mode` first, then the `Layout`, then the `Panel`, loads and mounts the same tree as the demo text). Edit `docs/technical-design.md`. In section 10, **Panel definitions**, add one bullet: `DefinitionLoader` (section 11) reads the three records from XML, and a loaded record is the same as a C# record. In section 11, before **### Stylesheet format**, add **### Definition format** with the demo text, the layout template, the C# code of the demo (`new DefinitionLoader(templates)`, `AddTemplate`, `Load`, `RegisterTo`), the table of The Format without `actions`, and the rules of The Factories. Run the build and the tests, and expect Framework +4. Run two plants. Plant 1: the panel factory caches the first root and returns it at each call. Expect `ThePanelFactoryLoadsItsTemplateAtEachCall` to fail. Plant 2: the `SlotContainer` factory ignores the value and uses the name `slot`. Expect `OneLoadedPanelMountsInItsSlot` and `TheLayoutTemplateBuildsASlotContainer` to fail. Record the counts, the plants, and the demo state in `context.md`.

**Estimated Steps:** about 75 (8 tasks, about nine steps for one task)

**Files (group 1, 5):** `context.md`, `DefinitionError.cs`, `SlotContainerTemplate.cs`, `DefinitionLoadResult.cs`, `DefinitionLoader.cs`
**Files (group 2, 5):** `DefinitionReader.cs`, `DefinitionReader.Resolve.cs`, `DefinitionLoaderDemoTests.cs`, `technical-design.md`, `context.md`

**Acceptance Criteria:**
- [ ] The 4 new Framework tests pass, and each existing test passes with no edit.
- [ ] `git diff 9fa4b43 -- src/BrutalUi.Markup src/BrutalUi.Core src/BrutalUi.Framework/Panels` gives no output.
- [ ] Plants 1 and 2 each give a non-zero exit code with 0 `error CS` lines, and each restored file equals its copy under `cmp`.
- [ ] The text of **### Definition format** equals the definition text of `OneLoadedPanelMountsInItsSlot`, character for character.

### Phase 2: Every Record Form, Equal to Code
**Goal:** A mode names its action map with `actions`. A `PanelRef` directly under `Mode` takes the `DefaultSlot` of its panel (Q5). A set of loaded records gives the same mode diff as the same set in C#. A mode switch on loaded records allocates 0 bytes after warm-up.
**Demo:** the developer runs `BrutalUi.Framework.Tests --filter-class '*DefinitionParityTests'` and reads a total of 2 with `failed: 0`. After each of five mode steps, with a layout swap and a back step, the layout root of the loaded manager and the layout root of the C# manager compare equal under `TreeCompare`, with the same panels in the same slots.
**Needs:** Phase 1
**Agent tier:** STANDARD (sonnet).

**Tasks (group 1, the record forms, 4 files):**
- [ ] 2.1 Edit `src/BrutalUi.Framework/Templates/DefinitionReader.cs`. Read the optional `actions` attribute of `Mode`. Read a `PanelRef` directly under `Mode` as a placement with no slot value.
- [ ] 2.2 Edit `src/BrutalUi.Framework/Templates/DefinitionReader.Resolve.cs`. Look up `actions` with `TryGetActionMap`, give `UnknownActionMap` at the `actions` attribute for an unknown name, and pass the map to `ModeDefinition`. A mode with no `actions` gets no map. A placement with no slot value takes the `DefaultSlot` of the panel record (Q5).
- [ ] 2.3 Write `tests/BrutalUi.Framework.Tests/Templates/DefinitionRecordTests.cs` with 5 tests. `AModeNamesItsActionMap` (`ActionMap` is the same instance as the added map, and after `SetMode` and one `Update`, `IsActive` is `true`). `AModeWithNoActionsHasNoMap` (`ActionMap` is `null`). `APanelRefWithNoSlotUsesTheDefaultSlot` (a direct `PanelRef` to a panel with `slot="bottom"` gives a `ModePanel` in `bottom`, and the root mounts there). `TheSlotOrderAndThePanelOrderFollowTheText` (two `Slot` elements and a direct `PanelRef` give the slot names and the panel names of `GetSlotName` and `GetPanelName` in the order of the first entry of each slot). `ABoundPanelTemplateBindsThroughTheAttachedBindings` (a `TemplateBindings` with a `Store<int>` key and a command is attached to the loader. After the mount, the label shows the store value, a store set and one `Update` change it, and a click on the button runs the command one time). Run the build and the tests, and expect Framework +5. Run two plants. Plant 3: a placement with no slot value takes the first slot of the mode. Expect `APanelRefWithNoSlotUsesTheDefaultSlot` to fail. Plant 4: the resolve pass passes `null` as the map. Expect `AModeNamesItsActionMap` to fail. Record the plants in `context.md`.

**Tasks (group 2, the parity, the gate, and the docs, 6 files):**
- [ ] 2.4 Write `tests/BrutalUi.Framework.Tests/Shared/DefinitionSet.cs` as `internal static class DefinitionSet`. It holds the template texts and one definition text: two layouts (`set.column` with the slots `top` and `bottom`, `set.overlay` with the slot `center`), four panels (each a small tree with a name, a `Label`, and a `Button`), and three modes (`set.main` and `set.detail` on `set.column`, both with one map, and `set.overlay` with no map, one direct `PanelRef`). `Load(UiManager, UiActionMap)` makes a `TemplateLoader` and a `DefinitionLoader`, adds each text and the map, loads, asserts `Succeeded`, and calls `RegisterTo`. `RegisterCode(UiManager, UiActionMap)` registers the same records in C#, with roots from the builder methods, the create values of `BuiltInElements.cs`, and `new SlotContainer(...)`. Then write `tests/BrutalUi.Framework.Tests/Templates/DefinitionParityTests.cs` with 2 tests. `TheLoadedRecordsGiveTheSameModeDiffAsTheCodeRecords` (two contexts and two managers. The steps `set.main`, `set.detail`, `set.overlay`, `History.Back()`, `set.main` run on both, each with one `Update`. After each step, assert one root in each context, `Assert.Null(TreeCompare.FirstDifference(code.Roots[0], loaded.Roots[0]))`, the same current mode name, the same `History.Count`, and the same `IsActive` of each map). `TheLoadedRecordsHoldTheSameDataAsTheCodeRecords` (each name, each default slot, each slot name and panel name of each mode, and each map reference equal in text order).
- [ ] 2.5 Edit `tests/BrutalUi.Framework.Tests/Panels/ModeDiffAllocationTests.cs`. Change `public sealed class` to `public sealed partial class` and change nothing else (Notes item 2). Write `tests/BrutalUi.Framework.Tests/Panels/ModeDiffAllocationTests.Loaded.cs` with `BuildLoadedScene()` and 1 test. The scene loads the shape of `BuildScene` from XML: the templates `mda.layout`, `mda.widgets`, `mda.state`, and `mda.info`, the map `mda.map`, and a `TemplateBindings` with the commands `mda.mode` and `mda.add` and the key `mda.score` on `scene.Score` with `s_score`. It fills `Scene.Main` and `Scene.Detail` from `result.Modes`, applies `mda.main`, runs one `Update` and one `Draw`, fills `Scene.Top` and `Scene.Bottom` with `SlotContainer.Find`, and runs one `ToggleStep`. `TheToggleCycleOnLoadedRecordsAllocatesNothing` runs 2 warm-up rounds of `RunSteps`, calls `AllocationGate.ReleaseAllocationContext()`, measures 100 toggle steps, and asserts 0 bytes, the sum `Iterations * 12`, and the text of the loaded score label equal to the store value. The measured body is the existing `ToggleStep`.
- [ ] 2.6 Edit `docs/technical-design.md`. In section 11, **### Definition format**, add the `actions` row, the direct `PanelRef` rule of Q5, and the parity rule. In section 10, **Mode set**, add one line: a loaded record gives the same diff as a C# record, and `DefinitionParityTests` guards it. In section 14, change the eighteenth gate bullet from 3 tests to 4 tests and add one sentence for `TheToggleCycleOnLoadedRecordsAllocatesNothing` and its plant. Run the build and the tests, and expect Framework +8 in this phase. Run plant 5: `GC.KeepAlive(new object());` as the first line of `UiManager.Apply`. Expect `TheToggleCycleOnLoadedRecordsAllocatesNothing` to fail (`TheToggleCycleAllocatesNothingAfterWarmUp` fails with it). Record the counts and the plants in `context.md`.

**Estimated Steps:** about 55 (6 tasks, about nine steps for one task)

**Files (group 1, 4):** `DefinitionReader.cs`, `DefinitionReader.Resolve.cs`, `DefinitionRecordTests.cs`, `context.md`
**Files (group 2, 6):** `DefinitionSet.cs`, `DefinitionParityTests.cs`, `ModeDiffAllocationTests.cs`, `ModeDiffAllocationTests.Loaded.cs`, `technical-design.md`, `context.md`

**Acceptance Criteria:**
- [ ] The 8 new Framework tests pass, and each existing test passes with no edit.
- [ ] `TheToggleCycleOnLoadedRecordsAllocatesNothing` measures 0 bytes over 100 toggle steps.
- [ ] `git diff 9fa4b43 -- tests/BrutalUi.Framework.Tests/Panels/ModeDiffAllocationTests.cs` shows one changed line: the `partial` keyword.
- [ ] Plants 3, 4, and 5 each give a non-zero exit code with 0 `error CS` lines, and each restored file equals its copy under `cmp`.

### Phase 3: The Positioned Definition Errors
**Goal:** The read pass reports each structural fault with its kind and position. The resolve pass adds the duplicate check, the load and the walk of each used layout template (Q3, A5), the slot check, and the follow-on rule. The constructor, `AddTemplate`, `AddActionMap`, and `RegisterTo` check their arguments. The docs and the changelog name the feature.
**Demo:** the developer runs `BrutalUi.Framework.Tests --filter-class '*DefinitionErrorTests'` and reads a total of 10 with `failed: 0`. A mode with `<ui:Slot name="left">` on a layout with the slots `top` and `bottom` gives one `UnknownSlot` error at the line and the column of `name`, and the result holds no record.
**Needs:** Phase 1, Phase 2
**Agent tier:** STANDARD (sonnet).

**Tasks (group 1, the errors and the error tests, 4 files):**
- [ ] 3.1 Edit `src/BrutalUi.Framework/Templates/DefinitionReader.cs`. Report each read fault with these messages. A root that is not `Definitions`: `'X' is not the root of a definition text. Write <ui:Definitions xmlns:ui="brutal-ui"> as the root.` (`UnknownElement`). An element outside the namespace: the message of `TemplateReader` step 1 (`UnknownElement`). `Window`: `'Window' is not a definition. WindowDefinition comes in a later version. The definitions are Panel, Layout, and Mode.` Another element under `Definitions`: `'X' is not a definition. The definitions are Panel, Layout, and Mode.` Under `Mode`: `'X' cannot be a child of Mode. Write Slot or PanelRef.` Under `Slot`: `'X' cannot be a child of Slot. Write PanelRef.` Under `Panel`, `Layout`, or `PanelRef`: `'X' cannot be a child of Panel. Remove it.` (each `UnknownElement`, A7). A prefixed attribute: `'p:name' is not an attribute of Panel. Write the attribute with no prefix.` Another attribute: `'colour' is not an attribute of Panel. The attributes of Panel are name, slot, template.` (each `UnknownAttribute`). A missing attribute: `Panel needs the attribute 'template'.` (`MissingAttribute`, at the element). An empty value: `'slot' needs a value. Write the name of a slot, for example 'top'.` (`InvalidValue`). Text or CDATA: `Text is not valid in a definition text. Remove the text.` (`UnexpectedText`). An entry with a name and another fault is faulty and keeps its name (the follow-on rule). The walk keeps reading after each error.
- [ ] 3.2 Edit `src/BrutalUi.Framework/Templates/DefinitionReader.Resolve.cs`. Add the duplicate check (`'hud' names a panel already. Give each panel one name.`, and the same for a layout and a mode, at the `name` attribute of the later entry). Add the panel check of a mode (`'hud' is in this mode already. A mode holds a panel one time.`, at the second `PanelRef`). Load each layout that a mode names, one time, with `Templates.Load(text, templateName)`. A failed load gives `TemplateFault` at the `Layout` element: `The template 'game.layout' of the layout 'game' does not load.` and one LF line for each `TemplateError.ToString` (A5). Walk the tree for each `SlotContainer` in pre-order, without a walk into a slot, and keep the `SlotName` copies. Two slots of one name give `DuplicateName` at the `Layout` element: `The template 'game.layout' holds two slots with the name 'top'. Give each SlotContainer one slot name.` Drop the tree. Check each placement slot: `'left' is not a slot of the layout 'game'. The slots are top, bottom.` at the `name` attribute of the `Slot`, and `The panel 'chat' has the default slot 'left', and the layout 'game' holds no such slot. Put the PanelRef in a Slot, or change the slot of the panel.` at the `name` attribute of a direct `PanelRef` (`UnknownSlot`). Complete the messages of the kinds of phase 1 and 2: `'hud.panl' is not a registered template. Add it with DefinitionLoader.AddTemplate before the load.`, `'play.map' is not a registered action map. Add it with DefinitionLoader.AddActionMap before the load.`, `'gam' is not a layout of this text. Add a Layout entry with this name.`, `'hdu' is not a panel of this text. Add a Panel entry with this name.` Apply the follow-on rule of The Two Passes.
- [ ] 3.3 Write `tests/BrutalUi.Framework.Tests/Templates/DefinitionErrorTests.cs` with 10 tests. `AnUnknownTemplateNameIsAnUnknownTemplateError` (a panel and a layout give 2 errors at their `template` attributes). `AnUnknownActionMapNameIsAnUnknownActionMapError`. `AnUnknownLayoutOrPanelNameIsAnError` (`UnknownLayout` at `layout`, `UnknownPanel` at the `name` of the `PanelRef`). `ADuplicateNameIsADuplicateNameError` (two panels, two layouts, two modes, and one panel two times in one mode give 4 errors in source order). `ASlotThatTheLayoutDoesNotHoldIsAnUnknownSlotError` (a `Slot name="left"` and a direct `PanelRef` to a panel with `slot="left"` give 2 errors, the demo case of this phase). `ALayoutTemplateFaultIsATemplateFaultAtTheLayoutEntry` (an unknown element in the layout template gives `TemplateFault` at the `Layout` element, and the message contains the `TemplateError.ToString` text. A template with two `top` slots gives `DuplicateName` at the `Layout` element. A `SlotContainer slot=""` gives `TemplateFault` with `InvalidValue` in the message. A bad layout that no mode names gives no error, Q3). `AWindowOrAnotherUnknownElementIsAnUnknownElementError` (`Window`, a `Slot` under `Definitions`, a `PanelRef` under `Panel`, an element in another namespace, and a root that is not `Definitions`). `AnAttributeFaultGivesItsKind` (`colour` and a prefixed attribute give `UnknownAttribute`, a `Panel` with no `template` gives `MissingAttribute` at the element, and `slot=""` gives `InvalidValue`). `TextAndBadXmlGiveTheirKinds` (text inside `Mode` gives `UnexpectedText`, a missing end tag gives one `NotWellFormed`, and an empty text gives `NotWellFormed` at 1,1). `OneFaultGivesOneErrorAndNoRecord` (a `Panel` with no `template` and a `Mode` that names it give one error. A `Layout` with an unknown template and a `Mode` with a `Slot` on it give one error. The errors of both passes sort by line, then column. `Panels`, `Layouts`, and `Modes` are empty. `ToString` of one error equals `defs.xml(<line>,<column>): UnknownTemplate: <message>`). Run the build and the tests, and expect Framework +10. Run three plants. Plant 6: the duplicate check skips the modes. Expect `ADuplicateNameIsADuplicateNameError` to fail. Plant 7: the slot check accepts each slot. Expect `ASlotThatTheLayoutDoesNotHoldIsAnUnknownSlotError` to fail. Plant 8: a faulty entry leaves its name table. Expect `OneFaultGivesOneErrorAndNoRecord` to fail. Record the plants in `context.md`.

**Tasks (group 2, the argument checks, the API tests, and the docs, 6 files):**
- [ ] 3.4 Edit `src/BrutalUi.Framework/Templates/DefinitionLoader.cs`. `AddTemplate` and `AddActionMap` throw `ArgumentException` for an empty name and, before the add, for a name that the table holds, each with a constant message that names the rule. The constructor remarks state the rule of one `DefinitionLoader` for each `TemplateLoader` and name the `ArgumentException` of `TemplateLoader.Register`.
- [ ] 3.5 Edit `src/BrutalUi.Framework/Templates/DefinitionLoadResult.cs`. The remarks of `RegisterTo` state that the call is not atomic and that a name that the manager holds throws `ArgumentException` (A6). Then write `tests/BrutalUi.Framework.Tests/Templates/DefinitionLoaderApiTests.cs` with 4 tests. `TheMethodsCheckTheirArguments` (a `null` loader, a `null` or empty name, a `null` map, a `null` source name, and a `null` manager throw with the parameter name). `ASecondAddForOneNameThrows` (a second `AddTemplate` and a second `AddActionMap` for one name throw `ArgumentException`, and the first value stays. A change of the source array after `AddTemplate` changes nothing). `ASecondDefinitionLoaderOnOneTemplateLoaderThrows` (the second constructor throws `ArgumentException`). `RegisterToChecksTheResultAndTheManager` (a failed result throws `InvalidOperationException` with each error line, and a later `SetMode` of its mode name throws `ArgumentException`, so the call registered nothing. A manager that holds the panel name throws `ArgumentException` in `RegisterTo`, A6). Run plant 9: `RegisterTo` skips the check of a failed result. Expect `RegisterToChecksTheResultAndTheManager` to fail.
- [ ] 3.6 Edit `docs/technical-design.md`. In section 11, **### Definition format**, add the `DefinitionErrorKind` block, a table of the 13 kinds with their causes, the position rule, the follow-on rule, the check of Q3 and A5, and the rules of the object. Add one line to `CHANGELOG.md` under `### Added`, tagged `(markup/definition-loader)`. Run the build and the tests, and expect Framework +14 in this phase and +26 in total. Run `pack`, `package-restore`, and `package` of `scripts/test-linux.sh` by hand, and expect `package-test: PASS Gauge level 0.5`. Record the counts, the plants, and the close state in `context.md`.

**Estimated Steps:** about 60 (6 tasks, about ten steps for one task)

**Files (group 1, 4):** `DefinitionReader.cs`, `DefinitionReader.Resolve.cs`, `DefinitionErrorTests.cs`, `context.md`
**Files (group 2, 6):** `DefinitionLoader.cs`, `DefinitionLoadResult.cs`, `DefinitionLoaderApiTests.cs`, `technical-design.md`, `CHANGELOG.md`, `context.md`

**Acceptance Criteria:**
- [ ] The 14 new Framework tests pass, and each existing test passes with no edit, also in `BrutalUi.Markup.Tests`.
- [ ] Each of the 13 kinds of `DefinitionErrorKind` occurs in at least one assert of `DefinitionErrorTests`.
- [ ] Plants 6 to 9 each give a non-zero exit code with 0 `error CS` lines, and each restored file equals its copy under `cmp`.
- [ ] The package step prints `package-test: PASS Gauge level 0.5`.

---

## Key Technical Decisions

### Confirmed Assumptions

- Q1 Template form: the UTF-8 XML text. Each panel and layout factory calls TemplateLoader.Load on it, with the attached TemplateBindings.
- Q2 Slot element: Framework registers a SlotContainer element type on the TemplateLoader, with a required "slot" attribute.
- Q3 Slot check: the loader loads each used layout template one time at load time, walks the tree for SlotContainer, and drops the tree.
- Q4 Error type: a new public DefinitionError struct and DefinitionErrorKind enum in Framework, in the shape of TemplateError.
- Q5 Default slot: a mode entry can omit the slot. The loader then uses PanelDefinition.DefaultSlot.
- Q6 Register step: the result holds the records, and RegisterTo(UiManager) registers panels, then layouts, then modes.
- Q7 Phase 1 demo: a test loads one XML text with one panel, one layout, and one mode, registers them, sets the mode, and finds the panel root in the slot.
- A1 The loader is a new class DefinitionLoader in src/BrutalUi.Framework/Templates/. It reads with XmlReader in the "brutal-ui" namespace, with the reader settings of TemplateLoader.
- A2 The format: one root <Definitions> that holds <Panel name slot template/>, <Layout name template/>, and <Mode name layout actions> with <Slot name> children that hold <PanelRef name/>. The architect can change the names, not the shape.
- A3 The game gives the DefinitionLoader one TemplateLoader with TemplateBindings attached. The game adds templates with AddTemplate(name, utf8) and maps with AddActionMap(name, map) before Load.
- A4 A panel template loads at each factory call. A panel template fault throws at that call through GetRootOrThrow. The load checks only the panel template name.
- A5 A layout template fault at load time (Q3) becomes a DefinitionError at the position of the Layout entry. The message holds the TemplateError text.
- A6 The load checks duplicates inside one text only. A name that the UiManager already holds throws in Register, as today.
- A7 A Window element or any other unknown element gives an UnknownElement error. The loader keeps reading after each error, as TemplateLoader does.
- A8 Markup stays unchanged. The SlotContainer type registers from Framework, through the public TemplateLoader.Register.
- A9 The parity test compares the ModeDiff result of XML records and C# records. ModeDiffAllocationTests gets one case on loaded records at 0 bytes after warm-up.

### Decision 1: Two passes over an entry list
**Choice:** `DefinitionReader` reads each entry with its value positions in one forward `XmlReader` pass. A second pass resolves the names, checks the layouts, and builds the records.
**Rationale:** This is the Chosen Approach. A forward name (a mode before its layout) resolves with no special case, so the entry order does not matter. Each error keeps the exact position of its attribute. The read pass follows `TemplateReader`: the same settings, the same frame stack, the same continue rule, and the same sort.
**Alternatives:** One pass that builds each record at its element (a forward name gives a wrong error). An `XDocument` tree (it loses the column of an attribute in the default load and allocates a second tree).

### Decision 2: The loader owns the template table and the map table
**Choice:** `DefinitionLoader` holds `Dictionary<string, byte[]>` and `Dictionary<string, UiActionMap>`, both ordinal. `AddTemplate` copies the text.
**Rationale:** A3 puts the add calls on the loader. The table is one visible object, so the duplicate check has one place. The `string` key follows Decision 5 of binding-attributes: the attribute value is a `string`, and the key has no span overload. The copy follows the records, which copy each name at build time. This settles open point 7 of xml-element-loader: the template registry lives in Framework.
**Alternatives:** A registry inside `TemplateLoader` (it changes Markup, against A8). A UTF-8 key (a conversion at each lookup).

### Decision 3: `SlotContainer` is a leaf type with a required `slot` row
**Choice:** `SlotContainerTemplate.Create()` gives an `ElementTypeInfo` with one `CreateRequired` row `slot`, `IsLeaf = true`, and a factory that calls `new SlotContainer(Encoding.UTF8.GetBytes(value))`. An empty value gives `InvalidValue`. The constructor of `DefinitionLoader` registers it.
**Rationale:** Q2 and A8. The constructor of `SlotContainer` needs the name, so the row is a create row. The children of a slot are the mounted panels, and `SlotContainer.Find` does not enter a slot, so a template child of a slot is a fault. The leaf flag makes the loader report it as `UnexpectedChild` with no new code. The value is used as is, as the bind keys are.
**Alternatives:** A set row with a default name (the constructor rejects an empty name). A public register method (no consumer needs it, YAGNI).

### Decision 4: The factory loads the template at each call
**Choice:** Each panel factory and each layout factory runs `templates.Load(text, templateName).GetRootOrThrow()`.
**Rationale:** Q1 and A4. Each call gives a new, detached root, as the factory contract of the records requires. Each load binds the new elements through the attached `TemplateBindings`, which binding-attributes proved for each load. The template name as the source name puts the right file name in each error line.
**Alternatives:** A cached root (it breaks the contract of a new root at each call, plant 1). A load of each panel template at load time (A4 checks the name only, and a panel template can need a store that the game makes later).

### Decision 5: `DefinitionError` copies the shape of `TemplateError`
**Choice:** A readonly struct with `Kind`, `Line`, `Column`, `Message`, `SourceName`, `ToString`, an internal constructor, and an explicit name switch. The enum has 13 kinds.
**Rationale:** Q4. A game prints both error types with one line form, and markup-sample prints both. The internal constructor keeps each error a product of the loader. The kinds reuse the names of `TemplateErrorKind` for the same causes (`NotWellFormed`, `UnknownElement`, `UnknownAttribute`, `MissingAttribute`, `InvalidValue`, `UnexpectedText`). The new kinds name the causes of requirements line 17 one for each, so a test and a game can read `Kind` and not the message. This settles open point 8 of xml-element-loader.
**Alternatives:** A public constructor on `TemplateError` (it changes Markup, against A8). One generic `UnknownName` kind (a game cannot tell a template from a map).

### Decision 6: A mode names the panels and the layouts of its own text
**Choice:** A `Mode` resolves its `layout` and each `PanelRef` against the entries of the same text. Another name gives `UnknownLayout` or `UnknownPanel`.
**Rationale:** The slot check of Q3 needs the layout template, and the default slot of Q5 needs the panel record. A record that C# code registers has neither in the text. The check at load time keeps `RegisterTo` free of a throw for a fault of the text.
**Alternatives:** Accept an unknown name and leave it to `UiManager.Register` (the error has no position, and the slot check cannot run). Notes item 3 records the open point.

### Decision 7: One fault gives one error
**Choice:** An entry with a name stays in its name table when another value has a fault. A reference to a faulty entry gives no error. A faulty layout skips the slot check of its modes.
**Rationale:** The quality bar asks for exact errors. A follow-on error points at a correct line and hides the real fault in a list. The rule follows the continue rule of `TemplateReader`, where a failed element pushes a failed frame and its children give no error.
**Alternatives:** Drop each faulty entry (each reference then gives a false `UnknownPanel` or `UnknownLayout`, plant 8).

### Decision 8: All or nothing, and `RegisterTo` in the order of Q6
**Choice:** A result with an error holds no record. `RegisterTo` throws `InvalidOperationException` for a failed result, and otherwise registers the panels, then the layouts, then the modes.
**Rationale:** The form follows `TemplateLoadResult` (a root or errors, `GetRootOrThrow`). `UiManager` resolves each mode against the names registered so far, so the modes come last. A partial set of records would register modes with missing panels.
**Alternatives:** A result with the good records and the errors (a game can register a broken set).

### Decision 9: The 0-byte case lives in a partial file of `ModeDiffAllocationTests`
**Choice:** `ModeDiffAllocationTests` becomes `partial`, and `ModeDiffAllocationTests.Loaded.cs` holds `BuildLoadedScene` and one test that reuses `Scene`, `ToggleStep`, and `RunSteps`.
**Rationale:** A9 puts the case in this gate. The existing file has 420 lines, so a second file keeps each file readable, and the case reuses the measured body with no copy. The loaded case and the C# case then measure the same body on two record sources.
**Alternatives:** A new gate class (A9 names this class). The case in the existing file (the file grows to about 520 lines).

### Decision 10: The parity test compares the layout root after each step
**Choice:** `DefinitionParityTests` runs five mode steps on two managers and compares `Roots[0]` of the two contexts with `TreeCompare.FirstDifference` after each step.
**Rationale:** A9 and requirements line 18. The layout root holds the slots and the mounted panel roots, so one compare covers the layout swap, the slot of each panel, the panel order, and each panel tree. The current mode name, the history count, and the map state cover the rest of the diff.

---

## Risks & Mitigation

| # | Risk | Impact | Mitigation |
|---|------|--------|------------|
| 1 | The check load of a layout template binds its elements to a store or a command, and the dropped tree stays alive. | A leak of one tree for each used layout. | A binding is pull-based: `StoreSlice` holds the store, and the store holds no element. `BindCommand` holds the registry in the button. So the dropped tree is garbage. Task 3.2 records the check in `context.md`. |
| 2 | A second `DefinitionLoader` on one `TemplateLoader`, or a game type `SlotContainer`. | `ArgumentException` in the constructor. | The constructor remarks state the rule, and `ASecondDefinitionLoaderOnOneTemplateLoaderThrows` pins it. Notes item 4 records the open point. |
| 3 | A name that the manager holds throws in the middle of `RegisterTo`. | The records before the throw stay registered. | A6 keeps the throw of `UiManager.Register`. The remarks state that the call is not atomic. Notes item 5 records the open point. |
| 4 | A loaded layout root takes each press that hits no element. | The action map and the game get no press. | The layout-root rule of `UiManager` applies. Each layout template in the tests and in section 11 writes `pickable="false"`. The loader does not check it, as `UiManager` does not. |
| 5 | `ModeDefinition` throws for a panel two times in one mode, or a record constructor throws for an empty name. | `Load` throws. | The read pass rejects an empty value with `InvalidValue`, and the resolve pass rejects a panel two times with `DuplicateName`, before each constructor call. `Load` throws for no text in the tests. |
| 6 | A type name of `BrutalUi.Markup` clashes with a name of `BrutalUi.Framework`. | `CS0104` in a new file. | The build shows a clash at once. Qualify the name in that file. |
| 7 | A plant in `UiManager.Apply` fails more than the new case. | A confusing plant log. | Plant 5 lists the expected extra failure (`TheToggleCycleAllocatesNothingAfterWarmUp`). The plant check reads the named test, not the count. |
| 8 | The resolve pass reads the template of an unused layout. | An extra load at build time against Q3. | The pass loads only the layouts that a mode names. `ALayoutTemplateFaultIsATemplateFaultAtTheLayoutEntry` asserts no error for a bad unused layout. |

---

## Dependencies

- xml-element-loader (complete): `TemplateLoader`, `Register`, `Load`, `TemplateLoadResult.GetRootOrThrow`, `ElementTypeInfo`, `AttributeRow`, `ElementCreateArgs`, `TemplateError`, `TemplateErrorKind`.
- binding-attributes (complete): `TemplateBindings` and `AttachTo`, and the reference from Framework to Markup.
- element-generator (complete): the built-in rows that the loaded templates use.
- builder-api (complete): `Find<T>`, the builder methods for the C# twin, and `TreeCompare.cs` (linked in the Framework tests already).
- panels-and-modes (Ready for Review): `PanelDefinition`, `LayoutDefinition`, `ModeDefinition`, `ModePanel`, `SlotContainer`, `UiManager`, `ModeHistory`.
- No new package, no native dependency, no new project reference, and no change to `Directory.Packages.props`.
- markup-sample depends on this feature.

---

## Definition of Done

### Functional Criteria
- [ ] `src/BrutalUi.Framework/Templates/` holds `DefinitionLoader.cs`, `DefinitionLoadResult.cs`, `DefinitionError.cs`, `DefinitionReader.cs`, `DefinitionReader.Resolve.cs`, and `SlotContainerTemplate.cs`. The new public types are `DefinitionLoader`, `DefinitionLoadResult`, `DefinitionError`, and `DefinitionErrorKind`, with the members of the Public API.
- [ ] A text with one `Panel`, one `Layout`, and one `Mode` loads, registers with `RegisterTo`, and after `SetMode` and one `Update` the panel root is the one child of its slot.
- [ ] A layout template with `<ui:SlotContainer slot="top" />` builds a `SlotContainer` with the `SlotName` `top`. A child of a `SlotContainer` in a template gives `UnexpectedChild`.
- [ ] A panel factory gives a new root at each call. A mode with `actions` holds the added map. A `PanelRef` directly under `Mode` takes the default slot of its panel.
- [ ] A loaded set and the same set in C# give equal layout roots under `TreeCompare.FirstDifference` after each of five mode steps, with a layout swap and a back step.
- [ ] `RegisterTo` registers the panels, then the layouts, then the modes. A failed result throws `InvalidOperationException` and registers nothing.

### Quality Criteria
- [ ] Each of the 13 kinds of `DefinitionErrorKind` has a test that asserts the kind, the line, and the column. The errors sort by line, then column. A failed result holds no record.
- [ ] One fault gives one error: a `Panel` with no `template` and a `Mode` that names it give one error.
- [ ] A layout template fault of a used layout gives `TemplateFault` at the `Layout` element, and the message holds the `TemplateError.ToString` text. A bad layout that no mode names gives no error.
- [ ] `Load` throws no exception for any definition text in the tests. A `null` argument throws `ArgumentNullException` with the parameter name. A duplicate `AddTemplate` or `AddActionMap` name throws `ArgumentException`.
- [ ] `TheToggleCycleOnLoadedRecordsAllocatesNothing` measures 0 bytes over 100 toggle steps, and plant 5 fails it.
- [ ] Each existing test passes with no edit. The Framework total grows by 26 from the baseline of `context.md`, and the other test projects keep their totals.
- [ ] The build gives `0 Warning(s)` and `0 Error(s)`. Each new public member has an XML doc comment. Each new file has fewer than 400 lines.

### Integration Criteria
- [ ] `src/BrutalUi.Markup/`, `src/BrutalUi.Core/`, `src/BrutalUi.Framework/Panels/`, `samples/`, `tests/BrutalUi.Markup.Tests/`, both csproj files of Framework and Markup, and `Directory.Packages.props` do not change.
- [ ] In `tests/BrutalUi.Framework.Tests/Panels/ModeDiffAllocationTests.cs`, only the class line changes, to `public sealed partial class ModeDiffAllocationTests`.
- [ ] `src/BrutalUi.Framework/Templates/Definition*.cs` and `SlotContainerTemplate.cs` use no `System.Reflection`, no `Activator`, no `MakeGenericType`, and no `GetType()`.
- [ ] Section 10 of `technical-design.md` names `DefinitionLoader` under **Panel definitions** and `DefinitionParityTests` under **Mode set**. Section 11 holds **### Definition format** with the format, the kinds, and the rules. Section 14 names `TheToggleCycleOnLoadedRecordsAllocatesNothing`. `CHANGELOG.md` has one line tagged `(markup/definition-loader)`.
- [ ] The package test prints `package-test: PASS Gauge level 0.5`.

### Verification Method
1. Run `export DOTNET_CLI_HOME=/tmp/dui-cli-home MSBUILDDISABLENODEREUSE=1; dotnet build brutal-ui.sln -nodeReuse:false 2>&1 | tail -n 30`. Expect `0 Warning(s)` and `0 Error(s)`.
2. Run `dotnet test --solution brutal-ui.sln 2>&1 | tail -n 30`. Expect 0 failed and 3 skipped, and a total 26 above the baseline of `context.md` (3193 at planning time).
3. Run `tests/BrutalUi.Framework.Tests/bin/Debug/net10.0/BrutalUi.Framework.Tests --filter-class '<Class>' 2>&1 | tail -n 5` for each class. Expect `failed: 0` and these totals: `*DefinitionLoaderDemoTests` 4, `*DefinitionRecordTests` 5, `*DefinitionParityTests` 2, `*ModeDiffAllocationTests` 4, `*DefinitionErrorTests` 10, `*DefinitionLoaderApiTests` 4.
4. Run `git diff --stat 9fa4b43 -- src/BrutalUi.Markup src/BrutalUi.Core src/BrutalUi.Framework/Panels src/BrutalUi.Framework/BrutalUi.Framework.csproj samples tests/BrutalUi.Markup.Tests Directory.Packages.props`. Expect no output.
5. Run `git diff 9fa4b43 -- tests/BrutalUi.Framework.Tests/Panels/ModeDiffAllocationTests.cs | grep -E '^[+-][^+-]'`. Expect two lines: the old class line and the new `partial` class line.
6. Run `git diff 9fa4b43 -- src | grep -E '^\+.*public (sealed |static |readonly |ref |abstract )*(partial )*(class|struct|interface|enum|record|delegate) '`. Expect four lines: `DefinitionLoader`, `DefinitionLoadResult`, `DefinitionErrorKind`, and `DefinitionError`.
7. Run `grep -rnE 'System\.Reflection|Activator|MakeGenericType|GetType\(\)' src/BrutalUi.Framework/Templates/Definition*.cs src/BrutalUi.Framework/Templates/SlotContainerTemplate.cs`. Expect no output.
8. Run `wc -l src/BrutalUi.Framework/Templates/Definition*.cs src/BrutalUi.Framework/Templates/SlotContainerTemplate.cs tests/BrutalUi.Framework.Tests/Panels/ModeDiffAllocationTests.Loaded.cs`. Expect 7 files, each under 400 lines.
9. For each kind in `NotWellFormed UnknownElement UnknownAttribute MissingAttribute InvalidValue UnexpectedText UnknownTemplate UnknownActionMap UnknownLayout UnknownPanel DuplicateName UnknownSlot TemplateFault`, run `grep -c "DefinitionErrorKind.<Kind>" tests/BrutalUi.Framework.Tests/Templates/DefinitionErrorTests.cs`. Expect at least 1 for each kind.
10. Run `grep -n '### Definition format' docs/technical-design.md`, `grep -n 'DefinitionParityTests' docs/technical-design.md`, and `grep -n 'TheToggleCycleOnLoadedRecordsAllocatesNothing' docs/technical-design.md`. Expect at least one line each. Run `grep -n 'markup/definition-loader' CHANGELOG.md`. Expect one line.
11. Compare the definition text of **### Definition format** in section 11 with the text of `OneLoadedPanelMountsInItsSlot`. Expect the same text.
12. Run `dotnet pack brutal-ui.sln --no-build --configuration Debug 2>&1 | tail -n 5`, then `rm -rf artifacts/package-test && dotnet restore tests/BrutalUi.PackageTest/BrutalUi.PackageTest.csproj --force 2>&1 | tail -n 3 && dotnet build-server shutdown && dotnet run --project tests/BrutalUi.PackageTest/BrutalUi.PackageTest.csproj --no-restore 2>&1 | tail -n 3`. Expect `package-test: PASS Gauge level 0.5`.
13. Read `context.md`. Expect plants 1 to 9, each with a non-zero exit code, 0 `error CS` lines, and the expected failed test names.

---

## Testing Strategy

- **Demo and record tests (Framework, 9):** `DefinitionLoaderDemoTests` and `DefinitionRecordTests` load a text, register it, set a mode, and read the slots. They cover each record, the slot type, the factory rule, the entry order, the action map, the default slot, and a bound panel template.
- **Parity tests (Framework, 2):** `DefinitionParityTests` compares a loaded set with the same set in C# through `TreeCompare` after five mode steps, and compares the record data. They prove requirements line 18.
- **Allocation case (Framework, 1):** `TheToggleCycleOnLoadedRecordsAllocatesNothing` in `ModeDiffAllocationTests` measures the toggle step on loaded records, with the sum and the label text as the real-work check. It proves requirements line 19.
- **Error and API tests (Framework, 14):** `DefinitionErrorTests` covers each kind with its line and column, A5, A7, Q3, the follow-on rule, the sort, and the text form. `DefinitionLoaderApiTests` covers the argument checks, the duplicate adds, the second loader, the failed `RegisterTo`, and A6.
- **Plants (9):** the factory cache, the slot value, the default slot, the action map, the mode diff allocation, the mode duplicate, the slot check, the follow-on rule, and the failed `RegisterTo`.
- **No regressions:** each existing test passes with no edit. The package test guards the pack.
- **No manual check:** no window, no sample change, and no Needs Human Verification item.

| Class | Project | Phase | Tests |
|---|---|---|---|
| `DefinitionLoaderDemoTests` | Framework | 1 | 4 |
| `DefinitionRecordTests` | Framework | 2 | 5 |
| `DefinitionParityTests` | Framework | 2 | 2 |
| `ModeDiffAllocationTests` (new case) | Framework | 2 | 1 |
| `DefinitionErrorTests` | Framework | 3 | 10 |
| `DefinitionLoaderApiTests` | Framework | 3 | 4 |

---

## Related Documentation

- `docs/features/markup/definition-loader/requirements.md`
- `docs/features/markup/epic-overview.md` and `docs/features/markup/epic-requirements.md`
- `docs/features/markup/xml-element-loader/implementation.md` (Notes items 7 and 8, the position rule, the continue rule)
- `docs/features/markup/binding-attributes/implementation.md` ("What definition-loader inherits", Decision 5)
- `docs/features/markup/markup-sample/requirements.md` (the consumer)
- `docs/technical-design.md` section 10 (**Panel definitions**, **Slot containers**, **Mode set**), section 11 (**XML element format**, **The binding attributes.**), and section 14 (the eighteenth gate)
- `src/BrutalUi.Markup/Template/TemplateLoader.cs`, `TemplateReader.cs`, `TemplateReader.Elements.cs`, `TemplateError.cs`, `TemplateLoadResult.cs`, `ElementTypeInfo.cs`, `AttributeContext.cs`
- `src/BrutalUi.Framework/Panels/PanelDefinition.cs`, `LayoutDefinition.cs`, `ModeDefinition.cs`, `SlotContainer.cs`, `UiManager.cs`
- `src/BrutalUi.Framework/Templates/TemplateBindings.cs`
- `tests/BrutalUi.Framework.Tests/Panels/ModeDiffAllocationTests.cs` (the gate model) and `tests/BrutalUi.Framework.Tests/Templates/TemplateBindingDemoTests.cs` (the test style)
- `tests/BrutalUi.Markup.Tests/Template/AttributeHookTests.cs` (the column helper)
- `.beast-mode/phase-handoff.md` section 11 (the slice rules)

No harness skill in `.claude/skills/` covers a C# pattern of this feature. The plan references the project files above.

---

## Notes

1. **A reading of A2 and Q5: a direct `PanelRef`.** A2 gives the shape `<Mode>` with `<Slot>` children that hold `<PanelRef>`. Q5 lets a mode entry omit the slot. The plan keeps the A2 shape and the A2 names, and adds one form: a `PanelRef` directly under `Mode` has no slot and takes the `DefaultSlot` of its panel. No other shape changes.
2. **Conflict: A9 and the size of `ModeDiffAllocationTests.cs`.** A9 puts the loaded case in `ModeDiffAllocationTests`. The file has 420 lines, and the case with its scene adds about 100 lines. The plan makes the class `partial` (a one-word edit, no test edit) and adds `ModeDiffAllocationTests.Loaded.cs` (Decision 9). The case stays in the class that A9 names.
3. **Open point: a mode that names a record of C# code.** The plan resolves each layout and panel of a mode against the same text (Decision 6), because Q3 and Q5 need the template and the record. Options: (a) the same text only (recommended, the plan). (b) Accept an unknown name, skip the slot check, and leave the fault to `UiManager.Register` with no position. (c) Give the loader a list of known C# names to add before the load.
4. **Open point: one `DefinitionLoader` for each `TemplateLoader`.** The constructor registers `SlotContainer` through the public `Register` (A8), and `TemplateLoader` gives no public query of its types. A second `DefinitionLoader` on one loader therefore throws `ArgumentException`. Options: (a) document and pin the throw (recommended, the plan). (b) Catch the `ArgumentException` of a second register (it hides a game type with the same name). (c) A public `TemplateLoader.IsRegistered` (it changes Markup, against A8).
5. **Open point: `RegisterTo` is not atomic.** A6 keeps the throw of `UiManager.Register` for a name that the manager holds. The records before the throw stay registered. `UiManager` gives no public lookup by name, so a pre-check needs a Panels change. Options: (a) document it (recommended, the plan). (b) Add a name query to `UiManager` in a later feature.
6. **A reading of Q3: an unused layout.** Q3 loads each used layout template. A bad template of a layout that no mode names gives no load error, and `RegisterTo` then throws `InvalidOperationException` from its factory, because `UiManager.Register(LayoutDefinition)` calls the factory. The plan follows Q3. Option: load each layout at load time, also an unused one (one more load for each unused layout, and each fault shows at load time).
7. **Conflict: sections of `technical-design.md`.** The requirements name sections 10 and 11. A9 grows the eighteenth gate, and section 14 describes that gate with its test count. The plan also edits that bullet in section 14 (task 2.6), as binding-attributes did for its gate.
8. **The whitespace of a value.** The plan uses each name value as is, as Decision 5 of binding-attributes does. `slot=" top"` names a slot with a space, and the slot check then gives `UnknownSlot` with the space in the quote. Only an empty value gives `InvalidValue`.
9. **A Slot with no PanelRef.** An empty `Slot` adds no placement. The slot check still runs on its name, so a typing fault in an empty slot gives `UnknownSlot`.
10. **The planned totals** come from the binding-attributes close: the solution 3167 with 3 skipped at `9fa4b43`. Task 1.1 records the real baseline and the Framework total, and the acceptance criteria count the deltas from it.
