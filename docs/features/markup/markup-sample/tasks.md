# markup/markup-sample - Task Checklist

**Epic:** markup (feature 6 of 6)
**Last Updated:** 2026-11-19 10:00
**Progress:** 14/22 tasks complete (64%)

Agent tiers: Phase 1 ADVANCED (opus), Phase 2 ADVANCED (opus), Phase 3 STANDARD (sonnet), Phase 4 STANDARD (sonnet).

The task text comes word for word from the phase sections of `implementation.md`. That file holds the commands, the plant rules, the window rule, and the test style above its phase sections. One step is one tool call. A task costs about six to ten steps (`.beast-mode/phase-handoff.md` section 1).

---

## Phase 1: The XML State Panel in HelloPanel (9/9 complete) — ADVANCED

**Demo:** the developer runs `HelloPanel --frames 120` on a host with a window and reads an eighth line `HelloPanel: markup: elements 6, bindings 1, commands 3, panels 3, layouts 1, modes 2.` Line 6 still shows `adds`, `resets`, and `versions` above 0, and the exit code is 0.
**Needs:** none
**Agent tier:** ADVANCED (opus). This phase changes the public API of a sibling and touches six integration points with no demo run on the agent host.

**Tasks (group 1, the factory hook and the custom type, 6 files):**
- [x] 1.1 Create `docs/features/markup/markup-sample/context.md` when `/start-feature` did not. Run `git status --porcelain`. Record the base commit (`git rev-parse HEAD`, `7948a78` at planning time), the solution total with the skipped count (3195 with 3 skipped at the definition-loader close), and the total of `BrutalUi.Framework.Tests` (384 at that close). Record the Public API Change, The Sample, The Two Files, Line 8 and Its Check, the Key Technical Decisions, and the Notes of this plan. Record the tool notes: `DOTNET_CLI_HOME`, the per-class binary, the `error CS` check of a plant, the `PreserveNewest` restore rule, and the window rule.
- [x] 1.2 Edit `src/BrutalUi.Framework/Templates/DefinitionLoader.cs`. Add the field `Dictionary<string, Func<PanelContext, Element>> _factoriesByName` with `StringComparer.Ordinal`. Add `public void AddFactory(string name, Func<PanelContext, Element> factory)` with the rules of The Rules of `AddFactory`: `ArgumentNullException` for a `null` name, then for a `null` factory, `ArgumentException` for an empty name, and `ArgumentException` before the add for a name that either table holds. `AddTemplate` also checks `_factoriesByName`. Reword `DuplicateTemplateNameMessage` to name a text or a factory (no test pins the text; check with `grep -rn 'One name holds' tests`). Add `internal bool TryGetFactory(string name, out Func<PanelContext, Element>? factory)`. Update the class remarks: one name table for texts and factories, and the load calls no factory.
- [x] 1.3 Edit `src/BrutalUi.Framework/Templates/DefinitionReader.Resolve.cs`. In step 2, a `template` value that misses `TryGetTemplate` and hits `TryGetFactory` builds the `PanelDefinition` with the factory as is. In step 3, do the same for a `LayoutDefinition`, and mark the layout as a factory layout. In step 3b, skip the check load of a factory layout. In step 4, set `checkSlots` only for a layout with a slot list (`layoutSlots.ContainsKey(layoutIndex)`), so a mode on a factory layout skips the slot check and a faulty layout keeps its old behavior. Leave the `UnknownTemplate` message as it is in this phase. Keep `DefinitionReader.Resolve.Checks.cs` unchanged. Update the class remarks with the factory rule.
- [x] 1.4 Edit `src/BrutalUi.Framework/Templates/DefinitionLoadResult.cs`. `RegisterTo` returns `IReadOnlyList<PanelController>`: the return value of each `ui.Register(PanelDefinition)` in the order of `Panels`, as a `ReadOnlyCollection`. The argument check and the failed-result throw come first, as today. Update the remarks: the list serves `PanelController.EnsureRoot` before the first frame, and a throw in the middle gives no list (the call is not atomic, A6 of definition-loader).
- [x] 1.5 Write `tests/BrutalUi.Framework.Tests/Templates/DefinitionFactoryTests.cs` with 5 tests. `AFactoryPanelAndAFactoryLayoutLoadAndMount` (a text with a factory panel in `top`, a template panel in `bottom`, a factory layout, and one mode. The load succeeds, `result.Panels[0].CreateRoot` is the same delegate as the added factory, and after `RegisterTo`, `SetMode`, and one `Update`, the slot `top` holds the factory root and the slot `bottom` holds the template root). `AFactoryLayoutGetsNoCheckLoadAndItsModesSkipTheSlotCheck` (a layout factory with a call counter. After the load the counter is 0, and a mode with `<ui:Slot name="left">` on the factory layout loads with no error. `RegisterTo` then calls the factory one time and throws `ArgumentException` from `UiManager.Register(ModeDefinition)`). `AddTemplateAndAddFactoryShareOneNameTable` (`AddFactory` after `AddTemplate` of one name throws `ArgumentException` with `ParamName` `name`, and so does `AddTemplate` after `AddFactory`. In both cases the first entry stays: the loaded panel uses the first text or the first factory). `AddFactoryChecksItsArguments` (a `null` name gives `name`, a `null` factory gives `factory`, and an empty name gives `ArgumentException` with `name`). `RegisterToGivesThePanelControllersInTextOrder` (two panels give a list of 2. `list[i].Definition` is the same object as `result.Panels[i]`, and `EnsureRoot` gives the root of each template). Run the build and the tests, and expect Framework +5. Run three plants. Plant 1: `AddTemplate` skips the factory table check. Expect `AddTemplateAndAddFactoryShareOneNameTable` to fail. Plant 2: a factory layout gets an empty slot list in place of no list. Expect `AFactoryLayoutGetsNoCheckLoadAndItsModesSkipTheSlotCheck` and `AFactoryPanelAndAFactoryLayoutLoadAndMount` to fail. Plant 3: `RegisterTo` returns an empty list. Expect `RegisterToGivesThePanelControllersInTextOrder` to fail. Record the plants in `context.md`.
- [x] 1.6 Write `samples/HelloPanel/HelloRow.cs`: `[TemplateElement("HelloRow")] internal sealed class HelloRow : Element` with a `private static readonly ClassId RowClass = ClassName.Intern("hello-row");` and a public constructor with no parameters that adds the class. The class has no own property, so it has no row (Q3). The remarks state that the generator writes `RegisterHelloPanelElements` from the assembly name, that the CSS rules `.hello-row` and `.hello-row > Label` match it by class, and that a game with its own `[TemplateElement]` types references `BrutalUi.Markup` and the generator line (A7).

