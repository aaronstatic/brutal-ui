# Markup Sample - Implementation Plan

**Epic:** markup (feature 6 of 6)
**Status:** In Progress
**Started:** 2026-11-19
**Target Completion:** TBD
**Last Updated:** 2026-11-19 08:28

---

## Quality Bar

**Target quality:** A copy-ready sample and one small framework API. A game developer copies the HelloPanel markup files and the load code as the start of a real screen. The feature closes the markup epic and Phase 3. Five things carry the feature.

- **Reliability:** The sample loads all its records and the state panel from XML, and the two modes and the `Mode (F1)` button work as before. The smoke run keeps lines 1 to 7 and their checks, and adds line 8 with its check. `scripts/test-linux.sh` and `scripts/test-windows.ps1` pass with no script change.
- **Clear errors before the window:** A fault in `HelloState.xml` or `HelloDefinitions.xml` prints one `ToString` line for each error (`<source>(<line>,<column>): <Kind>: <message>`) to standard error, and the sample exits with code 1 before `Glfw.Init`. A host with no GPU and no display proves it.
- **The 0-byte gate:** `Update` and `Draw` on the loaded sample shape allocate 0 bytes after warm-up, also for a click on the loaded `Add` button and for a mode toggle. A planted allocation fails the gate.
- **A small, tested API change:** `DefinitionLoader.AddFactory` joins the existing template table with the same checks as `AddTemplate`. Each new rule has a test and a plant. Markup, Core, and `src/BrutalUi.Framework/Panels/` do not change.
- **Copy-ready docs:** The README and section 11 of `technical-design.md` show the template of the sample, character for character equal to the file. Each public member has an XML doc comment in ASD-STE100 prose (CS1591 is an error). Each new source file stays under 400 lines.

---

## Executive Summary

This feature moves the HelloPanel records and one panel of the sample to XML, through the APIs of features 1 to 5 of the epic.

- `DefinitionLoader.AddFactory(name, factory)` is new in `BrutalUi.Framework` (Q1). A `template` value of a `Panel` or a `Layout` then names a UTF-8 template text or a C# factory. All six HelloPanel records (3 panels, 1 layout, 2 modes) load from `HelloDefinitions.xml`.
- `DefinitionLoadResult.RegisterTo` returns the controllers of the registered panels. The sample needs them to keep its build rule: each panel root exists before the first frame (Decision 4, Notes item 2).
- The state panel `hello.state` comes from `HelloState.xml` (Q2, A5). The `Add`, `Rebind (Up)`, and `Restore` buttons use `command`, and the score label uses `bind-text`. The row is `HelloRow`, a custom element type with `[TemplateElement("HelloRow")]` (Q3). The widget panel, the info panel, and the layout stay C# factories (A5, A9).
- The sample loads the two files before `Glfw.Init`. A load error prints each error line and exits with code 1 (A3).
- The smoke run prints line 8 with the counts of the loaded panel and the records, and exits with code 1 when a count is 0 (A2).
- A new allocation gate in `BrutalUi.Framework.Tests`, the twenty-third, rebuilds the sample shape from the linked sample files and measures `Update` and `Draw` at 0 bytes (A6).
- The README, sections 11 and 14 of `technical-design.md`, `docs/overview.md`, `CHANGELOG.md`, and the epic `human-testing.md` show the result (A8, A10).

The plan has 4 phases and 22 tasks, about 205 steps. Phase 1 (9 tasks) adds the factory hook with its tests and switches HelloPanel to the XML records, the XML state panel, `HelloRow`, and line 8 (the demo of Q4). Phase 2 (5 tasks) moves the load before the window and adds the error exit. Phase 3 (4 tasks) adds the 0-byte gate and removes the old C# state row (A9). Phase 4 (4 tasks) writes the docs. Phases 1 and 2 need an advanced dev agent. Phases 3 and 4 are STANDARD.

---

## User Personas Alignment

No `docs/user-personas.md` file exists. The personas come from `docs/mission-statement.md` and the epic docs.

**Target Users:**
- [ ] **The game developer** copies `HelloState.xml`, `HelloDefinitions.xml`, `HelloRow.cs`, and the load code of HelloPanel, and gets a bound panel on the screen from a store, a panel controller, and an XML template (the target outcome of the mission statement).
- [ ] **The markup author** edits the two XML files next to the executable and gets an error line with a line, a column, and a kind for each fault, with no window.
- [ ] **The framework contributor** reads the twenty-third gate and the eighth smoke line as the proof that a loaded tree stays at 0 bytes in the frame loop.

**User Subsets Served:**
- [ ] **Simple** - one XML panel with one store binding and three commands, mixed with C# panels through `AddFactory`.
- [ ] **Complex** - all records in XML, two modes with one action map, a custom element type from the generator, and C# factory panels and a C# layout next to an XML panel.

**UX Approach:**
- Build time: the sample reads the two files, loads, and registers before the first frame. Load-time code allocates (epic A10).
- Run time: the loaded panel is a normal tree. The frame loop does not change.
- No hot reload (Q6), no second sample project, and no move of the other screens to XML (requirements Out of Scope).

---

## Goals

### Primary Goals
1. Load the state panel of HelloPanel from `HelloState.xml` with `bind-text` and `command` (requirements line 12, Q2).
2. Load all HelloPanel records from `HelloDefinitions.xml`, with C# factories for the widget panel, the info panel, and the layout (requirements line 13, Q1, A5).
3. Use one custom element type with the generator attribute in the template (requirements line 14, Q3, A7).
4. Print each load error with its line, column, and kind, and exit with code 1 before the window (requirements line 15, A3).
5. Print smoke line 8 and fail the smoke run for a count of 0 (requirements line 16, A2).
6. Prove `Update` and `Draw` on the loaded sample shape at 0 bytes (requirements line 17, A6).
7. Show the sample template in the README and section 11, and list the windowed checks in the epic `human-testing.md` (requirements lines 18 and 19, A8, A10).

### Success Criteria
- [ ] The phase 1 demo passes on a host with a window: `HelloPanel --frames 120` prints line 8 with 6 counts above 0, line 6 counts above 0, and exit code 0.
- [ ] The phase 2 demo passes on any host: a broken `bind-text` key in the output copy of `HelloState.xml` gives one `UnknownBinding` line on standard error and exit code 1, with no GLFW line.
- [ ] The phase 3 demo passes: `SampleMarkupAllocationTests` reads a total of 3 with `failed: 0`.
- [ ] The phase 4 demo passes: the XML block of the README section equals `samples/HelloPanel/HelloState.xml`.
- [ ] Each existing test passes. The only edit of an existing test is two expected message strings in `DefinitionErrorTests.cs` (Notes item 9). The planned delta is Framework +8, so the solution grows from 3195 to 3203 with 3 skipped. Task 1.1 records the real baseline, and the deltas count from it.

---

## Architecture Overview

### The Place in the Epic

markup-sample is feature 6 of 6. It uses `TemplateLoader`, `TemplateLoadResult`, and `TemplateError` of xml-element-loader, the generator and `[TemplateElement]` of element-generator, `TemplateBindings` of binding-attributes, `Find<T>` of builder-api, and `DefinitionLoader` and `DefinitionLoadResult` of definition-loader. It changes two members of definition-loader: `AddFactory` is new, and `RegisterTo` returns the panel controllers. It follows the load flow of `docs/features/markup/definition-loader/context.md` (lines 38 and 429), binding-attributes Notes item 7 (the Markup reference of a game), element-generator Notes item 8 (the analyzer line and the register call), and binding-attributes `implementation.md` line 98 (print `TemplateError.ToString` on a load error).

### Public API Change (end state)

```csharp
namespace BrutalUi.Framework;

public sealed class DefinitionLoader
{
    public void AddTemplate(string name, ReadOnlySpan<byte> utf8);              // unchanged signature; also checks the factory names
    public void AddFactory(string name, Func<PanelContext, Element> factory);   // new (Q1)
}

public sealed class DefinitionLoadResult
{
    public IReadOnlyList<PanelController> RegisterTo(UiManager ui);   // was void; the controllers of Panels, same order
}
```

**The rules of `AddFactory`.**
- One name table holds the texts and the factories. A name holds one text or one factory. A second add of one name throws `ArgumentException` before the add, in each order (`AddTemplate` then `AddFactory`, and `AddFactory` then `AddTemplate`). The first entry stays.
- A `null` name throws `ArgumentNullException` (`name`), a `null` factory throws `ArgumentNullException` (`factory`), and an empty name throws `ArgumentException` (`name`). These are the checks of `AddTemplate`.
- A panel or a layout that names a factory gets the delegate as is. The record calls it as it calls a C# factory. The game owns the factory contract: a new, detached root at each call.
- The load calls no factory. A layout with a factory gets no check load, because the loader has no `PanelContext` and a game factory can have side effects (Decision 3). A mode on such a layout skips the slot check. `UiManager.Register(ModeDefinition)` still checks each slot inside `RegisterTo` and throws `ArgumentException` for an unknown slot, with no position.
- An unknown name gives `UnknownTemplate`. From phase 2, its message names both add methods.

### The Sample (end state)

