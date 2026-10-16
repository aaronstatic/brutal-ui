# dev-ops/linux-dev-environment - Context & Decisions

**Last Updated:** 2026-10-16 14:00
**Current Phase:** Complete (all three phases and the cross-phase review done)
**Status:** 🟢 Ready for Review

---

## Quick Status

**What's Done:**
- ✅ Implementation plan (`implementation.md`), with the scope answers of the author in Notes
- ✅ Dev docs scaffolded (`context.md`, `tasks.md`, `inbox.md`)
- ✅ Phase 1: `scripts/test-linux.sh` with the build, test, loader, and smoke steps. Verifier PASS with no shim, Demo PASS.
- ✅ Phase 2: `scripts/check-gpu.sh` and the preflight and gpu steps. Verifier PASS, Demo PASS.
- ✅ Phase 3: the runbook `docs/testing-linux.md`, the SDK check, the system dependency list, and the verification steps 1 to 14. Verifier PASS, Demo PASS.
- ✅ Cross-phase review: 7 findings. CR-1, CR-2, CR-3, CR-4, CR-6, and CR-7 fixed. CR-5 declined with a reason. Verifier PASS.
- ✅ Next feature draft: `docs/features/dev-ops/windows-test-path/implementation.md` (3 phases, 19 tasks, 6 open scope questions in Notes)

**What's Next:**
- 📋 A person tests the items of `## Needs Human Verification` (the desktop run I5 first)
- 📋 The caller commits the work

**Blockers:**
- none. The author installed libvulkan-dev (1.4.341.0-1) during the run.

---

## Key Files

### Core Implementation
- `scripts/test-linux.sh` - The full Linux test run: preflight, build, test, gpu, loader, smoke (phases 1 and 2)
- `scripts/check-gpu.sh` - The NVIDIA kernel module, user space library, and Vulkan ICD check (phase 2)
- `docs/testing-linux.md` - The runbook: one-time setup and the script call (phase 3)

### Related Files
- `docs/features/dev-ops/linux-dev-environment/implementation.md` - Implementation plan
- `docs/features/dev-ops/solution-skeleton/context.md` - The shared contract, the `HelloPanel` command line contract, and the Native Dependencies section

---

## Important Decisions

The plan holds decisions 1 to 10 and the scope answers 1 to 4. This section records the decisions made during the build.

### Phase 1 (2026-10-16)
- `scripts/test-linux.sh` exists, is executable, and passes `bash -n`. `shellcheck` does not exist on this machine, so no `shellcheck` run.
- The loader test reads `ldconfig -p` into a variable first. A direct pipe into `grep -q` can fail under `pipefail` (SIGPIPE). (Superseded by fix 1: the first pattern searched for the unversioned `libvulkan.so` entry, which `ldconfig -p` never lists.)
- **Change of task 1.4 (2026-10-16, from the coordinator of the epic run, inbox line 1).** `ldconfig -p` lists only the SONAME `libvulkan.so.1`, never the unversioned development link `libvulkan.so`. The `loader` step now takes the folder of the x86-64 `libvulkan.so.1` entry and tests `-e "<folder>/libvulkan.so"`. The plan (`implementation.md` step table row 5, task 1.4, Decision 2, and the runbook row of task 3.2) now states this lookup. The runbook check for the author is `ls -l /usr/lib/x86_64-linux-gnu/libvulkan.so`.
- The constants `SOLUTION`, `SAMPLE`, and `NVIDIA_ICD` exist as the brief states, but the build, test, and smoke lines use the literal strings that the later check searches for. The three constants are therefore unused in the script text.
- Check results of task 1.7. The real machine has no `libvulkan.so` (the author installs `libvulkan-dev` with `sudo`). Real run with no shim: `test-linux: FAIL at loader: the unversioned Vulkan loader libvulkan.so does not exist`, then `test-linux: fix: sudo apt install libvulkan-dev`, exit 1. This run waits on `sudo apt install libvulkan-dev`. Checks (a) to (e) ran with a shim outside the repository (`PATH=<scratchpad>/vkshim/bin:$PATH LD_LIBRARY_PATH=<scratchpad>/vkshim/lib`). The shim holds a link `libvulkan.so` to `libvulkan.so.1` and an `ldconfig` wrapper that adds one `libvulkan.so` line.
  - (a) with shim: `env -u DISPLAY -u WAYLAND_DISPLAY scripts/test-linux.sh` printed `test-linux: PASS build, test, smoke (120 frames) in 10 s`, exit=0.
  - (b) with shim: the run by absolute path from the scratchpad folder printed `test-linux: PASS build, test, smoke (120 frames) in 8 s`, exit=0.
  - (c) with shim: `BRUTAL_UI_FRAMES=0` printed `FAIL at smoke (exit 2)`, then `Usage: HelloPanel [--frames <N>]`, then the log path, exit=2.
  - (d) with shim: `Xvfb` count 0 before and 0 after. `BRUTAL_UI_FRAMES=100000000 BRUTAL_UI_SMOKE_TIMEOUT=5` printed `FAIL at smoke (exit 124), timed out after 5 s`, exit=124. No `HelloPanel` process remained.
  - (e) with shim: `VK_LOADER_DEBUG=all` run passed. `smoke.log` holds 12 lines with `nvidia_icd.json` and 0 lines with `lvp`.
  - (f) without shim need: `git status --porcelain` shows no `artifacts/` path. The only changes are the feature folder files (`implementation.md`, `context.md`, `inbox.md`, `tasks.md`) and the new `scripts/` folder.

