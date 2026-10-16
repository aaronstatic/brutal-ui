# Linux Dev Environment - Implementation Plan

**Epic:** dev-ops (feature 2 of 3)
**Status:** Ready for Review
**Started:** 2026-10-16
**Target Completion:** 2026-10-16
**Last Updated:** 2026-10-16 14:00

---

## Quality Bar

**Target quality:** Production-grade developer tooling. The agent runs `scripts/test-linux.sh` after each change in each later epic. The later `github-ci` epic reuses it. `dev-ops/windows-test-path` mirrors its pattern in PowerShell. A false PASS or a hang costs time in every later feature.

- **One exit code tells the truth:** The script exits 0 only when the build, the tests, the GPU check, and the smoke run all pass. Any failure gives a non-zero exit code. A GPU stall ends in a time limit, not a hang.
- **Quiet on success, specific on failure:** Success prints one line. A failure prints the failed step, its exit code, the last 30 lines of its output, and the path of the full log.
- **Hands-off:** The scripts need no `DISPLAY`, no user input, and no `sudo`. A step that needs `sudo` is a one-time step for the author in the runbook. The script checks its result and prints the fix.
- **The real GPU only:** The smoke run uses the NVIDIA Vulkan ICD. It never falls back to the software Lavapipe ICD.
- **The shared contract, exactly:** The scripts use the exact commands of the shared contract of `dev-ops/solution-skeleton`. No other `dotnet` form appears.
- **No change to the repository state:** A run writes only under the git-ignored `artifacts/` folder.

---

## Executive Summary

This feature gives the agent one command for the full Linux test run on this machine: `scripts/test-linux.sh`. The script builds the solution, runs the unit tests, checks the GPU driver, and runs the `HelloPanel` sample for 120 frames under `xvfb-run` on the NVIDIA Vulkan ICD. The author closes the `libvulkan.so` gap of the sibling feature with a one-time `sudo apt install libvulkan-dev`. The script checks for the file and prints the fix. A second script, `scripts/check-gpu.sh`, reports the NVIDIA kernel module version, the user space library version, and the Vulkan ICD. It exits non-zero on a mismatch and prints the fix. The runbook `docs/testing-linux.md` lists the one-time setup and the script call. The .NET SDK already exists on this machine, so the SDK install becomes a check plus a documented one-time command.

---

## User Personas Alignment

No `docs/user-personas.md` file exists. The personas below come from the epic docs.

**Target Users:**
- [x] **The agent on Linux** (runs one script with no display and no user input, and reads one exit code and one line)
- [x] **The author on Linux** (sets up the machine one time and reads the fix when the GPU check fails)
- [ ] **The author on Windows** (not served here. `dev-ops/windows-test-path` serves this persona with the same command contract)

**User Subsets Served:**
- [x] **Hands-off** - The agent runs one command and reads one exit code.
- [x] **Simple** - The author follows one short runbook. Each failure message names its fix.

**UX Approach:**
- One script call with no argument. Two environment variables change the frame count and the smoke time limit (scope answer 4).
- The output on success is one line. The output on failure is one header line, at most 30 lines of the failed step, and the log path.
- Each check prints the exact command that fixes it.

---

## Goals

### Primary Goals
1. Give the agent one script that proves the build, the unit tests, and the Vulkan path on the RTX 2060 with no display.
2. Detect a driver mismatch before the smoke run, and print the fix.
3. Close the `libvulkan.so` gap of `dev-ops/solution-skeleton` with a one-time `libvulkan-dev` install. The script checks for the file.
4. Set the script pattern that `dev-ops/windows-test-path` mirrors and the `github-ci` epic reuses.

### Success Criteria
- [ ] `dotnet --version` prints a `10.` version.
- [ ] `scripts/check-gpu.sh` exits 0 on this machine and prints the three items.
- [ ] `scripts/check-gpu.sh` exits 1 and prints the fix when the kernel module version and the user space library version differ.
- [ ] `scripts/test-linux.sh`, with no `DISPLAY`, exits 0 and prints exactly one line.
- [ ] `scripts/test-linux.sh` exits non-zero on a build failure, a test failure, a GPU check failure, a smoke failure, and a smoke time-out.
- [ ] The smoke run loads the NVIDIA ICD and not the Lavapipe ICD.
- [ ] `docs/testing-linux.md` lists the one-time setup and the script call.

---

## Architecture Overview

Two `bash` scripts and one runbook. `test-linux.sh` runs a fixed list of steps. Each step writes its output to one log file. The first failed step stops the run. `check-gpu.sh` is a separate script, so the author can run it alone after a driver update. `test-linux.sh` calls it as one step.

### File Tree (end state)

```
brutal-ui/
├── scripts/
│   ├── test-linux.sh                # phase 1, phase 2 adds preflight and gpu steps
│   └── check-gpu.sh                 # phase 2
├── docs/
│   ├── testing-linux.md             # phase 3
│   └── features/dev-ops/linux-dev-environment/context.md   # each phase records results
└── artifacts/test-linux/            # written by each run, ignored by git (artifacts/)
    └── preflight.log, build.log, test.log, gpu.log, smoke.log
```

No other file changes. `.gitignore` already holds `artifacts/`.

### The Steps of `scripts/test-linux.sh`

| # | Step | Command or check | Failure exit code | Phase |
|---|------|------------------|-------------------|-------|
| 1 | `preflight` | `dotnet` on `PATH` with a `10.` version, `xvfb-run` on `PATH`, `timeout` on `PATH` | 1 | 2 |
| 2 | `build` | `dotnet build brutal-ui.sln` | the exit code of `dotnet` | 1 |
| 3 | `test` | `dotnet test --solution brutal-ui.sln` | the exit code of `dotnet` | 1 |
| 4 | `gpu` | `scripts/check-gpu.sh` | 1 | 2 |
| 5 | `loader` | read the folder of the x86-64 `libvulkan.so.1` entry of `ldconfig -p`, then test that `<folder>/libvulkan.so` exists (from `libvulkan-dev`) | 1 | 1 |
| 6 | `smoke` | see below | the exit code of the sample, or 124 on a time-out | 1 |

The smoke step runs this command line (one line in the script, split here for reading):

