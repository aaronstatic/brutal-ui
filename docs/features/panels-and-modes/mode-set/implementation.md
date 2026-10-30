# Mode Set - Implementation Plan

**Epic:** panels-and-modes (feature 3 of 4)
**Status:** Planning
**Started:** 2026-10-30
**Target Completion:** TBD
**Last Updated:** 2026-10-30 07:30

---

## Quality Bar

**Target quality:** A library feature that is the center of the panels-and-modes epic. It adds the one Core change of the epic and the mode diff that each later screen of a game uses. A wrong diff loses the state of a panel, leaves a stale panel on the screen, or puts one root in two slots. A hidden allocation in the diff costs bytes at each mode change, and the `ModeDiffAllocationTests` gate of panel-sample then fails.

- **Reliability:** The diff gives the same tree for each path to a mode. A panel that both modes hold in the same slot stays with no join, so it keeps its style, its tweens, and its bindings. A panel that moves leaves and enters. A layout swap leaves no panel in the slots of the old layout. A factory exception leaves `Update`, and the next `Update` finishes the diff. A test pins each rule, and a plant fails each rule test.
- **API clarity:** A game creates one `UiManager`, registers its panels, layouts, and modes at build time, and calls `SetMode` with a UTF-8 literal. `ModeHistory.Back()` gives the back step. Each public member has an XML doc comment in ASD-STE100 prose. The remarks state the defer rule, the diff rules, the layout rule, the map rule, the history rule, the error rule, and the 0-byte rule.
- **0 bytes after warm-up:** `SetMode`, the diff (with a leave, an enter, a stay, a move, a layout swap, a map switch, and a back step), and an `Update` with no target allocate 0 bytes in the default Debug build. Two tests measure it, and four plants fail them.
- **One Core change:** `BrutalUi.Core` gets one slot, `UiContext.OnBeforeUpdate`, its delegate type, and its re-entry guard. No other Core member changes. Each existing allocation gate keeps 0 bytes. No project file changes.

---

## Executive Summary

This feature adds one slot to `BrutalUi.Core` and two types to `BrutalUi.Framework`:

- `UiContext.OnBeforeUpdate` (Core). `Update` calls it first, before the input pass. A write with `=` replaces the delegate, as for `OnUnhandledInput`. A call of `Update` from the slot throws `InvalidOperationException`. The delegate type is `UiBeforeUpdateHandler(float deltaTime)`.
- `UiManager` (Framework, `src/BrutalUi.Framework/Panels/`). It holds the registered panels, layouts, and modes, one `PanelController` for each panel, the current mode, and the target mode. Its constructor writes its handler into `OnBeforeUpdate`. `SetMode(name)` records the target and returns. The handler applies the diff at the start of the next `Update`: it unmounts each panel that leaves, swaps the layout root when the layout changes, mounts each panel of the new mode in order, and activates or deactivates the action map.
- `ModeHistory` (Framework). A fixed ring of the modes that each apply replaced. `Back()` sets the newest entry as the target and drops it. There is no forward step.

The plan has 4 phases and 25 tasks. Phase 1 (6 tasks) delivers the Core slot and the first mode on the screen. Phase 2 (7 tasks) delivers the mode change: the leave, the enter, the stay, the move, the layout swap, and the map switch. Phase 3 (4 tasks) delivers the history and the back step. Phase 4 (8 tasks in two groups) delivers the register checks, the error rule, the 0-byte tests, the `DefaultSlot` remark, and the docs. Phase 1 and phase 2 need an ADVANCED dev agent.

---

## User Personas Alignment

No `docs/user-personas.md` file exists. The personas come from `docs/mission-statement.md` and the epic docs, as in panel-definition and slot-container.

**Target Users:**
- [x] **The game developer** (registers the panels, the layouts, and the modes one time, and changes the screen with one `SetMode` call from a button, a key action, or game code)
- [x] **The sibling features** (panel-sample uses `UiManager`, `SetMode`, and `ModeHistory.Back()` in HelloPanel and measures the diff in `ModeDiffAllocationTests`. xml-markup later reads the same records from XML and registers them)
- [x] **The maintainer** (runs the new test classes through `scripts/test-linux.sh` with no script change)

**User Subsets Served:**
- [x] **Simple** - one layout with one slot, two modes, and one panel that leaves.
- [x] **Complex** - two layouts, three slots, a panel that stays, a panel that moves, a panel that only one mode holds, an action map for each mode, and a back step over several modes.

**UX Approach:**
- Build time: `var ui = new UiManager(new UiManager.CreateInfo { Context = context, Panels = panelContext });`, then `ui.Register(panel)`, `ui.Register(layout)`, and `ui.Register(mode)`.
- Frame time: `ui.SetMode("map"u8);` from a command. The game calls `context.Update(dt)` as before. The game writes no new line in the frame loop.
- No builder, no `IDisposable`, no event keyword, and no closure in the frame path.

---

## Goals

### Primary Goals
1. Add `UiContext.OnBeforeUpdate` with its delegate type and its re-entry guard (Q1 and A2 of the epic).
2. Add `UiManager` with `Register`, `SetMode`, `CurrentMode`, `TargetMode`, and the deferred diff (Q1, Q3, Q4, A4, A5 of the epic).
3. Add the layout swap and the action map switch (A4, A6 of the epic).
4. Add `ModeHistory` with `Back()` and no forward step (A3 of the epic).
5. Prove 0 bytes after warm-up for `SetMode`, the diff, and a frame with no target (A7 of the epic).
6. Settle the `DefaultSlot` follow-up of panel-definition, and state the rules in sections 4, 10, and 14 of the technical design and in the changelog.

### Success Criteria
- [ ] The phase 1 demo passes: `UiManagerTests` reads a total of 6 with `failed: 0`, and `BeforeUpdateTests` reads a total of 6 with `failed: 0`.
- [ ] The phase 2 demo passes: `ModeDiffTests` reads 7, `LayoutSwapTests` 5, and `ModeActionMapTests` 5, each with `failed: 0`.
- [ ] The phase 3 demo passes: `ModeHistoryTests` reads 9 with `failed: 0`.
- [ ] The phase 4 demo passes: `scripts/test-linux.sh` prints its PASS line, `UiManagerRegisterTests` reads 8, and `ModeDiffTests` reads 9, each with `failed: 0`.
- [ ] The two 0-byte tests measure 0 bytes in Debug, and the plants of task 4.4 fail them.
- [ ] Each existing test passes with no edit, and each existing allocation gate keeps 0 bytes.
- [ ] The only Core diff is in `src/BrutalUi.Core/Tree/UiContext.cs` and the new file `src/BrutalUi.Core/Tree/UiBeforeUpdateHandler.cs`.

---

## Architecture Overview

### The Place in the Epic

panel-definition gives the records and `PanelController.EnsureRoot()`. slot-container gives `SlotContainer.Mount`, `Unmount`, and `Find`. This feature joins them in `UiManager` and adds the one Core slot. panel-sample later puts two modes into HelloPanel and adds the gate `ModeDiffAllocationTests`.

```
Build time (allocates)
  new UiManager(info)              ──► context.OnBeforeUpdate = the handler of the manager
  ui.Register(panel)               ──► a PanelController (no root yet, Q3)
  ui.Register(layout)              ──► LayoutDefinition.CreateRoot(panels) ──► the layout root, kept
  ui.Register(mode)                ──► the layout entry, SlotContainer.Find for each slot, the controllers
Frame N (0 bytes)
  a command calls ui.SetMode("map"u8)  ──► the target. No tree change.
Frame N+1: UiContext.Update(dt) (0 bytes after warm-up)
  OnBeforeUpdate(dt)  ──► the diff (target != null only)
    1. mark the slot of each panel of the new mode
    2. leave: slot.Unmount(root) for each mounted panel with another slot or no slot
    3. layout: RemoveRoot(old layout root), AddRoot(new layout root), when the layout changes
    4. enter: slot.Mount(controller.EnsureRoot(), i) for each panel of the new mode, in order
    5. map: newMap.Activate(context), or oldMap.Deactivate() when the new mode has no map
    6. history: push the old mode (not for a back step). current = target. target = null.
  the input pass, the binding pass, the style pass, the transition pass, the layout pass (Core, no change)
```

### File Tree (end state)

```
src/BrutalUi.Core/Tree/
├── UiBeforeUpdateHandler.cs        # phase 1 (new): the delegate type
└── UiContext.cs                    # phase 1: OnBeforeUpdate, the guard flag, the call in Update, the remarks
src/BrutalUi.Framework/Panels/
├── UiManager.cs                    # phase 1 (new), phase 2, phase 3, phase 4
├── ModeHistory.cs                  # phase 3 (new)
└── PanelDefinition.cs              # phase 4: the remark of DefaultSlot only (Notes question 2)
tests/BrutalUi.Core.Tests/
└── BeforeUpdateTests.cs            # phase 1 (new): 6 tests
tests/BrutalUi.Framework.Tests/Panels/
├── UiManagerTests.cs               # phase 1 (new): 6 tests
├── ModeDiffTests.cs                # phase 2 (new): 7 tests. Phase 4 adds the 2 0-byte tests
├── LayoutSwapTests.cs              # phase 2 (new): 5 tests
├── ModeActionMapTests.cs           # phase 2 (new): 5 tests
├── ModeHistoryTests.cs             # phase 3 (new): 9 tests
└── UiManagerRegisterTests.cs       # phase 4 (new): 8 tests
docs/technical-design.md            # phase 4: sections 4, 10, and 14
CHANGELOG.md                        # phase 4: lines under Added and Changed
docs/features/panels-and-modes/mode-set/context.md   # each phase
```

These files do not change: each other file of `src/BrutalUi.Core/`, each other file of `src/BrutalUi.Framework/` (also `SlotContainer.cs`, `PanelController.cs`, `ModeDefinition.cs`, and `UiActionMap.cs`), each `.csproj`, `Directory.Build.props`, `Directory.Packages.props`, `brutal-ui.sln`, `scripts/`, `samples/`, and each existing test file. The new test files compile with no project edit. The Framework files use the namespace `BrutalUi.Framework`. The Framework test files use `BrutalUi.Framework.Tests`, and the Core test file uses `BrutalUi.Core.Tests`.

### Public API (end state)

