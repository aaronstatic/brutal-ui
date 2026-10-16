# Dev Ops - Epic Requirements

**Created:** 2026-10-16
**Phase:** Phase 1 (project skeleton)

> Epic-level requirements for the whole epic. Each child feature has its own `requirements.md` that `/plan-feature dev-ops/<feature-name>` consumes.

## Overview

This epic delivers the solution skeleton and the build tools of brutal-ui. It owns the solution file, the props files, the package pins, the test scripts, and the test runbooks. The features belong together because every later epic builds and tests through them.

## Requirements

- The solution builds on Linux and on Windows with `dotnet build` and no other tool.
- One `Directory.Packages.props` pins every `Ahwoo.BRUTAL.*` package at a 2026.10 release from nuget.org.
- `BrutalUi.Core` references no ImGui package. The compiler enforces it through the project references.
- Every project targets .NET 10 and C# 14, with nullable on and `AllowUnsafeBlocks` on.
- Each package project carries the pack metadata: package id, version, licence, and description. `dotnet pack` works. No publish step exists.
- One Linux script runs the unit tests and a GPU smoke run of a sample for N frames under Xvfb on the NVIDIA Vulkan ICD. The script exits with a non-zero code on any failure.
- The agent runs the Linux script on this machine without a display and without user input.
- One Windows procedure covers pull, build, test, and run of the sample. The author completes it in under five minutes.
- The repository stays private on GitHub. Each feature lands on `main` with a push.

## Planned Features

1. **solution-skeleton**. The solution, the props files, the package pins, and every empty project from the technical design. Every other feature needs projects to build and test.
2. **linux-dev-environment**. The .NET 10 SDK on this machine, a GPU health check, and the Linux test script. Needs the solution and the sample from feature 1. Proves the GPU path, the highest risk.
3. **windows-test-path**. PowerShell scripts and a runbook for Windows. Needs the solution from feature 1. Runs in parallel with feature 2.

## Dependencies

- The `Ahwoo.BRUTAL.*` 2026.10 packages on nuget.org.
- The Ubuntu apt package `dotnet-sdk-10.0`, installed one time with sudo.
- A reboot of this machine before any GPU test. The NVIDIA kernel module is 595.91 and the user space library is 595.99. The author does the reboot.
- `Xvfb`, `xvfb-run`, and the NVIDIA Vulkan ICD on this machine. All three exist today.

## Out of Scope

- GitHub Actions CI. A later `github-ci` epic reuses the scripts of this epic on hosted runners.
- A publish to nuget.org. Pack metadata only.
- Visual Studio solution filters or user files.
- A Docker build container.
- Any element, layout, style, or renderer code. Each project builds empty.

---

*Consumed by `/plan-epic dev-ops`. After planning, run `/plan-feature dev-ops/<feature-name>` per feature in the recommended order.*