```
env -u WAYLAND_DISPLAY -u VK_DRIVER_FILES -u VK_ADD_DRIVER_FILES \
    XDG_SESSION_TYPE=x11 \
    VK_ICD_FILENAMES=/usr/share/vulkan/icd.d/nvidia_icd.json \
    timeout --kill-after=10 "$SMOKE_TIMEOUT" \
    xvfb-run -a dotnet run --project samples/HelloPanel/HelloPanel.csproj --no-build -- --frames "$FRAMES"
```

- `VK_ICD_FILENAMES` holds only the NVIDIA ICD, so the Vulkan loader loads no other driver. The loader gives `VK_DRIVER_FILES` priority over `VK_ICD_FILENAMES`, and `VK_ADD_DRIVER_FILES` adds drivers. The script removes both, so a variable in the caller environment cannot bring back Lavapipe.
- GLFW 3.4 reads `XDG_SESSION_TYPE` and `WAYLAND_DISPLAY` to select a platform. The script removes `WAYLAND_DISPLAY` and sets `XDG_SESSION_TYPE=x11`, so GLFW uses the Xvfb display of `xvfb-run` and not the Wayland desktop.
- No `-c` option appears. The build, the test, and the smoke run all use the Debug configuration, so `--no-build` finds the build output.

### Output Contract

| Case | Standard output | Standard error | Exit code |
|------|-----------------|----------------|-----------|
| Success | One line: `test-linux: PASS build, test, gpu, smoke (<FRAMES> frames) in <S> s` | nothing | 0 |
| A step fails | nothing | `test-linux: FAIL at <step> (exit <rc>)`, then the last 30 lines of `artifacts/test-linux/<step>.log`, then `test-linux: full log: artifacts/test-linux/<step>.log` | `<rc>` of the step |
| A smoke time-out | nothing | the header adds `, timed out after <SMOKE_TIMEOUT> s` | 124 (137 when `timeout` must kill) |
| A check fails (`preflight`, `loader`) | nothing | `test-linux: FAIL at <step>: <problem>`, then `test-linux: fix: <command>` | 1 |

In phase 1 the success line reads `build, test, smoke`. Phase 2 adds `gpu`.

### Output Contract of `scripts/check-gpu.sh`

On success, four lines on standard output, then exit 0:

```
check-gpu: kernel module 595.99.02
check-gpu: user space library 595.99.02
check-gpu: Vulkan ICD /usr/share/vulkan/icd.d/nvidia_icd.json (library libGLX_nvidia.so.0, API 1.x.y)
check-gpu: OK
```

On a problem, the script still reports every item it can read. Then it writes `check-gpu: FAIL <problem>` and `check-gpu: fix: <command or action>` to standard error for each problem, and exits 1.

| Item | Source | Problem | Fix text |
|------|--------|---------|----------|
| Kernel module version | Line 1 of `/proc/driver/nvidia/version`, the first `N.N` or `N.N.N` number | The file does not exist | Reboot this machine. If the problem stays, reinstall the NVIDIA driver package. |
| User space library version | `ldconfig -p`, the x86-64 entry of `libnvidia-ml.so.1`, then `readlink -f`, then the suffix after `libnvidia-ml.so.` | No entry | Install the NVIDIA driver package, for example `sudo ubuntu-drivers install`. |
| Vulkan ICD | `/usr/share/vulkan/icd.d/nvidia_icd.json`, its `library_path` and `api_version` read with `sed` | The file does not exist, or its library does not resolve | Install the NVIDIA driver package with its Vulkan ICD (`libnvidia-gl-<branch>`). |
| Version match | The kernel module version and the user space library version | The two differ | Reboot this machine. The reboot loads the kernel module of the installed driver. |

`nvidia-smi` is not the source of the user space version, because it fails with "Driver/library version mismatch" in exactly the case that the script must report.

Two environment variables replace the sources for a test: `CHECK_GPU_PROC_FILE` (default `/proc/driver/nvidia/version`) and `CHECK_GPU_ICD_FILE` (default `/usr/share/vulkan/icd.d/nvidia_icd.json`). The header comment of the script names them as test seams.

---

## Implementation Phases

Each phase is a slice: one narrow path that works end to end, with a demo line that a person can check. Section 11 of `.beast-mode/phase-handoff.md` holds the rules. The requirements do not name the demo of phase 1. The author chose the line below (scope answer 2).

### Phase 1: The Linux Test Script Runs Build, Test, and the GPU Smoke Run
**Goal:** Write `scripts/test-linux.sh` with the build, test, loader, and smoke steps, the failure output, and the one-line summary. Prove the full GPU path with no display.
**Demo:** the agent runs `env -u DISPLAY scripts/test-linux.sh` at the repository root, sees one line that starts with `test-linux: PASS`, and the exit code is 0.
**Needs:** none
**Agent tier:** STANDARD (sonnet)

**Tier note:** The research already proved each command of this phase on this machine. The plan fixes the load-bearing details: the exit code capture under `set -e`, the time limit, the ICD selection, and the GLFW platform selection. When the smoke step hangs past its time limit, or leaves an `Xvfb` process, stop and report. Do not try other designs.

