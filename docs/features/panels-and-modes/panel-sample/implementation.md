# Panel Sample - Implementation Plan

**Epic:** panels-and-modes (feature 4 of 4, the last feature)
**Status:** Ready for Review
**Started:** 2026-11-08
**Target Completion:** TBD
**Last Updated:** 2026-11-08 11:42

---

## Quality Bar

**Target quality:** A sample and an allocation gate that close the panels-and-modes epic. The sample is the reference use of the mode model. A game developer copies it to start a screen with modes. The window must show the switch, and the smoke run must prove the switch under the 0-byte rule on the GPU. The gate must be a true 0-byte proof with a real-work check, so an empty body cannot pass.

- **Reference use:** `HelloModes.cs` holds the full mode model in one place: the panel context, the manager, the three panels, the layout, the two modes, the two commands, and the two keys. A reader finds each `Register` call in the order that the manager needs (the panels and the layout before the modes). Each name is a UTF-8 literal. Each handler is a static field with a target, so a command call allocates nothing.
- **Visible switch:** F1 removes the state row and shows the info panel. F2 brings the state row back. The `Mode (F1)` button does the same as F1. The widget panel stays in its slot with no join, so its hover, its focus, and its text field keep their state.
- **Smoke proof:** The smoke run switches the mode in each frame after warm-up. Each measured frame runs one diff. Line 3 keeps 0 bytes, and the new seventh line shows the switch count, the back count, and the frames in each mode. A count of 0 gives exit code 1.
- **True 0-byte gate:** `ModeDiffAllocationTests` measures a HelloPanel shape with real widgets (a `Button` with a command binding, a `Label` with a store binding, and a focused `TextField`), a toggle cycle, `Update`, and `Draw`. The measured body holds no lambda, no string, no boxed value, no array, and no `Assert`. The real-work test reads the tree after one step. Plants prove each test.
- **No library change:** No file of `src/` changes in the end state. A plant edits a `src/` file for one run only, and `cmp` checks the restore.

---

## Executive Summary

This feature changes the HelloPanel sample, adds one test class, and updates three docs.

- `HelloScreen.Build` splits into four build methods: `BuildWidgetPanel`, `BuildStatePanel`, `BuildInfoPanel`, and `BuildLayout`. The layout root holds two `SlotContainer` children, `top` and `bottom`.
- The new `samples/HelloPanel/HelloModes.cs` owns the `PanelContext`, the `UiManager`, the three panel registers (`hello.widgets`, `hello.state`, `hello.info`), the layout register (`hello.layout`), the two modes (`hello.main`, `hello.detail`), the two commands (`hello.mode`, `hello.back`), the two actions on `HelloState.Map`, the F1 and F2 keys, and the `Mode (F1)` button.
- `Program.cs` creates `HelloModes` and calls `SetMode("hello.main"u8)` in place of `ui.AddRoot(...)`. The smoke run presses F1 and F2 in turn, prints a seventh line, and exits with code 1 when a mode count is 0.
- `HelloScreen.css` gets the class `hello-slot-root` for the layout root, a type rule for `SlotContainer`, and the class rule `.hello-panel-root` for each panel root.
- `tests/BrutalUi.Framework.Tests/Panels/ModeDiffAllocationTests.cs` is the eighteenth allocation gate, with 3 tests.
- `docs/overview.md`, section 14 of `docs/technical-design.md`, and `CHANGELOG.md` record the result.

The plan has 3 phases and 25 tasks. Phase 1 (9 tasks) delivers the two modes in the window. Phase 2 (8 tasks) delivers the smoke run with the seventh line. Phase 3 (8 tasks in two groups) delivers the gate, the plants, the docs, and the full gate. Each phase routes to the STANDARD dev agent (sonnet). Phase 3 needs the most care.

---

## User Personas Alignment

No `docs/user-personas.md` file exists. The personas come from `docs/mission-statement.md` and the epic docs, as in the sibling plans.

**Target Users:**
- [x] **The game developer** (reads `HelloModes.cs` as the reference use of `UiManager`, copies the register order, and binds a mode switch to a button and a key)
- [x] **The maintainer** (runs `ModeDiffAllocationTests` and the smoke run through `scripts/test-linux.sh` with no script change, and reads the seventh line)
- [x] **The xml-markup epic** (reads the same `PanelDefinition`, `LayoutDefinition`, and `ModeDefinition` records from XML later. The factory shape `Func<PanelContext, Element>` of the sample is the shape that the loader fills)

**User Subsets Served:**
- [x] **Simple** - one layout, two slots, two modes, one shared action map, and a toggle command.
- [x] **Complex** - a panel that stays with no join, a panel that leaves and keeps its binding, a panel that enters, and a back step through `ModeHistory`.

**UX Approach:**
- Build time: `HelloModes modes = new(ui, state, checker);` then `modes.Manager.SetMode(HelloModes.MainName);`.
- Frame time: no new line in the frame loop. A key or a click runs a command. The command calls `SetMode` or `History.Back()`. The next `Update` applies the mode.

---

## Goals

### Primary Goals
1. Show two modes in HelloPanel with one layout, two slots, and three panels (A8 of the epic, Q1, A2).
2. Switch the mode with a button and the F1 key through `hello.mode`, and step back with the F2 key through `hello.back` (Q4, Q5, A3).
3. Switch the mode in each frame of the smoke run after warm-up, print the seventh line, and exit with code 1 when a count is 0 (Q2, A4).
4. Add `ModeDiffAllocationTests`, the eighteenth gate, with the toggle cycle, the idle frame, and the real-work check (A7 of the epic, Q3, A6).
5. Style each `SlotContainer` and each panel root by class (A5).
6. Record the smoke contract, the gate, and the changes in the docs (A7).

### Success Criteria
- [ ] The phase 1 demo passes: a person presses F1 and sees the state row leave and the info panel enter. F2 brings the state row back.
- [ ] The phase 2 demo passes: the smoke run with `--frames 120` prints seven lines, line 7 reads `HelloPanel: modes: switches 60, backs 59, main frames 59, detail frames 58.`, line 3 reads `Update and Draw 0`, and the exit code is 0.
- [ ] The phase 3 demo passes: `ModeDiffAllocationTests` reads a total of 3 with `failed: 0`, and `scripts/test-linux.sh` prints its PASS line (or the split evidence of A8).
- [ ] The solution reads 2824 tests (3 skipped), 0 failed. `BrutalUi.Framework.Tests` reads 225.
- [ ] No file of `src/` differs from the snapshot of task 1.1.

---

## Architecture Overview

### The Place in the Epic

panel-definition gives the records. slot-container gives `SlotContainer`. mode-set gives `UiManager`, `ModeHistory`, and the `OnBeforeUpdate` slot. This feature uses each of them in HelloPanel, with no library change, and adds the epic gate. The xml-markup epic comes next. It reads the same records from XML.

```
Build time (allocates)
  new HelloModes(ui, state, checker)
    register hello.mode and hello.back in state.Map.Commands (static handlers, target = HelloModes)
    state.Map: AddAction hello.mode and hello.back, BindKey F1 and F2
    ModeButton = new Button("Mode (F1)").BindCommand(commands, "hello.mode"u8)
    Panels = new PanelContext { Commands = state.Map.Commands, Events, Services }
    Manager = new UiManager(...)                  ──► ui.OnBeforeUpdate = the diff handler
    Register(hello.widgets), Register(hello.state), Register(hello.info)  ──► 3 controllers
    controller.EnsureRoot() for each panel         ──► each root built at build time
    Register(hello.layout)                         ──► BuildLayout: root + SlotContainer top + bottom
    Register(hello.main), Register(hello.detail)   ──► both name state.Map
  modes.Manager.SetMode("hello.main"u8)
Frame 0: ui.Update ──► OnBeforeUpdate: AddRoot(layout root), mount widgets in top, state in bottom, Activate(map)
Frame N: F1 key ──► input pass ──► map ──► hello.mode ──► Manager.SetMode(the other mode). No tree change.
Frame N+1: ui.Update ──► OnBeforeUpdate: the diff (state leaves, info enters, widgets stays), Activate(same map)
           ──► input pass (the hit test reads the layout of frame N) ──► style ──► layout ──► Draw
Frame M: F2 key ──► hello.back ──► History.Back() ──► target = the mode before ──► applies in frame M+1
```