**Tasks (group 2, the sample, 6 files):**
- [x] 1.7 Edit `samples/HelloPanel/HelloPanel.csproj`. Add `<ProjectReference Include="../../src/BrutalUi.Generators/BrutalUi.Generators.csproj" OutputItemType="Analyzer" ReferenceOutputAssembly="false" />` with the comment of `BrutalUi.Markup.Tests.csproj` (an analyzer from a ProjectReference does not flow on). Add `<None Include="HelloState.xml" CopyToOutputDirectory="PreserveNewest" />` and the same for `HelloDefinitions.xml` (A1). Write `samples/HelloPanel/HelloState.xml` and `samples/HelloPanel/HelloDefinitions.xml` with the text of The Two Files, word for word, with LF line ends and a final LF.
- [x] 1.8 Edit `samples/HelloPanel/HelloModes.cs`. The constructor becomes `HelloModes(UiContext context, HelloState state)`: the commands, the keys, the mode button, the panel context, and the manager, as today, and no register call. The factory methods `CreateWidgetPanel`, `CreateInfoPanel`, and `CreateLayout` become `internal`; `CreateStatePanel` goes away. Add `public HelloMount Mount(TextureHandle checker)`. It sets `_checker`, reads both files from `AppContext.BaseDirectory`, runs the flow of `HelloMarkup.Load` in The Sample, and throws `InvalidOperationException` with the LF-joined `ToString` lines of each error for a failed check load or a failed definitions load (Notes item 7; phase 2 moves the flow and the print). Then it calls `RegisterTo(Manager)`, calls `EnsureRoot` on each returned controller (the build rule), copies the seven out fields of the widget factory, finds the widget root and the state root by the controller `Definition.Name`, finds the `Add` button with `stateRoot.Find<Button>("add"u8)` (Q2), sets `Main` and `Detail` from `result.Modes` by `Name.SequenceEqual`, computes `MarkupCounts`, and returns a new `HelloMount`. Add `internal sealed class HelloMount` with `required` init properties `WidgetPanel`, `GateLabel`, `InputLabel`, `StickLabel`, `MoveLabel`, `SchemeLabel`, `RebindLabel`, `Widgets`, `AddButton`, and `Markup`. `Main` and `Detail` stay on `HelloModes` as `ModeDefinition?` that `Mount` sets, because `s_mode` reads them. A missing element or a missing mode throws `InvalidOperationException` with a message that names it. Update the class remarks: the register order now comes from `RegisterTo`, the build rule uses its controllers, and the records come from `HelloDefinitions.xml`.
- [x] 1.9 Edit `samples/HelloPanel/Program.cs`. In `RunImGui`, make `new HelloModes(ui, state)` and call `modes.Mount(handle)` after `ui.AddSheet`. `RunFrames` takes the `HelloMount` in place of the eight tree arguments; its first lines copy the members into the old local names, so the frame body keeps its text. The `Add` point reads `mount.AddButton` in place of `state.AddButton` (Q2). Add `MarkupCounts Markup` as the last member of `RunResult` with its `<param>` doc, and fill it from `mount.Markup`. Print line 8 after line 7 in the form of A2, and add the markup check of Line 8 and Its Check after the mode check. Update the remarks of the class (the eight lines at :22 to :38, the exit-code list, and the `Add` button sentence at :48) and of `Run` (line 8 and the markup check). Run the build and the tests, and expect no change in this group. Run the smoke command. On a host with a window, expect the demo of this phase. On this host, expect the GLFW line, and record the smoke as pending. Record the counts and the demo state in `context.md`.