### Phase 2 (2026-10-16)
- `scripts/check-gpu.sh` exists, is executable, and passes `bash -n`. `shellcheck` does not exist, so no `shellcheck` run.
- The script reads `ldconfig -p` into a variable first. The helper `ldconfig_path <soname>` matches the exact name with `x86-64`. It finds `libnvidia-ml.so.1` and the bare `library_path` of the ICD (`libGLX_nvidia.so.0`).
- The user space version comes from `readlink -f` of the `libnvidia-ml.so.1` path, not from `nvidia-smi`. `nvidia-smi` fails on a mismatch.
- The ICD `library_path` and `api_version` come from `sed`. The script uses no `jq`.
- `scripts/test-linux.sh` has the `preflight` step (`dotnet`, `xvfb-run`, `timeout`, and `dotnet --version` of 10.x), and `run_step gpu scripts/check-gpu.sh` between `test` and `loader`. The success line is `test-linux: PASS build, test, gpu, smoke (<FRAMES> frames) in <S> s`. The header comment lists the new steps.
- The preflight failure paths have a read check only (the three tools exist on this machine).
- Check results of task 2.7 (no shim, no `sudo`):
  - (a) `scripts/check-gpu.sh`: four lines (kernel module 595.99.02, user space library 595.99.02, Vulkan ICD with `libGLX_nvidia.so.0` and API 1.4.329, `OK`), exit=0.
  - (b) `CHECK_GPU_PROC_FILE=<scratchpad>/proc-version` (595.91): reported `595.91` and `595.99.02`, a `FAIL ... does not match` line, a `fix: Reboot ...` line, exit=1.
  - (c) `CHECK_GPU_PROC_FILE=/nonexistent`: FAIL and kernel module fix, the other two items still reported, exit=1.
  - (d) `CHECK_GPU_ICD_FILE=/nonexistent`: FAIL and ICD fix, kernel module and user space library reported, exit=1.
  - (e) `env -u DISPLAY scripts/test-linux.sh`: `test-linux: PASS build, test, gpu, smoke (120 frames) in 11 s`, exit=0.
  - (f) `CHECK_GPU_PROC_FILE=<file> scripts/test-linux.sh`: `test-linux: FAIL at gpu (exit 1)`, the check-gpu lines and the `Reboot` fix in the tail, exit=1.
  - (g) `BRUTAL_UI_FRAMES=0 scripts/test-linux.sh`: `test-linux: FAIL at smoke (exit 2)`, exit=2.
  - `git status --porcelain` shows no `artifacts/` path.
