# Markup - Epic Overview

**Epic:** markup
**Status:** In Progress (1/6 features complete)
**Last Updated:** 2026-11-12 10:40

> **This file is the epic marker.** Its presence in `docs/features/markup/` tells every Beast Mode command that this folder is an **epic**, not a plain feature. Keep it present and keep the required sections below filled in. The master `docs/overview.md` tracks this epic as a **single row**. The per-feature detail lives **here**.

---

## Purpose

This epic gives a developer two paths to one element tree: a C# builder API and an XML template. Today `BrutalUi.Markup` holds only the stylesheet parser, and `ElementBuilder` holds only `Class` and `Child`. A game builds each screen with object initializers and property writes. The epic adds the fluent methods, a query by name, the XML element loader, a source generator for the element table, the `command` and `bind-text` attributes, and the XML form of the panel, layout, and mode records.

These features belong together because of the rule "markup equals code": each element feature works from XML and from C#. The builder sets the property surface. The loader and the generator map XML to that surface. The Framework attributes and the record loader connect a template to a store, a command, and a mode. The epic closes Phase 3 and completes the target outcome of the mission statement: a store, a panel controller, and an XML template give a bound panel on the screen.

---

## Features

The constituent features of this epic, in build order. Each feature is a subfolder under `docs/features/markup/`.

| # | Feature | Status | Tasks | Description |
|---|---------|--------|-------|-------------|
| 1 | builder-api | Complete | 33/33 (100%) | The fluent build methods for each built-in element type, and a query that finds an element by name. Ready for Review on `feat/markup/builder-api`. |
| 2 | xml-element-loader | Planned | — | The XML template loader in `BrutalUi.Markup` on `XmlReader`: the type registry, a written table for the built-in types, the attribute hook, the errors with a line and a column, and the tree compare test. |
| 3 | element-generator | Planned | — | A Roslyn source generator that writes the table rows for the built-in types and for a custom type that a game marks with an attribute. |
| 4 | binding-attributes | Planned | — | The `command` and `bind-text` attributes in `BrutalUi.Framework`, with a typed selector that the panel code registers for each key. |
| 5 | definition-loader | Planned | — | The panel, layout, and mode records from XML, registered through `UiManager.Register`. |
| 6 | markup-sample | Planned | — | A HelloPanel panel from an XML template with a store binding and a command, the smoke line, and the allocation gate for a loaded tree. |

> Reference any feature with the slash form `markup/<feature-name>`. Task counts come from each feature's own `tasks.md`, and `/update-epic` refreshes them.

---

## Build Order / Dependencies

1. **builder-api** - It comes first because it sets the property surface that the XML attributes follow. It also gives the query by name that the panel code and the tests use. It is complete and Ready for Review on `feat/markup/builder-api`. xml-element-loader inherits four things from it: the property surface (43 `ElementBuilder` methods, `Command`, `Find`, and `Find<T>`), the 12 value names as the table rows, `tests/BrutalUi.Core.Tests/Shared/TreeCompare.cs` with the link line for `BrutalUi.Markup.Tests`, and the rule that a row for `value` runs after the rows for the range. The builder-api review left one note in the loader requirements: `Capacity` and `ScrollSpeed` exist only in the create info (A6), so they have no builder method, no public setter, and no table row.
2. **xml-element-loader** - It depends on the property surface of #1. It ships a written table for the built-in types, so the generator does not block the loader.
3. **element-generator** - It depends on the table shape of #2 and replaces the written table. It is the hardest feature, because it adds a new project and a package layout for an analyzer.
4. **binding-attributes** - It depends on the attribute hook of #2. It adds the reference from Framework to Markup.
5. **definition-loader** - It depends on #2 for the reader and on #4 for a template with bindings.
6. **markup-sample** - It depends on #1 to #5 and closes the epic and Phase 3.

**Dependencies between features:**
- builder-api → xml-element-loader (the property surface and the tree compare test)
- xml-element-loader → element-generator (the table shape)
- xml-element-loader → binding-attributes (the attribute hook)
- xml-element-loader → definition-loader (the reader and the template registry)
- binding-attributes → definition-loader (a panel template with bindings)
- element-generator and binding-attributes have no dependency between them. They can build in parallel after #2.
- External: `Microsoft.CodeAnalysis.CSharp` for the generator project, pinned in `Directory.Packages.props`. panels-and-modes is Ready for Review and gives the records.

**Confirmed interview items that shape the order:**
- Q2 A Roslyn source generator makes the table in this epic.
- Q5 The panel, layout, and mode records load from XML.
- A1 The epic has six features in this order: builder-api, xml-element-loader, element-generator, binding-attributes, definition-loader, markup-sample.
- A3 xml-element-loader ships a written table for the built-in types. element-generator then replaces it, so the hardest feature does not block the loader.
- A8 No existing feature moves into this epic. `StyleSheetParser` stays in style-system.

---

## Integration & Architecture

- **Within the epic:** builder-api adds the fluent methods to Core and the `Command` method to Framework. xml-element-loader reads a template with `XmlReader`, finds each type in the registry, and sets each attribute through a table row. An attribute that the table does not hold goes to the attribute hook. element-generator writes the rows at build time. binding-attributes registers `command` and `bind-text` on the hook. definition-loader reads the records and gives them to `UiManager.Register`.
- **With other epics / features:** core-tree gives `Element` and `ElementBuilder`. interactive-elements gives the widget types. style-system gives `StyleParseError` as the model for an error. state-and-commands gives `BindCommand`, `Store<T>`, and the element bindings. panels-and-modes gives the records and `UiManager.Register`. The Phase 4 epics register `ListView`, `DataList`, `WindowChrome`, and `WindowDefinition` on the same registry.
- **Key architectural decisions for the epic as a whole:**
  - Q1 Framework references Markup. Markup stays Core only and gives an attribute hook. Framework adds `command` and the definition loader.
  - Q2 A Roslyn source generator makes the table in this epic.
  - Q3 The element loader uses `System.Xml.XmlReader`.
  - Q4 The epic adds a query by name and a named bind attribute.
  - A4 The generator is a new project, `BrutalUi.Generators`, and ships inside the `BrutalUi.Markup` package as an analyzer. It adds no native dependency.
  - A10 Load-time code can allocate. The 0-byte gate covers `Update` and `Draw` on a loaded tree.

---

## Tech Debt / Findings

- (none yet. `/review-epic` adds the cross-feature findings here.)

---

## Master Overview Rollup

- **Rollup status:** In Progress (1/6 features complete, 33/33 tasks done in feature 1)
- **One-line summary for master:** builder-api (33/33) is Ready for Review on `feat/markup/builder-api`: 40 new fluent methods on `ElementBuilder` (43 total) for the eight built-in types, `Command` in Framework, and `Find` plus `Find<T>` on `Element` at 0 bytes. 44 new tests, solution 3032 with 0 failed and 3 skipped, 9 plants, and a 6-finding review with all 6 fixed. The loader, the generator, the attributes, the record loader, and the sample follow.

---

*This is a required file. Do not delete it, because it marks the folder as an epic. Update it with `/update-epic markup` after work on a feature, and run `/review-epic markup` to refresh the Tech Debt / Findings section.*