**Tasks:**
- [ ] 1.1 Write the frame of `scripts/test-linux.sh`. Start with `#!/usr/bin/env bash` and `set -euo pipefail`. Add a header comment in ASD-STE100 prose: the purpose, the steps, the exit codes, and the two environment variables. Change to the repository root from the script path (`cd -- "$(dirname -- "${BASH_SOURCE[0]}")/.."`). Define the constants: the solution `brutal-ui.sln`, the sample project `samples/HelloPanel/HelloPanel.csproj`, the ICD `/usr/share/vulkan/icd.d/nvidia_icd.json`, `FRAMES="${BRUTAL_UI_FRAMES:-120}"`, `SMOKE_TIMEOUT="${BRUTAL_UI_SMOKE_TIMEOUT:-60}"`, and `LOG_DIR=artifacts/test-linux`. Export `DOTNET_NOLOGO=1`. Delete and create `LOG_DIR` at the start of each run.
- [ ] 1.2 Write two helpers. `run_step <name> <command...>` sends standard output and standard error of the command to `LOG_DIR/<name>.log`. It captures the exit code with `rc=0; "$@" >"$log" 2>&1 || rc=$?`, so `set -e` does not stop the script before the report. On a non-zero code it writes the failure block of the output contract to standard error and exits with `rc`. For `rc` 124 or 137 on the `smoke` step, the header adds `, timed out after <SMOKE_TIMEOUT> s`. `fail <step> <problem> <fix>` writes the two check lines of the output contract to standard error and exits 1.
- [ ] 1.3 Add the build and test steps: `run_step build dotnet build brutal-ui.sln` and `run_step test dotnet test --solution brutal-ui.sln`. Use these exact forms. Do not add `-c`. Do not use the positional form `dotnet test brutal-ui.sln`. It fails in Microsoft.Testing.Platform mode.
- [ ] 1.4 Add the `loader` step. Read `ldconfig -p` (use `/sbin/ldconfig` when `ldconfig` is not on `PATH`). Find the `libvulkan.so.1` entry for `x86-64`, and take the folder of its path. Then test `-e "<that folder>/libvulkan.so"`. `ldconfig -p` lists only the SONAME `libvulkan.so.1`, never the unversioned development link, so the step must not search `ldconfig -p` for `libvulkan.so`. When no `libvulkan.so.1` entry exists, or the unversioned file does not exist, call `fail loader "the unversioned Vulkan loader libvulkan.so does not exist" "sudo apt install libvulkan-dev"`. Create no link, and do not set `LD_LIBRARY_PATH`. Add a comment that gives the reason: `Brutal.Vulkan` loads the name `vulkan`, so .NET probes the unversioned `libvulkan.so`, and only `libvulkan-dev` installs that file. Name `docs/features/dev-ops/solution-skeleton/context.md`, section "Native Dependencies", in the comment.
- [ ] 1.5 Add the `smoke` step with the command line of the Architecture Overview. Keep `--kill-after=10`. Do not add `--foreground` to `timeout`. Without it, `timeout` signals its whole process group, so `xvfb-run`, `Xvfb`, `dotnet run`, and `HelloPanel` all stop.
- [ ] 1.6 Add the summary. Read the elapsed time from the `SECONDS` variable of `bash`. On success, write exactly one line to standard output: `test-linux: PASS build, test, smoke (<FRAMES> frames) in <S> s`. Write nothing else to standard output or standard error. The script reads the exit code of the sample, not its output (Mesa layers can print stray lines). Run `chmod +x scripts/test-linux.sh`. Run `bash -n scripts/test-linux.sh`. When `shellcheck` exists on the machine, run it and fix each finding. Do not install `shellcheck`.
- [ ] 1.7 Verify in one shell command, and record each result in `docs/features/dev-ops/linux-dev-environment/context.md`. (a) `env -u DISPLAY -u WAYLAND_DISPLAY scripts/test-linux.sh; echo "exit=$?"`: expect one PASS line and `exit=0`. (b) Run the script by its absolute path from the session scratchpad folder: expect the same result. (c) `BRUTAL_UI_FRAMES=0 scripts/test-linux.sh; echo "exit=$?"`: expect `FAIL at smoke (exit 2)`, a tail with a `Usage:` line, and `exit=2`. (d) Count `Xvfb` processes with `pgrep -c -x Xvfb`, then run `BRUTAL_UI_FRAMES=100000000 BRUTAL_UI_SMOKE_TIMEOUT=5 scripts/test-linux.sh; echo "exit=$?"`: expect `timed out after 5 s` and `exit=124`, and the same `Xvfb` count after the run. (e) `VK_LOADER_DEBUG=all scripts/test-linux.sh`, then search `artifacts/test-linux/smoke.log` for `nvidia_icd.json` and for `lvp`: expect matches for the first and none for the second. (f) `git status --porcelain`: expect no `artifacts/` path and no change outside `scripts/` and the feature folder.

**Estimated Steps:** about 42 (7 tasks, about six steps for one task)

**Files (2):** `scripts/test-linux.sh`, `docs/features/dev-ops/linux-dev-environment/context.md`

**Acceptance Criteria:**
- [ ] With no `DISPLAY`, `scripts/test-linux.sh` exits 0 and writes exactly one line, which starts with `test-linux: PASS`.
- [ ] A smoke failure exits with the exit code of the sample and shows at most 30 lines of `smoke.log` plus the log path.
- [ ] A smoke time-out exits 124 and leaves no new `Xvfb` process.
- [ ] The loader debug output shows the NVIDIA ICD and no Lavapipe ICD.
- [ ] The script uses the exact build, test, and smoke commands of the shared contract.
- [ ] A run changes no tracked file and adds no untracked file outside `artifacts/`.

---

### Phase 2: GPU Health Check and Preflight
**Goal:** Write `scripts/check-gpu.sh`. Run it in `test-linux.sh` after the tests and before the smoke run. Add the preflight checks for `dotnet`, `xvfb-run`, and `timeout`. A driver mismatch or a missing tool then gives a clear fix in place of a Vulkan error.
**Demo:** the agent runs `scripts/check-gpu.sh`, sees the kernel module version, the user space library version, the Vulkan ICD, and `check-gpu: OK`, and the exit code is 0.
**Needs:** Phase 1
**Agent tier:** STANDARD (sonnet)

