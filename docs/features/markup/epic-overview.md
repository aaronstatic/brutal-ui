# Markup - Epic Overview

**Epic:** markup
**Status:** Ready for Review (6/6 features complete)
**Last Updated:** 2026-11-19 12:22

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
| 2 | xml-element-loader | Complete | 38/38 (100%) | The XML template loader in `BrutalUi.Markup` on `XmlReader`: the type registry, a written table for the built-in types, the attribute hook, the errors with a line and a column, and the tree compare test. Ready for Review on `feat/markup/xml-element-loader`. |
| 3 | element-generator | Complete | 46/46 (100%) | A Roslyn source generator that writes the table rows for the built-in types and for a custom type that a game marks with an attribute. Ready for Review on `feat/markup/element-generator`. |
| 4 | binding-attributes | Complete | 18/18 (100%) | The `command`, `bind-text`, `bind-checked`, and `bind-value` attributes in `BrutalUi.Framework` through `TemplateBindings` and one typed selector per key, with the positioned binding errors and the parity and 0-byte proofs. Ready for Review on `feat/markup/binding-attributes`. |
| 5 | definition-loader | Complete | 20/20 (100%) | The panel, layout, and mode records from XML through `DefinitionLoader` and `DefinitionLoadResult` in `BrutalUi.Framework`: the two-pass reader, 13 positioned error kinds, the `SlotContainer` element type, and the record parity and 0-byte proofs. Ready for Review on `feat/markup/definition-loader`. |
| 6 | markup-sample | Complete | 22/22 (100%) | The HelloPanel state panel from an XML template with one store binding and three commands, with `HelloDefinitions.xml` holding three panels, one layout, and two modes, `HelloRow` as a marked custom type, the `AddFactory` seam for a C# record, smoke line 8, the load error exit, and the twenty-third allocation gate. Ready for Review on `feat/markup/markup-sample`. |

> Reference any feature with the slash form `markup/<feature-name>`. Task counts come from each feature's own `tasks.md`, and `/update-epic` refreshes them.

---

## Build Order / Dependencies

1. **builder-api** - It comes first because it sets the property surface that the XML attributes follow. It also gives the query by name that the panel code and the tests use. It is complete and Ready for Review on `feat/markup/builder-api`. xml-element-loader inherits four things from it: the property surface (43 `ElementBuilder` methods, `Command`, `Find`, and `Find<T>`), the 12 value names as the table rows, `tests/BrutalUi.Core.Tests/Shared/TreeCompare.cs` with the link line for `BrutalUi.Markup.Tests`, and the rule that a row for `value` runs after the rows for the range. The builder-api review left one note in the loader requirements: `Capacity` and `ScrollSpeed` exist only in the create info (A6), so they have no builder method, no public setter, and no table row.
2. **xml-element-loader** - It depends on the property surface of #1. It ships a written table for the built-in types, so the generator does not block the loader. It is complete and Ready for Review on `feat/markup/xml-element-loader`. element-generator inherits from it: the `AttributeRow` kinds and the shape of the rows to generate (plan Notes item 10: `text` and `text-key` are create rows, `min`, `max`, `step`, `capacity`, and `scroll-speed` are create rows too, and the base rows keep the A2 names), the public `Register` with the full checks, the single replaceable file `BuiltInElements.cs` (the `capacity` ceiling `TemplateValues.MaxTextCapacity`, review CR-1, lives in the `TextField` factory and must survive the replacement), the hook contract for binding-attributes (`AddAttributeHandler`, `AttributeContext.AddError`, `TemplateError`), and the `TemplateError.ToString` form for markup-sample.
3. **element-generator** - It depends on the table shape of #2 and replaces the written table. It is the hardest feature, because it adds a new project and a package layout for an analyzer. It is complete and Ready for Review on `feat/markup/element-generator`. binding-attributes inherits from it: the hook contract is unchanged (`AddAttributeHandler`, `AttributeHandler`, `AttributeContext.AddError`, `TemplateError`), and `AttributeContext.cs` carries additions only (`TryReadInteger`, `TryReadKeyword`). markup-sample inherits: a game type marks its class with `[TemplateElement("Name")]` (plus `[TemplateFactory]`/`[TemplateCreateRow]` when it needs create rows), adds the analyzer `ProjectReference` line to its csproj (Notes item 8 — the analyzer does not flow through a project reference), calls the generated `loader.RegisterHelloPanelElements()`, and a bad marked type stops the build with one `BRUTALUI000N` error at the code at fault. The built-in rows now come from the generator through eight `[TemplateElement(..., Target = typeof(...))]` declarations in `BuiltInElements.cs`; the `capacity` ceiling stays in the `TextField` factory.
4. **binding-attributes** - It depends on the attribute hook of #2. It adds the reference from Framework to Markup. It is complete and Ready for Review on `feat/markup/binding-attributes`. definition-loader and markup-sample inherit from it: `TemplateBindings` (one `CommandRegistry`, one key table of typed selectors, `AddText` in two overloads, `AddChecked`, `AddValue`, and `AttachTo`), the four attribute names on the hook with one handler, the two new `TemplateErrorKind` members `UnknownBinding` and `BindingTypeMismatch` at the position of the attribute, the settled open point 9 of #2 (the registry of the load is the registry of the attached `TemplateBindings`), and the fact that a loaded bound tree equals the C# bound tree and allocates 0 bytes in `Update` and `Draw`. markup-sample must add its own generator `ProjectReference` line: the packed Framework nuspec marks the Markup dependency `exclude="Build,Analyzers"`, so a game with its own `[TemplateElement]` types needs the Markup package too (plan Notes item 7).
5. **definition-loader** - It depends on #2 for the reader and on #4 for a template with bindings. It is complete and Ready for Review on `feat/markup/definition-loader`. markup-sample inherits from it: `DefinitionLoader` with `AddTemplate`, `AddActionMap`, and `Load`, `DefinitionLoadResult` with `RegisterTo`, the `SlotContainer` element type, the two-pass reader with the follow-on rule (one fault gives one error, each at the line and column of its attribute name), and the rule that `Load` throws nothing for a text.
6. **markup-sample** - It depends on #1 to #5 and closes the epic and Phase 3. It is complete and Ready for Review on `feat/markup/markup-sample`. It adds `DefinitionLoader.AddFactory` for a panel or a layout that a game builds in C#, and makes `DefinitionLoadResult.RegisterTo` return the panel controllers. Both changes are additive, and every definition-loader test still passes with no edit.

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