### File Tree (end state)

```
samples/HelloPanel/
├── HelloModes.cs          # phase 1 (new): the mode model of the sample
├── HelloScreen.cs         # phase 1: Build splits into four build methods
├── HelloScreen.css        # phase 1: .hello-slot-root, SlotContainer, .hello-panel-root
├── HelloState.cs          # phase 1: doc comments only (the map rule)
└── Program.cs             # phase 1 (wiring), phase 2 (smoke run, line 7, exit rule)
tests/BrutalUi.Framework.Tests/Panels/
└── ModeDiffAllocationTests.cs   # phase 3 (new): 3 tests, the eighteenth gate
docs/overview.md                 # phase 2: the smoke contract line for the mode counts
docs/technical-design.md         # phase 3: section 14
CHANGELOG.md                     # phase 3: Added and Changed
docs/features/panels-and-modes/panel-sample/context.md   # each phase
```

These files do not change: each file of `src/`, each `.csproj`, `Directory.Build.props`, `Directory.Packages.props`, `brutal-ui.sln`, `scripts/`, `HelloWidgets.cs`, `SampledTexture.cs`, `VulkanContext.cs`, and each existing test file. The new test file compiles with no project edit.

### The Public Shape of `HelloModes`

```csharp
namespace HelloPanel;

internal sealed class HelloModes
{
    public HelloModes(UiContext context, HelloState state, TextureHandle checker);

    public static ReadOnlySpan<byte> MainName => "hello.main"u8;
    public static ReadOnlySpan<byte> DetailName => "hello.detail"u8;

    public UiManager Manager { get; }
    public ModeDefinition Main { get; }
    public ModeDefinition Detail { get; }
    public Element WidgetPanel { get; }      // the smoke run aims its pointer move at it
    public Label GateLabel { get; }
    public HelloWidgets Widgets { get; }
    public Button ModeButton { get; }        // a click runs hello.mode
    public int Switches { get; private set; } // the runs of hello.mode
    public int Backs { get; private set; }    // the runs of hello.back where Back() returned true
}
```

Private members: the static handlers `s_mode` and `s_back` (`CommandHandler`, target `HelloModes`), the four factory methods `CreateWidgetPanel`, `CreateStatePanel`, `CreateInfoPanel`, and `CreateLayout` (each `Element Name(PanelContext context)`), and the fields `_state`, `_checker`, `_gateLabel`, and `_widgets`.

The handlers:

```csharp
s_mode: modes = (HelloModes)target!;
        ModeDefinition? from = modes.Manager.TargetMode ?? modes.Manager.CurrentMode;
        modes.Manager.SetMode(ReferenceEquals(from, modes.Detail) ? MainName : DetailName);
        modes.Switches++;
s_back: modes = (HelloModes)target!;
        if (modes.Manager.History.Back()) { modes.Backs++; }
```

### The Tree (end state)

```
layout root (hello-slot-root, IsPickable = false)
├── SlotContainer "top" (IsPickable = false)
│   └── widget panel (hello-panel, hello-panel-root, focusable)   in hello.main and hello.detail
│       ├── greeting Label, gate Label, checker Image, widget section
│       └── mode row (hello-row): Button "Mode (F1)"
└── SlotContainer "bottom" (IsPickable = false)
    ├── state panel (hello-panel, hello-panel-root): HelloState.Row      in hello.main only
    └── info panel (hello-panel, hello-panel-root): Label "Detail mode. Press F2 to go back."   in hello.detail only
```

### Integration Points

| Point | Owner | Use in this feature | Rule that binds it |
|-------|-------|---------------------|--------------------|
| `UiManager.Register`, `SetMode`, `CurrentMode`, `TargetMode`, `History` | mode-set | `HelloModes` and the gate | Register the panels and the layout before the modes. `SetMode` from a command applies at the start of the next `Update`. |
| `ModeHistory.Back()` | mode-set | `hello.back` | Returns `false` for an empty history. The apply of a back step adds no entry. |
| `UiManager.Apply` step 5 (the map) | mode-set | Both modes name `HelloState.Map` | The diff calls `Activate` on each apply, also for the same map. `Activate` writes the same delegate again and allocates nothing. |
| `PanelController.EnsureRoot()` | panel-definition | `HelloModes` builds each root at build time | The manager mounts the built root. No factory runs in a frame. |
| `SlotContainer` type id `SlotContainer` | slot-container | The type rule of `HelloScreen.css` | A type selector matches each slot. |
| `UiActionMap.Commands`, `AddAction`, `BindKey` | state-and-commands | `hello.mode`, `hello.back`, F1, F2 | One registry for the map, the buttons, and the panel context. A focused text field passes a non-text key to the map. |
| `CommandBindings.BindCommand` | state-and-commands | `Mode (F1)` button | The bind reads the name at click time. |
| Smoke input and `ViewportCenter` | dev-ops, interactive-elements | The Add click and the F keys | `ViewportCenter` needs an element in the tree. The Add point comes from the last frame with the state panel in the tree (Decision 6). |
| `AllocationGate.ReleaseAllocationContext()` | state-and-commands | The gate | Call it right before each measure. |

---

## Implementation Phases

The feature has 25 tasks, above the "about 8 tasks" of Slice rule 7, so it has three slices. Section 11 of `.beast-mode/phase-handoff.md` holds the rules. Phase 1 is the smallest path that proves the idea: a key in the window changes the panels on the screen (Q6). Phase 2 adds the smoke proof. Phase 3 adds the gate proof and the docs.

Each verification command sets `MSBUILDDISABLENODEREUSE=1` and gives `-nodeReuse:false` to `dotnet build`, because stale MSBuild nodes on this machine hang the build. Do not run `dotnet build-server shutdown`. Each task group ends with `MSBUILDDISABLENODEREUSE=1 dotnet build brutal-ui.sln -nodeReuse:false 2>&1 | tail -n 30` and `MSBUILDDISABLENODEREUSE=1 dotnet test --solution brutal-ui.sln 2>&1 | tail -n 30` (tool rule 5). Expect `0 Warning(s)`, `0 Error(s)`, and 0 failed. A warning fails the build (`TreatWarningsAsErrors`). The projects have no implicit usings, so each new file holds its own `using` lines. Use file-scoped namespaces. Prose in comments follows ASD-STE100. Read `Program.cs` in ranges only (1166 lines). Run one Framework class with `tests/BrutalUi.Framework.Tests/bin/Debug/net10.0/BrutalUi.Framework.Tests --filter-class '*<Class>' 2>&1 | tail -n 5` after a build.

Two flakes are known. A test host can exit with 134 (Internal CLR error 0x80131506), and the Core test `TheCurvesAllocateNothing` failed one time with 8128 bytes. Run the tests a second time before you treat either as a fault. A plant that does not compile leaves the old binary, and the old binary passes. Check `0 Error(s)` before you read the result of a plant. For each plant, copy the file to the scratchpad first, run the check, expect the failure, restore the file, check it with `cmp`, and run the tests again.

The smoke run needs a window and a GPU. Run it directly with `dotnet run --project samples/HelloPanel/HelloPanel.csproj -- --frames 120; echo "exit=$?"` on the desktop session of this host. When the run cannot open a window, record the build result and the test result, write a "Needs Human Verification" line in `context.md`, and go on (A8).

