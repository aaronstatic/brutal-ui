# Element Bindings - Requirements

**Epic:** state-and-commands
**Created:** 2026-10-29

## Overview

This feature binds an element to a store slice. It adds the pooled binding list to `Element`, the binding pass to `UiContext.Update`, `Label.BindText`, and a value binding for each widget of the interactive-elements epic. It is the one change to the frame order in the state-and-commands epic.

## Requirements

- A binding is a struct in a pooled list on `Element`. The list grows at build time only.
- `Label.BindText(store, selector)` binds the text of a label to a store slice. A `Toggle`, a `Slider`, and a `TextField` each get a value binding on the same pattern.
- The binding pass runs in `UiContext.Update` before the style pass. It reads each store version, applies a changed value, and marks the element dirty. The game calls no apply method. (Q2)
- A text binding formats a number into the label buffer through a span formatter. No `ToString` runs in the frame loop. (A4)
- A widget with a value binding and a delegate slot does not loop: a change from the store does not call the delegate slot of the widget back into the store.
- A new gate `BoundTreeAllocationTests` puts a 1000-element tree with a changing store into the 0-byte check, and the test scripts run it. (A3)
- An element that leaves the tree drops its bindings, and a store with no binding keeps no reference to the element.

## Dependencies

- store-and-subscription must be complete: `IBindingSource`, `Store<T>`, and `StoreSubscription`.
- The interactive-elements epic: the delegate slot on each widget and `Label.SetText`.
- The core-tree epic: the dirty flags and `UiContext.Update`.
- It can be built in parallel with commands-and-events.

## Out of Scope

- A two-way binding helper that writes a widget change back to the store. The game sets the delegate slot for that in this epic.
- A binding to a panel root or a mode. The panels-and-modes epic owns it.
- A binding set from XML. The xml-markup epic owns it.