**Tasks:**
- [ ] 2.1 Write the frame of `scripts/check-gpu.sh`. Start with `#!/usr/bin/env bash` and `set -euo pipefail`. Add a header comment in ASD-STE100 prose: the purpose, the three items, the exit codes, and the two test seams `CHECK_GPU_PROC_FILE` and `CHECK_GPU_ICD_FILE` with their defaults. Write a `report` helper (one `check-gpu:` line to standard output) and a `problem` helper (the `FAIL` line and the `fix:` line to standard error, and a status flag set to 1). The script checks every item, and then exits with the status flag. Use the same `ldconfig` lookup as task 1.4.
- [ ] 2.2 Add the kernel module item. Read line 1 of `CHECK_GPU_PROC_FILE`. Take the first number of the form `N.N` or `N.N.N` (`grep -oE '[0-9]+\.[0-9]+(\.[0-9]+)?' | head -n 1`). Report it. When the file does not exist, or holds no version, call `problem` with the fix of the check table.
- [ ] 2.3 Add the user space library item. Find the `x86-64` entry of `libnvidia-ml.so.1` in `ldconfig -p`. Resolve it with `readlink -f`. Take the suffix after `libnvidia-ml.so.`. Report it. When no entry exists, call `problem` with the fix of the check table. Add a comment: `nvidia-smi` fails on a mismatch, so the script reads the library file name.
- [ ] 2.4 Add the Vulkan ICD item. When `CHECK_GPU_ICD_FILE` does not exist, call `problem`. Otherwise read `library_path` and `api_version` with `sed`. Do not use `jq`. Confirm that the library resolves: an absolute path exists, or the file name appears in `ldconfig -p`. Report the ICD path, the library, and the API version on one line. When the library does not resolve, call `problem` with the fix of the check table.
- [ ] 2.5 Add the version match. When both versions exist and differ, call `problem` with the text `kernel module <k> does not match user space library <u>` and the fix `Reboot this machine. The reboot loads the kernel module of the installed driver.`. When the status flag is 0, report `OK`. Exit with the status flag. Run `chmod +x scripts/check-gpu.sh`, `bash -n`, and `shellcheck` when it exists.
- [ ] 2.6 Edit `scripts/test-linux.sh`. Add the `preflight` step before `build`. It checks with `command -v` that `dotnet`, `xvfb-run`, and `timeout` exist, and that `dotnet --version` starts with `10.`. Each failure calls `fail` with its fix: `sudo apt install dotnet-sdk-10.0`, `sudo apt install xvfb`, and `sudo apt install coreutils`. Add `run_step gpu scripts/check-gpu.sh` after `test` and before `loader`. The GPU check comes after the tests, so a driver problem does not hide the unit test result. Change the success line to `test-linux: PASS build, test, gpu, smoke (<FRAMES> frames) in <S> s`. Update the header comment.
- [ ] 2.7 Verify in one shell command, and record each result in `context.md`. (a) `scripts/check-gpu.sh; echo "exit=$?"`: expect four lines and `exit=0`. (b) Write a file in the session scratchpad with the line `NVRM version: NVIDIA UNIX x86_64 Kernel Module  595.91  Release Build`. Run `CHECK_GPU_PROC_FILE=<file> scripts/check-gpu.sh; echo "exit=$?"`: expect `595.91` and the real library version, a `FAIL` line, a `fix:` line with `Reboot`, and `exit=1`. (c) `CHECK_GPU_PROC_FILE=/nonexistent scripts/check-gpu.sh`: expect exit 1, and the other two items still reported. (d) `CHECK_GPU_ICD_FILE=/nonexistent scripts/check-gpu.sh`: expect exit 1 with the ICD fix. (e) `env -u DISPLAY scripts/test-linux.sh; echo "exit=$?"`: expect the PASS line with `gpu` and `exit=0`. (f) `CHECK_GPU_PROC_FILE=<file> scripts/test-linux.sh; echo "exit=$?"`: expect `FAIL at gpu (exit 1)`, the fix in the tail, and `exit=1`. (g) Repeat check (c) of task 1.7.

**Estimated Steps:** about 42 (7 tasks, about six steps for one task)

**Files (3):** `scripts/check-gpu.sh`, `scripts/test-linux.sh`, `docs/features/dev-ops/linux-dev-environment/context.md`

**Acceptance Criteria:**
- [ ] `scripts/check-gpu.sh` exits 0 on this machine and prints the three items and `check-gpu: OK`.
- [ ] A kernel module version that differs from the user space library version gives exit 1, both versions, and a fix with `Reboot`.
- [ ] A missing kernel module file or a missing ICD file gives exit 1 and the fix of the check table. The script still reports the other items.
- [ ] `scripts/test-linux.sh` runs the GPU check after the tests. A GPU check failure gives `FAIL at gpu (exit 1)` and shows the fix.
- [ ] `scripts/test-linux.sh` stops with a fix when `dotnet`, `xvfb-run`, or `timeout` is missing, or when `dotnet --version` does not start with `10.` (read check of the code).

---

### Phase 3: The Linux Testing Runbook
**Goal:** Write `docs/testing-linux.md` with the one-time setup and the script call. Record the SDK state and the system dependencies in `context.md`.
**Demo:** the author opens `docs/testing-linux.md`, runs each check command of its one-time setup table, runs the one script call that it names, and sees `test-linux: PASS`.
**Needs:** Phase 2
**Agent tier:** STANDARD (sonnet)

**Tasks:**
- [ ] 3.1 Check the SDK install with no `sudo`, in one shell command: `dpkg-query -W -f='${Status} ${Version}\n' dotnet-sdk-10.0`, `readlink -f "$(command -v dotnet)"`, and `dotnet --version`. Record the results in `context.md`. When the apt package `dotnet-sdk-10.0` is not the source of `/usr/bin/dotnet`, record the conflict with the requirement in `context.md`. The runbook still names the apt package as the install command. Do not install anything.
- [ ] 3.2 Write `docs/testing-linux.md` in ASD-STE100 prose. Sections: (1) Purpose, two sentences. (2) One-time setup: a table with the columns Item, Check, and Fix (the author runs each fix one time, with `sudo`). Rows: the .NET 10 SDK (`dotnet --version`, `sudo apt install dotnet-sdk-10.0`, from the Ubuntu archive, with a warning not to mix it with the `packages.microsoft.com` feed), Xvfb (`command -v xvfb-run`, `sudo apt install xvfb`), the Vulkan loader (`ls -l /usr/lib/x86_64-linux-gnu/libvulkan.so`, `sudo apt install libvulkan-dev`, with one note: `ldconfig -p` lists only `libvulkan.so.1`, so the `loader` step reads the folder of that entry and tests for `libvulkan.so` in it), and the NVIDIA driver (`scripts/check-gpu.sh`, the fix that the script prints). Add one sentence: `Brutal.Vulkan` loads the unversioned `libvulkan.so`, and only `libvulkan-dev` installs that file. (3) Run the tests: the call `scripts/test-linux.sh` from any folder, the success line, and the log folder `artifacts/test-linux/`. (4) Exit codes: the table of the output contract. (5) Settings: `BRUTAL_UI_FRAMES` and `BRUTAL_UI_SMOKE_TIMEOUT` with their defaults. (6) Failures and fixes: one row for each `FAIL at <step>` header, with the first thing to read and the fix. Include the GPU mismatch, the time-out, and a `DllNotFoundException` for `vulkan`. (7) Limits: no CI, no Docker, no OpenGL run, no pixel compare, one run at a time.
- [ ] 3.3 Write the system dependencies into `context.md`, per the rule of `AGENTS.md`. The scripts add no native file to the repository. List each system item that the scripts use: `dotnet`, `xvfb-run` and `Xvfb`, `timeout` (coreutils), `ldconfig`, `libvulkan.so` (libvulkan-dev), the NVIDIA ICD and `libnvidia-ml.so.1` (the NVIDIA driver). Record the answer to the open question of `docs/features/dev-ops/solution-skeleton/context.md`: the author installs `libvulkan-dev` one time with `sudo`, and the `loader` step checks for `libvulkan.so`. Add a follow-up item: report to BRUTAL that `Brutal.Vulkan` must also try `libvulkan.so.1` on Linux.
- [ ] 3.4 Verify. Run the Verification Method of the Definition of Done, steps 1 to 14. Compare each command in `docs/testing-linux.md` with the scripts, character for character. When `python3 /home/aaronstatic/.claude/skills/asd-ste100/scripts/ste-lint.py` exists, run it on `docs/testing-linux.md` and fix each finding. Record the results in `context.md`.

