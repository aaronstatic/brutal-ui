# Binding Attributes - Requirements

**Epic:** markup
**Created:** 2026-11-12

## Overview

This feature connects an XML template to the Framework layer. It adds the `command` attribute and the `bind-text` attribute on the attribute hook of the XML loader. `BrutalUi.Framework` gets a reference to `BrutalUi.Markup`. After this feature, a template declares its command and its store bindings, and the panel code supplies the typed parts.

## Requirements

- `BrutalUi.Framework` references `BrutalUi.Markup`. Markup stays Core only. (Q1)
- `command="name"` on a `Button` calls `BindCommand` with the `CommandRegistry` of the load. (Q1, A6)
- `bind-text="key"` on a `Label` or a `TextField` binds the element to a store through a selector with that key. The panel code registers a typed selector for each key before the load. (Q4, A6)
- The same key form covers the other widget bindings of `WidgetBindings`: the checked state of a `Toggle` and the value of a `Slider`.
- A key with no registered selector, a `command` on an element that is not a `Button`, and a selector of the wrong type each give a load error with a line and a column.
- A bound element from a template behaves the same as an element that the C# bind methods bind. A test compares the two.
- A tree with these attributes allocates nothing in `Update` after warm-up. (A10)
- Tests in `tests/BrutalUi.Framework.Tests/` cover each attribute and each error. Sections 10 and 11 of `technical-design.md` get the attributes.

## Dependencies

- xml-element-loader must be complete. It gives the attribute hook and the error type.
- builder-api gives the `Command` method and the query by name.
- state-and-commands gives `BindCommand`, `Store<T>`, `LabelBindings`, and `WidgetBindings`.
- This feature can build in parallel with element-generator.

## Out of Scope

- A binding expression language in the template. The selector is C# code.
- A two-way binding syntax. A widget writes to a store through its delegate slot, as today.
- A binding by reflection on a property path.
- The panel, layout, and mode records. definition-loader reads them.
