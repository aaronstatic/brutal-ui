# dev-ops/linux-dev-environment - Task Checklist

**Last Updated:** 2026-10-16 14:00
**Progress:** 18/18 tasks complete (100%)
**Advanced phases:** none (all phases STANDARD, sonnet)

---

## Phase 1: The Linux Test Script Runs Build, Test, and the GPU Smoke Run (7/7 complete)

**Demo:** the agent runs `env -u DISPLAY scripts/test-linux.sh` at the repository root, sees one line that starts with `test-linux: PASS`, and the exit code is 0.
**Needs:** none

- [x] ✅ **1.1 Write the frame of `scripts/test-linux.sh`**
  - Description: `#!/usr/bin/env bash` and `set -euo pipefail`. ASD-STE100 header comment: the purpose, the steps, the exit codes, and the two environment variables. Change to the repository root from the script path (`cd -- "$(dirname -- "${BASH_SOURCE[0]}")/.."`). Constants: the solution `brutal-ui.sln`, the sample project `samples/HelloPanel/HelloPanel.csproj`, the ICD `/usr/share/vulkan/icd.d/nvidia_icd.json`, `FRAMES="${BRUTAL_UI_FRAMES:-120}"`, `SMOKE_TIMEOUT="${BRUTAL_UI_SMOKE_TIMEOUT:-60}"`, and `LOG_DIR=artifacts/test-linux`. Export `DOTNET_NOLOGO=1`. Delete and create `LOG_DIR` at the start of each run.
  - File(s): `scripts/test-linux.sh`
  - Estimate: 6 steps

- [x] ✅ **1.2 Write the `run_step` and `fail` helpers**
  - Description: `run_step <name> <command...>` sends standard output and standard error to `LOG_DIR/<name>.log`. It captures the exit code with `rc=0; "$@" >"$log" 2>&1 || rc=$?`. On a non-zero code it writes the failure block of the output contract to standard error and exits with `rc`. For `rc` 124 or 137 on the `smoke` step, the header adds `, timed out after <SMOKE_TIMEOUT> s`. `fail <step> <problem> <fix>` writes the two check lines of the output contract to standard error and exits 1.
  - File(s): `scripts/test-linux.sh`
  - Estimate: 6 steps

- [x] ✅ **1.3 Add the build and test steps**
  - Description: `run_step build dotnet build brutal-ui.sln` and `run_step test dotnet test --solution brutal-ui.sln`. Use these exact forms. No `-c`. No positional form `dotnet test brutal-ui.sln`.
  - File(s): `scripts/test-linux.sh`
  - Estimate: 3 steps

- [x] ✅ **1.4 Add the `loader` step**
  - Description: Read `ldconfig -p` (use `/sbin/ldconfig` when `ldconfig` is not on `PATH`). Find the `libvulkan.so.1` entry for `x86-64`, and take the folder of its path. Test `-e "<that folder>/libvulkan.so"`. `ldconfig -p` lists only the SONAME, never the unversioned link (change of 2026-10-16, see `inbox.md`). When no entry or no file exists, call `fail loader "the unversioned Vulkan loader libvulkan.so does not exist" "sudo apt install libvulkan-dev"`. Create no link, and do not set `LD_LIBRARY_PATH`. Comment with the reason, and name `docs/features/dev-ops/solution-skeleton/context.md`, section "Native Dependencies".
  - File(s): `scripts/test-linux.sh`
  - Estimate: 5 steps

- [x] ✅ **1.5 Add the `smoke` step**
  - Description: The command line of the Architecture Overview: `env -u WAYLAND_DISPLAY -u VK_DRIVER_FILES -u VK_ADD_DRIVER_FILES XDG_SESSION_TYPE=x11 VK_ICD_FILENAMES=/usr/share/vulkan/icd.d/nvidia_icd.json timeout --kill-after=10 "$SMOKE_TIMEOUT" xvfb-run -a dotnet run --project samples/HelloPanel/HelloPanel.csproj --no-build -- --frames "$FRAMES"`. Keep `--kill-after=10`. Do not add `--foreground`.
  - File(s): `scripts/test-linux.sh`
  - Estimate: 4 steps

- [x] ✅ **1.6 Add the summary and the static checks**
  - Description: Elapsed time from `SECONDS`. On success, exactly one line to standard output: `test-linux: PASS build, test, smoke (<FRAMES> frames) in <S> s`. Nothing else on standard output or standard error. Read the exit code of the sample, not its output. `chmod +x`, `bash -n`, and `shellcheck` when it exists. Do not install `shellcheck`.
  - File(s): `scripts/test-linux.sh`
  - Estimate: 5 steps

