# Hello Panel Sample - Requirements

**Epic:** imgui-renderer
**Created:** 2026-10-20

## Overview

This feature turns the HelloPanel sample into a static brutal-ui screen and proves the full path from `Element` to the Vulkan backend. It keeps the `--frames N` contract so both test scripts run it as the GPU smoke run. It comes last in the imgui-renderer epic and closes Phase 1.

## Requirements

- The sample builds a tree with a panel, a `Label`, and an `Image`, and shows it through `ImGuiRenderer`. The ImGui text window goes (Q5).
- The sample fills a small procedural texture in code, uploads it through its `VulkanContext`, and registers it with the texture service. No `Brutal.Texture` pin (Q4).
- The frame loop calls `UiContext.Update` and `UiContext.Draw` each frame and allocates no managed memory.
- The sample keeps the `--frames N` contract and the exit codes 0, 1, and 2.
- `scripts/test-linux.sh` and `scripts/test-windows.ps1` pass with the new sample, with no script change unless the smoke run needs one.
- `docs/features/imgui-renderer/human-testing.md` lists the manual checks on Windows.
- `CHANGELOG.md` records the draw allocation number and the benchmark numbers.
- The WorldSpace sample stays a stub (A8).

## Dependencies

- label-and-image must be complete. The sample uses the label and the image.
- `libvulkan-dev` on Linux and `vulkan-1.dll` on Windows, as the dev-ops epic records.

## Out of Scope

- Input, hover, and focus. The screen is static.
- Stylesheets and classes. The sample sets paint fields inline.
- The WorldSpace sample scene.
