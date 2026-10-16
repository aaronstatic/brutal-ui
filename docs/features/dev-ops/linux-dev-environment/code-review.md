# dev-ops/linux-dev-environment - Code Review

**Reviewed:** 2026-10-16
**Reviewer:** Agent (bash robustness, plan quality bar, AGENTS.md rules)
**Skills Applied:** none (the Vercel React rules do not apply to bash scripts)
**Status:** Resolved - 7 findings, 7 resolved (6 fixed, 1 declined). Verifier `review-verify.md`: PASS.

---

## Summary

The scripts are sound. No finding gives a false PASS, and each failure path gives a non-zero exit code. `scripts/check-gpu.sh` prints the four contract lines, and it reports every item on a problem. One finding needs a fix before the later `github-ci` epic reuses the script: the smoke step does not handle `Ctrl+C` or `SIGTERM` (CR-1). The other findings are small.

Count by severity: 0 HIGH, 1 MEDIUM, 6 LOW.

I ran four checks to confirm findings. A model of the smoke step confirmed CR-1. A test of `cd` with `CDPATH` confirmed CR-2. A test of `timeout` with bad values confirmed CR-3. A run of `scripts/check-gpu.sh`, with and without its test seams, matched the contract. I ran no build and no full `test-linux.sh` run.

## Findings

### CR-1: A signal does not stop the smoke step, and `SIGTERM` leaves `Xvfb` running
**Severity:** MEDIUM
**Rule:** `process-cleanup`
**File:** `scripts/test-linux.sh:134-139` (the smoke step), `scripts/test-linux.sh:68` (the `run_step` call)
**Problem:** `timeout` runs without `--foreground`. It moves itself and its child into a new process group. A terminal `Ctrl+C` signals only the foreground group, so `SIGINT` reaches bash and does not reach `timeout`. Bash waits for the child, so the script ignores `Ctrl+C` until the smoke step ends. The script has no `trap`. A `SIGTERM` to the script alone (for example a CI cancel) kills bash at once, and the group of `timeout`, `Xvfb`, and `HelloPanel` keeps running until the time limit.

I confirmed both cases with a model of the smoke step, `timeout --kill-after=10 20 xvfb-run -a sleep 100` inside a `set -euo pipefail` script, in its own session:

- `SIGINT` to the group: the script stayed alive and ended only at the time limit (`rc=124`). A smoke step that ends with exit 0 after a `Ctrl+C` prints PASS.
- `SIGTERM` to the script: the script exited at once. `Xvfb` and `sleep` stayed alive after the check. I killed them by hand.

The build and test steps are not affected. Their children stay in the group of the script and receive `SIGINT`. The plan (Quality Bar, "A GPU stall ends in a time limit, not a hang") is met. The cleanup on an interrupt is not.
```bash
"$@" >"$log" 2>&1 || rc=$?
```
**Fix:** Run the step in the background, store its PID, and trap `INT` and `TERM`. `timeout` forwards `TERM` to its child group. For example:
```bash
"$@" >"$log" 2>&1 &
step_pid=$!
trap 'kill -TERM "$step_pid" 2>/dev/null' INT TERM
wait "$step_pid" || rc=$?
trap - INT TERM
```
Note: `wait` returns above 128 when a trap fires. Wait a second time for the real exit code, and exit 130 or 143 after the signal.
**Effort:** Medium

### CR-2: `cd` with an exported `CDPATH` prints to standard output and can change to a wrong folder
**Severity:** LOW
**Rule:** `output-contract`, `robustness (run from any folder)`
**File:** `scripts/test-linux.sh:38`
**Problem:** The relative call `scripts/test-linux.sh` makes `cd -- "scripts/.."`. When the caller exports `CDPATH`, bash searches `CDPATH` for `scripts/..` first. It then prints the new folder to standard output. This breaks the one-line output contract, and the script then runs in the wrong folder. I confirmed this with `CDPATH=<other folder>`: bash printed the folder and changed to it. The next lines run `rm -rf -- artifacts/test-linux` in that folder.
```bash
cd -- "$(dirname -- "${BASH_SOURCE[0]}")/.."
```
**Fix:** Clear `CDPATH` for this one command.
```bash
CDPATH= cd -- "$(dirname -- "${BASH_SOURCE[0]}")/.."
```
**Effort:** Low

### CR-3: `BRUTAL_UI_SMOKE_TIMEOUT` has no input check, and `0` turns the time limit off
**Severity:** LOW
**Rule:** `input-validation`
**File:** `scripts/test-linux.sh:44`, `scripts/test-linux.sh:138`
**Problem:** The value goes straight to `timeout`. I confirmed three cases:

