# Definition Loader - Requirements

**Epic:** markup
**Created:** 2026-11-12

## Overview

This feature loads the panel, layout, and mode records from XML. Today a game builds each `PanelDefinition`, `LayoutDefinition`, and `ModeDefinition` in C#. The feature reads the same records from one XML text and gives them to `UiManager.Register`. It is in `BrutalUi.Framework`, on the reader of xml-element-loader.

## Requirements

- A loader in `BrutalUi.Framework` reads an XML text and returns the panel, layout, and mode records or a list of errors. (Q1, Q5)
- A panel entry gives the name, the default slot, and the name of its template. The record builds its root from that template. (A7)
- A layout entry gives the name and the name of its template. The template holds the slot containers.
- A mode entry gives the name, the layout, each slot with its panels, and the name of its action map. (A7)
- The game registers each template and each `UiActionMap` by name in C# before the load. (A7)
- A name with no registered template or map, a duplicate name, and a slot that the layout does not hold each give an error with a line and a column.
- A record from XML gives the same result in `UiManager` as a record from C#. A test registers both forms and compares the mode diff.
- A mode switch on loaded records allocates nothing after warm-up. The `ModeDiffAllocationTests` gate grows to cover it. (A10)
- Tests in `tests/BrutalUi.Framework.Tests/` cover each record and each error. Sections 10 and 11 of `technical-design.md` get the record format.

## Dependencies

- xml-element-loader and binding-attributes must be complete.
- panels-and-modes gives the three records and `UiManager.Register`. Its status is Ready for Review.

## Out of Scope

- `WindowDefinition`. The phase 4 window epic adds it. (A9)
- An action map in XML. The game builds each map in C#, and the binding text format of the input epic stays as it is.
- File IO and a search path. The game reads each file and gives the text.
- Hot reload. (Q6)