```csharp
namespace BrutalUi.Core;

public delegate void UiBeforeUpdateHandler(float deltaTime);

public sealed class UiContext
{
    public UiBeforeUpdateHandler? OnBeforeUpdate { get; set; }   // Update calls it first. Set it with =.
}

namespace BrutalUi.Framework;

public sealed class UiManager
{
    public const int DefaultHistoryCapacity = 16;

    public struct CreateInfo
    {
        public required UiContext Context;
        public required PanelContext Panels;
        public int HistoryCapacity;                       // 0 gives DefaultHistoryCapacity
    }

    public UiManager(in CreateInfo info);                 // writes context.OnBeforeUpdate
    public UiContext Context { get; }
    public PanelContext Panels { get; }
    public ModeHistory History { get; }
    public ModeDefinition? CurrentMode { get; }           // null before the first apply
    public ModeDefinition? TargetMode { get; }            // null when no change waits
    public PanelController Register(PanelDefinition panel);
    public void Register(LayoutDefinition layout);        // calls the factory of the layout one time
    public void Register(ModeDefinition mode);            // register its layout and its panels first
    public void SetMode(ReadOnlySpan<byte> name);         // ArgumentException for an unknown name
}

public sealed class ModeHistory
{
    public int Count { get; }
    public int Capacity { get; }
    public bool Back();                                   // false for an empty history
    public void Clear();
}
```

### Internal Shape

- **The Core slot (Decision 2).** `UiContext` gets `public UiBeforeUpdateHandler? OnBeforeUpdate { get; set; }` and `private bool _beforeUpdate;`. `Update` keeps its check of `_binding` first. It then checks `_beforeUpdate` and throws `InvalidOperationException("A before-update handler must not call Update.")`. It then reads the slot into a local. When the local is not `null`, it sets `_beforeUpdate = true`, calls the handler with `deltaTime` in a `try` block, and clears the flag in the `finally` block. `DrainEvents()` comes after the call. `Update` reads `_roots` after the drain and the binding pass already, so the passes see the new roots. An exception of the handler leaves `Update` before the input pass, and the ring keeps its entries. A `null` slot costs one field read and one compare.
- **The tables (Decision 4).** `UiManager` holds three arrays that grow by doubling at register time: `PanelEntry[]` (the controller, the slot that holds the root now, and the slot of the next mode), `LayoutEntry[]` (the definition and the built root), and `ModeEntry[]` (the definition, the layout entry, and a `Placement[]`). A `Placement` is a struct: the panel entry, the cached `SlotContainer`, and the index in the slot. The placements come in the slot order of the mode, and in the panel order inside a slot. The manager also holds `_current`, `_target`, `_shown` (the layout entry in the context), `_targetFromBack`, and `_applying`.
- **Register (Decision 4 and Decision 5).** `Register(PanelDefinition)` creates the controller with the `PanelContext` and returns it. `Register(LayoutDefinition)` calls `CreateRoot(Panels)` one time and keeps the root (Notes question 3). `Register(ModeDefinition)` finds the layout entry by name, calls `SlotContainer.Find(layoutRoot, slotName)` for each slot, finds the panel entry of each name, and builds the placements. Each name compare uses `SequenceEqual` on the spans. Phase 1 throws `ArgumentException` for a name that it cannot resolve. Phase 4 adds the null checks, the duplicate names, the check of the layout root, and the duplicate slot names.
- **SetMode (Decision 3).** `SetMode` throws `InvalidOperationException` while the diff runs (phase 4). It scans the modes for the name. An unknown name throws `ArgumentException` with `ParamName` `name` before a change. A hit sets `_target` and clears `_targetFromBack`. A second call replaces the target, so the last call wins.
- **The handler.** One delegate that the constructor creates: `private void OnBeforeUpdate(float deltaTime)`. It returns at once when `_target` is `null` (a frame with no target). Else it calls `Apply()`.
- **Apply (Decision 6, Decision 7, Decision 8).**
  ```
  if (target == current) { target = null; targetFromBack = false; return; }   // no diff, no history entry
  applying = true; try {
    for each panel entry e: e.Next = null
    for each placement p of target: p.Panel.Next = p.Slot                       // 1. mark
    for each panel entry e: if (e.Slot is { } s && s != e.Next) { s.Unmount(root of e); e.Slot = null; }   // 2. leave
    if (shown != target.Layout) { if (shown != null) Context.RemoveRoot(shown.Root);                    // 3. layout
                                  Context.AddRoot(target.Layout.Root); shown = target.Layout; }
    for each placement p of target in order: p.Slot.Mount(p.Panel.Controller.EnsureRoot(), p.Index);   // 4. enter
                                             p.Panel.Slot = p.Slot;
    if (target.ActionMap is { } map) map.Activate(Context);                                            // 5. map
    else current?.ActionMap?.Deactivate();
    if (current != null && !targetFromBack) History.Push(current);                                     // 6. history
    current = target; target = null; targetFromBack = false;
  } finally { applying = false; }
  ```
  The leave pass reads the real slot of each panel, not the placements of the old mode. Thus a diff after a failed diff removes each stale panel (Decision 8). A held root in its slot moves with no join. A new root inserts at its index. The loop over the indexes of a slot gives the order of the mode (the contract of slot-container). The diff allocates nothing after each root, each slot child array, and the history ring hold their largest size.
- **ModeHistory (Decision 9).** A ring of `int` mode indexes with a fixed capacity. `internal Push(int)` writes the newest entry and drops the oldest entry when the ring is full. `Back()` throws `InvalidOperationException` while the diff runs, returns `false` for an empty ring, and else pops the newest entry and calls an internal method of the manager that sets the target and sets `_targetFromBack`. `Clear()` sets the count to 0. The constructor is internal, and the manager creates the one instance.
- **The messages.** Each message is a `private const string`, as in `CommandRegistry` and the panel records.
- **The thread rule.** The manager and the history run on the UI thread and have no thread guard, as `UiActionMap` has none.

### Integration Points

| Sibling or caller | What this feature calls or gives | Contract |
|---|---|---|
| core-tree (`UiContext`) | `OnBeforeUpdate` (new), `AddRoot`, `RemoveRoot`, `Update` | The slot runs first in `Update`, so the input pass, the binding pass, the style pass, and the layout pass see the new tree in the same `Update`. The manager adds and removes only its layout roots. A root that the game adds stays. |
| panel-definition (done) | `PanelController`, `PanelController.EnsureRoot()`, the getters of `ModeDefinition`, `LayoutDefinition.CreateRoot`, `PanelDefinition.Name` | The manager creates one controller for each panel. `EnsureRoot` runs at the first mount only (Q3). The remark of `DefaultSlot` changes (Notes question 2). |
| slot-container (done) | `SlotContainer.Find`, `Mount`, `Unmount` | `Find` runs at register time, and the manager caches each result. The leave pass unmounts first, also a root that moves. Then `Mount(root_i, i)` runs in the order of the mode. |
| state-and-commands (done) | `UiActionMap.Activate(context)`, `Deactivate()` | The diff activates the map of the new mode. It deactivates the old map when the new mode has none (A6). Both calls allocate nothing. |
| panel-sample (next) | `UiManager`, `SetMode`, `CurrentMode`, `ModeHistory.Back()` | The sample registers two modes and switches them from a command and a key action. Its gate `ModeDiffAllocationTests` measures `SetMode` plus one `Update`. |
| xml-markup (later epic) | `Register` | A loader builds the records from XML and registers them in the same order rule: the layouts and the panels before the modes. |

### Data Flow

```
Build time:
  ui.Register(inventory) ──► controller (unbuilt)
  ui.Register(main)      ──► mainRoot = main.CreateRoot(panels): Element ─ SlotContainer "left", SlotContainer "right"
  ui.Register(explore)   ──► placements: (inventory, left, 0), (stats, left, 1), (map, right, 0)
  ui.SetMode("explore"u8); context.Update(0f)
      ──► AddRoot(mainRoot), Mount(inventoryRoot, 0), Mount(statsRoot, 1), Mount(mapRoot, 0) (each root built one time)

A change (0 bytes after warm-up):
  ui.SetMode("trade"u8)          trade: left [shop, inventory], right [stats]
  context.Update(dt) ──► the slot:
      leave: left.Unmount(statsRoot) (it moves), right.Unmount(mapRoot) (it leaves)
      enter: left.Mount(shopRoot, 0), left.Mount(inventoryRoot, 1) (a move, no join), right.Mount(statsRoot, 0)
      map:   trade.ActionMap.Activate(context)
      history: push explore
  ──► the input pass, the binding pass, the style pass (shopRoot and statsRoot join), the layout pass

A back step:
  ui.History.Back() ──► target explore, no history entry at the apply
```

---

## Implementation Phases

The feature has 25 tasks, above the "about 8 tasks" of Slice rule 7, so it has four slices. Section 11 of `.beast-mode/phase-handoff.md` holds the rules. Phase 1 is the smallest path that proves the idea: a `SetMode` call changes nothing at once, and the next `Update` shows the mode. Phase 2 adds the mode change. Phase 3 adds the back step. Phase 4 adds the checks, the error rule, the 0-byte proof, and the docs.

Each verification command sets `MSBUILDDISABLENODEREUSE=1` and gives `-nodeReuse:false` to `dotnet build`, because stale MSBuild nodes on this machine hang the build. Do not run `dotnet build-server shutdown`. Each task group ends with `MSBUILDDISABLENODEREUSE=1 dotnet build brutal-ui.sln -nodeReuse:false 2>&1 | tail -n 30` and `MSBUILDDISABLENODEREUSE=1 dotnet test --solution brutal-ui.sln 2>&1 | tail -n 30` (tool rule 5). Expect `0 Warning(s)`, `0 Error(s)`, and 0 failed. A warning fails the build (`TreatWarningsAsErrors`). Each public member needs an XML doc comment (CS1591 is an error). The projects have no implicit usings, so each new file holds its own `using` lines, also for each namespace that a `cref` names. Use file-scoped namespaces. Prose in comments follows ASD-STE100. Run one Framework class with `tests/BrutalUi.Framework.Tests/bin/Debug/net10.0/BrutalUi.Framework.Tests --filter-class '*<Class>' 2>&1 | tail -n 5`, and one Core class with `tests/BrutalUi.Core.Tests/bin/Debug/net10.0/BrutalUi.Core.Tests --filter-class '*<Class>' 2>&1 | tail -n 5`, after a build.

Two flakes are known. A test host can exit with 134 (Internal CLR error 0x80131506), and the Core test `TheCurvesAllocateNothing` failed one time with 8128 bytes. Run the tests a second time before you treat either as a fault. A plant that does not compile leaves the old test binary, and the old binary passes. Check `0 Error(s)` before you read the result of a plant. For each plant, copy the file to the scratchpad first, run `MSBUILDDISABLENODEREUSE=1 dotnet test --solution brutal-ui.sln > /dev/null 2>&1; echo "exit=$?"`, expect a value that is not 0, restore the file, check it with `cmp`, and run the tests again.

