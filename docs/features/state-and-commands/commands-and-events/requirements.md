# Commands and Events - Requirements

**Epic:** state-and-commands
**Created:** 2026-10-29

## Overview

This feature adds `EventBus`, `ServiceRegistry`, and `CommandRegistry` to `BrutalUi.Framework`, and a command bind on `Button`. A button click and an action map key call the same registry by a command name. The three registries belong in one feature because each one is a small copy from mosaic-ui on the same pattern. (Q4)

## Requirements

- `EventBus` publishes a typed event to each subscriber. The publish path allocates nothing after warm-up.
- `ServiceRegistry` registers and resolves a service by type. A missing service returns null or a result value, and it throws nothing in the frame loop.
- `CommandRegistry` maps a UTF-8 command name to a static handler plus a target, like the event slots on `Element`. (Q3)
- The invoke path allocates nothing after warm-up. An unknown name reports through a result value or a slot, with no exception in the frame loop.
- A command bind on a `Button` connects `Button.Clicked` to a command name. The bind runs at build time and may allocate once. (A5)
- Both copies follow the mosaic-ui design and keep the same names, with `INotifyBindablePropertyChanged` removed.
- Tests in `tests/BrutalUi.Framework.Tests/` cover publish, subscribe, resolve, register, invoke, the unknown name, and a zero-byte check on invoke and publish.

## Dependencies

- store-and-subscription must be complete, for the test project pattern and the first Framework types.
- The interactive-elements epic: `Button.Clicked` and the other delegate slots.
- It can be built in parallel with element-bindings.

## Out of Scope

- `UiActionMap` and the gamepad feed. action-map owns them.
- `CanExecute` on a command and a disabled state on a bound button. A later feature adds it when a panel needs it.
- `ModeHistory` and mode commands. The panels-and-modes epic owns them.