- Deviation: none.

### Phase 3 (2026-10-16)
- **Task 3.1, SDK check (no `sudo`, nothing installed).** `dpkg-query -W -f='${Status} ${Version}\n' dotnet-sdk-10.0` printed `install ok installed 10.0.112-0ubuntu1~26.04.1`. `readlink -f "$(command -v dotnet)"` printed `/usr/lib/dotnet/dotnet`. `dotnet --version` printed `10.0.112`. `/usr/bin/dotnet` is a link to `../lib/dotnet/dotnet`, and `dpkg -S /usr/lib/dotnet/dotnet` names the package `dotnet-host-10.0`, which the Ubuntu package `dotnet-sdk-10.0` pulls in. So the apt package `dotnet-sdk-10.0` is the source of `/usr/bin/dotnet`. No conflict with the requirement. The runbook names `sudo apt install dotnet-sdk-10.0` as the install command.
- **Task 3.2.** `docs/testing-linux.md` exists with the seven sections: Purpose, One-time setup, Run the tests, Exit codes, Settings, Failures and fixes, Limits. The output contract table and the strings of the fix text come from the two scripts.
- **Answer to the open `libvulkan.so` question of `docs/features/dev-ops/solution-skeleton/context.md`.** The author installs `libvulkan-dev` one time with `sudo apt install libvulkan-dev`. The `loader` step checks for `libvulkan.so` in the folder of the x86-64 `libvulkan.so.1` entry of `ldconfig -p`. The scripts create no link and set no `LD_LIBRARY_PATH`.
- **Task 3.4, Verification Method results** (run from the repository root, `DISPLAY` unset in the shell):
  1. `dotnet --version`: `10.0.112`.
  2. Shebang `#!/usr/bin/env bash` in both scripts, both executable, `bash -n` clean, `exit=0`. `set -euo pipefail` exists once in each script.
  3. `scripts/check-gpu.sh`: four lines (kernel module 595.99.02, user space library 595.99.02, Vulkan ICD with `libGLX_nvidia.so.0` and API 1.4.329, `OK`), `exit=0`.
  4. `CHECK_GPU_PROC_FILE=<scratchpad>/proc-version`: `595.91` and `595.99.02`, a `check-gpu: FAIL` line, a `fix: Reboot ...` line, `exit=1`.
  5. `CHECK_GPU_ICD_FILE=/nonexistent`: the ICD FAIL and fix, both version lines, `exit=1`.
  6. `env -u DISPLAY -u WAYLAND_DISPLAY scripts/test-linux.sh`: `exit=0`, `out` holds `test-linux: PASS build, test, gpu, smoke (120 frames) in 10 s` (1 line), `err` holds 0 lines.
  7. Run by absolute path from the scratchpad folder: `test-linux: PASS build, test, gpu, smoke (120 frames) in 8 s`, `exit=0`.
  8. `BRUTAL_UI_FRAMES=0`: `FAIL at smoke (exit 2)`, then the `Usage: HelloPanel [--frames <N>]` line, then `HelloPanel: The value '0' is below 1.`, then the log path last. `exit=2`.
  9. `Xvfb` count 0 before and 0 after. `BRUTAL_UI_FRAMES=100000000 BRUTAL_UI_SMOKE_TIMEOUT=5`: `FAIL at smoke (exit 124), timed out after 5 s`, `exit=124`. The log tail was empty, so the block holds the header and the log path only.
  10. Temporary `src/BrutalUi.Core/Broken.cs` with `class Broken {`: `exit=1`, `FAIL at build (exit 1)`, and an `error CS1513` line in the tail. The file is deleted, and `git status --porcelain src/` prints nothing.
  11. `CHECK_GPU_PROC_FILE=<scratchpad>/proc-version scripts/test-linux.sh`: `FAIL at gpu (exit 1)`, the check-gpu lines, and the `Reboot` fix in the tail, `exit=1`.
  12. `VK_LOADER_DEBUG=all scripts/test-linux.sh`: PASS (11 s). `smoke.log` holds 12 lines with `nvidia_icd.json` and 0 lines with `lvp`.
  13. `sudo` and `apt` appear only inside printed fix text (`check-gpu.sh` line 103, `test-linux.sh` lines 85 to 130). No `docker` and no `jq`. The six literal strings exist in `scripts/test-linux.sh` (the build and test strings appear twice: the header comment and the `run_step` line). No `-c` option and no `dotnet test brutal-ui.sln`.
  14. `git status --porcelain` shows no path under `artifacts/`, `bin/`, or `obj/` (`.gitignore` lists all three). `.gitattributes` does not exist, so `git check-attr` did not run. `.gitattributes` belongs to `dev-ops/windows-test-path`.