The test files use unique names for each test (classes with `ClassName.Intern(nameof(TestName) + ".x")`, and panel, layout, and mode names in UTF-8 literals), because xUnit runs the classes in parallel and the class table is global. Each test builds its own `UiContext` with a `RecordingRenderer` (linked from `BrutalUi.Core.Tests/Shared/`) and a viewport of 400 by 300, and its own `PanelContext` with a new `CommandRegistry`, `EventBus`, and `ServiceRegistry`. A panel factory in a test is a method of a small counter class that records each root it creates (the shape of `PanelControllerTests`). To read the tree between the diff and the passes, a test calls `context.OnBeforeUpdate!(0f)` directly, then `context.Update(0f)`.

### Phase 1: The Core Slot and the First Mode on the Screen
**Goal:** `UiContext.Update` calls `OnBeforeUpdate` first, and a call of `Update` from the slot throws. A game registers panels, a layout, and a mode in a `UiManager`, and calls `SetMode`. The call changes no tree. The next `Update` adds the layout root and mounts each panel in the order of the mode, and the same `Update` styles and lays out the panels.
**Demo:** a developer runs `tests/BrutalUi.Framework.Tests/bin/Debug/net10.0/BrutalUi.Framework.Tests --filter-class '*UiManagerTests'` after a build and reads a total of 6 with `failed: 0`.
**Needs:** none
**Agent tier:** ADVANCED (opus). The phase changes `UiContext.Update`, the frame loop of each game, and sets the hook of the diff.

**Tasks (one group):**
- [ ] 1.1 Fill in `docs/features/panels-and-modes/mode-set/context.md` (create it when it does not exist). Check that `SlotContainer.cs` and the six panel-definition files exist in `src/BrutalUi.Framework/Panels/`. Record the base commit (`git rev-parse HEAD`), the test count of the solution (with the skipped count), the count of `BrutalUi.Framework.Tests`, and the count of `BrutalUi.Core.Tests`. At planning time, the solution read 2773 (3 skipped) and `BrutalUi.Framework.Tests` read 180. Record the Internal Shape and the Integration Points of this plan.
- [ ] 1.2 Write `src/BrutalUi.Core/Tree/UiBeforeUpdateHandler.cs` with `public delegate void UiBeforeUpdateHandler(float deltaTime);`. The doc comment: `UiContext.Update` calls it first, before the input pass, with the `deltaTime` of the call. The handler can add and remove roots and change the tree, and the passes of the same `Update` see the change. A call of `Update` from the handler throws `InvalidOperationException`. An exception of the handler leaves `Update` before the input pass, the ring keeps its entries, and the next `Update` calls the handler again. Create the delegate one time and set the slot at build time.
- [ ] 1.3 Edit `src/BrutalUi.Core/Tree/UiContext.cs`. Add the property `OnBeforeUpdate`, the field `_beforeUpdate`, and the guard and the call of the Internal Shape. Put the property after `OnUnhandledInput`. Its remarks: the seam of the mode diff of the framework layer (Q1 and A2 of panels-and-modes). Core names no type of that layer. A write with `=` replaces the delegate. Do not use `+=`. `UiManager` writes the slot in its constructor. A `null` slot does nothing. Add one paragraph to the remarks of `Update` before the paragraph of the input pass: the slot runs first. The hit test of the input pass reads the layout of the last `Update`, so a root that the slot adds gets no hit until its first layout. Add one `<exception cref="InvalidOperationException">` tag: the before-update slot calls this method. Change no other member.
- [ ] 1.4 Write `tests/BrutalUi.Core.Tests/BeforeUpdateTests.cs` with 6 tests. Tests: `UpdateCallsTheSlotFirstWithTheDeltaTime` (the slot records `deltaTime` 0.25, `QueuedEventCount` 1 for a pushed key, and an order counter. A binding with a fake `IBindingSource` records the counter after the slot. The slot reads the ring before the drain and runs before the binding pass), `ARootThatTheSlotAddsGetsItsStyleAndLayoutInTheSameUpdate` (the slot adds a root of 100 by 50 with a class rule in a sheet. After one `Update`, `Roots` holds it, its `Layout.Rect` width is 100, and the class value is on it), `AnUpdateFromTheSlotThrowsAndTheContextStaysValid` (the slot calls `Update` only at its first call, so a missing guard gives a failed assert and no stack overflow. The outer `Update` throws `InvalidOperationException`. After the slot is set to `null`, the next `Update` drains the ring and works), `AnExceptionOfTheSlotLeavesUpdateBeforeTheInputPass` (the slot throws one time. `QueuedEventCount` stays 1, and a layout change of a root stays unapplied. The next `Update` calls the slot, drains the ring, and lays out the root), `AWriteReplacesTheDelegateInTheSlot` (set slot A, then slot B. `Update` calls B only. A `null` slot calls nothing), and `AnUpdateWithASlotAllocatesNothingAfterWarmUp` (a static handler in a `static readonly` field adds to a static counter. 2 warm-up rounds, `AllocationGate.ReleaseAllocationContext()` right before the measure, 100 calls of `Update(0f)`, `Assert.Equal(0L, after - before)`, and the counter after the measure).
- [ ] 1.5 Write `src/BrutalUi.Framework/Panels/UiManager.cs` with `CreateInfo`, the constructor, `Context`, `Panels`, `CurrentMode`, `TargetMode`, the three `Register` overloads, `SetMode`, the handler, and `Apply` for the first mode only: the mark, the layout add, the enter pass, and the end of the apply. Do not add `History`, the leave pass, `RemoveRoot`, or the map switch yet. The constructor throws `ArgumentNullException` with the parameter `info` for a `null` `Context` or `Panels` (the shape of `UiActionMap`), creates the handler delegate one time, and writes it into `Context.OnBeforeUpdate`. `Register` throws `ArgumentException` with a constant message for a layout, a slot, or a panel name of a mode that it cannot resolve. The class remarks: the defer rule (Q1. `SetMode` records the target, and the next `Update` applies it before the input pass, so a `SetMode` from a command applies at the start of the next `Update`), the register order (the layouts and the panels before the modes), the slot rule (the constructor writes `OnBeforeUpdate` and replaces a delegate of the game), the root rule (the manager adds and removes its layout roots only. The game does not add a layout root itself), the layout-root rule of the action map (the layout factory sets `IsPickable` to `false` on a root that fills the window), and the thread rule.
- [ ] 1.6 Write `tests/BrutalUi.Framework.Tests/Panels/UiManagerTests.cs` with 6 tests. Tests: `TheConstructorWritesTheBeforeUpdateSlot` (the slot is not `null` after the constructor. A create-info with a `null` `Context` or `Panels` throws `ArgumentNullException`), `SetModeRecordsTheTargetAndChangesNoTree` (`TargetMode` is the mode, `CurrentMode` is `null`, `Roots` is empty, and no factory ran), `TheNextUpdateAddsTheLayoutRootAndMountsEachPanelInTheOrderOfTheMode` (a layout with the slots `left` and `right`, and a mode with the entries `left a`, `right c`, `left b`. After one `Update`, `Roots` holds only the layout root, `left` holds `a` and `b` in order, `right` holds `c`, each root is `controller.Instance.Root` of the controller that `Register` returned, each root has a `Layout.Rect` height above 0, `CurrentMode` is the mode, and `TargetMode` is `null`), `TwoSetModeCallsInOneFrameApplyTheLast` (two modes on one layout. `SetMode(first)`, `SetMode(second)`, and `Update` give the second mode, and the factory of a panel that only the first mode holds never ran), `AFrameWithNoTargetChangesNothing` (a second `Update` keeps the roots, the children, the current mode, and each factory count at 1), and `SetModeRejectsAnUnknownName` (`ArgumentException` with `ParamName` `name` for `missing` and for an empty name. A target that an earlier call set stays). Then run the build, the tests, `UiManagerTests` (total 6), and `BeforeUpdateTests` (total 6). Expect `failed: 0`, and each other test passes with no edit. Run three plants. Plant 1: remove the `_beforeUpdate` check in `Update`. Expect `AnUpdateFromTheSlotThrowsAndTheContextStaysValid` to fail. Plant 2: move the slot call after `DrainEvents()`. Expect `UpdateCallsTheSlotFirstWithTheDeltaTime` to fail. Plant 3: call `Apply()` at the end of `SetMode`. Expect `SetModeRecordsTheTargetAndChangesNoTree` to fail. Record the demo, the counts, and the results in `context.md`.

**Estimated Steps:** about 45 (6 tasks, about seven to eight steps for one task)

**Files (one group, 6):** `UiBeforeUpdateHandler.cs`, `UiContext.cs`, `BeforeUpdateTests.cs`, `UiManager.cs`, `UiManagerTests.cs`, `context.md`

**Acceptance Criteria:**
- [ ] The 6 tests of `BeforeUpdateTests` and the 6 tests of `UiManagerTests` pass. Each existing test passes with no edit, and each existing allocation gate passes.
- [ ] The three plants of task 1.6 give a non-zero exit code, and each restored file equals its copy.
- [ ] The Core diff holds only `UiContext.cs` and the new `UiBeforeUpdateHandler.cs`. No project file changes.

### Phase 2: The Mode Change
**Goal:** A change from one mode to another unmounts each panel that leaves, mounts each panel that enters with a root that its controller builds one time, keeps each panel that stays in its slot with no join, and moves a panel to a new slot with a leave and an enter. Each slot takes the order of the new mode. A mode with another layout swaps the context root and leaves no panel in the old slots. The diff activates the map of the new mode, or deactivates the old map.
**Demo:** a developer runs the built Framework test binary with `--filter-class '*ModeDiffTests'`, `'*LayoutSwapTests'`, and `'*ModeActionMapTests'` and reads the totals 7, 5, and 5, each with `failed: 0`.
**Needs:** Phase 1
**Agent tier:** ADVANCED (opus). The phase holds the diff, the layout swap, and the map switch, and it touches three integration points of Core, slot-container, and state-and-commands.