### Phase 1: Two Modes in the Window
**Goal:** HelloPanel registers one layout with the slots `top` and `bottom`, the panels `hello.widgets`, `hello.state`, and `hello.info`, and the modes `hello.main` and `hello.detail`. F1 and the `Mode (F1)` button run `hello.mode`, and F2 runs `hello.back`. The sheet styles the layout root, each slot, and each panel root. The smoke run still prints its six lines and passes.
**Demo:** a person presses F1 and sees the state row leave and the info panel enter. F2 brings it back.
**Needs:** none
**Agent tier:** STANDARD (sonnet). The phase uses the mode-set API as it is and changes sample files only.

**Tasks (one group):**
- [ ] 1.1 Fill in `docs/features/panels-and-modes/panel-sample/context.md` (create it when it does not exist). Check that `src/BrutalUi.Framework/Panels/UiManager.cs`, `ModeHistory.cs`, and `src/BrutalUi.Core/Tree/UiBeforeUpdateHandler.cs` exist. The mode-set files are uncommitted on this branch, so record a snapshot of `src/`: `git diff HEAD -- src/ | sha256sum` and `git status --porcelain --untracked-files=all -- src/ | sha256sum`. Record the base commit (`git rev-parse HEAD`), the solution test count with the skipped count, and the count of `BrutalUi.Framework.Tests`. At planning time, the solution read 2821 (3 skipped) and `BrutalUi.Framework.Tests` read 222. Record the Integration Points of this plan.
- [ ] 1.2 Edit `samples/HelloPanel/HelloScreen.cs`. Replace `Build` with `public static Element BuildWidgetPanel(TextureHandle checker, Button modeButton, out Label gateLabel, out HelloWidgets widgets)`. It returns the current `panel` element with its children (greeting, gate label, image, widget section) and a new mode row (an `Element` with the class `hello-row` that holds `modeButton`) as the last child. The state row is no longer a child of this panel. The panel keeps `IsFocusable = true` and the class `hello-panel`, and it gets the class `hello-panel-root` too. Keep `LoadSheet`, `FillChecker`, `GateText`, and the gate texts as they are.
- [ ] 1.3 In the same file, add `BuildStatePanel(HelloState state)` (a new `Element` with the classes `hello-panel` and `hello-panel-root` that holds `state.Row`, not focusable), `BuildInfoPanel()` (a new `Element` with the same two classes that holds one `Label` with the text `Detail mode. Press F2 to go back.` and `IsPickable = false`), and `BuildLayout()` (a root with `IsPickable = false` and the class `hello-slot-root`, and two children `new SlotContainer("top"u8)` and `new SlotContainer("bottom"u8)`, each with `IsPickable = false`). Replace `RootClass` (`hello-root`) with `SlotRootClass` (`hello-slot-root`) and add `PanelRootClass` (`hello-panel-root`). Add `using BrutalUi.Framework;`. Rewrite the class remarks and the method remarks: the tree of the Architecture Overview, the Tab order (the panel, `Click me`, the toggle, the slider, the text field, `Mode (F1)`, and `Add` in `hello.main`), the slot rule (a slot is not pickable, so a point between two panels hits no element), and the class names `hello-slot-root`, `hello-panel`, `hello-panel-root`, `hello-widgets`, `hello-row`, and `hello-status`.
- [ ] 1.4 Edit `samples/HelloPanel/HelloScreen.css`. Rename the rule `.hello-root` to `.hello-slot-root` and set it to `flex-direction: column; align-items: flex-start; padding: 24px;`. After it, add `SlotContainer { flex-direction: column; align-items: flex-start; }` and `.hello-panel-root { margin: 0 0 12px; }`. Each rule gets a one-line comment: the layout root is a column of the two slots, a slot is a column of its panels, and the margin keeps the panels apart. The slot rule sets no padding and no margin, so the top-left corner of the widget panel stays at the root padding. In the header comment, change "The sheet holds 31 rules." to 33 rules, and name the three layout rules.
- [ ] 1.5 Write `samples/HelloPanel/HelloModes.cs`, part 1: the class, its remarks, `MainName`, `DetailName`, the handlers `s_mode` and `s_back` of the Architecture Overview, and the first part of the constructor. The constructor checks each argument with `ArgumentNullException.ThrowIfNull`, registers `"hello.mode"u8` and `"hello.back"u8` in `state.Map.Commands` with the target `this`, calls `state.Map.AddAction("hello.mode"u8, "hello.mode"u8)` and `AddAction("hello.back"u8, "hello.back"u8)`, binds `(int)GlfwKey.F1` to `hello.mode` and `(int)GlfwKey.F2` to `hello.back`, and creates `ModeButton = new Button(new Button.CreateInfo { Text = "Mode (F1)" }).BindCommand(state.Map.Commands, "hello.mode"u8)`. When `GlfwKey` has no `F1` member, use the raw codes 290 and 291 with a comment.
- [ ] 1.6 Write `HelloModes.cs`, part 2. Create `PanelContext { Commands = state.Map.Commands, Events = new EventBus(), Services = new ServiceRegistry() }` and `Manager = new UiManager(new UiManager.CreateInfo { Context = context, Panels = panels })`. Register the panels `hello.widgets` (default slot `top`), `hello.state` (default slot `bottom`), and `hello.info` (default slot `bottom`) with the four instance factory methods, and keep each controller. Call `EnsureRoot()` on each controller (Decision 3). The widget factory calls `HelloScreen.BuildWidgetPanel(_checker, ModeButton, out _gateLabel, out _widgets)`. After the `EnsureRoot()` calls, copy `_gateLabel` and `_widgets` into `GateLabel` and `Widgets`, and throw `InvalidOperationException` when either is `null`. Set `WidgetPanel` from the controller root. Register the layout `hello.layout` with `CreateLayout`. Register `Main = new ModeDefinition("hello.main"u8, "hello.layout"u8, [new ModePanel("top"u8, "hello.widgets"u8), new ModePanel("bottom"u8, "hello.state"u8)], state.Map)` and `Detail` with `hello.info` in place of `hello.state`. The class remarks state the register order, the build rule of Decision 3, the map rule (both modes name one map, so the switch keeps the map), the toggle rule (`hello.mode` reads `TargetMode ?? CurrentMode`), the back rule, and the frame rule (a key in frame N applies at the start of frame N+1).
- [ ] 1.7 Edit `samples/HelloPanel/Program.cs`, `RunImGui` (about lines 518 to 566). Replace `ui.AddRoot(HelloScreen.Build(...))` with `HelloModes modes = new(ui, state, handle);` and `modes.Manager.SetMode(HelloModes.MainName);`. Remove `state.Map.Activate(ui);`, because the first apply activates the map of `hello.main` before the input pass of frame 0. Pass `modes.WidgetPanel`, `modes.GateLabel`, and `modes.Widgets` to `RunFrames` in place of the three `out` values. Change the comments of the block to match.
- [ ] 1.8 Edit the doc comments of `samples/HelloPanel/HelloState.cs` (the class remarks, the constructor remark, and the `Map` summary) and of `Program.cs` (the `panel` parameter of `RunFrames` and `PushSmokeInput`). The map rule: the first apply of a mode that names the map activates it, so the caller does not call `Activate`. The `Row` summary: `HelloScreen.BuildStatePanel` puts the row in the state panel. Change no code line of `HelloState.cs`.
- [ ] 1.9 Run the build and the tests. Expect 2821 (3 skipped), 0 failed, and no change of the test count. Run the smoke run. Expect six lines, `Update and Draw 0`, each count of line 5 and line 6 above 0, and `exit=0`. The mode does not change in the smoke run yet, so the `Add` click still hits. Run `dotnet run --project samples/HelloPanel/HelloPanel.csproj` with no frame limit and check the demo: F1 removes the state row and shows the info panel, F2 brings the state row back, a second F2 changes nothing, and a click on `Mode (F1)` toggles the mode. Click the text field, then press F1, and check that the mode changes. Record the demo and the counts in `context.md`. When no window opens, record "Needs Human Verification" for the demo.

