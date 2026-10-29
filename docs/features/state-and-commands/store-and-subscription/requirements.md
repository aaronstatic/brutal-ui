# Store and Subscription - Requirements

**Epic:** state-and-commands
**Created:** 2026-10-29

## Overview

This feature opens `BrutalUi.Framework` with `Store<T>` and `StoreSubscription`, and it adds the `IBindingSource` contract to `BrutalUi.Core`. A store holds one state value and a version counter, so a binding pass can pull a change without a callback. It comes first in the state-and-commands epic because each binding and the sample read a store.

## Requirements

- `IBindingSource` lives in `BrutalUi.Core`. It exposes the version counter and nothing that names a Framework type. (Q1)
- `Store<T>` lives in `BrutalUi.Framework` and implements `IBindingSource`. It holds one value of `T`, and `Set` raises the version by one when the value changes.
- `StoreSubscription` records a store, a selector, and the last version it applied. A later feature puts it into a pooled list on `Element`.
- `Set` allocates nothing after warm-up, and a version read allocates nothing.
- `Store<T>` uses a `required` create-info struct for the first value, per the project rules.
- The store replaces `INotifyBindablePropertyChanged` from mosaic-ui. No event and no `INotifyPropertyChanged` in the frame loop.
- Tests in `tests/BrutalUi.Framework.Tests/` cover `Set`, the version counter, an equal value that raises no version, and a zero-byte check on `Set`. The epic makes no new test project. (A2)

## Dependencies

- The core-tree epic: `Element` and the allocation gate helpers in `tests/BrutalUi.Core.Tests/`.
- The dev-ops epic: the solution, `src/BrutalUi.Framework/`, and `tests/BrutalUi.Framework.Tests/`.
- No sibling feature. This feature starts the epic.

## Out of Scope

- The pooled binding list on `Element`, the binding pass, and `Label.BindText`. element-bindings owns them.
- A store of a collection, a derived store, or a selector chain. Phase 4 (`DataList`) decides them.
- Persistence of a store value to a file.