**Tasks (one group):**
- [ ] 2.1 Edit `src/BrutalUi.Framework/Panels/UiManager.cs`. Add the `Slot` and `Next` fields of each panel entry, the mark pass, and the leave pass of the Internal Shape. The enter pass sets `Slot` of each panel. The remarks of the class get the diff rules: a panel that only the old mode holds unmounts and keeps its root, its state, and its bindings. A panel that only the new mode holds mounts, and its controller builds the root at the first mount (Q3). A panel in the same slot element stays with no join (A5). A panel that moves to another slot leaves and enters, and it joins the new slot. Each slot takes the order of the new mode (A4). The stale rules of Core clear the hover and the focus of a leaving panel in the input pass of the same `Update`. A target that equals the current mode changes nothing.
- [ ] 2.2 Add the layout step of the Internal Shape: the field `_shown`, `Context.RemoveRoot` of the old layout root, and `Context.AddRoot` of the new layout root. The remarks: the layout root is the one root of the manager in the context (A4). A layout swap unmounts each panel from the slots of the old layout, because the new slots are other elements. A layout root lives for the life of the manager, and a swap back adds the same root again. `AddRoot` marks the full layout subtree for the style pass (the join rule of Core), so a swap restyles the layout.
- [ ] 2.3 Add the map step of the Internal Shape. The remarks: the diff calls `Activate` on the map of the new mode, also when the old mode holds the same map. It calls `Deactivate` on the old map when the new mode has no map (A6). Two modes with no map leave `OnUnhandledInput` as the game set it. The map switch comes before the input pass, so a key in the frame of the change reaches the map of the new mode.
- [ ] 2.4 Write `tests/BrutalUi.Framework.Tests/Panels/ModeDiffTests.cs` with 7 tests on one layout with the slots `left` and `right`. Tests: `APanelThatOnlyTheOldModeHoldsUnmountsAndKeepsItsState` (after the change, the root has no parent and keeps its children, its class, and its inline width. `controller.Instance.Root` is the same root), `APanelThatOnlyTheNewModeHoldsMountsAndItsRootIsBuiltOneTime` (the modes A, B, A, B. The factory of the panel of B ran one time, and the root is the same instance at each mount), `APanelInBothModesInTheSameSlotStaysWithNoJoin` (after a settled `Update`, the test calls `SetMode` and the slot directly. The staying root has the same parent, and its `Dirty` holds no `DirtyFlags.Style` bit. A root that enters holds the bit), `APanelThatMovesToAnotherSlotLeavesAndEnters` (after the slot call, `left` does not hold the root, `right` holds it, and it holds `DirtyFlags.Style`), `EachSlotTakesTheOrderOfTheNewMode` (`left [x, y, z]` to `left [z, w, x]` gives `z`, `w`, `x`. `y` has no parent), `AnUnmountedPanelKeepsItsBindingAndShowsTheNewValueWhenItReturns` (a `Store<int>` and a `Label` with `LabelBindings.BindText` in a panel that only mode A holds. A, `Update`, B, `Set(2)`, `Update`, A, `Update`: the label reads `2`, and `BindingCount` stays 1), and `ASetModeFromACommandAppliesAtTheStartOfTheNextUpdate` (a `Button` with `CommandBindings.BindCommand` in a panel. A command handler calls `SetMode`. A pushed click and `Update` keep the old mode with the new mode as the target. The next `Update` applies the new mode).
- [ ] 2.5 Write `tests/BrutalUi.Framework.Tests/Panels/LayoutSwapTests.cs` with 5 tests. Two layouts: `wide` with the slots `left` and `right`, and `tall` with the slot `main`. Tests: `AModeWithAnotherLayoutSwapsTheContextRoot` (after the change, `Roots` holds only the `tall` root), `TheSlotsOfTheOldLayoutHoldNoPanelAfterASwap` (`left` and `right` have `Count` 0. A panel in both modes is the same root, now in `main`), `ASwapBackReusesTheFirstLayoutRoot` (the modes on `wide`, `tall`, `wide`. Each layout factory ran one time, `Roots` holds the first `wide` root, and the panels are back in their slots), `TheNewLayoutIsStyledAndLaidOutInTheSameUpdate` (a type rule for `SlotContainer` sets a background color. After the `Update` of the change, `main` holds the color and each mounted root has a `Layout.Rect` height above 0), and `TheManagerKeepsTheRootsOfTheGame` (a root that the game added before the manager stays in `Roots` after two swaps, in the first position).
- [ ] 2.6 Write `tests/BrutalUi.Framework.Tests/Panels/ModeActionMapTests.cs` with 5 tests. Each map binds Escape (256) to a counted command. Tests: `AModeWithAMapActivatesIt` (`IsActive` is `true`, and a pushed Escape runs the command), `AModeWithAnotherMapReplacesTheOldMap` (the old map reads `IsActive` `false`, and Escape runs the command of the new map only), `AModeWithNoMapDeactivatesTheOldMap` (`OnUnhandledInput` is `null` after the change), `TwoModesWithNoMapKeepTheSlotOfTheGame` (a game delegate in `OnUnhandledInput` stays after a change between two modes with no map), and `AKeyInTheFrameOfTheChangeReachesTheNewMap` (`SetMode`, a pushed Escape, and one `Update`: the command of the new map runs and the old one does not).
- [ ] 2.7 Run the build, the tests, `ModeDiffTests` (total 7), `LayoutSwapTests` (total 5), and `ModeActionMapTests` (total 5). Expect `failed: 0`, and the 12 tests of phase 1 still pass. Run four plants on `UiManager.cs`. Plant 4: remove the `Unmount` call of the leave pass. Expect `APanelThatOnlyTheOldModeHoldsUnmountsAndKeepsItsState` to fail (the moves also throw from `Mount`). Plant 5: unmount each mounted panel in the leave pass, also a panel with the same slot. Expect `APanelInBothModesInTheSameSlotStaysWithNoJoin` to fail. Plant 6: remove the `RemoveRoot` call. Expect `AModeWithAnotherLayoutSwapsTheContextRoot` to fail. Plant 7: remove the `Deactivate` branch. Expect `AModeWithNoMapDeactivatesTheOldMap` to fail. Record the demo, the failed tests of each plant, and the results in `context.md`.

**Estimated Steps:** about 50 (7 tasks, about seven steps for one task)

**Files (one group, 5):** `UiManager.cs`, `ModeDiffTests.cs`, `LayoutSwapTests.cs`, `ModeActionMapTests.cs`, `context.md`

**Acceptance Criteria:**
- [ ] The 17 new tests pass. The 12 tests of phase 1 still pass. Each existing test passes with no edit.
- [ ] The four plants of task 2.7 give a non-zero exit code, and the restored file equals its copy.
- [ ] No file of `src/BrutalUi.Core/` changes in this phase.

### Phase 3: The Mode History and the Back Step
**Goal:** Each apply that replaces a mode records the old mode in `ModeHistory`. `ModeHistory.Back()` sets the newest entry as the target and drops it, and the apply of a back step adds no entry, so there is no forward step. `Back()` on an empty history returns `false`. A full history drops its oldest entry.
**Demo:** a developer runs the built Framework test binary with `--filter-class '*ModeHistoryTests'` and reads a total of 9 with `failed: 0`.
**Needs:** Phase 2
**Agent tier:** STANDARD (sonnet).

**Tasks (one group):**
- [ ] 3.1 Write `src/BrutalUi.Framework/Panels/ModeHistory.cs` with the public members of the Public API and the ring of the Internal Shape. The constructor is internal and takes the manager and the capacity. The remarks: the history holds the mode that each apply replaced. The first apply adds nothing. A target that equals the current mode adds nothing. `Back()` sets the newest entry as the target of the manager, drops it, and returns `true`. The next `Update` applies it, and that apply adds no entry (A3, no forward step). A `SetMode` after `Back()` in the same frame wins, and the dropped entry stays dropped. A full ring drops the oldest entry. `Back()`, the push, and `Clear()` allocate nothing. The history runs on the UI thread.
- [ ] 3.2 Edit `src/BrutalUi.Framework/Panels/UiManager.cs`. Add `DefaultHistoryCapacity` (16), `CreateInfo.HistoryCapacity` (0 gives the default, a value below 0 throws `ArgumentOutOfRangeException` with the parameter `info`), the `History` property, the internal method that `Back()` calls, and step 6 of `Apply`. `SetMode` clears `_targetFromBack`.
- [ ] 3.3 Write `tests/BrutalUi.Framework.Tests/Panels/ModeHistoryTests.cs` with 9 tests on four modes A, B, C, and D of one layout. Tests: `EachApplyAfterTheFirstAddsTheOldMode` (A, B, C give `Count` 2. The first apply gives 0), `BackSetsThePreviousModeAsTheTargetAndDropsIt` (after C, `Back()` gives `true`, `TargetMode` B, and `Count` 1. `CurrentMode` stays C until the next `Update`), `BackWalksToTheFirstModeAndThenReturnsFalse` (two back steps with an `Update` after each give B, then A. A third `Back()` gives `false`), `BackOnAnEmptyHistoryReturnsFalseAndKeepsTheTarget` (a target that `SetMode` set stays), `TheApplyOfABackStepAddsNoEntry` (after `Back()` and `Update`, `Count` is one less than before and stays so), `ASetModeOfTheCurrentModeChangesNothing` (`Count` stays, and no factory runs), `AFullHistoryDropsTheOldestMode` (capacity 2. A, B, C, D give `Count` 2. The back steps give C, then B, then `false`), `ASetModeAfterBackInOneFrameWins` (C with the history A, B. `Back()`, then `SetMode(A)`, then `Update` give A. The history holds A and C: B stays dropped, and the apply pushes C, because it is not a back step), and `TheCapacityComesFromTheCreateInfoAndClearEmptiesTheHistory` (0 gives 16, 3 gives 3, -1 throws `ArgumentOutOfRangeException`. `Clear()` gives `Count` 0, and `Back()` then gives `false`).
- [ ] 3.4 Run the build, the tests, and `ModeHistoryTests` (total 9). Expect `failed: 0`, and the 29 tests of phases 1 and 2 still pass. Run two plants. Plant 8: push the old mode also for a back step. Expect `TheApplyOfABackStepAddsNoEntry` to fail. Plant 9: when the ring is full, overwrite the newest entry in place of the oldest. Expect `AFullHistoryDropsTheOldestMode` to fail. Record the demo and the results in `context.md`.

**Estimated Steps:** about 25 (4 tasks, about six steps for one task)

**Files (one group, 4):** `ModeHistory.cs`, `UiManager.cs`, `ModeHistoryTests.cs`, `context.md`

**Acceptance Criteria:**
- [ ] The 9 tests of `ModeHistoryTests` pass. The 29 tests of phases 1 and 2 still pass. Each existing test passes with no edit.
- [ ] Both plants of task 3.4 give a non-zero exit code, and the restored files equal their copies.