**Estimated Steps:** about 24 (4 tasks, about six steps for one task)

**Files (2):** `docs/testing-linux.md`, `docs/features/dev-ops/linux-dev-environment/context.md`

**Acceptance Criteria:**
- [ ] `docs/testing-linux.md` holds the one-time setup table, the script call, the exit codes, the settings, the failures and fixes, and the limits.
- [ ] Each command in the runbook matches the scripts exactly.
- [ ] `context.md` records the SDK state, the system dependencies, and the `libvulkan.so` answer.

---

## Key Technical Decisions

### Confirmed Assumptions

The author answered the four scope questions on 2026-10-16. Notes, section "Scope Answers", holds the answers.

### Decision 1: Plain `bash` scripts with `set -euo pipefail`
**Context:** The chosen approach names plain scripts in `scripts/`. The `github-ci` epic later runs them on a hosted runner.
**Decision:** Two `bash` scripts with `#!/usr/bin/env bash` and `set -euo pipefail`. They call only `dotnet`, `xvfb-run`, `timeout`, `ldconfig`, `readlink`, `grep`, `sed`, `tail`, `env`, and `ln`. No `jq`, no Docker, no `sudo`.
**Rationale:** Every tool exists on a stock Ubuntu machine with Xvfb. A hosted runner has the same tools.
**Alternatives Considered:** A `dotnet` tool or a C# script. Rejected: it needs a build before the build. A `Makefile`. Rejected: it adds a tool and hides the exit code rules.

### Decision 2: A one-time `libvulkan-dev` install
**Context:** Research finding 5. `Brutal.Vulkan` loads `libvulkan.so`. Only `libvulkan-dev` installs it. The sample cannot add its own resolver.
**Decision:** The author installs `libvulkan-dev` one time with `sudo`. The `loader` step reads the folder of the x86-64 `libvulkan.so.1` entry of `ldconfig -p`, and tests that `libvulkan.so` exists in that folder. `ldconfig -p` lists only the SONAME `libvulkan.so.1`, never the unversioned development link, so a search of `ldconfig -p` for `libvulkan.so` always fails (change of 2026-10-16, from the coordinator of the epic run). When the file is missing, the step fails and prints `sudo apt install libvulkan-dev`. The script makes no link and sets no `LD_LIBRARY_PATH`.
**Rationale:** The author chose this answer (scope answer 1). The machine gets the standard system file, so a plain `dotnet run` of the sample also works. The script stays simpler.
**Alternatives Considered:** A link in `artifacts/test-linux/lib/` on `LD_LIBRARY_PATH`. Rejected by the author. A link only when `libvulkan.so` is missing. Rejected: two code paths.

### Decision 3: One log file for each step, under `artifacts/test-linux/`
**Context:** The requirements ask for the last 30 lines of a failed step and one line on success.
**Decision:** Each step writes to `artifacts/test-linux/<step>.log`. The script deletes the folder at the start of each run. The logs stay after the run.
**Rationale:** The agent reads the full log after a failure with no second run. A verifier reads `smoke.log` after a success to confirm the ICD. The folder is ignored by git. A `mktemp` folder needs a cleanup trap and loses the log.
**Consequence:** Two runs at the same time write to the same folder. The runbook says: one run at a time.

### Decision 4: The step order build, test, gpu, loader, smoke
**Context:** The requirements give build, test, then the smoke run.
**Decision:** `preflight` first, then `build`, `test`, `gpu`, `loader`, and `smoke`. The first failure stops the run.
**Rationale:** The unit tests need no GPU. A driver problem then does not hide the unit test result. The GPU check comes right before the GPU use, so a mismatch shows as a fix and not as a Vulkan error.

### Decision 5: The exit code of the failed step passes through
**Context:** The requirements ask for a non-zero code on any failure.
**Decision:** `test-linux.sh` exits with the exit code of the failed step: 1 or 2 from `HelloPanel`, 124 or 137 from `timeout`, the `dotnet` code from build and test, and 1 from a check.
**Rationale:** The code tells the agent the failure type with no parse: 2 is a script error, 1 is a runtime failure, 124 is a stall. This keeps Decision 9 of `dev-ops/solution-skeleton` visible through the script.

### Decision 6: A time limit on the smoke run
**Context:** Research finding 9. The sample waits for fences and swapchain images with no time limit.
**Decision:** `timeout --kill-after=10 60` wraps `xvfb-run`. `BRUTAL_UI_SMOKE_TIMEOUT` changes the 60 seconds. The script does not pass `--foreground`.
**Rationale:** 120 frames take a few seconds after the `dotnet run` start. 60 seconds leaves a wide margin. Without `--foreground`, `timeout` puts the child in a new process group and signals the whole group, so `Xvfb` and `HelloPanel` stop too. `--kill-after` sends `SIGKILL` when a process ignores `SIGTERM`.