**Estimated Steps:** about 55 (9 tasks, about six steps for one task)

**Files (one group, 6):** `context.md`, `HelloScreen.cs`, `HelloScreen.css`, `HelloModes.cs`, `Program.cs`, `HelloState.cs`

**Acceptance Criteria:**
- [ ] The build gives 0 warnings and 0 errors. The solution reads 2821 (3 skipped), 0 failed.
- [ ] The smoke run prints six lines with `Update and Draw 0` and exits with code 0, or `context.md` holds the "Needs Human Verification" line.
- [ ] F1, F2, and the `Mode (F1)` button change the panels as the demo states.
- [ ] `HelloState.cs` shows changed `///` lines only.

### Phase 2: The Mode Switch in the Smoke Run
**Goal:** The smoke run presses F1 and F2 in turn, so each measured frame runs one diff. The loop counts the frames in each mode. `Run` prints the seventh line and exits with code 1 when a mode count is 0. The `Add` click still hits in each `hello.main` frame. `docs/overview.md` holds the smoke contract line.
**Demo:** a developer runs `dotnet run --project samples/HelloPanel/HelloPanel.csproj -- --frames 120` and reads the seventh line `HelloPanel: modes: switches 60, backs 59, main frames 59, detail frames 58.` and the exit code 0.
**Needs:** Phase 1
**Agent tier:** STANDARD (sonnet). Take care with the frame rules of Decisions 5 and 6.

**Tasks (one group):**
- [ ] 2.1 Edit `samples/HelloPanel/Program.cs`, the record `RunResult`. Add the parameters `Switches`, `Backs`, `MainFrames`, and `DetailFrames` after `Versions`, each with a `<param>` doc. `Switches` and `Backs` cover the full run. `MainFrames` and `DetailFrames` count the measured frames with `hello.main` or `hello.detail` as `CurrentMode` after `Update`.
- [ ] 2.2 Edit `PushSmokeInput`. Add the parameter `float2 addPoint`, and push the `Add` click at `addPoint` in place of `ViewportCenter(state.AddButton)`. After the Escape block and before the longest text burst, push one key down and one key up with no target: F1 when `(frameIndex - WarmUpFrames) % 2 == 0`, F2 otherwise. The rule runs in each frame, also in the warm-up frames (Decision 5). Keep the order rule: the keys come before the field press and the move. Add a paragraph "The mode script" to the remarks: the keys, their place in the drain (the text field holds the focus and passes a non-text key to the map), the frame rule, and the counts that `--frames 120` gives.
- [ ] 2.3 Edit `RunFrames`. Add the parameter `HelloModes modes`. Add the locals `float2 addPoint = default`, `int mainFrames = 0`, and `int detailFrames = 0`. After `ui.Update(io.DeltaTime)`, when `ReferenceEquals(modes.Manager.CurrentMode, modes.Main)`, set `addPoint = ViewportCenter(state.AddButton)` (Decision 6). Pass `addPoint` to `PushSmokeInput`. Each line stays inside the UI region and allocates nothing.
- [ ] 2.4 In `RunFrames`, inside the `if (measured)` block, add 1 to `mainFrames` when `CurrentMode` is `modes.Main` after `Update`, and to `detailFrames` when it is `modes.Detail`. Read `CurrentMode` into a local right after `Update`, as `fieldFocused` is read. Return `modes.Switches`, `modes.Backs`, `mainFrames`, and `detailFrames` in the new `RunResult` fields. Pass `modes` from `RunImGui`. In the remarks, add the mode counts and line 7 to the paragraphs of the gate counts and of the result lines.
- [ ] 2.5 Edit `Run`. After the line of the state counts, print `HelloPanel: modes: switches {result.Switches}, backs {result.Backs}, main frames {result.MainFrames}, detail frames {result.DetailFrames}.`. After the state check, inside the `frameLimit != NoFrameLimit && result.MeasuredFrames >= 2` block, add the mode check: when a count of the four is 0, write `HelloPanel: the synthetic input did not reach the mode switch.` to standard error and return `ExitFailure`. Add a paragraph "The mode check" to the remarks of `Run`.
- [ ] 2.6 Edit `docs/overview.md`. After the line `**GPU smoke contract (Phase 3):** ... sixth line with the store counts ...`, add `- **GPU smoke contract (Phase 3, modes):** The HelloPanel smoke run switches the mode in each frame after warm-up, prints a seventh line with the mode counts, and exits with code 1 when a count is 0.`. Change no other line.
- [ ] 2.7 Run the build, the tests (2821, 3 skipped, 0 failed), and the smoke run. Expect seven lines, line 3 with `Update and Draw 0`, line 6 with each count above 0, line 7 with `switches 60, backs 59, main frames 59, detail frames 58`, and `exit=0`. When line 6 shows `adds 0`, read Risk 1 before a change. When line 3 shows bytes above 0, read Risk 4. Record the seven lines in `context.md`.
- [ ] 2.8 Run two plants on `samples/HelloPanel/HelloModes.cs` with the smoke run. Plant 1: remove the line `modes.Backs++;`. Expect line 7 with `backs 0`, the line `HelloPanel: the synthetic input did not reach the mode switch.`, and `exit=1`. Plant 2: add `GC.KeepAlive(new object());` as the first line of `s_mode`. Expect line 3 with bytes above 0 and `exit=1`. Restore the file after each plant, check it with `cmp`, and run the smoke run again. Record the lines in `context.md`. When no window opens, record both plants under "Needs Human Verification".

**Estimated Steps:** about 45 (8 tasks, about five to six steps for one task)

**Files (one group, 4):** `Program.cs`, `overview.md`, `HelloModes.cs` (plants only, then restored), `context.md`

**Acceptance Criteria:**
- [ ] The smoke run prints seven lines with the line 7 counts of the demo, `Update and Draw 0`, and exit code 0.
- [ ] Both plants give exit code 1, and the restored file equals its copy.
- [ ] `docs/overview.md` holds the new contract line, and no other line of it changed.

### Phase 3: The Gate, the Docs, and the Full Gate
**Goal:** `ModeDiffAllocationTests` proves 0 bytes for the toggle cycle on a HelloPanel shape and for an idle frame, and a real-work test reads the tree. Plants prove each test. Section 14 of the technical design and the changelog record the feature. `scripts/test-linux.sh` passes, and the STE lint passes.
**Demo:** a developer runs `tests/BrutalUi.Framework.Tests/bin/Debug/net10.0/BrutalUi.Framework.Tests --filter-class '*ModeDiffAllocationTests'` after a build and reads a total of 3 with `failed: 0`.
**Needs:** Phase 2
**Agent tier:** STANDARD (sonnet). This phase needs the most care of the three: the measured body must hold no hidden allocation, the real-work test must fail on an empty body, and the plants must fail the right tests.