### Phase 4: The Register Checks, the Error Rule, the 0-Byte Proof, and the Docs
**Goal:** `Register` rejects each bad input before a change. A `SetMode`, a `Back()`, or a `Register` from inside the diff throws. A factory exception leaves `Update`, and the next `Update` finishes the diff with no stale panel. `SetMode`, the diff, and a frame with no target allocate 0 bytes after warm-up. The remark of `DefaultSlot`, the technical design, and the changelog state the rules.
**Demo:** a developer runs `MSBUILDDISABLENODEREUSE=1 scripts/test-linux.sh` and reads its PASS line, and the built Framework test binary reads a total of 8 for `--filter-class '*UiManagerRegisterTests'` and 9 for `--filter-class '*ModeDiffTests'`, each with `failed: 0`.
**Needs:** Phase 3
**Agent tier:** STANDARD (sonnet). The diff and the layout swap are done. This phase adds checks, a guard flag, tests, and docs.

**Tasks (group 1, the code and the tests):**
- [ ] 4.1 Edit `src/BrutalUi.Framework/Panels/UiManager.cs`. Each check comes before a change, with a constant message. Each `Register` overload throws `ArgumentNullException` for a `null` definition and `ArgumentException` for a name that the manager holds. `Register(LayoutDefinition)` throws `InvalidOperationException` for a factory that returns `null` or an element with a parent, and `ArgumentException` for a layout root with two slots of one name. The duplicate check walks the layout root in depth-first pre-order, does not enter a slot, and checks that `SlotContainer.Find(root, slot.SlotName)` gives the same slot. A factory exception leaves `Register` with no wrap. `Register(ModeDefinition)` keeps the `ArgumentException` of phase 1 for an unknown layout, slot, or panel. `SetMode`, `ModeHistory.Back()`, and each `Register` throw `InvalidOperationException` while `_applying` is `true`. The remarks get the error rule: an exception of a factory or of `Mount` leaves `Update` before the input pass. The target stays, and the next `Update` runs the diff again. The leave pass reads the real slot of each panel, so the next diff, also to another target, leaves no stale panel. Each remark names the allocation rule: `Register` allocates, and `SetMode`, the diff, and a frame with no target allocate 0 bytes after warm-up.
- [ ] 4.2 Write `tests/BrutalUi.Framework.Tests/Panels/UiManagerRegisterTests.cs` with 8 tests. Tests: `RegisterRejectsANullDefinition` (each overload, `ArgumentNullException`), `RegisterRejectsADuplicateName` (a panel, a layout, and a mode. The first record stays), `RegisterOfALayoutBuildsItsRootOneTimeAndChecksIt` (the factory runs one time at the register. A factory that returns `null` and a factory that returns an element with a parent throw `InvalidOperationException`, and the name stays free for a later register), `RegisterOfALayoutRejectsTwoSlotsWithOneName` (two slots `left` at different depths), `RegisterOfAModeRejectsAnUnknownLayoutPanelOrSlot` (three cases, `ArgumentException`. `SetMode` of the name then throws `ArgumentException`), `SetModeBackAndRegisterFromTheDiffThrow` (a panel factory calls each of the three, catches each exception, records its type, and returns a root. Each record is `InvalidOperationException`, and the diff ends), `AFactoryExceptionLeavesUpdateAndTheNextUpdateFinishesTheDiff` (a panel factory throws at its first call. `Update` throws the factory exception, `CurrentMode` stays the old mode, and `TargetMode` stays. The next `Update` gives the new mode with each slot in the order of the mode), and `ANewTargetAfterAFailedDiffShowsOnlyItsOwnPanels` (a diff to B fails after it mounts one panel of B. `SetMode(C)` and `Update` give the panels of C only, and the panel of B has no parent).
- [ ] 4.3 Add two 0-byte tests to `tests/BrutalUi.Framework.Tests/Panels/ModeDiffTests.cs`. The scene, built before the measure: a context with a `RecordingRenderer`, the layouts `wide` (`left`, `right`) and `tall` (`main`), four panels `p1` to `p4`, two maps with one action each, and three modes: A on `wide` (`left [p1, p2]`, `right [p3]`, map 1), B on `wide` (`left [p2, p4]`, `right [p1]`, map 2), and C on `tall` (`main [p2]`, no map). Thus a step holds a leave, an enter, a stay with a new index, a move, a layout swap, a map switch, a deactivate, and a back step. `SetModeAndTheDiffAllocateNothingAfterWarmUp`: a private static step method runs `SetMode(B)`, `Update(0f)`, `SetMode(C)`, `Update(0f)`, `History.Back()`, `Update(0f)` (back to B), `SetMode(A)`, and `Update(0f)`. It adds the child count of each slot, one for each `ReferenceEquals` of `CurrentMode` with the expected mode, and one for each active map to a local `int`, and returns it. `AFrameWithNoTargetAllocatesNothing` runs `Update(0f)` with no target and adds the child counts. Each test runs 2 warm-up rounds, calls `AllocationGate.ReleaseAllocationContext()` right before the measure, runs 100 iterations, and asserts `Assert.Equal(0L, after - before)` and a fixed sum after the measure. The ring of the history fills during the measure, and a full ring allocates nothing. The measured body creates no lambda, no string, no array, and no `Assert`.
- [ ] 4.4 Run the build, the tests, `UiManagerRegisterTests` (total 8), and `ModeDiffTests` (total 9). Expect `failed: 0`. Run five plants on `UiManager.cs` and `ModeHistory.cs`. Plant 10: `GC.KeepAlive(new object());` as the first line of `Apply`. Expect `SetModeAndTheDiffAllocateNothingAfterWarmUp` to fail and `AFrameWithNoTargetAllocatesNothing` to pass. Plant 11: the same line as the first line of the handler, before the check of the target. Expect both 0-byte tests to fail. Plant 12: the same line as the first line of `SetMode`. Expect the diff test to fail. Plant 13: the same line as the first line of `ModeHistory.Back()`. Expect the diff test to fail. Plant 14: remove the `_applying` check of `SetMode`. Expect `SetModeBackAndRegisterFromTheDiffThrow` to fail. Record the failed tests and the measured bytes in `context.md`.

**Tasks (group 2, the docs):**
- [ ] 4.5 Edit the doc comments of `src/BrutalUi.Framework/Panels/PanelDefinition.cs` (Notes question 2, option A). The remark sentence, the `defaultSlot` parameter, and the `DefaultSlot` summary state the real use: the name of the slot that the panel suggests. A loader can use it to fill an entry of a mode that names no slot. `UiManager` does not read it, because each `ModePanel` names its slot. Change no code line.
- [ ] 4.6 Edit `docs/technical-design.md`. Section 4: add the line `├─ Before-update slot: OnBeforeUpdate (the mode diff of UiManager)` as the first line under `UiContext.Update(deltaTime)` in the overview. In step 2 of "Frame order", replace the sentence "The mode diff of the framework layer joins this order when the panel and mode features land (section 10)." with: `Update` calls the slot `OnBeforeUpdate` first, and `UiManager` applies the mode diff there (section 10). Section 10: in the Components table, remove "`ModeHistory` waits for panels-and-modes." from the row of `ModeHistory`. Add a sub-section `### Mode set` after "Slot containers" and before the `---` line, with these rules: the Core slot (first in `Update`, the `=` rule, the re-entry guard, the exception rule), the register rule (the order, the layout build, the cached slots, each check), the defer rule, the diff rules (the leave, the enter, the stay with no join, the move, the order), the layout rule, the map rule, the history rule, the error rule, the frame rule (the hit test of the same `Update` reads the old layout, and the stale rules clear the states of a leaving panel), the thread rule, and the 0-byte rule. End with one sentence that names the test classes (section 14). Section 14: add a bullet `**Mode tests.**` after the bullet "Slot tests" and before "Sample projects". Name the seven classes, their counts, the two 0-byte tests, and the plants of tasks 1.6, 2.7, 3.4, and 4.4 with their results. Do not call the 0-byte tests a numbered gate (Decision 10).
- [ ] 4.7 Edit `CHANGELOG.md`. Under `## [Unreleased]`, `### Added`, add two lines after the slot-container line: `UiContext.OnBeforeUpdate`, the slot that `Update` calls first (panels-and-modes/mode-set), and `UiManager` with `Register`, `SetMode`, the deferred mode diff, the layout swap, the action map switch, and `ModeHistory` with `Back` (panels-and-modes/mode-set). Under `### Changed` (add the heading when it does not exist), add one line: the remark of `PanelDefinition.DefaultSlot` states that the manager does not read it.
- [ ] 4.8 Run `MSBUILDDISABLENODEREUSE=1 scripts/test-linux.sh; echo "exit=$?"` and expect the PASS line and `exit=0`. Run the demo commands of each phase and expect the totals 6 and 6, 9 (`ModeDiffTests` after phase 4), 5, 5, 9, and 8, each with `failed: 0`. Run `python3 /home/aaronstatic/.claude/skills/asd-ste100/scripts/ste-lint.py --fail-over 2.5` on `docs/technical-design.md`, `CHANGELOG.md`, `context.md`, and this plan, and fix the prose until each file passes. Record the demo, the results, and each decision that this plan did not settle in `context.md`.

**Estimated Steps:** about 55 (8 tasks, about seven steps for one task)

**Files (group 1, 5):** `UiManager.cs`, `ModeHistory.cs` (plant 13 only, then restored), `UiManagerRegisterTests.cs`, `ModeDiffTests.cs`, `context.md`
**Files (group 2, 5):** `PanelDefinition.cs`, `technical-design.md`, `CHANGELOG.md`, `context.md`, `implementation.md`

**Acceptance Criteria:**
- [ ] The 8 tests of `UiManagerRegisterTests` and the 2 new tests of `ModeDiffTests` pass. Each earlier test of this feature still passes. Each existing test passes with no edit.
- [ ] The five plants of task 4.4 give a non-zero exit code, and the restored files equal their copies.
- [ ] `git diff` of `PanelDefinition.cs` shows changed `///` lines only.
- [ ] `scripts/test-linux.sh` prints its PASS line.

---

## Key Technical Decisions

### Confirmed Assumptions

None. This plan is a draft with no interview. The confirmed interview items of the epic (Q1 to Q4 and A1 to A9 in `docs/features/panels-and-modes/epic-overview.md`) bind the plan.

### Decision 1: `UiManager` and `ModeHistory` live in `src/BrutalUi.Framework/Panels/` (the chosen approach)
**Choice:** Both types are sealed classes in the folder of panel-definition and slot-container, in the flat namespace `BrutalUi.Framework`. `BrutalUi.Framework` references Core only (A2).
**Rationale:** One folder for the epic keeps the panel types together, as Decision 1 of slot-container states. mosaic-ui puts the manager in `Layout/` and the history in `Modes/`, but brutal-ui has no Framework folders of those names.
**Alternatives:** A folder `Modes/`: two folders for one data model, and the manager reads each panel type.

