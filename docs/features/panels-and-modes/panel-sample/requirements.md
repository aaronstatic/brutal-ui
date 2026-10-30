# Panel Sample - Requirements

**Epic:** panels-and-modes
**Created:** 2026-10-30

## Overview

This feature closes the epic. It shows two modes in the HelloPanel sample, adds the mode line to the smoke run, and adds the allocation gate for the mode diff. It proves the model on the GPU and under the 0-byte rule.

## Requirements

- HelloPanel registers one layout with two slots, two modes, and at least three panels. One panel is in both modes, one is in the first mode only, and one is in the second mode only (A8).
- A button and a key action switch the mode through a command. A second key action calls `ModeHistory.Back()`.
- The smoke run switches the mode in each frame after warm-up. It prints a seventh line with the mode counts, and exits with code 1 when a count is 0.
- `ModeDiffAllocationTests` in `tests/BrutalUi.Framework.Tests/` measures `SetMode` plus one `Update` on a warm tree at 0 bytes, and a frame with no target at 0 bytes (A7). Each gate calls `AllocationGate.ReleaseAllocationContext()` before its measure.
- `scripts/test-linux.sh` and `scripts/test-windows.ps1` pass. `docs/overview.md` gets the Phase 3 smoke contract line for the mode counts.
- The stylesheet of the sample styles each `SlotContainer` and each panel root by class.

## Dependencies

- mode-set for `UiManager`, `SetMode`, and `ModeHistory`.
- state-and-commands for the command, the key action, and the HelloPanel state sample.
- dev-ops for the smoke contract and the test scripts.

## Out of Scope

- A new sample project. The changes live in `samples/HelloPanel/`.
- A panel from XML. The xml-markup epic owns it.
