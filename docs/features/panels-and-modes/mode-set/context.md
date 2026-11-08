# panels-and-modes/mode-set - Context and Decisions

**Last Updated:** 2026-11-08 03:12
**Current Phase:** Complete (phases 1 to 4 and the cross-phase review are done)
**Status:** ✅ Ready for Review

---

## Quick Status

**Done:**

- ✅ The plan (`implementation.md`) is complete. It uses the recommended option (A) of each of its four open scope questions (Notes). The confirmed items of the epic (Q1 to Q4 and A1 to A9) bind the plan.

- ✅ The dev docs (`tasks.md`, `inbox.md`, this file) are current. panel-definition and slot-container stand ready for review on this branch.

- ✅ Phase 1 (tasks 1.1 to 1.6): the Core slot `OnBeforeUpdate`, `UiManager` with the first mode, `BeforeUpdateTests` (6), `UiManagerTests` (6), and plants 1 to 3.

- ✅ Phase 2 (tasks 2.1 to 2.7): the mode change in `UiManager.Apply`. The diff now holds the same-target return, the mark, the leave, the swap, the enter, and the map switch. The new classes are `ModeDiffTests` (7), `LayoutSwapTests` (5), and `ModeActionMapTests` (5). Plants 4 to 7 ran. The solution read 2802 (3 skipped), 0 failed. The build had 0 warnings and 0 errors.

- ✅ Phase 3 (tasks 3.1 to 3.4): `ModeHistory` with the fixed ring, `Push`, `Back`, and `Clear`. Step 6 now pushes the history, and `UiManager` holds the back-step target. `ModeHistoryTests` (9) is new. Plants 8 to 9 ran. The solution read 2811 (3 skipped), 0 failed. The build had 0 warnings and 0 errors.

- ✅ Phase 4 group 1 (tasks 4.1 to 4.4): the register checks with the check-before-change rule. The `_applying` guard now stands in `SetMode`, `Register`, and `ModeHistory.Back()`. The remarks state the error rule and the allocation rule.

  `UiManagerRegisterTests` (8) is new. `ModeDiffTests` now holds the two 0-byte tests (total 9). Plants 10 to 14 ran. The solution read 2821 (3 skipped), 0 failed. The build had 0 warnings and 0 errors.

- ✅ Phase 4 group 2 (tasks 4.5 to 4.8): the group changed the `DefaultSlot` remark, `docs/technical-design.md`, and `CHANGELOG.md`. The group ran the full gate and the STE lint. The gate passed every step except the smoke step. The smoke step fails on a host fault. See `Blockers`.

- ✅ The cross-phase review (`code-review.md`): 2 LOW findings. CR-2 adds the `SetMode`-then-`Back` order to `ASetModeAfterBackInOneFrameWins`; a plant on the `_targetFromBack` line of `SetMode` fails exactly it. CR-1 rewords one line comment in `UiManager.cs`; the Q4 gate grep now prints no line. The review verifier returns PASS, with Demo: MANUAL (the accepted smoke host fault).

**Next:**

- 📋 The person tests the "Needs Human Verification" items, then commits, pushes, and opens the PR for `feat/panels-and-modes/mode-set`.
- 📋 panel-sample (feature 4 of 4) continues the epic: two modes in HelloPanel, the seventh smoke line, and the `ModeDiffAllocationTests` gate.

**Blockers:**

- None for the code. The smoke step of `scripts/test-linux.sh` fails on this host. Xvfb segfaults inside the NVIDIA EGL stack during GLX init. No repo file causes it. This group changed only doc lines. The build, test, gpu, and loader steps pass. The owner accepted the split evidence; a repaired host must show the PASS line (see "Needs Human Verification").

---

## Base and Counts

Task 1.1 recorded these counts before any code, on branch `feat/panels-and-modes/mode-set`.

- Base commit: `7c80ca5bdc4efe0d7780326bb60ddc191f778684`.

- Solution test count: 2773 (3 skipped, all in `BrutalUi.Glfw.Tests`), 0 failed. `dotnet build brutal-ui.sln`: 0 Warning(s), 0 Error(s).

- `BrutalUi.Framework.Tests`: 180, 0 failed.

- `BrutalUi.Core.Tests`: 1745, 0 failed.

- Prerequisite check: `SlotContainer.cs` and the six panel-definition files exist in `src/BrutalUi.Framework/Panels/`. The files are `LayoutDefinition.cs`, `ModeDefinition.cs`, `PanelContext.cs`, `PanelController.cs`, `PanelDefinition.cs`, and `PanelInstance.cs`.

### After Phase 1 (2026-11-07)

- Solution: 2785 (3 skipped), 0 failed. `BrutalUi.Framework.Tests`: 186. `BrutalUi.Core.Tests`: 1751. The 12 new tests add to the two projects.

- Demo (task 1.6): `tests/BrutalUi.Framework.Tests/bin/Debug/net10.0/BrutalUi.Framework.Tests --filter-class '*UiManagerTests'` reads total 6, `failed: 0`. `BeforeUpdateTests` reads total 6, `failed: 0`.

- `dotnet build brutal-ui.sln -nodeReuse:false`: 0 Warning(s), 0 Error(s).

- Diff scope: Core holds only `UiContext.cs` (changed) and `UiBeforeUpdateHandler.cs` (new). Framework holds only `UiManager.cs` (new). Tests add `BeforeUpdateTests.cs` and `UiManagerTests.cs`. No project file changed.

- Plants: each one copies the file to `/tmp/brutal-ui-plants/mode-set/`, breaks it, runs the full solution test, restores, checks with `cmp`, and re-tests. One shell call runs all of it, because `/tmp` does not persist between calls.