### Decision 2: The Core slot is a named delegate with the `deltaTime` of the call, and a re-entry guard
**Choice:** `public delegate void UiBeforeUpdateHandler(float deltaTime);` in `src/BrutalUi.Core/Tree/`, and `UiContext.OnBeforeUpdate` of that type. `Update` calls it first, inside a guard flag, and a nested `Update` throws `InvalidOperationException`.
**Rationale:** Q1 and A2 of the epic ask for one Core slot that runs first and that a write with `=` sets. Core names each of its slots with its own delegate type (`UiUnhandledInputHandler`, `UiFileDropHandler`, `UiContentScaleHandler`), so the doc comment of the type holds the rules. The `deltaTime` costs nothing, and a game hook in the same slot can read it. The guard matches the guard of the binding pass: a nested call would run the passes inside the diff. The guard is a field write and a `finally` block, so the slot allocates nothing. A `null` slot costs one compare, so each existing gate keeps its count.
**Alternatives:** (a) `System.Action`: no rule text on the type, and it breaks the pattern of Core. (b) A handler with the context as argument: the manager holds the context already. (c) A list of hooks: more than the one slot of A2, and `+=` on a slot breaks the `=` rule.

### Decision 3: `SetMode` records the target, and the last call in a frame wins
**Choice:** `SetMode` scans the modes, throws `ArgumentException` with `ParamName` `name` for an unknown name before a change, and writes `_target`. A second call overwrites it. The handler applies the target at the start of the next `Update`.
**Rationale:** The requirements and Q1 of the epic ask for it. A `SetMode` from a command runs inside the input pass of `Update`, after the slot, so the change shows at the start of the next `Update`. A `SetMode` from game code before `Update` shows in the same `Update`. The scan over the byte names allocates nothing.
**Alternatives:** (a) An apply inside `SetMode`: a change in the middle of the input pass, against Q1. (b) A queue of targets: no use, because only the last target counts.

### Decision 4: The manager resolves each name at register time and caches each slot
**Choice:** `Register(ModeDefinition)` resolves the layout, each slot (through `SlotContainer.Find`), and each panel, and stores a `Placement[]` for the mode. The diff reads the placements only and does no lookup. The register order is the layouts and the panels before the modes. `Register(PanelDefinition)` returns the new controller.
**Rationale:** The panel-definition records leave the name check to mode-set, and the slot-container contract tells mode-set to cache each `Find`. A check at register time gives the error at build time, not in a frame. The returned controller gives the game and the tests the root of a panel with no lookup API.
**Alternatives:** (a) A lookup by name in each diff: 0 bytes too, but each diff repeats the work, and a bad name throws from `Update`. (b) A lazy resolve at the first `SetMode`: the order rule goes away, but the error comes later. (c) `Register` returns `void` for a panel, and a `GetController(name)` method: one more public member.

### Decision 5: `Register(LayoutDefinition)` builds the layout root at once (Notes question 3)
**Choice:** The register calls `CreateRoot(Panels)` one time and keeps the root for the life of the manager. A swap back adds the same root again.
**Rationale:** The slots of a mode can be checked at the register of the mode only when the layout root exists. The build is a build-time allocation, as the requirements allow. A layout root that no mode uses costs one build. The rule matches Q3 for a panel root: one build, and the root lives for the life of the manager.
**Alternatives:** (a) A build at the first diff that shows the layout: a layout that no mode uses costs nothing, but a missing slot then throws from `Update`, and the first swap to a layout allocates in a frame. (b) A new layout root at each swap: allocates in each swap, against A7.

### Decision 6: The diff runs in five steps, and the leave pass reads the real slot of each panel
**Choice:** The mark, the leave, the layout, the enter, and the map, as the Internal Shape states, then the history. Each panel entry records the slot that holds its root now. The leave pass unmounts each panel whose slot differs from the slot of the new mode. The enter pass calls `Mount(EnsureRoot(), i)` for each placement in order.
**Rationale:** The order is the contract of slot-container: unmount first, also a root that moves, then mount in the order of the mode. A held root moves with no join, so a panel that stays keeps its style, its tweens, and its bindings (A5). The real slot of each panel, not the old mode, drives the leave pass. Thus one code path covers the first mode, a change on one layout, a layout swap, and a diff after a failed diff. The mark pass costs one field write for each panel, and the scan has no nested loop.
**Alternatives:** (a) A diff of the placement lists of the old and the new mode: a nested scan, and a failed diff leaves stale panels that no later diff sees. (b) An unmount of each panel and a mount of each panel: each panel joins again, restyles, and drops its tweens, against A5.

### Decision 7: A layout swap removes the old root and adds the new root, and each panel leaves the old slots
**Choice:** When the layout of the new mode differs from the layout in the context, the diff calls `RemoveRoot` on the old root and `AddRoot` on the new root, between the leave pass and the enter pass. A panel in the old layout leaves, because the slots of the new layout are other elements, also for the same slot name.
**Rationale:** A4 of the epic keeps the layout root as the one root of the manager in the context. An old slot that keeps a panel root would make a later `Mount` of that root throw. With the rule, a layout that is not in the context holds no panel. The manager touches no root of the game.
**Alternatives:** (a) One shared root with the layout roots as children: two levels of roots, and the layout root is no longer the context root. (b) Keep the panels in the old slots: a panel in both modes then throws at its mount.

### Decision 8: The error rule keeps the target and runs the diff again
**Choice:** An exception of a factory or of `Mount` leaves the handler and `Update`. The target stays, and the current mode stays the old mode. The next `Update` runs the diff again. A `SetMode`, a `Back()`, or a `Register` from inside the diff throws `InvalidOperationException`.
**Rationale:** `PanelController` already retries a failed factory at its next call. With the real slot of each panel (Decision 6), a second run of the diff finishes the same tree, and a new target removes each stale panel. A call that changes the target or the tables during the diff would break the loop of the diff, so the guard rejects it.
**Alternatives:** (a) Clear the target on an exception: the screen stays half changed until the game calls `SetMode` again. (b) Roll back each step: more code and more state, for a fault that the game fixes in its factory.

### Decision 9: `ModeHistory` is a fixed ring of mode indexes with `Back()` on the history
**Choice:** The ring holds the index of each mode that an apply replaced. Its capacity comes from `CreateInfo.HistoryCapacity` (0 gives 16). A full ring drops its oldest entry. `ModeHistory.Back()` pops the newest entry, sets it as the target of the manager, and marks the target as a back step, so its apply adds no entry. A target that equals the current mode adds no entry.
**Rationale:** The requirements ask for a back step only, with `false` for an empty history (A3). panel-sample calls `ModeHistory.Back()`, so the method lives on the history. A fixed ring gives a push with 0 bytes after warm-up, also when a game toggles two modes for hours. A mode never unregisters, so an index stays valid. The capacity follows the pattern of `UiContext.CreateInfo.EventQueueCapacity`.
**Alternatives:** (a) `UiManager.Back()`: panel-sample names `ModeHistory.Back()`. (b) A list that grows: it allocates in a frame. (c) A history that records the current mode too: the back step must then drop two entries.

### Decision 10: The 0-byte checks are tests in `ModeDiffTests`, not a numbered gate
**Choice:** `SetModeAndTheDiffAllocateNothingAfterWarmUp` and `AFrameWithNoTargetAllocatesNothing` measure the diff in the shape of a gate, inside `ModeDiffTests`. `BeforeUpdateTests.AnUpdateWithASlotAllocatesNothingAfterWarmUp` measures the Core slot.
**Rationale:** panel-sample owns the gate `ModeDiffAllocationTests` (A7 of the epic), and its ordinal in section 14. The siblings use the same rule (Decision 8 of slot-container). The tests here still prove A7 for each path of the diff, so panel-sample starts from a known 0.
**Alternatives:** A class `ModeDiffAllocationTests` in this feature: it takes the gate of panel-sample.

### Decision 11: `PanelDefinition.DefaultSlot` keeps its API, and its remark states the real use (Notes question 2)
**Choice:** Option (A) of the panel-definition review. The manager does not read `DefaultSlot`. The doc comments say that a loader can use it to fill an entry of a mode that names no slot.
**Rationale:** `ModePanel` rejects an empty slot, so no mode can omit a slot, and the manager has no case for a default. The xml-markup epic can use the name for a short form of a mode entry. The fix changes doc lines only, and no test.
**Alternatives:** (b) A `ModePanel` with no slot, resolved at register time: it changes the `default(ModePanel)` check and the grouping of `ModeDefinition`. (c) Remove `DefaultSlot`: a change of a public API and of each test that builds a panel.

### Decision 12: The constructor of the manager writes `OnBeforeUpdate` (Notes question 4)
**Choice:** The constructor creates one handler delegate and writes it into `Context.OnBeforeUpdate` with `=`.
**Rationale:** The game then writes no wiring line, and the build allocates the delegate one time. `UiActionMap.Activate` writes `OnUnhandledInput` in the same way. The remarks state that the write replaces a delegate of the game, and that a game hook in the slot after the constructor stops the diff.
**Alternatives:** (a) A public `ApplyPendingMode()` method, and the game writes the slot: one more line in each game, and a missing line shows no mode with no error. (b) Both: a second path for one need.

---

## Risks & Mitigation