**Estimated Steps:** about 90 (9 tasks, about ten steps for one task)

**Files (group 1, 6):** `context.md`, `DefinitionLoader.cs`, `DefinitionReader.Resolve.cs`, `DefinitionLoadResult.cs`, `DefinitionFactoryTests.cs`, `HelloRow.cs`
**Files (group 2, 6):** `HelloPanel.csproj`, `HelloState.xml`, `HelloDefinitions.xml`, `HelloModes.cs`, `Program.cs`, `context.md`

**Acceptance Criteria:**
- [x] The 5 new Framework tests pass, and each existing test passes with no edit.
- [x] `git diff 7948a78 -- src/BrutalUi.Markup src/BrutalUi.Core src/BrutalUi.Framework/Panels` gives no output.
- [x] Plants 1, 2, and 3 each give a non-zero exit code with 0 `error CS` lines, and each restored file equals its copy under `cmp`.
- [x] `samples/HelloPanel/bin/Debug/net10.0/` holds `HelloState.xml` and `HelloDefinitions.xml`, equal to the source files under `cmp`.
- [x] `grep -rn 'RegisterHelloPanelElements' samples/HelloPanel/*.cs` gives one call, and the build compiles it (the generator runs on the sample).
- [x] On a host with a window, the smoke run prints 8 lines, line 8 equals the text of the Demo line, and the exit code is 0. On this host, `context.md` records the smoke as pending.

---

## Phase 2: The Load Error Exit Before the Window (5/5 complete) — ADVANCED

**Demo:** the developer changes `bind-text="hello.score"` to `bind-text="hello.scor"` in `samples/HelloPanel/bin/Debug/net10.0/HelloState.xml`, runs `HelloPanel --frames 120`, and reads one line that starts with `HelloState.xml(` and holds `): UnknownBinding: 'hello.scor' is not a bound key.` on standard error, no GLFW line, and exit code 1.
**Needs:** Phase 1
**Agent tier:** ADVANCED (opus). This phase moves the object build across the GLFW and ImGui boundary of `Program.cs` and changes the error path.

**Tasks (one group, 6 files):**
- [x] 2.1 Write `samples/HelloPanel/HelloMarkup.cs` with `internal static class HelloMarkup`: the two file name constants, the template name and factory name constants of The Two Files, the static selector `s_score` (`static s => s`), and `public static DefinitionLoadResult? Load(HelloState state, HelloModes modes, TextWriter errors)`. The method reads both files from `AppContext.BaseDirectory` and runs the flow of The Sample. It writes each `TemplateError.ToString` of the check load and each `DefinitionError.ToString` of the definitions load to `errors`, one line each, with no prefix (A3). It runs both loads, so one run gives each error, and returns `null` when one error or more exists. An IO fault throws, as `LoadSheet` does. The remarks state the A4 order, the check load of Decision 7, and the rule that the method needs no GPU and no window. Move the load code out of `HelloModes.Mount`.
- [x] 2.2 Edit `samples/HelloPanel/HelloModes.cs`. `Mount` becomes `Mount(DefinitionLoadResult definitions, TextureHandle checker)`: it throws `ArgumentNullException` for `null` and `ArgumentException` for a failed result, and starts at `RegisterTo`. Update the remarks: the constructor needs no GPU, and the factories run only inside `Mount`, after the texture exists.
- [x] 2.3 Edit `samples/HelloPanel/Program.cs`. In `Run`, after `LoadSheet` and before `Glfw.Init`, create the font service, the texture service, the renderer, the `UiContext`, the `HelloState`, and the `HelloModes`, then call `HelloMarkup.Load(state, modes, Console.Error)`. A `null` result returns `ExitFailure` with no GLFW call. Pass the renderer, the context, the state, the modes, and the result through `RunWindow` to `RunImGui`, and remove their creation from `RunImGui`; the texture service comes from `renderer.Textures`. `RunImGui` calls `modes.Mount(definitions, handle)`. Update the remarks of the class (the load of the two files before the window) and of `Run`.
- [x] 2.4 Edit `src/BrutalUi.Framework/Templates/DefinitionReader.Resolve.cs`. Put the `UnknownTemplate` message in one constant: `'<name>' is not a registered template. Add it with DefinitionLoader.AddTemplate or DefinitionLoader.AddFactory before the load.` Edit `tests/BrutalUi.Framework.Tests/Templates/DefinitionErrorTests.cs`: change the two expected strings (at :42 and :294 at planning time) to the new message and change nothing else (Notes item 9).
- [x] 2.5 Run the build and the tests, and expect no total change in this phase. Run the headless checks on the output folder. Check 1 (the demo): the broken key in `HelloState.xml` gives one `UnknownBinding` line, no GLFW line, and exit code 1. Check 2: `template="HelloModes.CreateLayot"` in `HelloDefinitions.xml` gives one line that starts with `HelloDefinitions.xml(` and holds `): UnknownTemplate: 'HelloModes.CreateLayot' is not a registered template.`, and exit code 1. Check 3: both faults in one run give both lines, sorted by file, and exit code 1. Check 4: the clean files on this host give no markup line, then the GLFW line, and exit code 1 (so the load passed before GLFW); on a host with a window, the clean run gives 8 lines and exit code 0. Restore each file with `cp` and check it with `cmp`. Record each check, the counts, and the demo state in `context.md`.