### Decision 7: The NVIDIA ICD only, and X11 only, for the smoke run
**Context:** Research finding 7. The Lavapipe ICD also runs the sample, so a fallback gives a false PASS. GLFW 3.4 selects its platform at run time and reads `XDG_SESSION_TYPE`.
**Decision:** The smoke command sets `VK_ICD_FILENAMES` to the NVIDIA ICD only, removes `VK_DRIVER_FILES` and `VK_ADD_DRIVER_FILES`, removes `WAYLAND_DISPLAY`, and sets `XDG_SESSION_TYPE=x11`.
**Rationale:** The loader then has one driver. When that driver fails, the sample exits 1. The run uses the Xvfb display on a desktop session too, so no window opens on the author screen.
**Sources:** https://glfw.org/docs/3.4/news.html (run-time platform selection, `XDG_SESSION_TYPE` support), https://github.com/KhronosGroup/Vulkan-Loader/blob/main/docs/LoaderInterfaceArchitecture.md (driver selection variables).

### Decision 8: The user space version comes from the `libnvidia-ml.so.1` file name
**Context:** `nvidia-smi` fails with "Driver/library version mismatch" when the versions differ.
**Decision:** `check-gpu.sh` resolves `libnvidia-ml.so.1` with `readlink -f` and reads the version from the file name. It reads the kernel module version from `/proc/driver/nvidia/version`.
**Rationale:** Both sources work in the mismatch case. `nvidia-smi` does not.

### Decision 9: The SDK install becomes a check
**Context:** Research finding 1. The SDK 10.0.112 exists at `/usr/bin/dotnet`.
**Decision:** The `preflight` step checks `dotnet --version` for `10.`. Task 3.1 checks the apt package with `dpkg-query`. The runbook holds `sudo apt install dotnet-sdk-10.0` as the one-time step.
**Rationale:** No agent task needs `sudo`. `global.json` pins `10.0.100` with `rollForward: latestFeature`, so any `10.0.1xx` SDK works.

### Decision 10: Two environment variables, no command-line options
**Context:** The verification needs a failure path and a time-out path. The requirements name "N frames".
**Decision:** `BRUTAL_UI_FRAMES` (default 120) and `BRUTAL_UI_SMOKE_TIMEOUT` (default 60). The configuration stays Debug. The ICD stays NVIDIA. `check-gpu.sh` has two test seams, `CHECK_GPU_PROC_FILE` and `CHECK_GPU_ICD_FILE`.
**Rationale:** The variables make the failure paths testable with no edit of the repository. `BRUTAL_UI_FRAMES=0` proves the exit code pass-through with the usage error of the sample. The `github-ci` epic adds a configuration or ICD option when it needs one (YAGNI). Open scope question 4 asks the author.

---

## Risks & Mitigation

| Risk | Likelihood | Impact | Mitigation |
|------|-----------|--------|------------|
| A driver update without a reboot breaks the GPU path again. | Medium | High | The `gpu` step reports the mismatch and the fix before the smoke run. |
| A GPU stall hangs the smoke run. | Low | High | `timeout --kill-after=10` with exit 124. Task 1.7 (d) tests it. |
| `timeout` stops `xvfb-run` but leaves `Xvfb` or `HelloPanel` alive. | Low | Medium | No `--foreground`, so `timeout` signals the whole process group. Task 1.7 (d) counts `Xvfb` processes before and after. |
| The loader falls back to Lavapipe and gives a false PASS. | Low | High | Decision 7. Task 1.7 (e) reads the loader debug output. |
| GLFW selects Wayland on the desktop session of the author and opens a real window. | Medium | Low | Decision 7. Definition of Done I5 (MANUAL) checks it on the desktop. |
| `ldconfig` is not on `PATH` for a non-root shell. | Low | Medium | Tasks 1.4 and 2.1 fall back to `/sbin/ldconfig`. |
| The `libvulkan.so.1` path differs on another distribution or a hosted runner. | Low | Low | The script reads the path from `ldconfig -p`. It holds no fixed library path. |
| A later BRUTAL version loads `libvulkan.so.1` itself. Then `libvulkan-dev` is not necessary. | Low | Low | The package does no harm. Remove the `loader` step when BRUTAL changes (follow-up of task 3.3). |
| A Windows checkout converts the `.sh` files to CRLF, and `bash` fails on `\r`. | Low | Medium | `dev-ops/windows-test-path` owns `.gitattributes` (scope answer 3). Definition of Done I2 checks the rule when the file exists. |
| The first restore needs the network. | Low | Low | The build log shows the NuGet error in the tail. The package cache on this machine already holds every package. |
| A Mesa layer or the loader prints extra lines. | Medium | Low | The output goes to the log only. The script reads exit codes, not output. |
| Two runs at the same time share `artifacts/test-linux/`. | Low | Low | The runbook says: one run at a time. |
| `shellcheck` does not exist on this machine. | Medium | Low | `bash -n` always runs. `shellcheck` runs when it exists. The plan installs nothing. |

---

## Dependencies

### Internal Dependencies
- `dev-ops/solution-skeleton` (complete): `brutal-ui.sln`, the three test projects, the `HelloPanel` sample, its command line contract, and the shared contract commands.
- `dev-ops/windows-test-path` (parallel, no blocking dependency): it owns `.gitattributes`. The `.sh` files need the rule `*.sh text eol=lf` there. This feature does not write `.gitattributes`.

### External Dependencies
- The .NET SDK 10.0.112 at `/usr/bin/dotnet` (research finding 1).
- The Ubuntu packages `xvfb` (for `xvfb-run` and `Xvfb`), `libvulkan1`, `libvulkan-dev`, `coreutils`, and `libc-bin` (for `ldconfig`). All exist today except `libvulkan-dev`, which the author installs one time.
- The NVIDIA driver 595.99.02 with the kernel module 595.99.02 and the ICD `/usr/share/vulkan/icd.d/nvidia_icd.json` (research finding 6).
- The NuGet package cache, or network access to nuget.org for a first restore.

---

## Definition of Done

All criteria below must pass. An independent evaluator runs them from the repository root `/home/aaronstatic/dev/brutal-ui` in a shell with no `DISPLAY`. "MANUAL" marks a criterion that needs the author or the desktop session.