**Tasks (group 1, the gate):**
- [x] 3.1 Write `tests/BrutalUi.Framework.Tests/Panels/ModeDiffAllocationTests.cs`, part 1: the class (namespace `BrutalUi.Framework.Tests`), its remarks (the eighteenth gate and the rule of the measured body), the constants `Iterations = 100`, `WarmUpRounds = 2`, and `EscapeKey = 256`, the static handler `s_count` and the selector `s_score = static s => s` in fields, a `Counter` class with `Calls`, a `Factory` class that records its calls and returns one built root, and a `Scene` class. `BuildScene` creates a `UiContext` with a `RecordingRenderer` measurer and a viewport of 800 by 600, a `CommandRegistry` with `mda.mode`, `mda.add`, and `mda.cancel` (each with its own `Counter`), one `UiActionMap` with the action `mda.cancel` on Escape, a `Store<int>`, and a `PanelContext`. The three panels use names with the prefix `mda.` and inline styles only. The widget panel holds a `Label`, a `Button` `Mode` bound to `mda.mode`, and a `TextField` (capacity 16, 120 by 24). The state panel holds a `Button` `Add` bound to `mda.add` (80 by 24) and a `Label` bound with `BindText(store, s_score)`. The info panel holds one `Label`. The layout root is a column of 800 by 600 with `IsPickable = false` and two `SlotContainer` children `top` and `bottom`. `Main` names `top [widgets]` and `bottom [state]`. `Detail` names `top [widgets]` and `bottom [info]`. Both modes name the one map. The build applies `Main`, gives the focus to the field, runs `Update` and `Draw`, and runs one toggle step, so each root is built, styled, and laid out.
- [x] 3.2 Write part 2: the step methods and the 3 tests. `ToggleStep(Scene)` runs `SetMode(Detail)`, `Update(0.016f)`, `Score.Set(Score.Value + 1)`, `SetMode(Main)`, `Update`, `History.Back()`, `Update` (back to `Detail`), `SetMode(Main)`, `Update`, and `Draw`. After each `Update`, it adds 1 when `CurrentMode` is the expected mode, plus the child count of `top` and of `bottom`, to a local `int`. It returns the sum (12 for one step). `IdleStep(Scene)` runs `Update(0.016f)` and `Draw` with no target and returns the same check for `Main` (3). The tests: `TheToggleCycleAllocatesNothingAfterWarmUp` (2 warm-up rounds, `AllocationGate.ReleaseAllocationContext()` right before the measure, 100 steps, `Assert.Equal(0L, after - before)`, then `Assert.Equal(Iterations * 12, sum)`), `AnIdleFrameAllocatesNothing` (the same shape, sum `Iterations * 3`), and `TheMeasuredBodyDoesRealWork` (one toggle step returns 12, `CurrentMode` is `Main`, `TargetMode` is `null`, `bottom` holds the state root only, the info root has no parent, the widget root has `top` as its parent, the score label text equals the UTF-8 text of `Score.Value`, `History.Count` is 2 more than before the step, each factory ran one time, and the map `IsActive`. One idle step returns 3 and keeps `History.Count`). The measured bodies hold no lambda, no string, no boxed value, no array, and no `Assert`.
- [x] 3.3 Run the build, the tests, and `ModeDiffAllocationTests` (total 3). Expect the solution at 2824 (3 skipped), 0 failed, and `BrutalUi.Framework.Tests` at 225. Run three plants with `MSBUILDDISABLENODEREUSE=1 dotnet test --solution brutal-ui.sln > /dev/null 2>&1; echo "exit=$?"` and the class run. Plant 3: `GC.KeepAlive(new object());` as the first line of `UiManager.Apply` in `src/BrutalUi.Framework/Panels/UiManager.cs`. Expect `TheToggleCycleAllocatesNothingAfterWarmUp` to fail and `AnIdleFrameAllocatesNothing` to pass. Plant 4: the same line as the first line of the `OnBeforeUpdate` handler of `UiManager`, before the target check. Expect both 0-byte tests to fail. Plant 5: an empty body of `ToggleStep` that returns 12. Expect `TheMeasuredBodyDoesRealWork` to fail and `TheToggleCycleAllocatesNothingAfterWarmUp` to pass. Restore each file, check it with `cmp`, and check the `src/` snapshot of task 1.1. Record the failed tests and the measured bytes in `context.md`.

**Tasks (group 2, the docs and the full gate):**
- [x] 3.4 Edit section 14 of `docs/technical-design.md`. In the bullet `**Mode tests.**`, change the sentence that says that panel-sample owns the gate `ModeDiffAllocationTests` to the past: the gate now exists (next bullet). After that bullet, add a bullet `**An eighteenth allocation gate.**` in the shape of the bullets of the earlier gates: `ModeDiffAllocationTests` (3 tests) in `tests/BrutalUi.Framework.Tests/Panels/`, the scene, the toggle step, the idle step, the real-work checks, and plants 3 to 5 with their results and bytes. In the bullet of the sample projects (the bullet that names `HelloPanel: state:`), add the two modes, the F1 and F2 script, the cached `Add` point, the seventh line with its format, the mode check, and plants 1 and 2 with their results.
- [x] 3.5 Edit `CHANGELOG.md`. Under `## [Unreleased]`, `### Added`, add three lines after the mode-set lines: HelloPanel shows two modes with the layout `hello.layout`, the panels `hello.widgets`, `hello.state`, and `hello.info`, a `Mode (F1)` button, F1 for the switch, and F2 for the back step (panels-and-modes/panel-sample). The HelloPanel smoke run switches the mode in each frame and prints a seventh line with the mode counts (panels-and-modes/panel-sample). `ModeDiffAllocationTests`, the eighteenth allocation gate (panels-and-modes/panel-sample). Under `### Changed`, add one line: `HelloScreen.Build` splits into four build methods, `UiManager` adds the root of the sample, and the class `hello-root` becomes `hello-slot-root`.
- [x] 3.6 Run `MSBUILDDISABLENODEREUSE=1 timeout 600 scripts/test-linux.sh; echo "exit=$?"`. Expect the PASS line and `exit=0`. When the smoke step fails from the host Xvfb fault (A8), record the split evidence (the build, test, gpu, and loader steps, with the test count) and the direct smoke run of task 2.7 in `context.md`, and add a "Needs Human Verification" line for a re-run on a repaired host. Add a "Needs Human Verification" line for `scripts/test-windows.ps1` on Windows.
- [x] 3.7 Run the demo of each phase again: the direct smoke run (line 7 of the phase 2 demo) and `ModeDiffAllocationTests` (total 3). Check the `src/` snapshot of task 1.1 (both hashes equal). Run `python3 /home/aaronstatic/.claude/skills/asd-ste100/scripts/ste-lint.py --fail-over 2.5` on `docs/technical-design.md`, `docs/overview.md`, `CHANGELOG.md`, `context.md`, and this plan, and fix the prose until each file passes.
- [x] 3.8 Record the demos, the results, the "Needs Human Verification" items (the F1 and F2 demo when no window opened, plants 1 and 2 when no window opened, the smoke step on a repaired host, and the Windows script), and each decision that this plan did not settle in `context.md`.

**Estimated Steps:** about 55 (8 tasks, about seven steps for one task)

**Files (group 1, 4):** `ModeDiffAllocationTests.cs`, `UiManager.cs` (plants 3 and 4 only, then restored), `context.md`, `implementation.md` (checkboxes)
**Files (group 2, 4):** `technical-design.md`, `CHANGELOG.md`, `context.md`, `implementation.md`

**Acceptance Criteria:**
- [ ] The 3 tests of `ModeDiffAllocationTests` pass. The solution reads 2824 (3 skipped), 0 failed.
- [ ] Plants 3 to 5 fail the tests that task 3.3 names, and each restored file equals its copy.
- [ ] `scripts/test-linux.sh` prints its PASS line, or `context.md` holds the split evidence and the "Needs Human Verification" line.
- [ ] Each changed doc passes the STE lint.

---

## Key Technical Decisions

### Confirmed Assumptions

- Q1 Panels: the widget panel is in both modes. The state row is in mode one only. A new info panel is in mode two only.
- Q2 Line: the seventh line counts the switches, the backs, and the frames in each mode. Exit 1 when a count is 0.
- Q3 Gate: `ModeDiffAllocationTests` measures a HelloPanel shape with real widgets, a toggle cycle, Update, and Draw. It is the eighteenth gate.
- Q4 Keys: F1 switches the mode. F2 steps back.
- Q5 Maps: both modes name the one map of `HelloState`. The map gets the actions `hello.mode` and `hello.back`.
- Q6 Demo: a person presses F1 and sees the state row leave and the info panel enter. F2 brings it back.
- A1 The code changes live in `samples/HelloPanel/`, one new test class, and the docs. No file of `src/` changes.
- A2 The names: the layout `hello.layout` with the slots `top` and `bottom`, the modes `hello.main` and `hello.detail`, the panels `hello.widgets`, `hello.state`, and `hello.info`.
- A3 The command `hello.mode` toggles between the two modes with `SetMode`. The command `hello.back` calls `History.Back()`.
- A4 The smoke input presses F1 in each even measured frame and F2 in each odd measured frame. Each measured frame then runs one diff.
- A5 `HelloScreen.css` gets a rule for `SlotContainer` by type and one class rule for each panel root. The classes are `hello-slot-root` for the layout root and `hello-panel-root` for each panel root.
- A6 The gate class lives in `tests/BrutalUi.Framework.Tests/Panels/`. It has three tests: the toggle cycle, the idle frame, and the real-work check.
- A7 The docs changes: the Phase 3 smoke contract line in `docs/overview.md`, section 14 of `docs/technical-design.md`, and `CHANGELOG.md`.
- A8 The smoke step of `scripts/test-linux.sh` fails on this host from the Xvfb fault. The plan treats it as a manual gate, as mode-set does.