- Plant 1 (phase 1): weakened `if (_beforeUpdate)` to `if (_beforeUpdate && false)`. Exit 2, 1 failed: `AnUpdateFromTheSlotThrowsAndTheContextStaysValid`. The restore was equal. The re-test read 0 failed.

- Plant 2 (phase 1): moved `DrainEvents()` before the slot call. Exit 2, 3 failed: `UpdateCallsTheSlotFirstWithTheDeltaTime` (expected), `AnUpdateFromTheSlotThrowsAndTheContextStaysValid`, and `AnExceptionOfTheSlotLeavesUpdateBeforeTheInputPass`. The last two share the order rule. The restore was equal. The re-test read 0 failed.

- Plant 3 (phase 1): appended `Apply();` in `SetMode`. Exit 2, 3 failed: `SetModeRecordsTheTargetAndChangesNoTree` (expected), `TwoSetModeCallsInOneFrameApplyTheLast`, and `SetModeRejectsAnUnknownName`. The last two share the defer rule. The restore was equal. The re-test read 0 failed.

### After Phase 2 (2026-11-07)

- Solution: 2802 (3 skipped), 0 failed. `BrutalUi.Framework.Tests`: 203 (186 plus the 17 new). `BrutalUi.Core.Tests`: 1751, unchanged. The 17 new tests add to `BrutalUi.Framework.Tests` only.

- Demo: `ModeDiffTests` total 7, `LayoutSwapTests` total 5, `ModeActionMapTests` total 5, each `failed: 0`. The phase 1 demo holds: `UiManagerTests` 6, `BeforeUpdateTests` 6, each `failed: 0`.

- `dotnet build brutal-ui.sln -nodeReuse:false`: 0 Warning(s), 0 Error(s).

- Diff scope of phase 2: `src/BrutalUi.Framework/Panels/UiManager.cs` (edited) and the three new test files. No file of `src/BrutalUi.Core/` changed in phase 2. The Core diff stays the phase 1 `UiContext.cs` and `UiBeforeUpdateHandler.cs`. `SlotContainer.cs`, `PanelController.cs`, `ModeDefinition.cs`, `UiActionMap.cs`, each project file, `scripts/`, `samples/`, and each existing test file did not change.

- Plants: each one ran inside one chained shell call with a fresh `mkdir -p` of the scratch folder. Each plant log showed 0 `error CS` lines before the run read its result.

- Plant 4 (phase 2): deleted `slot.Unmount(...)` from the leave pass. Exit 2, 11 failed: `APanelThatOnlyTheOldModeHoldsUnmountsAndKeepsItsState` (expected), `APanelThatOnlyTheNewModeHoldsMountsAndItsRootIsBuiltOneTime`, `APanelInBothModesInTheSameSlotStaysWithNoJoin`, `APanelThatMovesToAnotherSlotLeavesAndEnters`, `EachSlotTakesTheOrderOfTheNewMode`, `AnUnmountedPanelKeepsItsBindingAndShowsTheNewValueWhenItReturns`, and all five `LayoutSwapTests`. The moves throw from `Mount`, because another parent holds the root. The swap asserts a stale parent. The leave-first order is the only guard of the move path. The restore was equal. The re-test read 0 failed.

- Plant 5 (phase 2): the leave pass dropped `!ReferenceEquals(slot, e.Next)`, so it also unmounted a panel with the same slot. Exit 2, 1 failed: `APanelInBothModesInTheSameSlotStaysWithNoJoin` (expected). The staying root re-joined and got the `DirtyFlags.Style` bit. The restore was equal. The re-test read 0 failed.

- Plant 6 (phase 2): deleted the `if (_shown >= 0) { Context.RemoveRoot(...); }` block. Exit 2, 3 failed: `AModeWithAnotherLayoutSwapsTheContextRoot` (expected), `ASwapBackReusesTheFirstLayoutRoot`, and `TheManagerKeepsTheRootsOfTheGame`. The swap rule keeps the old root out of `Roots`. The restore was equal. The re-test read 0 failed.

- Plant 7 (phase 2): deleted the `else if (_current >= 0) { ... Deactivate(); }` branch. Exit 2, 1 failed: `AModeWithNoMapDeactivatesTheOldMap` (expected). The old map kept the slot. The restore was equal. The re-test read 0 failed.

### After Phase 3 (2026-11-07)

- Solution: 2811 (3 skipped), 0 failed. `BrutalUi.Framework.Tests`: 212 (203 plus the 9 new). `BrutalUi.Core.Tests`: 1751, unchanged. The 9 new tests add to `BrutalUi.Framework.Tests` only.

- Demo: `tests/BrutalUi.Framework.Tests/bin/Debug/net10.0/BrutalUi.Framework.Tests --filter-class '*ModeHistoryTests'` reads total 9, `failed: 0`. The phase 1 and 2 classes hold: `UiManagerTests` 6, `ModeDiffTests` 7, `LayoutSwapTests` 5, `ModeActionMapTests` 5, `BeforeUpdateTests` 6, each `failed: 0`.

- `dotnet build brutal-ui.sln -nodeReuse:false`: 0 Warning(s), 0 Error(s).

- Diff scope of phase 3: `src/BrutalUi.Framework/Panels/ModeHistory.cs` (new) and `src/BrutalUi.Framework/Panels/UiManager.cs` (edited). The test add is `ModeHistoryTests.cs`. No file of `src/BrutalUi.Core/` changed. The Core diff stays the phase 1 `UiContext.cs` and `UiBeforeUpdateHandler.cs`. `SlotContainer.cs`, `PanelController.cs`, `ModeDefinition.cs`, `UiActionMap.cs`, each project file, `scripts/`, `samples/`, and each existing test file did not change.