- **Runbook check.** Each command in `docs/testing-linux.md` matches the scripts: the call, the five step commands, the success line, the failure headers, the fix texts, and the two variables with their defaults 120 and 60. `ste-lint.py --fail-over 2.5` on `docs/testing-linux.md`: 632 words, 5 findings, 0.79 per 100 words, exit 0. No fix needed.
- Deviation: none.

---

## Gotchas & Learnings

- `ldconfig -p` lists only the SONAME of a library (`libvulkan.so.1`), never the unversioned development link (`libvulkan.so`). To find the dev link, take the folder of the SONAME entry and test the file there.
- Read `ldconfig -p` into a variable before a search. A direct pipe into `grep -q` can end in `SIGPIPE` and fail under `pipefail`.
- `timeout` without `--foreground` moves itself into a new process group. A terminal `Ctrl+C` then does not reach it. The smoke step runs in the background with an `INT` and `TERM` trap that forwards `TERM` to `timeout`.
- A background job of a non-interactive script starts with `SIGINT` ignored, and a trap cannot reset it. A signal test driver must start the script with `env --default-signal=INT`. Only the smoke step runs in the background for this reason.
- `cd` with an exported `CDPATH` prints the folder to standard output. Use `CDPATH= cd -- ...`.
- `dotnet build` leaves MSBuild node-reuse servers alive after each run. They end by themselves after an idle time. They are not part of the smoke step.
- `env` and `timeout` on this machine come from uutils coreutils 0.8.0. `timeout` forwards `TERM` to its group as GNU `timeout` does.

---

## Testing Approach

### Integration Tests
- The script run itself is the integration test: build, unit tests, GPU check, and the Vulkan smoke run on the RTX 2060.
- Failure injection with no edit of the repository: `BRUTAL_UI_FRAMES=0`, `BRUTAL_UI_SMOKE_TIMEOUT=5`, `CHECK_GPU_PROC_FILE`, and `CHECK_GPU_ICD_FILE`.

### Manual Testing Checklist
- [ ] The desktop run (Definition of Done I5): the PASS line, and no window on the screen

---

## Needs Human Verification

`/autorun-feature` adds one line per item that the verifier cannot check, for a person to test.

- [ ] Phase 1 demo: the agent runs `env -u DISPLAY scripts/test-linux.sh` at the repository root, sees one line that starts with `test-linux: PASS`, and the exit code is 0.
- [ ] Phase 2 demo: the agent runs `scripts/check-gpu.sh`, sees the kernel module version, the user space library version, the Vulkan ICD, and `check-gpu: OK`, and the exit code is 0.
- [ ] Phase 3 demo: the author opens `docs/testing-linux.md`, runs each check command of its one-time setup table, runs the one script call that it names, and sees `test-linux: PASS`.
- [ ] Desktop run (Definition of Done I5): run `scripts/test-linux.sh` in a terminal of the desktop session. Expect the PASS line, and no window on the screen. The agent ran it only with no `DISPLAY`.

---

## System Dependencies