**Estimated Steps:** about 50 (5 tasks, about ten steps for one task)

**Files (6):** `HelloMarkup.cs`, `HelloModes.cs`, `Program.cs`, `DefinitionReader.Resolve.cs`, `DefinitionErrorTests.cs`, `context.md`

**Acceptance Criteria:**
- [x] Checks 1 to 4 give the expected lines and exit codes, and each restored file equals its source under `cmp`.
- [x] In `Run`, the call of `HelloMarkup.Load` comes before the first `Glfw.` call (`grep -n 'HelloMarkup.Load\|Glfw.Init' samples/HelloPanel/Program.cs` lists the load first).
- [x] `git diff <phase 1 close> -- tests/BrutalUi.Framework.Tests/Templates/DefinitionErrorTests.cs | grep -E '^[+-][^+-]'` gives four lines: the two old and the two new message strings.
- [x] Each test passes, and the solution total equals the phase 1 close.

---

## Phase 3: The Sample Shape at Zero Bytes (0/4 complete) — STANDARD

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

---

## Phase 4: The Docs Show the Sample Template (0/4 complete) — STANDARD

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

## Bugs & Issues

**Active Bugs:**
- (none)

**Fixed Bugs:**
- (none)

---

## Technical Debt

- (none)

---

## Task Status Legend

- [ ] Not started
- [ ] 🔄 In progress
- [ ] ⏸️ Blocked (waiting on something)
- [x] ✅ Completed
- [x] ❌ Cancelled/Won't do

---

## Progress Tracking

### Completed This Session (2026-11-19)
- ✅ The plan and the dev docs exist (`/plan-feature`, `/start-feature`).
- ✅ Phase 1, group 1 (tasks 1.1 to 1.6): `DefinitionLoader.AddFactory`, the resolve rule, the `RegisterTo` controller list, `DefinitionFactoryTests` (5 tests, Framework 389, solution 3200 with 3 skipped), plants 1 to 3, and `HelloRow.cs`. See `context.md`.
- ✅ Phase 1, group 2 (tasks 1.7 to 1.9): the two XML files, the generator analyzer line and the copies in `HelloPanel.csproj`, `HelloModes.Mount` with `HelloMount` and `MarkupCounts`, and smoke line 8 with the markup check in `Program.cs`. Totals unchanged (3200, failed 0, skipped 3; Framework 389). The smoke run and its plant give the GLFW line here: pending a host with a window. See `context.md`.
- ✅ Phase 1 acceptance criteria closed on the send-back: each of the six items re-checked on this host (build 0 warnings; suite total 3200, failed 0, skipped 3; `*DefinitionFactoryTests` 5 passed; src diff limited to the three planned files with no edited test; plants 1 to 3 restored equal under `cmp`; the two XML output copies equal; one generated register call compiled; the smoke recorded pending). See the close record in `context.md`.
- ✅ Phase 2 (tasks 2.1 to 2.5): `HelloMarkup.Load` with the name constants and the error print; `Mount(DefinitionLoadResult, TextureHandle)`; the renderer, context, state, modes, and load moved before `Glfw.Init` in `Run`; the `UnknownTemplate` constant message; the two test strings. Totals unchanged (3200, failed 0, skipped 3; Framework 389). Headless checks 1 to 4 give the expected lines and exit codes; the output XML copies restored equal under `cmp`. See the close record in `context.md`.

### Discovered New Tasks
- (none)

### Blocked Items
- (none)

---

*Update this file as tasks are completed. Mark tasks with ✅ immediately when done. Add new tasks as they're discovered.*