### Decision 1: A new `HelloModes` class owns the mode model (the chosen approach)
**Choice:** `samples/HelloPanel/HelloModes.cs` owns the `PanelContext`, the `UiManager`, the register calls, the two commands, the two actions, the two keys, and the mode button. `HelloScreen` keeps the tree builders. `Program.cs` creates `HelloModes` and calls `SetMode`.
**Rationale:** One file to read the mode model, the least churn in the existing sample files, and the state sample stays apart from the mode sample.
**Alternatives:** (a) The manager inside `HelloState`: it mixes the two samples. (b) One file for each panel: the most churn. The user rejected both.

### Decision 2: One command registry for the map, the buttons, and the panel context
**Choice:** `HelloModes` registers `hello.mode` and `hello.back` in `state.Map.Commands`, and the `PanelContext` takes the same registry. `HelloState` changes no code line.
**Rationale:** The map gives the action name to its own registry at each invoke, so the commands of an action must live there (Q5). One registry also lets a panel factory bind a button to any command of the sample. `UiActionMap.Commands` is public, so the sample needs no new member.
**Alternatives:** A second registry in `HelloModes`: a key on the map cannot reach it.

### Decision 3: `HelloModes` builds each panel root at build time through `EnsureRoot()`
**Choice:** The three factories have the shape `Element Name(PanelContext context)` and build their trees. The constructor calls `EnsureRoot()` on each controller after the panel registers. The manager then mounts the built roots.
**Rationale:** The frame loop and the smoke run need `WidgetPanel`, `GateLabel`, and `Widgets` before frame 0. A lazy build of the info panel would also run in the first `hello.detail` frame and allocate in the frame. The factories keep the shape that the xml-markup loader fills later, and the sample shows that a game can choose the build time.
**Alternatives:** (a) Lazy roots: the references do not exist before frame 0. (b) Prebuilt roots that a factory only returns: the factory then hides the build, and the sample teaches a false shape.

### Decision 4: `hello.mode` toggles from `TargetMode ?? CurrentMode`, and `hello.back` counts a true `Back()` only
**Choice:** The toggle reads the waiting target first, then the current mode. It sets `hello.main` when that mode is `Detail`, and `hello.detail` otherwise. `Switches` counts each run of `hello.mode`. `Backs` counts each `Back()` that returns `true`.
**Rationale:** Two presses of F1 in one frame then toggle two times, as a person expects. `ReferenceEquals` on the stored definitions allocates nothing. A `Back()` on an empty history changes nothing, so it is not a back step. In the smoke run, the F2 of frame 0 meets an empty history and does not count.
**Alternatives:** (a) Toggle from `CurrentMode` only: a second press in one frame does nothing. (b) Count each run of `hello.back`: the count then holds steps that did no work.

### Decision 5: The mode keys run in each frame, also in the warm-up frames, with the parity of A4
**Choice:** `PushSmokeInput` pushes F1 when `(frameIndex - WarmUpFrames) % 2 == 0` and F2 otherwise, in each frame. Thus F1 runs in each even measured frame and F2 in each odd measured frame (A4). F1 also runs in frame 1, and F2 in frames 0 and 2.
**Rationale:** The first entry of the info panel then falls in frame 2, a warm-up frame, so its first style pass and its first layout do not count in line 3. Each measured frame from frame 3 applies one diff. With `--frames 120`, the run gives 60 switches (each odd frame), 59 backs (each even frame from 2), 59 measured `hello.main` frames, and 58 measured `hello.detail` frames. The existing widget events also run in the warm-up frames, so the pattern is the same.
**Alternatives:** (a) Keys in the measured frames only: the first entry of the info panel falls in frame 4, a measured frame. (b) A change of `WarmUpFrames`: the longest text burst needs an odd first measured frame, and the counts of lines 3 and 4 change.

### Decision 6: The `Add` click aims at a point from the last frame with the state panel in the tree
**Choice:** `RunFrames` sets `addPoint = ViewportCenter(state.AddButton)` after each `Update` that ends in `hello.main`, and `PushSmokeInput` pushes the `Add` click at `addPoint`.
**Rationale:** `ViewportCenter` adds the rects of the ancestors, so it needs an element in the tree. In a frame that starts in `hello.detail`, the state panel has no parent at push time, and the old code would aim at the wrong point. The diff at the start of the next `Update` mounts the state panel again with its old rects. The hit test of that `Update` reads them, so a click at the stored point hits `Add`. The store is a `float2` local and allocates nothing. In a `hello.detail` frame, the click hits the info panel or no element, and the slider press after it takes the focus as before.
**Alternatives:** (a) Push the `Add` click only in `hello.main` frames at a fresh point: the point is still stale at push time. (b) Put the state panel in both modes: against Q1.

### Decision 7: The layout root and each slot are not pickable
**Choice:** `BuildLayout` sets `IsPickable = false` on the layout root and on both slots.
**Rationale:** The layout-root rule of `UiManager` asks for a root that is not pickable. A slot is a structural element, so a point between two panels must reach the game. The off-panel move of the smoke run (8 pixels above and to the left of the widget panel) then still hits no element, and line 4 keeps its off-panel frames.
**Alternatives:** Pickable slots: a point in the gap of 12 pixels between the panels reads as over the UI.

### Decision 8: The sheet renames `.hello-root` to `.hello-slot-root` and adds two rules
**Choice:** `.hello-slot-root { flex-direction: column; align-items: flex-start; padding: 24px; }`, `SlotContainer { flex-direction: column; align-items: flex-start; }`, and `.hello-panel-root { margin: 0 0 12px; }`. The three panel roots keep `hello-panel` for the fill, the border, and the hover and focus rules, and get `hello-panel-root` for the space between them.
**Rationale:** A5 names the classes. The old root class has no other use, so a rename keeps one rule for the root. `flex-direction` is explicit, because the root now holds two children. A slot with no padding and no margin keeps the top-left corner of the widget panel at the root padding, so the smoke points stay valid.
**Alternatives:** Keep `.hello-root` next to `.hello-slot-root`: two rules for one element.

### Decision 9: The gate uses a closed toggle cycle with a store change and `Draw`
**Choice:** One step runs `SetMode(Detail)`, `Update`, a store set, `SetMode(Main)`, `Update`, `History.Back()`, `Update`, `SetMode(Main)`, `Update`, and `Draw`. The step ends in `Main`, so each step does the same work. The history grows by 2 in each step and fills its ring of 16. A full ring drops its oldest entry with no allocation.
**Rationale:** Q3 asks for real widgets, a toggle cycle, `Update`, and `Draw`. The store set while the state panel is unmounted makes the label binding do real work at the next mount. The focused text field makes the caret pass run in each `Update`. Both modes name one map, so each apply runs the same-map `Activate` of Q5. The gate differs from the two 0-byte tests of `ModeDiffTests`, which use synthetic panels, two layouts, and no `Draw`.
**Alternatives:** The open cycle `SetMode(Detail)`, `SetMode(Main)`, `Back()`: it ends in `Detail`, so the next step starts with a `SetMode` of the current mode, and the steps differ.