The scripts add no native file to the repository. They use these system items from the machine of the author.

| Item | Package | Used by |
|------|---------|---------|
| `dotnet` (SDK 10.0.x) | `dotnet-sdk-10.0` (Ubuntu archive) | `preflight`, `build`, `test`, `smoke` |
| `xvfb-run` and `Xvfb` | `xvfb` | `preflight`, `smoke` |
| `timeout` | `coreutils` | `preflight`, `smoke` |
| `ldconfig` | `libc-bin` | `loader`, `check-gpu.sh` |
| `libvulkan.so` | `libvulkan-dev` | `loader`, and `Brutal.Vulkan` at run time |
| The NVIDIA ICD (`/usr/share/vulkan/icd.d/nvidia_icd.json`) and `libnvidia-ml.so.1` | The NVIDIA driver (`libnvidia-gl-<branch>` and the driver package) | `check-gpu.sh`, `smoke` |

## Next Steps

### Immediate (Today/This Session)
1. The caller (`/autorun-epic`) commits the work and runs `/update-epic dev-ops` and `/update-master`.
2. A person tests the items of `## Needs Human Verification`, first the desktop run (I5).

### Short Term (This Week)
1. `dev-ops/windows-test-path`: its draft plan exists. It mirrors this script pattern in PowerShell and adds `.gitattributes` with `*.sh text eol=lf`.

### Follow-up
- Report to BRUTAL that `Brutal.Vulkan` must also try `libvulkan.so.1` on Linux. Then the `libvulkan-dev` requirement and the `loader` step can go.
- `github-ci` epic: a `SIGTERM` during the build or test step ends bash at once, and `dotnet` runs to its end. The script does not trap `SIGHUP`. Look at both when a runner cancel matters. Consider `MSBUILDDISABLENODEREUSE=1` on a hosted runner, so no MSBuild server outlives the job.
- `github-ci` epic: a hosted runner has no NVIDIA GPU. That epic must add an ICD choice or skip the `gpu` and `smoke` steps.

---

## Open Questions

- none

---

## Session Notes

### 2026-10-16 12:05
- Started the feature from `/autorun-feature` in the called mode of `/autorun-epic`.
- `libvulkan.so` is missing (see Blockers). `shellcheck` does not exist on this machine. `xvfb-run`, `timeout`, and `dotnet` exist.

### 2026-10-16 12:15
- Phase 1 done: tasks 1.1 to 1.7. See "Important Decisions", section Phase 1, for the check results. The real run still waited on `sudo apt install libvulkan-dev` (the shim results above are history).

### 2026-10-16 (fix 1 of phase 1: no-shim run)
- The author installed `libvulkan-dev` 1.4.341.0-1. `/usr/lib/x86_64-linux-gnu/libvulkan.so` links to `libvulkan.so.1`. `ldconfig -p` lists only `libvulkan.so.1 (libc6,x86-64) => /usr/lib/x86_64-linux-gnu/libvulkan.so.1`.
- Task 1.4 changed. The `loader` step in `scripts/test-linux.sh` now reads `ldconfig -p` into a variable, matches the exact entry `libvulkan.so.1` with `x86-64` (bash regex, no `libvulkan_*` driver matches), takes the folder of its path, and tests `-e "<folder>/libvulkan.so"`. The step does not search `ldconfig -p` for `libvulkan.so`. The header comment and the code comment describe the new lookup.
- `bash -n scripts/test-linux.sh`: OK.
- All checks ran with no shim (no `PATH` or `LD_LIBRARY_PATH` change):
  - (a) `env -u DISPLAY -u WAYLAND_DISPLAY scripts/test-linux.sh`: one line, `test-linux: PASS build, test, smoke (120 frames) in 8 s`, exit=0.
  - (b) absolute path from the scratchpad folder: `test-linux: PASS build, test, smoke (120 frames) in 6 s`, exit=0.
  - (c) `BRUTAL_UI_FRAMES=0`: `test-linux: FAIL at smoke (exit 2)`, `Usage: HelloPanel [--frames <N>]`, exit=2.
  - (d) `Xvfb` count 0 before and 0 after. `BRUTAL_UI_FRAMES=100000000 BRUTAL_UI_SMOKE_TIMEOUT=5`: `test-linux: FAIL at smoke (exit 124), timed out after 5 s`, exit=124.
  - (e) `VK_LOADER_DEBUG=all`: PASS. `smoke.log` holds 12 lines with `nvidia_icd.json` and 0 lines with `lvp`.
  - (f) `git status --porcelain`: no `artifacts/` path. Only the feature folder files and `scripts/` show.
