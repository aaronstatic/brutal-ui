# Windows Test Path: Requirements

**Epic:** dev-ops
**Created:** 2026-10-16

## Overview

This feature gives the author one short procedure for a test run on the Windows machine after a git pull. It delivers PowerShell scripts and a runbook. It owns the Windows test path of the dev-ops epic.

## Requirements

- A script `scripts/build.ps1` runs `dotnet build` on the solution and exits with a non-zero code on a failure.
- A script `scripts/test-windows.ps1` runs `dotnet build`, `dotnet test`, and then the `HelloPanel` sample for N frames in a visible window. It exits with a non-zero code on any failure.
- Both scripts run from a plain PowerShell prompt with the .NET 10 SDK on the path. No Visual Studio is required.
- A `docs/testing-windows.md` runbook holds the procedure: pull `main`, run the test script, run the sample, report the result. The author completes it in under five minutes.
- The runbook holds a short checklist for a manual look at the sample window.
- A `.gitattributes` file sets line endings so the scripts and the solution work on both operating systems.

## Dependencies

- `dev-ops/solution-skeleton` is complete. The scripts need the solution and the `HelloPanel` sample.
- The .NET 10 SDK on the Windows machine. The author installs it one time.
- This feature runs in parallel with `dev-ops/linux-dev-environment`.

## Out of Scope

- GitHub Actions on a Windows runner. A later epic.
- Visual Studio solution filters or user files.
- An installer or a packaged build of the sample.
- Automated screenshots on Windows. The author looks at the window.
