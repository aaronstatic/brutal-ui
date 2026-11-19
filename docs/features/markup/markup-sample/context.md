# markup/markup-sample - Context & Decisions

**Epic:** markup (feature 6 of 6)
**Last Updated:** 2026-11-19 09:35
**Current Phase:** Phase 2 - The Load Error Exit Before the Window
**Status:** 🟡 In Progress

---

## Quick Status

**What's Done:**
- ✅ The plan exists: `implementation.md`, with 4 phases and 22 tasks. The interview confirmed Q1 to Q5 and A1 to A10 over two rounds.
- ✅ The dev docs exist: `tasks.md`, `context.md`, and `inbox.md`.
- ✅ Phase 1, group 1 (tasks 1.1 to 1.6): `AddFactory` with the shared name table, the factory resolve rule, the `RegisterTo` controller list, `DefinitionFactoryTests` (5 tests, Framework 389), `HelloRow.cs`, and plants 1 to 3. See the group close record below.
- ✅ Phase 1, group 2 (tasks 1.7 to 1.9): `HelloState.xml`, `HelloDefinitions.xml`, the csproj analyzer and copy lines, `HelloModes.Mount` with `HelloMount` and `MarkupCounts`, and smoke line 8 with its markup check. Totals unchanged. See the group 2 close record below.

**What's Next:**
- 📋 Phase 2 (tasks 2.1 to 2.5): `HelloMarkup.Load`, the mount split, the load error exit before the window, the `UnknownTemplate` message, and the four headless checks.

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
- [ ] The headless checks 1 to 4 of phase 2 give the expected lines and exit codes.

---

## Needs Human Verification

`/autorun-feature` adds one line per item that the verifier cannot check, for a person to test.

- [ ] Phase 1 demo: the developer runs `HelloPanel --frames 120` on a host with a window and reads an eighth line `HelloPanel: markup: elements 6, bindings 1, commands 3, panels 3, layouts 1, modes 2.` Line 6 still shows `adds`, `resets`, and `versions` above 0, and the exit code is 0.

---

## Performance Considerations

- Load-time code can allocate (A10). `Update` and `Draw` on the loaded sample shape must allocate 0 bytes after warm-up. The smoke line 3 and the twenty-third gate check this.

---

## Next Steps

### Immediate (Today/This Session)
1. ✅ Phase 1, group 1: tasks 1.1 to 1.6. Closed 2026-11-19.
2. ✅ Phase 1, group 2: tasks 1.7 to 1.9. Closed 2026-11-19.

### Short Term (This Week)
1. Phase 2: the load error exit before the window.
2. Phase 3: the twenty-third gate and the removal of the C# state row.
3. Phase 4: the docs.

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

---

*Update this file at the end of each work session. Run `/update-feature` before compacting conversations.*