- Failure path of the new lookup: a fake `ldconfig` in the scratchpad (`.../scratchpad/noloader/bin/ldconfig`) calls the real `/sbin/ldconfig` and removes the `libvulkan.so.1` line. With `PATH=<that bin>:$PATH`: `test-linux: FAIL at loader: the unversioned Vulkan loader libvulkan.so does not exist`, `test-linux: fix: sudo apt install libvulkan-dev`, exit=1.
- Deviation: the fake `ldconfig` filters the live output in place of a stored copy of it. The result is the same.

### 2026-10-16 12:40
- Verifier `phase-1-verify.md` (second run, no shim): PASS. All six literal strings present. The `loader` regex is anchored on `libvulkan.so.1` with `x86-64`. PASS line with no display (10 s). Exit 2 for `BRUTAL_UI_FRAMES=0`. Exit 124 for the time-out, with 0 `Xvfb` before and after. 12 `nvidia_icd.json` lines and 0 `lvp` lines with `VK_LOADER_DEBUG=all`. The loader failure path gives exit 1 and the fix. Demo: PASS.
- The first verifier run (with the `vkshim` shim) also passed, before the change of task 1.4. Its plain run showed the false `FAIL at loader` that the change removes.
- Open item for the review: the constants `SOLUTION`, `SAMPLE`, and `NVIDIA_ICD` are unused.

### 2026-10-16 (phase 2)
- Phase 2 done: tasks 2.1 to 2.7. See "Important Decisions", section Phase 2, for the check results.

### 2026-10-16 12:55
- Verifier `phase-2-verify.md`: PASS. `check-gpu.sh` prints four lines (595.99.02, 595.99.02, ICD with `libGLX_nvidia.so.0` and API 1.4.329, `OK`) and exits 0. The 595.91 file, a missing proc file, and a missing ICD file each give exit 1, the fix, and the other items. `test-linux.sh` prints `test-linux: PASS build, test, gpu, smoke (120 frames) in 11 s`. `FAIL at gpu (exit 1)` with the `Reboot` fix. The loader failure path still gives exit 1. Demo: PASS.
- Inbox: no open line.

### 2026-10-16 13:10
- Verifier `phase-3-verify.md`: PASS. The runbook holds seven sections. Each command and output string matches the scripts. Each setup check shows the item as present. Lint 0.79 per 100 words. The full run prints `test-linux: PASS build, test, gpu, smoke (120 frames) in 11 s`. Demo: PASS.
- Inbox: no open line.

