# Element Generator - Requirements

**Epic:** markup
**Created:** 2026-11-12

## Overview

This feature adds a Roslyn source generator that writes the element table of the XML loader. A game marks a custom element type with an attribute, and the generator writes the factory and the attribute setters. The generator also writes the rows of the built-in types, so the written table of xml-element-loader goes away. It is the hardest feature of the markup epic, because it adds a project and an analyzer package layout.

## Requirements

- A new project, `BrutalUi.Generators`, holds an incremental source generator. It ships inside the `BrutalUi.Markup` package as an analyzer. (A4)
- A game marks an element type with an attribute that gives the XML name. The generator writes the table rows for each public settable property of a supported type. (Q2, A5)
- The supported property types are those of the written table: text, number, boolean, enum, and each other type that a built-in row uses.
- The generator reports a diagnostic for a marked type with no public constructor without parameters, and for a duplicate XML name.
- The generated code registers the types of an assembly with one call. The manual register call of xml-element-loader stays public. (A5)
- The generator writes the rows of the built-in types, and the written table goes away. The tests of xml-element-loader pass unchanged. (A3)
- The generated path uses no reflection at run time.
- `Microsoft.CodeAnalysis.CSharp` has a pin in `Directory.Packages.props`. The feature adds no native dependency. The feature `context.md` notes the new build-time package. (A4)
- `dotnet pack` puts the generator in `analyzers/dotnet/cs` of the Markup package. A test project that references the package gets the generated rows.
- Generator tests check the output for a sample type and each diagnostic. `scripts/test-linux.sh` and `scripts/test-windows.ps1` pass.

## Dependencies

- xml-element-loader must be complete. It gives the table shape and the registry.
- dev-ops/solution-skeleton gives the pack layout and the test scripts.
- This feature can build in parallel with binding-attributes.

## Out of Scope

- A generator for the builder API methods of a custom type.
- A generator for the stylesheet property table.
- A schema file for editor completion.
- Reflection as a fallback for a type with no generated rows.