- **Within the epic:** builder-api adds the fluent methods to Core and the `Command` method to Framework. xml-element-loader reads a template with `XmlReader`, finds each type in the registry, and sets each attribute through a table row. An attribute that the table does not hold goes to the attribute hook. element-generator writes the rows at build time. binding-attributes registers `command`, `bind-text`, `bind-checked`, and `bind-value` on the hook through one attached `TemplateBindings` object. definition-loader reads the records and gives them to `UiManager.Register`. markup-sample runs that whole path in a shipped sample: `HelloMarkup.Load` reads `HelloState.xml` and `HelloDefinitions.xml`, loads the records, registers them, and mounts them, and `RegisterTo` hands back the panel controllers.
- **With other epics / features:** core-tree gives `Element` and `ElementBuilder`. interactive-elements gives the widget types. style-system gives `StyleParseError` as the model for an error. state-and-commands gives `BindCommand`, `Store<T>`, and the element bindings. panels-and-modes gives the records and `UiManager.Register`. The Phase 4 epics register `ListView`, `DataList`, `WindowChrome`, and `WindowDefinition` on the same registry.
- **Key architectural decisions for the epic as a whole:**
  - Q1 Framework references Markup. Markup stays Core only and gives an attribute hook. Framework adds `command` and the definition loader.
  - Q2 A Roslyn source generator makes the table in this epic.
  - Q3 The element loader uses `System.Xml.XmlReader`.
  - Q4 The epic adds a query by name and a named bind attribute.
  - A4 The generator is a new project, `BrutalUi.Generators`, and ships inside the `BrutalUi.Markup` package as an analyzer. It adds no native dependency.
  - A10 Load-time code can allocate. The 0-byte gate covers `Update` and `Draw` on a loaded tree.
  - A definition entry names a template text or a game factory. `AddTemplate` and `AddFactory` share one ordinal name table, the load calls no factory, and a factory record gets the delegate as it is. A layout that a game builds in C# gets no check load, so `UiManager` checks its slots inside `RegisterTo`.

---

## Tech Debt / Findings

- (none yet. `/review-epic` adds the cross-feature findings here.)

---

## Master Overview Rollup

- **Rollup status:** Ready for Review (6/6 features complete, 177/177 tasks done)
- **One-line summary for master:** All six features are Ready for Review, and markup-sample closes the epic and Phase 3: a store, a panel controller, and an XML template give a bound panel on the screen. builder-api (33/33), xml-element-loader (38/38), element-generator (46/46), and binding-attributes (18/18) stand as before. definition-loader (20/20) adds `DefinitionLoader`, `DefinitionLoadResult`, `DefinitionError` with 13 kinds, and the `SlotContainer` element type in `BrutalUi.Framework`: a read pass with positions and a resolve pass turn one text into the panel, layout, and mode records that `UiManager.Register` takes, so the entry order does not matter, one fault gives one error at the line and column of its attribute name, a result with an error registers nothing and throws one line per error in `RegisterTo`, and the loaded records give the same mode diff as the C# records (`DefinitionParityTests`, and the eighteenth gate now measures 0 bytes on the loaded records). markup-sample (22/22) runs that path in HelloPanel: `HelloState.xml` builds the state panel, `HelloRow` is a `[TemplateElement]` type of the sample, and `HelloDefinitions.xml` holds three panels, one layout, and two modes, while the new `DefinitionLoader.AddFactory` carries the widget panel, the info panel, and the layout as C# records and `RegisterTo` returns the mounted controllers. The sample builds the tree, loads the records, and registers them before `Glfw.Init`, so a bad record prints one line and exits 1 with no window; smoke line 8 prints `HelloPanel: markup: elements 6, bindings 1, commands 3, panels 3, layouts 1, modes 2.`; and `SampleMarkupAllocationTests`, the twenty-third allocation gate, measures 0 bytes over 100 click frames on the loaded panel. The two cross-phase reviews closed 11 findings (5 and 6), each with a verifier PASS. The solution reads 3203 tests, 0 failed, 3 skipped, and `BrutalUi.Framework.Tests` 392 (356 before these two features); the package run reads `package-test: PASS Gauge level 0.5`. 16 human-test lines stand open in `docs/features/markup/human-testing.md` (4 for definition-loader, 12 for markup-sample), and the windowed smoke checks await a host with a GPU window.

---

*This is a required file. Do not delete it, because it marks the folder as an epic. Update it with `/update-epic markup` after work on a feature, and run `/review-epic markup` to refresh the Tech Debt / Findings section.*