### 2026-10-16 (review group 1: CR-1, CR-2, CR-3)
- CR-1: `run_step` runs the smoke step in the background, stores `step_pid`, and traps `INT` and `TERM`. The handler `stop_step` records the signal and sends `TERM` to `timeout`. `timeout` then sends `TERM` to its full process group. After the first `wait`, a loop waits again while the step is alive. Then the traps go away. After a signal the script writes `test-linux: FAIL at smoke: interrupted` and exits 130 (`INT`) or 143 (`TERM`). It never prints PASS. The traps go on before the step starts, and a check after the start kills the step when a signal came first. `--kill-after=10` stays. No `--foreground`.
- CR-1 decision: only the smoke step runs in the background. The build, test, and gpu steps stay in the foreground, so their code path does not change. Reason: bash starts a background job of a script with `SIGINT` ignored. A background `dotnet build` then ignores `Ctrl+C`. In the foreground, `Ctrl+C` reaches `dotnet` already (see CR-1).
- CR-2: `CDPATH= cd -- "$(dirname -- "${BASH_SOURCE[0]}")/.."`.
- CR-3: the preflight step checks `BRUTAL_UI_SMOKE_TIMEOUT` against `^[1-9][0-9]*$` after the `dotnet` version check, before the build. The runbook "Failures and fixes" table has the new row with the exact strings.
- Text changes: the script header lists exit codes 130 and 143, the new preflight check, and the valid range of `BRUTAL_UI_SMOKE_TIMEOUT`. The runbook adds the interrupt row to the exit code table (section 4), the check to step 1 (section 3), and the range to the settings table (section 5). The lines of CR-6 and CR-7 do not change.
- `bash -n scripts/test-linux.sh`: OK. The six literal strings stay in the script.
- Checks, all with no shim:
  - (a) `env -u DISPLAY -u WAYLAND_DISPLAY`: `test-linux: PASS build, test, gpu, smoke (120 frames) in 10 s`, 0 bytes on standard error, exit=0.
  - (b) `BRUTAL_UI_FRAMES=0`: `test-linux: FAIL at smoke (exit 2)`, `Usage:` line, exit=2.
  - (c) `Xvfb` 0 before, 0 after. `test-linux: FAIL at smoke (exit 124), timed out after 5 s`, exit=124.
  - (d) Driver `<scratch>/sigtest.sh`. `setsid` gives the script its own session. `SIGTERM` to the script PID: exit=143, the `interrupted` line, nothing on standard output, end in about 0.5 s. `SIGINT` to the group of the script: exit=130, the `interrupted` line, end in about 0.5 s. In both cases no `timeout`, `xvfb-run`, `Xvfb`, `dotnet run`, or `HelloPanel` process stays alive. `Xvfb` 0 before and 0 after each case.
  - (e) `abc`, `0`, `5s`, and `-3` each give `test-linux: FAIL at preflight: BRUTAL_UI_SMOKE_TIMEOUT <value> is not a whole number above 0`, the fix line, and exit=1. `artifacts/test-linux/` stays empty, so no build ran.
  - (f) From the scratchpad with `CDPATH=/tmp`, from the repository root with `CDPATH=/tmp`, and from the root with `CDPATH` at a decoy folder that holds `scripts/`: each prints only the PASS line, exit=0. The decoy folder gets no `artifacts/`.
  - (g) `git status --porcelain`: no `artifacts/` path.
- Test notes for (d):
  - The first `SIGINT` run did not stop the script. The driver started the script as a background job, so bash inherited `SIGINT` as ignored, and a trap cannot reset an ignored signal. The driver now starts the script with `env --default-signal=INT`. A terminal or a CI runner does not start the script with `SIGINT` ignored. This is a test artifact, not a script defect.
  - The build step leaves seven MSBuild node-reuse servers (`/nodeReuse:true`) in the group of the script. Every run leaves them, also a PASS run. They end by themselves after an idle time. They are not processes of the smoke step. The first driver run killed them by PID, with no effect on later runs.
  - The system `env` and `timeout` are uutils coreutils 0.8.0, not GNU. `timeout` forwards `TERM` to its group as GNU does.
- Follow-up (out of scope of CR-1): a `SIGTERM` to the script during the build or test step still ends bash at once, and `dotnet build` or `dotnet test` runs to its end. The script does not trap `SIGHUP`. Look at both in `github-ci`, when a runner cancel matters.

