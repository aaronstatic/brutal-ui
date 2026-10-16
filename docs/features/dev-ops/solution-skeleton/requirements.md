# Solution Skeleton: Requirements

**Epic:** dev-ops
**Created:** 2026-10-16

## Overview

This feature delivers the solution file, the props files, the package pins, and every empty project from `docs/technical-design.md` section 3. It owns the shape of the repository. Every other feature in the dev-ops epic builds and tests through it.

## Requirements

- `brutal-ui.sln` at the repository root holds every project below.
- `Directory.Build.props` sets .NET 10, C# 14, nullable on, `AllowUnsafeBlocks` on, and the pack metadata defaults.
- `Directory.Packages.props` enables central package management and pins every `Ahwoo.BRUTAL.*` package at a 2026.10 release. It also pins xUnit and BenchmarkDotNet.
- `src/` holds `BrutalUi.Core`, `BrutalUi.ImGui`, `BrutalUi.Framework`, `BrutalUi.Markup`, and `BrutalUi.Debugger`.
- `tests/` holds `BrutalUi.Core.Tests`, `BrutalUi.Framework.Tests`, `BrutalUi.Markup.Tests`, and `BrutalUi.Benchmarks`.
- `samples/` holds `HelloPanel` and `WorldSpace`.
- `BrutalUi.Core` references `Ahwoo.BRUTAL.Numerics` and `Ahwoo.BRUTAL.Localization` only. No ImGui package.
- Each test project holds one passing placeholder test. `dotnet test` passes.
- `HelloPanel` opens a GLFW window, runs for N frames from a command line argument, and exits with code 0. It draws one ImGui frame per loop through the Vulkan backend.
- `dotnet pack` on each `src/` project produces a `.nupkg` file.
- A `global.json` pins the SDK major version.
- An `.editorconfig` sets file-scoped namespaces and the BRUTAL style rules.

## Dependencies

- The `Ahwoo.BRUTAL.*` 2026.10 packages on nuget.org.
- A .NET 10 SDK on the build machine. On this Linux machine, the `linux-dev-environment` feature installs it. Plan this feature first, then install the SDK before the first build.

## Out of Scope

- Any element, layout, style, or renderer code. Each project builds empty.
- The Linux test script. That is `dev-ops/linux-dev-environment`.
- The Windows scripts and runbook. That is `dev-ops/windows-test-path`.
- A publish to nuget.org.
- The `WorldSpace` sample draws nothing. It builds and exits.