```
samples/HelloPanel/
  HelloState.xml         new: the state panel template (A1, Q2)
  HelloDefinitions.xml   new: 3 panels, 1 layout, 2 modes (A1, Q1)
  HelloRow.cs            new: [TemplateElement("HelloRow")] HelloRow : Element (Q3)
  HelloMarkup.cs         new (phase 2): reads the two files, runs the load flow of A4, prints the errors
  HelloModes.cs          constructor (no GPU) + Mount(definitions, checker) -> HelloMount
  HelloState.cs          the store, the commands, the map (Row and the C# buttons removed in phase 3, A9)
  HelloScreen.cs         the widget panel, the info panel, the layout (BuildStatePanel removed in phase 3)
  Program.cs             the build before Glfw.Init, line 8, the markup check
  HelloPanel.csproj      the generator analyzer line and two copy items (A1, A7)
```

```
Program.Run (end state)
  sheet       = HelloScreen.LoadSheet()                                  before GLFW, as today
  renderer    = new ImGuiRenderer(fonts, textures); ui = new UiContext(renderer)
  state       = new HelloState(ui); modes = new HelloModes(ui, state)
  definitions = HelloMarkup.Load(state, modes, Console.Error)            null: exit 1, no GLFW (A3)
  Glfw.Init -> RunWindow -> RunImGui
      handle = textures.AddVulkan(...); ui.AddSheet(sheet)
      mount  = modes.Mount(definitions, handle)    RegisterTo, EnsureRoot of each panel, the lookups, the counts
      modes.Manager.SetMode("hello.main"u8); RunFrames(..., mount, ...)
  print lines 1 to 8; run the checks
```

```
HelloMarkup.Load (the flow of A4)
  TemplateLoader templates = new();
  TemplateBindings bindings = new(state.Map.Commands);
  bindings.AddText("hello.score", state.Score, s_score);          // static selector, the score itself
  bindings.AttachTo(templates);
  templates.RegisterHelloPanelElements();                          // generated (element-generator Notes item 8)
  check = templates.Load(stateText, "HelloState.xml");             // the check load of A3 (Decision 7)
  DefinitionLoader definitions = new(templates);
  definitions.AddTemplate("HelloState.xml", stateText);
  definitions.AddFactory("HelloModes.CreateWidgetPanel", modes.CreateWidgetPanel);
  definitions.AddFactory("HelloModes.CreateInfoPanel", modes.CreateInfoPanel);
  definitions.AddFactory("HelloModes.CreateLayout", modes.CreateLayout);
  definitions.AddActionMap("hello.actions", state.Map);
  result = definitions.Load(definitionsText, "HelloDefinitions.xml");
  print each TemplateError and each DefinitionError; return null when one exists, else result
```

### The Two Files

`HelloState.xml` holds no comment, so the README block and the section 11 block equal the file:

```xml
<ui:Element class="hello-panel hello-panel-root" xmlns:ui="brutal-ui">
  <ui:HelloRow>
    <ui:Button name="add" text="Add" command="hello.add" />
    <ui:Button name="rebind" text="Rebind (Up)" command="hello.rebind" />
    <ui:Button name="restore" text="Restore" command="hello.restore" />
    <ui:Label name="score" pickable="false" bind-text="hello.score" />
  </ui:HelloRow>
</ui:Element>
```

`HelloDefinitions.xml`:

```xml
<ui:Definitions xmlns:ui="brutal-ui">
  <ui:Panel name="hello.widgets" slot="top" template="HelloModes.CreateWidgetPanel" />
  <ui:Panel name="hello.state" slot="bottom" template="HelloState.xml" />
  <ui:Panel name="hello.info" slot="bottom" template="HelloModes.CreateInfoPanel" />
  <ui:Layout name="hello.layout" template="HelloModes.CreateLayout" />
  <ui:Mode name="hello.main" layout="hello.layout" actions="hello.actions">
    <ui:Slot name="top">
      <ui:PanelRef name="hello.widgets" />
    </ui:Slot>
    <ui:Slot name="bottom">
      <ui:PanelRef name="hello.state" />
    </ui:Slot>
  </ui:Mode>
  <ui:Mode name="hello.detail" layout="hello.layout" actions="hello.actions">
    <ui:Slot name="top">
      <ui:PanelRef name="hello.widgets" />
    </ui:Slot>
    <ui:Slot name="bottom">
      <ui:PanelRef name="hello.info" />
    </ui:Slot>
  </ui:Mode>
</ui:Definitions>
```

The loaded state tree has the shape of the old C# row: an `Element` with `hello-panel` and `hello-panel-root`, one row with `hello-row` (now `HelloRow`), three buttons, and one label that is not pickable. The names `add`, `rebind`, `restore`, and `score` are new and change no draw. So line 2 of the smoke run must not change. The placements keep the order of the C# modes (`top`, then `bottom`).

### Line 8 and Its Check

`HelloPanel: markup: elements <E>, bindings <B>, commands <C>, panels <P>, layouts <L>, modes <M>.`

- `E` is the element count of the mounted state root, the root included (6). `B` is the sum of `Element.BindingCount` over that tree (1). `C` is the count of `Button` elements in that tree with a `Clicked` slot that is not `null` (3). `P`, `L`, and `M` are the counts of `result.Panels`, `result.Layouts`, and `result.Modes` (3, 1, 2).
- `Mount` computes the counts one time, after `EnsureRoot`, into `MarkupCounts` (a `readonly record struct` in `HelloModes.cs`). `RunResult` gets one member `Markup`.
- The check runs in the smoke block (frame limit set, 2 measured frames or more), after the mode check. A count of 0 writes `HelloPanel: the markup panel did not load.` to standard error and returns `ExitFailure`.

---

## Implementation Phases

The feature has 22 tasks, so it has four slices. Section 11 of `.beast-mode/phase-handoff.md` holds the rules. Rule 11 binds phase 1 to the demo of Q4. The Chosen Approach puts the hook and the sample in phase 1 and the load-error exit, the gate, and the docs in later slices. The file limit of a task group (6 files) sets the cut: group 1 of phase 1 holds the hook and `HelloRow.cs`, and group 2 holds the sample. The `UnknownTemplate` message moves to phase 2 (the error slice), and the removal of the C# row (A9) moves to phase 3 (Notes items 8 and 9).

**The agent tier.** Phase 1 is ADVANCED (opus). It changes two public members of a sibling feature and touches six integration points: `DefinitionLoader`, the resolve pass, `RegisterTo`, the generator in the sample, `TemplateBindings` in the sample, and the smoke output of `Program.cs`. The agent host cannot open a window, so the agent must get the sample right without the demo run. Phase 2 is ADVANCED (opus). It moves the build of the renderer, the context, the state, and the modes ahead of `Glfw.Init` in a 1389-line `Program.cs`, and it changes the error path. Phases 3 and 4 are STANDARD (sonnet).

**The commands.** Export `DOTNET_CLI_HOME=/tmp/dui-cli-home` and `MSBUILDDISABLENODEREUSE=1` in each command. Give `-nodeReuse:false` to `dotnet build` only. Each task group ends with `dotnet build brutal-ui.sln -nodeReuse:false 2>&1 | tail -n 30` and `dotnet test --solution brutal-ui.sln 2>&1 | tail -n 30` (tool rule 5). Expect `0 Warning(s)`, `0 Error(s)`, and 0 failed. A warning fails the build. Run one class after a build with `tests/BrutalUi.Framework.Tests/bin/Debug/net10.0/BrutalUi.Framework.Tests --filter-class '*<Class>' 2>&1 | tail -n 5`. Run the sample with `samples/HelloPanel/bin/Debug/net10.0/HelloPanel --frames 120; echo "exit=$?"`. The projects have no implicit usings, so each new file holds its own `using` lines. Use file-scoped namespaces. When a generated register method is missing after a build, run `dotnet build-server shutdown` and build again (a warm `VBCSCompiler` can keep an old analyzer set).

**The plants.** For a code plant, copy the file to `src/BrutalUi.Framework/obj/plantscratch/` (git-ignored), make the change, and run `dotnet test --solution brutal-ui.sln > <scratch>/pN.log 2>&1; echo "exit=$?"`. Check the `error CS` count first. It must be 0, because a plant that does not compile leaves the old binary, and the old binary passes. Expect an exit code that is not 0, and read the failed names from the log. Restore with `cp`, check with `cmp`, and run again. A test host can exit with 134 one time; run a second time before you treat it as a fault. For a plant in an output copy of an XML file, copy the file first and restore it with `cp` from `samples/HelloPanel/`. A build does not restore it: `PreserveNewest` keeps the newer output file. Check the restore with `cmp`.

**The window.** This agent host reports `HelloPanel: GLFW init failed: PlatformUnavailable` before any smoke line (section 14 of `technical-design.md`). When the smoke run gives that line, record "smoke pending a host with a window" in `context.md`, and add the check to the epic `human-testing.md` in phase 4. The build, the tests, and the headless checks of phase 2 still run here.

**The tests.** Follow the style of `TemplateBindingAllocationTests` and `DefinitionLoaderApiTests`. Pass each inline text as a `u8` literal. Build each context with a `RecordingRenderer` measurer, and each manager with a `PanelContext` of a new `CommandRegistry`, `EventBus`, and `ServiceRegistry`. Find a slot with `SlotContainer.Find(context.Roots[0], "top"u8)` and an element with `root.Find<T>("name"u8)`.