| Risk | Likelihood | Impact | Mitigation |
|------|------------|--------|------------|
| The Core change adds a cost or an allocation to each `Update`. | Low | High | A `null` slot costs one compare. `AnUpdateWithASlotAllocatesNothingAfterWarmUp` measures a set slot. Each existing gate runs with no edit. |
| A handler calls `Update` and runs the passes inside the diff. | Low | High | The guard of Decision 2. `AnUpdateFromTheSlotThrowsAndTheContextStaysValid` and plant 1 pin it. The test calls `Update` only at the first slot call, so a missing guard gives no stack overflow. |
| A diff that throws leaves a half tree or a stale panel. | Medium | Medium | Decision 8 and the real slot of each panel (Decision 6). `AFactoryExceptionLeavesUpdateAndTheNextUpdateFinishesTheDiff` and `ANewTargetAfterAFailedDiffShowsOnlyItsOwnPanels` pin it. |
| A staying panel joins again, restyles, and drops its tweens. | Medium | Medium | The leave pass skips a panel with the same slot, and `Mount` moves a held root. `APanelInBothModesInTheSameSlotStaysWithNoJoin` and plant 5 pin it. |
| A layout swap leaves a root in an old slot, and a later mount throws. | Medium | Medium | Decision 7. `TheSlotsOfTheOldLayoutHoldNoPanelAfterASwap` and `ASwapBackReusesTheFirstLayoutRoot` pin it. |
| `AddRoot`, `RemoveRoot`, the style pass of a joined panel, or the child arrays allocate in the measured diff. | Medium | High | The 0-byte test warms each path two times first, so each array holds its largest size. When a Core path allocates after warm-up, record it in `context.md` and stop. The main thread then decides, because a Core fix is outside this plan. |
| A key in the frame of a change hits the new tree with the old layout. | Medium | Low | The hit test reads the layout of the last `Update` (the Core rule). A new panel gets its first hit in the next frame. The remarks and section 10 state it. The stale rules clear the hover and the focus of a leaving panel in the same `Update`. |
| The game writes its own delegate into `OnBeforeUpdate` after the constructor, and no mode shows. | Low | Medium | Decision 12. The remarks of the class and of the slot state the rule. Notes question 4 holds the open choice. |
| Two managers share one context. | Low | Low | The second constructor replaces the slot. The remarks state one manager for each context. |
| The history grows with no bound. | Low | Medium | A fixed ring (Decision 9). `AFullHistoryDropsTheOldestMode` pins the drop. |
| A move between two slots or a swap drops the running tweens of a panel. | Medium | Low | The join rule of Core drops a tween at a join. A9 of the epic puts panel transitions out of scope. Section 10 states it. |
| Parallel test classes share the class table. | Medium | Low | Each test interns its class names with its own prefix. |
| The test host exits with 134, or `TheCurvesAllocateNothing` fails one time. | Medium | Low | Run the tests a second time. Both flakes passed at the second run before. |
| `dotnet build` or `dotnet test` hangs on stale MSBuild nodes. | High | Low | Each command sets `MSBUILDDISABLENODEREUSE=1` and `-nodeReuse:false`. The class runs use the built binaries. No `dotnet build-server shutdown`. |

---

## Dependencies

### Internal Dependencies
- `panels-and-modes/panel-definition` (done): `PanelDefinition`, `LayoutDefinition`, `ModeDefinition`, `ModePanel`, `PanelContext`, `PanelController`, and `PanelInstance`.
- `panels-and-modes/slot-container` (done, on the branch): `SlotContainer.Find`, `Mount`, and `Unmount`.
- `state-and-commands` (done): `UiActionMap.Activate`, `Deactivate`, and `IsActive`, `CommandRegistry`, `EventBus`, `ServiceRegistry`, `CommandBindings.BindCommand`, `Store<T>`, and `LabelBindings.BindText` (tests).
- `core-tree` (done): `UiContext.Update`, `AddRoot`, `RemoveRoot`, `Roots`, `OnUnhandledInput`, `Element`, and the dirty bits.
- The test helpers `tests/BrutalUi.Framework.Tests/Shared/AllocationGate.cs`, the Core `AllocationGate.cs`, and the linked `RecordingRenderer.cs` (done).
- `dev-ops`: `brutal-ui.sln` and `scripts/test-linux.sh`.

### External Dependencies
- None. No new package and no native dependency.

### Sibling Features That Depend on This One
- panel-sample (`UiManager`, `SetMode`, `CurrentMode`, `ModeHistory.Back()`, and the 0-byte paths that `ModeDiffAllocationTests` measures).

---

## Definition of Done

All criteria below must pass. An independent evaluator runs the Verification Method from `/home/aaronstatic/dev/brutal-ui` on Linux. `<base>` is the base commit that task 1.1 records in `context.md`.

### Functional Criteria
- [ ] F1. `UiContext.Update` calls `OnBeforeUpdate` first, with its `deltaTime`, before the input pass and the binding pass. A root that the slot adds gets its style and its layout in the same `Update`. A call of `Update` from the slot throws `InvalidOperationException`, and the context works after it. An exception of the slot leaves the ring entries for the next `Update`. A write with `=` replaces the delegate (`BeforeUpdateTests`).
- [ ] F2. The `UiManager` constructor writes `OnBeforeUpdate`. `SetMode` records the target and changes no tree. The next `Update` adds the layout root as the one root of the manager and mounts each panel in the order of the mode. Two `SetMode` calls in one frame apply the last. A frame with no target changes nothing. An unknown name throws `ArgumentException` with `ParamName` `name` and keeps the target (`UiManagerTests`).
- [ ] F3. A change unmounts a panel that only the old mode holds and keeps its state. It mounts a panel that only the new mode holds, and the factory of the panel runs one time over many changes. A panel in the same slot keeps its parent and gets no `DirtyFlags.Style` bit. A panel that moves leaves the old slot and gets the bit in the new slot. Each slot takes the order of the new mode. An unmounted panel shows the new store value when it returns. A `SetMode` from a command applies at the start of the next `Update` (`ModeDiffTests`).
- [ ] F4. A mode with another layout leaves only the new layout root of the manager in `Roots`, empties the old slots, builds each layout root one time, styles and lays out the new layout in the same `Update`, and keeps each root of the game (`LayoutSwapTests`).
- [ ] F5. A mode with a map activates it. A mode with another map replaces the old map. A mode with no map deactivates the old map. Two modes with no map keep the slot of the game. A key in the frame of the change reaches the new map (`ModeActionMapTests`).
- [ ] F6. Each apply after the first records the old mode. `Back()` sets the newest entry as the target, drops it, and returns `true`. The apply of a back step adds no entry. `Back()` on an empty history returns `false`. A full history drops its oldest entry. A `SetMode` after `Back()` in one frame wins. The capacity comes from the create-info (`ModeHistoryTests`).
- [ ] F7. `Register` rejects a `null` definition, a duplicate name, a bad layout root, two slots of one name, and a mode with an unknown layout, panel, or slot, each before a change. A call of `SetMode`, `Back()`, or `Register` from the diff throws `InvalidOperationException`. A factory exception leaves `Update`, the next `Update` finishes the diff, and a new target after a failed diff shows only its own panels (`UiManagerRegisterTests`).
- [ ] F8. `SetMode`, the diff, and an `Update` with no target allocate 0 bytes after warm-up in Debug, and an `Update` with a set slot allocates 0 bytes. Plants 10 to 13 of task 4.4 fail the diff test (`ModeDiffTests`, `BeforeUpdateTests`).

### Quality Criteria
- [ ] Q1. `dotnet build brutal-ui.sln -nodeReuse:false` gives `0 Warning(s)` and `0 Error(s)`.
- [ ] Q2. `dotnet test --solution brutal-ui.sln` gives 0 failed, with the skipped count of `<base>`. `BeforeUpdateTests` holds 6 tests, `UiManagerTests` 6, `ModeDiffTests` 9, `LayoutSwapTests` 5, `ModeActionMapTests` 5, `ModeHistoryTests` 9, and `UiManagerRegisterTests` 8. `BrutalUi.Framework.Tests` holds 42 more tests than at `<base>`, and `BrutalUi.Core.Tests` holds 6 more.
- [ ] Q3. Each public member of the new files and of the new Core members has an XML doc comment. The new doc comments and the changed docs use ASD-STE100 prose, and the lint gives no score above 2.5.
- [ ] Q4. `UiManager.cs` and `ModeHistory.cs` hold no `lock`, no `IDisposable`, no `event` keyword, no `ToString(` call, no `(object)` cast, no `new List`, no `Stack<`, and no `Queue<` in code.
- [ ] Q5. Each existing allocation gate passes with no edit.

### Integration Criteria
- [ ] I1. The Core diff from `<base>` holds only `src/BrutalUi.Core/Tree/UiContext.cs` (changed) and `src/BrutalUi.Core/Tree/UiBeforeUpdateHandler.cs` (new). The Framework diff holds only `UiManager.cs` (new), `ModeHistory.cs` (new), and `PanelDefinition.cs` (doc lines only). No project file, no file of `scripts/` or `samples/`, and no test file that existed at `<base>` changed.
- [ ] I2. `UiContext.Update` reads `OnBeforeUpdate` before its call of `DrainEvents()`. `UiManager` calls `SlotContainer.Find` only in `Register` and its helpers, and it calls `Mount`, `Unmount`, `AddRoot`, `RemoveRoot`, `Activate`, and `Deactivate` only in the diff.
- [ ] I3. Section 4 of `docs/technical-design.md` names `OnBeforeUpdate` in the frame order. Section 10 holds the sub-section `### Mode set`, and its Components table no longer says that `ModeHistory` waits. Section 14 holds the bullet `**Mode tests.**`. `CHANGELOG.md` names `UiContext.OnBeforeUpdate`, `UiManager`, and `ModeHistory`.
- [ ] I4. `scripts/test-linux.sh` prints its PASS line, so the HelloPanel smoke run keeps its 6 lines and its 0 bytes.

### Verification Method

Run each step from `/home/aaronstatic/dev/brutal-ui` on Linux. `<scratch>` is any empty folder outside the repository. Read `<base>` and the base counts from `docs/features/panels-and-modes/mode-set/context.md`. Do not run `dotnet build-server shutdown`. When a run exits with 134 or `TheCurvesAllocateNothing` fails, run it a second time.