### Functional Criteria
- [ ] F1. `dotnet --version` prints a version that starts with `10.`.
- [ ] F2. `scripts/check-gpu.sh` exits 0. Its standard output holds four lines: the kernel module version, the user space library version, the ICD line with `/usr/share/vulkan/icd.d/nvidia_icd.json`, and `check-gpu: OK`.
- [ ] F3. With `CHECK_GPU_PROC_FILE` set to a file that holds `NVRM version: NVIDIA UNIX x86_64 Kernel Module  595.91  Release Build`, `scripts/check-gpu.sh` exits 1. Its output holds `595.91`, the real user space version, a `check-gpu: FAIL` line, and a `check-gpu: fix:` line with the word `Reboot`.
- [ ] F4. With `CHECK_GPU_ICD_FILE=/nonexistent`, `scripts/check-gpu.sh` exits 1 and prints a fix for the ICD. It still prints both version lines.
- [ ] F5. `env -u DISPLAY -u WAYLAND_DISPLAY scripts/test-linux.sh` exits 0. Standard output holds exactly one line that matches `test-linux: PASS build, test, gpu, smoke (120 frames) in <N> s`. Standard error is empty.
- [ ] F6. The same call from another folder, by the absolute script path, gives the same result.
- [ ] F7. `BRUTAL_UI_FRAMES=0 scripts/test-linux.sh` exits 2. Standard error starts with `test-linux: FAIL at smoke (exit 2)`, holds at most 30 lines of log output with a `Usage:` line, and ends with the log path.
- [ ] F8. `BRUTAL_UI_FRAMES=100000000 BRUTAL_UI_SMOKE_TIMEOUT=5 scripts/test-linux.sh` exits 124. The header holds `timed out after 5 s`. `pgrep -c -x Xvfb` gives the same count before and after the run.
- [ ] F9. A temporary file `src/BrutalUi.Core/Broken.cs` with the text `class Broken {` makes `scripts/test-linux.sh` exit non-zero with `FAIL at build`. The tail shows a `CS` error. The evaluator deletes the file after the check.
- [ ] F10. With `CHECK_GPU_PROC_FILE` set to the file of F3, `scripts/test-linux.sh` exits 1 with `FAIL at gpu (exit 1)`, and the tail holds the fix.
- [ ] F11. After `VK_LOADER_DEBUG=all scripts/test-linux.sh`, `artifacts/test-linux/smoke.log` contains `nvidia_icd.json` and does not contain `lvp`.
- [ ] F12. `docs/testing-linux.md` exists and holds the one-time setup table, the call `scripts/test-linux.sh`, the exit code table, the two settings, the failures and fixes, and the limits.

### Quality Criteria
- [ ] Q1. Both scripts start with `#!/usr/bin/env bash`, hold `set -euo pipefail`, and are executable (`test -x`).
- [ ] Q2. `bash -n` passes for both scripts. When `shellcheck` exists, it reports no finding.
- [ ] Q3. A search for `sudo` in `scripts/` finds matches only inside fix text that the scripts print, never as a command.
- [ ] Q4. A search for `docker`, `jq`, and `apt` as commands in `scripts/` finds no match. (`apt` appears only inside fix text.)
- [ ] Q5. `scripts/test-linux.sh` holds `VK_ICD_FILENAMES=/usr/share/vulkan/icd.d/nvidia_icd.json`, `timeout --kill-after=10`, and `xvfb-run -a`.
- [ ] Q6. After a run, `git status --porcelain` shows no path under `artifacts/`, `bin/`, or `obj/`. The feature adds only `scripts/test-linux.sh`, `scripts/check-gpu.sh`, `docs/testing-linux.md`, and the feature docs.
- [ ] Q7. The header comments of both scripts and `docs/testing-linux.md` use ASD-STE100 prose: active voice, short sentences, no contractions.
- [ ] Q8. `docs/features/dev-ops/linux-dev-environment/context.md` lists each system dependency of the scripts and states that no native file enters the repository.

### Integration Criteria
- [ ] I1. `scripts/test-linux.sh` holds these exact strings: `dotnet build brutal-ui.sln`, `dotnet test --solution brutal-ui.sln`, and `dotnet run --project samples/HelloPanel/HelloPanel.csproj --no-build -- --frames`. It holds no `-c` option and no `dotnet test brutal-ui.sln`.
- [ ] I2. The feature adds no `.gitattributes`. When `.gitattributes` exists (from `dev-ops/windows-test-path`), `git check-attr eol -- scripts/test-linux.sh scripts/check-gpu.sh` prints `lf` for both files.
- [ ] I3. Each command in `docs/testing-linux.md` matches the scripts exactly.
- [ ] I4. `context.md` of this feature answers the open `libvulkan.so` question of `docs/features/dev-ops/solution-skeleton/context.md`.
- [ ] I5. (MANUAL) The author runs `scripts/test-linux.sh` in a terminal of the desktop session. The script prints the PASS line, and no window opens on the screen.

### Verification Method

Run each step from `/home/aaronstatic/dev/brutal-ui` in a shell with no `DISPLAY`. Use the session scratchpad folder for temporary files.

1. `dotnet --version`. Expect `10.` (F1).
2. `head -n 2 scripts/test-linux.sh scripts/check-gpu.sh && test -x scripts/test-linux.sh && test -x scripts/check-gpu.sh && bash -n scripts/test-linux.sh && bash -n scripts/check-gpu.sh; echo "exit=$?"`. Expect the shebang and `exit=0`. Search both files for `set -euo pipefail` (Q1, Q2). Run `shellcheck scripts/*.sh` when it exists.
3. `scripts/check-gpu.sh; echo "exit=$?"`. Expect four lines and `exit=0` (F2).
4. Write `<scratch>/proc-version` with the line of F3. Run `CHECK_GPU_PROC_FILE=<scratch>/proc-version scripts/check-gpu.sh; echo "exit=$?"`. Expect F3.
5. `CHECK_GPU_ICD_FILE=/nonexistent scripts/check-gpu.sh; echo "exit=$?"`. Expect F4.
6. `env -u DISPLAY -u WAYLAND_DISPLAY scripts/test-linux.sh >"<scratch>/out" 2>"<scratch>/err"; echo "exit=$?"; cat "<scratch>/out"; wc -l "<scratch>/out" "<scratch>/err"`. Expect `exit=0`, one line in `out`, zero lines in `err` (F5).
7. `cd <scratch> && /home/aaronstatic/dev/brutal-ui/scripts/test-linux.sh; echo "exit=$?"`. Expect the PASS line and `exit=0` (F6).
8. `BRUTAL_UI_FRAMES=0 scripts/test-linux.sh; echo "exit=$?"`. Expect F7.
9. `pgrep -c -x Xvfb; BRUTAL_UI_FRAMES=100000000 BRUTAL_UI_SMOKE_TIMEOUT=5 scripts/test-linux.sh; echo "exit=$?"; pgrep -c -x Xvfb`. Expect F8.
10. Write `src/BrutalUi.Core/Broken.cs` with `class Broken {`. Run `scripts/test-linux.sh; echo "exit=$?"`. Expect F9. Delete the file, and confirm with `git status --porcelain src/`.
11. `CHECK_GPU_PROC_FILE=<scratch>/proc-version scripts/test-linux.sh; echo "exit=$?"`. Expect F10.
12. `VK_LOADER_DEBUG=all scripts/test-linux.sh; grep -c nvidia_icd.json artifacts/test-linux/smoke.log; grep -c lvp artifacts/test-linux/smoke.log`. Expect a count above 0, then 0 (F11).
13. Search `scripts/` for `sudo`, `apt`, `docker`, and `jq`. Read each match (Q3, Q4). Search `scripts/test-linux.sh` for the strings of Q5 and I1.
14. `git status --porcelain`. Expect Q6. When `.gitattributes` exists, run the command of I2.
15. Read `docs/testing-linux.md`. Confirm the sections of F12, and compare each command with the scripts (I3).
16. Read `docs/features/dev-ops/linux-dev-environment/context.md`. Confirm Q8 and I4.
17. (MANUAL) On the desktop session, run `scripts/test-linux.sh`. Expect the PASS line and no window on the screen (I5).

