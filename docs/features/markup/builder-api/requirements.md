# Builder API - Requirements

**Epic:** markup
**Created:** 2026-11-12

## Overview

This feature completes the C# builder API of section 11 of `technical-design.md`. Today `ElementBuilder` holds `Class` and `Child` only. The feature adds a fluent method for each property that a template can set, and a query that finds an element by name. It is the first feature of the markup epic, and it sets the property surface that the XML attributes follow.

## Requirements

- A fluent method exists for each settable property of `Element`, `Label`, `Image`, `Button`, `Toggle`, `Slider`, `TextField`, and `ScrollView`. Each method returns the element with its own type. (A2)
- The example of section 11 compiles: `Name`, `Class`, `Text`, `Child`, and `Command` in one expression.
- The fluent methods for Core types are in `BrutalUi.Core`. The `Command` method is in `BrutalUi.Framework`, on `BindCommand`. (A2)
- Core gives a query that finds the first element with a given UTF-8 name below a root. A typed form returns the element as its own type. (Q4)
- The query allocates nothing. A game can call it in a frame.
- A build with the fluent methods gives the same tree as a build with object initializers. A test compares the two.
- Tests in `tests/BrutalUi.Core.Tests/` and `tests/BrutalUi.Framework.Tests/` cover each method and the query. Section 11 of `technical-design.md` gets the full method list.

## Dependencies

- core-tree, interactive-elements, and state-and-commands are complete.
- No sibling feature. This is the first feature of the epic.

## Out of Scope

- The XML loader. xml-element-loader reads a template onto this surface.
- A store binding by a key. binding-attributes adds it.
- A selector query by class or by type. The query takes a name only.
- New element types and new element properties.