1. `MSBUILDDISABLENODEREUSE=1 dotnet build brutal-ui.sln -nodeReuse:false 2>&1 | tail -n 30`. Expect `0 Warning(s)` and `0 Error(s)` (Q1).
2. `MSBUILDDISABLENODEREUSE=1 dotnet test --solution brutal-ui.sln 2>&1 | tail -n 30`. Expect 0 failed and the skipped count of `context.md` (Q2, Q5).
3. Run `tests/BrutalUi.Core.Tests/bin/Debug/net10.0/BrutalUi.Core.Tests --filter-class '*BeforeUpdateTests' 2>&1 | tail -n 5` and expect a total of 6 (F1, F8). Run `tests/BrutalUi.Framework.Tests/bin/Debug/net10.0/BrutalUi.Framework.Tests --filter-class '*<Class>' 2>&1 | tail -n 5` for each class and expect: `UiManagerTests` 6 (F2), `ModeDiffTests` 9 (F3, F8), `LayoutSwapTests` 5 (F4), `ModeActionMapTests` 5 (F5), `ModeHistoryTests` 9 (F6), and `UiManagerRegisterTests` 8 (F7). Each reads `failed: 0`. Run both binaries with no filter and compare the totals with the base counts plus 6 and plus 42 (Q2). Read the test names in each file and match them to the criteria.
4. Copy `src/BrutalUi.Framework/Panels/UiManager.cs` to `<scratch>`. Add `GC.KeepAlive(new object());` as the first line of `Apply`. Run `MSBUILDDISABLENODEREUSE=1 dotnet test --solution brutal-ui.sln > /dev/null 2>&1; echo "exit=$?"`. Expect a value that is not 0. Restore the file and check it with `cmp` (F8). Repeat with the `Unmount` call of the leave pass removed (F3), and with the `RemoveRoot` call removed (F4). Copy `src/BrutalUi.Core/Tree/UiContext.cs` to `<scratch>`, remove the `_beforeUpdate` check of `Update`, run the same command, expect a value that is not 0, and restore it with `cmp` (F1). Run step 2 again.
5. `grep -nE '\block\b|IDisposable|\bevent\b|ToString\(|\(object\)|new List|Stack<|Queue<' src/BrutalUi.Framework/Panels/UiManager.cs src/BrutalUi.Framework/Panels/ModeHistory.cs | grep -v '///'`. Expect no line (Q4).
6. `grep -n 'OnBeforeUpdate\|DrainEvents();' src/BrutalUi.Core/Tree/UiContext.cs`. Expect the read of the slot in `Update` on a line before the call `DrainEvents();` (I2). `grep -n 'SlotContainer.Find' src/BrutalUi.Framework/Panels/UiManager.cs`. Expect each hit inside a `Register` method or its private helpers (I2).
7. `git diff <base> --name-only -- src/ samples/ scripts/ '*.csproj' '*.props' '*.sln'` and `git status --porcelain --untracked-files=all -- src/ samples/ scripts/`. Expect only the five files of I1, plus `SlotContainer.cs` when `<base>` comes before its commit. `git diff <base> -- src/BrutalUi.Framework/Panels/PanelDefinition.cs | grep -E '^[+-] ' | grep -v '///'`. Expect no line. `git diff <base> --diff-filter=M --name-only -- tests/`. Expect no output (I1).
8. `MSBUILDDISABLENODEREUSE=1 scripts/test-linux.sh; echo "exit=$?"`. Expect the PASS line and `exit=0` (I4).
9. Read sections 4, 10, and 14 of `docs/technical-design.md` and the `## [Unreleased]` part of `CHANGELOG.md` (I3). Run `python3 /home/aaronstatic/.claude/skills/asd-ste100/scripts/ste-lint.py --fail-over 2.5` on each changed doc (Q3).

---

## Testing Strategy

### Unit Tests
- `Core.Tests/BeforeUpdateTests` (5 plus the 0-byte test): the call order and the `deltaTime`, a root that the slot adds, the re-entry guard, the exception rule, and the `=` rule.
- `Framework.Tests/Panels/UiManagerTests` (6): the slot write, the defer rule, the first mode, the last target, a frame with no target, and the unknown name.
- `Framework.Tests/Panels/ModeHistoryTests` (9): the record rule, the back step, the walk to the first mode, the empty history, no forward step, the same mode, the full ring, the order in one frame, and the capacity and `Clear`.
- `Framework.Tests/Panels/UiManagerRegisterTests` (8): the null and duplicate checks, the layout build and its checks, the duplicate slot names, the unknown names, the guard of the diff, and the error rule.

### Integration Tests
- `Framework.Tests/Panels/ModeDiffTests` (7 plus 2 0-byte tests): the leave, the enter with one build, the stay with no join, the move, the order, a binding across a leave, and a `SetMode` from a bound button command. Each test runs `UiContext.Update` through the Core passes.
- `Framework.Tests/Panels/LayoutSwapTests` (5): the root swap, the empty old slots, the reuse of a layout root, the style and the layout of the new layout in the same `Update`, and the roots of the game.
- `Framework.Tests/Panels/ModeActionMapTests` (5): the activate, the replace, the deactivate, the slot of the game, and a key in the frame of the change.

### Allocation Tests
- `SetModeAndTheDiffAllocateNothingAfterWarmUp` and `AFrameWithNoTargetAllocatesNothing` in `ModeDiffTests` (Decision 10): 2 warm-up rounds, a release of the allocation context, 100 measured iterations, an assert of 0 bytes and of a fixed sum. Plants 10 to 13 guard them.
- `AnUpdateWithASlotAllocatesNothingAfterWarmUp` in `BeforeUpdateTests`: the Core slot with a static handler.
- The epic gate `ModeDiffAllocationTests` of panel-sample measures the same paths later on the HelloPanel scene.

### Regression Tests
- Each existing test runs with no edit. The 17 numbered allocation gates run with a `null` slot and keep 0 bytes. `scripts/test-linux.sh` runs the HelloPanel smoke run, which does not use the manager yet.

### Manual Testing
- None. The feature has no visual result. panel-sample shows two modes in HelloPanel.

---

## Related Documentation

- **Requirements:** `docs/features/panels-and-modes/mode-set/requirements.md`
- **Epic:** `docs/features/panels-and-modes/epic-overview.md`, `docs/features/panels-and-modes/epic-requirements.md`
- **Siblings:** `docs/features/panels-and-modes/panel-definition/implementation.md`, its `context.md` and `code-review.md` (the `DefaultSlot` follow-up), `docs/features/panels-and-modes/slot-container/implementation.md` (the format reference and the mount contract) and its `context.md`, `docs/features/panels-and-modes/panel-sample/requirements.md`
- **Design:** `docs/technical-design.md` sections 3 (package layout), 4 (frame order), 5 (element tree), 9 (input, the action map), 10 (Framework Layer: "Panel definitions" and "Slot containers"), and 14 (Testing). `docs/mission-statement.md`
- **Rules:** `AGENTS.md`, `.beast-mode/phase-handoff.md` sections 1 and 11, `.beast-mode/tool-rules.md`
- **Code patterns:** `src/BrutalUi.Core/Tree/UiContext.cs` (`Update`, the guard of `_binding`, `OnUnhandledInput`, `AddRoot`, `RemoveRoot`), `src/BrutalUi.Core/Input/UiUnhandledInputHandler.cs` (a slot delegate with its rules), `src/BrutalUi.Framework/Actions/UiActionMap.cs` (the create-info, the one delegate of the constructor, `Activate`, `Deactivate`), `src/BrutalUi.Framework/Panels/SlotContainer.cs` (the mount contract and the messages), `src/BrutalUi.Framework/Panels/PanelController.cs` (the remark style), `tests/BrutalUi.Framework.Tests/Panels/SlotLookupTests.cs` and `PanelControllerTests.cs` (the 0-byte test shape), `tests/BrutalUi.Core.Tests/Input/UnhandledInputTests.cs` and `RoutingGateTests.cs` (a slot test and a re-entry test), `tests/BrutalUi.Framework.Tests/Commands/CommandBindingTests.cs` (a bound button in a test)
- **Skills:** no implementation skill applies. The ASD-STE100 lint script is at `/home/aaronstatic/.claude/skills/asd-ste100/scripts/ste-lint.py`.

---

## Notes

### Open Scope Questions

The plan uses the recommended option (A) of each question. A change before task 1.5 costs little.

1. **Which demo does phase 1 use?**
   - **(A) Recommended:** a developer runs `tests/BrutalUi.Framework.Tests/bin/Debug/net10.0/BrutalUi.Framework.Tests --filter-class '*UiManagerTests'` after a build and reads a total of 6 with `failed: 0`.
   - (B) a developer runs the Framework binary with `--filter-class '*UiManagerTests'` and the Core binary with `--filter-class '*BeforeUpdateTests'` after a build, and reads a total of 6 for each with `failed: 0`.
2. **How does the epic use `PanelDefinition.DefaultSlot`?** (the open follow-up of the panel-definition review)
   - **(A) Recommended:** keep the API. The manager does not read it, and the doc comments state that a loader can use it to fill a mode entry with no slot (Decision 11).
   - (B) add a `ModePanel` with no slot, and the manager fills the slot from `DefaultSlot` at register time.
   - (C) remove `DefaultSlot` from `PanelDefinition` and from its tests.
3. **When does the manager build a layout root?**
   - **(A) Recommended:** at `Register(LayoutDefinition)`. The register of a mode then checks each slot name, and the diff does no lookup (Decision 5).
   - (B) at the first diff that shows the layout. A layout that no mode uses costs nothing, but a missing slot throws from `Update`.
4. **Who writes `OnBeforeUpdate`?**
   - **(A) Recommended:** the `UiManager` constructor writes its handler into the slot (Decision 12).
   - (B) the game writes the slot with a public method `ApplyPendingMode` of the manager.
   - (C) the constructor writes the slot, and the manager also has a public `ApplyPendingMode` for a game that chains its own hook.

### Conflicts and Gaps

1. **"`ModeHistory` records each applied mode."** The plan records the mode that each apply replaces. The first apply records nothing, and a back step records nothing. The result is the back step that the requirements ask for: `Back()` gives the previous mode. The newest entry is never the current mode.
2. **`Back()` is on `ModeHistory`.** The mode-set requirements do not name the owner of `Back()`. The panel-sample requirements name `ModeHistory.Back()`, so the history holds a link to its manager (Decision 9).
3. **`Back()` then `SetMode` in one frame.** The requirements say that `Back()` drops the entry, and that the last call wins. The plan keeps both rules, so the dropped entry stays dropped when a `SetMode` follows in the same frame. `ASetModeAfterBackInOneFrameWins` pins it.
4. **"The same slot".** The plan reads it as the same `SlotContainer` element. A layout swap thus moves each panel, also a panel with the same slot name in both layouts, and each such panel joins again (Decision 7).
5. **"The manager rents each list at build time."** The plan reads "rent" as an allocation at register time. It uses no `ArrayPool`. Each table grows at register, each slot child array grows at the first mounts, and the history ring has a fixed size.
6. **The register order.** The requirements do not state it. The plan asks for the layouts and the panels before the modes, so each mode resolves its names at register (Decision 4).
7. **A target that equals the current mode.** The requirements do not state it. The plan runs no diff and records no entry (`ASetModeOfTheCurrentModeChangesNothing`).
8. **The frame of a change.** The input pass of the same `Update` hit-tests the new tree with the layout of the last `Update`. A new panel gets its first hit in the next frame. The plan states it and adds no Core change for it.
9. **panel-sample needs mode counts.** The manager gives `CurrentMode` only. The sample counts its own changes. No counter API is in this plan (YAGNI).
10. **The prose lint did not run.** The planner had no shell tool, so it did not run `ste-lint.py` on this file. Task 4.8 runs it on this plan and on the changed docs.
11. **The plan is not compiled.** The planner did not compile `UiManager` or the Core change. Task 1.6 is the first compile. `Update` reads `_roots` after the drain and the binding pass already, so the slot needs no other Core change.
12. **The Core test count is not known.** The context gives the solution count (2773) and the Framework count (180) only. Task 1.1 records the count of `BrutalUi.Core.Tests`. At the end, the solution reads the base count plus 48.

---

*This plan will be updated as implementation progresses. See context.md and tasks.md for current status.*