- [x] ✅ **1.7 Verify the script and record the results**
  - Description: Run checks (a) to (f) of task 1.7 in `implementation.md`. Record each result in `context.md`.
  - (a) and (b): the PASS run with no `DISPLAY`, and the run by the absolute path from the scratchpad.
  - (c) and (d): `BRUTAL_UI_FRAMES=0` gives exit 2. The time-out gives exit 124 and leaves no new `Xvfb`.
  - (e) and (f): `VK_LOADER_DEBUG=all` shows `nvidia_icd.json` and no `lvp`. `git status --porcelain` shows no `artifacts/` path.
  - File(s): `docs/features/dev-ops/linux-dev-environment/context.md`
  - Estimate: 8 steps

---

## Phase 2: GPU Health Check and Preflight (7/7 complete)

**Demo:** the agent runs `scripts/check-gpu.sh`, sees the kernel module version, the user space library version, the Vulkan ICD, and `check-gpu: OK`, and the exit code is 0.
**Needs:** Phase 1

- [x] ✅ **2.1 Write the frame of `scripts/check-gpu.sh`**
  - Description: `#!/usr/bin/env bash` and `set -euo pipefail`. ASD-STE100 header comment: the purpose, the three items, the exit codes, and the two test seams `CHECK_GPU_PROC_FILE` and `CHECK_GPU_ICD_FILE` with their defaults. A `report` helper (one `check-gpu:` line to standard output) and a `problem` helper (the `FAIL` line and the `fix:` line to standard error, and a status flag set to 1). Check every item, then exit with the status flag. Same `ldconfig` lookup as task 1.4.
  - File(s): `scripts/check-gpu.sh`
  - Estimate: 6 steps

- [x] ✅ **2.2 Add the kernel module item**
  - Description: Line 1 of `CHECK_GPU_PROC_FILE`, first `N.N` or `N.N.N` number (`grep -oE '[0-9]+\.[0-9]+(\.[0-9]+)?' | head -n 1`). Report it. A missing file or no version calls `problem` with the fix of the check table.
  - File(s): `scripts/check-gpu.sh`
  - Estimate: 4 steps

- [x] ✅ **2.3 Add the user space library item**
  - Description: The `x86-64` entry of `libnvidia-ml.so.1` in `ldconfig -p`, resolved with `readlink -f`, the suffix after `libnvidia-ml.so.`. Report it. No entry calls `problem` with the fix of the check table. Comment: `nvidia-smi` fails on a mismatch, so the script reads the library file name.
  - File(s): `scripts/check-gpu.sh`
  - Estimate: 4 steps

- [x] ✅ **2.4 Add the Vulkan ICD item**
  - Description: A missing `CHECK_GPU_ICD_FILE` calls `problem`. Otherwise read `library_path` and `api_version` with `sed` (no `jq`). Confirm that the library resolves: an absolute path exists, or the file name appears in `ldconfig -p`. Report the ICD path, the library, and the API version on one line. A library that does not resolve calls `problem` with the fix of the check table.
  - File(s): `scripts/check-gpu.sh`
  - Estimate: 5 steps

- [x] ✅ **2.5 Add the version match and the static checks**
  - Description: When both versions exist and differ, call `problem` with `kernel module <k> does not match user space library <u>` and the fix `Reboot this machine. The reboot loads the kernel module of the installed driver.`. When the status flag is 0, report `OK`. Exit with the status flag. `chmod +x`, `bash -n`, and `shellcheck` when it exists.
  - File(s): `scripts/check-gpu.sh`
  - Estimate: 4 steps

- [x] ✅ **2.6 Add the preflight and gpu steps to `test-linux.sh`**
  - Description: `preflight` before `build`: `command -v` for `dotnet`, `xvfb-run`, and `timeout`, and `dotnet --version` starts with `10.`. Fixes: `sudo apt install dotnet-sdk-10.0`, `sudo apt install xvfb`, and `sudo apt install coreutils`. `run_step gpu scripts/check-gpu.sh` after `test` and before `loader`. Success line becomes `test-linux: PASS build, test, gpu, smoke (<FRAMES> frames) in <S> s`. Update the header comment.
  - File(s): `scripts/test-linux.sh`
  - Estimate: 6 steps

