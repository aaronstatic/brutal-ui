# Dev Ops - Epic Overview

**Epic:** dev-ops
**Status:** Complete
**Last Updated:** 2026-10-16 18:30

> **This file is the epic marker.** Its presence in `docs/features/dev-ops/` tells every Beast Mode command that this folder is an **epic**, not a plain feature. Keep it present and keep the required sections below filled in. The master `docs/overview.md` tracks this epic as a **single row**. The per-feature detail lives **here**.

---

## Purpose

This epic scaffolds the brutal-ui solution and its build tools. It makes the project build on this Linux machine and on the Windows machine of the author. It gives the agent one script for an automated Linux test run, with unit tests and a GPU smoke run on the RTX 2060. It gives the author one short procedure for a Windows test run after a git pull.

The three features belong together because each later epic depends on the same solution, the same scripts, and the same two test paths.

---

## Features

The constituent features of this epic, in build order. Each feature is a subfolder under `docs/features/dev-ops/`.

| # | Feature | Status | Tasks | Description |
|---|---------|--------|-------|-------------|
| 1 | solution-skeleton | Complete | 31/31 (100%) | The solution, the props files, the package pins, and every empty project from the technical design. |
| 2 | linux-dev-environment | Complete | 18/18 (100%) | The .NET 10 SDK on this machine, a GPU health check, and one script for unit tests plus a Vulkan smoke run under Xvfb. |
| 3 | windows-test-path | Complete | 19/19 (100%) | PowerShell build and test scripts and a short runbook for pull, build, and run on Windows. |

> Reference any feature with the slash form `dev-ops/<feature-name>`. Task counts come from each feature's own `tasks.md` and `/update-epic` refreshes them.

---

## Build Order / Dependencies

1. **solution-skeleton**. Every other feature needs projects to build and test. It comes first.
2. **linux-dev-environment**. The test script needs the solution and the sample project from feature 1. It proves the GPU path on this machine, which is the highest risk in the epic.
3. **windows-test-path**. The scripts need the solution from feature 1. It runs in parallel with feature 2.

**Dependencies between features:**
- solution-skeleton → linux-dev-environment (the solution file, the test projects, and the `HelloPanel` sample)
- solution-skeleton → windows-test-path (the solution file and the sample)
- linux-dev-environment and windows-test-path have no dependency on each other. Build them in parallel.
- External: the `Ahwoo.BRUTAL.*` 2026.10 packages on nuget.org. The Ubuntu `dotnet-sdk-10.0` apt package. The Ubuntu `libvulkan-dev` apt package, because `Brutal.Vulkan` loads the unversioned `libvulkan.so`. No reboot was necessary: the NVIDIA kernel module and the user space library both report 595.99.02.
- The `/autorun-epic` run built features 2 and 3 one after the other, not in parallel.

---

## Integration & Architecture

- **Within the epic:** The solution and the props files are the shared contract. Both script sets call `dotnet build` and `dotnet test` on the same solution. Both script sets run the same sample with the same frame count argument.
- **With other epics / features:** Every later epic adds code to the projects from feature 1 and runs the scripts from features 2 and 3. A later `github-ci` epic reuses the same scripts on hosted runners.
- **Key architectural decisions for the epic as a whole:**
  - One `Directory.Packages.props` pins every `Ahwoo.BRUTAL.*` version.
  - `BrutalUi.Core` references no ImGui package. The project references enforce it.
  - A sample accepts a frame count argument and exits with a code. That is the GPU smoke contract on both operating systems.
  - Pack metadata lives in the projects. No publish step exists in this epic.

---

## Tech Debt / Findings

- (none yet: `/review-epic` will surface cross-feature integration issues and per-feature tech debt here)

---

## Master Overview Rollup

- **Rollup status:** Complete (3/3 features, 68/68 tasks). All 22 manual checks in `human-testing.md` pass.
- **One-line summary for master:** The solution builds and packs. `scripts/test-linux.sh` passes with a GPU smoke run. `scripts/test-windows.ps1` passes on Windows in 44 s.

---

*This is a required file. Do not delete it. It marks the folder as an epic. Update it with `/update-epic dev-ops` after work on the features, and run `/review-epic dev-ops` to refresh the Tech Debt / Findings section.*