### Decision 10: The plants edit `src/` for one run only
**Choice:** Plants 3 and 4 edit `UiManager.cs` and restore it. Task 3.3 and task 3.7 check the `src/` snapshot of task 1.1.
**Rationale:** A plant in `Apply` proves that the gate measures the diff. A1 binds the end state, and `cmp` and the snapshot prove it. The mode-set plan used the same plants.
**Alternatives:** Plants in the gate scene only: they cannot prove that the gate measures the library path.

---

## Risks & Mitigation

| Risk | Likelihood | Impact | Mitigation |
|------|------------|--------|------------|
| 1. The smoke input positions go stale when the layout changes in each frame. `ViewportCenter` of a detached `Add` button misses, line 6 shows `adds 0`, and the state check fails. | High | High | Decision 6 stores the point after each `hello.main` frame. The widget panel stays in `top` with no join, so the other widget points keep their parents. The bottom slot keeps its position in both modes. When the hit test prunes by the rect of the bottom slot, the info panel (one label at 26 pixels plus the padding) still covers the center of `Add`. Task 2.7 checks line 6. |
| 2. The text field holds the focus at the F keys. A field that takes F1 would stop the switch. | Low | High | The smoke pushes the keys after the Tab, as the Escape key, and line 6 already proves that a focused field passes a non-text key to the map. Task 1.9 checks F1 with the field focused by hand. Task 2.7 checks line 7. |
| 3. The host Xvfb fault stops the smoke step of `scripts/test-linux.sh` (A8). | High | Medium | Task 2.7 runs the smoke run directly on the desktop session. Task 3.6 records the split evidence and a "Needs Human Verification" line, as mode-set did. |
| 4. The first entry or the first return of a panel allocates in a measured frame (a first style pass, a first layout, a growth of a child list). Line 3 shows bytes above 0. | Medium | High | Decision 3 builds each root at build time. Decision 5 puts the first entry of the info panel in frame 2. The first return of the state panel falls in frame 3, the first measured frame. When line 3 shows bytes, probe the frame that allocates, record it in `context.md`, and escalate to the main thread before a change of `WarmUpFrames` (the longest text burst needs an odd first measured frame). |
| 5. The same-map `Activate` call on each apply. | Low | Low | `UiManager.Apply` step 5 calls `Activate` on the map of the new mode, also for the same map. `UiActionMap.Activate` writes the same delegate into `OnUnhandledInput` again and changes nothing else. It allocates nothing. The gate measures it (Decision 9). |
| 6. The frame rule of the hit test. The hit test of the `Update` of a change reads the layout of the frame before, so a new root gets no hit in that `Update`. | Medium | Medium | Each root is laid out in a warm-up frame first (Decisions 3 and 5), so each later return has its old rects. A press of a person in the frame of a change can hit the old tree. That is the documented rule of mode-set, and the remarks of `HelloModes` state it. |
| 7. The removal of `state.Map.Activate(ui)` leaves frame 0 with no active map before its `Update`. | Low | Low | The first apply runs at the start of `Update` of frame 0, before the input pass, so each key of frame 0 reaches the map. `PushGamepad` runs before `Update`, and the smoke run pushes no gamepad button. |
| 8. A `SetMode` call from inside the diff throws. | Low | Medium | The sample calls `SetMode` only from a command handler (the input pass) and from `RunImGui` before the loop. No factory calls it. |
| 9. `dotnet build` or `dotnet test` hangs on stale MSBuild nodes. | High | Low | Each command sets `MSBUILDDISABLENODEREUSE=1` and `-nodeReuse:false`. No `dotnet build-server shutdown`. |

---

## Dependencies

### Internal Dependencies
- `panels-and-modes/mode-set` (done, uncommitted on this branch): `UiManager`, `ModeHistory`, `UiContext.OnBeforeUpdate`. It does not change for this feature.
- `panels-and-modes/panel-definition` (done): `PanelDefinition`, `LayoutDefinition`, `ModeDefinition`, `ModePanel`, `PanelContext`, `PanelController.EnsureRoot()`.
- `panels-and-modes/slot-container` (done): `SlotContainer` and its type id.
- `state-and-commands` (done): `HelloState`, `UiActionMap`, `CommandRegistry`, `EventBus`, `ServiceRegistry`, `CommandBindings.BindCommand`, `Store<T>`, `LabelBindings.BindText`.
- `dev-ops` (done): the smoke contract and `scripts/test-linux.sh` (no script change).
- The test helpers `tests/BrutalUi.Framework.Tests/Shared/AllocationGate.cs` and the linked `RecordingRenderer.cs`.

### External Dependencies
- None. No new package and no native dependency.

### Features That Depend on This One
- None in this epic. The xml-markup epic reads the same records later.

---

## Definition of Done

All criteria below must pass. An independent evaluator runs the Verification Method from `/home/aaronstatic/dev/brutal-ui` on Linux. `<base>` is the base commit and the `src/` snapshot that task 1.1 records in `context.md`.

### Functional Criteria
- [ ] F1. HelloPanel shows the widget panel in the top slot and the state row panel in the bottom slot after start. F1 removes the state row panel and shows the info panel with the text `Detail mode. Press F2 to go back.`. F2 shows the state row panel again. A second F2 changes nothing. A click on `Mode (F1)` toggles the mode. F1 works while the text field holds the focus.
- [ ] F2. The widget panel stays in the top slot through each switch. Its text field keeps its text, and the score of the state row keeps its value over a switch and a return.
- [ ] F3. The smoke run with `--frames 120` prints seven lines. Line 7 reads `HelloPanel: modes: switches 60, backs 59, main frames 59, detail frames 58.`. Line 3 reads `Update and Draw 0`. Each count of line 5 and line 6 is above 0. The exit code is 0.
- [ ] F4. The smoke run exits with code 1 and writes `HelloPanel: the synthetic input did not reach the mode switch.` when a mode count is 0 (plant 1).
- [ ] F5. `ModeDiffAllocationTests` holds `TheToggleCycleAllocatesNothingAfterWarmUp`, `AnIdleFrameAllocatesNothing`, and `TheMeasuredBodyDoesRealWork`, and each passes. Each 0-byte test calls `AllocationGate.ReleaseAllocationContext()` right before its measure and asserts 0 bytes and a fixed sum.
- [ ] F6. `HelloScreen.css` holds `.hello-slot-root`, `SlotContainer`, and `.hello-panel-root`, and no `.hello-root`. The layout root has the class `hello-slot-root`, and each panel root has `hello-panel-root`.

### Quality Criteria
- [ ] Q1. `dotnet build brutal-ui.sln -nodeReuse:false` gives `0 Warning(s)` and `0 Error(s)`.
- [ ] Q2. `dotnet test --solution brutal-ui.sln` gives 2824 tests, 3 skipped, 0 failed. The 2821 tests of `<base>` still pass. `BrutalUi.Framework.Tests` holds 225.
- [ ] Q3. Plants 3 to 5 fail the tests that task 3.3 names. Plant 2 gives bytes above 0 in line 3. Each restored file equals its copy.
- [ ] Q4. The measured bodies of `ModeDiffAllocationTests` hold no `=>` lambda, no string literal, no `new` array, and no `Assert`. The handlers of `HelloModes` are `static` fields.
- [ ] Q5. Each changed doc and this plan pass the STE lint with `--fail-over 2.5`.