- [x] ✅ **2.7 Verify the GPU check and record the results**
  - Description: Checks (a) to (g) of task 2.7 in `implementation.md`: the OK run, the 595.91 mismatch file, `CHECK_GPU_PROC_FILE=/nonexistent`, `CHECK_GPU_ICD_FILE=/nonexistent`, the PASS run of `test-linux.sh` with `gpu`, `FAIL at gpu (exit 1)` with the mismatch file, and check (c) of task 1.7 again. Record each result in `context.md`.
  - File(s): `docs/features/dev-ops/linux-dev-environment/context.md`
  - Estimate: 8 steps

---

## Phase 3: The Linux Testing Runbook (4/4 complete)

**Demo:** the author opens `docs/testing-linux.md`, runs each check command of its one-time setup table, runs the one script call that it names, and sees `test-linux: PASS`.
**Needs:** Phase 2

- [x] ✅ **3.1 Check the SDK install with no `sudo`**
  - Description: `dpkg-query -W -f='${Status} ${Version}\n' dotnet-sdk-10.0`, `readlink -f "$(command -v dotnet)"`, and `dotnet --version` in one shell command. Record the results in `context.md`. When the apt package is not the source of `/usr/bin/dotnet`, record the conflict with the requirement. Install nothing.
  - File(s): `docs/features/dev-ops/linux-dev-environment/context.md`
  - Estimate: 3 steps

- [x] ✅ **3.2 Write `docs/testing-linux.md`**
  - Description: Write ASD-STE100 prose. Task 3.2 of `implementation.md` holds the full text of each section and each row.
  - Sections: (1) Purpose. (2) One-time setup. (3) Run the tests. (4) Exit codes. (5) Settings. (6) Failures and fixes. (7) Limits.
  - The setup table has the columns Item, Check, and Fix. Its rows are the .NET 10 SDK, Xvfb, the Vulkan loader, and the NVIDIA driver.
  - The failures table includes the GPU mismatch, the time-out, and a `DllNotFoundException` for `vulkan`.
  - File(s): `docs/testing-linux.md`
  - Estimate: 8 steps

- [x] ✅ **3.3 Record the system dependencies in `context.md`**
  - Description: Each system item that the scripts use: `dotnet`, `xvfb-run` and `Xvfb`, `timeout` (coreutils), `ldconfig`, `libvulkan.so` (libvulkan-dev), the NVIDIA ICD and `libnvidia-ml.so.1` (the NVIDIA driver). No native file enters the repository. The answer to the open `libvulkan.so` question of `docs/features/dev-ops/solution-skeleton/context.md`. A follow-up item: report to BRUTAL that `Brutal.Vulkan` must also try `libvulkan.so.1` on Linux.
  - File(s): `docs/features/dev-ops/linux-dev-environment/context.md`
  - Estimate: 3 steps

- [x] ✅ **3.4 Run the Definition of Done verification**
  - Description: Steps 1 to 14 of the Verification Method. Compare each command in `docs/testing-linux.md` with the scripts, character for character. Run `python3 /home/aaronstatic/.claude/skills/asd-ste100/scripts/ste-lint.py` on `docs/testing-linux.md` when it exists, and fix each finding. Record the results in `context.md`.
  - File(s): `docs/testing-linux.md`, `docs/features/dev-ops/linux-dev-environment/context.md`
  - Estimate: 10 steps

---

## Bugs & Issues

**Active Bugs:**
- none

**Fixed Bugs:**
- none

---

## Technical Debt

- none

---

## Task Status Legend

- [ ] Not started
- [ ] 🔄 In progress
- [ ] ⏸️ Blocked (waiting on something)
- [x] ✅ Completed
- [x] ❌ Cancelled (not done)

---

## Progress Tracking

### Completed This Session (2026-10-16)
- ✅ Tasks 1.1 to 1.7, 2.1 to 2.7, and 3.1 to 3.4. All completed 2026-10-16.
- ✅ Task 1.4 changed during the run: the `loader` step reads the folder of the `libvulkan.so.1` entry of `ldconfig -p` (fix brief `phase-1-fix1.md`).
- ✅ Cross-phase review: CR-1, CR-2, CR-3, CR-4, CR-6, and CR-7 fixed. CR-5 declined. Verifier PASS.

### Discovered New Tasks
- none in this feature. Follow-up items for later work sit in `context.md`, section "Next Steps".

### Blocked Items
- none

---

## Notes

### Task Estimation
One step is one tool call of an agent. A typical task costs about six steps (see `.beast-mode/phase-handoff.md` section 1).
- 🟢 Small (up to 6 steps)
- 🟡 Medium (7 to 15 steps)
- 🔴 Large (more than 15 steps, so split the task)

---

*Update this file when a task is done. Mark each done task with ✅ at once. Add each new task when you find it.*