- The first full-solution run hit the known flake (exit 134, Internal CLR error 0x80131506). The second run read 2811 (3 skipped), 0 failed. Gotcha 1 treats it as the flake.

- Plants: each one ran inside one chained shell call with a fresh `mkdir -p` of the scratch folder. Each plant log showed 0 `error CS` lines before the run read its result.

- Plant 8 (phase 3): weakened the push condition `!_targetFromBack`, so the apply of a back step also pushes. The break shape was `(_targetFromBack || !_targetFromBack)`, always true. Exit 2, 4 failed: `TheApplyOfABackStepAddsNoEntry` (expected), `BackWalksToTheFirstModeAndThenReturnsFalse`, `ASetModeAfterBackInOneFrameWins`, and `AFullHistoryDropsTheOldestMode`. They share the no-forward-step rule. The restore was equal. The re-test read 0 failed.

- Plant 9 (phase 3): made the full-ring branch of `Push` overwrite the newest entry, `_entries[(_head + _count - 1) % _entries.Length] = mode`, in place of the oldest. Exit 2, 1 failed: `AFullHistoryDropsTheOldestMode` (expected). The popped modes read D then A, in place of C then B. The restore was equal. The re-test read 0 failed.

### After Phase 4 Group 1 (2026-11-07)

- Solution: 2821 (3 skipped), 0 failed. `BrutalUi.Framework.Tests`: 222 (212 plus the 10 new). `BrutalUi.Core.Tests`: 1751, unchanged. The 10 new tests add to `BrutalUi.Framework.Tests` only.

- Demo: `tests/BrutalUi.Framework.Tests/bin/Debug/net10.0/BrutalUi.Framework.Tests --filter-class '*UiManagerRegisterTests'` reads total 8, `failed: 0`. `'*ModeDiffTests'` reads total 9, `failed: 0`. The earlier classes hold: `UiManagerTests` 6, `LayoutSwapTests` 5, `ModeActionMapTests` 5, `ModeHistoryTests` 9, `BeforeUpdateTests` 6, each `failed: 0`.

- `dotnet build brutal-ui.sln -nodeReuse:false`: 0 Warning(s), 0 Error(s).

- Diff scope of phase 4 group 1: `src/BrutalUi.Framework/Panels/UiManager.cs` (edited: the register checks, the `_applying` field and guard, `IsApplying`, `HoldsDuplicateSlot`, the `Apply` try/finally, and the remarks). `src/BrutalUi.Framework/Panels/ModeHistory.cs` (edited: the `Back()` applying guard and its remark only). New test file `tests/BrutalUi.Framework.Tests/Panels/UiManagerRegisterTests.cs`. Edited test file `tests/BrutalUi.Framework.Tests/Panels/ModeDiffTests.cs` (the gate consts, `TallFactory`, `Gate`, `NewGate`, `RunCycle`, `RunIdleFrame`, `RunBody`, and the two new tests). No file of `src/BrutalUi.Core/` changed. The Core diff stays the phase 1 `UiContext.cs` and `UiBeforeUpdateHandler.cs`. `SlotContainer.cs`, `PanelController.cs`, `ModeDefinition.cs`, `PanelDefinition.cs`, `UiActionMap.cs`, each project file, `scripts/`, `samples/`, and each other existing test file did not change. The group added no native dependency.

- Plants: each one ran inside one chained shell call with a fresh `mkdir -p` of the scratch folder. Each plant log showed 0 `error CS` lines before the run read its result. Each restored file compared equal. The run after each restore read 0 failed.

- Plant 10 (phase 4): `GC.KeepAlive(new object());` as the first line of `Apply`. Exit 2, 1 failed: `SetModeAndTheDiffAllocateNothingAfterWarmUp` (measured 9600 bytes: 4 applies per cycle, 24 bytes each, 100 iterations). `AFrameWithNoTargetAllocatesNothing` passed, as expected. The idle frame never enters `Apply`.

- Plant 11 (phase 4): the same line as the first line of the handler, before the check of the target. Exit 2, 2 failed: both 0-byte tests (`AFrameWithNoTargetAllocatesNothing` 2400 bytes, `SetModeAndTheDiffAllocateNothingAfterWarmUp` 9600 bytes).

- Plant 12 (phase 4): the same line as the first line of `SetMode`. Exit 2, 1 failed: `SetModeAndTheDiffAllocateNothingAfterWarmUp` (measured 7200 bytes: 3 `SetMode` calls per cycle). The idle test passed.

- Plant 13 (phase 4): the same line as the first line of `ModeHistory.Back()`. Exit 2, 1 failed: `SetModeAndTheDiffAllocateNothingAfterWarmUp` (measured 2400 bytes: 1 back step per cycle). The idle test passed.

- Plant 14 (phase 4): deleted the `_applying` check of `SetMode`. Exit 2, 1 failed: `SetModeBackAndRegisterFromTheDiffThrow` (expected `typeof(InvalidOperationException)`, actual `null`). The `SetMode` call of the probe inside the diff returned. No build error, because the other guards keep the reads of `_applying`.

### After Phase 4 Group 2 (2026-11-07)

- Tasks 4.5 to 4.7 changed the docs. The `DefaultSlot` remark in `PanelDefinition.cs`, the constructor parameter, and the property summary now state the real use. Section 4, section 10, and section 14 of the technical design state the mode set. The changelog carries the two Added lines and one Changed line.

- Diff scope: `src/BrutalUi.Framework/Panels/PanelDefinition.cs` doc lines only, `docs/technical-design.md`, `CHANGELOG.md`, and this file. `git diff` of `PanelDefinition.cs` shows changed `///` lines only. No code line changed. No test file changed.

- The Framework build after the doc edit: 0 Warning(s), 0 Error(s). The `<see cref="UiManager"/>` and `<see cref="ModePanel"/>` tags resolve, because both types are in `BrutalUi.Framework`.