### Phase 1: The XML State Panel in HelloPanel
**Goal:** `DefinitionLoader.AddFactory` exists with its checks and tests, and `RegisterTo` returns the panel controllers. HelloPanel loads its six records from `HelloDefinitions.xml` and the state panel from `HelloState.xml`, with `HelloRow` from the generator, and prints line 8.
**Demo:** the developer runs `HelloPanel --frames 120` on a host with a window and reads an eighth line `HelloPanel: markup: elements 6, bindings 1, commands 3, panels 3, layouts 1, modes 2.` Line 6 still shows `adds`, `resets`, and `versions` above 0, and the exit code is 0.
**Needs:** none
**Agent tier:** ADVANCED (opus). This phase changes the public API of a sibling and touches six integration points with no demo run on the agent host.

**Tasks (group 1, the factory hook and the custom type, 6 files):**
- [ ] 1.1 Create `docs/features/markup/markup-sample/context.md` when `/start-feature` did not. Run `git status --porcelain`. Record the base commit (`git rev-parse HEAD`, `7948a78` at planning time), the solution total with the skipped count (3195 with 3 skipped at the definition-loader close), and the total of `BrutalUi.Framework.Tests` (384 at that close). Record the Public API Change, The Sample, The Two Files, Line 8 and Its Check, the Key Technical Decisions, and the Notes of this plan. Record the tool notes: `DOTNET_CLI_HOME`, the per-class binary, the `error CS` check of a plant, the `PreserveNewest` restore rule, and the window rule.
- [ ] 1.2 Edit `src/BrutalUi.Framework/Templates/DefinitionLoader.cs`. Add the field `Dictionary<string, Func<PanelContext, Element>> _factoriesByName` with `StringComparer.Ordinal`. Add `public void AddFactory(string name, Func<PanelContext, Element> factory)` with the rules of The Rules of `AddFactory`: `ArgumentNullException` for a `null` name, then for a `null` factory, `ArgumentException` for an empty name, and `ArgumentException` before the add for a name that either table holds. `AddTemplate` also checks `_factoriesByName`. Reword `DuplicateTemplateNameMessage` to name a text or a factory (no test pins the text; check with `grep -rn 'One name holds' tests`). Add `internal bool TryGetFactory(string name, out Func<PanelContext, Element>? factory)`. Update the class remarks: one name table for texts and factories, and the load calls no factory.
- [ ] 1.3 Edit `src/BrutalUi.Framework/Templates/DefinitionReader.Resolve.cs`. In step 2, a `template` value that misses `TryGetTemplate` and hits `TryGetFactory` builds the `PanelDefinition` with the factory as is. In step 3, do the same for a `LayoutDefinition`, and mark the layout as a factory layout. In step 3b, skip the check load of a factory layout. In step 4, set `checkSlots` only for a layout with a slot list (`layoutSlots.ContainsKey(layoutIndex)`), so a mode on a factory layout skips the slot check and a faulty layout keeps its old behavior. Leave the `UnknownTemplate` message as it is in this phase. Keep `DefinitionReader.Resolve.Checks.cs` unchanged. Update the class remarks with the factory rule.
- [ ] 1.4 Edit `src/BrutalUi.Framework/Templates/DefinitionLoadResult.cs`. `RegisterTo` returns `IReadOnlyList<PanelController>`: the return value of each `ui.Register(PanelDefinition)` in the order of `Panels`, as a `ReadOnlyCollection`. The argument check and the failed-result throw come first, as today. Update the remarks: the list serves `PanelController.EnsureRoot` before the first frame, and a throw in the middle gives no list (the call is not atomic, A6 of definition-loader).
- [ ] 1.5 Write `tests/BrutalUi.Framework.Tests/Templates/DefinitionFactoryTests.cs` with 5 tests. `AFactoryPanelAndAFactoryLayoutLoadAndMount` (a text with a factory panel in `top`, a template panel in `bottom`, a factory layout, and one mode. The load succeeds, `result.Panels[0].CreateRoot` is the same delegate as the added factory, and after `RegisterTo`, `SetMode`, and one `Update`, the slot `top` holds the factory root and the slot `bottom` holds the template root). `AFactoryLayoutGetsNoCheckLoadAndItsModesSkipTheSlotCheck` (a layout factory with a call counter. After the load the counter is 0, and a mode with `<ui:Slot name="left">` on the factory layout loads with no error. `RegisterTo` then calls the factory one time and throws `ArgumentException` from `UiManager.Register(ModeDefinition)`). `AddTemplateAndAddFactoryShareOneNameTable` (`AddFactory` after `AddTemplate` of one name throws `ArgumentException` with `ParamName` `name`, and so does `AddTemplate` after `AddFactory`. In both cases the first entry stays: the loaded panel uses the first text or the first factory). `AddFactoryChecksItsArguments` (a `null` name gives `name`, a `null` factory gives `factory`, and an empty name gives `ArgumentException` with `name`). `RegisterToGivesThePanelControllersInTextOrder` (two panels give a list of 2. `list[i].Definition` is the same object as `result.Panels[i]`, and `EnsureRoot` gives the root of each template). Run the build and the tests, and expect Framework +5. Run three plants. Plant 1: `AddTemplate` skips the factory table check. Expect `AddTemplateAndAddFactoryShareOneNameTable` to fail. Plant 2: a factory layout gets an empty slot list in place of no list. Expect `AFactoryLayoutGetsNoCheckLoadAndItsModesSkipTheSlotCheck` and `AFactoryPanelAndAFactoryLayoutLoadAndMount` to fail. Plant 3: `RegisterTo` returns an empty list. Expect `RegisterToGivesThePanelControllersInTextOrder` to fail. Record the plants in `context.md`.
- [ ] 1.6 Write `samples/HelloPanel/HelloRow.cs`: `[TemplateElement("HelloRow")] internal sealed class HelloRow : Element` with a `private static readonly ClassId RowClass = ClassName.Intern("hello-row");` and a public constructor with no parameters that adds the class. The class has no own property, so it has no row (Q3). The remarks state that the generator writes `RegisterHelloPanelElements` from the assembly name, that the CSS rules `.hello-row` and `.hello-row > Label` match it by class, and that a game with its own `[TemplateElement]` types references `BrutalUi.Markup` and the generator line (A7).

**Tasks (group 2, the sample, 6 files):**
- [ ] 1.7 Edit `samples/HelloPanel/HelloPanel.csproj`. Add `<ProjectReference Include="../../src/BrutalUi.Generators/BrutalUi.Generators.csproj" OutputItemType="Analyzer" ReferenceOutputAssembly="false" />` with the comment of `BrutalUi.Markup.Tests.csproj` (an analyzer from a ProjectReference does not flow on). Add `<None Include="HelloState.xml" CopyToOutputDirectory="PreserveNewest" />` and the same for `HelloDefinitions.xml` (A1). Write `samples/HelloPanel/HelloState.xml` and `samples/HelloPanel/HelloDefinitions.xml` with the text of The Two Files, word for word, with LF line ends and a final LF.
- [ ] 1.8 Edit `samples/HelloPanel/HelloModes.cs`. The constructor becomes `HelloModes(UiContext context, HelloState state)`: the commands, the keys, the mode button, the panel context, and the manager, as today, and no register call. The factory methods `CreateWidgetPanel`, `CreateInfoPanel`, and `CreateLayout` become `internal`; `CreateStatePanel` goes away. Add `public HelloMount Mount(TextureHandle checker)`. It sets `_checker`, reads both files from `AppContext.BaseDirectory`, runs the flow of `HelloMarkup.Load` in The Sample, and throws `InvalidOperationException` with the LF-joined `ToString` lines of each error for a failed check load or a failed definitions load (Notes item 7; phase 2 moves the flow and the print). Then it calls `RegisterTo(Manager)`, calls `EnsureRoot` on each returned controller (the build rule), copies the seven out fields of the widget factory, finds the widget root and the state root by the controller `Definition.Name`, finds the `Add` button with `stateRoot.Find<Button>("add"u8)` (Q2), sets `Main` and `Detail` from `result.Modes` by `Name.SequenceEqual`, computes `MarkupCounts`, and returns a new `HelloMount`. Add `internal sealed class HelloMount` with `required` init properties `WidgetPanel`, `GateLabel`, `InputLabel`, `StickLabel`, `MoveLabel`, `SchemeLabel`, `RebindLabel`, `Widgets`, `AddButton`, and `Markup`. `Main` and `Detail` stay on `HelloModes` as `ModeDefinition?` that `Mount` sets, because `s_mode` reads them. A missing element or a missing mode throws `InvalidOperationException` with a message that names it. Update the class remarks: the register order now comes from `RegisterTo`, the build rule uses its controllers, and the records come from `HelloDefinitions.xml`.
- [ ] 1.9 Edit `samples/HelloPanel/Program.cs`. In `RunImGui`, make `new HelloModes(ui, state)` and call `modes.Mount(handle)` after `ui.AddSheet`. `RunFrames` takes the `HelloMount` in place of the eight tree arguments; its first lines copy the members into the old local names, so the frame body keeps its text. The `Add` point reads `mount.AddButton` in place of `state.AddButton` (Q2). Add `MarkupCounts Markup` as the last member of `RunResult` with its `<param>` doc, and fill it from `mount.Markup`. Print line 8 after line 7 in the form of A2, and add the markup check of Line 8 and Its Check after the mode check. Update the remarks of the class (the eight lines at :22 to :38, the exit-code list, and the `Add` button sentence at :48) and of `Run` (line 8 and the markup check). Run the build and the tests, and expect no change in this group. Run the smoke command. On a host with a window, expect the demo of this phase. On this host, expect the GLFW line, and record the smoke as pending. Record the counts and the demo state in `context.md`.