### 2026-10-16 (review group 2: CR-4, CR-6)
- CR-4: deleted the unused lines `SOLUTION`, `SAMPLE`, and `NVIDIA_ICD` from `scripts/test-linux.sh`. Added a comment above the smoke command: the literal ICD path must match the default `CHECK_GPU_ICD_FILE` of `scripts/check-gpu.sh`. A search for `$SOLUTION`, `$SAMPLE`, and `$NVIDIA_ICD` finds no match (also no match for the bare names). The six literal strings stay in the script (checked one by one).
- CR-6: the header comment of `scripts/test-linux.sh` and section 3 of `docs/testing-linux.md` now say: "The `preflight` and `loader` steps have no log. They write the problem and the fix to standard error." No other place in the runbook names a log for these two steps. The `ls -l` check in the setup table now reads "(the path on Ubuntu x86-64)" next to the command. The `ldconfig -p` note stays.
- Checks: `bash -n scripts/test-linux.sh`: OK. `env -u DISPLAY -u WAYLAND_DISPLAY scripts/test-linux.sh`: `test-linux: PASS build, test, gpu, smoke (120 frames) in 10 s`, exit=0. `ste-lint.py --fail-over 2.5 docs/testing-linux.md`: 0.89 per 100 words, exit=0.
- The findings stay unmarked in `code-review.md`. The parent marks them.

### 2026-10-16 (review group 3: CR-7)
- CR-7: split three long sentences. Comment and prose text only. No command, no output string, no code line changed.
  - `scripts/check-gpu.sh` header: "A driver update with no reboot is the most common problem. The kernel module in memory and the user space library on disk then have two versions."
  - `docs/testing-linux.md` section 1: "One script does the full path. It runs the build, the unit tests, and the GPU driver check. Then it runs the `HelloPanel` sample on the NVIDIA GPU inside Xvfb."
  - `scripts/test-linux.sh` header (found by the search, 33 words): "Prove the full path on a headless machine. The script runs the build, the unit tests, and the GPU driver check. Then it runs the HelloPanel sample on the NVIDIA GPU, inside Xvfb."
- A word count of all sentences in the header comments and the runbook prose found no other sentence over 25 words (the list and table lines are not sentences).
- Checks: `bash -n` on both scripts: OK. `scripts/check-gpu.sh`: four lines (kernel module 595.99.02, user space library 595.99.02, Vulkan ICD, OK), exit=0. `ste-lint.py --fail-over 2.5 docs/testing-linux.md`: 0.74 per 100 words, exit=0.
- The finding stays unmarked in `code-review.md`. The parent marks it.

### 2026-10-16 13:55
- Cross-phase review: `code-review.md` holds 7 findings (1 MEDIUM, 6 LOW). The Vercel React rules of `/review-feature` do not apply to bash, so the review used the plan quality bar, bash robustness, and `AGENTS.md`.
- Fixed: CR-1 (signal trap on the smoke step, exit 130 or 143), CR-2 (`CDPATH=` on the `cd`), CR-3 (`BRUTAL_UI_SMOKE_TIMEOUT` check in `preflight`), CR-4 (unused constants), CR-6 (the "no log" text and the Ubuntu path note), CR-7 (long sentences). Briefs `review-g1.md` to `review-g3.md`. CR-1 ran on the advanced agent, because it changes signal handling.
- Declined: CR-5 (the `ldconfig` lookup in both scripts). Each script must stand alone.
- Verifier `review-verify.md`: PASS on all 19 checks, with the Definition of Done steps 1 to 14, the signal path, the `CDPATH` run, and the timeout check.
- Inbox: 1 line, done (the change of task 1.4 from the coordinator).

### 2026-10-16 14:00
- Next feature draft: `docs/features/dev-ops/windows-test-path/implementation.md` (3 phases, 19 tasks, 6 open scope questions in Notes). Phase 1 is ADVANCED. Its top risk: the Windows name of the Vulkan loader that `Brutal.Vulkan` loads is not confirmed.
- Inbox change medium: Plan fix for task 1.4: do not use `ldconfig -p` to find the unversioned `libvulkan.so` (done: plan edited, code fixed by `phase-1-fix1.md`, verifier PASS with no shim)
- No open inbox line. No bug to promote.
- `implementation.md` status: Ready for Review.

---

*Update this file at the end of each work session. Run `/update-feature` before compacting conversations.*