- A word such as `abc` gives `timeout: invalid time interval` and exit 125. The error appears only after the build and the tests finish, so a typo costs minutes. The script then reports `FAIL at smoke (exit 125)`. No false PASS occurs.
- `0` makes `timeout` run with no limit (exit 0 for `timeout 0 true`). A stalled GPU then hangs the script, which the plan forbids.
- `5s` works, but the failure header then reads `timed out after 5s s`.

`BRUTAL_UI_FRAMES` needs no check. The plan wants a bad value to reach `HelloPanel` and give exit 2 (F7).
```bash
SMOKE_TIMEOUT="${BRUTAL_UI_SMOKE_TIMEOUT:-60}"
```
**Fix:** Check the value in the `preflight` step, before the build.
```bash
[[ $SMOKE_TIMEOUT =~ ^[1-9][0-9]*$ ]] ||
    fail preflight "BRUTAL_UI_SMOKE_TIMEOUT $SMOKE_TIMEOUT is not a whole number above 0" \
        "Set BRUTAL_UI_SMOKE_TIMEOUT to a number of seconds, for example 60."
```
Add the row to the failure table of the runbook.
**Effort:** Low

### CR-4: Three constants are unused, and the real values appear as literals
**Severity:** LOW
**Rule:** `dead-code`
**File:** `scripts/test-linux.sh:40-42`, `scripts/test-linux.sh:96`, `scripts/test-linux.sh:98`, `scripts/test-linux.sh:137`, `scripts/test-linux.sh:139`
**Problem:** `SOLUTION`, `SAMPLE`, and `NVIDIA_ICD` have no reader. The commands repeat their values as literals. The plan (Integration Criterion I1, Q5) needs the exact strings in the script, which explains the literals. The constants then mislead a reader, who thinks that a change of the constant changes the run. The ICD path also appears in `scripts/check-gpu.sh:32`, with a different default mechanism.
```bash
SOLUTION=brutal-ui.sln
SAMPLE=samples/HelloPanel/HelloPanel.csproj
NVIDIA_ICD=/usr/share/vulkan/icd.d/nvidia_icd.json
```
**Fix:** Delete the three lines. The literals stay, as the plan requires. Optionally, add a comment above the smoke step that says the literal ICD path must match the default of `check-gpu.sh`.
**Effort:** Low

### CR-5: The `ldconfig` lookup exists twice
**Severity:** LOW
**Rule:** `duplicate-logic`
**File:** `scripts/test-linux.sh:109-126`, `scripts/check-gpu.sh:52-72`
**Problem:** Both scripts pick `ldconfig` or `/sbin/ldconfig`, capture `ldconfig -p`, and match an x86-64 entry with the same regular expression and read loop. `test-linux.sh` repeats the logic inline for `libvulkan.so.1`. The two copies can drift apart.
**Fix:** Optional. Source a small helper file, or call `check-gpu.sh` for the loader check. Do nothing when the author prefers two stand-alone scripts. The two scripts then must keep the regular expression in step.
**Effort:** Medium
**Resolution:** Declined (2026-10-16). The author runs `check-gpu.sh` alone after a driver update, so each script must stand alone. A sourced helper adds a third file and a path lookup that the plan file tree does not hold. The copy is short, and the verifier checks both paths (the loader failure path and the GPU check). Keep the two copies in step by hand.

### CR-6: Two statements in the header and the runbook do not match the code
**Severity:** LOW
**Rule:** `runbook-accuracy`
**File:** `scripts/test-linux.sh:22`, `docs/testing-linux.md:15`, `docs/testing-linux.md:42`
**Problem:**

- The header comment and the runbook say "The preflight step has no log." The `loader` step has no log either, because it calls `fail`. The plan file tree names `preflight.log`. Its absence does not matter: the preflight message is one or two lines on standard error, and a log adds nothing. The text must name both steps, or a reader looks for `loader.log`.
- The runbook check for the Vulkan loader is `ls -l /usr/lib/x86_64-linux-gnu/libvulkan.so`. The script reads the folder from `ldconfig -p`. On Ubuntu both agree. The runbook should say that the path holds on Ubuntu only, or use the same method.

```
The `preflight` step has no log.
```
**Fix:** Change the text to "The `preflight` and `loader` steps have no log. They write the problem and the fix to standard error." Change it in both files.
**Effort:** Low