**Estimated Steps:** about 90 (9 tasks, about ten steps for one task)

**Files (group 1, 6):** `context.md`, `DefinitionLoader.cs`, `DefinitionReader.Resolve.cs`, `DefinitionLoadResult.cs`, `DefinitionFactoryTests.cs`, `HelloRow.cs`
**Files (group 2, 6):** `HelloPanel.csproj`, `HelloState.xml`, `HelloDefinitions.xml`, `HelloModes.cs`, `Program.cs`, `context.md`

**Acceptance Criteria:**
- [ ] The 5 new Framework tests pass, and each existing test passes with no edit.
- [ ] `git diff 7948a78 -- src/BrutalUi.Markup src/BrutalUi.Core src/BrutalUi.Framework/Panels` gives no output.
- [ ] Plants 1, 2, and 3 each give a non-zero exit code with 0 `error CS` lines, and each restored file equals its copy under `cmp`.
- [ ] `samples/HelloPanel/bin/Debug/net10.0/` holds `HelloState.xml` and `HelloDefinitions.xml`, equal to the source files under `cmp`.
- [ ] `grep -rn 'RegisterHelloPanelElements' samples/HelloPanel/*.cs` gives one call, and the build compiles it (the generator runs on the sample).
- [ ] On a host with a window, the smoke run prints 8 lines, line 8 equals the text of the Demo line, and the exit code is 0. On this host, `context.md` records the smoke as pending.

### Phase 2: The Load Error Exit Before the Window
**Goal:** HelloPanel builds its managed objects and loads the two files before `Glfw.Init`. Each template error and each definition error prints its `ToString` line to standard error, and the sample exits with code 1 with no GPU. The `UnknownTemplate` message names `AddTemplate` and `AddFactory`.
**Demo:** the developer changes `bind-text="hello.score"` to `bind-text="hello.scor"` in `samples/HelloPanel/bin/Debug/net10.0/HelloState.xml`, runs `HelloPanel --frames 120`, and reads one line that starts with `HelloState.xml(` and holds `): UnknownBinding: 'hello.scor' is not a bound key.` on standard error, no GLFW line, and exit code 1.
**Needs:** Phase 1
**Agent tier:** ADVANCED (opus). This phase moves the object build across the GLFW and ImGui boundary of `Program.cs` and changes the error path.

**Tasks (one group, 6 files):**
- [ ] 2.1 Write `samples/HelloPanel/HelloMarkup.cs` with `internal static class HelloMarkup`: the two file name constants, the template name and factory name constants of The Two Files, the static selector `s_score` (`static s => s`), and `public static DefinitionLoadResult? Load(HelloState state, HelloModes modes, TextWriter errors)`. The method reads both files from `AppContext.BaseDirectory` and runs the flow of The Sample. It writes each `TemplateError.ToString` of the check load and each `DefinitionError.ToString` of the definitions load to `errors`, one line each, with no prefix (A3). It runs both loads, so one run gives each error, and returns `null` when one error or more exists. An IO fault throws, as `LoadSheet` does. The remarks state the A4 order, the check load of Decision 7, and the rule that the method needs no GPU and no window. Move the load code out of `HelloModes.Mount`.
- [ ] 2.2 Edit `samples/HelloPanel/HelloModes.cs`. `Mount` becomes `Mount(DefinitionLoadResult definitions, TextureHandle checker)`: it throws `ArgumentNullException` for `null` and `ArgumentException` for a failed result, and starts at `RegisterTo`. Update the remarks: the constructor needs no GPU, and the factories run only inside `Mount`, after the texture exists.
- [ ] 2.3 Edit `samples/HelloPanel/Program.cs`. In `Run`, after `LoadSheet` and before `Glfw.Init`, create the font service, the texture service, the renderer, the `UiContext`, the `HelloState`, and the `HelloModes`, then call `HelloMarkup.Load(state, modes, Console.Error)`. A `null` result returns `ExitFailure` with no GLFW call. Pass the renderer, the context, the state, the modes, and the result through `RunWindow` to `RunImGui`, and remove their creation from `RunImGui`; the texture service comes from `renderer.Textures`. `RunImGui` calls `modes.Mount(definitions, handle)`. Update the remarks of the class (the load of the two files before the window) and of `Run`.
- [ ] 2.4 Edit `src/BrutalUi.Framework/Templates/DefinitionReader.Resolve.cs`. Put the `UnknownTemplate` message in one constant: `'<name>' is not a registered template. Add it with DefinitionLoader.AddTemplate or DefinitionLoader.AddFactory before the load.` Edit `tests/BrutalUi.Framework.Tests/Templates/DefinitionErrorTests.cs`: change the two expected strings (at :42 and :294 at planning time) to the new message and change nothing else (Notes item 9).
- [ ] 2.5 Run the build and the tests, and expect no total change in this phase. Run the headless checks on the output folder. Check 1 (the demo): the broken key in `HelloState.xml` gives one `UnknownBinding` line, no GLFW line, and exit code 1. Check 2: `template="HelloModes.CreateLayot"` in `HelloDefinitions.xml` gives one line that starts with `HelloDefinitions.xml(` and holds `): UnknownTemplate: 'HelloModes.CreateLayot' is not a registered template.`, and exit code 1. Check 3: both faults in one run give both lines, sorted by file, and exit code 1. Check 4: the clean files on this host give no markup line, then the GLFW line, and exit code 1 (so the load passed before GLFW); on a host with a window, the clean run gives 8 lines and exit code 0. Restore each file with `cp` and check it with `cmp`. Record each check, the counts, and the demo state in `context.md`.

**Estimated Steps:** about 50 (5 tasks, about ten steps for one task)

**Files (6):** `HelloMarkup.cs`, `HelloModes.cs`, `Program.cs`, `DefinitionReader.Resolve.cs`, `DefinitionErrorTests.cs`, `context.md`

**Acceptance Criteria:**
- [ ] Checks 1 to 4 give the expected lines and exit codes, and each restored file equals its source under `cmp`.
- [ ] In `Run`, the call of `HelloMarkup.Load` comes before the first `Glfw.` call (`grep -n 'HelloMarkup.Load\|Glfw.Init' samples/HelloPanel/Program.cs` lists the load first).
- [ ] `git diff <phase 1 close> -- tests/BrutalUi.Framework.Tests/Templates/DefinitionErrorTests.cs | grep -E '^[+-][^+-]'` gives four lines: the two old and the two new message strings.
- [ ] Each test passes, and the solution total equals the phase 1 close.

### Phase 3: The Sample Shape at Zero Bytes
**Goal:** The XML template is the only state row of the sample (A9). A new gate in `BrutalUi.Framework.Tests` rebuilds the sample shape from the linked sample files and asserts 0 bytes for `Update` and `Draw`, also for a click on the loaded `Add` button and for a mode toggle (A6).
**Demo:** the developer runs `BrutalUi.Framework.Tests --filter-class '*SampleMarkupAllocationTests'` and reads a total of 3 with `failed: 0`. `AnAddClickOnTheLoadedStatePanelAllocatesNothing` measures 0 bytes over 100 click frames on the panel that `HelloState.xml` builds.
**Needs:** Phase 1, Phase 2
**Agent tier:** STANDARD (sonnet).

