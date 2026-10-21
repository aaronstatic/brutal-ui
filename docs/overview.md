# brutal-ui - Overview

**Last Updated:** 2026-10-21 04:35
**Version:** v1.3

## Project Summary

brutal-ui gives BRUTAL developers a retained-mode game UI framework with zero allocations in the frame loop. It ships as NuGet packages and renders through the Dear ImGui draw list that BRUTAL already provides. It brings the panel, mode, store, and window model of mosaic-ui to BRUTAL with no Unity dependency.

## Feature Status

| Feature | Status | Tasks | Notes |
|---------|--------|-------|-------|
| dev-ops (epic) | Complete | 68/68 (100%) | The solution builds and packs. `scripts/test-linux.sh` passes with a GPU smoke run. `scripts/test-windows.ps1` passes on Windows in 44 s. |
| core-tree (epic) | Complete | 150/150 (100%) | The element tree, the C# flexbox engine (337 Yoga fixtures), and the 0-byte gate on a 1000-element tree. 574 tests pass. |
| imgui-renderer (epic) | Complete | 141/141 (100%) | `IUiRenderer`, `UiContext.Draw`, `ImGuiRenderer`, `Label`, `Image`, and the HelloPanel static screen on Vulkan. Draw allocates 0 bytes. 1013 tests pass. Closes Phase 1. |

> An epic is one row. Its child features live in `docs/features/<epic>/epic-overview.md`.

## Integration Points

- **dev-ops → every later epic:** Each later epic adds code to the projects of `dev-ops/solution-skeleton`. Each later epic runs `scripts/test-linux.sh` and `scripts/test-windows.ps1`.
- **GPU smoke contract:** A sample accepts `--frames N` and exits with a code. Both test scripts use this contract.
- **System dependency:** `Brutal.Vulkan` loads the unversioned `libvulkan.so` on Linux, so the machine needs `libvulkan-dev`. On Windows it loads `vulkan-1.dll` from `System32`.
- **core-tree → later epics:** The imgui-renderer epic adds `UiContext.Draw` and plugs text into the measure callback slot of `LayoutNode`. The style-system epic writes `ComputedStyle` from the cascade. The input epic adds events and hit testing on `Element`.
- **Allocation gate:** `ThousandElementAllocationTests` fails the test scripts when `UiContext.Update` or `UiContext.Draw` allocates on a warm 1000-element tree. Each zero-byte gate calls `AllocationGate.ReleaseAllocationContext()` before its measure.
- **imgui-renderer → later epics:** `IUiRenderer` is the only path to pixels. The style-system epic writes the paint members of `ComputedStyle`. The input epic reads the layout rects and `LayoutNode.ContentInsets`. The world-space epic adds a private draw list behind the same interface.
- **GPU smoke contract (Phase 1):** The HelloPanel smoke run prints 3 lines and exits with code 1 when `Update` and `Draw` allocate after warm-up.

## Changelog

- v1.3 (2026-10-21): **imgui-renderer epic complete** - 4/4 features, 141/141 tasks. renderer-interface adds `IUiRenderer`, the paint members, and `UiContext.Draw`. imgui-draw-list adds `ImGuiRenderer` with the font and texture services and the headless ImGui tests. label-and-image adds `Label`, `Image`, the measurer slot, word wrap, and `LayoutNode.ContentInsets`. hello-panel-sample shows the static screen on Vulkan and adds `TreeDrawBenchmark`. 1013 tests pass. Phase 1 is closed.

- v1.2 (2026-10-18): **core-tree epic complete** - 3/3 features, 150/150 tasks. element-tree adds `Element`, the pooled child list, the class list, the dirty flags, and `UiContext`. flexbox-layout adds the C# flexbox engine with 337 Yoga v3.2.1 fixture ports. allocation-guard adds the 0-byte gate, the layout benchmark, and `CHANGELOG.md`. 574 tests pass.
- v1.1 (2026-10-16): **dev-ops Windows checks pass** - The author ran all 22 manual checks. `scripts/test-windows.ps1` passes build, test, and a 120-frame smoke run in 44 s.
- v1.0 (2026-10-16): **Initial overview, dev-ops epic complete** - The overview tracks 1 epic. dev-ops: 3/3 features, 68/68 tasks. It adds the solution with 11 projects and 5 packs, the Linux test and GPU scripts, and the Windows scripts and runbook. The Windows runs wait for a manual check in `docs/features/dev-ops/human-testing.md`.
