# brutal-ui - Overview

**Last Updated:** 2026-10-16 18:30
**Version:** v1.0

## Project Summary

brutal-ui gives BRUTAL developers a retained-mode game UI framework with zero allocations in the frame loop. It ships as NuGet packages and renders through the Dear ImGui draw list that BRUTAL already provides. It brings the panel, mode, store, and window model of mosaic-ui to BRUTAL with no Unity dependency.

## Feature Status

| Feature | Status | Tasks | Notes |
|---------|--------|-------|-------|
| dev-ops (epic) | Complete | 68/68 (100%) | The solution builds and packs. `scripts/test-linux.sh` passes with a GPU smoke run. The Windows scripts and runbook wait for a manual run. |

> An epic is one row. Its child features live in `docs/features/<epic>/epic-overview.md`.

## Integration Points

- **dev-ops → every later epic:** Each later epic adds code to the projects of `dev-ops/solution-skeleton`. Each later epic runs `scripts/test-linux.sh` and `scripts/test-windows.ps1`.
- **GPU smoke contract:** A sample accepts `--frames N` and exits with a code. Both test scripts use this contract.
- **System dependency:** `Brutal.Vulkan` loads the unversioned `libvulkan.so` on Linux, so the machine needs `libvulkan-dev`. On Windows it loads `vulkan-1.dll` from `System32`.

## Changelog

- v1.0 (2026-10-16): **Initial overview, dev-ops epic complete** - The overview tracks 1 epic. dev-ops: 3/3 features, 68/68 tasks. It adds the solution with 11 projects and 5 packs, the Linux test and GPU scripts, and the Windows scripts and runbook. The Windows runs wait for a manual check in `docs/features/dev-ops/human-testing.md`.