**Tasks (one group, 5 files):**
- [ ] 3.1 Edit `samples/HelloPanel/HelloState.cs`. Remove `Row`, `AddButton`, `RebindButton`, `RestoreButton`, `RowClass`, `s_scoreText`, and their build lines (A9). Update the remarks: the state panel comes from `HelloState.xml`, `HelloMarkup` binds the key `hello.score` to `Score`, and the label rule (the longest score text fits the buffer) stays. Edit `samples/HelloPanel/HelloScreen.cs`. Remove `BuildStatePanel`, `RowClass` stays for the mode row. Update the remarks that name the state row: `HelloState.xml` builds it, `HelloRow` carries `hello-row`, and the Tab order stays. Run the build. Expect `0 Warning(s)`, and expect `grep -n 'BuildStatePanel\|\.Row\b\|AddButton' samples/HelloPanel/*.cs` to list only `mount.AddButton` and the `HelloMount` member.
- [ ] 3.2 Edit `tests/BrutalUi.Framework.Tests/BrutalUi.Framework.Tests.csproj`. Add the generator `ProjectReference` line of task 1.7 with its comment. Add `<Compile Include="../../samples/HelloPanel/HelloRow.cs" LinkBase="Shared" />`. Add three `None` items with `Link` and `CopyToOutputDirectory="PreserveNewest"` for `HelloState.xml`, `HelloDefinitions.xml`, and `HelloScreen.css`, in the form of line 21 of `BrutalUi.Markup.Tests.csproj` (A6, Notes item 4). Do not reference `HelloPanel.csproj`.
- [ ] 3.3 Write `tests/BrutalUi.Framework.Tests/Templates/SampleMarkupAllocationTests.cs` with 3 tests. `BuildScene` follows The Sample: a registry with `hello.add` (adds 1 to the store and counts), `hello.reset`, `hello.rebind`, and `hello.restore` (static handlers), a `Store<int>`, a `TemplateBindings` with `hello.score`, a `TemplateLoader` with `RegisterBrutalUiFrameworkTestsElements()`, a `DefinitionLoader` with the linked `HelloState.xml` under `HelloState.xml`, three test factories under the three factory names (a widget twin: a panel with the classes `hello-panel` and `hello-panel-root` and one label; an info twin with one label; a layout twin with the shape of `HelloScreen.BuildLayout`), and a map under `hello.actions`. It loads the linked `HelloDefinitions.xml`, asserts `Succeeded`, makes a context with a `RecordingRenderer` measurer, a default font size of 16, a viewport of 1280 by 720, and the parsed linked `HelloScreen.css`, calls `RegisterTo` and `EnsureRoot` on each controller, sets `hello.main`, runs one `Update`, finds the `Add` button by name, and caches its center and the two pointer events. `AnAddClickOnTheLoadedStatePanelAllocatesNothing` pushes a press and a release at `Add`, then runs `Update(0.016f)` and `Draw`: 2 warm-up rounds of 9 frames, `AllocationGate.ReleaseAllocationContext()`, 100 measured frames, 0 bytes. `AModeToggleOnTheLoadedSampleRecordsAllocatesNothing` sets `hello.detail` and `hello.main` in turn, then runs `Update` and `Draw`: the same rounds, 0 bytes. `TheMeasuredBodyDoesRealWork` checks that one click frame adds 1 to the store and the count and that the `score` label shows the new value, that one toggle frame makes `hello.detail` current with the info root as the one child of `bottom`, and that the state root holds a `HelloRow` with the class `hello-row`, 6 elements, a binding sum of 1, and 3 buttons with a `Clicked` slot. The measured bodies create no lambda, no string, no boxed value, and no Assert.
- [ ] 3.4 Run the build and the tests, and expect Framework +3. Run two plants. Plant 4: `GC.KeepAlive(new object());` as the first line of `LabelBindings.NumberText<T, TValue>.Apply`. Expect `AnAddClickOnTheLoadedStatePanelAllocatesNothing` to fail (the gates of `BoundTreeAllocationTests` and `TemplateBindingAllocationTests` fail with it). Plant 5: the same line as the first line of the apply step of `UiManager` (`UiManager.Apply`). Expect `AModeToggleOnTheLoadedSampleRecordsAllocatesNothing` to fail (the `ModeDiffAllocationTests` gates fail with it). Record the counts, the bytes of each plant, and the demo state in `context.md`.

**Estimated Steps:** about 35 (4 tasks, about nine steps for one task)

**Files (5):** `HelloState.cs`, `HelloScreen.cs`, `BrutalUi.Framework.Tests.csproj`, `SampleMarkupAllocationTests.cs`, `context.md`

**Acceptance Criteria:**
- [ ] The 3 new Framework tests pass, and each existing test passes.
- [ ] Plants 4 and 5 each give a non-zero exit code with 0 `error CS` lines, each fails its named test, and each restored file equals its copy under `cmp`.
- [ ] `grep -n 'HelloPanel.csproj' tests/BrutalUi.Framework.Tests/BrutalUi.Framework.Tests.csproj` gives no output.
- [ ] The headless check 4 of phase 2 still gives no markup line.

### Phase 4: The Docs Show the Sample Template
**Goal:** The README and section 11 of `technical-design.md` show the template of the sample and the load flow. Section 14 names the twenty-third gate and the eighth smoke line. `docs/overview.md`, `CHANGELOG.md`, and the epic `human-testing.md` name the result. The full gate passes.
**Demo:** the reader opens `README.md` at `### The same tree in XML` and sees the XML block of `samples/HelloPanel/HelloState.xml`, equal to the file character for character, with the C# load code under it.
**Needs:** Phase 1, Phase 2, Phase 3
**Agent tier:** STANDARD (sonnet).

**Tasks (one group, 6 files):**
- [ ] 4.1 Edit `README.md`. Rename `### The same tree in XML (planned)` to `### The same tree in XML`. Replace its body with the text of `HelloState.xml` as an `xml` block, one sentence on `HelloRow` and the generator, and a `csharp` block with the flow of The Sample (the bindings, the generated register call, `AddTemplate`, `AddFactory`, `AddActionMap`, `Load`, the error print, and `RegisterTo`). Add the note of A7: a game with its own `[TemplateElement]` types references `BrutalUi.Markup` and adds the generator analyzer line, because the packed Framework package excludes the Markup analyzers (binding-attributes Notes item 7). Make the "planned" lines current (A8): line 5 (the XML markup loader), line 16 (the XML loader), and line 28 (the `BrutalUi.Markup` row names the XML element loader, the element generator, and the stylesheet parser). Update line 151 and the `samples/` line of the repository layout to name the XML state panel.
- [ ] 4.2 Edit `docs/technical-design.md`. In section 11, **### Definition format**: add `AddFactory` to the C# block and to the rules of the object (one name table, the checks, the first entry stays), change the `Panel` and `Layout` rows and the `UnknownTemplate` row to name a text or a factory, add the factory-layout rule to the check of each layout (no check load, the slot check at `RegisterTo`), change the sentence "a successful result holds no factory that throws inside `RegisterTo`" to exclude a game factory (Notes item 12), and name the return value of `RegisterTo`. Add a paragraph **The HelloPanel sample.** with both XML files and the line `HelloPanel: markup: ...`. In section 14, add the bullet **A twenty-third allocation gate.** for `SampleMarkupAllocationTests` (3 tests, the scene, the two measured bodies, the real-work checks, and plants 4 and 5 with their bytes from `context.md`), and change the sample bullet to 8 lines with line 8, the markup check message, and the load error exit before the window.
- [ ] 4.3 Edit `docs/overview.md`: change "twenty-second gate" to "twenty-third gate" at each place (lines 23 and 55 at planning time) and name the new gate. Add one line to `CHANGELOG.md` under `### Added`, tagged `(markup/markup-sample)`: `DefinitionLoader.AddFactory`, the controller list of `RegisterTo`, the XML state panel and records of HelloPanel with `HelloRow`, smoke line 8, the load error exit, and the twenty-third gate. Edit `docs/features/markup/human-testing.md`: add `## markup-sample` with the demo line of each phase and the windowed checks of A10: the XML state panel shows in `hello.main` with the three buttons and the score; a click on `Add` raises the score; F1 and the `Mode (F1)` button toggle `hello.main` and `hello.detail`, and F2 steps back; the smoke run prints 8 lines with exit code 0; the plant of task 1.9 (remove `bind-text` from the output copy of `HelloState.xml`) gives `bindings 0`, the markup check line, and exit code 1; line 2 keeps the counts of the last measure; and `scripts/test-linux.sh` and `scripts/test-windows.ps1` print their PASS line.
- [ ] 4.4 Run the build and the tests, and expect the totals of the phase 3 close. Run `pack`, `package-restore`, and `package` of `scripts/test-linux.sh` by hand, and expect `package-test: PASS Gauge level 0.5`. Run the README check of the Verification Method (step 9). Run the headless checks 1 and 4 of phase 2 again. Record the close state in `context.md`, with the windowed checks as pending for a host with a window.

**Estimated Steps:** about 30 (4 tasks, about eight steps for one task)

**Files (6):** `README.md`, `technical-design.md`, `overview.md`, `CHANGELOG.md`, `human-testing.md`, `context.md`

**Acceptance Criteria:**
- [ ] The README check gives no difference, and `grep -n 'planned' README.md` lists no markup line.
- [ ] Section 11 holds `AddFactory` and **The HelloPanel sample.**, and section 14 holds **A twenty-third allocation gate.** and the line 8 text.
- [ ] `CHANGELOG.md` has one line tagged `(markup/markup-sample)`, and `human-testing.md` has `## markup-sample`.
- [ ] The package step prints `package-test: PASS Gauge level 0.5`.

---

## Key Technical Decisions

### Confirmed Assumptions