- Gate (task 4.8): `MSBUILDDISABLENODEREUSE=1 timeout 600 scripts/test-linux.sh`. The build, test, gpu, and loader steps passed. The test step read 2821 (3 skipped), 0 failed. The smoke step failed with exit 1. The script exit was 1. The cause is a host fault, see "Blockers".

- Demos (task 4.8): each class run reads its total with `failed: 0`. `BeforeUpdateTests` 6, `UiManagerTests` 6, `ModeDiffTests` 9, `LayoutSwapTests` 5, `ModeActionMapTests` 5, `ModeHistoryTests` 9, `UiManagerRegisterTests` 8.

- STE lint (`--fail-over 2.5`): `docs/technical-design.md` 1.53 pass. `CHANGELOG.md` 1.62 pass. `implementation.md` 1.96 pass. This file passed after this rewrite.

- Blocker detail: `xvfb-run -a` starts Xvfb, and Xvfb segfaults in `InitExtensions` inside the NVIDIA EGL stack (`libnvidia-egl-gbm`, driver 595.99.02). GLFW then reports `PlatformUnavailable (X11: Failed to open display :109)`. A direct `Xvfb :111 -screen 0 1024x768x24` aborts the same way, with exit 134. A run with `-extension GLX` gives a live display, but Vulkan then finds no surface extension. HelloPanel reaches no frame either way. No repo change can fix it. A doc-only group cannot cause it.

- Decisions that the plan did not settle: the XML doc tags of task 4.5 use `<see cref>` for `UiManager` and `ModePanel`. The plan named the wording, not the tag style. The section 10 rule names follow the style of the slot-container subsection. The section 14 bullet keeps the two 0-byte tests out of the gate numbers, per Decision 10.

---

## Key Files

### Core Implementation

- `src/BrutalUi.Core/Tree/UiBeforeUpdateHandler.cs` - the new delegate type (phase 1).
- `src/BrutalUi.Core/Tree/UiContext.cs` - the `OnBeforeUpdate` slot, the guard flag, and the call in `Update` (phase 1).
- `src/BrutalUi.Framework/Panels/UiManager.cs` - the manager, the tables, the register methods, `SetMode`, the handler, and `Apply` (phases 1 to 4).
- `src/BrutalUi.Framework/Panels/ModeHistory.cs` - the fixed ring and `Back()` (phase 3).

### Related Files

- `docs/features/panels-and-modes/epic-overview.md` - the epic rollup, the confirmed items, and the build order.
- `docs/features/panels-and-modes/mode-set/implementation.md` - the plan: the Internal Shape, the Public API, the Decisions, and the Verification Method.

---

## Internal Shape and Integration Points (Phase 1 record of the plan)

The plan holds the full text. This section records the phase 1 parts.

### Internal Shape (phase 1)

- **The Core slot (Decision 2).** `UiContext` gets `public UiBeforeUpdateHandler? OnBeforeUpdate { get; set; }` and `private bool _beforeUpdate;`. `Update` keeps its check of `_binding` first. It then checks `_beforeUpdate` and throws `InvalidOperationException("A before-update handler must not call Update.")`. It then reads the slot into a local. When the local is not `null`, it sets `_beforeUpdate = true`, calls the handler with `deltaTime` in a `try` block, and clears the flag in the `finally` block. `DrainEvents()` comes after the call. `Update` reads `_roots` after the drain and the binding pass already, so the passes see the new roots. An exception of the handler leaves `Update` before the input pass. The ring keeps its entries. A `null` slot costs one field read and one compare.

- **The tables (Decision 4).** `UiManager` holds `PanelEntry[]`, `LayoutEntry[]`, and `ModeEntry[]`, each grown by doubling at register time. A `Placement` struct holds the panel entry, the cached `SlotContainer`, and the index in the slot. Placements follow the slot order of the mode, then the panel order inside a slot. Phase 1 holds `_current` and `_target` only. Phases 2 to 4 add `_shown`, `_targetFromBack`, `_applying`, and the rest.

- **Register (Decisions 4 and 5).** The panel register creates the controller with the `PanelContext` and returns it. The layout register calls `CreateRoot(Panels)` one time and keeps the root. The mode register resolves the layout name, then finds each slot with `SlotContainer.Find`, then resolves each panel name, and builds the placements. The name compares use `SequenceEqual` on the spans. Phase 1 throws `ArgumentException` for a name that it cannot resolve.

- **SetMode (Decision 3).** `SetMode` scans the modes for the name. An unknown name throws `ArgumentException` with `ParamName` `name` before a change. A hit sets `_target`. A second call replaces the target, so the last call wins.

- **The handler.** One delegate that the constructor creates: `private void OnBeforeUpdate(float deltaTime)`. It returns at once when `_target` is `null`. Else it calls `Apply()`.

- **Apply (phase 1: first mode only).** Mark each panel of the new mode (`Next = slot`), then add the layout root with `Context.AddRoot(target.Layout.Root)`, then enter `p.Slot.Mount(p.Panel.Controller.EnsureRoot(), p.Index)` for each placement in order and set `Slot`, then end with `current = target; target = null`. No leave pass, no layout swap, no map switch, and no history push yet (phases 2 and 3).

### Integration Points (phase 1)

| Sibling | Contract for phase 1 |
|---|---|
| core-tree | The slot runs first in `Update`, so the input, binding, style, and layout passes see the new tree in the same frame. The manager adds and removes only its layout roots. A root that the game adds stays. |
| panel-definition (done) | One controller per panel. `EnsureRoot()` builds the root at the first mount only (Q3). The manager reads the `ModeDefinition` getters, `LayoutDefinition.CreateRoot(panels)`, and the `ModePanel` names. |
| slot-container (done) | `Find` runs at register time and the manager caches each result. `Mount(root, index)` puts a new root at the index, moves a held root with no join, and throws for another parent. |

