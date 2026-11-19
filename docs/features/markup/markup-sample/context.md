# markup/markup-sample - Context & Decisions

**Epic:** markup (feature 6 of 6)
**Last Updated:** 2026-11-19 11:12
**Current Phase:** Phase 4 - The Docs Show the Sample Template (closed)
**Status:** 🟢 Feature complete: 22/22 tasks. Phase 4 closed (tasks 4.1 to 4.4, verifier PASS, zero fix rounds, Demo: PASS). The windowed checks and the `human-testing.md` copy await the coordinator.

---

## Quick Status

**What's Done:**
- ✅ The plan exists: `implementation.md`, with 4 phases and 22 tasks. The interview confirmed Q1 to Q5 and A1 to A10 over two rounds.
- ✅ The dev docs exist: `tasks.md`, `context.md`, and `inbox.md`.
- ✅ Phase 1, group 1 (tasks 1.1 to 1.6): `AddFactory` with the shared name table, the factory resolve rule, the `RegisterTo` controller list, `DefinitionFactoryTests` (5 tests, Framework 389), `HelloRow.cs`, and plants 1 to 3. See the group close record below.
- ✅ Phase 1, group 2 (tasks 1.7 to 1.9): `HelloState.xml`, `HelloDefinitions.xml`, the csproj analyzer and copy lines, `HelloModes.Mount` with `HelloMount` and `MarkupCounts`, and smoke line 8 with its markup check. Totals unchanged. See the group 2 close record below.
- ✅ Phase 2 (tasks 2.1 to 2.5): `HelloMarkup.Load` with the name constants and the error print, `Mount(DefinitionLoadResult, TextureHandle)`, the managed build and the load before `Glfw.Init`, the `UnknownTemplate` constant message, the two test strings, and headless checks 1 to 4. Totals unchanged. See the phase 2 close record below.
- ✅ Phase 3 (tasks 3.1 to 3.4): the C# state row removed from `HelloState` and `HelloScreen`, the sample files linked into `BrutalUi.Framework.Tests`, and the twenty-third gate `SampleMarkupAllocationTests` (3 tests, Framework 392). Plants 4 and 5. See the phase 3 close record below.
- ✅ Phase 4 (tasks 4.1 to 4.4): the README XML section with the load flow and the A7 note, section 11 with `AddFactory` and **The HelloPanel sample.**, section 14 with the twenty-third gate bullet and the 8-line sample bullet, `docs/overview.md` and `CHANGELOG.md`, and the human-test lines in this file. Build 0 warnings, tests 3203, package PASS, checks 1 and 4 re-run. See the phase 4 close record below.

**What's Next:**
- 📋 The feature is built and documented. The coordinator: merge `feat/markup-sample` (or the current feature branch) into the epic branch, copy the open human tests under `## Needs Human Verification` to `docs/features/markup/human-testing.md` as a `## markup-sample` section, then run `/update-feature`, `/update-epic markup`, and `/update-master`. Question `msg_e893e050f051` asks the coordinator to confirm that split; no answer had arrived at the phase close.
- 📋 README line 7 (the status quote) keeps three stale claims (the loader "not written yet", "2921 tests", "the input epic has two features left"). Phase 4 named lines 5, 16, 28, 117, 151, and the `samples/` line only, so the quote waits for `/update-feature` or `/update-master`.

**Blockers:**
- None. The phase 1 demo needs a host with a GPU window. This agent host reports `GLFW init failed: PlatformUnavailable`, so the smoke run stays pending there (Notes item 6 of `implementation.md`).

---

## Baseline Record (Task 1.1)

**Date:** 2026-11-19. Phase 1, group 1, opens from this baseline.

- `git status --porcelain`: no tracked change. The four untracked files are the feature docs (`context.md`, `implementation.md`, `inbox.md`, `tasks.md`).
- Base commit: `7948a78c1ca875fc0552190df78e0e48ea800aea`.
- Measured baseline, full solution: total 3195, failed 0, succeeded 3192, skipped 3. The planned number holds.
- Measured baseline, `BrutalUi.Framework.Tests`: total 384, failed 0, skipped 0.
- Group 1 expects Framework +5 (the five `DefinitionFactoryTests`). The delta counts from 384 and 3195.

## Tool Notes (Task 1.1)

- Export `DOTNET_CLI_HOME=/tmp/dui-cli-home` and `MSBUILDDISABLENODEREUSE=1` in each command. Give `-nodeReuse:false` to `dotnet build` only. A warning fails the build.
- Per-class run after a build: `tests/BrutalUi.Framework.Tests/bin/Debug/net10.0/BrutalUi.Framework.Tests --filter-class '*<Class>' 2>&1 | tail -n 5`.
- Plant rule: copy the file to `src/BrutalUi.Framework/obj/plantscratch/`, change it, run the full test into a scratch log, and check the `error CS` count first. It must be 0, because a plant that does not compile leaves the old binary, and the old binary passes. Expect a non-zero exit code. Restore with `cp`, check with `cmp`, and run again. A test host can exit with 134 one time; run a second time before you treat it as a fault. Scratch logs go under `obj/plantscratch/` (git-ignored), not `/tmp`: the sandbox denies `/tmp` writes here (the baseline `tee /tmp/...` produced no file).
- `PreserveNewest` restore rule: a build does not restore a changed output copy of an XML file; restore with `cp` from `samples/HelloPanel/` and check with `cmp`.
- Window rule: this agent host reports `HelloPanel: GLFW init failed: PlatformUnavailable` before any smoke line. Record the smoke as pending a host with a window.
- The projects have no implicit usings. Each new file holds its own `using` lines. Use file-scoped namespaces. When a generated register method is missing after a build, run `dotnet build-server shutdown` and build again.

## Plan Excerpts (Task 1.1)

The sections below come from `implementation.md`. Word-for-word text stays there; this record keeps the shape of each part.

### Public API Change (end state)