- Q1 Records with a C# panel: Add `DefinitionLoader.AddFactory(name, factory)` in BrutalUi.Framework. A Panel or Layout `template="..."` can then name a C# factory, and every HelloPanel record (3 panels, 1 layout, 2 modes) loads from one XML file.
- Q2 Panel to XML: The state panel (`hello.state`) moves to an XML template. Add, Rebind, and Restore use `command`, and the score label uses `bind-text`. The smoke code finds the Add button by name.
- Q3 Custom type: `HelloRow : Element`, marked `[TemplateElement("HelloRow")]`. It adds the `hello-row` class in its constructor and needs no create rows.
- Q4 Phase 1 demo: the developer runs `HelloPanel --frames 120` and reads an eighth line with loaded-panel counts above 0. The state counts still rise, and the exit code is 0.
- Q5 RegisterTo and the build rule: `RegisterTo` returns `IReadOnlyList<PanelController>` in place of `void`. The sample calls `EnsureRoot` on each controller before the first frame, so the build rule stays.
- A1 The files are `HelloState.xml` (panel template) and `HelloDefinitions.xml` (records), next to `HelloScreen.css`. The csproj copies both to the output folder, and the sample reads them from `AppContext.BaseDirectory`.
- A2 Line 8 reads `HelloPanel: markup: elements <E>, bindings <B>, commands <C>, panels <P>, layouts <L>, modes <M>.` The sample exits with code 1 when any count is 0.
- A3 A template or definition error prints each error's `ToString()` (`<source>(<line>,<column>): <Kind>: <message>`) to standard error, and the sample exits with code 1. This happens before GLFW starts, so it needs no GPU, the same as the sheet error today.
- A4 One `TemplateLoader` is built with `TemplateBindings(state.Map.Commands)` attached and calls `RegisterHelloPanelElements()`. One `DefinitionLoader` follows, then one `Load`, then `RegisterTo(Manager)`. This is the flow in definition-loader `context.md`.
- A5 The `hello.layout` layout and the `hello.info` panel stay C# factories, registered through `AddFactory`. Only `hello.state` uses an XML template.
- A6 The allocation gate is a new test in `BrutalUi.Framework.Tests` (the twenty-third gate). It links the sample XML files the way `BrutalUi.Markup.Tests` links `HelloScreen.css`, and it rebuilds the sample shape in the test. The test does not reference the HelloPanel exe.
- A7 `HelloPanel.csproj` gets the generator line `<ProjectReference ... OutputItemType="Analyzer" ReferenceOutputAssembly="false" />`. The README says that a game with its own `[TemplateElement]` types must reference `BrutalUi.Markup` too (binding-attributes Notes item 7).
- A8 The README section "The same tree in XML (planned)" and the other "planned" loader lines become current. Section 11 of `technical-design.md` gets the sample template and the `AddFactory` row. Section 14 gets the twenty-third gate.
- A9 `HelloState.Row` and its C# buttons go away from the sample. The XML template is the only state row. The widget panel stays C# as the compare.
- A10 The epic `human-testing.md` gets a `## markup-sample` section with the windowed checks: the XML state panel shows, Add raises the score, and F1 toggles the modes.

### Decision 1: `AddFactory` joins the template table
**Choice:** `DefinitionLoader` holds a second ordinal dictionary for the factories. `AddTemplate` and `AddFactory` each check both dictionaries, so one name holds a text or a factory. The resolve pass looks up the text first, then the factory.
**Rationale:** Q1 lets one `template` value name either form. One name table keeps one duplicate rule and one `UnknownTemplate` kind, so the XML format and the 13 error kinds do not change. Two dictionaries keep the existing `TryGetTemplate` and the check load of a text layout unchanged. The checks copy the checks of `AddTemplate` (the parity that the epic asks for).
**Alternatives:** A new `factory="..."` attribute (it changes the format and the error table). One dictionary of a union type (it changes `TryGetTemplate` and `CheckLayout`).

### Decision 2: A factory record gets the delegate as is
**Choice:** A `Panel` or a `Layout` that names a factory builds its record with the added delegate, with no wrapper.
**Rationale:** A loaded record with a factory is then the same as a C# record with the same factory. The record contract (a new root at each call) is the contract of the game factory, as for a C# record. `AFactoryPanelAndAFactoryLayoutLoadAndMount` pins it with `Assert.Same`.
**Alternatives:** A wrapper that checks for a `null` root (`UiManager` and `PanelController` check it already).

### Decision 3: A factory layout gets no check load, and its modes skip the slot check
**Choice:** Step 3b skips a factory layout. It has no slot list, so the slot check of each mode on it does not run. `UiManager.Register(ModeDefinition)` checks the slots inside `RegisterTo`.
**Rationale:** The loader has no `PanelContext` to pass, and a game factory can have side effects at build time (the widget factory of HelloPanel writes seven fields). A load must stay free of game code, so the load never calls a factory. The manager check still stops a bad slot before the first frame, with an `ArgumentException` and no position. The sample layout is code, so its slot names live in code. Notes item 5 records the open point.
**Alternatives:** Call the factory at load time with an empty context (side effects and a dropped root). An `AddFactory` overload with a slot list for a layout (YAGNI: no consumer needs it).

### Decision 4: `RegisterTo` returns the panel controllers
**Choice:** `RegisterTo` returns an `IReadOnlyList<PanelController>` with the controller of each panel in the order of `Panels`.
**Rationale:** A4 makes the sample call `RegisterTo`. The build rule of `HelloModes` calls `EnsureRoot` on each controller before the first frame, so no factory runs in a frame and the widget factory fills its seven label fields before `RunFrames`. `UiManager` gives no lookup by name (definition-loader Notes item 5), so the list is the only path. A return value on a `void` method keeps each existing call compiling. The user confirmed this choice as Q5. Notes item 2 records the conflict and the options.
**Alternatives:** Drop the build rule and let the roots build at the first mount (the labels do not exist before frame 0). The sample registers each record itself (against A4). A name query on `UiManager` (it changes `src/BrutalUi.Framework/Panels/`).

### Decision 5: `HelloModes` has two steps
**Choice:** The constructor takes the context and the state and needs no GPU. `Mount` takes the load result and the texture, registers, builds each root, and returns `HelloMount` with the trees that the frame loop reads.
**Rationale:** The factories are methods of `HelloModes`, and `AddFactory` needs them before the load. The load runs before the window (A3), and the checker texture exists only after the window. The factories run only inside `Mount`, after the texture exists. `HelloMount` keeps each tree reference non-null with `required` init properties, so the nullable analysis needs no `null!`. Phase 1 makes the split, so phase 2 only moves the load and the build ahead of `Glfw.Init`.
**Alternatives:** Closures over a late `HelloModes` variable (a hidden order rule). One constructor after the window (against A3).

### Decision 6: The managed build moves ahead of `Glfw.Init`
**Choice:** Phase 2 creates the font service, the texture service, the renderer, the `UiContext`, the `HelloState`, and the `HelloModes` in `Run`, before `Glfw.Init`, and passes them down.
**Rationale:** The bindings need `state.Score` and `state.Map.Commands`, and `HelloState` needs a `UiContext`, which needs the renderer as its measurer. The constructors of `ImGuiRenderer`, `ImGuiFontService`, `ImGuiTextureService`, `UiContext`, `HelloState`, and the new `HelloModes` constructor store fields and call no ImGui, GLFW, or Vulkan function (checked at planning time). So the load runs with no GPU, as `LoadSheet` does today.
**Alternatives:** A check load with stand-in stores and registries before the window and the real load after it (two definition loads, against A4).

### Decision 7: The sample check-loads `HelloState.xml` before the window
**Choice:** `HelloMarkup.Load` runs `templates.Load(stateText, "HelloState.xml")` one time and prints its errors. The tree is dropped.
**Rationale:** `DefinitionLoader` checks only the name of a panel template (definition-loader A4), so a fault in the panel text shows at the first factory call, inside `Mount`, after the window opens. A3 asks for each template error before GLFW. A4 counts one `DefinitionLoader.Load`; the check load is a `TemplateLoader.Load` (Notes item 3). A binding is pull-based and the command target holds the registry, so the dropped tree is garbage (definition-loader Risk 1).
**Alternatives:** Let the factory throw inside `Mount` (needs a window, against A3). A check load of panel templates inside `DefinitionLoader` (it changes the rule of definition-loader A4).

### Decision 8: The template names say where each tree comes from
**Choice:** The text is added under `HelloState.xml`. The factories are added under `HelloModes.CreateWidgetPanel`, `HelloModes.CreateInfoPanel`, and `HelloModes.CreateLayout`. The map is added under `hello.actions`.
**Rationale:** The template name is the source name of each `TemplateError` of a factory load (definition-loader Decision 4), so an error line names the file. A factory name that equals its method tells the reader of `HelloDefinitions.xml` where to look.
**Alternatives:** Template names equal to the record names (the error line and the XML hide the source).

### Decision 9: Line 8 counts the mounted state root
**Choice:** `Mount` counts the elements, the bindings, and the command buttons of the controller root of `hello.state` after `EnsureRoot`, and the record counts of the result. The check runs after the mode check of the smoke block.
**Rationale:** The counts then describe the panel on the screen, not the dropped check tree. A missing `bind-text` or `command` shows as a 0 (the plant of task 1.9). The check sits with the other count checks of a smoke run, as A2 and requirements line 16 say.
**Alternatives:** Count the check-load tree (a fault after the check load does not show).

### Decision 10: The gate links the sample files and `HelloRow.cs`
**Choice:** `BrutalUi.Framework.Tests` links `HelloState.xml`, `HelloDefinitions.xml`, `HelloScreen.css`, and `HelloRow.cs`, and adds the generator analyzer. The scene uses the generated `RegisterBrutalUiFrameworkTestsElements` and test twins of the three C# factories.
**Rationale:** A6 links the XML files and rebuilds the shape in the test. Linking the real `HelloRow.cs` keeps the custom type equal to the sample type with no reference to the exe, and the generator proves the register path of a second assembly. The sheet gives the real sizes, so the click hits the real button rect.
**Alternatives:** A twin `HelloRow` in the test (it can drift). A hand-written `ElementTypeInfo` (it skips the generator path).