### Blast radius of the `UiContext.Update` change (traced before the edit)

- Callers of `Update`: the `BrutalUi.ImGui` integration, the samples (`HelloPanel`, `WorldSpace`), and every test tree. None of them set `OnBeforeUpdate` yet, so the new slot reads `null` and costs one field read plus one compare.

- `UiActionMap.Activate` writes `OnUnhandledInput` only. The two slots are independent. The order in `Update` is: the before-update slot, then the drain, then the binding pass. A mode change from an action therefore still applies at the start of the next `Update` (Q1 defer rule).

- Allocation gates: the new call is a direct delegate call with one local read. A `null` slot allocates nothing, so the existing gates keep their counts. The new `BeforeUpdateTests` gate proves the slot path itself.

- Re-entry guard: the `_beforeUpdate` flag follows the `_binding` pattern. An element handler that calls `Update` during the drain still hits the existing `_draining` rule inside `DrainEvents`. A before-update handler that calls `Update` hits the new guard.

---

## Important Decisions

The plan holds Decisions 1 to 12. Add a decision here only when the build changes or adds to the plan.

### Phase 1 build notes (no change to the plan)

- The manager holds the entry tables as three structs (`PanelEntry`, `LayoutEntry`, `ModeEntry`) plus the `Placement` struct of the plan, grown by doubling. The tables store mode indexes, not struct copies, in `_current` and `_target`. The value -1 means none.

- The manager keeps the delegate in a `readonly UiBeforeUpdateHandler _onBeforeUpdate` field, as `UiActionMap` keeps `_onInput`. Phase 2 can read it back when it checks the slot.

- The manager holds a `_shown` field (the layout entry index in the context, -1 for none). The plan keeps the full `_shown` swap for phase 2. The phase 1 layout-add rule ("add the layout root when the mode layout is not shown yet") needs this one field. Phase 1 never removes, so `_shown` only moves from -1 to a layout index.

- Phase 1 uses `deltaTime` in no code path. The handler signature keeps it (Decision 2), and the phase 1 handler body ignores it.

- The `Next` mark runs in phase 1 `Apply` as the plan states, even though the phase 2 leave pass is the reader.

### Phase 2 close-out

- Done in phase 2: `Apply` now holds the same-target return, the leave pass, the `_shown` swap with `Context.RemoveRoot`, and the map switch. The phase 1 throw from `SlotContainer.Mount` at a mode change is gone. `ModeDiffTests` and `LayoutSwapTests` pin the new paths.

### Phase 2 build notes (no change to the plan)

- `Apply` follows the plan end shape minus step 6 and the guards. Step 0 returns when `_target == _current` and clears `_target`. A target that equals the current mode changes nothing, and history has no entry yet. The mark loop clears `Next` on each panel entry, then sets it from the placements. The leave pass runs `slot.Unmount(e.Controller.Instance.Root)` under `e.Slot is { } slot && !ReferenceEquals(slot, e.Next)` and sets `Slot` to null. The layout step sits between the leave and the enter. Its `RemoveRoot` guard reads the `_shown` index (`_shown >= 0` means shown, per the index table rule). The enter sets `Slot`. The map step calls `Activate` always for a target map, also when the old mode holds the same map (A6). It calls `Deactivate` on the old map only when the target has no map and `_current >= 0`. No `_applying` and no `_targetFromBack` yet (phases 3 and 4).

- `ReferenceEquals` compares the slots. It avoids an operator call and allocates nothing.

- The leave pass reads the real slot of each panel, not the old placements (Decision 8). Plant 4 proved the collateral of a missing leave: the moves throw from `Mount`, and a swap leaves panels in the removed root.

- The layout swap unmounts every panel before `RemoveRoot`, because the leave pass of the same diff treats a slot of the old layout as "another slot" for every entry. The removed layout root therefore holds no panel.

- Blast radius (traced before the change): phase 2 edits `UiManager.Apply` and its remarks only. `UiContext`, `SlotContainer`, `PanelController`, `ModeDefinition`, and `UiActionMap` hold no edit. The new code calls three members it did not call before: `UiContext.RemoveRoot`, `SlotContainer.Unmount`, and `UiActionMap.Deactivate`. `UiContext.RemoveRoot` touches only the manager layout root. A root that the game adds stays in `Roots` (pinned by `TheManagerKeepsTheRootsOfTheGame`). `UiActionMap.Deactivate` clears the slot only when the map is active, so the two-modes-with-no-map rule holds (pinned by `TwoModesWithNoMapKeepTheSlotOfTheGame`).

### Phase 3 build notes (no change to the plan)

- `ModeHistory` is a sealed class, not a mutable struct like `EventQueue`. The manager owns the one instance. The history holds a back link to its manager (Notes conflict 2 of the plan). The class avoids the copy rule of the struct. The ring is a private `int[]` with `_head` and `_count`, in the shape of `EventQueue`.

- The internal seam: `ModeHistory.Push(int)` (called only by `Apply` step 6) and `UiManager.SetBackTarget(int)` (called only by `Back()`). `Back()` pops the newest entry first, then sets the target. A dropped entry stays dropped even when a later `SetMode` wins the frame.

- The constructor maps `HistoryCapacity` 0 to `DefaultHistoryCapacity` 16 before it builds the ring, as `UiContext` maps `EventQueueCapacity` 0. A negative value throws `ArgumentOutOfRangeException` with `ParamName` `info`, before any change, with a constant message.