### Integration Criteria
- [ ] I1. The `src/` snapshot equals the snapshot of task 1.1. No `.csproj`, `.props`, `.sln`, or `scripts/` file changed. The only changed test path is the new `ModeDiffAllocationTests.cs`.
- [ ] I2. `Program.cs` holds no `ui.AddRoot(` call and no `state.Map.Activate(` call. It holds `new HelloModes(` and one `SetMode(HelloModes.MainName)` call.
- [ ] I3. `HelloModes.cs` registers the three panels and the layout before the two modes, and both modes name `state.Map`.
- [ ] I4. `docs/overview.md` holds the Phase 3 modes contract line. Section 14 of `docs/technical-design.md` holds the bullet `**An eighteenth allocation gate.**` and the seventh line in the sample bullet. `CHANGELOG.md` names `ModeDiffAllocationTests` and the HelloPanel modes.
- [ ] I5. `scripts/test-linux.sh` prints its PASS line, or `context.md` holds the split evidence and the "Needs Human Verification" line (A8).

### Verification Method

Run each step from `/home/aaronstatic/dev/brutal-ui` on Linux. `<scratch>` is any empty folder outside the repository. Do not run `dotnet build-server shutdown`. When a run exits with 134 or `TheCurvesAllocateNothing` fails, run it a second time.

1. `MSBUILDDISABLENODEREUSE=1 dotnet build brutal-ui.sln -nodeReuse:false 2>&1 | tail -n 30`. Expect `0 Warning(s)` and `0 Error(s)` (Q1).
2. `MSBUILDDISABLENODEREUSE=1 dotnet test --solution brutal-ui.sln 2>&1 | tail -n 30`. Expect 2824 total, 3 skipped, 0 failed (Q2).
3. `tests/BrutalUi.Framework.Tests/bin/Debug/net10.0/BrutalUi.Framework.Tests --filter-class '*ModeDiffAllocationTests' 2>&1 | tail -n 5`. Expect a total of 3 and `failed: 0`. Read the three test names and the measured bodies (F5, Q4).
4. `dotnet run --project samples/HelloPanel/HelloPanel.csproj -- --frames 120; echo "exit=$?"`. Expect seven lines, line 7 `HelloPanel: modes: switches 60, backs 59, main frames 59, detail frames 58.`, line 3 with `Update and Draw 0`, and `exit=0` (F3). When no window opens, read the "Needs Human Verification" line of `context.md`.
5. `dotnet run --project samples/HelloPanel/HelloPanel.csproj`. Press F1 and see the state row panel leave and the info panel enter. Press F2 and see the state row panel return. Press F2 again and see no change. Click `Mode (F1)` two times and see two switches. Click the text field, type a char, press F1 and F1 again, and see the char stay (F1, F2).
6. Copy `samples/HelloPanel/HelloModes.cs` to `<scratch>`. Remove the line `modes.Backs++;`. Run step 4. Expect `backs 0`, the line of F4, and `exit=1`. Restore the file and check it with `cmp` (F4).
7. Copy `src/BrutalUi.Framework/Panels/UiManager.cs` to `<scratch>`. Add `GC.KeepAlive(new object());` as the first line of `Apply`. Run step 3. Expect `TheToggleCycleAllocatesNothingAfterWarmUp` to fail and `AnIdleFrameAllocatesNothing` to pass. Restore the file and check it with `cmp` (Q3).
8. `git diff HEAD -- src/ | sha256sum` and `git status --porcelain --untracked-files=all -- src/ | sha256sum`. Expect the two hashes of `context.md`. `git status --porcelain -- '*.csproj' '*.props' '*.sln' scripts/ tests/`. Expect only `tests/BrutalUi.Framework.Tests/Panels/ModeDiffAllocationTests.cs` as a new test file, plus the mode-set test files of `<base>` (I1).
9. `grep -n 'AddRoot(\|Map.Activate(\|new HelloModes(\|SetMode(' samples/HelloPanel/Program.cs`. Expect `new HelloModes(` and `SetMode(HelloModes.MainName)` only (I2). Read the constructor of `HelloModes.cs` (I3). `grep -n 'hello-root\|hello-slot-root\|SlotContainer\|hello-panel-root' samples/HelloPanel/HelloScreen.css` (F6).
10. `MSBUILDDISABLENODEREUSE=1 timeout 600 scripts/test-linux.sh; echo "exit=$?"`. Expect the PASS line and `exit=0`, or the split evidence of A8 (I5).
11. Read the new line of `docs/overview.md`, section 14 of `docs/technical-design.md`, and `## [Unreleased]` of `CHANGELOG.md` (I4). Run `python3 /home/aaronstatic/.claude/skills/asd-ste100/scripts/ste-lint.py --fail-over 2.5` on each changed doc (Q5).

---

## Testing Strategy

### Allocation Tests
- `Framework.Tests/Panels/ModeDiffAllocationTests` (3): the toggle cycle with real widgets, `Update`, and `Draw` at 0 bytes, the idle frame at 0 bytes, and the real-work check. 2 warm-up rounds, a release of the allocation context, 100 measured iterations, an assert of 0 bytes and of a fixed sum. Plants 3 to 5 guard them.

### Smoke Tests
- The HelloPanel smoke run with `--frames 120`: each measured frame runs one diff under the UI region of line 3. Line 7 and the mode check prove that the keys reach the commands and that both modes show. Plants 1 and 2 guard the check and the allocation gate of the sample.

### Regression Tests
- Each existing test runs with no edit. The six lines of the smoke run keep their checks. Lines 5 and 6 still show counts above 0 under the per-frame switch (Decision 6).

### Manual Testing
- The F1 and F2 demo, the `Mode (F1)` button, and F1 with a focused text field (Verification step 5).
- `scripts/test-windows.ps1` on Windows. The script reads the exit code of the smoke run, so the new mode check runs there too.

---

## Related Documentation

- `docs/features/panels-and-modes/panel-sample/requirements.md`
- `docs/features/panels-and-modes/epic-overview.md` (Q1 to Q4 and A1 to A9, with A7 and A8 for this feature)
- `docs/features/panels-and-modes/mode-set/implementation.md` (the diff rules, the map rule, the history rule, and Decision 10 that reserves the gate name)
- `docs/technical-design.md` sections 10 and 14
- `docs/overview.md` (the smoke contract lines)
- `.beast-mode/phase-handoff.md` section 11 (the slice rules)

---

## Notes

### Open Questions
None blocks the work. The items below record how the plan reads the confirmed items and the code.

### Notes on the Confirmed Assumptions
- A1: Plants 3 and 4 edit `src/BrutalUi.Framework/Panels/UiManager.cs` for one run and restore it (Decision 10). The end state keeps A1, and the snapshot check proves it. The mode-set files of `src/` are uncommitted on this branch, so the check compares a hash of the diff, not `git diff` against `HEAD` alone.
- A4: The plan also pushes the keys in the warm-up frames with the same parity (Decision 5). The measured frames follow A4 exactly. The warm-up keys move the first entry of the info panel out of the measured frames.
- A5: The current sheet holds `.hello-root`. The plan renames it to `.hello-slot-root` (Decision 8), so the sheet goes from 31 to 33 rules.
- Q2: The switch count and the back count cover the full run, as the counts of line 6 do. The frame counts cover the measured frames, as the counts of line 4 do. The back count holds only a `Back()` that returned `true` (Decision 4).
- Q5: Both modes name `HelloState.Map`, so each apply calls `Activate` on the active map again. The call changes nothing (Risk 5).

### Findings From the Code
- The smoke `Add` click uses `ViewportCenter(state.AddButton)`, and `ViewportCenter` needs an element in the tree. Under a switch in each frame, the state panel has no parent at push time in half of the frames, so the old point misses. Decision 6 fixes it in the sample. No library change is needed.
- `PanelController.EnsureRoot()` is public, so a game can build a panel root at build time and the manager mounts it later (Decision 3).
- `UiActionMap.Commands` is public, so `HelloModes` adds its commands to the registry of `HelloState` with no change of `HelloState` code (Decision 2).

### Agent Routing
Each phase routes to the STANDARD dev agent (sonnet). No phase is a refactor of a library type. Phase 3 needs the most care: the gate scene, the closed toggle cycle, the real-work asserts, and the plants that must fail the named tests. Phase 2 needs care with the frame rules of Decisions 5 and 6.