---

## Risks & Mitigation

| # | Risk | Impact | Mitigation |
|---|------|--------|------------|
| 1 | The agent host cannot open a window, so the phase 1 demo and the smoke checks do not run there. | A sample fault stays hidden until a person runs it. | Phase 2 adds headless checks before GLFW, phase 3 proves the loaded shape in a test, and phase 4 lists each windowed check in `human-testing.md`. |
| 2 | A later change of `ImGuiRenderer` or a service constructor calls ImGui before `ImGui.CreateContext`. | Phase 2 fails at start with no context. | Decision 6 records the check. The remarks of `Run` state the rule: these constructors run before the window and must call no native function. |
| 3 | A modified output copy of an XML file survives a build (`PreserveNewest`). | A plant stays in place and later runs fail. | The plant rule restores with `cp` from the source and checks with `cmp`. |
| 4 | The new return of `RegisterTo` changes the overload of `Assert.Throws` in `TheMethodsCheckTheirArguments`. | A compile warning or error. | The lambda binds to the `Func<object?>` overload and compiles. The group 1 build shows it. |
| 5 | A warm `VBCSCompiler` keeps an old analyzer set. | `RegisterHelloPanelElements` or `RegisterBrutalUiFrameworkTestsElements` is missing. | Run `dotnet build-server shutdown` and build again (the rule of the package step). |
| 6 | A slot typo in a mode on a factory layout shows only at `RegisterTo`, with no position. | A weaker error for that case. | Decision 3 and section 11 state the rule. The sample layout is code, and `UiManager` still stops the fault before the first frame. Notes item 5 lists the options. |
| 7 | The XML state panel differs from the old C# row in a draw detail. | Line 2 counts change. | The trees have the same shape and the same classes. A person re-measures line 2 (an item of `human-testing.md`). |
| 8 | The score label buffer grows in the gate. | A false non-zero measure. | The score stays under 1000 in 118 frames, and the label and the number binding start with 32 bytes (the label rule of `HelloState`). |
| 9 | `Mount` finds no `add` button or no mode. | A run fails at build time. | `Mount` throws `InvalidOperationException` with the missing name, and `Main` prints it with exit code 1. |

---

## Dependencies

- builder-api (complete): `Find<T>` by name, the builder methods of the test twins, and `TreeCompare`.
- xml-element-loader (complete): `TemplateLoader`, `TemplateLoadResult`, `TemplateError.ToString`, and the base rows `name`, `class`, and `pickable`.
- element-generator (complete): `BrutalUi.Generators`, `[TemplateElement]`, and the generated `Register<Id>Elements`.
- binding-attributes (complete): `TemplateBindings`, `command`, and `bind-text`.
- definition-loader (Ready for Review, merged into `epic/markup`): `DefinitionLoader`, `DefinitionLoadResult`, and `DefinitionError`. This feature changes `AddFactory` (new) and `RegisterTo` (return value).
- panels-and-modes/panel-sample: the two HelloPanel modes, `UiManager`, `PanelController.EnsureRoot`, and line 7.
- No new package, no native dependency, and no change to `Directory.Packages.props`. Two new analyzer `ProjectReference` lines (the sample and the Framework tests) point at an existing project.

---

## Definition of Done

### Functional Criteria
- [ ] `DefinitionLoader.AddFactory(string, Func<PanelContext, Element>)` exists. A `template` value names a text or a factory, one name holds one of them, and a second add of one name throws `ArgumentException` in each order.
- [ ] `DefinitionLoadResult.RegisterTo` returns the panel controllers in the order of `Panels`.
- [ ] `samples/HelloPanel/HelloState.xml` and `samples/HelloPanel/HelloDefinitions.xml` exist with the text of The Two Files, and the build copies both to the output folder.
- [ ] HelloPanel registers its 3 panels, 1 layout, and 2 modes through one `DefinitionLoader.Load` and one `RegisterTo`. The state panel comes from `HelloState.xml` with `HelloRow`, three `command` buttons, and one `bind-text` label.
- [ ] A fault in either file prints one `ToString` line for each error to standard error and gives exit code 1, with no GLFW call.
- [ ] On a host with a window, `HelloPanel --frames 120` prints 8 lines, line 8 reads `HelloPanel: markup: elements 6, bindings 1, commands 3, panels 3, layouts 1, modes 2.`, lines 6 and 7 show counts above 0, and the exit code is 0.

### Quality Criteria
- [ ] `SampleMarkupAllocationTests` measures 0 bytes for 100 click frames and for 100 toggle frames, and plants 4 and 5 fail it.
- [ ] Each of plants 1 to 5 gives a non-zero exit code with 0 `error CS` lines, fails its named test, and restores equal under `cmp`.
- [ ] Each existing test passes. The only edit of an existing test is the two message strings of `DefinitionErrorTests.cs`. The Framework total grows by 8 from the baseline of `context.md`, and the other test projects keep their totals.
- [ ] The build gives `0 Warning(s)` and `0 Error(s)`. Each new public member has an XML doc comment. Each new source file has fewer than 400 lines.

### Integration Criteria
- [ ] `src/BrutalUi.Markup/`, `src/BrutalUi.Core/`, `src/BrutalUi.Framework/Panels/`, `src/BrutalUi.Generators/`, and `Directory.Packages.props` do not change.
- [ ] In `src/`, only `DefinitionLoader.cs`, `DefinitionLoadResult.cs`, and `DefinitionReader.Resolve.cs` change, and no new public type appears.
- [ ] `HelloPanel.csproj` holds the generator analyzer line, and `BrutalUi.Framework.Tests.csproj` references no sample project.
- [ ] The README section `### The same tree in XML` holds the text of `HelloState.xml`. Section 11 holds `AddFactory` and **The HelloPanel sample.** Section 14 holds **A twenty-third allocation gate.** and line 8. `CHANGELOG.md` has one line tagged `(markup/markup-sample)`. `human-testing.md` has `## markup-sample` with the checks of A10.
- [ ] The package test prints `package-test: PASS Gauge level 0.5`. On hosts with a GPU window, `scripts/test-linux.sh` and `scripts/test-windows.ps1` print their PASS line.

### Verification Method
1. Run `export DOTNET_CLI_HOME=/tmp/dui-cli-home MSBUILDDISABLENODEREUSE=1; dotnet build brutal-ui.sln -nodeReuse:false 2>&1 | tail -n 30`. Expect `0 Warning(s)` and `0 Error(s)`.
2. Run `dotnet test --solution brutal-ui.sln 2>&1 | tail -n 30`. Expect 0 failed, 3 skipped, and a total 8 above the baseline of `context.md` (3203 at planning time).
3. Run `tests/BrutalUi.Framework.Tests/bin/Debug/net10.0/BrutalUi.Framework.Tests --filter-class '<Class>' 2>&1 | tail -n 5` for each class. Expect `failed: 0` and these totals: `*DefinitionFactoryTests` 5, `*SampleMarkupAllocationTests` 3, `*DefinitionErrorTests` 12, `*DefinitionLoaderApiTests` 4, `*TemplateBindingAllocationTests` 3.
4. Run `git diff --stat 7948a78 -- src/BrutalUi.Markup src/BrutalUi.Core src/BrutalUi.Framework/Panels src/BrutalUi.Generators Directory.Packages.props`. Expect no output. Run `git diff --stat 7948a78 -- src`. Expect three files: `DefinitionLoader.cs`, `DefinitionLoadResult.cs`, and `DefinitionReader.Resolve.cs`.
5. Run `git diff 7948a78 -- src | grep -E '^\+.*public (sealed |static |readonly |abstract )*(partial )*(class|struct|interface|enum|record|delegate) '`. Expect no output.
6. Run `cmp samples/HelloPanel/HelloState.xml samples/HelloPanel/bin/Debug/net10.0/HelloState.xml && cmp samples/HelloPanel/HelloDefinitions.xml samples/HelloPanel/bin/Debug/net10.0/HelloDefinitions.xml && echo same`. Expect `same`.
7. Copy `samples/HelloPanel/bin/Debug/net10.0/HelloState.xml` to a scratch folder, change `hello.score` to `hello.scor` in the output copy, run `samples/HelloPanel/bin/Debug/net10.0/HelloPanel --frames 120; echo "exit=$?"`. Expect one line with `): UnknownBinding: 'hello.scor' is not a bound key.`, no `GLFW` line, and `exit=1`. Restore with `cp samples/HelloPanel/HelloState.xml samples/HelloPanel/bin/Debug/net10.0/` and check with `cmp`.
8. Run the sample with the clean files. On a host with no window, expect no line with `HelloState.xml(` or `HelloDefinitions.xml(`, then `GLFW init failed`, and `exit=1`. On a host with a window, expect 8 lines, line 8 as in the Functional Criteria, and `exit=0`.
9. Run `diff <(awk '/^### The same tree in XML/{s=1;next} s&&/^### /{exit} s' README.md | awk '/^```xml$/{x=1;next} x&&/^```$/{exit} x') samples/HelloPanel/HelloState.xml && echo same`. Expect `same`.
10. Run `grep -n 'AddFactory' docs/technical-design.md`, `grep -n 'twenty-third allocation gate' docs/technical-design.md docs/overview.md`, `grep -n 'HelloPanel: markup:' docs/technical-design.md`, `grep -n 'markup/markup-sample' CHANGELOG.md`, and `grep -n '## markup-sample' docs/features/markup/human-testing.md`. Expect at least one line each.
11. Run `grep -rn 'BuildStatePanel\|state\.Row\|RebindButton\|RestoreButton' samples/HelloPanel/`. Expect no output.
12. Run `wc -l samples/HelloPanel/HelloRow.cs samples/HelloPanel/HelloMarkup.cs tests/BrutalUi.Framework.Tests/Templates/DefinitionFactoryTests.cs tests/BrutalUi.Framework.Tests/Templates/SampleMarkupAllocationTests.cs src/BrutalUi.Framework/Templates/DefinitionReader.Resolve.cs`. Expect each file under 400 lines.
13. Run `dotnet pack brutal-ui.sln --no-build --configuration Debug 2>&1 | tail -n 5`, then `rm -rf artifacts/package-test && dotnet restore tests/BrutalUi.PackageTest/BrutalUi.PackageTest.csproj --force 2>&1 | tail -n 3 && dotnet build-server shutdown && dotnet run --project tests/BrutalUi.PackageTest/BrutalUi.PackageTest.csproj --no-restore 2>&1 | tail -n 3`. Expect `package-test: PASS Gauge level 0.5`.
14. Read `context.md`. Expect plants 1 to 5, each with a non-zero exit code, 0 `error CS` lines, and the expected failed test names, and the headless checks 1 to 4 of phase 2 with their lines.
15. On a host with a GPU window, run `scripts/test-linux.sh` (Linux) and `scripts/test-windows.ps1` (Windows), and work through `## markup-sample` of `human-testing.md`. Expect each PASS line and each check as written.