- Step 0 of `Apply` (the same-target return) clears `_targetFromBack` too. After a `Back()` in the same frame, a `SetMode` of the current mode ends the frame with no diff, no entry, and no back flag left over.

- `Clear()` sets `_count` to 0 only. The ring holds `int` indexes, so no slot keeps an element reference, and the index of a mode never goes stale. A mode never unregisters.

### Phase 4 group 1 build notes (no change to the plan)

- The check-before-change order in `Register(LayoutDefinition)`: the null check, the applying guard, and the duplicate-name check all come before the factory call. A rejected layout never builds a root, and the name stays free. The root checks (null root, parent, duplicate slot name) come before the table write. An exception of the factory leaves the call with no wrap. `CreateRoot` runs outside any `try`.

- The duplicate slot-name check: `UiManager.HoldsDuplicateSlot(root, element)` walks the root in depth-first pre-order, stops at a `SlotContainer` without entering it, and throws for a slot where `SlotContainer.Find(root, slot.SlotName)` gives another instance. The root itself may be a slot, and the check then passes, because `Find` checks the root first. The check runs at register time only, so the walk cost sits at build time.

- The applying seam: `UiManager` holds `private bool _applying` and the internal read-only `IsApplying`. `Apply` sets it after the same-target return, in a `try/finally`, so the guard covers the mark, the leave, the layout swap, the enter, the map, and the history step, and a throw clears it. `ModeHistory.Back()` reads `_manager.IsApplying` before it pops an entry, so a back step inside the diff leaves the ring intact.

- `Register(ModeDefinition)` keeps the phase 1 name checks and adds the null, applying, and duplicate-mode-name checks before them. The check order in `SetMode` is: the applying guard, then the mode scan. An unknown name during the diff reports the diff rule.

- The two 0-byte tests sit in `ModeDiffTests` with the consts `Iterations` 100, `WarmUpRounds` 2, `CycleSum` 17, and `IdleFrameSum` 3. One cycle measures 10 slot children, 4 current-mode hits, and 3 active-map hits. The cycle pushes 3 history entries and pops 1, so the ring of 16 fills inside the measured region, and a full ring adds nothing to the count. The measured bodies use `u8` literals, `ReferenceEquals`, and the slot `Children.Count` reads only: no lambda, no string, no array, and no `Assert`.

- The tests run the full `UiContext.Update(0f)`, so they cover the style and layout passes after a swap, not only the diff.

### Follow-up work for phase 4

- ~~`Register(ModeDefinition)` does not check the layout root yet (no check that the root is a detached element), and no check rejects duplicate names of a layout, a panel, or a mode (phase 4).~~ Done in phase 4 group 1: the layout root check sits in `Register(LayoutDefinition)`, where the register builds the root, and each overload now rejects a duplicate name.

- ~~The error rule needs its `_applying` guard and its `SetMode` throw in phase 4.~~ Done in phase 4 group 1: the guard, the three throws, `UiManagerRegisterTests`, and plants 10 to 14.

---

## Gotchas and Learnings

### 1. The build and test commands need the MSBuild guard

**Problem:** Stale MSBuild nodes on this machine hang `dotnet` commands.

**Solution:** Set `MSBUILDDISABLENODEREUSE=1` on every `dotnet` command, add `-nodeReuse:false` to `dotnet build`, and never run `dotnet build-server shutdown`.

**Lesson:** Known flakes: a test host can exit 134, and `TheCurvesAllocateNothing` failed once. Run the tests a second time before treating either as a fault.

### 2. The /tmp scratch folder does not persist between shell calls

**Problem:** On this harness, each bash tool call runs in a fresh shell with its own `/tmp` view. A file written to `/tmp/brutal-ui-plants/mode-set/` in one call is gone in the next call.

**Solution:** Run each plant as one chained call: copy the file to the scratch folder, break it, run the full test command against the log in the same folder, read the exit code, restore the file, and check it with `cmp`. All of it runs inside that one call.

**Lesson:** Every plant call must create the folder itself. One phase 2 call reused the folder from the call before it. The copy failed, and the planted file lost its backup. The repair went through the edit tool and one more full test run. Start each plant call with `mkdir -p` and one `cp`, and check the marker lines before you read a plant result.

### 3. A plant that deletes the only read of a field fails the build, not the test

**Problem:** Plant 8 first replaced `if (_current >= 0 && !_targetFromBack)` with `if (_current >= 0)`. That removed the only read of `_targetFromBack`, CS0414 fired, and `TreatWarningsAsErrors` turned the build into an error. The run carried no test result at all.

**Solution:** Write the break so it keeps reading the field: `(_targetFromBack || !_targetFromBack)` is always true and is the same semantic break.

**Lesson:** Check the `error CS` count of the plant log before you read its test result. A failed build leaves the old test binary in place (gotcha 2 first sentence covers the same trap from the other side).

### 4. Xvfb crashes inside the NVIDIA EGL stack on this host (phase 4 group 2)

**Problem:** The smoke step of `scripts/test-linux.sh` fails with `GLFW init failed: PlatformUnavailable (X11: Failed to open display :109)`.

**Solution:** None in the repo. A direct `Xvfb :111 -screen 0 1024x768x24` segfaults in `InitExtensions` inside `libnvidia-egl-gbm` and `libEGL_nvidia` (driver 595.99.02), with exit 134. The `-extension GLX` workaround keeps the display alive, but then Vulkan finds no surface extension.

**Lesson:** A host fault can block the gate PASS line while the code is clean. Record the split evidence (build, test, gpu, loader pass) and ask the owner for a repaired host.

---

## Testing Approach

### Unit and integration tests

