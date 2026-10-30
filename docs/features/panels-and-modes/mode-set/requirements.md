# Mode Set - Requirements

**Epic:** panels-and-modes
**Created:** 2026-10-30

## Overview

This feature adds `UiManager`, the mode diff, `ModeHistory`, and the action map switch of a mode. It is the center of the epic. It adds one slot to `BrutalUi.Core`, `UiContext.OnBeforeUpdate`, and the manager applies the diff there.

## Requirements

- `UiContext.OnBeforeUpdate` is a new slot in Core. `Update` calls it first, before the input pass. The game or the manager sets it with `=`, as `OnUnhandledInput` is set (A2). A call of `Update` from the slot throws `InvalidOperationException`.
- `UiManager` holds the registered `PanelDefinition`, `LayoutDefinition`, and `ModeDefinition` records, one `PanelController` for each panel, the current mode, and the target mode. `Register` runs at build time and may allocate.
- `SetMode(name)` records the target mode and returns at once (Q1). An unknown name throws `ArgumentException`. The slot applies the diff at the start of the next `Update`. Two `SetMode` calls in one frame apply the last one.
- The diff compares the current mode against the target mode. A panel that only the old mode holds unmounts. A panel that only the new mode holds mounts, with a root that the controller creates one time (Q3). A panel that both modes hold in the same slot stays in place. A panel that moves to a new slot unmounts and mounts (A5).
- A mode with another layout swaps the layout root: the manager removes the old root from the context, adds the new root, and mounts each panel of the new mode. The layout root stays the one `UiContext` root (A4).
- The diff calls `Activate` on the `UiActionMap` of the new mode, or `Deactivate` on the map of the old mode when the new mode has none (A6).
- `ModeHistory` records each applied mode. `Back()` sets the previous mode as the target and drops it from the history. There is no forward step (A3). A `Back()` on an empty history returns `false`.
- `SetMode`, the diff, and a frame with no target allocate 0 bytes after warm-up (A7). The manager rents each list at build time.
- Tests in `tests/BrutalUi.Framework.Tests/` cover the slot order, the diff cases, the layout swap, the map switch, the history, and the exceptions. A Core test covers `OnBeforeUpdate`.

## Dependencies

- panel-definition for the records and `PanelController`.
- slot-container for the mount, the unmount, and the lookup.
- state-and-commands for `UiActionMap.Activate` and `Deactivate`.
- core-tree for `UiContext.Update`, `AddRoot`, and `RemoveRoot`.

## Out of Scope

- The HelloPanel sample and the allocation gate. panel-sample owns them.
- A forward step in the history (A3).
- A transition of a panel on enter or leave (A9).
- Window chrome and a window manager. Phase 4 owns them.
