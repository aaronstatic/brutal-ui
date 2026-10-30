# Panel Definition - Requirements

**Epic:** panels-and-modes
**Created:** 2026-10-30

## Overview

This feature adds the data model of the panels-and-modes epic to `BrutalUi.Framework`: the definition records and the panel controller. Each later feature of the epic reads these types. It owns no tree change and no frame order change.

## Requirements

- `PanelDefinition` names a panel, holds a root factory, and names a default slot. The names are UTF-8.
- `LayoutDefinition` names a layout and holds a factory for the layout root (Q4). The root holds the `SlotContainer` elements of the layout.
- `ModeDefinition` names a mode, names its layout, lists the panels of each slot in order, and holds one optional `UiActionMap` (A6).
- `PanelController` owns one `PanelInstance`. The controller creates the root one time through the factory at the first mount, and keeps it for the life of the manager (Q3). `PanelInstance.Root` is an `Element`.
- A controller reaches the `CommandRegistry`, the `EventBus`, and the `ServiceRegistry` through a context object that the game gives at build time.
- A definition is a plain C# record. A build-time construction may allocate. No method of the records or the controller allocates after warm-up.
- Tests in `tests/BrutalUi.Framework.Tests/` cover each record, the one-time root creation, and a factory that throws.

## Dependencies

- state-and-commands for `CommandRegistry`, `EventBus`, `ServiceRegistry`, and `UiActionMap`.
- core-tree for `Element`.
- It can be built first. slot-container can start in parallel after the shape of `PanelInstance` is set.

## Out of Scope

- `SlotContainer` and the mount of a root. slot-container owns them.
- `UiManager`, the diff, and `ModeHistory`. mode-set owns them.
- An XML loader for the records. The xml-markup epic owns it.