- `tests/BrutalUi.Core.Tests/BeforeUpdateTests.cs` (6, phase 1).
- `tests/BrutalUi.Framework.Tests/Panels/`: `UiManagerTests` (6, phase 1), `ModeDiffTests` (9: 7 from phase 2, plus the two 0-byte tests from phase 4), `LayoutSwapTests` (5, phase 2), `ModeActionMapTests` (5, phase 2), `ModeHistoryTests` (9, phase 3), `UiManagerRegisterTests` (8, phase 4).

### Plants

- Plants 1 to 3 (phase 1, task 1.6), 4 to 7 (phase 2, task 2.7), 8 to 9 (phase 3, task 3.4), 10 to 14 (phase 4, task 4.4). Each plant: copy the file to the scratchpad, break it, run the full solution test, expect a non-zero exit, restore, and check with `cmp`.

### Manual Testing

- None. The feature has no visual result. panel-sample shows two modes in HelloPanel.

---

## Needs Human Verification

`/autorun-feature` adds one line per item that the verifier cannot check, for a person to test.

- [ ] Phase 1 demo: a developer runs `tests/BrutalUi.Framework.Tests/bin/Debug/net10.0/BrutalUi.Framework.Tests --filter-class '*UiManagerTests'` after a build and reads a total of 6 with `failed: 0`.

- [ ] Phase 2 demo: a developer runs the built Framework test binary with `--filter-class '*ModeDiffTests'`, `'*LayoutSwapTests'`, and `'*ModeActionMapTests'` and reads the totals 7, 5, and 5, each with `failed: 0`. (`ModeDiffTests` reads 9 after phase 4.)

- [ ] Phase 3 demo: a developer runs the built Framework test binary with `--filter-class '*ModeHistoryTests'` and reads a total of 9 with `failed: 0`.

- [ ] Phase 4 group 1 demo: a developer runs the built Framework test binary with `--filter-class '*UiManagerRegisterTests'` and `'*ModeDiffTests'` and reads the totals 8 and 9, each with `failed: 0`.

- [ ] Phase 4 group 2 demo: a developer runs the seven class filters of task 4.8 and reads the totals 6, 6, 9, 5, 5, 9, and 8, each with `failed: 0`.

- [ ] Smoke gate: a developer re-runs `MSBUILDDISABLENODEREUSE=1 scripts/test-linux.sh` on a repaired host and reads the PASS line with exit 0.

---

## Next Steps

### Immediate (This Session)

1. ~~Phase 1 group 1: tasks 1.1 to 1.6 with the ADVANCED agent.~~ Done.
2. ~~Phase 2 group: tasks 2.1 to 2.7 with the ADVANCED agent (the mode change).~~ Done.
3. ~~Phase 3 group: tasks 3.1 to 3.4 (the history and the back step, STANDARD tier).~~ Done.
4. ~~Phase 4 group 1: tasks 4.1 to 4.4 (the register checks, the error rule, the 0-byte tests, and plants 10 to 14, STANDARD tier).~~ Done.
5. ~~Phase 4 group 2: tasks 4.5 to 4.8 (the `DefaultSlot` remark, the technical design, the changelog, and the full gate with the STE lint, STANDARD tier).~~ Done, with the smoke-step blocker.

### After This Feature

1. The owner repairs the host display stack or accepts the split evidence, then the gate prints its PASS line.
2. panel-sample (the next epic feature) shows two modes in HelloPanel and owns the numbered gate `ModeDiffAllocationTests`.

---

## Open Questions

- [ ] **Q:** Which demo does phase 1 use? **A:** Option (A), the Demo line of the plan: the `UiManagerTests` class run reads 6 with `failed: 0`.

- [ ] **Q:** How does the epic use `PanelDefinition.DefaultSlot`? **A:** Option (A): keep the API, and let the doc comments state the loader use (Decision 11, task 4.5).

- [ ] **Q:** When does the manager build a layout root? **A:** Option (A): at `Register(LayoutDefinition)` (Decision 5).

- [ ] **Q:** Who writes `OnBeforeUpdate`? **A:** Option (A): the `UiManager` constructor (Decision 12).

Phase 4 group 1 opened no new question. The public API of the register checks, the applying guard, and the two 0-byte tests follows the plan. The build changed nothing of it.

- [x] **Q:** The smoke step of the phase 4 gate fails on a host Xvfb crash. Does the owner repair the display stack and re-run `scripts/test-linux.sh`, or accept the split evidence (build, test, gpu, loader pass, and the seven demo runs)? The repo cannot fix it, and task 4.8 asks for the PASS line.
      **A:** The owner accepted the split evidence (2026-11-08 02:50, /autorun-feature question): the run continues; the smoke step is a `Demo: MANUAL` result; the "Smoke gate" line under "Needs Human Verification" holds the re-run duty on a repaired host.

---

## Session Notes

### 2026-11-08 03:12

- The review verifier returns PASS, with Demo: MANUAL. Its gates: build 0 Warning(s) and 0 Error(s); solution test total 2821, failed 0, skipped 3; the seven class runs 6, 6, 9, 5, 5, 9, 8, each `failed: 0`; the Q4 grep prints no line; `ASetModeAfterBackInOneFrameWins` pins both frame orders; the scope holds exactly the 12 expected paths. The `scripts/test-linux.sh` smoke step fails only with the accepted host fault (`GLFW init failed: PlatformUnavailable (X11: Failed to open display :109)`). The inbox has no open line.
- The feature is complete: 25/25 tasks, `code-review.md` Resolved (2 findings, 2 fixed, none left with a reason), `implementation.md` Ready for Review. The run makes no commit and no push.

### 2026-11-08 03:04