- `DefinitionLoader.AddFactory(string name, Func<PanelContext, Element> factory)` — new. `AddTemplate(string, ReadOnlySpan<byte>)` keeps its signature and also checks the factory names.
- `DefinitionLoadResult.RegisterTo(UiManager)` — was `void`, now returns `IReadOnlyList<PanelController>`: the controllers of `Panels`, in the order of `Panels`.
- Rules of `AddFactory` (see the Decisions section of the brief; in short): one name table holds texts and factories; a second add of one name throws `ArgumentException` before the add, in each order; the first entry stays; the checks equal `AddTemplate` (`null` name gives `ArgumentNullException` (`name`), `null` factory gives `ArgumentNullException` (`factory`), empty name gives `ArgumentException` (`name`); a factory record gets the delegate as is; the load calls no factory; a factory layout gets no check load and its modes skip the slot check; `UiManager.Register(ModeDefinition)` checks the slots inside `RegisterTo`; an unknown name gives `UnknownTemplate` (its message names both add methods from phase 2).

### The Sample (end state)

`samples/HelloPanel/` gains `HelloState.xml` (state panel template), `HelloDefinitions.xml` (3 panels, 1 layout, 2 modes), `HelloRow.cs` (`[TemplateElement("HelloRow")] HelloRow : Element`), `HelloMarkup.cs` (phase 2: reads both files, runs the load flow, prints the errors), and csproj changes (the generator analyzer line and two `PreserveNewest` copy items). `Program.Run` loads before `Glfw.Init` (phase 2); `RunImGui` calls `modes.Mount`; `RunFrames` reads the mounted trees; line 8 prints the markup counts. The widget panel, the info panel, and the layout stay C# factories (A5, A9).

### The Two Files

`HelloState.xml`: one `ui:Element` with `class="hello-panel hello-panel-root"`, one `ui:HelloRow`, three `ui:Button` with `command` (`add`, `rebind`, `restore`), and one `ui:Label name="score" pickable="false" bind-text="hello.score"`. The files stay word for word in `implementation.md`, section The Two Files.

`HelloDefinitions.xml`: panels `hello.widgets` (slot `top`, factory `HelloModes.CreateWidgetPanel`), `hello.state` (slot `bottom`, text `HelloState.xml`), `hello.info` (slot `bottom`, factory `HelloModes.CreateInfoPanel`); layout `hello.layout` (factory `HelloModes.CreateLayout`); modes `hello.main` and `hello.detail` on `hello.layout` with map `hello.actions`.

### Line 8 and Its Check

`HelloPanel: markup: elements <E>, bindings <B>, commands <C>, panels <P>, layouts <L>, modes <M>.` `E` counts the mounted state root (6), `B` sums `Element.BindingCount` (1), `C` counts `Button` elements with a `Clicked` slot (3), and `P`/`L`/`M` are the record counts (3/1/2). A count of 0 writes `HelloPanel: the markup panel did not load.` to standard error and returns exit code 1.

### Key Technical Decisions (one line each)

- Decision 1: a second ordinal dictionary for the factories; both add methods check both tables.
- Decision 2: a factory record gets the delegate as is, no wrapper (pinned with `Assert.Same`).
- Decision 3: a factory layout gets no check load; its modes skip the slot check; `UiManager` checks the slots inside `RegisterTo`.
- Decision 4: `RegisterTo` returns the panel controllers, in the order of `Panels`; the return keeps each existing call compiling.
- Decisions 5 to 10 cover the sample shape, the load order, the names, and the gate; they stay in `implementation.md`.

### Notes (from the plan)

1. A3 build order: phase 1 loads after the window; phase 2 moves the managed build and the load before `Glfw.Init` (Decision 6).
2. Q5 settled the A4/build-rule conflict: `RegisterTo` returns the controllers (option a).
3. A4 counts one `DefinitionLoader.Load`; the `TemplateLoader.Load` check of `HelloState.xml` is a separate check load (Decision 7).
4. A6 links the sample files and `HelloRow.cs` into the Framework tests; a linked file is not a project reference.
5. Open point: a slot typo on a factory layout shows only at `RegisterTo`, with no position (Decision 3).
6. The demo host needs a GPU window; record the smoke as pending here.
7. Phase 1 interim: `Mount` loads after the window and throws `InvalidOperationException` with the error lines.
8. Interim: `HelloState.Row`, its C# buttons, and `HelloScreen.BuildStatePanel` stay unmounted and unused until phase 3. Do not touch them.
9. Phase 2 edits only two message strings in `DefinitionErrorTests.cs`. No other existing-test edit.
10. Planned totals: the solution grows 3195 → 3203 (+8 Framework); the real baseline above.
11. The epic rollup belongs to `/update-epic`.
12. Section 11's "no factory throws inside RegisterTo" statement gets limited to template factories (phase 4).

---

## Key Files

### Core Implementation
- `src/BrutalUi.Framework/Templates/DefinitionLoader.cs` - The definition loader. Phase 1 adds `AddFactory` and the factory table.
- `src/BrutalUi.Framework/Templates/DefinitionReader.Resolve.cs` - The resolve pass. Phase 1 resolves a `template` value to a factory. Phase 2 changes the `UnknownTemplate` message.
- `src/BrutalUi.Framework/Templates/DefinitionLoadResult.cs` - Phase 1 makes `RegisterTo` return `IReadOnlyList<PanelController>` (Q5).
- `samples/HelloPanel/HelloRow.cs` - New. The custom `[TemplateElement("HelloRow")]` type (Q3).
- `samples/HelloPanel/HelloState.xml` and `samples/HelloPanel/HelloDefinitions.xml` - New. The state panel template and the six records (A1).
- `samples/HelloPanel/HelloMarkup.cs` - New in phase 2. It runs the load before the window and prints the errors (A3).
- `samples/HelloPanel/HelloModes.cs`, `samples/HelloPanel/Program.cs`, `samples/HelloPanel/HelloPanel.csproj` - The sample changes: the mount step, line 8, and the generator line.
- `tests/BrutalUi.Framework.Tests/Templates/DefinitionFactoryTests.cs` - New in phase 1. 5 tests.
- `tests/BrutalUi.Framework.Tests/Templates/SampleMarkupAllocationTests.cs` - New in phase 3. The twenty-third gate, with 3 tests.

### Related Files
- `docs/features/markup/markup-sample/implementation.md` - The implementation plan.
- `docs/features/markup/definition-loader/context.md` - The load flow of A4 (lines 38 and 429).
- `docs/features/markup/binding-attributes/implementation.md` - Notes item 7: a game with its own `[TemplateElement]` types references `BrutalUi.Markup`.
- `docs/features/markup/element-generator/implementation.md` - Notes item 8: the analyzer `ProjectReference` line.
- `docs/technical-design.md` - Section 11 (Markup) and section 14 (the gates and the smoke lines).

---

## Important Decisions

### Decision 1: The confirmed interview items
**Date:** 2026-11-19
**Context:** `/plan-feature` ran two interview rounds. Round 2 settled a conflict between A4 and the build rule of `HelloModes`.
**Decision:** The plan uses Q1 to Q5 and A1 to A10, word for word, under `### Confirmed Assumptions` in `implementation.md`. In short: `AddFactory` (Q1), the XML state panel (Q2), `HelloRow` (Q3), the smoke line 8 demo (Q4), and `RegisterTo` returns the controllers (Q5).
**Rationale:** The user confirmed each item ("all hold" after round 1 and after round 2).
**Impact:** Two members of the definition-loader API change: `AddFactory` is new, and `RegisterTo` returns a list. Both changes are additive and source-compatible.

### Decision 2: The approach is "hook first, then sample"
**Date:** 2026-11-19
**Context:** `/plan-feature` step 5.
**Decision:** Phase 1 adds the factory hook with its tests and switches the sample. Phases 2 to 4 add the error exit, the gate, and the docs.
**Rationale:** The demo comes early, and the Framework API ships with its tests in the same slice.
**Impact:** Phases 1 and 2 are ADVANCED (opus). Phases 3 and 4 are STANDARD (sonnet).

The Key Technical Decisions of `implementation.md` (Decisions 1 to 10) hold the rest. Do not copy them here. Record a new decision here when the work makes one.

---

## Gotchas & Learnings

- The projects have no implicit usings. `IReadOnlyList<T>` needs `using System.Collections.Generic;` in each new test file: the first build of `DefinitionFactoryTests.cs` failed with `CS0246` without it.
- `dotnet test --solution` gives exit code 2 for a failed test, not 1. The plant rule needs only a non-zero code.
- The plant scratch folder already holds `.orig` copies from earlier features. Group 1 uses the names `DefinitionLoader.cs.orig`, `DefinitionReader.Resolve.cs.orig`, and `DefinitionLoadResult.cs.orig`.
- `UiManager.Register(PanelDefinition)` defers the root build (the controller calls the factory in the mode diff). `UiManager.Register(LayoutDefinition)` calls the layout factory in the register call, so `RegisterTo` calls each layout factory one time.

---

## Phase 1 Group 1 Close Record (Tasks 1.2 to 1.6)

**Date:** 2026-11-19. The solution builds with `0 Warning(s)` and `0 Error(s)`.

### Totals

| Measure | Baseline (task 1.1) | Group 1 close | Delta |
|---|---|---|---|
| Solution total | 3195 (3 skipped, 0 failed) | 3200 (3 skipped, 0 failed) | +5 |
| `BrutalUi.Framework.Tests` | 384 | 389 | +5 |
| `*DefinitionFactoryTests` | (none) | 5, `failed: 0` | new |
| `*Definition*` classes | 27 | 32 | +5 |

### Plants

| # | Fault | `error CS` | Exit | Failed tests | Restore |
|---|---|---|---|---|---|
| 1 | `AddTemplate` skips the `_factoriesByName` check | 0 | 2 | `AddTemplateAndAddFactoryShareOneNameTable` (`Assert.Throws: No exception was thrown`, the `AddTemplate` after `AddFactory` order) | `cmp` equal |
| 2 | A factory layout gets an empty slot list in place of no list | 0 | 2 | `AFactoryLayoutGetsNoCheckLoadAndItsModesSkipTheSlotCheck` and `AFactoryPanelAndAFactoryLayoutLoadAndMount` (total 3200, failed: 2) | `cmp` equal |
| 3 | `RegisterTo` returns an empty list | 0 | 2 | `RegisterToGivesThePanelControllersInTextOrder` | `cmp` equal |

### Implementation Decisions (blast radius)

- The resolve pass keeps a parallel `bool[] layoutIsFactory` marker from step 3. Step 3b skips a marked layout, and step 4 answers the slot check only for a layout with a slot list (`layoutSlots.ContainsKey`). A faulty template-text layout keeps the old behavior: no list and the follow-on skip. No change to `DefinitionReader.Resolve.Checks.cs` and no change to the `UnknownTemplate` message (phase 2 owns it).
- `AddFactory` checks `name`, then `factory`, then the empty name, then both tables, before the add. The new duplicate message reads "One name holds one template text or one factory. ..." No test pins the text (verified with `grep -rn 'One name holds' tests`: only a stale `bin/` XML doc file matched).
- `RegisterTo` collects the controllers in a `PanelController[]` and wraps them in a `ReadOnlyCollection` at the end. A throw during any register call gives no list, as the A6 note states.
- Risk 4 stood: the `Assert.Throws` lambdas in the existing `DefinitionLoaderApiTests.TheMethodsCheckTheirArguments` and `RegisterToChecksTheResultAndTheManager` bind to the `Func<object?>` overload with no edit. The full suite proves it.
- `HelloRow.cs` compiles with no generator line in `HelloPanel.csproj` (Notes item 8). Nothing calls `RegisterHelloPanelElements` yet, so `grep -rn 'RegisterHelloPanelElements' samples/HelloPanel/*.cs` lists only the remarks line of `HelloRow.cs`. Group 2 adds the one real call.
- `HelloState.Row`, the C# buttons, and `HelloScreen.BuildStatePanel` stay untouched (Notes item 8).
- `git diff 7948a78 -- src/BrutalUi.Markup src/BrutalUi.Core src/BrutalUi.Framework/Panels` gives no output. In `src/`, only the three planned files change, and no new public type appears.

### Follow-up Work

- Group 2 (tasks 1.7 to 1.9) switches the sample: the two XML files, the analyzer line, `HelloModes.Mount`, `HelloMount`, `MarkupCounts`, and smoke line 8 with its check.
- Phase 2 owns the `UnknownTemplate` message change and the two `DefinitionErrorTests.cs` strings.

---

## Phase 1 Group 2 Close Record (Tasks 1.7 to 1.9)

**Date:** 2026-11-19. The solution builds with `0 Warning(s)` and `0 Error(s)`.

### Totals

| Measure | Group 1 close | Group 2 close | Delta |
|---|---|---|---|
| Solution total | 3200 (3 skipped, 0 failed) | 3200 (3 skipped, 0 failed) | none, as expected |
| `BrutalUi.Framework.Tests` | 389 | 389 | none |

### Sample Changes

- `HelloPanel.csproj`: the generator analyzer line with the `BrutalUi.Markup.Tests.csproj` comment, changed "test" to "sample" for this project, plus the two `PreserveNewest` `None` items (A1).
- `HelloState.xml` and `HelloDefinitions.xml`: the text of The Two Files, word for word, LF, final LF. The output copies under `bin/Debug/net10.0/` equal the sources under `cmp`.
- `HelloModes.cs`: the constructor loses the checker parameter and all register calls. `Mount(TextureHandle)` runs the flow of A4 inline (Notes item 7; phase 2 moves it to `HelloMarkup.Load`), throws `InvalidOperationException` with the LF-joined error lines for a failed check load or definitions load, calls `RegisterTo`, calls `EnsureRoot` on each controller, finds the roots by the controller `Definition.Name` and the `Add` button with `stateRoot.Find<Button>("add"u8)` (Q2), sets `Main` and `Detail` from `result.Modes` by `Name.SequenceEqual`, computes `MarkupCounts` once (Decision 9), and returns `HelloMount`. `CreateStatePanel` is gone; the three kept factories are `internal`.
- `Program.cs`: `RunImGui` calls `new HelloModes(ui, state)` then `modes.Mount(handle)` after `ui.AddSheet`, before `SetMode`. `RunFrames` takes the `HelloMount` in place of the eight tree arguments, copies them into the old local names, and aims the `Add` click at `mount.AddButton`. `RunResult` gains `MarkupCounts Markup` as the last member. Line 8 prints after line 7 in the A2 form; the markup check runs after the mode check and writes `HelloPanel: the markup panel did not load.` with exit code 1 on a 0 count.

### The Register Call

`grep -rn 'RegisterHelloPanelElements' samples/HelloPanel/*.cs` gives one comment line in `HelloRow.cs` and one real call in `HelloModes.cs:220`. The build compiles the call, so the generator runs on the sample.

### Counts and Demo State

The expected line 8 is `HelloPanel: markup: elements 6, bindings 1, commands 3, panels 3, layouts 1, modes 2.` The counts come from the tree walk in `CountTree` over the mounted state root and from `result.Panels.Count`, `result.Layouts.Count`, and `result.Modes.Count`. The measured demo needs a host with a window.

### Smoke and Plant State

- Smoke run on this host: `HelloPanel: GLFW init failed: PlatformUnavailable (Failed to detect any supported platform).` with exit code 1 before any smoke line, exactly the window rule. Smoke pending a host with a window. The phase 1 demo (line 8 with the counts above, line 6 counts above 0, exit code 0) is pending there too. Phase 4 adds the windowed checks to the epic `human-testing.md`.
- Plant (task 1.9 option): remove `bind-text` from the output copy of `HelloState.xml` and expect line 8 with `bindings 0`, the markup check line, and exit code 1. On this host the smoke stops at the GLFW line before `Mount` runs (phase 1 loads after the window), so the plant shows nothing here. Not run; recorded pending with the rest of the smoke. The output XML files were not touched, and both `cmp` checks pass.

### Implementation Decisions (blast radius)

- Group 2 changes no `src/` file. Only the three sample files change and two sample XML files appear; `HelloState.Row`, the C# buttons, and `HelloScreen.BuildStatePanel` stay untouched (Notes item 8).
- `HelloModes.Main` and `Detail` become nullable properties that `Mount` sets. `s_mode` reads them with `ReferenceEquals`, so the null compare is safe. `Program.cs` compares them with `ReferenceEquals` too.
- The `Mount` flow keeps one private static readonly `s_score` selector (`Func<int, int>`, the `AddText<T, TValue>` overload, like `HelloState.s_scoreText`). No lambda is created per call.
- `JoinErrors<T>` constrains `T : struct` because both error kinds are readonly structs; the unconstrained form tripped CS8602.
- Migration risk for phase 2: the flow and the print move out of `Mount` into `HelloMarkup.Load`, and the throw becomes the null result with the error lines on standard error. The file names, the template name `"HelloState.xml"`, and the three factory names stay the literals used here, so phase 2 can lift them to constants without a rename.

### Follow-up Work

- Phase 2: `HelloMarkup.Load`, the `Mount(DefinitionLoadResult, TextureHandle)` split, the load before `Glfw.Init`, the `UnknownTemplate` message, and the four headless checks.
- Phase 1 demo and the task 1.9 plant: verify on a host with a window.

---

## Phase 2 Close Record (Tasks 2.1 to 2.5)

**Date:** 2026-11-19. Base commit `949fda33e89acfa3274b6fa18b690ff86d35183d` (the phase 1 close). The solution builds with `0 Warning(s)` and `0 Error(s)`; build exit code 0.

### Totals

| Measure | Phase 1 close | Phase 2 close | Delta |
|---|---|---|---|
| Solution total | 3200 (3 skipped, 0 failed) | 3200 (3 skipped, 0 failed) | none, as expected |
| `BrutalUi.Framework.Tests` | 389 | 389 | none |
| `*DefinitionErrorTests` | 12 | 12, `failed: 0` | none |

`dotnet test --solution brutal-ui.sln`: `total: 3200, failed: 0, succeeded: 3197, skipped: 3`, exit 0. The per-assembly Framework run: `total: 389, failed: 0`.

### What Moved

- New `samples/HelloPanel/HelloMarkup.cs` (120 lines): `internal static class HelloMarkup` with the constants `StateFileName`, `DefinitionsFileName`, `StateTemplateName`, `WidgetPanelFactoryName`, `InfoPanelFactoryName`, `LayoutFactoryName`, `ActionsMapName`, the private `ScoreKey`, the static selector `s_score` (`static s => s`), and `public static DefinitionLoadResult? Load(HelloState state, HelloModes modes, TextWriter errors)`. The method reads both files from `AppContext.BaseDirectory`, runs the A4 flow with the check load of Decision 7, prints each `TemplateError.ToString` then each `DefinitionError.ToString` to `errors` one line each with no prefix, and returns `null` when one error or more exists. An IO fault throws.
- `HelloModes.Mount` is now `Mount(DefinitionLoadResult definitions, TextureHandle checker)`: `ArgumentNullException.ThrowIfNull(definitions)`, then `ArgumentException` for a failed result, then the method starts at `RegisterTo`. The file reads, the loaders, the flow, the throw-with-joined-lines, the `s_score` selector, and `JoinErrors` left the class. The remarks now state that the constructor needs no GPU and the factories run only inside `Mount`, after the texture exists.
- `Program.Run` builds `ImGuiFontService`, `ImGuiTextureService`, `ImGuiRenderer`, `UiContext`, `HelloState`, and `HelloModes`, then calls `HelloMarkup.Load(state, modes, Console.Error)`, all before `Glfw.Init`; a `null` result returns `ExitFailure` with no GLFW call. The five objects and the result pass through `RunWindow` to `RunImGui`, which no longer creates them; `RunImGui` takes the texture service from `renderer.Textures` and calls `modes.Mount(definitions, handle)`. The class remarks, the `Run` remarks, and the `RunWindow`/`RunImGui` docs carry the new order and the Decision 6 no-native-call rule.
- `DefinitionReader.Resolve.cs`: one constant `UnknownTemplateMessage` holds the fixed part, `' is not a registered template. Add it with DefinitionLoader.AddTemplate or DefinitionLoader.AddFactory before the load.`; each call site interpolates the quoted name before it (the constant form keeps the message in one place; the `'<name>'` shape of the plan holds at emit time). `DefinitionErrorTests.cs`: the two expected strings updated, nothing else (Notes item 9).

### Headless Checks (on `samples/HelloPanel/bin/Debug/net10.0/`)

Plants ran on the output copies; each file was restored with `cp` from `samples/HelloPanel/` and verified with `cmp` (all equal). The temporary `.orig` copies under the output folder were removed after the checks.

- **Check 1 (the demo):** `bind-text="hello.score"` to `bind-text="hello.scor"` in the output `HelloState.xml`. One line on standard error, no GLFW line, exit code 1:
  `HelloState.xml(6,45): UnknownBinding: 'hello.scor' is not a bound key. Register it with TemplateBindings.AddText on the TemplateBindings object that this loader attached first, before the load. ...`
  The definitions load ran too and gave no line, so the check passed on its own. `cmp` after restore: equal.
- **Check 2:** `template="HelloModes.CreateLayout"` to `template="HelloModes.CreateLayot"` in the output `HelloDefinitions.xml`. One line on standard error, exit code 1:
  `HelloDefinitions.xml(5,34): UnknownTemplate: 'HelloModes.CreateLayot' is not a registered template. Add it with DefinitionLoader.AddTemplate or DefinitionLoader.AddFactory before the load.`
  `cmp` after restore: equal.
- **Check 3:** both faults in one run. Two lines on standard error, exit code 1, in this order: the `HelloState.xml(6,45): UnknownBinding: ...` line first, then the `HelloDefinitions.xml(5,34): UnknownTemplate: ...` line. The observed order is sorted by file: the check load prints before the definitions load, and the check-load source name is the first file. `cmp` after restore: both equal.
- **Check 4:** clean files. No markup line, then `HelloPanel: GLFW init failed: PlatformUnavailable (Failed to detect any supported platform).` on standard error, exit code 1. The load passed before GLFW, exactly the window rule: the failure is the window, not the markup. On a host with a window this run gives the eight lines and exit code 0 (pending).

### Acceptance Checks

- `grep -n 'HelloMarkup.Load\|Glfw.Init' samples/HelloPanel/Program.cs`: the call at :422 comes before the first `Glfw.Init` at :428 (the earlier matches are doc crefs).
- `git diff 949fda33 -- tests/BrutalUi.Framework.Tests/Templates/DefinitionErrorTests.cs | grep -E '^[+-][^+-]'`: exactly four lines, the two old strings and the two new strings.
- The full suite passes with the phase 1 totals.

### Blast Radius

- `git diff --stat 949fda33`: the four planned files (`HelloModes.cs`, `Program.cs`, `DefinitionReader.Resolve.cs`, `DefinitionErrorTests.cs`) plus the new `HelloMarkup.cs`. No other file changed; no git write ran.
- The one `src/` change is the `UnknownTemplate` message and its constant. The message is public text: the only tests that pin it are the two strings edited here. `BrutalUi.Core`, `BrutalUi.Markup`, and `BrutalUi.Framework/Panels` are untouched.
- The managed-before-GLFW build moves the `UiManager` constructor slot write (Decision 6) before `Glfw.Init`; the constructors store fields and call no ImGui, GLFW, or Vulkan function, checked at planning time and kept in the remarks of `Run`. `HelloState.Row`, the C# buttons, and `HelloScreen.BuildStatePanel` stay untouched until phase 3 (Notes item 8).
- Phase 3 impact: `HelloMarkup.Load` is the flow the gate test duplicates; `SampleMarkupAllocationTests` links `HelloRow.cs` and the two XML files, not `HelloMarkup.cs`, so the sample-only class needs no test hook.

### Demo State

The phase 2 demo (check 1) passes on this host: one `UnknownBinding` line on standard error, no GLFW line, exit code 1. The clean windowed run (eight lines, exit 0) and the phase 1 demo stay pending on a host with a window.

### Migration Risks and Follow-up Work

- Risk: a game that copies the sample must build the six managed objects before `Glfw.Init` only if their constructors stay native-free; the `Run` remarks carry the rule. No follow-up is due inside this feature.
- Follow-up (phase 3): delete `HelloState.Row`, the C# buttons, and `HelloScreen.BuildStatePanel`; add the twenty-third gate.

---

## Phase 3 Close Record (Tasks 3.1 to 3.4)

**Date:** 2026-11-19. Base: the phase 2 working tree (`949fda33` plus the phase 1 and 2 changes, no new commit). The solution builds with `0 Warning(s)` and `0 Error(s)`; build exit code 0.

### Totals

| Measure | Phase 2 close | Phase 3 close | Delta |
|---|---|---|---|
| Solution total | 3200 (3 skipped, 0 failed) | 3203 (3 skipped, 0 failed) | +3 |
| `BrutalUi.Framework.Tests` | 389 | 392 | +3 |
| `*SampleMarkupAllocationTests` | (none) | 3, `failed: 0` | new |

### What Changed

- `HelloState.cs`: `Row`, `AddButton`, `RebindButton`, `RestoreButton`, `RowClass`, and `s_scoreText` are gone with their build lines (A9). The remarks now name `HelloState.xml` as the state panel, `HelloMarkup` as the holder of the `hello.score` binding, and keep the label rule.
- `HelloScreen.cs`: `BuildStatePanel` is gone. `RowClass` stays for the mode row. The remarks name `HelloState.xml` and `HelloRow` for the state row; the Tab order text stays.
- `BrutalUi.Framework.Tests.csproj`: the generator `ProjectReference` analyzer line with the task 1.7 comment, `<Compile Include="../../samples/HelloPanel/HelloRow.cs" LinkBase="Shared" />`, and three `None` items with `Link` and `CopyToOutputDirectory="PreserveNewest"` for `HelloState.xml`, `HelloDefinitions.xml`, and `HelloScreen.css` (Decision 10). No `HelloPanel.csproj` reference.
- `SampleMarkupAllocationTests.cs`: the twenty-third gate, 3 tests. `BuildScene` follows `HelloMarkup.Load`: the registry with the four commands, the `Store<int>`, the `TemplateBindings` with `hello.score`, the `TemplateLoader` with the generated `RegisterBrutalUiFrameworkTestsElements()`, the check load, the `DefinitionLoader` with the template text, the three factory twins under the three factory names, the map under `hello.actions`, the linked definitions load with `Assert.True(result.Succeeded)`, the `RecordingRenderer` measurer with `DefaultFontSize` 16, the viewport 1280 by 720, the parsed linked sheet, `RegisterTo`, `EnsureRoot` on each controller, `hello.main`, one `Update`, and the cached `Add` center with the two pointer events.

### Gates and Demo

- Demo (this host, no window needed): `BrutalUi.Framework.Tests --filter-class '*SampleMarkupAllocationTests'` gives `total: 3`, `failed: 0`. `AnAddClickOnTheLoadedStatePanelAllocatesNothing` measures 0 bytes over 100 click frames on the panel that `HelloState.xml` builds.
- `AnAddClickOnTheLoadedStatePanelAllocatesNothing`: 2 warm-up rounds of 9 frames, `AllocationGate.ReleaseAllocationContext()`, 100 measured click frames, 0 bytes.
- `AModeToggleOnTheLoadedSampleRecordsAllocatesNothing`: the same rounds; each frame sets `hello.detail` and `hello.main` in turn by the frame parity, then runs `Update` and `Draw`; 0 bytes.
- `TheMeasuredBodyDoesRealWork`: one click frame adds 1 to the store and the count and shows `1` in the `score` label; one toggle frame makes `hello.detail` current with the info root as the one child of `bottom`; the state root holds a `HelloRow` with the class `hello-row`, 6 elements, a binding sum of 1, and 3 buttons with a `Clicked` slot.

### Plants

| # | Fault | `error CS` | Exit | Failed tests | Bytes | Restore |
|---|---|---|---|---|---|---|
| 4 | `GC.KeepAlive(new object());` first line of `LabelBindings.NumberText<T, TValue>.Apply` | 0 | 2 | `AnAddClickOnTheLoadedStatePanelAllocatesNothing` (named), plus 6 gates on the same path: `BoundTreeAllocationTests` (3), `TemplateBindingAllocationTests.UpdateAndDrawOnALoadedBoundTreeAllocateNothing`, `ModeDiffAllocationTests` (2 toggle gates) | named 2400; BoundTree 93600; TemplateBinding 4800; ModeDiff 2400 | `cmp` equal |
| 5 | `GC.KeepAlive(new object());` first line of `UiManager.Apply` | 0 | 2 | `AModeToggleOnTheLoadedSampleRecordsAllocatesNothing` (named), plus `ModeDiffAllocationTests` (2 toggle gates) and `ModeDiffTests.SetModeAndTheDiffAllocateNothingAfterWarmUp` | named 2400; ModeDiff 9600 | `cmp` equal |

The named test of each plant fails with the plant and passes without it. The co-failures ride the same applied path: the number binding of plant 4 also runs inside the toggle bodies, and `UiManager.Apply` of plant 5 also runs inside the eighteenth gate. Logs: `obj/plantscratch/p4.log`, `p5.log`.

### Implementation Decisions (blast radius)

- Decision 10 holds: the gate links the sample files and `HelloRow.cs` and adds the generator analyzer; it does not reference `HelloPanel.csproj` (acceptance grep: no output). The generator writes `RegisterBrutalUiFrameworkTestsElements` from the assembly name with the marked linked `HelloRow`; the build compiles the call.
- The toggle measured body alternates the two modes by the frame `Round` parity, so each measured frame runs one real mode diff and the body holds no lambda, no string, no boxed value, and no `Assert`. `TheMeasuredBodyDoesRealWork` asserts that round 1 ends on `hello.detail` with the info root in `bottom`, so a body that never toggles cannot pass.
- The three factory twins: the widget twin and the info twin are panels with `hello-panel` and `hello-panel-root` and one label; the layout twin has the shape of `HelloScreen.BuildLayout` (`hello-slot-root`, the `top` and `bottom` slots, none pickable). The roots build once, so no factory runs in a frame.
- The test commands: `hello.add` adds 1 to the store and the count; `hello.reset` sets 0 and counts; `hello.rebind` and `hello.restore` count through one static handler. The sample rebind semantics need map actions that the gate does not exercise, so the twins keep the handler shape, not the listen.
- Group 1 changes no `src/` file in the end state: both plant files restore equal under `cmp`, and `git status --porcelain src/` gives no output after the plants.
- Task 3.1 grep: `grep -n 'BuildStatePanel\|\.Row\b\|AddButton' samples/HelloPanel/*.cs` lists only `HelloMount.AddButton` (the definition and the two doc crefs) and `mount.AddButton`. Notes item 8 closes: the interim C# state row is gone.
- Headless check 4 of phase 2 still holds after the edits: the clean run gives no markup line, then `HelloPanel: GLFW init failed: PlatformUnavailable`, exit code 1. The output copies of the two XML files equal the sources under `cmp`.

### Gotcha (new)

- After a plant restore, the per-class binary run keeps the planted `BrutalUi.Framework.dll` copy under the test `bin/` until the test project rebuilds. The re-run after a restore must build the test project (or run `dotnet test`); the phase 3 re-check hit this once: the class gave 2400 bytes again, and gave 0 after `dotnet build tests/...csproj`.

### Demo State

The phase 3 demo passes on this host (the gate needs no window). The smoke run still stops at the GLFW line here: the eight-line run and the earlier windowed items stay pending on a host with a window.

**Fix round 1 (2026-11-19 10:40):** The verifier fault: `SampleMarkupAllocationTests.cs` was 472 lines, over the 400-line cap. Fix: split the class into two partial files; `BuildScene`, the nested types, the ids, the handlers, and the helpers moved to `SampleMarkupAllocationScene.cs` (297 lines); `SampleMarkupAllocationTests.cs` keeps all test bodies and is now 185 lines. Re-check: build 0 warnings, 0 errors; class run total 3, failed 0; suite total 3203, failed 0, skipped 3; Framework 392.

---

## Phase 4 Close Record (Tasks 4.1 to 4.4)

**Date:** 2026-11-19. Base: the phase 3 close `6eb10d9` (no new commit; no git write ran). The phase is docs only, plus the runs of task 4.4. The solution builds with `0 Warning(s)` and `0 Error(s)`; build exit code 0.

### Totals

| Measure | Phase 3 close | Phase 4 close | Delta |
|---|---|---|---|
| Solution total | 3203 (3 skipped, 0 failed) | 3203 (3 skipped, 0 failed) | none, as expected |
| `BrutalUi.Framework.Tests` | 392 | 392 | none |

`dotnet test --solution brutal-ui.sln`: `total: 3203, failed: 0, succeeded: 3200, skipped: 3`. The phase 3 totals hold: no code or test changed.

### What Changed

- `README.md`: the section is now `### The same tree in XML`, with the text of `HelloState.xml` as the `xml` block, one sentence on `HelloRow` and the generator, a `csharp` block with the load flow (`TemplateBindings`, `AttachTo`, `RegisterHelloPanelElements`, `AddTemplate`, three `AddFactory` calls, `AddActionMap`, `Load`, the error print, and `RegisterTo` with the controller list), and the A7 note on the `BrutalUi.Markup` reference and the analyzer line. Line 5, line 16, and line 28 are current; line 28 names the XML element loader, the element generator, and the stylesheet parser. The `UiManager` line names `HelloDefinitions.xml` and the C# factories, and the `samples/` line names the XML state panel.
- `docs/technical-design.md`: section 11 **### Definition format** holds the `AddFactory` line in the C# block with the named `RegisterTo` return, the `Panel`, `Layout`, and `UnknownTemplate` rows now name a text or a factory, the **The factories.** paragraph states the game-factory rule (delegate as is, no call at load, game panel factory at the first mount, game layout factory inside `RegisterTo`), the **The check of each layout** paragraph states the factory-layout rule (no check load, the slot check at `RegisterTo` with no position) and limits the no-throw sentence to template factories (Notes item 12), and **The rules of the object** states one name table, the `AddFactory` checks, the first-entry rule, the text-then-factory lookup, and the `IReadOnlyList<PanelController>` return. **The HelloPanel sample.** holds both XML files and the line `HelloPanel: markup: elements 6, bindings 1, commands 3, panels 3, layouts 1, modes 2.` Section 14 holds the new bullet **A twenty-third allocation gate.** (the scene, the two measured bodies, the real-work checks, and plants 4 and 5 with their bytes from the phase 3 close record) and the sample bullet now lists 8 lines with the markup check message and the load error exit before `Glfw.Init`.
- `docs/overview.md`: the epic roll line and the v2.8 line now name markup-sample's `SampleMarkupAllocationTests` as the twenty-third allocation gate. See the reading decision below.
- `CHANGELOG.md`: one line under `### Added`, tagged `(markup/markup-sample)`: `AddFactory`, the controller list of `RegisterTo`, the XML state panel and records of HelloPanel with `HelloRow`, smoke line 8, the load error exit, and the twenty-third gate.
- `context.md` (this file): the header, the Quick Status, the phase 4 close record, the session note, and the six new lines under `## Needs Human Verification` (the phase 4 demo and the five new A10 windowed checks; the A10 "8 lines, exit 0" item holds in the phase 1 demo line, noted for the coordinator).

### Implementation Decisions (docs phase)

- **The overview.md reading (task 4.3, as the brief states).** The two `twenty-second gate` mentions at lines 23 and 55 name `TemplateBindingAllocationTests` of binding-attributes, and that number stays correct for that gate; no mention was relabeled. The roll reads twenty-third instead: the markup epic row and the v2.8 line now state that markup-sample adds `SampleMarkupAllocationTests`, the twenty-third allocation gate. `grep -n 'twenty-third allocation gate' docs/overview.md` gives two lines (23 and 55), so step 10 of the Verification Method passes.
- **The README status line (line 7) was not changed.** The brief names lines 5, 16, 28, 117, 151, and the `samples/` line only. Line 7 still reads "The XML markup loader and the debugger window are not written yet", "the suite runs 2921 tests", and "the input epic has two features left". Those three claims are stale (phase 4, `3203`, and the input epic close). The whole status quote is a rollup line and belongs to `/update-feature` and `/update-master`, so it stays for the orchestrator. The acceptance grep `grep -n 'planned' README.md` gives no line: the remaining `Planned` word at line 29 is the debugger row, uppercase, and not a markup line.
- **`docs/features/markup/human-testing.md` was not touched** (the edit limit of task 4.3). The content the plan wants in its `## markup-sample` section stands in `## Needs Human Verification` of this file; the coordinator copies it after the merge.
- **Section 11 demo shape.** The C# block adds `definitions.AddFactory("hud.live", liveFactory);` with the comment `// one name holds a text or a C# factory`. The XML demo text keeps its two template names, so the factory line shows the API, not a third record; the rows and the rules state the text-or-factory rule.

### Runs (task 4.4)

- Build: `0 Warning(s)`, `0 Error(s)`, exit 0 (`-nodeReuse:false`, `DOTNET_CLI_HOME=/tmp/dui-cli-home`, `MSBUILDDISABLENODEREUSE=1`).
- Tests: `total: 3203, failed: 0, succeeded: 3200, skipped: 3` — the phase 3 close totals.
- Package (the `pack`, `package-restore`, and `package` steps of `scripts/test-linux.sh`, by hand): pack wrote the five packages; `dotnet run --project tests/BrutalUi.PackageTest/... --no-restore` printed `package-test: PASS Gauge level 0.5`.
- README check (step 9): the `awk`+`diff` against `samples/HelloPanel/HelloState.xml` gives no difference, `same`. `grep -n 'planned' README.md` lists no markup line.
- Headless check 1 re-run (output copy of `HelloState.xml`, `hello.score` to `hello.scor`): one line, no GLFW line, exit code 1: `HelloState.xml(6,45): UnknownBinding: 'hello.scor' is not a bound key. ...`. Restored with `cp`; `cmp` equal. No `.orig` file under the output folder.
- Headless check 4 re-run (clean files): no `HelloState.xml(` or `HelloDefinitions.xml(` line, then `HelloPanel: GLFW init failed: PlatformUnavailable (Failed to detect any supported platform).`, exit code 1. The load passes before GLFW.

### Acceptance Checks (evidence for the orchestrator; boxes left open)

- README: the diff check gives `same`; `grep -n 'planned' README.md` gives no markup line. [box open]
- Section 11: `grep -n 'AddFactory' docs/technical-design.md` lists the C# block line and the four rule paragraphs; `grep -n 'The HelloPanel sample'` gives the paragraph at line 1783. Section 14: `grep -n 'twenty-third allocation gate'` gives the bullet at line 2105; the sample bullet lists 8 lines with `HelloPanel: markup: elements <E>, ...`. [box open]
- CHANGELOG: `grep -n 'markup/markup-sample' CHANGELOG.md` gives exactly one line (58). `human-testing.md` has no `## markup-sample` in this run by the edit limit; the content is in this file for the coordinator. [box open]
- Package: `package-test: PASS Gauge level 0.5`. [box open]

### Blast Radius

`git status --porcelain`: six modified files — `README.md`, `docs/technical-design.md`, `docs/overview.md`, `CHANGELOG.md`, `docs/features/markup/markup-sample/context.md`, and `docs/features/markup/markup-sample/tasks.md`. No source, sample, or test file changed; no git write ran; base `6eb10d9`.

### Demo State

The phase 4 demo passes on this host: `README.md` shows the `HelloState.xml` block equal to the file character for character, with the C# load code under it. The windowed checks stay pending on a host with a window: the eight-line smoke run with exit code 0, the `Add` click raising the score, the F1 and F2 checks, the task 1.9 plant with `bindings 0`, the line 2 counts, and both test-script PASS lines. They stand in `## Needs Human Verification`.

---

## API Integration

- `DefinitionLoader.AddFactory(string name, Func<PanelContext, Element> factory)` - new. A `template` value of a `Panel` or a `Layout` names a text or a factory, through one name table.
- `DefinitionLoadResult.RegisterTo(UiManager)` - now returns `IReadOnlyList<PanelController>` in the order of `Panels`.
- The sample uses `TemplateLoader`, `TemplateBindings`, the generated `RegisterHelloPanelElements()`, `DefinitionLoader`, and `UiManager`.

---

## State Management

- `HelloState.Score` (`Store<int>`) drives the score label of the XML state panel through `bind-text="hello.score"`.
- The commands `hello.add`, `hello.reset`, `hello.rebind`, and `hello.restore` live in `state.Map.Commands`. The `TemplateBindings` uses the same registry.

---

## Testing Approach

### Unit Tests
**Files:** `tests/BrutalUi.Framework.Tests/Templates/DefinitionFactoryTests.cs`
- The factory panel and the factory layout, the shared name table, the argument checks, and the controller list of `RegisterTo`. Plants 1 to 3.

### Integration Tests
**Files:** `tests/BrutalUi.Framework.Tests/Templates/SampleMarkupAllocationTests.cs`
- The sample shape at 0 bytes for an `Add` click and a mode toggle, and a real-work check. Plants 4 and 5.

### Manual Testing Checklist
- [ ] The smoke run prints 8 lines with exit code 0 on a host with a window.
- [x] The headless checks 1 to 4 of phase 2 give the expected lines and exit codes (this host, 2026-11-19; see the phase 2 close record).

---

## Needs Human Verification

`/autorun-feature` adds one line per item that the verifier cannot check, for a person to test.

- [ ] Phase 1 demo: the developer runs `HelloPanel --frames 120` on a host with a window and reads an eighth line `HelloPanel: markup: elements 6, bindings 1, commands 3, panels 3, layouts 1, modes 2.` Line 6 still shows `adds`, `resets`, and `versions` above 0, and the exit code is 0.
- [ ] Phase 2 demo: the developer changes `bind-text="hello.score"` to `bind-text="hello.scor"` in `samples/HelloPanel/bin/Debug/net10.0/HelloState.xml`, runs `HelloPanel --frames 120`, and reads one line that starts with `HelloState.xml(` and holds `): UnknownBinding: 'hello.scor' is not a bound key.` on standard error, no GLFW line, and exit code 1.
- [ ] Phase 2 windowed half of check 4: on a host with a window, the clean run gives 8 lines and exit code 0.
- [ ] Phase 3 demo: the developer runs `BrutalUi.Framework.Tests --filter-class '*SampleMarkupAllocationTests'` and reads a total of 3 with `failed: 0`. `AnAddClickOnTheLoadedStatePanelAllocatesNothing` measures 0 bytes over 100 click frames on the panel that `HelloState.xml` builds.
- [ ] Phase 4 demo: the reader opens `README.md` at `### The same tree in XML` and sees the XML block of `samples/HelloPanel/HelloState.xml`, equal to the file character for character, with the C# load code under it.
- [ ] markup-sample (A10): the XML state panel shows in `hello.main` with the `Add`, `Rebind (Up)`, and `Restore` buttons and the score label.
- [ ] markup-sample (A10): a click on `Add` raises the score.
- [ ] markup-sample (A10): F1 and the `Mode (F1)` button toggle `hello.main` and `hello.detail`, and F2 steps back.
- [ ] markup-sample (A10): the plant of task 1.9 — remove `bind-text="hello.score"` from the output copy of `HelloState.xml` under `bin/Debug/net10.0/`, run `HelloPanel --frames 120`, and read line 8 with `bindings 0`, the line `HelloPanel: the markup panel did not load.` on standard error, and exit code 1. Restore the file and check with `cmp`.
- [ ] markup-sample (A10): line 2 keeps the draw-command and vertex counts of the last measure: the XML state panel builds the same tree as the old C# row.
- [ ] markup-sample (A10): `scripts/test-linux.sh` and `scripts/test-windows.ps1` print their PASS line.

Note for the coordinator: the A10 item "the smoke run prints 8 lines with exit code 0" needs no extra line here. The Phase 1 demo line above states it in full: the run reads the eighth line with the counts and exits with code 0. `docs/features/markup/human-testing.md` stays untouched in this run (the phase 4 brief edit limit); copy this section there after the merge.

---

## Performance Considerations

- Load-time code can allocate (A10). `Update` and `Draw` on the loaded sample shape must allocate 0 bytes after warm-up. The smoke line 3 and the twenty-third gate check this.

---

## Next Steps

### Immediate (Today/This Session)
1. ✅ Phase 1, group 1: tasks 1.1 to 1.6. Closed 2026-11-19.
2. ✅ Phase 1, group 2: tasks 1.7 to 1.9. Closed 2026-11-19.
3. ✅ Phase 2: tasks 2.1 to 2.5. Closed 2026-11-19. See the phase 2 close record.
4. ✅ Phase 3: tasks 3.1 to 3.4. Closed 2026-11-19. See the phase 3 close record.
5. ✅ Phase 4: tasks 4.1 to 4.4. Closed 2026-11-19. See the phase 4 close record.

### Short Term (This Week)
1. The phase 4 verifier and the acceptance boxes; `/update-feature markup/markup-sample`.

### Future (After This Phase)
1. `/update-epic markup` closes the epic and Phase 3 (Notes item 11 of `implementation.md`).

---

## Open Questions

- (none)

---

## Session Notes

### 2026-11-19 08:28
- `/plan-feature markup/markup-sample` ran in an Orca plan worker. The interview took two rounds. The approach is "hook first, then sample".
- The solution-architect wrote `implementation.md` with 4 phases and 22 tasks, about 205 steps. Q5 went into the plan after round 2.
- `/start-feature` wrote `tasks.md`, `context.md`, and `inbox.md`. The inbox has no item.
- The next session starts at task 1.1.

### 2026-11-19 08:55 (Phase 1, group 1 close)
- The ADVANCED dev agent ran tasks 1.1 to 1.6. Base commit `7948a78`. No git write ran.
- `AddFactory` joined the name tables, the resolve pass resolves a factory template as is, a factory layout gets no check load, and `RegisterTo` returns the panel controllers.
- `DefinitionFactoryTests` added 5 tests. Framework grew 384 to 389; the solution grew 3195 to 3200, with 3 skipped and 0 failed. The build shows 0 warnings and 0 errors.
- Plants 1 to 3 each gave exit code 2 with 0 `error CS` lines, failed exactly the named tests, and restored equal under `cmp`. The table stands in the group close record.
- `HelloRow.cs` compiles in the sample with no generator line and no caller (Notes item 8).
- Smoke line 8 is not due in group 1: the sample behavior did not change. The Phase 1 demo still needs a host with a window (Notes item 6).
- No existing test changed. No open question.

### 2026-11-19 09:08 (Phase 1, group 2 close)
- The ADVANCED dev agent ran tasks 1.7 to 1.9 on base `7948a78` with the group 1 working tree. No git write ran.
- `HelloPanel` now loads the six records from `HelloDefinitions.xml` and the state panel from `HelloState.xml`. `HelloModes.Mount` runs the A4 flow inline, registers with `RegisterTo`, applies the build rule through the returned controllers, finds the `Add` button by name, and returns `HelloMount` with the mounted trees and `MarkupCounts`.
- Smoke line 8 prints the A2 form. The markup check writes `HelloPanel: the markup panel did not load.` with exit code 1 on a 0 count.
- The build shows 0 warnings and 0 errors. The tests give total 3200, failed 0, skipped 3; Framework 389. No change in this group, as expected.
- The output copies of both XML files equal the sources under `cmp`. `grep -rn 'RegisterHelloPanelElements' samples/HelloPanel/*.cs` gives the one comment line and the one real call, and the build compiles the call.
- The smoke run on this host gives the GLFW line and exit code 1 before any smoke line: smoke pending a host with a window. The task 1.9 plant is pending with it; the output files stay clean.
- No existing test changed. No open question.

### 2026-11-19 09:35 (Phase 1 acceptance close, send-back round)
- The coordinator read `tasks.md` and found the six Phase 1 acceptance items open. Each item was re-checked on this host, by the orchestrator, with a fresh run.
- Item 1: `dotnet build brutal-ui.sln` gives `0 Warning(s)`, `0 Error(s)`. `dotnet test --solution` gives total 3200, failed 0, skipped 3. `*DefinitionFactoryTests` gives total 5, failed 0. `git diff --stat 7948a78 -- tests` gives no output, so no existing test was edited.
- Item 2: `git diff 7948a78 -- src/BrutalUi.Markup src/BrutalUi.Core src/BrutalUi.Framework/Panels` gives no output.
- Item 3: the plant logs `p1.log`, `p2.log`, and `p3.log` under `src/BrutalUi.Framework/obj/plantscratch/` each hold 0 `error CS` lines and failed counts 1, 2, and 1 with the named tests (exit code 2); `cmp` on each source against its `.orig` copy gives equal.
- Item 4: `cmp` on both XML files against `samples/HelloPanel/bin/Debug/net10.0/` gives equal.
- Item 5: `grep -rn 'RegisterHelloPanelElements' samples/HelloPanel/*.cs` gives the one real call at `HelloModes.cs:220` plus the remarks line; the fresh build compiles it.
- Item 6: unchanged, `context.md` records the smoke as pending a host with a window; the windowed check stays open in `## Needs Human Verification`.
- The six acceptance boxes in `tasks.md` are now `[x]`. No file of the build changed in this round; no git write ran.

### 2026-11-19 09:50 (Phase 2 close)
- The ADVANCED dev agent ran tasks 2.1 to 2.5 on base `949fda33`. No git write ran.
- `HelloMarkup.Load` now owns the two file reads, the A4 flow with the Decision 7 check load, the error print (no prefix), and the name constants. `HelloModes.Mount(definitions, checker)` starts at `RegisterTo` and checks its argument. `Program.Run` builds the six managed objects and loads the markup before `Glfw.Init`; a `null` result exits 1 with no GLFW call. `RunImGui` takes the texture service from `renderer.Textures`.
- The `UnknownTemplate` message names `AddTemplate` and `AddFactory` from one constant; the two `DefinitionErrorTests.cs` strings changed and nothing else (4 diff lines against `949fda33`).
- The build shows 0 warnings and 0 errors. The tests give total 3200, failed 0, skipped 3; Framework 389. No total change, as expected.
- Headless checks 1 to 4 passed on the output folder with the expected lines and exit codes; the check 3 order is the `HelloState.xml(` line before the `HelloDefinitions.xml(` line (sorted by file); each restored file equals its source under `cmp`. Check 4 shows the GLFW line only: the load passed before GLFW on this no-window host.
- The phase 2 demo (check 1) holds here. The clean eight-line run and the phase 1 demo stay pending on a host with a window.
- No open question. No inbox message reached this phase.

### 2026-11-19 10:00 (Phase 2 verifier close)
- The verifier (brief `phase-2-verify.md`) returned PASS with zero fix rounds. Gates: build 0 warnings, 0 errors; suite total 3200, failed 0, skipped 3; `*DefinitionErrorTests` 12 and `*DefinitionFactoryTests` 5 passed; the load call at `Program.cs:422` precedes `Glfw.Init` at `:428`; the `DefinitionErrorTests.cs` diff against `949fda33` is exactly four lines; the blast radius is the four planned files plus the new `HelloMarkup.cs` (120 lines).
- Demo: PASS. Checks 1 to 4 reproduced on the output copies; each file restored equal under `cmp`. The windowed half of check 4 (8 lines, exit 0) stays pending on a host with a window.
- The four Phase 2 acceptance boxes in `tasks.md` are now `[x]`. The demo line is added to `## Needs Human Verification` here only; no line was written to `human-testing.md` (the coordinator copies them after the merge). No inbox item; no git write ran.

### 2026-11-19 10:25 (Phase 3, group 1 close)
- The STANDARD dev agent ran tasks 3.1 to 3.4 on the phase 2 working tree. No git write ran.
- The C# state row is gone: `HelloState.Row`, its three buttons, `RowClass`, and `s_scoreText` left `HelloState.cs`, and `HelloScreen.BuildStatePanel` left `HelloScreen.cs`. `HelloState.xml` is the only state row (A9). The remarks carry the new owners.
- `BrutalUi.Framework.Tests` now links `HelloRow.cs`, the generator analyzer line, and the three `PreserveNewest` copies (the two XML files and `HelloScreen.css`). No `HelloPanel.csproj` reference (Decision 10).
- `SampleMarkupAllocationTests` (the twenty-third gate) added 3 tests. Framework grew 389 to 392; the solution grew 3200 to 3203, with 3 skipped and 0 failed. The build shows 0 warnings and 0 errors. The demo passes on this host: the filter-class run gives total 3, failed 0.
- Plants 4 and 5 each gave exit code 2 with 0 `error CS` lines, failed exactly the named test plus the gates on the same path, and restored equal under `cmp` (table in the close record). The re-runs after each restore give 0 failed.
- Headless check 4 of phase 2 still gives no markup line, then the GLFW line, exit code 1. The smoke run and the eight-line demo stay pending on a host with a window.
- No existing test changed. No open question. No inbox message reached this phase.

### 2026-11-19 10:35 (Phase 3 verifier close)
- The verifier (brief `phase-3-verify.md`) returned FAIL on the first pass: gate step 10, `SampleMarkupAllocationTests.cs` was 472 lines, over the 400-line cap. Steps 1 to 9 and 11 passed; Demo: PASS. The fault became one open `bug` line in `inbox.md`.
- Fix round 1 (brief `phase-3-fix1.md`, STANDARD dev agent): partial-class split into `SampleMarkupAllocationTests.cs` (185 lines) and `SampleMarkupAllocationScene.cs` (297 lines). Test names, bodies, semantics, and totals unchanged; no other file touched.
- The verifier re-run returned PASS on all 11 gate steps. Build 0 warnings, 0 errors; suite total 3203, failed 0, skipped 3; class run total 3, failed 0; plants 4 and 5 evidence holds in the close record and the `p4.log`/`p5.log` logs. Demo: PASS (0 bytes over 100 click frames).
- The `bug` line is marked `done: phase-3-fix1.md`. The Phase 3 demo line is added to `## Needs Human Verification` here only; no line was written to `human-testing.md` (the coordinator copies them after the merge). No inbox item reached the phase beyond the verifier fault; no git write ran.

### 2026-11-19 10:55 (Phase 4 close)
- The STANDARD dev agent ran tasks 4.1 to 4.4 on base `6eb10d9`. No git write ran; no file outside the six docs changed.
- `README.md` now shows the sample template and the load flow: the XML block equals `HelloState.xml` character for character (step 9 gives `same`), the `planned` loader lines are current, and the A7 note stands. `technical-design.md` section 11 holds `AddFactory` in the C# block, the text-or-factory rows, the factory-layout rules, the template-factory limit (Notes item 12), the named `RegisterTo` return, and **The HelloPanel sample.** with both files and line 8. Section 14 holds **A twenty-third allocation gate.** with plants 4 and 5 and the 8-line sample bullet with the markup check message and the load error exit. `docs/overview.md` names the twenty-third gate in the epic roll and the v2.8 line; `CHANGELOG.md` has one `(markup/markup-sample)` line.
- Runs: build `0 Warning(s)`, `0 Error(s)`; tests `total: 3203, failed: 0, skipped: 3` (the phase 3 totals); `package-test: PASS Gauge level 0.5`; headless checks 1 and 4 re-run with the expected lines and exit code 1; the output XML restored equal under `cmp`.
- The windowed checks and the task 1.9 plant stay pending on a host with a window; the six human-test lines stand in `## Needs Human Verification` for the coordinator to copy into `human-testing.md` after the merge. `human-testing.md` and `epic-overview.md` were not touched.
- Open note for the orchestrator: README line 7 (the status quote) keeps three stale claims (the loader "not written yet", "2921 tests", "the input epic has two features left"); the brief did not name the line, so it waits for `/update-feature` or `/update-master`.
- No inbox item reached this phase; no open question.

### 2026-11-19 11:12 (Phase 4 verifier close)
- The verifier (brief `phase-4-verify.md`, STANDARD agent) returned PASS on the first pass, with zero fix rounds. All 18 gate steps ran and passed: build `0 Warning(s)`, `0 Error(s)`; suite `total: 3203, failed: 0, skipped: 3` (the phase 3 totals, no test growth); `*SampleMarkupAllocationTests` total 3, failed 0; the README XML block `same` against `samples/HelloPanel/HelloState.xml`; no `planned` markup line (the uppercase `Planned` at line 29 is the debugger row); section 11 holds `AddFactory`, the text-or-factory rows, the factory-layout rule, the template-factory limit, the named `RegisterTo` return, and **The HelloPanel sample.**; section 14 holds one **A twenty-third allocation gate.** bullet and the 8-line sample bullet with the markup check message and the load-error exit; `docs/overview.md` names the twenty-third gate on two lines and keeps both correct `twenty-second` labels; `CHANGELOG.md` has exactly one `(markup/markup-sample)` line naming all six items; both section 11 XML copies are equal to the files; the `UnknownTemplate` quote is verbatim equal to `DefinitionReader.Resolve.cs`; the added doc prose holds no contraction; `package-test: PASS Gauge level 0.5`; headless checks 1 and 4 re-run with the expected lines and exit code 1, and the output XML restored equal under `cmp`. Demo: PASS. UI check: none.
- The verifier also confirmed the two follow-up questions that reached its tab: the README `csharp` block matches the flow of `HelloMarkup.Load` in the abridged form that task 4.1 names (the check-load call is not in that list), and the `UnknownTemplate` message holds character for character. Those two questions arrived as the `Inbox:` line of the verifier result; they got a short answer and no inbox line (section 12 rule 3).
- Part 2 (record): the four Phase 4 acceptance boxes and the four task boxes read `[x]`; `tasks.md` now shows 22/22 tasks complete. Part 3 (called mode, with the run change): the Phase 4 demo line stands in `## Needs Human Verification` of this file only; `docs/features/markup/human-testing.md` holds no line from this run. Part 5 (read the inbox): no open line; the single `bug` line was closed at the phase 3 fix. Question `msg_e893e050f051` to the coordinator (who owns the `## markup-sample` section of `human-testing.md`) had no answer at the close; this run followed the phase spec.
- Blast radius: six modified files — `README.md`, `docs/technical-design.md`, `docs/overview.md`, `CHANGELOG.md`, and the feature `context.md` and `tasks.md`. No source, sample, test, script, package-pin, or epic-rollup change; no git write ran.

### Review Fix Group 1 Close Record — 2026-11-19 11:45 (CR-1, CR-2, CR-6)
- The ADVANCED dev agent fixed CR-1 (MEDIUM), CR-2 (LOW), and CR-6 (LOW) of `code-review.md` on the phase 4 working tree, per the brief `review-fix-g1.md`. No git write ran. Files changed: `src/BrutalUi.Framework/Templates/DefinitionReader.Resolve.cs`, `samples/HelloPanel/HelloModes.cs`, `samples/HelloPanel/HelloMarkup.cs`, `samples/HelloPanel/Program.cs`. `DefinitionFactoryTests.cs` needed no change: no pinned behaviour changed.
- **CR-1, the choice and the reason.** The register step moved out of `HelloModes.Mount` into the new `HelloModes.Register(DefinitionLoadResult)`, and `HelloMarkup.Load` calls it before the window, after both loads pass. Reason: Decision 3 holds that the resolve pass calls no factory and checks no slot of a factory layout, and `UiManager.Register(ModeDefinition)` checks the slots inside `RegisterTo`. The sample owns when it calls `RegisterTo`, so an early call closes the A3 gap with no design-rule flip and no Framework behaviour change. Registration needs no GPU: `UiManager.Register(PanelDefinition)` defers each root build (Decision 5 holds — the panel factories run only inside `Mount`, after the checker texture exists), and `UiManager.Register(LayoutDefinition)` calls the layout factory, which is `HelloScreen.BuildLayout`: two `SlotContainer` objects and no texture. `HelloMarkup.Load` catches the register `ArgumentException`, prints one line `HelloDefinitions.xml: <message>` to standard error, and returns `null`, so a slot typo gives exit code 1 before the first GLFW call. Known limit, kept by Decision 3: the line names no file position and no slot name.
- **CR-2.** `DefinitionReader.Resolve.cs`: one helper `TryResolveTemplate(DefinitionValue, out byte[]?, out Func<PanelContext, Element>?)` now serves the panel loop (step 2) and the layout loop (step 3): the text first, then the factory, else the `UnknownTemplate` error at the value position (Decision 1). The two duplicated blocks and the two empty-body branches are gone. Behaviour, error order, and the follow-on rule stand; the resolve tests pass unchanged.
- **CR-6.** `HelloModes`: `Register` runs one time (flag `_registered`; a second call throws `InvalidOperationException("Register runs one time. A second call registers each record twice.")`). `Mount(TextureHandle)` runs one time (flag `_mounted`; a second call throws `InvalidOperationException("Mount runs one time. A second call builds each panel root again.")`) and needs one succeeded `Register` before it. The one-time rule and the `Main`/`Detail` null state before `Mount` stand in the XML docs of `Register`, `Mount`, and the class remarks.
- Sample-only signature split: `Mount(DefinitionLoadResult, TextureHandle)` became `Register(DefinitionLoadResult)` + `Mount(TextureHandle)`; `Program.RunWindow` and `Program.RunImGui` no longer thread the result. The remarks of `HelloModes`, `HelloMarkup`, and `Program` carry the new order. Shipped docs to update by the coordinator (not edited, per the brief): `README.md` and `docs/technical-design.md` section 11 say the sample loads the two files before `Glfw.Init`; the register step now also runs there, and one sentence should say so.
- Runs: build `0 Warning(s)`, `0 Error(s)`. Full suite: total 3203, failed 0, skipped 3 — the baseline totals, no test added or changed. Per class: `*DefinitionFactoryTests` total 5, failed 0; `*DefinitionErrorTests` total 12, failed 0.
- Headless checks on the output copies under `samples/HelloPanel/bin/Debug/net10.0/` (each file restored with `cp` and equal under `cmp`; no copy left): the new CR-1 slot typo (`bottom` to `botom` in `hello.main`) gives one line, `HelloDefinitions.xml: A mode names a slot that its layout holds not. (Parameter 'mode')`, exit code 1, with no GLFW line and no line on standard output. Check 1 (`hello.score` to `hello.scor`) still gives the one `HelloState.xml(6,45): UnknownBinding: ...` line, exit code 1. The clean run still stops at `HelloPanel: GLFW init failed: PlatformUnavailable (Failed to detect any supported platform).`, exit code 1, after the loads and the register step passed.
- CR-6 guard demo: a temporary plant in `HelloMarkup.Load` (build, run, revert) made the second `Mount` print `Mount runs one time. A second call builds each panel root again.` and the second `Register` print `Register runs one time. A second call registers each record twice.`; both lines landed before the GLFW line. The plant left no residue in the source or the binary.
- The windowed smoke run and the eight-line demo stay pending on a host with a window.

### Review Fix Group 2 Close Record — 2026-11-19 11:55 (CR-5, CR-4, CR-3)
- The dev agent fixed CR-5 (LOW), CR-4 (LOW), and CR-3 (LOW, Medium effort) of `code-review.md`, per the brief `review-fix-g2.md`, on the group 1 working tree. No git write ran. Files changed: `tests/BrutalUi.Framework.Tests/Templates/DefinitionFactoryTests.cs`, `samples/HelloPanel/Program.cs` (one comment sentence), `samples/HelloPanel/HelloMarkupNames.cs` (new), `samples/HelloPanel/HelloMarkup.cs`, `tests/BrutalUi.Framework.Tests/BrutalUi.Framework.Tests.csproj`, `tests/BrutalUi.Framework.Tests/Templates/SampleMarkupAllocationScene.cs`.
- **CR-5.** `AFactoryPanelAndAFactoryLayoutLoadAndMount` now pins both records: `Assert.Same(layoutFactory, result.Layouts[0].CreateRoot);` joins the panel pin, so a wrapper plant on the layout branch of step 3 fails. The class keeps five tests.
- **CR-4.** The managed-build comment in `Program.Run` no longer names a build-time C# state row. The sentence now reads: the state builds the store, the commands, and the action map one time, at build time, and HelloState.xml builds the state row inside HelloModes.Mount. `grep -n 'state row one'` gives no match. The line 8 markup print is unchanged.
- **CR-3.** New `HelloMarkupNames.cs` (62 lines) holds the shared names once: the two file names, the state template name, the three factory names, the action map name, the score binding key, and the four command keys as `ReadOnlySpan<byte>` properties. `HelloMarkup.cs` dropped its own consts and calls the type. The test csproj links the file with `LinkBase="Shared"`, beside the `HelloRow.cs` link, and still holds no reference to `HelloPanel.csproj`. `BuildScene` reads the constants; no factory, template, definitions-file, map, or score-key literal stands in the scene. The scene keeps its own twins (the floor fix, not the full `HelloMarkup.Configure` move). The linked file compiles in both assemblies: it uses only `System` types.
- Runs: build `0 Warning(s)`, `0 Error(s)`. Full suite: total 3203, failed 0, skipped 3 — the baseline totals; CR-5 added one assert inside an existing test. Per class: `*DefinitionFactoryTests` total 5, failed 0; `*SampleMarkupAllocationTests` total 3, failed 0, and both allocation bodies (`AnAddClickOnTheLoadedStatePanelAllocatesNothing`, `AModeToggleOnTheLoadedSampleRecordsAllocatesNothing`) still measure 0 bytes.
- Demo: the headless sample run passed the loads with the shared names and stopped at `HelloPanel: GLFW init failed: PlatformUnavailable (Failed to detect any supported platform).` (exit 1), the window rule of this host. The windowed smoke run and the eight-line demo stay pending on a host with a window.

---

*Update this file at the end of each work session. Run `/update-feature` before compacting conversations.*
