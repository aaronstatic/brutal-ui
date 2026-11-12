# Markup Sample - Requirements

**Epic:** markup
**Created:** 2026-11-12

## Overview

This feature shows the markup epic in the HelloPanel sample. One panel of the sample comes from an XML template, with a store binding and a command. The feature adds the smoke line and the allocation gate for a loaded tree. It closes the markup epic and Phase 3.

## Requirements

- HelloPanel loads one panel from an XML template file next to `HelloScreen.css`. The panel shows a label with `bind-text` and a button with `command`.
- The sample loads its panel, layout, and mode records from an XML file. The two HelloPanel modes and the `Mode (F1)` button work as before.
- The sample holds one custom element type with the generator attribute, and the template uses it.
- A load error prints the line, the column, and the kind, and the sample exits with code 1.
- The smoke run prints an eighth line with the counts of the loaded panel, and exits with code 1 when a count is 0.
- An allocation gate in the tests runs `Update` and `Draw` on the loaded sample shape and asserts 0 bytes. (A10)
- `scripts/test-linux.sh` and `scripts/test-windows.ps1` pass. `README.md` and section 11 of `technical-design.md` show the template of the sample.
- `human-testing.md` of the epic lists the windowed checks for a host with a GPU window.

## Dependencies

- Each other feature of the epic must be complete.
- panels-and-modes/panel-sample gives the two HelloPanel modes and the seventh smoke line.

## Out of Scope

- A second sample project.
- A move of each HelloPanel screen to XML. One panel shows the path, and the C# screens stay as a compare.
- Hot reload. (Q6)