- Review G1 (CR-2) built by the STANDARD agent. `ASetModeAfterBackInOneFrameWins` in `ModeHistoryTests.cs` now pins both frame orders in one test. The back-then-set case and its `History.Count` 2 assert (the reviewer's fix text, already present) fail a stale back flag. The new set-then-back case (the reverse order of the finding title) covers the rest: start at C with history [A, B]; `SetMode(D)`, then `Back()`, then `Update`. It asserts `CurrentMode` B, `TargetMode` null, and `History.Count` 1: the back step wins, and its apply adds nothing. The class keeps 9 tests; the file adds no test method.
- Clean gate: the build read 0 Warning(s), 0 Error(s); the full suite read 2821, 0 failed; the `--filter-class '*ModeHistoryTests'` run read total 9, `failed: 0`.
- Plant (remove the `_targetFromBack = false;` line of `SetMode`, line 519 of `UiManager.cs`): exit 2, 1 failed: `ModeHistoryTests.ASetModeAfterBackInOneFrameWins`, `History.Count` Expected 2, Actual 1 (the assert of the back-then-set case; `ModeHistoryTests.cs:346` in the log). The plant log showed 0 `error CS` lines before the run read the result. One chained call started with `mkdir -p /tmp/brutal-ui-plants/mode-set/`. The file's backup lived in `artifacts/plants/mode-set/` for that call because `/tmp` does not persist between calls and the break ran through the edit tool; the call copied it back, `cmp` read equal, the re-test read 2821, 0 failed, and the folder was removed after. No other test failed under the plant. The CR-2 box in `code-review.md` stays open for the parent.
- Review G2 (CR-1) reworded the `//` comment at line 258 of `UiManager.cs` to close with "as UiContext maps it for its queue." No code line changed. The step-5 gate grep over `UiManager.cs` and `ModeHistory.cs` then printed no line, the build read 0 Warning(s), 0 Error(s), and the full suite read 2821, 0 failed. The CR-1 box in `code-review.md` stays open for the parent.

### 2026-11-07 20:10

- Phase 4 group 2 built by the STANDARD agent (tasks 4.5 to 4.8). The doc edit of `PanelDefinition.cs` holds `///` lines only. The technical design gained the before-update slot line (section 4), the frame-order sentence, the fixed Components row, the `### Mode set` subsection (section 10), and the `**Mode tests.**` bullet (section 14). The changelog gained two Added lines and one Changed line.
- The gate ran the build, test, gpu, and loader steps clean (2821 total, 3 skipped, 0 failed). The smoke step failed on the host Xvfb crash (gotcha 4). The seven demo runs read 6, 6, 9, 5, 5, 9, and 8, each with `failed: 0`.
- The STE lint passed on `docs/technical-design.md` (1.53), `CHANGELOG.md` (1.62), and `implementation.md` (1.96) on the first run. This file first read 3.31 per 100 words. The rewrite (semicolons out, short sentences, no possessive apostrophes) brings it under 2.5.

### 2026-11-07 19:58

- Phase 4 group 1 built by the STANDARD agent (tasks 4.1 to 4.4). The agent finished all four tasks. See the counts, the demo, and the plant results above. `UiManager` holds the register checks, the `_applying` guard, `IsApplying`, and `HoldsDuplicateSlot`. `ModeHistory.Back()` holds its guard. `UiManagerRegisterTests` adds 8 tests, and `ModeDiffTests` adds the two 0-byte tests (total 9). No file of `src/BrutalUi.Core/` changed, and the group added no native dependency.
- No plant hit the compile trap (gotcha 3): each log showed 0 `error CS` lines before the run read the result. Each plant ran inside one chained call that started with `mkdir -p` of the scratch folder (gotcha 2).
- Process note for the next group: one `tasks.md` update in this session went through a shell here-doc instead of the edit tool. The file content is correct; do not repeat the pattern.

### 2026-11-07 19:21

- Phase 3 group 1 built by the STANDARD agent (tasks 3.1 to 3.4). The agent finished all four tasks. See the counts, the demo, and the plant results above. `ModeHistory` holds the ring and `Back()`. `UiManager` holds `HistoryCapacity`, the `History` property, `SetBackTarget`, and the step-6 push. No file of `src/BrutalUi.Core/` changed.
- Plant 8 first failed to compile (gotcha 3): the break deleted the only read of `_targetFromBack`, and CS0414 became a build error. The file restored equal in the same call, the suite re-tested 0 failed, and the plant reran with a read-preserving break. The first full-solution test run hit the known CLR flake; the second run passed.

### 2026-11-07 19:10

- Phase 2 group 1 built by the ADVANCED agent (tasks 2.1 to 2.7). The agent finished all seven tasks. See the counts, the demo, and the plant results above. `UiManager.Apply` now holds the five-step diff of the plan minus the history push. No file of `src/BrutalUi.Core/` changed.
- One plant call hit gotcha 2 in a new form: it reused the scratch folder from the call before, so its backup copy failed after the break had applied. The repair: read the planted line, restore it with the edit tool, run the full suite clean, then redo the plant in one call that starts with `mkdir -p`.

### 2026-11-07 18:55

- Phase 1 group 1 built by the ADVANCED agent (tasks 1.1 to 1.6). The agent finished all six tasks. See the counts and the plant results above.
- `/tmp` does not persist between shell calls on this harness. Each plant ran its copy, break, full test, restore, `cmp`, and re-test inside one chained call.

### 2026-11-07 18:35

- `/autorun-feature panels-and-modes/mode-set` started the run on branch `feat/panels-and-modes/mode-set` (from `main`, which holds panel-definition and slot-container).
- `/start-feature` created `tasks.md`, `inbox.md`, and this file. `implementation.md` reads In Progress.
- The untracked `docs/features/input/` folder (a planned sibling epic) is on this branch worktree; the run does not touch it.

---

*Update this file at the end of each work session. Run `/update-feature` before compacting conversations.*