---

## Testing Strategy

### Unit Tests
- None. The feature holds no C# code. The scripts have no unit test framework (YAGNI).

### Integration Tests
- The script run itself is the integration test: build, unit tests, GPU check, and the Vulkan smoke run on the RTX 2060.
- Failure injection with no edit of the repository: `BRUTAL_UI_FRAMES=0` (exit code pass-through), `BRUTAL_UI_SMOKE_TIMEOUT=5` (time limit and process cleanup), `CHECK_GPU_PROC_FILE` and `CHECK_GPU_ICD_FILE` (GPU check failures).
- Failure injection with a temporary edit: one broken source file for the build step (F9). The evaluator deletes it after the check.
- `VK_LOADER_DEBUG=all` proves the ICD selection.

### Manual Testing
- The desktop run (I5): no window opens.
- A real driver mismatch after the next driver update: the author runs `scripts/check-gpu.sh` before the reboot and confirms the fix text.

### Not Tested Here
- A test failure in `dotnet test`. The build failure (F9) proves the same `run_step` path. A later feature with real tests exercises it.
- The Windows scripts. `dev-ops/windows-test-path` owns them.
- A hosted CI runner. The `github-ci` epic owns it.

---

## Related Documentation

- **Requirements:** `docs/features/dev-ops/linux-dev-environment/requirements.md`
- **Epic:** `docs/features/dev-ops/epic-overview.md`, `docs/features/dev-ops/epic-requirements.md`
- **Siblings:** `docs/features/dev-ops/solution-skeleton/implementation.md` (section "The Shared Contract with the Sibling Features"), `docs/features/dev-ops/solution-skeleton/context.md` (sections "Native Dependencies", "HelloPanel Command Line Contract", and "Open Questions"), `docs/features/dev-ops/windows-test-path/requirements.md`
- **Rules:** `AGENTS.md`, `.beast-mode/phase-handoff.md` sections 1 and 11, `.beast-mode/tool-rules.md`
- **External:** https://glfw.org/docs/3.4/news.html, https://github.com/KhronosGroup/Vulkan-Loader/blob/main/docs/LoaderInterfaceArchitecture.md

---

## Notes

### Scope Answers (2026-10-16)

1. **libvulkan.so:** The author installs `libvulkan-dev` one time with `sudo`. The `loader` step checks for `libvulkan.so` and prints the fix when it is missing.
2. **Phase 1 demo:** The agent runs `env -u DISPLAY scripts/test-linux.sh` at the repository root, sees one line that starts with `test-linux: PASS`, and the exit code is 0.
3. **`.gitattributes`:** `dev-ops/windows-test-path` owns `.gitattributes` and adds the `*.sh text eol=lf` rule. This feature writes no `.gitattributes`.
4. **Run settings:** `BRUTAL_UI_FRAMES` (default 120) and `BRUTAL_UI_SMOKE_TIMEOUT` (default 60 s). The configuration stays Debug, and the ICD stays NVIDIA.

### Other Notes

1. **SDK install conflict.** The requirements say "the apt package `dotnet-sdk-10.0` is installed". Research finding 1 says the SDK exists at `/usr/bin/dotnet`, but nobody confirmed the source. Task 3.1 checks the package with `dpkg-query` and records any conflict. No agent task installs the SDK.
2. **Reboot dependency.** The requirements name a reboot for the 595.91 and 595.99 mismatch. Research finding 6 says the reboot happened, and both versions are 595.99.02. `check-gpu.sh` guards a future mismatch.
3. **"The last 30 lines of each step on a failure."** The plan reads this as the last 30 lines of the failed step. The steps before it passed, and their logs stay in `artifacts/test-linux/`.
4. **`VK_ICD_FILENAMES` is the older name.** New Vulkan loaders prefer `VK_DRIVER_FILES` but still read `VK_ICD_FILENAMES`. The requirements name `VK_ICD_FILENAMES`, and research finding 7 proved it. The script removes `VK_DRIVER_FILES`, so the older name always takes effect.
5. **The `github-ci` epic.** A hosted runner has no NVIDIA GPU. That epic must add an ICD choice or skip the `gpu` and `smoke` steps. This plan does not prepare it (YAGNI).
6. **Upstream report.** `Brutal.Vulkan` must try `libvulkan.so.1` on Linux. Then the `loader` step can go. Task 3.3 records the follow-up.
7. **Agent discovery.** `AGENTS.md` does not name `scripts/test-linux.sh`. A later change can add one line under "Read first" or "Workflow". This feature does not edit `AGENTS.md`.
8. **The epic overview.** After this feature lands, `/update-epic dev-ops` records the `libvulkan.so` answer and the `.gitattributes` coordination under Tech Debt / Findings.
9. **No user personas file.** `docs/user-personas.md` does not exist. The personas of this plan come from the epic docs.

---

*This plan will be updated as implementation progresses. See context.md and tasks.md for current status.*