---

## Testing Strategy

- **Hook tests (Framework, 5):** `DefinitionFactoryTests` covers a factory panel and a factory layout with a mount, the no-check-load rule and the slot check at `RegisterTo`, the shared name table in both orders, the argument checks, and the controller list of `RegisterTo`. They prove Q1 and Decisions 1 to 4.
- **Error message (Framework, existing):** `DefinitionErrorTests` keeps its 12 tests, with the new `UnknownTemplate` text in two asserts.
- **Allocation gate (Framework, 3):** `SampleMarkupAllocationTests` measures a click on the loaded `Add` button and a mode toggle on the loaded records at 0 bytes, with a real-work test. It proves requirements line 17 and A6.
- **Sample checks (headless, 4):** the broken template key, the broken factory name, both at once, and the clean run up to the GLFW line. They prove A3 on any host.
- **Smoke run (window host):** line 8 and its check, lines 1 to 7 with their checks, and the plant of task 1.9. They prove Q4 and A2.
- **Plants (5 code plants, 4 file checks, 1 window plant):** the shared name table, the slot list of a factory layout, the controller list, the number text apply, and the mode apply.
- **No regressions:** each existing test passes. The package test guards the pack.
- **Manual checks:** the windowed items of `## markup-sample` in `human-testing.md` (A10).

| Class | Project | Phase | Tests |
|---|---|---|---|
| `DefinitionFactoryTests` | Framework | 1 | 5 |
| `DefinitionErrorTests` (2 message strings) | Framework | 2 | 0 new |
| `SampleMarkupAllocationTests` | Framework | 3 | 3 |

---

## Related Documentation

- `docs/features/markup/markup-sample/requirements.md`
- `docs/features/markup/epic-overview.md` and `docs/features/markup/epic-requirements.md`
- `docs/features/markup/definition-loader/implementation.md` and `context.md` (lines 38 and 429: the load flow; Notes item 5: no lookup by name; Decisions 4 and 15)
- `docs/features/markup/binding-attributes/implementation.md` (line 98: print `TemplateError.ToString`; Notes item 7: the Markup reference of a game)
- `docs/features/markup/element-generator/implementation.md` (Notes item 8: the analyzer line and the register call)
- `docs/technical-design.md` section 11 (**XML element format**, **Generated element types**, **Definition format**) and section 14 (the twenty-second gate, the sample bullet)
- `samples/HelloPanel/Program.cs`, `HelloModes.cs`, `HelloState.cs`, `HelloScreen.cs`, `HelloScreen.css`, `HelloPanel.csproj`
- `src/BrutalUi.Framework/Templates/DefinitionLoader.cs`, `DefinitionLoadResult.cs`, `DefinitionReader.Resolve.cs`, `DefinitionReader.Resolve.Checks.cs`, `TemplateBindings.cs`
- `tests/BrutalUi.Framework.Tests/Templates/TemplateBindingAllocationTests.cs` (the gate model), `DefinitionLoaderApiTests.cs` (the API test style), `tests/BrutalUi.Markup.Tests/BrutalUi.Markup.Tests.csproj` (the link and analyzer model)
- `scripts/test-linux.sh` and `scripts/test-windows.ps1` (the exit-code-only smoke step)
- `.beast-mode/phase-handoff.md` section 11 (the slice rules)

No harness skill in `.claude/skills/` covers a C# pattern of this feature. The plan references the project files above.

---

## Notes

1. **A reading of A3: the build order.** A3 puts the load before GLFW. Today `RunImGui` creates the renderer, the `UiContext`, and the `HelloState` after the window, and the bindings need the state. The plan keeps A3 and moves these managed objects ahead of `Glfw.Init` in phase 2 (Decision 6). Phase 1 still loads after the window (Notes item 7).
2. **Settled conflict: A4 and the build rule of `HelloModes` (Q5).** A4 makes the sample call `RegisterTo(Manager)`. `RegisterTo` returned no controller, and `UiManager` gives no lookup by name, so the sample could not call `EnsureRoot` before the first frame, and the widget factory could not fill its label fields before `RunFrames`. The user confirmed option (a) in interview round 2 (Q5). The options were: (a) `RegisterTo` returns the panel controllers (recommended, the plan, Decision 4; source-compatible). (b) Drop the build rule, build each root at its first mount in a warm-up frame, and look up the labels after frame 0 (a larger change of `Program.cs`). (c) The sample registers each record of the result itself (against A4).
3. **A reading of A4: the check load.** A4 lists one `DefinitionLoader.Load`. The sample also runs one `TemplateLoader.Load` of `HelloState.xml` before the window, because A3 asks for each template error before GLFW and `DefinitionLoader` does not load a panel template at load time (Decision 7). The flow keeps the order of A4.
4. **A reading of A6: the links.** A6 links the sample XML files and forbids a reference to the HelloPanel exe. The plan also links `HelloScreen.css` and `HelloRow.cs` and adds the generator analyzer to `BrutalUi.Framework.Tests` (Decision 10). A linked source file is not a project reference. Option: a twin `HelloRow` type in the test, with no analyzer change.
5. **Open point: the slot check of a factory layout.** Options: (a) no check load and no slot check at load time; `UiManager` checks at `RegisterTo` (recommended, the plan, Decision 3). (b) Call the factory at load time with an empty `PanelContext` (side effects in game code). (c) An `AddFactory` overload that takes the slot names of a layout (YAGNI).
6. **The demo host.** The phase 1 demo (Q4) needs a host with a GPU window. The agent host reports `GLFW init failed: PlatformUnavailable` (section 14 of `technical-design.md`), so the agent records the smoke as pending, and phase 4 adds it to `human-testing.md`. The phase 2, 3, and 4 demos run on any host.
7. **Interim state of phase 1.** In phase 1, `Mount` loads after the window and throws `InvalidOperationException` with the error lines for a load fault. `Main` prints the exception and exits with code 1. Phase 2 moves the load and the print before GLFW (A3).
8. **Interim state of phases 1 and 2.** `HelloState.Row`, its C# buttons, and `HelloScreen.BuildStatePanel` stay in the code, unmounted and unused, until phase 3 removes them (A9). The cut follows the file limit of a task group.
9. **The only edit of an existing test.** Phase 2 changes the `UnknownTemplate` message to name `AddFactory`. `DefinitionErrorTests.cs` asserts the full text in two places, so those two strings change. No other line of an existing test changes.
10. **The planned totals** come from the definition-loader close: the solution 3195 with 3 skipped and `BrutalUi.Framework.Tests` 384. The plan adds 8 Framework tests (5 in phase 1, 3 in phase 3). Task 1.1 records the real baseline and the base commit.
11. **The close of the epic.** The requirements say that this feature closes the markup epic and Phase 3. The epic rollup (`epic-overview.md`) and the phase notes of `technical-design.md` section 16 belong to `/update-epic`, so this plan does not edit them.
12. **A changed statement of section 11.** Section 11 says that a successful result holds no factory that throws inside `RegisterTo`. A layout factory of the game runs inside `RegisterTo` and can throw. Task 4.2 limits the statement to template factories.
