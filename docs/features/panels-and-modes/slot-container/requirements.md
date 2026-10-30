# Slot Container - Requirements

**Epic:** panels-and-modes
**Created:** 2026-10-30

## Overview

This feature adds `SlotContainer`, the `Element` that holds the panel roots of one slot. It owns the mount and unmount of a panel root, and the lookup of a slot by name in a layout root. mode-set calls these operations from the diff.

## Requirements

- `SlotContainer` is an `Element` subtype in `BrutalUi.Framework` with a UTF-8 slot name.
- A slot holds an ordered list of panel roots as its children, in the order of the mode definition (A4). A mount attaches the root as a child at a given index. An unmount detaches the root and keeps the root and its subtree alive.
- A mount of a root that another slot holds throws `InvalidOperationException`. An unmount of a root that the slot does not hold returns `false`.
- A static lookup finds a `SlotContainer` by name in a layout root. A missing name returns `null`. The lookup runs at build time and at the first diff, so it may walk the tree. The diff caches each result.
- The mount, the unmount, and the cached lookup allocate nothing after warm-up.
- The style and layout passes treat a `SlotContainer` as a plain `Element`. A stylesheet styles it by type name, class, or name.
- Tests in `tests/BrutalUi.Framework.Tests/` cover the order of the children, the detach that keeps state, the duplicate mount, and the lookup.

## Dependencies

- panel-definition for `PanelInstance.Root`.
- core-tree for `Element`, the child list, and the dirty flags.
- It can be built in parallel with the records of panel-definition.

## Out of Scope

- The diff that decides which root mounts or unmounts. mode-set owns it.
- A transition of a panel on enter or leave (A9).
