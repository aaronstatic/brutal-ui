# Linux Dev Environment: Requirements

**Epic:** dev-ops
**Created:** 2026-10-16

## Overview

This feature makes this Linux machine build and test brutal-ui without a display and without user input. It installs the .NET 10 SDK, checks the GPU driver, and gives the agent one script for the unit tests and a Vulkan smoke run. It owns the Linux test path of the dev-ops epic.

## Requirements

- The Ubuntu apt package `dotnet-sdk-10.0` is installed. `dotnet --version` prints a 10.x version.
- A script `scripts/check-gpu.sh` reports the NVIDIA kernel module version, the user space library version, and the Vulkan ICD. It exits with a non-zero code on a version mismatch and prints the fix.
- A script `scripts/test-linux.sh` runs `dotnet build`, `dotnet test`, and then the `HelloPanel` sample for N frames under `xvfb-run` on the NVIDIA Vulkan ICD. It exits with a non-zero code on any failure.
- The script selects the NVIDIA ICD with `VK_ICD_FILENAMES` so the run never falls back to the software Lavapipe ICD.
- The script prints the last 30 lines of each step on a failure, and a one-line summary on success.
- The agent runs the script from a Claude Code session with no `DISPLAY` set.
- A `docs/testing-linux.md` runbook lists the one-time setup and the script call.

## Dependencies

- `dev-ops/solution-skeleton` is complete. The script needs the solution, the test projects, and the `HelloPanel` sample.
- A reboot of this machine. The NVIDIA kernel module is 595.91 and the user space library is 595.99. The author does the reboot before the first GPU run.
- `Xvfb`, `xvfb-run`, and `/usr/share/vulkan/icd.d/nvidia_icd.json` exist on this machine today.
- This feature runs in parallel with `dev-ops/windows-test-path`.

## Out of Scope

- GitHub Actions. A later epic reuses the script on a hosted runner.
- A Docker build container.
- An OpenGL smoke run. Vulkan only in this epic.
- Pixel comparison of the sample output. The smoke run checks the exit code only.