### CR-7: Some sentences are longer than the ASD-STE100 limit
**Severity:** LOW
**Rule:** `asd-ste100`
**File:** `scripts/check-gpu.sh:5-7`, `docs/testing-linux.md:5`
**Problem:** `AGENTS.md` asks for short sentences. These sentences have more than 25 words:

- `check-gpu.sh`: "The most common problem is a driver update with no reboot: the kernel module in memory and the user space library on disk then have two different versions." (28 words, and a colon joins two clauses).
- Runbook section 1: "One script does the full path: the build, the unit tests, the GPU driver check, and a short Vulkan run of the `HelloPanel` sample on the NVIDIA GPU inside Xvfb." (31 words).

The scripts and the runbook have no contractions, and the voice is active. The rest of the prose follows the rule.
**Fix:** Split each sentence. For example: "A driver update with no reboot is the most common problem. The kernel module in memory and the user space library on disk then have two versions." And: "One script does the full path. It runs the build, the unit tests, and the GPU driver check. Then it runs the `HelloPanel` sample on the NVIDIA GPU inside Xvfb."
**Effort:** Low

## What's Already Good

- The exit code is true. `run_step` captures the code with `|| rc=$?`, so `set -e` does not hide it. The `if` and `||` forms in `check-gpu.sh` (`[ -e ... ] && resolved=1`) do not trigger `errexit`.
- The output contract holds on the success path. Each step sends all output to a log. The success line is the only output. Failures print the header, at most 30 lines, and the log path.
- `check-gpu.sh` reports every item before it exits. With a wrong kernel version and no ICD file it printed both versions, two FAIL lines, and two fix lines, and exited 1.
- The scripts avoid `local x=$(...)`, a `grep` with no match that stops the script, and `SIGPIPE` problems. They read `ldconfig -p` once into a variable, and every `grep` pipe ends in `|| true`.
- Variables are quoted. The script uses `--` before paths. `cd` uses `BASH_SOURCE`, so the run works from any folder.
- The smoke command matches the plan: exact `dotnet` strings, no `-c`, `VK_ICD_FILENAMES` with the NVIDIA ICD only, `VK_DRIVER_FILES` and `VK_ADD_DRIVER_FILES` removed, `XDG_SESSION_TYPE=x11`, and `timeout --kill-after=10`. Without `--foreground`, a time-out stops `Xvfb` too (confirmed in the model).
- `sudo` and `apt` appear only inside fix text. The scripts call no `jq` and no `docker`.
- The version of the user space driver comes from the library file name, not from `nvidia-smi`. This works in the mismatch case.
- The runbook headers and commands match the strings of the scripts, and the exit code of `HelloPanel` matches `Program.cs` (1 for a runtime failure, 2 for a usage error, `Usage:` line).
- Reuse: `scripts/check-gpu.sh` has no repository dependency, and the failure contract is easy to mirror in PowerShell. No finding blocks `dev-ops/windows-test-path` or `github-ci`. A hosted runner has no NVIDIA GPU, but the plan defers that need (Decision 10, YAGNI).

## Recommended Fix Order

| Order | Finding | Impact | Effort |
|-------|---------|--------|--------|
| 1 | CR-3 | The time limit can be off, and a typo costs minutes | Low |
| 2 | CR-2 | A wrong folder and a broken output contract, with one `CDPATH` | Low |
| 3 | CR-4 | Misleading constants | Low |
| 4 | CR-6 | Wrong text in the header and the runbook | Low |
| 5 | CR-7 | Prose style | Low |
| 6 | CR-1 | Leaked `Xvfb` and no `Ctrl+C` during smoke. Do it before `github-ci`. | Medium |
| 7 | CR-5 | Drift risk only | Medium |

## Progress

- [x] CR-1: A signal does not stop the smoke step, and `SIGTERM` leaves `Xvfb` running (fixed, `review-g1.md`: trap, exit 130 or 143, no process left)
- [x] CR-2: `cd` with an exported `CDPATH` prints to standard output and can change to a wrong folder (fixed, `review-g1.md`)
- [x] CR-3: `BRUTAL_UI_SMOKE_TIMEOUT` has no input check, and `0` turns the time limit off (fixed, `review-g1.md`: preflight check, runbook row)
- [x] CR-4: Three constants are unused, and the real values appear as literals (fixed, `review-g2.md`)
- [x] CR-5: The `ldconfig` lookup exists twice (declined, see the Resolution line)
- [x] CR-6: Two statements in the header and the runbook do not match the code (fixed, `review-g2.md`)
- [x] CR-7: Some sentences are longer than the ASD-STE100 limit (fixed, `review-g3.md`)
