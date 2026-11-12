# Markup - Epic Requirements

**Created:** 2026-11-12
**Phase:** Phase 3

> Epic-level requirements. `/plan-epic markup` reads this file. Each child feature below has its own `requirements.md` that `/plan-feature markup/<feature-name>` consumes.

## Overview

The markup epic gives a developer two paths to one element tree: a C# builder API and an XML template. It owns the XML element loader in `BrutalUi.Markup`, the source generator for the element table, the fluent build methods on the element types, and the markup attributes and the record loader in `BrutalUi.Framework`. The features belong together because of the rule "markup equals code": each element feature works from XML and from C#. The epic closes Phase 3 and completes the target outcome of the mission statement.

## Requirements

- The builder API covers each built-in element type: `Element`, `Label`, `Image`, `Button`, `Toggle`, `Slider`, `TextField`, and `ScrollView`. It sets the name, the classes, the text, the children, and the command. (A2)
- Core gives a query that finds an element by name below a root. (Q4)
- The XML loader reads a template in the format of section 11 of `technical-design.md` and builds an element tree. It uses `System.Xml.XmlReader`. (Q3)
- The loader maps an element name to a registered type and an attribute to a property through a table. The loaded path uses no reflection.
- A Roslyn source generator writes the table rows. A game marks a custom element type with an attribute. A manual register call also stays public. (Q2, A5)
- `BrutalUi.Markup` references Core only and gives an attribute hook. `BrutalUi.Framework` references Markup and adds the `command` attribute, the bind attribute, and the record loader. (Q1)
- The `command="..."` attribute calls `BindCommand`. The `bind-text="key"` attribute binds an element to a store through a typed selector that the panel code registers for the key. (Q4, A6)
- The loader reports an error with a line and a column, in the manner of `StyleParseError`.
- The XML path and the C# path produce the same tree. A test loads the XML, builds the C#, and compares the two trees.
- The panel, layout, and mode records of `UiManager.Register` load from XML or from C# records. A panel points to a template by name, and a mode names its action map. (Q5, A7)
- Load-time code can allocate. A tree from the loader allocates nothing in `Update` and `Draw` after warm-up, and the allocation gate grows to cover it. (A10)
- The epic adds no native dependency and no ImGui reference to `BrutalUi.Core`. (A4)
- HelloPanel shows a panel from an XML template, and the smoke run checks it. Each feature updates section 11 of `technical-design.md`.

## Planned Features

1. **builder-api** - The fluent build methods for each built-in element type and the query by name. It comes first because it sets the property surface that the XML attributes follow.
2. **xml-element-loader** - The XML reader, the type registry, a written table for the built-in types, the attribute hook, and the errors. It depends on the property surface of #1 and adds the tree compare test. (A3)
3. **element-generator** - The source generator that writes the table rows for the built-in types and for a custom type. It depends on the table shape of #2 and replaces the written table. (Q2, A3)
4. **binding-attributes** - The `command` and `bind-text` attributes in Framework, on the attribute hook of #2. (Q1, Q4)
5. **definition-loader** - The panel, layout, and mode records from XML. It depends on #2 for the reader and on #4 for a template with bindings. (Q5)
6. **markup-sample** - A HelloPanel panel from an XML template with a store binding and a command, plus the smoke line and the allocation gate. It depends on #1 to #5 and closes the epic.

## Dependencies

- core-tree: `Element`, `ElementBuilder`, and the allocation gate.
- interactive-elements: the widget types and their delegate slots.
- style-system: `StyleSheetParser`, `StyleTokenizer`, and `StyleParseError` in `BrutalUi.Markup`.
- state-and-commands: `BindCommand`, `Store<T>`, and the element bindings.
- panels-and-modes: `PanelDefinition`, `LayoutDefinition`, `ModeDefinition`, and `UiManager.Register`. Its status is Ready for Review.
- `Microsoft.CodeAnalysis.CSharp` for the generator project, pinned in `Directory.Packages.props`. It is a managed, build-time package.

## Out of Scope

- Hot reload of a template or a stylesheet. (Q6)
- `WindowDefinition` and the window XML. They belong to the phase 4 window epic. (A9)
- `ListView`, `DataList`, and `WindowChrome` as element names. Each later epic registers its own types. (A9)
- A move of an existing feature into this epic. `StyleSheetParser` stays in style-system. (A8)
- A visual editor, and a schema file for editor completion.
- Changes to the stylesheet format. The style-system epic owns it.
- File IO policy beyond a read of a UTF-8 span or a stream. The game owns the file path.

---

*Consumed by `/plan-epic markup`. After planning, run `/plan-feature markup/<feature-name>` for each feature in the recommended order.*
