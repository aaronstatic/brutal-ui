# XML Element Loader - Requirements

**Epic:** markup
**Created:** 2026-11-12

## Overview

This feature adds the XML template loader to `BrutalUi.Markup`. The loader reads a template in the format of section 11 of `technical-design.md` and builds an element tree. It owns the type registry, the table shape, the attribute hook, and the errors. Each later feature of the markup epic builds on it.

## Requirements

- The loader reads a UTF-8 template with `System.Xml.XmlReader` and returns the root element or a list of errors. (Q3)
- An element name in the `brutal-ui` namespace maps to a registered type. An attribute maps to a property through a table row. The loaded path uses no reflection.
- The feature ships a written table for `Element`, `Label`, `Image`, `Button`, `Toggle`, `Slider`, `TextField`, and `ScrollView`. Each property of the builder API has a row. (A3)
- A game registers a custom element type by name with a factory and its attribute setters. (A5)
- An attribute that no table row holds goes to an attribute hook. A handler on the hook takes the element, the attribute name, and the value. With no handler, the attribute is an error. (Q1)
- `BrutalUi.Markup` references `BrutalUi.Core` only. (Q1)
- An error holds a line, a column, and a kind, in the manner of `StyleParseError`. The loader reports an unknown element, an unknown attribute, a value that does not parse, and XML that is not well formed.
- A test loads a template, builds the same tree with the builder API, and compares the two trees.
- Load-time code can allocate. A loaded tree allocates nothing in `Update` and `Draw` after warm-up, and a test measures it. (A10)
- Tests in `tests/BrutalUi.Markup.Tests/` cover each built-in type, each error kind, and a custom type. Section 11 of `technical-design.md` gets the loader API.

## Dependencies

- builder-api must be complete. It gives the property surface and the tree compare.
- style-system gives `StyleParseError` as the model for an error.

## Out of Scope

- The source generator. element-generator replaces the written table.
- The `command` and `bind-text` attributes. binding-attributes registers them on the hook.
- The panel, layout, and mode records. definition-loader reads them.
- An own UTF-8 XML reader. (Q3)
- Hot reload, a template include, and a template with parameters. (Q6)
- A link from a template to a stylesheet file. The game adds each sheet with `AddSheet`.
